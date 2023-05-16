;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

EXTERN RhpGetInlinedThreadStaticBaseSlow : PROC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; The following helper will access ("probe") a word on each page of the stack
; starting with the page right beneath rsp down to the one pointed to by r11.
; The procedure is needed to make sure that the "guard" page is pushed down below the allocated stack frame.
; The call to the helper will be emitted by JIT in the function/funclet prolog when large (larger than 0x3000 bytes) stack frame is required.
;
; NOTE: this helper will NOT modify a value of rsp and can be defined as a leaf function.

PAGE_SIZE equ 1000h

LEAF_ENTRY RhpStackProbe, _TEXT
        ; On entry:
        ;   r11 - points to the lowest address on the stack frame being allocated (i.e. [InitialSp - FrameSize])
        ;   rsp - points to some byte on the last probed page
        ; On exit:
        ;   rax - is not preserved
        ;   r11 - is preserved
        ;
        ; NOTE: this helper will probe at least one page below the one pointed by rsp.

        mov     rax, rsp               ; rax points to some byte on the last probed page
        and     rax, -PAGE_SIZE        ; rax points to the **lowest address** on the last probed page
                                       ; This is done to make the following loop end condition simpler.

ProbeLoop:
        sub     rax, PAGE_SIZE         ; rax points to the lowest address of the **next page** to probe
        test    dword ptr [rax], eax   ; rax points to the lowest address on the **last probed** page
        cmp     rax, r11
        jg      ProbeLoop              ; If (rax > r11), then we need to probe at least one more page.

        ret

LEAF_END RhpStackProbe, _TEXT

LEAF_ENTRY RhpGetInlinedThreadStaticBase, _TEXT
        ; On exit:
        ;   rax - the thread static base for the given type

        ;; rcx = &tls_InlinedThreadStatics, TRASHES r8
        INLINE_GET_TLS_VAR rcx, r8, tls_InlinedThreadStatics

        ;; get per-thread storage
        mov     rax, [rcx]
        test    rax, rax
        jz      RhpGetInlinedThreadStaticBaseSlow   ;; rcx contains the storage ref

        ;; return it
        ret
LEAF_END RhpGetInlinedThreadStaticBase, _TEXT

end
