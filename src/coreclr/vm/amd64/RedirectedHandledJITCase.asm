; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ==++==
;

;
; ==--==

include AsmMacros.inc
include asmconstants.inc

Thread__GetAbortContext equ ?GetAbortContext@Thread@@QEAAPEAU_CONTEXT@@XZ

extern FixContextHandler:proc
extern LinkFrameAndThrow:proc
extern GetCurrentSavedRedirectContext:proc
extern Thread__GetAbortContext:proc
extern HijackHandler:proc
extern ThrowControlForThread:proc
extern FixRedirectContextHandler:proc

;
; WARNING!!  These functions immediately ruin thread unwindability.  This is
; WARNING!!  OK as long as there is a mechanism for saving the thread context
; WARNING!!  prior to running these functions as well as a mechanism for
; WARNING!!  restoring the context prior to any stackwalk.  This means that
; WARNING!!  we need to ensure that no GC can occur while the stack is
; WARNING!!  unwalkable.  This further means that we cannot allow any exception
; WARNING!!  to occure when the stack is unwalkable
;


; If you edit this macro, make sure you update GetCONTEXTFromRedirectedStubStackFrame.
; This function is used by both the personality routine and the debugger to retrieve the original CONTEXT.
GenerateRedirectedHandledJITCaseStub macro reason

extern ?RedirectedHandledJITCaseFor&reason&@Thread@@CAXXZ:proc

NESTED_ENTRY RedirectedHandledJITCaseFor&reason&_Stub, _TEXT, FixRedirectContextHandler

        ;
        ; To aid debugging, we'll fake a call to this function.  Allocate an
        ; extra stack slot that is hidden from the unwind info, where we can
        ; stuff the "return address".  If we wanted to preserve full
        ; unwindability, we would need to copy all preserved registers.
        ; Ordinarily, rbp is used for the frame pointer, so since we're only
        ; interested is debugability, we'll just handle that common case.
        ;

        push            rax                     ; where to stuff the fake return address
        push_nonvol_reg rbp                     ; save interrupted rbp for stack walk
        alloc_stack     28h                     ; CONTEXT*, callee scratch area
        set_frame       rbp, 0

.errnz REDIRECTSTUB_ESTABLISHER_OFFSET_RBP, REDIRECTSTUB_ESTABLISHER_OFFSET_RBP has changed - update asm stubs

        END_PROLOGUE

        ;
        ; Align rsp.  rsp must misaligned at entry to any C function.
        ;
        and             rsp, -16

        ;
        ; Save a copy of the redirect CONTEXT* in case an exception occurs.
        ; The personality routine will use this to restore unwindability for
        ; the exception dispatcher.
        ;
        call            GetCurrentSavedRedirectContext

        mov             [rbp+20h], rax
.errnz REDIRECTSTUB_RBP_OFFSET_CONTEXT - 20h, REDIRECTSTUB_RBP_OFFSET_CONTEXT has changed - update asm stubs

        ;
        ; Fetch the interrupted rip and save it as our return address.
        ;
        mov             rax, [rax + OFFSETOF__CONTEXT__Rip]
        mov             [rbp+30h], rax

        ;
        ; Call target, which will do whatever we needed to do in the context
        ; of the target thread, and will RtlRestoreContext when it is done.
        ;
        call            ?RedirectedHandledJITCaseFor&reason&@Thread@@CAXXZ

        int             3                       ; target shouldn't return.

; Put a label here to tell the debugger where the end of this function is.
PATCH_LABEL RedirectedHandledJITCaseFor&reason&_StubEnd

NESTED_END RedirectedHandledJITCaseFor&reason&_Stub, _TEXT

        endm


GenerateRedirectedHandledJITCaseStub GCThreadControl
GenerateRedirectedHandledJITCaseStub DbgThreadControl
GenerateRedirectedHandledJITCaseStub UserSuspend

ifdef _DEBUG
ifdef HAVE_GCCOVER
GenerateRedirectedHandledJITCaseStub GCStress
endif
endif


; scratch area; padding; GSCookie
OFFSET_OF_FRAME = SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES + 8 + SIZEOF_GSCookie

; force evaluation to avoid "expression is too complex errors"
SIZEOF__FaultingExceptionFrame = SIZEOF__FaultingExceptionFrame

GenerateRedirectedStubWithFrame macro STUB, FILTER, TARGET

altentry STUB&_RspAligned

NESTED_ENTRY STUB, _TEXT, FILTER

        ;
        ; IN: rcx: original IP before redirect
        ;

        mov             rdx, rsp

        ; This push of the return address must not be recorded in the unwind
        ; info.  After this push, unwinding will work.
        push            rcx

        test            rsp, 0fh
        jnz             STUB&_FixRsp

STUB&_RspAligned:

        ; Any stack operations hereafter must be recorded in the unwind info, but
        ; only nonvolatile register locations are needed.  Anything else is only
        ; a "sub rsp, 8" to the unwinder.

        ; m_ctx must be 16-byte aligned
