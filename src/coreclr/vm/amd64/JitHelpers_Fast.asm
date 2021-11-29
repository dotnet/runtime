; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ==++==
;

;
; ==--==
; ***********************************************************************
; File: JitHelpers_Fast.asm, see jithelp.asm for history
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

ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN g_card_bundle_table:QWORD
endif

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
EXTERN  g_sw_ww_table:QWORD
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

extern JIT_InternalThrow:proc


; Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
        ret
LEAF_END JIT_PatchedCodeStart, _TEXT


; This is used by the mechanism to hold either the JIT_WriteBarrier_PreGrow
; or JIT_WriteBarrier_PostGrow code (depending on the state of the GC). It _WILL_
; change at runtime as the GC changes. Initially it should simply be a copy of the
; larger of the two functions (JIT_WriteBarrier_PostGrow) to ensure we have created
; enough space to copy that code in.
LEAF_ENTRY JIT_WriteBarrier, _TEXT
        align 16

ifdef _DEBUG
        ; In debug builds, this just contains jump to the debug version of the write barrier by default
        mov     rax, JIT_WriteBarrier_Debug
        jmp     rax
endif

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        ; JIT_WriteBarrier_WriteWatch_PostGrow64

        ; Regarding patchable constants:
        ; - 64-bit constants have to be loaded into a register
        ; - The constants have to be aligned to 8 bytes so that they can be patched easily
        ; - The constant loads have been located to minimize NOP padding required to align the constants
        ; - Using different registers for successive constant loads helps pipeline better. Should we decide to use a special
        ;   non-volatile calling convention, this should be changed to use just one register.

        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        ; Update the write watch table if necessary
        mov     rax, rcx
        mov     r8, 0F0F0F0F0F0F0F0F0h
        shr     rax, 0Ch ; SoftwareWriteWatch::AddressToTableByteIndexShift
        NOP_2_BYTE ; padding for alignment of constant
        mov     r9, 0F0F0F0F0F0F0F0F0h
        add     rax, r8
        cmp     byte ptr [rax], 0h
        jne     CheckCardTable
        mov     byte ptr [rax], 0FFh

        NOP_3_BYTE ; padding for alignment of constant

        ; Check the lower and upper ephemeral region bounds
    CheckCardTable:
        cmp     rdx, r9
        jb      Exit

        NOP_3_BYTE ; padding for alignment of constant

        mov     r8, 0F0F0F0F0F0F0F0F0h

        cmp     rdx, r8
        jae     Exit

        nop ; padding for alignment of constant

        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Touch the card table entry, if not already dirty.
        shr     rcx, 0Bh
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rcx + rax], 0FFh
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        mov     rax, 0F0F0F0F0F0F0F0F0h
        shr     rcx, 0Ah
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [rcx + rax], 0FFh
endif
        ret

    align 16
    Exit:
        REPRET
else
        ; JIT_WriteBarrier_PostGrow64

        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        NOP_3_BYTE ; padding for alignment of constant

        ; Can't compare a 64 bit immediate, so we have to move them into a
        ; register.  Values of these immediates will be patched at runtime.
        ; By using two registers we can pipeline better.  Should we decide to use
        ; a special non-volatile calling convention, this should be changed to
        ; just one.

        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Check the lower and upper ephemeral region bounds
        cmp     rdx, rax
        jb      Exit

        nop ; padding for alignment of constant

        mov     r8, 0F0F0F0F0F0F0F0F0h

        cmp     rdx, r8
        jae     Exit

        nop ; padding for alignment of constant

        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Touch the card table entry, if not already dirty.
        shr     rcx, 0Bh
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rcx + rax], 0FFh
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        mov     rax, 0F0F0F0F0F0F0F0F0h
        shr     rcx, 0Ah
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [rcx + rax], 0FFh
endif
        ret

    align 16
    Exit:
        REPRET
endif

    ; make sure this is bigger than any of the others
    align 16
        nop
LEAF_END_MARKED JIT_WriteBarrier, _TEXT

; Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
        ret
LEAF_END JIT_PatchedCodeLast, _TEXT

; JIT_ByRefWriteBarrier has weird symantics, see usage in StubLinkerX86.cpp
;
; Entry:
;   RDI - address of ref-field (assigned to)
;   RSI - address of the data  (source)
;   RCX is trashed
;   RAX is trashed when FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP is defined
; Exit:
;   RDI, RSI are incremented by SIZEOF(LPVOID)
LEAF_ENTRY JIT_ByRefWriteBarrier, _TEXT
        mov     rcx, [rsi]

; If !WRITE_BARRIER_CHECK do the write first, otherwise we might have to do some ShadowGC stuff
ifndef WRITE_BARRIER_CHECK
        ; rcx is [rsi]
        mov     [rdi], rcx
endif

        ; When WRITE_BARRIER_CHECK is defined _NotInHeap will write the reference
        ; but if it isn't then it will just return.
        ;
        ; See if this is in GCHeap
        cmp     rdi, [g_lowest_address]
        jb      NotInHeap
        cmp     rdi, [g_highest_address]
        jnb     NotInHeap

