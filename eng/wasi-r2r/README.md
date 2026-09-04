# WASI composite-R2R splice tooling

Tooling for composing a **composite ReadyToRun image into a CoreCLR/WASI component**.
The in-tree CoreCLR-WASI app builder invokes it when `PublishReadyToRun=true`. The shipping WASI SDK
does not select the CoreCLR app builder yet, so this remains an experimental in-tree path.

Scope note, since this is easy to over-read: **the splice is a WASI requirement, not a composite
requirement.** `WasiStaticR2RProbe` serves `composite-r2r.wasm` only from a baked-in buffer that the
splice populates, so on WASI there is no way to hand the runtime a composite from disk. Browser has
no such constraint — `crossgen2 --composite --targetos:browser` plus a flat directory driven by
`corerun.js` works without any of this tooling. Browser also has a productised **non-composite** path
since [#132339](https://github.com/dotnet/runtime/pull/132339) (`-p:PublishReadyToRun=true`); that
path declines composite, but only as an SDK opt-out.

## Pieces

| Path | Purpose |
| --- | --- |
| `pipeline_shim.py` | The splice pipeline: unbundle → extract image base → generate shim → `wasm-merge` → `wasm-opt` fold → module-swap. |
| `comp.rsp.template` | `crossgen2` composite response file; replace `@ROOT@` with your worktree root. |

## Prerequisites

- `wasm-tools` and Binaryen (`wasm-merge`, `wasm-opt`) on `PATH`, plus Python 3.8+.
  `pipeline_shim.py` fails fast if any are missing. **WABT is not required** — the shim is
  assembled directly and every value that used to come from `wasm-objdump` is parsed from the
  module, which is also what lets the pipeline run on Windows.
- `wasmtime` on `PATH` for running the result.

There is no longer an out-of-repo dependency. The pipeline previously required `Nesm.dll` (a wasm
reader/writer from outside this repo) to drive two tools, `surgery` and `activate`, which rewrote the
merged module after the fact. Both are gone — see [How the splice works](#how-the-splice-works).

## Is the splice still needed?

**Yes, for WASI.** [#131016](https://github.com/dotnet/runtime/pull/131016) added VM-side loading of a
flat webcil composite, and that code is present — `NativeImage::Open` has a `TARGET_WASM` branch that
takes the R2R header from the decoder instead of the `RTR_HEADER` export. But it does not make direct
deployment work here, because the **WASI host probe never serves the composite from disk**:
`WasiStaticR2RProbe` ([`wasi_r2r_probe.hpp`](../../src/coreclr/hosts/corerun/wasi_r2r_probe.hpp))
special-cases `composite-r2r.wasm` and returns the baked-in `g_wasi_r2r_image` buffer, which only the
splice populates. Per-assembly stubs *are* read from `comp/<name>.wasm` on disk; the composite is not.

Measured on a stock (unspliced) `corerun` with the composite deployed alongside — both in the run root
and colocated in `comp/` — this is what happens:

1. `g_wasi_r2r_image` is empty, so `WasiWebcilPayloadSize` returns `<= 0` and the probe returns `false`.
2. `OpenR2RFromPE` falls through to `PEImageLayout::LoadNative`, which reads the raw file.
3. The file begins `\0asm` — it is webcil *wrapped in wasm* — so `WebcilDecoder::DetectWebcilFormat`,
   which tests for the ASCII bytes `WbIL`, returns false.
4. `InitDecoders` therefore selects `FORMAT_PE` and runs `PEDecoder` over a wasm file.

The result is **not** a graceful fallback. It is an out-of-bounds trap during EE startup:

```
0: corerun!PEDecoder::FindReadyToRunHeader() const
1: corerun!NativeImage::Open(...)
2: corerun!AssemblyBinder::LoadNativeImage(...)
3: corerun!AcquireCompositeImage(...)
4: corerun!ReadyToRunInfo::Initialize(...)
...
memory fault at wasm address 0x6541cc8b in linear memory of size 0x8000000
wasm trap: out of bounds memory access
```

That backtrace is the signature of this deployment gap. It looks like a broken composite and reads
like "R2R does not work on wasm"; it is neither. Gate it with `DOTNET_ReadyToRun=0` — if the app then
runs clean, the composite was simply never delivered to the runtime, and you need the splice.


## Usage

The in-tree publish path drives crossgen2, sizes the host's image buffer and table from the generated
composite, invokes the splice, and deploys the component stubs:

```bash
./dotnet.sh publish <project> -c Release -p:TargetOS=wasi \
  -p:RuntimeFlavor=CoreCLR -p:PublishReadyToRun=true
```

For manual experiments, `pipeline_shim.py` still accepts `COMP`, `CORERUN`, `OUTDIR`, and `ROOT`
through the environment. It prints the resolved bases, then `VALID` and the output path on success.

The activation log alone is not proof that a method executed from the composite. For a deterministic
check, break on the app method's wasm function from the final component; an interpreted fallback
cannot hit a breakpoint inside the R2R body.

### Runtime tests

The WASI runtime-test pipeline has separate interpreter and `R2R_CG2` legs. The R2R leg uses
Checked CoreCLR with Release libraries. Like the browser R2R leg, it compiles each merged runner
and its test assemblies at execution time. Unlike browser, it compiles them as one composite and
splices that composite into a private copy of `CORE_ROOT/corerun`.

The generated Bash wrappers enable this path with `RunCrossGen2=1`. They stage the assembly stubs
under `IL-CG2/wasi-r2r/comp`, run the splice from `CORE_ROOT/wasi-r2r/pipeline_shim.py`, and launch
`IL-CG2/wasi-r2r/corerun-composite.wasm`. `APP_ASSEMBLIES=EXTERNAL` enables the host's assembly
probe, `CORE_LIBRARIES` points it at these stubs, and `TEST_READY_TO_RUN_MODE=1` reaches the guest
for R2R-specific test conditions.
Without `RunCrossGen2`, the wrapper uses the unmodified interpreter host and does not probe those
stubs. Composition failures fail the test rather than falling back to interpretation.

Helix receives pinned Binaryen and `wasm-tools` binaries through a separate correlation payload,
provisioned by `eng/testing/wasi-r2r-provisioning.targets`; it does not need these tools preinstalled
in the queue image. Local runs need the same tools and Python 3 on `PATH`.

The runtime-test path compiles only the test assemblies, not the framework. It uses the shared
corerun's reserved image buffer and table slots, whose bounds the splice checks before merging.
Publish still sizes and relinks its host from the complete app/framework composite.

### Cost at framework scale

Measured on a 232,673-function framework composite (post-#132906, so 4 exports and a 28.8 MB `name`
section) spliced into `corerun`:

| step | wall | peak RSS | output |
| --- | --- | --- | --- |
| `wasm-merge -g` | — | **4.25 GB** | 134,753,587 bytes |
| `wasm-opt --simplify-globals -g` | 6.19 s | **2.70 GB** | names 34,152,317 bytes, 232,673 named |

The peak is the whole working set, not a delta, so it is straightforward to measure and reproduce.
Fine on a dev box; **a CI container with a 4 GB limit will not survive the merge.** Size the runner
before putting this in a pipeline.

`wasm-merge` renumbers the name map alongside the functions, verified at this scale: the composite's
function 0 lands at merged index 10,105, offset by corerun's own function count, and
`System_Console_System_Console__WriteLine` resolves at 17,514. A name section carried through
*unshifted* would have produced wrong names everywhere while still validating and still running, so
this is worth knowing rather than assuming.

**Both `-g` flags are load-bearing.** Dropping it from the fold removes the `name` section entirely
and `wasm-tools validate` still answers `YES` — measured, not inferred. Since #132906 the name
section is the only record of function names, so a post-processing step without `-g` silently
anonymises every frame.

## How the splice works

The composite `crossgen2` emits is **self-installing**: the webcil payload is an ACTIVE data segment
at `(global.get __memory_base)` and the R2R function table is an ACTIVE element segment at
`(global.get __table_base)`, so the engine installs both at instantiation. Nothing has to rewrite the
module afterwards, which is what retired `activate`.

`corerun` supplies five of the composite's seven imports directly, via link flags in
[`corerun/CMakeLists.txt`](../../src/coreclr/hosts/corerun/CMakeLists.txt):

```
-Wl,--table-base=<N+1>          # reserve table slots 1..N for the composite
-Wl,--export-table              # -> __indirect_function_table
-Wl,--export=__stack_pointer
-Wl,--export=__coreclr_wasm_rtlrestorecontext_tag
-Wl,--export=__async_continuation
```

That covers `memory`, `__indirect_function_table`, `__stack_pointer`,
`__coreclr_wasm_rtlrestorecontext_tag` and `__async_continuation`.

**The two it cannot supply are `__memory_base` and `__table_base`.** `wasm-ld` creates those globals
only in PIC mode, and a wasm global whose initializer is a data symbol's address is not expressible
from C — which is exactly what `surgery` used to inject post-link. `pipeline_shim.py` generates a
six-line shim module exporting them as constants and merges it as a third input, which retired
`surgery`.

Three things about this are easy to get wrong:

- **`--table-base`, not a growable table.** An ACTIVE element segment is installed by the engine at
  instantiation, so the table must *already* be large enough; growth at runtime does not help.
  Reserve by the composite's **function** count, not its assembly count. The reservation keeps the
  table fixed-size (`min == max`) so it still validates statically, and costs little — the extra bytes
  come from wider LEB encodings for the shifted indices, not from the table. Measured: `6298/6298` →
  `71834/71834` at `--table-base=65537`, +51 KB (0.14%) at 500,001 slots.
- **The fold is required, and is not free.** Merging internalizes the imported globals, and
  `global.get` of a *defined* global is a constant expression only under the GC proposal — so the
  merge needs `--enable-gc` and the result needs `wasm-opt --simplify-globals` to be portable
  (wasmtime rejects the unfolded form under `exceptions` alone; V8 accepts it, so "it loaded in node"
  proves nothing). The pass also propagates globals into function bodies, costing ~3.7% code size.
  A host that supplies the bases at *instantiation* instead — as the browser does — keeps `global.get`
  of an **imported** global, which is valid MVP, and pays neither cost.
- **Payload offset 28 must be patched before the runtime reads it.** The composition shim calls the
  composite's exported `patchWebcilHeader` from its start function, so the image owns its format.
  [`wasi_r2r_probe.hpp`](../../src/coreclr/hosts/corerun/wasi_r2r_probe.hpp) retains a native fallback
  only for older composites that do not export that function.

  This is measured, not argued. Setting the host's table base to 2 while the shim installs at 1 makes
  the run fail with `wasm trap: indirect call type mismatch` — a symptom nowhere near its cause. Note
  the corollary for the open `call_indirect` bugs: **table-index misalignment is a producer of that
  symptom, so a signature mismatch is not by itself evidence of a signature-encoding fault.**

## Historical note: the removed nesm dependency

`surgery` and `activate` existed because nothing supplied the composite's imports at link time and
nothing emitted its segments in active form. Both were addressable, and the result is *more*
declarative than the pipeline they replaced rather than less. Kept here because the measurement that
sized the reservation is still the one to reuse, and because the import accounting is easy to get
wrong in the same way twice.

**The host half is *mostly* done by the linker — five of the composite's seven imports, not all.**

> **Correction, recorded because the wrong number was load-bearing.** An earlier revision claimed
> **six** of seven, implying only one gap. Enumerating the exports of the corerun actually built with
> these flags gives nine — `cabi_realloc`, `GetDotNetRuntimeContractDescriptor`, `memory`,
> `wasi:cli/run@0.2.0#run`, `wasi_r2r_image_base`, `__async_continuation`,
> `__coreclr_wasm_rtlrestorecontext_tag`, `__indirect_function_table`, `__stack_pointer` — of which
> **five** match composite imports. `--table-base` shifts the table layout but creates no exported
> `__table_base` global, and `wasi_r2r_image_base` is a *function*, so it cannot satisfy a global
> import. Independently corroborated: merging the real composite into the real browser `corerun.wasm`
> leaves exactly `__memory_base` and `__table_base` unresolved and nothing else. Two hosts, two
> toolchains, same two globals — which is what identified the shim as the remaining work.

The extraction step the shim depends on is *not* new: reading the image base out of the linked host
was already how `surgery` got its argument. `wasi_r2r_image_base`'s body is a single
`i32.const <addr>` (it returns `&g_wasi_r2r_image[0]`), so it decodes statically with no
instantiation. Two things to carry forward:

- `wasm-tools component unbundle` is **mandatory** first — `corerun` is a WASI component and
  core-module readers reject components outright.
- The old shell pipeline extracted these values by scraping `wasm-objdump` text, and that is
  where its sharpest edges were: the `awk` form silently yielded an **empty string** under BSD
  `awk` (the macOS default), and the payload-size scrape selected `segment[1]` positionally and
  skipped its own cap check when the scrape came back empty. `pipeline_shim.py` parses the
  sections instead and selects by meaning — the payload is "the one active data segment", not
  an index — so a layout change is an error rather than a silently skipped check. The general
  lesson outlives the port: **scraping a disassembler's text makes a missing value
  indistinguishable from a zero.**

Measured on the real 36 MB corerun: table `6298/6298` → `71834/71834` with `--table-base=65537`,
exports 6 → 9, and the run still passes with `DOTNET_ReadyToRun=0` (verified against a same-binary
control, since the `StackTrace` frame count differs between R2R on and off for unrelated reasons).

Cost of the reservation is small and mostly independent of its size — the extra bytes come from wider
LEB encodings for the shifted function indices, not from the table itself:

| `--table-base` | corerun bytes | table min/max |
| --- | --- | --- |
| default (1) | 36,284,003 | 6,298 |
| 65,537 | 36,284,095 | 71,834 |
| 500,001 | 36,336,407 | 506,298 |

**Size from the composite's function count, not its assembly count.** Every function consumes a
table slot. The publish target inspects the completed composite before linking the host, reserves
exactly `function count + 1` table slots, and supplies a strong image-buffer symbol whose size exactly
matches the active payload. Non-R2R app links use the host archive's 64-byte weak fallback instead of
paying a fixed 16 MiB reservation.

That leaves the whole splice as `wasm-tools component unbundle` → `wasm-merge` → reassemble, all
standard tooling.

> **Do not populate the image or table from a `start` function.** A composite that grows its own table and populates it
> via `table.init`/`memory.init` at startup does work — verified end-to-end, including that
> `wasm-merge` correctly combines two start functions. But it replaces declarative, engine-applied
> installation with guest code mutating its own dispatch table at runtime, and it forfeits the
> statically-known table size. The small start function used here only calls `patchWebcilHeader`;
> active segments still install the payload and function table declaratively.
