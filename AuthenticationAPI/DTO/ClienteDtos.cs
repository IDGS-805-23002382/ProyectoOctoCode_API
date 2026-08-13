using System.ComponentModel.DataAnnotations;

namespace AuthenticationAPI.DTO
{
    // Usado por un administrador para registrar un nuevo cliente. Se genera una password
    // temporal y se envía por correo junto con el correo/usuario.
    public class RegistrarClienteDto
    {
        [Required]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string RazonSocial { get; set; } = string.Empty;

        public string? RFC { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
    }

    public class ClienteDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string? RFC { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public bool Activo { get; set; }
    }

    // Actualización de perfil por el propio cliente.
    public class ActualizarPerfilClienteDto
    {
        [Required]
        public string NombreCompleto { get; set; } = string.Empty;
        public string? RazonSocial { get; set; }
        public string? RFC { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
    }
}
