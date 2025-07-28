using GestorReservas.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls.WebParts;

namespace GestorReservas.DAL
{
	public class GestorReserva : DbContext
	{
        
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Espacio> Espacios { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
    }
}