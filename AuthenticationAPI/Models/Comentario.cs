using AuthenticationAPI.Enums;

namespace AuthenticationAPI.Models
{
    // Comentarios/quejas/dudas generales del cliente, con seguimiento por parte de un administrador
    // (no necesariamente ligados a un producto puntual).
    public class Comentario
    {
        public int Id { get; set; }
        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public string Asunto { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public EstatusComentario Estatus { get; set; } = EstatusComentario.Nuevo;
        public string? RespuestaAdmin { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaRespuesta { get; set; }
    }
}
