//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
#include "security.h"
#include "virtualcallstub.h"
#include "dllimport.h"
#include "mlinfo.h"
#include "dbginterface.h"
#include "mdaassistants.h"
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

#ifdef _TARGET_X86_
static PCODE g_pGenericComCallStubFields = NULL;
static PCODE g_pGenericComCallStub       = NULL;
#endif

UINT64 FieldCallWorker(Thread *pThread, ComMethodFrame* pFrame);
void FieldCallWorkerDebuggerWrapper(Thread *pThread, ComMethodFrame* pFrame);
void FieldCallWorkerBody(Thread *pThread, ComMethodFrame* pFrame);
extern "C" HRESULT STDCALL StubRareDisableHRWorker(Thread *pThread);

#ifndef CROSSGEN_COMPILE
//---------------------------------------------------------
// void SetupGenericStubs()
//
//  Throws on failure
//---------------------------------------------------------
static void SetupGenericStubs()
{
    STANDARD_VM_CONTRACT;
    
#ifdef _TARGET_X86_
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
#endif // _TARGET_X86_
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
        SO_TOLERANT;
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

    // @todo -  We need to probe here, but can't introduce destructors etc.
    BEGIN_CONTRACT_VIOLATION(SOToleranceViolation);


    // WARNING!!!!
    // when we start executing here, we are actually in cooperative mode.  But we
    // haven't synchronized with the barrier to reentry yet.  So we are in a highly
    // dangerous mode.  If we call managed code, we will potentially be active in
    // the GC heap, even as GC's are occuring!

    // Check for ShutDown scenario.  This happens only when we have initiated shutdown 
    // and someone is trying to call in after the CLR is suspended.  In that case, we
    // must either raise an unmanaged exception or return an HRESULT, depending on the
    // expectations of our caller.
    if (!CanRunManagedCode())
    {
        hr = E_PROCESS_SHUTDOWN_REENTRY;
    }
    else
    {
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
    }

    // should always be in coop mode here
    _ASSERTE(pThread->PreemptiveGCDisabled());

    END_CONTRACT_VIOLATION;

    // Note that this code does not handle rare signatures that do not return HRESULT properly

    return hr;
}

#ifdef _TARGET_X86_

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
    // NOTE! We do not use BEGIN_CALL_TO_MANAGEDEX around this call because we stayed in the SO_TOLERANT
    // mode and COMToCLRDispatchHelper is responsible for pushing/popping the CPFH into the FS:0 chain.
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

#else // _TARGET_X86_

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

    DWORD dwStackSlots = pCMD->GetNumStackBytes() / STACK_ELEM_SIZE;

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

#endif // _TARGET_X86_

NOINLINE
void InvokeStub_Hosted(ComCallMethodDesc *pCMD, PCODE pManagedTarget, OBJECTREF orThis, ComMethodFrame *pFrame, Thread *pThread,
                       UINT64* pRetValOut)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(CLRTaskHosted());

    ReverseEnterRuntimeHolderNoThrow REHolder;
    HRESULT hr = REHolder.AcquireNoThrow(); 
    if (FAILED(hr))
    {
        *pRetValOut = hr;
        return;
    }

    InvokeStub(pCMD, pManagedTarget, orThis, pFrame, pThread, pRetValOut);
}

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
        SO_TOLERANT; 
    }
    CONTRACTL_END;

    DELEGATEREF pDelObj = (DELEGATEREF)pWrap->GetObjectRef();
    _ASSERTE(pDelObj->GetMethodTable()->IsDelegate());

    // We don't have to go through the Invoke slot because we know what the delegate
    // target is. This is the same optimization that reverse P/Invoke stubs do.
    *ppManagedTargetOut = (PCODE)pDelObj->GetMethodPtr();
    return pDelObj->GetTarget();
}

// returns true on success, false otherwise
NOINLINE // keep the EH tax out of our caller
bool COMToCLRGetObjectAndTarget_WinRTCtor(Thread * pThread, MethodDesc * pRealMD, ComCallMethodDesc * pCMD, PCODE * ppManagedTargetOut, 
                                          OBJECTREF* pObjectOut, UINT64* pRetValOut)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_TOLERANT; 
    }
    CONTRACTL_END;

    // Ctor is not virtual and operates on a newly created object.
    _ASSERTE(!pCMD->IsVirtual());

    *pObjectOut = NULL;
    *ppManagedTargetOut = pRealMD->GetSingleCallableAddrOfCode();
    MethodTable *pMT = pRealMD->GetMethodTable();

    // We should not see a unsealed class here
    _ASSERTE(pMT->IsSealed());
    
    // we know for sure that we are allocating a new object

    // @TODO: move this object allocation into the IL stub to avoid the try/catch and SO-intolerant region.

    bool fSuccess = true;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(pThread, { *pRetValOut = COR_E_STACKOVERFLOW; return false; } );

    EX_TRY
    {
        *pObjectOut = AllocateObject(pMT);
    }
    EX_CATCH
    {
        fSuccess = false;
        *pRetValOut = SetupErrorInfo(GET_THROWABLE());
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_SO_INTOLERANT_CODE;

    return fSuccess;
}

