using AuthenticationAPI.Enums;

namespace AuthenticationAPI.Models
{
    // Un producto/proyecto que la empresa vende (ej. un sistema Acpan configurado para un cliente).
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioVenta { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Explosión de materiales (BOM/receta): de qué materias primas y en qué cantidad se compone.
        public ICollection<RecetaBOM> Receta { get; set; } = new List<RecetaBOM>();
        public ICollection<DocumentoProducto> Documentos { get; set; } = new List<DocumentoProducto>();
        public ICollection<ResenaProducto> Resenas { get; set; } = new List<ResenaProducto>();
    }

    // Renglón de la receta/BOM: X unidades de una materia prima por cada unidad de producto.
    public class RecetaBOM
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        public int MateriaPrimaId { get; set; }
        public MateriaPrima? MateriaPrima { get; set; }

        public decimal CantidadRequerida { get; set; }
    }

    // Documentación asociada al producto (manuales, guías) visible para el cliente que lo compró.
    public class DocumentoProducto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        public string NombreArchivo { get; set; } = string.Empty;
        public string RutaArchivo { get; set; } = string.Empty; // URL o path de almacenamiento
        public TipoDocumento Tipo { get; set; } = TipoDocumento.Manual;
        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
    }
}
