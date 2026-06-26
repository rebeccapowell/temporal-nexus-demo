namespace Storefront.Api.Infrastructure;

public class PurchaseIntentCookieManager
{
    private const string CookieName = "demo_purchase_intent_id";

    public string? GetPurchaseIntentId(HttpContext context) =>
        context.Request.Cookies.TryGetValue(CookieName, out var id) ? id : null;

    public string GetOrCreatePurchaseIntentId(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(CookieName, out var existing))
        {
            return existing;
        }

        var newId = $"pi-{Guid.NewGuid():N}";
        SetPurchaseIntentId(context, newId);
        return newId;
    }

    public void SetPurchaseIntentId(HttpContext context, string id)
    {
        context.Response.Cookies.Append(CookieName, id, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            IsEssential = true
        });
    }

    public void ClearCookie(HttpContext context) =>
        context.Response.Cookies.Delete(CookieName);
}
