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

        add         r0, sp, #rsp_offsetof_ExInfo                    // r0 <- ExInfo*
        add         r1, sp, #rsp_offsetof_Context                   // r1 <- PAL_LIMITED_CONTEXT*
        mov         r2, #2                                          // r2 <- ExKind.HardwareFault
        bl          C_FUNC(RhpPushExInfo)

        mov         r0, r4                                          // r0 <- exception code
        add         r1, sp, #rsp_offsetof_ExInfo                    // r1 <- ExInfo*
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

        add         r0, sp, #rsp_offsetof_ExInfo                    // r0 <- ExInfo*
        add         r1, sp, #rsp_offsetof_Context                   // r1 <- PAL_LIMITED_CONTEXT*
        // r2 contains the ExKind
        bl          C_FUNC(RhpPushExInfo)

        mov         r0, r4                                          // r0 <- exception object
        add         r1, sp, #rsp_offsetof_ExInfo                    // r1 <- ExInfo*
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

        add         r0, sp, #rsp_offsetof_ExInfo                    // r0 <- ExInfo*
        add         r1, sp, #rsp_offsetof_Context                   // r1 <- PAL_LIMITED_CONTEXT*
        mov         r2, #0                                          // r2 <- ExKind.None
        bl          C_FUNC(RhpPushExInfo)

        // r0 contains the currently active ExInfo
        add         r1, sp, #rsp_offsetof_ExInfo                    // r1 <- ExInfo*
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
