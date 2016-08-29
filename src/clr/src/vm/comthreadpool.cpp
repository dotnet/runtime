// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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
#include "security.h"
#include "eeconfig.h"
#include "corhost.h"
#include "nativeoverlapped.h"
#include "comsynchronizable.h"
#ifdef FEATURE_REMOTING
#include "crossdomaincalls.h"
#else
#include "callhelpers.h"
#endif
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
DelegateInfo *DelegateInfo::MakeDelegateInfo(AppDomain *pAppDomain,
                                             OBJECTREF *state,
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
        PRECONDITION(CheckPointer(pAppDomain));
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    // If there were any DelegateInfos waiting to be released, they'll get flushed now
    ThreadpoolMgr::FlushQueueOfTimerInfos();
    
    DelegateInfoHolder delegateInfo = (DelegateInfo*) ThreadpoolMgr::GetRecycledMemory(ThreadpoolMgr::MEMTYPE_DelegateInfo);
    
    delegateInfo->m_appDomainId = pAppDomain->GetId();

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

    delegateInfo->m_overridesCount = 0;
    delegateInfo->m_hasSecurityInfo = FALSE;

    delegateInfo.SuppressRelease();
    
    return delegateInfo;
}

/*****************************************************************************************************/
FCIMPL2(FC_BOOL_RET, ThreadPoolNative::CorSetMaxThreads,DWORD workerThreads, DWORD completionPortThreads)
{
    FCALL_CONTRACT;

    BOOL bRet = FALSE;
    HELPER_METHOD_FRAME_BEGIN_RET_0(); // Eventually calls BEGIN_SO_INTOLERANT_CODE_NOTHROW

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
    HELPER_METHOD_FRAME_BEGIN_RET_0(); // Eventually calls BEGIN_SO_INTOLERANT_CODE_NOTHROW

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

FCIMPL0(VOID, ThreadPoolNative::NotifyRequestProgress)
{
    FCALL_CONTRACT;

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
    ThreadpoolMgr::ReportThreadStatus(isWorking);
}
FCIMPLEND

FCIMPL0(FC_BOOL_RET, ThreadPoolNative::NotifyRequestComplete)
{
    FCALL_CONTRACT;

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
        pThread->HasThreadStateNC(Thread::TSNC_SOWorkNeeded) ||
        !pThread->IsBackground() ||
        pThread->HasCriticalRegion() ||
        pThread->HasThreadAffinity();

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
            pThread->InternalReset(FALSE, TRUE, TRUE, FALSE);

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

void QCALLTYPE ThreadPoolNative::InitializeVMTp(CLR_BOOL* pEnableWorkerTracking)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    ThreadpoolMgr::EnsureInitialized();
    *pEnableWorkerTracking = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ThreadPool_EnableWorkerTracking) ? TRUE : FALSE;
    END_QCALL;
}


FCIMPL0(FC_BOOL_RET, ThreadPoolNative::IsThreadPoolHosted)
{
    FCALL_CONTRACT;

    FCUnique(0x22);

    FC_RETURN_BOOL(ThreadpoolMgr::IsThreadPoolHosted());
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
    MethodDesc *pMeth = MscorlibBinder::GetMethod(METHOD__TPWAITORTIMER_HELPER__PERFORM_WAITORTIMER_CALLBACK);
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


void ResetThreadSecurityState(Thread* pThread)
{
    CONTRACTL 
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;
    
    if (pThread)
    {
        pThread->ResetSecurityInfo();
    }
}

// this holder resets our thread's security state
typedef Holder<Thread*, DoNothing<Thread*>, ResetThreadSecurityState> ThreadSecurityStateHolder;

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

    // This thread should not have any locks held at entry point.
    _ASSERTE(pThread->m_dwLockCount == 0);

    GCX_COOP();

    // this holder resets our thread's security state when exiting this scope
    ThreadSecurityStateHolder  secState(pThread);

    RegisterWaitForSingleObjectCallback_Args args = { ((DelegateInfo*) delegateInfo), TimerOrWaitFired };

    ManagedThreadBase::ThreadPool(((DelegateInfo*) delegateInfo)->m_appDomainId, RegisterWaitForSingleObjectCallback_Worker, &args);

    // We should have released all locks.
    _ASSERTE(g_fEEShutDown || pThread->m_dwLockCount == 0 || pThread->m_fRudeAborted);
    return;
}

