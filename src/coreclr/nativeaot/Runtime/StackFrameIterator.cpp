// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "gcenv.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "RedhawkWarnings.h"
#include "rhassert.h"
#include "slist.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "holder.h"
#include "Crst.h"
#include "event.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"
#include "stressLog.h"
#include "CommonMacros.inl"

#include "shash.h"
#include "RuntimeInstance.h"
#include "rhbinder.h"

#ifdef TARGET_UNIX
#include "UnixContext.h"
#endif

// warning C4061: enumerator '{blah}' in switch of enum '{blarg}' is not explicitly handled by a case label
#pragma warning(disable:4061)

#if !defined(USE_PORTABLE_HELPERS) // @TODO: these are (currently) only implemented in assembly helpers

#if defined(FEATURE_DYNAMIC_CODE)
EXTERN_C void * RhpUniversalTransition();
GPTR_IMPL_INIT(PTR_VOID, g_RhpUniversalTransitionAddr, (void**)&RhpUniversalTransition);

EXTERN_C PTR_VOID PointerToReturnFromUniversalTransition;
GVAL_IMPL_INIT(PTR_VOID, g_ReturnFromUniversalTransitionAddr, PointerToReturnFromUniversalTransition);

EXTERN_C PTR_VOID PointerToReturnFromUniversalTransition_DebugStepTailCall;
GVAL_IMPL_INIT(PTR_VOID, g_ReturnFromUniversalTransition_DebugStepTailCallAddr, PointerToReturnFromUniversalTransition_DebugStepTailCall);
#endif

#ifdef TARGET_X86
EXTERN_C void * PointerToRhpCallFunclet2;
GVAL_IMPL_INIT(PTR_VOID, g_RhpCallFunclet2Addr, PointerToRhpCallFunclet2);
#endif
EXTERN_C void * PointerToRhpCallCatchFunclet2;
GVAL_IMPL_INIT(PTR_VOID, g_RhpCallCatchFunclet2Addr, PointerToRhpCallCatchFunclet2);
EXTERN_C void * PointerToRhpCallFinallyFunclet2;
GVAL_IMPL_INIT(PTR_VOID, g_RhpCallFinallyFunclet2Addr, PointerToRhpCallFinallyFunclet2);
EXTERN_C void * PointerToRhpCallFilterFunclet2;
GVAL_IMPL_INIT(PTR_VOID, g_RhpCallFilterFunclet2Addr, PointerToRhpCallFilterFunclet2);
EXTERN_C void * PointerToRhpThrowEx2;
GVAL_IMPL_INIT(PTR_VOID, g_RhpThrowEx2Addr, PointerToRhpThrowEx2);
EXTERN_C void * PointerToRhpThrowHwEx2;
GVAL_IMPL_INIT(PTR_VOID, g_RhpThrowHwEx2Addr, PointerToRhpThrowHwEx2);
EXTERN_C void * PointerToRhpRethrow2;
GVAL_IMPL_INIT(PTR_VOID, g_RhpRethrow2Addr, PointerToRhpRethrow2);
#endif // !defined(USE_PORTABLE_HELPERS)

// Addresses of functions in the DAC won't match their runtime counterparts so we
// assign them to globals. However it is more performant in the runtime to compare
// against immediates than to fetch the global. This macro hides the difference.
//
// We use a special code path for the return address from thunks as
// having the return address public confuses today DIA stackwalker. Before we can
// ingest the updated DIA, we're instead exposing a global void * variable
// holding the return address.
#ifdef DACCESS_COMPILE
#define EQUALS_RETURN_ADDRESS(x, func_name) ((x) == g_ ## func_name ## Addr)
#else
#define EQUALS_RETURN_ADDRESS(x, func_name) (((x)) == (PTR_VOID)PCODEToPINSTR((PCODE)PointerTo ## func_name))
#endif

#ifdef DACCESS_COMPILE
#define FAILFAST_OR_DAC_FAIL(x) if(!(x)) { DacError(E_FAIL); }
#define FAILFAST_OR_DAC_FAIL_MSG(x, msg) if(!(x)) { DacError(E_FAIL); }
#define FAILFAST_OR_DAC_FAIL_UNCONDITIONALLY(msg) DacError(E_FAIL)
#else
#define FAILFAST_OR_DAC_FAIL(x) if(!(x)) { ASSERT_UNCONDITIONALLY(#x); RhFailFast(); }
#define FAILFAST_OR_DAC_FAIL_MSG(x, msg) if(!(x)) { ASSERT_MSG((x), msg); ASSERT_UNCONDITIONALLY(#x); RhFailFast(); }
#define FAILFAST_OR_DAC_FAIL_UNCONDITIONALLY(msg) { ASSERT_UNCONDITIONALLY(msg); RhFailFast(); }
#endif

StackFrameIterator::StackFrameIterator(Thread * pThreadToWalk, PInvokeTransitionFrame* pInitialTransitionFrame)
{
    STRESS_LOG0(LF_STACKWALK, LL_INFO10000, "----Init---- [ GC ]\n");
    ASSERT(!pThreadToWalk->IsHijacked());

    if (pInitialTransitionFrame == INTERRUPTED_THREAD_MARKER)
    {
        InternalInit(pThreadToWalk, pThreadToWalk->GetInterruptedContext(), GcStackWalkFlags | ActiveStackFrame);
    }
    else
    {
        InternalInit(pThreadToWalk, pInitialTransitionFrame, GcStackWalkFlags);
    }

    PrepareToYieldFrame();
}

StackFrameIterator::StackFrameIterator(Thread * pThreadToWalk, PTR_PAL_LIMITED_CONTEXT pCtx)
{
    STRESS_LOG0(LF_STACKWALK, LL_INFO10000, "----Init with limited ctx---- [ hijack ]\n");
    InternalInit(pThreadToWalk, pCtx, 0);
    PrepareToYieldFrame();
}

StackFrameIterator::StackFrameIterator(Thread* pThreadToWalk, NATIVE_CONTEXT* pCtx)
{
    STRESS_LOG0(LF_STACKWALK, LL_INFO10000, "----Init with native ctx---- [ hijack ]\n");
    InternalInit(pThreadToWalk, pCtx, 0);
    PrepareToYieldFrame();
}

void StackFrameIterator::ResetNextExInfoForSP(uintptr_t SP)
{
    while (m_pNextExInfo && (SP > (uintptr_t)dac_cast<TADDR>(m_pNextExInfo)))
        m_pNextExInfo = m_pNextExInfo->m_pPrevExInfo;
}

void StackFrameIterator::EnterInitialInvalidState(Thread * pThreadToWalk)
{
    m_pThread = pThreadToWalk;
    m_pInstance = GetRuntimeInstance();
    m_pCodeManager = NULL;
    m_pHijackedReturnValue = NULL;
    m_HijackedReturnValueKind = GCRK_Unknown;
    m_pConservativeStackRangeLowerBound = NULL;
    m_pConservativeStackRangeUpperBound = NULL;
    m_ShouldSkipRegularGcReporting = false;
    m_pendingFuncletFramePointer = NULL;
    m_pNextExInfo = pThreadToWalk->GetCurExInfo();
    m_pPreviousTransitionFrame = NULL;
    SetControlPC(0);
}

