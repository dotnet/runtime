;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.


        .586
        .model  flat
        option  casemap:none
        .code


include AsmMacros_Shared.inc

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
FASTCALL_FUNC  RhpThrowHwEx, 8

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

ALTERNATE_ENTRY _RhpThrowHwEx2

        ;; no return
        int 3

FASTCALL_ENDFUNC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpThrowExact
;;
;; SUMMARY:  Similar to RhpThrowEx, except that it sets the rethrow flag
;;
;; INPUT:  ECX:  exception object
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpThrowExact, 4

        mov         edx, 4                  ;; edx = ExKind.RethrowFlag
        jmp         RhpThrowImpl

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
FASTCALL_FUNC  RhpThrowEx, 4

        mov         edx, 1                  ;; edx = ExKind.Throw
        jmp         RhpThrowImpl

FASTCALL_ENDFUNC

FASTCALL_FUNC  RhpThrowImpl, 8
        esp_offsetof_ExInfo     textequ %0
        esp_offsetof_Context    textequ %SIZEOF__ExInfo

        push        ebp
        mov         ebp, esp

        lea         eax, [esp+8]    ;; calculate the RSP of the throw site

;;  struct PAL_LIMITED_CONTEXT
;;  {
        push        ebx
        push        eax
        push        esi
        push        edi
        mov         ebx, [ebp]
        push        ebx     ;; 'faulting' Rbp
        push        eax     ;; 'faulting' Rsp
        mov         eax, [ebp+4]    ;; get the throw site IP via the return address
        push        eax     ;; 'faulting' IP
;;  };

        sub         esp, SIZEOF__ExInfo

        ;; -------------------------

        lea                     ebx, [ebp+4]    ;; ebx <- addr of return address
        INLINE_GETTHREAD        eax, edi        ;; eax <- thread, edi <- trashed

        ;; There is runtime C# code that can tail call to RhpThrowEx using a binder intrinsic.  So the return
        ;; address could have been hijacked when we were in that C# code and we must remove the hijack and
        ;; reflect the correct return address in our exception context record.  The other throw helpers don't
        ;; need this because they cannot be tail-called from C#.

        INLINE_THREAD_UNHIJACK  eax, esi, edi       ;; trashes esi, edi

        mov                     edi, [ebx]          ;; edi <- return address
        mov                     [esp + esp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__IP], edi   ;; set 'faulting' IP after unhijack

        lea     edi, [esp + esp_offsetof_ExInfo]    ;; edi <- ExInfo*

        xor     esi, esi
        mov     [edi + OFFSETOF__ExInfo__m_exception], esi          ;; init the exception object to null
        mov     byte ptr [edi + OFFSETOF__ExInfo__m_passNumber], 1  ;; init to the first pass
        mov     dword ptr [edi + OFFSETOF__ExInfo__m_idxCurClause], 0FFFFFFFFh
        mov     byte ptr [edi + OFFSETOF__ExInfo__m_kind], dl       ;; ExKind

        ;; link the ExInfo into the thread's ExInfo chain
        mov     ebx, [eax + OFFSETOF__Thread__m_pExInfoStackHead]
        mov     [edi + OFFSETOF__ExInfo__m_pPrevExInfo], ebx        ;; pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        mov     [eax + OFFSETOF__Thread__m_pExInfoStackHead], edi   ;; m_pExInfoStackHead = pExInfo

        ;; set the exception context field on the ExInfo
        lea     ebx, [esp + esp_offsetof_Context]                   ;; ebx <- PAL_LIMITED_CONTEXT*
        mov     [edi + OFFSETOF__ExInfo__m_pExContext], ebx         ;; init ExInfo.m_pExContext
        mov     edx, edi

        ;; ecx still contains the exception object
        ;; edx contains the address of the ExInfo
        call    RhThrowEx

ALTERNATE_ENTRY _RhpThrowImpl2

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

ALTERNATE_ENTRY _RhpRethrow2

        ;; no return
        int 3

FASTCALL_ENDFUNC

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void FASTCALL RhpResumeAfterCatch(REGDISPLAY* pRegDisplay, void* pResumeIP)
;;
;; Resumes execution after a catch funclet called by CallEHFunclet has returned and the dispatcher has popped
;; the ExInfos below the resume SP. Does not return.
;;
;; INPUT:  ECX:  REGDISPLAY* of the frame that contains the catch
;;         EDX:  resume address returned by the catch funclet
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
FASTCALL_FUNC  RhpResumeAfterCatch, 8

        ;; The preserved registers are reloaded from the REGDISPLAY below.
        mov         esi, ecx
        mov         edi, edx

        INLINE_GETTHREAD    eax, ebx                                ;; eax <- Thread*, ebx is trashed

        ;; Clear the DoNotTriggerGc state before resuming in the managed code after the catch.
        lock and            dword ptr [eax + OFFSETOF__Thread__m_ThreadStateFlags], NOT TSF_DoNotTriggerGc

        ;; We must unhijack the thread at this point because the section of stack where the hijack is applied
        ;; may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK eax, ecx, edx                        ;; Thread in eax, trashes ecx and edx

        mov         ecx, esi
        mov         edx, edi

        LOAD_FUNCLET_REGISTER ecx, Ebx
        LOAD_FUNCLET_REGISTER ecx, Esi
        LOAD_FUNCLET_REGISTER ecx, Edi
        LOAD_FUNCLET_REGISTER ecx, Ebp

        ;; reset ESP and jump to the continuation address
        mov         esp, [ecx + OFFSETOF__REGDISPLAY__ResumeSP]
        jmp         edx

FASTCALL_ENDFUNC

        end
