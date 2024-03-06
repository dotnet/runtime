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
#include <minipal/cpufeatures.h>

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

#define READONLY_ATTR __attribute__((section(".rodata")))

// const is so that it gets placed in the .text section (which is read-only)
// volatile is so that accesses to it do not get optimized away because of the const
//

extern "C" volatile READONLY_ATTR const GSCookie __security_cookie = 0;
#else
extern "C" volatile GSCookie __security_cookie = 0;
#endif // FEATURE_READONLY_GS_COOKIE

#endif // TARGET_UNIX

static RhConfig g_sRhConfig;
RhConfig* g_pRhConfig = &g_sRhConfig;

void InitializeGCEventLock();
bool InitializeGC();

static bool InitDLL(HANDLE hPalInstance)
{
#ifdef FEATURE_CACHED_INTERFACE_DISPATCH
    //
    // Initialize interface dispatch.
    //
    if (!InitializeInterfaceDispatch())
        return false;
#endif

    InitializeGCEventLock();

#ifdef FEATURE_PERFTRACING
    // Initialize EventPipe
    EventPipe_Initialize();
    // Initialize DS
    DiagnosticServer_Initialize();
    DiagnosticServer_PauseForDiagnosticsMonitor();
#endif
#ifdef FEATURE_EVENT_TRACE
    EventTracing_Initialize();
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

    if (!InitializeGC())
        return false;

    STARTUP_TIMELINE_EVENT(GC_INIT_COMPLETE);

#ifdef FEATURE_PERFTRACING
    // Finish setting up rest of EventPipe - specifically enable SampleProfiler if it was requested at startup.
    // SampleProfiler needs to cooperate with the GC which hasn't fully finished setting up in the first part of the
    // EventPipe initialization, so this is done after the GC has been fully initialized.
    EventPipe_FinishInitialize();
#endif

#ifndef USE_PORTABLE_HELPERS
    if (!DetectCPUFeatures())
        return false;
#endif

#ifdef TARGET_UNIX
    if (!InitGSCookie())
        return false;
#endif

    return true;
}

#ifndef USE_PORTABLE_HELPERS

bool DetectCPUFeatures()
{
#if defined(HOST_X86) || defined(HOST_AMD64) || defined(HOST_ARM64)
    g_cpuFeatures = minipal_getcpufeatures();

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

#if defined(_WIN32) && defined(FEATURE_PERFTRACING)
bool g_safeToShutdownTracing;
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
#ifdef _WIN32
    // We forgo shutting down event pipe if it wouldn't be safe and could lead to a hang.
    // If there was an active trace session, the trace will likely be corrupted without
    // orderly shutdown. See https://github.com/dotnet/runtime/issues/89346.
    if (g_safeToShutdownTracing)
#endif
    {
        EventPipe_Shutdown();
        DiagnosticServer_Shutdown();
    }
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

#ifdef FEATURE_PERFTRACING
    EventPipe_ThreadShutdown();
#endif
}

extern "C" bool RhInitialize(bool isDll)
{
    if (!PalInit())
        return false;

#if defined(_WIN32) || defined(FEATURE_PERFTRACING)
    atexit(&OnProcessExit);
#endif

#if defined(_WIN32) && defined(FEATURE_PERFTRACING)
    g_safeToShutdownTracing = !isDll;
#endif

    if (!InitDLL(PalGetModuleHandleFromPointer((void*)&RhInitialize)))
        return false;

    // Populate the values needed for debugging
    PopulateDebugHeaders();

    return true;
}

#endif // !DACCESS_COMPILE
