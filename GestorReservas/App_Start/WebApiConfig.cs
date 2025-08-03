using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors; // ← AGREGAR ESTA LÍNEA

namespace GestorReservas
{
    public static class WebApiConfig
    {
        // Método para registrar la configuración de Web API
        public static void Register(HttpConfiguration config)
        {
            // Configuración y servicios de Web API

            // *** NUEVA CONFIGURACIÓN CORS ***
            // Habilita CORS (Cross-Origin Resource Sharing) para permitir solicitudes desde cualquier origen.
            // Útil en desarrollo, pero en producción se recomienda restringir los orígenes permitidos.
            var cors = new EnableCorsAttribute(
                origins: "*",           // Permite todos los orígenes
                headers: "*",           // Permite todos los headers
                methods: "*"            // Permite todos los métodos HTTP
            );
            config.EnableCors(cors);

            // Configura la serialización de enums como cadenas de texto en las respuestas JSON.
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());
            // Formatea la salida JSON con indentación para facilitar la lectura.
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;

            // Habilita el uso de rutas basadas en atributos en los controladores.
            config.MapHttpAttributeRoutes();

            // Define la ruta por defecto para las APIs REST.
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