// Prepare to start a stack walk from the context listed in the supplied PInvokeTransitionFrame.
// The supplied frame can be TOP_OF_STACK_MARKER to indicate that there are no more managed
// frames on the stack.  Otherwise, the context in the frame always describes a callsite
// where control transitioned from managed to unmanaged code.
// NOTE: When a return address hijack is executed, the PC in the generated PInvokeTransitionFrame
// matches the hijacked return address.  This PC is not guaranteed to be in managed code
// since the hijacked return address may refer to a location where an assembly thunk called
// into managed code.
// NOTE: When the PC is in an assembly thunk, this function will unwind to the next managed
// frame and may publish a conservative stack range (if and only if any of the unwound
// thunks report a conservative range).
void StackFrameIterator::InternalInit(Thread * pThreadToWalk, PInvokeTransitionFrame* pFrame, uint32_t dwFlags)
{
    // EH stackwalks are always required to unwind non-volatile floating point state.  This
    // state is never carried by PInvokeTransitionFrames, implying that they can never be used
    // as the initial state for an EH stackwalk.
    ASSERT_MSG(!(dwFlags & ApplyReturnAddressAdjustment),
        "PInvokeTransitionFrame content is not sufficient to seed an EH stackwalk");

    EnterInitialInvalidState(pThreadToWalk);

    if (pFrame == TOP_OF_STACK_MARKER)
    {
        // There are no managed frames on the stack.  Leave the iterator in its initial invalid state.
        return;
    }

    m_dwFlags = dwFlags;
    m_pPreviousTransitionFrame = pFrame;

    // We need to walk the ExInfo chain in parallel with the stackwalk so that we know when we cross over
    // exception throw points.  So we must find our initial point in the ExInfo chain here so that we can
    // properly walk it in parallel.
    ResetNextExInfoForSP((uintptr_t)dac_cast<TADDR>(pFrame));

#if !defined(USE_PORTABLE_HELPERS) // @TODO: no portable version of regdisplay
    memset(&m_RegDisplay, 0, sizeof(m_RegDisplay));
    m_RegDisplay.SetIP((PCODE)PCODEToPINSTR((PCODE)pFrame->m_RIP));
    SetControlPC(dac_cast<PTR_VOID>(m_RegDisplay.GetIP()));

    PTR_UIntNative pPreservedRegsCursor = (PTR_UIntNative)PTR_HOST_MEMBER(PInvokeTransitionFrame, pFrame, m_PreservedRegs);

#ifdef TARGET_ARM
    m_RegDisplay.pLR = (PTR_UIntNative)PTR_HOST_MEMBER(PInvokeTransitionFrame, pFrame, m_RIP);
    m_RegDisplay.pR11 = (PTR_UIntNative)PTR_HOST_MEMBER(PInvokeTransitionFrame, pFrame, m_ChainPointer);

    if (pFrame->m_Flags & PTFF_SAVE_R4)  { m_RegDisplay.pR4 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R5)  { m_RegDisplay.pR5 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R6)  { m_RegDisplay.pR6 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R7)  { m_RegDisplay.pR7 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R8)  { m_RegDisplay.pR8 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R9)  { m_RegDisplay.pR9 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R10)  { m_RegDisplay.pR10 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_SP)  { m_RegDisplay.SP  = *pPreservedRegsCursor++; }
    m_RegDisplay.pR11 = (PTR_UIntNative) PTR_HOST_MEMBER(PInvokeTransitionFrame, pFrame, m_FramePointer);
    if (pFrame->m_Flags & PTFF_SAVE_R0)  { m_RegDisplay.pR0 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R1)  { m_RegDisplay.pR1 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R2)  { m_RegDisplay.pR2 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R3)  { m_RegDisplay.pR3 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_LR)  { m_RegDisplay.pLR = pPreservedRegsCursor++; }

    if (pFrame->m_Flags & PTFF_R0_IS_GCREF)
    {
        m_pHijackedReturnValue = (PTR_OBJECTREF) m_RegDisplay.pR0;
        m_HijackedReturnValueKind = GCRK_Object;
    }
    if (pFrame->m_Flags & PTFF_R0_IS_BYREF)
    {
        m_pHijackedReturnValue = (PTR_OBJECTREF) m_RegDisplay.pR0;
        m_HijackedReturnValueKind = GCRK_Byref;
    }

#elif defined(TARGET_ARM64)
    m_RegDisplay.pFP = (PTR_UIntNative)PTR_HOST_MEMBER(PInvokeTransitionFrame, pFrame, m_FramePointer);
    m_RegDisplay.pLR = (PTR_UIntNative)PTR_HOST_MEMBER(PInvokeTransitionFrame, pFrame, m_RIP);

    ASSERT(!(pFrame->m_Flags & PTFF_SAVE_FP)); // FP should never contain a GC ref

    if (pFrame->m_Flags & PTFF_SAVE_X19) { m_RegDisplay.pX19 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X20) { m_RegDisplay.pX20 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X21) { m_RegDisplay.pX21 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X22) { m_RegDisplay.pX22 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X23) { m_RegDisplay.pX23 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X24) { m_RegDisplay.pX24 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X25) { m_RegDisplay.pX25 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X26) { m_RegDisplay.pX26 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X27) { m_RegDisplay.pX27 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X28) { m_RegDisplay.pX28 = pPreservedRegsCursor++; }

    if (pFrame->m_Flags & PTFF_SAVE_SP) { m_RegDisplay.SP = *pPreservedRegsCursor++; }

    if (pFrame->m_Flags & PTFF_SAVE_X0) { m_RegDisplay.pX0 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X1) { m_RegDisplay.pX1 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X2) { m_RegDisplay.pX2 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X3) { m_RegDisplay.pX3 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X4) { m_RegDisplay.pX4 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X5) { m_RegDisplay.pX5 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X6) { m_RegDisplay.pX6 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X7) { m_RegDisplay.pX7 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X8) { m_RegDisplay.pX8 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X9) { m_RegDisplay.pX9 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X10) { m_RegDisplay.pX10 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X11) { m_RegDisplay.pX11 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X12) { m_RegDisplay.pX12 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X13) { m_RegDisplay.pX13 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X14) { m_RegDisplay.pX14 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X15) { m_RegDisplay.pX15 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X16) { m_RegDisplay.pX16 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X17) { m_RegDisplay.pX17 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_X18) { m_RegDisplay.pX18 = pPreservedRegsCursor++; }

    if (pFrame->m_Flags & PTFF_SAVE_LR) { m_RegDisplay.pLR = pPreservedRegsCursor++; }

    GCRefKind retValueKind = TransitionFrameFlagsToReturnKind(pFrame->m_Flags);
    if (retValueKind != GCRK_Scalar)
    {
        m_pHijackedReturnValue = (PTR_OBJECTREF)m_RegDisplay.pX0;
        m_HijackedReturnValueKind = retValueKind;
    }

#else // TARGET_ARM
    if (pFrame->m_Flags & PTFF_SAVE_RBX)  { m_RegDisplay.pRbx = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_RSI)  { m_RegDisplay.pRsi = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_RDI)  { m_RegDisplay.pRdi = pPreservedRegsCursor++; }
    ASSERT(!(pFrame->m_Flags & PTFF_SAVE_RBP)); // RBP should never contain a GC ref because we require
                                                // a frame pointer for methods with pinvokes
#ifdef TARGET_AMD64
    if (pFrame->m_Flags & PTFF_SAVE_R12)  { m_RegDisplay.pR12 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R13)  { m_RegDisplay.pR13 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R14)  { m_RegDisplay.pR14 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R15)  { m_RegDisplay.pR15 = pPreservedRegsCursor++; }
#endif // TARGET_AMD64

    m_RegDisplay.pRbp = (PTR_UIntNative) PTR_HOST_MEMBER(PInvokeTransitionFrame, pFrame, m_FramePointer);

    if (pFrame->m_Flags & PTFF_SAVE_RSP)  { m_RegDisplay.SP   = *pPreservedRegsCursor++; }

    if (pFrame->m_Flags & PTFF_SAVE_RAX)  { m_RegDisplay.pRax = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_RCX)  { m_RegDisplay.pRcx = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_RDX)  { m_RegDisplay.pRdx = pPreservedRegsCursor++; }
#ifdef TARGET_AMD64
    if (pFrame->m_Flags & PTFF_SAVE_R8 )  { m_RegDisplay.pR8  = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R9 )  { m_RegDisplay.pR9  = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R10)  { m_RegDisplay.pR10 = pPreservedRegsCursor++; }
    if (pFrame->m_Flags & PTFF_SAVE_R11)  { m_RegDisplay.pR11 = pPreservedRegsCursor++; }
#endif // TARGET_AMD64

    GCRefKind retValueKind = TransitionFrameFlagsToReturnKind(pFrame->m_Flags);
    if (retValueKind != GCRK_Scalar)
    {
        m_pHijackedReturnValue = (PTR_OBJECTREF)m_RegDisplay.pRax;
        m_HijackedReturnValueKind = retValueKind;
    }

#endif // TARGET_ARM

#endif // defined(USE_PORTABLE_HELPERS)

    // This function guarantees that the final initialized context will refer to a managed
    // frame.  In the rare case where the PC does not refer to managed code (and refers to an
    // assembly thunk instead), unwind through the thunk sequence to find the nearest managed
    // frame.
    // NOTE: When thunks are present, the thunk sequence may report a conservative GC reporting
    // lower bound that must be applied when processing the managed frame.

    ReturnAddressCategory category = CategorizeUnadjustedReturnAddress(m_ControlPC);

    if (category == InManagedCode)
    {
        ASSERT(m_pInstance->IsManaged(m_ControlPC));
    }
    else if (IsNonEHThunk(category))
    {
        UnwindNonEHThunkSequence();
        ASSERT(m_pInstance->IsManaged(m_ControlPC));
    }
    else
    {
        FAILFAST_OR_DAC_FAIL_UNCONDITIONALLY("PInvokeTransitionFrame PC points to an unexpected assembly thunk kind.");
    }

    STRESS_LOG1(LF_STACKWALK, LL_INFO10000, "   %p\n", m_ControlPC);
}

#ifndef DACCESS_COMPILE

void StackFrameIterator::InternalInitForEH(Thread * pThreadToWalk, PAL_LIMITED_CONTEXT * pCtx, bool instructionFault)
{
    STRESS_LOG0(LF_STACKWALK, LL_INFO10000, "----Init---- [ EH ]\n");
    InternalInit(pThreadToWalk, pCtx, EHStackWalkFlags);

    if (instructionFault)
    {
        // We treat the IP as a return-address and adjust backward when doing EH-related things.  The faulting
        // instruction IP here will be the start of the faulting instruction and so we have the right IP for
        // EH-related things already.
        m_dwFlags &= ~ApplyReturnAddressAdjustment;
        PrepareToYieldFrame();
        m_dwFlags |= ApplyReturnAddressAdjustment;
    }
    else
    {
        PrepareToYieldFrame();
    }

    STRESS_LOG1(LF_STACKWALK, LL_INFO10000, "   %p\n", m_ControlPC);
}

void StackFrameIterator::InternalInitForStackTrace()
{
    STRESS_LOG0(LF_STACKWALK, LL_INFO10000, "----Init---- [ StackTrace ]\n");
    Thread * pThreadToWalk = ThreadStore::GetCurrentThread();
    PInvokeTransitionFrame* pFrame = pThreadToWalk->GetTransitionFrameForStackTrace();
    InternalInit(pThreadToWalk, pFrame, StackTraceStackWalkFlags);
    PrepareToYieldFrame();
}

#endif //!DACCESS_COMPILE

// Prepare to start a stack walk from the context listed in the supplied PAL_LIMITED_CONTEXT.
// The supplied context can describe a location in either managed or unmanaged code.  In the
// latter case the iterator is left in an invalid state when this function returns.
void StackFrameIterator::InternalInit(Thread * pThreadToWalk, PTR_PAL_LIMITED_CONTEXT pCtx, uint32_t dwFlags)
{
    ASSERT((dwFlags & MethodStateCalculated) == 0);

    EnterInitialInvalidState(pThreadToWalk);

    m_dwFlags = dwFlags;

    // We need to walk the ExInfo chain in parallel with the stackwalk so that we know when we cross over
    // exception throw points.  So we must find our initial point in the ExInfo chain here so that we can
    // properly walk it in parallel.
    ResetNextExInfoForSP(pCtx->GetSp());

    // This codepath is used by the hijack stackwalk and we can get arbitrary ControlPCs from there.  If this
    // context has a non-managed control PC, then we're done.
    if (!m_pInstance->IsManaged(dac_cast<PTR_VOID>(pCtx->GetIp())))
        return;

    //
    // control state
    //
    m_RegDisplay.SP   = pCtx->GetSp();
    m_RegDisplay.IP   = PCODEToPINSTR(pCtx->GetIp());
    SetControlPC(dac_cast<PTR_VOID>(m_RegDisplay.GetIP()));

#ifdef TARGET_ARM
    //
    // preserved regs
    //
    m_RegDisplay.pR4  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R4);
    m_RegDisplay.pR5  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R5);
    m_RegDisplay.pR6  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R6);
    m_RegDisplay.pR7  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R7);
    m_RegDisplay.pR8  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R8);
    m_RegDisplay.pR9  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R9);
    m_RegDisplay.pR10 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R10);
    m_RegDisplay.pR11 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R11);
    m_RegDisplay.pLR  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, LR);

    //
    // preserved vfp regs
    //
    for (int32_t i = 0; i < 16 - 8; i++)
    {
        m_RegDisplay.D[i] = pCtx->D[i];
    }
    //
    // scratch regs
    //
    m_RegDisplay.pR0  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R0);

#elif defined(TARGET_ARM64)
    //
    // preserved regs
    //
    m_RegDisplay.pX19 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, X19);
    m_RegDisplay.pX20 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, X20);
    m_RegDisplay.pX21 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, X21);
    m_RegDisplay.pX22 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, X22);
    m_RegDisplay.pX23 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, X23);
    m_RegDisplay.pX24 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, X24);
    m_RegDisplay.pX25 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, X25);
    m_RegDisplay.pX26 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, X26);
    m_RegDisplay.pX27 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, X27);
    m_RegDisplay.pX28 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, X28);
    m_RegDisplay.pFP = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, FP);
    m_RegDisplay.pLR = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, LR);

    //
    // preserved vfp regs
    //
    for (int32_t i = 0; i < 16 - 8; i++)
    {
        m_RegDisplay.D[i] = pCtx->D[i];
    }
    //
    // scratch regs
    //
    m_RegDisplay.pX0 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, X0);
    m_RegDisplay.pX1 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, X1);
    // TODO: Copy X2-X7 when we start supporting HVA's

#elif defined(UNIX_AMD64_ABI)
    //
    // preserved regs
    //
    m_RegDisplay.pRbp = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, Rbp);
    m_RegDisplay.pRbx = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, Rbx);
    m_RegDisplay.pR12 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R12);
    m_RegDisplay.pR13 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R13);
    m_RegDisplay.pR14 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R14);
    m_RegDisplay.pR15 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R15);

    //
    // scratch regs
    //
    m_RegDisplay.pRax = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, Rax);
    m_RegDisplay.pRcx = NULL;
    m_RegDisplay.pRdx = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, Rdx);
    m_RegDisplay.pRsi = NULL;
    m_RegDisplay.pRdi = NULL;
    m_RegDisplay.pR8  = NULL;
    m_RegDisplay.pR9  = NULL;
    m_RegDisplay.pR10 = NULL;
    m_RegDisplay.pR11 = NULL;

#elif defined(TARGET_X86) || defined(TARGET_AMD64)
    //
    // preserved regs
    //
    m_RegDisplay.pRbp = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, Rbp);
    m_RegDisplay.pRsi = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, Rsi);
    m_RegDisplay.pRdi = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, Rdi);
    m_RegDisplay.pRbx = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, Rbx);
#ifdef TARGET_AMD64
    m_RegDisplay.pR12 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R12);
    m_RegDisplay.pR13 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R13);
    m_RegDisplay.pR14 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R14);
    m_RegDisplay.pR15 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, R15);
    //
    // preserved xmm regs
    //
    memcpy(m_RegDisplay.Xmm, &pCtx->Xmm6, sizeof(m_RegDisplay.Xmm));
#endif // TARGET_AMD64

    //
    // scratch regs
    //
    m_RegDisplay.pRax = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pCtx, Rax);
    m_RegDisplay.pRcx = NULL;
    m_RegDisplay.pRdx = NULL;
