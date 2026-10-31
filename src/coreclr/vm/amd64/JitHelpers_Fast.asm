; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: JitHelpers_Fast.asm
;
; Notes: routinues which we believe to be on the hot path for managed
;        code in most scenarios.
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
EXTERN  g_region_shr:BYTE
EXTERN  g_region_use_bitwise_write_barrier:BYTE
EXTERN  g_region_to_generation_table:QWORD

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
extern JIT_WriteBarrier_Debug:proc
endif


Section segment para 'DATA'

        align   16

        public  JIT_WriteBarrier_Loc
JIT_WriteBarrier_Loc:
        dq 0

; There is an even more optimized version of these helpers possible which takes
; advantage of knowledge of which way the ephemeral heap is growing to only do 1/2
; that check (this is more significant in the JIT_WriteBarrier case).
;
; Additionally we can look into providing helpers which will take the src/dest from
; specific registers (like x86) which _could_ (??) make for easier register allocation
; for the JIT64, however it might lead to having to have some nasty code that treats
; these guys really special like... :(.
;
; Version that does the move, checks whether or not it's in the GC and whether or not
; it needs to have it's card updated
;
; void JIT_CheckedWriteBarrier(Object** dst, Object* src)
LEAF_ENTRY JIT_CheckedWriteBarrier, _TEXT

        ; When WRITE_BARRIER_CHECK is defined _NotInHeap will write the reference
        ; but if it isn't then it will just return.
        ;
        ; See if this is in GCHeap
        cmp     rcx, [g_lowest_address]
        jb      NotInHeap
        cmp     rcx, [g_highest_address]
        jnb     NotInHeap

        jmp     QWORD PTR [JIT_WriteBarrier_Loc]

    NotInHeap:
        ; See comment above about possible AV
        mov     [rcx], rdx
        ret
LEAF_END_MARKED JIT_CheckedWriteBarrier, _TEXT

LEAF_ENTRY JIT_ValidateIndirectCall, _TEXT
        ret
LEAF_END JIT_ValidateIndirectCall, _TEXT

LEAF_ENTRY JIT_DispatchIndirectCall, _TEXT
ifdef _DEBUG
        mov r10, 0CDCDCDCDCDCDCDCDh ; The real helper clobbers these registers, so clobber them too in the fake helper
        mov r11, 0CDCDCDCDCDCDCDCDh
endif
        rexw jmp rax
LEAF_END JIT_DispatchIndirectCall, _TEXT


        end
