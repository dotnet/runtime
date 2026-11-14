// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

#define STACKSIZEOF_ExInfo ((SIZEOF__ExInfo + 15)&(~15))

#define HARDWARE_EXCEPTION 1
#define SOFTWARE_EXCEPTION 0

.global RhpTrapThreads

// -----------------------------------------------------------------------------
// Macro used to create frame of exception throwing helpers (RhpThrowEx, RhpThrowHwEx)
    .macro ALLOC_THROW_FRAME exceptionType

        ori  $a3, $sp, 0

        // Setup a PAL_LIMITED_CONTEXT on the stack {
        // Total stack: 0xC0 (0x68+0x58)
        .if \exceptionType == HARDWARE_EXCEPTION
            addi.d  $sp, $sp, -0x58
            .cfi_adjust_cfa_offset  0x58
            st.d  $a3, $sp, 0      // a3 is the SP and a1 is the IP of the fault site
            st.d  $a1, $sp, 8
        .else
            PROLOG_STACK_ALLOC  0x58
            st.d  $a3, $sp, 0      // a3 is the SP and ra is the IP of the fault site
            st.d  $ra, $sp, 8
        .endif
        fst.d  $f24, $sp, 0x10
        fst.d  $f25, $sp, 0x18
        fst.d  $f26, $sp, 0x20
        fst.d  $f27, $sp, 0x28
        fst.d  $f28, $sp, 0x30
        fst.d  $f29, $sp, 0x38
        fst.d  $f30, $sp, 0x40
        fst.d  $f31, $sp, 0x48
        // Slot at $sp+0x50 is alignment padding

        PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 0x68
        st.d  $zero, $sp, 0x10 // locations reserved for return value, not used for exception handling
        st.d  $zero, $sp, 0x18
        PROLOG_SAVE_REG_PAIR  23, 24, 0x20
        PROLOG_SAVE_REG_PAIR  25, 26, 0x30
        PROLOG_SAVE_REG_PAIR  27, 28, 0x40
        PROLOG_SAVE_REG_PAIR  29, 30, 0x50
        PROLOG_SAVE_REG       31,     0x60
        // } end PAL_LIMITED_CONTEXT

        // The CFA is $fp-based here, so no CFA adjustment
        addi.d  $sp, $sp, -STACKSIZEOF_ExInfo
    .endm

#define rsp_offsetof_ExInfo  0
#define rsp_offsetof_Context STACKSIZEOF_ExInfo

//
// RhpThrowHwEx
//
// INPUT:  a0[31:0]:  exception code of fault
//         a1:  faulting IP
//
// OUTPUT:
//
    NESTED_ENTRY RhpThrowHwEx, _TEXT, NoHandler

        ALLOC_THROW_FRAME HARDWARE_EXCEPTION

        ori  $r23, $a0, 0                                // r23 <- exception code

        addi.d  $a0, $sp, rsp_offsetof_ExInfo            // a0 <- ExInfo*
        addi.d  $a1, $sp, rsp_offsetof_Context           // a1 <- PAL_LIMITED_CONTEXT*
        ori  $a2, $zero, 2                               // a2 <- ExKind.HardwareFault
        bl  C_FUNC(RhpPushExInfo)

        ori  $a0, $r23, 0                                // a0 <- exception code
        addi.d  $a1, $sp, rsp_offsetof_ExInfo            // a1 <- ExInfo*
        bl  C_FUNC(RhThrowHwEx)

    ALTERNATE_ENTRY RhpThrowHwEx2

        // no return
        EMIT_BREAKPOINT

    NESTED_END RhpThrowHwEx, _TEXT

//
// RhpThrowExact
//
// SUMMARY:  Similar to RhpThrowEx, except that it sets the rethrow flag
//
// INPUT:  a0:  exception object
//
// OUTPUT:
//

    LEAF_ENTRY RhpThrowExact, _TEXT

        ori  $a4, $zero, 4                               // a4 = ExKind.RethrowFlag
        b  C_FUNC(RhpThrowImpl)

    LEAF_END RhpThrowExact, _TEXT

