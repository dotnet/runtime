// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

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
        .cfi_adjust_cfa_offset  0x70
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
        PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED  fp, ra, 0x68
        PROLOG_SAVE_REG_PAIR  s1, s2,  0x10
        PROLOG_SAVE_REG_PAIR  s3, s4,  0x20
        PROLOG_SAVE_REG_PAIR  s5, s6,  0x30
        PROLOG_SAVE_REG_PAIR  s7, s8,  0x40
        PROLOG_SAVE_REG_PAIR  s9, s10, 0x50
        PROLOG_SAVE_REG       s11,     0x60
        mv  fp, sp
        .cfi_def_cfa_register fp

        .if \extraStackSize != 0
            PROLOG_STACK_ALLOC \extraStackSize
        .endif
    .endm

// Macro used to free frame of funclet calling helpers (RhpCallXXXXFunclet)
// extraStackSize - extra stack space that the user of the macro can use to
//                   store additional registers.
//                   It needs to match the value passed to the corresponding
//                   ALLOC_CALL_FUNCLET_FRAME.
.macro FREE_CALL_FUNCLET_FRAME extraStackSize

    .if \extraStackSize != 0
        EPILOG_STACK_FREE \extraStackSize
    .endif

    EPILOG_RESTORE_REG_PAIR  s1, s2,  0x10
    EPILOG_RESTORE_REG_PAIR  s3, s4,  0x20
    EPILOG_RESTORE_REG_PAIR  s5, s6,  0x30
    EPILOG_RESTORE_REG_PAIR  s7, s8,  0x40
    EPILOG_RESTORE_REG_PAIR  s9, s10, 0x50
    EPILOG_RESTORE_REG       s11,     0x60
    EPILOG_RESTORE_REG_PAIR_INDEXED  fp, ra, 0x68
.endm

// -----------------------------------------------------------------------------
// Macro used to restore preserved general purpose and FP registers from REGDISPLAY
// regdisplayReg - register pointing to the REGDISPLAY structure
   .macro RESTORE_PRESERVED_REGISTERS regdisplayReg

        // Load general-purpose registers that are defined
        ld  t3, OFFSETOF__REGDISPLAY__pS1(\regdisplayReg)  // Load address of pS1
        ld  s1, 0(t3)                                      // Load pS1 into s1
        ld  t3, OFFSETOF__REGDISPLAY__pS2(\regdisplayReg)  // Load address of pS2
        ld  s2, 0(t3)                                      // Load pS2 into s2
        ld  t3, OFFSETOF__REGDISPLAY__pS3(\regdisplayReg)  // Load address of pS3
        ld  s3, 0(t3)                                      // Load pS3 into s3
        ld  t3, OFFSETOF__REGDISPLAY__pS4(\regdisplayReg)  // Load address of pS4
        ld  s4, 0(t3)                                      // Load pS4 into s4
        ld  t3, OFFSETOF__REGDISPLAY__pS5(\regdisplayReg)  // Load address of pS5
        ld  s5, 0(t3)                                      // Load pS5 into s5
        ld  t3, OFFSETOF__REGDISPLAY__pS6(\regdisplayReg)  // Load address of pS6
        ld  s6, 0(t3)                                      // Load pS6 into s6
        ld  t3, OFFSETOF__REGDISPLAY__pS7(\regdisplayReg)  // Load address of pS7
        ld  s7, 0(t3)                                      // Load pS7 into s7
        ld  t3, OFFSETOF__REGDISPLAY__pS8(\regdisplayReg)  // Load address of pS8
        ld  s8, 0(t3)                                      // Load pS8 into s8
        ld  t3, OFFSETOF__REGDISPLAY__pS9(\regdisplayReg)  // Load address of pS9
        ld  s9, 0(t3)                                      // Load pS9 into s9
        ld  t3, OFFSETOF__REGDISPLAY__pS10(\regdisplayReg) // Load address of pS10
        ld  s10, 0(t3)                                     // Load pS10 into s10
        ld  t3, OFFSETOF__REGDISPLAY__pS11(\regdisplayReg) // Load address of pS11
        ld  s11, 0(t3)                                     // Load pS11 into s11
        ld  t3, OFFSETOF__REGDISPLAY__pFP(\regdisplayReg)  // Load address of pFP
        ld  fp, 0(t3)                                      // Load pFP into fp

        //
        // Load FP preserved registers
        //
        addi t3, \regdisplayReg, OFFSETOF__REGDISPLAY__F   // Base address of floating-point registers
        fld  fs0,  0x40(t3)                                // Load fs0
        fld  fs1,  0x48(t3)                                // Load fs1
        fld  fs2,  0x90(t3)                                // Load fs2
        fld  fs3,  0x98(t3)                                // Load fs3
        fld  fs4,  0xa0(t3)                                // Load fs4
        fld  fs5,  0xa8(t3)                                // Load fs5
        fld  fs6,  0xb0(t3)                                // Load fs6
        fld  fs7,  0xb8(t3)                                // Load fs7
        fld  fs8,  0xc0(t3)                                // Load fs8
        fld  fs9,  0xc8(t3)                                // Load fs9
        fld  fs10, 0xd0(t3)                                // Load fs10
        fld  fs11, 0xd8(t3)                                // Load fs11

    .endm

