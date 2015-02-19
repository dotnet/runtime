//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//
// This file contains the members of CodeGen that are defined and used
// only by the RyuJIT backend.  It is included by CodeGen.h in the
// definition of the CodeGen class.
//

#ifndef LEGACY_BACKEND // Not necessary (it's this way in the #include location), but helpful to IntelliSense

    void                genSetRegToConst(regNumber targetReg, var_types targetType, GenTreePtr tree);

    void                genCodeForTreeNode(GenTreePtr treeNode);

    void                genCodeForBinary(GenTreePtr treeNode);

    void                genCodeForDivMod(GenTreeOp* treeNode);

    void                genCodeForMulHi(GenTreeOp* treeNode);

    void                genCodeForPow2Div(GenTreeOp* treeNode);

    void                genLeaInstruction(GenTreeAddrMode *lea);

    void                genSetRegToCond(regNumber dstReg, GenTreePtr tree);

    void                genIntToIntCast(GenTreePtr treeNode);

    void                genFloatToFloatCast(GenTreePtr treeNode);

    void                genFloatToIntCast(GenTreePtr treeNode);

    void                genIntToFloatCast(GenTreePtr treeNode);

    void                genCkfinite(GenTreePtr treeNode);

    void                genMathIntrinsic(GenTreePtr treeNode);

    void                genPutArgStk(GenTreePtr treeNode);

#ifdef FEATURE_SIMD
    instruction         getOpForSIMDIntrinsic(SIMDIntrinsicID intrinsicId, var_types baseType, unsigned *ival = nullptr);
	void				genSIMDScalarMove(var_types type, regNumber target, regNumber src, bool zeroInit);
    void                genSIMDIntrinsicInit(GenTreeSIMD* simdNode);
    void                genSIMDIntrinsicInitN(GenTreeSIMD* simdNode);
    void                genSIMDIntrinsicInitArray(GenTreeSIMD* simdNode);
    void                genSIMDIntrinsicUnOp(GenTreeSIMD* simdNode);
    void                genSIMDIntrinsicBinOp(GenTreeSIMD* simdNode);
    void                genSIMDIntrinsicRelOp(GenTreeSIMD* simdNode);
    void                genSIMDIntrinsicDotProduct(GenTreeSIMD* simdNode);
    void                genSIMDIntrinsicSetItem(GenTreeSIMD* simdNode);
    void                genSIMDIntrinsicGetItem(GenTreeSIMD* simdNode);
    void                genSIMDIntrinsicShuffleSSE2(GenTreeSIMD* simdNode);
    void                genSIMDIntrinsicUpperSave(GenTreeSIMD* simdNode);
    void                genSIMDIntrinsicUpperRestore(GenTreeSIMD* simdNode);

    void                genSIMDIntrinsic(GenTreeSIMD* simdNode);
    void                genSIMDCheck(GenTree* treeNode);

    // TYP_SIMD12 (i.e Vector3 of size 12 bytes) is not a hardware supported size and requires
    // two reads/writes on 64-bit targets. These routines abstract reading/writing of Vector3
    // values through an indirection. Note that Vector3 locals allocated on stack would have
    // their size rounded to TARGET_POINTER_SIZE (which is 8 bytes on 64-bit targets) and hence
    // Vector3 locals could be treated as TYP_SIMD16 while reading/writing.
    void                genStoreIndTypeSIMD12(GenTree* treeNode);
    void                genStoreLclFldTypeSIMD12(GenTree* treeNode);
    void                genLoadIndTypeSIMD12(GenTree* treeNode);
    void                genLoadLclFldTypeSIMD12(GenTree* treeNode);    
#endif // FEATURE_SIMD

#if !defined(_TARGET_64BIT_)

    // CodeGen for Long Ints

    void                genStoreLongLclVar(GenTree* treeNode);

