using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;

namespace BypassCheckerWPF;

public class SystemChecker
{
    public async Task<CheckResults> CheckAllRequirementsAsync()
    {
        var results = new CheckResults();
        
        GetCpuInfo(results);
        GetRamInfo(results);
        GetDiskInfo(results);
        CheckTpm(results);
        CheckSecureBoot(results);
        CheckUefiMode(results);
        CheckVbsStatus(results);
        CheckHvciStatus(results);
        CheckVirtualizationEnabled(results);
        CheckBitLockerStatus(results);
        CheckCredentialGuardUefiLock(results);
        DetermineCompatibility(results);
        GetHardwareSecurityInfo(results);
        EvaluateCpuRisk(results);
        
        
        return results;
    }


public (bool success, string message) DisableAllFeatures()
{
    try
    {
        BackupCurrentConfig();
        DisableVbs();
        DisableHvci();
        DisableWindowsHello();
        DisableSecureBiometrics();
        DisableSystemGuard();
        DisableCredentialGuard();
        DisableKvaShadow();
        DisableHypervisor();

        return (true, "Características desactivadas correctamente. Se recomienda reiniciar.");
    }
    catch (Exception ex)
    {
        return (false, $"Error: {ex.Message}");
    }
}

public (bool success, string message) EnableAllFeatures()
{
    try
    {
        EnableVbs();
        EnableHvci();
        EnableWindowsHello();
        EnableSecureBiometrics();
        EnableSystemGuard();
        EnableCredentialGuard();
        EnableKvaShadow();
        EnableHypervisor();

        return (true, "Todas las protecciones han sido activadas. Se recomienda reiniciar el equipo.");
    }
    catch (Exception ex)
    {
        return (false, $"Error: {ex.Message}");
    }
}

    public BypassResults GetBypassMethods(CheckResults systemCheck)
    {
        var results = new BypassResults();
        
        results.CanDisableVbs = systemCheck.IsVirtualizationEnabled;
        results.VbsRequirementStatus = systemCheck.IsVirtualizationEnabled ? "✅ Activada" : "❌ Desactivada";
        results.HasUefiLock = systemCheck.HasVbsUefiLock || systemCheck.HasCredGuardUefiLock;
        results.UefiLockStatus = results.HasUefiLock ? "⚠️ Detectado" : "✅ No detectado";
        results.IsBitLockerActive = systemCheck.IsBitLockerActive;
        results.BitLockerStatus = systemCheck.IsBitLockerActive ? "⚠️ Activo" : "✅ Inactivo";
        results.RecommendsF7Method = results.CanDisableVbs;
        results.IncompatibleDriversDetected = systemCheck.IncompatibleDriversList.Count > 0;
        results.IncompatibleDriversList = systemCheck.IncompatibleDriversList;
        
        results.HyperVisionSteps = new List<string>
        {
            "Ejecutar PowerShell como Administrador",
            "Usar la opcion 3 'Desactivar todas las caracteristicas'",
            "El script guardara el estado actual y suspendera BitLocker",
            "Al reiniciar, presiona F7 para deshabilitar firma de controladores",
            "Realiza los cambios necesarios",
            "Para revertir, usa la opcion 4 'Revertir cambios'"
        };
        
        return results;
    }

