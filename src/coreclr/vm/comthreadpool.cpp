// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*============================================================
**
** Header: COMThreadPool.cpp
**
** Purpose: Native methods on System.ThreadPool
**          and its inner classes
**
**
===========================================================*/

/********************************************************************************************************************/
#include "common.h"
#include "comdelegate.h"
#include "comthreadpool.h"
#include "threadpoolrequest.h"
#include "win32threadpool.h"
#include "class.h"
#include "object.h"
#include "field.h"
#include "excep.h"
#include "eeconfig.h"
#include "corhost.h"
#include "nativeoverlapped.h"
#include "comsynchronizable.h"
#include "callhelpers.h"
#include "appdomain.inl"
/*****************************************************************************************************/
#ifdef _DEBUG
void LogCall(MethodDesc* pMD, LPCUTF8 api)
{
    LIMITED_METHOD_CONTRACT;

    LPCUTF8 cls  = pMD->GetMethodTable()->GetDebugClassName();
    LPCUTF8 name = pMD->GetName();

    LOG((LF_THREADPOOL,LL_INFO1000,"%s: ", api));
    LOG((LF_THREADPOOL, LL_INFO1000,
         " calling %s.%s\n", cls, name));
}
#else
#define LogCall(pMd,api)
#endif

VOID
AcquireDelegateInfo(DelegateInfo *pDelInfo)
{
    LIMITED_METHOD_CONTRACT;
}

VOID
ReleaseDelegateInfo(DelegateInfo *pDelInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    // The release methods of holders can be called with preemptive GC enabled. Ensure we're in cooperative mode
    // before calling pDelInfo->Release(), since that requires coop mode.
    GCX_COOP();

    pDelInfo->Release();
    ThreadpoolMgr::RecycleMemory( pDelInfo, ThreadpoolMgr::MEMTYPE_DelegateInfo );
}

//typedef Holder<DelegateInfo *, AcquireDelegateInfo, ReleaseDelegateInfo> DelegateInfoHolder;

typedef Wrapper<DelegateInfo *, AcquireDelegateInfo, ReleaseDelegateInfo> DelegateInfoHolder;

/*****************************************************************************************************/
// Caller has to GC protect Objectrefs being passed in
DelegateInfo *DelegateInfo::MakeDelegateInfo(OBJECTREF *state,
                                             OBJECTREF *waitEvent,
                                             OBJECTREF *registeredWaitHandle)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        if (state != NULL || waitEvent != NULL || registeredWaitHandle != NULL)
        {
            MODE_COOPERATIVE;
        }
        else
        {
            MODE_ANY;
        }
        PRECONDITION(state == NULL || IsProtectedByGCFrame(state));
        PRECONDITION(waitEvent == NULL || IsProtectedByGCFrame(waitEvent));
        PRECONDITION(registeredWaitHandle == NULL || IsProtectedByGCFrame(registeredWaitHandle));
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    DelegateInfoHolder delegateInfo = (DelegateInfo*) ThreadpoolMgr::GetRecycledMemory(ThreadpoolMgr::MEMTYPE_DelegateInfo);

    AppDomain* pAppDomain = ::GetAppDomain();

    if (state != NULL)
        delegateInfo->m_stateHandle = pAppDomain->CreateHandle(*state);
    else
        delegateInfo->m_stateHandle = NULL;

    if (waitEvent != NULL)
        delegateInfo->m_eventHandle = pAppDomain->CreateHandle(*waitEvent);
    else
        delegateInfo->m_eventHandle = NULL;

    if (registeredWaitHandle != NULL)
        delegateInfo->m_registeredWaitHandle = pAppDomain->CreateHandle(*registeredWaitHandle);
    else
        delegateInfo->m_registeredWaitHandle = NULL;

    delegateInfo.SuppressRelease();

    return delegateInfo;
}

