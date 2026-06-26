using Checkout.Worker.Workflows;
using ShoppingBasket.Contracts;
using Temporalio.Client;

namespace Storefront.Api.Endpoints;

public static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/checkouts/{checkoutId}");

        group.MapGet("/", async (string checkoutId, ITemporalClient temporal) =>
        {
            var handle = temporal.GetWorkflowHandle<CheckoutWorkflow>(checkoutId);
            var state = await handle.QueryAsync(workflow => workflow.GetCheckoutState(checkoutId));
            return Results.Ok(state);
        });

        group.MapPost("/shipping-address", async (
            string checkoutId,
            ProvideShippingAddressCommand command,
            ITemporalClient temporal) =>
        {
            var handle = temporal.GetWorkflowHandle<CheckoutWorkflow>(checkoutId);
            await handle.ExecuteUpdateAsync(workflow => workflow.ProvideShippingAddressAsync(command));
            return Results.Ok(new { success = true });
        });

        group.MapPost("/payment-method", async (
            string checkoutId,
            ProvidePaymentMethodCommand command,
            ITemporalClient temporal) =>
        {
            var handle = temporal.GetWorkflowHandle<CheckoutWorkflow>(checkoutId);
            await handle.ExecuteUpdateAsync(workflow => workflow.ProvidePaymentMethodAsync(command));
            return Results.Ok(new { success = true });
        });

        group.MapPost("/retry-payment", async (string checkoutId, ITemporalClient temporal) =>
        {
            var handle = temporal.GetWorkflowHandle<CheckoutWorkflow>(checkoutId);
            await handle.ExecuteUpdateAsync(workflow => workflow.RetryPaymentAsync());
            return Results.Ok(new { success = true });
        });

        group.MapPost("/cancel", async (string checkoutId, ITemporalClient temporal) =>
        {
            var handle = temporal.GetWorkflowHandle<CheckoutWorkflow>(checkoutId);
            await handle.ExecuteUpdateAsync(workflow => workflow.CancelCheckoutAsync());
            return Results.Ok(new { success = true });
        });
    }
}
