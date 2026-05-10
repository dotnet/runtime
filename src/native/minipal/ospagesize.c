// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// POSIX implementation of minipal_getpagesize. On WASM and Windows the page size
// is a compile-time constant and minipal_getpagesize is defined inline in the
// header; this file is excluded from the build on those platforms by
// src/native/minipal/CMakeLists.txt to avoid an empty translation unit.

#include <unistd.h>
#include "ospagesize.h"

size_t minipal_getpagesize(void)
{
    // Process-wide constant. Any thread that races to initialize the cache writes
    // the same value, so no synchronization is required.
    static size_t cached_page_size = 0;
    size_t page_size = cached_page_size;
    if (page_size == 0)
    {
        page_size = (size_t)getpagesize();
        cached_page_size = page_size;
    }
    return page_size;
}
