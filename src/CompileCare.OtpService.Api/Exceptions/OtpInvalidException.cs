namespace CompileCare.OtpService.Api.Exceptions
{
    public class OtpInvalidException : OtpException
    {
        public int RemainingAttempts { get; }

        public OtpInvalidException()
            : base("Invalid OTP code.")
        {
        }

        public OtpInvalidException(int remainingAttempts)
            : base($"Invalid OTP code. {remainingAttempts} attempts remaining.")
        {
            RemainingAttempts = remainingAttempts;
        }

        public OtpInvalidException(string message)
            : base(message)
        {
        }
    }
}
