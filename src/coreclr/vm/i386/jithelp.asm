; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: JIThelp.asm
;
; ***********************************************************************
;
;  *** NOTE:  If you make changes to this file, propagate the changes to
;             jithelp.s in this directory

; This contains JITinterface routines that are 100% x86 assembly

        .586
        .model  flat

        include asmconstants.inc
        include asmmacros.inc

        option  casemap:none
        .code
;
; <TODO>@TODO Switch to g_ephemeral_low and g_ephemeral_high
; @TODO instead of g_lowest_address, g_highest address</TODO>
;

ARGUMENT_REG1           equ     ecx
ARGUMENT_REG2           equ     edx
g_ephemeral_low                 TEXTEQU <_g_ephemeral_low>
g_ephemeral_high                TEXTEQU <_g_ephemeral_high>
g_lowest_address                TEXTEQU <_g_lowest_address>
g_highest_address               TEXTEQU <_g_highest_address>
g_card_table                    TEXTEQU <_g_card_table>
WriteBarrierAssert              TEXTEQU <_WriteBarrierAssert@8>
JIT_LLsh                        TEXTEQU <_JIT_LLsh@0>
JIT_LRsh                        TEXTEQU <_JIT_LRsh@0>
JIT_LRsz                        TEXTEQU <_JIT_LRsz@0>
JIT_LMul                        TEXTEQU <@JIT_LMul@16>
JIT_Dbl2LngOvf                  TEXTEQU <@JIT_Dbl2LngOvf@8>
JIT_Dbl2Lng                     TEXTEQU <@JIT_Dbl2Lng@8>
JIT_Dbl2IntSSE2                 TEXTEQU <@JIT_Dbl2IntSSE2@8>
JIT_Dbl2LngP4x87                TEXTEQU <@JIT_Dbl2LngP4x87@8>
JIT_Dbl2LngSSE3	                TEXTEQU <@JIT_Dbl2LngSSE3@8>
JIT_InternalThrowFromHelper     TEXTEQU <@JIT_InternalThrowFromHelper@4>
JIT_WriteBarrierReg_PreGrow     TEXTEQU <_JIT_WriteBarrierReg_PreGrow@0>
JIT_WriteBarrierReg_PostGrow    TEXTEQU <_JIT_WriteBarrierReg_PostGrow@0>
JIT_TailCall                    TEXTEQU <_JIT_TailCall@0>
JIT_TailCallLeave               TEXTEQU <_JIT_TailCallLeave@0>
JIT_TailCallVSDLeave            TEXTEQU <_JIT_TailCallVSDLeave@0>
JIT_TailCallHelper              TEXTEQU <_JIT_TailCallHelper@4>
JIT_TailCallReturnFromVSD       TEXTEQU <_JIT_TailCallReturnFromVSD@0>

EXTERN  g_ephemeral_low:DWORD
EXTERN  g_ephemeral_high:DWORD
EXTERN  g_lowest_address:DWORD
EXTERN  g_highest_address:DWORD
EXTERN  g_card_table:DWORD
ifdef _DEBUG
EXTERN  WriteBarrierAssert:PROC
endif ; _DEBUG
EXTERN  JIT_InternalThrowFromHelper:PROC
ifdef FEATURE_HIJACK
EXTERN  JIT_TailCallHelper:PROC
endif
EXTERN _g_TailCallFrameVptr:DWORD
EXTERN @JIT_FailFast@0:PROC
EXTERN _s_gsCookie:DWORD

ifdef WRITE_BARRIER_CHECK
; Those global variables are always defined, but should be 0 for Server GC
g_GCShadow                      TEXTEQU <?g_GCShadow@@3PAEA>
g_GCShadowEnd                   TEXTEQU <?g_GCShadowEnd@@3PAEA>
EXTERN  g_GCShadow:DWORD
EXTERN  g_GCShadowEnd:DWORD
INVALIDGCVALUE equ 0CCCCCCCDh
endif

EXTERN _COMPlusEndCatch@20:PROC

.686P
.XMM
; The following macro is needed because of a MASM issue with the
; movsd mnemonic
;
$movsd MACRO op1, op2
    LOCAL begin_movsd, end_movsd
begin_movsd:
    movupd op1, op2
end_movsd:
    org begin_movsd
    db 0F2h
    org end_movsd
ENDM
.586

; The following macro is used to match the JITs
; multi-byte NOP sequence
$nop3 MACRO
    db 090h
    db 090h
    db 090h
ENDM



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
;Exit:
;
;Uses:
;       EDX is destroyed.
;
;Exceptions:
;
;*******************************************************************************

; The code here is tightly coupled with AdjustContextForJITHelpers, if you change
; anything here, you might need to change AdjustContextForJITHelpers as well
; Note that beside the AV case, we might be unwinding inside the region where we have
; already push ecx and ebp in the branch under FEATURE_DATABREAKPOINT
WriteBarrierHelper MACRO rg
        ALIGN 4

    ;; The entry point is the fully 'safe' one in which we check if EDX (the REF
    ;; begin updated) is actually in the GC heap

PUBLIC _JIT_CheckedWriteBarrier&rg&@0
_JIT_CheckedWriteBarrier&rg&@0 PROC
        ;; check in the REF being updated is in the GC heap
        cmp             edx, g_lowest_address
        jb              WriteBarrier_NotInHeap_&rg
        cmp             edx, g_highest_address
        jae             WriteBarrier_NotInHeap_&rg

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
endif ;_DEBUG

        ; in the !WRITE_BARRIER_CHECK case this will be the move for all
        ; addresses in the GCHeap, addresses outside the GCHeap will get
        ; taken care of below at WriteBarrier_NotInHeap_&rg

