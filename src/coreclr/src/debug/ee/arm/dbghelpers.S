// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "unixasmmacros.inc"

.syntax unified
.thumb

//
// hijacking stub used to perform a func-eval, see Debugger::FuncEvalSetup() for use.
//
// on entry:
//   r0  : pointer to DebuggerEval object
//

NESTED_ENTRY FuncEvalHijack, _TEXT, UnhandledExceptionHandlerUnix

    // push arg to the stack so our personality routine can find it
    // push lr to get good stacktrace in debugger
    push {r0,lr}

    CHECK_STACK_ALIGNMENT

    // FuncEvalHijackWorker returns the address we should jump to.
    bl      C_FUNC(FuncEvalHijackWorker)

    // effective NOP to terminate unwind
    mov r2, r2

    free_stack 8
    bx r0

NESTED_END FuncEvalHijack, _TEXT

//
// This is the general purpose hijacking stub. DacDbiInterfaceImpl::Hijack() will
// set the registers with the appropriate parameters from out-of-process.
//
// on entry:
//   r0 : pointer to CONTEXT
//   r1 : pointer to EXCEPTION_RECORD
//   r2 : EHijackReason
//   r3 : void* pdata
//

NESTED_ENTRY ExceptionHijack, _TEXT, UnhandledExceptionHandlerUnix

    CHECK_STACK_ALIGNMENT

    // make the call
    bl C_FUNC(ExceptionHijackWorker)

    // effective NOP to terminate unwind
    mov r3, r3

    // *** should never get here ***
    EMIT_BREAKPOINT

NESTED_END ExceptionHijackEnd, _TEXT
