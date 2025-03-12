// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal/dbgmsg.h"
#include "pal/signal.hpp"

SET_DEFAULT_DEBUG_CHANNEL(EXCEPT); // some headers have code with asserts, so do this first

/* debugbreak */

extern "C" void
DBG_DebugBreak()
{
    asm volatile ("unreachable");
}

/* context */

extern "C" void
RtlCaptureContext(OUT PCONTEXT ContextRecord)
{
    _ASSERT("RtlCaptureContext not implemented on wasm");
}

extern "C" void
CONTEXT_CaptureContext(LPCONTEXT lpContext)
{
    _ASSERT("CONTEXT_CaptureContext not implemented on wasm");
}

extern "C" void ThrowExceptionFromContextInternal(CONTEXT* context, PAL_SEHException* ex)
{
    _ASSERT("ThrowExceptionFromContextInternal not implemented on wasm");
}

/* unwind */

void ExecuteHandlerOnCustomStack(int code, siginfo_t *siginfo, void *context, size_t sp, SignalHandlerWorkerReturnPoint* returnPoint)
{
    _ASSERT("ExecuteHandlerOnCustomStack not implemented on wasm");
}

extern "C" int unw_getcontext(int)
{
    _ASSERT("unw_getcontext not implemented on wasm");
    return 0;
}

extern "C" int unw_init_local(int, int)
{
    _ASSERT("unw_init_local not implemented on wasm");
    return 0;
}

extern "C" int unw_step(int)
{
    _ASSERT("unw_step not implemented on wasm");
    return 0;
}

extern "C" int unw_is_signal_frame(int)
{
    _ASSERT("unw_is_signal_frame not implemented on wasm");
    return 0;
}

/* threading */

extern "C" int pthread_setschedparam(pthread_t, int, const struct sched_param *)
{
    _ASSERT("pthread_setschedparam not implemented on wasm");
    return 0;
}
