// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"

void InitializeMemoryDebug(void);

PALEXPORT void CryptoNative_EnableMemoryTracking(int32_t enable);

PALEXPORT void CryptoNative_ForEachTrackedAllocation(void (*callback)(void* ptr, int size, const char* file, int line, void* ctx), void* ctx);

PALEXPORT int32_t CryptoNative_GetMemoryUse(int* totalUsed, int* allocationCount);
