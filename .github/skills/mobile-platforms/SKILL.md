---
name: mobile-platforms
description: "Domain knowledge for triaging and fixing .NET failures on Apple mobile (iOS, tvOS, MacCatalyst) and Android. Use when runtime-extra-platforms or mobile CI is failing, when investigating iOS, tvOS, MacCatalyst, iossimulator, tvossimulator, or Android build/test failures, or when a change touches mobile pipeline YAML, AppleAppBuilder/AndroidAppBuilder, code signing, provisioning, simulator/emulator startup, platform conditionals, or NativeAOT-on-mobile behavior. Covers failure triage (infrastructure vs code), CI pipeline structure, platform-specific code paths, and NativeAOT compilation on mobile."
---

# Mobile Platforms

Triage and fix .NET failures on Apple mobile (iOS, tvOS, MacCatalyst) and Android in dotnet/runtime.

## Why mobile is different

Mobile platforms run .NET on devices and simulators/emulators where the host OS controls code execution. Apple forbids JIT in production (simulators allow it), requires code signing, and has six OS variants that often need identical handling. Android requires cross-compilation and APK packaging across four architectures. Both surface failures invisible on desktop: provisioning errors, emulator boot timeouts, app bundle signing, native toolchain mismatches.

CoreCLR is the primary runtime. NativeAOT is the default publish framework for ahead-of-time compilation. Mono also runs in CI alongside CoreCLR.

## CI pipeline

Mobile tests run in the `runtime-extra-platforms` pipeline (AzDO definition 154, org `dnceng-public`, project `public`), daily on `main` and on relevant PRs.

Jobs follow `{platform} {config} {subset}`:

| Platform | Job pattern | Variants |
|---|---|---|
| iOS Simulator | `iossimulator-{x64,arm64}` | CoreCLR, Mono, NativeAOT |
| tvOS | `tvos_arm64` | CoreCLR, Mono, NativeAOT |
| MacCatalyst | `maccatalyst-{x64,arm64}` | CoreCLR, Mono, NativeAOT, AppSandbox |
| Android | `android-{arm,arm64,x64,x86}` | CoreCLR, Mono, NativeAOT |

Suffixes: empty (libraries), `RuntimeTests`, `Smoke`, `AppSandbox`, `Interp`.

## Where to look

Start from the failure type, not the platform. The tables below map failure symptoms to code paths.

### Build and packaging failures

These happen before tests run. The app bundle fails to build, sign, or package.

| Platform | Build targets | App builder task |
|---|---|---|
| Apple | `src/mono/msbuild/apple/build/` | `src/tasks/AppleAppBuilder/` |
| Android | `src/mono/msbuild/android/build/` | `src/tasks/AndroidAppBuilder/` |

Apple code signing uses `DevTeamProvisioning`: `-` for simulators, `adhoc` for MacCatalyst. If a signing error mentions provisioning profiles, check whether the right value is passed in the pipeline YAML.

### Test failures

The test itself fails or crashes on the device/emulator.

| Platform | Test runner | Test targets |
|---|---|---|
| Apple | `src/libraries/Common/tests/AppleTestRunner/` | `eng/testing/tests.ioslike.targets` |
| Android | `src/libraries/Common/tests/AndroidTestRunner/` | `eng/testing/tests.android.targets` |

Common cause: a test assumes desktop behavior (process spawning, filesystem layout, JIT availability). Check whether the test has platform-specific skip conditions using `PlatformDetection`.

### NativeAOT compilation failures

NativeAOT on mobile has a different target resolution path than desktop. `Directory.Build.targets` evaluates before NuGet package targets, so properties like `_IsApplePlatform` from `Microsoft.DotNet.ILCompiler` are not available in `eng/toolAot.targets`. For Apple mobile library tests, `ILCompilerTargetsPath` must be set to `$(CoreCLRBuildIntegrationDir)Microsoft.DotNet.ILCompiler.SingleEntry.targets` with `_IlcReferencedAsPackage=false`.

