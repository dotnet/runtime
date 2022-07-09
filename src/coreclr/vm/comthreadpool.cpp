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

    auto TryGetConfig =
        [=](const CLRConfig::ConfigDWORDInfo &configInfo, bool isBoolean, const WCHAR *appContextConfigName) -> bool
    {
        bool wasNotConfigured = true;
        *configValueRef = CLRConfig::GetConfigValue(configInfo, &wasNotConfigured);
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
            *configValueRef = 2;
            *isBooleanRef = false;
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
    Thread* pThread = GetThreadNULLOk();
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
    Thread* pThread = GetThreadNULLOk();
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
