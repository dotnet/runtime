#!/usr/bin/env python3
import argparse
from datetime import datetime
from enum import Enum

max_bitonic_sort_verctors = 16


def next_power_of_2(v):
    v = v - 1
    v |= v >> 1
    v |= v >> 2
    v |= v >> 4
    v |= v >> 8
    v |= v >> 16
    v = v + 1
    return int(v)


largest_merge_variant_needed = next_power_of_2(max_bitonic_sort_verctors) / 2;

## types to function suffix
bitonic_type_map = {
    "int32_t": "__m256i",
    "uint32_t": "__m256i",
    "float": "__m256",
    "int64_t": "__m256i",
    "uint64_t": "__m256i",
    "double": "__m256d",
}

bitonic_size_map = {
    "int32_t": 4,
    "uint32_t": 4,
    "float": 4,
    "int64_t": 8,
    "uint64_t": 8,
    "double": 8,
}

bitonic_types = bitonic_size_map.keys()


def i2d(v, t):
    if t == "double":
        return v
    elif t == "float":
        return f"s2d({v})"
    return f"i2d({v})"

def i2s(v, t):
    if t == "double":
        raise Exception("WTF")
    elif t == "float":
        return f"i2s({v})"
    return v


def d2i(v, t):
    if t == "double":
        return v
    elif t == "float":
        return f"d2s({v})"
    return f"d2i({v})"

def s2i(v, t):
    if t == "double":
        raise Exception("WTF")
    elif t == "float":
        return f"s2i({v})"
    return v



def generate_param_list(start, numParams):
    return str.join(", ", list(map(lambda p: f"d{p:02d}", range(start, start + numParams))))


def generate_param_def_list(numParams, nativeType):
    return str.join(", ", list(map(lambda p: f"{bitonic_type_map[nativeType]}& d{p:02d}", range(1, numParams + 1))))


def generate_shuffle_X1(v, t):
    if bitonic_size_map[t] == 4:
        return i2s(f"_mm256_shuffle_epi32({s2i(v, t)}, 0xB1)", t)
    elif bitonic_size_map[t] == 8:
        return d2i(f"_mm256_shuffle_pd({i2d(v, t)}, {i2d(v, t)}, 0x5)", t)


def generate_shuffle_X2(v, t):
    if bitonic_size_map[t] == 4:
        return i2s(f"_mm256_shuffle_epi32({s2i(v, t)}, 0x4E)", t)
    elif bitonic_size_map[t] == 8:
        return d2i(f"_mm256_permute4x64_pd({i2d(v, t)}, 0x4E)", t)


def generate_shuffle_XR(v, t):
    if bitonic_size_map[t] == 4:
        return i2s(f"_mm256_shuffle_epi32({s2i(v, t)}, 0x1B)", t)
    elif bitonic_size_map[t] == 8:
        return d2i(f"_mm256_permute4x64_pd({i2d(v, t)}, 0x1B)", t)


def generate_blend_B1(v1, v2, t, ascending):
    if bitonic_size_map[t] == 4:
        if ascending:
            return i2s(f"_mm256_blend_epi32({s2i(v1, t)}, {s2i(v2, t)}, 0xAA)", t)
        else:
            return  i2s(f"_mm256_blend_epi32({s2i(v2, t)}, {s2i(v1, t)}, 0xAA)", t)
    elif bitonic_size_map[t] == 8:
        if ascending:
            return d2i(f"_mm256_blend_pd({i2d(v1, t)}, {i2d(v2, t)}, 0xA)", t)
        else:
            return d2i(f"_mm256_blend_pd({i2d(v2, t)}, {i2d(v1, t)}, 0xA)", t)


def generate_blend_B2(v1, v2, t, ascending):
    if bitonic_size_map[t] == 4:
        if ascending:
            return i2s(f"_mm256_blend_epi32({s2i(v1, t)}, {s2i(v2, t)}, 0xCC)", t)
        else:
            return i2s(f"_mm256_blend_epi32({s2i(v2, t)}, {s2i(v1, t)}, 0xCC)", t)
    elif bitonic_size_map[t] == 8:
        if ascending:
            return d2i(f"_mm256_blend_pd({i2d(v1, t)}, {i2d(v2, t)}, 0xC)", t)
        else:
            return d2i(f"_mm256_blend_pd({i2d(v2, t)}, {i2d(v1, t)}, 0xC)", t)


def generate_blend_B4(v1, v2, t, ascending):
    if bitonic_size_map[t] == 4:
        if ascending:
            return i2s(f"_mm256_blend_epi32({s2i(v1, t)}, {s2i(v2, t)}, 0xF0)", t)
        else:
            return i2s(f"_mm256_blend_epi32({s2i(v2, t)}, {s2i(v1, t)}, 0xF0)", t)
    elif bitonic_size_map[t] == 8:
        raise Exception("WTF")


