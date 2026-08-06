using AuthenticationAPI.Enums;

namespace AuthenticationAPI.Models
{
    public class MateriaPrima
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        // Se define automáticamente (por defecto "PZA") y se puede corregir después,
        // no se captura en el alta manual porque en la práctica llega vía Compra.
        public string UnidadMedida { get; set; } = "PZA";

        public MetodoCosteo MetodoCosteo { get; set; } = MetodoCosteo.PromedioPonderado;

        // Stock y CostoUnitarioActual se calculan SIEMPRE a partir de las Compras
        // (vía ICosteoService). Nunca se capturan a mano en el formulario de alta.
        public decimal Stock { get; set; } = 0;
        public decimal CostoUnitarioActual { get; set; } = 0;
        public decimal StockMinimo { get; set; } = 0;
        public bool Activo { get; set; } = true;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Relación opcional con el proveedor: se asigna automáticamente cuando se
        // registra la primera Compra del insumo, no en el alta manual.
        public int? ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }

        public ICollection<CapaCosto> CapasCosto { get; set; } = new List<CapaCosto>();
        public ICollection<RecetaBOM> UsadaEnRecetas { get; set; } = new List<RecetaBOM>();
    }

    public class CapaCosto
    {
        public int Id { get; set; }
        public int MateriaPrimaId { get; set; }
        public MateriaPrima? MateriaPrima { get; set; }

        public int? CompraDetalleId { get; set; }
        public CompraDetalle? CompraDetalle { get; set; }

        public decimal CantidadOriginal { get; set; }
        public decimal CantidadDisponible { get; set; }
        public decimal CostoUnitario { get; set; }
        public DateTime FechaEntrada { get; set; } = DateTime.UtcNow;
    }
}
