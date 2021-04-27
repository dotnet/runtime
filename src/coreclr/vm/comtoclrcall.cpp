// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ==++==
//

//
// ==--==
//
// File: COMtoCLRCall.cpp
//

//
// COM to CLR call support.
//


#include "common.h"

#include "vars.hpp"
#include "clrtypes.h"
#include "stublink.h"
#include "excep.h"
#include "comtoclrcall.h"
#include "cgensys.h"
#include "method.hpp"
#include "siginfo.hpp"
#include "comcallablewrapper.h"
#include "field.h"
#include "virtualcallstub.h"
#include "dllimport.h"
#include "mlinfo.h"
#include "dbginterface.h"
#include "sigbuilder.h"
#include "notifyexternals.h"
#include "comdelegate.h"
#include "finalizerthread.h"

#ifdef _DEBUG
#define FORCEINLINE_NONDEBUG
#else
#define FORCEINLINE_NONDEBUG FORCEINLINE
#endif

#if !defined(DACCESS_COMPILE)

#ifdef TARGET_X86
static PCODE g_pGenericComCallStubFields = NULL;
static PCODE g_pGenericComCallStub       = NULL;
#endif

UINT64 FieldCallWorker(Thread *pThread, ComMethodFrame* pFrame);
void FieldCallWorkerDebuggerWrapper(Thread *pThread, ComMethodFrame* pFrame);
void FieldCallWorkerBody(Thread *pThread, ComMethodFrame* pFrame);

#ifndef CROSSGEN_COMPILE
//---------------------------------------------------------
// void SetupGenericStubs()
//
//  Throws on failure
//---------------------------------------------------------
static void SetupGenericStubs()
{
    STANDARD_VM_CONTRACT;

#ifdef TARGET_X86
    if ( (g_pGenericComCallStubFields != NULL) && (g_pGenericComCallStub != NULL))
        return;

    StubHolder<Stub> candidateCall, candidateFields;

    // Build each one.  If we get a collision on replacement, favor the one that's
    // already there.  (We have lifetime issues with these, because they are used
    // in every VTable without refcounting, so we don't want them to change
    // underneath us).

    // Allocate all three before setting - if an error occurs, we'll free the
    //  memory via holder objects and throw.
    candidateCall = ComCall::CreateGenericComCallStub(FALSE/*notField*/);
    candidateFields = ComCall::CreateGenericComCallStub(TRUE/*Field*/);

    if (InterlockedCompareExchangeT<PCODE>(&g_pGenericComCallStub, candidateCall->GetEntryPoint(), 0) == 0)
        candidateCall.SuppressRelease();

    if (InterlockedCompareExchangeT<PCODE>(&g_pGenericComCallStubFields, candidateFields->GetEntryPoint(), 0) == 0)
        candidateFields.SuppressRelease();
#endif // TARGET_X86
}

#ifdef PROFILING_SUPPORTED
// The sole purpose of this helper is to transition into preemptive mode
// and then call the profiler transition callbacks.  We can't use the GCX_PREEMP
// in a function with SEH (such as COMToCLRWorkerBody()).
NOINLINE
void ProfilerTransitionCallbackHelper(MethodDesc* pMD, Thread* pThread, COR_PRF_TRANSITION_REASON reason)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(CheckPointer(pThread));
        PRECONDITION(CORProfilerTrackTransitions());
    }
    CONTRACTL_END;

    GCX_PREEMP_THREAD_EXISTS(pThread);

    if (reason == COR_PRF_TRANSITION_CALL)
    {
        ProfilerUnmanagedToManagedTransitionMD(pMD, COR_PRF_TRANSITION_CALL);
    }
    else
    {
        ProfilerManagedToUnmanagedTransitionMD(pMD, COR_PRF_TRANSITION_RETURN);
    }
}
#endif // PROFILING_SUPPORTED

// Disable when calling into managed code from a place that fails via HRESULT
extern "C" HRESULT STDCALL StubRareDisableHRWorker(Thread *pThread)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;

    HRESULT hr = S_OK;

    // Do not add a CONTRACT here.  We haven't set up SEH.  We rely
    // on HandleThreadAbort dealing with this situation properly.

    // WARNING!!!!
    // when we start executing here, we are actually in cooperative mode.  But we
    // haven't synchronized with the barrier to reentry yet.  So we are in a highly
    // dangerous mode.  If we call managed code, we will potentially be active in
    // the GC heap, even as GC's are occuring!

    // We must do the following in this order, because otherwise we would be constructing
    // the exception for the abort without synchronizing with the GC.  Also, we have no
    // CLR SEH set up, despite the fact that we may throw a ThreadAbortException.
    pThread->RareDisablePreemptiveGC();
    EX_TRY
    {
        pThread->HandleThreadAbort();
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    // should always be in coop mode here
    _ASSERTE(pThread->PreemptiveGCDisabled());

    // Note that this code does not handle rare signatures that do not return HRESULT properly

    return hr;
}

#ifdef TARGET_X86

// defined in i386\asmhelpers.asm
extern "C" ARG_SLOT __fastcall COMToCLRDispatchHelper(
    INT_PTR dwArgECX,
    INT_PTR dwArgEDX,
    PCODE   pTarget,
    PCODE   pSecretArg,
    INT_PTR *pInputStack,
    WORD    wOutputStackSlots,
    UINT16  *pOutputStackOffsets,
    Frame   *pCurFrame);


inline static void InvokeStub(ComCallMethodDesc *pCMD, PCODE pManagedTarget, OBJECTREF orThis, ComMethodFrame *pFrame, Thread *pThread,
                              UINT64* pRetValOut)
{
    LIMITED_METHOD_CONTRACT;

    INT_PTR *pInputStack = (INT_PTR *)pFrame->GetPointerToArguments();
    PCODE pStubEntryPoint = pCMD->GetILStub();

    INT_PTR EDX = (pCMD->m_wSourceSlotEDX == (UINT16)-1 ? NULL : pInputStack[pCMD->m_wSourceSlotEDX]);

    ARG_SLOT retVal = 0;

    // Managed code is generally "THROWS" and we have no exception handler here that the contract system can
    // see.  We ensure that we don't get exceptions here by generating a try/catch in the IL stub that covers
    // any possible throw points, including all calls within the stub to helpers.
    PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonILStubWillNotThrow);

    //
    // NOTE! We do not use BEGIN_CALL_TO_MANAGEDEX around this call because COMToCLRDispatchHelper is
    // responsible for pushing/popping the CPFH into the FS:0 chain.
    //

    *pRetValOut = COMToCLRDispatchHelper(
        *((INT_PTR *) &orThis),           // pArgECX
        EDX,                              // pArgEDX
        pStubEntryPoint,                  // pTarget
        pManagedTarget,                   // pSecretArg
        pInputStack,                      // pInputStack
        pCMD->m_wStubStackSlotCount,      // wOutputStackSlots
        pCMD->m_pwStubStackSlotOffsets,   // pOutputStackOffsets
        pThread->GetFrame());             // pCurFrame
}

