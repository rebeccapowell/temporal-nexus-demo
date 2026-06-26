using Storefront.Api.Infrastructure;

namespace Storefront.Api.Endpoints;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/session");

        group.MapGet("/", (HttpContext ctx, PurchaseIntentCookieManager cookies) =>
            Results.Ok(new { purchaseIntentId = cookies.GetPurchaseIntentId(ctx) }));

        group.MapPost("/new-clean-customer", (HttpContext ctx, PurchaseIntentCookieManager cookies) =>
        {
            var newId = $"pi-{Guid.NewGuid():N}";
            cookies.SetPurchaseIntentId(ctx, newId);
            return Results.Ok(new { purchaseIntentId = newId });
        });

        group.MapPost("/clear-cookie", (HttpContext ctx, PurchaseIntentCookieManager cookies) =>
        {
            cookies.ClearCookie(ctx);
            return Results.Ok();
        });
    }
}