void ThreadPoolNative::Init()
{

}


FCIMPL7(LPVOID, ThreadPoolNative::CorRegisterWaitForSingleObject,
                                        Object* waitObjectUNSAFE,
                                        Object* stateUNSAFE,
                                        UINT32 timeout,
                                        CLR_BOOL executeOnlyOnce,
                                        Object* registeredWaitObjectUNSAFE,
                                        StackCrawlMark* stackMark,
                                        CLR_BOOL compressStack)
{
    FCALL_CONTRACT;
    
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
    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);  // Eventually calls BEGIN_SO_INTOLERANT_CODE_NOTHROW

    if(gc.waitObject == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_Obj"));

    _ASSERTE(gc.registeredWaitObject != NULL);

    ULONG flag = executeOnlyOnce ? WAIT_SINGLE_EXECUTION | WAIT_FREE_CONTEXT : WAIT_FREE_CONTEXT;

    HANDLE hWaitHandle = gc.waitObject->GetWaitHandle();
    _ASSERTE(hWaitHandle);

    Thread* pCurThread = GetThread();
    _ASSERTE( pCurThread);

    AppDomain* appDomain = pCurThread->GetDomain();
    _ASSERTE(appDomain);

    DelegateInfoHolder delegateInfo = DelegateInfo::MakeDelegateInfo(appDomain,
                                                                &gc.state,
                                                                (OBJECTREF *)&gc.waitObject,
                                                                &gc.registeredWaitObject);

    if (compressStack)
    {
        delegateInfo->SetThreadSecurityInfo( pCurThread, stackMark );
    }



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


VOID QueueUserWorkItemManagedCallback(PVOID pArg)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    _ASSERTE(NULL != pArg);

    // This thread should not have any locks held at entry point.
    _ASSERTE(GetThread()->m_dwLockCount == 0);

    bool* wasNotRecalled = (bool*)pArg;

    MethodDescCallSite dispatch(METHOD__TP_WAIT_CALLBACK__PERFORM_WAIT_CALLBACK);
    *wasNotRecalled = dispatch.Call_RetBool(NULL);
}


