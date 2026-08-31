# NativeAOT incremental compilation experiment

This directory documents a disabled-by-default, internal NativeAOT experiment. It is not a
supported product feature. The implementation recompiles a prevalidated finite set of method
bodies in one retained compiler process and patches copies of the original Windows COFF object.
It does not re-emit the dependency graph or object file.

## Activation

Set all three environment variables for one `ilc` invocation:

```text
DOTNET_ILC_INCREMENTAL=1
DOTNET_ILC_INCREMENTAL_UPDATED_ASSEMBLIES=<assembly>[;<assembly>...]
DOTNET_ILC_INCREMENTAL_OUTPUT_OBJECTS=<object>[;<object>...]
```

The two lists use the platform path separator and must have the same nonzero length. All updates
are validated before the clean baseline object is emitted. Each requested output must not exist.
An unsupported or failed update prints `ILC_INCREMENTAL_REJECTED`, exits with code 85, and removes
all incremental outputs it created. The experiment never silently falls back. A generic driver
owns fallback: after exit 85 it starts a new process with all `DOTNET_ILC_INCREMENTAL*` variables
removed. Other exit codes are ordinary compiler failures and must not be treated as clean-fallback
requests.

## Deliberately narrow envelope

The experiment rejects configurations outside all of these conditions:

- Windows x64 NativeAOT COFF, one primary input, and single-file compilation.
- `OptimizationMode.None` and exactly one compilation thread.
- Scanner, custom inlining, preinitialization, method-body folding, native debug information,
  profile data, custom ordering, custom JIT options/path, CFG, resilience, dehydration, and
  DWARF options disabled.
- Export files, dynamic or generated unmanaged exports, compiler and scanner DGML (including full
  dependency logs), IL dumps, map/mstat/SourceLink/metadata logs, repro packages, and reachability
  modes disabled.
- Default method and file layouts and only unwind-info object writing.
- Changed methods are non-generic, non-constructor leaf methods with exactly one marked
  `MethodCodeNode`; no unboxing thunk, conditional dependency, folding eligibility, or EH.
- The PE length, MVID, metadata method count, every non-body byte, and every method-body shape
  field remain identical after masking only timestamp, checksum, strong-name, debug-directory,
  and encoded body ranges.
- Opcode streams are identical and contain only argument loads, constants, arithmetic,
  bitwise/shift, simple conversions, and `ret`; only explicit integer or floating-point constant
  operands may change.
- Selected COFF fragments retain exact size, alignment, symbols, relocations, addends, GC info,
  frames, EH state, debug state, ordered dependencies, reasons, and marked state. Only the
  explicit Windows x64 relocation-width allowlist is accepted. COMDAT, duplicate, overlapping,
  or out-of-bounds ranges are rejected.

The baseline assembly bytes come from the primary input file and are byte-verified against the
`EcmaModule` already used by the graph. The immutable configuration fingerprint includes typed
target/generics/ISA/compiler-policy values, every
primary and reference input hash, relevant process environment, and a hash of the complete parsed
command-token stream. The emitted object is bound by length and SHA-256, reopened once for
verification, and retained under a non-writable, non-deletable handle. Every output is copied from
that same verified handle, patched into a unique same-directory file, and flushed to disk.

All requested updates are staged before any final path is visible. Publication then uses a
non-overwriting atomic rename for each file. If any rename fails, the compiler poisons the session,
attempts to delete every staged and already-published output, and reports every cleanup failure.
Filesystems do not provide a cross-file rename transaction: an external lock or permissions change
that also prevents rollback can leave part of a multi-file batch visible, and this is reported
loudly rather than hidden. Compiler diagnostics are finalized before publication, and a later
failure to publish the baseline object also triggers update-output cleanup. Incremental mode also
requires the baseline final path to be absent and publishes it without overwrite, so filesystem
aliases cannot silently replace an update output.

Every update starts from the immutable original object. For sequential updates, the dirty set is
the union of methods changed by the current and immediately preceding request. This makes
edit/different-method/revert sequences restore baseline bytes instead of accumulating prior
patches. Any failure after IL provider replacement poisons the session permanently.

## Differential validation

The `Determinism` smoke-test project contains an opt-in tiny fixture. The harness changes one IL
constant without changing the PE identity or body shape, performs an edit and revert in one
retained compilation, independently clean-compiles the edited assembly, and compares complete
object-file SHA-256 hashes.

After building `ILCompiler_publish`, run on Windows x64:

```powershell
$env:RunIncrementalCompilationExperiment = 'true'
src\tests\build.cmd nativeaot Release test nativeaot\SmokeTests\Determinism\Determinism.csproj
Remove-Item Env:RunIncrementalCompilationExperiment
```

The harness writes
`artifacts\tests\coreclr\windows.x64.Release\nativeaot\SmokeTests\Determinism\Determinism\incremental-differential\run.log`
and the four compared objects beside it. A local validation run took 3,869.293 ms for the clean
baseline plus retained edit/revert and 2,575.470 ms for the independent clean edited compilation.
These end-to-end totals validate correctness and do not measure isolated update latency. The
updated objects both hashed
`F65A302648638A253D25BB3A67407023531BDFC8C3846B1F650E9F45E967CE52`; the baseline and reverted
objects both hashed `561072F6353A6AF6360AB259C9DD3C0CCB20D1B0D30E6123A71B1C63A1B79F89`.

## Original experiment evidence

The motivating v10.0.11 experiment reported **291.837 ms** incremental versus **640.395 s**
clean (**2,194x**) for its measured case. Its output SHA-256 was
`B3140045782498DC4A06F712C2DAA329B732D6340DFCD3D80AD4181E17844206`; it reused
**13,455,307 / 13,455,308** nodes, changed one byte, and produced a **1.57 MB** object. The first
clean baseline was still required and retained **34–36 GiB** of state.

Those measurements describe one experimental workload and do not establish general performance,
memory, correctness, or determinism characteristics.

## Not included and productization gaps

The implementation intentionally excludes the v10 profiling recorder, RDM-specific drivers,
candidate/report scripts and data, clean-build optimization waves, dependency-graph
randomization, table-preservation changes, general relocation cloning, and full object
re-emission.

Before productization this would need a supported command-line/API contract, broader target and
method coverage, cross-process cache serialization, linker-level differential and stress
coverage, configuration-schema versioning, memory-lifetime controls, diagnostics, telemetry,
and a supported clean-fallback owner. The experiment's cross-assembly calls use fail-loud
reflection shims because the compiler assemblies compile overlapping linked internal sources;
productization requires direct, compile-time-checked internal seams and removal of those shims.
It currently retains the complete clean compiler graph and requires callers to perform any clean
differential comparison separately.
