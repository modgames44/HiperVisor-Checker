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
            DisplayBypassResults(bypassResults, systemResults); // 👈 Ahora pasamos systemResults
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
        
        
        sb.AppendLine("                     📊 ANÁLISIS DEL SISTEMA                      ");
        sb.AppendLine();
        
        sb.AppendLine("🖥️  PROCESADOR:");
        sb.AppendLine($"   • Modelo: {results.CpuName}");
        sb.AppendLine($"   • Núcleos: {results.CpuCores} | Hilos: {results.CpuThreads}");
        sb.AppendLine();
        
        // Información avanzada del CPU
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
        
        sb.AppendLine("🛡️  RIESGO ESTIMADO:");
        sb.AppendLine($"   • Nivel: {results.CpuRiskLevel}");
        sb.AppendLine($"   • {results.CpuRiskMessage}");
        sb.AppendLine();
        sb.AppendLine();
        
       
        string compatibilidad = results.IsCompatible ? "✅ SÍ ES COMPATIBLE" : "❌ NO ES COMPATIBLE";
        sb.AppendLine($"║  COMPATIBILIDAD: {compatibilidad,-35}║");
        
        
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

    private void DisplayBypassResults(BypassResults results, CheckResults systemResults)
    {
        var sb = new StringBuilder();
        
        
        sb.AppendLine("               🚀 MÉTODO HYPER VISION - DENUVO                   ");
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
        
        
        sb.AppendLine("                     📝 PASOS A SEGUIR                           ");
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
            sb.AppendLine("   ⚠️  IMPORTANTE: Desconecta INTERNET antes de comenzar        ");
            sb.AppendLine("   El método F7 es TEMPORAL - Solo dura esa sesión de arranque   ");
            
        }
        
        txtResults.Text = sb.ToString();
    }

    private void btnCompatibilityTable_Click(object sender, RoutedEventArgs e)
{
    var sb = new StringBuilder();
    

    sb.AppendLine("                                       📊 TABLA DE COMPATIBILIDAD UNIFICADA (CPU + WINDOWS)                                                             ");
    
    sb.AppendLine();
    
    // 1. Pantallazo Azul (BSOD)
    
    sb.AppendLine(" 1️⃣  PANTALLAZO AZUL (BSOD) / INESTABILIDAD GENERAL");
    sb.AppendLine();
    sb.AppendLine(" 🔴 RIESGO ALTO              🟡 RIESGO MEDIO              🟢 RIESGO BAJO                      💡 SOLUCIÓN / OBSERVACIÓN");
    sb.AppendLine();
    sb.AppendLine("  Intel: Core2, 1ª-7ª Gen    Intel: 8ª/9ª Gen           Intel: 10ª/11ª Gen         • Intel 13ª/14ª Gen: Actualizar BIOS a microcódigo 0x12B");
    sb.AppendLine("  Intel: 13ª/14ª Gen         AMD: Ryzen 3000            AMD: Ryzen 5000, 9000      • AMD Ryzen 7000/8000: Desactivar SVM en BIOS si hay reinicios");
    sb.AppendLine("  AMD: Ryzen 1000/2000       Windows 11 23H2            Windows 10 22H2            • Windows 11 24H2: Seguir guía específica para desactivar VBS");
    sb.AppendLine("  AMD: Ryzen 7000/8000                                                             • Desactivar VBS/HVCI, Secure Boot, BitLocker");
    sb.AppendLine("  Windows: 11 24H2 (VBS forzado)");
        
    sb.AppendLine();
    
    // 2. Rendimiento bajo / tirones
    
    sb.AppendLine("  2️⃣  RENDIMIENTO BAJO / TIRONES (FPS)                                                                                                          ");
    sb.AppendLine();
    sb.AppendLine("  🔴 RIESGO ALTO            🟡 RIESGO MEDIO            🟢 RIESGO BAJO             💡 SOLUCIÓN / OBSERVACIÓN                                     ");
    
    sb.AppendLine("  CPUs sin MBEC:            Intel 10ª/11ª Gen          CPUs con MBEC:             • Intel 12ª Gen: Forzar afinidad a núcleos P con Process Lasso ");
    sb.AppendLine();
    sb.AppendLine("  • Intel 8ª/9ª Gen                                    • Intel 11ª Gen+           • Desactivar E-cores en BIOS si es necesario                   ");
    sb.AppendLine("  • AMD Ryzen 3000                                     • AMD Zen 3+ (5000/7000)   • Aceptar penalización en CPUs sin MBEC                        ");
    sb.AppendLine("  Intel 12ª-14ª Gen si                                                                                                                           ");
    sb.AppendLine("  se ejecuta en E‑core                                                                                                                           ");
    
    sb.AppendLine();
    
    // 3. Fallo de arranque / pantalla negra
    
    sb.AppendLine("  3️⃣  FALLO DE ARRANQUE / PANTALLA NEGRA                                                                                                        ");
    sb.AppendLine();
    sb.AppendLine("  🔴 RIESGO ALTO            🟡 RIESGO MEDIO            🟢 RIESGO BAJO             💡 SOLUCIÓN / OBSERVACIÓN                                      ");
    
    sb.AppendLine("  Intel 12ª Gen + Win10     Windows 11 24H2            Windows 10 / 11 23H2       • Actualizar a Windows 11 o aplicar parche KB5007262 en Win10  ");
    sb.AppendLine();
    sb.AppendLine(" (sin parche scheduler)                               con configuración manual   • Desactivar Secure Boot y TPM en BIOS                         ");
    sb.AppendLine("  Secure Boot o TPM         Drivers desactualizados  │                            • Verificar flags de arranque (bcdedit)                        ");
    sb.AppendLine("  no desactivados                                                                                                                                ");
    
    sb.AppendLine();
    
    // 4. Conflictos con antivirus
    
    sb.AppendLine("  4️⃣  CONFLICTOS CON ANTIVIRUS / WINDOWS DEFENDER                                                                                               ");
    sb.AppendLine();
    sb.AppendLine("  🔴 RIESGO ALTO (Todos los sistemas)                                              💡 SOLUCIÓN                                                   ");
    
    sb.AppendLine("  El hipervisor opera en Ring -1 y es detectado como amenaza                       • Añadir carpeta del juego a exclusiones de Windows Defender ");
    sb.AppendLine("                                                                                   • Desactivar temporalmente el antivirus durante la sesión   ");
    sb.AppendLine();
    
    // 5. BitLocker / Windows Hello
    
    sb.AppendLine("  5️⃣  PROBLEMAS CON BITLOCKER / WINDOWS HELLO                                                                                                    ");
    sb.AppendLine();
    sb.AppendLine("  🔴 RIESGO ALTO            🟡 RIESGO MEDIO            🟢 RIESGO BAJO             💡 SOLUCIÓN / OBSERVACIÓN                                       ");
    sb.AppendLine();
    sb.AppendLine("  Windows 11 + BitLocker    Windows 10 + BitLocker     Sin BitLocker /            • Desactivar BitLocker ANTES de usar el método                 ");
    sb.AppendLine("  activo (TPM 2.0)                                     contraseña local           • Desactivar Windows Hello (PIN/huella)                        ");
    sb.AppendLine();
    
    // 6. Inestabilidad por múltiples hipervisores
    
    sb.AppendLine("  6️⃣  INESTABILIDAD POR MÚLTIPLES HIPERVISORES                                                                                                   ");
    sb.AppendLine();
    sb.AppendLine("  🔴 RIESGO ALTO                 🟡 RIESGO MEDIO            🟢 RIESGO BAJO             💡 SOLUCIÓN / OBSERVACIÓN                             ");
    sb.AppendLine();
    sb.AppendLine("  AMD Ryzen 7000/8000 con VBS    Intel 6ª-10ª Gen           CPUs modernos con         • Desactivar Hyper-V, Credential Guard, Device Guard ");
    sb.AppendLine("  o Hyper-V activos              (virtualización anidada    Hyper-V desactivado       • Usar comando: bcdedit /set hypervisorlaunchtype off ");
    sb.AppendLine("  Intel 13ª/14ª Gen con VBS      limitada)                                            • Desactivar VBS desde registro o grupo de políticas  ");
    sb.AppendLine("  activo                                                                                                                                    ");
   
    sb.AppendLine();
    
    // 7. Detección por Denuvo / Anti-Cheat
    
    sb.AppendLine("  7️⃣  DETECCIÓN POR DENUVO / ANTI-CHEAT                                                                                                         ");
    sb.AppendLine();
    sb.AppendLine("  🔴 RIESGO ALTO                   🟡 RIESGO MEDIO           🟢 RIESGO BAJO            💡 SOLUCIÓN / OBSERVACIÓN                        ");
    sb.AppendLine();
    sb.AppendLine("  CPUs muy nuevas:                 CPUs conocidas:            CPUs emuladas (QEMU        • Usar bypass actualizado que oculte CPUID/MSRs ");
    sb.AppendLine();
    sb.AppendLine("  • Intel Core Ultra 200           • Intel 12ª-14ª Gen        host passthrough)          • No usar en juegos online con anticheat        ");
    sb.AppendLine("  • AMD Ryzen 9000                 • AMD Zen 3-4                                         kernel (EAC, BattlEye, Vanguard)               ");
    sb.AppendLine("  (nuevas comprobaciones de                                                              • Modo offline siempre                           ");
    sb.AppendLine("  CPUID / instrucciones)                                                                                                                 ");
    sb.AppendLine();
    
    // Soluciones generales
    sb.AppendLine("  🔧 SOLUCIONES GENERALES (aplican a todos los casos)                                                                                          ");
    sb.AppendLine("• ✅ Desconectar internet antes de iniciar el método HyperVision.");
    sb.AppendLine("• ✅ Ejecutar PowerShell como Administrador.");
    sb.AppendLine("• ✅ Desactivar VBS, HVCI, Secure Boot, BitLocker y Windows Hello.");
    sb.AppendLine("• ✅ Al reiniciar, presionar F7 para deshabilitar firma de controladores.");
    sb.AppendLine("• ✅ Revertir cambios con opción 4 del script HyperVision al terminar.");
    
    txtResults.Text = sb.ToString();
    lblStatus.Text = "📋 Tabla de compatibilidad con soluciones mostrada";
}

