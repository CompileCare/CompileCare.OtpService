using CompileCare.OtpService.Api.Models;
using CompileCare.OtpService.Api.Services.Email;
using CompileCare.OtpService.Api.Services.SMS;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace CompileCare.OtpService.Api.Services
{
    public class OtpServiceCore
    {
        private readonly IDistributedCache _cache;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private const int OtpExpiryMinutes = 5;
        private const int RequestCooldownSeconds = 30; // rete limiting

        public OtpServiceCore(IDistributedCache cache, IEmailService emailService, ISmsService smsService)
        {
            _cache = cache;
            _emailService = emailService;
            _smsService = smsService;
        }

        public async Task<ApiResponse> GenerateOtpAsync(string userId)
        {
            var cacheKey = $"otp:{userId}";
            var requestKey = $"otp-req:{userId}";

            // Check cooldown
            if (await _cache.GetStringAsync(requestKey) != null)
            {
                return new ApiResponse(false, "OTP request too frequent. Try again later.");
            }

            var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OtpExpiryMinutes)
            };

            await _cache.SetStringAsync(cacheKey, otp, options);

            // Add request cooldown
            await _cache.SetStringAsync(requestKey, "cooldown", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(RequestCooldownSeconds)
            });

            // In real world → send OTP via SMS/Email gateway
            var deliveryMethod = DetectDeliveryMethod(userId);
            var sendSuccess = await SendOtpAsync(userId, otp, deliveryMethod);

            if (!sendSuccess)
            {
                await _cache.RemoveAsync(cacheKey);
                return new ApiResponse(false, "Failed to send OTP. Please try again.");
            }

            return new ApiResponse(true, "OTP sent successfully.");
        }

        public async Task<ApiResponse> VerifyOtpAsync(string userId, string otpCode)
        {
            var cacheKey = $"otp:{userId}";
            var storedOtp = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(storedOtp))
                return new ApiResponse(false, "OTP expired or not found.");

            if (storedOtp != otpCode)
                return new ApiResponse(false, "Invalid OTP.");

            await _cache.RemoveAsync(cacheKey);

            return new ApiResponse(true, "OTP verified successfully.");
        }

        public async Task<ApiResponse> ResendOtpAsync(string userId)
        {
            return await GenerateOtpAsync(userId); // same as generate with cooldown
        }

        private Channel DetectDeliveryMethod(string userId)
        {
            if (IsEmail(userId))
            {
                return Channel.Email;
            }
            else if (IsPhoneNumber(userId))
            {
                return Channel.SMS;
            }
            else
            {
                return Channel.Unkown;
            }
        }

        /// <summary>
        /// Checks if the string is a valid email
        /// </summary>
        private bool IsEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            try
            {
                // Simple email regex pattern
                var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(input, emailPattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the string is a valid phone number
        /// </summary>
        private bool IsPhoneNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Remove common phone number characters
            var cleaned = Regex.Replace(input, @"[\s\-\(\)\+]", "");

            // Check if it's all digits and has reasonable length (7-15 digits)
            return Regex.IsMatch(cleaned, @"^\d{7,15}$");
        }

        /// <summary>
        /// Sends OTP via the appropriate channel (Email or SMS)
        /// </summary>
        private async Task<bool> SendOtpAsync(string userId, string otp, Channel deliveryMethod)
        {
            try
            {
                switch (deliveryMethod)
                {
                    case Channel.Email:
                        // _logger.LogInformation("Sending OTP via Email to {Email}", userId);
                        return await _emailService.SendOtpEmailAsync(userId, otp);

                    case Channel.SMS:
                        // _logger.LogInformation("Sending OTP via SMS to {Phone}", userId);

                        // Normalize phone number (add + if missing)
                        var normalizedPhone = NormalizePhoneNumber(userId);
                        return await _smsService.SendOtpSmsAsync(normalizedPhone, otp);

                    default:
                        // _logger.LogWarning("Could not detect delivery method for {UserId}", userId);

                        // Never log OTPs or identifiers when the delivery channel is unknown.
                        return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Normalizes phone number to E.164 format
        /// </summary>
        private string NormalizePhoneNumber(string phoneNumber)
        {
            // Remove all non-digit characters except +
            var cleaned = Regex.Replace(phoneNumber, @"[^\d\+]", "");

            // If it doesn't start with +, add it
            if (!cleaned.StartsWith("+"))
            {
                // If it's an Indian number (10 digits), add +91
                if (cleaned.Length == 10 &&
                    (cleaned.StartsWith("6") || cleaned.StartsWith("7") ||
                     cleaned.StartsWith("8") || cleaned.StartsWith("9")))
                {
                    cleaned = "+91" + cleaned;
                }
                // If it starts with country code without +, add +
                else if (cleaned.Length > 10)
                {
                    cleaned = "+" + cleaned;
                }
            }
            return cleaned;
        }
    }
}
