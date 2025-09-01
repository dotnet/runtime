// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.syntax unified
.thumb

#include "AsmMacros_Shared.h"

#ifdef WRITE_BARRIER_CHECK

.macro UPDATE_GC_SHADOW BASENAME, REFREG, DESTREG
        // Todo: implement, debugging helper

LOCAL_LABEL(\BASENAME\()_UpdateShadowHeap_Done_\REFREG):

.endm

#else  // WRITE_BARRIER_CHECK

.macro UPDATE_GC_SHADOW BASENAME, REFREG, DESTREG
.endm

#endif // WRITE_BARRIER_CHECK

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
.macro UPDATE_WRITE_WATCH_TABLE ptrReg, tmpReg, __wbScratch

        PREPARE_EXTERNAL_VAR_INDIRECT g_write_watch_table, \__wbScratch
        cbz \__wbScratch, 2f
        add \__wbScratch, \__wbScratch, \ptrReg, lsr #0xc  // SoftwareWriteWatch::AddressToTableByteIndexShift

        ldrb \tmpReg, [\__wbScratch]
        cmp \tmpReg, #0xff
        itt ne
        movne \tmpReg, 0xff
        strbne \tmpReg, [\__wbScratch]

2:
.endm
#else
.macro UPDATE_WRITE_WATCH_TABLE ptrReg, tmpReg, __wbScratch
.endm
#endif

// There are several different helpers used depending on which register holds the object reference. Since all
// the helpers have identical structure we use a macro to define this structure. Two arguments are taken, the
// name of the register that points to the location to be updated and the name of the register that holds the
// object reference (this should be in upper case as it's used in the definition of the name of the helper).
.macro DEFINE_UNCHECKED_WRITE_BARRIER_CORE BASENAME, REFREG, TMPREG

          // Update the shadow copy of the heap with the same value just written to the same heap. (A no-op unless
          // we're in a debug build and write barrier checking has been enabled).
          UPDATE_GC_SHADOW \BASENAME, \REFREG, r0

          UPDATE_WRITE_WATCH_TABLE r0, r12, \TMPREG

          // If the reference is to an object that's not in an ephemeral generation we have no need to track it
          // (since the object won't be collected or moved by an ephemeral collection).
          PREPARE_EXTERNAL_VAR_INDIRECT g_ephemeral_low, r12
          cmp          \REFREG, r12
          blo          LOCAL_LABEL(\BASENAME\()_EXIT_\REFREG)

          PREPARE_EXTERNAL_VAR_INDIRECT g_ephemeral_high, r12
          cmp          \REFREG, r12
          bhs          LOCAL_LABEL(\BASENAME\()_EXIT_\REFREG)


          // We have a location on the GC heap being updated with a reference to an ephemeral object so we must
          // track this write. The location address is translated into an offset in the card table bitmap. We set
          // an entire byte in the card table since it's quicker than messing around with bitmasks and we only write
          // the byte if it hasn't already been done since writes are expensive and impact scaling.
          PREPARE_EXTERNAL_VAR_INDIRECT g_card_table, r12
          add          r0, r12, r0, lsr #10
          ldrb         r12, [r0]
          cmp          r12, #0x0FF
          bne          LOCAL_LABEL(\BASENAME\()_UpdateCardTable_\REFREG)

LOCAL_LABEL(\BASENAME\()_NoBarrierRequired_\REFREG):
          b            LOCAL_LABEL(\BASENAME\()_EXIT_\REFREG)

// We get here if it's necessary to update the card table.
LOCAL_LABEL(\BASENAME\()_UpdateCardTable_\REFREG):
          mov          r12, #0x0FF
          strb         r12, [r0]

LOCAL_LABEL(\BASENAME\()_EXIT_\REFREG):

.endm

// There are several different helpers used depending on which register holds the object reference. Since all
// the helpers have identical structure we use a macro to define this structure. One argument is taken, the
// name of the register that will hold the object reference (this should be in upper case as it's used in the
// definition of the name of the helper).
.macro DEFINE_UNCHECKED_WRITE_BARRIER REFREG, EXPORT_REG_NAME

// Define a helper with a name of the form RhpAssignRefEAX etc. (along with suitable calling standard
// decoration). The location to be updated is in DESTREG. The object reference that will be assigned into that
// location is in one of the other general registers determined by the value of REFREG.

// WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at WriteBarrierFunctionAvLOC
// - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and LR contains the return address
LEAF_ENTRY RhpAssignRef\EXPORT_REG_NAME, _TEXT