FORCEINLINE_NONDEBUG
OBJECTREF COMToCLRGetObjectAndTarget_Virtual(ComCallWrapper * pWrap, MethodDesc * pRealMD, ComCallMethodDesc * pCMD, PCODE * ppManagedTargetOut)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_TOLERANT; 
    }
    CONTRACTL_END;

    OBJECTREF pObject = pWrap->GetObjectRef();

    MethodTable *pMT = pObject->GetMethodTable();
        
    if (pMT->IsTransparentProxy() || pRealMD->IsInterface())
    {
        // For transparent proxies, we need to call on the interface method desc if
        // this method represents an interface method and not an IClassX method.           
        *ppManagedTargetOut = pCMD->GetCallMethodDesc()->GetSingleCallableAddrOfCode();
    }
    else if (pWrap->IsAggregated() && pWrap->GetComCallWrapperTemplate()->GetClassType().IsExportedToWinRT())
    {
        // we know the slot number for this method desc, grab the actual
        // address from the vtable for this slot. The slot number should
        // remain the same through out the heirarchy.
        //
        // This is the WinRT inheritance case where we want to always call the method as
        // most recently implemented in the managed world.
        *ppManagedTargetOut = pWrap->GetComCallWrapperTemplate()->GetClassType().GetMethodTable()->GetSlot(pCMD->GetSlot());
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
        SO_TOLERANT; 
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
        SO_TOLERANT; 
    }
    CONTRACTL_END;

#ifdef DEBUGGING_SUPPORTED
    if (CORDebuggerTraceCall())
    {
        g_pDebugInterface->TraceCall((const BYTE *)pManagedTarget);
    }
#endif // DEBUGGING_SUPPORTED


    if (CLRTaskHosted())
    {
        InvokeStub_Hosted(pCMD, pManagedTarget, pObject, pFrame, pThread, pRetValOut);
    }
    else
    {
        InvokeStub(pCMD, pManagedTarget, pObject, pFrame, pThread, pRetValOut);
    }
}

