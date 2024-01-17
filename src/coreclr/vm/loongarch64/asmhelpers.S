// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

LEAF_ENTRY GetCurrentIP, _TEXT
    ori  $a0, $ra, 0
    jirl  $r0, $ra, 0
LEAF_END GetCurrentIP, _TEXT

// LPVOID __stdcall GetCurrentSP(void)//
LEAF_ENTRY GetCurrentSP, _TEXT
    ori  $a0, $sp, 0
    jirl  $r0, $ra, 0
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
// The partner to WRITE_BARRIER_ENTRY, used like NESTED_END.
//
.macro WRITE_BARRIER_END name
    LEAF_END_MARKED \name, _TEXT
.endm

// void JIT_UpdateWriteBarrierState(bool skipEphemeralCheck, size_t writeableOffset)
//
// Update shadow copies of the various state info required for barrier
//
// State info is contained in a literal pool at the end of the function
// Placed in text section so that it is close enough to use ldr literal and still
// be relocatable. Eliminates need for PREPARE_EXTERNAL_VAR in hot code.
//
// Align and group state info together so it fits in a single cache line
// and each entry can be written atomically
//
WRITE_BARRIER_ENTRY JIT_UpdateWriteBarrierState

    // $a0-$a7,$t3 will contain intended new state
    // $t0 will preserve skipEphemeralCheck
    // $t2 will be used for pointers

    ori  $t0, $a0, 0
    ori  $t1, $a1, 0

    la.local  $a0, g_card_table
    ld.d  $a0, $a0, 0

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    la.local  $a1, g_card_bundle_table
    ld.d  $a1, $a1, 0
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    la.local  $a2, g_sw_ww_table
    ld.d  $a2, $a2, 0
#endif

    la.local  $a3, g_ephemeral_low
    ld.d  $a3, $a3, 0

    la.local  $a4, g_ephemeral_high
    ld.d  $a4, $a4, 0

    beq  $t0, $zero, LOCAL_LABEL(EphemeralCheckEnabled)

    ori  $a3, $zero, 0
    addi.d  $a4, $zero, -1
LOCAL_LABEL(EphemeralCheckEnabled):

    la.local  $a5, g_lowest_address
    ld.d  $a5, $a5, 0

    la.local  $a6, g_highest_address
    ld.d  $a6, $a6, 0

#ifdef WRITE_BARRIER_CHECK
    la.local  $a7, g_GCShadow
    ld.d  $a7, $a7, 0

    la.local  $t3, g_GCShadowEnd
    ld.d  $t3, $t3, 0
#endif

    // Update wbs state
    la.local  $t2, JIT_WriteBarrier_Table_Loc
    ld.d  $t2, $t2, 0
    add.d $t2,$t2,$t1

    st.d  $a0, $t2, 0
    st.d  $a1, $t2, 8
    st.d  $a2, $t2, 16
    st.d  $a3, $t2, 24
    st.d  $a4, $t2, 32
    st.d  $a5, $t2, 40
    st.d  $a6, $t2, 48
    st.d  $a7, $t2, 56
    st.d  $t3, $t2, 64

    EPILOG_RETURN

WRITE_BARRIER_END JIT_UpdateWriteBarrierState

// ----------------------------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_WriteBarrier_Callable(Object **dst, Object* val)
LEAF_ENTRY  JIT_WriteBarrier_Callable, _TEXT

    // Setup args for JIT_WriteBarrier. $t0 = dst ; $t1 = val
    ori     $t6, $a0, 0                 // $t6 = dst
    ori     $t7, $a1, 0                 // $t7 = val

    // Branch to the write barrier
    la.local  $r21, JIT_WriteBarrier_Loc
    ld.d  $r21, $r21, 0
    jirl  $r0, $r21, 0
LEAF_END JIT_WriteBarrier_Callable, _TEXT


.balign 64  // Align to power of two at least as big as patchable literal pool so that it fits optimally in cache line
// ------------------------------------------------------------------
// Start of the writeable code region
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
    jirl  $r0, $ra, 0
LEAF_END JIT_PatchedCodeStart, _TEXT

// void JIT_ByRefWriteBarrier
//
// On entry:
//   t8 : the source address (points to object reference to write)
//   t6: the destination address (object reference written here)
//
// On exit:
//   t8  : incremented by 8
//   t7  : trashed
//
WRITE_BARRIER_ENTRY JIT_ByRefWriteBarrier
    ld.d  $t7, $t8, 0
    addi.d  $t8, $t8, 8
    b  C_FUNC(JIT_CheckedWriteBarrier)
WRITE_BARRIER_END JIT_ByRefWriteBarrier

//-----------------------------------------------------------------------------
// Simple WriteBarriers
// void JIT_CheckedWriteBarrier(Object** dst, Object* src)
//
// On entry:
//   t6 : the destination address (LHS of the assignment)
//   t7 : the object reference (RHS of the assignment)
//
// On exit:
//   $t1  : trashed
//   $t0  : trashed
//   $t3  : trashed
//   $t4  : trashed
//   $t6  : trashed (incremented by 8 to implement JIT_ByRefWriteBarrier contract)
//
WRITE_BARRIER_ENTRY JIT_CheckedWriteBarrier

    // load the address wbs_card_table to $t3
