// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "memory.h"

#ifdef HOST_WINDOWS
#include <Windows.h>

void* minipal_co_task_mem_alloc(size_t cb)
{
    return CoTaskMemAlloc(cb);
}

void minipal_co_task_mem_free(void* pv)
{
    CoTaskMemFree(pv);
}
#else
// CoTaskMemAlloc always aligns on an 8-byte boundary.
#define ALIGN 8

void* minipal_co_task_mem_alloc(size_t cb)
{
    // Ensure malloc always allocates.
    if (cb == 0)
        cb = ALIGN;

    // Align the allocation size.
    size_t cb_safe = (cb + (ALIGN - 1)) & ~(ALIGN - 1);
    if (cb_safe < cb) // Overflow
        return NULL;

    return aligned_alloc(ALIGN, cb_safe);
}

void minipal_co_task_mem_free(void* pv)
{
    free(pv);
}
#endif
