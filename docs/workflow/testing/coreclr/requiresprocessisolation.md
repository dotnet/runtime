# When to Set `<RequiresProcessIsolation>true</RequiresProcessIsolation>`

`RequiresProcessIsolation` prevents a test project from being merged into a shared
test runner process. When set, the test is built as a standalone executable and
executed in its own process. A test project must set this property when any of the
rules below apply.

## Rules

### 1. Project sets `<CLRTestEnvironmentVariable>` or `<CLRTestBatchEnvironmentVariable>`

These items set environment variables for the test process. Environment variables are
process-wide state, so the test cannot share a process with other tests that may
need different values.

### 2. Project sets `<RuntimeHostConfigurationOption>`

Runtime host configuration options are read once at process startup and apply to the
entire process. Tests that configure the runtime host must run in isolation.

### 3. Project sets `<AutoreleasePoolSupport>true</AutoreleasePoolSupport>`

This enables the Objective-C autorelease pool at the process level.

### 4. Project sets `<TrimMode>`

Trimming settings change how the test is compiled and linked. A trimmed test cannot
be merged into a non-trimmed runner.

### 5. Project contains `<CMakeProjectReference>`

The test depends on a native C/C++ library built from a CMake project. The native
binary must be discoverable in the test's output directory, which is not guaranteed
when tests are merged into a shared runner folder.

### 6. Project includes `<Content>` items copied to the output directory

If the project copies files to its output directory (via `<Content>` or `<None>` with
`CopyToOutputDirectory`), those files may conflict with files from other tests or may
not be found when tests are merged.

### 7. Project sets `<GCStressIncompatible>true</GCStressIncompatible>`

The test execution script checks `DOTNET_GCStress` and skips the test when GC stress
is enabled. This skip logic runs per-process, so the test needs its own process to be
independently skippable.

### 8. Project sets `<HeapVerifyIncompatible>true</HeapVerifyIncompatible>`

Same pattern as `GCStressIncompatible`. The test is skipped when `DOTNET_HeapVerify`
is set, and the skip check requires its own process.

### 9. Project sets `<JitOptimizationSensitive>true</JitOptimizationSensitive>`

The test is sensitive to JIT optimization levels and is skipped when JIT stress modes,
min-opts, or tiered compilation are active. The skip check is per-process.

### 10. Project sets `<SuperPMICollectIncompatible>true</SuperPMICollectIncompatible>`

The test is skipped during SuperPMI collection runs. The skip check is per-process.

### 11. Project sets `<IlasmRoundTripIncompatible>true</IlasmRoundTripIncompatible>`

The test is skipped during IL round-trip validation runs. The skip check is per-process.

### 12. Project sets `<UnloadabilityIncompatible>true</UnloadabilityIncompatible>`

The test cannot run inside an unloadable `AssemblyLoadContext`. The skip check when
`RunInUnloadableContext` is set is per-process.

### 13. Project sets `<CLRTestTargetUnsupported>true</CLRTestTargetUnsupported>`

The test is not supported on the current target platform. This affects build-time
filtering and test execution.

### 14. Project sets `<NativeAotIncompatible>true</NativeAotIncompatible>`

The test cannot be compiled or run under NativeAOT mode. This affects build and
execution filtering.

### 15. Project sets `<MonoAotIncompatible>true</MonoAotIncompatible>`

The test is incompatible with Mono's AOT compiler. This affects build and execution
filtering for Mono AOT test runs.

### 17. Project sets `<CrossGenTest>false</CrossGenTest>`

The test is incompatible with CrossGen2 ahead-of-time compilation. The test needs
its own execution script to skip the crossgen step.

### 17. Source code calls `Environment.Exit()`

`Environment.Exit` terminates the entire process. If the test is merged with other
tests, calling `Environment.Exit` would kill the shared runner and prevent remaining
tests from executing.

### 18. Source code calls `GC.WaitForPendingFinalizers()`

This is a process-wide GC operation. Running it in a shared process can interfere
with the GC state of other tests, causing non-deterministic failures.

### 19. Source code uses collectible `AssemblyLoadContext`

Tests that create, load into, and unload collectible `AssemblyLoadContext` instances
modify process-wide assembly state. Sharing a process could cause conflicts between
tests that load or unload assemblies.

### 20. Source code modifies process-wide state

Examples include:
- Setting `AppContext` switches or data.
- Making assumptions about which assemblies are or are not loaded.
- Monitoring the number of JIT-compiled methods.
- Leaving secondary threads running after the test method returns.

