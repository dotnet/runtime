;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

;;
;; Define the helpers used to implement the write barrier required when writing an object reference into a
;; location residing on the GC heap. Such write barriers allow the GC to optimize which objects in
;; non-ephemeral generations need to be scanned for references to ephemeral objects during an ephemeral
;; collection.
;;

    .xmm
    .model  flat
    option  casemap:none
    .code

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

g_GCShadow      TEXTEQU <?g_GCShadow@@3PAEA>
g_GCShadowEnd   TEXTEQU <?g_GCShadowEnd@@3PAEA>
INVALIDGCVALUE  EQU 0CCCCCCCDh

EXTERN  g_GCShadow : DWORD
EXTERN  g_GCShadowEnd : DWORD

UPDATE_GC_SHADOW macro BASENAME, DESTREG, REFREG

    ;; If g_GCShadow is 0, don't perform the check.
    cmp     g_GCShadow, 0
    je      &BASENAME&_UpdateShadowHeap_Done_&DESTREG&_&REFREG&

    ;; Save DESTREG since we're about to modify it (and we need the original value both within the macro and
    ;; once we exit the macro).
    push    DESTREG

    ;; Transform DESTREG into the equivalent address in the shadow heap.
    sub     DESTREG, G_LOWEST_ADDRESS
    jb      &BASENAME&_UpdateShadowHeap_PopThenDone_&DESTREG&_&REFREG&
    add     DESTREG, [g_GCShadow]
    cmp     DESTREG, [g_GCShadowEnd]
    jae     &BASENAME&_UpdateShadowHeap_PopThenDone_&DESTREG&_&REFREG&

    ;; Update the shadow heap.
    mov     [DESTREG], REFREG

    ;; Now check that the real heap location still contains the value we just wrote into the shadow heap. This
    ;; read must be strongly ordered wrt to the previous write to prevent race conditions. We also need to
    ;; recover the old value of DESTREG for the comparison so use an xchg instruction (which has an implicit lock
    ;; prefix).
    xchg    [esp], DESTREG
    cmp     [DESTREG], REFREG
    jne     &BASENAME&_UpdateShadowHeap_Invalidate_&DESTREG&_&REFREG&

    ;; The original DESTREG value is now restored but the stack has a value (the shadow version of the
    ;; location) pushed. Need to discard this push before we are done.
    add     esp, 4
    jmp     &BASENAME&_UpdateShadowHeap_Done_&DESTREG&_&REFREG&

&BASENAME&_UpdateShadowHeap_Invalidate_&DESTREG&_&REFREG&:
    ;; Someone went and updated the real heap. We need to invalidate the shadow location since we can't
    ;; guarantee whose shadow update won.

    ;; Retrieve shadow location from the stack and restore original DESTREG to the stack. This is an
    ;; additional memory barrier we don't require but it's on the rare path and x86 doesn't have an xchg
    ;; variant that doesn't implicitly specify the lock prefix.
    xchg    [esp], DESTREG
    mov     dword ptr [DESTREG], INVALIDGCVALUE

&BASENAME&_UpdateShadowHeap_PopThenDone_&DESTREG&_&REFREG&:
    ;; Restore original DESTREG value from the stack.
    pop     DESTREG

&BASENAME&_UpdateShadowHeap_Done_&DESTREG&_&REFREG&:
endm

else ; WRITE_BARRIER_CHECK

UPDATE_GC_SHADOW macro BASENAME, DESTREG, REFREG
endm

endif ; WRITE_BARRIER_CHECK

