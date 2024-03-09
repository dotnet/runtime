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

RhpStackProbe PROC public
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

RhpStackProbe ENDP

;; *********************************************************************/
;; LLsh - long shift left
;;
;; Purpose:
;;    Does a Long Shift Left (signed and unsigned are identical)
;;    Shifts a long left any number of bits.
;;
;; Entry:
;;    EDX:EAX - long value to be shifted
;;        ECX - number of bits to shift by
;;
;; Exit:
;;    EDX:EAX - shifted value
;;
;; NOTE: Adapted from JIT_LLsh in CoreCLR
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
;; Entry:
;;    EDX:EAX - long value to be shifted
;;        ECX - number of bits to shift by
;;
;; Exit:
;;    EDX:EAX - shifted value
;;
;; NOTE: Adapted from JIT_LRsh in CoreCLR
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
;; LRsz
;;
;; Purpose:
;;    Does a unsigned Long Shift Right
;;    Shifts a long right any number of bits.
;;
;; Entry:
;;    EDX:EAX - long value to be shifted
;;        ECX - number of bits to shift by
;;
;; Exit:
;;    EDX:EAX - shifted value
;;
;; NOTE: Adapted from JIT_LRsz in CoreCLR
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

;; *********************************************************************/
;; LMul
;;
;; Purpose:
;;    Does a long multiply (same for signed/unsigned)
;;
;; Entry:
;;    Parameters are passed on the stack:
;;    1st pushed: multiplier (QWORD)
;;    2nd pushed: multiplicand (QWORD)
;;
;; Exit:
;;    EDX:EAX - product of multiplier and multiplicand
;;
;; NOTE: Adapted from clang generated code
;;
RhpLMul PROC public
    push    esi
    mov     ecx, dword ptr [esp + 16]
    mov     esi, dword ptr [esp + 8]
    mov     eax, ecx
    mul     esi
    imul    ecx, dword ptr [esp + 12]
    add     edx, ecx
    imul    esi, dword ptr [esp + 20]
    add     edx, esi
    pop     esi
    ret     16
RhpLMul ENDP

;; *********************************************************************/
;; Dbl2Lng
;;
;; Purpose:
;;    Converts a double to a long truncating toward zero (C semantics)
;;    Uses stdcall calling conventions
;;
;; NOTE: Adapted from JIT_Dbl2LngP4x87 in CoreCLR
;;
RhpDbl2Lng PROC public
ALTERNATE_ENTRY RhpDbl2UInt
    sub     esp, 8                  ; get some local space

    fld     qword ptr [esp+0Ch]     ; fetch arg
    fnstcw  word ptr [esp+0Ch]      ; store FPCW
    movzx   eax, word ptr [esp+0Ch] ; zero extend - wide
    or      ah, 0Ch                 ; turn on OE and DE flags
    mov     dword ptr [esp], eax    ; store new FPCW bits
    fldcw   word ptr  [esp]         ; reload FPCW with new bits
    fistp   qword ptr [esp]         ; convert
    mov     eax, dword ptr [esp]    ; reload FP result
    mov     edx, dword ptr [esp+4]
    fldcw   word ptr [esp+0Ch]      ; reload original FPCW value

    add     esp, 8                  ; restore stack

    ret	8
RhpDbl2Lng ENDP

;; *********************************************************************/
;; Dbl2Int
;;
;; Purpose:
;;    Converts a double to a long truncating toward zero (C semantics)
;;    Uses stdcall calling conventions
;;
RhpDbl2Int PROC public
    cvttsd2si eax, qword ptr [esp+4]
    ret       8
RhpDbl2Int ENDP

;; *********************************************************************/
;; Lng2Dbl
;;
;; Purpose:
;;    Converts a long to a double (C semantics)
;;    Uses stdcall calling conventions
;;
RhpLng2Dbl PROC public
    fild    qword ptr [esp+4]
    ret     8
RhpLng2Dbl ENDP

;; *********************************************************************/
;; ULng2Dbl
;;
;; Purpose:
;;    Converts an unsigned long to a double (C semantics)
;;    Uses stdcall calling conventions
;;
;; NOTE: Adapted from GCC generated code
;;
RhpULng2Dbl PROC public
    sub     esp, 12
    mov     eax, dword ptr [esp+20]
    fild    qword ptr [esp+16]
    test    eax, eax
    jns     L2
    fadd    TWO_TO_64
L2:
    fstp    qword ptr [esp]
    fld     qword ptr [esp]
    add     esp, 12
    ret     8
.data
    TWO_TO_64 dd 5f800000h   ;; 2^64
.code
RhpULng2Dbl ENDP

;;
;; https://github.com/dotnet/runtime/pull/98858 moves the following helpers to
;; managed code, so no need to optimize this
;;

REDIRECT_FUNC macro ExportName, ImportName
    EXTERN ImportName : PROC

    public  ExportName
    ExportName    proc
        jmp ImportName
    ExportName    endp
endm

REDIRECT_FUNC RhpDbl2ULng, @RhpDbl2ULng@8
REDIRECT_FUNC RhpFltRem, @RhpFltRem@8
REDIRECT_FUNC RhpDblRem, @RhpDblRem@16

end
