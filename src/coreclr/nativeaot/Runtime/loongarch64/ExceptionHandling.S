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

        ori  $a3, $sp, 0

        // Setup a PAL_LIMITED_CONTEXT on the stack {
        .if \exceptionType == HARDWARE_EXCEPTION
            addi.d  $sp, $sp, -0x50
            .cfi_adjust_cfa_offset  0x50
            st.d  $a3, $sp, 0      // a3 is the SP and a1 is the IP of the fault site
            st.d  $a1, $sp, 8
        .else
            PROLOG_STACK_ALLOC  0x50
            .cfi_adjust_cfa_offset  0x50
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
        PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, -0x70
        st.d  $zero, $sp, 0x10 // locations reserved for return value, not used for exception handling
        st.d  $zero, $sp, 0x18
        PROLOG_SAVE_REG_PAIR  23, 24, 0x20
        PROLOG_SAVE_REG_PAIR  25, 26, 0x30
        PROLOG_SAVE_REG_PAIR  27, 28, 0x40
        PROLOG_SAVE_REG_PAIR  29, 30, 0x50
        PROLOG_SAVE_REG_PAIR  31,  2, 0x60
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
        PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED  22, 1, -0x60
        PROLOG_SAVE_REG_PAIR  23, 24, 0x10
        PROLOG_SAVE_REG_PAIR  25, 26, 0x20
        PROLOG_SAVE_REG_PAIR  27, 28, 0x30
        PROLOG_SAVE_REG_PAIR  29, 30, 0x40
        PROLOG_SAVE_REG_PAIR  31,  2, 0x50
        ori  $fp, $sp, 0
        .cfi_def_cfa_register  22 //fp

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

        EPILOG_RESTORE_REG_PAIR  23, 24, 0x10
        EPILOG_RESTORE_REG_PAIR  25, 26, 0x20
        EPILOG_RESTORE_REG_PAIR  27, 28, 0x30
        EPILOG_RESTORE_REG_PAIR  29, 30, 0x40
        EPILOG_RESTORE_REG_PAIR  31,  2, 0x50
        EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 0x60
   .endm


// -----------------------------------------------------------------------------
// Macro used to restore preserved general purpose and FP registers from REGDISPLAY
// regdisplayReg - register pointing to the REGDISPLAY structure
   .macro RESTORE_PRESERVED_REGISTERS regdisplayReg

        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR23
        ld.d  $s0, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR24
        ld.d  $s1, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR25
        ld.d  $s2, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR26
        ld.d  $s3, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR27
        ld.d  $s4, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR28
        ld.d  $s5, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR29
        ld.d  $s6, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR30
        ld.d  $s7, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR31
        ld.d  $s8, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pFP
        ld.d  $fp, $t3, 0
        //
        // load FP preserved regs
        //
        addi.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__F
        fld.d  $f24, $t3, 0x00
        fld.d  $f25, $t3, 0x08
        fld.d  $f26, $t3, 0x10
        fld.d  $f27, $t3, 0x18
        fld.d  $f28, $t3, 0x20
        fld.d  $f29, $t3, 0x28
        fld.d  $f30, $t3, 0x30
        fld.d  $f31, $t3, 0x38
    .endm

// -----------------------------------------------------------------------------
// Macro used to save preserved general purpose and FP registers to REGDISPLAY
// regdisplayReg - register pointing to the REGDISPLAY structure
   .macro SAVE_PRESERVED_REGISTERS regdisplayReg
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR23
        st.d  $s0, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR24
        st.d  $s1, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR25
        st.d  $s2, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR26
        st.d  $s3, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR27
        st.d  $s4, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR28
        st.d  $s5, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR29
        st.d  $s6, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR30
        st.d  $s7, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR31
        st.d  $s8, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pFP
        st.d  $fp, $t3, 0
        //
        // store vfp preserved regs
        //
        addi.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__F
        fst.d  $f24, $t3, 0x00
        fst.d  $f25, $t3, 0x08
        fst.d  $f26, $t3, 0x10
        fst.d  $f27, $t3, 0x18
        fst.d  $f28, $t3, 0x20
        fst.d  $f29, $t3, 0x28
        fst.d  $f30, $t3, 0x30
        fst.d  $f31, $t3, 0x38
    .endm


