// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

// Macro used to copy contents of newly updated GC heap locations to a shadow copy of the heap. This is used
// during garbage collections to verify that object references where never written to the heap without using a
// write barrier. Note that we are potentially racing to update the shadow heap while other threads are writing
// new references to the real heap. Since this can not be solved perfectly without critical sections around the
// entire update process, we instead update the shadow location and then re-check the real location (as two
// ordered operations) and if there is a disparity we will re-write the shadow location with a special value
// (INVALIDGCVALUE) which disables the check for that location. Since the shadow heap is only validated at GC
// time and these write barrier operations are atomic wrt to GCs this is sufficient to guarantee that the
// shadow heap contains only valid copies of real heap values or INVALIDGCVALUE.
#ifdef WRITE_BARRIER_CHECK

    .global     $g_GCShadow
    .global     $g_GCShadowEnd

        // On entry:
        //  $destReg: location to be updated
        //  $refReg: objectref to be stored
        //
        // On exit:
        //  x9,x10: trashed
        //  other registers are preserved
        //
        .macro UPDATE_GC_SHADOW destReg, refReg

        // If g_GCShadow is 0, don't perform the check.
        PREPARE_EXTERNAL_VAR_INDIRECT g_GCShadow, X9
        cbz     x9, 1f

        // Save destReg since we're about to modify it (and we need the original value both within the macro and
        // once we exit the macro).
        mov     x10, \destReg

        // Transform destReg into the equivalent address in the shadow heap.
        PREPARE_EXTERNAL_VAR_INDIRECT g_lowest_address, X9
        subs    \destReg, \destReg, x9
        blo     0f

        PREPARE_EXTERNAL_VAR_INDIRECT g_GCShadow, X9
        add     \destReg, \destReg, x9

        PREPARE_EXTERNAL_VAR_INDIRECT g_GCShadowEnd, X9
        cmp     \destReg, x9
        bhs     0f

        // Update the shadow heap.
        str     \refReg, [\destReg]

        // The following read must be strongly ordered wrt to the write we have just performed in order to
        // prevent race conditions.
        dmb     ish

        // Now check that the real heap location still contains the value we just wrote into the shadow heap.
        mov     x9, x10
        ldr     x9, [x9]
        cmp     x9, \refReg
        beq     0f

        // Someone went and updated the real heap. We need to invalidate INVALIDGCVALUE the shadow location since we can not
        // guarantee whose shadow update won.
        movz x9, (INVALIDGCVALUE & 0xFFFF) // #0xcccd
        movk x9, ((INVALIDGCVALUE >> 16) & 0xFFFF), LSL #16
        str     x9, [\destReg]

0:
        // Restore original destReg value
        mov     \destReg, x10

1:
    .endm

#else // WRITE_BARRIER_CHECK

    .macro UPDATE_GC_SHADOW destReg, refReg
    .endm

#endif // WRITE_BARRIER_CHECK

// There are several different helpers used depending on which register holds the object reference. Since all
// the helpers have identical structure we use a macro to define this structure. Two arguments are taken, the
// name of the register that points to the location to be updated and the name of the register that holds the
// object reference (this should be in upper case as it is used in the definition of the name of the helper).

// Define a sub-macro first that expands to the majority of the barrier implementation. This is used below for
// some interlocked helpers that need an inline barrier.

        // On entry:
        //   destReg: location to be updated
        //   refReg:  objectref to be stored
        //   trash: register nr than can be trashed
        //
        // On exit:
        //   destReg:   trashed
        //
        .macro INSERT_UNCHECKED_WRITE_BARRIER_CORE destReg, refReg, trash

        // Update the shadow copy of the heap with the same value just written to the same heap. (A no-op unless
        // we are in a debug build and write barrier checking has been enabled).
        UPDATE_GC_SHADOW \destReg, \refReg

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        // Update the write watch table if necessary
        PREPARE_EXTERNAL_VAR_INDIRECT g_write_watch_table, x\trash

        cbz  x\trash, 2f
        add  x\trash, x\trash, \destReg, lsr #0xc  // SoftwareWriteWatch::AddressToTableByteIndexShift
        ldrb w17, [x\trash]
        cbnz x17, 2f
        mov  w17, #0xFF
        strb w17, [x\trash]