/*****************************************************************************************************/
// Enumerates some runtime config variables that are used by CoreLib for initialization. The config variable index should start
// at 0 to begin enumeration. If a config variable at or after the specified config variable index is configured, returns the
// next config variable index to pass in on the next call to continue enumeration.
FCIMPL4(INT32, ThreadPoolNative::GetNextConfigUInt32Value,
    INT32 configVariableIndex,
    UINT32 *configValueRef,
    BOOL *isBooleanRef,
    LPCWSTR *appContextConfigNameRef)
{
    FCALL_CONTRACT;
    _ASSERTE(configVariableIndex >= 0);
    _ASSERTE(configValueRef != NULL);
    _ASSERTE(isBooleanRef != NULL);
    _ASSERTE(appContextConfigNameRef != NULL);

    if (!ThreadpoolMgr::UsePortableThreadPool())
    {
        *configValueRef = 0;
        *isBooleanRef = false;
        *appContextConfigNameRef = NULL;
        return -1;
    }

    auto TryGetConfig =
        [=](const CLRConfig::ConfigDWORDInfo &configInfo, bool isBoolean, const WCHAR *appContextConfigName) -> bool
    {
        bool wasNotConfigured = true;
        *configValueRef = CLRConfig::GetConfigValue(configInfo, true /* acceptExplicitDefaultFromRegutil */, &wasNotConfigured);
        if (wasNotConfigured)
        {
            return false;
        }

        *isBooleanRef = isBoolean;
        *appContextConfigNameRef = appContextConfigName;
        return true;
    };

    switch (configVariableIndex)
    {
        case 0:
            // Special case for UsePortableThreadPool, which doesn't go into the AppContext
            *configValueRef = 1;
            *isBooleanRef = true;
            *appContextConfigNameRef = NULL;
            return 1;

        case 1: if (TryGetConfig(CLRConfig::INTERNAL_ThreadPool_ForceMinWorkerThreads, false, W("System.Threading.ThreadPool.MinThreads"))) { return 2; } FALLTHROUGH;
        case 2: if (TryGetConfig(CLRConfig::INTERNAL_ThreadPool_ForceMaxWorkerThreads, false, W("System.Threading.ThreadPool.MaxThreads"))) { return 3; } FALLTHROUGH;
        case 3: if (TryGetConfig(CLRConfig::INTERNAL_ThreadPool_DisableStarvationDetection, true, W("System.Threading.ThreadPool.DisableStarvationDetection"))) { return 4; } FALLTHROUGH;
        case 4: if (TryGetConfig(CLRConfig::INTERNAL_ThreadPool_DebugBreakOnWorkerStarvation, true, W("System.Threading.ThreadPool.DebugBreakOnWorkerStarvation"))) { return 5; } FALLTHROUGH;
        case 5: if (TryGetConfig(CLRConfig::INTERNAL_ThreadPool_EnableWorkerTracking, true, W("System.Threading.ThreadPool.EnableWorkerTracking"))) { return 6; } FALLTHROUGH;
        case 6: if (TryGetConfig(CLRConfig::INTERNAL_ThreadPool_UnfairSemaphoreSpinLimit, false, W("System.Threading.ThreadPool.UnfairSemaphoreSpinLimit"))) { return 7; } FALLTHROUGH;

        case 7: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_Disable, true, W("System.Threading.ThreadPool.HillClimbing.Disable"))) { return 8; } FALLTHROUGH;
        case 8: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_WavePeriod, false, W("System.Threading.ThreadPool.HillClimbing.WavePeriod"))) { return 9; } FALLTHROUGH;
        case 9: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_TargetSignalToNoiseRatio, false, W("System.Threading.ThreadPool.HillClimbing.TargetSignalToNoiseRatio"))) { return 10; } FALLTHROUGH;
        case 10: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_ErrorSmoothingFactor, false, W("System.Threading.ThreadPool.HillClimbing.ErrorSmoothingFactor"))) { return 11; } FALLTHROUGH;
        case 11: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_WaveMagnitudeMultiplier, false, W("System.Threading.ThreadPool.HillClimbing.WaveMagnitudeMultiplier"))) { return 12; } FALLTHROUGH;
        case 12: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_MaxWaveMagnitude, false, W("System.Threading.ThreadPool.HillClimbing.MaxWaveMagnitude"))) { return 13; } FALLTHROUGH;
        case 13: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_WaveHistorySize, false, W("System.Threading.ThreadPool.HillClimbing.WaveHistorySize"))) { return 14; } FALLTHROUGH;
        case 14: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_Bias, false, W("System.Threading.ThreadPool.HillClimbing.Bias"))) { return 15; } FALLTHROUGH;
        case 15: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_MaxChangePerSecond, false, W("System.Threading.ThreadPool.HillClimbing.MaxChangePerSecond"))) { return 16; } FALLTHROUGH;
        case 16: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_MaxChangePerSample, false, W("System.Threading.ThreadPool.HillClimbing.MaxChangePerSample"))) { return 17; } FALLTHROUGH;
        case 17: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_MaxSampleErrorPercent, false, W("System.Threading.ThreadPool.HillClimbing.MaxSampleErrorPercent"))) { return 18; } FALLTHROUGH;
        case 18: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_SampleIntervalLow, false, W("System.Threading.ThreadPool.HillClimbing.SampleIntervalLow"))) { return 19; } FALLTHROUGH;
        case 19: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_SampleIntervalHigh, false, W("System.Threading.ThreadPool.HillClimbing.SampleIntervalHigh"))) { return 20; } FALLTHROUGH;
        case 20: if (TryGetConfig(CLRConfig::INTERNAL_HillClimbing_GainExponent, false, W("System.Threading.ThreadPool.HillClimbing.GainExponent"))) { return 21; } FALLTHROUGH;

        default:
            *configValueRef = 0;
            *isBooleanRef = false;
            *appContextConfigNameRef = NULL;
            return -1;
    }
}
FCIMPLEND

