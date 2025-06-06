; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

;
; FILE: asmhelpers.asm
;
;  *** NOTE:  If you make changes to this file, propagate the changes to
;             asmhelpers.s in this directory
;

;
; ======================================================================================

        .586
        .model  flat

include asmconstants.inc
include asmmacros.inc

        assume fs: nothing
        option  casemap:none
        .code

g_pThrowDivideByZeroException    TEXTEQU <_g_pThrowDivideByZeroException>
g_pThrowOverflowException        TEXTEQU <_g_pThrowOverflowException>
EXTERN g_pThrowDivideByZeroException:DWORD
EXTERN g_pThrowOverflowException:DWORD

EXTERN __imp__RtlUnwind@16:DWORD
ifdef FEATURE_HIJACK
EXTERN _OnHijackWorker@4:PROC
endif ;FEATURE_HIJACK
ifdef FEATURE_EH_FUNCLETS
EXTERN _ProcessCLRException:PROC
EXTERN _UMEntryPrestubUnwindFrameChainHandler:PROC
EXTERN _CallDescrWorkerUnwindFrameChainHandler:PROC
ifdef FEATURE_COMINTEROP
EXTERN _ReverseComUnwindFrameChainHandler:PROC
endif ; FEATURE_COMINTEROP
else
EXTERN _COMPlusFrameHandler:PROC
ifdef FEATURE_COMINTEROP
EXTERN _COMPlusFrameHandlerRevCom:PROC
endif ; FEATURE_COMINTEROP
endif ; FEATURE_EH_FUNCLETS
EXTERN __alloca_probe:PROC
EXTERN _NDirectImportWorker@4:PROC

EXTERN _VarargPInvokeStubWorker@12:PROC
EXTERN _GenericPInvokeCalliStubWorker@12:PROC

EXTERN _PreStubWorker@8:PROC
EXTERN _TheUMEntryPrestubWorker@4:PROC

ifdef FEATURE_COMINTEROP
EXTERN _CLRToCOMWorker@8:PROC
EXTERN _COMToCLRWorker@4:PROC
endif

EXTERN _ExternalMethodFixupWorker@16:PROC

ifdef FEATURE_COMINTEROP
EXTERN _ComPreStubWorker@8:PROC
endif

ifdef FEATURE_READYTORUN
EXTERN _DynamicHelperWorker@20:PROC
endif

EXTERN @ProfileEnter@8:PROC
EXTERN @ProfileLeave@8:PROC
EXTERN @ProfileTailcall@8:PROC

EXTERN __alldiv:PROC
EXTERN __allrem:PROC
EXTERN __aulldiv:PROC
EXTERN __aullrem:PROC

EXTERN _VSD_ResolveWorker@12:PROC
EXTERN _BackPatchWorkerStaticStub@8:PROC
ifdef CHAIN_LOOKUP
g_chained_lookup_call_counter TEXTEQU <_g_chained_lookup_call_counter>
g_chained_lookup_miss_counter TEXTEQU <_g_chained_lookup_miss_counter>
g_dispatch_cache_chain_success_counter TEXTEQU <_g_dispatch_cache_chain_success_counter>
EXTERN @VSD_PromoteChainEntry@4:PROC
EXTERN g_chained_lookup_call_counter:DWORD
EXTERN g_chained_lookup_miss_counter:DWORD
EXTERN g_dispatch_cache_chain_success_counter:DWORD
endif

EXTERN @IL_Throw_x86@8:PROC
EXTERN @IL_ThrowExact_x86@8:PROC
EXTERN @IL_Rethrow_x86@4:PROC

UNREFERENCED macro arg
    local unref
    unref equ size arg
endm

ifndef FEATURE_EH_FUNCLETS
ifdef FEATURE_COMINTEROP
ifdef _DEBUG
    CPFH_STACK_SIZE     equ SIZEOF_FrameHandlerExRecord + STACK_OVERWRITE_BARRIER_SIZE*4
else ; _DEBUG
    CPFH_STACK_SIZE     equ SIZEOF_FrameHandlerExRecord
endif ; _DEBUG

PUSH_CPFH_FOR_COM macro trashReg, pFrameBaseReg, pFrameOffset

    ;
    ; Setup the FrameHandlerExRecord
    ;
    push    dword ptr [pFrameBaseReg + pFrameOffset]
    push    _COMPlusFrameHandlerRevCom
    mov     trashReg, fs:[0]
    push    trashReg
    mov     fs:[0], esp

ifdef _DEBUG
    mov     trashReg, STACK_OVERWRITE_BARRIER_SIZE
@@:
    push    STACK_OVERWRITE_BARRIER_VALUE
    dec     trashReg
    jnz     @B
endif ; _DEBUG

endm  ; PUSH_CPFH_FOR_COM


POP_CPFH_FOR_COM macro trashReg

    ;
    ; Unlink FrameHandlerExRecord from FS:0 chain
    ;
ifdef _DEBUG
    add     esp, STACK_OVERWRITE_BARRIER_SIZE*4
endif
    mov     trashReg, [esp + OFFSETOF__FrameHandlerExRecord__m_ExReg__Next]
    mov     fs:[0], trashReg
    add     esp, SIZEOF_FrameHandlerExRecord

endm  ; POP_CPFH_FOR_COM
endif ; FEATURE_COMINTEROP

PUSH_CLR_EXCEPTION_HANDLER macro handlerName
endm
POP_CLR_EXCEPTION_HANDLER macro
endm

else ; FEATURE_EH_FUNCLETS

CPFH_STACK_SIZE     equ 8

PUSH_CLR_EXCEPTION_HANDLER macro handlerName
    ; setup frame exception handler
    push        handlerName
    push        fs:[0]
    mov         fs:[0], esp
endm

POP_CLR_EXCEPTION_HANDLER macro
    ; remove frame exception handler
    pop         fs:[0]
    add         esp, 4
endm

PUSH_CPFH_FOR_COM macro trashReg, pFrameBaseReg, pFrameOffset
    PUSH_CLR_EXCEPTION_HANDLER _ProcessCLRException
endm

POP_CPFH_FOR_COM macro trashReg
    POP_CLR_EXCEPTION_HANDLER
endm

endif ; FEATURE_EH_FUNCLETS

;
; FramedMethodFrame prolog
;
STUB_PROLOG  macro
    ; push ebp-frame
    push        ebp
    mov         ebp,esp

    ; save CalleeSavedRegisters
    push        ebx
    push        esi
    push        edi

    ; push ArgumentRegisters
    push        ecx
    push        edx
endm

;
; FramedMethodFrame epilog
;
STUB_EPILOG macro
    ; pop ArgumentRegisters
    pop     edx
    pop     ecx

    ; pop CalleeSavedRegisters
    pop edi
    pop esi
    pop ebx
    pop ebp
endm

;
; FramedMethodFrame epilog
;
STUB_EPILOG_RETURN macro
    ; pop ArgumentRegisters
    add esp, 8

    ; pop CalleeSavedRegisters
    pop edi
    pop esi
    pop ebx
    pop ebp
endm

STUB_PROLOG_2_HIDDEN_ARGS macro

    ;
    ; The stub arguments are where we want to setup the TransitionBlock. We will
    ; setup the TransitionBlock later once we can trash them
    ;
    ; push ebp-frame
    ; push      ebp
    ; mov       ebp,esp

    ; save CalleeSavedRegisters
    ; push      ebx

    push        esi
    push        edi

    ; push ArgumentRegisters
    push        ecx
    push        edx

    mov         ecx, [esp + 4*4]
    mov         edx, [esp + 5*4]

    ; Setup up proper EBP frame now that the stub arguments can be trashed
    mov         [esp + 4*4],ebx
    mov         [esp + 5*4],ebp
    lea         ebp, [esp + 5*4]
endm

