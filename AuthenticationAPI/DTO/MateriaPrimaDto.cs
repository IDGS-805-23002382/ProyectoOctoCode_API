using System.ComponentModel.DataAnnotations;
using AuthenticationAPI.Enums;

namespace AuthenticationAPI.DTO
{
    // Vista completa de la materia prima (lectura). Incluye stock, costo y proveedor,
    // que son datos calculados/asignados automáticamente por las Compras, no capturados a mano.
    public class MateriaPrimaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string UnidadMedida { get; set; } = "PZA";
        public MetodoCosteo MetodoCosteo { get; set; } = MetodoCosteo.PromedioPonderado;
        public decimal Stock { get; set; }
        public decimal CostoUnitarioActual { get; set; }
        public decimal StockMinimo { get; set; }
        public bool Activo { get; set; } = true;
        public int? ProveedorId { get; set; }
        public string? Proveedor { get; set; }
    }

    // Alta manual de un insumo NUEVO. Unidad, Stock y Costo Unitario NO se piden aquí:
    // se generan solos cuando se registra la primera Compra de este insumo (ver
    // ComprasController). El Proveedor sí se pide aquí porque el formulario del panel
    // lo exige como obligatorio al crear.
    public class CrearMateriaPrimaDto
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public MetodoCosteo MetodoCosteo { get; set; } = MetodoCosteo.PromedioPonderado;

        [Range(0, double.MaxValue)]
        public decimal StockMinimo { get; set; }

        [Required]
        public int ProveedorId { get; set; }
    }

    // Edición de los datos de catálogo de un insumo ya existente. Tampoco permite tocar
    // Stock ni CostoUnitarioActual directamente: esos solo se mueven vía Compras/Mermas
    // para no romper la trazabilidad del kardex (CapaCosto). Unidad y Proveedor sí se
    // pueden corregir aquí porque son datos de catálogo, no de movimientos de inventario.
    public class EditarMateriaPrimaDto
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string UnidadMedida { get; set; } = "PZA";
        public MetodoCosteo MetodoCosteo { get; set; } = MetodoCosteo.PromedioPonderado;

        [Range(0, double.MaxValue)]
        public decimal StockMinimo { get; set; }
        public int? ProveedorId { get; set; }
        public bool Activo { get; set; } = true;
    }
}