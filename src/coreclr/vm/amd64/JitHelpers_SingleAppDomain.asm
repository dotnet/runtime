; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: JitHelpers_SingleAppDomain.asm
; ***********************************************************************

include AsmMacros.inc
include asmconstants.inc

; Min amount of stack space that a nested function should allocate.
MIN_SIZE equ 28h

EXTERN  g_pGetGCStaticBase:QWORD
EXTERN  g_pGetNonGCStaticBase:QWORD

LEAF_ENTRY JIT_GetDynamicNonGCStaticBase_SingleAppDomain, _TEXT
        ; If class is not initialized, bail to C++ helper
        mov     rax, [rcx + OFFSETOF__DynamicStaticsInfo__m_pNonGCStatics]
        test    al, 1
        jnz     CallHelper
        REPRET

    align 16
    CallHelper:
        mov     rcx, [rcx + OFFSETOF__DynamicStaticsInfo__m_pMethodTable]
        mov     rax, g_pGetNonGCStaticBase
        TAILJMP_RAX
LEAF_END JIT_GetDynamicNonGCStaticBase_SingleAppDomain, _TEXT

LEAF_ENTRY JIT_GetDynamicGCStaticBase_SingleAppDomain, _TEXT
        ; If class is not initialized, bail to C++ helper
        mov     rax,   [rcx + OFFSETOF__DynamicStaticsInfo__m_pGCStatics]
        test    al, 1
        jnz     CallHelper
        REPRET

    align 16
    CallHelper:
        mov     rcx, [rcx + OFFSETOF__DynamicStaticsInfo__m_pMethodTable]
        mov     rax, g_pGetGCStaticBase
        TAILJMP_RAX
LEAF_END JIT_GetDynamicGCStaticBase_SingleAppDomain, _TEXT

        end

