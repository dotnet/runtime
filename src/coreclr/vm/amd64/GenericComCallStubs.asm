; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

ifdef FEATURE_COMINTEROP

include AsmMacros.inc
include asmconstants.inc

extern CallDescrWorkerUnwindFrameChainHandler:proc
extern ReverseComUnwindFrameChainHandler:proc
extern COMToCLRWorker:proc
extern JIT_FailFast:proc
extern s_gsCookie:qword


NESTED_ENTRY GenericComCallStub, _TEXT, ReverseComUnwindFrameChainHandler

;
; Set up a ComMethodFrame and call COMToCLRWorker.
;
; Stack frame layout:
;
; (stack parameters)
; ...
; r9
; r8
; rdx
; rcx
; UnmanagedToManagedFrame::m_ReturnAddress
; UnmanagedToManagedFrame::m_Datum
; Frame::m_Next
; __VFN_table                                   <-- rsp + GenericComCallStub_ComMethodFrame_OFFSET
; GSCookie
; (optional padding to qword align xmm save area)
; xmm3
; xmm2
; xmm1
; xmm0                                          <-- rsp + GenericComCallStub_XMM_SAVE_OFFSET
; r12
; r13
; r14
; (optional padding to qword align rsp)
; callee's r9
; callee's r8
; callee's rdx
; callee's rcx

GenericComCallStub_STACK_FRAME_SIZE = 0

; ComMethodFrame MUST be the highest part of the stack frame, immediately
; below the return address and MethodDesc*, so that
; UnmanagedToManagedFrame::m_ReturnAddress and
; UnmanagedToManagedFrame::m_Datum are the right place.
GenericComCallStub_STACK_FRAME_SIZE = GenericComCallStub_STACK_FRAME_SIZE + (SIZEOF__ComMethodFrame - 8)
GenericComCallStub_ComMethodFrame_NEGOFFSET = GenericComCallStub_STACK_FRAME_SIZE

GenericComCallStub_STACK_FRAME_SIZE = GenericComCallStub_STACK_FRAME_SIZE + SIZEOF_GSCookie

; Ensure that the offset of the XMM save area will be 16-byte aligned.
if ((GenericComCallStub_STACK_FRAME_SIZE + 8) MOD 16) ne 0
GenericComCallStub_STACK_FRAME_SIZE = GenericComCallStub_STACK_FRAME_SIZE + 8
endif

; XMM save area MUST be immediately below GenericComCallStub
; (w/ alignment padding)
GenericComCallStub_STACK_FRAME_SIZE = GenericComCallStub_STACK_FRAME_SIZE + 4*16
GenericComCallStub_XMM_SAVE_NEGOFFSET = GenericComCallStub_STACK_FRAME_SIZE

; Add in the callee scratch area size.
GenericComCallStub_CALLEE_SCRATCH_SIZE = 4*8
GenericComCallStub_STACK_FRAME_SIZE = GenericComCallStub_STACK_FRAME_SIZE + GenericComCallStub_CALLEE_SCRATCH_SIZE

; Now we have the full size of the stack frame.  The offsets have been computed relative to the
; top, so negate them to make them relative to the post-prologue rsp.
GenericComCallStub_ComMethodFrame_OFFSET = GenericComCallStub_STACK_FRAME_SIZE - GenericComCallStub_ComMethodFrame_NEGOFFSET
GenericComCallStub_XMM_SAVE_OFFSET = GenericComCallStub_STACK_FRAME_SIZE - GenericComCallStub_XMM_SAVE_NEGOFFSET
OFFSETOF_GSCookie = GenericComCallStub_ComMethodFrame_OFFSET - SIZEOF_GSCookie

        .allocstack     8                       ; UnmanagedToManagedFrame::m_Datum, pushed by prepad

        ;
        ; Allocate the remainder of the ComMethodFrame.  The fields
        ; will be filled in by COMToCLRWorker
        ;
        alloc_stack     SIZEOF__ComMethodFrame - 10h

        ;
        ; Save ComMethodFrame* to pass to COMToCLRWorker
        ;
        mov             r10, rsp

        alloc_stack     GenericComCallStub_ComMethodFrame_OFFSET

        ;
        ; Save argument registers
        ;
        SAVE_ARGUMENT_REGISTERS GenericComCallStub_STACK_FRAME_SIZE + 8h

        ;
        ; spill the fp args
        ;
        SAVE_FLOAT_ARGUMENT_REGISTERS GenericComCallStub_XMM_SAVE_OFFSET

        END_PROLOGUE

        mov            rcx, s_gsCookie
        mov            [rsp + OFFSETOF_GSCookie], rcx

        ;
        ; Call COMToCLRWorker.  Note that the first parameter (pThread) is
        ; filled in by callee.
        ;

ifdef _DEBUG
        mov             rcx, 0cccccccccccccccch
endif
        mov             rdx, r10
        call            COMToCLRWorker

ifdef _DEBUG
        mov             rcx, s_gsCookie
        cmp             [rsp + OFFSETOF_GSCookie], rcx
        je              GoodGSCookie
        call            JIT_FailFast
GoodGSCookie:
endif ; _DEBUG

        ;
        ; epilogue
        ;
        add             rsp, GenericComCallStub_STACK_FRAME_SIZE
        ret

NESTED_END GenericComCallStub, _TEXT


