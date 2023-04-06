// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Work In Progress to add native events to EventPipe
// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// @TODO: Audit native events in NativeAOT Runtime

#include <common.h>
#include "eventpipeadapter.h"
#include "gcheaputilities.h"

#if defined(TARGET_UNIX)
#define wcslen PAL_wcslen
#endif

#ifndef _INC_WINDOWS
    typedef void* LPVOID;
    typedef uint32_t UINT;
    typedef void* PVOID;
    typedef uint64_t ULONGLONG;
    typedef uint32_t ULONG;
    typedef int64_t LONGLONG;
    typedef uint8_t BYTE;
    typedef uint16_t UINT16;
#endif // _INC_WINDOWS

#ifndef ERROR_WRITE_FAULT
#define ERROR_WRITE_FAULT 29L
#endif

#ifndef ERROR_SUCCESS
#define ERROR_SUCCESS   0
#endif

bool ResizeBuffer(uint8_t *&buffer, size_t& size, size_t currLen, size_t newSize, bool &fixedBuffer);

void InitProvidersAndEvents(void);
void InitDotNETRuntime(void);

bool WriteToBuffer(const BYTE *src, size_t len, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer);


template <typename T>
bool WriteToBuffer(const T &value, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer)
{
    if (sizeof(T) + offset > size)
    {
        if (!ResizeBuffer(buffer, size, offset, size + sizeof(T), fixedBuffer))
            return false;
    }

    memcpy(buffer + offset, (char *)&value, sizeof(T));
    offset += sizeof(T);
    return true;
}

bool WriteToBuffer(const BYTE *src, size_t len, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer)
{
    if (!src) return true;
    if (offset + len > size)
    {
        if (!ResizeBuffer(buffer, size, offset, size + len, fixedBuffer))
            return false;
    }

    memcpy(buffer + offset, src, len);
    offset += len;
    return true;
}

bool ResizeBuffer(BYTE *&buffer, size_t& size, size_t currLen, size_t newSize, bool &fixedBuffer)
{
    newSize = (size_t)(newSize * 1.5);
    _ASSERTE(newSize > size); // check for overflow

    if (newSize < 32)
        newSize = 32;

    BYTE *newBuffer = new (nothrow) BYTE[newSize];

    if (newBuffer == NULL)
        return false;

    memcpy(newBuffer, buffer, currLen);

    if (!fixedBuffer)
        delete[] buffer;

    buffer = newBuffer;
    size = newSize;
    fixedBuffer = false;

    return true;
}

// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// @TODO - Events need to be audited
const WCHAR* DotNETRuntimeName = L"Microsoft-Windows-DotNETRuntime";
EventPipeProvider *EventPipeProviderDotNETRuntime = nullptr;
EventPipeEvent *EventPipeEventDestroyGCHandle = nullptr;
EventPipeEvent *EventPipeEventGCStart_V2 = nullptr;
EventPipeEvent *EventPipeEventGCRestartEEEnd_V1 = nullptr;
EventPipeEvent *EventPipeEventGCRestartEEBegin_V1 = nullptr;
EventPipeEvent *EventPipeEventGCSuspendEEEnd_V1 = nullptr;
EventPipeEvent *EventPipeEventGCSuspendEEBegin_V1 = nullptr;

BOOL EventPipeEventEnabledDestroyGCHandle(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventDestroyGCHandle);
}

ULONG EventPipeWriteEventDestroyGCHandle(
    const void* HandleID,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledDestroyGCHandle())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(HandleID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventDestroyGCHandle, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCStart_V2(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCStart_V2);
}

ULONG EventPipeWriteEventGCStart_V2(
    const unsigned int Count,
    const unsigned int Depth,
    const unsigned int Reason,
    const unsigned int Type,
    const unsigned short ClrInstanceID,
    const unsigned __int64 ClientSequenceNumber,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCStart_V2())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Depth, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Reason, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Type, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClientSequenceNumber, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCStart_V2, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCRestartEEEnd_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCRestartEEEnd_V1);
}

ULONG EventPipeWriteEventGCRestartEEEnd_V1(
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCRestartEEEnd_V1())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCRestartEEEnd_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCRestartEEBegin_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCRestartEEBegin_V1);
}

ULONG EventPipeWriteEventGCRestartEEBegin_V1(
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCRestartEEBegin_V1())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCRestartEEBegin_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCSuspendEEEnd_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCSuspendEEEnd_V1);
}

ULONG EventPipeWriteEventGCSuspendEEEnd_V1(
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCSuspendEEEnd_V1())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCSuspendEEEnd_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCSuspendEEBegin_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCSuspendEEBegin_V1);
}

ULONG EventPipeWriteEventGCSuspendEEBegin_V1(
    const unsigned int Reason,
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCSuspendEEBegin_V1())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Reason, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCSuspendEEBegin_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

typedef struct _MCGEN_TRACE_CONTEXT
{
    TRACEHANDLE            RegistrationHandle;
    TRACEHANDLE            Logger;      // Used as pointer to provider traits.
    ULONGLONG              MatchAnyKeyword;
    ULONGLONG              MatchAllKeyword;
    ULONG                  Flags;
    ULONG                  IsEnabled;
    unsigned char          Level;
    unsigned char          Reserve;
    unsigned short                 EnableBitsCount;
    ULONG *                 EnableBitMask;
    const ULONGLONG*       EnableKeyWords;
    const unsigned char*   EnableLevel;
} MCGEN_TRACE_CONTEXT, *PMCGEN_TRACE_CONTEXT;

