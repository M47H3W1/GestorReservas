using System;
using System.Web.Http;
using GestorReservas.DAL;
using GestorReservas.Models;
using System.Linq;
using System.Data.Entity;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using GestorReservas.Utils;

namespace GestorReservas.Controllers
{
    /// <summary>
    /// Controlador para gestión de reservas de espacios
    /// Maneja CRUD de reservas con validaciones de permisos por rol
    /// y validaciones de disponibilidad de espacios
    /// </summary>
    public class ReservaController : ApiController
    {
        // Contexto de base de datos para Entity Framework
        private GestorReserva db = new GestorReserva();

        /// <summary>
        /// Constructor que valida la configuración JWT al inicializar
        /// </summary>
        public ReservaController()
        {
            AppConfig.ValidateJwtConfiguration();
        }

        #region GET - Obtener Reservas

        /// <summary>
        /// GET: api/Reserva
        /// Obtiene todas las reservas filtradas por rol del usuario:
        /// - Profesores: Solo sus propias reservas
        /// - Coordinadores: Reservas de su departamento
        /// - Administradores: Todas las reservas
        /// </summary>
        [HttpGet]
        public IHttpActionResult ObtenerReservas()
        {
            // Validar token JWT y obtener información del usuario
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Query base que incluye Usuario, Departamento y Espacio relacionados
            var query = db.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Usuario.Departamento) // AGREGAR: Incluir departamento del usuario
                .Include(r => r.Espacio)
                .AsQueryable();

            // Aplicar filtros según el rol del usuario autenticado
            if (userInfo.Role == "Profesor")
            {
                // Profesores solo ven sus propias reservas
                query = query.Where(r => r.UsuarioId == userInfo.Id);
            }
            else if (userInfo.Role == "Coordinador")
            {
                // Coordinadores ven reservas de profesores de su departamento
                var coordinador = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == userInfo.Id);
                if (coordinador?.Departamento == null)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Coordinador no tiene departamento asignado");

                query = query.Where(r => r.Usuario.DepartamentoId == coordinador.DepartamentoId);
            }
            // Administradores ven todas las reservas (no se filtra el query)

            // Ejecutar query ordenado por fecha descendente
            var reservas = query
                .OrderByDescending(r => r.Fecha)
                .ThenByDescending(r => r.Id)
                .ToList();

            // Crear respuesta sin referencias circulares (evita problemas de serialización JSON)
            var resultado = reservas.Select(r => new
            {
                Id = r.Id,
                UsuarioId = r.UsuarioId,
                Usuario = new
                {
                    Id = r.Usuario.Id,
                    Nombre = r.Usuario.Nombre,
                    Email = r.Usuario.Email,
                    Rol = r.Usuario.Rol.ToString(),
                    // AGREGAR: Información del departamento del usuario
                    Departamento = r.Usuario.Departamento != null ? new
                    {
                        Id = r.Usuario.Departamento.Id,
                        Nombre = r.Usuario.Departamento.Nombre,
                        Codigo = r.Usuario.Departamento.Codigo,
                        Tipo = r.Usuario.Departamento.Tipo.ToString()
                    } : null
                },
                EspacioId = r.EspacioId,
                Espacio = new
                {
                    Id = r.Espacio.Id,
                    Nombre = r.Espacio.Nombre,
                    Tipo = r.Espacio.Tipo.ToString(),
                    Capacidad = r.Espacio.Capacidad,
                    Ubicacion = r.Espacio.Ubicacion,
                    Descripcion = r.Espacio.Descripcion,
                    Disponible = r.Espacio.Disponible
                },
                Fecha = r.Fecha,
                Horario = r.Horario,
                Estado = r.Estado.ToString(),
                Descripcion = r.Descripcion
            }).ToList();

