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
    beq $a2, $zero, LOCAL_LABEL(JIT_MemSet_ret)

    ld.b  $zero, $a0, 0     //Is this really needed ?

    b  memset

LOCAL_LABEL(JIT_MemSet_ret):
    jirl  $r0, $ra, 0

////NOTO: Here must use LEAF_END_MARKED!  not LEAF_END !!!
LEAF_END_MARKED JIT_MemSet, _TEXT

LEAF_ENTRY JIT_MemCpy, _TEXT
    beq $a2, $zero, LOCAL_LABEL(JIT_MemCpy_ret)

    ld.b  $zero, $a0, 0
    ld.b  $zero, $a1, 0    //Is this really needed ?

    b  memcpy

LOCAL_LABEL(JIT_MemCpy_ret):
    jirl  $r0, $ra, 0

////NOTO: Here must use LEAF_END_MARKED!  not LEAF_END !!!
LEAF_END_MARKED JIT_MemCpy, _TEXT
