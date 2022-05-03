; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: RedirectedHandledJITCase.asm
;
; ***********************************************************************
;

; This contains thread-redirecting helper routines that are 100% x86 assembly

        .586
        .model  flat

        include asmconstants.inc

        option  casemap:none
        .code

EXTERN _GetCurrentSavedRedirectContext@0:PROC

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
GenerateRedirectedHandledJITCaseStub MACRO reason

EXTERN ?RedirectedHandledJITCaseFor&reason&@Thread@@CGXXZ:proc

        ALIGN 4
_RedirectedHandledJITCaseFor&reason&_Stub@0 PROC PUBLIC

        push            eax                     ; where to stuff the fake return address
        push            ebp                     ; save interrupted ebp for stack walk
        mov             ebp, esp
        sub             esp, 4                  ; stack slot to save the CONTEXT *

        ;
        ; Save a copy of the redirect CONTEXT*.
        ; This is needed for the debugger to unwind the stack.
        ;
        call            _GetCurrentSavedRedirectContext@0

        mov             [ebp-4], eax
.errnz REDIRECTSTUB_EBP_OFFSET_CONTEXT + 4, REDIRECTSTUB_EBP_OFFSET_CONTEXT has changed - update asm stubs

        ;
        ; Fetch the interrupted eip and save it as our return address.
        ;
        mov             eax, [eax + CONTEXT_Eip]
        mov             [ebp+4], eax

        ;
        ; Call target, which will do whatever we needed to do in the context
        ; of the target thread, and will RtlRestoreContext when it is done.
        ;
        call            ?RedirectedHandledJITCaseFor&reason&@Thread@@CGXXZ

        int             3                       ; target shouldn't return.

; Put a label here to tell the debugger where the end of this function is.
PUBLIC _RedirectedHandledJITCaseFor&reason&_StubEnd@0
_RedirectedHandledJITCaseFor&reason&_StubEnd@0:

_RedirectedHandledJITCaseFor&reason&_Stub@0 ENDP

ENDM

; HijackFunctionStart and HijackFunctionEnd are used to tell BBT to keep the hijacking functions together.
; Debugger uses range to check whether IP falls into one of them (see code:Debugger::s_hijackFunction).

_HijackFunctionStart@0 proc public
ret
_HijackFunctionStart@0 endp

GenerateRedirectedHandledJITCaseStub <GCThreadControl>
GenerateRedirectedHandledJITCaseStub <DbgThreadControl>
GenerateRedirectedHandledJITCaseStub <UserSuspend>

; Hijack for exceptions.
; This can be used to hijack at a 2nd-chance exception and execute the UEF

EXTERN _ExceptionHijackWorker@16:PROC

_ExceptionHijack@0 PROC PUBLIC

    ; This is where we land when we're hijacked from an IP by the debugger.
    ; The debugger has already pushed the args:
    ; - a CONTEXT
    ; - an EXCEPTION_RECORD onto the stack
    ; - an DWORD to use to mulitplex the hijack
    ; - an arbitrary void* data parameter
    call _ExceptionHijackWorker@16

    ; Don't expect to return from here. Debugger will unhijack us. It has the full
    ; context and can properly restore us.
    int 3

; Put a label here to tell the debugger where the end of this function is.
public _ExceptionHijackEnd@0
_ExceptionHijackEnd@0:

_ExceptionHijack@0 ENDP

; It is very important to have a dummy function here.
; Without it, the image has two labels without any instruction in between:
; One for the last label in this function, and one for the first function in the image following this asm file.
; Then the linker is free to remove from PDB the function symbol for the function
; immediately following this, and replace the reference with the last label in this file.
; When this happens, BBT loses info about function, moves pieces within the function to random place, and generates bad code.
_HijackFunctionLast@0 proc public
ret
_HijackFunctionLast@0 endp

; This is the first function outside the "keep together range". Used by BBT scripts.
_HijackFunctionEnd@0 proc public
ret
_HijackFunctionEnd@0 endp

END
