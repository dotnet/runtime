// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_PROFILERAPI_H__
#define __EVENTPIPE_PROFILERAPI_H__

#ifdef FEATURE_PERFTRACING

#ifdef PROFILING_SUPPORTED

#include "eeprofinterfaces.h"
#include "eventpipe.h"
#include "eventpipeeventinstance.h"

class EventPipeProfilerApi
{
    public:
        EventPipeProfilerApi() {};
        ~EventPipeProfilerApi() {};

        // Write an event to the profiler api.
        void WriteEvent(EventPipeEventInstance &instance);
};

#endif // PROFILING_SUPPORTED

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_PROFILERAPI_H__
