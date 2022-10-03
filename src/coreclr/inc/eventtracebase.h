// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: eventtracebase.h
// Abstract: This module implements base Event Tracing support (excluding some of the
// CLR VM-specific ETW helpers).
//

//

//
//
// #EventTracing
// Windows
// ETW (Event Tracing for Windows) is a high-performance, low overhead and highly scalable
// tracing facility provided by the Windows Operating System. ETW is available on Win2K and above. There are
// four main types of components in ETW: event providers, controllers, consumers, and event trace sessions.
// An event provider is a logical entity that writes events to ETW sessions. The event provider must register
// a provider ID with ETW through the registration API. A provider first registers with ETW and writes events
// from various points in the code by invoking the ETW logging API. When a provider is enabled dynamically by
// the ETW controller application, calls to the logging API sends events to a specific trace session
// designated by the controller. Each event sent by the event provider to the trace session consists of a
// fixed header that includes event metadata and additional variable user-context data. CLR is an event
// provider.
// ============================================================================

#ifndef _ETWTRACER_HXX_
#define _ETWTRACER_HXX_

struct EventStructTypeData;
void InitializeEventTracing();

class PrepareCodeConfig;

// !!!!!!! NOTE !!!!!!!!
// The flags must match those in the ETW manifest exactly
// !!!!!!! NOTE !!!!!!!!

// These flags need to be defined either when FEATURE_EVENT_TRACE is enabled or the
// PROFILING_SUPPORTED is set, since they are used both by event tracing and profiling.

enum EtwTypeFlags
{
    kEtwTypeFlagsDelegate =                         0x1,
    kEtwTypeFlagsFinalizable =                      0x2,
    kEtwTypeFlagsExternallyImplementedCOMObject =   0x4,
    kEtwTypeFlagsArray =                            0x8,
    kEtwTypeFlagsArrayRankBit0 =                  0x100,
    kEtwTypeFlagsArrayRankBit1 =                  0x200,
    kEtwTypeFlagsArrayRankBit2 =                  0x400,
    kEtwTypeFlagsArrayRankBit3 =                  0x800,
    kEtwTypeFlagsArrayRankBit4 =                 0x1000,
    kEtwTypeFlagsArrayRankBit5 =                 0x2000,

    kEtwTypeFlagsArrayRankMask =                 0x3F00,
    kEtwTypeFlagsArrayRankShift =                     8,
    kEtwTypeFlagsArrayRankMax = kEtwTypeFlagsArrayRankMask >> kEtwTypeFlagsArrayRankShift
};

enum EtwThreadFlags
{
    kEtwThreadFlagGCSpecial =         0x00000001,
    kEtwThreadFlagFinalizer =         0x00000002,
    kEtwThreadFlagThreadPoolWorker =  0x00000004,
};

enum EtwGCSettingFlags
{
    kEtwGCFlagConcurrent =      0x00000001,
    kEtwGCFlagLargePages =      0x00000002,
    kEtwGCFlagFrozenSegs =      0x00000004,
    kEtwGCFlagHardLimitConfig = 0x00000008,
    kEtwGCFlagNoAffinitize =    0x00000010,
};

#ifndef FEATURE_NATIVEAOT

#if defined(FEATURE_EVENT_TRACE)

#if defined(FEATURE_PERFTRACING)
#define EVENT_PIPE_ENABLED() (EventPipeHelper::Enabled())
#else
#define EVENT_PIPE_ENABLED() (FALSE)
#endif

#if  !defined(HOST_UNIX)

//
// Use this macro at the least before calling the Event Macros
//

#define ETW_TRACING_INITIALIZED(RegHandle) \
    ((g_pEtwTracer && (RegHandle)) || EVENT_PIPE_ENABLED())

//
// Use this macro to check if an event is enabled
// if the fields in the event are not cheap to calculate
//
#define ETW_EVENT_ENABLED(Context, EventDescriptor) \
    ((Context.EtwProvider->IsEnabled && McGenEventXplatEnabled(Context.EtwProvider, &EventDescriptor)) || EventPipeHelper::IsEnabled(Context, EventDescriptor.Level, EventDescriptor.Keyword))

//
// Use this macro to check if a category of events is enabled
//

#define ETW_CATEGORY_ENABLED(Context, Level, Keyword) \
    ((Context.EtwProvider->IsEnabled && McGenEventProviderEnabled(Context.EtwProvider, Level, Keyword)) || EventPipeHelper::IsEnabled(Context, Level, Keyword))


// This macro only checks if a provider is enabled
// It does not check the flags and keywords for which it is enabled
#define ETW_PROVIDER_ENABLED(ProviderSymbol)                 \
        ((ProviderSymbol##_Context.IsEnabled) || EVENT_PIPE_ENABLED())


#else //!defined(HOST_UNIX)
#if defined(FEATURE_PERFTRACING)
#define ETW_INLINE
#define ETWOnStartup(StartEventName, EndEventName)
#define ETWFireEvent(EventName) FireEtw##EventName(GetClrInstanceId())

#define ETW_TRACING_INITIALIZED(RegHandle) (TRUE)
#define ETW_EVENT_ENABLED(Context, EventDescriptor) (EventPipeHelper::IsEnabled(Context, EventDescriptor.Level, EventDescriptor.Keyword) || \
        (XplatEventLogger::IsKeywordEnabled(Context, EventDescriptor.Level, EventDescriptor.Keyword)))
#define ETW_CATEGORY_ENABLED(Context, Level, Keyword) (EventPipeHelper::IsEnabled(Context, Level, Keyword) || \
        (XplatEventLogger::IsKeywordEnabled(Context, Level, Keyword)))
#define ETW_TRACING_ENABLED(Context, EventDescriptor) (EventEnabled##EventDescriptor())
#define ETW_TRACING_CATEGORY_ENABLED(Context, Level, Keyword) (EventPipeHelper::IsEnabled(Context, Level, Keyword) || \
        (XplatEventLogger::IsKeywordEnabled(Context, Level, Keyword)))
#define ETW_PROVIDER_ENABLED(ProviderSymbol) (TRUE)
#else //defined(FEATURE_PERFTRACING)
#define ETW_INLINE
#define ETWOnStartup(StartEventName, EndEventName)
#define ETWFireEvent(EventName)

#define ETW_TRACING_INITIALIZED(RegHandle) (TRUE)
#define ETW_CATEGORY_ENABLED(Context, Level, Keyword) (XplatEventLogger::IsKeywordEnabled(Context, Level, Keyword))
#define ETW_EVENT_ENABLED(Context, EventDescriptor) (XplatEventLogger::IsKeywordEnabled(Context, EventDescriptor.Level, EventDescriptor.KeywordsBitmask))
#define ETW_TRACING_ENABLED(Context, EventDescriptor) (ETW_EVENT_ENABLED(Context, EventDescriptor) && EventEnabled##EventDescriptor())
#define ETW_TRACING_CATEGORY_ENABLED(Context, Level, Keyword) (ETW_CATEGORY_ENABLED(Context, Level, Keyword))
#define ETW_PROVIDER_ENABLED(ProviderSymbol) (XplatEventLogger::IsProviderEnabled(Context))
#endif // defined(FEATURE_PERFTRACING)
#endif // !defined(HOST_UNIX)

#else // FEATURE_EVENT_TRACE

#define ETWOnStartup(StartEventName, EndEventName)
#define ETWFireEvent(EventName)

#define ETW_TRACING_INITIALIZED(RegHandle) (FALSE)
#define ETW_EVENT_ENABLED(Context, EventDescriptor) (FALSE)
#define ETW_CATEGORY_ENABLED(Context, Level, Keyword) (FALSE)
#define ETW_TRACING_ENABLED(Context, EventDescriptor) (FALSE)
#define ETW_TRACING_CATEGORY_ENABLED(Context, Level, Keyword) (FALSE)
#define ETW_PROVIDER_ENABLED(ProviderSymbol) (TRUE)

#endif // FEATURE_EVENT_TRACE

#endif // FEATURE_NATIVEAOT

