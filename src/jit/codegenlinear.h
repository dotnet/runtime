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

#if defined(_TARGET_X86_)
void genCodeForLongUMod(GenTreeOp* node);
#endif // _TARGET_X86_

void genCodeForDivMod(GenTreeOp* treeNode);

void genCodeForMulHi(GenTreeOp* treeNode);

void genLeaInstruction(GenTreeAddrMode* lea);

void genSetRegToCond(regNumber dstReg, GenTreePtr tree);

#if !defined(_TARGET_64BIT_)
void genLongToIntCast(GenTreePtr treeNode);
#endif

void genIntToIntCast(GenTreePtr treeNode);

void genFloatToFloatCast(GenTreePtr treeNode);

void genFloatToIntCast(GenTreePtr treeNode);

void genIntToFloatCast(GenTreePtr treeNode);

void genCkfinite(GenTreePtr treeNode);

void genIntrinsic(GenTreePtr treeNode);

void genPutArgStk(GenTreePutArgStk* treeNode);
unsigned getBaseVarForPutArgStk(GenTreePtr treeNode);

#if defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_)
unsigned getFirstArgWithStackSlot();
#endif // _TARGET_XARCH_ || _TARGET_ARM64_

void genCompareFloat(GenTreePtr treeNode);

void genCompareInt(GenTreePtr treeNode);

#if !defined(_TARGET_64BIT_)
void genCompareLong(GenTreePtr treeNode);
#if defined(_TARGET_ARM_)
void genJccLongHi(genTreeOps cmp, BasicBlock* jumpTrue, BasicBlock* jumpFalse, bool isUnsigned = false);
void genJccLongLo(genTreeOps cmp, BasicBlock* jumpTrue, BasicBlock* jumpFalse);
#endif // defined(_TARGET_ARM_)
#endif

#ifdef FEATURE_SIMD
enum SIMDScalarMoveType
{
    SMT_ZeroInitUpper,                  // zero initlaize target upper bits
    SMT_ZeroInitUpper_SrcHasUpperZeros, // zero initialize target upper bits; source upper bits are known to be zero
    SMT_PreserveUpper                   // preserve target upper bits
};

instruction getOpForSIMDIntrinsic(SIMDIntrinsicID intrinsicId, var_types baseType, unsigned* ival = nullptr);
void genSIMDScalarMove(
    var_types targetType, var_types type, regNumber target, regNumber src, SIMDScalarMoveType moveType);
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
void genLoadIndTypeSIMD12(GenTree* treeNode);
void genStoreLclTypeSIMD12(GenTree* treeNode);
void genLoadLclTypeSIMD12(GenTree* treeNode);
#ifdef _TARGET_X86_
void genStoreSIMD12ToStack(regNumber operandReg, regNumber tmpReg);
void genPutArgStkSIMD12(GenTree* treeNode);
#endif // _TARGET_X86_
#endif // FEATURE_SIMD

#if !defined(_TARGET_64BIT_)

// CodeGen for Long Ints

void genStoreLongLclVar(GenTree* treeNode);

#endif // !defined(_TARGET_64BIT_)

void genProduceReg(GenTree* tree);

void genUnspillRegIfNeeded(GenTree* tree);

regNumber genConsumeReg(GenTree* tree);

void genCopyRegIfNeeded(GenTree* tree, regNumber needReg);
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

void genSetBlockSize(GenTreeBlk* blkNode, regNumber sizeReg);
void genConsumeBlockSrc(GenTreeBlk* blkNode);
void genSetBlockSrc(GenTreeBlk* blkNode, regNumber srcReg);
void genConsumeBlockOp(GenTreeBlk* blkNode, regNumber dstReg, regNumber srcReg, regNumber sizeReg);

#ifdef FEATURE_PUT_STRUCT_ARG_STK
void genConsumePutStructArgStk(GenTreePutArgStk* putArgStkNode, regNumber dstReg, regNumber srcReg, regNumber sizeReg);
#endif // FEATURE_PUT_STRUCT_ARG_STK

void genConsumeRegs(GenTree* tree);

void genConsumeOperands(GenTreeOp* tree);

void genEmitGSCookieCheck(bool pushReg);

void genSetRegToIcon(regNumber reg, ssize_t val, var_types type = TYP_INT, insFlags flags = INS_FLAGS_DONT_CARE);

void genCodeForShift(GenTreePtr tree);

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
void genCodeForShiftLong(GenTreePtr tree);
#endif

#ifdef _TARGET_XARCH_
void genCodeForShiftRMW(GenTreeStoreInd* storeInd);
#endif // _TARGET_XARCH_

void genCodeForLclFld(GenTreeLclFld* tree);

void genCodeForCpObj(GenTreeObj* cpObjNode);

void genCodeForCpBlk(GenTreeBlk* cpBlkNode);

void genCodeForCpBlkRepMovs(GenTreeBlk* cpBlkNode);

void genCodeForCpBlkUnroll(GenTreeBlk* cpBlkNode);

void genAlignStackBeforeCall(GenTreePutArgStk* putArgStk);
void genAlignStackBeforeCall(GenTreeCall* call);
void genRemoveAlignmentAfterCall(GenTreeCall* call, unsigned bias = 0);

