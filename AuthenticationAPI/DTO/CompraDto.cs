using System.ComponentModel.DataAnnotations;

namespace AuthenticationAPI.DTO
{
    public class CrearCompraDto
    {
        [Required]
        public int ProveedorId { get; set; }
        public string? Folio { get; set; }

        [Required, MinLength(1)]
        public List<CrearCompraDetalleDto> Detalles { get; set; } = new();
    }

    public class CrearCompraDetalleDto
    {
        // Opcional: si ya conoces el Id exacto de la fila (nombre+proveedor) puedes mandarlo.
        public int? MateriaPrimaId { get; set; }

        // Recomendado: nombre libre del insumo. El backend busca (o crea) la fila
        // exacta para este nombre + el ProveedorId de la compra, sin mezclar con
        // lo que ya exista para otros proveedores. Aquí es donde en la práctica se
        // define la Unidad, el Proveedor y (junto con Cantidad/CostoUnitario) el
        // Stock y Costo Unitario del insumo — por eso ya no se piden en el alta manual.
        public string? Nombre { get; set; }

        public string? UnidadMedida { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal Cantidad { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal CostoUnitario { get; set; }
    }

    public class CompraDto
    {
        public int Id { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public string? Folio { get; set; }
        public DateTime FechaCompra { get; set; }
        public string Estatus { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public List<CompraDetalleDto> Detalles { get; set; } = new();
    }

    public class CompraDetalleDto
    {
        public string MateriaPrima { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}
