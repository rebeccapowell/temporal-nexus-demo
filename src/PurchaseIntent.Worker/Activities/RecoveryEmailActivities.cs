using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Temporalio.Activities;

namespace PurchaseIntent.Worker.Activities;

public class RecoveryEmailActivities(IConfiguration configuration)
{
    [Activity]
    public async Task SendRecoveryEmailAsync(string email, string purchaseIntentId)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Shopping Demo", "demo@localhost"));
        message.To.Add(new MailboxAddress("Customer", email));
        message.Subject = "Come back and finish your order!";
        message.Body = new TextPart("html")
        {
            Text = $@"<h2>You left something behind!</h2>
<p>Your basket is waiting for you. <a href=""http://localhost:5173"">Return to the shop</a></p>
<p><small>Purchase Intent ID: {purchaseIntentId}</small></p>"
        };

        var host = configuration["MailPit:Host"] ?? "localhost";
        var port = int.Parse(configuration["MailPit:Port"] ?? "1025");

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.None);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);

        ActivityExecutionContext.Current.Logger.LogInformation("Recovery email sent (address not logged)");
    }
}
