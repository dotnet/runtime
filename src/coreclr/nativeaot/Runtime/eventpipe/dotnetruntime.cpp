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
EventPipeEvent *EventPipeEventExceptionThrown_V1 = nullptr;
EventPipeEvent *EventPipeEventGCAllocationTick_V1 = nullptr;
EventPipeEvent *EventPipeEventGCAllocationTick_V2 = nullptr;
EventPipeEvent *EventPipeEventGCAllocationTick_V3 = nullptr;
EventPipeEvent *EventPipeEventGCBulkEdge = nullptr;
EventPipeEvent *EventPipeEventGCBulkMovedObjectRanges = nullptr;
EventPipeEvent *EventPipeEventGCBulkNode = nullptr;
EventPipeEvent *EventPipeEventGCBulkRCW = nullptr;
EventPipeEvent *EventPipeEventGCBulkRootCCW = nullptr;
EventPipeEvent *EventPipeEventGCBulkRootConditionalWeakTableElementEdge = nullptr;
EventPipeEvent *EventPipeEventGCBulkRootEdge = nullptr;
EventPipeEvent *EventPipeEventGCBulkSurvivingObjectRanges = nullptr;
EventPipeEvent *EventPipeEventGCCreateConcurrentThread_V1 = nullptr;
EventPipeEvent *EventPipeEventGCCreateSegment_V1 = nullptr;
EventPipeEvent *EventPipeEventGCEnd_V1 = nullptr;
EventPipeEvent *EventPipeEventGCFreeSegment_V1 = nullptr;
EventPipeEvent *EventPipeEventGCGenerationRange = nullptr;
EventPipeEvent *EventPipeEventGCGlobalHeapHistory_V2 = nullptr;
EventPipeEvent *EventPipeEventGCHeapStats_V1 = nullptr;
EventPipeEvent *EventPipeEventGCJoin_V2 = nullptr;
EventPipeEvent *EventPipeEventGCMarkFinalizeQueueRoots = nullptr;
EventPipeEvent *EventPipeEventGCMarkHandles = nullptr;
EventPipeEvent *EventPipeEventGCMarkOlderGenerationRoots = nullptr;
EventPipeEvent *EventPipeEventGCMarkStackRoots = nullptr;
EventPipeEvent *EventPipeEventGCMarkWithType = nullptr;
EventPipeEvent *EventPipeEventGCPerHeapHistory_V3 = nullptr;
EventPipeEvent *EventPipeEventGCTerminateConcurrentThread_V1 = nullptr;
EventPipeEvent *EventPipeEventGCTriggered = nullptr;
EventPipeEvent *EventPipeEventModuleLoad_V2 = nullptr;
EventPipeEvent *EventPipeEventSetGCHandle = nullptr;
EventPipeEvent *EventPipeEventGCStart_V1 = nullptr;
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

BOOL EventPipeEventEnabledExceptionThrown_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventExceptionThrown_V1);
}

ULONG EventPipeWriteEventExceptionThrown_V1(
    const wchar_t* ExceptionType,
    const wchar_t* ExceptionMessage,
    const void* ExceptionEIP,
    const unsigned int ExceptionHRESULT,
    const unsigned short ExceptionFlags,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledExceptionThrown_V1())
        return ERROR_SUCCESS;

    size_t size = 144;
    BYTE stackBuffer[144];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    if (!ExceptionType) { ExceptionType = L"NULL"; }
    if (!ExceptionMessage) { ExceptionMessage = L"NULL"; }
    success &= WriteToBuffer(ExceptionType, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ExceptionMessage, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ExceptionEIP, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ExceptionHRESULT, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ExceptionFlags, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventExceptionThrown_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCAllocationTick_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCAllocationTick_V1);
}

ULONG EventPipeWriteEventGCAllocationTick_V1(
    const unsigned int AllocationAmount,
    const unsigned int AllocationKind,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCAllocationTick_V1())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(AllocationAmount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AllocationKind, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCAllocationTick_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCAllocationTick_V2(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCAllocationTick_V2);
}

