
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace BypassCheckerWPF;

public partial class MainWindow : Window
{
    private SystemChecker _checker;

    public MainWindow()
    {
        InitializeComponent();
        _checker = new SystemChecker();
    }

    private async void btnCheck_Click(object sender, RoutedEventArgs e)
    {
        btnCheck.IsEnabled = false;
        lblStatus.Text = "🔄 Verificando sistema...";
        txtResults.Text = "";
        
        try
        {
            var results = await Task.Run(() => _checker.CheckAllRequirementsAsync());
            DisplayResults(results);
            UpdateStats(results);
            lblStatus.Text = "✅ Verificación completada";
        }
        catch (Exception ex)
        {
            txtResults.Text = $"❌ Error: {ex.Message}";
            lblStatus.Text = "❌ Error en la verificación";
        }
        finally
        {
            btnCheck.IsEnabled = true;
        }
    }

    private async void btnBypass_Click(object sender, RoutedEventArgs e)
    {
        btnBypass.IsEnabled = false;
        lblStatus.Text = "🔄 Verificando métodos de bypass...";
        txtResults.Text = "";
        
        try
        {
            var systemResults = await Task.Run(() => _checker.CheckAllRequirementsAsync());
            var bypassResults = _checker.GetBypassMethods(systemResults);
            DisplayBypassResults(bypassResults);
            lblStatus.Text = "✅ Análisis de bypass completado";
        }
        catch (Exception ex)
        {
            txtResults.Text = $"❌ Error: {ex.Message}";
            lblStatus.Text = "❌ Error en la verificación";
        }
        finally
        {
            btnBypass.IsEnabled = true;
        }
    }

    private void btnClear_Click(object sender, RoutedEventArgs e)
    {
        txtResults.Text = "";
        lblStatus.Text = "✅ Listo para verificar";
    }

    private void UpdateStats(CheckResults results)
    {
        // Actualizar estado de virtualización
        if (results.IsVirtualizationEnabled)
        {
            lblVirtStatus.Text = "✅ Activada";
            lblVirtStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
        }
        else
        {
            lblVirtStatus.Text = "❌ Desactivada";
            lblVirtStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
        }
        
        // Actualizar estado VBS/HVCI
        if (results.IsVbsEnabled || results.IsHvciEnabled)
        {
            lblVbsStatus.Text = "⚠️ Activo";
            lblVbsStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
        }
        else
        {
            lblVbsStatus.Text = "✅ Inactivo";
            lblVbsStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
        }
        
        // Actualizar compatibilidad
        if (results.IsCompatible)
        {
            lblCompatStatus.Text = "✅ Compatible";
            lblCompatStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
        }
        else
        {
            lblCompatStatus.Text = "❌ No compatible";
            lblCompatStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
        }
    }

    private void DisplayResults(CheckResults results)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                    📊 ANÁLISIS DEL SISTEMA                     ║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        
        sb.AppendLine("🖥️  PROCESADOR:");
        sb.AppendLine($"   • Modelo: {results.CpuName}");
        sb.AppendLine($"   • Núcleos: {results.CpuCores} | Hilos: {results.CpuThreads}");
        sb.AppendLine();
        
        // Añade esto dentro del método DisplayResults, después de la sección de HYPERVISION
        sb.AppendLine("🖥️  INFORMACIÓN AVANZADA DEL CPU:");
        sb.AppendLine($"   • Marca: {results.HardwareSecurityInfo.Manufacturer}");
        sb.AppendLine($"   • Modelo: {results.HardwareSecurityInfo.Brand}");
        sb.AppendLine($"   • Generación: {results.HardwareSecurityInfo.Generation}");
        sb.AppendLine($"   • Núcleos: {results.HardwareSecurityInfo.Cores} | Hilos Lógicos: {results.HardwareSecurityInfo.LogicalProcessors}");
        sb.AppendLine($"   • Arquitectura: {results.HardwareSecurityInfo.Architecture}");
        sb.AppendLine($"   • Versión de Microcódigo: {results.HardwareSecurityInfo.MicrocodeVersion}");
        sb.AppendLine();
        sb.AppendLine("🛡️  SEGURIDAD Y RENDIMIENTO:");
        sb.AppendLine($"   • Soporte de Virtualización: {(results.HardwareSecurityInfo.SupportsVirtualization ? "✅ Activado" : "❌ Desactivado")}");
        sb.AppendLine($"   • Soporte MBEC: {(results.HardwareSecurityInfo.HasMBEC ? "✅ Sí (Rendimiento optimizado)" : "❌ No (Posible penalización de rendimiento)")}");
        sb.AppendLine($"   • Impacto Estimado con VBS: {results.HardwareSecurityInfo.PerformanceImpact}");
        sb.AppendLine($"   • Compatibilidad con Hyper-V: {results.HardwareSecurityInfo.HyperVCompatibilityStatus}");
        sb.AppendLine();
        sb.AppendLine("⚠️  VULNERABILIDADES CONOCIDAS:");
        if (results.HardwareSecurityInfo.KnownVulnerabilities.Count > 0)
            foreach (var vuln in results.HardwareSecurityInfo.KnownVulnerabilities)
                sb.AppendLine($"   • {vuln}");
        else
        sb.AppendLine("   • No se detectaron vulnerabilidades conocidas relevantes.");
        sb.AppendLine();
        
