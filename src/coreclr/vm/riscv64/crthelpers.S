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
    beq  a2, zero, LOCAL_LABEL(JIT_MemSet_ret)

    lb  zero, 0(a0) // Is this really needed ?

    tail memset

LOCAL_LABEL(JIT_MemSet_ret):
    ret
LEAF_END_MARKED JIT_MemSet, _TEXT

////NOTE: Here must use LEAF_END_MARKED!  not LEAF_END !!!
LEAF_ENTRY JIT_MemCpy, _TEXT
    beq  a2, zero, LOCAL_LABEL(JIT_MemCpy_ret)

    lb  zero, 0(a0)
    lb  zero, 0(a1) // Is this really needed ?

    tail memcpy 

LOCAL_LABEL(JIT_MemCpy_ret):
    ret

////NOTE: Here must use LEAF_END_MARKED!  not LEAF_END !!!
LEAF_END_MARKED JIT_MemCpy, _TEXT
