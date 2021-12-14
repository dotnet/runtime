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
        push_vol_reg    r8                      ;; padding
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

        EXPORT_POINTER_TO_ADDRESS PointerToRhpThrowHwEx2

        ;; no return
        int 3

NESTED_END RhpThrowHwEx, _TEXT

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpThrowEx
;;
;; INPUT:  RCX:  exception object
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY RhpThrowEx, _TEXT

        SIZEOF_XmmSaves equ SIZEOF__PAL_LIMITED_CONTEXT - OFFSETOF__PAL_LIMITED_CONTEXT__Xmm6
        STACKSIZEOF_ExInfo equ ((SIZEOF__ExInfo + 15) AND (NOT 15))

        SIZEOF_OutgoingScratch  equ 20h
        rsp_offsetof_ExInfo     equ SIZEOF_OutgoingScratch
        rsp_offsetof_Context    equ SIZEOF_OutgoingScratch + STACKSIZEOF_ExInfo

        lea     rax, [rsp+8]    ;; save the RSP of the throw site
        mov     rdx, [rsp]      ;; get return address

        xor     r8, r8

        alloc_stack     SIZEOF_XmmSaves + 8h    ;; reserve stack for the xmm saves (+8h to realign stack)
        push_vol_reg    r8                      ;; padding
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
        INLINE_THREAD_UNHIJACK  rax, r9, rdx        ;; trashes R9, RDX
        mov                     rdx, [rbx]          ;; rdx <- return address
        mov                     [rsp + rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__IP], rdx   ;; set 'faulting' IP after unhijack

        lea     rdx, [rsp + rsp_offsetof_ExInfo]                    ;; rdx <- ExInfo*

        mov     [rdx + OFFSETOF__ExInfo__m_exception], r8           ;; init the exception object to null
        mov     byte ptr [rdx + OFFSETOF__ExInfo__m_passNumber], 1  ;; init to the first pass
        mov     dword ptr [rdx + OFFSETOF__ExInfo__m_idxCurClause], 0FFFFFFFFh
        mov     byte ptr [rdx + OFFSETOF__ExInfo__m_kind], 1        ;; ExKind.Throw

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

        EXPORT_POINTER_TO_ADDRESS PointerToRhpThrowEx2

        ;; no return
        int 3

NESTED_END RhpThrowEx, _TEXT

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
        push_vol_reg    r8                      ;; padding
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

        EXPORT_POINTER_TO_ADDRESS PointerToRhpRethrow2

        ;; no return
        int 3

NESTED_END RhpRethrow, _TEXT

;;
;; Prologue of all funclet calling helpers (RhpCallXXXXFunclet)
;;
FUNCLET_CALL_PROLOGUE macro localsCount, alignStack

    push_nonvol_reg r15     ;; save preserved regs for OS stackwalker
    push_nonvol_reg r14     ;; ...
    push_nonvol_reg r13     ;; ...
    push_nonvol_reg r12     ;; ...
    push_nonvol_reg rbx     ;; ...
    push_nonvol_reg rsi     ;; ...
    push_nonvol_reg rdi     ;; ...
    push_nonvol_reg rbp     ;; ...

    arguments_scratch_area_size = 20h
    xmm_save_area_size = 10 * 10h ;; xmm6..xmm15 save area
    stack_alloc_size = arguments_scratch_area_size + localsCount * 8 + alignStack * 8 + xmm_save_area_size
    rsp_offsetof_arguments = stack_alloc_size + 8*8h + 8h
    rsp_offsetof_locals = arguments_scratch_area_size + xmm_save_area_size

    alloc_stack     stack_alloc_size

    save_xmm128_postrsp xmm6,  (arguments_scratch_area_size + 0 * 10h)
    save_xmm128_postrsp xmm7,  (arguments_scratch_area_size + 1 * 10h)
    save_xmm128_postrsp xmm8,  (arguments_scratch_area_size + 2 * 10h)
    save_xmm128_postrsp xmm9,  (arguments_scratch_area_size + 3 * 10h)
    save_xmm128_postrsp xmm10, (arguments_scratch_area_size + 4 * 10h)
    save_xmm128_postrsp xmm11, (arguments_scratch_area_size + 5 * 10h)
    save_xmm128_postrsp xmm12, (arguments_scratch_area_size + 6 * 10h)
    save_xmm128_postrsp xmm13, (arguments_scratch_area_size + 7 * 10h)
    save_xmm128_postrsp xmm14, (arguments_scratch_area_size + 8 * 10h)
    save_xmm128_postrsp xmm15, (arguments_scratch_area_size + 9 * 10h)

    END_PROLOGUE
