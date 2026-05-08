// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_OSPAGESIZE_H
#define HAVE_MINIPAL_OSPAGESIZE_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

// Returns the OS page size in bytes.
//
// On WASM the page size is a compile-time constant (16KB) and is defined inline so
// callers see a constant. This matters for the GC, which expects GetPageSize to fold
// into a constant for alignment math.
//
// On other platforms the value is queried from the OS once and cached; the
// definition lives in ospagesize.c so there is exactly one cache per process.
#if defined(HOST_WASM)
static inline uint32_t minipal_getpagesize(void)
{
    // WASM has no hardware pages; getpagesize() returns the 64KB memory.grow granularity,
    // which is too coarse for GC alignment and thresholds. Reduce the OS page size used
    // by the runtime on WASM to 16KB.
    return 16 * 1024;
}
#else
uint32_t minipal_getpagesize(void);
#endif

#ifdef __cplusplus
}
#endif

#endif // HAVE_MINIPAL_OSPAGESIZE_H
