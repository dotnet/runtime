// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"

void InitializeMemoryDebug(void);

PALEXPORT void CryptoNative_EnableMemoryTracking(int32_t enable);

PALEXPORT void CryptoNative_ForEachTrackedAllocation(void (*callback)(void* ptr, uint64_t size, const char* file, int32_t line, void* ctx), void* ctx);

PALEXPORT void CryptoNative_GetMemoryUse(uint64_t* totalUsed, uint64_t* allocationCount);
