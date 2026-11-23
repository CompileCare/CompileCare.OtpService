namespace CompileCare.OtpService.Api.Services.Email
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends an OTP code to the specified email address
        /// </summary>
        /// <param name="toEmail">Recipient email address</param>
        /// <param name="otpCode">The OTP code to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if email sent successfully, false otherwise</returns>
        Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default);
    }
}
