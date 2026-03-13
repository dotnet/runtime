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

### Setting up a solution

For VS Code and Visual Studio, create a file `cdac.slnx` in the runtime repo root to bring
all the cDAC projects into scope:

```xml
<Solution>
  <Configurations>
    <Platform Name="Any CPU" />
    <Platform Name="x64" />
    <Platform Name="x86" />
  </Configurations>
  <Folder Name="/cdac/">
    <Project Path="src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Abstractions/Microsoft.Diagnostics.DataContractReader.Abstractions.csproj" />
    <Project Path="src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Microsoft.Diagnostics.DataContractReader.Contracts.csproj" />
    <Project Path="src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader/Microsoft.Diagnostics.DataContractReader.csproj" />
    <Project Path="src/native/managed/cdac/mscordaccore_universal/mscordaccore_universal.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="src/native/managed/cdac/tests/Microsoft.Diagnostics.DataContractReader.Tests.csproj" />
  </Folder>
</Solution>
```

In VS Code, run the ".NET: Open Solution" command and select `cdac.slnx`. In Visual Studio,
open the solution file directly. You can then use Test Explorer to run and debug tests.

### Running unit tests from the command line

Use the `dotnet.sh` (or `dotnet.cmd`) script in the repo root:

```bash
./dotnet.sh build /t:Test \
  src/native/managed/cdac/tests/Microsoft.Diagnostics.DataContractReader.Tests.csproj \
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

Create a PowerShell script `debug.ps1` to launch WinDbg with the cDAC enabled:

```powershell
$env:DOTNET_ENABLE_CDAC=1
windbgx C:\runtime\artifacts\bin\testhost\net10.0-windows-Debug-x64\dotnet.exe .\bin\Debug\net9.0\helloworld.dll
```

Replace `C:\runtime` with your runtime repo checkout path. You can also use `corerun.exe`
with a CORE_ROOT directory instead of the testhost `dotnet.exe`.

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
exercise the cDAC end-to-end against a live .NET process. These tests can run in two modes:
with the legacy DAC or with the cDAC enabled.

### How cDAC is activated

`SOSDacImpl` has `#if DEBUG` cross-validation that compares cDAC results against the legacy
DAC. To enable this, build the cDAC in Debug configuration while everything else can be
Release. Note that some legacy calls must run outside `#if DEBUG` when their results are
used functionally (not just for validation) — see the
[Legacy project README](Microsoft.Diagnostics.DataContractReader.Legacy/README.md) for
details.

At runtime, the DAC checks the `ENABLE_CDAC` config knob
([daccess.cpp](/src/coreclr/debug/daccess/daccess.cpp)). When set to `1`, it looks up the
`DotNetRuntimeContractDescriptor` symbol in the target process, creates the managed cDAC
interface via `mscordaccore_universal`, and routes SOS queries through it.

### Building the runtime for SOS testing

Build from the runtime repo root:

```bash
./build.sh clr+clr.hosts+libs+tools.cdac -c Debug -lc Release
```

The debug build of the runtime (`-rc Debug`, which is the default when `-c Debug` is used)
is required for the brittle DAC to delegate to the cDAC. Release build of the libraries
(`-lc Release`) is highly recommended for a faster inner loop.

Once the initial build is done, shorter incremental rebuilds can be done with:

```bash
./build.sh clr.native+tools.cdac -c Debug -lc Release
```

This produces a testhost at:
`artifacts/bin/testhost/net<version>-<os>-Debug-<arch>/shared/Microsoft.NETCore.App/<version>/`

### Running SOS tests in the diagnostics repo

See [privatebuildtesting.md](https://github.com/dotnet/diagnostics/blob/main/documentation/privatebuildtesting.md)
in the diagnostics repo for the full procedure. The key steps are:

```bash
# Build managed code (skip native if already built)
./eng/build.sh -c Release --restore --build -skipnative

# Install test runtimes, overlay your local build, and run tests with cDAC
./eng/build.sh -c Release -test -useCdac -privatebuild -installruntimes \
  -liveRuntimeDir <path-to-testhost-shared-framework>
```

The `-useCdac` flag sets `SOS_TEST_CDAC=true`, which causes the test runner (`SOSRunner.cs`)
to set `DOTNET_ENABLE_CDAC=1` on each test process.

### CI pipeline

The `runtime-diagnostics.yml` pipeline runs the SOS tests automatically on every PR that
touches `src/native/managed/cdac/**` or `src/coreclr/debug/runtimeinfo/**`. It runs the
tests twice — once with `-useCdac` (cDAC path) and once without (legacy DAC path) — on
Windows x64.

> **Note:** The runtime and diagnostics repos must be on the same major version. CLRMD
> validates the DAC binary version against the runtime, so a cross-major-version mismatch
> (e.g., 11.0 runtime with 10.0 diagnostics repo) causes `CreateDacInstance` failures.
