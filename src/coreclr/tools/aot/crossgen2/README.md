# Crossgen2 partitioned Browser-WASM output

`--wasm-auxiliary-modules` partitions a composite Browser-WASM ReadyToRun image
into independently activated WebAssembly modules. It requires a single-threaded
target and retained IL; it cannot be combined with `--strip-il-bodies`.

For example:

```shell
crossgen2 app.dll --composite --targetarch wasm --targetos browser \
    --wasm-auxiliary-modules -r:System.Private.CoreLib.dll -o:app.r2r.wasm
```

In addition to the composite image and its component forwarding images, this emits:

| File | Contents |
|---|---|
| `app.r2r.wasm` | Hot methods, interpreter fallback thunks, shared imports and reserved logical memory/table layout |
| `app.r2r.entrypoints.wasm` | Auxiliary sparse and instantiated-method entrypoint records |
| `app.r2r.1.wasm` | Cold methods and funclets, their fixup lists, exclusive import cells/signatures, GC/unwind and exception data |
| `app.r2r.auxiliary.json` | Sibling filenames: `{"entrypoints":"app.r2r.entrypoints.wasm","modules":["app.r2r.1.wasm"]}` |

Public nongeneric methods initially stay hot. Compiler-generated wrappers,
unmanaged entrypoints, runtime helpers and async/resumption methods also stay hot.
Other eligible IL methods go into the cold module. Managed calls use indirect
imports, and non-managed helper function bodies are duplicated where needed.

All modules use the primary image's `webcil` imports and the same memory/table
bases. Sidecar instantiation does not install code or data. Its idempotent
`ActivateModule()` export installs passive data, rebases its owned import cells,
installs cold table entries, then publishes activation status. The entrypoint and
body sidecars can activate in either order. The host must notify the runtime when
entrypoints activate.

The primary exports `getAuxiliaryModuleMetadata(): i32`. It returns an absolute
pointer to section 128, or zero for an ordinary image. The header has four
little-endian DWORDs: entrypoint status, primary managed function count, auxiliary
module count, and entrypoint filename RVA. Each auxiliary descriptor has four
DWORDs: filename RVA, table-relative start, managed function count, and status.
Counts include funclets but exclude helper slots. Status zero means pending;
status one means active. Sections 129 and 130 hold auxiliary per-component
MethodDef and image-wide instantiated-method entrypoints.

Shared/eager imports, method read-only/writable data and async resume-info remain
resident. The final runtime-function entry and its unwind data also remain
resident so the runtime can reserve the complete virtual-IP range before download.
Cold metadata never overwrites a fixup cell that primary code can resolve.

Per-module WebAssembly name sections and primary symbol maps are supported.
Single-image map, PDB, perfmap and call-chain profile output is rejected because
its file-relative addresses cannot describe multiple physical modules.
