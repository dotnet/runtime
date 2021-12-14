;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros.h"

        TEXTAREA

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpThrowHwEx
;;
;; INPUT:  R0:  exception code of fault
;;         R1:  faulting IP
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY RhpThrowHwEx

#define STACKSIZEOF_ExInfo ((SIZEOF__ExInfo + 7)&(~7))

#define rsp_offsetof_ExInfo  0
#define rsp_offsetof_Context STACKSIZEOF_ExInfo

        PROLOG_NOP mov r2, r0       ;; save exception code into r2
        PROLOG_NOP mov r0, sp       ;; get SP of fault site

        PROLOG_NOP mov lr, r1       ;; set IP of fault site

        ;; Setup a PAL_LIMITED_CONTEXT on the stack {
        PROLOG_NOP vpush {d8-d15}
        PROLOG_NOP push {r0,lr}     ;; push {sp, pc} of fault site
        PROLOG_PUSH_MACHINE_FRAME   ;; unwind code only
        PROLOG_PUSH {r0,r4-r11,lr}
        ;; } end PAL_LIMITED_CONTEXT

        PROLOG_STACK_ALLOC STACKSIZEOF_ExInfo

        ; r0: SP of fault site
        ; r1: IP of fault site
        ; r2: exception code of fault
        ; lr: IP of fault site (as a 'return address')

        mov         r0, r2          ;; r0 <- exception code of fault

        ;; r2 = GetThread(), TRASHES r1
        INLINE_GETTHREAD r2, r1

        add         r1, sp, #rsp_offsetof_ExInfo                    ;; r1 <- ExInfo*
        mov         r3, #0
        str         r3, [r1, #OFFSETOF__ExInfo__m_exception]        ;; pExInfo->m_exception = null
        mov         r3, #1
        strb        r3, [r1, #OFFSETOF__ExInfo__m_passNumber]       ;; pExInfo->m_passNumber = 1
        mov         r3, #0xFFFFFFFF
        str         r3, [r1, #OFFSETOF__ExInfo__m_idxCurClause]     ;; pExInfo->m_idxCurClause = MaxTryRegionIdx
        mov         r3, #2
        strb        r3, [r1, #OFFSETOF__ExInfo__m_kind]             ;; pExInfo->m_kind = ExKind.HardwareFault


        ;; link the ExInfo into the thread's ExInfo chain
        ldr         r3, [r2, #OFFSETOF__Thread__m_pExInfoStackHead]
        str         r3, [r1, #OFFSETOF__ExInfo__m_pPrevExInfo]      ;; pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        str         r1, [r2, #OFFSETOF__Thread__m_pExInfoStackHead] ;; m_pExInfoStackHead = pExInfo

        ;; set the exception context field on the ExInfo
        add         r2, sp, #rsp_offsetof_Context                   ;; r2 <- PAL_LIMITED_CONTEXT*
        str         r2, [r1, #OFFSETOF__ExInfo__m_pExContext]       ;; pExInfo->m_pExContext = pContext

        ;; r0: exception code
        ;; r1: ExInfo*
        bl          RhThrowHwEx

        EXPORT_POINTER_TO_ADDRESS PointerToRhpThrowHwEx2

        ;; no return
        __debugbreak

    NESTED_END RhpThrowHwEx

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; RhpThrowEx
;;
;; INPUT:  R0:  exception object
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY RhpThrowEx

        ;; Setup a PAL_LIMITED_CONTEXT on the stack {
        PROLOG_VPUSH {d8-d15}
        PROLOG_PUSH {r0,lr}         ;; Reserve space for SP and store LR
        PROLOG_PUSH {r0,r4-r11,lr}
        ;; } end PAL_LIMITED_CONTEXT

        PROLOG_STACK_ALLOC STACKSIZEOF_ExInfo

        ;; Compute and save SP at callsite.
        add         r1, sp, #(STACKSIZEOF_ExInfo + SIZEOF__PAL_LIMITED_CONTEXT)
        str         r1, [sp, #(rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__SP)]

        ;; r2 = GetThread(), TRASHES r1
        INLINE_GETTHREAD r2, r1

        ;; There is runtime C# code that can tail call to RhpThrowEx using a binder intrinsic.  So the return
        ;; address could have been hijacked when we were in that C# code and we must remove the hijack and
        ;; reflect the correct return address in our exception context record.  The other throw helpers don't
        ;; need this because they cannot be tail-called from C#.

        ;; NOTE: we cannot use INLINE_THREAD_UNHIJACK because it will write into the stack at the location
        ;; where the tail-calling thread had saved LR, which may not match where we have saved LR.

        ldr         r1, [r2, #OFFSETOF__Thread__m_pvHijackedReturnAddress]
        cbz         r1, NotHijacked

        ldr         r3, [r2, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]

        ;; r0: exception object
        ;; r1: hijacked return address
        ;; r2: pThread
        ;; r3: hijacked return address location

        add         r12, sp, #(STACKSIZEOF_ExInfo + SIZEOF__PAL_LIMITED_CONTEXT)        ;; re-compute SP at callsite
        cmp         r3, r12             ;; if (m_ppvHijackedReturnAddressLocation < SP at callsite)
        blo         TailCallWasHijacked

        ;; normal case where a valid return address location is hijacked
        str         r1, [r3]
        b           ClearThreadState

TailCallWasHijacked

        ;; Abnormal case where the return address location is now invalid because we ended up here via a tail
        ;; call.  In this case, our hijacked return address should be the correct caller of this method.
        ;;

        ;; stick the previous return address in LR as well as in the right spots in our PAL_LIMITED_CONTEXT.
        mov         lr, r1
        str         lr, [sp, #(rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__LR)]
        str         lr, [sp, #(rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__IP)]

ClearThreadState

        ;; clear the Thread's hijack state
        mov         r3, #0
        str         r3, [r2, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
        str         r3, [r2, #OFFSETOF__Thread__m_pvHijackedReturnAddress]

NotHijacked

        add         r1, sp, #rsp_offsetof_ExInfo                    ;; r1 <- ExInfo*
        mov         r3, #0
        str         r3, [r1, #OFFSETOF__ExInfo__m_exception]        ;; pExInfo->m_exception = null
        mov         r3, #1
        strb        r3, [r1, #OFFSETOF__ExInfo__m_passNumber]       ;; pExInfo->m_passNumber = 1
        mov         r3, #0xFFFFFFFF
        str         r3, [r1, #OFFSETOF__ExInfo__m_idxCurClause]     ;; pExInfo->m_idxCurClause = MaxTryRegionIdx
        mov         r3, #1
        strb        r3, [r1, #OFFSETOF__ExInfo__m_kind]             ;; pExInfo->m_kind = ExKind.Throw

        ;; link the ExInfo into the thread's ExInfo chain
        ldr         r3, [r2, #OFFSETOF__Thread__m_pExInfoStackHead]
        str         r3, [r1, #OFFSETOF__ExInfo__m_pPrevExInfo]      ;; pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        str         r1, [r2, #OFFSETOF__Thread__m_pExInfoStackHead] ;; m_pExInfoStackHead = pExInfo

        ;; set the exception context field on the ExInfo
        add         r2, sp, #rsp_offsetof_Context                   ;; r2 <- PAL_LIMITED_CONTEXT*
        str         r2, [r1, #OFFSETOF__ExInfo__m_pExContext]       ;; pExInfo->m_pExContext = pContext

        ;; r0: exception object
        ;; r1: ExInfo*
        bl          RhThrowEx

        EXPORT_POINTER_TO_ADDRESS PointerToRhpThrowEx2

        ;; no return
        __debugbreak

    NESTED_END RhpThrowEx

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
    NESTED_ENTRY RhpRethrow

        ;; Setup a PAL_LIMITED_CONTEXT on the stack {
        PROLOG_VPUSH {d8-d15}
        PROLOG_PUSH {r0,lr}         ;; Reserve space for SP and store LR
        PROLOG_PUSH {r0,r4-r11,lr}
        ;; } end PAL_LIMITED_CONTEXT

        PROLOG_STACK_ALLOC STACKSIZEOF_ExInfo

        ;; Compute and save SP at callsite.
        add         r1, sp, #(STACKSIZEOF_ExInfo + SIZEOF__PAL_LIMITED_CONTEXT)
        str         r1, [sp, #(rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__SP)]

        ;; r2 = GetThread(), TRASHES r1
        INLINE_GETTHREAD r2, r1

        add         r1, sp, #rsp_offsetof_ExInfo                    ;; r1 <- ExInfo*
        mov         r3, #0
        str         r3, [r1, #OFFSETOF__ExInfo__m_exception]        ;; pExInfo->m_exception = null
        strb        r3, [r1, #OFFSETOF__ExInfo__m_kind]             ;; init to a deterministic value (ExKind.None)
        mov         r3, #1
        strb        r3, [r1, #OFFSETOF__ExInfo__m_passNumber]       ;; pExInfo->m_passNumber = 1
        mov         r3, #0xFFFFFFFF
        str         r3, [r1, #OFFSETOF__ExInfo__m_idxCurClause]     ;; pExInfo->m_idxCurClause = MaxTryRegionIdx

        ;; link the ExInfo into the thread's ExInfo chain
        ldr         r3, [r2, #OFFSETOF__Thread__m_pExInfoStackHead]
        mov         r0, r3                                          ;; r0 <- current ExInfo
        str         r3, [r1, #OFFSETOF__ExInfo__m_pPrevExInfo]      ;; pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        str         r1, [r2, #OFFSETOF__Thread__m_pExInfoStackHead] ;; m_pExInfoStackHead = pExInfo

        ;; set the exception context field on the ExInfo
        add         r2, sp, #rsp_offsetof_Context                   ;; r2 <- PAL_LIMITED_CONTEXT*
        str         r2, [r1, #OFFSETOF__ExInfo__m_pExContext]       ;; pExInfo->m_pExContext = pContext

        ;; r0 contains the currently active ExInfo
        ;; r1 contains the address of the new ExInfo
        bl          RhRethrow

        EXPORT_POINTER_TO_ADDRESS PointerToRhpRethrow2

        ;; no return
        __debugbreak

    NESTED_END RhpRethrow

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* FASTCALL RhpCallCatchFunclet(RtuObjectRef exceptionObj, void* pHandlerIP, REGDISPLAY* pRegDisplay,
;;                                    ExInfo* pExInfo)
;;
;; INPUT:  R0:  exception object
;;         R1:  handler funclet address
;;         R2:  REGDISPLAY*
;;         R3:  ExInfo*
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY RhpCallCatchFunclet

        PROLOG_PUSH     {r0,r2-r11,lr}  ;; r0, r2 & r3 are saved so we have the exception object,
                                        ;; REGDISPLAY and ExInfo later
        PROLOG_VPUSH    {d8-d15}

#define rsp_offset_is_not_handling_thread_abort (8 * 8) + 0
#define rsp_offset_r2 (8 * 8) + 4
#define rsp_offset_r3 (8 * 8) + 8

        ;;
        ;; clear the DoNotTriggerGc flag, trashes r4-r6
        ;;
        INLINE_GETTHREAD    r5, r6      ;; r5 <- Thread*, r6 <- trashed

        ldr         r4, [r5, #OFFSETOF__Thread__m_threadAbortException]
        sub         r4, r0
        str         r4, [sp, #rsp_offset_is_not_handling_thread_abort] ;; Non-zero if the exception is not ThreadAbortException

ClearRetry_Catch
        ldrex       r4, [r5, #OFFSETOF__Thread__m_ThreadStateFlags]
        bic         r4, #TSF_DoNotTriggerGc
        strex       r6, r4, [r5, #OFFSETOF__Thread__m_ThreadStateFlags]
        cbz         r6, ClearSuccess_Catch
        b           ClearRetry_Catch
ClearSuccess_Catch

        ;;
        ;; set preserved regs to the values expected by the funclet
        ;;
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR4]
        ldr         r4, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR5]
        ldr         r5, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR6]
        ldr         r6, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR7]
        ldr         r7, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR8]
        ldr         r8, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR9]
        ldr         r9, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR10]
        ldr         r10, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR11]
        ldr         r11, [r12]

#if 0 // def _DEBUG  ;; @TODO: temporarily removed because trashing the frame pointer breaks the debugger
        ;; trash the values at the old homes to make sure nobody uses them
        movw        r3, #0xdeed
        movt        r3, #0xbaad
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR4]
        str         r3, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR5]
        str         r3, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR6]
        str         r3, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR7]
        str         r3, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR8]
        str         r3, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR9]
        str         r3, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR10]
        str         r3, [r12]
        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR11]
        str         r3, [r12]
#endif // _DEBUG

        ;;
        ;; load vfp preserved regs
        ;;
        add         r12, r2, #OFFSETOF__REGDISPLAY__D
        vldm        r12!, {d8-d15}

        ;;
        ;; call the funclet
        ;;
        ;; r0 still contains the exception object
        blx         r1

        EXPORT_POINTER_TO_ADDRESS PointerToRhpCallCatchFunclet2

        ;; r0 contains resume IP

        ldr         r2, [sp, #rsp_offset_r2]                    ;; r2 <- REGDISPLAY*

;; @TODO: add debug-only validation code for ExInfo pop

        INLINE_GETTHREAD r1, r3                                 ;; r1 <- Thread*, r3 <- trashed

        ;; We must unhijack the thread at this point because the section of stack where the hijack is applied
        ;; may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK r1, r3, r12                      ;; Thread in r1, trashes r3 and r12

        ldr         r3, [sp, #rsp_offset_r3]                    ;; r3 <- current ExInfo*
        ldr         r2, [r2, #OFFSETOF__REGDISPLAY__SP]         ;; r2 <- resume SP value

PopExInfoLoop
        ldr         r3, [r3, #OFFSETOF__ExInfo__m_pPrevExInfo]  ;; r3 <- next ExInfo
        cbz         r3, DonePopping                             ;; if (pExInfo == null) { we're done }
        cmp         r3, r2
        blt         PopExInfoLoop                               ;; if (pExInfo < resume SP} { keep going }

DonePopping
        str         r3, [r1, #OFFSETOF__Thread__m_pExInfoStackHead]     ;; store the new head on the Thread

        ldr         r3, =RhpTrapThreads
        ldr         r3, [r3]
        tst         r3, #TrapThreadsFlags_AbortInProgress
        beq         NoAbort

        ldr         r3, [sp, #rsp_offset_is_not_handling_thread_abort]
        cmp         r3, #0
        bne         NoAbort

        ;; It was the ThreadAbortException, so rethrow it
        ;; reset SP
        mov         r1, r0                                     ;; r1 <- continuation address as exception PC
        mov         r0, #STATUS_REDHAWK_THREAD_ABORT
        mov         sp, r2
        b           RhpThrowHwEx

NoAbort
        ;; reset SP and jump to continuation address
        mov         sp, r2
        bx          r0

    NESTED_END RhpCallCatchFunclet

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void FASTCALL RhpCallFinallyFunclet(void* pHandlerIP, REGDISPLAY* pRegDisplay)
;;
;; INPUT:  R0:  handler funclet address
;;         R1:  REGDISPLAY*
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY RhpCallFinallyFunclet

        PROLOG_PUSH     {r1,r4-r11,lr}  ;; r1 is saved so we have the REGDISPLAY later
        PROLOG_VPUSH    {d8-d15}
#define rsp_offset_r1 8 * 8

        ;;
        ;; We want to suppress hijacking between invocations of subsequent finallys.  We do this because we
        ;; cannot tolerate a GC after one finally has run (and possibly side-effected the GC state of the
        ;; method) and then been popped off the stack, leaving behind no trace of its effect.
        ;;
        ;; So we clear the state before and set it after invocation of the handler.
        ;;

        ;;
        ;; clear the DoNotTriggerGc flag, trashes r1-r3
        ;;
        INLINE_GETTHREAD    r2, r3      ;; r2 <- Thread*, r3 <- trashed
ClearRetry
        ldrex       r1, [r2, #OFFSETOF__Thread__m_ThreadStateFlags]
        bic         r1, #TSF_DoNotTriggerGc
        strex       r3, r1, [r2, #OFFSETOF__Thread__m_ThreadStateFlags]
        cbz         r3, ClearSuccess
        b           ClearRetry
ClearSuccess

        ldr         r1, [sp, #rsp_offset_r1]        ;; reload REGDISPLAY pointer

        ;;
        ;; set preserved regs to the values expected by the funclet
        ;;
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR4]
        ldr         r4, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR5]
        ldr         r5, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR6]
        ldr         r6, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR7]
        ldr         r7, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR8]
        ldr         r8, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR9]
        ldr         r9, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR10]
        ldr         r10, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR11]
        ldr         r11, [r12]

#if 0 // def _DEBUG  ;; @TODO: temporarily removed because trashing the frame pointer breaks the debugger
        ;; trash the values at the old homes to make sure nobody uses them
        movw        r3, #0xdeed
        movt        r3, #0xbaad
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR4]
        str         r3, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR5]
        str         r3, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR6]
        str         r3, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR7]
        str         r3, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR8]
        str         r3, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR9]
        str         r3, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR10]
        str         r3, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR11]
        str         r3, [r12]
#endif // _DEBUG

        ;;
        ;; load vfp preserved regs
        ;;
        add         r12, r1, #OFFSETOF__REGDISPLAY__D
        vldm        r12!, {d8-d15}

        ;;
        ;; call the funclet
        ;;
        blx         r0

        EXPORT_POINTER_TO_ADDRESS PointerToRhpCallFinallyFunclet2

        ldr         r1, [sp, #rsp_offset_r1]        ;; reload REGDISPLAY pointer

        ;;
        ;; save new values of preserved regs into REGDISPLAY
        ;;
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR4]
        str         r4, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR5]
        str         r5, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR6]
        str         r6, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR7]
        str         r7, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR8]
        str         r8, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR9]
        str         r9, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR10]
        str         r10, [r12]
        ldr         r12, [r1, #OFFSETOF__REGDISPLAY__pR11]
        str         r11, [r12]

        ;;
        ;; store vfp preserved regs
        ;;
        add         r12, r1, #OFFSETOF__REGDISPLAY__D
        vstm        r12!, {d8-d15}

        ;;
        ;; set the DoNotTriggerGc flag, trashes r1-r3
        ;;
        INLINE_GETTHREAD    r2, r3      ;; r2 <- Thread*, r3 <- trashed
SetRetry
        ldrex       r1, [r2, #OFFSETOF__Thread__m_ThreadStateFlags]
        orr         r1, #TSF_DoNotTriggerGc
        strex       r3, r1, [r2, #OFFSETOF__Thread__m_ThreadStateFlags]
        cbz         r3, SetSuccess
        b           SetRetry
SetSuccess

        EPILOG_VPOP {d8-d15}
        EPILOG_POP {r1,r4-r11,pc}

    NESTED_END RhpCallFinallyFunclet

        INLINE_GETTHREAD_CONSTANT_POOL

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
;; void* FASTCALL RhpCallFilterFunclet(RtuObjectRef exceptionObj, void* pFilterIP, REGDISPLAY* pRegDisplay)
;;
;; INPUT:  R0:  exception object
;;         R1:  filter funclet address
;;         R2:  REGDISPLAY*
;;
;; OUTPUT:
;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
    NESTED_ENTRY RhpCallFilterFunclet

        PROLOG_PUSH     {r2,r4-r11,lr}
        PROLOG_VPUSH    {d8-d15}

        ldr         r12, [r2, #OFFSETOF__REGDISPLAY__pR7]
        ldr         r7, [r12]

        ;;
        ;; call the funclet
        ;;
        ;; r0 still contains the exception object
        blx         r1

        EXPORT_POINTER_TO_ADDRESS PointerToRhpCallFilterFunclet2

        EPILOG_VPOP {d8-d15}
        EPILOG_POP {r2,r4-r11,pc}

    NESTED_END RhpCallFilterFunclet

        end
