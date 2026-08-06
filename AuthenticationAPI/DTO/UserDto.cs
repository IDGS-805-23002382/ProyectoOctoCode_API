namespace AuthenticationAPI.DTO
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public bool Estatus { get; set; } = true;
    }
}
