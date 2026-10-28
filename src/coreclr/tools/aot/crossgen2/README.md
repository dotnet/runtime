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

Without `--mibc`, public nongeneric methods on nongeneric types initially stay hot. Compiler-generated wrappers,
unmanaged entrypoints, runtime helpers and async/resumption methods also stay hot.
Other eligible IL methods go into the cold module. Ordinary managed calls use
indirect imports. Eligible managed JIT helpers remain directly callable: their
bodies and funclets are copied into body modules while their logical table slots,
virtual-IP identities and metadata remain canonical in the primary.
Non-managed helper function bodies are also duplicated where needed.

With one or more `--mibc startup.mibc` inputs, methods present in the union of
the profiles stay primary, regardless of visibility or genericity. Remaining
eligible methods go cold. Concrete reference-type instantiations select their
shared canonical body; value-type instantiations remain distinct. Mandatory
primary methods and helpers keep the rules above. An explicitly supplied empty
profile, or one with no matching methods, moves every eligible method cold;
it does not restore the no-profile heuristic. Embedded IBC and synthesized
optimization profiles do not select auxiliary-module ownership.

For example:

```shell
crossgen2 app.dll --composite --targetarch wasm --targetos browser \
    --wasm-auxiliary-modules --mibc startup.mibc \
    -r:System.Private.CoreLib.dll -o:app.r2r.wasm
```

Partitioning uses method membership, not a CPU-weight threshold. Use a profile
captured over the intended startup/interaction interval and matching the
published assemblies. This option consumes existing MIBC data; it does not
collect a browser profile.

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
