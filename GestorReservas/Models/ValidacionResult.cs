using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GestorReservas.Models
{
    // Clase auxiliar para encapsular resultados de validaciones personalizadas
    // Proporciona una forma estandarizada de devolver éxito/fallo con mensaje descriptivo
    // Utilizada principalmente en métodos de validación de controladores
    // Reemplaza el uso de booleanos simples por información más rica
    public class ValidacionResult
    {
        // Indica si la validación fue exitosa o falló
        // true = validación pasó correctamente
        // false = validación falló, revisar Mensaje para detalles
        // Propiedad principal para determinar el flujo de ejecución
        public bool EsValido { get; set; }

        // Mensaje descriptivo del resultado de la validación
        // Para validaciones exitosas: mensaje de confirmación (ej: "Contraseña válida")
        // Para validaciones fallidas: descripción del error (ej: "La contraseña debe tener al menos 6 caracteres")
        // Utilizado para mostrar feedback específico al usuario o para logging
        public string Mensaje { get; set; }

        // Constructor que requiere ambos parámetros para crear instancia completa
        // Garantiza que toda validación tenga estado y mensaje asociado
        // Facilita la creación de objetos ValidacionResult en una sola línea
        // Parámetros:
        //   esValido: resultado booleano de la validación
        //   mensaje: descripción textual del resultado
        public ValidacionResult(bool esValido, string mensaje)
        {
            EsValido = esValido;
            Mensaje = mensaje;
        }
    }
}