ResetCurrentContext PROC stdcall public
        LOCAL ctrlWord:WORD

        ; Clear the direction flag (used for rep instructions)
        cld

        fnstcw ctrlWord
        fninit                  ; reset FPU
        and ctrlWord, 0f00h     ; preserve precision and rounding control
        or  ctrlWord, 007fh     ; mask all exceptions
        fldcw ctrlWord          ; preserve precision control
        RET
ResetCurrentContext ENDP

;Incoming:
;   ESP+4: Pointer to buffer to which FPU state should be saved
_CaptureFPUContext@4 PROC public

        mov ecx, [esp+4]
        fnstenv [ecx]
        retn 4

_CaptureFPUContext@4 ENDP

; Incoming:
;  ESP+4: Pointer to buffer from which FPU state should be restored
_RestoreFPUContext@4 PROC public

        mov ecx, [esp+4]
        fldenv [ecx]
        retn 4

_RestoreFPUContext@4 ENDP

; Register CLR exception handlers defined on the C++ side with SAFESEH.
; Note that these directives must be in a file that defines symbols that will be used during linking,
; otherwise it's possible that the resulting .obj will completely be ignored by the linker and these
; directives will have no effect.
ifndef FEATURE_EH_FUNCLETS
COMPlusFrameHandler proto c
.safeseh COMPlusFrameHandler
COMPlusNestedExceptionHandler proto c
.safeseh COMPlusNestedExceptionHandler
FastNExportExceptHandler proto c
.safeseh FastNExportExceptHandler
ifdef FEATURE_COMINTEROP
COMPlusFrameHandlerRevCom proto c
.safeseh COMPlusFrameHandlerRevCom
endif
else ; FEATURE_EH_FUNCLETS
ProcessCLRException proto c
.safeseh ProcessCLRException
UMEntryPrestubUnwindFrameChainHandler proto c
.safeseh UMEntryPrestubUnwindFrameChainHandler
CallDescrWorkerUnwindFrameChainHandler proto c
.safeseh CallDescrWorkerUnwindFrameChainHandler
ifdef FEATURE_COMINTEROP
ReverseComUnwindFrameChainHandler proto c
.safeseh ReverseComUnwindFrameChainHandler
endif ; FEATURE_COMINTEROP
endif ; FEATURE_EH_FUNCLETS

ifdef HAS_ADDRESS_SANITIZER
EXTERN ___asan_handle_no_return:PROC
endif

; Note that RtlUnwind trashes EBX, ESI and EDI, so this wrapper preserves them
CallRtlUnwind PROC stdcall public USES ebx esi edi, pEstablisherFrame :DWORD, callback :DWORD, pExceptionRecord :DWORD, retVal :DWORD

        push retVal
        push pExceptionRecord
        push callback
        push pEstablisherFrame
        call dword ptr [__imp__RtlUnwind@16]

        ; return 1
        push 1
        pop eax

        RET
CallRtlUnwind ENDP

ifndef FEATURE_EH_FUNCLETS
_ResumeAtJitEHHelper@4 PROC public
        ; Call ___asan_handle_no_return here as we are not going to return.
ifdef HAS_ADDRESS_SANITIZER
        call    ___asan_handle_no_return
endif
        mov     edx, [esp+4]     ; edx = pContext (EHContext*)

        mov     ebx, [edx+EHContext_Ebx]
        mov     esi, [edx+EHContext_Esi]
        mov     edi, [edx+EHContext_Edi]
        mov     ebp, [edx+EHContext_Ebp]
        mov     ecx, [edx+EHContext_Esp]
        mov     eax, [edx+EHContext_Eip]
        mov     [ecx-4], eax
        mov     eax, [edx+EHContext_Eax]
        mov     [ecx-8], eax
        mov     eax, [edx+EHContext_Ecx]
        mov     [ecx-0Ch], eax
        mov     eax, [edx+EHContext_Edx]
        mov     [ecx-10h], eax
        lea     esp, [ecx-10h]
        pop     edx
        pop     ecx
        pop     eax
        ret
_ResumeAtJitEHHelper@4 ENDP

; int __stdcall CallJitEHFilterHelper(size_t *pShadowSP, EHContext *pContext);
;   on entry, only the pContext->Esp, Ebx, Esi, Edi, Ebp, and Eip are initialized
_CallJitEHFilterHelper@8 PROC public
        ; Call ___asan_handle_no_return here as we touch registers that ASAN uses.
ifdef HAS_ADDRESS_SANITIZER
        call    ___asan_handle_no_return
endif
        push    ebp
        mov     ebp, esp
        push    ebx
        push    esi
        push    edi

        pShadowSP equ [ebp+8]
        pContext  equ [ebp+12]

        mov     eax, pShadowSP      ; Write esp-4 to the shadowSP slot
        test    eax, eax
        jz      DONE_SHADOWSP_FILTER
        mov     ebx, esp
        sub     ebx, 4
        or      ebx, SHADOW_SP_IN_FILTER_ASM
        mov     [eax], ebx
    DONE_SHADOWSP_FILTER:

        mov     edx, [pContext]
        mov     eax, [edx+EHContext_Eax]
        mov     ebx, [edx+EHContext_Ebx]
        mov     esi, [edx+EHContext_Esi]
        mov     edi, [edx+EHContext_Edi]
        mov     ebp, [edx+EHContext_Ebp]

        call    dword ptr [edx+EHContext_Eip]
ifdef _DEBUG
        nop  ; Indicate that it is OK to call managed code directly from here
endif

        pop     edi
        pop     esi
        pop     ebx
        pop     ebp ; don't use 'leave' here, as ebp as been trashed
        retn    8
_CallJitEHFilterHelper@8 ENDP


; void __stdcall CallJITEHFinallyHelper(size_t *pShadowSP, EHContext *pContext);
;   on entry, only the pContext->Esp, Ebx, Esi, Edi, Ebp, and Eip are initialized
_CallJitEHFinallyHelper@8 PROC public
        ; Call ___asan_handle_no_return here as we touch registers that ASAN uses.
ifdef HAS_ADDRESS_SANITIZER
        call    ___asan_handle_no_return
endif
        push    ebp
        mov     ebp, esp
        push    ebx
        push    esi
        push    edi

        pShadowSP equ [ebp+8]
        pContext  equ [ebp+12]

        mov     eax, pShadowSP      ; Write esp-4 to the shadowSP slot
        test    eax, eax
        jz      DONE_SHADOWSP_FINALLY
        mov     ebx, esp
        sub     ebx, 4
        mov     [eax], ebx
    DONE_SHADOWSP_FINALLY:

        mov     edx, [pContext]
        mov     eax, [edx+EHContext_Eax]
        mov     ebx, [edx+EHContext_Ebx]
        mov     esi, [edx+EHContext_Esi]
        mov     edi, [edx+EHContext_Edi]
        mov     ebp, [edx+EHContext_Ebp]
        call    dword ptr [edx+EHContext_Eip]
ifdef _DEBUG
        nop  ; Indicate that it is OK to call managed code directly from here
endif

        ; Reflect the changes to the context and only update non-volatile registers.
        ; This will be used later to update REGDISPLAY
        mov     edx, [esp+12+12]
        mov     [edx+EHContext_Ebx], ebx
        mov     [edx+EHContext_Esi], esi
        mov     [edx+EHContext_Edi], edi
        mov     [edx+EHContext_Ebp], ebp

        pop     edi
        pop     esi
        pop     ebx
        pop     ebp ; don't use 'leave' here, as ebp as been trashed
        retn    8
_CallJitEHFinallyHelper@8 ENDP
endif

