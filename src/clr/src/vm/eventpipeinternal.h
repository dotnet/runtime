// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPEINTERNAL_H__
#define __EVENTPIPEINTERNAL_H__

#ifdef FEATURE_PERFTRACING

// TODO: Maybe we should move the other types that are used on PInvoke here?

class EventPipeInternal
{
private:
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

public:
    //!
    //! Sets the sampling rate and enables the event pipe for the specified configuration.
    //!
    static UINT64 QCALLTYPE Enable(
        __in_z LPCWSTR outputFile,
        UINT32 circularBufferSizeInMB,
        EventPipeProviderConfiguration *pProviders,
        UINT32 numProviders);

    //!
    //! Disables the specified session Id.
    //!
    static void QCALLTYPE Disable(UINT64 sessionID);

    static bool QCALLTYPE GetSessionInfo(UINT64 sessionID, EventPipeSessionInfo *pSessionInfo);

    static INT_PTR QCALLTYPE CreateProvider(
        __in_z LPCWSTR providerName,
        EventPipeCallback pCallbackFunc);

    static INT_PTR QCALLTYPE DefineEvent(
        INT_PTR provHandle,
        UINT32 eventID,
        __int64 keywords,
        UINT32 eventVersion,
        UINT32 level,
        void *pMetadata,
        UINT32 metadataLength);

    static INT_PTR QCALLTYPE GetProvider(
        __in_z LPCWSTR providerName);

    static void QCALLTYPE DeleteProvider(
        INT_PTR provHandle);

    static int QCALLTYPE EventActivityIdControl(
        uint32_t controlCode,
        GUID *pActivityId);

    static void QCALLTYPE WriteEvent(
        INT_PTR eventHandle,
        UINT32 eventID,
        void *pData,
        UINT32 length,
        LPCGUID pActivityId, LPCGUID pRelatedActivityId);

    static void QCALLTYPE WriteEventData(
        INT_PTR eventHandle,
        UINT32 eventID,
        EventData *pEventData,
        UINT32 eventDataCount,
        LPCGUID pActivityId, LPCGUID pRelatedActivityId);

    static bool QCALLTYPE GetNextEvent(
        UINT64 sessionID,
        EventPipeEventInstanceData *pInstance);
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPEINTERNAL_H__
