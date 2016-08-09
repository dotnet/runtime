// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This file contains the members of CodeGen that are defined and used
// only by the RyuJIT backend.  It is included by CodeGen.h in the
// definition of the CodeGen class.
//

#ifndef LEGACY_BACKEND // Not necessary (it's this way in the #include location), but helpful to IntelliSense

void genSetRegToConst(regNumber targetReg, var_types targetType, GenTreePtr tree);

void genCodeForTreeNode(GenTreePtr treeNode);

void genCodeForBinary(GenTreePtr treeNode);

void genCodeForDivMod(GenTreeOp* treeNode);

void genCodeForMulHi(GenTreeOp* treeNode);

void genLeaInstruction(GenTreeAddrMode* lea);

void genSetRegToCond(regNumber dstReg, GenTreePtr tree);

void genIntToIntCast(GenTreePtr treeNode);

void genFloatToFloatCast(GenTreePtr treeNode);

void genFloatToIntCast(GenTreePtr treeNode);

void genIntToFloatCast(GenTreePtr treeNode);

void genCkfinite(GenTreePtr treeNode);

void genIntrinsic(GenTreePtr treeNode);

void genPutArgStk(GenTreePtr treeNode);
unsigned getBaseVarForPutArgStk(GenTreePtr treeNode);

#if defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_)
unsigned getFirstArgWithStackSlot();
#endif // _TARGET_XARCH_ || _TARGET_ARM64_

void genCompareFloat(GenTreePtr treeNode);

void genCompareInt(GenTreePtr treeNode);

#if !defined(_TARGET_64BIT_)
void genCompareLong(GenTreePtr treeNode);
void genJTrueLong(GenTreePtr treeNode);
#endif

#ifdef FEATURE_SIMD
enum SIMDScalarMoveType
{
    SMT_ZeroInitUpper,                  // zero initlaize target upper bits
    SMT_ZeroInitUpper_SrcHasUpperZeros, // zero initialize target upper bits; source upper bits are known to be zero
    SMT_PreserveUpper                   // preserve target upper bits
};

instruction getOpForSIMDIntrinsic(SIMDIntrinsicID intrinsicId, var_types baseType, unsigned* ival = nullptr);
void genSIMDScalarMove(var_types type, regNumber target, regNumber src, SIMDScalarMoveType moveType);
void genSIMDZero(var_types targetType, var_types baseType, regNumber targetReg);
void genSIMDIntrinsicInit(GenTreeSIMD* simdNode);
void genSIMDIntrinsicInitN(GenTreeSIMD* simdNode);
void genSIMDIntrinsicInitArray(GenTreeSIMD* simdNode);
void genSIMDIntrinsicUnOp(GenTreeSIMD* simdNode);
void genSIMDIntrinsicBinOp(GenTreeSIMD* simdNode);
void genSIMDIntrinsicRelOp(GenTreeSIMD* simdNode);
void genSIMDIntrinsicDotProduct(GenTreeSIMD* simdNode);
void genSIMDIntrinsicSetItem(GenTreeSIMD* simdNode);
void genSIMDIntrinsicGetItem(GenTreeSIMD* simdNode);
void genSIMDIntrinsicShuffleSSE2(GenTreeSIMD* simdNode);
void genSIMDIntrinsicUpperSave(GenTreeSIMD* simdNode);
void genSIMDIntrinsicUpperRestore(GenTreeSIMD* simdNode);

void genSIMDIntrinsic(GenTreeSIMD* simdNode);
void genSIMDCheck(GenTree* treeNode);

// TYP_SIMD12 (i.e Vector3 of size 12 bytes) is not a hardware supported size and requires
// two reads/writes on 64-bit targets. These routines abstract reading/writing of Vector3
// values through an indirection. Note that Vector3 locals allocated on stack would have
// their size rounded to TARGET_POINTER_SIZE (which is 8 bytes on 64-bit targets) and hence
// Vector3 locals could be treated as TYP_SIMD16 while reading/writing.
void genStoreIndTypeSIMD12(GenTree* treeNode);
void genStoreLclFldTypeSIMD12(GenTree* treeNode);
void genLoadIndTypeSIMD12(GenTree* treeNode);
void genLoadLclFldTypeSIMD12(GenTree* treeNode);
#endif // FEATURE_SIMD

#if !defined(_TARGET_64BIT_)