;------------------------------------------------------------------------------
; This helper routine enregisters the appropriate arguments and makes the
; actual call.
;------------------------------------------------------------------------------
; void STDCALL CallDescrWorkerInternal(CallDescrWorkerParams *  pParams)
CallDescrWorkerInternal PROC stdcall public USES EBX,
                         pParams: DWORD

        mov     ebx, pParams

        ; We are about to run managed code so we need to put an exception
        ; handler on the SEH stack; Note that ThePreStub and CallCatchFunclet
        ; may replace where the handler points to!
        PUSH_CLR_EXCEPTION_HANDLER _ProcessCLRException

        mov     ecx, [ebx+CallDescrData__numStackSlots]
        mov     eax, [ebx+CallDescrData__pSrc]            ; copy the stack
        test    ecx, ecx
        jz      donestack
        lea     eax, [eax+4*ecx-4]          ; last argument
        push    dword ptr [eax]
        dec     ecx
        jz      donestack
        sub     eax, 4
        push    dword ptr [eax]
        dec     ecx
        jz      donestack
stackloop:
        sub     eax, 4
        push    dword ptr [eax]
        dec     ecx
        jnz     stackloop
donestack:

        ; now we must push each field of the ArgumentRegister structure
        mov     eax, [ebx+CallDescrData__pArgumentRegisters]
        mov     edx, dword ptr [eax]
        mov     ecx, dword ptr [eax+4]

        call    [ebx+CallDescrData__pTarget]

CallDescrWorkerInternalReturnAddress:
ifdef _DEBUG
    nop     ; Debug-only tag used in asserts.
            ; FCalls expect to be called from Jitted code or specific approved call sites,
            ; like this one.
endif

        ; Save FP return value if necessary
        mov     ecx, [ebx+CallDescrData__fpReturnSize]
        cmp     ecx, 0
        je      ReturnsInt

        cmp     ecx, 4
        je      ReturnsFloat
        cmp     ecx, 8
        je      ReturnsDouble
        ; unexpected
        jmp     Epilog

ReturnsInt:
        mov     [ebx+CallDescrData__returnValue], eax
        mov     [ebx+CallDescrData__returnValue+4], edx

Epilog:
        POP_CLR_EXCEPTION_HANDLER
        ret

ReturnsFloat:
        fstp    dword ptr [ebx+CallDescrData__returnValue]    ; Spill the Float return value
        jmp     Epilog

ReturnsDouble:
        fstp    qword ptr [ebx+CallDescrData__returnValue]    ; Spill the Double return value
        jmp     Epilog

public _CallDescrWorkerInternalReturnAddressOffset
_CallDescrWorkerInternalReturnAddressOffset:
        dd      CallDescrWorkerInternalReturnAddress - CallDescrWorkerInternal

CallDescrWorkerInternal endp

ifdef FEATURE_HIJACK

; A JITted method's return address was hijacked to return to us here.
; VOID OnHijackTripThread()
OnHijackTripThread PROC stdcall public

    ; Don't fiddle with this unless you change HijackFrame::UpdateRegDisplay
    ; and HijackArgs
    push    eax         ; make room for the real return address (Eip)
    push    ebp
    push    eax
    push    ecx
    push    edx
    push    ebx
    push    esi
    push    edi

    ; unused space for floating point state
    sub     esp,12

    push    esp
    call    _OnHijackWorker@4

    ; unused space for floating point state
    add     esp,12

    pop     edi
    pop     esi
    pop     ebx
    pop     edx
    pop     ecx
    pop     eax
    pop     ebp
    retn                ; return to the correct place, adjusted by our caller
OnHijackTripThread ENDP

; VOID OnHijackFPTripThread()
OnHijackFPTripThread PROC stdcall public

    ; Don't fiddle with this unless you change HijackFrame::UpdateRegDisplay
    ; and HijackArgs
    push    eax         ; make room for the real return address (Eip)
    push    ebp
    push    eax
    push    ecx
    push    edx
    push    ebx
    push    esi
    push    edi

    sub     esp,12

    ; save top of the floating point stack (there is return value passed in it)
    ; save full 10 bytes to avoid precision loss
    fstp    tbyte ptr [esp]

    push    esp
    call    _OnHijackWorker@4

    ; restore top of the floating point stack
    fld     tbyte ptr [esp]

    add     esp,12

    pop     edi
    pop     esi
    pop     ebx
    pop     edx
    pop     ecx
    pop     eax
    pop     ebp
    retn                ; return to the correct place, adjusted by our caller
OnHijackFPTripThread ENDP

endif ; FEATURE_HIJACK



;==========================================================================
; This function is reached only via the embedded ImportThunkGlue code inside
; an NDirectMethodDesc. It's purpose is to load the DLL associated with an
; N/Direct method, then backpatch the DLL target into the methoddesc.
;
; Initial state:
;
;      Preemptive GC is *enabled*: we are actually in an unmanaged state.
;
;
;      [esp+...]   - The *unmanaged* parameters to the DLL target.
;      [esp+4]     - Return address back into the JIT'ted code that made
;                    the DLL call.
;      [esp]       - Contains the "return address." Because we got here
;                    thru a call embedded inside a MD, this "return address"
;                    gives us an easy to way to find the MD (which was the
;                    whole purpose of the embedded call manuever.)
;
;
;
;==========================================================================
_NDirectImportThunk@0 proc public

        ; Preserve argument registers
        push    ecx
        push    edx

        ; Invoke the function that does the real work.
        push    eax
        call    _NDirectImportWorker@4

        ; Restore argument registers
        pop     edx
        pop     ecx

        ; If we got back from NDirectImportWorker, the MD has been successfully
        ; linked and "eax" contains the DLL target. Proceed to execute the
        ; original DLL call.
        jmp     eax     ; Jump to DLL target
_NDirectImportThunk@0 endp

; void __stdcall setFPReturn(int fpSize, INT64 retVal)
_setFPReturn@12 proc public
    mov     ecx, [esp+4]

    ; leave the return value in eax:edx if it is not the floating point case
    mov     eax, [esp+8]
    mov     edx, [esp+12]

    cmp     ecx, 4
    jz      setFPReturn4

    cmp     ecx, 8
    jnz     setFPReturnNot8
    fld     qword ptr [esp+8]
setFPReturnNot8:
    retn    12

setFPReturn4:
    fld     dword ptr [esp+8]
    retn    12
_setFPReturn@12 endp

; void __stdcall getFPReturn(int fpSize, INT64 *pretVal)
_getFPReturn@8 proc public
   mov     ecx, [esp+4]
   mov     eax, [esp+8]
   cmp     ecx, 4
   jz      getFPReturn4

   cmp     ecx, 8
   jnz     getFPReturnNot8
   fstp    qword ptr [eax]
getFPReturnNot8:
   retn    8

getFPReturn4:
   fstp    dword ptr [eax]
   retn    8
_getFPReturn@8 endp


; void __stdcall JIT_ProfilerEnterLeaveTailcallStub(UINT_PTR ProfilerHandle)
_JIT_ProfilerEnterLeaveTailcallStub@4 proc public
    ; this function must preserve all registers, including scratch
    retn    4
_JIT_ProfilerEnterLeaveTailcallStub@4 endp

;
; Used to get the current instruction pointer value
;
; UINT_PTR __stdcall GetCurrentIP(void);
_GetCurrentIP@0 proc public
    mov     eax, [esp]
    retn
_GetCurrentIP@0 endp

; LPVOID __stdcall GetCurrentSP(void);
_GetCurrentSP@0 proc public
    mov     eax, esp
    retn
_GetCurrentSP@0 endp


