// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

// DWORD64 __stdcall GetDataCacheZeroIDReg(void)
LEAF_ENTRY GetDataCacheZeroIDReg, _TEXT
    mrs     x0, dczid_el0
    and     x0, x0, 31
    ret     lr
LEAF_END GetDataCacheZeroIDReg, _TEXT

// uint64_t GetSveLengthFromOS(void);
.arch_extension sve
    LEAF_ENTRY GetSveLengthFromOS, _TEXT
        rdvl    x0, 1
        ret     lr
    LEAF_END GetSveLengthFromOS, _TEXT

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

.endm

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
    bl C_FUNC(NDirectImportWorker)
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

NESTED_ENTRY ThePreStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    add x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
    mov x1, METHODDESC_REGISTER // pMethodDesc

    bl  C_FUNC(PreStubWorker)

    mov x9, x0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG  x9

NESTED_END ThePreStub, _TEXT

// ------------------------------------------------------------------
// ThePreStubPatch()

LEAF_ENTRY ThePreStubPatch, _TEXT
    nop
PATCH_LABEL ThePreStubPatchLabel
    ret lr
LEAF_END ThePreStubPatch, _TEXT

// ------------------------// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_WriteBarrier_Callable(Object **dst, Object* val)
LEAF_ENTRY  JIT_WriteBarrier_Callable, _TEXT

    // Setup args for JIT_WriteBarrier. x14 = dst ; x15 = val
    mov     x14, x0                     // x14 = dst
    mov     x15, x1                     // x15 = val

    // Branch to the write barrier
    PREPARE_EXTERNAL_VAR JIT_WriteBarrier_Loc, x17
    ldr     x17, [x17]
    br      x17
LEAF_END JIT_WriteBarrier_Callable, _TEXT

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

