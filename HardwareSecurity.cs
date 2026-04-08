using System.Collections.Generic;

namespace BypassCheckerWPF
{
    public class HardwareSecurity
    {
        // Propiedades del CPU (Básicas)
        public string Manufacturer { get; set; } = "Desconocido";
        public string Brand { get; set; } = "Desconocido";
        public string Generation { get; set; } = "Desconocida";
        public string Architecture { get; set; } = "Desconocida";
        public int Cores { get; set; }
        public int LogicalProcessors { get; set; }

        // Propiedades de Seguridad (Compatibilidad)
        public bool SupportsVirtualization { get; set; }
        public bool HasMBEC { get; set; }
        public bool HasGMET { get; set; }
        public string HyperVCompatibilityStatus { get; set; } = "Desconocido";
        public List<string> KnownVulnerabilities { get; set; } = new List<string>();
        public List<string> MitigationsStatus { get; set; } = new List<string>();
        public string PerformanceImpact { get; set; } = "Desconocido";

        // Propiedades Adicionales
        public string MicrocodeVersion { get; set; } = "Desconocida";
        public bool IsServerProcessor { get; set; }
        public string IntegratedGPU { get; set; } = "No detectada";
    }
}