def generate_cross(v, t):
    if bitonic_size_map[t] == 4:
        return d2i(f"_mm256_permute4x64_pd({i2d(v, t)}, 0x4E)", t)
    elif bitonic_size_map[t] == 8:
        raise Exception("WTF")


def generate_reverse(v, t):
    if bitonic_size_map[t] == 4:
        v = f"_mm256_shuffle_epi32({s2i(v, t)}, 0x1B)"
        return d2i(f"_mm256_permute4x64_pd({i2d(v, 'int32_t')}, 0x4E)", t)
    elif bitonic_size_map[t] == 8:
        return d2i(f"_mm256_permute4x64_pd({i2d(v, t)}, 0x1B)", t)


def crappity_crap_crap(v1, v2, t):
    if t == "int64_t":
        return f"cmp = _mm256_cmpgt_epi64({v1}, {v2});"
    elif t == "uint64_t":
        return f"cmp = _mm256_cmpgt_epi64(_mm256_xor_si256(topBit, {v1}), _mm256_xor_si256(topBit, {v2}));"

    return ""


def generate_min(v1, v2, t):
    if t == "int32_t":
        return f"_mm256_min_epi32({v1}, {v2})"
    elif t == "uint32_t":
        return f"_mm256_min_epu32({v1}, {v2})"
    elif t == "float":
        return f"_mm256_min_ps({v1}, {v2})"
    elif t == "int64_t":
        return d2i(f"_mm256_blendv_pd({i2d(v1, t)}, {i2d(v2, t)}, i2d(cmp))", t)
    elif t == "uint64_t":
        return d2i(f"_mm256_blendv_pd({i2d(v1, t)}, {i2d(v2, t)}, i2d(cmp))", t)
    elif t == "double":
        return f"_mm256_min_pd({v1}, {v2})"


def generate_max(v1, v2, t):
    if t == "int32_t":
        return f"_mm256_max_epi32({v1}, {v2})"
    elif t == "uint32_t":
        return f"_mm256_max_epu32({v1}, {v2})"
    elif t == "float":
        return f"_mm256_max_ps({v1}, {v2})"
    elif t == "int64_t":
        return d2i(f"_mm256_blendv_pd({i2d(v2, t)}, {i2d(v1, t)}, i2d(cmp))", t)
    elif t == "uint64_t":
        return d2i(f"_mm256_blendv_pd({i2d(v2, t)}, {i2d(v1, t)}, i2d(cmp))", t)
    elif t == "double":
        return f"_mm256_max_pd({v1}, {v2})"


def generate_1v_basic_sorters(f, type, ascending):
    maybe_cmp = lambda: ", cmp" if (type == "int64_t" or type == "uint64_t") else ""
    maybe_topbit = lambda: f"\n        {bitonic_type_map[type]} topBit = _mm256_set1_epi64x(1LLU << 63);" if (
                type == "uint64_t") else ""

    suffix = "ascending" if ascending else "descending"

    s = f"""    static INLINE void sort_01v_{suffix}({generate_param_def_list(1, type)}) {{
        {bitonic_type_map[type]}  min, max, s{maybe_cmp()};{maybe_topbit()}

        s = {generate_shuffle_X1("d01", type)};
        {crappity_crap_crap("s", "d01", type)}
        min = {generate_min("s", "d01", type)};
        max = {generate_max("s", "d01", type)};
        d01 = {generate_blend_B1("min", "max", type, ascending)};

        s = {generate_shuffle_XR("d01", type)};
        {crappity_crap_crap("s", "d01", type)}
        min = {generate_min("s", "d01", type)};
        max = {generate_max("s", "d01", type)};
        d01 = {generate_blend_B2("min", "max", type, ascending)};

        s = {generate_shuffle_X1("d01", type)};
        {crappity_crap_crap("s", "d01", type)}
        min = {generate_min("s", "d01", type)};
        max = {generate_max("s", "d01", type)};
        d01 = {generate_blend_B1("min", "max", type, ascending)};"""

    print(s, file=f)

    if bitonic_size_map[type] == 4:
        s = f"""
        s = {generate_reverse("d01", type)};
        min = {generate_min("s", "d01", type)};
        max = {generate_max("s", "d01", type)};
        d01 = {generate_blend_B4("min", "max", type, ascending)};

        s = {generate_shuffle_X2("d01", type)};
        min = {generate_min("s", "d01", type)};
        max = {generate_max("s", "d01", type)};
        d01 = {generate_blend_B2("min", "max", type, ascending)};

        s = {generate_shuffle_X1("d01", type)};
        min = {generate_min("s", "d01", type)};
        max = {generate_max("s", "d01", type)};
        d01 = {generate_blend_B1("min", "max", type, ascending)};"""
        print(s, file=f)
    print("}", file=f)


