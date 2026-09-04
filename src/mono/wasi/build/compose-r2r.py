#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Compose a wasm R2R image into a WASI host component.

The composite crossgen2 emits is SELF-INSTALLING: the webcil payload is an ACTIVE data
segment at (global.get __memory_base) and the R2R function table is an ACTIVE element
segment at (global.get __table_base). The engine installs both at instantiation.

corerun supplies five of the composite's seven imports directly (memory, __stack_pointer,
__indirect_function_table, __coreclr_wasm_rtlrestorecontext_tag, __async_continuation).
The remaining two are the base globals, which wasm-ld only creates in PIC mode -- so a
generated shim module supplies them instead.

Requires: wasm-tools and binaryen (wasm-merge, wasm-opt). The shim is assembled here
and the host's reservation parameters are read directly from its module.

  COMP=<composite.wasm> CORERUN=<host component> OUTDIR=<output directory> python3 compose-r2r.py
"""

import os
import shutil
import subprocess
import sys
from pathlib import Path

# ---------------------------------------------------------------- wasm reading

SEC_CUSTOM, SEC_TYPE, SEC_IMPORT, SEC_FUNCTION = 0, 1, 2, 3
SEC_GLOBAL, SEC_EXPORT, SEC_START = 6, 7, 8
SEC_ELEMENT, SEC_CODE, SEC_DATA = 9, 10, 11

EXTERNKIND_FUNC = 0


class WasmError(Exception):
    pass


def _uleb(data, pos):
    result = shift = 0
    while pos < len(data):
        byte = data[pos]
        pos += 1
        result |= (byte & 0x7F) << shift
        shift += 7
        if not byte & 0x80:
            return result, pos
        if shift >= 64:
            raise WasmError("invalid overlong ULEB128 value")
    raise WasmError("truncated ULEB128 value")


def _sleb(data, pos):
    result = shift = 0
    while pos < len(data):
        byte = data[pos]
        pos += 1
        result |= (byte & 0x7F) << shift
        shift += 7
        if not byte & 0x80:
            if shift < 64 and byte & 0x40:
                result -= 1 << shift
            return result, pos
        if shift >= 64:
            raise WasmError("invalid overlong SLEB128 value")
    raise WasmError("truncated SLEB128 value")


def _emit_uleb(value):
    out = bytearray()
    while True:
        byte = value & 0x7F
        value >>= 7
        if value:
            out.append(byte | 0x80)
        else:
            out.append(byte)
            return bytes(out)


def _emit_sleb(value):
    out = bytearray()
    while True:
        byte = value & 0x7F
        value >>= 7
        done = (value == 0 and not byte & 0x40) or (value == -1 and byte & 0x40)
        out.append(byte if done else byte | 0x80)
        if done:
            return bytes(out)


class WasmModule:
    """Minimal core-module reader: just enough to answer the splice's questions."""

    def __init__(self, path):
        self.path = Path(path)
        self.data = self.path.read_bytes()
        if len(self.data) < 8 or self.data[:4] != b"\0asm":
            raise WasmError(f"{path} is not a wasm core module (bad magic). "
                            "If it is a component, unbundle it first.")
        self.sections = []  # (id, payload_start, payload_end)
        pos = 8
        while pos < len(self.data):
            sec_id = self.data[pos]
            size, pos = _uleb(self.data, pos + 1)
            if size > len(self.data) - pos:
                raise WasmError(f"section {sec_id} in {self.path.name} extends beyond the file")
            self.sections.append((sec_id, pos, pos + size))
            pos += size

    def _section(self, sec_id):
        for sid, start, end in self.sections:
            if sid == sec_id:
                return start, end
        return None

    def _vec(self, sec_id):
        span = self._section(sec_id)
        if span is None:
            return 0, None
        count, pos = _uleb(self.data, span[0])
        return count, pos

    def func_import_count(self):
        count, pos = self._vec(SEC_IMPORT)
        if pos is None:
            return 0
        total = 0
        for _ in range(count):
            for _ in range(2):  # module, name
                length, pos = _uleb(self.data, pos)
                pos += length
            kind = self.data[pos]
            pos += 1
            if kind == EXTERNKIND_FUNC:
                total += 1
                _, pos = _uleb(self.data, pos)
            elif kind == 1:  # table
                pos += 1
                limits = self.data[pos]
                pos += 1
                _, pos = _uleb(self.data, pos)
                if limits:
                    _, pos = _uleb(self.data, pos)
            elif kind == 2:  # memory
                limits = self.data[pos]
                pos += 1
                _, pos = _uleb(self.data, pos)
                if limits:
                    _, pos = _uleb(self.data, pos)
            elif kind == 3:  # global
                pos += 2
            elif kind == 4:  # tag
                pos += 1
                _, pos = _uleb(self.data, pos)
            else:
                raise WasmError(f"unknown import kind {kind} in {self.path.name}")
        return total

    def defined_func_count(self):
        count, _ = self._vec(SEC_FUNCTION)
        return count

    def exports(self):
        """name -> (kind, index)"""
        count, pos = self._vec(SEC_EXPORT)
        found = {}
        if pos is None:
            return found
        for _ in range(count):
            length, pos = _uleb(self.data, pos)
            name = self.data[pos:pos + length].decode("utf-8", "replace")
            pos += length
            kind = self.data[pos]
            pos += 1
            index, pos = _uleb(self.data, pos)
            found[name] = (kind, index)
        return found

    def _code_body(self, defined_index):
        count, pos = self._vec(SEC_CODE)
        if pos is None or defined_index >= count:
            return None
        for i in range(count):
            size, body = _uleb(self.data, pos)
            if i == defined_index:
                return self.data[body:body + size]
            pos = body + size
        return None

    def const_i32_export(self, name):
        """Value of an exported function whose whole body is `i32.const N`.

        The host publishes each splice parameter this way so it decodes statically, with no
        instantiation and no copy of the value living in this script.
        """
        entry = self.exports().get(name)
        if entry is None or entry[0] != EXTERNKIND_FUNC:
            return None
        defined = entry[1] - self.func_import_count()
        if defined < 0:
            return None  # an imported function has no body to read
        body = self._code_body(defined)
        if not body:
            return None
        local_decls, pos = _uleb(body, 0)
        for _ in range(local_decls):
            _, pos = _uleb(body, pos)
            pos += 1
        if body[pos] != 0x41:  # i32.const
            return None
        value, pos = _sleb(body, pos + 1)
        return value if body[pos] == 0x0B else None

    def _const_offset(self, pos):
        """Decode a constant init_expr. Returns (value_or_None, kind, next_pos)."""
        op = self.data[pos]
        if op == 0x41:  # i32.const
            value, pos = _sleb(self.data, pos + 1)
            return value, "i32.const", pos + 1  # skip 0x0B
        if op == 0x23:  # global.get
            index, pos = _uleb(self.data, pos + 1)
            return index, "global.get", pos + 1
        raise WasmError(f"unsupported init_expr opcode 0x{op:02x} in {self.path.name}")

    def element_segments(self):
        count, pos = self._vec(SEC_ELEMENT)
        out = []
        if pos is None:
            return out
        for _ in range(count):
            flags, pos = _uleb(self.data, pos)
            seg = {"flags": flags, "active": flags in (0, 2, 4, 6), "offset": None,
                   "offset_kind": None, "count": 0}
            if flags in (2, 6):
                _, pos = _uleb(self.data, pos)  # table index
            if seg["active"]:
                value, kind, pos = self._const_offset(pos)
                seg["offset"], seg["offset_kind"] = value, kind
            if flags in (1, 2, 5, 6):
                pos += 1  # elemkind / reftype
            elif flags in (3, 7):
                pos += 1
            n, pos = _uleb(self.data, pos)
            seg["count"] = n
            for _ in range(n):
                _, pos = _uleb(self.data, pos)
            out.append(seg)
        return out

    def data_segments(self):
        count, pos = self._vec(SEC_DATA)
        out = []
        if pos is None:
            return out
        for _ in range(count):
            flags, pos = _uleb(self.data, pos)
            seg = {"flags": flags, "active": flags in (0, 2), "offset": None,
                   "offset_kind": None, "size": 0}
            if flags == 2:
                _, pos = _uleb(self.data, pos)  # memory index
            if seg["active"]:
                value, kind, pos = self._const_offset(pos)
                seg["offset"], seg["offset_kind"] = value, kind
            size, pos = _uleb(self.data, pos)
            seg["size"] = size
            pos += size
            out.append(seg)
        return out