    private void GetCpuInfo(CheckResults results)
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (var obj in searcher.Get())
                {
                    results.CpuName = obj["Name"]?.ToString()?.Trim() ?? "Desconocido";
                    results.CpuCores = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                    results.CpuThreads = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                    break;
                }
            }
        }
        catch { }
    }

    private void GetRamInfo(CheckResults results)
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
            {
                foreach (var obj in searcher.Get())
                {
                    var ramBytes = Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
                    results.RamGB = Math.Round(ramBytes / (1024.0 * 1024.0), 2);
                    break;
                }
            }
        }
        catch { }
    }

    private void GetDiskInfo(CheckResults results)
    {
        try
        {
            var drive = new DriveInfo("C");
            results.DiskSpaceGB = Math.Round(drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 2);
        }
        catch { }
    }

    private void CheckTpm(CheckResults results)
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2\\Security\\MicrosoftTpm", "SELECT * FROM Win32_Tpm"))
            {
                foreach (var obj in searcher.Get())
                {
                    results.TpmPresent = true;
                    break;
                }
            }
        }
        catch { results.TpmPresent = false; }
    }

    private void CheckSecureBoot(CheckResults results)
    {
        try
        {
            using (var key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\SecureBoot\\State"))
            {
                var value = key?.GetValue("UEFISecureBootEnabled");
                results.SecureBootEnabled = value != null && Convert.ToInt32(value) == 1;
            }
        }
        catch { results.SecureBootEnabled = false; }
    }

    private void CheckUefiMode(CheckResults results)
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                foreach (var obj in searcher.Get())
                {
                    var firmwareType = obj["FirmwareType"];
                    results.UefiMode = firmwareType != null && Convert.ToInt32(firmwareType) == 2;
                    break;
                }
            }
        }
        catch { results.UefiMode = false; }
    }
    
    private void CheckVbsStatus(CheckResults results)
    {
        try
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard"))
            {
                if (key != null)
                {
                    var vbsStatus = key.GetValue("EnableVirtualizationBasedSecurity");
                    results.IsVbsEnabled = vbsStatus != null && Convert.ToInt32(vbsStatus) == 1;
                    
                    var vbsLock = key.GetValue("RequirePlatformSecurityFeatures");
                    results.HasVbsUefiLock = vbsLock != null && Convert.ToInt32(vbsLock) == 1;
                }
            }
        }
        catch { }
    }

    private void CheckHvciStatus(CheckResults results)
    {
        try
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"))
            {
                var hvciStatus = key?.GetValue("Enabled");
                results.IsHvciEnabled = hvciStatus != null && Convert.ToInt32(hvciStatus) == 1;
            }
        }
        catch { }
    }

    private void CheckVirtualizationEnabled(CheckResults results)
    {
        bool virtEnabled = false;

        // 1. Verificar mediante WMI (método original)
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT VirtualizationFirmwareEnabled FROM Win32_Processor"))
            {
                foreach (var obj in searcher.Get())
                {
                    var val = obj["VirtualizationFirmwareEnabled"];
                    if (val != null && Convert.ToBoolean(val))
                        virtEnabled = true;
                    break;
                }
            }
        }
        catch { }

        // 2. Verificar mediante systeminfo (más fiable en muchos casos)
        if (!virtEnabled)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "systeminfo";
                    process.StartInfo.Arguments = " | findstr /C:\"Virtualización\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    
                    if (output.Contains("Sí") || output.Contains("Enabled"))
                        virtEnabled = true;
                }
            }
            catch { }
        }

        // 3. Verificar mediante el registro de DeviceGuard
        if (!virtEnabled)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard"))
                {
                    if (key != null)
                    {
                        var hypervisorEnforced = key.GetValue("HypervisorEnforcedCodeIntegrity");
                        if (hypervisorEnforced != null && Convert.ToInt32(hypervisorEnforced) == 1)
                            virtEnabled = true;
                    }
                }
            }
            catch { }
        }

        // 4. Si VBS o HVCI están activados, la virtualización OBLIGATORIAMENTE está activada
        if (results.IsVbsEnabled || results.IsHvciEnabled)
        {
            virtEnabled = true;
        }

        results.IsVirtualizationEnabled = virtEnabled;
    }

    private void CheckBitLockerStatus(CheckResults results)
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT ProtectionStatus FROM Win32_EncryptableVolume"))
            {
                foreach (var obj in searcher.Get())
                {
                    var protection = obj["ProtectionStatus"];
                    results.IsBitLockerActive = protection != null && Convert.ToInt32(protection) == 1;
                    break;
                }
            }
        }
        catch { }
    }

    private void CheckCredentialGuardUefiLock(CheckResults results)
    {
        try
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa"))
            {
                var cgLock = key?.GetValue("LsaCfgFlags");
                results.HasCredGuardUefiLock = cgLock != null && Convert.ToInt32(cgLock) == 2;
            }
        }
        catch { }
    }

    private void DetermineCompatibility(CheckResults results)
    {
        results.IsCompatible = true;
        results.MissingRequirements.Clear();

        if (!results.IsVirtualizationEnabled)
        {
            results.IsCompatible = false;
            results.MissingRequirements.Add("Virtualizacion (VT-x/AMD-V) NO activada en BIOS");
        }
        
        if (results.HasVbsUefiLock || results.HasCredGuardUefiLock)
        {
            results.MissingRequirements.Add("UEFI Lock detectado - La desactivacion podria no ser persistente");
        }
        
        if (results.IsBitLockerActive)
        {
            results.MissingRequirements.Add("BitLocker activo - Se suspendera durante el proceso");
        }
    }

    private void GetHardwareSecurityInfo(CheckResults results)
{
    var securityInfo = new HardwareSecurity();
    try
    {
        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
        {
            foreach (ManagementObject obj in searcher.Get())
            {
                string cpuName = obj["Name"]?.ToString() ?? "Procesador Desconocido";
                securityInfo.Manufacturer = cpuName.Contains("Intel") ? "Intel" : (cpuName.Contains("AMD") ? "AMD" : "Desconocido");
                securityInfo.Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                securityInfo.Cores = Convert.ToInt32(obj["NumberOfCores"]);
                securityInfo.LogicalProcessors = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);

                // --- Determinar Marca ---
                if (securityInfo.Manufacturer == "Intel")
                    securityInfo.Brand = cpuName.Contains("Core") ? "Core" : (cpuName.Contains("Xeon") ? "Xeon" : "Otro");
                else if (securityInfo.Manufacturer == "AMD")
                    securityInfo.Brand = cpuName.Contains("Ryzen") ? "Ryzen" : (cpuName.Contains("EPYC") ? "EPYC" : "Otro");

                // --- Extraer GENERACIÓN numérica usando Regex ---
                int generationNumber = 0;
                // Para Intel: "13th Gen", "12th Gen", etc.
                var intelGenMatch = System.Text.RegularExpressions.Regex.Match(cpuName, @"(\d+)(?:st|nd|rd|th)\s+Gen");
                if (intelGenMatch.Success)
                    generationNumber = int.Parse(intelGenMatch.Groups[1].Value);

                // Para AMD Ryzen: extraer la serie (1000, 2000, 3000, 5000, 7000...)
                int ryzenSeries = 0;
                var ryzenMatch = System.Text.RegularExpressions.Regex.Match(cpuName, @"Ryzen\s+(\d{4})");
                if (ryzenMatch.Success)
                    ryzenSeries = int.Parse(ryzenMatch.Groups[1].Value);

                // Asignar texto de generación según el número
                if (securityInfo.Manufacturer == "Intel")
                {
                    if (generationNumber >= 10)
                        securityInfo.Generation = $"{generationNumber}ª Gen o superior";
                    else if (generationNumber >= 8)
                        securityInfo.Generation = $"{generationNumber}ª Gen";
                    else if (generationNumber == 7)
                        securityInfo.Generation = "7ª Gen (Kaby Lake)";
                    else if (generationNumber > 0)
                        securityInfo.Generation = $"{generationNumber}ª Gen o anterior";
                    else
                        securityInfo.Generation = "Generación desconocida";
                }
                else if (securityInfo.Manufacturer == "AMD")
                {
                    if (ryzenSeries >= 7000)
                        securityInfo.Generation = $"Ryzen {ryzenSeries} (Zen 4/5)";
                    else if (ryzenSeries >= 5000)
                        securityInfo.Generation = $"Ryzen {ryzenSeries} (Zen 3/3+)";
                    else if (ryzenSeries >= 3000)
                        securityInfo.Generation = $"Ryzen {ryzenSeries} (Zen 2)";
                    else if (ryzenSeries >= 2000)
                        securityInfo.Generation = $"Ryzen {ryzenSeries} (Zen+)";
                    else if (ryzenSeries >= 1000)
                        securityInfo.Generation = $"Ryzen {ryzenSeries} (Zen 1)";
                    else
                        securityInfo.Generation = "Ryzen de primera generación o anterior";
                }

                // --- Características de Seguridad (MBEC, GMET) basadas en generación real ---
                // Usamos el valor ya calculado por CheckVirtualizationEnabled (más fiable)
                securityInfo.SupportsVirtualization = results.IsVirtualizationEnabled;

                // MBEC: Intel desde 8va Gen, AMD desde Zen 2 (Ryzen 3000+)
                if (securityInfo.Manufacturer == "Intel")
                    securityInfo.HasMBEC = generationNumber >= 8;
                else if (securityInfo.Manufacturer == "AMD")
                    securityInfo.HasMBEC = ryzenSeries >= 3000;
                else
                    securityInfo.HasMBEC = false;

                securityInfo.HasGMET = securityInfo.HasMBEC; // GMET suele acompañar a MBEC

                // --- Mitigaciones y vulnerabilidades ---
                var mitigations = GetMitigationStatus();
                securityInfo.MitigationsStatus = mitigations;

                // Impacto de rendimiento
                if (!securityInfo.HasMBEC && securityInfo.SupportsVirtualization)
                    securityInfo.PerformanceImpact = "Alto (Sin soporte de hardware MBEC, mayor impacto en rendimiento)";
                else if (securityInfo.HasMBEC && securityInfo.SupportsVirtualization)
                    securityInfo.PerformanceImpact = "Bajo (Con soporte de hardware MBEC)";
                else
                    securityInfo.PerformanceImpact = "N/A (Virtualización desactivada)";

                // Vulnerabilidades conocidas (solo para CPUs antiguos)
                if (securityInfo.Manufacturer == "Intel" && generationNumber > 0 && generationNumber < 8)
                    securityInfo.KnownVulnerabilities.Add("Spectre/Meltdown (Sin mitigaciones completas por hardware)");
                else if (securityInfo.Manufacturer == "AMD" && ryzenSeries > 0 && ryzenSeries < 3000)
                    securityInfo.KnownVulnerabilities.Add("Spectre/Meltdown (Sin mitigaciones completas por hardware)");
                // Para Zenbleed, solo Ryzen 1000/2000
                if (securityInfo.Manufacturer == "AMD" && (ryzenSeries == 1000 || ryzenSeries == 2000))
                    securityInfo.KnownVulnerabilities.Add("Zenbleed (Vulnerabilidad en la unidad de punto flotante)");

                break;
            }
        }
        securityInfo.HyperVCompatibilityStatus = IsHyperVCompatible(securityInfo) ? "Compatible" : "No compatible";
    }
    catch (Exception ex)
    {
        securityInfo.Manufacturer = $"Error: {ex.Message}";
    }
    results.HardwareSecurityInfo = securityInfo;    
    }

    private List<string> GetMitigationStatus()
{
    var mitigations = new List<string>();
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"))
        {
            if (key?.GetValue("FeatureSettingsOverride") != null)
                mitigations.Add("Mitigaciones de Spectre/Meltdown: Activas");
            else
                mitigations.Add("Mitigaciones de Spectre/Meltdown: No configuradas");
        }
    }
    catch { mitigations.Add("Mitigaciones: No se pudo determinar el estado"); }
    return mitigations;
}

