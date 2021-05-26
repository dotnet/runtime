// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                               Lower                                       XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifndef _LOWER_H_
#define _LOWER_H_

#include "compiler.h"
#include "phase.h"
#include "lsra.h"
#include "sideeffects.h"

class Lowering final : public Phase
{
public:
    inline Lowering(Compiler* compiler, LinearScanInterface* lsra)
        : Phase(compiler, PHASE_LOWERING), vtableCallTemp(BAD_VAR_NUM)
    {
        m_lsra = (LinearScan*)lsra;
        assert(m_lsra);
    }
    virtual PhaseStatus DoPhase() override;

    // This variant of LowerRange is called from outside of the main Lowering pass,
    // so it creates its own instance of Lowering to do so.
    void LowerRange(BasicBlock* block, LIR::ReadOnlyRange& range)
    {
        Lowering lowerer(comp, m_lsra);
        lowerer.m_block = block;

        lowerer.LowerRange(range);
    }

private:
    // LowerRange handles new code that is introduced by or after Lowering.
    void LowerRange(LIR::ReadOnlyRange& range)
    {
        for (GenTree* newNode : range)
        {
            LowerNode(newNode);
        }
    }
    void LowerRange(GenTree* firstNode, GenTree* lastNode)
    {
        LIR::ReadOnlyRange range(firstNode, lastNode);
        LowerRange(range);
    }

    // ContainCheckRange handles new code that is introduced by or after Lowering,
    // and that is known to be already in Lowered form.
    void ContainCheckRange(LIR::ReadOnlyRange& range)
    {
        for (GenTree* newNode : range)
        {
            ContainCheckNode(newNode);
        }
    }
    void ContainCheckRange(GenTree* firstNode, GenTree* lastNode)
    {
        LIR::ReadOnlyRange range(firstNode, lastNode);
        ContainCheckRange(range);
    }

    void InsertTreeBeforeAndContainCheck(GenTree* insertionPoint, GenTree* tree)
    {
        LIR::Range range = LIR::SeqTree(comp, tree);
        ContainCheckRange(range);
        BlockRange().InsertBefore(insertionPoint, std::move(range));
    }

    void ContainCheckNode(GenTree* node);

    void ContainCheckDivOrMod(GenTreeOp* node);
    void ContainCheckReturnTrap(GenTreeOp* node);
    void ContainCheckArrOffset(GenTreeArrOffs* node);
    void ContainCheckLclHeap(GenTreeOp* node);
    void ContainCheckRet(GenTreeUnOp* ret);
    void ContainCheckJTrue(GenTreeOp* node);

    void ContainCheckBitCast(GenTree* node);
    void ContainCheckCallOperands(GenTreeCall* call);
    void ContainCheckIndir(GenTreeIndir* indirNode);
    void ContainCheckStoreIndir(GenTreeIndir* indirNode);
    void ContainCheckMul(GenTreeOp* node);
    void ContainCheckShiftRotate(GenTreeOp* node);
    void ContainCheckStoreLoc(GenTreeLclVarCommon* storeLoc) const;
    void ContainCheckCast(GenTreeCast* node);
    void ContainCheckCompare(GenTreeOp* node);
    void ContainCheckBinary(GenTreeOp* node);
    void ContainCheckBoundsChk(GenTreeBoundsChk* node);
#ifdef TARGET_XARCH
    void ContainCheckFloatBinary(GenTreeOp* node);
    void ContainCheckIntrinsic(GenTreeOp* node);
#endif // TARGET_XARCH
#ifdef FEATURE_SIMD
    void ContainCheckSIMD(GenTreeSIMD* simdNode);
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
    void ContainCheckHWIntrinsicAddr(GenTreeHWIntrinsic* node, GenTree* addr);
    void ContainCheckHWIntrinsic(GenTreeHWIntrinsic* node);
#endif // FEATURE_HW_INTRINSICS

#ifdef DEBUG
    static void CheckCallArg(GenTree* arg);
    static void CheckCall(GenTreeCall* call);
    static void CheckNode(Compiler* compiler, GenTree* node);
    static bool CheckBlock(Compiler* compiler, BasicBlock* block);
#endif // DEBUG

