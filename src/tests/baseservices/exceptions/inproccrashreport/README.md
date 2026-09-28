# In-process crash reporter tests

Under normal use, the in-process crash reporter writes diagnostics while handling a fatal signal and the application then terminates. These tests invoke the reporter with fixed crash data without raising a real signal, which keeps the process alive so managed assertions can inspect the generated report.

The test-native library links the production formatter, writers, lifecycle, reporter, and watchdog from CoreCLR's built PAL archive. The native driver initializes its own PAL instance and supplies deterministic threads, stack frames, modules, exception details, signal information, and register state, then calls `InProcCrashReportSignalDispatcher` directly. This validates report construction and formatting, but not PAL signal dispatch, process termination, real VM stack walking, module lookup, or watchdog timing.

The same managed validation and native driver run as regular CoreCLR tests on desktop Unix, through AndroidAppBuilder on Android, and through AppleAppBuilder on iOS, iOS Simulator, and MacCatalyst arm64. Mobile projects are thin packaging adapters, not separate reporter implementations.

| Scenario | Expected output |
| --- | --- |
| `RichSigsegv` | One completed JSON file and a compact report: three thread records, interleaved managed/native frames, generic names, exact registers and frame metadata, exception details, and module associations. |
| `Abort` | One completed JSON file and a compact report: two thread records, `SIGABRT`, a native-only crash stack, and no managed exception fields. |
| `StackOverflow` | One completed JSON file and a compact report: a managed stack-overflow exception, 42 total frames represented by three trace entries, and a recursive entry repeated 40 times. Tests emission, not stack exhaustion or trace compression. |
| `ConsoleOnly` | The rich compact report, without even creating the lifecycle report directory despite being given a valid root. |
| `OnDemand` | Sequential and concurrent JSON/compact requests with changing signals, using the same rich assertions. Null, nested, and overlapping requests are rejected; failed sinks stop receiving writes and later requests succeed. Mixed signal-shaped/on-demand requests test both directions of contention. The first request needs no services; only the final signal-shaped owner creates lifecycle output. |

Every test must exit normally with code 100; none intentionally crashes. Desktop projects use isolated generated test runners because the native reporter retains process-wide state. Thread records in the reports are synthetic; the on-demand concurrency checks use real threads to issue competing requests.

The concurrency checks hold an on-demand request inside its first output callback while JSON and compact requests run on two other threads. Both contenders must return false without invoking their output callbacks or writing bytes, before the owner is released. A signal-dispatch request also runs while that owner is held and must return without enumerating threads, emitting compact output, or creating lifecycle files. Four cases cover JSON/compact owners that either complete or fail their sink. Successful owner output and subsequent requests are validated to detect writer-state corruption or an unreleased guard.

A final case holds a signal-shaped owner inside the fixture's thread-enumeration callback while on-demand requests in both formats are rejected. After release, the signal owner must produce one completed JSON file and a valid compact report. On-demand requests must remain rejected afterwards because the signal path retains its guard. This case runs last in the existing isolated process; the reporter is not reset or replaced. Coordination uses explicit handshakes and bounded waits, not timing sleeps. All of these calls target the same private reporter instance and exercise its admission guard, not PAL's separate fatal-signal gate.

Each invocation owns a unique output directory. Successful runs delete it; failed runs print its location and retain the files, including incomplete reports. Bounded report contents and native I/O diagnostics also appear in the test log.

Desktop Helix runs write beneath `HELIX_WORKITEM_UPLOAD_ROOT` so failed outputs are collected automatically; local runs use the temporary directory. Android failures package the files as `inproccrashreport.zip` in the app's external files directory. The local runner and Helix use XHarness's existing `--device-out-folder` option to retrieve this archive before uninstalling the app. An empty archive remains on success because XHarness requires the configured artifact to exist. A single archive also works with XHarness's Android 11 file-copy fallback, which cannot copy a directory. Archiving requires the managed failure handler to run; an unexpected native crash or forced termination may leave only the empty archive and runner logs.

Apple runs retain the standard XHarness logs, but app-owned crash-report files are not currently copied to the host before app cleanup.

## Layout

The projects under this directory run on desktop Unix CoreCLR. Matching projects under `src/tests/FunctionalTests/Android/Device_Emulator/InProcCrashReport` package the same test sources for Android. The Android projects are excluded from Mono discovery because the reporter is a CoreCLR component.

`Shared/Program.cs` runs the scenario and validates its outputs. `Shared/inproccrashreport_test_driver.cpp` initializes PAL, checks its process ID, and supplies deterministic callback data and register state. `Shared/InProcCrashReport.Unix.props` connects the non-mobile Unix projects to `CMakeLists.txt`; the Android projects use `InProcCrashReport.Common.props`. Both adapters link the driver to the built PAL archive and minipal. PAL symbols stay private to the test library, separate from the PAL inside the hosting CoreCLR.

