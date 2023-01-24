// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

.macro CALL_SIGNAL_HANDLER_WRAPPER Alignment

.globl C_FUNC(SignalHandlerWorkerReturnOffset\Alignment)
C_FUNC(SignalHandlerWorkerReturnOffset\Alignment):
    .int LOCAL_LABEL(SignalHandlerWorkerReturn\Alignment)-C_FUNC(CallSignalHandlerWrapper\Alignment)

// This function is never called, only a fake stack frame will be setup to have a return
// address set to SignalHandlerWorkerReturn during SIGSEGV handling.
// It enables the unwinder to unwind stack from the handling code to the actual failure site.
NESTED_ENTRY CallSignalHandlerWrapper\Alignment, _TEXT, NoHandler
    mflr %r0
	std %r0, 16(%r1)
    stdu %r1,-32(%r1)
    bl      EXTERNAL_C_FUNC(signal_handler_worker)
LOCAL_LABEL(SignalHandlerWorkerReturn\Alignment):
    addi %r1, %r1, 32
    ld %r0, 16(%r1)
    mtlr %r0
    blr
NESTED_END CallSignalHandlerWrapper\Alignment, _TEXT

.endm

CALL_SIGNAL_HANDLER_WRAPPER 0
CALL_SIGNAL_HANDLER_WRAPPER 8
