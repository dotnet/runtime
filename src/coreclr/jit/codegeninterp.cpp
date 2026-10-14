// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "codegen.h"

//------------------------------------------------------------------------
// genInterpCode: Generate interpreter bytecode for the method from rationalized LIR.
//
// Arguments:
//    methodCodePtr  - [out] the published method code
//    methodCodeSize - [out] its size in bytes
//
void Compiler::genInterpCode(void** methodCodePtr, uint32_t* methodCodeSize)
{
    NYI_INTERP("Interpreter bytecode generation");
}

//------------------------------------------------------------------------
// The native code generator does not run for the interpreter target (see
// Compiler::compCompile). The definitions below satisfy the target hooks that
// the shared codegen code references.
//

void CodeGen::genMarkLabelsForCodegen()
{
    unreached();
}

void CodeGen::genBeginFnProlog()
{
    unreached();
}

void CodeGen::genFnEpilog(BasicBlock* block)
{
    unreached();
}

void CodeGen::genFuncletProlog(BasicBlock* block)
{
    unreached();
}

void CodeGen::genFuncletEpilog(BasicBlock* block)
{
    unreached();
}

void CodeGen::genEmitStartBlock(BasicBlock* block)
{
    unreached();
}

void CodeGen::genCodeForTreeNode(GenTree* treeNode)
{
    unreached();
}

void CodeGen::genCallFinally(BasicBlock* block)
{
    unreached();
}

void CodeGen::genEHCatchRet(BasicBlock* block)
{
    unreached();
}

void CodeGen::genIntToIntCast(GenTreeCast* cast)
{
    unreached();
}

void CodeGen::genFloatToFloatCast(GenTree* treeNode)
{
    unreached();
}

void CodeGen::genFloatToIntCast(GenTree* treeNode)
{
    unreached();
}

void CodeGen::genIntToFloatCast(GenTree* treeNode)
{
    unreached();
}

void CodeGen::genLoadLocalIntoReg(regNumber targetReg, unsigned lclNum)
{
    unreached();
}

void CodeGen::genStructReturn(GenTree* treeNode)
{
    unreached();
}

void CodeGen::genEmitGSCookieCheck(bool tailCall)
{
    unreached();
}

void CodeGen::genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize, regNumber callTarget)
{
    unreached();
}

void CodeGen::genProfilingLeaveCallback(unsigned helper)
{
    unreached();
}

void CodeGen::inst_JMP(emitJumpKind jmp, BasicBlock* tgtBlock)
{
    unreached();
}

void CodeGen::instGen_Set_Reg_To_Zero(emitAttr size, regNumber reg, insFlags flags)
{
    unreached();
}

void CodeGen::genReportEH()
{
    unreached();
}

void CodeGen::genCreateAndStoreGCInfo(unsigned            codeSize,
                                      unsigned            prologSize,
                                      unsigned epilogSize DEBUGARG(void* codePtr))
{
    unreached();
}

void RegSet::verifyRegUsed(regNumber reg)
{
}

// Interpreter frames are laid out by the interpreter, so there is no native frame to measure.

int CodeGenInterface::genTotalFrameSize() const
{
    return 0;
}

int CodeGenInterface::genSPtoFPdelta() const
{
    return 0;
}

int CodeGenInterface::genCallerSPtoFPdelta() const
{
    return 0;
}

int CodeGenInterface::genCallerSPtoInitialSPdelta() const
{
    return 0;
}