ULONG EventPipeWriteEventGCAllocationTick_V2(
    const unsigned int AllocationAmount,
    const unsigned int AllocationKind,
    const unsigned short ClrInstanceID,
    const unsigned __int64 AllocationAmount64,
    const void* TypeID,
    const wchar_t* TypeName,
    const unsigned int HeapIndex,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCAllocationTick_V2())
        return ERROR_SUCCESS;

    size_t size = 94;
    BYTE stackBuffer[94];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    if (!TypeName) { TypeName = L"NULL"; }
    success &= WriteToBuffer(AllocationAmount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AllocationKind, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AllocationAmount64, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TypeID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TypeName, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(HeapIndex, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCAllocationTick_V2, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCAllocationTick_V3(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCAllocationTick_V3);
}

ULONG EventPipeWriteEventGCAllocationTick_V3(
    const unsigned int AllocationAmount,
    const unsigned int AllocationKind,
    const unsigned short ClrInstanceID,
    const unsigned __int64 AllocationAmount64,
    const void* TypeID,
    const wchar_t* TypeName,
    const unsigned int HeapIndex,
    const void* Address,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCAllocationTick_V3())
        return ERROR_SUCCESS;

    size_t size = 102;
    BYTE stackBuffer[102];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    if (!TypeName) { TypeName = L"NULL"; }
    success &= WriteToBuffer(AllocationAmount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AllocationKind, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AllocationAmount64, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TypeID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TypeName, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(HeapIndex, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Address, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCAllocationTick_V3, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCBulkEdge(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCBulkEdge);
}

ULONG EventPipeWriteEventGCBulkEdge(
    const unsigned int Index,
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCBulkEdge())
        return ERROR_SUCCESS;

    size_t size = 42;
    BYTE stackBuffer[42];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Index, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)Values_ElementSize * (int)Count, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCBulkEdge, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCBulkMovedObjectRanges(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCBulkMovedObjectRanges);
}

ULONG EventPipeWriteEventGCBulkMovedObjectRanges(
    const unsigned int Index,
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCBulkMovedObjectRanges())
        return ERROR_SUCCESS;

    size_t size = 42;
    BYTE stackBuffer[42];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Index, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)Values_ElementSize * (int)Count, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCBulkMovedObjectRanges, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCBulkNode(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCBulkNode);
}

ULONG EventPipeWriteEventGCBulkNode(
    const unsigned int Index,
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCBulkNode())
        return ERROR_SUCCESS;

    size_t size = 42;
    BYTE stackBuffer[42];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Index, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)Values_ElementSize * (int)Count, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCBulkNode, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCBulkRCW(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCBulkRCW);
}

ULONG EventPipeWriteEventGCBulkRCW(
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCBulkRCW())
        return ERROR_SUCCESS;

    size_t size = 38;
    BYTE stackBuffer[38];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)(Values_ElementSize), buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCBulkRCW, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCBulkRootCCW(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCBulkRootCCW);
}

ULONG EventPipeWriteEventGCBulkRootCCW(
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCBulkRootCCW())
        return ERROR_SUCCESS;

    size_t size = 38;
    BYTE stackBuffer[38];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)(Values_ElementSize), buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCBulkRootCCW, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCBulkRootConditionalWeakTableElementEdge(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCBulkRootConditionalWeakTableElementEdge);
}

ULONG EventPipeWriteEventGCBulkRootConditionalWeakTableElementEdge(
    const unsigned int Index,
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCBulkRootConditionalWeakTableElementEdge())
        return ERROR_SUCCESS;

    size_t size = 42;
    BYTE stackBuffer[42];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Index, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)Values_ElementSize * (int)Count, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCBulkRootConditionalWeakTableElementEdge, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCBulkRootEdge(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCBulkRootEdge);
}

