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

#ifdef FEATURE_PROFAPI_ATTACH_DETACH 
#include "profattach.h"
#endif // FEATURE_PROFAPI_ATTACH_DETACH 

BOOL FinalizerThread::fRunFinalizersOnUnload = FALSE;
BOOL FinalizerThread::fQuitFinalizer = FALSE;

#if defined(__linux__) && defined(FEATURE_EVENT_TRACE)
#define LINUX_HEAP_DUMP_TIME_OUT 10000

extern bool s_forcedGCInProgress;
ULONGLONG FinalizerThread::LastHeapDumpTime = 0;

Volatile<BOOL> g_TriggerHeapDump = FALSE;
#endif // __linux__

CLREvent * FinalizerThread::hEventFinalizer = NULL;
CLREvent * FinalizerThread::hEventFinalizerDone = NULL;
CLREvent * FinalizerThread::hEventShutDownToFinalizer = NULL;
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
__declspec(noinline)
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

struct FinalizeAllObjects_Args {
    OBJECTREF fobj;
    int bitToCheck;
};

void FinalizerThread::FinalizeAllObjects_Wrapper(void *ptr)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    FinalizeAllObjects_Args *args = (FinalizeAllObjects_Args *) ptr;
    _ASSERTE(args->fobj);
    Object *fobj = OBJECTREFToObject(args->fobj);
    args->fobj = NULL;      // don't want to do this guy again, if we take an exception here:
    args->fobj = ObjectToOBJECTREF(FinalizeAllObjects(fobj, args->bitToCheck));
}

// The following is inadequate when we have multiple Finalizer threads in some future release.
// Instead, we will have to store this in TLS or pass it through the call tree of finalization.
// It is used to tie together the base exception handling and the AppDomain transition exception
// handling for this thread.
static struct ManagedThreadCallState *pThreadTurnAround;

Object * FinalizerThread::DoOneFinalization(Object* fobj, Thread* pThread,int bitToCheck,bool *pbTerminate)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    bool fTerminate=false;
    Object *pReturnObject = NULL;
    

    AppDomain* targetAppDomain = fobj->GetAppDomain();
    AppDomain* currentDomain = pThread->GetDomain();
    if (! targetAppDomain)
    {
        // if can't get into domain to finalize it, then it must be agile so finalize in current domain
        targetAppDomain = currentDomain;
    }

    if (targetAppDomain == currentDomain)
    {
        class ResetFinalizerStartTime
        {
        public:
            ResetFinalizerStartTime()
            {
                if (CLRHosted())
                {
                    g_ObjFinalizeStartTime = CLRGetTickCount64();
                }                    
            }
            ~ResetFinalizerStartTime()
            {
                if (g_ObjFinalizeStartTime)
                {
                    g_ObjFinalizeStartTime = 0;
                }
            }
        };
        {
            ThreadLocaleHolder localeHolder;

            {
                ResetFinalizerStartTime resetTime;
                CallFinalizer(fobj);
            }
        }
        pThread->InternalReset(FALSE);
    } 
    else 
    {
        if (!currentDomain->IsDefaultDomain())
        {
            // this means we are in some other domain, so need to return back out through the DoADCallback
            // and handle the object from there in another domain.
            pReturnObject = fobj;
            fTerminate = true;
        } 
        else
        {
            // otherwise call back to ourselves to process as many as we can in that other domain
            FinalizeAllObjects_Args args;
            args.fobj = ObjectToOBJECTREF(fobj);
            args.bitToCheck = bitToCheck;
            GCPROTECT_BEGIN(args.fobj);
            {
                ThreadLocaleHolder localeHolder;

                _ASSERTE(pThreadTurnAround != NULL);
                ManagedThreadBase::FinalizerAppDomain(targetAppDomain,
                                                      FinalizeAllObjects_Wrapper,
                                                      &args,
                                                      pThreadTurnAround);
            }
            pThread->InternalReset(FALSE);
            // process the object we got back or be done if we got back null
            pReturnObject = OBJECTREFToObject(args.fobj);
            GCPROTECT_END();
        }
    }        
        
    *pbTerminate = fTerminate;
    return pReturnObject;
}

