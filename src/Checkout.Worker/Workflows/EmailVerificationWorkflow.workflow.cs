using Checkout.Worker.Activities;
using Microsoft.Extensions.Logging;
using ShoppingBasket.Contracts;
using Temporalio.Workflows;

namespace Checkout.Worker.Workflows;

[Workflow]
public class EmailVerificationWorkflow
{
    private bool _verified;
    private string? _expectedToken;
    private string? _checkoutId;

    [WorkflowRun]
    public async Task RunAsync(EmailVerificationStart input)
    {
        Workflow.Logger.LogInformation("Email verification started for purchase intent {Id}", input.PurchaseIntentId);

        _expectedToken = input.VerificationToken;

        await Workflow.ExecuteActivityAsync(
            (CheckoutActivities activities) =>
                activities.SendVerificationEmailAsync(input.Email, input.VerificationToken, input.PurchaseIntentId),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(2) });

        var verified = await Workflow.WaitConditionAsync(() => _verified, timeout: TimeSpan.FromHours(24));
        if (!verified)
        {
            Workflow.Logger.LogWarning("Email verification timed out for purchase intent {Id}", input.PurchaseIntentId);
            return;
        }

        // Notify PurchaseIntentWorkflow
        await Workflow.ExecuteActivityAsync(
            (CheckoutActivities activities) => activities.NotifyEmailVerifiedAsync(input.PurchaseIntentId),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(1) });

        // If checkout started while we were waiting, unblock it now.
        if (_checkoutId is { } checkoutId)
        {
            Workflow.Logger.LogInformation("Forwarding email-verified to active checkout {Id}", checkoutId);
            await Workflow.ExecuteActivityAsync(
                (CheckoutActivities activities) => activities.ForwardEmailVerifiedToCheckoutAsync(checkoutId),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(1) });
        }

        Workflow.Logger.LogInformation("Email verified for purchase intent {Id}", input.PurchaseIntentId);
    }

    [WorkflowSignal]
    public Task RegisterCheckoutAsync(string checkoutId)
    {
        _checkoutId = checkoutId;
        Workflow.Logger.LogInformation("Checkout {Id} registered with email verification workflow", checkoutId);
        return Task.CompletedTask;
    }

    [WorkflowSignal]
    public Task VerifyAsync(string token)
    {
        if (token == _expectedToken)
        {
            _verified = true;
            Workflow.Logger.LogInformation("Verification token accepted");
        }
        else
        {
            Workflow.Logger.LogWarning("Verification token rejected — mismatch");
        }
        return Task.CompletedTask;
    }
}