#else // TARGET_X86

// defined in amd64\GenericComCallStubs.asm
extern "C" ARG_SLOT COMToCLRDispatchHelper(
    DWORD          dwStackSlots,
    ComMethodFrame *pFrame,
    PCODE          pTarget,
    PCODE          pR10,
    INT_PTR        pDangerousThis);


inline static void InvokeStub(ComCallMethodDesc *pCMD, PCODE pManagedTarget, OBJECTREF orThis, ComMethodFrame *pFrame, Thread *pThread,
                              UINT64* pRetValOut)
{
    WRAPPER_NO_CONTRACT;

    ARG_SLOT retVal = 0;
    PCODE pStubEntryPoint = pCMD->GetILStub();

    INT_PTR dangerousThis;
    *(OBJECTREF *)&dangerousThis = orThis;

    DWORD dwStackSlots = pCMD->GetNumStackBytes() / TARGET_POINTER_SIZE;

    // Managed code is generally "THROWS" and we have no exception handler here that the contract system can
    // see.  We ensure that we don't get exceptions here by generating a try/catch in the IL stub that covers
    // any possible throw points, including all calls within the stub to helpers.
    PERMANENT_CONTRACT_VIOLATION(ThrowsViolation, ReasonILStubWillNotThrow);

    //
    // NOTE! We do not use BEGIN_CALL_TO_MANAGEDEX around this call because we stayed in the SO_TOLERANT
    // mode and we have no need to push/pop FS:0 on non-x86 Windows platforms.
    //

    *pRetValOut = COMToCLRDispatchHelper(
        dwStackSlots,     // dwStackSlots
        pFrame,           // pFrame
        pStubEntryPoint,  // pTarget
        pManagedTarget,   // pSecretArg
        dangerousThis);   // pDangerousThis
}

#endif // TARGET_X86

#if defined(_MSC_VER) && !defined(_DEBUG)
#pragma optimize("t", on)   // optimize for speed
#endif

OBJECTREF COMToCLRGetObjectAndTarget_Delegate(ComCallWrapper * pWrap, PCODE * ppManagedTargetOut)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    DELEGATEREF pDelObj = (DELEGATEREF)pWrap->GetObjectRef();
    _ASSERTE(pDelObj->GetMethodTable()->IsDelegate());

    // We don't have to go through the Invoke slot because we know what the delegate
    // target is. This is the same optimization that reverse P/Invoke stubs do.
    *ppManagedTargetOut = (PCODE)pDelObj->GetMethodPtr();
    return pDelObj->GetTarget();
}

FORCEINLINE_NONDEBUG
OBJECTREF COMToCLRGetObjectAndTarget_Virtual(ComCallWrapper * pWrap, MethodDesc * pRealMD, ComCallMethodDesc * pCMD, PCODE * ppManagedTargetOut)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF pObject = pWrap->GetObjectRef();

    MethodTable *pMT = pObject->GetMethodTable();

    if (pRealMD->IsInterface())
    {
        // For transparent proxies, we need to call on the interface method desc if
        // this method represents an interface method and not an IClassX method.
        *ppManagedTargetOut = pCMD->GetCallMethodDesc()->GetSingleCallableAddrOfCode();
    }
    else
    {
        // we know the slot number for this method desc, grab the actual
        // address from the vtable for this slot. The slot number should
        // remain the same through out the heirarchy.
        *ppManagedTargetOut = pMT->GetSlotForVirtual(pCMD->GetSlot());
    }
    return pObject;
}

FORCEINLINE_NONDEBUG
OBJECTREF COMToCLRGetObjectAndTarget_NonVirtual(ComCallWrapper * pWrap, MethodDesc * pRealMD, ComCallMethodDesc * pCMD, PCODE * ppManagedTargetOut)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    //NOTE: No need to optimize for stub dispatch since non-virtuals are retrieved quickly.
    *ppManagedTargetOut = pRealMD->GetSingleCallableAddrOfCode();

    return pWrap->GetObjectRef();
}

FORCEINLINE_NONDEBUG
void COMToCLRInvokeTarget(PCODE pManagedTarget, OBJECTREF pObject, ComCallMethodDesc * pCMD,
                          ComMethodFrame * pFrame, Thread * pThread, UINT64* pRetValOut)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

#ifdef DEBUGGING_SUPPORTED
    if (CORDebuggerTraceCall())
    {
        g_pDebugInterface->TraceCall((const BYTE *)pManagedTarget);
    }
#endif // DEBUGGING_SUPPORTED

    InvokeStub(pCMD, pManagedTarget, pObject, pFrame, pThread, pRetValOut);
}

NOINLINE
void COMToCLRWorkerBody_Rare(Thread * pThread, ComMethodFrame * pFrame, ComCallWrapper * pWrap,
                             MethodDesc * pRealMD, ComCallMethodDesc * pCMD, DWORD maskedFlags,
                             UINT64 * pRetValOut)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    PCODE pManagedTarget;
    OBJECTREF pObject;

    int fpReturnSize = 0;
    if (maskedFlags & enum_NativeR8Retval)
        fpReturnSize = 8;
    if (maskedFlags & enum_NativeR4Retval)
        fpReturnSize = 4;

    maskedFlags &= ~(enum_NativeR4Retval|enum_NativeR8Retval);

    switch (maskedFlags)
    {
    case enum_IsDelegateInvoke|enum_IsVirtual:
    case enum_IsDelegateInvoke: pObject = COMToCLRGetObjectAndTarget_Delegate(pWrap, &pManagedTarget); break;
    case enum_IsVirtual:        pObject = COMToCLRGetObjectAndTarget_Virtual(pWrap, pRealMD, pCMD, &pManagedTarget); break;
    case 0:                     pObject = COMToCLRGetObjectAndTarget_NonVirtual(pWrap, pRealMD, pCMD, &pManagedTarget); break;
    default:                    UNREACHABLE();
    }

    COMToCLRInvokeTarget(pManagedTarget, pObject, pCMD, pFrame, pThread, pRetValOut);

    if (fpReturnSize != 0)
        getFPReturn(fpReturnSize, (INT64*)pRetValOut);

