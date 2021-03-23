// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

// Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
        ret
LEAF_END JIT_PatchedCodeStart, _TEXT


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
#ifdef FEATURE_WRITEBARRIER_COPY
        // jb      NotInHeap
        .byte 0x72, 0x12
#else
        // jb      NotInHeap
        .byte 0x72, 0x0e
#endif
        PREPARE_EXTERNAL_VAR g_highest_address, rax
        cmp     rdi, [rax]

#ifdef FEATURE_WRITEBARRIER_COPY
        // jnb     NotInHeap
        .byte 0x73, 0x06
        jmp     [rip + C_FUNC(JIT_WriteBarrier_Loc)]
#else
        // jnb     NotInHeap
        .byte 0x73, 0x02
        // jmp C_FUNC(JIT_WriteBarrier)
        .byte 0xeb, 0x05
#endif

    NotInHeap:
        // See comment above about possible AV
        mov     [rdi], rsi
        ret
LEAF_END_MARKED JIT_CheckedWriteBarrier, _TEXT


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

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        // JIT_WriteBarrier_WriteWatch_PostGrow64

        // Regarding patchable constants:
        // - 64-bit constants have to be loaded into a register
        // - The constants have to be aligned to 8 bytes so that they can be patched easily
        // - The constant loads have been located to minimize NOP padding required to align the constants
        // - Using different registers for successive constant loads helps pipeline better. Should we decide to use a special
        //   non-volatile calling convention, this should be changed to use just one register.

        // Do the move into the GC .  It is correct to take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rdi], rsi

        // Update the write watch table if necessary
        mov     rax, rdi
        movabs  r10, 0xF0F0F0F0F0F0F0F0
        shr     rax, 0xC // SoftwareWriteWatch::AddressToTableByteIndexShift
        NOP_2_BYTE // padding for alignment of constant
        movabs  r11, 0xF0F0F0F0F0F0F0F0
        add     rax, r10
        cmp     byte ptr [rax], 0x0
        .byte 0x75, 0x06
        // jne     CheckCardTable
        mov     byte ptr [rax], 0xFF

        NOP_3_BYTE // padding for alignment of constant

        // Check the lower and upper ephemeral region bounds
    CheckCardTable:
        cmp     rsi, r11
        .byte 0x72,0x3D
        // jb      Exit

        NOP_3_BYTE // padding for alignment of constant

        movabs  r10, 0xF0F0F0F0F0F0F0F0

        cmp     rsi, r10
        .byte 0x73,0x2B
        // jae     Exit

        nop // padding for alignment of constant

        movabs  rax, 0xF0F0F0F0F0F0F0F0

        // Touch the card table entry, if not already dirty.
        shr     rdi, 0x0B
        cmp     byte ptr [rdi + rax], 0xFF
        .byte 0x75, 0x02
        // jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rdi + rax], 0xFF

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        NOP_2_BYTE // padding for alignment of constant
        shr     rdi, 0x0A

        movabs  rax, 0xF0F0F0F0F0F0F0F0
        cmp     byte ptr [rdi + rax], 0xFF

        .byte 0x75, 0x02
        // jne     UpdateCardBundle_WriteWatch_PostGrow64
        REPRET

    UpdateCardBundle_WriteWatch_PostGrow64:
        mov     byte ptr [rdi + rax], 0xFF
#endif

        ret

    .balign 16
    Exit:
        REPRET
#else
        // JIT_WriteBarrier_PostGrow64

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
        .byte 0x75, 0x02
        // jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rdi + rax], 0FFh

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        NOP_6_BYTE // padding for alignment of constant

        movabs  rax, 0xF0F0F0F0F0F0F0F0

        // Touch the card bundle, if not already dirty.
        // rdi is already shifted by 0xB, so shift by 0xA more
        shr     rdi, 0x0A
        cmp     byte ptr [rdi + rax], 0FFh

        .byte 0x75, 0x02
        // jne     UpdateCardBundle
        REPRET

    UpdateCardBundle:
        mov     byte ptr [rdi + rax], 0FFh
#endif

        ret

    .balign 16
    Exit:
        REPRET
#endif

    // make sure this is bigger than any of the others 
    .balign 16
        nop
LEAF_END_MARKED JIT_WriteBarrier, _TEXT

// Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
        ret
LEAF_END JIT_PatchedCodeLast, _TEXT

// JIT_ByRefWriteBarrier has weird symantics, see usage in StubLinkerX86.cpp
//
// Entry:
//   RDI - address of ref-field (assigned to)
//   RSI - address of the data  (source)
//
//   Note: RyuJIT assumes that all volatile registers can be trashed by
//   the CORINFO_HELP_ASSIGN_BYREF helper (i.e. JIT_ByRefWriteBarrier)
//   except RDI and RSI. This helper uses and defines RDI and RSI, so
//   they remain as live GC refs or byrefs, and are not killed.
//
//
//   RCX is trashed
//   RAX is trashed
//   R10 is trashed
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
        movabs  r11, INVALIDGCVALUE
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

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        // Update the write watch table if necessary
        PREPARE_EXTERNAL_VAR g_sw_ww_enabled_for_gc_heap, rax
        cmp     byte ptr [rax], 0x0
        je      CheckCardTable_ByRefWriteBarrier
        mov     rax, rdi
        shr     rax, 0xC // SoftwareWriteWatch::AddressToTableByteIndexShift
        PREPARE_EXTERNAL_VAR g_sw_ww_table, r10
        add     rax, qword ptr [r10]
        cmp     byte ptr [rax], 0x0
        jne     CheckCardTable_ByRefWriteBarrier
        mov     byte ptr [rax], 0xFF
