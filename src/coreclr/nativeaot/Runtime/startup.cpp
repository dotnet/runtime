// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
#include "slist.h"
#include "gcrhinterface.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "holder.h"
#include "Crst.h"
#include "event.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "RuntimeInstance.h"
#include "rhbinder.h"
#include "CachedInterfaceDispatch.h"
#include "RhConfig.h"
#include "stressLog.h"
#include "RestrictedCallouts.h"
#include "yieldprocessornormalized.h"

#ifdef FEATURE_PERFTRACING
#include "EventPipeInterface.h"
#endif

#ifndef DACCESS_COMPILE

#ifdef PROFILE_STARTUP
unsigned __int64 g_startupTimelineEvents[NUM_STARTUP_TIMELINE_EVENTS] = { 0 };
#endif // PROFILE_STARTUP

#ifdef TARGET_UNIX
int32_t RhpHardwareExceptionHandler(uintptr_t faultCode, uintptr_t faultAddress, PAL_LIMITED_CONTEXT* palContext, uintptr_t* arg0Reg, uintptr_t* arg1Reg);
#else
int32_t __stdcall RhpVectoredExceptionHandler(PEXCEPTION_POINTERS pExPtrs);
#endif

extern "C" void PopulateDebugHeaders();

static bool DetectCPUFeatures();

extern RhConfig * g_pRhConfig;

EXTERN_C bool g_fHasFastFxsave;
bool g_fHasFastFxsave = false;

CrstStatic g_ThunkPoolLock;

#if defined(HOST_X86) || defined(HOST_AMD64) || defined(HOST_ARM64)
// This field is inspected from the generated code to determine what intrinsics are available.
EXTERN_C int g_cpuFeatures;
int g_cpuFeatures = 0;

// This field is defined in the generated code and sets the ISA expectations.
EXTERN_C int g_requiredCpuFeatures;
#endif

#ifdef TARGET_UNIX
static bool InitGSCookie();

//-----------------------------------------------------------------------------
// GSCookies (guard-stack cookies) for detecting buffer overruns
//-----------------------------------------------------------------------------
typedef size_t GSCookie;

#ifdef FEATURE_READONLY_GS_COOKIE

#ifdef __APPLE__
#define READONLY_ATTR_ARGS section("__DATA,__const")
#else
#define READONLY_ATTR_ARGS section(".rodata")
#endif
#define READONLY_ATTR __attribute__((READONLY_ATTR_ARGS))

// const is so that it gets placed in the .text section (which is read-only)
// volatile is so that accesses to it do not get optimized away because of the const
//

extern "C" volatile READONLY_ATTR const GSCookie __security_cookie = 0;
#else
extern "C" volatile GSCookie __security_cookie = 0;
#endif // FEATURE_READONLY_GS_COOKIE

#endif // TARGET_UNIX