// -----------------------------------------------------------------------------
// Macro used to save preserved general purpose and FP registers to REGDISPLAY
// regdisplayReg - register pointing to the REGDISPLAY structure
.macro SAVE_PRESERVED_REGISTERS regdisplayReg

        // Save general purpose registers
        ld  t3, OFFSETOF__REGDISPLAY__pS1(\regdisplayReg)
        sd  s1, 0(t3)
        ld  t3, OFFSETOF__REGDISPLAY__pS2(\regdisplayReg)
        sd  s2, 0(t3)
        ld  t3, OFFSETOF__REGDISPLAY__pS3(\regdisplayReg)
        sd  s3, 0(t3)
        ld  t3, OFFSETOF__REGDISPLAY__pS4(\regdisplayReg)
        sd  s4, 0(t3)
        ld  t3, OFFSETOF__REGDISPLAY__pS5(\regdisplayReg)
        sd  s5, 0(t3)
        ld  t3, OFFSETOF__REGDISPLAY__pS6(\regdisplayReg)
        sd  s6, 0(t3)
        ld  t3, OFFSETOF__REGDISPLAY__pS7(\regdisplayReg)
        sd  s7, 0(t3)
        ld  t3, OFFSETOF__REGDISPLAY__pS8(\regdisplayReg)
        sd  s8, 0(t3)
        ld  t3, OFFSETOF__REGDISPLAY__pS9(\regdisplayReg)
        sd  s9, 0(t3)
        ld  t3, OFFSETOF__REGDISPLAY__pS10(\regdisplayReg)
        sd  s10, 0(t3)
        ld  t3, OFFSETOF__REGDISPLAY__pS11(\regdisplayReg)
        sd  s11, 0(t3)
        ld  t3, OFFSETOF__REGDISPLAY__pFP(\regdisplayReg)
        sd  fp, 0(t3)

        // Save floating-point registers
        addi  t3, \regdisplayReg, OFFSETOF__REGDISPLAY__F
        fsd  fs0,  0x40(t3)
        fsd  fs1,  0x48(t3)
        fsd  fs2,  0x90(t3)
        fsd  fs3,  0x98(t3)
        fsd  fs4,  0xa0(t3)
        fsd  fs5,  0xa8(t3)
        fsd  fs6,  0xb0(t3)
        fsd  fs7,  0xb8(t3)
        fsd  fs8,  0xc0(t3)
        fsd  fs9,  0xc8(t3)
        fsd  fs10, 0xd0(t3)
        fsd  fs11, 0xd8(t3)

.endm

// -----------------------------------------------------------------------------
// Macro used to thrash preserved general purpose registers in REGDISPLAY
// to make sure nobody uses them
// regdisplayReg - register pointing to the REGDISPLAY structure
.macro TRASH_PRESERVED_REGISTERS_STORAGE regdisplayReg

