using System;
using System.Web.Http;
using GestorReservas.DAL;
using GestorReservas.Models;
using System.Linq;
using System.Data.Entity;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Collections.Generic;
using GestorReservas.Utils; // ← AGREGAR ESTA LÍNEA

namespace GestorReservas.Controllers
{
    public class ReservaController : ApiController
    {
        private GestorReserva db = new GestorReserva();
        // ← ELIMINAR: private readonly string secretKey = "...";

        // Constructor para validar configuración
        public ReservaController()
        {
            AppConfig.ValidateJwtConfiguration();
        }

        // GET: api/Reserva
        [HttpGet]
        public IEnumerable<Reserva> ObtenerReservas()
        {
            return db.Reservas.Include(r => r.Usuario).Include(r => r.Espacio).ToList();
        }

        // GET: api/Reserva/{id}
        [HttpGet]
        public IHttpActionResult ObtenerReserva(int id)
        {
            var reserva = db.Reservas.Include(r => r.Usuario).Include(r => r.Espacio).FirstOrDefault(r => r.Id == id);
            if (reserva == null)
                return NotFound();
            return Ok(reserva);
        }

        // POST: api/Reserva
        [HttpPost]
        public IHttpActionResult CrearReserva(Reserva reserva)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return BadRequest("Token de autenticación inválido. Debe autenticarse para crear reservas");

            // Validar que el usuario existe
            var usuario = db.Usuarios.Find(reserva.UsuarioId);
            if (usuario == null)
                return BadRequest(string.Format("No existe un usuario con ID {0}", reserva.UsuarioId));

            // Validar que el usuario del token coincide con el de la reserva
            if (userInfo.Id != reserva.UsuarioId)
                return BadRequest("No puede crear reservas para otros usuarios");

            // Validar roles
            if (usuario.Rol != RolUsuario.Profesor && usuario.Rol != RolUsuario.Coordinador && usuario.Rol != RolUsuario.Administrador)
                return BadRequest("Solo profesores, coordinadores y administradores pueden crear reservas");

            // Validar que el espacio existe
            var espacio = db.Espacios.Find(reserva.EspacioId);
            if (espacio == null)
                return BadRequest(string.Format("No existe un espacio con ID {0}", reserva.EspacioId));

            // NUEVA VALIDACIÓN: Validar formato y lógica del horario
            var validacionHorario = ValidarHorario(reserva.Horario);
            if (!validacionHorario.EsValido)
                return BadRequest(validacionHorario.Mensaje);

            // NUEVA VALIDACIÓN: Validar que la fecha no sea en el pasado
            if (reserva.Fecha.Date < DateTime.Now.Date)
                return BadRequest("No se pueden hacer reservas para fechas pasadas");

            // NUEVA VALIDACIÓN: Validar solapamiento de horarios para el mismo espacio
            var validacionSolapamiento = ValidarSolapamientoEspacio(reserva.EspacioId, reserva.Fecha, reserva.Horario);
            if (!validacionSolapamiento.EsValido)
                return BadRequest(validacionSolapamiento.Mensaje);

            // NUEVA VALIDACIÓN: Validar que el usuario no tenga otra reserva en el mismo horario
            var validacionUsuario = ValidarConflictoUsuario(reserva.UsuarioId, reserva.Fecha, reserva.Horario);
            if (!validacionUsuario.EsValido)
                return BadRequest(validacionUsuario.Mensaje);

            // Crear reserva
            reserva.Estado = EstadoReserva.Pendiente;
            db.Reservas.Add(reserva);
            db.SaveChanges();

            // Recargar la reserva con las relaciones incluidas
            var reservaCreada = db.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Espacio)
                .FirstOrDefault(r => r.Id == reserva.Id);

            // Crear respuesta sin datos sensibles
            var respuesta = new
            {
                Id = reservaCreada.Id,
                UsuarioId = reservaCreada.UsuarioId,
                Usuario = new
                {
                    Id = reservaCreada.Usuario.Id,
                    Nombre = reservaCreada.Usuario.Nombre,
                    Email = reservaCreada.Usuario.Email,
                    Rol = reservaCreada.Usuario.Rol.ToString()
                },
                EspacioId = reservaCreada.EspacioId,
                Espacio = new
                {
                    Id = reservaCreada.Espacio.Id,
                    Nombre = reservaCreada.Espacio.Nombre,
                    Tipo = reservaCreada.Espacio.Tipo.ToString(),
                    Ubicacion = reservaCreada.Espacio.Ubicacion
                },
                Fecha = reservaCreada.Fecha,
                Horario = reservaCreada.Horario,
                Estado = reservaCreada.Estado.ToString(),
                Message = "Reserva creada exitosamente"
            };

