// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef MINIPAL_WASM_H
#define MINIPAL_WASM_H

// Cross-platform page size accessor
#ifdef __cplusplus
inline
#else
static inline
#endif
int minipal_getpagesize(void)
{
#ifdef __wasm__
    // The OS page size used by CoreCLR on WASM (16KB).
    // WASM has no hardware pages; getpagesize() returns the 64KB memory.grow granularity,
    // which is too coarse for GC alignment and thresholds.
    return 16 * 1024;
#else
    return getpagesize();
#endif
}

#endif // MINIPAL_WASM_H
