// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================

#include "common.h"

#include "finalizerthread.h"
#include "threadsuspend.h"
#include "jithost.h"

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#endif

BOOL FinalizerThread::fQuitFinalizer = FALSE;

#if defined(__linux__) && defined(FEATURE_EVENT_TRACE)
#define LINUX_HEAP_DUMP_TIME_OUT 10000

extern bool s_forcedGCInProgress;
ULONGLONG FinalizerThread::LastHeapDumpTime = 0;

Volatile<BOOL> g_TriggerHeapDump = FALSE;
#endif // __linux__

CLREvent * FinalizerThread::hEventFinalizer = NULL;
CLREvent * FinalizerThread::hEventFinalizerDone = NULL;
CLREvent * FinalizerThread::hEventFinalizerToShutDown = NULL;

HANDLE FinalizerThread::MHandles[kHandleCount];

BOOL FinalizerThread::IsCurrentThreadFinalizer()
{
    LIMITED_METHOD_CONTRACT;

    return GetThread() == g_pFinalizerThread;
}

void FinalizerThread::EnableFinalization()
{
    WRAPPER_NO_CONTRACT;

    hEventFinalizer->Set();
}

BOOL FinalizerThread::HaveExtraWorkForFinalizer()
{
    WRAPPER_NO_CONTRACT;

    return GetFinalizerThread()->HaveExtraWorkForFinalizer();
}

// This helper is here to avoid EH goo associated with DefineFullyQualifiedNameForStack being 
// invoked when logging is off.
NOINLINE
void LogFinalization(Object* obj)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

#ifdef FEATURE_EVENT_TRACE
    ETW::GCLog::SendFinalizeObjectEvent(obj->GetMethodTable(), obj);
#endif // FEATURE_EVENT_TRACE
}


void CallFinalizer(Object* obj)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    MethodTable     *pMT = obj->GetMethodTable();
    STRESS_LOG2(LF_GC, LL_INFO1000, "Finalizing object %p MT %pT\n", obj, pMT);
    LOG((LF_GC, LL_INFO1000, "Finalizing " LOG_OBJECT_CLASS(obj)));

    _ASSERTE(GetThread()->PreemptiveGCDisabled());
    // if we don't have a class, we can't call the finalizer
    // if the object has been marked run as finalizer run don't call either
    if (pMT)
    {
        if (!((obj->GetHeader()->GetBits()) & BIT_SBLK_FINALIZER_RUN))
        {

            _ASSERTE(obj->GetMethodTable() == pMT);
            _ASSERTE(pMT->HasFinalizer());

            LogFinalization(obj);
            MethodTable::CallFinalizer(obj);
        }
        else
        {
            //reset the bit so the object can be put on the list 
            //with RegisterForFinalization
            obj->GetHeader()->ClrBit (BIT_SBLK_FINALIZER_RUN);
        }
    }
}

void FinalizerThread::DoOneFinalization(Object* fobj, Thread* pThread)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    CallFinalizer(fobj);

    pThread->InternalReset();
}

void FinalizerThread::FinalizeAllObjects(int bitToCheck)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    FireEtwGCFinalizersBegin_V1(GetClrInstanceId());

    unsigned int fcount = 0; 

    Object* fobj = GCHeapUtilities::GetGCHeap()->GetNextFinalizable();

    Thread *pThread = GetThread();

    // Finalize everyone
    while (fobj)
    {
        if (fobj->GetHeader()->GetBits() & bitToCheck)
        {
            fobj = GCHeapUtilities::GetGCHeap()->GetNextFinalizable();
        }
        else
        {
            fcount++;
            DoOneFinalization(fobj, pThread);
            fobj = GCHeapUtilities::GetGCHeap()->GetNextFinalizable();
        }
    }
    FireEtwGCFinalizersEnd_V1(fcount, GetClrInstanceId());
}

