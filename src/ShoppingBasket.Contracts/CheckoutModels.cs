namespace ShoppingBasket.Contracts;

public record CheckoutStart(
    string CheckoutId,
    string PurchaseIntentId,
    IReadOnlyList<BasketItem> Items,
    string Email);

public record ShippingAddress(
    string FullName,
    string Line1,
    string? Line2,
    string City,
    string PostalCode,
    string Country);

public record PaymentMethod(
    string CardHolder,
    string MaskedNumber,
    string ExpiryMonth,
    string ExpiryYear);

public record ProvideShippingAddressCommand(ShippingAddress Address);

public record ProvidePaymentMethodCommand(PaymentMethod Payment);

public enum CheckoutStatus
{
    WaitingForConditions,
    ProcessingInventory,
    ProcessingPayment,
    ProcessingFulfillment,
    Completed,
    Failed,
    Cancelled
}

public record CheckoutState(
    string CheckoutId,
    string PurchaseIntentId,
    CheckoutStatus Status,
    string? FailureReason,
    ShippingAddress? ShippingAddress,
    PaymentMethod? Payment,
    bool EmailVerified,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);

public record EmailVerificationStart(
    string VerificationId,
    string PurchaseIntentId,
    string Email,
    string VerificationToken);
