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

    mv  a3, sp

    // Setup a PAL_LIMITED_CONTEXT on the stack {
    .if \exceptionType == HARDWARE_EXCEPTION
        addi  sp, sp, -0x50
        .cfi_adjust_cfa_offset  0x50
        sd  a3, 0(sp)          // a3 is the SP and a1 is the IP of the fault site
        sd  a1, 8(sp)
    .else
        PROLOG_STACK_ALLOC  0x50
        .cfi_adjust_cfa_offset  0x50
        sd  a3, 0(sp)          // a3 is the SP and ra is the IP of the fault site
        sd  ra, 8(sp)
    .endif

    // Safely using available registers for floating-point saves
    fsd  ft0, 0x10(sp)
    fsd  ft1, 0x18(sp)
    fsd  ft2, 0x20(sp)
    fsd  ft3, 0x28(sp)
    fsd  ft4, 0x30(sp)
    fsd  ft5, 0x38(sp)
    fsd  ft6, 0x40(sp)
    fsd  ft7, 0x48(sp)

    // Adjust the registers used in the following line
    PROLOG_SAVE_REG_PAIR_INDEXED  s10, s11, 0x70

    sd  zero, 0x10(sp)        // locations reserved for return value, not used for exception handling
    sd  zero, 0x18(sp)
    PROLOG_SAVE_REG_PAIR  s0, s1, 0x20
    PROLOG_SAVE_REG_PAIR  s2, s3, 0x30
    PROLOG_SAVE_REG_PAIR  s4, s5, 0x40
    PROLOG_SAVE_REG_PAIR  s6, s7, 0x50
    PROLOG_SAVE_REG_PAIR  s8, s9, 0x60
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
        PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED x22, x1, 96
        PROLOG_SAVE_REG_PAIR x23, x24, 16
        PROLOG_SAVE_REG_PAIR x25, x26, 32
        PROLOG_SAVE_REG_PAIR x27, x28, 48
        PROLOG_SAVE_REG_PAIR x29, x30, 64
        PROLOG_SAVE_REG_PAIR x31, x2, 80
        addi  fp, sp, 0
        .cfi_def_cfa_register 22 //fp

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

    EPILOG_RESTORE_REG_PAIR t0, t1, 16
    EPILOG_RESTORE_REG_PAIR t2, t3, 32
    EPILOG_RESTORE_REG_PAIR t4, t5, 48
    EPILOG_RESTORE_REG_PAIR t6, s0, 64
    EPILOG_RESTORE_REG_PAIR s1, s2, 80
    EPILOG_RESTORE_REG_PAIR_INDEXED s3, ra, 96
.endm

// -----------------------------------------------------------------------------
// Macro used to restore preserved general purpose and FP registers from REGDISPLAY
// regdisplayReg - register pointing to the REGDISPLAY structure
   .macro RESTORE_PRESERVED_REGISTERS regdisplayReg

        // Load general-purpose registers that are defined
        ld t3, OFFSETOF__REGDISPLAY__pR4(\regdisplayReg)   // Load address of pR4
        ld s0, 0(t3)                                       // Load pR4 into s0
        ld t3, OFFSETOF__REGDISPLAY__pR5(\regdisplayReg)   // Load address of pR5
        ld s1, 0(t3)                                       // Load pR5 into s1
        ld t3, OFFSETOF__REGDISPLAY__pR6(\regdisplayReg)   // Load address of pR6
        ld s2, 0(t3)                                       // Load pR6 into s2
        ld t3, OFFSETOF__REGDISPLAY__pR7(\regdisplayReg)   // Load address of pR7
        ld s3, 0(t3)                                       // Load pR7 into s3
        ld t3, OFFSETOF__REGDISPLAY__pR8(\regdisplayReg)   // Load address of pR8
        ld s4, 0(t3)                                       // Load pR8 into s4
        ld t3, OFFSETOF__REGDISPLAY__pR9(\regdisplayReg)   // Load address of pR9
        ld s5, 0(t3)                                       // Load pR9 into s5
        ld t3, OFFSETOF__REGDISPLAY__pR10(\regdisplayReg)  // Load address of pR10
        ld s6, 0(t3)                                       // Load pR10 into s6
        ld t3, OFFSETOF__REGDISPLAY__pR11(\regdisplayReg)  // Load address of pR11
        ld s7, 0(t3)                                       // Load pR11 into s7
        ld t3, OFFSETOF__REGDISPLAY__pR12(\regdisplayReg)  // Load address of pR12
        ld s8, 0(t3)                                       // Load pR12 into s8
        ld t3, OFFSETOF__REGDISPLAY__pFP(\regdisplayReg)   // Load address of pFP
        ld fp, 0(t3)                                       // Load pFP into fp

        //
        // Load FP preserved registers
        //
        addi t3, \regdisplayReg, OFFSETOF__REGDISPLAY__F   // Base address of floating-point registers
        fld f24, 0(t3)                                      // Load f24
        fld f25, 8(t3)                                      // Load f25
        fld f26, 16(t3)                                     // Load f26
        fld f27, 24(t3)                                     // Load f27
        fld f28, 32(t3)                                     // Load f28
        fld f29, 40(t3)                                     // Load f29
        fld f30, 48(t3)                                     // Load f30
        fld f31, 56(t3)                                     // Load f31

    .endm

