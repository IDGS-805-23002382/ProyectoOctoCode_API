using AuthenticationAPI.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrador,admin")]
    public class RolesController : ControllerBase
    {

        private readonly IUserService _service;

        public RolesController(IUserService service) 
        {
            _service = service;
        
        }

        [HttpPost("{userId}/{role}")]
        public async Task<IActionResult> Assig(string userId, string role)
        {
            return Ok(await _service.AssignRoleAsync(userId, role));
        }
    }
}