// During a heap walk, this is the storage for keeping track of all the nodes and edges
// being batched up by ETW, and for remembering whether we're also supposed to call into
// a profapi profiler.  This is allocated toward the end of a GC and passed to us by the
// GC heap walker.
struct ProfilerWalkHeapContext
{
public:
    ProfilerWalkHeapContext(BOOL fProfilerPinnedParam, LPVOID pvEtwContextParam)
    {
        fProfilerPinned = fProfilerPinnedParam;
        pvEtwContext = pvEtwContextParam;
    }

    BOOL fProfilerPinned;
    LPVOID pvEtwContext;
};

#ifdef FEATURE_EVENT_TRACE

class Object;
#if !defined(HOST_UNIX)

/***************************************/
/* Tracing levels supported by CLR ETW */
/***************************************/
#define ETWMAX_TRACE_LEVEL 6        // Maximum Number of Trace Levels supported
#define TRACE_LEVEL_FATAL       1   // Abnormal exit or termination
#define TRACE_LEVEL_ERROR       2   // Severe errors that need logging
#define TRACE_LEVEL_WARNING     3   // Warnings such as allocation failure
#define TRACE_LEVEL_INFORMATION 4   // Includes non-error cases such as Entry-Exit
#define TRACE_LEVEL_VERBOSE     5   // Detailed traces from intermediate steps

struct ProfilingScanContext;

//
// Use this macro to check if ETW is initialized and the event is enabled
//
#define ETW_TRACING_ENABLED(Context, EventDescriptor) \
    ((Context.EtwProvider->IsEnabled && ETW_TRACING_INITIALIZED(Context.EtwProvider->RegistrationHandle) && ETW_EVENT_ENABLED(Context, EventDescriptor))|| \
        EventPipeHelper::IsEnabled(Context, EventDescriptor.Level, EventDescriptor.Keyword))

//
// Using KEYWORDZERO means when checking the events category ignore the keyword
//
#define KEYWORDZERO 0x0

//
// Use this macro to check if ETW is initialized and the category is enabled
//
#define ETW_TRACING_CATEGORY_ENABLED(Context, Level, Keyword) \
    (ETW_TRACING_INITIALIZED(Context.EtwProvider->RegistrationHandle) && ETW_CATEGORY_ENABLED(Context, Level, Keyword))

#define ETWOnStartup(StartEventName, EndEventName) \
    ETWTraceStartup trace##StartEventName##(Microsoft_Windows_DotNETRuntimePrivateHandle, &StartEventName, &StartupId, &EndEventName, &StartupId);
#define ETWFireEvent(EventName) FireEtw##EventName(GetClrInstanceId())

#ifndef FEATURE_NATIVEAOT
// Headers
#include <initguid.h>
#include <wmistr.h>
#include <evntrace.h>
#include <evntprov.h>
#endif //!FEATURE_NATIVEAOT
#endif //!defined(HOST_UNIX)


#else // FEATURE_EVENT_TRACE

#include "../gc/env/etmdummy.h"
#endif // FEATURE_EVENT_TRACE

#ifndef FEATURE_NATIVEAOT

#include "corprof.h"

// g_nClrInstanceId is defined in Utilcode\Util.cpp. The definition goes into Utilcode.lib.
// This enables both the VM and Utilcode to raise ETW events.
extern UINT32 g_nClrInstanceId;

#define GetClrInstanceId()  (static_cast<UINT16>(g_nClrInstanceId))
#if defined(HOST_UNIX) && (defined(FEATURE_EVENT_TRACE) || defined(FEATURE_EVENTSOURCE_XPLAT))
#define KEYWORDZERO 0x0

#define DEF_LTTNG_KEYWORD_ENABLED 1
#ifdef FEATURE_EVENT_TRACE
#include "clrproviders.h"
#endif // FEATURE_EVENT_TRACE
#include "clrconfig.h"

#endif // defined(HOST_UNIX) && (defined(FEATURE_EVENT_TRACE) || defined(FEATURE_EVENTSOURCE_XPLAT))

#if defined(FEATURE_PERFTRACING) || defined(FEATURE_EVENTSOURCE_XPLAT)

/***************************************/
/* Tracing levels supported by CLR ETW */
/***************************************/
#define MAX_TRACE_LEVEL         6   // Maximum Number of Trace Levels supported
#define TRACE_LEVEL_FATAL       1   // Abnormal exit or termination
#define TRACE_LEVEL_ERROR       2   // Severe errors that need logging
#define TRACE_LEVEL_WARNING     3   // Warnings such as allocation failure
#define TRACE_LEVEL_INFORMATION 4   // Includes non-error cases such as Entry-Exit
#define TRACE_LEVEL_VERBOSE     5   // Detailed traces from intermediate steps

class XplatEventLoggerConfiguration
{
public:
    XplatEventLoggerConfiguration() = default;

    XplatEventLoggerConfiguration(XplatEventLoggerConfiguration const & other) = delete;
    XplatEventLoggerConfiguration(XplatEventLoggerConfiguration && other)
    {
        _provider = std::move(other._provider);
        _isValid = other._isValid;
        _enabledKeywords = other._enabledKeywords;
        _level = other._level;
    }

    ~XplatEventLoggerConfiguration()
    {
        _provider = nullptr;
    }

    void Parse(LPWSTR configString)
    {
        auto providerComponent = GetNextComponentString(configString);
        _provider = ParseProviderName(providerComponent);
        if (_provider == nullptr)
        {
            _isValid = false;
            return;
        }

        auto keywordsComponent = GetNextComponentString(providerComponent.End + 1);
        _enabledKeywords = ParseEnabledKeywordsMask(keywordsComponent);

        auto levelComponent = GetNextComponentString(keywordsComponent.End + 1);
        _level = ParseLevel(levelComponent);

        auto argumentComponent = GetNextComponentString(levelComponent.End + 1);
        _argument = ParseArgument(argumentComponent);

        _isValid = true;
    }

    bool IsValid() const
    {
        return _isValid;
    }

    LPCWSTR GetProviderName() const
    {
        return _provider;
    }

    uint64_t GetEnabledKeywordsMask() const
    {
        return _enabledKeywords;
    }

    uint32_t GetLevel() const
    {
        return _level;
    }

    LPCWSTR GetArgument() const
    {
        return _argument;
    }

private:
    struct ComponentSpan
    {
    public:
        ComponentSpan(LPCWSTR start, LPCWSTR end)
        : Start(start), End(end)
        {
        }

        LPCWSTR Start;
        LPCWSTR End;
    };

    ComponentSpan GetNextComponentString(LPCWSTR start) const
    {
        const WCHAR ComponentDelimiter = W(':');
        const WCHAR * end = wcschr(start, ComponentDelimiter);
        if (end == nullptr)
        {
            end = start + wcslen(start);
        }

        return ComponentSpan(start, end);
    }

    NewArrayHolder<WCHAR> ParseProviderName(ComponentSpan const & component) const
    {
        NewArrayHolder<WCHAR> providerName = nullptr;
        if ((component.End - component.Start) != 0)
        {
            auto const length = component.End - component.Start;
            providerName = new WCHAR[length + 1];
            wcsncpy(providerName, component.Start, length);
            providerName[length] = '\0';
        }
        return providerName;
    }

    uint64_t ParseEnabledKeywordsMask(ComponentSpan const & component) const
    {
        auto enabledKeywordsMask = (uint64_t)(-1);
        if ((component.End - component.Start) != 0)
        {
            enabledKeywordsMask = _wcstoui64(component.Start, nullptr, 16);
        }
        return enabledKeywordsMask;
    }

    uint32_t ParseLevel(ComponentSpan const & component) const
    {
        int level = TRACE_LEVEL_VERBOSE; // Verbose
        if ((component.End - component.Start) != 0)
        {
            level = _wtoi(component.Start);
        }
        return level;
    }

    NewArrayHolder<WCHAR> ParseArgument(ComponentSpan const & component) const
    {
        NewArrayHolder<WCHAR> argument = nullptr;
        if ((component.End - component.Start) != 0)
        {
            auto const length = component.End - component.Start;
            argument = new WCHAR[length + 1];
            wcsncpy(argument, component.Start, length);
            argument[length] = '\0';
        }
        return argument;
    }

