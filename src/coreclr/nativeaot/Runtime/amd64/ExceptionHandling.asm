;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include asmmacros.inc

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpThrowHwEx
;;
;; INPUT:  RCX:  exception code of fault
;;         RDX:  faulting RIP
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY RhpThrowHwEx, _TEXT

ALTERNATE_ENTRY RhpThrowHwExGEHCONT ; this needs to be an EHCONT target since we'll be context-jumping here.

.GEHCONT RhpThrowHwExGEHCONT

        SIZEOF_XmmSaves equ SIZEOF__PAL_LIMITED_CONTEXT - OFFSETOF__PAL_LIMITED_CONTEXT__Xmm6
        STACKSIZEOF_ExInfo equ ((SIZEOF__ExInfo + 15) AND (NOT 15))

        SIZEOF_OutgoingScratch  equ 20h
        rsp_offsetof_ExInfo     equ SIZEOF_OutgoingScratch
        rsp_offsetof_Context    equ SIZEOF_OutgoingScratch + STACKSIZEOF_ExInfo

        mov     rax, rsp        ;; save the faulting RSP

        ;; Align the stack towards zero
        and     rsp, -16

        ;; Push the expected "machine frame" for the unwinder to see.  All that it looks at is the faulting
        ;; RSP and RIP, so we push zero for the others.
        xor     r8, r8
        push    r8              ;; SS
        push    rax             ;; faulting RSP
        pushfq                  ;; EFLAGS
        push    r8              ;; CS
        push    rdx             ;; faulting RIP

        ; Tell the unwinder that the frame is there now
        .pushframe

        alloc_stack     SIZEOF_XmmSaves + 8h    ;; reserve stack for the xmm saves (+8h to realign stack)
        rdsspq  r8                              ;; nop if SSP is not implemented, 0 if not enabled
        push_vol_reg    r8                      ;; SSP
        xor     r8, r8
        push_nonvol_reg r15
        push_nonvol_reg r14
        push_nonvol_reg r13
        push_nonvol_reg r12
        push_nonvol_reg rbx
        push_vol_reg    r8
        push_nonvol_reg rsi
        push_nonvol_reg rdi
        push_nonvol_reg rbp
        push_vol_reg    rax             ;; faulting RSP
        push_vol_reg    rdx             ;; faulting IP

        ;; allocate outgoing args area and space for the ExInfo
        alloc_stack     SIZEOF_OutgoingScratch + STACKSIZEOF_ExInfo

        save_xmm128_postrsp     Xmm6 , rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm6
        save_xmm128_postrsp     Xmm7 , rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm7
        save_xmm128_postrsp     Xmm8 , rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm8
        save_xmm128_postrsp     Xmm9 , rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm9
        save_xmm128_postrsp     Xmm10, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm10
        save_xmm128_postrsp     Xmm11, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm11
        save_xmm128_postrsp     Xmm12, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm12
        save_xmm128_postrsp     Xmm13, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm13
        save_xmm128_postrsp     Xmm14, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm14
        save_xmm128_postrsp     Xmm15, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm15

        END_PROLOGUE

        INLINE_GETTHREAD    rax, rbx                                ;; rax <- Thread*, rbx is trashed

        lea     rdx, [rsp + rsp_offsetof_ExInfo]                    ;; rdx <- ExInfo*

        xor     r8, r8
        mov     [rdx + OFFSETOF__ExInfo__m_exception], r8           ;; init the exception object to null
        mov     byte ptr [rdx + OFFSETOF__ExInfo__m_passNumber], 1  ;; init to the first pass
        mov     dword ptr [rdx + OFFSETOF__ExInfo__m_idxCurClause], 0FFFFFFFFh
        mov     byte ptr [rdx + OFFSETOF__ExInfo__m_kind], 2        ;; ExKind.HardwareFault

        ;; link the ExInfo into the thread's ExInfo chain
        mov     r8, [rax + OFFSETOF__Thread__m_pExInfoStackHead]
        mov     [rdx + OFFSETOF__ExInfo__m_pPrevExInfo], r8         ;; pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        mov     [rax + OFFSETOF__Thread__m_pExInfoStackHead], rdx   ;; m_pExInfoStackHead = pExInfo

        ;; set the exception context field on the ExInfo
        lea     r8, [rsp + rsp_offsetof_Context]                    ;; r8 <- PAL_LIMITED_CONTEXT*
        mov     [rdx + OFFSETOF__ExInfo__m_pExContext], r8          ;; init ExInfo.m_pExContext

        ;; rcx still contains the exception code
        ;; rdx contains the address of the ExInfo
        call    RhThrowHwEx

