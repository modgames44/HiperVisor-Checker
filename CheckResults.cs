
using System.Collections.Generic;

namespace BypassCheckerWPF;

public class CheckResults
{
    // Propiedades básicas del sistema
    public string CpuName { get; set; } = "";
    public int CpuCores { get; set; }
    public int CpuThreads { get; set; }
    public string Architecture { get; set; } = "";
    public double RamGB { get; set; }
    public double DiskSpaceGB { get; set; }
    
    // Propiedades de seguridad
    public bool TpmPresent { get; set; }
    public bool SecureBootEnabled { get; set; }
    public bool UefiMode { get; set; }
    public bool IsCompatible { get; set; }
    public HardwareSecurity HardwareSecurityInfo { get; set; } = new HardwareSecurity();
    public List<string> MissingRequirements { get; set; } = new List<string>();
    
    // Propiedades para HyperVision
    public bool IsVirtualizationEnabled { get; set; }
    public bool IsVbsEnabled { get; set; }
    public bool IsHvciEnabled { get; set; }
    public bool IsBitLockerActive { get; set; }
    public bool HasVbsUefiLock { get; set; }
    public bool HasCredGuardUefiLock { get; set; }
    public List<string> IncompatibleDriversList { get; set; } = new List<string>();
}