    NewArrayHolder<WCHAR> _provider;
    uint64_t _enabledKeywords;
    uint32_t _level;
    NewArrayHolder<WCHAR> _argument;
    bool _isValid;
};
#endif // defined(FEATURE_PERFTRACING) || defined(FEATURE_EVENTSOURCE_XPLAT)

#if defined(HOST_UNIX) && (defined(FEATURE_EVENT_TRACE) || defined(FEATURE_EVENTSOURCE_XPLAT))

class XplatEventLoggerController
{
public:

    static void UpdateProviderContext(XplatEventLoggerConfiguration const &config)
    {
        if (!config.IsValid())
        {
            return;
        }

        auto providerName = config.GetProviderName();
        auto enabledKeywordsMask = config.GetEnabledKeywordsMask();
        auto level = config.GetLevel();
        if (_wcsicmp(providerName, W("*")) == 0 && enabledKeywordsMask == (ULONGLONG)(-1) && level == TRACE_LEVEL_VERBOSE)
        {
            ActivateAllKeywordsOfAllProviders();
        }
#ifdef FEATURE_EVENT_TRACE
        else
        {
            LTTNG_TRACE_CONTEXT *provider = GetProvider(providerName);
            if (provider == nullptr)
            {
                return;
            }
            provider->EnabledKeywordsBitmask = enabledKeywordsMask;
            provider->Level = level;
            provider->IsEnabled = true;
        }
#endif
    }

    static void ActivateAllKeywordsOfAllProviders()
    {
#ifdef FEATURE_EVENT_TRACE
        for (LTTNG_TRACE_CONTEXT * const provider : ALL_LTTNG_PROVIDERS_CONTEXT)
        {
            provider->EnabledKeywordsBitmask = (ULONGLONG)(-1);
            provider->Level = TRACE_LEVEL_VERBOSE;
            provider->IsEnabled = true;
        }
#endif
    }

private:
#ifdef FEATURE_EVENT_TRACE
    static LTTNG_TRACE_CONTEXT * const GetProvider(LPCWSTR providerName)
    {
        auto length = wcslen(providerName);
        for (auto provider : ALL_LTTNG_PROVIDERS_CONTEXT)
        {
            if (_wcsicmp(provider->Name, providerName) == 0)
            {
                return provider;
            }
        }
        return nullptr;
    }
#endif
};

class XplatEventLogger
{
public:

    inline static BOOL IsEventLoggingEnabled()
    {
        static ConfigDWORD configEventLogging;
        return configEventLogging.val(CLRConfig::EXTERNAL_EnableEventLog);
    }

#ifdef FEATURE_EVENT_TRACE
    inline static bool IsProviderEnabled(DOTNET_TRACE_CONTEXT providerCtx)
    {
        return providerCtx.LttngProvider->IsEnabled;
    }

    inline static bool IsKeywordEnabled(DOTNET_TRACE_CONTEXT providerCtx, UCHAR level, ULONGLONG keyword)
    {
        if (!providerCtx.LttngProvider->IsEnabled)
        {
            return false;
        }

        if ((level <= providerCtx.LttngProvider->Level) || (providerCtx.LttngProvider->Level == 0))
        {
            if ((keyword == 0) || ((keyword & providerCtx.LttngProvider->EnabledKeywordsBitmask) != 0))
            {
                return true;
            }
        }
        return false;
    }
#endif

    /*
    This method is where COMPlus_LTTngConfig environment variable is parsed and is registered with the runtime provider
    context structs generated by src/scripts/genEventing.py.
    It expects the environment variable to look like:
    provider:keywords:level,provider:keywords:level
    (Notice the "arguments" part is missing compared to EventPipe configuration)

    Ex)
    Microsoft-Windows-DotNETRuntime:deadbeefdeadbeef:4,Microsoft-Windows-DotNETRuntimePrivate:deafbeefdeadbeef:5
    */
    static void InitializeLogger()
    {
        if (!IsEventLoggingEnabled())
        {
            return;
        }

        LPWSTR xplatEventConfig = NULL;
        CLRConfig::GetConfigValue(CLRConfig::INTERNAL_LTTngConfig, &xplatEventConfig);
        auto configuration = XplatEventLoggerConfiguration();
        auto configToParse = xplatEventConfig;

        if (configToParse == nullptr || *configToParse == L'\0')
        {
            XplatEventLoggerController::ActivateAllKeywordsOfAllProviders();
            return;
        }
        while (configToParse != nullptr)
        {
            const WCHAR comma = W(',');
            auto end = wcschr(configToParse, comma);
            configuration.Parse(configToParse);
            XplatEventLoggerController::UpdateProviderContext(configuration);
            if (end == nullptr)
            {
                break;
            }
            configToParse = end + 1;
        }
    }
};


#endif  // defined(HOST_UNIX) && (defined(FEATURE_EVENT_TRACE) || defined(FEATURE_EVENTSOURCE_XPLAT))

#if defined(FEATURE_EVENT_TRACE)

#ifdef FEATURE_PERFTRACING
#include "../vm/eventpipeadaptertypes.h"
#endif // FEATURE_PERFTRACING

VOID EventPipeEtwCallbackDotNETRuntimeStress(
    _In_ LPCGUID SourceId,
    _In_ ULONG ControlCode,
    _In_ UCHAR Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext);

VOID EventPipeEtwCallbackDotNETRuntime(
    _In_ LPCGUID SourceId,
    _In_ ULONG ControlCode,
    _In_ UCHAR Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext);

VOID EventPipeEtwCallbackDotNETRuntimeRundown(
    _In_ LPCGUID SourceId,
    _In_ ULONG ControlCode,
    _In_ UCHAR Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext);

VOID EventPipeEtwCallbackDotNETRuntimePrivate(
    _In_ LPCGUID SourceId,
    _In_ ULONG ControlCode,
    _In_ UCHAR Level,
    _In_ ULONGLONG MatchAnyKeyword,
    _In_ ULONGLONG MatchAllKeyword,
    _In_opt_ EventFilterDescriptor* FilterData,
    _Inout_opt_ PVOID CallbackContext);

#ifndef  HOST_UNIX
// Callback and stack support
#if !defined(DONOT_DEFINE_ETW_CALLBACK) && !defined(DACCESS_COMPILE)
extern "C" {
    /* ETW control callback
         * Desc:        This function handles the ETW control
         *              callback.
         * Ret:         success or failure
     ***********************************************/
    VOID EtwCallback(
        _In_ LPCGUID SourceId,
        _In_ ULONG ControlCode,
        _In_ UCHAR Level,
        _In_ ULONGLONG MatchAnyKeyword,
        _In_ ULONGLONG MatchAllKeyword,
        _In_opt_ PEVENT_FILTER_DESCRIPTOR FilterData,
        _Inout_opt_ PVOID CallbackContext);
}

//
// User defined callback
//
#define MCGEN_PRIVATE_ENABLE_CALLBACK(RequestCode, Context, InOutBufferSize, Buffer) \
        EtwCallback(NULL /* SourceId */, ((RequestCode)==WMI_ENABLE_EVENTS) ? EVENT_CONTROL_CODE_ENABLE_PROVIDER : EVENT_CONTROL_CODE_DISABLE_PROVIDER, 0 /* Level */, 0 /* MatchAnyKeyword */, 0 /* MatchAllKeyword */, NULL /* FilterData */, Context)

//
// User defined callback2
//
#define MCGEN_PRIVATE_ENABLE_CALLBACK_V2(SourceId, ControlCode, Level, MatchAnyKeyword, MatchAllKeyword, FilterData, CallbackContext) \
        EtwCallback(SourceId, ControlCode, Level, MatchAnyKeyword, MatchAllKeyword, FilterData, CallbackContext)

extern "C" {
    /* ETW callout
         * Desc:        This function handles the ETW callout
         * Ret:         success or failure
     ***********************************************/
    VOID EtwCallout(
        REGHANDLE RegHandle,
        PCEVENT_DESCRIPTOR Descriptor,
        ULONG ArgumentCount,
        PEVENT_DATA_DESCRIPTOR EventData);
}

