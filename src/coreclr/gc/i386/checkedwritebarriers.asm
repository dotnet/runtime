; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat

        include asmconstants.inc
        include asmmacros.inc

        option  casemap:none
        option  noscoped
        .code

g_ephemeral_low        TEXTEQU <_g_ephemeral_low>
g_ephemeral_high       TEXTEQU <_g_ephemeral_high>
g_lowest_address       TEXTEQU <_g_lowest_address>
g_highest_address      TEXTEQU <_g_highest_address>
g_card_table           TEXTEQU <_g_card_table>
WriteBarrierAssert     TEXTEQU <_WriteBarrierAssert@8>

EXTERN g_ephemeral_low:DWORD
EXTERN g_ephemeral_high:DWORD
EXTERN g_lowest_address:DWORD
EXTERN g_highest_address:DWORD
EXTERN g_card_table:DWORD
ifdef _DEBUG
EXTERN WriteBarrierAssert:PROC
endif

ifdef WRITE_BARRIER_CHECK
g_GCShadow             TEXTEQU <?g_GCShadow@@3PAEA>
g_GCShadowEnd          TEXTEQU <?g_GCShadowEnd@@3PAEA>
EXTERN g_GCShadow:DWORD
EXTERN g_GCShadowEnd:DWORD
INVALIDGCVALUE equ 0CCCCCCCDh
endif

;***
;JIT_WriteBarrier* - GC write barrier helper
;
;Purpose:
;   Helper calls in order to assign an object to a field
;   Enables book-keeping of the GC.
;
;Entry:
;   EDX - address of ref-field (assigned to)
;   the resp. other reg - RHS of assignment
;
;Uses:
;       EDX is destroyed.
;
;*******************************************************************************

; The code here is tightly coupled with AdjustContextForJITHelpers, if you change
; anything here, you might need to change AdjustContextForJITHelpers as well
WriteBarrierHelper MACRO rg
        ALIGN 4

    ;; The entry point is the fully 'safe' one in which we check if EDX (the REF
    ;; begin updated) is actually in the GC heap

PUBLIC _JIT_CheckedWriteBarrier&rg&@0
_JIT_CheckedWriteBarrier&rg&@0 PROC
        ;; check in the REF being updated is in the GC heap
        cmp     edx, g_lowest_address
        jb      WriteBarrier_NotInHeap_&rg
        cmp     edx, g_highest_address
        jae     WriteBarrier_NotInHeap_&rg

        ;; fall through to unchecked routine
        ;; note that its entry point also happens to be aligned

ifdef WRITE_BARRIER_CHECK
    ;; This entry point is used when you know the REF pointer being updated
    ;; is in the GC heap
PUBLIC _JIT_DebugWriteBarrier&rg&@0
_JIT_DebugWriteBarrier&rg&@0:
endif

ifdef _DEBUG
        push    edx
        push    ecx
        push    eax

        push    rg
        push    edx
        call    WriteBarrierAssert

        pop     eax
        pop     ecx
        pop     edx
endif

        ; in the !WRITE_BARRIER_CHECK case this will be the move for all
        ; addresses in the GCHeap, addresses outside the GCHeap will get
        ; taken care of below at WriteBarrier_NotInHeap_&rg

ifndef WRITE_BARRIER_CHECK
        mov     DWORD PTR [edx], rg
endif