The driver uses `PAL_InitializeDLL`, which does not install fatal-signal handlers. Reporter callback registration uses real PAL code, but the tests invoke the dispatcher themselves. The real watchdog is linked but disabled in the service settings; PAL and reporter state remain alive until the isolated process exits.

The Android `Shared` directory contains `android_log_interpose.c/.h` to capture compact reports from Android logging using `--wrap=__android_log_write`, forwarding calls to liblog through `__real___android_log_write`. Its `config.h` supplies CoreCLR platform/architecture definitions missing from AndroidAppBuilder. Desktop tests redirect stderr and get those definitions from the native test build, without a test-specific configuration header.

The Apple projects live under `src/tests/FunctionalTests/iOS/InProcCrashReport`. Each scenario is a separate app and process, including OnDemand's final signal-report-first case. Their `Shared/CMakeLists.txt` imports the repository's native platform/compiler configuration and the shared fixture target. It links the matching product PAL and minipal archives into `libInProcCrashReportNative.dylib`, exporting only `InProcCrashReportTest_*`. The hosting CoreCLR is a different dylib with a different PAL instance. Do not link the fixture PAL into the app executable or substitute a macOS archive for an iOS, simulator, or Catalyst archive.

AppleAppBuilder's existing dylib resource packaging and signing handle the fixture. No Android native-source injection, ELF linker flags, copied reporter implementation, or builder extension is used. Native P/Invoke resolves the packaged dylib; OnDemand also exercises reverse P/Invoke on real managed threads. Apple uses the same stderr capture and Darwin `ucontext_t`/machine-context register initialization as desktop macOS.

AndroidAppBuilder normally uses `ANDROID_STL=none` because its app-launcher sources are C. The Android test props explicitly add the NDK C++ headers and statically link `libc++_static` and `libc++abi`, keeping those symbols private. No replacement C++ runtime is needed, and no `libc++_shared.so` needs to be packaged. Desktop builds use the normal C++ runtime.

Real fatal-signal and real-runtime callback coverage requires an external process to inspect output after the crashing process exits and is intentionally separate from these deterministic fidelity tests. Watchdog behavior, concurrent crashes, frame limits/truncation, and lifecycle retention or disk failures also need separate coverage.

## CI coverage

Desktop tests use normal CoreCLR test discovery. Android tests are included in full-suite CoreCLR archive builds, not the default PR smoke selection, to keep APK packaging and device execution off that path. They run in the daily `runtime-extra-platforms` lanes and explicitly requested `/azp run runtime-android` or `/azp run runtime-androidemulator` PR builds. Selection uses `RunSmokeTestsOnly=false` and `ArchiveTests=true`, not an outerloop category; Mono and NativeAOT remain excluded.

Apple projects have the same full-suite/archive restriction and are limited to CoreCLR arm64 on `ios`, `iossimulator`, and `maccatalyst`. They are outside the existing CoreCLR smoke-project glob. The run-only Helix registration explicitly expects **100**, rather than the sample apps' default of 42. The current simulator and Catalyst extra-platforms CoreCLR lanes build apps on Helix; the archive contains the already-linked fixture dylib, managed inputs, and the standard Apple proxy project. PAL archives, native source paths, and native build intermediates are not required on the runner. Discovery and archive inspection alone do not establish hosted Helix coverage.

## Run locally

Build CoreCLR for the matching platform, architecture, and configuration before building these tests. All adapters use its installed PAL archive; Android and Apple also use minipal from the matching native build intermediates. Rebuild CoreCLR after changing reporter implementation code; rebuilding only the test does not rebuild the reporter.

For a desktop product in a different location or configuration, pass `-cmakeargs "-DCRASH_REPORT_PAL_LIBRARY=<path to libcoreclrpal.a>"` to the native test build. Custom system-libunwind products also require the matching `CLR_CMAKE_USE_SYSTEM_LIBUNWIND` CMake setting.

Run a desktop scenario with the normal CoreCLR test command for its project. For Android, build a CoreCLR Android test environment and start an emulator as described in the [CoreCLR Android documentation](../../../../../docs/workflow/building/coreclr/android.md), then run the matching Android project through its `Test` target.

### macOS arm64

From the repository root:

