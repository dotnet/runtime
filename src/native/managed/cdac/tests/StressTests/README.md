# cDAC Stress Tests

This folder contains stress tests that verify the cDAC's stack reference
enumeration against the runtime's GC root scanning. The tests run managed
debuggee applications under `corerun` with cDAC stress flags enabled,
triggering verification at allocation points, GC points, or instruction-level
GC stress points.

## Quick Start

```powershell
# Prerequisites: build CoreCLR Checked and generate core_root
#   build.cmd clr+libs -rc Checked -lc Release
#   src\tests\build.cmd Checked generatelayoutonly /p:LibrariesConfiguration=Release

# Run all debuggees (allocation-point verification, no GCStress)
./RunStressTests.ps1 -SkipBuild

# Run a single debuggee
./RunStressTests.ps1 -SkipBuild -Debuggee BasicAlloc

# Run with instruction-level GCStress (slower, more thorough)
./RunStressTests.ps1 -SkipBuild -CdacStress 0x14 -GCStress 0x4

# Full comparison including walk parity and DAC cross-check
./RunStressTests.ps1 -SkipBuild -CdacStress 0x74 -GCStress 0x4
```

## How It Works

### DOTNET_CdacStress Flags

The `DOTNET_CdacStress` environment variable is a bitmask that controls
**where** and **what** the runtime verifies:

| Bit | Flag | Description |
|-----|------|-------------|
| 0x1 | ALLOC | Verify at managed allocation points |
| 0x2 | GC | Verify at GC collection points |
| 0x4 | INSTR | Verify at instruction-level GC stress points (requires `DOTNET_GCStress`) |
| 0x10 | REFS | Compare GC stack references (cDAC vs runtime) |
| 0x20 | WALK | Compare stack walk frame ordering (cDAC vs DAC) |
| 0x40 | USE_DAC | Also compare GC refs against the legacy DAC |
| 0x100 | UNIQUE | Only verify each instruction pointer once |

Common combinations:
- `0x11` — ALLOC + REFS (fast, default)
- `0x14` — INSTR + REFS (thorough, requires `DOTNET_GCStress=0x4`)
- `0x31` — ALLOC + REFS + WALK (fast with walk parity check)
- `0x74` — INSTR + REFS + WALK + USE_DAC (full comparison)

### Verification Flow

At each stress point, the native hook (`cdacstress.cpp`) in the runtime:

1. Suspends the current thread's context
2. Calls the cDAC's `GetStackReferences` to enumerate GC roots
3. Compares against the runtime's own GC root enumeration
4. Optionally compares against the legacy DAC's enumeration
5. Optionally compares stack walk frame ordering
6. Logs `[PASS]` or `[FAIL]` per verification point

The script collects these results and reports aggregate pass/fail counts.

## Debuggees

Each debuggee is a standalone console application under `Debuggees/`:

| Debuggee | Scenarios |
|----------|-----------|
| **BasicAlloc** | Object allocation, strings, arrays, many live refs |
| **Comprehensive** | All-in-one: allocations, deep stacks, exceptions, generics, P/Invoke, threading |

All debuggees return exit code 100 on success.

### Adding a New Debuggee

1. Create a new folder under `Debuggees/` (e.g., `Debuggees/MyScenario/`)
2. Add a minimal `.csproj`:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk" />
   ```
   The `Directory.Build.props` provides all common settings.
3. Add a `Program.cs` with a `Main()` that returns 100
4. Use `[MethodImpl(MethodImplOptions.NoInlining)]` and `GC.KeepAlive()`
   to prevent the JIT from optimizing away allocations and references

The script auto-discovers all debuggees by scanning for `.csproj` files.

## Script Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `-Configuration` | `Checked` | Runtime build configuration |
| `-CdacStress` | `0x11` | Hex bitmask for `DOTNET_CdacStress` |
| `-GCStress` | _(empty)_ | Hex value for `DOTNET_GCStress` (e.g., `0x4`) |
| `-Debuggee` | _(all)_ | Which debuggee(s) to run |
| `-SkipBuild` | off | Skip CoreCLR/cDAC build step |
| `-SkipBaseline` | off | Skip baseline (no-stress) verification |

## Expected Results

Most runs achieve >99.5% pass rate. A small number of failures (~0.2%)
are expected due to the ScanFrameRoots gap — the cDAC does not yet enumerate
GC roots from explicit frame stub data (e.g., `StubDispatchFrame`,
`PInvokeCalliFrame`). These are tracked in [known-issues.md](known-issues.md).

Walk parity (`WALK` flag) should show 0 mismatches.
