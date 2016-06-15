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
    ldr x1, [x0, #MachState__isValid]
    cmp x1, #0
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

    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, #-144
    SAVE_ARGUMENT_REGISTERS sp, 16
    SAVE_FLOAT_ARGUMENT_REGISTERS sp, 80 

    mov x0, x12
    bl NDirectImportWorker
    mov x12, x0

    // pop the stack and restore original register state
    RESTORE_FLOAT_ARGUMENT_REGISTERS sp, 80
    RESTORE_ARGUMENT_REGISTERS sp, 16
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, #144

    // If we got back from NDirectImportWorker, the MD has been successfully
    // linked. Proceed to execute the original DLL call.
    EPILOG_BRANCH_REG x12

NESTED_END NDirectImportThunk, _TEXT

// ------------------------------------------------------------------
// ARM64TODO: Implement PrecodeFixupThunk when PreCode is Enabled
NESTED_ENTRY PrecodeFixupThunk, _TEXT, NoHandler
     brk #0
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


// ------------------------------------------------------------------
// void ResolveWorkerAsmStub(args in regs x0-x7 & stack, x11:IndirectionCellAndFlags, x12:DispatchToken)
//
// The stub dispatch thunk which transfers control to VSD_ResolveWorker.
NESTED_ENTRY ResolveWorkerAsmStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    add x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
    and x1, x11, #-4 // Indirection cell
    mov x2, x12 // DispatchToken
    and x3, x11, #3 // flag
    bl C_FUNC(VSD_ResolveWorker)
    mov x9, x0
   
    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

    EPILOG_BRANCH_REG x9

NESTED_END ResolveWorkerAsmStub, _TEXT

NESTED_ENTRY ResolveWorkerChainLookupAsmStub, _TEXT, NoHandler

    // ARMSTUB TODO: implement chained lookup
    b C_FUNC(ResolveWorkerAsmStub)

NESTED_END ResolveWorkerChainLookupAsmStub, _TEXT

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
//   x14  : incremented by 8
//   x15  : trashed
//
WRITE_BARRIER_ENTRY JIT_CheckedWriteBarrier
    // ARM64TODO: Temporary indirect access till support for :lo12:symbol is added
    ldr  x12,  =g_lowest_address
    ldr  x12,  [x12]
    cmp  x14,  x12
    blt  LOCAL_LABEL(NotInHeap)

    // ARM64TODO: Temporary indirect access till support for :lo12:symbol is added
    ldr  x12, =g_highest_address 
    ldr  x12, [x12] 
    cmp  x14, x12
    blt  C_FUNC(JIT_WriteBarrier)

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
//   x14  : incremented by 8
//   x15  : trashed
//
WRITE_BARRIER_ENTRY JIT_WriteBarrier
    dmb  ST
    str  x15, [x14], 8

    // Branch to Exit if the reference is not in the Gen0 heap
    //
    // ARM64TODO: Temporary indirect access till support for :lo12:symbol is added
    ldr  x12,  =g_ephemeral_low
    ldr  x12,  [x12]
    cmp  x15,  x12
    blt  LOCAL_LABEL(Exit)

    // ARM64TODO: Temporary indirect access till support for :lo12:symbol is added
    ldr  x12, =g_ephemeral_high 
    ldr  x12, [x12]
    cmp  x15,  x12
    bgt  LOCAL_LABEL(Exit)

    // Check if we need to update the card table
    // ARM64TODO: Temporary indirect access till support for :lo12:symbol is added
    ldr  x12, =g_card_table
    ldr  x12, [x12]
    add  x15,  x12, x14, lsr #11
    ldrb w12, [x15]
    cmp  x12, 0xFF
    beq  LOCAL_LABEL(Exit)

LOCAL_LABEL(UpdateCardTable):
    mov  x12, 0xFF 
    strb w12, [x15]
LOCAL_LABEL(Exit):
    ret  lr  
WRITE_BARRIER_END JIT_WriteBarrier

// ------------------------------------------------------------------
// Start of the writeable code region
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
    ret  lr
LEAF_END JIT_PatchedCodeStart, _TEXT

// ------------------------------------------------------------------
// End of the writeable code region
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
    ret  lr
LEAF_END JIT_PatchedCodeLast, _TEXT

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
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, #-144
    SAVE_ARGUMENT_REGISTERS sp, 16
    SAVE_FLOAT_ARGUMENT_REGISTERS sp, 80 

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
    RESTORE_FLOAT_ARGUMENT_REGISTERS sp, 80
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, #144

    PATCH_LABEL VirtualMethodFixupPatchLabel

    // and tailcall to the actual method
    EPILOG_BRANCH_REG x12

NESTED_END VirtualMEthodFixupStub, _TEXT

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

//
// x12 = UMEntryThunk*
//
NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix

    // Save arguments and return address
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, #-144
    SAVE_ARGUMENT_REGISTERS sp, 16
    SAVE_FLOAT_ARGUMENT_REGISTERS sp, 80 

    mov x0, x12
    bl C_FUNC(TheUMEntryPrestubWorker)

    // save real target address in x12.
    mov x12, x0

    // pop the stack and restore original register state
    RESTORE_ARGUMENT_REGISTERS sp, 16
    RESTORE_FLOAT_ARGUMENT_REGISTERS sp, 80
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, #144

    // and tailcall to the actual method
    EPILOG_BRANCH_REG x12

NESTED_END TheUMEntryPrestub, _TEXT

//
// x12 = UMEntryThunk*
//
NESTED_ENTRY UMThunkStub, _TEXT, UnhandledExceptionHandlerUnix

    // Save arguments and return address
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, #-96 // 64 for regArgs, 8 for x19 & 8 for x12
    // save callee saved reg x19. x19 is used in the method to store thread*
    PROLOG_SAVE_REG x19, #88

    SAVE_ARGUMENT_REGISTERS sp, 16

#define UMThunkStub_HiddenArg 80 // offset of saved UMEntryThunk *
#define UMThunkStub_StackArgs 96 // offset of original stack args (total size of UMThunkStub frame)

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

    ldr x2, =g_TrapReturningThreads
    ldr x3, [x2]
    // assuming x0 contains Thread* before jumping to UMThunkStub_DoTrapReturningThreads
    cbnz x3, LOCAL_LABEL(UMThunkStub_DoTrapReturningThreads)

LOCAL_LABEL(UMThunkStub_InCooperativeMode):
    ldr x12, [fp, #UMThunkStub_HiddenArg] // x12 = UMEntryThunk*

    ldr x0, [x19, #Thread__m_pDomain]

    // m_dwDomainId is 4 bytes so using 32-bit variant
    ldr w1, [x12, #UMEntryThunk__m_dwDomainId]
    ldr w0, [x0, #AppDomain__m_dwId]
    cmp w0, w1
    bne LOCAL_LABEL(UMThunkStub_WrongAppDomain)

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
    EPILOG_RESTORE_REG x19, #88
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr, #96

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

LOCAL_LABEL(UMThunkStub_WrongAppDomain):
    // Saving FP Args as this is read by UM2MThunk_WrapperHelper
    sub sp, sp, #SIZEOF__FloatArgumentRegisters
    SAVE_FLOAT_ARGUMENT_REGISTERS sp, 0

    // UMEntryThunk* pUMEntry
    ldr x0, [fp, #UMThunkStub_HiddenArg]

    // void * pArgs
    add x2, fp, #16  

    // remaining arguments are unused
    bl C_FUNC(UM2MDoADCallBack)

    // restore integral return value
    ldr x0, [fp, #16]

    // restore FP or HFA return value
    RESTORE_FLOAT_ARGUMENT_REGISTERS sp, 0

    b LOCAL_LABEL(UMThunkStub_PostCall)

NESTED_END UMThunkStub, _TEXT


// UM2MThunk_WrapperHelper(void *pThunkArgs, // x0
//                         int cbStackArgs,  // x1 (unused)
//                         void *pAddr,  // x2 (unused)
//                         UMEntryThunk *pEntryThunk,// x3
//                         Thread *pThread)  // x4

// pThunkArgs points to the argument registers pushed on the stack by UMThunkStub

NESTED_ENTRY UM2MThunk_WrapperHelper, _TEXT, NoHandler

    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, #-32
    PROLOG_SAVE_REG  x19, #16


    // save pThunkArgs in non-volatile reg. It is required after return from call to ILStub
    mov x19, x0  

    // ARM64TODO - Is this required by ILStub
    mov x12, x3  //// x12 = UMEntryThunk *

    //
    // Note that layout of the arguments is given by UMThunkStub frame
    //
    ldr x3, [x3, #UMEntryThunk__m_pUMThunkMarshInfo]

    // m_cbActualArgSize is 4-byte field
    ldr w2, [x3, #UMThunkMarshInfo__m_cbActualArgSize]
    cbz w2, LOCAL_LABEL(UM2MThunk_WrapperHelper_RegArgumentsSetup)

    // extend to 64- bits
    uxtw x2, w2 

    // Source pointer. Subtracting 16 bytes due to fp & lr
    add x6, x0, #(UMThunkStub_StackArgs-16) 

    // move source ptr to end of Stack Args
    add x6, x6, x2 

    // Count of stack slot pairs to copy (divide by 16)
    lsr x1, x2, #4

    // Is there an extra stack slot? (can happen when stack arg bytes not multiple of 16)
    and x2, x2, #8

    // If yes then start source pointer from 16 byte aligned stack slot
    add x6, x6, x2

    // increment stack slot pair count by 1 if x2 is not zero
    add x1, x1, x2, LSR #3

LOCAL_LABEL(UM2MThunk_WrapperHelper_StackLoop):
    ldp x4, x5, [x6, #-16]!
    stp x4, x5, [sp, #-16]!
    subs x1, x1, #1
    bne LOCAL_LABEL(UM2MThunk_WrapperHelper_StackLoop)

LOCAL_LABEL(UM2MThunk_WrapperHelper_RegArgumentsSetup):
    ldr x16, [x3, #(UMThunkMarshInfo__m_pILStub)]

    // reload floating point registers
    RESTORE_FLOAT_ARGUMENT_REGISTERS x0, -1 * (SIZEOF__FloatArgumentRegisters + 16)

    // reload argument registers
    RESTORE_ARGUMENT_REGISTERS x0, 0

    blr x16

    // save integral return value
    str x0, [x19]
    // save FP/HFA return values
    SAVE_FLOAT_ARGUMENT_REGISTERS x19, -1 * (SIZEOF__FloatArgumentRegisters + 16)

    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG  x19, #16
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, #32
    EPILOG_RETURN

NESTED_END UM2MThunk_WrapperHelper, _TEXT



// ------------------------------------------------------------------
// Redirection Stub for GC in fully interruptible method
//GenerateRedirectedHandledJITCaseStub GCThreadControl
// ------------------------------------------------------------------
//GenerateRedirectedHandledJITCaseStub DbgThreadControl
// ------------------------------------------------------------------
//GenerateRedirectedHandledJITCaseStub UserSuspend
// ------------------------------------------------------------------
//GenerateRedirectedHandledJITCaseStub YieldTask

#ifdef _DEBUG
// ------------------------------------------------------------------
// Redirection Stub for GC Stress
GenerateRedirectedHandledJITCaseStub GCStress
#endif


// ------------------------------------------------------------------

// This helper enables us to call into a funclet after restoring Fp register
NESTED_ENTRY CallEHFunclet, _TEXT, NoHandler

    // Using below prolog instead of PROLOG_SAVE_REG_PAIR fp,lr, #-16
    // is intentional. Above statement would also emit instruction to save
    // sp in fp. If sp is saved in fp in prolog then it is not expected that fp can change in the body
    // of method. However, this method needs to be able to change fp before calling funclet.
    // This is required to access locals in funclet.
    PROLOG_SAVE_REG_PAIR_INDEXED x19,x20, #-16
    PROLOG_SAVE_REG   fp, #0
    PROLOG_SAVE_REG   lr, #8

    // On entry:
    //
    // X0 = throwable
    // X1 = PC to invoke
    // X2 = address of X19 register in CONTEXT record// used to restore the non-volatile registers of CrawlFrame
    // X3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    //
    // Save the SP of this function
    str fp, [x3]

    ldr fp, [x2, #80] // offset of fp in CONTEXT relative to X19

    // Invoke the funclet
    blr x1
    nop

    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr, #16
    EPILOG_RETURN

NESTED_END CallEHFunclet, _TEXT

// This helper enables us to call into a filter funclet by passing it the CallerSP to lookup the 
// frame pointer for accessing the locals in the parent method.
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler

    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, #-16

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

    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr,   #16
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

        PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, #-16
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
//
// Helpers for async (NullRef, AccessViolation) exceptions
//

NESTED_ENTRY NakedThrowHelper2, _TEXT ,FixContextHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp,lr, #-16

    // On entry:
    //
    // X0 = Address of FaultingExceptionFrame
    bl C_FUNC(LinkFrameAndThrow)

    // Target should not return.
    EMIT_BREAKPOINT

NESTED_END NakedThrowHelper2, _TEXT

GenerateRedirectedStubWithFrame NakedThrowHelper, NakedThrowHelper2

#ifdef FEATURE_READYTORUN

NESTED_ENTRY DelayLoad_MethodCall_FakeProlog, _TEXT, NoHandler
DelayLoad_MethodCall:
    .global DelayLoad_MethodCall

    EMIT_BREAKPOINT
NESTED_END DelayLoad_MethodCall_FakeProlog, _TEXT


.macro DynamicHelper frameFlags, suffix
    NESTED_ENTRY DelayLoad_Helper\suffix\()_FakeProlog, _TEXT, NoHandler
DelayLoad_Helper\suffix:
        .global DelayLoad_Helper\suffix

        EMIT_BREAKPOINT

    NESTED_END DelayLoad_Helper\suffix\()_FakeProlog, _TEXT
.endm

DynamicHelper DynamicHelperFrameFlags_Default
DynamicHelper DynamicHelperFrameFlags_ObjectArg, _Obj
DynamicHelper DynamicHelperFrameFlags_ObjectArg | DynamicHelperFrameFlags_ObjectArg2, _ObjObj

#endif
