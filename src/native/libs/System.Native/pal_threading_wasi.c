// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_threading.h"
#include "pal_errno.h"

#include <stdio.h>
#include <limits.h>
#include <sched.h>
#include <assert.h>
#include <stdbool.h>
#include <stdlib.h>

#ifdef DEBUG
#define DEBUGNOTRETURN __attribute__((noreturn))
#else
#define DEBUGNOTRETURN
#endif

struct LowLevelMonitor
{
    bool Dummy;
};

LowLevelMonitor* SystemNative_LowLevelMonitor_Create(void)
{
    return (LowLevelMonitor *)malloc(sizeof(LowLevelMonitor));
}

void SystemNative_LowLevelMonitor_Destroy(LowLevelMonitor* monitor)
{
    assert(monitor != NULL);
    free(monitor);
}

void SystemNative_LowLevelMonitor_Acquire(LowLevelMonitor* monitor)
{
    assert(monitor != NULL);
}

void SystemNative_LowLevelMonitor_Release(LowLevelMonitor* monitor)
{
    assert(monitor != NULL);
}

void SystemNative_LowLevelMonitor_Wait(LowLevelMonitor* monitor)
{
    assert(monitor != NULL);
}

int32_t SystemNative_LowLevelMonitor_TimedWait(LowLevelMonitor *monitor, int32_t timeoutMilliseconds)
{
    assert(timeoutMilliseconds >= 0);
    return true;
}

void SystemNative_LowLevelMonitor_Signal_Release(LowLevelMonitor* monitor)
{
    assert(monitor != NULL);
}

int32_t SystemNative_CreateThread(uintptr_t stackSize, void *(*startAddress)(void*), void *parameter)
{
    return false;
}

int32_t SystemNative_SchedGetCpu(void)
{
    return -1;
}

DEBUGNOTRETURN
void SystemNative_Exit(int32_t exitCode)
{
    exit(exitCode);
}

DEBUGNOTRETURN
void SystemNative_Abort(void)
{
    abort();
}

// Gets a non-truncated OS thread ID that is also suitable for diagnostics, for platforms that offer a 64-bit ID
uint64_t SystemNative_GetUInt64OSThreadId(void)
{
    assert(false);
    return 0;
}

// Tries to get a non-truncated OS thread ID that is also suitable for diagnostics, for platforms that offer a 32-bit ID.
// Returns (uint32_t)-1 when the implementation does not know how to get the OS thread ID.
uint32_t SystemNative_TryGetUInt32OSThreadId(void)
{
    return (uint32_t)-1;
}

struct LowLevelCrossProcessMutex
{
    bool Dummy;
};

int32_t SystemNative_LowLevelCrossPlatformMutex_Size(void)
{
    return (int32_t)sizeof(LowLevelCrossProcessMutex);
}

int32_t SystemNative_LowLevelCrossPlatformMutex_Init(LowLevelCrossProcessMutex* mutex)
{
    assert(false);
    return Error_EINVAL;
}

int32_t SystemNative_LowLevelCrossPlatformMutex_Acquire(LowLevelCrossProcessMutex* mutex, int32_t timeoutMilliseconds)
{
    assert(false);
    return Error_EINVAL;
}

int32_t SystemNative_LowLevelCrossPlatformMutex_Release(LowLevelCrossProcessMutex* mutex)
{
    assert(false);
    return Error_EINVAL;
}

int32_t SystemNative_LowLevelCrossPlatformMutex_Destroy(LowLevelCrossProcessMutex* mutex)
{
    assert(false);
    return Error_EINVAL;
}

void SystemNative_LowLevelCrossPlatformMutex_GetOwnerProcessAndThreadId(LowLevelCrossProcessMutex* mutex, uint32_t* pOwnerProcessId, uint32_t* pOwnerThreadId)
{
    assert(false);
}

void SystemNative_LowLevelCrossPlatformMutex_SetOwnerProcessAndThreadId(LowLevelCrossProcessMutex* mutex, uint32_t ownerProcessId, uint32_t ownerThreadId)
{
    assert(false);
}

uint8_t SystemNative_LowLevelCrossPlatformMutex_IsAbandoned(LowLevelCrossProcessMutex* mutex)
{
    assert(false);
    return Error_EINVAL;
}

void SystemNative_LowLevelCrossPlatformMutex_SetAbandoned(LowLevelCrossProcessMutex* mutex, uint8_t isAbandoned)
{
    assert(false);
}
