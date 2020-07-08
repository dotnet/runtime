// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "vxsort_targets_enable_avx512.h"

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
            return -0x7fffffff - 1;
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
            return -0x7fffffffffffffffi64 - 1;
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
#include "machine_traits.avx512.h"

void do_vxsort_avx512 (uint8_t** low, uint8_t** high)
{
  auto sorter = vxsort::vxsort<int64_t, vxsort::vector_machine::AVX512, 8>();
  sorter.sort ((int64_t*)low, (int64_t*)high);
}

void do_vxsort_avx512 (int32_t* low, int32_t* high)
{
  auto sorter = vxsort::vxsort<int32_t, vxsort::vector_machine::AVX512, 8>();
  sorter.sort (low, high);
}

#include "vxsort_targets_disable.h"