bool COMToCLRWorkerBody_SecurityCheck(ComCallMethodDesc * pCMD, MethodDesc * pMD, Thread * pThread, UINT64 * pRetValOut)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_TOLERANT; 
    }
    CONTRACTL_END;

    bool result = true;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(pThread, { *pRetValOut = COR_E_STACKOVERFLOW; return false; } );

    EX_TRY
    {

        // Need to check for the presence of a security link demand on the target
        // method. If we're hosted inside of an app domain with security, we perform
        // the link demand against that app domain's grant set.
        Security::CheckLinkDemandAgainstAppDomain(pMD);

        if (pCMD->IsEarlyBoundUnsafe())
            COMPlusThrow(kSecurityException);

    }
    EX_CATCH
    {
        *pRetValOut = SetupErrorInfo(GET_THROWABLE());
        result = false;
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_SO_INTOLERANT_CODE;

    return result;
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
        SO_TOLERANT; 
    }
    CONTRACTL_END;

    PCODE pManagedTarget;
    OBJECTREF pObject;

    int fpReturnSize = 0;
    if (maskedFlags & enum_NeedsSecurityCheck)
    {
        if (!COMToCLRWorkerBody_SecurityCheck(pCMD, pRealMD, pThread, pRetValOut))
            return;
    }
    if (maskedFlags & enum_NativeR8Retval)
        fpReturnSize = 8;
    if (maskedFlags & enum_NativeR4Retval)
        fpReturnSize = 4;

    maskedFlags &= ~(enum_NeedsSecurityCheck|enum_NativeR4Retval|enum_NativeR8Retval);

    CONSISTENCY_CHECK(maskedFlags != (                      enum_IsWinRTCtor|enum_IsVirtual));
    CONSISTENCY_CHECK(maskedFlags != (enum_IsDelegateInvoke|enum_IsWinRTCtor|enum_IsVirtual));
    CONSISTENCY_CHECK(maskedFlags != (enum_IsDelegateInvoke|enum_IsWinRTCtor               ));
    switch (maskedFlags)
    {
    case enum_IsDelegateInvoke|enum_IsVirtual:
    case enum_IsDelegateInvoke: pObject = COMToCLRGetObjectAndTarget_Delegate(pWrap, &pManagedTarget); break;
    case enum_IsVirtual:        pObject = COMToCLRGetObjectAndTarget_Virtual(pWrap, pRealMD, pCMD, &pManagedTarget); break;
    case 0:                     pObject = COMToCLRGetObjectAndTarget_NonVirtual(pWrap, pRealMD, pCMD, &pManagedTarget); break;
    case enum_IsWinRTCtor:
        if (!COMToCLRGetObjectAndTarget_WinRTCtor(pThread, pRealMD, pCMD, &pManagedTarget, &pObject, pRetValOut))
            return;
        break;
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
        SO_TOLERANT; 
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
        enum_NeedsSecurityCheck |
        enum_IsDelegateInvoke |
        enum_IsWinRTCtor |
        enum_IsVirtual |
        enum_NativeR4Retval | 
        enum_NativeR8Retval);
    DWORD maskedFlags = pCMD->GetFlags() & mask;

    CONSISTENCY_CHECK(maskedFlags != (                      enum_IsWinRTCtor|enum_IsVirtual));
    CONSISTENCY_CHECK(maskedFlags != (enum_IsDelegateInvoke|enum_IsWinRTCtor|enum_IsVirtual));
    CONSISTENCY_CHECK(maskedFlags != (enum_IsDelegateInvoke|enum_IsWinRTCtor               ));
    switch (maskedFlags)
    {
    case enum_IsDelegateInvoke|enum_IsVirtual:
    case enum_IsDelegateInvoke: pObject = COMToCLRGetObjectAndTarget_Delegate(pWrap, &pManagedTarget); break;
    case enum_IsVirtual:        pObject = COMToCLRGetObjectAndTarget_Virtual(pWrap, pRealMD, pCMD, &pManagedTarget); break;
    case 0:                     pObject = COMToCLRGetObjectAndTarget_NonVirtual(pWrap, pRealMD, pCMD, &pManagedTarget); break;
    case enum_IsWinRTCtor:
        if (!COMToCLRGetObjectAndTarget_WinRTCtor(pThread, pRealMD, pCMD, &pManagedTarget, &pObject, pRetValOut))
            return;
        break;
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

void COMToCLRWorkerBody_SOIntolerant(Thread * pThread, ComMethodFrame * pFrame, ComCallWrapper * pWrap, UINT64 * pRetValOut)
{
    STATIC_CONTRACT_THROWS;             // THROWS due to END_SO_TOLERANT_CODE
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_SO_INTOLERANT; 

    BEGIN_SO_TOLERANT_CODE(pThread);

    COMToCLRWorkerBody(pThread, pFrame, pWrap, pRetValOut);

    END_SO_TOLERANT_CODE;
}

#ifdef _TARGET_X86_
// On x86, we do not want the non-AD-transition path to push an extra FS:0 handler just to 
// pop off the ComMethodFrame.  On non-x86, we have a personality routine that does this
// (ReverseComUnwindFrameChainHandler), but on x86 we will latch onto the typical CPFH by
// pushing COMPlusFrameHandlerRevCom as the FS:0 handler instead of COMPlusFrameHandler.  
// COMPlusFrameHandlerRevCom will look at the Frame chain from the current Frame up to
// the ComMethodFrame and, if it finds a ContextTransitionFrame, it will do nothing.  
// Otherwise, it will unwind the Frame chain up to the ComMethodFrame.  So here we latch
// onto the AD transition rethrow as the point at which to unwind the Frame chain up to
// the ComMethodFrame.
#define REVERSE_COM_RETHROW_HOOK(pFrame)    { ComMethodFrame::DoSecondPassHandlerCleanup(pFrame); }
#else
#define REVERSE_COM_RETHROW_HOOK(pFrame)    NULL
#endif // _TARGET_X86_

NOINLINE
void COMToCLRWorkerBodyWithADTransition(
    Thread * pThread,
    ComMethodFrame * pFrame,
    ComCallWrapper * pWrap,
    UINT64 * pRetValOut)
{
    CONTRACTL
    {
        NOTHROW;    // Although CSE can be thrown 
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_TOLERANT; 
    }
    CONTRACTL_END;

    BOOL fEnteredDomain = FALSE;
    BEGIN_SO_INTOLERANT_CODE_NOTHROW(pThread, { *pRetValOut = COR_E_STACKOVERFLOW; return; } );
    EX_TRY
    {
        bool fNeedToTranslateTAEtoADUE = false;
        ADID pTgtDomain = pWrap->GetDomainID();
        ENTER_DOMAIN_ID(pTgtDomain)
        {
            fEnteredDomain = TRUE;
            COMToCLRWorkerBody_SOIntolerant(pThread, pFrame, pWrap, pRetValOut);

            //
            // Below is some logic adapted from Thread::RaiseCrossContextExceptionHelper, which we now
            // bypass because the IL stub is catching the ThreadAbortException instead of a proper domain 
            // transition, where the logic typically resides.  This code applies some policy to transform 
            // the ThreadAbortException into an AppDomainUnloadedException and sets up the HRESULT and 
            // IErrorInfo accordingly.
            //

            // If the IL stub caught a TAE...
            if (COR_E_THREADABORTED == ((HRESULT)*pRetValOut))
            {
                // ...first, make sure it was actually an HRESULT return value...
                ComCallMethodDesc* pCMD = pFrame->GetComCallMethodDesc();
                if (pCMD->IsNativeHResultRetVal()) 
                {
                    // There may be multiple AD transitions on the stack so the current unload boundary may
                    // not be the transition frame that was set up to make our AD switch. Detect that by
                    // comparing the unload boundary's Next with our ComMethodFrame and proceed to translate
                    // the exception to ADUE only if they match. Otherwise the exception should stay as TAE.

                    Frame* pUnloadBoundary = pThread->GetUnloadBoundaryFrame();
                    // ...and we are at an unload boundary with a pending unload...
                    if (    (    pUnloadBoundary != NULL
                             && (pUnloadBoundary->Next() == pFrame
                             &&  pThread->ShouldChangeAbortToUnload(pUnloadBoundary, pUnloadBoundary))
                            )
                        // ... or we don't have an unload boundary, but we're otherwise unloading 
                        //     this domain from another thread (and we aren't the finalizer)...
                        ||  (   (NULL == pUnloadBoundary)
                             && (pThread->GetDomain() == SystemDomain::AppDomainBeingUnloaded())
                             && (pThread != SystemDomain::System()->GetUnloadingThread())
                             && (pThread != FinalizerThread::GetFinalizerThread())
                            )
                       )
                    {
                        // ... we take note and then create an ADUE in the domain we're returning to.
                        fNeedToTranslateTAEtoADUE = true;
                    }
                }
            }
        }
        END_DOMAIN_TRANSITION;

        if (fNeedToTranslateTAEtoADUE)
        {
            EEResourceException ex(kAppDomainUnloadedException, W("Remoting_AppDomainUnloaded_ThreadUnwound"));
            OBJECTREF oEx = CLRException::GetThrowableFromException(&ex);
            *pRetValOut = SetupErrorInfo(oEx, pFrame->GetComCallMethodDesc());
        }
    }
    EX_CATCH
    {
        *pRetValOut = SetupErrorInfo(GET_THROWABLE(), pFrame->GetComCallMethodDesc());
    }
    EX_END_CATCH(
        RethrowCorruptingExceptionsExAndHookRethrow(
            // If it was thrown at us from the IL stub (which will evaluate the CE policy), then we must 
            // rethrow it here.  But we should swallow exceptions generated by our domain transition.
            fEnteredDomain,   
            REVERSE_COM_RETHROW_HOOK(pThread->GetFrame())
            ));

    END_SO_INTOLERANT_CODE;
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
#if defined(_TARGET_X86_)
        MODE_COOPERATIVE; // X86 sets up COOP in stublinker-generated stub
#else
        // This contract is disabled because user code can illegally reenter here through no fault of the
        // CLR (i.e. it's a user code bug), so we shouldn't be popping ASSERT dialogs in those cases.  Note
        // that this reentrancy check is already done by the stublinker-generated stub on x86, so it's OK
        // to leave the MODE_ contract enabled on x86.
        DISABLED(MODE_PREEMPTIVE);
#endif
        SO_TOLERANT; 
        PRECONDITION(CheckPointer(pFrame));
        PRECONDITION(CheckPointer(pThread, NULL_OK));
    }
    CONTRACTL_END;

    UINT64 retVal = 0;

    ComCallMethodDesc* pCMD = pFrame->GetComCallMethodDesc();

#if !defined(_TARGET_X86_)
    //
    // The following code is a transcription of the code that is generated by CreateGenericComCallStub.  The
    // idea is that we needn't really do this work either in static assembly code nor in dynamically 
    // generated code since the benefit/cost ratio is low.  There are some minor differences in the below 
    // code, compared to x86.  First, the reentrancy and loader lock checks are optionally compiled into the
    // stub on x86, depending on whether or not the corresponding MDAs are active at stub-generation time.  
    // We must check each time at runtime here because we're using static code.
    //
    HRESULT hr = S_OK;

    pThread = GetThread();
    if (NULL == pThread)
    {
        pThread = SetupThreadNoThrow();
        if (pThread == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto ErrorExit;
        }
    }

    // Check for an illegal coop->coop transition.  We may fire the Reentrancy MDA as a result.
    if (pThread->PreemptiveGCDisabled())
        HasIllegalReentrancy();

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

#ifdef MDA_SUPPORTED
    // Check for and trigger the LoaderLock MDA
    if (ShouldCheckLoaderLock())
    {
        BOOL IsHeld;
        if (AuxUlibIsDLLSynchronizationHeld(&IsHeld) && IsHeld)
        {
            MDA_TRIGGER_ASSISTANT(LoaderLock, ReportViolation(0));
        }
    }
#endif // MDA_SUPPORTED

    // Initialize the frame's VPTR and GS cookie.
    *((TADDR*)pFrame) = ComMethodFrame::GetMethodFrameVPtr();
    *pFrame->GetGSCookiePtr() = GetProcessGSCookie();
    // Link frame into the chain.
    pFrame->Push(pThread);

#endif // !_TARGET_X86_

    _ASSERTE(pThread);

    // At this point we should be in preemptive GC mode (regardless of if it happened
    // in the stub or in the worker).
    _ASSERTE(pThread->PreemptiveGCDisabled());

    {
#ifndef _TARGET_X86_
        if (pCMD->IsFieldCall())
        {
            retVal = FieldCallWorker(pThread, pFrame);
        }
        else
#endif // !_TARGET_X86_
        {
            IUnknown **pip = (IUnknown **)pFrame->GetPointerToArguments();
            IUnknown *pUnk = (IUnknown *)*pip; 
            _ASSERTE(pUnk != NULL);

            // Obtain the managed 'this' for the call
            ComCallWrapper *pWrap = ComCallWrapper::GetWrapperFromIP(pUnk);
            _ASSERTE(pWrap != NULL);
            if (pWrap->NeedToSwitchDomains(pThread))
            {
                COMToCLRWorkerBodyWithADTransition(pThread, pFrame, pWrap, &retVal);
            }
            else
            {
                // This is the common case that needs to be fast: we are in the right domain and
                // all we have to do is marshal the parameters and deliver the call. 
                COMToCLRWorkerBody(pThread, pFrame, pWrap, &retVal);
            }
        }
    }

#ifndef _TARGET_X86_
    // Note: the EH subsystem will handle reseting the frame chain and setting 
    // the correct GC mode on exception.
    pFrame->Pop(pThread);
    pThread->EnablePreemptiveGC();
#endif

    LOG((LF_STUBS, LL_INFO1000000, "COMToCLRWorker leave\n"));

    // The call was successfull. If the native return type is a floating point
    // value, then we need to set the floating point registers appropriately.
    if (pCMD->IsNativeFloatingPointRetVal()) // single check skips both cases
    {
        if (pCMD->IsNativeR4RetVal())
            setFPReturn(4, retVal);
        else
            setFPReturn(8, retVal);
    }
    return retVal;

#ifndef _TARGET_X86_
ErrorExit:
    if (pThread->PreemptiveGCDisabled())
        pThread->EnablePreemptiveGC();

    // The call failed so we need to report an error to the caller.
    if (pCMD->IsNativeHResultRetVal())
    {
        _ASSERTE(FAILED(hr));
        retVal = hr;
    }
    else if (pCMD->IsNativeBoolRetVal())
        retVal = 0;
    else if (pCMD->IsNativeR4RetVal())
        setFPReturn(4, CLR_NAN_32);
    else if (pCMD->IsNativeR8RetVal())
        setFPReturn(8, CLR_NAN_64);
    else
        _ASSERTE(pCMD->IsNativeVoidRetVal());
    return retVal;
#endif // _TARGET_X86_
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


#ifdef MDA_SUPPORTED
    MDA_TRIGGER_ASSISTANT(GcUnmanagedToManaged, TriggerGC());
#endif

    LOG((LF_STUBS, LL_INFO1000000, "FieldCallWorker enter\n"));
    
    HRESULT hrRetVal = S_OK;

    BEGIN_SO_INTOLERANT_CODE_NOTHROW(pThread, return COR_E_STACKOVERFLOW);
    // BEGIN_ENTRYPOINT_NOTHROW_WITH_THREAD(pThread);
   
    IUnknown** pip = (IUnknown **)pFrame->GetPointerToArguments();
    IUnknown* pUnk = (IUnknown *)*pip; 
    _ASSERTE(pUnk != NULL);

    ComCallWrapper* pWrap =  ComCallWrapper::GetWrapperFromIP(pUnk);
    _ASSERTE(pWrap != NULL);
        
    GCX_ASSERT_COOP();
    OBJECTREF pThrowable = NULL;
    GCPROTECT_BEGIN(pThrowable);
    {
        if (!pWrap->NeedToSwitchDomains(pThread))
        {
            // This is the common case that needs to be fast: we are in the right domain and
            // all we have to do is marshal the parameters and deliver the call. We still have to
            // set up an EX_TRY/EX_CATCH to transform any exceptions that were thrown into 
            // HRESULTs.       
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
                hrRetVal = SetupErrorInfo(pThrowable, pFrame->GetComCallMethodDesc());
                pThrowable = NULL;
            }
        }
        else
        {
            ADID pTgtDomain = pWrap->GetDomainID();
            if (!pTgtDomain.m_dwId)
            {
                hrRetVal = COR_E_APPDOMAINUNLOADED;
            }
            else
            {
                // We need a try/catch around the code to enter the domain since entering
                // an AppDomain can throw an exception. 
                EX_TRY
                {
                    ENTER_DOMAIN_ID(pTgtDomain)
                    {
                        // Set up a new GC protection frame for any exceptions thrown inside the AppDomain. Do
                        // this so we can be sure we don't leak an AppDomain-specific object outside the
                        // lifetime of the AppDomain (which can happen if an AppDomain unload causes us to
                        // unwind out via a ThreadAbortException).
                        OBJECTREF pAppDomainThrowable = NULL;
                        GCPROTECT_BEGIN(pAppDomainThrowable);
                        {
                            // We need a try/catch around the call to the worker since we need
                            // to transform any exceptions into HRESULTs. We want to do this
                            // inside the AppDomain of the CCW.
                            EX_TRY
                            {
                                FieldCallWorkerDebuggerWrapper(pThread, pFrame);
                            }
                            EX_CATCH
                            {
                                pAppDomainThrowable = GET_THROWABLE();
                            }
                            EX_END_CATCH(RethrowTerminalExceptions);

                            if (pAppDomainThrowable != NULL)
                            {
                                // Transform the exception into an HRESULT. This also sets up
                                // an IErrorInfo on the current thread for the exception.
                                hrRetVal = SetupErrorInfo(pAppDomainThrowable, pFrame->GetComCallMethodDesc());
                                pAppDomainThrowable = NULL;
                            }
                        }
                        GCPROTECT_END();
                    }
                    END_DOMAIN_TRANSITION;        
                }
                EX_CATCH
                {
                    // Transform the exception into an HRESULT. This also sets up
                    // an IErrorInfo on the current thread for the exception.
                    pThrowable = GET_THROWABLE();
                }
                EX_END_CATCH(SwallowAllExceptions);

                if (pThrowable != NULL)
                {
                    // Transform the exception into an HRESULT. This also sets up
                    // an IErrorInfo on the current thread for the exception.
                    hrRetVal = SetupErrorInfo(pThrowable, pFrame->GetComCallMethodDesc());
                    pThrowable = NULL;
                }
            }
        }
    }

    GCPROTECT_END();

