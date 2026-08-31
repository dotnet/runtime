// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "eventpipeadapter.h"
#include "eventpipeinternal.h"

#ifdef TARGET_UNIX
#include "pal.h"
#endif // TARGET_UNIX

#include <minipal/guid.h>
#include <minipal/time.h>

#ifdef FEATURE_PERFTRACING

extern "C" QCallExceptionStatus QCALLTYPE EventPipeInternal_Enable(
    _In_z_ LPCWSTR outputFile,
    EventPipeSerializationFormat format,
    UINT32 circularBufferSizeInMB,
    /* COR_PRF_EVENTPIPE_PROVIDER_CONFIG */ LPCVOID pProviders,
    UINT32 numProviders, UINT64* pReturnValue)
{
    QCALL_CONTRACT;

    UINT64 sessionID = 0;

    // Invalid input!
    if (circularBufferSizeInMB == 0 ||
        format >= EP_SERIALIZATION_FORMAT_COUNT ||
        numProviders == 0 ||
        pProviders == nullptr)
    {
        *pReturnValue = 0;
        return QCallExceptionStatus();
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
            nullptr,
            nullptr);
        if (sessionID != 0)
        {
            EventPipeAdapter::StartStreaming(sessionID);
        }
    }
    *pReturnValue = sessionID;

    END_QCALL;
}

extern "C" QCallExceptionStatus QCALLTYPE EventPipeInternal_Disable(UINT64 sessionID)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    EventPipeAdapter::Disable(sessionID);
    END_QCALL;
}

extern "C" QCallExceptionStatus QCALLTYPE EventPipeInternal_GetSessionInfo(UINT64 sessionID, EventPipeSessionInfo *pSessionInfo, BOOL* pReturnValue)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    bool retVal = false;

    if (pSessionInfo != NULL)
    {
        EventPipeSession *pSession = EventPipeAdapter::GetSession(sessionID);
        if (pSession != NULL)
        {
            pSessionInfo->StartTimeAsUTCFileTime = EventPipeAdapter::GetSessionStartTime(pSession);
            pSessionInfo->StartTimeStamp.QuadPart = EventPipeAdapter::GetSessionStartTimestamp(pSession);
            pSessionInfo->TimeStampFrequency.QuadPart = minipal_hires_tick_frequency();
            retVal = true;
        }
    }

    *pReturnValue = retVal;

    END_QCALL;
}

extern "C" QCallExceptionStatus QCALLTYPE EventPipeInternal_CreateProvider(
    _In_z_ LPCWSTR providerName,
    EventPipeCallback pCallbackFunc,
    void* pCallbackContext, INT_PTR* pReturnValue)
{
    QCALL_CONTRACT;

    EventPipeProvider *pProvider = NULL;

    BEGIN_QCALL;

    pProvider = EventPipeAdapter::CreateProvider(providerName, pCallbackFunc, pCallbackContext);

    *pReturnValue = reinterpret_cast<INT_PTR>(pProvider);

    END_QCALL;
}

extern "C" QCallExceptionStatus QCALLTYPE EventPipeInternal_DefineEvent(
    INT_PTR provHandle,
    UINT32 eventID,
    int64_t keywords,
    UINT32 eventVersion,
    UINT32 level,
    void *pMetadata,
    UINT32 metadataLength, INT_PTR* pReturnValue)
{
    QCALL_CONTRACT;

    EventPipeEvent *pEvent = NULL;

    BEGIN_QCALL;

    _ASSERTE(provHandle != (INT_PTR)NULL);
    EventPipeProvider *pProvider = reinterpret_cast<EventPipeProvider *>(provHandle);
    pEvent = EventPipeAdapter::AddEvent(pProvider, eventID, keywords, eventVersion, (EventPipeEventLevel)level, /* needStack = */ true, (BYTE *)pMetadata, metadataLength);
    _ASSERTE(pEvent != NULL);

    *pReturnValue = reinterpret_cast<INT_PTR>(pEvent);

    END_QCALL;
}

extern "C" QCallExceptionStatus QCALLTYPE EventPipeInternal_GetProvider(_In_z_ LPCWSTR providerName, INT_PTR* pReturnValue)
{
    QCALL_CONTRACT;

    EventPipeProvider *pProvider = NULL;

    BEGIN_QCALL;

    pProvider = EventPipeAdapter::GetProvider(providerName);

    *pReturnValue = reinterpret_cast<INT_PTR>(pProvider);

    END_QCALL;
}

extern "C" QCallExceptionStatus QCALLTYPE EventPipeInternal_DeleteProvider(INT_PTR provHandle)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    if (provHandle != 0)
    {
        EventPipeProvider *pProvider = reinterpret_cast<EventPipeProvider *>(provHandle);
        EventPipeAdapter::DeleteProvider(pProvider);
    }

    END_QCALL;
}

extern "C" QCallExceptionStatus QCALLTYPE EventPipeInternal_EventActivityIdControl(uint32_t controlCode, GUID *pActivityId, int* pReturnValue)
{

    QCALL_CONTRACT;

    BEGIN_QCALL;

    int retVal = 0;

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

            minipal_guid_v4_create(pActivityId);
            break;

        case ActivityControlCode::EVENT_ACTIVITY_CONTROL_GET_SET_ID:

            currentActivityId = *pThread->GetActivityId();
            pThread->SetActivityId(pActivityId);
            *pActivityId = currentActivityId;
            break;

        case ActivityControlCode::EVENT_ACTIVITY_CONTROL_CREATE_SET_ID:

            *pActivityId = *pThread->GetActivityId();
            minipal_guid_v4_create(&currentActivityId);
            pThread->SetActivityId(&currentActivityId);
            break;

        default:
            retVal = 1;
        }
    }

    *pReturnValue = retVal;

    END_QCALL;
}

extern "C" QCallExceptionStatus QCALLTYPE EventPipeInternal_WriteEventData(
    INT_PTR eventHandle,
    EventData *pEventData,
    UINT32 eventDataCount,
    LPCGUID pActivityId,
    LPCGUID pRelatedActivityId)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    _ASSERTE(eventHandle != (INT_PTR)NULL);
    EventPipeEvent *pEvent = reinterpret_cast<EventPipeEvent *>(eventHandle);
    EventPipeAdapter::WriteEvent(pEvent, pEventData, eventDataCount, pActivityId, pRelatedActivityId);

    END_QCALL;
}

extern "C" QCallExceptionStatus QCALLTYPE EventPipeInternal_GetNextEvent(UINT64 sessionID, EventPipeEventInstanceData *pInstance, BOOL* pReturnValue)
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

    *pReturnValue = pNextInstance != NULL;

    END_QCALL;
}

extern "C" QCallExceptionStatus QCALLTYPE EventPipeInternal_SignalSession(UINT64 sessionID, BOOL* pReturnValue)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    bool result = false;

    result = EventPipeAdapter::SignalSession(sessionID);

    *pReturnValue = result;

    END_QCALL;
}

extern "C" QCallExceptionStatus QCALLTYPE EventPipeInternal_WaitForSessionSignal(UINT64 sessionID, INT32 timeoutMs, BOOL* pReturnValue)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    bool result = false;

    result = EventPipeAdapter::WaitForSessionSignal(sessionID, timeoutMs);

    *pReturnValue = result;

    END_QCALL;
}

#endif // FEATURE_PERFTRACING