void FinalizerThread::WaitForFinalizerEvent (CLREvent *event)
{
    // Non-host environment

    // We don't want kLowMemoryNotification to starve out kFinalizer
    // (as the latter may help correct the former), and we don't want either
    // to starve out kProfilingAPIAttach, as we want decent responsiveness
    // to a user trying to attach a profiler.  So check in this order:
    //     kProfilingAPIAttach alone (0 wait)
    //     kFinalizer alone (2s wait)
    //     all events together (infinite wait)

    //give a chance to the finalizer event (2s)
    switch (event->Wait(2000, FALSE))
    {
    case (WAIT_OBJECT_0):
        return;
    case (WAIT_ABANDONED):
        return;
    case (WAIT_TIMEOUT):
        break;
    }
    MHandles[kFinalizer] = event->GetHandleUNHOSTED();
    while (1)
    {
        // WaitForMultipleObjects will wait on the event handles in MHandles
        // starting at this offset
        UINT uiEventIndexOffsetForWait = 0;
            
        // WaitForMultipleObjects will wait on this number of event handles
        DWORD cEventsForWait = kHandleCount;

        // #MHandleTypeValues:
        // WaitForMultipleObjects will now wait on a subset of the events in the
        // MHandles array. At this point kFinalizer should have a non-NULL entry
        // in the array. Wait on the following events:
        //
        //     * kLowMemoryNotification (if it's non-NULL && g_fEEStarted)
        //     * kFinalizer (always)
        //     * kProfilingAPIAttach (if it's non-NULL)
        //
        // The enum code:MHandleType values become important here, as
        // WaitForMultipleObjects needs to wait on a contiguous set of non-NULL
        // entries in MHandles, so we'll assert the values are contiguous as we
        // expect.
        _ASSERTE(kLowMemoryNotification == 0);
        _ASSERTE((kFinalizer == 1) && (MHandles[1] != NULL));

        // Exclude the low-memory notification event from the wait if the event
        // handle is NULL or the EE isn't fully started up yet.
        if ((MHandles[kLowMemoryNotification] == NULL) || !g_fEEStarted)
        {
            uiEventIndexOffsetForWait = kLowMemoryNotification + 1;
            cEventsForWait--;
        }

        switch (WaitForMultipleObjectsEx(
            cEventsForWait,                           // # objects to wait on
            &(MHandles[uiEventIndexOffsetForWait]),   // array of objects to wait on
            FALSE,          // bWaitAll == FALSE, so wait for first signal
#if defined(__linux__) && defined(FEATURE_EVENT_TRACE)
            LINUX_HEAP_DUMP_TIME_OUT,
#else
            INFINITE,       // timeout
#endif
            FALSE)          // alertable
                
            // Adjust the returned array index for the offset we used, so the return
            // value is relative to entire MHandles array
            + uiEventIndexOffsetForWait)
        {
        case (WAIT_OBJECT_0 + kLowMemoryNotification):
            //short on memory GC immediately
            GetFinalizerThread()->DisablePreemptiveGC();
            GCHeapUtilities::GetGCHeap()->GarbageCollect(0, true);
            GetFinalizerThread()->EnablePreemptiveGC();
            //wait only on the event for 2s
            switch (event->Wait(2000, FALSE))
            {
            case (WAIT_OBJECT_0):
                return;
            case (WAIT_ABANDONED):
                return;
            case (WAIT_TIMEOUT):
                break;
            }
            break;
        case (WAIT_OBJECT_0 + kFinalizer):
            return;
#if defined(__linux__) && defined(FEATURE_EVENT_TRACE)
        case (WAIT_TIMEOUT + kLowMemoryNotification):
        case (WAIT_TIMEOUT + kFinalizer):
            if (g_TriggerHeapDump)
            {
                return;
            }

            break;
#endif
        default:
            //what's wrong?
            _ASSERTE (!"Bad return code from WaitForMultipleObjects");
            return;
        }
    }
}



static BOOL s_FinalizerThreadOK = FALSE;



