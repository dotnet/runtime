// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: nativeeventsource.h
// Abstract: This module implements native part of Event Source support in VM
//

//

//
// ============================================================================
#ifndef _NATIVEEVENTSOURCE_H_
#define _NATIVEEVENTSOURCE_H_

#include "qcall.h"

#if defined(FEATURE_PERFTRACING)
class XplatEventSourceLogger
{
public:
    static void QCALLTYPE LogEventSource(__in_z int eventID, __in_z LPCWSTR eventName, __in_z LPCWSTR eventSourceName, __in_z LPCWSTR payload);
    static BOOL QCALLTYPE IsEventSourceLoggingEnabled();
};
#endif //defined(FEATURE_PERFTRACING)

#if defined(FEATURE_PERFTRACING)
class NativeEventLogger
{
public:
    static void QCALLTYPE LogThreadPoolWorkerThreadStart(__in_z uint activeWorkerThreadCount, __in_z uint retiredWorkerThreadCount, __in_z short clrInstanceID);
    static void QCALLTYPE LogThreadPoolWorkerThreadStop(__in_z uint activeWorkerThreadCount, __in_z uint retiredWorkerThreadCount, __in_z short clrInstanceID);
    static void QCALLTYPE LogThreadPoolWorkerThreadWait(__in_z uint activeWorkerThreadCount, __in_z uint retiredWorkerThreadCount, __in_z short clrInstanceID);
    static void QCALLTYPE LogThreadPoolWorkerThreadAdjustmentSample(__in_z double throughput, __in_z short clrInstanceID);
    static void QCALLTYPE LogThreadPoolWorkerThreadAdjustmentAdjustment(__in_z double averageThroughput, __in_z uint newWorkerThreadCount, __in_z uint reason, __in_z short clrInstanceID);
    static void QCALLTYPE LogThreadPoolWorkerThreadAdjustmentStats(__in_z double duration, __in_z double throughput, __in_z double threadWave, __in_z double throughputWave, __in_z double throughputErrorEstimate, __in_z double AverageThroughputErrorEstimate, __in_z double ThroughputRatio, __in_z double confidence, __in_z double newControlSetting, __in_z short newThreadWaveMagnitude, __in_z short ClrInstanceID);
    static void QCALLTYPE LogThreadPoolIOEnqueue(__in_z void* nativeOverlapped, __in_z void* overlapped, __in_z bool multiDequeues, __in_z short ClrInstanceID);
    static void QCALLTYPE LogThreadPoolIODequeue(__in_z void* nativeOverlapped, __in_z void* overlapped, __in_z short ClrInstanceID);
    static void QCALLTYPE LogThreadPoolWorkingThreadCount(__in_z uint count, __in_z short ClrInstanceID);
};
#endif // defined(FEATURE_PERFTRACING)

#endif //_NATIVEEVENTSOURCE_H_
