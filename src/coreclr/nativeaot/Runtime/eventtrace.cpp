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

#include <minipal/utf8.h>

EVENTPIPE_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context = { W("Microsoft-Windows-DotNETRuntime"), 0, false, 0 };
DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context = {
#ifdef FEATURE_ETW
    &MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context,
#endif
    MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context
};

EVENTPIPE_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context = { W("Microsoft-Windows-DotNETRuntimeRundown"), 0, false, 0 };
DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context = {
#ifdef FEATURE_ETW
    &MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context,
#endif
    MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context
};

EVENTPIPE_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_EVENTPIPE_Context = { W("Microsoft-Windows-DotNETRuntimePrivate"), 0, false, 0 };
DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context = {
#ifdef FEATURE_ETW
    &MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context,
#endif
    MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_EVENTPIPE_Context
};

bool IsRuntimeProviderEnabled(uint8_t level, uint64_t keyword)
{
    return RUNTIME_PROVIDER_CATEGORY_ENABLED(level, keyword);
}

bool IsRuntimeRundownProviderEnabled(uint8_t level, uint64_t keyword)
{
    return RUNTIME_RUNDOWN_PROVIDER_CATEGORY_ENABLED(level, keyword);
}

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
            // No InterlockedExchange64 on NativeAOT (presumably b/c there is no compiler
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
    MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context.IsEnabled = FALSE;

    // Register the ETW providers with the system.
    EventRegisterMicrosoft_Windows_DotNETRuntimePrivate();
    EventRegisterMicrosoft_Windows_DotNETRuntime();
    EventRegisterMicrosoft_Windows_DotNETRuntimeRundown();

    MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_DotNETRuntimePrivateHandle;
    MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_DotNETRuntimeHandle;
    MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_Context.RegistrationHandle = Microsoft_Windows_DotNETRuntimeRundownHandle;
#endif // FEATURE_ETW
}

enum CallbackProviderIndex
{
    DotNETRuntime = 0,
    DotNETRuntimeRundown = 1,
    DotNETRuntimeStress = 2,
    DotNETRuntimePrivate = 3
};

enum SessionChange
{
    EventPipeSessionDisable = 0,
    EventPipeSessionEnable = 1,
    EtwSessionChangeUnknown = 2
};

#ifdef FEATURE_ETW
// EventFilterType identifies the filter type used by the PEVENT_FILTER_DESCRIPTOR
enum EventFilterType
{
    // data should be pairs of UTF8 null terminated strings all concatenated together.
    // The first element of the pair is the key and the 2nd is the value. We expect one of the
    // keys to be the string "GCSeqNumber" and the value to be a number encoded as text.
    // This is the standard way EventPipe encodes filter values
    StringKeyValueEncoding = 0,
    // data should be an 8 byte binary LONGLONG value
    // this is the historic encoding defined by .NET Framework for use with ETW
    LongBinaryClientSequenceNumber = 1
};

void ParseFilterDataClientSequenceNumber(
    EVENT_FILTER_DESCRIPTOR * FilterData,
    LONGLONG * pClientSequenceNumber)
{
    if (FilterData == NULL)
        return;

    if (FilterData->Type == LongBinaryClientSequenceNumber && FilterData->Size == sizeof(LONGLONG))
    {
        *pClientSequenceNumber = *(LONGLONG *) (FilterData->Ptr);
    }
    else if (FilterData->Type == StringKeyValueEncoding)
    {
        const char* buffer = reinterpret_cast<const char*>(FilterData->Ptr);
        const char* buffer_end = buffer + FilterData->Size;

        while (buffer < buffer_end)
        {
            const char* key = buffer;
            size_t key_len = strnlen(key, buffer_end - buffer);
            buffer += key_len + 1;

            if (buffer >= buffer_end)
                break;

            const char* value = buffer;
            size_t value_len = strnlen(value, buffer_end - buffer);
            buffer += value_len + 1;

            if (buffer > buffer_end)
                break;

            if (strcmp(key, "GCSeqNumber") != 0)
                continue;

            char* endPtr = nullptr;
            long parsedValue = strtol(value, &endPtr, 10);
            if (endPtr != value && *endPtr == '\0')
            {
                *pClientSequenceNumber = static_cast<LONGLONG>(parsedValue);
                break;
            }
        }
    }
}
#endif // FEATURE_ETW