// -----------------------------------------------------------------------------
// Macro used to thrash preserved general purpose registers in REGDISPLAY
// to make sure nobody uses them
// regdisplayReg - register pointing to the REGDISPLAY structure
   .macro TRASH_PRESERVED_REGISTERS_STORAGE regdisplayReg

#if _DEBUG
        lu12i.w  $a3, 0xbaadd
        ori  $a3, $a3, 0xeed
        lu32i.d  $a3, 0xddeed
        lu52i.d  $a3, $a3, 0xbaa
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR23
        st.d  $a3, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR24
        st.d  $a3, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR25
        st.d  $a3, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR26
        st.d  $a3, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR27
        st.d  $a3, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR28
        st.d  $a3, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR29
        st.d  $a3, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR30
        st.d  $a3, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pR31
        st.d  $a3, $t3, 0
        ld.d  $t3, \regdisplayReg, OFFSETOF__REGDISPLAY__pFP
        st.d  $a3, $t3, 0
#endif // _DEBUG
    .endm

.macro GetThreadX2
    st.d  $a0, $sp, -16
    st.d  $a1, $sp, -8
    addi.d  $sp, $sp, -16
    bl  C_FUNC(RhpGetThread)
    ori  $a2, $a0, 0
    ld.d  $a0, $sp, 0
    ld.d  $a1, $sp, 8
    addi.d $sp, $sp, 16
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

        GetThreadX2

        addi.d  $a1, $sp, rsp_offsetof_ExInfo            // a1 <- ExInfo*
        st.d  $zero, $a1, OFFSETOF__ExInfo__m_exception  // pExInfo->m_exception = null
        ori  $a3, $zero, 1
        st.b  $a3, $a1, OFFSETOF__ExInfo__m_passNumber   // pExInfo->m_passNumber = 1
        lu12i.w  $a3, -1
        ori  $a3, $a3, 0xfff
        st.w  $a3, $a1, OFFSETOF__ExInfo__m_idxCurClause // pExInfo->m_idxCurClause = MaxTryRegionIdx
        ori  $a3, $zero, 2
        st.b  $a3, $a1, OFFSETOF__ExInfo__m_kind         // pExInfo->m_kind = ExKind.HardwareFault

        // link the ExInfo into the thread's ExInfo chain
        ld.d  $a3, $a2, OFFSETOF__Thread__m_pExInfoStackHead
        st.d  $a3, $a1, OFFSETOF__ExInfo__m_pPrevExInfo       // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        st.d  $a1, $a2, OFFSETOF__Thread__m_pExInfoStackHead  // m_pExInfoStackHead = pExInfo

        // set the exception context field on the ExInfo
        addi.d  $a2, $sp, rsp_offsetof_Context                // a2 <- PAL_LIMITED_CONTEXT*
        st.d  $a2, $a1, OFFSETOF__ExInfo__m_pExContext        // pExInfo->m_pExContext = pContext

        // a0[31:0]: exception code
        // a1: ExInfo*
        bl  C_FUNC(RhThrowHwEx)

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

        GetThreadX2

        // There is runtime C# code that can tail call to RhpThrowEx using a binder intrinsic.  So the return
        // address could have been hijacked when we were in that C# code and we must remove the hijack and
        // reflect the correct return address in our exception context record.  The other throw helpers don't
        // need this because they cannot be tail-called from C#.

        // NOTE: we cannot use INLINE_THREAD_UNHIJACK because it will write into the stack at the location
        // where the tail-calling thread had saved RA, which may not match where we have saved RA.

        ld.d  $a1, $a2, OFFSETOF__Thread__m_pvHijackedReturnAddress
        beq  $a1, $zero, NotHijacked

        ld.d  $a3, $a2, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation

        // a0: exception object
        // a1: hijacked return address
        // a2: pThread
        // a3: hijacked return address location

        addi.d  $t3, $sp, (STACKSIZEOF_ExInfo + SIZEOF__PAL_LIMITED_CONTEXT) // re-compute SP at callsite
        bltu  $a3, $t3, TailCallWasHijacked // if (m_ppvHijackedReturnAddressLocation < SP at callsite)

        // normal case where a valid return address location is hijacked
        st.d  $a1, $a3, 0
        b  ClearThreadState

