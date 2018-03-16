// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This file contains the members of CodeGen that are defined and used
// only by the RyuJIT backend.  It is included by CodeGen.h in the
// definition of the CodeGen class.
//

#ifndef LEGACY_BACKEND // Not necessary (it's this way in the #include location), but helpful to IntelliSense

void genSetRegToConst(regNumber targetReg, var_types targetType, GenTree* tree);
void genCodeForTreeNode(GenTree* treeNode);
void genCodeForBinary(GenTree* treeNode);

#if defined(_TARGET_X86_)
void genCodeForLongUMod(GenTreeOp* node);
#endif // _TARGET_X86_

void genCodeForDivMod(GenTreeOp* treeNode);
void genCodeForMul(GenTreeOp* treeNode);
void genCodeForMulHi(GenTreeOp* treeNode);
void genLeaInstruction(GenTreeAddrMode* lea);
void genSetRegToCond(regNumber dstReg, GenTree* tree);

#if defined(_TARGET_ARMARCH_)
void genScaledAdd(emitAttr attr, regNumber targetReg, regNumber baseReg, regNumber indexReg, int scale);
#endif // _TARGET_ARMARCH_

#if defined(_TARGET_ARM_)
void genCodeForMulLong(GenTreeMultiRegOp* treeNode);
#endif // _TARGET_ARM_

#if !defined(_TARGET_64BIT_)
void genLongToIntCast(GenTree* treeNode);
#endif

void genIntToIntCast(GenTree* treeNode);
void genFloatToFloatCast(GenTree* treeNode);
void genFloatToIntCast(GenTree* treeNode);
void genIntToFloatCast(GenTree* treeNode);
void genCkfinite(GenTree* treeNode);
void genCodeForCompare(GenTreeOp* tree);
void genIntrinsic(GenTree* treeNode);
void genPutArgStk(GenTreePutArgStk* treeNode);
void genPutArgReg(GenTreeOp* tree);
#ifdef _TARGET_ARM_
void genPutArgSplit(GenTreePutArgSplit* treeNode);
#endif

#if defined(_TARGET_XARCH_)
unsigned getBaseVarForPutArgStk(GenTree* treeNode);
#endif // _TARGET_XARCH_

unsigned getFirstArgWithStackSlot();

void genCompareFloat(GenTree* treeNode);
void genCompareInt(GenTree* treeNode);

#ifdef FEATURE_SIMD
enum SIMDScalarMoveType
{
    SMT_ZeroInitUpper,                  // zero initlaize target upper bits
    SMT_ZeroInitUpper_SrcHasUpperZeros, // zero initialize target upper bits; source upper bits are known to be zero
    SMT_PreserveUpper                   // preserve target upper bits
};

#ifdef _TARGET_ARM64_
insOpts genGetSimdInsOpt(emitAttr size, var_types elementType);
#endif
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
void genSIMDLo64BitConvert(SIMDIntrinsicID intrinsicID,
                           var_types       simdType,
                           var_types       baseType,
                           regNumber       tmpReg,
                           regNumber       tmpIntReg,
                           regNumber       targetReg);
void genSIMDIntrinsic32BitConvert(GenTreeSIMD* simdNode);
void genSIMDIntrinsic64BitConvert(GenTreeSIMD* simdNode);
void genSIMDIntrinsicNarrow(GenTreeSIMD* simdNode);
void genSIMDExtractUpperHalf(GenTreeSIMD* simdNode, regNumber srcReg, regNumber tgtReg);
void genSIMDIntrinsicWiden(GenTreeSIMD* simdNode);
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

#ifdef FEATURE_HW_INTRINSICS
void genHWIntrinsic(GenTreeHWIntrinsic* node);
#if defined(_TARGET_XARCH_)
void genHWIntrinsic_R_R_RM(GenTreeHWIntrinsic* node, instruction ins);
void genHWIntrinsic_R_R_RM_I(GenTreeHWIntrinsic* node, instruction ins);
void genSSEIntrinsic(GenTreeHWIntrinsic* node);
void genSSE2Intrinsic(GenTreeHWIntrinsic* node);
void genSSE41Intrinsic(GenTreeHWIntrinsic* node);
void genSSE42Intrinsic(GenTreeHWIntrinsic* node);
void genAvxOrAvx2Intrinsic(GenTreeHWIntrinsic* node);
void genAESIntrinsic(GenTreeHWIntrinsic* node);
void genBMI1Intrinsic(GenTreeHWIntrinsic* node);
void genBMI2Intrinsic(GenTreeHWIntrinsic* node);
void genFMAIntrinsic(GenTreeHWIntrinsic* node);
void genLZCNTIntrinsic(GenTreeHWIntrinsic* node);
void genPCLMULQDQIntrinsic(GenTreeHWIntrinsic* node);
void genPOPCNTIntrinsic(GenTreeHWIntrinsic* node);
template <typename HWIntrinsicSwitchCaseBody>
void genHWIntrinsicJumpTableFallback(NamedIntrinsic            intrinsic,
                                     regNumber                 nonConstImmReg,
                                     regNumber                 baseReg,
                                     regNumber                 offsReg,
                                     HWIntrinsicSwitchCaseBody emitSwCase);