#if defined(PROFILING_SUPPORTED)
    // Notify the profiler of the return out of the runtime.
    if (CORProfilerTrackTransitions())
    {
        ProfilerTransitionCallbackHelper(pRealMD, pThread, COR_PRF_TRANSITION_RETURN);
    }
#endif // PROFILING_SUPPORTED

    return;
}


// This is the factored out body of COMToCLRWorker.
FORCEINLINE_NONDEBUG
void COMToCLRWorkerBody(
    Thread * pThread,
    ComMethodFrame * pFrame,
    ComCallWrapper * pWrap,
    UINT64 * pRetValOut)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    ComCallMethodDesc* pCMD = pFrame->GetComCallMethodDesc();
    MethodDesc *pRealMD = pCMD->GetMethodDesc();

#if defined(PROFILING_SUPPORTED)
    // @TODO: PERF: x86: we are making profiler callbacks in the StubLinker stub as well as here.
    // The checks for these callbacks add about 5% to the path length, so we should remove these
    // callbacks in the next SxS release because they are redundant.
    //
    // Notify the profiler of the call into the runtime.
    // 32-bit does this callback in the stubs before calling into COMToCLRWorker().
    BOOL fNotifyProfiler = CORProfilerTrackTransitions();
    if (fNotifyProfiler)
    {
        ProfilerTransitionCallbackHelper(pRealMD, pThread, COR_PRF_TRANSITION_CALL);
    }
#endif // PROFILING_SUPPORTED

    LOG((LF_STUBS, LL_INFO1000000, "Calling COMToCLRWorker %s::%s \n", pRealMD->m_pszDebugClassName, pRealMD->m_pszDebugMethodName));

    //
    // In order to find the managed target code address and target object, we need to know
    // what scenario we're in.  We do this by switching on the flags of interest.  We include
    // the NeedsSecurityCheck flag in the calculation even though it's really orthogonal so
    // that the faster case--where no security check is needed--can be matched immediately.
    //
    PCODE pManagedTarget;
    OBJECTREF pObject;

    DWORD mask = (
        enum_IsDelegateInvoke |
        enum_IsVirtual |
        enum_NativeR4Retval |
        enum_NativeR8Retval);
    DWORD maskedFlags = pCMD->GetFlags() & mask;

    switch (maskedFlags)
    {
    case enum_IsDelegateInvoke|enum_IsVirtual:
    case enum_IsDelegateInvoke: pObject = COMToCLRGetObjectAndTarget_Delegate(pWrap, &pManagedTarget); break;
    case enum_IsVirtual:        pObject = COMToCLRGetObjectAndTarget_Virtual(pWrap, pRealMD, pCMD, &pManagedTarget); break;
    case 0:                     pObject = COMToCLRGetObjectAndTarget_NonVirtual(pWrap, pRealMD, pCMD, &pManagedTarget); break;
    default:
        COMToCLRWorkerBody_Rare(pThread, pFrame, pWrap, pRealMD, pCMD, maskedFlags, pRetValOut);
        return;
    }

    COMToCLRInvokeTarget(pManagedTarget, pObject, pCMD, pFrame, pThread, pRetValOut);

#if defined(PROFILING_SUPPORTED)
    // Notify the profiler of the return out of the runtime.
    if (fNotifyProfiler)
    {
        ProfilerTransitionCallbackHelper(pRealMD, pThread, COR_PRF_TRANSITION_RETURN);
    }
#endif // PROFILING_SUPPORTED

    return;
}

//------------------------------------------------------------------
// UINT64 __stdcall COMToCLRWorker(Thread *pThread,
//                                  ComMethodFrame* pFrame)
//------------------------------------------------------------------
extern "C" UINT64 __stdcall COMToCLRWorker(Thread *pThread, ComMethodFrame* pFrame)
{
    CONTRACTL
    {
        NOTHROW; // Although CSE can be thrown
        GC_TRIGGERS;
#if defined(TARGET_X86)
        MODE_COOPERATIVE; // X86 sets up COOP in stublinker-generated stub
#else
        // This contract is disabled because user code can illegally reenter here through no fault of the
        // CLR (i.e. it's a user code bug), so we shouldn't be popping ASSERT dialogs in those cases.  Note
        // that this reentrancy check is already done by the stublinker-generated stub on x86, so it's OK
        // to leave the MODE_ contract enabled on x86.
        DISABLED(MODE_PREEMPTIVE);
#endif
        PRECONDITION(CheckPointer(pFrame));
        PRECONDITION(CheckPointer(pThread, NULL_OK));
    }
    CONTRACTL_END;

    UINT64 retVal = 0;

    ComCallMethodDesc* pCMD = pFrame->GetComCallMethodDesc();

#if !defined(TARGET_X86)
    //
    // The following code is a transcription of the code that is generated by CreateGenericComCallStub.  The
    // idea is that we needn't really do this work either in static assembly code nor in dynamically
    // generated code since the benefit/cost ratio is low.  There are some minor differences in the below
    // code, compared to x86.  First, the reentrancy and loader lock checks are optionally compiled into the
    // stub on x86, depending on whether or not the corresponding MDAs are active at stub-generation time.
    // We must check each time at runtime here because we're using static code.
    //
    HRESULT hr = S_OK;

    pThread = GetThreadNULLOk();
    if (pThread == NULL)
    {
        pThread = SetupThreadNoThrow();
        if (pThread == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto ErrorExit;
        }
    }

    // Attempt to switch GC modes.  Note that this is performed manually just like in the x86 stub because
    // we have additional checks for shutdown races, MDAs, and thread abort that are performed only when
    // g_TrapReturningThreads is set.
    pThread->m_fPreemptiveGCDisabled.StoreWithoutBarrier(1);
    if (g_TrapReturningThreads.LoadWithoutBarrier())
    {
        hr = StubRareDisableHRWorker(pThread);
        if (S_OK != hr)
            goto ErrorExit;
    }

    // Initialize the frame's VPTR and GS cookie.
    *((TADDR*)pFrame) = ComMethodFrame::GetMethodFrameVPtr();
    *pFrame->GetGSCookiePtr() = GetProcessGSCookie();
    // Link frame into the chain.
    pFrame->Push(pThread);

#endif // !TARGET_X86

    _ASSERTE(pThread);

    // At this point we should be in preemptive GC mode (regardless of if it happened
    // in the stub or in the worker).
    _ASSERTE(pThread->PreemptiveGCDisabled());

    {
#ifndef TARGET_X86
        if (pCMD->IsFieldCall())
        {
            retVal = FieldCallWorker(pThread, pFrame);
        }
        else
#endif // !TARGET_X86
        {
            IUnknown **pip = (IUnknown **)pFrame->GetPointerToArguments();
            IUnknown *pUnk = (IUnknown *)*pip;
            _ASSERTE(pUnk != NULL);

            // Obtain the managed 'this' for the call
            ComCallWrapper *pWrap = ComCallWrapper::GetWrapperFromIP(pUnk);
            COMToCLRWorkerBody(pThread, pFrame, pWrap, &retVal);
        }
    }

#ifndef TARGET_X86
    // Note: the EH subsystem will handle reseting the frame chain and setting
    // the correct GC mode on exception.
    pFrame->Pop(pThread);
    pThread->EnablePreemptiveGC();
#endif

    LOG((LF_STUBS, LL_INFO1000000, "COMToCLRWorker leave\n"));

    // The call was successful. If the native return type is a floating point
    // value, then we need to set the floating point registers appropriately.
    if (pCMD->IsNativeFloatingPointRetVal()) // single check skips both cases
    {
        if (pCMD->IsNativeR4RetVal())
            setFPReturn(4, retVal);
        else
            setFPReturn(8, retVal);
    }
    return retVal;

#ifndef TARGET_X86
ErrorExit:
    if (pThread != NULL && pThread->PreemptiveGCDisabled())
        pThread->EnablePreemptiveGC();

    // The call failed so we need to report an error to the caller.
    if (pCMD->IsNativeHResultRetVal())
    {
        _ASSERTE(FAILED(hr));
        retVal = hr;
    }
    else if (pCMD->IsNativeBoolRetVal())
    {
        retVal = FALSE;
    }
    else if (pCMD->IsNativeR4RetVal())
    {
        setFPReturn(4, CLR_NAN_32);
    }
    else if (pCMD->IsNativeR8RetVal())
    {
        setFPReturn(8, CLR_NAN_64);
    }
    else
    {
        _ASSERTE(pCMD->IsNativeVoidRetVal());
    }

    return retVal;
#endif // TARGET_X86
}

