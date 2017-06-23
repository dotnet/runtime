// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                               Lower                                       XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifndef _LOWER_H_
#define _LOWER_H_

#include "compiler.h"
#include "phase.h"
#include "lsra.h"
#include "sideeffects.h"

class Lowering : public Phase
{
public:
    inline Lowering(Compiler* compiler, LinearScanInterface* lsra)
        : Phase(compiler, "Lowering", PHASE_LOWERING), vtableCallTemp(BAD_VAR_NUM)
    {
        m_lsra = (LinearScan*)lsra;
        assert(m_lsra);
    }
    virtual void DoPhase() override;

    // If requiresOverflowCheck is false, all other values will be unset
    struct CastInfo
    {
        bool requiresOverflowCheck; // Will the cast require an overflow check
        bool unsignedSource;        // Is the source unsigned
        bool unsignedDest;          // is the dest unsigned

        // All other fields are only meaningful if requiresOverflowCheck is set.

        ssize_t typeMin;       // Lowest storable value of the dest type
        ssize_t typeMax;       // Highest storable value of the dest type
        ssize_t typeMask;      // For converting from/to unsigned
        bool    signCheckOnly; // For converting between unsigned/signed int
    };

    static void getCastDescription(GenTreePtr treeNode, CastInfo* castInfo);

private:
#ifdef DEBUG
    static void CheckCallArg(GenTree* arg);
    static void CheckCall(GenTreeCall* call);
    static void CheckNode(GenTree* node);
    static bool CheckBlock(Compiler* compiler, BasicBlock* block);
#endif // DEBUG

    void LowerBlock(BasicBlock* block);
    GenTree* LowerNode(GenTree* node);
    void CheckVSQuirkStackPaddingNeeded(GenTreeCall* call);

    // ------------------------------
    // Call Lowering
    // ------------------------------
    void LowerCall(GenTree* call);
    void LowerCompare(GenTree* tree);
    void LowerJmpMethod(GenTree* jmp);
    void LowerRet(GenTree* ret);
    GenTree* LowerDelegateInvoke(GenTreeCall* call);
    GenTree* LowerIndirectNonvirtCall(GenTreeCall* call);
    GenTree* LowerDirectCall(GenTreeCall* call);
    GenTree* LowerNonvirtPinvokeCall(GenTreeCall* call);
    GenTree* LowerTailCallViaHelper(GenTreeCall* callNode, GenTree* callTarget);
    void LowerFastTailCall(GenTreeCall* callNode);
    void InsertProfTailCallHook(GenTreeCall* callNode, GenTree* insertionPoint);
    GenTree* LowerVirtualVtableCall(GenTreeCall* call);
    GenTree* LowerVirtualStubCall(GenTreeCall* call);
    void LowerArgsForCall(GenTreeCall* call);
    void ReplaceArgWithPutArgOrCopy(GenTreePtr* ppChild, GenTreePtr newNode);
    GenTree* NewPutArg(GenTreeCall* call, GenTreePtr arg, fgArgTabEntryPtr info, var_types type);
    void LowerArg(GenTreeCall* call, GenTreePtr* ppTree);
    void InsertPInvokeCallProlog(GenTreeCall* call);
    void InsertPInvokeCallEpilog(GenTreeCall* call);
    void InsertPInvokeMethodProlog();
    void InsertPInvokeMethodEpilog(BasicBlock* returnBB DEBUGARG(GenTreePtr lastExpr));
    GenTree* SetGCState(int cns);
    GenTree* CreateReturnTrapSeq();
    enum FrameLinkAction
    {
        PushFrame,
        PopFrame
    };
    GenTree* CreateFrameLinkUpdate(FrameLinkAction);
    GenTree* AddrGen(ssize_t addr, regNumber reg = REG_NA);
    GenTree* AddrGen(void* addr, regNumber reg = REG_NA);

    GenTree* Ind(GenTree* tree)
    {
        return comp->gtNewOperNode(GT_IND, TYP_I_IMPL, tree);
    }

    GenTree* PhysReg(regNumber reg, var_types type = TYP_I_IMPL)
    {
        return comp->gtNewPhysRegNode(reg, type);
    }