VOID FinalizerThread::FinalizerThreadWorker(void *args)
{
    SCAN_IGNORE_THROW;
    SCAN_IGNORE_TRIGGER;

    BOOL bPriorityBoosted = FALSE;

    while (!fQuitFinalizer)
    {
        // Wait for work to do...

        _ASSERTE(GetFinalizerThread()->PreemptiveGCDisabled());
#ifdef _DEBUG
        if (g_pConfig->FastGCStressLevel())
        {
            GetFinalizerThread()->m_GCOnTransitionsOK = FALSE;
        }
#endif
        GetFinalizerThread()->EnablePreemptiveGC();
#ifdef _DEBUG
        if (g_pConfig->FastGCStressLevel())
        {
            GetFinalizerThread()->m_GCOnTransitionsOK = TRUE;
        }
#endif
#if 0
        // Setting the event here, instead of at the bottom of the loop, could
        // cause us to skip draining the Q, if the request is made as soon as
        // the app starts running.
        SignalFinalizationDone(TRUE);
#endif //0

        WaitForFinalizerEvent (hEventFinalizer);

#if defined(__linux__) && defined(FEATURE_EVENT_TRACE)
        if (g_TriggerHeapDump && (CLRGetTickCount64() > (LastHeapDumpTime + LINUX_HEAP_DUMP_TIME_OUT)))
        {
            s_forcedGCInProgress = true;
            GetFinalizerThread()->DisablePreemptiveGC();
            GCHeapUtilities::GetGCHeap()->GarbageCollect(2, false, collection_blocking);
            GetFinalizerThread()->EnablePreemptiveGC();
            s_forcedGCInProgress = false;
            
            LastHeapDumpTime = CLRGetTickCount64();
            g_TriggerHeapDump = FALSE;
        }
#endif

        if (!bPriorityBoosted)
        {
            if (GetFinalizerThread()->SetThreadPriority(THREAD_PRIORITY_HIGHEST))
                bPriorityBoosted = TRUE;
        }

        JitHost::Reclaim();

        GetFinalizerThread()->DisablePreemptiveGC();

#ifdef _DEBUG
        // <TODO> workaround.  make finalization very lazy for gcstress 3 or 4.  
        // only do finalization if the system is quiescent</TODO>
        if (g_pConfig->GetGCStressLevel() > 1)
        {
            size_t last_gc_count;
            DWORD dwSwitchCount = 0;

            do
            {
                last_gc_count = GCHeapUtilities::GetGCHeap()->CollectionCount(0);
                GetFinalizerThread()->m_GCOnTransitionsOK = FALSE; 
                GetFinalizerThread()->EnablePreemptiveGC();
                __SwitchToThread (0, ++dwSwitchCount);
                GetFinalizerThread()->DisablePreemptiveGC();             
                // If no GCs happended, then we assume we are quiescent
                GetFinalizerThread()->m_GCOnTransitionsOK = TRUE; 
            } while (GCHeapUtilities::GetGCHeap()->CollectionCount(0) - last_gc_count > 0);
        }
#endif //_DEBUG

        // we might want to do some extra work on the finalizer thread
        // check and do it
        if (GetFinalizerThread()->HaveExtraWorkForFinalizer())
        {
            GetFinalizerThread()->DoExtraWorkForFinalizer();
        }
        LOG((LF_GC, LL_INFO100, "***** Calling Finalizers\n"));
        // We may mark the finalizer thread for abort.  If so the abort request is for previous finalizer method, not for next one.
        if (GetFinalizerThread()->IsAbortRequested())
        {
            GetFinalizerThread()->EEResetAbort(Thread::TAR_ALL);
        }

        FinalizeAllObjects(0);
        _ASSERTE(GetFinalizerThread()->GetDomain()->IsDefaultDomain());

        // We may still have the finalizer thread for abort.  If so the abort request is for previous finalizer method, not for next one.
        if (GetFinalizerThread()->IsAbortRequested())
        {
            GetFinalizerThread()->EEResetAbort(Thread::TAR_ALL);
        }

        // Anyone waiting to drain the Q can now wake up.  Note that there is a
        // race in that another thread starting a drain, as we leave a drain, may
        // consider itself satisfied by the drain that just completed.  This is
        // acceptable.
        SignalFinalizationDone(TRUE);
    }
}

