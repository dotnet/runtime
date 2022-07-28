// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This header provides Redhawk-specific ETW code and macros, to allow sharing of common
// ETW code between Redhawk and desktop CLR.
//
#ifndef __RHEVENTTRACE_INCLUDED
#define __RHEVENTTRACE_INCLUDED


#ifdef FEATURE_ETW

// FireEtwGCPerHeapHistorySpecial() has to be defined manually rather than via the manifest because it does
// not have a standard signature.
#define FireEtwGCPerHeapHistorySpecial(DataPerHeap, DataSize, ClrId) (MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_Context.IsEnabled && PalEventEnabled(Microsoft_Windows_Redhawk_GC_PrivateHandle, &GCPerHeapHistory)) ? Template_GCPerHeapHistorySpecial(Microsoft_Windows_Redhawk_GC_PrivateHandle, &GCPerHeapHistory, DataPerHeap, DataSize, ClrId) : 0

// Map the CLR private provider to our version so we can avoid inserting more #ifdef's in the code.
#define MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context MICROSOFT_WINDOWS_NATIVEAOT_GC_PRIVATE_PROVIDER_Context
#define MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context MICROSOFT_WINDOWS_NATIVEAOT_GC_PUBLIC_PROVIDER_Context
#define Microsoft_Windows_DotNETRuntimeHandle Microsoft_Windows_Redhawk_GC_PublicHandle

#define CLR_GC_KEYWORD 0x1
#define CLR_FUSION_KEYWORD 0x4
#define CLR_LOADER_KEYWORD 0x8
#define CLR_JIT_KEYWORD 0x10
#define CLR_NGEN_KEYWORD 0x20
#define CLR_STARTENUMERATION_KEYWORD 0x40
#define CLR_ENDENUMERATION_KEYWORD 0x80
#define CLR_SECURITY_KEYWORD 0x400
#define CLR_APPDOMAINRESOURCEMANAGEMENT_KEYWORD 0x800
#define CLR_JITTRACING_KEYWORD 0x1000
#define CLR_INTEROP_KEYWORD 0x2000
#define CLR_CONTENTION_KEYWORD 0x4000
#define CLR_EXCEPTION_KEYWORD 0x8000
#define CLR_THREADING_KEYWORD 0x10000
#define CLR_JITTEDMETHODILTONATIVEMAP_KEYWORD 0x20000
#define CLR_OVERRIDEANDSUPPRESSNGENEVENTS_KEYWORD 0x40000
#define CLR_TYPE_KEYWORD 0x80000
#define CLR_GCHEAPDUMP_KEYWORD 0x100000
#define CLR_GCHEAPALLOC_KEYWORD 0x200000
#define CLR_GCHEAPSURVIVALANDMOVEMENT_KEYWORD 0x400000
#define CLR_GCHEAPCOLLECT_KEYWORD 0x800000
#define CLR_GCHEAPANDTYPENAMES_KEYWORD 0x1000000
#define CLR_PERFTRACK_KEYWORD 0x20000000
#define CLR_STACK_KEYWORD 0x40000000
#ifndef ERROR_SUCCESS
#define ERROR_SUCCESS   0
#endif

#undef ETW_TRACING_INITIALIZED
#define ETW_TRACING_INITIALIZED(RegHandle) (RegHandle != NULL)

#undef ETW_CATEGORY_ENABLED
#define ETW_CATEGORY_ENABLED(Context, LevelParam, Keyword) \
    (Context.IsEnabled &&                                                               \
    (                                                                                   \
        (LevelParam <= ((Context).Level)) ||                                                    \
        ((Context.Level) == 0)                                                           \
    ) &&                                                                                \
    (   \
        (Keyword == (ULONGLONG)0) ||    \
        (   \
            (Keyword & (Context.MatchAnyKeyword)) && \
            (   \
                (Keyword & (Context.MatchAllKeyword)) == (Context.MatchAllKeyword)    \
            )   \
        )   \
    )   \
    )

class MethodTable;
class BulkTypeEventLogger;

namespace ETW
{
    // Class to wrap all type system logic for ETW
    class TypeSystemLog
    {
    public:
        // This enum is unused on Redhawk, but remains here to keep Redhawk / desktop CLR
        // code shareable.
        enum TypeLogBehavior
        {
            kTypeLogBehaviorTakeLockAndLogIfFirstTime,
            kTypeLogBehaviorAssumeLockAndLogIfFirstTime,
            kTypeLogBehaviorAlwaysLog,
        };

        static void LogTypeAndParametersIfNecessary(BulkTypeEventLogger * pLogger, uint64_t thAsAddr, TypeLogBehavior typeLogBehavior);
    };
};

#else
#define FireEtwGCPerHeapHistorySpecial(DataPerHeap, DataSize, ClrId)
#endif

#endif //__RHEVENTTRACE_INCLUDED
