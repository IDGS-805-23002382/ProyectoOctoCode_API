using System.ComponentModel.DataAnnotations;

namespace AuthenticationAPI.DTO
{
    public class CrearProductoDto
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PrecioVenta { get; set; }
    }

    public class ProductoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioVenta { get; set; }
        public bool Activo { get; set; }
        public decimal CostoUnitarioEstimado { get; set; } // suma de la receta a costo actual de c/materia prima
        public List<RecetaBOMDto> Receta { get; set; } = new();
        public List<DocumentoProductoDto> Documentos { get; set; } = new();
    }

    // Explosión de materiales: uno o varios renglones de materia prima por producto.
    public class RecetaBOMDto
    {
        public int MateriaPrimaId { get; set; }
        public string? MateriaPrima { get; set; }
        public decimal CantidadRequerida { get; set; }
        public string? UnidadMedida { get; set; }
    }

    public class AgregarRecetaDto
    {
        [Required]
        public int MateriaPrimaId { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal CantidadRequerida { get; set; }
    }

    public class DocumentoProductoDto
    {
        public int Id { get; set; }

        [Required]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        public string RutaArchivo { get; set; } = string.Empty;

        public string Tipo { get; set; } = "Manual";
    }

    // Variante de solo lectura usada en el listado de "mis documentos" del cliente,
    // donde sí se necesita saber a qué producto pertenece cada documento para agruparlos.
    public class DocumentoProductoConProductoDto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string Producto { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public string RutaArchivo { get; set; } = string.Empty;
        public string Tipo { get; set; } = "Manual";
    }
}
