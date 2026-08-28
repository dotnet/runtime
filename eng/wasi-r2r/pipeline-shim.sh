#!/usr/bin/env bash
# Splice a wasm R2R composite into corerun using only stock tooling — no nesm.
#
# Replaces pipeline-sym.sh (surgery + activate) for composites emitted by a crossgen2 that
# produces SELF-INSTALLING images: the webcil payload as an ACTIVE data segment at
# (global.get __memory_base) and the R2R function table as an ACTIVE element segment at
# (global.get __table_base). The engine installs both at instantiation.
#
# corerun supplies five of the composite's seven imports directly (memory, __stack_pointer,
# __indirect_function_table, __coreclr_wasm_rtlrestorecontext_tag, __async_continuation). The
# remaining two are the base globals, which wasm-ld only creates in PIC mode — so a generated
# shim module supplies them instead. That is what surgery used to do by post-link injection.
#
# Requires: wasm-tools, wabt (wasm-objdump, wat2wasm), binaryen (wasm-merge, wasm-opt), python3.
#
#   COMP=<composite.wasm> CORERUN=<corerun component> ./pipeline-shim.sh
set -euo pipefail

ROOT=${ROOT:-$(cd "$(dirname "$0")/../.." && pwd)}
COMP=${COMP:-$ROOT/r2rtest/out2/composite-r2r.wasm}
CORERUN=${CORERUN:-$ROOT/artifacts/obj/coreclr/wasi.wasm.Release/hosts/corerun/corerun}
D=${OUTDIR:-$ROOT/r2rtest/shimout}

# The table slot at which the composite installs. Must match WASI_R2R_TABLE_BASE in
# corerun/wasi_r2r_probe.hpp, which patches it into the webcil header at payload offset 28.
# Under -Wl,--table-base=N the linker leaves slots 1..N-1 free, so this is always 1.
TABLE_BASE=${TABLE_BASE:-1}

[ -f "$COMP" ]    || { echo "error: composite not found at '$COMP'" >&2; exit 1; }
[ -f "$CORERUN" ] || { echo "error: corerun not found at '$CORERUN'" >&2; exit 1; }

rm -rf "$D"; mkdir -p "$D"

# 1. Unbundle the corerun component -> core module. Mandatory: corerun is a WASI component and
#    wasm-objdump rejects components outright ("wasm components are not yet supported").
wasm-tools component unbundle "$CORERUN" --module-dir "$D" -o /dev/null >/dev/null 2>&1
MAIN=$(ls "$D"/*module0*.wasm | head -1)

# 2. Read the image base out of the LINKED host. wasi_r2r_image_base's body is a single
#    i32.const holding &g_wasi_r2r_image[0], so it decodes statically with no instantiation.
#    NOTE: use sed, not awk. The awk form in pipeline-sym.sh silently yields an EMPTY string
#    under BSD awk (the macOS default), which would feed an empty base to the shim.
IDX=$(wasm-objdump -j Export -x "$MAIN" | grep -i wasi_r2r_image_base | grep -oE 'func\[[0-9]+\]' | grep -oE '[0-9]+')
ADDR=$(wasm-objdump -d "$MAIN" | grep -A1 "func\[$IDX\] <wasi_r2r_image_base>" \
        | grep 'i32\.const' | sed -E 's/.*i32\.const +([0-9]+).*/\1/')
case "$ADDR" in ''|*[!0-9]*) echo "error: could not extract imageBase (got '$ADDR')" >&2; exit 1;; esac

TBL=$(wasm-objdump -x "$MAIN" | grep -iE "^ - table\[0\]" | grep -oE "initial=[0-9]+" | grep -oE "[0-9]+")
NFUNC=$(wasm-objdump -h "$COMP" | grep -iE "^ Function " | grep -oE "count: [0-9]+" | grep -oE "[0-9]+")
echo "SHIM: imageBase=$ADDR tableBase=$TABLE_BASE hostTable=$TBL compositeFuncs=$NFUNC"

# The engine applies an ACTIVE element segment at instantiation, so the host table must already
# be large enough. Too small is an instantiation failure, which is loud — but catching it here
# names the cause instead of leaving "active segments don't work".
if [ "$((TABLE_BASE + NFUNC))" -gt "$TBL" ]; then
    echo "error: host table $TBL too small for $NFUNC functions at base $TABLE_BASE." >&2
    echo "       Raise -Wl,--table-base in corerun/CMakeLists.txt to at least $((TABLE_BASE + NFUNC + 1))." >&2
    exit 1
fi

# 3. Generate the shim supplying the two globals wasm-ld cannot emit for a non-PIC main module.
cat > "$D/shim.wat" <<WAT
(module
  (global (export "__memory_base") i32 (i32.const $ADDR))
  (global (export "__table_base")  i32 (i32.const $TABLE_BASE)))
WAT
wat2wasm "$D/shim.wat" -o "$D/shim.wasm"

# 4. Merge host + shim + composite. Host and shim share the name the composite imports from, so
#    both contribute exports to it. --enable-gc is needed only for the INTERMEDIATE: merging
#    internalizes the imported globals, and global.get of a *defined* global is a constant
#    expression only under the GC proposal. The fold in step 5 removes that requirement again.
wasm-merge -g --all-features --enable-gc \
    "$MAIN" webcil "$D/shim.wasm" webcil "$COMP" composite -o "$D/merged.wasm" 2>&1 | tail -1

# 5. Fold global.get -> i32.const so the result is MVP-valid. Without this, wasmtime rejects the
#    module unless the embedder enables GC. Costs ~3.7% code size: the pass also propagates
#    globals into function bodies, and a multi-byte i32.const is larger than a 2-byte global.get.
wasm-opt "$D/merged.wasm" --all-features --simplify-globals -o "$D/final.wasm"

# 6. Swap the merged core module back into the corerun component.
python3 - "$CORERUN" "$D/final.wasm" "$D/corerun-composite.wasm" <<'PY'
import sys
cp, mp, op = sys.argv[1:4]
merged = open(mp, 'rb').read(); data = open(cp, 'rb').read()
def wl(v):
    o = bytearray()
    while True:
        b = v & 0x7f; v >>= 7
        if v: o.append(b | 0x80)
        else: o.append(b); break
    return bytes(o)
def rl(d, p):
    r = s = 0
    while True:
        b = d[p]; p += 1; r |= (b & 0x7f) << s; s += 7
        if not (b & 0x80): break
    return r, p
out = bytearray(data[:8]); pos = 8; sw = False
while pos < len(data):
    sid = data[pos]; ss = pos; pos += 1
    size, pos = rl(data, pos)
    if sid == 1 and not sw:
        out.append(1); out += wl(len(merged)); out += merged; sw = True
    else:
        out += data[ss:pos+size]
    pos += size
open(op, 'wb').write(out)
PY

wasm-tools validate --features all "$D/corerun-composite.wasm" >/dev/null 2>&1 && echo "VALID" || echo "INVALID"
echo "OUT: $D/corerun-composite.wasm"
