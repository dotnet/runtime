// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

#define STACKSIZEOF_ExInfo ((SIZEOF__ExInfo + 15)&(~15))

#define HARDWARE_EXCEPTION 1
#define SOFTWARE_EXCEPTION 0

.global RhpTrapThreads

// -----------------------------------------------------------------------------
// Macro used to create frame of exception throwing helpers (RhpThrowEx, RhpThrowHwEx)
    .macro ALLOC_THROW_FRAME exceptionType

        addi  a3, sp, 0

        // Setup a PAL_LIMITED_CONTEXT on the stack
        .if \exceptionType == HARDWARE_EXCEPTION
            addi  sp, sp, -80
            .cfi_adjust_cfa_offset 80
            sd    a3, 0(sp)      // a3 is the SP and a1 is the IP of the fault site
            sd    a1, 8(sp)
        .else
            PROLOG_STACK_ALLOC 80
            .cfi_adjust_cfa_offset 80
            sd    a3, 0(sp)      // a3 is the SP and ra is the IP of the fault site
            sd    ra, 8(sp)
        .endif
        fsd   f24, 16(sp)
        fsd   f25, 24(sp)
        fsd   f26, 32(sp)
        fsd   f27, 40(sp)
        fsd   f28, 48(sp)
        fsd   f29, 56(sp)
        fsd   f30, 64(sp)
        fsd   f31, 72(sp)
        PROLOG_SAVE_REG_PAIR_INDEXED 22, 1, 112
        sd    zero, 16(sp)  // locations reserved for return value, not used for exception handling
        sd    zero, 24(sp)
        PROLOG_SAVE_REG_PAIR 23, 24, 32
        PROLOG_SAVE_REG_PAIR 25, 26, 48
        PROLOG_SAVE_REG_PAIR 27, 28, 64
        PROLOG_SAVE_REG_PAIR 29, 30, 80
        PROLOG_SAVE_REG_PAIR 31, 2, 96
        // } end PAL_LIMITED_CONTEXT

        PROLOG_STACK_ALLOC STACKSIZEOF_ExInfo
    .endm

// -----------------------------------------------------------------------------
// Macro used to create frame of funclet calling helpers (RhpCallXXXXFunclet)
// extraStackSize - extra stack space that the user of the macro can use to
//                   store additional registers
    .macro ALLOC_CALL_FUNCLET_FRAME extraStackSize

        // Using below prolog instead of PROLOG_SAVE_REG_PAIR fp,ra, #-60!
        // is intentional. Above statement would also emit instruction to save
        // sp in fp. If sp is saved in fp in prolog then it is not expected that fp can change in the body
        // of method. However, this method needs to be able to change fp before calling funclet.
        // This is required to access locals in funclet.
        PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED 22, 1, 96
        PROLOG_SAVE_REG_PAIR 23, 24, 16
        PROLOG_SAVE_REG_PAIR 25, 26, 32
        PROLOG_SAVE_REG_PAIR 27, 28, 48
        PROLOG_SAVE_REG_PAIR 29, 30, 64
        PROLOG_SAVE_REG_PAIR 31, 2, 80
        addi  fp, sp, 0
        .cfi_def_cfa_register 22 //fp

        .if \extraStackSize != 0
            PROLOG_STACK_ALLOC \extraStackSize
        .endif
    .endm

// -----------------------------------------------------------------------------
// Macro used to free frame of funclet calling helpers (RhpCallXXXXFunclet)
// extraStackSize - extra stack space that the user of the macro can use to
//                   store additional registers.
//                   It needs to match the value passed to the corresponding
//                   ALLOC_CALL_FUNCLET_FRAME.
    .macro FREE_CALL_FUNCLET_FRAME extraStackSize

        .if \extraStackSize != 0
            EPILOG_STACK_FREE \extraStackSize
        .endif

        EPILOG_RESTORE_REG_PAIR 23, 24, 16
        EPILOG_RESTORE_REG_PAIR 25, 26, 32
        EPILOG_RESTORE_REG_PAIR 27, 28, 48
        EPILOG_RESTORE_REG_PAIR 29, 30, 64
        EPILOG_RESTORE_REG_PAIR 31, 2, 80
        EPILOG_RESTORE_REG_PAIR_INDEXED 22, 1, 96
   .endm

