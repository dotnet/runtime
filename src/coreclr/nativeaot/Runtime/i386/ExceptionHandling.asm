;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.


        .586
        .model  flat
        option  casemap:none
        .code


include AsmMacros.inc

RhpCallFunclet equ @RhpCallFunclet@0
RhpThrowHwEx equ @RhpThrowHwEx@0

extern RhpCallFunclet : proc

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpThrowHwEx
;;
;; INPUT:  ECX:  exception code of fault
;;         EDX:  faulting RIP
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpThrowHwEx, 0

        esp_offsetof_ExInfo     textequ %0
        esp_offsetof_Context    textequ %SIZEOF__ExInfo

        push    edx         ; make it look like we were called by pushing the faulting IP like a return address
        push    ebp
        mov     ebp, esp

        lea     eax, [esp+8]    ;; calculate the RSP of the throw site
                                ;; edx already contains the throw site IP

;;  struct PAL_LIMITED_CONTEXT
;;  {
        push        ebx
        push        eax
        push        esi
        push        edi
        mov         ebx, [ebp]
        push        ebx     ;; 'faulting' Rbp
        push        eax     ;; 'faulting' Rsp
        push        edx     ;; 'faulting' IP
;;  };

        sub         esp, SIZEOF__ExInfo

        INLINE_GETTHREAD        eax, edx        ;; eax <- thread, edx <- trashed

        lea     edx, [esp + esp_offsetof_ExInfo]                    ;; edx <- ExInfo*

        xor     esi, esi
        mov     [edx + OFFSETOF__ExInfo__m_exception], esi          ;; init the exception object to null
        mov     byte ptr [edx + OFFSETOF__ExInfo__m_passNumber], 1  ;; init to the first pass
        mov     dword ptr [edx + OFFSETOF__ExInfo__m_idxCurClause], 0FFFFFFFFh
        mov     byte ptr [edx + OFFSETOF__ExInfo__m_kind], 2        ;; ExKind.HardwareFault

        ;; link the ExInfo into the thread's ExInfo chain
        mov     ebx, [eax + OFFSETOF__Thread__m_pExInfoStackHead]
        mov     [edx + OFFSETOF__ExInfo__m_pPrevExInfo], ebx        ;; pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        mov     [eax + OFFSETOF__Thread__m_pExInfoStackHead], edx   ;; m_pExInfoStackHead = pExInfo

        ;; set the exception context field on the ExInfo
        lea     ebx, [esp + esp_offsetof_Context]                   ;; ebx <- PAL_LIMITED_CONTEXT*
        mov     [edx + OFFSETOF__ExInfo__m_pExContext], ebx         ;; init ExInfo.m_pExContext

        ;; ecx still contains the exception code
        ;; edx contains the address of the ExInfo
        call    RhThrowHwEx

        EXPORT_POINTER_TO_ADDRESS _PointerToRhpThrowHwEx2

        ;; no return
        int 3

FASTCALL_ENDFUNC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpThrowEx
;;
;; INPUT:  ECX:  exception object
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpThrowEx, 0

        esp_offsetof_ExInfo     textequ %0
        esp_offsetof_Context    textequ %SIZEOF__ExInfo

        push        ebp
        mov         ebp, esp

        lea         eax, [esp+8]    ;; calculate the RSP of the throw site
        mov         edx, [esp+4]    ;; get the throw site IP via the return address