#if defined(_MSC_VER) && !defined(_DEBUG)
#pragma optimize("", on)   // restore settings
#endif


static UINT64 __stdcall FieldCallWorker(Thread *pThread, ComMethodFrame* pFrame)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        ENTRY_POINT;
        PRECONDITION(CheckPointer(pThread));
        PRECONDITION(CheckPointer(pFrame));
    }
    CONTRACTL_END;

    LOG((LF_STUBS, LL_INFO1000000, "FieldCallWorker enter\n"));

    HRESULT hrRetVal = S_OK;

    IUnknown** pip = (IUnknown **)pFrame->GetPointerToArguments();
    IUnknown* pUnk = (IUnknown *)*pip;
    _ASSERTE(pUnk != NULL);

    ComCallWrapper* pWrap =  ComCallWrapper::GetWrapperFromIP(pUnk);
    _ASSERTE(pWrap != NULL);

    GCX_ASSERT_COOP();
    OBJECTREF pThrowable = NULL;
    GCPROTECT_BEGIN(pThrowable);
    {
        EX_TRY
        {
            FieldCallWorkerDebuggerWrapper(pThread, pFrame);
        }
        EX_CATCH
        {
            pThrowable = GET_THROWABLE();
        }
        EX_END_CATCH(SwallowAllExceptions);

        if (pThrowable != NULL)
        {
            // Transform the exception into an HRESULT. This also sets up
            // an IErrorInfo on the current thread for the exception.
            hrRetVal = SetupErrorInfo(pThrowable);
        }
    }

    GCPROTECT_END();

    LOG((LF_STUBS, LL_INFO1000000, "FieldCallWorker leave\n"));

    return hrRetVal;
}

static void FieldCallWorkerDebuggerWrapper(Thread *pThread, ComMethodFrame* pFrame)
{
    // Use static contracts b/c we have SEH.
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    struct Param : public NotifyOfCHFFilterWrapperParam {
        Thread*         pThread;
    } param;
    param.pFrame = pFrame;
    param.pThread = pThread;

    // @todo - we have a PAL_TRY/PAL_EXCEPT here as a general (cross-platform) way to get a 1st-pass
    // filter. If that's bad perf, we could inline an FS:0 handler for x86-only; and then inline
    // both this wrapper and the main body.
    PAL_TRY(Param *, pParam, &param)
    {
        FieldCallWorkerBody(pParam->pThread, (ComMethodFrame*)pParam->pFrame);
    }
    PAL_EXCEPT_FILTER(NotifyOfCHFFilterWrapper)
    {
        // Should never reach here b/c handler should always continue search.
        _ASSERTE(false);
    }
    PAL_ENDTRY
}

static void FieldCallWorkerBody(Thread *pThread, ComMethodFrame* pFrame)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;     // Dependant on machine type (X86 sets COOP in stub)
        PRECONDITION(CheckPointer(pThread));
        PRECONDITION(CheckPointer(pFrame));
    }
    CONTRACTL_END;

    IUnknown** pip = (IUnknown **)pFrame->GetPointerToArguments();
    IUnknown* pUnk = (IUnknown *)*pip;
    _ASSERTE(pUnk != NULL);

    ComCallWrapper* pWrap =  ComCallWrapper::GetWrapperFromIP(pUnk);
    _ASSERTE(pWrap != NULL);

    ComCallMethodDesc *pCMD = pFrame->GetComCallMethodDesc();
    _ASSERTE(pCMD->IsFieldCall());
    _ASSERTE(pCMD->IsNativeHResultRetVal());

#ifdef PROFILING_SUPPORTED
    // Notify the profiler of the call into the runtime.
    // 32-bit does this callback in the stubs before calling into FieldCallWorker().
    if (CORProfilerTrackTransitions())
    {
        MethodDesc* pMD = pCMD->GetMethodDesc();
        ProfilerTransitionCallbackHelper(pMD, pThread, COR_PRF_TRANSITION_CALL);
    }
#endif // PROFILING_SUPPORTED

    UINT64 retVal;
    InvokeStub(pCMD, NULL, pWrap->GetObjectRef(), pFrame, pThread, &retVal);