ifndef WRITE_BARRIER_CHECK
        mov     DWORD PTR [edx], rg
endif

ifdef WRITE_BARRIER_CHECK
        ; Test dest here so if it is bad AV would happen before we change register/stack
        ; status. This makes job of AdjustContextForJITHelpers easier.
        cmp     [edx], 0
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
        sub     ecx, g_lowest_address   ; U/V
        jb      WriteBarrier_NoShadow_&rg
        add     ecx, [g_GCShadow]
        cmp     ecx, [g_GCShadowEnd]
        jae     WriteBarrier_NoShadow_&rg

        ; TODO: In Orcas timeframe if we move to P4+ only on X86 we should enable
        ; mfence barriers on either side of these two writes to make sure that
        ; they stay as close together as possible

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
        ;;
        ;; TODO: Should be changed to a push if the mfence described above is added.
        ;;
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
        mov     [ecx], INVALIDGCVALUE

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


;***
;JIT_ByRefWriteBarrier* - GC write barrier helper
;
;Purpose:
;   Helper calls in order to assign an object to a byref field
;   Enables book-keeping of the GC.
;
;Entry:
;   EDI - address of ref-field (assigned to)
;   ESI - address of the data  (source)
;   ECX can be trashed
;
;Exit:
;
;Uses:
;   EDI and ESI are incremented by a DWORD
;
;Exceptions:
;
;*******************************************************************************

; The code here is tightly coupled with AdjustContextForJITHelpers, if you change
; anything here, you might need to change AdjustContextForJITHelpers as well

ByRefWriteBarrierHelper MACRO
        ALIGN 4
PUBLIC _JIT_ByRefWriteBarrier@0
_JIT_ByRefWriteBarrier@0 PROC
        ;;test for dest in range
        mov     ecx, [esi]
        cmp     edi, g_lowest_address
        jb      ByRefWriteBarrier_NotInHeap
        cmp     edi, g_highest_address
        jae     ByRefWriteBarrier_NotInHeap

ifndef WRITE_BARRIER_CHECK
        ;;write barrier
        mov     [edi],ecx
endif

ifdef WRITE_BARRIER_CHECK
        ; Test dest here so if it is bad AV would happen before we change register/stack
        ; status. This makes job of AdjustContextForJITHelpers easier.
        cmp     [edi], 0

        ;; ALSO update the shadow GC heap if that is enabled

        ; use edx for address in GC Shadow,
        push    edx

        ;if g_GCShadow is 0, don't do the update
        cmp     g_GCShadow, 0
        je      ByRefWriteBarrier_NoShadow

        mov     edx, edi
        sub     edx, g_lowest_address   ; U/V
        jb      ByRefWriteBarrier_NoShadow
        add     edx, [g_GCShadow]
        cmp     edx, [g_GCShadowEnd]
        jae     ByRefWriteBarrier_NoShadow

        ; TODO: In Orcas timeframe if we move to P4+ only on X86 we should enable
        ; mfence barriers on either side of these two writes to make sure that
        ; they stay as close together as possible

        ; edi contains address in GC
        ; edx contains address in ShadowGC
        ; ecx is the value to assign

        ;; When we're writing to the shadow GC heap we want to be careful to minimize
        ;; the risk of a race that can occur here where the GC and ShadowGC don't match
        mov     DWORD PTR [edi], ecx
        mov     DWORD PTR [edx], ecx

        ;; We need a scratch register to verify the shadow heap.  We also need to
        ;; construct a memory barrier so that the write to the shadow heap happens
        ;; before the read from the GC heap.  We can do both by using SUB/XCHG
        ;; rather than PUSH.
        ;;
        ;; TODO: Should be changed to a push if the mfence described above is added.
        ;;
        sub     esp, 4
        xchg    [esp], eax

        ;; As part of our race avoidance (see above) we will now check whether the values
        ;; in the GC and ShadowGC match. There is a possibility that we're wrong here but
        ;; being overaggressive means we might mask a case where someone updates GC refs
        ;; without going to a write barrier, but by its nature it will be indeterminant
        ;; and we will find real bugs whereas the current implementation is indeterminant
        ;; but only leads to investigations that find that this code is fundamentally flawed

        mov     eax, [edi]
        cmp     [edx], eax
        je      ByRefWriteBarrier_CleanupShadowCheck
        mov     [edx], INVALIDGCVALUE
ByRefWriteBarrier_CleanupShadowCheck:
        pop     eax
        jmp     ByRefWriteBarrier_ShadowCheckEnd

ByRefWriteBarrier_NoShadow:
        ; If we come here then we haven't written the value to the GC and need to.
        mov     DWORD PTR [edi], ecx

ByRefWriteBarrier_ShadowCheckEnd:
        pop     edx
endif
        ;;test for *src in ephemeral segement
        cmp     ecx, g_ephemeral_low
        jb      ByRefWriteBarrier_NotInEphemeral
        cmp     ecx, g_ephemeral_high
        jae     ByRefWriteBarrier_NotInEphemeral

        mov     ecx, edi
        add     esi,4
        add     edi,4

        shr     ecx, 10
        add     ecx, [g_card_table]
        cmp     byte ptr [ecx], 0FFh
        jne     ByRefWriteBarrier_UpdateCardTable
        ret
ByRefWriteBarrier_UpdateCardTable:
        mov     byte ptr [ecx], 0FFh
        ret

ByRefWriteBarrier_NotInHeap:
        ; If it wasn't in the heap then we haven't updated the dst in memory yet
        mov     [edi],ecx
