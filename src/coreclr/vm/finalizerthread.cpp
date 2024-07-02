// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================

#include "common.h"

#include "finalizerthread.h"
#include "threadsuspend.h"
#include "jithost.h"
#include "genanalysis.h"
#include "eventpipeadapter.h"

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

    return GetThreadNULLOk() == g_pFinalizerThread;
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

static void CallFinalizerOnThreadObject(OBJECTREF obj)
{
    STATIC_CONTRACT_MODE_COOPERATIVE;

    THREADBASEREF   refThis = (THREADBASEREF)obj;
    Thread*         thread  = refThis->GetInternal();

    // Prevent multiple calls to Finalize
    // Objects can be resurrected after being finalized.  However, there is no
    // race condition here.  We always check whether an exposed thread object is
    // still attached to the internal Thread object, before proceeding.
    if (thread)
    {
        refThis->ResetStartHelper();

        // During process shutdown, we finalize even reachable objects.  But if we break
        // the link between the System.Thread and the internal Thread object, the runtime
        // may not work correctly.  In particular, we won't be able to transition between
        // contexts and domains to finalize other objects.  Since the runtime doesn't
        // require that Threads finalize during shutdown, we need to disable this.  If
        // we wait until phase 2 of shutdown finalization (when the EE is suspended and
        // will never resume) then we can simply skip the side effects of Thread
        // finalization.
        if ((g_fEEShutDown & ShutDown_Finalize2) == 0)
        {
            if (GetThreadNULLOk() != thread)
            {
                refThis->ClearInternal();
            }

            thread->SetThreadState(Thread::TS_Finalized);
            Thread::SetCleanupNeededForFinalizedThread();
        }
    }
}

OBJECTREF FinalizerThread::GetNextFinalizableObject()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

Again:
    if (fQuitFinalizer)
        return NULL;

    OBJECTREF obj = ObjectToOBJECTREF(GCHeapUtilities::GetGCHeap()->GetNextFinalizable());
    if (obj == NULL)
        return NULL;

    MethodTable     *pMT = obj->GetMethodTable();
    STRESS_LOG2(LF_GC, LL_INFO1000, "Finalizing object %p MT %pT\n", OBJECTREFToObject(obj), pMT);
    LOG((LF_GC, LL_INFO1000, "Finalizing " LOG_OBJECT_CLASS(OBJECTREFToObject(obj))));

    if ((obj->GetHeader()->GetBits()) & BIT_SBLK_FINALIZER_RUN)
    {
        //reset the bit so the object can be put on the list
        //with RegisterForFinalization
        obj->GetHeader()->ClrBit (BIT_SBLK_FINALIZER_RUN);
        goto Again;
    }

    _ASSERTE(pMT->HasFinalizer());

#ifdef FEATURE_EVENT_TRACE
    ETW::GCLog::SendFinalizeObjectEvent(pMT, OBJECTREFToObject(obj));
#endif // FEATURE_EVENT_TRACE

    // Check for precise init class constructors that have failed, if any have failed, then we didn't run the
    // constructor for the object, and running the finalizer for the object would violate the CLI spec by running
    // instance code without having successfully run the precise-init class constructor.
    if (pMT->HasPreciseInitCctors())
    {
        MethodTable *pMTCur = pMT;
        do
        {
            if ((!pMTCur->GetClass()->IsBeforeFieldInit()) && pMTCur->IsInitError())
            {
                // Precise init Type Initializer for type failed... do not run finalizer
                goto Again;
            }

            pMTCur = pMTCur->GetParentMethodTable();
        }
        while (pMTCur != NULL);
    }
  
    if (pMT == g_pThreadClass)
    {
        // Finalizing Thread object requires ThreadStoreLock.  It is expensive if
        // we keep taking ThreadStoreLock.  This is very bad if we have high retiring
        // rate of Thread objects.
        // To avoid taking ThreadStoreLock multiple times, we mark Thread with TS_Finalized
        // and clean up a batch of them when we take ThreadStoreLock next time.

        // To avoid possible hierarchy requirement between critical finalizers, we call cleanup
        // code directly.
        CallFinalizerOnThreadObject(obj);
        goto Again;
    }

    return obj;
}

void FinalizerThread::FinalizeAllObjects()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    FireEtwGCFinalizersBegin_V1(GetClrInstanceId());

    PREPARE_NONVIRTUAL_CALLSITE(METHOD__GC__RUN_FINALIZERS);
    DECLARE_ARGHOLDER_ARRAY(args, 0);

    uint32_t count;
    CALL_MANAGED_METHOD(count, uint32_t, args);

    FireEtwGCFinalizersEnd_V1(count, GetClrInstanceId());
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
static BOOL s_InitializedFinalizerThreadForPlatform = FALSE;

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
        SignalFinalizationDone();
