using Temporalio.Activities;

namespace PurchaseIntent.Worker.Activities;

public class PurchaseIntentActivities
{
    [Activity]
    public Task<bool> CheckEmailVerificationStatusAsync(string purchaseIntentId) =>
        Task.FromResult(false);
}
