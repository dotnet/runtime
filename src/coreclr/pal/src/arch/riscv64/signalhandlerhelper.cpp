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
#error "TODO-RISCV64: missing implementation"
}