def make_shim(memory_base, table_base, patch_header, payload_size):
    """Assemble the module supplying what wasm-ld cannot emit for a non-PIC main module.

    Always exports the two base globals. When `patch_header` is set it additionally imports
    the composite's `patchWebcilHeader` and calls it from a start function, so the composite
    fills in its own header's TableBase field using the `__table_base` this shim defines.
    That is strictly better than the host writing that field: the composite owns both the
    offset and the value, so the two cannot disagree about the format.

    The import lives here rather than in corerun deliberately. corerun is a WASI *component*,
    and an arbitrary core import is not expressible in a WIT world -- `wasm-component-ld`
    rejects it with "failed to decode world from module". The shim is merged and never
    componentized, so the import is resolved by the merge and the component wrapper never
    sees it.

    Hand-assembled rather than written as WAT so wabt is not a prerequisite; the caller
    validates the result before it reaches the merge.
    """
    def section(sec_id, payload):
        return bytes([sec_id]) + _emit_uleb(len(payload)) + payload

    def name(text):
        raw = text.encode("utf-8")
        return _emit_uleb(len(raw)) + raw

    out = bytearray(b"\0asm\x01\x00\x00\x00")

    if patch_header:
        # (i32,i32)->()  for patchWebcilHeader, and ()->() for the start function.
        types = _emit_uleb(2) + b"\x60\x02\x7f\x7f\x00" + b"\x60\x00\x00"
        out += section(SEC_TYPE, types)
        out += section(SEC_IMPORT,
                       _emit_uleb(1) + name("composite") + name("patchWebcilHeader")
                       + b"\x00" + _emit_uleb(0))
        out += section(SEC_FUNCTION, _emit_uleb(1) + _emit_uleb(1))

    def global_entry(value):
        return b"\x7f\x00\x41" + _emit_sleb(value) + b"\x0b"  # i32, immutable, i32.const

    out += section(SEC_GLOBAL,
                   _emit_uleb(2) + global_entry(memory_base) + global_entry(table_base))
    out += section(SEC_EXPORT,
                   _emit_uleb(2)
                   + name("__memory_base") + b"\x03" + _emit_uleb(0)
                   + name("__table_base") + b"\x03" + _emit_uleb(1))

    if patch_header:
        # Function 0 is the import, so the start function is index 1.
        out += section(SEC_START, _emit_uleb(1))
        # Let the composite validate the complete payload extent rather than duplicating its
        # current header size in the host-side shim.
        body = (_emit_uleb(0)                              # no locals
                + b"\x41" + _emit_sleb(memory_base)        # i32.const dest
                + b"\x41" + _emit_sleb(payload_size)        # i32.const length
                + b"\x10" + _emit_uleb(0)                  # call 0
                + b"\x0b")                                 # end
        out += section(SEC_CODE, _emit_uleb(1) + _emit_uleb(len(body)) + body)

    return bytes(out)


