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
#include "RWLock.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "RuntimeInstance.h"
#include "rhbinder.h"
#include "CachedInterfaceDispatch.h"
#include "RhConfig.h"
#include "stressLog.h"
#include "RestrictedCallouts.h"
#include "yieldprocessornormalized.h"

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

CrstStatic g_CastCacheLock;
CrstStatic g_ThunkPoolLock;

#if defined(HOST_X86) || defined(HOST_AMD64) || defined(HOST_ARM64)
// This field is inspected from the generated code to determine what intrinsics are available.
EXTERN_C int g_cpuFeatures;
int g_cpuFeatures = 0;

// This field is defined in the generated code and sets the ISA expectations.
EXTERN_C int g_requiredCpuFeatures;
#endif

static bool InitDLL(HANDLE hPalInstance)
{
#ifdef FEATURE_CACHED_INTERFACE_DISPATCH
    //
    // Initialize interface dispatch.
    //
    if (!InitializeInterfaceDispatch())
        return false;
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

    STARTUP_TIMELINE_EVENT(NONGC_INIT_COMPLETE);

    if (!RedhawkGCInterface::InitializeSubsystems())
        return false;

    STARTUP_TIMELINE_EVENT(GC_INIT_COMPLETE);

#ifdef STRESS_LOG
    uint32_t dwTotalStressLogSize = g_pRhConfig->GetTotalStressLogSize();
    uint32_t dwStressLogLevel = g_pRhConfig->GetStressLogLevel();

    unsigned facility = (unsigned)LF_ALL;
    unsigned dwPerThreadChunks = (dwTotalStressLogSize / 24) / STRESSLOG_CHUNK_SIZE;
    if (dwTotalStressLogSize != 0)
    {
        StressLog::Initialize(facility, dwStressLogLevel,
                              dwPerThreadChunks * STRESSLOG_CHUNK_SIZE,
                              (unsigned)dwTotalStressLogSize, hPalInstance);
    }
#endif // STRESS_LOG

#ifndef USE_PORTABLE_HELPERS
    if (!DetectCPUFeatures())
        return false;
#endif

    if (!g_CastCacheLock.InitNoThrow(CrstType::CrstCastCache))
        return false;

    if (!g_ThunkPoolLock.InitNoThrow(CrstType::CrstCastCache))
        return false;

    return true;
}

#ifndef USE_PORTABLE_HELPERS

