// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Implementation of the PAL_DispatchExceptionWrapper that is
// interposed between a function that caused a hardware fault
// and PAL_DispatchException that throws an SEH exception for
// the fault, to make the stack unwindable.
//

.intel_syntax noprefix
#include "unixasmmacros.inc"

// Offset of the return address from the PAL_DispatchException in the PAL_DispatchExceptionWrapper
.globl C_FUNC(PAL_DispatchExceptionReturnOffset)
C_FUNC(PAL_DispatchExceptionReturnOffset):
    .int LOCAL_LABEL(PAL_DispatchExceptionReturn) - C_FUNC(PAL_DispatchExceptionWrapper)

//
// PAL_DispatchExceptionWrapper will never be called; it only serves
// to be referenced from a stack frame on the faulting thread.  Its
// unwinding behavior is equivalent to any standard function having
// an ebp frame. It is analogous to the following source file.
//
// extern "C" void PAL_DispatchException(CONTEXT *pContext, EXCEPTION_RECORD *pExceptionRecord, MachExceptionInfo *pMachExceptionInfo);
//
// extern "C" void PAL_DispatchExceptionWrapper()
// {
//     CONTEXT Context;
//     EXCEPTION_RECORD ExceptionRecord;
//     MachExceptionInfo MachExceptionInfo;
//     PAL_DispatchException(&Context, &ExceptionRecord, &MachExceptionInfo);
// }
//

NESTED_ENTRY PAL_DispatchExceptionWrapper, _TEXT, NoHandler
    push_nonvol_reg rbp
    mov     rbp, rsp
    set_cfa_register rbp, (2*8)
    int3
    call    C_FUNC(PAL_DispatchException)
LOCAL_LABEL(PAL_DispatchExceptionReturn):
    int3
    pop_nonvol_reg rbp
    ret
NESTED_END PAL_DispatchExceptionWrapper, _TEXT
