// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef TARGET_WASM
#error "This should be included only for Wasm"
#endif // TARGET_WASM

void Compiler::unwindBegProlog()
{
}

void Compiler::unwindEndProlog()
{
}

//------------------------------------------------------------------------
// Compiler::unwindAllocStack: account for stack pointer movement in the prolog.
//
// Arguments:
//    size - The amount of stack space to allocate.
//
void Compiler::unwindAllocStack(unsigned size)
{
    FuncInfoDsc* const func = funCurrentFunc();
    assert(func != nullptr);
    func->funWasmFrameSize += size;
}

//------------------------------------------------------------------------
// Compiler::unwindReserveFunc: Reserve the unwind information from the VM for a
// given main function or funclet.
//
// Arguments:
//    func - The main function or funclet to reserve unwind info for.
//
void Compiler::unwindReserve()
{
    assert(!compGeneratingProlog);
    assert(!compGeneratingEpilog);

    for (FuncInfoDsc* const func : Funcs())
    {
        unwindReserveFunc(func);
    }
}

//------------------------------------------------------------------------
// Compiler::unwindReserveFunc: Reserve the unwind information from the VM for
//  the main function or funclet.
//
// Arguments:
//    func      - The main function or funclet to reserve unwind info for.
//
void Compiler::unwindReserveFunc(FuncInfoDsc* func)
{
    bool  isFunclet   = func->IsFunclet();
    bool  isColdCode  = false;
    ULONG encodedSize = emitter::SizeOfULEB128(func->funWasmFrameSize)
                      + emitter::SizeOfULEB128(func->funWasmVirtualIPCount);

    eeReserveUnwindInfo(isFunclet, isColdCode, encodedSize);
}

//------------------------------------------------------------------------
// Compiler::unwindEmit: Report all the unwind information to the VM.
//
// Arguments:
//    pHotCode  - Pointer to the beginning of the memory with the function and funclet hot  code.
//    pColdCode - Pointer to the beginning of the memory with the function and funclet cold code.
//
void Compiler::unwindEmit(void* pHotCode, void* pColdCode)
{
    assert(!compGeneratingProlog);
    assert(!compGeneratingEpilog);

    for (FuncInfoDsc* const func : Funcs())
    {
        unwindEmitFunc(func, pHotCode, pColdCode);
    }
}

//------------------------------------------------------------------------
// Compiler::unwindEmitFunc: Report the unwind information to the VM for
//    the main function or funclet.
//
// Arguments:
//    func      - The main function or funclet to report unwind info for.
//    pHotCode  - Pointer to the beginning of the memory with the function and funclet hot  code.
//    pColdCode - Pointer to the beginning of the memory with the function and funclet cold code.
//
// Notes:
//   For Wasm the unwind extent describes the entire span of Wasm code for the method or funclet.
//
void Compiler::unwindEmitFunc(FuncInfoDsc* func, void* pHotCode, void* pColdCode)
{
    UNATIVE_OFFSET startOffset = func->startLoc->CodeOffset(GetEmitter());
    UNATIVE_OFFSET endOffset   = func->endLoc->CodeOffset(GetEmitter());

    // Wasm does not have cold code.
    //
    pColdCode = nullptr;

    // Unwind info is the frame size followed by the virtual IP count,
    // both encoded as ULEB128.
    //
    uint8_t buffer[10];
    ULONG   encodedSize = (ULONG)GetEmitter()->emitOutputULEB128(buffer, func->funWasmFrameSize);
    encodedSize += (ULONG)GetEmitter()->emitOutputULEB128(buffer + encodedSize, func->funWasmVirtualIPCount);
    // WASM-TODO, sum total of virtual IP size should be recorded into the GC info for the method as well.
    assert(encodedSize <= sizeof(buffer));

    eeAllocUnwindInfo((BYTE*)pHotCode, (BYTE*)pColdCode, startOffset, endOffset, encodedSize, (BYTE*)&buffer,
                      (CorJitFuncKind)func->funKind);
}