;;  struct PAL_LIMITED_CONTEXT
;;  {
        push        ebx
        push        eax
        push        esi
        push        edi
        mov         ebx, [ebp]
        push        ebx     ;; 'faulting' Rbp
        push        eax     ;; 'faulting' Rsp
        push        edx     ;; 'faulting' IP
;;  };

        sub         esp, SIZEOF__ExInfo

        ;; -------------------------

        lea                     ebx, [eax-4]    ;; ebx <- addr of return address
        INLINE_GETTHREAD        eax, edx        ;; eax <- thread, edx <- trashed

        ;; There is runtime C# code that can tail call to RhpThrowEx using a binder intrinsic.  So the return
        ;; address could have been hijacked when we were in that C# code and we must remove the hijack and
        ;; reflect the correct return address in our exception context record.  The other throw helpers don't
        ;; need this because they cannot be tail-called from C#.

        INLINE_THREAD_UNHIJACK  eax, esi, edx       ;; trashes esi, edx

        mov                     edx, [ebx]          ;; edx <- return address
        mov                     [esp + esp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__IP], edx   ;; set 'faulting' IP after unhijack

        lea     edx, [esp + esp_offsetof_ExInfo]    ;; edx <- ExInfo*

        xor     esi, esi
        mov     [edx + OFFSETOF__ExInfo__m_exception], esi          ;; init the exception object to null
        mov     byte ptr [edx + OFFSETOF__ExInfo__m_passNumber], 1  ;; init to the first pass
        mov     dword ptr [edx + OFFSETOF__ExInfo__m_idxCurClause], 0FFFFFFFFh
        mov     byte ptr [edx + OFFSETOF__ExInfo__m_kind], 1        ;; ExKind.Throw

        ;; link the ExInfo into the thread's ExInfo chain
        mov     ebx, [eax + OFFSETOF__Thread__m_pExInfoStackHead]
        mov     [edx + OFFSETOF__ExInfo__m_pPrevExInfo], ebx        ;; pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        mov     [eax + OFFSETOF__Thread__m_pExInfoStackHead], edx   ;; m_pExInfoStackHead = pExInfo

        ;; set the exception context field on the ExInfo
        lea     ebx, [esp + esp_offsetof_Context]                   ;; ebx <- PAL_LIMITED_CONTEXT*
        mov     [edx + OFFSETOF__ExInfo__m_pExContext], ebx         ;; init ExInfo.m_pExContext

        ;; ecx still contains the exception object
        ;; edx contains the address of the ExInfo
        call    RhThrowEx

        EXPORT_POINTER_TO_ADDRESS _PointerToRhpThrowEx2

        ;; no return
        int 3

FASTCALL_ENDFUNC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void FASTCALL RhpRethrow()
;;
;; SUMMARY:  Similar to RhpThrowEx, except that it passes along the currently active ExInfo
;;
;; INPUT:
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpRethrow, 0


        esp_offsetof_ExInfo     textequ %0
        esp_offsetof_Context    textequ %SIZEOF__ExInfo

        push        ebp
        mov         ebp, esp

        lea         eax, [esp+8]    ;; calculate the RSP of the throw site
        mov         edx, [esp+4]    ;; get the throw site IP via the return address

;;  struct PAL_LIMITED_CONTEXT
;;  {
        push        ebx
        push        eax
        push        esi
        push        edi
        mov         ebx, [ebp]
        push        ebx     ;; 'faulting' Rbp
        push        eax     ;; 'faulting' Rsp
        push        edx     ;; 'faulting' IP
;;  };

        sub         esp, SIZEOF__ExInfo

        ;; -------------------------

        lea                     ebx, [eax-4]    ;; ebx <- addr of return address
        INLINE_GETTHREAD        eax, edx        ;; eax <- thread, edx <- trashed

        lea     edx, [esp + esp_offsetof_ExInfo]    ;; edx <- ExInfo*

        xor     esi, esi
        mov     [edx + OFFSETOF__ExInfo__m_exception], esi          ;; init the exception object to null
        mov     byte ptr [edx + OFFSETOF__ExInfo__m_passNumber], 1  ;; init to the first pass
        mov     dword ptr [edx + OFFSETOF__ExInfo__m_idxCurClause], 0FFFFFFFFh
        mov     byte ptr [edx + OFFSETOF__ExInfo__m_kind], 0        ;; init to a deterministic value (ExKind.None)

        ;; link the ExInfo into the thread's ExInfo chain
        mov     ecx, [eax + OFFSETOF__Thread__m_pExInfoStackHead]   ;; ecx <- currently active ExInfo
        mov     [edx + OFFSETOF__ExInfo__m_pPrevExInfo], ecx        ;; pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        mov     [eax + OFFSETOF__Thread__m_pExInfoStackHead], edx   ;; m_pExInfoStackHead = pExInfo

        ;; set the exception context field on the ExInfo
        lea     ebx, [esp + esp_offsetof_Context]                   ;; ebx <- PAL_LIMITED_CONTEXT*
        mov     [edx + OFFSETOF__ExInfo__m_pExContext], ebx         ;; init ExInfo.m_pExContext

        ;; ecx contains the currently active ExInfo
        ;; edx contains the address of the new ExInfo
        call    RhRethrow

        EXPORT_POINTER_TO_ADDRESS _PointerToRhpRethrow2

        ;; no return
        int 3

FASTCALL_ENDFUNC

;;
;; Prologue of all funclet calling helpers (RhpCallXXXXFunclet)
;;
FUNCLET_CALL_PROLOGUE macro localsCount
    push        ebp
    mov         ebp, esp

    push        ebx     ;; save preserved registers (for the stackwalker)
    push        esi     ;;
    push        edi     ;;

    stack_alloc_size = localsCount * 4

    if stack_alloc_size ne 0
    sub         esp, stack_alloc_size
    endif
endm