ALTERNATE_ENTRY RhpThrowHwEx2

        ;; no return
        int 3

NESTED_END RhpThrowHwEx, _TEXT

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpThrowExact
;;
;; SUMMARY:  Similar to RhpThrowEx, except that it sets the rethrow flag
;;
;; INPUT:  RCX:  exception object
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
LEAF_ENTRY RhpThrowExact, _TEXT

        mov     r9d, 4                  ;; r9d = ExKind.RethrowFlag
        jmp     RhpThrowImpl

LEAF_END RhpThrowExact, _TEXT

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpThrowEx
;;
;; INPUT:  RCX:  exception object
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
LEAF_ENTRY RhpThrowEx, _TEXT

        mov     r9d, 1                  ;; r9d = ExKind.Throw
        jmp     RhpThrowImpl
LEAF_END RhpThrowEx, _TEXT


NESTED_ENTRY RhpThrowImpl, _TEXT

        SIZEOF_XmmSaves equ SIZEOF__PAL_LIMITED_CONTEXT - OFFSETOF__PAL_LIMITED_CONTEXT__Xmm6
        STACKSIZEOF_ExInfo equ ((SIZEOF__ExInfo + 15) AND (NOT 15))

        SIZEOF_OutgoingScratch  equ 20h
        rsp_offsetof_ExInfo     equ SIZEOF_OutgoingScratch
        rsp_offsetof_Context    equ SIZEOF_OutgoingScratch + STACKSIZEOF_ExInfo

        lea     rax, [rsp+8]    ;; save the RSP of the throw site
        mov     rdx, [rsp]      ;; get return address

        xor     r8, r8

        alloc_stack     SIZEOF_XmmSaves + 8h    ;; reserve stack for the xmm saves (+8h to realign stack)
        rdsspq  r8                              ;; nop if SSP is not implemented, 0 if not enabled
        test    r8, r8
        je      @f
        add     r8, 8                           ;; Move SSP to match RSP of the throw site
    @@:
        push_vol_reg    r8                      ;; SSP
        xor     r8, r8
        push_nonvol_reg r15
        push_nonvol_reg r14
        push_nonvol_reg r13
        push_nonvol_reg r12
        push_nonvol_reg rbx
        push_vol_reg    r8
        push_nonvol_reg rsi
        push_nonvol_reg rdi
        push_nonvol_reg rbp
        push_vol_reg    rax             ;; 'faulting' RSP
        push_vol_reg    rdx             ;; 'faulting' IP

        ;; allocate outgoing args area and space for the ExInfo
        alloc_stack     SIZEOF_OutgoingScratch + STACKSIZEOF_ExInfo

        save_xmm128_postrsp     Xmm6 , rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm6
        save_xmm128_postrsp     Xmm7 , rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm7
        save_xmm128_postrsp     Xmm8 , rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm8
        save_xmm128_postrsp     Xmm9 , rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm9
        save_xmm128_postrsp     Xmm10, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm10
        save_xmm128_postrsp     Xmm11, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm11
        save_xmm128_postrsp     Xmm12, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm12
        save_xmm128_postrsp     Xmm13, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm13
        save_xmm128_postrsp     Xmm14, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm14
        save_xmm128_postrsp     Xmm15, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm15

        END_PROLOGUE

        INLINE_GETTHREAD        rax, rbx            ;; rax <- Thread*, rbx is trashed

        lea                     rbx, [rsp + rsp_offsetof_Context + SIZEOF__PAL_LIMITED_CONTEXT + 8h]    ;; rbx <- addr of return address

        ;; There is runtime C# code that can tail call to RhpThrowEx using a binder intrinsic.  So the return
        ;; address could have been hijacked when we were in that C# code and we must remove the hijack and
        ;; reflect the correct return address in our exception context record.  The other throw helpers don't
        ;; need this because they cannot be tail-called from C#.
        INLINE_THREAD_UNHIJACK  rax, r10, rdx       ;; trashes R10, RDX
        mov                     rdx, [rbx]          ;; rdx <- return address
        mov                     [rsp + rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__IP], rdx   ;; set 'faulting' IP after unhijack

        lea     rdx, [rsp + rsp_offsetof_ExInfo]                    ;; rdx <- ExInfo*

        mov     [rdx + OFFSETOF__ExInfo__m_exception], r8           ;; init the exception object to null
        mov     byte ptr [rdx + OFFSETOF__ExInfo__m_passNumber], 1  ;; init to the first pass
        mov     dword ptr [rdx + OFFSETOF__ExInfo__m_idxCurClause], 0FFFFFFFFh
        mov     byte ptr [rdx + OFFSETOF__ExInfo__m_kind], r9b      ;; ExKind (from r9b)

        ;; link the ExInfo into the thread's ExInfo chain
        mov     r8, [rax + OFFSETOF__Thread__m_pExInfoStackHead]
        mov     [rdx + OFFSETOF__ExInfo__m_pPrevExInfo], r8         ;; pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        mov     [rax + OFFSETOF__Thread__m_pExInfoStackHead], rdx   ;; m_pExInfoStackHead = pExInfo

        ;; set the exception context field on the ExInfo
        lea     r8, [rsp + rsp_offsetof_Context]                    ;; r8 <- PAL_LIMITED_CONTEXT*
        mov     [rdx + OFFSETOF__ExInfo__m_pExContext], r8          ;; init ExInfo.m_pExContext

        ;; rcx still contains the exception object
        ;; rdx contains the address of the ExInfo
        call    RhThrowEx

