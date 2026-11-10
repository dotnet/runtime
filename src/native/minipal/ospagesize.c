// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ospagesize.h"

#if defined(HOST_WASM) || defined(HOST_WINDOWS)

extern uint32_t minipal_getpagesize(void);

#else

#include <unistd.h>
#include <stdlib.h>
#include <stdatomic.h>

uint32_t minipal_getpagesize(void)
{
#if defined(TARGET_HAIKU) && defined(__clang__)
    static _Atomic uint32_t cached_page_size = 0;
    uint32_t page_size = __c11_atomic_load(&cached_page_size, memory_order_relaxed);
#else
    static atomic_uint cached_page_size = 0;
    uint32_t page_size = atomic_load_explicit(&cached_page_size, memory_order_relaxed);
#endif
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
#if defined(TARGET_HAIKU) && defined(__clang__)
        __c11_atomic_store(&cached_page_size, page_size, memory_order_relaxed);
#else
        atomic_store_explicit(&cached_page_size, page_size, memory_order_relaxed);
#endif
    }
    return page_size;
}

#endif // HOST_WASM || HOST_WINDOWS
