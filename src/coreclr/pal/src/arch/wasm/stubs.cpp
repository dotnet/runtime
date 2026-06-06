// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal/dbgmsg.h"
#include "pal/signal.hpp"
#include "pal/context.h"

#if defined(TARGET_BROWSER)
#include <emscripten/emscripten.h>
#elif defined(TARGET_WASI)
#include <stdlib.h>
#include <stdint.h>
#include <errno.h>
#include <stdio.h>
#include "pal/wasi/pal_wasi_missing.h"
#endif

SET_DEFAULT_DEBUG_CHANNEL(EXCEPT); // some headers have code with asserts, so do this first

/* debugbreak */

#ifdef _DEBUG
extern void DBG_PrintInterpreterStack();
#endif // _DEBUG

extern "C" void
DBG_DebugBreak()
{
#if defined(TARGET_BROWSER)
#ifdef _DEBUG
    DBG_PrintInterpreterStack();
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
#else // _DEBUG
    emscripten_throw_string("Debug break called in release build.");
#endif // _DEBUG
#elif defined(TARGET_WASI)
    // WASI has no host-side debugger hook; use the wasm trap intrinsic.
#ifdef _DEBUG
    DBG_PrintInterpreterStack();
#endif
    __builtin_debugtrap();
    abort();
#endif
}

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
#if defined(TARGET_BROWSER)
    _ASSERT(!"ThrowExceptionFromContextInternal not implemented on wasm");
#else
    // On WASI native wasm exceptions are used; the interpreter throws via the
    // C++ exception machinery directly. This entrypoint exists for the WIN-style
    // signature only.
    _ASSERT(!"ThrowExceptionFromContextInternal not implemented on wasi");
#endif
}

/* unwind */

void ExecuteHandlerOnCustomStack(int code, siginfo_t *siginfo, void *context, size_t sp, SignalHandlerWorkerReturnPoint* returnPoint)
{
    _ASSERT(!"ExecuteHandlerOnCustomStack not implemented on wasm");
}

#if defined(TARGET_BROWSER)
// On the browser-wasm target seh-unwind.cpp is excluded from the build (see
// pal/src/CMakeLists.txt) but emscripten's libunwind shim still exposes the
// unw_* symbols at link time. Provide trap stubs to satisfy any residual
// references during the wasm interpreter's stack walking.
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
#endif // TARGET_BROWSER

/* threading */

extern "C" int pthread_setschedparam(pthread_t, int, const struct sched_param *)
{
    _ASSERT(!"pthread_setschedparam not implemented on wasm");
    return 0;
}

#if defined(TARGET_WASI)
// WASI build skips thread/context.cpp (no native_context_t / siginfo_t / FPE_*).
// Provide trap stubs for the PAL context API that file would otherwise supply.
// These are never reached on WASI: hardware exceptions don't exist, signals
// aren't delivered, and the interpreter walks its own explicit frame chain
// rather than calling these PAL APIs. Stubs exist purely so the linker can
// resolve references in headers that are still compiled.

extern "C" BOOL CONTEXT_GetRegisters(DWORD processId, LPCONTEXT lpContext)
{
    _ASSERT(!"CONTEXT_GetRegisters not implemented on wasi");
    return FALSE;
}

void CONTEXTToNativeContext(CONST CONTEXT *lpContext, native_context_t *native)
{
    _ASSERT(!"CONTEXTToNativeContext not implemented on wasi");
}

void CONTEXTFromNativeContext(const native_context_t *native, LPCONTEXT lpContext,
                              ULONG contextFlags)
{
    _ASSERT(!"CONTEXTFromNativeContext not implemented on wasi");
}

LPVOID GetNativeContextPC(const native_context_t *context)
{
    _ASSERT(!"GetNativeContextPC not implemented on wasi");
    return nullptr;
}

LPVOID GetNativeContextSP(const native_context_t *context)
{
    _ASSERT(!"GetNativeContextSP not implemented on wasi");
    return nullptr;
}

DWORD CONTEXTGetExceptionCodeForSignal(const siginfo_t *siginfo,
                                       const native_context_t *context)
{
    _ASSERT(!"CONTEXTGetExceptionCodeForSignal not implemented on wasi");
    return 0;
}

// RaiseException is the WIN32-style SEH entry — normally provided by
// seh-unwind.cpp, which we exclude on WASI. The managed exception path on the
// WASI interpreter throws PAL_SEHException via C++ throw instead, so this
// surface is never reached. Stub kept only to satisfy linker references in
// SString / NamespaceUtil / CLRConfig.
extern "C" VOID PALAPI
RaiseException(
    IN DWORD dwExceptionCode,
    IN DWORD dwExceptionFlags,
    IN DWORD nNumberOfArguments,
    IN CONST ULONG_PTR *lpArguments)
{
    (void)dwExceptionFlags; (void)nNumberOfArguments; (void)lpArguments;
    _ASSERT(!"RaiseException not implemented on wasi");
    abort();
}

