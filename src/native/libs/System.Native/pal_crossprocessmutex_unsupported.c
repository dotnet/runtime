// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_crossprocessmutex.h"
#include "pal_errno.h"

#include <stdio.h>
#include <limits.h>
#include <sched.h>
#include <assert.h>
#include <stdbool.h>
#include <stdlib.h>

struct LowLevelCrossProcessMutex
{
    bool Dummy;
};

int32_t SystemNative_LowLevelCrossProcessMutex_Size(void)
{
    return (int32_t)sizeof(LowLevelCrossProcessMutex);
}

int32_t SystemNative_LowLevelCrossProcessMutex_Init(LowLevelCrossProcessMutex* mutex)
{
    (void)mutex;
    return Error_EINVAL;
}

int32_t SystemNative_LowLevelCrossProcessMutex_Acquire(LowLevelCrossProcessMutex* mutex, int32_t timeoutMilliseconds)
{
    (void)mutex;
    (void)timeoutMilliseconds;
    return Error_EINVAL;
}

int32_t SystemNative_LowLevelCrossProcessMutex_Release(LowLevelCrossProcessMutex* mutex)
{
    (void)mutex;
    return Error_EINVAL;
}

int32_t SystemNative_LowLevelCrossProcessMutex_Destroy(LowLevelCrossProcessMutex* mutex)
{
    (void)mutex;
    return Error_EINVAL;
}

void SystemNative_LowLevelCrossProcessMutex_GetOwnerProcessAndThreadId(LowLevelCrossProcessMutex* mutex, uint32_t* pOwnerProcessId, uint32_t* pOwnerThreadId)
{
    (void)mutex;
    (void)pOwnerProcessId;
    (void)pOwnerThreadId;
}

void SystemNative_LowLevelCrossProcessMutex_SetOwnerProcessAndThreadId(LowLevelCrossProcessMutex* mutex, uint32_t ownerProcessId, uint32_t ownerThreadId)
{
    (void)mutex;
    (void)ownerProcessId;
    (void)ownerThreadId;
}

uint8_t SystemNative_LowLevelCrossProcessMutex_IsAbandoned(LowLevelCrossProcessMutex* mutex)
{
    (void)mutex;
    return false;
}

void SystemNative_LowLevelCrossProcessMutex_SetAbandoned(LowLevelCrossProcessMutex* mutex, uint8_t isAbandoned)
{
    (void)mutex;
    (void)isAbandoned;
}