    void LowerBlock(BasicBlock* block);
    GenTree* LowerNode(GenTree* node);

    // ------------------------------
    // Call Lowering
    // ------------------------------
    void LowerCall(GenTree* call);
#ifndef TARGET_64BIT
    GenTree* DecomposeLongCompare(GenTree* cmp);
#endif
    GenTree* OptimizeConstCompare(GenTree* cmp);
    GenTree* LowerCompare(GenTree* cmp);
    GenTree* LowerJTrue(GenTreeOp* jtrue);
    GenTreeCC* LowerNodeCC(GenTree* node, GenCondition condition);
    void LowerJmpMethod(GenTree* jmp);
    void LowerRet(GenTreeUnOp* ret);
    void LowerStoreLocCommon(GenTreeLclVarCommon* lclVar);
    void LowerRetStruct(GenTreeUnOp* ret);
    void LowerRetSingleRegStructLclVar(GenTreeUnOp* ret);
    void LowerCallStruct(GenTreeCall* call);
    void LowerStoreSingleRegCallStruct(GenTreeBlk* store);
#if !defined(WINDOWS_AMD64_ABI)
    GenTreeLclVar* SpillStructCallResult(GenTreeCall* call) const;
#endif // WINDOWS_AMD64_ABI
    GenTree* LowerDelegateInvoke(GenTreeCall* call);
    GenTree* LowerIndirectNonvirtCall(GenTreeCall* call);
    GenTree* LowerDirectCall(GenTreeCall* call);
    GenTree* LowerNonvirtPinvokeCall(GenTreeCall* call);
    GenTree* LowerTailCallViaJitHelper(GenTreeCall* callNode, GenTree* callTarget);
    void LowerFastTailCall(GenTreeCall* callNode);
    void RehomeArgForFastTailCall(unsigned int lclNum,
                                  GenTree*     insertTempBefore,
                                  GenTree*     lookForUsesStart,
                                  GenTreeCall* callNode);
    void InsertProfTailCallHook(GenTreeCall* callNode, GenTree* insertionPoint);
    GenTree* LowerVirtualVtableCall(GenTreeCall* call);
    GenTree* LowerVirtualStubCall(GenTreeCall* call);
    void LowerArgsForCall(GenTreeCall* call);
    void ReplaceArgWithPutArgOrBitcast(GenTree** ppChild, GenTree* newNode);
    GenTree* NewPutArg(GenTreeCall* call, GenTree* arg, fgArgTabEntry* info, var_types type);
    void LowerArg(GenTreeCall* call, GenTree** ppTree);
#ifdef TARGET_ARMARCH
    GenTree* LowerFloatArg(GenTree** pArg, fgArgTabEntry* info);
    GenTree* LowerFloatArgReg(GenTree* arg, regNumber regNum);
#endif

    void InsertPInvokeCallProlog(GenTreeCall* call);
    void InsertPInvokeCallEpilog(GenTreeCall* call);
    void InsertPInvokeMethodProlog();
    void InsertPInvokeMethodEpilog(BasicBlock* returnBB DEBUGARG(GenTree* lastExpr));
    GenTree* SetGCState(int cns);
    GenTree* CreateReturnTrapSeq();
    enum FrameLinkAction
    {
        PushFrame,
        PopFrame
    };
    GenTree* CreateFrameLinkUpdate(FrameLinkAction);
    GenTree* AddrGen(ssize_t addr);
    GenTree* AddrGen(void* addr);

    GenTree* Ind(GenTree* tree, var_types type = TYP_I_IMPL)
    {
        return comp->gtNewOperNode(GT_IND, type, tree);
    }

    GenTree* PhysReg(regNumber reg, var_types type = TYP_I_IMPL)
    {
        return comp->gtNewPhysRegNode(reg, type);
    }

    GenTree* ThisReg(GenTreeCall* call)
    {
        return PhysReg(comp->codeGen->genGetThisArgReg(call), TYP_REF);
    }