//
// RhpThrowEx
//
// INPUT:  a0:  exception object
//
// OUTPUT:
//

    LEAF_ENTRY RhpThrowEx, _TEXT

        ori  $a4, $zero, 1                               // a4 = ExKind.Throw
        b  C_FUNC(RhpThrowImpl)

    LEAF_END RhpThrowEx, _TEXT

    NESTED_ENTRY RhpThrowImpl, _TEXT, NoHandler

        ALLOC_THROW_FRAME SOFTWARE_EXCEPTION

        ori  $r23, $a0, 0                                // r23 <- exception object

        addi.d  $a0, $sp, rsp_offsetof_ExInfo            // a0 <- ExInfo*
        addi.d  $a1, $sp, rsp_offsetof_Context           // a1 <- PAL_LIMITED_CONTEXT*
        ori  $a2, $a4, 0                                 // a2 <- ExKind
        bl  C_FUNC(RhpPushExInfo)

        ori  $a0, $r23, 0                                // a0 <- exception object
        addi.d  $a1, $sp, rsp_offsetof_ExInfo            // a1 <- ExInfo*
        bl  C_FUNC(RhThrowEx)

    ALTERNATE_ENTRY RhpThrowImpl2

        // no return
        EMIT_BREAKPOINT
    NESTED_END RhpThrowImpl, _TEXT

//
// void FASTCALL RhpRethrow()
//
// SUMMARY:  Similar to RhpThrowEx, except that it passes along the currently active ExInfo
//
// INPUT:
//
// OUTPUT:
//

    NESTED_ENTRY RhpRethrow, _TEXT, NoHandler

        ALLOC_THROW_FRAME SOFTWARE_EXCEPTION

        addi.d  $a0, $sp, rsp_offsetof_ExInfo            // a0 <- ExInfo*
        addi.d  $a1, $sp, rsp_offsetof_Context           // a1 <- PAL_LIMITED_CONTEXT*
        ori  $a2, $zero, 0                               // a2 <- ExKind.None
        bl  C_FUNC(RhpPushExInfo)

        // a0 contains the currently active ExInfo
        addi.d  $a1, $sp, rsp_offsetof_ExInfo            // a1 <- ExInfo*
        bl  C_FUNC(RhRethrow)

    ALTERNATE_ENTRY RhpRethrow2

        // no return
        EMIT_BREAKPOINT
    NESTED_END RhpRethrow, _TEXT

//
// void FASTCALL RhpResumeAfterCatch(REGDISPLAY* pRegDisplay, void* pResumeIP)
//
// Resumes execution after a catch funclet called by CallEHFunclet has returned and the dispatcher has popped
// the ExInfos below the resume SP. Does not return.
//
// INPUT:  $a0:  REGDISPLAY* of the frame that contains the catch
//         $a1:  resume address returned by the catch funclet
//

    LEAF_ENTRY RhpResumeAfterCatch, _TEXT

        INLINE_GETTHREAD $a3                                // $a3 <- Thread*

        // Clear the DoNotTriggerGc state before resuming in the managed code after the catch.
        addi.d  $t3, $a3, OFFSETOF__Thread__m_ThreadStateFlags
        addi.w  $t4, $zero, ~TSF_DoNotTriggerGc
        amand_db.w $t5, $t4, $t3

        // We must unhijack the thread at this point because the section of stack where the hijack is applied
        // may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK  $a3, $a4, $t3               // Thread in $a3, trashes $a4 and $t3

        LOAD_FUNCLET_REGISTER $a0, 22, $t3
        LOAD_FUNCLET_REGISTER $a0, 23, $t3
        LOAD_FUNCLET_REGISTER $a0, 24, $t3
        LOAD_FUNCLET_REGISTER $a0, 25, $t3
        LOAD_FUNCLET_REGISTER $a0, 26, $t3
        LOAD_FUNCLET_REGISTER $a0, 27, $t3
        LOAD_FUNCLET_REGISTER $a0, 28, $t3
        LOAD_FUNCLET_REGISTER $a0, 29, $t3
        LOAD_FUNCLET_REGISTER $a0, 30, $t3
        LOAD_FUNCLET_REGISTER $a0, 31, $t3

        LOAD_FUNCLET_FP_REGISTER $a0, 24
        LOAD_FUNCLET_FP_REGISTER $a0, 25
        LOAD_FUNCLET_FP_REGISTER $a0, 26
        LOAD_FUNCLET_FP_REGISTER $a0, 27
        LOAD_FUNCLET_FP_REGISTER $a0, 28
        LOAD_FUNCLET_FP_REGISTER $a0, 29
        LOAD_FUNCLET_FP_REGISTER $a0, 30
        LOAD_FUNCLET_FP_REGISTER $a0, 31

        // reset SP and jump to continuation address
        ld.d    $a2, $a0, OFFSETOF__REGDISPLAY__SP
        ori     $sp, $a2, 0
        jirl    $r0, $a1, 0

    LEAF_END RhpResumeAfterCatch, _TEXT
