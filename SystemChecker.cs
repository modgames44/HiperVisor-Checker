
using System.Management;
using Microsoft.Win32;
using System.IO;

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
        CheckVirtualizationEnabled(results);
        CheckVbsStatus(results);
        CheckHvciStatus(results);
        CheckBitLockerStatus(results);
        CheckCredentialGuardUefiLock(results);
        DetermineCompatibility(results);
        
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

    private void CheckVirtualizationEnabled(CheckResults results)
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (var obj in searcher.Get())
                {
                    var virtFirmware = obj["VirtualizationFirmwareEnabled"];
                    results.IsVirtualizationEnabled = virtFirmware != null && Convert.ToBoolean(virtFirmware);
                    break;
                }
            }
        }
        catch { results.IsVirtualizationEnabled = false; }
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
}
