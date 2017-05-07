// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipeprofilerapi.h"

#ifdef FEATURE_PERFTRACING

#ifdef PROFILING_SUPPORTED

void EventPipeProfilerApi::WriteEvent(EventPipeEventInstance &instance)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    auto event = instance.GetEvent();
    auto stackContents = instance.GetStack();

    BEGIN_PIN_PROFILER(CORProfilerIsMonitoringEventPipe());
    g_profControlBlock.pProfInterface->EventPipeEventDelivered(
        event->GetProvider()->GetProviderID(),
        event->GetEventID(),
        event->GetEventVersion(),
        instance.GetThreadId(),
        instance.GetTimestamp(),
        instance.GetLength(),
        instance.GetData(),
        stackContents->GetLength(),
        reinterpret_cast<UINT_PTR*>(stackContents->GetPointer()));
    END_PIN_PROFILER();
}

#endif // PROFILING_SUPPORTED

#endif // FEATURE_PERFTRACING