.Ltmp0:
    pcaddi $t3, 0
    .reloc .Ltmp0, R_LARCH_PCREL20_S2, wbs_card_table
    // wbs_highest_address = wbs_card_table + 48
    ld.d  $t1, $t3, 48
    // wbs_lowest_address  = wbs_card_table + 40
    ld.d  $t3, $t3, 40
    slt  $t4, $t6, $t3

    slt  $t0, $t1, $t6
    or  $t4, $t0, $t4
    beq  $t4, $zero, C_FUNC(JIT_WriteBarrier)

    st.d  $t7, $t6, 0
    addi.d  $t6, $t6, 8
    jirl  $r0, $ra, 0
WRITE_BARRIER_END JIT_CheckedWriteBarrier

// void JIT_WriteBarrier(Object** dst, Object* src)
// On entry:
//   t6  : the destination address (LHS of the assignment)
//   t7  : the object reference (RHS of the assignment)
//
// On exit:
//   $t0  : trashed
//   $t1  : trashed
//   $t3  : trashed
//   $t4  : trashed
//   $t6  : trashed (incremented by 8 to implement JIT_ByRefWriteBarrier contract)
//   $t7  : trashed
//
WRITE_BARRIER_ENTRY JIT_WriteBarrier

    dbar 0 // TODO: sync_release (runtime detection required)

    st.d  $t7, $t6, 0

    // load the address wbs_card_table to the $t3
    // and will cache the wbs_card_table by $t3 within the JIT_WriteBarrier.
    // So please DONT'T overwrite the $t3 within the JIT_WriteBarrier !!!
.Ltmp1:
    pcaddi $t3, 0
    .reloc .Ltmp1, R_LARCH_PCREL20_S2, wbs_card_table

#ifdef WRITE_BARRIER_CHECK
    // Update GC Shadow Heap

    // the wbs_GCShadow = $t3 + 56
    ld.d $t1, $t3, 56

    // Do not perform the work if g_GCShadow is 0
    beq  $t1, $zero, 22f //LOCAL_LABEL(ShadowUpdateDisabled)

    // Compute address of shadow heap location:
    //   pShadow = g_GCShadow + ($t6 - g_lowest_address)
    // the wbs_lowest_address = $t3 + 40
    ld.d $t0, $t3, 40

    sub.d  $t0, $t6, $t0
    add.d  $t0, $t0, $t1

    // if (pShadow >= g_GCShadowEnd) goto end.
    // load the wbs_GCShadowEnd = $t3 + 64
    ld.d $t1, $t3, 64

    slt  $t4, $t0, $t1
    beq  $t4, $zero, 22f //LOCAL_LABEL(ShadowUpdateEnd)

    // *pShadow = $t7
    st.d  $t7, $t0, 0

    // Ensure that the write to the shadow heap occurs before the read from the GC heap so that race
    // conditions are caught by INVALIDGCVALUE.
    dbar 0

    // if (*$t6 == $t7) goto end
    ld.d  $t1, $t6, 0
    beq  $t1, $t7, 22f //LOCAL_LABEL(ShadowUpdateEnd)

    // *pShadow = INVALIDGCVALUE (0xcccccccd)
    //lu12i.w  $t1, 0xccccc
    lu12i.w  $t1, -209716
    ori  $t1, $t1, 0xccd
    st.d $t1, $t0, 0
22:
//LOCAL_LABEL(ShadowUpdateEnd):
//LOCAL_LABEL(ShadowUpdateDisabled):
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    // Update the write watch table if necessary

    // the wbs_sw_ww_table = $t3 + 16
    ld.d  $t4, $t3, 16
    beq  $t4, $zero, 1f //LOCAL_LABEL(CheckCardTable)

    srli.d  $t0, $t6, 0xc
    add.d  $t4, $t4, $t0  // SoftwareWriteWatch::AddressToTableByteIndexShift
    ld.b  $t0, $t4, 0
    bne  $t0, $zero, 1f //LOCAL_LABEL(CheckCardTable)

    ori  $t0, $zero, 0xFF
    st.b  $t0, $t4, 0
1:
//LOCAL_LABEL(CheckCardTable):
#endif
    // Branch to Exit if the reference is not in the Gen0 heap
    // the wbs_ephemeral_low = $t3 + 24
    ld.d  $t0, $t3, 24
    beq  $t0, $zero, LOCAL_LABEL(SkipEphemeralCheck)

    slt  $t4, $t7, $t0
    // the wbs_ephemeral_high = $t3 + 32
    ld.d  $t1, $t3, 32
    slt  $t0, $t1, $t7
    or  $t4, $t0, $t4
    bne  $t4, $zero, LOCAL_LABEL(Exit)
LOCAL_LABEL(SkipEphemeralCheck):
    // Check if we need to update the card table
    // the wbs_card_table = $t3
    ld.d  $t0, $t3, 0
    srli.d  $t4, $t6, 11
    add.d  $t7, $t0, $t4
    ld.bu  $t1, $t7, 0
    ori  $t4, $zero, 0xFF
    beq  $t1, $t4, LOCAL_LABEL(Exit)

    st.b  $t4, $t7, 0

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    // Check if we need to update the card bundle table
    // the wbs_card_bundle_table = $t3 + 8
    ld.d  $t0, $t3, 8
    srli.d  $t4, $t6, 21
    add.d  $t7, $t0, $t4

    ld.bu  $t1, $t7, 0
    ori  $t4, $zero, 0xFF
    beq  $t1, $t4, LOCAL_LABEL(Exit)

    st.b  $t4, $t7, 0