#endif

    CheckCardTable_ByRefWriteBarrier:
        // See if we can just quick out
        PREPARE_EXTERNAL_VAR g_ephemeral_low, rax
        cmp     rcx, [rax]
        jb      Exit_ByRefWriteBarrier
        PREPARE_EXTERNAL_VAR g_ephemeral_high, rax
        cmp     rcx, [rax]
        jnb     Exit_ByRefWriteBarrier

        // move current rdi value into rcx and then increment the pointers
        mov     rcx, rdi
        add     rsi, 0x8
        add     rdi, 0x8

        // Check if we need to update the card table
        // Calc pCardByte
        shr     rcx, 0x0B

        PREPARE_EXTERNAL_VAR g_card_table, rax
        mov     rax, [rax]

        // Check if this card is dirty
        cmp     byte ptr [rcx + rax], 0xFF

        jne     UpdateCardTable_ByRefWriteBarrier
        REPRET

    UpdateCardTable_ByRefWriteBarrier:
        mov     byte ptr [rcx + rax], 0xFF

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        // Shift rcx by 0x0A more to get the card bundle byte (we shifted by 0x0B already)
        shr     rcx, 0x0A

        PREPARE_EXTERNAL_VAR g_card_bundle_table, rax
        add     rcx, [rax]

        // Check if this bundle byte is dirty
        cmp     byte ptr [rcx], 0xFF

        jne     UpdateCardBundle_ByRefWriteBarrier
        REPRET

    UpdateCardBundle_ByRefWriteBarrier:
        mov     byte ptr [rcx], 0xFF
#endif

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
        add     rdi, 0x8
        add     rsi, 0x8
        ret
LEAF_END_MARKED JIT_ByRefWriteBarrier, _TEXT

#ifdef FEATURE_WRITEBARRIER_COPY
        // When JIT_WriteBarrier is copied into an allocated page,
        // helpers use this global variable to jump to it. This variable is set in InitThreadManager.
        .global _JIT_WriteBarrier_Loc
        .zerofill __DATA,__common,_JIT_WriteBarrier_Loc,8,3

// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_WriteBarrier_Callable(Object **dst, Object* val)
.balign 16
LEAF_ENTRY  JIT_WriteBarrier_Callable, _TEXT
    // JIT_WriteBarrier(Object** dst, Object* src)
    jmp     [rip + C_FUNC(JIT_WriteBarrier_Loc)]
LEAF_END JIT_WriteBarrier_Callable, _TEXT

#endif // FEATURE_WRITEBARRIER_COPY


// The following helper will access ("probe") a word on each page of the stack
// starting with the page right beneath rsp down to the one pointed to by r11.
// The procedure is needed to make sure that the "guard" page is pushed down below the allocated stack frame.
// The call to the helper will be emitted by JIT in the function/funclet prolog when large (larger than 0x3000 bytes) stack frame is required.
//
// NOTE: On Linux we must advance the stack pointer as we probe - it is not allowed to access 65535 bytes below rsp.
// Since this helper will modify a value of rsp - it must establish the frame pointer.
//
// See also https://github.com/dotnet/runtime/issues/9899#issue-303331518 for more information.

#define PROBE_PAGE_SIZE 0x1000

LEAF_ENTRY JIT_StackProbe, _TEXT
        // On entry:
        //   r11 - points to the lowest address on the stack frame being allocated (i.e. [InitialSp - FrameSize])
        //   rsp - points to some byte on the last probed page
        // On exit:
        //   r11 - is preserved
        //
        // NOTE: this helper will probe at least one page below the one pointed by rsp.

        push_nonvol_reg rbp
        mov     rbp, rsp
        set_cfa_register rbp, 16

    END_PROLOGUE

        and     rsp, -PROBE_PAGE_SIZE  // rsp points to the **lowest address** on the last probed page
                                       // This is done to make the following loop end condition simpler.

LOCAL_LABEL(ProbeLoop):
        sub     rsp, PROBE_PAGE_SIZE   // rsp points to the lowest address of the **next page** to probe
        test    dword ptr [rsp], eax   // rsp points to the lowest address on the **last probed** page
        cmp     rsp, r11
        jg      LOCAL_LABEL(ProbeLoop) // if (rsp > r11), then we need to probe at least one more page.

        RESET_FRAME_WITH_RBP
        ret

LEAF_END_MARKED JIT_StackProbe, _TEXT
