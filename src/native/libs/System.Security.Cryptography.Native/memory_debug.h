// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"

void InitializeMemoryDebug(void);

typedef enum
{
    MallocOperation = 1,
    ReallocOperation = 2,
    FreeOperation = 3,
} MemoryOperation;

typedef void (*CRYPTO_allocation_cb)(MemoryOperation operation, void* ptr, void* oldPtr, int size, const char *file, int line);

PALEXPORT int32_t CryptoNative_SetMemoryTracking(CRYPTO_allocation_cb callback);

PALEXPORT int32_t CryptoNative_GetMemoryUse(int* totalUsed, int* allocationCount);
