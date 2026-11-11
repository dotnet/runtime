;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros_Shared.inc

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; The following helper will access ("probe") a word on each page of the stack
; starting with the page right beneath esp down to the one pointed to by eax.
; The procedure is needed to make sure that the "guard" page is pushed down below the allocated stack frame.
; The call to the helper will be emitted by JIT in the function prolog when large (larger than 0x3000 bytes) stack frame is required.
;
; NOTE: this helper will modify a value of esp and must establish the frame pointer.
PROBE_STEP equ 1000h

RhpStackProbe PROC public
    ALTERNATE_ENTRY _RhpStackProbe@0

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

;; CoreCLR translates faults between RhpStackProbe and RhpStackProbe_End into faults at the call site.
_RhpStackProbe_End@0 PROC public
    ret
_RhpStackProbe_End@0 ENDP

RhpGcPoll PROC public
    ALTERNATE_ENTRY @RhpGcPoll@0

        cmp         [G_TRAP_THREADS], 0
        jne         @F                  ; forward branch - predicted not taken
        ret
@@:
        TAILCALL_GC_POLL_RARE
RhpGcPoll ENDP

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
RhpLLsh PROC public
    ALTERNATE_ENTRY _RhpLLsh@0

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
RhpLRsh PROC public
    ALTERNATE_ENTRY _RhpLRsh@0

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
RhpLRsz PROC public
    ALTERNATE_ENTRY _RhpLRsz@0

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

end
