using ShoppingBasket.Contracts;
using NexusRpc;

namespace ShoppingBasket.NexusContracts;

public record ReserveInventoryInput(string CheckoutId, IReadOnlyList<BasketItem> Items);

public record ReserveInventoryOutput(bool Success, string? FailureReason);

[NexusService]
public interface IInventoryNexusService
{
    [NexusOperation]
    ReserveInventoryOutput ReserveInventory(ReserveInventoryInput input);
}
