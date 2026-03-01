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

// Returns the runtime's RecommendedReaderVersion global. Returns 0 if the global is absent.
uint RecommendedReaderVersion { get; }

// An embedded version constant indicating how much runtime functionality this reader knows how to parse.
uint CurrentReaderVersion { get; }
```

## Version 1

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| Architecture | string | Target architecture |
| OperatingSystem | string | Target operating system |
| RecommendedReaderVersion | uint32 | Incremented when an update to the latest contracts is recommended |

The contract implementation returns the architecture and operating system global values parsed as the
respective enum case-insensitively. If these globals are not available, the contract returns Unknown.

### Reader versioning scheme

When the .NET runtime team wants to signal that an update is recommended we update both the
`CurrentReaderVersion` constant in the cDAC implementation and the `RecommendedReaderVersion`
global value in the runtime. This causes older tools on older cDAC versions to observe
RecommendedReaderVersion > CurrentReaderVersion. The tool can notify the user that an update
is recommended.
