// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

#define STACKSIZEOF_ExInfo ((SIZEOF__ExInfo + 7) & ~7)

#define HARDWARE_EXCEPTION 1
#define SOFTWARE_EXCEPTION 0

.global RhpTrapThreads

// -----------------------------------------------------------------------------
// Macro used to create frame of exception throwing helpers (RhpThrowEx, RhpThrowHwEx)
.macro ALLOC_THROW_FRAME exceptionType

    mv  a3, sp

    // Setup a PAL_LIMITED_CONTEXT on the stack {
    .if \exceptionType == HARDWARE_EXCEPTION
        addi  sp, sp, -0x70
        .cfi_adjust_cfa_offset  0x70
        sd  a3, 0(sp)          // a3 is the SP and a1 is the IP of the fault site
        sd  a1, 8(sp)
    .else
        PROLOG_STACK_ALLOC  0x70
        sd  a3, 0(sp)          // a3 is the SP and ra is the IP of the fault site
        sd  ra, 8(sp)
    .endif

    // Safely using available registers for floating-point saves
    fsd  fs0, 0x10(sp)
    fsd  fs1, 0x18(sp)
    fsd  fs2, 0x20(sp)
    fsd  fs3, 0x28(sp)
    fsd  fs4, 0x30(sp)
    fsd  fs5, 0x38(sp)
    fsd  fs6, 0x40(sp)
    fsd  fs7, 0x48(sp)
    fsd  fs8, 0x50(sp)
    fsd  fs9, 0x58(sp)
    fsd  fs10, 0x60(sp)
    fsd  fs11, 0x68(sp)

    PROLOG_SAVE_REG_PAIR_INDEXED  fp, ra, 0x78

    sd  zero, 0x10(sp)        // locations reserved for return value, not used for exception handling
    sd  zero, 0x18(sp)
    PROLOG_SAVE_REG_PAIR  s1, s2,  0x20
    PROLOG_SAVE_REG_PAIR  s3, s4,  0x30
    PROLOG_SAVE_REG_PAIR  s5, s6,  0x40
    PROLOG_SAVE_REG_PAIR  s7, s8,  0x50
    PROLOG_SAVE_REG_PAIR  s9, s10, 0x60
    PROLOG_SAVE_REG       s11,     0x70
    // } end PAL_LIMITED_CONTEXT

    // The CFA is fp-based here, so no CFA adjustment
    addi  sp, sp, -STACKSIZEOF_ExInfo
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

        mv     s1, a0                                   // s1 <- exception code

        addi   a0, sp, rsp_offsetof_ExInfo              // a0 <- ExInfo*
        addi   a1, sp, rsp_offsetof_Context             // a1 <- PAL_LIMITED_CONTEXT*
        li     a2, 2                                    // a2 <- ExKind.HardwareFault
        call   C_FUNC(RhpPushExInfo)

        mv     a0, s1                                   // a0 <- exception code
        addi   a1, sp, rsp_offsetof_ExInfo              // a1 <- ExInfo*
        call  C_FUNC(RhThrowHwEx)

    ALTERNATE_ENTRY RhpThrowHwEx2

        // No return
        EMIT_BREAKPOINT

    NESTED_END RhpThrowHwEx, _TEXT

//
// RhpThrowExact
//
// SUMMARY: Similar to RhpThrowEx, except that it sets the rethrow flag
//
// INPUT:  a0:  exception object
//
// OUTPUT:
//

    LEAF_ENTRY RhpThrowExact, _TEXT

        li  a4, 4                                       // a4 = ExKind.RethrowFlag
        j   C_FUNC(RhpThrowImpl)

    LEAF_END RhpThrowExact, _TEXT

