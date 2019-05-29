// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeconfiguration.h"
#include "eventpipeeventinstance.h"
#include "eventpipeinternal.h"
#include "eventpipeprovider.h"
#include "eventpipesession.h"
#include "eventpipesessionprovider.h"

#ifdef FEATURE_PAL
#include "pal.h"
#endif // FEATURE_PAL

#ifdef FEATURE_PERFTRACING

UINT64 QCALLTYPE EventPipeInternal::Enable(
    __in_z LPCWSTR outputFile,
    UINT32 circularBufferSizeInMB,
    EventPipeProviderConfiguration *pProviders,
    UINT32 numProviders)
{
    QCALL_CONTRACT;

    UINT64 sessionID = 0;

    // Invalid input!
    if (circularBufferSizeInMB == 0 ||
        numProviders == 0 ||
        pProviders == nullptr)
    {
        return 0;
    }

    BEGIN_QCALL;
    {
        sessionID = EventPipe::Enable(
            outputFile,
            circularBufferSizeInMB,
            pProviders,
            numProviders,
            outputFile != NULL ? EventPipeSessionType::File : EventPipeSessionType::Listener,
            nullptr);
    }
    END_QCALL;

    return sessionID;
}

void QCALLTYPE EventPipeInternal::Disable(UINT64 sessionID)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    EventPipe::Disable(sessionID);
    END_QCALL;
}

bool QCALLTYPE EventPipeInternal::GetSessionInfo(UINT64 sessionID, EventPipeSessionInfo *pSessionInfo)
{
    QCALL_CONTRACT;

    bool retVal = false;
    BEGIN_QCALL;

    if (pSessionInfo != NULL)
    {
        EventPipeSession *pSession = EventPipe::GetSession(sessionID);
        if (pSession != NULL)
        {
            pSessionInfo->StartTimeAsUTCFileTime = pSession->GetStartTime();
            pSessionInfo->StartTimeStamp.QuadPart = pSession->GetStartTimeStamp().QuadPart;
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

    pProvider = EventPipe::CreateProvider(providerName, pCallbackFunc, NULL);

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
    pEvent = pProvider->AddEvent(eventID, keywords, eventVersion, (EventPipeEventLevel)level, /* needStack = */ true, (BYTE *)pMetadata, metadataLength);
    _ASSERTE(pEvent != NULL);

    END_QCALL;

    return reinterpret_cast<INT_PTR>(pEvent);
}

INT_PTR QCALLTYPE EventPipeInternal::GetProvider(__in_z LPCWSTR providerName)
{
    QCALL_CONTRACT;

    EventPipeProvider *pProvider = NULL;

    BEGIN_QCALL;

    pProvider = EventPipe::GetProvider(providerName);

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
        EventPipe::DeleteProvider(pProvider);
    }

    END_QCALL;
}

int QCALLTYPE EventPipeInternal::EventActivityIdControl(uint32_t controlCode, GUID *pActivityId)
{

    QCALL_CONTRACT;

    int retVal = 0;

    BEGIN_QCALL;

    Thread *pThread = GetThread();
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

void QCALLTYPE EventPipeInternal::WriteEvent(
    INT_PTR eventHandle,
    UINT32 eventID,
    void *pData,
    UINT32 length,
    LPCGUID pActivityId,
    LPCGUID pRelatedActivityId)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    _ASSERTE(eventHandle != NULL);
    EventPipeEvent *pEvent = reinterpret_cast<EventPipeEvent *>(eventHandle);
    EventPipe::WriteEvent(*pEvent, (BYTE *)pData, length, pActivityId, pRelatedActivityId);

    END_QCALL;
}

void QCALLTYPE EventPipeInternal::WriteEventData(
    INT_PTR eventHandle,
    UINT32 eventID,
    EventData *pEventData,
    UINT32 eventDataCount,
    LPCGUID pActivityId,
    LPCGUID pRelatedActivityId)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    _ASSERTE(eventHandle != NULL);
    EventPipeEvent *pEvent = reinterpret_cast<EventPipeEvent *>(eventHandle);
    EventPipe::WriteEvent(*pEvent, pEventData, eventDataCount, pActivityId, pRelatedActivityId);

    END_QCALL;
}

bool QCALLTYPE EventPipeInternal::GetNextEvent(UINT64 sessionID, EventPipeEventInstanceData *pInstance)
{
    QCALL_CONTRACT;

    EventPipeEventInstance *pNextInstance = NULL;
    BEGIN_QCALL;

    _ASSERTE(pInstance != NULL);

    pNextInstance = EventPipe::GetNextEvent(sessionID);
    if (pNextInstance)
    {
        pInstance->ProviderID = pNextInstance->GetEvent()->GetProvider();
        pInstance->EventID = pNextInstance->GetEvent()->GetEventID();
        pInstance->ThreadID = pNextInstance->GetThreadId();
        pInstance->TimeStamp.QuadPart = pNextInstance->GetTimeStamp()->QuadPart;
        pInstance->ActivityId = *pNextInstance->GetActivityId();
        pInstance->RelatedActivityId = *pNextInstance->GetRelatedActivityId();
        pInstance->Payload = pNextInstance->GetData();
        pInstance->PayloadLength = pNextInstance->GetDataLength();
    }

    END_QCALL;
    return pNextInstance != NULL;
}

#endif // FEATURE_PERFTRACING