#ifdef FEATURE_HIJACK
// ------------------------------------------------------------------
// Hijack function for functions which return a scalar type or a struct (value type)
NESTED_ENTRY OnHijackTripThread, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -192
    // Spill callee saved registers
    PROLOG_SAVE_REG_PAIR   x19, x20, 16
    PROLOG_SAVE_REG_PAIR   x21, x22, 32
    PROLOG_SAVE_REG_PAIR   x23, x24, 48
    PROLOG_SAVE_REG_PAIR   x25, x26, 64
    PROLOG_SAVE_REG_PAIR   x27, x28, 80

    // save any integral return value(s)
    stp x0, x1, [sp, #96]

    // save async continuation return value
    str x2, [sp, #112]

    // save any FP/HFA return value(s)
    stp q0, q1, [sp, #128]
    stp q2, q3, [sp, #160]

    mov x0, sp
    bl C_FUNC(OnHijackWorker)

    // restore any integral return value(s)
    ldp x0, x1, [sp, #96]

    // restore async continuation return value
    ldr x2, [sp, #112]

    // restore any FP/HFA return value(s)
    ldp q0, q1, [sp, #128]
    ldp q2, q3, [sp, #160]

    EPILOG_RESTORE_REG_PAIR   x19, x20, 16
    EPILOG_RESTORE_REG_PAIR   x21, x22, 32
    EPILOG_RESTORE_REG_PAIR   x23, x24, 48
    EPILOG_RESTORE_REG_PAIR   x25, x26, 64
    EPILOG_RESTORE_REG_PAIR   x27, x28, 80
    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr,   192
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
    PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED   x29, lr, -96

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

// This helper enables us to call into a filter funclet after restoring Fp register
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler

    PROLOG_SAVE_REG_PAIR_NO_FP_INDEXED   fp, lr, -16

    // On entry:
    //
    // X0 = throwable
    // X1 = FP of main function
    // X2 = PC to invoke
    // X3 = address of the location where the SP of funclet's caller (i.e. this helper) should be saved.
    //
    // Save the SP of this function
    mov fp, sp
    str fp, [x3]
    // Restore frame pointer
    mov fp, x1
    // Invoke the filter funclet
    blr x2

    EPILOG_RESTORE_REG_PAIR_INDEXED   fp, lr,   16
    EPILOG_RETURN

NESTED_END CallEHFilterFunclet, _TEXT

#define FaultingExceptionFrame_StackAlloc (SIZEOF__FaultingExceptionFrame)
#define FaultingExceptionFrame_FrameOffset 0

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

.endm

// ------------------------------------------------------------------
//
// Helpers for ThreadAbort exceptions
//

        NESTED_ENTRY RedirectForThreadAbort2, _TEXT, NoHandler
        PROLOG_SAVE_REG_PAIR_INDEXED fp,lr, -16

        // stack must be 16 byte aligned
        CHECK_STACK_ALIGNMENT

        // On entry:
        //
        // x0 = address of FaultingExceptionFrame
        //
        // Invoke the helper to setup the FaultingExceptionFrame and raise the exception
        bl              C_FUNC(ThrowControlForThread)

        // ThrowControlForThread doesn't return.
        EMIT_BREAKPOINT

        NESTED_END RedirectForThreadAbort2, _TEXT

GenerateRedirectedStubWithFrame RedirectForThreadAbort, RedirectForThreadAbort2

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
#ifdef FEATURE_VIRTUAL_STUB_DISPATCH

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
    br      x16               // branch to interface implementation target

LOCAL_LABEL(Promote):
                              // Move this entry to head position of the chain
    mov     x16, #256
    str     x16, [x13]         // be quick to reset the counter so we don't get a bunch of contending threads
    orr     x11, x11, #PROMOTE_CHAIN_FLAG   // set PROMOTE_CHAIN_FLAG
    mov     x12, x9           // We pass the ResolveCacheElem to ResolveWorkerAsmStub instead of the DispatchToken

LOCAL_LABEL(Fail):
    b       C_FUNC(ResolveWorkerAsmStub) // call the ResolveWorkerAsmStub method to transition into the VM

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
    bl C_FUNC(VSD_ResolveWorker)
    mov x9, x0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

    EPILOG_BRANCH_REG  x9

NESTED_END ResolveWorkerAsmStub, _TEXT
#endif // FEATURE_VIRTUAL_STUB_DISPATCH
#ifdef FEATURE_READYTORUN

NESTED_ENTRY DelayLoad_MethodCall_FakeProlog, _TEXT, NoHandler
#if defined(__APPLE__)
    .alt_entry C_FUNC(DelayLoad_MethodCall)
#endif
    .global C_FUNC(DelayLoad_MethodCall)
C_FUNC(DelayLoad_MethodCall):

    PROLOG_WITH_TRANSITION_BLOCK

    add x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
    mov x1, x11 // Indirection cell
    mov x2, x9 // sectionIndex
    mov x3, x10 // Module*
    bl C_FUNC(ExternalMethodFixupWorker)
    mov x12, x0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG   x12

NESTED_END DelayLoad_MethodCall_FakeProlog, _TEXT


.macro DynamicHelper frameFlags, suffix
NESTED_ENTRY DelayLoad_Helper\suffix\()_FakeProlog, _TEXT, NoHandler
#if defined(__APPLE__)
    .alt_entry C_FUNC(DelayLoad_Helper\suffix)
#endif
    .global C_FUNC(DelayLoad_Helper\suffix)
C_FUNC(DelayLoad_Helper\suffix):

    PROLOG_WITH_TRANSITION_BLOCK

    add x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
    mov x1, x11 // Indirection cell
    mov x2, x9 // sectionIndex
    mov x3, x10 // Module*
    mov x4, \frameFlags
    bl C_FUNC(DynamicHelperWorker)
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

//
// JIT Static access helpers when coreclr host specifies single appdomain flag
//

// ------------------------------------------------------------------
// void* JIT_GetDynamicNonGCStaticBase(DynamicStaticsInfo* pStaticsInfo)

LEAF_ENTRY JIT_GetDynamicNonGCStaticBase_SingleAppDomain, _TEXT
    // If class is not initialized, bail to C++ helper
    add x1, x0, #OFFSETOF__DynamicStaticsInfo__m_pNonGCStatics
    ldar x1, [x1]
    tbnz x1, #0, LOCAL_LABEL(JIT_GetDynamicNonGCStaticBase_SingleAppDomain_CallHelper)
    mov x0, x1
    ret lr

LOCAL_LABEL(JIT_GetDynamicNonGCStaticBase_SingleAppDomain_CallHelper):
    // Tail call GetNonGCStaticBase
    ldr x0, [x0, #OFFSETOF__DynamicStaticsInfo__m_pMethodTable]
    PREPARE_EXTERNAL_VAR g_pGetNonGCStaticBase, x1
    ldr      x1, [x1]
    br       x1
LEAF_END JIT_GetDynamicNonGCStaticBase_SingleAppDomain, _TEXT

// ------------------------------------------------------------------
// void* JIT_GetDynamicGCStaticBase(DynamicStaticsInfo* pStaticsInfo)

LEAF_ENTRY JIT_GetDynamicGCStaticBase_SingleAppDomain, _TEXT
    // If class is not initialized, bail to C++ helper
    add x1, x0, #OFFSETOF__DynamicStaticsInfo__m_pGCStatics
    ldar x1, [x1]
    tbnz x1, #0, LOCAL_LABEL(JIT_GetDynamicGCStaticBase_SingleAppDomain_CallHelper)
    mov x0, x1
    ret lr

LOCAL_LABEL(JIT_GetDynamicGCStaticBase_SingleAppDomain_CallHelper):
    // Tail call GetGCStaticBase
    ldr x0, [x0, #OFFSETOF__DynamicStaticsInfo__m_pMethodTable]
    PREPARE_EXTERNAL_VAR g_pGetGCStaticBase, x1
    ldr      x1, [x1]
    br       x1
LEAF_END JIT_GetDynamicGCStaticBase_SingleAppDomain, _TEXT

#ifdef PROFILING_SUPPORTED

// ------------------------------------------------------------------
LEAF_ENTRY JIT_ProfilerEnterLeaveTailcallStub, _TEXT
    ret     lr
LEAF_END JIT_ProfilerEnterLeaveTailcallStub, _TEXT

// ------------------------------------------------------------------
.macro GenerateProfileHelper helper, flags
NESTED_ENTRY \helper\()Naked, _TEXT, NoHandler
    // On entry:
    //   x10 = functionIDOrClientID
    //   x11 = profiledSp
    //   x12 = throwable
    //
    // On exit:
    //   Values of x0-x8, q0-q7, fp are preserved.
    //   Values of other volatile registers are not preserved.

    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA // Allocate space and save Fp, Pc.
    SAVE_ARGUMENT_REGISTERS sp, PROFILE_PLATFORM_SPECIFIC_DATA__x8  // Save x8 and argument registers (x0-x7).
    str     xzr, [sp, PROFILE_PLATFORM_SPECIFIC_DATA__functionId]  // Clear functionId.
    SAVE_FLOAT_ARGUMENT_REGISTERS sp, PROFILE_PLATFORM_SPECIFIC_DATA__floatArgumentRegisters
    add     x12, fp, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA  // Compute probeSp - initial value of Sp on entry to the helper.
    stp     x12, x11, [sp, PROFILE_PLATFORM_SPECIFIC_DATA__probeSp]  // Save probeSp, profiledSp.
    str     xzr, [sp, PROFILE_PLATFORM_SPECIFIC_DATA__hiddenArg]  // Clear hiddenArg.
    mov     w12, \flags
    stp     w12, wzr, [sp, PROFILE_PLATFORM_SPECIFIC_DATA__flags]  // Save flags and clear unused field.

    mov     x0, x10
    mov     x1, sp
    bl      C_FUNC(\helper)

    RESTORE_ARGUMENT_REGISTERS sp, PROFILE_PLATFORM_SPECIFIC_DATA__x8  // Restore x8 and argument registers.
    RESTORE_FLOAT_ARGUMENT_REGISTERS sp, PROFILE_PLATFORM_SPECIFIC_DATA__floatArgumentRegisters  // Restore floating-point/SIMD registers.

    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA
    EPILOG_RETURN

NESTED_END \helper\()Naked, _TEXT
.endm

GenerateProfileHelper ProfileEnter, PROFILE_ENTER
GenerateProfileHelper ProfileLeave, PROFILE_LEAVE
GenerateProfileHelper ProfileTailcall, PROFILE_TAILCALL

#endif

#ifdef FEATURE_TIERED_COMPILATION

NESTED_ENTRY OnCallCountThresholdReachedStub, _TEXT, NoHandler
    PROLOG_WITH_TRANSITION_BLOCK

    add     x0, sp, #__PWTB_TransitionBlock // TransitionBlock *
    mov     x1, x9 // stub-identifying token
    bl      C_FUNC(OnCallCountThresholdReached)
    mov     x9, x0

    EPILOG_WITH_TRANSITION_BLOCK_TAILCALL
    EPILOG_BRANCH_REG x9
NESTED_END OnCallCountThresholdReachedStub, _TEXT

NESTED_ENTRY JIT_Patchpoint, _TEXT, NoHandler
    PROLOG_WITH_TRANSITION_BLOCK

    add     x0, sp, #__PWTB_TransitionBlock // TransitionBlock *
    bl      C_FUNC(JIT_PatchpointWorkerWorkerWithPolicy)

    EPILOG_WITH_TRANSITION_BLOCK_RETURN
NESTED_END JIT_Patchpoint, _TEXT

// first arg register holds iloffset, which needs to be moved to the second register, and the first register filled with NULL
LEAF_ENTRY JIT_PatchpointForced, _TEXT
    mov x1, x0
    mov x0, #0
    b C_FUNC(JIT_Patchpoint)
LEAF_END JIT_PatchpointForced, _TEXT

#endif // FEATURE_TIERED_COMPILATION

LEAF_ENTRY JIT_ValidateIndirectCall, _TEXT
    ret lr
LEAF_END JIT_ValidateIndirectCall, _TEXT

LEAF_ENTRY JIT_DispatchIndirectCall, _TEXT
    br x9
LEAF_END JIT_DispatchIndirectCall, _TEXT

#ifdef TARGET_APPLE
// ------------------------------------------------------------------
// void* GetThreadVarsAddress()

// Helper to calculate the address of relevant __thread_vars section that holds the address of symbol tlv_get_address for thread
// local `t_ThreadStatics`. The address is updated by the linker, which we retrieve here. In JIT code, this address is called
// to retrieve the address of the thread local.

LEAF_ENTRY GetThreadVarsAddress, _TEXT
        adrp    x0,     _t_ThreadStatics@TLVPPAGE
        ldr     x0,     [x0, _t_ThreadStatics@TLVPPAGEOFF]
        ret
LEAF_END GetThreadVarsAddress, _TEXT
// ------------------------------------------------------------------
#endif // TARGET_APPLE

#ifndef TARGET_APPLE
// ------------------------------------------------------------------
// size_t GetThreadStaticsVariableOffset()

#ifndef TARGET_ANDROID
// Helper to calculate the offset of native thread local variable `t_ThreadStatics` in TCB. The offset has to be found at runtime
// once linker does its relocation and fixup of thread locals. The offset, after calculation is returned in `x0` register.

LEAF_ENTRY GetThreadStaticsVariableOffset, _TEXT
        PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -32
        adrp    x0,     :tlsdesc:t_ThreadStatics
        ldr     x1,     [x0, #:tlsdesc_lo12:t_ThreadStatics]
        add     x0,     x0, :tlsdesc_lo12:t_ThreadStatics
        .tlsdesccall    t_ThreadStatics
        blr     x1
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
        EPILOG_RETURN
LEAF_END GetThreadStaticsVariableOffset, _TEXT
// ------------------------------------------------------------------

// ------------------------------------------------------------------
// size_t GetTLSResolverAddress()

// Helper to get the TLS resolver address. This will be then used to determine if we have a static or dynamic resolver.
LEAF_ENTRY GetTLSResolverAddress, _TEXT
        PROLOG_SAVE_REG_PAIR_INDEXED   fp, lr, -32
        adrp    x0,     :tlsdesc:t_ThreadStatics
        ldr     x1,     [x0, #:tlsdesc_lo12:t_ThreadStatics]
        mov     x0, x1
        EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
        EPILOG_RETURN
LEAF_END GetTLSResolverAddress, _TEXT
// ------------------------------------------------------------------
#endif // TARGET_ANDROID
#endif // !TARGET_APPLE

LEAF_ENTRY  JIT_PollGC, _TEXT
        PREPARE_EXTERNAL_VAR g_TrapReturningThreads, x9
        ldr     w9, [x9]
        cbnz    w9, LOCAL_LABEL(JIT_PollGCRarePath)
        ret
LOCAL_LABEL(JIT_PollGCRarePath):
        PREPARE_EXTERNAL_VAR g_pPollGC, x9
        ldr     x9, [x9]
        br x9
LEAF_END  JIT_PollGC, _TEXT

//x0 -This pointer
//x1 -ReturnBuffer
LEAF_ENTRY ThisPtrRetBufPrecodeWorker, _TEXT
    ldr  x12, [METHODDESC_REGISTER, #ThisPtrRetBufPrecodeData__Target]
    mov  x11, x0     // Move first arg pointer to temp register
    mov  x0,  x1     // Move ret buf arg pointer from location in ABI for return buffer for instance method to location in ABI for return buffer for static method
    mov  x1, x11     // Move temp register to first arg register for static method with return buffer
    EPILOG_BRANCH_REG x12
LEAF_END ThisPtrRetBufPrecodeWorker, _TEXT

#ifdef FEATURE_INTERPRETER
NESTED_ENTRY InterpreterStub, _TEXT, NoHandler

    PROLOG_WITH_TRANSITION_BLOCK

    add x0, sp, #__PWTB_TransitionBlock // pTransitionBlock
    mov x1, METHODDESC_REGISTER // pMethodDesc

    bl  C_FUNC(ExecuteInterpretedMethod)

    EPILOG_WITH_TRANSITION_BLOCK_RETURN

NESTED_END InterpreterStub, _TEXT

// Copy arguments from the interpreter stack to the processor stack
// The CPU stack slots are aligned to pointer size.
LEAF_ENTRY Load_Stack
    ldr w14, [x10], #4 // SP offset
    ldr w12, [x10], #4 // number of stack slots
    add x14, sp, x14
LOCAL_LABEL(CopyLoop):
    ldr x13, [x9], #8
    str x13, [x14], #8
    subs x12, x12, #8
    bne LOCAL_LABEL(CopyLoop)
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_Stack

#ifdef TARGET_APPLE

// Copy single byte argument from the interpreter stack to the processor stack
LEAF_ENTRY Load_Stack_1B
    ldr x14, [x10], #8 // SP offset
    add x14, sp, x14
    ldrb w13, [x9], #8
    strb w13, [x14]
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_Stack_1B

// Copy two byte argument from the interpreter stack to the processor stack
LEAF_ENTRY Load_Stack_2B
    ldr x14, [x10], #8 // SP offset
    add x14, sp, x14
    ldrh w13, [x9], #8
    strh w13, [x14]
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_Stack_2B

// Copy four byte argument from the interpreter stack to the processor stack
LEAF_ENTRY Load_Stack_4B
    ldr x14, [x10], #8 // SP offset
    add x14, sp, x14
    ldr w13, [x9], #8
    str w13, [x14]
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_Stack_4B

#endif // TARGET_APPLE

// Routines for passing value type arguments by reference in general purpose registers X0..X7

LEAF_ENTRY Load_Ref_X0
    mov x0, x9
    ldr x12, [x10], #8
    add x9, x9, x12
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X0

LEAF_ENTRY Load_Ref_X1
    mov x1, x9
    ldr x12, [x10], #8
    add x9, x9, x12
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X1

LEAF_ENTRY Load_Ref_X2
    mov x2, x9
    ldr x12, [x10], #8
    add x9, x9, x12
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_Ref_X2

LEAF_ENTRY Load_Ref_X3
    mov x3, x9
    ldr x12, [x10], #8
    add x9, x9, x12
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_Ref_X3

LEAF_ENTRY Load_Ref_X4
    mov x4, x9
    ldr x12, [x10], #8
    add x9, x9, x12
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_Ref_X4

LEAF_ENTRY Load_Ref_X5
    mov x5, x9
    ldr x12, [x10], #8
    add x9, x9, x12
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_Ref_X5

LEAF_ENTRY Load_Ref_X6
    mov x6, x9
    ldr x12, [x10], #8
    add x9, x9, x12
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_Ref_X6

LEAF_ENTRY Load_Ref_X7
    mov x7, x9
    ldr x12, [x10], #8
    add x9, x9, x12
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_Ref_X7

// Routines for passing arguments by value in general purpose registers X0..X7

LEAF_ENTRY Load_X0
    ldr x0, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X0

LEAF_ENTRY Load_X0_X1
    ldp x0, x1, [x9], #16
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X0_X1

LEAF_ENTRY Load_X0_X1_X2
    ldp x0, x1, [x9], #16
ALTERNATE_ENTRY Load_X2
    ldr x2, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X0_X1_X2

LEAF_ENTRY Load_X0_X1_X2_X3
    ldp x0, x1, [x9], #16
ALTERNATE_ENTRY Load_X2_X3
    ldp x2, x3, [x9], #16
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X0_X1_X2_X3

LEAF_ENTRY Load_X0_X1_X2_X3_X4
    ldp x0, x1, [x9], #16
ALTERNATE_ENTRY Load_X2_X3_X4
    ldp x2, x3, [x9], #16
ALTERNATE_ENTRY Load_X4
    ldr x4, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X0_X1_X2_X3_X4

LEAF_ENTRY Load_X0_X1_X2_X3_X4_X5
    ldp x0, x1, [x9], #16
ALTERNATE_ENTRY Load_X2_X3_X4_X5
    ldp x2, x3, [x9], #16
ALTERNATE_ENTRY Load_X4_X5
    ldp x4, x5, [x9], #16
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X0_X1_X2_X3_X4_X5

LEAF_ENTRY Load_X0_X1_X2_X3_X4_X5_X6
    ldp x0, x1, [x9], #16
ALTERNATE_ENTRY Load_X2_X3_X4_X5_X6
    ldp x2, x3, [x9], #16
ALTERNATE_ENTRY Load_X4_X5_X6
    ldp x4, x5, [x9], #16
ALTERNATE_ENTRY Load_X6
    ldr x6, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X0_X1_X2_X3_X4_X5_X6

LEAF_ENTRY Load_X0_X1_X2_X3_X4_X5_X6_X7
    ldp x0, x1, [x9], #16
ALTERNATE_ENTRY Load_X2_X3_X4_X5_X6_X7
    ldp x2, x3, [x9], #16
ALTERNATE_ENTRY Load_X4_X5_X6_X7
    ldp x4, x5, [x9], #16
ALTERNATE_ENTRY Load_X6_X7
    ldp x6, x7, [x9], #16
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X0_X1_X2_X3_X4_X5_X6_X7

LEAF_ENTRY Load_X1
    ldr x1, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X1

LEAF_ENTRY Load_X1_X2
    ldp x1, x2, [x9], #16
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X1_X2

LEAF_ENTRY Load_X1_X2_X3
    ldp x1, x2, [x9], #16
ALTERNATE_ENTRY Load_X3
    ldr x3, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X1_X2_X3

LEAF_ENTRY Load_X1_X2_X3_X4
    ldp x1, x2, [x9], #16
ALTERNATE_ENTRY Load_X3_X4
    ldp x3, x4, [x9], #16
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X1_X2_X3_X4

LEAF_ENTRY Load_X1_X2_X3_X4_X5
    ldp x1, x2, [x9], #16
ALTERNATE_ENTRY Load_X3_X4_X5
    ldp x3, x4, [x9], #16
ALTERNATE_ENTRY Load_X5
    ldr x5, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X1_X2_X3_X4_X5

LEAF_ENTRY Load_X1_X2_X3_X4_X5_X6
    ldp x1, x2, [x9], #16
ALTERNATE_ENTRY Load_X3_X4_X5_X6
    ldp x3, x4, [x9], #16
ALTERNATE_ENTRY Load_X5_X6
    ldp x5, x6, [x9], #16
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X1_X2_X3_X4_X5_X6

LEAF_ENTRY Load_X1_X2_X3_X4_X5_X6_X7
    ldp x1, x2, [x9], #16
ALTERNATE_ENTRY Load_X3_X4_X5_X6_X7
    ldp x3, x4, [x9], #16
ALTERNATE_ENTRY Load_X5_X6_X7
    ldp x5, x6, [x9], #16
ALTERNATE_ENTRY Load_X7
    ldr x7, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_X1_X2_X3_X4_X5_X6_X7

// Routines for passing arguments in floating point registers D0..D7

LEAF_ENTRY Load_D0
    ldr d0, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_D0

LEAF_ENTRY Load_D1
    ldr d1, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_D1

LEAF_ENTRY Load_D0_D1
    ldp d0, d1, [x9], #16
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_D0_D1

LEAF_ENTRY Load_D0_D1_D2
    ldr d0, [x9], #8
ALTERNATE_ENTRY Load_D1_D2
    ldr d1, [x9], #8
ALTERNATE_ENTRY Load_D2
    ldr d2, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_D0_D1_D2

LEAF_ENTRY Load_D0_D1_D2_D3
    ldr d0, [x9], #8
ALTERNATE_ENTRY Load_D1_D2_D3
    ldr d1, [x9], #8
ALTERNATE_ENTRY Load_D2_D3
    ldr d2, [x9], #8
ALTERNATE_ENTRY Load_D3
    ldr d3, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_D0_D1_D2_D3

LEAF_ENTRY Load_D0_D1_D2_D3_D4
    ldr d0, [x9], #8
ALTERNATE_ENTRY Load_D1_D2_D3_D4
    ldr d1, [x9], #8
ALTERNATE_ENTRY Load_D2_D3_D4
    ldr d2, [x9], #8
ALTERNATE_ENTRY Load_D3_D4
    ldr d3, [x9], #8
ALTERNATE_ENTRY Load_D4
    ldr d4, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_D0_D1_D2_D3_D4

LEAF_ENTRY Load_D0_D1_D2_D3_D4_D5
    ldr d0, [x9], #8
ALTERNATE_ENTRY Load_D1_D2_D3_D4_D5
    ldr d1, [x9], #8
ALTERNATE_ENTRY Load_D2_D3_D4_D5
    ldr d2, [x9], #8
ALTERNATE_ENTRY Load_D3_D4_D5
    ldr d3, [x9], #8
ALTERNATE_ENTRY Load_D4_D5
    ldr d4, [x9], #8
ALTERNATE_ENTRY Load_D5
    ldr d5, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_D0_D1_D2_D3_D4_D5

LEAF_ENTRY Load_D0_D1_D2_D3_D4_D5_D6
    ldr d0, [x9], #8
ALTERNATE_ENTRY Load_D1_D2_D3_D4_D5_D6
    ldr d1, [x9], #8
ALTERNATE_ENTRY Load_D2_D3_D4_D5_D6
    ldr d2, [x9], #8
ALTERNATE_ENTRY Load_D3_D4_D5_D6
    ldr d3, [x9], #8
ALTERNATE_ENTRY Load_D4_D5_D6
    ldr d4, [x9], #8
ALTERNATE_ENTRY Load_D5_D6
    ldr d5, [x9], #8
ALTERNATE_ENTRY Load_D6
    ldr d6, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_D0_D1_D2_D3_D4_D5_D6

LEAF_ENTRY Load_D0_D1_D2_D3_D4_D5_D6_D7
    ldp d0, d1, [x9], #16
ALTERNATE_ENTRY Load_D2_D3_D4_D5_D6_D7
    ldp d2, d3, [x9], #16
ALTERNATE_ENTRY Load_D4_D5_D6_D7
    ldp d4, d5, [x9], #16
ALTERNATE_ENTRY Load_D6_D7
    ldp d6, d7, [x9], #16
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_D0_D1_D2_D3_D4_D5_D6_D7

LEAF_ENTRY Load_D1_D2_D3_D4_D5_D6_D7
    ldp d1, d2, [x9], #16
ALTERNATE_ENTRY Load_D3_D4_D5_D6_D7
    ldp d3, d4, [x9], #16
ALTERNATE_ENTRY Load_D5_D6_D7
    ldp d5, d6, [x9], #16
ALTERNATE_ENTRY Load_D7
    ldr d7, [x9], #8
    ldr x11, [x10], #8
    EPILOG_BRANCH_REG x11
LEAF_END Load_D1_D2_D3_D4_D5_D6_D7

// Functions to invoke a sequence of routines to:
// 1. load arguments from the interpreter stack to registers / stack based on the calling convention
// 2. call the target method
// 3. put the return value of the target method to the interpreter stack

// X0 - routines array
// X1 - interpreter stack args location
// X2 - stack arguments size (properly aligned)
NESTED_ENTRY CallJittedMethodRetVoid, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -16
    sub sp, sp, x3
    mov x10, x0
    mov x9, x1
    ldr x11, [x10], #8
    blr x11
    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 16
    EPILOG_RETURN
NESTED_END CallJittedMethodRetVoid, _TEXT

// X0 - routines array
// X1 - interpreter stack args location
// X2 - interpreter stack return value location
// X3 - stack arguments size (properly aligned)
NESTED_ENTRY CallJittedMethodRetBuff, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -16
    sub sp, sp, x3
    mov x10, x0
    mov x9, x1
    mov x8, x2
    ldr x11, [x10], #8
    blr x11
    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 16
    EPILOG_RETURN
NESTED_END CallJittedMethodRetBuff, _TEXT

// X0 - routines array
// X1 - interpreter stack args location
// X2 - interpreter stack return value location
// X3 - stack arguments size (properly aligned)
NESTED_ENTRY CallJittedMethodRetI8, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -32
    str x2, [sp, #16]
    sub sp, sp, x3
    mov x10, x0
    mov x9, x1
    ldr x11, [x10], #8
    blr x11
    ldr x2, [sp, #16]
    str x0, [x2]
    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
    EPILOG_RETURN
NESTED_END CallJittedMethodRetI8, _TEXT

// X0 - routines array
// X1 - interpreter stack args location
// X2 - interpreter stack return value location
// X3 - stack arguments size (properly aligned)
NESTED_ENTRY CallJittedMethodRet2I8, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -32
    str x2, [sp, #16]
    sub sp, sp, x3
    mov x10, x0
    mov x9, x1
    ldr x11, [x10], #8
    blr x11
    ldr x2, [sp, #16]
    stp x0, x1, [x2]
    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
    EPILOG_RETURN
NESTED_END CallJittedMethodRet2I8, _TEXT

// X0 - routines array
// X1 - interpreter stack args location
// X2 - interpreter stack return value location
// X3 - stack arguments size (properly aligned)
NESTED_ENTRY CallJittedMethodRetDouble, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -32
    str x2, [sp, #16]
    sub sp, sp, x3
    mov x10, x0
    mov x9, x1
    ldr x11, [x10], #8
    blr x11
    ldr x2, [sp, #16]
    str d0, [x2]
    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
    EPILOG_RETURN
NESTED_END CallJittedMethodRetDouble, _TEXT

// X0 - routines array
// X1 - interpreter stack args location
// X2 - interpreter stack return value location
// X3 - stack arguments size (properly aligned)
NESTED_ENTRY CallJittedMethodRet2Double, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -32
    str x2, [sp, #16]
    sub sp, sp, x3
    mov x10, x0
    mov x9, x1
    ldr x11, [x10], #8
    blr x11
    ldr x2, [sp, #16]
    stp d0, d1, [x2]
    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
    EPILOG_RETURN
NESTED_END CallJittedMethodRet2Double, _TEXT

// X0 - routines array
// X1 - interpreter stack args location
// X2 - interpreter stack return value location
// X3 - stack arguments size (properly aligned)
NESTED_ENTRY CallJittedMethodRet3Double, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -32
    str x2, [sp, #16]
    sub sp, sp, x3
    mov x10, x0
    mov x9, x1
    ldr x11, [x10], #8
    blr x11
    ldr x2, [sp, #16]
    stp d0, d1, [x2], #16
    str d2, [x2]
    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
    EPILOG_RETURN
NESTED_END CallJittedMethodRet3Double, _TEXT

// X0 - routines array
// X1 - interpreter stack args location
// X2 - interpreter stack return value location
// X3 - stack arguments size (properly aligned)
NESTED_ENTRY CallJittedMethodRet4Double, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -32
    str x2, [sp, #16]
    sub sp, sp, x3
    mov x10, x0
    mov x9, x1
    ldr x11, [x10], #8
    blr x11
    ldr x2, [sp, #16]
    stp d0, d1, [x2], #16
    stp d2, d3, [x2]
    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
    EPILOG_RETURN
NESTED_END CallJittedMethodRet4Double, _TEXT

// X0 - routines array
// X1 - interpreter stack args location
// X2 - interpreter stack return value location
// X3 - stack arguments size (properly aligned)
NESTED_ENTRY CallJittedMethodRetFloat, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -32
    str x2, [sp, #16]
    sub sp, sp, x3
    mov x10, x0
    mov x9, x1
    ldr x11, [x10], #8
    blr x11
    ldr x2, [sp, #16]
    str s0, [x2]
    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
    EPILOG_RETURN
NESTED_END CallJittedMethodRetFloat, _TEXT

// X0 - routines array
// X1 - interpreter stack args location
// X2 - interpreter stack return value location
// X3 - stack arguments size (properly aligned)
NESTED_ENTRY CallJittedMethodRet2Float, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -32
    str x2, [sp, #16]
    sub sp, sp, x3
    mov x10, x0
    mov x9, x1
    ldr x11, [x10], #8
    blr x11
    ldr x2, [sp, #16]
    stp s0, s1, [x2]
    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
    EPILOG_RETURN
NESTED_END CallJittedMethodRet2Float, _TEXT

// X0 - routines array
// X1 - interpreter stack args location
// X2 - interpreter stack return value location
// X3 - stack arguments size (properly aligned)
NESTED_ENTRY CallJittedMethodRet3Float, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -32
    str x2, [sp, #16]
    sub sp, sp, x3
    mov x10, x0
    mov x9, x1
    ldr x11, [x10], #8
    blr x11
    ldr x2, [sp, #16]
    stp s0, s1, [x2], #8
    str s2, [x2]
    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
    EPILOG_RETURN
NESTED_END CallJittedMethodRet3Float, _TEXT

// X0 - routines array
// X1 - interpreter stack args location
// X2 - interpreter stack return value location
// X3 - stack arguments size (properly aligned)
NESTED_ENTRY CallJittedMethodRet4Float, _TEXT, NoHandler
    PROLOG_SAVE_REG_PAIR_INDEXED fp, lr, -32
    str x2, [sp, #16]
    sub sp, sp, x3
    mov x10, x0
    mov x9, x1
    ldr x11, [x10], #8
    blr x11
    ldr x2, [sp, #16]
    stp s0, s1, [x2], #8
    stp s2, s3, [x2]
    EPILOG_STACK_RESTORE
    EPILOG_RESTORE_REG_PAIR_INDEXED fp, lr, 32
    EPILOG_RETURN
NESTED_END CallJittedMethodRet4Float, _TEXT

#endif // FEATURE_INTERPRETER
