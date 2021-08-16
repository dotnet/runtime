// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"


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
// Exit:
//
// Uses:
//    EDX is destroyed.
//
// Exceptions:
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
    ja      LOCAL_LABEL(WriteBarrier_NoShadow_\rg)

    // TODO: In Orcas timeframe if we move to P4+ only on X86 we should enable
    // mfence barriers on either side of these two writes to make sure that
    // they stay as close together as possible

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
    //
    // TODO: Should be changed to a push if the mfence described above is added.
    //
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


// ***
// JIT_ByRefWriteBarrier* - GC write barrier helper
//
// Purpose:
//    Helper calls in order to assign an object to a byref field
//    Enables book-keeping of the GC.
//
// Entry:
//    EDI - address of ref-field (assigned to)
//    ESI - address of the data  (source)
//    ECX can be trashed
//
// Exit:
//
// Uses:
//    EDI and ESI are incremented by a DWORD
//
// Exceptions:
//
// *******************************************************************************
//
//  The code here is tightly coupled with AdjustContextForJITHelpers, if you change
//  anything here, you might need to change AdjustContextForJITHelpers as well
//
.macro ByRefWriteBarrierHelper
.align 4

LEAF_ENTRY JIT_ByRefWriteBarrier, _TEXT
    // test for dest in range
    mov     ecx, [esi]
    push    eax
    PREPARE_EXTERNAL_VAR g_lowest_address, eax
    cmp     edi, [eax]
    pop     eax
    jb      LOCAL_LABEL(ByRefWriteBarrier_NotInHeap)
    push    eax
    PREPARE_EXTERNAL_VAR g_highest_address, eax
    cmp     edi, [eax]
    pop     eax
    jae     LOCAL_LABEL(ByRefWriteBarrier_NotInHeap)

#ifndef WRITE_BARRIER_CHECK
    // write barrier
    mov     [edi], ecx
#endif // !WRITE_BARRIER_CHECK

#ifdef WRITE_BARRIER_CHECK
    // Test dest here so if it is bad AV would happen before we change register/stack
    // status. This makes job of AdjustContextForJITHelpers easier.
    cmp     BYTE PTR [edi], 0

    // ALSO update the shadow GC heap if that is enabled

    // use edx for address in GC Shadow,
    push    edx

    // if g_GCShadow is 0, don't do the update
    push    ebx
    PREPARE_EXTERNAL_VAR g_GCShadow, ebx
    cmp     DWORD PTR [ebx], 0
    pop     ebx
    je      LOCAL_LABEL(ByRefWriteBarrier_NoShadow)

    mov     edx, edi
    push    ebx
    PREPARE_EXTERNAL_VAR g_lowest_address, ebx
    sub     edx, [ebx] // U/V
    pop     ebx
    jb      LOCAL_LABEL(ByRefWriteBarrier_NoShadow)
    push    eax
    PREPARE_EXTERNAL_VAR g_GCShadow, eax
    mov     eax, [eax]
    add     edx, [eax]
    PREPARE_EXTERNAL_VAR g_GCShadowEnd, eax
    mov     eax, [eax]
    cmp     edx, [eax]
    pop     eax
    ja      LOCAL_LABEL(ByRefWriteBarrier_NoShadow)

    // TODO: In Orcas timeframe if we move to P4+ only on X86 we should enable
    // mfence barriers on either side of these two writes to make sure that
    // they stay as close together as possible

    // edi contains address in GC
    // edx contains address in ShadowGC
    // ecx is the value to assign

    // When we're writing to the shadow GC heap we want to be careful to minimize
    // the risk of a race that can occur here where the GC and ShadowGC don't match
    mov     DWORD PTR [edi], ecx
    mov     DWORD PTR [edx], ecx

    // We need a scratch register to verify the shadow heap.  We also need to
    // construct a memory barrier so that the write to the shadow heap happens
    // before the read from the GC heap.  We can do both by using SUB/XCHG
    // rather than PUSH.
    //
    // TODO: Should be changed to a push if the mfence described above is added.
    //
    sub     esp, 4
    xchg    [esp], eax

    // As part of our race avoidance (see above) we will now check whether the values
    // in the GC and ShadowGC match. There is a possibility that we're wrong here but
    // being overaggressive means we might mask a case where someone updates GC refs
    // without going to a write barrier, but by its nature it will be indeterminant
    // and we will find real bugs whereas the current implementation is indeterminant
    // but only leads to investigations that find that this code is fundamentally flawed

    mov     eax, [edi]
    cmp     [edx], eax
    je      LOCAL_LABEL(ByRefWriteBarrier_CleanupShadowCheck)
    mov     DWORD PTR [edx], INVALIDGCVALUE
