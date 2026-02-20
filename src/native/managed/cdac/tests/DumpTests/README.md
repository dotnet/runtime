# cDAC Dump Tests

Integration tests that validate the cDAC (contract-based Data Access Component) reader
against real crash dumps. Each test loads a dump produced by a purpose-built debuggee
app, creates a `ContractDescriptorTarget` via ClrMD, and exercises one or more cDAC
contracts against real runtime data structures.

## Overview

### Architecture

```
Debuggees/          Purpose-built console apps that crash via FailFast
DumpTests.targets   MSBuild logic to build debuggees, run them, and collect dumps
RunDumpTests.ps1    Windows helper script to orchestrate dump generation + test runs
DumpTestBase.cs     Base class: loads a dump, creates a cDAC Target, handles skipping
ClrMdDumpHost.cs    Wraps ClrMD DataTarget for memory reads and symbol lookup
*DumpTests.cs       Test classes organized by cDAC contract
```

### Debuggees

Each debuggee is a small console app under `Debuggees/` that exercises specific runtime
features and then calls `Environment.FailFast()` to produce a crash dump.

| Debuggee | Purpose | Dump Type |
|----------|---------|-----------|
| BasicThreads | Thread management, thread store | Heap |
| ExceptionState | Nested exception chains | Heap |
| GCRoots | GC object graphs, pinned handles | Heap |
| ServerGC | Server GC mode heap structures | Heap |
| StackWalk | Deterministic call stack (Main→A→B→C→FailFast) | Heap |
| MultiModule | Multi-assembly metadata resolution | Full |
| TypeHierarchy | Type inheritance, method tables | Full |

The dump type is configured per-debuggee via the `DumpTypes` property in each debuggee's
`.csproj` (default: `Heap`, set in `Debuggees/Directory.Build.props`). Debuggees that
need full memory content (e.g., metadata-heavy scenarios) override this to `Full`.

### Test Classes

Each test class targets a specific cDAC contract and specifies which debuggee dump to
use. Tests that require full dumps override `DumpType` to `"full"`.

| Test Class | Contract | Debuggee | Dump Type |
|------------|----------|----------|-----------|
| ThreadDumpTests | Thread | BasicThreads | heap |
| RuntimeInfoDumpTests | RuntimeInfo | BasicThreads | heap |
| ExceptionDumpTests | Exception | ExceptionState | heap |
| ObjectDumpTests | Object | GCRoots | heap |
| ServerGCDumpTests | GCHeap | ServerGC | heap |
| StackWalkDumpTests | StackWalk | StackWalk | heap |
| RuntimeTypeSystemDumpTests | RuntimeTypeSystem | TypeHierarchy | full |
| LoaderDumpTests | Loader | MultiModule | full |
| EcmaMetadataDumpTests | EcmaMetadata | MultiModule | full |

### Runtime Versions

Tests run against two runtime versions:

- **`local`** — Framework-dependent build run with the repo's testhost (the runtime
  you just built). Used for validating current development work.
- **`net10.0`** — Self-contained publish against the released .NET 10 runtime. Used
  for cross-version compatibility testing.

Each test class has `_Local` and `_Net10` subclasses that set the `RuntimeVersion`.

> **Note:** .NET 10 did not support cDAC in heap dumps. Heap dump tests for net10.0
> are automatically skipped. Only full dump tests (RuntimeTypeSystem, Loader,
> EcmaMetadata) run against net10.0.

### Dump Directory Layout

Dumps are written to:

```
artifacts/dumps/cdac/{version}/{dumptype}/{debuggee}/{debuggee}.dmp
```

For example:

```
artifacts/dumps/cdac/
  local/
    heap/
      BasicThreads/BasicThreads.dmp
      ExceptionState/ExceptionState.dmp
    full/
      TypeHierarchy/TypeHierarchy.dmp
      MultiModule/MultiModule.dmp
  net10.0/
    full/
      TypeHierarchy/TypeHierarchy.dmp
      MultiModule/MultiModule.dmp
```

## Running Locally (Windows)

### Prerequisites

1. Build the runtime and test host:

   ```cmd
   build.cmd clr+libs+tools.cdac -rc release
   ```

2. Ensure the repo dotnet (`.dotnet/dotnet.exe`) is available.

### Using RunDumpTests.ps1

