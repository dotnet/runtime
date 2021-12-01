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

    _ASSERTE(IS_ALIGNED(faultSp, 4));

    if (customSp == 0)
    {
        // preserve 8 bytes long red zone and align stack pointer
        customSp = ALIGN_DOWN(faultSp - 8, 8);
    }

    size_t fakeFrameReturnAddress;

    if (IS_ALIGNED(faultSp, 8))
    {
        fakeFrameReturnAddress = (size_t)SignalHandlerWorkerReturnOffset0 + (size_t)CallSignalHandlerWrapper0;
    }
    else
    {
        fakeFrameReturnAddress = (size_t)SignalHandlerWorkerReturnOffset4 + (size_t)CallSignalHandlerWrapper4;
    }

    size_t* sp = (size_t*)customSp;

#ifndef __linux__
    size_t cpsr = (size_t)MCREG_Cpsr(ucontext->uc_mcontext);

    // Build fake stack frame to enable the stack unwinder to unwind from signal_handler_worker to the faulting instruction
    // align
    --sp;
    // pushed LR with correct mode bit
    *--sp = (size_t)MCREG_Pc(ucontext->uc_mcontext) | ((cpsr & (1 << 5)) >> 5);
    // pushed frame pointer
    *--sp = (size_t)MCREG_R11(ucontext->uc_mcontext);
    *--sp = (size_t)MCREG_R7(ucontext->uc_mcontext);
#else
    size_t size = ALIGN_UP(sizeof(ucontext->uc_mcontext), 8);
    sp -= size / sizeof(size_t);
    *(mcontext_t *)sp = ucontext->uc_mcontext;
#endif

    // Switch the current context to the signal_handler_worker and the original stack
    CONTEXT context2;
    RtlCaptureContext(&context2);

    // We don't care about the other registers state since the stack unwinding restores
    // them for the target frame directly from the signal context.
    context2.Sp = (size_t)sp;
    context2.R7 = (size_t)sp; // Fp and Sp are the same
    context2.Lr = fakeFrameReturnAddress;
    context2.Pc = (size_t)signal_handler_worker;
    context2.R0 = code;
    context2.R1 = (size_t)siginfo;
    context2.R2 = (size_t)context;
    context2.R3 = (size_t)returnPoint;

    RtlRestoreContext(&context2, NULL);
}
