// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"
#include <time.h>
#include <sys/time.h>

#define INITGUID
#include <guiddef.h>

DEFINE_GUID(IID_IUnknown, 0x00000000, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

#if defined(__linux__)
#define PAL_FUNCTION_PREFIX "DAC_"
#else
#define PAL_FUNCTION_PREFIX
#endif

typedef int (*PFN_PAL_InitializeDLL)();
typedef void (*PFN_PAL_TerminateEx)(int);

typedef BOOL (*PFN_PAL_VirtualUnwindOutOfProc)(
    CONTEXT *context,
    KNONVOLATILE_CONTEXT_POINTERS *contextPointers,
    PULONG64 functionStart,
    SIZE_T baseAddress,
    UnwindReadMemoryCallback readMemoryCallback);

typedef BOOL (*PFN_PAL_GetUnwindInfoSize)(
    SIZE_T baseAddress,
    ULONG64 ehFrameHdrAddr,
    UnwindReadMemoryCallback readMemoryCallback,
    PULONG64 ehFrameStart,
    PULONG64 ehFrameSize);

bool g_initialized = false;
PFN_PAL_InitializeDLL g_PAL_InitializeDLL = nullptr;
PFN_PAL_TerminateEx g_PAL_TerminateEx = nullptr;
PFN_PAL_VirtualUnwindOutOfProc g_PAL_VirtualUnwindOutOfProc = nullptr;
PFN_PAL_GetUnwindInfoSize g_PAL_GetUnwindInfoSize = nullptr;

bool
InitializePAL()
{
    if (g_initialized)
    {
        return true;
    }
    g_initialized = true;

    // Get createdump's path and load the DAC next to it.
    Dl_info info;
    if (dladdr((PVOID)&InitializePAL, &info) == 0)
    {
        printf_error("InitializePAL: dladdr(&InitializePAL) FAILED %s\n", dlerror());
        return false;
    }
    std::string dacPath;
    dacPath.append(info.dli_fname);
    dacPath = GetDirectory(dacPath);
    dacPath.append(MAKEDLLNAME_A("mscordaccore"));

    // Load the DAC next to createdump
    void* dacModule = dlopen(dacPath.c_str(), RTLD_LAZY);
    if (dacModule == nullptr)
    {
        printf_error("InitializePAL: dlopen(%s) FAILED %s\n", dacPath.c_str(), dlerror());
        return false;
    }

    // Get the PAL entry points needed by createdump
    g_PAL_InitializeDLL = (PFN_PAL_InitializeDLL)dlsym(dacModule, PAL_FUNCTION_PREFIX "PAL_InitializeDLL");
    if (g_PAL_InitializeDLL == nullptr)
    {
        printf_error("InitializePAL: dlsym(PAL_InitializeDLL) FAILED %s\n", dlerror());
        return false;
    }
    if (g_PAL_InitializeDLL() != 0)
    {
        printf_error("InitializePAL: PAL initialization FAILED\n");
        return false;
    }
    g_PAL_TerminateEx = (PFN_PAL_TerminateEx)dlsym(dacModule, PAL_FUNCTION_PREFIX "PAL_TerminateEx");
    g_PAL_VirtualUnwindOutOfProc = (PFN_PAL_VirtualUnwindOutOfProc)dlsym(dacModule, PAL_FUNCTION_PREFIX "PAL_VirtualUnwindOutOfProc");
    g_PAL_GetUnwindInfoSize = (PFN_PAL_GetUnwindInfoSize)dlsym(dacModule, PAL_FUNCTION_PREFIX "PAL_GetUnwindInfoSize");
    return true;
}

void
UninitializePAL(
    int exitCode)
{
    if (g_PAL_TerminateEx != nullptr)
    {
        g_PAL_TerminateEx(exitCode);
    }
}

#define tccSecondsToNanoSeconds 1000000000      // 10^9

BOOL
PALAPI
QueryPerformanceCounter(
    OUT LARGE_INTEGER* lpPerformanceCount)
{
#if HAVE_CLOCK_GETTIME_NSEC_NP
    lpPerformanceCount->QuadPart = (LONGLONG)clock_gettime_nsec_np(CLOCK_UPTIME_RAW);
#elif HAVE_CLOCK_MONOTONIC
    struct timespec ts;
    int result = clock_gettime(CLOCK_MONOTONIC, &ts);
    if (result != 0)
    {
        return TRUE;
    }
    else
    {
        lpPerformanceCount->QuadPart = ((LONGLONG)(ts.tv_sec) * (LONGLONG)(tccSecondsToNanoSeconds)) + (LONGLONG)(ts.tv_nsec);
    }
#else
    #error "The createdump requires either mach_absolute_time() or clock_gettime(CLOCK_MONOTONIC) to be supported."
#endif
    return TRUE;
}

BOOL
PALAPI
QueryPerformanceFrequency(
    OUT LARGE_INTEGER* lpFrequency)
{
#if HAVE_CLOCK_GETTIME_NSEC_NP
    lpFrequency->QuadPart = (LONGLONG)(tccSecondsToNanoSeconds);
#elif HAVE_CLOCK_MONOTONIC
    // clock_gettime() returns a result in terms of nanoseconds rather than a count. This
    // means that we need to either always scale the result by the actual resolution (to
    // get a count) or we need to say the resolution is in terms of nanoseconds. We prefer
    // the latter since it allows the highest throughput and should minimize error propagated
    // to the user.
    lpFrequency->QuadPart = (LONGLONG)(tccSecondsToNanoSeconds);
#else
    #error "The createdump requires either mach_absolute_time() or clock_gettime(CLOCK_MONOTONIC) to be supported."
#endif
    return TRUE;
}

#define TEMP_DIRECTORY_PATH "/tmp/"

DWORD
PALAPI
GetTempPathA(
    IN DWORD nBufferLength,
    OUT LPSTR lpBuffer)
{
    DWORD dwPathLen = 0;
    const char *tempDir = getenv("TMPDIR");
    if (tempDir == nullptr)
    {
        tempDir = TEMP_DIRECTORY_PATH;
    }
    size_t tempDirLen = strlen(tempDir);
    if (tempDirLen < nBufferLength)
    {
        dwPathLen = tempDirLen;
        strcpy_s(lpBuffer, nBufferLength, tempDir);
    }
    else
    {
        // Get the required length
        dwPathLen = tempDirLen + 1;
    }
    return dwPathLen;
}

BOOL
PALAPI
PAL_VirtualUnwindOutOfProc(
    CONTEXT *context,
    KNONVOLATILE_CONTEXT_POINTERS *contextPointers,
    PULONG64 functionStart,
    SIZE_T baseAddress,
    UnwindReadMemoryCallback readMemoryCallback)
{
    if (!InitializePAL() || g_PAL_VirtualUnwindOutOfProc == nullptr)
    {
        return FALSE;
    }
    return g_PAL_VirtualUnwindOutOfProc(context, contextPointers, functionStart, baseAddress, readMemoryCallback);
}

BOOL
PALAPI
PAL_GetUnwindInfoSize(
    SIZE_T baseAddress,
    ULONG64 ehFrameHdrAddr,
    UnwindReadMemoryCallback readMemoryCallback,
    PULONG64 ehFrameStart,
    PULONG64 ehFrameSize)
{
    if (!InitializePAL() || g_PAL_GetUnwindInfoSize == nullptr)
    {
        return FALSE;
    }
    return g_PAL_GetUnwindInfoSize(baseAddress, ehFrameHdrAddr, readMemoryCallback, ehFrameStart, ehFrameSize);
}

//
// Used in pal\inc\rt\safecrt.h's _invalid_parameter handler
//

VOID
PALAPI
RaiseException(
    IN DWORD dwExceptionCode,
    IN DWORD dwExceptionFlags,
    IN DWORD nNumberOfArguments,
    IN CONST ULONG_PTR* lpArguments)
{
    throw;
}

size_t u16_strlen(const WCHAR* str)
{
    size_t nChar = 0;
    while (*str++)
        nChar++;
    return nChar;
}

//
// Used by _ASSERTE
//

#ifdef _DEBUG
DWORD
PALAPI
GetCurrentProcessId()
{
    return getpid();
}

VOID
PALAPI
DebugBreak()
{
    abort();
}

#endif // DEBUG

