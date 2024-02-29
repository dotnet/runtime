// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

// JIT_ByRefWriteBarrier has weird semantics, see usage in StubLinkerX86.cpp
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
//   Keep in sync with JIT_ByRefWriteBarrierBatch!!
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
        jb      LOCAL_LABEL(NotInHeap_ByRefWriteBarrier)
        PREPARE_EXTERNAL_VAR g_highest_address, rax
        cmp     rdi, [rax]
        jnb     LOCAL_LABEL(NotInHeap_ByRefWriteBarrier)

#ifdef WRITE_BARRIER_CHECK
        // **ALSO update the shadow GC heap if that is enabled**
        // Do not perform the work if g_GCShadow is 0
        PREPARE_EXTERNAL_VAR g_GCShadow, rax
        cmp     qword ptr [rax], 0
        je      LOCAL_LABEL(NoShadow_ByRefWriteBarrier)

        // If we end up outside of the heap don't corrupt random memory
        mov     r10, rdi
        PREPARE_EXTERNAL_VAR g_lowest_address, rax
        sub     r10, [rax]
        jb      LOCAL_LABEL(NoShadow_ByRefWriteBarrier)

        // Check that our adjusted destination is somewhere in the shadow gc
        PREPARE_EXTERNAL_VAR g_GCShadow, rax
        add     r10, [rax]
        PREPARE_EXTERNAL_VAR g_GCShadowEnd, rax
        cmp     r10, [rax]
        jnb     LOCAL_LABEL(NoShadow_ByRefWriteBarrier)

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
        je      LOCAL_LABEL(DoneShadow_ByRefWriteBarrier)
        movabs  r11, INVALIDGCVALUE
        mov     [r10], r11

        jmp     LOCAL_LABEL(DoneShadow_ByRefWriteBarrier)

    // If we don't have a shadow GC we won't have done the write yet
    LOCAL_LABEL(NoShadow_ByRefWriteBarrier):
        mov     [rdi], rcx

    // If we had a shadow GC then we already wrote to the real GC at the same time
    // as the shadow GC so we want to jump over the real write immediately above.
    // Additionally we know for sure that we are inside the heap and therefore don't
    // need to replicate the above checks.
    LOCAL_LABEL(DoneShadow_ByRefWriteBarrier):
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        // Update the write watch table if necessary
        PREPARE_EXTERNAL_VAR g_sw_ww_enabled_for_gc_heap, rax
        cmp     byte ptr [rax], 0x0
        je      LOCAL_LABEL(CheckCardTable_ByRefWriteBarrier)
        mov     rax, rdi
        shr     rax, 0xC // SoftwareWriteWatch::AddressToTableByteIndexShift
        PREPARE_EXTERNAL_VAR g_sw_ww_table, r10
        add     rax, qword ptr [r10]
        cmp     byte ptr [rax], 0x0
        jne     LOCAL_LABEL(CheckCardTable_ByRefWriteBarrier)
        mov     byte ptr [rax], 0xFF
