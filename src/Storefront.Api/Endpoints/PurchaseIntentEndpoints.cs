using Checkout.Worker.Workflows;
using PurchaseIntent.Worker.Workflows;
using ShoppingBasket.Contracts;
using Storefront.Api.Infrastructure;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;

namespace Storefront.Api.Endpoints;

public static class PurchaseIntentEndpoints
{
    public static void MapPurchaseIntentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/purchase-intent");

        group.MapGet("/current", async (
            HttpContext ctx,
            PurchaseIntentCookieManager cookies,
            ITemporalClient temporal) =>
        {
            var id = cookies.GetOrCreatePurchaseIntentId(ctx);
            var workflowId = $"purchase-intent-{id}";

            try
            {
                var handle = temporal.GetWorkflowHandle<PurchaseIntentWorkflow>(workflowId);
                var state = await handle.QueryAsync(workflow => workflow.GetPurchaseIntent(id));
                return Results.Ok(state);
            }
            catch (Exception ex) when (ex.Message.Contains("workflow not found", StringComparison.OrdinalIgnoreCase) ||
                                       ex.Message.Contains("not exist", StringComparison.OrdinalIgnoreCase))
            {
                await temporal.StartWorkflowAsync(
                    (PurchaseIntentWorkflow workflow) => workflow.RunAsync(new PurchaseIntentStart(id)),
                    new WorkflowOptions
                    {
                        Id = workflowId,
                        TaskQueue = "purchase-intent-queue"
                    });

                var handle = temporal.GetWorkflowHandle<PurchaseIntentWorkflow>(workflowId);
                var state = await handle.QueryAsync(workflow => workflow.GetPurchaseIntent(id));
                return Results.Ok(state);
            }
        });

        group.MapPost("/items", async (
            AddItemCommand command,
            HttpContext ctx,
            PurchaseIntentCookieManager cookies,
            ITemporalClient temporal) =>
        {
            var id = cookies.GetOrCreatePurchaseIntentId(ctx);
            var handle = temporal.GetWorkflowHandle<PurchaseIntentWorkflow>($"purchase-intent-{id}");
            await handle.ExecuteUpdateAsync(workflow => workflow.AddItemAsync(command));
            return Results.Ok(new { success = true });
        });

        group.MapPut("/items/{sku}", async (
            string sku,
            UpdateQuantityCommand command,
            HttpContext ctx,
            PurchaseIntentCookieManager cookies,
            ITemporalClient temporal) =>
        {
            var id = cookies.GetOrCreatePurchaseIntentId(ctx);
            var handle = temporal.GetWorkflowHandle<PurchaseIntentWorkflow>($"purchase-intent-{id}");
            await handle.ExecuteUpdateAsync(workflow => workflow.UpdateQuantityAsync(sku, command));
            return Results.Ok(new { success = true });
        });

        group.MapDelete("/items/{sku}", async (
            string sku,
            HttpContext ctx,
            PurchaseIntentCookieManager cookies,
            ITemporalClient temporal) =>
        {
            var id = cookies.GetOrCreatePurchaseIntentId(ctx);
            var handle = temporal.GetWorkflowHandle<PurchaseIntentWorkflow>($"purchase-intent-{id}");
            await handle.ExecuteUpdateAsync(workflow => workflow.UpdateQuantityAsync(sku, new UpdateQuantityCommand(0)));
            return Results.Ok(new { success = true });
        });

        group.MapPost("/email", async (
            ProvideEmailCommand command,
            HttpContext ctx,
            PurchaseIntentCookieManager cookies,
            ITemporalClient temporal) =>
        {
            var id = cookies.GetOrCreatePurchaseIntentId(ctx);
            var handle = temporal.GetWorkflowHandle<PurchaseIntentWorkflow>($"purchase-intent-{id}");
            await handle.ExecuteUpdateAsync(workflow => workflow.ProvideEmailAsync(command));

            var verificationId = $"email-verify-{id}";
            var token = Guid.NewGuid().ToString("N");

            try
            {
                await temporal.StartWorkflowAsync(
                    (EmailVerificationWorkflow workflow) => workflow.RunAsync(
                        new EmailVerificationStart(verificationId, id, command.Email, token)),
                    new WorkflowOptions
                    {
                        Id = verificationId,
                        TaskQueue = "checkout-queue",
                        IdConflictPolicy = WorkflowIdConflictPolicy.TerminateExisting
                    });
            }
            catch
            {
            }

            return Results.Ok(new { verificationId });
        });

        group.MapPost("/checkout", async (
            HttpContext ctx,
            PurchaseIntentCookieManager cookies,
            ITemporalClient temporal) =>
        {
            var id = cookies.GetOrCreatePurchaseIntentId(ctx);
            var piHandle = temporal.GetWorkflowHandle<PurchaseIntentWorkflow>($"purchase-intent-{id}");
            var piState = await piHandle.QueryAsync(workflow => workflow.GetPurchaseIntent(id));

            if (!piState.Items.Any())
            {
                return Results.BadRequest(new { error = "Basket is empty." });
            }

            if (string.IsNullOrWhiteSpace(piState.Email))
            {
                return Results.BadRequest(new { error = "Email is required." });
            }

            var checkoutId = $"checkout-{Guid.NewGuid():N}";

            await temporal.StartWorkflowAsync(
                (CheckoutWorkflow workflow) => workflow.RunAsync(
                    new CheckoutStart(checkoutId, id, piState.Items, piState.Email!)),
                new WorkflowOptions
                {
                    Id = checkoutId,
                    TaskQueue = "checkout-queue"
                });

            await piHandle.ExecuteUpdateAsync(workflow => workflow.StartCheckoutAsync(checkoutId));

            if (piState.EmailVerified)
            {
                var checkoutHandle = temporal.GetWorkflowHandle<CheckoutWorkflow>(checkoutId);
                await checkoutHandle.SignalAsync(workflow => workflow.EmailVerifiedAsync());
            }

            return Results.Ok(new { checkoutId });
        });
    }
}
