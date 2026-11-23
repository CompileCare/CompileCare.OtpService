namespace CompileCare.OtpService.Api.Exceptions
{
    public class OtpCooldownException : OtpException
    {
        public int CooldownSeconds { get; }

        public OtpCooldownException(int cooldownSeconds)
            : base($"OTP request too frequent. Please wait {cooldownSeconds} seconds.")
        {
            CooldownSeconds = cooldownSeconds;
        }

        public OtpCooldownException(string message)
            : base(message)
        {
        }
    }
}