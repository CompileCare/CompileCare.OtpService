namespace CompileCare.OtpService.Api.Models
{
    public record OtpVerifyRequest(string UserId, string OtpCode);
}