; ARG_SLOT COMToCLRDispatchHelperWithStack(DWORD  dwStackSlots,    // rcx
;                                          ComMethodFrame *pFrame, // rdx
;                                          PCODE  pTarget,         // r8
;                                          PCODE  pSecretArg,      // r9
;                                          INT_PTR pDangerousThis  // rbp+40h
;                                          );
NESTED_ENTRY COMToCLRDispatchHelperWithStack, _TEXT, CallDescrWorkerUnwindFrameChainHandler

ComMethodFrame_Arguments_OFFSET = SIZEOF__ComMethodFrame
ComMethodFrame_XMM_SAVE_OFFSET = GenericComCallStub_XMM_SAVE_OFFSET - GenericComCallStub_ComMethodFrame_OFFSET

        push_nonvol_reg rdi             ; save nonvolatile registers
        push_nonvol_reg rsi             ;
        push_nonvol_reg rbp             ;
        set_frame rbp, 0                ; set frame pointer

        END_PROLOGUE


        ;
        ; copy stack
        ;
        lea     rsi, [rdx + ComMethodFrame_Arguments_OFFSET]
        add     ecx, 4                  ; outgoing argument homes
        mov     eax, ecx                ; number of stack slots
        shl     eax, 3                  ; compute number of argument bytes
        add     eax, 8h                 ; alignment padding
        and     rax, 0FFFFFFFFFFFFFFf0h ; for proper stack alignment, v-liti remove partial register stall
        sub     rsp, rax                ; allocate argument list
        mov     rdi, rsp                ; set destination argument list address
        rep movsq                       ; copy arguments to the stack


        ; Stack layout:
        ;
        ; callee's rcx (to be loaded into rcx) <- rbp+40h
        ; r9           (to be loaded into r10)
        ; r8           (IL stub entry point)
        ; rdx          (ComMethodFrame ptr)
        ; rcx          (number of stack slots to repush)
        ; return address
        ; saved rdi
        ; saved rsi
        ; saved rbp                            <- rbp
        ; alignment
        ; (stack parameters)
        ; callee's r9
        ; callee's r8
        ; callee's rdx
        ; callee's rcx (not loaded into rcx)   <- rsp

        ;
        ; load fp registers
        ;
        movdqa  xmm0, [rdx + ComMethodFrame_XMM_SAVE_OFFSET + 00h]
        movdqa  xmm1, [rdx + ComMethodFrame_XMM_SAVE_OFFSET + 10h]
        movdqa  xmm2, [rdx + ComMethodFrame_XMM_SAVE_OFFSET + 20h]
        movdqa  xmm3, [rdx + ComMethodFrame_XMM_SAVE_OFFSET + 30h]

        ;
        ; load secret arg and target
        ;
        mov     r10, r9
        mov     rax, r8

        ;
        ; load argument registers
        ;
        mov     rcx, [rbp + 40h]        ; ignoring the COM IP at [rsp]
        mov     rdx, [rsp + 08h]
        mov     r8,  [rsp + 10h]
        mov     r9,  [rsp + 18h]

        ;
        ; call the target
        ;
        call    rax

        ; It is important to have an instruction between the previous call and the epilog.
        ; If the return address is in epilog, OS won't call personality routine because
        ; it thinks personality routine does not help in this case.
        nop

        ;
        ; epilog
        ;
        lea     rsp, 0[rbp]             ; deallocate argument list
        pop     rbp                     ; restore nonvolatile register
        pop     rsi                     ;
        pop     rdi                     ;
        ret

NESTED_END COMToCLRDispatchHelperWithStack, _TEXT

; ARG_SLOT COMToCLRDispatchHelper(DWORD  dwStackSlots,    // rcx
;                                 ComMethodFrame *pFrame, // rdx
;                                 PCODE  pTarget,         // r8
;                                 PCODE  pSecretArg,      // r9
;                                 INT_PTR pDangerousThis  // rsp + 28h on entry
;                                 );
NESTED_ENTRY COMToCLRDispatchHelper, _TEXT, CallDescrWorkerUnwindFrameChainHandler

        ;
        ; Check to see if we have stack to copy and, if so, tail call to
        ; the routine that can handle that.
        ;
        test    ecx, ecx
        jnz     COMToCLRDispatchHelperWithStack

        alloc_stack     28h     ; alloc scratch space + alignment,   pDangerousThis moves to [rsp+50]
        END_PROLOGUE


        ; get pointer to arguments
        lea     r11, [rdx + ComMethodFrame_Arguments_OFFSET]

        ;
        ; load fp registers
        ;
        movdqa  xmm0, [rdx + ComMethodFrame_XMM_SAVE_OFFSET + 00h]
        movdqa  xmm1, [rdx + ComMethodFrame_XMM_SAVE_OFFSET + 10h]
        movdqa  xmm2, [rdx + ComMethodFrame_XMM_SAVE_OFFSET + 20h]
        movdqa  xmm3, [rdx + ComMethodFrame_XMM_SAVE_OFFSET + 30h]

        ;
        ; load secret arg and target
        ;
        mov     r10, r9
        mov     rax, r8

        ;
        ; load argument registers
        ;
        mov     rcx, [rsp + 50h]        ; ignoring the COM IP at [r11 + 00h]
        mov     rdx, [r11 + 08h]
        mov     r8,  [r11 + 10h]
        mov     r9,  [r11 + 18h]

        ;
        ; call the target
        ;
        call    rax

        ; It is important to have an instruction between the previous call and the epilog.
        ; If the return address is in epilog, OS won't call personality routine because
        ; it thinks personality routine does not help in this case.
        nop

        ;
        ; epilog
        ;
        add     rsp, 28h
        ret
NESTED_END COMToCLRDispatchHelper, _TEXT



endif ; FEATURE_COMINTEROP

        end

