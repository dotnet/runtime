// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "eventpipeadapter.h"

#ifdef FEATURE_PERFTRACING

#include "gcenv.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "SpinLock.h"

struct EventPipeEventInstanceData;

struct EventPipeSessionInfo;

EXTERN_C NATIVEAOT_API uint64_t __cdecl RhEventPipeInternal_Enable(
    LPCWSTR outputFile,
    EventPipeSerializationFormat format,
    uint32_t circularBufferSizeInMB,
    /* COR_PRF_EVENTPIPE_PROVIDER_CONFIG */ const void * pProviders,
    uint32_t numProviders)
{
    PalDebugBreak();
    return 0;
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_Disable(uint64_t sessionID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API intptr_t __cdecl RhEventPipeInternal_CreateProvider(
    LPCWSTR providerName,
    EventPipeCallback pCallbackFunc,
    void* pCallbackContext)
{
    return 0;
}

EXTERN_C NATIVEAOT_API intptr_t __cdecl RhEventPipeInternal_DefineEvent(
    intptr_t provHandle,
    uint32_t eventID,
    int64_t keywords,
    uint32_t eventVersion,
    uint32_t level,
    void *pMetadata,
    uint32_t metadataLength)
{
    return 0;
}

EXTERN_C NATIVEAOT_API intptr_t __cdecl RhEventPipeInternal_GetProvider(LPCWSTR providerName)
{
    PalDebugBreak();
    return 0;
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_DeleteProvider(intptr_t provHandle)
{
}

EXTERN_C NATIVEAOT_API int __cdecl RhEventPipeInternal_EventActivityIdControl(uint32_t controlCode, GUID *pActivityId)
{
    PalDebugBreak();
    return 0;
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_WriteEventData(
    intptr_t eventHandle,
    EventData *pEventData,
    uint32_t eventDataCount,
    const GUID * pActivityId,
    const GUID * pRelatedActivityId)
{
}

EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhEventPipeInternal_GetSessionInfo(uint64_t sessionID, EventPipeSessionInfo *pSessionInfo)
{
    PalDebugBreak();
    return FALSE;
}

EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhEventPipeInternal_GetNextEvent(uint64_t sessionID, EventPipeEventInstanceData *pInstance)
{
    PalDebugBreak();
    return FALSE;
}

EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhEventPipeInternal_SignalSession(uint64_t sessionID)
{
    PalDebugBreak();
    return FALSE;
}

EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhEventPipeInternal_WaitForSessionSignal(uint64_t sessionID, int32_t timeoutMs)
{
    PalDebugBreak();
    return FALSE;
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogContentionLockCreated(intptr_t LockID, intptr_t AssociatedObjectID, uint16_t ClrInstanceID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogContentionStart(uint16_t ContentionFlags, uint16_t ClrInstanceID, intptr_t LockID, intptr_t AssociatedObjectID, uint64_t LockOwnerThreadID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogContentionStop(uint16_t ContentionFlags, uint16_t ClrInstanceID, double DurationNs)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkerThreadStart(uint32_t activeWorkerThreadCount, uint32_t retiredWorkerThreadCount, uint16_t clrInstanceID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkerThreadStop(uint32_t ActiveWorkerThreadCount, uint32_t RetiredWorkerThreadCount, uint16_t ClrInstanceID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkerThreadWait(uint32_t ActiveWorkerThreadCount, uint32_t RetiredWorkerThreadCount, uint16_t ClrInstanceID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolMinMaxThreads(uint16_t MinWorkerThreads, uint16_t MaxWorkerThreads, uint16_t MinIOCompletionThreads, uint16_t MaxIOCompletionThreads, uint16_t ClrInstanceID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkerThreadAdjustmentSample(double Throughput, uint16_t ClrInstanceID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkerThreadAdjustmentAdjustment(double AverageThroughput, uint32_t NewWorkerThreadCount, uint32_t Reason, uint16_t ClrInstanceID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkerThreadAdjustmentStats(
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
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolIOEnqueue(
    uint32_t * NativeOverlapped,
    uint32_t * Overlapped,
    bool MultiDequeues,
    uint16_t ClrInstanceID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolIODequeue(uint32_t * NativeOverlapped, uint32_t * Overlapped, uint16_t ClrInstanceID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkingThreadCount(uint32_t Count, uint16_t ClrInstanceID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolIOPack(uint32_t * NativeOverlapped, uint32_t * Overlapped, uint16_t ClrInstanceID)
{
    PalDebugBreak();
}

EXTERN_C NATIVEAOT_API void __cdecl RhpEtwExceptionThrown(LPCWSTR exceptionTypeName, LPCWSTR exceptionMessage, void* faultingIP, HRESULT hresult)
{
    PalDebugBreak();
}

#endif // FEATURE_PERFTRACING
