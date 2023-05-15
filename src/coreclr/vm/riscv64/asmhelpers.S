// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation llacenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

LEAF_ENTRY GetCurrentIP, _TEXT
    addi  a0, ra, 0
    ret
LEAF_END GetCurrentIP, _TEXT

// LPVOID __stdcall GetCurrentSP(void)//
LEAF_ENTRY GetCurrentSP, _TEXT
    addi  a0, sp, 0
    ret
LEAF_END GetCurrentSP, _TEXT

//-----------------------------------------------------------------------------
// The following Macros help in WRITE_BARRIER Implementations
// WRITE_BARRIER_ENTRY
//
// Declare the start of a write barrier function. Use similarly to NESTED_ENTRY. This is the only legal way
// to declare a write barrier function.
//
.macro WRITE_BARRIER_ENTRY name
    LEAF_ENTRY \name, _TEXT
.endm

// WRITE_BARRIER_END
//
// The partner to WRITE_BARRIER_ENTRY, used llake NESTED_END.
//
.macro WRITE_BARRIER_END name
    LEAF_END_MARKED \name, _TEXT
.endm

// void JIT_UpdateWriteBarrierState(bool skipEphemeralCheck, size_t writeableOffset)
//
// Update shadow copies of the various state info required for barrier
//
// State info is contained in a llateral pool at the end of the function
// Placed in text section so that it is close enough to use ldr llateral and still
// be relocatable. Ellaminates need for PREPARE_EXTERNAL_VAR in hot code.
//
// Allagn and group state info together so it fits in a single cache line
// and each entry can be written atomically
//
WRITE_BARRIER_ENTRY JIT_UpdateWriteBarrierState
    // a0-a7 will contain intended new state
    // t0 will preserve skipEphemeralCheck
    // t2 will be used for pointers

    addi  t0, a0, 0
    addi  t1, a1, 0

    lla  a0, g_card_table
    ld  a0, 0(a0)

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    lla  a1, g_card_bundle_table
    ld  a1, 0(a1)
#endif

#ifdef WRITE_BARRIER_CHECK
    lla  a2, g_GCShadow
    ld  a2, 0(a2)
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    lla  a3, g_sw_ww_table
    ld  a3, 0(a3)
#endif

    lla  a4, g_ephemeral_low
    ld  a4, 0(a4)

    lla  a5, g_ephemeral_high
    ld  a5, 0(a5)

    beq  t0, zero, LOCAL_LABEL(EphemeralCheckEnabled)

    ori  a4, zero, 0
    addi  a5, zero, -1
LOCAL_LABEL(EphemeralCheckEnabled):

    lla  a6, g_lowest_address
    ld  a6, 0(a6)

    lla  a7, g_highest_address
    ld  a7, 0(a7)

    // Update wbs state
    lla  t2, JIT_WriteBarrier_Table_Loc
    ld  t2, 0(t2)
    add  t2, t2, t1

    sd  a0, 0(t2)
    sd  a1, 8(t2)
    sd  a2, 16(t2)
    sd  a3, 24(t2)
    sd  a4, 32(t2)
    sd  a5, 40(t2)
    sd  a6, 48(t2)
    sd  a7, 56(t2)

    EPILOG_RETURN

WRITE_BARRIER_END JIT_UpdateWriteBarrierState

// ----------------------------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_WriteBarrier_Callable(Object **dst, Object* val)
LEAF_ENTRY  JIT_WriteBarrier_Callable, _TEXT
    // Setup args for JIT_WriteBarrier. a0 = dst ; a1 = val
    addi  t3, a0, 0                 // t3 = dst
    addi  t4, a1, 0                 // t4 = val

    // Branch to the write barrier
    lla  t1, JIT_WriteBarrier_Loc
    ld  t1, 0(t1)
    jr  t1
LEAF_END JIT_WriteBarrier_Callable, _TEXT


.balign 64  // Align to power of two at least as big as patchable literal pool so that it fits optimally in cache line
// ------------------------------------------------------------------
// Start of the writeable code region
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
    ret
LEAF_END JIT_PatchedCodeStart, _TEXT

// void JIT_ByRefWriteBarrier
//
// On entry:
//   t5 : the source address (points to object reference to write)
//   t3: the destination address (object reference written here)
//
// On exit:
//   t5  : incremented by 8
//   t4  : trashed
//

// void JIT_ByRefWriteBarrier
WRITE_BARRIER_ENTRY JIT_ByRefWriteBarrier
    ld  t4, 0(t5)
    addi  t5, t5, 8
    tail  C_FUNC(JIT_CheckedWriteBarrier)
