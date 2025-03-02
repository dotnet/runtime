// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>

// Macro used to copy contents of newly updated GC heap locations to a shadow copy of the heap. This is used
// during garbage collections to verify that object references were never written to the heap without using a
// write barrier. Note that we are potentially racing to update the shadow heap while other threads are writing
// new references to the real heap. Since this cannot be solved perfectly without critical sections around the
// entire update process, we instead update the shadow location and then re-check the real location (as two
// ordered operations) and if there is a disparity we will re-write the shadow location with a special value
// (INVALIDGCVALUE) which disables the check for that location. Since the shadow heap is only validated at GC
// time and these write barrier operations are atomic wrt to GCs this is sufficient to guarantee that the
// shadow heap contains only valid copies of real heap values or INVALIDGCVALUE.
#ifdef WRITE_BARRIER_CHECK

    .global     g_GCShadow
    .global     g_GCShadowEnd

        // On entry:
        //  destReg: location to be updated
        //  refReg: objectref to be stored
        //
        // On exit:
        //  t3,t4: trashed
        //  other registers are preserved
        //
        .macro UPDATE_GC_SHADOW destReg, refReg

        // If g_GCShadow is 0, don't perform the check.
        la    t3, g_GCShadow
        ld    t3, 0(t3)
        beq  t3, zero, 1f

        // Save destReg since we're about to modify it (and we need the original value both within the macro and
        // once we exit the macro).
        mv   t4, \destReg

        // Transform destReg into the equivalent address in the shadow heap.
        la    t3, g_lowest_address
        ld    t3, 0(t3)
        sub  \destReg, \destReg, t3
        bltz \destReg, 0f

        la    t3, g_GCShadow
        ld    t3, 0(t3)
        add  \destReg, \destReg, t3

        la    t3, g_GCShadowEnd
        ld    t3, 0(t3)
        bgeu \destReg, t3, 0f

        // Update the shadow heap.
        sd   \refReg, 0(\destReg)

        // The following read must be strongly ordered with respect to the write we have just performed in order to
        // prevent race conditions.
        fence rw, rw

        // Now check that the real heap location still contains the value we just wrote into the shadow heap.
        mv   t3, t4
        ld   t3, 0(t3)
        beq  t3, \refReg, 0f

        // Someone went and updated the real heap. We need to invalidate INVALIDGCVALUE the shadow location since we cannot
        // guarantee whose shadow update won.
        li   t3, INVALIDGCVALUE
        sd   t3, 0(\destReg)

0:
        // Restore original destReg value
        mv   \destReg, t4

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
        //   destReg: location to be updated (cannot be t2,t6)
        //   refReg:  objectref to be stored (cannot be t2,t6)
        //
        // On exit:
        //   t2, t6: trashed
        //
        .macro INSERT_UNCHECKED_WRITE_BARRIER_CORE destReg, refReg

        // Update the shadow copy of the heap with the same value just written to the same heap.
        // (A no-op unless we are in a debug build and write barrier checking has been enabled).
        UPDATE_GC_SHADOW \destReg, \refReg

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        // Update the write watch table if necessary
        la      t2, g_write_watch_table
        ld      t2, (t2)

        beqz    t2, 2f
        srli    t6, \destReg, 12   // SoftwareWriteWatch::AddressToTableByteIndexShift
        add     t2, t2, t6
        lb      t6, 0(t2)
        bnez    t6, 2f
        li      t6, 0xFF
        sb      t6, 0(t2)
#endif

2:
        // We can skip the card table write if the reference is to
        // an object not on the ephemeral segment.
        la      t2, g_ephemeral_low
        ld      t2, (t2)
        la      t6, g_ephemeral_high
        ld      t6, (t6)
        bltu    \refReg, t2, 0f
        bgeu    \refReg, t6, 0f

        // Set this object's card, if it has not already been set.
        la      t2, g_card_table
        ld      t2, (t2)
        srli    t6, \destReg, 11
        add     t6, t2, t6

        // Check that this card has not already been written. Avoiding useless writes
        // is a big win on multi-proc systems since it avoids cache thrashing.
        lbu     t2, 0(t6)
        addi    t2, t2, -0xFF
        beqz    t2, 0f

        li      t2, 0xFF
        sb      t2, 0(t6)

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        // Check if we need to update the card bundle table
        la      t2, g_card_bundle_table
        ld      t2, (t2)

        srli    t6, \destReg, 21
        add     t6, t2, t6
        lbu     t2, 0(t6)
        addi    t2, t2, -0xFF
        beqz    t2, 0f

        li      t2, 0xFF
        sb      t2, 0(t6)
#endif

0:
        // Exit label
    .endm

        // On entry:
        //   destReg: location to be updated
        //   refReg:  objectref to be stored
        //
        // On exit:
        //   t2, t6: trashed
        //
        .macro INSERT_CHECKED_WRITE_BARRIER_CORE destReg, refReg

        // The "check" of this checked write barrier - is destReg within the heap?
        // If no, early out.

        la      t2, g_lowest_address
        ld      t2, (t2)
        bltu    \destReg, t2, 0f

        la      t2, g_highest_address
        ld      t2, (t2)
        bgeu    \destReg, t2, 0f

1:
        INSERT_UNCHECKED_WRITE_BARRIER_CORE \destReg, \refReg

0:
        // Exit label
    .endm