Object * FinalizerThread::FinalizeAllObjects(Object* fobj, int bitToCheck)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    FireEtwGCFinalizersBegin_V1(GetClrInstanceId());

    unsigned int fcount = 0; 
    bool fTerminate = false;

    if (fobj == NULL)
    {
        fobj = GCHeapUtilities::GetGCHeap()->GetNextFinalizable();
    }

    Thread *pThread = GetThread();

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
    ULONGLONG ui64TimestampLastCheckedProfAttachEventMs = 0;
#endif //FEATURE_PROFAPI_ATTACH_DETACH

    // Finalize everyone
    while (fobj)
    {
#ifdef FEATURE_PROFAPI_ATTACH_DETACH
        // Don't let an overloaded finalizer queue starve out
        // an attaching profiler.  In between running finalizers,
        // check the profiler attach event without blocking.
        ProcessProfilerAttachIfNecessary(&ui64TimestampLastCheckedProfAttachEventMs);
#endif // FEATURE_PROFAPI_ATTACH_DETACH

        if (fobj->GetHeader()->GetBits() & bitToCheck)
        {
            fobj = GCHeapUtilities::GetGCHeap()->GetNextFinalizable();
        }
        else
        {
            fcount++;
            fobj = DoOneFinalization(fobj, pThread, bitToCheck,&fTerminate);
            if (fTerminate)
            {
                break;
            }

            if (fobj == NULL)
            {
                fobj = GCHeapUtilities::GetGCHeap()->GetNextFinalizable();
            }
        }
    }
    FireEtwGCFinalizersEnd_V1(fcount, GetClrInstanceId());
    
    return fobj;
}


#ifdef FEATURE_PROFAPI_ATTACH_DETACH

// ----------------------------------------------------------------------------
// ProcessProfilerAttachIfNecessary
// 
// Description:
//    This is called to peek at the Profiler Attach Event in between finalizers to check
//    if it's signaled. If it is, this calls
//    code:ProfilingAPIAttachDetach::ProcessSignaledAttachEvent to deal with it.
//    
//
// Arguments:
//     * pui64TimestampLastCheckedEventMs: [in / out]  This keeps track of how often the
//         Profiler Attach Event is checked, so it's not checked too often during a
//         tight loop (in particular, the loop in code:SVR::FinalizeAllObjects which
//         executes all finalizer routines in the queue).  This argument has the
//         following possible values:
//         * [in] (pui64TimestampLastCheckedEventMs) == NULL: Means the arg is not used, so
//             just check the event and ignore this argument
//         * [in] (*pui64TimestampLastCheckedEventMs) == 0: Arg is uninitialized.  Just
//             initialize it with the current tick count and return without checking the
//             event (as the event was probably just checked before entering the loop
//             that called this function).
//         * [in] (*pui64TimestampLastCheckedEventMs) != 0: Arg is initialized to the
//             approximate tick count of when the event was last checked.  If it's time
//             to check the event again, do so and update this parameter on [out] with
//             the current timestamp.  Otherwise, do nothing and return.
//             
// Notes:
//    * The Profiler Attach Event is also checked in the main WaitForMultipleObjects in
//        WaitForFinalizerEvent
//        

// static
void FinalizerThread::ProcessProfilerAttachIfNecessary(ULONGLONG * pui64TimestampLastCheckedEventMs)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    if (MHandles[kProfilingAPIAttach] == NULL)
    {
        return;
    }

    if (pui64TimestampLastCheckedEventMs != NULL)
    {
        if (*pui64TimestampLastCheckedEventMs == 0)
        {
            // Just initialize timestamp and leave
            *pui64TimestampLastCheckedEventMs = CLRGetTickCount64();
            return;
        }

        static DWORD dwMsBetweenCheckingProfAPIAttachEvent = 0;
        if (dwMsBetweenCheckingProfAPIAttachEvent == 0)
        {
            // First time through, initialize with how long to wait between checking the
            // event.
            dwMsBetweenCheckingProfAPIAttachEvent = CLRConfig::GetConfigValue(
                CLRConfig::EXTERNAL_MsBetweenAttachCheck);
        }
        ULONGLONG ui64TimestampNowMs = CLRGetTickCount64();
        _ASSERTE(ui64TimestampNowMs >= (*pui64TimestampLastCheckedEventMs));
        if (ui64TimestampNowMs - (*pui64TimestampLastCheckedEventMs) <
            dwMsBetweenCheckingProfAPIAttachEvent)
        {
            // Too soon, go home
            return;
        }

        // Otherwise, update the timestamp and wait on the finalizer event below
        *pui64TimestampLastCheckedEventMs = ui64TimestampNowMs;
    }

    // Check the attach event without waiting; only if it's signaled right now will we
    // process the event.
    if (WaitForSingleObject(MHandles[kProfilingAPIAttach], 0) != WAIT_OBJECT_0)
    {
        // Any return value that indicates we can't verify the attach event is signaled
        // right now means we should just forget about it and immediately return to
        // whatever we were doing
        return;
    }

    // Event is signaled; process it by spawning a new thread to do the work
    ProfilingAPIAttachDetach::ProcessSignaledAttachEvent();
}

