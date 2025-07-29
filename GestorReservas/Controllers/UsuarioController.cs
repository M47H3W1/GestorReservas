using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using GestorReservas.DAL;
using GestorReservas.Models;
using GestorReservas.Models.DTOs;
using GestorReservas.Utils; // ← AGREGAR ESTA LÍNEA
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace GestorReservas.Controllers
{
    public class UsuarioController : ApiController
    {
        private GestorReserva db = new GestorReserva();
        // ← ELIMINAR: private readonly string secretKey = "...";

        // Constructor para validar configuración
        public UsuarioController()
        {
            AppConfig.ValidateJwtConfiguration();
        }

        // GET: api/Usuario
        [HttpGet]
        public IHttpActionResult Get()
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            var usuarios = db.Usuarios.ToList();

            // No devolver contraseñas
            var usuariosResponse = usuarios.Select(u => new
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Email = u.Email,
                Rol = u.Rol.ToString()
            });

            return Ok(usuariosResponse);
        }

        // GET: api/Usuario/{id}
        [HttpGet]
        public IHttpActionResult Get(int id)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            var usuario = db.Usuarios.Find(id);
            if (usuario == null)
                return NotFound();

            // Solo devolver datos propios o si es admin/coordinador
            if (userInfo.Id != id && userInfo.Role != "Administrador" && userInfo.Role != "Coordinador")
                return Unauthorized();

            var usuarioResponse = new
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString()
            };

            return Ok(usuarioResponse);
        }

        // POST: api/Usuario
        [HttpPost]
        public IHttpActionResult Post(UsuarioRegistroDto usuarioDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar que el email no exista
            var existeUsuario = db.Usuarios.Any(u => u.Email == usuarioDto.Email);
            if (existeUsuario)
                return BadRequest("Ya existe un usuario con este email");

            // Validar email formato
            if (!EsEmailValido(usuarioDto.Email))
                return BadRequest("El formato del email no es válido");

            // Validar contraseña
            var validacionPassword = ValidarPassword(usuarioDto.Password);
            if (!validacionPassword.EsValido)
                return BadRequest(validacionPassword.Mensaje);

            // Crear usuario
            var usuario = new Usuario
            {
                Nombre = usuarioDto.Nombre,
                Email = usuarioDto.Email.ToLower(),
                Password = HashPassword(usuarioDto.Password),
                Rol = usuarioDto.Rol
                // ← QUITAR: FechaCreacion = DateTime.Now
            };

            db.Usuarios.Add(usuario);
            db.SaveChanges();

            var respuesta = new
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString(),
                Message = "Usuario registrado exitosamente"
                // ← QUITAR: FechaCreacion = usuario.FechaCreacion
            };

            return Ok(respuesta); // Cambiar CreatedAtRoute por Ok
        }

        // PUT: api/Usuario/{id}
        [HttpPut]
        public IHttpActionResult Put(int id, UsuarioActualizarDto usuarioDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            var usuario = db.Usuarios.Find(id);
            if (usuario == null)
                return NotFound();

            // Solo puede actualizar datos propios o si es admin
            if (userInfo.Id != id && userInfo.Role != "Administrador")
                return Unauthorized();

            // Validar email si se está cambiando
            if (!string.IsNullOrEmpty(usuarioDto.Email) && usuarioDto.Email != usuario.Email)
            {
                if (!EsEmailValido(usuarioDto.Email))
                    return BadRequest("El formato del email no es válido");

                var existeEmail = db.Usuarios.Any(u => u.Email == usuarioDto.Email.ToLower() && u.Id != id);
                if (existeEmail)
                    return BadRequest("Ya existe otro usuario con este email");

                usuario.Email = usuarioDto.Email.ToLower();
            }

            // Actualizar nombre si se proporciona
            if (!string.IsNullOrEmpty(usuarioDto.Nombre))
                usuario.Nombre = usuarioDto.Nombre;

            // Actualizar rol solo si es admin
            if (userInfo.Role == "Administrador" && usuarioDto.Rol.HasValue)
                usuario.Rol = usuarioDto.Rol.Value;

            // Actualizar contraseña si se proporciona
            if (!string.IsNullOrEmpty(usuarioDto.NuevoPassword))
            {
                var validacionPassword = ValidarPassword(usuarioDto.NuevoPassword);
                if (!validacionPassword.EsValido)
                    return BadRequest(validacionPassword.Mensaje);

                usuario.Password = HashPassword(usuarioDto.NuevoPassword);
            }

            db.Entry(usuario).State = EntityState.Modified;
            db.SaveChanges();

            return Ok(new
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString(),
                Message = "Usuario actualizado exitosamente"
            });
        }

        // DELETE: api/Usuario/{id}
        [HttpDelete]
        public IHttpActionResult Delete(int id)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // Solo admin puede eliminar usuarios
            if (userInfo.Role != "Administrador")
                return Unauthorized();

            var usuario = db.Usuarios.Find(id);
            if (usuario == null)
                return NotFound();

            // No permitir eliminar el último administrador
            if (usuario.Rol == RolUsuario.Administrador)
            {
                var totalAdmins = db.Usuarios.Count(u => u.Rol == RolUsuario.Administrador);
                if (totalAdmins <= 1)
                    return BadRequest("No se puede eliminar el último administrador del sistema");
            }

            // Verificar si tiene reservas pendientes o aprobadas
            var tieneReservasActivas = db.Reservas.Any(r => r.UsuarioId == id &&
                (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Aprobada));

            if (tieneReservasActivas)
                return BadRequest("El usuario tiene reservas activas. Cancele las reservas antes de eliminar.");

            db.Usuarios.Remove(usuario);
            db.SaveChanges();

            return Ok(new
            {
                Message = "Usuario eliminado exitosamente",
                UsuarioId = id
            });
        }

        // GET: api/Usuario/Perfil
        [HttpGet]
        [Route("api/Usuario/Perfil")]
        public IHttpActionResult Perfil()
        {
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            var usuario = db.Usuarios.Find(userInfo.Id);
            if (usuario == null)
                return NotFound();

            return Ok(new
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString()
            });
        }

        // Métodos auxiliares
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

        private bool EsEmailValido(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
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

        private string GenerarTokenJWT(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(AppConfig.JwtSecretKey); // ← USAR CONFIG

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("id", usuario.Id.ToString()),
                    new System.Security.Claims.Claim("email", usuario.Email),
                    new System.Security.Claims.Claim("role", usuario.Rol.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(AppConfig.JwtExpirationDays), // ← USAR CONFIG
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
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
