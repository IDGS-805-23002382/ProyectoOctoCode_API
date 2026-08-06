namespace AuthenticationAPI.interfaces
{
    public class AcpanCotizacionResult
    {
        public decimal MontoEstimado { get; set; }
        public string? Detalle { get; set; }
    }

    public class AcpanInventarioItem
    {
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Existencia { get; set; }
    }

    // Cliente HTTP hacia la API de Acpan (proyecto de IoT/tratamiento de agua),
    // usado para obtener cotizaciones de componentes y su inventario disponible.
    public interface IAcpanIntegrationService
    {
        Task<AcpanCotizacionResult> ObtenerCotizacionAsync(string descripcionProyecto);
        Task<List<AcpanInventarioItem>> ObtenerInventarioAsync();
    }
}
