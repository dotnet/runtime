// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

typedef struct LowLevelCrossProcessMutex LowLevelCrossProcessMutex;

PALEXPORT int32_t SystemNative_LowLevelCrossProcessMutex_Size(void);

PALEXPORT int32_t SystemNative_LowLevelCrossProcessMutex_Init(LowLevelCrossProcessMutex* mutex);

PALEXPORT int32_t SystemNative_LowLevelCrossProcessMutex_Acquire(LowLevelCrossProcessMutex* mutex, int32_t timeoutMilliseconds);

PALEXPORT int32_t SystemNative_LowLevelCrossProcessMutex_Release(LowLevelCrossProcessMutex* mutex);

PALEXPORT int32_t SystemNative_LowLevelCrossProcessMutex_Destroy(LowLevelCrossProcessMutex* mutex);

PALEXPORT void SystemNative_LowLevelCrossProcessMutex_GetOwnerProcessAndThreadId(LowLevelCrossProcessMutex* mutex, uint32_t* pOwnerProcessId, uint32_t* pOwnerThreadId);

PALEXPORT void SystemNative_LowLevelCrossProcessMutex_SetOwnerProcessAndThreadId(LowLevelCrossProcessMutex* mutex, uint32_t ownerProcessId, uint32_t ownerThreadId);

PALEXPORT uint8_t SystemNative_LowLevelCrossProcessMutex_IsAbandoned(LowLevelCrossProcessMutex* mutex);

PALEXPORT void SystemNative_LowLevelCrossProcessMutex_SetAbandoned(LowLevelCrossProcessMutex* mutex, uint8_t isAbandoned);