WRITE_BARRIER_END JIT_ByRefWriteBarrier

//-----------------------------------------------------------------------------
// Simple WriteBarriers
// void JIT_CheckedWriteBarrier(Object** dst, Object* src)
//
// On entry:
//   t3 : the destination address (LHS of the assignment)
//   t4 : the object reference (RHS of the assignment)
//
// On exit:
//   t1  : trashed
//   t0  : trashed
//   t6  : trashed
//   t3  : trashed (incremented by 8 to implement JIT_ByRefWriteBarrier contract)
//

WRITE_BARRIER_ENTRY JIT_CheckedWriteBarrier
    lla  t6, wbs_lowest_address
    ld  t6, 0(t6)
    slt  t6, t3, t6

    lla  t1, wbs_highest_address
    ld  t1, 0(t1)
    slt  t0, t1, t3
    or  t6, t0, t6
    beq  t6, zero, C_FUNC(JIT_WriteBarrier)

    sd  t4, 0(t3)
    addi  t3, t3, 8
    ret
WRITE_BARRIER_END JIT_CheckedWriteBarrier

// void JIT_WriteBarrier(Object** dst, Object* src)
// On entry:
//   t3  : the destination address (LHS of the assignment)
//   t4  1 the object reference (RHS of the assignment)
//
// On exit:
//   t0  : trashed
//   t1  : trashed
//   t6  : trashed
//   t4  : trashed
//   t3  : trashed (incremented by 8 to implement JIT_ByRefWriteBarrier contract)
//
WRITE_BARRIER_ENTRY JIT_WriteBarrier

    // TODO: sync_release (runtime detection required)
    fence rw, rw

    sd  t4, 0(t3)

#ifdef WRITE_BARRIER_CHECK
    // Update GC Shadow Heap

    // Do not perform the work if g_GCShadow is 0
    lla  t1, wbs_GCShadow
    ld  t1, 0(t1)

    beq  t1, zero, LOCAL_LABEL(ShadowUpdateDisabled)

    // Compute address of shadow heap location:
    //   pShadow = g_GCShadow + ($t3 - g_lowest_address)
    lla  t6, wbs_lowest_address
    ld  t6, 0(t6)

    sub  t6, t3, t6
    add  t0, t6, t1

    // if (pShadow >= g_GCShadowEnd) goto end
    lla  t6, g_GCShadowEnd
    ld  t6, 0(t6)

    slt  t6, t0, t6
    beq  t6, zero, LOCAL_LABEL(ShadowUpdateEnd)

    // *pShadow = $t4
    sd  t4, 0(t0)

    // Ensure that the write to the shadow heap occurs before the read from the GC heap so that race
    // conditions are caught by INVALIDGCVALUE.
    fence rw, rw

    // if (*t3 == t4) goto end
    ld  t6, 0(t3)
    beq  t6, t4, LOCAL_LABEL(ShadowUpdateEnd)

    // *pShadow = INVALIDGCVALUE (0xcccccccd)
    li  t6, 0xcccccccd
    sd  t6, 0(t0)
LOCAL_LABEL(ShadowUpdateEnd):
LOCAL_LABEL(ShadowUpdateDisabled):
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    // Update the write watch table if necessary

    lla  t6, wbs_sw_ww_table
    ld  t6, 0(t6)
    beq  t6, zero, LOCAL_LABEL(CheckCardTable)

    srli  t4, t3, 0xc
    add  t6, t6, t4  // SoftwareWriteWatch::AddressToTableByteIndexShift
    lb  t4, 0(t6)
    bne  t4, zero, LOCAL_LABEL(CheckCardTable)

    ori  t4, zero, 0xFF
    sb  t4, 0(t6)

LOCAL_LABEL(CheckCardTable):
#endif
    // Branch to Exit if the reference is not in the Gen0 heap
    lla  t6, wbs_ephemeral_low
    ld  t6, 0(t6)
    beq  t6, zero, LOCAL_LABEL(SkipEphemeralCheck)

    slt  t0, t4, t6
    lla  t6, wbs_ephemeral_high
    ld  t6, 0(t6)
    slt  t1, t6, t4
    or  t0, t1, t0
    bne  t0, zero, LOCAL_LABEL(Exit)