#endif // FEATURE_PROFAPI_ATTACH_DETACH

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

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
    // NULL means check attach event now, and don't worry about how long it was since
    // the last time the event was checked.
    ProcessProfilerAttachIfNecessary(NULL);
#endif // FEATURE_PROFAPI_ATTACH_DETACH

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
#ifdef FEATURE_PROFAPI_ATTACH_DETACH 
        _ASSERTE(kProfilingAPIAttach == 2);
#endif //FEATURE_PROFAPI_ATTACH_DETACH 
            
        // Exclude the low-memory notification event from the wait if the event
        // handle is NULL or the EE isn't fully started up yet.
        if ((MHandles[kLowMemoryNotification] == NULL) || !g_fEEStarted)
        {
            uiEventIndexOffsetForWait = kLowMemoryNotification + 1;
            cEventsForWait--;
        }

#ifdef FEATURE_PROFAPI_ATTACH_DETACH 
        // Exclude kProfilingAPIAttach if it's NULL
        if (MHandles[kProfilingAPIAttach] == NULL)
        {
            cEventsForWait--;
        }
#endif //FEATURE_PROFAPI_ATTACH_DETACH 

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
#ifdef FEATURE_PROFAPI_ATTACH_DETACH
        case (WAIT_OBJECT_0 + kProfilingAPIAttach):
            // Spawn thread to perform the profiler attach, then resume our wait
            ProfilingAPIAttachDetach::ProcessSignaledAttachEvent();
            break;
#endif // FEATURE_PROFAPI_ATTACH_DETACH
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
    // TODO: The following line should be removed after contract violation is fixed.
    // See bug 27409
    SCAN_IGNORE_THROW;
    SCAN_IGNORE_TRIGGER;

    // This is used to stitch together the exception handling at the base of our thread with
    // any eventual transitions into different AppDomains for finalization.
    _ASSERTE(args != NULL);
    pThreadTurnAround = (ManagedThreadCallState *) args;

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
        FastInterlockExchange ((LONG*)&g_FinalizerIsRunning, TRUE);

        FinalizeAllObjects(NULL, 0);
        _ASSERTE(GetFinalizerThread()->GetDomain()->IsDefaultDomain());

        FastInterlockExchange ((LONG*)&g_FinalizerIsRunning, FALSE);
        // We may still have the finalizer thread for abort.  If so the abort request is for previous finalizer method, not for next one.
        if (GetFinalizerThread()->IsAbortRequested())
        {
            GetFinalizerThread()->EEResetAbort(Thread::TAR_ALL);
        }

        // Increment the loop count. This is currently used by the AddMemoryPressure heuristic to see
        // if finalizers have run since the last time it triggered GC.
        FastInterlockIncrement((LONG *)&g_FinalizerLoopCount);

        // Anyone waiting to drain the Q can now wake up.  Note that there is a
        // race in that another thread starting a drain, as we leave a drain, may
        // consider itself satisfied by the drain that just completed.  This is
        // acceptable.
        SignalFinalizationDone(TRUE);
    }
}


