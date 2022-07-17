; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

ifdef FEATURE_COMINTEROP

include AsmMacros.inc
include asmconstants.inc

; extern "C" const BYTE* ComPreStubWorker(ComPrestubMethodFrame *pPFrame, UINT64 *pErrorResult)
extern ComPreStubWorker:proc
extern JIT_FailFast:proc
extern s_gsCookie:qword


; extern "C" VOID ComCallPreStub()
NESTED_ENTRY ComCallPreStub, _TEXT

;
; Stack layout:
;
; (stack parameters)
; ...
; r9
; r8
; rdx
; rcx
; ComPrestubMethodFrame::m_ReturnAddress
; ComPrestubMethodFrame::m_pFuncDesc
; Frame::m_Next
; __VFN_table                                   <-- rsp + ComCallPreStub_ComPrestubMethodFrame_OFFSET
; gsCookie
; HRESULT                                       <-- rsp + ComCallPreStub_HRESULT_OFFSET
; (optional padding to qword align xmm save area)
; xmm3
; xmm2
; xmm1
; xmm0                                          <-- rsp + ComCallPreStub_XMM_SAVE_OFFSET
; callee's r9
; callee's r8
; callee's rdx
; callee's rcx

ComCallPreStub_STACK_FRAME_SIZE = 0

; ComPrestubMethodFrame MUST be the highest part of the stack frame,
; immediately below the return address, so that
; ComPrestubMethodFrame::m_ReturnAddress and m_FuncDesc are in the right place.
ComCallPreStub_STACK_FRAME_SIZE = ComCallPreStub_STACK_FRAME_SIZE + SIZEOF__ComPrestubMethodFrame - 8
ComCallPreStub_ComPrestubMethodFrame_NEGOFFSET = ComCallPreStub_STACK_FRAME_SIZE

; CalleeSavedRegisters MUST be immediately below ComPrestubMethodFrame
ComCallPreStub_STACK_FRAME_SIZE = ComCallPreStub_STACK_FRAME_SIZE + 8*8
ComCallPreStub_CalleeSavedRegisters_NEGOFFSET = ComCallPreStub_STACK_FRAME_SIZE

; GSCookie MUST be immediately below CalleeSavedRegisters
ComCallPreStub_STACK_FRAME_SIZE = ComCallPreStub_STACK_FRAME_SIZE + SIZEOF_GSCookie

; UINT64 (out param to ComPreStubWorker)
ComCallPreStub_STACK_FRAME_SIZE = ComCallPreStub_STACK_FRAME_SIZE + 8
ComCallPreStub_ERRORRETVAL_NEGOFFSET = ComCallPreStub_STACK_FRAME_SIZE

; Ensure that the offset of the XMM save area will be 16-byte aligned.
if ((ComCallPreStub_STACK_FRAME_SIZE + SIZEOF__Frame + 8) mod 16) ne 0
ComCallPreStub_STACK_FRAME_SIZE = ComCallPreStub_STACK_FRAME_SIZE + 8
endif

; FP parameters (xmm0-xmm3)
ComCallPreStub_STACK_FRAME_SIZE = ComCallPreStub_STACK_FRAME_SIZE + 40h
ComCallPreStub_XMM_SAVE_NEGOFFSET = ComCallPreStub_STACK_FRAME_SIZE

; Callee scratch area
ComCallPreStub_STACK_FRAME_SIZE = ComCallPreStub_STACK_FRAME_SIZE + 20h

; Now we have the full size of the stack frame.  The offsets have been computed relative to the
; top, so negate them to make them relative to the post-prologue rsp.
ComCallPreStub_ComPrestubMethodFrame_OFFSET = ComCallPreStub_STACK_FRAME_SIZE - ComCallPreStub_ComPrestubMethodFrame_NEGOFFSET
OFFSETOF_GSCookie                           = ComCallPreStub_ComPrestubMethodFrame_OFFSET - SIZEOF_GSCookie
ComCallPreStub_ERRORRETVAL_OFFSET           = ComCallPreStub_STACK_FRAME_SIZE - ComCallPreStub_ERRORRETVAL_NEGOFFSET
ComCallPreStub_XMM_SAVE_OFFSET              = ComCallPreStub_STACK_FRAME_SIZE - ComCallPreStub_XMM_SAVE_NEGOFFSET

        .allocstack     8                       ; ComPrestubMethodFrame::m_pFuncDesc, pushed by prepad

        alloc_stack     SIZEOF__ComPrestubMethodFrame - 2*8

        ;
        ; Save ComPrestubMethodFrame* to pass to ComPreStubWorker
        ;
        mov             r10, rsp

        ;
        ; Allocate callee scratch area and save FP parameters
        ;
        alloc_stack     ComCallPreStub_ComPrestubMethodFrame_OFFSET

        ;
        ; Save argument registers
        ;
        SAVE_ARGUMENT_REGISTERS ComCallPreStub_STACK_FRAME_SIZE + 8h

        ;
        ; spill the fp args
        ;
        SAVE_FLOAT_ARGUMENT_REGISTERS ComCallPreStub_XMM_SAVE_OFFSET

        END_PROLOGUE

        mov             rcx, s_gsCookie
        mov             [rsp + OFFSETOF_GSCookie], rcx
        ;
        ; Resolve target.
        ;
        mov             rcx, r10
        lea             rdx, [rsp + ComCallPreStub_ERRORRETVAL_OFFSET]
        call            ComPreStubWorker
        test            rax, rax
        jz              ExitError

ifdef _DEBUG
        mov             rcx, s_gsCookie
        cmp             [rsp + OFFSETOF_GSCookie], rcx
        je              GoodGSCookie
        call            JIT_FailFast
GoodGSCookie:
endif ; _DEBUG

        ;
        ; Restore FP parameters
        ;
        RESTORE_FLOAT_ARGUMENT_REGISTERS ComCallPreStub_XMM_SAVE_OFFSET

        ;
        ; Restore integer parameters
        ;
        RESTORE_ARGUMENT_REGISTERS ComCallPreStub_STACK_FRAME_SIZE + 8h

        add             rsp, ComCallPreStub_ComPrestubMethodFrame_OFFSET + SIZEOF__ComPrestubMethodFrame - 8

        TAILJMP_RAX

ExitError:
        mov             rax, [rsp + ComCallPreStub_ERRORRETVAL_OFFSET]
        add             rsp, ComCallPreStub_ComPrestubMethodFrame_OFFSET + SIZEOF__ComPrestubMethodFrame - 8

        ret

NESTED_END ComCallPreStub, _TEXT

endif ; FEATURE_COMINTEROP

        end

