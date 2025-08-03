using System;
using System.Web.Http;
using GestorReservas.DAL;
using GestorReservas.Models;
using GestorReservas.Models.DTOs;
using GestorReservas.Utils;
using System.Linq;
using System.Data.Entity; // Importa Entity Framework para operaciones con la base de datos
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;

namespace GestorReservas.Controllers
{
    // Controlador para autenticación y gestión de usuarios
    public class AuthController : ApiController
    {
        // Contexto de base de datos
        private GestorReserva db = new GestorReserva();

        // Constructor: valida la configuración JWT al inicializar el controlador
        public AuthController()
        {
            AppConfig.ValidateJwtConfiguration();
        }

        // Endpoint para iniciar sesión
        [HttpPost]
        [Route("api/Auth/login")]
        public IHttpActionResult Login([FromBody] LoginRequest request)
        {
            // Valida el modelo recibido
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verifica que email y contraseña no estén vacíos
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Email y contraseña son obligatorios");

            // Busca el usuario por email (en minúsculas) e incluye el departamento
            var usuarioCompleto = db.Usuarios
                .Include(u => u.Departamento)
                .FirstOrDefault(u => u.Email == request.Email.ToLower());

            // Si no existe el usuario, retorna error
            if (usuarioCompleto == null)
                return BadRequest("Credenciales inválidas");

            // Verifica la contraseña hasheada
            if (!VerificarPassword(request.Password, usuarioCompleto.Password))
                return BadRequest("Credenciales inválidas");

            // Genera el token JWT
            var token = GenerateJwtToken(usuarioCompleto);

            // Construye el objeto de respuesta con información del usuario
            var usuarioResponse = new UsuarioResponseDto
            {
                Id = usuarioCompleto.Id,
                Nombre = usuarioCompleto.Nombre,
                Email = usuarioCompleto.Email,
                Rol = usuarioCompleto.Rol.ToString(),
                FechaCreacion = DateTime.Now,
                DepartamentoId = usuarioCompleto.DepartamentoId,
                Departamento = usuarioCompleto.Departamento != null ? new DepartamentoBasicoDto
                {
                    Id = usuarioCompleto.Departamento.Id,
                    Nombre = usuarioCompleto.Departamento.Nombre,
                    Codigo = usuarioCompleto.Departamento.Codigo,
                    Tipo = usuarioCompleto.Departamento.Tipo.ToString()
                } : null,
                EsJefeDepartamento = usuarioCompleto.Departamento != null && usuarioCompleto.Departamento.JefeId == usuarioCompleto.Id
            };

            // Retorna la respuesta con el token y datos del usuario
            return Ok(new
            {
                Message = "Login exitoso",
                Usuario = usuarioResponse,
                Token = token,
                ExpiresIn = 25200 // 7 días en segundos
            });
        }

        // Endpoint para cerrar sesión (solo informativo en JWT)
        [HttpPost]
        [Route("api/Auth/logout")]
        public IHttpActionResult Logout()
        {
            // Obtiene el header de autorización
            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || authHeader.Scheme != "Bearer")
                return BadRequest("Token no proporcionado");

            // En JWT, el logout se maneja en el cliente
            return Ok(new
            {
                Message = "Logout exitoso. El token será invalidado del lado del cliente."
            });
        }

        // Endpoint para validar el token JWT
        [HttpPost]
        [Route("api/Auth/validate-token")]
        public IHttpActionResult ValidateToken()
        {
            // Valida el token y obtiene la información del usuario
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // Busca el usuario en la base de datos
            var usuario = db.Usuarios.Find(userInfo.Id);
            if (usuario == null)
                return BadRequest("Usuario no encontrado");

            // Retorna la información básica del usuario si el token es válido
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

        // Endpoint para cambiar la contraseña del usuario autenticado
        [HttpPost]
        [Route("api/Auth/change-password")]
        public IHttpActionResult CambiarPassword([FromBody] CambiarPasswordDto request)
        {
            // Valida el modelo recibido
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Valida el token JWT
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // Busca el usuario en la base de datos
            var usuario = db.Usuarios.Find(userInfo.Id);
            if (usuario == null)
                return BadRequest("Usuario no encontrado");

            // Verifica la contraseña actual
            if (!VerificarPassword(request.PasswordActual, usuario.Password))
                return BadRequest("La contraseña actual es incorrecta");

            // Valida la nueva contraseña
            var validacion = ValidarPassword(request.PasswordNuevo);
            if (!validacion.EsValido)
                return BadRequest(validacion.Mensaje);

            // Actualiza la contraseña en la base de datos
            usuario.Password = HashPassword(request.PasswordNuevo);
            db.SaveChanges();

            // Retorna mensaje de éxito
            return Ok(new
            {
                Message = "Contraseña actualizada exitosamente"
            });
        }

        // Método auxiliar para generar el token JWT
        private string GenerateJwtToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(AppConfig.JwtSecretKey); // Obtiene la clave secreta de la configuración

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", usuario.Id.ToString()),
                    new Claim("email", usuario.Email),
                    new Claim("role", usuario.Rol.ToString()),
                    new Claim("name", usuario.Nombre)
                }),
                Expires = DateTime.UtcNow.AddDays(AppConfig.JwtExpirationDays), // Establece la expiración del token
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Método auxiliar para validar el token JWT y extraer información del usuario
        private dynamic ValidateJwtToken()
        {
            try
            {
                var authHeader = Request.Headers.Authorization;
                if (authHeader == null || authHeader.Scheme != "Bearer")
                    return null;

                var token = authHeader.Parameter;
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(AppConfig.JwtSecretKey);

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

        // Método auxiliar para hashear contraseñas usando SHA256
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Método auxiliar para verificar si la contraseña ingresada coincide con la almacenada
        private bool VerificarPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
        }

        // Método auxiliar para validar reglas de contraseña
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

        // Libera los recursos del contexto de base de datos
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
