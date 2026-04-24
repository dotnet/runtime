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
    Browser,
    Apple,
}
```

```csharp
// Gets the targets architecture. If this information is not available returns Unknown.
RuntimeInfoArchitecture GetTargetArchitecture();

// Gets the targets operating system. If this information is not available returns Unknown.
RuntimeInfoOperatingSystem GetTargetOperatingSystem();

// Returns the runtime's RecommendedReaderVersion global. Returns 0 if the global is absent.
uint GetRecommendedReaderVersion();

// An embedded version constant indicating how much runtime functionality this reader knows how to parse.
uint GetCurrentReaderVersion();
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

`Apple` covers all Apple platforms (macOS, iOS, tvOS, MacCatalyst) — i.e. any target where the
runtime is compiled with `TARGET_APPLE` defined. It is distinct from `Unix` so that consumers which
need to apply Apple-specific ABI or platform rules (for example, the Apple ARM64 stack-argument
alignment) can detect the target reliably. Apple platforms are still POSIX and will behave like
`Unix` for the purposes of any `GetTargetOperatingSystem() != Windows` check.

### Reader versioning scheme

When the .NET runtime team wants to signal that an update is recommended we update both the
value returned by `GetCurrentReaderVersion()` in the cDAC implementation and the `RecommendedReaderVersion`
global value in the runtime. This causes older tools on older cDAC versions to observe
`GetRecommendedReaderVersion()` > `GetCurrentReaderVersion()`. The tool can notify the user that an update
is recommended.
