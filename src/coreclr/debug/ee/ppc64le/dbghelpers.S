// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "asmconstants.h"
#include "unixasmmacros.inc"

//
// Hijacking stub used for function evaluation.
// See Debugger::FuncEvalSetup() for details.
//
// On entry:
//   r3  : pointer to DebuggerEval object
//

NESTED_ENTRY FuncEvalHijack, _TEXT, UnhandledExceptionHandlerUnix
    // NOTE: FuncEvalHijackPersonalityRoutine depends on this stack layout
    
    // ELFv2 ABI compliant register saving
    PROLOG_SAVE_REG_PAIR_INDEXED r29, r30, -32   // Save frame pointer and link register
    std     %r2, -16(%r1)        // Save TOC with proper alignment
    std     %r3, 16(%r1)         // Store DebuggerEval pointer

    // Call worker function
    bl      C_FUNC(FuncEvalHijackWorker)
    ld      %r2, -16(%r1)        // Immediate TOC restore

    // Verify return address and branch
    cmpdi   %r3, 0              // Check for null return address
    beq     1f                 // Branch if null
    mtctr   %r3                 // Move return address to CTR
    EPILOG_STACK_FREE 32
    bctr                       // Branch to new instruction pointer
1:  EMIT_BREAKPOINT           // Handle null pointer case

NESTED_END FuncEvalHijack

NESTED_ENTRY ExceptionHijack, _TEXT, UnhandledExceptionHandlerUnix
    // Save TOC and make the call
    std     %r2, -16(%r1)        // Save TOC with proper alignment
    bl      C_FUNC(ExceptionHijackWorker)
    ld      %r2, -16(%r1)        // Immediate TOC restore

    // Critical for proper unwinding
    nop

    // *** Should never get here ***
    EMIT_BREAKPOINT

// Debug information label
PATCH_LABEL ExceptionHijackEnd
NESTED_END ExceptionHijack, _TEXT