ByRefWriteBarrier_NotInEphemeral:
        ; If it is in the GC Heap but isn't in the ephemeral range we've already
        ; updated the Heap with the Object*.
        add     esi,4
        add     edi,4
        ret
_JIT_ByRefWriteBarrier@0 ENDP
ENDM

;*******************************************************************************
; Write barrier wrappers with fcall calling convention
;

        .data
        ALIGN 4
        public  _JIT_WriteBarrierEAX_Loc
_JIT_WriteBarrierEAX_Loc dd 0

        .code

; WriteBarrierStart and WriteBarrierEnd are used to determine bounds of
; WriteBarrier functions so can determine if got AV in them.
;
PUBLIC _JIT_WriteBarrierGroup@0
_JIT_WriteBarrierGroup@0 PROC
ret
_JIT_WriteBarrierGroup@0 ENDP

        ALIGN 4
PUBLIC @JIT_WriteBarrier_Callable@8
@JIT_WriteBarrier_Callable@8 PROC
        mov eax,edx
        mov edx,ecx
        jmp DWORD PTR [_JIT_WriteBarrierEAX_Loc]

@JIT_WriteBarrier_Callable@8 ENDP

UniversalWriteBarrierHelper MACRO name
        ALIGN 4
PUBLIC @JIT_&name&@8
@JIT_&name&@8 PROC
        mov eax,edx
        mov edx,ecx
        jmp _JIT_&name&EAX@0
@JIT_&name&@8 ENDP
ENDM

ifdef FEATURE_USE_ASM_GC_WRITE_BARRIERS
; Only define these if we're using the ASM GC write barriers; if this flag is not defined,
; we'll use C++ versions of these write barriers.
UniversalWriteBarrierHelper <CheckedWriteBarrier>
UniversalWriteBarrierHelper <WriteBarrier>
endif

WriteBarrierHelper <EAX>
WriteBarrierHelper <EBX>
WriteBarrierHelper <ECX>
WriteBarrierHelper <ESI>
WriteBarrierHelper <EDI>
WriteBarrierHelper <EBP>

ByRefWriteBarrierHelper

; This is the first function outside the "keep together range". Used by BBT scripts.
PUBLIC _JIT_WriteBarrierGroup_End@0
_JIT_WriteBarrierGroup_End@0 PROC
ret
_JIT_WriteBarrierGroup_End@0 ENDP

;*********************************************************************/
;llshl - long shift left
;
;Purpose:
;   Does a Long Shift Left (signed and unsigned are identical)
;   Shifts a long left any number of bits.
;
;       NOTE:  This routine has been adapted from the Microsoft CRTs.
;
;Entry:
;   EDX:EAX - long value to be shifted
;       ECX - number of bits to shift by
;
;Exit:
;   EDX:EAX - shifted value
;
        ALIGN 16
PUBLIC JIT_LLsh
JIT_LLsh PROC
; Reduce shift amount mod 64
        and     ecx, 63
; Handle shifts of between bits 0 and 31
        cmp     ecx, 32
        jae     short LLshMORE32
        shld    edx,eax,cl
        shl     eax,cl
        ret
; Handle shifts of between bits 32 and 63
LLshMORE32:
        ; The x86 shift instructions only use the lower 5 bits.
        mov     edx,eax
        xor     eax,eax
        shl     edx,cl
        ret
JIT_LLsh ENDP


;*********************************************************************/
;LRsh - long shift right
;
;Purpose:
;   Does a signed Long Shift Right
;   Shifts a long right any number of bits.
;
;       NOTE:  This routine has been adapted from the Microsoft CRTs.
;
;Entry:
;   EDX:EAX - long value to be shifted
;       ECX - number of bits to shift by
;
;Exit:
;   EDX:EAX - shifted value
;
        ALIGN 16
PUBLIC JIT_LRsh
JIT_LRsh PROC
; Reduce shift amount mod 64
        and     ecx, 63
; Handle shifts of between bits 0 and 31
        cmp     ecx, 32
        jae     short LRshMORE32
        shrd    eax,edx,cl
        sar     edx,cl
        ret
; Handle shifts of between bits 32 and 63
LRshMORE32:
        ; The x86 shift instructions only use the lower 5 bits.
        mov     eax,edx
        sar     edx, 31
        sar     eax,cl
        ret
JIT_LRsh ENDP


;*********************************************************************/
; LRsz:
;Purpose:
;   Does a unsigned Long Shift Right
;   Shifts a long right any number of bits.
;
;       NOTE:  This routine has been adapted from the Microsoft CRTs.
;
;Entry:
;   EDX:EAX - long value to be shifted
;       ECX - number of bits to shift by
;
;Exit:
;   EDX:EAX - shifted value
;
        ALIGN 16
PUBLIC JIT_LRsz
JIT_LRsz PROC
; Reduce shift amount mod 64
        and     ecx, 63
; Handle shifts of between bits 0 and 31
        cmp     ecx, 32
        jae     short LRszMORE32
        shrd    eax,edx,cl
        shr     edx,cl
        ret
; Handle shifts of between bits 32 and 63
LRszMORE32:
        ; The x86 shift instructions only use the lower 5 bits.
        mov     eax,edx
        xor     edx,edx
        shr     eax,cl
        ret
JIT_LRsz ENDP

;*********************************************************************/
; LMul:
;Purpose:
;   Does a long multiply (same for signed/unsigned)
;
;       NOTE:  This routine has been adapted from the Microsoft CRTs.
;
;Entry:
;   Parameters are passed on the stack:
;               1st pushed: multiplier (QWORD)
;               2nd pushed: multiplicand (QWORD)
;
;Exit:
;   EDX:EAX - product of multiplier and multiplicand
;
        ALIGN 16
