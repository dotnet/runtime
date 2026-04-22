# cDAC Stress Tests

Integration tests that verify the cDAC's stack reference enumeration matches the runtime's
GC root scanning under GC stress conditions.

## How It Works

Each test runs a debuggee console app under `corerun` with `DOTNET_CdacStress=0x51`, which enables:
- **0x01**: Allocation-point verification (triggers at every managed allocation)
- **0x10**: GC reference comparison (compares cDAC stack refs against runtime refs)
- **0x40**: Legacy DAC comparison (three-way: cDAC vs DAC vs runtime)

The native `cdacstress.cpp` hook writes structured per-frame comparison results to a log file.
On failure, it shows per-frame diffs with resolved method names, making it easy to identify
which frame and method has mismatched GC references.

Pass/fail semantics:
- **[PASS]**: cDAC matches DAC (may include `[RT_DIFF]` annotation if RT differs)
- **[FAIL]**: cDAC does NOT match DAC
- **[SKIP]**: cDAC GetStackReferences failed (e.g., during EH)

## Prerequisites

Build the runtime with the cDAC stress hook enabled:

```powershell
# From repo root
.\build.cmd -subset clr.native+tools.cdac -c Debug -rc Checked -lc Release
.\src\tests\build.cmd Checked generatelayoutonly -SkipRestorePackages /p:LibrariesConfiguration=Release
```

## Running Tests

### Using RunStressTests.ps1

```powershell
# Run all debuggees (allocation-point verification, no GCStress)
./RunStressTests.ps1 -SkipBuild

# Run a single debuggee
./RunStressTests.ps1 -SkipBuild -Debuggee BasicAlloc

# Run with instruction-level GCStress (slower, more thorough)
./RunStressTests.ps1 -SkipBuild -CdacStress 0x14 -GCStress 0x4
```

### Using dotnet test (xUnit)

```powershell
# Build and run all stress tests
.\.dotnet\dotnet.exe test src\native\managed\cdac\tests\StressTests

# Run a specific debuggee
.\.dotnet\dotnet.exe test src\native\managed\cdac\tests\StressTests --filter "FullyQualifiedName~BasicAlloc"

# Set CORE_ROOT manually if needed
$env:CORE_ROOT = "path\to\Core_Root"
.\.dotnet\dotnet.exe test src\native\managed\cdac\tests\StressTests
```

## Adding a New Debuggee

1. Create a folder under `Debuggees/` with a `.csproj` and `Program.cs`
2. The `.csproj` just needs: `<Project Sdk="Microsoft.NET.Sdk" />`
   (inherits OutputType=Exe and TFM from `Directory.Build.props`)
3. `Main()` must return `100` on success
4. Use `[MethodImpl(MethodImplOptions.NoInlining)]` on methods to prevent inlining
5. Use `GC.KeepAlive()` to ensure objects are live at GC stress points
6. Add the debuggee name to `BasicStressTests.Debuggees`

## Debuggee Catalog

| Debuggee | Scenarios |
|----------|-----------|
| **BasicAlloc** | Objects, strings, arrays, many live refs |
| **ExceptionHandling** | try/catch/finally funclets, nested exceptions, filter funclets, rethrow |
| **DeepStack** | Deep recursion with live refs at each frame |
| **Generics** | Generic method instantiations, interface dispatch, delegates |
| **PInvoke** | P/Invoke transitions, pinned GC handles, struct with object refs |
| **MultiThread** | Concurrent threads with synchronized GC stress |
| **Comprehensive** | All-in-one: every scenario in a single run |
| **StructScenarios** | Struct returns, by-ref params |
| **DynamicMethods** | DynamicMethod / IL emit |

## Architecture

```
CdacStressTestBase.RunGCStress(debuggeeName)
  │
  ├── Locate core_root/corerun (CORE_ROOT env or default path)
  ├── Locate debuggee DLL (artifacts/bin/StressTests/<name>/...)
  ├── Start Process: corerun <debuggee.dll>
  │     Environment:
  │       DOTNET_CdacStress=0x51
  │       DOTNET_CdacStressStep=1
  │       DOTNET_CdacStressLogFile=<temp file>
  │       DOTNET_ContinueOnAssert=1
  ├── Wait for exit (timeout: 300s)
  ├── Parse results log → CdacStressResults
  └── Assert: exit=100, zero failures
```
