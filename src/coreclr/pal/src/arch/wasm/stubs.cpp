// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal/dbgmsg.h"
#include "pal/signal.hpp"
#ifdef __EMSCRIPTEN__
#include <emscripten/emscripten.h>
#endif

SET_DEFAULT_DEBUG_CHANNEL(EXCEPT); // some headers have code with asserts, so do this first

/* debugbreak */

#ifdef _DEBUG
extern void DBG_PrintInterpreterStack();
#endif // _DEBUG

extern "C" void
DBG_DebugBreak()
{
#ifdef _DEBUG
    DBG_PrintInterpreterStack();
#ifdef __EMSCRIPTEN__
    double start = emscripten_get_now();
    emscripten_debugger();
    double end = emscripten_get_now();
    // trying to guess if the debugger was attached
    if (end - start < 100)
    {
        // If the debugger was not attached, abort the process
        // to match other platforms and fail fast
        emscripten_throw_string("Debugger not attached");
    }
#else
    abort();
#endif // __EMSCRIPTEN__
#else // _DEBUG
#ifdef __EMSCRIPTEN__
    emscripten_throw_string("Debug break called in release build.");
#else
    abort();
#endif // __EMSCRIPTEN__
#endif // _DEBUG
}

#ifdef TARGET_WASI
extern "C" void DebugBreak()
{
    DBG_DebugBreak();
}

extern "C" void PALAPI OutputDebugStringA(LPCSTR lpOutputString)
{
    (void)lpOutputString;
}

extern "C" void PALAPI OutputDebugStringW(LPCWSTR lpOutputString)
{
    (void)lpOutputString;
}

// C++ EH runtime symbols (__cxa_thread_atexit, __cxa_allocate_exception,
// __cxa_throw, __cxa_begin_catch, __cxa_end_catch) are provided by the
// wasm-exceptions-enabled libc++abi and libunwind.

BOOL PAL_ProbeMemory(PVOID pBuffer, DWORD cbBuffer, BOOL fWriteAccess)
{
    (void)pBuffer; (void)cbBuffer; (void)fWriteAccess;
    return TRUE;
}

void SEHCleanupSignals(bool isChildProcess)
{
    (void)isChildProcess;
}

void UnmaskActivationSignal()
{
}

BOOL SEHInitializeSignals(CorUnix::CPalThread *pthrCurrent, DWORD flags)
{
    (void)pthrCurrent; (void)flags;
    return TRUE;
}

extern "C" int shm_open(const char *name, int oflag, int mode)
{
    (void)name; (void)oflag; (void)mode;
    return -1;
}

extern "C" int shm_unlink(const char *name)
{
    (void)name;
    return -1;
}

BOOL PALAPI FlushInstructionCache(HANDLE hProcess, LPCVOID lpBaseAddress, SIZE_T dwSize)
{
    (void)hProcess; (void)lpBaseAddress; (void)dwSize;
    return TRUE;
}

BOOL PALAPI GetThreadContext(HANDLE hThread, LPCONTEXT lpContext)
{
    (void)hThread; (void)lpContext;
    return FALSE;
}

void PALAPI PAL_EnableCrashReportBeforeSignalChaining()
{
}

void PALAPI PAL_FreeExceptionRecords(IN EXCEPTION_RECORD *exceptionRecord, IN CONTEXT *contextRecord)
{
    (void)exceptionRecord; (void)contextRecord;
}

BOOL PALAPI PAL_VirtualUnwind(CONTEXT *context)
{
    (void)context;
    return FALSE;
}

VOID PALAPI RaiseException(DWORD dwExceptionCode, DWORD dwExceptionFlags, DWORD nNumberOfArguments, CONST ULONG_PTR *lpArguments)
{
    (void)dwExceptionCode; (void)dwExceptionFlags; (void)nNumberOfArguments; (void)lpArguments;
    abort();
}

extern "C" void SystemJS_GetLocaleInfo(void)
{
}
#endif

/* context */

extern "C" void
RtlCaptureContext(OUT PCONTEXT pContextRecord)
{
    // we cannot implement this function for wasm because there is no way to capture the current execution context
    memset(pContextRecord, 0, sizeof(*pContextRecord));
}

extern "C" void
CONTEXT_CaptureContext(LPCONTEXT lpContext)
{
    _ASSERT(!"CONTEXT_CaptureContext not implemented on wasm");
}

extern "C" void ThrowExceptionFromContextInternal(CONTEXT* context, PAL_SEHException* ex)
{
    _ASSERT(!"ThrowExceptionFromContextInternal not implemented on wasm");
}

/* unwind */

void ExecuteHandlerOnCustomStack(int code, siginfo_t *siginfo, void *context, size_t sp, SignalHandlerWorkerReturnPoint* returnPoint)
{
    _ASSERT(!"ExecuteHandlerOnCustomStack not implemented on wasm");
}

extern "C" int unw_getcontext(int)
{
    _ASSERT(!"unw_getcontext not implemented on wasm");
    return 0;
}

extern "C" int unw_init_local(int, int)
{
    _ASSERT(!"unw_init_local not implemented on wasm");
    return 0;
}

extern "C" int unw_step(int)
{
    _ASSERT(!"unw_step not implemented on wasm");
    return 0;
}

extern "C" int unw_is_signal_frame(int)
{
    _ASSERT(!"unw_is_signal_frame not implemented on wasm");
    return 0;
}

/* threading */

extern "C" int pthread_setschedparam(pthread_t, int, const struct sched_param *)
{
    _ASSERT(!"pthread_setschedparam not implemented on wasm");
    return 0;
}

#ifdef TARGET_WASI
extern "C" int pthread_getschedparam(pthread_t, int *policy, struct sched_param *param)
{
    if (policy) *policy = 0;
    return 0;
}
#endif // TARGET_WASI
