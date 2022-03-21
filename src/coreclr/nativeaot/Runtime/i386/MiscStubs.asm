;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

        .586
        .model  flat
        option  casemap:none
        .code

include AsmMacros.inc

EXTERN _memcpy                  : PROC
EXTERN _memcpyGCRefs            : PROC
EXTERN _memcpyGCRefsWithWriteBarrier  : PROC
EXTERN _memcpyAnyWithWriteBarrier     : PROC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* __cdecl RhpCopyMultibyteNoGCRefs(void*, void*, size_t)
;;
;; The purpose of this wrapper is to hoist the potential null reference exceptions of copying memory up to a place where
;; the stack unwinder and exception dispatch can properly transform the exception into a managed exception and dispatch
;; it to managed code.
;;
_RhpCopyMultibyteNoGCRefs PROC PUBLIC

        ;    #locals, num_params, prolog bytes, #regs saved, use ebp, frame type (0 == FRAME_FPO)
        .FPO(      0,          3,            0,           0,       0,          0)

        ; [esp + 0] return address
        ; [esp + 4] dest
        ; [esp + 8] src
        ; [esp + c] count

        cmp         dword ptr [esp + 0Ch], 0        ; check for a zero-length copy
        jz          NothingToCopy

        mov         ecx, [esp + 4]  ; ecx <- dest
        mov         edx, [esp + 8]  ; edx <- src

        ; Now check the dest and src pointers.  If they AV, the EH subsystem will recognize the address of the AV,
        ; unwind the frame, and fixup the stack to make it look like the (managed) caller AV'ed, which will be
        ; translated to a managed exception as usual.
ALTERNATE_ENTRY RhpCopyMultibyteNoGCRefsDestAVLocation
        cmp         byte ptr [ecx], 0
ALTERNATE_ENTRY RhpCopyMultibyteNoGCRefsSrcAVLocation
        cmp         byte ptr [edx], 0

        ; tail-call to plain-old-memcpy
        jmp         _memcpy

NothingToCopy:
        mov         eax, [esp + 4]                  ; return dest
        ret

_RhpCopyMultibyteNoGCRefs ENDP

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* __cdecl RhpCopyMultibyte(void*, void*, size_t)
;;
;; The purpose of this wrapper is to hoist the potential null reference exceptions of copying memory up to a place where
;; the stack unwinder and exception dispatch can properly transform the exception into a managed exception and dispatch
;; it to managed code.
;;
_RhpCopyMultibyte PROC PUBLIC

        ;    #locals, num_params, prolog bytes, #regs saved, use ebp, frame type (0 == FRAME_FPO)
        .FPO(      0,          3,            0,           0,       0,          0)

        ; [esp + 0] return address
        ; [esp + 4] dest
        ; [esp + 8] src
        ; [esp + c] count

        cmp         dword ptr [esp + 0Ch], 0        ; check for a zero-length copy
        jz          NothingToCopy

        mov         ecx, [esp + 4]  ; ecx <- dest
        mov         edx, [esp + 8]  ; edx <- src

        ; Now check the dest and src pointers.  If they AV, the EH subsystem will recognize the address of the AV,
        ; unwind the frame, and fixup the stack to make it look like the (managed) caller AV'ed, which will be
        ; translated to a managed exception as usual.
ALTERNATE_ENTRY RhpCopyMultibyteDestAVLocation
        cmp         byte ptr [ecx], 0
ALTERNATE_ENTRY RhpCopyMultibyteSrcAVLocation
        cmp         byte ptr [edx], 0

        ; tail-call to the GC-safe memcpy implementation
        ; NOTE: this is also a __cdecl function
        jmp         _memcpyGCRefs

NothingToCopy:
        mov         eax, [esp + 4]                  ; return dest
        ret

_RhpCopyMultibyte ENDP

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* __cdecl RhpCopyMultibyteWithWriteBarrier(void*, void*, size_t)
;;
;; The purpose of this wrapper is to hoist the potential null reference exceptions of copying memory up to a place where
;; the stack unwinder and exception dispatch can properly transform the exception into a managed exception and dispatch
;; it to managed code.
;; Runs a card table update via RhpBulkWriteBarrier after the copy
;;
_RhpCopyMultibyteWithWriteBarrier PROC PUBLIC

        ;    #locals, num_params, prolog bytes, #regs saved, use ebp, frame type (0 == FRAME_FPO)
        .FPO(      0,          3,            0,           0,       0,          0)

        ; [esp + 0] return address
        ; [esp + 4] dest
        ; [esp + 8] src
        ; [esp + c] count

        cmp         dword ptr [esp + 0Ch], 0        ; check for a zero-length copy
        jz          NothingToCopy

        mov         ecx, [esp + 4]  ; ecx <- dest
        mov         edx, [esp + 8]  ; edx <- src

        ; Now check the dest and src pointers.  If they AV, the EH subsystem will recognize the address of the AV,
        ; unwind the frame, and fixup the stack to make it look like the (managed) caller AV'ed, which will be
        ; translated to a managed exception as usual.
