// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __EVENTPIPEINTERNAL_H__
#define __EVENTPIPEINTERNAL_H__

#ifdef FEATURE_PERFTRACING

// TODO: Maybe we should move the other types that are used on PInvoke here?

enum class ActivityControlCode
{
    EVENT_ACTIVITY_CONTROL_GET_ID = 1,
    EVENT_ACTIVITY_CONTROL_SET_ID = 2,
    EVENT_ACTIVITY_CONTROL_CREATE_ID = 3,
    EVENT_ACTIVITY_CONTROL_GET_SET_ID = 4,
    EVENT_ACTIVITY_CONTROL_CREATE_SET_ID = 5
};

struct EventPipeEventInstanceData
{
    void *ProviderID;
    unsigned int EventID;
    unsigned int ThreadID;
    LARGE_INTEGER TimeStamp;
    GUID ActivityId;
    GUID RelatedActivityId;
    const BYTE *Payload;
    unsigned int PayloadLength;
};

struct EventPipeSessionInfo
{
    FILETIME StartTimeAsUTCFileTime;
    LARGE_INTEGER StartTimeStamp;
    LARGE_INTEGER TimeStampFrequency;
};

//!
//! Sets the sampling rate and enables the event pipe for the specified configuration.
//!
extern "C" UINT64 QCALLTYPE EventPipeInternal_Enable(
    _In_z_ LPCWSTR outputFile,
    EventPipeSerializationFormat format,
    UINT32 circularBufferSizeInMB,
    /* COR_PRF_EVENTPIPE_PROVIDER_CONFIG */ LPCVOID pProviders,
    UINT32 numProviders);

//!
//! Disables the specified session Id.
//!
extern "C" void QCALLTYPE EventPipeInternal_Disable(UINT64 sessionID);

extern "C" bool QCALLTYPE EventPipeInternal_GetSessionInfo(UINT64 sessionID, EventPipeSessionInfo *pSessionInfo);

extern "C" INT_PTR QCALLTYPE EventPipeInternal_CreateProvider(
    _In_z_ LPCWSTR providerName,
    EventPipeCallback pCallbackFunc);

extern "C" INT_PTR QCALLTYPE EventPipeInternal_DefineEvent(
    INT_PTR provHandle,
    UINT32 eventID,
    __int64 keywords,
    UINT32 eventVersion,
    UINT32 level,
    void *pMetadata,
    UINT32 metadataLength);

extern "C" INT_PTR QCALLTYPE EventPipeInternal_GetProvider(
    _In_z_ LPCWSTR providerName);

extern "C" void QCALLTYPE EventPipeInternal_DeleteProvider(
    INT_PTR provHandle);

extern "C" int QCALLTYPE EventPipeInternal_EventActivityIdControl(
    uint32_t controlCode,
    GUID *pActivityId);

extern "C" void QCALLTYPE EventPipeInternal_WriteEventData(
    INT_PTR eventHandle,
    EventData *pEventData,
    UINT32 eventDataCount,
    LPCGUID pActivityId, LPCGUID pRelatedActivityId);

extern "C" bool QCALLTYPE EventPipeInternal_GetNextEvent(
    UINT64 sessionID,
    EventPipeEventInstanceData *pInstance);

extern "C" HANDLE QCALLTYPE EventPipeInternal_GetWaitHandle(
    UINT64 sessionID);

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPEINTERNAL_H__