#endif

    LOCAL_LABEL(CheckCardTable_ByRefWriteBarrier):
        // See if we can just quick out
        PREPARE_EXTERNAL_VAR g_ephemeral_low, rax
        cmp     rcx, [rax]
        jb      LOCAL_LABEL(Exit_ByRefWriteBarrier)
        PREPARE_EXTERNAL_VAR g_ephemeral_high, rax
        cmp     rcx, [rax]
        jnb     LOCAL_LABEL(Exit_ByRefWriteBarrier)

        mov     rax, rcx

        PREPARE_EXTERNAL_VAR g_region_shr, rcx
        mov     cl, [rcx]
        test    cl, cl
        je      LOCAL_LABEL(SkipCheck_ByRefWriteBarrier)

        // check if the source is in gen 2 - then it's not an ephemeral pointer
        shr     rax, cl
        PREPARE_EXTERNAL_VAR g_region_to_generation_table, r10
        mov     r10, [r10]
        cmp     byte ptr [rax + r10], 0x82
        je      LOCAL_LABEL(Exit_ByRefWriteBarrier)

        // check if the destination happens to be in gen 0
        mov     rax, rdi
        shr     rax, cl
        cmp     byte ptr [rax + r10], 0
        je      LOCAL_LABEL(Exit_ByRefWriteBarrier)
    LOCAL_LABEL(SkipCheck_ByRefWriteBarrier):

        PREPARE_EXTERNAL_VAR g_card_table, r10
        mov     r10, [r10]

        PREPARE_EXTERNAL_VAR g_region_use_bitwise_write_barrier, rax
        cmp     byte ptr [rax], 0
        je      LOCAL_LABEL(CheckCardTableByte_ByRefWriteBarrier)

        // compute card table bit
        mov     ecx, edi
        mov     al, 1
        shr     ecx, 8
        and     cl, 7
        shl     al, cl

        // move current rdi value into rcx and then increment the pointers
        mov     rcx, rdi
        add     rsi, 0x8
        add     rdi, 0x8

        // Check if we need to update the card table
        // Calc pCardByte
        shr     rcx, 0xB
        // Check if this card table bit is already set
        test    byte ptr [rcx + r10], al
        je      LOCAL_LABEL(SetCardTableBit_ByRefWriteBarrier)
        REPRET

    LOCAL_LABEL(SetCardTableBit_ByRefWriteBarrier):
        lock or byte ptr [rcx + r10], al

        jmp     LOCAL_LABEL(CheckCardBundle_ByRefWriteBarrier)

    LOCAL_LABEL(CheckCardTableByte_ByRefWriteBarrier):
        // move current rdi value into rcx and then increment the pointers
        mov     rcx, rdi
        add     rsi, 0x8
        add     rdi, 0x8

        shr     rcx, 0xB
        cmp     byte ptr [rcx + r10], 0xFF
        jne     LOCAL_LABEL(SetCardTableByte_ByRefWriteBarrier)
        REPRET
    LOCAL_LABEL(SetCardTableByte_ByRefWriteBarrier):
        mov     byte ptr [rcx + r10], 0xFF

    LOCAL_LABEL(CheckCardBundle_ByRefWriteBarrier):

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        // Shift rcx by 0x0A more to get the card bundle byte (we shifted by 0x0B already)
        shr     rcx, 0x0A

        PREPARE_EXTERNAL_VAR g_card_bundle_table, rax
        add     rcx, [rax]

        // Check if this bundle byte is dirty
        cmp     byte ptr [rcx], 0xFF

        jne     LOCAL_LABEL(UpdateCardBundle_ByRefWriteBarrier)
        REPRET

    LOCAL_LABEL(UpdateCardBundle_ByRefWriteBarrier):
        mov     byte ptr [rcx], 0xFF
#endif

        ret

    .balign 16
    LOCAL_LABEL(NotInHeap_ByRefWriteBarrier):
// If WRITE_BARRIER_CHECK then we won't have already done the mov and should do it here
// If !WRITE_BARRIER_CHECK we want _NotInHeap and _Leave to be the same and have both
// 16 byte aligned.
#ifdef WRITE_BARRIER_CHECK
        // rcx is [rsi]
        mov     [rdi], rcx
#endif
    LOCAL_LABEL(Exit_ByRefWriteBarrier):
        // Increment the pointers before leaving
        add     rdi, 0x8
        add     rsi, 0x8
        ret
LEAF_END_MARKED JIT_ByRefWriteBarrier, _TEXT


// JIT_ByRefWriteBarrierBatch is a batch version of JIT_ByRefWriteBarrier, so see comments there first
//
// Entry:
//   RDI - address of ref-field (assigned to)
//   RSI - address of the data  (source)
//   R8  - number of byrefs to write
//
//   RCX is trashed
//   RAX is trashed
//   R10 is trashed
//   R11 is trashed on Debug build
// Exit:
//   RDI, RSI are incremented by SIZEOF(LPVOID)
//   R8 is zeroed
LEAF_ENTRY JIT_ByRefWriteBarrierBatch, _TEXT
    LOCAL_LABEL(NextByref_ByRefWriteBarrierBatch):
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
        jb      LOCAL_LABEL(NotInHeap_ByRefWriteBarrierBatch)
        PREPARE_EXTERNAL_VAR g_highest_address, rax
        cmp     rdi, [rax]
        jnb     LOCAL_LABEL(NotInHeap_ByRefWriteBarrierBatch)

