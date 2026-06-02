using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using FlowSharp.Infrastructure.Identity;

namespace FlowSharp.Web.Components.Account;

/// <summary>
/// SMTP tabanli e-posta gonderici. Ayarlar <c>Email</c> bolumunden okunur:
/// <c>Email:Host</c>, <c>Email:Port</c>, <c>Email:User</c>, <c>Email:Password</c>,
/// <c>Email:From</c>, <c>Email:FromName</c>, <c>Email:EnableSsl</c>.
/// <c>Email:Host</c> bos ise (ornegin lokal gelistirme) e-posta gonderilmez; bunun yerine
/// icerik (onay/sifirlama linki) loglanir, boylece kayit akisi kesintiye ugramaz.
/// </summary>
internal sealed class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, "E-postanizi dogrulayin",
            $"Hesabinizi dogrulamak icin <a href='{confirmationLink}'>buraya tiklayin</a>.");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "Sifrenizi sifirlayin",
            $"Sifrenizi sifirlamak icin <a href='{resetLink}'>buraya tiklayin</a>.");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, "Sifrenizi sifirlayin",
            $"Sifre sifirlama kodunuz: {resetCode}");

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        var host = configuration["Email:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            // SMTP yapilandirilmamis (genelde lokal gelistirme): icerigi logla, gondermeyi atla.
            logger.LogWarning(
                "SMTP yapilandirilmamis (Email:Host bos). E-posta gonderilmedi. Alici: {To}, Konu: {Subject}, Icerik: {Body}",
                to, subject, htmlBody);
            return;
        }

        var port = configuration.GetValue("Email:Port", 587);
        var enableSsl = configuration.GetValue("Email:EnableSsl", true);
        var user = configuration["Email:User"];
        var password = configuration["Email:Password"];
        var from = configuration["Email:From"] ?? user ?? "no-reply@flowsharp.local";
        var fromName = configuration["Email:FromName"] ?? "FlowSharp";

        using var message = new MailMessage
        {
            From = new MailAddress(from, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
        if (!string.IsNullOrWhiteSpace(user))
        {
            client.Credentials = new NetworkCredential(user, password);
        }

        await client.SendMailAsync(message);
        logger.LogInformation("E-posta gonderildi. Alici: {To}, Konu: {Subject}", to, subject);
    }
}