/*****************************************************************************************************/
FCIMPL1(FC_BOOL_RET, ThreadPoolNative::CorCanSetMinIOCompletionThreads, DWORD ioCompletionThreads)
{
    FCALL_CONTRACT;
    _ASSERTE_ALL_BUILDS(__FILE__, ThreadpoolMgr::UsePortableThreadPool());

    BOOL result = ThreadpoolMgr::CanSetMinIOCompletionThreads(ioCompletionThreads);
    FC_RETURN_BOOL(result);
}
FCIMPLEND

/*****************************************************************************************************/
FCIMPL1(FC_BOOL_RET, ThreadPoolNative::CorCanSetMaxIOCompletionThreads, DWORD ioCompletionThreads)
{
    FCALL_CONTRACT;
    _ASSERTE_ALL_BUILDS(__FILE__, ThreadpoolMgr::UsePortableThreadPool());

    BOOL result = ThreadpoolMgr::CanSetMaxIOCompletionThreads(ioCompletionThreads);
    FC_RETURN_BOOL(result);
}
FCIMPLEND

/*****************************************************************************************************/
FCIMPL2(FC_BOOL_RET, ThreadPoolNative::CorSetMaxThreads,DWORD workerThreads, DWORD completionPortThreads)
{
    FCALL_CONTRACT;

    BOOL bRet = FALSE;
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    bRet = ThreadpoolMgr::SetMaxThreads(workerThreads,completionPortThreads);
    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(bRet);
}
FCIMPLEND

/*****************************************************************************************************/
FCIMPL2(VOID, ThreadPoolNative::CorGetMaxThreads,DWORD* workerThreads, DWORD* completionPortThreads)
{
    FCALL_CONTRACT;

    ThreadpoolMgr::GetMaxThreads(workerThreads,completionPortThreads);
    return;
}
FCIMPLEND

/*****************************************************************************************************/
FCIMPL2(FC_BOOL_RET, ThreadPoolNative::CorSetMinThreads,DWORD workerThreads, DWORD completionPortThreads)
{
    FCALL_CONTRACT;

    BOOL bRet = FALSE;
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    bRet = ThreadpoolMgr::SetMinThreads(workerThreads,completionPortThreads);
    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(bRet);
}
FCIMPLEND

/*****************************************************************************************************/
FCIMPL2(VOID, ThreadPoolNative::CorGetMinThreads,DWORD* workerThreads, DWORD* completionPortThreads)
{
    FCALL_CONTRACT;

    ThreadpoolMgr::GetMinThreads(workerThreads,completionPortThreads);
    return;
}
FCIMPLEND

/*****************************************************************************************************/
FCIMPL2(VOID, ThreadPoolNative::CorGetAvailableThreads,DWORD* workerThreads, DWORD* completionPortThreads)
{
    FCALL_CONTRACT;

    ThreadpoolMgr::GetAvailableThreads(workerThreads,completionPortThreads);
    return;
}
FCIMPLEND

/*****************************************************************************************************/
FCIMPL0(INT32, ThreadPoolNative::GetThreadCount)
{
    FCALL_CONTRACT;
    return ThreadpoolMgr::GetThreadCount();
}
FCIMPLEND

/*****************************************************************************************************/
INT64 QCALLTYPE ThreadPoolNative::GetCompletedWorkItemCount()
{
    QCALL_CONTRACT;

    INT64 result = 0;

    BEGIN_QCALL;

    result = (INT64)Thread::GetTotalThreadPoolCompletionCount();

    END_QCALL;
    return result;
}

/*****************************************************************************************************/
FCIMPL0(INT64, ThreadPoolNative::GetPendingUnmanagedWorkItemCount)
{
    FCALL_CONTRACT;
    _ASSERTE_ALL_BUILDS(__FILE__, !ThreadpoolMgr::UsePortableThreadPool());

    return PerAppDomainTPCountList::GetUnmanagedTPCount()->GetNumRequests();
}
FCIMPLEND

/*****************************************************************************************************/

FCIMPL0(VOID, ThreadPoolNative::NotifyRequestProgress)
{
    FCALL_CONTRACT;
    _ASSERTE_ALL_BUILDS(__FILE__, !ThreadpoolMgr::UsePortableThreadPool());
    _ASSERTE(ThreadpoolMgr::IsInitialized()); // can't be here without requesting a thread first

    ThreadpoolMgr::NotifyWorkItemCompleted();

    if (ThreadpoolMgr::ShouldAdjustMaxWorkersActive())
    {
        DangerousNonHostedSpinLockTryHolder tal(&ThreadpoolMgr::ThreadAdjustmentLock);
        if (tal.Acquired())
        {
            HELPER_METHOD_FRAME_BEGIN_0();
            ThreadpoolMgr::AdjustMaxWorkersActive();
            HELPER_METHOD_FRAME_END();
        }
        else
        {
            // the lock is held by someone else, so they will take care of this for us.
        }
    }
}
FCIMPLEND