    GenTree* Offset(GenTree* base, unsigned offset)
    {
        var_types resultType = (base->TypeGet() == TYP_REF) ? TYP_BYREF : base->TypeGet();
        return new (comp, GT_LEA) GenTreeAddrMode(resultType, base, nullptr, 0, offset);
    }

    GenTree* OffsetByIndex(GenTree* base, GenTree* index)
    {
        var_types resultType = (base->TypeGet() == TYP_REF) ? TYP_BYREF : base->TypeGet();
        return new (comp, GT_LEA) GenTreeAddrMode(resultType, base, index, 0, 0);
    }

    GenTree* OffsetByIndexWithScale(GenTree* base, GenTree* index, unsigned scale)
    {
        var_types resultType = (base->TypeGet() == TYP_REF) ? TYP_BYREF : base->TypeGet();
        return new (comp, GT_LEA) GenTreeAddrMode(resultType, base, index, scale, 0);
    }

    // Replace the definition of the given use with a lclVar, allocating a new temp
    // if 'tempNum' is BAD_VAR_NUM. Returns the LclVar node.
    GenTreeLclVar* ReplaceWithLclVar(LIR::Use& use, unsigned tempNum = BAD_VAR_NUM)
    {
        GenTree* oldUseNode = use.Def();
        if ((oldUseNode->gtOper != GT_LCL_VAR) || (tempNum != BAD_VAR_NUM))
        {
            GenTree* assign;
            use.ReplaceWithLclVar(comp, tempNum, &assign);

            GenTree* newUseNode = use.Def();
            ContainCheckRange(oldUseNode->gtNext, newUseNode);

            // We need to lower the LclVar and assignment since there may be certain
            // types or scenarios, such as TYP_SIMD12, that need special handling

            LowerNode(assign);
            LowerNode(newUseNode);

            return newUseNode->AsLclVar();
        }
        return oldUseNode->AsLclVar();
    }

    // return true if this call target is within range of a pc-rel call on the machine
    bool IsCallTargetInRange(void* addr);

#if defined(TARGET_XARCH)
    GenTree* PreferredRegOptionalOperand(GenTree* tree);

    // ------------------------------------------------------------------
    // SetRegOptionalBinOp - Indicates which of the operands of a bin-op
    // register requirement is optional. Xarch instruction set allows
    // either of op1 or op2 of binary operation (e.g. add, mul etc) to be
    // a memory operand.  This routine provides info to register allocator
    // which of its operands optionally require a register.  Lsra might not
    // allocate a register to RefTypeUse positions of such operands if it
    // is beneficial. In such a case codegen will treat them as memory
    // operands.
    //
    // Arguments:
    //     tree  -             Gentree of a binary operation.
    //     isSafeToMarkOp1     True if it's safe to mark op1 as register optional
    //     isSafeToMarkOp2     True if it's safe to mark op2 as register optional
    //
    // Returns
    //     The caller is expected to get isSafeToMarkOp1 and isSafeToMarkOp2
    //     by calling IsSafeToContainMem.
    //
    // Note: On xarch at most only one of the operands will be marked as
    // reg optional, even when both operands could be considered register
    // optional.
    void SetRegOptionalForBinOp(GenTree* tree, bool isSafeToMarkOp1, bool isSafeToMarkOp2)
    {
        assert(GenTree::OperIsBinary(tree->OperGet()));

        GenTree* const op1 = tree->gtGetOp1();
        GenTree* const op2 = tree->gtGetOp2();

        const unsigned operatorSize = genTypeSize(tree->TypeGet());

        const bool op1Legal =
            isSafeToMarkOp1 && tree->OperIsCommutative() && (operatorSize == genTypeSize(op1->TypeGet()));
        const bool op2Legal = isSafeToMarkOp2 && (operatorSize == genTypeSize(op2->TypeGet()));

        GenTree* regOptionalOperand = nullptr;
        if (op1Legal)
        {
            regOptionalOperand = op2Legal ? PreferredRegOptionalOperand(tree) : op1;
        }
        else if (op2Legal)
        {
            regOptionalOperand = op2;
        }
        if (regOptionalOperand != nullptr)
        {
            regOptionalOperand->SetRegOptional();
        }
    }
#endif // defined(TARGET_XARCH)

