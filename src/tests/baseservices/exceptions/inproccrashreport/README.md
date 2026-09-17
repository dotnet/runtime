# In-process crash reporter tests

Under normal use, the in-process crash reporter writes diagnostics while handling a fatal signal and the application then terminates. These tests invoke the reporter with fixed crash data without raising a real signal, which keeps the process alive so managed assertions can inspect the generated report.

The test-native library links the production formatter, writers, lifecycle, reporter, and watchdog from CoreCLR's built PAL archive. The native driver initializes its own PAL instance and supplies deterministic threads, stack frames, modules, exception details, signal information, and register state, then calls `InProcCrashReportSignalDispatcher` directly. This validates report construction and formatting, but not PAL signal dispatch, process termination, real VM stack walking, module lookup, or watchdog timing.

The same managed validation and native driver run as regular CoreCLR tests on desktop Unix and through AndroidAppBuilder on Android. The Android projects are separate adapters because they need APK packaging and logcat interception. Apple mobile targets do not currently have an equivalent adapter for these shared native sources.

| Scenario | Expected output |
| --- | --- |
| `RichSigsegv` | One completed JSON file and a compact report: three thread records, interleaved managed/native frames, generic names, exact registers and frame metadata, exception details, and module associations. |
| `Abort` | One completed JSON file and a compact report: two thread records, `SIGABRT`, a native-only crash stack, and no managed exception fields. |
| `StackOverflow` | One completed JSON file and a compact report: a managed stack-overflow exception, 42 total frames represented by three trace entries, and a recursive entry repeated 40 times. Tests emission, not stack exhaustion or trace compression. |
| `ConsoleOnly` | The rich compact report, without even creating the lifecycle report directory despite being given a valid root. |

Every test must exit normally with code 100; none intentionally crashes. Desktop projects use isolated generated test runners because the native reporter retains process-wide state. The additional threads are fixed callback records, not real concurrent threads.

Each invocation owns a unique output directory. Successful runs delete it; failed runs print its location and retain the files, including incomplete reports. Bounded report contents and native I/O diagnostics also appear in the test log.

Desktop Helix runs write beneath `HELIX_WORKITEM_UPLOAD_ROOT` so failed outputs are collected automatically; local runs use the temporary directory. Android failures package the files as `inproccrashreport.zip` in the app's external files directory. The local runner and Helix use XHarness's existing `--device-out-folder` option to retrieve this archive before uninstalling the app. An empty archive remains on success because XHarness requires the configured artifact to exist. A single archive also works with XHarness's Android 11 file-copy fallback, which cannot copy a directory. Archiving requires the managed failure handler to run; an unexpected native crash or forced termination may leave only the empty archive and runner logs.

## Layout

The projects under this directory run on desktop Unix CoreCLR. Matching projects under `src/tests/FunctionalTests/Android/Device_Emulator/InProcCrashReport` package the same test sources for Android. The Android projects are excluded from Mono discovery because the reporter is a CoreCLR component.

`Shared/Program.cs` runs the scenario and validates its outputs. `Shared/inproccrashreport_test_driver.cpp` initializes PAL, checks its process ID, and supplies deterministic callback data and register state. `Shared/InProcCrashReport.Unix.props` connects the non-mobile Unix projects to `CMakeLists.txt`; the Android projects use `InProcCrashReport.Common.props`. Both adapters link the driver to the built PAL archive and minipal. PAL symbols stay private to the test library, separate from the PAL inside the hosting CoreCLR.

The driver uses `PAL_InitializeDLL`, which does not install fatal-signal handlers. Reporter callback registration uses real PAL code, but the tests invoke the dispatcher themselves. The real watchdog is linked but disabled in the service settings; PAL and reporter state remain alive until the isolated process exits.

The Android `Shared` directory contains `android_log_interpose.c/.h` to capture compact reports from Android logging, and `config.h` to supply CoreCLR platform/architecture definitions missing from AndroidAppBuilder. Desktop tests redirect stderr and get those definitions from the native test build, without a test-specific configuration header.

AndroidAppBuilder normally uses `ANDROID_STL=none` because its app-launcher sources are C. The Android test props explicitly add the NDK C++ headers and statically link `libc++_static` and `libc++abi`, keeping those symbols private. No replacement C++ runtime is needed, and no `libc++_shared.so` needs to be packaged. Desktop builds use the normal C++ runtime.

Real fatal-signal and real-runtime callback coverage requires an external process to inspect output after the crashing process exits and is intentionally separate from these deterministic fidelity tests. Watchdog behavior, concurrent crashes, frame limits/truncation, and lifecycle retention or disk failures also need separate coverage.

## CI coverage

Desktop tests use normal CoreCLR test discovery. Android tests are included in full-suite CoreCLR archive builds, not the default PR smoke selection, to keep APK packaging and device execution off that path. They run in the daily `runtime-extra-platforms` lanes and explicitly requested `/azp run runtime-android` or `/azp run runtime-androidemulator` PR builds. Selection uses `RunSmokeTestsOnly=false` and `ArchiveTests=true`, not an outerloop category; Mono and NativeAOT remain excluded.

## Run locally

Build CoreCLR for the matching platform, architecture, and configuration before building these tests. Both use its installed PAL archive; Android also uses minipal from the native build intermediates. Rebuild CoreCLR after changing reporter implementation code; rebuilding only the test does not rebuild the reporter.

For a desktop product in a different location or configuration, pass `-cmakeargs "-DCRASH_REPORT_PAL_LIBRARY=<path to libcoreclrpal.a>"` to the native test build. Custom system-libunwind products also require the matching `CLR_CMAKE_USE_SYSTEM_LIBUNWIND` CMake setting.

Run a desktop scenario with the normal CoreCLR test command for its project. For Android, build a CoreCLR Android test environment and start an emulator as described in the [CoreCLR Android documentation](../../../../../docs/workflow/building/coreclr/android.md), then run the matching Android project through its `Test` target.