#if _DEBUG
        // Create a pattern to store
        li  a3, 0xbaaddeed
        mv  t0, a3
        slli  a3, a3, 32
        or  a3, a3, t0

        // Store the pattern into each register's location
        ld t3, OFFSETOF__REGDISPLAY__pS1(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pS2(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pS3(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pS4(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pS5(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pS6(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pS7(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pS8(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pS9(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pS10(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pS11(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pFP(\regdisplayReg)
        sd a3, 0(t3)
#endif // _DEBUG
.endm

.macro GetThreadA2
    addi  sp, sp, -16
    sd    a0, 0(sp)
    sd    a1, 8(sp)
    call  C_FUNC(RhpGetThread)
    mv    a2, a0
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

        // Compute address for ExInfo*
        addi   a1, sp, rsp_offsetof_ExInfo              // a1 <- ExInfo*
        sd     zero, OFFSETOF__ExInfo__m_exception(a1)    // pExInfo->m_exception = null
        li     a3, 1
        sb     a3, OFFSETOF__ExInfo__m_passNumber(a1)   // pExInfo->m_passNumber = 1
        addiw  a3, zero, -1
        sw     a3, OFFSETOF__ExInfo__m_idxCurClause(a1) // pExInfo->m_idxCurClause = MaxTryRegionIdx
        li     a3, 2
        sb     a3, OFFSETOF__ExInfo__m_kind(a1)         // pExInfo->m_kind = ExKind.HardwareFault

        // Link the ExInfo into the thread's ExInfo chain
        ld     a3, OFFSETOF__Thread__m_pExInfoStackHead(a2)
        sd     a3, OFFSETOF__ExInfo__m_pPrevExInfo(a1)       // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        sd     a1, OFFSETOF__Thread__m_pExInfoStackHead(a2)  // m_pExInfoStackHead = pExInfo

        // Set the exception context field on the ExInfo
        addi   a2, sp, rsp_offsetof_Context                  // a2 <- PAL_LIMITED_CONTEXT*
        sd     a2, OFFSETOF__ExInfo__m_pExContext(a1)        // pExInfo->m_pExContext = pContext

        // a0[31:0]: exception code
        // a1: ExInfo*
        call  C_FUNC(RhThrowHwEx)

    ALTERNATE_ENTRY RhpThrowHwEx2

        // No return
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

        ld   a1, OFFSETOF__Thread__m_pvHijackedReturnAddress(a2)
        beq  a1, zero, LOCAL_LABEL(NotHijacked)

        ld   a3, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation(a2)

        // Recompute SP at callsite
        addi  t3, sp, (STACKSIZEOF_ExInfo + SIZEOF__PAL_LIMITED_CONTEXT)
        bltu  a3, t3, LOCAL_LABEL(TailCallWasHijacked) // if (m_ppvHijackedReturnAddressLocation < SP at callsite)

        // Normal case where a valid return address location is hijacked
        sd    a1, 0(a3)
        tail  LOCAL_LABEL(ClearThreadState)

LOCAL_LABEL(TailCallWasHijacked):

        // Abnormal case where the return address location is now invalid because we ended up here via a tail
        // call. In this case, our hijacked return address should be the correct caller of this method.

        // Stick the previous return address in RA as well as in the right spots in our PAL_LIMITED_CONTEXT.
        mv  ra, a1

        // Compute offsets for PAL_LIMITED_CONTEXT
        sd  ra, (rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__RA)(sp)
        sd  ra, (rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__IP)(sp)

LOCAL_LABEL(ClearThreadState):

        // Clear the Thread's hijack state
        sd  zero, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation(a2)
        sd  zero, OFFSETOF__Thread__m_pvHijackedReturnAddress(a2)

LOCAL_LABEL(NotHijacked):

        // Compute the offset for ExInfo
        addi   a1, sp, rsp_offsetof_ExInfo              // a1 <- ExInfo*
        sd     zero, OFFSETOF__ExInfo__m_exception(a1)    // pExInfo->m_exception = null
        li     a3, 1
        sb     a3, OFFSETOF__ExInfo__m_passNumber(a1)   // pExInfo->m_passNumber = 1
        addiw  a3, zero, -1
        sw     a3, OFFSETOF__ExInfo__m_idxCurClause(a1) // pExInfo->m_idxCurClause = MaxTryRegionIdx
        li     a3, 1
        sb     a3, OFFSETOF__ExInfo__m_kind(a1)         // pExInfo->m_kind = ExKind.Throw

        // Link the ExInfo into the thread's ExInfo chain
        ld  a3, OFFSETOF__Thread__m_pExInfoStackHead(a2)
        sd  a3, OFFSETOF__ExInfo__m_pPrevExInfo(a1)      // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        sd  a1, OFFSETOF__Thread__m_pExInfoStackHead(a2) // m_pExInfoStackHead = pExInfo

        // Set the exception context field on the ExInfo
        addi  a2, sp, rsp_offsetof_Context           // a2 <- PAL_LIMITED_CONTEXT*
        sd    a2, OFFSETOF__ExInfo__m_pExContext(a1) // pExInfo->m_pExContext = pContext

        // a0: exception object
        // a1: ExInfo*
        call C_FUNC(RhThrowEx)

    ALTERNATE_ENTRY RhpThrowEx2

        // No return
        EMIT_BREAKPOINT

    NESTED_END RhpThrowEx, _TEXT

//
// void FASTCALL RhpRethrow()
//
// SUMMARY: Similar to RhpThrowEx, except that it passes along the currently active ExInfo
//
// OUTPUT:
//

    NESTED_ENTRY RhpRethrow, _TEXT, NoHandler

        ALLOC_THROW_FRAME SOFTWARE_EXCEPTION

        GetThreadA2

        // a1 <- ExInfo*
        addi  a1, sp, rsp_offsetof_ExInfo
        // pExInfo->m_exception = null
        sd  zero, OFFSETOF__ExInfo__m_exception(a1)
        // init to a deterministic value (ExKind.None)
        sb  zero, OFFSETOF__ExInfo__m_kind(a1)
        // pExInfo->m_passNumber = 1
        li  a3, 1
        sb  a3, OFFSETOF__ExInfo__m_passNumber(a1)
        // pExInfo->m_idxCurClause = MaxTryRegionIdx
        addiw  a3, zero, -1
        sw     a3, OFFSETOF__ExInfo__m_idxCurClause(a1)

        // link the ExInfo into the thread's ExInfo chain
        ld  a3, OFFSETOF__Thread__m_pExInfoStackHead(a2)
        // a0 <- current ExInfo
        mv a0, a3
        // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        sd a3, OFFSETOF__ExInfo__m_pPrevExInfo(a1)
        // m_pExInfoStackHead = pExInfo
        sd a1, OFFSETOF__Thread__m_pExInfoStackHead(a2)

        // set the exception context field on the ExInfo
        // a2 <- PAL_LIMITED_CONTEXT*
        addi  a2, sp, rsp_offsetof_Context
        // pExInfo->m_pExContext = pContext
        sd  a2, OFFSETOF__ExInfo__m_pExContext(a1)

        // a0 contains the currently active ExInfo
        // a1 contains the address of the new ExInfo
        call  C_FUNC(RhRethrow)

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

        // Allocate space for the call funclet frame
        ALLOC_CALL_FUNCLET_FRAME 0x90

        // Save floating-point registers
        fsd  fs0,  0x00(sp)
        fsd  fs1,  0x08(sp)
        fsd  fs2,  0x10(sp)
        fsd  fs3,  0x18(sp)
        fsd  fs4,  0x20(sp)
        fsd  fs5,  0x28(sp)
        fsd  fs6,  0x30(sp)
        fsd  fs7,  0x38(sp)
        fsd  fs8,  0x40(sp)
        fsd  fs9,  0x48(sp)
        fsd  fs10, 0x50(sp)
        fsd  fs11, 0x58(sp)

        // Save integer registers
        sd  a0, 0x60(sp)    // Save a0 to a3
        sd  a1, 0x68(sp)
        sd  a2, 0x70(sp)
        sd  a3, 0x78(sp)
        sd  zero, 0x80(sp) // Make space for local "is_not_handling_thread_abort"; last qword will store the thread obj

#define rsp_offset_is_not_handling_thread_abort 0x80
#define rsp_offset_a0 0x60
#define rsp_offset_a1 0x68
#define rsp_offset_a2 0x70
#define rsp_offset_a3 0x78
#define rsp_CatchFunclet_offset_thread 0x88

        // Clear the DoNotTriggerGc flag, trashes a4-a6
        call  C_FUNC(RhpGetThread)  // Call the RhpGetThread function
        sd  a0, rsp_CatchFunclet_offset_thread(sp)
        mv  a5, a0
        ld  a0, rsp_offset_a0(sp)
        ld  a1, rsp_offset_a1(sp)
        ld  a2, rsp_offset_a2(sp)
        ld  a3, rsp_offset_a3(sp)

        ld  a4, OFFSETOF__Thread__m_threadAbortException(a5)
        sub a4, a4, a0
        sd  a4, rsp_offset_is_not_handling_thread_abort(sp) // Non-zero if the exception is not ThreadAbortException

        addi  t3, a5, OFFSETOF__Thread__m_ThreadStateFlags

        addiw  a6, zero, -17  // Mask value (0xFFFFFFEF)
        amoand.w  a4, a6, (t3)

        // Set preserved regs to the values expected by the funclet
        RESTORE_PRESERVED_REGISTERS a2

        // Trash the values at the old homes to make sure nobody uses them
        TRASH_PRESERVED_REGISTERS_STORAGE a2

        // Call the funclet
        // a0 still contains the exception object
        jalr  a1 // Jump to the handler funclet

    ALTERNATE_ENTRY RhpCallCatchFunclet2

        // a0 contains resume IP

        ld  a2, rsp_offset_a2(sp) // a2 <- REGDISPLAY*

#ifdef _DEBUG
        // Call into some C++ code to validate the pop of the ExInfo. We only do this in debug because we
        // have to spill all the preserved registers and then refill them after the call.

        sd  a0, rsp_offset_a0(sp)

        SAVE_PRESERVED_REGISTERS a2

        ld  a0, rsp_CatchFunclet_offset_thread(sp) // a0 <- Thread*
        ld  a1, rsp_offset_a3(sp)                  // a1 <- current ExInfo*
        ld  a2, OFFSETOF__REGDISPLAY__SP(a2)       // a2 <- resume SP value
        call  C_FUNC(RhpValidateExInfoPop)

        ld  a2, rsp_offset_a2(sp)                  // a2 <- REGDISPLAY*

        RESTORE_PRESERVED_REGISTERS a2

        ld  a0, rsp_offset_a0(sp)                  // Reload resume IP
#endif

        ld  a1, rsp_CatchFunclet_offset_thread(sp)

        // We must unhijack the thread at this point because the section of stack where the hijack is applied
        // may go dead. If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK a1, a3, t3  // Thread in a1, trashes a3 and t3

        ld  a3, rsp_offset_a3(sp)                         // a3 <- current ExInfo*
        ld  a2, OFFSETOF__REGDISPLAY__SP(a2)              // a2 <- resume SP value

LOCAL_LABEL(PopExInfoLoop):
        ld   a3, OFFSETOF__ExInfo__m_pPrevExInfo(a3)      // a3 <- next ExInfo
        beq  a3, zero, LOCAL_LABEL(DonePopping)           // if (pExInfo == null) { we're done }
        blt  a3, a2, LOCAL_LABEL(PopExInfoLoop)           // if (pExInfo < resume SP} { keep going }

LOCAL_LABEL(DonePopping):
        sd  a3, OFFSETOF__Thread__m_pExInfoStackHead(a1)  // Store the new head on the Thread

        PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, a3

        // Use `andi` with the immediate value 0
        andi  a6, a3, (1 << TrapThreadsFlags_AbortInProgress_Bit)  // Apply the mask directly
        beq   a6, zero, LOCAL_LABEL(NoAbort)

        ld   a3, rsp_offset_is_not_handling_thread_abort(sp)
        bne  a3, zero, LOCAL_LABEL(NoAbort)

        // It was the ThreadAbortException, so rethrow it
        // Reset SP
        mv  a1, a0  // a1 <- continuation address as exception PC
        li  a0, STATUS_REDHAWK_THREAD_ABORT
        mv  sp, a2
        tail  C_FUNC(RhpThrowHwEx)

LOCAL_LABEL(NoAbort):
        // Reset SP and jump to continuation address
        mv  sp, a2
        jalr  zero, 0(a0)  // Jump to the continuation address

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

        // Allocate space for the call funclet frame
        ALLOC_CALL_FUNCLET_FRAME 0x80

        // Save floating-point registers
        fsd  fs0,  0x00(sp)
        fsd  fs1,  0x08(sp)
        fsd  fs2,  0x10(sp)
        fsd  fs3,  0x18(sp)
        fsd  fs4,  0x20(sp)
        fsd  fs5,  0x28(sp)
        fsd  fs6,  0x30(sp)
        fsd  fs7,  0x38(sp)
        fsd  fs8,  0x40(sp)
        fsd  fs9,  0x48(sp)
        fsd  fs10, 0x50(sp)
        fsd  fs11, 0x58(sp)

        // Save integer registers
        sd a0, 0x60(sp)  // Save a0 to 0x60
        sd a1, 0x68(sp)  // Save a1 to 0x68

#define rsp_offset_a1 0x68
#define rsp_FinallyFunclet_offset_thread 0x70

        // Clear the DoNotTriggerGc flag
        call  C_FUNC(RhpGetThread)
        sd  a0, rsp_FinallyFunclet_offset_thread(sp)
        mv  a2, a0
        ld  a0, 0x60(sp)
        ld  a1, 0x68(sp)

        // Set the DoNotTriggerGc flag
        addi  t3, a2, OFFSETOF__Thread__m_ThreadStateFlags
        addiw  a3, zero, -17  // Mask value (0xFFFFFFEF)
        amoand.w  a4, a3, (t3)

        // Restore preserved registers
        RESTORE_PRESERVED_REGISTERS a1

        // Trash the values at the old homes to make sure nobody uses them
        TRASH_PRESERVED_REGISTERS_STORAGE a1

        // Call the funclet
        jalr  a0  // Jump to the funclet

    ALTERNATE_ENTRY RhpCallFinallyFunclet2

        ld  a1, rsp_offset_a1(sp)  // Reload REGDISPLAY pointer

        // Save new values of preserved registers into REGDISPLAY
        SAVE_PRESERVED_REGISTERS a1

        // Restore the DoNotTriggerGc flag
        ld  a2, rsp_FinallyFunclet_offset_thread(sp)

        addi  t3, a2, OFFSETOF__Thread__m_ThreadStateFlags
        addiw  a3, zero, 16  // Mask value (0x10)
        amoor.w  a1, a3, (t3)

        // Restore floating-point registers
        fld  fs0,  0x00(sp)
        fld  fs1,  0x08(sp)
        fld  fs2,  0x10(sp)
        fld  fs3,  0x18(sp)
        fld  fs4,  0x20(sp)
        fld  fs5,  0x28(sp)
        fld  fs6,  0x30(sp)
        fld  fs7,  0x38(sp)
        fld  fs8,  0x40(sp)
        fld  fs9,  0x48(sp)
        fld  fs10, 0x50(sp)
        fld  fs11, 0x58(sp)

        // Free call funclet frame
        FREE_CALL_FUNCLET_FRAME 0x80

        // Return
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
        ALLOC_CALL_FUNCLET_FRAME 0x60
        fsd  fs0,  0x00(sp)
        fsd  fs1,  0x08(sp)
        fsd  fs2,  0x10(sp)
        fsd  fs3,  0x18(sp)
        fsd  fs4,  0x20(sp)
        fsd  fs5,  0x28(sp)
        fsd  fs6,  0x30(sp)
        fsd  fs7,  0x38(sp)
        fsd  fs8,  0x40(sp)
        fsd  fs9,  0x48(sp)
        fsd  fs10, 0x50(sp)
        fsd  fs11, 0x58(sp)

        ld  t3, OFFSETOF__REGDISPLAY__pFP(a2)
        ld  fp, 0(t3)

        //
        // call the funclet
        //
        // a0 still contains the exception object
        jalr  a1

    ALTERNATE_ENTRY RhpCallFilterFunclet2

        fld  fs0,  0x00(sp)
        fld  fs1,  0x08(sp)
        fld  fs2,  0x10(sp)
        fld  fs3,  0x18(sp)
        fld  fs4,  0x20(sp)
        fld  fs5,  0x28(sp)
        fld  fs6,  0x30(sp)
        fld  fs7,  0x38(sp)
        fld  fs8,  0x40(sp)
        fld  fs9,  0x48(sp)
        fld  fs10, 0x50(sp)
        fld  fs11, 0x58(sp)

        FREE_CALL_FUNCLET_FRAME 0x60
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
        PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED fp, ra, 0x40
        mv    fp, sp
        sd    a0, rsp_offset_a0(sp)  // a0 to a4 are stored to restore them anytime
        sd    a1, rsp_offset_a1(sp)
        sd    a2, rsp_offset_a2(sp)
        sd    a3, rsp_offset_a3(sp)
        sd    a4, rsp_offset_a4(sp)
        sd    zero, rsp_CallPropagationCallback_offset_thread(sp) // zero makes space to store the thread obj

        // clear the DoNotTriggerGc flag, trashes a4-a6
        call  C_FUNC(RhpGetThread)
        sd    a0, rsp_CallPropagationCallback_offset_thread(sp)
        mv    a5, a0
        ld    a0, rsp_offset_a0(sp)
        ld    a1, rsp_offset_a1(sp)
        ld    a2, rsp_offset_a2(sp)
        ld    a3, rsp_offset_a3(sp)

        addi  t3, a5, OFFSETOF__Thread__m_ThreadStateFlags

        addiw  a6, zero, -17  // Mask value (0xFFFFFFEF)
        amoand.w  a4, t3, a6

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
        ld    a2, OFFSETOF__REGDISPLAY__SP(a2)                   // a2 <- resume SP value
        call  C_FUNC(RhpValidateExInfoPop)

        ld    a2, rsp_offset_a2(sp)                              // a2 <- REGDISPLAY*

        RESTORE_PRESERVED_REGISTERS a2
#endif

        ld    a1, rsp_CallPropagationCallback_offset_thread(sp)

        // We must unhijack the thread at this point because the section of stack where the hijack is applied
        // may go dead.  If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK a1, a3, t3              // Thread in a1, trashes a3 and t3

        ld    a3, rsp_offset_a3(sp)                    // a3 <- current ExInfo*
        ld    a2, OFFSETOF__REGDISPLAY__SP(a2)         // a2 <- resume SP value

LOCAL_LABEL(Propagate_PopExInfoLoop):
        ld    a3, OFFSETOF__ExInfo__m_pPrevExInfo(a3)       // a3 <- next ExInfo
        beqz  a3, LOCAL_LABEL(Propagate_DonePopping)        // if (pExInfo == null) { we're done }
        blt   a3, a2, LOCAL_LABEL(Propagate_PopExInfoLoop)  // if (pExInfo < resume SP) { keep going }

LOCAL_LABEL(Propagate_DonePopping):
        sd    a3, OFFSETOF__Thread__m_pExInfoStackHead(a1)  // store the new head on the Thread

        // restore preemptive mode
        ld    a4, rsp_offset_a4(sp)                         // pPreviousTransitionFrame
        sd    a4, OFFSETOF__Thread__m_pTransitionFrame(a1)

        // reset SP and RA and jump to continuation address
        ld    a0, rsp_offset_a0(sp)                    // callback context
        ld    a1, rsp_offset_a1(sp)                    // callback
        ld    a2, rsp_offset_a2(sp)                    // REGDISPLAY*
        ld    a3, OFFSETOF__REGDISPLAY__pRA(a2)        // a3 <- &resume RA value
        ld    ra, 0(a3)
        ld    a3, OFFSETOF__REGDISPLAY__SP(a2)         // a3 <- resume SP value
        mv    sp, a3
        jalr  zero, 0(a1)

#undef rsp_offset_a0
#undef rsp_offset_a1
#undef rsp_offset_a2
#undef rsp_offset_a3
#undef rsp_CallPropagationCallback_offset_thread

    NESTED_END RhpCallPropagateExceptionCallback, _TEXT

#endif // FEATURE_OBJCMARSHAL