    GenTree* PhysRegDst(regNumber reg, GenTree* src)
    {
        return comp->gtNewPhysRegNode(reg, src);
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

    // returns true if the tree can use the read-modify-write memory instruction form
    bool isRMWRegOper(GenTreePtr tree);

    // return true if this call target is within range of a pc-rel call on the machine
    bool IsCallTargetInRange(void* addr);

#ifdef _TARGET_X86_
    bool ExcludeNonByteableRegisters(GenTree* tree);
#endif

    void TreeNodeInfoInit(GenTree* stmt);

    void TreeNodeInfoInitCheckByteable(GenTree* tree);

    void SetDelayFree(GenTree* delayUseSrc);

#if defined(_TARGET_XARCH_)
    void TreeNodeInfoInitSimple(GenTree* tree);

    //----------------------------------------------------------------------
    // SetRegOptional - sets a bit to indicate to LSRA that register
    // for a given tree node is optional for codegen purpose.  If no
    // register is allocated to such a tree node, its parent node treats
    // it as a contained memory operand during codegen.
    //
    // Arguments:
    //    tree    -   GenTree node
    //
    // Returns
    //    None
    void SetRegOptional(GenTree* tree)
    {
        tree->gtLsraInfo.regOptional = true;
    }

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
    //     tree  -  Gentree of a binary operation.
    //
    // Returns
    //     None.
    //
    // Note: On xarch at most only one of the operands will be marked as
    // reg optional, even when both operands could be considered register
    // optional.
    void SetRegOptionalForBinOp(GenTree* tree)
    {
        assert(GenTree::OperIsBinary(tree->OperGet()));

        GenTree* const op1 = tree->gtGetOp1();
        GenTree* const op2 = tree->gtGetOp2();

        const unsigned operatorSize = genTypeSize(tree->TypeGet());

        const bool op1Legal = tree->OperIsCommutative() && (operatorSize == genTypeSize(op1->TypeGet()));
        const bool op2Legal = operatorSize == genTypeSize(op2->TypeGet());

        if (op1Legal)
        {
            SetRegOptional(op2Legal ? PreferredRegOptionalOperand(tree) : op1);
        }
        else if (op2Legal)
        {
            SetRegOptional(op2);
        }
    }
#endif // defined(_TARGET_XARCH_)
    void TreeNodeInfoInitStoreLoc(GenTree* tree);
    void TreeNodeInfoInitReturn(GenTree* tree);
    void TreeNodeInfoInitShiftRotate(GenTree* tree);
    void TreeNodeInfoInitPutArgReg(
        GenTreeUnOp* node, regNumber argReg, TreeNodeInfo& info, bool isVarArgs, bool* callHasFloatRegArgs);
    void TreeNodeInfoInitCall(GenTreeCall* call);
    void TreeNodeInfoInitCmp(GenTreePtr tree);
    void TreeNodeInfoInitStructArg(GenTreePtr structArg);
    void TreeNodeInfoInitBlockStore(GenTreeBlk* blkNode);
    void TreeNodeInfoInitLogicalOp(GenTree* tree);
    void TreeNodeInfoInitModDiv(GenTree* tree);
    void TreeNodeInfoInitIntrinsic(GenTree* tree);
    void TreeNodeInfoInitStoreLoc(GenTreeLclVarCommon* tree);
    void TreeNodeInfoInitIndir(GenTree* indirTree);
    void TreeNodeInfoInitGCWriteBarrier(GenTree* tree);
#if !CPU_LOAD_STORE_ARCH
    bool TreeNodeInfoInitIfRMWMemOp(GenTreePtr storeInd);
#endif
#ifdef FEATURE_SIMD
    void TreeNodeInfoInitSIMD(GenTree* tree);
#endif // FEATURE_SIMD
    void TreeNodeInfoInitCast(GenTree* tree);
#ifdef _TARGET_ARM64_
    void LowerPutArgStk(GenTreePutArgStk* argNode, fgArgTabEntryPtr info);
    void TreeNodeInfoInitPutArgStk(GenTreePutArgStk* argNode, fgArgTabEntryPtr info);
#endif // _TARGET_ARM64_
#ifdef _TARGET_ARM_
    void LowerPutArgStk(GenTreePutArgStk* argNode, fgArgTabEntryPtr info);
    void TreeNodeInfoInitPutArgStk(GenTreePutArgStk* argNode, fgArgTabEntryPtr info);
#endif // _TARGET_ARM64_
#ifdef FEATURE_PUT_STRUCT_ARG_STK
    void LowerPutArgStk(GenTreePutArgStk* tree);
    void TreeNodeInfoInitPutArgStk(GenTreePutArgStk* tree);
#ifdef _TARGET_ARM_
    void TreeNodeInfoInitPutArgSplit(GenTreePutArgSplit* tree, TreeNodeInfo& info, fgArgTabEntryPtr argInfo);
#endif
#endif // FEATURE_PUT_STRUCT_ARG_STK
    void TreeNodeInfoInitLclHeap(GenTree* tree);

    void DumpNodeInfoMap();

    // Per tree node member functions
    void LowerStoreInd(GenTree* node);
    GenTree* LowerAdd(GenTree* node);
    GenTree* LowerUnsignedDivOrMod(GenTreeOp* divMod);
    GenTree* LowerSignedDivOrMod(GenTree* node);
    void LowerBlockStore(GenTreeBlk* blkNode);

    GenTree* TryCreateAddrMode(LIR::Use&& use, bool isIndir);
    void AddrModeCleanupHelper(GenTreeAddrMode* addrMode, GenTree* node);

    GenTree* LowerSwitch(GenTree* node);
    void LowerCast(GenTree* node);

#if defined(_TARGET_XARCH_)
    void TreeNodeInfoInitMul(GenTreePtr tree);
    void SetContainsAVXFlags(bool isFloatingPointType = true, unsigned sizeOfSIMDVector = 0);
#endif // defined(_TARGET_XARCH_)

#if !CPU_LOAD_STORE_ARCH
    bool IsRMWIndirCandidate(GenTree* operand, GenTree* storeInd);
    bool IsBinOpInRMWStoreInd(GenTreePtr tree);
    bool IsRMWMemOpRootedAtStoreInd(GenTreePtr storeIndTree, GenTreePtr* indirCandidate, GenTreePtr* indirOpSource);
#endif
    void LowerStoreLoc(GenTreeLclVarCommon* tree);
    GenTree* LowerArrElem(GenTree* node);
    void LowerRotate(GenTree* tree);
    void LowerShift(GenTreeOp* shift);

    // Utility functions
    void MorphBlkIntoHelperCall(GenTreePtr pTree, GenTreePtr treeStmt);

public:
    static bool IndirsAreEquivalent(GenTreePtr pTreeA, GenTreePtr pTreeB);

private:
    static bool NodesAreEquivalentLeaves(GenTreePtr candidate, GenTreePtr storeInd);

    bool AreSourcesPossiblyModifiedLocals(GenTree* addr, GenTree* base, GenTree* index);

    // return true if 'childNode' is an immediate that can be contained
    //  by the 'parentNode' (i.e. folded into an instruction)
    //  for example small enough and non-relocatable
    bool IsContainableImmed(GenTree* parentNode, GenTree* childNode);

    // Return true if 'node' is a containable memory op.
    bool IsContainableMemoryOp(GenTree* node, bool useTracked);

    // Makes 'childNode' contained in the 'parentNode'
    void MakeSrcContained(GenTreePtr parentNode, GenTreePtr childNode);

    // Checks and makes 'childNode' contained in the 'parentNode'
    bool CheckImmedAndMakeContained(GenTree* parentNode, GenTree* childNode);

    // Checks for memory conflicts in the instructions between childNode and parentNode, and returns true if childNode
    // can be contained.
    bool IsSafeToContainMem(GenTree* parentNode, GenTree* childNode);

    inline LIR::Range& BlockRange() const
    {
        return LIR::AsRange(m_block);
    }

    LinearScan*   m_lsra;
    unsigned      vtableCallTemp;       // local variable we use as a temp for vtable calls
    SideEffectSet m_scratchSideEffects; // SideEffectSet used for IsSafeToContainMem and isRMWIndirCandidate
    BasicBlock*   m_block;
};

#endif // _LOWER_H_
