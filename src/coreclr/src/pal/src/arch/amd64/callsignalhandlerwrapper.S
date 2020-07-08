// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
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
    .cfi_def_cfa_offset (128 + 8 + \Alignment) // red zone + return address + alignment
    .cfi_offset rip, -(128 + 8 + \Alignment)
    push_nonvol_reg rbp
    call    EXTERNAL_C_FUNC(signal_handler_worker)
LOCAL_LABEL(SignalHandlerWorkerReturn\Alignment):
    pop rbp
    ret
NESTED_END CallSignalHandlerWrapper\Alignment, _TEXT

.endm

CALL_SIGNAL_HANDLER_WRAPPER 0
CALL_SIGNAL_HANDLER_WRAPPER 8
