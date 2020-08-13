// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "vxsort_targets_enable_avx2.h"

#include "vxsort.h"
#include "machine_traits.avx2.h"
#include "packer.h"

void do_vxsort_avx2 (uint8_t** low, uint8_t** high, uint8_t* range_low, uint8_t* range_high)
{
    const int shift = 3;
    assert((1 << shift) == sizeof(size_t));
    auto sorter = vxsort::vxsort<int64_t, vxsort::vector_machine::AVX2, 8, shift>();
    sorter.sort ((int64_t*)low, (int64_t*)high, (int64_t)range_low, (int64_t)(range_high+sizeof(uint8_t*)));
}
#include "vxsort_targets_disable.h"
