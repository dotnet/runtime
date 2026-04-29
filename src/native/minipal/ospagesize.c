// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/ospagesize.h>

#ifdef __wasm__
// The OS page size used by CoreCLR on WASM (16KB).
// WASM has no hardware pages; getpagesize() returns the 64KB memory.grow granularity,
// which is too coarse for GC alignment and thresholds.
int minipal_getpagesize(void)
{
    return 16 * 1024;
}
#elif HOST_WINDOWS
int minipal_getpagesize(void)
{
    // The page size on Windows is 4KB and is not going to change.
    return 4 * 1024;
}
#else
#include <unistd.h>
int minipal_getpagesize(void)
{
    // Use volatile to prevent the compiler from reordering loads/stores of the cache,
    // which could cause callers to observe a zero value after another thread initialized it.
    static volatile int cached_page_size = 0;
    int page_size = cached_page_size;
    if (page_size == 0)
    {
        page_size = getpagesize();
        cached_page_size = page_size;
    }
    return page_size;
}
#endif