PUBLIC JIT_LMul
JIT_LMul PROC

;       AHI, BHI : upper 32 bits of A and B
;       ALO, BLO : lower 32 bits of A and B
;
;             ALO * BLO
;       ALO * BHI
; +     BLO * AHI
; ---------------------

        mov     eax,[esp + 8]   ; AHI
        mov     ecx,[esp + 16]  ; BHI
        or      ecx,eax         ;test for both hiwords zero.
        mov     ecx,[esp + 12]  ; BLO
        jnz     LMul_hard       ;both are zero, just mult ALO and BLO

        mov     eax,[esp + 4]
        mul     ecx

        ret     16              ; callee restores the stack

LMul_hard:
        push    ebx

        mul     ecx             ;eax has AHI, ecx has BLO, so AHI * BLO
        mov     ebx,eax         ;save result

        mov     eax,[esp + 8]   ; ALO
        mul     dword ptr [esp + 20] ;ALO * BHI
        add     ebx,eax         ;ebx = ((ALO * BHI) + (AHI * BLO))

        mov     eax,[esp + 8]   ; ALO   ;ecx = BLO
        mul     ecx             ;so edx:eax = ALO*BLO
        add     edx,ebx         ;now edx has all the LO*HI stuff

        pop     ebx

        ret     16              ; callee restores the stack

JIT_LMul ENDP

;*********************************************************************/
; JIT_Dbl2LngOvf

;Purpose:
;   converts a double to a long truncating toward zero (C semantics)
;   with check for overflow
;
;       uses stdcall calling conventions
;
PUBLIC JIT_Dbl2LngOvf
JIT_Dbl2LngOvf PROC
        fnclex
        fld     qword ptr [esp+4]
        push    ecx
        push    ecx
        fstp    qword ptr [esp]
        call    JIT_Dbl2Lng
        mov     ecx,eax
        fnstsw  ax
        test    ax,01h
        jnz     Dbl2LngOvf_throw
        mov     eax,ecx
        ret     8

Dbl2LngOvf_throw:
        mov     ECX, CORINFO_OverflowException_ASM
        call    JIT_InternalThrowFromHelper
        ret     8
JIT_Dbl2LngOvf ENDP

;*********************************************************************/
; JIT_Dbl2Lng

;Purpose:
;   converts a double to a long truncating toward zero (C semantics)
;
;       uses stdcall calling conventions
;
;   note that changing the rounding mode is very expensive.  This
;   routine basiclly does the truncation semantics without changing
;   the rounding mode, resulting in a win.
;
PUBLIC JIT_Dbl2Lng
JIT_Dbl2Lng PROC
        fld qword ptr[ESP+4]            ; fetch arg
        lea ecx,[esp-8]
        sub esp,16                      ; allocate frame
        and ecx,-8                      ; align pointer on boundary of 8
        fld st(0)                       ; duplciate top of stack
        fistp qword ptr[ecx]            ; leave arg on stack, also save in temp
        fild qword ptr[ecx]             ; arg, round(arg) now on stack
        mov edx,[ecx+4]                 ; high dword of integer
        mov eax,[ecx]                   ; low dword of integer
        test eax,eax
        je integer_QNaN_or_zero

arg_is_not_integer_QNaN:
        fsubp st(1),st                  ; TOS=d-round(d),
                                        ; { st(1)=st(1)-st & pop ST }
        test edx,edx                    ; what's sign of integer
        jns positive
                                        ; number is negative
                                        ; dead cycle
                                        ; dead cycle
        fstp dword ptr[ecx]             ; result of subtraction
        mov ecx,[ecx]                   ; dword of difference(single precision)
        add esp,16
        xor ecx,80000000h
        add ecx,7fffffffh               ; if difference>0 then increment integer
        adc eax,0                       ; inc eax (add CARRY flag)
        adc edx,0                       ; propagate carry flag to upper bits
        ret 8

positive:
        fstp dword ptr[ecx]             ;17-18 ; result of subtraction
        mov ecx,[ecx]                   ; dword of difference (single precision)
        add esp,16
        add ecx,7fffffffh               ; if difference<0 then decrement integer
        sbb eax,0                       ; dec eax (subtract CARRY flag)
        sbb edx,0                       ; propagate carry flag to upper bits
        ret 8

integer_QNaN_or_zero:
        test edx,7fffffffh
        jnz arg_is_not_integer_QNaN
        fstp st(0)                      ;; pop round(arg)
        fstp st(0)                      ;; arg
        add esp,16
        ret 8
JIT_Dbl2Lng ENDP

;*********************************************************************/
; JIT_Dbl2LngP4x87

;Purpose:
;   converts a double to a long truncating toward zero (C semantics)
;
;	uses stdcall calling conventions
;
;   This code is faster on a P4 than the Dbl2Lng code above, but is
;   slower on a PIII.  Hence we choose this code when on a P4 or above.
;
PUBLIC JIT_Dbl2LngP4x87
JIT_Dbl2LngP4x87 PROC
arg1	equ	<[esp+0Ch]>

    sub 	esp, 8                  ; get some local space

    fld	qword ptr arg1              ; fetch arg
    fnstcw  word ptr arg1           ; store FPCW
    movzx   eax, word ptr arg1      ; zero extend - wide
    or	ah, 0Ch                     ; turn on OE and DE flags
    mov	dword ptr [esp], eax        ; store new FPCW bits
    fldcw   word ptr  [esp]         ; reload FPCW with new bits
    fistp   qword ptr [esp]         ; convert
    mov	eax, dword ptr [esp]        ; reload FP result
    mov	edx, dword ptr [esp+4]      ;
    fldcw   word ptr arg1           ; reload original FPCW value

    add esp, 8                      ; restore stack

    ret	8
