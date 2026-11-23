using CompileCare.OtpService.Api.Services;

namespace CompileCare.OtpService.Api.Endpoints
{
    public sealed class CompileCareOtp : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/otp/generate", async (string userId, OtpServiceCore service) =>
            await service.GenerateOtpAsync(userId));

            app.MapPost("/otp/verify", async (string userId, string otp, OtpServiceCore service) =>
            await service.VerifyOtpAsync(userId, otp));

            app.MapPost("/otp/resend", async (string userId, OtpServiceCore service) =>
                await service.ResendOtpAsync(userId));
        }
    }
}
