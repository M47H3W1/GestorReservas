using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace GestorReservas.Utils
{
    // Clase estática que centraliza la configuración de la aplicación
    // Proporciona acceso tipado y validado a los valores del Web.config
    // Evita repetir código de ConfigurationManager.AppSettings en múltiples lugares
    // Facilita el mantenimiento y modificación de configuraciones
    public static class AppConfig
    {
        // Clave secreta utilizada para firmar y verificar tokens JWT
        // Obtenida desde appSettings del Web.config con key "JwtSecretKey"
        // CRÍTICA para la seguridad: debe ser suficientemente larga y compleja
        // Se valida que exista y tenga mínimo 32 caracteres en ValidateJwtConfiguration()
        public static string JwtSecretKey => ConfigurationManager.AppSettings["JwtSecretKey"];

        // Número de días que permanece válido un token JWT antes de expirar
        // Valor por defecto: 7 días si no se especifica en Web.config
        // Se convierte de string a int automáticamente
        // Utilizado para calcular la fecha de expiración al generar tokens
        public static int JwtExpirationDays => int.Parse(ConfigurationManager.AppSettings["JwtExpirationDays"] ?? "7");

        // Emisor (issuer) del token JWT para validación de origen
        // Identifica quién generó el token (generalmente el nombre de la aplicación)
        // Obtenido desde appSettings con key "JwtIssuer"
        // Utilizado en la validación de tokens para confirmar que fueron emitidos por esta aplicación
        public static string JwtIssuer => ConfigurationManager.AppSettings["JwtIssuer"];

        // Audiencia (audience) del token JWT para validación de destino
        // Identifica para quién está destinado el token (generalmente la URL de la API)
        // Obtenido desde appSettings con key "JwtAudience"
        // Utilizado en la validación de tokens para confirmar que están destinados a esta aplicación
        public static string JwtAudience => ConfigurationManager.AppSettings["JwtAudience"];

        // Método de validación que verifica que todas las configuraciones JWT críticas estén presentes
        // Debe llamarse al inicio de la aplicación (Global.asax Application_Start)
        // Lanza excepciones descriptivas si alguna configuración es inválida
        // Previene errores en tiempo de ejecución por configuraciones faltantes
        public static void ValidateJwtConfiguration()
        {
            // Verificar que la clave secreta esté configurada
            // Sin esta clave no se pueden generar ni validar tokens JWT
            if (string.IsNullOrEmpty(JwtSecretKey))
                throw new ArgumentException("JwtSecretKey no está configurada en Web.config");

            // Verificar que la clave secreta tenga longitud suficiente para seguridad
            // Claves cortas son vulnerables a ataques de fuerza bruta
            // 32 caracteres es el mínimo recomendado para algoritmos HMAC-SHA256
            if (JwtSecretKey.Length < 32)
                throw new ArgumentException("JwtSecretKey debe tener al menos 32 caracteres");

            // VALIDACIONES ADICIONALES QUE SE PODRÍAN AGREGAR:
            /*
            if (string.IsNullOrEmpty(JwtIssuer))
                throw new ArgumentException("JwtIssuer no está configurado en Web.config");

            if (string.IsNullOrEmpty(JwtAudience))
                throw new ArgumentException("JwtAudience no está configurado en Web.config");

            if (JwtExpirationDays <= 0)
                throw new ArgumentException("JwtExpirationDays debe ser mayor a 0");
            */
        }
    }
}