FCIMPL1(VOID, ThreadPoolNative::ReportThreadStatus, CLR_BOOL isWorking)
{
    FCALL_CONTRACT;
    _ASSERTE_ALL_BUILDS(__FILE__, !ThreadpoolMgr::UsePortableThreadPool());

    ThreadpoolMgr::ReportThreadStatus(isWorking);
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, ThreadPoolNative::NotifyRequestComplete)
{
    FCALL_CONTRACT;
    _ASSERTE_ALL_BUILDS(__FILE__, !ThreadpoolMgr::UsePortableThreadPool());
    _ASSERTE(ThreadpoolMgr::IsInitialized()); // can't be here without requesting a thread first

    ThreadpoolMgr::NotifyWorkItemCompleted();

    //
    // Now we need to possibly do one or both of:  reset the thread's state, and/or perform a
    // "worker thread adjustment" (i.e., invoke Hill Climbing).  We try to avoid these at all costs,
    // because they require an expensive helper method frame.  So we first try a minimal thread reset,
    // then check if it covered everything that was needed, and we ask ThreadpoolMgr whether
    // we need a thread adjustment, before setting up the frame.
    //
    Thread *pThread = GetThread();
    _ASSERTE (pThread);

    INT32 priority = pThread->ResetManagedThreadObjectInCoopMode(ThreadNative::PRIORITY_NORMAL);

    bool needReset =
        priority != ThreadNative::PRIORITY_NORMAL ||
        !pThread->IsBackground();

    bool shouldAdjustWorkers = ThreadpoolMgr::ShouldAdjustMaxWorkersActive();

    //
    // If it's time for a thread adjustment, try to get the lock.  This is just a "try," it won't block,
    // so it's ok to do this in cooperative mode.  If we can't get the lock, then some other thread is
    // already doing the thread adjustment, so we needn't bother.
    //
    DangerousNonHostedSpinLockTryHolder tal(&ThreadpoolMgr::ThreadAdjustmentLock, shouldAdjustWorkers);
    if (!tal.Acquired())
        shouldAdjustWorkers = false;

    if (needReset || shouldAdjustWorkers)
    {
        HELPER_METHOD_FRAME_BEGIN_RET_0();

        if (shouldAdjustWorkers)
        {
            ThreadpoolMgr::AdjustMaxWorkersActive();
            tal.Release();
        }

        if (needReset)
            pThread->InternalReset(TRUE, TRUE, FALSE);

        HELPER_METHOD_FRAME_END();
    }

    //
    // Finally, ask ThreadpoolMgr whether it's ok to keep running work on this thread.  Maybe Hill Climbing
    // wants this thread back.
    //
    BOOL result = ThreadpoolMgr::ShouldWorkerKeepRunning() ? TRUE : FALSE;
    FC_RETURN_BOOL(result);
}
FCIMPLEND


/*****************************************************************************************************/

FCIMPL0(FC_BOOL_RET, ThreadPoolNative::GetEnableWorkerTracking)
{
    FCALL_CONTRACT;
    _ASSERTE_ALL_BUILDS(__FILE__, !ThreadpoolMgr::UsePortableThreadPool());

    BOOL result = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ThreadPool_EnableWorkerTracking) ? TRUE : FALSE;
    FC_RETURN_BOOL(result);
}
FCIMPLEND

/*****************************************************************************************************/

struct RegisterWaitForSingleObjectCallback_Args
{
    DelegateInfo *delegateInfo;
    BOOLEAN TimerOrWaitFired;
};

static VOID
RegisterWaitForSingleObjectCallback_Worker(LPVOID ptr)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF orState = NULL;

    GCPROTECT_BEGIN( orState );

    RegisterWaitForSingleObjectCallback_Args *args = (RegisterWaitForSingleObjectCallback_Args *) ptr;
    orState = ObjectFromHandle(((DelegateInfo*) args->delegateInfo)->m_stateHandle);

#ifdef _DEBUG
    MethodDesc *pMeth = CoreLibBinder::GetMethod(METHOD__TPWAITORTIMER_HELPER__PERFORM_WAITORTIMER_CALLBACK);
    LogCall(pMeth,"RWSOCallback");
#endif

    // Caution: the args are not protected, we have to garantee there's no GC from here till
    // the managed call happens.
    PREPARE_NONVIRTUAL_CALLSITE(METHOD__TPWAITORTIMER_HELPER__PERFORM_WAITORTIMER_CALLBACK);
    DECLARE_ARGHOLDER_ARRAY(arg, 2);
    arg[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(orState);
    arg[ARGNUM_1]  = DWORD_TO_ARGHOLDER(args->TimerOrWaitFired);

    // Call the method...
    CALL_MANAGED_METHOD_NORET(arg);

    GCPROTECT_END();
}

