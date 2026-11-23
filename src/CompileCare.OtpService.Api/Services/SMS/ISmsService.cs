namespace CompileCare.OtpService.Api.Services.SMS
{
    public interface ISmsService
    {
        /// <summary>
        /// Sends an OTP code to the specified phone number
        /// </summary>
        /// <param name="phoneNumber">Recipient phone number (E.164 format recommended: +919876543210)</param>
        /// <param name="otpCode">The OTP code to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if SMS sent successfully, false otherwise</returns>
        Task<bool> SendOtpSmsAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default);
    }
}