#endif // defined(_TARGET_XARCH_)
#if defined(_TARGET_ARM64_)
instruction getOpForHWIntrinsic(GenTreeHWIntrinsic* node, var_types instrType);
void genHWIntrinsicUnaryOp(GenTreeHWIntrinsic* node);
void genHWIntrinsicCrcOp(GenTreeHWIntrinsic* node);
void genHWIntrinsicSimdBinaryOp(GenTreeHWIntrinsic* node);
void genHWIntrinsicSimdExtractOp(GenTreeHWIntrinsic* node);
void genHWIntrinsicSimdInsertOp(GenTreeHWIntrinsic* node);
void genHWIntrinsicSimdSelectOp(GenTreeHWIntrinsic* node);
void genHWIntrinsicSimdSetAllOp(GenTreeHWIntrinsic* node);
void genHWIntrinsicSimdUnaryOp(GenTreeHWIntrinsic* node);
void genHWIntrinsicSimdBinaryRMWOp(GenTreeHWIntrinsic* node);
void genHWIntrinsicSimdTernaryRMWOp(GenTreeHWIntrinsic* node);
void genHWIntrinsicShaHashOp(GenTreeHWIntrinsic* node);
void genHWIntrinsicShaRotateOp(GenTreeHWIntrinsic* node);
template <typename HWIntrinsicSwitchCaseBody>
void genHWIntrinsicSwitchTable(regNumber swReg, regNumber tmpReg, int swMax, HWIntrinsicSwitchCaseBody emitSwCase);
#endif // defined(_TARGET_XARCH_)
#endif // FEATURE_HW_INTRINSICS

#if !defined(_TARGET_64BIT_)

// CodeGen for Long Ints

void genStoreLongLclVar(GenTree* treeNode);

#endif // !defined(_TARGET_64BIT_)

void genProduceReg(GenTree* tree);
void genUnspillRegIfNeeded(GenTree* tree);
regNumber genConsumeReg(GenTree* tree);
void genCopyRegIfNeeded(GenTree* tree, regNumber needReg);
void genConsumeRegAndCopy(GenTree* tree, regNumber needReg);

void genConsumeIfReg(GenTree* tree)
{
    if (!tree->isContained())
    {
        (void)genConsumeReg(tree);
    }
}

void genRegCopy(GenTree* tree);
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
#ifdef _TARGET_ARM_
void genConsumeArgSplitStruct(GenTreePutArgSplit* putArgNode);
#endif

void genConsumeRegs(GenTree* tree);
void genConsumeOperands(GenTreeOp* tree);
void genEmitGSCookieCheck(bool pushReg);
void genSetRegToIcon(regNumber reg, ssize_t val, var_types type = TYP_INT, insFlags flags = INS_FLAGS_DONT_CARE);
void genCodeForShift(GenTree* tree);

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
void genCodeForShiftLong(GenTree* tree);
#endif

#ifdef _TARGET_XARCH_
void genCodeForShiftRMW(GenTreeStoreInd* storeInd);
void genCodeForBT(GenTreeOp* bt);
#endif // _TARGET_XARCH_

void genCodeForCast(GenTreeOp* tree);
void genCodeForLclAddr(GenTree* tree);
void genCodeForIndexAddr(GenTreeIndexAddr* tree);
void genCodeForIndir(GenTreeIndir* tree);
void genCodeForNegNot(GenTree* tree);
void genCodeForLclVar(GenTreeLclVar* tree);
void genCodeForLclFld(GenTreeLclFld* tree);
void genCodeForStoreLclFld(GenTreeLclFld* tree);
void genCodeForStoreLclVar(GenTreeLclVar* tree);
void genCodeForReturnTrap(GenTreeOp* tree);
void genCodeForJcc(GenTreeCC* tree);
void genCodeForSetcc(GenTreeCC* setcc);
void genCodeForStoreInd(GenTreeStoreInd* tree);
void genCodeForSwap(GenTreeOp* tree);
void genCodeForCpObj(GenTreeObj* cpObjNode);
void genCodeForCpBlk(GenTreeBlk* cpBlkNode);
void genCodeForCpBlkRepMovs(GenTreeBlk* cpBlkNode);
void genCodeForCpBlkUnroll(GenTreeBlk* cpBlkNode);
void genCodeForPhysReg(GenTreePhysReg* tree);
void genCodeForNullCheck(GenTreeOp* tree);
void genCodeForCmpXchg(GenTreeCmpXchg* tree);

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
bool genEmitOptimizedGCWriteBarrier(GCInfo::WriteBarrierForm writeBarrierForm, GenTree* addr, GenTree* data);
void genCallInstruction(GenTreeCall* call);
void genJmpMethod(GenTree* jmp);
BasicBlock* genCallFinally(BasicBlock* block);
void genCodeForJumpTrue(GenTree* tree);
#ifdef _TARGET_ARM64_
void genCodeForJumpCompare(GenTreeOp* tree);
#endif // _TARGET_ARM64_

#if FEATURE_EH_FUNCLETS
void genEHCatchRet(BasicBlock* block);
#else  // !FEATURE_EH_FUNCLETS
void genEHFinallyOrFilterRet(BasicBlock* block);
#endif // !FEATURE_EH_FUNCLETS

void genMultiRegCallStoreToLocal(GenTree* treeNode);

// Deals with codegen for muti-register struct returns.
bool isStructReturn(GenTree* treeNode);
void genStructReturn(GenTree* treeNode);

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
void genLongReturn(GenTree* treeNode);
#endif // _TARGET_X86_ ||  _TARGET_ARM_

#if defined(_TARGET_X86_)
void genFloatReturn(GenTree* treeNode);
#endif // _TARGET_X86_

#if defined(_TARGET_ARM64_)
void genSimpleReturn(GenTree* treeNode);
#endif // _TARGET_ARM64_

void genReturn(GenTree* treeNode);

void genLclHeap(GenTree* tree);

bool genIsRegCandidateLocal(GenTree* tree)
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
