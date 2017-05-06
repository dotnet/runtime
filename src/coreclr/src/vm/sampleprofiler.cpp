// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipeeventinstance.h"
#include "sampleprofiler.h"
#include "hosting.h"
#include "threadsuspend.h"

#ifdef FEATURE_PERFTRACING

Volatile<BOOL> SampleProfiler::s_profilingEnabled = false;
Thread* SampleProfiler::s_pSamplingThread = NULL;
const GUID SampleProfiler::s_providerID = {0x3c530d44,0x97ae,0x513a,{0x1e,0x6d,0x78,0x3e,0x8f,0x8e,0x03,0xa9}}; // {3c530d44-97ae-513a-1e6d-783e8f8e03a9}
EventPipeProvider* SampleProfiler::s_pEventPipeProvider = NULL;
EventPipeEvent* SampleProfiler::s_pThreadTimeEvent = NULL;
CLREventStatic SampleProfiler::s_threadShutdownEvent;
#ifdef FEATURE_PAL
long SampleProfiler::s_samplingRateInNs = 1000000; // 1ms
#endif

void SampleProfiler::Enable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(s_pSamplingThread == NULL);
        // Synchronization of multiple callers occurs in EventPipe::Enable.
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    if(s_pEventPipeProvider == NULL)
    {
        s_pEventPipeProvider = new EventPipeProvider(s_providerID);
        s_pThreadTimeEvent = s_pEventPipeProvider->AddEvent(
            0, /* keywords */
            0, /* eventID */
            0, /* eventVersion */
            EventPipeEventLevel::Informational,
            false /* NeedStack */);
    }

    s_profilingEnabled = true;
    s_pSamplingThread = SetupUnstartedThread();
    if(s_pSamplingThread->CreateNewThread(0, ThreadProc, NULL))
    {
        // Start the sampling thread.
        s_pSamplingThread->SetBackground(TRUE);
        s_pSamplingThread->StartThread();
    }
    else
    {
        _ASSERT(!"Unable to create sample profiler thread.");
    }
}

void SampleProfiler::Disable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        // Synchronization of multiple callers occurs in EventPipe::Disable.
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    // Bail early if profiling is not enabled.
    if(!s_profilingEnabled)
    {
        return;
    }

    // Reset the event before shutdown.
    s_threadShutdownEvent.Reset();

    // The sampling thread will watch this value and exit
    // when profiling is disabled.
    s_profilingEnabled = false;

    // Wait for the sampling thread to clean itself up.
    s_threadShutdownEvent.Wait(0, FALSE /* bAlertable */);
}

DWORD WINAPI SampleProfiler::ThreadProc(void *args)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(s_pSamplingThread != NULL);
    }
    CONTRACTL_END;

    // Complete thread initialization and start the profiling loop.
    if(s_pSamplingThread->HasStarted())
    {
        // Switch to pre-emptive mode so that this thread doesn't starve the GC.
        GCX_PREEMP();

        while(s_profilingEnabled)
        {
            // Check to see if we can suspend managed execution.
            if(ThreadSuspend::SysIsSuspendInProgress() || (ThreadSuspend::GetSuspensionThread() != 0))
            {
                // Skip the current sample.
#ifdef FEATURE_PAL
                PAL_nanosleep(s_samplingRateInNs);
#else
                ClrSleepEx(1, FALSE);
#endif
                continue;
            }

            // Actually suspend managed execution.
            ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_REASON::SUSPEND_OTHER);

            // Walk all managed threads and capture stacks.
            WalkManagedThreads();

            // Resume managed execution.
            ThreadSuspend::RestartEE(FALSE /* bFinishedGC */, TRUE /* SuspendSucceeded */);

            // Wait until it's time to sample again.
#ifdef FEATURE_PAL
            PAL_nanosleep(s_samplingRateInNs);
#else
            ClrSleepEx(1, FALSE);
#endif
        }
    }

    // Destroy the sampling thread when done running.
    DestroyThread(s_pSamplingThread);
    s_pSamplingThread = NULL;

    // Signal Disable() that the thread has been destroyed.
    s_threadShutdownEvent.Set();
 
    return S_OK;
}

// The thread store lock must already be held by the thread before this function
// is called.  ThreadSuspend::SuspendEE acquires the thread store lock.
void SampleProfiler::WalkManagedThreads()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    Thread *pThread = NULL;

    // Iterate over all managed threads.
    // Assumes that the ThreadStoreLock is held because we've suspended all threads.
    while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
    {
        SampleProfilerEventInstance instance(pThread);
        StackContents &stackContents = *(instance.GetStack());

        // Walk the stack and write it out as an event.
        if(EventPipe::WalkManagedStackForThread(pThread, stackContents) && !stackContents.IsEmpty())
        {
            EventPipe::WriteSampleProfileEvent(instance);
        }
    }
}

#endif // FEATURE_PERFTRACING
