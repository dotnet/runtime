; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;  *** NOTE:  If you make changes to this file, propagate the changes to
;             dbghelpers.s in this directory

	.586
	.model	flat
        .code

        extern _FuncEvalHijackWorker@4:PROC

; @dbgtodo- once we port Funceval, use the ExceptionHijack stub instead of this func-eval stub.
;
; This is the method that we hijack a thread running managed code. It calls
; FuncEvalHijackWorker, which actually performs the func eval, then jumps to
; the patch address so we can complete the cleanup.
;
; Note: the parameter is passed in eax - see Debugger::FuncEvalSetup for
;       details
;
_FuncEvalHijack@0 proc public
        push eax       ; the ptr to the DebuggerEval
        call _FuncEvalHijackWorker@4
        jmp  eax       ; return is the patch address to jmp to
_FuncEvalHijack@0 endp



;
; Flares for interop debugging.
; Flares are exceptions (breakpoints) at well known addresses which the RS
; listens for when interop debugging.
;

; This exception is from managed code.
_SignalHijackStartedFlare@0 proc public
        int 3
        ; make sure that the basic block is unique
        test eax,1
        ret
_SignalHijackStartedFlare@0 endp

; Start the handoff
_ExceptionForRuntimeHandoffStartFlare@0 proc public
        int 3
        ; make sure that the basic block is unique
        test eax,2
        ret
_ExceptionForRuntimeHandoffStartFlare@0 endp

; Finish the handoff.
_ExceptionForRuntimeHandoffCompleteFlare@0 proc public
        int 3
        ; make sure that the basic block is unique
        test eax,3
        ret
_ExceptionForRuntimeHandoffCompleteFlare@0 endp

; Return thread to pre-hijacked context.
_SignalHijackCompleteFlare@0 proc public
        int 3
        ; make sure that the basic block is unique
        test eax,4
        ret
_SignalHijackCompleteFlare@0 endp

; This exception is from unmanaged code.
_ExceptionNotForRuntimeFlare@0 proc public
        int 3
        ; make sure that the basic block is unique
        test eax,5
        ret
_ExceptionNotForRuntimeFlare@0 endp

; The Runtime is synchronized.
_NotifyRightSideOfSyncCompleteFlare@0 proc public
        int 3
        ; make sure that the basic block is unique
        test eax,6
        ret
_NotifyRightSideOfSyncCompleteFlare@0 endp



; This goes at the end of the assembly file
	end






