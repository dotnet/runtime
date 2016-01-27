// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix
#include "unixasmmacros.inc"

// Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
        ret
LEAF_END JIT_PatchedCodeStart, _TEXT

// This is used by the mechanism to hold either the JIT_WriteBarrier_PreGrow 
// or JIT_WriteBarrier_PostGrow code (depending on the state of the GC). It _WILL_
// change at runtime as the GC changes. Initially it should simply be a copy of the 
// larger of the two functions (JIT_WriteBarrier_PostGrow) to ensure we have created
// enough space to copy that code in.
.balign 16
LEAF_ENTRY JIT_WriteBarrier, _TEXT
#ifdef _DEBUG
        // In debug builds, this just contains jump to the debug version of the write barrier by default
        jmp C_FUNC(JIT_WriteBarrier_Debug)
#endif

        // Do the move into the GC .  It is correct to take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rdi], rsi

        NOP_3_BYTE // padding for alignment of constant

        // Can't compare a 64 bit immediate, so we have to move them into a
        // register.  Values of these immediates will be patched at runtime.
        // By using two registers we can pipeline better.  Should we decide to use
        // a special non-volatile calling convention, this should be changed to
        // just one.

        movabs  rax, 0xF0F0F0F0F0F0F0F0

        // Check the lower and upper ephemeral region bounds
        cmp     rsi, rax
        // jb      Exit
        .byte 0x72, 0x36

        nop // padding for alignment of constant

        movabs  r8, 0xF0F0F0F0F0F0F0F0

        cmp     rsi, r8
        // jae     Exit
        .byte 0x73, 0x26

        nop // padding for alignment of constant

        movabs  rax, 0xF0F0F0F0F0F0F0F0

        // Touch the card table entry, if not already dirty.
        shr     rdi, 0Bh
        cmp     byte ptr [rdi + rax], 0FFh
        // jne     UpdateCardTable
        .byte 0x75, 0x02
        REPRET

    UpdateCardTable:
        mov     byte ptr [rdi + rax], 0FFh
        ret

    .balign 16
    Exit:
        REPRET
    // make sure this guy is bigger than any of the other guys
    .balign 16
        nop
LEAF_END_MARKED JIT_WriteBarrier, _TEXT

// Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
        ret
LEAF_END JIT_PatchedCodeLast, _TEXT

// There is an even more optimized version of these helpers possible which takes
// advantage of knowledge of which way the ephemeral heap is growing to only do 1/2
// that check (this is more significant in the JIT_WriteBarrier case).
//
// Additionally we can look into providing helpers which will take the src/dest from
// specific registers (like x86) which _could_ (??) make for easier register allocation
// for the JIT64, however it might lead to having to have some nasty code that treats
// these guys really special like... :(.
//
// Version that does the move, checks whether or not it's in the GC and whether or not
// it needs to have it's card updated
//
// void JIT_CheckedWriteBarrier(Object** dst, Object* src)
LEAF_ENTRY JIT_CheckedWriteBarrier, _TEXT

        // When WRITE_BARRIER_CHECK is defined _NotInHeap will write the reference
        // but if it isn't then it will just return.
        //
        // See if this is in GCHeap
        PREPARE_EXTERNAL_VAR g_lowest_address, rax
        cmp     rdi, [rax]
        // jb      NotInHeap
        .byte 0x72, 0x0e
        PREPARE_EXTERNAL_VAR g_highest_address, rax
        cmp     rdi, [rax]
        // jnb     NotInHeap
        .byte 0x73, 0x02
        
        // call C_FUNC(JIT_WriteBarrier)
        .byte 0xeb, 0x84

    NotInHeap:
        // See comment above about possible AV
        mov     [rdi], rsi
        ret
LEAF_END_MARKED JIT_CheckedWriteBarrier, _TEXT

// JIT_ByRefWriteBarrier has weird symantics, see usage in StubLinkerX86.cpp
//
// Entry:
//   RDI - address of ref-field (assigned to)
//   RSI - address of the data  (source)
//
//   Note: RyuJIT assumes that all volatile registers can be trashed by
//   the CORINFO_HELP_ASSIGN_BYREF helper (i.e. JIT_ByRefWriteBarrier).
//   The precise set is defined by RBM_CALLEE_TRASH.
//
//   RCX is trashed
//   RAX is trashed
//   R10 is trashed on Debug build
//   R11 is trashed on Debug build
// Exit:
//   RDI, RSI are incremented by SIZEOF(LPVOID)
LEAF_ENTRY JIT_ByRefWriteBarrier, _TEXT
        mov     rcx, [rsi]

