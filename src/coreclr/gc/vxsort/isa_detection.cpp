// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "do_vxsort.h"

#include <minipal/cpufeatures.h>

enum class SupportedISA
{
    None = 0,
    AVX2 = 1 << (int)InstructionSet::AVX2,
    AVX512F = 1 << (int)InstructionSet::AVX512F
};

SupportedISA DetermineSupportedISA()
{
    int cpuFeatures = minipal_getcpufeatures();
    if ((cpuFeatures & XArchIntrinsicConstants_Avx2) != 0)
    {
        if ((cpuFeatures & XArchIntrinsicConstants_Avx512) != 0)
            return (SupportedISA)((int)SupportedISA::AVX2 | (int)SupportedISA::AVX512F);
        else
            return SupportedISA::AVX2;
    }
    else
    {
        return SupportedISA::None;
    }
}

static bool s_initialized;
static SupportedISA s_supportedISA;

bool IsSupportedInstructionSet (InstructionSet instructionSet)
{
    assert(s_initialized);
    assert(instructionSet == InstructionSet::AVX2 || instructionSet == InstructionSet::AVX512F);
    return ((int)s_supportedISA & (1 << (int)instructionSet)) != 0;
}

void InitSupportedInstructionSet (int32_t configSetting)
{
    s_supportedISA = (SupportedISA)((int)DetermineSupportedISA() & configSetting);
    // we are assuming that AVX2 can be used if AVX512F can,
    // so if AVX2 is disabled, we need to disable AVX512F as well
    if (!((int)s_supportedISA & (int)SupportedISA::AVX2))
        s_supportedISA = SupportedISA::None;
    s_initialized = true;
}