;; There are several different helpers used depending on which register holds the object reference. Since all
;; the helpers have identical structure we use a macro to define this structure. Two arguments are taken, the
;; name of the register that points to the location to be updated and the name of the register that holds the
;; object reference (this should be in upper case as it's used in the definition of the name of the helper).
DEFINE_WRITE_BARRIER macro DESTREG, REFREG

;; Define a helper with a name of the form RhpAssignRefEAX etc. (along with suitable calling standard
;; decoration). The location to be updated is in DESTREG. The object reference that will be assigned into that
;; location is in one of the other general registers determined by the value of REFREG.
FASTCALL_FUNC RhpAssignRef&REFREG&, 8
ALTERNATE_HELPER_ENTRY RhpAssignRef&REFREG&

    ;; Export the canonical write barrier under unqualified name as well
    ifidni <REFREG>, <EDX>
    @RhpAssignRef@8 label proc
    PUBLIC @RhpAssignRef@8
    ALTERNATE_HELPER_ENTRY RhpAssignRef
    ALTERNATE_ENTRY RhpAssignRefAVLocation
    endif

    ;; Write the reference into the location. Note that we rely on the fact that no GC can occur between here
    ;; and the card table update we may perform below.
    mov     dword ptr [DESTREG], REFREG

    ;; Update the shadow copy of the heap with the same value (if enabled).
    UPDATE_GC_SHADOW RhpAssignRef, DESTREG, REFREG

    ;; If the reference is to an object that's not in an ephemeral generation we have no need to track it
    ;; (since the object won't be collected or moved by an ephemeral collection).
    cmp     REFREG, [G_EPHEMERAL_LOW]
    jb      WriteBarrier_NoBarrierRequired_&DESTREG&_&REFREG&
    cmp     REFREG, [G_EPHEMERAL_HIGH]
    jae     WriteBarrier_NoBarrierRequired_&DESTREG&_&REFREG&

    ;; We have a location on the GC heap being updated with a reference to an ephemeral object so we must
    ;; track this write. The location address is translated into an offset in the card table bitmap. We set
    ;; an entire byte in the card table since it's quicker than messing around with bitmasks and we only write
    ;; the byte if it hasn't already been done since writes are expensive and impact scaling.
    shr     DESTREG, 10
    add     DESTREG, [G_CARD_TABLE]
    cmp     byte ptr [DESTREG], 0FFh
    jne     WriteBarrier_UpdateCardTable_&DESTREG&_&REFREG&

WriteBarrier_NoBarrierRequired_&DESTREG&_&REFREG&:
    ret

;; We get here if it's necessary to update the card table.
WriteBarrier_UpdateCardTable_&DESTREG&_&REFREG&:
    mov     byte ptr [DESTREG], 0FFh
    ret
FASTCALL_ENDFUNC
endm

RET4    macro
    ret     4
endm

DEFINE_CHECKED_WRITE_BARRIER_CORE macro BASENAME, DESTREG, REFREG, RETINST

    ;; The location being updated might not even lie in the GC heap (a handle or stack location for instance),
    ;; in which case no write barrier is required.
    cmp     DESTREG, [G_LOWEST_ADDRESS]
    jb      &BASENAME&_NoBarrierRequired_&DESTREG&_&REFREG&
    cmp     DESTREG, [G_HIGHEST_ADDRESS]
    jae     &BASENAME&_NoBarrierRequired_&DESTREG&_&REFREG&

    ;; Update the shadow copy of the heap with the same value just written to the same heap. (A no-op unless
    ;; we're in a debug build and write barrier checking has been enabled).
    UPDATE_GC_SHADOW BASENAME, DESTREG, REFREG

    ;; If the reference is to an object that's not in an ephemeral generation we have no need to track it
    ;; (since the object won't be collected or moved by an ephemeral collection).
    cmp     REFREG, [G_EPHEMERAL_LOW]
    jb      &BASENAME&_NoBarrierRequired_&DESTREG&_&REFREG&
    cmp     REFREG, [G_EPHEMERAL_HIGH]
    jae     &BASENAME&_NoBarrierRequired_&DESTREG&_&REFREG&

    ;; We have a location on the GC heap being updated with a reference to an ephemeral object so we must
    ;; track this write. The location address is translated into an offset in the card table bitmap. We set
    ;; an entire byte in the card table since it's quicker than messing around with bitmasks and we only write
    ;; the byte if it hasn't already been done since writes are expensive and impact scaling.
    shr     DESTREG, 10
    add     DESTREG, [G_CARD_TABLE]
    cmp     byte ptr [DESTREG], 0FFh
    jne     &BASENAME&_UpdateCardTable_&DESTREG&_&REFREG&

&BASENAME&_NoBarrierRequired_&DESTREG&_&REFREG&:
    RETINST

;; We get here if it's necessary to update the card table.
&BASENAME&_UpdateCardTable_&DESTREG&_&REFREG&:
    mov     byte ptr [DESTREG], 0FFh
    RETINST

endm


;; This macro is very much like the one above except that it generates a variant of the function which also
;; checks whether the destination is actually somewhere within the GC heap.
DEFINE_CHECKED_WRITE_BARRIER macro DESTREG, REFREG

;; Define a helper with a name of the form RhpCheckedAssignRefEAX etc. (along with suitable calling standard
;; decoration). The location to be updated is in DESTREG. The object reference that will be assigned into
;; that location is in one of the other general registers determined by the value of REFREG.

;; WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
;; - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen on the first instruction
;; - Function "UnwindSimpleHelperToCaller" assumes the stack contains just the pushed return address
FASTCALL_FUNC RhpCheckedAssignRef&REFREG&, 8
ALTERNATE_HELPER_ENTRY RhpCheckedAssignRef&REFREG&

    ;; Export the canonical write barrier under unqualified name as well
    ifidni <REFREG>, <EDX>
    @RhpCheckedAssignRef@8 label proc
    PUBLIC @RhpCheckedAssignRef@8
    ALTERNATE_HELPER_ENTRY RhpCheckedAssignRef
    ALTERNATE_ENTRY RhpCheckedAssignRefAVLocation
    endif

    ;; Write the reference into the location. Note that we rely on the fact that no GC can occur between here
    ;; and the card table update we may perform below.
    mov     dword ptr [DESTREG], REFREG

    DEFINE_CHECKED_WRITE_BARRIER_CORE RhpCheckedAssignRef, DESTREG, REFREG, ret

FASTCALL_ENDFUNC

endm

;; One day we might have write barriers for all the possible argument registers but for now we have
;; just one write barrier that assumes the input register is EDX.
DEFINE_CHECKED_WRITE_BARRIER ECX, EDX
DEFINE_WRITE_BARRIER ECX, EDX

;; Need some more write barriers to run CLR compiled MDIL on Redhawk - commented out for now
DEFINE_WRITE_BARRIER EDX, EAX
DEFINE_WRITE_BARRIER EDX, ECX
DEFINE_WRITE_BARRIER EDX, EBX
DEFINE_WRITE_BARRIER EDX, ESI
DEFINE_WRITE_BARRIER EDX, EDI
DEFINE_WRITE_BARRIER EDX, EBP

DEFINE_CHECKED_WRITE_BARRIER EDX, EAX
DEFINE_CHECKED_WRITE_BARRIER EDX, ECX
DEFINE_CHECKED_WRITE_BARRIER EDX, EBX
DEFINE_CHECKED_WRITE_BARRIER EDX, ESI
DEFINE_CHECKED_WRITE_BARRIER EDX, EDI
DEFINE_CHECKED_WRITE_BARRIER EDX, EBP

;; WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
;; - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at @RhpCheckedLockCmpXchgAVLocation@0
;; - Function "UnwindSimpleHelperToCaller" assumes the stack contains just the pushed return address
;; pass third argument in EAX
FASTCALL_FUNC RhpCheckedLockCmpXchg, 12
ALTERNATE_ENTRY RhpCheckedLockCmpXchgAVLocation
    lock cmpxchg    [ecx], edx
    jne              RhpCheckedLockCmpXchg_NoBarrierRequired_ECX_EDX

    DEFINE_CHECKED_WRITE_BARRIER_CORE RhpCheckedLockCmpXchg, ECX, EDX, ret

FASTCALL_ENDFUNC

;; WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
;; - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen at @RhpCheckedXchgAVLocation@0
;; - Function "UnwindSimpleHelperToCaller" assumes the stack contains just the pushed return address
FASTCALL_FUNC RhpCheckedXchg, 8

    ;; Setup eax with the new object for the exchange, that way it will automatically hold the correct result
    ;; afterwards and we can leave edx unaltered ready for the GC write barrier below.
    mov             eax, edx
ALTERNATE_ENTRY RhpCheckedXchgAVLocation
    xchg            [ecx], eax

    DEFINE_CHECKED_WRITE_BARRIER_CORE RhpCheckedXchg, ECX, EDX, ret

FASTCALL_ENDFUNC

;;
;; RhpByRefAssignRef simulates movs instruction for object references.
;;
;; On entry:
;;      edi: address of ref-field (assigned to)
;;      esi: address of the data (source)
;;
;; On exit:
;;      edi, esi are incremented by 4,
;;      ecx: trashed
;;
FASTCALL_FUNC RhpByRefAssignRef, 8
ALTERNATE_HELPER_ENTRY RhpByRefAssignRef
ALTERNATE_ENTRY RhpByRefAssignRefAVLocation1
    mov     ecx, [esi]
ALTERNATE_ENTRY RhpByRefAssignRefAVLocation2
    mov     [edi], ecx

    ;; Check whether the writes were even into the heap. If not there's no card update required.
    cmp     edi, [G_LOWEST_ADDRESS]
    jb      RhpByRefAssignRef_NoBarrierRequired
    cmp     edi, [G_HIGHEST_ADDRESS]
    jae     RhpByRefAssignRef_NoBarrierRequired

    UPDATE_GC_SHADOW BASENAME, ecx, edi

    ;; If the reference is to an object that's not in an ephemeral generation we have no need to track it
    ;; (since the object won't be collected or moved by an ephemeral collection).
    cmp     ecx, [G_EPHEMERAL_LOW]
    jb      RhpByRefAssignRef_NoBarrierRequired
    cmp     ecx, [G_EPHEMERAL_HIGH]
    jae     RhpByRefAssignRef_NoBarrierRequired

    mov     ecx, edi
    shr     ecx, 10
    add     ecx, [G_CARD_TABLE]
    cmp     byte ptr [ecx], 0FFh
    je      RhpByRefAssignRef_NoBarrierRequired

    mov     byte ptr [ecx], 0FFh

RhpByRefAssignRef_NoBarrierRequired:
    ;; Increment the pointers before leaving
    add     esi,4
    add     edi,4
    ret
FASTCALL_ENDFUNC

    end