// -----------------------------------------------------------------------------
// Macro used to restore preserved general purpose and FP registers from REGDISPLAY
// regdisplayReg - register pointing to the REGDISPLAY structure
   .macro RESTORE_PRESERVED_REGISTERS regdisplayReg

        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR23
        ld    s0, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR24
        ld    s1, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR25
        ld    s2, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR26
        ld    s3, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR27
        ld    s4, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR28
        ld    s5, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR29
        ld    s6, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR30
        ld    s7, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR31
        ld    s8, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pFP
        ld    fp, t3, 0

        // load FP preserved regs
        addi  t3, \regdisplayReg, OFFSETOF__REGDISPLAY__F
        fld   f24, t3, 0x00
        fld   f25, t3, 0x08
        fld   f26, t3, 0x10
        fld   f27, t3, 0x18
        fld   f28, t3, 0x20
        fld   f29, t3, 0x28
        fld   f30, t3, 0x30
        fld   f31, t3, 0x38
    .endm

// -----------------------------------------------------------------------------
// Macro used to save preserved general purpose and FP registers to REGDISPLAY
// regdisplayReg - register pointing to the REGDISPLAY structure
   .macro SAVE_PRESERVED_REGISTERS regdisplayReg
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR23
        sd    s0, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR24
        sd    s1, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR25
        sd    s2, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR26
        sd    s3, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR27
        sd    s4, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR28
        sd    s5, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR29
        sd    s6, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR30
        sd    s7, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR31
        sd    s8, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pFP
        sd    fp, t3, 0

        // store FP preserved regs
        addi  t3, \regdisplayReg, OFFSETOF__REGDISPLAY__F
        fsd   f24, t3, 0x00
        fsd   f25, t3, 0x08
        fsd   f26, t3, 0x10
        fsd   f27, t3, 0x18
        fsd   f28, t3, 0x20
        fsd   f29, t3, 0x28
        fsd   f30, t3, 0x30
        fsd   f31, t3, 0x38
    .endm

// -----------------------------------------------------------------------------
// Macro used to thrash preserved general purpose registers in REGDISPLAY
// to make sure nobody uses them
// regdisplayReg - register pointing to the REGDISPLAY structure
    .macro TRASH_PRESERVED_REGISTERS_STORAGE regdisplayReg

#if _DEBUG
        lui   a3, 0xBAD // 0xBAAD
        ori   a3, a3, 0xEED
        lui   t3, 0xDDEE // 0xDDEED
        addi  t3, t3, -1110 // 0xBAA
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR23
        sd    a3, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR24
        sd    a3, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR25
        sd    a3, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR26
        sd    a3, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR27
        sd    a3, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR28
        sd    a3, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR29
        sd    a3, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR30
        sd    a3, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR31
        sd    a3, t3, 0
        ld    t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pFP
        sd    a3, t3, 0
#endif // _DEBUG
    .endm

.macro GetThreadA2
    addi  sp, sp, -16
    sd    a0, 0(sp)
    sd    a1, 8(sp)
    call  C_FUNC(RhpGetThread)
    addi  a2, a0, 0
    ld    a0, 0(sp)
    ld    a1, 8(sp)
    addi  sp, sp, 16
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

        GetThreadA2

        addi  a1, sp, rsp_offsetof_ExInfo            // a1 <- ExInfo*
        sd    zero, 0(a1)      // pExInfo->m_exception = null
        li    a3, 1
        sb    a3, 8(a1)       // pExInfo->m_passNumber = 1
        li    a3, -1
        sw    a3, 12(a1)      // pExInfo->m_idxCurClause = MaxTryRegionIdx
        li    a3, 2
        sb    a3, 16(a1)      // pExInfo->m_kind = ExKind.HardwareFault

        // link the ExInfo into the thread's ExInfo chain
        ld    a3, OFFSETOF__Thread__m_pExInfoStackHead(a2)
        sd    a3, OFFSETOF__ExInfo__m_pPrevExInfo(a1)       // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        sd    a1, OFFSETOF__Thread__m_pExInfoStackHead(a2)  // m_pExInfoStackHead = pExInfo

        // set the exception context field on the ExInfo
        addi  a2, sp, rsp_offsetof_Context                // a2 <- PAL_LIMITED_CONTEXT*
        sd    a2, OFFSETOF__ExInfo__m_pExContext(a1)        // pExInfo->m_pExContext = pContext

        // a0[31:0]: exception code
        // a1: ExInfo*
        call  C_FUNC(RhThrowHwEx)

    ALTERNATE_ENTRY RhpThrowHwEx2

        // no return
        EMIT_BREAKPOINT

    NESTED_END RhpThrowHwEx, _TEXT