VOID NTAPI RegisterWaitForSingleObjectCallback(PVOID delegateInfo, BOOLEAN TimerOrWaitFired)
{
    Thread* pThread = GetThread();
    if (pThread == NULL)
    {
        ClrFlsSetThreadType(ThreadType_Threadpool_Worker);
        pThread = SetupThreadNoThrow();
        if (pThread == NULL) {
            return;
        }
    }

    CONTRACTL
    {
        MODE_PREEMPTIVE;    // Worker thread will be in preempt mode. We switch to coop below.
        THROWS;
        GC_TRIGGERS;

        PRECONDITION(CheckPointer(delegateInfo));
    }
    CONTRACTL_END;

    GCX_COOP();

    RegisterWaitForSingleObjectCallback_Args args = { ((DelegateInfo*) delegateInfo), TimerOrWaitFired };

    ManagedThreadBase::ThreadPool(RegisterWaitForSingleObjectCallback_Worker, &args);
}

FCIMPL5(LPVOID, ThreadPoolNative::CorRegisterWaitForSingleObject,
                                        Object* waitObjectUNSAFE,
                                        Object* stateUNSAFE,
                                        UINT32 timeout,
                                        CLR_BOOL executeOnlyOnce,
                                        Object* registeredWaitObjectUNSAFE)
{
    FCALL_CONTRACT;
    _ASSERTE_ALL_BUILDS(__FILE__, !ThreadpoolMgr::UsePortableThreadPool());

    HANDLE handle = 0;
    struct _gc
    {
        WAITHANDLEREF waitObject;
        OBJECTREF state;
        OBJECTREF registeredWaitObject;
    } gc;
    gc.waitObject = (WAITHANDLEREF) ObjectToOBJECTREF(waitObjectUNSAFE);
    gc.state = (OBJECTREF) stateUNSAFE;
    gc.registeredWaitObject = (OBJECTREF) registeredWaitObjectUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    if(gc.waitObject == NULL)
        COMPlusThrow(kArgumentNullException);

    _ASSERTE(gc.registeredWaitObject != NULL);

    ULONG flag = executeOnlyOnce ? WAIT_SINGLE_EXECUTION | WAIT_FREE_CONTEXT : WAIT_FREE_CONTEXT;

    HANDLE hWaitHandle = gc.waitObject->GetWaitHandle();
    _ASSERTE(hWaitHandle);

    Thread* pCurThread = GetThread();
    _ASSERTE( pCurThread);

    DelegateInfoHolder delegateInfo = DelegateInfo::MakeDelegateInfo(
                                                                &gc.state,
                                                                (OBJECTREF *)&gc.waitObject,
                                                                &gc.registeredWaitObject);

    if (!(ThreadpoolMgr::RegisterWaitForSingleObject(&handle,
                                          hWaitHandle,
                                          RegisterWaitForSingleObjectCallback,
                                          (PVOID) delegateInfo,
                                          (ULONG) timeout,
                                          flag)))

    {
        _ASSERTE(GetLastError() != ERROR_CALL_NOT_IMPLEMENTED);

        COMPlusThrowWin32();
    }

    delegateInfo.SuppressRelease();
    HELPER_METHOD_FRAME_END();
    return (LPVOID) handle;
}
FCIMPLEND

