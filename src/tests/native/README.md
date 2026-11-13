# Native Component Tests

This directory tests native runtime components. These are ordinary runtime
tests: a managed CoreCLR or NativeAOT launcher starts a native test executable
as a separate process and checks its result. Managed EventPipe runtime-behavior
tests remain under `src/tests/tracing/eventpipe`.

| Directory | Contents |
|-----------|----------|
| `common/` | Shared native-test runner and managed process helper |
| `containers/` | Tests for `src/native/containers`, including SIMD hash tables |
| `eventpipe/` | Tests for `src/native/eventpipe` using a standalone native test host |
| `minipal/` | Tests for `src/native/minipal` and `src/coreclr/minipal` |

## Build and output layout

Use the normal runtime-test build with `-Tree native` to select all suites, or
`-Tree native\containers` / `-Tree native\eventpipe` / `-Tree native\minipal`
to select one. See the [runtime-test instructions](../README.md) for baseline
and build requirements. Mobile and WebAssembly targets are not supported.

The `native.csproj` merged runner discovers the component test projects for
normal CI execution and Helix deployment. Each component test retains process
isolation, so its native driver is packaged beside its managed launcher.

For Windows x64 Checked, the runnable tests are deployed under
`artifacts\tests\coreclr\windows.x64.Checked\native\containers\NativeContainerTests`,
`artifacts\tests\coreclr\windows.x64.Checked\native\eventpipe\NativeEventPipeTests`,
and `artifacts\tests\coreclr\windows.x64.Checked\native\minipal\NativeMinipalTests`.
Each directory contains the CoreCLR launcher and its native driver. NativeAOT
places its launcher in the usual `native` subdirectory, and uses the driver in
the parent directory.

Intermediates retain the standard build-category prefix followed by the
source-relative directory: `obj\windows.x64.Checked\Managed\native\...` for the
managed launchers and `obj\windows.x64.Checked\Native\native\...` for native code.

## Opt-in native container performance suite

The SIMD hash-table correctness tests, performance measurements, and private
comparison hash table are implemented together in
`containers/dn-simdhash-tests.c` and compiled into
`NativeContainerTestsDriver` during ordinary native test builds. There is no
separate benchmark executable, build target, or build script. The generic
container `perf` group registers `test_simdhash_perf`; other container performance
tests can be added to `containers/dn-container-perf-tests.c`.

The `perf` group is excluded from unfiltered native runs and `--list`; use
`--list-all` to include opt-in groups in discovery. The managed
`NativeContainerTests.RunPerformanceSuite` test is skipped unless
`DOTNET_TEST_NATIVE_CONTAINER_PERF=1`, so normal CI builds the suite without
executing it.

Standalone managed launchers do not support method-name filtering: enabling this
flag runs the performance test in addition to the correctness tests. Use the
native `perf` invocation below to run only the performance suite.

For example, from the built container-test directory in Windows cmd.exe, with
`CORE_ROOT` set to the matching runtime layout:

```bat
set DOTNET_TEST_NATIVE_CONTAINER_PERF=1
"%CORE_ROOT%\corerun.exe" NativeContainerTests.dll
set DOTNET_TEST_NATIVE_CONTAINER_PERF=
```

For NativeAOT, run the generated `NativeContainerTests` executable with the same
opt-in environment variable. To invoke only the native suite directly:

```bat
NativeContainerTestsDriver.exe perf
```

Use a Release native test build for performance measurements. All 18 original
workloads and their warmup and measurement durations are preserved, so a complete
run takes approximately seven minutes, plus setup overhead. The opt-in managed
suite has a fifteen-minute native-process timeout; ordinary native test calls
retain their sixty-second timeout. Timings are printed for local comparison,
not used as pass/fail thresholds. Measurements use minipal's high-resolution
monotonic timer and report elapsed time, not process CPU time, on all platforms.

## Opt-in native EventPipe performance suite

The buffer, buffer-manager, and event-writing performance tests are compiled into
`NativeEventPipeTestsDriver` during ordinary native test builds in every
configuration. There is no separate performance build option. As with containers,
the `perf` group is excluded from unfiltered native runs and `--list`; use
`--list-all` to discover it or select `perf` explicitly to execute it.

The suite is registered in `eventpipe/ep-perf-tests.c`, matching the
container performance-suite registry. Individual benchmark implementations stay
in their subsystem test files alongside the helpers they use.

The managed `NativeEventPipeTests.RunPerformanceSuite` test is skipped unless
`DOTNET_TEST_NATIVE_EVENTPIPE_PERF=1`. As with containers, the standalone managed
launcher runs the performance test in addition to its correctness tests and does
not support method-name filtering. From the built EventPipe-test directory in
Windows cmd.exe, with `CORE_ROOT` set to the matching runtime layout:

```bat
set DOTNET_TEST_NATIVE_EVENTPIPE_PERF=1
"%CORE_ROOT%\corerun.exe" NativeEventPipeTests.dll
set DOTNET_TEST_NATIVE_EVENTPIPE_PERF=
```

For NativeAOT, run the generated `NativeEventPipeTests` executable with the same
opt-in environment variable. To invoke only the native suite directly:

```bat
NativeEventPipeTestsDriver.exe perf
```

Use a Release native test build for meaningful performance measurements. The
opt-in managed suite has a fifteen-minute native-process timeout; ordinary native
test calls retain their sixty-second timeout. Normal CI builds the performance
code without running it, and performance timings have no pass/fail thresholds.