LOCAL_LABEL(ByRefWriteBarrier_CleanupShadowCheck):
    pop     eax
    jmp     LOCAL_LABEL(ByRefWriteBarrier_ShadowCheckEnd)

LOCAL_LABEL(ByRefWriteBarrier_NoShadow):
    // If we come here then we haven't written the value to the GC and need to.
    mov     DWORD PTR [edi], ecx

LOCAL_LABEL(ByRefWriteBarrier_ShadowCheckEnd):
    pop     edx
#endif // WRITE_BARRIER_CHECK

    // test for *src in ephemeral segement
    push    eax
    PREPARE_EXTERNAL_VAR g_ephemeral_low, eax
    cmp     ecx, [eax]
    pop     eax
    jb      LOCAL_LABEL(ByRefWriteBarrier_NotInEphemeral)
    push    eax
    PREPARE_EXTERNAL_VAR g_ephemeral_high, eax
    cmp     ecx, [eax]
    pop     eax
    jae     LOCAL_LABEL(ByRefWriteBarrier_NotInEphemeral)

    mov     ecx, edi
    add     esi, 4
    add     edi, 4

    shr     ecx, 10
    push    eax
    PREPARE_EXTERNAL_VAR g_card_table, eax
    add     ecx, [eax]
    pop     eax
    cmp     BYTE PTR [ecx], 0xFF
    jne     LOCAL_LABEL(ByRefWriteBarrier_UpdateCardTable)
    ret
LOCAL_LABEL(ByRefWriteBarrier_UpdateCardTable):
    mov     BYTE PTR [ecx], 0xFF
    ret

LOCAL_LABEL(ByRefWriteBarrier_NotInHeap):
    // If it wasn't in the heap then we haven't updated the dst in memory yet
    mov     [edi], ecx
LOCAL_LABEL(ByRefWriteBarrier_NotInEphemeral):
    // If it is in the GC Heap but isn't in the ephemeral range we've already
    // updated the Heap with the Object*.
    add     esi, 4
    add     edi, 4
    ret
NESTED_END JIT_ByRefWriteBarrier, _TEXT

.endm

// JIT_WriteBarrierGroup and JIT_WriteBarrierGroup_End are used
// to determine bounds of WriteBarrier functions so can determine if got AV in them.
//
LEAF_ENTRY JIT_WriteBarrierGroup, _TEXT
    ret
LEAF_END JIT_WriteBarrierGroup, _TEXT

    .data
    .align 4
    .global C_FUNC(JIT_WriteBarrierEAX_Loc)
C_FUNC(JIT_WriteBarrierEAX_Loc):
    .word 0
    .text

LEAF_ENTRY JIT_WriteBarrier_Callable, _TEXT
    mov eax, edx
    mov edx, ecx
    push    eax
    call    1f
1:
    pop     eax
2:
.att_syntax
    addl     $_GLOBAL_OFFSET_TABLE_+(2b-1b), %eax
.intel_syntax noprefix
    mov     eax, dword ptr [eax + C_FUNC(JIT_WriteBarrierEAX_Loc)@GOT]
    xchg    eax, dword ptr [esp]
    ret
LEAF_END JIT_WriteBarrier_Callable, _TEXT

.macro UniversalWriteBarrierHelper name
.align 4

LEAF_ENTRY JIT_\name, _TEXT
    mov eax, edx
    mov edx, ecx
    jmp C_FUNC(JIT_\name\()EAX)
LEAF_END JIT_\name, _TEXT

.endm

#ifdef FEATURE_USE_ASM_GC_WRITE_BARRIERS
// *******************************************************************************
//  Write barrier wrappers with fcall calling convention
//

