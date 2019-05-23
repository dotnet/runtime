// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "asmconstants.h"
#include "unixasmmacros.inc"

// LPVOID __stdcall GetCurrentIP(void)//
LEAF_ENTRY GetCurrentIP, _TEXT
    mov x0, lr
    ret lr
LEAF_END GetCurrentIP, _TEXT

// LPVOID __stdcall GetCurrentSP(void)//
LEAF_ENTRY GetCurrentSP, _TEXT
    mov x0, sp
    ret lr
LEAF_END GetCurrentSP, _TEXT

//-----------------------------------------------------------------------------
// This routine captures the machine state. It is used by helper method frame
//-----------------------------------------------------------------------------
//void LazyMachStateCaptureState(struct LazyMachState *pState)//
LEAF_ENTRY LazyMachStateCaptureState, _TEXT
    // marks that this is not yet valid
    mov w1, #0
    str w1, [x0, #MachState__isValid]

    str lr, [x0, #LazyMachState_captureIp]

    // str instruction does not save sp register directly so move to temp register
    mov x1, sp
    str x1, [x0, #LazyMachState_captureSp]

    // save non-volatile registers that can contain object references
    add x1, x0, #LazyMachState_captureX19_X29
    stp x19, x20, [x1, #(16*0)]
    stp x21, x22, [x1, #(16*1)]
    stp x23, x24, [x1, #(16*2)]
    stp x25, x26, [x1, #(16*3)]
    stp x27, x28, [x1, #(16*4)]
    str x29, [x1, #(16*5)]

    ret lr
LEAF_END LazyMachStateCaptureState, _TEXT

//
// If a preserved register were pushed onto the stack between
// the managed caller and the H_M_F, ptrX19_X29 will point to its
// location on the stack and it would have been updated on the
// stack by the GC already and it will be popped back into the
// appropriate register when the appropriate epilog is run.
//
// Otherwise, the register is preserved across all the code
// in this HCALL or FCALL, so we need to update those registers
// here because the GC will have updated our copies in the
// frame.
//
// So, if ptrX19_X29 points into the MachState, we need to update
// the register here.  That's what this macro does.
//
.macro RestoreRegMS regIndex, reg
    // Incoming:
    //
    // x0 = address of MachState
    //
    // $regIndex: Index of the register (x19-x28). For x19, index is 19.
    //For x20, index is 20, and so on.
    //
    // $reg: Register name (e.g. x19, x20, etc)
    //
    // Get the address of the specified captured register from machine state
    add x2, x0, #(MachState__captureX19_X29 + ((\regIndex-19)*8))

    // Get the content of specified preserved register pointer from machine state
    ldr x3, [x0, #(MachState__ptrX19_X29 + ((\regIndex-19)*8))]

    cmp x2, x3
    bne LOCAL_LABEL(NoRestore_\reg)
    ldr \reg, [x2]
LOCAL_LABEL(NoRestore_\reg):

.endmacro

// EXTERN_C int __fastcall HelperMethodFrameRestoreState(
// INDEBUG_COMMA(HelperMethodFrame *pFrame)
// MachState *pState
// )
LEAF_ENTRY HelperMethodFrameRestoreState, _TEXT

    #ifdef _DEBUG
    mov x0, x1
    #endif

    // If machine state is invalid, then simply exit
    ldr w1, [x0, #MachState__isValid]
    cmp w1, #0
    beq LOCAL_LABEL(Done)

    RestoreRegMS 19, X19
    RestoreRegMS 20, X20
    RestoreRegMS 21, X21
    RestoreRegMS 22, X22
    RestoreRegMS 23, X23
    RestoreRegMS 24, X24
    RestoreRegMS 25, X25
    RestoreRegMS 26, X26
    RestoreRegMS 27, X27
    RestoreRegMS 28, X28
    RestoreRegMS 29, X29
LOCAL_LABEL(Done):
    // Its imperative that the return value of HelperMethodFrameRestoreState is zero
    // as it is used in the state machine to loop until it becomes zero.
    // Refer to HELPER_METHOD_FRAME_END macro for details.
    mov x0,#0
    ret lr

LEAF_END HelperMethodFrameRestoreState, _TEXT

// ------------------------------------------------------------------
// The call in ndirect import precode points to this function.
NESTED_ENTRY NDirectImportThunk, _TEXT, NoHandler

    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -224
    SAVE_ARGUMENT_REGISTERS sp, 16
    SAVE_FLOAT_ARGUMENT_REGISTERS sp, 96

    mov x0, x12
    bl NDirectImportWorker
    mov x12, x0

    // pop the stack and restore original register state
    RESTORE_FLOAT_ARGUMENT_REGISTERS sp, 96
    RESTORE_ARGUMENT_REGISTERS sp, 16
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 224

    // If we got back from NDirectImportWorker, the MD has been successfully
    // linked. Proceed to execute the original DLL call.
    EPILOG_BRANCH_REG x12

NESTED_END NDirectImportThunk, _TEXT

// ------------------------------------------------------------------
// The call in fixup precode initally points to this function.
// The pupose of this function is to load the MethodDesc and forward the call to prestub.
NESTED_ENTRY PrecodeFixupThunk, _TEXT, NoHandler
    // x12 = FixupPrecode *
    // On Exit
    // x12 = MethodDesc*
    // x13, x14 Trashed
    // Inline computation done by FixupPrecode::GetMethodDesc()
    ldrb    w13, [x12, #Offset_PrecodeChunkIndex]    //m_PrecodeChunkIndex
    ldrb    w14, [x12, #Offset_MethodDescChunkIndex] // m_MethodDescChunkIndex

    add     x12, x12, w13, uxtw #FixupPrecode_ALIGNMENT_SHIFT_1
    add     x13, x12, w13, uxtw #FixupPrecode_ALIGNMENT_SHIFT_2
    ldr     x13, [x13, #SIZEOF__FixupPrecode]
    add     x12, x13, w14, uxtw #MethodDesc_ALIGNMENT_SHIFT

    b ThePreStub
NESTED_END PrecodeFixupThunk, _TEXT
// ------------------------------------------------------------------

NESTED_ENTRY ThePreStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    add x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
    mov x1, METHODDESC_REGISTER // pMethodDesc

    bl  PreStubWorker

    mov x9, x0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG  x9

NESTED_END ThePreStub, _TEXT

// ------------------------------------------------------------------
// ThePreStubPatch()

LEAF_ENTRY ThePreStubPatch, _TEXT
    nop
.globl C_FUNC(ThePreStubPatchLabel)
C_FUNC(ThePreStubPatchLabel):
    ret lr
LEAF_END ThePreStubPatch, _TEXT


//-----------------------------------------------------------------------------
// The following Macros help in WRITE_BARRIER Implemetations
// WRITE_BARRIER_ENTRY
//
// Declare the start of a write barrier function. Use similarly to NESTED_ENTRY. This is the only legal way
// to declare a write barrier function.
//
.macro WRITE_BARRIER_ENTRY name
    LEAF_ENTRY \name, _TEXT
.endmacro

// WRITE_BARRIER_END
//
// The partner to WRITE_BARRIER_ENTRY, used like NESTED_END.
//
.macro WRITE_BARRIER_END name
    LEAF_END_MARKED \name, _TEXT
.endmacro

// ------------------------------------------------------------------
// Start of the writeable code region
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
    ret  lr
LEAF_END JIT_PatchedCodeStart, _TEXT

// void JIT_UpdateWriteBarrierState(bool skipEphemeralCheck)
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
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -16

    // x0-x7 will contain intended new state
    // x8 will preserve skipEphemeralCheck
    // x12 will be used for pointers

    mov x8, x0

    PREPARE_EXTERNAL_VAR g_card_table, x12
    ldr  x0, [x12]

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    PREPARE_EXTERNAL_VAR g_card_bundle_table, x12
    ldr  x1, [x12]
#endif

#ifdef WRITE_BARRIER_CHECK
    PREPARE_EXTERNAL_VAR g_GCShadow, x12
    ldr  x2, [x12]
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    PREPARE_EXTERNAL_VAR g_sw_ww_table, x12
    ldr  x3, [x12]
#endif

    PREPARE_EXTERNAL_VAR g_ephemeral_low, x12
    ldr  x4, [x12]

    PREPARE_EXTERNAL_VAR g_ephemeral_high, x12
    ldr  x5, [x12]

    cbz  x8, LOCAL_LABEL(EphemeralCheckEnabled)
    movz x4, #0
    movn x5, #0
LOCAL_LABEL(EphemeralCheckEnabled):

    PREPARE_EXTERNAL_VAR g_lowest_address, x12
    ldr  x6, [x12]

    PREPARE_EXTERNAL_VAR g_highest_address, x12
    ldr  x7, [x12]

    // Update wbs state
    adr  x12, LOCAL_LABEL(wbs_begin)

    stp  x0, x1, [x12], 16
    stp  x2, x3, [x12], 16
    stp  x4, x5, [x12], 16
    stp  x6, x7, [x12], 16

    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 16
    EPILOG_RETURN

    // Begin patchable literal pool
    .balign 64  // Align to power of two at least as big as patchable literal pool so that it fits optimally in cache line
LOCAL_LABEL(wbs_begin):
LOCAL_LABEL(wbs_card_table):
    .quad 0
LOCAL_LABEL(wbs_card_bundle_table):
    .quad 0
LOCAL_LABEL(wbs_GCShadow):
    .quad 0
LOCAL_LABEL(wbs_sw_ww_table):
    .quad 0
LOCAL_LABEL(wbs_ephemeral_low):
    .quad 0
LOCAL_LABEL(wbs_ephemeral_high):
    .quad 0
LOCAL_LABEL(wbs_lowest_address):
    .quad 0
LOCAL_LABEL(wbs_highest_address):
    .quad 0
WRITE_BARRIER_END JIT_UpdateWriteBarrierState


// ------------------------------------------------------------------
// End of the writeable code region
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
    ret  lr
LEAF_END JIT_PatchedCodeLast, _TEXT

// void JIT_ByRefWriteBarrier
// On entry:
//   x13  : the source address (points to object reference to write)
//   x14  : the destination address (object reference written here)
//
// On exit:
//   x12  : trashed
//   x13  : incremented by 8
//   x14  : incremented by 8
//   x15  : trashed
//   x17  : trashed (ip1) if FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
//
WRITE_BARRIER_ENTRY JIT_ByRefWriteBarrier

    ldr  x15, [x13], 8
    b C_FUNC(JIT_CheckedWriteBarrier)

WRITE_BARRIER_END JIT_ByRefWriteBarrier 

//-----------------------------------------------------------------------------
// Simple WriteBarriers
// void JIT_CheckedWriteBarrier(Object** dst, Object* src)
// On entry:
//   x14  : the destination address (LHS of the assignment)
//   x15  : the object reference (RHS of the assignment)
//
// On exit:
//   x12  : trashed
//   x14  : trashed (incremented by 8 to implement JIT_ByRefWriteBarrier contract)
//   x15  : trashed
//   x17  : trashed (ip1) if FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
//
WRITE_BARRIER_ENTRY JIT_CheckedWriteBarrier
    ldr  x12,  LOCAL_LABEL(wbs_lowest_address)
    cmp  x14,  x12

    ldr  x12, LOCAL_LABEL(wbs_highest_address)

    // Compare against the upper bound if the previous comparison indicated
    // that the destination address is greater than or equal to the lower
    // bound. Otherwise, set the C flag (specified by the 0x2) so that the
    // branch below is not taken.
    ccmp x14, x12, #0x2, hs

    blo  C_FUNC(JIT_WriteBarrier)

LOCAL_LABEL(NotInHeap):
    str  x15, [x14], 8
    ret  lr
WRITE_BARRIER_END JIT_CheckedWriteBarrier

// void JIT_WriteBarrier(Object** dst, Object* src)
// On entry:
//   x14  : the destination address (LHS of the assignment)
//   x15  : the object reference (RHS of the assignment)
//
// On exit:
//   x12  : trashed
//   x14  : trashed (incremented by 8 to implement JIT_ByRefWriteBarrier contract)
//   x15  : trashed
//   x17  : trashed (ip1) if FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
//
WRITE_BARRIER_ENTRY JIT_WriteBarrier
    stlr  x15, [x14]

#ifdef WRITE_BARRIER_CHECK
    // Update GC Shadow Heap

    // Do not perform the work if g_GCShadow is 0
    ldr  x12, LOCAL_LABEL(wbs_GCShadow)
    cbz  x12, LOCAL_LABEL(ShadowUpdateDisabled)

    // need temporary register. Save before using.
    str  x13, [sp, #-16]!

    // Compute address of shadow heap location:
    //   pShadow = g_GCShadow + (x14 - g_lowest_address)
    ldr  x13, LOCAL_LABEL(wbs_lowest_address)
    sub  x13, x14, x13
    add  x12, x13, x12

    // if (pShadow >= g_GCShadowEnd) goto end
    PREPARE_EXTERNAL_VAR g_GCShadowEnd, x13
    ldr  x13, [x13]
    cmp  x12, x13
    bhs  LOCAL_LABEL(ShadowUpdateEnd)

    // *pShadow = x15
    str  x15, [x12]

    // Ensure that the write to the shadow heap occurs before the read from the GC heap so that race
    // conditions are caught by INVALIDGCVALUE.
    dmb  ish

    // if ([x14] == x15) goto end
    ldr  x13, [x14]
    cmp  x13, x15
    beq LOCAL_LABEL(ShadowUpdateEnd)

    // *pShadow = INVALIDGCVALUE (0xcccccccd)
    movz x13, #0xcccd
    movk x13, #0xcccc, LSL #16
    str  x13, [x12]

LOCAL_LABEL(ShadowUpdateEnd):
    ldr  x13, [sp], #16
LOCAL_LABEL(ShadowUpdateDisabled):
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    // Update the write watch table if necessary
    ldr  x12, LOCAL_LABEL(wbs_sw_ww_table)
    cbz  x12, LOCAL_LABEL(CheckCardTable)
    add  x12, x12, x14, lsr #0xc  // SoftwareWriteWatch::AddressToTableByteIndexShift
    ldrb w17, [x12]
    cbnz x17, LOCAL_LABEL(CheckCardTable)
    mov  w17, #0xFF
    strb w17, [x12]
#endif

LOCAL_LABEL(CheckCardTable):
    // Branch to Exit if the reference is not in the Gen0 heap
    //
    ldr  x12, LOCAL_LABEL(wbs_ephemeral_low)
    cbz  x12, LOCAL_LABEL(SkipEphemeralCheck)
    cmp  x15,  x12

    ldr  x12, LOCAL_LABEL(wbs_ephemeral_high)

    // Compare against the upper bound if the previous comparison indicated
    // that the destination address is greater than or equal to the lower
    // bound. Otherwise, set the C flag (specified by the 0x2) so that the
    // branch to exit is taken.
    ccmp x15, x12, #0x2, hs

    bhi  LOCAL_LABEL(Exit)

LOCAL_LABEL(SkipEphemeralCheck):
    // Check if we need to update the card table
    ldr  x12, LOCAL_LABEL(wbs_card_table)
    add  x15, x12, x14, lsr #11
    ldrb w12, [x15]
    cmp  x12, 0xFF
    beq  LOCAL_LABEL(Exit)

LOCAL_LABEL(UpdateCardTable):
    mov  x12, 0xFF 
    strb w12, [x15]

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    // Check if we need to update the card bundle table
    ldr  x12, LOCAL_LABEL(wbs_card_bundle_table)
    add  x15, x12, x14, lsr #21
    ldrb w12, [x15]
    cmp  x12, 0xFF
    beq  LOCAL_LABEL(Exit)

LOCAL_LABEL(UpdateCardBundle):
    mov  x12, 0xFF
    strb w12, [x15]
#endif

LOCAL_LABEL(Exit):
    add  x14, x14, 8
    ret  lr  
WRITE_BARRIER_END JIT_WriteBarrier

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
// x0 = "this" pointer
// x12 = Address of thunk

NESTED_ENTRY VirtualMethodFixupStub, _TEXT, NoHandler

    // Save arguments and return address
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -224
    SAVE_ARGUMENT_REGISTERS sp, 16
    SAVE_FLOAT_ARGUMENT_REGISTERS sp, 96

    // Refer to ZapImportVirtualThunk::Save
    // for details on this.
    //
    // Move the thunk start address in x1
    mov x1, x12

    // Call the helper in the VM to perform the actual fixup
    // and tell us where to tail call. x0 already contains
    // the this pointer.
    bl C_FUNC(VirtualMethodFixupWorker)
    // On return, x0 contains the target to tailcall to
    mov x12, x0

    // pop the stack and restore original register state
    RESTORE_ARGUMENT_REGISTERS sp, 16
    RESTORE_FLOAT_ARGUMENT_REGISTERS sp, 96
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 224

    PATCH_LABEL VirtualMethodFixupPatchLabel

    // and tailcall to the actual method
    EPILOG_BRANCH_REG x12

NESTED_END VirtualMethodFixupStub, _TEXT
#endif // FEATURE_PREJIT

//------------------------------------------------
// ExternalMethodFixupStub
//
// In NGEN images, calls to cross-module external methods initially
// point to a jump thunk that calls into the following function that will
// call into a VM helper. The VM helper is responsible for patching up the
// thunk, upon executing the precode, so that all subsequent calls go directly
// to the actual method body.
//
// This is done lazily for performance reasons.
//
// On entry:
//
// x12 = Address of thunk 

NESTED_ENTRY ExternalMethodFixupStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    add x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
    mov x1, x12 // pThunk

    bl C_FUNC(ExternalMethodFixupWorker)

    // mov the address we patched to in x12 so that we can tail call to it
    mov x12, x0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    PATCH_LABEL ExternalMethodFixupPatchLabel
    EPILOG_BRANCH_REG   x12

NESTED_END ExternalMethodFixupStub, _TEXT

// void SinglecastDelegateInvokeStub(Delegate *pThis)
LEAF_ENTRY SinglecastDelegateInvokeStub, _TEXT
    cmp x0, #0
    beq LOCAL_LABEL(LNullThis)

    ldr x16, [x0, #DelegateObject___methodPtr]
    ldr x0, [x0, #DelegateObject___target]

    br x16

LOCAL_LABEL(LNullThis):
    mov x0, #CORINFO_NullReferenceException_ASM
    b C_FUNC(JIT_InternalThrow)

LEAF_END SinglecastDelegateInvokeStub, _TEXT

#ifdef FEATURE_COMINTEROP

#define ComCallPreStub_FrameSize (SIZEOF__GSCookie + SIZEOF__ComMethodFrame)
#define ComCallPreStub_FirstStackAdjust  (8 + SIZEOF__ArgumentRegisters + 2 * 8) // x8, reg args , fp & lr already pushed
#define ComCallPreStub_StackAlloc0        (ComCallPreStub_FrameSize - ComCallPreStub_FirstStackAdjust)
#define ComCallPreStub_StackAlloc1        (ComCallPreStub_StackAlloc0 + SIZEOF__FloatArgumentRegisters + 8)// 8 for ErrorReturn
#define ComCallPreStub_StackAlloc    (ComCallPreStub_StackAlloc1 + (ComCallPreStub_StackAlloc1 & 8))

#define ComCallPreStub_FrameOffset   (ComCallPreStub_StackAlloc - (SIZEOF__ComMethodFrame - ComCallPreStub_FirstStackAdjust))
#define ComCallPreStub_ErrorReturnOffset0 SIZEOF__FloatArgumentRegisters

#define ComCallPreStub_FirstStackAdjust (ComCallPreStub_ErrorReturnOffset0 + (ComCallPreStub_ErrorReturnOffset0 & 8))

// ------------------------------------------------------------------
// COM to CLR stub called the first time a particular method is invoked.//
//
// On entry:
//   x12         : ComCallMethodDesc* provided by prepad thunk
//   plus user arguments in registers and on the stack
//
// On exit:
//   tail calls to real method
//
NESTED_ENTRY ComCallPreStub, _TEXT, NoHandler

    // Save arguments and return address
    PROLOG_SAVE_REG_PAIR           fp, lr, -ComCallPreStub_FirstStackAdjust!
    PROLOG_STACK_ALLOC  ComCallPreStub_StackAlloc

    SAVE_ARGUMENT_REGISTERS        sp, (16+ComCallPreStub_StackAlloc)

    SAVE_FLOAT_ARGUMENT_REGISTERS  sp, 0

    str x12, [sp, #(ComCallPreStub_FrameOffset + UnmanagedToManagedFrame__m_pvDatum)]
    add x0, sp, #(ComCallPreStub_FrameOffset)
    add x1, sp, #(ComCallPreStub_ErrorReturnOffset)
    bl ComPreStubWorker

    cbz x0, ComCallPreStub_ErrorExit

    mov x12, x0

    // pop the stack and restore original register state
    RESTORE_FLOAT_ARGUMENT_REGISTERS  sp, 0
    RESTORE_ARGUMENT_REGISTERS        sp, (16+ComCallPreStub_StackAlloc)

    EPILOG_STACK_FREE ComCallPreStub_StackAlloc
    EPILOG_RESTORE_REG_PAIR           fp, lr, ComCallPreStub_FirstStackAdjust!

    // and tailcall to the actual method
    EPILOG_BRANCH_REG x12

ComCallPreStub_ErrorExit
    ldr x0, [sp, #(ComCallPreStub_ErrorReturnOffset)] // ErrorReturn

    // pop the stack
    EPILOG_STACK_FREE ComCallPreStub_StackAlloc
    EPILOG_RESTORE_REG_PAIR           fp, lr, ComCallPreStub_FirstStackAdjust!

    EPILOG_RETURN

NESTED_END ComCallPreStub, _TEXT

// ------------------------------------------------------------------
// COM to CLR stub which sets up a ComMethodFrame and calls COMToCLRWorker.
//
// On entry:
//   x12         : ComCallMethodDesc*  provided by prepad thunk
//   plus user arguments in registers and on the stack
//
// On exit:
//   Result in x0/d0 as per the real method being called
//
    NESTED_ENTRY GenericComCallStub, _TEXT, NoHandler

    // Save arguments and return address
    PROLOG_SAVE_REG_PAIR           fp, lr, -GenericComCallStub_FirstStackAdjust!
    PROLOG_STACK_ALLOC  GenericComCallStub_StackAlloc

    SAVE_ARGUMENT_REGISTERS        sp, (16+GenericComCallStub_StackAlloc)
    SAVE_FLOAT_ARGUMENT_REGISTERS  sp, 0

    str x12, [sp, #(GenericComCallStub_FrameOffset + UnmanagedToManagedFrame__m_pvDatum)]
    add x1, sp, #GenericComCallStub_FrameOffset
    bl COMToCLRWorker

    // pop the stack
    EPILOG_STACK_FREE GenericComCallStub_StackAlloc
    EPILOG_RESTORE_REG_PAIR           fp, lr, GenericComCallStub_FirstStackAdjust!

    EPILOG_RETURN

    NESTED_END GenericComCallStub, _TEXT

// ------------------------------------------------------------------
// COM to CLR stub called from COMToCLRWorker that actually dispatches to the real managed method.
//
// On entry:
//   x0          : dwStackSlots, count of argument stack slots to copy
//   x1          : pFrame, ComMethodFrame pushed by GenericComCallStub above
//   x2          : pTarget, address of code to call
//   x3          : pSecretArg, hidden argument passed to target above in x12
//   x4          : pDangerousThis, managed 'this' reference
//
// On exit:
//   Result in x0/d0 as per the real method being called
//
    NESTED_ENTRY COMToCLRDispatchHelper, _TEXT,CallDescrWorkerUnwindFrameChainHandler

    PROLOG_SAVE_REG_PAIR           fp, lr, -16!

    cbz x0, COMToCLRDispatchHelper_RegSetup

    add x9, x1, #SIZEOF__ComMethodFrame
    add x9, x9, x0, LSL #3
COMToCLRDispatchHelper_StackLoop
    ldr x8, [x9, #-8]!
    str x8, [sp, #-8]!
    sub x0, x0, #1
    cbnz x0, COMToCLRDispatchHelper_StackLoop

COMToCLRDispatchHelper_RegSetup

    RESTORE_FLOAT_ARGUMENT_REGISTERS x1, -1 * GenericComCallStub_FrameOffset

    mov lr, x2
    mov x12, x3

    mov x0, x4

    ldp x2, x3, [x1, #(SIZEOF__ComMethodFrame - SIZEOF__ArgumentRegisters + 16)]
    ldp x4, x5, [x1, #(SIZEOF__ComMethodFrame - SIZEOF__ArgumentRegisters + 32)]
    ldp x6, x7, [x1, #(SIZEOF__ComMethodFrame - SIZEOF__ArgumentRegisters + 48)]
    ldr x8, [x1, #(SIZEOF__ComMethodFrame - SIZEOF__ArgumentRegisters - 8)]

    ldr x1, [x1, #(SIZEOF__ComMethodFrame - SIZEOF__ArgumentRegisters + 8)]

    blr lr

    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR           fp, lr, 16!
    EPILOG_RETURN

    NESTED_END COMToCLRDispatchHelper, _TEXT

#endif // FEATURE_COMINTEROP
//
// x12 = UMEntryThunk*
//
NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix

    // Save arguments and return address
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -224
    SAVE_ARGUMENT_REGISTERS sp, 16
    SAVE_FLOAT_ARGUMENT_REGISTERS sp, 96

    mov x0, x12
    bl C_FUNC(TheUMEntryPrestubWorker)

    // save real target address in x12.
    mov x12, x0

    // pop the stack and restore original register state
    RESTORE_ARGUMENT_REGISTERS sp, 16
    RESTORE_FLOAT_ARGUMENT_REGISTERS sp, 96
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 224

    // and tailcall to the actual method
    EPILOG_BRANCH_REG x12

NESTED_END TheUMEntryPrestub, _TEXT

//
// x12 = UMEntryThunk*
//
NESTED_ENTRY UMThunkStub, _TEXT, UnhandledExceptionHandlerUnix

    // Save arguments and return address
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -112 // 72 for regArgs, 8 for x19 & 8 for x12
    // save callee saved reg x19. x19 is used in the method to store thread*
    PROLOG_SAVE_REG x19, 96

    SAVE_ARGUMENT_REGISTERS sp, 16

#define UMThunkStub_HiddenArg 88 // offset of saved UMEntryThunk *
#define UMThunkStub_StackArgs 112 // offset of original stack args (total size of UMThunkStub frame)

    // save UMEntryThunk*
    str x12, [sp, #UMThunkStub_HiddenArg]

    // assuming GetThread does not clobber FP Args
    bl C_FUNC(GetThread)
    cbz x0, LOCAL_LABEL(UMThunkStub_DoThreadSetup)

LOCAL_LABEL(UMThunkStub_HaveThread):
    mov x19, x0  // x19 = Thread *

    mov x9, 1
    // m_fPreemptiveGCDisabled is 4 byte field so using 32-bit variant
    str w9, [x19, #Thread__m_fPreemptiveGCDisabled]

    PREPARE_EXTERNAL_VAR g_TrapReturningThreads, x2
    ldr x3, [x2]
    // assuming x0 contains Thread* before jumping to UMThunkStub_DoTrapReturningThreads
    cbnz x3, LOCAL_LABEL(UMThunkStub_DoTrapReturningThreads)

LOCAL_LABEL(UMThunkStub_InCooperativeMode):
    ldr x12, [fp, #UMThunkStub_HiddenArg] // x12 = UMEntryThunk*
    ldr x3, [x12, #UMEntryThunk__m_pUMThunkMarshInfo] // x3 = m_pUMThunkMarshInfo

    // m_cbActualArgSize is UINT32 and hence occupies 4 bytes
    ldr w2, [x3, #UMThunkMarshInfo__m_cbActualArgSize] // w2 = Stack arg bytes
    cbz w2, LOCAL_LABEL(UMThunkStub_RegArgumentsSetup)

    // extend to 64-bits
    uxtw x2, w2

    // Source pointer
    add x0, fp, #UMThunkStub_StackArgs

    // move source pointer to end of Stack Args
    add x0, x0, x2 

    // Count of stack slot pairs to copy (divide by 16)
    lsr x1, x2, #4

    // Is there an extra stack slot (can happen when stack arg bytes not multiple of 16)
    and x2, x2, #8

    // If yes then start source pointer from 16 byte aligned stack slot
    add x0, x0, x2  

    // increment stack slot pair count by 1 if x2 is not zero
    add x1, x1, x2, LSR #3 

LOCAL_LABEL(UMThunkStub_StackLoop):
    ldp x4, x5, [x0, #-16]! // pre-Index
    stp x4, x5, [sp, #-16]! // pre-Index
    subs x1, x1, #1
    bne LOCAL_LABEL(UMThunkStub_StackLoop)

LOCAL_LABEL(UMThunkStub_RegArgumentsSetup):
    ldr x16, [x3, #UMThunkMarshInfo__m_pILStub]

    RESTORE_ARGUMENT_REGISTERS fp, 16

    blr x16

LOCAL_LABEL(UMThunkStub_PostCall):
    mov x4, 0
    // m_fPreemptiveGCDisabled is 4 byte field so using 32-bit variant
    str w4, [x19, #Thread__m_fPreemptiveGCDisabled]

    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG x19, 96
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr, 112

    EPILOG_RETURN

LOCAL_LABEL(UMThunkStub_DoThreadSetup):
    sub sp, sp, #SIZEOF__FloatArgumentRegisters
    SAVE_FLOAT_ARGUMENT_REGISTERS sp, 0
    bl C_FUNC(CreateThreadBlockThrow)
    RESTORE_FLOAT_ARGUMENT_REGISTERS sp, 0
    add sp, sp, #SIZEOF__FloatArgumentRegisters
    b LOCAL_LABEL(UMThunkStub_HaveThread)

LOCAL_LABEL(UMThunkStub_DoTrapReturningThreads):
    sub sp, sp, #SIZEOF__FloatArgumentRegisters
    SAVE_FLOAT_ARGUMENT_REGISTERS sp, 0
    // x0 already contains Thread* pThread
    // UMEntryThunk* pUMEntry
    ldr x1, [fp, #UMThunkStub_HiddenArg]
    bl C_FUNC(UMThunkStubRareDisableWorker)
    RESTORE_FLOAT_ARGUMENT_REGISTERS sp, 0
    add sp, sp, #SIZEOF__FloatArgumentRegisters
    b LOCAL_LABEL(UMThunkStub_InCooperativeMode)

NESTED_END UMThunkStub, _TEXT

#ifdef FEATURE_HIJACK
// ------------------------------------------------------------------
// Hijack function for functions which return a scalar type or a struct (value type)
NESTED_ENTRY OnHijackTripThread, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -176
    // Spill callee saved registers
    PROLOG_SAVE_REG_PAIR   x19, x20, 16
    PROLOG_SAVE_REG_PAIR   x21, x22, 32
    PROLOG_SAVE_REG_PAIR   x23, x24, 48
    PROLOG_SAVE_REG_PAIR   x25, x26, 64
    PROLOG_SAVE_REG_PAIR   x27, x28, 80

    // save any integral return value(s)
    stp x0, x1, [sp, #96]

    // save any FP/HFA return value(s)
    stp q0, q1, [sp, #112]
    stp q2, q3, [sp, #144]

    mov x0, sp
    bl OnHijackWorker

    // restore any integral return value(s)
    ldp x0, x1, [sp, #96]

    // restore any FP/HFA return value(s)
    ldp q0, q1, [sp, #112]
    ldp q2, q3, [sp, #144]

    EPILOG_RESTORE_REG_PAIR   x19, x20, 16
    EPILOG_RESTORE_REG_PAIR   x21, x22, 32
    EPILOG_RESTORE_REG_PAIR   x23, x24, 48
    EPILOG_RESTORE_REG_PAIR   x25, x26, 64
    EPILOG_RESTORE_REG_PAIR   x27, x28, 80
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr,   176
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
    // X0 = throwable
    // X1 = PC to invoke
    // X2 = address of X19 register in CONTEXT record// used to restore the non-volatile registers of CrawlFrame
    // X3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    //

    // Using below prolog instead of PROLOG_SAVE_REG_PAIR_INDEXED fp,lr, -96
    // is intentional. Above statement would also emit instruction to save
    // sp in fp. If sp is saved in fp in prolog then it is not expected that fp can change in the body
    // of method. However, this method needs to be able to change fp before calling funclet.
    // This is required to access locals in funclet.
    PROLOG_SAVE_REG_PAIR_INDEXED   x29, lr, -96

    // Spill callee saved registers
    PROLOG_SAVE_REG_PAIR   x19, x20, 16
    PROLOG_SAVE_REG_PAIR   x21, x22, 32
    PROLOG_SAVE_REG_PAIR   x23, x24, 48
    PROLOG_SAVE_REG_PAIR   x25, x26, 64
    PROLOG_SAVE_REG_PAIR   x27, x28, 80

    // Save the SP of this function
    mov x4, sp
    str x4, [x3]

    ldp x19, x20, [x2, #0]
    ldp x21, x22, [x2, #16]
    ldp x23, x24, [x2, #32]
    ldp x25, x26, [x2, #48]
    ldp x27, x28, [x2, #64]
    ldr fp, [x2, #80] // offset of fp in CONTEXT relative to X19

    // Invoke the funclet
    blr x1
    nop

    EPILOG_RESTORE_REG_PAIR   x19, x20, 16
    EPILOG_RESTORE_REG_PAIR   x21, x22, 32
    EPILOG_RESTORE_REG_PAIR   x23, x24, 48
    EPILOG_RESTORE_REG_PAIR   x25, x26, 64
    EPILOG_RESTORE_REG_PAIR   x27, x28, 80
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr,   96
    EPILOG_RETURN

NESTED_END CallEHFunclet, _TEXT

// This helper enables us to call into a filter funclet by passing it the CallerSP to lookup the 
// frame pointer for accessing the locals in the parent method.
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler

    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -16

    // On entry:
    //
    // X0 = throwable
    // X1 = SP of the caller of the method/funclet containing the filter
    // X2 = PC to invoke
    // X3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    //
    // Save the SP of this function
    str fp, [x3]
    // Invoke the filter funclet
    blr x2

    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr,   16
    EPILOG_RETURN

NESTED_END CallEHFilterFunclet, _TEXT

#define FaultingExceptionFrame_StackAlloc (SIZEOF__GSCookie + SIZEOF__FaultingExceptionFrame)
#define FaultingExceptionFrame_FrameOffset SIZEOF__GSCookie

.macro GenerateRedirectedStubWithFrame stub, target

    // 
    // This is the primary function to which execution will be redirected to.
    //
    NESTED_ENTRY \stub, _TEXT, NoHandler

        //
        // IN: lr: original IP before redirect
        //

        PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -16
        PROLOG_STACK_ALLOC  FaultingExceptionFrame_StackAlloc

        // At this point, the stack maybe misaligned if the thread abort was asynchronously
        // triggered in the prolog or epilog of the managed method. For such a case, we must
        // align the stack before calling into the VM.
        //
        // Runtime check for 16-byte alignment. 
        mov x0, sp
        and x0, x0, #15
        sub sp, sp, x0

        // Save pointer to FEF for GetFrameFromRedirectedStubStackFrame
        add x19, sp, #FaultingExceptionFrame_FrameOffset

        // Prepare to initialize to NULL
        mov x1,#0
        str x1, [x19]// Initialize vtbl (it is not strictly necessary)
        str x1, [x19, #FaultingExceptionFrame__m_fFilterExecuted]// Initialize BOOL for personality routine

        mov x0, x19   // move the ptr to FEF in X0

        bl C_FUNC(\target)

        // Target should not return.
        EMIT_BREAKPOINT

    NESTED_END \stub, _TEXT

.endmacro


// ------------------------------------------------------------------
// ResolveWorkerChainLookupAsmStub
//
// This method will perform a quick chained lookup of the entry if the
//  initial cache lookup fails.
//
// On Entry:
//   x9        contains the pointer to the current ResolveCacheElem
//   x11       contains the address of the indirection (and the flags in the low two bits)
//   x12       contains our contract the DispatchToken
// Must be preserved:
//   x0        contains the instance object ref that we are making an interface call on
//   x9        Must point to a ResolveCacheElem [For Sanity]
//  [x1-x7]    contains any additional register arguments for the interface method
//
// Loaded from x0
//   x13       contains our type     the MethodTable  (from object ref in x0)
//
// On Exit:
//   x0, [x1-x7] arguments for the interface implementation target
//
// On Exit (to ResolveWorkerAsmStub):
//   x11       contains the address of the indirection and the flags in the low two bits.
//   x12       contains our contract (DispatchToken)
//   x16,x17   will be trashed
//

#define BACKPATCH_FLAG      1
#define PROMOTE_CHAIN_FLAG  2

NESTED_ENTRY ResolveWorkerChainLookupAsmStub, _TEXT, NoHandler

    tst     x11, #BACKPATCH_FLAG    // First we check if x11 has the BACKPATCH_FLAG set
    bne     LOCAL_LABEL(Fail)       // If the BACKPATCH_FLAGS is set we will go directly to the ResolveWorkerAsmStub

    ldr     x13, [x0]         // retrieve the MethodTable from the object ref in x0
LOCAL_LABEL(MainLoop):
    ldr     x9, [x9, #ResolveCacheElem__pNext]     // x9 <= the next entry in the chain
    cmp     x9, #0
    beq     LOCAL_LABEL(Fail)

    ldp     x16, x17, [x9]
    cmp     x16, x13          // compare our MT with the one in the ResolveCacheElem
    bne     LOCAL_LABEL(MainLoop)

    cmp     x17, x12          // compare our DispatchToken with one in the ResolveCacheElem
    bne     LOCAL_LABEL(MainLoop)

LOCAL_LABEL(Success):
    PREPARE_EXTERNAL_VAR g_dispatch_cache_chain_success_counter, x13
    ldr     x16, [x13]
    subs    x16, x16, #1
    str     x16, [x13]
    blt     LOCAL_LABEL(Promote)

    ldr     x16, [x9, #ResolveCacheElem__target]    // get the ImplTarget
    br      x16               // branch to interface implemenation target

LOCAL_LABEL(Promote):
                              // Move this entry to head postion of the chain
    mov     x16, #256
    str     x16, [x13]         // be quick to reset the counter so we don't get a bunch of contending threads
    orr     x11, x11, #PROMOTE_CHAIN_FLAG   // set PROMOTE_CHAIN_FLAG
    mov     x12, x9           // We pass the ResolveCacheElem to ResolveWorkerAsmStub instead of the DispatchToken

LOCAL_LABEL(Fail):
    b       ResolveWorkerAsmStub // call the ResolveWorkerAsmStub method to transition into the VM

NESTED_END ResolveWorkerChainLookupAsmStub, _TEXT

// ------------------------------------------------------------------
// void ResolveWorkerAsmStub(args in regs x0-x7 & stack and possibly retbuf arg in x8, x11:IndirectionCellAndFlags, x12:DispatchToken)
//
// The stub dispatch thunk which transfers control to VSD_ResolveWorker.
NESTED_ENTRY ResolveWorkerAsmStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    add x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
    and x1, x11, #-4 // Indirection cell
    mov x2, x12 // DispatchToken
    and x3, x11, #3 // flag
    bl VSD_ResolveWorker
    mov x9, x0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

    EPILOG_BRANCH_REG  x9

NESTED_END ResolveWorkerAsmStub, _TEXT

#ifdef FEATURE_READYTORUN

NESTED_ENTRY DelayLoad_MethodCall_FakeProlog, _TEXT, NoHandler
DelayLoad_MethodCall:
    .global DelayLoad_MethodCall
    PROLOG_WITH_TRANSITION_BLOCK

    add x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
    mov x1, x11 // Indirection cell
    mov x2, x9 // sectionIndex
    mov x3, x10 // Module*
    bl ExternalMethodFixupWorker
    mov x12, x0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    // Share patch label
    b ExternalMethodFixupPatchLabel

NESTED_END DelayLoad_MethodCall_FakeProlog, _TEXT


.macro DynamicHelper frameFlags, suffix
NESTED_ENTRY DelayLoad_Helper\suffix\()_FakeProlog, _TEXT, NoHandler
DelayLoad_Helper\suffix:
    .global DelayLoad_Helper\suffix

    PROLOG_WITH_TRANSITION_BLOCK

    add x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
    mov x1, x11 // Indirection cell
    mov x2, x9 // sectionIndex
    mov x3, x10 // Module*
    mov x4, \frameFlags
    bl DynamicHelperWorker
    cbnz x0, LOCAL_LABEL(FakeProlog\suffix\()_0)
    ldr x0, [sp, #__PWTB_ArgumentRegister_FirstArg]
    EPILOG_WITH_TRANSITION_BLOCK_RETURN
LOCAL_LABEL(FakeProlog\suffix\()_0):
    mov x12, x0
    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG  x12

NESTED_END DelayLoad_Helper\suffix\()_FakeProlog, _TEXT
.endm

DynamicHelper DynamicHelperFrameFlags_Default
DynamicHelper DynamicHelperFrameFlags_ObjectArg, _Obj
DynamicHelper DynamicHelperFrameFlags_ObjectArg | DynamicHelperFrameFlags_ObjectArg2, _ObjObj
#endif

#ifdef FEATURE_PREJIT
// ------------------------------------------------------------------
// void StubDispatchFixupStub(args in regs x0-x7 & stack and possibly retbuff arg in x8, x11:IndirectionCellAndFlags)
//
// The stub dispatch thunk which transfers control to StubDispatchFixupWorker.
NESTED_ENTRY StubDispatchFixupStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    add x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
    and x1, x11, #-4 // Indirection cell
    mov x2, #0 // sectionIndex
    mov x3, #0 // pModule
    bl C_FUNC(StubDispatchFixupWorker)
    mov x12, x0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    PATCH_LABEL StubDispatchFixupPatchLabel
    EPILOG_BRANCH_REG  x12

NESTED_END StubDispatchFixupStub, _TEXT
#endif

#ifdef FEATURE_COMINTEROP

// Function used by COM interop to get floating point return value (since it's not in the same
// register(s) as non-floating point values).
//
// On entry//
//   x0          : size of the FP result (4 or 8 bytes)
//   x1          : pointer to 64-bit buffer to receive result
//
// On exit:
//   buffer pointed to by x1 on entry contains the float or double argument as appropriate
//
LEAF_ENTRY getFPReturn, _TEXT
    str d0, [x1]
LEAF_END getFPReturn, _TEXT

// ------------------------------------------------------------------
// Function used by COM interop to set floating point return value (since it's not in the same
// register(s) as non-floating point values).
//
// On entry:
//   x0          : size of the FP result (4 or 8 bytes)
//   x1          : 32-bit or 64-bit FP result
//
// On exit:
//   s0          : float result if x0 == 4
//   d0          : double result if x0 == 8
//
LEAF_ENTRY setFPReturn, _TEXT
    fmov d0, x1
LEAF_END setFPReturn, _TEXT
#endif

//
// JIT Static access helpers when coreclr host specifies single appdomain flag 
//

// ------------------------------------------------------------------
// void* JIT_GetSharedNonGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT
    // If class is not initialized, bail to C++ helper
    add x2, x0, #DomainLocalModule__m_pDataBlob
    ldrb w2, [x2, w1, UXTW]
    tst w2, #1
    beq LOCAL_LABEL(JIT_GetSharedNonGCStaticBase_SingleAppDomain_CallHelper)

    ret lr

LOCAL_LABEL(JIT_GetSharedNonGCStaticBase_SingleAppDomain_CallHelper):
    // Tail call JIT_GetSharedNonGCStaticBase_Helper
    b C_FUNC(JIT_GetSharedNonGCStaticBase_Helper)
LEAF_END JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT


// ------------------------------------------------------------------
// void* JIT_GetSharedNonGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT
    ret lr
LEAF_END JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT


// ------------------------------------------------------------------
// void* JIT_GetSharedGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT
    // If class is not initialized, bail to C++ helper
    add x2, x0, #DomainLocalModule__m_pDataBlob
    ldrb w2, [x2, w1, UXTW]
    tst w2, #1
    beq LOCAL_LABEL(JIT_GetSharedGCStaticBase_SingleAppDomain_CallHelper)

    ldr x0, [x0, #DomainLocalModule__m_pGCStatics]
    ret lr

LOCAL_LABEL(JIT_GetSharedGCStaticBase_SingleAppDomain_CallHelper):
    // Tail call Jit_GetSharedGCStaticBase_Helper
    b C_FUNC(JIT_GetSharedGCStaticBase_Helper)
LEAF_END JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT


// ------------------------------------------------------------------
// void* JIT_GetSharedGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT
    ldr x0, [x0, #DomainLocalModule__m_pGCStatics]
    ret lr
LEAF_END JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT

// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_Stelem_Ref(PtrArray* array, unsigned idx, Object* val)
LEAF_ENTRY JIT_Stelem_Ref, _TEXT
    // We retain arguments as they were passed and use x0 == array x1 == idx x2 == val

    // check for null array
    cbz     x0, LOCAL_LABEL(ThrowNullReferenceException)

    // idx bounds check
    ldr     x3,[x0,#ArrayBase__m_NumComponents]
    cmp     x3, x1
    bls     LOCAL_LABEL(ThrowIndexOutOfRangeException)

    // fast path to null assignment (doesn't need any write-barriers)
    cbz     x2, LOCAL_LABEL(AssigningNull)

    // Verify the array-type and val-type matches before writing
    ldr     x12, [x0] // x12 = array MT
    ldr     x3, [x2] // x3 = val->GetMethodTable()
    ldr     x12, [x12, #MethodTable__m_ElementType] // array->GetArrayElementTypeHandle()
    cmp     x3, x12
    beq     C_FUNC(JIT_Stelem_DoWrite)

    // Types didnt match but allow writing into an array of objects
    PREPARE_EXTERNAL_VAR g_pObjectClass, x3
    ldr     x3, [x3]  // x3 = *g_pObjectClass
    cmp     x3, x12   // array type matches with Object*
    beq     C_FUNC(JIT_Stelem_DoWrite)

    // array type and val type do not exactly match. Raise frame and do detailed match
    b       C_FUNC(JIT_Stelem_Ref_NotExactMatch)

LOCAL_LABEL(AssigningNull):
    // Assigning null doesn't need write barrier
    add     x0, x0, x1, LSL #3           // x0 = x0 + (x1 x 8) = array->m_array[idx]
    str     x2, [x0, #PtrArray__m_Array] // array->m_array[idx] = val
    ret

LOCAL_LABEL(ThrowNullReferenceException):
    // Tail call JIT_InternalThrow(NullReferenceException)
    ldr     x0, =CORINFO_NullReferenceException_ASM
    b       C_FUNC(JIT_InternalThrow)

LOCAL_LABEL(ThrowIndexOutOfRangeException):
    // Tail call JIT_InternalThrow(NullReferenceException)
    ldr     x0, =CORINFO_IndexOutOfRangeException_ASM
    b       C_FUNC(JIT_InternalThrow)

LEAF_END JIT_Stelem_Ref, _TEXT

// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_Stelem_Ref_NotExactMatch(PtrArray* array,
//                                                       unsigned idx, Object* val)
//   x12 = array->GetArrayElementTypeHandle()
//
NESTED_ENTRY JIT_Stelem_Ref_NotExactMatch, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -48
    // Spill callee saved registers
    PROLOG_SAVE_REG_PAIR   x0, x1, 16
    PROLOG_SAVE_REG        x2, 32

    // allow in case val can be casted to array element type
    // call ObjIsInstanceOfNoGC(val, array->GetArrayElementTypeHandle())
    mov     x1, x12 // array->GetArrayElementTypeHandle()
    mov     x0, x2
    bl      C_FUNC(ObjIsInstanceOfNoGC)
    cmp     x0, TypeHandle_CanCast
    beq     LOCAL_LABEL(DoWrite)             // ObjIsInstance returned TypeHandle::CanCast

    // check via raising frame
LOCAL_LABEL(NeedFrame):
    add     x1, sp, #16             // x1 = &array
    add     x0, sp, #32             // x0 = &val

    bl      C_FUNC(ArrayStoreCheck) // ArrayStoreCheck(&val, &array)

LOCAL_LABEL(DoWrite):
    EPILOG_RESTORE_REG_PAIR           x0, x1, 16
    EPILOG_RESTORE_REG                x2, 32
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr,   48
    b C_FUNC(JIT_Stelem_DoWrite)
NESTED_END JIT_Stelem_Ref_NotExactMatch, _TEXT

// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_Stelem_DoWrite(PtrArray* array, unsigned idx, Object* val)
LEAF_ENTRY  JIT_Stelem_DoWrite, _TEXT

    // Setup args for JIT_WriteBarrier. x14 = &array->m_array[idx] x15 = val
    add     x14, x0, #PtrArray__m_Array // x14 = &array->m_array
    add     x14, x14, x1, LSL #3
    mov     x15, x2                     // x15 = val

    // Branch to the write barrier (which is already correctly overwritten with
    // single or multi-proc code based on the current CPU
    b       C_FUNC(JIT_WriteBarrier)
LEAF_END JIT_Stelem_DoWrite, _TEXT