#ifdef MDA_SUPPORTED
    MDA_TRIGGER_ASSISTANT(GcManagedToUnmanaged, TriggerGC());
#endif

    LOG((LF_STUBS, LL_INFO1000000, "FieldCallWorker leave\n"));

    END_SO_INTOLERANT_CODE;
    //END_ENTRYPOINT_NOTHROW_WITH_THREAD;
    
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
    
    ReverseEnterRuntimeHolder REHolder(TRUE);
    
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

    if (pCMD->IsEarlyBoundUnsafe())
    {
        COMPlusThrow(kSecurityException);
    }

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
        MethodDesc *pMD = ((IsWinRTCtor() || IsWinRTStatic()) ? GetMethodDesc() : GetCallMethodDesc());

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

#ifdef _TARGET_X86_
    // make sure our native stack computation in code:ComCallMethodDesc.InitNativeInfo is right
    _ASSERTE(HasMarshalError() || !pStubMD->IsILStub() || pStubMD->AsDynamicMethodDesc()->GetNativeStackArgSize() == m_StackBytes);
#else // _TARGET_X86_
    if (pStubMD->IsILStub())
    {
        m_StackBytes = pStubMD->AsDynamicMethodDesc()->GetNativeStackArgSize();
        _ASSERTE(m_StackBytes == pStubMD->SizeOfArgStack());
    }
    else
    {
        m_StackBytes = pStubMD->SizeOfArgStack();
    }
