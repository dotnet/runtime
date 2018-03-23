// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix
#include "unixasmmacros.inc"

//extern FuncEvalHijackWorker:proc

// @dbgtodo- once we port Funceval, use the ExceptionHijack stub instead of this func-eval stub.
NESTED_ENTRY FuncEvalHijack, _TEXT, UnhandledExceptionHandlerUnix
#define STK_ALIGN_PADDING 12
        sub  esp, STK_ALIGN_PADDING
        push eax        // the ptr to the DebuggerEval
        CHECK_STACK_ALIGNMENT
        call C_FUNC(FuncEvalHijackWorker)
        add  esp, (4 + STK_ALIGN_PADDING)
        jmp  eax        // return is the patch addresss to jmp to
#undef STK_ALIGN_PADDING
NESTED_END FuncEvalHijack, _TEXT

//
// Flares for interop debugging.
// Flares are exceptions (breakpoints) at well known addresses which the RS
// listens for when interop debugging.
//

// This exception is from managed code.
LEAF_ENTRY SignalHijackStartedFlare, _TEXT
        int 3
        // make sure that the basic block is unique
        test eax,1
        ret
LEAF_END SignalHijackStartedFlare, _TEXT

// Start the handoff
LEAF_ENTRY ExceptionForRuntimeHandoffStartFlare, _TEXT
        int 3
        // make sure that the basic block is unique
        test eax,2
        ret
LEAF_END ExceptionForRuntimeHandoffStartFlare, _TEXT

// Finish the handoff.
LEAF_ENTRY ExceptionForRuntimeHandoffCompleteFlare, _TEXT
        int 3
        // make sure that the basic block is unique
        test eax,3
        ret
LEAF_END ExceptionForRuntimeHandoffCompleteFlare, _TEXT

// Signal execution return to unhijacked state
LEAF_ENTRY SignalHijackCompleteFlare, _TEXT
        int 3
        // make sure that the basic block is unique
        test eax,4
        ret
LEAF_END SignalHijackCompleteFlare, _TEXT

// This exception is from unmanaged code.
LEAF_ENTRY ExceptionNotForRuntimeFlare, _TEXT
        int 3
        // make sure that the basic block is unique
        test eax,5
        ret
LEAF_END ExceptionNotForRuntimeFlare, _TEXT

// The Runtime is synchronized.
LEAF_ENTRY NotifyRightSideOfSyncCompleteFlare, _TEXT
        int 3
        // make sure that the basic block is unique
        test eax,6
        ret
LEAF_END NotifyRightSideOfSyncCompleteFlare, _TEXT
