##
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
##

from datetime import datetime

from utils import native_size_map, next_power_of_2
from bitonic_isa import BitonicISA
import os


class AVX512BitonicISA(BitonicISA):
    def __init__(self, type):
        self.vector_size_in_bytes = 64

        self.type = type

        self.bitonic_size_map = {}

        for t, s in native_size_map.items():
            self.bitonic_size_map[t] = int(self.vector_size_in_bytes / s)

        self.bitonic_type_map = {
            "int32_t": "__m512i",
            "uint32_t": "__m512i",
            "float": "__m512",
            "int64_t": "__m512i",
            "uint64_t": "__m512i",
            "double": "__m512d",
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
            raise Exception("Incorrect Type")
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
            raise Exception("Incorrect Type")
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

    def generate_shuffle_S1(self, v):
        t = self.type
        size = self.bitonic_size_map[t]
        if size == 16:
            return self.i2s(f"_mm512_shuffle_epi32({self.s2i(v)}, _MM_PERM_CDAB)")
        elif size == 8:
            return self.d2i(f"_mm512_permute_pd({self.i2d(v)}, _MM_PERM_BBBB)")

    def generate_shuffle_X4(self, v):
        t = self.type
        size = self.bitonic_size_map[t]
        if size == 16:
            return self.i2s(f"_mm512_shuffle_epi32({self.s2i(v)}, _MM_PERM_ABCD)")
        elif size == 8:
            return self.d2i(f"_mm512_permutex_pd({self.i2d(v)}, _MM_PERM_ABCD)")

    def generate_shuffle_X8(self, v):
        t = self.type
        size = self.bitonic_size_map[t]
        if size == 16:
            s1 = f"_mm512_shuffle_epi32({self.s2i(v)}, _MM_PERM_ABCD)"
            return self.i2s(f"_mm512_permutex_epi64({s1}, _MM_PERM_BADC)")
        elif size == 8:
            s1 = f"_mm512_permutex_pd({self.i2d(v)}, _MM_PERM_ABCD)"
            return self.d2i(f"_mm512_shuffle_f64x2({s1}, {s1}, _MM_PERM_BADC)")

    def generate_shuffle_S2(self, v):
        t = self.type
        size = self.bitonic_size_map[t]
        if size == 16:
            return self.i2s(f"_mm512_shuffle_epi32({self.s2i(v)}, _MM_PERM_BADC)")
        elif size == 8:
            return self.d2i(f"_mm512_permutex_pd({self.i2d(v)}, _MM_PERM_BADC)")

    def generate_shuffle_X16(self, v):
        t = self.type
        size = self.bitonic_size_map[t]
        if size == 16:
            s1 = f"_mm512_shuffle_epi32({self.s2i(v)}, _MM_PERM_ABCD)"
            return self.i2s(f"_mm512_shuffle_i64x2({s1}, {s1}, _MM_PERM_ABCD)")
        elif size == 8:
            return self.d2i(f"_mm512_shuffle_pd({self.i2d(v)}, {self.i2d(v)}, 0xB1)")

    def generate_shuffle_S4(self, v):
        t = self.type
        size = self.bitonic_size_map[t]
        if size == 16:
            return self.i2s(f"_mm512_permutex_epi64({self.s2i(v)}, _MM_PERM_BADC)")
        elif size == 8:
            return self.d2i(f"_mm512_shuffle_f64x2({self.i2d(v)}, {self.i2d(v)}, _MM_PERM_BADC)")

    def generate_shuffle_S8(self, v):
        t = self.type
        size = self.bitonic_size_map[t]
        if size == 16:
            return self.i2s(f"_mm512_shuffle_i64x2({self.s2i(v)}, {self.s2i(v)}, _MM_PERM_BADC)")
        elif size == 8:
            return self.d2i(f"_mm512_shuffle_pd({self.i2d(v)}, {self.i2d(v)}, 0xB1)")

    def generate_min(self, v1, v2):
        t = self.type
        if t == "int32_t":
            return f"_mm512_min_epi32({v1}, {v2})"
        elif t == "uint32_t":
            return f"_mm512_min_epu32({v1}, {v2})"
        elif t == "float":
            return f"_mm512_min_ps({v1}, {v2})"
        elif t == "int64_t":
            return f"_mm512_min_epi64({v1}, {v2})"
        elif t == "uint64_t":
            return f"_mm512_min_epu64({v1}, {v2})"
        elif t == "double":
            return f"_mm512_min_pd({v1}, {v2})"

    def generate_max(self, v1, v2):
        t = self.type
        if t == "int32_t":
            return f"_mm512_max_epi32({v1}, {v2})"
        elif t == "uint32_t":
            return f"_mm512_max_epu32({v1}, {v2})"
        elif t == "float":
            return f"_mm512_max_ps({v1}, {v2})"
        elif t == "int64_t":
            return f"_mm512_max_epi64({v1}, {v2})"
        elif t == "uint64_t":
            return f"_mm512_max_epu64({v1}, {v2})"
        elif t == "double":
            return f"_mm512_max_pd({v1}, {v2})"

    def generate_mask(self, stride, ascending):
        b = 1 << stride
        b = b - 1
        if ascending:
            b = b << stride

        mask = 0
        size = self.vector_size()
        while size > 0:
            mask = mask << (stride * 2) | b
            size = size - (stride * 2)
        return mask


    def generate_max_with_blend(self, src, v1, v2, stride, ascending):
        mask = self.generate_mask(stride, ascending)
        t = self.type
        if t == "int32_t":
            return f"_mm512_mask_max_epi32({src}, 0x{mask:04X}, {v1}, {v2})"
        elif t == "uint32_t":
            return f"_mm512_mask_max_epu32({src}, 0x{mask:04X}, {v1}, {v2})"
        elif t == "float":
            return f"_mm512_mask_max_ps({src}, 0x{mask:04X}, {v1}, {v2})"
        elif t == "int64_t":
            return f"_mm512_mask_max_epi64({src}, 0x{mask:04X}, {v1}, {v2})"
        elif t == "uint64_t":
            return f"_mm512_mask_max_epu64({src}, 0x{mask:04X}, {v1}, {v2})"
        elif t == "double":
            return f"_mm512_mask_max_pd({src}, 0x{mask:04X}, {v1}, {v2})"


    def get_load_intrinsic(self, v, offset):
        t = self.type
        if t == "double":
            return f"_mm512_loadu_pd(({t} const *) ((__m512d const *) {v} + {offset}))"
        if t == "float":
            return f"_mm512_loadu_ps(({t} const *) ((__m512 const *) {v} + {offset}))"
        return f"_mm512_loadu_si512((__m512i const *) {v} + {offset});"

    def get_mask_load_intrinsic(self, v, offset, mask):
        t = self.type

        if self.vector_size() == 8:
            int_suffix = "epi64"
            max_value = f"_mm512_set1_epi64(MAX)"
        elif self.vector_size() == 16:
            int_suffix = "epi32"
            max_value = f"_mm512_set1_epi32(MAX)"

        if t == "double":
            return f"""_mm512_mask_loadu_pd(_mm512_set1_pd(MAX),
                                           {mask},
                                           ({t} const *) ((__m512d const *) {v} + {offset}))"""
        elif t == "float":
            return f"""_mm512_mask_loadu_ps(_mm512_set1_ps(MAX),
                                           {mask},
                                           ({t} const *) ((__m512 const *) {v} + {offset}))"""

        return f"""_mm512_mask_loadu_{int_suffix}({max_value},
                                              {mask},
                                              ({t} const *) ((__m512i const *) {v} + {offset}))"""


    def get_store_intrinsic(self, ptr, offset, value):
        t = self.type
        if t == "double":
            return f"_mm512_storeu_pd(({t} *) ((__m512d *)  {ptr} + {offset}), {value})"
        if t == "float":
            return f"_mm512_storeu_ps(({t} *) ((__m512 *)  {ptr} + {offset}), {value})"
        return f"_mm512_storeu_si512((__m512i *) {ptr} + {offset}, {value})"

    def get_mask_store_intrinsic(self, ptr, offset, value, mask):
        t = self.type

        if self.vector_size() == 8:
            int_suffix = "epi64"
        elif self.vector_size() == 16:
            int_suffix = "epi32"

        if t == "double":
            return f"_mm512_mask_storeu_pd(({t} *) ((__m512d *)  {ptr} + {offset}), {mask}, {value})"
        if t == "float":
            return f"_mm512_mask_storeu_ps(({t} *) ((__m512 *)  {ptr} + {offset}), {mask}, {value})"
        return f"_mm512_mask_storeu_{int_suffix}((__m512i *) {ptr} + {offset}, {mask}, {value})"

    def autogenerated_blabber(self):
        return f"""// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/////////////////////////////////////////////////////////////////////////////
////
// This file was auto-generated by a tool at {datetime.now().strftime("%F %H:%M:%S")}
//
// It is recommended you DO NOT directly edit this file but instead edit
// the code-generator that generated this source file instead.
/////////////////////////////////////////////////////////////////////////////"""

    def generate_prologue(self, f):
        t = self.type
        s = f"""{self.autogenerated_blabber()}

#ifndef BITONIC_SORT_AVX512_{t.upper()}_H
#define BITONIC_SORT_AVX512_{t.upper()}_H


#ifdef __GNUC__
#ifdef __clang__
#pragma clang attribute push (__attribute__((target("avx512f"))), apply_to = any(function))
#else
#pragma GCC push_options
#pragma GCC target("avx512f")
#endif
#endif

#include <immintrin.h>
#include "bitonic_sort.h"

#define i2d _mm512_castsi512_pd
#define d2i _mm512_castpd_si512
#define i2s _mm512_castsi512_ps
#define s2i _mm512_castps_si512
#define s2d _mm512_castps_pd
#define d2s _mm521_castpd_ps

namespace vxsort {{
namespace smallsort {{
template<> struct bitonic<{t}, AVX512> {{
    static const int N = {self.vector_size()};
    static constexpr {t} MAX = std::numeric_limits<{t}>::max();
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
        suffix = "ascending" if ascending else "descending"

        s = f"""    static INLINE void sort_01v_{suffix}({g.generate_param_def_list(1)}) {{
        {g.vector_type()}  min, s;

        s = {g.generate_shuffle_S1("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 1, ascending)};

        s = {g.generate_shuffle_X4("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 2, ascending)};

        s = {g.generate_shuffle_S1("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 1, ascending)};

        s = {g.generate_shuffle_X8("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 4, ascending)};

        s = {g.generate_shuffle_S2("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 2, ascending)};

        s = {g.generate_shuffle_S1("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 1, ascending)};"""

        print(s, file=f)

        if g.vector_size() == 16:
            s = f"""
        s = {g.generate_shuffle_X16("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 8, ascending)};

        s = {g.generate_shuffle_S4("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 4, ascending)};

        s = {g.generate_shuffle_S2("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 2, ascending)};

        s = {g.generate_shuffle_S1("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 1, ascending)};"""
            print(s, file=f)
        print("    }", file=f)

    def generate_1v_merge_sorters(self, f, ascending: bool):
        g = self
        type = self.type
        suffix = "ascending" if ascending else "descending"

        s = f"""    static INLINE void sort_01v_merge_{suffix}({g.generate_param_def_list(1)}) {{
        {g.vector_type()}  min, s;"""
        print(s, file=f)

        if g.vector_size() == 16:
            s = f"""
        s = {g.generate_shuffle_S8("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 8, ascending)};"""
            print(s, file=f)

        s = f"""
        s = {g.generate_shuffle_S4("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 4, ascending)};

        s = {g.generate_shuffle_S2("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 2, ascending)};

        s = {g.generate_shuffle_S1("d01")};
        min = {g.generate_min("s", "d01")};
        d01 = {g.generate_max_with_blend("min", "s", "d01", 1, ascending)};"""

        print(s, file=f)
        print("    }", file=f)


    def generate_compounded_sorter(self, f, width, ascending, inline):
        type = self.type
        g = self

        w1 = int(next_power_of_2(width) / 2)
        w2 = int(width - w1)

        suffix = "ascending" if ascending else "descending"
        rev_suffix = "descending" if ascending else "ascending"

        inl = "INLINE" if inline else "NOINLINE"

        s = f"""    static {inl} void sort_{width:02d}v_{suffix}({g.generate_param_def_list(width)}) {{
        {g.vector_type()}  tmp;

        sort_{w1:02d}v_{suffix}({g.generate_param_list(1, w1)});
        sort_{w2:02d}v_{rev_suffix}({g.generate_param_list(w1 + 1, w2)});"""

        print(s, file=f)

        for r in range(w1 + 1, width + 1):
            x = w1 + 1 - (r - w1)
            s = f"""
        tmp = d{r:02d};
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

        w1 = int(next_power_of_2(width) / 2)
        w2 = int(width - w1)

        suffix = "ascending" if ascending else "descending"
        rev_suffix = "descending" if ascending else "ascending"

        inl = "INLINE" if inline else "NOINLINE"

        s = f"""    static {inl} void sort_{width:02d}v_merge_{suffix}({g.generate_param_def_list(width)}) {{
        {g.vector_type()}  tmp;"""
        print(s, file=f)

        for r in range(w1 + 1, width + 1):
            x = r - w1
            s = f"""
        tmp = d{x:02d};
        d{x:02d} = {g.generate_min(f"d{r:02d}", f"d{x:02d}")};
        d{r:02d} = {g.generate_max(f"d{r:02d}", "tmp")};"""
            print(s, file=f)

        s = f"""
        sort_{w1:02d}v_merge_{suffix}({g.generate_param_list(1, w1)});
        sort_{w2:02d}v_merge_{suffix}({g.generate_param_list(w1 + 1, w2)});"""
        print(s, file=f)
        print("    }", file=f)


    def generate_entry_points_old(self, f):
        type = self.type
        g = self
        for m in range(1, g.max_bitonic_sort_vectors() + 1):
            s = f"""
    static NOINLINE void sort_{m:02d}v_old({type} *ptr) {{"""
            print(s, file=f)

            for l in range(0, m):
                s = f"        {g.vector_type()} d{l + 1:02d} = {g.get_load_intrinsic('ptr', l)};"
                print(s, file=f)

            s = f"        sort_{m:02d}v_ascending({g.generate_param_list(1, m)});"
            print(s, file=f)

            for l in range(0, m):
                s = f"        {g.get_store_intrinsic('ptr', l, f'd{l + 1:02d}')};"
                print(s, file=f)

            print("    }", file=f)

    def generate_entry_points(self, f):
        type = self.type
        g = self
        for m in range(1, g.max_bitonic_sort_vectors() + 1):
            if self.vector_size() == 8:
                cast_to = "uint8_t"
            elif self.vector_size() == 16:
                cast_to = "uint16_t"

            s = f"""
    static NOINLINE void sort_{m:02d}v_alt({type} *ptr, int remainder) {{
        const auto mask = ({cast_to})(0x{((1 << self.vector_size()) - 1):X} >> ((N - remainder) & (N-1)));
"""
            print(s, file=f)

            for l in range(0, m-1):
                s = f"        {g.vector_type()} d{l + 1:02d} = {g.get_load_intrinsic('ptr', l)};"
                print(s, file=f)
            s = f"        {g.vector_type()} d{m:02d} = {g.get_mask_load_intrinsic('ptr', m - 1, 'mask')};"
            print(s, file=f)

            s = f"        sort_{m:02d}v_ascending({g.generate_param_list(1, m)});"
            print(s, file=f)

            for l in range(0, m-1):
                s = f"        {g.get_store_intrinsic('ptr', l, f'd{l + 1:02d}')};"
                print(s, file=f)
            s = f"        {g.get_mask_store_intrinsic('ptr', m - 1, f'd{m:02d}', 'mask')};"
            print(s, file=f)

            print("    }", file=f)


    def generate_master_entry_point(self, f_header, f_src):
        basename = os.path.basename(f_header.name)
        s = f"""// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "{basename}"

using namespace vxsort;
"""
        print(s, file=f_src)

        t = self.type
        g = self

        # s = f"""    static void sort_old({t} *ptr, size_t length);"""
        # print(s, file=f_header)

        s = f"""    static void sort({t} *ptr, size_t length);"""
        print(s, file=f_header)


    #     s = f"""void vxsort::smallsort::bitonic<{t}, vector_machine::AVX512 >::sort_old({t} *ptr, size_t length) {{
    # switch(length / N) {{"""
    #     print(s, file=f_src)
    #
    #     for m in range(1, self.max_bitonic_sort_vectors() + 1):
    #         s = f"        case {m}: sort_{m:02d}v(ptr); break;"
    #         print(s, file=f_src)
    #     print("    }", file=f_src)
    #     print("}", file=f_src)


        s = f"""void vxsort::smallsort::bitonic<{t}, vector_machine::AVX512 >::sort({t} *ptr, size_t length) {{
    const auto fullvlength = length / N;
    const int remainder = (int) (length - fullvlength * N);
    const auto v = fullvlength + ((remainder > 0) ? 1 : 0);
    switch(v) {{"""
        print(s, file=f_src)

        for m in range(1, self.max_bitonic_sort_vectors() + 1):
            s = f"        case {m}: sort_{m:02d}v_alt(ptr, remainder); break;"
            print(s, file=f_src)
        print("    }", file=f_src)

        print("}", file=f_src)
        pass
