using CompileCare.OtpService.Api.Services;
using CompileCare.OtpService.Api.Services.Email;
using CompileCare.OtpService.Api.Services.SMS;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Text;

namespace CompileCare.OtpService.Tests.Services
{
    public class OtpServiceCoreTests
    {
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly OtpServiceCore _otpService;
        private readonly Mock<IEmailService> _emailMock;
        private readonly Mock<ISmsService> _smsMock;

        public OtpServiceCoreTests()
        {
            _cacheMock = new Mock<IDistributedCache>();
            _emailMock = new Mock<IEmailService>();
            _smsMock = new Mock<ISmsService>();
            _emailMock.Setup(x => x.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _smsMock.Setup(x => x.SendOtpSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _otpService = new OtpServiceCore(_cacheMock.Object, _emailMock.Object, _smsMock.Object);
        }

        [Fact]
        public async Task GenerateOtpAsync_ShouldReturnSuccess_WhenNoCooldown()
        {
            string userId = "user@example.com";
            _cacheMock.Setup(c => c.GetAsync($"otp-req:{userId}", It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[])null);

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()));

            var result = await _otpService.GenerateOtpAsync(userId);

            Assert.True(result.Success);
            Assert.Equal("OTP sent successfully.", result.Message);

            _cacheMock.Verify(c => c.SetAsync(
                It.Is<string>(k => k == $"otp:{userId}"),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GenerateOtpAsync_ShouldReturnFailure_WhenCooldownExists()
        {
            string userId = "user123";
            _cacheMock.Setup(c => c.GetAsync($"otp-req:{userId}", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(Encoding.UTF8.GetBytes("cooldown"));

            var result = await _otpService.GenerateOtpAsync(userId);

            Assert.False(result.Success);
            Assert.Equal("OTP request too frequent. Try again later.", result.Message);
        }

        [Fact]
        public async Task VerifyOtpAsync_ShouldReturnSuccess_WhenOtpMatches()
        {
            string userId = "user123";
            string otp = "123456";
            _cacheMock.Setup(c => c.GetAsync($"otp:{userId}", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(Encoding.UTF8.GetBytes(otp));

            _cacheMock.Setup(c => c.RemoveAsync($"otp:{userId}", It.IsAny<CancellationToken>()));

            var result = await _otpService.VerifyOtpAsync(userId, otp);

            Assert.True(result.Success);
            Assert.Equal("OTP verified successfully.", result.Message);

            _cacheMock.Verify(c => c.RemoveAsync($"otp:{userId}", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task VerifyOtpAsync_ShouldReturnFailure_WhenOtpDoesNotMatch()
        {
            string userId = "user123";
            _cacheMock.Setup(c => c.GetAsync($"otp:{userId}", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(Encoding.UTF8.GetBytes("654321"));

            var result = await _otpService.VerifyOtpAsync(userId, "123456");

            Assert.False(result.Success);
            Assert.Equal("Invalid OTP.", result.Message);
        }

        [Fact]
        public async Task VerifyOtpAsync_ShouldReturnFailure_WhenOtpIsMissing()
        {
            string userId = "user123";
            _cacheMock.Setup(c => c.GetAsync($"otp:{userId}", It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[])null);

            var result = await _otpService.VerifyOtpAsync(userId, "123456");

            Assert.False(result.Success);
            Assert.Equal("OTP expired or not found.", result.Message);
        }

        [Fact]
        public async Task ResendOtpAsync_ShouldCallGenerateOtpAsync()
        {
            string userId = "user123";
            _cacheMock.Setup(c => c.GetAsync($"otp-req:{userId}", It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[])null);

            _cacheMock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()));

            var result = await _otpService.ResendOtpAsync(userId);

            Assert.True(result.Success);
            Assert.Equal("OTP sent successfully.", result.Message);
        }
    }
}
