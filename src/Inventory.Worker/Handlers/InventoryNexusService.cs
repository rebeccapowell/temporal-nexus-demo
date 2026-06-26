using NexusRpc.Handlers;
using ShoppingBasket.NexusContracts;

namespace Inventory.Worker.Handlers;

[NexusServiceHandler(typeof(IInventoryNexusService))]
public class InventoryNexusService
{
    private static readonly Dictionary<string, int> Stock = new()
    {
        ["SKU-001"] = 10,
        ["SKU-002"] = 5,
        ["SKU-003"] = 20,
        ["SKU-004"] = 8,
    };

    [NexusOperationHandler]
    public IOperationHandler<ReserveInventoryInput, ReserveInventoryOutput> ReserveInventory() =>
        OperationHandler.Sync<ReserveInventoryInput, ReserveInventoryOutput>((_, input) =>
        {
            foreach (var item in input.Items)
            {
                if (!Stock.TryGetValue(item.Sku, out var available) || available < item.Quantity)
                {
                    return new ReserveInventoryOutput(
                        false,
                        $"Insufficient stock for {item.Sku}: requested {item.Quantity}, available {available}");
                }
            }

            foreach (var item in input.Items)
            {
                Stock[item.Sku] -= item.Quantity;
            }

            return new ReserveInventoryOutput(true, null);
        });
}