    // Per tree node member functions
    void LowerStoreIndirCommon(GenTreeIndir* ind);
    void LowerIndir(GenTreeIndir* ind);
    void LowerStoreIndir(GenTreeIndir* node);
    GenTree* LowerAdd(GenTreeOp* node);
    bool LowerUnsignedDivOrMod(GenTreeOp* divMod);
    GenTree* LowerConstIntDivOrMod(GenTree* node);
    GenTree* LowerSignedDivOrMod(GenTree* node);
    void LowerBlockStore(GenTreeBlk* blkNode);
    void LowerBlockStoreCommon(GenTreeBlk* blkNode);
    void ContainBlockStoreAddress(GenTreeBlk* blkNode, unsigned size, GenTree* addr);
    void LowerPutArgStk(GenTreePutArgStk* tree);

    bool TryCreateAddrMode(GenTree* addr, bool isContainable);

    bool TryTransformStoreObjAsStoreInd(GenTreeBlk* blkNode);

    GenTree* LowerSwitch(GenTree* node);
    bool TryLowerSwitchToBitTest(
        BasicBlock* jumpTable[], unsigned jumpCount, unsigned targetCount, BasicBlock* bbSwitch, GenTree* switchValue);

    void LowerCast(GenTree* node);

#if !CPU_LOAD_STORE_ARCH
    bool IsRMWIndirCandidate(GenTree* operand, GenTree* storeInd);
    bool IsBinOpInRMWStoreInd(GenTree* tree);
    bool IsRMWMemOpRootedAtStoreInd(GenTree* storeIndTree, GenTree** indirCandidate, GenTree** indirOpSource);
    bool LowerRMWMemOp(GenTreeIndir* storeInd);
#endif

    void WidenSIMD12IfNecessary(GenTreeLclVarCommon* node);
    bool CheckMultiRegLclVar(GenTreeLclVar* lclNode, const ReturnTypeDesc* retTypeDesc);
    void LowerStoreLoc(GenTreeLclVarCommon* tree);
    GenTree* LowerArrElem(GenTree* node);
    void LowerRotate(GenTree* tree);
    void LowerShift(GenTreeOp* shift);
#ifdef FEATURE_SIMD
    void LowerSIMD(GenTreeSIMD* simdNode);
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
    void LowerHWIntrinsic(GenTreeHWIntrinsic* node);
    void LowerHWIntrinsicCC(GenTreeHWIntrinsic* node, NamedIntrinsic newIntrinsicId, GenCondition condition);
    void LowerHWIntrinsicCmpOp(GenTreeHWIntrinsic* node, genTreeOps cmpOp);
    void LowerHWIntrinsicCreate(GenTreeHWIntrinsic* node);
    void LowerHWIntrinsicDot(GenTreeHWIntrinsic* node);
#if defined(TARGET_XARCH)
    void LowerFusedMultiplyAdd(GenTreeHWIntrinsic* node);
    void LowerHWIntrinsicToScalar(GenTreeHWIntrinsic* node);
    void LowerHWIntrinsicGetElement(GenTreeHWIntrinsic* node);
    void LowerHWIntrinsicWithElement(GenTreeHWIntrinsic* node);
#elif defined(TARGET_ARM64)
    bool IsValidConstForMovImm(GenTreeHWIntrinsic* node);
    void LowerHWIntrinsicFusedMultiplyAddScalar(GenTreeHWIntrinsic* node);
#endif // !TARGET_XARCH && !TARGET_ARM64

    union VectorConstant {
        int8_t   i8[32];
        uint8_t  u8[32];
        int16_t  i16[16];
        uint16_t u16[16];
        int32_t  i32[8];
        uint32_t u32[8];
        int64_t  i64[4];
        uint64_t u64[4];
        float    f32[8];
        double   f64[4];
    };