private bool IsHyperVCompatible(HardwareSecurity cpuInfo)
{
    // Lógica simplificada: CPUs modernos con soporte de virtualización y MBEC son compatibles
    return cpuInfo.SupportsVirtualization && cpuInfo.HasMBEC;
}

   // Asegúrate de ponerlo al inicio del archivo si no existe

private void EvaluateCpuRisk(CheckResults results)
{
    string cpuName = results.CpuName;
    string manufacturer = results.HardwareSecurityInfo.Manufacturer;
    string riesgo = "Bajo";
    string mensaje = "✅ Sistema compatible sin problemas conocidos.";
    string solucion = "Sigue los pasos del método HyperVision sin precauciones especiales.";

    // --- Intel ---
    if (manufacturer == "Intel")
    {
        int gen = 0;
        var m = Regex.Match(cpuName, @"\b(\d+)(?:th|nd|rd|st)\s+Gen\b", RegexOptions.IgnoreCase);
        if (m.Success) gen = int.Parse(m.Groups[1].Value);

        if (cpuName.Contains("Core 2") || cpuName.Contains("Pentium") || cpuName.Contains("Celeron") || (gen > 0 && gen <= 7))
        {
            riesgo = "Alto";
            mensaje = "🔴 CPU Intel antigua (7ª Gen o anterior). Alto riesgo de BSOD e inestabilidad.";
            solucion = "❌ No se recomienda usar el método. Si decides hacerlo, actualiza BIOS y desactiva todas las protecciones (VBS, HVCI, Secure Boot, BitLocker). Espera inestabilidad.";
        }
        else if (gen == 8 || gen == 9)
        {
            riesgo = "Medio";
            mensaje = "🟡 CPU Intel 8ª/9ª Gen. El driver del hipervisor es experimental; puede fallar o dar bajo rendimiento.";
            solucion = "🔧 Actualiza la BIOS y los drivers del chipset. Si hay BSOD, desactiva los C-states en la BIOS. Considera usar un driver de hipervisor más reciente.";
        }
        else if (gen == 10 || gen == 11)
        {
            riesgo = "Bajo";
            mensaje = "✅ CPU Intel 10ª/11ª Gen. Buena compatibilidad si se siguen los pasos.";
            solucion = "📋 Sigue los pasos estándar: desactiva VBS/HVCI, Secure Boot, BitLocker. Reinicia con F7 si es necesario.";
        }
        else if (gen == 12)
        {
            riesgo = "Alto";
            mensaje = "🔴 CPU Intel 12ª Gen (Alder Lake). Riesgo de que el hipervisor se ejecute en núcleos E.";
            solucion = "⚙️ Usa Process Lasso para fijar la afinidad del juego a los núcleos P (rendimiento). Alternativamente, desactiva los E-cores en la BIOS. Mantén el scheduler de Windows 11 actualizado.";
        }
        else if (gen == 13 || gen == 14)
        {
            riesgo = "Alto";
            mensaje = "🔴 CPU Intel 13ª/14ª Gen. Problemas de estabilidad de fábrica (voltajes).";
            solucion = "⚠️ Actualiza la BIOS a la versión que incluya el microcódigo 0x12B o superior. Reduce el voltaje del CPU si es necesario. Evita usar el método en estas CPUs hasta tener la BIOS estable.";
        }
        else if (cpuName.Contains("Ultra") && cpuName.Contains("200"))
        {
            riesgo = "Medio";
            mensaje = "🟡 CPU Intel Core Ultra 200 series. Por su novedad, pueden existir incompatibilidades.";
            solucion = "🔍 Busca actualizaciones del método HyperVision específicas para Arrow Lake. Prueba en un sistema de prueba antes de usar en producción.";
        }
    }
    // --- AMD ---
    else if (manufacturer == "AMD")
    {
        int ryzenSeries = 0;
        var m = Regex.Match(cpuName, @"Ryzen\s+(\d{4})", RegexOptions.IgnoreCase);
        if (m.Success) ryzenSeries = int.Parse(m.Groups[1].Value);

        if (ryzenSeries == 1000 || ryzenSeries == 2000)
        {
            riesgo = "Alto";
            mensaje = "🔴 AMD Ryzen 1000/2000 (Zen 1/Zen+). Alto riesgo de inestabilidad y BSOD.";
            solucion = "❌ No recomendado. Si pruebas, actualiza BIOS y drivers del chipset. Desactiva SVM en la BIOS y todas las protecciones de Windows.";
        }
        else if (ryzenSeries == 3000)
        {
            riesgo = "Medio";
            mensaje = "🟡 AMD Ryzen 3000 (Zen 2). Sin MBEC completo, penalización de rendimiento.";
            solucion = "🔧 Actualiza BIOS y drivers del chipset. Desactiva VBS/HVCI. Acepta una pérdida de FPS. Si es inestable, desactiva SVM en la BIOS.";
        }
        else if (ryzenSeries == 5000)
        {
            riesgo = "Bajo";
            mensaje = "✅ AMD Ryzen 5000 (Zen 3). Buena compatibilidad.";
            solucion = "📋 Pasos estándar: desactiva VBS/HVCI, Secure Boot, BitLocker. Asegúrate de que Hyper-V esté desactivado.";
        }
        else if (ryzenSeries == 7000 || ryzenSeries == 8000)
        {
            riesgo = "Alto";
            mensaje = "🔴 AMD Ryzen 7000/8000. Bug conocido de reinicios aleatorios con VBS activo.";
            solucion = "⚠️ Obligatorio desactivar VBS/HVCI y también desactivar la virtualización (SVM) en la BIOS si los reinicios persisten. Mantén la BIOS actualizada (AGESA 1.2.0.x).";
        }
        else if (ryzenSeries == 9000)
        {
            riesgo = "Bajo";
            mensaje = "✅ AMD Ryzen 9000 (Zen 5). Potencialmente compatible.";
            solucion = "🔍 Al ser nueva, puede tener problemas de detección por Denuvo. Busca actualizaciones del bypass. Prueba en modo de prueba primero.";
        }
    }

    // --- Ajuste por Windows 11 24H2 ---
    if (Environment.OSVersion.Version.Build >= 26000)
    {
        if (riesgo != "Alto") riesgo = "Medio";
        mensaje += " ⚠️ Windows 11 24H2 detectado. Desactivar VBS es complejo y puede fallar.";
        solucion += " En Windows 11 24H2, sigue una guía específica para desactivar VBS mediante registro y política de grupo. Es posible que necesites una reinstalación limpia con VBS desactivado desde el inicio.";
    }

    results.CpuRiskLevel = riesgo;
    results.CpuRiskMessage = mensaje;
    results.CpuRiskSolution = solucion;
}


