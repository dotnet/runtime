;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

;; Macro used to copy contents of newly updated GC heap locations to a shadow copy of the heap. This is used
;; during garbage collections to verify that object references where never written to the heap without using a
;; write barrier. Note that we're potentially racing to update the shadow heap while other threads are writing
;; new references to the real heap. Since this can't be solved perfectly without critical sections around the
;; entire update process, we instead update the shadow location and then re-check the real location (as two
;; ordered operations) and if there is a disparity we'll re-write the shadow location with a special value
;; (INVALIDGCVALUE) which disables the check for that location. Since the shadow heap is only validated at GC
;; time and these write barrier operations are atomic wrt to GCs this is sufficient to guarantee that the
;; shadow heap contains only valid copies of real heap values or INVALIDGCVALUE.
ifdef WRITE_BARRIER_CHECK

g_GCShadow      TEXTEQU <?g_GCShadow@@3PEAEEA>
g_GCShadowEnd   TEXTEQU <?g_GCShadowEnd@@3PEAEEA>
INVALIDGCVALUE  EQU 0CCCCCCCDh

EXTERN  g_GCShadow : QWORD
EXTERN  g_GCShadowEnd : QWORD

UPDATE_GC_SHADOW macro BASENAME, REFREG, DESTREG

    ;; If g_GCShadow is 0, don't perform the check.
    cmp     g_GCShadow, 0
    je      &BASENAME&_UpdateShadowHeap_Done_&REFREG&

    ;; Save DESTREG since we're about to modify it (and we need the original value both within the macro and
    ;; once we exit the macro). Note that this is naughty since we're altering the stack pointer outside of
    ;; the prolog inside a method without a frame. But given that this is only debug code and generally we
    ;; shouldn't be walking the stack at this point it seems preferable to recoding the all the barrier
    ;; variants to set up frames. Unlike RhpBulkWriteBarrier below which is treated as a helper call using the
    ;; usual calling convention, the compiler knows exactly which registers are trashed in the simple write
    ;; barrier case, so we don't have any more scratch registers to play with (and doing so would only make
    ;; things harder if at a later stage we want to allow multiple barrier versions based on the input
    ;; registers).
    push    DESTREG

    ;; Transform DESTREG into the equivalent address in the shadow heap.
    sub     DESTREG, g_lowest_address
    jb      &BASENAME&_UpdateShadowHeap_PopThenDone_&REFREG&
    add     DESTREG, [g_GCShadow]
    cmp     DESTREG, [g_GCShadowEnd]
    jae     &BASENAME&_UpdateShadowHeap_PopThenDone_&REFREG&

    ;; Update the shadow heap.
    mov     [DESTREG], REFREG

    ;; Now check that the real heap location still contains the value we just wrote into the shadow heap. This
    ;; read must be strongly ordered wrt to the previous write to prevent race conditions. We also need to
    ;; recover the old value of DESTREG for the comparison so use an xchg instruction (which has an implicit lock
    ;; prefix).
    xchg    [rsp], DESTREG
    cmp     [DESTREG], REFREG
    jne     &BASENAME&_UpdateShadowHeap_Invalidate_&REFREG&

    ;; The original DESTREG value is now restored but the stack has a value (the shadow version of the
    ;; location) pushed. Need to discard this push before we are done.
    add     rsp, 8
    jmp     &BASENAME&_UpdateShadowHeap_Done_&REFREG&

&BASENAME&_UpdateShadowHeap_Invalidate_&REFREG&:
    ;; Someone went and updated the real heap. We need to invalidate the shadow location since we can't
    ;; guarantee whose shadow update won.

    ;; Retrieve shadow location from the stack and restore original DESTREG to the stack. This is an
    ;; additional memory barrier we don't require but it's on the rare path and x86 doesn't have an xchg
    ;; variant that doesn't implicitly specify the lock prefix. Note that INVALIDGCVALUE is a 64-bit
    ;; immediate and therefore must be moved into a register before it can be written to the shadow
    ;; location.
    xchg    [rsp], DESTREG
    push    REFREG
    mov     REFREG, INVALIDGCVALUE
    mov     qword ptr [DESTREG], REFREG
    pop     REFREG

