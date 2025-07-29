using System.Web.Http;
using System.Data.Entity;
using GestorReservas.DAL;
using GestorReservas.Models;
using System.Collections.Generic;
using System.Linq;
using System;

namespace GestorReservas.Controllers
{
    public class ReservaController : ApiController
    {
        private GestorReserva db = new GestorReserva();
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

            // Validar que el usuario existe y es profesor
            var usuario = db.Usuarios.Find(reserva.UsuarioId);
            if (usuario == null)
                return BadRequest("Usuario no encontrado");

            if (usuario.Rol != RolUsuario.Profesor && usuario.Rol != RolUsuario.Coordinador)
                return BadRequest("Solo profesores y coordinadores pueden crear reservas");

            // Validar disponibilidad antes de crear
            var conflictos = db.Reservas
                .Where(r => r.EspacioId == reserva.EspacioId &&
                           r.Fecha.Date == reserva.Fecha.Date &&
                           r.Horario == reserva.Horario &&
                           r.Estado != EstadoReserva.Rechazada)
                .Any();

            if (conflictos)
                return BadRequest("El espacio no está disponible en ese horario");

            // Establecer estado inicial
            reserva.Estado = EstadoReserva.Pendiente;

            db.Reservas.Add(reserva);
            db.SaveChanges();
            return CreatedAtRoute("DefaultApi", new { id = reserva.Id }, reserva);
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

        [HttpGet]
        [Route("api/Reserva/disponibilidad/{espacioId}")]
        public IHttpActionResult ConsultarDisponibilidad(int espacioId, string fecha, string horario)
        {
            // Convertir fecha string a DateTime
            if (!DateTime.TryParse(fecha, out DateTime fechaConsulta))
                return BadRequest("Formato de fecha inválido");

            // Crear el rango de fechas para el día completo
            DateTime fechaInicio = fechaConsulta.Date;
            DateTime fechaFin = fechaInicio.AddDays(1);

            // Buscar reservas que coincidan con espacio, fecha y horario
            var reservasExistentes = db.Reservas
                .Where(r => r.EspacioId == espacioId &&
                           r.Fecha >= fechaInicio &&
                           r.Fecha < fechaFin &&
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
            if (!DateTime.TryParse(fecha, out DateTime fechaConsulta))
                return BadRequest("Formato de fecha inválido");

            // Crear el rango de fechas para el día completo
            DateTime fechaInicio = fechaConsulta.Date;
            DateTime fechaFin = fechaInicio.AddDays(1);

            // Espacios ocupados en esa fecha/horario
            var espaciosOcupados = db.Reservas
                .Where(r => r.Fecha >= fechaInicio &&
                           r.Fecha < fechaFin &&
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
            if (!string.IsNullOrEmpty(fechaInicio) && DateTime.TryParse(fechaInicio, out DateTime inicio))
            {
                query = query.Where(r => r.Fecha >= inicio);
            }

            if (!string.IsNullOrEmpty(fechaFin) && DateTime.TryParse(fechaFin, out DateTime fin))
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
            if (!string.IsNullOrEmpty(fechaInicio) && DateTime.TryParse(fechaInicio, out DateTime inicio))
            {
                query = query.Where(r => r.Fecha >= inicio);
            }

            if (!string.IsNullOrEmpty(fechaFin) && DateTime.TryParse(fechaFin, out DateTime fin))
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
            if (!string.IsNullOrEmpty(fechaInicio) && DateTime.TryParse(fechaInicio, out DateTime inicio))
            {
                query = query.Where(r => r.Fecha >= inicio);
            }

            if (!string.IsNullOrEmpty(fechaFin) && DateTime.TryParse(fechaFin, out DateTime fin))
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
    }
}
