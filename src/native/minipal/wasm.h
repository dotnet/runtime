// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_WASM_H
#define HAVE_MINIPAL_WASM_H

#ifndef __wasm__
#include <unistd.h>
#endif

#ifdef __cplusplus
extern "C" {
#endif

static inline int minipal_getpagesize(void)
{
#ifdef __wasm__
    // The OS page size used by CoreCLR on WASM (16KB).
    // WASM has no hardware pages; getpagesize() returns the 64KB memory.grow granularity,
    // which is too coarse for GC alignment and thresholds.
    return 16 * 1024;
#else
    static int cached_page_size = 0;
    if (cached_page_size == 0)
        cached_page_size = getpagesize();
    return cached_page_size;
#endif
}

#ifdef __cplusplus
} // extern "C"
#endif

#endif // HAVE_MINIPAL_WASM_H
