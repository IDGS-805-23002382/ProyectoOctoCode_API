namespace AuthenticationAPI.DTO
{
    public class RegistrarVentaDto
    {
        public int ClienteId { get; set; }
        // Cambiamos o aseguramos que los elementos del carrito apunten correctamente a los campos de tu Venta
        public List<ItemVentaDto> Detalles { get; set; } = new();
    }

    public class ItemVentaDto
    {
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

    public class VentaDto
    {
        public int Id { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string Estatus { get; set; } = string.Empty;
    }

    public class ActualizarEstatusDto
    {
        public string Estatus { get; set; } = string.Empty;
    }
}