    //----------------------------------------------------------------------------------------------
    // VectorConstantIsBroadcastedI64: Check N i64 elements in a constant vector for equality
    //
    //  Arguments:
    //     vecCns  - Constant vector
    //     count   - Amount of i64 components to compare
    //
    //  Returns:
    //     true if N i64 elements of the given vector are equal
    static bool VectorConstantIsBroadcastedI64(VectorConstant& vecCns, int count)
    {
        assert(count >= 1 && count <= 4);
        for (int i = 1; i < count; i++)
        {
            if (vecCns.i64[i] != vecCns.i64[0])
            {
                return false;
            }
        }
        return true;
    }

    //----------------------------------------------------------------------------------------------
    // ProcessArgForHWIntrinsicCreate: Processes an argument for the Lowering::LowerHWIntrinsicCreate method
    //
    //  Arguments:
    //     arg      - The argument to process
    //     argIdx   - The index of the argument being processed
    //     vecCns   - The vector constant being constructed
    //     baseType - The base type of the vector constant
    //
    //  Returns:
    //     true if arg was a constant; otherwise, false
    static bool HandleArgForHWIntrinsicCreate(GenTree* arg, int argIdx, VectorConstant& vecCns, var_types baseType)
    {
        switch (baseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                if (arg->IsCnsIntOrI())
                {
                    vecCns.i8[argIdx] = static_cast<int8_t>(arg->AsIntCon()->gtIconVal);
                    return true;
                }
                else
                {
                    // We expect the VectorConstant to have been already zeroed
                    assert(vecCns.i8[argIdx] == 0);
                }
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                if (arg->IsCnsIntOrI())
                {
                    vecCns.i16[argIdx] = static_cast<int16_t>(arg->AsIntCon()->gtIconVal);
                    return true;
                }
                else
                {
                    // We expect the VectorConstant to have been already zeroed
                    assert(vecCns.i16[argIdx] == 0);
                }
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                if (arg->IsCnsIntOrI())
                {
                    vecCns.i32[argIdx] = static_cast<int32_t>(arg->AsIntCon()->gtIconVal);
                    return true;
                }
                else
                {
                    // We expect the VectorConstant to have been already zeroed
                    assert(vecCns.i32[argIdx] == 0);
                }
                break;
            }

            case TYP_LONG:
            case TYP_ULONG:
            {
#if defined(TARGET_64BIT)
                if (arg->IsCnsIntOrI())
                {
                    vecCns.i64[argIdx] = static_cast<int64_t>(arg->AsIntCon()->gtIconVal);
                    return true;
                }
#else
                if (arg->OperIsLong() && arg->AsOp()->gtOp1->IsCnsIntOrI() && arg->AsOp()->gtOp2->IsCnsIntOrI())
                {
                    // 32-bit targets will decompose GT_CNS_LNG into two GT_CNS_INT
                    // We need to reconstruct the 64-bit value in order to handle this

                    INT64 gtLconVal = arg->AsOp()->gtOp2->AsIntCon()->gtIconVal;
                    gtLconVal <<= 32;
                    gtLconVal |= arg->AsOp()->gtOp1->AsIntCon()->gtIconVal;

                    vecCns.i64[argIdx] = gtLconVal;
                    return true;
                }
#endif // TARGET_64BIT
                else
                {
                    // We expect the VectorConstant to have been already zeroed
                    assert(vecCns.i64[argIdx] == 0);
                }
                break;
            }

            case TYP_FLOAT:
            {
                if (arg->IsCnsFltOrDbl())
                {
                    vecCns.f32[argIdx] = static_cast<float>(arg->AsDblCon()->gtDconVal);
                    return true;
                }
                else
                {
                    // We expect the VectorConstant to have been already zeroed
                    // We check against the i32, rather than f32, to account for -0.0
                    assert(vecCns.i32[argIdx] == 0);
                }
                break;
            }

            case TYP_DOUBLE:
            {
                if (arg->IsCnsFltOrDbl())
                {
                    vecCns.f64[argIdx] = static_cast<double>(arg->AsDblCon()->gtDconVal);
                    return true;
                }
                else
                {
                    // We expect the VectorConstant to have been already zeroed
                    // We check against the i64, rather than f64, to account for -0.0
                    assert(vecCns.i64[argIdx] == 0);
                }
                break;
            }

            default:
            {
                unreached();
            }
        }

        return false;
    }