// During shutdown, finalize all objects that haven't been run yet... whether reachable or not.
void FinalizerThread::FinalizeObjectsOnShutdown(LPVOID args)
{
    WRAPPER_NO_CONTRACT;

    // This is used to stitch together the exception handling at the base of our thread with
    // any eventual transitions into different AppDomains for finalization.
    _ASSERTE(args != NULL);
    pThreadTurnAround = (ManagedThreadCallState *) args;

    FinalizeAllObjects(NULL, BIT_SBLK_FINALIZER_RUN);
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

#ifdef FEATURE_PROFAPI_ATTACH_DETACH 
            // Add the Profiler Attach Event to the array of event handles that the
            // finalizer thread waits on. If the process is not enabled for profiler
            // attach (e.g., running memory- or sync-hosted, or there is some other error
            // that causes the Profiler Attach Event not to be created), then this just
            // adds NULL in the slot where the Profiler Attach Event handle would go. In
            // this case, WaitForFinalizerEvent will know to ignore that handle when it
            // waits.
            // 
            // Calling ProfilingAPIAttachDetach::GetAttachEvent induces lazy
            // initialization of the profiling API attach/detach support objects,
            // including the event itself and its security descriptor. So switch to
            // preemptive mode during these OS calls
            GetFinalizerThread()->EnablePreemptiveGC();
            MHandles[kProfilingAPIAttach] = ::ProfilingAPIAttachDetach::GetAttachEvent();
            GetFinalizerThread()->DisablePreemptiveGC();
#endif // FEATURE_PROFAPI_ATTACH_DETACH 
    
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

            // Tell shutdown thread we are done with finalizing dead objects.
            hEventFinalizerToShutDown->Set();

            // Wait for shutdown thread to signal us.
            GetFinalizerThread()->EnablePreemptiveGC();
            hEventShutDownToFinalizer->Wait(INFINITE,FALSE);
            GetFinalizerThread()->DisablePreemptiveGC();

            AppDomain::RaiseExitProcessEvent();

            hEventFinalizerToShutDown->Set();

            // Phase 1 ends.
            // Now wait for Phase 2 signal.

            // Wait for shutdown thread to signal us.
            GetFinalizerThread()->EnablePreemptiveGC();
            hEventShutDownToFinalizer->Wait(INFINITE,FALSE);
            GetFinalizerThread()->DisablePreemptiveGC();

            // We have been asked to quit, so must be shutting down
            _ASSERTE(g_fEEShutDown);
            _ASSERTE(GetFinalizerThread()->PreemptiveGCDisabled());

            if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_FinalizeOnShutdown) != 0)
            {
                // Finalize all registered objects during shutdown, even they are still reachable.
                GCHeapUtilities::GetGCHeap()->SetFinalizeQueueForShutdown(FALSE);

                // This will apply any policy for swallowing exceptions during normal
                // processing, without allowing the finalizer thread to disappear on us.
                ManagedThreadBase::FinalizerBase(FinalizeObjectsOnShutdown);
            }

            _ASSERTE(GetFinalizerThread()->GetDomain()->IsDefaultDomain());

            // we might want to do some extra work on the finalizer thread
            // check and do it
            if (GetFinalizerThread()->HaveExtraWorkForFinalizer())
            {
                GetFinalizerThread()->DoExtraWorkForFinalizer();
            }

            hEventFinalizerToShutDown->Set();

            // Wait for shutdown thread to signal us.
            GetFinalizerThread()->EnablePreemptiveGC();
            hEventShutDownToFinalizer->Wait(INFINITE,FALSE);
            GetFinalizerThread()->DisablePreemptiveGC();

#ifdef FEATURE_COMINTEROP
            // Do extra cleanup for part 1 of shutdown.
            // If we hang here (bug 87809) shutdown thread will
            // timeout on us and will proceed normally
            //
            // We cannot call CoEEShutDownCOM, since the BEGIN_EXTERNAL_ENTRYPOINT
            // will turn our call into a NOP.  We can no longer execute managed
            // code for an external caller.
            InnerCoEEShutDownCOM();
