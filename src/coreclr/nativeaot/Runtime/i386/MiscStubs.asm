;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
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
;; NOTE: Adapted from JIT_LMul in CoreCLR
;;
RhpLMul PROC public
    mov     eax, dword ptr [esp + 8]   ; AHI
    mov     ecx, dword ptr [esp + 16]  ; BHI
    or      ecx, eax                   ; test for both hiwords zero.
    mov     ecx, dword ptr [esp + 12]  ; BLO
    jnz     LMul_hard                  ; both are zero, just mult ALO and BLO

    mov     eax, dword ptr [esp + 4]
    mul     ecx
    ret     16

LMul_hard:
    push    ebx
    mul     ecx                        ; eax has AHI, ecx has BLO, so AHI * BLO
    mov     ebx, eax                   ; save result
    mov     eax, dword ptr [esp + 8]   ; ALO
    mul     dword ptr [esp + 20]       ; ALO * BHI
    add     ebx, eax                   ; ebx = ((ALO * BHI) + (AHI * BLO))
    mov     eax, dword ptr [esp + 8]   ; ALO   ;ecx = BLO
    mul     ecx                        ; so edx:eax = ALO*BLO
    add     edx, ebx                   ; now edx has all the LO*HI stuff
    pop     ebx
    ret     16
RhpLMul ENDP

end