#ifdef WRITE_BARRIER_CHECK
        // **ALSO update the shadow GC heap if that is enabled**
        // Do not perform the work if g_GCShadow is 0
        PREPARE_EXTERNAL_VAR g_GCShadow, rax
        cmp     qword ptr [rax], 0
        je      LOCAL_LABEL(NoShadow_ByRefWriteBarrierBatch)

        // If we end up outside of the heap don't corrupt random memory
        mov     r10, rdi
        PREPARE_EXTERNAL_VAR g_lowest_address, rax
        sub     r10, [rax]
        jb      LOCAL_LABEL(NoShadow_ByRefWriteBarrierBatch)

        // Check that our adjusted destination is somewhere in the shadow gc
        PREPARE_EXTERNAL_VAR g_GCShadow, rax
        add     r10, [rax]
        PREPARE_EXTERNAL_VAR g_GCShadowEnd, rax
        cmp     r10, [rax]
        jnb     LOCAL_LABEL(NoShadow_ByRefWriteBarrierBatch)

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
        je      LOCAL_LABEL(DoneShadow_ByRefWriteBarrierBatch)
        movabs  r11, INVALIDGCVALUE
        mov     [r10], r11

        jmp     LOCAL_LABEL(DoneShadow_ByRefWriteBarrierBatch)

    // If we don't have a shadow GC we won't have done the write yet
    LOCAL_LABEL(NoShadow_ByRefWriteBarrierBatch):
        mov     [rdi], rcx

    // If we had a shadow GC then we already wrote to the real GC at the same time
    // as the shadow GC so we want to jump over the real write immediately above.
    // Additionally we know for sure that we are inside the heap and therefore don't
    // need to replicate the above checks.
    LOCAL_LABEL(DoneShadow_ByRefWriteBarrierBatch):
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        // Update the write watch table if necessary
        PREPARE_EXTERNAL_VAR g_sw_ww_enabled_for_gc_heap, rax
        cmp     byte ptr [rax], 0x0
        je      LOCAL_LABEL(CheckCardTable_ByRefWriteBarrierBatch)
        mov     rax, rdi
        shr     rax, 0xC // SoftwareWriteWatch::AddressToTableByteIndexShift
        PREPARE_EXTERNAL_VAR g_sw_ww_table, r10
        add     rax, qword ptr [r10]
        cmp     byte ptr [rax], 0x0
        jne     LOCAL_LABEL(CheckCardTable_ByRefWriteBarrierBatch)
        mov     byte ptr [rax], 0xFF
#endif

    LOCAL_LABEL(CheckCardTable_ByRefWriteBarrierBatch):
        // See if we can just quick out
        PREPARE_EXTERNAL_VAR g_ephemeral_low, rax
        cmp     rcx, [rax]
        jb      LOCAL_LABEL(Exit_ByRefWriteBarrierBatch)
        PREPARE_EXTERNAL_VAR g_ephemeral_high, rax
        cmp     rcx, [rax]
        jnb     LOCAL_LABEL(Exit_ByRefWriteBarrierBatch)

        mov     rax, rcx

        PREPARE_EXTERNAL_VAR g_region_shr, rcx
        mov     cl, [rcx]
        test    cl, cl
        je      LOCAL_LABEL(SkipCheck_ByRefWriteBarrierBatch)

        // check if the source is in gen 2 - then it's not an ephemeral pointer
        shr     rax, cl
        PREPARE_EXTERNAL_VAR g_region_to_generation_table, r10
        mov     r10, [r10]
        cmp     byte ptr [rax + r10], 0x82
        je      LOCAL_LABEL(Exit_ByRefWriteBarrierBatch)

        // check if the destination happens to be in gen 0
        mov     rax, rdi
        shr     rax, cl
        cmp     byte ptr [rax + r10], 0
        je      LOCAL_LABEL(Exit_ByRefWriteBarrierBatch)
    LOCAL_LABEL(SkipCheck_ByRefWriteBarrierBatch):

        PREPARE_EXTERNAL_VAR g_card_table, r10
        mov     r10, [r10]

        PREPARE_EXTERNAL_VAR g_region_use_bitwise_write_barrier, rax
        cmp     byte ptr [rax], 0
        je      LOCAL_LABEL(CheckCardTableByte_ByRefWriteBarrierBatch)

        // compute card table bit
        mov     ecx, edi
        mov     al, 1
        shr     ecx, 8
        and     cl, 7
        shl     al, cl

        // move current rdi value into rcx and then increment the pointers
        mov     rcx, rdi
        add     rsi, 0x8
        add     rdi, 0x8

        // Check if we need to update the card table
        // Calc pCardByte
        shr     rcx, 0xB
        // Check if this card table bit is already set
        test    byte ptr [rcx + r10], al
        je      LOCAL_LABEL(SetCardTableBit_ByRefWriteBarrierBatch)
        // Check if we have more in the batch and run again
        dec     r8d
        jne     LOCAL_LABEL(NextByref_ByRefWriteBarrierBatch)
        REPRET

    LOCAL_LABEL(SetCardTableBit_ByRefWriteBarrierBatch):
        lock or byte ptr [rcx + r10], al

        jmp     LOCAL_LABEL(CheckCardBundle_ByRefWriteBarrierBatch)

    LOCAL_LABEL(CheckCardTableByte_ByRefWriteBarrierBatch):
        // move current rdi value into rcx and then increment the pointers
        mov     rcx, rdi
        add     rsi, 0x8
        add     rdi, 0x8

        shr     rcx, 0xB
        cmp     byte ptr [rcx + r10], 0xFF
        jne     LOCAL_LABEL(SetCardTableByte_ByRefWriteBarrierBatch)
        // Check if we have more in the batch and run again
        dec     r8d
        jne     LOCAL_LABEL(NextByref_ByRefWriteBarrierBatch)
        REPRET
    LOCAL_LABEL(SetCardTableByte_ByRefWriteBarrierBatch):
        mov     byte ptr [rcx + r10], 0xFF

    LOCAL_LABEL(CheckCardBundle_ByRefWriteBarrierBatch):

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        // Shift rcx by 0x0A more to get the card bundle byte (we shifted by 0x0B already)
        shr     rcx, 0x0A

        PREPARE_EXTERNAL_VAR g_card_bundle_table, rax
        add     rcx, [rax]

        // Check if this bundle byte is dirty
        cmp     byte ptr [rcx], 0xFF

        jne     LOCAL_LABEL(UpdateCardBundle_ByRefWriteBarrierBatch)
        // Check if we have more in the batch and run again
        dec     r8d
        jne     LOCAL_LABEL(NextByref_ByRefWriteBarrierBatch)
        REPRET

    LOCAL_LABEL(UpdateCardBundle_ByRefWriteBarrierBatch):
        mov     byte ptr [rcx], 0xFF
