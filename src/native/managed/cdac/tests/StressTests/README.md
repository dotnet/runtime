# cDAC Stress Tests

Integration tests that verify the cDAC's stack reference enumeration matches the runtime's
GC root scanning under GC stress conditions.

See [known-issues.md](known-issues.md) for the current pass/fail matrix and a catalog of
investigated gaps.

## Quickstart (Windows x64)

From the repo root:

```powershell
# 1. Build CoreCLR + cDAC (Checked is recommended; Debug also works)
.\build.cmd -subset clr.native+tools.cdac -c Checked -rc Checked -lc Release

# 2. Generate the Core_Root layout the debuggees run against
.\src\tests\build.cmd Checked generatelayoutonly -SkipRestorePackages /p:LibrariesConfiguration=Release

# 3. Run the stress suite (debuggees auto-built; allocation-point verification)
.\src\native\managed\cdac\tests\StressTests\RunStressTests.ps1 -SkipBuild -Configuration Checked
```

Equivalent on Linux/macOS: replace `.cmd` with `.sh` and invoke `pwsh ./RunStressTests.ps1 ...`.

## How It Works

Each test runs a debuggee console app under `corerun` with `DOTNET_CdacStress` set, which
turns on hooks in `src/coreclr/vm/cdacstress.cpp`. The native hook:

1. Walks the stack at each managed allocation (the only trigger point currently wired —
   `gchelpers.cpp` call sites; the historical `gccover.cpp` instruction-level hooks
   have been removed).
2. Compares cDAC's `GetStackReferences` output against the runtime's own GC root
   enumeration (the single oracle).
3. Writes structured per-frame results (with resolved method names) to
   `DOTNET_CdacStressLogFile`.

### `DOTNET_CdacStress` flag layout

The DWORD is split into byte-wide regions:

| Byte | Region   | Bits        | Meaning                                       |
|------|----------|-------------|-----------------------------------------------|
| 0    | WHERE    | `0x000000FF`| Trigger points -- when the harness fires      |
| 1    | WHAT     | `0x0000FF00`| Sub-checks -- which comparison runs           |
| 2    | MODIFIERS| `0x00FF0000`| Output / behavior knobs                       |

A useful configuration sets at least one WHERE and at least one WHAT bit.

| Bits         | Region   | Name      | Meaning                                                                      |
|--------------|----------|-----------|------------------------------------------------------------------------------|
| `0x00000001` | WHERE    | ALLOC     | Verify at every managed allocation (`gchelpers.cpp`)                         |
| `0x00000100` | WHAT     | GCREFS    | Compare cDAC `GetStackReferences` vs runtime GC root oracle                  |
| `0x00000200` | WHAT     | ARGITER   | Compare cDAC `CallingConvention.EnumerateArguments`-derived GCRefMap blobs vs runtime `ComputeCallRefMap` byte-for-byte (`[ARG_PASS]` / `[ARG_FAIL]` / `[ARG_SKIP]` / `[ARG_ERROR]` per MD, with a `[ARG_STATS]` summary at shutdown) |
| `0x00010000` | MODIFIER | VERBOSE   | Rich per-ref diagnostics in the log                                          |

