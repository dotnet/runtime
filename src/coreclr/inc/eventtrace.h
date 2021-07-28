// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: eventtrace.h
// Abstract: This module implements Event Tracing support.  This includes
// eventtracebase.h, and adds VM-specific ETW helpers to support features like type
// logging, allocation logging, and gc heap walk logging.
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

// Mac
// DTrace is similar to ETW and has been made to look like ETW at most of the places.
// For convenience, it is called ETM (Event Tracing for Mac) and exists only on the Mac Leopard OS
// ============================================================================

#ifndef _VMEVENTTRACE_H_
#define _VMEVENTTRACE_H_

#include "eventtracebase.h"
#include "gcinterface.h"

#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
struct ProfilingScanContext : ScanContext
{
    BOOL fProfilerPinned;
    void * pvEtwContext;
    void *pHeapId;

    ProfilingScanContext(BOOL fProfilerPinnedParam) : ScanContext()
    {
        LIMITED_METHOD_CONTRACT;

        pHeapId = NULL;
        fProfilerPinned = fProfilerPinnedParam;
        pvEtwContext = NULL;
#ifdef FEATURE_CONSERVATIVE_GC
        // To not confuse GCScan::GcScanRoots
        promotion = g_pConfig->GetGCConservative();
#endif
    }
};
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

#ifndef FEATURE_REDHAWK

namespace ETW
{
    class LoggedTypesFromModule;

    // We keep a hash of these to keep track of:
    //     * Which types have been logged through ETW (so we can avoid logging dupe Type
    //         events), and
    //     * GCSampledObjectAllocation stats to help with "smart sampling" which
    //         dynamically adjusts sampling rate of objects by type.
    // See code:LoggedTypesFromModuleTraits
    struct TypeLoggingInfo
    {
    public:
        TypeLoggingInfo(TypeHandle thParam)
        {
            Init(thParam);
        }

        TypeLoggingInfo()
        {
            Init(TypeHandle());
        }

        void Init(TypeHandle thParam)
        {
            th = thParam;
            dwTickOfCurrentTimeBucket = 0;
            dwAllocCountInCurrentBucket = 0;
            flAllocPerMSec = 0;

            dwAllocsToSkipPerSample = 0;
            dwAllocsSkippedForSample = 0;
            cbIgnoredSizeForSample = 0;
        };

        // The type this TypeLoggingInfo represents
        TypeHandle th;

        // Smart sampling

        // These bucket values remember stats of a particular time slice that are used to
        // help adjust the sampling rate
        DWORD dwTickOfCurrentTimeBucket;
        DWORD dwAllocCountInCurrentBucket;
        float flAllocPerMSec;

        // The number of data points to ignore before taking a "sample" (i.e., logging a
        // GCSampledObjectAllocation ETW event for this type)
        DWORD dwAllocsToSkipPerSample;

        // The current number of data points actually ignored for the current sample
        DWORD dwAllocsSkippedForSample;

        // The current count of bytes of objects of this type actually allocated (and
        // ignored) for the current sample
        SIZE_T cbIgnoredSizeForSample;
    };

    // Class to wrap all type system logic for ETW
    class TypeSystemLog
    {
    private:
        // Global type hash
        static AllLoggedTypes *s_pAllLoggedTypes;

        // An unsigned value that gets incremented whenever a global change is made.
        // When this occurs, threads must synchronize themselves with the global state.
        // Examples include unloading of modules and disabling of allocation sampling.
        static unsigned int s_nEpoch;

        // See code:ETW::TypeSystemLog::PostRegistrationInit
        static BOOL s_fHeapAllocEventEnabledOnStartup;
        static BOOL s_fHeapAllocHighEventEnabledNow;
        static BOOL s_fHeapAllocLowEventEnabledNow;