TailCallWasHijacked:

        // Abnormal case where the return address location is now invalid because we ended up here via a tail
        // call.  In this case, our hijacked return address should be the correct caller of this method.

        // stick the previous return address in RA as well as in the right spots in our PAL_LIMITED_CONTEXT.
        ori  $ra, $a1, 0
        st.d  $ra, $sp, (rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__RA)
        st.d  $ra, $sp, (rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__IP)

ClearThreadState:

        // clear the Thread's hijack state
        st.d  $zero, $a2, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation
        st.d  $zero, $a2, OFFSETOF__Thread__m_pvHijackedReturnAddress

NotHijacked:

        addi.d  $a1, $sp, rsp_offsetof_ExInfo            // a1 <- ExInfo*
        st.d  $zero, $a1, OFFSETOF__ExInfo__m_exception  // pExInfo->m_exception = null
        ori  $a3, $zero, 1
        st.b  $a3, $a1, OFFSETOF__ExInfo__m_passNumber   // pExInfo->m_passNumber = 1
        lu12i.w  $a3, -1
        ori  $a3, $a3, 0xfff
        st.w  $a3, $a1, OFFSETOF__ExInfo__m_idxCurClause // pExInfo->m_idxCurClause = MaxTryRegionIdx
        ori  $a3, $zero, 1
        st.b  $a3, $a1, OFFSETOF__ExInfo__m_kind         // pExInfo->m_kind = ExKind.Throw

        // link the ExInfo into the thread's ExInfo chain
        ld.d  $a3, $a2, OFFSETOF__Thread__m_pExInfoStackHead
        st.d  $a3, $a1, OFFSETOF__ExInfo__m_pPrevExInfo       // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        st.d  $a1, $a2, OFFSETOF__Thread__m_pExInfoStackHead  // m_pExInfoStackHead = pExInfo

        // set the exception context field on the ExInfo
        addi.d  $a2, $sp, rsp_offsetof_Context                // a2 <- PAL_LIMITED_CONTEXT*
        st.d  $a2, $a1, OFFSETOF__ExInfo__m_pExContext        // pExInfo->m_pExContext = pContext

        // a0: exception object
        // a1: ExInfo*
        bl  C_FUNC(RhThrowEx)

    ALTERNATE_ENTRY RhpThrowEx2

        // no return
        EMIT_BREAKPOINT
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

        GetThreadX2

        addi.d  $a1, $sp, rsp_offsetof_ExInfo            // a1 <- ExInfo*
        st.d  $zero, $a1, OFFSETOF__ExInfo__m_exception  // pExInfo->m_exception = null
        st.b  $zero, $a1, OFFSETOF__ExInfo__m_kind       // init to a deterministic value (ExKind.None)
        ori  $a3, $zero, 1
        st.b  $a3, $a1, OFFSETOF__ExInfo__m_passNumber   // pExInfo->m_passNumber = 1
        lu12i.w  $a3, -1
        ori  $a3, $a3, 0xfff
        st.w  $a3, $a1, OFFSETOF__ExInfo__m_idxCurClause // pExInfo->m_idxCurClause = MaxTryRegionIdx

        // link the ExInfo into the thread's ExInfo chain
        ld.d  $a3, $a2, OFFSETOF__Thread__m_pExInfoStackHead
        ori  $a0, $a3, 0                                      // a0 <- current ExInfo
        st.d  $a3, $a1, OFFSETOF__ExInfo__m_pPrevExInfo       // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        st.d  $a1, $a2, OFFSETOF__Thread__m_pExInfoStackHead  // m_pExInfoStackHead = pExInfo

        // set the exception context field on the ExInfo
        addi.d  $a2, $sp, rsp_offsetof_Context                // a2 <- PAL_LIMITED_CONTEXT*
        st.d  $a2, $a1, OFFSETOF__ExInfo__m_pExContext        // pExInfo->m_pExContext = pContext

        // a0 contains the currently active ExInfo
        // a1 contains the address of the new ExInfo
        bl  C_FUNC(RhRethrow)

    ALTERNATE_ENTRY RhpRethrow2

        // no return
        EMIT_BREAKPOINT
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

        ALLOC_CALL_FUNCLET_FRAME 0x70 // Size needs to be equal with ExceptionHandling.asm variant of this function
        fst.d  $f24, $sp, 0x00
        fst.d  $f25, $sp, 0x08
        fst.d  $f26, $sp, 0x10
        fst.d  $f27, $sp, 0x18
        fst.d  $f28, $sp, 0x20
        fst.d  $f29, $sp, 0x28
        fst.d  $f30, $sp, 0x30
        fst.d  $f31, $sp, 0x38
        st.d  $a0, $sp, 0x40    // a0 to a3 are stored to restore them anytime
        st.d  $a1, $sp, 0x48
        st.d  $a2, $sp, 0x50
        st.d  $a3, $sp, 0x58
        st.d  $zero, $sp, 0x60  // $zero makes space for the local "is_not_handling_thread_abort"; last qword will store the thread obj