#endif // FEATURE_HW_INTRINSICS

    //----------------------------------------------------------------------------------------------
    // TryRemoveCastIfPresent: Removes op it is a cast operation and the size of its input is at
    //                         least the size of expectedType
    //
    //  Arguments:
    //     expectedType - The expected type of the cast operation input if it is to be removed
    //     op           - The tree to remove if it is a cast op whose input is at least the size of expectedType
    //
    //  Returns:
    //     op if it was not a cast node or if its input is not at least the size of expected type;
    //     Otherwise, it returns the underlying operation that was being casted
    GenTree* TryRemoveCastIfPresent(var_types expectedType, GenTree* op)
    {
        if (!op->OperIs(GT_CAST))
        {
            return op;
        }

        GenTree* castOp = op->AsCast()->CastOp();

        if (genTypeSize(castOp->gtType) >= genTypeSize(expectedType))
        {
            BlockRange().Remove(op);
            return castOp;
        }

        return op;
    }

    // Utility functions
public:
    static bool IndirsAreEquivalent(GenTree* pTreeA, GenTree* pTreeB);

    // return true if 'childNode' is an immediate that can be contained
    //  by the 'parentNode' (i.e. folded into an instruction)
    //  for example small enough and non-relocatable
    bool IsContainableImmed(GenTree* parentNode, GenTree* childNode) const;

    // Return true if 'node' is a containable memory op.
    bool IsContainableMemoryOp(GenTree* node)
    {
        return m_lsra->isContainableMemoryOp(node);
    }

#ifdef FEATURE_HW_INTRINSICS
    // Return true if 'node' is a containable HWIntrinsic op.
    bool IsContainableHWIntrinsicOp(GenTreeHWIntrinsic* containingNode, GenTree* node, bool* supportsRegOptional);
#endif // FEATURE_HW_INTRINSICS

    static void TransformUnusedIndirection(GenTreeIndir* ind, Compiler* comp, BasicBlock* block);

private:
    static bool NodesAreEquivalentLeaves(GenTree* candidate, GenTree* storeInd);

    bool AreSourcesPossiblyModifiedLocals(GenTree* addr, GenTree* base, GenTree* index);

    // Makes 'childNode' contained in the 'parentNode'
    void MakeSrcContained(GenTree* parentNode, GenTree* childNode) const;

    // Checks and makes 'childNode' contained in the 'parentNode'
    bool CheckImmedAndMakeContained(GenTree* parentNode, GenTree* childNode);

    // Checks for memory conflicts in the instructions between childNode and parentNode, and returns true if childNode
    // can be contained.
    bool IsSafeToContainMem(GenTree* parentNode, GenTree* childNode);

    inline LIR::Range& BlockRange() const
    {
        return LIR::AsRange(m_block);
    }

    // Any tracked lclVar accessed by a LCL_FLD or STORE_LCL_FLD should be marked doNotEnregister.
    // This method checks, and asserts in the DEBUG case if it is not so marked,
    // but in the non-DEBUG case (asserts disabled) set the flag so that we don't generate bad code.
    // This ensures that the local's value is valid on-stack as expected for a *LCL_FLD.
    void verifyLclFldDoNotEnregister(unsigned lclNum)
    {
        LclVarDsc* varDsc = &(comp->lvaTable[lclNum]);
        // Do a couple of simple checks before setting lvDoNotEnregister.
        // This may not cover all cases in 'isRegCandidate()' but we don't want to
        // do an expensive check here. For non-candidates it is not harmful to set lvDoNotEnregister.
        if (varDsc->lvTracked && !varDsc->lvDoNotEnregister)
        {
            assert(!m_lsra->isRegCandidate(varDsc));
            comp->lvaSetVarDoNotEnregister(lclNum DEBUG_ARG(Compiler::DNER_LocalField));
        }
    }

    LinearScan*   m_lsra;
    unsigned      vtableCallTemp;       // local variable we use as a temp for vtable calls
    SideEffectSet m_scratchSideEffects; // SideEffectSet used for IsSafeToContainMem and isRMWIndirCandidate
    BasicBlock*   m_block;
};

#endif // _LOWER_H_