DWORD WINAPI FinalizerThread::FinalizerThreadStart(void *args)
{
    ClrFlsSetThreadType (ThreadType_Finalizer);

    ASSERT(args == 0);
    ASSERT(hEventFinalizer->IsValid());

    // TODO: The following line should be removed after contract violation is fixed.
    // See bug 27409
    SCAN_IGNORE_THROW;
    SCAN_IGNORE_TRIGGER;

    LOG((LF_GC, LL_INFO10, "Finalizer thread starting...\n"));

    _ASSERTE(GetFinalizerThread()->GetDomain()->IsDefaultDomain());

#if defined(FEATURE_COMINTEROP_APARTMENT_SUPPORT) && !defined(FEATURE_COMINTEROP)
    // Make sure the finalizer thread is set to MTA to avoid hitting
    // DevDiv Bugs 180773 - [Stress Failure] AV at CoreCLR!SafeQueryInterfaceHelper 
    GetFinalizerThread()->SetApartment(Thread::AS_InMTA, FALSE);
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT && !FEATURE_COMINTEROP

    s_FinalizerThreadOK = GetFinalizerThread()->HasStarted();

    _ASSERTE(s_FinalizerThreadOK);
    _ASSERTE(GetThread() == GetFinalizerThread());

    // finalizer should always park in default domain

    if (s_FinalizerThreadOK)
    {
        INSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP;

#ifdef _DEBUG       // The only purpose of this try/finally is to trigger an assertion
        EE_TRY_FOR_FINALLY(void *, unused, NULL)
        {
#endif
            GetFinalizerThread()->SetBackground(TRUE);

            EnsureYieldProcessorNormalizedInitialized();

            while (!fQuitFinalizer)
            {
                // This will apply any policy for swallowing exceptions during normal
                // processing, without allowing the finalizer thread to disappear on us.
                ManagedThreadBase::FinalizerBase(FinalizerThreadWorker);

                // If we came out on an exception, then we probably lost the signal that
                // there are objects in the queue ready to finalize.  The safest thing is
                // to reenable finalization.
                if (!fQuitFinalizer)
                    EnableFinalization();
            }

            AppDomain::RaiseExitProcessEvent();

            // We have been asked to quit, so must be shutting down
            _ASSERTE(g_fEEShutDown);
            _ASSERTE(GetFinalizerThread()->PreemptiveGCDisabled());

            hEventFinalizerToShutDown->Set();

#ifdef _DEBUG       // The only purpose of this try/finally is to trigger an assertion
        }
        EE_FINALLY
        {
            // We can have exception to reach here if policy tells us to 
            // let exception go on finalizer thread.
            //
            if (GOT_EXCEPTION() && SwallowUnhandledExceptions())
                _ASSERTE(!"Exception in the finalizer thread!");

        }
        EE_END_FINALLY;
#endif
        UNINSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP;
    }
    // finalizer should always park in default domain
    _ASSERTE(GetThread()->GetDomain()->IsDefaultDomain());

    LOG((LF_GC, LL_INFO10, "Finalizer thread done."));

    // Enable pre-emptive GC before we leave so that anybody trying to suspend
    // us will not end up waiting forever. Don't do a DestroyThread because this
    // will happen soon when we tear down the thread store.
    GetFinalizerThread()->EnablePreemptiveGC();

    // We do not want to tear Finalizer thread,
    // since doing so will cause OLE32 to CoUninitialize.
    while (1)
    {
        PAL_TRY(void *, unused, NULL)
        {
            __SwitchToThread(INFINITE, CALLER_LIMITS_SPINNING);
        }
        PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
        {
        }
        PAL_ENDTRY
    }

    return 0;
}

void FinalizerThread::FinalizerThreadCreate()
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