;;
;; Epilogue of all funclet calling helpers (RhpCallXXXXFunclet)
;;
FUNCLET_CALL_EPILOGUE macro
    if stack_alloc_size ne 0
    add         esp, stack_alloc_size
    endif
    pop         edi
    pop         esi
    pop         ebx
    pop         ebp
endm

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* FASTCALL RhpCallCatchFunclet(RtuObjectRef exceptionObj, void* pHandlerIP, REGDISPLAY* pRegDisplay,
;;                                    ExInfo* pExInfo)
;;
;; INPUT:  ECX:         exception object
;;         EDX:         handler funclet address
;;         [ESP + 4]:   REGDISPLAY*
;;         [ESP + 8]:   ExInfo*
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpCallCatchFunclet, 0

        FUNCLET_CALL_PROLOGUE 2

        esp_offsetof_ResumeIP                   textequ %00h        ;; [esp + 00h]: continuation address
        esp_offsetof_is_handling_thread_abort   textequ %04h        ;; [esp + 04h]: set if we are handling ThreadAbortException
                                                                    ;; [esp + 08h]: edi save
                                                                    ;; [esp + 0ch]: esi save
                                                                    ;; [esp + 10h]: ebx save
        esp_offsetof_PrevEBP                    textequ %14h        ;; [esp + 14h]: prev ebp
        esp_offsetof_RetAddr                    textequ %18h        ;; [esp + 18h]: return address
        esp_offsetof_RegDisplay                 textequ %1ch        ;; [esp + 1Ch]: REGDISPLAY*
        esp_offsetof_ExInfo                     textequ %20h        ;; [esp + 20h]: ExInfo*

        ;; Clear the DoNotTriggerGc state before calling out to our managed catch funclet.
        INLINE_GETTHREAD    eax, ebx        ;; eax <- Thread*, ebx is trashed
        lock and            dword ptr [eax + OFFSETOF__Thread__m_ThreadStateFlags], NOT TSF_DoNotTriggerGc

        cmp         ecx, [eax + OFFSETOF__Thread__m_threadAbortException]
        setz        byte ptr [esp + esp_offsetof_is_handling_thread_abort]

        mov         edi, [esp + esp_offsetof_RegDisplay]            ;; edi <- REGDISPLAY *

        mov         eax, [edi + OFFSETOF__REGDISPLAY__pRbx]
        mov         ebx, [eax]

        mov         eax, [edi + OFFSETOF__REGDISPLAY__pRbp]
        mov         eax, [eax]
        push        eax     ; save the funclet's EBP value for later

        mov         eax, [edi + OFFSETOF__REGDISPLAY__pRsi]
        mov         esi, [eax]

        mov         eax, [edi + OFFSETOF__REGDISPLAY__pRdi]
        mov         edi, [eax]

        pop         eax     ; get the funclet's EBP value

        ;; ECX still contains the exception object
        ;; EDX: funclet IP
        ;; EAX: funclet EBP
        call        RhpCallFunclet

        EXPORT_POINTER_TO_ADDRESS _PointerToRhpCallCatchFunclet2

        ;; eax: resume IP
        mov         [esp + esp_offsetof_ResumeIP], eax              ;; save for later

        INLINE_GETTHREAD edx, ecx                                   ;; edx <- Thread*, trash ecx

        ;; We must unhijack the thread at this point because the section of stack where the hijack is applied
        ;; may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK edx, ecx, eax                        ;; Thread in edx, trashes ecx and eax

        mov         ecx, [esp + esp_offsetof_ExInfo]                ;; ecx <- current ExInfo *
        mov         eax, [esp + esp_offsetof_RegDisplay]            ;; eax <- REGDISPLAY*
        mov         eax, [eax + OFFSETOF__REGDISPLAY__SP]           ;; eax <- resume SP value

    @@: mov         ecx, [ecx + OFFSETOF__ExInfo__m_pPrevExInfo]    ;; ecx <- next ExInfo
        cmp         ecx, 0
        je          @F                                              ;; we're done if it's null
        cmp         ecx, eax
        jl          @B                                              ;; keep looping if it's lower than the new SP

    @@: mov         [edx + OFFSETOF__Thread__m_pExInfoStackHead], ecx   ;; store the new head on the Thread

        test        [RhpTrapThreads], TrapThreadsFlags_AbortInProgress
        jz          @f

        ;; test if the exception handled by the catch was the ThreadAbortException
        cmp         byte ptr [esp + esp_offsetof_is_handling_thread_abort], 0
        je          @f

        ;; RhpCallFunclet preserved our local EBP value, so let's fetch the correct one for the resume address
        mov         ecx, [esp + esp_offsetof_RegDisplay]            ;; ecx <- REGDISPLAY *
        mov         ecx, [ecx + OFFSETOF__REGDISPLAY__pRbp]
        mov         ebp, [ecx]

        ;; It was the ThreadAbortException, so rethrow it
        mov         ecx, STATUS_REDHAWK_THREAD_ABORT
        mov         edx, [esp + esp_offsetof_ResumeIP]
        mov         esp, eax                                        ;; reset the SP to resume SP value
        jmp         RhpThrowHwEx                                    ;; Throw the ThreadAbortException as a special kind of hardware exception

    @@:
        ;; RhpCallFunclet preserved our local EBP value, so let's fetch the correct one for the resume address
        mov         ecx, [esp + esp_offsetof_RegDisplay]            ;; ecx <- REGDISPLAY *
        mov         ecx, [ecx + OFFSETOF__REGDISPLAY__pRbp]
        mov         ebp, [ecx]

        ;; reset ESP and jump to the continuation address
        mov         ecx, [esp + esp_offsetof_ResumeIP]
        mov         esp, eax
        jmp         ecx

