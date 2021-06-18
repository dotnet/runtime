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
    size_t faultSp = (size_t)MCREG_R15(ucontext->uc_mcontext);

    _ASSERTE(IS_ALIGNED(faultSp, 8));

    if (customSp == 0)
    {
        customSp = faultSp;
    }

    size_t fakeFrameReturnAddress;
    fakeFrameReturnAddress = (size_t)SignalHandlerWorkerReturnOffset0 + (size_t)CallSignalHandlerWrapper0;

    // Build fake stack frame to enable the stack unwinder to unwind from signal_handler_worker to the faulting instruction
    size_t* saveArea = (size_t*)(customSp - 160);
    saveArea[14] = (size_t)MCREG_PSWAddr(ucontext->uc_mcontext);
    saveArea[15] = faultSp;
    size_t sp = (size_t)saveArea - 160;

    // Switch the current context to the signal_handler_worker and the custom stack
    CONTEXT context2;
    RtlCaptureContext(&context2);

    context2.PSWAddr = (size_t)signal_handler_worker;
    context2.R2 = code;
    context2.R3 = (size_t)siginfo;
    context2.R4 = (size_t)context;
    context2.R5 = (size_t)returnPoint;
    context2.R6 = (size_t)MCREG_R6(ucontext->uc_mcontext);
    context2.R7 = (size_t)MCREG_R7(ucontext->uc_mcontext);
    context2.R8 = (size_t)MCREG_R8(ucontext->uc_mcontext);
    context2.R9 = (size_t)MCREG_R9(ucontext->uc_mcontext);
    context2.R10 = (size_t)MCREG_R10(ucontext->uc_mcontext);
    context2.R11 = (size_t)MCREG_R11(ucontext->uc_mcontext);
    context2.R12 = (size_t)MCREG_R12(ucontext->uc_mcontext);
    context2.R13 = (size_t)MCREG_R13(ucontext->uc_mcontext);
    context2.R14 = fakeFrameReturnAddress;
    context2.R15 = sp;

    RtlRestoreContext(&context2, NULL);
}