#endif // !defined(_TARGET_64BIT_)

    void                genProduceReg(GenTree *tree);

    void                genUnspillRegIfNeeded(GenTree* tree);
    
    regNumber           genConsumeReg(GenTree *tree);

    void                genConsumeRegAndCopy(GenTree *tree, regNumber needReg);

    void                genConsumeIfReg(GenTreePtr tree)
    {
        if (!tree->isContained())
            (void) genConsumeReg(tree);
    }

    void                genRegCopy(GenTreePtr tree);

    void                genTransferRegGCState(regNumber dst, regNumber src);

    void                genConsumeAddress(GenTree* addr);

    void                genConsumeAddrMode(GenTreeAddrMode *mode);

    void                genConsumeBlockOp(GenTreeBlkOp* blkNode, regNumber dstReg, regNumber srcReg, regNumber sizeReg);

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    void                genConsumePutArgStk(GenTreePutArgStk* putArgStkNode, regNumber dstReg, regNumber srcReg, regNumber sizeReg);
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

    void                genConsumeRegs(GenTree* tree);

    void                genConsumeOperands(GenTreeOp* tree);

    void                genEmitGSCookieCheck(bool           pushReg);

    void                genSetRegToIcon     (regNumber      reg,
                                             ssize_t        val,
                                             var_types      type  = TYP_INT,
                                             insFlags       flags = INS_FLAGS_DONT_CARE);

    void                genCodeForShift     (GenTreePtr dst,
                                             GenTreePtr src,
                                             GenTreePtr treeNode);

    void                genCodeForCpObj          (GenTreeCpObj* cpObjNode);

    void                genCodeForCpBlk          (GenTreeCpBlk* cpBlkNode);

    void                genCodeForCpBlkRepMovs   (GenTreeCpBlk* cpBlkNode);

    void                genCodeForCpBlkUnroll    (GenTreeCpBlk* cpBlkNode);

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    void                genCodeForPutArgRepMovs(GenTreePutArgStk* putArgStkNode);
    void                genCodeForPutArgUnroll(GenTreePutArgStk* putArgStkNode);
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

    void                genCodeForLoadOffset(instruction ins, emitAttr size, regNumber dst, GenTree* base, unsigned offset);

    void                genCodeForStoreOffset(instruction ins, emitAttr size, regNumber dst, GenTree* base, unsigned offset);

    void                genCodeForInitBlk        (GenTreeInitBlk* initBlkNode);

    void                genCodeForInitBlkRepStos (GenTreeInitBlk* initBlkNode);

    void                genCodeForInitBlkUnroll  (GenTreeInitBlk* initBlkNode);

    void                genJumpTable(GenTree* tree);

    void                genTableBasedSwitch(GenTree* tree);

    void                genCodeForArrIndex  (GenTreeArrIndex* treeNode);

    void                genCodeForArrOffset (GenTreeArrOffs* treeNode);

    instruction         genGetInsForOper    (genTreeOps oper, var_types type);

    void                genCallInstruction(GenTreePtr call);
    
    void                genJmpMethod(GenTreePtr jmp);

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    void genGetStructTypeSizeOffset(const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR& structDesc,
                                    var_types* type0,
                                    var_types* type1,
                                    emitAttr* size0,
                                    emitAttr* size1,
                                    unsigned __int8* offset0,
                                    unsigned __int8* offset1);

    bool                genStoreRegisterReturnInLclVar(GenTreePtr treeNode);
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

    void                genLclHeap(GenTreePtr tree);

    bool                genIsRegCandidateLocal (GenTreePtr    tree)
    {
        if (!tree->IsLocal()) return false;
        const LclVarDsc * varDsc = &compiler->lvaTable[tree->gtLclVarCommon.gtLclNum];
        return(varDsc->lvIsRegCandidate());
    }

#ifdef DEBUG
    GenTree*            lastConsumedNode;
    void                genCheckConsumeNode(GenTree* treeNode);
#else // !DEBUG
    inline void         genCheckConsumeNode(GenTree* treeNode) {}
#endif // DEBUG

#endif // !LEGACY_BACKEND
