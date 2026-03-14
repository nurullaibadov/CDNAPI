using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CdnApi.Application.Interfaces.Services;
using CdnApi.Application.Interfaces.Repositories;
using CdnApi.Domain.Entities;

namespace CdnApi.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IUnitOfWork unitOfWork)
    {
        _configuration = configuration;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var emailLog = new EmailLog
        {
            ToEmail = toEmail,
            Subject = subject,
            Body = body,
            EmailType = "General"
        };

        try
        {
            using var client = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"] ?? "587"))
            {
                Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true"),
                Timeout = 30000
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["FromEmail"]!, smtpSettings["FromName"] ?? "CDN API"),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);

            emailLog.IsSent = true;
            emailLog.SentAt = DateTime.UtcNow;
            _logger.LogInformation("Email sent to {Email} - Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            emailLog.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
        finally
        {
            await _unitOfWork.EmailLogs.AddAsync(emailLog);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task SendVerificationEmailAsync(string toEmail, string userName, string token)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"];
        var verifyUrl = $"{baseUrl}/api/auth/verify-email?token={token}";

        var body = GetVerificationEmailTemplate(userName, verifyUrl);
        await SendEmailAsync(toEmail, "Verify Your Email Address - CDN API", body);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string token)
    {
        var baseUrl = _configuration["AppSettings:FrontendUrl"] ?? _configuration["AppSettings:BaseUrl"];
        var resetUrl = $"{baseUrl}/reset-password?token={token}&email={toEmail}";

        var body = GetPasswordResetEmailTemplate(userName, resetUrl);
        await SendEmailAsync(toEmail, "Reset Your Password - CDN API", body);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var body = GetWelcomeEmailTemplate(userName);
        await SendEmailAsync(toEmail, "Welcome to CDN API!", body);
    }

    public async Task SendPasswordChangedNotificationAsync(string toEmail, string userName)
    {
        var body = GetPasswordChangedTemplate(userName);
        await SendEmailAsync(toEmail, "Your Password Has Been Changed - CDN API", body);
    }

    public async Task SendBanNotificationAsync(string toEmail, string userName, string reason)
    {
        var body = GetBanNotificationTemplate(userName, reason);
        await SendEmailAsync(toEmail, "Account Suspended - CDN API", body);
    }

    private string GetBaseTemplate(string content) => $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; }}
        .header h1 {{ color: #ffffff; margin: 0; font-size: 24px; }}
        .content {{ padding: 40px; color: #333333; line-height: 1.6; }}
        .button {{ display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff !important; padding: 14px 32px; border-radius: 6px; text-decoration: none; font-weight: 600; margin: 20px 0; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; color: #6c757d; font-size: 13px; border-top: 1px solid #eee; }}
        .warning {{ background: #fff3cd; border: 1px solid #ffc107; border-radius: 6px; padding: 15px; margin: 15px 0; }}
        .info-box {{ background: #e8f4fd; border-left: 4px solid #667eea; padding: 15px; margin: 15px 0; border-radius: 0 6px 6px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header""><h1>⚡ CDN API</h1></div>
        <div class=""content"">{content}</div>
        <div class=""footer"">
            <p>© {DateTime.UtcNow.Year} CDN API. All rights reserved.</p>
            <p>This is an automated message. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";

    private string GetVerificationEmailTemplate(string userName, string verifyUrl) =>
        GetBaseTemplate($@"
            <h2>Hello, {userName}! 👋</h2>
            <p>Welcome to CDN API! To complete your registration and start using our service, please verify your email address.</p>
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{verifyUrl}"" class=""button"">✅ Verify Email Address</a>
            </div>
            <div class=""warning"">
                <strong>⏰ This link expires in 24 hours.</strong>
            </div>
            <p>If you did not create an account, please ignore this email.</p>");

    private string GetPasswordResetEmailTemplate(string userName, string resetUrl) =>
        GetBaseTemplate($@"
            <h2>Password Reset Request</h2>
            <p>Hi <strong>{userName}</strong>,</p>
            <p>We received a request to reset your CDN API password. Click the button below to set a new password:</p>
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{resetUrl}"" class=""button"">🔑 Reset My Password</a>
            </div>
            <div class=""warning"">
                <strong>⏰ This link expires in 1 hour.</strong><br>
                If you didn't request a password reset, please ignore this email. Your account is still secure.
            </div>");

    private string GetWelcomeEmailTemplate(string userName) =>
        GetBaseTemplate($@"
            <h2>Welcome to CDN API! 🎉</h2>
            <p>Hi <strong>{userName}</strong>,</p>
            <p>Your account has been successfully created and verified. You're now ready to start using CDN API!</p>
            <div class=""info-box"">
                <strong>What you can do:</strong><br>
                ✅ Upload and manage files<br>
                ✅ Generate CDN URLs for fast delivery<br>
                ✅ Create API keys for programmatic access<br>
                ✅ Monitor usage via dashboard
            </div>");

    private string GetPasswordChangedTemplate(string userName) =>
        GetBaseTemplate($@"
            <h2>Password Changed Successfully</h2>
            <p>Hi <strong>{userName}</strong>,</p>
            <p>Your CDN API account password was successfully changed.</p>
            <div class=""warning"">
                If you did NOT make this change, please contact our support immediately and reset your password.
            </div>");

    private string GetBanNotificationTemplate(string userName, string reason) =>
        GetBaseTemplate($@"
            <h2>Account Suspended</h2>
            <p>Hi <strong>{userName}</strong>,</p>
            <p>Your CDN API account has been suspended by an administrator.</p>
            <div class=""warning"">
                <strong>Reason:</strong> {reason}
            </div>
            <p>If you believe this is a mistake, please contact our support team.</p>");
}
