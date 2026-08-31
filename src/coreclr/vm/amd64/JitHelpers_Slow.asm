; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: JitHelpers_Slow.asm
;
; Notes: These are ASM routinues which we believe to be cold in normal
;        AMD64 scenarios, mainly because they have other versions which
;        have some more performant nature which will be used in the best
;        cases.
; ***********************************************************************

include AsmMacros.inc
include asmconstants.inc

; Min amount of stack space that a nested function should allocate.
MIN_SIZE equ 28h

EXTERN  g_ephemeral_low:QWORD
EXTERN  g_ephemeral_high:QWORD
EXTERN  g_lowest_address:QWORD
EXTERN  g_highest_address:QWORD
EXTERN  g_card_table:QWORD

ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN g_card_bundle_table:QWORD
endif

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
EXTERN  g_write_watch_table:QWORD
EXTERN  g_sw_ww_enabled_for_gc_heap:BYTE
endif

ifdef WRITE_BARRIER_CHECK
; Those global variables are always defined, but should be 0 for Server GC
g_GCShadow                      TEXTEQU <?g_GCShadow@@3PEAEEA>
g_GCShadowEnd                   TEXTEQU <?g_GCShadowEnd@@3PEAEEA>
EXTERN  g_GCShadow:QWORD
EXTERN  g_GCShadowEnd:QWORD
endif

INVALIDGCVALUE          equ     0CCCCCCCDh

ifdef _DEBUG
; Version for when we're sure to be in the GC, checks whether or not the card
; needs to be updated
;
; void JIT_WriteBarrier_Debug(Object** dst, Object* src)
LEAF_ENTRY JIT_WriteBarrier_Debug, _TEXT

ifdef WRITE_BARRIER_CHECK
        ; **ALSO update the shadow GC heap if that is enabled**
        ; Do not perform the work if g_GCShadow is 0
        cmp     g_GCShadow, 0
        je      NoShadow

        ; If we end up outside of the heap don't corrupt random memory
        mov     r10, rcx
        sub     r10, [g_lowest_address]
        jb      NoShadow

        ; Check that our adjusted destination is somewhere in the shadow gc
        add     r10, [g_GCShadow]
        cmp     r10, [g_GCShadowEnd]
        jnb     NoShadow

        ; Write ref into real GC; see comment below about possibility of AV
        mov     [rcx], rdx
        ; Write ref into shadow GC
        mov     [r10], rdx

        ; Ensure that the write to the shadow heap occurs before the read from
        ; the GC heap so that race conditions are caught by INVALIDGCVALUE
        mfence

        ; Check that GC/ShadowGC values match
        mov     r11, [rcx]
        mov     rax, [r10]
        cmp     rax, r11
        je      DoneShadow
        mov     r11, INVALIDGCVALUE
        mov     [r10], r11

        jmp     DoneShadow

    ; If we don't have a shadow GC we won't have done the write yet
    NoShadow:
endif

        mov     rax, rdx

        ; Do the move. It is correct to possibly take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rax

ifdef WRITE_BARRIER_CHECK
    ; If we had a shadow GC then we already wrote to the real GC at the same time
    ; as the shadow GC so we want to jump over the real write immediately above
    DoneShadow:
endif

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        ; Update the write watch table if necessary
        cmp     byte ptr [g_sw_ww_enabled_for_gc_heap], 0h
        je      CheckCardTable
        mov     r10, rcx
        shr     r10, 0Ch ; SoftwareWriteWatch::AddressToTableByteIndexShift
        add     r10, qword ptr [g_write_watch_table]
        cmp     byte ptr [r10], 0h
        jne     CheckCardTable
        mov     byte ptr [r10], 0FFh
endif

    CheckCardTable:
        ; See if we can just quick out
        cmp     rax, [g_ephemeral_low]
        jb      Exit
        cmp     rax, [g_ephemeral_high]
        jnb     Exit

        ; Check if we need to update the card table
        ; Calc pCardByte
        shr     rcx, 0Bh
        add     rcx, [g_card_table]

        ; Check if this card is dirty
        cmp     byte ptr [rcx], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rcx], 0FFh
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        sub     rcx, [g_card_table]
        shr     rcx, 0Ah
        add     rcx, [g_card_bundle_table]
        cmp     byte ptr [rcx], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [rcx], 0FFh
endif
        ret

    align 16
    Exit:
        REPRET
LEAF_END_MARKED JIT_WriteBarrier_Debug, _TEXT
endif

        end