// If !WRITE_BARRIER_CHECK do the write first, otherwise we might have to do some ShadowGC stuff
#ifndef WRITE_BARRIER_CHECK
        // rcx is [rsi]
        mov     [rdi], rcx
#endif

        // When WRITE_BARRIER_CHECK is defined _NotInHeap will write the reference
        // but if it isn't then it will just return.
        //
        // See if this is in GCHeap
        PREPARE_EXTERNAL_VAR g_lowest_address, rax
        cmp     rdi, [rax]
        jb      NotInHeap_ByRefWriteBarrier
        PREPARE_EXTERNAL_VAR g_highest_address, rax
        cmp     rdi, [rax]
        jnb     NotInHeap_ByRefWriteBarrier

#ifdef WRITE_BARRIER_CHECK
        // **ALSO update the shadow GC heap if that is enabled**
        // Do not perform the work if g_GCShadow is 0
        PREPARE_EXTERNAL_VAR g_GCShadow, rax
        cmp     qword ptr [rax], 0
        je      NoShadow_ByRefWriteBarrier

        // If we end up outside of the heap don't corrupt random memory
        mov     r10, rdi
        PREPARE_EXTERNAL_VAR g_lowest_address, rax
        sub     r10, [rax]
        jb      NoShadow_ByRefWriteBarrier

        // Check that our adjusted destination is somewhere in the shadow gc
        PREPARE_EXTERNAL_VAR g_GCShadow, rax
        add     r10, [rax]
        PREPARE_EXTERNAL_VAR g_GCShadowEnd, rax
        cmp     r10, [rax]
        ja      NoShadow_ByRefWriteBarrier

        // Write ref into real GC
        mov     [rdi], rcx
        // Write ref into shadow GC
        mov     [r10], rcx

        // Ensure that the write to the shadow heap occurs before the read from
        // the GC heap so that race conditions are caught by INVALIDGCVALUE
        mfence

        // Check that GC/ShadowGC values match
        mov     r11, [rdi]
        mov     rax, [r10]
        cmp     rax, r11
        je      DoneShadow_ByRefWriteBarrier
        mov     r11, INVALIDGCVALUE
        mov     [r10], r11

        jmp     DoneShadow_ByRefWriteBarrier

    // If we don't have a shadow GC we won't have done the write yet
    NoShadow_ByRefWriteBarrier:
        mov     [rdi], rcx

    // If we had a shadow GC then we already wrote to the real GC at the same time
    // as the shadow GC so we want to jump over the real write immediately above.
    // Additionally we know for sure that we are inside the heap and therefore don't
    // need to replicate the above checks.
    DoneShadow_ByRefWriteBarrier:
#endif

        // See if we can just quick out
        PREPARE_EXTERNAL_VAR g_ephemeral_low, rax
        cmp     rcx, [rax]
        jb      Exit_ByRefWriteBarrier
        PREPARE_EXTERNAL_VAR g_ephemeral_high, rax
        cmp     rcx, [rax]
        jnb     Exit_ByRefWriteBarrier

        // move current rdi value into rcx and then increment the pointers
        mov     rcx, rdi
        add     rsi, 8h
        add     rdi, 8h

        // Check if we need to update the card table
        // Calc pCardByte
        shr     rcx, 0Bh
        PREPARE_EXTERNAL_VAR g_card_table, rax
        add     rcx, [rax]

        // Check if this card is dirty
        cmp     byte ptr [rcx], 0FFh
        jne     UpdateCardTable_ByRefWriteBarrier
        REPRET

    UpdateCardTable_ByRefWriteBarrier:
        mov     byte ptr [rcx], 0FFh
        ret

    .balign 16
    NotInHeap_ByRefWriteBarrier:
// If WRITE_BARRIER_CHECK then we won't have already done the mov and should do it here
// If !WRITE_BARRIER_CHECK we want _NotInHeap and _Leave to be the same and have both
// 16 byte aligned.
#ifdef WRITE_BARRIER_CHECK
        // rcx is [rsi]
        mov     [rdi], rcx
#endif
    Exit_ByRefWriteBarrier:
        // Increment the pointers before leaving
        add     rdi, 8h
        add     rsi, 8h
        ret
LEAF_END JIT_ByRefWriteBarrier, _TEXT
