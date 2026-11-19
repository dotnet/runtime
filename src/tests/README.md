# Runtime Tests

This folder is the **CoreCLR / runtime test tree**. Tests here exercise the
runtime itself (JIT, GC, EE, interop, threading, tracing, …) rather than the
managed libraries — for those, see `src/libraries/<Name>/tests/`.

Tests are driven by `src/tests/build.sh` and `src/tests/run.sh`, not by
`dotnet test`. The wrong tool will silently report "0 test projects" or fail
to find a testhost.

## Building and running

For up-front baseline build requirements (mandatory under CCA,
probe-and-fall-back under CLI), see
[`.github/copilot-instructions.md`](/.github/copilot-instructions.md).

- [Testing CoreCLR](/docs/workflow/testing/coreclr/testing.md) — the
  authoritative guide for this tree, including `Core_Root` setup, single-test
  builds, and `corerun`
- [Test configuration options](/docs/workflow/testing/coreclr/test-configuration.md)
- [`<RequiresProcessIsolation>` tests](/docs/workflow/testing/coreclr/requiresprocessisolation.md)
- [Disasm checks](/docs/workflow/testing/coreclr/disasm-checks.md)
- [Unix test instructions](/docs/workflow/testing/coreclr/unix-test-instructions.md) · [Windows test instructions](/docs/workflow/testing/coreclr/windows-test-instructions.md)
- [Running ARM32 tests](/docs/workflow/testing/coreclr/running-arm32-tests.md)
- [GC stress runs](/docs/workflow/testing/coreclr/gc-stress-run-readme.md)

Run `src/tests/build.sh -h` for the full flag list. A few commonly-missed
flags:

| Flag | Description |
|------|-------------|
| `-Test <path>` | Build one project |
| `-Tree <path>` | Build a subtree recursively |
| `-priority1` (`-Priority 1` on Windows) | Required for tests with `<CLRTestPriority>1</CLRTestPriority>`; without it the build silently reports "0 test projects" |
| `-GenerateLayoutOnly` | Generate `Core_Root` only (required before running individual tests with `corerun`) |

Many subdirectories under `src/tests/` have their own `README.md` with
area-specific authoring and run guidance — read them before making changes.

### Local CoreCLR browser-wasm64 tests

With matching `browser-wasm64` runtime and library builds and Node.js on `PATH`,
generate `Core_Root` and build only the standalone managed tests you want to run:

```bash
export PATH="$PWD/.dotnet:$PATH"
src/tests/build.sh -arch wasm64 -browser Release generatelayoutonly
BuildAllTestsAsStandalone=true src/tests/build.sh -arch wasm64 -browser Release \
    skipnative skipgeneratelayout -priority1 \
    -test:Loader/classloader/generics/Instantiation/Regressions/607/DevDiv607.csproj

test_root="$PWD/artifacts/tests/coreclr/browser.wasm64.Release"
export CORE_ROOT="$test_root/Tests/Core_Root"
wrapper="$test_root/Loader/classloader/generics/Instantiation/Regressions/607/DevDiv607/DevDiv607.sh"
env -u RunCrossGen2 DOTNET_ReadyToRun=0 bash "$wrapper"
env -u DOTNET_ReadyToRun RunCrossGen2=1 bash "$wrapper"
```

For ReadyToRun, the layout needs the host-native Crossgen2, jitinterface,
wasm64 cross-JIT, and R2RDump. If Crossgen2 and its native libraries were built
outside the usual CoreCLR output directory, pass
`/p:Crossgen2InBuildDir=/absolute/path/to/crossgen2` when generating the layout.
The wrapper enables experimental wasm64 compilation, non-composite by default,
and runs R2RDump validation before running the test. Set `CompositeBuildMode=1`
for a composite of the test assemblies, or `WholeProgramCompositeBuildMode=1` to
include CoreLib, as on wasm32. This is a Node/CoreRun workflow,
not a Chrome or Helix test leg.

Tests with their own native library also need the memory64 test-native build and a
test-specific CoreRun. Build the native tree (or configure it with `-configureonly`
and build only the libraries you need), then run the `copynativeonly` step with the
same `-test:` filters. That step stages the static libraries and relinks a CoreRun
next to each test. Relinking needs the wasm64 CoreCLR `corerun-link` kit and
Crossgen2 in `artifacts/bin/coreclr/browser.wasm64.<Configuration>/<host arch>/crossgen2`.

```bash
src/tests/build.sh -arch wasm64 -browser Release skipmanaged skipgeneratelayout
src/tests/build.sh -arch wasm64 -browser Release copynativeonly -priority1 \
    -test:Interop/PrimitiveMarshalling/UIntPtr/PInvokeUIntPtrTest.csproj
```
