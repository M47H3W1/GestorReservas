using System.Web.Http;
using System.Data.Entity;
using GestorReservas.DAL;
using GestorReservas.Models;
using System.Collections.Generic;
using System.Linq;

namespace GestorReservas.Controllers
{
    public class UsuarioController : ApiController
    {
        // Instancia del contexto de base de datos
        private GestorReserva db = new GestorReserva();

        [HttpPost]
        public IHttpActionResult RegistrarUsuario(Usuario usuario)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            db.Usuarios.Add(usuario);
            db.SaveChanges();
            return CreatedAtRoute("DefaultApi", new { id = usuario.Id }, usuario);
        }


        [HttpPost]
        [Route("api/Usuario/autenticar")]
        public IHttpActionResult AutenticarUsuario(Usuario usuario)
        {
            var usuarioAutenticado = db.Usuarios.FirstOrDefault(u => u.Email == usuario.Email && u.Password == usuario.Password);
            if (usuarioAutenticado == null)
                return Unauthorized();
            return Ok(usuarioAutenticado);
        }

        // GET: api/Usuario
        [HttpGet]
        public IEnumerable<Usuario> ObtenerUsuarios()
        {
            return db.Usuarios.ToList();
        }

        // GET: api/Usuario/{id}
        [HttpGet]
        public IHttpActionResult ObtenerUsuario(int id)
        {
            var usuario = db.Usuarios.Find(id);
            if (usuario == null)
                return NotFound();
            return Ok(usuario);
        }

        // PUT: api/Usuario/{id}
        [HttpPut]
        public IHttpActionResult ActualizarUsuario(int id, Usuario usuario)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (id != usuario.Id)
                return BadRequest();
            db.Entry(usuario).State = EntityState.Modified;
            db.SaveChanges();
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        // DELETE: api/Usuario/{id}
        [HttpDelete]
        public IHttpActionResult BorrarUsuario(int id)
        {
            var usuario = db.Usuarios.Find(id);
            if (usuario == null)
                return NotFound();
            db.Usuarios.Remove(usuario);
            db.SaveChanges();
            return Ok(usuario);
        }
    }
}
