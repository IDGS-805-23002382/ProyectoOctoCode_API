namespace AuthenticationAPI.Models
{
    // Opinión/reseña que el cliente deja sobre un producto que adquirió.
    public class ResenaProducto
    {
        public int Id { get; set; }

        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        public int VentaId { get; set; }
        public Venta? Venta { get; set; }

        public int Calificacion { get; set; } // 1 a 5
        public string? Comentario { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