#ifdef PROFILING_SUPPORTED
    // Notify the profiler of the return out of the runtime.
    if (CORProfilerTrackTransitions())
    {
        MethodDesc* pMD = pCMD->GetMethodDesc();
        ProfilerTransitionCallbackHelper(pMD, pThread, COR_PRF_TRANSITION_RETURN);
    }
#endif // PROFILING_SUPPORTED
}

//---------------------------------------------------------
PCODE ComCallMethodDesc::CreateCOMToCLRStub(DWORD dwStubFlags, MethodDesc **ppStubMD)
{
    CONTRACT(PCODE)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(ppStubMD));
        POSTCONDITION(CheckPointer(*ppStubMD));
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    MethodDesc * pStubMD;

    if (IsFieldCall())
    {
        FieldDesc *pFD = GetFieldDesc();
        pStubMD = ComCall::GetILStubMethodDesc(pFD, dwStubFlags);
    }
    else
    {
        // if this represents a ctor or static, use the class method (i.e. the actual ctor or static)
        MethodDesc *pMD = GetCallMethodDesc();

        // first see if we have an NGENed stub
        pStubMD = GetStubMethodDescFromInteropMethodDesc(pMD, dwStubFlags);
        if (pStubMD != NULL)
        {
            pStubMD = RestoreNGENedStub(pStubMD);
        }
        if (pStubMD == NULL)
        {
            // no NGENed stub - create a new one
            pStubMD = ComCall::GetILStubMethodDesc(pMD, dwStubFlags);
        }
    }

    *ppStubMD = pStubMD;

#ifdef TARGET_X86
    // make sure our native stack computation in code:ComCallMethodDesc.InitNativeInfo is right
    _ASSERTE(HasMarshalError() || !pStubMD->IsILStub() || pStubMD->AsDynamicMethodDesc()->GetNativeStackArgSize() == m_StackBytes);
#else // TARGET_X86
    if (pStubMD->IsILStub())
    {
        m_StackBytes = pStubMD->AsDynamicMethodDesc()->GetNativeStackArgSize();
        _ASSERTE(m_StackBytes == pStubMD->SizeOfArgStack());
    }
    else
    {
        m_StackBytes = pStubMD->SizeOfArgStack();
    }
#endif // TARGET_X86

    RETURN JitILStub(pStubMD);
}

//---------------------------------------------------------
void ComCallMethodDesc::InitRuntimeNativeInfo(MethodDesc *pStubMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pStubMD));
    }
    CONTRACTL_END;

#ifdef TARGET_X86
    // Parse the stub signature to figure out how we're going to transform the incoming arguments
    // into stub arguments (i.e. ECX and possibly EDX get enregisterable args, stack gets reversed).

    MetaSig msig(pStubMD);
    ArgIterator argit(&msig);

    UINT dwArgStack = argit.SizeOfArgStack();
    if (!FitsInU2(dwArgStack))
        COMPlusThrow(kMarshalDirectiveException, IDS_EE_SIGTOOCOMPLEX);

    NewArrayHolder<UINT16> pwStubStackSlotOffsets;
    UINT16 *pOutputStack = NULL;

    UINT16 wStubStackSlotCount = static_cast<UINT16>(dwArgStack) / TARGET_POINTER_SIZE;
    if (wStubStackSlotCount > 0)
    {
        pwStubStackSlotOffsets = new UINT16[wStubStackSlotCount];
        pOutputStack = pwStubStackSlotOffsets + wStubStackSlotCount;
    }

    UINT16 wSourceSlotEDX = (UINT16)-1;

    int numRegistersUsed = 0;
    UINT16 wInputStack   = 0;

    // process this
    if (!pStubMD->IsStatic())
    {
        numRegistersUsed++;
        wInputStack += TARGET_POINTER_SIZE;
    }

    // process the return buffer parameter
    if (argit.HasRetBuffArg())
    {
        numRegistersUsed++;
        wSourceSlotEDX = wInputStack / TARGET_POINTER_SIZE;
        wInputStack += TARGET_POINTER_SIZE;
    }

    // process ordinary parameters
    for (UINT i = msig.NumFixedArgs(); i > 0; i--)
    {
        TypeHandle thValueType;
        CorElementType type = msig.NextArgNormalized(&thValueType);

        UINT cbSize = MetaSig::GetElemSize(type, thValueType);

        if (ArgIterator::IsArgumentInRegister(&numRegistersUsed, type, thValueType))
        {
            wSourceSlotEDX = wInputStack / TARGET_POINTER_SIZE;
            wInputStack += TARGET_POINTER_SIZE;
        }
        else
        {
            // we may need more stack slots for larger parameters
            UINT slotsCount = StackElemSize(cbSize) / TARGET_POINTER_SIZE;
            pOutputStack -= slotsCount;
            for (UINT slot = 0; slot < slotsCount; slot++)
            {
                pOutputStack[slot] = wInputStack;
                wInputStack += TARGET_POINTER_SIZE;
            }
        }
    }

    // write the computed data into this ComCallMethodDesc
    m_dwSlotInfo = (wSourceSlotEDX | (wStubStackSlotCount << 16));
    if (pwStubStackSlotOffsets != NULL)
    {
        if (FastInterlockCompareExchangePointer(&m_pwStubStackSlotOffsets, pwStubStackSlotOffsets.GetValue(), NULL) == NULL)
        {
            pwStubStackSlotOffsets.SuppressRelease();
        }
    }

    //
    // Fill in return thunk with proper native arg size.
    //

    BYTE *pMethodDescMemory = ((BYTE*)this) + GetOffsetOfReturnThunk();

    //
    // encodes a "ret nativeArgSize" to return and
    // pop off the args off the stack
    //
    pMethodDescMemory[0] = 0xc2;

    UINT16 nativeArgSize = GetNumStackBytes();

    if (!(nativeArgSize < 0x7fff))
        COMPlusThrow(kTypeLoadException, IDS_EE_SIGTOOCOMPLEX);

    *(SHORT *)&pMethodDescMemory[1] = nativeArgSize;

    FlushInstructionCache(GetCurrentProcess(), pMethodDescMemory, sizeof pMethodDescMemory[0] + sizeof(SHORT));
#endif // TARGET_X86
}
#endif //CROSSGEN_COMPILE

