//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// switches.h switch configuration of common runtime features
//


#ifndef CROSSGEN_COMPILE
#define STRESS_HEAP
#endif

#if !defined(CROSSGEN_COMPILE) && !defined(FEATURE_CORECLR)
#define STRESS_THREAD
#endif

// On CoreCLR, define VERIFY_HEAP only in debug builds
#if defined(_DEBUG) || !defined(FEATURE_CORECLR)
#define VERIFY_HEAP
#endif

// define this to test data safety for the DAC. See code:DataTest::TestDataSafety. 
#define TEST_DATA_CONSISTENCY

#if !defined(STRESS_LOG) && !defined(FEATURE_UTILCODE_NO_DEPENDENCIES)
#define STRESS_LOG
#endif

#if defined(_DEBUG) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
#define USE_CHECKED_OBJECTREFS
#endif

#define FAT_DISPATCH_TOKENS

#define FEATURE_SHARE_GENERIC_CODE

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
    #define LOGGING
#endif

#if !defined(_TARGET_X86_)
#define WIN64EXCEPTIONS
#endif

#if !defined(FEATURE_UTILCODE_NO_DEPENDENCIES)
// Failpoint support
#if defined(_DEBUG) && !defined(DACCESS_COMPILE) && !defined(FEATURE_PAL)
#define FAILPOINTS_ENABLED
#endif
#endif //!defined(FEATURE_UTILCODE_NO_DEPENDENCIES)

#if 0
    #define APPDOMAIN_STATE
    #define BREAK_ON_UNLOAD
    #define AD_LOG_MEMORY
    #define AD_NO_UNLOAD
    #define AD_SNAPSHOT
    #define BREAK_META_ACCESS
    #define AD_BREAK_ON_CANNOT_UNLOAD
    #define BREAK_ON_CLSLOAD

    // Enable to track details of EESuspension
    #define TIME_SUSPEND
#endif // 0

#ifndef DACCESS_COMPILE
// Enabled to track GC statistics
#define GC_STATS
#endif

#if !defined(FEATURE_CORECLR)
#define EMIT_FIXUPS
#endif

#if defined(_DEBUG) && !defined(DACCESS_COMPILE) && (defined(_TARGET_X86_) || defined(_TARGET_AMD64_))
// On x86/x64 Windows debug builds, respect the COMPLUS_EnforceEEThreadNotRequiredContracts
// runtime switch. See code:InitThreadManager and code:GetThreadGenericFullCheck
#define ENABLE_GET_THREAD_GENERIC_FULL_CHECK
#endif

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
    #define PAGE_SIZE               0x1000
    #define USE_UPPER_ADDRESS       0

#elif defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
    #define PAGE_SIZE               0x1000
    #define USE_UPPER_ADDRESS       1
    #define UPPER_ADDRESS_MAPPING_FACTOR 2
    #define CLR_UPPER_ADDRESS_MIN   0x64400000000
    #define CODEHEAP_START_ADDRESS  0x64480000000
    #define CLR_UPPER_ADDRESS_MAX   0x644FC000000

#else
    #error Please add a new #elif clause and define all portability macros for the new platform
#endif

#ifndef OS_PAGE_SIZE
#define OS_PAGE_SIZE PAGE_SIZE
#endif

#if defined(_WIN64)
#define JIT_IS_ALIGNED
#endif

// ALLOW_SXS_JIT enables AltJit support for JIT-ing, via COMPLUS_AltJit / COMPLUS_AltJitName.
// ALLOW_SXS_JIT_NGEN enables AltJit support for NGEN, via COMPLUS_AltJitNgen / COMPLUS_AltJitName.
// Note that if ALLOW_SXS_JIT_NGEN is defined, then ALLOW_SXS_JIT must be defined.
#define ALLOW_SXS_JIT
#if defined(ALLOW_SXS_JIT)
#define ALLOW_SXS_JIT_NGEN
#endif // ALLOW_SXS_JIT

