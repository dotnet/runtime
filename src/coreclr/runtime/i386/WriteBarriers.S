// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "AsmMacros_Shared.h"

// TODO! This is implemented, but not tested.

#ifdef WRITE_BARRIER_CHECK

.macro UPDATE_GC_SHADOW BASENAME, DESTREG, REFREG, TEMPREG

    // If g_GCShadow is 0, don't perform the check.
    PREPARE_EXTERNAL_VAR g_GCShadow, \TEMPREG
    cmp     dword ptr [\TEMPREG], 0
    je      LOCAL_LABEL(\BASENAME\()UpdateShadowHeap_Done_\DESTREG\()_\REFREG)

    // Save DESTREG since we're about to modify it (and we need the original value both within the macro and
    // once we exit the macro).
    push    \DESTREG

    // Transform DESTREG into the equivalent address in the shadow heap.
    PREPARE_EXTERNAL_VAR g_lowest_address, \TEMPREG
    sub     \DESTREG, [\TEMPREG]
    jb      LOCAL_LABEL(\BASENAME\()UpdateShadowHeap_PopThenDone_\DESTREG\()_\REFREG)
    PREPARE_EXTERNAL_VAR g_GCShadow, \TEMPREG
    add     \DESTREG, [\TEMPREG]
    PREPARE_EXTERNAL_VAR g_GCShadowEnd, \TEMPREG
    cmp     \DESTREG, [\TEMPREG]
    jae     LOCAL_LABEL(\BASENAME\()UpdateShadowHeap_PopThenDone_\DESTREG\()_\REFREG)

    // Update the shadow heap.
    mov     [\DESTREG], \REFREG

    // Now check that the real heap location still contains the value we just wrote into the shadow heap. This
    // read must be strongly ordered wrt to the previous write to prevent race conditions. We also need to
    // recover the old value of DESTREG for the comparison so use an xchg instruction (which has an implicit lock
    // prefix).
    xchg    [esp], \DESTREG
    cmp     [\DESTREG], \REFREG
    jne     LOCAL_LABEL(\BASENAME\()UpdateShadowHeap_Invalidate_\DESTREG\()_\REFREG)

    // The original DESTREG value is now restored but the stack has a value (the shadow version of the
    // location) pushed. Need to discard this push before we are done.
    add     esp, 4
    jmp     LOCAL_LABEL(\BASENAME\()UpdateShadowHeap_Done_\DESTREG\()_\REFREG)

LOCAL_LABEL(\BASENAME\()UpdateShadowHeap_Invalidate_\DESTREG\()_\REFREG):
    // Someone went and updated the real heap. We need to invalidate the shadow location since we can't
    // guarantee whose shadow update won.

    // Retrieve shadow location from the stack and restore original DESTREG to the stack. This is an
    // additional memory barrier we don't require but it's on the rare path and x86 doesn't have an xchg
    // variant that doesn't implicitly specify the lock prefix.
    xchg    [esp], \DESTREG
    mov     dword ptr [\DESTREG], INVALIDGCVALUE

LOCAL_LABEL(\BASENAME\()UpdateShadowHeap_PopThenDone_\DESTREG\()_\REFREG):
    // Restore original DESTREG value from the stack.
    pop     \DESTREG

LOCAL_LABEL(\BASENAME\()UpdateShadowHeap_Done_\DESTREG\()_\REFREG):
.endm

#else // WRITE_BARRIER_CHECK

.macro UPDATE_GC_SHADOW BASENAME, DESTREG, REFREG, TEMPREG
.endm
#endif

// There are several different helpers used depending on which register holds the object reference. Since all
// the helpers have identical structure we use a macro to define this structure. Two arguments are taken, the
// name of the register that points to the location to be updated and the name of the register that holds the
// object reference (this should be in upper case as it's used in the definition of the name of the helper).
.macro DEFINE_WRITE_BARRIER DESTREG, REFREG, TEMPREG

