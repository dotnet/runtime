# cDAC (Data Contract Reader)

The cDAC is a managed implementation of the diagnostic data access layer. It enables
diagnostic tools to inspect .NET runtime process state by reading memory through
well-defined data contracts, without requiring version-matched native DAC/DBI libraries.

See [docs/design/datacontracts/datacontracts_design.md](/docs/design/datacontracts/datacontracts_design.md)
for the full design and motivation.

## Architecture

The cDAC has a layered architecture. When implementing or testing, it's important to
understand which layer you're working at:

```
ISOSDacInterface* / IXCLRDataProcess (COM-style API surface)
        │
        ▼
   SOSDacImpl          (Microsoft.Diagnostics.DataContractReader.Legacy)
        │                 Translates COM APIs into contract calls.
        │                 Handles HResult protocols, pointer conversions,
        │                 and #if DEBUG cross-validation with legacy DAC.
        ▼
   Contract interfaces   (Microsoft.Diagnostics.DataContractReader.Contracts)
        │                 e.g., IGC, IThread, ILoader — pure managed APIs
        │                 returning strongly-typed structs.
        ▼
   Data types            (Microsoft.Diagnostics.DataContractReader.Contracts/Data/)
        │                 e.g., Data.Generation, Data.CFinalize — read fields
        │                 from target memory at specified addresses/offsets.
        ▼
   Target memory         (Microsoft.Diagnostics.DataContractReader.Abstractions)
                          ReadPointer, ReadGlobal, ReadNUInt, etc.
```

- **To implement a new SOSDac API**: work in `SOSDacImpl` (Legacy project), calling
  existing contracts. See the [Legacy project README](Microsoft.Diagnostics.DataContractReader.Legacy/README.md).
- **To implement a new contract**: work in the Contracts project. See the
  [contract specifications](/docs/design/datacontracts/) for the data descriptors
  and algorithms each contract must implement.
- **To write tests**: see the [tests README](tests/README.md).

## Project structure

| Directory | Purpose |
|-----------|---------|
| `Microsoft.Diagnostics.DataContractReader.Abstractions` | Core abstractions: `Target`, `TargetPointer`, `DataType`, contract interfaces |
| `Microsoft.Diagnostics.DataContractReader.Contracts` | Contract implementations (e.g., `GC_1`) and data type readers |
| `Microsoft.Diagnostics.DataContractReader.Legacy` | `SOSDacImpl` — bridges `ISOSDacInterface*` COM APIs to contracts |
| `Microsoft.Diagnostics.DataContractReader` | Contract/data descriptor parsing and `Target` construction |
| `mscordaccore_universal` | Entry point that wires everything together |
| `mscordaccore_cdac_validation_shim` | Test-only shim that compares the production cDAC against the legacy DAC (never packaged) |
| `tests` | Unit tests with mock memory infrastructure |

## Contract specifications

Each contract has a specification document in
[docs/design/datacontracts/](/docs/design/datacontracts/) describing:

