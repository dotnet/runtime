// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//

// ==--==
//
// Implementation of the PAL_DispatchExceptionWrapper that is
// interposed between a function that caused a hardware fault
// and PAL_DispatchException that throws an SEH exception for
// the fault, to make the stack unwindable.
//

.intel_syntax noprefix
#include "unixasmmacros.inc"

#if defined(__x86_64__)
#define PAL_DISPATCHEXCEPTION __Z21PAL_DispatchExceptionmmmmmmP8_CONTEXTP17_EXCEPTION_RECORDP11MachMessage
#else //!defined(_AMD64_)
#define PAL_DISPATCHEXCEPTION __Z21PAL_DispatchExceptionP8_CONTEXTP17_EXCEPTION_RECORDP11MachMessage
#endif // defined(_AMD64_)

// Offset of the return address from the PAL_DISPATCHEXCEPTION in the PAL_DispatchExceptionWrapper
.globl C_FUNC(PAL_DispatchExceptionReturnOffset)
C_FUNC(PAL_DispatchExceptionReturnOffset):
    .int LOCAL_LABEL(PAL_DispatchExceptionReturn)-C_FUNC(PAL_DispatchExceptionWrapper)

//
// PAL_DispatchExceptionWrapper will never be called; it only serves
// to be referenced from a stack frame on the faulting thread.  Its
// unwinding behavior is equivalent to any standard function having
// an ebp frame. It is analogous to the following source file.
// 
// void PAL_DispatchException(CONTEXT *pContext, EXCEPTION_RECORD *pExceptionRecord, MachMessage *pMessage);
// 
// extern "C" void PAL_DispatchExceptionWrapper()
// {
//     CONTEXT Context;
//     EXCEPTION_RECORD ExceptionRecord;
//     MachMessage Message;
//     PAL_DispatchException(&Context, &ExceptionRecord, &Message);
// }
//

NESTED_ENTRY PAL_DispatchExceptionWrapper, _TEXT, NoHandler
    push_nonvol_reg rbp
    mov     rbp, rsp
    set_cfa_register rbp, (2*8)
    int3
    call    PAL_DISPATCHEXCEPTION
LOCAL_LABEL(PAL_DispatchExceptionReturn):
    int3
    pop_nonvol_reg rbp
    ret
NESTED_END PAL_DispatchExceptionWrapper, _TEXT