// Define a helper with a name of the form RhpAssignRefEAX etc. (along with suitable calling standard
// decoration). The location to be updated is in DESTREG. The object reference that will be assigned into that
// location is in one of the other general registers determined by the value of REFREG.
LEAF_ENTRY RhpAssignRef\REFREG, _TEXT

    // Export the canonical write barrier under unqualified name as well
    .ifc \REFREG, EDX
    ALTERNATE_ENTRY RhpAssignRef
    ALTERNATE_ENTRY RhpAssignRefAVLocation
    .endif

    ALTERNATE_ENTRY RhpAssignRef\REFREG\()AVLocation

    // Write the reference into the location. Note that we rely on the fact that no GC can occur between here
    // and the card table update we may perform below.
    mov     dword ptr [\DESTREG], \REFREG

    // Save a register so that we have an available register as a temporary for PREPARE_EXTERNAL_VAR
    push \TEMPREG

    // Update the shadow copy of the heap with the same value (if enabled).
    UPDATE_GC_SHADOW RhpAssignRef, \DESTREG, \REFREG, \TEMPREG

    // If the reference is to an object that's not in an ephemeral generation we have no need to track it
    // (since the object won't be collected or moved by an ephemeral collection).
    PREPARE_EXTERNAL_VAR g_ephemeral_low, \TEMPREG
    cmp     \REFREG, [\TEMPREG]
    jb      LOCAL_LABEL(WriteBarrier_NoBarrierRequired_\DESTREG\()_\REFREG)
    PREPARE_EXTERNAL_VAR g_ephemeral_high, \TEMPREG
    cmp     \REFREG, [\TEMPREG]
    jae     LOCAL_LABEL(WriteBarrier_NoBarrierRequired_\DESTREG\()_\REFREG)

    // We have a location on the GC heap being updated with a reference to an ephemeral object so we must
    // track this write. The location address is translated into an offset in the card table bitmap. We set
    // an entire byte in the card table since it's quicker than messing around with bitmasks and we only write
    // the byte if it hasn't already been done since writes are expensive and impact scaling.
    shr     \DESTREG, 10
    PREPARE_EXTERNAL_VAR g_card_table, \TEMPREG
    add     \DESTREG, [\TEMPREG]
    cmp     byte ptr [\DESTREG], 0xFF
    jne     LOCAL_LABEL(WriteBarrier_UpdateCardTable_\DESTREG\()_\REFREG)

LOCAL_LABEL(WriteBarrier_NoBarrierRequired_\DESTREG\()_\REFREG):
    pop \TEMPREG
    ret

// We get here if it's necessary to update the card table.
LOCAL_LABEL(WriteBarrier_UpdateCardTable_\DESTREG\()_\REFREG):
    pop \TEMPREG
    mov     byte ptr [\DESTREG], 0xFF
    ret
LEAF_END RhpAssignRef\REFREG, _TEXT
.endm

