// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"

#ifdef _DEBUG
// Version for when we're sure to be in the GC, checks whether or not the card
// needs to be updated
//
// void JIT_WriteBarrier_Debug(Object** dst, Object* src)
LEAF_ENTRY JIT_WriteBarrier_Debug, _TEXT

#ifdef WRITE_BARRIER_CHECK
        // **ALSO update the shadow GC heap if that is enabled**
        // Do not perform the work if g_GCShadow is 0
        PREPARE_EXTERNAL_VAR g_GCShadow, rax
        cmp     qword ptr [rax], 0
        je      NoShadow

        // If we end up outside of the heap don't corrupt random memory
        mov     r10, rdi
        PREPARE_EXTERNAL_VAR g_lowest_address, r11
        sub     r10, [r11]
        jb      NoShadow

        // Check that our adjusted destination is somewhere in the shadow gc
        add     r10, [rax]
        PREPARE_EXTERNAL_VAR g_GCShadowEnd, r11
        cmp     r10, [r11]
        ja      NoShadow

        // Write ref into real GC// see comment below about possibility of AV
        mov     [rdi], rsi
        // Write ref into shadow GC
        mov     [r10], rsi

        // Ensure that the write to the shadow heap occurs before the read from
        // the GC heap so that race conditions are caught by INVALIDGCVALUE
        mfence

        // Check that GC/ShadowGC values match
        mov     r11, [rdi]
        mov     rax, [r10]
        cmp     rax, r11
        je      DoneShadow
        movabs  r11, INVALIDGCVALUE
        mov     [r10], r11

        jmp     DoneShadow

    // If we don't have a shadow GC we won't have done the write yet
    NoShadow:
#endif

        mov     rax, rsi

        // Do the move. It is correct to possibly take an AV here, the EH code
        // figures out that this came from a WriteBarrier and correctly maps it back
        // to the managed method which called the WriteBarrier (see setup in
        // InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rdi], rax

#ifdef WRITE_BARRIER_CHECK
    // If we had a shadow GC then we already wrote to the real GC at the same time
    // as the shadow GC so we want to jump over the real write immediately above
    DoneShadow:
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        // Update the write watch table if necessary
        PREPARE_EXTERNAL_VAR g_sw_ww_enabled_for_gc_heap, r10
        cmp     byte ptr [r10], 0x0
        je      CheckCardTable_Debug
        mov     r10, rdi
        shr     r10, 0xC // SoftwareWriteWatch::AddressToTableByteIndexShift
        PREPARE_EXTERNAL_VAR g_sw_ww_table, r11
        add     r10, qword ptr [r11]
        cmp     byte ptr [r10], 0x0
        jne     CheckCardTable_Debug
        mov     byte ptr [r10], 0xFF
#endif

    CheckCardTable_Debug:
        // See if we can just quick out
        PREPARE_EXTERNAL_VAR g_ephemeral_low, r10
        cmp     rax, [r10]
        jb      Exit_Debug
        PREPARE_EXTERNAL_VAR g_ephemeral_high, r10
        cmp     rax, [r10]
        jnb     Exit_Debug

        // Check if we need to update the card table
        // Calc pCardByte
        shr     rdi, 0x0B

        PREPARE_EXTERNAL_VAR g_card_table, r10
        mov     r10, [r10]

        // Check if this card is dirty
        cmp     byte ptr [rdi + r10], 0xFF

        jne     UpdateCardTable_Debug
        REPRET

    UpdateCardTable_Debug:
        mov     byte ptr [rdi + r10], 0xFF

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        // Shift rdi by 0x0A more to get the card bundle byte (we shifted by 0x0B already)
        shr     rdi, 0x0A

        PREPARE_EXTERNAL_VAR g_card_bundle_table, r10
        add     rdi, [r10]

        // Check if this bundle byte is dirty
        cmp     byte ptr [rdi], 0xFF

        jne     UpdateCardBundle_Debug
        REPRET

    UpdateCardBundle_Debug:
        mov     byte ptr [rdi], 0xFF
#endif

        ret

    .balign 16
    Exit_Debug:
        REPRET
LEAF_END_MARKED JIT_WriteBarrier_Debug, _TEXT
#endif
