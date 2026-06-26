using NexusRpc.Handlers;
using ShoppingBasket.NexusContracts;

namespace Payment.Worker.Handlers;

[NexusServiceHandler(typeof(IPaymentNexusService))]
public class PaymentNexusService
{
    private static int _callCount;

    [NexusOperationHandler]
    public IOperationHandler<AuthorizePaymentInput, AuthorizePaymentOutput> AuthorizePayment() =>
        OperationHandler.Sync<AuthorizePaymentInput, AuthorizePaymentOutput>((_, input) =>
        {
            _callCount++;

            if (_callCount % 5 == 0)
            {
                return new AuthorizePaymentOutput(
                    false,
                    null,
                    "Payment declined by simulated processor.");
            }

            var authCode = $"AUTH-{Guid.NewGuid():N8}";
            return new AuthorizePaymentOutput(true, authCode, null);
        });
}
