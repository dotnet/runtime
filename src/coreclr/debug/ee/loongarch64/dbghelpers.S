// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

//
// hijacking stub used to perform a func-eval, see Debugger::FuncEvalSetup() for use.
//
// on entry:
//   x0  : pointer to DebuggerEval object
//

// @dbgtodo- once we port Funceval, use the ExceptionHijack stub instead of this func-eval stub.
NESTED_ENTRY FuncEvalHijack, _TEXT, UnhandledExceptionHandlerUnix
    // NOTE: FuncEvalHijackPersonalityRoutine is dependent on the stack layout so if
    //       you change the prolog you will also need to update the personality routine.

    // push arg to the stack so our personality routine can find it
    // push lr to get good stacktrace in debugger
    //                            $fp,$ra
    PROLOG_SAVE_REG_PAIR_INDEXED  22, 1, -32
    st.d  $a0, $sp, 16

    // FuncEvalHijackWorker returns the address we should jump to.
    bl C_FUNC(FuncEvalHijackWorker)

    EPILOG_STACK_FREE  32
    EPILOG_BRANCH_REG  $v0
NESTED_END FuncEvalHijack

// This is the general purpose hijacking stub. The DacDbi Hijack primitive will
// set up the stack and then set the IP here, and so this just makes the call.
NESTED_ENTRY ExceptionHijack, _TEXT, UnhandledExceptionHandlerUnix
    // make the call
    bl C_FUNC(ExceptionHijackWorker)

    // effective NOP to terminate unwind  ???
    nop

    // *** should never get here ***
    EMIT_BREAKPOINT
// exported label so the debugger knows where the end of this function is
PATCH_LABEL ExceptionHijackEnd

NESTED_END ExceptionHijack, _TEXT