ifdef WRITE_BARRIER_CHECK
        ; we can only trash rcx in this function so in _DEBUG we need to save
        ; some scratch registers.
        push    r10
        push    r11
        push    rax

        ; **ALSO update the shadow GC heap if that is enabled**
        ; Do not perform the work if g_GCShadow is 0
        cmp     g_GCShadow, 0
        je      NoShadow

        ; If we end up outside of the heap don't corrupt random memory
        mov     r10, rdi
        sub     r10, [g_lowest_address]
        jb      NoShadow

        ; Check that our adjusted destination is somewhere in the shadow gc
        add     r10, [g_GCShadow]
        cmp     r10, [g_GCShadowEnd]
        ja      NoShadow

        ; Write ref into real GC
        mov     [rdi], rcx
        ; Write ref into shadow GC
        mov     [r10], rcx

        ; Ensure that the write to the shadow heap occurs before the read from
        ; the GC heap so that race conditions are caught by INVALIDGCVALUE
        mfence

        ; Check that GC/ShadowGC values match
        mov     r11, [rdi]
        mov     rax, [r10]
        cmp     rax, r11
        je      DoneShadow
        mov     r11, INVALIDGCVALUE
        mov     [r10], r11

        jmp     DoneShadow

    ; If we don't have a shadow GC we won't have done the write yet
    NoShadow:
        mov     [rdi], rcx

    ; If we had a shadow GC then we already wrote to the real GC at the same time
    ; as the shadow GC so we want to jump over the real write immediately above.
    ; Additionally we know for sure that we are inside the heap and therefore don't
    ; need to replicate the above checks.
    DoneShadow:
        pop     rax
        pop     r11
        pop     r10
endif

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        ; Update the write watch table if necessary
        cmp     byte ptr [g_sw_ww_enabled_for_gc_heap], 0h
        je      CheckCardTable
        mov     rax, rdi
        shr     rax, 0Ch ; SoftwareWriteWatch::AddressToTableByteIndexShift
        add     rax, qword ptr [g_sw_ww_table]
        cmp     byte ptr [rax], 0h
        jne     CheckCardTable
        mov     byte ptr [rax], 0FFh
endif

        ; See if we can just quick out
    CheckCardTable:
        cmp     rcx, [g_ephemeral_low]
        jb      Exit
        cmp     rcx, [g_ephemeral_high]
        jnb     Exit

        ; move current rdi value into rcx and then increment the pointers
        mov     rcx, rdi
        add     rsi, 8h
        add     rdi, 8h

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
        ; check if we need to update the card bundle table
        ; restore destination address from rdi - rdi has been incremented by 8 already
        lea     rcx, [rdi-8]
        shr     rcx, 15h
        add     rcx, [g_card_bundle_table]
        cmp     byte ptr [rcx], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [rcx], 0FFh
endif
        ret

    align 16
    NotInHeap:
; If WRITE_BARRIER_CHECK then we won't have already done the mov and should do it here
; If !WRITE_BARRIER_CHECK we want _NotInHeap and _Leave to be the same and have both
; 16 byte aligned.
ifdef WRITE_BARRIER_CHECK
        ; rcx is [rsi]
        mov     [rdi], rcx
endif
    Exit:
        ; Increment the pointers before leaving
        add     rdi, 8h
        add     rsi, 8h
        ret
LEAF_END_MARKED JIT_ByRefWriteBarrier, _TEXT

Section segment para 'DATA'

        align   16

        public  JIT_WriteBarrier_Loc
JIT_WriteBarrier_Loc:
        dq 0

LEAF_ENTRY  JIT_WriteBarrier_Callable, _TEXT
        ; JIT_WriteBarrier(Object** dst, Object* src)
        jmp     QWORD PTR [JIT_WriteBarrier_Loc]
LEAF_END JIT_WriteBarrier_Callable, _TEXT

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

; The following helper will access ("probe") a word on each page of the stack
; starting with the page right beneath rsp down to the one pointed to by r11.
; The procedure is needed to make sure that the "guard" page is pushed down below the allocated stack frame.
; The call to the helper will be emitted by JIT in the function/funclet prolog when large (larger than 0x3000 bytes) stack frame is required.
;
; NOTE: this helper will NOT modify a value of rsp and can be defined as a leaf function.

PROBE_PAGE_SIZE equ 1000h

LEAF_ENTRY JIT_StackProbe, _TEXT
        ; On entry:
        ;   r11 - points to the lowest address on the stack frame being allocated (i.e. [InitialSp - FrameSize])
        ;   rsp - points to some byte on the last probed page
        ; On exit:
        ;   rax - is not preserved
        ;   r11 - is preserved
        ;
        ; NOTE: this helper will probe at least one page below the one pointed by rsp.

        mov     rax, rsp               ; rax points to some byte on the last probed page
        and     rax, -PROBE_PAGE_SIZE  ; rax points to the **lowest address** on the last probed page
                                       ; This is done to make the following loop end condition simpler.

ProbeLoop:
        sub     rax, PROBE_PAGE_SIZE   ; rax points to the lowest address of the **next page** to probe
        test    dword ptr [rax], eax   ; rax points to the lowest address on the **last probed** page
        cmp     rax, r11
        jg      ProbeLoop              ; If (rax > r11), then we need to probe at least one more page.

        ret

LEAF_END_MARKED JIT_StackProbe, _TEXT

        end