#ifdef TARGET_WINDOWS // the IO completion thread pool is currently only available on Windows
FCIMPL1(void, ThreadPoolNative::CorQueueWaitCompletion, Object* completeWaitWorkItemObjectUNSAFE)
{
    FCALL_CONTRACT;
    _ASSERTE_ALL_BUILDS(__FILE__, ThreadpoolMgr::UsePortableThreadPool());

    OBJECTREF completeWaitWorkItemObject = ObjectToOBJECTREF(completeWaitWorkItemObjectUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(completeWaitWorkItemObject);

    _ASSERTE(completeWaitWorkItemObject != NULL);

    OBJECTHANDLE completeWaitWorkItemObjectHandle = GetAppDomain()->CreateHandle(completeWaitWorkItemObject);
    ThreadpoolMgr::PostQueuedCompletionStatus(
        (LPOVERLAPPED)completeWaitWorkItemObjectHandle,
        ThreadpoolMgr::ManagedWaitIOCompletionCallback);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
#endif // TARGET_WINDOWS

VOID QueueUserWorkItemManagedCallback(PVOID pArg)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(NULL != pArg);

    bool* wasNotRecalled = (bool*)pArg;

    MethodDescCallSite dispatch(METHOD__TP_WAIT_CALLBACK__PERFORM_WAIT_CALLBACK);
    *wasNotRecalled = dispatch.Call_RetBool(NULL);
}


BOOL QCALLTYPE ThreadPoolNative::RequestWorkerThread()
{
    QCALL_CONTRACT;

    BOOL res = FALSE;

    BEGIN_QCALL;

    _ASSERTE_ALL_BUILDS(__FILE__, !ThreadpoolMgr::UsePortableThreadPool());

    ThreadpoolMgr::EnsureInitialized();
    ThreadpoolMgr::SetAppDomainRequestsActive();

    res = ThreadpoolMgr::QueueUserWorkItem(NULL,
                                           NULL,
                                           0,
                                           FALSE);
    if (!res)
    {
        if (GetLastError() == ERROR_CALL_NOT_IMPLEMENTED)
            COMPlusThrow(kNotSupportedException);
        else
            COMPlusThrowWin32();
    }

    END_QCALL;
    return res;
}

BOOL QCALLTYPE ThreadPoolNative::PerformGateActivities(INT32 cpuUtilization)
{
    QCALL_CONTRACT;

    bool needGateThread = false;

    BEGIN_QCALL;

    _ASSERTE_ALL_BUILDS(__FILE__, ThreadpoolMgr::UsePortableThreadPool());

    ThreadpoolMgr::PerformGateActivities(cpuUtilization);
    needGateThread = ThreadpoolMgr::NeedGateThreadForIOCompletions();

    END_QCALL;

    return needGateThread;
}

/********************************************************************************************************************/

FCIMPL2(FC_BOOL_RET, ThreadPoolNative::CorUnregisterWait, LPVOID WaitHandle, Object* objectToNotify)
{
    FCALL_CONTRACT;
    _ASSERTE_ALL_BUILDS(__FILE__, !ThreadpoolMgr::UsePortableThreadPool());

    BOOL retVal = false;
    SAFEHANDLEREF refSH = (SAFEHANDLEREF) ObjectToOBJECTREF(objectToNotify);
    HELPER_METHOD_FRAME_BEGIN_RET_1(refSH);

    HANDLE hWait = (HANDLE) WaitHandle;
    HANDLE hObjectToNotify = NULL;

    ThreadpoolMgr::WaitInfo *pWaitInfo = (ThreadpoolMgr::WaitInfo *)hWait;
    _ASSERTE(pWaitInfo != NULL);

    ThreadpoolMgr::WaitInfoHolder   wiHolder(NULL);

    if (refSH != NULL)
    {
        // Create a GCHandle in the WaitInfo, so that it can hold on to the safe handle
        pWaitInfo->ExternalEventSafeHandle = GetAppDomain()->CreateHandle(NULL);

        // Holder will now release objecthandle in face of exceptions
        wiHolder.Assign(pWaitInfo);

        // Store SafeHandle in object handle. Holder will now release both safehandle and objecthandle
        // in case of exceptions
        StoreObjectInHandle(pWaitInfo->ExternalEventSafeHandle, refSH);

        // Acquire safe handle to examine its handle, then release.
        SafeHandleHolder shHolder(&refSH);

        if (refSH->GetHandle() == INVALID_HANDLE_VALUE)
        {
            hObjectToNotify = INVALID_HANDLE_VALUE;
            // We do not need the ObjectHandle, refcount on the safehandle etc
            wiHolder.Release();
            _ASSERTE(pWaitInfo->ExternalEventSafeHandle == NULL);
        }
    }

    _ASSERTE(hObjectToNotify == NULL || hObjectToNotify == INVALID_HANDLE_VALUE);

    // When hObjectToNotify is NULL ExternalEventSafeHandle contains event to notify (if it is non NULL).
    // When hObjectToNotify is INVALID_HANDLE_VALUE, UnregisterWaitEx blocks until dispose is complete.
    retVal = ThreadpoolMgr::UnregisterWaitEx(hWait, hObjectToNotify);

    if (retVal)
        wiHolder.SuppressRelease();

    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(retVal);
}
FCIMPLEND

/********************************************************************************************************************/
FCIMPL1(void, ThreadPoolNative::CorWaitHandleCleanupNative, LPVOID WaitHandle)
{
    FCALL_CONTRACT;
    _ASSERTE_ALL_BUILDS(__FILE__, !ThreadpoolMgr::UsePortableThreadPool());

    HELPER_METHOD_FRAME_BEGIN_0();

    HANDLE hWait = (HANDLE)WaitHandle;
    ThreadpoolMgr::WaitHandleCleanup(hWait);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

/********************************************************************************************************************/

void QCALLTYPE ThreadPoolNative::ExecuteUnmanagedThreadPoolWorkItem(LPTHREAD_START_ROUTINE callback, LPVOID state)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    _ASSERTE_ALL_BUILDS(__FILE__, ThreadpoolMgr::UsePortableThreadPool());
    callback(state);

    END_QCALL;
}

/********************************************************************************************************************/

struct BindIoCompletion_Args
{
    DWORD ErrorCode;
    DWORD numBytesTransferred;
    LPOVERLAPPED lpOverlapped;
};

VOID BindIoCompletionCallBack_Worker(LPVOID args)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;

    DWORD        ErrorCode = ((BindIoCompletion_Args *)args)->ErrorCode;
    DWORD        numBytesTransferred = ((BindIoCompletion_Args *)args)->numBytesTransferred;
    LPOVERLAPPED lpOverlapped = ((BindIoCompletion_Args *)args)->lpOverlapped;

    OVERLAPPEDDATAREF overlapped = ObjectToOVERLAPPEDDATAREF(OverlappedDataObject::GetOverlapped(lpOverlapped));

    GCPROTECT_BEGIN(overlapped);
    // we set processed to TRUE, now it's our responsibility to guarantee proper cleanup

#ifdef _DEBUG
    MethodDesc *pMeth = CoreLibBinder::GetMethod(METHOD__IOCB_HELPER__PERFORM_IOCOMPLETION_CALLBACK);
    LogCall(pMeth,"IOCallback");
#endif

    if (overlapped->m_callback != NULL)
    {
        // Caution: the args are not protected, we have to garantee there's no GC from here till
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__IOCB_HELPER__PERFORM_IOCOMPLETION_CALLBACK);
        DECLARE_ARGHOLDER_ARRAY(arg, 3);
        arg[ARGNUM_0]  = DWORD_TO_ARGHOLDER(ErrorCode);
        arg[ARGNUM_1]  = DWORD_TO_ARGHOLDER(numBytesTransferred);
        arg[ARGNUM_2]  = PTR_TO_ARGHOLDER(lpOverlapped);

        // Call the method...
        CALL_MANAGED_METHOD_NORET(arg);
    }
    else
    {
        // no user delegate to callback
        _ASSERTE((overlapped->m_callback == NULL) || !"This is benign, but should be optimized");
    }
    GCPROTECT_END();
}

