using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace GestorReservas.Utils
{
    public static class AppConfig
    {
        public static string JwtSecretKey => ConfigurationManager.AppSettings["JwtSecretKey"];
        public static int JwtExpirationDays => int.Parse(ConfigurationManager.AppSettings["JwtExpirationDays"] ?? "7");
        public static string JwtIssuer => ConfigurationManager.AppSettings["JwtIssuer"];
        public static string JwtAudience => ConfigurationManager.AppSettings["JwtAudience"];

        /// <summary>
        /// Valida que todas las configuraciones JWT estén presentes
        /// </summary>
        public static void ValidateJwtConfiguration()
        {
            if (string.IsNullOrEmpty(JwtSecretKey))
                throw new ArgumentException("JwtSecretKey no está configurada en Web.config");

            if (JwtSecretKey.Length < 32)
                throw new ArgumentException("JwtSecretKey debe tener al menos 32 caracteres");
        }
    }
}