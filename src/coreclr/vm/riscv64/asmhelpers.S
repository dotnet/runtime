// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

// #error "TODO-RISCV64: missing implementation"

LEAF_ENTRY GetCurrentIP, _TEXT
// TOOD RISCV64 NYI
LEAF_END GetCurrentIP, _TEXT

// LPVOID __stdcall GetCurrentSP(void)//
LEAF_ENTRY GetCurrentSP, _TEXT
// TOOD RISCV64 NYI
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
// TOOD RISCV64 NYI
WRITE_BARRIER_END JIT_UpdateWriteBarrierState

// ----------------------------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_WriteBarrier_Callable(Object **dst, Object* val)
LEAF_ENTRY  JIT_WriteBarrier_Callable, _TEXT
// TOOD RISCV64 NYI
LEAF_END JIT_WriteBarrier_Callable, _TEXT


.balign 64  // Align to power of two at least as big as patchable literal pool so that it fits optimally in cache line
// ------------------------------------------------------------------
// Start of the writeable code region
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
// TOOD RISCV64 NYI
LEAF_END JIT_PatchedCodeStart, _TEXT

// void JIT_ByRefWriteBarrier
WRITE_BARRIER_ENTRY JIT_ByRefWriteBarrier
// TOOD RISCV64 NYI
WRITE_BARRIER_END JIT_ByRefWriteBarrier

//-----------------------------------------------------------------------------
// Simple WriteBarriers
// void JIT_CheckedWriteBarrier(Object** dst, Object* src)
//
WRITE_BARRIER_ENTRY JIT_CheckedWriteBarrier
// TOOD RISCV64 NYI
WRITE_BARRIER_END JIT_CheckedWriteBarrier

// void JIT_WriteBarrier(Object** dst, Object* src)
//
WRITE_BARRIER_ENTRY JIT_WriteBarrier
// TOOD RISCV64 NYI
WRITE_BARRIER_END JIT_WriteBarrier


WRITE_BARRIER_ENTRY JIT_WriteBarrier_Table
// TOOD RISCV64 NYI
WRITE_BARRIER_END JIT_WriteBarrier_Table

// ------------------------------------------------------------------
// End of the writeable code region
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
// TOOD RISCV64 NYI
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
// TOOD RISCV64 NYI
.endm

NESTED_ENTRY ThePreStub, _TEXT, NoHandler
// TOOD RISCV64 NYI
NESTED_END ThePreStub, _TEXT

// ------------------------------------------------------------------\

// EXTERN_C int __fastcall HelperMethodFrameRestoreState(
// INDEBUG_COMMA(HelperMethodFrame *pFrame)
// MachState *pState
// )
LEAF_ENTRY HelperMethodFrameRestoreState, _TEXT
// TOOD RISCV64 NYI
LEAF_END HelperMethodFrameRestoreState, _TEXT

//-----------------------------------------------------------------------------
// This routine captures the machine state. It is used by helper method frame
//-----------------------------------------------------------------------------
//void LazyMachStateCaptureState(struct LazyMachState *pState)//
LEAF_ENTRY LazyMachStateCaptureState, _TEXT
// TOOD RISCV64 NYI
LEAF_END LazyMachStateCaptureState, _TEXT

// ------------------------------------------------------------------
// The call in ndirect import precode points to this function.
NESTED_ENTRY NDirectImportThunk, _TEXT, NoHandler
// TOOD RISCV64 NYI
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
// TOOD RISCV64 NYI
NESTED_END VirtualMethodFixupStub, _TEXT
#endif // FEATURE_PREJIT

// void SinglecastDelegateInvokeStub(Delegate *pThis)
LEAF_ENTRY SinglecastDelegateInvokeStub, _TEXT
// TOOD RISCV64 NYI
LEAF_END SinglecastDelegateInvokeStub, _TEXT

// ------------------------------------------------------------------
// ThePreStubPatch()
LEAF_ENTRY ThePreStubPatch, _TEXT
.globl C_FUNC(ThePreStubPatchLabel)
C_FUNC(ThePreStubPatchLabel):
// TOOD RISCV64 NYI
LEAF_END ThePreStubPatch, _TEXT

#ifdef FEATURE_PREJIT
// ------------------------------------------------------------------
// void StubDispatchFixupStub(args in regs $a0-$a7 & stack, $t1:IndirectionCellAndFlags)
//
// The stub dispatch thunk which transfers control to StubDispatchFixupWorker.
NESTED_ENTRY StubDispatchFixupStub, _TEXT, NoHandler
// TOOD RISCV64 NYI
NESTED_END StubDispatchFixupStub, _TEXT
#endif

NESTED_ENTRY TheUMEntryPrestub, _TEXT, UnhandledExceptionHandlerUnix
// TOOD RISCV64 NYI
NESTED_END TheUMEntryPrestub, _TEXT

// ------------------------------------------------------------------
// void* JIT_GetSharedGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT
// TOOD RISCV64 NYI
LEAF_END JIT_GetSharedGCStaticBase_SingleAppDomain, _TEXT