void __stdcall BindIoCompletionCallbackStubEx(DWORD ErrorCode,
                                              DWORD numBytesTransferred,
                                              LPOVERLAPPED lpOverlapped,
                                              BOOL setStack)
{
    Thread* pThread = GetThread();
    if (pThread == NULL)
    {
        // TODO: how do we notify user of OOM here?
        ClrFlsSetThreadType(ThreadType_Threadpool_Worker);
        pThread = SetupThreadNoThrow();
        if (pThread == NULL) {
            return;
        }
    }

    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    LOG((LF_INTEROP, LL_INFO10000, "In IO_CallBackStub thread 0x%x retCode 0x%x, overlap 0x%x\n",  pThread, ErrorCode, lpOverlapped));

    GCX_COOP();

    BindIoCompletion_Args args = {ErrorCode, numBytesTransferred, lpOverlapped};
    ManagedThreadBase::ThreadPool(BindIoCompletionCallBack_Worker, &args);

    LOG((LF_INTEROP, LL_INFO10000, "Leaving IO_CallBackStub thread 0x%x retCode 0x%x, overlap 0x%x\n",  pThread, ErrorCode, lpOverlapped));
}

void WINAPI BindIoCompletionCallbackStub(DWORD ErrorCode,
                                            DWORD numBytesTransferred,
                                            LPOVERLAPPED lpOverlapped)
{
    WRAPPER_NO_CONTRACT;
    BindIoCompletionCallbackStubEx(ErrorCode, numBytesTransferred, lpOverlapped, TRUE);
}

FCIMPL1(FC_BOOL_RET, ThreadPoolNative::CorBindIoCompletionCallback, HANDLE fileHandle)
{
    FCALL_CONTRACT;

    BOOL retVal = FALSE;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    HANDLE hFile = (HANDLE) fileHandle;
    DWORD errCode = 0;

    retVal = ThreadpoolMgr::BindIoCompletionCallback(hFile,
                                           BindIoCompletionCallbackStub,
                                           0,     // reserved, must be 0
                                           OUT errCode);
    if (!retVal)
    {
        if (errCode == ERROR_CALL_NOT_IMPLEMENTED)
            COMPlusThrow(kPlatformNotSupportedException);
        else
        {
            SetLastError(errCode);
            COMPlusThrowWin32();
        }
    }

    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(retVal);
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, ThreadPoolNative::CorPostQueuedCompletionStatus, LPOVERLAPPED lpOverlapped)
{
    FCALL_CONTRACT;

    OVERLAPPEDDATAREF   overlapped = ObjectToOVERLAPPEDDATAREF(OverlappedDataObject::GetOverlapped(lpOverlapped));

    BOOL res = FALSE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(overlapped);

    // OS doesn't signal handle, so do it here
    lpOverlapped->Internal = 0;

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadPoolIOEnqueue))
        FireEtwThreadPoolIOEnqueue(lpOverlapped, OBJECTREFToObject(overlapped), false, GetClrInstanceId());

    res = ThreadpoolMgr::PostQueuedCompletionStatus(lpOverlapped,
        BindIoCompletionCallbackStub);

    if (!res)
    {
        if (GetLastError() == ERROR_CALL_NOT_IMPLEMENTED)
            COMPlusThrow(kPlatformNotSupportedException);
        else
            COMPlusThrowWin32();
    }

    HELPER_METHOD_FRAME_END();
    FC_RETURN_BOOL(res);
}
FCIMPLEND


