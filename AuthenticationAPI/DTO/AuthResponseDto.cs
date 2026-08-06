namespace AuthenticationAPI.DTO
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public DateTime Expiration {  get; set; }
        public string? RefreshToken { get; set; }
        public bool Requiere2FA { get; set; }
        public string? Email { get; set; }
    }
}