        // If COMPLUS_UNSUPPORTED_ETW_ObjectAllocationEventsPerTypePerSec is set, then
        // this is used to determine the event frequency, overriding
        // s_nDefaultMsBetweenEvents above (regardless of which
        // GCSampledObjectAllocation*Keyword was used)
        static int s_nCustomMsBetweenEvents;

    public:
        // This customizes the type logging behavior in LogTypeAndParametersIfNecessary
        enum TypeLogBehavior
        {
            // Take lock, and consult hash table to see if this is the first time we've
            // encountered the type, in which case, log it
            kTypeLogBehaviorTakeLockAndLogIfFirstTime,

            // Don't take lock, don't consult hash table. Just log the type. (This is
            // used in cases when checking for dupe type logging isn't worth it, such as
            // when logging the finalization of an object.)
            kTypeLogBehaviorAlwaysLog,

            // When logging the type for GCSampledObjectAllocation events,
            // we already know we need to log the type (since we already
            // looked it up in the hash).  But we would still need to consult the hash
            // for any type parameters, so kTypeLogBehaviorAlwaysLog isn't appropriate,
            // and this is used instead.
            kTypeLogBehaviorAlwaysLogTopLevelType,
        };

        static HRESULT PreRegistrationInit();
        static void PostRegistrationInit();
        static BOOL IsHeapAllocEventEnabled();
        static void SendObjectAllocatedEvent(Object * pObject);
        static CrstBase * GetHashCrst();
        static VOID LogTypeAndParametersIfNecessary(BulkTypeEventLogger * pBulkTypeEventLogger, ULONGLONG thAsAddr, TypeLogBehavior typeLogBehavior);
        static VOID OnModuleUnload(Module * pModule);
        static void OnKeywordsChanged();
        static void Cleanup();
        static VOID DeleteTypeHashNoLock(AllLoggedTypes **ppAllLoggedTypes);
        static VOID FlushObjectAllocationEvents();
        static UINT32 TypeLoadBegin();
        static VOID TypeLoadEnd(UINT32 typeLoad, TypeHandle th, UINT16 loadLevel);

    private:
        static BOOL ShouldLogType(TypeHandle th);
        static TypeLoggingInfo LookupOrCreateTypeLoggingInfo(TypeHandle th, BOOL * pfCreatedNew, LoggedTypesFromModule ** ppLoggedTypesFromModule = NULL);
        static BOOL AddTypeToGlobalCacheIfNotExists(TypeHandle th, BOOL * pfCreatedNew);
        static BOOL AddOrReplaceTypeLoggingInfo(ETW::LoggedTypesFromModule * pLoggedTypesFromModule, const ETW::TypeLoggingInfo * pTypeLoggingInfo);
        static int GetDefaultMsBetweenEvents();
        static VOID OnTypesKeywordTurnedOff();
    };

#endif // FEATURE_REDHAWK


    // Class to wrap all GC logic for ETW
    class GCLog
    {
    private:
        // When WPA triggers a GC, it gives us this unique number to append to our
        // GCStart event so WPA can correlate the CLR's GC with the JScript GC they
        // triggered at the same time.
        //
        // We set this value when the GC is triggered, and then retrieve the value on the
        // first subsequent FireGcStart() method call for a full, induced GC, assuming
        // that that's the GC that WPA triggered. This is imperfect, and if we were in
        // the act of beginning another full, induced GC (for some other reason), then
        // we'll attach this sequence number to that GC instead of to the WPA-induced GC,
        // but who cares? When parsing ETW logs later on, it's indistinguishable if both
        // GCs really were induced at around the same time.
#ifdef FEATURE_REDHAWK
        static volatile LONGLONG s_l64LastClientSequenceNumber;
#else // FEATURE_REDHAWK
        static Volatile<LONGLONG> s_l64LastClientSequenceNumber;
#endif // FEATURE_REDHAWK

