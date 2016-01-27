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

class Lowering : public Phase
{
public:
    inline Lowering(Compiler* compiler, LinearScanInterface* lsra)
        : Phase(compiler, "Lowering", PHASE_LOWERING),
        vtableCallTemp(BAD_VAR_NUM)
    {
        m_lsra = (LinearScan *)lsra;
        assert(m_lsra);
    }
    virtual void DoPhase();
    
    // If requiresOverflowCheck is false, all other values will be unset
    struct CastInfo
    {
        bool    requiresOverflowCheck;      // Will the cast require an overflow check
        bool    unsignedSource;             // Is the source unsigned
        bool    unsignedDest;               // is the dest unsigned

        // All other fields are only meaningful if requiresOverflowCheck is set.

        ssize_t typeMin;                    // Lowest storable value of the dest type
        ssize_t typeMax;                    // Highest storable value of the dest type
        ssize_t typeMask;                   // For converting from/to unsigned
        bool    signCheckOnly;              // For converting between unsigned/signed int
    };

#ifdef _TARGET_64BIT_
    static void getCastDescription(GenTreePtr treeNode, CastInfo* castInfo);
#endif // _TARGET_64BIT_

private:
    // Friends
    static Compiler::fgWalkResult LowerNodeHelper   (GenTreePtr* ppTree, Compiler::fgWalkData* data);
    static Compiler::fgWalkResult DecompNodeHelper  (GenTreePtr* ppTree, Compiler::fgWalkData* data);
    static Compiler::fgWalkResult TreeInfoInitHelper(GenTreePtr* ppTree, Compiler::fgWalkData* data);
    
    // Member Functions
    void DecomposeNode(GenTreePtr* tree, Compiler::fgWalkData* data);
    void LowerNode(GenTreePtr* tree, Compiler::fgWalkData* data);
    GenTreeStmt* LowerMorphAndSeqTree(GenTree *tree);
    void CheckVSQuirkStackPaddingNeeded(GenTreeCall* call);

    // ------------------------------
    // Call Lowering
    // ------------------------------
    void LowerCall                    (GenTree*     call);
    void LowerJmpMethod               (GenTree*     jmp);
    void LowerRet                     (GenTree*     ret);
    GenTree* LowerDelegateInvoke      (GenTreeCall* call);
    GenTree* LowerIndirectNonvirtCall (GenTreeCall* call);
    GenTree* LowerDirectCall          (GenTreeCall* call);
    GenTree* LowerNonvirtPinvokeCall  (GenTreeCall* call);
    GenTree* LowerTailCallViaHelper   (GenTreeCall* callNode, GenTree *callTarget);
    void     LowerFastTailCall        (GenTreeCall* callNode);
    void     InsertProfTailCallHook   (GenTreeCall* callNode, GenTree *insertionPoint);
    GenTree* LowerVirtualVtableCall   (GenTreeCall* call);
    GenTree* LowerVirtualStubCall     (GenTreeCall* call);
    void     LowerArgsForCall         (GenTreeCall* call);
    GenTree* NewPutArg                (GenTreeCall* call, GenTreePtr arg, fgArgTabEntryPtr fp, var_types type);
    void     LowerArg                 (GenTreeCall* call, GenTreePtr *ppTree);
    void     InsertPInvokeCallProlog  (GenTreeCall* call);
    void     InsertPInvokeCallEpilog  (GenTreeCall* call);
    void     InsertPInvokeMethodProlog();
    void     InsertPInvokeMethodEpilog(BasicBlock *returnBB DEBUGARG(GenTreePtr lastExpr));
    GenTree *SetGCState(int cns);
    GenTree *CreateReturnTrapSeq();
    enum FrameLinkAction { PushFrame, PopFrame };
    GenTree *CreateFrameLinkUpdate(FrameLinkAction);
    GenTree *AddrGen(ssize_t addr, regNumber reg = REG_NA);
    GenTree *AddrGen(void *addr, regNumber reg = REG_NA);

    // return concatenation of two trees, which currently uses a comma and really should not
    // because we're not supposed to have commas in codegen
    GenTree *Concat(GenTree *first, GenTree *second) 
    { 
        // if any is null, it must be the first
        if (first == nullptr)
        {
            return second;
        }
        else if (second == nullptr)
        {
            return first;
        }
        else
        {
            return comp->gtNewOperNode(GT_COMMA, TYP_I_IMPL, first, second); 
        }
    }

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
        return new(comp, GT_LEA) GenTreeAddrMode(resultType, base, nullptr, 0, offset);
    }

    // returns true if the tree can use the read-modify-write memory instruction form
    bool isRMWRegOper(GenTreePtr tree);
    
    // return true if this call target is within range of a pc-rel call on the machine
    bool IsCallTargetInRange(void *addr);

    void TreeNodeInfoInit(GenTree* stmt);
    void TreeNodeInfoInit(GenTreePtr* tree, GenTree* parent);
