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

    .cfi_def_cfa_offset (4 + \Alignment) // return address + stack alignment
    .cfi_offset eip, -(4 + \Alignment)
    push ebp
    .cfi_adjust_cfa_offset 4
    .cfi_rel_offset ebp, 0
    mov ebp, esp
    .cfi_def_cfa_register ebp
    // Align stack
    sub  esp, 8
    // Simulate arguments pushing
    push eax
    push eax
    push eax
    push eax
    call    EXTERNAL_C_FUNC(signal_handler_worker)
LOCAL_LABEL(SignalHandlerWorkerReturn\Alignment):
    add esp, 4 * 4 + 8
    pop ebp
    ret

NESTED_END CallSignalHandlerWrapper\Alignment, _TEXT

.endm

CALL_SIGNAL_HANDLER_WRAPPER 0
CALL_SIGNAL_HANDLER_WRAPPER 4
CALL_SIGNAL_HANDLER_WRAPPER 8
CALL_SIGNAL_HANDLER_WRAPPER 12