def generate_1v_merge_sorters(f, type, ascending):
    maybe_cmp = lambda: ", cmp" if (type == "int64_t" or type == "uint64_t") else ""
    maybe_topbit = lambda: f"\n        {bitonic_type_map[type]} topBit = _mm256_set1_epi64x(1LLU << 63);" if (
                type == "uint64_t") else ""

    suffix = "ascending" if ascending else "descending"

    s = f"""    static INLINE void sort_01v_merge_{suffix}({generate_param_def_list(1, type)}) {{
        {bitonic_type_map[type]}  min, max, s{maybe_cmp()};{maybe_topbit()}"""
    print(s, file=f)

    if bitonic_size_map[type] == 4:
        s = f"""
        s = {generate_cross("d01", type)};
        min = {generate_min("s", "d01", type)};
        max = {generate_max("s", "d01", type)};
        d01 = {generate_blend_B4("min", "max", type, ascending)};"""
        print(s, file=f)

    s = f"""
        s = {generate_shuffle_X2("d01", type)};
        {crappity_crap_crap("s", "d01", type)}
        min = {generate_min("s", "d01", type)};
        max = {generate_max("s", "d01", type)};
        d01 = {generate_blend_B2("min", "max", type, ascending)};

        s = {generate_shuffle_X1("d01", type)};
        {crappity_crap_crap("s", "d01", type)}
        min = {generate_min("s", "d01", type)};
        max = {generate_max("s", "d01", type)};
        d01 = {generate_blend_B1("min", "max", type, ascending)};"""

    print(s, file=f)
    print("    }", file=f)


def generate_1v_sorters(f, type, ascending):
    generate_1v_basic_sorters(f, type, ascending)
    generate_1v_merge_sorters(f, type, ascending)


def generate_compounded_sorters(f, width, type, ascending):
    maybe_cmp = lambda: ", cmp" if (type == "int64_t" or type == "uint64_t") else ""
    maybe_topbit = lambda: f"\n        {bitonic_type_map[type]} topBit = _mm256_set1_epi64x(1LLU << 63);" if (
                type == "uint64_t") else ""

    w1 = int(next_power_of_2(width) / 2)
    w2 = int(width - w1)

    suffix = "ascending" if ascending else "descending"
    rev_suffix = "descending" if ascending else "ascending"

    s = f"""    static INLINE void sort_{width:02d}v_{suffix}({generate_param_def_list(width, type)}) {{
    {bitonic_type_map[type]}  tmp{maybe_cmp()};{maybe_topbit()}

    sort_{w1:02d}v_{suffix}({generate_param_list(1, w1)});
    sort_{w2:02d}v_{rev_suffix}({generate_param_list(w1 + 1, w2)});"""

    print(s, file=f)

    for r in range(w1 + 1, width + 1):
        x = w1 + 1 - (r - w1)
        s = f"""
    tmp = d{r:02d};
    {crappity_crap_crap(f"d{x:02d}", f"d{r:02d}", type)}
    d{r:02d} = {generate_max(f"d{x:02d}", f"d{r:02d}", type)};
    d{x:02d} = {generate_min(f"d{x:02d}", "tmp", type)};"""

        print(s, file=f)

    s = f"""
    sort_{w1:02d}v_merge_{suffix}({generate_param_list(1, w1)});
    sort_{w2:02d}v_merge_{suffix}({generate_param_list(w1 + 1, w2)});"""
    print(s, file=f)
    print("    }", file=f)


def generate_compounded_mergers(f, width, type, ascending):
    maybe_cmp = lambda: ", cmp" if (type == "int64_t" or type == "uint64_t") else ""
    maybe_topbit = lambda: f"\n        {bitonic_type_map[type]} topBit = _mm256_set1_epi64x(1LLU << 63);" if (
                type == "uint64_t") else ""

    w1 = int(next_power_of_2(width) / 2)
    w2 = int(width - w1)

    suffix = "ascending" if ascending else "descending"
    rev_suffix = "descending" if ascending else "ascending"

    s = f"""    static INLINE void sort_{width:02d}v_merge_{suffix}({generate_param_def_list(width, type)}) {{
    {bitonic_type_map[type]}  tmp{maybe_cmp()};{maybe_topbit()}"""
    print(s, file=f)

    for r in range(w1 + 1, width + 1):
        x = r - w1
        s = f"""
    tmp = d{x:02d};
    {crappity_crap_crap(f"d{r:02d}", f"d{x:02d}", type)}
    d{x:02d} = {generate_min(f"d{r:02d}", f"d{x:02d}", type)};
    {crappity_crap_crap(f"d{r:02d}", "tmp", type)}
    d{r:02d} = {generate_max(f"d{r:02d}", "tmp", type)};"""
        print(s, file=f)

    s = f"""
    sort_{w1:02d}v_merge_{suffix}({generate_param_list(1, w1)});
    sort_{w2:02d}v_merge_{suffix}({generate_param_list(w1 + 1, w2)});"""
    print(s, file=f)
    print("    }", file=f)


