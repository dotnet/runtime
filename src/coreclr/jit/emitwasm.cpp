// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// clang-format off
/*static*/ const BYTE CodeGenInterface::instInfo[] =
{
    #define INST(id, nm, info, opcode) info,
    #include "instrs.h"
};
// clang-format on

void emitter::emitIns(instruction ins)
{
    NYI_WASM("emitIns");
}

void emitter::emitIns_I(instruction ins, emitAttr attr, ssize_t imm)
{
    NYI_WASM("emitIns_I");
}

void emitter::emitIns_R(instruction ins, emitAttr attr, regNumber reg)
{
    NYI_WASM("emitIns_R");
}

void emitter::emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t imm)
{
    NYI_WASM("emitIns_R_I");
}

void emitter::emitIns_Mov(instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip)
{
    NYI_WASM("emitIns_Mov");
}

void emitter::emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2)
{
    NYI_WASM("emitIns_R_R");
}

void emitter::emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs)
{
    NYI_WASM("emitIns_S_R");
}

bool emitter::emitInsIsStore(instruction ins)
{
    NYI_WASM("emitInsIsStore");
    return false;
}

size_t emitter::emitSizeOfInsDsc(instrDesc* id) const
{
    NYI_WASM("emitSizeOfInsDsc"); // Note this should return the size of the "id" structure itself.
    return 0;
}

void emitter::emitSetShortJump(instrDescJmp* id)
{
    NYI_WASM("emitSetShortJump");
}

size_t emitter::emitOutputInstr(insGroup* ig, instrDesc* id, BYTE** dp)
{
    NYI_WASM("emitOutputInstr");
    return 0;
}

void emitter::emitDispIns(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* pCode, size_t sz, insGroup* ig)
{
    NYI_WASM("emitDispIns");
}

#if defined(DEBUG) || defined(LATE_DISASM)
emitter::insExecutionCharacteristics emitter::getInsExecutionCharacteristics(instrDesc* id)
{
    NYI_WASM("getInsSveExecutionCharacteristics");
    return {};
}
#endif // defined(DEBUG) || defined(LATE_DISASM)

#ifdef DEBUG
void emitter::emitInsSanityCheck(instrDesc* id)
{
    NYI_WASM("emitInsSanityCheck");
}
#endif // DEBUG