Common combinations:
- `0x00101` -- ALLOC + GCREFS (default for `RunStressTests.ps1` and `GCStress_*` xunit theories)
- `0x00201` -- ALLOC + ARGITER (default for `ArgIterStress_*` xunit theories; independent run on the same Helix build so the two sub-checks don't share state)
- `0x00301` -- ALLOC + GCREFS + ARGITER (validates both sub-checks in one process)
- `0x10101` -- ALLOC + GCREFS + VERBOSE (use when triaging a GCREFS mismatch)

### Per-sub-check summary markers

The native harness emits one machine-readable line per enabled sub-check at
shutdown, parsed by `CdacStressResults`:

- `[GC_STATS] verifications=N pass=N fail=N known_issue=N` -- emitted iff GCREFS ran
- `[ARG_STATS] pass=N fail=N skip=N error=N` -- emitted iff ARGITER ran

Both lines are gated on their respective `IsCdacStress*Enabled()` helpers, so a
pure-ARGITER run does not produce `[GC_STATS]` and vice versa. The xunit
`AssertAll*Passed` helpers use the presence of the marker (`AnyGcRefsRecorded`
/ `AnyArgIterRecorded`) to distinguish "sub-check did not run" from "ran but
recorded zero verifications".

### Pass/fail semantics in the log

- **[PASS]** — cDAC matches the runtime
- **[KNOWN_ISSUE]** — cDAC differs, but every diff is on a Frame the cDAC explicitly
  marked as deferred (e.g. `PromoteCallerStack` not yet ported for that transition type)
- **[FAIL]** — cDAC differs from the runtime on a Frame that *should* be implemented,
  or cDAC's `GetStackReferences` failed at the API boundary

See [known-issues.md § Log Format](known-issues.md#log-format) for the per-frame log shape.

## Running Tests

### Using `RunStressTests.ps1` (recommended for local dev)

```powershell
# Run all debuggees (allocation-point verification, no GCStress)
.\RunStressTests.ps1 -SkipBuild -Configuration Checked

# Run a single debuggee
.\RunStressTests.ps1 -SkipBuild -Configuration Checked -Debuggee BasicAlloc

# Run with verbose per-ref diagnostics (use when triaging a mismatch)
.\RunStressTests.ps1 -SkipBuild -Configuration Checked -CdacStress 0x10101
```

Logs land under
`artifacts\tests\coreclr\<os>.<arch>.<config>\Tests\cdacstresslogs\<debuggee>.log`.

### Using `dotnet test` (xUnit harness — same path CI runs)

The xUnit harness defaults to `DOTNET_CdacStress=0x101` (ALLOC + GCREFS).

```powershell
# Build and run all stress tests
.\.dotnet\dotnet.exe test src\native\managed\cdac\tests\StressTests

# Run a specific debuggee
.\.dotnet\dotnet.exe test src\native\managed\cdac\tests\StressTests --filter "FullyQualifiedName~BasicAlloc"

# Override CdacStress flags for a single run (e.g. enable verbose diagnostics)
$env:DOTNET_CdacStress = "0x10101"
.\.dotnet\dotnet.exe test src\native\managed\cdac\tests\StressTests

# Point at an existing Core_Root explicitly
$env:CORE_ROOT = "path\to\Core_Root"
.\.dotnet\dotnet.exe test src\native\managed\cdac\tests\StressTests
```

## Triaging Failures

1. Open the per-debuggee log (`<debuggee>.log`).
2. Search for `^\[FAIL\]` to find failing verifications.
3. Each failure prints `[STACK_TRACE]` with `cDAC=X RT=Y` per frame; the `[<-- MISMATCH]`
   marker pinpoints the offending frame.
4. Cross-check against [known-issues.md](known-issues.md) — the gap may already be tracked.
5. To reproduce in a debugger, rerun the single debuggee under `corerun` with the same
   `DOTNET_CdacStress` value and attach.

## Adding a New Debuggee

1. Create a folder under `Debuggees/` with a `.csproj` and `Program.cs`
2. The `.csproj` just needs: `<Project Sdk="Microsoft.NET.Sdk" />`
   (inherits OutputType=Exe and TFM from `Directory.Build.props`)
3. `Main()` must return `100` on success
4. Use `[MethodImpl(MethodImplOptions.NoInlining)]` on methods to prevent inlining
5. Use `GC.KeepAlive()` to ensure objects are live at GC stress points
6. Add the debuggee name to `CdacStressTests.Debuggees`

## Debuggee Catalog

| Debuggee | Scenarios |
|----------|-----------|
| **BasicAlloc** | Objects, strings, arrays, many live refs |
| **ExceptionHandling** | try/catch/finally funclets, nested exceptions, filter funclets, rethrow |
| **DeepStack** | Deep recursion with live refs at each frame |
| **Generics** | Generic method instantiations, interface dispatch, delegates |
| **PInvoke** | P/Invoke transitions, pinned GC handles, struct with object refs (Windows-only) |
| **MultiThread** | Concurrent threads with synchronized GC stress |
| **Comprehensive** | All-in-one: every scenario in a single run |
| **StructScenarios** | Struct returns, by-ref params |
| **DynamicMethods** | DynamicMethod / IL emit |
| **CallSignatures** | Wide signature surface for the ARGITER sub-check (primitives, byref/ptr, structs, generics) |
| **CrossModule** | Calls across multiple assemblies exercising cross-module type references |
| **VarArgs** | `__arglist` / VASigCookie validation for ARGITER (Windows x86/x64/ARM64 only; excluded from GCREFS until GetStackReferences walks the cookie signature) |

## Architecture

```
CdacStressTestBase.RunGCStressAsync(debuggeeName)
  │
  ├── Locate core_root/corerun (CORE_ROOT env or default path)
  ├── Locate debuggee DLL (artifacts/bin/StressTests/<name>/...)
  ├── Start Process: corerun <debuggee.dll>
  │     Environment:
  │       DOTNET_CdacStress=0x101
  │       DOTNET_CdacStressLogFile=<temp file>
  │       DOTNET_ContinueOnAssert=1
  ├── Wait for exit (timeout: 300s)
  ├── Parse results log → CdacStressResults
  └── Assert: exit=100, zero failures
```