// NOTE: When multiple ETW or EventPipe sessions are enabled, the ControlCode will be
// EVENT_CONTROL_CODE_ENABLE_PROVIDER even if the session invoking this callback is being disabled.
void EtwCallbackCommon(
    CallbackProviderIndex ProviderIndex,
    ULONG ControlCode,
    unsigned char Level,
    ULONGLONG MatchAnyKeyword,
    PVOID pFilterData,
    SessionChange Change)
{
//     LIMITED_METHOD_CONTRACT;

    bool bIsPublicTraceHandle = ProviderIndex == DotNETRuntime;

    DOTNET_TRACE_CONTEXT * ctxToUpdate;
    switch(ProviderIndex)
    {
    case DotNETRuntime:
        ctxToUpdate = &MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context;
        break;
    case DotNETRuntimeRundown:
        ctxToUpdate = &MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context;
        break;
    case DotNETRuntimePrivate:
        ctxToUpdate = &MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context;
        break;

    default:
        _ASSERTE(!"EtwCallbackCommon was called with invalid context");
        return;
    }

    // This callback gets called on both ETW/EventPipe session enable/disable.
    // We need toupdate the EventPipe provider context if we are in a callback
    // from EventPipe, but not from ETW.
    if (Change == EventPipeSessionEnable || Change == EventPipeSessionDisable)
    {
        ctxToUpdate->EventPipeProvider.Level = Level;
        ctxToUpdate->EventPipeProvider.EnabledKeywordsBitmask = MatchAnyKeyword;
        ctxToUpdate->EventPipeProvider.IsEnabled = ControlCode;

        // For EventPipe, ControlCode can only be either 0 or 1.
        _ASSERTE(ControlCode == EVENT_CONTROL_CODE_DISABLE_PROVIDER || ControlCode == EVENT_CONTROL_CODE_ENABLE_PROVIDER);
    }

    if (
#ifdef FEATURE_ETW
        (ControlCode == EVENT_CONTROL_CODE_ENABLE_PROVIDER || ControlCode == EVENT_CONTROL_CODE_DISABLE_PROVIDER) &&
#endif
        (ProviderIndex == DotNETRuntime || ProviderIndex == DotNETRuntimePrivate))
    {
#ifdef FEATURE_ETW
        // Consolidate level and keywords across event pipe and ETW contexts.
        // ETW may still want to see events that event pipe doesn't care about and vice versa
        GCEventKeyword keywords = static_cast<GCEventKeyword>(ctxToUpdate->EventPipeProvider.EnabledKeywordsBitmask |
                                                              ctxToUpdate->EtwProvider->MatchAnyKeyword);
        GCEventLevel level = static_cast<GCEventLevel>(max(ctxToUpdate->EventPipeProvider.Level,
                                                           ctxToUpdate->EtwProvider->Level));
#else
        GCEventKeyword keywords = static_cast<GCEventKeyword>(ctxToUpdate->EventPipeProvider.EnabledKeywordsBitmask);
        GCEventLevel level = static_cast<GCEventLevel>(ctxToUpdate->EventPipeProvider.Level);
#endif
        GCHeapUtilities::RecordEventStateChange(bIsPublicTraceHandle, keywords, level);
    }

    // Special check for a profiler requested GC.
    // A full GC will be forced if:
    // 1. The GC Heap is initialized.
    // 2. The public provider is requesting GC.
    // 3. The provider's ManagedHeapCollectKeyword is enabled.
    // 4. If it is an ETW provider, the control code is to enable or capture the state of the provider.
    // 5. If it is an EventPipe provider, the session is not being disabled.
    bool bValidGCRequest =
        GCHeapUtilities::IsGCHeapInitialized() &&
        bIsPublicTraceHandle &&
        ((MatchAnyKeyword & CLR_MANAGEDHEAPCOLLECT_KEYWORD) != 0) &&
        ((ControlCode == EVENT_CONTROL_CODE_ENABLE_PROVIDER) ||
         (ControlCode == EVENT_CONTROL_CODE_CAPTURE_STATE)) &&
        ((Change == EtwSessionChangeUnknown) ||
         (Change == EventPipeSessionEnable));

    if (bValidGCRequest)
    {
        // Profilers may (optionally) specify extra data in the filter parameter
        // to log with the GCStart event.
        LONGLONG l64ClientSequenceNumber = 0;
#if !defined(HOST_UNIX)
        ParseFilterDataClientSequenceNumber((EVENT_FILTER_DESCRIPTOR*)pFilterData, &l64ClientSequenceNumber);
#endif // !defined(HOST_UNIX)
        ETW::GCLog::ForceGC(l64ClientSequenceNumber);
    }
}