// WIN32 debug-output APIs. WASI build excludes pal/src/debug/debug.cpp; route
// to the same internal channels stderr would on a real platform.
extern "C" VOID PALAPI
DebugBreak()
{
    _ASSERT(!"DebugBreak not implemented on wasi");
    __builtin_debugtrap();
    abort();
}

extern "C" VOID PALAPI
OutputDebugStringA(IN LPCSTR lpOutputString)
{
    if (lpOutputString) fputs(lpOutputString, stderr);
}

extern "C" VOID PALAPI
OutputDebugStringW(IN LPCWSTR lpOutputString)
{
    (void)lpOutputString;  // wchar_t* — not converted here; stub only
}

// PAL exception-record allocation. Normally provided by seh-unwind.cpp, which
// we exclude on WASI. The WASI interpreter doesn't dispatch through the
// EXCEPTION_RECORD/CONTEXT machinery (it throws PAL_SEHException directly),
// so these are only here to satisfy linker references in dispatch helpers
// that never run.
extern "C" PALIMPORT VOID PALAPI
PAL_FreeExceptionRecords(IN EXCEPTION_RECORD *exceptionRecord, IN CONTEXT *contextRecord)
{
    (void)exceptionRecord;
    free(contextRecord);
}

extern "C" VOID
AllocateExceptionRecords(EXCEPTION_RECORD** exceptionRecord, CONTEXT** contextRecord)
{
    *exceptionRecord = (EXCEPTION_RECORD*)calloc(1, sizeof(EXCEPTION_RECORD));
    *contextRecord = (CONTEXT*)calloc(1, sizeof(CONTEXT));
}

extern "C" PALIMPORT BOOL PALAPI
PAL_VirtualUnwind(CONTEXT *context)
{
    (void)context;
    _ASSERT(!"PAL_VirtualUnwind not implemented on wasi");
    return FALSE;
}

// Signal-handling surface — pal/src/exception/signal.cpp is excluded on WASI.
// Stubs return success/no-op so the PAL init/shutdown paths continue.
// These are C++ symbols (declared without extern "C" in pal/src/include/pal/signal.hpp).
BOOL SEHInitializeSignals(CorUnix::CPalThread* pthrCurrent, DWORD flags)
{
    (void)pthrCurrent; (void)flags;
    return TRUE;
}

void SEHCleanupSignals(bool isChildProcess)
{
    (void)isChildProcess;
}

void UnmaskActivationSignal()
{
}

extern "C" VOID PALAPI
PAL_EnableCrashReportBeforeSignalChaining(void)
{
    // No-op: WASI has no signal delivery.
}

// Per-thread context retrieval. Normally implemented via ptrace on Unix.
// WASI single-process has no other thread to inspect.
extern "C" PALIMPORT BOOL PALAPI
GetThreadContext(IN HANDLE hThread, IN OUT LPCONTEXT lpContext)
{
    (void)hThread; (void)lpContext;
    _ASSERT(!"GetThreadContext not implemented on wasi");
    return FALSE;
}

// FlushInstructionCache: no-op on WASI. wasm has no separate I-cache to flush.
extern "C" PALIMPORT BOOL PALAPI
FlushInstructionCache(IN HANDLE hProcess, IN LPCVOID lpBaseAddress, IN SIZE_T dwSize)
{
    (void)hProcess; (void)lpBaseAddress; (void)dwSize;
    return TRUE;
}

// SystemJS_GetLocaleInfo is the browser ICU shim entrypoint
// (src/native/libs/System.Native.Browser/native/globalization-locale.ts).
// On WASI we don't have a JS host; return null. The managed Globalization
// layer must already gracefully handle this for the browser-not-loaded case.
extern "C" uint16_t* SystemJS_GetLocaleInfo(
    const uint16_t* locale, int32_t localeLength,
    const uint16_t* culture, int32_t cultureLength,
    const uint16_t* result, int32_t resultMaxLength,
    int* resultLength)
{
    (void)locale; (void)localeLength; (void)culture; (void)cultureLength;
    (void)result; (void)resultMaxLength;
    if (resultLength) *resultLength = 0;
    return nullptr;
}

// PAL also indirectly references getpwuid_r through PAL_GetUserName-style
// paths (init/pal.cpp transitively includes <pwd.h>). Provide a stub so
// transitive references resolve.
extern "C" int getpwuid_r(uid_t uid, struct passwd *pwd, char *buf, size_t buflen, struct passwd **result)
{
    (void)uid; (void)pwd; (void)buf; (void)buflen;
    if (result) *result = nullptr;
    return ENOTSUP;
}
#endif // TARGET_WASI