void ComCallMethodDesc::InitMethod(MethodDesc *pMD, MethodDesc *pInterfaceMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    m_flags = pMD->IsVirtual() ? enum_IsVirtual : 0;

    m_pMD = pMD;
    m_pInterfaceMD = PTR_MethodDesc(pInterfaceMD);
    m_pILStub = NULL;

#ifdef TARGET_X86
    m_dwSlotInfo = 0;
    m_pwStubStackSlotOffsets = NULL;
#endif // TARGET_X86

    if (!SystemDomain::GetCurrentDomain()->IsCompilationDomain())
    {
        // Initialize the native type information size of native stack, native retval flags, etc).
        InitNativeInfo();
    }

    if (pMD->IsEEImpl() && COMDelegate::IsDelegateInvokeMethod(pMD))
    {
        m_flags |= enum_IsDelegateInvoke;
    }
}

void ComCallMethodDesc::InitField(FieldDesc* pFD, BOOL isGetter)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    m_pFD = pFD;
    m_pILStub = NULL;

#ifdef TARGET_X86
    m_dwSlotInfo = 0;
    m_pwStubStackSlotOffsets = NULL;
#endif // TARGET_X86

    m_flags = enum_IsFieldCall; // mark the attribute as a field
    m_flags |= isGetter ? enum_IsGetter : 0;

    if (!SystemDomain::GetCurrentDomain()->IsCompilationDomain())
    {
        // Initialize the native type information size of native stack, native retval flags, etc).
        InitNativeInfo();
    }
};

// Initialize the member's native type information (size of native stack, native retval flags, etc).
// It is unfortunate that we have to touch all this metadata at creation time. The reason for this
// is that we need to know size of the native stack to be able to return back to unmanaged code in
// case ComPrestub fails. If it fails because the target appdomain has already been unloaded, it is
// too late to make this computation - the metadata is no longer available.
void ComCallMethodDesc::InitNativeInfo()
{
    CONTRACT_VOID
    {
        STANDARD_VM_CHECK;
        PRECONDITION(!IsNativeInfoInitialized());
    }
    CONTRACT_END;

    m_StackBytes = (UINT16)-1;

    EX_TRY
    {
#ifdef TARGET_X86
        // On x86, this method has to compute size of arguments because we need to know size of the native stack
        // to be able to return back to unmanaged code
        UINT16 nativeArgSize;
#endif

        if (IsFieldCall())
        {
            FieldDesc          *pFD = GetFieldDesc();
            _ASSERTE(pFD != NULL);

#ifdef _DEBUG
            LPCUTF8             szDebugName = pFD->GetDebugName();
            LPCUTF8             szDebugClassName = pFD->GetEnclosingMethodTable()->GetDebugClassName();

            if (g_pConfig->ShouldBreakOnComToClrNativeInfoInit(szDebugName))
                CONSISTENCY_CHECK_MSGF(false, ("BreakOnComToClrNativeInfoInit: '%s' ", szDebugName));
#endif // _DEBUG

#ifdef TARGET_X86
            MetaSig fsig(pFD);
            fsig.NextArg();

            // Look up the best fit mapping info via Assembly & Interface level attributes
            BOOL BestFit = TRUE;
            BOOL ThrowOnUnmappableChar = FALSE;
            ReadBestFitCustomAttribute(fsig.GetModule(), pFD->GetEnclosingMethodTable()->GetCl(), &BestFit, &ThrowOnUnmappableChar);

            MarshalInfo info(fsig.GetModule(), fsig.GetArgProps(), fsig.GetSigTypeContext(), pFD->GetMemberDef(), MarshalInfo::MARSHAL_SCENARIO_COMINTEROP,
                             (CorNativeLinkType)0, (CorNativeLinkFlags)0,
                             FALSE, 0, fsig.NumFixedArgs(), BestFit, ThrowOnUnmappableChar, FALSE, NULL, FALSE
#ifdef _DEBUG
                             , szDebugName, szDebugClassName, 0
#endif
                             );

            if (IsFieldGetter())
            {
                // getter takes 'this' and the output argument by-ref
                nativeArgSize = sizeof(void *) + sizeof(void *);
            }
            else
            {
                info.SetupArgumentSizes();

                // setter takes 'this' and the input argument by-value
                nativeArgSize = sizeof(void *) + info.GetNativeArgSize();
            }
#endif // TARGET_X86

            // Field calls always return HRESULTs.
            m_flags |= enum_NativeHResultRetVal;
        }
        else
        {
            MethodDesc *pMD = GetCallMethodDesc();

#ifdef _DEBUG
            LPCUTF8         szDebugName = pMD->m_pszDebugMethodName;
            LPCUTF8         szDebugClassName = pMD->m_pszDebugClassName;

            if (g_pConfig->ShouldBreakOnComToClrNativeInfoInit(szDebugName))
                CONSISTENCY_CHECK_MSGF(false, ("BreakOnComToClrNativeInfoInit: '%s' ", szDebugName));
#endif // _DEBUG

            MethodTable * pMT = pMD->GetMethodTable();
            IMDInternalImport * pInternalImport = pMT->GetMDImport();

            mdMethodDef md = pMD->GetMemberDef();

            ULONG ulCodeRVA;
            DWORD dwImplFlags;
            IfFailThrow(pInternalImport->GetMethodImplProps(md, &ulCodeRVA, &dwImplFlags));

            // Determine if we need to do HRESULT munging for this method.
            BOOL fPreserveSig = IsMiPreserveSig(dwImplFlags);

#ifndef TARGET_X86
            if (!fPreserveSig)
            {
                // PreserveSig=false methods always return HRESULTs.
                m_flags |= enum_NativeHResultRetVal;
                goto Done;
            }
#endif

            MetaSig msig(pMD);

#ifndef TARGET_X86
            if (msig.IsReturnTypeVoid())
            {
                // The method has a void return type on the native side.
                m_flags |= enum_NativeVoidRetVal;
                goto Done;
            }
#endif

            // Look up the best fit mapping info via Assembly & Interface level attributes
            BOOL BestFit = TRUE;
            BOOL ThrowOnUnmappableChar = FALSE;
            ReadBestFitCustomAttribute(pMD, &BestFit, &ThrowOnUnmappableChar);

            int numArgs = msig.NumFixedArgs();

            // Collects ParamDef information in an indexed array where element 0 represents
            // the return type.
            mdParamDef *params = (mdParamDef*)_alloca((numArgs+1) * sizeof(mdParamDef));
            CollateParamTokens(pInternalImport, md, numArgs, params);

#ifdef TARGET_X86
            // If this is a method call then check to see if we need to do LCID conversion.
            int iLCIDArg = GetLCIDParameterIndex(pMD);
            if (iLCIDArg != -1)
                iLCIDArg++;

            nativeArgSize = sizeof(void*);

            int iArg = 1;
            CorElementType mtype;
            while (ELEMENT_TYPE_END != (mtype = msig.NextArg()))
            {
                // Check to see if this is the parameter after which we need to read the LCID from.
                if (iArg == iLCIDArg)
                    nativeArgSize += StackElemSize(sizeof(LCID));

                MarshalInfo info(msig.GetModule(), msig.GetArgProps(), msig.GetSigTypeContext(), params[iArg],
                                 MarshalInfo::MARSHAL_SCENARIO_COMINTEROP,
                                 (CorNativeLinkType)0, (CorNativeLinkFlags)0,
                                 TRUE, iArg, numArgs, BestFit, ThrowOnUnmappableChar, FALSE, pMD, FALSE
#ifdef _DEBUG
                                 , szDebugName, szDebugClassName, iArg
#endif
                                 );

                if (info.GetMarshalType() == MarshalInfo::MARSHAL_TYPE_UNKNOWN)
                {
                    nativeArgSize += StackElemSize(sizeof(LPVOID));
                    m_flags |= enum_HasMarshalError;
                }
                else
                {
                    info.SetupArgumentSizes();

                    nativeArgSize += info.GetNativeArgSize();
                }

                ++iArg;
            }

            // Check to see if this is the parameter after which we need to read the LCID from.
            if (iArg == iLCIDArg)
                nativeArgSize += StackElemSize(sizeof(LCID));
#endif // TARGET_X86


            //
            // Return value
            //

#ifndef TARGET_X86
            // Handled above
            _ASSERTE(!msig.IsReturnTypeVoid());
#else
            if (msig.IsReturnTypeVoid())
            {
                if (!fPreserveSig)
                {
                    // PreserveSig=false methods always return HRESULTs.
                    m_flags |= enum_NativeHResultRetVal;
                }
                else
                {
                    // The method has a void return type on the native side.
                    m_flags |= enum_NativeVoidRetVal;
                }

                goto Done;
            }
#endif // TARGET_X86

            {
                MarshalInfo info(msig.GetModule(), msig.GetReturnProps(), msig.GetSigTypeContext(), params[0],
                                    MarshalInfo::MARSHAL_SCENARIO_COMINTEROP,
                                    (CorNativeLinkType)0, (CorNativeLinkFlags)0,
                                    FALSE, 0, numArgs, BestFit, ThrowOnUnmappableChar, FALSE, pMD, FALSE
#ifdef _DEBUG
                                ,szDebugName, szDebugClassName, 0
#endif
                );

#ifndef TARGET_X86
                // Handled above
                _ASSERTE(fPreserveSig);
#else
                if (!fPreserveSig)
                {
                    // PreserveSig=false methods always return HRESULTs.
                    m_flags |= enum_NativeHResultRetVal;

                    // count the output by-ref argument
                    nativeArgSize += sizeof(void *);

                    goto Done;
                }
#endif // TARGET_X86

                // Ignore the secret return buffer argument - we don't allow returning
                // structures by value in COM interop.
                if (info.IsFpuReturn())
                {
                    if (info.GetMarshalType() == MarshalInfo::MARSHAL_TYPE_FLOAT)
                    {
                        m_flags |= enum_NativeR4Retval;
                    }
                    else
                    {
                        _ASSERTE(info.GetMarshalType() == MarshalInfo::MARSHAL_TYPE_DOUBLE);
                        m_flags |= enum_NativeR8Retval;
                    }
                }
                else
                {
                    CorElementType returnType = msig.GetReturnType();
                    if (returnType == ELEMENT_TYPE_I4 || returnType == ELEMENT_TYPE_U4)
                    {
                        // If the method is PreserveSig=true and returns either an I4 or an U4, then we
                        // will assume the users wants to return an HRESULT in case of failure.
                        m_flags |= enum_NativeHResultRetVal;
                    }
                    else if (info.GetMarshalType() == MarshalInfo::MARSHAL_TYPE_DATE)
                    {
                        // DateTime is returned as an OLEAUT DATE which is actually an R8.
                        m_flags |= enum_NativeR8Retval;
                    }
                    else
                    {
                        // The method doesn't return an FP value nor should we treat it as returning
                        // an HRESULT so we will return 0 in case of failure.
                        m_flags |= enum_NativeBoolRetVal;
                    }
                }
            }
        }

Done:

#ifdef TARGET_X86
        // The above algorithm to compute nativeArgSize is x86-specific. We will compute
        // the correct value later for other platforms.
        m_StackBytes = nativeArgSize;
#endif

        m_flags |= enum_NativeInfoInitialized;
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(RethrowTransientExceptions)

    RETURN;
}

SpinLock* ComCall::s_plock=NULL;

//---------------------------------------------------------
// One-time init
//---------------------------------------------------------
/*static*/
void ComCall::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    s_plock = new SpinLock();
    s_plock->Init(LOCK_COMCALL);
}

