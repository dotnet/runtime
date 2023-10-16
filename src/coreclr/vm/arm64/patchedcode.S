// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

//-----------------------------------------------------------------------------
// The following Macros help in WRITE_BARRIER Implementations
// WRITE_BARRIER_ENTRY
//
// Declare the start of a write barrier function. Use similarly to NESTED_ENTRY. This is the only legal way
// to declare a write barrier function.
//
.macro WRITE_BARRIER_ENTRY name
    LEAF_ENTRY \name, _TEXT
.endm

// WRITE_BARRIER_END
//
// The partner to WRITE_BARRIER_ENTRY, used like NESTED_END.
//
.macro WRITE_BARRIER_END name
    LEAF_END_MARKED \name, _TEXT
.endm

.balign 64  // Align to power of two at least as big as patchable literal pool so that it fits optimally in cache line
//------------------------------------------
// Start of the writeable code region
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
    ret  lr
LEAF_END JIT_PatchedCodeStart, _TEXT

// void JIT_ByRefWriteBarrier
// On entry:
//   x13  : the source address (points to object reference to write)
//   x14  : the destination address (object reference written here)
//
// On exit:
//   x12  : trashed
//   x13  : incremented by 8
//   x14  : incremented by 8
//   x15  : trashed
//   x17  : trashed (ip1) if FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
//
WRITE_BARRIER_ENTRY JIT_ByRefWriteBarrier

    ldr  x15, [x13], 8
    b C_FUNC(JIT_CheckedWriteBarrier)

WRITE_BARRIER_END JIT_ByRefWriteBarrier

//-----------------------------------------------------------------------------
// Simple WriteBarriers
// void JIT_CheckedWriteBarrier(Object** dst, Object* src)
// On entry:
//   x14  : the destination address (LHS of the assignment)
//   x15  : the object reference (RHS of the assignment)
//
// On exit:
//   x12  : trashed
//   x14  : trashed (incremented by 8 to implement JIT_ByRefWriteBarrier contract)
//   x15  : trashed
//   x17  : trashed (ip1) if FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
//
WRITE_BARRIER_ENTRY JIT_CheckedWriteBarrier
    ldr  x12,  LOCAL_LABEL(wbs_lowest_address)
    cmp  x14,  x12

    ldr  x12, LOCAL_LABEL(wbs_highest_address)

    // Compare against the upper bound if the previous comparison indicated
    // that the destination address is greater than or equal to the lower
    // bound. Otherwise, set the C flag (specified by the 0x2) so that the
    // branch below is not taken.
    ccmp x14, x12, #0x2, hs

    bhs  LOCAL_LABEL(NotInHeap)

    b C_FUNC(JIT_WriteBarrier)

LOCAL_LABEL(NotInHeap):
    str  x15, [x14], 8
    ret  lr
WRITE_BARRIER_END JIT_CheckedWriteBarrier

// void JIT_WriteBarrier(Object** dst, Object* src)
// On entry:
//   x14  : the destination address (LHS of the assignment)
//   x15  : the object reference (RHS of the assignment)
//
// On exit:
//   x12  : trashed
//   x14  : trashed (incremented by 8 to implement JIT_ByRefWriteBarrier contract)
//   x15  : trashed
//   x17  : trashed (ip1) if FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
//
WRITE_BARRIER_ENTRY JIT_WriteBarrier
    stlr  x15, [x14]

