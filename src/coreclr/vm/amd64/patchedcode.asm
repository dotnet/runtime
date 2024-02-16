; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: patchedcode.asm
;
; Notes: routinues which are patched at runtime and need to be linked in
;        their declared order.
; ***********************************************************************


include AsmMacros.inc
include asmconstants.inc

ifdef _DEBUG
extern JIT_WriteBarrier_Debug:proc
endif


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

        NOP_3_BYTE
        NOP_3_BYTE
        NOP_3_BYTE
        NOP_3_BYTE
        NOP_3_BYTE

        NOP_3_BYTE
        NOP_3_BYTE
        NOP_3_BYTE
        NOP_3_BYTE
        NOP_3_BYTE

        NOP_3_BYTE
        NOP_3_BYTE
        NOP_3_BYTE
        NOP_3_BYTE
        NOP_3_BYTE

        NOP_3_BYTE
        NOP_3_BYTE
        NOP_3_BYTE
        NOP_3_BYTE
        NOP_3_BYTE

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

        end
