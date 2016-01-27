; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
;

;
; ==--==
; ***********************************************************************
; File: JitHelpers_InlineGetAppDomain.asm, see history in jithelp.asm
;
; Notes: These routinues will be patched at runtime with the location in 
;        the TLS to find the AppDomain* and are the fastest implementation 
;        of their specific functionality.
; ***********************************************************************

include AsmMacros.inc
include asmconstants.inc

; Min amount of stack space that a nested function should allocate.
MIN_SIZE equ 28h

; Macro to create a patchable inline GetAppdomain, if we decide to create patchable
; high TLS inline versions then just change this macro to make sure to create enough
; space in the asm to patch the high TLS getter instructions.
PATCHABLE_INLINE_GETAPPDOMAIN macro Reg, PatchLabel
PATCH_LABEL PatchLabel
        mov     Reg, gs:[OFFSET__TEB__TlsSlots]
        endm

extern JIT_GetSharedNonGCStaticBase_Helper:proc
extern JIT_GetSharedGCStaticBase_Helper:proc

LEAF_ENTRY JIT_GetSharedNonGCStaticBase_InlineGetAppDomain, _TEXT
        ; Check if rcx (moduleDomainID) is not a moduleID
        mov     rax, rcx
        test    rax, 1
        jz      HaveLocalModule

        PATCHABLE_INLINE_GETAPPDOMAIN rax, JIT_GetSharedNonGCStaticBase__PatchTLSLabel

        ; Get the LocalModule, rcx will always be odd, so: rcx * 4 - 4 <=> (rcx >> 1) * 8
        mov     rax, [rax + OFFSETOF__AppDomain__m_sDomainLocalBlock + OFFSETOF__DomainLocalBlock__m_pModuleSlots]
        mov     rax, [rax + rcx * 4 - 4]

    HaveLocalModule:
        ; If class is not initialized, bail to C++ helper
        test    byte ptr [rax + OFFSETOF__DomainLocalModule__m_pDataBlob + rdx], 1
        jz      CallHelper
        REPRET

    align 16
    CallHelper:
        ; Tail call JIT_GetSharedNonGCStaticBase_Helper
        mov     rcx, rax
        jmp     JIT_GetSharedNonGCStaticBase_Helper
LEAF_END JIT_GetSharedNonGCStaticBase_InlineGetAppDomain, _TEXT

LEAF_ENTRY JIT_GetSharedNonGCStaticBaseNoCtor_InlineGetAppDomain, _TEXT
        ; Check if rcx (moduleDomainID) is not a moduleID
        mov     rax, rcx
        test    rax, 1
        jz      HaveLocalModule

        PATCHABLE_INLINE_GETAPPDOMAIN rax, JIT_GetSharedNonGCStaticBaseNoCtor__PatchTLSLabel

        ; Get the LocalModule,  rcx will always be odd, so: rcx * 4 - 4 <=> (rcx >> 1) * 8
        mov     rax, [rax + OFFSETOF__AppDomain__m_sDomainLocalBlock + OFFSETOF__DomainLocalBlock__m_pModuleSlots]
        mov     rax, [rax + rcx * 4 - 4]
        ret

    align 16
    HaveLocalModule:
        REPRET
LEAF_END JIT_GetSharedNonGCStaticBaseNoCtor_InlineGetAppDomain, _TEXT

LEAF_ENTRY JIT_GetSharedGCStaticBase_InlineGetAppDomain, _TEXT
        ; Check if rcx (moduleDomainID) is not a moduleID
        mov     rax, rcx
        test    rax, 1
        jz      HaveLocalModule

        PATCHABLE_INLINE_GETAPPDOMAIN rax, JIT_GetSharedGCStaticBase__PatchTLSLabel

        ; Get the LocalModule, rcx will always be odd, so: rcx * 4 - 4 <=> (rcx >> 1) * 8
        mov     rax, [rax + OFFSETOF__AppDomain__m_sDomainLocalBlock + OFFSETOF__DomainLocalBlock__m_pModuleSlots]
        mov     rax, [rax + rcx * 4 - 4]

    HaveLocalModule:
        ; If class is not initialized, bail to C++ helper
        test    byte ptr [rax + OFFSETOF__DomainLocalModule__m_pDataBlob + rdx], 1
        jz      CallHelper

        mov     rax, [rax + OFFSETOF__DomainLocalModule__m_pGCStatics]
        ret

    align 16
    CallHelper:
        ; Tail call Jit_GetSharedGCStaticBase_Helper
        mov     rcx, rax
        jmp     JIT_GetSharedGCStaticBase_Helper
LEAF_END JIT_GetSharedGCStaticBase_InlineGetAppDomain, _TEXT

LEAF_ENTRY JIT_GetSharedGCStaticBaseNoCtor_InlineGetAppDomain, _TEXT
        ; Check if rcx (moduleDomainID) is not a moduleID
        mov     rax, rcx
        test    rax, 1
        jz      HaveLocalModule

        PATCHABLE_INLINE_GETAPPDOMAIN rax, JIT_GetSharedGCStaticBaseNoCtor__PatchTLSLabel

        ; Get the LocalModule, rcx will always be odd, so: rcx * 4 - 4 <=> (rcx >> 1) * 8
        mov     rax, [rax + OFFSETOF__AppDomain__m_sDomainLocalBlock + OFFSETOF__DomainLocalBlock__m_pModuleSlots]
        mov     rax, [rax + rcx * 4 - 4]

    HaveLocalModule:
        mov     rax, [rax + OFFSETOF__DomainLocalModule__m_pGCStatics]
        ret
LEAF_END JIT_GetSharedGCStaticBaseNoCtor_InlineGetAppDomain, _TEXT

        end

