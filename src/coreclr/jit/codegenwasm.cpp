// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "codegen.h"

void CodeGen::genMarkLabelsForCodegen()
{
    // TODO-WASM: serialize fgWasmControlFlow results into codegen-level metadata/labels
    // (or use them directly and leave this empty).
}

void CodeGen::genFnProlog()
{
    NYI_WASM("Uncomment CodeGen::genFnProlog and proceed from there");
}

void CodeGen::genFnEpilog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFnEpilog()\n");
    }
#endif // DEBUG

    NYI_WASM("genFnEpilog");
}

void CodeGen::genCaptureFuncletPrologEpilogInfo()
{
}

void CodeGen::genFuncletProlog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletProlog()\n");
    }
#endif

    NYI_WASM("genFuncletProlog");
}

void CodeGen::genFuncletEpilog()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletEpilog()\n");
    }
#endif

    NYI_WASM("genFuncletEpilog");
}

void CodeGen::genCodeForTreeNode(GenTree* node)
{
    NYI_WASM("genCodeForTreeNode");
}

BasicBlock* CodeGen::genCallFinally(BasicBlock* block)
{
    assert(block->KindIs(BBJ_CALLFINALLY));
    NYI_WASM("genCallFinally");
    return nullptr;
}

void CodeGen::genEHCatchRet(BasicBlock* block)
{
    NYI_WASM("genEHCatchRet");
}

void CodeGen::genEmitGSCookieCheck(bool tailCall)
{
    // TODO-WASM: GS cookie checks have limited utility on WASM since they can only help
    // with detecting linear memory stack corruption. Decide if we want them anyway.
    NYI_WASM("genEmitGSCookieCheck");
}

void CodeGen::genSpillVar(GenTree* tree)
{
    NYI_WASM("Put all spillng to memory under '#if HAS_FIXED_REGISTER_SET'");
}

//------------------------------------------------------------------------
// inst_JMP: Generate a jump instruction.
//
void CodeGen::inst_JMP(emitJumpKind jmp, BasicBlock* tgtBlock)
{
    NYI_WASM("inst_JMP");
}

void CodeGen::genCreateAndStoreGCInfo(unsigned codeSize, unsigned prologSize, unsigned epilogSize DEBUGARG(void* code))
{
    // GCInfo not captured/created by codegen.
}

void CodeGen::genReportEH()
{
    // EHInfo not captured/created by codegen.
}

//---------------------------------------------------------------------
// genTotalFrameSize - return the total size of the linear memory stack frame.
//
// Return value:
//    Total linear memory frame size
//
int CodeGenInterface::genTotalFrameSize() const
{
    assert(compiler->compLclFrameSize >= 0);
    return compiler->compLclFrameSize;
}

//---------------------------------------------------------------------
// genSPtoFPdelta - return the offset from SP to the frame pointer.
// This number is going to be positive, since SP must be at the lowest
// address.
//
// There must be a frame pointer to call this function!
int CodeGenInterface::genSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    NYI_WASM("genSPtoFPdelta");
    return 0;
}

//---------------------------------------------------------------------
// genCallerSPtoFPdelta - return the offset from Caller-SP to the frame pointer.
// This number is going to be negative, since the Caller-SP is at a higher
// address than the frame pointer.
//
// There must be a frame pointer to call this function!
int CodeGenInterface::genCallerSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    NYI_WASM("genCallerSPtoFPdelta");
    return 0;
}

//---------------------------------------------------------------------
// genCallerSPtoInitialSPdelta - return the offset from Caller-SP to Initial SP.
//
// This number will be negative.
int CodeGenInterface::genCallerSPtoInitialSPdelta() const
{
    NYI_WASM("genCallerSPtoInitialSPdelta");
    return 0;
}

void CodeGenInterface::genUpdateVarReg(LclVarDsc* varDsc, GenTree* tree, int regIndex)
{
    NYI_WASM("Move genUpdateVarReg from codegenlinear.cpp to codegencommon.cpp shared code");
}

void CodeGenInterface::genUpdateVarReg(LclVarDsc* varDsc, GenTree* tree)
{
    NYI_WASM("Move genUpdateVarReg from codegenlinear.cpp to codegencommon.cpp shared code");
}

void RegSet::verifyRegUsed(regNumber reg)
{
}