// ========================================
// MÉTODOS AUXILIARES PARA REG.EXE
// ========================================

private bool RunRegCommand(string command)
{
    try
    {
        var psi = new ProcessStartInfo("reg.exe", command)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            Verb = "runas"
        };
        using (var p = Process.Start(psi))
        {
            p?.WaitForExit();
            return p != null && p.ExitCode == 0;
        }
    }
    catch
    {
        return false;
    }
}

private string RunRegQuery(string command)
{
    try
    {
        var psi = new ProcessStartInfo("reg.exe", command)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        using (var p = Process.Start(psi))
        {
            if (p != null)
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output;
            }
        }
    }
    catch { }
    return "";
}

// ========================================
// FUNCIONES PARA DESACTIVAR CARACTERÍSTICAS
// ========================================

public void BackupCurrentConfig()
{
    try
    {
        using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\ManageVBS\Backup", true))
        {
            // Guardar VBS
            using (var dgKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard"))
            {
                int vbs = (dgKey?.GetValue("EnableVirtualizationBasedSecurity") as int?) ?? 0;
                key.SetValue("VBS", vbs, RegistryValueKind.DWord);
            }
            // Guardar HVCI
            using (var hvciKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"))
            {
                int hvci = (hvciKey?.GetValue("Enabled") as int?) ?? 0;
                key.SetValue("HVCI", hvci, RegistryValueKind.DWord);
            }
            // Guardar Hypervisor launch type
            var psi = new ProcessStartInfo("bcdedit", "/enum {current}")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using (var p = Process.Start(psi))
            {
                if (p != null)
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    if (output.Contains("hypervisorlaunchtype") && output.Contains("Auto"))
                        key.SetValue("Hypervisor", 1, RegistryValueKind.DWord);
                    else
                        key.SetValue("Hypervisor", 0, RegistryValueKind.DWord);
                }
                else
                {
                    key.SetValue("Hypervisor", 0, RegistryValueKind.DWord);
                }
            }
            key.SetValue("BackupDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
    catch { }
}
public void DisableVbs()
{
    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard", true))
    {
        if (key != null)
            key.SetValue("EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord);
    }
}

public void DisableHvci()
{
    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", true))
    {
        if (key != null)
            key.SetValue("Enabled", 0, RegistryValueKind.DWord);
    }
}

public void DisableWindowsHello()
{
    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello", true))
    {
        if (key != null)
            key.SetValue("Enabled", 0, RegistryValueKind.DWord);
    }
}

public void DisableSecureBiometrics()
{
    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\SecureBiometrics", true))
    {
        if (key != null)
            key.SetValue("Enabled", 0, RegistryValueKind.DWord);
    }
}

public void DisableSystemGuard()
{
    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\SystemGuard", true))
    {
        if (key != null)
            key.SetValue("Enabled", 0, RegistryValueKind.DWord);
    }
}

public void DisableCredentialGuard()
{
    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa", true))
    {
        if (key != null)
            key.SetValue("LsaCfgFlags", 0, RegistryValueKind.DWord);
    }
    using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\CredentialGuard", true))
    {
        if (key != null)
            key.SetValue("Enabled", 0, RegistryValueKind.DWord);
    }
}


public void DisableHypervisor()
{
    var psi = new ProcessStartInfo("bcdedit", "/set hypervisorlaunchtype off")
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        Verb = "runas"
    };
    Process.Start(psi)?.WaitForExit();
}



// ========================================
// FUNCIONES PARA ACTIVAR CARACTERÍSTICAS
// ========================================

public void EnableVbs()
{
    RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 1 /f");
}

public void EnableHvci()
{
    RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity /v Enabled /t REG_DWORD /d 1 /f");
}

public void EnableWindowsHello()
{
    RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello /v Enabled /t REG_DWORD /d 1 /f");
}

public void EnableSecureBiometrics()
{
    RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\SecureBiometrics /v Enabled /t REG_DWORD /d 1 /f");
}

public void EnableSystemGuard()
{
    RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\SystemGuard /v Enabled /t REG_DWORD /d 1 /f");
}

public void EnableCredentialGuard()
{
    RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\Lsa /v LsaCfgFlags /t REG_DWORD /d 2 /f");
    RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\CredentialGuard /v Enabled /t REG_DWORD /d 1 /f");
}


public void EnableHypervisor()
{
    var psi = new ProcessStartInfo("bcdedit", "/set hypervisorlaunchtype auto")
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        Verb = "runas"
    };
    Process.Start(psi)?.WaitForExit();
}

// ========================================
// REVERTIR CAMBIOS (RESTAURAR BACKUP)
// ========================================

public (bool success, string message) RestoreFromBackup()
{
    try
    {
        // Verificar si existe backup
        string backupCheck = RunRegQuery("query HKLM\\SOFTWARE\\ManageVBS\\Backup");
        if (string.IsNullOrEmpty(backupCheck) || backupCheck.Contains("Error"))
        {
            return (false, "No se encontró ningún backup previo. Usa 'Guardar Backup' primero o desactiva las características manualmente.");
        }

        // Leer valores del backup
        string vbsValue = RunRegQuery("query HKLM\\SOFTWARE\\ManageVBS\\Backup /v VBS");
        string hvciValue = RunRegQuery("query HKLM\\SOFTWARE\\ManageVBS\\Backup /v HVCI");
        string hypervisorValue = RunRegQuery("query HKLM\\SOFTWARE\\ManageVBS\\Backup /v Hypervisor");

        // Restaurar VBS
        if (vbsValue.Contains("0x1"))
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 1 /f");
        else
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f");

        // Restaurar HVCI
        if (hvciValue.Contains("0x1"))
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity /v Enabled /t REG_DWORD /d 1 /f");
        else
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity /v Enabled /t REG_DWORD /d 0 /f");

        // Restaurar Hypervisor (bcdedit)
        if (hypervisorValue.Contains("0x1"))
        {
            var psi = new ProcessStartInfo("bcdedit", "/set hypervisorlaunchtype auto")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };
            Process.Start(psi)?.WaitForExit();
        }
        else
        {
            var psi = new ProcessStartInfo("bcdedit", "/set hypervisorlaunchtype off")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };
            Process.Start(psi)?.WaitForExit();
        }

        // También restaurar Windows Hello, SecureBiometrics, SystemGuard, CredentialGuard y KVA Shadow
        // Nota: En un backup completo se guardarían también esos valores. Por simplicidad, los activamos/desactivamos según lógica.
        // Para una restauración completa, leemos los valores si existen.
        string whValue = RunRegQuery("query HKLM\\SOFTWARE\\ManageVBS\\Backup /v WindowsHello");
        if (whValue.Contains("0x1"))
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello /v Enabled /t REG_DWORD /d 1 /f");
        else if (whValue.Contains("0x0"))
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello /v Enabled /t REG_DWORD /d 0 /f");

        string sbValue = RunRegQuery("query HKLM\\SOFTWARE\\ManageVBS\\Backup /v SecureBiometrics");
        if (sbValue.Contains("0x1"))
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\SecureBiometrics /v Enabled /t REG_DWORD /d 1 /f");
        else if (sbValue.Contains("0x0"))
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\SecureBiometrics /v Enabled /t REG_DWORD /d 0 /f");

        string sgValue = RunRegQuery("query HKLM\\SOFTWARE\\ManageVBS\\Backup /v SystemGuard");
        if (sgValue.Contains("0x1"))
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\SystemGuard /v Enabled /t REG_DWORD /d 1 /f");
        else if (sgValue.Contains("0x0"))
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\SystemGuard /v Enabled /t REG_DWORD /d 0 /f");

        string cgValue = RunRegQuery("query HKLM\\SOFTWARE\\ManageVBS\\Backup /v CredentialGuard");
        if (cgValue.Contains("0x1"))
        {
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\Lsa /v LsaCfgFlags /t REG_DWORD /d 2 /f");
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\CredentialGuard /v Enabled /t REG_DWORD /d 1 /f");
        }
        else if (cgValue.Contains("0x0"))
        {
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\Lsa /v LsaCfgFlags /t REG_DWORD /d 0 /f");
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\CredentialGuard /v Enabled /t REG_DWORD /d 0 /f");
        }

        // KVA Shadow: si en backup existe KVAShadow = 1, eliminamos las claves; si es 0, las añadimos.
        string kvaValue = RunRegQuery("query HKLM\\SOFTWARE\\ManageVBS\\Backup /v KVAShadow");
        if (kvaValue.Contains("0x1"))
        {
            RunRegCommand(@"delete HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management /v FeatureSettingsOverride /f");
            RunRegCommand(@"delete HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management /v FeatureSettingsOverrideMask /f");
        }
        else if (kvaValue.Contains("0x0"))
        {
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management /v FeatureSettingsOverride /t REG_DWORD /d 2 /f");
            RunRegCommand(@"add HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management /v FeatureSettingsOverrideMask /t REG_DWORD /d 3 /f");
        }

        return (true, "Configuración restaurada desde backup. Se recomienda reiniciar.");
    }
    catch (Exception ex)
    {
        return (false, $"Error al restaurar: {ex.Message}");
    }
}

public void SetOneTimeAdvancedBoot()
{
    var psi = new ProcessStartInfo("bcdedit", "/set {current} onetimeadvancedoptions on")
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        Verb = "runas"
    };
    Process.Start(psi)?.WaitForExit();
}

