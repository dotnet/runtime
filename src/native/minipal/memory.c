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
void* minipal_co_task_mem_alloc(size_t cb)
{
    return malloc(cb);
}

void minipal_co_task_mem_free(void* pv)
{
    free(pv);
}
#endif