Key files: `eng/targetingpacks.targets`, `eng/toolAot.targets`.

### Native interop crashes

Stack traces pointing into native code or P/Invoke calls.

| Platform | Native libs |
|---|---|
| Apple | `src/native/libs/System.Security.Cryptography.Native.Apple/`, `src/native/libs/System.Native/ios/` |
| Android | `src/native/libs/` (shared with Linux/Bionic) |

### Pipeline YAML

When the job definition itself is wrong (e.g., missing a platform, wrong build args):

| Platform | Pipeline files |
|---|---|
| Apple (ioslike) | `eng/pipelines/extra-platforms/runtime-extra-platforms-ioslike.yml` |
| Apple (simulator) | `eng/pipelines/extra-platforms/runtime-extra-platforms-ioslikesimulator.yml` |
| Apple (maccatalyst) | `eng/pipelines/extra-platforms/runtime-extra-platforms-maccatalyst.yml` |
| Android | `eng/pipelines/extra-platforms/runtime-extra-platforms-android.yml` |
| Android (emulator) | `eng/pipelines/extra-platforms/runtime-extra-platforms-androidemulator.yml` |

## Platform gotchas

Things that have caused confusion before and will again:

- **Apple has six OS variants**: `ios`, `iossimulator`, `tvos`, `tvossimulator`, `maccatalyst` (x64 + arm64). A fix for one variant almost always needs to cover all six. Missing one causes the next pipeline run to fail on a different job.
- **dsymutil outputs errors to stdout**, not stderr. `2>/dev/null` does not suppress its errors; use `>/dev/null 2>&1`.
- **Android has four architectures**: `android-arm`, `android-arm64`, `android-x64`, `android-x86`. x86 is 32-bit and can expose different issues than the 64-bit targets.
- **MSBuild evaluation order**: `Directory.Build.targets` runs before NuGet package `.targets`. Properties from packages are not available in `eng/toolAot.targets`.

## Failure triage

Classify every failure as **infrastructure** or **code** before acting.

### Infrastructure failures

Caused by the test environment, not code. Indicators:
- Timeout with no test output (emulator/simulator boot failure)
- "device not found", "provisioning profile", "code signing" errors
- Network errors during Helix payload download
- Machine-specific: same test passes on other machines
- "No space left on device", Helix agent crashes

Report infrastructure failures on existing tracking issues with a table entry:

| Build | Date | Machine | Job | Error |
|---|---|---|---|---|
| [#buildNumber](link) | YYYY-MM-DD | machineName | jobName | brief error |

If no matching issue exists, create one with `area-Infrastructure` and platform labels (`os-ios`, `os-tvos`, `os-maccatalyst`, `os-android`).

For already tracked known issues, check whether the root cause is actionable. If a code or configuration fix is feasible, open a PR. If the issue is purely infrastructure (device provisioning, network), add occurrence data to the tracking issue instead.

### Code failures

Caused by recent commits. Indicators:
- Test was passing in previous builds
- Error references managed code
- Recent commit touched platform-sensitive code

Investigate code failures by starting with `git log --oneline --since='3 days ago'` on the relevant path. If nothing matches, widen the window or check for intermittent patterns across recent builds. Common patterns:
- New API missing mobile platform handling
- `#if` conditional compilation missing a mobile target
- P/Invoke change without a mobile native implementation
- Test assuming desktop behavior (process spawning, JIT, filesystem paths)

## Self-improvement

When a mobile fix workflow discovers new patterns or workarounds, record the finding as a comment on the tracking issue (or create a new issue labeled `area-Infrastructure-mono`) so the team can later incorporate it into this document. Add findings to the relevant section above:
- New failure patterns not yet documented
- Code paths that turned out to be relevant
- Recurring infrastructure issues and their workarounds
- Platform gotchas discovered during investigation

Keep PRs focused on the mobile failure being fixed. Unrelated skill edits make review and blame harder, so avoid editing this file in PRs that are not fixing or documenting mobile platform work.
