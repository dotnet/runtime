# Contract RuntimeInfo

This contract encapsulates support for fetching information about the target runtime.

## APIs of contract

```csharp
public enum RuntimeInfoArchitecture : uint
{
    Unknown = 0,
    X86,
    Arm32,
    X64,
    Arm64,
    LoongArch64,
    RISCV,
}

public enum RuntimeInfoOperatingSystem : uint
{
    Unknown = 0,
    Win,
    Unix,
}
```

```csharp
// Gets the targets architecture. If this information is not available returns Unknown.
RuntimeInfoArchitecture GetTargetArchitecture();

// Gets the targets operating system. If this information is not available returns Unknown.
RuntimeInfoOperatingSystem GetTargetOperatingSystem();
```

## Version 1

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| Architecture | string | Target architecture |
| OperatingSystem | string | Target operating system |

The contract implementation simply returns the contract descriptor global values parsed as the respective enum case-insensitively. If these globals are not available, the contract returns Unknown.