#define rsp_offset_is_not_handling_thread_abort 0x60
#define rsp_offset_a0 0x40
#define rsp_offset_a1 0x48
#define rsp_offset_a2 0x50
#define rsp_offset_a3 0x58
#define rsp_CatchFunclet_offset_thread 0x68

        //
        // clear the DoNotTriggerGc flag, trashes a4-a6
        //

        bl  C_FUNC(RhpGetThread)
        st.d  $a0, $sp, rsp_CatchFunclet_offset_thread
        ori  $a5, $a0, 0
        ld.d  $a0, $sp, 0x40
        ld.d  $a1, $sp, 0x48
        ld.d  $a2, $sp, 0x50
        ld.d  $a3, $sp, 0x58

        ld.d  $a4, $a5, OFFSETOF__Thread__m_threadAbortException
        sub.d  $a4, $a4, $a0
        st.d  $a4, $sp, rsp_offset_is_not_handling_thread_abort // Non-zero if the exception is not ThreadAbortException

        addi.d  $t3, $a5, OFFSETOF__Thread__m_ThreadStateFlags

ClearRetry_Catch: //TODO-LOONGARCH64: change ld/st to atomic instructions.
        ld.w  $a4, $t3, 0
        bstrins.w  $a4, $zero, 4, 4 // $a4 = $a4 & ~TSF_DoNotTriggerGc
        st.w  $a4, $t3, 0
        ori  $a6, $zero, 0
        beq  $a6, $zero, ClearSuccess_Catch
        b  ClearRetry_Catch
ClearSuccess_Catch:

        //
        // set preserved regs to the values expected by the funclet
        //
        RESTORE_PRESERVED_REGISTERS  $a2
        //
        // trash the values at the old homes to make sure nobody uses them
        //
        TRASH_PRESERVED_REGISTERS_STORAGE  $a2

        //
        // call the funclet
        //
        // a0 still contains the exception object
        jirl  $ra, $a1, 0

    ALTERNATE_ENTRY RhpCallCatchFunclet2

        // $a0 contains resume IP

        ld.d  $a2, $sp, rsp_offset_a2                     // a2 <- REGDISPLAY*

#ifdef _DEBUG
        // Call into some C++ code to validate the pop of the ExInfo.  We only do this in debug because we
        // have to spill all the preserved registers and then refill them after the call.

        st.d  $a0, $sp, rsp_offset_a0

        SAVE_PRESERVED_REGISTERS  $a2

        ld.d  $a0, $sp, rsp_CatchFunclet_offset_thread    // a0 <- Thread*
        ld.d  $a1, $sp, rsp_offset_a3                     // a1 <- current ExInfo*
        ld.d  $a2, $a2, OFFSETOF__REGDISPLAY__SP          // a2 <- resume SP value
        bl  C_FUNC(RhpValidateExInfoPop)

        ld.d  $a2, $sp, rsp_offset_a2                     // a2 <- REGDISPLAY*

        RESTORE_PRESERVED_REGISTERS  $a2

        ld.d  $a0, $sp, rsp_offset_a0                     // reload resume IP