BOOL QCALLTYPE ThreadPoolNative::RequestWorkerThread()
{
    QCALL_CONTRACT;

    BOOL res = FALSE;

    BEGIN_QCALL;

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


/********************************************************************************************************************/

FCIMPL2(FC_BOOL_RET, ThreadPoolNative::CorUnregisterWait, LPVOID WaitHandle, Object* objectToNotify)
{
    FCALL_CONTRACT;

    BOOL retVal = false;
    SAFEHANDLEREF refSH = (SAFEHANDLEREF) ObjectToOBJECTREF(objectToNotify);
    HELPER_METHOD_FRAME_BEGIN_RET_1(refSH); // Eventually calls BEGIN_SO_INTOLERANT_CODE_NOTHROW

    HANDLE hWait = (HANDLE) WaitHandle;
    HANDLE hObjectToNotify = NULL;

    ThreadpoolMgr::WaitInfo *pWaitInfo = (ThreadpoolMgr::WaitInfo *)hWait;
    _ASSERTE(pWaitInfo != NULL);

    ThreadpoolMgr::WaitInfoHolder   wiHolder(NULL);

    if (refSH != NULL)
    {
        // Create a GCHandle in the WaitInfo, so that it can hold on to the safe handle
        pWaitInfo->ExternalEventSafeHandle = GetAppDomain()->CreateHandle(NULL);
        pWaitInfo->handleOwningAD = GetAppDomain()->GetId();

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

    HELPER_METHOD_FRAME_BEGIN_0(); // Eventually calls BEGIN_SO_INTOLERANT_CODE_NOTHROW

    HANDLE hWait = (HANDLE)WaitHandle;
    ThreadpoolMgr::WaitHandleCleanup(hWait);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

/********************************************************************************************************************/

/********************************************************************************************************************/

struct BindIoCompletion_Args
{
    DWORD ErrorCode;
    DWORD numBytesTransferred;
    LPOVERLAPPED lpOverlapped;
    BOOL *pfProcessed;
};

void SetAsyncResultProperties(
    OVERLAPPEDDATAREF overlapped,
    DWORD dwErrorCode, 
    DWORD dwNumBytes
)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_SO_TOLERANT;

    ASYNCRESULTREF asyncResult = overlapped->m_asyncResult;
    // only filestream is expected to have a null delegate in which
    // case we do the necessary book-keeping here. However, for robustness
    // we should make sure that the asyncResult is indeed an instance of
    // FileStreamAsyncResult
    if (asyncResult->GetMethodTable() == g_pAsyncFileStream_AsyncResultClass)
    {
        // Handle reading from & writing to closed pipes. It's possible for
        // an async read on a pipe to be issued and then the pipe is closed,
        // returning this error.  This may very well be necessary. -BG
        if (dwErrorCode == ERROR_BROKEN_PIPE || dwErrorCode == ERROR_NO_DATA)
            dwErrorCode = 0;
        asyncResult->SetErrorCode(dwErrorCode);
        asyncResult->SetNumBytes(dwNumBytes);
        asyncResult->SetCompletedAsynchronously();
        asyncResult->SetIsComplete();

        // Signal the event - the OS does not do this for us.
        WAITHANDLEREF waitHandle = asyncResult->GetWaitHandle();
        HANDLE h = waitHandle->GetWaitHandle();
        if ((h != NULL) && (h != (HANDLE) -1))
            UnsafeSetEvent(h);
    }
}

VOID BindIoCompletionCallBack_Worker(LPVOID args)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_SO_INTOLERANT;
    
    DWORD        ErrorCode = ((BindIoCompletion_Args *)args)->ErrorCode;
    DWORD        numBytesTransferred = ((BindIoCompletion_Args *)args)->numBytesTransferred;
    LPOVERLAPPED lpOverlapped = ((BindIoCompletion_Args *)args)->lpOverlapped;
    
    OVERLAPPEDDATAREF overlapped = ObjectToOVERLAPPEDDATAREF(OverlappedDataObject::GetOverlapped(lpOverlapped));

    GCPROTECT_BEGIN(overlapped);
    *(((BindIoCompletion_Args *)args)->pfProcessed) = TRUE;
    // we set processed to TRUE, now it's our responsibility to guarantee proper cleanup

#ifdef _DEBUG
    MethodDesc *pMeth = MscorlibBinder::GetMethod(METHOD__IOCB_HELPER__PERFORM_IOCOMPLETION_CALLBACK);
    LogCall(pMeth,"IOCallback");
#endif

    if (overlapped->m_iocb != NULL)
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
    { // no user delegate to callback
        _ASSERTE((overlapped->m_iocbHelper == NULL) || !"This is benign, but should be optimized");

        // we cannot do this at threadpool initialization time since mscorlib may not have been loaded
        if (!g_pAsyncFileStream_AsyncResultClass)
        {
            g_pAsyncFileStream_AsyncResultClass = MscorlibBinder::GetClass(CLASS__FILESTREAM_ASYNCRESULT);
        }

        SetAsyncResultProperties(overlapped, ErrorCode, numBytesTransferred);
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
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    // This thread should not have any locks held at entry point.
    _ASSERTE(pThread->m_dwLockCount == 0);

    LOG((LF_INTEROP, LL_INFO10000, "In IO_CallBackStub thread 0x%x retCode 0x%x, overlap 0x%x\n",  pThread, ErrorCode, lpOverlapped));

    GCX_COOP();

    // NOTE: there is a potential race between the time we retrieve the app domain pointer,
    // and the time which this thread enters the domain.
    //
    // To solve the race, we rely on the fact that there is a thread sync (via GC)
    // between releasing an app domain's handle, and destroying the app domain.  Thus
    // it is important that we not go into preemptive gc mode in that window.
    //

    //IMPORTANT - do not gc protect overlapped here - it belongs to another appdomain
    //so if it stops being pinned it should be able to go away
    OVERLAPPEDDATAREF overlapped = ObjectToOVERLAPPEDDATAREF(OverlappedDataObject::GetOverlapped(lpOverlapped));
    AppDomainFromIDHolder appDomain(ADID(overlapped->GetAppDomainId()), TRUE);
    BOOL fProcessed = FALSE;
    if (!appDomain.IsUnloaded())
    {
        // this holder resets our thread's security state when exiting this scope, 
        // but only if setStack is TRUE.
        Thread* pHolderThread = NULL;
        if (setStack)
        {
            pHolderThread = pThread; 
        }

        ThreadSecurityStateHolder  secState(pHolderThread);

        BindIoCompletion_Args args = {ErrorCode, numBytesTransferred, lpOverlapped, &fProcessed};
        appDomain.Release();
        ManagedThreadBase::ThreadPool(ADID(overlapped->GetAppDomainId()), BindIoCompletionCallBack_Worker, &args);
    }

 


    LOG((LF_INTEROP, LL_INFO10000, "Leaving IO_CallBackStub thread 0x%x retCode 0x%x, overlap 0x%x\n",  pThread, ErrorCode, lpOverlapped));
        // We should have released all locks.
    _ASSERTE(g_fEEShutDown || pThread->m_dwLockCount == 0 || pThread->m_fRudeAborted);
    return;
}

void WINAPI BindIoCompletionCallbackStub(DWORD ErrorCode,
                                            DWORD numBytesTransferred,
                                            LPOVERLAPPED lpOverlapped)
{
    WRAPPER_NO_CONTRACT;
    BindIoCompletionCallbackStubEx(ErrorCode, numBytesTransferred, lpOverlapped, TRUE);

#ifndef FEATURE_PAL
    extern Volatile<ULONG> g_fCompletionPortDrainNeeded;

    Thread *pThread = GetThread();
    if (g_fCompletionPortDrainNeeded && pThread)
    {
        // We have started draining completion port.
        // The next job picked up by this thread is going to be after our special marker.
        if (!pThread->IsCompletionPortDrained())
        {
            pThread->MarkCompletionPortDrained();
        }
    }
#endif // !FEATURE_PAL
}

FCIMPL1(FC_BOOL_RET, ThreadPoolNative::CorBindIoCompletionCallback, HANDLE fileHandle)
{
    FCALL_CONTRACT;

    BOOL retVal = FALSE;

    HELPER_METHOD_FRAME_BEGIN_RET_0(); // Eventually calls BEGIN_SO_INTOLERANT_CODE_NOTHROW

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

    HELPER_METHOD_FRAME_BEGIN_RET_1(overlapped); // Eventually calls BEGIN_SO_INTOLERANT_CODE_NOTHROW

    // OS doesn't signal handle, so do it here
    overlapped->Internal = 0;

    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, ThreadPoolIOEnqueue))
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
    MethodDesc *pMeth = MscorlibBinder::GetMethod(METHOD__TIMER_QUEUE__APPDOMAIN_TIMER_CALLBACK);
    LogCall(pMeth,"AppDomainTimerCallback");
#endif

    MethodDescCallSite(METHOD__TIMER_QUEUE__APPDOMAIN_TIMER_CALLBACK).Call(NULL);
}

