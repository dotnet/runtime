// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: nativeeventsource.cpp
// Abstract: This module implements native part of Event Source support in VM
//
//
//
// ============================================================================

#include "common.h"
#include "nativeeventsource.h"

#if defined(FEATURE_EVENTSOURCE_XPLAT)

extern "C" void QCALLTYPE LogEventSource(_In_z_ int eventID, _In_z_ LPCWSTR eventName, _In_z_ LPCWSTR eventSourceName, _In_z_ LPCWSTR payload)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;
    FireEtwEventSource(eventID, eventName, eventSourceName, payload);
    END_QCALL;
}

extern "C" BOOL QCALLTYPE IsEventSourceLoggingEnabled()
{
    QCALL_CONTRACT;

    BOOL retVal = FALSE;

    BEGIN_QCALL;
    retVal = XplatEventLogger::IsEventLoggingEnabled();
    END_QCALL;

    return retVal;
}

#endif //defined(FEATURE_EVENTSOURCE_XPLAT)

#ifdef FEATURE_PERFTRACING

// These are native QCalls that call into corresponding FireEtw* events for events that want to be emitted from the managed
// side using NativeRuntimeEventSource.
// You need to add them to src/libraries/System.Private.CoreLib/src/System/Diagnostics/Tracing/XplatEventLogger.cs and
// change genRuntimeEventSources.py script to not emit the body that throws NotImplementedException for the event that
// want to be fired from managed code.
// See https://github.com/dotnet/runtime/pull/47829 for an example of how to do this.
extern "C" void QCALLTYPE LogThreadPoolWorkerThreadStart(_In_z_ uint activeWorkerThreadCount, _In_z_ uint retiredWorkerThreadCount, _In_z_ short clrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkerThreadStart(activeWorkerThreadCount, retiredWorkerThreadCount, clrInstanceID);

    END_QCALL;
}

extern "C" void QCALLTYPE LogThreadPoolWorkerThreadStop(_In_z_ uint activeWorkerThreadCount, _In_z_ uint retiredWorkerThreadCount, _In_z_ short clrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkerThreadStop(activeWorkerThreadCount, retiredWorkerThreadCount, clrInstanceID);

    END_QCALL;
}

extern "C" void QCALLTYPE LogThreadPoolWorkerThreadWait(_In_z_ uint activeWorkerThreadCount, _In_z_ uint retiredWorkerThreadCount, _In_z_ short clrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkerThreadWait(activeWorkerThreadCount, retiredWorkerThreadCount, clrInstanceID);

    END_QCALL;
}

extern "C" void QCALLTYPE LogThreadPoolWorkerThreadAdjustmentSample(_In_z_ double throughput, _In_z_ short clrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkerThreadAdjustmentSample(throughput, clrInstanceID);

    END_QCALL;
}

extern "C" void QCALLTYPE LogThreadPoolWorkerThreadAdjustmentAdjustment(_In_z_ double averageThroughput, _In_z_ uint newWorkerThreadCount, _In_z_ uint reason, _In_z_ short clrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkerThreadAdjustmentAdjustment(averageThroughput, newWorkerThreadCount, reason, clrInstanceID);

    END_QCALL;
}

extern "C" void QCALLTYPE LogThreadPoolWorkerThreadAdjustmentStats(_In_z_ double duration, _In_z_ double throughput, _In_z_ double threadWave, _In_z_ double throughputWave, _In_z_ double throughputErrorEstimate, _In_z_ double AverageThroughputErrorEstimate, _In_z_ double ThroughputRatio, _In_z_ double confidence, _In_z_ double newControlSetting, _In_z_ short newThreadWaveMagnitude, _In_z_ short ClrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkerThreadAdjustmentStats(duration, throughput, threadWave, throughputWave, throughputErrorEstimate, AverageThroughputErrorEstimate, ThroughputRatio, confidence, newControlSetting, newThreadWaveMagnitude, ClrInstanceID);

    END_QCALL;
}

extern "C" void QCALLTYPE LogThreadPoolIOEnqueue(_In_z_ void* nativeOverlapped, _In_z_ void* overlapped, _In_z_ bool multiDequeues, _In_z_ short ClrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolIOEnqueue(nativeOverlapped, overlapped, multiDequeues, ClrInstanceID);

    END_QCALL;
}

extern "C" void QCALLTYPE LogThreadPoolIODequeue(_In_z_ void* nativeOverlapped, _In_z_ void* overlapped, _In_z_ short ClrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolIODequeue(nativeOverlapped, overlapped, ClrInstanceID);

    END_QCALL;
}

extern "C" void QCALLTYPE LogThreadPoolWorkingThreadCount(_In_z_ uint count, _In_z_ short ClrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkingThreadCount(count, ClrInstanceID);

    END_QCALL;
}

extern "C" void QCALLTYPE LogThreadPoolIOPack(_In_z_ void* nativeOverlapped, _In_z_ void* overlapped, _In_z_ short ClrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolIOPack(nativeOverlapped, overlapped, ClrInstanceID);

    END_QCALL;
}
#endif // FEATURE_PERFTRACING
