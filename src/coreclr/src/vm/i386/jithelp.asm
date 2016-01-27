; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
; 

; 
; ==--==
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
EXTERN @JITutil_IsInstanceOfInterface@8:PROC
EXTERN @JITutil_ChkCastInterface@8:PROC
EXTERN @JITutil_IsInstanceOfAny@8:PROC
EXTERN @JITutil_ChkCastAny@8:PROC
ifdef FEATURE_IMPLICIT_TLS
EXTERN _GetThread@0:PROC
endif

ifdef WRITE_BARRIER_CHECK 
; Those global variables are always defined, but should be 0 for Server GC
g_GCShadow                      TEXTEQU <?g_GCShadow@@3PAEA>
g_GCShadowEnd                   TEXTEQU <?g_GCShadowEnd@@3PAEA>
EXTERN  g_GCShadow:DWORD
EXTERN  g_GCShadowEnd:DWORD
INVALIDGCVALUE equ 0CCCCCCCDh
endif

ifdef FEATURE_REMOTING
EXTERN _TransparentProxyStub_CrossContext@0:PROC
EXTERN _InContextTPQuickDispatchAsmStub@0:PROC
endif

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

; The code here is tightly coupled with AdjustContextForWriteBarrier, if you change
; anything here, you might need to change AdjustContextForWriteBarrier as well
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
        ; status. This makes job of AdjustContextForWriteBarrier easier.
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
        ja      WriteBarrier_NoShadow_&rg

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

; The code here is tightly coupled with AdjustContextForWriteBarrier, if you change
; anything here, you might need to change AdjustContextForWriteBarrier as well

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
        ; status. This makes job of AdjustContextForWriteBarrier easier.
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
        ja      ByRefWriteBarrier_NoShadow

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
UniversalWriteBarrierHelper MACRO name
        ALIGN 4
PUBLIC @JIT_&name&@8
@JIT_&name&@8 PROC
        mov eax,edx
        mov edx,ecx
        jmp _JIT_&name&EAX@0
@JIT_&name&@8 ENDP
ENDM

; WriteBarrierStart and WriteBarrierEnd are used to determine bounds of
; WriteBarrier functions so can determine if got AV in them. 
; 
PUBLIC _JIT_WriteBarrierStart@0
_JIT_WriteBarrierStart@0 PROC
ret
_JIT_WriteBarrierStart@0 ENDP

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

PUBLIC _JIT_WriteBarrierLast@0
_JIT_WriteBarrierLast@0 PROC
ret
_JIT_WriteBarrierLast@0 ENDP

; This is the first function outside the "keep together range". Used by BBT scripts.
PUBLIC _JIT_WriteBarrierEnd@0
_JIT_WriteBarrierEnd@0 PROC
ret
_JIT_WriteBarrierEnd@0 ENDP

