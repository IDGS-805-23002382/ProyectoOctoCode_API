namespace AuthenticationAPI.Models
{
    // Datos de negocio del cliente. Un Cliente SIEMPRE está ligado a un ApplicationUser
    // (creado por un administrador), que es lo que le da acceso a la Sección de Clientes.
    public class Cliente
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? Usuario { get; set; }

        public string RazonSocial { get; set; } = string.Empty;
        public string? RFC { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public ICollection<Compra> Compras { get; set; } = new List<Compra>();
        public ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();
        public ICollection<ResenaProducto> Resenas { get; set; } = new List<ResenaProducto>();
    }
}
