# WebAssembly baseline features

The following list of WASM features (https://webassembly.org/features/) will be considered "baseline" for ReadyToRun code, meaning it will not be possible to turn them off and the assumption of them being available will be hardcoded into RyuJit and Crossgen2.

The names and semantics of these features are taken from https://github.com/WebAssembly/tool-conventions/blob/main/Linking.md#target-features-section.

See also: https://webassembly.org/features/.

The `caniuse.com` website can also be used to check the avalability of features, for example: `https://caniuse.com/?search=nontrapping`.

## .NET 11

- `mutable-globals`
- `sign-ext`
- `nontrapping-fptoint`
- `exception-handling` (with exnref)
- `reference-types` (dependency of `exception-handling` with exnref)
- `simd128`
- `extended-constant-expressions` (possible dependency for R2R modules)