The `RunDumpTests.ps1` script is the recommended way to run locally on Windows.
Run it from the `DumpTests/` directory.

```powershell
# Generate dumps and run all tests
.\RunDumpTests.ps1

# Generate dumps only (no tests)
.\RunDumpTests.ps1 -Action dumps

# Run tests only (dumps must already exist)
.\RunDumpTests.ps1 -Action test

# Target a specific runtime version
.\RunDumpTests.ps1 -Versions local

# Force regeneration of existing dumps
.\RunDumpTests.ps1 -Force

# Filter tests by name
.\RunDumpTests.ps1 -Filter "*StackWalk*"
```

> **Admin Note (Windows):** Heap dumps require the DAC (`mscordaccore.dll`), which is
> unsigned in local builds. The script and MSBuild targets automatically set the
> `DisableAuxProviderSignatureCheck` registry key under
> `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\MiniDumpSettings`.
> This may require running as Administrator on the first invocation.

### Using MSBuild Directly

You can also invoke dump generation directly via MSBuild:

```cmd
.dotnet\dotnet msbuild src\native\managed\cdac\tests\DumpTests\Microsoft.Diagnostics.DataContractReader.DumpTests.csproj /t:GenerateAllDumps /v:minimal
```

And run the tests:

```cmd
.dotnet\dotnet test src\native\managed\cdac\tests\DumpTests\Microsoft.Diagnostics.DataContractReader.DumpTests.csproj
```

### Using a CI Dump Archive

If you have a `.tar.gz` archive of dumps from CI, you can run tests against them
without generating dumps locally:

```powershell
.\RunDumpTests.ps1 -DumpArchive C:\Downloads\CdacDumps_linux_x64.tar.gz
```

This extracts the archive, auto-detects which runtime versions are present, and
runs the tests with `CDAC_DUMP_ROOT` pointing to the extracted dumps.

## CI Pipeline

The CI pipeline is defined in `eng/pipelines/runtime-diagnostics.yml` and runs on
a schedule (nightly) and on PRs that touch `src/native/managed/cdac/**` or
`eng/pipelines/**`.

### Current CI Behavior

CI currently generates **local dumps only** (`CIDumpVersionsOnly=true`). The net10.0
self-contained publish requires a separate .NET 10 SDK which is not available on CI
agents.

The pipeline has three stages:

1. **Build** — Builds the runtime (`clr+libs+tools.cdac+host+packs`) and runs the
   standard diagnostics test suite (with and without cDAC).

2. **DumpCreation** — Builds the runtime, invokes `GenerateAllDumps` to create crash
   dumps for each debuggee, and publishes the dumps as pipeline artifacts. Runs on
   each platform in `cdacDumpPlatforms` (currently `windows_x64` and `linux_x64`).

3. **DumpTest** — Downloads dump artifacts from all platforms and runs the test suite
   against each set of dumps. This enables cross-platform validation (e.g., running
   Linux dump analysis on a Windows host). Tests for net10.0 are skipped via
   `SkipDumpVersions=net10.0`.

### Future Plans

The dump tests are planned to move to **Helix** for broader platform coverage and
better integration with the existing test infrastructure. This would allow running
dump generation and analysis across a wider matrix of OS/architecture combinations
without adding load to the main CI pool.

## Adding a New Debuggee

1. Create a new directory under `Debuggees/` with a `.csproj` and `Program.cs`.
2. The app should exercise the runtime feature you want to test, then call
   `Environment.FailFast("message")` to trigger a crash dump.
3. The `.csproj` inherits defaults from `Debuggees/Directory.Build.props`
   (output path, target frameworks, `DumpTypes=Heap`).
4. Override `<DumpTypes>Full</DumpTypes>` if your test needs full memory dumps.
5. The debuggee is auto-discovered by `DumpTests.targets` — no list to update.

## Adding a New Test

1. Create a new test class inheriting from `DumpTestBase`.
2. Override `DebuggeeName` to match the debuggee directory name.
3. Override `DumpType` if the debuggee uses full dumps (`"full"`).
4. Create `_Local` and `_Net10` subclasses that set `RuntimeVersion`.
5. Use `[ConditionalFact]` and `SkipIfVersion()`/`SkipIfTargetOS()` for
   conditional test skipping.