#endif // FEATURE_COMINTEROP

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
    hEventShutDownToFinalizer = new CLREvent();
    hEventShutDownToFinalizer->CreateAutoEvent(FALSE);

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
        BOOL fADUnloadHelper = (pThread && pThread->HasThreadStateNC(Thread::TSNC_ADUnloadHelper));

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
            //WaitForSingleObject(hEventFinalizerDone, INFINITE);

            if (fADUnloadHelper)
            {
                timeout = GetEEPolicy()->GetTimeout(OPR_FinalizerRun);
            }

            DWORD status = hEventFinalizerDone->Wait(timeout,TRUE);
            if (status != WAIT_TIMEOUT && !(g_FinalizerWaiterStatus & FWS_WaitInterrupt))
            {
                return;
            }
            if (!fADUnloadHelper)
            {
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
            else
            {
                if (status == WAIT_TIMEOUT)
                {
                    ULONGLONG finalizeStartTime = GetObjFinalizeStartTime();
                    if (finalizeStartTime)
                    {
                        if (CLRGetTickCount64() >= finalizeStartTime+timeout)
                        {
                            GCX_COOP();
                            FinalizerThreadAbortOnTimeout();
                        }
                    }
                }
                if (endTime != MAXULONGLONG)
                {
                    ULONGLONG curTime = CLRGetTickCount64();
                    if (curTime >= endTime)
                    {
                        return;
                    }
                }
            }
        }
    }
}


#ifdef _DEBUG
#define FINALIZER_WAIT_TIMEOUT 250
#else
#define FINALIZER_WAIT_TIMEOUT 200
#endif
#define FINALIZER_TOTAL_WAIT 2000

static BOOL s_fRaiseExitProcessEvent = FALSE;
static DWORD dwBreakOnFinalizeTimeOut = (DWORD) -1;

static ULONGLONG ShutdownEnd;


BOOL FinalizerThread::FinalizerThreadWatchDog()
{
    Thread *pThread = GetThread();

    if (dwBreakOnFinalizeTimeOut == (DWORD) -1) {
        dwBreakOnFinalizeTimeOut = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_BreakOnFinalizeTimeOut);
    }

    // Do not wait for FinalizerThread if the current one is FinalizerThread.
    if (pThread == GetFinalizerThread())
        return TRUE;

    // If finalizer thread is gone, just return.
    if (GetFinalizerThread()->Join (0, FALSE) != WAIT_TIMEOUT)
        return TRUE;

    // *** This is the first call ShutDown -> Finalizer to Finilize dead objects ***
    if ((g_fEEShutDown & ShutDown_Finalize1) &&
        !(g_fEEShutDown & ShutDown_Finalize2)) {
        ShutdownEnd = CLRGetTickCount64() + GetEEPolicy()->GetTimeout(OPR_ProcessExit);
        // Wait for the finalizer...
        LOG((LF_GC, LL_INFO10, "Signalling finalizer to quit..."));

        fQuitFinalizer = TRUE;
        hEventFinalizerDone->Reset();
        EnableFinalization();

        LOG((LF_GC, LL_INFO10, "Waiting for finalizer to quit..."));
        
        if (pThread)
        {
            pThread->EnablePreemptiveGC();
        }

        BOOL fTimeOut = FinalizerThreadWatchDogHelper();
        
        if (!fTimeOut) {
            hEventShutDownToFinalizer->Set();

            // Wait for finalizer thread to finish raising ExitProcess Event.
            s_fRaiseExitProcessEvent = TRUE;
            fTimeOut = FinalizerThreadWatchDogHelper();
            s_fRaiseExitProcessEvent = FALSE;
        }
        
        if (pThread)
        {
           pThread->DisablePreemptiveGC();
        }
        
        // Can not call ExitProcess here if we are in a hosting environment.
        // The host does not expect that we terminate the process.
        //if (fTimeOut)
        //{
            //::ExitProcess (GetLatchedExitCode());
        //}
        
        return !fTimeOut;
    }

    // *** This is the second call ShutDown -> Finalizer to ***
    // suspend the Runtime and Finilize live objects
    if ( g_fEEShutDown & ShutDown_Finalize2 &&
        !(g_fEEShutDown & ShutDown_COM) ) {
        
#ifdef BACKGROUND_GC
        gc_heap::gc_can_use_concurrent = FALSE;

        if (pGenGCHeap->settings.concurrent)
            pGenGCHeap->background_gc_wait();
#endif //BACKGROUND_GC

        _ASSERTE((g_fEEShutDown & ShutDown_Finalize1) || g_fFastExitProcess);

        if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_FinalizeOnShutdown) != 0)
        {
            // When running finalizers on shutdown (including for reachable objects), suspend threads for shutdown before
            // running finalizers, so that the reachable objects will not be used after they are finalized.

            ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_SHUTDOWN);

            g_fSuspendOnShutdown = TRUE;

            // Do not balance the trap returning threads.
            // We are shutting down CLR.  Only Finalizer/Shutdown threads can
            // return from DisablePreemptiveGC.
            ThreadStore::TrapReturningThreads(TRUE);

            ThreadSuspend::RestartEE(FALSE, TRUE);
        }

        if (g_fFastExitProcess)
        {
            return TRUE;
        }

        // !!! Before we wake up Finalizer thread, we need to enable preemptive gc on the
        // !!! shutdown thread.  Otherwise we may see a deadlock during debug test.
        if (pThread)
        {
            pThread->EnablePreemptiveGC();
        }
        
        GCHeapUtilities::GetGCHeap()->SetFinalizeRunOnShutdown(true);
        
        // Wait for finalizer thread to finish finalizing all objects.
        hEventShutDownToFinalizer->Set();
        BOOL fTimeOut = FinalizerThreadWatchDogHelper();

        if (!fTimeOut) {
            GCHeapUtilities::GetGCHeap()->SetFinalizeRunOnShutdown(false);
        }
        
        // Can not call ExitProcess here if we are in a hosting environment.
        // The host does not expect that we terminate the process.
        //if (fTimeOut) {
        //    ::ExitProcess (GetLatchedExitCode());
        //}

        if (pThread)
        {
        pThread->DisablePreemptiveGC();
        }
        return !fTimeOut;
    }

    // *** This is the third call ShutDown -> Finalizer ***
    // to do additional cleanup
    if (g_fEEShutDown & ShutDown_COM) {
        _ASSERTE (g_fEEShutDown & (ShutDown_Finalize2 | ShutDown_Finalize1));

        if (pThread)
        {
            pThread->EnablePreemptiveGC();
        }

        GCHeapUtilities::GetGCHeap()->SetFinalizeRunOnShutdown(true);
        
        hEventShutDownToFinalizer->Set();
        DWORD status = WAIT_OBJECT_0;
        while (CLREventWaitWithTry(hEventFinalizerToShutDown, FINALIZER_WAIT_TIMEOUT, TRUE, &status))
        {
        }
        
        BOOL fTimeOut = (status == WAIT_TIMEOUT) ? TRUE : FALSE;

        if (fTimeOut) 
        {
            if (dwBreakOnFinalizeTimeOut) {
                LOG((LF_GC, LL_INFO10, "Finalizer took too long to clean up COM IP's.\n"));
                DebugBreak();
            }
        }

        if (pThread)
        {
            pThread->DisablePreemptiveGC();
        }

        return !fTimeOut;
    }

    _ASSERTE(!"Should never reach this point");
    return FALSE;
}

