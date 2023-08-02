// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: eventtrace.cpp
// Abstract: This module implements Event Tracing support
//
// ============================================================================

#include "common.h"

#include "gcenv.h"
#include "gcheaputilities.h"

#include "daccess.h"

#include "eventtrace_etw.h"
#include "eventtracebase.h"
#include "eventtrace_context.h"

#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"

EVENTPIPE_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context = { W("Microsoft-Windows-DotNETRuntime"), 0, false, 0 };

DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context = {
#ifdef FEATURE_ETW
    &MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
#endif
    MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context
};

volatile LONGLONG ETW::GCLog::s_l64LastClientSequenceNumber = 0;

//---------------------------------------------------------------------------------------
//
// Helper to fire the GCStart event.  Figures out which version of GCStart to fire, and
// includes the client sequence number, if available.
//
// Arguments:
//      pGcInfo - ETW_GC_INFO containing details from GC about this collection
//

// static
void ETW::GCLog::FireGcStart(ETW_GC_INFO* pGcInfo)
{
    LIMITED_METHOD_CONTRACT;

    if (RUNTIME_PROVIDER_CATEGORY_ENABLED(TRACE_LEVEL_INFORMATION, CLR_GC_KEYWORD))
    {
        // If the controller specified a client sequence number for us to log with this
        // GCStart, then retrieve it
        LONGLONG l64ClientSequenceNumberToLog = 0;
        if ((s_l64LastClientSequenceNumber != 0) &&
            (pGcInfo->GCStart.Depth == GCHeapUtilities::GetGCHeap()->GetMaxGeneration()) &&
            (pGcInfo->GCStart.Reason == ETW_GC_INFO::GC_INDUCED))
        {
            // No InterlockedExchange64 on Redhawk (presumably b/c there is no compiler
            // intrinsic for this on x86, even though there is one for InterlockedCompareExchange64)
            l64ClientSequenceNumberToLog = PalInterlockedCompareExchange64(&s_l64LastClientSequenceNumber, 0, s_l64LastClientSequenceNumber);
        }

        FireEtwGCStart_V2(pGcInfo->GCStart.Count, pGcInfo->GCStart.Depth, pGcInfo->GCStart.Reason, pGcInfo->GCStart.Type, GetClrInstanceId(), l64ClientSequenceNumberToLog);
    }
}

void EventTracing_Initialize()
{
#ifdef FEATURE_ETW
    MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.IsEnabled = FALSE;
    MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.IsEnabled = FALSE;

    // Register the ETW providers with the system.
    RH_ETW_REGISTER_Microsoft_Windows_DotNETRuntimePrivate();
    RH_ETW_REGISTER_Microsoft_Windows_DotNETRuntime();

    MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_DotNETRuntimePrivateHandle;
    MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_DotNETRuntimeHandle;
#endif // FEATURE_ETW
}

enum CallbackProviderIndex
{
    DotNETRuntime = 0,
    DotNETRuntimeRundown = 1,
    DotNETRuntimeStress = 2,
    DotNETRuntimePrivate = 3
};

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

#ifdef FEATURE_ETW
//
// -----------------------------------------------------------------------------------------------------------
//
// The automatically generated part of the Redhawk ETW infrastructure (EtwEvents.h) calls the following
// function whenever the system enables or disables tracing for this provider.
//

void EtwCallback(
    GUID * /*SourceId*/,
    uint32_t IsEnabled,
    uint8_t Level,
    uint64_t MatchAnyKeyword,
    uint64_t MatchAllKeyword,
    EVENT_FILTER_DESCRIPTOR * FilterData,
    void * CallbackContext)
{
    RH_ETW_CONTEXT * pContext = (RH_ETW_CONTEXT*)CallbackContext;
    if (pContext == NULL)
        return;

    pContext->Level = Level;
    pContext->MatchAnyKeyword = MatchAnyKeyword;
    pContext->MatchAllKeyword = MatchAllKeyword;
    pContext->FilterData = FilterData;
    pContext->IsEnabled = IsEnabled;

    GCHeapUtilities::RecordEventStateChange(!!(pContext->RegistrationHandle == Microsoft_Windows_DotNETRuntimeHandle),
                                            static_cast<GCEventKeyword>(pContext->MatchAnyKeyword),
                                            static_cast<GCEventLevel>(pContext->Level));

    if (IsEnabled &&
        (pContext->RegistrationHandle == Microsoft_Windows_DotNETRuntimePrivateHandle) &&
        GCHeapUtilities::IsGCHeapInitialized())
    {
        FireEtwGCSettings(GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(FALSE),
                          GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(TRUE),
                          GCHeapUtilities::IsServerHeap());
        GCHeapUtilities::GetGCHeap()->DiagTraceGCSegments();
    }

    // Special check for the runtime provider's ManagedHeapCollectKeyword.  Profilers
    // flick this to force a full GC.
    if (IsEnabled &&
        (pContext->RegistrationHandle == Microsoft_Windows_DotNETRuntimeHandle) &&
        GCHeapUtilities::IsGCHeapInitialized() &&
        ((pContext->MatchAnyKeyword & CLR_MANAGEDHEAPCOLLECT_KEYWORD) != 0))
    {
        // Profilers may (optionally) specify extra data in the filter parameter
        // to log with the GCStart event.
        LONGLONG l64ClientSequenceNumber = 0;
        if ((pContext->FilterData != NULL) &&
            (pContext->FilterData->Type == 1) &&
            (pContext->FilterData->Size == sizeof(l64ClientSequenceNumber)))
        {
            l64ClientSequenceNumber = *(LONGLONG *) (pContext->FilterData->Ptr);
        }
        ETW::GCLog::ForceGC(l64ClientSequenceNumber);
    }
}
#endif // FEATURE_ETW

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
