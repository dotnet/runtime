// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "MiscNativeHelpers.h"
#include "ProcessEnvironment.h"
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

#ifdef TARGET_UNIX
// Returns -1 if the variable is not set, the length of the value if it fits in the buffer,
// or the required buffer size including the null terminator otherwise.
extern "C" int32_t QCALLTYPE Environment_GetEnvironmentVariable(const CHAR16_T* name, CHAR16_T* buffer, int32_t bufferSize)
{
    uint32_t result = ProcessEnvironment_GetVariable(name, buffer, (uint32_t)bufferSize);

    // 0 is also returned for a variable with an empty value; probing without a buffer
    // returns the required size, which is non-zero for any variable that is set.
    if (result == 0 && ProcessEnvironment_GetVariable(name, nullptr, 0) == 0)
    {
        return -1;
    }

    return (int32_t)result;
}

extern "C" int32_t QCALLTYPE Environment_SetEnvironmentVariable(const CHAR16_T* name, const CHAR16_T* value)
{
    return ProcessEnvironment_SetVariable(name, value) ? 1 : 0;
}

extern "C" CHAR16_T* QCALLTYPE Environment_GetEnvironmentStrings()
{
    return ProcessEnvironment_GetStrings();
}

extern "C" void QCALLTYPE Environment_FreeEnvironmentStrings(CHAR16_T* strings)
{
    ProcessEnvironment_FreeStrings(strings);
}
#endif // TARGET_UNIX