ULONG EventPipeWriteEventGCBulkRootEdge(
    const unsigned int Index,
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCBulkRootEdge())
        return ERROR_SUCCESS;

    size_t size = 42;
    BYTE stackBuffer[42];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Index, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)Values_ElementSize * (int)Count, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCBulkRootEdge, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCBulkSurvivingObjectRanges(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCBulkSurvivingObjectRanges);
}

ULONG EventPipeWriteEventGCBulkSurvivingObjectRanges(
    const unsigned int Index,
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCBulkSurvivingObjectRanges())
        return ERROR_SUCCESS;

    size_t size = 42;
    BYTE stackBuffer[42];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Index, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)Values_ElementSize * (int)Count, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCBulkSurvivingObjectRanges, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCCreateConcurrentThread_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCCreateConcurrentThread_V1);
}

ULONG EventPipeWriteEventGCCreateConcurrentThread_V1(
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCCreateConcurrentThread_V1())
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

    EventPipeAdapter::WriteEvent(EventPipeEventGCCreateConcurrentThread_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCCreateSegment_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCCreateSegment_V1);
}

ULONG EventPipeWriteEventGCCreateSegment_V1(
    const unsigned __int64 Address,
    const unsigned __int64 Size,
    const unsigned int Type,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCCreateSegment_V1())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Address, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Size, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Type, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCCreateSegment_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCEnd_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCEnd_V1);
}

ULONG EventPipeWriteEventGCEnd_V1(
    const unsigned int Count,
    const unsigned int Depth,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCEnd_V1())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Depth, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCEnd_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCFreeSegment_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCFreeSegment_V1);
}

ULONG EventPipeWriteEventGCFreeSegment_V1(
    const unsigned __int64 Address,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCFreeSegment_V1())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Address, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCFreeSegment_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCGenerationRange(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCGenerationRange);
}

