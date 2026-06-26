using ShoppingBasket.Contracts;
using Microsoft.Extensions.Logging;
using Temporalio.Workflows;

namespace PurchaseIntent.Worker.Workflows;

[Workflow]
public class PurchaseIntentWorkflow
{
    private readonly List<BasketItem> _items = [];
    private string? _email;
    private bool _emailVerified;
    private string? _checkoutId;
    private PurchaseIntentStatus _status = PurchaseIntentStatus.Active;
    private string? _failureReason;

    [WorkflowRun]
    public async Task RunAsync(PurchaseIntentStart input)
    {
        Workflow.Logger.LogInformation("Purchase intent created: {Id}", input.PurchaseIntentId);

        var finished = await Workflow.WaitConditionAsync(
            () => _status is PurchaseIntentStatus.CheckoutCompleted
                or PurchaseIntentStatus.CheckoutFailed
                or PurchaseIntentStatus.Abandoned,
            timeout: TimeSpan.FromDays(30));

        if (!finished && _status == PurchaseIntentStatus.Active)
        {
            _status = PurchaseIntentStatus.Abandoned;
        }

        Workflow.Logger.LogInformation(
            "Purchase intent {Id} ended with status {Status}",
            input.PurchaseIntentId,
            _status);
    }

    [WorkflowQuery]
    public PurchaseIntentState GetPurchaseIntent(string purchaseIntentId) =>
        new(
            purchaseIntentId,
            _items.AsReadOnly(),
            _email,
            _emailVerified,
            _status,
            _checkoutId,
            Workflow.UtcNow);

    [WorkflowQuery]
    public string? GetFailureReason() => _failureReason;

    [WorkflowUpdate]
    public Task AddItemAsync(AddItemCommand command)
    {
        var existing = _items.FirstOrDefault(item => item.Sku == command.Sku);
        if (existing is not null)
        {
            _items.Remove(existing);
            _items.Add(existing with { Quantity = existing.Quantity + command.Quantity });
        }
        else
        {
            var product = DemoProducts.Catalogue.FirstOrDefault(item => item.Sku == command.Sku)
                ?? throw new ApplicationException($"Product {command.Sku} not found.");
            _items.Add(new BasketItem(command.Sku, product.Name, product.Price, command.Quantity));
        }

        Workflow.Logger.LogInformation("Item added: {Sku} x{Qty}", command.Sku, command.Quantity);
        return Task.CompletedTask;
    }

    [WorkflowUpdate]
    public Task UpdateQuantityAsync(string sku, UpdateQuantityCommand command)
    {
        var existing = _items.FirstOrDefault(item => item.Sku == sku)
            ?? throw new ApplicationException($"Item {sku} not in basket.");

        if (command.Quantity <= 0)
        {
            _items.Remove(existing);
        }
        else
        {
            _items[_items.IndexOf(existing)] = existing with { Quantity = command.Quantity };
        }

        return Task.CompletedTask;
    }

    [WorkflowUpdate]
    public Task ProvideEmailAsync(ProvideEmailCommand command)
    {
        _email = command.Email;
        Workflow.Logger.LogInformation("Email provided for purchase intent (not logged)");
        return Task.CompletedTask;
    }

    [WorkflowUpdate]
    public Task StartCheckoutAsync(string checkoutId)
    {
        _checkoutId = checkoutId;
        _status = PurchaseIntentStatus.CheckoutStarted;
        Workflow.Logger.LogInformation("Checkout started: {CheckoutId}", checkoutId);
        return Task.CompletedTask;
    }

    [WorkflowSignal]
    public Task CheckoutCompletedAsync(string checkoutId)
    {
        if (_checkoutId == checkoutId)
        {
            _status = PurchaseIntentStatus.CheckoutCompleted;
        }

        return Task.CompletedTask;
    }

    [WorkflowSignal]
    public Task CheckoutFailedAsync(string checkoutId, string reason)
    {
        if (_checkoutId == checkoutId)
        {
            _status = PurchaseIntentStatus.CheckoutFailed;
            _failureReason = reason;
        }

        return Task.CompletedTask;
    }

    [WorkflowSignal]
    public Task EmailVerifiedAsync()
    {
        _emailVerified = true;
        Workflow.Logger.LogInformation("Email verified for purchase intent");
        return Task.CompletedTask;
    }
}
