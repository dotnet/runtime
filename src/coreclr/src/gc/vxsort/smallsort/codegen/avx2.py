##
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
##

import os
from datetime import datetime

from utils import native_size_map, next_power_of_2
from bitonic_isa import BitonicISA


class AVX2BitonicISA(BitonicISA):
    def __init__(self, type):
        self.vector_size_in_bytes = 32

        self.type = type

        self.bitonic_size_map = {}

        for t, s in native_size_map.items():
            self.bitonic_size_map[t] = int(self.vector_size_in_bytes / s)

        self.bitonic_type_map = {
            "int32_t": "__m256i",
            "uint32_t": "__m256i",
            "float": "__m256",
            "int64_t": "__m256i",
            "uint64_t": "__m256i",
            "double": "__m256d",
        }

    def max_bitonic_sort_vectors(self):
        return 16

    def vector_size(self):
        return self.bitonic_size_map[self.type]

    def vector_type(self):
        return self.bitonic_type_map[self.type]

    @classmethod
    def supported_types(cls):
        return native_size_map.keys()

    def i2d(self, v):
        t = self.type
        if t == "double":
            return v
        elif t == "float":
            return f"s2d({v})"
        return f"i2d({v})"

    def i2s(self, v):
        t = self.type
        if t == "double":
            raise Exception("Incorrect Type")
        elif t == "float":
            return f"i2s({v})"
        return v

    def d2i(self, v):
        t = self.type
        if t == "double":
            return v
        elif t == "float":
            return f"d2s({v})"
        return f"d2i({v})"

    def s2i(self, v):
        t = self.type
        if t == "double":
            raise Exception("Incorrect Type")
        elif t == "float":
            return f"s2i({v})"
        return v

    def generate_param_list(self, start, numParams):
        return str.join(", ", list(map(lambda p: f"d{p:02d}", range(start, start + numParams))))

    def generate_param_def_list(self, numParams):
        t = self.type
        return str.join(", ", list(map(lambda p: f"{self.vector_type()}& d{p:02d}", range(1, numParams + 1))))

    def generate_shuffle_X1(self, v):
        size = self.vector_size()
        if size == 8:
            return self.i2s(f"_mm256_shuffle_epi32({self.s2i(v)}, 0xB1)")
        elif size == 4:
            return self.d2i(f"_mm256_shuffle_pd({self.i2d(v)}, {self.i2d(v)}, 0x5)")

    def generate_shuffle_X2(self, v):
        size = self.vector_size()
        if size == 8:
            return self.i2s(f"_mm256_shuffle_epi32({self.s2i(v)}, 0x4E)")
        elif size == 4:
            return self.d2i(f"_mm256_permute4x64_pd({self.i2d(v)}, 0x4E)")

    def generate_shuffle_XR(self, v):
        size = self.vector_size()
        if size == 8:
            return self.i2s(f"_mm256_shuffle_epi32({self.s2i(v)}, 0x1B)")
        elif size == 4:
            return self.d2i(f"_mm256_permute4x64_pd({self.i2d(v)}, 0x1B)")

    def generate_blend_B1(self, v1, v2, ascending):
        size = self.vector_size()
        if size == 8:
            if ascending:
                return self.i2s(f"_mm256_blend_epi32({self.s2i(v1)}, {self.s2i(v2)}, 0xAA)")
            else:
                return self.i2s(f"_mm256_blend_epi32({self.s2i(v2)}, {self.s2i(v1)}, 0xAA)")
        elif size == 4:
            if ascending:
                return self.d2i(f"_mm256_blend_pd({self.i2d(v1)}, {self.i2d(v2)}, 0xA)")
            else:
                return self.d2i(f"_mm256_blend_pd({self.i2d(v2)}, {self.i2d(v1)}, 0xA)")

    def generate_blend_B2(self, v1, v2, ascending):
        size = self.vector_size()
        if size == 8:
            if ascending:
                return self.i2s(f"_mm256_blend_epi32({self.s2i(v1)}, {self.s2i(v2)}, 0xCC)")
            else:
                return self.i2s(f"_mm256_blend_epi32({self.s2i(v2)}, {self.s2i(v1)}, 0xCC)")
        elif size == 4:
            if ascending:
                return self.d2i(f"_mm256_blend_pd({self.i2d(v1)}, {self.i2d(v2)}, 0xC)")
            else:
                return self.d2i(f"_mm256_blend_pd({self.i2d(v2)}, {self.i2d(v1)}, 0xC)")

    def generate_blend_B4(self, v1, v2, ascending):
        size = self.vector_size()
        if size == 8:
            if ascending:
                return self.i2s(f"_mm256_blend_epi32({self.s2i(v1)}, {self.s2i(v2)}, 0xF0)")
            else:
                return self.i2s(f"_mm256_blend_epi32({self.s2i(v2)}, {self.s2i(v1)}, 0xF0)")
        elif size == 4:
            raise Exception("Incorrect Size")

    def generate_cross(self, v):
        size = self.vector_size()
        if size == 8:
            return self.d2i(f"_mm256_permute4x64_pd({self.i2d(v)}, 0x4E)")
        elif size == 4:
            raise Exception("Incorrect Size")

    def generate_reverse(self, v):
        size = self.vector_size()
        if size == 8:
            v = f"_mm256_shuffle_epi32({self.s2i(v)}, 0x1B)"
            return self.d2i(f"_mm256_permute4x64_pd(i2d({v}), 0x4E)")
        elif size == 4:
            return self.d2i(f"_mm256_permute4x64_pd({self.i2d(v)}, 0x1B)")

    def crappity_crap_crap(self, v1, v2):
        t = self.type
        if t == "int64_t":
            return f"cmp = _mm256_cmpgt_epi64({v1}, {v2});"
        elif t == "uint64_t":
            return f"cmp = _mm256_cmpgt_epi64(_mm256_xor_si256(topBit, {v1}), _mm256_xor_si256(topBit, {v2}));"

        return ""

    def generate_min(self, v1, v2):
        t = self.type
        if t == "int32_t":
            return f"_mm256_min_epi32({v1}, {v2})"
        elif t == "uint32_t":
            return f"_mm256_min_epu32({v1}, {v2})"
        elif t == "float":
            return f"_mm256_min_ps({v1}, {v2})"
        elif t == "int64_t":
            return self.d2i(f"_mm256_blendv_pd({self.i2d(v1)}, {self.i2d(v2)}, i2d(cmp))")
        elif t == "uint64_t":
            return self.d2i(f"_mm256_blendv_pd({self.i2d(v1)}, {self.i2d(v2)}, i2d(cmp))")
        elif t == "double":
            return f"_mm256_min_pd({v1}, {v2})"

    def generate_max(self, v1, v2):
        t = self.type
        if t == "int32_t":
            return f"_mm256_max_epi32({v1}, {v2})"
        elif t == "uint32_t":
            return f"_mm256_max_epu32({v1}, {v2})"
        elif t == "float":
            return f"_mm256_max_ps({v1}, {v2})"
        elif t == "int64_t":
            return self.d2i(f"_mm256_blendv_pd({self.i2d(v2)}, {self.i2d(v1)}, i2d(cmp))")
        elif t == "uint64_t":
            return self.d2i(f"_mm256_blendv_pd({self.i2d(v2)}, {self.i2d(v1)}, i2d(cmp))")
        elif t == "double":
            return f"_mm256_max_pd({v1}, {v2})"

    def get_load_intrinsic(self, v, offset):
        t = self.type
        if t == "double":
            return f"_mm256_loadu_pd(({t} const *) ((__m256d const *) {v} + {offset}))"
        if t == "float":
            return f"_mm256_loadu_ps(({t} const *) ((__m256 const *) {v} + {offset}))"
        return f"_mm256_lddqu_si256((__m256i const *) {v} + {offset});"


    def get_store_intrinsic(self, ptr, offset, value):
        t = self.type
        if t == "double":
            return f"_mm256_storeu_pd(({t} *) ((__m256d *)  {ptr} + {offset}), {value})"
        if t == "float":
            return f"_mm256_storeu_ps(({t} *) ((__m256 *)  {ptr} + {offset}), {value})"
        return f"_mm256_storeu_si256((__m256i *) {ptr} + {offset}, {value})"

    def autogenerated_blabber(self):
        return f"""/////////////////////////////////////////////////////////////////////////////
////
// This file was auto-generated by a tool at {datetime.now().strftime("%F %H:%M:%S")}
//
// It is recommended you DO NOT directly edit this file but instead edit
// the code-generator that generated this source file instead.
/////////////////////////////////////////////////////////////////////////////"""

    def generate_prologue(self, f):
        t = self.type
        s = f"""{self.autogenerated_blabber()}

#ifndef BITONIC_SORT_AVX2_{t.upper()}_H
#define BITONIC_SORT_AVX2_{t.upper()}_H

#ifdef __GNUC__
#ifdef __clang__
#pragma clang attribute push (__attribute__((target("avx2"))), apply_to = any(function))
#else
#pragma GCC push_options
#pragma GCC target("avx2")
#endif
#endif

#include <immintrin.h>
#include "bitonic_sort.h"

#define i2d _mm256_castsi256_pd
#define d2i _mm256_castpd_si256
#define i2s _mm256_castsi256_ps
#define s2i _mm256_castps_si256
#define s2d _mm256_castps_pd
#define d2s _mm256_castpd_ps

namespace vxsort {{
namespace smallsort {{
template<> struct bitonic<{t}, AVX2> {{
public:
"""
        print(s, file=f)

    def generate_epilogue(self, f):
        s = f"""
}};
}}
}}

#undef i2d
#undef d2i
#undef i2s
#undef s2i
#undef s2d
#undef d2s

#ifdef __GNUC__
#ifdef __clang__
#pragma clang attribute pop
#else
#pragma GCC pop_options
#endif
#endif
#endif
    """
        print(s, file=f)

    def generate_1v_basic_sorters(self, f, ascending):
        g = self
        type = self.type
        maybe_cmp = lambda: ", cmp" if (type == "int64_t" or type == "uint64_t") else ""
        maybe_topbit = lambda: f"\n        {g.vector_type()} topBit = _mm256_set1_epi64x(1LLU << 63);" if (type == "uint64_t") else ""
        suffix = "ascending" if ascending else "descending"

        s = f"""    static INLINE void sort_01v_{suffix}({g.generate_param_def_list(1)}) {{
            {g.vector_type()}  min, max, s{maybe_cmp()};{maybe_topbit()}

            s = {g.generate_shuffle_X1("d01")};
            {g.crappity_crap_crap("s", "d01")}
            min = {g.generate_min("s", "d01")};
            max = {g.generate_max("s", "d01")};
            d01 = {g.generate_blend_B1("min", "max", ascending)};

            s = {g.generate_shuffle_XR("d01")};
            {g.crappity_crap_crap("s", "d01")}
            min = {g.generate_min("s", "d01")};
            max = {g.generate_max("s", "d01")};
            d01 = {g.generate_blend_B2("min", "max", ascending)};

            s = {g.generate_shuffle_X1("d01")};
            {g.crappity_crap_crap("s", "d01")}
            min = {g.generate_min("s", "d01")};
            max = {g.generate_max("s", "d01")};
            d01 = {g.generate_blend_B1("min", "max", ascending)};"""

        print(s, file=f)

        if g.vector_size() == 8:
            s = f"""
            s = {g.generate_reverse("d01")};
            min = {g.generate_min("s", "d01")};
            max = {g.generate_max("s", "d01")};
            d01 = {g.generate_blend_B4("min", "max", ascending)};

            s = {g.generate_shuffle_X2("d01")};
            min = {g.generate_min("s", "d01")};
            max = {g.generate_max("s", "d01")};
            d01 = {g.generate_blend_B2("min", "max", ascending)};

            s = {g.generate_shuffle_X1("d01")};
            min = {g.generate_min("s", "d01")};
            max = {g.generate_max("s", "d01")};
            d01 = {g.generate_blend_B1("min", "max", ascending)};"""
            print(s, file=f)
        print("}", file=f)



    def generate_1v_merge_sorters(self, f, ascending: bool):
        g = self
        type = self.type
        maybe_cmp = lambda: ", cmp" if (type == "int64_t" or type == "uint64_t") else ""
        maybe_topbit = lambda: f"\n        {g.vector_type()} topBit = _mm256_set1_epi64x(1LLU << 63);" if (
                type == "uint64_t") else ""

        suffix = "ascending" if ascending else "descending"

        s = f"""    static INLINE void sort_01v_merge_{suffix}({g.generate_param_def_list(1)}) {{
            {g.vector_type()}  min, max, s{maybe_cmp()};{maybe_topbit()}"""
        print(s, file=f)

        if g.vector_size() == 8:
            s = f"""
            s = {g.generate_cross("d01")};
            min = {g.generate_min("s", "d01")};
            max = {g.generate_max("s", "d01")};
            d01 = {g.generate_blend_B4("min", "max", ascending)};"""
            print(s, file=f)

        s = f"""
            s = {g.generate_shuffle_X2("d01")};
            {g.crappity_crap_crap("s", "d01")}
            min = {g.generate_min("s", "d01")};
            max = {g.generate_max("s", "d01")};
            d01 = {g.generate_blend_B2("min", "max", ascending)};

            s = {g.generate_shuffle_X1("d01")};
            {g.crappity_crap_crap("s", "d01")}
            min = {g.generate_min("s", "d01")};
            max = {g.generate_max("s", "d01")};
            d01 = {g.generate_blend_B1("min", "max", ascending)};"""

        print(s, file=f)
        print("    }", file=f)

    def generate_compounded_sorter(self, f, width, ascending, inline):
        type = self.type
        g = self
        maybe_cmp = lambda: ", cmp" if (type == "int64_t" or type == "uint64_t") else ""
        maybe_topbit = lambda: f"\n        {g.vector_type()} topBit = _mm256_set1_epi64x(1LLU << 63);" if (
                type == "uint64_t") else ""

        w1 = int(next_power_of_2(width) / 2)
        w2 = int(width - w1)

        suffix = "ascending" if ascending else "descending"
        rev_suffix = "descending" if ascending else "ascending"

        inl = "INLINE" if inline else "NOINLINE"

        s = f"""    static {inl} void sort_{width:02d}v_{suffix}({g.generate_param_def_list(width)}) {{
        {g.vector_type()}  tmp{maybe_cmp()};{maybe_topbit()}

        sort_{w1:02d}v_{suffix}({g.generate_param_list(1, w1)});
        sort_{w2:02d}v_{rev_suffix}({g.generate_param_list(w1 + 1, w2)});"""

        print(s, file=f)

        for r in range(w1 + 1, width + 1):
            x = w1 + 1 - (r - w1)
            s = f"""
        tmp = d{r:02d};
        {g.crappity_crap_crap(f"d{x:02d}", f"d{r:02d}")}
        d{r:02d} = {g.generate_max(f"d{x:02d}", f"d{r:02d}")};
        d{x:02d} = {g.generate_min(f"d{x:02d}", "tmp")};"""

            print(s, file=f)

        s = f"""
        sort_{w1:02d}v_merge_{suffix}({g.generate_param_list(1, w1)});
        sort_{w2:02d}v_merge_{suffix}({g.generate_param_list(w1 + 1, w2)});"""
        print(s, file=f)
        print("    }", file=f)


    def generate_compounded_merger(self, f, width, ascending, inline):
        type = self.type
        g = self
        maybe_cmp = lambda: ", cmp" if (type == "int64_t" or type == "uint64_t") else ""
        maybe_topbit = lambda: f"\n        {g.vector_type()} topBit = _mm256_set1_epi64x(1LLU << 63);" if (
                type == "uint64_t") else ""

        w1 = int(next_power_of_2(width) / 2)
        w2 = int(width - w1)

        suffix = "ascending" if ascending else "descending"
        rev_suffix = "descending" if ascending else "ascending"
        
        inl = "INLINE" if inline else "NOINLINE"

        s = f"""    static {inl} void sort_{width:02d}v_merge_{suffix}({g.generate_param_def_list(width)}) {{
        {g.vector_type()}  tmp{maybe_cmp()};{maybe_topbit()}"""
        print(s, file=f)

        for r in range(w1 + 1, width + 1):
            x = r - w1
            s = f"""
        tmp = d{x:02d};
        {g.crappity_crap_crap(f"d{r:02d}", f"d{x:02d}")}
        d{x:02d} = {g.generate_min(f"d{r:02d}", f"d{x:02d}")};
        {g.crappity_crap_crap(f"d{r:02d}", "tmp")}
        d{r:02d} = {g.generate_max(f"d{r:02d}", "tmp")};"""
            print(s, file=f)

        s = f"""
        sort_{w1:02d}v_merge_{suffix}({g.generate_param_list(1, w1)});
        sort_{w2:02d}v_merge_{suffix}({g.generate_param_list(w1 + 1, w2)});"""
        print(s, file=f)
        print("    }", file=f)


    def generate_entry_points(self, f):
        type = self.type
        g = self
        for m in range(1, g.max_bitonic_sort_vectors() + 1):
            s = f"""
        static NOINLINE void sort_{m:02d}v({type} *ptr) {{"""
            print(s, file=f)

            for l in range(0, m):
                s = f"        {g.vector_type()} d{l + 1:02d} = {g.get_load_intrinsic('ptr', l)};"
                print(s, file=f)

            s = f"        sort_{m:02d}v_ascending({g.generate_param_list(1, m)});"
            print(s, file=f)

            for l in range(0, m):
                s = f"        {g.get_store_intrinsic('ptr', l, f'd{l + 1:02d}')};"
                print(s, file=f)

            print("}", file=f)


    def generate_master_entry_point(self, f_header, f_src):
        basename = os.path.basename(f_header.name)
        s = f"""#include "{basename}"

using namespace vxsort;
"""
        print(s, file=f_src)

        t = self.type
        g = self

        s = f"""    static void sort({t} *ptr, size_t length);"""
        print(s, file=f_header)

        s = f"""void vxsort::smallsort::bitonic<{t}, vector_machine::AVX2 >::sort({t} *ptr, size_t length) {{
    const int N = {g.vector_size()};

    switch(length / N) {{"""
        print(s, file=f_src)

        for m in range(1, self.max_bitonic_sort_vectors() + 1):
            s = f"        case {m}: sort_{m:02d}v(ptr); break;"
            print(s, file=f_src)
        print("    }", file=f_src)
        print("}", file=f_src)
        pass
