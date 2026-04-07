
using System.Collections.Generic;

namespace BypassCheckerWPF;

public class BypassResults
{
    public bool CanDisableVbs { get; set; }
    public string VbsRequirementStatus { get; set; } = "";
    public bool HasUefiLock { get; set; }
    public string UefiLockStatus { get; set; } = "";
    public bool IsBitLockerActive { get; set; }
    public string BitLockerStatus { get; set; } = "";
    public bool RecommendsF7Method { get; set; }
    public bool IncompatibleDriversDetected { get; set; }
    public List<string> IncompatibleDriversList { get; set; } = new List<string>();
    public List<string> HyperVisionSteps { get; set; } = new List<string>();
}