#endif

        ld.d  $a1, $sp, rsp_CatchFunclet_offset_thread

        // We must unhijack the thread at this point because the section of stack where the hijack is applied
        // may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK  $a1, $a3, $t3                   // Thread in a1, trashes a3 and t3

        ld.d  $a3, $sp, rsp_offset_a3                           // a3 <- current ExInfo*
        ld.d  $a2, $a2, OFFSETOF__REGDISPLAY__SP                // a2 <- resume SP value

PopExInfoLoop:
        ld.d  $a3, $a3, OFFSETOF__ExInfo__m_pPrevExInfo         // a3 <- next ExInfo
        beq  $a3, $zero, DonePopping                            // if (pExInfo == null) { we're done }
        blt  $a3, $a2, PopExInfoLoop                            // if (pExInfo < resume SP} { keep going }

DonePopping:
        st.d  $a3, $a1, OFFSETOF__Thread__m_pExInfoStackHead    // store the new head on the Thread

        PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, $a3

        andi  $t7, $a3, TrapThreadsFlags_AbortInProgress_Bit
        beq  $t7, $zero, NoAbort

        ld.d  $a3, $sp, rsp_offset_is_not_handling_thread_abort
        bne  $a3, $zero, NoAbort

        // It was the ThreadAbortException, so rethrow it
        // reset SP
        ori  $a1, $a0, 0                                        // a1 <- continuation address as exception PC
        addi.w  $a0, $zero, STATUS_REDHAWK_THREAD_ABORT
        ori  $sp, $a2, 0
        b  C_FUNC(RhpThrowHwEx)

NoAbort:
        // reset SP and jump to continuation address
        ori  $sp, $a2, 0
        jirl  $r0, $a0, 0

#undef rsp_offset_is_not_handling_thread_abort
#undef rsp_offset_a0
#undef rsp_offset_a1
#undef rsp_offset_a2
#undef rsp_offset_a3
#undef rsp_CatchFunclet_offset_thread

    NESTED_END RhpCallCatchFunclet, _Text

//
// void FASTCALL RhpCallFinallyFunclet(void* pHandlerIP, REGDISPLAY* pRegDisplay)
//
// INPUT:  a0:  handler funclet address
//         a1:  REGDISPLAY*
//
// OUTPUT:
//

    NESTED_ENTRY RhpCallFinallyFunclet, _TEXT, NoHandler

        ALLOC_CALL_FUNCLET_FRAME 0x60 // Size needs to be equal with ExceptionHandling.asm variant of this function
        fst.d  $f24, $sp, 0x00
        fst.d  $f25, $sp, 0x08
        fst.d  $f26, $sp, 0x10
        fst.d  $f27, $sp, 0x18
        fst.d  $f28, $sp, 0x20
        fst.d  $f29, $sp, 0x28
        fst.d  $f30, $sp, 0x30
        fst.d  $f31, $sp, 0x38
        st.d  $a0, $sp, 0x40          // a0 and a1 are saved so we have them later
        st.d  $a1, $sp, 0x48

#define rsp_offset_a1 0x48
#define rsp_FinallyFunclet_offset_thread 0x50


        // We want to suppress hijacking between invocations of subsequent finallys.  We do this because we
        // cannot tolerate a GC after one finally has run (and possibly side-effected the GC state of the
        // method) and then been popped off the stack, leaving behind no trace of its effect.
        //
        // So we clear the state before and set it after invocation of the handler.
        //

        //
        // clear the DoNotTriggerGc flag, trashes a2-a4
        //

        bl  C_FUNC(RhpGetThread)
        st.d  $a0, $sp, rsp_FinallyFunclet_offset_thread
        ori  $a2, $a0, 0
        ld.d  $a0, $sp, 0x40
        ld.d  $a1, $sp, 0x48

        addi.d  $t3, $a2, OFFSETOF__Thread__m_ThreadStateFlags

ClearRetry: //TODO-LOONGARCH64: change ld/st to atomic instructions.
        ld.w  $a4, $t3, 0
        bstrins.w  $a4, $zero, 4, 4 // $a4 = $a4 & ~TSF_DoNotTriggerGc
        st.w  $a4, $t3, 0
        ori  $a3, $zero, 0
        beq  $a3, $zero, ClearSuccess
        b  ClearRetry
