// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

typedef DWORD NativeCodeVersionId; // keep in sync with codeversion.h

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
};

enum EtwThreadFlags
{
    kEtwThreadFlagGCSpecial =         0x00000001,
    kEtwThreadFlagFinalizer =         0x00000002,
    kEtwThreadFlagThreadPoolWorker =  0x00000004,
};

#ifndef FEATURE_REDHAWK

#if defined(FEATURE_EVENT_TRACE)

#if defined(FEATURE_PERFTRACING)
#define EVENT_PIPE_ENABLED() (EventPipeHelper::Enabled())
#else
#define EVENT_PIPE_ENABLED() (FALSE)
#endif

#if  !defined(FEATURE_PAL)

//
// Use this macro at the least before calling the Event Macros
//

#define ETW_TRACING_INITIALIZED(RegHandle) \
    ((g_pEtwTracer && RegHandle) || EVENT_PIPE_ENABLED())

//
// Use this macro to check if an event is enabled
// if the fields in the event are not cheap to calculate
//
#define ETW_EVENT_ENABLED(Context, EventDescriptor) \
    ((MCGEN_ENABLE_CHECK(Context, EventDescriptor)) || EVENT_PIPE_ENABLED())

//
// Use this macro to check if a category of events is enabled
//

#define ETW_CATEGORY_ENABLED(Context, Level, Keyword) \
    ((Context.IsEnabled && McGenEventProviderEnabled(&Context, Level, Keyword)) || EVENT_PIPE_ENABLED())


