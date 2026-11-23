using CompileCare.OtpService.Api.Models.Notifications.Email;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CompileCare.OtpService.Api.Services.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            IOptions<EmailSettings> settings,
            ILogger<SmtpEmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendOtpEmailAsync(
            string toEmail,
            string otpCode,
            CancellationToken cancellationToken = default)
        {
            // Basic validation of configuration and inputs to avoid silent success
            if (string.IsNullOrWhiteSpace(_settings.SmtpHost) || _settings.SmtpPort == 0)
            {
                _logger.LogError("SMTP configuration is invalid. Host: {Host}, Port: {Port}", _settings.SmtpHost, _settings.SmtpPort);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
            {
                _logger.LogError("SMTP sender email is not configured.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogError("Recipient email is empty.");
                return false;
            }

            try
            {
                var message = new MimeMessage();
                // Ensure Sender is set (some SMTP relays require this)
                message.Sender = new MailboxAddress(_settings.SenderName, _settings.SenderEmail);
                message.From.Clear();
                message.From.Add(message.Sender);
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = "Your OTP Code - CompileCare";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = GenerateOtpEmailTemplate(otpCode),
                    TextBody = $"Your OTP code is: {otpCode}\n\nThis code will expire in 5 minutes.\n\nIf you didn't request this code, please ignore this email."
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                // Choose appropriate SecureSocketOptions. For common ports:
                // 465 -> SslOnConnect, 587 -> StartTls, else use StartTls when EnableSsl is true, otherwise None
                SecureSocketOptions socketOptions;
                if (_settings.SmtpPort == 465)
                {
                    socketOptions = SecureSocketOptions.SslOnConnect;
                }
                else if (_settings.SmtpPort == 587)
                {
                    socketOptions = SecureSocketOptions.StartTls;
                }
                else if (_settings.EnableSsl)
                {
                    socketOptions = SecureSocketOptions.StartTls;
                }
                else
                {
                    socketOptions = SecureSocketOptions.None;
                }

                _logger.LogInformation("Connecting to SMTP server {Host}:{Port} using {Options}", _settings.SmtpHost, _settings.SmtpPort, socketOptions);

                // Connect to SMTP server
                await client.ConnectAsync(
                    _settings.SmtpHost,
                    _settings.SmtpPort,
                    socketOptions,
                    cancellationToken);

                _logger.LogInformation("SMTP connected: {IsConnected}", client.IsConnected);

                // Authenticate if credentials are provided
                if (!string.IsNullOrEmpty(_settings.Username))
                {
                    try
                    {
                        _logger.LogInformation("Authenticating to SMTP server as {User}", _settings.Username);
                        await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
                        _logger.LogInformation("SMTP authenticated: {IsAuthenticated}", client.IsAuthenticated);

                        // Some SMTP providers require the envelope sender to match the authenticated identity.
                        // If the Username looks like an email, prefer it as the envelope sender.
                        if (_settings.Username.Contains('@'))
                        {
                            var authSender = new MailboxAddress(_settings.SenderName, _settings.Username);
                            _logger.LogInformation("Switching envelope sender to authenticated user {AuthSender}", _settings.Username);
                            message.Sender = authSender;
                            message.From.Clear();
                            message.From.Add(authSender);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "SMTP authentication failed for user {User}", _settings.Username);
                        // Disconnect before returning
                        try { await client.DisconnectAsync(true, cancellationToken); } catch { }
                        return false;
                    }
                }

                // Send email
                _logger.LogInformation("Sending OTP email to {Email} with From={From}", toEmail, message.From);
                await client.SendAsync(message, cancellationToken);

                // Gracefully disconnect
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation("OTP email sent successfully to {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
                return false;
            }
        }

        private string GenerateOtpEmailTemplate(string otpCode)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .container {{
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            padding: 40px;
            text-align: center;
        }}
        .logo {{
            font-size: 24px;
            font-weight: bold;
            color: #4F46E5;
            margin-bottom: 30px;
        }}
        .otp-box {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            font-size: 32px;
            font-weight: bold;
            letter-spacing: 8px;
            padding: 20px;
            border-radius: 8px;
            margin: 30px 0;
            display: inline-block;
        }}
        .message {{
            color: #666;
            font-size: 14px;
            margin-top: 20px;
        }}
        .warning {{
            background-color: #FEF3C7;
            border-left: 4px solid #F59E0B;
            padding: 12px;
            margin-top: 20px;
            text-align: left;
            font-size: 13px;
            color: #92400E;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #E5E7EB;
            font-size: 12px;
            color: #9CA3AF;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='logo'>CompileCare</div>
        <h2 style='color: #1F2937; margin-bottom: 20px;'>Your Verification Code</h2>
        <p style='color: #6B7280; font-size: 16px;'>
            Use the following code to complete your verification:
        </p>
        <div class='otp-box'>{otpCode}</div>
        <p class='message'>
            This code will expire in <strong>5 minutes</strong>.<br>
            Please do not share this code with anyone.
        </p>
        <div class='warning'>
            <strong>⚠️ Security Notice:</strong><br>
            If you didn't request this code, please ignore this email. 
            Your account security is important to us.
        </div>
        <div class='footer'>
            <p>© 2025 CompileCare. All rights reserved.</p>
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
