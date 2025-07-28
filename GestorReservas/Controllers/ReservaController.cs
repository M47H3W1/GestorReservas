using System.Web.Http;
using System.Data.Entity;
using GestorReservas.DAL;
using GestorReservas.Models;
using System.Collections.Generic;
using System.Linq;

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
    }
}