//
// Call user defined callout
//
#define MCGEN_CALLOUT(RegHandle, Descriptor, NumberOfArguments, EventData) \
        EtwCallout(RegHandle, Descriptor, NumberOfArguments, EventData)
#endif //!DONOT_DEFINE_ETW_CALLBACK && !DACCESS_COMPILE

#endif //!HOST_UNIX
#include "clretwallmain.h"

#if defined(FEATURE_PERFTRACING)
class EventPipeHelper
{
public:
    static bool Enabled();
    static bool IsEnabled(DOTNET_TRACE_CONTEXT Context, UCHAR Level, ULONGLONG Keyword);
};
#endif // defined(FEATURE_PERFTRACING)

#endif // FEATURE_EVENT_TRACE

/**************************/
/* CLR ETW infrastructure */
/**************************/
// #CEtwTracer
// On Windows Vista, ETW has gone through a major upgrade, and one of the most significant changes is the
// introduction of the unified event provider model and APIs. The older architecture used the classic ETW
// events. The new ETW architecture uses the manifest based events. To support both types of events at the
// same time, we use the manpp tool for generating event macros that can be directly used to fire ETW events
// from various components within the CLR.
// (http://diagnostics/sites/etw/Lists/Announcements/DispForm.aspx?ID=10&Source=http%3A%2F%2Fdiagnostics%2Fsites%2Fetw%2Fdefault%2Easpx)
// Every ETW provider has to Register itself to the system, so that when enabled, it is capable of firing
// ETW events. file:../VM/eventtrace.cpp#Registration is where the actual Provider Registration takes place.
// At process shutdown, a registered provider need to be unregistered.
// file:../VM/eventtrace.cpp#Unregistration. Since ETW can also be enabled at any instant after the process
// has started, one may want to do something useful when that happens (e.g enumerate all the loaded modules
// in the system). To enable this, we have to implement a callback routine.
// file:../VM/eventtrace.cpp#EtwCallback is CLR's implementation of the callback.
//

#include "daccess.h"
class Module;
class Assembly;
class MethodDesc;
class MethodTable;
class BaseDomain;
class AppDomain;
class SString;
class CrawlFrame;
class LoaderAllocator;
class AssemblyLoaderAllocator;
struct AllLoggedTypes;
class CrstBase;
class BulkTypeEventLogger;
class TypeHandle;
class Thread;
template<typename ELEMENT, typename TRAITS>
class SetSHash;
template<typename ELEMENT>
class PtrSetSHashTraits;
typedef SetSHash<MethodDesc*, PtrSetSHashTraits<MethodDesc*>> MethodDescSet;

// All ETW helpers must be a part of this namespace
// We have auto-generated macros to directly fire the events
// but in some cases, gathering the event payload information involves some work
// and it can be done in a relevant helper class like the one's in this namespace
namespace ETW
{
    // Class to wrap the ETW infrastructure logic
#if  !defined(HOST_UNIX)
    class CEtwTracer
    {
#if defined(FEATURE_EVENT_TRACE)
        ULONG RegGuids(LPCGUID ProviderId, PENABLECALLBACK EnableCallback, PVOID CallbackContext, PREGHANDLE RegHandle);
#endif

    public:
#ifdef FEATURE_EVENT_TRACE
        // Registers all the Event Tracing providers
        HRESULT Register();

        // Unregisters all the Event Tracing providers
        HRESULT UnRegister();
#else
        HRESULT Register()
        {
            return S_OK;
        }
        HRESULT UnRegister()
        {
            return S_OK;
        }
#endif // FEATURE_EVENT_TRACE
    };
#endif // !defined(HOST_UNIX)

    class LoaderLog;
    class MethodLog;
    // Class to wrap all the enumeration logic for ETW
    class EnumerationLog
    {
        friend class ETW::LoaderLog;
        friend class ETW::MethodLog;
#ifdef FEATURE_EVENT_TRACE
        static VOID SendThreadRundownEvent();
        static VOID SendGCRundownEvent();
        static VOID IterateDomain(BaseDomain *pDomain, DWORD enumerationOptions);
        static VOID IterateAppDomain(AppDomain * pAppDomain, DWORD enumerationOptions);
        static VOID IterateCollectibleLoaderAllocator(AssemblyLoaderAllocator *pLoaderAllocator, DWORD enumerationOptions);
        static VOID IterateAssembly(Assembly *pAssembly, DWORD enumerationOptions);
        static VOID IterateModule(Module *pModule, DWORD enumerationOptions);
        static VOID EnumerationHelper(Module *moduleFilter, BaseDomain *domainFilter, DWORD enumerationOptions);
        static DWORD GetEnumerationOptionsFromRuntimeKeywords();
    public:
        typedef union _EnumerationStructs
        {
            typedef enum _EnumerationOptions
            {
                None=                               0x00000000,
                DomainAssemblyModuleLoad=           0x00000001,
                DomainAssemblyModuleUnload=         0x00000002,
                DomainAssemblyModuleDCStart=        0x00000004,
                DomainAssemblyModuleDCEnd=          0x00000008,
                JitMethodLoad=                      0x00000010,
                JitMethodUnload=                    0x00000020,
                JitMethodDCStart=                   0x00000040,
                JitMethodDCEnd=                     0x00000080,
                NgenMethodLoad=                     0x00000100,
                NgenMethodUnload=                   0x00000200,
                NgenMethodDCStart=                  0x00000400,
                NgenMethodDCEnd=                    0x00000800,
                ModuleRangeLoad=                    0x00001000,
                ModuleRangeDCStart=                 0x00002000,
                ModuleRangeDCEnd=                   0x00004000,
                ModuleRangeLoadPrivate=             0x00008000,
                MethodDCStartILToNativeMap=         0x00010000,
                MethodDCEndILToNativeMap=           0x00020000,
                JitMethodILToNativeMap=             0x00040000,
                TypeUnload=                         0x00080000,
                JittedMethodRichDebugInfo=          0x00100000,

                // Helpers
                ModuleRangeEnabledAny = ModuleRangeLoad | ModuleRangeDCStart | ModuleRangeDCEnd | ModuleRangeLoadPrivate,
                JitMethodLoadOrDCStartAny = JitMethodLoad | JitMethodDCStart | MethodDCStartILToNativeMap,
                JitMethodUnloadOrDCEndAny = JitMethodUnload | JitMethodDCEnd | MethodDCEndILToNativeMap,
            }EnumerationOptions;
        }EnumerationStructs;

        static VOID ProcessShutdown();
        static VOID ModuleRangeRundown();
        static VOID SendOneTimeRundownEvents();
        static VOID StartRundown();
        static VOID EndRundown();
        static VOID EnumerateForCaptureState();
#else
    public:
        static VOID ProcessShutdown() {};
        static VOID StartRundown() {};
        static VOID EndRundown() {};
#endif // FEATURE_EVENT_TRACE
    };


    // Class to wrap all the sampling logic for ETW

    class SamplingLog
    {
#if defined(FEATURE_EVENT_TRACE) && !defined(HOST_UNIX)
    public:
        typedef enum _EtwStackWalkStatus
        {
            Completed = 0,
            UnInitialized = 1,
            InProgress = 2
        } EtwStackWalkStatus;
    private:
        static const UINT8 s_MaxStackSize=100;
        UINT32 m_FrameCount;
        SIZE_T m_EBPStack[SamplingLog::s_MaxStackSize];
        VOID Append(SIZE_T currentFrame);
        EtwStackWalkStatus SaveCurrentStack(int skipTopNFrames=1);
    public:
        static ULONG SendStackTrace(MCGEN_TRACE_CONTEXT TraceContext, PCEVENT_DESCRIPTOR Descriptor, LPCGUID EventGuid);
        EtwStackWalkStatus GetCurrentThreadsCallStack(UINT32 *frameCount, PVOID **Stack);
#endif // FEATURE_EVENT_TRACE && !defined(HOST_UNIX)
    };

