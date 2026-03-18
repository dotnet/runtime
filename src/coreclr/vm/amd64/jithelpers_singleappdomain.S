// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// JIT Static access helpers when coreclr host specifies single
// appdomain flag
//

LEAF_ENTRY JIT_GetDynamicNonGCStaticBase_SingleAppDomain, _TEXT
        // If class is not initialized, bail to C++ helper
        mov     rax, [rdi + OFFSETOF__DynamicStaticsInfo__m_pNonGCStatics]
        test    al, 1
        jnz     CallHelper
        rep ret

.balign 16
CallHelper:
        // Tail call managed GetSharedNonGCStaticBase helper
        mov     rdi, [rdi + OFFSETOF__DynamicStaticsInfo__m_pMethodTable]
        PREPARE_EXTERNAL_VAR g_pGetNonGCStaticBase, r10
        jmp     [r10]
LEAF_END_MARKED JIT_GetDynamicNonGCStaticBase_SingleAppDomain, _TEXT

LEAF_ENTRY JIT_GetDynamicGCStaticBase_SingleAppDomain, _TEXT
        // If class is not initialized, bail to C++ helper
        mov     rax, [rdi + OFFSETOF__DynamicStaticsInfo__m_pGCStatics]
        test    al, 1
        jnz     CallHelper1
        rep ret

.balign 16
CallHelper1:
        // Tail call managed GetSharedGCStaticBase helper
        mov     rdi, [rdi + OFFSETOF__DynamicStaticsInfo__m_pMethodTable]
        PREPARE_EXTERNAL_VAR g_pGetGCStaticBase, r10
        jmp     [r10]
LEAF_END JIT_GetDynamicGCStaticBase_SingleAppDomain, _TEXT

