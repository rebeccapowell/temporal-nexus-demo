using ShoppingBasket.Contracts;
using NexusRpc;

namespace ShoppingBasket.NexusContracts;

public record AuthorizePaymentInput(string CheckoutId, decimal Amount, PaymentMethod Payment);

public record AuthorizePaymentOutput(bool Success, string? AuthorizationCode, string? FailureReason);

[NexusService]
public interface IPaymentNexusService
{
    [NexusOperation]
    AuthorizePaymentOutput AuthorizePayment(AuthorizePaymentInput input);
}