BOOL FinalizerThread::FinalizerThreadWatchDogHelper()
{
    // Since our thread is blocking waiting for the finalizer thread, we must be in preemptive GC
    // so that we don't in turn block the finalizer on us in a GC.
    Thread *pCurrentThread;
    pCurrentThread = GetThread();
    _ASSERTE (pCurrentThread == NULL || !pCurrentThread->PreemptiveGCDisabled());

    // We're monitoring the finalizer thread.
    Thread *pThread = GetFinalizerThread(); 
    _ASSERTE(pThread != pCurrentThread);
    
    ULONGLONG dwBeginTickCount = CLRGetTickCount64();
    
    size_t prevCount;
    size_t curCount;
    BOOL fTimeOut = FALSE;
    DWORD nTry = 0;
    DWORD maxTotalWait = (DWORD)(ShutdownEnd - dwBeginTickCount);
    DWORD totalWaitTimeout;
    totalWaitTimeout = GetEEPolicy()->GetTimeout(OPR_FinalizerRun);
    if (totalWaitTimeout == (DWORD)-1)
    {
        totalWaitTimeout = FINALIZER_TOTAL_WAIT;
    }

    if (s_fRaiseExitProcessEvent)
    {
        DWORD tmp = maxTotalWait/20;  // Normally we assume 2 seconds timeout if total timeout is 40 seconds.
        if (tmp > totalWaitTimeout)
        {
            totalWaitTimeout = tmp;
        }
        prevCount = MAXLONG;
    }
    else
    {
        prevCount = GCHeapUtilities::GetGCHeap()->GetNumberOfFinalizable();
    }

    DWORD maxTry = (DWORD)(totalWaitTimeout*1.0/FINALIZER_WAIT_TIMEOUT + 0.5);
    BOOL bAlertable = TRUE; //(g_fEEShutDown & ShutDown_Finalize2) ? FALSE:TRUE;

    if (dwBreakOnFinalizeTimeOut == (DWORD) -1) {
        dwBreakOnFinalizeTimeOut = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_BreakOnFinalizeTimeOut);
    }

    DWORD dwTimeout = FINALIZER_WAIT_TIMEOUT;

    // This used to set the dwTimeout to infinite, but this can cause a hang when shutting down
    // if a finalizer tries to take a lock that another suspended managed thread already has.
    // This results in the hang because the other managed thread is never going to be resumed
    // because we're in shutdown.  So we make a compromise here - make the timeout for every
    // iteration 10 times longer and make the total wait infinite - so if things hang we will
    // eventually shutdown but we also give things a chance to finish if they're running slower
    // because of the profiler.