// This macro only checks if a provider is enabled
// It does not check the flags and keywords for which it is enabled
#define ETW_PROVIDER_ENABLED(ProviderSymbol)                 \
        ((ProviderSymbol##_Context.IsEnabled) || EVENT_PIPE_ENABLED())


#else //defined(FEATURE_PAL)
#if defined(FEATURE_PERFTRACING)
#define ETW_INLINE
#define ETWOnStartup(StartEventName, EndEventName)
#define ETWFireEvent(EventName)

#define ETW_TRACING_INITIALIZED(RegHandle) (TRUE)
#define ETW_EVENT_ENABLED(Context, EventDescriptor) (EventPipeHelper::Enabled() || XplatEventLogger::IsEventLoggingEnabled())
#define ETW_CATEGORY_ENABLED(Context, Level, Keyword) (EventPipeHelper::Enabled() || XplatEventLogger::IsEventLoggingEnabled())
#define ETW_TRACING_ENABLED(Context, EventDescriptor) (EventEnabled##EventDescriptor())
#define ETW_TRACING_CATEGORY_ENABLED(Context, Level, Keyword) (EventPipeHelper::Enabled() || XplatEventLogger::IsEventLoggingEnabled())
#define ETW_PROVIDER_ENABLED(ProviderSymbol) (TRUE)
#else //defined(FEATURE_PERFTRACING)
#define ETW_INLINE
#define ETWOnStartup(StartEventName, EndEventName)
#define ETWFireEvent(EventName)

#define ETW_TRACING_INITIALIZED(RegHandle) (TRUE)
#define ETW_EVENT_ENABLED(Context, EventDescriptor) (XplatEventLogger::IsEventLoggingEnabled())
#define ETW_CATEGORY_ENABLED(Context, Level, Keyword) (XplatEventLogger::IsEventLoggingEnabled())
#define ETW_TRACING_ENABLED(Context, EventDescriptor) (EventEnabled##EventDescriptor())
#define ETW_TRACING_CATEGORY_ENABLED(Context, Level, Keyword) (XplatEventLogger::IsEventLoggingEnabled())
#define ETW_PROVIDER_ENABLED(ProviderSymbol) (TRUE)
#endif // defined(FEATURE_PERFTRACING)
#endif // !defined(FEATURE_PAL)

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

#endif // FEATURE_REDHAWK

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
#if !defined(FEATURE_PAL)

/***************************************/
/* Tracing levels supported by CLR ETW */
/***************************************/
#define ETWMAX_TRACE_LEVEL 6        // Maximum Number of Trace Levels supported
#define TRACE_LEVEL_NONE        0   // Tracing is not on
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
    ((Context.IsEnabled && ETW_TRACING_INITIALIZED(Context.RegistrationHandle) && ETW_EVENT_ENABLED(Context, EventDescriptor)) || EVENT_PIPE_ENABLED())

//
// Using KEYWORDZERO means when checking the events category ignore the keyword
//
#define KEYWORDZERO 0x0

//
// Use this macro to check if ETW is initialized and the category is enabled
//
#define ETW_TRACING_CATEGORY_ENABLED(Context, Level, Keyword) \
    ((ETW_TRACING_INITIALIZED(Context.RegistrationHandle) && ETW_CATEGORY_ENABLED(Context, Level, Keyword)) || EVENT_PIPE_ENABLED())

    #define ETWOnStartup(StartEventName, EndEventName) \
        ETWTraceStartup trace##StartEventName##(Microsoft_Windows_DotNETRuntimePrivateHandle, &StartEventName, &StartupId, &EndEventName, &StartupId);
    #define ETWFireEvent(EventName) \
        ETWTraceStartup::StartupTraceEvent(Microsoft_Windows_DotNETRuntimePrivateHandle, &EventName, &StartupId);

#ifndef FEATURE_REDHAWK
// Headers
#include <initguid.h>
#include <wmistr.h>
#include <evntrace.h>
#include <evntprov.h>
#endif //!FEATURE_REDHAWK
#endif //!defined(FEATURE_PAL)


#else // FEATURE_EVENT_TRACE

#include "etmdummy.h"
#endif // FEATURE_EVENT_TRACE

#ifndef FEATURE_REDHAWK

#include "corprof.h"

// g_nClrInstanceId is defined in Utilcode\Util.cpp. The definition goes into Utilcode.lib.
// This enables both the VM and Utilcode to raise ETW events.
extern UINT32 g_nClrInstanceId;

#define GetClrInstanceId()  (static_cast<UINT16>(g_nClrInstanceId))

#if defined(FEATURE_PERFTRACING)
class EventPipeHelper
{
public:
    static bool Enabled();
};
#endif // defined(FEATURE_PERFTRACING)

#if defined(FEATURE_EVENT_TRACE) || defined(FEATURE_EVENTSOURCE_XPLAT)

#include "clrconfig.h"
 class XplatEventLogger
{
    public:
        inline static BOOL  IsEventLoggingEnabled()
        {
            static ConfigDWORD configEventLogging;
            return configEventLogging.val(CLRConfig::EXTERNAL_EnableEventLog);
        }
};

#endif //defined(FEATURE_EVENT_TRACE)

#if defined(FEATURE_EVENT_TRACE)

struct EventFilterDescriptor;

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

#ifndef  FEATURE_PAL
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
        EtwCallback(NULL /* SourceId */, (RequestCode==WMI_ENABLE_EVENTS) ? EVENT_CONTROL_CODE_ENABLE_PROVIDER : EVENT_CONTROL_CODE_DISABLE_PROVIDER, 0 /* Level */, 0 /* MatchAnyKeyword */, 0 /* MatchAllKeyword */, NULL /* FilterData */, Context)

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

#endif //!FEATURE_PAL

#include "clretwallmain.h"

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


// All ETW helpers must be a part of this namespace
// We have auto-generated macros to directly fire the events
// but in some cases, gathering the event payload information involves some work
// and it can be done in a relevant helper class like the one's in this namespace
namespace ETW
{
    // Class to wrap the ETW infrastructure logic
#if  !defined(FEATURE_PAL)
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
#endif // !defined(FEATURE_PAL)

    class LoaderLog;
    class MethodLog;
    // Class to wrap all the enumeration logic for ETW
    class EnumerationLog
    {
        friend class ETW::LoaderLog;
        friend class ETW::MethodLog;
#ifdef FEATURE_EVENT_TRACE
        static VOID SendThreadRundownEvent();
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

                // Helpers
                ModuleRangeEnabledAny = ModuleRangeLoad | ModuleRangeDCStart | ModuleRangeDCEnd | ModuleRangeLoadPrivate,
                JitMethodLoadOrDCStartAny = JitMethodLoad | JitMethodDCStart | MethodDCStartILToNativeMap,
                JitMethodUnloadOrDCEndAny = JitMethodUnload | JitMethodDCEnd | MethodDCEndILToNativeMap,
            }EnumerationOptions;
        }EnumerationStructs;

        static VOID ProcessShutdown();
        static VOID ModuleRangeRundown();
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
#if defined(FEATURE_EVENT_TRACE) && !defined(FEATURE_PAL)
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
#endif // FEATURE_EVENT_TRACE && !defined(FEATURE_PAL)
    };

    // Class to wrap all Loader logic for ETW
    class LoaderLog
    {
        friend class ETW::EnumerationLog;
#if defined(FEATURE_EVENT_TRACE)
        static VOID SendModuleEvent(Module *pModule, DWORD dwEventOptions, BOOL bFireDomainModuleEvents=FALSE);
        static ULONG SendModuleRange(__in Module *pModule, __in DWORD dwEventOptions);
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

        static VOID DomainLoadReal(BaseDomain *pDomain, __in_opt LPWSTR wszFriendlyName=NULL);

        static VOID DomainLoad(BaseDomain *pDomain, __in_opt LPWSTR wszFriendlyName = NULL)
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
        static VOID DomainLoad(BaseDomain *pDomain, __in_opt LPWSTR wszFriendlyName=NULL) {};
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
            BOOL fGetCodeIds);
        static VOID SendEventsForNgenMethods(Module *pModule, DWORD dwEventOptions);
        static VOID SendMethodJitStartEvent(MethodDesc *pMethodDesc, SString *namespaceOrClassName=NULL, SString *methodName=NULL, SString *methodSignature=NULL);
        static VOID SendMethodILToNativeMapEvent(MethodDesc * pMethodDesc, DWORD dwEventOptions, PCODE pNativeCodeStartAddress, ReJITID ilCodeId);
        static VOID SendMethodEvent(MethodDesc *pMethodDesc, DWORD dwEventOptions, BOOL bIsJit, SString *namespaceOrClassName=NULL, SString *methodName=NULL, SString *methodSignature=NULL, PCODE pNativeCodeStartAddress = 0, NativeCodeVersionId nativeCodeId = 0, BOOL bProfilerRejectedPrecompiledCode = FALSE, BOOL bReadyToRunRejectedPrecompiledCode = FALSE);
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
            }MethodFlags;

            typedef enum _MethodExtent
            {
                HotSection=0x00000000,
                ColdSection=0x10000000
            }MethodExtent;

        }MethodStructs;

        static VOID GetR2RGetEntryPoint(MethodDesc *pMethodDesc, PCODE pEntryPoint);
        static VOID MethodJitting(MethodDesc *pMethodDesc, SString *namespaceOrClassName=NULL, SString *methodName=NULL, SString *methodSignature=NULL);
        static VOID MethodJitted(MethodDesc *pMethodDesc, SString *namespaceOrClassName=NULL, SString *methodName=NULL, SString *methodSignature=NULL, PCODE pNativeCodeStartAddress = 0, ReJITID ilCodeId = 0, NativeCodeVersionId nativeCodeId = 0, BOOL bProfilerRejectedPrecompiledCode = FALSE, BOOL bReadyToRunRejectedPrecompiledCode = FALSE);
        static VOID StubInitialized(ULONGLONG ullHelperStartAddress, LPCWSTR pHelperName);
        static VOID StubsInitialized(PVOID *pHelperStartAddresss, PVOID *pHelperNames, LONG ulNoOfHelpers);
        static VOID MethodRestored(MethodDesc * pMethodDesc);
        static VOID MethodTableRestored(MethodTable * pMethodTable);
        static VOID DynamicMethodDestroyed(MethodDesc *pMethodDesc);