JIT_Dbl2LngP4x87 ENDP

;*********************************************************************/
; JIT_Dbl2LngSSE3

;Purpose:
;   converts a double to a long truncating toward zero (C semantics)
;
;	uses stdcall calling conventions
;
;   This code is faster than the above P4 x87 code for Intel processors
;   equal or later than Core2 and Atom that have SSE3 support
;
.686P
.XMM
PUBLIC JIT_Dbl2LngSSE3
JIT_Dbl2LngSSE3 PROC
arg1	equ	<[esp+0Ch]>

    sub esp, 8                      ; get some local space

    fld qword ptr arg1              ; fetch arg
    fisttp qword ptr [esp]          ; convert
    mov eax, dword ptr [esp]        ; reload FP result
    mov edx, dword ptr [esp+4]

    add esp, 8                      ; restore stack

    ret	8
JIT_Dbl2LngSSE3 ENDP
.586

;*********************************************************************/
; JIT_Dbl2IntSSE2

;Purpose:
;   converts a double to a long truncating toward zero (C semantics)
;
;	uses stdcall calling conventions
;
;   This code is even faster than the P4 x87 code for Dbl2LongP4x87,
;   but only returns a 32 bit value (only good for int).
;
.686P
.XMM
PUBLIC JIT_Dbl2IntSSE2
JIT_Dbl2IntSSE2 PROC
	$movsd	xmm0, [esp+4]
	cvttsd2si eax, xmm0
	ret 8
JIT_Dbl2IntSSE2 ENDP
.586


;*********************************************************************/
; This is the small write barrier thunk we use when we know the
; ephemeral generation is higher in memory than older generations.
; The 0x0F0F0F0F values are bashed by the two functions above.
; This the generic version - wherever the code says ECX,
; the specific register is patched later into a copy
; Note: do not replace ECX by EAX - there is a smaller encoding for
; the compares just for EAX, which won't work for other registers.
;
; READ THIS!!!!!!
; it is imperative that the addresses of the values that we overwrite
; (card table, ephemeral region ranges, etc) are naturally aligned since
; there are codepaths that will overwrite these values while the EE is running.
;
PUBLIC JIT_WriteBarrierReg_PreGrow
JIT_WriteBarrierReg_PreGrow PROC
        mov     DWORD PTR [edx], ecx
        cmp     ecx, 0F0F0F0F0h
        jb      NoWriteBarrierPre

        shr     edx, 10
        nop ; padding for alignment of constant
        cmp     byte ptr [edx+0F0F0F0F0h], 0FFh
        jne     WriteBarrierPre
NoWriteBarrierPre:
        ret
        nop ; padding for alignment of constant
        nop ; padding for alignment of constant
WriteBarrierPre:
        mov     byte ptr [edx+0F0F0F0F0h], 0FFh
        ret
JIT_WriteBarrierReg_PreGrow ENDP

;*********************************************************************/
; This is the larger write barrier thunk we use when we know that older
; generations may be higher in memory than the ephemeral generation
; The 0x0F0F0F0F values are bashed by the two functions above.
; This the generic version - wherever the code says ECX,
; the specific register is patched later into a copy
; Note: do not replace ECX by EAX - there is a smaller encoding for
; the compares just for EAX, which won't work for other registers.
; NOTE: we need this aligned for our validation to work properly
        ALIGN 4
PUBLIC JIT_WriteBarrierReg_PostGrow
JIT_WriteBarrierReg_PostGrow PROC
        mov     DWORD PTR [edx], ecx
        cmp     ecx, 0F0F0F0F0h
        jb      NoWriteBarrierPost
        cmp     ecx, 0F0F0F0F0h
        jae     NoWriteBarrierPost

        shr     edx, 10
        nop ; padding for alignment of constant
        cmp     byte ptr [edx+0F0F0F0F0h], 0FFh
        jne     WriteBarrierPost
NoWriteBarrierPost:
        ret
        nop ; padding for alignment of constant
        nop ; padding for alignment of constant
WriteBarrierPost:
        mov     byte ptr [edx+0F0F0F0F0h], 0FFh
        ret
JIT_WriteBarrierReg_PostGrow ENDP

;*********************************************************************/
;

        ; a fake virtual stub dispatch register indirect callsite
        $nop3
        call    dword ptr [eax]


PUBLIC JIT_TailCallReturnFromVSD
JIT_TailCallReturnFromVSD:
ifdef _DEBUG
        nop                         ; blessed callsite
endif
        call    VSDHelperLabel      ; keep call-ret count balanced.
VSDHelperLabel:

; Stack at this point :
;    ...
; m_ReturnAddress
; m_regs
; m_CallerAddress
; m_pThread
; vtbl
; GSCookie
; &VSDHelperLabel
OffsetOfTailCallFrame = 8

; ebx = pThread

ifdef _DEBUG
        mov     esi, _s_gsCookie        ; GetProcessGSCookie()
        cmp     dword ptr [esp+OffsetOfTailCallFrame-SIZEOF_GSCookie], esi
        je      TailCallFrameGSCookieIsValid
        call    @JIT_FailFast@0
    TailCallFrameGSCookieIsValid:
