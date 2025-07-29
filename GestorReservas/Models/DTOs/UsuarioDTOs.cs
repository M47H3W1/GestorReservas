using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using GestorReservas.DAL;
using GestorReservas.Models;
using GestorReservas.Models.DTOs;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace GestorReservas.Controllers
{
    public class UsuarioRegistroDto
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public RolUsuario Rol { get; set; }
    }

    public class UsuarioActualizarDto
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string NuevoPassword { get; set; }
        public RolUsuario? Rol { get; set; }
    }

    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; }
        public System.DateTime FechaCreacion { get; set; }
    }
}

