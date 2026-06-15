// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// POSIX implementation of minipal_getpagesize. On WASM and Windows the page size
// is a compile-time constant and minipal_getpagesize is defined inline in the
// header; this file is excluded from the build on those platforms by
// src/native/minipal/CMakeLists.txt to avoid an empty translation unit.

#include <unistd.h>
#include <stdlib.h>
#include <stdatomic.h>
#include "ospagesize.h"

uint32_t minipal_getpagesize(void)
{
    static atomic_uint cached_page_size = 0;
    uint32_t page_size = atomic_load_explicit(&cached_page_size, memory_order_relaxed);
    if (page_size == 0)
    {
        long sc = sysconf(_SC_PAGESIZE);
        // _SC_PAGESIZE is mandatory in POSIX 2001; treat any failure as fatal
        // rather than caching a nonsense value (e.g. (uint32_t)-1).
        if (sc <= 0)
        {
            abort();
        }
        page_size = (uint32_t)sc;
        atomic_store_explicit(&cached_page_size, page_size, memory_order_relaxed);
    }
    return page_size;
}
