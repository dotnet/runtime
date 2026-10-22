; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; The LXR write barrier for Windows AMD64. See lxr_writebarrier.S for the full description; this file is the same
; barrier in MASM. The runtime installs it through IGCHeap::GetAssignRefFunctions as CORINFO_HELP_ASSIGN_REF and
; CORINFO_HELP_CHECKED_ASSIGN_REF (the "specialized" barrier ABI).
;
; Register contract of the helpers (see RBM_CALLEE_TRASH_WRITEBARRIER in jit/targetamd64.h):
;   in:     rcx = destination slot, rdx = new reference
;   killed: rax, rcx, rdx, r8, r9, r10, r11 and the flags
;   kept:   every other register, and ALL vector state (xmm0-15, ymm upper halves, zmm0-31, k0-7, mxcsr)
; The slow path calls C++ code. Windows keeps only the low halves of xmm6-15 across a call, and the compiler may use
; any vector register, so the slow path saves the whole vector state with xsave (fxsave if the OS does not enable
; xsave) and restores it after the call.
;
; Test controls (the native barrier test builds this file with one of these defines and must see a failure):
;   LXR_WB_CONTROL_NO_VECTOR_SAVE       do not save and restore vector state
;   LXR_WB_CONTROL_MISALIGN             call the C++ function with a misaligned stack
;   LXR_WB_CONTROL_NO_STORE_AFTER_SLOW  forget the store after the slow path

include AsmConstants.inc
include AsmMacros.inc

; Size of the xsave area. The mask 0xE7 (x87, SSE, AVX, opmask, ZMM_Hi256, Hi16_ZMM) needs 2688 bytes in the
; standard format. The frame has room for the 64-byte alignment too.
LXR_XSAVE_FRAME_SIZE    equ 2816
LXR_XSAVE_HEADER_OFFSET equ 512

;
;   rcx - destination address (any address; outside the LXR heap the helper only stores)
;   rdx - new reference
;
        LEAF_ENTRY LxrCheckedAssignRef, _TEXT
        mov     rax, rcx
        sub     rax, qword ptr [HeapLowSlot]
        cmp     rax, qword ptr [HeapSizeSlot]
        jb      LxrAssignRefEntry
        ALTERNATE_ENTRY LxrCheckedAssignRefAVLocation
        mov     [rcx], rdx
        ret
        LEAF_END_MARKED LxrCheckedAssignRef, _TEXT

;
;   rcx - destination address, must be inside the LXR heap
;   rdx - new reference
;
        LEAF_ENTRY LxrAssignRef, _TEXT
        ALTERNATE_ENTRY LxrAssignRefEntry
        ; metadata byte = base[dst >> 6], bit = (dst >> 3) & 7 (granularity: one bit for every 8 bytes)
        mov     rax, rcx
        shr     rax, 6
        mov     r8, qword ptr [MetadataBaseSlot]
        ALTERNATE_ENTRY LxrAssignRefMetadataAVLocation
        movzx   eax, byte ptr [rax + r8]
        mov     r9d, ecx
        shr     r9d, 3
        and     r9d, 7
        bt      eax, r9d
        jc      LxrBarrierSlow
        ALTERNATE_ENTRY LxrAssignRefAVLocation
        mov     [rcx], rdx
        ret
        LEAF_END_MARKED LxrAssignRef, _TEXT

;
; Slow path. Entered by a jump from the fast path, so the stack looks as if LxrAssignRef was just called.
; A nested function with unwind information. The runtime unwinds copied code through the original address.
;
        NESTED_ENTRY LxrBarrierSlow, _TEXT
        push_nonvol_reg rbp
        set_frame rbp, 0
        END_PROLOGUE
        ; keep the arguments: the call below kills rcx and rdx
        push    rcx
        push    rdx
        sub     rsp, LXR_XSAVE_FRAME_SIZE
        and     rsp, -64

ifndef LXR_WB_CONTROL_NO_VECTOR_SAVE
        ; the header of the xsave area must be zero before xrstor sees it
        xor     eax, eax
        mov     qword ptr [rsp + LXR_XSAVE_HEADER_OFFSET + 0], rax
        mov     qword ptr [rsp + LXR_XSAVE_HEADER_OFFSET + 8], rax
        mov     qword ptr [rsp + LXR_XSAVE_HEADER_OFFSET + 16], rax
        mov     qword ptr [rsp + LXR_XSAVE_HEADER_OFFSET + 24], rax
        mov     qword ptr [rsp + LXR_XSAVE_HEADER_OFFSET + 32], rax
        mov     qword ptr [rsp + LXR_XSAVE_HEADER_OFFSET + 40], rax
        mov     qword ptr [rsp + LXR_XSAVE_HEADER_OFFSET + 48], rax
        mov     qword ptr [rsp + LXR_XSAVE_HEADER_OFFSET + 56], rax

        mov     rax, qword ptr [XsaveMaskSlot]
        test    rax, rax
        jz      SaveWithFxsave
        mov     rdx, rax
        shr     rdx, 32
        xsave   [rsp]
        ; C++ code uses legacy SSE encoding. Clear the upper halves, so that it does not pay for mixing with the
        ; caller's AVX state; xrstor puts them back.
        test    al, 4
        jz      CallManaged
        vzeroupper
        jmp     CallManaged
SaveWithFxsave:
        fxsave  [rsp]
endif

CallManaged:
        ; shadow space for the call
        sub     rsp, 32
ifdef LXR_WB_CONTROL_MISALIGN
        sub     rsp, 8
endif
        ; rcx still holds the destination: LxrBarrierSlowPath(Object** destination)
        call    qword ptr [SlowPathSlot]
ifdef LXR_WB_CONTROL_MISALIGN
        add     rsp, 8
endif
        add     rsp, 32

ifndef LXR_WB_CONTROL_NO_VECTOR_SAVE
        mov     rax, qword ptr [XsaveMaskSlot]
        test    rax, rax
        jz      RestoreWithFxrstor
        mov     rdx, rax
        shr     rdx, 32
        xrstor  [rsp]
        jmp     Restored
RestoreWithFxrstor:
        fxrstor [rsp]
Restored:
endif

        lea     rsp, [rbp - 16]
        pop     rdx
        pop     rcx
        pop     rbp
        ALTERNATE_ENTRY LxrBarrierSlowAVLocation
ifndef LXR_WB_CONTROL_NO_STORE_AFTER_SLOW
        mov     [rcx], rdx
endif
        ret
        NESTED_END_MARKED LxrBarrierSlow, _TEXT

;
; Patchable slots. Each one is 8-byte aligned, so the runtime updates it with one store. The runtime copies the
; value of a global (named in GetAssignRefFunctions) into the slot of the copied block.
;
_TEXT   segment para 'CODE'
        align   8
        PATCH_LABEL LxrPatchable_MetadataBase
MetadataBaseSlot dq 0
        PATCH_LABEL LxrPatchable_HeapLow
HeapLowSlot     dq 0
        PATCH_LABEL LxrPatchable_HeapSize
HeapSizeSlot    dq 0
        PATCH_LABEL LxrPatchable_SlowPath
SlowPathSlot    dq 0
        PATCH_LABEL LxrPatchable_XsaveMask
XsaveMaskSlot   dq 0

        public  LxrWriteBarrierBlock_End
LxrWriteBarrierBlock_End label qword
        nop
_TEXT   ends

        end
