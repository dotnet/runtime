# cDAC GC Stress Tests

Integration tests that verify the cDAC's stack reference enumeration matches the runtime's
GC root scanning under GC stress conditions.

## How It Works

Each test runs a debuggee console app under `corerun` with `DOTNET_GCStress=0x24`, which enables:
- **0x4**: Instruction-level JIT stress (triggers GC at every safe point)
- **0x20**: cDAC verification (compares cDAC stack refs against runtime refs)

`DOTNET_GCStressCdacStep` throttles verification to every Nth stress point. The default
is 1 (verify every point). Higher values reduce cDAC overhead while maintaining instruction-level
breakpoint coverage for code path diversity.

The native `cdacgcstress.cpp` hook writes `[PASS]`/`[FAIL]`/`[SKIP]` lines to a log file.
The test framework parses this log and asserts a high pass rate (≥99.9% for most debuggees,
≥99% for ExceptionHandling which has known funclet gaps).

## Prerequisites

Build the runtime with the cDAC GC stress hook enabled:

```powershell
# From repo root
.\build.cmd -subset clr.native+tools.cdac -c Debug -rc Checked -lc Release
.\.dotnet\dotnet.exe msbuild src\libraries\externals.csproj /t:Build /p:Configuration=Release /p:RuntimeConfiguration=Checked /p:TargetOS=windows /p:TargetArchitecture=x64 -v:minimal
.\src\tests\build.cmd Checked generatelayoutonly -SkipRestorePackages /p:LibrariesConfiguration=Release
```

## Running Tests

```powershell
# Build and run all GC stress tests
.\.dotnet\dotnet.exe test src\native\managed\cdac\tests\GCStressTests

# Run a specific debuggee
.\.dotnet\dotnet.exe test src\native\managed\cdac\tests\GCStressTests --filter "debuggeeName=BasicAlloc"

# Set CORE_ROOT manually if needed
$env:CORE_ROOT = "path\to\Core_Root"
.\.dotnet\dotnet.exe test src\native\managed\cdac\tests\GCStressTests
```

## Adding a New Debuggee

1. Create a folder under `Debuggees/` with a `.csproj` and `Program.cs`
2. The `.csproj` just needs: `<Project Sdk="Microsoft.NET.Sdk" />`
   (inherits OutputType=Exe and TFM from `Directory.Build.props`)
3. `Main()` must return `100` on success
4. Use `[MethodImpl(MethodImplOptions.NoInlining)]` on methods to prevent inlining
5. Use `GC.KeepAlive()` to ensure objects are live at GC stress points
6. Add the debuggee name to `BasicGCStressTests.Debuggees`

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

## Architecture

```
GCStressTestBase.RunGCStress(debuggeeName)
  │
  ├── Locate core_root/corerun (CORE_ROOT env or default path)
  ├── Locate debuggee DLL (artifacts/bin/GCStressTests/<name>/...)
  ├── Start Process: corerun <debuggee.dll>
  │     Environment:
  │       DOTNET_GCStress=0x24
  │       DOTNET_GCStressCdacStep=1
  │       DOTNET_GCStressCdacLogFile=<temp file>
  │       DOTNET_ContinueOnAssert=1
  ├── Wait for exit (timeout: 300s)
  ├── Parse results log → GCStressResults
  └── Assert: exit=100, pass rate ≥ 99.9%
```
