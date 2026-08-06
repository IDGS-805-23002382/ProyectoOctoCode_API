namespace AuthenticationAPI.Models
{
    // Registro de baja de inventario por daño, caducidad, pérdida, error de registro, etc.
    // No es una venta ni una compra: descuenta stock (vía ICosteoService, igual que cualquier
    // consumo) y deja rastro de quién la registró, qué pasó y cuándo.
    public class MermaMateriaPrima
    {
        public int Id { get; set; }

        public int MateriaPrimaId { get; set; }
        public MateriaPrima? MateriaPrima { get; set; }

        public decimal Cantidad { get; set; }

        // Costo unitario promedio al momento del consumo (costoTotal/Cantidad que devolvió
        // ICosteoService.ConsumirAsync). Se guarda para poder restaurar el inventario al mismo valor.
        public decimal CostoUnitario { get; set; }

        // Elegido de una lista desplegable en el formulario (ej. "Dañado", "Caducado", "Perdido",
        // "Error de registro", "Otro").
        public string TipoMerma { get; set; } = string.Empty;

        // Texto libre: qué pasó exactamente con el insumo.
        public string Detalle { get; set; } = string.Empty;

        // Se llena automáticamente con el usuario autenticado que registra la merma (no se captura en el form).
        public string UsuarioRegistroId { get; set; } = string.Empty;
        public ApplicationUser? UsuarioRegistro { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public DateTime? FechaUltimaEdicion { get; set; }

        // Si se detectó un error y se decidió regresar la cantidad a inventario.
        public bool Restaurada { get; set; } = false;
        public DateTime? FechaRestauracion { get; set; }
    }
}