    // Class to wrap all Loader logic for ETW
    class LoaderLog
    {
        friend class ETW::EnumerationLog;
#if defined(FEATURE_EVENT_TRACE)
        static VOID SendModuleEvent(Module *pModule, DWORD dwEventOptions, BOOL bFireDomainModuleEvents=FALSE);
        static ULONG SendModuleRange(_In_ Module *pModule, _In_ DWORD dwEventOptions);
        static VOID SendAssemblyEvent(Assembly *pAssembly, DWORD dwEventOptions);
        static VOID SendDomainEvent(BaseDomain *pBaseDomain, DWORD dwEventOptions, LPCWSTR wszFriendlyName=NULL);
    public:
        typedef union _LoaderStructs
        {
            typedef enum _AppDomainFlags
            {
                DefaultDomain=0x1,
                ExecutableDomain=0x2,
                SharedDomain=0x4
            }AppDomainFlags;

            typedef enum _AssemblyFlags
            {
                DomainNeutralAssembly=0x1,
                DynamicAssembly=0x2,
                NativeAssembly=0x4,
                CollectibleAssembly=0x8,
                ReadyToRunAssembly=0x10,
            }AssemblyFlags;

            typedef enum _ModuleFlags
            {
                DomainNeutralModule=0x1,
                NativeModule=0x2,
                DynamicModule=0x4,
                ManifestModule=0x8,
                IbcOptimized=0x10,
                ReadyToRunModule=0x20,
                PartialReadyToRunModule=0x40,
            }ModuleFlags;

            typedef enum _RangeFlags
            {
                HotRange=0x0
            }RangeFlags;

        }LoaderStructs;

        static VOID DomainLoadReal(BaseDomain *pDomain, _In_opt_ LPWSTR wszFriendlyName=NULL);

        static VOID DomainLoad(BaseDomain *pDomain, _In_opt_ LPWSTR wszFriendlyName = NULL)
        {
            if (ETW_PROVIDER_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER))
            {
                DomainLoadReal(pDomain, wszFriendlyName);
            }
        }

