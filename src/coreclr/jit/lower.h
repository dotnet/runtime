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
        : Phase(compiler, PHASE_LOWERING)
        , vtableCallTemp(BAD_VAR_NUM)
#ifdef TARGET_ARM64
        , m_blockIndirs(compiler->getAllocator(CMK_ArrayStack))
#endif
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
    void ContainCheckLclHeap(GenTreeOp* node);
    void ContainCheckRet(GenTreeUnOp* ret);
#ifdef TARGET_ARM64
    bool TryLowerAndOrToCCMP(GenTreeOp* tree, GenTree** next);
    insCflags TruthifyingFlags(GenCondition cond);
    void ContainCheckConditionalCompare(GenTreeCCMP* ccmp);
    void ContainCheckNeg(GenTreeOp* neg);
    void TryLowerCnsIntCselToCinc(GenTreeOp* select, GenTree* cond);
    void TryLowerCselToCSOp(GenTreeOp* select, GenTree* cond);
    bool TryLowerAddSubToMulLongOp(GenTreeOp* op, GenTree** next);
    bool TryLowerNegToMulLongOp(GenTreeOp* op, GenTree** next);
#endif
    void ContainCheckSelect(GenTreeOp* select);
    void ContainCheckBitCast(GenTree* node);
    void ContainCheckCallOperands(GenTreeCall* call);
    void ContainCheckIndir(GenTreeIndir* indirNode);
    void ContainCheckStoreIndir(GenTreeStoreInd* indirNode);
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
#ifdef FEATURE_HW_INTRINSICS
    void ContainCheckHWIntrinsicAddr(GenTreeHWIntrinsic* node, GenTree* addr);
    void ContainCheckHWIntrinsic(GenTreeHWIntrinsic* node);
#ifdef TARGET_XARCH
    void TryFoldCnsVecForEmbeddedBroadcast(GenTreeHWIntrinsic* parentNode, GenTreeVecCon* childNode);
    void TryCompressConstVecData(GenTreeStoreInd* node);
#endif // TARGET_XARCH
#endif // FEATURE_HW_INTRINSICS

#ifdef DEBUG
    static void CheckCallArg(GenTree* arg);
    static void CheckCall(GenTreeCall* call);
    static void CheckNode(Compiler* compiler, GenTree* node);
    static bool CheckBlock(Compiler* compiler, BasicBlock* block);
#endif // DEBUG

    void LowerBlock(BasicBlock* block);
    GenTree* LowerNode(GenTree* node);

    bool IsCFGCallArgInvariantInRange(GenTree* node, GenTree* endExclusive);

    // ------------------------------
    // Call Lowering
    // ------------------------------
    GenTree* LowerCall(GenTree* call);
    bool LowerCallMemmove(GenTreeCall* call, GenTree** next);
    bool LowerCallMemcmp(GenTreeCall* call, GenTree** next);
    void LowerCFGCall(GenTreeCall* call);
    void MoveCFGCallArgs(GenTreeCall* call);
    void MoveCFGCallArg(GenTreeCall* call, GenTree* node);
#ifndef TARGET_64BIT
    GenTree* DecomposeLongCompare(GenTree* cmp);
#endif
    GenTree* OptimizeConstCompare(GenTree* cmp);
    GenTree* LowerCompare(GenTree* cmp);
    GenTree* LowerJTrue(GenTreeOp* jtrue);
    GenTree* LowerSelect(GenTreeConditional* cond);
    bool TryLowerConditionToFlagsNode(GenTree* parent, GenTree* condition, GenCondition* code);
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
    GenTree* FindEarliestPutArg(GenTreeCall* call);
    size_t MarkPutArgNodes(GenTree* node);
    GenTree* LowerVirtualVtableCall(GenTreeCall* call);
    GenTree* LowerVirtualStubCall(GenTreeCall* call);
    void LowerArgsForCall(GenTreeCall* call);
    void ReplaceArgWithPutArgOrBitcast(GenTree** ppChild, GenTree* newNode);
    GenTree* NewPutArg(GenTreeCall* call, GenTree* arg, CallArg* callArg, var_types type);
    void LowerArg(GenTreeCall* call, CallArg* callArg, bool late);