// Export the canonical write barrier under unqualified name as well
.ifc \REFREG, r1
ALTERNATE_ENTRY RhpAssignRef
.endif

          // Use the GC write barrier as a convenient place to implement the managed memory model for ARM. The
          // intent is that writes to the target object ($REFREG) will be visible across all CPUs before the
          // write to the destination ($DESTREG). This covers most of the common scenarios where the programmer
          // might assume strongly ordered accessess, namely where the preceding writes are used to initialize
          // the object and the final write, made by this barrier in the instruction following the DMB,
          // publishes that object for other threads/cpus to see.
          //
          // Note that none of this is relevant for single cpu machines. We may choose to implement a
          // uniprocessor specific version of this barrier if uni-proc becomes a significant scenario again.
          dmb

          // Write the reference into the location. Note that we rely on the fact that no GC can occur between here
          // and the card table update we may perform below.
GLOBAL_LABEL "RhpAssignRefAvLocation"\EXPORT_REG_NAME  // WriteBarrierFunctionAvLocation
.ifc \REFREG, r1
GLOBAL_LABEL RhpAssignRefAVLocation
.endif
          str          \REFREG, [r0]

          DEFINE_UNCHECKED_WRITE_BARRIER_CORE RhpAssignRef, \REFREG, r3

          bx           lr
LEAF_END RhpAssignRef\EXPORT_REG_NAME, _TEXT
.endm

// One day we might have write barriers for all the possible argument registers but for now we have
// just one write barrier that assumes the input register is RSI.
DEFINE_UNCHECKED_WRITE_BARRIER r1, r1

//
// Define the helpers used to implement the write barrier required when writing an object reference into a
// location residing on the GC heap. Such write barriers allow the GC to optimize which objects in
// non-ephemeral generations need to be scanned for references to ephemeral objects during an ephemeral
// collection.
//

.macro DEFINE_CHECKED_WRITE_BARRIER_CORE BASENAME, REFREG, TMPREG

          // The location being updated might not even lie in the GC heap (a handle or stack location for instance),
          // in which case no write barrier is required.
          PREPARE_EXTERNAL_VAR_INDIRECT g_lowest_address, r12
          cmp          r0, r12
          blo          LOCAL_LABEL(\BASENAME\()_NoBarrierRequired_\REFREG)
          PREPARE_EXTERNAL_VAR_INDIRECT g_highest_address, r12
          cmp          r0, r12
          bhs          LOCAL_LABEL(\BASENAME\()_NoBarrierRequired_\REFREG)

          DEFINE_UNCHECKED_WRITE_BARRIER_CORE \BASENAME, \REFREG, \TMPREG

.endm

// There are several different helpers used depending on which register holds the object reference. Since all
// the helpers have identical structure we use a macro to define this structure. One argument is taken, the
// name of the register that will hold the object reference (this should be in upper case as it's used in the
// definition of the name of the helper).
.macro DEFINE_CHECKED_WRITE_BARRIER REFREG, EXPORT_REG_NAME

// Define a helper with a name of the form RhpCheckedAssignRefEAX etc. (along with suitable calling standard
// decoration). The location to be updated is always in R0. The object reference that will be assigned into
// that location is in one of the other general registers determined by the value of REFREG.

// WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen on the first instruction
// - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and LR contains the return address
LEAF_ENTRY RhpCheckedAssignRef\EXPORT_REG_NAME, _TEXT

// Export the canonical write barrier under unqualified name as well
.ifc \REFREG, r1
ALTERNATE_ENTRY RhpCheckedAssignRef
.endif

          // Use the GC write barrier as a convenient place to implement the managed memory model for ARM. The
          // intent is that writes to the target object ($REFREG) will be visible across all CPUs before the
          // write to the destination ($DESTREG). This covers most of the common scenarios where the programmer
          // might assume strongly ordered accessess, namely where the preceding writes are used to initialize
          // the object and the final write, made by this barrier in the instruction following the DMB,
          // publishes that object for other threads/cpus to see.
          //
          // Note that none of this is relevant for single cpu machines. We may choose to implement a
          // uniprocessor specific version of this barrier if uni-proc becomes a significant scenario again.
          dmb
          // Write the reference into the location. Note that we rely on the fact that no GC can occur between here
          // and the card table update we may perform below.
GLOBAL_LABEL "RhpCheckedAssignRefAvLocation"\EXPORT_REG_NAME // WriteBarrierFunctionAvLocation
.ifc \REFREG, r1
GLOBAL_LABEL RhpCheckedAssignRefAVLocation
.endif
          str          \REFREG, [r0]

          DEFINE_CHECKED_WRITE_BARRIER_CORE RhpCheckedAssignRef, \REFREG, r3

          bx           lr
LEAF_END RhpCheckedAssignRef\EXPORT_REG_NAME, _TEXT
.endm

// One day we might have write barriers for all the possible argument registers but for now we have
// just one write barrier that assumes the input register is RSI.
DEFINE_CHECKED_WRITE_BARRIER r1, r1

#ifdef FEATURE_NATIVEAOT