ALTERNATE_ENTRY RhpThrowImpl2

        ;; no return
        int 3

NESTED_END RhpThrowImpl, _TEXT

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
NESTED_ENTRY RhpRethrow, _TEXT

        SIZEOF_XmmSaves equ SIZEOF__PAL_LIMITED_CONTEXT - OFFSETOF__PAL_LIMITED_CONTEXT__Xmm6
        STACKSIZEOF_ExInfo equ ((SIZEOF__ExInfo + 15) AND (NOT 15))

        SIZEOF_OutgoingScratch  equ 20h
        rsp_offsetof_ExInfo     equ SIZEOF_OutgoingScratch
        rsp_offsetof_Context    equ SIZEOF_OutgoingScratch + STACKSIZEOF_ExInfo

        lea     rax, [rsp+8]    ;; save the RSP of the throw site
        mov     rdx, [rsp]      ;; get return address

        xor     r8, r8

        alloc_stack     SIZEOF_XmmSaves + 8h    ;; reserve stack for the xmm saves (+8h to realign stack)
        rdsspq  r8                              ;; nop if SSP is not implemented, 0 if not enabled
        test    r8, r8
        je      @f
        add     r8, 8                           ;; Move SSP to match RSP of the throw site
    @@:
        push_vol_reg    r8                      ;; SSP
        xor     r8, r8
        push_nonvol_reg r15
        push_nonvol_reg r14
        push_nonvol_reg r13
        push_nonvol_reg r12
        push_nonvol_reg rbx
        push_vol_reg    r8
        push_nonvol_reg rsi
        push_nonvol_reg rdi
        push_nonvol_reg rbp
        push_vol_reg    rax             ;; 'faulting' RSP
        push_vol_reg    rdx             ;; 'faulting' IP

        ;; allocate outgoing args area and space for the ExInfo
        alloc_stack     SIZEOF_OutgoingScratch + STACKSIZEOF_ExInfo

        save_xmm128_postrsp     Xmm6 , rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm6
        save_xmm128_postrsp     Xmm7 , rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm7
        save_xmm128_postrsp     Xmm8 , rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm8
        save_xmm128_postrsp     Xmm9 , rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm9
        save_xmm128_postrsp     Xmm10, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm10
        save_xmm128_postrsp     Xmm11, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm11
        save_xmm128_postrsp     Xmm12, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm12
        save_xmm128_postrsp     Xmm13, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm13
        save_xmm128_postrsp     Xmm14, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm14
        save_xmm128_postrsp     Xmm15, rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__Xmm15

        END_PROLOGUE

        INLINE_GETTHREAD    rax, rbx                                ;; rax <- Thread*, rbx is trashed

        lea     rdx, [rsp + rsp_offsetof_ExInfo]                    ;; rdx <- ExInfo*

        mov     [rdx + OFFSETOF__ExInfo__m_exception], r8           ;; init the exception object to null
        mov     byte ptr [rdx + OFFSETOF__ExInfo__m_passNumber], 1  ;; init to the first pass
        mov     dword ptr [rdx + OFFSETOF__ExInfo__m_idxCurClause], 0FFFFFFFFh
        mov     byte ptr [rdx + OFFSETOF__ExInfo__m_kind], 0        ;; init to a deterministic value (ExKind.None)


        ;; link the ExInfo into the thread's ExInfo chain
        mov     rcx, [rax + OFFSETOF__Thread__m_pExInfoStackHead]   ;; rcx <- currently active ExInfo
        mov     [rdx + OFFSETOF__ExInfo__m_pPrevExInfo], rcx        ;; pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        mov     [rax + OFFSETOF__Thread__m_pExInfoStackHead], rdx   ;; m_pExInfoStackHead = pExInfo

        ;; set the exception context field on the ExInfo
        lea     r8, [rsp + rsp_offsetof_Context]                    ;; r8 <- PAL_LIMITED_CONTEXT*
        mov     [rdx + OFFSETOF__ExInfo__m_pExContext], r8          ;; init ExInfo.m_pExContext

        ;; rcx contains the currently active ExInfo
        ;; rdx contains the address of the new ExInfo
        call    RhRethrow