endm

;;
;; Epilogue of all funclet calling helpers (RhpCallXXXXFunclet)
;;
FUNCLET_CALL_EPILOGUE macro
    movdqa  xmm6,  [rsp + arguments_scratch_area_size + 0 * 10h]
    movdqa  xmm7,  [rsp + arguments_scratch_area_size + 1 * 10h]
    movdqa  xmm8,  [rsp + arguments_scratch_area_size + 2 * 10h]
    movdqa  xmm9,  [rsp + arguments_scratch_area_size + 3 * 10h]
    movdqa  xmm10, [rsp + arguments_scratch_area_size + 4 * 10h]
    movdqa  xmm11, [rsp + arguments_scratch_area_size + 5 * 10h]
    movdqa  xmm12, [rsp + arguments_scratch_area_size + 6 * 10h]
    movdqa  xmm13, [rsp + arguments_scratch_area_size + 7 * 10h]
    movdqa  xmm14, [rsp + arguments_scratch_area_size + 8 * 10h]
    movdqa  xmm15, [rsp + arguments_scratch_area_size + 9 * 10h]

    add     rsp, stack_alloc_size
    pop     rbp
    pop     rdi
    pop     rsi
    pop     rbx
    pop     r12
    pop     r13
    pop     r14
    pop     r15
endm

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* FASTCALL RhpCallCatchFunclet(RtuObjectRef exceptionObj, void* pHandlerIP, REGDISPLAY* pRegDisplay,
;;                                    ExInfo* pExInfo)
;;
;; INPUT:  RCX:  exception object
;;         RDX:  handler funclet address
;;         R8:   REGDISPLAY*
;;         R9:   ExInfo*
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY RhpCallCatchFunclet, _TEXT

        FUNCLET_CALL_PROLOGUE 3, 0

        ;; locals
        rsp_offsetof_thread = rsp_offsetof_locals
        rsp_offsetof_resume_ip = rsp_offsetof_locals + 8;
        rsp_offsetof_is_handling_thread_abort = rsp_offsetof_locals + 16;

        mov     [rsp + rsp_offsetof_arguments + 0h], rcx            ;; save arguments for later
        mov     [rsp + rsp_offsetof_arguments + 8h], rdx
        mov     [rsp + rsp_offsetof_arguments + 10h], r8
        mov     [rsp + rsp_offsetof_arguments + 18h], r9

        INLINE_GETTHREAD    rax, rbx                                ;; rax <- Thread*, rbx is trashed
        mov     [rsp + rsp_offsetof_thread], rax                    ;; save Thread* for later

        cmp     rcx, [rax + OFFSETOF__Thread__m_threadAbortException]
        setz    byte ptr [rsp + rsp_offsetof_is_handling_thread_abort]

        ;; Clear the DoNotTriggerGc state before calling out to our managed catch funclet.
        lock and            dword ptr [rax + OFFSETOF__Thread__m_ThreadStateFlags], NOT TSF_DoNotTriggerGc

        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRbx]
        mov     rbx, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRbp]
        mov     rbp, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRsi]
        mov     rsi, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRdi]
        mov     rdi, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pR12]
        mov     r12, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pR13]
        mov     r13, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pR14]
        mov     r14, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pR15]
        mov     r15, [rax]