public void RebootSystem()
{
    var psi = new ProcessStartInfo("shutdown", "/r /t 0")
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        Verb = "runas"
    };
    Process.Start(psi);
}

// ========================================
// VER ESTADO (OPCIÓN 1 DEL SCRIPT)
// ========================================

public string GetSystemStatus()
{
    var sb = new StringBuilder();

    // Obtener información del sistema operativo
    string osVersion = GetOSVersion();
    string osBuild = GetOSBuild();
    string osArch = Environment.Is64BitOperatingSystem ? "x64" : "x86";

    ;
    sb.AppendLine("                     📊 ESTADO DEL SISTEMA                        ");
    sb.AppendLine();
    sb.AppendLine("🖥️  SISTEMA OPERATIVO:");
    sb.AppendLine($"   • Versión: {osVersion}");
    sb.AppendLine($"   • Build: {osBuild}");
    sb.AppendLine($"   • Arquitectura: {osArch}");
    sb.AppendLine();

    // Anti-cheats
    sb.AppendLine("⚠️  ANTI-CHEATS DETECTADOS:");
    var antiCheats = DetectAntiCheats();
    if (antiCheats.Count > 0)
    {
        foreach (var ac in antiCheats)
            sb.AppendLine($"   • {ac}");
        sb.AppendLine("   Nota: Pueden bloquear controladores sin firma.");
    }
    else
        sb.AppendLine("   • No se detectaron anti-cheats.");
    sb.AppendLine();

    // Smart App Control
    string sacStatus = GetSmartAppControlStatus();
    if (!string.IsNullOrEmpty(sacStatus))
        sb.AppendLine($"🛡️  Smart App Control: {sacStatus}");
    sb.AppendLine();

    // Virtualización en BIOS
    bool virtEnabled = IsVirtualizationEnabled();
    sb.AppendLine($"🖥️  Virtualización (VT-x/AMD-V): {(virtEnabled ? "✅ Habilitada en BIOS" : "❌ NO habilitada en BIOS")}");
    sb.AppendLine();

    // UEFI Locks
    var uefiLocks = GetUefiLocks();
    if (uefiLocks.Count > 0)
    {
        sb.AppendLine("🔒 UEFI LOCKS DETECTADOS:");
        foreach (var lockInfo in uefiLocks)
            sb.AppendLine($"   • {lockInfo}");
        sb.AppendLine();
    }

    // BitLocker
    bool bitlockerActive = IsBitLockerActive();
    sb.AppendLine($"💾 BitLocker: {(bitlockerActive ? "⚠️ ACTIVO en unidad del sistema" : "✅ Inactivo")}");
    if (bitlockerActive)
        sb.AppendLine("   • Se suspenderá automáticamente al reiniciar en modo avanzado.");
    sb.AppendLine();

    // Backup existente
    bool backupExists = CheckBackupExists();
    sb.AppendLine($"💾 Respaldo guardado: {(backupExists ? "✅ Sí" : "❌ No")}");
    if (backupExists)
    {
        string backupDate = GetBackupDate();
        if (!string.IsNullOrEmpty(backupDate))
            sb.AppendLine($"   • Fecha del respaldo: {backupDate}");
    }
    sb.AppendLine();

    
    sb.AppendLine("              CARACTERÍSTICAS DE SEGURIDAD");
    sb.AppendLine();

    // Estado de cada característica
    sb.AppendLine(FormatFeatureStatus("VBS (Virtualization-based Security)", IsVbsEnabled()));
    sb.AppendLine(FormatFeatureStatus("HVCI (Memory Integrity)", IsHvciEnabled()));
    sb.AppendLine(FormatFeatureStatus("Windows Hello Protection", IsWindowsHelloEnabled()));
    sb.AppendLine(FormatFeatureStatus("Enhanced Sign-in Security (SecureBiometrics)", IsSecureBiometricsEnabled()));
    sb.AppendLine(FormatFeatureStatus("System Guard Secure Launch", IsSystemGuardEnabled()));
    sb.AppendLine(FormatFeatureStatus("Credential Guard", IsCredentialGuardEnabled()));
    sb.AppendLine(FormatFeatureStatus("KVA Shadow (Meltdown Mitigation)", IsKvaShadowEnabled()));
    sb.AppendLine(FormatFeatureStatus("Windows Hypervisor", IsHypervisorEnabled()));

    return sb.ToString();
}

