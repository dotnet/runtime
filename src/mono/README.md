# Mono

This folder contains the source for Mono, the .NET runtime implementation used
for mobile (iOS, Android), browser (WebAssembly), and WASI workloads.

## Building and testing

Mono uses different build subsets and host configurations than CoreCLR. See
the workflow docs for the authoritative instructions — they cover the
configuration matrix (LLVM, AOT, interpreter, WASM, mobile) that the top-level
`build.sh`/`build.cmd` help text does not. For up-front baseline build
requirements (mandatory under CCA, probe-and-fall-back under CLI), see
[`.github/copilot-instructions.md`](/.github/copilot-instructions.md).

- [Building Mono](/docs/workflow/building/mono/README.md)
- [Testing Mono](/docs/workflow/testing/mono/testing.md)
- [Testing libraries on WebAssembly](/docs/workflow/testing/libraries/testing-wasm.md)
- [Testing libraries on Android](/docs/workflow/testing/libraries/testing-android.md)
- [Testing libraries on Apple platforms](/docs/workflow/testing/libraries/testing-apple.md)

CoreCLR-style runtime tests under [`../tests`](../tests/) can also be run
against Mono — see the testing doc above.