        static VOID DomainUnload(AppDomain *pDomain);
        static VOID CollectibleLoaderAllocatorUnload(AssemblyLoaderAllocator *pLoaderAllocator);
        static VOID ModuleLoad(Module *pModule, LONG liReportedSharedModule);
#else
    public:
        static VOID DomainLoad(BaseDomain *pDomain, _In_opt_ LPWSTR wszFriendlyName=NULL) {};
        static VOID DomainUnload(AppDomain *pDomain) {};
        static VOID CollectibleLoaderAllocatorUnload(AssemblyLoaderAllocator *pLoaderAllocator) {};
        static VOID ModuleLoad(Module *pModule, LONG liReportedSharedModule) {};
#endif // FEATURE_EVENT_TRACE
    };

    // Class to wrap all Method logic for ETW
    class MethodLog
    {
        friend class ETW::EnumerationLog;
#ifdef FEATURE_EVENT_TRACE
        static VOID SendEventsForJitMethods(BaseDomain *pDomainFilter, LoaderAllocator *pLoaderAllocatorFilter, DWORD dwEventOptions);
        static VOID SendEventsForJitMethodsHelper(
            LoaderAllocator *pLoaderAllocatorFilter,
            DWORD dwEventOptions,
            BOOL fLoadOrDCStart,
            BOOL fUnloadOrDCEnd,
            BOOL fSendMethodEvent,
            BOOL fSendILToNativeMapEvent,
            BOOL fSendRichDebugInfoEvent,
            BOOL fGetCodeIds);
        static VOID SendEventsForNgenMethods(Module *pModule, DWORD dwEventOptions);
        static VOID SendMethodJitStartEvent(MethodDesc *pMethodDesc, SString *namespaceOrClassName=NULL, SString *methodName=NULL, SString *methodSignature=NULL);
        static VOID SendMethodILToNativeMapEvent(MethodDesc * pMethodDesc, DWORD dwEventOptions, PCODE pNativeCodeStartAddress, DWORD nativeCodeId, ReJITID ilCodeId);
        static VOID SendMethodRichDebugInfo(MethodDesc * pMethodDesc, PCODE pNativeCodeStartAddress, DWORD nativeCodeId, ReJITID ilCodeId, MethodDescSet* sentMethodDetailsSet);
        static VOID SendMethodEvent(MethodDesc *pMethodDesc, DWORD dwEventOptions, BOOL bIsJit, SString *namespaceOrClassName=NULL, SString *methodName=NULL, SString *methodSignature=NULL, PCODE pNativeCodeStartAddress = 0, PrepareCodeConfig *pConfig = NULL, MethodDescSet* sentMethodDetailsSet = NULL);
        static VOID SendHelperEvent(ULONGLONG ullHelperStartAddress, ULONG ulHelperSize, LPCWSTR pHelperName);
    public:
        typedef union _MethodStructs
        {
            typedef enum _MethodFlags
            {
                DynamicMethod=0x1,
                GenericMethod=0x2,
                SharedGenericCode=0x4,
                JittedMethod=0x8,
                JitHelperMethod=0x10,
                ProfilerRejectedPrecompiledCode=0x20,
                ReadyToRunRejectedPrecompiledCode=0x40,
                // 0x80 to 0x200 are used for the optimization tier
            }MethodFlags;

            typedef enum _MethodExtent
            {
                HotSection=0x00000000,
                ColdSection=0x10000000
            }MethodExtent;

        }MethodStructs;

        static const UINT8 MethodFlagsJitOptimizationTierShift = 7;
        static const unsigned int MethodFlagsJitOptimizationTierLowMask = 0x7;

        static VOID GetR2RGetEntryPointStart(MethodDesc *pMethodDesc);
        static VOID GetR2RGetEntryPoint(MethodDesc *pMethodDesc, PCODE pEntryPoint);
        static VOID MethodJitting(MethodDesc *pMethodDesc, SString *namespaceOrClassName, SString *methodName, SString *methodSignature);
        static VOID MethodJitted(MethodDesc *pMethodDesc, SString *namespaceOrClassName, SString *methodName, SString *methodSignature, PCODE pNativeCodeStartAddress, PrepareCodeConfig *pConfig);
        static VOID SendMethodDetailsEvent(MethodDesc *pMethodDesc);
        static VOID SendNonDuplicateMethodDetailsEvent(MethodDesc* pMethodDesc, MethodDescSet* set);
        static VOID StubInitialized(ULONGLONG ullHelperStartAddress, LPCWSTR pHelperName);
        static VOID StubsInitialized(PVOID *pHelperStartAddress, PVOID *pHelperNames, LONG ulNoOfHelpers);
        static VOID MethodRestored(MethodDesc * pMethodDesc);
        static VOID MethodTableRestored(MethodTable * pMethodTable);
        static VOID DynamicMethodDestroyed(MethodDesc *pMethodDesc);
        static VOID LogMethodInstrumentationData(MethodDesc* method, uint32_t cbData, BYTE *data, TypeHandle* pTypeHandles, uint32_t numTypeHandles, MethodDesc** pMethods, uint32_t numMethods);
#else // FEATURE_EVENT_TRACE
    public:
        static VOID GetR2RGetEntryPointStart(MethodDesc *pMethodDesc) {};
        static VOID GetR2RGetEntryPoint(MethodDesc *pMethodDesc, PCODE pEntryPoint) {};
        static VOID MethodJitting(MethodDesc *pMethodDesc, SString *namespaceOrClassName, SString *methodName, SString *methodSignature);
        static VOID MethodJitted(MethodDesc *pMethodDesc, SString *namespaceOrClassName, SString *methodName, SString *methodSignature, PCODE pNativeCodeStartAddress, PrepareCodeConfig *pConfig);
        static VOID StubInitialized(ULONGLONG ullHelperStartAddress, LPCWSTR pHelperName) {};
        static VOID StubsInitialized(PVOID *pHelperStartAddress, PVOID *pHelperNames, LONG ulNoOfHelpers) {};
        static VOID MethodRestored(MethodDesc * pMethodDesc) {};
        static VOID MethodTableRestored(MethodTable * pMethodTable) {};
        static VOID DynamicMethodDestroyed(MethodDesc *pMethodDesc) {};
        static VOID LogMethodInstrumentationData(MethodDesc* method, uint32_t cbData, BYTE *data, TypeHandle* pTypeHandles, uint32_t numTypeHandles, MethodDesc** pMethods, uint32_t numMethods) {};
#endif // FEATURE_EVENT_TRACE
    };

    // Class to wrap all Security logic for ETW
    class SecurityLog
    {
#ifdef FEATURE_EVENT_TRACE
    public:
        static VOID StrongNameVerificationStart(DWORD dwInFlags, _In_ LPWSTR strFullyQualifiedAssemblyName);
        static VOID StrongNameVerificationStop(DWORD dwInFlags,ULONG result, _In_ LPWSTR strFullyQualifiedAssemblyName);

        static void FireFieldTransparencyComputationStart(LPCWSTR wszFieldName,
                                                          LPCWSTR wszModuleName,
                                                          DWORD dwAppDomain);
        static void FireFieldTransparencyComputationEnd(LPCWSTR wszFieldName,
                                                        LPCWSTR wszModuleName,
                                                        DWORD dwAppDomain,
                                                        BOOL fIsCritical,
                                                        BOOL fIsTreatAsSafe);

        static void FireMethodTransparencyComputationStart(LPCWSTR wszMethodName,
                                                           LPCWSTR wszModuleName,
                                                           DWORD dwAppDomain);
        static void FireMethodTransparencyComputationEnd(LPCWSTR wszMethodName,
                                                         LPCWSTR wszModuleName,
                                                         DWORD dwAppDomain,
                                                         BOOL fIsCritical,
                                                         BOOL fIsTreatAsSafe);

        static void FireModuleTransparencyComputationStart(LPCWSTR wszModuleName, DWORD dwAppDomain);
        static void FireModuleTransparencyComputationEnd(LPCWSTR wszModuleName,
                                                         DWORD dwAppDomain,
                                                         BOOL fIsAllCritical,
                                                         BOOL fIsAllTransparent,
                                                         BOOL fIsTreatAsSafe,
                                                         BOOL fIsOpportunisticallyCritical,
                                                         DWORD dwSecurityRuleSet);

        static void FireTokenTransparencyComputationStart(DWORD dwToken,
                                                          LPCWSTR wszModuleName,
                                                          DWORD dwAppDomain);
        static void FireTokenTransparencyComputationEnd(DWORD dwToken,
                                                        LPCWSTR wszModuleName,
                                                        DWORD dwAppDomain,
                                                        BOOL fIsCritical,
                                                        BOOL fIsTreatAsSafe);

        static void FireTypeTransparencyComputationStart(LPCWSTR wszTypeName,
                                                         LPCWSTR wszModuleName,
                                                         DWORD dwAppDomain);
        static void FireTypeTransparencyComputationEnd(LPCWSTR wszTypeName,
                                                       LPCWSTR wszModuleName,
                                                       DWORD dwAppDomain,
                                                       BOOL fIsAllCritical,
                                                       BOOL fIsAllTransparent,
                                                       BOOL fIsCritical,
                                                       BOOL fIsTreatAsSafe);
#else
    public:
        static VOID StrongNameVerificationStart(DWORD dwInFlags, _In_z_ LPWSTR strFullyQualifiedAssemblyName) {};
        static VOID StrongNameVerificationStop(DWORD dwInFlags,ULONG result, _In_z_ LPWSTR strFullyQualifiedAssemblyName) {};

        static void FireFieldTransparencyComputationStart(LPCWSTR wszFieldName,
                                                          LPCWSTR wszModuleName,
                                                          DWORD dwAppDomain) {};
        static void FireFieldTransparencyComputationEnd(LPCWSTR wszFieldName,
                                                        LPCWSTR wszModuleName,
                                                        DWORD dwAppDomain,
                                                        BOOL fIsCritical,
                                                        BOOL fIsTreatAsSafe) {};

        static void FireMethodTransparencyComputationStart(LPCWSTR wszMethodName,
                                                           LPCWSTR wszModuleName,
                                                           DWORD dwAppDomain) {};
        static void FireMethodTransparencyComputationEnd(LPCWSTR wszMethodName,
                                                         LPCWSTR wszModuleName,
                                                         DWORD dwAppDomain,
                                                         BOOL fIsCritical,
                                                         BOOL fIsTreatAsSafe) {};

        static void FireModuleTransparencyComputationStart(LPCWSTR wszModuleName, DWORD dwAppDomain) {};
        static void FireModuleTransparencyComputationEnd(LPCWSTR wszModuleName,
                                                         DWORD dwAppDomain,
                                                         BOOL fIsAllCritical,
                                                         BOOL fIsAllTransparent,
                                                         BOOL fIsTreatAsSafe,
                                                         BOOL fIsOpportunisticallyCritical,
                                                         DWORD dwSecurityRuleSet) {};

        static void FireTokenTransparencyComputationStart(DWORD dwToken,
                                                          LPCWSTR wszModuleName,
                                                          DWORD dwAppDomain) {};
        static void FireTokenTransparencyComputationEnd(DWORD dwToken,
                                                        LPCWSTR wszModuleName,
                                                        DWORD dwAppDomain,
                                                        BOOL fIsCritical,
                                                        BOOL fIsTreatAsSafe) {};

        static void FireTypeTransparencyComputationStart(LPCWSTR wszTypeName,
                                                         LPCWSTR wszModuleName,
                                                         DWORD dwAppDomain) {};
        static void FireTypeTransparencyComputationEnd(LPCWSTR wszTypeName,
                                                       LPCWSTR wszModuleName,
                                                       DWORD dwAppDomain,
                                                       BOOL fIsAllCritical,
                                                       BOOL fIsAllTransparent,
                                                       BOOL fIsCritical,
                                                       BOOL fIsTreatAsSafe) {};
#endif // FEATURE_EVENT_TRACE
    };

    // Class to wrap all Binder logic for ETW
    class BinderLog
    {
    public:
        typedef union _BinderStructs {
            typedef  enum _NGENBINDREJECT_REASON {
                NGEN_BIND_START_BIND = 0,
                NGEN_BIND_NO_INDEX = 1,
                NGEN_BIND_SYSTEM_ASSEMBLY_NOT_AVAILABLE = 2,
                NGEN_BIND_NO_NATIVE_IMAGE = 3,
                NGEN_BIND_REJECT_CONFIG_MASK = 4,
                NGEN_BIND_FAIL = 5,
                NGEN_BIND_INDEX_CORRUPTION = 6,
                NGEN_BIND_REJECT_TIMESTAMP = 7,
                NGEN_BIND_REJECT_NATIVEIMAGE_NOT_FOUND = 8,
                NGEN_BIND_REJECT_IL_SIG = 9,
                NGEN_BIND_REJECT_LOADER_EVAL_FAIL = 10,
                NGEN_BIND_MISSING_FOUND = 11,
                NGEN_BIND_REJECT_HOSTASM = 12,
                NGEN_BIND_REJECT_IL_NOT_FOUND = 13,
                NGEN_BIND_REJECT_APPBASE_NOT_FILE = 14,
                NGEN_BIND_BIND_DEPEND_REJECT_REF_DEF_MISMATCH = 15,
                NGEN_BIND_BIND_DEPEND_REJECT_NGEN_SIG = 16,
                NGEN_BIND_APPLY_EXTERNAL_RELOCS_FAILED = 17,
                NGEN_BIND_SYSTEM_ASSEMBLY_NATIVEIMAGE_NOT_AVAILABLE = 18,
                NGEN_BIND_ASSEMBLY_HAS_DIFFERENT_GRANT = 19,
                NGEN_BIND_ASSEMBLY_NOT_DOMAIN_NEUTRAL = 20,
                NGEN_BIND_NATIVEIMAGE_VERSION_MISMATCH = 21,
                NGEN_BIND_LOADFROM_NOT_ALLOWED = 22,
                NGEN_BIND_DEPENDENCY_HAS_DIFFERENT_IDENTITY = 23
            } NGENBINDREJECT_REASON;
        } BinderStructs;
    };

    // Class to wrap all Exception logic for ETW
    class ExceptionLog
    {
    public:
#ifdef FEATURE_EVENT_TRACE
        static VOID ExceptionThrown(CrawlFrame  *pCf, BOOL bIsReThrownException, BOOL bIsNewException);
        static VOID ExceptionThrownEnd();
        static VOID ExceptionCatchBegin(MethodDesc * pMethodDesc, PVOID pEntryEIP);
        static VOID ExceptionCatchEnd();
        static VOID ExceptionFinallyBegin(MethodDesc * pMethodDesc, PVOID pEntryEIP);
        static VOID ExceptionFinallyEnd();
        static VOID ExceptionFilterBegin(MethodDesc * pMethodDesc, PVOID pEntryEIP);
        static VOID ExceptionFilterEnd();

#else
        static VOID ExceptionThrown(CrawlFrame  *pCf, BOOL bIsReThrownException, BOOL bIsNewException) {};
        static VOID ExceptionThrownEnd() {};
        static VOID ExceptionCatchBegin(MethodDesc * pMethodDesc, PVOID pEntryEIP) {};
        static VOID ExceptionCatchEnd() {};
        static VOID ExceptionFinallyBegin(MethodDesc * pMethodDesc, PVOID pEntryEIP) {};
        static VOID ExceptionFinallyEnd() {};
        static VOID ExceptionFilterBegin(MethodDesc * pMethodDesc, PVOID pEntryEIP) {};
        static VOID ExceptionFilterEnd() {};
#endif // FEATURE_EVENT_TRACE
        typedef union _ExceptionStructs
        {
            typedef enum _ExceptionThrownFlags
            {
                HasInnerException=0x1,
                IsNestedException=0x2,
                IsReThrownException=0x4,
                IsCSE=0x8,
                IsCLSCompliant=0x10
            }ExceptionThrownFlags;
        }ExceptionStructs;
    };
    // Class to wrap all Contention logic for ETW
    class ContentionLog
    {
    public:
        typedef union _ContentionStructs
        {
            typedef  enum _ContentionFlags {
                ManagedContention=0,
                NativeContention=1
            } ContentionFlags;
        } ContentionStructs;
    };
    // Class to wrap all Interop logic for ETW
    class InteropLog
    {
    public:
    };

    // Class to wrap all Information logic for ETW
    class InfoLog
    {
    public:
        typedef union _InfoStructs
        {
            typedef enum _StartupMode
            {
                ManagedExe=0x1,
                HostedCLR=0x2,
                IJW=0x4,
                COMActivated=0x8,
                Other=0x10
            }StartupMode;

            typedef enum _Sku
            {
                DesktopCLR=0x1,
                CoreCLR=0x2,
                Mono=0x4
            }Sku;

            typedef enum _EtwMode
            {
                Normal=0x0,
                Callback=0x1
            }EtwMode;
        }InfoStructs;

#ifdef FEATURE_EVENT_TRACE
        static VOID RuntimeInformation(INT32 type);
#else
        static VOID RuntimeInformation(INT32 type) {};
#endif // FEATURE_EVENT_TRACE
    };

    class CodeSymbolLog
    {
    public:
#ifdef FEATURE_EVENT_TRACE
        static VOID EmitCodeSymbols(Module* pModule);
        static HRESULT GetInMemorySymbolsLength(Module* pModule, DWORD* pCountSymbolBytes);
        static HRESULT ReadInMemorySymbols(Module* pmodule, DWORD symbolsReadOffset, BYTE* pSymbolBytes,
            DWORD countSymbolBytes,    DWORD* pCountSymbolBytesRead);
#else
        static VOID EmitCodeSymbols(Module* pModule) {}
        static HRESULT GetInMemorySymbolsLength(Module* pModule, DWORD* pCountSymbolBytes) { return S_OK; }
        static HRESULT ReadInMemorySymbols(Module* pmodule, DWORD symbolsReadOffset, BYTE* pSymbolBytes,
            DWORD countSymbolBytes, DWORD* pCountSymbolBytesRead) {    return S_OK; }
#endif // FEATURE_EVENT_TRACE
    };