#endif

2:
        // We can skip the card table write if the reference is to
        // an object not on the epehemeral segment.
        PREPARE_EXTERNAL_VAR_INDIRECT g_ephemeral_low, x\trash
        cmp     \refReg, x\trash
        blo     0f

        PREPARE_EXTERNAL_VAR_INDIRECT g_ephemeral_high, x\trash
        cmp     \refReg, x\trash
        bhs     0f

        // Set this objects card, if it has not already been set.

        PREPARE_EXTERNAL_VAR_INDIRECT g_card_table, x\trash
        add     x17, x\trash, \destReg, lsr #11

        // Check that this card has not already been written. Avoiding useless writes is a big win on
        // multi-proc systems since it avoids cache thrashing.
        ldrb    w\trash, [x17]
        cmp     x\trash, 0xFF
        beq     0f

        mov     x\trash, 0xFF
        strb    w\trash, [x17]

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        // Check if we need to update the card bundle table
        PREPARE_EXTERNAL_VAR_INDIRECT g_card_bundle_table, x\trash
        add  x17, x\trash, \destReg, lsr #21
        ldrb w\trash, [x17]
        cmp  x\trash, 0xFF
        beq  0f

        mov  x\trash, 0xFF
        strb w\trash, [x17]
#endif

0:
        // Exit label
    .endm

        // On entry:
        //   destReg: location to be updated
        //   refReg:  objectref to be stored
        //   trash: register nr than can be trashed
        //
        // On exit:
        //   destReg:   trashed
        //
        .macro INSERT_CHECKED_WRITE_BARRIER_CORE destReg, refReg, trash

        // The "check" of this checked write barrier - is destReg
        // within the heap? if no, early out.

        PREPARE_EXTERNAL_VAR_INDIRECT g_lowest_address, x\trash
        cmp     \destReg, x\trash

        PREPARE_EXTERNAL_VAR_INDIRECT g_highest_address, x\trash

        // If \destReg >= g_lowest_address, compare \destReg to g_highest_address.
        // Otherwise, set the C flag (0x2) to take the next branch.
        ccmp    \destReg, x\trash, #0x2, hs
        bhs     0f

        INSERT_UNCHECKED_WRITE_BARRIER_CORE \destReg, \refReg, \trash

0:
        // Exit label
    .endm

// RhpCheckedAssignRef(Object** dst, Object* src)
//
// Write barrier for writes to objects that may reside
// on the managed heap.
//
// On entry:
//   x0 : the destination address (LHS of the assignment).
//        May not be an object reference (hence the checked).
//   x1 : the object reference (RHS of the assignment).
// On exit:
//   x1 : trashed
//   x9 : trashed
 LEAF_ENTRY RhpCheckedAssignRef, _TEXT
    ALTERNATE_ENTRY RhpCheckedAssignRefX1

        mov     x14, x0                     ; x14 = dst
        mov     x15, x1                     ; x15 = val
        b       C_FUNC(RhpCheckedAssignRefArm64)

LEAF_END RhpCheckedAssignRef, _TEXT

// RhpAssignRef(Object** dst, Object* src)
//
// Write barrier for writes to objects that are known to
// reside on the managed heap.
//
// On entry:
//  x0 : the destination address (LHS of the assignment).
//  x1 : the object reference (RHS of the assignment).
// On exit:
//  x1 : trashed
//  x9 : trashed
LEAF_ENTRY RhpAssignRef, _TEXT
    ALTERNATE_ENTRY RhpAssignRefX1

        mov     x14, x0                     ; x14 = dst
        mov     x15, x1                     ; x15 = val
        b       C_FUNC(RhpAssignRefArm64)

