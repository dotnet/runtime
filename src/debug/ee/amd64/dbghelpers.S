// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.intel_syntax noprefix
#include "unixasmmacros.inc"

//extern FuncEvalHijackWorker:proc

// @dbgtodo- once we port Funceval, use the ExceptionHijack stub instead of this func-eval stub.
NESTED_ENTRY FuncEvalHijack, _TEXT, UnhandledExceptionHandlerUnix
        // the stack should be aligned at this point, since we do not call this
        // function explicitly
        alloc_stack 0x20
        END_PROLOGUE

        mov     [rsp], rdi
        call    C_FUNC(FuncEvalHijackWorker)

        //
        // The following nop is crucial.  It is important that the OS *not* recognize
        // the instruction immediately following the call above as an epilog, if it
        // does recognize it as an epilogue, it unwinds this function itself rather
        // than calling our personality routine to do the unwind, and then stack
        // tracing is hosed.
        //
        nop

        //
        // epilogue
        //
        add     rsp, 20h
        TAILJMP_RAX
NESTED_END FuncEvalHijack, _TEXT

//extern ExceptionHijackWorker:proc

// This is the general purpose hijacking stub. The DacDbi Hijack primitive will 
// set up the stack and then set the IP here, and so this just makes the call.
NESTED_ENTRY ExceptionHijack, _TEXT, UnhandledExceptionHandlerUnix
        // the stack should be aligned at this point, since we do not call this
        // function explicitly
        // 
        // There is a problem here.  The Orcas assembler doesn't like a 0-sized stack frame.
        // So we allocate 4 stack slots as the outgoing argument home and just copy the 
        // arguments set up by DacDbi into these stack slots.  We will take a perf hit,
        // but this is not a perf critical code path anyway.

        // There is an additional dependency on this alloc_stack: the
        // ExceptionHijackPersonalityRoutine assumes that it can find
        // the first argument to HijackWorker in the stack frame of
        // ExceptionHijack, at an offset of exactly 0x20 bytes from
        // ExceptionHijackWorker's stack frame. Therefore it is
        // important that we move the stack pointer by the same amount.
        alloc_stack 0x20
        END_PROLOGUE

        // We used to do an "alloc_stack 0h" because the stack has been allocated for us
        // by the OOP hijacking routine.  Our arguments have also been pushed onto the 
        // stack for us.  However, the Orcas compilers don't like a 0-sized frame, so 
        // we need to allocate something here and then just copy the stack arguments to 
        // their new argument homes.

        // In x86, ExceptionHijackWorker is marked STDCALL, so it finds
        // its arguments on the stack. In x64, it gets its arguments in
        // registers (set up for us by DacDbiInterfaceImpl::Hijack),
        // and this stack space may be reused.
        mov     rax, [rsp + 20h]
        mov     [rsp], rax
        mov     rax, [rsp + 28h]
        mov     [rsp + 8h], rax
        mov     rax, [rsp + 30h]
        mov     [rsp + 10h], rax
        mov     rax, [rsp + 38h]
        mov     [rsp + 18h], rax
        
        // DD Hijack primitive already set the stack. So just make the call now.
        call    C_FUNC(ExceptionHijackWorker)

        //
        // The following nop is crucial.  It is important that the OS *not* recognize
        // the instruction immediately following the call above as an epilog, if it
        // does recognize it as an epilogue, it unwinds this function itself rather
        // than calling our personality routine to do the unwind, and then stack
        // tracing is hosed.
        //
        nop

        // *** Should never get here ***
        // Hijack should have restored itself first.
        int 3

        //
        // epilogue
        //
        add     rsp, 20h
        TAILJMP_RAX

// Put a label here to tell the debugger where the end of this function is.
PATCH_LABEL ExceptionHijackEnd

NESTED_END ExceptionHijack, _TEXT

//
// Flares for interop debugging.
// Flares are exceptions (breakpoints) at well known addresses which the RS
// listens for when interop debugging.
//

// This exception is from managed code.
LEAF_ENTRY SignalHijackStartedFlare, _TEXT
        int 3
        // make sure that the basic block is unique
        test rax,1
        ret
LEAF_END SignalHijackStartedFlare, _TEXT

// Start the handoff
LEAF_ENTRY ExceptionForRuntimeHandoffStartFlare, _TEXT
        int 3
        // make sure that the basic block is unique
        test rax,2
        ret
LEAF_END ExceptionForRuntimeHandoffStartFlare, _TEXT

// Finish the handoff.
LEAF_ENTRY ExceptionForRuntimeHandoffCompleteFlare, _TEXT
        int 3
        // make sure that the basic block is unique
        test rax,3
        ret
LEAF_END ExceptionForRuntimeHandoffCompleteFlare, _TEXT

// Signal execution return to unhijacked state
LEAF_ENTRY SignalHijackCompleteFlare, _TEXT
        int 3
        // make sure that the basic block is unique
        test rax,4
        ret
LEAF_END SignalHijackCompleteFlare, _TEXT

// This exception is from unmanaged code.
LEAF_ENTRY ExceptionNotForRuntimeFlare, _TEXT
        int 3
        // make sure that the basic block is unique
        test rax,5
        ret
LEAF_END ExceptionNotForRuntimeFlare, _TEXT

// The Runtime is synchronized.
LEAF_ENTRY NotifyRightSideOfSyncCompleteFlare, _TEXT
        int 3
        // make sure that the basic block is unique
        test rax,6
        ret
LEAF_END NotifyRightSideOfSyncCompleteFlare, _TEXT