ALTERNATE_ENTRY RhpRethrow2

        ;; no return
        int 3

NESTED_END RhpRethrow, _TEXT

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void FASTCALL RhpResumeAfterCatch(REGDISPLAY* pRegDisplay, ExInfo* pExInfo, void* pResumeIP)
;;
;; Resumes execution after a catch funclet called by CallEHFunclet has returned. Does not return.
;;
;; INPUT:  RCX:  REGDISPLAY* of the frame that contains the catch
;;         RDX:  ExInfo*
;;         R8:   resume address returned by the catch funclet
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY RhpResumeAfterCatch, _TEXT

        LOCAL_STACK_USE equ 28h                                     ;; arguments scratch area and stack alignment

        alloc_stack     LOCAL_STACK_USE
        END_PROLOGUE

        ;; The preserved registers are reloaded from the REGDISPLAY before resuming, so they can be used here.
        mov     rbx, rcx                                            ;; rbx <- REGDISPLAY*
        mov     rsi, rdx                                            ;; rsi <- current ExInfo*
        mov     rdi, r8                                             ;; rdi <- resume IP

        INLINE_GETTHREAD    rax, r10                                ;; rax <- Thread*, r10 is trashed
        mov     r12, rax                                            ;; r12 <- Thread*

        ;; Clear the DoNotTriggerGc state before resuming in the managed code after the catch.
        lock and            dword ptr [r12 + OFFSETOF__Thread__m_ThreadStateFlags], NOT TSF_DoNotTriggerGc

ifdef _DEBUG
        ;; Call into some C++ code to validate the pop of the ExInfo.
        mov     rcx, r12                                            ;; rcx <- Thread*
        mov     rdx, rsi                                            ;; rdx <- current ExInfo*
        mov     r8, [rbx + OFFSETOF__REGDISPLAY__SP]                ;; r8  <- resume SP value
        call    RhpValidateExInfoPop
