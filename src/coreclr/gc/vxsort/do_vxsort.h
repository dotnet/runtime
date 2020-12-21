// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Enum for the IsSupportedInstructionSet method
enum class InstructionSet
{
    AVX2 = 0,
    AVX512F = 1,
};

void InitSupportedInstructionSet (int32_t configSetting);
bool IsSupportedInstructionSet (InstructionSet instructionSet);

void do_vxsort_avx2 (uint8_t** low, uint8_t** high, uint8_t *range_low, uint8_t *range_high);

void do_vxsort_avx512 (uint8_t** low, uint8_t** high, uint8_t* range_low, uint8_t* range_high);
