// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __SAMPLEPROFILER_H__
#define __SAMPLEPROFILER_H__

#ifdef FEATURE_PERFTRACING

#include "common.h"
#include "eventpipe.h"

enum class SampleProfilerSampleType
{
    Error = 0,
    External = 1,
    Managed = 2
};

class SampleProfiler
{

    // Declare friends.
    friend class EventPipe;
    friend class SampleProfilerEventInstance;

    public:

        // Enable profiling.
        static void Enable();

        // Disable profiling.
        static void Disable();

        // Set the sampling rate.
        static void SetSamplingRate(long nanoseconds);

    private:

        // Iterate through all managed threads and walk all stacks.
        static void WalkManagedThreads();

        // Profiling thread proc.  Invoked on a new thread when profiling is enabled.
        static DWORD WINAPI ThreadProc(void *args);

        // True when profiling is enabled.
        static Volatile<BOOL> s_profilingEnabled;

        // The sampling thread.
        static Thread *s_pSamplingThread;

        // The provider and event emitted by the profiler.
        static const GUID s_providerID;
        static EventPipeProvider *s_pEventPipeProvider;
        static EventPipeEvent *s_pThreadTimeEvent;

        // Event payloads.
        // External represents a sample in external or native code.
        // Managed represents a sample in managed code.
        static BYTE *s_pPayloadExternal;
        static BYTE *s_pPayloadManaged;
        static const unsigned int c_payloadSize = sizeof(unsigned int);

        // Thread shutdown event for synchronization between Disable() and the sampling thread.
        static CLREventStatic s_threadShutdownEvent;

        // The sampling rate.
        static long s_samplingRateInNs;
};

#endif // FEATURE_PERFTRACING

#endif // __SAMPLEPROFILER_H__