#ifdef TARGET_AMD64
    m_RegDisplay.pR8  = NULL;
    m_RegDisplay.pR9  = NULL;
    m_RegDisplay.pR10 = NULL;
    m_RegDisplay.pR11 = NULL;
#endif // TARGET_AMD64
#else
    PORTABILITY_ASSERT("StackFrameIterator::InternalInit");
#endif // TARGET_ARM
}

// Prepare to start a stack walk from the context listed in the supplied NATIVE_CONTEXT.
// The supplied context can describe a location in managed code.
void StackFrameIterator::InternalInit(Thread * pThreadToWalk, NATIVE_CONTEXT* pCtx, uint32_t dwFlags)
{
    ASSERT((dwFlags & MethodStateCalculated) == 0);

    EnterInitialInvalidState(pThreadToWalk);

    m_dwFlags = dwFlags;

    // We need to walk the ExInfo chain in parallel with the stackwalk so that we know when we cross over
    // exception throw points.  So we must find our initial point in the ExInfo chain here so that we can
    // properly walk it in parallel.
    ResetNextExInfoForSP(pCtx->GetSp());

    // This codepath is used by the hijack stackwalk. The IP must be in managed code.
    ASSERT(m_pInstance->IsManaged(dac_cast<PTR_VOID>(pCtx->GetIp())));

    //
    // control state
    //
    SetControlPC(dac_cast<PTR_VOID>(pCtx->GetIp()));
    m_RegDisplay.SP   = pCtx->GetSp();
    m_RegDisplay.IP   = pCtx->GetIp();

#ifdef TARGET_UNIX
#define PTR_TO_REG(ptr, reg) (&((ptr)->reg()))
#else
#define PTR_TO_REG(ptr, reg) (&((ptr)->reg))
#endif

#ifdef TARGET_ARM64

    //
    // preserved regs
    //
    m_RegDisplay.pX19 = (PTR_UIntNative)PTR_TO_REG(pCtx, X19);
    m_RegDisplay.pX20 = (PTR_UIntNative)PTR_TO_REG(pCtx, X20);
    m_RegDisplay.pX21 = (PTR_UIntNative)PTR_TO_REG(pCtx, X21);
    m_RegDisplay.pX22 = (PTR_UIntNative)PTR_TO_REG(pCtx, X22);
    m_RegDisplay.pX23 = (PTR_UIntNative)PTR_TO_REG(pCtx, X23);
    m_RegDisplay.pX24 = (PTR_UIntNative)PTR_TO_REG(pCtx, X24);
    m_RegDisplay.pX25 = (PTR_UIntNative)PTR_TO_REG(pCtx, X25);
    m_RegDisplay.pX26 = (PTR_UIntNative)PTR_TO_REG(pCtx, X26);
    m_RegDisplay.pX27 = (PTR_UIntNative)PTR_TO_REG(pCtx, X27);
    m_RegDisplay.pX28 = (PTR_UIntNative)PTR_TO_REG(pCtx, X28);
    m_RegDisplay.pFP = (PTR_UIntNative)PTR_TO_REG(pCtx, Fp);
    m_RegDisplay.pLR = (PTR_UIntNative)PTR_TO_REG(pCtx, Lr);

    //
    // scratch regs
    //
    m_RegDisplay.pX0 = (PTR_UIntNative)PTR_TO_REG(pCtx, X0);
    m_RegDisplay.pX1 = (PTR_UIntNative)PTR_TO_REG(pCtx, X1);
    m_RegDisplay.pX2 = (PTR_UIntNative)PTR_TO_REG(pCtx, X2);
    m_RegDisplay.pX3 = (PTR_UIntNative)PTR_TO_REG(pCtx, X3);
    m_RegDisplay.pX4 = (PTR_UIntNative)PTR_TO_REG(pCtx, X4);
    m_RegDisplay.pX5 = (PTR_UIntNative)PTR_TO_REG(pCtx, X5);
    m_RegDisplay.pX6 = (PTR_UIntNative)PTR_TO_REG(pCtx, X6);
    m_RegDisplay.pX7 = (PTR_UIntNative)PTR_TO_REG(pCtx, X7);
    m_RegDisplay.pX8 = (PTR_UIntNative)PTR_TO_REG(pCtx, X8);
    m_RegDisplay.pX9 = (PTR_UIntNative)PTR_TO_REG(pCtx, X9);
    m_RegDisplay.pX10 = (PTR_UIntNative)PTR_TO_REG(pCtx, X10);
    m_RegDisplay.pX11 = (PTR_UIntNative)PTR_TO_REG(pCtx, X11);
    m_RegDisplay.pX12 = (PTR_UIntNative)PTR_TO_REG(pCtx, X12);
    m_RegDisplay.pX13 = (PTR_UIntNative)PTR_TO_REG(pCtx, X13);
    m_RegDisplay.pX14 = (PTR_UIntNative)PTR_TO_REG(pCtx, X14);
    m_RegDisplay.pX15 = (PTR_UIntNative)PTR_TO_REG(pCtx, X15);
    m_RegDisplay.pX16 = (PTR_UIntNative)PTR_TO_REG(pCtx, X16);
    m_RegDisplay.pX17 = (PTR_UIntNative)PTR_TO_REG(pCtx, X17);
    m_RegDisplay.pX18 = (PTR_UIntNative)PTR_TO_REG(pCtx, X18);

#elif defined(TARGET_AMD64)

    //
    // preserved regs
    //
    m_RegDisplay.pRbp = (PTR_UIntNative)PTR_TO_REG(pCtx, Rbp);
    m_RegDisplay.pRsi = (PTR_UIntNative)PTR_TO_REG(pCtx, Rsi);
    m_RegDisplay.pRdi = (PTR_UIntNative)PTR_TO_REG(pCtx, Rdi);
    m_RegDisplay.pRbx = (PTR_UIntNative)PTR_TO_REG(pCtx, Rbx);
    m_RegDisplay.pR12 = (PTR_UIntNative)PTR_TO_REG(pCtx, R12);
    m_RegDisplay.pR13 = (PTR_UIntNative)PTR_TO_REG(pCtx, R13);
    m_RegDisplay.pR14 = (PTR_UIntNative)PTR_TO_REG(pCtx, R14);
    m_RegDisplay.pR15 = (PTR_UIntNative)PTR_TO_REG(pCtx, R15);

    //
    // scratch regs
    //
    m_RegDisplay.pRax = (PTR_UIntNative)PTR_TO_REG(pCtx, Rax);
    m_RegDisplay.pRcx = (PTR_UIntNative)PTR_TO_REG(pCtx, Rcx);
    m_RegDisplay.pRdx = (PTR_UIntNative)PTR_TO_REG(pCtx, Rdx);
    m_RegDisplay.pR8  = (PTR_UIntNative)PTR_TO_REG(pCtx, R8);
    m_RegDisplay.pR9  = (PTR_UIntNative)PTR_TO_REG(pCtx, R9);
    m_RegDisplay.pR10 = (PTR_UIntNative)PTR_TO_REG(pCtx, R10);
    m_RegDisplay.pR11 = (PTR_UIntNative)PTR_TO_REG(pCtx, R11);
#elif defined(TARGET_X86)

    //
    // preserved regs
    //
    m_RegDisplay.pRbp = (PTR_UIntNative)PTR_TO_REG(pCtx, Ebp);
    m_RegDisplay.pRsi = (PTR_UIntNative)PTR_TO_REG(pCtx, Esi);
    m_RegDisplay.pRdi = (PTR_UIntNative)PTR_TO_REG(pCtx, Edi);
    m_RegDisplay.pRbx = (PTR_UIntNative)PTR_TO_REG(pCtx, Ebx);

    //
    // scratch regs
    //
    m_RegDisplay.pRax = (PTR_UIntNative)PTR_TO_REG(pCtx, Eax);
    m_RegDisplay.pRcx = (PTR_UIntNative)PTR_TO_REG(pCtx, Ecx);
    m_RegDisplay.pRdx = (PTR_UIntNative)PTR_TO_REG(pCtx, Edx);
#elif defined(TARGET_ARM)

    m_RegDisplay.pR0 = (PTR_UIntNative)PTR_TO_REG(pCtx, R0);
    m_RegDisplay.pR1 = (PTR_UIntNative)PTR_TO_REG(pCtx, R1);
    m_RegDisplay.pR2 = (PTR_UIntNative)PTR_TO_REG(pCtx, R2);
    m_RegDisplay.pR3 = (PTR_UIntNative)PTR_TO_REG(pCtx, R3);
    m_RegDisplay.pR4 = (PTR_UIntNative)PTR_TO_REG(pCtx, R4);
    m_RegDisplay.pR5 = (PTR_UIntNative)PTR_TO_REG(pCtx, R5);
    m_RegDisplay.pR6 = (PTR_UIntNative)PTR_TO_REG(pCtx, R6);
    m_RegDisplay.pR7 = (PTR_UIntNative)PTR_TO_REG(pCtx, R7);
    m_RegDisplay.pR8 = (PTR_UIntNative)PTR_TO_REG(pCtx, R8);
    m_RegDisplay.pR9 = (PTR_UIntNative)PTR_TO_REG(pCtx, R9);
    m_RegDisplay.pR10 = (PTR_UIntNative)PTR_TO_REG(pCtx, R10);
    m_RegDisplay.pR11 = (PTR_UIntNative)PTR_TO_REG(pCtx, R11);
    m_RegDisplay.pLR = (PTR_UIntNative)PTR_TO_REG(pCtx, Lr);
#else
    PORTABILITY_ASSERT("StackFrameIterator::InternalInit");
#endif // TARGET_ARM

#undef PTR_TO_REG
}

PTR_VOID StackFrameIterator::HandleExCollide(PTR_ExInfo pExInfo)
{
    STRESS_LOG3(LF_STACKWALK, LL_INFO10000, "   [ ex collide ] kind = %d, pass = %d, idxCurClause = %d\n",
                pExInfo->m_kind, pExInfo->m_passNumber, pExInfo->m_idxCurClause);

    PTR_VOID collapsingTargetFrame = NULL;
    uint32_t curFlags = m_dwFlags;

    // Capture and clear the pending funclet frame pointer (if any).  This field is only set
    // when stack walks collide with active exception dispatch, and only exists to save the
    // funclet frame pointer until the next ExInfo collision (which has now occurred).
    PTR_VOID activeFuncletFramePointer = m_pendingFuncletFramePointer;
    m_pendingFuncletFramePointer = NULL;

    // If we aren't invoking a funclet (i.e. idxCurClause == -1), and we're doing a GC stackwalk, we don't
    // want the 2nd-pass collided behavior because that behavior assumes that the previous frame was a
    // funclet, which isn't the case when taking a GC at some points in the EH dispatch code.  So we treat it
    // as if the 2nd pass hasn't actually started yet.
    if ((pExInfo->m_passNumber == 1) ||
        (pExInfo->m_idxCurClause == 0xFFFFFFFF))
    {
        FAILFAST_OR_DAC_FAIL_MSG(!(curFlags & ApplyReturnAddressAdjustment),
            "did not expect to collide with a 1st-pass ExInfo during a EH stackwalk");
        InternalInit(m_pThread, pExInfo->m_pExContext, curFlags);
        m_pNextExInfo = pExInfo->m_pPrevExInfo;
        CalculateCurrentMethodState();
        ASSERT(IsValid());

        if ((pExInfo->m_kind & EK_HardwareFault) && (curFlags & RemapHardwareFaultsToSafePoint))
            m_effectiveSafePointAddress = GetCodeManager()->RemapHardwareFaultToGCSafePoint(&m_methodInfo, m_ControlPC);
    }
    else
    {
        ASSERT_MSG(activeFuncletFramePointer != NULL,
            "collided with an active funclet invoke but the funclet frame pointer is unknown");

        //
        // Copy our state from the previous StackFrameIterator
        //
        this->UpdateFromExceptionDispatch((PTR_StackFrameIterator)&pExInfo->m_frameIter);

        // Sync our 'current' ExInfo with the updated state (we may have skipped other dispatches)
        ResetNextExInfoForSP(m_RegDisplay.GetSP());

        // In case m_ControlPC is pre-adjusted, counteract here, since the caller of this routine
        // will apply the adjustment again once we return. If the m_ControlPC is not pre-adjusted,
        // this is simply an no-op.
        m_ControlPC = m_OriginalControlPC;

        m_dwFlags = curFlags;

        // The iterator has been moved to the "owner frame" (either a parent funclet or the main
        // code body) of the funclet being invoked by this ExInfo.  As a result, both the active
        // funclet and the current frame must be "part of the same function" and therefore must
        // have identical frame pointer values.

        CalculateCurrentMethodState();
        ASSERT(IsValid());
        ASSERT(m_FramePointer == activeFuncletFramePointer);

        if ((m_ControlPC != 0) &&           // the dispatch in ExInfo could have gone unhandled
            (m_dwFlags & CollapseFunclets))
        {
            // GC stack walks must skip the owner frame since GC information for the entire function
            // has already been reported by the leafmost active funclet.  In general, the GC stack walk
            // must skip all parent frames that are "part of the same function" (i.e., have the same
            // frame pointer).
            collapsingTargetFrame = activeFuncletFramePointer;
        }
    }
    return collapsingTargetFrame;
}