; void __stdcall ProfileEnterNaked(FunctionIDOrClientID functionIDOrClientID);
_ProfileEnterNaked@4 proc public
    push    esi
    push    edi

    ;
    ; Push in reverse order the fields of ProfilePlatformSpecificData
    ;
    push    dword ptr [esp+8] ; EIP of the managed code that we return to.	-- struct ip field
    push    ebp          ; Methods are always EBP framed
    add     [esp], 8     ; Skip past the return IP, straight to the stack args that were passed to our caller
                         ; Skip past saved EBP value: 4 bytes
                         ;   - plus return address from caller's caller: 4 bytes
                         ;
                         ; Assuming Foo() calls Bar(), and Bar() calls ProfileEnterNake() as illustrated (stack
                         ; grows up). We want to get what Foo() passed on the stack to Bar(), so we need to pass
                         ; the return address from caller's caller which is Foo() in this example.
                         ;
                         ; ProfileEnterNaked()
                         ; Bar()
                         ; Foo()
                         ;
                         ; [ESP] is now the ESP of caller's caller pointing to the arguments to the caller.

    push    ecx	         ;                                                  -- struct ecx field
    push    edx	         ;                                                  -- struct edx field
    push    eax	         ;                                                  -- struct eax field
    push    0            ; Create buffer space in the structure             -- struct floatingPointValuePresent field
    push    0            ; Create buffer space in the structure             -- struct floatBuffer field
    push    0            ; Create buffer space in the structure             -- struct doubleBuffer2 field
    push    0            ; Create buffer space in the structure             -- struct doubleBuffer1 field
    push    0            ; Create buffer space in the structure             -- struct functionId field

    mov     edx, esp     ; the address of the Platform structure
    mov     ecx, [esp+52]; The functionIDOrClientID parameter that was pushed to FunctionEnter
                         ; Skip past ProfilePlatformSpecificData we pushed: 40 bytes
                         ;   - plus saved edi, esi : 8 bytes
                         ;   - plus return address from caller: 4 bytes

    call    @ProfileEnter@8

    add     esp, 20      ; Remove buffer space
    pop     eax
    pop     edx
    pop     ecx
    add     esp, 8       ; Remove buffer space
    pop     edi
    pop     esi

    retn    4
_ProfileEnterNaked@4 endp

; void __stdcall ProfileLeaveNaked(FunctionIDOrClientID functionIDOrClientID);
_ProfileLeaveNaked@4 proc public
    push    ecx       ; We do not strictly need to save ECX, however
                      ; emitNoGChelper(CORINFO_HELP_PROF_FCN_LEAVE) returns true in the JITcompiler
    push    edx       ; Return value may be in EAX:EDX

    ;
    ; Push in reverse order the fields of ProfilePlatformSpecificData
    ;
    push    dword ptr [esp+8] ; EIP of the managed code that we return to.	-- struct ip field
    push    ebp          ; Methods are always EBP framed
    add     [esp], 8     ; Skip past the return IP, straight to the stack args that were passed to our caller
                         ; Skip past saved EBP value: 4 bytes
                         ;   - plus return address from caller's caller: 4 bytes
                         ;
                         ; Assuming Foo() calls Bar(), and Bar() calls ProfileEnterNake() as illustrated (stack
                         ; grows up). We want to get what Foo() passed on the stack to Bar(), so we need to pass
                         ; the return address from caller's caller which is Foo() in this example.
                         ;
                         ; ProfileEnterNaked()
                         ; Bar()
                         ; Foo()
                         ;
                         ; [ESP] is now the ESP of caller's caller pointing to the arguments to the caller.

    push    ecx	         ;                                                  -- struct ecx field
    push    edx	         ;                                                  -- struct edx field
    push    eax	         ;                                                  -- struct eax field

    ; Check if we need to save off any floating point registers
    fstsw   ax
    and     ax, 3800h    ; Check the top-of-fp-stack bits
    cmp     ax, 0        ; If non-zero, we have something to save
    jnz     SaveFPReg

    push    0            ; Create buffer space in the structure             -- struct floatingPointValuePresent field
    push    0            ; Create buffer space in the structure             -- struct floatBuffer field
    push    0            ; Create buffer space in the structure             -- struct doubleBuffer2 field
    push    0            ; Create buffer space in the structure             -- struct doubleBuffer1 field
    jmp     Continue

SaveFPReg:
    push    1            ; mark that a float value is present               -- struct floatingPointValuePresent field
    sub     esp, 4       ; Make room for the FP value
    fst     dword ptr [esp] ; Copy the FP value to the buffer as a float    -- struct floatBuffer field
    sub     esp, 8       ; Make room for the FP value
    fstp    qword ptr [esp] ; Copy FP values to the buffer as a double      -- struct doubleBuffer1 and doubleBuffer2 fields

Continue:
    push    0            ; Create buffer space in the structure             -- struct functionId field

    mov     edx, esp     ; the address of the Platform structure
    mov     ecx, [esp+52]; The clientData that was pushed to FunctionEnter
                         ; Skip past ProfilePlatformSpecificData we pushed: 40 bytes
                         ;   - plus saved edx, ecx : 8 bytes
                         ;   - plus return address from caller: 4 bytes

    call    @ProfileLeave@8

    ;
    ; Now see if we have to restore and floating point registers
    ;

    cmp     [esp + 16], 0
    jz      NoRestore

    fld     qword ptr [esp + 4]

NoRestore:

    add     esp, 20      ; Remove buffer space
    pop     eax
    add     esp, 16      ; Remove buffer space
    pop     edx
    pop     ecx
    retn    4
_ProfileLeaveNaked@4 endp


; void __stdcall ProfileTailcallNaked(FunctionIDOrClientID functionIDOrClientID);
_ProfileTailcallNaked@4 proc public
    push    ecx
    push    edx

    ;
    ; Push in reverse order the fields of ProfilePlatformSpecificData
    ;
    push    dword ptr [esp+8] ; EIP of the managed code that we return to.	-- struct ip field
    push    ebp          ; Methods are always EBP framed
    add     [esp], 8     ; Skip past the return IP, straight to the stack args that were passed to our caller
                         ; Skip past saved EBP value: 4 bytes
                         ;   - plus return address from caller's caller: 4 bytes
                         ;
                         ; Assuming Foo() calls Bar(), and Bar() calls ProfileEnterNake() as illustrated (stack
                         ; grows up). We want to get what Foo() passed on the stack to Bar(), so we need to pass
                         ; the return address from caller's caller which is Foo() in this example.
                         ;
                         ; ProfileEnterNaked()
                         ; Bar()
                         ; Foo()
                         ;
                         ; [ESP] is now the ESP of caller's caller pointing to the arguments to the caller.

    push    ecx	         ;                                                  -- struct ecx field
    push    edx	         ;                                                  -- struct edx field
    push    eax	         ;                                                  -- struct eax field
    push    0            ; Create buffer space in the structure             -- struct floatingPointValuePresent field
    push    0            ; Create buffer space in the structure             -- struct floatBuffer field
    push    0            ; Create buffer space in the structure             -- struct doubleBuffer2 field
    push    0            ; Create buffer space in the structure             -- struct doubleBuffer1 field
    push    0            ; Create buffer space in the structure             -- struct functionId field

    mov     edx, esp     ; the address of the Platform structure
    mov     ecx, [esp+52]; The clientData that was pushed to FunctionEnter
                         ; Skip past ProfilePlatformSpecificData we pushed: 40 bytes
                         ;   - plus saved edx, ecx : 8 bytes
                         ;   - plus return address from caller: 4 bytes

    call    @ProfileTailcall@8

    add     esp, 40      ; Remove buffer space
    pop     edx
    pop     ecx
    retn    4
_ProfileTailcallNaked@4 endp

;==========================================================================
; Invoked for vararg forward P/Invoke calls as a stub.
; Except for secret return buffer, arguments come on the stack so EDX is available as scratch.
; EAX       - the NDirectMethodDesc
; ECX       - may be return buffer address
; [ESP + 4] - the VASigCookie
;
_VarargPInvokeStub@0 proc public
    ; EDX <- VASigCookie
    mov     edx, [esp + 4]           ; skip retaddr

    mov     edx, [edx + VASigCookie__StubOffset]
    test    edx, edx

    jz      GoCallVarargWorker
    ; ---------------------------------------

    ; EAX contains MD ptr for the IL stub
    jmp     edx