// -----------------------------------------------------------------------------
// Macro used to save preserved general purpose and FP registers to REGDISPLAY
// regdisplayReg - register pointing to the REGDISPLAY structure
.macro SAVE_PRESERVED_REGISTERS regdisplayReg

        // Save general purpose registers
        ld t3, OFFSETOF__REGDISPLAY__pR4(\regdisplayReg)
        sd s0, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR5(\regdisplayReg)
        sd s1, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR6(\regdisplayReg)
        sd s2, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR7(\regdisplayReg)
        sd s3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR8(\regdisplayReg)
        sd s4, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR9(\regdisplayReg)
        sd s5, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR10(\regdisplayReg)
        sd s6, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR11(\regdisplayReg)
        sd s7, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR12(\regdisplayReg)
        sd s8, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pFP(\regdisplayReg)
        sd fp, 0(t3)

        // Save floating-point registers
        addi t3, \regdisplayReg, OFFSETOF__REGDISPLAY__F
        fsd f24, 0(t3)
        fsd f25, 8(t3)
        fsd f26, 16(t3)
        fsd f27, 24(t3)
        fsd f28, 32(t3)
        fsd f29, 40(t3)
        fsd f30, 48(t3)
        fsd f31, 56(t3)

.endm

// -----------------------------------------------------------------------------
// Macro used to thrash preserved general purpose registers in REGDISPLAY
// to make sure nobody uses them
// regdisplayReg - register pointing to the REGDISPLAY structure
.macro TRASH_PRESERVED_REGISTERS_STORAGE regdisplayReg