//
// RhpThrowEx
//
// INPUT:  a0:  exception object
//
// OUTPUT:
//

    NESTED_ENTRY RhpThrowEx, _TEXT, NoHandler

        ALLOC_THROW_FRAME SOFTWARE_EXCEPTION

        GetThreadA2

        // There is runtime C# code that can tail call to RhpThrowEx using a binder intrinsic.  So the return
        // address could have been hijacked when we were in that C# code and we must remove the hijack and
        // reflect the correct return address in our exception context record.  The other throw helpers don't
        // need this because they cannot be tail-called from C#.

        // NOTE: we cannot use INLINE_THREAD_UNHIJACK because it will write into the stack at the location
        // where the tail-calling thread had saved RA, which may not match where we have saved RA.

        ld    a1, OFFSETOF__Thread__m_pvHijackedReturnAddress(a2)
        beq   a1, zero, NotHijacked

        ld    a3, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation(a2)

        // a0: exception object
        // a1: hijacked return address
        // a2: pThread
        // a3: hijacked return address location

        addi  t3, sp, STACKSIZEOF_ExInfo + SIZEOF__PAL_LIMITED_CONTEXT // re-compute SP at callsite
        bltu  a3, t3, TailCallWasHijacked // if (m_ppvHijackedReturnAddressLocation < SP at callsite)

        // normal case where a valid return address location is hijacked
        sd    a1, 0(a3)
        b     ClearThreadState

TailCallWasHijacked:

        // Abnormal case where the return address location is now invalid because we ended up here via a tail
        // call.  In this case, our hijacked return address should be the correct caller of this method.

        // stick the previous return address in RA as well as in the right spots in our PAL_LIMITED_CONTEXT.
        ori   ra, a1, zero
        sd    ra, rsp_offsetof_Context(sp) + OFFSETOF__PAL_LIMITED_CONTEXT__RA
        sd    ra, rsp_offsetof_Context(sp) + OFFSETOF__PAL_LIMITED_CONTEXT__IP

ClearThreadState:

        // clear the Thread's hijack state
        sd    zero, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation(a2)
        sd    zero, OFFSETOF__Thread__m_pvHijackedReturnAddress(a2)

NotHijacked:

        addi  a1, sp, rsp_offsetof_ExInfo            // a1 <- ExInfo*
        sd    zero, OFFSETOF__ExInfo__m_exception(a1)  // pExInfo->m_exception = null
        li    a3, 1
        sb    a3, OFFSETOF__ExInfo__m_passNumber(a1)   // pExInfo->m_passNumber = 1
        li    a3, -1
        sw    a3, OFFSETOF__ExInfo__m_idxCurClause(a1) // pExInfo->m_idxCurClause = MaxTryRegionIdx
        li    a3, 1
        sb    a3, OFFSETOF__ExInfo__m_kind(a1)         // pExInfo->m_kind = ExKind.Throw

        // link the ExInfo into the thread's ExInfo chain
        ld    a3, OFFSETOF__Thread__m_pExInfoStackHead(a2)
        sd    a3, OFFSETOF__ExInfo__m_pPrevExInfo(a1)       // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        sd    a1, OFFSETOF__Thread__m_pExInfoStackHead(a2)  // m_pExInfoStackHead = pExInfo

        // set the exception context field on the ExInfo
        addi  a2, sp, rsp_offsetof_Context                // a2 <- PAL_LIMITED_CONTEXT*
        sd    a2, OFFSETOF__ExInfo__m_pExContext(a1)        // pExInfo->m_pExContext = pContext

        // a0: exception object
        // a1: ExInfo*
        call  RhThrowEx

    ALTERNATE_ENTRY RhpThrowEx2

        // no return
        BREAK
    NESTED_END RhpThrowEx, _TEXT


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

        GetThreadA2

        addi  a1, sp, rsp_offsetof_ExInfo            // a1 <- ExInfo*
        sd    zero, OFFSETOF__ExInfo__m_exception(a1)  // pExInfo->m_exception = null
        sb    zero, OFFSETOF__ExInfo__m_kind(a1)       // init to a deterministic value (ExKind.None)
        li    a3, 1
        sb    a3, OFFSETOF__ExInfo__m_passNumber(a1)   // pExInfo->m_passNumber = 1
        li    a3, -1
        sw    a3, OFFSETOF__ExInfo__m_idxCurClause(a1) // pExInfo->m_idxCurClause = MaxTryRegionIdx

        // link the ExInfo into the thread's ExInfo chain
        ld    a3, OFFSETOF__Thread__m_pExInfoStackHead(a2)
        mv    a0, a3                                      // a0 <- current ExInfo
        sd    a3, OFFSETOF__ExInfo__m_pPrevExInfo(a1)       // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        sd    a1, OFFSETOF__Thread__m_pExInfoStackHead(a2)  // m_pExInfoStackHead = pExInfo

        // set the exception context field on the ExInfo
        addi  a2, sp, rsp_offsetof_Context                // a2 <- PAL_LIMITED_CONTEXT*
        sd    a2, OFFSETOF__ExInfo__m_pExContext(a1)        // pExInfo->m_pExContext = pContext

        // a0 contains the currently active ExInfo
        // a1 contains the address of the new ExInfo
        call  RhRethrow

    ALTERNATE_ENTRY RhpRethrow2

        // no return
        BREAK
    NESTED_END RhpRethrow, _TEXT