GoCallVarargWorker:
    ;
    ; MD ptr in EAX, VASigCookie ptr at [esp+4]
    ;

    STUB_PROLOG

    mov         esi, esp

    ; save pMD
    push        eax

    push        eax                     ; pMD
    push        dword ptr [esi + 4*7]   ; pVaSigCookie
    push        esi                     ; pTransitionBlock

    call        _VarargPInvokeStubWorker@12

    ; restore pMD
    pop     eax

    STUB_EPILOG

    ; jump back to the helper - this time it won't come back here as the stub already exists
    jmp _VarargPInvokeStub@0

_VarargPInvokeStub@0 endp

;==========================================================================
; Invoked for marshaling-required unmanaged CALLI calls as a stub.
; EAX       - the unmanaged target
; ECX, EDX  - arguments
; EBX       - the VASigCookie
;
_GenericPInvokeCalliHelper@0 proc public

    cmp     dword ptr [ebx + VASigCookie__StubOffset], 0
    jz      GoCallCalliWorker

    ; Stub is already prepared, just jump to it
    jmp     dword ptr [ebx + VASigCookie__StubOffset]

GoCallCalliWorker:
    ;
    ; call the stub generating worker
    ; target ptr in EAX, VASigCookie ptr in EBX
    ;

    STUB_PROLOG

    mov         esi, esp

    ; save target
    push        eax

    push        eax                         ; unmanaged target
    push        ebx                         ; pVaSigCookie (first stack argument)
    push        esi                         ; pTransitionBlock

    call        _GenericPInvokeCalliStubWorker@12

    ; restore target
    pop     eax

    STUB_EPILOG

    ; jump back to the helper - this time it won't come back here as the stub already exists
    jmp _GenericPInvokeCalliHelper@0

_GenericPInvokeCalliHelper@0 endp

ifdef FEATURE_COMINTEROP

;==========================================================================
; This is a fast alternative to CallDescr* tailored specifically for
; COM to CLR calls. Stack arguments don't come in a continuous buffer
; and secret argument can be passed in EAX.
;

; extern "C" ARG_SLOT __fastcall COMToCLRDispatchHelper(
;     INT_PTR dwArgECX,                 ; ecx
;     INT_PTR dwArgEDX,                 ; edx
;     PCODE   pTarget,                  ; [esp + 4]
;     PCODE   pSecretArg,               ; [esp + 8]
;     INT_PTR *pInputStack,             ; [esp + c]
;     WORD    wOutputStackSlots,        ; [esp +10]
;     UINT16  *pOutputStackOffsets,     ; [esp +14]
;     Frame   *pCurFrame);              ; [esp +18]

FASTCALL_FUNC COMToCLRDispatchHelper, 32

    ; ecx: dwArgECX
    ; edx: dwArgEDX

    offset_pTarget              equ 4
    offset_pSecretArg           equ 8
    offset_pInputStack          equ 0Ch
    offset_wOutputStackSlots    equ 10h
    offset_pOutputStackOffsets  equ 14h
    offset_pCurFrame            equ 18h

    movzx   eax, word ptr [esp + offset_wOutputStackSlots]
    test    eax, eax
    jnz     CopyStackArgs

    ; There are no stack args to copy and ECX and EDX are already setup
    ; with the correct arguments for the callee, so we just have to
    ; push the CPFH and make the call.

    PUSH_CPFH_FOR_COM   eax, esp, offset_pCurFrame     ; trashes eax

    mov     eax, [esp + offset_pSecretArg + CPFH_STACK_SIZE]
    call    [esp + offset_pTarget + CPFH_STACK_SIZE]
ifdef _DEBUG
    nop     ; This is a tag that we use in an assert.
endif

    POP_CPFH_FOR_COM    ecx     ; trashes ecx

    ret     18h


CopyStackArgs:
    ; eax: num stack slots
    ; ecx: dwArgECX
    ; edx: dwArgEDX

    push    ebp
    mov     ebp, esp
    push    ebx
    push    esi
    push    edi

    ebpFrame_adjust         equ 4h
    ebp_offset_pCurFrame    equ ebpFrame_adjust + offset_pCurFrame

    PUSH_CPFH_FOR_COM   ebx, ebp, ebp_offset_pCurFrame     ; trashes ebx

    mov     edi, [ebp + ebpFrame_adjust + offset_pOutputStackOffsets]
    mov     esi, [ebp + ebpFrame_adjust + offset_pInputStack]

    ; eax: num stack slots
    ; ecx: dwArgECX
    ; edx: dwArgEDX
    ; edi: pOutputStackOffsets
    ; esi: pInputStack

CopyStackLoop:
    dec     eax
    movzx   ebx, word ptr [edi + 2 * eax] ; ebx <- input stack offset
    push    [esi + ebx]                   ; stack <- value on the input stack
    jnz     CopyStackLoop

    ; ECX and EDX are setup with the correct arguments for the callee,
    ; and we've copied the stack arguments over as well, so now it's
    ; time to make the call.

    mov     eax, [ebp + ebpFrame_adjust + offset_pSecretArg]
    call    [ebp + ebpFrame_adjust + offset_pTarget]
ifdef _DEBUG
    nop     ; This is a tag that we use in an assert.
endif

    POP_CPFH_FOR_COM    ecx     ; trashes ecx

    pop     edi
    pop     esi
    pop     ebx
    pop     ebp

    ret     18h

FASTCALL_ENDFUNC

endif ; FEATURE_COMINTEROP

ifdef FEATURE_READYTORUN
;==========================================================================
_DelayLoad_MethodCall@0 proc public

    STUB_PROLOG_2_HIDDEN_ARGS

    mov         esi, esp

    push        ecx
    push        edx

    push        eax

    ; pTransitionBlock
    push        esi

    call        _ExternalMethodFixupWorker@16

    ; eax now contains replacement stub. PreStubWorker will never return
    ; NULL (it throws an exception if stub creation fails.)

    ; From here on, mustn't trash eax

    STUB_EPILOG

    ; Tailcall target
    jmp eax

    ; This will never be executed. It is just to help out stack-walking logic
    ; which disassembles the epilog to unwind the stack.
    ret

_DelayLoad_MethodCall@0 endp
endif

;==========================================================================
; The prestub
_ThePreStub@0 proc public

    STUB_PROLOG

    mov         esi, esp

    ; EAX contains MethodDesc* from the precode. Push it here as argument
    ; for PreStubWorker
    push        eax

    push        esi

    call        _PreStubWorker@8

    ; eax now contains replacement stub. PreStubWorker will never return
    ; NULL (it throws an exception if stub creation fails.)

    ; From here on, mustn't trash eax

    STUB_EPILOG

    ; Tailcall target
    jmp eax

    ; This will never be executed. It is just to help out stack-walking logic
    ; which disassembles the epilog to unwind the stack.
    ret

_ThePreStub@0 endp

; This method does nothing.  It's just a fixed function for the debugger to put a breakpoint
; on so that it can trace a call target.
_ThePreStubPatch@0 proc public
    ; make sure that the basic block is unique
    test eax,34
_ThePreStubPatchLabel@0:
public _ThePreStubPatchLabel@0
    ret
_ThePreStubPatch@0 endp

_TheUMEntryPrestub@0 proc public
    ; push argument registers
    push        ecx
    push        edx

    PUSH_CLR_EXCEPTION_HANDLER _UMEntryPrestubUnwindFrameChainHandler

    push    eax     ; UMEntryThunkData*
    call    _TheUMEntryPrestubWorker@4

    POP_CLR_EXCEPTION_HANDLER

    ; pop argument registers
    pop         edx
    pop         ecx

    ; eax = PCODE
    jmp     eax     ; Tail Jmp
_TheUMEntryPrestub@0 endp