#endif
LOCAL_LABEL(Exit):
    addi.d  $t6, $t6, 8
    jirl  $r0, $ra, 0
WRITE_BARRIER_END JIT_WriteBarrier


// Begin patchable literal pool
    .balign 64  // Align to power of two at least as big as patchable literal pool so that it fits optimally in cache line
WRITE_BARRIER_ENTRY JIT_WriteBarrier_Table
wbs_card_table:
    .quad 0
//wbs_card_bundle_table: =wbs_card_table+8
    .quad 0
//wbs_sw_ww_table:       =wbs_card_table+16
    .quad 0
//wbs_ephemeral_low:     =wbs_card_table+24
    .quad 0
//wbs_ephemeral_high:    =wbs_card_table+32
    .quad 0
//wbs_lowest_address:    =wbs_card_table+40
    .quad 0
//wbs_highest_address:   =wbs_card_table+48
    .quad 0
//wbs_GCShadow:          =wbs_card_table+56
    .quad 0
//wbs_GCShadowEnd:       =wbs_card_table+64
    .quad 0
WRITE_BARRIER_END JIT_WriteBarrier_Table

// ------------------------------------------------------------------
// End of the writeable code region
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
    jirl  $r0, $ra, 0
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
.macro RestoreRegMS regIndex, reg
    // Incoming:
    //
    // $a0 = address of MachState
    //
    // $regIndex: Index of the register (s0-s8). For s0, index is 23.
    //For s1, index is 24, and so on.
    //
    // $reg: Register name (e.g. s0, s1, etc)
    //
    // Get the address of the specified captured register from machine state
    addi.d  $a2, $a0,(MachState__captureCalleeSavedRegisters + ((\regIndex-23)*8))

    //// Get the content of specified preserved register pointer from machine state
    ld.d  $a3, $a0, (MachState__ptrCalleeSavedRegisters + ((\regIndex-23)*8))

    bne  $a2, $a3, LOCAL_LABEL(NoRestore_\reg)

    ld.d  $\reg, $a2, 0
LOCAL_LABEL(NoRestore_\reg):

.endm

NESTED_ENTRY ThePreStub, _TEXT, NoHandler
    PROLOG_WITH_TRANSITION_BLOCK

    ori  $a1, $METHODDESC_REGISTER, 0 // pMethodDesc

    addi.d $a0, $sp, __PWTB_TransitionBlock        // pTransitionBlock
    bl  PreStubWorker
    ori  $t4,$a0,0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG  $t4
NESTED_END ThePreStub, _TEXT

// ------------------------------------------------------------------
// EXTERN_C int __fastcall HelperMethodFrameRestoreState(
// INDEBUG_COMMA(HelperMethodFrame *pFrame)
// MachState *pState
// )
LEAF_ENTRY HelperMethodFrameRestoreState, _TEXT
#ifdef _DEBUG
    ori  $a0, $a1, 0
#endif

    // If machine state is invalid, then simply exit
    ld.w  $a1, $a0, MachState__isValid
    beq  $a1, $zero, LOCAL_LABEL(Done)

    RestoreRegMS  23, s0
    RestoreRegMS  24, s1
    RestoreRegMS  25, s2
    RestoreRegMS  26, s3
    RestoreRegMS  27, s4
    RestoreRegMS  28, s5
    RestoreRegMS  29, s6
    RestoreRegMS  30, s7
    RestoreRegMS  31, s8
    RestoreRegMS  32, fp  //NOTE: here 32 is not the real fp register number.
    RestoreRegMS  33, tp  //NOTE: here 33 is not the real tp register number.
LOCAL_LABEL(Done):
    // Its imperative that the return value of HelperMethodFrameRestoreState is zero
    // as it is used in the state machine to loop until it becomes zero.
    // Refer to HELPER_METHOD_FRAME_END macro for details.
    ori  $a0, $zero, 0
    jirl  $r0, $ra, 0
LEAF_END HelperMethodFrameRestoreState, _TEXT

//-----------------------------------------------------------------------------
// This routine captures the machine state. It is used by helper method frame
//-----------------------------------------------------------------------------
//void LazyMachStateCaptureState(struct LazyMachState *pState)//
LEAF_ENTRY LazyMachStateCaptureState, _TEXT
    // marks that this is not yet valid
    st.w  $zero, $a0, MachState__isValid

    st.d  $ra, $a0, LazyMachState_captureIp

    // save $sp register.
    st.d  $sp, $a0, LazyMachState_captureSp

    // save non-volatile registers that can contain object references
    addi.d  $a1, $a0, LazyMachState_captureCalleeSavedRegisters
    st.d  $s0, $a1, 0
    st.d  $s1, $a1, 8
    st.d  $s2, $a1, 16
    st.d  $s3, $a1, 24
    st.d  $s4, $a1, 32
    st.d  $s5, $a1, 40
    st.d  $s6, $a1, 48
    st.d  $s7, $a1, 56
    st.d  $s8, $a1, 64
    st.d  $fp, $a1, 72
    st.d  $tp, $a1, 80

    jirl  $r0, $ra, 0
