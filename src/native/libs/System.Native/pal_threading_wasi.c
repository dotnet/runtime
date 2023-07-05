// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_threading.h"

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
