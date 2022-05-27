// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Implementation of the PAL_DispatchExceptionWrapper that is
// interposed between a function that caused a hardware fault
// and PAL_DispatchException that throws an SEH exception for
// the fault, to make the stack unwindable.
//

#include "unixasmmacros.inc"

// Offset of the return address from the PAL_DispatchException in the PAL_DispatchExceptionWrapper
.globl C_FUNC(PAL_DispatchExceptionReturnOffset)
C_FUNC(PAL_DispatchExceptionReturnOffset):
    .int LOCAL_LABEL(PAL_DispatchExceptionReturn) - C_FUNC(PAL_DispatchExceptionWrapper)

//
// PAL_DispatchExceptionWrapper will never be called; it only serves
// to be referenced from a stack frame on the faulting thread.  Its
// unwinding behavior is equivalent to any standard function.
// It is analogous to the following source file.
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
    //                           $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED 22, 1, -16
    // Should never actually run
    EMIT_BREAKPOINT
    bl      EXTERNAL_C_FUNC(PAL_DispatchException)
LOCAL_LABEL(PAL_DispatchExceptionReturn):
    // Should never return
    EMIT_BREAKPOINT
    //                              $fp,$ra
    EPILOG_RESTORE_REG_PAIR_INDEXED 22, 1, 16
    jirl  $r0, $ra, 0
NESTED_END PAL_DispatchExceptionWrapper, _TEXT
