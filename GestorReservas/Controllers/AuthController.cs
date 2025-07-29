using System;
using System.Web.Http;
using GestorReservas.DAL;
using GestorReservas.Models;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GestorReservas.Controllers
{
    public class AuthController : ApiController
    {
        private GestorReserva db = new GestorReserva();
        private readonly string secretKey = "tu-clave-secreta-super-segura-de-al-menos-32-caracteres"; // En producción usar configuración

        [HttpPost]
        [Route("api/Auth/login")]
        public IHttpActionResult Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = db.Usuarios
                .FirstOrDefault(u => u.Email == request.Email && u.Password == request.Password);

            if (usuario == null)
                return BadRequest("Credenciales inválidas");

            // Generar JWT token
            var token = GenerateJwtToken(usuario);

            return Ok(new
            {
                Message = "Login exitoso",
                Usuario = new
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Email = usuario.Email,
                    Rol = usuario.Rol.ToString()
                },
                Token = token,
                ExpiresIn = 3600 // 1 hora en segundos
            });
        }

        private string GenerateJwtToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", usuario.Id.ToString()),
                    new Claim("email", usuario.Email),
                    new Claim("role", usuario.Rol.ToString()),
                    new Claim("name", usuario.Nombre)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}