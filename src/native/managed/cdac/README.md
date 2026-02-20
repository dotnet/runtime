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

## Integration testing with SOS

The [dotnet/diagnostics](https://github.com/dotnet/diagnostics) repo has SOS tests that
exercise the cDAC end-to-end against a live .NET process. These tests can run in two modes:
with the legacy DAC or with the cDAC enabled.

### How cDAC is activated

`SOSDacImpl` has `#if DEBUG` cross-validation that compares cDAC results against the legacy
DAC. To enable this, build the cDAC in Debug configuration while everything else can be Release.

At runtime, the DAC checks the `ENABLE_CDAC` config knob
([daccess.cpp](/src/coreclr/debug/daccess/daccess.cpp)). When set to `1`, it looks up the
`DotNetRuntimeContractDescriptor` symbol in the target process, creates the managed cDAC
interface via `mscordaccore_universal`, and routes SOS queries through it.

### Building the runtime for SOS testing

Build from the runtime repo root with the cDAC in Debug and everything else in Release:

```bash
./build.sh -s clr+libs+tools.cdac+host+packs -c Debug -rc release -lc release
```

This produces a testhost at:
`artifacts/bin/testhost/net<version>-<os>-Release-<arch>/shared/Microsoft.NETCore.App/<version>/`

### Running SOS tests in the diagnostics repo

From the diagnostics repo:

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
> See [privatebuildtesting.md](https://github.com/dotnet/diagnostics/blob/main/documentation/privatebuildtesting.md)
> in the diagnostics repo for the full private build overlay procedure.