LEAF_END LazyMachStateCaptureState, _TEXT

// ------------------------------------------------------------------
// The call in ndirect import precode points to this function.
NESTED_ENTRY NDirectImportThunk, _TEXT, NoHandler

    //                            $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 0xa0
    //PROLOG_SAVE_REG gp, 16
    SAVE_ARGUMENT_REGISTERS  $sp, 0x20
    SAVE_FLOAT_ARGUMENT_REGISTERS  $sp, 0x60

    ori  $a0, $t2, 0
    bl C_FUNC(NDirectImportWorker)
    ori  $t4,$a0,0

    // pop the stack and restore original register state
    RESTORE_FLOAT_ARGUMENT_REGISTERS  $sp, 0x60
    RESTORE_ARGUMENT_REGISTERS  $sp, 0x20
    //EPILOG_RESTORE_REG  gp, 16
    //                              $fp,$ra
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 0xa0

    // If we got back from NDirectImportWorker, the MD has been successfully
    // linked. Proceed to execute the original DLL call.
    EPILOG_BRANCH_REG  $t4
NESTED_END NDirectImportThunk, _TEXT

#ifdef FEATURE_PREJIT
//------------------------------------------------
// VirtualMethodFixupStub
//
// In NGEN images, virtual slots inherited from cross-module dependencies
// point to a jump thunk that calls into the following function that will
// call into a VM helper. The VM helper is responsible for patching up
// thunk, upon executing the precode, so that all subsequent calls go directly
// to the actual method body.
//
// This is done lazily for performance reasons.
//
// On entry:
//
// $a0 = "this" pointer
// $t2 = Address of thunk

NESTED_ENTRY VirtualMethodFixupStub, _TEXT, NoHandler

    // Save arguments and return address
    //                            $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 0xa0
    //PROLOG_SAVE_REG  gp, 16
    SAVE_ARGUMENT_REGISTERS  $sp, 32
    SAVE_FLOAT_ARGUMENT_REGISTERS  $sp, 96


    // Call the helper in the VM to perform the actual fixup
    // and tell us where to tail call. $a0 already contains
    // the this pointer.

    // Move the thunk start address in $a1
    ori  $a1, $t2, 0
    bl VirtualMethodFixupWorker
    ori  $t4,$a0,0

    // On return, a0 contains the target to tailcall to

    // pop the stack and restore original register state
    RESTORE_FLOAT_ARGUMENT_REGISTERS  $sp, 96
    RESTORE_ARGUMENT_REGISTERS  $sp, 32
    //EPILOG_RESTORE_REG  gp, 16
    //                              $fp,$ra
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 0xa0

    PATCH_LABEL  VirtualMethodFixupPatchLabel

    // and tailcall to the actual method
    EPILOG_BRANCH_REG  $t4
NESTED_END VirtualMethodFixupStub, _TEXT
#endif // FEATURE_PREJIT

// void SinglecastDelegateInvokeStub(Delegate *pThis)
LEAF_ENTRY SinglecastDelegateInvokeStub, _TEXT
    beq  $a0, $zero, LOCAL_LABEL(LNullThis)

    ld.d $t4, $a0, DelegateObject___methodPtr
    ld.d $a0, $a0, DelegateObject___target
    jirl  $r0, $t4, 0

LOCAL_LABEL(LNullThis):
    addi.d  $a0, $zero, CORINFO_NullReferenceException_ASM
    b JIT_InternalThrow
LEAF_END SinglecastDelegateInvokeStub, _TEXT

// ------------------------------------------------------------------
// ThePreStubPatch()
LEAF_ENTRY ThePreStubPatch, _TEXT
.globl C_FUNC(ThePreStubPatchLabel)
C_FUNC(ThePreStubPatchLabel):
    jirl  $r0, $ra, 0
LEAF_END ThePreStubPatch, _TEXT

#ifdef FEATURE_PREJIT
// ------------------------------------------------------------------
// void StubDispatchFixupStub(args in regs $a0-$a7 & stack, $t1:IndirectionCellAndFlags)
//
// The stub dispatch thunk which transfers control to StubDispatchFixupWorker.
NESTED_ENTRY StubDispatchFixupStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    srli.d  $a1, $t8, 2
    slli.d  $a1, $a1, 2 // Indirection cell

    addi.d $a0, $sp, __PWTB_TransitionBlock        // pTransitionBlock
    ori  $a2, $zero, 0 // sectionIndex
    ori  $a3, $zero, 0 // pModule
    bl StubDispatchFixupWorker
    ori  $t4,$a0,0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    PATCH_LABEL StubDispatchFixupPatchLabel
    EPILOG_BRANCH_REG  $t4
NESTED_END StubDispatchFixupStub, _TEXT
#endif

