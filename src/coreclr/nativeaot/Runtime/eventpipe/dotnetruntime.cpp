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

#ifndef ERROR_WRITE_FAULT
#define ERROR_WRITE_FAULT 29L
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
EventPipeEvent *EventPipeEventGCStart = nullptr;
EventPipeEvent *EventPipeEventGCStart_V1 = nullptr;
EventPipeEvent *EventPipeEventGCStart_V2 = nullptr;
EventPipeEvent *EventPipeEventGCEnd = nullptr;
EventPipeEvent *EventPipeEventGCEnd_V1 = nullptr;
EventPipeEvent *EventPipeEventGCRestartEEEnd = nullptr;
EventPipeEvent *EventPipeEventGCRestartEEEnd_V1 = nullptr;
EventPipeEvent *EventPipeEventGCHeapStats = nullptr;
EventPipeEvent *EventPipeEventGCHeapStats_V1 = nullptr;
EventPipeEvent *EventPipeEventGCHeapStats_V2 = nullptr;
EventPipeEvent *EventPipeEventGCCreateSegment = nullptr;
EventPipeEvent *EventPipeEventGCCreateSegment_V1 = nullptr;
EventPipeEvent *EventPipeEventGCFreeSegment = nullptr;
EventPipeEvent *EventPipeEventGCFreeSegment_V1 = nullptr;
EventPipeEvent *EventPipeEventGCRestartEEBegin = nullptr;
EventPipeEvent *EventPipeEventGCRestartEEBegin_V1 = nullptr;
EventPipeEvent *EventPipeEventGCSuspendEEEnd = nullptr;
EventPipeEvent *EventPipeEventGCSuspendEEEnd_V1 = nullptr;
EventPipeEvent *EventPipeEventGCSuspendEEBegin = nullptr;
EventPipeEvent *EventPipeEventGCSuspendEEBegin_V1 = nullptr;
EventPipeEvent *EventPipeEventGCAllocationTick = nullptr;
EventPipeEvent *EventPipeEventGCAllocationTick_V1 = nullptr;
EventPipeEvent *EventPipeEventGCAllocationTick_V2 = nullptr;
EventPipeEvent *EventPipeEventGCAllocationTick_V3 = nullptr;
EventPipeEvent *EventPipeEventGCAllocationTick_V4 = nullptr;
EventPipeEvent *EventPipeEventGCCreateConcurrentThread = nullptr;
EventPipeEvent *EventPipeEventGCCreateConcurrentThread_V1 = nullptr;
EventPipeEvent *EventPipeEventGCTerminateConcurrentThread = nullptr;
EventPipeEvent *EventPipeEventGCTerminateConcurrentThread_V1 = nullptr;
EventPipeEvent *EventPipeEventGCFinalizersEnd = nullptr;
EventPipeEvent *EventPipeEventGCFinalizersEnd_V1 = nullptr;
EventPipeEvent *EventPipeEventGCFinalizersBegin = nullptr;
EventPipeEvent *EventPipeEventGCFinalizersBegin_V1 = nullptr;
EventPipeEvent *EventPipeEventBulkType = nullptr;
EventPipeEvent *EventPipeEventGCBulkRootEdge = nullptr;
EventPipeEvent *EventPipeEventGCBulkRootConditionalWeakTableElementEdge = nullptr;
EventPipeEvent *EventPipeEventGCBulkNode = nullptr;
EventPipeEvent *EventPipeEventGCBulkEdge = nullptr;
EventPipeEvent *EventPipeEventGCSampledObjectAllocationHigh = nullptr;
EventPipeEvent *EventPipeEventGCBulkSurvivingObjectRanges = nullptr;
EventPipeEvent *EventPipeEventGCBulkMovedObjectRanges = nullptr;
EventPipeEvent *EventPipeEventGCGenerationRange = nullptr;
EventPipeEvent *EventPipeEventGCMarkStackRoots = nullptr;
EventPipeEvent *EventPipeEventGCMarkFinalizeQueueRoots = nullptr;
EventPipeEvent *EventPipeEventGCMarkHandles = nullptr;
EventPipeEvent *EventPipeEventGCMarkOlderGenerationRoots = nullptr;
EventPipeEvent *EventPipeEventFinalizeObject = nullptr;
EventPipeEvent *EventPipeEventSetGCHandle = nullptr;
EventPipeEvent *EventPipeEventDestroyGCHandle = nullptr;
EventPipeEvent *EventPipeEventGCSampledObjectAllocationLow = nullptr;
EventPipeEvent *EventPipeEventPinObjectAtGCTime = nullptr;
EventPipeEvent *EventPipeEventGCTriggered = nullptr;
EventPipeEvent *EventPipeEventGCBulkRootCCW = nullptr;
EventPipeEvent *EventPipeEventGCBulkRCW = nullptr;
EventPipeEvent *EventPipeEventGCBulkRootStaticVar = nullptr;
EventPipeEvent *EventPipeEventGCDynamicEvent = nullptr;
EventPipeEvent *EventPipeEventWorkerThreadCreate = nullptr;
EventPipeEvent *EventPipeEventWorkerThreadTerminate = nullptr;
EventPipeEvent *EventPipeEventWorkerThreadRetire = nullptr;
EventPipeEvent *EventPipeEventWorkerThreadUnretire = nullptr;
EventPipeEvent *EventPipeEventIOThreadCreate = nullptr;
EventPipeEvent *EventPipeEventIOThreadCreate_V1 = nullptr;
EventPipeEvent *EventPipeEventIOThreadTerminate = nullptr;
EventPipeEvent *EventPipeEventIOThreadTerminate_V1 = nullptr;
EventPipeEvent *EventPipeEventIOThreadRetire = nullptr;
EventPipeEvent *EventPipeEventIOThreadRetire_V1 = nullptr;
EventPipeEvent *EventPipeEventIOThreadUnretire = nullptr;
EventPipeEvent *EventPipeEventIOThreadUnretire_V1 = nullptr;
EventPipeEvent *EventPipeEventThreadpoolSuspensionSuspendThread = nullptr;
EventPipeEvent *EventPipeEventThreadpoolSuspensionResumeThread = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadStart = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadStop = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadRetirementStart = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadRetirementStop = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadAdjustmentSample = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadAdjustmentAdjustment = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadAdjustmentStats = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkerThreadWait = nullptr;
EventPipeEvent *EventPipeEventYieldProcessorMeasurement = nullptr;
EventPipeEvent *EventPipeEventThreadPoolMinMaxThreads = nullptr;
EventPipeEvent *EventPipeEventThreadPoolWorkingThreadCount = nullptr;
EventPipeEvent *EventPipeEventThreadPoolEnqueue = nullptr;
EventPipeEvent *EventPipeEventThreadPoolDequeue = nullptr;
EventPipeEvent *EventPipeEventThreadPoolIOEnqueue = nullptr;
EventPipeEvent *EventPipeEventThreadPoolIODequeue = nullptr;
EventPipeEvent *EventPipeEventThreadPoolIOPack = nullptr;
EventPipeEvent *EventPipeEventThreadCreating = nullptr;
EventPipeEvent *EventPipeEventThreadRunning = nullptr;
EventPipeEvent *EventPipeEventMethodDetails = nullptr;
EventPipeEvent *EventPipeEventTypeLoadStart = nullptr;
EventPipeEvent *EventPipeEventTypeLoadStop = nullptr;
EventPipeEvent *EventPipeEventExceptionThrown = nullptr;
EventPipeEvent *EventPipeEventExceptionThrown_V1 = nullptr;
EventPipeEvent *EventPipeEventExceptionCatchStart = nullptr;
EventPipeEvent *EventPipeEventExceptionCatchStop = nullptr;
EventPipeEvent *EventPipeEventExceptionFinallyStart = nullptr;
EventPipeEvent *EventPipeEventExceptionFinallyStop = nullptr;
EventPipeEvent *EventPipeEventExceptionFilterStart = nullptr;
EventPipeEvent *EventPipeEventExceptionFilterStop = nullptr;
EventPipeEvent *EventPipeEventExceptionThrownStop = nullptr;
EventPipeEvent *EventPipeEventContention = nullptr;
EventPipeEvent *EventPipeEventContentionStart_V1 = nullptr;
EventPipeEvent *EventPipeEventContentionStart_V2 = nullptr;
EventPipeEvent *EventPipeEventContentionStop = nullptr;
EventPipeEvent *EventPipeEventContentionStop_V1 = nullptr;
EventPipeEvent *EventPipeEventLockCreated = nullptr;
EventPipeEvent *EventPipeEventCLRStackWalk = nullptr;
EventPipeEvent *EventPipeEventAppDomainMemAllocated = nullptr;
EventPipeEvent *EventPipeEventAppDomainMemSurvived = nullptr;
EventPipeEvent *EventPipeEventThreadCreated = nullptr;
EventPipeEvent *EventPipeEventThreadTerminated = nullptr;
EventPipeEvent *EventPipeEventThreadDomainEnter = nullptr;
EventPipeEvent *EventPipeEventILStubGenerated = nullptr;
EventPipeEvent *EventPipeEventILStubCacheHit = nullptr;
EventPipeEvent *EventPipeEventDCStartCompleteV2 = nullptr;
EventPipeEvent *EventPipeEventDCEndCompleteV2 = nullptr;
EventPipeEvent *EventPipeEventMethodDCStartV2 = nullptr;
EventPipeEvent *EventPipeEventMethodDCEndV2 = nullptr;
EventPipeEvent *EventPipeEventMethodDCStartVerboseV2 = nullptr;
EventPipeEvent *EventPipeEventMethodDCEndVerboseV2 = nullptr;
EventPipeEvent *EventPipeEventMethodLoad = nullptr;
EventPipeEvent *EventPipeEventMethodLoad_V1 = nullptr;
EventPipeEvent *EventPipeEventMethodLoad_V2 = nullptr;
EventPipeEvent *EventPipeEventR2RGetEntryPoint = nullptr;
EventPipeEvent *EventPipeEventR2RGetEntryPointStart = nullptr;
EventPipeEvent *EventPipeEventMethodUnload = nullptr;
EventPipeEvent *EventPipeEventMethodUnload_V1 = nullptr;
EventPipeEvent *EventPipeEventMethodUnload_V2 = nullptr;
EventPipeEvent *EventPipeEventMethodLoadVerbose = nullptr;
EventPipeEvent *EventPipeEventMethodLoadVerbose_V1 = nullptr;
EventPipeEvent *EventPipeEventMethodLoadVerbose_V2 = nullptr;
EventPipeEvent *EventPipeEventMethodUnloadVerbose = nullptr;
EventPipeEvent *EventPipeEventMethodUnloadVerbose_V1 = nullptr;
EventPipeEvent *EventPipeEventMethodUnloadVerbose_V2 = nullptr;
EventPipeEvent *EventPipeEventMethodJittingStarted = nullptr;
EventPipeEvent *EventPipeEventMethodJittingStarted_V1 = nullptr;
EventPipeEvent *EventPipeEventMethodJitMemoryAllocatedForCode = nullptr;
EventPipeEvent *EventPipeEventMethodJitInliningSucceeded = nullptr;
EventPipeEvent *EventPipeEventMethodJitInliningFailedAnsi = nullptr;
EventPipeEvent *EventPipeEventMethodJitTailCallSucceeded = nullptr;
EventPipeEvent *EventPipeEventMethodJitTailCallFailedAnsi = nullptr;
EventPipeEvent *EventPipeEventMethodILToNativeMap = nullptr;
EventPipeEvent *EventPipeEventMethodILToNativeMap_V1 = nullptr;
EventPipeEvent *EventPipeEventMethodJitTailCallFailed = nullptr;
EventPipeEvent *EventPipeEventMethodJitInliningFailed = nullptr;
EventPipeEvent *EventPipeEventModuleDCStartV2 = nullptr;
EventPipeEvent *EventPipeEventModuleDCEndV2 = nullptr;
EventPipeEvent *EventPipeEventDomainModuleLoad = nullptr;
EventPipeEvent *EventPipeEventDomainModuleLoad_V1 = nullptr;
EventPipeEvent *EventPipeEventModuleLoad = nullptr;
EventPipeEvent *EventPipeEventModuleLoad_V1 = nullptr;
EventPipeEvent *EventPipeEventModuleLoad_V2 = nullptr;
EventPipeEvent *EventPipeEventModuleUnload = nullptr;
EventPipeEvent *EventPipeEventModuleUnload_V1 = nullptr;
EventPipeEvent *EventPipeEventModuleUnload_V2 = nullptr;
EventPipeEvent *EventPipeEventAssemblyLoad = nullptr;
EventPipeEvent *EventPipeEventAssemblyLoad_V1 = nullptr;
EventPipeEvent *EventPipeEventAssemblyUnload = nullptr;
EventPipeEvent *EventPipeEventAssemblyUnload_V1 = nullptr;
EventPipeEvent *EventPipeEventAppDomainLoad = nullptr;
EventPipeEvent *EventPipeEventAppDomainLoad_V1 = nullptr;
EventPipeEvent *EventPipeEventAppDomainUnload = nullptr;
EventPipeEvent *EventPipeEventAppDomainUnload_V1 = nullptr;
EventPipeEvent *EventPipeEventModuleRangeLoad = nullptr;
EventPipeEvent *EventPipeEventStrongNameVerificationStart = nullptr;
EventPipeEvent *EventPipeEventStrongNameVerificationStart_V1 = nullptr;
EventPipeEvent *EventPipeEventStrongNameVerificationStop = nullptr;
EventPipeEvent *EventPipeEventStrongNameVerificationStop_V1 = nullptr;
EventPipeEvent *EventPipeEventAuthenticodeVerificationStart = nullptr;
EventPipeEvent *EventPipeEventAuthenticodeVerificationStart_V1 = nullptr;
EventPipeEvent *EventPipeEventAuthenticodeVerificationStop = nullptr;
EventPipeEvent *EventPipeEventAuthenticodeVerificationStop_V1 = nullptr;
EventPipeEvent *EventPipeEventRuntimeInformationStart = nullptr;
EventPipeEvent *EventPipeEventIncreaseMemoryPressure = nullptr;
EventPipeEvent *EventPipeEventDecreaseMemoryPressure = nullptr;
EventPipeEvent *EventPipeEventGCMarkWithType = nullptr;
EventPipeEvent *EventPipeEventGCJoin_V2 = nullptr;
EventPipeEvent *EventPipeEventGCPerHeapHistory_V3 = nullptr;
EventPipeEvent *EventPipeEventGCGlobalHeapHistory_V2 = nullptr;
EventPipeEvent *EventPipeEventGCGlobalHeapHistory_V3 = nullptr;
EventPipeEvent *EventPipeEventGCGlobalHeapHistory_V4 = nullptr;
EventPipeEvent *EventPipeEventGenAwareBegin = nullptr;
EventPipeEvent *EventPipeEventGenAwareEnd = nullptr;
EventPipeEvent *EventPipeEventGCLOHCompact = nullptr;
EventPipeEvent *EventPipeEventGCFitBucketInfo = nullptr;
EventPipeEvent *EventPipeEventDebugIPCEventStart = nullptr;
EventPipeEvent *EventPipeEventDebugIPCEventEnd = nullptr;
EventPipeEvent *EventPipeEventDebugExceptionProcessingStart = nullptr;
EventPipeEvent *EventPipeEventDebugExceptionProcessingEnd = nullptr;
EventPipeEvent *EventPipeEventCodeSymbols = nullptr;
EventPipeEvent *EventPipeEventEventSource = nullptr;
EventPipeEvent *EventPipeEventTieredCompilationSettings = nullptr;
EventPipeEvent *EventPipeEventTieredCompilationPause = nullptr;
EventPipeEvent *EventPipeEventTieredCompilationResume = nullptr;
EventPipeEvent *EventPipeEventTieredCompilationBackgroundJitStart = nullptr;
EventPipeEvent *EventPipeEventTieredCompilationBackgroundJitStop = nullptr;
EventPipeEvent *EventPipeEventAssemblyLoadStart = nullptr;
EventPipeEvent *EventPipeEventAssemblyLoadStop = nullptr;
EventPipeEvent *EventPipeEventResolutionAttempted = nullptr;
EventPipeEvent *EventPipeEventAssemblyLoadContextResolvingHandlerInvoked = nullptr;
EventPipeEvent *EventPipeEventAppDomainAssemblyResolveHandlerInvoked = nullptr;
EventPipeEvent *EventPipeEventAssemblyLoadFromResolveHandlerInvoked = nullptr;
EventPipeEvent *EventPipeEventKnownPathProbed = nullptr;
EventPipeEvent *EventPipeEventJitInstrumentationData = nullptr;
EventPipeEvent *EventPipeEventJitInstrumentationDataVerbose = nullptr;
EventPipeEvent *EventPipeEventProfilerMessage = nullptr;
EventPipeEvent *EventPipeEventExecutionCheckpoint = nullptr;


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