//
// void* FASTCALL RhpCallCatchFunclet(OBJECTREF exceptionObj, void* pHandlerIP, REGDISPLAY* pRegDisplay,
//                                    ExInfo* pExInfo)
//
// INPUT:  a0:  exception object
//         a1:  handler funclet address
//         a2:  REGDISPLAY*
//         a3:  ExInfo*
//
// OUTPUT:
//

    NESTED_ENTRY RhpCallCatchFunclet, _TEXT, NoHandler

        ALLOC_CALL_FUNCLET_FRAME 0x70
        fsd   f24, 0(sp)
        fsd   f25, 8(sp)
        fsd   f26, 16(sp)
        fsd   f27, 24(sp)
        fsd   f28, 32(sp)
        fsd   f29, 40(sp)
        fsd   f30, 48(sp)
        fsd   f31, 56(sp)
        sd    a0, 64(sp)    // a0 to a3 are stored to restore them anytime
        sd    a1, 72(sp)
        sd    a2, 80(sp)
        sd    a3, 88(sp)
        sd    zero, 96(sp)  // $zero makes space for the local "is_not_handling_thread_abort"; last qword will store the thread obj

#define rsp_offset_is_not_handling_thread_abort 96
#define rsp_offset_a0 64
#define rsp_offset_a1 72
#define rsp_offset_a2 80
#define rsp_offset_a3 88
#define rsp_CatchFunclet_offset_thread 104

        //
        // clear the DoNotTriggerGc flag, trashes a4-a6
        //

        call  RhpGetThread
        sd    a0, rsp_CatchFunclet_offset_thread(sp)
        mv    a5, a0
        ld    a0, rsp_offset_a0(sp)
        ld    a1, rsp_offset_a1(sp)
        ld    a2, rsp_offset_a2(sp)
        ld    a3, rsp_offset_a3(sp)

        ld    a4, OFFSETOF__Thread__m_threadAbortException(a5)
        sub   a4, a4, a0
        sd    a4, rsp_offset_is_not_handling_thread_abort(sp) // Non-zero if the exception is not ThreadAbortException

        addi  t3, a5, OFFSETOF__Thread__m_ThreadStateFlags

        addi  a6, zero, -17    // a6 = a6 & ~TSF_DoNotTriggerGc, TSF_DoNotTriggerGc=0x10.
        and   a4, a6, t3

        //
        // set preserved regs to the values expected by the funclet
        //
        RESTORE_PRESERVED_REGISTERS  a2
        //
        // trash the values at the old homes to make sure nobody uses them
        //
        TRASH_PRESERVED_REGISTERS_STORAGE  a2

        //
        // call the funclet
        //
        // a0 still contains the exception object
        jalr  ra, a1, 0

    ALTERNATE_ENTRY RhpCallCatchFunclet2

        // a0 contains resume IP

        ld    a2, rsp_offset_a2(sp)                     // a2 <- REGDISPLAY*

