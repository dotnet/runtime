// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"

// JIT_MemSet/JIT_MemCpy
//
// It is IMPORTANT that the exception handling code is able to find these guys
// on the stack, but on non-windows platforms we can just defer to the platform
// implementation.
//
LEAF_ENTRY JIT_MemSet, _TEXT
    cbz x2, LOCAL_LABEL(JIT_MemSet_ret)

    ldrb wzr, [x0]

    b C_PLTFUNC(memset)

LOCAL_LABEL(JIT_MemSet_ret):
    ret         lr
LEAF_END_MARKED JIT_MemSet, _TEXT

LEAF_ENTRY JIT_MemCpy, _TEXT
    cbz x2, LOCAL_LABEL(JIT_MemCpy_ret)

    ldrb wzr, [x0]
    ldrb wzr, [x1]

    b C_PLTFUNC(memcpy)

LOCAL_LABEL(JIT_MemCpy_ret):
    ret         lr
LEAF_END_MARKED JIT_MemCpy, _TEXT
