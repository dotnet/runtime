;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .686P
        .XMM
        .model  flat
        option  casemap:none
        .code

include AsmMacros.inc

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; The following helper will access ("probe") a word on each page of the stack
; starting with the page right beneath esp down to the one pointed to by eax.
; The procedure is needed to make sure that the "guard" page is pushed down below the allocated stack frame.
; The call to the helper will be emitted by JIT in the function prolog when large (larger than 0x3000 bytes) stack frame is required.
;
; NOTE: this helper will modify a value of esp and must establish the frame pointer.
PROBE_STEP equ 1000h

_RhpStackProbe PROC public
ALTERNATE_HELPER_ENTRY RhpStackProbe
    ; On entry:
    ;   eax - the lowest address of the stack frame being allocated (i.e. [InitialSp - FrameSize])
    ;
    ; NOTE: this helper will probe at least one page below the one pointed by esp.
    push    ebp
    mov     ebp, esp

    and     esp, -PROBE_STEP      ; esp points to the **lowest address** on the last probed page
                                 ; This is done to make the loop end condition simpler.
ProbeLoop:
    sub     esp, PROBE_STEP       ; esp points to the lowest address of the **next page** to probe
    test    [esp], eax           ; esp points to the lowest address on the **last probed** page
    cmp     esp, eax
    jg      ProbeLoop            ; if esp > eax, then we need to probe at least one more page.

    mov     esp, ebp
    pop     ebp
    ret

_RhpStackProbe ENDP

_RhpEndCatch PROC stdcall public
ALTERNATE_HELPER_ENTRY RhpEndCatch
    int 3

_RhpEndCatch ENDP

;; *********************************************************************/
;; llshl - long shift left
;;
;; Purpose:
;;    Does a Long Shift Left (signed and unsigned are identical)
;;    Shifts a long left any number of bits.
;;
;;        NOTE:  This routine has been adapted from the Microsoft CRTs.
;;
;; Entry:
;;    EDX:EAX - long value to be shifted
;;        ECX - number of bits to shift by
;;
;; Exit:
;;    EDX:EAX - shifted value
;;
RhpLLsh PROC public
    ;; Reduce shift amount mod 64
    and     ecx, 63

    cmp     ecx, 32
    jae     LLshMORE32

    ;; Handle shifts of between bits 0 and 31
    shld    edx, eax, cl
    shl     eax, cl
    ret

LLshMORE32:
    ;; Handle shifts of between bits 32 and 63
    ;; The x86 shift instructions only use the lower 5 bits.
    mov     edx, eax
    xor     eax, eax
    shl     edx, cl
    ret
RhpLLsh ENDP

;; *********************************************************************/
;; LRsh - long shift right
;;
;; Purpose:
;;    Does a signed Long Shift Right
;;    Shifts a long right any number of bits.
;;
;;        NOTE:  This routine has been adapted from the Microsoft CRTs.
;;
;; Entry:
;;    EDX:EAX - long value to be shifted
;;        ECX - number of bits to shift by
;;
;; Exit:
;;    EDX:EAX - shifted value
;;
RhpLRsh PROC public
    ;; Reduce shift amount mod 64
    and     ecx, 63

    cmp     ecx, 32
    jae     LRshMORE32

    ;; Handle shifts of between bits 0 and 31
    shrd    eax, edx, cl
    sar     edx, cl
    ret

LRshMORE32:
    ;; Handle shifts of between bits 32 and 63
    ;; The x86 shift instructions only use the lower 5 bits.
    mov     eax, edx
    sar     edx, 31
    sar     eax, cl
    ret
RhpLRsh ENDP

;; *********************************************************************/
;;  LRsz:
;; Purpose:
;;    Does a unsigned Long Shift Right
;;    Shifts a long right any number of bits.
;;
;;        NOTE:  This routine has been adapted from the Microsoft CRTs.
;;
;; Entry:
;;    EDX:EAX - long value to be shifted
;;        ECX - number of bits to shift by
;;
;; Exit:
;;    EDX:EAX - shifted value
;;
RhpLRsz PROC public
    ;; Reduce shift amount mod 64
    and     ecx, 63

    cmp     ecx, 32
    jae     LRszMORE32

    ;; Handle shifts of between bits 0 and 31
    shrd    eax, edx, cl
    shr     edx, cl
    ret

LRszMORE32:
    ;; Handle shifts of between bits 32 and 63
    ;; The x86 shift instructions only use the lower 5 bits.
    mov     eax, edx
    xor     edx, edx
    shr     eax, cl
    ret
RhpLRsz ENDP

EXTERN __dtol3 : PROC

RhpDbl2Lng PROC public
    movsd   xmm0, qword ptr [esp+4]
    call    __dtol3
    ret 8
RhpDbl2Lng ENDP

RhpDbl2Int PROC public
    movsd   xmm0, qword ptr [esp+4]
    cvttsd2si eax, xmm0
    ret 8
RhpDbl2Int ENDP

RhpDbl2UInt PROC public
    movsd   xmm0, qword ptr [esp+4]
    call    __dtol3
    ret 8
RhpDbl2UInt ENDP

EXTERN @RhpDbl2ULng@8 : PROC

RhpDbl2ULng PROC public
    jmp @RhpDbl2ULng@8
RhpDbl2ULng ENDP

EXTERN __ltod3 : PROC

RhpLng2Dbl PROC public
    mov     edx, dword ptr [esp+8]
    mov     ecx, dword ptr [esp+4]
    call    __ltod3
    movsd   qword ptr [esp+4], xmm0
    fld     qword ptr [esp+4]
    ret     8
RhpLng2Dbl ENDP

EXTERN __ultod3 : PROC

RhpULng2Dbl PROC public
    mov     edx, dword ptr [esp+8]
    mov     ecx, dword ptr [esp+4]
    call    __ultod3
    movsd   qword ptr [esp+4], xmm0
    fld     qword ptr [esp+4]
    ret     8
RhpULng2Dbl ENDP

EXTERN __alldiv : PROC

RhpLDiv PROC public
    push    dword ptr [esp+16]
    push    dword ptr [esp+16]
    push    dword ptr [esp+16]
    push    dword ptr [esp+16]
    call    __alldiv
    ret     16
RhpLDiv ENDP

EXTERN __allrem : PROC

RhpLMod PROC public
    push    dword ptr [esp+16]
    push    dword ptr [esp+16]
    push    dword ptr [esp+16]
    push    dword ptr [esp+16]
    call    __allrem
    ret     16
RhpLMod ENDP

EXTERN __aulldiv : PROC

RhpULDiv PROC public
    push    dword ptr [esp+16]
    push    dword ptr [esp+16]
    push    dword ptr [esp+16]
    push    dword ptr [esp+16]
    call    __aulldiv
    ret     16
RhpULDiv ENDP

EXTERN __aullrem : PROC

RhpULMod PROC public
    push    dword ptr [esp+16]
    push    dword ptr [esp+16]
    push    dword ptr [esp+16]
    push    dword ptr [esp+16]
    call    __aullrem
    ret     16
RhpULMod ENDP

EXTERN __allmul : PROC

RhpLMul PROC public
    push    DWORD PTR [esp+16]
    push    DWORD PTR [esp+16]
    push    DWORD PTR [esp+16]
    push    DWORD PTR [esp+16]
    call    __allmul
    ret     16
RhpLMul ENDP

end
