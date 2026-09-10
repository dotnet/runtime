// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"

// ***
// JIT_WriteBarrier* - GC write barrier helper
//
// Purpose:
//    Helper calls in order to assign an object to a field
//    Enables book-keeping of the GC.
//
// Entry:
//    EDX - address of ref-field (assigned to)
//    the resp. other reg - RHS of assignment
//
// Uses:
//    EDX is destroyed.
//
// *******************************************************************************

//  The code here is tightly coupled with AdjustContextForJITHelpers, if you change
//  anything here, you might need to change AdjustContextForJITHelpers as well
.macro WriteBarrierHelper rg
.align 4

// The entry point is the fully 'safe' one in which we check if EDX (the REF
// begin updated) is actually in the GC heap
NESTED_ENTRY JIT_CheckedWriteBarrier\rg, _TEXT, NoHandler
    // check in the REF being updated is in the GC heap
    push    eax
    PREPARE_EXTERNAL_VAR g_lowest_address, eax
    cmp     edx, [eax]
    pop     eax
    jb      LOCAL_LABEL(WriteBarrier_NotInHeap_\rg)
    push    eax
    PREPARE_EXTERNAL_VAR g_highest_address, eax
    cmp     edx, [eax]
    pop     eax
    jae     LOCAL_LABEL(WriteBarrier_NotInHeap_\rg)

    // fall through to unchecked routine
    // note that its entry point also happens to be aligned

#ifdef WRITE_BARRIER_CHECK
    // This entry point is used when you know the REF pointer being updated
    // is in the GC heap
PATCH_LABEL JIT_DebugWriteBarrier\rg
#endif // WRITE_BARRIER_CHECK

#ifdef _DEBUG
    push    edx
    push    ecx
    push    eax

    push    \rg
    push    edx
    call    C_FUNC(WriteBarrierAssert)

    pop     eax
    pop     ecx
    pop     edx
#endif // _DEBUG

    // in the !WRITE_BARRIER_CHECK case this will be the move for all
    // addresses in the GCHeap, addresses outside the GCHeap will get
    // taken care of below at WriteBarrier_NotInHeap_&rg

#ifndef WRITE_BARRIER_CHECK
    mov     DWORD PTR [edx], \rg
#endif // !WRITE_BARRIER_CHECK

#ifdef WRITE_BARRIER_CHECK
    // Test dest here so if it is bad AV would happen before we change register/stack
    // status. This makes job of AdjustContextForJITHelpers easier.
    cmp     BYTE PTR [edx], 0
    // ALSO update the shadow GC heap if that is enabled
    // Make ebp into the temporary src register. We need to do this so that we can use ecx
    // in the calculation of the shadow GC address, but still have access to the src register
    push    ecx
    push    ebp
    mov     ebp, \rg

    // if g_GCShadow is 0, don't perform the check
    push    eax
    PREPARE_EXTERNAL_VAR g_GCShadow, eax
    cmp     DWORD PTR [eax], 0
    pop     eax
    je      LOCAL_LABEL(WriteBarrier_NoShadow_\rg)

    mov     ecx, edx
    push    eax
    PREPARE_EXTERNAL_VAR g_lowest_address, eax
    sub     ecx, [eax]
    pop     eax
    jb      LOCAL_LABEL(WriteBarrier_NoShadow_\rg)
    push    edx
    PREPARE_EXTERNAL_VAR g_GCShadow, edx
    mov     [edx], edx
    add     ecx, [edx]
    PREPARE_EXTERNAL_VAR g_GCShadowEnd, edx
    mov     [edx], edx
    cmp     ecx, [edx]
    pop     edx
    jae     LOCAL_LABEL(WriteBarrier_NoShadow_\rg)

    // edx contains address in GC
    // ecx contains address in ShadowGC
    // ebp temporarially becomes the src register

    // When we're writing to the shadow GC heap we want to be careful to minimize
    // the risk of a race that can occur here where the GC and ShadowGC don't match
    mov     DWORD PTR [edx], ebp
    mov     DWORD PTR [ecx], ebp

    // We need a scratch register to verify the shadow heap.  We also need to
    // construct a memory barrier so that the write to the shadow heap happens
    // before the read from the GC heap.  We can do both by using SUB/XCHG
    // rather than PUSH.
    sub     esp, 4
    xchg    [esp], eax

    // As part of our race avoidance (see above) we will now check whether the values
    // in the GC and ShadowGC match. There is a possibility that we're wrong here but
    // being overaggressive means we might mask a case where someone updates GC refs
    // without going to a write barrier, but by its nature it will be indeterminant
    // and we will find real bugs whereas the current implementation is indeterminant
    // but only leads to investigations that find that this code is fundamentally flawed
    mov     eax, [edx]
    cmp     [ecx], eax
    je      LOCAL_LABEL(WriteBarrier_CleanupShadowCheck_\rg)
    mov     DWORD PTR [ecx], INVALIDGCVALUE

