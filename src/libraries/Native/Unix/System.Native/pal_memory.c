// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_memory.h"

#include <stdlib.h>
#include <string.h>

void* SystemNative_MemAlloc(uintptr_t size)
{
    return malloc(size);
}

void* SystemNative_MemReAlloc(void* ptr, uintptr_t size)
{
    return realloc(ptr, size);
}

void SystemNative_MemFree(void* ptr)
{
    free(ptr);
}

void* SystemNative_MemSet(void* s, int c, uintptr_t n)
{
    return memset(s, c, n);
}
