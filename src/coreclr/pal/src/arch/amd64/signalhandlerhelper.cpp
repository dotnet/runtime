// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(EXCEPT); // some headers have code with asserts, so do this first

#include "pal/palinternal.h"
#include "pal/context.h"
#include "pal/signal.hpp"
#include "pal/utils.h"
#include <sys/ucontext.h>

/*++
Function :
    ExecuteHandlerOnCustomStack

    Execute signal handler on a custom stack, the current stack pointer is specified by the customSp
    If the customSp is 0, then the handler is executed on the original stack where the signal was fired.
    It installs a fake stack frame to enable stack unwinding to the signal source location.

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)
    returnPoint - context to which the function returns if the common_signal_handler returns

    (no return value)
--*/
void ExecuteHandlerOnCustomStack(int code, siginfo_t *siginfo, void *context, size_t customSp, SignalHandlerWorkerReturnPoint* returnPoint)
{
    ucontext_t *ucontext = (ucontext_t *)context;
    size_t faultSp = (size_t)MCREG_Rsp(ucontext->uc_mcontext);

    _ASSERTE(IS_ALIGNED(faultSp, 8));

    if (customSp == 0)
    {
        // preserve 128 bytes long red zone and align stack pointer
        customSp = ALIGN_DOWN(faultSp - 128, 16);
    }

    size_t fakeFrameReturnAddress;

    if (IS_ALIGNED(faultSp, 16))
    {
        fakeFrameReturnAddress = (size_t)SignalHandlerWorkerReturnOffset0 + (size_t)CallSignalHandlerWrapper0;
    }
    else
    {
        fakeFrameReturnAddress = (size_t)SignalHandlerWorkerReturnOffset8 + (size_t)CallSignalHandlerWrapper8;
    }

    size_t* sp = (size_t*)customSp;

    // Build fake stack frame to enable the stack unwinder to unwind from signal_handler_worker to the faulting instruction
    *--sp = (size_t)MCREG_Rip(ucontext->uc_mcontext);
    *--sp = (size_t)MCREG_Rbp(ucontext->uc_mcontext);
    size_t fp = (size_t)sp;
    *--sp = fakeFrameReturnAddress;

    // Switch the current context to the signal_handler_worker and the custom stack
    CONTEXT context2;
    RtlCaptureContext(&context2);

    // We don't care about the other registers state since the stack unwinding restores
    // them for the target frame directly from the signal context.
    context2.Rsp = (size_t)sp;
    context2.Rbx = (size_t)faultSp;
    context2.Rbp = (size_t)fp;
    context2.Rip = (size_t)signal_handler_worker;
    context2.Rdi = code;
    context2.Rsi = (size_t)siginfo;
    context2.Rdx = (size_t)context;
    context2.Rcx = (size_t)returnPoint;

    RtlRestoreContext(&context2, NULL);
}