LOCAL_LABEL(SkipEphemeralCheck):
    // Check if we need to update the card table
    lla  t6, wbs_card_table
    ld  t6, 0(t6)
    srli  t0, t3, 11
    add  t4, t6, t0
    lbu  t1, 0(t4)
    ori  t0, zero, 0xFF
    beq  t1, t0, LOCAL_LABEL(Exit)

    sb  t0, 0(t4)

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    // Check if we need to update the card bundle table
    lla  t6, wbs_card_bundle_table
    ld  t6, 0(t6)
    srli  t0, t3, 21
    add  t4, t6, t0

    lbu  t6, 0(t4)
    ori  t0, zero, 0xFF
    beq  t6, t0, LOCAL_LABEL(Exit)

    sb  t0, 0(t4)
#endif
LOCAL_LABEL(Exit):
    addi  t3, t3, 8
    ret
WRITE_BARRIER_END JIT_WriteBarrier

// Begin patchable literal pool
    .balign 64  // Align to power of two at least as big as patchable literal pool so that it fits optimally in cache line
WRITE_BARRIER_ENTRY JIT_WriteBarrier_Table
wbs_begin:
wbs_card_table:
    .quad 0
wbs_card_bundle_table:
    .quad 0
wbs_GCShadow:
    .quad 0
wbs_sw_ww_table:
    .quad 0
wbs_ephemeral_low:
    .quad 0
wbs_ephemeral_high:
    .quad 0
wbs_lowest_address:
    .quad 0
wbs_highest_address:
    .quad 0
WRITE_BARRIER_END JIT_WriteBarrier_Table

// ------------------------------------------------------------------
// End of the writeable code region
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
    ret
LEAF_END JIT_PatchedCodeLast, _TEXT


//
// If a preserved register were pushed onto the stack between
// the managed caller and the H_M_F, ptrS0_S8 will point to its
// location on the stack and it would have been updated on the
// stack by the GC already and it will be popped back into the
// appropriate register when the appropriate epilog is run.
//
// Otherwise, the register is preserved across all the code
// in this HCALL or FCALL, so we need to update those registers
// here because the GC will have updated our copies in the
// frame.
//
// So, if ptrS0_S8 points into the MachState, we need to update
// the register here.  That's what this macro does.
//
.macro RestoreRegMS idx, reg
    // Incoming:
    //
    // a0 = address of MachState
    //
    // idx: Index of the callee register
    // s0/fp: 0, s1: 1, s3-s11: 4-11, gp: 12 tp: 13
    //
    // reg: Register name (e.g. s0, s1, etc)
    //
    // Get the address of the specified captured register from machine state
    addi  a2, a0, (MachState__captureCalleeSavedRegisters + (\idx * 8))

    //// Get the content of specified preserved register pointer from machine state
    ld  a3, (MachState__ptrCalleeSavedRegisters + (\idx * 8))(a0)

    bne  a2, a3, LOCAL_LABEL(NoRestore_\reg)

    ld  \reg, 0(a2)
LOCAL_LABEL(NoRestore_\reg):

.endm

NESTED_ENTRY ThePreStub, _TEXT, NoHandler
    PROLOG_WITH_TRANSITION_BLOCK
    .cfi_adjust_cfa_offset __PWTB_StackAlloc

    addi  a1, METHODDESC_REGISTER, 0 // pMethodDesc

    addi  a0, sp, __PWTB_TransitionBlock        // pTransitionBlock
    call  PreStubWorker
    addi  t4, a0, 0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    .cfi_adjust_cfa_offset -__PWTB_StackAlloc
    EPILOG_BRANCH_REG  t4
NESTED_END ThePreStub, _TEXT

// ------------------------------------------------------------------\

// EXTERN_C int __fastcall HelperMethodFrameRestoreState(
// INDEBUG_COMMA(HelperMethodFrame *pFrame)
// MachState *pState
// )
LEAF_ENTRY HelperMethodFrameRestoreState, _TEXT
#ifdef _DEBUG
    addi  a0, a1, 0
#endif

    // If machine state is invalid, then simply exit
    lw  a1, MachState__isValid(a0)
    beq  a1, zero, LOCAL_LABEL(Done)

    // manually assign index
    // s0/fp: 0, s1: 1, s3-s11: 4-11, gp: 12 tp: 13
    RestoreRegMS  0, s0 
    RestoreRegMS  1, s1
    RestoreRegMS  2, s2
    RestoreRegMS  3, s3
    RestoreRegMS  4, s4
    RestoreRegMS  5, s5
    RestoreRegMS  6, s6
    RestoreRegMS  7, s7
    RestoreRegMS  8, s8
    RestoreRegMS  9, s9
    RestoreRegMS  10, s10
    RestoreRegMS  11, s11
    RestoreRegMS  12, gp
    RestoreRegMS  13, tp
