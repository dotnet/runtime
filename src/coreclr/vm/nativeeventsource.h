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

#if defined(FEATURE_EVENTSOURCE_XPLAT)
extern "C" void QCALLTYPE LogEventSource(_In_z_ int eventID, _In_z_ LPCWSTR eventName, _In_z_ LPCWSTR eventSourceName, _In_z_ LPCWSTR payload);
extern "C" BOOL QCALLTYPE IsEventSourceLoggingEnabled();
extern "C" LPWSTR QCALLTYPE EventSource_GetClrConfig(LPCWSTR configName);
#endif //defined(FEATURE_EVENTSOURCE_XPLAT)

#if defined(FEATURE_PERFTRACING)
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogThreadPoolWorkerThreadStart(_In_z_ uint activeWorkerThreadCount, _In_z_ uint retiredWorkerThreadCount, _In_z_ short clrInstanceID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogThreadPoolWorkerThreadStop(_In_z_ uint activeWorkerThreadCount, _In_z_ uint retiredWorkerThreadCount, _In_z_ short clrInstanceID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogThreadPoolWorkerThreadWait(_In_z_ uint activeWorkerThreadCount, _In_z_ uint retiredWorkerThreadCount, _In_z_ short clrInstanceID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogThreadPoolMinMaxThreads(_In_z_ short minWorkerThreads, _In_z_ short maxWorkerThreads, _In_z_ short minIOCompletionThreads, _In_z_ short maxIOCompletionThreads, _In_z_ short clrInstanceID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentSample(_In_z_ double throughput, _In_z_ short clrInstanceID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentAdjustment(_In_z_ double averageThroughput, _In_z_ uint newWorkerThreadCount, _In_z_ uint reason, _In_z_ short clrInstanceID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentStats(_In_z_ double duration, _In_z_ double throughput, _In_z_ double threadWave, _In_z_ double throughputWave, _In_z_ double throughputErrorEstimate, _In_z_ double AverageThroughputErrorEstimate, _In_z_ double ThroughputRatio, _In_z_ double confidence, _In_z_ double newControlSetting, _In_z_ short newThreadWaveMagnitude, _In_z_ short ClrInstanceID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogThreadPoolIOEnqueue(_In_z_ void* nativeOverlapped, _In_z_ void* overlapped, _In_z_ bool multiDequeues, _In_z_ short ClrInstanceID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogThreadPoolIODequeue(_In_z_ void* nativeOverlapped, _In_z_ void* overlapped, _In_z_ short ClrInstanceID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogThreadPoolWorkingThreadCount(_In_z_ uint count, _In_z_ short ClrInstanceID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogThreadPoolIOPack(_In_z_ void* nativeOverlapped, _In_z_ void* overlapped, _In_z_ short ClrInstanceID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogContentionLockCreated(void* LockID, void* AssociatedObjectID, uint16_t ClrInstanceID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogContentionStart(uint8_t ContentionFlags, uint16_t ClrInstanceID, void* LockID, void* AssociatedObjectID, uint64_t LockOwnerThreadID);
extern "C" void QCALLTYPE NativeRuntimeEventSource_LogContentionStop(uint8_t ContentionFlags, uint16_t ClrInstanceID, double DurationNs);
#endif // defined(FEATURE_PERFTRACING)

#endif //_NATIVEEVENTSOURCE_H_