def swap_core_module(component_path, module_path, out_path):
    """Replace the first core-module section of a component with the merged module."""
    component = Path(component_path).read_bytes()
    merged = Path(module_path).read_bytes()
    out = bytearray(component[:8])
    pos, swapped = 8, False
    while pos < len(component):
        sec_id = component[pos]
        start = pos
        size, pos = _uleb(component, pos + 1)
        if sec_id == 1 and not swapped:  # core module
            out += bytes([1]) + _emit_uleb(len(merged)) + merged
            swapped = True
        else:
            out += component[start:pos + size]
        pos += size
    if not swapped:
        raise WasmError(f"{component_path} has no core-module section to replace")
    Path(out_path).write_bytes(bytes(out))


# ---------------------------------------------------------------- external tools

def tool(name):
    found = shutil.which(name)
    if found is None:
        raise WasmError(f"required tool '{name}' is not on PATH. "
                        "Install wasm-tools and binaryen; see docs/workflow/building/coreclr/wasi-r2r.md.")
    return found


def run(args, capture=False):
    result = subprocess.run(args, check=False, text=True,
                            stdout=subprocess.PIPE if capture else None,
                            stderr=subprocess.STDOUT if capture else None)
    if result.returncode != 0:
        detail = f"\n{result.stdout.strip()}" if capture and result.stdout else ""
        raise WasmError(f"{Path(args[0]).name} failed (exit {result.returncode}){detail}")
    return result.stdout if capture else ""


