using ShoppingBasket.Contracts;
using NexusRpc;

namespace ShoppingBasket.NexusContracts;

public record RequestFulfillmentInput(
    string CheckoutId,
    IReadOnlyList<BasketItem> Items,
    ShippingAddress ShippingAddress,
    string Email);

public record RequestFulfillmentOutput(bool Success, string? TrackingNumber, string? FailureReason);

[NexusService]
public interface IFulfillmentNexusService
{
    [NexusOperation]
    RequestFulfillmentOutput RequestFulfillment(RequestFulfillmentInput input);
}
