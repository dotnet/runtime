// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "eventpipeadapter.h"
#include "eventpipeinternal.h"

#ifdef TARGET_UNIX
#include "pal.h"
#endif // TARGET_UNIX

#ifdef FEATURE_PERFTRACING

UINT64 QCALLTYPE EventPipeInternal::Enable(
    __in_z LPCWSTR outputFile,
    EventPipeSerializationFormat format,
    UINT32 circularBufferSizeInMB,
    /* COR_PRF_EVENTPIPE_PROVIDER_CONFIG */ LPCVOID pProviders,
    UINT32 numProviders)
{
    QCALL_CONTRACT;

    UINT64 sessionID = 0;

    // Invalid input!
    if (circularBufferSizeInMB == 0 ||
        format >= EP_SERIALIZATION_FORMAT_COUNT ||
        numProviders == 0 ||
        pProviders == nullptr)
    {
        return 0;
    }

    BEGIN_QCALL;
    {
        EventPipeProviderConfigurationAdapter configAdapter(reinterpret_cast<const COR_PRF_EVENTPIPE_PROVIDER_CONFIG *>(pProviders), numProviders);
        sessionID = EventPipeAdapter::Enable(
            outputFile,
            circularBufferSizeInMB,
            configAdapter,
            outputFile != NULL ? EP_SESSION_TYPE_FILE : EP_SESSION_TYPE_LISTENER,
            format,
            true,
            nullptr,
            nullptr);
        EventPipeAdapter::StartStreaming(sessionID);
    }
    END_QCALL;

    return sessionID;
}

void QCALLTYPE EventPipeInternal::Disable(UINT64 sessionID)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    EventPipeAdapter::Disable(sessionID);
    END_QCALL;
}

bool QCALLTYPE EventPipeInternal::GetSessionInfo(UINT64 sessionID, EventPipeSessionInfo *pSessionInfo)
{
    QCALL_CONTRACT;

    bool retVal = false;
    BEGIN_QCALL;

    if (pSessionInfo != NULL)
    {
        EventPipeSession *pSession = EventPipeAdapter::GetSession(sessionID);
        if (pSession != NULL)
        {
            pSessionInfo->StartTimeAsUTCFileTime = EventPipeAdapter::GetSessionStartTime(pSession);
            pSessionInfo->StartTimeStamp.QuadPart = EventPipeAdapter::GetSessionStartTimestamp(pSession);
            QueryPerformanceFrequency(&pSessionInfo->TimeStampFrequency);
            retVal = true;
        }
    }

    END_QCALL;
    return retVal;
}

INT_PTR QCALLTYPE EventPipeInternal::CreateProvider(
    __in_z LPCWSTR providerName,
    EventPipeCallback pCallbackFunc)
{
    QCALL_CONTRACT;

    EventPipeProvider *pProvider = NULL;

    BEGIN_QCALL;

    pProvider = EventPipeAdapter::CreateProvider(providerName, pCallbackFunc);

    END_QCALL;

    return reinterpret_cast<INT_PTR>(pProvider);
}

INT_PTR QCALLTYPE EventPipeInternal::DefineEvent(
    INT_PTR provHandle,
    UINT32 eventID,
    __int64 keywords,
    UINT32 eventVersion,
    UINT32 level,
    void *pMetadata,
    UINT32 metadataLength)
{
    QCALL_CONTRACT;

    EventPipeEvent *pEvent = NULL;

    BEGIN_QCALL;

    _ASSERTE(provHandle != NULL);
    EventPipeProvider *pProvider = reinterpret_cast<EventPipeProvider *>(provHandle);
    pEvent = EventPipeAdapter::AddEvent(pProvider, eventID, keywords, eventVersion, (EventPipeEventLevel)level, /* needStack = */ true, (BYTE *)pMetadata, metadataLength);
    _ASSERTE(pEvent != NULL);

    END_QCALL;

    return reinterpret_cast<INT_PTR>(pEvent);
}

INT_PTR QCALLTYPE EventPipeInternal::GetProvider(__in_z LPCWSTR providerName)
{
    QCALL_CONTRACT;

    EventPipeProvider *pProvider = NULL;

    BEGIN_QCALL;

    pProvider = EventPipeAdapter::GetProvider(providerName);

    END_QCALL;

    return reinterpret_cast<INT_PTR>(pProvider);
}

void QCALLTYPE EventPipeInternal::DeleteProvider(INT_PTR provHandle)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    if (provHandle != NULL)
    {
        EventPipeProvider *pProvider = reinterpret_cast<EventPipeProvider *>(provHandle);
        EventPipeAdapter::DeleteProvider(pProvider);
    }

    END_QCALL;
}

int QCALLTYPE EventPipeInternal::EventActivityIdControl(uint32_t controlCode, GUID *pActivityId)
{

    QCALL_CONTRACT;

    int retVal = 0;

    BEGIN_QCALL;

    Thread *pThread = GetThreadNULLOk();
    if (pThread == NULL || pActivityId == NULL)
    {
        retVal = 1;
    }
    else
    {
        ActivityControlCode activityControlCode = (ActivityControlCode)controlCode;
        GUID currentActivityId;
        switch (activityControlCode)
        {
        case ActivityControlCode::EVENT_ACTIVITY_CONTROL_GET_ID:

            *pActivityId = *pThread->GetActivityId();
            break;

        case ActivityControlCode::EVENT_ACTIVITY_CONTROL_SET_ID:

            pThread->SetActivityId(pActivityId);
            break;

        case ActivityControlCode::EVENT_ACTIVITY_CONTROL_CREATE_ID:

            CoCreateGuid(pActivityId);
            break;

        case ActivityControlCode::EVENT_ACTIVITY_CONTROL_GET_SET_ID:

            currentActivityId = *pThread->GetActivityId();
            pThread->SetActivityId(pActivityId);
            *pActivityId = currentActivityId;
            break;

        case ActivityControlCode::EVENT_ACTIVITY_CONTROL_CREATE_SET_ID:

            *pActivityId = *pThread->GetActivityId();
            CoCreateGuid(&currentActivityId);
            pThread->SetActivityId(&currentActivityId);
            break;

        default:
            retVal = 1;
        }
    }

    END_QCALL;
    return retVal;
}

void QCALLTYPE EventPipeInternal::WriteEventData(
    INT_PTR eventHandle,
    EventData *pEventData,
    UINT32 eventDataCount,
    LPCGUID pActivityId,
    LPCGUID pRelatedActivityId)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    _ASSERTE(eventHandle != NULL);
    EventPipeEvent *pEvent = reinterpret_cast<EventPipeEvent *>(eventHandle);
    EventPipeAdapter::WriteEvent(pEvent, pEventData, eventDataCount, pActivityId, pRelatedActivityId);

    END_QCALL;
}

bool QCALLTYPE EventPipeInternal::GetNextEvent(UINT64 sessionID, EventPipeEventInstanceData *pInstance)
{
    QCALL_CONTRACT;

    EventPipeEventInstance *pNextInstance = NULL;
    BEGIN_QCALL;

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

    END_QCALL;
    return pNextInstance != NULL;
}

HANDLE QCALLTYPE EventPipeInternal::GetWaitHandle(UINT64 sessionID)
{
    QCALL_CONTRACT;

    HANDLE waitHandle;
    BEGIN_QCALL;

    waitHandle = EventPipeAdapter::GetWaitHandle(sessionID);

    END_QCALL;
    return waitHandle;
}

#endif // FEATURE_PERFTRACING