VOID WINAPI AppDomainTimerCallback(PVOID delegateInfo, BOOLEAN timerOrWaitFired)
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
        SO_INTOLERANT;
        
        PRECONDITION(CheckPointer(delegateInfo));
    }
    CONTRACTL_END;

    // This thread should not have any locks held at entry point.
    _ASSERTE(pThread->m_dwLockCount == 0);

    GCX_COOP();

    {
        ThreadSecurityStateHolder  secState(pThread);
        ManagedThreadBase::ThreadPool(((DelegateInfo*)delegateInfo)->m_appDomainId, AppDomainTimerCallback_Worker, NULL);
    }
    
    // We should have released all locks.
    _ASSERTE(g_fEEShutDown || pThread->m_dwLockCount == 0 || pThread->m_fRudeAborted);
}

HANDLE QCALLTYPE AppDomainTimerNative::CreateAppDomainTimer(INT32 dueTime)
{
    QCALL_CONTRACT;

    HANDLE hTimer = NULL;
    BEGIN_QCALL;

    _ASSERTE(dueTime >= 0);

    AppDomain* pAppDomain = GetThread()->GetDomain();
    ADID adid = pAppDomain->GetId();

    DelegateInfoHolder delegateInfo = DelegateInfo::MakeDelegateInfo(
        pAppDomain,
        NULL,
        NULL,
        NULL);

    BOOL res = ThreadpoolMgr::CreateTimerQueueTimer(
        &hTimer,
        (WAITORTIMERCALLBACK)AppDomainTimerCallback,
        (PVOID)delegateInfo,
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
        delegateInfo.SuppressRelease();
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