#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    GenTree* LowerFloatArg(GenTree** pArg, CallArg* callArg);
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
        return comp->gtNewIndir(type, tree);
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
    GenTree* PreferredRegOptionalOperand(GenTree* op1, GenTree* op2);

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
    //     tree  -             GenTree of a binary operation.
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

        const bool op1Legal = isSafeToMarkOp1 && tree->OperIsCommutative() && IsContainableMemoryOpSize(tree, op1);
        const bool op2Legal = isSafeToMarkOp2 && IsContainableMemoryOpSize(tree, op2);

        GenTree* regOptionalOperand = nullptr;

        if (op1Legal)
        {
            regOptionalOperand = op2Legal ? PreferredRegOptionalOperand(op1, op2) : op1;
        }
        else if (op2Legal)
        {
            regOptionalOperand = op2;
        }

        if (regOptionalOperand != nullptr)
        {
            MakeSrcRegOptional(tree, regOptionalOperand);
        }
    }
#endif // defined(TARGET_XARCH)

    // Per tree node member functions
    void LowerStoreIndirCommon(GenTreeStoreInd* ind);
    GenTree* LowerIndir(GenTreeIndir* ind);
    bool OptimizeForLdp(GenTreeIndir* ind);
    bool TryMakeIndirsAdjacent(GenTreeIndir* prevIndir, GenTreeIndir* indir);
    void MarkTree(GenTree* root);
    void UnmarkTree(GenTree* root);
    void LowerStoreIndir(GenTreeStoreInd* node);
    void LowerStoreIndirCoalescing(GenTreeStoreInd* node);
    GenTree* LowerAdd(GenTreeOp* node);
    GenTree* LowerMul(GenTreeOp* mul);
    bool TryLowerAndNegativeOne(GenTreeOp* node, GenTree** nextNode);
    GenTree* LowerBinaryArithmetic(GenTreeOp* binOp);
    bool LowerUnsignedDivOrMod(GenTreeOp* divMod);
    bool TryLowerConstIntDivOrMod(GenTree* node, GenTree** nextNode);
    GenTree* LowerSignedDivOrMod(GenTree* node);
    void LowerBlockStore(GenTreeBlk* blkNode);
    void LowerBlockStoreCommon(GenTreeBlk* blkNode);
    void LowerBlockStoreAsHelperCall(GenTreeBlk* blkNode);
    void LowerLclHeap(GenTree* node);
    void ContainBlockStoreAddress(GenTreeBlk* blkNode, unsigned size, GenTree* addr, GenTree* addrParent);
    void LowerPutArgStkOrSplit(GenTreePutArgStk* putArgNode);
    GenTree* LowerArrLength(GenTreeArrCommon* node);

#ifdef TARGET_XARCH
    void LowerPutArgStk(GenTreePutArgStk* putArgStk);
    GenTree* TryLowerMulWithConstant(GenTreeOp* node);
#endif // TARGET_XARCH

    bool TryCreateAddrMode(GenTree* addr, bool isContainable, GenTree* parent);

    bool TryTransformStoreObjAsStoreInd(GenTreeBlk* blkNode);

    void TryRetypingFloatingPointStoreToIntegerStore(GenTree* store);

    GenTree* LowerSwitch(GenTree* node);
    bool TryLowerSwitchToBitTest(
        FlowEdge* jumpTable[], unsigned jumpCount, unsigned targetCount, BasicBlock* bbSwitch, GenTree* switchValue);

    void LowerCast(GenTree* node);

#if !CPU_LOAD_STORE_ARCH
    bool IsRMWIndirCandidate(GenTree* operand, GenTree* storeInd);
    bool IsBinOpInRMWStoreInd(GenTree* tree);
    bool IsRMWMemOpRootedAtStoreInd(GenTree* storeIndTree, GenTree** indirCandidate, GenTree** indirOpSource);
    bool LowerRMWMemOp(GenTreeIndir* storeInd);
#endif

    void WidenSIMD12IfNecessary(GenTreeLclVarCommon* node);
    bool CheckMultiRegLclVar(GenTreeLclVar* lclNode, int registerCount);
    void LowerStoreLoc(GenTreeLclVarCommon* tree);
    void LowerRotate(GenTree* tree);
    void LowerShift(GenTreeOp* shift);
