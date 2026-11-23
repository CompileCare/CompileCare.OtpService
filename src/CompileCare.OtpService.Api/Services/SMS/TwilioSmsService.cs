using CompileCare.OtpService.Api.Models.Notifications.SMS;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
namespace CompileCare.OtpService.Api.Services.SMS
{
    public class TwilioSmsService : ISmsService
    {
        private readonly SmsSettings _settings;
        private readonly ILogger<TwilioSmsService> _logger;

        public TwilioSmsService(
            IOptions<SmsSettings> settings,
            ILogger<TwilioSmsService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            // Initialize Twilio client
            TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
        }

        public async Task<bool> SendOtpSmsAsync(
            string phoneNumber,
            string otpCode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var messageBody = $"Your CompileCare verification code is: {otpCode}\n\nThis code expires in 5 minutes.\n\nDo not share this code with anyone.";

                var message = await MessageResource.CreateAsync(
                    body: messageBody,
                    from: new PhoneNumber(_settings.FromPhoneNumber),
                    to: new PhoneNumber(phoneNumber)
                );

                if (message.Status == MessageResource.StatusEnum.Failed ||
                    message.Status == MessageResource.StatusEnum.Undelivered)
                {
                    _logger.LogWarning(
                        "SMS failed to send to {PhoneNumber}. Status: {Status}, ErrorCode: {ErrorCode}",
                        phoneNumber, message.Status, message.ErrorCode);
                    return false;
                }

                _logger.LogInformation(
                    "OTP SMS sent successfully to {PhoneNumber}. MessageSid: {MessageSid}",
                    phoneNumber, message.Sid);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }
    }
}