void StackFrameIterator::UpdateFromExceptionDispatch(PTR_StackFrameIterator pSourceIterator)
{
    ASSERT(m_pendingFuncletFramePointer == NULL);
    PreservedRegPtrs thisFuncletPtrs = this->m_funcletPtrs;

    // Blast over 'this' with everything from the 'source'.
    *this = *pSourceIterator;

    // Clear the funclet frame pointer (if any) that was loaded from the previous iterator.
    // This field does not relate to the transferrable state of the previous iterator (it
    // instead tracks the frame-by-frame progression of a particular iterator instance) and
    // therefore has no meaning in the context of the current stack walk.
    m_pendingFuncletFramePointer = NULL;

    // Then, put back the pointers to the funclet's preserved registers (since those are the correct values
    // until the funclet completes, at which point the values will be copied back to the ExInfo's REGDISPLAY).

#ifdef TARGET_ARM
    m_RegDisplay.pR4  = thisFuncletPtrs.pR4 ;
    m_RegDisplay.pR5  = thisFuncletPtrs.pR5 ;
    m_RegDisplay.pR6  = thisFuncletPtrs.pR6 ;
    m_RegDisplay.pR7  = thisFuncletPtrs.pR7 ;
    m_RegDisplay.pR8  = thisFuncletPtrs.pR8 ;
    m_RegDisplay.pR9  = thisFuncletPtrs.pR9 ;
    m_RegDisplay.pR10 = thisFuncletPtrs.pR10;
    m_RegDisplay.pR11 = thisFuncletPtrs.pR11;

#elif defined(TARGET_ARM64)
    m_RegDisplay.pX19 = thisFuncletPtrs.pX19;
    m_RegDisplay.pX20 = thisFuncletPtrs.pX20;
    m_RegDisplay.pX21 = thisFuncletPtrs.pX21;
    m_RegDisplay.pX22 = thisFuncletPtrs.pX22;
    m_RegDisplay.pX23 = thisFuncletPtrs.pX23;
    m_RegDisplay.pX24 = thisFuncletPtrs.pX24;
    m_RegDisplay.pX25 = thisFuncletPtrs.pX25;
    m_RegDisplay.pX26 = thisFuncletPtrs.pX26;
    m_RegDisplay.pX27 = thisFuncletPtrs.pX27;
    m_RegDisplay.pX28 = thisFuncletPtrs.pX28;
    m_RegDisplay.pFP = thisFuncletPtrs.pFP;

#elif defined(UNIX_AMD64_ABI)
    // Save the preserved regs portion of the REGDISPLAY across the unwind through the C# EH dispatch code.
    m_RegDisplay.pRbp = thisFuncletPtrs.pRbp;
    m_RegDisplay.pRbx = thisFuncletPtrs.pRbx;
    m_RegDisplay.pR12 = thisFuncletPtrs.pR12;
    m_RegDisplay.pR13 = thisFuncletPtrs.pR13;
    m_RegDisplay.pR14 = thisFuncletPtrs.pR14;
    m_RegDisplay.pR15 = thisFuncletPtrs.pR15;

#elif defined(TARGET_X86) || defined(TARGET_AMD64)
    // Save the preserved regs portion of the REGDISPLAY across the unwind through the C# EH dispatch code.
    m_RegDisplay.pRbp = thisFuncletPtrs.pRbp;
    m_RegDisplay.pRdi = thisFuncletPtrs.pRdi;
    m_RegDisplay.pRsi = thisFuncletPtrs.pRsi;
    m_RegDisplay.pRbx = thisFuncletPtrs.pRbx;
#ifdef TARGET_AMD64
    m_RegDisplay.pR12 = thisFuncletPtrs.pR12;
    m_RegDisplay.pR13 = thisFuncletPtrs.pR13;
    m_RegDisplay.pR14 = thisFuncletPtrs.pR14;
    m_RegDisplay.pR15 = thisFuncletPtrs.pR15;
#endif // TARGET_AMD64
#else
    PORTABILITY_ASSERT("StackFrameIterator::UpdateFromExceptionDispatch");
#endif
}

#ifdef TARGET_AMD64
typedef DPTR(Fp128) PTR_Fp128;
#endif

// The invoke of a funclet is a bit special and requires an assembly thunk, but we don't want to break the
// stackwalk due to this.  So this routine will unwind through the assembly thunks used to invoke funclets.
// It's also used to disambiguate exceptionally- and non-exceptionally-invoked funclets.
void StackFrameIterator::UnwindFuncletInvokeThunk()
{
    ASSERT((m_dwFlags & MethodStateCalculated) == 0);

#if defined(USE_PORTABLE_HELPERS) // @TODO: Currently no funclet invoke defined in a portable way
    return;
#else // defined(USE_PORTABLE_HELPERS)
    ASSERT((CategorizeUnadjustedReturnAddress(m_ControlPC) == InFuncletInvokeThunk) || 
           (CategorizeUnadjustedReturnAddress(m_ControlPC) == InFilterFuncletInvokeThunk));

    PTR_UIntNative SP;

#ifdef TARGET_X86
    // First, unwind RhpCallFunclet
    SP = (PTR_UIntNative)(m_RegDisplay.SP + 0x4);   // skip the saved assembly-routine-EBP
    m_RegDisplay.SetIP(*SP++);
    m_RegDisplay.SetSP((uintptr_t)dac_cast<TADDR>(SP));
    SetControlPC(dac_cast<PTR_VOID>(m_RegDisplay.GetIP()));

    ASSERT(
        EQUALS_RETURN_ADDRESS(m_ControlPC, RhpCallCatchFunclet2) ||
        EQUALS_RETURN_ADDRESS(m_ControlPC, RhpCallFinallyFunclet2) ||
        EQUALS_RETURN_ADDRESS(m_ControlPC, RhpCallFilterFunclet2)
        );
#endif

    bool isFilterInvoke = EQUALS_RETURN_ADDRESS(m_ControlPC, RhpCallFilterFunclet2);

#if defined(UNIX_AMD64_ABI)
    SP = (PTR_UIntNative)(m_RegDisplay.SP);

    if (isFilterInvoke)
    {
        SP++; // stack alignment
    }
    else
    {
        // Save the preserved regs portion of the REGDISPLAY across the unwind through the C# EH dispatch code.
        m_funcletPtrs.pRbp = m_RegDisplay.pRbp;
        m_funcletPtrs.pRbx = m_RegDisplay.pRbx;
        m_funcletPtrs.pR12 = m_RegDisplay.pR12;
        m_funcletPtrs.pR13 = m_RegDisplay.pR13;
        m_funcletPtrs.pR14 = m_RegDisplay.pR14;
        m_funcletPtrs.pR15 = m_RegDisplay.pR15;

        if (EQUALS_RETURN_ADDRESS(m_ControlPC, RhpCallCatchFunclet2))
        {
            SP += 6 + 1; // 6 locals and stack alignment
        }
        else
        {
            SP += 3; // 3 locals
        }
    }

    m_RegDisplay.pRbp = SP++;
    m_RegDisplay.pRbx = SP++;
    m_RegDisplay.pR12 = SP++;
    m_RegDisplay.pR13 = SP++;
    m_RegDisplay.pR14 = SP++;
    m_RegDisplay.pR15 = SP++;
#elif defined(TARGET_AMD64)
    static const int ArgumentsScratchAreaSize = 4 * 8;

    PTR_Fp128 xmm = (PTR_Fp128)(m_RegDisplay.SP + ArgumentsScratchAreaSize);

    for (int i = 0; i < 10; i++)
    {
        m_RegDisplay.Xmm[i] = *xmm++;
    }

    SP = (PTR_UIntNative)xmm;

    if (isFilterInvoke)
    {
        SP++; // stack alignment
    }
    else
    {
        // Save the preserved regs portion of the REGDISPLAY across the unwind through the C# EH dispatch code.
        m_funcletPtrs.pRbp = m_RegDisplay.pRbp;
        m_funcletPtrs.pRdi = m_RegDisplay.pRdi;
        m_funcletPtrs.pRsi = m_RegDisplay.pRsi;
        m_funcletPtrs.pRbx = m_RegDisplay.pRbx;
        m_funcletPtrs.pR12 = m_RegDisplay.pR12;
        m_funcletPtrs.pR13 = m_RegDisplay.pR13;
        m_funcletPtrs.pR14 = m_RegDisplay.pR14;
        m_funcletPtrs.pR15 = m_RegDisplay.pR15;

        if (EQUALS_RETURN_ADDRESS(m_ControlPC, RhpCallCatchFunclet2))
        {
            SP += 3; // 3 locals
        }
        else
        {
            SP++; // 1 local
        }
    }

    m_RegDisplay.pRbp = SP++;
    m_RegDisplay.pRdi = SP++;
    m_RegDisplay.pRsi = SP++;
    m_RegDisplay.pRbx = SP++;
    m_RegDisplay.pR12 = SP++;
    m_RegDisplay.pR13 = SP++;
    m_RegDisplay.pR14 = SP++;
    m_RegDisplay.pR15 = SP++;

#elif defined(TARGET_X86)
    SP = (PTR_UIntNative)(m_RegDisplay.SP);

    if (!isFilterInvoke)
    {
        // Save the preserved regs portion of the REGDISPLAY across the unwind through the C# EH dispatch code.
        m_funcletPtrs.pRbp = m_RegDisplay.pRbp;
        m_funcletPtrs.pRdi = m_RegDisplay.pRdi;
        m_funcletPtrs.pRsi = m_RegDisplay.pRsi;
        m_funcletPtrs.pRbx = m_RegDisplay.pRbx;
    }

    if (EQUALS_RETURN_ADDRESS(m_ControlPC, RhpCallCatchFunclet2))
    {
        SP += 2; // 2 locals
    }
    else
    {
        SP++; // 1 local
    }
    m_RegDisplay.pRdi = SP++;
    m_RegDisplay.pRsi = SP++;
    m_RegDisplay.pRbx = SP++;
    m_RegDisplay.pRbp = SP++;
#elif defined(TARGET_ARM)

    PTR_UInt64 d = (PTR_UInt64)(m_RegDisplay.SP);

    for (int i = 0; i < 8; i++)
    {
        m_RegDisplay.D[i] = *d++;
    }

    SP = (PTR_UIntNative)d;

    // RhpCallCatchFunclet puts a couple of extra things on the stack that aren't put there by the other two
    // thunks, but we don't need to know what they are here, so we just skip them.
    SP += EQUALS_RETURN_ADDRESS(m_ControlPC, RhpCallCatchFunclet2) ? 3 : 1;

    if (!isFilterInvoke)
    {
        // Save the preserved regs portion of the REGDISPLAY across the unwind through the C# EH dispatch code.
        m_funcletPtrs.pR4  = m_RegDisplay.pR4;
        m_funcletPtrs.pR5  = m_RegDisplay.pR5;
        m_funcletPtrs.pR6  = m_RegDisplay.pR6;
        m_funcletPtrs.pR7  = m_RegDisplay.pR7;
        m_funcletPtrs.pR8  = m_RegDisplay.pR8;
        m_funcletPtrs.pR9  = m_RegDisplay.pR9;
        m_funcletPtrs.pR10 = m_RegDisplay.pR10;
        m_funcletPtrs.pR11 = m_RegDisplay.pR11;
    }

    m_RegDisplay.pR4 = SP++;
    m_RegDisplay.pR5 = SP++;
    m_RegDisplay.pR6 = SP++;
    m_RegDisplay.pR7 = SP++;
    m_RegDisplay.pR8 = SP++;
    m_RegDisplay.pR9 = SP++;
    m_RegDisplay.pR10 = SP++;
    m_RegDisplay.pR11 = SP++;

#elif defined(TARGET_ARM64)
    PTR_UInt64 d = (PTR_UInt64)(m_RegDisplay.SP);

    for (int i = 0; i < 8; i++)
    {
        m_RegDisplay.D[i] = *d++;
    }

    SP = (PTR_UIntNative)d;

    if (!isFilterInvoke)
    {
        // RhpCallCatchFunclet puts a couple of extra things on the stack that aren't put there by the other two
        // thunks, but we don't need to know what they are here, so we just skip them.
        SP += EQUALS_RETURN_ADDRESS(m_ControlPC, RhpCallCatchFunclet2) ? 6 : 4;

        // Save the preserved regs portion of the REGDISPLAY across the unwind through the C# EH dispatch code.
        m_funcletPtrs.pX19  = m_RegDisplay.pX19;
        m_funcletPtrs.pX20  = m_RegDisplay.pX20;
        m_funcletPtrs.pX21  = m_RegDisplay.pX21;
        m_funcletPtrs.pX22  = m_RegDisplay.pX22;
        m_funcletPtrs.pX23  = m_RegDisplay.pX23;
        m_funcletPtrs.pX24  = m_RegDisplay.pX24;
        m_funcletPtrs.pX25  = m_RegDisplay.pX25;
        m_funcletPtrs.pX26  = m_RegDisplay.pX26;
        m_funcletPtrs.pX27  = m_RegDisplay.pX27;
        m_funcletPtrs.pX28  = m_RegDisplay.pX28;
        m_funcletPtrs.pFP   = m_RegDisplay.pFP;
    }

    m_RegDisplay.pFP  = SP++;

    m_RegDisplay.SetIP(*SP++);

    m_RegDisplay.pX19 = SP++;
    m_RegDisplay.pX20 = SP++;
    m_RegDisplay.pX21 = SP++;
    m_RegDisplay.pX22 = SP++;
    m_RegDisplay.pX23 = SP++;
    m_RegDisplay.pX24 = SP++;
    m_RegDisplay.pX25 = SP++;
    m_RegDisplay.pX26 = SP++;
    m_RegDisplay.pX27 = SP++;
    m_RegDisplay.pX28 = SP++;

#else
    SP = (PTR_UIntNative)(m_RegDisplay.SP);
    ASSERT_UNCONDITIONALLY("NYI for this arch");
#endif

#if !defined(TARGET_ARM64)
    m_RegDisplay.SetIP(PCODEToPINSTR(*SP++));
#endif

    m_RegDisplay.SetSP((uintptr_t)dac_cast<TADDR>(SP));
    SetControlPC(dac_cast<PTR_VOID>(m_RegDisplay.GetIP()));

    // We expect to be called by the runtime's C# EH implementation, and since this function's notion of how
    // to unwind through the stub is brittle relative to the stub itself, we want to check as soon as we can.
    ASSERT(m_pInstance->IsManaged(m_ControlPC) && "unwind from funclet invoke stub failed");
#endif // defined(USE_PORTABLE_HELPERS)
}

