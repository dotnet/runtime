// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.syntax unified
.thumb

#include "AsmMacros_Shared.h"

#define STACKSIZEOF_ExInfo ((SIZEOF__ExInfo + 7)&(~7))

#define rsp_offsetof_ExInfo  0
#define rsp_offsetof_Context STACKSIZEOF_ExInfo

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// RhpThrowHwEx
//
// INPUT:  R0:  exception code of fault
//         R1:  faulting RIP
//
// OUTPUT:
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
NESTED_ENTRY RhpThrowHwEx, _TEXT, NoHandler

        mov         r2, r0         // save exception code into r2
        mov         r0, sp         // get SP of fault site

        mov         lr, r1         // set IP of fault site

        // Setup a PAL_LIMITED_CONTEXT on the stack {
        PROLOG_VPUSH {d8-d15}
        PROLOG_PUSH "{r0,lr}"        // push {sp, pc} of fault site
        PROLOG_PUSH "{r0,r4-r11,lr}"
        // } end PAL_LIMITED_CONTEXT

        PROLOG_STACK_ALLOC STACKSIZEOF_ExInfo

        // r0: SP of fault site
        // r1: IP of fault site
        // r2: exception code of fault
        // lr: IP of fault site (as a 'return address')
        mov         r4, r2 // save exception code of fault

        // r0 = GetThread()
        INLINE_GETTHREAD

        // r1 <- ExInfo*
        add         r1, sp, #rsp_offsetof_ExInfo
        mov         r3, #0
        str         r3, [r1, #OFFSETOF__ExInfo__m_exception]        // pExInfo->m_exception = null
        mov         r3, #1
        strb        r3, [r1, #OFFSETOF__ExInfo__m_passNumber]       // pExInfo->m_passNumber = 1
        mov         r3, #0xFFFFFFFF
        str         r3, [r1, #OFFSETOF__ExInfo__m_idxCurClause]     // pExInfo->m_idxCurClause = MaxTryRegionIdx
        mov         r3, #2
        strb        r3, [r1, #OFFSETOF__ExInfo__m_kind]             // pExInfo->m_kind = ExKind.HardwareFault

        // link the ExInfo into the thread's ExInfo chain
        ldr         r3, [r0, #OFFSETOF__Thread__m_pExInfoStackHead]
        str         r3, [r1, #OFFSETOF__ExInfo__m_pPrevExInfo]      // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        str         r1, [r0, #OFFSETOF__Thread__m_pExInfoStackHead] // m_pExInfoStackHead = pExInfo

        // set the exception context field on the ExInfo
        add         r2, sp, #rsp_offsetof_Context                   // r2 <- PAL_LIMITED_CONTEXT*
        str         r2, [r1, #OFFSETOF__ExInfo__m_pExContext]       // pExInfo->m_pExContext = pContext

        mov         r0, r4 	        // restore the exception code
        // r0 contains the exception code
        // r1 contains the address of the ExInfo
        bl          C_FUNC(RhThrowHwEx)

GLOBAL_LABEL RhpThrowHwEx2

        // no return
        EMIT_BREAKPOINT

NESTED_END RhpThrowHwEx


//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// RhpThrowExact
//
// SUMMARY:  Similar to RhpThrowEx, except that it sets the rethrow flag
//
// INPUT:  R0:  exception object
//
// OUTPUT:
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
LEAF_ENTRY RhpThrowExact, _TEXT

        mov         r2, #4                                          // r2 = ExKind.RethrowFlag
        b           C_FUNC(RhpThrowImpl)

LEAF_END RhpThrowExact, _TEXT

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// RhpThrowEx
//
// INPUT:  R0:  exception object
//
// OUTPUT:
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
LEAF_ENTRY RhpThrowEx, _TEXT

        mov         r2, #1                                          // r2 = ExKind.Throw
        b           C_FUNC(RhpThrowImpl)
LEAF_END RhpThrowEx, _TEXT

NESTED_ENTRY RhpThrowImpl, _TEXT, NoHandler

        // Setup a PAL_LIMITED_CONTEXT on the stack {
        PROLOG_VPUSH {d8-d15}
        PROLOG_PUSH "{r0,lr}"        	// Reserve space for SP and store LR
        PROLOG_PUSH "{r0,r4-r11,lr}"
        // } end PAL_LIMITED_CONTEXT

        PROLOG_STACK_ALLOC STACKSIZEOF_ExInfo

        // Calculate  SP at callsite and save into the PAL_LIMITED_CONTEXT
        add         r4, sp, #(STACKSIZEOF_ExInfo + SIZEOF__PAL_LIMITED_CONTEXT)
        str         r4, [sp, #(rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__SP)]

        mov         r4, r0 // Save exception object
        mov         r5, r2 // Save ExKind
        // r0 = GetThread()
        INLINE_GETTHREAD

        // There is runtime C# code that can tail call to RhpThrowEx using a binder intrinsic. So  the return
        // address could have been hijacked when we were in that C# code and we must remove the hijack and
        // reflect the correct return address in our exception context record.  The other throw helpers don't
        // need this because they cannot be tail-called from C#.
        // NOTE: we cannot use INLINE_THREAD_UNHIJACK because it will write into the stack at the location
        // where the tail-calling thread had saved LR, which may not match where we have saved LR.

        ldr         r1, [r0, #OFFSETOF__Thread__m_pvHijackedReturnAddress]
        cbz         r1, LOCAL_LABEL(NotHiJacked)

        ldr         r3, [r0, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]

        // r4: exception object
        // r5: ExKind
        // r1: hijacked return address
        // r0: pThread
        // r3: hijacked return address location

        add         r12, sp, #(STACKSIZEOF_ExInfo + SIZEOF__PAL_LIMITED_CONTEXT)        // re-compute SP at callsite
        cmp         r3, r12             // if (m_ppvHijackedReturnAddressLocation < SP at callsite)
        blo         LOCAL_LABEL(TailCallWasHijacked)

        // normal case where a valid return address location is hijacked
        str         r1, [r3]
        b           LOCAL_LABEL(ClearThreadState)

LOCAL_LABEL(TailCallWasHijacked):

        // Abnormal case where the return address location is now invalid because we ended up here via a tail
        // call.  In this case, our hijacked return address should be the correct caller of this method.
        //

        // stick the previous return address in LR as well as in the right spots in our PAL_LIMITED_CONTEXT.
        mov         lr, r1
        str         lr, [sp, #(rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__LR)]
        str         lr, [sp, #(rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__IP)]

LOCAL_LABEL(ClearThreadState):

        // clear the Thread's hijack state
        mov         r3, #0
        str         r3, [r0, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
        str         r3, [r0, #OFFSETOF__Thread__m_pvHijackedReturnAddress]

LOCAL_LABEL(NotHiJacked):

        add         r1, sp, #rsp_offsetof_ExInfo                    // r1 <- ExInfo*
        mov         r3, #0
        str         r3, [r1, #OFFSETOF__ExInfo__m_exception] 	      // init the exception object to null
        mov         r3, #1
        strb        r3, [r1, #OFFSETOF__ExInfo__m_passNumber]       // init to the first pass
        strb        r5, [r1, #OFFSETOF__ExInfo__m_kind]             // ExKind
        mov         r3, #0xFFFFFFFF
        str         r3, [r1, #OFFSETOF__ExInfo__m_idxCurClause]

        // link the ExInfo into the thread's ExInfo chain
        ldr         r3, [r0, #OFFSETOF__Thread__m_pExInfoStackHead]
        str         r3, [r1, #OFFSETOF__ExInfo__m_pPrevExInfo]  	  // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        str         r1, [r0, #OFFSETOF__Thread__m_pExInfoStackHead] // m_pExInfoStackHead = pExInfo

        // set the exception context field on the ExInfo
        add         r3, sp, #rsp_offsetof_Context             // r3 <- PAL_LIMITED_CONTEXT*
        str         r3, [r1, #OFFSETOF__ExInfo__m_pExContext] // init ExInfo.m_pExContext

        mov         r0, r4 // Restore exception object
        // r0 contains the exception object
        // r1 contains the address of the new ExInfo
        bl          C_FUNC(RhThrowEx)

GLOBAL_LABEL RhpThrowImpl2

        // no return
        EMIT_BREAKPOINT

NESTED_END RhpThrowImpl, _TEXT


//////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// void FASTCALL RhpRethrow()
//
// SUMMARY:  Similar to RhpThrowEx, except that it passes along the currently active ExInfo
//
// INPUT:
//
// OUTPUT:
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
NESTED_ENTRY RhpRethrow, _TEXT, NoHandler

        // Setup a PAL_LIMITED_CONTEXT on the stack {
        PROLOG_VPUSH {d8-d15}
        PROLOG_PUSH "{r0,lr}"         // Reserve space for SP and store LR
        PROLOG_PUSH "{r0,r4-r11,lr}"
        // } end PAL_LIMITED_CONTEXT

        PROLOG_STACK_ALLOC STACKSIZEOF_ExInfo

        // Compute and save SP at callsite.
        add         r1, sp, #(STACKSIZEOF_ExInfo + SIZEOF__PAL_LIMITED_CONTEXT)
        str         r1, [sp, #(rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__SP)]

        // r0 = GetThread();
        INLINE_GETTHREAD

        // r1 <- ExInfo*
        add         r1, sp, #rsp_offsetof_ExInfo

        mov         r3, #0
        str         r3, [r1, #OFFSETOF__ExInfo__m_exception]         // init the exception object to null
        strb        r3, [r1, #OFFSETOF__ExInfo__m_kind]              // init to a deterministic value (ExKind.None)
        mov         r3, #1
        strb        r3, [r1, #OFFSETOF__ExInfo__m_passNumber]        // pExInfo->m_passNumber = 1
        mov         r3, #0xFFFFFFFF
        str         r3, [r1, #OFFSETOF__ExInfo__m_idxCurClause]

        // link the ExInfo into the thread's ExInfo chain
        ldr         r3, [r0, #OFFSETOF__Thread__m_pExInfoStackHead] 	// r3 <- currently active ExInfo
        str         r3, [r1, #OFFSETOF__ExInfo__m_pPrevExInfo]        // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        str         r1, [r0, #OFFSETOF__Thread__m_pExInfoStackHead]   // m_pExInfoStackHead = pExInfo

        // set the exception context field on the ExInfo
        add         r2, sp, #rsp_offsetof_Context                 	  // r2 <- PAL_LIMITED_CONTEXT*
        str         r2, [r1, #OFFSETOF__ExInfo__m_pExContext]         // init ExInfo.m_pExContext

        mov         r0, r3
        // r0 contains the currently active ExInfo
        // r1 contains the address of the new ExInfo
        blx    C_FUNC(RhRethrow)

GLOBAL_LABEL RhpRethrow2

        // no return
        EMIT_BREAKPOINT

NESTED_END RhpRethrow, _TEXT

//
// void FASTCALL RhpResumeAfterCatch(REGDISPLAY* pRegDisplay, void* pResumeIP)
//
// Resumes execution after a catch funclet called by CallEHFunclet has returned and the dispatcher has popped
// the ExInfos below the resume SP. Does not return.
//
// INPUT:  R0:  REGDISPLAY* of the frame that contains the catch
//         R1:  resume address returned by the catch funclet
//

LEAF_ENTRY RhpResumeAfterCatch, _TEXT

        // INLINE_GETTHREAD trashes r0-r3; the preserved registers are reloaded from the REGDISPLAY below.
        mov         r4, r0
        mov         r5, r1
        INLINE_GETTHREAD                                        // r0 <- Thread*

        // Clear the DoNotTriggerGc state before resuming in the managed code after the catch.
LOCAL_LABEL(ResumeAfterCatch_ClearRetry):
        ldrex       r1, [r0, #OFFSETOF__Thread__m_ThreadStateFlags]
        bic         r1, r1, #TSF_DoNotTriggerGc
        strex       r2, r1, [r0, #OFFSETOF__Thread__m_ThreadStateFlags]
        cmp         r2, #0
        bne         LOCAL_LABEL(ResumeAfterCatch_ClearRetry)

        // We must unhijack the thread at this point because the section of stack where the hijack is applied
        // may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK r0, r2, r3                       // Thread in r0, trashes r2 and r3

        mov         r0, r4
        mov         r1, r5

        LOAD_FUNCLET_REGISTER r0, 4, r12
        LOAD_FUNCLET_REGISTER r0, 5, r12
        LOAD_FUNCLET_REGISTER r0, 6, r12
        LOAD_FUNCLET_REGISTER r0, 7, r12
        LOAD_FUNCLET_REGISTER r0, 8, r12
        LOAD_FUNCLET_REGISTER r0, 9, r12
        LOAD_FUNCLET_REGISTER r0, 10, r12
        LOAD_FUNCLET_REGISTER r0, 11, r12

        LOAD_FUNCLET_FP_REGISTER r0, 8
        LOAD_FUNCLET_FP_REGISTER r0, 9
        LOAD_FUNCLET_FP_REGISTER r0, 10
        LOAD_FUNCLET_FP_REGISTER r0, 11
        LOAD_FUNCLET_FP_REGISTER r0, 12
        LOAD_FUNCLET_FP_REGISTER r0, 13
        LOAD_FUNCLET_FP_REGISTER r0, 14
        LOAD_FUNCLET_FP_REGISTER r0, 15

        // reset SP and jump to continuation address
        ldr         r2, [r0, #OFFSETOF__REGDISPLAY__SP]
        mov         sp, r2
        bx          r1

LEAF_END RhpResumeAfterCatch, _TEXT
