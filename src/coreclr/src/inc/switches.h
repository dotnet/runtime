// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// switches.h switch configuration of common runtime features
//


#ifndef CROSSGEN_COMPILE
#define STRESS_HEAP
#endif


#define VERIFY_HEAP

#define GC_CONFIG_DRIVEN

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

#if !defined(_TARGET_X86_) || defined(FEATURE_PAL)
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

    // Enable to track details of EESuspension
    #define TIME_SUSPEND
#endif // 0

#ifndef DACCESS_COMPILE
// Enabled to track GC statistics
#define GC_STATS
#endif

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
    #define USE_UPPER_ADDRESS       0

#elif defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
    #define UPPER_ADDRESS_MAPPING_FACTOR 2
    #define CLR_UPPER_ADDRESS_MIN   0x64400000000
    #define CODEHEAP_START_ADDRESS  0x64480000000
    #define CLR_UPPER_ADDRESS_MAX   0x644FC000000

#if !defined(FEATURE_PAL)
    #define USE_UPPER_ADDRESS       1
#else
    #define USE_UPPER_ADDRESS       0
#endif // !FEATURE_PAL

#else
    #error Please add a new #elif clause and define all portability macros for the new platform
#endif

#if defined(_WIN64)
#define JIT_IS_ALIGNED
#endif

// ALLOW_SXS_JIT enables AltJit support for JIT-ing, via COMPlus_AltJit / COMPlus_AltJitName.
// ALLOW_SXS_JIT_NGEN enables AltJit support for NGEN, via COMPlus_AltJitNgen / COMPlus_AltJitName.
// Note that if ALLOW_SXS_JIT_NGEN is defined, then ALLOW_SXS_JIT must be defined.
#define ALLOW_SXS_JIT
#define ALLOW_SXS_JIT_NGEN

//master switch for gc suspension not based on hijacking
#define FEATURE_ENABLE_GCPOLL

#if defined(_TARGET_X86_)
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


#if defined(STRESS_HEAP) && defined(_DEBUG) && defined(FEATURE_HIJACK)
#define HAVE_GCCOVER
#endif

// Some platforms may see spurious AVs when GcCoverage is enabled because of races.
// Enable further processing to see if they recur.
#if defined(HAVE_GCCOVER) && (defined(_TARGET_X86_) || defined(_TARGET_AMD64_)) && !defined(FEATURE_PAL)
#define GCCOVER_TOLERATE_SPURIOUS_AV
#endif

//Turns on a startup delay to allow simulation of slower and faster startup times.
#define ENABLE_STARTUP_DELAY


#ifdef _DEBUG

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

#endif // _DEBUG

// MUST NEVER CHECK IN WITH THIS ENABLED.
// This is just for convenience in doing performance investigations in a checked-out enlistment.
// #define FEATURE_ENABLE_NO_RANGE_CHECKS

// This controls whether a compilation-timing feature that relies on Windows APIs, if available, else direct
// hardware instructions (rdtsc), for accessing high-resolution hardware timers is enabled. This is disabled
// in Silverlight (just to avoid thinking about whether the extra code space is worthwhile).
#define FEATURE_JIT_TIMER

// This feature in RyuJIT supersedes the FEATURE_JIT_TIMER. In addition to supporting the time log file, this
// feature also supports using COMPlus_JitTimeLogCsv=a.csv, which will dump method-level and phase-level timing
// statistics. Also see comments on FEATURE_JIT_TIMER.
#define FEATURE_JIT_METHOD_PERF


#ifndef FEATURE_USE_ASM_GC_WRITE_BARRIERS
// If we're not using assembly write barriers, then this turns on a performance measurement
// mode that gathers and prints statistics about # of GC write barriers invokes.
// #define FEATURE_COUNT_GC_WRITE_BARRIERS
#endif

// Enables a mode in which GC is completely conservative in stacks and registers: all stack slots and registers
// are treated as potential pinned interior pointers. When enabled, the runtime flag COMPLUS_GCCONSERVATIVE 
// determines dynamically whether GC is conservative. Note that appdomain unload, LCG and unloadable assemblies
// do not work reliably with conservative GC.
#define FEATURE_CONSERVATIVE_GC 1

#if (defined(_TARGET_ARM_) && !defined(ARM_SOFTFP)) || defined(_TARGET_ARM64_)
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

// If defined, support interpretation.
#if !defined(CROSSGEN_COMPILE)

#if !defined(FEATURE_PAL)
#define FEATURE_STACK_SAMPLING
#endif // defined (ALLOW_SXS_JIT)

#endif // !defined(CROSSGEN_COMPILE)

#if defined(FEATURE_INTERPRETER) && defined(CROSSGEN_COMPILE)
#undef FEATURE_INTERPRETER
#endif