private async void btnDisableAll_Click(object sender, RoutedEventArgs e)
{
    var confirm = MessageBox.Show(
        "⚠️ ADVERTENCIA\n\n" +
        "Esta acción desactivará las siguientes protecciones:\n" +
        "• VBS, HVCI, Windows Hello, Credential Guard, etc.\n\n" +
        "Esto puede reducir la seguridad del sistema.\n\n" +
        "¿Deseas continuar?",
        "Confirmar desactivación",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

    if (confirm != MessageBoxResult.Yes) return;

    btnDisableAll.IsEnabled = false;
    lblStatus.Text = "🔄 Desactivando protecciones...";
    txtResults.Text = "";

    try
    {
        var (success, message) = await Task.Run(() => _checker.DisableAllFeatures());
        MessageBox.Show(message, success ? "Éxito" : "Error", MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Error);

        if (success)
        {
            // Preguntar si desea reiniciar
            var reboot = MessageBox.Show(
                "La desactivación se ha completado. ¿Deseas reiniciar el equipo ahora para aplicar los cambios?\n\n" +
                "El reinicio se realizará en modo avanzado (presiona F7 al iniciar).",
                "Reinicio necesario",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (reboot == MessageBoxResult.Yes)
            {
                _checker.SetOneTimeAdvancedBoot();
                _checker.RebootSystem();
            }
            else
            {
                lblStatus.Text = "✅ Protecciones desactivadas (reinicio pendiente)";
            }

            // Refrescar la vista (opcional)
            var newResults = await _checker.CheckAllRequirementsAsync();
            DisplayResults(newResults);
            UpdateStats(newResults);
        }
        else
        {
            lblStatus.Text = "❌ Error al desactivar";
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        lblStatus.Text = "❌ Error";
    }
    finally
    {
        btnDisableAll.IsEnabled = true;
    }
}

private void btnEnableAll_Click(object sender, RoutedEventArgs e)
{
    var confirm = MessageBox.Show(
        "🔒 ACTIVAR MODO MÁXIMA SEGURIDAD\n\n" +
        "Esta acción activará todas las protecciones de Windows:\n" +
        "• VBS (Virtualization-based Security)\n" +
        "• HVCI (Memory Integrity)\n" +
        "• Windows Hello Protection\n" +
        "• Enhanced Sign-in Security\n" +
        "• System Guard Secure Launch\n" +
        "• Credential Guard\n" +
        "• KVA Shadow\n" +
        "• Windows Hypervisor\n\n" +
        "Esto puede mejorar la seguridad del sistema pero podría afectar el rendimiento en juegos.\n\n" +
        "¿Deseas continuar?",
        "Confirmar activación",
        MessageBoxButton.YesNo,
        MessageBoxImage.Information);

    if (confirm == MessageBoxResult.Yes)
    {
        var (success, message) = _checker.EnableAllFeatures();
        MessageBox.Show(message, success ? "Éxito" : "Error", MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Error);
        if (success)
        {
            // Refrescar la vista
            _ = Task.Run(async () =>
            {
                var newResults = await _checker.CheckAllRequirementsAsync();
                Dispatcher.Invoke(() => DisplayResults(newResults));
            });
        }
    }
}

private async void btnRevert_Click(object sender, RoutedEventArgs e)
{
    var confirm = MessageBox.Show(
        "Esta acción restaurará la configuración de seguridad guardada en el último backup.\n\n¿Deseas continuar?",
        "Revertir cambios",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

    if (confirm != MessageBoxResult.Yes) return;

    btnRevert.IsEnabled = false;
    lblStatus.Text = "🔄 Restaurando configuración...";
    txtResults.Text = "";

    try
    {
        var (success, message) = await Task.Run(() => _checker.RestoreFromBackup());
        MessageBox.Show(message, success ? "Éxito" : "Error", MessageBoxButton.OK,
                        success ? MessageBoxImage.Information : MessageBoxImage.Error);

        if (success)
        {
            // Preguntar si desea reiniciar en modo normal
            var reboot = MessageBox.Show(
                "La restauración se ha completado. ¿Deseas reiniciar el equipo ahora para aplicar los cambios?",
                "Reinicio necesario",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (reboot == MessageBoxResult.Yes)
            {
                _checker.RebootSystem(); // Reinicio normal (sin modo avanzado)
            }
            else
            {
                lblStatus.Text = "✅ Configuración restaurada (reinicio pendiente)";
            }

            // Refrescar vista
            var newResults = await _checker.CheckAllRequirementsAsync();
            DisplayResults(newResults);
            UpdateStats(newResults);
        }
        else
        {
            lblStatus.Text = "❌ Error al restaurar";
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        lblStatus.Text = "❌ Error";
    }
    finally
    {
        btnRevert.IsEnabled = true;
    }
}

private void btnF7Reboot_Click(object sender, RoutedEventArgs e)
{
    var result = MessageBox.Show(
        "⚠️ Reinicio en modo avanzado (F7)\n\n" +
        "Esta acción configurará el equipo para que al reiniciar muestre el menú de opciones avanzadas.\n" +
        "Allí deberás presionar la tecla F7 para deshabilitar la firma obligatoria de controladores.\n\n" +
        "¿Deseas continuar y reiniciar ahora?",
        "Confirmar reinicio F7",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

    if (result == MessageBoxResult.Yes)
    {
        _checker.SetOneTimeAdvancedBoot();
        _checker.RebootSystem();
    }
}

private void btnShowStatus_Click(object sender, RoutedEventArgs e)
{
    txtResults.Text = "";
    lblStatus.Text = "📊 Obteniendo estado detallado...";
    try
    {
        string status = _checker.GetSystemStatus();
        txtResults.Text = status;
        lblStatus.Text = "✅ Estado actualizado";
    }
    catch (Exception ex)
    {
        txtResults.Text = $"Error: {ex.Message}";
        lblStatus.Text = "❌ Error al obtener estado";
    }
}

private void btnCustomize_Click(object sender, RoutedEventArgs e)
{
    var customizeWindow = new CustomizeWindow(_checker);
    customizeWindow.Owner = this;
    customizeWindow.ShowDialog();
    
    // No se refresca automáticamente
}

}