#endif //0

        WaitForFinalizerEvent (hEventFinalizer);

        // Process pending finalizer work items from the GC first.
        FinalizerWorkItem* pWork = GCHeapUtilities::GetGCHeap()->GetExtraWorkForFinalization();
        while (pWork != NULL)
        {
            FinalizerWorkItem* pNext = pWork->next;
            pWork->callback(pWork);
            pWork = pNext;
        }

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
        if (gcGenAnalysisState == GcGenAnalysisState::Done)
        {
            gcGenAnalysisState = GcGenAnalysisState::Disabled;
            if (gcGenAnalysisTrace)
            {
                EventPipeAdapter::Disable(gcGenAnalysisEventPipeSessionId);
#ifdef GEN_ANALYSIS_STRESS
                GenAnalysis::EnableGenerationalAwareSession();
#endif
            }

            // Writing an empty file to indicate completion
            WCHAR outputPath[MAX_PATH];
            ReplacePid(GENAWARE_COMPLETION_FILE_NAME, outputPath, MAX_PATH);
            fclose(_wfopen(outputPath, W("w+")));
        }

        if (!bPriorityBoosted)
        {
            if (GetFinalizerThread()->SetThreadPriority(THREAD_PRIORITY_HIGHEST))
                bPriorityBoosted = TRUE;
        }

        // The Finalizer thread is started very early in EE startup. We deferred
        // some initialization until a point we are sure the EE is up and running. At
        // this point we make a single attempt and if it fails won't try again.
        if (!s_InitializedFinalizerThreadForPlatform)
        {
            s_InitializedFinalizerThreadForPlatform = TRUE;
            Thread::InitializationForManagedThreadInNative(GetFinalizerThread());
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
                // If no GCs happened, then we assume we are quiescent
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

        FinalizeAllObjects();

        // Anyone waiting to drain the Q can now wake up.  Note that there is a
        // race in that another thread starting a drain, as we leave a drain, may
        // consider itself satisfied by the drain that just completed.  This is
        // acceptable.
        SignalFinalizationDone();
    }

    if (s_InitializedFinalizerThreadForPlatform)
        Thread::CleanUpForManagedThreadInNative(GetFinalizerThread());
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

#if defined(FEATURE_COMINTEROP_APARTMENT_SUPPORT) && !defined(FEATURE_COMINTEROP)
    // Make sure the finalizer thread is set to MTA to avoid hitting
    // DevDiv Bugs 180773 - [Stress Failure] AV at CoreCLR!SafeQueryInterfaceHelper
    GetFinalizerThread()->SetApartment(Thread::AS_InMTA);
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT && !FEATURE_COMINTEROP

    s_FinalizerThreadOK = GetFinalizerThread()->HasStarted();

    _ASSERTE(s_FinalizerThreadOK);
    _ASSERTE(GetThread() == GetFinalizerThread());

    // finalizer should always park in default domain

    if (s_FinalizerThreadOK)
    {
        INSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP;
        {
            GetFinalizerThread()->SetBackground(TRUE);

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
        }
        UNINSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP;
    }

    LOG((LF_GC, LL_INFO10, "Finalizer thread done."));

    // Enable pre-emptive GC before we leave so that anybody trying to suspend
    // us will not end up waiting forever. Don't do a DestroyThread because this
    // will happen soon when we tear down the thread store.
    GetFinalizerThread()->EnablePreemptiveGC();

    // We do not want to tear Finalizer thread,
    // since doing so will cause OLE32 to CoUninitialize.
    while (1)
    {
        __SwitchToThread(INFINITE, CALLER_LIMITS_SPINNING);
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

#ifndef TARGET_UNIX
    MHandles[kLowMemoryNotification] =
        CreateMemoryResourceNotification(LowMemoryResourceNotification);
#endif // TARGET_UNIX

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

void FinalizerThread::SignalFinalizationDone()
{
    WRAPPER_NO_CONTRACT;

    hEventFinalizerDone->Set();
}

// Wait for the finalizer thread to complete one pass.
void FinalizerThread::FinalizerThreadWait()
{
    ASSERT(hEventFinalizerDone->IsValid());
    ASSERT(hEventFinalizer->IsValid());
    ASSERT(GetFinalizerThread());

    // Can't call this from within a finalized method.
    if (!IsCurrentThreadFinalizer())
    {
        GCX_PREEMP();

#ifdef FEATURE_COMINTEROP
        // To help combat finalizer thread starvation, we check to see if there are any wrappers
        // scheduled to be cleaned up for our context.  If so, we'll do them here to avoid making
        // the finalizer thread do a transition.
        if (g_pRCWCleanupList != NULL)
            g_pRCWCleanupList->CleanupWrappersInCurrentCtxThread();
#endif // FEATURE_COMINTEROP

        hEventFinalizerDone->Reset();

        EnableFinalization();

        hEventFinalizerDone->Wait(INFINITE,TRUE);
    }
}