#define DISABLE_CONSTRUCT_COPY(T) \
    T() = delete; \
    T(const T &) = delete; \
    T &operator =(const T &) = delete

    // Class to wrap all Compilation logic for ETW
    class CompilationLog
    {
    public:
        class Runtime
        {
        public:
#ifdef FEATURE_EVENT_TRACE
            static bool IsEnabled();
#else
            static bool IsEnabled() { return false; }
#endif

            DISABLE_CONSTRUCT_COPY(Runtime);
        };

        class Rundown
        {
        public:
#ifdef FEATURE_EVENT_TRACE
            static bool IsEnabled();
#else
            static bool IsEnabled() { return false; }
#endif

            DISABLE_CONSTRUCT_COPY(Rundown);
        };

        // Class to wrap all TieredCompilation logic for ETW
        class TieredCompilation
        {
        private:
            static void GetSettings(UINT32 *flagsRef);

        public:
            class Runtime
            {
            public:
#ifdef FEATURE_EVENT_TRACE
                static bool IsEnabled();
                static void SendSettings();
                static void SendPause();
                static void SendResume(UINT32 newMethodCount);
                static void SendBackgroundJitStart(UINT32 pendingMethodCount);
                static void SendBackgroundJitStop(UINT32 pendingMethodCount, UINT32 jittedMethodCount);
#else
                static bool IsEnabled() { return false; }
                static void SendSettings() {}
                static void SendPause() {}
                static void SendResume(UINT32 newMethodCount) {}
                static void SendBackgroundJitStart(UINT32 pendingMethodCount) {}
                static void SendBackgroundJitStop(UINT32 pendingMethodCount, UINT32 jittedMethodCount) {}
#endif

                DISABLE_CONSTRUCT_COPY(Runtime);
            };

            class Rundown
            {
            public:
#ifdef FEATURE_EVENT_TRACE
                static bool IsEnabled();
                static void SendSettings();
#else
                static bool IsEnabled() { return false; }
                static void SendSettings() {}
#endif

                DISABLE_CONSTRUCT_COPY(Rundown);
            };

            DISABLE_CONSTRUCT_COPY(TieredCompilation);
        };

        DISABLE_CONSTRUCT_COPY(CompilationLog);
    };

#undef DISABLE_CONSTRUCT_COPY
};


#define ETW_IS_TRACE_ON(level) ( FALSE ) // for fusion which is eventually going to get removed
#define ETW_IS_FLAG_ON(flag) ( FALSE ) // for fusion which is eventually going to get removed

// Commonly used constats for ETW Assembly Loader and Assembly Binder events.
#define ETWLoadContextNotAvailable (LOADCTX_TYPE_HOSTED + 1)
#define ETWAppDomainIdNotAvailable 0 // Valid AppDomain IDs start from 1

#define ETWFieldUnused 0 // Indicates that a particular field in the ETW event payload template is currently unused.

#define ETWLoaderLoadTypeNotAvailable 0 // Static or Dynamic Load is only valid at LoaderPhaseStart and LoaderPhaseEnd events - for other events, 0 indicates "not available"
#define ETWLoaderStaticLoad 0 // Static reference load
#define ETWLoaderDynamicLoad 1 // Dynamic assembly load

#if defined(FEATURE_EVENT_TRACE) && !defined(HOST_UNIX)
//
// The ONE and only ONE global instantiation of this class
//
extern ETW::CEtwTracer *  g_pEtwTracer;

EXTERN_C DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context;
EXTERN_C DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context;
EXTERN_C DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context;
EXTERN_C DOTNET_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_DOTNET_Context;

//
// Special Handling of Startup events
//

