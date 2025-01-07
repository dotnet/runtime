// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "PalRedhawk.h"

#include <eventpipe/ep.h>

#ifdef FEATURE_PERFTRACING

struct EventPipeEventInstanceData;

struct EventPipeSessionInfo;

EXTERN_C uint64_t QCALLTYPE EventPipeInternal_Enable(
    const WCHAR* outputFile,
    EventPipeSerializationFormat format,
    uint32_t circularBufferSizeInMB,
    /* COR_PRF_EVENTPIPE_PROVIDER_CONFIG */ const void * pProviders,
    uint32_t numProviders)
{
    return 0;
}

EXTERN_C void QCALLTYPE EventPipeInternal_Disable(uint64_t sessionID)
{
}

EXTERN_C intptr_t QCALLTYPE EventPipeInternal_CreateProvider(
    const WCHAR* providerName,
    EventPipeCallback pCallbackFunc,
    void* pCallbackContext)
{
    return 0;
}

EXTERN_C intptr_t QCALLTYPE EventPipeInternal_DefineEvent(
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

EXTERN_C intptr_t QCALLTYPE EventPipeInternal_GetProvider(const WCHAR* providerName)
{
    return 0;
}

EXTERN_C void QCALLTYPE EventPipeInternal_DeleteProvider(intptr_t provHandle)
{
}

EXTERN_C int QCALLTYPE EventPipeInternal_EventActivityIdControl(uint32_t controlCode, GUID *pActivityId)
{
    return 0;
}

EXTERN_C void QCALLTYPE EventPipeInternal_WriteEventData(
    intptr_t eventHandle,
    EventData *pEventData,
    uint32_t eventDataCount,
    const GUID * pActivityId,
    const GUID * pRelatedActivityId)
{
}

EXTERN_C UInt32_BOOL QCALLTYPE EventPipeInternal_GetSessionInfo(uint64_t sessionID, EventPipeSessionInfo *pSessionInfo)
{
    return FALSE;
}

EXTERN_C UInt32_BOOL QCALLTYPE EventPipeInternal_GetNextEvent(uint64_t sessionID, EventPipeEventInstanceData *pInstance)
{
    return FALSE;
}

EXTERN_C UInt32_BOOL QCALLTYPE EventPipeInternal_SignalSession(uint64_t sessionID)
{
    return FALSE;
}

EXTERN_C UInt32_BOOL QCALLTYPE EventPipeInternal_WaitForSessionSignal(uint64_t sessionID, int32_t timeoutMs)
{
    return FALSE;
}

#endif // FEATURE_PERFTRACING