        sb.AppendLine("💾 MEMORIA Y ALMACENAMIENTO:");
        sb.AppendLine($"   • RAM: {results.RamGB} GB");
        sb.AppendLine($"   • Disco libre: {results.DiskSpaceGB} GB");
        sb.AppendLine();
        
        sb.AppendLine("🔒 SEGURIDAD:");
        sb.AppendLine($"   • TPM: {(results.TpmPresent ? "✅ Presente" : "❌ No presente")}");
        sb.AppendLine($"   • Secure Boot: {(results.SecureBootEnabled ? "✅ Activado" : "❌ Desactivado")}");
        sb.AppendLine($"   • UEFI: {(results.UefiMode ? "✅ Modo UEFI" : "❌ Modo Legacy")}");
        sb.AppendLine();
        
        sb.AppendLine("🛡️  HYPERVISION:");
        sb.AppendLine($"   • Virtualización (VT-x/AMD-V): {(results.IsVirtualizationEnabled ? "✅ Activada" : "❌ Desactivada")}");
        sb.AppendLine($"   • VBS: {(results.IsVbsEnabled ? "⚠️ Activado" : "✅ Desactivado")}");
        sb.AppendLine($"   • HVCI: {(results.IsHvciEnabled ? "⚠️ Activado" : "✅ Desactivado")}");
        sb.AppendLine($"   • BitLocker: {(results.IsBitLockerActive ? "⚠️ Activo" : "✅ Inactivo")}");
        sb.AppendLine();
        
        sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        string compatibilidad = results.IsCompatible ? "✅ SÍ ES COMPATIBLE" : "❌ NO ES COMPATIBLE";
        sb.AppendLine($"║  COMPATIBILIDAD: {compatibilidad,-35}║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        
        if (!results.IsCompatible && results.MissingRequirements.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("⚠️  REQUISITOS NO CUMPLIDOS:");
            foreach (var missing in results.MissingRequirements)
            {
                sb.AppendLine($"   • {missing}");
            }
        }
        
        txtResults.Text = sb.ToString();
    }

    private void DisplayBypassResults(BypassResults results)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║              🚀 MÉTODO HYPER VISION - DENUVO                  ║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        
        sb.AppendLine("📋 DIAGNÓSTICO:");
        sb.AppendLine($"   • Estado: {(results.CanDisableVbs ? "✅ SISTEMA COMPATIBLE" : "❌ SISTEMA NO COMPATIBLE")}");
        sb.AppendLine($"   • Virtualización: {results.VbsRequirementStatus}");
        sb.AppendLine($"   • UEFI Lock: {results.UefiLockStatus}");
        sb.AppendLine($"   • BitLocker: {results.BitLockerStatus}");
        sb.AppendLine();
        
        if (results.IncompatibleDriversDetected)
        {
            sb.AppendLine("⚠️  DRIVERS INCOMPATIBLES DETECTADOS:");
            foreach (var driver in results.IncompatibleDriversList)
            {
                sb.AppendLine($"   • {driver}");
            }
            sb.AppendLine();
        }
        
        sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                    📝 PASOS A SEGUIR                          ║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        
        int stepNum = 1;
        foreach (var step in results.HyperVisionSteps)
        {
            sb.AppendLine($" {stepNum}. {step}");
            stepNum++;
        }
        
        if (results.RecommendsF7Method)
        {
            sb.AppendLine();
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║  ⚠️  IMPORTANTE: Desconecta INTERNET antes de comenzar        ║");
            sb.AppendLine("║  El método F7 es TEMPORAL - Solo dura esa sesión de arranque  ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        }
        
        txtResults.Text = sb.ToString();
    }
}
