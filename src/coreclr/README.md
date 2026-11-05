# CoreCLR

This folder contains the source for CoreCLR, the .NET runtime implementation
used by most workloads. CoreCLR tests live in [`../tests`](../tests/) (the
`src/tests` subtree).

## macOS NativeAOT tool metadata

NativeAOT tool publishing projects set `HostBundleIdentifier` and `HostBundleName`
to embed an Info.plist in macOS executables. `eng/toolAot.targets` omits the optional
bundle version keys because assembly versions do not follow Apple's bundle version
format.
Bundle identifiers use `com.microsoft.dotnet.<executable-name>` in lowercase;
`HostBundleName` sets `CFBundleName` to the executable name without that prefix.
Native CMake executables use the same identifier and name convention.
Both builds use `eng/native/Info.plist.in`; NativeAOT generates it with
`Microsoft.DotNet.Build.Tasks.Templating`.

This metadata does not create an app bundle. Final signing should use the embedded
bundle identifier, or explicitly supply the same value. The linker-generated ad
hoc signature does not automatically use the embedded bundle identifier.
`AdHocSignHost` applies the identifier after native linking and symbol stripping,
before the executable is copied to its publish directory. It preserves existing
entitlements, requirements, signing flags, hardened runtime version, and launch and
library constraints while explicitly setting the identifier, without adding JIT
entitlements. Official signing must preserve the identifier and security settings
or explicitly supply the same values.

## Building and testing

See the workflow docs for the authoritative instructions — they cover the
specific subset, configuration, and incremental-iteration flags that the
top-level `build.sh`/`build.cmd` help text does not. For up-front baseline
build requirements (mandatory under CCA, probe-and-fall-back under CLI), see
[`.github/copilot-instructions.md`](/.github/copilot-instructions.md).

- [Building CoreCLR](/docs/workflow/building/coreclr/README.md)
- [Testing CoreCLR](/docs/workflow/testing/coreclr/testing.md)
- [Test configuration options](/docs/workflow/testing/coreclr/test-configuration.md)
- [Unix test instructions](/docs/workflow/testing/coreclr/unix-test-instructions.md) · [Windows test instructions](/docs/workflow/testing/coreclr/windows-test-instructions.md)
- [Disasm checks](/docs/workflow/testing/coreclr/disasm-checks.md) · [GC stress runs](/docs/workflow/testing/coreclr/gc-stress-run-readme.md)
- [Using `corerun` and `Core_Root`](/docs/workflow/testing/using-corerun-and-coreroot.md)

Some subdirectories contain their own `README.md` with component-specific
guidance — read any you find before making changes.