#ifdef _DEBUG
        // Call into some C++ code to validate the pop of the ExInfo.  We only do this in debug because we
        // have to spill all the preserved registers and then refill them after the call.

        sd    a0, rsp_offset_a0(sp)

        SAVE_PRESERVED_REGISTERS  a2

        ld    a0, rsp_CatchFunclet_offset_thread(sp)    // a0 <- Thread*
        ld    a1, rsp_offset_a3(sp)                     // a1 <- current ExInfo*
        ld    a2, OFFSETOF__REGDISPLAY__SP(a2)          // a2 <- resume SP value
        call  RhpValidateExInfoPop

        ld    a2, rsp_offset_a2(sp)                     // a2 <- REGDISPLAY*

        RESTORE_PRESERVED_REGISTERS  a2

        ld    a0, rsp_offset_a0(sp)                     // reload resume IP
#endif

        ld    a1, rsp_CatchFunclet_offset_thread(sp)

        // We must unhijack the thread at this point because the section of stack where the hijack is applied
        // may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK  a1, a3, t3                   // Thread in a1, trashes a3 and t3

        ld    a3, rsp_offset_a3(sp)                           // a3 <- current ExInfo*
        ld    a2, OFFSETOF__REGDISPLAY__SP(a2)                // a2 <- resume SP value

PopExInfoLoop:
        ld    a3, OFFSETOF__ExInfo__m_pPrevExInfo(a3)         // a3 <- next ExInfo
        beq   a3, zero, DonePopping                            // if (pExInfo == null) { we're done }
        blt   a3, a2, PopExInfoLoop                            // if (pExInfo < resume SP} { keep going }

DonePopping:
        sd    a3, OFFSETOF__Thread__m_pExInfoStackHead(a1)    // store the new head on the Thread

        PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, a3

        andi  t7, a3, TrapThreadsFlags_AbortInProgress_Bit
        beq   t7, zero, NoAbort

        ld    a3, rsp_offset_is_not_handling_thread_abort(sp)
        bne   a3, zero, NoAbort

        // It was the ThreadAbortException, so rethrow it
        // reset SP
        mv    a1, a0                                        // a1 <- continuation address as exception PC
        li    a0, STATUS_REDHAWK_THREAD_ABORT
        mv    sp, a2
        call  RhpThrowHwEx

NoAbort:
        // reset SP and jump to continuation address
        mv    sp, a2
        jalr  zero, a0, 0

#undef rsp_offset_is_not_handling_thread_abort
#undef rsp_offset_a0
#undef rsp_offset_a1
#undef rsp_offset_a2
#undef rsp_offset_a3
#undef rsp_CatchFunclet_offset_thread

    NESTED_END RhpCallCatchFunclet, _TEXT

//
// void FASTCALL RhpCallFinallyFunclet(void* pHandlerIP, REGDISPLAY* pRegDisplay)
//
// INPUT:  a0:  handler funclet address
//         a1:  REGDISPLAY*
//
// OUTPUT:
//

    NESTED_ENTRY RhpCallFinallyFunclet, _TEXT, NoHandler

        ALLOC_CALL_FUNCLET_FRAME 0x60
        fsd   f24, 0(sp)
        fsd   f25, 8(sp)
        fsd   f26, 16(sp)
        fsd   f27, 24(sp)
        fsd   f28, 32(sp)
        fsd   f29, 40(sp)
        fsd   f30, 48(sp)
        fsd   f31, 56(sp)
        sd    a0, 64(sp)          // a0 and a1 are saved so we have them later
        sd    a1, 72(sp)

