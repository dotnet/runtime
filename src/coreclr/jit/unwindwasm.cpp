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
    assert(!GetEmitter()->emitGeneratingPrologOrFuncletProlog());
    assert(!GetEmitter()->emitGeneratingEpilogOrFuncletEpilog());

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
    bool isFunclet  = func->IsFunclet();
    bool isColdCode = false;
    assert(func->endVirtualIP > func->startVirtualIP);
    ULONG encodedSize = emitter::SizeOfULEB128(func->funWasmFrameSize) +
                        emitter::SizeOfULEB128(func->endVirtualIP - func->startVirtualIP);

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
    assert(!GetEmitter()->emitGeneratingPrologOrFuncletProlog());
    assert(!GetEmitter()->emitGeneratingEpilogOrFuncletEpilog());

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
//   For Wasm the unwind extent describes the entire span of Wasm code for the method or funclet,
//   and the virtual IP "length" for the method or funclet.
//
void Compiler::unwindEmitFunc(FuncInfoDsc* func, void* pHotCode, void* pColdCode)
{
    UNATIVE_OFFSET startOffset = func->startLoc->CodeOffset(GetEmitter());
    UNATIVE_OFFSET endOffset   = func->endLoc->CodeOffset(GetEmitter());

    // Wasm does not have cold code.
    //
    pColdCode = nullptr;

    // Unwind info is the frame size and the virtual IP length.
    // All values encoded via ULEB128.
    //
    uint8_t buffer[10];
    size_t  index = 0;
    assert(func->endVirtualIP > func->startVirtualIP);
    index += GetEmitter()->emitOutputULEB128(buffer + index, func->funWasmFrameSize);
    index += GetEmitter()->emitOutputULEB128(buffer + index, func->endVirtualIP - func->startVirtualIP);
    assert(index <= sizeof(buffer));

    JITDUMP("Unwind info for %s %u: VIP range [%u, %u); frame size %u\n", func->IsFunclet() ? "funclet" : "main",
            func->GetFuncletIdx(this), func->startVirtualIP, func->endVirtualIP, func->funWasmFrameSize);

    eeAllocUnwindInfo((BYTE*)pHotCode, (BYTE*)pColdCode, startOffset, endOffset, (ULONG)index, (BYTE*)&buffer,
                      (CorJitFuncKind)func->funKind);
}
