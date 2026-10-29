// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "MiscNativeHelpers.h"
#include "yieldprocessornormalized.h"
#include <minipal/cpuid.h>
#include <minipal/memorybarrierprocesswide.h>
#include <minipal/time.h>

#if defined(TARGET_X86) || defined(TARGET_AMD64)
extern "C" void QCALLTYPE X86Base_CpuId(int cpuInfo[4], int functionId, int subFunctionId)
{
    __cpuidex(cpuInfo, functionId, subFunctionId);
}
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

extern "C" void QCALLTYPE Interlocked_MemoryBarrierProcessWide()
{
    minipal_memory_barrier_process_wide();
}

extern "C" void QCALLTYPE Thread_SpinWait(int32_t iterations)
{
    if (iterations <= 0)
    {
        return;
    }

    YieldProcessorNormalized((unsigned int)iterations);
}

extern "C" int32_t QCALLTYPE Thread_Yield()
{
    return minipal_switch_to_thread(0) ? 1 : 0;
}

extern "C" int32_t QCALLTYPE Thread_GetOptimalMaxSpinWaitsPerSpinIteration()
{
    return (int32_t)YieldProcessorNormalization::GetOptimalMaxNormalizedYieldsPerSpinIteration();
}

// 64-bit multiply and shift helpers called from JIT-generated code. x86 implements them in assembly
// (i386/MiscStubs), except for RhpLMul on Unix in CoreCLR. NativeAOT only uses them on ARM, where the
// plain C calling convention is the same as CoreCLR's HCIMPL2_VV.
#ifdef FEATURE_NATIVEAOT
#define LONG_HELPER_IMPL2(rettype, funcname, a1, a2) EXTERN_C rettype F_CALL_CONV funcname(a1, a2) {
#define LONG_HELPER_IMPLEND }
#ifdef HOST_ARM
#define NEED_LONG_MUL_HELPER
#define NEED_LONG_SHIFT_HELPERS
#endif
#else // FEATURE_NATIVEAOT
#define LONG_HELPER_IMPL2(rettype, funcname, a1, a2) HCIMPL2_VV(rettype, funcname, a1, a2) FCALL_CONTRACT;
#define LONG_HELPER_IMPLEND HCIMPLEND
#if !defined(TARGET_X86) || defined(TARGET_UNIX)
#define NEED_LONG_MUL_HELPER
#endif
#if !defined(HOST_64BIT) && !defined(TARGET_X86)
#define NEED_LONG_SHIFT_HELPERS
#endif
#endif // FEATURE_NATIVEAOT

#ifdef NEED_LONG_MUL_HELPER
LONG_HELPER_IMPL2(int64_t, RhpLMul, int64_t val1, int64_t val2)
{
    return val1 * val2;
}
LONG_HELPER_IMPLEND
#endif // NEED_LONG_MUL_HELPER

#ifdef NEED_LONG_SHIFT_HELPERS
LONG_HELPER_IMPL2(uint64_t, RhpLLsh, uint64_t num, int32_t shift)
{
    return num << (shift & 0x3F);
}
LONG_HELPER_IMPLEND

LONG_HELPER_IMPL2(int64_t, RhpLRsh, int64_t num, int32_t shift)
{
    return num >> (shift & 0x3F);
}
LONG_HELPER_IMPLEND

LONG_HELPER_IMPL2(uint64_t, RhpLRsz, uint64_t num, int32_t shift)
{
    return num >> (shift & 0x3F);
}
LONG_HELPER_IMPLEND
#endif // NEED_LONG_SHIFT_HELPERS
