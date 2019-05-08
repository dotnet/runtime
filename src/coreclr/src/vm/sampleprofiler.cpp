// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipebuffermanager.h"
#include "eventpipeeventinstance.h"
#include "sampleprofiler.h"
#include "hosting.h"
#include "threadsuspend.h"

#ifdef FEATURE_PERFTRACING

#ifndef FEATURE_PAL
#include <mmsystem.h>
#endif //FEATURE_PAL

const unsigned long NUM_NANOSECONDS_IN_1_MS = 1000000;

Volatile<BOOL> SampleProfiler::s_profilingEnabled = false;
Thread* SampleProfiler::s_pSamplingThread = NULL;
const WCHAR* SampleProfiler::s_providerName = W("Microsoft-DotNETCore-SampleProfiler");
EventPipeProvider* SampleProfiler::s_pEventPipeProvider = NULL;
EventPipeEvent* SampleProfiler::s_pThreadTimeEvent = NULL;
BYTE* SampleProfiler::s_pPayloadExternal = NULL;
BYTE* SampleProfiler::s_pPayloadManaged = NULL;
CLREventStatic SampleProfiler::s_threadShutdownEvent;
unsigned long SampleProfiler::s_samplingRateInNs = NUM_NANOSECONDS_IN_1_MS; // 1ms
bool SampleProfiler::s_timePeriodIsSet = FALSE;

#ifndef FEATURE_PAL
PVOID SampleProfiler::s_timeBeginPeriodFn = NULL;
PVOID SampleProfiler::s_timeEndPeriodFn = NULL;
HINSTANCE SampleProfiler::s_hMultimediaLib = NULL;

typedef MMRESULT (WINAPI *TimePeriodFnPtr) (UINT uPeriod);
#endif //FEATURE_PAL

void SampleProfiler::Enable(EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue)
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

    LoadDependencies();

    if(s_pEventPipeProvider == NULL)
    {
        s_pEventPipeProvider = EventPipe::CreateProvider(SL(s_providerName), nullptr, nullptr, pEventPipeProviderCallbackDataQueue);
        s_pThreadTimeEvent = s_pEventPipeProvider->AddEvent(
            0, /* eventID */
            0, /* keywords */
            0, /* eventVersion */
            EventPipeEventLevel::Informational,
            false /* NeedStack */);
    }

    // Check to see if the sample profiler event is enabled.  If it is not, do not spin up the sampling thread.
    if(!s_pThreadTimeEvent->IsEnabled())
    {
        return;
    }

    if(s_pPayloadExternal == NULL)
    {
        s_pPayloadExternal = new BYTE[sizeof(unsigned int)];
        *((unsigned int *)s_pPayloadExternal) = static_cast<unsigned int>(SampleProfilerSampleType::External);

        s_pPayloadManaged = new BYTE[sizeof(unsigned int)];
        *((unsigned int *)s_pPayloadManaged) = static_cast<unsigned int>(SampleProfilerSampleType::Managed);
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

    s_threadShutdownEvent.CreateManualEvent(FALSE);

    SetTimeGranularity();
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

    // If g_fProcessDetach is true, the sampling profiler thread probably got
    // ripped because someone called ExitProcess(). This check is an attempt 
    // to recognize that case and avoid waiting for the (already dead) sampling
    // profiler thread to tell us it is terminated.
    if (!g_fProcessDetach)
    {
        // Reset the event before shutdown.
        s_threadShutdownEvent.Reset();

        // The sampling thread will watch this value and exit
        // when profiling is disabled.
        s_profilingEnabled = false;

        // Wait for the sampling thread to clean itself up.
        s_threadShutdownEvent.Wait(INFINITE, FALSE /* bAlertable */);
        s_threadShutdownEvent.CloseEvent();
    }

    if(s_timePeriodIsSet)
    {
        ResetTimeGranularity();
    }
    UnloadDependencies();
}