#endif
        // Check if we have more in the batch and run again
        dec     r8d
        jne     LOCAL_LABEL(NextByref_ByRefWriteBarrierBatch)
        ret

    .balign 16
    LOCAL_LABEL(NotInHeap_ByRefWriteBarrierBatch):
// If WRITE_BARRIER_CHECK then we won't have already done the mov and should do it here
// If !WRITE_BARRIER_CHECK we want _NotInHeap and _Leave to be the same and have both
// 16 byte aligned.
#ifdef WRITE_BARRIER_CHECK
        // rcx is [rsi]
        mov     [rdi], rcx
#endif

    // At least one write is already done, increment the pointers
        add     rdi, 0x8
        add     rsi, 0x8
        dec     r8d
        je      LOCAL_LABEL(NotInHeapExit)
    // Now we can do the rest of the writes without checking the heap
    LOCAL_LABEL(NextByrefUnchecked):
        mov     rcx, [rsi]
        mov     [rdi], rcx
        add     rdi, 0x8
        add     rsi, 0x8
        dec     r8d
        jne     LOCAL_LABEL(NextByrefUnchecked)
    LOCAL_LABEL(NotInHeapExit):
        ret

    LOCAL_LABEL(Exit_ByRefWriteBarrierBatch):
        // Increment the pointers before leaving
        add     rdi, 0x8
        add     rsi, 0x8
        // Check if we have more in the batch and run again
        dec     r8d
        jne     LOCAL_LABEL(NextByref_ByRefWriteBarrierBatch)
        ret
LEAF_END_MARKED JIT_ByRefWriteBarrierBatch, _TEXT

        // When JIT_WriteBarrier is copied into an allocated page,
        // helpers use this global variable to jump to it. This variable is set in InitThreadManager.
        .global C_FUNC(JIT_WriteBarrier_Loc)
#ifdef TARGET_OSX
        .zerofill __DATA,__common,C_FUNC(JIT_WriteBarrier_Loc),8,3
#else
        .data
    C_FUNC(JIT_WriteBarrier_Loc):
        .quad 0
        .text
#endif

// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_WriteBarrier_Callable(Object **dst, Object* val)
.balign 16
LEAF_ENTRY  JIT_WriteBarrier_Callable, _TEXT
    // JIT_WriteBarrier(Object** dst, Object* src)
    jmp     [rip + C_FUNC(JIT_WriteBarrier_Loc)]
LEAF_END JIT_WriteBarrier_Callable, _TEXT


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

LEAF_ENTRY JIT_ValidateIndirectCall, _TEXT
        ret
LEAF_END JIT_ValidateIndirectCall, _TEXT

LEAF_ENTRY JIT_DispatchIndirectCall, _TEXT
        movabs r10, 0xCDCDCDCDCDCDCDCD // The real helper clobbers these registers, so clobber them too in the fake helper
        movabs r11, 0xCDCDCDCDCDCDCDCD
        rex64 jmp rax
LEAF_END JIT_DispatchIndirectCall, _TEXT