// ------------------------------------------------------------------
// ResolveWorkerChainLookupAsmStub
//
// This method will perform a quick chained lookup of the entry if the
//  initial cache lookup fails.
//

NESTED_ENTRY ResolveWorkerChainLookupAsmStub, _TEXT, NoHandler
// TOOD RISCV64 NYI
NESTED_END ResolveWorkerChainLookupAsmStub, _TEXT

// ------------------------------------------------------------------
// void ResolveWorkerAsmStub(args in regs $a0-$a7 & stack, t8:IndirectionCellAndFlags, $t2:DispatchToken)
//
// The stub dispatch thunk which transfers control to VSD_ResolveWorker.
NESTED_ENTRY ResolveWorkerAsmStub, _TEXT, NoHandler
// TOOD RISCV64 NYI
NESTED_END ResolveWorkerAsmStub, _TEXT

// ------------------------------------------------------------------
// void* JIT_GetSharedNonGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT
// TOOD RISCV64 NYI
LEAF_END JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain, _TEXT

// ------------------------------------------------------------------
// void* JIT_GetSharedGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT
// TOOD RISCV64 NYI
LEAF_END JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain, _TEXT


#ifdef FEATURE_HIJACK
// ------------------------------------------------------------------
// Hijack function for functions which return a scalar type or a struct (value type)
NESTED_ENTRY OnHijackTripThread, _TEXT, NoHandler
// TOOD RISCV64 NYI
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
// TOOD RISCV64 NYI
NESTED_END CallEHFunclet, _TEXT

// This helper enables us to call into a filter funclet by passing it the CallerSP to lookup the
// frame pointer for accessing the locals in the parent method.
NESTED_ENTRY CallEHFilterFunclet, _TEXT, NoHandler
// TOOD RISCV64 NYI
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
// TOOD RISCV64 NYI
LEAF_END getFPReturn, _TEXT

// ------------------------------------------------------------------
// Function used by COM interop to set floating point return value (since it's not in the same
// register(s) as non-floating point values).
//
LEAF_ENTRY setFPReturn, _TEXT
// TOOD RISCV64 NYI
LEAF_END setFPReturn, _TEXT

#endif // FEATURE_COMINTEROP

//
// JIT Static access helpers when coreclr host specifies single appdomain flag
//

// ------------------------------------------------------------------
// void* JIT_GetSharedNonGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

LEAF_ENTRY JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT
// TOOD RISCV64 NYI
LEAF_END JIT_GetSharedNonGCStaticBase_SingleAppDomain, _TEXT

#ifdef FEATURE_READYTORUN

NESTED_ENTRY DelayLoad_MethodCall_FakeProlog, _TEXT, NoHandler
C_FUNC(DelayLoad_MethodCall):
    .global C_FUNC(DelayLoad_MethodCall)
// TOOD RISCV64 NYI
    PATCH_LABEL ExternalMethodFixupPatchLabel
// TOOD RISCV64 NYI
NESTED_END DelayLoad_MethodCall_FakeProlog, _TEXT


.macro DynamicHelper frameFlags, suffix
NESTED_ENTRY DelayLoad_Helper\suffix\()_FakeProlog, _TEXT, NoHandler
DelayLoad_Helper\suffix:
    .global DelayLoad_Helper\suffix
// TOOD RISCV64 NYI
NESTED_END DelayLoad_Helper\suffix\()_FakeProlog, _TEXT
.endm

DynamicHelper DynamicHelperFrameFlags_Default
DynamicHelper DynamicHelperFrameFlags_ObjectArg, _Obj
DynamicHelper DynamicHelperFrameFlags_ObjectArg | DynamicHelperFrameFlags_ObjectArg2, _ObjObj
#endif


#ifdef PROFILING_SUPPORTED

// ------------------------------------------------------------------
LEAF_ENTRY JIT_ProfilerEnterLeaveTailcallStub, _TEXT
// TOOD RISCV64 NYI
LEAF_END JIT_ProfilerEnterLeaveTailcallStub, _TEXT

// ------------------------------------------------------------------
#define PROFILE_ENTER    1
#define PROFILE_LEAVE    2
#define PROFILE_TAILCALL 4
#define SIZEOF__PROFILE_PLATFORM_SPECIFIC_DATA 272

// ------------------------------------------------------------------
.macro GenerateProfileHelper helper, flags
NESTED_ENTRY \helper\()Naked, _TEXT, NoHandler
// TOOD RISCV64 NYI
NESTED_END \helper\()Naked, _TEXT
.endm

GenerateProfileHelper ProfileEnter, PROFILE_ENTER
GenerateProfileHelper ProfileLeave, PROFILE_LEAVE
GenerateProfileHelper ProfileTailcall, PROFILE_TAILCALL

#endif // PROFILING_SUPPORTED


#ifdef FEATURE_TIERED_COMPILATION

NESTED_ENTRY OnCallCountThresholdReachedStub, _TEXT, NoHandler
// TOOD RISCV64 NYI
NESTED_END OnCallCountThresholdReachedStub, _TEXT

#endif // FEATURE_TIERED_COMPILATION
