// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "codegen.h"

// clang-format off
/*static*/ const BYTE CodeGenInterface::instInfo[] =
{
    #define INST(id, nm, info, fmt) info,
    #include "instrs.h"
};
// clang-format on

//------------------------------------------------------------------------
// Interpreter bytecode is generated directly from LIR (see codegeninterp.cpp),
// so the emitter does not run for this target. The definitions below satisfy
// the target hooks that the shared emitter code references.
//

void emitter::emitIns(instruction ins)
{
    unreached();
}

void emitter::emitIns_I(instruction ins, emitAttr attr, cnsval_ssize_t imm)
{
    unreached();
}

void emitter::emitIns_J(instruction ins, emitAttr attr, cnsval_ssize_t imm, BasicBlock* tgtBlock)
{
    unreached();
}

void emitter::emitIns_R(instruction ins, emitAttr attr, regNumber reg)
{
    unreached();
}

void emitter::emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, cnsval_ssize_t imm)
{
    unreached();
}

void emitter::emitIns_Mov(instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip)
{
    unreached();
}

void emitter::emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2)
{
    unreached();
}

void emitter::emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs)
{
    unreached();
}

void emitter::emitIns_Call(const EmitCallParams& params)
{
    unreached();
}

bool emitter::emitInsIsStore(instruction ins)
{
    return false;
}

emitter::insFormat emitter::emitInsFormat(instruction ins)
{
    unreached();
}

size_t emitter::emitSizeOfInsDsc(instrDesc* id) const
{
    unreached();
}

unsigned emitter::instrDesc::idCodeSize() const
{
    unreached();
}

void emitter::emitSetShortJump(instrDescJmp* id)
{
    unreached();
}

size_t emitter::emitOutputInstr(insGroup* ig, instrDesc* id, BYTE** dp)
{
    unreached();
}

#if defined(DEBUG) || defined(LATE_DISASM)
emitter::insExecutionCharacteristics emitter::getInsExecutionCharacteristics(instrDesc* id)
{
    unreached();
}
#endif // defined(DEBUG) || defined(LATE_DISASM)

void emitter::emitDispInst(instruction ins)
{
}

void emitter::emitDispIns(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* pCode, size_t sz, insGroup* ig)
{
}

#if defined(DEBUG)
void emitter::emitInsSanityCheck(instrDesc* id)
{
}
#endif // DEBUG