ifdef FEATURE_COMINTEROP
;==========================================================================
; CLR -> COM generic or late-bound call
_GenericCLRToCOMCallStub@0 proc public

    STUB_PROLOG

    ; pTransitionBlock
    mov         esi, esp

    ; return value
    sub         esp, 8

    ; save pMD
    mov         ebx, eax

    push        eax                 ; pMD
    push        esi                 ; pTransitionBlock
    call        _CLRToCOMWorker@8

    push        eax
    call        _setFPReturn@12     ; pop & set the return value

    ; From here on, mustn't trash eax:edx

    ; Get pCLRToCOMCallInfo for return thunk
    mov         ecx, [ebx + CLRToCOMCallMethodDesc__m_pCLRToCOMCallInfo]
    ; Get size of arguments to pop
    movzx       ecx, word ptr [ecx + CLRToCOMCallInfo__m_cbStackPop]
    ; Get the return address, pushed registers on stack are 24 bytes big
    mov         ebx, [esp + 24]
    ; Set the return address on stack at the last stack slot
    mov         [esp + ecx + 24], ebx

    STUB_EPILOG_RETURN

    ; Move esp to point to the last stack slot where we put the return
    ; address earlier
    lea         esp, [esp + ecx]

    ret

_GenericCLRToCOMCallStub@0 endp

_GenericComCallStub@0 proc public

    ; Pop ComCallMethodDesc* pushed by prestub
    pop         eax

    ; Create UnmanagedToManagedFrame on stack

    ; push ebp-frame
    push        ebp
    mov         ebp,esp

    ; save CalleeSavedRegisters
    push        ebx
    push        esi
    push        edi

    push        eax         ; UnmanagedToManagedFrame::m_pvDatum = ComCallMethodDesc*
    sub         esp, OFFSETOF__UnmanagedToManagedFrame__m_pvDatum

    mov         esi, esp

    PUSH_CLR_EXCEPTION_HANDLER _ReverseComUnwindFrameChainHandler

    push        esi
    call        _COMToCLRWorker@4

    POP_CLR_EXCEPTION_HANDLER

    add         esp, OFFSETOF__UnmanagedToManagedFrame__m_pvDatum

    ; pop the ComCallMethodDesc*
    pop         ecx

    ; pop CalleeSavedRegisters
    pop         edi
    pop         esi
    pop         ebx

    pop         ebp

    sub         ecx, COMMETHOD_PREPAD_ASM
    jmp         ecx

    ; This will never be executed. It is just to help out stack-walking logic
    ; which disassembles the epilog to unwind the stack.
    ret

_GenericComCallStub@0 endp

endif ; FEATURE_COMINTEROP


ifdef FEATURE_COMINTEROP
;--------------------------------------------------------------------------
; This is the code that all com call method stubs run initially.
; Most of the real work occurs in ComStubWorker(), a C++ routine.
; The template only does the part that absolutely has to be in assembly
; language.
;--------------------------------------------------------------------------
_ComCallPreStub@0 proc public
    pop     eax                 ;ComCallMethodDesc*

    ; push ebp-frame
    push        ebp
    mov         ebp,esp

    ; save CalleeSavedRegisters
    push        ebx
    push        esi
    push        edi

    push        eax         ; ComCallMethodDesc*
    sub         esp, 4*4    ; next, vtable, 64-bit error return

    lea     edi, [esp]      ; Point at the 64-bit error return
    lea     esi, [esp+2*4]  ; Leave space for the 64-bit error return

    push    edi                 ; pErrorReturn
    push    esi                 ; pFrame
    call    _ComPreStubWorker@8

    ; eax now contains replacement stub. ComStubWorker will  return NULL if stub creation fails
    cmp eax, 0
    je nostub                   ; oops we could not create a stub

    add     esp, 5*4            ; Pop off 64-bit error return, vtable, next and ComCallMethodDesc*

    ; pop CalleeSavedRegisters
    pop edi
    pop esi
    pop ebx
    pop ebp

    jmp     eax                 ; Reexecute with replacement stub.
    ; We will never get here. This "ret" is just so that code-disassembling
    ; profilers know to stop disassembling any further
    ret

nostub:

    ; Even though the ComPreStubWorker sets a 64 bit value as the error return code.
    ; Only the lower 32 bits contain usefula data. The reason for this is that the
    ; possible error return types are: failure HRESULT, 0 and floating point 0.
    ; In each case, the data fits in 32 bits. Instead, we use the upper half of
    ; the return value to store number of bytes to pop
    mov     eax, [edi]
    mov     edx, [edi+4]

    add     esp, 5*4            ; Pop off 64-bit error return, vtable, next and ComCallMethodDesc*

    ; pop CalleeSavedRegisters
    pop edi
    pop esi
    pop ebx
    pop ebp

    pop     ecx                 ; return address
    add     esp, edx            ; pop bytes of the stack
    push    ecx                 ; return address

    ; We need to deal with the case where the method is PreserveSig=true and has an 8
    ; byte return type. There are 2 types of 8 byte return types: integer and floating point.
    ; For integer 8 byte return types, we always return 0 in case of failure. For floating
    ; point return types, we return the value in the floating point register. In both cases
    ; edx should be 0.
    xor     edx, edx            ; edx <-- 0

    ret

_ComCallPreStub@0 endp
endif ; FEATURE_COMINTEROP

ifdef FEATURE_READYTORUN
;==========================================================================
; Define helpers for delay loading of readytorun helpers

DYNAMICHELPER macro frameFlags, suffix

_DelayLoad_Helper&suffix&@0 proc public

    STUB_PROLOG_2_HIDDEN_ARGS

    mov         esi, esp

    push        frameFlags
    push        ecx             ; module
    push        edx             ; section index

    push        eax             ; indirection cell address.
    push        esi             ; pTransitionBlock

    call        _DynamicHelperWorker@20
    test        eax,eax
    jnz         @F

    mov         eax, [esi]      ; The result is stored in the argument area of the transition block
    STUB_EPILOG_RETURN
    ret

@@:
    STUB_EPILOG
    jmp eax

_DelayLoad_Helper&suffix&@0 endp

    endm

DYNAMICHELPER DynamicHelperFrameFlags_Default
DYNAMICHELPER DynamicHelperFrameFlags_ObjectArg, _Obj
DYNAMICHELPER <DynamicHelperFrameFlags_ObjectArg OR DynamicHelperFrameFlags_ObjectArg2>, _ObjObj

endif ; FEATURE_READYTORUN

ifdef FEATURE_TIERED_COMPILATION

EXTERN _OnCallCountThresholdReached@8:proc

_OnCallCountThresholdReachedStub@0 proc public
    STUB_PROLOG

    mov     esi, esp

    push    eax ; stub-identifying token, see OnCallCountThresholdReachedStub
    push    esi ; TransitionBlock *
    call    _OnCallCountThresholdReached@8

    STUB_EPILOG
    jmp     eax

    ; This will never be executed. It is just to help out stack-walking logic
    ; which disassembles the epilog to unwind the stack.
    ret
_OnCallCountThresholdReachedStub@0 endp

endif ; FEATURE_TIERED_COMPILATION

    DivisorLow32BitsOffset     equ 04h
    DivisorHi32BitsOffset     equ 08h
    DividendLow32BitsOffset     equ 0Ch
    DividendHi32BitsOffset     equ 10h

;; Stack on entry
;; dividend (hi 32 bits)
;; dividend (lo 32 bits)
;; divisor (hi 32 bits)
;; divisor (lo 32 bits)
;; return address
ALIGN 16
FASTCALL_FUNC JIT_LDiv, 16
    mov     eax,dword ptr [esp + DivisorLow32BitsOffset]
    cdq
    cmp     edx,dword ptr [esp + DivisorHi32BitsOffset]
    jne     JIT_LDiv_Call__alldiv ; We're going to call CRT Helper routine
    test    eax, eax
    je      JIT_ThrowDivideByZero_Long
    cmp     eax,0FFFFFFFFh
    jne     JIT_LDiv_DoDivideBy32BitDivisor
    mov     eax,dword  ptr [esp + DividendLow32BitsOffset]
    test    eax, eax
    mov     edx,dword  ptr [esp + DividendHi32BitsOffset]
    jne     JIT_LDiv_DoNegate
    cmp     edx,80000000h
    je      JIT_ThrowOverflow_Long
