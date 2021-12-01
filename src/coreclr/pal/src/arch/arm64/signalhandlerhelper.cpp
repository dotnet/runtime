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
    size_t faultSp = (size_t)MCREG_Sp(ucontext->uc_mcontext);
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

    // preserve 128 bytes long red zone and align stack pointer
    size_t* sp = (size_t*)customSp;

    // Build fake stack frame to enable the stack unwinder to unwind from signal_handler_worker to the faulting instruction
    // pushed LR
    *--sp = (size_t)MCREG_Pc(ucontext->uc_mcontext);
    // pushed frame pointer
    *--sp = (size_t)MCREG_Fp(ucontext->uc_mcontext);

    // Switch the current context to the signal_handler_worker and the original stack
    CONTEXT context2;
    RtlCaptureContext(&context2);

    context2.Sp = (size_t)sp;
    context2.Fp = (size_t)sp;
    context2.Lr = fakeFrameReturnAddress;
    context2.Pc = (size_t)signal_handler_worker;
    context2.X0 = code;
    context2.X1 = (size_t)siginfo;
    context2.X2 = (size_t)context;
    context2.X3 = (size_t)returnPoint;

    RtlRestoreContext(&context2, NULL);
}