endif
        ; remove the padding frame from the chain
        mov     esi, dword ptr [esp+OffsetOfTailCallFrame+4]    ; esi = TailCallFrame::m_Next
        mov     dword ptr [ebx + Thread_m_pFrame], esi

        ; skip the frame
        add     esp, 20     ; &VSDHelperLabel, GSCookie, vtbl, m_Next, m_CallerAddress

        pop     edi         ; restore callee saved registers
        pop     esi
        pop     ebx
        pop     ebp

        ret                 ; return to m_ReturnAddress

;------------------------------------------------------------------------------
;

PUBLIC JIT_TailCall
JIT_TailCall PROC

; the stack layout at this point is:
;
;   ebp+8+4*nOldStackArgs   <- end of argument destination
;    ...                       ...
;   ebp+8+                     old args (size is nOldStackArgs)
;    ...                       ...
;   ebp+8                   <- start of argument destination
;   ebp+4                   ret addr
;   ebp+0                   saved ebp
;   ebp-c                   saved ebx, esi, edi (if have callee saved regs = 1)
;
;                           other stuff (local vars) in the jitted callers' frame
;
;   esp+20+4*nNewStackArgs  <- end of argument source
;    ...                       ...
;   esp+20+                    new args (size is nNewStackArgs) to be passed to the target of the tail-call
;    ...                       ...
;   esp+20                  <- start of argument source
;   esp+16                  nOldStackArgs
;   esp+12                  nNewStackArgs
;   esp+8                   flags (1 = have callee saved regs, 2 = virtual stub dispatch)
;   esp+4                   target addr
;   esp+0                   retaddr
;
;   If you change this function, make sure you update code:TailCallStubManager as well.

RetAddr         equ 0
TargetAddr      equ 4
nNewStackArgs   equ 12
nOldStackArgs   equ 16
NewArgs         equ 20

; extra space is incremented as we push things on the stack along the way
ExtraSpace      = 0

        push    0           ; Thread*

        ; save ArgumentRegisters
        push    ecx
        push    edx

        ; eax = GetThread(). Trashes edx
        INLINE_GETTHREAD eax, edx

        mov     [esp + 8], eax

ExtraSpace      = 12    ; pThread, ecx, edx

ifdef FEATURE_HIJACK
        ; Make sure that the EE does have the return address patched. So we can move it around.
        test    dword ptr [eax+Thread_m_State], TS_Hijacked_ASM
        jz      NoHijack

        ; JIT_TailCallHelper(Thread *)
        push    eax
        call    JIT_TailCallHelper  ; this is __stdcall

NoHijack:
endif

        mov     edx, dword ptr [esp+ExtraSpace+JIT_TailCall_StackOffsetToFlags]           ; edx = flags

        mov     eax, dword ptr [esp+ExtraSpace+nOldStackArgs]   ; eax = nOldStackArgs
        mov     ecx, dword ptr [esp+ExtraSpace+nNewStackArgs]   ; ecx = nNewStackArgs

        ; restore callee saved registers
        ; <TODO>@TODO : esp based - doesnt work with localloc</TODO>
        test    edx, 1
        jz      NoCalleeSaveRegisters

        mov     edi, dword ptr [ebp-4]              ; restore edi
        mov     esi, dword ptr [ebp-8]              ; restore esi
        mov     ebx, dword ptr [ebp-12]             ; restore ebx

NoCalleeSaveRegisters:

        push    dword ptr [ebp+4]                   ; save the original return address for later
        push    edi
        push    esi

ExtraSpace      = 24    ; pThread, ecx, edx, orig retaddr, edi, esi
CallersEsi      = 0
CallersEdi      = 4
OrigRetAddr     = 8
pThread         = 20

        lea     edi, [ebp+8+4*eax]                  ; edi = the end of argument destination
        lea     esi, [esp+ExtraSpace+NewArgs+4*ecx] ; esi = the end of argument source

        mov     ebp, dword ptr [ebp]        ; restore ebp (do not use ebp as scratch register to get a good stack trace in debugger)

        test    edx, 2
        jnz     VSDTailCall

        ; copy the arguments to the final destination
        test    ecx, ecx
        jz      ArgumentsCopied
ArgumentCopyLoop:
        ; At this point, this is the value of the registers :
        ; edi = end of argument dest
        ; esi = end of argument source
        ; ecx = nNewStackArgs
        mov     eax, dword ptr [esi-4]
        sub     edi, 4
        sub     esi, 4
        mov     dword ptr [edi], eax
        dec     ecx
        jnz     ArgumentCopyLoop
ArgumentsCopied:

        ; edi = the start of argument destination

        mov     eax, dword ptr [esp+4+4]                    ; return address
        mov     ecx, dword ptr [esp+ExtraSpace+TargetAddr]  ; target address

        mov     dword ptr [edi-4], eax      ; return address
        mov     dword ptr [edi-8], ecx      ; target address

        lea     eax, [edi-8]                ; new value for esp

        pop     esi
        pop     edi
        pop     ecx         ; skip original return address
        pop     edx
        pop     ecx

        mov     esp, eax

PUBLIC JIT_TailCallLeave    ; add a label here so that TailCallStubManager can access it
JIT_TailCallLeave:
        retn                ; Will branch to targetAddr.  This matches the
                            ; "call" done by JITted code, keeping the
                            ; call-ret count balanced.

        ;----------------------------------------------------------------------
