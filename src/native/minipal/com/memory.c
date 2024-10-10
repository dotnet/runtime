// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <stdlib.h>
#include "minipal_com.h"

// CoTaskMemAlloc always aligns on an 8-byte boundary.
#define ALIGN 8

LPVOID PAL_CoTaskMemAlloc(SIZE_T cb)
{
    // Ensure malloc always allocates.
    if (cb == 0)
        cb = ALIGN;

    // Align the allocation size.
    SIZE_T cb_safe = (cb + (ALIGN - 1)) & ~(ALIGN - 1);
    if (cb_safe < cb) // Overflow
        return NULL;

    return aligned_alloc(ALIGN, cb_safe);
}

void PAL_CoTaskMemFree(LPVOID pv)
{
    free(pv);
}