// r0 = destination address
// r1 = value
// r2 = comparand
LEAF_ENTRY RhpCheckedLockCmpXchg, _TEXT
          // To implement our chosen memory model for ARM we insert a memory barrier at GC write brriers. This
          // barrier must occur before the object reference update, so we have to do it unconditionally even
          // though the update may fail below.
          dmb
LOCAL_LABEL(RhpCheckedLockCmpXchgRetry):
          ldrex        r3, [r0]
          cmp          r2, r3
          bne          LOCAL_LABEL(RhpCheckedLockCmpXchg_NoBarrierRequired_r1)
          strex        r3, r1, [r0]
          cmp          r3, #0
          bne          LOCAL_LABEL(RhpCheckedLockCmpXchgRetry)
          mov          r3, r2

          DEFINE_CHECKED_WRITE_BARRIER_CORE RhpCheckedLockCmpXchg, r1, r2

          mov          r0, r3
          bx           lr
LEAF_END RhpCheckedLockCmpXchg, _TEXT

// r0 = destination address
// r1 = value
LEAF_ENTRY RhpCheckedXchg, _TEXT
          // To implement our chosen memory model for ARM we insert a memory barrier at GC write barriers. This
          // barrier must occur before the object reference update.
          dmb
LOCAL_LABEL(RhpCheckedXchgRetry):
          ldrex        r2, [r0]
          strex        r3, r1, [r0]
          cmp          r3, #0
          bne          LOCAL_LABEL(RhpCheckedXchgRetry)

          DEFINE_CHECKED_WRITE_BARRIER_CORE RhpCheckedXchg, r1, r3

          // The original value is currently in r2. We need to return it in r0.
          mov          r0, r2

          bx           lr
LEAF_END RhpCheckedXchg, _TEXT
#endif // FEATURE_NATIVEAOT

//
// RhpByRefAssignRef simulates movs instruction for object references.
//
// On entry:
//      r0: address of ref-field (assigned to)
//      r1: address of the data (source)
//      r2, r3: be trashed
//
// On exit:
//      r0, r1 are incremented by 4,
//      r2, r3: trashed
//
// WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpByRefAssignRefAVLocation1/2
// - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and LR contains the return address
LEAF_ENTRY RhpByRefAssignRef, _TEXT
          // See comment in RhpAssignRef
          dmb

GLOBAL_LABEL RhpByRefAssignRefAVLocation1
          ldr          r2, [r1]
GLOBAL_LABEL RhpByRefAssignRefAVLocation2
          str          r2, [r0]

          // Check whether the writes were even into the heap. If not there's no card update required.
          PREPARE_EXTERNAL_VAR_INDIRECT g_lowest_address, r3
          cmp          r0, r3
          blo          LOCAL_LABEL(RhpByRefAssignRef_NotInHeap)
          PREPARE_EXTERNAL_VAR_INDIRECT g_highest_address, r3
          cmp          r0, r3
          bhs          LOCAL_LABEL(RhpByRefAssignRef_NotInHeap)

          // Update the shadow copy of the heap with the same value just written to the same heap. (A no-op unless
          // we're in a debug build and write barrier checking has been enabled).
          UPDATE_GC_SHADOW BASENAME, r2, r0

          UPDATE_WRITE_WATCH_TABLE r0, r12, r3

          // If the reference is to an object that's not in an ephemeral generation we have no need to track it
          // (since the object won't be collected or moved by an ephemeral collection).
          PREPARE_EXTERNAL_VAR_INDIRECT g_ephemeral_low, r3
          cmp          r2, r3
          blo          LOCAL_LABEL(RhpByRefAssignRef_NotInHeap)
          PREPARE_EXTERNAL_VAR_INDIRECT g_ephemeral_high, r3
          cmp          r2, r3
          bhs          LOCAL_LABEL(RhpByRefAssignRef_NotInHeap)

          // move current r0 value into r2 and then increment the pointers
          mov          r2, r0
          add          r1, #4
          add          r0, #4

          // We have a location on the GC heap being updated with a reference to an ephemeral object so we must
          // track this write. The location address is translated into an offset in the card table bitmap. We set
          // an entire byte in the card table since it's quicker than messing around with bitmasks and we only write
          // the byte if it hasn't already been done since writes are expensive and impact scaling.
          PREPARE_EXTERNAL_VAR_INDIRECT g_card_table, r3
          add           r2, r3, r2, lsr #10
          ldrb          r3, [r2]
          cmp           r3, #0x0FF
          bne           LOCAL_LABEL(RhpByRefAssignRef_UpdateCardTable)
          bx            lr

// We get here if it's necessary to update the card table.
LOCAL_LABEL(RhpByRefAssignRef_UpdateCardTable):
          mov           r3, #0x0FF
          strb          r3, [r2]
          bx            lr

LOCAL_LABEL(RhpByRefAssignRef_NotInHeap):
          // Increment the pointers before leaving
          add           r0, #4
          add           r1, #4
          bx            lr
LEAF_END RhpByRefAssignRef, _TEXT
