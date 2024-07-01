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
        //  t3,t4: trashed
        //  other registers are preserved
        //
        .macro UPDATE_GC_SHADOW destReg, refReg

        // If g_GCShadow is 0, don't perform the check.
        PREPARE_EXTERNAL_VAR_INDIRECT g_GCShadow, $t3
        beq  $t3, $zero, 1f
        ori  $t4, $t3, 0

        // Save destReg since we're about to modify it (and we need the original value both within the macro and
        // once we exit the macro).
        ori  $t4, \destReg, 0

        // Transform destReg into the equivalent address in the shadow heap.
        PREPARE_EXTERNAL_VAR_INDIRECT g_lowest_address, $t3
        sub.d  \destReg, \destReg, $t3
        bltu  $t4, $zero, 0f

        PREPARE_EXTERNAL_VAR_INDIRECT g_GCShadow, $t3
        add.d  \destReg, \destReg, $t3

        PREPARE_EXTERNAL_VAR_INDIRECT g_GCShadowEnd, $t3
        bgeu  \destReg, $t3, 0f

        // Update the shadow heap.
        st.d  \refReg, \destReg, 0

        // The following read must be strongly ordered wrt to the write we have just performed in order to
        // prevent race conditions.
        dbar  0

        // Now check that the real heap location still contains the value we just wrote into the shadow heap.
        ori  $t3, $t4, 0
        ld.d  $t3, $t3, 0
        beq  $t3, \refReg, 0f

        // Someone went and updated the real heap. We need to invalidate INVALIDGCVALUE the shadow location since we can not
        // guarantee whose shadow update won.
        lu12i.w  $t3, ((INVALIDGCVALUE >> 12) & 0xFFFFF)
        ori  $t3, $t3, (INVALIDGCVALUE & 0xFFF)
        st.d  $t3, \destReg, 0

0:
        // Restore original destReg value
        ori  \destReg, $t4, 0

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
        //   destReg: location to be updated (cannot be t3,t4)
        //   refReg:  objectref to be stored (cannot be t3,t4)
        //
        // On exit:
        //   t3,t4: trashed
        //
        .macro INSERT_UNCHECKED_WRITE_BARRIER_CORE destReg, refReg

        // Update the shadow copy of the heap with the same value just written to the same heap. (A no-op unless
        // we are in a debug build and write barrier checking has been enabled).
        UPDATE_GC_SHADOW \destReg, \refReg

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        // Update the write watch table if necessary
        PREPARE_EXTERNAL_VAR_INDIRECT g_write_watch_table, $t3

        beq  $t3, $zero, 2f
        srli.d  $t5, \destReg, 12
        add.d  $t3, $t3, $t5      // SoftwareWriteWatch::AddressToTableByteIndexShift
        ld.b  $t4, $t3, 0
        bne  $t4, $zero, 2f
        ori  $t4, $zero, 0xFF
        st.b  $t4, $t3, 0
#endif

2:
        // We can skip the card table write if the reference is to
        // an object not on the epehemeral segment.
        PREPARE_EXTERNAL_VAR_INDIRECT g_ephemeral_low, $t3
        bltu  \refReg, $t3, 0f

        PREPARE_EXTERNAL_VAR_INDIRECT g_ephemeral_high, $t3
        bgeu  \refReg, $t3, 0f

        // Set this objects card, if it has not already been set.
        PREPARE_EXTERNAL_VAR_INDIRECT g_card_table, $t3
        srli.d  $t5, \destReg, 11
        add.d  $t4, $t3, $t5

        // Check that this card has not already been written. Avoiding useless writes is a big win on
        // multi-proc systems since it avoids cache thrashing.
        ld.b  $t3, $t4, 0
        ori  $t5, $zero, 0xFF
        beq  $t3, $t5, 0f

        ori  $t3, $zero, 0xFF
        st.b  $t3, $t4, 0

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        // Check if we need to update the card bundle table
        PREPARE_EXTERNAL_VAR_INDIRECT g_card_bundle_table, $t3
        srli.d  $t5, \destReg, 21
        add.d  $t4, $t3, $t5
        ld.b  $t3, $t4, 0
        ori  $t5, $zero, 0xFF
        beq  $t3, $t5, 0f

        ori  $t3, $zero, 0xFF
        st.b  $t3, $t4, 0
#endif

0:
        // Exit label
    .endm

        // On entry:
        //   destReg: location to be updated
        //   refReg:  objectref to be stored
        //
        // On exit:
        //   t3, t4:   trashed
        //
        .macro INSERT_CHECKED_WRITE_BARRIER_CORE destReg, refReg

        // The "check" of this checked write barrier - is destReg
        // within the heap? if no, early out.
        PREPARE_EXTERNAL_VAR_INDIRECT g_lowest_address, $t3
        sltu  $t4, \destReg, $t3

        PREPARE_EXTERNAL_VAR_INDIRECT g_highest_address, $t3

        // If \destReg >= g_lowest_address, compare \destReg to g_highest_address.
        // Otherwise, set the C flag (0x2) to take the next branch.
        bnez  $t4, 1f
        bgeu  \destReg, $t3, 0f

1:
        INSERT_UNCHECKED_WRITE_BARRIER_CORE \destReg, \refReg

0:
        // Exit label
    .endm