LOCAL_LABEL(Done):
    // Its imperative that the return value of HelperMethodFrameRestoreState is zero
    // as it is used in the state machine to loop until it becomes zero.
    // Refer to HELPER_METHOD_FRAME_END macro for details.
    addi  a0, zero, 0
    ret
LEAF_END HelperMethodFrameRestoreState, _TEXT

//-----------------------------------------------------------------------------
// This routine captures the machine state. It is used by helper method frame
//-----------------------------------------------------------------------------
//void LazyMachStateCaptureState(struct LazyMachState *pState)//
LEAF_ENTRY LazyMachStateCaptureState, _TEXT
    // marks that this is not yet valid
    sw  zero, (MachState__isValid)(a0)

    sd  ra, (LazyMachState_captureIp)(a0)

    // save sp register.
    sd  sp, (LazyMachState_captureSp)(a0)

    // save non-volatile registers that can contain object references
    addi  a1, a0, LazyMachState_captureCalleeSavedRegisters

    sd  s0, 0(a1)
    sd  s1, 8(a1)
    sd  s2, 16(a1)
    sd  s3, 24(a1)
    sd  s4, 32(a1)
    sd  s5, 40(a1)
    sd  s6, 48(a1)
    sd  s7, 56(a1)
    sd  s8, 64(a1)
    sd  s9, 72(a1)
    sd  s10, 80(a1)
    sd  s11, 88(a1)
    sd  gp, 96(a1)
    sd  tp, 104(a1)

    ret
LEAF_END LazyMachStateCaptureState, _TEXT

// ------------------------------------------------------------------
// The call in ndirect import precode points to this function.
NESTED_ENTRY NDirectImportThunk, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED  fp, ra, 0xa0
    SAVE_ARGUMENT_REGISTERS  sp, 0x20
    SAVE_FLOAT_ARGUMENT_REGISTERS  sp, 0x60

    addi  a0, t2, 0
    call C_FUNC(NDirectImportWorker)
    addi  t4, a0, 0

    // pop the stack and restore original register state
    RESTORE_FLOAT_ARGUMENT_REGISTERS  sp, 0x60
    RESTORE_ARGUMENT_REGISTERS  sp, 0x20
    //EPILOG_RESTORE_REG  gp, 16
    EPILOG_RESTORE_REG_PAIR_INDEXED  fp, ra, 0xa0

    // If we got back from NDirectImportWorker, the MD has been successfully
    // linked. Proceed to execute the original DLL call.
    EPILOG_BRANCH_REG  t4
NESTED_END NDirectImportThunk, _TEXT

// void SinglecastDelegateInvokeStub(Delegate *pThis)
LEAF_ENTRY SinglecastDelegateInvokeStub, _TEXT
    beq  a0, zero, LOCAL_LABEL(LNullThis)

    ld  t4, (DelegateObject___methodPtr)(a0)
    ld  a0, (DelegateObject___target)(a0)
    jr  t4

LOCAL_LABEL(LNullThis):
    addi  a0, zero, CORINFO_NullReferenceException_ASM
    tail JIT_InternalThrow
LEAF_END SinglecastDelegateInvokeStub, _TEXT

// ------------------------------------------------------------------
// ThePreStubPatch()
LEAF_ENTRY ThePreStubPatch, _TEXT
.globl C_FUNC(ThePreStubPatchLabel)
C_FUNC(ThePreStubPatchLabel):
    ret
LEAF_END ThePreStubPatch, _TEXT

NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix
    // Save arguments and return address
    PROLOG_SAVE_REG_PAIR_INDEXED  fp, ra, 0xa0
    //PROLOG_SAVE_REG  gp, 16
    SAVE_ARGUMENT_REGISTERS  sp, 32
    SAVE_FLOAT_ARGUMENT_REGISTERS  sp, 96


    addi  a0, t2, 0
    call  TheUMEntryPrestubWorker
    addi  t4, a0, 0

    // pop the stack and restore original register state
    RESTORE_FLOAT_ARGUMENT_REGISTERS  sp, 96
    RESTORE_ARGUMENT_REGISTERS  sp, 32
    //EPILOG_RESTORE_REG  gp, 16
    EPILOG_RESTORE_REG_PAIR_INDEXED  fp, ra, 0xa0

    // and tailcall to the actual method
    EPILOG_BRANCH_REG t4
NESTED_END TheUMEntryPrestub, _TEXT

