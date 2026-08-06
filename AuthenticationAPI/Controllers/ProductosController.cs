using AuthenticationAPI.Data;
using AuthenticationAPI.DTO;
using AuthenticationAPI.Enums;
using AuthenticationAPI.interfaces;
using AuthenticationAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace AuthenticationAPI.Controllers
{
    public class SubirDocumentoDto
    {
        public string NombreArchivo { get; set; }
        public string Tipo { get; set; }
        public IFormFile Archivo { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ICosteoService _costeoService;

        public ProductosController(ApplicationDbContext db, ICosteoService costeoService)
        {
            _db = db;
            _costeoService = costeoService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> GetAll()
        {
            var productos = await _db.Productos
                .Include(p => p.Receta).ThenInclude(r => r.MateriaPrima)
                .Include(p => p.Documentos)
                .ToListAsync();
            var resultado = new List<ProductoDto>();
            foreach (var p in productos) resultado.Add(await MapDtoAsync(p));
            return Ok(resultado);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductoDto>> GetById(int id)
        {
            var producto = await _db.Productos
                .Include(p => p.Receta).ThenInclude(r => r.MateriaPrima)
                .Include(p => p.Documentos)
                .FirstOrDefaultAsync(p => p.Id == id);
            return producto == null ? NotFound() : Ok(await MapDtoAsync(producto));
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpPost]
        public async Task<ActionResult<ProductoDto>> Create(CrearProductoDto dto)
        {
            var producto = new Producto { Nombre = dto.Nombre, Descripcion = dto.Descripcion, PrecioVenta = dto.PrecioVenta };
            _db.Productos.Add(producto);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = producto.Id }, await MapDtoAsync(producto));
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, CrearProductoDto dto)
        {
            var producto = await _db.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            producto.Nombre = dto.Nombre;
            producto.Descripcion = dto.Descripcion;
            producto.PrecioVenta = dto.PrecioVenta;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpPatch("{id:int}/estatus")]
        public async Task<IActionResult> CambiarEstatus(int id, [FromBody] bool activo)
        {
            var producto = await _db.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            producto.Activo = activo;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpPost("{id:int}/receta")]
        public async Task<IActionResult> AgregarReceta(int id, AgregarRecetaDto dto)
        {
            var producto = await _db.Productos.FindAsync(id);
            if (producto == null) return NotFound("Producto no encontrado.");

            var materia = await _db.MateriasPrimas.FindAsync(dto.MateriaPrimaId);
            if (materia == null) return BadRequest("Materia prima no encontrada.");

            var renglon = await _db.RecetasBOM.FirstOrDefaultAsync(r => r.ProductoId == id && r.MateriaPrimaId == dto.MateriaPrimaId);
            if (renglon == null)
            {
                _db.RecetasBOM.Add(new RecetaBOM { ProductoId = id, MateriaPrimaId = dto.MateriaPrimaId, CantidadRequerida = dto.CantidadRequerida });
            }
            else
            {
                renglon.CantidadRequerida = dto.CantidadRequerida;
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpDelete("{id:int}/receta/{materiaPrimaId:int}")]
        public async Task<IActionResult> QuitarReceta(int id, int materiaPrimaId)
        {
            var renglon = await _db.RecetasBOM.FirstOrDefaultAsync(r => r.ProductoId == id && r.MateriaPrimaId == materiaPrimaId);
            if (renglon == null) return NotFound();
            _db.RecetasBOM.Remove(renglon);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpPost("{id:int}/fabricar")]
        public async Task<ActionResult<object>> Fabricar(int id, [FromQuery] decimal unidades = 1)
        {
            var receta = await _db.RecetasBOM.Where(r => r.ProductoId == id).ToListAsync();
            if (!receta.Any()) return BadRequest("El producto no tiene receta (BOM) definida.");

            decimal costoTotal = 0;
            foreach (var renglon in receta)
            {
                costoTotal += await _costeoService.ConsumirAsync(renglon.MateriaPrimaId, renglon.CantidadRequerida * unidades);
            }

            return Ok(new { ProductoId = id, Unidades = unidades, CostoTotalProduccion = costoTotal, CostoUnitario = costoTotal / unidades });
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpPost("{id:int}/documentos")]
        public async Task<IActionResult> AgregarDocumento(int id, [FromForm] SubirDocumentoDto dto)
        {
            var producto = await _db.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            string rutaArchivo = "";
            string nombreFinalArchivo = dto.NombreArchivo;

            if (dto.Archivo != null && dto.Archivo.Length > 0)
            {
                if (string.IsNullOrWhiteSpace(nombreFinalArchivo))
                {
                    nombreFinalArchivo = Path.GetFileNameWithoutExtension(dto.Archivo.FileName);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documentos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(dto.Archivo.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Archivo.CopyToAsync(stream);
                }
                rutaArchivo = $"/uploads/documentos/{uniqueFileName}";
            }

            if (string.IsNullOrWhiteSpace(nombreFinalArchivo))
            {
                return BadRequest("El nombre del archivo es obligatorio.");
            }

            var tipo = Enum.TryParse<TipoDocumento>(dto.Tipo, true, out var t) ? t : TipoDocumento.Otro;

            _db.DocumentosProducto.Add(new DocumentoProducto
            {
                ProductoId = id,
                NombreArchivo = nombreFinalArchivo,
                RutaArchivo = rutaArchivo,
                Tipo = tipo
            });

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = "Administrador,admin")]
        [HttpDelete("{id:int}/documentos/{documentoId:int}")]
        public async Task<IActionResult> QuitarDocumento(int id, int documentoId)
        {
            var documento = await _db.DocumentosProducto.FirstOrDefaultAsync(d => d.Id == documentoId && d.ProductoId == id);
            if (documento == null) return NotFound();
            _db.DocumentosProducto.Remove(documento);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpGet("documentos/{documentoId:int}/descargar")]
        public async Task<IActionResult> DescargarDocumento(int documentoId)
        {
            var documento = await _db.DocumentosProducto.FindAsync(documentoId);
            if (documento == null || string.IsNullOrWhiteSpace(documento.RutaArchivo)) return NotFound();

            var rutaRelativa = documento.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var rutaFisica = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", rutaRelativa);

            if (!System.IO.File.Exists(rutaFisica)) return NotFound("El archivo ya no está disponible.");

            var extension = Path.GetExtension(documento.RutaArchivo);
            var nombreDescarga = documento.NombreArchivo.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                ? documento.NombreArchivo
                : $"{documento.NombreArchivo}{extension}";

            var proveedorTipos = new FileExtensionContentTypeProvider();
            if (!proveedorTipos.TryGetContentType(rutaFisica, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return PhysicalFile(rutaFisica, contentType, nombreDescarga);
        }

        private async Task<ProductoDto> MapDtoAsync(Producto p) => new()
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            PrecioVenta = p.PrecioVenta,
            Activo = p.Activo,
            CostoUnitarioEstimado = await _costeoService.CalcularCostoProductoAsync(p.Id),
            Receta = p.Receta.Select(r => new RecetaBOMDto
            {
                MateriaPrimaId = r.MateriaPrimaId,
                MateriaPrima = r.MateriaPrima?.Nombre,
                CantidadRequerida = r.CantidadRequerida,
                UnidadMedida = r.MateriaPrima?.UnidadMedida
            }).ToList(),
            Documentos = p.Documentos.Select(d => new DocumentoProductoDto
            {
                Id = d.Id,
                NombreArchivo = d.NombreArchivo,
                RutaArchivo = d.RutaArchivo,
                Tipo = d.Tipo.ToString()
            }).ToList()
        };
    }
}