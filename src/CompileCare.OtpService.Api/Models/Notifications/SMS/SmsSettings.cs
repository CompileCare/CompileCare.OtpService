namespace CompileCare.OtpService.Api.Models.Notifications.SMS
{
    public class SmsSettings
    {
        public string Provider { get; set; } = "Twilio"; // Can be: Twilio, AWS SNS, etc.
        public string AccountSid { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string FromPhoneNumber { get; set; } = string.Empty;
    }
}