//
// $t2 = UMEntryThunk*
//
NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix

    // Save arguments and return address
    //                            $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 0xa0
    //PROLOG_SAVE_REG  gp, 16
    SAVE_ARGUMENT_REGISTERS  $sp, 32
    SAVE_FLOAT_ARGUMENT_REGISTERS  $sp, 96


    ori  $a0, $t2, 0
    bl  TheUMEntryPrestubWorker
    ori  $t4,$a0,0

    // pop the stack and restore original register state
    RESTORE_FLOAT_ARGUMENT_REGISTERS  $sp, 96
    RESTORE_ARGUMENT_REGISTERS  $sp, 32
    //EPILOG_RESTORE_REG  gp, 16
    //                              $fp,$ra
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 0xa0

    // and tailcall to the actual method
    EPILOG_BRANCH_REG $t4
NESTED_END TheUMEntryPrestub, _TEXT

// ------------------------------------------------------------------
// void* JIT_GetSharedGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT
    // If class is not initialized, bail to C++ helper
    addi.d $a2, $a0, DomainLocalModule__m_pDataBlob
    add.d $a2, $a2, $a1
    ld.b $a2, $a2, 0
    andi  $t8, $a2, 1
    beq  $t8, $zero, 1f //LOCAL_LABEL(JIT_GetSharedGCStaticBase_SingleAppDomain_CallHelper)

    ld.d  $a0, $a0, DomainLocalModule__m_pGCStatics
    jirl  $r0, $ra, 0

1:
//LOCAL_LABEL(JIT_GetSharedGCStaticBase_SingleAppDomain_CallHelper):
    // Tail call JIT_GetSharedGCStaticBase_Helper
    bl  JIT_GetSharedGCStaticBase_Helper
LEAF_END JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT

// Make sure the `FaultingExceptionFrame_StackAlloc` is 16-byte aligned.
#define FaultingExceptionFrame_StackAlloc (SIZEOF__GSCookie + SIZEOF__FaultingExceptionFrame + 0x8)
#define FaultingExceptionFrame_FrameOffset SIZEOF__GSCookie

.macro GenerateRedirectedStubWithFrame stub, target

    //
    // This is the primary function to which execution will be redirected to.
    //
    NESTED_ENTRY \stub, _TEXT, NoHandler

        //
        // IN: ra: original IP before redirect
        //

        PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, 16

        // alloc stack for FaultingExceptionFrame.
        addi.d  $sp, $sp, -FaultingExceptionFrame_StackAlloc

        // stack must be 16 bytes aligned
        CHECK_STACK_ALIGNMENT

        // Save pointer to FEF for GetFrameFromRedirectedStubStackFrame
        addi.d  $a0, $sp, FaultingExceptionFrame_FrameOffset

        // Prepare to initialize to NULL
        st.d    $r0, $a0, 0  // Initialize vtbl (it is not strictly necessary)
        st.d    $r0, $a0, FaultingExceptionFrame__m_fFilterExecuted  // Initialize BOOL for personality routine

        bl   C_FUNC(\target)
        // Target should not return.
        EMIT_BREAKPOINT

    NESTED_END \stub, _TEXT

.endm

GenerateRedirectedStubWithFrame RedirectForThreadAbort, ThrowControlForThread

// ------------------------------------------------------------------
// ResolveWorkerChainLookupAsmStub
//
// This method will perform a quick chained lookup of the entry if the
//  initial cache lookup fails.
//
// On Entry:
//   $t1       contains the pointer to the current ResolveCacheElem
//   t8       contains the address of the indirection (and the flags in the low two bits)
//   $t2       contains our contract the DispatchToken
// Must be preserved:
//   $a0       contains the instance object ref that we are making an interface call on
//   $t1       Must point to a ResolveCacheElem [For Sanity]
//  [$a1-$a7]   contains any additional register arguments for the interface method
//
// Loaded from $a0
//   $t3       contains our type     the MethodTable  (from object ref in $a0)
//
// On Exit:
//   $a0, [$a1-$a7] arguments for the interface implementation target
//
// On Exit (to ResolveWorkerAsmStub):
//   t8       contains the address of the indirection and the flags in the low two bits.
//   $t2       contains our contract (DispatchToken)
//   t4 will be trashed
//

#define BACKPATCH_FLAG      1
#define PROMOTE_CHAIN_FLAG  2

NESTED_ENTRY ResolveWorkerChainLookupAsmStub, _TEXT, NoHandler
    andi  $t4, $t8, BACKPATCH_FLAG     // First we check if t8 has the BACKPATCH_FLAG set
    bne  $t4, $zero, LOCAL_LABEL(Fail) // If the BACKPATCH_FLAGS is set we will go directly to the ResolveWorkerAsmStub

    ld.d  $t3, $a0, 0         // retrieve the MethodTable from the object ref in $a0
LOCAL_LABEL(MainLoop):
    ld.d  $t1, $t1, ResolveCacheElem__pNext     // $t1 <= the next entry in the chain
    beq  $t1, $zero, LOCAL_LABEL(Fail)

    ld.d  $t4, $t1, 0
    // compare our MT with the one in the ResolveCacheElem
    bne  $t4, $t3, LOCAL_LABEL(MainLoop)

    ld.d  $t4, $t1, 8
    // compare our DispatchToken with one in the ResolveCacheElem
    bne  $t2, $t4, LOCAL_LABEL(MainLoop)

