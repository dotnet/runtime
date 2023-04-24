// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

.macro CALL_SIGNAL_HANDLER_WRAPPER Alignment

.globl C_FUNC(SignalHandlerWorkerReturnOffset\Alignment)
C_FUNC(SignalHandlerWorkerReturnOffset\Alignment):
    .dword LOCAL_LABEL(SignalHandlerWorkerReturn\Alignment)-C_FUNC(CallSignalHandlerWrapper\Alignment)

// This function is never called, only a fake stack frame will be setup to have a return
// address set to SignalHandlerWorkerReturn during SIGSEGV handling.
// It enables the unwinder to unwind stack from the handling code to the actual failure site.
NESTED_ENTRY CallSignalHandlerWrapper\Alignment, _TEXT, NoHandler
__StackAllocationSize = (128 + 8 + 8 + \Alignment) // red zone + fp + ra + alignment
    PROLOG_STACK_ALLOC __StackAllocationSize
    .cfi_adjust_cfa_offset __StackAllocationSize

    PROLOG_SAVE_REG_PAIR fp, ra, 0

    call signal_handler_worker

LOCAL_LABEL(SignalHandlerWorkerReturn\Alignment):
    EPILOG_RESTORE_REG_PAIR fp, ra, 0
    EPILOG_STACK_FREE __StackAllocationSize
    .cfi_adjust_cfa_offset -__StackAllocationSize
    ret

NESTED_END CallSignalHandlerWrapper\Alignment, _TEXT

.endm

CALL_SIGNAL_HANDLER_WRAPPER 0
CALL_SIGNAL_HANDLER_WRAPPER 8
