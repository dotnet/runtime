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
        test    byte ptr [rdi + OFFSETOF__DynamicStaticsInfo__m_AuxData__m_dwFlags], 1
        jz      CallHelper
        mov     rax, [rdi + OFFSETOF__DynamicStaticsInfo__m_pNonGCStatics]
        rep ret

.balign 16
CallHelper:
        // Tail call JIT_GetSharedNonGCStaticBase_Helper
        jmp     C_FUNC(JIT_GetDynamicNonGCStaticBase_Portable)
LEAF_END_MARKED JIT_GetDynamicNonGCStaticBase_SingleAppDomain, _TEXT

LEAF_ENTRY JIT_GetDynamicGCStaticBase_SingleAppDomain, _TEXT
        // If class is not initialized, bail to C++ helper
        test    byte ptr [rdi + OFFSETOF__DynamicStaticsInfo__m_AuxData__m_dwFlags], 1
        jz      CallHelper1

        mov     rax, [rdi + OFFSETOF__DynamicStaticsInfo__m_pGCStatics]
        rep ret

.balign 16
CallHelper1:
        // Tail call Jit_GetSharedGCStaticBase_Helper
        jmp     C_FUNC(JIT_GetDynamicGCStaticBase_Portable)
LEAF_END JIT_GetDynamicGCStaticBase_SingleAppDomain, _TEXT

