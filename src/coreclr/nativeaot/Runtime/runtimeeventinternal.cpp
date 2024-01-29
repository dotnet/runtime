// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "gcenv.h"

#ifdef FEATURE_PERFTRACING

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogContentionLockCreated(intptr_t LockID, intptr_t AssociatedObjectID, uint16_t ClrInstanceID)
{
    FireEtwContentionLockCreated(reinterpret_cast<const void*>(LockID), reinterpret_cast<const void*>(AssociatedObjectID), ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogContentionStart(uint8_t ContentionFlags, uint16_t ClrInstanceID, intptr_t LockID, intptr_t AssociatedObjectID, uint64_t LockOwnerThreadID)
{
    FireEtwContentionStart_V2((const unsigned char)(ContentionFlags), ClrInstanceID, reinterpret_cast<const void*>(LockID), reinterpret_cast<const void*>(AssociatedObjectID), LockOwnerThreadID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogContentionStop(uint8_t ContentionFlags, uint16_t ClrInstanceID, double DurationNs)
{
    FireEtwContentionStop_V1((const unsigned char)(ContentionFlags), ClrInstanceID, DurationNs);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkerThreadStart(uint32_t activeWorkerThreadCount, uint32_t retiredWorkerThreadCount, uint16_t clrInstanceID)
{
    FireEtwThreadPoolWorkerThreadStart(activeWorkerThreadCount, retiredWorkerThreadCount, clrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkerThreadStop(uint32_t ActiveWorkerThreadCount, uint32_t RetiredWorkerThreadCount, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolWorkerThreadStop(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkerThreadWait(uint32_t ActiveWorkerThreadCount, uint32_t RetiredWorkerThreadCount, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolWorkerThreadWait(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolMinMaxThreads(uint16_t MinWorkerThreads, uint16_t MaxWorkerThreads, uint16_t MinIOCompletionThreads, uint16_t MaxIOCompletionThreads, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolMinMaxThreads(MinWorkerThreads, MaxWorkerThreads, MinIOCompletionThreads, MaxIOCompletionThreads, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentSample(double Throughput, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolWorkerThreadAdjustmentSample(Throughput, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentAdjustment(double AverageThroughput, uint32_t NewWorkerThreadCount, uint32_t Reason, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolWorkerThreadAdjustmentAdjustment(AverageThroughput, NewWorkerThreadCount, Reason, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentStats(
    double Duration,
    double Throughput,
    double ThreadPoolWorkerThreadWait,
    double ThroughputWave,
    double ThroughputErrorEstimate,
    double AverageThroughputErrorEstimate,
    double ThroughputRatio,
    double Confidence,
    double NewControlSetting,
    uint16_t NewThreadWaveMagnitude,
    uint16_t ClrInstanceID)
{
    FireEtwThreadPoolWorkerThreadAdjustmentStats(Duration, Throughput, ThreadPoolWorkerThreadWait, ThroughputWave, ThroughputErrorEstimate, AverageThroughputErrorEstimate, ThroughputRatio, Confidence, NewControlSetting, NewThreadWaveMagnitude, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolIOEnqueue(
    void * NativeOverlapped,
    void * Overlapped,
    BOOL MultiDequeues,
    uint16_t ClrInstanceID)
{
    FireEtwThreadPoolIOEnqueue(NativeOverlapped, Overlapped, MultiDequeues, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolIODequeue(void * NativeOverlapped, void * Overlapped, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolIODequeue(NativeOverlapped, Overlapped, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkingThreadCount(uint32_t Count, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolWorkingThreadCount(Count, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolIOPack(void * NativeOverlapped, void * Overlapped, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolIOPack(NativeOverlapped, Overlapped, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogExceptionThrown(const WCHAR* exceptionTypeName, const WCHAR* exceptionMessage, void* faultingIP, HRESULT hresult)
{
    FireEtwExceptionThrown_V1(exceptionTypeName,
        exceptionMessage,
        faultingIP,
        hresult,
        0,
        GetClrInstanceId());
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogWaitHandleWaitStart(uint8_t WaitSource, intptr_t AssociatedObjectID, uint16_t ClrInstanceID)
{
    FireEtwWaitHandleWaitStart(WaitSource, reinterpret_cast<const void*>(AssociatedObjectID), ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogWaitHandleWaitStop(uint16_t ClrInstanceID)
{
    FireEtwWaitHandleWaitStop(ClrInstanceID);
}

#endif // FEATURE_PERFTRACING