LOCAL_LABEL(Success):
    PREPARE_EXTERNAL_VAR  g_dispatch_cache_chain_success_counter, $t3
    ld.d  $t4, $t3, 0
    addi.d $t4, $t4, -1
    st.d  $t4, $t3, 0
    blt $t4, $zero, LOCAL_LABEL(Promote)

    ld.d  $t4, $t1, ResolveCacheElem__target    // get the ImplTarget
    jirl  $r0, $t4, 0                // branch to interface implementation target

LOCAL_LABEL(Promote):
                          // Move this entry to head position of the chain
    addi.d  $t4, $zero, 256
    st.d  $t4, $t3, 0         // be quick to reset the counter so we don't get a bunch of contending threads
    ori  $t8, $t8, PROMOTE_CHAIN_FLAG   // set PROMOTE_CHAIN_FLAG
    ori  $t2, $t1, 0           // We pass the ResolveCacheElem to ResolveWorkerAsmStub instead of the DispatchToken

LOCAL_LABEL(Fail):
    b       C_FUNC(ResolveWorkerAsmStub) // call the ResolveWorkerAsmStub method to transition into the VM
NESTED_END ResolveWorkerChainLookupAsmStub, _TEXT


//NOTE:  Frame_Size = SIZEOF__ArgumentRegisters + SIZEOF__FloatArgumentRegisters + extra.
//
//     |gp   |
//     |s0   |
//     |$t2   |
//     |t9   |
//     |$a7   |
//     |$a6   |
//     |$a5   |
//     |$a4   |
//     |$a3   |
//     |$a2   |
//     |$a1   |
//     |$a0   |
//     |$ra   | $sp+8
//     |fp   | $sp
//
//     |f19  | if needed.
//     |f18  |
//     |f17  |
//     |f16  |
//     |f15  |
//     |f14  |
//     |f13  |
//     |f12  |
//
//#define UMThunkStub_Offset_t9    0x50
#define UMThunkStub_Offset_Entry 0x58   // offset of saved UMEntryThunk *
#define UMThunkStub_Offset_s0    0x60
#define UMThunkStub_StackArgs    0x70   // Frame size.


// ------------------------------------------------------------------
// void ResolveWorkerAsmStub(args in regs $a0-$a7 & stack, t8:IndirectionCellAndFlags, $t2:DispatchToken)
//
// The stub dispatch thunk which transfers control to VSD_ResolveWorker.
NESTED_ENTRY ResolveWorkerAsmStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    ori  $a2, $t2, 0                 // DispatchToken
    addi.d  $a0, $sp, __PWTB_TransitionBlock        // pTransitionBlock
    srli.d  $a1, $t8, 2
    andi  $a3, $t8, 3              // flag
    slli.d  $a1, $a1, 2
    bl C_FUNC(VSD_ResolveWorker)
    ori  $t4,$a0,0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

    EPILOG_BRANCH_REG  $t4
NESTED_END ResolveWorkerAsmStub, _TEXT

// ------------------------------------------------------------------
// void* JIT_GetSharedNonGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT
    jirl  $r0, $ra, 0
LEAF_END JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT

// ------------------------------------------------------------------
// void* JIT_GetSharedGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT
    ld.d  $a0, $a0, DomainLocalModule__m_pGCStatics
    jirl  $r0, $ra, 0
LEAF_END JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT


#ifdef FEATURE_HIJACK
// ------------------------------------------------------------------
// Hijack function for functions which return a scalar type or a struct (value type)
NESTED_ENTRY OnHijackTripThread, _TEXT, NoHandler
    //                             $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED   22, 1, 0x80

    // Spill callee saved registers
    //                    $s0,$s1
    PROLOG_SAVE_REG_PAIR   23, 24, 16
    PROLOG_SAVE_REG_PAIR   25, 26, 32
    PROLOG_SAVE_REG_PAIR   27, 28, 48
    PROLOG_SAVE_REG_PAIR   29, 30, 64
    PROLOG_SAVE_REG 31, 80

    // save any integral return value(s)
    st.d  $a0, $sp, 96
    st.d  $a1, $sp, 104

    // save any FP/HFA return value(s)
    fst.d  $f0, $sp, 112
    fst.d  $f1, $sp, 120

    ori  $a0, $sp, 0
    bl C_FUNC(OnHijackWorker)

    // restore callee saved registers

    // restore any integral return value(s)
    ld.d  $a0, $sp, 96
    ld.d  $a1, $sp, 104

    // restore any FP/HFA return value(s)
    fst.d  $f0, $sp, 112
    fst.d  $f1, $sp, 120

    EPILOG_RESTORE_REG_PAIR   23, 24, 16
    EPILOG_RESTORE_REG_PAIR   25, 26, 32
    EPILOG_RESTORE_REG_PAIR   27, 28, 48
    EPILOG_RESTORE_REG_PAIR   29, 30, 64
    EPILOG_RESTORE_REG  31, 80
    //                              $fp,$ra
    EPILOG_RESTORE_REG_PAIR_INDEXED  22, 1, 0x80
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
    // $a0 = throwable
    // $a1 = PC to invoke
    // $a2 = address of s0 register in CONTEXT record// used to restore the non-volatile registers of CrawlFrame
    // $a3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    //

    //                            $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED   22, 1, 96, 0

    // Spill callee saved registers
    //                    $s0,$s1
    PROLOG_SAVE_REG_PAIR   23, 24, 16
    PROLOG_SAVE_REG_PAIR   25, 26, 32
    PROLOG_SAVE_REG_PAIR   27, 28, 48
    PROLOG_SAVE_REG_PAIR   29, 30, 64
    //                    $s8,$tp
    PROLOG_SAVE_REG_PAIR   31, 2,  80
    //PROLOG_SAVE_REG 31, 80

    // Save the SP of this function
    st.d  $sp, $a3, 0

    ld.d  $tp, $a2, -168  // offset of tp in PCONTEXT relative to S0.
    ld.d  $fp, $a2, -8  // offset of fp in PCONTEXT relative to S0.
    ld.d  $s0, $a2, 0
    ld.d  $s1, $a2, 8
    ld.d  $s2, $a2, 16
    ld.d  $s3, $a2, 24
    ld.d  $s4, $a2, 32
    ld.d  $s5, $a2, 40
    ld.d  $s6, $a2, 48
    ld.d  $s7, $a2, 56
    ld.d  $s8, $a2, 64  // offset of fp in PCONTEXT relative to S0.

    // Invoke the funclet
    jirl $ra, $a1, 0

    EPILOG_RESTORE_REG_PAIR   23, 24, 16
    EPILOG_RESTORE_REG_PAIR   25, 26, 32
    EPILOG_RESTORE_REG_PAIR   27, 28, 48
    EPILOG_RESTORE_REG_PAIR   29, 30, 64
    EPILOG_RESTORE_REG_PAIR   31,  2, 80
    //EPILOG_RESTORE_REG  31, 80
    //                              $fp,$ra
    EPILOG_RESTORE_REG_PAIR_INDEXED   22, 1, 96
    EPILOG_RETURN