// void JIT_ByRefWriteBarrier
// On entry:
//   t5  : the source address (points to object reference to write)
//   t3  : the destination address (object reference written here)
//
// On exit:
//   t5  : incremented by 8
//   t3  : incremented by 8
//   t4  : trashed
//   t2, t3  : trashed
//
//   NOTE: Keep in sync with RBM_CALLEE_TRASH_WRITEBARRIER_BYREF and RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF
//         if you add more trashed registers.
//
// WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpByRefAssignRefAVLocation1
// - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and RA contains the return address
LEAF_ENTRY RhpByRefAssignRef, _TEXT

    ALTERNATE_ENTRY RhpByRefAssignRefAVLocation1
        ld    t4, 0(t5)
        addi  t5, t5, 8
        j C_FUNC(RhpCheckedAssignRef)

LEAF_END RhpByRefAssignRef, _TEXT

// JIT_CheckedWriteBarrier(Object** dst, Object* src)
//
// Write barrier for writes to objects that may reside
// on the managed heap.
//
// On entry:
//   t3 : the destination address (LHS of the assignment).
//         May not be a heap location (hence the checked).
//   t4 : the object reference (RHS of the assignment).
//
// On exit:
//   t2, t6 : trashed
//   t3      : incremented by 8
LEAF_ENTRY RhpCheckedAssignRef, _TEXT

        # Check if the destination is within the heap bounds
        la      t2, C_FUNC(g_lowest_address)
        ld      t2, (t2)
        la      t6, C_FUNC(g_highest_address)
        ld      t6, (t6)

        bltu    t3, t2, LOCAL_LABEL(NotInHeap)
        bgeu    t3, t6, LOCAL_LABEL(NotInHeap)

        j       C_FUNC(RhpAssignRefRiscV64)

LOCAL_LABEL(NotInHeap):
        ALTERNATE_ENTRY RhpCheckedAssignRefAVLocation
        sd      t4, 0(t3)
        addi    t3, t3, 8

        ret

LEAF_END RhpCheckedAssignRef, _TEXT

// JIT_WriteBarrier(Object** dst, Object* src)
//
// Write barrier for writes to objects that are known to
// reside on the managed heap.
//
// On entry:
//  t3 : the destination address (LHS of the assignment).
//  t4 : the object reference (RHS of the assignment).
//
// On exit:
//  t2, t6 : trashed
//  t3 : incremented by 8
LEAF_ENTRY RhpAssignRefRiscV64, _TEXT
        fence rw, rw

    ALTERNATE_ENTRY RhpAssignRefAVLocation
        sd    t4, 0(t3)

        INSERT_UNCHECKED_WRITE_BARRIER_CORE t3, t4

        addi  t3, t3, 8

        ret

LEAF_END RhpAssignRefRiscV64, _TEXT

// Same as RhpAssignRefRiscV64, but with standard ABI.
LEAF_ENTRY RhpAssignRef, _TEXT
        mv   t3, a0                    ; t3 = dst
        mv   t4, a1                    ; t4 = val
        mv   a1, ra
        j    C_FUNC(RhpAssignRefRiscV64)
LEAF_END RhpAssignRef, _TEXT


// Interlocked operation helpers where the location is an objectref, thus requiring a GC write barrier upon
// successful updates.

// WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen within at RhpCheckedLockCmpXchgAVLocation
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
//  t0, t1, t2, t6: trashed
//
LEAF_ENTRY RhpCheckedLockCmpXchg

LOCAL_LABEL(CmpXchgRetry):
        // Load the current value at the destination address.
        lr.d.aqrl    t0, (a0)       // t0 = *dest (load with acquire-release ordering)
        // Compare the loaded value with the comparand.
        bne     t0, a2, LOCAL_LABEL(CmpXchgNoUpdate) // if (*dest != comparand) goto CmpXchgNoUpdate

        // Attempt to store the exchange value at the destination address.
        sc.d.rl    t1, a1, (a0)  // t1 = (store conditional result: 0 if successful, with release ordering)
        bnez    t1, LOCAL_LABEL(CmpXchgRetry) // if store conditional failed, retry

        // See comment at the top of PalInterlockedOperationBarrier method for explanation why this memory
        // barrier is necessary.
        fence   rw, rw

LOCAL_LABEL(DoCardsCmpXchg):
        // We have successfully updated the value of the objectref so now we need a GC write barrier.
        // The following barrier code takes the destination in a0 and the value in a1 so the arguments are
        // already correctly set up.
        INSERT_CHECKED_WRITE_BARRIER_CORE a0, a1

LOCAL_LABEL(CmpXchgNoUpdate):
        // t0 still contains the original value.
        mv      a0, t0

        ret

LEAF_END RhpCheckedLockCmpXchg

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
//  t1, t6: trashed
//
LEAF_ENTRY RhpCheckedXchg
        amoswap.d.aqrl t1, a1, (a0)

        // See comment at the top of PalInterlockedOperationBarrier method for explanation why this memory
        // barrier is necessary.
        fence rw, rw

DoCardsXchg:
        // We have successfully updated the value of the objectref so now we need a GC write barrier.
        // The following barrier code takes the destination in a0 and the value in a1 so the arguments are
        // already correctly set up.

        INSERT_CHECKED_WRITE_BARRIER_CORE a0, a1

        // t1 still contains the original value.
        mv   a0, t1

        jalr ra

LEAF_END RhpCheckedXchg, _TEXT