// CodeGen for Long Ints

void genStoreLongLclVar(GenTree* treeNode);

#endif // !defined(_TARGET_64BIT_)

void genProduceReg(GenTree* tree);

void genUnspillRegIfNeeded(GenTree* tree);

regNumber genConsumeReg(GenTree* tree);

void genConsumeRegAndCopy(GenTree* tree, regNumber needReg);

void genConsumeIfReg(GenTreePtr tree)
{
    if (!tree->isContained())
    {
        (void)genConsumeReg(tree);
    }
}

void genRegCopy(GenTreePtr tree);

void genTransferRegGCState(regNumber dst, regNumber src);

void genConsumeAddress(GenTree* addr);

void genConsumeAddrMode(GenTreeAddrMode* mode);

void genConsumeBlockOp(GenTreeBlkOp* blkNode, regNumber dstReg, regNumber srcReg, regNumber sizeReg);

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
void genConsumePutStructArgStk(
    GenTreePutArgStk* putArgStkNode, regNumber dstReg, regNumber srcReg, regNumber sizeReg, unsigned baseVarNum);
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

void genConsumeRegs(GenTree* tree);

void genConsumeOperands(GenTreeOp* tree);

void genEmitGSCookieCheck(bool pushReg);

void genSetRegToIcon(regNumber reg, ssize_t val, var_types type = TYP_INT, insFlags flags = INS_FLAGS_DONT_CARE);

void genCodeForShift(GenTreePtr tree);

#ifdef _TARGET_XARCH_
void genCodeForShiftRMW(GenTreeStoreInd* storeInd);
#endif // _TARGET_XARCH_

void genCodeForCpObj(GenTreeCpObj* cpObjNode);

void genCodeForCpBlk(GenTreeCpBlk* cpBlkNode);

void genCodeForCpBlkRepMovs(GenTreeCpBlk* cpBlkNode);

void genCodeForCpBlkUnroll(GenTreeCpBlk* cpBlkNode);

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
void genPutStructArgStk(GenTreePtr treeNode, unsigned baseVarNum);

void genStructPutArgRepMovs(GenTreePutArgStk* putArgStkNode, unsigned baseVarNum);
void genStructPutArgUnroll(GenTreePutArgStk* putArgStkNode, unsigned baseVarNum);
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

void genCodeForLoadOffset(instruction ins, emitAttr size, regNumber dst, GenTree* base, unsigned offset);

void genCodeForStoreOffset(instruction ins, emitAttr size, regNumber dst, GenTree* base, unsigned offset);

void genCodeForInitBlk(GenTreeInitBlk* initBlkNode);

void genCodeForInitBlkRepStos(GenTreeInitBlk* initBlkNode);

void genCodeForInitBlkUnroll(GenTreeInitBlk* initBlkNode);

void genJumpTable(GenTree* tree);

void genTableBasedSwitch(GenTree* tree);

void genCodeForArrIndex(GenTreeArrIndex* treeNode);

void genCodeForArrOffset(GenTreeArrOffs* treeNode);

instruction genGetInsForOper(genTreeOps oper, var_types type);

void genStoreInd(GenTreePtr node);

bool genEmitOptimizedGCWriteBarrier(GCInfo::WriteBarrierForm writeBarrierForm, GenTree* addr, GenTree* data);

void genCallInstruction(GenTreePtr call);

void genJmpMethod(GenTreePtr jmp);

void genMultiRegCallStoreToLocal(GenTreePtr treeNode);

// Deals with codegen for muti-register struct returns.
bool isStructReturn(GenTreePtr treeNode);
void genStructReturn(GenTreePtr treeNode);

// Codegen for GT_RETURN.
void genReturn(GenTreePtr treeNode);

void genLclHeap(GenTreePtr tree);

bool genIsRegCandidateLocal(GenTreePtr tree)
{
    if (!tree->IsLocal())
    {
        return false;
    }
    const LclVarDsc* varDsc = &compiler->lvaTable[tree->gtLclVarCommon.gtLclNum];
    return (varDsc->lvIsRegCandidate());
}

#ifdef DEBUG
GenTree* lastConsumedNode;
void genCheckConsumeNode(GenTree* treeNode);
#else  // !DEBUG
inline void genCheckConsumeNode(GenTree* treeNode)
{
}
#endif // DEBUG

#endif // !LEGACY_BACKEND