            return CreatedAtRoute("DefaultApi", new { id = reserva.Id }, respuesta);
        }

        // NUEVA FUNCIÓN: Validar formato y lógica del horario
        private ValidacionResult ValidarHorario(string horario)
        {
            if (string.IsNullOrEmpty(horario))
                return new ValidacionResult(false, "El horario es obligatorio");

            // Validar formato HH:mm-HH:mm
            var partes = horario.Split('-');
            if (partes.Length != 2)
                return new ValidacionResult(false, "El horario debe tener el formato HH:mm-HH:mm (ejemplo: 09:00-10:00)");

            TimeSpan horaInicio;
            TimeSpan horaFin;

            if (!TimeSpan.TryParse(partes[0], out horaInicio))
                return new ValidacionResult(false, string.Format("Hora de inicio inválida: {0}. Use formato HH:mm", partes[0]));

            if (!TimeSpan.TryParse(partes[1], out horaFin))
                return new ValidacionResult(false, string.Format("Hora de fin inválida: {0}. Use formato HH:mm", partes[1]));

            // Validar que hora inicio sea menor que hora fin
            if (horaInicio >= horaFin)
                return new ValidacionResult(false, "La hora de inicio debe ser menor que la hora de fin");

            // Validar que la duración sea al menos 30 minutos
            var duracion = horaFin - horaInicio;
            if (duracion.TotalMinutes < 30)
                return new ValidacionResult(false, "La reserva debe tener una duración mínima de 30 minutos");

            // Validar horarios laborales (6:00 AM - 10:00 PM)
            if (horaInicio < TimeSpan.FromHours(6) || horaFin > TimeSpan.FromHours(22))
                return new ValidacionResult(false, "Las reservas solo se permiten entre 06:00 y 22:00");

            return new ValidacionResult(true, "Horario válido");
        }

        // NUEVA FUNCIÓN: Validar solapamiento de horarios en el mismo espacio
        private ValidacionResult ValidarSolapamientoEspacio(int espacioId, DateTime fecha, string horario)
        {
            DateTime fechaInicio = fecha.Date;
            DateTime fechaFin = fechaInicio.AddDays(1);

            var reservasExistentes = db.Reservas
                .Where(r => r.EspacioId == espacioId &&
                           r.Fecha >= fechaInicio &&
                           r.Fecha < fechaFin &&
                           r.Estado != EstadoReserva.Rechazada)
                .Include(r => r.Usuario)
                .ToList();

            var partesHorarioNuevo = horario.Split('-');
            var horaInicioNueva = TimeSpan.Parse(partesHorarioNuevo[0]);
            var horaFinNueva = TimeSpan.Parse(partesHorarioNuevo[1]);

            foreach (var reserva in reservasExistentes)
            {
                var partesHorarioExistente = reserva.Horario.Split('-');
                var horaInicioExistente = TimeSpan.Parse(partesHorarioExistente[0]);
                var horaFinExistente = TimeSpan.Parse(partesHorarioExistente[1]);

                // Verificar solapamiento
                bool hayConflicto = !(horaFinNueva <= horaInicioExistente || horaInicioNueva >= horaFinExistente);

                if (hayConflicto)
                {
                    return new ValidacionResult(false, string.Format("El horario {0} se solapa con una reserva existente ({1}) del usuario {2} en estado {3}",
                        horario, reserva.Horario, reserva.Usuario.Nombre, reserva.Estado));
                }
            }

            return new ValidacionResult(true, "No hay conflictos de horario");
        }

        // NUEVA FUNCIÓN: Validar que el usuario no tenga otra reserva en el mismo horario
        private ValidacionResult ValidarConflictoUsuario(int usuarioId, DateTime fecha, string horario)
        {
            DateTime fechaInicio = fecha.Date;
            DateTime fechaFin = fechaInicio.AddDays(1);

            var reservasUsuario = db.Reservas
                .Where(r => r.UsuarioId == usuarioId &&
                           r.Fecha >= fechaInicio &&
                           r.Fecha < fechaFin &&
                           r.Estado != EstadoReserva.Rechazada)
                .Include(r => r.Espacio)
                .ToList();

            var partesHorarioNuevo = horario.Split('-');
            var horaInicioNueva = TimeSpan.Parse(partesHorarioNuevo[0]);
            var horaFinNueva = TimeSpan.Parse(partesHorarioNuevo[1]);

            foreach (var reserva in reservasUsuario)
            {
                var partesHorarioExistente = reserva.Horario.Split('-');
                var horaInicioExistente = TimeSpan.Parse(partesHorarioExistente[0]);
                var horaFinExistente = TimeSpan.Parse(partesHorarioExistente[1]);

                // Verificar solapamiento
                bool hayConflicto = !(horaFinNueva <= horaInicioExistente || horaInicioNueva >= horaFinExistente);

                if (hayConflicto)
                {
                    return new ValidacionResult(false, string.Format("Ya tiene una reserva en el horario {0} para el espacio {1} en estado {2}. No puede tener dos reservas simultáneas.",
                        reserva.Horario, reserva.Espacio.Nombre, reserva.Estado));
                }
            }

            return new ValidacionResult(true, "No hay conflictos de usuario");
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

        // PUT: api/Reserva/{id}
        [HttpPut]
        public IHttpActionResult ActualizarReserva(int id, Reserva reserva)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (id != reserva.Id)
                return BadRequest();
            db.Entry(reserva).State = EntityState.Modified;
            db.SaveChanges();
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        // DELETE: api/Reserva/{id}
        [HttpDelete]
        public IHttpActionResult BorrarReserva(int id)
        {
            var reserva = db.Reservas.Find(id);
            if (reserva == null)
                return NotFound();
            db.Reservas.Remove(reserva);
            db.SaveChanges();
            return Ok(reserva);
        }

        // Agregar este método al ReservaController existente
        [HttpGet]
        [Route("api/Reserva/disponibilidad/{espacioId}")]
        public IHttpActionResult ConsultarDisponibilidad(int espacioId, string fecha, string horario)
        {
            // Convertir fecha string a DateTime
            DateTime fechaConsulta;
            if (!DateTime.TryParse(fecha, out fechaConsulta))
                return BadRequest("Formato de fecha inválido");

            // Buscar reservas que coincidan con espacio, fecha y horario
            var reservasExistentes = db.Reservas
                .Where(r => r.EspacioId == espacioId &&
                           r.Fecha.Date == fechaConsulta.Date &&
                           r.Horario == horario &&
                           r.Estado != EstadoReserva.Rechazada)
                .Include(r => r.Usuario)
                .Include(r => r.Espacio)
                .ToList();

            var disponible = !reservasExistentes.Any();

            return Ok(new
            {
                EspacioId = espacioId,
                Fecha = fechaConsulta.Date,
                Horario = horario,
                Disponible = disponible,
                ReservasExistentes = reservasExistentes
            });
        }

        [HttpGet]
        [Route("api/Reserva/espacios-disponibles")]
        public IHttpActionResult ConsultarEspaciosDisponibles(string fecha, string horario)
        {
            DateTime fechaConsulta;
            if (!DateTime.TryParse(fecha, out fechaConsulta))
                return BadRequest("Formato de fecha inválido");

            // Espacios ocupados en esa fecha/horario
            var espaciosOcupados = db.Reservas
                .Where(r => r.Fecha.Date == fechaConsulta.Date &&
                           r.Horario == horario &&
                           r.Estado != EstadoReserva.Rechazada)
                .Select(r => r.EspacioId)
                .Distinct()
                .ToList();

            // Espacios disponibles
            var espaciosDisponibles = db.Espacios
                .Where(e => !espaciosOcupados.Contains(e.Id))
                .ToList();

            return Ok(espaciosDisponibles);
        }

        // Aprobar reserva
        [HttpPut]
        [Route("api/Reserva/{id}/aprobar")]
        public IHttpActionResult AprobarReserva(int id)
        {
            var reserva = db.Reservas.Find(id);
            if (reserva == null)
                return NotFound();

            if (reserva.Estado != EstadoReserva.Pendiente)
                return BadRequest("Solo se pueden aprobar reservas pendientes");

            reserva.Estado = EstadoReserva.Aprobada;
            db.SaveChanges();

            return Ok(new
            {
                Message = "Reserva aprobada exitosamente",
                ReservaId = id,
                Estado = reserva.Estado
            });
        }

        // Rechazar reserva
        [HttpPut]
        [Route("api/Reserva/{id}/rechazar")]
        public IHttpActionResult RechazarReserva(int id)
        {
            var reserva = db.Reservas.Find(id);
            if (reserva == null)
                return NotFound();

            if (reserva.Estado != EstadoReserva.Pendiente)
                return BadRequest("Solo se pueden rechazar reservas pendientes");

            reserva.Estado = EstadoReserva.Rechazada;
            db.SaveChanges();

            return Ok(new
            {
                Message = "Reserva rechazada exitosamente",
                ReservaId = id,
                Estado = reserva.Estado
            });
        }

        // Obtener reservas pendientes para gestión
        [HttpGet]
        [Route("api/Reserva/pendientes")]
        public IHttpActionResult ObtenerReservasPendientes()
        {
            var reservasPendientes = db.Reservas
                .Where(r => r.Estado == EstadoReserva.Pendiente)
                .Include(r => r.Usuario)
                .Include(r => r.Espacio)
                .ToList();

            return Ok(reservasPendientes);
        }

        // Consultar historial de reservas por usuario
        [HttpGet]
        [Route("api/Reserva/historial/usuario/{usuarioId}")]
        public IHttpActionResult ConsultarHistorialPorUsuario(int usuarioId, string fechaInicio = null, string fechaFin = null)
        {
            var query = db.Reservas
                .Where(r => r.UsuarioId == usuarioId)
                .Include(r => r.Usuario)
                .Include(r => r.Espacio);

            // Filtros opcionales de fecha
            DateTime inicio;
            if (!string.IsNullOrEmpty(fechaInicio) && DateTime.TryParse(fechaInicio, out inicio))
            {
                query = query.Where(r => r.Fecha >= inicio);
            }

            DateTime fin;
            if (!string.IsNullOrEmpty(fechaFin) && DateTime.TryParse(fechaFin, out fin))
            {
                DateTime finDelDia = fin.Date.AddDays(1);
                query = query.Where(r => r.Fecha < finDelDia);
            }

            var historialUsuario = query
                .OrderByDescending(r => r.Fecha)
                .ThenByDescending(r => r.Id)
                .ToList();

            return Ok(new
            {
                UsuarioId = usuarioId,
                TotalReservas = historialUsuario.Count,
                FechaConsulta = DateTime.Now,
                Reservas = historialUsuario
            });
        }

        // Consultar historial de reservas por espacio
        [HttpGet]
        [Route("api/Reserva/historial/espacio/{espacioId}")]
        public IHttpActionResult ConsultarHistorialPorEspacio(int espacioId, string fechaInicio = null, string fechaFin = null)
        {
            var query = db.Reservas
                .Where(r => r.EspacioId == espacioId)
                .Include(r => r.Usuario)
                .Include(r => r.Espacio);

            // Filtros opcionales de fecha
            DateTime inicio;
            if (!string.IsNullOrEmpty(fechaInicio) && DateTime.TryParse(fechaInicio, out inicio))
            {
                query = query.Where(r => r.Fecha >= inicio);
            }

            DateTime fin;
            if (!string.IsNullOrEmpty(fechaFin) && DateTime.TryParse(fechaFin, out fin))
            {
                DateTime finDelDia = fin.Date.AddDays(1);
                query = query.Where(r => r.Fecha < finDelDia);
            }

            var historialEspacio = query
                .OrderByDescending(r => r.Fecha)
                .ThenByDescending(r => r.Id)
                .ToList();

            return Ok(new
            {
                EspacioId = espacioId,
                TotalReservas = historialEspacio.Count,
                FechaConsulta = DateTime.Now,
                Reservas = historialEspacio
            });
        }

        // Consultar historial completo con filtros múltiples
        [HttpGet]
        [Route("api/Reserva/historial")]
        public IHttpActionResult ConsultarHistorialCompleto(int? usuarioId = null, int? espacioId = null,
            string fechaInicio = null, string fechaFin = null, int? estado = null)
        {
            var query = db.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Espacio)
                .AsQueryable();

            // Filtro por usuario
            if (usuarioId.HasValue)
                query = query.Where(r => r.UsuarioId == usuarioId.Value);

            // Filtro por espacio
            if (espacioId.HasValue)
                query = query.Where(r => r.EspacioId == espacioId.Value);

            // Filtro por estado
            if (estado.HasValue)
                query = query.Where(r => (int)r.Estado == estado.Value);

            // Filtros de fecha
            DateTime inicio;
            if (!string.IsNullOrEmpty(fechaInicio) && DateTime.TryParse(fechaInicio, out inicio))
            {
                query = query.Where(r => r.Fecha >= inicio);
            }

            DateTime fin;
            if (!string.IsNullOrEmpty(fechaFin) && DateTime.TryParse(fechaFin, out fin))
            {
                DateTime finDelDia = fin.Date.AddDays(1);
                query = query.Where(r => r.Fecha < finDelDia);
            }

            var historial = query
                .OrderByDescending(r => r.Fecha)
                .ThenByDescending(r => r.Id)
                .ToList();

            return Ok(new
            {
                TotalReservas = historial.Count,
                FechaConsulta = DateTime.Now,
                Filtros = new
                {
                    UsuarioId = usuarioId,
                    EspacioId = espacioId,
                    Estado = estado,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin
                },
                Reservas = historial
            });
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