BOOL EventPipeEventEnabledMethodJitInliningFailedAnsi(void)
{
    return EventPipeAdapter::EventIsEnabled(EventPipeEventMethodJitInliningFailedAnsi);
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
    EventPipeEventGCStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,1,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCStart_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,1,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCStart_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,1,1,2,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCEnd = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,2,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCEnd_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,2,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCRestartEEEnd = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,3,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCRestartEEEnd_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,3,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCHeapStats = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,4,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCHeapStats_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,4,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCHeapStats_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,4,1,2,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCCreateSegment = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,5,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCCreateSegment_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,5,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCFreeSegment = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,6,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCFreeSegment_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,6,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCRestartEEBegin = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,7,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCRestartEEBegin_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,7,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCSuspendEEEnd = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,8,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCSuspendEEEnd_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,8,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCSuspendEEBegin = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,9,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCSuspendEEBegin_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,9,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCAllocationTick = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,10,1,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCAllocationTick_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,10,1,1,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCAllocationTick_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,10,1,2,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCAllocationTick_V3 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,10,1,3,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCAllocationTick_V4 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,10,1,4,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCCreateConcurrentThread = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,11,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCCreateConcurrentThread_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,11,65537,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCTerminateConcurrentThread = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,12,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCTerminateConcurrentThread_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,12,65537,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCFinalizersEnd = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,13,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCFinalizersEnd_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,13,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCFinalizersBegin = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,14,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCFinalizersBegin_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,14,1,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventBulkType = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,15,524288,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkRootEdge = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,16,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkRootConditionalWeakTableElementEdge = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,17,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkNode = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,18,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkEdge = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,19,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCSampledObjectAllocationHigh = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,20,2097152,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCBulkSurvivingObjectRanges = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,21,4194304,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkMovedObjectRanges = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,22,4194304,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCGenerationRange = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,23,4194304,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCMarkStackRoots = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,25,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCMarkFinalizeQueueRoots = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,26,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCMarkHandles = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,27,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCMarkOlderGenerationRoots = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,28,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventFinalizeObject = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,29,1,0,EP_EVENT_LEVEL_VERBOSE,false);
    EventPipeEventSetGCHandle = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,30,2,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventDestroyGCHandle = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,31,2,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCSampledObjectAllocationLow = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,32,33554432,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventPinObjectAtGCTime = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,33,1,0,EP_EVENT_LEVEL_VERBOSE,false);
    EventPipeEventGCTriggered = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,35,1,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCBulkRootCCW = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,36,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkRCW = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,37,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCBulkRootStaticVar = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,38,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCDynamicEvent = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,39,66060291,0,EP_EVENT_LEVEL_LOGALWAYS,true);
    EventPipeEventWorkerThreadCreate = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,40,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventWorkerThreadTerminate = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,41,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventWorkerThreadRetire = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,42,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventWorkerThreadUnretire = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,43,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventIOThreadCreate = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,44,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventIOThreadCreate_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,44,65536,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventIOThreadTerminate = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,45,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventIOThreadTerminate_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,45,65536,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventIOThreadRetire = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,46,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventIOThreadRetire_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,46,65536,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventIOThreadUnretire = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,47,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventIOThreadUnretire_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,47,65536,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventThreadpoolSuspensionSuspendThread = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,48,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventThreadpoolSuspensionResumeThread = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,49,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventThreadPoolWorkerThreadStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,50,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventThreadPoolWorkerThreadStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,51,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventThreadPoolWorkerThreadRetirementStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,52,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventThreadPoolWorkerThreadRetirementStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,53,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventThreadPoolWorkerThreadAdjustmentSample = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,54,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventThreadPoolWorkerThreadAdjustmentAdjustment = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,55,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventThreadPoolWorkerThreadAdjustmentStats = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,56,65536,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventThreadPoolWorkerThreadWait = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,57,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventYieldProcessorMeasurement = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,58,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventThreadPoolMinMaxThreads = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,59,65536,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventThreadPoolWorkingThreadCount = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,60,65536,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventThreadPoolEnqueue = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,61,2147549184,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventThreadPoolDequeue = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,62,2147549184,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventThreadPoolIOEnqueue = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,63,2147549184,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventThreadPoolIODequeue = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,64,2147549184,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventThreadPoolIOPack = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,65,65536,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventThreadCreating = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,70,2147549184,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventThreadRunning = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,71,2147549184,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventMethodDetails = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,72,274877906944,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventTypeLoadStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,73,549755813888,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventTypeLoadStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,74,549755813888,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventExceptionThrown = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,80,0,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventExceptionThrown_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,80,8589967360,1,EP_EVENT_LEVEL_ERROR,true);
    EventPipeEventExceptionCatchStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,250,32768,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventExceptionCatchStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,251,32768,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventExceptionFinallyStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,252,32768,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventExceptionFinallyStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,253,32768,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventExceptionFilterStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,254,32768,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventExceptionFilterStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,255,32768,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventExceptionThrownStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,256,32768,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventContention = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,81,0,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventContentionStart_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,81,16384,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventContentionStart_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,81,16384,2,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventContentionStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,91,16384,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventContentionStop_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,91,16384,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventLockCreated = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,90,16384,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventCLRStackWalk = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,82,1073741824,0,EP_EVENT_LEVEL_LOGALWAYS,false);
    EventPipeEventAppDomainMemAllocated = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,83,2048,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAppDomainMemSurvived = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,84,2048,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventThreadCreated = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,85,67584,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventThreadTerminated = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,86,67584,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventThreadDomainEnter = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,87,67584,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventILStubGenerated = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,88,8192,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventILStubCacheHit = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,89,8192,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventDCStartCompleteV2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,135,48,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventDCEndCompleteV2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,136,48,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventMethodDCStartV2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,137,48,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventMethodDCEndV2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,138,48,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventMethodDCStartVerboseV2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,139,48,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventMethodDCEndVerboseV2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,140,48,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventMethodLoad = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,141,48,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventMethodLoad_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,141,48,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventMethodLoad_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,141,48,2,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventR2RGetEntryPoint = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,159,137438953472,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventR2RGetEntryPointStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,160,137438953472,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventMethodUnload = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,142,48,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventMethodUnload_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,142,48,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventMethodUnload_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,142,48,2,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventMethodLoadVerbose = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,143,48,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventMethodLoadVerbose_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,143,48,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventMethodLoadVerbose_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,143,48,2,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventMethodUnloadVerbose = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,144,48,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventMethodUnloadVerbose_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,144,48,1,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventMethodUnloadVerbose_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,144,48,2,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventMethodJittingStarted = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,145,16,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventMethodJittingStarted_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,145,16,1,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventMethodJitMemoryAllocatedForCode = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,146,16,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventMethodJitInliningSucceeded = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,185,4096,0,EP_EVENT_LEVEL_VERBOSE,false);
    EventPipeEventMethodJitInliningFailedAnsi = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,186,4096,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventMethodJitTailCallSucceeded = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,188,4096,0,EP_EVENT_LEVEL_VERBOSE,false);
    EventPipeEventMethodJitTailCallFailedAnsi = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,189,4096,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventMethodILToNativeMap = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,190,131072,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventMethodILToNativeMap_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,190,131072,1,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventMethodJitTailCallFailed = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,191,4096,0,EP_EVENT_LEVEL_VERBOSE,false);
    EventPipeEventMethodJitInliningFailed = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,192,4096,0,EP_EVENT_LEVEL_VERBOSE,false);
    EventPipeEventModuleDCStartV2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,149,8,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventModuleDCEndV2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,150,8,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventDomainModuleLoad = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,151,8,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventDomainModuleLoad_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,151,8,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventModuleLoad = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,152,8,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventModuleLoad_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,152,536870920,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventModuleLoad_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,152,536870920,2,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventModuleUnload = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,153,8,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventModuleUnload_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,153,536870920,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventModuleUnload_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,153,536870920,2,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAssemblyLoad = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,154,8,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAssemblyLoad_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,154,8,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAssemblyUnload = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,155,8,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAssemblyUnload_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,155,8,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAppDomainLoad = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,156,8,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAppDomainLoad_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,156,8,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAppDomainUnload = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,157,8,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAppDomainUnload_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,157,8,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventModuleRangeLoad = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,158,536870912,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventStrongNameVerificationStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,181,1024,0,EP_EVENT_LEVEL_VERBOSE,false);
    EventPipeEventStrongNameVerificationStart_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,181,1024,1,EP_EVENT_LEVEL_VERBOSE,false);
    EventPipeEventStrongNameVerificationStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,182,1024,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventStrongNameVerificationStop_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,182,1024,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAuthenticodeVerificationStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,183,1024,0,EP_EVENT_LEVEL_VERBOSE,false);
    EventPipeEventAuthenticodeVerificationStart_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,183,1024,1,EP_EVENT_LEVEL_VERBOSE,false);
    EventPipeEventAuthenticodeVerificationStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,184,1024,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAuthenticodeVerificationStop_V1 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,184,1024,1,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventRuntimeInformationStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,187,0,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventIncreaseMemoryPressure = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,200,1,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventDecreaseMemoryPressure = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,201,1,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCMarkWithType = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,202,1,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCJoin_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,203,1,2,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventGCPerHeapHistory_V3 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,204,1,3,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventGCGlobalHeapHistory_V2 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,205,1,2,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCGlobalHeapHistory_V3 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,205,1,3,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCGlobalHeapHistory_V4 = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,205,1,4,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGenAwareBegin = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,206,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGenAwareEnd = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,207,1048576,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCLOHCompact = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,208,1,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventGCFitBucketInfo = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,209,1,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventDebugIPCEventStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,240,4294967296,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventDebugIPCEventEnd = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,241,4294967296,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventDebugExceptionProcessingStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,242,4294967296,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventDebugExceptionProcessingEnd = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,243,4294967296,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventCodeSymbols = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,260,17179869184,0,EP_EVENT_LEVEL_VERBOSE,true);
    EventPipeEventEventSource = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,270,34359738368,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventTieredCompilationSettings = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,280,68719476736,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventTieredCompilationPause = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,281,68719476736,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventTieredCompilationResume = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,282,68719476736,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventTieredCompilationBackgroundJitStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,283,68719476736,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventTieredCompilationBackgroundJitStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,284,68719476736,0,EP_EVENT_LEVEL_INFORMATIONAL,false);
    EventPipeEventAssemblyLoadStart = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,290,4,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAssemblyLoadStop = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,291,4,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventResolutionAttempted = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,292,4,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAssemblyLoadContextResolvingHandlerInvoked = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,293,4,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAppDomainAssemblyResolveHandlerInvoked = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,294,4,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventAssemblyLoadFromResolveHandlerInvoked = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,295,4,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventKnownPathProbed = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,296,4,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventJitInstrumentationData = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,297,1099511627776,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventJitInstrumentationDataVerbose = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,298,1099511627776,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventProfilerMessage = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,299,2199023255552,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
    EventPipeEventExecutionCheckpoint = EventPipeAdapter::AddEvent(EventPipeProviderDotNETRuntime,300,536870912,0,EP_EVENT_LEVEL_INFORMATIONAL,true);
}
