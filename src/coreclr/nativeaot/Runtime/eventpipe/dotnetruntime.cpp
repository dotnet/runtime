// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Work In Progress to add native events to EventPipe
// shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
// @TODO: Audit native events in NativeAOT Runtime

#include <common.h>
#include <gcenv.h>

#include <eventpipeadapter.h>
#include <eventtrace_context.h>
#include <gcheaputilities.h>

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

bool WriteToBuffer(const WCHAR* str, BYTE *&buffer, size_t& offset, size_t& size, bool &fixedBuffer)
{
    if (str == NULL)
        return true;

    size_t byteCount = (ep_rt_utf16_string_len(reinterpret_cast<const ep_char16_t*>(str)) + 1) * sizeof(*str);
    if (offset + byteCount > size)
    {
        if (!ResizeBuffer(buffer, size, offset, size + byteCount, fixedBuffer))
            return false;
    }

    memcpy(buffer + offset, str, byteCount);
    offset += byteCount;
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
const WCHAR* DotNETRuntimeName = W("Microsoft-Windows-DotNETRuntime");
EventPipeProvider *EventPipeProviderDotNETRuntime = nullptr;
EventPipeEvent *EventPipeEventDestroyGCHandle = nullptr;
EventPipeEvent *EventPipeEventExceptionThrown_V1 = nullptr;
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
EventPipeEvent *EventPipeEventGCStart_V2 = nullptr;
EventPipeEvent *EventPipeEventGCRestartEEEnd_V1 = nullptr;
EventPipeEvent *EventPipeEventGCRestartEEBegin_V1 = nullptr;
EventPipeEvent *EventPipeEventGCSuspendEEEnd_V1 = nullptr;
EventPipeEvent *EventPipeEventGCSuspendEEBegin_V1 = nullptr;
EventPipeEvent *EventPipeEventDecreaseMemoryPressure = nullptr;
EventPipeEvent *EventPipeEventFinalizeObject = nullptr;
EventPipeEvent *EventPipeEventGCFinalizersBegin_V1 = nullptr;
EventPipeEvent *EventPipeEventGCFinalizersEnd_V1 = nullptr;
EventPipeEvent *EventPipeEventContentionStart_V2 = nullptr;
EventPipeEvent *EventPipeEventContentionStop_V1 = nullptr;
EventPipeEvent *EventPipeEventContentionLockCreated = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadStart = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadStop = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadWait = nullptr;
EventPipeEvent *EventPipeEventThreadPoolMinMaxThreads = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadAdjustmentSample = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadAdjustmentAdjustment = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadAdjustmentStats = nullptr;
EventPipeEvent *EventPipeEventThreadPoolIOEnqueue = nullptr;
EventPipeEvent *EventPipeEventThreadPoolIODequeue = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkingThreadCount = nullptr;
EventPipeEvent *EventPipeEventThreadPoolIOPack = nullptr;
EventPipeEvent *EventPipeEventGCAllocationTick_V4 = nullptr;
EventPipeEvent *EventPipeEventGCHeapStats_V2 = nullptr;
EventPipeEvent *EventPipeEventGCSampledObjectAllocationHigh = nullptr;
EventPipeEvent *EventPipeEventGCSampledObjectAllocationLow = nullptr;
EventPipeEvent *EventPipeEventPinObjectAtGCTime = nullptr;
EventPipeEvent *EventPipeEventGCBulkRootStaticVar = nullptr;
EventPipeEvent *EventPipeEventIncreaseMemoryPressure = nullptr;
EventPipeEvent *EventPipeEventGCGlobalHeapHistory_V4 = nullptr;
EventPipeEvent *EventPipeEventGenAwareBegin = nullptr;
EventPipeEvent *EventPipeEventGenAwareEnd = nullptr;
EventPipeEvent *EventPipeEventGCLOHCompact = nullptr;
EventPipeEvent *EventPipeEventGCFitBucketInfo = nullptr;

BOOL EventPipeEventEnabledDestroyGCHandle(void)
{
    return ep_event_is_enabled(EventPipeEventDestroyGCHandle);
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
    return ep_event_is_enabled(EventPipeEventExceptionThrown_V1);
}

ULONG EventPipeWriteEventExceptionThrown_V1(
    const WCHAR* ExceptionType,
    const WCHAR* ExceptionMessage,
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

    if (!ExceptionType) { ExceptionType = W("NULL"); }
    if (!ExceptionMessage) { ExceptionMessage = W("NULL"); }
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

BOOL EventPipeEventEnabledGCBulkEdge(void)
{
    return ep_event_is_enabled(EventPipeEventGCBulkEdge);
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
    return ep_event_is_enabled(EventPipeEventGCBulkMovedObjectRanges);
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
    return ep_event_is_enabled(EventPipeEventGCBulkNode);
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
    return ep_event_is_enabled(EventPipeEventGCBulkRCW);
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
    return ep_event_is_enabled(EventPipeEventGCBulkRootCCW);
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
    return ep_event_is_enabled(EventPipeEventGCBulkRootConditionalWeakTableElementEdge);
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
    return ep_event_is_enabled(EventPipeEventGCBulkRootEdge);
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
    return ep_event_is_enabled(EventPipeEventGCBulkSurvivingObjectRanges);
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
    return ep_event_is_enabled(EventPipeEventGCCreateConcurrentThread_V1);
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
    return ep_event_is_enabled(EventPipeEventGCCreateSegment_V1);
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
    return ep_event_is_enabled(EventPipeEventGCEnd_V1);
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
    return ep_event_is_enabled(EventPipeEventGCFreeSegment_V1);
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
    return ep_event_is_enabled(EventPipeEventGCGenerationRange);
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

BOOL EventPipeEventEnabledGCHeapStats_V1(void)
{
    return ep_event_is_enabled(EventPipeEventGCHeapStats_V1);
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
    return ep_event_is_enabled(EventPipeEventGCJoin_V2);
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
    return ep_event_is_enabled(EventPipeEventGCMarkFinalizeQueueRoots);
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
    return ep_event_is_enabled(EventPipeEventGCMarkHandles);
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
    return ep_event_is_enabled(EventPipeEventGCMarkOlderGenerationRoots);
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
    return ep_event_is_enabled(EventPipeEventGCMarkStackRoots);
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
    return ep_event_is_enabled(EventPipeEventGCMarkWithType);
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
    return ep_event_is_enabled(EventPipeEventGCPerHeapHistory_V3);
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
    return ep_event_is_enabled(EventPipeEventGCTerminateConcurrentThread_V1);
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
    return ep_event_is_enabled(EventPipeEventGCTriggered);
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
    return ep_event_is_enabled(EventPipeEventModuleLoad_V2);
}

ULONG EventPipeWriteEventModuleLoad_V2(
    const unsigned __int64 ModuleID,
    const unsigned __int64 AssemblyID,
    const unsigned int ModuleFlags,
    const unsigned int Reserved1,
    const WCHAR* ModuleILPath,
    const WCHAR* ModuleNativePath,
    const unsigned short ClrInstanceID,
    const GUID* ManagedPdbSignature,
    const unsigned int ManagedPdbAge,
    const WCHAR* ManagedPdbBuildPath,
    const GUID* NativePdbSignature,
    const unsigned int NativePdbAge,
    const WCHAR* NativePdbBuildPath,
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

    if (!ModuleILPath) { ModuleILPath = W("NULL"); }
    if (!ModuleNativePath) { ModuleNativePath = W("NULL"); }
    if (!ManagedPdbBuildPath) { ManagedPdbBuildPath = W("NULL"); }
    if (!NativePdbBuildPath) { NativePdbBuildPath = W("NULL"); }
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
    return ep_event_is_enabled(EventPipeEventSetGCHandle);
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

BOOL EventPipeEventEnabledGCStart_V2(void)
{
    return ep_event_is_enabled(EventPipeEventGCStart_V2);
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
    return ep_event_is_enabled(EventPipeEventGCRestartEEEnd_V1);
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
    return ep_event_is_enabled(EventPipeEventGCRestartEEBegin_V1);
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
    return ep_event_is_enabled(EventPipeEventGCSuspendEEEnd_V1);
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
    return ep_event_is_enabled(EventPipeEventGCSuspendEEBegin_V1);
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

BOOL EventPipeEventEnabledDecreaseMemoryPressure(void)
{
    return ep_event_is_enabled(EventPipeEventDecreaseMemoryPressure);
}

ULONG EventPipeWriteEventDecreaseMemoryPressure(
    const unsigned __int64 BytesFreed,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledDecreaseMemoryPressure())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(BytesFreed, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventDecreaseMemoryPressure, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledFinalizeObject(void)
{
    return ep_event_is_enabled(EventPipeEventFinalizeObject);
}

ULONG EventPipeWriteEventFinalizeObject(
    const void* TypeID,
    const void* ObjectID,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledFinalizeObject())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(TypeID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ObjectID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventFinalizeObject, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCFinalizersBegin_V1(void)
{
    return ep_event_is_enabled(EventPipeEventGCFinalizersBegin_V1);
}

ULONG EventPipeWriteEventGCFinalizersBegin_V1(
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCFinalizersBegin_V1())
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

    EventPipeAdapter::WriteEvent(EventPipeEventGCFinalizersBegin_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCFinalizersEnd_V1(void)
{
    return ep_event_is_enabled(EventPipeEventGCFinalizersEnd_V1);
}

ULONG EventPipeWriteEventGCFinalizersEnd_V1(
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCFinalizersEnd_V1())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCFinalizersEnd_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledContentionStart_V2(void)
{
    return ep_event_is_enabled(EventPipeEventContentionStart_V2);
}

ULONG EventPipeWriteEventContentionStart_V2(
    const unsigned char ContentionFlags,
    const unsigned short ClrInstanceID,
    const void* LockID,
    const void* AssociatedObjectID,
    const unsigned __int64 LockOwnerThreadID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledContentionStart_V2())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(ContentionFlags, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(LockID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AssociatedObjectID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(LockOwnerThreadID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventContentionStart_V2, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledContentionStop_V1(void)
{
    return ep_event_is_enabled(EventPipeEventContentionStop_V1);
}

ULONG EventPipeWriteEventContentionStop_V1(
    const unsigned char ContentionFlags,
    const unsigned short ClrInstanceID,
    const double DurationNs,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledContentionStop_V1())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(ContentionFlags, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(DurationNs, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventContentionStop_V1, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledContentionLockCreated(void)
{
    return ep_event_is_enabled(EventPipeEventContentionLockCreated);
}

ULONG EventPipeWriteEventContentionLockCreated(
    const void* LockID,
    const void* AssociatedObjectID,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledContentionLockCreated())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(LockID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AssociatedObjectID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventContentionLockCreated, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledThreadPoolWorkerThreadStart(void)
{
    return ep_event_is_enabled(EventPipeEventThreadPoolWorkerThreadStart);
}

ULONG EventPipeWriteEventThreadPoolWorkerThreadStart(
    const unsigned int ActiveWorkerThreadCount,
    const unsigned int RetiredWorkerThreadCount,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledThreadPoolWorkerThreadStart())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(ActiveWorkerThreadCount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(RetiredWorkerThreadCount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventThreadPoolWorkerThreadStart, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledThreadPoolWorkerThreadStop(void)
{
    return ep_event_is_enabled(EventPipeEventThreadPoolWorkerThreadStop);
}

ULONG EventPipeWriteEventThreadPoolWorkerThreadStop(
    const unsigned int ActiveWorkerThreadCount,
    const unsigned int RetiredWorkerThreadCount,
    const unsigned short ClrInstanceID,
    const GUID *  ActivityId,
    const GUID *  RelatedActivityId)
{
    if (!EventPipeEventEnabledThreadPoolWorkerThreadStop())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(ActiveWorkerThreadCount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(RetiredWorkerThreadCount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventThreadPoolWorkerThreadStop, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledThreadPoolWorkerThreadWait(void)
{
    return ep_event_is_enabled(EventPipeEventThreadPoolWorkerThreadWait);
}

ULONG EventPipeWriteEventThreadPoolWorkerThreadWait(
    const unsigned int ActiveWorkerThreadCount,
    const unsigned int RetiredWorkerThreadCount,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledThreadPoolWorkerThreadWait())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(ActiveWorkerThreadCount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(RetiredWorkerThreadCount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventThreadPoolWorkerThreadWait, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledThreadPoolMinMaxThreads(void)
{
    return ep_event_is_enabled(EventPipeEventThreadPoolMinMaxThreads);
}

ULONG EventPipeWriteEventThreadPoolMinMaxThreads(
    const unsigned short MinWorkerThreads,
    const unsigned short MaxWorkerThreads,
    const unsigned short MinIOCompletionThreads,
    const unsigned short MaxIOCompletionThreads,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledThreadPoolMinMaxThreads())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(MinWorkerThreads, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(MaxWorkerThreads, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(MinIOCompletionThreads, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(MaxIOCompletionThreads, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventThreadPoolMinMaxThreads, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledThreadPoolWorkerThreadAdjustmentSample(void)
{
    return ep_event_is_enabled(EventPipeEventThreadPoolWorkerThreadAdjustmentSample);
}

ULONG EventPipeWriteEventThreadPoolWorkerThreadAdjustmentSample(
    const double Throughput,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledThreadPoolWorkerThreadAdjustmentSample())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Throughput, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventThreadPoolWorkerThreadAdjustmentSample, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledThreadPoolWorkerThreadAdjustmentAdjustment(void)
{
    return ep_event_is_enabled(EventPipeEventThreadPoolWorkerThreadAdjustmentAdjustment);
}

ULONG EventPipeWriteEventThreadPoolWorkerThreadAdjustmentAdjustment(
    const double AverageThroughput,
    const unsigned int NewWorkerThreadCount,
    const unsigned int Reason,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledThreadPoolWorkerThreadAdjustmentAdjustment())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(AverageThroughput, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(NewWorkerThreadCount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Reason, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventThreadPoolWorkerThreadAdjustmentAdjustment, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledThreadPoolWorkerThreadAdjustmentStats(void)
{
    return ep_event_is_enabled(EventPipeEventThreadPoolWorkerThreadAdjustmentStats);
}

ULONG EventPipeWriteEventThreadPoolWorkerThreadAdjustmentStats(
    const double Duration,
    const double Throughput,
    const double ThreadWave,
    const double ThroughputWave,
    const double ThroughputErrorEstimate,
    const double AverageThroughputErrorEstimate,
    const double ThroughputRatio,
    const double Confidence,
    const double NewControlSetting,
    const unsigned short NewThreadWaveMagnitude,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledThreadPoolWorkerThreadAdjustmentStats())
        return ERROR_SUCCESS;

    size_t size = 76;
    BYTE stackBuffer[76];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Duration, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Throughput, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ThreadWave, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ThroughputWave, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ThroughputErrorEstimate, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AverageThroughputErrorEstimate, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ThroughputRatio, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Confidence, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(NewControlSetting, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(NewThreadWaveMagnitude, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventThreadPoolWorkerThreadAdjustmentStats, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledThreadPoolIOEnqueue(void)
{
    return ep_event_is_enabled(EventPipeEventThreadPoolIOEnqueue);
}

ULONG EventPipeWriteEventThreadPoolIOEnqueue(
    const void* NativeOverlapped,
    const void* Overlapped,
    const BOOL MultiDequeues,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledThreadPoolIOEnqueue())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(NativeOverlapped, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Overlapped, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(MultiDequeues, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventThreadPoolIOEnqueue, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledThreadPoolIODequeue(void)
{
    return ep_event_is_enabled(EventPipeEventThreadPoolIODequeue);
}

ULONG EventPipeWriteEventThreadPoolIODequeue(
    const void* NativeOverlapped,
    const void* Overlapped,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledThreadPoolIODequeue())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(NativeOverlapped, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Overlapped, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventThreadPoolIODequeue, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledThreadPoolWorkingThreadCount(void)
{
    return ep_event_is_enabled(EventPipeEventThreadPoolWorkingThreadCount);
}

ULONG EventPipeWriteEventThreadPoolWorkingThreadCount(
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledThreadPoolWorkingThreadCount())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventThreadPoolWorkingThreadCount, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledThreadPoolIOPack(void)
{
    return ep_event_is_enabled(EventPipeEventThreadPoolIOPack);
}

ULONG EventPipeWriteEventThreadPoolIOPack(
    const void* NativeOverlapped,
    const void* Overlapped,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledThreadPoolIOPack())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(NativeOverlapped, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Overlapped, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventThreadPoolIOPack, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;

    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCAllocationTick_V4(void)
{
    return ep_event_is_enabled(EventPipeEventGCAllocationTick_V4);
}

ULONG EventPipeWriteEventGCAllocationTick_V4(
    const unsigned int AllocationAmount,
    const unsigned int AllocationKind,
    const unsigned short ClrInstanceID,
    const unsigned __int64 AllocationAmount64,
    const void* TypeID,
    const WCHAR* TypeName,
    const unsigned int HeapIndex,
    const void* Address,
    const unsigned __int64 ObjectSize,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCAllocationTick_V4())
        return ERROR_SUCCESS;

    size_t size = 110;
    BYTE stackBuffer[110];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    if (!TypeName) { TypeName = W("NULL"); }
    success &= WriteToBuffer(AllocationAmount, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AllocationKind, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AllocationAmount64, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TypeID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TypeName, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(HeapIndex, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Address, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ObjectSize, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCAllocationTick_V4, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCHeapStats_V2(void)
{
    return ep_event_is_enabled(EventPipeEventGCHeapStats_V2);
}

ULONG EventPipeWriteEventGCHeapStats_V2(
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
    const unsigned __int64 GenerationSize4,
    const unsigned __int64 TotalPromotedSize4,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCHeapStats_V2())
        return ERROR_SUCCESS;

    size_t size = 110;
    BYTE stackBuffer[110];
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
    success &= WriteToBuffer(GenerationSize4, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TotalPromotedSize4, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCHeapStats_V2, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCSampledObjectAllocationHigh(void)
{
    return ep_event_is_enabled(EventPipeEventGCSampledObjectAllocationHigh);
}

ULONG EventPipeWriteEventGCSampledObjectAllocationHigh(
    const void* Address,
    const void* TypeID,
    const unsigned int ObjectCountForTypeSample,
    const unsigned __int64 TotalSizeForTypeSample,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCSampledObjectAllocationHigh())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Address, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TypeID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ObjectCountForTypeSample, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TotalSizeForTypeSample, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCSampledObjectAllocationHigh, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCSampledObjectAllocationLow(void)
{
    return ep_event_is_enabled(EventPipeEventGCSampledObjectAllocationLow);
}

ULONG EventPipeWriteEventGCSampledObjectAllocationLow(
    const void* Address,
    const void* TypeID,
    const unsigned int ObjectCountForTypeSample,
    const unsigned __int64 TotalSizeForTypeSample,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCSampledObjectAllocationLow())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Address, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TypeID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ObjectCountForTypeSample, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TotalSizeForTypeSample, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCSampledObjectAllocationLow, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledPinObjectAtGCTime(void)
{
    return ep_event_is_enabled(EventPipeEventPinObjectAtGCTime);
}

ULONG EventPipeWriteEventPinObjectAtGCTime(
    const void* HandleID,
    const void* ObjectID,
    const unsigned __int64 ObjectSize,
    const WCHAR* TypeName,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledPinObjectAtGCTime())
        return ERROR_SUCCESS;

    size_t size = 90;
    BYTE stackBuffer[90];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    if (!TypeName) { TypeName = W("NULL"); }
    success &= WriteToBuffer(HandleID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ObjectID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ObjectSize, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TypeName, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventPinObjectAtGCTime, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCBulkRootStaticVar(void)
{
    return ep_event_is_enabled(EventPipeEventGCBulkRootStaticVar);
}

ULONG EventPipeWriteEventGCBulkRootStaticVar(
    const unsigned int Count,
    const unsigned __int64 AppDomainID,
    const unsigned short ClrInstanceID,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCBulkRootStaticVar())
        return ERROR_SUCCESS;

    size_t size = 46;
    BYTE stackBuffer[46];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(AppDomainID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)(Values_ElementSize), buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCBulkRootStaticVar, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledIncreaseMemoryPressure(void)
{
    return ep_event_is_enabled(EventPipeEventIncreaseMemoryPressure);
}

ULONG EventPipeWriteEventIncreaseMemoryPressure(
    const unsigned __int64 BytesAllocated,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledIncreaseMemoryPressure())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(BytesAllocated, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventIncreaseMemoryPressure, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCGlobalHeapHistory_V4(void)
{
    return ep_event_is_enabled(EventPipeEventGCGlobalHeapHistory_V4);
}

ULONG EventPipeWriteEventGCGlobalHeapHistory_V4(
    const unsigned __int64 FinalYoungestDesired,
    const signed int NumHeaps,
    const unsigned int CondemnedGeneration,
    const unsigned int Gen0ReductionCount,
    const unsigned int Reason,
    const unsigned int GlobalMechanisms,
    const unsigned short ClrInstanceID,
    const unsigned int PauseMode,
    const unsigned int MemoryPressure,
    const unsigned int CondemnReasons0,
    const unsigned int CondemnReasons1,
    const unsigned int Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCGlobalHeapHistory_V4())
        return ERROR_SUCCESS;

    size_t size = 82;
    BYTE stackBuffer[82];
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
    success &= WriteToBuffer(CondemnReasons0, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(CondemnReasons1, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)Values_ElementSize * (int)Count, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCGlobalHeapHistory_V4, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGenAwareBegin(void)
{
    return ep_event_is_enabled(EventPipeEventGenAwareBegin);
}

ULONG EventPipeWriteEventGenAwareBegin(
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGenAwareBegin())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGenAwareBegin, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGenAwareEnd(void)
{
    return ep_event_is_enabled(EventPipeEventGenAwareEnd);
}

ULONG EventPipeWriteEventGenAwareEnd(
    const unsigned int Count,
    const unsigned short ClrInstanceID,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGenAwareEnd())
        return ERROR_SUCCESS;

    size_t size = 32;
    BYTE stackBuffer[32];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGenAwareEnd, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCLOHCompact(void)
{
    return ep_event_is_enabled(EventPipeEventGCLOHCompact);
}

ULONG EventPipeWriteEventGCLOHCompact(
    const unsigned short ClrInstanceID,
    const unsigned short Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCLOHCompact())
        return ERROR_SUCCESS;

    size_t size = 36;
    BYTE stackBuffer[36];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)Values_ElementSize * (int)Count, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCLOHCompact, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

BOOL EventPipeEventEnabledGCFitBucketInfo(void)
{
    return ep_event_is_enabled(EventPipeEventGCFitBucketInfo);
}

ULONG EventPipeWriteEventGCFitBucketInfo(
    const unsigned short ClrInstanceID,
    const unsigned short BucketKind,
    const unsigned __int64 TotalSize,
    const unsigned short Count,
    int Values_ElementSize,
    const void* Values,
    const GUID * ActivityId,
    const GUID * RelatedActivityId)
{
    if (!EventPipeEventEnabledGCFitBucketInfo())
        return ERROR_SUCCESS;

    size_t size = 46;
    BYTE stackBuffer[46];
    BYTE *buffer = stackBuffer;
    size_t offset = 0;
    bool fixedBuffer = true;
    bool success = true;

    success &= WriteToBuffer(ClrInstanceID, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(BucketKind, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(TotalSize, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer(Count, buffer, offset, size, fixedBuffer);
    success &= WriteToBuffer((const BYTE *)Values, (int)Values_ElementSize * (int)Count, buffer, offset, size, fixedBuffer);

    if (!success)
    {
        if (!fixedBuffer)
            delete[] buffer;
        return ERROR_WRITE_FAULT;
    }

    EventPipeAdapter::WriteEvent(EventPipeEventGCFitBucketInfo, (BYTE *)buffer, (unsigned int)offset, ActivityId, RelatedActivityId);

    if (!fixedBuffer)
        delete[] buffer;


    return ERROR_SUCCESS;
}

void InitProvidersAndEvents(void)
{
    InitDotNETRuntime();
}

void InitDotNETRuntime(void)
{
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    EventPipeProviderDotNETRuntime = EventPipeAdapter::CreateProvider(DotNETRuntimeName, reinterpret_cast<EventPipeCallback>(EventPipeEtwCallbackDotNETRuntime));
    EventPipeEventDestroyGCHandle = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,31,2,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventExceptionThrown_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,80,8589967360,1,EP_EVENT_LEVEL_ERROR,true);
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
    EventPipeEventGCStart_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,1,1,2,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCRestartEEEnd_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,3,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCRestartEEBegin_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,7,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCSuspendEEEnd_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,8,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCSuspendEEBegin_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,9,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventDecreaseMemoryPressure = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,201,1,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventFinalizeObject = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,29,1,0,EP_EVENT_LEVEL_VERBOSE,false);
    EventPipeEventGCFinalizersBegin_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,14,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCFinalizersEnd_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,13,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventContentionStart_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,81,16384,2,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventContentionStop_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,91,16384,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventContentionLockCreated = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,90,16384,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventThreadPoolWorkerThreadStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,50,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventThreadPoolWorkerThreadStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,51,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventThreadPoolWorkerThreadWait = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,57,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventThreadPoolMinMaxThreads = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,59,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventThreadPoolWorkerThreadAdjustmentSample = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,54,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventThreadPoolWorkerThreadAdjustmentAdjustment = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,55,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventThreadPoolWorkerThreadAdjustmentStats = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,56,65536,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventThreadPoolIOEnqueue = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,63,2147549184,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventThreadPoolIODequeue = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,64,2147549184,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventThreadPoolIOPack = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,65,65536,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventThreadPoolWorkingThreadCount = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,60,65536,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCAllocationTick_V4 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,10,1,4,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCHeapStats_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,4,1,2,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCSampledObjectAllocationHigh = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,20,2097152,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCSampledObjectAllocationLow = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,32,33554432,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventPinObjectAtGCTime = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,33,1,0,EP_EVENT_LEVEL_VERBOSE,false);
    EventPipeEventGCBulkRootStaticVar = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,38,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventIncreaseMemoryPressure = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,200,1,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCGlobalHeapHistory_V4 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,205,1,4,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGenAwareBegin = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,206,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGenAwareEnd = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,207,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCLOHCompact = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,208,1,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCFitBucketInfo = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,209,1,0,EP_EVENT_LEVEL_VERBOSE,true);
}

bool DotNETRuntimeProvider_IsEnabled(unsigned char level, unsigned long long keyword)
{
    if (!ep_enabled())
        return false;

    EVENTPIPE_TRACE_CONTEXT& context = MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context.EventPipeProvider;
    if (!context.IsEnabled)
        return false;

    if (level > context.Level)
        return false;

    return (keyword == (ULONGLONG)0) || (keyword & context.EnabledKeywordsBitmask) != 0;
}
