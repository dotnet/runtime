; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: JitHelpers_SingleAppDomain.asm
;
; Notes: JIT Static access helpers when coreclr host specifies single
;        appdomain flag
; ***********************************************************************

include AsmMacros.inc
include asmconstants.inc

; Min amount of stack space that a nested function should allocate.
MIN_SIZE equ 28h

extern JIT_GetSharedNonGCStaticBase_Helper:proc
extern JIT_GetSharedGCStaticBase_Helper:proc

LEAF_ENTRY JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT
        ; If class is not initialized, bail to C++ helper
        test    byte ptr [rcx + OFFSETOF__DomainLocalModule__m_pDataBlob + rdx], 1
        jz      CallHelper
        mov     rax, rcx
        REPRET

    align 16
    CallHelper:
        ; Tail call JIT_GetSharedNonGCStaticBase_Helper
        jmp     JIT_GetSharedNonGCStaticBase_Helper
LEAF_END JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT

LEAF_ENTRY JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT
        mov     rax, rcx
        ret
LEAF_END JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT

LEAF_ENTRY JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT
        ; If class is not initialized, bail to C++ helper
        test    byte ptr [rcx + OFFSETOF__DomainLocalModule__m_pDataBlob + rdx], 1
        jz      CallHelper

        mov     rax, [rcx + OFFSETOF__DomainLocalModule__m_pGCStatics]
        REPRET

    align 16
    CallHelper:
        ; Tail call Jit_GetSharedGCStaticBase_Helper
        jmp     JIT_GetSharedGCStaticBase_Helper
LEAF_END JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT

LEAF_ENTRY JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT
        mov     rax, [rcx + OFFSETOF__DomainLocalModule__m_pGCStatics]
        ret
LEAF_END JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT

        end