// For a given target architecture, the layout of this structure must precisely match the
// stack frame layout used by the associated architecture-specific RhpUniversalTransition
// implementation.
struct UniversalTransitionStackFrame
{

// In DAC builds, the "this" pointer refers to an object in the DAC host.
#define GET_POINTER_TO_FIELD(_FieldName) \
    (PTR_UIntNative)PTR_HOST_MEMBER(UniversalTransitionStackFrame, this, _FieldName)

#if defined(UNIX_AMD64_ABI)

    // Conservative GC reporting must be applied to everything between the base of the
    // ReturnBlock and the top of the StackPassedArgs.
private:
    Fp128 m_fpArgRegs[8];                   // ChildSP+000 CallerSP-0D0 (0x80 bytes)    (xmm0-xmm7)
    uintptr_t m_returnBlock[2];            // ChildSP+080 CallerSP-050 (0x10 bytes)
    uintptr_t m_intArgRegs[6];             // ChildSP+090 CallerSP-040 (0x30 bytes)    (rdi,rsi,rcx,rdx,r8,r9)
    uintptr_t m_alignmentPad;              // ChildSP+0C0 CallerSP-010 (0x8 bytes)
    uintptr_t m_callerRetaddr;             // ChildSP+0C8 CallerSP-008 (0x8 bytes)
    uintptr_t m_stackPassedArgs[1];        // ChildSP+0D0 CallerSP+000 (unknown size)

public:
    PTR_UIntNative get_CallerSP() { return GET_POINTER_TO_FIELD(m_stackPassedArgs[0]); }
    PTR_UIntNative get_AddressOfPushedCallerIP() { return GET_POINTER_TO_FIELD(m_callerRetaddr); }
    PTR_UIntNative get_LowerBoundForConservativeReporting() { return GET_POINTER_TO_FIELD(m_returnBlock[0]); }

    void UnwindNonVolatileRegisters(REGDISPLAY * pRegisterSet)
    {
        // RhpUniversalTransition does not touch any non-volatile state on amd64.
        UNREFERENCED_PARAMETER(pRegisterSet);
    }

#elif defined(TARGET_AMD64)

    // Conservative GC reporting must be applied to everything between the base of the
    // ReturnBlock and the top of the StackPassedArgs.
private:
    uintptr_t m_calleeArgumentHomes[4];    // ChildSP+000 CallerSP-080 (0x20 bytes)
    Fp128 m_fpArgRegs[4];                   // ChildSP+020 CallerSP-060 (0x40 bytes)    (xmm0-xmm3)
    uintptr_t m_returnBlock[2];            // ChildSP+060 CallerSP-020 (0x10 bytes)
    uintptr_t m_alignmentPad;              // ChildSP+070 CallerSP-010 (0x8 bytes)
    uintptr_t m_callerRetaddr;             // ChildSP+078 CallerSP-008 (0x8 bytes)
    uintptr_t m_intArgRegs[4];             // ChildSP+080 CallerSP+000 (0x20 bytes)    (rcx,rdx,r8,r9)
    uintptr_t m_stackPassedArgs[1];        // ChildSP+0a0 CallerSP+020 (unknown size)

public:
    PTR_UIntNative get_CallerSP() { return GET_POINTER_TO_FIELD(m_intArgRegs[0]); }
    PTR_UIntNative get_AddressOfPushedCallerIP() { return GET_POINTER_TO_FIELD(m_callerRetaddr); }
    PTR_UIntNative get_LowerBoundForConservativeReporting() { return GET_POINTER_TO_FIELD(m_returnBlock[0]); }

    void UnwindNonVolatileRegisters(REGDISPLAY * pRegisterSet)
    {
        // RhpUniversalTransition does not touch any non-volatile state on amd64.
        UNREFERENCED_PARAMETER(pRegisterSet);
    }

#elif defined(TARGET_ARM)

    // Conservative GC reporting must be applied to everything between the base of the
    // ReturnBlock and the top of the StackPassedArgs.
private:
    uintptr_t m_pushedR11;                 // ChildSP+000 CallerSP-078 (0x4 bytes)     (r11)
    uintptr_t m_pushedLR;                  // ChildSP+004 CallerSP-074 (0x4 bytes)     (lr)
    uint64_t m_fpArgRegs[8];                  // ChildSP+008 CallerSP-070 (0x40 bytes)    (d0-d7)
    uint64_t m_returnBlock[4];                // ChildSP+048 CallerSP-030 (0x20 bytes)
    uintptr_t m_intArgRegs[4];             // ChildSP+068 CallerSP-010 (0x10 bytes)    (r0-r3)
    uintptr_t m_stackPassedArgs[1];        // ChildSP+078 CallerSP+000 (unknown size)

public:
    PTR_UIntNative get_CallerSP() { return GET_POINTER_TO_FIELD(m_stackPassedArgs[0]); }
    PTR_UIntNative get_AddressOfPushedCallerIP() { return GET_POINTER_TO_FIELD(m_pushedLR); }
    PTR_UIntNative get_LowerBoundForConservativeReporting() { return GET_POINTER_TO_FIELD(m_returnBlock[0]); }

    void UnwindNonVolatileRegisters(REGDISPLAY * pRegisterSet)
    {
        pRegisterSet->pR11 = GET_POINTER_TO_FIELD(m_pushedR11);
    }

#elif defined(TARGET_X86)

    // Conservative GC reporting must be applied to everything between the base of the
    // IntArgRegs and the top of the StackPassedArgs.
private:
    uintptr_t m_intArgRegs[2];             // ChildSP+000 CallerSP-018 (0x8 bytes)     (edx,ecx)
    uintptr_t m_returnBlock[2];            // ChildSP+008 CallerSP-010 (0x8 bytes)
    uintptr_t m_pushedEBP;                 // ChildSP+010 CallerSP-008 (0x4 bytes)
    uintptr_t m_callerRetaddr;             // ChildSP+014 CallerSP-004 (0x4 bytes)
    uintptr_t m_stackPassedArgs[1];        // ChildSP+018 CallerSP+000 (unknown size)

public:
    PTR_UIntNative get_CallerSP() { return GET_POINTER_TO_FIELD(m_stackPassedArgs[0]); }
    PTR_UIntNative get_AddressOfPushedCallerIP() { return GET_POINTER_TO_FIELD(m_callerRetaddr); }
    PTR_UIntNative get_LowerBoundForConservativeReporting() { return GET_POINTER_TO_FIELD(m_intArgRegs[0]); }

    void UnwindNonVolatileRegisters(REGDISPLAY * pRegisterSet)
    {
        pRegisterSet->pRbp = GET_POINTER_TO_FIELD(m_pushedEBP);
    }

#elif defined(TARGET_ARM64)

    // Conservative GC reporting must be applied to everything between the base of the
    // ReturnBlock and the top of the StackPassedArgs.
private:
    uintptr_t m_pushedFP;                  // ChildSP+000     CallerSP-100 (0x08 bytes)    (fp)
    uintptr_t m_pushedLR;                  // ChildSP+008     CallerSP-0F8 (0x08 bytes)    (lr)
    Fp128   m_fpArgRegs[8];                // ChildSP+010     CallerSP-0F0 (0x80 bytes)    (q0-q7)
    uintptr_t m_returnBlock[4];            // ChildSP+090     CallerSP-070 (0x40 bytes)
    uintptr_t m_intArgRegs[9];             // ChildSP+0B0     CallerSP-050 (0x48 bytes)    (x0-x8)
    uintptr_t m_alignmentPad;              // ChildSP+0F8     CallerSP-008 (0x08 bytes)
    uintptr_t m_stackPassedArgs[1];        // ChildSP+100     CallerSP+000 (unknown size)

public:
    PTR_UIntNative get_CallerSP() { return GET_POINTER_TO_FIELD(m_stackPassedArgs[0]); }
    PTR_UIntNative get_AddressOfPushedCallerIP() { return GET_POINTER_TO_FIELD(m_pushedLR); }
    PTR_UIntNative get_LowerBoundForConservativeReporting() { return GET_POINTER_TO_FIELD(m_returnBlock[0]); }