// "mc.exe -MOF" already generates this block for XP-supported builds inside ClrEtwAll.h;
// on Vista+ builds, mc is run without -MOF, and we still have code that depends on it, so
// we manually place it here.
ETW_INLINE
ULONG
CoMofTemplate_h(
    _In_ REGHANDLE RegHandle,
    _In_ PCEVENT_DESCRIPTOR Descriptor,
    _In_opt_ LPCGUID EventGuid,
    _In_ const unsigned short  ClrInstanceID
    )
{
#define ARGUMENT_COUNT_h 1
    ULONG Error = ERROR_SUCCESS;
typedef struct _MCGEN_TRACE_BUFFER {
    EVENT_TRACE_HEADER Header;
    EVENT_DATA_DESCRIPTOR EventData[ARGUMENT_COUNT_h];
} MCGEN_TRACE_BUFFER;

    MCGEN_TRACE_BUFFER TraceBuf;
    PEVENT_DATA_DESCRIPTOR EventData = TraceBuf.EventData;

    EventDataDescCreate(&EventData[0], &ClrInstanceID, sizeof(const unsigned short)  );


  {
    Error = EventWrite(RegHandle, Descriptor, ARGUMENT_COUNT_h, EventData);

  }

#ifdef MCGEN_CALLOUT
MCGEN_CALLOUT(RegHandle,
              Descriptor,
              ARGUMENT_COUNT_h,
              EventData);
#endif

    return Error;
}

class ETWTraceStartup {
    REGHANDLE TraceHandle;
    PCEVENT_DESCRIPTOR EventStartDescriptor;
    LPCGUID EventStartGuid;
    PCEVENT_DESCRIPTOR EventEndDescriptor;
    LPCGUID EventEndGuid;
public:
    ETWTraceStartup(REGHANDLE _TraceHandle, PCEVENT_DESCRIPTOR _EventStartDescriptor, LPCGUID _EventStartGuid, PCEVENT_DESCRIPTOR _EventEndDescriptor, LPCGUID _EventEndGuid) {
        TraceHandle = _TraceHandle;
        EventStartDescriptor = _EventStartDescriptor;
        EventEndDescriptor = _EventEndDescriptor;
        EventStartGuid = _EventStartGuid;
        EventEndGuid = _EventEndGuid;
        StartupTraceEvent(TraceHandle, EventStartDescriptor, EventStartGuid);
    }
    ~ETWTraceStartup() {
        StartupTraceEvent(TraceHandle, EventEndDescriptor, EventEndGuid);
    }
    static void StartupTraceEvent(REGHANDLE _TraceHandle, PCEVENT_DESCRIPTOR _EventDescriptor, LPCGUID _EventGuid) {
        EVENT_DESCRIPTOR desc = *_EventDescriptor;
        if(ETW_TRACING_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context, desc))
        {
            CoMofTemplate_h(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.RegistrationHandle, _EventDescriptor, _EventGuid, GetClrInstanceId());
        }
    }
};
// "mc.exe -MOF" already generates this block for XP-supported builds inside ClrEtwAll.h;
// on Vista+ builds, mc is run without -MOF, and we still have code that depends on it, so
// we manually place it here.
FORCEINLINE
BOOLEAN __stdcall
McGenEventTracingEnabled(
    _In_ PMCGEN_TRACE_CONTEXT EnableInfo,
    _In_ PCEVENT_DESCRIPTOR EventDescriptor
    )
{

    if(!EnableInfo){
        return FALSE;
    }


    //
    // Check if the event Level is lower than the level at which
    // the channel is enabled.
    // If the event Level is 0 or the channel is enabled at level 0,
    // all levels are enabled.
    //

    if ((EventDescriptor->Level <= EnableInfo->Level) || // This also covers the case of Level == 0.
        (EnableInfo->Level == 0)) {

        //
        // Check if Keyword is enabled
        //

        if ((EventDescriptor->Keyword == (ULONGLONG)0) ||
            ((EventDescriptor->Keyword & EnableInfo->MatchAnyKeyword) &&
             ((EventDescriptor->Keyword & EnableInfo->MatchAllKeyword) == EnableInfo->MatchAllKeyword))) {
            return TRUE;
        }
    }

    return FALSE;
}


ETW_INLINE
ULONG
ETW::SamplingLog::SendStackTrace(
    MCGEN_TRACE_CONTEXT TraceContext,
    PCEVENT_DESCRIPTOR Descriptor,
    LPCGUID EventGuid)
{
#define ARGUMENT_COUNT_CLRStackWalk 5
    ULONG Result = ERROR_SUCCESS;
typedef struct _MCGEN_TRACE_BUFFER {
    EVENT_TRACE_HEADER Header;
    EVENT_DATA_DESCRIPTOR EventData[ARGUMENT_COUNT_CLRStackWalk];
} MCGEN_TRACE_BUFFER;

    REGHANDLE RegHandle = TraceContext.RegistrationHandle;
    if(!TraceContext.IsEnabled || !McGenEventTracingEnabled(&TraceContext, Descriptor))
    {
        return Result;
    }

    PVOID *Stack = NULL;
    UINT32 FrameCount = 0;
    ETW::SamplingLog stackObj;
    if(stackObj.GetCurrentThreadsCallStack(&FrameCount, &Stack) == ETW::SamplingLog::Completed)
    {
        UCHAR Reserved1=0, Reserved2=0;
        UINT16 ClrInstanceId = GetClrInstanceId();
        MCGEN_TRACE_BUFFER TraceBuf;
        PEVENT_DATA_DESCRIPTOR EventData = TraceBuf.EventData;

        EventDataDescCreate(&EventData[0], &ClrInstanceId, sizeof(const UINT16)  );

        EventDataDescCreate(&EventData[1], &Reserved1, sizeof(const UCHAR)  );

        EventDataDescCreate(&EventData[2], &Reserved2, sizeof(const UCHAR)  );

        EventDataDescCreate(&EventData[3], &FrameCount, sizeof(const unsigned int)  );

        EventDataDescCreate(&EventData[4], Stack, sizeof(PVOID) * FrameCount );

        return EventWrite(RegHandle, Descriptor, ARGUMENT_COUNT_CLRStackWalk, EventData);
    }
    return Result;
};

#endif // FEATURE_EVENT_TRACE && !defined(HOST_UNIX)
#ifdef FEATURE_EVENT_TRACE
#ifdef TARGET_X86
struct CallStackFrame
{
    struct CallStackFrame* m_Next;
    SIZE_T m_ReturnAddress;
};
#endif // TARGET_X86
#endif // FEATURE_EVENT_TRACE

#if defined(FEATURE_EVENT_TRACE) && !defined(HOST_UNIX)
FORCEINLINE
BOOLEAN __stdcall
McGenEventProviderEnabled(
    _In_ PMCGEN_TRACE_CONTEXT Context,
    _In_ UCHAR Level,
    _In_ ULONGLONG Keyword
    )
{
    if(!Context) {
        return FALSE;
    }

    //
    // Check if the event Level is lower than the level at which
    // the channel is enabled.
    // If the event Level is 0 or the channel is enabled at level 0,
    // all levels are enabled.
    //

    if ((Level <= Context->Level) || // This also covers the case of Level == 0.
        (Context->Level == 0)) {

        //
        // Check if Keyword is enabled
        //

        if ((Keyword == (ULONGLONG)0) ||
            ((Keyword & Context->MatchAnyKeyword) &&
             ((Keyword & Context->MatchAllKeyword) == Context->MatchAllKeyword))) {
            return TRUE;
        }
    }
    return FALSE;
}
#endif // FEATURE_EVENT_TRACE && !defined(HOST_UNIX)


#endif // !FEATURE_NATIVEAOT

// These parts of the ETW namespace are common for both FEATURE_NATIVEAOT and
// !FEATURE_NATIVEAOT builds.


struct ProfilingScanContext;
class Object;

namespace ETW
{
    // Class to wrap the logging of threads (runtime and rundown providers)
    class ThreadLog
    {
    private:
        static DWORD GetEtwThreadFlags(Thread * pThread);

    public:
        static VOID FireThreadCreated(Thread * pThread);
        static VOID FireThreadDC(Thread * pThread);
    };
};


#endif //_ETWTRACER_HXX_