JIT_LDiv_DoNegate:
    neg     eax
    adc     edx,0
    neg     edx
    ret     10h
ALIGN 16
JIT_LDiv_DoDivideBy32BitDivisor:
    ; First check to see if dividend is also 32 bits
    mov     ecx, eax ; Put divisor in ecx
    mov     eax, dword ptr  [esp + DividendLow32BitsOffset]
    cdq
    cmp     edx, dword ptr [esp + DividendHi32BitsOffset]
    jne     JIT_LDiv_Call__alldiv ; We're going to call CRT Helper routine
    idiv    ecx
    cdq
    ret     10h
ALIGN 16
JIT_LDiv_Call__alldiv:
    ; Swap the divisor and dividend in output
    mov     eax,dword ptr [esp + DivisorLow32BitsOffset]
    mov     ecx,dword ptr [esp + DivisorHi32BitsOffset]
    mov     edx,dword ptr [esp + DividendLow32BitsOffset]
    mov     dword ptr [esp + DividendLow32BitsOffset], eax
    mov     eax,dword ptr [esp + DividendHi32BitsOffset]
    mov     dword ptr [esp + DividendHi32BitsOffset], ecx
    mov     dword ptr [esp + DivisorLow32BitsOffset], edx
    mov     dword ptr [esp + DivisorHi32BitsOffset], eax
    jne     __alldiv ; Tail call the CRT Helper routine for 64 bit divide
FASTCALL_ENDFUNC

;; Stack on entry
;; dividend (hi 32 bits)
;; dividend (lo 32 bits)
;; divisor (hi 32 bits)
;; divisor (lo 32 bits)
;; return address
ALIGN 16
FASTCALL_FUNC JIT_LMod, 16
    mov     eax,dword ptr [esp + DivisorLow32BitsOffset]
    cdq
    cmp     edx,dword ptr [esp + DivisorHi32BitsOffset]
    jne     JIT_LMod_Call__allrem ; We're going to call CRT Helper routine
    test    eax, eax
    je      JIT_ThrowDivideByZero_Long
    cmp     eax,0FFFFFFFFh
    jne     JIT_LMod_DoDivideBy32BitDivisor
    mov     eax,dword  ptr [esp + DividendLow32BitsOffset]
    test    eax, eax
    jne     JIT_LMod_ReturnZero
    mov     edx,dword  ptr [esp + DividendHi32BitsOffset]
    cmp     edx,80000000h
    je      JIT_ThrowOverflow_Long
JIT_LMod_ReturnZero:
    xor     eax, eax
    xor     edx, edx
    ret     10h
ALIGN 16
JIT_LMod_DoDivideBy32BitDivisor:
    ; First check to see if dividend is also 32 bits
    mov     ecx, eax ; Put divisor in ecx
    mov     eax, dword ptr [esp + DividendLow32BitsOffset]
    cdq
    cmp     edx, dword ptr [esp + DividendHi32BitsOffset]
    jne     JIT_LMod_Call__allrem ; We're going to call CRT Helper routine
    idiv    ecx
    mov     eax,edx
    cdq
    ret     10h
ALIGN 16
JIT_LMod_Call__allrem:
    ; Swap the divisor and dividend in output
    mov     eax,dword ptr [esp + DivisorLow32BitsOffset]
    mov     ecx,dword ptr [esp + DivisorHi32BitsOffset]
    mov     edx,dword ptr [esp + DividendLow32BitsOffset]
    mov     dword ptr [esp + DividendLow32BitsOffset], eax
    mov     eax,dword ptr [esp + DividendHi32BitsOffset]
    mov     dword ptr [esp + DividendHi32BitsOffset], ecx
    mov     dword ptr [esp + DivisorLow32BitsOffset], edx
    mov     dword ptr [esp + DivisorHi32BitsOffset], eax
    jne     __allrem ; Tail call the CRT Helper routine for 64 bit divide
FASTCALL_ENDFUNC

;; Stack on entry
;; dividend (hi 32 bits)
;; dividend (lo 32 bits)
;; divisor (hi 32 bits)
;; divisor (lo 32 bits)
;; return address
ALIGN 16
FASTCALL_FUNC JIT_ULDiv, 16
    mov     eax,dword ptr [esp + DivisorLow32BitsOffset]
    cmp     dword ptr [esp + DivisorHi32BitsOffset], 0
    jne     JIT_ULDiv_Call__aulldiv ; We're going to call CRT Helper routine
    test    eax, eax
    je      JIT_ThrowDivideByZero_Long
    ; First check to see if dividend is also 32 bits
    mov     ecx, eax ; Put divisor in ecx
    cmp     dword ptr [esp + DividendHi32BitsOffset], 0
    jne     JIT_ULDiv_Call__aulldiv ; We're going to call CRT Helper routine
    mov     eax, dword ptr [esp + DividendLow32BitsOffset]
    xor     edx, edx
    div     ecx
    xor     edx, edx
    ret     10h
ALIGN 16
JIT_ULDiv_Call__aulldiv:
    ; Swap the divisor and dividend in output
    mov     ecx,dword ptr [esp + DivisorHi32BitsOffset]
    mov     edx,dword ptr [esp + DividendLow32BitsOffset]
    mov     dword ptr [esp + DividendLow32BitsOffset], eax
    mov     eax,dword ptr [esp + DividendHi32BitsOffset]
    mov     dword ptr [esp + DividendHi32BitsOffset], ecx
    mov     dword ptr [esp + DivisorLow32BitsOffset], edx
    mov     dword ptr [esp + DivisorHi32BitsOffset], eax
    jne    __aulldiv ; Tail call the CRT Helper routine for 64 bit unsigned divide
FASTCALL_ENDFUNC

;; Stack on entry
;; dividend (hi 32 bits)
;; dividend (lo 32 bits)
;; divisor (hi 32 bits)
;; divisor (lo 32 bits)
;; return address
ALIGN 16
FASTCALL_FUNC JIT_ULMod, 16
    mov     eax,dword ptr [esp + DivisorLow32BitsOffset]
    cdq
    cmp     dword ptr [esp + DivisorHi32BitsOffset], 0
    jne     JIT_LMod_Call__aullrem ; We're going to call CRT Helper routine
    test    eax, eax
    je      JIT_ThrowDivideByZero_Long
    ; First check to see if dividend is also 32 bits
    mov     ecx, eax ; Put divisor in ecx
    cmp     dword ptr [esp + DividendHi32BitsOffset], 0
    jne     JIT_LMod_Call__aullrem ; We're going to call CRT Helper routine
    mov     eax, dword ptr [esp + DividendLow32BitsOffset]
    xor     edx, edx
    div     ecx
    mov     eax, edx
    xor     edx, edx
    ret     10h
ALIGN 16
JIT_LMod_Call__aullrem:
    ; Swap the divisor and dividend in output
    mov     ecx,dword ptr [esp + DivisorHi32BitsOffset]
    mov     edx,dword ptr [esp + DividendLow32BitsOffset]
    mov     dword ptr [esp + DividendLow32BitsOffset], eax
    mov     eax,dword ptr [esp + DividendHi32BitsOffset]
    mov     dword ptr [esp + DividendHi32BitsOffset], ecx
    mov     dword ptr [esp + DivisorLow32BitsOffset], edx
    mov     dword ptr [esp + DivisorHi32BitsOffset], eax
    jne     __aullrem ; Tail call the CRT Helper routine for 64 bit unsigned modulus
FASTCALL_ENDFUNC