    void UnwindNonVolatileRegisters(REGDISPLAY * pRegisterSet)
    {
        pRegisterSet->pFP = GET_POINTER_TO_FIELD(m_pushedFP);
    }
#elif defined(TARGET_WASM)
private:
    // WASMTODO: #error NYI for this arch
    uintptr_t m_stackPassedArgs[1];        // Placeholder
public:
    PTR_UIntNative get_CallerSP() { PORTABILITY_ASSERT("@TODO: FIXME:WASM"); return NULL; }
    PTR_UIntNative get_AddressOfPushedCallerIP() { PORTABILITY_ASSERT("@TODO: FIXME:WASM"); return NULL; }
    PTR_UIntNative get_LowerBoundForConservativeReporting() { PORTABILITY_ASSERT("@TODO: FIXME:WASM"); return NULL; }

    void UnwindNonVolatileRegisters(REGDISPLAY * pRegisterSet)
    {
        UNREFERENCED_PARAMETER(pRegisterSet);
        PORTABILITY_ASSERT("@TODO: FIXME:WASM");
    }
#else
#error NYI for this arch
#endif

#undef GET_POINTER_TO_FIELD

};

typedef DPTR(UniversalTransitionStackFrame) PTR_UniversalTransitionStackFrame;

// NOTE: This function always publishes a non-NULL conservative stack range lower bound.
//
// NOTE: In x86 cases, the unwound callsite often uses a calling convention that expects some amount
// of stack-passed argument space to be callee-popped before control returns (or unwinds) to the
// callsite.  Since the callsite signature (and thus the amount of callee-popped space) is unknown,
// the recovered SP does not account for the callee-popped space is therefore "wrong" for the
// purposes of unwind.  This implies that any x86 function which calls into RhpUniversalTransition
// must have a frame pointer to ensure that the incorrect SP value is ignored and does not break the
// unwind.
void StackFrameIterator::UnwindUniversalTransitionThunk()
{
    ASSERT((m_dwFlags & MethodStateCalculated) == 0);

#if defined(USE_PORTABLE_HELPERS) // @TODO: Corresponding helper code is only defined in assembly code
    return;
#else // defined(USE_PORTABLE_HELPERS)
    ASSERT(CategorizeUnadjustedReturnAddress(m_ControlPC) == InUniversalTransitionThunk);

    // The current PC is within RhpUniversalTransition, so establish a view of the surrounding stack frame.
    // NOTE: In DAC builds, the pointer will refer to a newly constructed object in the DAC host.
    UniversalTransitionStackFrame * stackFrame = (PTR_UniversalTransitionStackFrame)m_RegDisplay.SP;

    stackFrame->UnwindNonVolatileRegisters(&m_RegDisplay);

    PTR_UIntNative addressOfPushedCallerIP = stackFrame->get_AddressOfPushedCallerIP();
    m_RegDisplay.SetIP(PCODEToPINSTR(*addressOfPushedCallerIP));
    m_RegDisplay.SetSP((uintptr_t)dac_cast<TADDR>(stackFrame->get_CallerSP()));
    SetControlPC(dac_cast<PTR_VOID>(m_RegDisplay.GetIP()));

    // All universal transition cases rely on conservative GC reporting being applied to the
    // full argument set that flowed into the call.  Report the lower bound of this range (the
    // caller will compute the upper bound).
    PTR_UIntNative pLowerBound = stackFrame->get_LowerBoundForConservativeReporting();
    ASSERT(pLowerBound != NULL);
    ASSERT(m_pConservativeStackRangeLowerBound == NULL);
    m_pConservativeStackRangeLowerBound = pLowerBound;
#endif // defined(USE_PORTABLE_HELPERS)
}

#ifdef TARGET_AMD64
#define STACK_ALIGN_SIZE 16
#elif defined(TARGET_ARM)
#define STACK_ALIGN_SIZE 8
#elif defined(TARGET_ARM64)
#define STACK_ALIGN_SIZE 16
#elif defined(TARGET_X86)
#define STACK_ALIGN_SIZE 4
#elif defined(TARGET_WASM)
#define STACK_ALIGN_SIZE 4
#endif

void StackFrameIterator::UnwindThrowSiteThunk()
{
    ASSERT((m_dwFlags & MethodStateCalculated) == 0);

#if defined(USE_PORTABLE_HELPERS) // @TODO: no portable version of throw helpers
    return;
#else // defined(USE_PORTABLE_HELPERS)
    ASSERT(CategorizeUnadjustedReturnAddress(m_ControlPC) == InThrowSiteThunk);

    const uintptr_t STACKSIZEOF_ExInfo = ((sizeof(ExInfo) + (STACK_ALIGN_SIZE-1)) & ~(STACK_ALIGN_SIZE-1));
#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)
    const uintptr_t SIZEOF_OutgoingScratch = 0x20;
#else
    const uintptr_t SIZEOF_OutgoingScratch = 0;
#endif

    PTR_PAL_LIMITED_CONTEXT pContext = (PTR_PAL_LIMITED_CONTEXT)
                                        (m_RegDisplay.SP + SIZEOF_OutgoingScratch + STACKSIZEOF_ExInfo);

#if defined(UNIX_AMD64_ABI)
    m_RegDisplay.pRbp = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, Rbp);
    m_RegDisplay.pRbx = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, Rbx);
    m_RegDisplay.pR12 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R12);
    m_RegDisplay.pR13 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R13);
    m_RegDisplay.pR14 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R14);
    m_RegDisplay.pR15 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R15);
#elif defined(TARGET_AMD64)
    m_RegDisplay.pRbp = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, Rbp);
    m_RegDisplay.pRdi = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, Rdi);
    m_RegDisplay.pRsi = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, Rsi);
    m_RegDisplay.pRbx = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, Rbx);
    m_RegDisplay.pR12 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R12);
    m_RegDisplay.pR13 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R13);
    m_RegDisplay.pR14 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R14);
    m_RegDisplay.pR15 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R15);
#elif defined(TARGET_ARM)
    m_RegDisplay.pR4  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R4);
    m_RegDisplay.pR5  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R5);
    m_RegDisplay.pR6  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R6);
    m_RegDisplay.pR7  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R7);
    m_RegDisplay.pR8  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R8);
    m_RegDisplay.pR9  = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R9);
    m_RegDisplay.pR10 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R10);
    m_RegDisplay.pR11 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, R11);
#elif defined(TARGET_ARM64)
    m_RegDisplay.pX19 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, X19);
    m_RegDisplay.pX20 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, X20);
    m_RegDisplay.pX21 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, X21);
    m_RegDisplay.pX22 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, X22);
    m_RegDisplay.pX23 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, X23);
    m_RegDisplay.pX24 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, X24);
    m_RegDisplay.pX25 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, X25);
    m_RegDisplay.pX26 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, X26);
    m_RegDisplay.pX27 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, X27);
    m_RegDisplay.pX28 = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, X28);
    m_RegDisplay.pFP = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, FP);
#elif defined(TARGET_X86)
    m_RegDisplay.pRbp = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, Rbp);
    m_RegDisplay.pRdi = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, Rdi);
    m_RegDisplay.pRsi = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, Rsi);
    m_RegDisplay.pRbx = PTR_TO_MEMBER(PAL_LIMITED_CONTEXT, pContext, Rbx);
#else
    ASSERT_UNCONDITIONALLY("NYI for this arch");
#endif

    m_RegDisplay.SetIP(PCODEToPINSTR(pContext->IP));
    m_RegDisplay.SetSP(pContext->GetSp());
    SetControlPC(dac_cast<PTR_VOID>(m_RegDisplay.GetIP()));

    // We expect the throw site to be in managed code, and since this function's notion of how to unwind
    // through the stub is brittle relative to the stub itself, we want to check as soon as we can.
    ASSERT(m_pInstance->IsManaged(m_ControlPC) && "unwind from throw site stub failed");
#endif // defined(USE_PORTABLE_HELPERS)
}

bool StackFrameIterator::IsValid()
{
    return (m_ControlPC != 0);
}

void StackFrameIterator::Next()
{
    NextInternal();
    STRESS_LOG1(LF_STACKWALK, LL_INFO10000, "   %p\n", m_ControlPC);
}