ClearSuccess:

        //
        // set preserved regs to the values expected by the funclet
        //
        RESTORE_PRESERVED_REGISTERS  $a1
        //
        // trash the values at the old homes to make sure nobody uses them
        //
        TRASH_PRESERVED_REGISTERS_STORAGE  $a1

        //
        // call the funclet
        //
        jirl  $ra, $a0, 0

    ALTERNATE_ENTRY RhpCallFinallyFunclet2

        ld.d  $a1, $sp, rsp_offset_a1 // reload REGDISPLAY pointer

        //
        // save new values of preserved regs into REGDISPLAY
        //
        SAVE_PRESERVED_REGISTERS  $a1

        //
        // set the DoNotTriggerGc flag, trashes a1-a3
        //

        ld.d  $a2, $sp, rsp_FinallyFunclet_offset_thread

        addi.d  $t3, $a2, OFFSETOF__Thread__m_ThreadStateFlags
SetRetry: //TODO-LOONGARCH64: change ld/st to atomic instructions.
        ld.w  $a1, $t3, 0
        ori  $a1, $a1, TSF_DoNotTriggerGc
        st.w  $a1, $t3, 0
        ori  $a3, $zero, 0
        beq  $a3, $zero, SetSuccess
        b  SetRetry
SetSuccess:

        fld.d  $f24, $sp, 0x00
        fld.d  $f25, $sp, 0x08
        fld.d  $f26, $sp, 0x10
        fld.d  $f27, $sp, 0x18
        fld.d  $f28, $sp, 0x20
        fld.d  $f29, $sp, 0x28
        fld.d  $f30, $sp, 0x30
        fld.d  $f31, $sp, 0x38

        FREE_CALL_FUNCLET_FRAME 0x60
        EPILOG_RETURN

#undef rsp_offset_a1
#undef rsp_FinallyFunclet_offset_thread

    NESTED_END RhpCallFinallyFunclet, _Text


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
        fst.d  $f24, $sp, 0x00
        fst.d  $f25, $sp, 0x08
        fst.d  $f26, $sp, 0x10
        fst.d  $f27, $sp, 0x18
        fst.d  $f28, $sp, 0x20
        fst.d  $f29, $sp, 0x28
        fst.d  $f30, $sp, 0x30
        fst.d  $f31, $sp, 0x38

        ld.d  $t3, $a2, OFFSETOF__REGDISPLAY__pFP
        ld.d  $fp, $t3, 0

        //
        // call the funclet
        //
        // $a0 still contains the exception object
        jirl  $ra, $a1, 0

    ALTERNATE_ENTRY RhpCallFilterFunclet2

        fld.d  $f24, $sp, 0x00
        fld.d  $f25, $sp, 0x08
        fld.d  $f26, $sp, 0x10
        fld.d  $f27, $sp, 0x18
        fld.d  $f28, $sp, 0x20
        fld.d  $f29, $sp, 0x28
        fld.d  $f30, $sp, 0x30
        fld.d  $f31, $sp, 0x38

        FREE_CALL_FUNCLET_FRAME 0x40
        EPILOG_RETURN

    NESTED_END RhpCallFilterFunclet, Text

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

#define rsp_offset_a0 0x10
#define rsp_offset_a1 0x18
#define rsp_offset_a2 0x20
#define rsp_offset_a3 0x28
#define rsp_offset_a4 0x30
#define rsp_CallPropagationCallback_offset_thread 0x38

        // Using the NO_FP macro so that the debugger unwinds using SP.
        // This makes backtraces work even after using RESTORE_PRESERVED_REGISTERS.
        PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED 22, 1, -0x40
        ori  $fp, $sp, 0
        st.d  $a0, $sp, rsp_offset_a0  // a0 to a3 are stored to restore them anytime
        st.d  $a1, $sp, rsp_offset_a1  // a0 to a3 are stored to restore them anytime
        st.d  $a2, $sp, rsp_offset_a2
        st.d  $a3, $sp, rsp_offset_a3
        st.d  $a4, $sp, rsp_offset_a4
        st.d  $zero, $sp, rsp_CallPropagationCallback_offset_thread // $zero makes space to store the thread obj

        //
        // clear the DoNotTriggerGc flag, trashes a4-a6
        //

        bl  C_FUNC(RhpGetThread)
        st.d  $a0, $sp, rsp_CallPropagationCallback_offset_thread
        ori  $a5, $a0, 0
        ld.d  $a0, $sp, rsp_offset_a0
        ld.d  $a1, $sp, rsp_offset_a1
        ld.d  $a2, $sp, rsp_offset_a2
        ld.d  $a3, $sp, rsp_offset_a3

        addi.d  $t3, $a5, OFFSETOF__Thread__m_ThreadStateFlags

