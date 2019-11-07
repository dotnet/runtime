// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __SAMPLEPROFILER_H__
#define __SAMPLEPROFILER_H__

#ifdef FEATURE_PERFTRACING

#include "common.h"
#include "eventpipe.h"

enum class SampleProfilerSampleType : uint32_t
{
    Error = 0,
    External = 1,
    Managed = 2
};

class SampleProfiler
{
    // Declare friends.
    friend class EventPipe;

public:
    // Initialize the sample profiler.
    static void Initialize(EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue);

    // Enable profiling.
    static void Enable(EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue);

    // Disable profiling.
    static void Disable();

    // Set the sampling rate.
    static void SetSamplingRate(unsigned long nanoseconds);

    static unsigned long GetSamplingRate()
    {
        LIMITED_METHOD_CONTRACT;
        return s_samplingRateInNs;
    }

private:
    union SampleProfilerPayload
    {
        SampleProfilerSampleType Type;
        BYTE Rawdata[sizeof(SampleProfilerSampleType)];
    };

    // Iterate through all managed threads and walk all stacks.
    static void WalkManagedThreads();

    // Profiling thread proc.  Invoked on a new thread when profiling is enabled.
    static DWORD WINAPI ThreadProc(void *args);

    // Calls either PAL_nanosleep or ClrSleepEx depending on platform
    // Note: Although we specify the time in ns, that is no indication
    // of the actually accuracy with which we will return from sleep
    // In reality Unix will have a minimum granularity of ~10ms
    // and Windows has a default granularity of ~16ms, but can be
    // adjusted to as low as ~1ms
    // Even this however is not gaurenteed. If the system is under load
    // the sampling thread may be delayed up to hundreds of ms due to
    // scheduling priority. There is no way to prevent this from user threads
    // Additionally we may get lucky and there will be an open CPU to run
    // and under light load the timings will achieve great accuracy!
    static void PlatformSleep(unsigned long nanoseconds);

    static bool LoadDependencies();
    static void UnloadDependencies();

#ifndef FEATURE_PAL
    static HINSTANCE s_hMultimediaLib;
    static PVOID s_timeBeginPeriodFn;
    static PVOID s_timeEndPeriodFn;
#endif //FEATURE_PAL

    static void SetTimeGranularity();
    static void ResetTimeGranularity();

    // True when profiling is enabled.
    static Volatile<BOOL> s_profilingEnabled;

    // The sampling thread.
    static Thread *s_pSamplingThread;

    // The provider and event emitted by the profiler.
    static const WCHAR *s_providerName;
    static EventPipeProvider *s_pEventPipeProvider;
    static EventPipeEvent *s_pThreadTimeEvent;

    // Event payloads.
    // External represents a sample in external or native code.
    // Managed represents a sample in managed code.
    static SampleProfilerPayload s_ExternalPayload;
    static SampleProfilerPayload s_ManagedPayload;
    static const unsigned int c_payloadSize = sizeof(unsigned int);

    // Thread shutdown event for synchronization between Disable() and the sampling thread.
    static CLREventStatic s_threadShutdownEvent;

    // The sampling rate.
    static unsigned long s_samplingRateInNs;

    // Whether or not timeBeginPeriod has been used to set the scheduler period
    static bool s_timePeriodIsSet;

    static int32_t s_RefCount;
};

#endif // FEATURE_PERFTRACING

#endif // __SAMPLEPROFILER_H__
