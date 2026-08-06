using AuthenticationAPI.Enums;

namespace AuthenticationAPI.Models
{
    // Solicitud de cotización de un proyecto (ej. un sistema Acpan) para un cliente,
    // apoyándose en IAcpanIntegrationService para calcular componentes/costos del lado de Acpan.
    public class SolicitudCotizacion
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public string DescripcionProyecto { get; set; } = string.Empty;
        public decimal MontoEstimado { get; set; }
        public EstatusCotizacion Estatus { get; set; } = EstatusCotizacion.Pendiente;
        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
        public DateTime? FechaRespuesta { get; set; }
    }
}