// Only define these if we're using the ASM GC write barriers; if this flag is not defined,
// we'll use C++ versions of these write barriers.
UniversalWriteBarrierHelper CheckedWriteBarrier
UniversalWriteBarrierHelper WriteBarrier
#endif // FEATURE_USE_ASM_GC_WRITE_BARRIERS

WriteBarrierHelper EAX
WriteBarrierHelper EBX
WriteBarrierHelper ECX
WriteBarrierHelper ESI
WriteBarrierHelper EDI
WriteBarrierHelper EBP

ByRefWriteBarrierHelper

// This is the first function outside the "keep together range". Used by BBT scripts.
LEAF_ENTRY JIT_WriteBarrierGroup_End, _TEXT
    ret
LEAF_END JIT_WriteBarrierGroup_End, _TEXT

// *********************************************************************/
// llshl - long shift left
//
// Purpose:
//    Does a Long Shift Left (signed and unsigned are identical)
//    Shifts a long left any number of bits.
//
//        NOTE:  This routine has been adapted from the Microsoft CRTs.
//
// Entry:
//    EDX:EAX - long value to be shifted
//        ECX - number of bits to shift by
//
// Exit:
//    EDX:EAX - shifted value
//
.align 16
LEAF_ENTRY JIT_LLsh, _TEXT
    // Reduce shift amount mod 64
    and     ecx, 63

    cmp     ecx, 32
    jae     LOCAL_LABEL(LLshMORE32)

    // Handle shifts of between bits 0 and 31
    shld    edx, eax, cl
    shl     eax, cl
    ret

LOCAL_LABEL(LLshMORE32):
    // Handle shifts of between bits 32 and 63
    // The x86 shift instructions only use the lower 5 bits.
    mov     edx, eax
    xor     eax, eax
    shl     edx, cl
    ret
LEAF_END JIT_LLsh, _TEXT

// *********************************************************************/
// LRsh - long shift right
//
// Purpose:
//    Does a signed Long Shift Right
//    Shifts a long right any number of bits.
//
//        NOTE:  This routine has been adapted from the Microsoft CRTs.
//
// Entry:
//    EDX:EAX - long value to be shifted
//        ECX - number of bits to shift by
//
// Exit:
//    EDX:EAX - shifted value
//
.align 16
LEAF_ENTRY JIT_LRsh, _TEXT
    // Reduce shift amount mod 64
    and     ecx, 63

    cmp     ecx, 32
    jae     LOCAL_LABEL(LRshMORE32)

    // Handle shifts of between bits 0 and 31
    shrd    eax, edx, cl
    sar     edx, cl
    ret

LOCAL_LABEL(LRshMORE32):
    // Handle shifts of between bits 32 and 63
    // The x86 shift instructions only use the lower 5 bits.
    mov     eax, edx
    sar     edx, 31
    sar     eax, cl
    ret
LEAF_END JIT_LRsh, _TEXT

// *********************************************************************/
//  LRsz:
// Purpose:
//    Does a unsigned Long Shift Right
//    Shifts a long right any number of bits.
//
//        NOTE:  This routine has been adapted from the Microsoft CRTs.
//
// Entry:
//    EDX:EAX - long value to be shifted
//        ECX - number of bits to shift by
//
// Exit:
//    EDX:EAX - shifted value
//
.align 16
LEAF_ENTRY JIT_LRsz, _TEXT
    // Reduce shift amount mod 64
    and     ecx, 63

    cmp     ecx, 32
    jae     LOCAL_LABEL(LRszMORE32)

    // Handle shifts of between bits 0 and 31
    shrd    eax, edx, cl
    shr     edx, cl
    ret

LOCAL_LABEL(LRszMORE32):
    // Handle shifts of between bits 32 and 63
    // The x86 shift instructions only use the lower 5 bits.
    mov     eax, edx
    xor     edx, edx
    shr     eax, cl
    ret
LEAF_END JIT_LRsz, _TEXT

