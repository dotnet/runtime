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
| `build_id`  | A value used to correlate a module with its symbols/sources.            | Emitted by `wasm-ld` via the `--build-id` linker flag. |

Both sections are **linker-driven**: they are produced by/through `wasm-ld` during the native link,
rather than by rewriting the module after the fact.

## Scope

- Applies to **application** builds that relink the native runtime (`WasmBuildNative=true`), for both
  **Mono** and **CoreCLR** browser-wasm runtimes (in-tree targets only).
- The **default** `dotnet.native.wasm` shipped in the runtime pack (consumed by non-relinking app
  builds) is intended to carry a `build_id` equal to the runtime git hash, stamped at runtime-build
  time and surfaced the same way the git hash is (see [Default runtime pack](#default-runtime-pack)).

## `build_id`

`build_id` is emitted by the WebAssembly linker. `wasm-ld` accepts, among others:

```
--build-id=0x<hexstring>   # a caller-specified value
--build-id=uuid            # random UUID per link
--build-id=fast|sha1|none
```

Behavior:

- `build_id` is emitted **only** when the app sets `$(WasmNativeBuildId)` to a hex string; the link
  then uses `--build-id=0x<hex>` to pin that exact value. There is intentionally **no** implicit
  random-per-build id.
- Setting `$(WasmNativeBuildId)` **forces a native build**. If `WasmBuildNative=false` is also set, the
  build fails with an error, because a specific `build_id` can only be applied by relinking.

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

> Note: emscripten runs `wasm-opt` after the link. Binaryen preserves the `producers` section by
> default, so the merged entries survive optimization; this should be verified end-to-end by a real relink.

## Implementation

- `src/tasks/Microsoft.NET.WebAssembly.Webcil/WasmCustomSectionWriter.cs` – a reusable managed writer.
  `WriteProducersObject` emits a minimal relocatable wasm object (module header + an empty `linking`
  section that marks it as an object + the `producers` custom section).
- `src/tasks/WasmAppBuilder/WasmEmitProducersObject.cs` – the MSBuild task wrapping the writer. It runs
  **before** the link and writes `dotnet-producers.o`, which is added to the link inputs.
- MSBuild wiring:
  - Mono: `src/mono/browser/build/BrowserWasmApp.targets` (+ shared logic in
    `src/mono/wasm/build/WasmApp.Common.targets`).
  - CoreCLR: `src/mono/browser/build/BrowserWasmApp.CoreCLR.targets`.

## JavaScript exposure

`build_id` is **not** read back from the compiled module. It is exposed on `runtimeBuildInfo.buildId`
through the same two-channel mechanism used for options such as `wasmEnableSIMD` (both channels are fed
by the `WASM_BUILD_ID` environment variable, kept in sync with the `--build-id` flag):

- **Default `dotnet.*.js` build** (no relink): a rollup constant (`consts:buildId`) baked into
  `dotnet.js`/`dotnet.runtime.js` at runtime-pack build time provides the value.
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

The default `dotnet.native.wasm` in the runtime pack is linked by the core native build. To give it a
stable `build_id` equal to the runtime git hash, that build sets `$(WasmNativeBuildId)` to the git hash
(so `--build-id` stamps the section) and passes the matching `WASM_BUILD_ID` into both the rollup
constant and the native footer, so `runtimeBuildInfo.buildId` reports the same value the way the git
hash is reported. When a value is not
stamped, `runtimeBuildInfo.buildId` is `""`, accurately reflecting the absence of a `build_id` section.

## MSBuild properties

| Property                     | Default | Meaning |
|------------------------------|---------|---------|
| `WasmEmitProducersSection`   | `true`  | Emit/merge the `producers` section (via a linkable object). |
| `WasmNativeBuildId`          | (unset) | Hex string to stamp as the `build_id`. Forces a native build. Empty means no `build_id` section. |

## Open questions / future work

- **Upstream registration.** The `processed-by`/`sdk` values are not yet registered with the
  tool-conventions repository.