endif

        ;; We must unhijack the thread at this point because the section of stack where the hijack is applied
        ;; may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK r12, rcx, r9                         ;; Thread in r12, trashes rcx and r9

        mov     rcx, rsi                                            ;; rcx <- current ExInfo*
        mov     r8, [rbx + OFFSETOF__REGDISPLAY__SP]                ;; r8 <- resume SP value
        xor     r9, r9                                              ;; r9 <- 0

   @@:  mov     rcx, [rcx + OFFSETOF__ExInfo__m_pPrevExInfo]        ;; rcx <- next ExInfo
        cmp     rcx, r9
        je      @F                                                  ;; we're done if it's null
        cmp     rcx, r8
        jl      @B                                                  ;; keep looping if it's lower than the new SP

   @@:  mov     [r12 + OFFSETOF__Thread__m_pExInfoStackHead], rcx   ;; store the new head on the Thread

   ;; Sanity check: if we have shadow stack, it should agree with what we have in rsp
   ifdef _DEBUG
        rdsspq  r9                                                  ;; NB, r9 == 0 prior to this
        test    r9, r9
        jz      @f
        mov     r9, [r9]
        cmp     [rsp + LOCAL_STACK_USE], r9
        je      @f
        int     3
   @@:
        xor     r9, r9                                              ;; r9 <- 0
   endif

        mov     rax, rdi                                            ;; rax <- resume IP
        mov     rdx, rbx                                            ;; rdx <- REGDISPLAY*
        mov     r11, [rdx + OFFSETOF__REGDISPLAY__SSP]              ;; r11 <- resume SSP value

        mov     rcx, [rdx + OFFSETOF__REGDISPLAY__pRbx]
        mov     rbx, [rcx]
        mov     rcx, [rdx + OFFSETOF__REGDISPLAY__pRbp]
        mov     rbp, [rcx]
        mov     rcx, [rdx + OFFSETOF__REGDISPLAY__pRsi]
        mov     rsi, [rcx]
        mov     rcx, [rdx + OFFSETOF__REGDISPLAY__pRdi]
        mov     rdi, [rcx]
        mov     rcx, [rdx + OFFSETOF__REGDISPLAY__pR12]
        mov     r12, [rcx]
        mov     rcx, [rdx + OFFSETOF__REGDISPLAY__pR13]
        mov     r13, [rcx]
        mov     rcx, [rdx + OFFSETOF__REGDISPLAY__pR14]
        mov     r14, [rcx]
        mov     rcx, [rdx + OFFSETOF__REGDISPLAY__pR15]
        mov     r15, [rcx]

        movdqa  xmm6, [rdx + OFFSETOF__REGDISPLAY__Xmm + 0*10h]
        movdqa  xmm7, [rdx + OFFSETOF__REGDISPLAY__Xmm + 1*10h]
        movdqa  xmm8, [rdx + OFFSETOF__REGDISPLAY__Xmm + 2*10h]
        movdqa  xmm9, [rdx + OFFSETOF__REGDISPLAY__Xmm + 3*10h]
        movdqa  xmm10,[rdx + OFFSETOF__REGDISPLAY__Xmm + 4*10h]
        movdqa  xmm11,[rdx + OFFSETOF__REGDISPLAY__Xmm + 5*10h]
        movdqa  xmm12,[rdx + OFFSETOF__REGDISPLAY__Xmm + 6*10h]
        movdqa  xmm13,[rdx + OFFSETOF__REGDISPLAY__Xmm + 7*10h]
        movdqa  xmm14,[rdx + OFFSETOF__REGDISPLAY__Xmm + 8*10h]
        movdqa  xmm15,[rdx + OFFSETOF__REGDISPLAY__Xmm + 9*10h]

        ;; reset RSP and jump to RAX
        mov     rsp, r8                                             ;; reset the SP to resume SP value

        ;; if have shadow stack, then we need to reconcile it with the rsp change we have just made. (r11 must contain target SSP)
        rdsspq  r9                                                  ;; NB, r9 == 0 prior to this
        test    r9, r9
        je      No_Ssp_Update
        sub     r11, r9
        shr     r11, 3
        ;; the incsspq instruction uses only the lowest 8 bits of the argument, so we need to loop in case the increment is larger than 255
        mov     r9, 255
    Update_Loop:
        cmp     r11, r9
        cmovb   r9, r11
        incsspq r9
        sub     r11, r9
        ja      Update_Loop


No_Ssp_Update:  jmp     rax

NESTED_END RhpResumeAfterCatch, _TEXT

        end