- The API surface (C# structs and methods)
- Data descriptors (type layouts and field offsets)
- Global variables (with types and which GC mode they apply to)
- Algorithmic pseudo-code for the implementation

Key specs: [GC](/docs/design/datacontracts/GC.md) ·
[Thread](/docs/design/datacontracts/Thread.md) ·
[Loader](/docs/design/datacontracts/Loader.md) ·
[RuntimeTypeSystem](/docs/design/datacontracts/RuntimeTypeSystem.md)

## Unit testing

### Opening the solution

The [`cdac.slnx`](cdac.slnx) solution file in this directory brings all cDAC projects and
tests into scope. In VS Code, run the ".NET: Open Solution" command and select
`src/native/managed/cdac/cdac.slnx`. In Visual Studio, open the file directly. You can then
use Test Explorer to run and debug tests.

### Running unit tests from the command line

Use the `dotnet.sh` (or `dotnet.cmd`) script in the repo root:

```bash
./dotnet.sh build /t:Test \
  src/native/managed/cdac/tests/UnitTests/Microsoft.Diagnostics.DataContractReader.Tests.csproj \
  -c Debug -p:RuntimeConfiguration=Debug -p:LibrariesConfiguration=Release
```

> **Note:** If you mix release libraries and a debug runtime, you must pass both
> `-p:RuntimeConfiguration=Debug` and `-p:LibrariesConfiguration=Release` so the test
> project resolves the correct shared framework. If everything is Debug, then just
> `-c Debug` is sufficient.

## End-to-end testing with WinDbg

### Building a sample app

Create a hello-world app to use as a debugger target:

```cmd
cd C:\helloworld
dotnet new console -f net9.0
```

Add `<RollForward>LatestMajor</RollForward>` to the `.csproj` `<PropertyGroup>` so it can
run on a .NET 10+ checkout. Add a `Console.ReadKey()` in `Program.cs` to keep the process
alive while debugging.

Create a PowerShell script `debug.ps1` to launch WinDbg:

```powershell
windbgx C:\runtime\artifacts\bin\testhost\net10.0-windows-Debug-x64\dotnet.exe .\bin\Debug\net9.0\helloworld.dll
```

Replace `C:\runtime` with your runtime repo checkout path. You can also use `corerun.exe`
with a CORE_ROOT directory instead of the testhost `dotnet.exe`.

SOS decides whether to load the cDAC on its own (`runtimes --usecdac true` forces it). To
point SOS at a specific cDAC binary — the one you just built, or the validation shim — set
`DOTNET_CDAC_PATH` to its full path before launching the debugger.

### Debugging the cDAC with Visual Studio

1. Run `debug.ps1` from above.
2. In WinDbg, hit Run and wait for the app to reach the `Console.ReadKey()` pause.
3. Open Visual Studio and select "Attach to process".
4. Attach to the `enghost.exe` process with mixed native and managed debugging.
5. Set breakpoints in `request.cpp` (native DAC) or `SOSDacImpl.cs` (managed cDAC).

### Useful SOS commands for testing

| Command | What it exercises |
|---------|-------------------|
| `!clrthreads` | Thread enumeration APIs |
| `!dumpstack` | Stack walking — calls many SOS APIs in `request.cpp` |
| `!dso` / `!dumpstackobjects` | Object inspection for specific object types |

Click on thread hyperlinks from `!clrthreads` output to switch the active thread before
running `!dumpstack`.

## Integration testing with SOS

The [dotnet/diagnostics](https://github.com/dotnet/diagnostics) repo has SOS tests that
exercise the cDAC end-to-end against a live .NET process. The `-dacMode` build argument
selects which data-access implementation SOS loads:

| `-dacMode` | What SOS loads |
|------------|----------------|
| `dac`      | The legacy in-box DAC (`mscordaccore`) only. |
| `cdac`     | The standalone production cDAC (`mscordaccore_universal`) only. |
| `cdacfallback` | The [validation shim](mscordaccore_cdac_validation_shim/README.md), comparing the production cDAC against the legacy DAC; unimplemented cDAC APIs delegate to the legacy DAC. |
| `cdacverify`   | The validation shim in strict mode: only the allowlisted APIs may delegate. |

### How the cDAC is activated

The cDAC is a standalone module. Nothing in the runtime hosts it: SOS loads
`mscordaccore_universal` itself, hands it an `ICLRDataTarget`, and calls
`CLRDataCreateInstance`. `DOTNET_CDAC_PATH` overrides which binary SOS loads, which is how
the validation shim is substituted for the production cDAC in the two comparison modes.

The cDAC no longer contains any legacy-DAC comparison code. All comparison lives in the
validation shim, which is built only for testing and is never packaged.

### Building the runtime for SOS testing

Build from the runtime repo root:

```bash
./build.sh clr+clr.hosts+libs+tools.cdac -c Debug -lc Release
```

Add `tools.cdacvalidationshim` when you want to run the `cdacfallback` or `cdacverify`
modes:

```bash
./build.sh clr+clr.hosts+libs+tools.cdac+tools.cdacvalidationshim -c Debug -lc Release
```

Release build of the libraries (`-lc Release`) is highly recommended for a faster inner
loop.

Once the initial build is done, shorter incremental rebuilds can be done with:

```bash
./build.sh clr.native+tools.cdac -c Debug -lc Release
```

This produces a testhost at:
`artifacts/bin/testhost/net<version>-<os>-Debug-<arch>/shared/Microsoft.NETCore.App/<version>/`

The cDAC and the shim are published outside the testhost, at
`artifacts/bin/mscordaccore_universal/<config>/<rid>/publish/` and
`artifacts/bin/mscordaccore_cdac_validation_shim/<config>/<rid>/publish/`.

### Running SOS tests in the diagnostics repo

See [privatebuildtesting.md](https://github.com/dotnet/diagnostics/blob/main/documentation/privatebuildtesting.md)
in the diagnostics repo for the full procedure. The key steps are:

```bash
# Build managed code (skip native if already built)
./eng/build.sh -c Release --restore --build -skipnative

# Standalone cDAC
./eng/build.sh -c Release -test -privatebuild -installruntimes \
  -dacMode cdac \
  -cdacPath <runtime>/artifacts/bin/mscordaccore_universal/<config>/<rid>/publish/libmscordaccore_universal.so \
  -liveRuntimeDir <path-to-testhost-shared-framework>

# Validation shim (fallback mode; use -dacMode cdacverify for strict mode)
./eng/build.sh -c Release -test -privatebuild -installruntimes \
  -dacMode cdacfallback \
  -cdacPath <runtime>/artifacts/bin/mscordaccore_universal/<config>/<rid>/publish/libmscordaccore_universal.so \
  -shimPath <runtime>/artifacts/bin/mscordaccore_cdac_validation_shim/<config>/<rid>/publish/libmscordaccore_cdac_validation_shim.so \
  -legacyDacPath <path-to-testhost-shared-framework>/libmscordaccore.so \
  -liveRuntimeDir <path-to-testhost-shared-framework>
```

The shim must be deployed next to the production cDAC, because it loads the cDAC from its
own directory. `eng/build.*` in the diagnostics repo does that: `-cdacPath` and `-shimPath`
both copy into the SOS native binaries directory, and the test harness then sets
`DOTNET_CDAC_PATH` (to the shim), `DOTNET_CDAC_LEGACY_DAC_PATH` and
`DOTNET_CDAC_VALIDATION_MODE` on each debugger process.

### CI pipeline

The `runtime-diagnostics.yml` pipeline runs the SOS tests automatically on every PR that
touches `src/native/managed/cdac/**` or `src/coreclr/debug/runtimeinfo/**`. The `SOSTests`
stage runs four legs on Windows x64 over one shared build: `cDAC`, `cDAC_fallback`,
`cDAC_verify` and `DAC`.

> **Note:** The runtime and diagnostics repos must be on the same major version. CLRMD
> validates the DAC binary version against the runtime, so a cross-major-version mismatch
> (e.g., 11.0 runtime with 10.0 diagnostics repo) causes `CreateDacInstance` failures.
