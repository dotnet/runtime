# WebAssembly `producers` and `build_id` custom sections

This document describes how .NET 12 and later emit the two WebAssembly [tool-conventions](https://github.com/WebAssembly/tool-conventions)
custom sections into `dotnet.native.wasm` for browser-wasm applications, and the decisions behind the
current (prototype) implementation. Older target frameworks retain their existing output without
these metadata sections.

Related issues:
- [dotnet/runtime#91049](https://github.com/dotnet/runtime/issues/91049) – emit `build_id`
- [dotnet/runtime#96334](https://github.com/dotnet/runtime/issues/96334) – emit `producers`

Specs:
- [ProducersSection.md](https://github.com/WebAssembly/tool-conventions/blob/main/ProducersSection.md)
- [BuildId.md](https://github.com/WebAssembly/tool-conventions/blob/main/BuildId.md)

## Summary of the two sections

| Section     | Purpose                                                                 | How it is produced |
|-------------|-------------------------------------------------------------------------|--------------------|
| `producers` | Static telemetry about the toolchain that produced the module.          | Assembly source is passed to Emscripten and merged by `wasm-ld` into the module's `producers` section. |
| `build_id`  | A value used to correlate a module with its symbols/sources.            | Stamped into a project-local final module by a managed MSBuild task. |

`producers` is linker-driven so `wasm-ld` can merge entries from every tool. `build_id` is stamped
after selecting either the runtime-pack module or a relinked module, so it does not require a native
toolchain.

## Scope

- Applies to application builds for both **Mono** and **CoreCLR**, with or without native relinking.

## `build_id`

Behavior:

- When the app sets `$(WasmNativeBuildId)` to a hex string, that build-wide ID is stamped into every
  emitted Wasm file: `dotnet.native.wasm`, regular Webcil, and R2R Webcil.
- Without an explicit ID, each Webcil module uses its own SHA-256 content hash while
  `dotnet.native.wasm` has no application-stamped build ID.
- Shared runtime-pack files are copied to project-local intermediate paths before being modified.
  Runtime-pack files in the NuGet cache are never changed.
- The managed stamping task runs before static-web-asset fingerprinting, integrity calculation,
  compression, and bundling.
- Setting `$(WasmNativeBuildId)` does not force a native build or require `wasm-tools`.

## `producers`

The `producers` section records the toolchain. Per the spec it must appear at most once. `wasm-ld`
already **merges** the `producers` sections found in its input objects (clang/LLVM contribute
`language`/`processed-by` automatically). `wasm-ld` has no command-line option to inject additional
`producers` entries, so each .NET 12+ browser runtime pack ships a tiny **WebAssembly assembly
source** defining a `producers` custom section. Native relinks hand that source to Emscripten, which
assembles it before `wasm-ld` merges it with the rest while keeping the section single. .NET contributes:

- `processed-by`: the runtime name (`Mono` or `CoreCLR`) with the product version
- `sdk`: `.NET` with the product version

Emitting `producers` can be disabled with `$(WasmEmitProducersSection)=false`.

Emscripten strips `producers` by default, so the native link passes
`-sEMIT_PRODUCERS_SECTION=1` when `$(WasmEmitProducersSection)` is enabled. This preserves the
single section merged by `wasm-ld` through Emscripten's post-link processing.

## Implementation

- `src/coreclr/tools/Common/Wasm/WasmCustomSectionWriter.cs` – a reusable managed writer for Webcil,
  R2R Webcil, and build-ID stamping.
- `src/tasks/WasmAppBuilder/WasmEmitProducersSource.cs` – emits the flavor- and version-specific
  `dotnet-producers.s` assembly source. Labels let the assembler encode string lengths without
  duplicating WebAssembly object-file logic in managed code.
- `src/installer/pkg/sfx/Microsoft.NETCore.App/Microsoft.NETCore.App.Runtime.props` – packages the
  producer source under `runtimes/browser-wasm/native`.
- Full repo builds generate an intermediate source while linking the prebuilt Mono/CoreCLR
  browser runtime, which runs before the runtime pack is assembled. Application relinks consume the
  shipped runtime-pack source.
- `src/tasks/Microsoft.NET.Sdk.WebAssembly.Pack.Tasks/WasmStampBuildId.cs` – copies the selected
  native module to a project-local path and replaces or appends its `build_id`.
- MSBuild wiring:
  - Mono: `src/mono/browser/build/BrowserWasmApp.targets` (+ shared logic in
    `src/mono/wasm/build/WasmApp.Common.targets`).
  - CoreCLR: `src/mono/browser/build/BrowserWasmApp.CoreCLR.targets`.

## JavaScript exposure

`build_id` is **not** read back from the compiled module. The generated boot configuration carries
the normalized value and the loader exposes it on `runtimeBuildInfo.buildId` for both relinked and
non-relinked applications.

```js
const { runtimeBuildInfo } = await dotnet.create();
console.log(runtimeBuildInfo.buildId); // "" when no build_id was stamped
```

## Default runtime pack

Runtime-pack Wasm files remain unchanged in the NuGet cache. When an application sets
`$(WasmNativeBuildId)`, the build copies shared native and Webcil modules to project-local output
before stamping them. When the property is empty, `runtimeBuildInfo.buildId` is `""`, native Wasm
has no application-stamped build ID, and Webcil modules retain their per-module content hashes.
When native relinking already produced a project-local module, the build stamps that module in place.

## MSBuild properties

| Property                     | Default | Meaning |
|------------------------------|---------|---------|
| `WasmEmitProducersSection`   | `true` on .NET 12+ | Emit/merge the `producers` section from the runtime-pack assembly source. |
| `WasmNativeBuildId`          | (unset) | .NET 12+: build-wide hex string stamped into every emitted Wasm file without forcing a native build. Empty leaves native Wasm unstamped and uses per-module hashes for Webcil. |

## Open questions / future work

- **Upstream registration.** The `processed-by`/`sdk` values are not yet registered with the
  tool-conventions repository.
