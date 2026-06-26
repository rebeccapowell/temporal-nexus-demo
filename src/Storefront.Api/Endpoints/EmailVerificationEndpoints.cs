using Checkout.Worker.Workflows;
using Temporalio.Client;

namespace Storefront.Api.Endpoints;

public static class EmailVerificationEndpoints
{
    public static void MapEmailVerificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/email-verification");

        group.MapGet("/confirm", async (
            string purchaseIntentId,
            string token,
            ITemporalClient temporal) =>
        {
            var verificationId = $"email-verify-{purchaseIntentId}";
            var handle = temporal.GetWorkflowHandle<EmailVerificationWorkflow>(verificationId);
            await handle.SignalAsync(workflow => workflow.VerifyAsync(token));
            return Results.Ok(new { message = "Email verified. You can close this window." });
        });

        group.MapPost("/confirm", async (
            EmailConfirmRequest request,
            ITemporalClient temporal) =>
        {
            var verificationId = $"email-verify-{request.PurchaseIntentId}";
            var handle = temporal.GetWorkflowHandle<EmailVerificationWorkflow>(verificationId);
            await handle.SignalAsync(workflow => workflow.VerifyAsync(request.Token));
            return Results.Ok(new { success = true });
        });
    }
}

public record EmailConfirmRequest(string PurchaseIntentId, string Token);
