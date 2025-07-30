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
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación inválido. Debe autenticarse para crear reservas");

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
        public IHttpActionResult ActualizarReserva(int id, Reserva reservaDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != reservaDto.Id)
                return BadRequest("El ID de la URL no coincide con el ID del objeto");

            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Buscar la reserva existente
            var reservaExistente = db.Reservas.Include(r => r.Usuario).Include(r => r.Espacio).FirstOrDefault(r => r.Id == id);
            if (reservaExistente == null)
                return NotFound();

            // Validaciones de permisos según rol
            if (userInfo.Role == "Profesor")
            {
                // Profesor solo puede editar sus propias reservas
                if (reservaExistente.UsuarioId != userInfo.Id)
                    return Content(System.Net.HttpStatusCode.Unauthorized, "Los profesores solo pueden editar sus propias reservas");

                // Profesor no puede cambiar el UsuarioId
                if (reservaDto.UsuarioId != reservaExistente.UsuarioId)
                    return BadRequest("Los profesores no pueden cambiar el usuario de la reserva");

                // Profesor solo puede editar espacio y horario
                reservaDto.UsuarioId = reservaExistente.UsuarioId;
                reservaDto.Estado = reservaExistente.Estado; // Mantener estado actual
            }
            else if (userInfo.Role == "Coordinador")
            {
                // Coordinador puede editar cualquier reserva y cambiar usuario
                // Pero mantiene el estado actual a menos que sea admin
                reservaDto.Estado = reservaExistente.Estado;
            }
            else if (userInfo.Role == "Administrador")
            {
                // Administrador puede editar todo
            }
            else
            {
                return Content(System.Net.HttpStatusCode.Unauthorized, "No tiene permisos para actualizar reservas");
            }

            // Validar que el nuevo usuario existe (si se cambió)
            if (reservaDto.UsuarioId != reservaExistente.UsuarioId)
            {
                var nuevoUsuario = db.Usuarios.Find(reservaDto.UsuarioId);
                if (nuevoUsuario == null)
                    return BadRequest($"No existe un usuario con ID {reservaDto.UsuarioId}");

                // Validar que el nuevo usuario puede hacer reservas
                if (nuevoUsuario.Rol != RolUsuario.Profesor && nuevoUsuario.Rol != RolUsuario.Coordinador && nuevoUsuario.Rol != RolUsuario.Administrador)
                    return BadRequest("Solo profesores, coordinadores y administradores pueden tener reservas");
            }

            // Validar que el espacio existe (si se cambió)
            if (reservaDto.EspacioId != reservaExistente.EspacioId)
            {
                var nuevoEspacio = db.Espacios.Find(reservaDto.EspacioId);
                if (nuevoEspacio == null)
                    return BadRequest($"No existe un espacio con ID {reservaDto.EspacioId}");

                if (!nuevoEspacio.Disponible)
                    return BadRequest("El espacio seleccionado no está disponible");
            }

            // Validar formato del horario (si se cambió)
            if (reservaDto.Horario != reservaExistente.Horario)
            {
                var validacionHorario = ValidarHorario(reservaDto.Horario);
                if (!validacionHorario.EsValido)
                    return BadRequest(validacionHorario.Mensaje);
            }

            // Validar que la fecha no sea en el pasado (si se cambió)
            if (reservaDto.Fecha.Date != reservaExistente.Fecha.Date && reservaDto.Fecha.Date < DateTime.Now.Date)
                return BadRequest("No se pueden programar reservas para fechas pasadas");

            // Validar solapamiento de horarios en el espacio (si cambió espacio, fecha u horario)
            if (reservaDto.EspacioId != reservaExistente.EspacioId ||
                reservaDto.Fecha.Date != reservaExistente.Fecha.Date ||
                reservaDto.Horario != reservaExistente.Horario)
            {
                var validacionSolapamiento = ValidarSolapamientoEspacioParaActualizacion(
                    reservaDto.EspacioId, reservaDto.Fecha, reservaDto.Horario, id);
                if (!validacionSolapamiento.EsValido)
                    return BadRequest(validacionSolapamiento.Mensaje);
            }

            // Validar conflicto de usuario (si cambió usuario, fecha u horario)
            if (reservaDto.UsuarioId != reservaExistente.UsuarioId ||
                reservaDto.Fecha.Date != reservaExistente.Fecha.Date ||
                reservaDto.Horario != reservaExistente.Horario)
            {
                var validacionUsuario = ValidarConflictoUsuarioParaActualizacion(
                    reservaDto.UsuarioId, reservaDto.Fecha, reservaDto.Horario, id);
                if (!validacionUsuario.EsValido)
                    return BadRequest(validacionUsuario.Mensaje);
            }

            // Actualizar los campos
            reservaExistente.UsuarioId = reservaDto.UsuarioId;
            reservaExistente.EspacioId = reservaDto.EspacioId;
            reservaExistente.Fecha = reservaDto.Fecha;
            reservaExistente.Horario = reservaDto.Horario;
            reservaExistente.Estado = reservaDto.Estado;

            // Si no es admin y la reserva estaba aprobada, volver a pendiente si cambió algo importante
            if (userInfo.Role != "Administrador" && reservaExistente.Estado == EstadoReserva.Aprobada)
            {
                if (reservaDto.EspacioId != reservaExistente.EspacioId ||
                    reservaDto.Fecha.Date != reservaExistente.Fecha.Date ||
                    reservaDto.Horario != reservaExistente.Horario)
                {
                    reservaExistente.Estado = EstadoReserva.Pendiente;
                }
            }

            db.Entry(reservaExistente).State = EntityState.Modified;
            db.SaveChanges();

            // Recargar con relaciones
            var reservaActualizada = db.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Espacio)
                .FirstOrDefault(r => r.Id == id);

            var respuesta = new
            {
                Id = reservaActualizada.Id,
                UsuarioId = reservaActualizada.UsuarioId,
                Usuario = new
                {
                    Id = reservaActualizada.Usuario.Id,
                    Nombre = reservaActualizada.Usuario.Nombre,
                    Email = reservaActualizada.Usuario.Email,
                    Rol = reservaActualizada.Usuario.Rol.ToString()
                },
                EspacioId = reservaActualizada.EspacioId,
                Espacio = new
                {
                    Id = reservaActualizada.Espacio.Id,
                    Nombre = reservaActualizada.Espacio.Nombre,
                    Tipo = reservaActualizada.Espacio.Tipo.ToString(),
                    Ubicacion = reservaActualizada.Espacio.Ubicacion
                },
                Fecha = reservaActualizada.Fecha,
                Horario = reservaActualizada.Horario,
                Estado = reservaActualizada.Estado.ToString(),
                Message = "Reserva actualizada exitosamente"
            };

            return Ok(respuesta);
        }

        // NUEVA FUNCIÓN: Validar solapamiento excluyendo la reserva actual
        private ValidacionResult ValidarSolapamientoEspacioParaActualizacion(int espacioId, DateTime fecha, string horario, int reservaIdExcluir)
        {
            DateTime fechaInicio = fecha.Date;
            DateTime fechaFin = fechaInicio.AddDays(1);

            var reservasExistentes = db.Reservas
                .Where(r => r.EspacioId == espacioId &&
                           r.Fecha >= fechaInicio &&
                           r.Fecha < fechaFin &&
                           r.Estado != EstadoReserva.Rechazada &&
                           r.Id != reservaIdExcluir) // Excluir la reserva actual
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
                    return new ValidacionResult(false, $"El horario {horario} se solapa con una reserva existente ({reserva.Horario}) del usuario {reserva.Usuario.Nombre} en estado {reserva.Estado}");
                }
            }

            return new ValidacionResult(true, "No hay conflictos de horario");
        }

        // NUEVA FUNCIÓN: Validar conflicto de usuario excluyendo la reserva actual
        private ValidacionResult ValidarConflictoUsuarioParaActualizacion(int usuarioId, DateTime fecha, string horario, int reservaIdExcluir)
        {
            DateTime fechaInicio = fecha.Date;
            DateTime fechaFin = fechaInicio.AddDays(1);

            var reservasUsuario = db.Reservas
                .Where(r => r.UsuarioId == usuarioId &&
                           r.Fecha >= fechaInicio &&
                           r.Fecha < fechaFin &&
                           r.Estado != EstadoReserva.Rechazada &&
                           r.Id != reservaIdExcluir) // Excluir la reserva actual
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
                    return new ValidacionResult(false, $"El usuario ya tiene una reserva en el horario {reserva.Horario} para el espacio {reserva.Espacio.Nombre} en estado {reserva.Estado}. No puede tener dos reservas simultáneas.");
                }
            }

            return new ValidacionResult(true, "No hay conflictos de usuario");
        }

        // DELETE: api/Reserva/{id}
        [HttpDelete]
        public IHttpActionResult BorrarReserva(int id)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Buscar la reserva existente
            var reserva = db.Reservas.Include(r => r.Usuario).Include(r => r.Espacio).FirstOrDefault(r => r.Id == id);
            if (reserva == null)
                return NotFound();

            // Validaciones de permisos según rol
            if (userInfo.Role == "Profesor")
            {
                // Profesor solo puede eliminar sus propias reservas
                if (reserva.UsuarioId != userInfo.Id)
                    return Content(System.Net.HttpStatusCode.Unauthorized, "Los profesores solo pueden eliminar sus propias reservas");
            }
            else if (userInfo.Role == "Coordinador")
            {
                // Coordinador puede eliminar cualquier reserva
            }
            else if (userInfo.Role == "Administrador")
            {
                // Administrador puede eliminar cualquier reserva
            }
            else
            {
                return Content(System.Net.HttpStatusCode.Unauthorized, "No tiene permisos para eliminar reservas");
            }

            // Validar si la reserva se puede eliminar según su estado
            if (reserva.Estado == EstadoReserva.Aprobada)
            {
                // Solo coordinadores y administradores pueden eliminar reservas aprobadas
                if (userInfo.Role != "Coordinador" && userInfo.Role != "Administrador")
                    return BadRequest("No se pueden eliminar reservas aprobadas. Contacte al coordinador.");

                // Validar si la reserva ya pasó (opcional: evitar eliminar reservas del pasado)
                if (reserva.Fecha.Date < DateTime.Now.Date)
                {
                    if (userInfo.Role != "Administrador")
                        return BadRequest("No se pueden eliminar reservas de fechas pasadas. Solo los administradores pueden hacerlo.");
                }
            }

            // Guardar información para la respuesta
            var respuestaEliminacion = new
            {
                Id = reserva.Id,
                UsuarioId = reserva.UsuarioId,
                Usuario = new
                {
                    Id = reserva.Usuario.Id,
                    Nombre = reserva.Usuario.Nombre,
                    Email = reserva.Usuario.Email,
                    Rol = reserva.Usuario.Rol.ToString()
                },
                EspacioId = reserva.EspacioId,
                Espacio = new
                {
                    Id = reserva.Espacio.Id,
                    Nombre = reserva.Espacio.Nombre,
                    Tipo = reserva.Espacio.Tipo.ToString(),
                    Ubicacion = reserva.Espacio.Ubicacion
                },
                Fecha = reserva.Fecha,
                Horario = reserva.Horario,
                Estado = reserva.Estado.ToString(),
                EliminadoPor = new
                {
                    Id = userInfo.Id,
                    Email = userInfo.Email,
                    Rol = userInfo.Role
                },
                FechaEliminacion = DateTime.Now,
                Message = "Reserva eliminada exitosamente"
            };

            // Eliminar la reserva
            db.Reservas.Remove(reserva);
            db.SaveChanges();

            return Ok(respuestaEliminacion);
        }

        // Agregar este método al ReservaController existente
        [HttpGet]
        [Route("api/Reserva/disponibilidad/{espacioId}")]
        public IHttpActionResult ConsultarDisponibilidad(int espacioId, string fecha = null, string horario = null)
        {
            // Validar que el espacio existe
            var espacio = db.Espacios.Find(espacioId);
            if (espacio == null)
                return BadRequest($"No existe un espacio con ID {espacioId}");

            if (!espacio.Disponible)
                return BadRequest("El espacio consultado no está disponible");

            // Variables para el filtrado
            DateTime? fechaConsulta = null;
            TimeSpan horaInicioConsulta = TimeSpan.Zero;
            TimeSpan horaFinConsulta = TimeSpan.Zero;
            bool validarHorario = false;

            // Validar fecha si se proporciona
            if (!string.IsNullOrEmpty(fecha))
            {
                DateTime tempFecha;
                if (!DateTime.TryParse(fecha, out tempFecha))
                    return BadRequest("Formato de fecha inválido. Use formato YYYY-MM-DD");
                fechaConsulta = tempFecha;
            }

            // Validar horario si se proporciona
            if (!string.IsNullOrEmpty(horario))
            {
                var validacionHorario = ValidarHorario(horario);
                if (!validacionHorario.EsValido)
                    return BadRequest(validacionHorario.Mensaje);

                var partesHorario = horario.Split('-');
                horaInicioConsulta = TimeSpan.Parse(partesHorario[0]);
                horaFinConsulta = TimeSpan.Parse(partesHorario[1]);
                validarHorario = true;
            }

            // Construir query base
            var query = db.Reservas
                .Where(r => r.EspacioId == espacioId && r.Estado != EstadoReserva.Rechazada)
                .Include(r => r.Usuario)
                .Include(r => r.Espacio);

            // Aplicar filtro de fecha si se especifica
            if (fechaConsulta.HasValue)
            {
                DateTime fechaInicio = fechaConsulta.Value.Date;
                DateTime fechaFin = fechaInicio.AddDays(1);
                query = query.Where(r => r.Fecha >= fechaInicio && r.Fecha < fechaFin);
            }

            var reservasExistentes = query.ToList();

            // Si no hay filtro de horario, mostrar todas las reservas encontradas
            if (!validarHorario)
            {
                var mensaje = string.Empty;
                if (fechaConsulta.HasValue && !reservasExistentes.Any())
                {
                    mensaje = $"Espacio disponible para toda la fecha {fechaConsulta.Value.Date:yyyy-MM-dd}";
                }
                else if (fechaConsulta.HasValue && reservasExistentes.Any())
                {
                    mensaje = $"Espacio tiene {reservasExistentes.Count} reserva(s) para la fecha {fechaConsulta.Value.Date:yyyy-MM-dd}";
                }
                else if (!fechaConsulta.HasValue && reservasExistentes.Any())
                {
                    mensaje = $"Espacio tiene {reservasExistentes.Count} reserva(s) en total";
                }
                else
                {
                    mensaje = "Espacio sin reservas";
                }

                return Ok(new
                {
                    EspacioId = espacioId,
                    Espacio = new
                    {
                        Id = espacio.Id,
                        Nombre = espacio.Nombre,
                        Tipo = espacio.Tipo.ToString(),
                        Ubicacion = espacio.Ubicacion,
                        Disponible = espacio.Disponible
                    },
                    Fecha = fechaConsulta?.Date,
                    Horario = horario,
                    TotalReservasEncontradas = reservasExistentes.Count,
                    Disponible = !reservasExistentes.Any(),
                    ReservasExistentes = reservasExistentes.Select(r => new
                    {
                        Id = r.Id,
                        Usuario = new
                        {
                            Id = r.Usuario.Id,
                            Nombre = r.Usuario.Nombre,
                            Email = r.Usuario.Email
                        },
                        Fecha = r.Fecha,
                        Horario = r.Horario,
                        Estado = r.Estado.ToString()
                    }),
                    Mensaje = mensaje
                });
            }

            // Si hay filtro de horario, verificar solapamientos
            var reservasConflictivas = new List<dynamic>();
            bool disponible = true;

            foreach (var reserva in reservasExistentes)
            {
                var partesHorarioExistente = reserva.Horario.Split('-');
                var horaInicioExistente = TimeSpan.Parse(partesHorarioExistente[0]);
                var horaFinExistente = TimeSpan.Parse(partesHorarioExistente[1]);

                // Verificar solapamiento
                bool hayConflicto = !(horaFinConsulta <= horaInicioExistente || horaInicioConsulta >= horaFinExistente);

                if (hayConflicto)
                {
                    disponible = false;
                    reservasConflictivas.Add(new
                    {
                        Id = reserva.Id,
                        Usuario = new
                        {
                            Id = reserva.Usuario.Id,
                            Nombre = reserva.Usuario.Nombre,
                            Email = reserva.Usuario.Email
                        },
                        Fecha = reserva.Fecha,
                        Horario = reserva.Horario,
                        Estado = reserva.Estado.ToString(),
                        TipoConflicto = "Solapamiento de horarios"
                    });
                }
            }

            var mensajeFinal = string.Empty;
            if (fechaConsulta.HasValue)
            {
                mensajeFinal = disponible
                    ? $"Espacio disponible para el horario {horario} en la fecha {fechaConsulta.Value.Date:yyyy-MM-dd}"
                    : $"Espacio no disponible para el horario {horario} en la fecha {fechaConsulta.Value.Date:yyyy-MM-dd}. Se encontraron {reservasConflictivas.Count} conflicto(s)";
            }
            else
            {
                mensajeFinal = disponible
                    ? $"Espacio disponible para el horario {horario} en cualquier fecha"
                    : $"Espacio tiene conflictos para el horario {horario}. Se encontraron {reservasConflictivas.Count} conflicto(s)";
            }

            return Ok(new
            {
                EspacioId = espacioId,
                Espacio = new
                {
                    Id = espacio.Id,
                    Nombre = espacio.Nombre,
                    Tipo = espacio.Tipo.ToString(),
                    Ubicacion = espacio.Ubicacion,
                    Disponible = espacio.Disponible
                },
                Fecha = fechaConsulta?.Date,
                Horario = horario,
                Disponible = disponible,
                TotalReservasExistentes = reservasExistentes.Count,
                TotalReservasConflictivas = reservasConflictivas.Count,
                ReservasConflictivas = reservasConflictivas,
                ReservasNoConflictivas = reservasExistentes.Where(r =>
                {
                    var partesHorario = r.Horario.Split('-');
                    var horaInicio = TimeSpan.Parse(partesHorario[0]);
                    var horaFin = TimeSpan.Parse(partesHorario[1]);
                    bool noHayConflicto = (horaFinConsulta <= horaInicio || horaInicioConsulta >= horaFin);
                    return noHayConflicto;
                }).Select(r => new
                {
                    Id = r.Id,
                    Usuario = new
                    {
                        Id = r.Usuario.Id,
                        Nombre = r.Usuario.Nombre,
                        Email = r.Usuario.Email
                    },
                    Fecha = r.Fecha,
                    Horario = r.Horario,
                    Estado = r.Estado.ToString()
                }),
                Mensaje = mensajeFinal
            });
        }

        // Aprobar reserva
        [HttpPut]
        [Route("api/Reserva/{id}/aprobar")]
        public IHttpActionResult AprobarReserva(int id)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Solo administradores pueden aprobar reservas
            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo los administradores pueden aprobar reservas");

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
                Estado = reserva.Estado.ToString(),
                AprobadoPor = new
                {
                    Id = userInfo.Id,
                    Email = userInfo.Email,
                    Rol = userInfo.Role
                },
                FechaAprobacion = DateTime.Now
            });
        }

        // Rechazar reserva
        [HttpPut]
        [Route("api/Reserva/{id}/rechazar")]
        public IHttpActionResult RechazarReserva(int id)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Solo administradores pueden rechazar reservas
            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo los administradores pueden rechazar reservas");

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
                Estado = reserva.Estado.ToString(),
                RechazadoPor = new
                {
                    Id = userInfo.Id,
                    Email = userInfo.Email,
                    Rol = userInfo.Role
                },
                FechaRechazo = DateTime.Now
            });
        }

        // Obtener reservas pendientes para gestión
        [HttpGet]
        [Route("api/Reserva/pendientes")]
        public IHttpActionResult ObtenerReservasPendientes()
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Solo administradores pueden ver reservas pendientes
            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo los administradores pueden consultar reservas pendientes");

            var reservasPendientes = db.Reservas
                .Where(r => r.Estado == EstadoReserva.Pendiente)
                .Include(r => r.Usuario)
                .Include(r => r.Espacio)
                .OrderByDescending(r => r.Fecha)
                .ThenByDescending(r => r.Id)
                .ToList();

            return Ok(new
            {
                TotalReservasPendientes = reservasPendientes.Count,
                FechaConsulta = DateTime.Now,
                ConsultadoPor = new
                {
                    Id = userInfo.Id,
                    Email = userInfo.Email,
                    Rol = userInfo.Role
                },
                ReservasPendientes = reservasPendientes
            });
        }

        // Obtener reservas pendientes filtradas por fecha y/o horario
        [HttpGet]
        [Route("api/Reserva/pendientes/filtradas")]
        public IHttpActionResult ObtenerReservasPendientesFiltradas(string fecha = null, string horario = null, int? espacioId = null)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Solo administradores pueden ver reservas pendientes
            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo los administradores pueden consultar reservas pendientes");

            var query = db.Reservas
                .Where(r => r.Estado == EstadoReserva.Pendiente)
                .Include(r => r.Usuario)
                .Include(r => r.Espacio);

            // Filtro por fecha
            DateTime fechaConsulta;
            if (!string.IsNullOrEmpty(fecha))
            {
                if (!DateTime.TryParse(fecha, out fechaConsulta))
                    return BadRequest("Formato de fecha inválido. Use formato YYYY-MM-DD");

                DateTime fechaInicio = fechaConsulta.Date;
                DateTime fechaFin = fechaInicio.AddDays(1);
                query = query.Where(r => r.Fecha >= fechaInicio && r.Fecha < fechaFin);
            }

            // Filtro por horario (validar solapamiento)
            TimeSpan horaInicioFiltro = TimeSpan.Zero;
            TimeSpan horaFinFiltro = TimeSpan.Zero;
            bool validarHorario = false;

            if (!string.IsNullOrEmpty(horario))
            {
                var validacionHorario = ValidarHorario(horario);
                if (!validacionHorario.EsValido)
                    return BadRequest(validacionHorario.Mensaje);

                var partesHorario = horario.Split('-');
                horaInicioFiltro = TimeSpan.Parse(partesHorario[0]);
                horaFinFiltro = TimeSpan.Parse(partesHorario[1]);
                validarHorario = true;
            }

            // Filtro por espacio
            if (espacioId.HasValue)
                query = query.Where(r => r.EspacioId == espacioId.Value);

            var reservasPendientes = query
                .OrderByDescending(r => r.Fecha)
                .ThenBy(r => r.Horario)
                .ThenByDescending(r => r.Id)
                .ToList();

            // Si hay filtro de horario, aplicar filtrado por solapamiento
            if (validarHorario)
            {
                reservasPendientes = reservasPendientes.Where(reserva =>
                {
                    var partesHorarioReserva = reserva.Horario.Split('-');
                    var horaInicioReserva = TimeSpan.Parse(partesHorarioReserva[0]);
                    var horaFinReserva = TimeSpan.Parse(partesHorarioReserva[1]);

                    // Verificar solapamiento
                    bool hayConflicto = !(horaFinFiltro <= horaInicioReserva || horaInicioFiltro >= horaFinReserva);
                    return hayConflicto;
                }).ToList();
            }

            return Ok(new
            {
                TotalReservasPendientes = reservasPendientes.Count,
                FechaConsulta = DateTime.Now,
                Filtros = new
                {
                    Fecha = fecha,
                    Horario = horario,
                    EspacioId = espacioId
                },
                ConsultadoPor = new
                {
                    Id = userInfo.Id,
                    Email = userInfo.Email,
                    Rol = userInfo.Role
                },
                ReservasPendientes = reservasPendientes.Select(r => new
                {
                    Id = r.Id,
                    UsuarioId = r.UsuarioId,
                    Usuario = new
                    {
                        Id = r.Usuario.Id,
                        Nombre = r.Usuario.Nombre,
                        Email = r.Usuario.Email,
                        Rol = r.Usuario.Rol.ToString()
                    },
                    EspacioId = r.EspacioId,
                    Espacio = new
                    {
                        Id = r.Espacio.Id,
                        Nombre = r.Espacio.Nombre,
                        Tipo = r.Espacio.Tipo.ToString(),
                        Ubicacion = r.Espacio.Ubicacion
                    },
                    Fecha = r.Fecha,
                    Horario = r.Horario,
                    Estado = r.Estado.ToString()
                })
            });
        }

        // Consultar historial de reservas por usuario
        [HttpGet]
        [Route("api/Reserva/historial/usuario/{usuarioId}")]
        public IHttpActionResult ConsultarHistorialPorUsuario(int usuarioId, string fechaInicio = null, string fechaFin = null)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Solo administradores pueden consultar historial
            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo los administradores pueden consultar historiales de reservas");

            // Validar que el usuario existe
            var usuario = db.Usuarios.Find(usuarioId);
            if (usuario == null)
                return BadRequest($"No existe un usuario con ID {usuarioId}");

            var query = db.Reservas
                .Where(r => r.UsuarioId == usuarioId)
                .Include(r => r.Usuario)
                .Include(r => r.Espacio);

            // Validar y aplicar filtros de fecha
            if (!string.IsNullOrEmpty(fechaInicio))
            {
                DateTime inicio;
                if (!DateTime.TryParse(fechaInicio, out inicio))
                    return BadRequest("Formato de fecha de inicio inválido. Use formato YYYY-MM-DD");

                // Aplicar filtro si la fecha es válida
                query = query.Where(r => r.Fecha >= inicio);
            }

            if (!string.IsNullOrEmpty(fechaFin))
            {
                DateTime fin;
                if (!DateTime.TryParse(fechaFin, out fin))
                    return BadRequest("Formato de fecha de fin inválido. Use formato YYYY-MM-DD");

                // Validar que fecha inicio no sea mayor que fecha fin (solo si ambas están presentes)
                if (!string.IsNullOrEmpty(fechaInicio))
                {
                    DateTime inicio;
                    DateTime.TryParse(fechaInicio, out inicio); // Ya sabemos que es válida
                    if (inicio.Date > fin.Date)
                        return BadRequest("La fecha de inicio no puede ser mayor que la fecha de fin");
                }

                // Aplicar filtro si la fecha es válida
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
                Usuario = new
                {
                    Id = usuario.Id,
                    Nombre = usuario.Nombre,
                    Email = usuario.Email,
                    Rol = usuario.Rol.ToString()
                },
                TotalReservas = historialUsuario.Count,
                FechaConsulta = DateTime.Now,
                Filtros = new
                {
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin
                },
                ConsultadoPor = new
                {
                    Id = userInfo.Id,
                    Email = userInfo.Email,
                    Rol = userInfo.Role
                },
                Reservas = historialUsuario.Select(r => new
                {
                    Id = r.Id,
                    UsuarioId = r.UsuarioId,
                    EspacioId = r.EspacioId,
                    Espacio = new
                    {
                        Id = r.Espacio.Id,
                        Nombre = r.Espacio.Nombre,
                        Tipo = r.Espacio.Tipo.ToString(),
                        Ubicacion = r.Espacio.Ubicacion
                    },
                    Fecha = r.Fecha,
                    Horario = r.Horario,
                    Estado = r.Estado.ToString()
                })
            });
        }

        // Consultar historial de reservas por espacio
        [HttpGet]
        [Route("api/Reserva/historial/espacio/{espacioId}")]
        public IHttpActionResult ConsultarHistorialPorEspacio(int espacioId, string fechaInicio = null, string fechaFin = null)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Solo administradores pueden consultar historial
            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo los administradores pueden consultar historiales de reservas");

            // Validar que el espacio existe
            var espacio = db.Espacios.Find(espacioId);
            if (espacio == null)
                return BadRequest($"No existe un espacio con ID {espacioId}");

            var query = db.Reservas
                .Where(r => r.EspacioId == espacioId)
                .Include(r => r.Usuario)
                .Include(r => r.Espacio);

            // Validar y aplicar filtros de fecha
            if (!string.IsNullOrEmpty(fechaInicio))
            {
                DateTime inicio;
                if (!DateTime.TryParse(fechaInicio, out inicio))
                    return BadRequest("Formato de fecha de inicio inválido. Use formato YYYY-MM-DD");

                // Aplicar filtro si la fecha es válida
                query = query.Where(r => r.Fecha >= inicio);
            }

            if (!string.IsNullOrEmpty(fechaFin))
            {
                DateTime fin;
                if (!DateTime.TryParse(fechaFin, out fin))
                    return BadRequest("Formato de fecha de fin inválido. Use formato YYYY-MM-DD");

                // Validar que fecha inicio no sea mayor que fecha fin (solo si ambas están presentes)
                if (!string.IsNullOrEmpty(fechaInicio))
                {
                    DateTime inicio;
                    DateTime.TryParse(fechaInicio, out inicio); // Ya sabemos que es válida
                    if (inicio.Date > fin.Date)
                        return BadRequest("La fecha de inicio no puede ser mayor que la fecha de fin");
                }

                // Aplicar filtro si la fecha es válida
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
                Espacio = new
                {
                    Id = espacio.Id,
                    Nombre = espacio.Nombre,
                    Tipo = espacio.Tipo.ToString(),
                    Capacidad = espacio.Capacidad,
                    Ubicacion = espacio.Ubicacion,
                    Descripcion = espacio.Descripcion,
                    Disponible = espacio.Disponible
                },
                TotalReservas = historialEspacio.Count,
                FechaConsulta = DateTime.Now,
                Filtros = new
                {
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin
                },
                ConsultadoPor = new
                {
                    Id = userInfo.Id,
                    Email = userInfo.Email,
                    Rol = userInfo.Role
                },
                Reservas = historialEspacio.Select(r => new
                {
                    Id = r.Id,
                    UsuarioId = r.UsuarioId,
                    Usuario = new
                    {
                        Id = r.Usuario.Id,
                        Nombre = r.Usuario.Nombre,
                        Email = r.Usuario.Email,
                        Rol = r.Usuario.Rol.ToString()
                    },
                    EspacioId = r.EspacioId,
                    Fecha = r.Fecha,
                    Horario = r.Horario,
                    Estado = r.Estado.ToString()
                })
            });
        }

        // Consultar historial completo con filtros múltiples
        [HttpGet]
        [Route("api/Reserva/historial")]
        public IHttpActionResult ConsultarHistorialCompleto(int? usuarioId = null, int? espacioId = null,
            string fechaInicio = null, string fechaFin = null, int? estado = null)
        {
            // Validar JWT token
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Solo administradores pueden consultar historial completo
            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo los administradores pueden consultar historiales completos de reservas");

            var query = db.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Espacio)
                .AsQueryable();

            // Filtro por usuario
            if (usuarioId.HasValue)
            {
                var usuario = db.Usuarios.Find(usuarioId.Value);
                if (usuario == null)
                    return BadRequest($"No existe un usuario con ID {usuarioId.Value}");
                query = query.Where(r => r.UsuarioId == usuarioId.Value);
            }

            // Filtro por espacio
            if (espacioId.HasValue)
            {
                var espacio = db.Espacios.Find(espacioId.Value);
                if (espacio == null)
                    return BadRequest($"No existe un espacio con ID {espacioId.Value}");
                query = query.Where(r => r.EspacioId == espacioId.Value);
            }

            // Filtro por estado
            if (estado.HasValue)
            {
                if (!Enum.IsDefined(typeof(EstadoReserva), estado.Value))
                    return BadRequest($"Estado de reserva inválido: {estado.Value}. Estados válidos: 0=Pendiente, 1=Aprobada, 2=Rechazada");
                query = query.Where(r => (int)r.Estado == estado.Value);
            }

            // Validar y aplicar filtros de fecha
            if (!string.IsNullOrEmpty(fechaInicio))
            {
                DateTime inicio;
                if (!DateTime.TryParse(fechaInicio, out inicio))
                    return BadRequest("Formato de fecha de inicio inválido. Use formato YYYY-MM-DD");

                // Aplicar filtro si la fecha es válida
                query = query.Where(r => r.Fecha >= inicio);
            }

            if (!string.IsNullOrEmpty(fechaFin))
            {
                DateTime fin;
                if (!DateTime.TryParse(fechaFin, out fin))
                    return BadRequest("Formato de fecha de fin inválido. Use formato YYYY-MM-DD");

                // Validar que fecha inicio no sea mayor que fecha fin (solo si ambas están presentes)
                if (!string.IsNullOrEmpty(fechaInicio))
                {
                    DateTime inicio;
                    DateTime.TryParse(fechaInicio, out inicio); // Ya sabemos que es válida
                    if (inicio.Date > fin.Date)
                        return BadRequest("La fecha de inicio no puede ser mayor que la fecha de fin");
                }

                // Aplicar filtro si la fecha es válida
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
                    EstadoTexto = estado.HasValue ? ((EstadoReserva)estado.Value).ToString() : null,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin
                },
                ConsultadoPor = new
                {
                    Id = userInfo.Id,
                    Email = userInfo.Email,
                    Rol = userInfo.Role
                },
                Reservas = historial.Select(r => new
                {
                    Id = r.Id,
                    UsuarioId = r.UsuarioId,
                    Usuario = new
                    {
                        Id = r.Usuario.Id,
                        Nombre = r.Usuario.Nombre,
                        Email = r.Usuario.Email,
                        Rol = r.Usuario.Rol.ToString()
                    },
                    EspacioId = r.EspacioId,
                    Espacio = new
                    {
                        Id = r.Espacio.Id,
                        Nombre = r.Espacio.Nombre,
                        Tipo = r.Espacio.Tipo.ToString(),
                        Ubicacion = r.Espacio.Ubicacion
                    },
                    Fecha = r.Fecha,
                    Horario = r.Horario,
                    Estado = r.Estado.ToString()
                })
            });
        }

        // Consultar espacios disponibles para una fecha específica
        [HttpGet]
        [Route("api/Reserva/espacios-disponibles")]
        public IHttpActionResult ConsultarEspaciosDisponibles(string fecha = null, string horario = null)
        {
            // Variables para el filtrado
            DateTime? fechaConsulta = null;
            TimeSpan horaInicioConsulta = TimeSpan.Zero;
            TimeSpan horaFinConsulta = TimeSpan.Zero;
            bool validarHorario = false;

            // Validar fecha si se proporciona
            if (!string.IsNullOrEmpty(fecha))
            {
                DateTime tempFecha;
                if (!DateTime.TryParse(fecha, out tempFecha))
                    return BadRequest("Formato de fecha inválido. Use formato YYYY-MM-DD");
                fechaConsulta = tempFecha;
            }

            // Validar horario si se proporciona
            if (!string.IsNullOrEmpty(horario))
            {
                var validacionHorario = ValidarHorario(horario);
                if (!validacionHorario.EsValido)
                    return BadRequest(validacionHorario.Mensaje);

                var partesHorario = horario.Split('-');
                horaInicioConsulta = TimeSpan.Parse(partesHorario[0]);
                horaFinConsulta = TimeSpan.Parse(partesHorario[1]);
                validarHorario = true;
            }

            // Obtener todos los espacios disponibles
            var espaciosDisponibles = db.Espacios
                .Where(e => e.Disponible)
                .ToList();

            var resultadosEspacios = new List<dynamic>();

            foreach (var espacio in espaciosDisponibles)
            {
                // Construir query para reservas del espacio
                var queryReservas = db.Reservas
                    .Where(r => r.EspacioId == espacio.Id && r.Estado != EstadoReserva.Rechazada)
                    .Include(r => r.Usuario);

                // Aplicar filtro de fecha si se especifica
                if (fechaConsulta.HasValue)
                {
                    DateTime fechaInicio = fechaConsulta.Value.Date;
                    DateTime fechaFin = fechaInicio.AddDays(1);
                    queryReservas = queryReservas.Where(r => r.Fecha >= fechaInicio && r.Fecha < fechaFin);
                }

                var reservasExistentes = queryReservas.ToList();
                bool espacioDisponible = true;
                var reservasConflictivas = new List<dynamic>();

                // Si hay filtro de horario, verificar solapamientos
                if (validarHorario)
                {
                    foreach (var reserva in reservasExistentes)
                    {
                        var partesHorarioExistente = reserva.Horario.Split('-');
                        var horaInicioExistente = TimeSpan.Parse(partesHorarioExistente[0]);
                        var horaFinExistente = TimeSpan.Parse(partesHorarioExistente[1]);

                        // Verificar solapamiento
                        bool hayConflicto = !(horaFinConsulta <= horaInicioExistente || horaInicioConsulta >= horaFinExistente);

                        if (hayConflicto)
                        {
                            espacioDisponible = false;
                            reservasConflictivas.Add(new
                            {
                                Id = reserva.Id,
                                Usuario = new
                                {
                                    Id = reserva.Usuario.Id,
                                    Nombre = reserva.Usuario.Nombre,
                                    Email = reserva.Usuario.Email
                                },
                                Fecha = reserva.Fecha,
                                Horario = reserva.Horario,
                                Estado = reserva.Estado.ToString(),
                                TipoConflicto = "Solapamiento de horarios"
                            });
                        }
                    }
                }
                else
                {
                    // Si no hay filtro de horario, el espacio está disponible si no tiene reservas en la fecha
                    espacioDisponible = !reservasExistentes.Any();
                }

                // Determinar el mensaje para este espacio
                string mensajeEspacio = "";
                if (fechaConsulta.HasValue && !string.IsNullOrEmpty(horario))
                {
                    mensajeEspacio = espacioDisponible
                        ? $"Disponible para {horario} el {fechaConsulta.Value.Date:yyyy-MM-dd}"
                        : $"No disponible para {horario} el {fechaConsulta.Value.Date:yyyy-MM-dd} - {reservasConflictivas.Count} conflicto(s)";
                }
                else if (fechaConsulta.HasValue)
                {
                    mensajeEspacio = espacioDisponible
                        ? $"Disponible el {fechaConsulta.Value.Date:yyyy-MM-dd}"
                        : $"Tiene {reservasExistentes.Count} reserva(s) el {fechaConsulta.Value.Date:yyyy-MM-dd}";
                }
                else if (!string.IsNullOrEmpty(horario))
                {
                    mensajeEspacio = espacioDisponible
                        ? $"Disponible para {horario}"
                        : $"No disponible para {horario} - {reservasConflictivas.Count} conflicto(s)";
                }
                else
                {
                    mensajeEspacio = espacioDisponible
                        ? "Disponible"
                        : $"Tiene {reservasExistentes.Count} reserva(s)";
                }

                resultadosEspacios.Add(new
                {
                    EspacioId = espacio.Id,
                    Espacio = new
                    {
                        Id = espacio.Id,
                        Nombre = espacio.Nombre,
                        Tipo = espacio.Tipo.ToString(),
                        Capacidad = espacio.Capacidad,
                        Ubicacion = espacio.Ubicacion,
                        Descripcion = espacio.Descripcion,
                        Disponible = espacio.Disponible
                    },
                    DisponibleParaConsulta = espacioDisponible,
                    TotalReservasExistentes = reservasExistentes.Count,
                    TotalReservasConflictivas = reservasConflictivas.Count,
                    Mensaje = mensajeEspacio,
                    ReservasConflictivas = validarHorario ? reservasConflictivas : null,
                    ReservasExistentes = !validarHorario ? reservasExistentes.Select(r => new
                    {
                        Id = r.Id,
                        Usuario = new
                        {
                            Id = r.Usuario.Id,
                            Nombre = r.Usuario.Nombre,
                            Email = r.Usuario.Email
                        },
                        Fecha = r.Fecha,
                        Horario = r.Horario,
                        Estado = r.Estado.ToString()
                    }) : null
                });
            }

            // Separar espacios disponibles y no disponibles
            var espaciosLibres = resultadosEspacios.Where(e => (bool)e.DisponibleParaConsulta).ToList();
            var espaciosOcupados = resultadosEspacios.Where(e => !(bool)e.DisponibleParaConsulta).ToList();

            string mensajeGeneral = "";
            if (fechaConsulta.HasValue && !string.IsNullOrEmpty(horario))
            {
                mensajeGeneral = $"Consulta de disponibilidad para {horario} el {fechaConsulta.Value.Date:yyyy-MM-dd}";
            }
            else if (fechaConsulta.HasValue)
            {
                mensajeGeneral = $"Consulta de disponibilidad para {fechaConsulta.Value.Date:yyyy-MM-dd}";
            }
            else if (!string.IsNullOrEmpty(horario))
            {
                mensajeGeneral = $"Consulta de disponibilidad para {horario}";
            }
            else
            {
                mensajeGeneral = "Consulta general de disponibilidad de espacios";
            }

            return Ok(new
            {
                FechaConsulta = DateTime.Now,
                Parametros = new
                {
                    Fecha = fecha,
                    Horario = horario
                },
                TotalEspaciosDisponibles = espaciosDisponibles.Count,
                EspaciosLibres = espaciosLibres.Count,
                EspaciosOcupados = espaciosOcupados.Count,
                Mensaje = mensajeGeneral,
                Resultados = new
                {
                    EspaciosDisponibles = espaciosLibres,
                    EspaciosNoDisponibles = espaciosOcupados
                }
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
