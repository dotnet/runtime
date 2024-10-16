// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: comem.cpp
//
// ===========================================================================

#include "common.h"
#include <minipal/memory.h>

STDAPI_(LPVOID) CoTaskMemAlloc(SIZE_T cb)
{
    return minipal_co_task_mem_alloc(cb);
}

STDAPI_(void) CoTaskMemFree(LPVOID pv)
{
    minipal_co_task_mem_free(pv);
}
