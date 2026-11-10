// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "MiscNativeHelpers.h"
#include "ProcessEnvironment.h"
#include "yieldprocessornormalized.h"
#include <minipal/cpuid.h>
#include <minipal/memorybarrierprocesswide.h>
#include <minipal/time.h>
#include <errno.h>

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
// Names and values are UTF-8. Returns -1 if the variable is not set, the length of the value in bytes
// if it fits in the buffer, or the required buffer size including the null terminator otherwise.
extern "C" int32_t QCALLTYPE Environment_GetEnvironmentVariable(const char* name, char* buffer, int32_t bufferSize)
{
    uint32_t length;
    if (!ProcessEnvironment_TryGetVariableUtf8(name, buffer, (uint32_t)bufferSize, &length))
    {
        return -1;
    }

    return (int32_t)length;
}

// Returns 0 on success, including when unsetting a variable that is not set, 1 for an invalid name,
// or 2 when out of memory.
extern "C" int32_t QCALLTYPE Environment_SetEnvironmentVariable(const char* name, const char* value)
{
    switch (ProcessEnvironment_SetVariableUtf8(name, value))
    {
        case 0:
        case ENOENT:
            return 0;
        case EINVAL:
            return 1;
        default:
            return 2;
    }
}

// Returns a double-null-terminated block of UTF-8 "name=value" strings.
extern "C" char* QCALLTYPE Environment_GetEnvironmentStrings()
{
    return ProcessEnvironment_GetStringsUtf8();
}

extern "C" void QCALLTYPE Environment_FreeEnvironmentStrings(char* strings)
{
    ProcessEnvironment_FreeStringsUtf8(strings);
}
#endif // TARGET_UNIX
