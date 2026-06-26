using Checkout.Worker.Activities;
using Microsoft.Extensions.Logging;
using ShoppingBasket.Contracts;
using ShoppingBasket.NexusContracts;
using Temporalio.Workflows;

namespace Checkout.Worker.Workflows;

[Workflow]
public class CheckoutWorkflow
{
    private ShippingAddress? _shippingAddress;
    private PaymentMethod? _payment;
    private bool _emailVerified;
    private bool _cancelled;
    private bool _retryPayment;
    private CheckoutStatus _status = CheckoutStatus.WaitingForConditions;
    private string? _failureReason;

    [WorkflowRun]
    public async Task RunAsync(CheckoutStart input)
    {
        Workflow.Logger.LogInformation("Checkout started: {CheckoutId}", input.CheckoutId);

        _status = CheckoutStatus.WaitingForConditions;
        var conditionsMet = await Workflow.WaitConditionAsync(
            () => ((_shippingAddress is not null && _payment is not null && _emailVerified) || _cancelled),
            timeout: TimeSpan.FromHours(24));

        if (_cancelled || !conditionsMet)
        {
            _status = CheckoutStatus.Cancelled;
            Workflow.Logger.LogInformation("Checkout {CheckoutId} cancelled or timed out", input.CheckoutId);
            await Workflow.ExecuteActivityAsync(
                (CheckoutActivities a) => a.NotifyPurchaseIntentCheckoutFailedAsync(
                    input.PurchaseIntentId, input.CheckoutId, "Checkout was cancelled or timed out."),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(1) });
            return;
        }

        var paymentSnapshot = _payment!;
        var addressSnapshot = _shippingAddress!;
        var totalAmount = input.Items.Sum(item => item.UnitPrice * item.Quantity);

        _status = CheckoutStatus.ProcessingInventory;
        var inventoryClient = Workflow.CreateNexusWorkflowClient<IInventoryNexusService>("inventory-service");
        var inventoryResult = await inventoryClient.ExecuteNexusOperationAsync(
            service => service.ReserveInventory(new ReserveInventoryInput(input.CheckoutId, input.Items)),
            new NexusWorkflowOperationOptions { ScheduleToCloseTimeout = TimeSpan.FromMinutes(5) });

        if (!inventoryResult.Success)
        {
            _status = CheckoutStatus.Failed;
            _failureReason = inventoryResult.FailureReason ?? "Inventory reservation failed.";
            Workflow.Logger.LogWarning("Inventory failed for {CheckoutId}: {Reason}", input.CheckoutId, _failureReason);
            var inventoryFailReason = _failureReason;
            await Workflow.ExecuteActivityAsync(
                (CheckoutActivities a) => a.NotifyPurchaseIntentCheckoutFailedAsync(
                    input.PurchaseIntentId, input.CheckoutId, inventoryFailReason),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(1) });
            return;
        }

        _status = CheckoutStatus.ProcessingPayment;
        var paymentClient = Workflow.CreateNexusWorkflowClient<IPaymentNexusService>("payment-service");

        AuthorizePaymentOutput paymentResult;
        while (true)
        {
            _retryPayment = false;
            paymentResult = await paymentClient.ExecuteNexusOperationAsync(
                service => service.AuthorizePayment(
                    new AuthorizePaymentInput(input.CheckoutId, totalAmount, paymentSnapshot)),
                new NexusWorkflowOperationOptions { ScheduleToCloseTimeout = TimeSpan.FromMinutes(5) });

            if (paymentResult.Success) break;

            _status = CheckoutStatus.PaymentDeclined;
            _failureReason = paymentResult.FailureReason ?? "Payment authorization failed.";
            Workflow.Logger.LogWarning("Payment declined for {CheckoutId}: {Reason}", input.CheckoutId, _failureReason);

            // Wait up to 30 minutes for the user to retry or cancel.
            var shouldContinue = await Workflow.WaitConditionAsync(
                () => _retryPayment || _cancelled,
                timeout: TimeSpan.FromMinutes(30));

            if (!shouldContinue || _cancelled)
            {
                _status = CheckoutStatus.Failed;
                var paymentFailReason = _failureReason;
                await Workflow.ExecuteActivityAsync(
                    (CheckoutActivities a) => a.NotifyPurchaseIntentCheckoutFailedAsync(
                        input.PurchaseIntentId, input.CheckoutId, paymentFailReason),
                    new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(1) });
                return;
            }

            _failureReason = null;
            _status = CheckoutStatus.ProcessingPayment;
        }

        _status = CheckoutStatus.ProcessingFulfillment;
        var fulfillmentClient = Workflow.CreateNexusWorkflowClient<IFulfillmentNexusService>("fulfillment-service");
        var fulfillmentResult = await fulfillmentClient.ExecuteNexusOperationAsync(
            service => service.RequestFulfillment(
                new RequestFulfillmentInput(input.CheckoutId, input.Items, addressSnapshot, input.Email)),
            new NexusWorkflowOperationOptions { ScheduleToCloseTimeout = TimeSpan.FromMinutes(5) });

        if (!fulfillmentResult.Success)
        {
            _status = CheckoutStatus.Failed;
            _failureReason = fulfillmentResult.FailureReason ?? "Fulfillment request failed.";
            var fulfillFailReason = _failureReason;
            await Workflow.ExecuteActivityAsync(
                (CheckoutActivities a) => a.NotifyPurchaseIntentCheckoutFailedAsync(
                    input.PurchaseIntentId, input.CheckoutId, fulfillFailReason),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(1) });
            return;
        }

        await Workflow.ExecuteActivityAsync(
            (CheckoutActivities activities) => activities.SendConfirmationEmailAsync(
                input.Email,
                input.CheckoutId,
                fulfillmentResult.TrackingNumber ?? "N/A"),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(2) });

        _status = CheckoutStatus.Completed;
        Workflow.Logger.LogInformation("Checkout completed: {CheckoutId}", input.CheckoutId);

        await Workflow.ExecuteActivityAsync(
            (CheckoutActivities a) => a.NotifyPurchaseIntentCheckoutCompletedAsync(
                input.PurchaseIntentId, input.CheckoutId),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(1) });
    }

    [WorkflowQuery]
    public CheckoutState GetCheckoutState(string checkoutId) =>
        new(
            checkoutId,
            string.Empty,
            _status,
            _failureReason,
            _shippingAddress,
            _payment,
            _emailVerified,
            Workflow.UtcNow,
            _status == CheckoutStatus.Completed ? Workflow.UtcNow : null);

    [WorkflowUpdate]
    public Task ProvideShippingAddressAsync(ProvideShippingAddressCommand command)
    {
        _shippingAddress = command.Address;
        return Task.CompletedTask;
    }

    [WorkflowUpdate]
    public Task ProvidePaymentMethodAsync(ProvidePaymentMethodCommand command)
    {
        _payment = command.Payment;
        return Task.CompletedTask;
    }

    [WorkflowUpdate]
    public Task RetryPaymentAsync()
    {
        _retryPayment = true;
        return Task.CompletedTask;
    }

    [WorkflowUpdate]
    public Task CancelCheckoutAsync()
    {
        _cancelled = true;
        return Task.CompletedTask;
    }

    [WorkflowSignal]
    public Task EmailVerifiedAsync()
    {
        _emailVerified = true;
        return Task.CompletedTask;
    }
}
