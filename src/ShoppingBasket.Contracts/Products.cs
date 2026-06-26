namespace ShoppingBasket.Contracts;

public record Product(string Sku, string Name, string Description, decimal Price, string ImageUrl);

public static class DemoProducts
{
    public static readonly IReadOnlyList<Product> Catalogue =
    [
        new("SKU-001", "Temporal Hoodie", "Stay warm while your workflows run forever.", 49.99m, "/images/hoodie.png"),
        new("SKU-002", "Nexus Cap", "Cross-namespace style, right on your head.", 24.99m, "/images/cap.png"),
        new("SKU-003", "Durable Mug", "Coffee persists. So do your workflows.", 14.99m, "/images/mug.png"),
        new("SKU-004", "Signal Tee", "Send signals everywhere you go.", 29.99m, "/images/tee.png"),
    ];
}