#else // FEATURE_EVENT_TRACE
    public:
        static VOID GetR2RGetEntryPoint(MethodDesc *pMethodDesc, PCODE pEntryPoint) {};
        static VOID MethodJitting(MethodDesc *pMethodDesc, SString *namespaceOrClassName=NULL, SString *methodName=NULL, SString *methodSignature=NULL) {};
        static VOID MethodJitted(MethodDesc *pMethodDesc, SString *namespaceOrClassName=NULL, SString *methodName=NULL, SString *methodSignature=NULL, PCODE pNativeCodeStartAddress = 0, ReJITID ilCodeId = 0, NativeCodeVersionId nativeCodeId = 0, BOOL bProfilerRejectedPrecompiledCode = FALSE, BOOL bReadyToRunRejectedPrecompiledCode = FALSE) {};
        static VOID StubInitialized(ULONGLONG ullHelperStartAddress, LPCWSTR pHelperName) {};
        static VOID StubsInitialized(PVOID *pHelperStartAddresss, PVOID *pHelperNames, LONG ulNoOfHelpers) {};
        static VOID MethodRestored(MethodDesc * pMethodDesc) {};
        static VOID MethodTableRestored(MethodTable * pMethodTable) {};
        static VOID DynamicMethodDestroyed(MethodDesc *pMethodDesc) {};
