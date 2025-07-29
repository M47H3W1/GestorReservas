using System.Web.Http;
using System.Data.Entity;
using GestorReservas.DAL;
using GestorReservas.Models;
using System.Collections.Generic;
using System.Linq;

namespace GestorReservas.Controllers
{
    public class EspacioController : ApiController
    {
        private GestorReserva db = new GestorReserva();

        // GET: api/Espacio
        [HttpGet]
        public IEnumerable<Espacio> ObtenerEspacios()
        {
            return db.Espacios.ToList();
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
        public IHttpActionResult CrearEspacio(Espacio espacio)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar que no exista un espacio con el mismo nombre
            var espacioExistente = db.Espacios.FirstOrDefault(e => e.Nombre == espacio.Nombre);
            if (espacioExistente != null)
                return BadRequest("Ya existe un espacio con ese nombre");

            db.Espacios.Add(espacio);
            db.SaveChanges();
            return CreatedAtRoute("DefaultApi", new { id = espacio.Id }, espacio);
        }

        // PUT: api/Espacio/{id}
        [HttpPut]
        public IHttpActionResult ActualizarEspacio(int id, Espacio espacio)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != espacio.Id)
                return BadRequest();

            db.Entry(espacio).State = EntityState.Modified;
            db.SaveChanges();
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        // DELETE: api/Espacio/{id}
        [HttpDelete]
        public IHttpActionResult BorrarEspacio(int id)
        {
            var espacio = db.Espacios.Find(id);
            if (espacio == null)
                return NotFound();

            // Verificar si tiene reservas asociadas
            var tieneReservas = db.Reservas.Any(r => r.EspacioId == id);
            if (tieneReservas)
                return BadRequest("No se puede eliminar un espacio que tiene reservas asociadas");

            db.Espacios.Remove(espacio);
            db.SaveChanges();
            return Ok(espacio);
        }

        // GET: api/Espacio/tipo/{tipo}
        [HttpGet]
        [Route("api/Espacio/tipo/{tipo}")]
        public IHttpActionResult ObtenerEspaciosPorTipo(int tipo)
        {
            var espacios = db.Espacios
                .Where(e => (int)e.Tipo == tipo)
                .ToList();

            return Ok(espacios);
        }
    }
}