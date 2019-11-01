// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(EXCEPT); // some headers have code with asserts, so do this first

#include "pal/palinternal.h"
#include "pal/context.h"
#include "pal/signal.hpp"
#include "pal/utils.h"
#include <sys/ucontext.h>

/*++
Function :
    signal_handler_worker

    Handles signal on the original stack where the signal occured. 
    Invoked via setcontext.

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)
    returnPoint - context to which the function returns if the common_signal_handler returns

    (no return value)
--*/
void ExecuteHandlerOnOriginalStack(int code, siginfo_t *siginfo, void *context, SignalHandlerWorkerReturnPoint* returnPoint)
{
    ucontext_t *ucontext = (ucontext_t *)context;
    size_t faultSp = (size_t)MCREG_Rsp(ucontext->uc_mcontext);

    _ASSERTE(IS_ALIGNED(faultSp, 8));

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
    size_t* sp = (size_t*)ALIGN_DOWN(faultSp - 128, 16);

    // Build fake stack frame to enable the stack unwinder to unwind from signal_handler_worker to the faulting instruction
    *--sp = (size_t)MCREG_Rip(ucontext->uc_mcontext);
    *--sp = (size_t)MCREG_Rbp(ucontext->uc_mcontext);
    size_t fp = (size_t)sp;
    *--sp = fakeFrameReturnAddress;

    // Switch the current context to the signal_handler_worker and the original stack
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