#ifndef FEATURE_PAL
    MHandles[kLowMemoryNotification] =
        CreateMemoryResourceNotification(LowMemoryResourceNotification);
#endif // FEATURE_PAL

    hEventFinalizerDone = new CLREvent();
    hEventFinalizerDone->CreateManualEvent(FALSE);
    hEventFinalizer = new CLREvent();
    hEventFinalizer->CreateAutoEvent(FALSE);
    hEventFinalizerToShutDown = new CLREvent();
    hEventFinalizerToShutDown->CreateAutoEvent(FALSE);

    _ASSERTE(g_pFinalizerThread == 0);
    g_pFinalizerThread = SetupUnstartedThread();

    // We don't want the thread block disappearing under us -- even if the
    // actual thread terminates.
    GetFinalizerThread()->IncExternalCount();

    if (GetFinalizerThread()->CreateNewThread(0, &FinalizerThreadStart, NULL, W(".NET Finalizer")) )
    {
        DWORD dwRet = GetFinalizerThread()->StartThread();

        // When running under a user mode native debugger there is a race
        // between the moment we've created the thread (in CreateNewThread) and 
        // the moment we resume it (in StartThread); the debugger may receive 
        // the "ct" (create thread) notification, and it will attempt to 
        // suspend/resume all threads in the process.  Now imagine the debugger
        // resumes this thread first, and only later does it try to resume the
        // newly created thread (the finalizer thread).  In these conditions our
        // call to ResumeThread may come before the debugger's call to ResumeThread
        // actually causing dwRet to equal 2.
        // We cannot use IsDebuggerPresent() in the condition below because the 
        // debugger may have been detached between the time it got the notification
        // and the moment we execute the test below.
        _ASSERTE(dwRet == 1 || dwRet == 2);
    }
}

void FinalizerThread::SignalFinalizationDone(BOOL fFinalizer)
{
    WRAPPER_NO_CONTRACT;

    if (fFinalizer)
    {
        FastInterlockAnd((DWORD*)&g_FinalizerWaiterStatus, ~FWS_WaitInterrupt);
    }
    hEventFinalizerDone->Set();
}

// Wait for the finalizer thread to complete one pass.
void FinalizerThread::FinalizerThreadWait(DWORD timeout)
{
    ASSERT(hEventFinalizerDone->IsValid());
    ASSERT(hEventFinalizer->IsValid());
    ASSERT(GetFinalizerThread());

    // Can't call this from within a finalized method.
    if (!IsCurrentThreadFinalizer())
    {
#ifdef FEATURE_COMINTEROP
        // To help combat finalizer thread starvation, we check to see if there are any wrappers
        // scheduled to be cleaned up for our context.  If so, we'll do them here to avoid making
        // the finalizer thread do a transition.
        if (g_pRCWCleanupList != NULL)
            g_pRCWCleanupList->CleanupWrappersInCurrentCtxThread();
#endif // FEATURE_COMINTEROP

        GCX_PREEMP();

        Thread *pThread = GetThread();

        ULONGLONG startTime = CLRGetTickCount64();
        ULONGLONG endTime;
        if (timeout == INFINITE)
        {
            endTime = MAXULONGLONG;
        }
        else
        {
            endTime = timeout + startTime;
        }

        while (TRUE)
        {
            hEventFinalizerDone->Reset();
            EnableFinalization();

            //----------------------------------------------------
            // Do appropriate wait and pump messages if necessary
            //----------------------------------------------------

            DWORD status = hEventFinalizerDone->Wait(timeout,TRUE);
            if (status != WAIT_TIMEOUT && !(g_FinalizerWaiterStatus & FWS_WaitInterrupt))
            {
                return;
            }
            // recalculate timeout
            if (timeout != INFINITE)
            {
                ULONGLONG curTime = CLRGetTickCount64();
                if (curTime >= endTime)
                {
                    return;
                }
                else
                {
                    timeout = (DWORD)(endTime - curTime);
                }
            }
        }
    }
}