#endif // _TARGET_X86_

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

#ifdef _TARGET_X86_
    // Parse the stub signature to figure out how we're going to transform the incoming arguments
    // into stub arguments (i.e. ECX and possibly EDX get enregisterable args, stack gets reversed).

    MetaSig msig(pStubMD);
    ArgIterator argit(&msig);
    
    UINT dwArgStack = argit.SizeOfArgStack();
    if (!FitsInU2(dwArgStack))
        COMPlusThrow(kMarshalDirectiveException, IDS_EE_SIGTOOCOMPLEX);

    NewArrayHolder<UINT16> pwStubStackSlotOffsets;
    UINT16 *pOutputStack = NULL;

    UINT16 wStubStackSlotCount = static_cast<UINT16>(dwArgStack) / STACK_ELEM_SIZE;
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
        wInputStack += STACK_ELEM_SIZE;
    }

    // process the return buffer parameter
    if (argit.HasRetBuffArg())
    {
        numRegistersUsed++;
        wSourceSlotEDX = wInputStack / STACK_ELEM_SIZE;
        wInputStack += STACK_ELEM_SIZE;
    }

    // process ordinary parameters
    for (UINT i = msig.NumFixedArgs(); i > 0; i--)
    {
        TypeHandle thValueType;
        CorElementType type = msig.NextArgNormalized(&thValueType);

        UINT cbSize = MetaSig::GetElemSize(type, thValueType);

        if (ArgIterator::IsArgumentInRegister(&numRegistersUsed, type))
        {
            wSourceSlotEDX = wInputStack / STACK_ELEM_SIZE;
            wInputStack += STACK_ELEM_SIZE;
        }
        else
        {
            // we may need more stack slots for larger parameters
            pOutputStack -= StackElemSize(cbSize) / STACK_ELEM_SIZE;
            for (UINT slot = 0; slot < (StackElemSize(cbSize) / STACK_ELEM_SIZE); slot++)
            {
                pOutputStack[slot] = wInputStack;
                wInputStack += STACK_ELEM_SIZE;
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
#endif // _TARGET_X86_
}
#endif //CROSSGEN_COMPILE

void ComCallMethodDesc::InitMethod(MethodDesc *pMD, MethodDesc *pInterfaceMD, BOOL fRedirectedInterface /* = FALSE */)
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

#ifdef _TARGET_X86_
    m_dwSlotInfo = 0;
    m_pwStubStackSlotOffsets = NULL;
#endif // _TARGET_X86_

    if (fRedirectedInterface)
        m_flags |= enum_IsWinRTRedirected;

    // check whether this is a WinRT ctor/static/event method
    MethodDesc *pCallMD = GetCallMethodDesc();
    MethodTable *pCallMT = pCallMD->GetMethodTable();
    if (pCallMT->IsProjectedFromWinRT() || pCallMT->IsExportedToWinRT())
    {
        m_flags |= enum_IsWinRTCall;

        if (pMD->IsCtor())
        {
            m_flags |= enum_IsWinRTCtor;
        }
        else
        {
            if (pMD->IsStatic())
                m_flags |= enum_IsWinRTStatic;
        }
    }

    if (!SystemDomain::GetCurrentDomain()->IsCompilationDomain())
    {
        // Initialize the native type information size of native stack, native retval flags, etc).
        InitNativeInfo();

        // If this interface method is implemented on a class which lives
        //  in an assembly without UnmanagedCodePermission, then
        //  we mark the ComCallMethodDesc as unsafe for being called early-bound.
        Module* pModule = pMD->GetModule();
        if (!Security::CanCallUnmanagedCode(pModule))
        {
            m_flags |= (enum_NeedsSecurityCheck | enum_IsEarlyBoundUnsafe);
        }
        else if (pMD->RequiresLinktimeCheck())
        {
            // remember that we have to call Security::CheckLinkDemandAgainstAppDomain at invocation time
            m_flags |= enum_NeedsSecurityCheck;
        }
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

#ifdef _TARGET_X86_
    m_dwSlotInfo = 0;
    m_pwStubStackSlotOffsets = NULL;
#endif // _TARGET_X86_

    m_flags = enum_IsFieldCall; // mark the attribute as a field
    m_flags |= isGetter ? enum_IsGetter : 0;

    if (!SystemDomain::GetCurrentDomain()->IsCompilationDomain())
    {
        // Initialize the native type information size of native stack, native retval flags, etc).
        InitNativeInfo();
        
        // If this interface method is implemented on a class which lives
        //  in an assembly without UnmanagedCodePermission, then
        //  we mark the ComCallMethodDesc as unsafe for being called early-bound.
        Module* pModule = pFD->GetModule();
        if (!Security::CanCallUnmanagedCode(pModule))
        {
            m_flags |= enum_IsEarlyBoundUnsafe;
        }
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
#ifdef _TARGET_X86_
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
            
#ifdef _TARGET_X86_
            MetaSig fsig(pFD);
            fsig.NextArg();

            // Look up the best fit mapping info via Assembly & Interface level attributes
            BOOL BestFit = TRUE;
            BOOL ThrowOnUnmappableChar = FALSE;
            ReadBestFitCustomAttribute(fsig.GetModule()->GetMDImport(), pFD->GetEnclosingMethodTable()->GetCl(), &BestFit, &ThrowOnUnmappableChar);

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
#endif // _TARGET_X86_

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

#ifndef _TARGET_X86_
            if (!fPreserveSig)
            {
                // PreserveSig=false methods always return HRESULTs. 
                m_flags |= enum_NativeHResultRetVal;
                goto Done;
            }
#endif

            MetaSig msig(pMD);

#ifndef _TARGET_X86_
            if (msig.IsReturnTypeVoid())
            {
                // The method has a void return type on the native side.
                m_flags |= enum_NativeVoidRetVal;
                goto Done;
            }
#endif

            BOOL WinRTType = pMT->IsProjectedFromWinRT();

            // Look up the best fit mapping info via Assembly & Interface level attributes
            BOOL BestFit = TRUE;
            BOOL ThrowOnUnmappableChar = FALSE;

            // Marshaling is fully described by the parameter type in WinRT. BestFit custom attributes 
            // are not going to affect the marshaling behavior.
            if (!WinRTType)
            {
                ReadBestFitCustomAttribute(pMD, &BestFit, &ThrowOnUnmappableChar);
            }
         
            int numArgs = msig.NumFixedArgs();

            // Collects ParamDef information in an indexed array where element 0 represents 
            // the return type.
            mdParamDef *params = (mdParamDef*)_alloca((numArgs+1) * sizeof(mdParamDef));
            CollateParamTokens(pInternalImport, md, numArgs, params);

#ifdef _TARGET_X86_
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
                                 WinRTType ? MarshalInfo::MARSHAL_SCENARIO_WINRT : MarshalInfo::MARSHAL_SCENARIO_COMINTEROP,
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

                    if (info.GetMarshalType() == MarshalInfo::MARSHAL_TYPE_HIDDENLENGTHARRAY)
                    {
                        // count the hidden length
                        nativeArgSize += info.GetHiddenLengthParamStackSize();
                    }
                }
                
                ++iArg;
            }

            // Check to see if this is the parameter after which we need to read the LCID from.
            if (iArg == iLCIDArg)
                nativeArgSize += StackElemSize(sizeof(LCID));
#endif // _TARGET_X86_


            //
            // Return value
            //

#ifndef _TARGET_X86_
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
#endif // _TARGET_X86_

            {
                MarshalInfo info(msig.GetModule(), msig.GetReturnProps(), msig.GetSigTypeContext(), params[0],
                                    WinRTType ? MarshalInfo::MARSHAL_SCENARIO_WINRT : MarshalInfo::MARSHAL_SCENARIO_COMINTEROP,
                                    (CorNativeLinkType)0, (CorNativeLinkFlags)0,
                                    FALSE, 0, numArgs, BestFit, ThrowOnUnmappableChar, FALSE, pMD, FALSE
#ifdef _DEBUG
                                ,szDebugName, szDebugClassName, 0
#endif
                );

#ifndef _TARGET_X86_
                // Handled above
                _ASSERTE(fPreserveSig);
#else
                if (!fPreserveSig)
                {
                    // PreserveSig=false methods always return HRESULTs. 
                    m_flags |= enum_NativeHResultRetVal;

                    // count the output by-ref argument
                    nativeArgSize += sizeof(void *);

                    if (info.GetMarshalType() == MarshalInfo::MARSHAL_TYPE_HIDDENLENGTHARRAY)
                    {
                        // count the output hidden length
                        nativeArgSize += info.GetHiddenLengthParamStackSize();
                    }

                    goto Done;
                }
#endif // _TARGET_X86_

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

#ifdef _TARGET_X86_
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
        ReadBestFitCustomAttribute(pMT->GetMDImport(), pMT->GetCl(), &BestFit, &ThrowOnUnmappableChar);
    }
    else
    {
        MethodDesc *pMD = pCMD->GetCallMethodDesc();
        _ASSERTE(IsMethodVisibleFromCom(pMD) && "Calls are not permitted on this member since it isn't visible from COM. The only way you can have reached this code path is if your native interface doesn't match the managed interface.");

        MethodTable *pMT = pMD->GetMethodTable();
        if (pMT->IsProjectedFromWinRT() || pMT->IsExportedToWinRT() || pCMD->IsWinRTRedirectedMethod())
        {
            dwStubFlags |= NDIRECTSTUB_FL_WINRT;

            if (pMT->IsDelegate())
                dwStubFlags |= NDIRECTSTUB_FL_WINRTDELEGATE;
            else if (pCMD->IsWinRTCtor())
            {
                dwStubFlags |= NDIRECTSTUB_FL_WINRTCTOR;
            }
            else
            {
                if (pCMD->IsWinRTStatic())
                    dwStubFlags |= NDIRECTSTUB_FL_WINRTSTATIC;
            }
        }
        else
        {
            // Marshaling is fully described by the parameter type in WinRT. BestFit custom attributes 
            // are not going to affect the marshaling behavior.
            ReadBestFitCustomAttribute(pMD, &BestFit, &ThrowOnUnmappableChar);
        }
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
#ifdef _TARGET_X86_
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
#endif // _TARGET_X86_

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

#ifdef _TARGET_X86_
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

    PCCOR_SIGNATURE pSig;
    DWORD           cSig;

    // Get the call signature information
    StubSigDesc sigDesc(pCallMD);

    return NDirect::CreateCLRToNativeILStub(&sigDesc,
                                            (CorNativeLinkType)0,
                                            (CorNativeLinkFlags)0,
                                            (CorPinvokeMap)0,
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

// static
MethodDesc *ComCall::GetCtorForWinRTFactoryMethod(MethodTable *pClsMT, MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pClsMT->IsSealed());
    }
    CONTRACTL_END;
    
    PCCOR_SIGNATURE pSig;
    DWORD cSig;
    pMD->GetSig(&pSig, &cSig);
    SigParser sig(pSig, cSig);
    
    ULONG numArgs;
    CorElementType et;

    IfFailThrow(sig.GetCallingConv(NULL)); // calling convention
    IfFailThrow(sig.GetData(&numArgs));    // number of args
    IfFailThrow(sig.SkipExactlyOne());     // skip return type

    SigBuilder sigBuilder;
    sigBuilder.AppendByte(IMAGE_CEE_CS_CALLCONV_HASTHIS);
    sigBuilder.AppendData(numArgs);

    // ctor returns void
    sigBuilder.AppendElementType(ELEMENT_TYPE_VOID);

    sig.GetSignature(&pSig, &cSig);

    // parameter types are identical for sealed classes
    sigBuilder.AppendBlob((const PVOID)pSig, cSig);

    pSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature(&cSig);

    MethodDesc *pCtorMD = MemberLoader::FindMethod(pClsMT, COR_CTOR_METHOD_NAME, pSig, cSig, pMD->GetModule());

    if (pCtorMD == NULL)
    {
        SString ctorMethodName(SString::Utf8, COR_CTOR_METHOD_NAME);
        COMPlusThrowNonLocalized(kMissingMethodException, ctorMethodName.GetUnicode());
    }
    return pCtorMD;
}

// static
MethodDesc *ComCall::GetStaticForWinRTFactoryMethod(MethodTable *pClsMT, MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    PCCOR_SIGNATURE pSig;
    DWORD cSig;
    pMD->GetSig(&pSig, &cSig);
    SigParser sig(pSig, cSig);
    
    IfFailThrow(sig.GetCallingConv(NULL)); // calling convention
    
    SigBuilder sigBuilder;
    sigBuilder.AppendByte(IMAGE_CEE_CS_CALLCONV_DEFAULT);

    // number of parameters, return type, and parameter types are identical
    sig.GetSignature(&pSig, &cSig);
    sigBuilder.AppendBlob((const PVOID)pSig, cSig);

    pSig = (PCCOR_SIGNATURE)sigBuilder.GetSignature(&cSig);

    MethodDesc *pStaticMD = MemberLoader::FindMethod(pClsMT, pMD->GetName(), pSig, cSig, pMD->GetModule());

    if (pStaticMD == NULL)
    {
        SString staticMethodName(SString::Utf8, pMD->GetName());
        COMPlusThrowNonLocalized(kMissingMethodException, staticMethodName.GetUnicode());
    }
    return pStaticMD;
}

#endif // DACCESS_COMPILE
