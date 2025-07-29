using System;
using System.Web.Http;
using GestorReservas.DAL;
using GestorReservas.Models;
using GestorReservas.Models.DTOs;
using GestorReservas.Utils; // ← AGREGAR ESTA LÍNEA
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;

namespace GestorReservas.Controllers
{
    public class AuthController : ApiController
    {
        private GestorReserva db = new GestorReserva();
        
        // Constructor para validar configuración
        public AuthController()
        {
            AppConfig.ValidateJwtConfiguration();
        }

        [HttpPost]
        [Route("api/Auth/login")]
        public IHttpActionResult Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validaciones adicionales
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Email y contraseña son obligatorios");

            // Buscar usuario por email
            var usuario = db.Usuarios
                .FirstOrDefault(u => u.Email == request.Email);

            if (usuario == null)
                return BadRequest("Credenciales inválidas");

            // Verificar contraseña hasheada
            if (!VerificarPassword(request.Password, usuario.Password))
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
                ExpiresIn = 25200 // 7 días en segundos (7 * 24 * 60 * 60)
            });
        }

        [HttpPost]
        [Route("api/Auth/logout")]
        public IHttpActionResult Logout()
        {
            // En JWT no hay logout del lado servidor, pero podemos validar el token
            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || authHeader.Scheme != "Bearer")
                return BadRequest("Token no proporcionado");

            return Ok(new
            {
                Message = "Logout exitoso. El token será invalidado del lado del cliente."
            });
        }

        [HttpPost]
        [Route("api/Auth/validate-token")]
        public IHttpActionResult ValidateToken()
        {
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            var usuario = db.Usuarios.Find(userInfo.Id);
            if (usuario == null)
                return BadRequest("Usuario no encontrado");

            return Ok(new
            {
                Valid = true,
                Usuario = new
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Email = usuario.Email,
                    Rol = usuario.Rol.ToString()
                }
            });
        }

        [HttpPost]
        [Route("api/Auth/change-password")]
        public IHttpActionResult CambiarPassword([FromBody] CambiarPasswordDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            var usuario = db.Usuarios.Find(userInfo.Id);
            if (usuario == null)
                return BadRequest("Usuario no encontrado");

            // Verificar contraseña actual
            if (!VerificarPassword(request.PasswordActual, usuario.Password))
                return BadRequest("La contraseña actual es incorrecta");

            // Validar nueva contraseña
            var validacion = ValidarPassword(request.PasswordNuevo);
            if (!validacion.EsValido)
                return BadRequest(validacion.Mensaje);

            // Actualizar contraseña
            usuario.Password = HashPassword(request.PasswordNuevo);
            db.SaveChanges();

            return Ok(new
            {
                Message = "Contraseña actualizada exitosamente"
            });
        }

        // Métodos auxiliares
        private string GenerateJwtToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(AppConfig.JwtSecretKey); // ← USAR CONFIG

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", usuario.Id.ToString()),
                    new Claim("email", usuario.Email),
                    new Claim("role", usuario.Rol.ToString()),
                    new Claim("name", usuario.Nombre)
                }),
                Expires = DateTime.UtcNow.AddDays(AppConfig.JwtExpirationDays), // ← USAR CONFIG
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private dynamic ValidateJwtToken()
        {
            try
            {
                var authHeader = Request.Headers.Authorization;
                if (authHeader == null || authHeader.Scheme != "Bearer")
                    return null;

                var token = authHeader.Parameter;
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(AppConfig.JwtSecretKey); // ← USAR CONFIG

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
                var userEmail = jwtToken.Claims.First(x => x.Type == "email").Value;
                var userRole = jwtToken.Claims.First(x => x.Type == "role").Value;

                return new { Id = userId, Email = userEmail, Role = userRole };
            }
            catch
            {
                return null;
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerificarPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
        }

        private ValidacionResult ValidarPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return new ValidacionResult(false, "La contraseña es obligatoria");

            if (password.Length < 6)
                return new ValidacionResult(false, "La contraseña debe tener al menos 6 caracteres");

            if (password.Length > 100)
                return new ValidacionResult(false, "La contraseña no puede tener más de 100 caracteres");

            return new ValidacionResult(true, "Contraseña válida");
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