void StackFrameIterator::NextInternal()
{
UnwindOutOfCurrentManagedFrame:
    ASSERT(m_dwFlags & MethodStateCalculated);
    // Due to the lack of an ICodeManager for native code, we can't unwind from a native frame.
    ASSERT((m_dwFlags & (SkipNativeFrames|UnwoundReversePInvoke)) != UnwoundReversePInvoke);
    m_dwFlags &= ~(ExCollide|MethodStateCalculated|UnwoundReversePInvoke|ActiveStackFrame);
    ASSERT(IsValid());

    m_pHijackedReturnValue = NULL;
    m_HijackedReturnValueKind = GCRK_Unknown;

#ifdef _DEBUG
    SetControlPC(dac_cast<PTR_VOID>((void*)666));
#endif // _DEBUG

    // Clear any preceding published conservative range.  The current unwind will compute a new range
    // from scratch if one is needed.
    m_pConservativeStackRangeLowerBound = NULL;
    m_pConservativeStackRangeUpperBound = NULL;

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
    uintptr_t DEBUG_preUnwindSP = m_RegDisplay.GetSP();
#endif

    uint32_t unwindFlags = USFF_None;
    if ((m_dwFlags & SkipNativeFrames) != 0)
    {
        unwindFlags |= USFF_StopUnwindOnTransitionFrame;
    }

    if ((m_dwFlags & GcStackWalkFlags) == GcStackWalkFlags)
    {
        unwindFlags |= USFF_GcUnwind;
    }

    FAILFAST_OR_DAC_FAIL(GetCodeManager()->UnwindStackFrame(&m_methodInfo, unwindFlags, &m_RegDisplay,
                                                            &m_pPreviousTransitionFrame));

    if (m_pPreviousTransitionFrame != NULL)
    {
        m_dwFlags |= UnwoundReversePInvoke;
    }

    bool doingFuncletUnwind = GetCodeManager()->IsFunclet(&m_methodInfo);

    if (m_pPreviousTransitionFrame != NULL && (m_dwFlags & SkipNativeFrames) != 0)
    {
        ASSERT(!doingFuncletUnwind);

        if (m_pPreviousTransitionFrame == TOP_OF_STACK_MARKER)
        {
            SetControlPC(0);
        }
        else
        {
            // NOTE: This can generate a conservative stack range if the recovered PInvoke callsite
            // resides in an assembly thunk and not in normal managed code.  In this case InternalInit
            // will unwind through the thunk and back to the nearest managed frame, and therefore may
            // see a conservative range reported by one of the thunks encountered during this "nested"
            // unwind.
            InternalInit(m_pThread, m_pPreviousTransitionFrame, GcStackWalkFlags);
            m_dwFlags |= UnwoundReversePInvoke;
            ASSERT(m_pInstance->IsManaged(m_ControlPC));
        }
    }
    else
    {
        // if the thread is safe to walk, it better not have a hijack in place.
        ASSERT(!m_pThread->IsHijacked());

        SetControlPC(dac_cast<PTR_VOID>(PCODEToPINSTR(m_RegDisplay.GetIP())));

        PTR_VOID collapsingTargetFrame = NULL;

        // Starting from the unwound return address, unwind further (if needed) until reaching
        // either the next managed frame (i.e., the next frame that should be yielded from the
        // stack frame iterator) or a collision point that requires complex handling.

        bool exCollide = false;
        ReturnAddressCategory category = CategorizeUnadjustedReturnAddress(m_ControlPC);

        if (doingFuncletUnwind)
        {
            ASSERT(m_pendingFuncletFramePointer == NULL);
            ASSERT(m_FramePointer != NULL);

            if (category == InFuncletInvokeThunk)
            {
                // The iterator is unwinding out of an exceptionally invoked funclet.  Before proceeding,
                // record the funclet frame pointer so that the iterator can verify that the remainder of
                // the stack walk encounters "owner frames" (i.e., parent funclets or the main code body)
                // in the expected order.
                // NOTE: m_pendingFuncletFramePointer will be cleared by HandleExCollide the stack walk
                // collides with the ExInfo that invoked this funclet.
                m_pendingFuncletFramePointer = m_FramePointer;

                // Unwind through the funclet invoke assembly thunk to reach the topmost managed frame in
                // the exception dispatch code.  All non-GC stack walks collide at this point (whereas GC
                // stack walks collide at the throw site which is reached after processing all of the
                // exception dispatch frames).
                UnwindFuncletInvokeThunk();
                if (!(m_dwFlags & CollapseFunclets))
                {
                    exCollide = true;
                }
            }
            else if (category == InFilterFuncletInvokeThunk)
            {
                // Unwind through the funclet invoke assembly thunk to reach the topmost managed frame in
                // the exception dispatch code.
                UnwindFuncletInvokeThunk();
            }
            else if (category == InManagedCode)
            {
                // Non-exceptionally invoked funclet case.  The caller is processed as a normal managed
                // frame, with the caveat that funclet collapsing must be applied in GC stack walks (since
                // the caller is either a parent funclet or the main code body and the leafmost funclet
                // already provided GC information for the entire function).
                if (m_dwFlags & CollapseFunclets)
                {
                    collapsingTargetFrame = m_FramePointer;
                }
            }
            else
            {
                FAILFAST_OR_DAC_FAIL_UNCONDITIONALLY("Unexpected thunk encountered when unwinding out of a funclet.");
            }
        }
        else if (category != InManagedCode)
        {
            // Unwinding the current (non-funclet) managed frame revealed that its caller is one of the
            // well-known assembly thunks.  Unwind through the thunk to find the next managed frame
            // that should be yielded from the stack frame iterator.
            // NOTE: It is generally possible for a sequence of multiple thunks to appear "on top of
            // each other" on the stack (e.g., the CallDescrThunk can be used to invoke the
            // UniversalTransitionThunk), but EH thunks can never appear in such sequences.

            if (IsNonEHThunk(category))
            {
                // Unwind the current sequence of one or more thunks until the next managed frame is reached.
                // NOTE: This can generate a conservative stack range if one or more of the thunks in the
                // sequence report a conservative lower bound.
                UnwindNonEHThunkSequence();
            }
            else if (category == InThrowSiteThunk)
            {
                // EH stack walks collide at the funclet invoke thunk and are never expected to encounter
                // throw sites (except in illegal cases such as exceptions escaping from the managed
                // exception dispatch code itself).
                FAILFAST_OR_DAC_FAIL_MSG(!(m_dwFlags & ApplyReturnAddressAdjustment),
                    "EH stack walk is attempting to propagate an exception across a throw site.");

                UnwindThrowSiteThunk();

                if (m_dwFlags & CollapseFunclets)
                {
                    uintptr_t postUnwindSP = m_RegDisplay.SP;

                    if (m_pNextExInfo && (postUnwindSP > ((uintptr_t)dac_cast<TADDR>(m_pNextExInfo))))
                    {
                        // This GC stack walk has processed all managed exception frames associated with the
                        // current throw site, meaning it has now collided with the associated ExInfo.
                        exCollide = true;
                    }
                }
            }
            else
            {
                FAILFAST_OR_DAC_FAIL_UNCONDITIONALLY("Unexpected thunk encountered when unwinding out of a non-funclet.");
            }
        }

        if (exCollide)
        {
            // OK, so we just hit (collided with) an exception throw point.  We continue by consulting the
            // ExInfo.

            // In the GC stackwalk, this means walking all the way off the end of the managed exception
            // dispatch code to the throw site.  In the EH stackwalk, this means hitting the special funclet
            // invoke ASM thunks.

            // Double-check that the ExInfo that is being consulted is at or below the 'current' stack pointer
            ASSERT(DEBUG_preUnwindSP <= (uintptr_t)m_pNextExInfo);

            ASSERT(collapsingTargetFrame == NULL);

            collapsingTargetFrame = HandleExCollide(m_pNextExInfo);
        }

        // Now that all assembly thunks and ExInfo collisions have been processed, it is guaranteed
        // that the next managed frame has been located. Or the next native frame
        // if we are not skipping them. The located frame must now be yielded
        // from the iterator with the one and only exception being cases where a managed frame must
        // be skipped due to funclet collapsing.

        ASSERT(m_pInstance->IsManaged(m_ControlPC) || (m_pPreviousTransitionFrame != NULL && (m_dwFlags & SkipNativeFrames) == 0));

        if (collapsingTargetFrame != NULL)
        {
            // The iterator is positioned on a parent funclet or main code body in a function where GC
            // information has already been reported by the leafmost funclet, implying that the current
            // frame needs to be skipped by the GC stack walk.  In general, the GC stack walk must skip
            // all parent frames that are "part of the same function" (i.e., have the same frame
            // pointer).
            ASSERT(m_dwFlags & CollapseFunclets);
            CalculateCurrentMethodState();
            ASSERT(IsValid());
            FAILFAST_OR_DAC_FAIL(m_FramePointer == collapsingTargetFrame);

            // Fail if the skipped frame has no associated conservative stack range (since any
            // attached stack range is about to be dropped without ever being reported to the GC).
            // This should never happen since funclet collapsing cases and only triggered when
            // unwinding out of managed frames and never when unwinding out of the thunks that report
            // conservative ranges.
            FAILFAST_OR_DAC_FAIL(m_pConservativeStackRangeLowerBound == NULL);

            STRESS_LOG0(LF_STACKWALK, LL_INFO10000, "[ KeepUnwinding ]\n");
            goto UnwindOutOfCurrentManagedFrame;
        }

        // Before yielding this frame, indicate that it was located via an ExInfo collision as
        // opposed to normal unwind.
        if (exCollide)
            m_dwFlags |= ExCollide;
    }

    // At this point, the iterator is in an invalid state if there are no more managed frames
    // on the current stack, and is otherwise positioned on the next managed frame to yield to
    // the caller.
    PrepareToYieldFrame();
}

// NOTE: This function will publish a non-NULL conservative stack range lower bound if and
// only if one or more of the thunks in the sequence report conservative stack ranges.
void StackFrameIterator::UnwindNonEHThunkSequence()
{
    ReturnAddressCategory category = CategorizeUnadjustedReturnAddress(m_ControlPC);
    ASSERT(IsNonEHThunk(category));

    // Unwind the current sequence of thunks until the next managed frame is reached, being
    // careful to detect and aggregate any conservative stack ranges reported by the thunks.
    PTR_UIntNative pLowestLowerBound = NULL;
    PTR_UIntNative pPrecedingLowerBound = NULL;
    while (category != InManagedCode)
    {
        ASSERT(m_pConservativeStackRangeLowerBound == NULL);

        if (category == InUniversalTransitionThunk)
        {
            UnwindUniversalTransitionThunk();
            ASSERT(m_pConservativeStackRangeLowerBound != NULL);
        }
        else
        {
            FAILFAST_OR_DAC_FAIL_UNCONDITIONALLY("Unexpected thunk encountered when unwinding a non-EH thunk sequence.");
        }

        if (m_pConservativeStackRangeLowerBound != NULL)
        {
            // The newly unwound thunk reported a conservative stack range lower bound.  The thunk
            // sequence being unwound needs to generate a single conservative range that will be
            // reported along with the managed frame eventually yielded by the iterator.  To ensure
            // sufficient reporting, this range always extends from the first (i.e., lowest) lower
            // bound all the way to the top of the outgoing arguments area in the next managed frame.
            // This aggregate range therefore covers all intervening thunk frames (if any), and also
            // covers all necessary conservative ranges in the pathological case where a sequence of
            // thunks contains multiple frames which report distinct conservative lower bound values.
            //
            // Capture the initial lower bound, and assert that the lower bound values are compatible
            // with the "aggregate range" approach described above (i.e., that they never exceed the
            // unwound thunk's stack frame and are always larger than all previously encountered lower
            // bound values).

            if (pLowestLowerBound == NULL)
                pLowestLowerBound = m_pConservativeStackRangeLowerBound;

            FAILFAST_OR_DAC_FAIL(m_pConservativeStackRangeLowerBound < (PTR_UIntNative)m_RegDisplay.SP);
            FAILFAST_OR_DAC_FAIL(m_pConservativeStackRangeLowerBound > pPrecedingLowerBound);
            pPrecedingLowerBound = m_pConservativeStackRangeLowerBound;
            m_pConservativeStackRangeLowerBound = NULL;
        }

        category = CategorizeUnadjustedReturnAddress(m_ControlPC);
    }

    // The iterator has reached the next managed frame.  Publish the computed lower bound value.
    ASSERT(m_pConservativeStackRangeLowerBound == NULL);
    m_pConservativeStackRangeLowerBound = pLowestLowerBound;
}

// This function is called immediately before a given frame is yielded from the iterator
// (i.e., before a given frame is exposed outside of the iterator).  At yield points,
// iterator must either be invalid (indicating that all managed frames have been processed)
// or must describe a valid managed frame.  In the latter case, some common postprocessing
// steps must always be applied before the frame is exposed outside of the iterator.
void StackFrameIterator::PrepareToYieldFrame()
{
    if (!IsValid())
        return;

    ASSERT(m_pInstance->IsManaged(m_ControlPC) ||
         ((m_dwFlags & SkipNativeFrames) == 0 && (m_dwFlags & UnwoundReversePInvoke) != 0));

    if (m_dwFlags & ApplyReturnAddressAdjustment)
    {
        m_ControlPC = AdjustReturnAddressBackward(m_ControlPC);
    }

    m_ShouldSkipRegularGcReporting = false;

    // Each time a managed frame is yielded, configure the iterator to explicitly indicate
    // whether or not unwinding to the current frame has revealed a stack range that must be
    // conservatively reported by the GC.
    if ((m_pConservativeStackRangeLowerBound != NULL) && (m_dwFlags & CollapseFunclets))
    {
        // Conservatively reported stack ranges always correspond to the full extent of the
        // argument set (including stack-passed arguments and spilled argument registers) that
        // flowed into a managed callsite which called into the runtime.  The runtime has no
        // knowledge of the callsite signature in these cases, and unwind through these callsites
        // is only possible via the associated assembly thunk (e.g., the ManagedCalloutThunk or
        // UniversalTransitionThunk).
        //
        // The iterator is currently positioned on the managed frame which contains the callsite of
        // interest.  The lower bound of the argument set was already computed while unwinding
        // through the assembly thunk.  The upper bound of the argument set is always at or below
        // the top of the outgoing arguments area in the current managed frame (i.e., in the
        // managed frame which contains the callsite).
        //
        // Compute a conservative upper bound and then publish the total range so that it can be
        // observed by the current GC stack walk (via HasStackRangeToReportConservatively).  Note
        // that the upper bound computation never mutates m_RegDisplay.
        CalculateCurrentMethodState();
        ASSERT(IsValid());

        uintptr_t rawUpperBound = GetCodeManager()->GetConservativeUpperBoundForOutgoingArgs(&m_methodInfo, &m_RegDisplay);
        m_pConservativeStackRangeUpperBound = (PTR_UIntNative)rawUpperBound;

        ASSERT(m_pConservativeStackRangeLowerBound != NULL);
        ASSERT(m_pConservativeStackRangeUpperBound != NULL);
        ASSERT(m_pConservativeStackRangeUpperBound > m_pConservativeStackRangeLowerBound);
    }
    else
    {
        m_pConservativeStackRangeLowerBound = NULL;
        m_pConservativeStackRangeUpperBound = NULL;
    }
}

