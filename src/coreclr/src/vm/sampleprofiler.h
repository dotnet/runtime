// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __SAMPLEPROFILER_H__
#define __SAMPLEPROFILER_H__

#include "common.h"
#include "eventpipe.h"

class SampleProfiler
{
    public:

        // Enable profiling.
        static void Enable();

        // Disable profiling.
        static void Disable();

    private:

        // Iterate through all managed threads and walk all stacks.
        static void WalkManagedThreads();

        // Profiling thread proc.  Invoked on a new thread when profiling is enabled.
        static DWORD WINAPI ThreadProc(void *args);

        // True when profiling is enabled.
        static Volatile<BOOL> s_profilingEnabled;

        // The sampling thread.
        static Thread *s_pSamplingThread;

        // Thread shutdown event for synchronization between Disable() and the sampling thread.
        static CLREventStatic s_threadShutdownEvent;

#ifdef FEATURE_PAL
        // The sampling rate.
        static long s_samplingRateInNs;
#endif
};

#endif // __SAMPLEPROFILER_H__
