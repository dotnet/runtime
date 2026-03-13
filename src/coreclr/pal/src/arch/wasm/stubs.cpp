// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal/dbgmsg.h"
#include "pal/signal.hpp"
#include <emscripten/emscripten.h>

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
