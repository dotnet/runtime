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
| `build_id`  | A per-file value used to correlate a module with its symbols/sources.   | Native linking or the Webcil writer computes it when producing the file. |

`producers` is linker-driven so `wasm-ld` can merge entries from every tool. Each producer also owns
the identity of the file it creates.

## Scope

- Applies to application builds for both **Mono** and **CoreCLR**, with or without native relinking.

## `build_id`

Behavior:

- Native linking asks wasm-ld to generate a deterministic build ID for each `dotnet.native.wasm`.
  Runtime-pack modules therefore arrive in applications with their original ID intact, while an
  application native relink creates a new ID for the new binary.
- When `wasm-opt` rewrites a relinked native module, the build recomputes its ID from the final
  module with the previous `build_id` section removed.
- Regular and R2R Webcil modules use their own SHA-256 content hashes.
- Application builds preserve IDs already present in runtime-pack Wasm files.

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
- MSBuild wiring:
  - Mono: `src/mono/browser/build/BrowserWasmApp.targets` (+ shared logic in
    `src/mono/wasm/build/WasmApp.Common.targets`).
  - CoreCLR: `src/mono/browser/build/BrowserWasmApp.CoreCLR.targets`.

## Default runtime pack

Runtime-pack Wasm files already contain per-file IDs and remain unchanged in the NuGet cache and
application output. Native relinking and Webcil conversion produce new files with new IDs.

## MSBuild properties

| Property                     | Default | Meaning |
|------------------------------|---------|---------|
| `WasmEmitProducersSection`   | `true` on .NET 12+ | Emit/merge the `producers` section from the runtime-pack assembly source. |

## Open questions / future work

- **Upstream registration.** The `processed-by`/`sdk` values are not yet registered with the
  tool-conventions repository.