REGDISPLAY * StackFrameIterator::GetRegisterSet()
{
    ASSERT(IsValid());
    return &m_RegDisplay;
}

PTR_VOID StackFrameIterator::GetEffectiveSafePointAddress()
{
    ASSERT(IsValid());
    ASSERT(m_effectiveSafePointAddress);
    return m_effectiveSafePointAddress;
}

PTR_ICodeManager StackFrameIterator::GetCodeManager()
{
    ASSERT(IsValid());
    return m_pCodeManager;
}

MethodInfo * StackFrameIterator::GetMethodInfo()
{
    ASSERT(IsValid());
    return &m_methodInfo;
}

bool StackFrameIterator::IsActiveStackFrame()
{
    ASSERT(IsValid());
    return (m_dwFlags & ActiveStackFrame) != 0;
}

#ifdef DACCESS_COMPILE
#define FAILFAST_OR_DAC_RETURN_FALSE(x) if(!(x)) return false;
#else
#define FAILFAST_OR_DAC_RETURN_FALSE(x) if(!(x)) { ASSERT_UNCONDITIONALLY(#x); RhFailFast(); }
#endif

void StackFrameIterator::CalculateCurrentMethodState()
{
    if (m_dwFlags & MethodStateCalculated)
        return;

    // Check if we are on a native frame.
    if ((m_dwFlags & (SkipNativeFrames|UnwoundReversePInvoke)) == UnwoundReversePInvoke)
    {
        // There is no implementation of ICodeManager for native code.
        m_pCodeManager = nullptr;
        m_effectiveSafePointAddress = nullptr;
        m_FramePointer = nullptr;
        m_dwFlags |= MethodStateCalculated;
        return;
    }

    // Assume that the caller is likely to be in the same module
    if (m_pCodeManager == NULL || !m_pCodeManager->FindMethodInfo(m_ControlPC, &m_methodInfo))
    {
        m_pCodeManager = dac_cast<PTR_ICodeManager>(m_pInstance->GetCodeManagerForAddress(m_ControlPC));
        FAILFAST_OR_DAC_FAIL(m_pCodeManager);

        FAILFAST_OR_DAC_FAIL(m_pCodeManager->FindMethodInfo(m_ControlPC, &m_methodInfo));
    }

    m_effectiveSafePointAddress = m_ControlPC;
    m_FramePointer = GetCodeManager()->GetFramePointer(&m_methodInfo, &m_RegDisplay);

    m_dwFlags |= MethodStateCalculated;
}

bool StackFrameIterator::GetHijackedReturnValueLocation(PTR_OBJECTREF * pLocation, GCRefKind * pKind)
{
    if (GCRK_Unknown == m_HijackedReturnValueKind)
        return false;

    ASSERT((GCRK_Scalar < m_HijackedReturnValueKind) && (m_HijackedReturnValueKind <= GCRK_LastValid));

    *pLocation = m_pHijackedReturnValue;
    *pKind = m_HijackedReturnValueKind;
    return true;
}

void StackFrameIterator::SetControlPC(PTR_VOID controlPC)
{
#if TARGET_ARM
    // Ensure that PC doesn't have the Thumb bit set. This needs to be
    // consistent for EQUALS_RETURN_ADDRESS to work.
    ASSERT(((uintptr_t)controlPC & 1) == 0);
#endif
    m_OriginalControlPC = m_ControlPC = controlPC;
}

bool StackFrameIterator::IsNonEHThunk(ReturnAddressCategory category)
{
    switch (category)
    {
        default:
            return false;
        case InUniversalTransitionThunk:
            return true;
    }
}

bool StackFrameIterator::IsValidReturnAddress(PTR_VOID pvAddress)
{
    // These are return addresses into functions that call into managed (non-funclet) code, so we might see
    // them as hijacked return addresses.
    ReturnAddressCategory category = CategorizeUnadjustedReturnAddress(pvAddress);

    // All non-EH thunks call out to normal managed code, implying that return addresses into
    // them can be hijacked.
    if (IsNonEHThunk(category))
        return true;

    // Throw site thunks call out to managed code, but control never returns from the managed
    // callee.  As a result, return addresses into these thunks can be hijacked, but the
    // hijacks will never execute.
    if (category == InThrowSiteThunk)
        return true;

    return GetRuntimeInstance()->IsManaged(pvAddress);
}

// Support for conservatively reporting GC references in a stack range. This is used when managed methods with
// an unknown signature potentially including GC references call into the runtime and we need to let a GC
// proceed (typically because we call out into managed code again). Instead of storing signature metadata for
// every possible managed method that might make such a call we identify a small range of the stack that might
// contain outgoing arguments. We then report every pointer that looks like it might refer to the GC heap as a
// fixed interior reference.

bool StackFrameIterator::HasStackRangeToReportConservatively()
{
    // When there's no range to report both the lower and upper bounds will be NULL.
    return IsValid() && (m_pConservativeStackRangeUpperBound != NULL);
}

void StackFrameIterator::GetStackRangeToReportConservatively(PTR_OBJECTREF * ppLowerBound, PTR_OBJECTREF * ppUpperBound)
{
    ASSERT(HasStackRangeToReportConservatively());
    *ppLowerBound = (PTR_OBJECTREF)m_pConservativeStackRangeLowerBound;
    *ppUpperBound = (PTR_OBJECTREF)m_pConservativeStackRangeUpperBound;
}

PTR_VOID StackFrameIterator::AdjustReturnAddressBackward(PTR_VOID controlPC)
{
#ifdef TARGET_ARM
    return (PTR_VOID)(((PTR_UInt8)controlPC) - 2);
#elif defined(TARGET_ARM64)
    return (PTR_VOID)(((PTR_UInt8)controlPC) - 4);
#else
    return (PTR_VOID)(((PTR_UInt8)controlPC) - 1);
#endif
}

// Given a return address, determine the category of function where it resides.  In
// general, return addresses encountered by the stack walker are required to reside in
// managed code unless they reside in one of the well-known assembly thunks.

// static
StackFrameIterator::ReturnAddressCategory StackFrameIterator::CategorizeUnadjustedReturnAddress(PTR_VOID returnAddress)
{
#if defined(USE_PORTABLE_HELPERS) // @TODO: no portable thunks are defined

    return InManagedCode;

#else // defined(USE_PORTABLE_HELPERS)

#if defined(FEATURE_DYNAMIC_CODE)
    if (EQUALS_RETURN_ADDRESS(returnAddress, ReturnFromUniversalTransition) ||
             EQUALS_RETURN_ADDRESS(returnAddress, ReturnFromUniversalTransition_DebugStepTailCall))
    {
        return InUniversalTransitionThunk;
    }
#endif

    if (EQUALS_RETURN_ADDRESS(returnAddress, RhpThrowEx2) ||
        EQUALS_RETURN_ADDRESS(returnAddress, RhpThrowHwEx2) ||
        EQUALS_RETURN_ADDRESS(returnAddress, RhpRethrow2))
    {
        return InThrowSiteThunk;
    }

#ifdef TARGET_X86
    if (EQUALS_RETURN_ADDRESS(returnAddress, RhpCallFunclet2))
    {
        PORTABILITY_ASSERT("CategorizeUnadjustedReturnAddress");
#if 0
        // See if it is a filter funclet based on the caller of RhpCallFunclet
        PTR_UIntNative SP = (PTR_UIntNative)(m_RegDisplay.SP + 0x4);   // skip the saved assembly-routine-EBP
        PTR_UIntNative ControlPC = *SP++;
        if (EQUALS_RETURN_ADDRESS(ControlPC, RhpCallFilterFunclet2))
        {
            return InFilterFuncletInvokeThunk;
        }
        else
#endif
        {
            return InFuncletInvokeThunk;
        }
    }
#else // TARGET_X86
    if (EQUALS_RETURN_ADDRESS(returnAddress, RhpCallCatchFunclet2) ||
        EQUALS_RETURN_ADDRESS(returnAddress, RhpCallFinallyFunclet2))
    {
        return InFuncletInvokeThunk;
    }

    if (EQUALS_RETURN_ADDRESS(returnAddress, RhpCallFilterFunclet2))
    {
        return InFilterFuncletInvokeThunk;
    }
#endif // TARGET_X86
    return InManagedCode;
#endif // defined(USE_PORTABLE_HELPERS)
}

bool StackFrameIterator::ShouldSkipRegularGcReporting()
{
    return m_ShouldSkipRegularGcReporting;
}

#ifndef DACCESS_COMPILE

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhpSfiInit, (StackFrameIterator* pThis, PAL_LIMITED_CONTEXT* pStackwalkCtx, CLR_BOOL instructionFault, CLR_BOOL* pfIsExceptionIntercepted))
{
    Thread * pCurThread = ThreadStore::GetCurrentThread();

    // The stackwalker is intolerant to hijacked threads, as it is largely expecting to be called from C++
    // where the hijack state of the thread is invariant.  Because we've exposed the iterator out to C#, we
    // need to unhijack every time we callback into C++ because the thread could have been hijacked during our
    // time executing C#.
    pCurThread->Unhijack();

    // Passing NULL is a special-case to request a standard managed stack trace for the current thread.
    if (pStackwalkCtx == NULL)
        pThis->InternalInitForStackTrace();
    else
        pThis->InternalInitForEH(pCurThread, pStackwalkCtx, instructionFault);

    bool isValid = pThis->IsValid();
    if (isValid)
        pThis->CalculateCurrentMethodState();

    if (pfIsExceptionIntercepted)
    {
        *pfIsExceptionIntercepted = false;
    }

    FC_RETURN_BOOL(isValid);
}

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhpSfiNext, (StackFrameIterator* pThis, uint32_t* puExCollideClauseIdx, CLR_BOOL* pfUnwoundReversePInvoke, CLR_BOOL* pfIsExceptionIntercepted))
{
    // The stackwalker is intolerant to hijacked threads, as it is largely expecting to be called from C++
    // where the hijack state of the thread is invariant.  Because we've exposed the iterator out to C#, we
    // need to unhijack every time we callback into C++ because the thread could have been hijacked during our
    // time executing C#.
    ThreadStore::GetCurrentThread()->Unhijack();

    const uint32_t MaxTryRegionIdx = 0xFFFFFFFF;

    ExInfo * pCurExInfo = pThis->m_pNextExInfo;
    pThis->Next();
    bool isValid = pThis->IsValid();
    if (isValid)
        pThis->CalculateCurrentMethodState();

    if (puExCollideClauseIdx != NULL)
    {
        if (pThis->m_dwFlags & StackFrameIterator::ExCollide)
        {
            ASSERT(pCurExInfo->m_idxCurClause != MaxTryRegionIdx);
            *puExCollideClauseIdx = pCurExInfo->m_idxCurClause;
            pCurExInfo->m_kind = (ExKind)(pCurExInfo->m_kind | EK_SupersededFlag);
        }
        else
        {
            *puExCollideClauseIdx = MaxTryRegionIdx;
        }
    }

    if (pfUnwoundReversePInvoke != NULL)
    {
        *pfUnwoundReversePInvoke = (pThis->m_dwFlags & StackFrameIterator::UnwoundReversePInvoke) != 0;
    }

    if (pfIsExceptionIntercepted)
    {
        *pfIsExceptionIntercepted = false;
    }

    FC_RETURN_BOOL(isValid);
}

#endif // !DACCESS_COMPILE