//
/*static*/
void ComCall::PopulateComCallMethodDesc(ComCallMethodDesc *pCMD, DWORD *pdwStubFlags)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCMD));
        PRECONDITION(CheckPointer(pdwStubFlags));
    }
    CONTRACTL_END;

    DWORD dwStubFlags = NDIRECTSTUB_FL_COM | NDIRECTSTUB_FL_REVERSE_INTEROP;

    BOOL BestFit               = TRUE;
    BOOL ThrowOnUnmappableChar = FALSE;

    if (pCMD->IsFieldCall())
    {
        if (pCMD->IsFieldGetter())
            dwStubFlags |= NDIRECTSTUB_FL_FIELDGETTER;
        else
            dwStubFlags |= NDIRECTSTUB_FL_FIELDSETTER;

        FieldDesc *pFD = pCMD->GetFieldDesc();
        _ASSERTE(IsMemberVisibleFromCom(pFD->GetApproxEnclosingMethodTable(), pFD->GetMemberDef(), mdTokenNil) && "Calls are not permitted on this member since it isn't visible from COM. The only way you can have reached this code path is if your native interface doesn't match the managed interface.");

        MethodTable *pMT = pFD->GetEnclosingMethodTable();
        ReadBestFitCustomAttribute(pMT->GetModule(), pMT->GetCl(), &BestFit, &ThrowOnUnmappableChar);
    }
    else
    {
        MethodDesc *pMD = pCMD->GetCallMethodDesc();
        _ASSERTE(IsMethodVisibleFromCom(pMD) && "Calls are not permitted on this member since it isn't visible from COM. The only way you can have reached this code path is if your native interface doesn't match the managed interface.");

        // Marshaling is fully described by the parameter type in WinRT. BestFit custom attributes
        // are not going to affect the marshaling behavior.
        ReadBestFitCustomAttribute(pMD, &BestFit, &ThrowOnUnmappableChar);
    }

    if (BestFit)
        dwStubFlags |= NDIRECTSTUB_FL_BESTFIT;

    if (ThrowOnUnmappableChar)
        dwStubFlags |= NDIRECTSTUB_FL_THROWONUNMAPPABLECHAR;

    //
    // fill in out param
    //
    *pdwStubFlags = dwStubFlags;
}

