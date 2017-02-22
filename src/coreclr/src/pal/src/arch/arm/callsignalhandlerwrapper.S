// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "unixasmmacros.inc"
#include "asmconstants.h"

.syntax unified
.thumb

.macro CALL_SIGNAL_HANDLER_WRAPPER Alignment

.globl C_FUNC(SignalHandlerWorkerReturnOffset\Alignment)
C_FUNC(SignalHandlerWorkerReturnOffset\Alignment):
    .int LOCAL_LABEL(SignalHandlerWorkerReturn\Alignment)-C_FUNC(CallSignalHandlerWrapper\Alignment)

// This function is never called, only a fake stack frame will be setup to have a return
// address set to SignalHandlerWorkerReturn during SIGSEGV handling.
// It enables the unwinder to unwind stack from the handling code to the actual failure site.
NESTED_ENTRY CallSignalHandlerWrapper\Alignment, _TEXT, NoHandler
    sub     sp, sp, #(8 + \Alignment) // red zone + alignment
    stmfd   sp!, {r7, lr}
    bl      EXTERNAL_C_FUNC(signal_handler_worker)
LOCAL_LABEL(SignalHandlerWorkerReturn\Alignment):
    ldmfd   sp!, {r7, lr}
    bx      lr
NESTED_END CallSignalHandlerWrapper\Alignment, _TEXT

.endm

CALL_SIGNAL_HANDLER_WRAPPER 0
CALL_SIGNAL_HANDLER_WRAPPER 4
