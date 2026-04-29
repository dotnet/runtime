// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_OSPAGESIZE_H
#define HAVE_MINIPAL_OSPAGESIZE_H

#if !defined(__wasm__) && !defined(HOST_WINDOWS) && !defined(_WIN32)
#include <unistd.h>
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Returns the OS page size in bytes.
//
// Defined inline so that callers see a compile-time constant on platforms where
// the page size is fixed (Windows: 4KB, WASM: 16KB). This matters for the GC,
// which expects GetPageSize to fold into a constant for alignment math.
static inline int minipal_getpagesize(void)
{
#ifdef __wasm__
    // The OS page size used by CoreCLR on WASM (16KB).
    // WASM has no hardware pages; getpagesize() returns the 64KB memory.grow granularity,
    // which is too coarse for GC alignment and thresholds.
    return 16 * 1024;
#elif defined(HOST_WINDOWS) || defined(_WIN32)
    // The page size on Windows is 4KB and is not going to change.
    return 4 * 1024;
#else
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
#endif
}

#ifdef __cplusplus
}
#endif

#endif // HAVE_MINIPAL_OSPAGESIZE_H
