// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifdef TARGET_WINDOWS
#include <intrin.h>
#include <windows.h>
#endif

#include "do_vxsort.h"

enum class SupportedISA
{
    None = 0,
    AVX2 = 1 << (int)InstructionSet::AVX2,
    AVX512F = 1 << (int)InstructionSet::AVX512F
};

#if defined(TARGET_AMD64) && defined(TARGET_WINDOWS)

static DWORD64 GetEnabledXStateFeaturesHelper()
{
    // On Windows we have an api(GetEnabledXStateFeatures) to check if AVX is supported
    typedef DWORD64(WINAPI* PGETENABLEDXSTATEFEATURES)();
    PGETENABLEDXSTATEFEATURES pfnGetEnabledXStateFeatures = NULL;

    HMODULE hMod = LoadLibraryExW(L"kernel32.dll", NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (hMod == NULL)
        return 0;

    pfnGetEnabledXStateFeatures = (PGETENABLEDXSTATEFEATURES)GetProcAddress(hMod, "GetEnabledXStateFeatures");

    if (pfnGetEnabledXStateFeatures == NULL)
    {
        return 0;
    }

    DWORD64 FeatureMask = pfnGetEnabledXStateFeatures();

    return FeatureMask;
}

SupportedISA DetermineSupportedISA()
{
    // register definitions to make the following code more readable
    enum reg
    {
        EAX = 0,
        EBX = 1,
        ECX = 2,
        EDX = 3,
        COUNT = 4
    };

    // bit definitions to make code more readable
    enum bits
    {
        OCXSAVE  = 1<<27,
        AVX      = 1<<28,
        AVX2     = 1<< 5,
        AVX512F  = 1<<16,
        AVX512DQ = 1<<17,
    };
    int reg[COUNT];

    __cpuid(reg, 0);
    if (reg[EAX] < 7)
        return SupportedISA::None;

    __cpuid(reg, 1);

    // both AVX and OCXSAVE feature flags must be enabled
    if ((reg[ECX] & (OCXSAVE|AVX)) != (OCXSAVE | AVX))
        return SupportedISA::None;

    // get xcr0 register
    DWORD64 xcr0 = _xgetbv(0);

    // get OS XState info 
    DWORD64 FeatureMask = GetEnabledXStateFeaturesHelper();

    // get processor extended feature flag info
    __cpuid(reg, 7);

    // check if all of AVX2, AVX512F and AVX512DQ are supported by both processor and OS
    if ((reg[EBX] & (AVX2 | AVX512F | AVX512DQ)) == (AVX2 | AVX512F | AVX512DQ) &&
        (xcr0 & 0xe6) == 0xe6 &&
        (FeatureMask & (XSTATE_MASK_AVX | XSTATE_MASK_AVX512)) == (XSTATE_MASK_AVX | XSTATE_MASK_AVX512))
    {
        return (SupportedISA)((int)SupportedISA::AVX2 | (int)SupportedISA::AVX512F);
    }

    // check if AVX2 is supported by both processor and OS
    if ((reg[EBX] & AVX2) &&
        (xcr0 & 0x06) == 0x06 &&
        (FeatureMask & XSTATE_MASK_AVX) == XSTATE_MASK_AVX)
    {
        return SupportedISA::AVX2;
    }

    return SupportedISA::None;
}

#elif defined(TARGET_UNIX)

SupportedISA DetermineSupportedISA()
{
    __builtin_cpu_init();
    if (__builtin_cpu_supports("avx2"))
    {
        if (__builtin_cpu_supports("avx512f"))
            return (SupportedISA)((int)SupportedISA::AVX2 | (int)SupportedISA::AVX512F);
        else
            return SupportedISA::AVX2;
    }
    else
    {
        return SupportedISA::None;
    }
}

#endif // defined(TARGET_UNIX)

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