static bool InitDLL(HANDLE hPalInstance)
{
#ifdef FEATURE_CACHED_INTERFACE_DISPATCH
    //
    // Initialize interface dispatch.
    //
    if (!InitializeInterfaceDispatch())
        return false;
#endif

#ifdef FEATURE_PERFTRACING
    // Initialize EventPipe
    EventPipeAdapter_Initialize();
    // Initialize DS
    DiagnosticServerAdapter_Initialize();
    DiagnosticServerAdapter_PauseForDiagnosticsMonitor();
#endif

    //
    // Initialize support for registering GC and HandleTable callouts.
    //
    if (!RestrictedCallouts::Initialize())
        return false;

    //
    // Initialize RuntimeInstance state
    //
    if (!RuntimeInstance::Initialize(hPalInstance))
        return false;

    // Note: The global exception handler uses RuntimeInstance
#if !defined(USE_PORTABLE_HELPERS)
#ifndef TARGET_UNIX
    PalAddVectoredExceptionHandler(1, RhpVectoredExceptionHandler);
#else
    PalSetHardwareExceptionHandler(RhpHardwareExceptionHandler);
#endif
#endif // !USE_PORTABLE_HELPERS

    InitializeYieldProcessorNormalizedCrst();

#ifdef STRESS_LOG
    uint32_t dwTotalStressLogSize = (uint32_t)g_pRhConfig->GetTotalStressLogSize();
    uint32_t dwStressLogLevel = (uint32_t)g_pRhConfig->GetStressLogLevel();

    unsigned facility = (unsigned)LF_ALL;
    unsigned dwPerThreadChunks = (dwTotalStressLogSize / 24) / STRESSLOG_CHUNK_SIZE;
    if (dwTotalStressLogSize != 0)
    {
        StressLog::Initialize(facility, dwStressLogLevel,
                              dwPerThreadChunks * STRESSLOG_CHUNK_SIZE,
                              (unsigned)dwTotalStressLogSize, hPalInstance);
    }
#endif // STRESS_LOG

    STARTUP_TIMELINE_EVENT(NONGC_INIT_COMPLETE);

    if (!RedhawkGCInterface::InitializeSubsystems())
        return false;

    STARTUP_TIMELINE_EVENT(GC_INIT_COMPLETE);

#ifdef FEATURE_PERFTRACING
    // Finish setting up rest of EventPipe - specifically enable SampleProfiler if it was requested at startup.
    // SampleProfiler needs to cooperate with the GC which hasn't fully finished setting up in the first part of the
    // EventPipe initialization, so this is done after the GC has been fully initialized.
    EventPipeAdapter_FinishInitialize();
#endif

#ifndef USE_PORTABLE_HELPERS
    if (!DetectCPUFeatures())
        return false;
#endif

#ifdef TARGET_UNIX
    if (!InitGSCookie())
        return false;
#endif

    if (!g_ThunkPoolLock.InitNoThrow(CrstType::CrstThunkPool))
        return false;

    return true;
}

#ifndef USE_PORTABLE_HELPERS