#if _DEBUG
        // Create a pattern to store
        li a3, 0xbaadd          // Load immediate lower 16 bits
        slli a3, a3, 16         // Shift left by 16 bits
        li t0, 0xeed           // Load immediate lower bits
        or a3, a3, t0          // Combine both parts into a3
        li t1, 0xddeed         // Load 32-bit value
        sd a3, 0(t1)           // Store double (64-bit) into a3
        li t2, 0xbaa           // Load lower part
        slli t2, t2, 16        // Shift left by 16 bits
        or a3, a3, t2          // Combine into a3

        // Store the pattern into each register's location
        ld t3, OFFSETOF__REGDISPLAY__pR4(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR5(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR6(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR7(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR8(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR9(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR10(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR11(\regdisplayReg)
        sd a3, 0(t3)
        ld t3, OFFSETOF__REGDISPLAY__pR12(\regdisplayReg)
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
    addi  a2, a0, 0
    ld    a0, 0(sp)
    ld    a1, 8(sp)
    addi  sp, sp, 16
.endm

#define rsp_offsetof_ExInfo  0
#define rsp_offsetof_Context STACKSIZEOF_ExInfo

.macro ADD_LARGE_IMM reg, base, imm
    // Handle cases where the immediate is within the 12-bit range
    lui t0, %hi(\imm)           // Load upper 20 bits of the immediate
    addi t0, t0, %lo(\imm)      // Add lower 12 bits of the immediate
    add \reg, \base, t0         // Add the result to the base register
.endm

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
        ADD_LARGE_IMM a1, sp, rsp_offsetof_ExInfo  // a1 <- ExInfo*
        sd    zero, OFFSETOF__ExInfo__m_exception(a1)  // pExInfo->m_exception = null
        li    a3, 1
        sb    a3, OFFSETOF__ExInfo__m_passNumber(a1)   // pExInfo->m_passNumber = 1
        li    a3, -1
        sw    a3, OFFSETOF__ExInfo__m_idxCurClause(a1) // pExInfo->m_idxCurClause = MaxTryRegionIdx
        li    a3, 2
        sb    a3, OFFSETOF__ExInfo__m_kind(a1)         // pExInfo->m_kind = ExKind.HardwareFault

        // Link the ExInfo into the thread's ExInfo chain
        ld    a3, OFFSETOF__Thread__m_pExInfoStackHead(a2)
        sd    a3, OFFSETOF__ExInfo__m_pPrevExInfo(a1)       // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        sd    a1, OFFSETOF__Thread__m_pExInfoStackHead(a2)  // m_pExInfoStackHead = pExInfo

        // Set the exception context field on the ExInfo
        ADD_LARGE_IMM a2, sp, rsp_offsetof_Context    // a2 <- PAL_LIMITED_CONTEXT*
        sd    a2, OFFSETOF__ExInfo__m_pExContext(a1)  // pExInfo->m_pExContext = pContext

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

        ld a1, OFFSETOF__Thread__m_pvHijackedReturnAddress(a2)
        beq a1, zero, NotHijacked

        ld a3, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation(a2)

        // Recompute SP at callsite
        ADD_LARGE_IMM t3, sp, (STACKSIZEOF_ExInfo + SIZEOF__PAL_LIMITED_CONTEXT)
        bltu a3, t3, TailCallWasHijacked // if (m_ppvHijackedReturnAddressLocation < SP at callsite)

        // Normal case where a valid return address location is hijacked
        sd a1, 0(a3)
        j ClearThreadState

    TailCallWasHijacked:

        // Abnormal case where the return address location is now invalid because we ended up here via a tail
        // call. In this case, our hijacked return address should be the correct caller of this method.

        // Stick the previous return address in RA as well as in the right spots in our PAL_LIMITED_CONTEXT.
        ori ra, a1, 0

        // Compute offsets for PAL_LIMITED_CONTEXT
        ADD_LARGE_IMM t0, sp, (rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__RA)
        sd ra, 0(t0)
        ADD_LARGE_IMM t0, sp, (rsp_offsetof_Context + OFFSETOF__PAL_LIMITED_CONTEXT__IP)
        sd ra, 0(t0)

    ClearThreadState:

        // Clear the Thread's hijack state
        sd zero, OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation(a2)
        sd zero, OFFSETOF__Thread__m_pvHijackedReturnAddress(a2)

    NotHijacked:

        // Compute the offset for ExInfo
        ADD_LARGE_IMM a1, sp, rsp_offsetof_ExInfo // a1 <- ExInfo*
        sd zero, OFFSETOF__ExInfo__m_exception(a1) // pExInfo->m_exception = null
        li a3, 1
        sb a3, OFFSETOF__ExInfo__m_passNumber(a1) // pExInfo->m_passNumber = 1
        li a3, -1
        sw a3, OFFSETOF__ExInfo__m_idxCurClause(a1) // pExInfo->m_idxCurClause = MaxTryRegionIdx
        li a3, 1
        sb a3, OFFSETOF__ExInfo__m_kind(a1) // pExInfo->m_kind = ExKind.Throw

        // Link the ExInfo into the thread's ExInfo chain
        ld a3, OFFSETOF__Thread__m_pExInfoStackHead(a2)
        sd a3, OFFSETOF__ExInfo__m_pPrevExInfo(a1) // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        sd a1, OFFSETOF__Thread__m_pExInfoStackHead(a2) // m_pExInfoStackHead = pExInfo

        // Set the exception context field on the ExInfo
        ADD_LARGE_IMM a2, sp, rsp_offsetof_Context // a2 <- PAL_LIMITED_CONTEXT*
        sd a2, OFFSETOF__ExInfo__m_pExContext(a1) // pExInfo->m_pExContext = pContext

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
        addi a1, sp, rsp_offsetof_ExInfo
        // pExInfo->m_exception = null
        sd zero, OFFSETOF__ExInfo__m_exception(a1)
        // init to a deterministic value (ExKind.None)
        sb zero, OFFSETOF__ExInfo__m_kind(a1)
        // pExInfo->m_passNumber = 1
        li a3, 1
        sb a3, OFFSETOF__ExInfo__m_passNumber(a1)
        // pExInfo->m_idxCurClause = MaxTryRegionIdx
        li a3, -1
        sw a3, OFFSETOF__ExInfo__m_idxCurClause(a1)

        // link the ExInfo into the thread's ExInfo chain
        ld a3, OFFSETOF__Thread__m_pExInfoStackHead(a2)
        // a0 <- current ExInfo
        mv a0, a3
        // pExInfo->m_pPrevExInfo = m_pExInfoStackHead
        sd a3, OFFSETOF__ExInfo__m_pPrevExInfo(a1)
        // m_pExInfoStackHead = pExInfo
        sd a1, OFFSETOF__Thread__m_pExInfoStackHead(a2)

        // set the exception context field on the ExInfo
        // a2 <- PAL_LIMITED_CONTEXT*
        ADD_LARGE_IMM a2, sp, rsp_offsetof_Context
        // pExInfo->m_pExContext = pContext
        sd a2, OFFSETOF__ExInfo__m_pExContext(a1)

        // a0 contains the currently active ExInfo
        // a1 contains the address of the new ExInfo
        jal C_FUNC(RhRethrow)

    ALTERNATE_ENTRY RhpRethrow2

        // no return
        ebreak
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
        ALLOC_CALL_FUNCLET_FRAME 0x70

        // Save floating-point registers
        fsd f24, 0(sp)
        fsd f25, 8(sp)
        fsd f26, 16(sp)
        fsd f27, 24(sp)
        fsd f28, 32(sp)
        fsd f29, 40(sp)
        fsd f30, 48(sp)
        fsd f31, 56(sp)

        // Save integer registers
        sd a0, 64(sp)    // Save a0 to a3
        sd a1, 72(sp)
        sd a2, 80(sp)
        sd a3, 88(sp)
        sd zero, 96(sp) // Make space for local "is_not_handling_thread_abort"; last qword will store the thread obj

#define rsp_offset_is_not_handling_thread_abort 96
#define rsp_offset_a0 64
#define rsp_offset_a1 72
#define rsp_offset_a2 80
#define rsp_offset_a3 88
#define rsp_CatchFunclet_offset_thread 104

        // Clear the DoNotTriggerGc flag, trashes a4-a6
        jal C_FUNC(RhpGetThread)  // Call the RhpGetThread function
        sd a0, rsp_CatchFunclet_offset_thread(sp)
        li a5, 0
        ld a0, rsp_offset_a0(sp)
        ld a1, rsp_offset_a1(sp)
        ld a2, rsp_offset_a2(sp)
        ld a3, rsp_offset_a3(sp)

        ld a4, OFFSETOF__Thread__m_threadAbortException(a5)
        sub a4, a4, a0
        sd a4, rsp_offset_is_not_handling_thread_abort(sp) // Non-zero if the exception is not ThreadAbortException

        // Handle large immediate values
        lui t3, %hi(0xFFFFFFEF)      # Load upper 20 bits of 0xFFFFFFEF
        addi t3, t3, %lo(0xFFFFFFEF) # Adjust with the lower 12 bits

        // Use `andi` with a large immediate value
        and a4, a3, t3               # Apply the mask

        // Set preserved regs to the values expected by the funclet
        RESTORE_PRESERVED_REGISTERS a2

        // Trash the values at the old homes to make sure nobody uses them
        TRASH_PRESERVED_REGISTERS_STORAGE a2

        // Call the funclet
        // a0 still contains the exception object
        jalr a1, 0  // Jump to the handler funclet

    ALTERNATE_ENTRY RhpCallCatchFunclet2

        // a0 contains resume IP

        ld a2, rsp_offset_a2(sp)  // a2 <- REGDISPLAY*

#ifdef _DEBUG
        // Call into some C++ code to validate the pop of the ExInfo. We only do this in debug because we
        // have to spill all the preserved registers and then refill them after the call.

        sd a0, rsp_offset_a0(sp)

        SAVE_PRESERVED_REGISTERS a2

        ld a0, rsp_CatchFunclet_offset_thread(sp)  // a0 <- Thread*
        ld a1, rsp_offset_a3(sp)  // a1 <- current ExInfo*
        ld a2, OFFSETOF__REGDISPLAY__SP(a2)  // a2 <- resume SP value
        jal C_FUNC(RhpValidateExInfoPop)

        ld a2, rsp_offset_a2(sp)  // a2 <- REGDISPLAY*

        RESTORE_PRESERVED_REGISTERS a2

        ld a0, rsp_offset_a0(sp)  // Reload resume IP
#endif

        ld a1, rsp_CatchFunclet_offset_thread(sp)

        // We must unhijack the thread at this point because the section of stack where the hijack is applied
        // may go dead. If it does, then the next time we try to unhijack the thread, it will corrupt the stack.
        INLINE_THREAD_UNHIJACK a1, a3, t3  // Thread in a1, trashes a3 and t3

        ld a3, rsp_offset_a3(sp)  // a3 <- current ExInfo*
        ld a2, OFFSETOF__REGDISPLAY__SP(a2)  // a2 <- resume SP value

PopExInfoLoop:
        ld a3, OFFSETOF__ExInfo__m_pPrevExInfo(a3)  // a3 <- next ExInfo
        beq a3, zero, DonePopping  // if (pExInfo == null) { we're done }
        blt a3, a2, PopExInfoLoop  // if (pExInfo < resume SP} { keep going }

DonePopping:
        sd a3, OFFSETOF__Thread__m_pExInfoStackHead(a1)  // Store the new head on the Thread

        PREPARE_EXTERNAL_VAR_INDIRECT_W RhpTrapThreads, a3

        // Use `andi` with the immediate value 0
        andi a6, a3, TrapThreadsFlags_AbortInProgress_Bit  // Apply the mask directly

        beq a6, zero, NoAbort

        ld a3, rsp_offset_is_not_handling_thread_abort(sp)
        bne a3, zero, NoAbort

        // It was the ThreadAbortException, so rethrow it
        // Reset SP
        mv a1, a0  // a1 <- continuation address as exception PC
        li a0, STATUS_REDHAWK_THREAD_ABORT
        mv sp, a2
        jal C_FUNC(RhpThrowHwEx)

NoAbort:
        // Reset SP and jump to continuation address
        mv sp, a2
        jalr zero, a0  // Jump to the continuation address

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
        ALLOC_CALL_FUNCLET_FRAME 0x60
        
        // Save floating-point registers
        fsd f24, 0(sp)
        fsd f25, 8(sp)
        fsd f26, 16(sp)
        fsd f27, 24(sp)
        fsd f28, 32(sp)
        fsd f29, 40(sp)
        fsd f30, 48(sp)
        fsd f31, 56(sp)
        
        // Save integer registers
        sd a0, 64(sp)  // Save a0 to 0x40
        sd a1, 72(sp)  // Save a1 to 0x48

#define rsp_offset_a1 72
#define rsp_FinallyFunclet_offset_thread 80

        // Clear the DoNotTriggerGc flag
        call C_FUNC(RhpGetThread)
        sd a0, rsp_FinallyFunclet_offset_thread(sp)
        li a2, 0
        ld a0, 64(sp)
        ld a1, 72(sp)

        // Set the DoNotTriggerGc flag
        la t3, OFFSETOF__Thread__m_ThreadStateFlags
        li a3, -17  // Mask value (0xFFFFFFEF)
        and a4, a3, t3

        // Restore preserved registers
        RESTORE_PRESERVED_REGISTERS a1

        // Trash the values at the old homes to make sure nobody uses them
        TRASH_PRESERVED_REGISTERS_STORAGE a1

        // Call the funclet
        jalr a0, 0  // Jump to the funclet

    ALTERNATE_ENTRY RhpCallFinallyFunclet2

        ld a1, rsp_offset_a1(sp)  // Reload REGDISPLAY pointer

        // Save new values of preserved registers into REGDISPLAY
        SAVE_PRESERVED_REGISTERS a1

        // Restore the DoNotTriggerGc flag
        ld a2, rsp_FinallyFunclet_offset_thread(sp)
        la t3, OFFSETOF__Thread__m_ThreadStateFlags
        li a3, -17  // Mask value (0xFFFFFFEF)
        and a1, a3, t3

        // Restore floating-point registers
        fld f24, 0(sp)
        fld f25, 8(sp)
        fld f26, 16(sp)
        fld f27, 24(sp)
        fld f28, 32(sp)
        fld f29, 40(sp)
        fld f30, 48(sp)
        fld f31, 56(sp)

        // Free call funclet frame
        FREE_CALL_FUNCLET_FRAME 0x60
        
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
        ALLOC_CALL_FUNCLET_FRAME 0x40
        fsd  f24, 0x00(sp)
        fsd  f25, 0x08(sp)
        fsd  f26, 0x10(sp)
        fsd  f27, 0x18(sp)
        fsd  f28, 0x20(sp)
        fsd  f29, 0x28(sp)
        fsd  f30, 0x30(sp)
        fsd  f31, 0x38(sp)

        ld  t3, OFFSETOF__REGDISPLAY__pFP(a2)
        ld  fp, 0(t3)

        //
        // call the funclet
        //
        // a0 still contains the exception object
        jalr ra, a1, 0

    ALTERNATE_ENTRY RhpCallFilterFunclet2

        fld  f24, 0x00(sp)
        fld  f25, 0x08(sp)
        fld  f26, 0x10(sp)
        fld  f27, 0x18(sp)
        fld  f28, 0x20(sp)
        fld  f29, 0x28(sp)
        fld  f30, 0x30(sp)
        fld  f31, 0x38(sp)

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