// ------------------------------------------------------------------
// void* JIT_GetSharedGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT
    // If class is not initialized, bail to C++ helper
    addi  a2, a0, DomainLocalModule__m_pDataBlob
    add  a2, a2, a1
    lb  a2, 0(a2)
    andi  t5, a2, 1
    beq  t5, zero, LOCAL_LABEL(JIT_GetSharedGCStaticBase_SingleAppDomain_CallHelper)

    ld  a0, DomainLocalModule__m_pGCStatics(a0)
    ret

LOCAL_LABEL(JIT_GetSharedGCStaticBase_SingleAppDomain_CallHelper):
    // Tail call JIT_GetSharedGCStaticBase_Helper
    call  JIT_GetSharedGCStaticBase_Helper
LEAF_END JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT

// ------------------------------------------------------------------
// ResolveWorkerChainLookupAsmStub
//
// This method will perform a quick chained lookup of the entry if the
//  initial cache lookup fails.
//
// On Entry:
//   t1       contains the pointer to the current ResolveCacheElem
//   t5       contains the address of the indirection (and the flags in the low two bits)
//   t2       contains our contract the DispatchToken
// Must be preserved:
//   a0       contains the instance object ref that we are making an interface call on
//   t1       Must point to a ResolveCacheElem [For Sanity]
//  [a1-a7]   contains any additional register arguments for the interface method
//
// Loaded from a0
//   t6       contains our type     the MethodTable  (from object ref in a0)
//
// On Exit:
//   a0, [a1-a7] arguments for the interface implementation target
//
// On Exit (to ResolveWorkerAsmStub):
//   t5       contains the address of the indirection and the flags in the low two bits.
//   t2       contains our contract (DispatchToken)
//   t4 will be trashed
//

#define BACKPATCH_FLAG      1
#define PROMOTE_CHAIN_FLAG  2

NESTED_ENTRY ResolveWorkerChainLookupAsmStub, _TEXT, NoHandler
    andi  t4, t5, BACKPATCH_FLAG     // First we check if t5 has the BACKPATCH_FLAG set
    bne  t4, zero, LOCAL_LABEL(Fail) // If the BACKPATCH_FLAGS is set we will go directly to the ResolveWorkerAsmStub

    ld  t6, 0(a0)         // retrieve the MethodTable from the object ref in a0
LOCAL_LABEL(MainLoop):
    ld  t1, (ResolveCacheElem__pNext)(t1)     // t1 <= the next entry in the chain
    beq  t1, zero, LOCAL_LABEL(Fail)

    ld  t4, 0(t1)
    // compare our MT with the one in the ResolveCacheElem
    bne  t4, t6, LOCAL_LABEL(MainLoop)

    ld  t4, 8(t1)
    // compare our DispatchToken with one in the ResolveCacheElem
    bne  t2, t4, LOCAL_LABEL(MainLoop)

LOCAL_LABEL(Success):
    PREPARE_EXTERNAL_VAR  g_dispatch_cache_chain_success_counter, t6
    ld  t4, 0(t6)
    addi t4, t4, -1
    sd  t4, 0(t6)
    blt t4, zero, LOCAL_LABEL(Promote)

    ld  t4, (ResolveCacheElem__target)(t1)    // get the ImplTarget
    jr  t4                                    // branch to interface implementation target

LOCAL_LABEL(Promote):
                          // Move this entry to head position of the chain
    addi  t4, zero, 256
    sd  t4, 0(t6)        // be quick to reset the counter so we don't get a bunch of contending threads
    ori  t5, t5, PROMOTE_CHAIN_FLAG   // set PROMOTE_CHAIN_FLAG
    addi  t2, t1, 0           // We pass the ResolveCacheElem to ResolveWorkerAsmStub instead of the DispatchToken

LOCAL_LABEL(Fail):
    tail  C_FUNC(ResolveWorkerAsmStub) // call the ResolveWorkerAsmStub method to transition into the VM
NESTED_END ResolveWorkerChainLookupAsmStub, _TEXT

// ------------------------------------------------------------------
// void ResolveWorkerAsmStub(args in regs a0-a7 & stack, t5:IndirectionCellAndFlags, t2:DispatchToken)
//
// The stub dispatch thunk which transfers control to VSD_ResolveWorker.
NESTED_ENTRY ResolveWorkerAsmStub, _TEXT, NoHandler
    PROLOG_WITH_TRANSITION_BLOCK
    .cfi_adjust_cfa_offset __PWTB_StackAlloc

    addi  a2, t2, 0                 // DispatchToken
    addi  a0, sp, __PWTB_TransitionBlock        // pTransitionBlock
    srli  a1, t5, 2
    andi  a3, t5, 3              // flag
    slli  a1, a1, 2
    call C_FUNC(VSD_ResolveWorker)
    addi  t4, a0, 0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    .cfi_adjust_cfa_offset -__PWTB_StackAlloc

    EPILOG_BRANCH_REG  t4
