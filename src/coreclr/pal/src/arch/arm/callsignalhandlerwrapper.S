// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

.syntax unified
#ifndef __armv6__
.thumb
#endif

.macro CALL_SIGNAL_HANDLER_WRAPPER Alignment

.globl C_FUNC(SignalHandlerWorkerReturnOffset\Alignment)
C_FUNC(SignalHandlerWorkerReturnOffset\Alignment):
    .int LOCAL_LABEL(SignalHandlerWorkerReturn\Alignment)-C_FUNC(CallSignalHandlerWrapper\Alignment)

// This function is never called, only a fake stack frame will be setup to have a return
// address set to SignalHandlerWorkerReturn during SIGSEGV handling.
// It enables the unwinder to unwind stack from the handling code to the actual failure site.
NESTED_ENTRY CallSignalHandlerWrapper\Alignment, _TEXT, NoHandler
#ifndef __linux__
__StackAllocationSize = (8 + 4 + \Alignment) // red zone + alignment
    alloc_stack __StackAllocationSize
    PROLOG_PUSH "{r7, r11, lr}"
    bl      EXTERNAL_C_FUNC(signal_handler_worker)
LOCAL_LABEL(SignalHandlerWorkerReturn\Alignment):
    EPILOG_POP "{r7, r11, lr}"
    free_stack __StackAllocationSize
    bx      lr
#else
    // This unwind information is needed for lldb gdb doesn't use it and tries
    // to read all registers from $sp + 12
    .save {r0-r15}
    .pad #12
    bl      EXTERNAL_C_FUNC(signal_handler_worker)
LOCAL_LABEL(SignalHandlerWorkerReturn\Alignment):
    // Following instruction are needed to say gdb that this frame is SIGTRAMP_FRAME
    // and it can restore all registers from stack
    mov.w r7, #119
    svc 0
#endif
NESTED_END CallSignalHandlerWrapper\Alignment, _TEXT

.endm

CALL_SIGNAL_HANDLER_WRAPPER 0
CALL_SIGNAL_HANDLER_WRAPPER 4
