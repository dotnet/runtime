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
#if defined(FEATURE_EVENTSOURCE_XPLAT)
#include "nativeeventsource.h"

void QCALLTYPE XplatEventSourceLogger::LogEventSource(__in_z int eventID, __in_z LPCWSTR eventName, __in_z LPCWSTR eventSourceName, __in_z LPCWSTR payload)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;
    FireEtwEventSource(eventID, eventName, eventSourceName, payload);
    END_QCALL;
}

BOOL QCALLTYPE XplatEventSourceLogger::IsEventSourceLoggingEnabled()
{
    QCALL_CONTRACT;

    BOOL retVal = FALSE;

    BEGIN_QCALL;
    retVal = XplatEventLogger::IsEventLoggingEnabled();
    END_QCALL;

    return retVal;
}

void QCALLTYPE XplatEventSourceLogger::LogThreadPoolWorkerThreadStart(__in_z uint activeWorkerThreadCount, __in_z uint retiredWorkerThreadCount, __in_z short clrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkerThreadStart(activeWorkerThreadCount, retiredWorkerThreadCount, clrInstanceID);

    END_QCALL;
}

void QCALLTYPE XplatEventSourceLogger::LogThreadPoolWorkerThreadStop(__in_z uint activeWorkerThreadCount, __in_z uint retiredWorkerThreadCount, __in_z short clrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkerThreadStop(activeWorkerThreadCount, retiredWorkerThreadCount, clrInstanceID);

    END_QCALL;
}

void QCALLTYPE XplatEventSourceLogger::LogThreadPoolWorkerThreadWait(__in_z uint activeWorkerThreadCount, __in_z uint retiredWorkerThreadCount, __in_z short clrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkerThreadWait(activeWorkerThreadCount, retiredWorkerThreadCount, clrInstanceID);

    END_QCALL;
}

void QCALLTYPE XplatEventSourceLogger::LogThreadPoolWorkerThreadAdjustmentSample(__in_z double throughput, __in_z short clrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkerThreadAdjustmentSample(throughput, clrInstanceID);

    END_QCALL;
}

void QCALLTYPE XplatEventSourceLogger::LogThreadPoolWorkerThreadAdjustmentAdjustment(__in_z double averageThroughput, __in_z uint newWorkerThreadCount, __in_z uint reason, __in_z short clrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkerThreadAdjustmentAdjustment(averageThroughput, newWorkerThreadCount, reason, clrInstanceID);
    
    END_QCALL;
}

void QCALLTYPE XplatEventSourceLogger::LogThreadPoolWorkerThreadAdjustmentStats(__in_z double duration, __in_z double throughput, __in_z double threadWave, __in_z double throughputWave, __in_z double throughputErrorEstimate, __in_z double AverageThroughputErrorEstimate, __in_z double ThroughputRatio, __in_z double confidence, __in_z double newControlSetting, __in_z short newThreadWaveMagnitude, __in_z double ClrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkerThreadAdjustmentStats(duration, throughput, threadWave, throughputWave, throughputErrorEstimate, AverageThroughputErrorEstimate, ThroughputRatio, confidence, newControlSetting, newThreadWaveMagnitude, ClrInstanceID);

    END_QCALL;
}

void QCALLTYPE XplatEventSourceLogger::LogThreadPoolIOEnqueue(__in_z void* nativeOverlapped, __in_z void* overlapped, __in_z bool multiDequeues, __in_z short ClrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolIOEnqueue(nativeOverlapped, overlapped, multiDequeues, ClrInstanceID);

    END_QCALL;
}

void QCALLTYPE XplatEventSourceLogger::LogThreadPoolIODequeue(__in_z void* nativeOverlapped, __in_z void* overlapped, __in_z short ClrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolIODequeue(nativeOverlapped, overlapped, ClrInstanceID);

    END_QCALL;
}

void QCALLTYPE XplatEventSourceLogger::LogThreadPoolWorkingThreadCount(__in_z uint count, __in_z short ClrInstanceID)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    FireEtwThreadPoolWorkingThreadCount(count, ClrInstanceID);

    END_QCALL;
}

#endif //defined(FEATURE_EVENTSOURCE_XPLAT)
