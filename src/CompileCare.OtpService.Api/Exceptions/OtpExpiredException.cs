namespace CompileCare.OtpService.Api.Exceptions
{
    public class OtpExpiredException : OtpException
    {
        public OtpExpiredException()
            : base("OTP has expired. Please request a new one.")
        {
        }

        public OtpExpiredException(string message)
            : base(message)
        {
        }
    }
}
