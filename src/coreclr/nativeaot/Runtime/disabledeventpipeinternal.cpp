// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "PalRedhawk.h"

#include <eventpipe/ep.h>

#ifdef FEATURE_PERFTRACING

struct EventPipeEventInstanceData;

struct EventPipeSessionInfo;

EXTERN_C NATIVEAOT_API uint64_t __cdecl RhEventPipeInternal_Enable(
    const WCHAR* outputFile,
    EventPipeSerializationFormat format,
    uint32_t circularBufferSizeInMB,
    /* COR_PRF_EVENTPIPE_PROVIDER_CONFIG */ const void * pProviders,
    uint32_t numProviders)
{
    return 0;
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_Disable(uint64_t sessionID)
{
}

EXTERN_C NATIVEAOT_API intptr_t __cdecl RhEventPipeInternal_CreateProvider(
    const WCHAR* providerName,
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

EXTERN_C NATIVEAOT_API intptr_t __cdecl RhEventPipeInternal_GetProvider(const WCHAR* providerName)
{
    return 0;
}

EXTERN_C NATIVEAOT_API void __cdecl RhEventPipeInternal_DeleteProvider(intptr_t provHandle)
{
}

EXTERN_C NATIVEAOT_API int __cdecl RhEventPipeInternal_EventActivityIdControl(uint32_t controlCode, GUID *pActivityId)
{
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
    return FALSE;
}

EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhEventPipeInternal_GetNextEvent(uint64_t sessionID, EventPipeEventInstanceData *pInstance)
{
    return FALSE;
}

EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhEventPipeInternal_SignalSession(uint64_t sessionID)
{
    return FALSE;
}

EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhEventPipeInternal_WaitForSessionSignal(uint64_t sessionID, int32_t timeoutMs)
{
    return FALSE;
}

#endif // FEATURE_PERFTRACING
