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
    // Controlador para la gestión de departamentos
    public class DepartamentoController : ApiController
    {
        // Instancia del contexto de base de datos para acceder a las entidades
        private GestorReserva db = new GestorReserva();

        // Constructor: valida la configuración JWT al inicializar el controlador
        public DepartamentoController()
        {
            // Verifica que la configuración de JWT esté correctamente definida
            AppConfig.ValidateJwtConfiguration();
        }

        // GET: api/Departamento
        // Obtiene la lista de todos los departamentos con información de jefe y profesores
        [HttpGet]
        public IHttpActionResult ObtenerDepartamentos()
        {
            // Valida el token JWT del usuario que realiza la petición
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                // Si el token no es válido, retorna estado 401 (no autorizado)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Consulta todos los departamentos e incluye la información del jefe y los profesores relacionados
            var departamentos = db.Departamentos
                .Include(d => d.Jefe)         // Incluye la relación con el jefe del departamento
                .Include(d => d.Profesores)   // Incluye la relación con los profesores del departamento
                .ToList();                    // Convierte el resultado a una lista

            // Proyecta la información relevante de cada departamento en un objeto anónimo
            var resultado = departamentos.Select(d => new
            {
                Id = d.Id,                                   // Identificador del departamento
                Nombre = d.Nombre,                           // Nombre del departamento
                Codigo = d.Codigo,                           // Código único del departamento
                Tipo = d.Tipo.ToString(),                    // Tipo de departamento (convertido a string)
                Descripcion = d.Descripcion,                 // Descripción del departamento
                Jefe = d.Jefe != null ? new                  // Información del jefe si existe
                {
                    Id = d.Jefe.Id,
                    Nombre = d.Jefe.Nombre,
                    Email = d.Jefe.Email
                } : null,
                TotalProfesores = d.Profesores.Count(),      // Número total de profesores en el departamento
                Profesores = d.Profesores.Select(p => new    // Lista de profesores con información básica
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Email = p.Email
                }).ToList()
            });

            // Retorna la lista de departamentos en formato JSON
            return Ok(resultado);
        }

        // POST: api/Departamento
        // Crea un nuevo departamento (solo administradores)
        [HttpPost]
        public IHttpActionResult CrearDepartamento(Departamento departamento)
        {
            // Valida el token JWT del usuario
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                // Si el token no es válido, retorna estado 401
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Verifica que el usuario tenga el rol de Administrador
            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo administradores pueden crear departamentos");

            // Valida el modelo recibido
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verifica que el código del departamento sea único en la base de datos
            if (db.Departamentos.Any(d => d.Codigo == departamento.Codigo))
                return BadRequest($"Ya existe un departamento con el código {departamento.Codigo}");

            // Si se especifica jefe, valida que sea coordinador y no jefe de otro departamento
            if (departamento.JefeId.HasValue)
            {
                // Busca el usuario que será jefe
                var jefe = db.Usuarios.Find(departamento.JefeId.Value);
                if (jefe == null)
                    return BadRequest($"No existe un usuario con ID {departamento.JefeId}");

                // Verifica que el usuario tenga el rol de Coordinador
                if (jefe.Rol != RolUsuario.Coordinador)
                    return BadRequest("Solo coordinadores pueden ser jefes de departamento");

                // Verifica que el usuario no sea ya jefe de otro departamento
                if (db.Departamentos.Any(d => d.JefeId == departamento.JefeId))
                    return BadRequest("Este coordinador ya es jefe de otro departamento");
            }

            // Agrega el nuevo departamento a la base de datos
            db.Departamentos.Add(departamento);
            db.SaveChanges(); // Guarda los cambios

            // Retorna el departamento creado con estado 201 (Created)
            return CreatedAtRoute("DefaultApi", new { id = departamento.Id }, departamento);
        }

        // PUT: api/Departamento/{id}/asignar-jefe
        // Asigna un jefe a un departamento (solo administradores)
        [HttpPut]
        [Route("api/Departamento/{id}/asignar-jefe")]
        public IHttpActionResult AsignarJefe(int id, [FromBody] dynamic data)
        {
            // Valida el token JWT
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Verifica que el usuario sea administrador
            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo administradores pueden asignar jefes de departamento");

            // Busca el departamento por ID
            var departamento = db.Departamentos.Find(id);
            if (departamento == null)
                return NotFound();

            // Obtiene el ID del jefe desde el cuerpo de la petición
            int jefeId = data.jefeId;
            var jefe = db.Usuarios.Find(jefeId);
            if (jefe == null)
                return BadRequest($"No existe un usuario con ID {jefeId}");

            // Verifica que el usuario tenga el rol de Coordinador
            if (jefe.Rol != RolUsuario.Coordinador)
                return BadRequest("Solo coordinadores pueden ser jefes de departamento");

            // Verifica que el usuario no sea ya jefe de otro departamento
            if (db.Departamentos.Any(d => d.JefeId == jefeId && d.Id != id))
                return BadRequest("Este coordinador ya es jefe de otro departamento");

            // Asigna el jefe al departamento y guarda los cambios
            departamento.JefeId = jefeId;
            db.SaveChanges();

            // Retorna información del departamento y del jefe asignado
            return Ok(new
            {
                Message = "Jefe de departamento asignado exitosamente",
                Departamento = departamento.Nombre,
                Jefe = new
                {
                    Id = jefe.Id,
                    Nombre = jefe.Nombre,
                    Email = jefe.Email
                }
            });
        }

        // GET: api/Departamento/profesores-sin-departamento
        // Obtiene la lista de profesores que no están asignados a ningún departamento (solo administradores)
        [HttpGet]
        [Route("api/Departamento/profesores-sin-departamento")]
        public IHttpActionResult ObtenerProfesoresSinDepartamento()
        {
            // Valida el token JWT
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Verifica que el usuario sea administrador
            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo administradores pueden consultar esta información");

            // Consulta los profesores que no tienen departamento asignado
            var profesoresSinDepartamento = db.Usuarios
                .Where(u => u.Rol == RolUsuario.Profesor && u.DepartamentoId == null)
                .Select(u => new
                {
                    Id = u.Id,           // ID del profesor
                    Nombre = u.Nombre,   // Nombre del profesor
                    Email = u.Email      // Email del profesor
                })
                .ToList();

            // Retorna la lista de profesores sin departamento
            return Ok(profesoresSinDepartamento);
        }

        // PUT: api/Departamento/{id}/asignar-profesor
        // Asigna un profesor a un departamento (solo administradores)
        [HttpPut]
        [Route("api/Departamento/{id}/asignar-profesor")]
        public IHttpActionResult AsignarProfesorADepartamento(int id, [FromBody] dynamic data)
        {
            // Valida el token JWT
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            // Verifica que el usuario sea administrador
            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo administradores pueden asignar profesores a departamentos");

            // Busca el departamento por ID
            var departamento = db.Departamentos.Find(id);
            if (departamento == null)
                return NotFound();

            // Obtiene el ID del profesor desde el cuerpo de la petición
            int profesorId = data.profesorId;
            var profesor = db.Usuarios.Find(profesorId);
            if (profesor == null)
                return BadRequest($"No existe un usuario con ID {profesorId}");

            // Verifica que el usuario tenga el rol de Profesor
            if (profesor.Rol != RolUsuario.Profesor)
                return BadRequest("Solo profesores pueden ser asignados a departamentos");

            // Asigna el departamento al profesor y guarda los cambios
            profesor.DepartamentoId = id;
            db.SaveChanges();

            // Retorna información del departamento y del profesor asignado
            return Ok(new
            {
                Message = "Profesor asignado exitosamente al departamento",
                Departamento = departamento.Nombre,
                Profesor = new
                {
                    Id = profesor.Id,
                    Nombre = profesor.Nombre,
                    Email = profesor.Email
                }
            });
        }

        // Método auxiliar para validar el token JWT y extraer información del usuario
        private JwtUserInfo ValidateJwtToken()
        {
            try
            {
                // Obtiene el header de autorización de la petición
                var authHeader = Request.Headers.Authorization;
                if (authHeader == null || authHeader.Scheme != "Bearer")
                    // Si no existe o el esquema no es Bearer, retorna null
                    return null;

                // Obtiene el token JWT del header
                var token = authHeader.Parameter;
                var tokenHandler = new JwtSecurityTokenHandler();
                // Obtiene la clave secreta desde la configuración
                var key = Encoding.ASCII.GetBytes(AppConfig.JwtSecretKey);

                // Valida el token JWT usando los parámetros de seguridad
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,              // Valida la firma del emisor
                    IssuerSigningKey = new SymmetricSecurityKey(key), // Clave de firma
                    ValidateIssuer = false,                       // No valida el emisor
                    ValidateAudience = false,                     // No valida la audiencia
                    ClockSkew = TimeSpan.Zero                     // Sin tolerancia de tiempo
                }, out SecurityToken validatedToken);

                // Extrae los claims del token JWT
                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
                var userEmail = jwtToken.Claims.First(x => x.Type == "email").Value;
                var userRole = jwtToken.Claims.First(x => x.Type == "role").Value;

                // Retorna la información del usuario extraída del token
                return new JwtUserInfo { Id = userId, Email = userEmail, Role = userRole };
            }
            catch
            {
                // Si ocurre algún error en la validación, retorna null
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

