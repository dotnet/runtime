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

PALEXPORT void SystemNative_LowLevelFutex_WaitOnAddress(int32_t* address, int32_t comparand);

PALEXPORT int32_t SystemNative_LowLevelFutex_WaitOnAddressTimeout(int32_t* address, int32_t comparand, int32_t timeoutMilliseconds);

PALEXPORT void SystemNative_LowLevelFutex_WakeByAddressSingle(int32_t* address);

PALEXPORT int32_t SystemNative_CreateThread(uintptr_t stackSize, void *(*startAddress)(void*), void *parameter);

PALEXPORT int32_t SystemNative_SchedGetCpu(void);

PALEXPORT __attribute__((noreturn)) void SystemNative_Exit(int32_t exitCode);

PALEXPORT __attribute__((noreturn)) void SystemNative_Abort(void);

PALEXPORT uint64_t SystemNative_GetCurrentThreadId(void);

// Sends a signal to a thread of a process. Only supported on Linux; other platforms fail with ENOSYS.
// Returns 0 on success and -1 with errno set on failure. Signal 0 only checks whether the thread exists.
PALEXPORT int32_t SystemNative_TgKill(int32_t pid, int32_t tid, int32_t signal);