ULONG EventPipeWriteEventGCGenerationRange(
    const unsigned char Generation,
    const void* RangeStart,
    const unsigned __int64 RangeUsedLength,
    const unsigned __int64 RangeReservedLength,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCGenerationRange())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Generation, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(RangeStart, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(RangeUsedLength, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(RangeReservedLength, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCGenerationRange, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCGlobalHeapHistory_V2(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCGlobalHeapHistory_V2);
}

ULONG EventPipeWriteEventGCGlobalHeapHistory_V2(
    const unsigned __int64 FinalYoungestDesired,
    const signed int NumHeaps,
    const unsigned int CondemnedGeneration,
    const unsigned int Gen0ReductionCount,
    const unsigned int Reason,
    const unsigned int GlobalMechanisms,
    const unsigned short ClrInstanceID,
    const unsigned int PauseMode,
    const unsigned int MemoryPressure,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCGlobalHeapHistory_V2())
        return ERROR_SUCCESS;

    size_t size = 38;
    BYTE stackBuffer[38];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(FinalYoungestDesired, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(NumHeaps, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(CondemnedGeneration, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Gen0ReductionCount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Reason, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(GlobalMechanisms, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(PauseMode, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(MemoryPressure, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCGlobalHeapHistory_V2, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCHeapStats_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCHeapStats_V1);
}

ULONG EventPipeWriteEventGCHeapStats_V1(
    const unsigned __int64 GenerationSize0,
    const unsigned __int64 TotalPromotedSize0,
    const unsigned __int64 GenerationSize1,
    const unsigned __int64 TotalPromotedSize1,
    const unsigned __int64 GenerationSize2,
    const unsigned __int64 TotalPromotedSize2,
    const unsigned __int64 GenerationSize3,
    const unsigned __int64 TotalPromotedSize3,
    const unsigned __int64 FinalizationPromotedSize,
    const unsigned __int64 FinalizationPromotedCount,
    const unsigned int PinnedObjectCount,
    const unsigned int SinkBlockCount,
    const unsigned int GCHandleCount,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCHeapStats_V1())
        return ERROR_SUCCESS;

    size_t size = 94;
    BYTE stackBuffer[94];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(GenerationSize0, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TotalPromotedSize0, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(GenerationSize1, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TotalPromotedSize1, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(GenerationSize2, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TotalPromotedSize2, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(GenerationSize3, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TotalPromotedSize3, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(FinalizationPromotedSize, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(FinalizationPromotedCount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(PinnedObjectCount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(SinkBlockCount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(GCHandleCount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCHeapStats_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCJoin_V2(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCJoin_V2);
}

ULONG EventPipeWriteEventGCJoin_V2(
    const unsigned int Heap,
    const unsigned int JoinTime,
    const unsigned int JoinType,
    const unsigned short ClrInstanceID,
    const unsigned int JoinID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCJoin_V2())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Heap, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(JoinTime, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(JoinType, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(JoinID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCJoin_V2, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCMarkFinalizeQueueRoots(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCMarkFinalizeQueueRoots);
}

ULONG EventPipeWriteEventGCMarkFinalizeQueueRoots(
    const unsigned int HeapNum,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCMarkFinalizeQueueRoots())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(HeapNum, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCMarkFinalizeQueueRoots, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCMarkHandles(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCMarkHandles);
}

ULONG EventPipeWriteEventGCMarkHandles(
    const unsigned int HeapNum,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCMarkHandles())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(HeapNum, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCMarkHandles, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCMarkOlderGenerationRoots(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCMarkOlderGenerationRoots);
}

ULONG EventPipeWriteEventGCMarkOlderGenerationRoots(
    const unsigned int HeapNum,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCMarkOlderGenerationRoots())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(HeapNum, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCMarkOlderGenerationRoots, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCMarkStackRoots(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCMarkStackRoots);
}

ULONG EventPipeWriteEventGCMarkStackRoots(
    const unsigned int HeapNum,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCMarkStackRoots())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(HeapNum, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCMarkStackRoots, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCMarkWithType(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCMarkWithType);
}

ULONG EventPipeWriteEventGCMarkWithType(
    const unsigned int HeapNum,
    const unsigned short ClrInstanceID,
    const unsigned int Type,
    const unsigned __int64 Bytes,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCMarkWithType())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(HeapNum, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Type, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Bytes, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCMarkWithType, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCPerHeapHistory_V3(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCPerHeapHistory_V3);
}

ULONG EventPipeWriteEventGCPerHeapHistory_V3(
    const unsigned short ClrInstanceID,
    const void* FreeListAllocated,
    const void* FreeListRejected,
    const void* EndOfSegAllocated,
    const void* CondemnedAllocated,
    const void* PinnedAllocated,
    const void* PinnedAllocatedAdvance,
    const unsigned int RunningFreeListEfficiency,
    const unsigned int CondemnReasons0,
    const unsigned int CondemnReasons1,
    const unsigned int CompactMechanisms,
    const unsigned int ExpandMechanisms,
    const unsigned int HeapIndex,
    const void* ExtraGen0Commit,
    const unsigned int Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCPerHeapHistory_V3())
        return ERROR_SUCCESS;

    size_t size = 118;
    BYTE stackBuffer[118];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(FreeListAllocated, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(FreeListRejected, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(EndOfSegAllocated, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(CondemnedAllocated, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(PinnedAllocated, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(PinnedAllocatedAdvance, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(RunningFreeListEfficiency, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(CondemnReasons0, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(CondemnReasons1, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(CompactMechanisms, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ExpandMechanisms, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(HeapIndex, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ExtraGen0Commit, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)Values_ElementSize * (int)Count, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCPerHeapHistory_V3, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCTerminateConcurrentThread_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCTerminateConcurrentThread_V1);
}

ULONG EventPipeWriteEventGCTerminateConcurrentThread_V1(
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCTerminateConcurrentThread_V1())
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

    EventPipeAdapter::WriteEvent(EventPipeEventGCTerminateConcurrentThread_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCTriggered(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCTriggered);
}

ULONG EventPipeWriteEventGCTriggered(
    const unsigned int Reason,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCTriggered())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Reason, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCTriggered, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledModuleLoad_V2(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventModuleLoad_V2);
}

ULONG EventPipeWriteEventModuleLoad_V2(
    const unsigned __int64 ModuleID,
    const unsigned __int64 AssemblyID,
    const unsigned int ModuleFlags,
    const unsigned int Reserved1,
    const wchar_t* ModuleILPath,
    const wchar_t* ModuleNativePath,
    const unsigned short ClrInstanceID,
    const GUID* ManagedPdbSignature,
    const unsigned int ManagedPdbAge,
    const wchar_t* ManagedPdbBuildPath,
    const GUID* NativePdbSignature,
    const unsigned int NativePdbAge,
    const wchar_t* NativePdbBuildPath,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledModuleLoad_V2())
        return ERROR_SUCCESS;

    size_t size = 290;
    BYTE stackBuffer[290];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    if (!ModuleILPath) { ModuleILPath = L"NULL"; }
    if (!ModuleNativePath) { ModuleNativePath = L"NULL"; }
    if (!ManagedPdbBuildPath) { ManagedPdbBuildPath = L"NULL"; }
    if (!NativePdbBuildPath) { NativePdbBuildPath = L"NULL"; }
    success &= WriteToBuffer(ModuleID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AssemblyID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ModuleFlags, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Reserved1, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ModuleILPath, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ModuleNativePath, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(*ManagedPdbSignature, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ManagedPdbAge, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ManagedPdbBuildPath, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(*NativePdbSignature, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(NativePdbAge, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(NativePdbBuildPath, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventModuleLoad_V2, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledSetGCHandle(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventSetGCHandle);
}

ULONG EventPipeWriteEventSetGCHandle(
    const void* HandleID,
    const void* ObjectID,
    const unsigned int Kind,
    const unsigned int Generation,
    const unsigned __int64 AppDomainID,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledSetGCHandle())
        return ERROR_SUCCESS;

    size_t size = 34;
    BYTE stackBuffer[34];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(HandleID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ObjectID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Kind, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Generation, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AppDomainID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventSetGCHandle, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCStart_V1(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventGCStart_V1);
}

ULONG EventPipeWriteEventGCStart_V1(
    const unsigned int Count,
    const unsigned int Depth,
    const unsigned int Reason,
    const unsigned int Type,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCStart_V1())
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

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCStart_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

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
    EventPipeEventExceptionThrown_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,80,8589967360,1,EP_EVENT_LEVEL_ERROR,true);
    EventPipeEventGCAllocationTick_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,10,1,1,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCAllocationTick_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,10,1,2,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCAllocationTick_V3 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,10,1,3,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCBulkEdge = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,19,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkMovedObjectRanges = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,22,4194304,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkNode = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,18,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkRCW = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,37,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkRootCCW = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,36,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkRootConditionalWeakTableElementEdge = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,17,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkRootEdge = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,16,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkSurvivingObjectRanges = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,21,4194304,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCCreateConcurrentThread_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,11,65537,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCCreateSegment_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,5,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCEnd_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,2,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCFreeSegment_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,6,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCGenerationRange = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,23,4194304,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCGlobalHeapHistory_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,205,1,2,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCHeapStats_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,4,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCJoin_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,203,1,2,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCMarkFinalizeQueueRoots = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,26,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCMarkHandles = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,27,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCMarkOlderGenerationRoots = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,28,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCMarkStackRoots = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,25,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCMarkWithType = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,202,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCPerHeapHistory_V3 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,204,1,3,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCTerminateConcurrentThread_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,12,65537,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCTriggered = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,35,1,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventModuleLoad_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,152,536870920,2,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventSetGCHandle = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,30,2,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCStart_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,1,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCStart_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,1,1,2,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCRestartEEEnd_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,3,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCRestartEEBegin_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,7,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCSuspendEEEnd_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,8,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCSuspendEEBegin_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,9,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
}
