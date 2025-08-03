using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using GestorReservas.DAL;
using GestorReservas.Models;
using GestorReservas.Models.DTOs;
using GestorReservas.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GestorReservas.Controllers
{
    // Controlador para la gestión de espacios físicos
    public class EspacioController : ApiController
    {
        // Contexto de base de datos para acceder a entidades
        private GestorReserva db = new GestorReserva();

        // Constructor: valida la configuración JWT al inicializar el controlador
        public EspacioController()
        {
            AppConfig.ValidateJwtConfiguration();
        }

        // GET: api/Espacio
        // Obtiene todos los espacios registrados en la base de datos
        [HttpGet]
        public IHttpActionResult ObtenerEspacios()
        {
            var espacios = db.Espacios.ToList(); // Recupera todos los espacios
            return Ok(espacios); // Devuelve la lista en formato JSON
        }

        // GET: api/Espacio/{id}
        // Obtiene un espacio específico por su ID
        [HttpGet]
        public IHttpActionResult ObtenerEspacio(int id)
        {
            var espacio = db.Espacios.Find(id); // Busca el espacio por ID
            if (espacio == null) // Si no existe, retorna 404
                return NotFound();
            return Ok(espacio); // Devuelve el espacio encontrado
        }

        // POST: api/Espacio
        // Crea un nuevo espacio físico
        [HttpPost]
        public IHttpActionResult CrearEspacio(EspacioDto espacioDto)
        {
            if (!ModelState.IsValid) // Valida el modelo recibido
                return BadRequest(ModelState);

            // Validar JWT token para autenticación
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // Solo administradores y coordinadores pueden crear espacios
            if (userInfo.Role != "Administrador" && userInfo.Role != "Coordinador")
                return Unauthorized();

            // Verifica que no exista un espacio con el mismo nombre (case-insensitive)
            var existeEspacio = db.Espacios.Any(e => e.Nombre.ToLower() == espacioDto.Nombre.ToLower());
            if (existeEspacio)
                return BadRequest("Ya existe un espacio con este nombre");

            // Valida los datos del espacio usando reglas de negocio
            var validacion = ValidarEspacio(espacioDto);
            if (!validacion.EsValido)
                return BadRequest(validacion.Mensaje);

            // Crea la entidad Espacio a partir del DTO recibido
            var espacio = new Espacio
            {
                Nombre = espacioDto.Nombre,
                Tipo = espacioDto.Tipo,
                Capacidad = espacioDto.Capacidad,
                Ubicacion = espacioDto.Ubicacion,
                Descripcion = espacioDto.Descripcion,
                Disponible = espacioDto.Disponible.HasValue ? espacioDto.Disponible.Value : true // Por defecto disponible
            };

            db.Espacios.Add(espacio); // Agrega el espacio a la base de datos
            db.SaveChanges(); // Guarda los cambios

            // Devuelve el espacio creado y un mensaje de éxito
            return CreatedAtRoute("DefaultApi", new { id = espacio.Id }, new
            {
                Id = espacio.Id,
                Nombre = espacio.Nombre,
                Tipo = espacio.Tipo.ToString(),
                Capacidad = espacio.Capacidad,
                Ubicacion = espacio.Ubicacion,
                Descripcion = espacio.Descripcion,
                Disponible = espacio.Disponible,
                Message = "Espacio creado exitosamente"
            });
        }

        // PUT: api/Espacio/{id}
        // Actualiza los datos de un espacio existente
        [HttpPut]
        public IHttpActionResult ActualizarEspacio(int id, EspacioDto espacioDto)
        {
            if (!ModelState.IsValid) // Valida el modelo recibido
                return BadRequest(ModelState);

            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // Solo administradores y coordinadores pueden actualizar espacios
            if (userInfo.Role != "Administrador" && userInfo.Role != "Coordinador")
                return Unauthorized();

            var espacio = db.Espacios.Find(id); // Busca el espacio por ID
            if (espacio == null)
                return NotFound();

            // Verifica que no exista otro espacio con el mismo nombre
            if (espacioDto.Nombre != espacio.Nombre)
            {
                var existeEspacio = db.Espacios.Any(e => e.Nombre.ToLower() == espacioDto.Nombre.ToLower() && e.Id != id);
                if (existeEspacio)
                    return BadRequest("Ya existe otro espacio con este nombre");
            }

            // Valida los datos del espacio
            var validacion = ValidarEspacio(espacioDto);
            if (!validacion.EsValido)
                return BadRequest(validacion.Mensaje);

            // Actualiza las propiedades del espacio
            espacio.Nombre = espacioDto.Nombre;
            espacio.Tipo = espacioDto.Tipo;
            espacio.Capacidad = espacioDto.Capacidad;
            espacio.Ubicacion = espacioDto.Ubicacion;
            espacio.Descripcion = espacioDto.Descripcion;

            if (espacioDto.Disponible.HasValue)
                espacio.Disponible = espacioDto.Disponible.Value;

            db.Entry(espacio).State = EntityState.Modified; // Marca la entidad como modificada
            db.SaveChanges(); // Guarda los cambios

            // Devuelve el espacio actualizado y un mensaje de éxito
            return Ok(new
            {
                Id = espacio.Id,
                Nombre = espacio.Nombre,
                Tipo = espacio.Tipo.ToString(),
                Capacidad = espacio.Capacidad,
                Ubicacion = espacio.Ubicacion,
                Descripcion = espacio.Descripcion,
                Disponible = espacio.Disponible,
                Message = "Espacio actualizado exitosamente"
            });
        }

        // DELETE: api/Espacio/{id}
        // Elimina un espacio físico (solo administradores)
        [HttpDelete]
        public IHttpActionResult EliminarEspacio(int id)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // Solo administradores pueden eliminar espacios
            if (userInfo.Role != "Administrador")
                return Unauthorized();

            var espacio = db.Espacios.Find(id); // Busca el espacio por ID
            if (espacio == null)
                return NotFound();

            // Verifica si el espacio tiene reservas activas (pendientes o aprobadas)
            var tieneReservasActivas = db.Reservas.Any(r => r.EspacioId == id &&
                (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Aprobada));

            if (tieneReservasActivas)
                return BadRequest("El espacio tiene reservas activas. Cancele las reservas antes de eliminar.");

            db.Espacios.Remove(espacio); // Elimina el espacio
            db.SaveChanges(); // Guarda los cambios

            // Devuelve mensaje de éxito y el ID del espacio eliminado
            return Ok(new
            {
                Message = "Espacio eliminado exitosamente",
                EspacioId = id
            });
        }

        // GET: api/Espacio/tipos
        // Obtiene la lista de tipos de espacio definidos en el enum TipoEspacio
        [HttpGet]
        [Route("api/Espacio/tipos")]
        public IHttpActionResult ObtenerTiposEspacio()
        {
            // Obtiene todos los valores del enum TipoEspacio y los proyecta en objetos con valor y nombre
            var tipos = Enum.GetValues(typeof(TipoEspacio))
                .Cast<TipoEspacio>()
                .Select(t => new {
                    Valor = (int)t,
                    Nombre = t.ToString()
                })
                .ToList();

            return Ok(tipos); // Devuelve la lista de tipos
        }

        // GET: api/Espacio/disponibles
        // Obtiene todos los espacios que están marcados como disponibles
        [HttpGet]
        [Route("api/Espacio/disponibles")]
        public IHttpActionResult ObtenerEspaciosDisponibles()
        {
            var espaciosDisponibles = db.Espacios
                .Where(e => e.Disponible) // Filtra solo los espacios disponibles
                .ToList();

            return Ok(espaciosDisponibles); // Devuelve la lista
        }

        // GET: api/Espacio/tipo/{tipo}
        // Obtiene los espacios filtrados por tipo
        [HttpGet]
        [Route("api/Espacio/tipo/{tipo}")]
        public IHttpActionResult ObtenerEspaciosPorTipo(int tipo)
        {
            // Valida que el tipo recibido sea válido en el enum
            if (!Enum.IsDefined(typeof(TipoEspacio), tipo))
                return BadRequest("Tipo de espacio inválido");

            var tipoEspacio = (TipoEspacio)tipo; // Convierte el entero al enum
            var espacios = db.Espacios
                .Where(e => e.Tipo == tipoEspacio) // Filtra por tipo
                .ToList();

            return Ok(espacios); // Devuelve la lista filtrada
        }

        // PUT: api/Espacio/{id}/disponibilidad
        // Cambia la disponibilidad de un espacio (habilitado/deshabilitado)
        [HttpPut]
        [Route("api/Espacio/{id}/disponibilidad")]
        public IHttpActionResult CambiarDisponibilidad(int id, DisponibilidadDto disponibilidadDto)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // Solo administradores y coordinadores pueden cambiar disponibilidad
            if (userInfo.Role != "Administrador" && userInfo.Role != "Coordinador")
                return Unauthorized();

            var espacio = db.Espacios.Find(id); // Busca el espacio por ID
            if (espacio == null)
                return NotFound();

            // Si se va a deshabilitar el espacio, verifica que no tenga reservas activas
            if (!disponibilidadDto.Disponible)
            {
                var tieneReservasActivas = db.Reservas.Any(r => r.EspacioId == id &&
                    (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Aprobada));

                if (tieneReservasActivas)
                    return BadRequest("El espacio tiene reservas activas. Cancele las reservas antes de deshabilitar.");
            }

            espacio.Disponible = disponibilidadDto.Disponible; // Actualiza la disponibilidad
            db.Entry(espacio).State = EntityState.Modified; // Marca la entidad como modificada
            db.SaveChanges(); // Guarda los cambios

            var estado = disponibilidadDto.Disponible ? "habilitado" : "deshabilitado";
            // Devuelve mensaje de éxito y el estado actual
            return Ok(new
            {
                Message = string.Format("Espacio {0} exitosamente", estado),
                EspacioId = id,
                Disponible = espacio.Disponible
            });
        }

        // GET: api/Espacio/{id}/estadisticas
        // Obtiene estadísticas de uso para un espacio específico
        [HttpGet]
        [Route("api/Espacio/{id}/estadisticas")]
        public IHttpActionResult ObtenerEstadisticasEspacio(int id)
        {
            var espacio = db.Espacios.Find(id); // Busca el espacio por ID
            if (espacio == null)
                return NotFound();

            var reservasDelEspacio = db.Reservas.Where(r => r.EspacioId == id); // Todas las reservas del espacio

            // Construye el objeto de estadísticas
            var estadisticas = new
            {
                EspacioId = id,
                NombreEspacio = espacio.Nombre,
                TotalReservas = reservasDelEspacio.Count(),
                ReservasPendientes = reservasDelEspacio.Count(r => r.Estado == EstadoReserva.Pendiente),
                ReservasAprobadas = reservasDelEspacio.Count(r => r.Estado == EstadoReserva.Aprobada),
                ReservasRechazadas = reservasDelEspacio.Count(r => r.Estado == EstadoReserva.Rechazada),
                UltimaReserva = reservasDelEspacio
                    .OrderByDescending(r => r.Fecha)
                    .Select(r => new { r.Fecha, r.Horario })
                    .FirstOrDefault(), // Última reserva realizada
                FechaConsulta = DateTime.Now // Fecha de la consulta
            };

            return Ok(estadisticas); // Devuelve las estadísticas
        }

        // Métodos auxiliares

        // Valida los datos de un espacio antes de crear o actualizar
        private ValidacionResult ValidarEspacio(EspacioDto espacioDto)
        {
            if (string.IsNullOrEmpty(espacioDto.Nombre))
                return new ValidacionResult(false, "El nombre del espacio es obligatorio");

            if (espacioDto.Nombre.Length > 100)
                return new ValidacionResult(false, "El nombre no puede tener más de 100 caracteres");

            if (espacioDto.Capacidad <= 0)
                return new ValidacionResult(false, "La capacidad debe ser mayor a 0");

            if (espacioDto.Capacidad > 1000)
                return new ValidacionResult(false, "La capacidad no puede ser mayor a 1000");

            if (string.IsNullOrEmpty(espacioDto.Ubicacion))
                return new ValidacionResult(false, "La ubicación es obligatoria");

            if (espacioDto.Ubicacion.Length > 200)
                return new ValidacionResult(false, "La ubicación no puede tener más de 200 caracteres");

            if (!string.IsNullOrEmpty(espacioDto.Descripcion) && espacioDto.Descripcion.Length > 500)
                return new ValidacionResult(false, "La descripción no puede tener más de 500 caracteres");

            return new ValidacionResult(true, "Espacio válido");
        }

        // Valida el token JWT y extrae la información del usuario
        private dynamic ValidateJwtToken()
        {
            try
            {
                var authHeader = Request.Headers.Authorization; // Obtiene el header de autorización
                if (authHeader == null || authHeader.Scheme != "Bearer")
                    return null;

                var token = authHeader.Parameter; // Extrae el token JWT
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(AppConfig.JwtSecretKey); // Clave secreta para validar el token

                // Valida el token usando los parámetros de seguridad
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                // Extrae los claims del token
                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
                var userEmail = jwtToken.Claims.First(x => x.Type == "email").Value;
                var userRole = jwtToken.Claims.First(x => x.Type == "role").Value;

                // Devuelve la información relevante del usuario
                return new { Id = userId, Email = userEmail, Role = userRole };
            }
            catch
            {
                // Si ocurre un error en la validación, retorna null
                return null;
            }
        }

        // Libera los recursos del contexto de base de datos cuando el controlador se destruye
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose(); // Libera el contexto de base de datos
            }
            base.Dispose(disposing);
        }
    }
}
