// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_memory.h"

#include <assert.h>
#include <stdlib.h>
#include <string.h>

#if HAVE_MALLOC_SIZE
    #include <malloc/malloc.h>
    #define MALLOC_SIZE(s) malloc_size(s)
#elif HAVE_MALLOC_USABLE_SIZE
    #include <malloc.h>
    #define MALLOC_SIZE(s) malloc_usable_size(s)
#elif HAVE_MALLOC_USABLE_SIZE_NP
    #include <malloc_np.h>
    #define MALLOC_SIZE(s) malloc_usable_size(s)
#elif defined(TARGET_SUNOS)
    #define MALLOC_SIZE(s) (*((size_t*)(s)-1))
#else
    #error "Platform doesn't support malloc_usable_size or malloc_size"
#endif

void* SystemNative_AlignedAlloc(uintptr_t alignment, uintptr_t size)
{
#if HAVE_ALIGNED_ALLOC
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

void* SystemNative_AlignedRealloc(void* ptr, uintptr_t alignment, uintptr_t new_size)
{
    void* result = SystemNative_AlignedAlloc(alignment, new_size);

    if (result != NULL)
    {
        uintptr_t old_size = MALLOC_SIZE(ptr);
        assert((ptr != NULL) || (old_size == 0));

        memcpy(result, ptr, (new_size < old_size) ? new_size : old_size);
        SystemNative_AlignedFree(ptr);
    }

    return result;
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