VSDTailCall:
        ;----------------------------------------------------------------------

        ; For the Virtual Stub Dispatch, we create a fake callsite to fool
        ; the callsite probes. In order to create the call site, we need to insert TailCallFrame
        ; if we do not have one already.
        ;
        ; ecx = nNewStackArgs
        ; esi = the end of argument source
        ; edi = the end of argument destination
        ;
        ; The stub has pushed the following onto the stack at this point :
        ; pThread, ecx, edx, orig retaddr, edi, esi


        cmp     dword ptr [esp+OrigRetAddr], JIT_TailCallReturnFromVSD
        jz      VSDTailCallFrameInserted_DoSlideUpArgs ; There is an exiting TailCallFrame that can be reused

        ; try to allocate space for the frame / check whether there is enough space
        ; If there is sufficient space, we will setup the frame and then slide
        ; the arguments up the stack. Else, we first need to slide the arguments
        ; down the stack to make space for the TailCallFrame
        sub     edi, (SIZEOF_GSCookie + SIZEOF_TailCallFrame)
        cmp     edi, esi
        jae     VSDSpaceForFrameChecked

        ; There is not sufficient space to wedge in the TailCallFrame without
        ; overwriting the new arguments.
        ; We need to allocate the extra space on the stack,
        ; and slide down the new arguments

        mov     eax, esi
        sub     eax, edi
        sub     esp, eax

        mov     eax, ecx                        ; to subtract the size of arguments
        mov     edx, ecx                        ; for counter

        neg     eax

        ; copy down the arguments to the final destination, need to copy all temporary storage as well
        add     edx, (ExtraSpace+NewArgs)/4

        lea     esi, [esi+4*eax-(ExtraSpace+NewArgs)]
        lea     edi, [edi+4*eax-(ExtraSpace+NewArgs)]

VSDAllocFrameCopyLoop:
        mov     eax, dword ptr [esi]
        mov     dword ptr [edi], eax
        add     esi, 4
        add     edi, 4
        dec     edx
        jnz     VSDAllocFrameCopyLoop

        ; the argument source and destination are same now
        mov     esi, edi

VSDSpaceForFrameChecked:

        ; At this point, we have enough space on the stack for the TailCallFrame,
        ; and we may already have slided down the arguments

        mov     eax, _s_gsCookie                ; GetProcessGSCookie()
        mov     dword ptr [edi], eax            ; set GSCookie
        mov     eax, _g_TailCallFrameVptr       ; vptr
        mov     edx, dword ptr [esp+OrigRetAddr]        ; orig return address
        mov     dword ptr [edi+SIZEOF_GSCookie], eax            ; TailCallFrame::vptr
        mov     dword ptr [edi+SIZEOF_GSCookie+28], edx         ; TailCallFrame::m_ReturnAddress

        mov     eax, dword ptr [esp+CallersEdi]         ; restored edi
        mov     edx, dword ptr [esp+CallersEsi]         ; restored esi
        mov     dword ptr [edi+SIZEOF_GSCookie+12], eax         ; TailCallFrame::m_regs::edi
        mov     dword ptr [edi+SIZEOF_GSCookie+16], edx         ; TailCallFrame::m_regs::esi
        mov     dword ptr [edi+SIZEOF_GSCookie+20], ebx         ; TailCallFrame::m_regs::ebx
        mov     dword ptr [edi+SIZEOF_GSCookie+24], ebp         ; TailCallFrame::m_regs::ebp

        mov     ebx, dword ptr [esp+pThread]            ; ebx = pThread

        mov     eax, dword ptr [ebx+Thread_m_pFrame]
        lea     edx, [edi+SIZEOF_GSCookie]
        mov     dword ptr [edi+SIZEOF_GSCookie+4], eax          ; TailCallFrame::m_pNext
        mov     dword ptr [ebx+Thread_m_pFrame], edx    ; hook the new frame into the chain

        ; setup ebp chain
        lea     ebp, [edi+SIZEOF_GSCookie+24]                   ; TailCallFrame::m_regs::ebp

        ; Do not copy arguments again if they are in place already
        ; Otherwise, we will need to slide the new arguments up the stack
        cmp     esi, edi
        jne     VSDTailCallFrameInserted_DoSlideUpArgs

        ; At this point, we must have already previously slided down the new arguments,
        ; or the TailCallFrame is a perfect fit
        ; set the caller address
        mov     edx, dword ptr [esp+ExtraSpace+RetAddr] ; caller address
        mov     dword ptr [edi+SIZEOF_GSCookie+8], edx         ; TailCallFrame::m_CallerAddress

        ; adjust edi as it would by copying
        neg     ecx
        lea     edi, [edi+4*ecx]

        jmp     VSDArgumentsCopied

VSDTailCallFrameInserted_DoSlideUpArgs:
        ; set the caller address
        mov     edx, dword ptr [esp+ExtraSpace+RetAddr] ; caller address
        mov     dword ptr [edi+SIZEOF_GSCookie+8], edx          ; TailCallFrame::m_CallerAddress

        ; copy the arguments to the final destination
        test    ecx, ecx
        jz      VSDArgumentsCopied
VSDArgumentCopyLoop:
        mov     eax, dword ptr [esi-4]
        sub     edi, 4
        sub     esi, 4
        mov     dword ptr [edi], eax
        dec     ecx
        jnz     VSDArgumentCopyLoop
VSDArgumentsCopied:

        ; edi = the start of argument destination

        mov     ecx, dword ptr [esp+ExtraSpace+TargetAddr]   ; target address

        mov     dword ptr [edi-4], JIT_TailCallReturnFromVSD ; return address
        mov     dword ptr [edi-12], ecx     ; address of indirection cell
        mov     ecx, [ecx]
        mov     dword ptr [edi-8], ecx      ; target address

        ; skip original return address and saved esi, edi
        add     esp, 12

        pop     edx
        pop     ecx

        lea     esp, [edi-12]   ; new value for esp
        pop     eax

