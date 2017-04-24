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
    size_t faultSp = (size_t)MCREG_Esp(ucontext->uc_mcontext);

    _ASSERTE(IS_ALIGNED(faultSp, 4));

    size_t fakeFrameReturnAddress;

    switch (faultSp & 0xc)
    {
        case 0x0:
            fakeFrameReturnAddress = (size_t)SignalHandlerWorkerReturnOffset0 + (size_t)CallSignalHandlerWrapper0;
            break;
        case 0x4:
            fakeFrameReturnAddress = (size_t)SignalHandlerWorkerReturnOffset4 + (size_t)CallSignalHandlerWrapper4;
            break;
        case 0x8:
            fakeFrameReturnAddress = (size_t)SignalHandlerWorkerReturnOffset8 + (size_t)CallSignalHandlerWrapper8;
            break;
        case 0xc:
            fakeFrameReturnAddress = (size_t)SignalHandlerWorkerReturnOffset12 + (size_t)CallSignalHandlerWrapper12;
            break;
    }

    size_t* sp = (size_t*)ALIGN_DOWN(faultSp, 16);

    // Build fake stack frame to enable the stack unwinder to unwind from signal_handler_worker to the faulting instruction
    *--sp = (size_t)MCREG_Eip(ucontext->uc_mcontext);
    *--sp = (size_t)MCREG_Ebp(ucontext->uc_mcontext);
    size_t fp = (size_t)sp;
    // Align stack
    sp -= 2; 
    *--sp = (size_t)returnPoint;
    *--sp = (size_t)context;
    *--sp = (size_t)siginfo;
    *--sp = code;
    *--sp = fakeFrameReturnAddress;

    // Switch the current context to the signal_handler_worker and the original stack
    CONTEXT context2;
    RtlCaptureContext(&context2);

    // We don't care about the other registers state since the stack unwinding restores
    // them for the target frame directly from the signal context.
    context2.Esp = (size_t)sp;
    context2.Ebp = (size_t)fp;
    context2.Eip = (size_t)signal_handler_worker;

    RtlRestoreContext(&context2, NULL);
}