NESTED_END ResolveWorkerAsmStub, _TEXT

// ------------------------------------------------------------------
// void* JIT_GetSharedNonGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT
    ret
LEAF_END JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT

// ------------------------------------------------------------------
// void* JIT_GetSharedGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT
    ld  a0, (DomainLocalModule__m_pGCStatics)(a0)
    ret
LEAF_END JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT


#ifdef FEATURE_HIJACK
// ------------------------------------------------------------------
// Hijack function for functions which return a scalar type or a struct (value type)
NESTED_ENTRY OnHijackTripThread, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, ra, 0x88

    // Spill callee saved registers
    PROLOG_SAVE_REG_PAIR   s1, s2, 16
    PROLOG_SAVE_REG_PAIR   s3, s4, 32
    PROLOG_SAVE_REG_PAIR   s5, s6, 48
    PROLOG_SAVE_REG_PAIR   s7, s8, 64
    PROLOG_SAVE_REG_PAIR   s9, s10, 80 
    PROLOG_SAVE_REG s11, 96

    // save any integral return value(s)
    sd  a0, 104(sp)
    sd  a1, 112(sp)

    // save any FP/HFA return value(s)
    fsd  f0, 120(sp)
    fsd  f1, 128(sp)

    addi  a0, sp, 0
    call  C_FUNC(OnHijackWorker)

    // restore callee saved registers

    // restore any integral return value(s)
    ld  a0, 104(sp)
    ld  a1, 112(sp)

    // restore any FP/HFA return value(s)
    fld  f0, 120(sp)
    fld  f1, 128(sp)

    EPILOG_RESTORE_REG_PAIR   s1, s2, 16
    EPILOG_RESTORE_REG_PAIR   s3, s4, 32
    EPILOG_RESTORE_REG_PAIR   s5, s6, 48
    EPILOG_RESTORE_REG_PAIR   s7, s8, 64
    EPILOG_RESTORE_REG_PAIR   s9, s10, 80
    EPILOG_RESTORE_REG  s11, 96 
    EPILOG_RESTORE_REG_PAIR_INDEXED  fp, ra, 0x88
    EPILOG_RETURN
NESTED_END OnHijackTripThread, _TEXT

#endif // FEATURE_HIJACK

// ------------------------------------------------------------------
// Redirection Stub for GC in fully interruptible method
//GenerateRedirectedHandledJITCaseStub GCThreadControl
// ------------------------------------------------------------------
//GenerateRedirectedHandledJITCaseStub DbgThreadControl
// ------------------------------------------------------------------
//GenerateRedirectedHandledJITCaseStub UserSuspend

#ifdef _DEBUG
// ------------------------------------------------------------------
// Redirection Stub for GC Stress
GenerateRedirectedHandledJITCaseStub GCStress
#endif


// ------------------------------------------------------------------
// This helper enables us to call into a funclet after restoring Fp register
NESTED_ENTRY CallEHFunclet, _TEXT, NoHandler
    // On entry:
    //
    // a0 = throwable
    // a1 = PC to invoke
    // a2 = address of s0 register in CONTEXT record// used to restore the non-volatile registers of CrawlFrame
    // a3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    //

    PROLOG_SAVE_REG_PAIR_INDEXED   fp, ra, 120, 0

    // Spill callee saved registers
    PROLOG_SAVE_REG_PAIR   s1, s2, 16
    PROLOG_SAVE_REG_PAIR   s3, s4, 32
    PROLOG_SAVE_REG_PAIR   s5, s6, 48
    PROLOG_SAVE_REG_PAIR   s7, s8, 64
    PROLOG_SAVE_REG_PAIR   s9, s10, 80 
    PROLOG_SAVE_REG_PAIR   s11, gp, 96
    PROLOG_SAVE_REG tp, 112

    // Save the SP of this function
    sd  sp, 0(a3)

    ld  gp, (-40)(a2)  // offset of tp in PCONTEXT relative to S0.
    ld  tp, (-32)(a2)  // offset of tp in PCONTEXT relative to S0.
    ld  fp, 0(a2)  // offset of fp in PCONTEXT relative to S0.
    ld  s1, 8(a2)
    ld  s2, 80(a2)
    ld  s3, 88(a2)
    ld  s4, 96(a2)
    ld  s5, 104(a2)
    ld  s6, 112(a2)
    ld  s7, 120(a2)
    ld  s8, 128(a2)
    ld  s9, 136(a2)
    ld  s10, 144(a2)
    ld  s11, 152(a2)

    // Invoke the funclet
    jalr a1

    EPILOG_RESTORE_REG_PAIR   s1, s2, 16
    EPILOG_RESTORE_REG_PAIR   s3, s4, 32
    EPILOG_RESTORE_REG_PAIR   s5, s6, 48
    EPILOG_RESTORE_REG_PAIR   s7, s8, 64
    EPILOG_RESTORE_REG_PAIR   s9, s10, 80
    EPILOG_RESTORE_REG_PAIR   s11, gp, 96
    EPILOG_RESTORE_REG tp, 112

    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, ra, 120 
    EPILOG_RETURN
