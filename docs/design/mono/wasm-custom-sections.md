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
| `producers` | Static telemetry about the toolchain that produced the module.          | Written by a managed post-link MSBuild task. |
| `build_id`  | A value that is (with high probability) unique per build, used to correlate a module with its symbols/sources. | Emitted by `wasm-ld` via the `--build-id` linker flag. |

The two sections are independent: `producers` is deterministic toolchain metadata, whereas `build_id`
is intended to change whenever the produced binary changes.

## Scope of the prototype

- Applies to **application** builds that relink the native runtime (`WasmBuildNative=true`), for both
  **Mono** and **CoreCLR** browser-wasm runtimes (in-tree targets only).
- Non-native app builds (which consume a prebuilt `dotnet.native.wasm` from the runtime pack) keep
  whatever the runtime pack shipped. Baking a git-hash `build_id` into the runtime pack at
  runtime-build time is future work (see "Open questions").

## `build_id`

`build_id` is emitted by the WebAssembly linker. `wasm-ld` accepts:

```
--build-id                 # default algorithm (sha1)
--build-id=uuid            # random UUID per link
--build-id=0x<hexstring>   # a caller-specified value
--build-id=fast|sha1|none
```

Behavior:

- When the app does **not** set `$(WasmNativeBuildId)`, the link uses `--build-id=uuid`, so each native
  build gets a fresh random id (answer to "use a GUID for native builds which didn't specify it").
- When the app sets `$(WasmNativeBuildId)` to a hex string, the link uses `--build-id=0x<hex>` to pin
  that exact value.
- Setting `$(WasmNativeBuildId)` **forces a native build**. If `WasmBuildNative=false` is also set, the
  build fails with an error, because a specific `build_id` can only be applied by relinking.

Emitting `build_id` can be disabled with `$(WasmEmitBuildId)=false`.

## `producers`

The `producers` section records the toolchain. Per the spec it must appear at most once, so the writer
**merges** into any existing `producers` section (e.g. one already emitted by clang/LLVM/Emscripten)
rather than adding a second one. .NET contributes:

- `language`: `C#`
- `processed-by`: the runtime name (`Mono` or `CoreCLR`) with the product version
- `sdk`: `.NET` with the product version

Emitting `producers` can be disabled with `$(WasmEmitProducersSection)=false`.

## Implementation

- `src/tasks/Microsoft.NET.WebAssembly.Webcil/WasmCustomSectionWriter.cs` – a reusable managed writer
  that parses a wasm module and writes/merges the `producers` and `build_id` custom sections.
- `src/tasks/WasmAppBuilder/WasmWriteMetadataSections.cs` – the MSBuild task wrapping the writer. It
  runs **after** the link step (and thus after `wasm-opt`), so the `producers` section it adds is not
  stripped by optimization.
- MSBuild wiring:
  - Mono: `src/mono/browser/build/BrowserWasmApp.targets` (+ shared logic in
    `src/mono/wasm/build/WasmApp.Common.targets`).
  - CoreCLR: `src/mono/browser/build/BrowserWasmApp.CoreCLR.targets`.

## JavaScript exposure

The `build_id` is read back from the compiled module at load time using
`WebAssembly.Module.customSections(module, "build_id")` and exposed as a lowercase hex string on the
runtime API:

```js
const { runtimeBuildInfo } = await dotnet.create();
console.log(runtimeBuildInfo.buildId); // "" when the module has no build_id section
```

## MSBuild properties

| Property                     | Default | Meaning |
|------------------------------|---------|---------|
| `WasmEmitBuildId`            | `true`  | Emit the `build_id` section (via the `--build-id` link flag). |
| `WasmEmitProducersSection`   | `true`  | Emit/merge the `producers` section (via the post-link task). |
| `WasmNativeBuildId`          | (unset) | Hex string to pin the `build_id`. Forces a native build. |

## Open questions / future work

- **`wasm-opt` and custom sections.** `wasm-opt` may drop unknown custom sections during optimization.
  The `producers` section is added after `wasm-opt` so it survives; the linker-emitted `build_id` runs
  before `wasm-opt`. If it is stripped, an alternative is to also write `build_id` via the post-link
  task (which would additionally give MSBuild a known id value to expose without reading it back).
- **Non-native builds.** Provide a shared `build_id` for apps that do not relink (e.g. the VMR/runtime
  git hash baked into the runtime pack `dotnet.native.wasm` at runtime-build time).
- **Upstream registration.** The `processed-by`/`sdk` values are not yet registered with the
  tool-conventions repository.
