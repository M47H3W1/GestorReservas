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
        public static void Register(HttpConfiguration config)
        {
            // Configuración y servicios de Web API

            // *** NUEVA CONFIGURACIÓN CORS ***
            // Habilitar CORS para todas las APIs
            var cors = new EnableCorsAttribute(
                origins: "*",           // Permitir todos los orígenes (para desarrollo)
                headers: "*",           // Permitir todos los headers
                methods: "*"            // Permitir todos los métodos HTTP
            );
            config.EnableCors(cors);

            // Configurar serialización de enums como strings
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;


            // Rutas de Web API
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
