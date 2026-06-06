// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal/dbgmsg.h"
#include "pal/signal.hpp"
#include "pal/context.h"

#if defined(TARGET_BROWSER)
#include <emscripten/emscripten.h>
#elif defined(TARGET_WASI)
#include <stdlib.h>
#include <errno.h>
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
