// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

NESTED_ENTRY FuncEvalHijack, _TEXT, UnhandledExceptionHandlerUnix
// NOT YET IMPLEMENTED
.word 0x0001
NESTED_END FuncEvalHijack, _TEXT

// This is the general purpose hijacking stub. The DacDbi Hijack primitive will
// set up the stack and then set the IP here, and so this just makes the call.
NESTED_ENTRY ExceptionHijack, _TEXT, UnhandledExceptionHandlerUnix
// NOT YET IMPLEMENTED
.word 0x0001

// Put a label here to tell the debugger where the end of this function is.
PATCH_LABEL ExceptionHijackEnd

NESTED_END ExceptionHijack, _TEXT