if 0 ;; _DEBUG  ;; @TODO: temporarily removed because trashing RBP breaks the debugger
        ;; trash the values at the old homes to make sure nobody uses them
        mov     r9, 0baaddeedh
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRbx]
        mov     [rax], r9
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRbp]
        mov     [rax], r9
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRsi]
        mov     [rax], r9
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRdi]
        mov     [rax], r9
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pR12]
        mov     [rax], r9
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pR13]
        mov     [rax], r9
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pR14]
        mov     [rax], r9
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pR15]
        mov     [rax], r9
endif

        movdqa  xmm6, [r8 + OFFSETOF__REGDISPLAY__Xmm + 0*10h]
        movdqa  xmm7, [r8 + OFFSETOF__REGDISPLAY__Xmm + 1*10h]
        movdqa  xmm8, [r8 + OFFSETOF__REGDISPLAY__Xmm + 2*10h]
        movdqa  xmm9, [r8 + OFFSETOF__REGDISPLAY__Xmm + 3*10h]
        movdqa  xmm10,[r8 + OFFSETOF__REGDISPLAY__Xmm + 4*10h]

        movdqa  xmm11,[r8 + OFFSETOF__REGDISPLAY__Xmm + 5*10h]
        movdqa  xmm12,[r8 + OFFSETOF__REGDISPLAY__Xmm + 6*10h]
        movdqa  xmm13,[r8 + OFFSETOF__REGDISPLAY__Xmm + 7*10h]
        movdqa  xmm14,[r8 + OFFSETOF__REGDISPLAY__Xmm + 8*10h]
        movdqa  xmm15,[r8 + OFFSETOF__REGDISPLAY__Xmm + 9*10h]

        mov     rcx, [r8 + OFFSETOF__REGDISPLAY__SP]                ;; rcx <- establisher frame
        mov     rdx, [rsp + rsp_offsetof_arguments + 0h]            ;; rdx <- exception object
        call    qword ptr [rsp + rsp_offsetof_arguments + 8h]       ;; call handler funclet

        EXPORT_POINTER_TO_ADDRESS PointerToRhpCallCatchFunclet2

        mov     r8, [rsp + rsp_offsetof_arguments + 10h]            ;; r8 <- dispatch context

ifdef _DEBUG
        ;; Call into some C++ code to validate the pop of the ExInfo.  We only do this in debug because we
        ;; have to spill all the preserved registers and then refill them after the call.
        mov     [rsp + rsp_offsetof_resume_ip], rax                                    ;; save resume IP for later

        mov     rcx, [r8 + OFFSETOF__REGDISPLAY__pRbx]
        mov     [rcx]                           , rbx
        mov     rcx, [r8 + OFFSETOF__REGDISPLAY__pRbp]
        mov     [rcx]                           , rbp
        mov     rcx, [r8 + OFFSETOF__REGDISPLAY__pRsi]
        mov     [rcx]                           , rsi
        mov     rcx, [r8 + OFFSETOF__REGDISPLAY__pRdi]
        mov     [rcx]                           , rdi
        mov     rcx, [r8 + OFFSETOF__REGDISPLAY__pR12]
        mov     [rcx]                           , r12
        mov     rcx, [r8 + OFFSETOF__REGDISPLAY__pR13]
        mov     [rcx]                           , r13
        mov     rcx, [r8 + OFFSETOF__REGDISPLAY__pR14]
        mov     [rcx]                           , r14
        mov     rcx, [r8 + OFFSETOF__REGDISPLAY__pR15]
        mov     [rcx]                           , r15

        mov     rcx, [rsp + rsp_offsetof_thread]                    ;; rcx <- Thread*
        mov     rdx, [rsp + rsp_offsetof_arguments + 18h]           ;; rdx <- current ExInfo *
        mov     r8, [r8 + OFFSETOF__REGDISPLAY__SP]                 ;; r8  <- resume SP value
        call    RhpValidateExInfoPop

        mov     r8, [rsp + rsp_offsetof_arguments + 10h]            ;; r8 <- dispatch context
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRbx]
        mov     rbx, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRbp]
        mov     rbp, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRsi]
        mov     rsi, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRdi]
        mov     rdi, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pR12]
        mov     r12, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pR13]
        mov     r13, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pR14]
        mov     r14, [rax]
        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pR15]
        mov     r15, [rax]

        mov     rax, [rsp + rsp_offsetof_resume_ip]                 ;; reload resume IP
