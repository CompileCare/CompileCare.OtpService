namespace CompileCare.OtpService.Api.Exceptions
{
    public class OtpException : Exception
    {
        public OtpException(string message) : base(message)
        {
        }

        public OtpException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}