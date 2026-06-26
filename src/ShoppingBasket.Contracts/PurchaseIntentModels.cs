namespace ShoppingBasket.Contracts;

public record PurchaseIntentStart(string PurchaseIntentId);

public record BasketItem(string Sku, string Name, decimal UnitPrice, int Quantity);

public record AddItemCommand(string Sku, int Quantity);

public record UpdateQuantityCommand(int Quantity);

public record ProvideEmailCommand(string Email);

public enum PurchaseIntentStatus
{
    Active,
    CheckoutStarted,
    CheckoutCompleted,
    CheckoutFailed,
    Abandoned
}

public record PurchaseIntentState(
    string PurchaseIntentId,
    IReadOnlyList<BasketItem> Items,
    string? Email,
    bool EmailVerified,
    PurchaseIntentStatus Status,
    string? CheckoutId,
    DateTimeOffset CreatedAt);
