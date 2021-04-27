##
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
##

#!/usr/bin/env python3
#
# This is a tool to generate the bitonic sorter code that is used for small arrays.
#
# usage: bitonic_gen.py [-h] [--vector-isa VECTOR_ISA [VECTOR_ISA ...]]
#                     [--break-inline BREAK_INLINE] [--output-dir OUTPUT_DIR]
#
# the files in src/coreclr/gc/vxsort/smallsort that are currently checked in can be generated with:
#   python bitonic_gen.py --output-dir c:\temp --vector-isa AVX2 AVX512  --break-inline 4
#
import argparse
import os
from enum import Enum

from avx2 import AVX2BitonicISA
from avx512 import AVX512BitonicISA
from bitonic_isa import BitonicISA

BitonicISA.register(AVX2BitonicISA)
BitonicISA.register(AVX512BitonicISA)


def get_generator_supported_types(vector_isa):
    if isinstance(vector_isa, str):
        vector_isa = VectorISA[vector_isa]
    if vector_isa == VectorISA.AVX2:
        return AVX2BitonicISA.supported_types()
    elif vector_isa == VectorISA.AVX512:
        return AVX512BitonicISA.supported_types()
    else:
        raise Exception(f"Non-supported vector machine-type: {vector_isa}")


def get_generator(vector_isa, type):
    if isinstance(vector_isa, str):
        vector_isa = VectorISA[vector_isa]
    if vector_isa == VectorISA.AVX2:
        return AVX2BitonicISA(type)
    elif vector_isa == VectorISA.AVX512:
        return AVX512BitonicISA(type)
    else:
        raise Exception(f"Non-supported vector machine-type: {vector_isa}")


def generate_per_type(f_header, f_src, type, vector_isa, break_inline):
    g = get_generator(vector_isa, type)
    g.generate_prologue(f_header)
    g.generate_1v_sorters(f_header, ascending=True)
    g.generate_1v_sorters(f_header, ascending=False)
    for width in range(2, g.max_bitonic_sort_vectors() + 1):

        # Allow breaking the inline chain once in a while (configurable)
        if break_inline == 0 or width % break_inline != 0:
            inline = True
        else:
            inline = False
        g.generate_compounded_sorter(f_header, width, ascending=True, inline=inline)
        g.generate_compounded_sorter(f_header, width, ascending=False, inline=inline)
        if width <= g.largest_merge_variant_needed():
            g.generate_compounded_merger(f_header, width, ascending=True, inline=inline)
            g.generate_compounded_merger(f_header, width, ascending=False, inline=inline)

    #g.generate_entry_points_old(f_header)
    g.generate_entry_points(f_header)
    g.generate_master_entry_point(f_header, f_src)
    g.generate_epilogue(f_header)


class Language(Enum):
    csharp = 'csharp'
    cpp = 'cpp'
    rust = 'rust'

    def __str__(self):
        return self.value


class VectorISA(Enum):
    AVX2 = 'AVX2'
    AVX512 = 'AVX512'
    SVE = 'SVE'

    def __str__(self):
        return self.value

def generate_all_types():
    parser = argparse.ArgumentParser()
    #parser.add_argument("--language", type=Language, choices=list(Language),
    #                    help="select output language: csharp/cpp/rust")
    parser.add_argument("--vector-isa",
                        nargs='+',
                        default='all',
                        help='list of vector ISA to generate',
                        choices=list(VectorISA).append("all"))
    parser.add_argument("--break-inline", type=int, default=0, help="break inlining every N levels")

    parser.add_argument("--output-dir", type=str,
                        help="output directory")

    opts = parser.parse_args()

    if 'all' in opts.vector_isa:
        opts.vector_isa = list(VectorISA)

    for isa in opts.vector_isa:
        for t in get_generator_supported_types(isa):
            filename = f"bitonic_sort.{isa}.{t}.generated"
            print(f"Generating {filename}.{{h,.cpp}}")
            h_filename = os.path.join(opts.output_dir, filename + ".h")
            h_src = os.path.join(opts.output_dir, filename + ".cpp")
            with open(h_filename, "w") as f_header, open(h_src, "w") as f_source:
                generate_per_type(f_header, f_source, t, isa, opts.break_inline)

if __name__ == '__main__':
    generate_all_types()