.macro DEFINE_CHECKED_WRITE_BARRIER_CORE BASENAME, DESTREG, REFREG, TEMPREG

    // The location being updated might not even lie in the GC heap (a handle or stack location for instance),
    // in which case no write barrier is required.
    PREPARE_EXTERNAL_VAR g_lowest_address, \TEMPREG
    cmp     \DESTREG, [\TEMPREG]
    jb      LOCAL_LABEL(\BASENAME\()CheckedWriteBarrier_NoBarrierRequired_\DESTREG\()_\REFREG)
    PREPARE_EXTERNAL_VAR g_highest_address, \TEMPREG
    cmp     \DESTREG, [\TEMPREG]
    jae     LOCAL_LABEL(\BASENAME\()CheckedWriteBarrier_NoBarrierRequired_\DESTREG\()_\REFREG)

    // Update the shadow copy of the heap with the same value just written to the same heap. (A no-op unless
    // we're in a debug build and write barrier checking has been enabled).
    UPDATE_GC_SHADOW \BASENAME, \DESTREG, \REFREG, \TEMPREG

    // If the reference is to an object that's not in an ephemeral generation we have no need to track it
    // (since the object won't be collected or moved by an ephemeral collection).
    PREPARE_EXTERNAL_VAR g_ephemeral_low, \TEMPREG
    cmp     \REFREG, [\TEMPREG]
    jb      LOCAL_LABEL(\BASENAME\()CheckedWriteBarrier_NoBarrierRequired_\DESTREG\()_\REFREG)
    PREPARE_EXTERNAL_VAR g_ephemeral_high, \TEMPREG
    cmp     \REFREG, [\TEMPREG]
    jae     LOCAL_LABEL(\BASENAME\()CheckedWriteBarrier_NoBarrierRequired_\DESTREG\()_\REFREG)

    // We have a location on the GC heap being updated with a reference to an ephemeral object so we must
    // track this write. The location address is translated into an offset in the card table bitmap. We set
    // an entire byte in the card table since it's quicker than messing around with bitmasks and we only write
    // the byte if it hasn't already been done since writes are expensive and impact scaling.
    shr     \DESTREG, 10
    PREPARE_EXTERNAL_VAR g_card_table, \TEMPREG
    add     \DESTREG, [\TEMPREG]
    cmp     byte ptr [\DESTREG], 0xFF
    je      LOCAL_LABEL(\BASENAME\()CheckedWriteBarrier_NoBarrierRequired_\DESTREG\()_\REFREG)
    // We get here if it's necessary to update the card table.
    mov     byte ptr [\DESTREG], 0xFF
LOCAL_LABEL(\BASENAME\()CheckedWriteBarrier_NoBarrierRequired_\DESTREG\()_\REFREG):
.endm

// This macro is very much like the one above except that it generates a variant of the function which also
// checks whether the destination is actually somewhere within the GC heap.
.macro DEFINE_CHECKED_WRITE_BARRIER DESTREG, REFREG, TEMPREG

// Define a helper with a name of the form RhpCheckedAssignRefEAX etc. (along with suitable calling standard
// decoration). The location to be updated is in DESTREG. The object reference that will be assigned into
// that location is in one of the other general registers determined by the value of REFREG.

// WARNING: Code in EHHelpers.cpp makes assumptions about write barrier code, in particular:
// - Function "InWriteBarrierHelper" assumes an AV due to passed in null pointer will happen on the first instruction
// - Function "UnwindSimpleHelperToCaller" assumes the stack contains just the pushed return address
LEAF_ENTRY RhpCheckedAssignRef\REFREG, _TEXT

    // Export the canonical write barrier under unqualified name as well
    .ifc \REFREG, EDX
    ALTERNATE_ENTRY RhpCheckedAssignRef
    ALTERNATE_ENTRY RhpCheckedAssignRefAVLocation
    .endif

    ALTERNATE_ENTRY RhpCheckedAssignRef\REFREG\()AVLocation

    // Write the reference into the location. Note that we rely on the fact that no GC can occur between here
    // and the card table update we may perform below.
    mov     dword ptr [\DESTREG], \REFREG

    // Save a register so that we have an available register as a temporary for PREPARE_EXTERNAL_VAR
    push \TEMPREG

    DEFINE_CHECKED_WRITE_BARRIER_CORE RhpCheckedAssignRef, \DESTREG, \REFREG, \TEMPREG
    pop \TEMPREG
    ret

LEAF_END RhpCheckedAssignRef\REFREG, _TEXT

.endm

// One day we might have write barriers for all the possible argument registers but for now we have
// just one write barrier that assumes the input register is EDX.
DEFINE_CHECKED_WRITE_BARRIER ECX, EDX, EAX
DEFINE_WRITE_BARRIER ECX, EDX, EAX

DEFINE_WRITE_BARRIER EDX, EAX, ECX
DEFINE_WRITE_BARRIER EDX, ECX, EAX
DEFINE_WRITE_BARRIER EDX, EBX, EAX
DEFINE_WRITE_BARRIER EDX, ESI, EAX
DEFINE_WRITE_BARRIER EDX, EDI, EAX
DEFINE_WRITE_BARRIER EDX, EBP, EAX