// void JIT_ByRefWriteBarrier
// On entry:
//   t8  : the source address (points to object reference to write)
//   t6  : the destination address (object reference written here)
//
// On exit:
//   t8  : incremented by 8
//   t6  : incremented by 8
//   t7  : trashed
//   t3, t4  : trashed
//
//   NOTE: Keep in sync with RBM_CALLEE_TRASH_WRITEBARRIER_BYREF and RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF
//         if you add more trashed registers.
//
// WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpByRefAssignRefAVLocation1
// - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and RA contains the return address
LEAF_ENTRY RhpByRefAssignRef, _TEXT

    ALTERNATE_ENTRY RhpByRefAssignRefAVLocation1
        ld.d  $t7, $t8, 0
        addi.d  $t8, $t8, 8
        b  C_FUNC(RhpCheckedAssignRef)

LEAF_END RhpByRefAssignRef, _TEXT

// JIT_CheckedWriteBarrier(Object** dst, Object* src)
//
// Write barrier for writes to objects that may reside
// on the managed heap.
//
// On entry:
//   t6 : the destination address (LHS of the assignment).
//         May not be a heap location (hence the checked).
//   t7 : the object reference (RHS of the assignment).
//
// On exit:
//   t3, t4 : trashed
//   t6      : incremented by 8
 LEAF_ENTRY RhpCheckedAssignRef, _TEXT

        // is destReg within the heap?
        PREPARE_EXTERNAL_VAR_INDIRECT g_lowest_address, $t3
        sltu  $t4, $t6, $t3

        PREPARE_EXTERNAL_VAR_INDIRECT g_highest_address, $t3
        sltu  $t0, $t3, $t6
        or  $t4, $t0, $t4
        beq  $t4, $zero, C_FUNC(RhpAssignRefLoongArch64)

NotInHeap:
    ALTERNATE_ENTRY RhpCheckedAssignRefAVLocation
        st.d  $t7, $t6, 0
        addi.d $t6, $t6, 8
        jirl  $r0, $ra, 0

LEAF_END RhpCheckedAssignRef, _TEXT

// JIT_WriteBarrier(Object** dst, Object* src)
//
// Write barrier for writes to objects that are known to
// reside on the managed heap.
//
// On entry:
//  t6 : the destination address (LHS of the assignment).
//  t7 : the object reference (RHS of the assignment).
//
// On exit:
//  t3, t4 : trashed
//  t6 : incremented by 8
LEAF_ENTRY RhpAssignRefLoongArch64, _TEXT

    ALTERNATE_ENTRY RhpAssignRefAVLocation
        st.d  $t7, $t6, 0

        INSERT_UNCHECKED_WRITE_BARRIER_CORE  $t6, $t7

        addi.d  $t6, $t6, 8
        jirl  $r0, $ra, 0

LEAF_END RhpAssignRefLoongArch64, _TEXT

// Same as RhpAssignRefLoongArch64, but with standard ABI.
LEAF_ENTRY RhpAssignRef, _TEXT
        ori  $t6, $a0, 0                    ; t6 = dst
        ori  $t7, $a1, 0                    ; t7 = val
        b  C_FUNC(RhpAssignRefLoongArch64)
LEAF_END RhpAssignRef, _TEXT


// Interlocked operation helpers where the location is an objectref, thus requiring a GC write barrier upon
// successful updates.

// WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpCheckedLockCmpXchgAVLocation
// - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and RA contains the return address

// RhpCheckedLockCmpXchg(Object** dest, Object* value, Object* comparand)
//
// Interlocked compare exchange on objectref.
//
// On entry:
//  a0: pointer to objectref
//  a1: exchange value
//  a2: comparand
//
// On exit:
//  a0: original value of objectref
//  t1, t3, t6, t4: trashed
//
    LEAF_ENTRY RhpCheckedLockCmpXchg

        ori  $t1, $a2, 0
        ld.d  $t0, $a0, 0
        beq  $t0, $t1, 12
        ori  $t1, $t0, 0
        b  EndOfExchange
        st.d  $a1, $a0, 0

EndOfExchange:
        bne  $a2, $t1, CmpXchgNoUpdate

DoCardsCmpXchg:
        // We have successfully updated the value of the objectref so now we need a GC write barrier.
        // The following barrier code takes the destination in $a0 and the value in $a1 so the arguments are
        // already correctly set up.

        INSERT_CHECKED_WRITE_BARRIER_CORE  $a0, $a1

CmpXchgNoUpdate:
        // t1 still contains the original value.
        ori  $a0, $t1, 0

        jirl  $r0, $ra, 0

    LEAF_END RhpCheckedLockCmpXchg, _TEXT

// WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen within at RhpCheckedXchgAVLocation
// - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and RA contains the return address

// RhpCheckedXchg(Object** destination, Object* value)
//
// Interlocked exchange on objectref.
//
// On entry:
//  a0: pointer to objectref
//  a1: exchange value
//
// On exit:
//  a0: original value of objectref
//  t1: trashed
//  t3, t6, t4: trashed
//
    LEAF_ENTRY RhpCheckedXchg, _TEXT

        ld.d  $t1, $a0, 0
        st.d  $a1, $a0, 0

DoCardsXchg:
        // We have successfully updated the value of the objectref so now we need a GC write barrier.
        // The following barrier code takes the destination in $a0 and the value in $a1 so the arguments are
        // already correctly set up.

        INSERT_CHECKED_WRITE_BARRIER_CORE  $a0, $a1

        // $t1 still contains the original value.
        ori  $a0, $t1, 0

        jirl  $r0, $ra, 0

    LEAF_END RhpCheckedXchg, _TEXT