// *********************************************************************/
//  JIT_Dbl2LngP4x87
//
// Purpose:
//   converts a double to a long truncating toward zero (C semantics)
//
// uses stdcall calling conventions
//
//   This code is faster on a P4 than the Dbl2Lng code above, but is
//   slower on a PIII.  Hence we choose this code when on a P4 or above.
//
LEAF_ENTRY JIT_Dbl2LngP4x87, _TEXT
    // get some local space
    sub 	esp, 8

    #define arg1 [esp + 0x0C]
    fld     QWORD PTR arg1          // fetch arg
    fnstcw  WORD PTR arg1           // store FPCW
    movzx   eax, WORD PTR arg1      // zero extend - wide
    or      ah, 0x0C                // turn on OE and DE flags
    mov     DWORD PTR [esp], eax    // store new FPCW bits
    fldcw   WORD PTR  [esp]         // reload FPCW with new bits
    fistp   QWORD PTR [esp]         // convert

    // reload FP result
    mov	    eax, DWORD PTR [esp]
    mov	    edx, DWORD PTR [esp + 4]

    // reload original FPCW value
    fldcw   WORD PTR arg1
    #undef arg1

    // restore stack
    add     esp, 8

    ret
LEAF_END JIT_Dbl2LngP4x87, _TEXT

// *********************************************************************/
//  JIT_Dbl2LngSSE3
//
// Purpose:
//    converts a double to a long truncating toward zero (C semantics)
//
//  uses stdcall calling conventions
//
//    This code is faster than the above P4 x87 code for Intel processors
//    equal or later than Core2 and Atom that have SSE3 support
//
LEAF_ENTRY JIT_Dbl2LngSSE3, _TEXT
    // get some local space
    sub     esp, 8

    fld     QWORD PTR [esp + 0x0C]   // fetch arg
    fisttp  QWORD PTR [esp]         // convert
    mov     eax, DWORD PTR [esp]    // reload FP result
    mov     edx, DWORD PTR [esp + 4]

    // restore stack
    add     esp, 8

    ret
LEAF_END JIT_Dbl2LngSSE3, _TEXT

// *********************************************************************/
// JIT_Dbl2IntSSE2
//
// Purpose:
//  converts a double to a long truncating toward zero (C semantics)
//
// uses stdcall calling conventions
//
// This code is even faster than the P4 x87 code for Dbl2LongP4x87,
// but only returns a 32 bit value (only good for int).
//
LEAF_ENTRY JIT_Dbl2IntSSE2, _TEXT
    movsd     xmm0, [esp + 4]
    cvttsd2si eax, xmm0
    ret
LEAF_END JIT_Dbl2IntSSE2, _TEXT

// *********************************************************************/
// JIT_StackProbe
//
// Purpose:
//   the helper will access ("probe") a word on each page of the stack
//   starting with the page right beneath esp down to the one pointed to by eax.
//   The procedure is needed to make sure that the "guard" page is pushed down below the allocated stack frame.
//   The call to the helper will be emitted by JIT in the function prolog when large (larger than 0x3000 bytes) stack frame is required.
//
// NOTE: this helper will modify a value of esp and must establish the frame pointer.
// NOTE: On Linux we must advance the stack pointer as we probe - it is not allowed to access 65535 bytes below esp.
//
#define PROBE_PAGE_SIZE 0x1000
NESTED_ENTRY JIT_StackProbe, _TEXT, NoHandler
    // On entry:
    //   eax - the lowest address of the stack frame being allocated (i.e. [InitialSp - FrameSize])
    //
    // NOTE: this helper will probe at least one page below the one pointed by esp.
    PROLOG_BEG
    PROLOG_END

    and     esp, -PROBE_PAGE_SIZE  // esp points to the **lowest address** on the last probed page
                                   // This is done to make the loop end condition simpler.

LOCAL_LABEL(ProbeLoop):
    sub     esp, PROBE_PAGE_SIZE   // esp points to the lowest address of the **next page** to probe
    test    [esp], eax             // esp points to the lowest address on the **last probed** page
    cmp     esp, eax
    jg      LOCAL_LABEL(ProbeLoop) // if esp > eax, then we need to probe at least one more page.

    EPILOG_BEG
    mov     esp, ebp
    EPILOG_END
    ret

NESTED_END JIT_StackProbe, _TEXT