endif
        mov     rdx, [rsp + rsp_offsetof_thread]                    ;; rdx <- Thread*

        ;; We must unhijack the thread at this point because the section of stack where the hijack is applied
        ;; may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK rdx, rcx, r9                         ;; Thread in rdx, trashes rcx and r9

        mov     rcx, [rsp + rsp_offsetof_arguments + 18h]           ;; rcx <- current ExInfo *
        mov     r8, [r8 + OFFSETOF__REGDISPLAY__SP]                 ;; r8 <- resume SP value
        xor     r9d, r9d                                            ;; r9 <- 0

   @@:  mov     rcx, [rcx + OFFSETOF__ExInfo__m_pPrevExInfo]        ;; rcx <- next ExInfo
        cmp     rcx, r9
        je      @F                                                  ;; we're done if it's null
        cmp     rcx, r8
        jl      @B                                                  ;; keep looping if it's lower than the new SP

   @@:  mov     [rdx + OFFSETOF__Thread__m_pExInfoStackHead], rcx   ;; store the new head on the Thread

        test    [RhpTrapThreads], TrapThreadsFlags_AbortInProgress
        jz      @f

        ;; test if the exception handled by the catch was the ThreadAbortException
        cmp     byte ptr [rsp + rsp_offsetof_is_handling_thread_abort], 0
        je      @f

        ;; It was the ThreadAbortException, so rethrow it
        mov     rcx, STATUS_REDHAWK_THREAD_ABORT
        mov     rdx, rax                                            ;; rdx <- continuation address as exception RIP
        mov     rsp, r8                                             ;; reset the SP to resume SP value
        jmp     RhpThrowHwEx ;; Throw the ThreadAbortException as a special kind of hardware exception

        ;; reset RSP and jump to the continuation address
   @@:  mov     rsp, r8                                             ;; reset the SP to resume SP value
        jmp     rax


NESTED_END RhpCallCatchFunclet, _TEXT

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void FASTCALL RhpCallFinallyFunclet(void* pHandlerIP, REGDISPLAY* pRegDisplay)
;;
;; INPUT:  RCX:  handler funclet address
;;         RDX:  REGDISPLAY*
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY RhpCallFinallyFunclet, _TEXT

        FUNCLET_CALL_PROLOGUE 1, 0

        mov     [rsp + rsp_offsetof_arguments + 0h], rcx            ;; save arguments for later
        mov     [rsp + rsp_offsetof_arguments + 8h], rdx

        rsp_offsetof_thread = rsp_offsetof_locals

        INLINE_GETTHREAD    rax, rbx                                ;; rax <- Thread*, rbx is trashed
        mov     [rsp + rsp_offsetof_thread], rax                    ;; save Thread* for later

        ;;
        ;; We want to suppress hijacking between invocations of subsequent finallys.  We do this because we
        ;; cannot tolerate a GC after one finally has run (and possibly side-effected the GC state of the
        ;; method) and then been popped off the stack, leaving behind no trace of its effect.
        ;;
        ;; So we clear the state before and set it after invocation of the handler.
        ;;
        lock and            dword ptr [rax + OFFSETOF__Thread__m_ThreadStateFlags], NOT TSF_DoNotTriggerGc

        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pRbx]
        mov     rbx, [rax]
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pRbp]
        mov     rbp, [rax]
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pRsi]
        mov     rsi, [rax]
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pRdi]
        mov     rdi, [rax]
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pR12]
        mov     r12, [rax]
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pR13]
        mov     r13, [rax]
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pR14]
        mov     r14, [rax]
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pR15]
        mov     r15, [rax]

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