FASTCALL_ENDFUNC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void FASTCALL RhpCallFinallyFunclet(void* pHandlerIP, REGDISPLAY* pRegDisplay)
;;
;; INPUT:  ECX:  handler funclet address
;;         EDX:  REGDISPLAY*
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpCallFinallyFunclet, 0

        FUNCLET_CALL_PROLOGUE 0

        push        edx     ;; save REGDISPLAY*

        ;; Clear the DoNotTriggerGc state before calling out to our managed catch funclet.
        INLINE_GETTHREAD    eax, ebx        ;; eax <- Thread*, ebx is trashed
        lock and            dword ptr [eax + OFFSETOF__Thread__m_ThreadStateFlags], NOT TSF_DoNotTriggerGc

        ;;
        ;; load preserved registers for funclet
        ;;

        mov         eax, [edx + OFFSETOF__REGDISPLAY__pRbx]
        mov         ebx, [eax]

        mov         eax, [edx + OFFSETOF__REGDISPLAY__pRsi]
        mov         esi, [eax]

        mov         eax, [edx + OFFSETOF__REGDISPLAY__pRdi]
        mov         edi, [eax]

        mov         eax, [edx + OFFSETOF__REGDISPLAY__pRbp]
        mov         eax, [eax]
        mov         edx, ecx

        ;; ECX: not used
        ;; EDX: funclet IP
        ;; EAX: funclet EBP
        call        RhpCallFunclet

        EXPORT_POINTER_TO_ADDRESS _PointerToRhpCallFinallyFunclet2

        pop         edx     ;; restore REGDISPLAY*

        ;;
        ;; save preserved registers from funclet
        ;;
        mov         eax, [edx + OFFSETOF__REGDISPLAY__pRbx]
        mov         [eax], ebx

        mov         eax, [edx + OFFSETOF__REGDISPLAY__pRsi]
        mov         [eax], esi

        mov         eax, [edx + OFFSETOF__REGDISPLAY__pRdi]
        mov         [eax], edi

        INLINE_GETTHREAD    eax, ebx        ;; eax <- Thread*, ebx is trashed
        lock or             dword ptr [eax + OFFSETOF__Thread__m_ThreadStateFlags], TSF_DoNotTriggerGc

        FUNCLET_CALL_EPILOGUE
        ret

FASTCALL_ENDFUNC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* FASTCALL RhpCallFilterFunclet(RtuObjectRef exceptionObj, void* pFilterIP, REGDISPLAY* pRegDisplay)
;;
;; INPUT:  ECX:         exception object
;;         EDX:         filter funclet address
;;         [ESP + 4]:   REGDISPLAY*
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpCallFilterFunclet, 0

        FUNCLET_CALL_PROLOGUE 0

        push        edx     ;; save filter funclet address

        ;;
        ;; load preserved registers for funclet
        ;;
        mov         edx, [ebp + 8]
        mov         eax, [edx + OFFSETOF__REGDISPLAY__pRbp]
        mov         eax, [eax]

        ;; ECX still contains exception object
        ;; EAX contains the funclet EBP value
        mov         edx, [esp + 0]                  ;; reload filter funclet address

        call        RhpCallFunclet

        EXPORT_POINTER_TO_ADDRESS _PointerToRhpCallFilterFunclet2

        ;; EAX contains the result of the filter execution
        mov         edx, [ebp + 8]

        pop         ecx         ;; pop scratch slot

        FUNCLET_CALL_EPILOGUE
        ret

FASTCALL_ENDFUNC

        end
