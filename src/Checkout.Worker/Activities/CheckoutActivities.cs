using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Temporalio.Activities;
using Temporalio.Client;

namespace Checkout.Worker.Activities;

public class CheckoutActivities(IConfiguration configuration, ITemporalClient temporalClient)
{
    [Activity]
    public async Task SendVerificationEmailAsync(string email, string token, string purchaseIntentId)
    {
        var apiBase = configuration["Storefront:ApiBase"] ?? "http://localhost:5176";
        var verifyUrl = $"{apiBase}/api/email-verification/confirm?purchaseIntentId={purchaseIntentId}&token={token}";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Shopping Demo", "demo@localhost"));
        message.To.Add(new MailboxAddress("Customer", email));
        message.Subject = "Verify your email address";
        message.Body = new TextPart("html")
        {
            Text = $@"<h2>Verify your email</h2>
<p><a href=""{verifyUrl}"">Click here to verify your email address</a></p>
<p>Or paste this link: {verifyUrl}</p>"
        };

        var host = configuration["MailPit:Host"] ?? "localhost";
        var port = int.Parse(configuration["MailPit:Port"] ?? "1025");

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.None);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        ActivityExecutionContext.Current.Logger.LogInformation("Verification email sent (address not logged)");
    }

    [Activity]
    public async Task NotifyEmailVerifiedAsync(string purchaseIntentId)
    {
        var piHandle = temporalClient.GetWorkflowHandle($"purchase-intent-{purchaseIntentId}");
        await piHandle.SignalAsync("EmailVerified", Array.Empty<object>());
        ActivityExecutionContext.Current.Logger.LogInformation(
            "Email verified signal sent to purchase intent {Id}", purchaseIntentId);
    }

    [Activity]
    public async Task ForwardEmailVerifiedToCheckoutAsync(string checkoutId)
    {
        var handle = temporalClient.GetWorkflowHandle(checkoutId);
        await handle.SignalAsync("EmailVerified", Array.Empty<object>());
        ActivityExecutionContext.Current.Logger.LogInformation(
            "Email verified signal forwarded to checkout {Id}", checkoutId);
    }

    [Activity]
    public async Task NotifyPurchaseIntentCheckoutCompletedAsync(string purchaseIntentId, string checkoutId)
    {
        var handle = temporalClient.GetWorkflowHandle($"purchase-intent-{purchaseIntentId}");
        await handle.SignalAsync("CheckoutCompleted", new[] { (object)checkoutId });
        ActivityExecutionContext.Current.Logger.LogInformation(
            "Checkout completed signal sent to purchase intent {Id}", purchaseIntentId);
    }

    [Activity]
    public async Task NotifyPurchaseIntentCheckoutFailedAsync(string purchaseIntentId, string checkoutId, string reason)
    {
        var handle = temporalClient.GetWorkflowHandle($"purchase-intent-{purchaseIntentId}");
        await handle.SignalAsync("CheckoutFailed", new object[] { checkoutId, reason });
        ActivityExecutionContext.Current.Logger.LogInformation(
            "Checkout failed signal sent to purchase intent {Id}", purchaseIntentId);
    }

    [Activity]
    public async Task SendConfirmationEmailAsync(string email, string checkoutId, string trackingNumber)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Shopping Demo", "demo@localhost"));
        message.To.Add(new MailboxAddress("Customer", email));
        message.Subject = "Order confirmed!";
        message.Body = new TextPart("html")
        {
            Text = $@"<h2>Your order is confirmed!</h2>
<p>Checkout ID: {checkoutId}</p>
<p>Tracking Number: {trackingNumber}</p>
<p>Thanks for shopping with us!</p>"
        };

        var host = configuration["MailPit:Host"] ?? "localhost";
        var port = int.Parse(configuration["MailPit:Port"] ?? "1025");

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.None);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        ActivityExecutionContext.Current.Logger.LogInformation("Confirmation email sent for checkout {Id}", checkoutId);
    }
}