#if defined(UNIX_X86_ABI)

unsigned curNestedAlignment; // Keep track of alignment adjustment required during codegen.
unsigned maxNestedAlignment; // The maximum amount of alignment adjustment required.

void SubtractNestedAlignment(unsigned adjustment)
{
    assert(curNestedAlignment >= adjustment);
    unsigned newNestedAlignment = curNestedAlignment - adjustment;
    if (curNestedAlignment != newNestedAlignment)
    {
        JITDUMP("Adjusting stack nested alignment from %d to %d\n", curNestedAlignment, newNestedAlignment);
    }
    curNestedAlignment = newNestedAlignment;
}

void AddNestedAlignment(unsigned adjustment)
{
    unsigned newNestedAlignment = curNestedAlignment + adjustment;
    if (curNestedAlignment != newNestedAlignment)
    {
        JITDUMP("Adjusting stack nested alignment from %d to %d\n", curNestedAlignment, newNestedAlignment);
    }
    curNestedAlignment = newNestedAlignment;

    if (curNestedAlignment > maxNestedAlignment)
    {
        JITDUMP("Max stack nested alignment changed from %d to %d\n", maxNestedAlignment, curNestedAlignment);
        maxNestedAlignment = curNestedAlignment;
    }
}

#endif

#ifdef FEATURE_PUT_STRUCT_ARG_STK
#ifdef _TARGET_X86_
bool genAdjustStackForPutArgStk(GenTreePutArgStk* putArgStk);
void genPushReg(var_types type, regNumber srcReg);
void genPutArgStkFieldList(GenTreePutArgStk* putArgStk);
#endif // _TARGET_X86_

void genPutStructArgStk(GenTreePutArgStk* treeNode);

unsigned genMove8IfNeeded(unsigned size, regNumber tmpReg, GenTree* srcAddr, unsigned offset);
unsigned genMove4IfNeeded(unsigned size, regNumber tmpReg, GenTree* srcAddr, unsigned offset);
unsigned genMove2IfNeeded(unsigned size, regNumber tmpReg, GenTree* srcAddr, unsigned offset);
unsigned genMove1IfNeeded(unsigned size, regNumber tmpReg, GenTree* srcAddr, unsigned offset);
void genStructPutArgRepMovs(GenTreePutArgStk* putArgStkNode);
void genStructPutArgUnroll(GenTreePutArgStk* putArgStkNode);
void genStoreRegToStackArg(var_types type, regNumber reg, int offset);
#endif // FEATURE_PUT_STRUCT_ARG_STK

void genCodeForLoadOffset(instruction ins, emitAttr size, regNumber dst, GenTree* base, unsigned offset);

void genCodeForStoreOffset(instruction ins, emitAttr size, regNumber src, GenTree* base, unsigned offset);

#ifdef _TARGET_ARM64_
void genCodeForLoadPairOffset(regNumber dst, regNumber dst2, GenTree* base, unsigned offset);

void genCodeForStorePairOffset(regNumber src, regNumber src2, GenTree* base, unsigned offset);
#endif // _TARGET_ARM64_

void genCodeForStoreBlk(GenTreeBlk* storeBlkNode);

void genCodeForInitBlk(GenTreeBlk* initBlkNode);

void genCodeForInitBlkRepStos(GenTreeBlk* initBlkNode);

void genCodeForInitBlkUnroll(GenTreeBlk* initBlkNode);

void genJumpTable(GenTree* tree);

void genTableBasedSwitch(GenTree* tree);

void genCodeForArrIndex(GenTreeArrIndex* treeNode);

void genCodeForArrOffset(GenTreeArrOffs* treeNode);

instruction genGetInsForOper(genTreeOps oper, var_types type);

void genStoreInd(GenTreePtr node);

bool genEmitOptimizedGCWriteBarrier(GCInfo::WriteBarrierForm writeBarrierForm, GenTree* addr, GenTree* data);

void genCallInstruction(GenTreeCall* call);

void genJmpMethod(GenTreePtr jmp);

BasicBlock* genCallFinally(BasicBlock* block);

void genCodeForJumpTrue(GenTreePtr tree);

#if FEATURE_EH_FUNCLETS
void genEHCatchRet(BasicBlock* block);
#else  // !FEATURE_EH_FUNCLETS
void genEHFinallyOrFilterRet(BasicBlock* block);
#endif // !FEATURE_EH_FUNCLETS

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

#ifdef FEATURE_PUT_STRUCT_ARG_STK
#ifdef _TARGET_X86_
bool m_pushStkArg;
#else  // !_TARGET_X86_
unsigned m_stkArgVarNum;
unsigned m_stkArgOffset;
#endif // !_TARGET_X86_
#endif // !FEATURE_PUT_STRUCT_ARG_STK

#ifdef DEBUG
GenTree* lastConsumedNode;
void genNumberOperandUse(GenTree* const operand, int& useNum) const;
void genCheckConsumeNode(GenTree* const node);
#else  // !DEBUG
inline void genCheckConsumeNode(GenTree* treeNode)
{
}
#endif // DEBUG

#endif // !LEGACY_BACKEND