bool DetectCPUFeatures()
{
#if defined(HOST_X86) || defined(HOST_AMD64) || defined(HOST_ARM64)

#if defined(HOST_X86) || defined(HOST_AMD64)

    int cpuidInfo[4];

    const int EAX = 0;
    const int EBX = 1;
    const int ECX = 2;
    const int EDX = 3;

    __cpuid(cpuidInfo, 0x00000000);
    uint32_t maxCpuId = static_cast<uint32_t>(cpuidInfo[EAX]);

    if (maxCpuId >= 1)
    {
        __cpuid(cpuidInfo, 0x00000001);

        if (((cpuidInfo[EDX] & (1 << 25)) != 0) && ((cpuidInfo[EDX] & (1 << 26)) != 0))                     // SSE & SSE2
        {
            if ((cpuidInfo[ECX] & (1 << 25)) != 0)                                                          // AESNI
            {
                g_cpuFeatures |= XArchIntrinsicConstants_Aes;
            }

            if ((cpuidInfo[ECX] & (1 << 1)) != 0)                                                           // PCLMULQDQ
            {
                g_cpuFeatures |= XArchIntrinsicConstants_Pclmulqdq;
            }

            if ((cpuidInfo[ECX] & (1 << 0)) != 0)                                                           // SSE3
            {
                g_cpuFeatures |= XArchIntrinsicConstants_Sse3;

                if ((cpuidInfo[ECX] & (1 << 9)) != 0)                                                       // SSSE3
                {
                    g_cpuFeatures |= XArchIntrinsicConstants_Ssse3;

                    if ((cpuidInfo[ECX] & (1 << 19)) != 0)                                                  // SSE4.1
                    {
                        g_cpuFeatures |= XArchIntrinsicConstants_Sse41;

                        if ((cpuidInfo[ECX] & (1 << 20)) != 0)                                              // SSE4.2
                        {
                            g_cpuFeatures |= XArchIntrinsicConstants_Sse42;

                            if ((cpuidInfo[ECX] & (1 << 23)) != 0)                                          // POPCNT
                            {
                                g_cpuFeatures |= XArchIntrinsicConstants_Popcnt;
                            }

                            if (((cpuidInfo[ECX] & (1 << 27)) != 0) && ((cpuidInfo[ECX] & (1 << 28)) != 0)) // OSXSAVE & AVX
                            {
                                if (PalIsAvxEnabled() && (xmmYmmStateSupport() == 1))
                                {
                                    g_cpuFeatures |= XArchIntrinsicConstants_Avx;

                                    if ((cpuidInfo[ECX] & (1 << 12)) != 0)                                  // FMA
                                    {
                                        g_cpuFeatures |= XArchIntrinsicConstants_Fma;
                                    }

                                    if (maxCpuId >= 0x07)
                                    {
                                        __cpuidex(cpuidInfo, 0x00000007, 0x00000000);

                                        if ((cpuidInfo[EBX] & (1 << 5)) != 0)                               // AVX2
                                        {
                                            g_cpuFeatures |= XArchIntrinsicConstants_Avx2;

                                            __cpuidex(cpuidInfo, 0x00000007, 0x00000001);
                                            if ((cpuidInfo[EAX] & (1 << 4)) != 0)                           // AVX-VNNI
                                            {
                                                g_cpuFeatures |= XArchIntrinsicConstants_AvxVnni;
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

            if ((cpuidInfo[EBX] & (1 << 3)) != 0)                                                           // BMI1
            {
                g_cpuFeatures |= XArchIntrinsicConstants_Bmi1;
            }

            if ((cpuidInfo[EBX] & (1 << 8)) != 0)                                                           // BMI2
            {
                g_cpuFeatures |= XArchIntrinsicConstants_Bmi2;
            }
        }
    }

    __cpuid(cpuidInfo, 0x80000000);
    uint32_t maxCpuIdEx = static_cast<uint32_t>(cpuidInfo[EAX]);

    if (maxCpuIdEx >= 0x80000001)
    {
        __cpuid(cpuidInfo, 0x80000001);

        if ((cpuidInfo[ECX] & (1 << 5)) != 0)                                                               // LZCNT
        {
            g_cpuFeatures |= XArchIntrinsicConstants_Lzcnt;
        }

#ifdef HOST_AMD64
        // AMD has a "fast" mode for fxsave/fxrstor, which omits the saving of xmm registers.  The OS will enable this mode
        // if it is supported.  So if we continue to use fxsave/fxrstor, we must manually save/restore the xmm registers.
        // fxsr_opt is bit 25 of EDX
        if ((cpuidInfo[EDX] & (1 << 25)) != 0)
            g_fHasFastFxsave = true;
#endif
    }
#endif // HOST_X86 || HOST_AMD64

#if defined(HOST_ARM64)
    PAL_GetCpuCapabilityFlags (&g_cpuFeatures);
#endif

    if ((g_cpuFeatures & g_requiredCpuFeatures) != g_requiredCpuFeatures)
    {
        return false;
    }
#endif // HOST_X86|| HOST_AMD64 || HOST_ARM64

    return true;
}
#endif // !USE_PORTABLE_HELPERS

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

volatile bool g_processShutdownHasStarted = false;

static void DllThreadDetach()
{
    // BEWARE: loader lock is held here!

    // Should have already received a call to FiberDetach for this thread's "home" fiber.
    Thread* pCurrentThread = ThreadStore::GetCurrentThreadIfAvailable();
    if (pCurrentThread != NULL && !pCurrentThread->IsDetached())
    {
        // Once shutdown starts, RuntimeThreadShutdown callbacks are ignored, implying that
        // it is no longer guaranteed that exiting threads will be detached.
        if (!g_processShutdownHasStarted)
        {
            ASSERT_UNCONDITIONALLY("Detaching thread whose home fiber has not been detached");
            RhFailFast();
        }
    }
}

void RuntimeThreadShutdown(void* thread)
{
    // Note: loader lock is normally *not* held here!
    // The one exception is that the loader lock may be held during the thread shutdown callback
    // that is made for the single thread that runs the final stages of orderly process
    // shutdown (i.e., the thread that delivers the DLL_PROCESS_DETACH notifications when the
    // process is being torn down via an ExitProcess call).

    UNREFERENCED_PARAMETER(thread);

#ifdef TARGET_UNIX
    // Some Linux toolset versions call thread-local destructors during shutdown on a wrong thread.
    if ((Thread*)thread != ThreadStore::GetCurrentThread())
    {
        return;
    }
#else
    ASSERT((Thread*)thread == ThreadStore::GetCurrentThread());
#endif

    if (g_processShutdownHasStarted)
    {
        return;
    }

    ThreadStore::DetachCurrentThread();
}

extern "C" bool RhInitialize()
{
    if (!PalInit())
        return false;

    if (!InitDLL(PalGetModuleHandleFromPointer((void*)&RhInitialize)))
        return false;

    // Populate the values needed for debugging
    PopulateDebugHeaders();

    return true;
}

COOP_PINVOKE_HELPER(void, RhpEnableConservativeStackReporting, ())
{
    GetRuntimeInstance()->EnableConservativeStackReporting();
}

//
// Currently called only from a managed executable once Main returns, this routine does whatever is needed to
// cleanup managed state before exiting. There's not a lot here at the moment since we're always about to let
// the OS tear the process down anyway.
//
// @TODO: Eventually we'll probably have a hosting API and explicit shutdown request. When that happens we'll
// something more sophisticated here since we won't be able to rely on the OS cleaning up after us.
//
COOP_PINVOKE_HELPER(void, RhpShutdown, ())
{
    // Indicate that runtime shutdown is complete and that the caller is about to start shutting down the entire process.
    g_processShutdownHasStarted = true;
}

#ifdef _WIN32
EXTERN_C UInt32_BOOL WINAPI RtuDllMain(HANDLE hPalInstance, uint32_t dwReason, void* /*pvReserved*/)
{
    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
    {
        STARTUP_TIMELINE_EVENT(PROCESS_ATTACH_BEGIN);

        if (!InitDLL(hPalInstance))
            return FALSE;

        STARTUP_TIMELINE_EVENT(PROCESS_ATTACH_COMPLETE);
    }
    break;

    case DLL_PROCESS_DETACH:
        UninitDLL();
        break;

    case DLL_THREAD_DETACH:
        DllThreadDetach();
        break;
    }

    return TRUE;
}
#endif // _WIN32

#endif // !DACCESS_COMPILE