if 0 ;; _DEBUG ;; @TODO: temporarily removed because trashing RBP breaks the debugger
        ;; trash the values at the old homes to make sure nobody uses them
        mov     r9, 0baaddeedh
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pRbx]
        mov     [rax], r9
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pRbp]
        mov     [rax], r9
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pRsi]
        mov     [rax], r9
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pRdi]
        mov     [rax], r9
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pR12]
        mov     [rax], r9
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pR13]
        mov     [rax], r9
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pR14]
        mov     [rax], r9
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pR15]
        mov     [rax], r9
endif

        mov     rcx, [rdx + OFFSETOF__REGDISPLAY__SP]               ;; rcx <- establisher frame
        call    qword ptr [rsp + rsp_offsetof_arguments + 0h]       ;; handler funclet address

        EXPORT_POINTER_TO_ADDRESS PointerToRhpCallFinallyFunclet2

        mov     rdx, [rsp + rsp_offsetof_arguments + 8h]            ;; rdx <- regdisplay

        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pRbx]
        mov     [rax]                            , rbx
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pRbp]
        mov     [rax]                            , rbp
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pRsi]
        mov     [rax]                            , rsi
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pRdi]
        mov     [rax]                            , rdi
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pR12]
        mov     [rax]                            , r12
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pR13]
        mov     [rax]                            , r13
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pR14]
        mov     [rax]                            , r14
        mov     rax, [rdx + OFFSETOF__REGDISPLAY__pR15]
        mov     [rax]                            , r15

        movdqa  [rdx + OFFSETOF__REGDISPLAY__Xmm + 0*10h], xmm6
        movdqa  [rdx + OFFSETOF__REGDISPLAY__Xmm + 1*10h], xmm7
        movdqa  [rdx + OFFSETOF__REGDISPLAY__Xmm + 2*10h], xmm8
        movdqa  [rdx + OFFSETOF__REGDISPLAY__Xmm + 3*10h], xmm9
        movdqa  [rdx + OFFSETOF__REGDISPLAY__Xmm + 4*10h], xmm10

        movdqa  [rdx + OFFSETOF__REGDISPLAY__Xmm + 5*10h], xmm11
        movdqa  [rdx + OFFSETOF__REGDISPLAY__Xmm + 6*10h], xmm12
        movdqa  [rdx + OFFSETOF__REGDISPLAY__Xmm + 7*10h], xmm13
        movdqa  [rdx + OFFSETOF__REGDISPLAY__Xmm + 8*10h], xmm14
        movdqa  [rdx + OFFSETOF__REGDISPLAY__Xmm + 9*10h], xmm15

        mov     rax, [rsp + rsp_offsetof_thread]                                    ;; rax <- Thread*
        lock or             dword ptr [rax + OFFSETOF__Thread__m_ThreadStateFlags], TSF_DoNotTriggerGc

        FUNCLET_CALL_EPILOGUE

        ret

NESTED_END RhpCallFinallyFunclet, _TEXT

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* FASTCALL RhpCallFilterFunclet(RtuObjectRef exceptionObj, void* pFilterIP, REGDISPLAY* pRegDisplay)
;;
;; INPUT:  RCX:  exception object
;;         RDX:  filter funclet address
;;         R8:   REGDISPLAY*
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY RhpCallFilterFunclet, _TEXT

        FUNCLET_CALL_PROLOGUE 0, 1

        mov     rax, [r8 + OFFSETOF__REGDISPLAY__pRbp]
        mov     rbp, [rax]

        mov     rax, rdx                                            ;; rax <- handler funclet address
        mov     rdx, rcx                                            ;; rdx <- exception object
        mov     rcx, [r8 + OFFSETOF__REGDISPLAY__SP]                ;; rcx <- establisher frame
        call    rax

        EXPORT_POINTER_TO_ADDRESS PointerToRhpCallFilterFunclet2

        ;; RAX contains the result of the filter execution

        FUNCLET_CALL_EPILOGUE

        ret

NESTED_END RhpCallFilterFunclet, _TEXT

        end