#ifdef FEATURE_HW_INTRINSICS
    GenTree* LowerHWIntrinsic(GenTreeHWIntrinsic* node);
    void LowerHWIntrinsicCC(GenTreeHWIntrinsic* node, NamedIntrinsic newIntrinsicId, GenCondition condition);
    GenTree* LowerHWIntrinsicCmpOp(GenTreeHWIntrinsic* node, genTreeOps cmpOp);
    GenTree* LowerHWIntrinsicCreate(GenTreeHWIntrinsic* node);
    GenTree* LowerHWIntrinsicDot(GenTreeHWIntrinsic* node);
#if defined(TARGET_XARCH)
    void LowerFusedMultiplyAdd(GenTreeHWIntrinsic* node);
    GenTree* LowerHWIntrinsicToScalar(GenTreeHWIntrinsic* node);
    GenTree* LowerHWIntrinsicGetElement(GenTreeHWIntrinsic* node);
    GenTree* LowerHWIntrinsicCndSel(GenTreeHWIntrinsic* node);
    GenTree* LowerHWIntrinsicTernaryLogic(GenTreeHWIntrinsic* node);
    GenTree* LowerHWIntrinsicWithElement(GenTreeHWIntrinsic* node);
    GenTree* TryLowerAndOpToResetLowestSetBit(GenTreeOp* andNode);
    GenTree* TryLowerAndOpToExtractLowestSetBit(GenTreeOp* andNode);
    GenTree* TryLowerAndOpToAndNot(GenTreeOp* andNode);
    GenTree* TryLowerXorOpToGetMaskUpToLowestSetBit(GenTreeOp* xorNode);
    void LowerBswapOp(GenTreeOp* node);
#elif defined(TARGET_ARM64)
    bool IsValidConstForMovImm(GenTreeHWIntrinsic* node);
    void LowerHWIntrinsicFusedMultiplyAddScalar(GenTreeHWIntrinsic* node);
    void LowerModPow2(GenTree* node);
    bool TryLowerAddForPossibleContainment(GenTreeOp* node, GenTree** next);
