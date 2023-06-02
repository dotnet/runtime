// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "eventpipeadapter.h"

#include "gcenv.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "SpinLock.h"

#ifdef FEATURE_PERFTRACING

struct EventPipeEventInstanceData
{
    void *ProviderID;
    unsigned int EventID;
    unsigned int ThreadID;
    LARGE_INTEGER TimeStamp;
    GUID ActivityId;
    GUID RelatedActivityId;
    const uint8_t *Payload;
    unsigned int PayloadLength;
};

struct EventPipeSessionInfo
{
    FILETIME StartTimeAsUTCFileTime;
    LARGE_INTEGER StartTimeStamp;
    LARGE_INTEGER TimeStampFrequency;
};

EXTERN_C NATIVEAOT_API uint64_t __cdecl RhEventPipeInternal_Enable(
    LPCWSTR outputFile,
    EventPipeSerializationFormat format,
    uint32_t circularBufferSizeInMB,
    /* COR_PRF_EVENTPIPE_PROVIDER_CONFIG */ const void * pProviders,
    uint32_t numProviders)
{
    uint64_t sessionID = 0;
    // Invalid input!
    if (circularBufferSizeInMB == 0 ||
        format >= EP_SERIALIZATION_FORMAT_COUNT ||
        numProviders == 0 ||
        pProviders == nullptr)
    {
        return 0;
    }

    EventPipeProviderConfigurationAdapter configAdapter(reinterpret_cast<const COR_PRF_EVENTPIPE_PROVIDER_CONFIG *>(pProviders), numProviders);

    sessionID = EventPipeAdapter::Enable(
        outputFile,
        circularBufferSizeInMB,
        configAdapter,
        outputFile != NULL ? EP_SESSION_TYPE_FILE : EP_SESSION_TYPE_LISTENER,
        format,
        true,
        nullptr,
        nullptr,
        nullptr);
    EventPipeAdapter::StartStreaming(sessionID);

    return sessionID;
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_Disable(uint64_t sessionID)
{
    EventPipeAdapter::Disable(sessionID);
}

EXTERN_C NATIVEAOT_API intptr_t __cdecl RhEventPipeInternal_CreateProvider(
    LPCWSTR providerName,
    EventPipeCallback pCallbackFunc,
    void* pCallbackContext)
{
    EventPipeProvider* pProvider = EventPipeAdapter::CreateProvider(providerName, pCallbackFunc, pCallbackContext);
    return reinterpret_cast<intptr_t>(pProvider);
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
    EventPipeEvent *pEvent = NULL;

    _ASSERTE(provHandle != 0);
    EventPipeProvider *pProvider = reinterpret_cast<EventPipeProvider *>(provHandle);
    pEvent = EventPipeAdapter::AddEvent(pProvider, eventID, keywords, eventVersion, (EventPipeEventLevel)level, /* needStack = */ true, (uint8_t *)pMetadata, metadataLength);
    _ASSERTE(pEvent != NULL);

    return reinterpret_cast<intptr_t>(pEvent);
}

EXTERN_C NATIVEAOT_API intptr_t __cdecl RhEventPipeInternal_GetProvider(LPCWSTR providerName)
{
    EventPipeProvider* pProvider = EventPipeAdapter::GetProvider(providerName);
    return reinterpret_cast<intptr_t>(pProvider);
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_DeleteProvider(intptr_t provHandle)
{
    if (provHandle != 0)
    {
        EventPipeProvider *pProvider = reinterpret_cast<EventPipeProvider *>(provHandle);
        EventPipeAdapter::DeleteProvider(pProvider);
    }
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
    _ASSERTE(eventHandle != 0);
    EventPipeEvent *pEvent = reinterpret_cast<EventPipeEvent *>(eventHandle);
    EventPipeAdapter::WriteEvent(pEvent, pEventData, eventDataCount, pActivityId, pRelatedActivityId);
}

EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhEventPipeInternal_GetSessionInfo(uint64_t sessionID, EventPipeSessionInfo *pSessionInfo)
{
    bool retVal = false;
    if (pSessionInfo != NULL)
    {
        EventPipeSession *pSession = EventPipeAdapter::GetSession(sessionID);
        if (pSession != NULL)
        {
            pSessionInfo->StartTimeAsUTCFileTime = EventPipeAdapter::GetSessionStartTime(pSession);
            pSessionInfo->StartTimeStamp.QuadPart = EventPipeAdapter::GetSessionStartTimestamp(pSession);
            // @TODO
            //pSessionInfo->TimeStampFrequency = reinterpret_cast<LARGE_INTEGER>(PalQueryPerformanceFrequency());
            retVal = true;
        }
    }
    return retVal;
}

EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhEventPipeInternal_GetNextEvent(uint64_t sessionID, EventPipeEventInstanceData *pInstance)
{
    EventPipeEventInstance *pNextInstance = NULL;
    _ASSERTE(pInstance != NULL);

    pNextInstance = EventPipeAdapter::GetNextEvent(sessionID);
    if (pNextInstance)
    {
        pInstance->ProviderID = EventPipeAdapter::GetEventProvider(pNextInstance);
        pInstance->EventID = EventPipeAdapter::GetEventID(pNextInstance);
        pInstance->ThreadID = static_cast<uint32_t>(EventPipeAdapter::GetEventThreadID(pNextInstance));
        pInstance->TimeStamp.QuadPart = EventPipeAdapter::GetEventTimestamp(pNextInstance);
        pInstance->ActivityId = *EventPipeAdapter::GetEventActivityID(pNextInstance);
        pInstance->RelatedActivityId = *EventPipeAdapter::GetEventRelativeActivityID(pNextInstance);
        pInstance->Payload = EventPipeAdapter::GetEventData(pNextInstance);
        pInstance->PayloadLength = EventPipeAdapter::GetEventDataLen(pNextInstance);
    }

    return pNextInstance != NULL;
}

EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhEventPipeInternal_SignalSession(uint64_t sessionID)
{
    return EventPipeAdapter::SignalSession(sessionID);
}

EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhEventPipeInternal_WaitForSessionSignal(uint64_t sessionID, int32_t timeoutMs)
{
    return EventPipeAdapter::WaitForSessionSignal(sessionID, timeoutMs);
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkerThreadStart(uint32_t activeWorkerThreadCount, uint32_t retiredWorkerThreadCount, uint16_t clrInstanceID)
{
    FireEtwThreadPoolWorkerThreadStart(activeWorkerThreadCount, retiredWorkerThreadCount, clrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkerThreadStop(uint32_t ActiveWorkerThreadCount, uint32_t RetiredWorkerThreadCount, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolWorkerThreadStop(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkerThreadWait(uint32_t ActiveWorkerThreadCount, uint32_t RetiredWorkerThreadCount, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolWorkerThreadWait(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolMinMaxThreads(uint16_t MinWorkerThreads, uint16_t MaxWorkerThreads, uint16_t MinIOCompletionThreads, uint16_t MaxIOCompletionThreads, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolMinMaxThreads(MinWorkerThreads, MaxWorkerThreads, MinIOCompletionThreads, MaxIOCompletionThreads, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkerThreadAdjustmentSample(double Throughput, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolWorkerThreadAdjustmentSample(Throughput, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkerThreadAdjustmentAdjustment(double AverageThroughput, uint32_t NewWorkerThreadCount, uint32_t Reason, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolWorkerThreadAdjustmentAdjustment(AverageThroughput, NewWorkerThreadCount, Reason, ClrInstanceID);
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
    FireEtwThreadPoolWorkerThreadAdjustmentStats(Duration, Throughput, ThreadPoolWorkerThreadWait, ThroughputWave, ThroughputErrorEstimate, AverageThroughputErrorEstimate, ThroughputRatio, Confidence, NewControlSetting, NewThreadWaveMagnitude, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolIOEnqueue(
    uint32_t * NativeOverlapped,
    uint32_t * Overlapped,
    bool MultiDequeues,
    uint16_t ClrInstanceID)
{
    FireEtwThreadPoolIOEnqueue(NativeOverlapped, Overlapped, MultiDequeues, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolIODequeue(uint32_t * NativeOverlapped, uint32_t * Overlapped, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolIODequeue(NativeOverlapped, Overlapped, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolWorkingThreadCount(uint32_t Count, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolWorkingThreadCount(Count, ClrInstanceID);
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_LogThreadPoolIOPack(uint32_t * NativeOverlapped, uint32_t * Overlapped, uint16_t ClrInstanceID)
{
    FireEtwThreadPoolIOPack(NativeOverlapped, Overlapped, ClrInstanceID);
}

#endif // FEATURE_PERFTRACING
