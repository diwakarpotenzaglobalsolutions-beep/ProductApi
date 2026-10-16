using System.Net;
using System.Net.Mail;

namespace MyMvcApp.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration configuration;

    public EmailService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var senderEmail = configuration["EmailSettings:SenderEmail"];
        if (string.IsNullOrWhiteSpace(senderEmail))
            throw new EmailDeliveryException("Email delivery is not configured. Contact the administrator.");

        using var smtpClient = new SmtpClient();
        if (string.Equals(configuration["EmailSettings:DeliveryMethod"], "SpecifiedPickupDirectory", StringComparison.OrdinalIgnoreCase))
        {
            var pickupDirectory = configuration["EmailSettings:PickupDirectory"];
            if (string.IsNullOrWhiteSpace(pickupDirectory))
                throw new EmailDeliveryException("Email delivery is not configured. Contact the administrator.");
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            smtpClient.PickupDirectoryLocation = Path.GetFullPath(pickupDirectory);
            Directory.CreateDirectory(smtpClient.PickupDirectoryLocation);
        }
        else
        {
            var smtpHost = configuration["EmailSettings:SmtpHost"];
            var senderPassword = configuration["EmailSettings:Password"];
            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(senderPassword)
                || !int.TryParse(configuration["EmailSettings:SmtpPort"], out var smtpPort)
                || smtpPort is < 1 or > 65535)
                throw new EmailDeliveryException("Email delivery is not configured. Contact the administrator.");

            if (string.Equals(smtpHost, "smtp.gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                // Google displays App Passwords in groups of four characters.
                senderPassword = string.Concat(senderPassword.Where(c => !char.IsWhiteSpace(c)));
                if (senderPassword.Length != 16)
                    throw new EmailDeliveryException("Gmail SMTP requires a 16-character App Password. The administrator must update EmailSettings:Password.");
            }

            smtpClient.Host = smtpHost;
            smtpClient.Port = smtpPort;
            smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);
            smtpClient.EnableSsl = true;
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail!),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        mailMessage.To.Add(toEmail);

        try
        {
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
        }
        catch (SmtpException ex)
        {
            throw new EmailDeliveryException("Unable to send the password-reset email. Please try again later or contact the administrator to check SMTP credentials and connectivity.", ex);
        }
    }
}