#endif // FEATURE_EVENT_TRACE
    };

    // Class to wrap all Security logic for ETW
    class SecurityLog
    {
#ifdef FEATURE_EVENT_TRACE
    public:
        static VOID StrongNameVerificationStart(DWORD dwInFlags, __in LPWSTR strFullyQualifiedAssemblyName);
        static VOID StrongNameVerificationStop(DWORD dwInFlags,ULONG result, __in LPWSTR strFullyQualifiedAssemblyName);

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
                CoreCLR=0x2
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

#if defined(FEATURE_EVENT_TRACE) && !defined(FEATURE_PAL)
//
// The ONE and only ONE global instantiation of this class
//
extern ETW::CEtwTracer *  g_pEtwTracer;

//
// Special Handling of Startup events
//

// "mc.exe -MOF" already generates this block for XP-suported builds inside ClrEtwAll.h;
// on Vista+ builds, mc is run without -MOF, and we still have code that depends on it, so
// we manually place it here.
ETW_INLINE
ULONG
CoMofTemplate_h(
    __in REGHANDLE RegHandle,
    __in PCEVENT_DESCRIPTOR Descriptor,
    __in_opt LPCGUID EventGuid,
    __in const unsigned short  ClrInstanceID
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
        if(ETW_TRACING_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, desc))
        {
            CoMofTemplate_h(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context.RegistrationHandle, _EventDescriptor, _EventGuid, GetClrInstanceId());
        }
    }
};
// "mc.exe -MOF" already generates this block for XP-suported builds inside ClrEtwAll.h;
// on Vista+ builds, mc is run without -MOF, and we still have code that depends on it, so
// we manually place it here.
FORCEINLINE
BOOLEAN __stdcall
McGenEventTracingEnabled(
    __in PMCGEN_TRACE_CONTEXT EnableInfo,
    __in PCEVENT_DESCRIPTOR EventDescriptor
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

#endif // FEATURE_EVENT_TRACE && !defined(FEATURE_PAL)
#ifdef FEATURE_EVENT_TRACE
#ifdef _TARGET_X86_
struct CallStackFrame
{
    struct CallStackFrame* m_Next;
    SIZE_T m_ReturnAddress;
};
#endif // _TARGET_X86_
#endif // FEATURE_EVENT_TRACE

#if defined(FEATURE_EVENT_TRACE) && !defined(FEATURE_PAL)
FORCEINLINE
BOOLEAN __stdcall
McGenEventProviderEnabled(
    __in PMCGEN_TRACE_CONTEXT Context,
    __in UCHAR Level,
    __in ULONGLONG Keyword
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
#endif // FEATURE_EVENT_TRACE && !defined(FEATURE_PAL)


#endif // !FEATURE_REDHAWK

// These parts of the ETW namespace are common for both FEATURE_REDHAWK and
// !FEATURE_REDHAWK builds.


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