#if !defined(EVENTPIPE_TRACE_CONTEXT_DEF)
#define EVENTPIPE_TRACE_CONTEXT_DEF
typedef struct _EVENTPIPE_TRACE_CONTEXT
{
    const WCHAR * Name;
    unsigned char  Level;
    bool IsEnabled;
    ULONGLONG EnabledKeywordsBitmask;
} EVENTPIPE_TRACE_CONTEXT, *PEVENTPIPE_TRACE_CONTEXT;
#endif // EVENTPIPE_TRACE_CONTEXT_DEF

#if !defined(DOTNET_TRACE_CONTEXT_DEF)
#define DOTNET_TRACE_CONTEXT_DEF
typedef struct _DOTNET_TRACE_CONTEXT
{
    PMCGEN_TRACE_CONTEXT EtwProvider;
    EVENTPIPE_TRACE_CONTEXT EventPipeProvider;
} DOTNET_TRACE_CONTEXT, *PDOTNET_TRACE_CONTEXT;
#endif // DOTNET_TRACE_CONTEXT_DEF


enum CallbackProviderIndex
{
    DotNETRuntime = 0,
    DotNETRuntimeRundown = 1,
    DotNETRuntimeStress = 2,
    DotNETRuntimePrivate = 3
};

void EtwCallbackCommon(
    CallbackProviderIndex ProviderIndex,
    ULONG ControlCode,
    unsigned char Level,
    ULONGLONG MatchAnyKeyword,
    PVOID pFilterData,
    BOOL isEventPipeCallback);

void EventPipeEtwCallbackDotNETRuntime(
    _In_ GUID * SourceId,
    _In_ ULONG ControlCode,
    _In_ unsigned char Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext);


void EventPipeEtwCallbackDotNETRuntime(
    _In_ GUID * SourceId,
    _In_ ULONG ControlCode,
    _In_ unsigned char Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext)
{
    EtwCallbackCommon(DotNETRuntime, ControlCode, Level, MatchAnyKeyword, FilterData, true);
}

DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context;

// @TODO 
int const EVENT_CONTROL_CODE_ENABLE_PROVIDER=1;
int const EVENT_CONTROL_CODE_DISABLE_PROVIDER=0;

void EtwCallbackCommon(
    CallbackProviderIndex ProviderIndex,
    ULONG ControlCode,
    unsigned char Level,
    ULONGLONG MatchAnyKeyword,
    PVOID pFilterData,
    BOOL isEventPipeCallback)
{
//     LIMITED_METHOD_CONTRACT;

    bool bIsPublicTraceHandle = ProviderIndex == DotNETRuntime;

    DOTNET_TRACE_CONTEXT * ctxToUpdate;
    switch(ProviderIndex)
    {
    case DotNETRuntime:
        ctxToUpdate = &MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context;
        break;
    default:
        _ASSERTE(!"EtwCallbackCommon was called with invalid context");
        return;
    }

    // This callback gets called on both ETW/EventPipe session enable/disable.
    // We need toupdate the EventPipe provider context if we are in a callback
    // from EventPipe, but not from ETW.
    if (isEventPipeCallback)
    {
        ctxToUpdate->EventPipeProvider.Level = Level;
        ctxToUpdate->EventPipeProvider.EnabledKeywordsBitmask = MatchAnyKeyword;
        ctxToUpdate->EventPipeProvider.IsEnabled = ControlCode;

        // For EventPipe, ControlCode can only be either 0 or 1.
        _ASSERTE(ControlCode == 0 || ControlCode == 1);
    }

    if (
#if !defined(HOST_UNIX)
        (ControlCode == EVENT_CONTROL_CODE_ENABLE_PROVIDER || ControlCode == EVENT_CONTROL_CODE_DISABLE_PROVIDER) &&
#endif
        (ProviderIndex == DotNETRuntime || ProviderIndex == DotNETRuntimePrivate))
    {
        GCEventKeyword keywords = static_cast<GCEventKeyword>(ctxToUpdate->EventPipeProvider.EnabledKeywordsBitmask);
        GCEventLevel level = static_cast<GCEventLevel>(ctxToUpdate->EventPipeProvider.Level);
        GCHeapUtilities::RecordEventStateChange(bIsPublicTraceHandle, keywords, level);
    }
}


void InitProvidersAndEvents(void)
{
    InitDotNETRuntime();
}

void InitDotNETRuntime(void)
{
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // @TODO - Events need to be audited
    EventPipeProviderDotNETRuntime = EventPipeAdapter::CreateProvider(DotNETRuntimeName, reinterpret_cast<EventPipeCallback>(EventPipeEtwCallbackDotNETRuntime));
    EventPipeEventDestroyGCHandle = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,31,2,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCStart_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,1,1,2,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCRestartEEEnd_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,3,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCRestartEEBegin_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,7,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCSuspendEEEnd_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,8,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCSuspendEEBegin_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,9,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
}
