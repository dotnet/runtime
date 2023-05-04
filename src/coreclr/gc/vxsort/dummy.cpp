// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "do_vxsort.h"

//
// Dummy replacement VXSORT support that always says the CPU doesn't
// support the required instruction set.
//

bool IsSupportedInstructionSet (InstructionSet instructionSet)
{
    return false;
}

void InitSupportedInstructionSet (int32_t configSetting)
{
}

void do_vxsort_avx2 (uint8_t** low, uint8_t** high, uint8_t *range_low, uint8_t *range_high)
{
    assert(false);
}

void do_vxsort_avx512 (uint8_t** low, uint8_t** high, uint8_t* range_low, uint8_t* range_high)
{
    assert(false);
}