### 21. Test requires an explicit `Main` entry point

Some tests need a custom `Main` because they:
- Parse command-line arguments for local testing.
- Use `async Main`.
- Test runtime crash / unhandled exception scenarios where the process exit code matters.
- Modify assembly visibility or perform interop setup before any test code runs.

A custom `Main` is incompatible with the merged test runner's generated entry point.

### 22. Test intentionally crashes the process

Tests that validate crash behavior (stack overflow, fatal error handling, unhandled
exceptions) must run in their own process so the crash does not kill other tests.

### 23. Test is a separate executable launched by another test

If the project produces a helper executable that is launched by a different test
project (e.g., via `Process.Start`), it must build as a standalone executable with
process isolation.

### 24. Test loads native libraries from specific paths

Tests that probe for native libraries relative to their output directory or test
custom native library loading behavior need a predictable directory layout, which
merged runners do not guarantee.

### 25. Project uses a custom `<AppManifest>`

Tests that embed a custom application manifest (e.g., for COM activation) must run
in their own process because merged runners do not include per-test manifests.

### 26. Source code sets module-level attributes like `SkipLocalsInit`

Module-level attributes such as `[module: SkipLocalsInit]` affect all code in the
assembly. This is incompatible with merging into a shared runner where other tests
expect default behavior.

### 27. Test registers process-wide event handlers it does not clean up

Tests that register handlers on process-wide events (e.g.,
`AssemblyLoadContext.Default.ResolvingUnmanagedDll`) and do not unregister them
before returning can interfere with subsequent tests in a shared runner.

### 28. Test uses `IlcMultiModule` or multimodule NativeAOT builds

Multimodule NativeAOT tests have different build and link behavior that is
incompatible with the standard merged runner pipeline.

### 29. Test requires specific framework compilation settings

Tests that depend on the framework itself being compiled with non-default settings
(e.g., `UseSystemResourceKeys`) must run in a process whose runtime matches those
settings.

## Summary of Property-Based Triggers

If the project file contains **any** of the following properties, set
`<RequiresProcessIsolation>true</RequiresProcessIsolation>`:

| Property | Reason |
|---|---|
| `CLRTestEnvironmentVariable` | Process-wide env vars |
| `CLRTestBatchEnvironmentVariable` | Process-wide env vars |
| `RuntimeHostConfigurationOption` | Process-wide host config |
| `AutoreleasePoolSupport` | Process-wide runtime feature |
| `TrimMode` | Incompatible build mode |
| `CMakeProjectReference` | Native binary dependency |
| `Content` / `None` with `CopyToOutputDirectory` | Output directory conflicts |
| `GCStressIncompatible` | Per-process skip check |
| `HeapVerifyIncompatible` | Per-process skip check |
| `JitOptimizationSensitive` | Per-process skip check |
| `SuperPMICollectIncompatible` | Per-process skip check |
| `IlasmRoundTripIncompatible` | Per-process skip check |
| `UnloadabilityIncompatible` | Per-process skip check |
| `CLRTestTargetUnsupported` | Build/execution filtering |
| `NativeAotIncompatible` | Build/execution filtering |
| `MonoAotIncompatible` | Build/execution filtering |
| `CrossGenTest` (set to `false`) | Crossgen skip needed |
| `AppManifest` | Per-process manifest |
| `IlcMultiModule` | Incompatible build mode |

## Summary of Source-Code-Based Triggers

If the source code (`.cs` or `.il`) contains **any** of the following patterns, set
`<RequiresProcessIsolation>true</RequiresProcessIsolation>`:

| Pattern | Reason |
|---|---|
| `Environment.Exit(` | Terminates the process |
| `GC.WaitForPendingFinalizers(` | Process-wide GC side effect |
| Collectible `AssemblyLoadContext` | Process-wide assembly state |
| Explicit / custom `Main` entry point | Incompatible with merged runner entry point |
| `async Main` | Incompatible with merged runner entry point |
| Process crash by design | Would kill the shared runner |
| `AppContext` state changes | Process-wide state |
| Assumptions about loaded assemblies | Process-wide state |
| Secondary threads left running | Process-wide state |
| Native library loading from relative paths | Directory layout dependency |
| Standalone helper exe for another test | Must be its own executable |
| `[module: SkipLocalsInit]` or similar | Module-level attribute affects entire assembly |
| Unregistered process-wide event handlers | Leaks into subsequent tests |
| Requires non-default framework compilation | Needs matching runtime settings |