&BASENAME&_UpdateShadowHeap_PopThenDone_&REFREG&:
    ;; Restore original DESTREG value from the stack.
    pop     DESTREG

&BASENAME&_UpdateShadowHeap_Done_&REFREG&:
endm

else ; WRITE_BARRIER_CHECK

UPDATE_GC_SHADOW macro BASENAME, REFREG, DESTREG
endm

endif ; WRITE_BARRIER_CHECK

;; There are several different helpers used depending on which register holds the object reference. Since all
;; the helpers have identical structure we use a macro to define this structure. Two arguments are taken, the
;; name of the register that points to the location to be updated and the name of the register that holds the
;; object reference (this should be in upper case as it's used in the definition of the name of the helper).
DEFINE_UNCHECKED_WRITE_BARRIER_CORE macro BASENAME, REFREG

    ;; Update the shadow copy of the heap with the same value just written to the same heap. (A no-op unless
    ;; we're in a debug build and write barrier checking has been enabled).
    UPDATE_GC_SHADOW BASENAME, REFREG, rcx

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    mov     r11, [g_write_watch_table]
    cmp     r11, 0
    je      &BASENAME&_CheckCardTable_&REFREG&

    mov     r10, rcx
    shr     r10, 0Ch ;; SoftwareWriteWatch::AddressToTableByteIndexShift
    add     r10, r11
    cmp     byte ptr [r10], 0
    jne     &BASENAME&_CheckCardTable_&REFREG&
    mov     byte ptr [r10], 0FFh
endif

&BASENAME&_CheckCardTable_&REFREG&:

    ;; If the reference is to an object that's not in an ephemeral generation we have no need to track it
    ;; (since the object won't be collected or moved by an ephemeral collection).
    cmp     REFREG, [g_ephemeral_low]
    jb      &BASENAME&_NoBarrierRequired_&REFREG&
    cmp     REFREG, [g_ephemeral_high]
    jae     &BASENAME&_NoBarrierRequired_&REFREG&

    mov     r10, REFREG
    mov     r11, rcx

    mov     cl, [g_region_shr]
    test    cl, cl
    je      &BASENAME&_SkipCheck_&REFREG&

    ; check if the source is in gen 2 - then it's not an ephemeral pointer
    shr     r10, cl
    add     r10, [g_region_to_generation_table]
    cmp     byte ptr [r10], 82h
    je      &BASENAME&_NoBarrierRequired_&REFREG&

    ; check if the destination happens to be in gen 0
    mov     r10, r11
    shr     r10, cl
    add     r10, [g_region_to_generation_table]
    cmp     byte ptr [r10], 0    
    je      &BASENAME&_NoBarrierRequired_&REFREG&
&BASENAME&_SkipCheck_&REFREG&:

    cmp     [g_region_use_bitwise_write_barrier], 0
    mov     rcx, r11
    je      &BASENAME&_CheckCardTableByte_&REFREG&

    ; compute card table bit
    mov     r10b, 1
    shr     rcx, 8
    and     cl, 7
    shl     r10b, cl

    ; Check if we need to update the card table
    ; Calc pCardByte
    mov     rcx, r11
    shr     rcx, 0Bh
    add     rcx, [g_card_table]

    ; Check if this card table bit is already set
    test    byte ptr [rcx], r10b
    jne     &BASENAME&_NoBarrierRequired_&REFREG&

    lock or byte ptr [rcx], r10b

    mov     rcx, r11
    shr     rcx, 15h
    jmp     &BASENAME&_CheckCardBundle_&REFREG&

&BASENAME&_CheckCardTableByte_&REFREG&:

    ;; We have a location on the GC heap being updated with a reference to an ephemeral object so we must
    ;; track this write. The location address is translated into an offset in the card table bitmap. We set
    ;; an entire byte in the card table since it's quicker than messing around with bitmasks and we only write
    ;; the byte if it hasn't already been done since writes are expensive and impact scaling.
    shr     rcx, 0Bh
    mov     r10, [g_card_table]
    cmp     byte ptr [rcx + r10], 0FFh
    je      &BASENAME&_NoBarrierRequired_&REFREG&

    ;; We get here if it's necessary to update the card table.
    mov     byte ptr [rcx + r10], 0FFh

ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    ;; Shift rcx by 0Ah more to get the card bundle byte (we shifted by 0x0B already)
    shr     rcx, 0Ah
&BASENAME&_CheckCardBundle_&REFREG&:
    add     rcx, [g_card_bundle_table]
    cmp     byte ptr [rcx], 0FFh
    je      &BASENAME&_NoBarrierRequired_&REFREG&

    mov     byte ptr [rcx], 0FFh
else
&BASENAME&_CheckCardBundle_&REFREG&:
endif

&BASENAME&_NoBarrierRequired_&REFREG&:
    ret

endm

;; There are several different helpers used depending on which register holds the object reference. Since all
;; the helpers have identical structure we use a macro to define this structure. One argument is taken, the
;; name of the register that will hold the object reference (this should be in upper case as it's used in the
;; definition of the name of the helper).
DEFINE_UNCHECKED_WRITE_BARRIER macro REFREG, EXPORT_REG_NAME

;; Define a helper with a name of the form RhpAssignRefEAX etc. (along with suitable calling standard
;; decoration). The location to be updated is in DESTREG. The object reference that will be assigned into that
;; location is in one of the other general registers determined by the value of REFREG.

;; WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
;; - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen on the first instruction
;; - Function "UnwindSimpleHelperToCaller" assumes the stack contains just the pushed return address
LEAF_ENTRY RhpAssignRef&EXPORT_REG_NAME&, _TEXT

    ;; Export the canonical write barrier under unqualified name as well
    ifidni <REFREG>, <RDX>
    ALTERNATE_ENTRY RhpAssignRef
    ALTERNATE_ENTRY RhpAssignRefAVLocation
    endif

    ;; Write the reference into the location. Note that we rely on the fact that no GC can occur between here
    ;; and the card table update we may perform below.
    mov     qword ptr [rcx], REFREG

    DEFINE_UNCHECKED_WRITE_BARRIER_CORE RhpAssignRef, REFREG

LEAF_END RhpAssignRef&EXPORT_REG_NAME&, _TEXT
endm

;; One day we might have write barriers for all the possible argument registers but for now we have
;; just one write barrier that assumes the input register is RDX.
DEFINE_UNCHECKED_WRITE_BARRIER RDX, EDX

;;
;; Define the helpers used to implement the write barrier required when writing an object reference into a
;; location residing on the GC heap. Such write barriers allow the GC to optimize which objects in
;; non-ephemeral generations need to be scanned for references to ephemeral objects during an ephemeral
;; collection.
;;

DEFINE_CHECKED_WRITE_BARRIER_CORE macro BASENAME, REFREG

    ;; The location being updated might not even lie in the GC heap (a handle or stack location for instance),
    ;; in which case no write barrier is required.
    cmp     rcx, [g_lowest_address]
    jb      &BASENAME&_NoBarrierRequired_&REFREG&
    cmp     rcx, [g_highest_address]
    jae     &BASENAME&_NoBarrierRequired_&REFREG&

    DEFINE_UNCHECKED_WRITE_BARRIER_CORE BASENAME, REFREG

endm

;; There are several different helpers used depending on which register holds the object reference. Since all
;; the helpers have identical structure we use a macro to define this structure. One argument is taken, the
;; name of the register that will hold the object reference (this should be in upper case as it's used in the
;; definition of the name of the helper).
DEFINE_CHECKED_WRITE_BARRIER macro REFREG, EXPORT_REG_NAME

;; Define a helper with a name of the form RhpCheckedAssignRefEAX etc. (along with suitable calling standard
;; decoration). The location to be updated is always in RCX. The object reference that will be assigned into
;; that location is in one of the other general registers determined by the value of REFREG.

;; WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
;; - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen on the first instruction
;; - Function "UnwindSimpleHelperToCaller" assumes the stack contains just the pushed return address
LEAF_ENTRY RhpCheckedAssignRef&EXPORT_REG_NAME&, _TEXT

    ;; Export the canonical write barrier under unqualified name as well
    ifidni <REFREG>, <RDX>
    ALTERNATE_ENTRY RhpCheckedAssignRef
    ALTERNATE_ENTRY RhpCheckedAssignRefAVLocation
    endif

    ;; Write the reference into the location. Note that we rely on the fact that no GC can occur between here
    ;; and the card table update we may perform below.
    mov     qword ptr [rcx], REFREG

    DEFINE_CHECKED_WRITE_BARRIER_CORE RhpCheckedAssignRef, REFREG

LEAF_END RhpCheckedAssignRef&EXPORT_REG_NAME&, _TEXT
endm

;; One day we might have write barriers for all the possible argument registers but for now we have
;; just one write barrier that assumes the input register is RDX.
DEFINE_CHECKED_WRITE_BARRIER RDX, EDX

;; WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
;; - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpCheckedLockCmpXchgAVLocation
;; - Function "UnwindSimpleHelperToCaller" assumes the stack contains just the pushed return address
LEAF_ENTRY RhpCheckedLockCmpXchg, _TEXT
    mov             rax, r8
ALTERNATE_ENTRY RhpCheckedLockCmpXchgAVLocation
    lock cmpxchg    [rcx], rdx
    jne             RhpCheckedLockCmpXchg_NoBarrierRequired_RDX

    DEFINE_CHECKED_WRITE_BARRIER_CORE RhpCheckedLockCmpXchg, RDX

LEAF_END RhpCheckedLockCmpXchg, _TEXT

;; WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
;; - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpCheckedXchgAVLocation
;; - Function "UnwindSimpleHelperToCaller" assumes the stack contains just the pushed return address
LEAF_ENTRY RhpCheckedXchg, _TEXT

    ;; Setup rax with the new object for the exchange, that way it will automatically hold the correct result
    ;; afterwards and we can leave rdx unaltered ready for the GC write barrier below.
    mov             rax, rdx
ALTERNATE_ENTRY RhpCheckedXchgAVLocation
    xchg            [rcx], rax

    DEFINE_CHECKED_WRITE_BARRIER_CORE RhpCheckedXchg, RDX

LEAF_END RhpCheckedXchg, _TEXT

;;
;; RhpByRefAssignRef simulates movs instruction for object references.
;;
;; On entry:
;;      rdi: address of ref-field (assigned to)
;;      rsi: address of the data (source)
;;
;; On exit:
;;      rdi, rsi are incremented by 8,
;;      rcx, r10, r11: trashed
;;
;; WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
;; - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at RhpByRefAssignRefAVLocation1/2
;; - Function "UnwindSimpleHelperToCaller" assumes the stack contains just the pushed return address
LEAF_ENTRY RhpByRefAssignRef, _TEXT
ALTERNATE_ENTRY RhpByRefAssignRefAVLocation1
    mov     rcx, [rsi]
ALTERNATE_ENTRY RhpByRefAssignRefAVLocation2
    mov     [rdi], rcx

    ;; Check whether the writes were even into the heap. If not there's no card update required.
    cmp     rdi, [g_lowest_address]
    jb      RhpByRefAssignRef_NoBarrierRequired
    cmp     rdi, [g_highest_address]
    jae     RhpByRefAssignRef_NoBarrierRequired

    ;; Update the shadow copy of the heap with the same value just written to the same heap. (A no-op unless
    ;; we're in a debug build and write barrier checking has been enabled).
    UPDATE_GC_SHADOW BASENAME, rcx, rdi

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    mov     r11, [g_write_watch_table]
    cmp     r11, 0
    je      RhpByRefAssignRef_CheckCardTable

    mov     r10, rdi
    shr     r10, 0Ch ;; SoftwareWriteWatch::AddressToTableByteIndexShift
    add     r10, r11
    cmp     byte ptr [r10], 0
    jne     RhpByRefAssignRef_CheckCardTable
    mov     byte ptr [r10], 0FFh
endif

RhpByRefAssignRef_CheckCardTable:

    ;; If the reference is to an object that's not in an ephemeral generation we have no need to track it
    ;; (since the object won't be collected or moved by an ephemeral collection).
    cmp     rcx, [g_ephemeral_low]
    jb      RhpByRefAssignRef_NoBarrierRequired
    cmp     rcx, [g_ephemeral_high]
    jae     RhpByRefAssignRef_NoBarrierRequired

    mov     r10, rcx

    mov     cl, [g_region_shr]
    test    cl, cl
    je      RhpByRefAssignRef_SkipCheck

    ; check if the source is in gen 2 - then it's not an ephemeral pointer
    shr     r10, cl
    add     r10, [g_region_to_generation_table]
    cmp     byte ptr [r10], 82h
    je      RhpByRefAssignRef_NoBarrierRequired

    ; check if the destination happens to be in gen 0
    mov     r10, rdi
    shr     r10, cl
    add     r10, [g_region_to_generation_table]
    cmp     byte ptr [r10], 0    
    je      RhpByRefAssignRef_NoBarrierRequired
RhpByRefAssignRef_SkipCheck:

    cmp     [g_region_use_bitwise_write_barrier], 0
    je      RhpByRefAssignRef_CheckCardTableByte

    ; compute card table bit
    mov     rcx, rdi
    mov     r10b, 1
    shr     rcx, 8
    and     cl, 7
    shl     r10b, cl

    ; Check if we need to update the card table
    ; Calc pCardByte
    mov     rcx, rdi
    shr     rcx, 0Bh
    add     rcx, [g_card_table]

    ; Check if this card table bit is already set
    test    byte ptr [rcx], r10b
    jne     RhpByRefAssignRef_NoBarrierRequired

    lock or byte ptr [rcx], r10b

    mov     rcx, rdi
    shr     rcx, 15h
    jmp     RhpByRefAssignRef_CheckCardBundle

RhpByRefAssignRef_CheckCardTableByte:

    ;; move current rdi value into rcx, we need to keep rdi and eventually increment by 8
    mov     rcx, rdi

    ;; We have a location on the GC heap being updated with a reference to an ephemeral object so we must
    ;; track this write. The location address is translated into an offset in the card table bitmap. We set
    ;; an entire byte in the card table since it's quicker than messing around with bitmasks and we only write
    ;; the byte if it hasn't already been done since writes are expensive and impact scaling.
    shr     rcx, 0Bh
    mov     r10, [g_card_table]
    cmp     byte ptr [rcx + r10], 0FFh
    je      RhpByRefAssignRef_NoBarrierRequired

;; We get here if it's necessary to update the card table.
    mov     byte ptr [rcx + r10], 0FFh

ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    ;; Shift rcx by 0Ah more to get the card bundle byte (we shifted by 0Bh already)
    shr     rcx, 0Ah
RhpByRefAssignRef_CheckCardBundle:
    add     rcx, [g_card_bundle_table]
    cmp     byte ptr [rcx], 0FFh
    je      RhpByRefAssignRef_NoBarrierRequired

    mov     byte ptr [rcx], 0FFh
else
RhpByRefAssignRef_CheckCardBundle:
endif

RhpByRefAssignRef_NoBarrierRequired:
    ;; Increment the pointers before leaving
    add     rdi, 8h
    add     rsi, 8h
    ret
LEAF_END RhpByRefAssignRef, _TEXT

    end
