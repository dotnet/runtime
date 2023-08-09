// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "PalRedhawk.h"

#ifdef FEATURE_PERFTRACING

// We will do a no-op for events in the disabled EventPipe This is similar to the way eventpipe checks if the provider and an event is enabled before firting the event, and no-op otherwise.

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogContentionLockCreated(intptr_t LockID, intptr_t AssociatedObjectID, uint16_t ClrInstanceID)
{
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogContentionStart(uint8_t ContentionFlags, uint16_t ClrInstanceID, intptr_t LockID, intptr_t AssociatedObjectID, uint64_t LockOwnerThreadID)
{
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogContentionStop(uint8_t ContentionFlags, uint16_t ClrInstanceID, double DurationNs)
{
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkerThreadStart(uint32_t activeWorkerThreadCount, uint32_t retiredWorkerThreadCount, uint16_t clrInstanceID)
{
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkerThreadStop(uint32_t ActiveWorkerThreadCount, uint32_t RetiredWorkerThreadCount, uint16_t ClrInstanceID)
{
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkerThreadWait(uint32_t ActiveWorkerThreadCount, uint32_t RetiredWorkerThreadCount, uint16_t ClrInstanceID)
{
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolMinMaxThreads(uint16_t MinWorkerThreads, uint16_t MaxWorkerThreads, uint16_t MinIOCompletionThreads, uint16_t MaxIOCompletionThreads, uint16_t ClrInstanceID)
{
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentSample(double Throughput, uint16_t ClrInstanceID)
{
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentAdjustment(double AverageThroughput, uint32_t NewWorkerThreadCount, uint32_t Reason, uint16_t ClrInstanceID)
{
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
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolIOEnqueue(
    void * NativeOverlapped,
    void * Overlapped,
    bool MultiDequeues,
    uint16_t ClrInstanceID)
{
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolIODequeue(void * NativeOverlapped, void * Overlapped, uint16_t ClrInstanceID)
{
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolWorkingThreadCount(uint32_t Count, uint16_t ClrInstanceID)
{
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogThreadPoolIOPack(void * NativeOverlapped, void * Overlapped, uint16_t ClrInstanceID)
{
}

EXTERN_C NATIVEAOT_API void __cdecl NativeRuntimeEventSource_LogExceptionThrown(const WCHAR* exceptionTypeName, const WCHAR* exceptionMessage, void* faultingIP, HRESULT hresult)
{
}

#endif // FEATURE_PERFTRACING