void SampleProfiler::SetSamplingRate(unsigned long nanoseconds)
{
    LIMITED_METHOD_CONTRACT;

    // If the time period setting was modified by us,
    // make sure to change it back before changing our period
    // and losing track of what we set it to
    if(s_timePeriodIsSet)
    {
        ResetTimeGranularity();
    }

    s_samplingRateInNs = nanoseconds;

    if(!s_timePeriodIsSet)
    {
        SetTimeGranularity();
    }
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
                PlatformSleep(s_samplingRateInNs);
                continue;
            }

            // Actually suspend managed execution.
            ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_REASON::SUSPEND_OTHER);

            // Walk all managed threads and capture stacks.
            WalkManagedThreads();

            // Resume managed execution.
            ThreadSuspend::RestartEE(FALSE /* bFinishedGC */, TRUE /* SuspendSucceeded */);

            // Wait until it's time to sample again.
            PlatformSleep(s_samplingRateInNs);
        }
    }

    // Destroy the sampling thread when it is done running.
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

    Thread *pTargetThread = NULL;

    // Iterate over all managed threads.
    // Assumes that the ThreadStoreLock is held because we've suspended all threads.
    while ((pTargetThread = ThreadStore::GetThreadList(pTargetThread)) != NULL)
    {
        StackContents stackContents;

        // Walk the stack and write it out as an event.
        if(EventPipe::WalkManagedStackForThread(pTargetThread, stackContents) && !stackContents.IsEmpty())
        {
            // Set the payload.  If the GC mode on suspension > 0, then the thread was in cooperative mode.
            // Even though there are some cases where this is not managed code, we assume it is managed code here.
            // If the GC mode on suspension == 0 then the thread was in preemptive mode, which we qualify as external here.
            BYTE *pPayload = s_pPayloadExternal;
            if(pTargetThread->GetGCModeOnSuspension())
            {
                pPayload = s_pPayloadManaged;
            }

            // Write the sample.
            EventPipe::WriteSampleProfileEvent(s_pSamplingThread, s_pThreadTimeEvent, pTargetThread, stackContents, pPayload, c_payloadSize);
        }

        // Reset the GC mode.
        pTargetThread->ClearGCModeOnSuspension();
    }
}

void SampleProfiler::PlatformSleep(unsigned long nanoseconds)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_PAL
    PAL_nanosleep(nanoseconds);
#else //FEATURE_PAL
    ClrSleepEx(s_samplingRateInNs / NUM_NANOSECONDS_IN_1_MS, FALSE);
#endif //FEATURE_PAL
}

void SampleProfiler::SetTimeGranularity()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef FEATURE_PAL
    // Attempt to set the systems minimum timer period to the sampling rate
    // If the sampling rate is lower than the current system setting (16ms by default),
    // this will cause the OS to wake more often for scheduling descsion, allowing us to take samples
    // Note that is effects a system-wide setting and when set low will increase the amount of time
    // the OS is on-CPU, decreasing overall system performance and increasing power consumption
    if(s_timeBeginPeriodFn != NULL)
    {
        if(((TimePeriodFnPtr) s_timeBeginPeriodFn)(s_samplingRateInNs / NUM_NANOSECONDS_IN_1_MS) == TIMERR_NOERROR)
        {
            s_timePeriodIsSet = TRUE;
        }
    }
#endif //FEATURE_PAL
}

void SampleProfiler::ResetTimeGranularity()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef FEATURE_PAL
    // End the modifications we had to the timer period in Enable
    if(s_timeEndPeriodFn != NULL)
    {
        if(((TimePeriodFnPtr) s_timeEndPeriodFn)(s_samplingRateInNs / NUM_NANOSECONDS_IN_1_MS) == TIMERR_NOERROR)
        {
            s_timePeriodIsSet = FALSE;
        }
    }
#endif //FEATURE_PAL
}

bool SampleProfiler::LoadDependencies()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef FEATURE_PAL
    s_hMultimediaLib = WszLoadLibrary(W("winmm.dll"));

    if (s_hMultimediaLib != NULL)
    {
        s_timeBeginPeriodFn = (PVOID) GetProcAddress(s_hMultimediaLib, "timeBeginPeriod");
        s_timeEndPeriodFn = (PVOID) GetProcAddress(s_hMultimediaLib, "timeEndPeriod");
    }

    return s_hMultimediaLib != NULL && s_timeBeginPeriodFn != NULL && s_timeEndPeriodFn != NULL;
#else
    return FALSE;
#endif //FEATURE_PAL
}

void SampleProfiler::UnloadDependencies()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef FEATURE_PAL
    if (s_hMultimediaLib != NULL)
    {
        FreeLibrary(s_hMultimediaLib);
        s_hMultimediaLib = NULL;
        s_timeBeginPeriodFn = NULL;
        s_timeEndPeriodFn = NULL;
    }
#endif //FEATURE_PAL
}

#endif // FEATURE_PERFTRACING
