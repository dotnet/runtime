#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Hash the machine code of the stock write barrier and allocation helpers in libcoreclr.so (Linux x64).

usage: helperhash.py <libcoreclr.so> [<libcoreclr.so.dbg with symbols>] [--dump <dir>]
Each helper is disassembled with objdump. Relative displacements and absolute call or branch targets are replaced
by symbol names, so a relink that only moves code gives the same hash. Helpers that the build does not define are
printed as MISSING and are not part of the total.
"""
import bisect
import hashlib
import os
import re
import subprocess
import sys

lib = sys.argv[1]
symlib = sys.argv[2] if len(sys.argv) > 2 and not sys.argv[2].startswith('--') else lib
dump = sys.argv[sys.argv.index('--dump') + 1] if '--dump' in sys.argv else None
nm = subprocess.run(['nm', '-S', '--defined-only', symlib], capture_output=True, text=True).stdout
syms = {}
for line in nm.splitlines():
    p = line.split()
    if len(p) >= 3:
        addr = int(p[0], 16)
        size = int(p[1], 16) if len(p) == 4 else 0
        syms[p[-1]] = (addr, size)

addrs = sorted((a, n) for n, (a, s) in syms.items())
akeys = [a for a, n in addrs]


def symof(hexaddr):
    a = int(hexaddr, 16)
    i = bisect.bisect_right(akeys, a) - 1
    return '<%s>' % addrs[i][1] if i >= 0 and a - akeys[i] < 0x2000 else '<?>'


# (name, end symbol or None)
targets = [
    ('JIT_WriteBarrier_PreGrow64', 'JIT_WriteBarrier_PreGrow64_End'),
    ('JIT_WriteBarrier_PostGrow64', 'JIT_WriteBarrier_PostGrow64_End'),
    ('JIT_WriteBarrier_SVR64', 'JIT_WriteBarrier_SVR64_End'),
    ('JIT_WriteBarrier_Byte_Region64', 'JIT_WriteBarrier_Byte_Region64_End'),
    ('JIT_WriteBarrier_Bit_Region64', 'JIT_WriteBarrier_Bit_Region64_End'),
    ('JIT_WriteBarrier_WriteWatch_PreGrow64', 'JIT_WriteBarrier_WriteWatch_PreGrow64_End'),
    ('JIT_WriteBarrier_WriteWatch_PostGrow64', 'JIT_WriteBarrier_WriteWatch_PostGrow64_End'),
    ('JIT_WriteBarrier_WriteWatch_SVR64', 'JIT_WriteBarrier_WriteWatch_SVR64_End'),
    ('JIT_WriteBarrier_WriteWatch_Byte_Region64', 'JIT_WriteBarrier_WriteWatch_Byte_Region64_End'),
    ('JIT_WriteBarrier_WriteWatch_Bit_Region64', 'JIT_WriteBarrier_WriteWatch_Bit_Region64_End'),
    ('JIT_WriteBarrier', 'JIT_WriteBarrier_End'),
    ('JIT_CheckedWriteBarrier', 'JIT_CheckedWriteBarrier_End'),
    ('JIT_ByRefWriteBarrier', 'JIT_ByRefWriteBarrier_End'),
    ('RhpNewFast', None), ('RhpNewArrayFast', None), ('RhpNewPtrArrayFast', None), ('RhNewString', None),
    ('RhpNewFastAlign8', None), ('JIT_NewS_MP_FastPortable', None), ('JIT_NewArr1VC_MP_FastPortable', None),
    ('JIT_NewArr1OBJ_MP_FastPortable', None), ('JIT_Patchpoint', None),
]


def normalize(text):
    out = []
    for line in text.splitlines():
        m = re.match(r'\s*[0-9a-f]+:\s+(.*)$', line)
        if not m:
            continue
        ins = m.group(1)
        ins = re.sub(r'\[rip\+0x[0-9a-f]+\]', '[rip+X]', ins)
        ins = re.sub(r'#\s*([0-9a-f]+)\s*', lambda k: '# ' + symof(k.group(1)) + ' ', ins)
        ins = re.sub(r'^(call|jmp|j[a-z]+)\s+([0-9a-f]+)\s*', lambda k: k.group(1) + ' ' + symof(k.group(2)) + ' ', ins)
        ins = re.sub(r'<([^>+]+)\+0x[0-9a-f]+>', r'<\1+off>', ins)
        out.append(ins.strip())
    return '\n'.join(out)


total = hashlib.sha256()
for name, end in targets:
    if name not in syms:
        print('%-45s MISSING' % name)
        continue
    start, size = syms[name]
    if end and end in syms:
        stop = syms[end][0]
    elif size:
        stop = start + size
    else:
        print('%-45s no size' % name)
        continue
    dis = subprocess.run(['objdump', '-d', '--no-show-raw-insn', '-M', 'intel', '--start-address=%d' % start,
                          '--stop-address=%d' % stop, lib], capture_output=True, text=True).stdout
    text = normalize(dis)
    digest = hashlib.sha256(text.encode()).hexdigest()[:16]
    total.update((name + digest).encode())
    print('%-45s bytes=%-5d insns=%-4d %s' % (name, stop - start, text.count('\n') + 1, digest))
    if dump:
        os.makedirs(dump, exist_ok=True)
        open(os.path.join(dump, name + '.txt'), 'w').write(text)
print('TOTAL', total.hexdigest()[:32])