JIT_ThrowDivideByZero_Long proc public
    pop eax ; Pop return address into eax
    add esp, 10h
    push eax ; Fix return address
    mov eax, g_pThrowDivideByZeroException
    jmp eax
JIT_ThrowDivideByZero_Long endp

JIT_ThrowOverflow_Long proc public
    pop eax ; Pop return address into eax
    add esp, 10h
    push eax ; Fix return address
    mov eax, g_pThrowOverflowException
    jmp eax
JIT_ThrowOverflow_Long endp

; rcx -This pointer
; rdx -ReturnBuffer
_ThisPtrRetBufPrecodeWorker@0 proc public
    mov  eax, [eax + ThisPtrRetBufPrecodeData__Target]
    ; Use XOR swap technique to set avoid the need to spill to the stack
    xor ecx, edx
    xor edx, ecx
    xor ecx, edx
    jmp eax
_ThisPtrRetBufPrecodeWorker@0 endp

;==========================================================================
; Call the resolver, it will return where we are supposed to go.
; There is a little stack magic here, in that we are entered with one
; of the arguments for the resolver (the token) on the stack already.
; We just push the other arguments, <this> in the call frame and the call site pointer,
; and call the resolver.
;
; On return we have the stack frame restored to the way it was when the ResolveStub
; was called, i.e. as it was at the actual call site.  The return value from
; the resolver is the address we need to transfer control to, simulating a direct
; call from the original call site.  If we get passed back NULL, it means that the
; resolution failed, an unimpelemented method is being called.
;
; Entry stack:
;          dispatch token
;          siteAddrForRegisterIndirect (used only if this is a RegisterIndirect dispatch call)
;          return address of caller to stub
;
; Call stack:
;          pointer to TransitionBlock
;          call site
;          dispatch token
;          TransitionBlock
;              ArgumentRegisters (ecx, edx)
;              CalleeSavedRegisters (ebp, ebx, esi, edi)
;          return address of caller to stub
;==========================================================================
_ResolveWorkerAsmStub@0 proc public
    ;
    ; The stub arguments are where we want to setup the TransitionBlock. We will
    ; setup the TransitionBlock later once we can trash them
    ;
    ; push ebp-frame
    ; push  ebp
    ; mov   ebp,esp

    ; save CalleeSavedRegisters
    ; push  ebx

    push    esi
    push    edi

    ; push ArgumentRegisters
    push    ecx
    push    edx

    mov     esi, esp

    push    [esi + 4*4]     ; dispatch token
    push    [esi + 5*4]     ; siteAddrForRegisterIndirect
    push    esi             ; pTransitionBlock

    ; Setup up proper EBP frame now that the stub arguments can be trashed
    mov     [esi + 4*4], ebx
    mov     [esi + 5*4], ebp
    lea     ebp, [esi + 5*4]

    ; Make the call
    call    _VSD_ResolveWorker@12

    ; From here on, mustn't trash eax

    STUB_EPILOG
    jmp     eax

    ; This will never be executed. It is just to help out stack-walking logic
    ; which disassembles the epilog to unwind the stack.
    ret
_ResolveWorkerAsmStub@0 endp

ifdef CHAIN_LOOKUP
;==========================================================================
; This will perform a chained lookup of the entry if the initial cache lookup fails
;
; Entry stack:
;          dispatch token
;          siteAddrForRegisterIndirect (used only if this is a RegisterIndirect dispatch call)
;          return address of caller to stub
; Also, EAX contains the pointer to the first ResolveCacheElem pointer for the calculated
; bucket in the cache table.
;==========================================================================
_ResolveWorkerChainLookupAsmStub@0 proc public
    ; this is the part of the stack that is present as we enter this function:

    ChainLookup__token equ                  00h
    ChainLookup__indirect_addr equ          04h
    ChainLookup__caller_ret_addr equ        08h
    ChainLookup__ret_esp equ                0ch

    ChainLookup_spilled_reg_size equ        8

ifdef STUB_LOGGING
    inc     g_chained_lookup_call_counter
endif

    ; spill regs
    push    edx
    push    ecx

    ; move the token into edx
    mov     edx, [esp + ChainLookup_spilled_reg_size + ChainLookup__token]

    ; move the MT into ecx
    mov     ecx, [ecx]

main_loop:

    ; get the next entry in the chain (don't bother checking the first entry again)
    mov     eax, [eax + ResolveCacheElem__pNext]

    ; test if we hit a terminating NULL
    test    eax, eax
    jz      fail

    ; compare the MT of the ResolveCacheElem
    cmp     ecx, [eax + ResolveCacheElem__pMT]
    jne     main_loop

    ; compare the token of the ResolveCacheElem
    cmp     edx, [eax + ResolveCacheElem__token]
    jne     main_loop

    ; success
    ; decrement success counter and move entry to start if necessary
    sub     g_dispatch_cache_chain_success_counter, 1

    ; @TODO: Perhaps this should be a jl for better branch prediction?
    jge     nopromote

    ; be quick to reset the counter so we don't get a bunch of contending threads
    add     g_dispatch_cache_chain_success_counter, ASM__CALL_STUB_CACHE_INITIAL_SUCCESS_COUNT

    ; promote the entry to the beginning of the chain
    mov     ecx, eax
    call    @VSD_PromoteChainEntry@4

nopromote:
    pop     ecx
    pop     edx
    add     esp, (ChainLookup__caller_ret_addr - ChainLookup__token)
    mov     eax, [eax + ResolveCacheElem__target]
    jmp     eax

fail:
ifdef STUB_LOGGING
    inc     g_chained_lookup_miss_counter
endif

    ; restore registers
    pop     ecx
    pop     edx
    jmp     _ResolveWorkerAsmStub@0
_ResolveWorkerChainLookupAsmStub@0 endp
endif ; CHAIN_LOOKUP

;==========================================================================
; Call the callsite back patcher.  The fail stub piece of the resolver is being
; call too often, i.e. dispatch stubs are failing the expect MT test too often.
; In this stub wraps the call to the BackPatchWorker to take care of any stack magic
; needed.
;==========================================================================
_BackPatchWorkerAsmStub@0 proc public
    push    ebp
    mov     ebp, esp

    push    eax                 ; it may contain siteAddrForRegisterIndirect
    push    ecx
    push    edx

    push    eax                 ; push any indirect call address as the second arg to BackPatchWorker
    push    dword ptr [ebp + 8] ; and push return address as the first arg to BackPatchWorker
    call    _BackPatchWorkerStaticStub@8

    pop     edx
    pop     ecx
    pop     eax

    mov     esp, ebp
    pop     ebp
    ret
_BackPatchWorkerAsmStub@0 endp

;==========================================================================
; Capture a transition block with register values and call the IL_Throw
; implementation written in C.
;
; Input state:
;   ECX = Pointer to exception object
;==========================================================================
FASTCALL_FUNC IL_Throw, 4
    STUB_PROLOG

    mov     edx, esp
    call    @IL_Throw_x86@8

    STUB_EPILOG
    ret     4
FASTCALL_ENDFUNC IL_Throw

;==========================================================================
; Capture a transition block with register values and call the IL_ThrowExact
; implementation written in C.
;
; Input state:
;   ECX = Pointer to exception object
;==========================================================================
FASTCALL_FUNC IL_ThrowExact, 4
    STUB_PROLOG

    mov     edx, esp
    call    @IL_ThrowExact_x86@8

    STUB_EPILOG
    ret     4
FASTCALL_ENDFUNC IL_ThrowExact

;==========================================================================
; Capture a transition block with register values and call the IL_Rethrow
; implementation written in C.
;==========================================================================
FASTCALL_FUNC IL_Rethrow, 0
    STUB_PROLOG

    mov     ecx, esp
    call    @IL_Rethrow_x86@4

    STUB_EPILOG
    ret     4
FASTCALL_ENDFUNC IL_Rethrow

    end