DEFINE_CHECKED_WRITE_BARRIER EDX, EAX, ECX
DEFINE_CHECKED_WRITE_BARRIER EDX, ECX, EAX
DEFINE_CHECKED_WRITE_BARRIER EDX, EBX, EAX
DEFINE_CHECKED_WRITE_BARRIER EDX, ESI, EAX
DEFINE_CHECKED_WRITE_BARRIER EDX, EDI, EAX
DEFINE_CHECKED_WRITE_BARRIER EDX, EBP, EAX

LEAF_ENTRY RhpCheckedLockCmpXchg, _TEXT
    mov             eax, [esp+4]
    lock cmpxchg    [ecx], edx
    jne             LOCAL_LABEL(RhpCheckedLockCmpXchg_NoWrite)
    push eax

    DEFINE_CHECKED_WRITE_BARRIER_CORE RhpCheckedLockCmpXchg, ECX, EDX, EAX
    pop eax
LOCAL_LABEL(RhpCheckedLockCmpXchg_NoWrite):
    ret 4
LEAF_END RhpCheckedLockCmpXchg, _TEXT

LEAF_ENTRY RhpCheckedXchg, _TEXT

    // Setup eax with the new object for the exchange, that way it will automatically hold the correct result
    // afterwards and we can leave edx unaltered ready for the GC write barrier below.
    mov             eax, edx
    xchg            [ecx], eax
    push eax

    DEFINE_CHECKED_WRITE_BARRIER_CORE RhpCheckedXchg, ECX, EDX, EAX
    pop eax
    ret
LEAF_END RhpCheckedXchg, _TEXT

//
// RhpByRefAssignRef simulates movs instruction for object references.
//
// On entry:
//      edi: address of ref-field (assigned to)
//      esi: address of the data (source)
//
// On exit:
//      edi, esi are incremented by 4,
//      ecx: trashed
//
LEAF_ENTRY RhpByRefAssignRef, _TEXT
ALTERNATE_ENTRY RhpByRefAssignRefAVLocation1
    mov     ecx, [esi]
ALTERNATE_ENTRY RhpByRefAssignRefAVLocation2
    mov     [edi], ecx

    push eax

    // Check whether the writes were even into the heap. If not there's no card update required.
    PREPARE_EXTERNAL_VAR g_lowest_address, eax
    cmp     edi, [eax]
    jb      LOCAL_LABEL(RhpByRefAssignRef_NoBarrierRequired)
    PREPARE_EXTERNAL_VAR g_highest_address, eax
    cmp     edi, [eax]
    jae     LOCAL_LABEL(RhpByRefAssignRef_NoBarrierRequired)

    UPDATE_GC_SHADOW RhpByRefAssignRef, ecx, edi, eax

    // If the reference is to an object that's not in an ephemeral generation we have no need to track it
    // (since the object won't be collected or moved by an ephemeral collection).
    PREPARE_EXTERNAL_VAR g_ephemeral_low, eax
    cmp     ecx, [eax]
    jb      LOCAL_LABEL(RhpByRefAssignRef_NoBarrierRequired)
    PREPARE_EXTERNAL_VAR g_ephemeral_high, eax
    cmp     ecx, [eax]
    jae     LOCAL_LABEL(RhpByRefAssignRef_NoBarrierRequired)

    mov     ecx, edi
    shr     ecx, 10
    PREPARE_EXTERNAL_VAR g_card_table, eax
    add     ecx, [eax]
    cmp     byte ptr [ecx], 0xFF
    je      LOCAL_LABEL(RhpByRefAssignRef_NoBarrierRequired)

    mov     byte ptr [ecx], 0xFF

LOCAL_LABEL(RhpByRefAssignRef_NoBarrierRequired):
    // Increment the pointers before leaving
    add     esi,4
    add     edi,4
    pop     eax
    ret
LEAF_END RhpByRefAssignRef, _TEXT