#ifdef WRITE_BARRIER_CHECK
    // Update GC Shadow Heap

    // Do not perform the work if g_GCShadow is 0
    ldr  x12, LOCAL_LABEL(wbs_GCShadow)
    cbz  x12, LOCAL_LABEL(ShadowUpdateDisabled)

    // need temporary register. Save before using.
    str  x13, [sp, #-16]!

    // Compute address of shadow heap location:
    //   pShadow = g_GCShadow + (x14 - g_lowest_address)
    ldr  x13, LOCAL_LABEL(wbs_lowest_address)
    sub  x13, x14, x13
    add  x12, x13, x12

    // if (pShadow >= g_GCShadowEnd) goto end
    ldr  x13, LOCAL_LABEL(wbs_GCShadowEnd)
    cmp  x12, x13
    bhs  LOCAL_LABEL(ShadowUpdateEnd)

    // *pShadow = x15
    str  x15, [x12]

    // Ensure that the write to the shadow heap occurs before the read from the GC heap so that race
    // conditions are caught by INVALIDGCVALUE.
    dmb  ish

    // if ([x14] == x15) goto end
    ldr  x13, [x14]
    cmp  x13, x15
    beq LOCAL_LABEL(ShadowUpdateEnd)

    // *pShadow = INVALIDGCVALUE (0xcccccccd)
    movz x13, #0xcccd
    movk x13, #0xcccc, LSL #16
    str  x13, [x12]

LOCAL_LABEL(ShadowUpdateEnd):
    ldr  x13, [sp], #16
LOCAL_LABEL(ShadowUpdateDisabled):
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    // Update the write watch table if necessary
    ldr  x12, LOCAL_LABEL(wbs_sw_ww_table)
    cbz  x12, LOCAL_LABEL(CheckCardTable)
    add  x12, x12, x14, lsr #0xc  // SoftwareWriteWatch::AddressToTableByteIndexShift
    ldrb w17, [x12]
    cbnz x17, LOCAL_LABEL(CheckCardTable)
    mov  w17, #0xFF
    strb w17, [x12]
#endif

LOCAL_LABEL(CheckCardTable):
    // Branch to Exit if the reference is not in the Gen0 heap
    //
    ldr  x12, LOCAL_LABEL(wbs_ephemeral_low)
    cbz  x12, LOCAL_LABEL(SkipEphemeralCheck)
    cmp  x15,  x12

    ldr  x12, LOCAL_LABEL(wbs_ephemeral_high)

    // Compare against the upper bound if the previous comparison indicated
    // that the destination address is greater than or equal to the lower
    // bound. Otherwise, set the C flag (specified by the 0x2) so that the
    // branch to exit is taken.
    ccmp x15, x12, #0x2, hs

    bhs  LOCAL_LABEL(Exit)

LOCAL_LABEL(SkipEphemeralCheck):
    // Check if we need to update the card table
    ldr  x12, LOCAL_LABEL(wbs_card_table)
    add  x15, x12, x14, lsr #11
    ldrb w12, [x15]
    cmp  x12, 0xFF
    beq  LOCAL_LABEL(Exit)

LOCAL_LABEL(UpdateCardTable):
    mov  x12, 0xFF
    strb w12, [x15]

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    // Check if we need to update the card bundle table
    ldr  x12, LOCAL_LABEL(wbs_card_bundle_table)
    add  x15, x12, x14, lsr #21
    ldrb w12, [x15]
    cmp  x12, 0xFF
    beq  LOCAL_LABEL(Exit)

LOCAL_LABEL(UpdateCardBundle):
    mov  x12, 0xFF
    strb w12, [x15]
#endif

LOCAL_LABEL(Exit):
    add  x14, x14, 8
    ret  lr
WRITE_BARRIER_END JIT_WriteBarrier

    // Begin patchable literal pool
    .balign 64  // Align to power of two at least as big as patchable literal pool so that it fits optimally in cache line
WRITE_BARRIER_ENTRY JIT_WriteBarrier_Table
LOCAL_LABEL(wbs_begin):
LOCAL_LABEL(wbs_card_table):
    .quad 0
LOCAL_LABEL(wbs_card_bundle_table):
    .quad 0
LOCAL_LABEL(wbs_sw_ww_table):
    .quad 0
LOCAL_LABEL(wbs_ephemeral_low):
    .quad 0
LOCAL_LABEL(wbs_ephemeral_high):
    .quad 0
LOCAL_LABEL(wbs_lowest_address):
    .quad 0
LOCAL_LABEL(wbs_highest_address):
    .quad 0
#ifdef WRITE_BARRIER_CHECK
LOCAL_LABEL(wbs_GCShadow):
    .quad 0
LOCAL_LABEL(wbs_GCShadowEnd):
    .quad 0
#endif
WRITE_BARRIER_END JIT_WriteBarrier_Table


// ------------------------------------------------------------------
// End of the writeable code region
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
    ret  lr
LEAF_END JIT_PatchedCodeLast, _TEXT
