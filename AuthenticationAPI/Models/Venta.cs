namespace AuthenticationAPI.Models
{
    // Una compra REALIZADA POR UN CLIENTE (venta de la empresa hacia el cliente).
    // No confundir con Compra (que es la empresa comprando materia prima a un Proveedor).
    public class Venta
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public decimal PrecioPagado { get; set; }
        public int Cantidad { get; set; } = 1;

        public string Estatus { get; set; } = "Completada";
    }
}