            // Respuesta con metadata de consulta
            return Ok(new
            {
                TotalReservas = resultado.Count,
                FechaConsulta = DateTime.Now,
                ConsultadoPor = new
                {
                    Id = userInfo.Id,
                    Email = userInfo.Email,
                    Rol = userInfo.Role
                },
                Reservas = resultado
            });
        }

        /// <summary>
        /// GET: api/Reserva/{id}
        /// Obtiene una reserva específica por ID
        /// Aplica validaciones de permisos según el rol
        /// </summary>
        [HttpGet]
        public IHttpActionResult ObtenerReserva(int id)
        {
            // Validar token JWT
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Buscar la reserva incluyendo entidades relacionadas
            var reserva = db.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Usuario.Departamento) // AGREGAR: Incluir departamento del usuario
                .Include(r => r.Espacio)
                .FirstOrDefault(r => r.Id == id);

            if (reserva == null)
                return NotFound();

            // Validar permisos según rol del usuario autenticado
            if (userInfo.Role == "Profesor")
            {
                // Profesores solo pueden ver sus propias reservas
                if (reserva.UsuarioId != userInfo.Id)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Solo puede consultar sus propias reservas");
            }
            else if (userInfo.Role == "Coordinador")
            {
                // Coordinadores solo pueden ver reservas de su departamento
                var coordinador = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == userInfo.Id);
                if (coordinador?.Departamento == null)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Coordinador no tiene departamento asignado");

                var usuarioReserva = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == reserva.UsuarioId);
                if (usuarioReserva?.DepartamentoId != coordinador.DepartamentoId)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Solo puede consultar reservas de profesores de su departamento");
            }
            // Administradores pueden ver cualquier reserva

            // Crear respuesta sin referencias circulares
            var resultado = new
            {
                Id = reserva.Id,
                UsuarioId = reserva.UsuarioId,
                Usuario = new
                {
                    Id = reserva.Usuario.Id,
                    Nombre = reserva.Usuario.Nombre,
                    Email = reserva.Usuario.Email,
                    Rol = reserva.Usuario.Rol.ToString(),
                    // AGREGAR: Información del departamento del usuario
                    Departamento = reserva.Usuario.Departamento != null ? new
                    {
                        Id = reserva.Usuario.Departamento.Id,
                        Nombre = reserva.Usuario.Departamento.Nombre,
                        Codigo = reserva.Usuario.Departamento.Codigo,
                        Tipo = reserva.Usuario.Departamento.Tipo.ToString()
                    } : null
                },
                EspacioId = reserva.EspacioId,
                Espacio = new
                {
                    Id = reserva.Espacio.Id,
                    Nombre = reserva.Espacio.Nombre,
                    Tipo = reserva.Espacio.Tipo.ToString(),
                    Capacidad = reserva.Espacio.Capacidad,
                    Ubicacion = reserva.Espacio.Ubicacion,
                    Descripcion = reserva.Espacio.Descripcion,
                    Disponible = reserva.Espacio.Disponible
                },
                Fecha = reserva.Fecha,
                Horario = reserva.Horario,
                Estado = reserva.Estado.ToString(),
                Descripcion = reserva.Descripcion
            };

            return Ok(resultado);
        }

        #endregion

        #region POST - Crear Reserva

        /// <summary>
        /// POST: api/Reserva
        /// Crea una nueva reserva con múltiples validaciones:
        /// - Validación de permisos por rol
        /// - Coordinadores pueden crear reservas para usuarios de su departamento
        /// - Validación de horario y disponibilidad del espacio
        /// - Validación de conflictos de usuario
        /// </summary>
        [HttpPost]
        public IHttpActionResult CrearReserva(Reserva reserva)
        {
            // Validar modelo recibido
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar token JWT
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación inválido. Debe autenticarse para crear reservas");

            // Obtener información del usuario autenticado
            var usuarioAutenticado = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == userInfo.Id);
            if (usuarioAutenticado == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Usuario del token no encontrado en el sistema");

            // Validar que el usuario para quien se hace la reserva existe
            var usuarioReserva = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == reserva.UsuarioId);
            if (usuarioReserva == null)
                return BadRequest($"No existe un usuario con ID {reserva.UsuarioId}");

            // Validar permisos según el rol del usuario autenticado
            if (userInfo.Role == "Profesor")
            {
                // Profesores solo pueden crear reservas para sí mismos
                if (userInfo.Id != reserva.UsuarioId)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Los profesores solo pueden crear reservas para sí mismos");
            }
            else if (userInfo.Role == "Coordinador")
            {
                // Coordinadores pueden crear reservas para usuarios de su departamento
                if (usuarioAutenticado.Departamento == null)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Coordinador no tiene departamento asignado. No puede crear reservas");

                // Si no es para sí mismo, validar que el usuario objetivo pertenezca al mismo departamento
                if (userInfo.Id != reserva.UsuarioId)
                {
                    if (usuarioReserva.DepartamentoId != usuarioAutenticado.DepartamentoId)
                        return Content(System.Net.HttpStatusCode.Forbidden,
                            $"Solo puede crear reservas para usuarios de su departamento ({usuarioAutenticado.Departamento.Nombre})");
                }
            }
            else if (userInfo.Role == "Administrador")
            {
                // Administradores pueden crear reservas para cualquier usuario
                // No hay restricciones adicionales
            }
            else
            {
                return Content(System.Net.HttpStatusCode.Forbidden, "No tiene permisos para crear reservas");
            }

            // Validar que el usuario objetivo tiene un rol válido para tener reservas
            if (usuarioReserva.Rol != RolUsuario.Profesor &&
                usuarioReserva.Rol != RolUsuario.Coordinador &&
                usuarioReserva.Rol != RolUsuario.Administrador)
                return BadRequest("Solo profesores, coordinadores y administradores pueden tener reservas");

            // Validar que el espacio existe y está disponible
            var espacio = db.Espacios.Find(reserva.EspacioId);
            if (espacio == null)
                return BadRequest($"No existe un espacio con ID {reserva.EspacioId}");

            if (!espacio.Disponible)
                return BadRequest("El espacio seleccionado no está disponible para reservas");

            // Validar formato y lógica del horario (HH:mm-HH:mm)
            var validacionHorario = ValidarHorario(reserva.Horario);
            if (!validacionHorario.EsValido)
                return BadRequest(validacionHorario.Mensaje);

            // Validar que la fecha no sea en el pasado
            if (reserva.Fecha.Date < DateTime.Now.Date)
                return BadRequest("No se pueden hacer reservas para fechas pasadas");

            // Validar que no haya solapamiento de horarios en el mismo espacio
            var validacionSolapamiento = ValidarSolapamientoEspacio(reserva.EspacioId, reserva.Fecha, reserva.Horario);
            if (!validacionSolapamiento.EsValido)
                return BadRequest(validacionSolapamiento.Mensaje);

            // Validar que el usuario objetivo no tenga otra reserva en el mismo horario
            var validacionConflictoUsuario = ValidarConflictoUsuario(reserva.UsuarioId, reserva.Fecha, reserva.Horario);
            if (!validacionConflictoUsuario.EsValido)
                return BadRequest(validacionConflictoUsuario.Mensaje);

            // Crear reserva con estado pendiente por defecto
            reserva.Estado = EstadoReserva.Pendiente;
            db.Reservas.Add(reserva);
            db.SaveChanges();

            // Recargar la reserva con las relaciones incluidas para la respuesta
            var reservaCreada = db.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Usuario.Departamento)
                .Include(r => r.Espacio)
                .FirstOrDefault(r => r.Id == reserva.Id);

            // Determinar quién creó la reserva para el mensaje
            var creadaPorOtro = userInfo.Id != reserva.UsuarioId;
            var mensajeCreacion = creadaPorOtro
                ? $"Reserva creada exitosamente para {reservaCreada.Usuario.Nombre} por {usuarioAutenticado.Nombre}"
                : "Reserva creada exitosamente";

            // Crear respuesta completa
            var respuesta = new
            {
                Id = reservaCreada.Id,
                UsuarioId = reservaCreada.UsuarioId,
                Usuario = new
                {
                    Id = reservaCreada.Usuario.Id,
                    Nombre = reservaCreada.Usuario.Nombre,
                    Email = reservaCreada.Usuario.Email,
                    Rol = reservaCreada.Usuario.Rol.ToString(),
                    Departamento = reservaCreada.Usuario.Departamento != null ? new
                    {
                        Id = reservaCreada.Usuario.Departamento.Id,
                        Nombre = reservaCreada.Usuario.Departamento.Nombre,
                        Codigo = reservaCreada.Usuario.Departamento.Codigo
                    } : null
                },
                EspacioId = reservaCreada.EspacioId,
                Espacio = new
                {
                    Id = reservaCreada.Espacio.Id,
                    Nombre = reservaCreada.Espacio.Nombre,
                    Tipo = reservaCreada.Espacio.Tipo.ToString(),
                    Capacidad = reservaCreada.Espacio.Capacidad,
                    Ubicacion = reservaCreada.Espacio.Ubicacion
                },
                Fecha = reservaCreada.Fecha,
                Horario = reservaCreada.Horario,
                Estado = reservaCreada.Estado.ToString(),
                Descripcion = reservaCreada.Descripcion,
                CreadaPor = new
                {
                    Id = usuarioAutenticado.Id,
                    Nombre = usuarioAutenticado.Nombre,
                    Email = usuarioAutenticado.Email,
                    Rol = usuarioAutenticado.Rol.ToString(),
                    EsCreadorDiferente = creadaPorOtro
                },
                FechaCreacion = DateTime.Now,
                Message = mensajeCreacion
            };

            // Retornar 201 Created con ubicación del recurso
            return CreatedAtRoute("DefaultApi", new { id = reserva.Id }, respuesta);
        }

        #endregion

        #region Métodos de Validación

        /// <summary>
        /// Valida el formato y lógica del horario
        /// Formato esperado: HH:mm-HH:mm (ej: 09:00-10:00)
        /// </summary>
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

            // Validar que las horas sean válidas
            if (!TimeSpan.TryParse(partes[0], out horaInicio))
                return new ValidacionResult(false, string.Format("Hora de inicio inválida: {0}. Use formato HH:mm", partes[0]));

            if (!TimeSpan.TryParse(partes[1], out horaFin))
                return new ValidacionResult(false, string.Format("Hora de fin inválida: {0}. Use formato HH:mm", partes[1]));

            // Validar que hora inicio sea menor que hora fin
            if (horaInicio >= horaFin)
                return new ValidacionResult(false, "La hora de inicio debe ser menor que la hora de fin");

            // Validar duración mínima de 30 minutos
            var duracion = horaFin - horaInicio;
            if (duracion.TotalMinutes < 30)
                return new ValidacionResult(false, "La reserva debe tener una duración mínima de 30 minutos");

            // Validar horarios laborales (6:00 AM - 10:00 PM)
            if (horaInicio < TimeSpan.FromHours(6) || horaFin > TimeSpan.FromHours(22))
                return new ValidacionResult(false, "Las reservas solo se permiten entre 06:00 y 22:00");

            return new ValidacionResult(true, "Horario válido");
        }

        /// <summary>
        /// Valida que no haya solapamiento de horarios en el mismo espacio
        /// para una fecha específica
        /// </summary>
        private ValidacionResult ValidarSolapamientoEspacio(int espacioId, DateTime fecha, string horario)
        {
            // Definir rango de fecha (todo el día)
            DateTime fechaInicio = fecha.Date;
            DateTime fechaFin = fechaInicio.AddDays(1);

            // Obtener reservas existentes del espacio en la fecha (excluyendo rechazadas)
            var reservasExistentes = db.Reservas
                .Where(r => r.EspacioId == espacioId &&
                           r.Fecha >= fechaInicio &&
                           r.Fecha < fechaFin &&
                           r.Estado != EstadoReserva.Rechazada)
                .Include(r => r.Usuario)
                .ToList();

            // Parsear horario de la nueva reserva
            var partesHorarioNuevo = horario.Split('-');
            var horaInicioNueva = TimeSpan.Parse(partesHorarioNuevo[0]);
            var horaFinNueva = TimeSpan.Parse(partesHorarioNuevo[1]);

            // Verificar solapamiento con cada reserva existente
            foreach (var reserva in reservasExistentes)
            {
                var partesHorarioExistente = reserva.Horario.Split('-');
                var horaInicioExistente = TimeSpan.Parse(partesHorarioExistente[0]);
                var horaFinExistente = TimeSpan.Parse(partesHorarioExistente[1]);

                // Lógica de solapamiento: dos intervalos se solapan si NO es cierto que
                // uno termine antes de que empiece el otro
                bool hayConflicto = !(horaFinNueva <= horaInicioExistente || horaInicioNueva >= horaFinExistente);

                if (hayConflicto)
                {
                    return new ValidacionResult(false, string.Format("El horario {0} se solapa con una reserva existente ({1}) del usuario {2} en estado {3}",
                        horario, reserva.Horario, reserva.Usuario.Nombre, reserva.Estado));
                }
            }

            return new ValidacionResult(true, "No hay conflictos de horario");
        }

        /// <summary>
        /// Valida que el usuario no tenga otra reserva en el mismo horario
        /// (evita que un usuario esté en dos lugares al mismo tiempo)
        /// </summary>
        private ValidacionResult ValidarConflictoUsuario(int usuarioId, DateTime fecha, string horario)
        {
            // Definir rango de fecha
            DateTime fechaInicio = fecha.Date;
            DateTime fechaFin = fechaInicio.AddDays(1);

            // Obtener todas las reservas del usuario en la fecha
            var reservasUsuario = db.Reservas
                .Where(r => r.UsuarioId == usuarioId &&
                           r.Fecha >= fechaInicio &&
                           r.Fecha < fechaFin &&
                           r.Estado != EstadoReserva.Rechazada)
                .Include(r => r.Espacio)
                .ToList();

            // Parsear horario de la nueva reserva
            var partesHorarioNuevo = horario.Split('-');
            var horaInicioNueva = TimeSpan.Parse(partesHorarioNuevo[0]);
            var horaFinNueva = TimeSpan.Parse(partesHorarioNuevo[1]);

            // Verificar conflictos con reservas existentes del usuario
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

        #endregion

        #region JWT Validation

        /// <summary>
        /// Valida el token JWT del header Authorization
        /// Extrae información del usuario (ID, Email, Role)
        /// </summary>
        private JwtUserInfo ValidateJwtToken()
        {
            try
            {
                // Obtener header de autorización
                var authHeader = Request.Headers.Authorization;
                if (authHeader == null || authHeader.Scheme != "Bearer")
                    return null;

                var token = authHeader.Parameter;
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(AppConfig.JwtSecretKey);

                // Validar el token
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                // Extraer claims del token
                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
                var userEmail = jwtToken.Claims.First(x => x.Type == "email").Value;
                var userRole = jwtToken.Claims.First(x => x.Type == "role").Value;

                return new JwtUserInfo { Id = userId, Email = userEmail, Role = userRole };
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region PUT - Actualizar Reserva

        /// <summary>
        /// PUT: api/Reserva/{id}
        /// Actualiza una reserva existente con validaciones de permisos
        /// y re-validación de disponibilidad si cambian datos críticos
        /// </summary>
        [HttpPut]
        public IHttpActionResult ActualizarReserva(int id, Reserva reservaDto)
        {
            // Validar modelo
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar que el ID coincida
            if (id != reservaDto.Id)
                return BadRequest("El ID de la URL no coincide con el ID del objeto");

            // Validar token JWT
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

                // Profesor no puede cambiar el usuario asignado
                if (reservaDto.UsuarioId != reservaExistente.UsuarioId)
                    return BadRequest("Los profesores no pueden cambiar el usuario de la reserva");

                // Mantener valores que el profesor no puede cambiar
                reservaDto.UsuarioId = reservaExistente.UsuarioId;
                reservaDto.Estado = reservaExistente.Estado; // Mantener estado actual
            }
            else if (userInfo.Role == "Coordinador")
            {
                // Coordinador solo puede gestionar reservas de su departamento
                var coordinador = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == userInfo.Id);
                if (coordinador?.Departamento == null)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Coordinador no tiene departamento asignado");

                var usuarioReserva = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == reservaExistente.UsuarioId);
                if (usuarioReserva?.DepartamentoId != coordinador.DepartamentoId)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Solo puede gestionar reservas de profesores de su departamento");

                // Si cambia el usuario, validar que el nuevo usuario sea de su departamento
                if (reservaDto.UsuarioId != reservaExistente.UsuarioId)
                {
                    var nuevoUsuario = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == reservaDto.UsuarioId);
                    if (nuevoUsuario?.DepartamentoId != coordinador.DepartamentoId)
                        return BadRequest("Solo puede asignar reservas a profesores de su departamento");
                }
            }
            else if (userInfo.Role == "Administrador")
            {
                // Administrador puede editar cualquier reserva sin restricciones
            }
            else
            {
                return Content(System.Net.HttpStatusCode.Unauthorized, "No tiene permisos para editar reservas");
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
            reservaExistente.Descripcion = reservaDto.Descripcion; // AGREGAR: Actualizar descripción

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

            // Guardar cambios
            db.Entry(reservaExistente).State = EntityState.Modified;
            db.SaveChanges();

            // Recargar con relaciones para la respuesta
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
                Descripcion = reservaActualizada.Descripcion, // AGREGAR: Descripción de la reserva
                Message = "Reserva actualizada exitosamente"
            };

            return Ok(respuesta);
        }

        /// <summary>
        /// Versión de ValidarSolapamientoEspacio que excluye la reserva actual
        /// (útil para actualizaciones donde la reserva puede mantener su horario actual)
        /// </summary>
        private ValidacionResult ValidarSolapamientoEspacioParaActualizacion(int espacioId, DateTime fecha, string horario, int reservaIdExcluir)
        {
            DateTime fechaInicio = fecha.Date;
            DateTime fechaFin = fechaInicio.AddDays(1);

            var reservasExistentes = db.Reservas
                .Where(r => r.EspacioId == espacioId &&
                           r.Fecha >= fechaInicio &&
                           r.Fecha < fechaFin &&
                           r.Estado != EstadoReserva.Rechazada &&
                           r.Id != reservaIdExcluir) // Excluir la reserva actual de la validación
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

                bool hayConflicto = !(horaFinNueva <= horaInicioExistente || horaInicioNueva >= horaFinExistente);

                if (hayConflicto)
                {
                    return new ValidacionResult(false, $"El horario {horario} se solapa con una reserva existente ({reserva.Horario}) del usuario {reserva.Usuario.Nombre} en estado {reserva.Estado}");
                }
            }

            return new ValidacionResult(true, "No hay conflictos de horario");
        }

        /// <summary>
        /// Versión de ValidarConflictoUsuario que excluye la reserva actual
        /// </summary>
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

                bool hayConflicto = !(horaFinNueva <= horaInicioExistente || horaInicioNueva >= horaFinExistente);

                if (hayConflicto)
                {
                    return new ValidacionResult(false, $"El usuario ya tiene una reserva en el horario {reserva.Horario} para el espacio {reserva.Espacio.Nombre} en estado {reserva.Estado}. No puede tener dos reservas simultáneas.");
                }
            }

            return new ValidacionResult(true, "No hay conflictos de usuario");
        }

        #endregion

        #region DELETE - Eliminar Reserva

        /// <summary>
        /// DELETE: api/Reserva/{id}
        /// Elimina una reserva con validaciones de permisos y estado
        /// </summary>
        [HttpDelete]
        public IHttpActionResult BorrarReserva(int id)
        {
            // Validar token JWT
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
                // Coordinador solo puede eliminar reservas de su departamento
                var coordinador = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == userInfo.Id);
                if (coordinador?.Departamento == null)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Coordinador no tiene departamento asignado");

                var usuarioReserva = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == reserva.UsuarioId);
                if (usuarioReserva?.DepartamentoId != coordinador.DepartamentoId)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Solo puede eliminar reservas de profesores de su departamento");
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

                // Validar si la reserva ya pasó
                if (reserva.Fecha.Date < DateTime.Now.Date)
                {
                    if (userInfo.Role != "Administrador")
                        return BadRequest("No se pueden eliminar reservas de fechas pasadas. Solo los administradores pueden hacerlo.");
                }
            }

            // Guardar información para la respuesta antes de eliminar
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
                Descripcion = reserva.Descripcion, // AGREGAR: Descripción de la reserva
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

        #endregion

        #region Consultas de Disponibilidad

        /// <summary>
        /// GET: api/Reserva/disponibilidad/{espacioId}
        /// Consulta la disponibilidad de un espacio específico
        /// Permite filtrar por fecha y/o horario
        /// </summary>
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

            // Construir query base para reservas del espacio
            var query = db.Reservas
                .Where(r => r.EspacioId == espacioId && r.Estado != EstadoReserva.Rechazada)
                .Include(r => r.Usuario)
                .Include(r => r.Usuario.Departamento) // AGREGAR: Incluir departamento
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
                            Email = r.Usuario.Email,
                            // AGREGAR: Información del departamento
                            Departamento = r.Usuario.Departamento != null ? new
                            {
                                Id = r.Usuario.Departamento.Id,
                                Nombre = r.Usuario.Departamento.Nombre,
                                Codigo = r.Usuario.Departamento.Codigo
                            } : null
                        },
                        Fecha = r.Fecha,
                        Horario = r.Horario,
                        Estado = r.Estado.ToString(),
                        Descripcion = r.Descripcion // AGREGAR: Descripción de la reserva
                    }),
                    Mensaje = mensaje
                });
            }

            // Si hay filtro de horario, verificar solapamientos específicos
            var reservasConflictivas = new List<dynamic>();
            bool disponible = true;

            foreach (var reserva in reservasExistentes)
            {
                var partesHorarioExistente = reserva.Horario.Split('-');
                var horaInicioExistente = TimeSpan.Parse(partesHorarioExistente[0]);
                var horaFinExistente = TimeSpan.Parse(partesHorarioExistente[1]);

                // Verificar solapamiento con el horario consultado
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
                        Descripcion = reserva.Descripcion, // AGREGAR: Descripción de la reserva
                        TipoConflicto = "Solapamiento de horarios"
                    });
                }
            }

            // Generar mensaje final
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

        #endregion

        #region Historial

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
            var usuario = db.Usuarios
    .Include(u => u.Departamento)
    .FirstOrDefault(u => u.Id == usuarioId);

            if (usuario == null)
                return BadRequest($"No existe un usuario con ID {usuarioId}");

            var query = db.Reservas
                .Where(r => r.UsuarioId == usuarioId)
                .Include(r => r.Usuario)
                .Include(r => r.Usuario.Departamento) // AGREGAR: Incluir departamento
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
                    Rol = usuario.Rol.ToString(),
                    // AGREGAR: Información del departamento
                    Departamento = usuario.Departamento != null ? new
                    {
                        Id = usuario.Departamento.Id,
                        Nombre = usuario.Departamento.Nombre,
                        Codigo = usuario.Departamento.Codigo,
                        Tipo = usuario.Departamento.Tipo.ToString()
                    } : null
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
                    Estado = r.Estado.ToString(),
                    Descripcion = r.Descripcion
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
                .Include(r => r.Usuario.Departamento) // AGREGAR: Incluir departamento
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
                        Rol = r.Usuario.Rol.ToString(),
                        // AGREGAR: Información del departamento
                        Departamento = r.Usuario.Departamento != null ? new
                        {
                            Id = r.Usuario.Departamento.Id,
                            Nombre = r.Usuario.Departamento.Nombre,
                            Codigo = r.Usuario.Departamento.Codigo,
                            Tipo = r.Usuario.Departamento.Tipo.ToString()
                        } : null
                    },
                    EspacioId = r.EspacioId,
                    Fecha = r.Fecha,
                    Horario = r.Horario,
                    Estado = r.Estado.ToString(),
                    Descripcion = r.Descripcion
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
                .Include(r => r.Usuario.Departamento) // AGREGAR: Incluir departamento
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
                        Rol = r.Usuario.Rol.ToString(),
                        // AGREGAR: Información del departamento
                        Departamento = r.Usuario.Departamento != null ? new
                        {
                            Id = r.Usuario.Departamento.Id,
                            Nombre = r.Usuario.Departamento.Nombre,
                            Codigo = r.Usuario.Departamento.Codigo,
                            Tipo = r.Usuario.Departamento.Tipo.ToString()
                        } : null
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
                    Estado = r.Estado.ToString(),
                    Descripcion = r.Descripcion
                })
            });
        }

        #endregion




        #region Gestión de Estados de Reserva

        /// <summary>
        /// PUT: api/Reserva/{id}/aprobar
        /// Aprueba una reserva pendiente
        /// Solo coordinadores y administradores pueden aprobar
        /// </summary>
        [HttpPut]
        [Route("api/Reserva/{id}/aprobar")]
        public IHttpActionResult AprobarReserva(int id)
        {
            // Validar token JWT
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            var reserva = db.Reservas.Find(id);
            if (reserva == null)
                return NotFound();

            // Validar permisos para aprobar
            if (userInfo.Role == "Administrador")
            {
                // Administradores pueden aprobar cualquier reserva
            }
            else if (userInfo.Role == "Coordinador")
            {
                // Coordinadores solo pueden aprobar reservas de su departamento
                var coordinador = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == userInfo.Id);
                if (coordinador?.Departamento == null)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Coordinador no tiene departamento asignado");

                var usuarioReserva = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == reserva.UsuarioId);
                if (usuarioReserva?.DepartamentoId != coordinador.DepartamentoId)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Solo puede aprobar reservas de profesores de su departamento");
            }
            else
            {
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo administradores y coordinadores pueden aprobar reservas");
            }

            // Validar que la reserva esté en estado pendiente
            if (reserva.Estado != EstadoReserva.Pendiente)
                return BadRequest("Solo se pueden aprobar reservas pendientes");

            // Cambiar estado a aprobada
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

        /// <summary>
        /// PUT: api/Reserva/{id}/rechazar
        /// Rechaza una reserva pendiente
        /// Solo coordinadores y administradores pueden rechazar
        /// </summary>
        [HttpPut]
        [Route("api/Reserva/{id}/rechazar")]
        public IHttpActionResult RechazarReserva(int id)
        {
            // Validar token JWT
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            var reserva = db.Reservas.Find(id);
            if (reserva == null)
                return NotFound();

            // Validar permisos para rechazar (mismo que aprobar)
            if (userInfo.Role == "Administrador")
            {
                // Administradores pueden rechazar cualquier reserva
            }
            else if (userInfo.Role == "Coordinador")
            {
                // Coordinadores solo pueden rechazar reservas de su departamento
                var coordinador = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == userInfo.Id);
                if (coordinador?.Departamento == null)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Coordinador no tiene departamento asignado");

                var usuarioReserva = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == reserva.UsuarioId);
                if (usuarioReserva?.DepartamentoId != coordinador.DepartamentoId)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Solo puede rechazar reservas de profesores de su departamento");
            }
            else
            {
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo administradores y coordinadores pueden rechazar reservas");
            }

            // Validar que la reserva esté en estado pendiente
            if (reserva.Estado != EstadoReserva.Pendiente)
                return BadRequest("Solo se pueden rechazar reservas pendientes");

            // Cambiar estado a rechazada
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

        /// <summary>
        /// GET: api/Reserva/pendientes
        /// Obtiene todas las reservas pendientes de aprobación
        /// Filtradas por departamento para coordinadores
        /// </summary>
        [HttpGet]
        [Route("api/Reserva/pendientes")]
        public IHttpActionResult ObtenerReservasPendientes()
        {
            // Validar token JWT
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Query base para reservas pendientes
            var query = db.Reservas
                .Where(r => r.Estado == EstadoReserva.Pendiente)
                .Include(r => r.Usuario)
                .Include(r => r.Usuario.Departamento) // AGREGAR: Incluir departamento
                .Include(r => r.Espacio)
                .AsQueryable();

            // Filtrar por departamento para coordinadores
            if (userInfo.Role == "Coordinador")
            {
                var coordinador = db.Usuarios.Include(u => u.Departamento).FirstOrDefault(u => u.Id == userInfo.Id);
                if (coordinador?.Departamento == null)
                    return Content(System.Net.HttpStatusCode.Forbidden, "Coordinador no tiene departamento asignado");

                query = query.Where(r => r.Usuario.DepartamentoId == coordinador.DepartamentoId);
            }
            else if (userInfo.Role != "Administrador")
            {
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo administradores y coordinadores pueden consultar reservas pendientes");
            }

            // Ejecutar query ordenado
            var reservasPendientes = query
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
                ReservasPendientes = reservasPendientes.Select(r => new
                {
                    Id = r.Id,
                    UsuarioId = r.UsuarioId,
                    Usuario = new
                    {
                        Id = r.Usuario.Id,
                        Nombre = r.Usuario.Nombre,
                        Email = r.Usuario.Email,
                        Rol = r.Usuario.Rol.ToString(),
                        // AGREGAR: Información del departamento
                        Departamento = r.Usuario.Departamento != null ? new
                        {
                            Id = r.Usuario.Departamento.Id,
                            Nombre = r.Usuario.Departamento.Nombre,
                            Codigo = r.Usuario.Departamento.Codigo,
                            Tipo = r.Usuario.Departamento.Tipo.ToString()
                        } : null
                    },
                    EspacioId = r.EspacioId,
                    Espacio = new
                    {
                        Id = r.Espacio.Id,
                        Nombre = r.Espacio.Nombre,
                        Tipo = r.Espacio.Tipo.ToString(),
                        Capacidad = r.Espacio.Capacidad,
                        Ubicacion = r.Espacio.Ubicacion
                    },
                    Fecha = r.Fecha,
                    Horario = r.Horario,
                    Estado = r.Estado.ToString(),
                    Descripcion = r.Descripcion
                })
            });
        }

        #endregion

        #region Consulta General de Espacios Disponibles

        /// <summary>
        /// GET: api/Reserva/espacios-disponibles
        /// Consulta la disponibilidad de todos los espacios
        /// Permite filtrar por fecha y/o horario específicos
        /// Útil para encontrar espacios libres para reservar
        /// </summary>
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

            // Evaluar la disponibilidad de cada espacio
            foreach (var espacio in espaciosDisponibles)
            {
                // Construir query para reservas del espacio
                var queryReservas = db.Reservas
                    .Where(r => r.EspacioId == espacio.Id && r.Estado != EstadoReserva.Rechazada)
                    .Include(r => r.Usuario)
                    .Include(r => r.Usuario.Departamento); // AGREGAR: Incluir departamento

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
                                Descripcion = reserva.Descripcion, // AGREGAR: Descripción de la reserva
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

                // Generar mensaje específico para este espacio
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
                            Email = r.Usuario.Email,
                            // AGREGAR: Información del departamento
                            Departamento = r.Usuario.Departamento != null ? new
                            {
                                Id = r.Usuario.Departamento.Id,
                                Nombre = r.Usuario.Departamento.Nombre,
                                Codigo = r.Usuario.Departamento.Codigo
                            } : null
                        },
                        Fecha = r.Fecha,
                        Horario = r.Horario,
                        Estado = r.Estado.ToString(),
                        Descripcion = r.Descripcion // AGREGAR: Descripción de la reserva
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

        #endregion

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
