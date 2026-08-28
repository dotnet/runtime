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

# imageBase, tableBase and the buffer cap are all read from the linked host below -- this script
# deliberately holds no copy of any of them. The host is the single source of truth; anything it does
# not export is a build-time error rather than a silently mismatched image.

[ -f "$COMP" ]    || { echo "error: composite not found at '$COMP'" >&2; exit 1; }
[ -f "$CORERUN" ] || { echo "error: corerun not found at '$CORERUN'" >&2; exit 1; }

rm -rf "$D"; mkdir -p "$D"

# 1. Unbundle the corerun component -> core module. Mandatory: corerun is a WASI component and
#    wasm-objdump rejects components outright ("wasm components are not yet supported").
wasm-tools component unbundle "$CORERUN" --module-dir "$D" -o /dev/null >/dev/null 2>&1
MAIN=$(ls "$D"/*module0*.wasm | head -1)

# 2. Read the R2R parameters out of the LINKED host. Each is exported as a function whose body is a
#    single i32.const, so they decode statically with no instantiation. The host owns these values;
#    this script must not carry its own copy of any of them, or a rebuild with different settings
#    silently produces a mismatched image.
#    NOTE: use sed, not awk. The awk form in the original pipeline silently yields an EMPTY string
#    under BSD awk (the macOS default), which would feed an empty value downstream.
read_i32_export() { # $1=module $2=export name -> prints the i32.const in its body
    local _idx
    _idx=$(wasm-objdump -j Export -x "$1" | grep -i "$2" | grep -oE 'func\[[0-9]+\]' | grep -oE '[0-9]+' || true)
    case "$_idx" in ''|*[!0-9]*) return 1;; esac
    wasm-objdump -d "$1" | grep -A1 "func\[$_idx\] <$2>" \
        | grep 'i32\.const' | sed -E 's/.*i32\.const +([0-9]+).*/\1/' || true
}

ADDR=$(read_i32_export "$MAIN" wasi_r2r_image_base || true)
CAP=$(read_i32_export "$MAIN" wasi_r2r_image_cap || true)
TABLE_BASE=$(read_i32_export "$MAIN" wasi_r2r_table_base || true)
for _v in ADDR:"$ADDR" CAP:"$CAP" TABLE_BASE:"$TABLE_BASE"; do
    case "${_v#*:}" in ''|*[!0-9]*)
        echo "error: the host does not export ${_v%%:*} as an R2R parameter." >&2
        echo "       Link it with CORERUN_WASI_COMPOSITE_R2R=ON (corerun) or WasiEnableCompositeR2R=true" >&2
        echo "       (apps); without those flags the probe is present but can never be satisfied." >&2
        exit 1;;
    esac
done

# The composite installs at TABLE_BASE and must end before the host's OWN element segment begins --
# not merely inside the table. Both are ACTIVE segments in the merged module, so an overlap silently
# overwrites the host's function pointers rather than failing to link. Derive the boundary from the
# artifact rather than from --table-base, so it cannot drift from what was actually linked.
RESERVED=$(wasm-objdump -x "$MAIN" | grep -E "^ - segment\[0\] flags=0 table=0" | sed -E 's/.*init i32=([0-9]+).*/\1/' | head -1 || true)
case "$RESERVED" in ''|*[!0-9]*) RESERVED=0;; esac

NFUNC=$(wasm-objdump -h "$COMP" | grep -iE "^ Function " | grep -oE "count: [0-9]+" | grep -oE "[0-9]+")
echo "SHIM: imageBase=$ADDR tableBase=$TABLE_BASE reservedSlots=$RESERVED compositeFuncs=$NFUNC cap=$CAP"

if [ "$RESERVED" -eq 0 ]; then
    echo "error: the host reserves no table slots (its element segment starts at 0 or was not found)." >&2
    exit 1
fi
if [ "$((TABLE_BASE + NFUNC))" -gt "$RESERVED" ]; then
    echo "error: composite needs slots $TABLE_BASE..$((TABLE_BASE + NFUNC - 1)) but the host's own" >&2
    echo "       functions begin at $RESERVED. They would overlap and silently corrupt dispatch." >&2
    echo "       Raise the table base to at least $((TABLE_BASE + NFUNC)):" >&2
    echo "         corerun -DCORERUN_WASI_R2R_TABLE_BASE=$((TABLE_BASE + NFUNC))" >&2
    echo "         apps    -p:WasiCompositeR2RTableBase=$((TABLE_BASE + NFUNC))" >&2
    exit 1
fi

# The payload is installed by the engine directly into the host's staging buffer BEFORE any host code
# runs. The host's own cap test therefore cannot protect that buffer -- by the time it executes, an
# over-cap payload has already overwritten whatever follows. This is the only place it is enforceable.
PAYLOAD=$(wasm-objdump -x "$COMP" | grep -iE "^ - segment\[1\]" | grep -oE "size=[0-9]+" | grep -oE "[0-9]+" | head -1 || true)
if [ -n "$PAYLOAD" ] && [ "$PAYLOAD" -gt "$CAP" ]; then
    echo "error: composite payload $PAYLOAD bytes exceeds the host's staging buffer ($CAP)." >&2
    echo "       Raise WASI_R2R_IMAGE_CAP in corerun/wasi_r2r_probe.hpp and rebuild the host." >&2
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
#    -g preserves the name section; without it wasm-opt strips the names wasm-merge just kept, and
#    every function in the spliced host becomes anonymous to a debugger.
wasm-opt "$D/merged.wasm" --all-features -g --simplify-globals -o "$D/final.wasm"

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
