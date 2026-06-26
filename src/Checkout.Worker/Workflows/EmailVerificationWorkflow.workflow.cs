using Checkout.Worker.Activities;
using Microsoft.Extensions.Logging;
using ShoppingBasket.Contracts;
using Temporalio.Activities;
using Temporalio.Workflows;

namespace Checkout.Worker.Workflows;

[Workflow]
public class EmailVerificationWorkflow
{
    private bool _verified;

    [WorkflowRun]
    public async Task RunAsync(EmailVerificationStart input)
    {
        Workflow.Logger.LogInformation("Email verification started for purchase intent {Id}", input.PurchaseIntentId);

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

        await Workflow.ExecuteActivityAsync(
            (CheckoutActivities activities) => activities.NotifyEmailVerifiedAsync(input.PurchaseIntentId),
            new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(1) });

        Workflow.Logger.LogInformation("Email verified for purchase intent {Id}", input.PurchaseIntentId);
    }

    [WorkflowSignal]
    public Task VerifyAsync(string token)
    {
        _verified = true;
        Workflow.Logger.LogInformation("Verification token received");
        return Task.CompletedTask;
    }
}