ALTERNATE_ENTRY RhpCopyMultibyteWithWriteBarrierDestAVLocation
        cmp         byte ptr [ecx], 0
ALTERNATE_ENTRY RhpCopyMultibyteWithWriteBarrierSrcAVLocation
        cmp         byte ptr [edx], 0

        ; tail-call to the GC-safe memcpy implementation
        ; NOTE: this is also a __cdecl function
        jmp         _memcpyGCRefsWithWriteBarrier

NothingToCopy:
        mov         eax, [esp + 4]                  ; return dest
        ret

_RhpCopyMultibyteWithWriteBarrier ENDP

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* __cdecl RhpCopyAnyWithWriteBarrier(void*, void*, size_t)
;;
;; The purpose of this wrapper is to hoist the potential null reference exceptions of copying memory up to a place where
;; the stack unwinder and exception dispatch can properly transform the exception into a managed exception and dispatch
;; it to managed code.
;; Runs a card table update via RhpBulkWriteBarrier after the copy if it contained GC pointers
;;
_RhpCopyAnyWithWriteBarrier PROC PUBLIC

        ;    #locals, num_params, prolog bytes, #regs saved, use ebp, frame type (0 == FRAME_FPO)
        .FPO(      0,          3,            0,           0,       0,          0)

        ; [esp + 0] return address
        ; [esp + 4] dest
        ; [esp + 8] src
        ; [esp + c] count

        cmp         dword ptr [esp + 0Ch], 0        ; check for a zero-length copy
        jz          NothingToCopy

        mov         ecx, [esp + 4]  ; ecx <- dest
        mov         edx, [esp + 8]  ; edx <- src

        ; Now check the dest and src pointers.  If they AV, the EH subsystem will recognize the address of the AV,
        ; unwind the frame, and fixup the stack to make it look like the (managed) caller AV'ed, which will be
        ; translated to a managed exception as usual.
ALTERNATE_ENTRY RhpCopyAnyWithWriteBarrierDestAVLocation
        cmp         byte ptr [ecx], 0
ALTERNATE_ENTRY RhpCopyAnyWithWriteBarrierSrcAVLocation
        cmp         byte ptr [edx], 0

        ; tail-call to the GC-safe memcpy implementation
        ; NOTE: this is also a __cdecl function
        jmp         _memcpyAnyWithWriteBarrier

NothingToCopy:
        mov         eax, [esp + 4]                  ; return dest
        ret

_RhpCopyAnyWithWriteBarrier ENDP

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; The following helper will access ("probe") a word on each page of the stack
; starting with the page right beneath esp down to the one pointed to by eax.
; The procedure is needed to make sure that the "guard" page is pushed down below the allocated stack frame.
; The call to the helper will be emitted by JIT in the function prolog when large (larger than 0x3000 bytes) stack frame is required.
;
; NOTE: this helper will modify a value of esp and must establish the frame pointer.
PAGE_SIZE equ 1000h

_RhpStackProbe PROC public
    ; On entry:
    ;   eax - the lowest address of the stack frame being allocated (i.e. [InitialSp - FrameSize])
    ;
    ; NOTE: this helper will probe at least one page below the one pointed by esp.
    push    ebp
    mov     ebp, esp

    and     esp, -PAGE_SIZE      ; esp points to the **lowest address** on the last probed page
                                 ; This is done to make the loop end condition simpler.
ProbeLoop:
    sub     esp, PAGE_SIZE       ; esp points to the lowest address of the **next page** to probe
    test    [esp], eax           ; esp points to the lowest address on the **last probed** page
    cmp     esp, eax
    jg      ProbeLoop            ; if esp > eax, then we need to probe at least one more page.

    mov     esp, ebp
    pop     ebp
    ret

_RhpStackProbe ENDP

end