#ifdef PROFILING_SUPPORTED
    if (CORProfilerPresent())
    {
        dwTimeout *= 10;
        maxTotalWait = INFINITE;
    }
#endif // PROFILING_SUPPORTED

    // This change was added late in Windows Phone 8, so we want to keep it minimal.
    // We should consider refactoring this later, as we've got a lot of dead code here now on CoreCLR.
    dwTimeout = INFINITE;
    maxTotalWait = INFINITE;

    while (1) {
        struct Param
        {
            DWORD status;
            DWORD dwTimeout;
            BOOL bAlertable;
        } param;
        param.status = 0;
        param.dwTimeout = dwTimeout;
        param.bAlertable = bAlertable;

        PAL_TRY(Param *, pParam, &param)
        {
            pParam->status = hEventFinalizerToShutDown->Wait(pParam->dwTimeout, pParam->bAlertable);
        }
        PAL_EXCEPT (EXCEPTION_EXECUTE_HANDLER)
        {
            param.status = WAIT_TIMEOUT;
        }
        PAL_ENDTRY

        if (param.status != WAIT_TIMEOUT) {
            break;
        }
        nTry ++;
        // ExitProcessEventCount is incremental
        // FinalizableObjects is decremental
        if (s_fRaiseExitProcessEvent)
        {
            curCount = MAXLONG - GetProcessedExitProcessEventCount();
        }
        else
        {
            curCount = GCHeapUtilities::GetGCHeap()->GetNumberOfFinalizable();
        }

        if ((prevCount <= curCount)
            && !GCHeapUtilities::GetGCHeap()->ShouldRestartFinalizerWatchDog()
            && (pThread == NULL || !(pThread->m_State & (Thread::TS_UserSuspendPending | Thread::TS_DebugSuspendPending)))){
            if (nTry == maxTry) {
                if (!s_fRaiseExitProcessEvent) {
                    LOG((LF_GC, LL_INFO10, "Finalizer took too long on one object.\n"));
                }
                else
                    LOG((LF_GC, LL_INFO10, "Finalizer took too long to process ExitProcess event.\n"));

                fTimeOut = TRUE;
                if (dwBreakOnFinalizeTimeOut != 2) {
                    break;
                }
            }
        }
        else
        {
            nTry = 0;
            prevCount = curCount;
        }
        ULONGLONG dwCurTickCount = CLRGetTickCount64();
        if (pThread && pThread->m_State & (Thread::TS_UserSuspendPending | Thread::TS_DebugSuspendPending)) {
            // CoreCLR does not support user-requested thread suspension
            _ASSERTE(!(pThread->m_State & Thread::TS_UserSuspendPending));
            dwBeginTickCount = dwCurTickCount;
        }
        if (dwCurTickCount - dwBeginTickCount >= maxTotalWait)
        {
            LOG((LF_GC, LL_INFO10, "Finalizer took too long on shutdown.\n"));
            fTimeOut = TRUE;
            if (dwBreakOnFinalizeTimeOut != 2) {
                break;
            }
        }
    }

    if (fTimeOut)
    {
        if (dwBreakOnFinalizeTimeOut){
            DebugBreak();
        }
    }

    return fTimeOut;
}
