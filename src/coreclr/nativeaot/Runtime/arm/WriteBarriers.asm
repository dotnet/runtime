;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

;;
;; Define the helpers used to implement the write barrier required when writing an object reference into a
;; location residing on the GC heap. Such write barriers allow the GC to optimize which objects in
;; non-ephemeral generations need to be scanned for references to ephemeral objects during an ephemeral
;; collection.
;;

#include "AsmMacros.h"

        TEXTAREA

;; Macro used to copy contents of newly updated GC heap locations to a shadow copy of the heap. This is used
;; during garbage collections to verify that object references where never written to the heap without using a
;; write barrier. Note that we're potentially racing to update the shadow heap while other threads are writing
;; new references to the real heap. Since this can't be solved perfectly without critical sections around the
;; entire update process, we instead update the shadow location and then re-check the real location (as two
;; ordered operations) and if there is a disparity we'll re-write the shadow location with a special value
;; (INVALIDGCVALUE) which disables the check for that location. Since the shadow heap is only validated at GC
;; time and these write barrier operations are atomic wrt to GCs this is sufficient to guarantee that the
;; shadow heap contains only valid copies of real heap values or INVALIDGCVALUE.
#ifdef WRITE_BARRIER_CHECK

    SETALIAS    g_GCShadow, ?g_GCShadow@@3PAEA
    SETALIAS    g_GCShadowEnd, ?g_GCShadowEnd@@3PAEA

    EXTERN      $g_GCShadow
    EXTERN      $g_GCShadowEnd

    MACRO
        ;; On entry:
        ;;  $DESTREG: location to be updated
        ;;  $REFREG: objectref to be stored
        ;;
        ;; On exit:
        ;;  r12: trashed
        ;;  other registers are preserved
        ;;
        UPDATE_GC_SHADOW $DESTREG, $REFREG

        ;; If g_GCShadow is 0, don't perform the check.
        ldr     r12, =$g_GCShadow
        ldr     r12, [r12]
        cmp     r12, 0
        beq     %ft1

        ;; Save $DESTREG since we're about to modify it (and we need the original value both within the macro and
        ;; once we exit the macro). Note that this is naughty since we're altering the stack pointer outside of
        ;; the prolog inside a method without a frame. But given that this is only debug code and generally we
        ;; shouldn't be walking the stack at this point it seems preferable to recoding the all the barrier
        ;; variants to set up frames. The compiler knows exactly which registers are trashed in the simple write
        ;; barrier case, so we don't have any more scratch registers to play with (and doing so would only make
        ;; things harder if at a later stage we want to allow multiple barrier versions based on the input
        ;; registers).
        push    $DESTREG

        ;; Transform $DESTREG into the equivalent address in the shadow heap.
        ldr     r12, =$G_LOWEST_ADDRESS
        ldr     r12, [r12]
        subs    $DESTREG, r12
        blo     %ft0

        ldr     r12, =$g_GCShadow
        ldr     r12, [r12]
        add     $DESTREG, r12

        ldr     r12, =$g_GCShadowEnd
        ldr     r12, [r12]
        cmp     $DESTREG, r12
        bhs     %ft0

        ;; Update the shadow heap.
        str     $REFREG, [$DESTREG]

        ;; The following read must be strongly ordered wrt to the write we've just performed in order to
        ;; prevent race conditions.
        dmb

        ;; Now check that the real heap location still contains the value we just wrote into the shadow heap.
        ldr     r12, [sp]
        ldr     r12, [r12]
        cmp     r12, $REFREG
        beq     %ft0

        ;; Someone went and updated the real heap. We need to invalidate the shadow location since we can't
        ;; guarantee whose shadow update won.
        movw    r12, #0xcccd
        movt    r12, #0xcccc
        str     r12, [$DESTREG]

0
        ;; Restore original $DESTREG value from the stack.
        pop     $DESTREG

1
    MEND

#else // WRITE_BARRIER_CHECK

    MACRO
        UPDATE_GC_SHADOW $DESTREG, $REFREG
    MEND

#endif // WRITE_BARRIER_CHECK

