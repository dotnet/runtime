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
