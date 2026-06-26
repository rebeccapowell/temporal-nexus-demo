using NexusRpc.Handlers;
using ShoppingBasket.NexusContracts;

namespace Fulfillment.Worker.Handlers;

[NexusServiceHandler(typeof(IFulfillmentNexusService))]
public class FulfillmentNexusService
{
    [NexusOperationHandler]
    public IOperationHandler<RequestFulfillmentInput, RequestFulfillmentOutput> RequestFulfillment() =>
        OperationHandler.Sync<RequestFulfillmentInput, RequestFulfillmentOutput>((_, input) =>
        {
            var trackingNumber = $"TRK-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            return new RequestFulfillmentOutput(true, trackingNumber, null);
        });
}
