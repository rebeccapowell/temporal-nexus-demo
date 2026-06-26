using ShoppingBasket.Contracts;

namespace Storefront.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        app.MapGet("/api/products", () => Results.Ok(DemoProducts.Catalogue));
    }
}