# ---------------------------------------------------------------- pipeline

def composite_requirements(composite):
    n_funcs = composite.defined_func_count()

    # The payload is the composite's ONE active data segment. Select it by meaning: the
    # 9-byte webcilCount segment is passive, so index-based selection would be a positional
    # assumption that breaks silently if crossgen2 ever reorders segments.
    payloads = [s for s in composite.data_segments() if s["active"]]
    if len(payloads) != 1:
        raise WasmError(
            f"expected exactly one active data segment in {composite.path.name} "
            f"(the webcil payload), found {len(payloads)}. The composite layout changed; "
            "this check would otherwise pick the wrong segment.")

    return n_funcs, payloads[0]["size"]


def main():
    if len(sys.argv) == 3 and sys.argv[1] in ("--describe", "--function-count", "--payload-size"):
        composite = WasmModule(sys.argv[2])
        n_funcs, payload = composite_requirements(composite)
        if sys.argv[1] == "--describe":
            print(f"{n_funcs},{payload}")
        else:
            print(n_funcs if sys.argv[1] == "--function-count" else payload)
        return

    if len(sys.argv) != 1:
        raise WasmError(
            "usage: compose-r2r.py [--describe|--function-count|--payload-size] "
            "<composite.wasm>")

    missing = [name for name in ("COMP", "CORERUN", "OUTDIR") if not os.environ.get(name)]
    if missing:
        raise WasmError(f"set the required environment variables: {', '.join(missing)}")
    comp = Path(os.environ["COMP"])
    corerun = Path(os.environ["CORERUN"])
    outdir = Path(os.environ["OUTDIR"])

    for label, path in (("composite", comp), ("corerun", corerun)):
        if not path.is_file():
            raise WasmError(f"{label} not found at {path}")
    outdir.mkdir(parents=True, exist_ok=True)

    wasm_tools = tool("wasm-tools")

    # 1. Unbundle the corerun component -> core module. Mandatory: corerun is a WASI
    #    component, and a component is not a core module.
    run([wasm_tools, "component", "unbundle", str(corerun),
         "--module-dir", str(outdir), "-o", os.devnull], capture=True)
    modules = sorted(outdir.glob("*module0*.wasm"))
    if not modules:
        raise WasmError(f"unbundling {corerun.name} produced no *module0*.wasm in {outdir}")
    host = WasmModule(modules[0])

    # 2. Read the R2R parameters out of the LINKED host. The host owns these values; this
    #    script must not carry its own copy, or a rebuild with different settings silently
    #    produces a mismatched image.
    params = {}
    for key, export in (("image_base", "wasi_r2r_image_base"),
                        ("cap", "wasi_r2r_image_cap"),
                        ("table_base", "wasi_r2r_table_base")):
        value = host.const_i32_export(export)
        if value is None:
            raise WasmError(
                f"the host does not export {export} as an R2R parameter.\n"
                "       Link it with CORERUN_WASI_COMPOSITE_R2R=ON (corerun) or\n"
                "       WasiEnableCompositeR2R=true (apps); without those flags the probe\n"
                "       is present but can never be satisfied.")
        params[key] = value

    composite = WasmModule(comp)

    # The composite installs at table_base and must end before the host's OWN element
    # segment begins -- not merely inside the table. Both are ACTIVE in the merged module,
    # so an overlap silently overwrites the host's function pointers rather than failing.
    host_active = [s for s in host.element_segments()
                   if s["active"] and s["offset_kind"] == "i32.const"]
    if not host_active:
        raise WasmError("the host has no active element segment, so it reserves no table "
                        "slots for the composite.")
    reserved = min(s["offset"] for s in host_active)

    n_funcs, payload = composite_requirements(composite)

    print(f"SHIM: imageBase={params['image_base']} tableBase={params['table_base']} "
          f"reservedSlots={reserved} compositeFuncs={n_funcs} payload={payload} "
          f"cap={params['cap']}")

    if params["table_base"] + n_funcs > reserved:
        need = params["table_base"] + n_funcs
        raise WasmError(
            f"composite needs slots {params['table_base']}..{need - 1} but the host's own\n"
            f"       functions begin at {reserved}. They would overlap and silently corrupt\n"
            f"       dispatch. Raise the table base to at least {need}:\n"
            f"         corerun -DCORERUN_WASI_R2R_TABLE_BASE={need}\n"
            f"         apps    -p:WasiCompositeR2RTableBase={need}")

    # The engine installs the payload into the host's staging buffer BEFORE any host code
    # runs, so the host's own cap test cannot protect that buffer. This is the only place
    # it is enforceable.
    if payload > params["cap"]:
        raise WasmError(
            f"composite payload {payload} bytes exceeds the host's staging buffer "
            f"({params['cap']}).\n"
            "       Raise WASI_R2R_IMAGE_CAP in corerun/wasi_r2r_probe.hpp and rebuild.")

    # 3. Generate the shim supplying the two globals wasm-ld cannot emit for a non-PIC main
    #    module, and validate it before it reaches the merge.
    #
    #    If the composite exports patchWebcilHeader (the self-installing shape in
    #    docs/design/mono/webcil.md), have the shim call it from a start function so the
    #    composite writes its own TableBase field. Older composites predate that export, so
    #    detect rather than assume -- importing a function the composite does not export makes
    #    the merge leave it unresolved and the host fails to instantiate. When it is absent the
    #    host's own fallback in wasi_r2r_probe.hpp writes the field instead.
    patch_header = "patchWebcilHeader" in composite.exports()
    shim = outdir / "shim.wasm"
    shim.write_bytes(make_shim(
        params["image_base"], params["table_base"], patch_header, payload))
    run([wasm_tools, "validate", "--features", "all", str(shim)], capture=True)
    print(f"SHIM: tableBase written by "
          f"{'the composite (patchWebcilHeader)' if patch_header else 'the host (fallback)'}")

    # 4. Merge host + shim + composite. --enable-gc is needed only for the INTERMEDIATE:
    #    merging internalizes the imported globals, and global.get of a *defined* global is
    #    a constant expression only under the GC proposal. Step 5 removes that requirement.
    #    -g carries the name section through; see step 5 for why that matters.
    merged = outdir / "merged.wasm"
    run([tool("wasm-merge"), "-g", "--all-features", "--enable-gc",
         str(modules[0]), "webcil", str(shim), "webcil", str(comp), "composite",
         "-o", str(merged)], capture=True)

    # 5. Fold global.get -> i32.const so the result is MVP-valid; without this wasmtime
    #    rejects the module unless the embedder enables GC. Costs ~3.7% code size.
    #    -g preserves the name section. Since #132906 that section is the ONLY record of
    #    function names, and dropping it still validates and still runs -- the sole symptom
    #    is that every frame goes anonymous.
    final = outdir / "final.wasm"
    run([tool("wasm-opt"), str(merged), "--all-features", "-g", "--simplify-globals",
         "-o", str(final)], capture=True)

    # 6. Swap the merged core module back into the corerun component.
    out = outdir / "corerun-composite.wasm"
    swap_core_module(corerun, final, out)

    run([wasm_tools, "validate", "--features", "all", str(out)], capture=True)
    print("VALID")
    print(f"OUT: {out}")


if __name__ == "__main__":
    try:
        main()
    except WasmError as error:
        print(f"error: {error}", file=sys.stderr)
        sys.exit(1)
    except IndexError:
        print("error: malformed wasm input ended unexpectedly", file=sys.stderr)
        sys.exit(1)
