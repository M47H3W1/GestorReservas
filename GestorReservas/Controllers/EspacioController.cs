using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using GestorReservas.DAL;
using GestorReservas.Models;
using GestorReservas.Models.DTOs;
using GestorReservas.Utils; // ← AGREGAR ESTA LÍNEA
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GestorReservas.Controllers
{
    public class EspacioController : ApiController
    {
        private GestorReserva db = new GestorReserva();
        // ← ELIMINAR: private readonly string secretKey = "...";

        // Constructor para validar configuración
        public EspacioController()
        {
            AppConfig.ValidateJwtConfiguration();
        }

        // GET: api/Espacio
        [HttpGet]
        public IHttpActionResult ObtenerEspacios()
        {
            var espacios = db.Espacios.ToList();
            return Ok(espacios);
        }

        // GET: api/Espacio/{id}
        [HttpGet]
        public IHttpActionResult ObtenerEspacio(int id)
        {
            var espacio = db.Espacios.Find(id);
            if (espacio == null)
                return NotFound();
            return Ok(espacio);
        }

        // POST: api/Espacio
        [HttpPost]
        public IHttpActionResult CrearEspacio(EspacioDto espacioDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // Solo admin y coordinador pueden crear espacios
            if (userInfo.Role != "Administrador" && userInfo.Role != "Coordinador")
                return Unauthorized();

            // Validar que no exista un espacio con el mismo nombre
            var existeEspacio = db.Espacios.Any(e => e.Nombre.ToLower() == espacioDto.Nombre.ToLower());
            if (existeEspacio)
                return BadRequest("Ya existe un espacio con este nombre");

            // Validar datos del espacio
            var validacion = ValidarEspacio(espacioDto);
            if (!validacion.EsValido)
                return BadRequest(validacion.Mensaje);

            var espacio = new Espacio
            {
                Nombre = espacioDto.Nombre,
                Tipo = espacioDto.Tipo,
                Capacidad = espacioDto.Capacidad,
                Ubicacion = espacioDto.Ubicacion,
                Descripcion = espacioDto.Descripcion,
                Disponible = espacioDto.Disponible.HasValue ? espacioDto.Disponible.Value : true
            };

            db.Espacios.Add(espacio);
            db.SaveChanges();

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
        [HttpPut]
        public IHttpActionResult ActualizarEspacio(int id, EspacioDto espacioDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // Solo admin y coordinador pueden actualizar espacios
            if (userInfo.Role != "Administrador" && userInfo.Role != "Coordinador")
                return Unauthorized();

            var espacio = db.Espacios.Find(id);
            if (espacio == null)
                return NotFound();

            // Validar que no exista otro espacio con el mismo nombre
            if (espacioDto.Nombre != espacio.Nombre)
            {
                var existeEspacio = db.Espacios.Any(e => e.Nombre.ToLower() == espacioDto.Nombre.ToLower() && e.Id != id);
                if (existeEspacio)
                    return BadRequest("Ya existe otro espacio con este nombre");
            }

            // Validar datos del espacio
            var validacion = ValidarEspacio(espacioDto);
            if (!validacion.EsValido)
                return BadRequest(validacion.Mensaje);

            // Actualizar propiedades
            espacio.Nombre = espacioDto.Nombre;
            espacio.Tipo = espacioDto.Tipo;
            espacio.Capacidad = espacioDto.Capacidad;
            espacio.Ubicacion = espacioDto.Ubicacion;
            espacio.Descripcion = espacioDto.Descripcion;

            if (espacioDto.Disponible.HasValue)
                espacio.Disponible = espacioDto.Disponible.Value;

            db.Entry(espacio).State = EntityState.Modified;
            db.SaveChanges();

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
        [HttpDelete]
        public IHttpActionResult EliminarEspacio(int id)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // Solo admin puede eliminar espacios
            if (userInfo.Role != "Administrador")
                return Unauthorized();

            var espacio = db.Espacios.Find(id);
            if (espacio == null)
                return NotFound();

            // Verificar si tiene reservas activas
            var tieneReservasActivas = db.Reservas.Any(r => r.EspacioId == id &&
                (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Aprobada));

            if (tieneReservasActivas)
                return BadRequest("El espacio tiene reservas activas. Cancele las reservas antes de eliminar.");

            db.Espacios.Remove(espacio);
            db.SaveChanges();

            return Ok(new
            {
                Message = "Espacio eliminado exitosamente",
                EspacioId = id
            });
        }

        // GET: api/Espacio/tipos
        [HttpGet]
        [Route("api/Espacio/tipos")]
        public IHttpActionResult ObtenerTiposEspacio()
        {
            var tipos = Enum.GetValues(typeof(TipoEspacio))
                .Cast<TipoEspacio>()
                .Select(t => new {
                    Valor = (int)t,
                    Nombre = t.ToString()
                })
                .ToList();

            return Ok(tipos);
        }

        // GET: api/Espacio/disponibles
        [HttpGet]
        [Route("api/Espacio/disponibles")]
        public IHttpActionResult ObtenerEspaciosDisponibles()
        {
            var espaciosDisponibles = db.Espacios
                .Where(e => e.Disponible)
                .ToList();

            return Ok(espaciosDisponibles);
        }

        // GET: api/Espacio/tipo/{tipo}
        [HttpGet]
        [Route("api/Espacio/tipo/{tipo}")]
        public IHttpActionResult ObtenerEspaciosPorTipo(int tipo)
        {
            if (!Enum.IsDefined(typeof(TipoEspacio), tipo))
                return BadRequest("Tipo de espacio inválido");

            var tipoEspacio = (TipoEspacio)tipo;
            var espacios = db.Espacios
                .Where(e => e.Tipo == tipoEspacio)
                .ToList();

            return Ok(espacios);
        }

        // PUT: api/Espacio/{id}/disponibilidad
        [HttpPut]
        [Route("api/Espacio/{id}/disponibilidad")]
        public IHttpActionResult CambiarDisponibilidad(int id, DisponibilidadDto disponibilidadDto)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Unauthorized();

            // Solo admin y coordinador pueden cambiar disponibilidad
            if (userInfo.Role != "Administrador" && userInfo.Role != "Coordinador")
                return Unauthorized();

            var espacio = db.Espacios.Find(id);
            if (espacio == null)
                return NotFound();

            // Si se va a deshabilitar, verificar reservas activas
            if (!disponibilidadDto.Disponible)
            {
                var tieneReservasActivas = db.Reservas.Any(r => r.EspacioId == id &&
                    (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Aprobada));

                if (tieneReservasActivas)
                    return BadRequest("El espacio tiene reservas activas. Cancele las reservas antes de deshabilitar.");
            }

            espacio.Disponible = disponibilidadDto.Disponible;
            db.Entry(espacio).State = EntityState.Modified;
            db.SaveChanges();

            var estado = disponibilidadDto.Disponible ? "habilitado" : "deshabilitado";
            return Ok(new
            {
                Message = string.Format("Espacio {0} exitosamente", estado),
                EspacioId = id,
                Disponible = espacio.Disponible
            });
        }

        // GET: api/Espacio/{id}/estadisticas
        [HttpGet]
        [Route("api/Espacio/{id}/estadisticas")]
        public IHttpActionResult ObtenerEstadisticasEspacio(int id)
        {
            var espacio = db.Espacios.Find(id);
            if (espacio == null)
                return NotFound();

            var reservasDelEspacio = db.Reservas.Where(r => r.EspacioId == id);

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
                    .FirstOrDefault(),
                FechaConsulta = DateTime.Now
            };

            return Ok(estadisticas);
        }

        // Métodos auxiliares
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