// *********************************************************************/
//  This is the small write barrier thunk we use when we know the
//  ephemeral generation is higher in memory than older generations.
//  The 0x0F0F0F0F values are bashed by the two functions above.
//  This the generic version - wherever the code says ECX,
//  the specific register is patched later into a copy
//  Note: do not replace ECX by EAX - there is a smaller encoding for
//  the compares just for EAX, which won't work for other registers.
//
//  READ THIS!!!!!!
//  it is imperative that the addresses of of the values that we overwrite
//  (card table, ephemeral region ranges, etc) are naturally aligned since
//  there are codepaths that will overwrite these values while the EE is running.
//
LEAF_ENTRY JIT_WriteBarrierReg_PreGrow, _TEXT
    mov     DWORD PTR [edx], ecx
    cmp     ecx, 0xF0F0F0F0
    jb      LOCAL_LABEL(NoWriteBarrierPre)

    shr     edx, 10
    nop     // padding for alignment of constant
    cmp     BYTE PTR [edx + 0xF0F0F0F0], 0xFF
    jne     LOCAL_LABEL(WriteBarrierPre)

LOCAL_LABEL(NoWriteBarrierPre):
    ret
    nop     // padding for alignment of constant
    nop     // padding for alignment of constant

LOCAL_LABEL(WriteBarrierPre):
    mov     BYTE PTR [edx+0xF0F0F0F0], 0xFF
    ret
LEAF_END JIT_WriteBarrierReg_PreGrow, _TEXT

// *********************************************************************/
//  This is the larger write barrier thunk we use when we know that older
//  generations may be higher in memory than the ephemeral generation
//  The 0x0F0F0F0F values are bashed by the two functions above.
//  This the generic version - wherever the code says ECX,
//  the specific register is patched later into a copy
//  Note: do not replace ECX by EAX - there is a smaller encoding for
//  the compares just for EAX, which won't work for other registers.
//  NOTE: we need this aligned for our validation to work properly
.align 4
LEAF_ENTRY JIT_WriteBarrierReg_PostGrow, _TEXT
    mov     DWORD PTR [edx], ecx
    cmp     ecx, 0xF0F0F0F0
    jb      LOCAL_LABEL(NoWriteBarrierPost)
    cmp     ecx, 0xF0F0F0F0
    jae     LOCAL_LABEL(NoWriteBarrierPost)

    shr     edx, 10
    nop     // padding for alignment of constant
    cmp     BYTE PTR [edx + 0xF0F0F0F0], 0xFF
    jne     LOCAL_LABEL(WriteBarrierPost)

LOCAL_LABEL(NoWriteBarrierPost):
    ret
    nop     // padding for alignment of constant
    nop     // padding for alignment of constant

LOCAL_LABEL(WriteBarrierPost):
    mov     BYTE PTR [edx + 0xF0F0F0F0], 0xFF
    ret
LEAF_END JIT_WriteBarrierReg_PostGrow,_TEXT

// PatchedCodeStart and PatchedCodeEnd are used to determine bounds of patched code.
//

LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
    ret
LEAF_END JIT_PatchedCodeStart, _TEXT

// **********************************************************************
// Write barriers generated at runtime

LEAF_ENTRY JIT_PatchedWriteBarrierGroup, _TEXT
    ret
LEAF_END JIT_PatchedWriteBarrierGroup, _TEXT

.macro PatchedWriteBarrierHelper rg
.align 8
LEAF_ENTRY JIT_WriteBarrier\rg, _TEXT
    // Just allocate space that will be filled in at runtime
    .space 0xCC, 48
LEAF_END JIT_WriteBarrier\rg, _TEXT

.endm

PatchedWriteBarrierHelper EAX
PatchedWriteBarrierHelper EBX
PatchedWriteBarrierHelper ECX
PatchedWriteBarrierHelper ESI
PatchedWriteBarrierHelper EDI
PatchedWriteBarrierHelper EBP

// This is the first function outside the "keep together range". Used by BBT scripts.
LEAF_ENTRY JIT_PatchedWriteBarrierGroup_End, _TEXT
    ret
LEAF_END JIT_PatchedWriteBarrierGroup_End, _TEXT

LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
    ret
LEAF_END JIT_PatchedCodeLast, _TEXT
