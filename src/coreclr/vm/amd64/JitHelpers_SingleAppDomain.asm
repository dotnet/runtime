; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: JitHelpers_SingleAppDomain.asm
; ***********************************************************************

include AsmMacros.inc
include asmconstants.inc

; Min amount of stack space that a nested function should allocate.
MIN_SIZE equ 28h

extern JIT_GetDynamicNonGCStaticBase_Portable:proc
extern JIT_GetDynamicGCStaticBase_Portable:proc

LEAF_ENTRY JIT_GetDynamicNonGCStaticBase_SingleAppDomain, _TEXT
        ; If class is not initialized, bail to C++ helper
        test    byte ptr [rcx + OFFSETOF__DynamicStaticsInfo__m_AuxData__m_dwFlags], 1
        jz      CallHelper
        mov     rax, [rcx + OFFSETOF__DynamicStaticsInfo__m_pNonGCStatics]
        REPRET

    align 16
    CallHelper:
        ; Tail call JIT_GetDynamicNonGCStaticBase_Portable
        jmp     JIT_GetDynamicNonGCStaticBase_Portable
LEAF_END JIT_GetDynamicNonGCStaticBase_SingleAppDomain, _TEXT

LEAF_ENTRY JIT_GetDynamicGCStaticBase_SingleAppDomain, _TEXT
        ; If class is not initialized, bail to C++ helper
        test    byte ptr [rcx + OFFSETOF__DynamicStaticsInfo__m_AuxData__m_dwFlags], 1
        jz      CallHelper
        mov     rax, [rcx + OFFSETOF__DynamicStaticsInfo__m_pGCStatics]
        REPRET

    align 16
    CallHelper:
        ; Tail call JIT_GetDynamicGCStaticBase_Portable
        jmp     JIT_GetDynamicGCStaticBase_Portable
LEAF_END JIT_GetDynamicGCStaticBase_SingleAppDomain, _TEXT

        end

