;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

EXTERN memcpy                       : PROC
EXTERN memcpyGCRefs                 : PROC
EXTERN memcpyGCRefsWithWriteBarrier : PROC
EXTERN memcpyAnyWithWriteBarrier    : PROC
EXTERN RhpGetThreadStaticBaseForTypeSlow : PROC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* RhpCopyMultibyteNoGCRefs(void*, void*, size_t)
;;
;; The purpose of this wrapper is to hoist the potential null reference exceptions of copying memory up to a place where
;; the stack unwinder and exception dispatch can properly transform the exception into a managed exception and dispatch
;; it to managed code.
;;
LEAF_ENTRY RhpCopyMultibyteNoGCRefs, _TEXT

        ; rcx       dest
        ; rdx       src
        ; r8        count

        test        r8, r8              ; check for a zero-length copy
        jz          NothingToCopy

        ; Now check the dest and src pointers.  If they AV, the EH subsystem will recognize the address of the AV,
        ; unwind the frame, and fixup the stack to make it look like the (managed) caller AV'ed, which will be
        ; translated to a managed exception as usual.
ALTERNATE_ENTRY RhpCopyMultibyteNoGCRefsDestAVLocation
        cmp         byte ptr [rcx], 0
ALTERNATE_ENTRY RhpCopyMultibyteNoGCRefsSrcAVLocation
        cmp         byte ptr [rdx], 0

        ; tail-call to plain-old-memcpy
        jmp         memcpy

NothingToCopy:
        mov         rax, rcx            ; return dest
        ret

LEAF_END RhpCopyMultibyteNoGCRefs, _TEXT

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* RhpCopyMultibyte(void*, void*, size_t)
;;
;; The purpose of this wrapper is to hoist the potential null reference exceptions of copying memory up to a place where
;; the stack unwinder and exception dispatch can properly transform the exception into a managed exception and dispatch
;; it to managed code.
;;
LEAF_ENTRY RhpCopyMultibyte, _TEXT

        ; rcx       dest
        ; rdx       src
        ; r8        count

        test        r8, r8              ; check for a zero-length copy
        jz          NothingToCopy

        ; Now check the dest and src pointers.  If they AV, the EH subsystem will recognize the address of the AV,
        ; unwind the frame, and fixup the stack to make it look like the (managed) caller AV'ed, which will be
        ; translated to a managed exception as usual.
ALTERNATE_ENTRY RhpCopyMultibyteDestAVLocation
        cmp         byte ptr [rcx], 0
ALTERNATE_ENTRY RhpCopyMultibyteSrcAVLocation
        cmp         byte ptr [rdx], 0

        ; tail-call to the GC-safe memcpy implementation
        jmp         memcpyGCRefs

NothingToCopy:
        mov         rax, rcx            ; return dest
        ret

LEAF_END RhpCopyMultibyte, _TEXT

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* RhpCopyMultibyteWithWriteBarrier(void*, void*, size_t)
;;
;; The purpose of this wrapper is to hoist the potential null reference exceptions of copying memory up to a place where
;; the stack unwinder and exception dispatch can properly transform the exception into a managed exception and dispatch
;; it to managed code.
;; Runs a card table update via RhpBulkWriteBarrier after the copy
;;
LEAF_ENTRY RhpCopyMultibyteWithWriteBarrier, _TEXT

        ; rcx       dest
        ; rdx       src
        ; r8        count

        test        r8, r8              ; check for a zero-length copy
        jz          NothingToCopy

        ; Now check the dest and src pointers.  If they AV, the EH subsystem will recognize the address of the AV,
        ; unwind the frame, and fixup the stack to make it look like the (managed) caller AV'ed, which will be
        ; translated to a managed exception as usual.
ALTERNATE_ENTRY RhpCopyMultibyteWithWriteBarrierDestAVLocation
        cmp         byte ptr [rcx], 0
ALTERNATE_ENTRY RhpCopyMultibyteWithWriteBarrierSrcAVLocation
        cmp         byte ptr [rdx], 0

        ; tail-call to the GC-safe memcpy implementation
        jmp         memcpyGCRefsWithWriteBarrier

NothingToCopy:
        mov         rax, rcx            ; return dest
        ret

LEAF_END RhpCopyMultibyteWithWriteBarrier, _TEXT

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* RhpCopyAnyWithWriteBarrier(void*, void*, size_t)
;;
;; The purpose of this wrapper is to hoist the potential null reference exceptions of copying memory up to a place where
;; the stack unwinder and exception dispatch can properly transform the exception into a managed exception and dispatch
;; it to managed code.
;; Runs a card table update via RhpBulkWriteBarrier after the copy if the copy may contain GC pointers
;;
LEAF_ENTRY RhpCopyAnyWithWriteBarrier, _TEXT

        ; rcx       dest
        ; rdx       src
        ; r8        count

        test        r8, r8              ; check for a zero-length copy
        jz          NothingToCopy

        ; Now check the dest and src pointers.  If they AV, the EH subsystem will recognize the address of the AV,
        ; unwind the frame, and fixup the stack to make it look like the (managed) caller AV'ed, which will be
        ; translated to a managed exception as usual.
ALTERNATE_ENTRY RhpCopyAnyWithWriteBarrierDestAVLocation
        cmp         byte ptr [rcx], 0
ALTERNATE_ENTRY RhpCopyAnyWithWriteBarrierSrcAVLocation
        cmp         byte ptr [rdx], 0

        ; tail-call to the GC-safe memcpy implementation
        jmp         memcpyAnyWithWriteBarrier

NothingToCopy:
        mov         rax, rcx            ; return dest
        ret

LEAF_END RhpCopyAnyWithWriteBarrier, _TEXT

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

LEAF_ENTRY RhpGetThreadStaticBaseForType, _TEXT
        ; On entry and thorough the procedure:
        ;   rcx - TypeManagerSlot*
        ;   rdx - type index
        ; On exit:
        ;   rax - the thread static base for the given type

        ;; rax = GetThread(), TRASHES r8
        INLINE_GETTHREAD rax, r8

        mov     r8d, [rcx + 8]         ; Get ModuleIndex out of the TypeManagerSlot
        cmp     r8d, [rax + OFFSETOF__Thread__m_numThreadLocalModuleStatics]
        jae     RhpGetThreadStaticBaseForType_RarePath

        mov     r9, [rax + OFFSETOF__Thread__m_pThreadLocalModuleStatics]
        mov     rax, [r9 + r8 * 8]     ; Index into the array of modules
        test    rax, rax
        jz      RhpGetThreadStaticBaseForType_RarePath

        mov     r8, [rax]              ; Get the managed array from the handle
        cmp     edx, [r8 + OFFSETOF__Array__m_Length]
        jae     RhpGetThreadStaticBaseForType_RarePath
        mov     rax, [r8 + rdx * 8 + 10h]

        test    rax, rax
        jz      RhpGetThreadStaticBaseForType_RarePath

        ret

RhpGetThreadStaticBaseForType_RarePath:
        ;; We kept the arguments in their appropriate registers
        ;; and we can tailcall right away.
        jmp     RhpGetThreadStaticBaseForTypeSlow

LEAF_END RhpGetThreadStaticBaseForType, _TEXT

end
