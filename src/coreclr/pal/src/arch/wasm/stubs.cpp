// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal/dbgmsg.h"
#include "pal/signal.hpp"
#include "pal/context.h"
#include "pal/seh.hpp"

#if defined(TARGET_BROWSER)
#include <emscripten/emscripten.h>
#elif defined(TARGET_WASI)
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <errno.h>
#include <stdio.h>
#include "pal/wasi/pal_wasi_missing.h"
// RESERVED_SEH_BIT is defined file-locally in seh.cpp (internal linkage) and
// isn't exposed in a header, so redefine the value here for WASI.
namespace { const UINT WASI_RESERVED_SEH_BIT = 0x800000; }
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
    _ASSERT(!"ThrowExceptionFromContextInternal not implemented on wasm");
}

/* unwind */

void ExecuteHandlerOnCustomStack(int code, siginfo_t *siginfo, void *context, size_t sp, SignalHandlerWorkerReturnPoint* returnPoint)
{
    _ASSERT(!"ExecuteHandlerOnCustomStack not implemented on wasm");
}

#if defined(TARGET_BROWSER)
// seh-unwind.cpp is excluded from the browser-wasm build, but emscripten's
// libunwind shim still exposes the unw_* symbols at link time. Trap stubs
// satisfy any residual references.
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
// WASI build skips thread/context.cpp (no native_context_t / siginfo_t /
// FPE_*). Trap stubs satisfy linker references in headers still compiled;
// these are never reached on WASI.

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

// WIN32 SEH entry — normally in seh-unwind.cpp which we exclude on WASI.
// Mirrors that file's RaiseException: allocate EXCEPTION_RECORD + CONTEXT,
// fill the record, throw PAL_SEHException (what RtlpRaiseException does on
// Unix). The CONTEXT capture / PAL_VirtualUnwind step is skipped — already
// guarded by #ifndef TARGET_WASM in seh-unwind.cpp.
extern "C" VOID PALAPI
RaiseException(
    IN DWORD dwExceptionCode,
    IN DWORD dwExceptionFlags,
    IN DWORD nNumberOfArguments,
    IN CONST ULONG_PTR *lpArguments)
{
    if (dwExceptionCode & WASI_RESERVED_SEH_BIT)
    {
        dwExceptionCode ^= WASI_RESERVED_SEH_BIT;
    }

    if (nNumberOfArguments > EXCEPTION_MAXIMUM_PARAMETERS)
    {
        nNumberOfArguments = EXCEPTION_MAXIMUM_PARAMETERS;
    }

    EXCEPTION_RECORD *exceptionRecord;
    CONTEXT *contextRecord;
    AllocateExceptionRecords(&exceptionRecord, &contextRecord);

    exceptionRecord->ExceptionCode = dwExceptionCode;
    exceptionRecord->ExceptionFlags = dwExceptionFlags;
    exceptionRecord->ExceptionRecord = NULL;
    exceptionRecord->ExceptionAddress = NULL;
    exceptionRecord->NumberParameters = nNumberOfArguments;
    if (nNumberOfArguments)
    {
        memcpy(exceptionRecord->ExceptionInformation, lpArguments,
               nNumberOfArguments * sizeof(ULONG_PTR));
    }

    throw PAL_SEHException(exceptionRecord, contextRecord);
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

// PAL exception-record allocation — normally in seh-unwind.cpp which we
// exclude on WASI. Both records share a single allocation (mirroring the
// Unix layout) so the matching free is a single free() of the combined
// memory, keyed off the contextRecord pointer.
struct WasiExceptionRecords
{
    CONTEXT          ContextRecord;
    EXCEPTION_RECORD ExceptionRecord;
};

extern "C" PALIMPORT VOID PALAPI
PAL_FreeExceptionRecords(IN EXCEPTION_RECORD *exceptionRecord, IN CONTEXT *contextRecord)
{
    (void)exceptionRecord;
    // contextRecord is the start of the combined WasiExceptionRecords allocation.
    free(contextRecord);
}

VOID
AllocateExceptionRecords(EXCEPTION_RECORD** exceptionRecord, CONTEXT** contextRecord)
{
    WasiExceptionRecords* records = (WasiExceptionRecords*)calloc(1, sizeof(WasiExceptionRecords));
    if (records == nullptr)
    {
        // No fallback pool on WASI (single-threaded, no async signals).
        abort();
    }
    *contextRecord = &records->ContextRecord;
    *exceptionRecord = &records->ExceptionRecord;
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