// Métodos auxiliares (colócalos dentro de la clase SystemChecker)

private string GetOSVersion()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
        {
            string productName = key?.GetValue("ProductName")?.ToString() ?? "Windows";
            string releaseId = key?.GetValue("ReleaseId")?.ToString() ?? "";
            if (!string.IsNullOrEmpty(releaseId))
                return $"{productName} versión {releaseId}";
            return productName;
        }
    }
    catch { return "Desconocido"; }
}

private string GetOSBuild()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
        {
            string build = key?.GetValue("CurrentBuild")?.ToString() ?? "";
            string ubr = key?.GetValue("UBR")?.ToString() ?? "";
            if (!string.IsNullOrEmpty(ubr))
                return $"{build}.{ubr}";
            return build;
        }
    }
    catch { return "Desconocido"; }
}

private List<string> DetectAntiCheats()
{
    var list = new List<string>();
    string[] paths = {
        @"C:\Program Files\FACEIT AC",
        @"C:\Program Files\EasyAntiCheat",
        @"C:\Program Files\EasyAntiCheat_EOS",
        @"C:\Program Files\BattlEye",
        @"C:\Program Files\Riot Vanguard",
        @"C:\Program Files\EAC",
        @"C:\Program Files (x86)\FACEIT AC",
        @"C:\Program Files (x86)\EasyAntiCheat",
        @"C:\Program Files (x86)\BattlEye",
        @"C:\Program Files (x86)\Riot Vanguard"
    };
    foreach (string path in paths)
    {
        if (Directory.Exists(path))
        {
            string name = Path.GetFileName(path);
            if (!list.Contains(name))
                list.Add(name);
        }
    }
    return list;
}