    public:
        typedef union st_GCEventInfo {
            // These values are gotten from the gc_reason
            // in gcimpl.h
            typedef  enum _GC_REASON {
                GC_ALLOC_SOH = 0,
                GC_INDUCED = 1,
                GC_LOWMEMORY = 2,
                GC_EMPTY = 3,
                GC_ALLOC_LOH = 4,
                GC_OOS_SOH = 5,
                GC_OOS_LOH = 6,
                GC_INDUCED_NOFORCE = 7,
                GC_GCSTRESS = 8,
                GC_LOWMEMORY_BLOCKING = 9,
                GC_INDUCED_COMPACTING = 10,
                GC_LOWMEMORY_HOST = 11
            } GC_REASON;
            typedef  enum _GC_TYPE {
                GC_NGC = 0,
                GC_BGC = 1,
                GC_FGC = 2
            } GC_TYPE;
            struct {
                ULONG Count;
                ULONG Depth;
                GC_REASON Reason;
                GC_TYPE Type;
            } GCStart;
            struct {
                ULONG Reason;
                // This is only valid when SuspendEE is called by GC (ie, Reason is either
                // SUSPEND_FOR_GC or SUSPEND_FOR_GC_PREP.
                ULONG GcCount;
            } SuspendEE;
            struct {
                ULONGLONG SegmentSize;
                ULONGLONG LargeObjectSegmentSize;
                BOOL ServerGC; // TRUE means it's server GC; FALSE means it's workstation.
            } GCSettings;
        } ETW_GC_INFO, *PETW_GC_INFO;

#ifdef FEATURE_EVENT_TRACE
        static VOID GCSettingsEvent();
#else
        static VOID GCSettingsEvent() {};
#endif // FEATURE_EVENT_TRACE

        static BOOL ShouldWalkHeapObjectsForEtw();
        static BOOL ShouldWalkHeapRootsForEtw();
        static BOOL ShouldTrackMovementForEtw();
        static HRESULT ForceGCForDiagnostics();
        static VOID ForceGC(LONGLONG l64ClientSequenceNumber);
        static VOID FireGcStart(ETW_GC_INFO * pGcInfo);
        static VOID RootReference(
            LPVOID pvHandle,
            Object * pRootedNode,
            Object * pSecondaryNodeForDependentHandle,
            BOOL fDependentHandle,
            ProfilingScanContext * profilingScanContext,
            DWORD dwGCFlags,
            DWORD rootFlags);
        static VOID ObjectReference(
            ProfilerWalkHeapContext * profilerWalkHeapContext,
            Object * pObjReferenceSource,
            ULONGLONG typeID,
            ULONGLONG cRefs,
            Object ** rgObjReferenceTargets);
        static BOOL ShouldWalkStaticsAndCOMForEtw();
        static VOID WalkStaticsAndCOMForETW();
        static VOID EndHeapDump(ProfilerWalkHeapContext * profilerWalkHeapContext);
#ifdef FEATURE_EVENT_TRACE
        static VOID BeginMovedReferences(size_t * pProfilingContext);
        static VOID MovedReference(BYTE * pbMemBlockStart, BYTE * pbMemBlockEnd, ptrdiff_t cbRelocDistance, size_t profilingContext, BOOL fCompacting, BOOL fAllowProfApiNotification = TRUE);
        static VOID EndMovedReferences(size_t profilingContext, BOOL fAllowProfApiNotification = TRUE);
#else
        // TODO: Need to be implemented for PROFILING_SUPPORTED.
        static VOID BeginMovedReferences(size_t * pProfilingContext) {};
        static VOID MovedReference(BYTE * pbMemBlockStart, BYTE * pbMemBlockEnd, ptrdiff_t cbRelocDistance, size_t profilingContext, BOOL fCompacting, BOOL fAllowProfApiNotification = TRUE) {};
        static VOID EndMovedReferences(size_t profilingContext, BOOL fAllowProfApiNotification = TRUE) {};
#endif // FEATURE_EVENT_TRACE
        static VOID SendFinalizeObjectEvent(MethodTable * pMT, Object * pObj);
    };
};


#endif //_VMEVENTTRACE_H_
