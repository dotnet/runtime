// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

//
// JIT Static access helpers when coreclr host specifies single
// appdomain flag
//

LEAF_ENTRY JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT
        // If class is not initialized, bail to C++ helper
        test    byte ptr [rdi + OFFSETOF__DomainLocalModule__m_pDataBlob + rsi], 1
        jz      CallHelper
        mov     rax, rdi
        rep ret

.balign 16
CallHelper:
        // Tail call JIT_GetSharedNonGCStaticBase_Helper
        jmp     C_FUNC(JIT_GetSharedNonGCStaticBase_Helper)
LEAF_END_MARKED JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT

LEAF_ENTRY JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT
        mov     rax, rdi
        ret
LEAF_END JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT

LEAF_ENTRY JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT
        // If class is not initialized, bail to C++ helper
        test    byte ptr [rdi + OFFSETOF__DomainLocalModule__m_pDataBlob + rsi], 1
        jz      CallHelper1

        mov     rax, [rdi + OFFSETOF__DomainLocalModule__m_pGCStatics]
        rep ret

.balign 16
CallHelper1:
        // Tail call Jit_GetSharedGCStaticBase_Helper
        jmp     C_FUNC(JIT_GetSharedGCStaticBase_Helper)
LEAF_END JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT

LEAF_ENTRY JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT
        mov     rax, [rdi + OFFSETOF__DomainLocalModule__m_pGCStatics]
        ret
LEAF_END JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT
