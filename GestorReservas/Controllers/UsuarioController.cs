using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using GestorReservas.DAL;
using GestorReservas.Models;
using GestorReservas.Models.DTOs;
using GestorReservas.Utils;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace GestorReservas.Controllers
{
    /// <summary>
    /// Controlador para gestionar operaciones CRUD de usuarios
    /// Incluye autenticación JWT y autorización por roles
    /// </summary>
    public class UsuarioController : ApiController
    {
        // Contexto de base de datos para Entity Framework
        private GestorReserva db = new GestorReserva();

        /// <summary>
        /// Constructor que valida la configuración JWT al inicializar el controlador
        /// </summary>
        public UsuarioController()
        {
            AppConfig.ValidateJwtConfiguration();
        }

        #region GET Methods

        /// <summary>
        /// GET: api/Usuario
        /// Obtiene la lista completa de usuarios con información de departamento
        /// Requiere autenticación JWT válida
        /// </summary>
        /// <returns>Lista de usuarios con departamentos incluidos</returns>
        [HttpGet]
        public IHttpActionResult Get()
        {
            // Validar JWT token - verificar que el usuario esté autenticado
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // INCLUIR DEPARTAMENTO EN LA CONSULTA usando Entity Framework Include
            // Esto evita consultas N+1 cargando departamentos en una sola query
            var usuarios = db.Usuarios
                .Include(u => u.Departamento)
                .ToList();

            // ACTUALIZAR RESPUESTA PARA INCLUIR DEPARTAMENTO
            // Mapear usuarios a objeto anónimo con información completa
            var usuariosResponse = usuarios.Select(u => new
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Email = u.Email,
                Rol = u.Rol.ToString(), // Convertir enum a string
                DepartamentoId = u.DepartamentoId,
                // Incluir información completa del departamento si existe
                Departamento = u.Departamento != null ? new
                {
                    Id = u.Departamento.Id,
                    Nombre = u.Departamento.Nombre,
                    Codigo = u.Departamento.Codigo,
                    Tipo = u.Departamento.Tipo.ToString()
                } : null,
                // Determinar si el usuario es jefe de su departamento
                EsJefeDepartamento = u.Departamento != null && u.Departamento.JefeId == u.Id
            });

            return Ok(usuariosResponse);
        }

        /// <summary>
        /// GET: api/Usuario/{id}
        /// Obtiene un usuario específico por ID con información de departamento
        /// Autorización: Solo el propio usuario, administradores o coordinadores
        /// </summary>
        /// <param name="id">ID del usuario a consultar</param>
        /// <returns>Información del usuario con departamento</returns>
        [HttpGet]
        public IHttpActionResult Get(int id)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // INCLUIR DEPARTAMENTO EN LA CONSULTA
            var usuario = db.Usuarios
                .Include(u => u.Departamento)
                .FirstOrDefault(u => u.Id == id);

            if (usuario == null)
                return NotFound();

            // CONTROL DE ACCESO: Solo devolver datos propios o si es admin/coordinador
            // Esto implementa el principio de menor privilegio
            if (userInfo.Id != id && userInfo.Role != "Administrador" && userInfo.Role != "Coordinador")
                return Unauthorized();

            // ACTUALIZAR RESPUESTA PARA INCLUIR DEPARTAMENTO
            var usuarioResponse = new
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString(),
                DepartamentoId = usuario.DepartamentoId,
                Departamento = usuario.Departamento != null ? new
                {
                    Id = usuario.Departamento.Id,
                    Nombre = usuario.Departamento.Nombre,
                    Codigo = usuario.Departamento.Codigo,
                    Tipo = usuario.Departamento.Tipo.ToString()
                } : null,
                EsJefeDepartamento = usuario.Departamento != null && usuario.Departamento.JefeId == usuario.Id
            };

            return Ok(usuarioResponse);
        }

        /// <summary>
        /// GET: api/Usuario/Perfil
        /// Obtiene el perfil del usuario autenticado actualmente
        /// </summary>
        /// <returns>Información completa del perfil del usuario logueado</returns>
        [HttpGet]
        [Route("api/Usuario/Perfil")]
        public IHttpActionResult Perfil()
        {
            // Validar token JWT y obtener información del usuario
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // CONSULTAR USUARIO CON DEPARTAMENTO - userInfo.Id funciona correctamente
            var usuario = db.Usuarios
                .Include(u => u.Departamento)
                .FirstOrDefault(u => u.Id == userInfo.Id);

            if (usuario == null)
                return NotFound();

            // Retornar información completa del perfil
            return Ok(new
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString(),
                DepartamentoId = usuario.DepartamentoId,
                Departamento = usuario.Departamento != null ? new
                {
                    Id = usuario.Departamento.Id,
                    Nombre = usuario.Departamento.Nombre,
                    Codigo = usuario.Departamento.Codigo,
                    Tipo = usuario.Departamento.Tipo.ToString()
                } : null,
                EsJefeDepartamento = usuario.Departamento != null && usuario.Departamento.JefeId == usuario.Id
            });
        }

        #endregion

        #region POST Methods

        /// <summary>
        /// POST: api/Usuario
        /// Crea un nuevo usuario en el sistema
        /// Incluye validaciones de email, contraseña y departamento
        /// </summary>
        /// <param name="usuarioDto">Datos del usuario a registrar</param>
        /// <returns>Usuario creado con información completa</returns>
        [HttpPost]
        public IHttpActionResult Post(UsuarioRegistroDto usuarioDto)
        {
            // Validar modelo según DataAnnotations
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // VALIDACIÓN DE EMAIL ÚNICO
            var existeUsuario = db.Usuarios.Any(u => u.Email == usuarioDto.Email);
            if (existeUsuario)
                return BadRequest("Ya existe un usuario con este email");

            // VALIDACIÓN DE FORMATO DE EMAIL
            if (!EsEmailValido(usuarioDto.Email))
                return BadRequest("El formato del email no es válido");

            // VALIDACIÓN DE CONTRASEÑA (longitud, complejidad, etc.)
            var validacionPassword = ValidarPassword(usuarioDto.Password);
            if (!validacionPassword.EsValido)
                return BadRequest(validacionPassword.Mensaje);

            // VALIDAR DEPARTAMENTO SI SE PROPORCIONA
            if (usuarioDto.DepartamentoId.HasValue)
            {
                var existeDepartamento = db.Departamentos.Any(d => d.Id == usuarioDto.DepartamentoId.Value);
                if (!existeDepartamento)
                    return BadRequest("El departamento especificado no existe");
            }

            // CREAR USUARIO CON CONTRASEÑA HASHEADA
            var usuario = new Usuario
            {
                Nombre = usuarioDto.Nombre,
                Email = usuarioDto.Email.ToLower(), // Normalizar email a minúsculas
                Password = HashPassword(usuarioDto.Password), // Hash SHA256
                Rol = usuarioDto.Rol,
                DepartamentoId = usuarioDto.DepartamentoId
            };

            // Guardar en base de datos
            db.Usuarios.Add(usuario);
            db.SaveChanges();

            // INCLUIR DEPARTAMENTO EN LA RESPUESTA - consulta adicional necesaria
            var usuarioConDepartamento = db.Usuarios
                .Include(u => u.Departamento)
                .FirstOrDefault(u => u.Id == usuario.Id);

            // Preparar respuesta con información completa
            var respuesta = new
            {
                Id = usuarioConDepartamento.Id,
                Nombre = usuarioConDepartamento.Nombre,
                Email = usuarioConDepartamento.Email,
                Rol = usuarioConDepartamento.Rol.ToString(),
                DepartamentoId = usuarioConDepartamento.DepartamentoId,
                Departamento = usuarioConDepartamento.Departamento != null ? new
                {
                    Id = usuarioConDepartamento.Departamento.Id,
                    Nombre = usuarioConDepartamento.Departamento.Nombre,
                    Codigo = usuarioConDepartamento.Departamento.Codigo,
                    Tipo = usuarioConDepartamento.Departamento.Tipo.ToString()
                } : null,
                Message = "Usuario registrado exitosamente"
            };

            return Ok(respuesta);
        }

        #endregion

        #region PUT Methods

        /// <summary>
        /// PUT: api/Usuario/{id}
        /// Actualiza un usuario existente
        /// Autorización: Solo el propio usuario o administradores
        /// Los administradores pueden cambiar roles y departamentos
        /// </summary>
        /// <param name="id">ID del usuario a actualizar</param>
        /// <param name="usuarioDto">Datos de actualización</param>
        /// <returns>Usuario actualizado</returns>
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

            // CONTROL DE ACCESO: Solo puede actualizar datos propios, si es admin, o si es coordinador del mismo departamento
            bool esCoordinadorDelMismoDepartamento = false;
            if (userInfo.Role == "Coordinador")
            {
                var coordinador = db.Usuarios.Find(userInfo.Id);
                esCoordinadorDelMismoDepartamento = coordinador != null &&
                    usuario.DepartamentoId.HasValue &&
                    coordinador.DepartamentoId.HasValue &&
                    usuario.DepartamentoId == coordinador.DepartamentoId;
            }

            if (userInfo.Id != id && userInfo.Role != "Administrador" && !esCoordinadorDelMismoDepartamento)
                return Unauthorized();

            // ACTUALIZAR EMAIL SI SE ESTÁ CAMBIANDO
            if (!string.IsNullOrEmpty(usuarioDto.Email) && usuarioDto.Email != usuario.Email)
            {
                // Validar formato
                if (!EsEmailValido(usuarioDto.Email))
                    return BadRequest("El formato del email no es válido");

                // Verificar que no exista otro usuario con el mismo email
                var existeEmail = db.Usuarios.Any(u => u.Email == usuarioDto.Email.ToLower() && u.Id != id);
                if (existeEmail)
                    return BadRequest("Ya existe otro usuario con este email");

                usuario.Email = usuarioDto.Email.ToLower();
            }

            // ACTUALIZAR NOMBRE SI SE PROPORCIONA
            if (!string.IsNullOrEmpty(usuarioDto.Nombre))
                usuario.Nombre = usuarioDto.Nombre;

            // ACTUALIZAR ROL - SOLO ADMINISTRADORES PUEDEN CAMBIAR ROLES
            if (userInfo.Role == "Administrador" && usuarioDto.Rol.HasValue)
                usuario.Rol = usuarioDto.Rol.Value;

            // ACTUALIZAR DEPARTAMENTO - SOLO ADMINISTRADORES
            if (userInfo.Role == "Administrador" && usuarioDto.DepartamentoId.HasValue)
            {
                if (usuarioDto.DepartamentoId.Value == 0)
                {
                    // Valor 0 significa remover departamento
                    usuario.DepartamentoId = null;
                }
                else
                {
                    // Validar que el departamento existe
                    var existeDepartamento = db.Departamentos.Any(d => d.Id == usuarioDto.DepartamentoId.Value);
                    if (!existeDepartamento)
                        return BadRequest("El departamento especificado no existe");

                    usuario.DepartamentoId = usuarioDto.DepartamentoId.Value;
                }
            }

            // ACTUALIZAR CONTRASEÑA SI SE PROPORCIONA
            if (!string.IsNullOrEmpty(usuarioDto.NuevoPassword))
            {
                var validacionPassword = ValidarPassword(usuarioDto.NuevoPassword);
                if (!validacionPassword.EsValido)
                    return BadRequest(validacionPassword.Mensaje);

                usuario.Password = HashPassword(usuarioDto.NuevoPassword);
            }

            // Marcar como modificado y guardar
            db.Entry(usuario).State = EntityState.Modified;
            db.SaveChanges();

            // INCLUIR DEPARTAMENTO EN LA RESPUESTA
            var usuarioActualizado = db.Usuarios
                .Include(u => u.Departamento)
                .FirstOrDefault(u => u.Id == id);

            return Ok(new
            {
                Id = usuarioActualizado.Id,
                Nombre = usuarioActualizado.Nombre,
                Email = usuarioActualizado.Email,
                Rol = usuarioActualizado.Rol.ToString(),
                DepartamentoId = usuarioActualizado.DepartamentoId,
                Departamento = usuarioActualizado.Departamento != null ? new
                {
                    Id = usuarioActualizado.Departamento.Id,
                    Nombre = usuarioActualizado.Departamento.Nombre,
                    Codigo = usuarioActualizado.Departamento.Codigo,
                    Tipo = usuarioActualizado.Departamento.Tipo.ToString()
                } : null,
                Message = "Usuario actualizado exitosamente"
            });
        }

        #endregion

        #region DELETE Methods

        /// <summary>
        /// DELETE: api/Usuario/{id}
        /// Elimina un usuario del sistema
        /// Autorización: Solo administradores
        /// Incluye validaciones de integridad referencial
        /// </summary>
        /// <param name="id">ID del usuario a eliminar</param>
        /// <returns>Confirmación de eliminación</returns>
        [HttpDelete]
        public IHttpActionResult Delete(int id)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // SOLO ADMINISTRADORES PUEDEN ELIMINAR USUARIOS
            if (userInfo.Role != "Administrador")
                return Unauthorized();

            var usuario = db.Usuarios.Find(id);
            if (usuario == null)
                return NotFound();

            // PROTECCIÓN: No permitir eliminar el último administrador
            if (usuario.Rol == RolUsuario.Administrador)
            {
                var totalAdmins = db.Usuarios.Count(u => u.Rol == RolUsuario.Administrador);
                if (totalAdmins <= 1)
                    return BadRequest("No se puede eliminar el último administrador del sistema");
            }

            // VERIFICAR SI ES JEFE DE DEPARTAMENTO
            // Prevenir eliminación de usuarios que son jefes
            var esJefeDepartamento = db.Departamentos.Any(d => d.JefeId == id);
            if (esJefeDepartamento)
                return BadRequest("No se puede eliminar un usuario que es jefe de departamento. Asigne otro jefe primero.");

            // VERIFICAR INTEGRIDAD REFERENCIAL - reservas activas
            var tieneReservasActivas = db.Reservas.Any(r => r.UsuarioId == id &&
                (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Aprobada));

            if (tieneReservasActivas)
                return BadRequest("El usuario tiene reservas activas. Cancele las reservas antes de eliminar.");

            // Proceder con la eliminación
            db.Usuarios.Remove(usuario);
            db.SaveChanges();

            return Ok(new
            {
                Message = "Usuario eliminado exitosamente",
                UsuarioId = id
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

        /// <summary>
        /// Valida el formato de email usando MailAddress
        /// </summary>
        /// <param name="email">Email a validar</param>
        /// <returns>True si el formato es válido</returns>
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

        #endregion

        #region Métodos de Criptografía

        /// <summary>
        /// Genera hash SHA256 de la contraseña
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <returns>Hash en Base64</returns>
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// Verifica si una contraseña coincide con su hash
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <param name="hashedPassword">Hash almacenado</param>
        /// <returns>True si coinciden</returns>
        private bool VerificarPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
        }

        #endregion

        #region JWT Methods

        /// <summary>
        /// Genera token JWT para un usuario autenticado
        /// </summary>
        /// <param name="usuario">Usuario para el cual generar token</param>
        /// <returns>Token JWT como string</returns>
        private string GenerarTokenJWT(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(AppConfig.JwtSecretKey);

            // Configurar claims del token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("id", usuario.Id.ToString()),
                    new System.Security.Claims.Claim("email", usuario.Email),
                    new System.Security.Claims.Claim("role", usuario.Rol.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(AppConfig.JwtExpirationDays),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Valida token JWT del header Authorization
        /// </summary>
        /// <returns>Información del usuario extraída del token o null si inválido</returns>
        private JwtUserInfo ValidateJwtToken()
        {
            try
            {
                // Obtener header Authorization
                var authHeader = Request.Headers.Authorization;
                if (authHeader == null || authHeader.Scheme != "Bearer")
                    return null;

                var token = authHeader.Parameter;
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(AppConfig.JwtSecretKey);

                // Validar token con parámetros de seguridad
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false, // No validamos issuer en este caso
                    ValidateAudience = false, // No validamos audience en este caso
                    ClockSkew = TimeSpan.Zero // Sin tolerancia de tiempo
                }, out SecurityToken validatedToken);

                // Extraer claims del token validado
                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
                var userEmail = jwtToken.Claims.First(x => x.Type == "email").Value;
                var userRole = jwtToken.Claims.First(x => x.Type == "role").Value;

                // RETORNAR OBJETO TIPADO JwtUserInfo
                return new JwtUserInfo
                {
                    Id = userId,
                    Email = userEmail,
                    Role = userRole
                };
            }
            catch
            {
                // Cualquier error en validación retorna null (no autorizado)
                return null;
            }
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Libera recursos del contexto de base de datos
        /// </summary>
        /// <param name="disposing">Si está siendo disposed</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
