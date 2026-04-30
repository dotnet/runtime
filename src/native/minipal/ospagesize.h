// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_OSPAGESIZE_H
#define HAVE_MINIPAL_OSPAGESIZE_H

#ifdef __cplusplus
extern "C" {
#endif

// Returns the OS page size in bytes.
//
// On platforms where the page size is fixed (Windows: 4KB, WASM: 16KB) this is
// defined inline so callers see a compile-time constant. This matters for the GC,
// which expects GetPageSize to fold into a constant for alignment math.
//
// On other platforms the value is queried from the OS once and cached; the
// definition lives in ospagesize.c so there is exactly one cache per process.
#if defined(__wasm__)
static inline int minipal_getpagesize(void)
{
    // The OS page size used by CoreCLR on WASM (16KB).
    // WASM has no hardware pages; getpagesize() returns the 64KB memory.grow granularity,
    // which is too coarse for GC alignment and thresholds.
    return 16 * 1024;
}
#elif defined(HOST_WINDOWS) || defined(_WIN32)
static inline int minipal_getpagesize(void)
{
    // The page size on Windows is 4KB and is not going to change.
    return 4 * 1024;
}
#else
int minipal_getpagesize(void);
#endif

#ifdef __cplusplus
}
#endif

#endif // HAVE_MINIPAL_OSPAGESIZE_H