LOCAL_LABEL(WriteBarrier_CleanupShadowCheck_\rg):
    pop     eax
    jmp     LOCAL_LABEL(WriteBarrier_ShadowCheckEnd_\rg)

LOCAL_LABEL(WriteBarrier_NoShadow_\rg):
    // If we come here then we haven't written the value to the GC and need to.
    //   ebp contains rg
    // We restore ebp/ecx immediately after this, and if either of them is the src
    // register it will regain its value as the src register.
    mov     DWORD PTR [edx], ebp
LOCAL_LABEL(WriteBarrier_ShadowCheckEnd_\rg):
    pop     ebp
    pop     ecx
#endif // WRITE_BARRIER_CHECK

    push    eax
    push    ebx
    mov     eax, \rg
    PREPARE_EXTERNAL_VAR g_ephemeral_low, ebx
    cmp     eax, [ebx]
    pop     ebx
    pop     eax
    jb      LOCAL_LABEL(WriteBarrier_NotInEphemeral_\rg)
    push    eax
    push    ebx
    mov     eax, \rg
    PREPARE_EXTERNAL_VAR g_ephemeral_high, ebx
    cmp     eax, [ebx]
    pop     ebx
    pop     eax
    jae     LOCAL_LABEL(WriteBarrier_NotInEphemeral_\rg)

    shr     edx, 10
    push    eax
    PREPARE_EXTERNAL_VAR g_card_table, eax
    add     edx, [eax]
    pop     eax
    cmp     BYTE PTR [edx], 0xFF
    jne     LOCAL_LABEL(WriteBarrier_UpdateCardTable_\rg)
    ret

LOCAL_LABEL(WriteBarrier_UpdateCardTable_\rg):
    mov     BYTE PTR [edx], 0xFF
    ret

LOCAL_LABEL(WriteBarrier_NotInHeap_\rg):
    // If it wasn't in the heap then we haven't updated the dst in memory yet
    mov     DWORD PTR [edx], \rg

LOCAL_LABEL(WriteBarrier_NotInEphemeral_\rg):
    // If it is in the GC Heap but isn't in the ephemeral range we've already
    // updated the Heap with the Object*.
    ret
NESTED_END JIT_CheckedWriteBarrier\rg, _TEXT
.endm

// JIT_WriteBarrierGroup and JIT_WriteBarrierGroup_End are used
// to determine bounds of WriteBarrier functions so can determine if got AV in them.
//
LEAF_ENTRY JIT_WriteBarrierGroup, _TEXT
    ret
LEAF_END JIT_WriteBarrierGroup, _TEXT

WriteBarrierHelper EAX
WriteBarrierHelper EBX
WriteBarrierHelper ECX
WriteBarrierHelper ESI
WriteBarrierHelper EDI
WriteBarrierHelper EBP

LEAF_ENTRY JIT_WriteBarrierGroup_End, _TEXT
    ret
LEAF_END JIT_WriteBarrierGroup_End, _TEXT
