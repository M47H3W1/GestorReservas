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
    public class DepartamentoController : ApiController
    {
        private GestorReserva db = new GestorReserva();

        // Constructor para validar configuración
        public DepartamentoController()
        {
            AppConfig.ValidateJwtConfiguration();
        }

        // GET: api/Departamento
        [HttpGet]
        public IHttpActionResult ObtenerDepartamentos()
        {
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            var departamentos = db.Departamentos
                .Include(d => d.Jefe)
                .Include(d => d.Profesores)
                .ToList();

            var resultado = departamentos.Select(d => new
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Codigo = d.Codigo,
                Tipo = d.Tipo.ToString(),
                Descripcion = d.Descripcion,
                Jefe = d.Jefe != null ? new
                {
                    Id = d.Jefe.Id,
                    Nombre = d.Jefe.Nombre,
                    Email = d.Jefe.Email
                } : null,
                TotalProfesores = d.Profesores.Count(),
                Profesores = d.Profesores.Select(p => new
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Email = p.Email
                }).ToList()
            });

            return Ok(resultado);
        }

        // POST: api/Departamento
        [HttpPost]
        public IHttpActionResult CrearDepartamento(Departamento departamento)
        {
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo administradores pueden crear departamentos");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar que el código sea único
            if (db.Departamentos.Any(d => d.Codigo == departamento.Codigo))
                return BadRequest($"Ya existe un departamento con el código {departamento.Codigo}");

            // Si se especifica un jefe, validar que sea coordinador y no sea jefe de otro departamento
            if (departamento.JefeId.HasValue)
            {
                var jefe = db.Usuarios.Find(departamento.JefeId.Value);
                if (jefe == null)
                    return BadRequest($"No existe un usuario con ID {departamento.JefeId}");

                if (jefe.Rol != RolUsuario.Coordinador)
                    return BadRequest("Solo coordinadores pueden ser jefes de departamento");

                if (db.Departamentos.Any(d => d.JefeId == departamento.JefeId))
                    return BadRequest("Este coordinador ya es jefe de otro departamento");
            }

            db.Departamentos.Add(departamento);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = departamento.Id }, departamento);
        }

        // PUT: api/Departamento/{id}/asignar-jefe
        [HttpPut]
        [Route("api/Departamento/{id}/asignar-jefe")]
        public IHttpActionResult AsignarJefe(int id, [FromBody] dynamic data)
        {
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo administradores pueden asignar jefes de departamento");

            var departamento = db.Departamentos.Find(id);
            if (departamento == null)
                return NotFound();

            int jefeId = data.jefeId;
            var jefe = db.Usuarios.Find(jefeId);
            if (jefe == null)
                return BadRequest($"No existe un usuario con ID {jefeId}");

            if (jefe.Rol != RolUsuario.Coordinador)
                return BadRequest("Solo coordinadores pueden ser jefes de departamento");

            if (db.Departamentos.Any(d => d.JefeId == jefeId && d.Id != id))
                return BadRequest("Este coordinador ya es jefe de otro departamento");

            departamento.JefeId = jefeId;
            db.SaveChanges();

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
        [HttpGet]
        [Route("api/Departamento/profesores-sin-departamento")]
        public IHttpActionResult ObtenerProfesoresSinDepartamento()
        {
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo administradores pueden consultar esta información");

            var profesoresSinDepartamento = db.Usuarios
                .Where(u => u.Rol == RolUsuario.Profesor && u.DepartamentoId == null)
                .Select(u => new
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Email = u.Email
                })
                .ToList();

            return Ok(profesoresSinDepartamento);
        }

        // PUT: api/Departamento/{id}/asignar-profesor
        [HttpPut]
        [Route("api/Departamento/{id}/asignar-profesor")]
        public IHttpActionResult AsignarProfesorADepartamento(int id, [FromBody] dynamic data)
        {
            var userInfo = ValidateJwtToken();
            if (userInfo == null)
                return Content(System.Net.HttpStatusCode.Unauthorized, "Token de autenticación requerido");

            if (userInfo.Role != "Administrador")
                return Content(System.Net.HttpStatusCode.Forbidden, "Solo administradores pueden asignar profesores a departamentos");

            var departamento = db.Departamentos.Find(id);
            if (departamento == null)
                return NotFound();

            int profesorId = data.profesorId;
            var profesor = db.Usuarios.Find(profesorId);
            if (profesor == null)
                return BadRequest($"No existe un usuario con ID {profesorId}");

            if (profesor.Rol != RolUsuario.Profesor)
                return BadRequest("Solo profesores pueden ser asignados a departamentos");

            profesor.DepartamentoId = id;
            db.SaveChanges();

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

        // MÉTODO PARA VALIDAR JWT TOKEN (usa la clase JwtUserInfo del ReservaController)
        private JwtUserInfo ValidateJwtToken()
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

                return new JwtUserInfo { Id = userId, Email = userEmail, Role = userRole };
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