ClearRetry_Propagate: //TODO-LOONGARCH64: change ld/st to atomic instructions.
        ld.w  $a4, $t3, 0
        bstrins.w  $a4, $zero, 4, 4 // $a4 = $a4 & ~TSF_DoNotTriggerGc
        st.w  $a4, $t3, 0
        ori  $a6, $zero, 0
        beq  $a6, $zero, ClearSuccess_Propagate
        b  ClearRetry_Propagate
ClearSuccess_Propagate:

        //
        // set preserved regs to the values expected by the funclet
        //
        RESTORE_PRESERVED_REGISTERS  $a2
        //
        // trash the values at the old homes to make sure nobody uses them
        //
        TRASH_PRESERVED_REGISTERS_STORAGE  $a2

#ifdef _DEBUG
        // Call into some C++ code to validate the pop of the ExInfo.  We only do this in debug because we
        // have to spill all the preserved registers and then refill them after the call.

        SAVE_PRESERVED_REGISTERS  $a2

        ld.d  $a0, $sp, rsp_CallPropagationCallback_offset_thread  // a0 <- Thread*
        ld.d  $a1, $sp, rsp_offset_a3                              // a1 <- current ExInfo*
        ld.d  $a2, $a2, OFFSETOF__REGDISPLAY__SP                   // a2 <- resume SP value
        bl  C_FUNC(RhpValidateExInfoPop)

        ld.d  $a2, $sp, rsp_offset_a2                              // a2 <- REGDISPLAY*

        RESTORE_PRESERVED_REGISTERS  $a2
#endif

        ld.d  $a1, $sp, rsp_CallPropagationCallback_offset_thread

        // We must unhijack the thread at this point because the section of stack where the hijack is applied
        // may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK  $a1, $a3, $t3            // Thread in a1, trashes a3 and t3

        ld.d  $a3, $sp, rsp_offset_a3                    // a3 <- current ExInfo*
        ld.d  $a2, $a2, OFFSETOF__REGDISPLAY__SP         // a2 <- resume SP value

Propagate_PopExInfoLoop:
        ld.d  $a3, $a3, OFFSETOF__ExInfo__m_pPrevExInfo  // a3 <- next ExInfo
        beq  $a3, $zero, Propagate_DonePopping           // if (pExInfo == null) { we're done }
        blt  $a3, $a2, Propagate_PopExInfoLoop           // if (pExInfo < resume SP} { keep going }

Propagate_DonePopping:
        st.d  $a3, $a1, OFFSETOF__Thread__m_pExInfoStackHead // store the new head on the Thread

        // restore preemptive mode
        ld.d  $a4, $sp, rsp_offset_a4                    // pPreviousTransitionFrame
        st.d  $a4, $a1, OFFSETOF__Thread__m_pTransitionFrame

        // reset SP and RA and jump to continuation address
        ld.d  $a0, $sp, rsp_offset_a0                    // callback context
        ld.d  $a1, $sp, rsp_offset_a1                    // callback
        ld.d  $a2, $sp, rsp_offset_a2                    // REGDISPLAY*
        ld.d  $a3, $a2, OFFSETOF__REGDISPLAY__pRA        // a3 <- &resume RA value
        ld.d  $ra, $a3
        ld.d  $a3, $a2, OFFSETOF__REGDISPLAY__SP         // a3 <- resume SP value
        ori  $sp, $a3, 0
        jirl  $r0, $a1, 0

#undef rsp_offset_a0
#undef rsp_offset_a1
#undef rsp_offset_a2
#undef rsp_offset_a3
#undef rsp_CallPropagationCallback_offset_thread

    NESTED_END RhpCallPropagateExceptionCallback, _Text

#endif // FEATURE_OBJCMARSHAL
