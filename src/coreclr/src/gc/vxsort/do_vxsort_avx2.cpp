// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "vxsort_targets_enable_avx2.h"

namespace std
{
    template <class _Ty>
    class numeric_limits
    {
    public:
        static _Ty Max()
        {
            return _Ty();
        }
        static _Ty Min()
        {
            return _Ty();
        }
    };
    template <>
    class numeric_limits<int32_t>
    {
    public:
        static int32_t Max()
        {
            return 0x7fffffff;
        }
        static int32_t Min()
        {
            return -0x7fffffff-1;
        }
    };
    template <>
    class numeric_limits<int64_t>
    {
    public:
        static int64_t Max()
        {
            return 0x7fffffffffffffffi64;
        }

        static int64_t Min()
        {
            return -0x7fffffffffffffffi64-1;
        }
    };
}

#ifndef max
template <typename T>
T max (T a, T b)
{
    if (a > b) return a; else return b;
}
#endif
#include "vxsort.h"
#include "machine_traits.avx2.h"
#include "packer.h"

void do_vxsort_avx2 (uint8_t** low, uint8_t** high)
{
  auto sorter = vxsort::vxsort<int64_t, vxsort::vector_machine::AVX2, 8>();
  sorter.sort ((int64_t*)low, (int64_t*)high);
}

void do_vxsort_avx2 (int32_t* low, int32_t* high)
{
  auto sorter = vxsort::vxsort<int32_t, vxsort::vector_machine::AVX2, 8>();
  sorter.sort (low, high);
}

void do_pack_avx2 (uint8_t** mem, size_t len, uint8_t* base)
{
    auto packer = vxsort::packer<int64_t, int32_t, vxsort::vector_machine::AVX2, 3>();
    packer.pack ((int64_t*)mem, len, (int64_t)base);
}

void do_unpack_avx2 (int32_t* mem, size_t len, uint8_t* base)
{
    auto packer = vxsort::packer<int64_t, int32_t, vxsort::vector_machine::AVX2, 3>();
    packer.unpack (mem, len, (int64_t)base);
}
#include "vxsort_targets_disable.h"
