// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_memory.h"

#include <stdlib.h>
#include <string.h>

void* SystemNative_AlignedAlloc(uintptr_t alignment, uintptr_t size)
{
#if HAVE_ALIGNED_ALLOC && !defined(__APPLE__)
    // We want to prefer the standardized aligned_alloc function. However
    // it cannot be used on __APPLE__ since we target 10.13 and it was
    // only added in 10.15, but we might be compiling on a 10.15 box.
    return aligned_alloc(alignment, size);
#elif HAVE_POSIX_MEMALIGN
    void* result = NULL;
    posix_memalign(&result, alignment, size);
    return result;
#else
    #error "Platform doesn't support aligned_alloc or posix_memalign"
#endif
}

void SystemNative_AlignedFree(void* ptr)
{
    free(ptr);
}

void* SystemNative_Calloc(uintptr_t num, uintptr_t size)
{
    return calloc(num, size);
}

void SystemNative_Free(void* ptr)
{
    free(ptr);
}

void* SystemNative_Malloc(uintptr_t size)
{
    return malloc(size);
}

void* SystemNative_MemSet(void* s, int c, uintptr_t n)
{
    return memset(s, c, n);
}

void* SystemNative_Realloc(void* ptr, uintptr_t new_size)
{
    return realloc(ptr, new_size);
}