.errnz (OFFSET_OF_FRAME + SIZEOF__FaultingExceptionFrame) MOD 16

        alloc_stack     OFFSET_OF_FRAME + SIZEOF__FaultingExceptionFrame

.errnz THROWSTUB_ESTABLISHER_OFFSET_FaultingExceptionFrame - OFFSET_OF_FRAME, THROWSTUB_ESTABLISHER_OFFSET_FaultingExceptionFrame has changed - update asm stubs

        END_PROLOGUE

        lea             rcx, [rsp + OFFSET_OF_FRAME]

        mov             dword ptr [rcx], 0                                                          ; Initialize vtbl (it is not strictly necessary)
        mov             dword ptr [rcx + OFFSETOF__FaultingExceptionFrame__m_fFilterExecuted], 0    ; Initialize BOOL for personality routine

        call            TARGET

        ; Target should not return.
        int             3

NESTED_END STUB, _TEXT

; This function is used by the stub above to adjust the stack alignment.  The
; stub can't conditionally push something on the stack because the unwind
; encodings have no way to express that.
;
; CONSIDER: we could move the frame pointer above the FaultingExceptionFrame,
; and detect the misalignment adjustment in
; GetFrameFromRedirectedStubStackFrame.  This is probably less code and more
; straightforward.
LEAF_ENTRY STUB&_FixRsp, _TEXT

        call            STUB&_RspAligned

        ; Target should not return.
        int             3

LEAF_END STUB&_FixRsp, _TEXT

        endm


REDIRECT_FOR_THROW_CONTROL_FRAME_SIZE = SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES + 8

NESTED_ENTRY RedirectForThrowControl2, _TEXT

        ; On entry
        ; rcx -> FaultingExceptionFrame
        ; rdx -> Original RSP

        alloc_stack     REDIRECT_FOR_THROW_CONTROL_FRAME_SIZE

        save_reg_postrsp    rcx, REDIRECT_FOR_THROW_CONTROL_FRAME_SIZE + 8h     ; FaultingExceptionFrame
        save_reg_postrsp    rdx, REDIRECT_FOR_THROW_CONTROL_FRAME_SIZE + 10h    ; Original RSP

        END_PROLOGUE

        ; Fetch rip from a CONTEXT, and store it as our return address.
        INLINE_GETTHREAD rcx

        call            Thread__GetAbortContext

        mov             rax, [rax + OFFSETOF__CONTEXT__Rip]
        mov             rdx, [rsp + REDIRECT_FOR_THROW_CONTROL_FRAME_SIZE + 10h] ; Original RSP
        mov             [rdx - 8], rax

        mov             rcx, [rsp + REDIRECT_FOR_THROW_CONTROL_FRAME_SIZE + 8h] ; FaultingExceptionFrame
        call            ThrowControlForThread

        ; ThrowControlForThread doesn't return.
        int             3

NESTED_END RedirectForThrowControl2, _TEXT

GenerateRedirectedStubWithFrame RedirectForThrowControl, HijackHandler, RedirectForThrowControl2


NAKED_THROW_HELPER_FRAME_SIZE = SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES + 8

NESTED_ENTRY NakedThrowHelper2, _TEXT

        ; On entry
        ; rcx -> FaultingExceptionFrame

        alloc_stack     NAKED_THROW_HELPER_FRAME_SIZE
        END_PROLOGUE

        call            LinkFrameAndThrow

        ; LinkFrameAndThrow doesn't return.
        int             3

NESTED_END NakedThrowHelper2, _TEXT

GenerateRedirectedStubWithFrame NakedThrowHelper, FixContextHandler, NakedThrowHelper2


ifdef FEATURE_SPECIAL_USER_MODE_APC

extern ?ApcActivationCallback@Thread@@CAX_K@Z:proc

; extern "C" void NTAPI ApcActivationCallbackStub(ULONG_PTR Parameter);
NESTED_ENTRY ApcActivationCallbackStub, _TEXT, FixRedirectContextHandler

        push_nonvol_reg rbp
        alloc_stack     30h ; padding for alignment, CONTEXT *, callee scratch area
        set_frame       rbp, 0
    .errnz REDIRECTSTUB_ESTABLISHER_OFFSET_RBP, REDIRECTSTUB_ESTABLISHER_OFFSET_RBP has changed - update asm stubs
        END_PROLOGUE

        ; Save the pointer to the interrupted context on the stack for the stack walker
        mov             rax, [rcx + OFFSETOF__APC_CALLBACK_DATA__ContextRecord]
        mov             [rbp + 20h], rax
    .errnz REDIRECTSTUB_RBP_OFFSET_CONTEXT - 20h, REDIRECTSTUB_RBP_OFFSET_CONTEXT has changed - update asm stubs

        call            ?ApcActivationCallback@Thread@@CAX_K@Z

        add             rsp, 30h
        pop             rbp
        ret

        ; Put a label here to tell the debugger where the end of this function is.
    PATCH_LABEL ApcActivationCallbackStubEnd

NESTED_END ApcActivationCallbackStub, _TEXT

endif ; FEATURE_SPECIAL_USER_MODE_APC


        end
