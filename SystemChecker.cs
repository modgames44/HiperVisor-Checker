using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;

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
        
        return results;
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

}