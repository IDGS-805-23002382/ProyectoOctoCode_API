using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthenticationAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        // ---- Modulo de negocio (fusionado desde OctoCode) ----
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<MateriaPrima> MateriasPrimas => Set<MateriaPrima>();
        public DbSet<CapaCosto> CapasCosto => Set<CapaCosto>();
        public DbSet<Compra> Compras => Set<Compra>();
        public DbSet<CompraDetalle> ComprasDetalle => Set<CompraDetalle>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<RecetaBOM> RecetasBOM => Set<RecetaBOM>();
        public DbSet<DocumentoProducto> DocumentosProducto => Set<DocumentoProducto>();
        public DbSet<Venta> Ventas => Set<Venta>();
        public DbSet<Comentario> Comentarios => Set<Comentario>();
        public DbSet<ResenaProducto> Resenas => Set<ResenaProducto>();
        public DbSet<SolicitudCotizacion> SolicitudesCotizacion => Set<SolicitudCotizacion>();
        public DbSet<MermaMateriaPrima> MermasMateriaPrima => Set<MermaMateriaPrima>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.NombreCompleto).HasMaxLength(200).IsRequired();
                entity.Property(e => e.RefreshToken).HasMaxLength(500);
            });

            // ---- Relaciones y precision numerica del modulo de negocio ----

            builder.Entity<Cliente>()
                .HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Cliente>()
                .HasIndex(c => c.UserId)
                .IsUnique();

            builder.Entity<RecetaBOM>()
                .HasIndex(r => new { r.ProductoId, r.MateriaPrimaId })
                .IsUnique();

            builder.Entity<MateriaPrima>()
                .Property(m => m.Stock).HasPrecision(18, 4);
            builder.Entity<MateriaPrima>()
                .Property(m => m.CostoUnitarioActual).HasPrecision(18, 4);
            builder.Entity<CapaCosto>()
                .Property(c => c.CantidadOriginal).HasPrecision(18, 4);
            builder.Entity<CapaCosto>()
                .Property(c => c.CantidadDisponible).HasPrecision(18, 4);
            builder.Entity<CapaCosto>()
                .Property(c => c.CostoUnitario).HasPrecision(18, 4);
            builder.Entity<CompraDetalle>()
                .Property(c => c.Cantidad).HasPrecision(18, 4);
            builder.Entity<CompraDetalle>()
                .Property(c => c.CostoUnitario).HasPrecision(18, 4);
            builder.Entity<RecetaBOM>()
                .Property(r => r.CantidadRequerida).HasPrecision(18, 4);
            builder.Entity<Producto>()
                .Property(p => p.PrecioVenta).HasPrecision(18, 2);
            builder.Entity<Compra>()
                .Property(c => c.Total).HasPrecision(18, 2);
            builder.Entity<Venta>()
                .Property(v => v.PrecioPagado).HasPrecision(18, 2);
            builder.Entity<SolicitudCotizacion>()
                .Property(s => s.MontoEstimado).HasPrecision(18, 2);
            builder.Entity<ResenaProducto>()
                .HasOne(r => r.Venta)
                .WithMany()
                .HasForeignKey(r => r.VentaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MermaMateriaPrima>()
                .Property(m => m.Cantidad).HasPrecision(18, 4);
            builder.Entity<MermaMateriaPrima>()
                .Property(m => m.CostoUnitario).HasPrecision(18, 4);
            builder.Entity<MermaMateriaPrima>()
                .HasOne(m => m.MateriaPrima)
                .WithMany()
                .HasForeignKey(m => m.MateriaPrimaId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<MermaMateriaPrima>()
                .HasOne(m => m.UsuarioRegistro)
                .WithMany()
                .HasForeignKey(m => m.UsuarioRegistroId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
