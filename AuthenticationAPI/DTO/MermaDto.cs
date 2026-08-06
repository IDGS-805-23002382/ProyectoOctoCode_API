using System.ComponentModel.DataAnnotations;

namespace AuthenticationAPI.DTO
{
    public class RegistrarMermaDto
    {
        [Required]
        public int MateriaPrimaId { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public decimal Cantidad { get; set; }

        [Required(ErrorMessage = "Selecciona el tipo de merma.")]
        public string TipoMerma { get; set; } = string.Empty;

        [Required(ErrorMessage = "Describe qué pasó con el insumo.")]
        public string Detalle { get; set; } = string.Empty;
    }

    public class EditarMermaDto
    {
        [Required(ErrorMessage = "Selecciona el tipo de merma.")]
        public string TipoMerma { get; set; } = string.Empty;

        [Required(ErrorMessage = "Describe qué pasó con el insumo.")]
        public string Detalle { get; set; } = string.Empty;
    }

    public class MermaDto
    {
        public int Id { get; set; }
        public int MateriaPrimaId { get; set; }
        public string MateriaPrima { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal CostoTotal { get; set; }
        public string TipoMerma { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public string UsuarioRegistro { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaUltimaEdicion { get; set; }
        public bool Restaurada { get; set; }
        public DateTime? FechaRestauracion { get; set; }

        // true si aún está dentro de los 15 días y no ha sido restaurada: el frontend
        // usa esto para mostrar u ocultar los botones de Editar/Restaurar.
        public bool PuedeModificarse { get; set; }
    }
}