def get_load_intrinsic(type, v, offset):
    if type == "double":
        return f"_mm256_loadu_pd(({type} const *) ((__m256d const *) {v} + {offset}))"
    if type == "float":
        return f"_mm256_loadu_ps(({type} const *) ((__m256 const *) {v} + {offset}))"
    return f"_mm256_lddqu_si256((__m256i const *) {v} + {offset});"


def get_store_intrinsic(type, ptr, offset, value):
    if type == "double":
        return f"_mm256_storeu_pd(({type} *) ((__m256d *)  {ptr} + {offset}), {value})"
    if type == "float":
        return f"_mm256_storeu_ps(({type} *) ((__m256 *)  {ptr} + {offset}), {value})"
    return f"_mm256_storeu_si256((__m256i *) {ptr} + {offset}, {value})"


def generate_entry_points(f, type):
    for m in range(1, max_bitonic_sort_verctors + 1):
        s = f"""
static NOINLINE void sort_{m:02d}v({type} *ptr) {{"""
        print(s, file=f)

        for l in range(0, m):
            s = f"    {bitonic_type_map[type]} d{l + 1:02d} = {get_load_intrinsic(type, 'ptr', l)};"
            print(s, file=f)

        s = f"    sort_{m:02d}v_ascending({generate_param_list(1, m)});"
        print(s, file=f)

        for l in range(0, m):
            s = f"    {get_store_intrinsic(type, 'ptr', l, f'd{l + 1:02d}')};"
            print(s, file=f)

        print("}", file=f)


def generate_master_entry_point(f, type):
    s = f"""    static void sort({type} *ptr, size_t length) {{
    const int N = {int(32 / bitonic_size_map[type])};

    switch(length / N) {{"""
    print(s, file=f)

    for m in range(1, max_bitonic_sort_verctors + 1):
        s = f"        case {m}: sort_{m:02d}v(ptr); break;"
        print(s, file=f)
    print("    }", file=f)
    print("}", file=f)
    pass


def autogenerated_blabber():
    return f"""/////////////////////////////////////////////////////////////////////////////
////
// This file was auto-generated by a tool at {datetime.now().strftime("%F %H:%M:%S")}
//
// It is recommended you DO NOT directly edit this file but instead edit
// the code-generator that generated this source file instead.
/////////////////////////////////////////////////////////////////////////////"""


def generate_per_type(f, type, opts):
    s = f"""{autogenerated_blabber()}

#ifndef BITONIC_SORT_{str(opts.vector_isa).upper()}_{type.upper()}_H
#define BITONIC_SORT_{str
    (opts.vector_isa).upper()}_{type.upper()}_H

#include <immintrin.h>
#include "bitonic_sort.h"
                                                                                                                                                                                                                                                                                                                          
#ifdef _MSC_VER
    // MSVC
	#define INLINE __forceinline
	#define NOINLINE __declspec(noinline)
#else
    // GCC + Clang
	#define INLINE  __attribute__((always_inline))
	#define NOINLINE __attribute__((noinline))
#endif

#define i2d _mm256_castsi256_pd
#define d2i _mm256_castpd_si256
#define i2s _mm256_castsi256_ps
#define s2i _mm256_castps_si256
#define s2d _mm256_castps_pd
#define d2s _mm256_castpd_ps

namespace gcsort {{
namespace smallsort {{
template<> struct bitonic<{type}> {{
public:
"""
    print(s, file=f)
    generate_1v_sorters(f, type, ascending=True)
    generate_1v_sorters(f, type, ascending=False)
    for width in range(2, max_bitonic_sort_verctors + 1):
        generate_compounded_sorters(f, width, type, ascending=True)
        generate_compounded_sorters(f, width, type, ascending=False)
        if width <= largest_merge_variant_needed:
            generate_compounded_mergers(f, width, type, ascending=True)
            generate_compounded_mergers(f, width, type, ascending=False)

    generate_entry_points(f, type)
    generate_master_entry_point(f, type)
    print("};\n}\n}\n#endif", file=f)


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
    parser.add_argument("--language", type=Language, choices=list(Language), help="select output language: csharp/cpp/rust")
    parser.add_argument("--vector-isa", type=VectorISA, choices=list(VectorISA), help="select vector isa: AVX2/AVX512/SVE")

    opts = parser.parse_args()

    for type in bitonic_types:
        with open(f"bitonic_sort.{opts.vector_isa}.{type}.generated.h", "w") as f:
            generate_per_type(f, type, opts)


if __name__ == '__main__':
    generate_all_types()