#endif // !TARGET_XARCH && !TARGET_ARM64
    GenTree* InsertNewSimdCreateScalarUnsafeNode(var_types   type,
                                                 GenTree*    op1,
                                                 CorInfoType simdBaseJitType,
                                                 unsigned    simdSize);
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
        if (!op->OperIs(GT_CAST) || !comp->opts.OptimizationEnabled())
        {
            return op;
        }

        GenTreeCast* cast   = op->AsCast();
        GenTree*     castOp = cast->CastOp();

        // FP <-> INT casts should be kept
        if (varTypeIsFloating(castOp) ^ varTypeIsFloating(expectedType))
        {
            return op;
        }

        // Keep casts which can overflow
        if (cast->gtOverflow())
        {
            return op;
        }

        // Keep casts with operands usable from memory.
        if (castOp->isContained() || castOp->IsRegOptional())
        {
            return op;
        }

        if (genTypeSize(cast->CastToType()) >= genTypeSize(expectedType))
        {
#ifndef TARGET_64BIT
            // Don't expose TYP_LONG on 32bit
            if (castOp->TypeIs(TYP_LONG))
            {
                return op;
            }
#endif
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
    bool IsContainableMemoryOp(GenTree* node) const
    {
        return m_lsra->isContainableMemoryOp(node);
    }

    // Return true if 'childNode' is a containable memory op by its size relative to the 'parentNode'.
    // Currently very conservative.
    bool IsContainableMemoryOpSize(GenTree* parentNode, GenTree* childNode) const
    {
        if (parentNode->OperIsBinary())
        {
            const unsigned operatorSize = genTypeSize(parentNode->TypeGet());

#ifdef TARGET_XARCH

            // Conservative - only do this for AND, OR, XOR.
            if (parentNode->OperIs(GT_AND, GT_OR, GT_XOR))
            {
                return genTypeSize(childNode->TypeGet()) >= operatorSize;
            }

#endif // TARGET_XARCH

            return genTypeSize(childNode->TypeGet()) == operatorSize;
        }

        return false;
    }

#ifdef TARGET_ARM64
    bool IsContainableUnaryOrBinaryOp(GenTree* parentNode, GenTree* childNode) const;
#endif // TARGET_ARM64

#if defined(FEATURE_HW_INTRINSICS)
    bool IsContainableHWIntrinsicOp(GenTreeHWIntrinsic* parentNode, GenTree* childNode, bool* supportsRegOptional);
#endif // FEATURE_HW_INTRINSICS

    static void TransformUnusedIndirection(GenTreeIndir* ind, Compiler* comp, BasicBlock* block);

private:
    static bool NodesAreEquivalentLeaves(GenTree* candidate, GenTree* storeInd);

    bool AreSourcesPossiblyModifiedLocals(GenTree* addr, GenTree* base, GenTree* index);

    // Makes 'childNode' contained in the 'parentNode'
    void MakeSrcContained(GenTree* parentNode, GenTree* childNode) const;

    // Makes 'childNode' regOptional in the 'parentNode'
    void MakeSrcRegOptional(GenTree* parentNode, GenTree* childNode) const;

    // Tries to make 'childNode' contained or regOptional in the 'parentNode'
    void TryMakeSrcContainedOrRegOptional(GenTree* parentNode, GenTree* childNode) const;

#if defined(FEATURE_HW_INTRINSICS)
    // Tries to make 'childNode' contained or regOptional in the 'parentNode'
    void TryMakeSrcContainedOrRegOptional(GenTreeHWIntrinsic* parentNode, GenTree* childNode) const;
#endif

    // Checks and makes 'childNode' contained in the 'parentNode'
    bool CheckImmedAndMakeContained(GenTree* parentNode, GenTree* childNode);

    bool IsInvariantInRange(GenTree* node, GenTree* endExclusive) const;
    bool IsInvariantInRange(GenTree* node, GenTree* endExclusive, GenTree* ignoreNode) const;
    bool IsRangeInvariantInRange(GenTree* rangeStart,
                                 GenTree* rangeEnd,
                                 GenTree* endExclusive,
                                 GenTree* ignoreNode) const;

    // Checks for memory conflicts in the instructions between childNode and parentNode, and returns true if childNode
    // can be contained.
    bool IsSafeToContainMem(GenTree* parentNode, GenTree* childNode) const;

    // Similar to above, but allows bypassing a "transparent" parent.
    bool IsSafeToContainMem(GenTree* grandparentNode, GenTree* parentNode, GenTree* childNode) const;

    // Check if marking an operand of a node as reg-optional is safe.
    bool IsSafeToMarkRegOptional(GenTree* parentNode, GenTree* node) const;

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
        LclVarDsc* varDsc = comp->lvaGetDesc(lclNum);
        // Do a couple of simple checks before setting lvDoNotEnregister.
        // This may not cover all cases in 'isRegCandidate()' but we don't want to
        // do an expensive check here. For non-candidates it is not harmful to set lvDoNotEnregister.
        if (varDsc->lvTracked && !varDsc->lvDoNotEnregister)
        {
            assert(!m_lsra->isRegCandidate(varDsc));
            comp->lvaSetVarDoNotEnregister(lclNum DEBUG_ARG(DoNotEnregisterReason::LocalField));
        }
    }

    void RequireOutgoingArgSpace(GenTree* node, unsigned numBytes);
    void FinalizeOutgoingArgSpace();

    LinearScan*           m_lsra;
    unsigned              vtableCallTemp;       // local variable we use as a temp for vtable calls
    mutable SideEffectSet m_scratchSideEffects; // SideEffectSet used for IsSafeToContainMem and isRMWIndirCandidate
    BasicBlock*           m_block;

#ifdef FEATURE_FIXED_OUT_ARGS
    unsigned m_outgoingArgSpaceSize = 0;
#endif

#ifdef TARGET_ARM64
    struct SavedIndir
    {
        GenTreeIndir*  Indir;
        GenTreeLclVar* AddrBase;
        target_ssize_t Offset;

        SavedIndir(GenTreeIndir* indir, GenTreeLclVar* addrBase, target_ssize_t offset)
            : Indir(indir), AddrBase(addrBase), Offset(offset)
        {
        }
    };
    ArrayStack<SavedIndir> m_blockIndirs;
#endif
};

#endif // _LOWER_H_
