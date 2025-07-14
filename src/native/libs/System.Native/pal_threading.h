// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

typedef struct LowLevelMonitor LowLevelMonitor;
typedef struct LowLevelCrossProcessMutex LowLevelCrossProcessMutex;

PALEXPORT LowLevelMonitor *SystemNative_LowLevelMonitor_Create(void);

PALEXPORT void SystemNative_LowLevelMonitor_Destroy(LowLevelMonitor* monitor);

PALEXPORT void SystemNative_LowLevelMonitor_Acquire(LowLevelMonitor* monitor);

PALEXPORT void SystemNative_LowLevelMonitor_Release(LowLevelMonitor* monitor);

PALEXPORT void SystemNative_LowLevelMonitor_Wait(LowLevelMonitor* monitor);

PALEXPORT int32_t SystemNative_LowLevelMonitor_TimedWait(LowLevelMonitor *monitor, int32_t timeoutMilliseconds);

PALEXPORT void SystemNative_LowLevelMonitor_Signal_Release(LowLevelMonitor* monitor);

PALEXPORT int32_t SystemNative_CreateThread(uintptr_t stackSize, void *(*startAddress)(void*), void *parameter);

PALEXPORT int32_t SystemNative_SchedGetCpu(void);

PALEXPORT __attribute__((noreturn)) void SystemNative_Exit(int32_t exitCode);

PALEXPORT __attribute__((noreturn)) void SystemNative_Abort(void);

PALEXPORT uint64_t SystemNative_GetUInt64OSThreadId(void);
PALEXPORT uint32_t SystemNative_TryGetUInt32OSThreadId(void);

PALEXPORT int32_t SystemNative_PThreadMutex_Init(void* mutex);

PALEXPORT int32_t SystemNative_PThreadMutex_Acquire(void* mutex, int32_t timeoutMilliseconds);

PALEXPORT int32_t SystemNative_PThreadMutex_Release(void* mutex);

PALEXPORT int32_t SystemNative_PThreadMutex_Destroy(void* mutex);

PALEXPORT int32_t SystemNative_PThreadMutex_Size(void);

PALEXPORT int32_t SystemNative_LowLevelCrossPlatformMutex_Size(void);
PALEXPORT int32_t SystemNative_LowLevelCrossPlatformMutex_Init(LowLevelCrossProcessMutex* mutex);
PALEXPORT int32_t SystemNative_LowLevelCrossPlatformMutex_Acquire(LowLevelCrossProcessMutex* mutex, int32_t timeoutMilliseconds);
PALEXPORT int32_t SystemNative_LowLevelCrossPlatformMutex_Release(LowLevelCrossProcessMutex* mutex);
PALEXPORT int32_t SystemNative_LowLevelCrossPlatformMutex_Destroy(LowLevelCrossProcessMutex* mutex);
PALEXPORT void SystemNative_LowLevelCrossPlatformMutex_GetOwnerProcessAndThreadId(LowLevelCrossProcessMutex* mutex, uint32_t* pOwnerProcessId, uint32_t* pOwnerThreadId);
PALEXPORT void SystemNative_LowLevelCrossPlatformMutex_SetOwnerProcessAndThreadId(LowLevelCrossProcessMutex* mutex, uint32_t ownerProcessId, uint32_t ownerThreadId);
PALEXPORT uint8_t SystemNative_LowLevelCrossPlatformMutex_IsAbandoned(LowLevelCrossProcessMutex* mutex);
PALEXPORT void SystemNative_LowLevelCrossPlatformMutex_SetAbandoned(LowLevelCrossProcessMutex* mutex, uint8_t isAbandoned);