LEAF_END RhpAssignRef, _TEXT

// Interlocked operation helpers where the location is an objectref, thus requiring a GC write barrier upon
// successful updates.

// WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpCheckedLockCmpXchgAVLocation
// - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and LR contains the return address

// RhpCheckedLockCmpXchg(Object** dest, Object* value, Object* comparand)
//
// Interlocked compare exchange on objectref.
//
// On entry:
//  x0: pointer to objectref
//  x1: exchange value
//  x2: comparand
//
// On exit:
//  x0: original value of objectref
//  x9: trashed
//  x10: trashed
//
    LEAF_ENTRY RhpCheckedLockCmpXchg
    ALTERNATE_ENTRY  RhpCheckedLockCmpXchgAVLocation

CmpXchgRetry:
        // Check location value is what we expect.
        ldaxr   x10, [x0]
        cmp     x10, x2
        bne     CmpXchgNoUpdate

        // Current value matches comparand, attempt to update with the new value.
        stlxr   w9, x1, [x0]
        cbnz    w9, CmpXchgRetry

        // We have successfully updated the value of the objectref so now we need a GC write barrier.
        // The following barrier code takes the destination in x0 and the value in x1 so the arguments are
        // already correctly set up.

        INSERT_CHECKED_WRITE_BARRIER_CORE x0, x1, 9

CmpXchgNoUpdate:
        // x10 still contains the original value.
        mov     x0, x10
        InterlockedOperationBarrier
        ret     lr

    LEAF_END RhpCheckedLockCmpXchg, _TEXT

// WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen within at RhpCheckedXchgAVLocation
// - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and LR contains the return address

// RhpCheckedXchg(Object** destination, Object* value)
//
// Interlocked exchange on objectref.
//
// On entry:
//  x0: pointer to objectref
//  x1: exchange value
//
// On exit:
//  x0: original value of objectref
//  x9: trashed
//  x10: trashed
//
    LEAF_ENTRY RhpCheckedXchg, _TEXT
    ALTERNATE_ENTRY  RhpCheckedXchgAVLocation

ExchangeRetry:
        // Read the existing memory location.
        ldaxr   x10,  [x0]

        // Attempt to update with the new value.
        stlxr   w9, x1, [x0]
        cbnz    w9, ExchangeRetry

        // We have successfully updated the value of the objectref so now we need a GC write barrier.
        // The following barrier code takes the destination in x0 and the value in x1 so the arguments are
        // already correctly set up.

        INSERT_CHECKED_WRITE_BARRIER_CORE x0, x1, 9

        // x10 still contains the original value.
        mov     x0, x10
        InterlockedOperationBarrier
        ret

    LEAF_END RhpCheckedXchg, _TEXT

LEAF_ENTRY RhpAssignRefArm64, _TEXT
    ALTERNATE_ENTRY RhpAssignRefAVLocation
    ALTERNATE_ENTRY RhpAssignRefX1AVLocation
        stlr    x15, [x14]

        INSERT_UNCHECKED_WRITE_BARRIER_CORE x14, x15, 12

        ret
LEAF_END RhpAssignRefArm64, _TEXT

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
LEAF_ENTRY RhpCheckedAssignRefArm64, _TEXT
    ALTERNATE_ENTRY RhpCheckedAssignRefAVLocation
    ALTERNATE_ENTRY RhpCheckedAssignRefX1AVLocation

        stlr    x15, [x14]

        INSERT_CHECKED_WRITE_BARRIER_CORE x14, x15, 12

        ret
LEAF_END RhpCheckedAssignRefArm64, _TEXT

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
LEAF_ENTRY RhpByRefAssignRefArm64, _TEXT
    ldr     x15, [x13]
    stlr    x15, [x14]

    INSERT_CHECKED_WRITE_BARRIER_CORE x14, x15, 12

    add     X13, x13, #8
    add     x14, x14, #8

    ret
LEAF_END RhpByRefAssignRefArm64, _TEXT
