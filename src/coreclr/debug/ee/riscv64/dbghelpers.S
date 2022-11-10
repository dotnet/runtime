// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

// #error "TODO-RISCV64: missing implementation"

//
// hijacking stub used to perform a func-eval, see Debugger::FuncEvalSetup() for use.
//

// @dbgtodo- once we port Funceval, use the ExceptionHijack stub instead of this func-eval stub.
NESTED_ENTRY FuncEvalHijack, _TEXT, UnhandledExceptionHandlerUnix
// TODO RISCV64 NYI
  sw ra, 0(zero)
NESTED_END FuncEvalHijack

// This is the general purpose hijacking stub. The DacDbi Hijack primitive will
// set up the stack and then set the IP here, and so this just makes the call.
NESTED_ENTRY ExceptionHijack, _TEXT, UnhandledExceptionHandlerUnix
// TODO RISCV64 NYI
  sw ra, 0(zero)
NESTED_END ExceptionHijack, _TEXT