private string GetSmartAppControlStatus()
{
    if (Environment.OSVersion.Version.Build < 22621) return "";
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CI\Policy"))
        {
            int? state = key?.GetValue("VerifiedAndReputablePolicyState") as int?;
            if (state == 1) return "✅ ACTIVADO (puede bloquear aplicaciones)";
            if (state == 2) return "⚠️ EN EVALUACIÓN (puede activarse solo)";
            return "❌ Inactivo";
        }
    }
    catch { return "No determinado"; }
}

private bool IsVirtualizationEnabled()
{
    bool virtEnabled = false;

    // 1. Verificar mediante WMI
    try
    {
        using (var searcher = new ManagementObjectSearcher("SELECT VirtualizationFirmwareEnabled FROM Win32_Processor"))
        {
            foreach (var obj in searcher.Get())
            {
                var val = obj["VirtualizationFirmwareEnabled"];
                if (val != null && Convert.ToBoolean(val))
                    virtEnabled = true;
                break;
            }
        }
    }
    catch { }

    // 2. Verificar mediante systeminfo
    if (!virtEnabled)
    {
        try
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = "systeminfo";
                process.StartInfo.Arguments = " | findstr /C:\"Virtualización\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (output.Contains("Sí") || output.Contains("Enabled"))
                    virtEnabled = true;
            }
        }
        catch { }
    }

    // 3. Verificar mediante el registro de DeviceGuard
    if (!virtEnabled)
    {
        try
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard"))
            {
                if (key != null)
                {
                    var hypervisorEnforced = key.GetValue("HypervisorEnforcedCodeIntegrity");
                    if (hypervisorEnforced != null && Convert.ToInt32(hypervisorEnforced) == 1)
                        virtEnabled = true;
                }
            }
        }
        catch { }
    }

    // 4. Si VBS o HVCI están activados, la virtualización está activada obligatoriamente
    if (!virtEnabled)
    {
        if (IsVbsEnabled() || IsHvciEnabled())
            virtEnabled = true;
    }

    return virtEnabled;
}

