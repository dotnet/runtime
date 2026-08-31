// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "vxsort.h"
#include "machine_traits.neon.h"

void do_vxsort_neon (uint8_t** low, uint8_t** high, uint8_t* range_low, uint8_t* range_high)
{
    const int shift = 3;
    assert((1 << shift) == sizeof(size_t));
    auto sorter = vxsort::vxsort<uint64_t, vxsort::vector_machine::NEON, 8, shift>();
    sorter.sort ((uint64_t*)low, (uint64_t*)high, (uint64_t)range_low, (uint64_t)(range_high+sizeof(uint8_t*)));
}