#ifdef FEATURE_ETW

void EtwCallback(
    const GUID * /*SourceId*/,
    uint32_t ControlCode,
    uint8_t Level,
    uint64_t MatchAnyKeyword,
    uint64_t MatchAllKeyword,
    EVENT_FILTER_DESCRIPTOR * FilterData,
    void * CallbackContext)
{
    MCGEN_TRACE_CONTEXT * context = (MCGEN_TRACE_CONTEXT*)CallbackContext;
    if (context == NULL)
        return;

    // A manifest based provider can be enabled to multiple event tracing sessions
    // As long as there is atleast 1 enabled session, IsEnabled will be TRUE
    // Since classic providers can be enabled to only a single session,
    // IsEnabled will be TRUE when it is enabled and FALSE when disabled
    BOOL bEnabled =
        ((ControlCode == EVENT_CONTROL_CODE_ENABLE_PROVIDER) ||
            (ControlCode == EVENT_CONTROL_CODE_CAPTURE_STATE));

    context->Level = Level;
    context->MatchAnyKeyword = MatchAnyKeyword;
    context->MatchAllKeyword = MatchAllKeyword;
    context->IsEnabled = bEnabled;

    CallbackProviderIndex providerIndex = DotNETRuntime;
    DOTNET_TRACE_CONTEXT providerContext = MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context;
    if (context->RegistrationHandle == Microsoft_Windows_DotNETRuntimeHandle) {
        providerIndex = DotNETRuntime;
        providerContext = MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context;
    } else if (context->RegistrationHandle == Microsoft_Windows_DotNETRuntimeRundownHandle) {
        providerIndex = DotNETRuntimeRundown;
        providerContext = MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context;
    } else if (context->RegistrationHandle == Microsoft_Windows_DotNETRuntimePrivateHandle) {
        providerIndex = DotNETRuntimePrivate;
        providerContext = MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context;
    } else {
        _ASSERTE(!"Unknown registration handle");
        return;
    }

    EtwCallbackCommon(providerIndex, ControlCode, Level, MatchAnyKeyword, FilterData, EtwSessionChangeUnknown);

    if (bEnabled)
    {
        if (context->RegistrationHandle == Microsoft_Windows_DotNETRuntimePrivateHandle &&
            GCHeapUtilities::IsGCHeapInitialized())
        {
            FireEtwGCSettings_V1(GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(FALSE),
                              GCHeapUtilities::GetGCHeap()->GetValidSegmentSize(TRUE),
                              GCHeapUtilities::IsServerHeap(), GetClrInstanceId());
            GCHeapUtilities::GetGCHeap()->DiagTraceGCSegments();
        }

        if (context->RegistrationHandle == Microsoft_Windows_DotNETRuntimeRundownHandle)
        {
            if (IsRuntimeRundownProviderEnabled(TRACE_LEVEL_INFORMATION, CLR_RUNDOWNEND_KEYWORD))
                ETW::EnumerationLog::EndRundown();
        }
    }
}

#endif // FEATURE_ETW

void ETW::LoaderLog::ModuleLoad(HANDLE pModule)
{
    if (IsRuntimeProviderEnabled(TRACE_LEVEL_INFORMATION, KEYWORDZERO))
    {
        SendModuleEvent(pModule, ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad);
    }
}

