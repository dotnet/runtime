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
    bool isFunclet  = (func->funKind != FUNC_ROOT);
    bool isColdCode = false;

    eeReserveUnwindInfo(isFunclet, isColdCode, sizeof(UNWIND_INFO));
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
//    func      - The main function or funclet to reserve unwind info for.
//    pHotCode  - Pointer to the beginning of the memory with the function and funclet hot  code.
//    pColdCode - Pointer to the beginning of the memory with the function and funclet cold code.
//
void Compiler::unwindEmitFunc(FuncInfoDsc* func, void* pHotCode, void* pColdCode)
{
    UNATIVE_OFFSET startOffset;
    UNATIVE_OFFSET endOffset;

    BasicBlock* const firstBlock = func->GetStartBlock(this);
    emitLocation      startLoc(ehEmitCookie(firstBlock));

    if (startLoc.GetIG() == nullptr)
    {
        startOffset = 0;
    }
    else
    {
        startOffset = startLoc.CodeOffset(GetEmitter());
    }

    BasicBlock* const blockAfterLast = func->GetLastBlock(this)->Next();

    if (blockAfterLast == nullptr)
    {
        endOffset = info.compNativeCodeSize;
    }
    else
    {
        emitLocation endLoc(ehEmitCookie(blockAfterLast));
        endOffset = endLoc.CodeOffset(GetEmitter());
    }

    // Wasm does not have cold code.
    //
    pColdCode = nullptr;

    UNWIND_INFO unwindInfo;
    unwindInfo.FrameSize = func->funWasmFrameSize;

    eeAllocUnwindInfo((BYTE*)pHotCode, (BYTE*)pColdCode, startOffset, endOffset, sizeof(UNWIND_INFO),
                      (BYTE*)&unwindInfo, (CorJitFuncKind)func->funKind);
}
