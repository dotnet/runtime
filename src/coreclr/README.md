# CoreCLR

This folder contains the source for CoreCLR, the .NET runtime implementation
used by most workloads. CoreCLR tests live in [`../tests`](../tests/) (the
`src/tests` subtree).

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
