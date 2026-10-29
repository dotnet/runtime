;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros_Shared.inc

;;
;; Prologue of the funclet calling helpers. NativeAOT's StackFrameIterator::UnwindFuncletInvokeThunk
;; hard-codes the resulting frame layout.
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
;; Epilogue of the funclet calling helpers
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
;; UINT_PTR CallEHFunclet(Object* pThrowable, PCODE pFunclet, EHFuncletRegs* pRegs, UINT_PTR* pFuncletCallerSP)
;;
;; Calls a catch or finally funclet with the preserved registers of the parent frame loaded through the
;; register pointers in pRegs (the REGDISPLAY on NativeAOT), and XMM6-15 loaded from pRegs.
;;
;; On NativeAOT, the funclet runs with TSF_DoNotTriggerGc cleared, and the preserved general purpose registers
;; are written back through pRegs after it returns since a GC during the funclet may have updated them.
;;
;; INPUT:  RCX:  exception object (null for finally funclets)
;;         RDX:  funclet address
;;         R8:   register pointers
;;         R9:   address where the SP of this helper (the funclet's caller) is saved
;;
;; OUTPUT: RAX:  resume address for catch funclets
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY CallEHFunclet, _TEXT

        FUNCLET_CALL_PROLOGUE 2, 1

        rsp_offsetof_thread = rsp_offsetof_locals
        rsp_offsetof_regs = rsp_offsetof_locals + 8

        mov     [r9], rsp

ifdef FEATURE_NATIVEAOT
        mov     [rsp + rsp_offsetof_regs], r8

        INLINE_GETTHREAD    rax, r10                                ;; rax <- Thread*, r10 is trashed
        mov     [rsp + rsp_offsetof_thread], rax

        ;; Clear the DoNotTriggerGc state before calling out to the managed funclet.
        lock and            dword ptr [rax + OFFSETOF__Thread__m_ThreadStateFlags], NOT TSF_DoNotTriggerGc
endif

        mov     rax, [r8 + OFFSETOF__EHFuncletRegs__pRbx]
        mov     rbx, [rax]
        mov     rax, [r8 + OFFSETOF__EHFuncletRegs__pRbp]
        mov     rbp, [rax]
        mov     rax, [r8 + OFFSETOF__EHFuncletRegs__pRsi]
        mov     rsi, [rax]
        mov     rax, [r8 + OFFSETOF__EHFuncletRegs__pRdi]
        mov     rdi, [rax]
        mov     rax, [r8 + OFFSETOF__EHFuncletRegs__pR12]
        mov     r12, [rax]
        mov     rax, [r8 + OFFSETOF__EHFuncletRegs__pR13]
        mov     r13, [rax]
        mov     rax, [r8 + OFFSETOF__EHFuncletRegs__pR14]
        mov     r14, [rax]
        mov     rax, [r8 + OFFSETOF__EHFuncletRegs__pR15]
        mov     r15, [rax]

        movdqa  xmm6, [r8 + OFFSETOF__EHFuncletRegs__Xmm + 0*10h]
        movdqa  xmm7, [r8 + OFFSETOF__EHFuncletRegs__Xmm + 1*10h]
        movdqa  xmm8, [r8 + OFFSETOF__EHFuncletRegs__Xmm + 2*10h]
        movdqa  xmm9, [r8 + OFFSETOF__EHFuncletRegs__Xmm + 3*10h]
        movdqa  xmm10,[r8 + OFFSETOF__EHFuncletRegs__Xmm + 4*10h]
        movdqa  xmm11,[r8 + OFFSETOF__EHFuncletRegs__Xmm + 5*10h]
        movdqa  xmm12,[r8 + OFFSETOF__EHFuncletRegs__Xmm + 6*10h]
        movdqa  xmm13,[r8 + OFFSETOF__EHFuncletRegs__Xmm + 7*10h]
        movdqa  xmm14,[r8 + OFFSETOF__EHFuncletRegs__Xmm + 8*10h]
        movdqa  xmm15,[r8 + OFFSETOF__EHFuncletRegs__Xmm + 9*10h]

        call    rdx

ALTERNATE_ENTRY CallEHFunclet2

ifdef FEATURE_NATIVEAOT
        mov     rdx, [rsp + rsp_offsetof_regs]
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pRbx]
        mov     [rcx], rbx
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pRbp]
        mov     [rcx], rbp
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pRsi]
        mov     [rcx], rsi
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pRdi]
        mov     [rcx], rdi
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pR12]
        mov     [rcx], r12
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pR13]
        mov     [rcx], r13
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pR14]
        mov     [rcx], r14
        mov     rcx, [rdx + OFFSETOF__EHFuncletRegs__pR15]
        mov     [rcx], r15

        ;; XMM6-15 do not need copy-back (no GC adjustment required).

        ;; Suppress GC again until the dispatcher calls the next funclet or resumes after the catch.
        mov     rcx, [rsp + rsp_offsetof_thread]
        lock or             dword ptr [rcx + OFFSETOF__Thread__m_ThreadStateFlags], TSF_DoNotTriggerGc
endif

        FUNCLET_CALL_EPILOGUE
        ret

NESTED_END CallEHFunclet, _TEXT

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; UINT_PTR CallEHFilterFunclet(Object* pThrowable, TADDR parentFP, PCODE pFilter, UINT_PTR* pFuncletCallerSP)
;;
;; INPUT:  RCX:  exception object
;;         RDX:  RBP of the parent method
;;         R8:   filter funclet address
;;         R9:   address where the SP of this helper (the funclet's caller) is saved
;;
;; OUTPUT: RAX:  result of the filter execution
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
NESTED_ENTRY CallEHFilterFunclet, _TEXT

        FUNCLET_CALL_PROLOGUE 0, 1

        mov     [r9], rsp
        mov     rbp, rdx

        call    r8

ALTERNATE_ENTRY CallEHFilterFunclet2

        FUNCLET_CALL_EPILOGUE
        ret

NESTED_END CallEHFilterFunclet, _TEXT

        end