//
// RhpThrowEx
//
// INPUT:  a0:  exception object
//
// OUTPUT:
//

    LEAF_ENTRY RhpThrowEx, _TEXT

        li  a4, 1                                       // a4 = ExKind.Throw
        j   C_FUNC(RhpThrowImpl)
    
    LEAF_END RhpThrowEx, _TEXT

    NESTED_ENTRY RhpThrowImpl, _TEXT, NoHandler

        ALLOC_THROW_FRAME SOFTWARE_EXCEPTION

        mv     s1, a0                                   // s1 <- exception object

        addi   a0, sp, rsp_offsetof_ExInfo              // a0 <- ExInfo*
        addi   a1, sp, rsp_offsetof_Context             // a1 <- PAL_LIMITED_CONTEXT*
        mv     a2, a4                                   // a2 <- ExKind
        call   C_FUNC(RhpPushExInfo)

        mv     a0, s1                                   // a0 <- exception object
        addi   a1, sp, rsp_offsetof_ExInfo              // a1 <- ExInfo*
        call C_FUNC(RhThrowEx)

    ALTERNATE_ENTRY RhpThrowImpl2

        // No return
        EMIT_BREAKPOINT

    NESTED_END RhpThrowImpl, _TEXT

//
// void FASTCALL RhpRethrow()
//
// SUMMARY: Similar to RhpThrowEx, except that it passes along the currently active ExInfo
//
// OUTPUT:
//

    NESTED_ENTRY RhpRethrow, _TEXT, NoHandler

        ALLOC_THROW_FRAME SOFTWARE_EXCEPTION

        addi   a0, sp, rsp_offsetof_ExInfo              // a0 <- ExInfo*
        addi   a1, sp, rsp_offsetof_Context             // a1 <- PAL_LIMITED_CONTEXT*
        li     a2, 0                                    // a2 <- ExKind.None
        call   C_FUNC(RhpPushExInfo)

        // a0 contains the currently active ExInfo
        addi   a1, sp, rsp_offsetof_ExInfo              // a1 <- ExInfo*
        call  C_FUNC(RhRethrow)

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
// INPUT:  a0:  REGDISPLAY* of the frame that contains the catch
//         a1:  resume address returned by the catch funclet
//

    LEAF_ENTRY RhpResumeAfterCatch, _TEXT

        // INLINE_GETTHREAD trashes a0.
        mv      a2, a0                                      // a2 <- REGDISPLAY*
        INLINE_GETTHREAD a3                                 // a3 <- Thread*

        // Clear the DoNotTriggerGc state before resuming in the managed code after the catch.
        addi    t3, a3, OFFSETOF__Thread__m_ThreadStateFlags
        addiw   t4, zero, ~TSF_DoNotTriggerGc
        amoand.w zero, t4, (t3)

        // We must unhijack the thread at this point because the section of stack where the hijack is applied
        // may go dead. If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK a3, a4, t3                   // Thread in a3, trashes a4 and t3

        LOAD_FUNCLET_REGISTER a2, 8, t3
        LOAD_FUNCLET_REGISTER a2, 9, t3
        LOAD_FUNCLET_REGISTER a2, 18, t3
        LOAD_FUNCLET_REGISTER a2, 19, t3
        LOAD_FUNCLET_REGISTER a2, 20, t3
        LOAD_FUNCLET_REGISTER a2, 21, t3
        LOAD_FUNCLET_REGISTER a2, 22, t3
        LOAD_FUNCLET_REGISTER a2, 23, t3
        LOAD_FUNCLET_REGISTER a2, 24, t3
        LOAD_FUNCLET_REGISTER a2, 25, t3
        LOAD_FUNCLET_REGISTER a2, 26, t3
        LOAD_FUNCLET_REGISTER a2, 27, t3

        LOAD_FUNCLET_FP_REGISTER a2, 8
        LOAD_FUNCLET_FP_REGISTER a2, 9
        LOAD_FUNCLET_FP_REGISTER a2, 18
        LOAD_FUNCLET_FP_REGISTER a2, 19
        LOAD_FUNCLET_FP_REGISTER a2, 20
        LOAD_FUNCLET_FP_REGISTER a2, 21
        LOAD_FUNCLET_FP_REGISTER a2, 22
        LOAD_FUNCLET_FP_REGISTER a2, 23
        LOAD_FUNCLET_FP_REGISTER a2, 24
        LOAD_FUNCLET_FP_REGISTER a2, 25
        LOAD_FUNCLET_FP_REGISTER a2, 26
        LOAD_FUNCLET_FP_REGISTER a2, 27

        // Reset SP and jump to the continuation address
        ld      t3, OFFSETOF__REGDISPLAY__SP(a2)
        mv      sp, t3
        jr      a1

    LEAF_END RhpResumeAfterCatch, _TEXT