;*********************************************************************/
; In cases where we support it we have an optimized GC Poll callback.  Normall (when we're not trying to
; suspend for GC, the CORINFO_HELP_POLL_GC helper points to this nop routine.  When we're ready to suspend
; for GC, we whack the Jit Helper table entry to point to the real helper.  When we're done with GC we
; whack it back.
PUBLIC @JIT_PollGC_Nop@0
@JIT_PollGC_Nop@0 PROC
ret
@JIT_PollGC_Nop@0 ENDP

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
;   routine basiclly does the truncation sematics without changing
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
; it is imperative that the addresses of of the values that we overwrite
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

        call    _GetThread@0; eax = Thread*
        push    eax         ; Thread*

        ; save ArgumentRegisters
        push    ecx
        push    edx

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

g_SystemInfo            TEXTEQU <?g_SystemInfo@@3U_SYSTEM_INFO@@A>
g_SpinConstants         TEXTEQU <?g_SpinConstants@@3USpinConstants@@A>
g_pSyncTable            TEXTEQU <?g_pSyncTable@@3PAVSyncTableEntry@@A>
JITutil_MonEnterWorker  TEXTEQU <@JITutil_MonEnterWorker@4>
JITutil_MonReliableEnter TEXTEQU <@JITutil_MonReliableEnter@8>
JITutil_MonTryEnter     TEXTEQU <@JITutil_MonTryEnter@12>
JITutil_MonExitWorker   TEXTEQU <@JITutil_MonExitWorker@4>
JITutil_MonContention   TEXTEQU <@JITutil_MonContention@4>       
JITutil_MonReliableContention   TEXTEQU <@JITutil_MonReliableContention@8>       
JITutil_MonSignal       TEXTEQU <@JITutil_MonSignal@4>
JIT_InternalThrow       TEXTEQU <@JIT_InternalThrow@4>
EXTRN	g_SystemInfo:BYTE
EXTRN	g_SpinConstants:BYTE
EXTRN	g_pSyncTable:DWORD
EXTRN	JITutil_MonEnterWorker:PROC
EXTRN	JITutil_MonReliableEnter:PROC
EXTRN	JITutil_MonTryEnter:PROC
EXTRN	JITutil_MonExitWorker:PROC
EXTRN	JITutil_MonContention:PROC
EXTRN	JITutil_MonReliableContention:PROC
EXTRN	JITutil_MonSignal:PROC
EXTRN	JIT_InternalThrow:PROC

ifdef MON_DEBUG
ifdef TRACK_SYNC
EnterSyncHelper TEXTEQU <_EnterSyncHelper@8>
LeaveSyncHelper TEXTEQU <_LeaveSyncHelper@8>          
EXTRN	EnterSyncHelper:PROC
EXTRN	LeaveSyncHelper:PROC
endif ;TRACK_SYNC
endif ;MON_DEBUG

; The following macro is needed because MASM returns
; "instruction prefix not allowed" error message for
; rep nop mnemonic
$repnop MACRO
    db 0F3h
    db 090h
ENDM

; Safe ThreadAbort does not abort a thread if it is running finally or has lock counts.
; At the time we call Monitor.Enter, we initiate the abort if we can.
; We do not need to do the same for Monitor.Leave, since most of time, Monitor.Leave is called
; during finally.

;**********************************************************************
; This is a frameless helper for entering a monitor on a object.
; The object is in ARGUMENT_REG1.  This tries the normal case (no
; blocking or object allocation) in line and calls a framed helper
; for the other cases.
; ***** NOTE: if you make any changes to this routine, build with MON_DEBUG undefined
; to make sure you don't break the non-debug build. This is very fragile code.
; Also, propagate the changes to jithelp.s which contains the same helper and assembly code
; (in AT&T syntax) for gnu assembler.
@JIT_MonEnterWorker@4 proc public
        ; Initialize delay value for retry with exponential backoff
        push    ebx
        mov     ebx, dword ptr g_SpinConstants+SpinConstants_dwInitialDuration

        ; We need yet another register to avoid refetching the thread object
        push    esi
        
        ; Check if the instance is NULL.
        test    ARGUMENT_REG1, ARGUMENT_REG1
        jz      MonEnterFramedLockHelper

        call    _GetThread@0
        mov     esi,eax
        
        ; Check if we can abort here
        mov     eax, [esi+Thread_m_State]
        and     eax, TS_CatchAtSafePoint_ASM
        jz      MonEnterRetryThinLock
        ; go through the slow code path to initiate ThreadAbort.
        jmp     MonEnterFramedLockHelper

MonEnterRetryThinLock: 
        ; Fetch the object header dword
        mov     eax, [ARGUMENT_REG1-SyncBlockIndexOffset_ASM]

        ; Check whether we have the "thin lock" layout, the lock is free and the spin lock bit not set
        ; SBLK_COMBINED_MASK_ASM = BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX + BIT_SBLK_SPIN_LOCK + SBLK_MASK_LOCK_THREADID + SBLK_MASK_LOCK_RECLEVEL
        test    eax, SBLK_COMBINED_MASK_ASM
        jnz     MonEnterNeedMoreTests

        ; Everything is fine - get the thread id to store in the lock
        mov     edx, [esi+Thread_m_ThreadId]

        ; If the thread id is too large, we need a syncblock for sure
        cmp     edx, SBLK_MASK_LOCK_THREADID_ASM
        ja      MonEnterFramedLockHelper

        ; We want to store a new value with the current thread id set in the low 10 bits
        or      edx,eax
        lock cmpxchg dword ptr [ARGUMENT_REG1-SyncBlockIndexOffset_ASM], edx
        jnz     MonEnterPrepareToWaitThinLock

        ; Everything went fine and we're done
        add     [esi+Thread_m_dwLockCount],1
        pop     esi
        pop     ebx
        ret

MonEnterNeedMoreTests: 
        ; Ok, it's not the simple case - find out which case it is
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX_ASM
        jnz     MonEnterHaveHashOrSyncBlockIndex

        ; The header is transitioning or the lock - treat this as if the lock was taken
        test    eax, BIT_SBLK_SPIN_LOCK_ASM
        jnz     MonEnterPrepareToWaitThinLock

        ; Here we know we have the "thin lock" layout, but the lock is not free.
        ; It could still be the recursion case - compare the thread id to check
        mov     edx,eax
        and     edx, SBLK_MASK_LOCK_THREADID_ASM
        cmp     edx, [esi+Thread_m_ThreadId]
        jne     MonEnterPrepareToWaitThinLock

        ; Ok, the thread id matches, it's the recursion case.
        ; Bump up the recursion level and check for overflow
        lea     edx, [eax+SBLK_LOCK_RECLEVEL_INC_ASM]
        test    edx, SBLK_MASK_LOCK_RECLEVEL_ASM
        jz      MonEnterFramedLockHelper

        ; Try to put the new recursion level back. If the header was changed in the meantime,
        ; we need a full retry, because the layout could have changed.
        lock cmpxchg [ARGUMENT_REG1-SyncBlockIndexOffset_ASM], edx
        jnz     MonEnterRetryHelperThinLock

        ; Everything went fine and we're done
        pop     esi
        pop     ebx
        ret

MonEnterPrepareToWaitThinLock: 
        ; If we are on an MP system, we try spinning for a certain number of iterations
        cmp     dword ptr g_SystemInfo+SYSTEM_INFO_dwNumberOfProcessors,1
        jle     MonEnterFramedLockHelper

        ; exponential backoff: delay by approximately 2*ebx clock cycles (on a PIII)
        mov     eax, ebx
MonEnterdelayLoopThinLock:
        $repnop ; indicate to the CPU that we are spin waiting (useful for some Intel P4 multiprocs)
        dec     eax
        jnz     MonEnterdelayLoopThinLock

        ; next time, wait a factor longer
        imul    ebx, dword ptr g_SpinConstants+SpinConstants_dwBackoffFactor

        cmp     ebx, dword ptr g_SpinConstants+SpinConstants_dwMaximumDuration
        jle     MonEnterRetryHelperThinLock

        jmp     MonEnterFramedLockHelper

MonEnterRetryHelperThinLock: 
        jmp     MonEnterRetryThinLock

MonEnterHaveHashOrSyncBlockIndex: 
        ; If we have a hash code already, we need to create a sync block
        test    eax, BIT_SBLK_IS_HASHCODE_ASM
        jnz     MonEnterFramedLockHelper

        ; Ok, we have a sync block index - just and out the top bits and grab the syncblock index
        and     eax, MASK_SYNCBLOCKINDEX_ASM

        ; Get the sync block pointer.
        mov     ARGUMENT_REG2, dword ptr g_pSyncTable
        mov     ARGUMENT_REG2, [ARGUMENT_REG2+eax*SizeOfSyncTableEntry_ASM+SyncTableEntry_m_SyncBlock]

        ; Check if the sync block has been allocated.
        test    ARGUMENT_REG2, ARGUMENT_REG2
        jz      MonEnterFramedLockHelper

        ; Get a pointer to the lock object.
        lea     ARGUMENT_REG2, [ARGUMENT_REG2+SyncBlock_m_Monitor]

        ; Attempt to acquire the lock.
MonEnterRetrySyncBlock: 
        mov     eax, [ARGUMENT_REG2+AwareLock_m_MonitorHeld]
        test    eax,eax
        jne     MonEnterHaveWaiters

        ; Common case, lock isn't held and there are no waiters. Attempt to
        ; gain ownership ourselves.
        mov     ARGUMENT_REG1,1
        lock cmpxchg [ARGUMENT_REG2+AwareLock_m_MonitorHeld], ARGUMENT_REG1
        jnz     MonEnterRetryHelperSyncBlock

        ; Success. Save the thread object in the lock and increment the use count.
        mov     dword ptr [ARGUMENT_REG2+AwareLock_m_HoldingThread],esi
        inc     dword ptr [esi+Thread_m_dwLockCount]
        inc     dword ptr [ARGUMENT_REG2+AwareLock_m_Recursion]

ifdef MON_DEBUG
ifdef TRACK_SYNC
        push    ARGUMENT_REG2 ; AwareLock
        push    [esp+4]   ; return address
        call    EnterSyncHelper
endif ;TRACK_SYNC
endif ;MON_DEBUG
        pop     esi
        pop     ebx
        ret

        ; It's possible to get here with waiters but no lock held, but in this
        ; case a signal is about to be fired which will wake up a waiter. So
        ; for fairness sake we should wait too.
        ; Check first for recursive lock attempts on the same thread.
MonEnterHaveWaiters: 
        ; Is mutex already owned by current thread?
        cmp     [ARGUMENT_REG2+AwareLock_m_HoldingThread],esi
        jne     MonEnterPrepareToWait

        ; Yes, bump our use count.
        inc     dword ptr [ARGUMENT_REG2+AwareLock_m_Recursion]
ifdef MON_DEBUG
ifdef TRACK_SYNC
        push    ARGUMENT_REG2 ; AwareLock
        push    [esp+4]   ; return address
        call    EnterSyncHelper
endif ;TRACK_SYNC        
endif ;MON_DEBUG
        pop     esi
        pop     ebx
        ret

MonEnterPrepareToWait: 
        ; If we are on an MP system, we try spinning for a certain number of iterations
        cmp     dword ptr g_SystemInfo+SYSTEM_INFO_dwNumberOfProcessors,1
        jle     MonEnterHaveWaiters1

        ; exponential backoff: delay by approximately 2*ebx clock cycles (on a PIII)
        mov     eax,ebx
MonEnterdelayLoop:
        $repnop ; indicate to the CPU that we are spin waiting (useful for some Intel P4 multiprocs)
        dec     eax
        jnz     MonEnterdelayLoop

        ; next time, wait a factor longer
        imul    ebx, dword ptr g_SpinConstants+SpinConstants_dwBackoffFactor

        cmp     ebx, dword ptr g_SpinConstants+SpinConstants_dwMaximumDuration
        jle     MonEnterRetrySyncBlock

MonEnterHaveWaiters1: 

        pop     esi
        pop     ebx

        ; Place AwareLock in arg1 then call contention helper.
        mov     ARGUMENT_REG1, ARGUMENT_REG2
        jmp     JITutil_MonContention

MonEnterRetryHelperSyncBlock: 
        jmp     MonEnterRetrySyncBlock

        ; ECX has the object to synchronize on
MonEnterFramedLockHelper: 
        pop     esi
        pop     ebx
        jmp     JITutil_MonEnterWorker

@JIT_MonEnterWorker@4 endp

;**********************************************************************
; This is a frameless helper for entering a monitor on a object, and
; setting a flag to indicate that the lock was taken.
; The object is in ARGUMENT_REG1.  The flag is in ARGUMENT_REG2.
; This tries the normal case (no blocking or object allocation) in line 
; and calls a framed helper for the other cases.
; ***** NOTE: if you make any changes to this routine, build with MON_DEBUG undefined
; to make sure you don't break the non-debug build. This is very fragile code.
; Also, propagate the changes to jithelp.s which contains the same helper and assembly code
; (in AT&T syntax) for gnu assembler.
@JIT_MonReliableEnter@8 proc public
        ; Initialize delay value for retry with exponential backoff
        push    ebx
        mov     ebx, dword ptr g_SpinConstants+SpinConstants_dwInitialDuration
        
        ; Put pbLockTaken in edi
        push	edi
        mov		edi, ARGUMENT_REG2

        ; We need yet another register to avoid refetching the thread object
        push    esi
        
        ; Check if the instance is NULL.
        test    ARGUMENT_REG1, ARGUMENT_REG1
        jz      MonReliableEnterFramedLockHelper

        call    _GetThread@0
        mov     esi,eax
        
        ; Check if we can abort here
        mov     eax, [esi+Thread_m_State]
        and     eax, TS_CatchAtSafePoint_ASM
        jz      MonReliableEnterRetryThinLock
        ; go through the slow code path to initiate ThreadAbort.
        jmp     MonReliableEnterFramedLockHelper

MonReliableEnterRetryThinLock: 
        ; Fetch the object header dword
        mov     eax, [ARGUMENT_REG1-SyncBlockIndexOffset_ASM]

        ; Check whether we have the "thin lock" layout, the lock is free and the spin lock bit not set
        ; SBLK_COMBINED_MASK_ASM = BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX + BIT_SBLK_SPIN_LOCK + SBLK_MASK_LOCK_THREADID + SBLK_MASK_LOCK_RECLEVEL
        test    eax, SBLK_COMBINED_MASK_ASM
        jnz     MonReliableEnterNeedMoreTests

        ; Everything is fine - get the thread id to store in the lock
        mov     edx, [esi+Thread_m_ThreadId]

        ; If the thread id is too large, we need a syncblock for sure
        cmp     edx, SBLK_MASK_LOCK_THREADID_ASM
        ja      MonReliableEnterFramedLockHelper

        ; We want to store a new value with the current thread id set in the low 10 bits
        or      edx,eax
        lock cmpxchg dword ptr [ARGUMENT_REG1-SyncBlockIndexOffset_ASM], edx
        jnz     MonReliableEnterPrepareToWaitThinLock

        ; Everything went fine and we're done
        add     [esi+Thread_m_dwLockCount],1
        ; Set *pbLockTaken=true
        mov		byte ptr [edi],1
        pop     esi
        pop		edi
        pop     ebx
        ret

MonReliableEnterNeedMoreTests: 
        ; Ok, it's not the simple case - find out which case it is
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX_ASM
        jnz     MonReliableEnterHaveHashOrSyncBlockIndex

        ; The header is transitioning or the lock - treat this as if the lock was taken
        test    eax, BIT_SBLK_SPIN_LOCK_ASM
        jnz     MonReliableEnterPrepareToWaitThinLock

        ; Here we know we have the "thin lock" layout, but the lock is not free.
        ; It could still be the recursion case - compare the thread id to check
        mov     edx,eax
        and     edx, SBLK_MASK_LOCK_THREADID_ASM
        cmp     edx, [esi+Thread_m_ThreadId]
        jne     MonReliableEnterPrepareToWaitThinLock

        ; Ok, the thread id matches, it's the recursion case.
        ; Bump up the recursion level and check for overflow
        lea     edx, [eax+SBLK_LOCK_RECLEVEL_INC_ASM]
        test    edx, SBLK_MASK_LOCK_RECLEVEL_ASM
        jz      MonReliableEnterFramedLockHelper

        ; Try to put the new recursion level back. If the header was changed in the meantime,
        ; we need a full retry, because the layout could have changed.
        lock cmpxchg [ARGUMENT_REG1-SyncBlockIndexOffset_ASM], edx
        jnz     MonReliableEnterRetryHelperThinLock

        ; Everything went fine and we're done
        ; Set *pbLockTaken=true
        mov		byte ptr [edi],1
        pop     esi
        pop		edi
        pop     ebx
        ret

MonReliableEnterPrepareToWaitThinLock: 
        ; If we are on an MP system, we try spinning for a certain number of iterations
        cmp     dword ptr g_SystemInfo+SYSTEM_INFO_dwNumberOfProcessors,1
        jle     MonReliableEnterFramedLockHelper

        ; exponential backoff: delay by approximately 2*ebx clock cycles (on a PIII)
        mov     eax, ebx
MonReliableEnterdelayLoopThinLock:
        $repnop ; indicate to the CPU that we are spin waiting (useful for some Intel P4 multiprocs)
        dec     eax
        jnz     MonReliableEnterdelayLoopThinLock

        ; next time, wait a factor longer
        imul    ebx, dword ptr g_SpinConstants+SpinConstants_dwBackoffFactor

        cmp     ebx, dword ptr g_SpinConstants+SpinConstants_dwMaximumDuration
        jle     MonReliableEnterRetryHelperThinLock

        jmp     MonReliableEnterFramedLockHelper

MonReliableEnterRetryHelperThinLock: 
        jmp     MonReliableEnterRetryThinLock

MonReliableEnterHaveHashOrSyncBlockIndex: 
        ; If we have a hash code already, we need to create a sync block
        test    eax, BIT_SBLK_IS_HASHCODE_ASM
        jnz     MonReliableEnterFramedLockHelper

        ; Ok, we have a sync block index - just and out the top bits and grab the syncblock index
        and     eax, MASK_SYNCBLOCKINDEX_ASM

        ; Get the sync block pointer.
        mov     ARGUMENT_REG2, dword ptr g_pSyncTable
        mov     ARGUMENT_REG2, [ARGUMENT_REG2+eax*SizeOfSyncTableEntry_ASM+SyncTableEntry_m_SyncBlock]

        ; Check if the sync block has been allocated.
        test    ARGUMENT_REG2, ARGUMENT_REG2
        jz      MonReliableEnterFramedLockHelper

        ; Get a pointer to the lock object.
        lea     ARGUMENT_REG2, [ARGUMENT_REG2+SyncBlock_m_Monitor]

        ; Attempt to acquire the lock.
MonReliableEnterRetrySyncBlock: 
        mov     eax, [ARGUMENT_REG2+AwareLock_m_MonitorHeld]
        test    eax,eax
        jne     MonReliableEnterHaveWaiters

        ; Common case, lock isn't held and there are no waiters. Attempt to
        ; gain ownership ourselves.
        mov     ARGUMENT_REG1,1
        lock cmpxchg [ARGUMENT_REG2+AwareLock_m_MonitorHeld], ARGUMENT_REG1
        jnz     MonReliableEnterRetryHelperSyncBlock

        ; Success. Save the thread object in the lock and increment the use count.
        mov     dword ptr [ARGUMENT_REG2+AwareLock_m_HoldingThread],esi
        inc     dword ptr [esi+Thread_m_dwLockCount]
        inc     dword ptr [ARGUMENT_REG2+AwareLock_m_Recursion]
        ; Set *pbLockTaken=true
        mov		byte ptr [edi],1

ifdef MON_DEBUG
ifdef TRACK_SYNC
        push    ARGUMENT_REG2 ; AwareLock
        push    [esp+4]   ; return address
        call    EnterSyncHelper
endif ;TRACK_SYNC
endif ;MON_DEBUG
        pop     esi
        pop		edi
        pop     ebx
        ret

        ; It's possible to get here with waiters but no lock held, but in this
        ; case a signal is about to be fired which will wake up a waiter. So
        ; for fairness sake we should wait too.
        ; Check first for recursive lock attempts on the same thread.
MonReliableEnterHaveWaiters: 
        ; Is mutex already owned by current thread?
        cmp     [ARGUMENT_REG2+AwareLock_m_HoldingThread],esi
        jne     MonReliableEnterPrepareToWait

        ; Yes, bump our use count.
        inc     dword ptr [ARGUMENT_REG2+AwareLock_m_Recursion]
        ; Set *pbLockTaken=true
        mov		byte ptr [edi],1
ifdef MON_DEBUG
ifdef TRACK_SYNC
        push    ARGUMENT_REG2 ; AwareLock
        push    [esp+4]   ; return address
        call    EnterSyncHelper
endif ;TRACK_SYNC        
endif ;MON_DEBUG
        pop     esi
        pop		edi
        pop     ebx
        ret

MonReliableEnterPrepareToWait: 
        ; If we are on an MP system, we try spinning for a certain number of iterations
        cmp     dword ptr g_SystemInfo+SYSTEM_INFO_dwNumberOfProcessors,1
        jle     MonReliableEnterHaveWaiters1

        ; exponential backoff: delay by approximately 2*ebx clock cycles (on a PIII)
        mov     eax,ebx
MonReliableEnterdelayLoop:
        $repnop ; indicate to the CPU that we are spin waiting (useful for some Intel P4 multiprocs)
        dec     eax
        jnz     MonReliableEnterdelayLoop

        ; next time, wait a factor longer
        imul    ebx, dword ptr g_SpinConstants+SpinConstants_dwBackoffFactor

        cmp     ebx, dword ptr g_SpinConstants+SpinConstants_dwMaximumDuration
        jle     MonReliableEnterRetrySyncBlock

MonReliableEnterHaveWaiters1: 

        ; Place AwareLock in arg1, pbLockTaken in arg2, then call contention helper.
        mov     ARGUMENT_REG1, ARGUMENT_REG2
        mov		ARGUMENT_REG2, edi

        pop     esi
        pop		edi
        pop     ebx

        jmp     JITutil_MonReliableContention

MonReliableEnterRetryHelperSyncBlock: 
        jmp     MonReliableEnterRetrySyncBlock

        ; ECX has the object to synchronize on
MonReliableEnterFramedLockHelper: 
	    mov		ARGUMENT_REG2, edi
        pop     esi
        pop		edi
        pop     ebx
        jmp     JITutil_MonReliableEnter

@JIT_MonReliableEnter@8 endp

;************************************************************************
; This is a frameless helper for trying to enter a monitor on a object.
; The object is in ARGUMENT_REG1 and a timeout in ARGUMENT_REG2. This tries the
; normal case (no object allocation) in line and calls a framed helper for the
; other cases.
; ***** NOTE: if you make any changes to this routine, build with MON_DEBUG undefined
; to make sure you don't break the non-debug build. This is very fragile code.
; Also, propagate the changes to jithelp.s which contains the same helper and assembly code
; (in AT&T syntax) for gnu assembler.
@JIT_MonTryEnter@12 proc public
        ; Save the timeout parameter.
        push    ARGUMENT_REG2

        ; Initialize delay value for retry with exponential backoff
        push    ebx
        mov     ebx, dword ptr g_SpinConstants+SpinConstants_dwInitialDuration

        ; The thin lock logic needs another register to store the thread
        push    esi
        
        ; Check if the instance is NULL.
        test    ARGUMENT_REG1, ARGUMENT_REG1
        jz      MonTryEnterFramedLockHelper

        ; Check if the timeout looks valid
        cmp     ARGUMENT_REG2,-1
        jl      MonTryEnterFramedLockHelper

        ; Get the thread right away, we'll need it in any case
        call    _GetThread@0
        mov     esi,eax

        ; Check if we can abort here
        mov     eax, [esi+Thread_m_State]
        and     eax, TS_CatchAtSafePoint_ASM
        jz      MonTryEnterRetryThinLock
        ; go through the slow code path to initiate ThreadAbort.
        jmp     MonTryEnterFramedLockHelper

MonTryEnterRetryThinLock: 
        ; Get the header dword and check its layout
        mov     eax, [ARGUMENT_REG1-SyncBlockIndexOffset_ASM]

        ; Check whether we have the "thin lock" layout, the lock is free and the spin lock bit not set
        ; SBLK_COMBINED_MASK_ASM = BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX + BIT_SBLK_SPIN_LOCK + SBLK_MASK_LOCK_THREADID + SBLK_MASK_LOCK_RECLEVEL
        test    eax, SBLK_COMBINED_MASK_ASM
        jnz     MonTryEnterNeedMoreTests

        ; Ok, everything is fine. Fetch the thread id and make sure it's small enough for thin locks
        mov     edx, [esi+Thread_m_ThreadId]
        cmp     edx, SBLK_MASK_LOCK_THREADID_ASM
        ja      MonTryEnterFramedLockHelper

        ; Try to put our thread id in there
        or      edx,eax
        lock cmpxchg [ARGUMENT_REG1-SyncBlockIndexOffset_ASM],edx
        jnz     MonTryEnterRetryHelperThinLock

        ; Got the lock - everything is fine"
        add     [esi+Thread_m_dwLockCount],1
        pop     esi

        ; Delay value no longer needed
        pop     ebx

        ; Timeout parameter not needed, ditch it from the stack.
        add     esp,4

		mov		eax, [esp+4]
        mov     byte ptr [eax], 1
        ret		4

MonTryEnterNeedMoreTests: 
        ; Ok, it's not the simple case - find out which case it is
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX_ASM
        jnz     MonTryEnterHaveSyncBlockIndexOrHash

        ; The header is transitioning or the lock is taken
        test    eax, BIT_SBLK_SPIN_LOCK_ASM
        jnz     MonTryEnterRetryHelperThinLock

        mov     edx, eax
        and     edx, SBLK_MASK_LOCK_THREADID_ASM
        cmp     edx, [esi+Thread_m_ThreadId]
        jne     MonTryEnterPrepareToWaitThinLock

        ; Ok, the thread id matches, it's the recursion case.
        ; Bump up the recursion level and check for overflow
        lea     edx, [eax+SBLK_LOCK_RECLEVEL_INC_ASM]
        test    edx, SBLK_MASK_LOCK_RECLEVEL_ASM
        jz      MonTryEnterFramedLockHelper

        ; Try to put the new recursion level back. If the header was changed in the meantime,
        ; we need a full retry, because the layout could have changed.
        lock cmpxchg [ARGUMENT_REG1-SyncBlockIndexOffset_ASM],edx
        jnz     MonTryEnterRetryHelperThinLock

        ; Everything went fine and we're done
        pop     esi
        pop     ebx

        ; Timeout parameter not needed, ditch it from the stack.
        add     esp, 4
		mov		eax, [esp+4]
        mov     byte ptr [eax], 1
        ret		4

MonTryEnterPrepareToWaitThinLock:
        ; If we are on an MP system, we try spinning for a certain number of iterations
        cmp     dword ptr g_SystemInfo+SYSTEM_INFO_dwNumberOfProcessors,1
        jle     MonTryEnterFramedLockHelper

        ; exponential backoff: delay by approximately 2*ebx clock cycles (on a PIII)
        mov     eax, ebx
MonTryEnterdelayLoopThinLock:
        $repnop ; indicate to the CPU that we are spin waiting (useful for some Intel P4 multiprocs)
        dec     eax
        jnz     MonTryEnterdelayLoopThinLock

        ; next time, wait a factor longer
        imul    ebx, dword ptr g_SpinConstants+SpinConstants_dwBackoffFactor

        cmp     ebx, dword ptr g_SpinConstants+SpinConstants_dwMaximumDuration
        jle     MonTryEnterRetryHelperThinLock

        jmp     MonTryEnterWouldBlock

MonTryEnterRetryHelperThinLock: 
        jmp     MonTryEnterRetryThinLock


MonTryEnterHaveSyncBlockIndexOrHash: 
        ; If we have a hash code already, we need to create a sync block
        test    eax, BIT_SBLK_IS_HASHCODE_ASM
        jnz     MonTryEnterFramedLockHelper

        ; Just and out the top bits and grab the syncblock index
        and     eax, MASK_SYNCBLOCKINDEX_ASM

        ; Get the sync block pointer.
        mov     ARGUMENT_REG2, dword ptr g_pSyncTable
        mov     ARGUMENT_REG2, [ARGUMENT_REG2+eax*SizeOfSyncTableEntry_ASM+SyncTableEntry_m_SyncBlock]

        ; Check if the sync block has been allocated.
        test    ARGUMENT_REG2, ARGUMENT_REG2
        jz      MonTryEnterFramedLockHelper

        ; Get a pointer to the lock object.
        lea     ARGUMENT_REG2, [ARGUMENT_REG2+SyncBlock_m_Monitor]        

MonTryEnterRetrySyncBlock: 
        ; Attempt to acquire the lock.
        mov     eax, [ARGUMENT_REG2+AwareLock_m_MonitorHeld]
        test    eax,eax
        jne     MonTryEnterHaveWaiters

        ; We need another scratch register for what follows, so save EBX now so"
        ; we can use it for that purpose."
        push    ebx

        ; Common case, lock isn't held and there are no waiters. Attempt to
        ; gain ownership ourselves.
        mov     ebx,1
        lock cmpxchg [ARGUMENT_REG2+AwareLock_m_MonitorHeld],ebx

        pop     ebx
        
        jnz     MonTryEnterRetryHelperSyncBlock

        ; Success. Save the thread object in the lock and increment the use count.
        mov     dword ptr [ARGUMENT_REG2+AwareLock_m_HoldingThread],esi
        inc     dword ptr [ARGUMENT_REG2+AwareLock_m_Recursion]        
        inc     dword ptr [esi+Thread_m_dwLockCount]

ifdef MON_DEBUG
ifdef TRACK_SYNC
        push    ARGUMENT_REG2 ; AwareLock
        push    [esp+4]   ; return address
        call    EnterSyncHelper
endif ;TRACK_SYNC        
endif ;MON_DEBUG

        pop     esi
        pop     ebx

        ; Timeout parameter not needed, ditch it from the stack."
        add     esp,4

		mov		eax, [esp+4]
        mov     byte ptr [eax], 1
        ret		4

        ; It's possible to get here with waiters but no lock held, but in this
        ; case a signal is about to be fired which will wake up a waiter. So
        ; for fairness sake we should wait too.
        ; Check first for recursive lock attempts on the same thread.
MonTryEnterHaveWaiters: 
        ; Is mutex already owned by current thread?
        cmp     [ARGUMENT_REG2+AwareLock_m_HoldingThread],esi
        jne     MonTryEnterPrepareToWait

        ; Yes, bump our use count.
        inc     dword ptr [ARGUMENT_REG2+AwareLock_m_Recursion]
ifdef MON_DEBUG
ifdef TRACK_SYNC
        push    ARGUMENT_REG2 ; AwareLock
        push    [esp+4]   ; return address
        call    EnterSyncHelper
endif ;TRACK_SYNC        
endif ;MON_DEBUG
        pop     esi
        pop     ebx

        ; Timeout parameter not needed, ditch it from the stack.
        add     esp,4

		mov		eax, [esp+4]
        mov     byte ptr [eax], 1
        ret		4

MonTryEnterPrepareToWait:
        ; If we are on an MP system, we try spinning for a certain number of iterations
        cmp     dword ptr g_SystemInfo+SYSTEM_INFO_dwNumberOfProcessors,1
        jle     MonTryEnterWouldBlock

        ; exponential backoff: delay by approximately 2*ebx clock cycles (on a PIII)
        mov     eax, ebx
MonTryEnterdelayLoop:
        $repnop ; indicate to the CPU that we are spin waiting (useful for some Intel P4 multiprocs)
        dec     eax
        jnz     MonTryEnterdelayLoop

        ; next time, wait a factor longer
        imul    ebx, dword ptr g_SpinConstants+SpinConstants_dwBackoffFactor

        cmp     ebx, dword ptr g_SpinConstants+SpinConstants_dwMaximumDuration
        jle     MonTryEnterRetrySyncBlock

        ; We would need to block to enter the section. Return failure if
        ; timeout is zero, else call the framed helper to do the blocking
        ; form of TryEnter."
MonTryEnterWouldBlock: 
        pop     esi
        pop     ebx
        pop     ARGUMENT_REG2
        test    ARGUMENT_REG2, ARGUMENT_REG2
        jnz     MonTryEnterBlock
		mov		eax, [esp+4]
        mov     byte ptr [eax], 0
        ret		4

MonTryEnterRetryHelperSyncBlock: 
        jmp     MonTryEnterRetrySyncBlock

MonTryEnterFramedLockHelper: 
        ; ARGUMENT_REG1 has the object to synchronize on, must retrieve the
        ; timeout parameter from the stack.
        pop     esi
        pop     ebx
        pop     ARGUMENT_REG2
MonTryEnterBlock:        
        jmp     JITutil_MonTryEnter

@JIT_MonTryEnter@12 endp

;**********************************************************************
; This is a frameless helper for exiting a monitor on a object.
; The object is in ARGUMENT_REG1.  This tries the normal case (no
; blocking or object allocation) in line and calls a framed helper
; for the other cases.
; ***** NOTE: if you make any changes to this routine, build with MON_DEBUG undefined
; to make sure you don't break the non-debug build. This is very fragile code.
; Also, propagate the changes to jithelp.s which contains the same helper and assembly code
; (in AT&T syntax) for gnu assembler.
@JIT_MonExitWorker@4 proc public
        ; The thin lock logic needs an additional register to hold the thread, unfortunately
        push    esi
        
        ; Check if the instance is NULL.
        test    ARGUMENT_REG1, ARGUMENT_REG1
        jz      MonExitFramedLockHelper
        
        call    _GetThread@0
        mov     esi,eax

MonExitRetryThinLock: 
        ; Fetch the header dword and check its layout and the spin lock bit
        mov     eax, [ARGUMENT_REG1-SyncBlockIndexOffset_ASM]
        ;BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX_SPIN_LOCK_ASM = BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX + BIT_SBLK_SPIN_LOCK
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX_SPIN_LOCK_ASM
        jnz     MonExitNeedMoreTests

        ; Ok, we have a "thin lock" layout - check whether the thread id matches
        mov     edx,eax
        and     edx, SBLK_MASK_LOCK_THREADID_ASM
        cmp     edx, [esi+Thread_m_ThreadId]
        jne     MonExitFramedLockHelper

        ; Check the recursion level
        test    eax, SBLK_MASK_LOCK_RECLEVEL_ASM
        jne     MonExitDecRecursionLevel

        ; It's zero - we're leaving the lock.
        ; So try to put back a zero thread id.
        ; edx and eax match in the thread id bits, and edx is zero elsewhere, so the xor is sufficient
        xor     edx,eax
        lock cmpxchg [ARGUMENT_REG1-SyncBlockIndexOffset_ASM],edx
        jnz     MonExitRetryHelperThinLock

        ; We're done
        sub     [esi+Thread_m_dwLockCount],1
        pop     esi
        ret

MonExitDecRecursionLevel: 
        lea     edx, [eax-SBLK_LOCK_RECLEVEL_INC_ASM]
        lock cmpxchg [ARGUMENT_REG1-SyncBlockIndexOffset_ASM],edx
        jnz     MonExitRetryHelperThinLock

        ; We're done
        pop     esi
        ret

MonExitNeedMoreTests:
        ;Forward all special cases to the slow helper
        ;BIT_SBLK_IS_HASHCODE_OR_SPIN_LOCK_ASM = BIT_SBLK_IS_HASHCODE + BIT_SBLK_SPIN_LOCK
        test    eax, BIT_SBLK_IS_HASHCODE_OR_SPIN_LOCK_ASM
        jnz     MonExitFramedLockHelper

        ; Get the sync block index and use it to compute the sync block pointer
        mov     ARGUMENT_REG2, dword ptr g_pSyncTable
        and     eax, MASK_SYNCBLOCKINDEX_ASM
        mov     ARGUMENT_REG2, [ARGUMENT_REG2+eax*SizeOfSyncTableEntry_ASM+SyncTableEntry_m_SyncBlock]        

        ; was there a sync block?
        test    ARGUMENT_REG2, ARGUMENT_REG2
        jz      MonExitFramedLockHelper

        ; Get a pointer to the lock object.
        lea     ARGUMENT_REG2, [ARGUMENT_REG2+SyncBlock_m_Monitor]

        ; Check if lock is held.
        cmp     [ARGUMENT_REG2+AwareLock_m_HoldingThread],esi
        jne     MonExitFramedLockHelper

ifdef MON_DEBUG
ifdef TRACK_SYNC
        push    ARGUMENT_REG1 ; preserve regs
        push    ARGUMENT_REG2

        push    ARGUMENT_REG2 ; AwareLock
        push    [esp+8]       ; return address
        call    LeaveSyncHelper

        pop     ARGUMENT_REG2 ; restore regs
        pop     ARGUMENT_REG1
endif ;TRACK_SYNC        
endif ;MON_DEBUG
        ; Reduce our recursion count.
        dec     dword ptr [ARGUMENT_REG2+AwareLock_m_Recursion]
        jz      MonExitLastRecursion

        pop     esi
        ret

MonExitRetryHelperThinLock: 
        jmp     MonExitRetryThinLock

MonExitFramedLockHelper: 
        pop     esi
        jmp     JITutil_MonExitWorker

        ; This is the last count we held on this lock, so release the lock.
MonExitLastRecursion: 
        dec     dword ptr [esi+Thread_m_dwLockCount]
        mov     dword ptr [ARGUMENT_REG2+AwareLock_m_HoldingThread],0

MonExitRetry: 
        mov     eax, [ARGUMENT_REG2+AwareLock_m_MonitorHeld]
        lea     esi, [eax-1]
        lock cmpxchg [ARGUMENT_REG2+AwareLock_m_MonitorHeld], esi
        jne     MonExitRetryHelper        
        pop     esi        
        test    eax,0FFFFFFFEh
        jne     MonExitMustSignal

        ret

MonExitMustSignal:
        mov     ARGUMENT_REG1, ARGUMENT_REG2
        jmp     JITutil_MonSignal

MonExitRetryHelper: 
        jmp     MonExitRetry

@JIT_MonExitWorker@4 endp

;**********************************************************************
; This is a frameless helper for entering a static monitor on a class.
; The methoddesc is in ARGUMENT_REG1.  This tries the normal case (no
; blocking or object allocation) in line and calls a framed helper
; for the other cases.
; Note we are changing the methoddesc parameter to a pointer to the
; AwareLock.
; ***** NOTE: if you make any changes to this routine, build with MON_DEBUG undefined
; to make sure you don't break the non-debug build. This is very fragile code.
; Also, propagate the changes to jithelp.s which contains the same helper and assembly code
; (in AT&T syntax) for gnu assembler.
@JIT_MonEnterStatic@4 proc public
        ; We need another scratch register for what follows, so save EBX now so
        ; we can use it for that purpose.
        push    ebx

        ; Attempt to acquire the lock
MonEnterStaticRetry: 
        mov     eax, [ARGUMENT_REG1+AwareLock_m_MonitorHeld]
        test    eax,eax
        jne     MonEnterStaticHaveWaiters

        ; Common case, lock isn't held and there are no waiters. Attempt to
        ; gain ownership ourselves.
        mov     ebx,1
        lock cmpxchg [ARGUMENT_REG1+AwareLock_m_MonitorHeld],ebx
        jnz     MonEnterStaticRetryHelper

        pop     ebx

        ; Success. Save the thread object in the lock and increment the use count.
        call    _GetThread@0
        mov     [ARGUMENT_REG1+AwareLock_m_HoldingThread], eax
        inc     dword ptr [ARGUMENT_REG1+AwareLock_m_Recursion]
        inc     dword ptr [eax+Thread_m_dwLockCount]

ifdef MON_DEBUG
ifdef TRACK_SYNC
        push    ARGUMENT_REG1   ; AwareLock
        push    [esp+4]         ; return address
        call    EnterSyncHelper
endif ;TRACK_SYNC
endif ;MON_DEBUG
        ret

        ; It's possible to get here with waiters but no lock held, but in this
        ; case a signal is about to be fired which will wake up a waiter. So
        ; for fairness sake we should wait too.
        ; Check first for recursive lock attempts on the same thread.
MonEnterStaticHaveWaiters: 
        ; Get thread but preserve EAX (contains cached contents of m_MonitorHeld).
        push    eax
        call    _GetThread@0
        mov     ebx,eax
        pop     eax

        ; Is mutex already owned by current thread?
        cmp     [ARGUMENT_REG1+AwareLock_m_HoldingThread],ebx
        jne     MonEnterStaticPrepareToWait

        ; Yes, bump our use count.
        inc     dword ptr [ARGUMENT_REG1+AwareLock_m_Recursion]
ifdef MON_DEBUG
ifdef TRACK_SYNC
        push    ARGUMENT_REG1   ; AwareLock
        push    [esp+4]         ; return address
        call    EnterSyncHelper
endif ;TRACK_SYNC
endif ;MON_DEBUG
        pop     ebx
        ret

MonEnterStaticPrepareToWait: 
        pop     ebx

        ; ARGUMENT_REG1 should have AwareLock. Call contention helper.
        jmp     JITutil_MonContention

MonEnterStaticRetryHelper: 
        jmp     MonEnterStaticRetry
@JIT_MonEnterStatic@4 endp

;**********************************************************************
; A frameless helper for exiting a static monitor on a class.
; The methoddesc is in ARGUMENT_REG1.  This tries the normal case (no
; blocking or object allocation) in line and calls a framed helper
; for the other cases.
; Note we are changing the methoddesc parameter to a pointer to the
; AwareLock.
; ***** NOTE: if you make any changes to this routine, build with MON_DEBUG undefined
; to make sure you don't break the non-debug build. This is very fragile code.
; Also, propagate the changes to jithelp.s which contains the same helper and assembly code
; (in AT&T syntax) for gnu assembler.
@JIT_MonExitStatic@4 proc public

ifdef MON_DEBUG
ifdef TRACK_SYNC
        push    ARGUMENT_REG1   ; preserve regs

        push    ARGUMENT_REG1   ; AwareLock
        push    [esp+8]         ; return address
        call    LeaveSyncHelper

        pop     [ARGUMENT_REG1] ; restore regs
endif ;TRACK_SYNC
endif ;MON_DEBUG

        ; Check if lock is held.
        call    _GetThread@0
        cmp     [ARGUMENT_REG1+AwareLock_m_HoldingThread],eax
        jne     MonExitStaticLockError

        ; Reduce our recursion count.
        dec     dword ptr [ARGUMENT_REG1+AwareLock_m_Recursion]
        jz      MonExitStaticLastRecursion

        ret

        ; This is the last count we held on this lock, so release the lock.
MonExitStaticLastRecursion: 
        ; eax must have the thread object
        dec     dword ptr [eax+Thread_m_dwLockCount]
        mov     dword ptr [ARGUMENT_REG1+AwareLock_m_HoldingThread],0
        push    ebx

MonExitStaticRetry: 
        mov     eax, [ARGUMENT_REG1+AwareLock_m_MonitorHeld]
        lea     ebx, [eax-1]
        lock cmpxchg [ARGUMENT_REG1+AwareLock_m_MonitorHeld],ebx
        jne     MonExitStaticRetryHelper
        pop     ebx
        test    eax,0FFFFFFFEh
        jne     MonExitStaticMustSignal

        ret

MonExitStaticMustSignal: 
        jmp     JITutil_MonSignal

MonExitStaticRetryHelper: 
        jmp     MonExitStaticRetry
        ; Throw a synchronization lock exception.
MonExitStaticLockError: 
        mov     ARGUMENT_REG1, CORINFO_SynchronizationLockException_ASM
        jmp     JIT_InternalThrow

@JIT_MonExitStatic@4 endp

; PatchedCodeStart and PatchedCodeEnd are used to determine bounds of patched code.
; 

_JIT_PatchedCodeStart@0 proc public
ret
_JIT_PatchedCodeStart@0 endp

;
; Optimized TLS getters
;

            ALIGN 4
            
ifndef FEATURE_IMPLICIT_TLS
_GetThread@0 proc public
            ; This will be overwritten at runtime with optimized GetThread implementation
            jmp short _GetTLSDummy@0
            ; Just allocate space that will be filled in at runtime
            db (TLS_GETTER_MAX_SIZE_ASM - 2) DUP (0CCh)
_GetThread@0 endp

            ALIGN 4

_GetAppDomain@0 proc public
            ; This will be overwritten at runtime with optimized GetAppDomain implementation
            jmp short _GetTLSDummy@0
            ; Just allocate space that will be filled in at runtime
            db (TLS_GETTER_MAX_SIZE_ASM - 2) DUP (0CCh)
_GetAppDomain@0 endp

_GetTLSDummy@0 proc public
            xor eax,eax
            ret
_GetTLSDummy@0 endp

            ALIGN 4

_ClrFlsGetBlock@0 proc public
            ; This will be overwritten at runtime with optimized ClrFlsGetBlock implementation
            jmp short _GetTLSDummy@0
            ; Just allocate space that will be filled in at runtime
            db (TLS_GETTER_MAX_SIZE_ASM - 2) DUP (0CCh)
_ClrFlsGetBlock@0 endp
endif

;**********************************************************************
; Write barriers generated at runtime

PUBLIC _JIT_PatchedWriteBarrierStart@0
_JIT_PatchedWriteBarrierStart@0 PROC
ret
_JIT_PatchedWriteBarrierStart@0 ENDP

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

PUBLIC _JIT_PatchedWriteBarrierLast@0
_JIT_PatchedWriteBarrierLast@0 PROC
ret
_JIT_PatchedWriteBarrierLast@0 ENDP

;**********************************************************************
; PrecodeRemotingThunk is patched at runtime to activate it
ifdef FEATURE_REMOTING
        ALIGN 16
_PrecodeRemotingThunk@0 proc public

        ret                             ; This is going to be patched to "test ecx,ecx"
        nop

        jz      RemotingDone            ; predicted not taken

        cmp     dword ptr [ecx],11111111h ; This is going to be patched to address of the transparent proxy
        je      RemotingCheck           ; predicted not taken

RemotingDone:
        ret

RemotingCheck:
        push     eax            ; save method desc
        mov      eax, dword ptr [ecx + TransparentProxyObject___stubData]
        call     [ecx + TransparentProxyObject___stub]
        test     eax, eax
        jnz      RemotingCtxMismatch
        mov      eax, [esp]
        mov      ax, [eax + MethodDesc_m_wFlags]
        and      ax, MethodDesc_mdcClassification
        cmp      ax, MethodDesc_mcComInterop
        je       ComPlusCall
        pop      eax            ; throw away method desc
        jmp      RemotingDone

RemotingCtxMismatch:
        pop      eax            ; restore method desc
        add      esp, 4         ; pop return address into the precode
        jmp      _TransparentProxyStub_CrossContext@0
        
ComPlusCall:
        pop      eax            ; restore method desc
        mov      [esp],eax      ; replace return address into the precode with method desc (argument for TP stub)
        jmp      _InContextTPQuickDispatchAsmStub@0        

_PrecodeRemotingThunk@0 endp
endif ;  FEATURE_REMOTING

_JIT_PatchedCodeLast@0 proc public
ret
_JIT_PatchedCodeLast@0 endp

; This is the first function outside the "keep together range". Used by BBT scripts.
_JIT_PatchedCodeEnd@0 proc public
ret
_JIT_PatchedCodeEnd@0 endp

; This is the ASM portion of JIT_IsInstanceOfInterface.  For all the bizarre cases, it quickly
; fails and falls back on the JITutil_IsInstanceOfAny helper.  So all failure cases take
; the slow path, too.
;
; ARGUMENT_REG1 = array or interface to check for.
; ARGUMENT_REG2 = instance to be cast.

        ALIGN 16
PUBLIC @JIT_IsInstanceOfInterface@8
@JIT_IsInstanceOfInterface@8 PROC
        test    ARGUMENT_REG2, ARGUMENT_REG2
        jz      IsNullInst

        mov     eax, [ARGUMENT_REG2]            ; get MethodTable

        push    ebx
        push    esi
        movzx   ebx, word ptr [eax+MethodTable_m_wNumInterfaces]

        ; check if this MT implements any interfaces
        test    ebx, ebx
        jz      IsInstanceOfInterfaceDoBizarre

        ; move Interface map ptr into eax
        mov     eax, [eax+MethodTable_m_pInterfaceMap]

IsInstanceOfInterfaceTop:
        ; eax -> current InterfaceInfo_t entry in interface map list
ifdef FEATURE_PREJIT
        mov     esi, [eax]
        test    esi, 1
        ; Move the deference out of line so that this jump is correctly predicted for the case
        ; when there is no indirection
        jnz     IsInstanceOfInterfaceIndir
        cmp     ARGUMENT_REG1, esi
else
        cmp     ARGUMENT_REG1, [eax]
endif
        je      IsInstanceOfInterfaceFound

IsInstanceOfInterfaceNext:
        add     eax, SIZEOF_InterfaceInfo_t
        dec     ebx
        jnz     IsInstanceOfInterfaceTop

        ; fall through to DoBizarre

IsInstanceOfInterfaceDoBizarre:
        pop     esi
        pop     ebx
        mov     eax, [ARGUMENT_REG2]    ; get MethodTable
        test    dword ptr [eax+MethodTable_m_dwFlags], NonTrivialInterfaceCastFlags
        jnz     IsInstanceOfInterfaceNonTrivialCast

IsNullInst:
        xor     eax,eax
        ret

ifdef FEATURE_PREJIT
IsInstanceOfInterfaceIndir:
        cmp     ARGUMENT_REG1,[esi-1]
        jne     IsInstanceOfInterfaceNext
endif

IsInstanceOfInterfaceFound:
        pop     esi
        pop     ebx
        mov     eax, ARGUMENT_REG2      ; the successful instance
        ret

IsInstanceOfInterfaceNonTrivialCast:
        jmp     @JITutil_IsInstanceOfInterface@8

@JIT_IsInstanceOfInterface@8 endp

; This is the ASM portion of JIT_ChkCastInterface.  For all the bizarre cases, it quickly
; fails and falls back on the JITutil_ChkCastAny helper.  So all failure cases take
; the slow path, too.
;
; ARGUMENT_REG1 = array or interface to check for.
; ARGUMENT_REG2 = instance to be cast.

        ALIGN 16
PUBLIC @JIT_ChkCastInterface@8
@JIT_ChkCastInterface@8 PROC
        test    ARGUMENT_REG2, ARGUMENT_REG2
        jz      ChkCastInterfaceIsNullInst

        mov     eax, [ARGUMENT_REG2]            ; get MethodTable

        push    ebx
        push    esi
        movzx   ebx, word ptr [eax+MethodTable_m_wNumInterfaces]

        ; speculatively move Interface map ptr into eax
        mov     eax, [eax+MethodTable_m_pInterfaceMap]

        ; check if this MT implements any interfaces
        test    ebx, ebx
        jz      ChkCastInterfaceDoBizarre

ChkCastInterfaceTop:
        ; eax -> current InterfaceInfo_t entry in interface map list
ifdef FEATURE_PREJIT
        mov     esi, [eax]
        test    esi, 1
        ; Move the deference out of line so that this jump is correctly predicted for the case
        ; when there is no indirection
        jnz     ChkCastInterfaceIndir
        cmp     ARGUMENT_REG1, esi
else
        cmp     ARGUMENT_REG1, [eax]
endif
        je      ChkCastInterfaceFound

ChkCastInterfaceNext:
        add     eax, SIZEOF_InterfaceInfo_t
        dec     ebx
        jnz     ChkCastInterfaceTop

        ; fall through to DoBizarre

ChkCastInterfaceDoBizarre:
        pop     esi
        pop     ebx
        jmp     @JITutil_ChkCastInterface@8

ifdef FEATURE_PREJIT
ChkCastInterfaceIndir:
        cmp     ARGUMENT_REG1,[esi-1]
        jne     ChkCastInterfaceNext
endif

ChkCastInterfaceFound:
        pop     esi
        pop     ebx

ChkCastInterfaceIsNullInst:
        mov     eax, ARGUMENT_REG2      ; either null, or the successful instance
        ret

@JIT_ChkCastInterface@8 endp

    end
