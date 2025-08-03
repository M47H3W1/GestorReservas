using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GestorReservas.Models
{
    public class JwtUserInfo
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}