//---------------------------------------------------------------------------------------
//
// send a module load/unload or rundown event and domainmodule load and rundown event
//
// Arguments:
//      * pModule - Module loading or unloading
//      * dwEventOptions - Bitmask of which events to fire
//
void ETW::LoaderLog::SendModuleEvent(HANDLE pModule, uint32_t dwEventOptions)
{
    const WCHAR * wszModuleFileName = NULL;
    const WCHAR * wszModuleILFileName = W("");

#if TARGET_WINDOWS
    PalGetModuleFileName(&wszModuleFileName, pModule);
#else
    const char * wszModuleFileNameUtf8 = NULL;
    uint32_t moduleFileNameCharCount = PalGetModuleFileName(&wszModuleFileNameUtf8, pModule);
    CHAR16_T wszModuleFileNameUnicode[1024];
    minipal_convert_utf8_to_utf16(wszModuleFileNameUtf8, moduleFileNameCharCount, &wszModuleFileNameUnicode[0], ARRAY_SIZE(wszModuleFileNameUnicode), MINIPAL_MB_NO_REPLACE_INVALID_CHARS);
    wszModuleFileName = (const WCHAR *)&wszModuleFileNameUnicode[0];
#endif

    GUID nativeGuid;
    uint32_t dwAge;
    WCHAR wszPath[1024];
    PalGetPDBInfo(pModule, &nativeGuid, &dwAge, wszPath, ARRAY_SIZE(wszPath));

    GUID zeroGuid = { 0 };

    if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleLoad)
    {
        FireEtwModuleLoad_V2(
            ULONGLONG(pModule),
            0,                      // AssemblyID
            ETW::LoaderLog::LoaderStructs::NativeModule, // Module Flags
            0,                      // Reserved1, 
            wszModuleILFileName,    // ModuleILPath, 
            wszModuleFileName,      // ModuleNativePath, 
            GetClrInstanceId(),
            &zeroGuid,              // ManagedPdbSignature,
            0,                      // ManagedPdbAge, 
            NULL,                   // ManagedPdbBuildPath, 
            &nativeGuid,            // NativePdbSignature,
            dwAge,                  // NativePdbAge, 
            wszPath                 // NativePdbBuildPath, 
            );
    }
    else if (dwEventOptions & ETW::EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd)
    {
        FireEtwModuleDCEnd_V2(
            ULONGLONG(pModule),
            0,                      // AssemblyID
            ETW::LoaderLog::LoaderStructs::NativeModule, // Module Flags
            0,                      // Reserved1, 
            wszModuleILFileName,    // ModuleILPath, 
            wszModuleFileName,      // ModuleNativePath, 
            GetClrInstanceId(),
            &zeroGuid,              // ManagedPdbSignature,
            0,                      // ManagedPdbAge, 
            NULL,                   // ManagedPdbBuildPath, 
            &nativeGuid,            // NativePdbSignature,
            dwAge,                  // NativePdbAge, 
            wszPath                 // NativePdbBuildPath, 
            );
    }
    else
    {
        ASSERT(0);
    }
}

/**************************************************************************************/
/* Called when ETW is turned OFF on an existing process .Will be used by the controller for end rundown*/
/**************************************************************************************/
void ETW::EnumerationLog::EndRundown()
{
    if (IsRuntimeRundownProviderEnabled(TRACE_LEVEL_INFORMATION, CLR_RUNDOWNLOADER_KEYWORD))
    {
        // begin marker event will go to the rundown provider
        FireEtwDCEndInit_V1(GetClrInstanceId());

        HANDLE pModule = PalGetModuleHandleFromPointer((void*)&EndRundown);

        LoaderLog::SendModuleEvent(pModule, EnumerationLog::EnumerationStructs::DomainAssemblyModuleDCEnd);

        // end marker event will go to the rundown provider
        FireEtwDCEndComplete_V1(GetClrInstanceId());
    }
}

void EventPipeEtwCallbackDotNETRuntime(
    _In_ GUID * SourceId,
    _In_ ULONG ControlCode,
    _In_ unsigned char Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext)
{
    SessionChange change = SourceId == NULL ? EventPipeSessionDisable : EventPipeSessionEnable;

    EtwCallbackCommon(DotNETRuntime, ControlCode, Level, MatchAnyKeyword, FilterData, change);
}

void EventPipeEtwCallbackDotNETRuntimeRundown(
    _In_ GUID * SourceId,
    _In_ ULONG ControlCode,
    _In_ unsigned char Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext)
{
    SessionChange change = SourceId == NULL ? EventPipeSessionDisable : EventPipeSessionEnable;

    EtwCallbackCommon(DotNETRuntimeRundown, ControlCode, Level, MatchAnyKeyword, FilterData, change);
}

void EventPipeEtwCallbackDotNETRuntimePrivate(
    _In_ GUID * SourceId,
    _In_ ULONG ControlCode,
    _In_ unsigned char Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext)
{
    SessionChange change = SourceId == NULL ? EventPipeSessionDisable : EventPipeSessionEnable;

    EtwCallbackCommon(DotNETRuntimePrivate, ControlCode, Level, MatchAnyKeyword, FilterData, change);
}