#ifdef _TARGET_ARM_
    void TreeNodeInfoInitCall(GenTreePtr tree, TreeNodeInfo &info, int &srcCount, int &dstCount);
#endif // _TARGET_ARM_
    void TreeNodeInfoInitStructArg(GenTreePtr structArg);
    void TreeNodeInfoInitBlockStore(GenTreeBlkOp* blkNode);
#ifdef FEATURE_SIMD
    void TreeNodeInfoInitSIMD(GenTree* tree, LinearScan* lsra);
#endif // FEATURE_SIMD

#if defined(_TARGET_XARCH_)
    void TreeNodeInfoInitSimple(GenTree* tree, TreeNodeInfo* info, unsigned kind);
#endif // defined(_TARGET_XARCH_)

    void SpliceInUnary(GenTreePtr parent, GenTreePtr* ppChild, GenTreePtr newNode);
    void DumpNodeInfoMap();

    // Per tree node member functions
    void LowerInd(GenTreePtr* ppTree);
    void LowerAddrMode(GenTreePtr* ppTree, GenTree* before, Compiler::fgWalkData* data, bool isIndir);
    void LowerAdd(GenTreePtr* ppTree, Compiler::fgWalkData* data);

    // Remove the nodes that are no longer used after an addressing mode is constructed under a GT_IND
    void LowerIndCleanupHelper(GenTreeAddrMode* addrMode, GenTreePtr tree);
    void LowerSwitch(GenTreePtr* ppTree);
    void LowerCast(GenTreePtr* ppTree);
    void LowerCntBlockOp(GenTreePtr* ppTree);

    void SetMulOpCounts(GenTreePtr tree);
    void LowerCmp(GenTreePtr tree);

#if !CPU_LOAD_STORE_ARCH
    bool IsBinOpInRMWStoreInd(GenTreePtr tree);
    bool IsRMWMemOpRootedAtStoreInd(GenTreePtr storeIndTree, GenTreePtr *indirCandidate, GenTreePtr *indirOpSource);
    bool SetStoreIndOpCountsIfRMWMemOp(GenTreePtr storeInd);
#endif
    void LowerStoreLoc(GenTreeLclVarCommon* tree);
    void SetIndirAddrOpCounts(GenTree *indirTree);
    void LowerGCWriteBarrier(GenTree *tree);
    void LowerArrElem(GenTree **ppTree, Compiler::fgWalkData* data);
    void LowerRotate(GenTree *tree);

    // Utility functions
    void MorphBlkIntoHelperCall         (GenTreePtr pTree, GenTreePtr treeStmt);
public:
    static bool IndirsAreEquivalent            (GenTreePtr pTreeA, GenTreePtr pTreeB);
private:
    static bool NodesAreEquivalentLeaves       (GenTreePtr candidate, GenTreePtr storeInd);

    GenTreePtr  CreateLocalTempAsg      (GenTreePtr rhs, unsigned refCount, GenTreePtr *ppLclVar = nullptr);

    bool AreSourcesPossiblyModified     (GenTree* use, GenTree* src1, GenTree *src2);
    void ReplaceNode                    (GenTree** ppTreeLocation,
                                         GenTree* replacementNode,
                                         GenTree* stmt,
                                         BasicBlock* block);

    void UnlinkNode                     (GenTree** ppParentLink, GenTree* stmt, BasicBlock* block);
    void SimpleLinkNodeAfter            (GenTree* prevTree, GenTree* newTree);

    // return true if 'childNode' is an immediate that can be contained 
    //  by the 'parentNode' (i.e. folded into an instruction)
    //  for example small enough and non-relocatable
    bool IsContainableImmed(GenTree* parentNode, GenTree* childNode);

    // Makes 'childNode' contained in the 'parentNode'
    void MakeSrcContained(GenTreePtr parentNode, GenTreePtr childNode);

    // Checks and makes 'childNode' contained in the 'parentNode'
    bool CheckImmedAndMakeContained(GenTree* parentNode, GenTree* childNode);
    
    // Checks for memory conflicts in the instructions between childNode and parentNode, and returns true if childNode can be contained.
    bool IsSafeToContainMem(GenTree* parentNode, GenTree* childNode);

    LinearScan *m_lsra;
    BasicBlock *currBlock;
    unsigned vtableCallTemp; // local variable we use as a temp for vtable calls
};

#endif // _LOWER_H_
