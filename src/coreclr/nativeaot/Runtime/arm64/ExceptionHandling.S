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

        mov x3, sp

        // Setup a PAL_LIMITED_CONTEXT on the stack {
        .if \exceptionType == HARDWARE_EXCEPTION
            sub sp,sp,#0x50
            .cfi_adjust_cfa_offset 0x50
            stp x3, x1, [sp]   // x3 is the SP and x1 is the IP of the fault site
        .else
            PROLOG_STACK_ALLOC 0x50
            .cfi_adjust_cfa_offset 0x50
            stp x3, lr, [sp]   // x3 is the SP and lr is the IP of the fault site
        .endif
        stp d8, d9, [sp, #0x10]
        stp d10, d11, [sp, #0x20]
        stp d12, d13, [sp, #0x30]
        stp d14, d15, [sp, #0x40]
        PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -0x70
        stp xzr, xzr, [sp, #0x10] // locations reserved for return value, not used for exception handling
        PROLOG_SAVE_REG_PAIR x19, x20, 0x20
        PROLOG_SAVE_REG_PAIR x21, x22, 0x30
        PROLOG_SAVE_REG_PAIR x23, x24, 0x40
        PROLOG_SAVE_REG_PAIR x25, x26, 0x50
        PROLOG_SAVE_REG_PAIR x27, x28, 0x60
        // } end PAL_LIMITED_CONTEXT

        PROLOG_STACK_ALLOC STACKSIZEOF_ExInfo
    .endm

// -----------------------------------------------------------------------------
// Macro used to create frame of funclet calling helpers (RhpCallXXXXFunclet)
// extraStackSize - extra stack space that the user of the macro can use to
//                   store additional registers
    .macro ALLOC_CALL_FUNCLET_FRAME extraStackSize

        // Using below prolog instead of PROLOG_SAVE_REG_PAIR fp,lr, #-60!
        // is intentional. Above statement would also emit instruction to save
        // sp in fp. If sp is saved in fp in prolog then it is not expected that fp can change in the body
        // of method. However, this method needs to be able to change fp before calling funclet.
        // This is required to access locals in funclet.
        PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED fp,lr, -0x60
        PROLOG_SAVE_REG_PAIR x19, x20, 0x10
        PROLOG_SAVE_REG_PAIR x21, x22, 0x20
        PROLOG_SAVE_REG_PAIR x23, x24, 0x30
        PROLOG_SAVE_REG_PAIR x25, x26, 0x40
        PROLOG_SAVE_REG_PAIR x27, x28, 0x50
        mov fp, sp
        .cfi_def_cfa_register fp

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

        EPILOG_RESTORE_REG_PAIR x19, x20, 0x10
        EPILOG_RESTORE_REG_PAIR x21, x22, 0x20
        EPILOG_RESTORE_REG_PAIR x23, x24, 0x30
        EPILOG_RESTORE_REG_PAIR x25, x26, 0x40
        EPILOG_RESTORE_REG_PAIR x27, x28, 0x50
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 0x60
   .endm


// -----------------------------------------------------------------------------
// Macro used to restore preserved general purpose and FP registers from REGDISPLAY
// regdisplayReg - register pointing to the REGDISPLAY structure
   .macro RESTORE_PRESERVED_REGISTERS regdisplayReg

        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX19]
        ldr         x19, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX20]
        ldr         x20, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX21]
        ldr         x21, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX22]
        ldr         x22, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX23]
        ldr         x23, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX24]
        ldr         x24, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX25]
        ldr         x25, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX26]
        ldr         x26, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX27]
        ldr         x27, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX28]
        ldr         x28, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pFP]
        ldr         fp,  [x12]
        //
        // load FP preserved regs
        //
        add         x12, \regdisplayReg, #OFFSETOF__REGDISPLAY__D
        ldp         d8, d9,   [x12, #0x00]
        ldp         d10, d11, [x12, #0x10]
        ldp         d12, d13, [x12, #0x20]
        ldp         d14, d15, [x12, #0x30]
    .endm

// -----------------------------------------------------------------------------
// Macro used to save preserved general purpose and FP registers to REGDISPLAY
// regdisplayReg - register pointing to the REGDISPLAY structure
   .macro SAVE_PRESERVED_REGISTERS regdisplayReg
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX19]
        str         x19, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX20]
        str         x20, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX21]
        str         x21, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX22]
        str         x22, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX23]
        str         x23, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX24]
        str         x24, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX25]
        str         x25, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX26]
        str         x26, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX27]
        str         x27, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX28]
        str         x28, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pFP]
        str         fp,  [x12]
        //
        // store vfp preserved regs
        //
        add         x12, \regdisplayReg, #OFFSETOF__REGDISPLAY__D
        stp         d8, d9,   [x12, #0x00]
        stp         d10, d11, [x12, #0x10]
        stp         d12, d13, [x12, #0x20]
        stp         d14, d15, [x12, #0x30]
    .endm


// -----------------------------------------------------------------------------
// Macro used to thrash preserved general purpose registers in REGDISPLAY
// to make sure nobody uses them
// regdisplayReg - register pointing to the REGDISPLAY structure
   .macro TRASH_PRESERVED_REGISTERS_STORAGE regdisplayReg

#if _DEBUG
        movz        x3, #0xbaad, LSL #48
        movk        x3, #0xdeed, LSL #32
        movk        x3, #0xbaad, LSL #16
        movk        x3, #0xdeed
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX19]
        str         x3, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX20]
        str         x3, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX21]
        str         x3, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX22]
        str         x3, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX23]
        str         x3, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX24]
        str         x3, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX25]
        str         x3, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX26]
        str         x3, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX27]
        str         x3, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pX28]
        str         x3, [x12]
        ldr         x12, [\regdisplayReg, #OFFSETOF__REGDISPLAY__pFP]
        str         x3, [x12]
#endif // _DEBUG
    .endm

.macro GetThreadX2
    stp x0, x1, [sp, -16]!
    bl C_FUNC(RhpGetThread)
    mov x2, x0
    ldp x0, x1, [sp], 16
.endm

#define rsp_offsetof_ExInfo  0
#define rsp_offsetof_Context STACKSIZEOF_ExInfo

//
// RhpThrowHwEx
//
// INPUT:  W0:  exception code of fault
//         X1:  faulting IP
//
// OUTPUT:
//
    NESTED_ENTRY RhpThrowHwEx, _TEXT, NoHandler

        ALLOC_THROW_FRAME HARDWARE_EXCEPTION

        GetThreadX2

        add         x1, sp, #rsp_offsetof_ExInfo                    // x1 <- ExInfo*
        str         xzr, [x1, #OFFSETOF__ExInfo__m_exception]       // pExInfo->m_exception = null
        mov         w3, #1
        strb        w3, [x1, #OFFSETOF__ExInfo__m_passNumber]       // pExInfo->m_passNumber = 1
        mov         w3, #0xFFFFFFFF
        str         w3, [x1, #OFFSETOF__ExInfo__m_idxCurClause]     // pExInfo->m_idxCurClause = MaxTryRegionIdx
        mov         w3, #2
        strb        w3, [x1, #OFFSETOF__ExInfo__m_kind]             // pExInfo->m_kind = ExKind.HardwareFault

        // link the ExInfo into the thread's ExInfo chain
        ldr         x3, [x2, #OFFSETOF__Thread__m_pExInfoStackHead]
        str         x3, [x1, #OFFSETOF__ExInfo__m_pPrevExInfo]      // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        str         x1, [x2, #OFFSETOF__Thread__m_pExInfoStackHead] // m_pExInfoStackHead = pExInfo

        // set the exception context field on the ExInfo
        add         x2, sp, #rsp_offsetof_Context                   // x2 <- PAL_LIMITED_CONTEXT*
        str         x2, [x1, #OFFSETOF__ExInfo__m_pExContext]       // pExInfo->m_pExContext = pContext

        // w0: exception code
        // x1: ExInfo*
        bl          C_FUNC(RhThrowHwEx)

    EXPORT_POINTER_TO_ADDRESS PointerToRhpThrowHwEx2

        // no return
        EMIT_BREAKPOINT

    NESTED_END RhpThrowHwEx, _TEXT

//
// RhpThrowEx
//
// INPUT:  X0:  exception object
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
        // where the tail-calling thread had saved LR, which may not match where we have saved LR.

        ldr         x1, [x2, #OFFSETOF__Thread__m_pvHijackedReturnAddress]
        cbz         x1, NotHijacked

        ldr         x3, [x2, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]

        // x0: exception object
        // x1: hijacked return address
        // x2: pThread
        // x3: hijacked return address location

        add         x12, sp, #(STACKSIZEOF_ExInfo + SIZEOF__PAL_LIMITED_CONTEXT)        // re-compute SP at callsite
        cmp         x3, x12             // if (m_ppvHijackedReturnAddressLocation < SP at callsite)
        blo         TailCallWasHijacked

        // normal case where a valid return address location is hijacked
        str         x1, [x3]
        b           ClearThreadState

TailCallWasHijacked:

        // Abnormal case where the return address location is now invalid because we ended up here via a tail
        // call.  In this case, our hijacked return address should be the correct caller of this method.

        // stick the previous return address in LR as well as in the right spots in our PAL_LIMITED_CONTEXT.
        mov         lr, x1
        str         lr, [sp, #(rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__LR)]
        str         lr, [sp, #(rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__IP)]

ClearThreadState:

        // clear the Thread's hijack state
        str         xzr, [x2, #OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation]
        str         xzr, [x2, #OFFSETOF__Thread__m_pvHijackedReturnAddress]

NotHijacked:

        add         x1, sp, #rsp_offsetof_ExInfo                    // x1 <- ExInfo*
        str         xzr, [x1, #OFFSETOF__ExInfo__m_exception]       // pExInfo->m_exception = null
        mov         w3, #1
        strb        w3, [x1, #OFFSETOF__ExInfo__m_passNumber]       // pExInfo->m_passNumber = 1
        mov         w3, #0xFFFFFFFF
        str         w3, [x1, #OFFSETOF__ExInfo__m_idxCurClause]     // pExInfo->m_idxCurClause = MaxTryRegionIdx
        mov         w3, #1
        strb        w3, [x1, #OFFSETOF__ExInfo__m_kind]             // pExInfo->m_kind = ExKind.Throw

        // link the ExInfo into the thread's ExInfo chain
        ldr         x3, [x2, #OFFSETOF__Thread__m_pExInfoStackHead]
        str         x3, [x1, #OFFSETOF__ExInfo__m_pPrevExInfo]      // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        str         x1, [x2, #OFFSETOF__Thread__m_pExInfoStackHead] // m_pExInfoStackHead = pExInfo

        // set the exception context field on the ExInfo
        add         x2, sp, #rsp_offsetof_Context                   // x2 <- PAL_LIMITED_CONTEXT*
        str         x2, [x1, #OFFSETOF__ExInfo__m_pExContext]       // pExInfo->m_pExContext = pContext

        // x0: exception object
        // x1: ExInfo*
        bl          C_FUNC(RhThrowEx)

    EXPORT_POINTER_TO_ADDRESS PointerToRhpThrowEx2

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

        add         x1, sp, #rsp_offsetof_ExInfo                    // x1 <- ExInfo*
        str         xzr, [x1, #OFFSETOF__ExInfo__m_exception]       // pExInfo->m_exception = null
        strb        wzr, [x1, #OFFSETOF__ExInfo__m_kind]            // init to a deterministic value (ExKind.None)
        mov         w3, #1
        strb        w3, [x1, #OFFSETOF__ExInfo__m_passNumber]       // pExInfo->m_passNumber = 1
        mov         w3, #0xFFFFFFFF
        str         w3, [x1, #OFFSETOF__ExInfo__m_idxCurClause]     // pExInfo->m_idxCurClause = MaxTryRegionIdx

        // link the ExInfo into the thread's ExInfo chain
        ldr         x3, [x2, #OFFSETOF__Thread__m_pExInfoStackHead]
        mov         x0, x3                                          // x0 <- current ExInfo
        str         x3, [x1, #OFFSETOF__ExInfo__m_pPrevExInfo]      // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        str         x1, [x2, #OFFSETOF__Thread__m_pExInfoStackHead] // m_pExInfoStackHead = pExInfo

        // set the exception context field on the ExInfo
        add         x2, sp, #rsp_offsetof_Context                   // x2 <- PAL_LIMITED_CONTEXT*
        str         x2, [x1, #OFFSETOF__ExInfo__m_pExContext]       // pExInfo->m_pExContext = pContext

        // x0 contains the currently active ExInfo
        // x1 contains the address of the new ExInfo
        bl          C_FUNC(RhRethrow)

    EXPORT_POINTER_TO_ADDRESS PointerToRhpRethrow2

        // no return
        EMIT_BREAKPOINT
    NESTED_END RhpRethrow, _TEXT

//
// void* FASTCALL RhpCallCatchFunclet(RtuObjectRef exceptionObj, void* pHandlerIP, REGDISPLAY* pRegDisplay,
//                                    ExInfo* pExInfo)
//
// INPUT:  X0:  exception object
//         X1:  handler funclet address
//         X2:  REGDISPLAY*
//         X3:  ExInfo*
//
// OUTPUT:
//

    NESTED_ENTRY RhpCallCatchFunclet, _TEXT, NoHandler

        ALLOC_CALL_FUNCLET_FRAME 0x70 // Size needs to be equal with ExceptionHandling.asm variant of this function
        stp d8, d9,   [sp, #0x00]
        stp d10, d11, [sp, #0x10]
        stp d12, d13, [sp, #0x20]
        stp d14, d15, [sp, #0x30]
        stp x0, x1,   [sp, #0x40] // x0 to x3 are stored to restore them anytime
        stp x2, x3,   [sp, #0x50]
        str xzr,      [sp, #0x60] // xzr makes space for the local "is_not_handling_thread_abort"; last qword will store the thread obj

#define rsp_offset_is_not_handling_thread_abort 0x60
#define rsp_offset_x0 0x40
#define rsp_offset_x1 0x48
#define rsp_offset_x2 0x50
#define rsp_offset_x3 0x58
#define rsp_CatchFunclet_offset_thread 0x68

        //
        // clear the DoNotTriggerGc flag, trashes x4-x6
        //

        bl C_FUNC(RhpGetThread)
        str x0, [sp, rsp_CatchFunclet_offset_thread]
        mov x5,x0
        ldp x0, x1,   [sp, #0x40]
        ldp x2, x3,   [sp, #0x50]

        ldr         x4, [x5, #OFFSETOF__Thread__m_threadAbortException]
        sub         x4, x4, x0
        str         x4, [sp, #rsp_offset_is_not_handling_thread_abort] // Non-zero if the exception is not ThreadAbortException

        add         x12, x5, #OFFSETOF__Thread__m_ThreadStateFlags

ClearRetry_Catch:
        ldxr        w4, [x12]
        bic         w4, w4, #TSF_DoNotTriggerGc
        stxr        w6, w4, [x12]
        cbz         w6, ClearSuccess_Catch
        b           ClearRetry_Catch
ClearSuccess_Catch:

        //
        // set preserved regs to the values expected by the funclet
        //
        RESTORE_PRESERVED_REGISTERS x2
        //
        // trash the values at the old homes to make sure nobody uses them
        //
        TRASH_PRESERVED_REGISTERS_STORAGE x2

        //
        // call the funclet
        //
        // x0 still contains the exception object
        blr         x1

        EXPORT_POINTER_TO_ADDRESS PointerToRhpCallCatchFunclet2

        // x0 contains resume IP

        ldr         x2, [sp, #rsp_offset_x2]                    // x2 <- REGDISPLAY*

#ifdef _DEBUG
        // Call into some C++ code to validate the pop of the ExInfo.  We only do this in debug because we
        // have to spill all the preserved registers and then refill them after the call.

        str         x0, [sp, rsp_offset_x0]

        SAVE_PRESERVED_REGISTERS x2

        ldr         x0, [sp, rsp_CatchFunclet_offset_thread]    // x0 <- Thread*
        ldr         x1, [sp, #rsp_offset_x3]                    // x1 <- current ExInfo*
        ldr         x2, [x2, #OFFSETOF__REGDISPLAY__SP]         // x2 <- resume SP value
        bl          C_FUNC(RhpValidateExInfoPop)

        ldr         x2, [sp, rsp_offset_x2]                             // x2 <- REGDISPLAY*

        RESTORE_PRESERVED_REGISTERS x2

        ldr         x0, [sp, rsp_offset_x0]                             // reload resume IP
#endif

        ldr         x1, [sp, rsp_CatchFunclet_offset_thread]

        ldr x1, [sp, rsp_CatchFunclet_offset_thread]

        // We must unhijack the thread at this point because the section of stack where the hijack is applied
        // may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK x1, x3, x12                      // Thread in x1, trashes x3 and x12

        ldr         x3, [sp, #rsp_offset_x3]                    // x3 <- current ExInfo*
        ldr         x2, [x2, #OFFSETOF__REGDISPLAY__SP]         // x2 <- resume SP value

PopExInfoLoop:
        ldr         x3, [x3, #OFFSETOF__ExInfo__m_pPrevExInfo]  // x3 <- next ExInfo
        cbz         x3, DonePopping                             // if (pExInfo == null) { we're done }
        cmp         x3, x2
        blt         PopExInfoLoop                               // if (pExInfo < resume SP} { keep going }

DonePopping:
        str         x3, [x1, #OFFSETOF__Thread__m_pExInfoStackHead]     // store the new head on the Thread

        PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, 3

        tbz         x3, #TrapThreadsFlags_AbortInProgress_Bit, NoAbort

        ldr         x3, [sp, #rsp_offset_is_not_handling_thread_abort]
        cbnz        x3, NoAbort

        // It was the ThreadAbortException, so rethrow it
        // reset SP
        mov         x1, x0                                     // x1 <- continuation address as exception PC
        mov         w0, #STATUS_REDHAWK_THREAD_ABORT
        mov         sp, x2
        b           C_FUNC(RhpThrowHwEx)

NoAbort:
        // reset SP and jump to continuation address
        mov         sp, x2
        br          x0

    NESTED_END RhpCallCatchFunclet, _Text

//
// void FASTCALL RhpCallFinallyFunclet(void* pHandlerIP, REGDISPLAY* pRegDisplay)
//
// INPUT:  X0:  handler funclet address
//         X1:  REGDISPLAY*
//
// OUTPUT:
//

    NESTED_ENTRY RhpCallFinallyFunclet, _TEXT, NoHandler

        ALLOC_CALL_FUNCLET_FRAME 0x60 // Size needs to be equal with ExceptionHandling.asm variant of this function
        stp d8, d9,   [sp, #0x00]
        stp d10, d11, [sp, #0x10]
        stp d12, d13, [sp, #0x20]
        stp d14, d15, [sp, #0x30]
        stp x0, x1,   [sp, #0x40]   // x0 and x1 are saved so we have them later

#define rsp_offset_x1 0x48
#define rsp_FinallyFunclet_offset_thread 0x50


        // We want to suppress hijacking between invocations of subsequent finallys.  We do this because we
        // cannot tolerate a GC after one finally has run (and possibly side-effected the GC state of the
        // method) and then been popped off the stack, leaving behind no trace of its effect.
        //
        // So we clear the state before and set it after invocation of the handler.
        //

        //
        // clear the DoNotTriggerGc flag, trashes x2-x4
        //

        bl C_FUNC(RhpGetThread)
        str x0, [sp, rsp_FinallyFunclet_offset_thread]
        mov x2, x0
        ldp x0, x1, [sp, #0x40]

        add         x12, x2, #OFFSETOF__Thread__m_ThreadStateFlags

ClearRetry:
        ldxr        w4, [x12]
        bic         w4, w4, #TSF_DoNotTriggerGc
        stxr        w3, w4, [x12]
        cbz         w3, ClearSuccess
        b           ClearRetry
ClearSuccess:

        //
        // set preserved regs to the values expected by the funclet
        //
        RESTORE_PRESERVED_REGISTERS x1
        //
        // trash the values at the old homes to make sure nobody uses them
        //
        TRASH_PRESERVED_REGISTERS_STORAGE x1

        //
        // call the funclet
        //
        blr         x0

    EXPORT_POINTER_TO_ADDRESS PointerToRhpCallFinallyFunclet2

        ldr         x1, [sp, #rsp_offset_x1]        // reload REGDISPLAY pointer

        //
        // save new values of preserved regs into REGDISPLAY
        //
        SAVE_PRESERVED_REGISTERS x1

        //
        // set the DoNotTriggerGc flag, trashes x1-x3
        //

        ldr x2, [sp, rsp_FinallyFunclet_offset_thread]

        add         x12, x2, #OFFSETOF__Thread__m_ThreadStateFlags
SetRetry:
        ldxr        w1, [x12]
        orr         w1, w1, #TSF_DoNotTriggerGc
        stxr        w3, w1, [x12]
        cbz         w3, SetSuccess
        b           SetRetry
SetSuccess:

        ldp         d8, d9,   [sp, #0x00]
        ldp         d10, d11, [sp, #0x10]
        ldp         d12, d13, [sp, #0x20]
        ldp         d14, d15, [sp, #0x30]

        FREE_CALL_FUNCLET_FRAME 0x60
        EPILOG_RETURN

    NESTED_END RhpCallFinallyFunclet, _Text


//
// void* FASTCALL RhpCallFilterFunclet(RtuObjectRef exceptionObj, void* pFilterIP, REGDISPLAY* pRegDisplay)
//
// INPUT:  X0:  exception object
//         X1:  filter funclet address
//         X2:  REGDISPLAY*
//
// OUTPUT:
//

    NESTED_ENTRY RhpCallFilterFunclet, _TEXT, NoHandler
        ALLOC_CALL_FUNCLET_FRAME 0x40
        stp d8, d9,   [sp, #0x00]
        stp d10, d11, [sp, #0x10]
        stp d12, d13, [sp, #0x20]
        stp d14, d15, [sp, #0x30]

        ldr         x12, [x2, #OFFSETOF__REGDISPLAY__pFP]
        ldr         fp, [x12]

        //
        // call the funclet
        //
        // x0 still contains the exception object
        blr         x1

    EXPORT_POINTER_TO_ADDRESS PointerToRhpCallFilterFunclet2

        ldp         d8, d9,   [sp, #0x00]
        ldp         d10, d11, [sp, #0x10]
        ldp         d12, d13, [sp, #0x20]
        ldp         d14, d15, [sp, #0x30]

        FREE_CALL_FUNCLET_FRAME 0x40
        EPILOG_RETURN

    NESTED_END RhpCallFilterFunclet, Text
