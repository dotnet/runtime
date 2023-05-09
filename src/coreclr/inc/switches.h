// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// switches.h switch configuration of common runtime features
//


#define STRESS_HEAP


#define VERIFY_HEAP

#define GC_CONFIG_DRIVEN

// define this to test data safety for the DAC. See code:DataTest::TestDataSafety.
#define TEST_DATA_CONSISTENCY

#if !defined(STRESS_LOG) && !defined(FEATURE_UTILCODE_NO_DEPENDENCIES)
#define STRESS_LOG
#endif

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
#define USE_CHECKED_OBJECTREFS
#endif

#ifndef TARGET_64BIT
#define FAT_DISPATCH_TOKENS
#endif

#define FEATURE_SHARE_GENERIC_CODE

#if defined(_DEBUG)
    #define LOGGING
#endif

#if !defined(FEATURE_UTILCODE_NO_DEPENDENCIES)
// Failpoint support
#if defined(_DEBUG) && !defined(DACCESS_COMPILE) && !defined(TARGET_UNIX)
#define FAILPOINTS_ENABLED
#endif
#endif //!defined(FEATURE_UTILCODE_NO_DEPENDENCIES)

#if 0
    // Enable to track details of EESuspension
    #define TIME_SUSPEND
#endif // 0

#ifndef DACCESS_COMPILE
// Enabled to track GC statistics
#define GC_STATS
#endif

#if defined(TARGET_X86) || defined(TARGET_ARM)
    #define USE_LAZY_PREFERRED_RANGE       0

#elif defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_S390X) || defined(TARGET_LOONGARCH64) || defined(TARGET_POWERPC64) || defined(TARGET_RISCV64)

#if defined(HOST_UNIX)
    // In PAL we have a smechanism that reserves memory on start up that is
    // close to libcoreclr and intercepts calls to VirtualAlloc to serve back
    // from this area.
    #define USE_LAZY_PREFERRED_RANGE       0
#else
    // On Windows we lazily try to reserve memory close to coreclr.dll.
    #define USE_LAZY_PREFERRED_RANGE       1
#endif

#else
    #error Please add a new #elif clause and define all portability macros for the new platform
#endif

#if defined(HOST_64BIT)
#define JIT_IS_ALIGNED
#endif

// ALLOW_SXS_JIT enables AltJit support for JIT-ing, via DOTNET_AltJit / DOTNET_AltJitName.
// ALLOW_SXS_JIT_NGEN enables AltJit support for NGEN, via DOTNET_AltJitNgen / DOTNET_AltJitName.
// Note that if ALLOW_SXS_JIT_NGEN is defined, then ALLOW_SXS_JIT must be defined.
#define ALLOW_SXS_JIT
#define ALLOW_SXS_JIT_NGEN

#if !defined(TARGET_UNIX)
// PLATFORM_SUPPORTS_THREADSUSPEND is defined for platforms where it is safe to call
//   SuspendThread.  This API is dangerous on non-Windows platforms, as it can lead to
//   deadlocks, due to low level OS resources that the PAL is not aware of, or due to
//   the fact that PAL-unaware code in the process may hold onto some OS resources.
#define PLATFORM_SUPPORTS_SAFE_THREADSUSPEND
#endif // !TARGET_UNIX


#if defined(STRESS_HEAP) && defined(_DEBUG) && defined(FEATURE_HIJACK)
#define HAVE_GCCOVER
#endif

// Some platforms may see spurious AVs when GcCoverage is enabled because of races.
// Enable further processing to see if they recur.
#if defined(HAVE_GCCOVER) && (defined(TARGET_X86) || defined(TARGET_AMD64)) && !defined(TARGET_UNIX)
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
// feature also supports using DOTNET_JitTimeLogCsv=a.csv, which will dump method-level and phase-level timing
// statistics. Also see comments on FEATURE_JIT_TIMER.
#define FEATURE_JIT_METHOD_PERF


#ifndef FEATURE_USE_ASM_GC_WRITE_BARRIERS
// If we're not using assembly write barriers, then this turns on a performance measurement
// mode that gathers and prints statistics about # of GC write barriers invokes.
// #define FEATURE_COUNT_GC_WRITE_BARRIERS
#endif

// Enables a mode in which GC is completely conservative in stacks and registers: all stack slots and registers
// are treated as potential pinned interior pointers. When enabled, the runtime flag DOTNET_GCCONSERVATIVE
// determines dynamically whether GC is conservative. Note that appdomain unload, LCG and unloadable assemblies
// do not work reliably with conservative GC.
#define FEATURE_CONSERVATIVE_GC 1

#if (defined(TARGET_ARM) && (!defined(ARM_SOFTFP) || defined(CONFIGURABLE_ARM_ABI))) || defined(TARGET_ARM64)
#define FEATURE_HFA
#endif

// ARM requires that 64-bit primitive types are aligned at 64-bit boundaries for interlocked-like operations.
// Additionally the platform ABI requires these types and composite type containing them to be similarly
// aligned when passed as arguments.
#ifdef TARGET_ARM
#define FEATURE_64BIT_ALIGNMENT
#endif

// Prefer double alignment for structs and arrays with doubles. Put arrays of doubles more agressively
// into large object heap for performance because large object heap is 8 byte aligned
#if !defined(FEATURE_64BIT_ALIGNMENT) && !defined(HOST_64BIT)
#define FEATURE_DOUBLE_ALIGNMENT_HINT
#endif

#define FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

// If defined, support interpretation.

#if !defined(TARGET_UNIX)
#define FEATURE_STACK_SAMPLING
#endif // defined (ALLOW_SXS_JIT)