#ifndef CROSSGEN_COMPILE
#ifdef TARGET_X86
//---------------------------------------------------------
// Creates the generic ComCall stub.
//
//  Throws in case of error.
//---------------------------------------------------------
/*static*/
Stub* ComCall::CreateGenericComCallStub(BOOL isFieldAccess)
{
    CONTRACT (Stub*)
    {
        STANDARD_VM_CHECK;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    CPUSTUBLINKER sl;
    CPUSTUBLINKER *psl = &sl;

    // These new CodeLabels are allocated on a
    //  "throwaway" heap.  Do not worry about
    //  deallocating them if one of the allocations
    //  ends up throwing an OOM exception.

    CodeLabel* rgRareLabels[] = {
                                  psl->NewCodeLabel(),
                                  psl->NewCodeLabel(),
                                  psl->NewCodeLabel()
                                };


    CodeLabel* rgRejoinLabels[] = {
                                    psl->NewCodeLabel(),
                                    psl->NewCodeLabel(),
                                    psl->NewCodeLabel()
                                  };

    // Pop ComCallMethodDesc* pushed by prestub
    psl->X86EmitPopReg(kEAX);

    // emit the initial prolog
    // NOTE: Don't profile field accesses yet.
    psl->EmitComMethodStubProlog(ComMethodFrame::GetMethodFrameVPtr(),
                                 rgRareLabels,
                                 rgRejoinLabels,
                                 !isFieldAccess);

    // ******* NOTE ********
    // We now have a frame on the stack that is unproctected by an SEH handler.  If we take an
    // SO before getting into the target, we'll have a corrupted frame chain.  In EmitComMethodStubProlog
    // we probe-by-touch for 4 DWORDS to ensure that can set up the SEH handler before linking in
    // the frame.   So long as we don't use more than that here (currently 3 DWORDS - for the two args plus
    // the return address, we are OK.  If we decrement ESP more than an additional DWORD here before
    // calling the target, we will need to probe farther.

    psl->X86EmitPushReg(kESI);      // push frame as an ARG
    psl->X86EmitPushReg(kEBX);      // push ebx (push current thread as ARG)
    LPVOID pTarget = isFieldAccess ? (LPVOID)FieldCallWorker : (LPVOID)COMToCLRWorker;
    psl->X86EmitCall(psl->NewExternalCodeLabel(pTarget), 8);

    // emit the epilog
    // NOTE: Don't profile field accesses yet.
    psl->EmitSharedComMethodStubEpilog(ComMethodFrame::GetMethodFrameVPtr(), rgRareLabels, rgRejoinLabels,
                                       ComCallMethodDesc::GetOffsetOfReturnThunk(), !isFieldAccess);

    // Process-wide stubs that never unload.
    RETURN (psl->Link(SystemDomain::GetGlobalLoaderAllocator()->GetStubHeap()));
}
#endif // TARGET_X86

//---------------------------------------------------------
// Either creates or retrieves from the cache, a stub to
// invoke ComCall methods. Each call refcounts the returned stub.
// This routines throws an exception rather than returning
// NULL.
//---------------------------------------------------------
/*static*/
PCODE ComCall::GetComCallMethodStub(ComCallMethodDesc *pCMD)
{
    CONTRACT (PCODE)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCMD));
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    SetupGenericStubs();

    // The stub style we return is to a single generic stub for method calls and to
    // a single generic stub for field accesses.  The generic stub parameterizes
    // its behavior based on the ComCallMethodDesc.

    PCODE             pTempILStub  = NULL;
    DWORD             dwStubFlags;

    PopulateComCallMethodDesc(pCMD, &dwStubFlags);

    MethodDesc *pStubMD;
    pTempILStub = pCMD->CreateCOMToCLRStub(dwStubFlags, &pStubMD);

    // Compute stack layout and prepare the return thunk on x86
    pCMD->InitRuntimeNativeInfo(pStubMD);

    InterlockedCompareExchangeT<PCODE>(pCMD->GetAddrOfILStubField(), pTempILStub, NULL);

#ifdef TARGET_X86
    // Finally, we need to build a stub that represents the entire call.  This
    // is always generic.
    RETURN (pCMD->IsFieldCall() ? g_pGenericComCallStubFields : g_pGenericComCallStub);
#else
    RETURN GetEEFuncEntryPoint(GenericComCallStub);
#endif
}
#endif // CROSSGEN_COMPILE

// Called both at run-time and by NGEN - generates method stub.
/*static*/
MethodDesc* ComCall::GetILStubMethodDesc(MethodDesc *pCallMD, DWORD dwStubFlags)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCallMD));
        PRECONDITION(SF_IsReverseCOMStub(dwStubFlags));
    }
    CONTRACTL_END;

    // Get the call signature information
    StubSigDesc sigDesc(pCallMD);

    return NDirect::CreateCLRToNativeILStub(&sigDesc,
                                            (CorNativeLinkType)0,
                                            (CorNativeLinkFlags)0,
                                            MetaSig::GetDefaultUnmanagedCallingConvention(),
                                            dwStubFlags);
}

// Called at run-time - generates field access stub. We don't currently NGEN field access stubs
// as the scenario is too rare to justify the extra NGEN logic. The workaround is trivial - make
// the field non-public and add a public property to access it.
/*static*/
MethodDesc* ComCall::GetILStubMethodDesc(FieldDesc *pFD, DWORD dwStubFlags)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pFD));
        PRECONDITION(SF_IsFieldGetterStub(dwStubFlags) || SF_IsFieldSetterStub(dwStubFlags));
    }
    CONTRACTL_END;

    PCCOR_SIGNATURE pSig;
    DWORD           cSig;

    // Get the field signature information
    pFD->GetSig(&pSig, &cSig);

    return NDirect::CreateFieldAccessILStub(pSig,
                                            cSig,
                                            pFD->GetModule(),
                                            pFD->GetMemberDef(),
                                            dwStubFlags,
                                            pFD);
}

#endif // DACCESS_COMPILE