;; There are several different helpers used depending on which register holds the object reference. Since all
;; the helpers have identical structure we use a macro to define this structure. Two arguments are taken, the
;; name of the register that points to the location to be updated and the name of the register that holds the
;; object reference (this should be in upper case as it's used in the definition of the name of the helper).

;; Define a sub-macro first that expands to the majority of the barrier implementation. This is used below for
;; some interlocked helpers that need an inline barrier.
    MACRO
        ;; On entry:
        ;;  $DESTREG: location to be updated
        ;;  $REFREG: objectref to be stored
        ;;
        ;; On exit:
        ;;  $DESTREG, r12: trashed
        ;;  other registers are preserved
        ;;
        INSERT_CHECKED_WRITE_BARRIER_CORE  $DESTREG, $REFREG

        ;; The location being updated might not even lie in the GC heap (a handle or stack location for
        ;; instance), in which case no write barrier is required.
        ldr     r12, =$G_LOWEST_ADDRESS
        ldr     r12, [r12]
        cmp     $DESTREG, r12
        blo     %ft0
        ldr     r12, =$G_HIGHEST_ADDRESS
        ldr     r12, [r12]
        cmp     $DESTREG, r12
        bhs     %ft0

        ;; Update the shadow copy of the heap with the same value just written to the same heap. (A no-op unless
        ;; we're in a debug build and write barrier checking has been enabled).
        UPDATE_GC_SHADOW $DESTREG, $REFREG

        ;; If the reference is to an object that's not in an ephemeral generation we have no need to track it
        ;; (since the object won't be collected or moved by an ephemeral collection).
        ldr     r12, =$G_EPHEMERAL_LOW
        ldr     r12, [r12]
        cmp     $REFREG, r12
        blo     %ft0
        ldr     r12, =$G_EPHEMERAL_HIGH
        ldr     r12, [r12]
        cmp     $REFREG, r12
        bhs     %ft0

        ;; All tests pass, so update the card table.
        ldr     r12, =$G_CARD_TABLE
        ldr     r12, [r12]
        add     r12, r12, $DESTREG, lsr #10

        ;; Check that this card hasn't already been written. Avoiding useless writes is a big win on
        ;; multi-proc systems since it avoids cache thrashing.
        ldrb    $DESTREG, [r12]
        cmp     $DESTREG, #0xFF
        beq     %ft0
        mov     $DESTREG, #0xFF
        strb    $DESTREG, [r12]

0

    MEND

;; There are several different helpers used depending on which register holds the object reference. Since all
;; the helpers have identical structure we use a macro to define this structure. Two arguments are taken, the
;; name of the register that points to the location to be updated and the name of the register that holds the
;; object reference (this should be in upper case as it's used in the definition of the name of the helper).

;; Define a sub-macro first that expands to the majority of the barrier implementation. This is used below for
;; some interlocked helpers that need an inline barrier.
    MACRO
        ;; On entry:
        ;;  $DESTREG: location to be updated
        ;;  $REFREG: objectref to be stored
        ;;
        ;; On exit:
        ;;  $DESTREG, r12: trashed
        ;;  other registers are preserved
        ;;
        INSERT_UNCHECKED_WRITE_BARRIER_CORE  $DESTREG, $REFREG

        ;; Update the shadow copy of the heap with the same value just written to the same heap. (A no-op unless
        ;; we're in a debug build and write barrier checking has been enabled).
        UPDATE_GC_SHADOW $DESTREG, $REFREG

        ;; If the reference is to an object that's not in an ephemeral generation we have no need to track it
        ;; (since the object won't be collected or moved by an ephemeral collection).
        ldr     r12, =$G_EPHEMERAL_LOW
        ldr     r12, [r12]
        cmp     $REFREG, r12
        blo     %ft0
        ldr     r12, =$G_EPHEMERAL_HIGH
        ldr     r12, [r12]
        cmp     $REFREG, r12
        bhs     %ft0

        ;; All tests pass, so update the card table.
        ldr     r12, =$G_CARD_TABLE
        ldr     r12, [r12]
        add     r12, r12, $DESTREG, lsr #10

        ;; Check that this card hasn't already been written. Avoiding useless writes is a big win on
        ;; multi-proc systems since it avoids cache thrashing.
        ldrb    $DESTREG, [r12]
        cmp     $DESTREG, #0xFF
        beq     %ft0
        mov     $DESTREG, #0xFF
        strb    $DESTREG, [r12]

0

    MEND

    MACRO
        ;; Define a helper with a name of the form RhpCheckedAssignRefR0 etc. The location to be updated is in
        ;; $DESTREG. The object reference that will be assigned into that location is in one of the other
        ;; general registers determined by the value of $REFREG. R12 is used as a scratch register.

        ;; WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
        ;; - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at WriteBarrierFunctionAvLocation
        ;; - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and LR contains the return address

        DEFINE_CHECKED_WRITE_BARRIER  $DESTREG, $REFREG

        gbls WriteBarrierFunction
        gbls WriteBarrierFunctionAvLocation
WriteBarrierFunction SETS "RhpCheckedAssignRef":cc:"$REFREG"
WriteBarrierFunctionAvLocation SETS "RhpCheckedAssignRefAvLocation":cc:"$REFREG"

        EXPORT $WriteBarrierFunction
$WriteBarrierFunction

        ;; Export the canonical write barrier under unqualified name as well
        IF "$REFREG" == "R1"
        ALTERNATE_ENTRY RhpCheckedAssignRef
        ENDIF

        ;; Use the GC write barrier as a convenient place to implement the managed memory model for ARM. The
        ;; intent is that writes to the target object ($REFREG) will be visible across all CPUs before the
        ;; write to the destination ($DESTREG). This covers most of the common scenarios where the programmer
        ;; might assume strongly ordered accessess, namely where the preceding writes are used to initialize
        ;; the object and the final write, made by this barrier in the instruction following the DMB,
        ;; publishes that object for other threads/cpus to see.
        ;;
        ;; Note that none of this is relevant for single cpu machines. We may choose to implement a
        ;; uniprocessor specific version of this barrier if uni-proc becomes a significant scenario again.
        dmb

        ;; Write the reference into the location. Note that we rely on the fact that no GC can occur between
        ;; here and the card table update we may perform below.
        ALTERNATE_ENTRY $WriteBarrierFunctionAvLocation
        IF "$REFREG" == "R1"
        ALTERNATE_ENTRY RhpCheckedAssignRefAVLocation
        ENDIF
        str     $REFREG, [$DESTREG]

        INSERT_CHECKED_WRITE_BARRIER_CORE $DESTREG, $REFREG

        bx      lr

    MEND


    MACRO
        ;; Define a helper with a name of the form RhpAssignRefR0 etc. The location to be updated is in
        ;; $DESTREG. The object reference that will be assigned into that location is in one of the other
        ;; general registers determined by the value of $REFREG. R12 is used as a scratch register.

        ;; WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
        ;; - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at WriteBarrierFunctionAvLocation
        ;; - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and LR contains the return address

        DEFINE_UNCHECKED_WRITE_BARRIER  $DESTREG, $REFREG

        gbls WriteBarrierFunction
        gbls WriteBarrierFunctionAvLocation
WriteBarrierFunction SETS "RhpAssignRef":cc:"$REFREG"
WriteBarrierFunctionAvLocation SETS "RhpAssignRefAvLocation":cc:"$REFREG"

        ;; Export the canonical write barrier under unqualified name as well
        IF "$REFREG" == "R1"
        ALTERNATE_ENTRY RhpAssignRef
        ENDIF

        EXPORT $WriteBarrierFunction
$WriteBarrierFunction

        ;; Use the GC write barrier as a convenient place to implement the managed memory model for ARM. The
        ;; intent is that writes to the target object ($REFREG) will be visible across all CPUs before the
        ;; write to the destination ($DESTREG). This covers most of the common scenarios where the programmer
        ;; might assume strongly ordered accessess, namely where the preceding writes are used to initialize
        ;; the object and the final write, made by this barrier in the instruction following the DMB,
        ;; publishes that object for other threads/cpus to see.
        ;;
        ;; Note that none of this is relevant for single cpu machines. We may choose to implement a
        ;; uniprocessor specific version of this barrier if uni-proc becomes a significant scenario again.
        dmb

        ;; Write the reference into the location. Note that we rely on the fact that no GC can occur between
        ;; here and the card table update we may perform below.
        ALTERNATE_ENTRY $WriteBarrierFunctionAvLocation
        IF "$REFREG" == "R1"
        ALTERNATE_ENTRY RhpAssignRefAVLocation
        ENDIF
        str     $REFREG, [$DESTREG]

        INSERT_UNCHECKED_WRITE_BARRIER_CORE $DESTREG, $REFREG

        bx      lr

    MEND

;; One day we might have write barriers for all the possible argument registers but for now we have
;; just one write barrier that assumes the input register is R1.
        DEFINE_CHECKED_WRITE_BARRIER R0, R1

        DEFINE_UNCHECKED_WRITE_BARRIER R0, R1

;; Interlocked operation helpers where the location is an objectref, thus requiring a GC write barrier upon
;; successful updates.

;; WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
;; - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpCheckedLockCmpXchgAVLocation
;; - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and LR contains the return address

        ;; Interlocked compare exchange on objectref.
        ;;
        ;; On entry:
        ;;  r0: pointer to objectref
        ;;  r1: exchange value
        ;;  r2: comparand
        ;;
        ;; On exit:
        ;;  r0: original value of objectref
        ;;  r1,r2,r3,r12: trashed
        ;;
        LEAF_ENTRY RhpCheckedLockCmpXchg

        ;; To implement our chosen memory model for ARM we insert a memory barrier at GC write barriers. This
        ;; barrier must occur before the object reference update, so we have to do it unconditionally even
        ;; though the update may fail below.
        dmb

CX_Retry
        ;; Check location value is what we expect.
        ALTERNATE_ENTRY  RhpCheckedLockCmpXchgAVLocation
        ldrex   r3, [r0]
        cmp     r2, r3
        bne     CX_NoUpdate

        ;; Current value matches comparand, attempt to update with the new value.
        strex   r3, r1, [r0]
        cmp     r3, #0
        bne     CX_Retry        ; Retry the operation if another write beat us there

        ;; We've successfully updated the value of the objectref so now we need a GC write barrier.
        ;; The following barrier code takes the destination in r0 and the value in r1 so the arguments are
        ;; already correctly set up.

        INSERT_CHECKED_WRITE_BARRIER_CORE r0, r1

        ;; The original value was equal to the comparand which is still in r2 so we can return that.
        mov     r0, r2
        bx      lr

CX_NoUpdate
        ;; Location value didn't match comparand, return that value.
        mov     r0, r3
        bx      lr

        LEAF_END RhpCheckedLockCmpXchg

        ;; Interlocked exchange on objectref.
        ;;
        ;; On entry:
        ;;  r0: pointer to objectref
        ;;  r1: exchange value
        ;;
        ;; On exit:
        ;;  r0: original value of objectref
        ;;  r1,r2,r3,r12: trashed
        ;;

        ;; WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
        ;; - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen within at RhpCheckedXchgAVLocation
        ;; - Function "UnwindSimpleHelperToCaller" assumes no registers were pushed and LR contains the return address

        LEAF_ENTRY RhpCheckedXchg

        ;; To implement our chosen memory model for ARM we insert a memory barrier at GC write barriers. This
        ;; barrier must occur before the object reference update.
        dmb

X_Retry
        ALTERNATE_ENTRY  RhpCheckedXchgAVLocation
        ;; Read the original contents of the location.
        ldrex   r2, [r0]

        ;; Attempt to update with the new value.
        strex   r3, r1, [r0]
        cmp     r3, #0
        bne     X_Retry        ; Retry the operation if another write beat us there

        ;; We've successfully updated the value of the objectref so now we need a GC write barrier.
        ;; The following barrier code takes the destination in r0 and the value in r1 so the arguments are
        ;; already correctly set up.

        INSERT_CHECKED_WRITE_BARRIER_CORE r0, r1

        ;; The original value is currently in r2. We need to return it in r0.
        mov     r0, r2
        bx      lr

        LEAF_END RhpCheckedXchg

    end
