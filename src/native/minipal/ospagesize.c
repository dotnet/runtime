// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// On WASM and Windows the page size is a compile-time constant and minipal_getpagesize
// is defined inline in the header. This file provides the POSIX implementation, which
// must call getpagesize() once per process and cache the result.

#if !defined(__wasm__) && !defined(HOST_WINDOWS) && !defined(_WIN32)

#include <unistd.h>
#include "ospagesize.h"

int minipal_getpagesize(void)
{
    // Process-wide constant. Any thread that races to initialize the cache writes
    // the same value, so no synchronization is required.
    static int cached_page_size = 0;
    int page_size = cached_page_size;
    if (page_size == 0)
    {
        page_size = getpagesize();
        cached_page_size = page_size;
    }
    return page_size;
}

#endif
