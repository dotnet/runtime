# WebAssembly `producers` and `build_id` custom sections

This document describes how .NET emits the two WebAssembly [tool-conventions](https://github.com/WebAssembly/tool-conventions)
custom sections into `dotnet.native.wasm` for browser-wasm applications, and the decisions behind the
current (prototype) implementation.

Related issues:
- [dotnet/runtime#91049](https://github.com/dotnet/runtime/issues/91049) – emit `build_id`
- [dotnet/runtime#96334](https://github.com/dotnet/runtime/issues/96334) – emit `producers`

Specs:
- [ProducersSection.md](https://github.com/WebAssembly/tool-conventions/blob/main/ProducersSection.md)
- [BuildId.md](https://github.com/WebAssembly/tool-conventions/blob/main/BuildId.md)

## Summary of the two sections

| Section     | Purpose                                                                 | How it is produced |
|-------------|-------------------------------------------------------------------------|--------------------|
| `producers` | Static telemetry about the toolchain that produced the module.          | A small linkable object is passed to `wasm-ld`, which merges it into the module's `producers` section. |
| `build_id`  | A value used to correlate a module with its symbols/sources.            | Stamped into a project-local final module by a managed MSBuild task. |

`producers` is linker-driven so `wasm-ld` can merge entries from every tool. `build_id` is stamped
after selecting either the runtime-pack module or a relinked module, so it does not require a native
toolchain.

## Scope

- Applies to application builds for both **Mono** and **CoreCLR**, with or without native relinking.
- The **default** `dotnet.native.wasm` shipped in the runtime pack (consumed by non-relinking app
  builds) is intended to carry a `build_id` equal to the runtime git hash, stamped at runtime-build
  time and surfaced the same way the git hash is (see [Default runtime pack](#default-runtime-pack)).

## `build_id`

Behavior:

- `build_id` is emitted only when the app sets `$(WasmNativeBuildId)` to a hex string. There is
  intentionally no implicit random-per-build ID.
- The selected `dotnet.native.wasm` is copied to a project-local intermediate path before being
  modified. Runtime-pack files in the NuGet cache are never changed.
- The managed stamping task runs before static-web-asset fingerprinting, integrity calculation,
  compression, and bundling.
- Setting `$(WasmNativeBuildId)` does not force a native build or require `wasm-tools`.

## `producers`

The `producers` section records the toolchain. Per the spec it must appear at most once. `wasm-ld`
already **merges** the `producers` sections found in its input objects (clang/LLVM contribute
`language`/`processed-by` automatically). `wasm-ld` has no command-line option to inject additional
`producers` entries, so .NET contributes its entries by handing `wasm-ld` a tiny **relocatable wasm
object** whose only meaningful content is a `producers` custom section. `wasm-ld` merges it with the
rest, keeping the section single. .NET contributes:

- `language`: `C#`
- `processed-by`: the runtime name (`Mono` or `CoreCLR`) with the product version
- `sdk`: `.NET` with the product version

Emitting `producers` can be disabled with `$(WasmEmitProducersSection)=false`.

Emscripten strips `producers` by default, so the native link passes
`-sEMIT_PRODUCERS_SECTION=1` when `$(WasmEmitProducersSection)` is enabled. This preserves the
single section merged by `wasm-ld` through Emscripten's post-link processing.

## Implementation

- `src/tasks/Microsoft.NET.WebAssembly.Webcil/WasmCustomSectionWriter.cs` – a reusable managed writer.
  `WriteProducersObject` emits a minimal relocatable wasm object (module header + an empty `linking`
  section that marks it as an object + the `producers` custom section).
- `src/tasks/WasmAppBuilder/WasmEmitProducersObject.cs` – the MSBuild task wrapping the writer. It runs
  **before** the link and writes `dotnet-producers.o`, which is added to the link inputs.
- `src/tasks/Microsoft.NET.Sdk.WebAssembly.Pack.Tasks/WasmStampBuildId.cs` – copies the selected
  native module to a project-local path and replaces or appends its `build_id`.
- MSBuild wiring:
  - Mono: `src/mono/browser/build/BrowserWasmApp.targets` (+ shared logic in
    `src/mono/wasm/build/WasmApp.Common.targets`).
  - CoreCLR: `src/mono/browser/build/BrowserWasmApp.CoreCLR.targets`.

## JavaScript exposure

`build_id` is **not** read back from the compiled module. It is exposed on `runtimeBuildInfo.buildId`
through the generated boot configuration. Relinked native modules may also carry the value through
their Emscripten footer; the footer value takes precedence and the boot configuration is the
workload-free fallback.

- **Application build without relinking**: the generated boot configuration provides the
  application-stamped value.
- **Application re-link**: the (re)linked native module carries the value via the emscripten `.lib.js`
  footer, which reads `process.env.WASM_BUILD_ID` at link time. When present, this overrides the rollup
  constant (which cannot change on a relink, since `dotnet.runtime.js` is not rebuilt):
  - Mono: the footer emits it onto `emscriptenBuildOptions.buildId`; `passEmscriptenInternals` copies it
    onto `runtimeBuildInfo.buildId`.
  - CoreCLR: the footer exposes it on the `$DOTNET` object; the native module init copies it onto
    `runtimeBuildInfo.buildId`.

```js
const { runtimeBuildInfo } = await dotnet.create();
console.log(runtimeBuildInfo.buildId); // "" when no build_id was stamped
```

## Default runtime pack

The runtime-pack `dotnet.native.wasm` remains unchanged in the NuGet cache. When an application sets
`$(WasmNativeBuildId)`, the build copies that module to its intermediate output, stamps the copy, and
uses the copy for that application's static web assets. When the property is empty,
`runtimeBuildInfo.buildId` is `""` unless the selected runtime module already provides a value.

## MSBuild properties

| Property                     | Default | Meaning |
|------------------------------|---------|---------|
| `WasmEmitProducersSection`   | `true`  | Emit/merge the `producers` section (via a linkable object). |
| `WasmNativeBuildId`          | (unset) | Hex string to stamp as the `build_id` without forcing a native build. Empty means no application-stamped `build_id` section. |

## Open questions / future work

- **Upstream registration.** The `processed-by`/`sdk` values are not yet registered with the
  tool-conventions repository.
