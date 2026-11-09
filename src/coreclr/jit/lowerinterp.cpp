// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                     Lowering for the interpreter target                   XX
XX                                                                           XX
XX  Interpreter instructions operate on stack slots and have no addressing   XX
XX  modes or immediate forms to contain into, so these hooks leave the IR    XX
XX  unchanged. Bytecode is currently generated straight from rationalized    XX
XX  LIR (see codegeninterp.cpp), so the lowering phase does not run yet.     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "lower.h"

bool Lowering::IsCallTargetInRange(void* addr)
{
    return true;
}

bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode) const
{
    return false;
}

GenTree* Lowering::LowerStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    return storeLoc->gtNext;
}

GenTree* Lowering::LowerStoreIndir(GenTreeStoreInd* node)
{
    return node->gtNext;
}

GenTree* Lowering::LowerMul(GenTreeOp* mul)
{
    return mul->gtNext;
}

GenTree* Lowering::LowerBinaryArithmetic(GenTreeOp* binOp)
{
    return binOp->gtNext;
}

void Lowering::LowerCast(GenTree* tree)
{
}

void Lowering::LowerRotate(GenTree* tree)
{
}

void Lowering::LowerPutArgStk(GenTreePutArgStk* putArgNode)
{
}

void Lowering::ContainBlockStoreAddress(GenTreeBlk* blkNode, unsigned size, GenTree* addr, GenTree* addrParent)
{
}

void Lowering::ContainCheckCallOperands(GenTreeCall* call)
{
}

void Lowering::ContainCheckNonLocalJmp(GenTreeUnOp* node)
{
}

void Lowering::ContainCheckStoreIndir(GenTreeStoreInd* node)
{
}

void Lowering::ContainCheckIndir(GenTreeIndir* indirNode)
{
}

void Lowering::ContainCheckBinary(GenTreeOp* node)
{
}

void Lowering::ContainCheckMul(GenTreeOp* node)
{
}

void Lowering::ContainCheckDivOrMod(GenTreeOp* node)
{
}

void Lowering::ContainCheckShiftRotate(GenTreeOp* node)
{
}

void Lowering::ContainCheckStoreLoc(GenTreeLclVarCommon* storeLoc) const
{
}

void Lowering::ContainCheckCast(GenTreeCast* node)
{
}

void Lowering::ContainCheckCompare(GenTreeOp* cmp)
{
}

void Lowering::ContainCheckSelect(GenTreeOp* node)
{
}