#if defined(FEATURE_CORECLR)
//master switch for gc suspension not based on hijacking
#define FEATURE_ENABLE_GCPOLL
#endif //FEATURE_CORECLR

#if defined(FEATURE_ENABLE_GCPOLL) && defined(_TARGET_X86_)
//this enables a fast version of the GC Poll helper instead of the default portable one.
#define ENABLE_FAST_GCPOLL_HELPER
#endif // defined(FEATURE_ENABLE_GCPOLL) && defined(_TARGET_X86_)

#if !defined(FEATURE_PAL)
// PLATFORM_SUPPORTS_THREADSUSPEND is defined for platforms where it is safe to call 
//   SuspendThread.  This API is dangerous on non-Windows platforms, as it can lead to 
//   deadlocks, due to low level OS resources that the PAL is not aware of, or due to 
//   the fact that PAL-unaware code in the process may hold onto some OS resources.
#define PLATFORM_SUPPORTS_SAFE_THREADSUSPEND
#endif // !FEATURE_PAL

#if !defined(PLATFORM_SUPPORTS_SAFE_THREADSUSPEND) && !defined(FEATURE_ENABLE_GCPOLL)
#error "Platform must support either safe thread suspension or GC polling"
#endif

// GCCoverage has a dependency on msvcdisXXX.dll, which is not available for CoreSystem. Hence, it is disabled for CoreSystem builds.
#if defined(STRESS_HEAP) && defined(_DEBUG) && defined(FEATURE_HIJACK) && !(defined(FEATURE_CORESYSTEM) && (defined(_TARGET_X86_) || defined(_TARGET_AMD64_)))
#define HAVE_GCCOVER
#endif

#ifdef FEATURE_CORECLR
//Turns on a startup delay to allow simulation of slower and faster startup times.
#define ENABLE_STARTUP_DELAY
#endif


#ifndef ALLOW_LOCAL_WORKER
#define ALLOW_LOCAL_WORKER
#endif


#if defined(_DEBUG) && (defined(_TARGET_X86_) || defined(_WIN64) || defined(_TARGET_ARM_))

//hurray DAC makes everything more fun - you can't have defines that control whether
//or not data members are visible which differ between DAC and non-DAC builds.
//All of the _DATA defines match DAC and non-DAC, the other defines here are off in the DAC.
#if defined(PROFILING_SUPPORTED_DATA) || defined(PROFILING_SUPPORTED)
// See code:ProfControlBlock#TestOnlyELT.
#define PROF_TEST_ONLY_FORCE_ELT_DATA
// See code:ProfControlBlock#TestOnlyObjectAllocated.
#define PROF_TEST_ONLY_FORCE_OBJECT_ALLOCATED_DATA
#endif // PROFILING_SUPPORTED_DATA || PROFILING_SUPPORTED

#if defined(PROFILING_SUPPORTED)
// See code:ProfControlBlock#TestOnlyELT.
#define PROF_TEST_ONLY_FORCE_ELT
// See code:ProfControlBlock#TestOnlyObjectAllocated.
#define PROF_TEST_ONLY_FORCE_OBJECT_ALLOCATED
#endif // PROFILING_SUPPORTED

#endif // defined(_DEBUG) && (defined(_TARGET_X86_) || defined(_WIN64))



#if defined(PROFILING_SUPPORTED)
// On desktop CLR builds, the profiling API uses the event log for end-user-friendly
// diagnostic messages.  CoreCLR on Windows ouputs debug strings for diagnostic messages.
// Rotor builds have no access to event log message resources, though, so they simply 
// display popup dialogs for now.
#define FEATURE_PROFAPI_EVENT_LOGGING
#endif // defined(PROFILING_SUPPORTED)

// Windows desktop supports the profiling API attach / detach feature.
// This will eventually be supported on coreclr as well. 
#if defined(PROFILING_SUPPORTED) && !defined(FEATURE_CORECLR)
#define FEATURE_PROFAPI_ATTACH_DETACH
#endif