#define rsp_offset_a1 72
#define rsp_FinallyFunclet_offset_thread 80

        // We want to suppress hijacking between invocations of subsequent finallys.  We do this because we
        // cannot tolerate a GC after one finally has run (and possibly side-effected the GC state of the
        // method) and then been popped off the stack, leaving behind no trace of its effect.
        //
        // So we clear the state before and set it after invocation of the handler.
        //

        // clear the DoNotTriggerGc flag, trashes a2-a4
        call  RhpGetThread
        sd    a0, rsp_FinallyFunclet_offset_thread(sp)
        mv    a2, a0
        ld    a0, rsp_offset_a0(sp)
        ld    a1, rsp_offset_a1(sp)

        addi  t3, a2, OFFSETOF__Thread__m_ThreadStateFlags

        addi  a3, zero, -17     // a3 = a3 & ~TSF_DoNotTriggerGc, TSF_DoNotTriggerGc=0x10.
        and   a4, a3, t3

        // set preserved regs to the values expected by the funclet
        RESTORE_PRESERVED_REGISTERS a1
        // trash the values at the old homes to make sure nobody uses them
        TRASH_PRESERVED_REGISTERS_STORAGE a1

        // call the funclet
        jalr  ra, a0, 0

    ALTERNATE_ENTRY RhpCallFinallyFunclet2

        ld    a1, rsp_offset_a1(sp) // reload REGDISPLAY pointer

        // save new values of preserved regs into REGDISPLAY
        SAVE_PRESERVED_REGISTERS a1

        // set the DoNotTriggerGc flag, trashes a1-a3
        ld    a2, rsp_FinallyFunclet_offset_thread(sp)
        addi  t3, a2, OFFSETOF__Thread__m_ThreadStateFlags
        addi  a3, zero, -17     // a3 = a3 & ~TSF_DoNotTriggerGc, TSF_DoNotTriggerGc=0x10.
        and   a1, a3, t3

        fld   f24, 0(sp)
        fld   f25, 8(sp)
        fld   f26, 16(sp)
        fld   f27, 24(sp)
        fld   f28, 32(sp)
        fld   f29, 40(sp)
        fld   f30, 48(sp)
        fld   f31, 56(sp)

        FREE_CALL_FUNCLET_FRAME 0x60
        EPILOG_RETURN

#undef rsp_offset_a1
#undef rsp_FinallyFunclet_offset_thread

    NESTED_END RhpCallFinallyFunclet, _TEXT


//
// void* FASTCALL RhpCallFilterFunclet(OBJECTREF exceptionObj, void* pFilterIP, REGDISPLAY* pRegDisplay)
//
// INPUT:  a0:  exception object
//         a1:  filter funclet address
//         a2:  REGDISPLAY*
//
// OUTPUT:
//

    NESTED_ENTRY RhpCallFilterFunclet, _TEXT, NoHandler
        ALLOC_CALL_FUNCLET_FRAME 0x40
        fsd   f24, 0(sp)
        fsd   f25, 8(sp)
        fsd   f26, 16(sp)
        fsd   f27, 24(sp)
        fsd   f28, 32(sp)
        fsd   f29, 40(sp)
        fsd   f30, 48(sp)
        fsd   f31, 56(sp)

        ld    t3, OFFSETOF__REGDISPLAY__pFP(a2)
        ld    fp, t3, 0

        // call the funclet
        // a0 still contains the exception object
        jalr  ra, a1, 0

    ALTERNATE_ENTRY RhpCallFilterFunclet2

        fld   f24, 0(sp)
        fld   f25, 8(sp)
        fld   f26, 16(sp)
        fld   f27, 24(sp)
        fld   f28, 32(sp)
        fld   f29, 40(sp)
        fld   f30, 48(sp)
        fld   f31, 56(sp)

        FREE_CALL_FUNCLET_FRAME 0x40
        EPILOG_RETURN

    NESTED_END RhpCallFilterFunclet, _TEXT

#ifdef FEATURE_OBJCMARSHAL

//
// void* FASTCALL RhpCallPropagateExceptionCallback(void* pCallbackContext, void* pCallback, REGDISPLAY* pRegDisplay,
//                                    ExInfo* pExInfo, PInvokeTransitionFrame* pPreviousTransitionFrame)
//
// INPUT:  a0:  callback context
//         a1:  callback
//         a2:  REGDISPLAY*
//         a3:  ExInfo*
//         a4:  pPreviousTransitionFrame
//
// OUTPUT:
//

    NESTED_ENTRY RhpCallPropagateExceptionCallback, _TEXT, NoHandler