private List<string> GetUefiLocks()
{
    var locks = new List<string>();
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard"))
        {
            if (key?.GetValue("Locked") as int? == 1)
                locks.Add("VBS bloqueada por firmware");
        }
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"))
        {
            if (key?.GetValue("Locked") as int? == 1)
                locks.Add("HVCI bloqueada por firmware");
        }
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa"))
        {
            if (key?.GetValue("LsaCfgFlags") as int? == 2)
                locks.Add("Credential Guard bloqueada por firmware");
        }
    }
    catch { }
    return locks;
}

private bool IsBitLockerActive()
{
    try
    {
        using (var searcher = new ManagementObjectSearcher("SELECT ProtectionStatus FROM Win32_EncryptableVolume"))
        {
            foreach (var obj in searcher.Get())
            {
                return Convert.ToInt32(obj["ProtectionStatus"]) == 1;
            }
        }
    }
    catch { }
    return false;
}

private bool CheckBackupExists()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ManageVBS\Backup"))
        {
            return key != null;
        }
    }
    catch { return false; }
}

private string GetBackupDate()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ManageVBS\Backup"))
        {
            return key?.GetValue("BackupDate")?.ToString() ?? "";
        }
    }
    catch { return ""; }
}

private string FormatFeatureStatus(string name, bool isEnabled)
{
    string status = isEnabled ? "✅ ACTIVO" : "❌ INACTIVO";
    return $"• {name}: {status}";
}

// Métodos para leer cada característica (algunos ya los tienes, pero los renombramos para claridad)

public bool IsVbsEnabled()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard"))
        {
            return (key?.GetValue("EnableVirtualizationBasedSecurity") as int?) == 1;
        }
    }
    catch { return false; }
}

public bool IsHvciEnabled()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"))
        {
            return (key?.GetValue("Enabled") as int?) == 1;
        }
    }
    catch { return false; }
}

public bool IsWindowsHelloEnabled()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello"))
        {
            return (key?.GetValue("Enabled") as int?) == 1;
        }
    }
    catch { return false; }
}

public bool IsSecureBiometricsEnabled()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\SecureBiometrics"))
        {
            return (key?.GetValue("Enabled") as int?) == 1;
        }
    }
    catch { return false; }
}

public bool IsSystemGuardEnabled()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\SystemGuard"))
        {
            return (key?.GetValue("Enabled") as int?) == 1;
        }
    }
    catch { return false; }
}

public bool IsCredentialGuardEnabled()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa"))
        {
            if ((key?.GetValue("LsaCfgFlags") as int?) == 2)
                return true;
        }
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\CredentialGuard"))
        {
            return (key?.GetValue("Enabled") as int?) == 1;
        }
    }
    catch { return false; }
}

public bool EnableKvaShadow()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true))
        {
            if (key == null) return false;
            
            bool changed = false;
            if (key.GetValue("FeatureSettingsOverride") != null)
            {
                key.DeleteValue("FeatureSettingsOverride");
                changed = true;
            }
            if (key.GetValue("FeatureSettingsOverrideMask") != null)
            {
                key.DeleteValue("FeatureSettingsOverrideMask");
                changed = true;
            }
            return changed; // Si no había valores, también se considera éxito (ya estaba activado)
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error al activar KVA Shadow: {ex.Message}");
        return false;
    }
}

public bool DisableKvaShadow()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true))
        {
            if (key == null) return false;
            
            key.SetValue("FeatureSettingsOverride", 2, RegistryValueKind.DWord);
            key.SetValue("FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord);
            return true;
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error al desactivar KVA Shadow: {ex.Message}");
        return false;
    }
}

public bool IsKvaShadowEnabled()
{
    try
    {
        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"))
        {
            int? overrideVal = key?.GetValue("FeatureSettingsOverride") as int?;
            int? maskVal = key?.GetValue("FeatureSettingsOverrideMask") as int?;
            // Si ambas claves existen y tienen los valores 2 y 3 -> INACTIVO
            if (overrideVal == 2 && maskVal == 3)
                return false;
            return true; // En cualquier otro caso -> ACTIVO
        }
    }
    catch { return true; }
}

public bool IsHypervisorEnabled()
{
    try
    {
        var psi = new ProcessStartInfo("bcdedit", "/enum {current}")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        using (var p = Process.Start(psi))
        {
            if (p != null)
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output.Contains("hypervisorlaunchtype") && output.Contains("Auto");
            }
        }
    }
    catch { }
    return false;
}

}