PUBLIC JIT_TailCallVSDLeave ; add a label here so that TailCallStubManager can access it
JIT_TailCallVSDLeave:
        retn                ; Will branch to targetAddr.  This matches the
                            ; "call" done by JITted code, keeping the
                            ; call-ret count balanced.

JIT_TailCall ENDP


;------------------------------------------------------------------------------

; HCIMPL2_VV(float, JIT_FltRem, float dividend, float divisor)
@JIT_FltRem@8 proc public
        fld  dword ptr [esp+4]          ; divisor
        fld  dword ptr [esp+8]          ; dividend
fremloop:
        fprem
        fstsw   ax
        fwait
        sahf
        jp      fremloop        ; Continue while the FPU status bit C2 is set
        fxch    ; swap, so divisor is on top and result is in st(1)
        fstp    ST(0)           ; Pop the divisor from the FP stack
        retn    8               ; Return value is in st(0)
@JIT_FltRem@8 endp

; HCIMPL2_VV(float, JIT_DblRem, float dividend, float divisor)
@JIT_DblRem@16 proc public
        fld  qword ptr [esp+4]          ; divisor
        fld  qword ptr [esp+12]         ; dividend
fremloopd:
        fprem
        fstsw   ax
        fwait
        sahf
        jp      fremloopd       ; Continue while the FPU status bit C2 is set
        fxch    ; swap, so divisor is on top and result is in st(1)
        fstp    ST(0)           ; Pop the divisor from the FP stack
        retn    16              ; Return value is in st(0)
@JIT_DblRem@16 endp

;------------------------------------------------------------------------------

; PatchedCodeStart and PatchedCodeEnd are used to determine bounds of patched code.
;

            ALIGN 4

_JIT_PatchedCodeStart@0 proc public
ret
_JIT_PatchedCodeStart@0 endp

            ALIGN 4

;**********************************************************************
; Write barriers generated at runtime

PUBLIC _JIT_PatchedWriteBarrierGroup@0
_JIT_PatchedWriteBarrierGroup@0 PROC
ret
_JIT_PatchedWriteBarrierGroup@0 ENDP

PatchedWriteBarrierHelper MACRO rg
        ALIGN 8
PUBLIC _JIT_WriteBarrier&rg&@0
_JIT_WriteBarrier&rg&@0 PROC
        ; Just allocate space that will be filled in at runtime
        db (48) DUP (0CCh)
_JIT_WriteBarrier&rg&@0 ENDP

ENDM

PatchedWriteBarrierHelper <EAX>
PatchedWriteBarrierHelper <EBX>
PatchedWriteBarrierHelper <ECX>
PatchedWriteBarrierHelper <ESI>
PatchedWriteBarrierHelper <EDI>
PatchedWriteBarrierHelper <EBP>

PUBLIC _JIT_PatchedWriteBarrierGroup_End@0
_JIT_PatchedWriteBarrierGroup_End@0 PROC
ret
_JIT_PatchedWriteBarrierGroup_End@0 ENDP

_JIT_PatchedCodeLast@0 proc public
ret
_JIT_PatchedCodeLast@0 endp

; This is the first function outside the "keep together range". Used by BBT scripts.
_JIT_PatchedCodeEnd@0 proc public
ret
_JIT_PatchedCodeEnd@0 endp


; Note that the debugger skips this entirely when doing SetIP,
; since COMPlusCheckForAbort should always return 0.  Excep.cpp:LeaveCatch
; asserts that to be true.  If this ends up doing more work, then the
; debugger may need additional support.
; void __stdcall JIT_EndCatch();
JIT_EndCatch PROC stdcall public

    ; make temp storage for return address, and push the address of that
    ; as the last arg to COMPlusEndCatch
    mov     ecx, [esp]
    push    ecx;
    push    esp;

    ; push the rest of COMPlusEndCatch's args, right-to-left
    push    esi
    push    edi
    push    ebx
    push    ebp

    call    _COMPlusEndCatch@20 ; returns old esp value in eax, stores jump address
    ; now eax = new esp, [esp] = new eip

    pop     edx         ; edx = new eip
    mov     esp, eax    ; esp = new esp
    jmp     edx         ; eip = new eip

JIT_EndCatch ENDP

; The following helper will access ("probe") a word on each page of the stack
; starting with the page right beneath esp down to the one pointed to by eax.
; The procedure is needed to make sure that the "guard" page is pushed down below the allocated stack frame.
; The call to the helper will be emitted by JIT in the function prolog when large (larger than 0x3000 bytes) stack frame is required.
;
; NOTE: this helper will modify a value of esp and must establish the frame pointer.
PROBE_PAGE_SIZE equ 1000h

_JIT_StackProbe@0 PROC public
    ; On entry:
    ;   eax - the lowest address of the stack frame being allocated (i.e. [InitialSp - FrameSize])
    ;
    ; NOTE: this helper will probe at least one page below the one pointed by esp.
    push    ebp
    mov     ebp, esp

    and     esp, -PROBE_PAGE_SIZE ; esp points to the **lowest address** on the last probed page
                                  ; This is done to make the loop end condition simpler.
ProbeLoop:
    test    [esp - 4], eax        ; esp points to the lowest address on the **last probed** page
    sub     esp, PROBE_PAGE_SIZE  ; esp points to the lowest address of the **next page** to probe
    cmp     esp, eax
    jg      ProbeLoop             ; if esp > eax, then we need to probe at least one more page.

    mov     esp, ebp
    pop     ebp
    ret

_JIT_StackProbe@0 ENDP

PUBLIC _JIT_StackProbe_End@0
_JIT_StackProbe_End@0 PROC
    ret
_JIT_StackProbe_End@0 ENDP

    end
