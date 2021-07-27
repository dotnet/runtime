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
    .cfi_def_cfa 15, 160
    stmg    %r14, %r15, 112(%r15)
    .cfi_offset 14, -48
    .cfi_offset 15, -40
    aghi    %r15, -160
    .cfi_def_cfa_offset 320
    brasl   %r14,EXTERNAL_C_FUNC(signal_handler_worker)
LOCAL_LABEL(SignalHandlerWorkerReturn\Alignment):
    lmg     %r14, %r15, 272(%r11)
    .cfi_restore 15
    .cfi_restore 14
    .cfi_def_cfa 15, 160
    br      %r14
NESTED_END CallSignalHandlerWrapper\Alignment, _TEXT

.endm

CALL_SIGNAL_HANDLER_WRAPPER 0
