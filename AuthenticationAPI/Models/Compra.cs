using AuthenticationAPI.Enums;

namespace AuthenticationAPI.Models
{
    // Encabezado de una compra a proveedor.
    public class Compra
    {
        public int Id { get; set; }
        public int ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }

        public DateTime FechaCompra { get; set; } = DateTime.UtcNow;
        public string? Folio { get; set; }
        public EstatusCompra Estatus { get; set; } = EstatusCompra.Pendiente;
        public decimal Total { get; set; } = 0;

        public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
    }

    // Detalle de compra: cada renglón genera una CapaCosto al recibirse.
    public class CompraDetalle
    {
        public int Id { get; set; }
        public int CompraId { get; set; }
        public Compra? Compra { get; set; }

        public int MateriaPrimaId { get; set; }
        public MateriaPrima? MateriaPrima { get; set; }

        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal Subtotal => Cantidad * CostoUnitario;
    }
}
