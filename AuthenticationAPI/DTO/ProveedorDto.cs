using System.ComponentModel.DataAnnotations;

namespace AuthenticationAPI.DTO
{
    public class ProveedorDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [RegularExpression(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$", ErrorMessage = "El RFC no tiene un formato válido.")]
        public string? RFC { get; set; }

        [Required(ErrorMessage = "El contacto es obligatorio.")]
        public string Contacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [Phone(ErrorMessage = "El teléfono no es válido.")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "El teléfono debe tener entre 10 y 15 dígitos.")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string Email { get; set; } = string.Empty;

        public string? Direccion { get; set; }
        public bool Activo { get; set; } = true;
    }
}