NESTED_END CallEHFunclet, _TEXT

// This helper enables us to call into a filter funclet by passing it the CallerSP to lookup the
// frame pointer for accessing the locals in the parent method.
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler
    //                             $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED   22, 1, 16

    // On entry:
    //
    // $a0 = throwable
    // $a1 = SP of the caller of the method/funclet containing the filter
    // $a2 = PC to invoke
    // $a3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    //
    // Save the SP of this function
    st.d $fp, $a3, 0
    // Invoke the filter funclet
    jirl  $ra, $a2, 0

    EPILOG_RESTORE_REG_PAIR_INDEXED   22, 1, 16
    EPILOG_RETURN
NESTED_END CallEHFilterFunclet, _TEXT

#ifdef FEATURE_COMINTEROP
// Function used by COM interop to get floating point return value (since it's not in the same
// register(s) as non-floating point values).
//
// On entry//
//   $a0          : size of the FP result (4 or 8 bytes)
//   $a1          : pointer to 64-bit buffer to receive result
//
// On exit:
//   buffer pointed to by $a1 on entry contains the float or double argument as appropriate
//
LEAF_ENTRY getFPReturn, _TEXT
    fst.d  $f0, $a1, 0
LEAF_END getFPReturn, _TEXT

// ------------------------------------------------------------------
// Function used by COM interop to set floating point return value (since it's not in the same
// register(s) as non-floating point values).
//
// On entry:
//   $a0          : size of the FP result (4 or 8 bytes)
//   $a1          : 32-bit or 64-bit FP result
//
// On exit:
//   f0          : float result if x0 == 4
//   f0          : double result if x0 == 8
//
LEAF_ENTRY setFPReturn, _TEXT
    movgr2fr.d  $f0, $a1
LEAF_END setFPReturn, _TEXT

#endif // FEATURE_COMINTEROP

//
// JIT Static access helpers when coreclr host specifies single appdomain flag
//

// ------------------------------------------------------------------
// void* JIT_GetSharedNonGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT
    // If class is not initialized, bail to C++ helper
    //dext $a1, $a1, 0, 32
    addi.d  $a2, $a0, DomainLocalModule__m_pDataBlob

    ldx.b $a2, $a2, $a1
    andi  $t4, $a2, 1
    beq  $t4, $zero, LOCAL_LABEL(JIT_GetSharedNonGCStaticBase_SingleAppDomain_CallHelper)

    jirl  $r0, $ra, 0

LOCAL_LABEL(JIT_GetSharedNonGCStaticBase_SingleAppDomain_CallHelper):
    // Tail call JIT_GetSharedNonGCStaticBase_Helper
    b  JIT_GetSharedNonGCStaticBase_Helper
LEAF_END JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT

#ifdef FEATURE_READYTORUN

NESTED_ENTRY DelayLoad_MethodCall_FakeProlog, _TEXT, NoHandler
C_FUNC(DelayLoad_MethodCall):
    .global C_FUNC(DelayLoad_MethodCall)
    PROLOG_WITH_TRANSITION_BLOCK

    ori $a1, $t8, 0      // Indirection cell
    ori $a2, $t0, 0      // sectionIndex
    ori $a3, $t1, 0      // Module*

    addi.d  $a0, $sp, __PWTB_TransitionBlock        // pTransitionBlock
    bl C_FUNC(ExternalMethodFixupWorker)
    ori  $t4,$a0,0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    PATCH_LABEL ExternalMethodFixupPatchLabel
    EPILOG_BRANCH_REG   $t4