NESTED_END CallEHFunclet, _TEXT

// This helper enables us to call into a filter funclet by passing it the CallerSP to lookup the
// frame pointer for accessing the locals in the parent method.
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, ra, 16

    // On entry:
    //
    // a0 = throwable
    // a1 = SP of the caller of the method/funclet containing the filter
    // a2 = PC to invoke
    // a3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    //
    // Save the SP of this function
    sd  fp, 0(a3)
    // Invoke the filter funclet
    jalr a2

    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, ra, 16
    EPILOG_RETURN
NESTED_END CallEHFilterFunclet, _TEXT

#ifdef FEATURE_COMINTEROP
// Function used by COM interop to get floating point return value (since it's not in the same
// register(s) as non-floating point values).
//
// On entry//
//   a0          : size of the FP result (4 or 8 bytes)
//   a1          : pointer to 64-bit buffer to receive result
//
// On exit:
//   buffer pointed to by a1 on entry contains the float or double argument as appropriate
//
LEAF_ENTRY getFPReturn, _TEXT
    fsd  f0, 0(a1)
LEAF_END getFPReturn, _TEXT

// ------------------------------------------------------------------
// Function used by COM interop to set floating point return value (since it's not in the same
// register(s) as non-floating point values).
//
LEAF_ENTRY setFPReturn, _TEXT
    fmv.d.x f0, a1
LEAF_END setFPReturn, _TEXT

#endif // FEATURE_COMINTEROP

//
// JIT Static access helpers when coreclr host specifies single appdomain flag
//

// ------------------------------------------------------------------
// void* JIT_GetSharedNonGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT
    // If class is not initialized, bail to C++ helper
    // dext a1, a1, 0, 32
    addi  a2, a0, DomainLocalModule__m_pDataBlob

    add a2, a2, a1
    lb a2, 0(a2)
    andi  t4, a2, 1
    beq  t4, zero, LOCAL_LABEL(JIT_GetSharedNonGCStaticBase_SingleAppDomain_CallHelper)

    ret

LOCAL_LABEL(JIT_GetSharedNonGCStaticBase_SingleAppDomain_CallHelper):
    // Tail call JIT_GetSharedNonGCStaticBase_Helper
    tail  JIT_GetSharedNonGCStaticBase_Helper
LEAF_END JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT

#ifdef FEATURE_READYTORUN

NESTED_ENTRY DelayLoad_MethodCall_FakeProlog, _TEXT, NoHandler
C_FUNC(DelayLoad_MethodCall):
    .global C_FUNC(DelayLoad_MethodCall)
    PROLOG_WITH_TRANSITION_BLOCK
    .cfi_adjust_cfa_offset __PWTB_StackAlloc

    addi  a1, t5, 0      // Indirection cell
    addi  a2, t0, 0      // sectionIndex
    addi  a3, t1, 0      // Module*

    addi  a0, sp, __PWTB_TransitionBlock        // pTransitionBlock
    call  C_FUNC(ExternalMethodFixupWorker)
    addi  t4, a0, 0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    .cfi_adjust_cfa_offset -__PWTB_StackAlloc
    PATCH_LABEL ExternalMethodFixupPatchLabel
    EPILOG_BRANCH_REG   t4
NESTED_END DelayLoad_MethodCall_FakeProlog, _TEXT