/********************************************************************************************************************/


/******************************************************************************************/
/*                                                                                        */
/*                              Timer Functions                                           */
/*                                                                                        */
/******************************************************************************************/

void AppDomainTimerCallback_Worker(LPVOID ptr)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    MethodDesc *pMeth = CoreLibBinder::GetMethod(METHOD__TIMER_QUEUE__APPDOMAIN_TIMER_CALLBACK);
    LogCall(pMeth,"AppDomainTimerCallback");
#endif

    ThreadpoolMgr::TimerInfoContext* pTimerInfoContext = (ThreadpoolMgr::TimerInfoContext*)ptr;
    ARG_SLOT args[] = { PtrToArgSlot(pTimerInfoContext->TimerId) };
    MethodDescCallSite(METHOD__TIMER_QUEUE__APPDOMAIN_TIMER_CALLBACK).Call(args);
}

VOID WINAPI AppDomainTimerCallback(PVOID callbackState, BOOLEAN timerOrWaitFired)
{
    Thread* pThread = GetThread();
    if (pThread == NULL)
    {
        // TODO: how do we notify user of OOM here?
        ClrFlsSetThreadType(ThreadType_Threadpool_Worker);
        pThread = SetupThreadNoThrow();
        if (pThread == NULL) {
            return;
        }
    }

    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    GCX_COOP();

    ThreadpoolMgr::TimerInfoContext* pTimerInfoContext = (ThreadpoolMgr::TimerInfoContext*)callbackState;
    ManagedThreadBase::ThreadPool(AppDomainTimerCallback_Worker, pTimerInfoContext);
}

HANDLE QCALLTYPE AppDomainTimerNative::CreateAppDomainTimer(INT32 dueTime, INT32 timerId)
{
    QCALL_CONTRACT;

    HANDLE hTimer = NULL;
    BEGIN_QCALL;

    _ASSERTE(dueTime >= 0);
    _ASSERTE(timerId >= 0);

    AppDomain* pAppDomain = GetThread()->GetDomain();

    ThreadpoolMgr::TimerInfoContext* timerContext = new ThreadpoolMgr::TimerInfoContext();
    timerContext->TimerId = timerId;
    NewHolder<ThreadpoolMgr::TimerInfoContext> timerContextHolder(timerContext);

    BOOL res = ThreadpoolMgr::CreateTimerQueueTimer(
        &hTimer,
        (WAITORTIMERCALLBACK)AppDomainTimerCallback,
        (PVOID)timerContext,
        (ULONG)dueTime,
        (ULONG)-1 /* this timer doesn't repeat */,
        0 /* no flags */);

    if (!res)
    {
        if (GetLastError() == ERROR_CALL_NOT_IMPLEMENTED)
            COMPlusThrow(kNotSupportedException);
        else
            COMPlusThrowWin32();
    }
    else
    {
        timerContextHolder.SuppressRelease();
    }

    END_QCALL;
    return hTimer;
}

BOOL QCALLTYPE AppDomainTimerNative::DeleteAppDomainTimer(HANDLE hTimer)
{
    QCALL_CONTRACT;

    BOOL res = FALSE;
    BEGIN_QCALL;

    _ASSERTE(hTimer != NULL && hTimer != INVALID_HANDLE_VALUE);
    res = ThreadpoolMgr::DeleteTimerQueueTimer(hTimer, NULL);

    if (!res)
    {
        DWORD errorCode = ::GetLastError();
        if (errorCode != ERROR_IO_PENDING)
            COMPlusThrowWin32(HRESULT_FROM_WIN32(errorCode));
    }

    END_QCALL;
    return res;
}


BOOL QCALLTYPE AppDomainTimerNative::ChangeAppDomainTimer(HANDLE hTimer, INT32 dueTime)
{
    QCALL_CONTRACT;

    BOOL res = FALSE;
    BEGIN_QCALL;

    _ASSERTE(hTimer != NULL && hTimer != INVALID_HANDLE_VALUE);
    _ASSERTE(dueTime >= 0);

    res = ThreadpoolMgr::ChangeTimerQueueTimer(
        hTimer,
        (ULONG)dueTime,
        (ULONG)-1 /* this timer doesn't repeat */);

    if (!res)
        COMPlusThrowWin32();

    END_QCALL;
    return res;
}
