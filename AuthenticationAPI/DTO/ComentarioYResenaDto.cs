using System.ComponentModel.DataAnnotations;

namespace AuthenticationAPI.DTO
{
    public class CrearComentarioDto
    {
        [Required]
        public string Asunto { get; set; } = string.Empty;
        [Required]
        public string Mensaje { get; set; } = string.Empty;
    }

    public class ResponderComentarioDto
    {
        [Required]
        public string Respuesta { get; set; } = string.Empty;
        [Required]
        public string Estatus { get; set; } = "Resuelto"; // Nuevo, EnRevision, Resuelto, Cerrado
    }

    public class ComentarioDto
    {
        public int Id { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Estatus { get; set; } = string.Empty;
        public string? RespuestaAdmin { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class CrearResenaDto
    {
        [Required]
        public int VentaId { get; set; }

        [Range(1, 5)]
        public int Calificacion { get; set; }

        public string? Comentario { get; set; }
    }

    public class ResenaDto
    {
        public int Id { get; set; }
        public string Producto { get; set; } = string.Empty;
        public int Calificacion { get; set; }
        public string? Comentario { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class CotizacionRequestDto
    {
        [Required]
        public string DescripcionProyecto { get; set; } = string.Empty;
    }
}