NESTED_END DelayLoad_MethodCall_FakeProlog, _TEXT


.macro DynamicHelper frameFlags, suffix
NESTED_ENTRY DelayLoad_Helper\suffix\()_FakeProlog, _TEXT, NoHandler
DelayLoad_Helper\suffix:
    .global DelayLoad_Helper\suffix

    PROLOG_WITH_TRANSITION_BLOCK

    //DynamicHelperWorker(TransitionBlock * pTransitionBlock, TADDR * pCell,
    //                    DWORD sectionIndex, Module * pModule, INT frameFlags)
    ori  $a1, $t8, 0      // Indirection cell
    ori  $a2, $t0, 0      // sectionIndex
    ori  $a3, $t1, 0      // Module*
    ori  $a4, $r0, \frameFlags

    addi.d  $a0, $sp, __PWTB_TransitionBlock        // pTransitionBlock
    bl  DynamicHelperWorker

    bne $a0, $r0, LOCAL_LABEL(FakeProlog\suffix\()_0)

    ld.d  $a0, $sp, __PWTB_ArgumentRegisters
    EPILOG_WITH_TRANSITION_BLOCK_RETURN
LOCAL_LABEL(FakeProlog\suffix\()_0):
    ori  $t4,$a0,0
    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG  $t4

NESTED_END DelayLoad_Helper\suffix\()_FakeProlog, _TEXT
.endm

DynamicHelper DynamicHelperFrameFlags_Default
DynamicHelper DynamicHelperFrameFlags_ObjectArg, _Obj
DynamicHelper DynamicHelperFrameFlags_ObjectArg | DynamicHelperFrameFlags_ObjectArg2, _ObjObj
#endif


#ifdef PROFILING_SUPPORTED

// ------------------------------------------------------------------
LEAF_ENTRY JIT_ProfilerEnterLeaveTailcallStub, _TEXT
    jirl  $r0, $ra, 0
LEAF_END JIT_ProfilerEnterLeaveTailcallStub, _TEXT

// ------------------------------------------------------------------
.macro GenerateProfileHelper helper, flags
NESTED_ENTRY \helper\()Naked, _TEXT, NoHandler
    // On entry:
    //   $t1 = functionIDOrClientID
    //   $t2 = profiledSp
    //   $t3 = throwable
    //
    // On exit:
    //   Values of $a0-$a7, $fa0-$fa7, $fp are preserved.
    //   Values of other volatile registers are not preserved.

    //                          $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED 22, 1, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA // Allocate space and save Fp, Pc.

    SAVE_ARGUMENT_REGISTERS       $sp, PROFILE_PLATFORM_SPECIFIC_DATA__argumentRegisters
    SAVE_FLOAT_ARGUMENT_REGISTERS $sp, PROFILE_PLATFORM_SPECIFIC_DATA__floatArgumentRegisters
    st.d    $zero, $sp, PROFILE_PLATFORM_SPECIFIC_DATA__functionId
    addi.d  $t3, $sp, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA // Compute probeSp - initial value of Sp on entry to the helper.
    st.d    $t3, $sp, PROFILE_PLATFORM_SPECIFIC_DATA__probeSp
    st.d    $t2, $sp, PROFILE_PLATFORM_SPECIFIC_DATA__profiledSp

    st.d    $zero, $sp, PROFILE_PLATFORM_SPECIFIC_DATA__hiddenArg
    addi.w  $t3, $zero, \flags
    st.w    $t3, $sp, PROFILE_PLATFORM_SPECIFIC_DATA__flags

    ori     $a0, $t1, 0
    ori     $a1, $sp, 0
    bl      C_FUNC(\helper)

    RESTORE_ARGUMENT_REGISTERS       $sp, PROFILE_PLATFORM_SPECIFIC_DATA__argumentRegisters
    RESTORE_FLOAT_ARGUMENT_REGISTERS $sp, PROFILE_PLATFORM_SPECIFIC_DATA__floatArgumentRegisters
    //                             $fp, $ra
    EPILOG_RESTORE_REG_PAIR_INDEXED 22, 1, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA
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

    addi.d     $a0, $sp, __PWTB_TransitionBlock // TransitionBlock *
    ori     $a1, $t1, 0 // stub-identifying token
    bl      C_FUNC(OnCallCountThresholdReached)
    ori     $t4,$a0,0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG $t4
NESTED_END OnCallCountThresholdReachedStub, _TEXT

#endif // FEATURE_TIERED_COMPILATION

// ------------------------------------------------------------------
// size_t GetThreadStaticsVariableOffset()

// Helper to calculate the offset of native thread local variable `t_ThreadStatics` in TCB. The offset has to be found at runtime
// once linker does its relocation and fixup of thread locals. The offset, after calculation is returned in `$a0` register.

LEAF_ENTRY GetThreadStaticsVariableOffset, _TEXT
        PROLOG_SAVE_REG_PAIR_INDEXED   22, 1, 16
        la.tls.ie   $a0, t_ThreadStatics
        EPILOG_RESTORE_REG_PAIR_INDEXED 22, 1, 16
        EPILOG_RETURN
LEAF_END GetThreadStaticsVariableOffset, _TEXT
// ------------------------------------------------------------------