// Windows desktop DAC builds need to see some of the data used in the profiling API
// attach / detach feature, particularly Thread::m_dwProfilerEvacuationCounter 
#if defined(PROFILING_SUPPORTED_DATA) && !defined(FEATURE_CORECLR)
#define DATA_PROFAPI_ATTACH_DETACH
#endif

// MUST NEVER CHECK IN WITH THIS ENABLED.
// This is just for convenience in doing performance investigations in a checked-out enlistment.
// #define FEATURE_ENABLE_NO_RANGE_CHECKS

#ifndef FEATURE_CORECLR
// This controls whether a compilation-timing feature that relies on Windows APIs, if available, else direct
// hardware instructions (rdtsc), for accessing high-resolution hardware timers is enabled. This is disabled
// in Silverlight (just to avoid thinking about whether the extra code space is worthwhile).
#define FEATURE_JIT_TIMER

// This feature in RyuJIT supersedes the FEATURE_JIT_TIMER. In addition to supporting the time log file, this
// feature also supports using COMPLUS_JitTimeLogCsv=a.csv, which will dump method-level and phase-level timing
// statistics. Also see comments on FEATURE_JIT_TIMER.
#define FEATURE_JIT_METHOD_PERF
#endif // FEATURE_CORECLR


#ifndef FEATURE_USE_ASM_GC_WRITE_BARRIERS
// If we're not using assembly write barriers, then this turns on a performance measurement
// mode that gathers and prints statistics about # of GC write barriers invokes.
// #define FEATURE_COUNT_GC_WRITE_BARRIERS
#endif

// Enables a mode in which GC is completely conservative in stacks and registers: all stack slots and registers
// are treated as potential pinned interior pointers. When enabled, the runtime flag COMPLUS_GCCONSERVATIVE 
// determines dynamically whether GC is conservative. Note that appdomain unload, LCG and unloadable assemblies
// do not work reliably with conservative GC.
#if defined(FEATURE_CORECLR) && !defined(BINDER)
#define FEATURE_CONSERVATIVE_GC 1
#endif

#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
#define FEATURE_HFA
#endif

// ARM requires that 64-bit primitive types are aligned at 64-bit boundaries for interlocked-like operations.
// Additionally the platform ABI requires these types and composite type containing them to be similarly
// aligned when passed as arguments.
#ifdef _TARGET_ARM_
#define FEATURE_64BIT_ALIGNMENT
#endif

// Prefer double alignment for structs and arrays with doubles. Put arrays of doubles more agressively 
// into large object heap for performance because large object heap is 8 byte aligned 
#if !defined(FEATURE_64BIT_ALIGNMENT) && !defined(_WIN64)
#define FEATURE_DOUBLE_ALIGNMENT_HINT
#endif

#if defined(FEATURE_CORESYSTEM)
#define FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
#endif // defined(FEATURE_CORESYSTEM)

#if defined(FEATURE_PREJIT) && defined(FEATURE_CORECLR) && defined(FEATURE_CORESYSTEM)
// Desktop CLR allows profilers and debuggers to opt out of loading NGENd images, and to
// JIT everything instead. "FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS" is roughly the
// equivalent for Apollo, where MSIL images may not be available at all.
// FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS allows profilers or debuggers to state
// they don't want to use pregenerated code, and to instead load the NGENd image but
// treat it as if it were MSIL by ignoring the prejitted code and prebaked structures,
// and instead to JIT and load types at run-time.
#define FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS
#endif

// If defined, support interpretation.
#if !defined(CROSSGEN_COMPILE)

#if defined(ALLOW_SXS_JIT) && !defined(FEATURE_PAL)
#define FEATURE_STACK_SAMPLING
#endif // defined (ALLOW_SXS_JIT)

#if defined(_TARGET_ARM64_)
#define FEATURE_INTERPRETER
#endif // defined(_TARGET_ARM64_)

#endif // !defined(CROSSGEN_COMPILE)