```bash
./build.sh clr+libs -arch arm64 -c Release -rc Checked -lc Release
src/tests/build.sh arm64 Checked -tree baseservices/exceptions/inproccrashreport -p:LibrariesConfiguration=Release
src/tests/build.sh arm64 Checked -generatelayoutonly -p:LibrariesConfiguration=Release
export CORE_ROOT="$PWD/artifacts/tests/coreclr/osx.arm64.Checked/Tests/Core_Root"
scenario=RichSigsegv
"$CORE_ROOT/corerun" "artifacts/tests/coreclr/osx.arm64.Checked/baseservices/exceptions/inproccrashreport/$scenario/InProcCrashReport.$scenario/InProcCrashReport.$scenario.dll"
```

The direct `corerun` command returns 100 on success, not shell exit code zero.

### Apple app bundles

Build each target separately. `iossimulator` is not interchangeable with `ios` or `maccatalyst`.

```bash
target=iossimulator # or maccatalyst or ios
./build.sh clr+clr.runtime+libs+packs -os "$target" -arch arm64 -cross -c Release
scenario=RichSigsegv
project="src/tests/FunctionalTests/iOS/InProcCrashReport/$scenario/iOS.InProcCrashReport.$scenario.Test.csproj"
./dotnet.sh build "$project" -c Release -t:Test \
  -p:TargetOS="$target" -p:TargetArchitecture=arm64 \
  -p:UseMonoRuntime=false -p:UseNativeAOTRuntime=false
```

Start with RichSigsegv to check both JSON and compact output, then run `Abort`, `StackOverflow`, `ConsoleOnly`, and `OnDemand` through their own projects. Repeat OnDemand by relaunching its app, not by calling its entry point again in the same process. The test runner returns zero only when XHarness observes the app's expected exit code 100.

CoreCLR Apple builds use composite Mach-O ReadyToRun with interpreter fallback by default. `-p:PublishReadyToRun=false` selects interpreted managed code instead; neither mode uses Mono, NativeAOT, or assumes desktop JIT support. Simulator builds use unsigned bundles, MacCatalyst defaults to ad-hoc signing, and physical iOS execution requires a connected device and a valid development team/provisioning profile (`-p:DevTeamProvisioning=<team>`). The in-tree `DevTeamProvisioning=-` default permits unsigned device build inspection, not device execution.

Without a provisioned physical device, build with `-p:TargetOS=ios -p:DevTeamProvisioning=-` and **omit `-t:Test`**. This produces an unsigned `Release-iphoneos` bundle; it does not validate installation, device execution, or provisioning.

For a build-on-Helix input archive, use a clean app-bundle output directory when switching from local execution or changing trimming settings. Otherwise, the existing archive bundler can retain stale assemblies from the earlier configuration. Build targets sequentially because NuGet intermediates are shared across target OSes:

```bash
signing=-
if [ "$target" = maccatalyst ]; then signing=adhoc; fi
./dotnet.sh build "$project" -c Release \
  -p:TargetOS="$target" -p:TargetArchitecture=arm64 \
  -p:UseMonoRuntime=false -p:UseNativeAOTRuntime=false \
  -p:ArchiveTests=true -p:BuildTestsOnHelix=true \
  -p:ContinuousIntegrationBuild=true -p:DevTeamProvisioning="$signing"
```

Inspect `artifacts/helix/runonly/<target>.AnyCPU.Release/` and its generated proxy inputs. The archive does not need the fixture's PAL/minipal archives or source tree on the runner; standard runtime-pack assets are still included. On the host, `nm -gU` should show only the four reporter-test exports in the fixture; `otool -L` must not contain developer-machine library paths. App signing also covers packaged dylibs. A locally executed app or locally replayed proxy build is not a hosted Helix run.

Generate the actual Helix work-item commands **without submitting a job** using this specific target:

```bash
./dotnet.sh msbuild src/libraries/sendtohelixhelp.proj \
  -t:CreateAppleWorkItems -p:TargetOS="$target" -p:TargetArchitecture=arm64 \
  -p:Configuration=Release -p:RuntimeFlavor=CoreCLR \
  -p:Scenario=BuildiOSApps -p:NeedsToBuildAppsOnHelix=true \
  -p:HelixTargetQueue=local-inspection-only -getItem:HelixWorkItem
```

Each new work item must use the matching Apple target and `--expected-exit-code "100"`. This checks registration and command generation, not hosted execution.

To repeat an already built app without rebuilding:

```bash
app="<absolute-path-to-OnDemand.app>"
target=ios-simulator-64 # or maccatalyst or ios-device
for iteration in {1..20}; do
  ./dotnet.sh xharness apple run --app "$app" --targets "$target" \
    --output-directory "$PWD/artifacts/tmp/ondemand-$iteration" \
    --expected-exit-code 100 --signal-app-end --timeout 00:05:00 || break
done
```