#define rsp_offset_a0 16
#define rsp_offset_a1 24
#define rsp_offset_a2 32
#define rsp_offset_a3 40
#define rsp_offset_a4 48
#define rsp_CallPropagationCallback_offset_thread 56

        // Using the NO_FP macro so that the debugger unwinds using SP.
        // This makes backtraces work even after using RESTORE_PRESERVED_REGISTERS.
        PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED 22, 1, 64
        mv    fp, sp
        sd    a0, rsp_offset_a0(sp)  // a0 to a4 are stored to restore them anytime
        sd    a1, rsp_offset_a1(sp)
        sd    a2, rsp_offset_a2(sp)
        sd    a3, rsp_offset_a3(sp)
        sd    a4, rsp_offset_a4(sp)
        sd    zero, rsp_CallPropagationCallback_offset_thread(sp) // zero makes space to store the thread obj

        // clear the DoNotTriggerGc flag, trashes a4-a6
        call  RhpGetThread
        sd    a0, rsp_CallPropagationCallback_offset_thread(sp)
        mv    a5, a0
        ld    a0, rsp_offset_a0(sp)
        ld    a1, rsp_offset_a1(sp)
        ld    a2, rsp_offset_a2(sp)
        ld    a3, rsp_offset_a3(sp)

        addi  t3, a5, OFFSETOF__Thread__m_ThreadStateFlags

        addi  a6, zero, -17    // a6 = a6 & ~TSF_DoNotTriggerGc, TSF_DoNotTriggerGc=0x10.
        and   a4, a6, t3

        // set preserved regs to the values expected by the funclet
        RESTORE_PRESERVED_REGISTERS a2
        // trash the values at the old homes to make sure nobody uses them
        TRASH_PRESERVED_REGISTERS_STORAGE a2

#ifdef _DEBUG
        // Call into some C++ code to validate the pop of the ExInfo.  We only do this in debug because we
        // have to spill all the preserved registers and then refill them after the call.

        SAVE_PRESERVED_REGISTERS a2

        ld    a0, rsp_CallPropagationCallback_offset_thread(sp)  // a0 <- Thread*
        ld    a1, rsp_offset_a3(sp)                              // a1 <- current ExInfo*
        ld    a2, a2, OFFSETOF__REGDISPLAY__SP                   // a2 <- resume SP value
        call  RhpValidateExInfoPop

        ld    a2, rsp_offset_a2(sp)                              // a2 <- REGDISPLAY*

        RESTORE_PRESERVED_REGISTERS a2
#endif

        ld    a1, rsp_CallPropagationCallback_offset_thread(sp)

        // We must unhijack the thread at this point because the section of stack where the hijack is applied
        // may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK a1, a3, t3            // Thread in a1, trashes a3 and t3

        ld    a3, rsp_offset_a3(sp)                    // a3 <- current ExInfo*
        ld    a2, a2, OFFSETOF__REGDISPLAY__SP         // a2 <- resume SP value

Propagate_PopExInfoLoop:
        ld    a3, a3, OFFSETOF__ExInfo__m_pPrevExInfo  // a3 <- next ExInfo
        beqz  a3, Propagate_DonePopping           // if (pExInfo == null) { we're done }
        blt   a3, a2, Propagate_PopExInfoLoop    // if (pExInfo < resume SP) { keep going }

Propagate_DonePopping:
        sd    a3, a1, OFFSETOF__Thread__m_pExInfoStackHead // store the new head on the Thread

        // restore preemptive mode
        ld    a4, rsp_offset_a4(sp)                    // pPreviousTransitionFrame
        sd    a4, a1, OFFSETOF__Thread__m_pTransitionFrame

        // reset SP and RA and jump to continuation address
        ld    a0, rsp_offset_a0(sp)                    // callback context
        ld    a1, rsp_offset_a1(sp)                    // callback
        ld    a2, rsp_offset_a2(sp)                    // REGDISPLAY*
        ld    a3, a2, OFFSETOF__REGDISPLAY__pRA        // a3 <- &resume RA value
        ld    ra, a3
        ld    a3, a2, OFFSETOF__REGDISPLAY__SP         // a3 <- resume SP value
        mv    sp, a3
        jalr  ra, a1, 0

#undef rsp_offset_a0
#undef rsp_offset_a1
#undef rsp_offset_a2
#undef rsp_offset_a3
#undef rsp_CallPropagationCallback_offset_thread

    NESTED_END RhpCallPropagateExceptionCallback, _TEXT

#endif // FEATURE_OBJCMARSHAL