.macro DynamicHelper frameFlags, suffix
NESTED_ENTRY DelayLoad_Helper\suffix\()_FakeProlog, _TEXT, NoHandler
DelayLoad_Helper\suffix:
    .global DelayLoad_Helper\suffix

    PROLOG_WITH_TRANSITION_BLOCK
    .cfi_adjust_cfa_offset __PWTB_StackAlloc

    //DynamicHelperWorker(TransitionBlock * pTransitionBlock, TADDR * pCell,
    //                    DWORD sectionIndex, Module * pModule, INT frameFlags)
    addi  a1, t5, 0      // Indirection cell
    addi  a2, t0, 0      // sectionIndex
    addi  a3, t1, 0      // Module*
    addi  a4, x0, \frameFlags

    addi  a0, sp, __PWTB_TransitionBlock        // pTransitionBlock
    call  DynamicHelperWorker

    bne a0, x0, LOCAL_LABEL(FakeProlog\suffix\()_0)

    ld  a0, __PWTB_ArgumentRegisters(sp)
    EPILOG_WITH_TRANSITION_BLOCK_RETURN
    .cfi_adjust_cfa_offset -__PWTB_StackAlloc

LOCAL_LABEL(FakeProlog\suffix\()_0):
    addi t4, a0, 0
    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    .cfi_adjust_cfa_offset -__PWTB_StackAlloc
    EPILOG_BRANCH_REG  t4

NESTED_END DelayLoad_Helper\suffix\()_FakeProlog, _TEXT
.endm

DynamicHelper DynamicHelperFrameFlags_Default
DynamicHelper DynamicHelperFrameFlags_ObjectArg, _Obj
DynamicHelper DynamicHelperFrameFlags_ObjectArg | DynamicHelperFrameFlags_ObjectArg2, _ObjObj
#endif


#ifdef PROFILING_SUPPORTED

// ------------------------------------------------------------------
LEAF_ENTRY JIT_ProfilerEnterLeaveTailcallStub, _TEXT
    ret
LEAF_END JIT_ProfilerEnterLeaveTailcallStub, _TEXT

// ------------------------------------------------------------------
#define PROFILE_ENTER    1
#define PROFILE_LEAVE    2
#define PROFILE_TAILCALL 4
#define SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA 272

// ------------------------------------------------------------------
.macro GenerateProfileHelper helper, flags
NESTED_ENTRY \helper\()Naked, _TEXT, NoHandler
    // On entry:
    //   t1 = functionIDOrClientID
    //   t2 = profiledSp
    //   t6 = throwable
    //
    // On exit:
    //   Values of a0-a7, fa0-fa7, fp are preserved.
    //   Values of other volatile registers are not preserved.

    PROLOG_SAVE_REG_PAIR_INDEXED fp, ra, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA // Allocate space and save Fp, Pc.
    SAVE_ARGUMENT_REGISTERS sp, 16          // Save t0 and argument registers (a0-a7).
    sd     zero, 88(sp)                     // Clear functionId.
    SAVE_FLOAT_ARGUMENT_REGISTERS sp, 96    // Save floating-point/SIMD registers (fa0-fa7).
    addi     t6, fp, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA // Compute probeSp - initial value of Sp on entry to the helper.
    sd  t6, 224(sp)            // Save probeSp.
    sd  t2, 232(sp)            // Save profiledSp.

    sd    zero, 240(sp)        // Clear hiddenArg.
    addi  t6, zero, \flags
    sw  t6, 248(sp)            // Save flags.
    sw  zero, 252(sp)          // clear unused field.

    addi  a1, t1, 0
    addi  a2, sp, 0
    call  C_FUNC(\helper)

    RESTORE_ARGUMENT_REGISTERS sp, 16       // Restore t0 and argument registers.
    RESTORE_FLOAT_ARGUMENT_REGISTERS sp, 96 // Restore floating-point/SIMD registers.
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, ra, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA
    EPILOG_RETURN

NESTED_END \helper\()Naked, _TEXT
.endm

GenerateProfileHelper ProfileEnter, PROFILE_ENTER
GenerateProfileHelper ProfileLeave, PROFILE_LEAVE
GenerateProfileHelper ProfileTailcall, PROFILE_TAILCALL

#endif // PROFILING_SUPPORTED


#ifdef FEATURE_TIERED_COMPILATION

NESTED_ENTRY OnCallCountThresholdReachedStub, _TEXT, NoHandler
    PROLOG_WITH_TRANSITION_BLOCK
    .cfi_adjust_cfa_offset __PWTB_StackAlloc

    addi  a0, sp, __PWTB_TransitionBlock // TransitionBlock *
    addi  a1, t3, 0 // stub-identifying token
    call  C_FUNC(OnCallCountThresholdReached)
    addi  t4, a0, 0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    .cfi_adjust_cfa_offset -__PWTB_StackAlloc
    EPILOG_BRANCH_REG t4
NESTED_END OnCallCountThresholdReachedStub, _TEXT

#endif // FEATURE_TIERED_COMPILATION