bool DetectCPUFeatures()
{
#if defined(HOST_X86) || defined(HOST_AMD64) || defined(HOST_ARM64)

#if defined(HOST_X86) || defined(HOST_AMD64)

    int cpuidInfo[4];

    const int CPUID_EAX = 0;
    const int CPUID_EBX = 1;
    const int CPUID_ECX = 2;
    const int CPUID_EDX = 3;

    __cpuid(cpuidInfo, 0x00000000);
    uint32_t maxCpuId = static_cast<uint32_t>(cpuidInfo[CPUID_EAX]);

    if (maxCpuId >= 1)
    {
        __cpuid(cpuidInfo, 0x00000001);

        if (((cpuidInfo[CPUID_EDX] & (1 << 25)) != 0) && ((cpuidInfo[CPUID_EDX] & (1 << 26)) != 0))                     // SSE & SSE2
        {
            if ((cpuidInfo[CPUID_ECX] & (1 << 25)) != 0)                                                          // AESNI
            {
                g_cpuFeatures |= XArchIntrinsicConstants_Aes;
            }

            if ((cpuidInfo[CPUID_ECX] & (1 << 1)) != 0)                                                           // PCLMULQDQ
            {
                g_cpuFeatures |= XArchIntrinsicConstants_Pclmulqdq;
            }

            if ((cpuidInfo[CPUID_ECX] & (1 << 0)) != 0)                                                           // SSE3
            {
                g_cpuFeatures |= XArchIntrinsicConstants_Sse3;

                if ((cpuidInfo[CPUID_ECX] & (1 << 9)) != 0)                                                       // SSSE3
                {
                    g_cpuFeatures |= XArchIntrinsicConstants_Ssse3;

                    if ((cpuidInfo[CPUID_ECX] & (1 << 19)) != 0)                                                  // SSE4.1
                    {
                        g_cpuFeatures |= XArchIntrinsicConstants_Sse41;

                        if ((cpuidInfo[CPUID_ECX] & (1 << 20)) != 0)                                              // SSE4.2
                        {
                            g_cpuFeatures |= XArchIntrinsicConstants_Sse42;

                            if ((cpuidInfo[CPUID_ECX] & (1 << 22)) != 0)                                          // MOVBE
                            {
                                g_cpuFeatures |= XArchIntrinsicConstants_Movbe;
                            }

                            if ((cpuidInfo[CPUID_ECX] & (1 << 23)) != 0)                                          // POPCNT
                            {
                                g_cpuFeatures |= XArchIntrinsicConstants_Popcnt;
                            }

                            if (((cpuidInfo[CPUID_ECX] & (1 << 27)) != 0) && ((cpuidInfo[CPUID_ECX] & (1 << 28)) != 0)) // OSXSAVE & AVX
                            {
                                if (PalIsAvxEnabled() && (xmmYmmStateSupport() == 1))
                                {
                                    g_cpuFeatures |= XArchIntrinsicConstants_Avx;

                                    if ((cpuidInfo[CPUID_ECX] & (1 << 12)) != 0)                                  // FMA
                                    {
                                        g_cpuFeatures |= XArchIntrinsicConstants_Fma;
                                    }

                                    if (maxCpuId >= 0x07)
                                    {
                                        __cpuidex(cpuidInfo, 0x00000007, 0x00000000);

                                        if ((cpuidInfo[CPUID_EBX] & (1 << 5)) != 0)                               // AVX2
                                        {
                                            g_cpuFeatures |= XArchIntrinsicConstants_Avx2;

                                            __cpuidex(cpuidInfo, 0x00000007, 0x00000001);
                                            if ((cpuidInfo[CPUID_EAX] & (1 << 4)) != 0)                           // AVX-VNNI
                                            {
                                                g_cpuFeatures |= XArchIntrinsicConstants_AvxVnni;
                                            }

                                            if (PalIsAvx512Enabled() && (avx512StateSupport() == 1))       // XGETBV XRC0[7:5] == 111
                                            {
                                                if ((cpuidInfo[CPUID_EBX] & (1 << 16)) != 0)                     // AVX512F
                                                {
                                                    g_cpuFeatures |= XArchIntrinsicConstants_Avx512f;

                                                    bool isAVX512_VLSupported = false;
                                                    if ((cpuidInfo[CPUID_EBX] & (1 << 31)) != 0)                 // AVX512VL
                                                    {
                                                        g_cpuFeatures |= XArchIntrinsicConstants_Avx512f_vl;
                                                        isAVX512_VLSupported = true;
                                                    }

                                                    if ((cpuidInfo[CPUID_EBX] & (1 << 30)) != 0)                 // AVX512BW
                                                    {
                                                        g_cpuFeatures |= XArchIntrinsicConstants_Avx512bw;
                                                        if (isAVX512_VLSupported)
                                                        {
                                                            g_cpuFeatures |= XArchIntrinsicConstants_Avx512bw_vl;
                                                        }
                                                    }

                                                    if ((cpuidInfo[CPUID_EBX] & (1 << 28)) != 0)                 // AVX512CD
                                                    {
                                                        g_cpuFeatures |= XArchIntrinsicConstants_Avx512cd;
                                                        if (isAVX512_VLSupported)
                                                        {
                                                            g_cpuFeatures |= XArchIntrinsicConstants_Avx512cd_vl;
                                                        }
                                                    }

                                                    if ((cpuidInfo[CPUID_EBX] & (1 << 17)) != 0)                 // AVX512DQ
                                                    {
                                                        g_cpuFeatures |= XArchIntrinsicConstants_Avx512dq;
                                                        if (isAVX512_VLSupported)
                                                        {
                                                            g_cpuFeatures |= XArchIntrinsicConstants_Avx512dq_vl;
                                                        }
                                                    }

                                                    if ((cpuidInfo[CPUID_ECX] & (1 << 1)) != 0)                  // AVX512VBMI
                                                    {
                                                        g_cpuFeatures |= XArchIntrinsicConstants_Avx512Vbmi;
                                                        if (isAVX512_VLSupported)
                                                        {
                                                            g_cpuFeatures |= XArchIntrinsicConstants_Avx512Vbmi_vl;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (maxCpuId >= 0x07)
        {
            __cpuidex(cpuidInfo, 0x00000007, 0x00000000);

            if ((cpuidInfo[CPUID_EBX] & (1 << 3)) != 0)                                                           // BMI1
            {
                g_cpuFeatures |= XArchIntrinsicConstants_Bmi1;
            }

            if ((cpuidInfo[CPUID_EBX] & (1 << 8)) != 0)                                                           // BMI2
            {
                g_cpuFeatures |= XArchIntrinsicConstants_Bmi2;
            }
        }
    }

    __cpuid(cpuidInfo, 0x80000000);
    uint32_t maxCpuIdEx = static_cast<uint32_t>(cpuidInfo[CPUID_EAX]);

    if (maxCpuIdEx >= 0x80000001)
    {
        __cpuid(cpuidInfo, 0x80000001);

        if ((cpuidInfo[CPUID_ECX] & (1 << 5)) != 0)                                                               // LZCNT
        {
            g_cpuFeatures |= XArchIntrinsicConstants_Lzcnt;
        }

#ifdef HOST_AMD64
        // AMD has a "fast" mode for fxsave/fxrstor, which omits the saving of xmm registers.  The OS will enable this mode
        // if it is supported.  So if we continue to use fxsave/fxrstor, we must manually save/restore the xmm registers.
        // fxsr_opt is bit 25 of CPUID_EDX
        if ((cpuidInfo[CPUID_EDX] & (1 << 25)) != 0)
            g_fHasFastFxsave = true;
#endif
    }
#endif // HOST_X86 || HOST_AMD64

#if defined(HOST_ARM64)
    PAL_GetCpuCapabilityFlags (&g_cpuFeatures);
#endif

    if ((g_cpuFeatures & g_requiredCpuFeatures) != g_requiredCpuFeatures)
    {
        PalPrintFatalError("\nThe required instruction sets are not supported by the current CPU.\n");
        RhFailFast();
    }
#endif // HOST_X86|| HOST_AMD64 || HOST_ARM64

    return true;
}
#endif // !USE_PORTABLE_HELPERS

#ifdef TARGET_UNIX
inline
GSCookie * GetProcessGSCookiePtr() { return  const_cast<GSCookie *>(&__security_cookie); }

bool InitGSCookie()
{
    volatile GSCookie * pGSCookiePtr = GetProcessGSCookiePtr();

#ifdef FEATURE_READONLY_GS_COOKIE
    // The GS cookie is stored in a read only data segment
    if (!PalVirtualProtect((void*)pGSCookiePtr, sizeof(GSCookie), PAGE_READWRITE))
    {
        return false;
    }
#endif

    // REVIEW: Need something better for PAL...
    GSCookie val = (GSCookie)PalGetTickCount64();

#ifdef _DEBUG
    // In _DEBUG, always use the same value to make it easier to search for the cookie
    val = (GSCookie)(0x9ABCDEF012345678);
#endif

    *pGSCookiePtr = val;

#ifdef FEATURE_READONLY_GS_COOKIE
    return PalVirtualProtect((void*)pGSCookiePtr, sizeof(GSCookie), PAGE_READONLY);
#else
    return true;
#endif
}
#endif // TARGET_UNIX

#ifdef PROFILE_STARTUP
#define STD_OUTPUT_HANDLE ((uint32_t)-11)

struct RegisterModuleTrace
{
    LARGE_INTEGER Begin;
    LARGE_INTEGER End;
};

const int NUM_REGISTER_MODULE_TRACES = 16;
int g_registerModuleCount = 0;

RegisterModuleTrace g_registerModuleTraces[NUM_REGISTER_MODULE_TRACES] = { 0 };

static void AppendInt64(char * pBuffer, uint32_t* pLen, uint64_t value)
{
    char localBuffer[20];
    int cch = 0;

    do
    {
        localBuffer[cch++] = '0' + (value % 10);
        value = value / 10;
    } while (value);

    for (int i = 0; i < cch; i++)
    {
        pBuffer[(*pLen)++] = localBuffer[cch - i - 1];
    }

    pBuffer[(*pLen)++] = ',';
    pBuffer[(*pLen)++] = ' ';
}
#endif // PROFILE_STARTUP

static void UninitDLL()
{
#ifdef PROFILE_STARTUP
    char buffer[1024];

    uint32_t len = 0;

    AppendInt64(buffer, &len, g_startupTimelineEvents[PROCESS_ATTACH_BEGIN]);
    AppendInt64(buffer, &len, g_startupTimelineEvents[NONGC_INIT_COMPLETE]);
    AppendInt64(buffer, &len, g_startupTimelineEvents[GC_INIT_COMPLETE]);
    AppendInt64(buffer, &len, g_startupTimelineEvents[PROCESS_ATTACH_COMPLETE]);

    for (int i = 0; i < g_registerModuleCount; i++)
    {
        AppendInt64(buffer, &len, g_registerModuleTraces[i].Begin.QuadPart);
        AppendInt64(buffer, &len, g_registerModuleTraces[i].End.QuadPart);
    }

    buffer[len++] = '\n';

    fwrite(buffer, len, 1, stdout);
#endif // PROFILE_STARTUP
}

#ifdef _WIN32
// This is set to the thread that initiates and performs the shutdown and may run
// after other threads are rudely terminated. So far this is a Windows-specific concern.
//
// On POSIX OSes a process typically lives as long as any of its threads are alive or until
// the process is terminated via `exit()` or a signal. Thus there is no such distinction
// between threads.
Thread* g_threadPerformingShutdown = NULL;
#endif

static void __cdecl OnProcessExit()
{
#ifdef _WIN32
    // The process is exiting and the current thread is performing the shutdown.
    // When this thread exits some threads may be already rudely terminated.
    // It would not be a good idea for this thread to wait on any locks
    // or run managed code at shutdown, so we will not try detaching it.
    Thread* currentThread = ThreadStore::RawGetCurrentThread();
    g_threadPerformingShutdown = currentThread;
#endif

#ifdef FEATURE_PERFTRACING
    EventPipeAdapter_Shutdown();
    DiagnosticServerAdapter_Shutdown();
#endif
}

void RuntimeThreadShutdown(void* thread)
{
    // Note: loader lock is normally *not* held here!
    // The one exception is that the loader lock may be held during the thread shutdown callback
    // that is made for the single thread that runs the final stages of orderly process
    // shutdown (i.e., the thread that delivers the DLL_PROCESS_DETACH notifications when the
    // process is being torn down via an ExitProcess call).
    // In such case we do not detach.

#ifdef _WIN32
    ASSERT((Thread*)thread == ThreadStore::GetCurrentThread());

    // Do not try detaching the thread that performs the shutdown.
    if (g_threadPerformingShutdown == thread)
    {
        // At this point other threads could be terminated rudely while leaving runtime
        // in inconsistent state, so we would be risking blocking the process from exiting.
        return;
    }
#else
    // Some Linux toolset versions call thread-local destructors during shutdown on a wrong thread.
    if ((Thread*)thread != ThreadStore::GetCurrentThread())
    {
        return;
    }
#endif

    ThreadStore::DetachCurrentThread();
}

extern "C" bool RhInitialize()
{
    if (!PalInit())
        return false;

#if defined(_WIN32) || defined(FEATURE_PERFTRACING)
    atexit(&OnProcessExit);
#endif

    if (!InitDLL(PalGetModuleHandleFromPointer((void*)&RhInitialize)))
        return false;

    // Populate the values needed for debugging
    PopulateDebugHeaders();

    return true;
}

#endif // !DACCESS_COMPILE