ifdef WRITE_BARRIER_CHECK
        ; Test dest here so if it is bad AV would happen before we change register/stack
        ; status. This makes job of AdjustContextForJITHelpers easier.
        cmp     DWORD PTR [edx], 0
        ;; ALSO update the shadow GC heap if that is enabled
        ; Make ebp into the temporary src register. We need to do this so that we can use ecx
        ; in the calculation of the shadow GC address, but still have access to the src register
        push    ecx
        push    ebp
        mov     ebp, rg

        ; if g_GCShadow is 0, don't perform the check
        cmp     g_GCShadow, 0
        je      WriteBarrier_NoShadow_&rg

        mov     ecx, edx
        sub     ecx, g_lowest_address
        jb      WriteBarrier_NoShadow_&rg
        add     ecx, [g_GCShadow]
        cmp     ecx, [g_GCShadowEnd]
        jae     WriteBarrier_NoShadow_&rg

        ; edx contains address in GC
        ; ecx contains address in ShadowGC
        ; ebp temporarially becomes the src register

        ;; When we're writing to the shadow GC heap we want to be careful to minimize
        ;; the risk of a race that can occur here where the GC and ShadowGC don't match
        mov     DWORD PTR [edx], ebp
        mov     DWORD PTR [ecx], ebp

        ;; We need a scratch register to verify the shadow heap.  We also need to
        ;; construct a memory barrier so that the write to the shadow heap happens
        ;; before the read from the GC heap.  We can do both by using SUB/XCHG
        ;; rather than PUSH.
        sub     esp, 4
        xchg    [esp], eax

        ;; As part of our race avoidance (see above) we will now check whether the values
        ;; in the GC and ShadowGC match. There is a possibility that we're wrong here but
        ;; being overaggressive means we might mask a case where someone updates GC refs
        ;; without going to a write barrier, but by its nature it will be indeterminant
        ;; and we will find real bugs whereas the current implementation is indeterminant
        ;; but only leads to investigations that find that this code is fundamentally flawed
        mov     eax, [edx]
        cmp     [ecx], eax
        je      WriteBarrier_CleanupShadowCheck_&rg
        mov     DWORD PTR [ecx], INVALIDGCVALUE

WriteBarrier_CleanupShadowCheck_&rg:
        pop     eax
        jmp     WriteBarrier_ShadowCheckEnd_&rg

WriteBarrier_NoShadow_&rg:
        ; If we come here then we haven't written the value to the GC and need to.
        ;   ebp contains rg
        ; We restore ebp/ecx immediately after this, and if either of them is the src
        ; register it will regain its value as the src register.
        mov     DWORD PTR [edx], ebp
WriteBarrier_ShadowCheckEnd_&rg:
        pop     ebp
        pop     ecx
endif

        cmp     rg, g_ephemeral_low
        jb      WriteBarrier_NotInEphemeral_&rg
        cmp     rg, g_ephemeral_high
        jae     WriteBarrier_NotInEphemeral_&rg

        shr     edx, 10
        add     edx, [g_card_table]
        cmp     BYTE PTR [edx], 0FFh
        jne     WriteBarrier_UpdateCardTable_&rg
        ret

WriteBarrier_UpdateCardTable_&rg:
        mov     BYTE PTR [edx], 0FFh
        ret

WriteBarrier_NotInHeap_&rg:
        ; If it wasn't in the heap then we haven't updated the dst in memory yet
        mov     DWORD PTR [edx], rg
WriteBarrier_NotInEphemeral_&rg:
        ; If it is in the GC Heap but isn't in the ephemeral range we've already
        ; updated the Heap with the Object*.
        ret
_JIT_CheckedWriteBarrier&rg&@0 ENDP
ENDM

; JIT_WriteBarrierGroup and JIT_WriteBarrierGroup_End are used
; to determine bounds of WriteBarrier functions so can determine if got AV in them.
;
PUBLIC _JIT_WriteBarrierGroup@0
_JIT_WriteBarrierGroup@0 PROC
        ret
_JIT_WriteBarrierGroup@0 ENDP

WriteBarrierHelper <EAX>
WriteBarrierHelper <EBX>
WriteBarrierHelper <ECX>
WriteBarrierHelper <ESI>
WriteBarrierHelper <EDI>
WriteBarrierHelper <EBP>

; This is the first function outside the "keep together range". Used by BBT scripts.
PUBLIC _JIT_WriteBarrierGroup_End@0
_JIT_WriteBarrierGroup_End@0 PROC
        ret
_JIT_WriteBarrierGroup_End@0 ENDP

        end
