// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

                 Linear Scan Register Allocation

                         a.k.a. LSRA

  Preconditions
    - All register requirements are expressed in the code stream, either as destination
      registers of tree nodes, or as internal registers.  These requirements are
      expressed in the RefPositions built for each node by BuildNode(), which includes:
      - The register uses and definitions.
      - The register restrictions (candidates) of the target register, both from itself,
        as producer of the value (dstCandidates), and from its consuming node (srcCandidates).
        Note that when we talk about srcCandidates we are referring to the destination register
        (not any of its sources).
      - The number (internalCount) of registers required, and their register restrictions (internalCandidates).
        These are neither inputs nor outputs of the node, but used in the sequence of code generated for the tree.
    "Internal registers" are registers used during the code sequence generated for the node.
    The register lifetimes must obey the following lifetime model:
    - First, any internal registers are defined.
    - Next, any source registers are used (and are then freed if they are last use and are not identified as
      "delayRegFree").
    - Next, the internal registers are used (and are then freed).
    - Next, any registers in the kill set for the instruction are killed.
    - Next, the destination register(s) are defined (multiple destination registers are only supported on ARM)
    - Finally, any "delayRegFree" source registers are freed.
  There are several things to note about this order:
    - The internal registers will never overlap any use, but they may overlap a destination register.
    - Internal registers are never live beyond the node.
    - The "delayRegFree" annotation is used for instructions that are only available in a Read-Modify-Write form.
      That is, the destination register is one of the sources.  In this case, we must not use the same register for
      the non-RMW operand as for the destination.

  Overview (doLinearScan):
    - Walk all blocks, building intervals and RefPositions (buildIntervals)
    - Allocate registers (allocateRegisters)
    - Annotate nodes with register assignments (resolveRegisters)
    - Add move nodes as needed to resolve conflicting register
      assignments across non-adjacent edges. (resolveEdges, called from resolveRegisters)

  Postconditions:

    Tree nodes (GenTree):
    - GenTree::GetRegNum() (and gtRegPair for ARM) is annotated with the register
      assignment for a node. If the node does not require a register, it is
      annotated as such (GetRegNum() = REG_NA). For a variable definition or interior
      tree node (an "implicit" definition), this is the register to put the result.
      For an expression use, this is the place to find the value that has previously
      been computed.
      - In most cases, this register must satisfy the constraints specified for the RefPosition.
      - In some cases, this is difficult:
        - If a lclVar node currently lives in some register, it may not be desirable to move it
          (i.e. its current location may be desirable for future uses, e.g. if it's a callee save register,
          but needs to be in a specific arg register for a call).
        - In other cases there may be conflicts on the restrictions placed by the defining node and the node which
          consumes it
      - If such a node is constrained to a single fixed register (e.g. an arg register, or a return from a call),
        then LSRA is free to annotate the node with a different register.  The code generator must issue the appropriate
        move.
      - However, if such a node is constrained to a set of registers, and its current location does not satisfy that
        requirement, LSRA must insert a GT_COPY node between the node and its parent.  The GetRegNum() on the GT_COPY
        node must satisfy the register requirement of the parent.
    - GenTree::gtRsvdRegs has a set of registers used for internal temps.
    - A tree node is marked GTF_SPILL if the tree node must be spilled by the code generator after it has been
      evaluated.
      - LSRA currently does not set GTF_SPILLED on such nodes, because it caused problems in the old code generator.
        In the new backend perhaps this should change (see also the note below under CodeGen).
    - A tree node is marked GTF_SPILLED if it is a lclVar that must be reloaded prior to use.
      - The register (GetRegNum()) on the node indicates the register to which it must be reloaded.
      - For lclVar nodes, since the uses and defs are distinct tree nodes, it is always possible to annotate the node
        with the register to which the variable must be reloaded.
      - For other nodes, since they represent both the def and use, if the value must be reloaded to a different
        register, LSRA must insert a GT_RELOAD node in order to specify the register to which it should be reloaded.

    Local variable table (LclVarDsc):
    - LclVarDsc::lvRegister is set to true if a local variable has the
      same register assignment for its entire lifetime.
    - LclVarDsc::lvRegNum / GetOtherReg(): these are initialized to their
      first value at the end of LSRA (it looks like GetOtherReg() isn't?
      This is probably a bug (ARM)). Codegen will set them to their current value
      as it processes the trees, since a variable can (now) be assigned different
      registers over its lifetimes.

XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "lsra.h"

#ifdef DEBUG
const char* LinearScan::resolveTypeName[] = {"Split", "Join", "Critical", "SharedCritical"};
#endif // DEBUG

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                    Small Helper functions                                 XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

//--------------------------------------------------------------
// lsraAssignRegToTree: Assign the given reg to tree node.
//
// Arguments:
//    tree    -    Gentree node
//    reg     -    register to be assigned
//    regIdx  -    register idx, if tree is a multi-reg call node.
//                 regIdx will be zero for single-reg result producing tree nodes.
//
// Return Value:
//    None
//
void lsraAssignRegToTree(GenTree* tree, regNumber reg, unsigned regIdx)
{
    if (regIdx == 0)
    {
        tree->SetRegNum(reg);
    }
#if !defined(TARGET_64BIT)
    else if (tree->OperIsMultiRegOp())
    {
        assert(regIdx == 1);
        GenTreeMultiRegOp* mul = tree->AsMultiRegOp();
        mul->gtOtherReg        = reg;
    }
#endif // TARGET_64BIT
#if FEATURE_MULTIREG_RET
    else if (tree->OperGet() == GT_COPY)
    {
        assert(regIdx == 1);
        GenTreeCopyOrReload* copy = tree->AsCopyOrReload();
        copy->gtOtherRegs[0]      = (regNumberSmall)reg;
    }
#endif // FEATURE_MULTIREG_RET
#if FEATURE_ARG_SPLIT
    else if (tree->OperIsPutArgSplit())
    {
        GenTreePutArgSplit* putArg = tree->AsPutArgSplit();
        putArg->SetRegNumByIdx(reg, regIdx);
    }
#endif // FEATURE_ARG_SPLIT
#if defined(TARGET_XARCH) && defined(FEATURE_HW_INTRINSICS)
    else if (tree->OperIs(GT_HWINTRINSIC))
    {
        assert(regIdx == 1);
        tree->AsHWIntrinsic()->SetOtherReg(reg);
    }
#endif
    else if (tree->OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR))
    {
        tree->AsLclVar()->SetRegNumByIdx(reg, regIdx);
    }
    else
    {
        assert(tree->IsMultiRegCall());
        GenTreeCall* call = tree->AsCall();
        call->SetRegNumByIdx(reg, regIdx);
    }
}

//-------------------------------------------------------------
// getWeight: Returns the weight of the RefPosition.
//
// Arguments:
//    refPos   -   ref position
//
// Returns:
//    Weight of ref position.
BasicBlock::weight_t LinearScan::getWeight(RefPosition* refPos)
{
    BasicBlock::weight_t weight;
    GenTree*             treeNode = refPos->treeNode;

    if (treeNode != nullptr)
    {
        if (isCandidateLocalRef(treeNode))
        {
            // Tracked locals: use weighted ref cnt as the weight of the
            // ref position.
            GenTreeLclVarCommon* lclCommon = treeNode->AsLclVarCommon();
            LclVarDsc*           varDsc    = &(compiler->lvaTable[lclCommon->GetLclNum()]);
            weight                         = varDsc->lvRefCntWtd();
            if (refPos->getInterval()->isSpilled)
            {
                // Decrease the weight if the interval has already been spilled.
                if (varDsc->lvLiveInOutOfHndlr)
                {
                    // An EH var is always spilled at defs, and we'll decrease the weight by half,
                    // since only the reload is needed.
                    weight = weight / 2;
                }
                else
                {
                    weight -= BB_UNITY_WEIGHT;
                }
            }
        }
        else
        {
            // Non-candidate local ref or non-lcl tree node.
            // These are considered to have two references in the basic block:
            // a def and a use and hence weighted ref count would be 2 times
            // the basic block weight in which they appear.
            // However, it is generally more harmful to spill tree temps, so we
            // double that.
            const unsigned TREE_TEMP_REF_COUNT    = 2;
            const unsigned TREE_TEMP_BOOST_FACTOR = 2;
            weight = TREE_TEMP_REF_COUNT * TREE_TEMP_BOOST_FACTOR * blockInfo[refPos->bbNum].weight;
        }
    }
    else
    {
        // Non-tree node ref positions.  These will have a single
        // reference in the basic block and hence their weighted
        // refcount is equal to the block weight in which they
        // appear.
        weight = blockInfo[refPos->bbNum].weight;
    }

    return weight;
}

// allRegs represents a set of registers that can
// be used to allocate the specified type in any point
// in time (more of a 'bank' of registers).
regMaskTP LinearScan::allRegs(RegisterType rt)
{
    assert((rt != TYP_UNDEF) && (rt != TYP_STRUCT));
    if (rt == TYP_FLOAT)
    {
        return availableFloatRegs;
    }
    else if (rt == TYP_DOUBLE)
    {
        return availableDoubleRegs;
    }
#ifdef FEATURE_SIMD
    // TODO-Cleanup: Add an RBM_ALLSIMD
    else if (varTypeIsSIMD(rt))
    {
        return availableDoubleRegs;
    }
#endif // FEATURE_SIMD
    else
    {
        return availableIntRegs;
    }
}

regMaskTP LinearScan::allByteRegs()
{
#ifdef TARGET_X86
    return availableIntRegs & RBM_BYTE_REGS;
#else
    return availableIntRegs;
#endif
}

regMaskTP LinearScan::allSIMDRegs()
{
    return availableFloatRegs;
}

void LinearScan::updateNextFixedRef(RegRecord* regRecord, RefPosition* nextRefPosition)
{
    LsraLocation nextLocation;

    if (nextRefPosition == nullptr)
    {
        nextLocation = MaxLocation;
        fixedRegs &= ~genRegMask(regRecord->regNum);
    }
    else
    {
        nextLocation = nextRefPosition->nodeLocation;
        fixedRegs |= genRegMask(regRecord->regNum);
    }
    nextFixedRef[regRecord->regNum] = nextLocation;
}

regMaskTP LinearScan::getMatchingConstants(regMaskTP mask, Interval* currentInterval, RefPosition* refPosition)
{
    assert(currentInterval->isConstant && RefTypeIsDef(refPosition->refType));
    regMaskTP candidates = (mask & m_RegistersWithConstants);
    regMaskTP result     = RBM_NONE;
    while (candidates != RBM_NONE)
    {
        regMaskTP candidateBit = genFindLowestBit(candidates);
        candidates &= ~candidateBit;
        regNumber  regNum        = genRegNumFromMask(candidateBit);
        RegRecord* physRegRecord = getRegisterRecord(regNum);
        if (isMatchingConstant(physRegRecord, refPosition))
        {
            result |= candidateBit;
        }
    }
    return result;
}

void LinearScan::clearNextIntervalRef(regNumber reg, var_types regType)
{
    nextIntervalRef[reg] = MaxLocation;
#ifdef TARGET_ARM
    if (regType == TYP_DOUBLE)
    {
        assert(genIsValidDoubleReg(reg));
        regNumber otherReg        = REG_NEXT(reg);
        nextIntervalRef[otherReg] = MaxLocation;
    }
#endif
}

void LinearScan::clearSpillCost(regNumber reg, var_types regType)
{
    spillCost[reg] = 0;
#ifdef TARGET_ARM
    if (regType == TYP_DOUBLE)
    {
        assert(genIsValidDoubleReg(reg));
        regNumber otherReg  = REG_NEXT(reg);
        spillCost[otherReg] = 0;
    }
#endif
}

void LinearScan::updateNextIntervalRef(regNumber reg, Interval* interval)
{
    LsraLocation nextRefLocation = interval->getNextRefLocation();
    nextIntervalRef[reg]         = nextRefLocation;
#ifdef TARGET_ARM
    if (interval->registerType == TYP_DOUBLE)
    {
        regNumber otherReg        = REG_NEXT(reg);
        nextIntervalRef[otherReg] = nextRefLocation;
    }
#endif
}

void LinearScan::updateSpillCost(regNumber reg, Interval* interval)
{
    // An interval can have no recentRefPosition if this is the initial assignment
    // of a parameter to its home register.
    float cost     = (interval->recentRefPosition != nullptr) ? getWeight(interval->recentRefPosition) : 0;
    spillCost[reg] = cost;
#ifdef TARGET_ARM
    if (interval->registerType == TYP_DOUBLE)
    {
        regNumber otherReg  = REG_NEXT(reg);
        spillCost[otherReg] = cost;
    }
#endif
}

//------------------------------------------------------------------------
// internalFloatRegCandidates: Return the set of registers that are appropriate
//                             for use as internal float registers.
//
// Return Value:
//    The set of registers (as a regMaskTP).
//
// Notes:
//    compFloatingPointUsed is only required to be set if it is possible that we
//    will use floating point callee-save registers.
//    It is unlikely, if an internal register is the only use of floating point,
//    that it will select a callee-save register.  But to be safe, we restrict
//    the set of candidates if compFloatingPointUsed is not already set.

regMaskTP LinearScan::internalFloatRegCandidates()
{
    if (compiler->compFloatingPointUsed)
    {
        return allRegs(TYP_FLOAT);
    }
    else
    {
        return RBM_FLT_CALLEE_TRASH;
    }
}

bool LinearScan::isFree(RegRecord* regRecord)
{
    return ((regRecord->assignedInterval == nullptr || !regRecord->assignedInterval->isActive) &&
            !isRegBusy(regRecord->regNum, regRecord->registerType));
}

RegRecord* LinearScan::getRegisterRecord(regNumber regNum)
{
    assert((unsigned)regNum < ArrLen(physRegs));
    return &physRegs[regNum];
}

#ifdef DEBUG

//----------------------------------------------------------------------------
// getConstrainedRegMask: Returns new regMask which is the intersection of
// regMaskActual and regMaskConstraint if the new regMask has at least
// minRegCount registers, otherwise returns regMaskActual.
//
// Arguments:
//     regMaskActual      -  regMask that needs to be constrained
//     regMaskConstraint  -  regMask constraint that needs to be
//                           applied to regMaskActual
//     minRegCount        -  Minimum number of regs that should be
//                           be present in new regMask.
//
// Return Value:
//     New regMask that has minRegCount registers after instersection.
//     Otherwise returns regMaskActual.
regMaskTP LinearScan::getConstrainedRegMask(regMaskTP regMaskActual, regMaskTP regMaskConstraint, unsigned minRegCount)
{
    regMaskTP newMask = regMaskActual & regMaskConstraint;
    if (genCountBits(newMask) >= minRegCount)
    {
        return newMask;
    }

    return regMaskActual;
}

//------------------------------------------------------------------------
// stressLimitRegs: Given a set of registers, expressed as a register mask, reduce
//            them based on the current stress options.
//
// Arguments:
//    mask      - The current mask of register candidates for a node
//
// Return Value:
//    A possibly-modified mask, based on the value of COMPlus_JitStressRegs.
//
// Notes:
//    This is the method used to implement the stress options that limit
//    the set of registers considered for allocation.

regMaskTP LinearScan::stressLimitRegs(RefPosition* refPosition, regMaskTP mask)
{
    if (getStressLimitRegs() != LSRA_LIMIT_NONE)
    {
        // The refPosition could be null, for example when called
        // by getTempRegForResolution().
        int minRegCount = (refPosition != nullptr) ? refPosition->minRegCandidateCount : 1;

        switch (getStressLimitRegs())
        {
            case LSRA_LIMIT_CALLEE:
                if (!compiler->opts.compDbgEnC)
                {
                    mask = getConstrainedRegMask(mask, RBM_CALLEE_SAVED, minRegCount);
                }
                break;

            case LSRA_LIMIT_CALLER:
            {
                mask = getConstrainedRegMask(mask, RBM_CALLEE_TRASH, minRegCount);
            }
            break;

            case LSRA_LIMIT_SMALL_SET:
                if ((mask & LsraLimitSmallIntSet) != RBM_NONE)
                {
                    mask = getConstrainedRegMask(mask, LsraLimitSmallIntSet, minRegCount);
                }
                else if ((mask & LsraLimitSmallFPSet) != RBM_NONE)
                {
                    mask = getConstrainedRegMask(mask, LsraLimitSmallFPSet, minRegCount);
                }
                break;

            default:
                unreached();
        }

        if (refPosition != nullptr && refPosition->isFixedRegRef)
        {
            mask |= refPosition->registerAssignment;
        }
    }

    return mask;
}
#endif // DEBUG

//------------------------------------------------------------------------
// conflictingFixedRegReference: Determine whether the 'reg' has a
//                               fixed register use that conflicts with 'refPosition'
//
// Arguments:
//    regNum      - The register of interest
//    refPosition - The RefPosition of interest
//
// Return Value:
//    Returns true iff the given RefPosition is NOT a fixed use of this register,
//    AND either:
//    - there is a RefPosition on this RegRecord at the nodeLocation of the given RefPosition, or
//    - the given RefPosition has a delayRegFree, and there is a RefPosition on this RegRecord at
//      the nodeLocation just past the given RefPosition.
//
// Assumptions:
//    'refPosition is non-null.

bool LinearScan::conflictingFixedRegReference(regNumber regNum, RefPosition* refPosition)
{
    // Is this a fixed reference of this register?  If so, there is no conflict.
    if (refPosition->isFixedRefOfRegMask(genRegMask(regNum)))
    {
        return false;
    }

    // Otherwise, check for conflicts.
    // There is a conflict if:
    // 1. There is a recent RefPosition on this RegRecord that is at this location, OR
    // 2. There is an upcoming RefPosition at this location, or at the next location
    //    if refPosition is a delayed use (i.e. must be kept live through the next/def location).

    LsraLocation refLocation = refPosition->nodeLocation;
    RegRecord*   regRecord   = getRegisterRecord(regNum);
    if (isRegInUse(regNum, refPosition->getInterval()->registerType) &&
        (regRecord->assignedInterval != refPosition->getInterval()))
    {
        return true;
    }

    LsraLocation nextPhysRefLocation = nextFixedRef[regNum];
    if (nextPhysRefLocation == refLocation || (refPosition->delayRegFree && nextPhysRefLocation == (refLocation + 1)))
    {
        return true;
    }
    return false;
}

/*****************************************************************************
 * Inline functions for Interval
 *****************************************************************************/
RefPosition* Referenceable::getNextRefPosition()
{
    if (recentRefPosition == nullptr)
    {
        return firstRefPosition;
    }
    else
    {
        return recentRefPosition->nextRefPosition;
    }
}

LsraLocation Referenceable::getNextRefLocation()
{
    RefPosition* nextRefPosition = getNextRefPosition();
    if (nextRefPosition == nullptr)
    {
        return MaxLocation;
    }
    else
    {
        return nextRefPosition->nodeLocation;
    }
}

#ifdef DEBUG
void LinearScan::dumpVarToRegMap(VarToRegMap map)
{
    bool anyPrinted = false;
    for (unsigned varIndex = 0; varIndex < compiler->lvaTrackedCount; varIndex++)
    {
        if (map[varIndex] != REG_STK)
        {
            printf("V%02u=%s ", compiler->lvaTrackedIndexToLclNum(varIndex), getRegName(map[varIndex]));
            anyPrinted = true;
        }
    }
    if (!anyPrinted)
    {
        printf("none");
    }
    printf("\n");
}

void LinearScan::dumpInVarToRegMap(BasicBlock* block)
{
    printf("Var=Reg beg of " FMT_BB ": ", block->bbNum);
    VarToRegMap map = getInVarToRegMap(block->bbNum);
    dumpVarToRegMap(map);
}

void LinearScan::dumpOutVarToRegMap(BasicBlock* block)
{
    printf("Var=Reg end of " FMT_BB ": ", block->bbNum);
    VarToRegMap map = getOutVarToRegMap(block->bbNum);
    dumpVarToRegMap(map);
}

#endif // DEBUG

LinearScanInterface* getLinearScanAllocator(Compiler* comp)
{
    return new (comp, CMK_LSRA) LinearScan(comp);
}

//------------------------------------------------------------------------
// LSRA constructor
//
// Arguments:
//    theCompiler
//
// Notes:
//    The constructor takes care of initializing the data structures that are used
//    during Lowering, including (in DEBUG) getting the stress environment variables,
//    as they may affect the block ordering.

LinearScan::LinearScan(Compiler* theCompiler)
    : compiler(theCompiler)
    , intervals(theCompiler->getAllocator(CMK_LSRA_Interval))
    , allocationPassComplete(false)
    , refPositions(theCompiler->getAllocator(CMK_LSRA_RefPosition))
    , listNodePool(theCompiler)
{
    regSelector  = new (theCompiler, CMK_LSRA) RegisterSelection(this);
    firstColdLoc = MaxLocation;

#ifdef DEBUG
    maxNodeLocation   = 0;
    activeRefPosition = nullptr;

    // Get the value of the environment variable that controls stress for register allocation
    lsraStressMask = JitConfig.JitStressRegs();

#if 0
    if (lsraStressMask != 0)
    {
        // The code in this #if can be used to debug JitStressRegs issues according to
        // method hash or method count.
        // To use, simply set environment variables:
        //   JitStressRegsHashLo and JitStressRegsHashHi to set the range of method hash, or
        //   JitStressRegsStart and JitStressRegsEnd to set the range of method count
        //     (Compiler::jitTotalMethodCount as reported by COMPlus_DumpJittedMethods).
        unsigned methHash = compiler->info.compMethodHash();
        char* lostr = getenv("JitStressRegsHashLo");
        unsigned methHashLo = 0;
        bool dump = false;
        if (lostr != nullptr)
        {
            sscanf_s(lostr, "%x", &methHashLo);
            dump = true;
        }
        char* histr = getenv("JitStressRegsHashHi");
        unsigned methHashHi = UINT32_MAX;
        if (histr != nullptr)
        {
            sscanf_s(histr, "%x", &methHashHi);
            dump = true;
        }
        if (methHash < methHashLo || methHash > methHashHi)
        {
            lsraStressMask = 0;
        }
        // Check method count
        unsigned count = Compiler::jitTotalMethodCompiled;
        unsigned start = 0;
        unsigned end = UINT32_MAX;
        char* startStr = getenv("JitStressRegsStart");
        char* endStr = getenv("JitStressRegsEnd");
        if (startStr != nullptr)
        {
            sscanf_s(startStr, "%d", &start);
            dump = true;
        }
        if (endStr != nullptr)
        {
            sscanf_s(endStr, "%d", &end);
            dump = true;
        }
        if (count < start || (count > end))
        {
            lsraStressMask = 0;
        }
        if ((lsraStressMask != 0) && (dump == true))
        {
            printf("JitStressRegs = %x for method %d: %s, hash = 0x%x.\n",
                lsraStressMask, Compiler::jitTotalMethodCompiled, compiler->info.compFullName, compiler->info.compMethodHash());
            printf("");         // flush
        }
    }
#endif // 0
#endif // DEBUG

    // Assume that we will enregister local variables if it's not disabled. We'll reset it if we
    // have no tracked locals when we start allocating. Note that new tracked lclVars may be added
    // after the first liveness analysis - either by optimizations or by Lowering, and the tracked
    // set won't be recomputed until after Lowering (and this constructor is called prior to Lowering),
    // so we don't want to check that yet.
    enregisterLocalVars = ((compiler->opts.compFlags & CLFLG_REGVAR) != 0);
#ifdef TARGET_ARM64
    availableIntRegs = (RBM_ALLINT & ~(RBM_PR | RBM_FP | RBM_LR) & ~compiler->codeGen->regSet.rsMaskResvd);
#else
    availableIntRegs   = (RBM_ALLINT & ~compiler->codeGen->regSet.rsMaskResvd);
#endif

#if ETW_EBP_FRAMED
    availableIntRegs &= ~RBM_FPBASE;
#endif // ETW_EBP_FRAMED

    availableFloatRegs  = RBM_ALLFLOAT;
    availableDoubleRegs = RBM_ALLDOUBLE;

#ifdef TARGET_AMD64
    if (compiler->opts.compDbgEnC)
    {
        // On x64 when the EnC option is set, we always save exactly RBP, RSI and RDI.
        // RBP is not available to the register allocator, so RSI and RDI are the only
        // callee-save registers available.
        availableIntRegs &= ~RBM_CALLEE_SAVED | RBM_RSI | RBM_RDI;
        availableFloatRegs &= ~RBM_CALLEE_SAVED;
        availableDoubleRegs &= ~RBM_CALLEE_SAVED;
    }
#endif // TARGET_AMD64
    compiler->rpFrameType           = FT_NOT_SET;
    compiler->rpMustCreateEBPCalled = false;

    compiler->codeGen->intRegState.rsIsFloat   = false;
    compiler->codeGen->floatRegState.rsIsFloat = true;

    // Block sequencing (the order in which we schedule).
    // Note that we don't initialize the bbVisitedSet until we do the first traversal
    // This is so that any blocks that are added during the first traversal
    // are accounted for (and we don't have BasicBlockEpoch issues).
    blockSequencingDone   = false;
    blockSequence         = nullptr;
    blockSequenceWorkList = nullptr;
    curBBSeqNum           = 0;
    bbSeqCount            = 0;

    // Information about each block, including predecessor blocks used for variable locations at block entry.
    blockInfo = nullptr;

    pendingDelayFree = false;
    tgtPrefUse       = nullptr;
}

//------------------------------------------------------------------------
// getNextCandidateFromWorkList: Get the next candidate for block sequencing
//
// Arguments:
//    None.
//
// Return Value:
//    The next block to be placed in the sequence.
//
// Notes:
//    This method currently always returns the next block in the list, and relies on having
//    blocks added to the list only when they are "ready", and on the
//    addToBlockSequenceWorkList() method to insert them in the proper order.
//    However, a block may be in the list and already selected, if it was subsequently
//    encountered as both a flow and layout successor of the most recently selected
//    block.

BasicBlock* LinearScan::getNextCandidateFromWorkList()
{
    BasicBlockList* nextWorkList = nullptr;
    for (BasicBlockList* workList = blockSequenceWorkList; workList != nullptr; workList = nextWorkList)
    {
        nextWorkList          = workList->next;
        BasicBlock* candBlock = workList->block;
        removeFromBlockSequenceWorkList(workList, nullptr);
        if (!isBlockVisited(candBlock))
        {
            return candBlock;
        }
    }
    return nullptr;
}

//------------------------------------------------------------------------
// setBlockSequence: Determine the block order for register allocation.
//
// Arguments:
//    None
//
// Return Value:
//    None
//
// Notes:
//    On return, the blockSequence array contains the blocks, in the order in which they
//    will be allocated.
//    This method clears the bbVisitedSet on LinearScan, and when it returns the set
//    contains all the bbNums for the block.

void LinearScan::setBlockSequence()
{
    assert(!blockSequencingDone); // The method should be called only once.

    compiler->EnsureBasicBlockEpoch();
#ifdef DEBUG
    blockEpoch = compiler->GetCurBasicBlockEpoch();
#endif // DEBUG

    // Initialize the "visited" blocks set.
    bbVisitedSet = BlockSetOps::MakeEmpty(compiler);

    BlockSet readySet(BlockSetOps::MakeEmpty(compiler));
    BlockSet predSet(BlockSetOps::MakeEmpty(compiler));

    assert(blockSequence == nullptr && bbSeqCount == 0);
    blockSequence            = new (compiler, CMK_LSRA) BasicBlock*[compiler->fgBBcount];
    bbNumMaxBeforeResolution = compiler->fgBBNumMax;
    blockInfo                = new (compiler, CMK_LSRA) LsraBlockInfo[bbNumMaxBeforeResolution + 1];

    assert(blockSequenceWorkList == nullptr);

    verifiedAllBBs   = false;
    hasCriticalEdges = false;
    BasicBlock* nextBlock;
    // We use a bbNum of 0 for entry RefPositions.
    // The other information in blockInfo[0] will never be used.
    blockInfo[0].weight = BB_UNITY_WEIGHT;
#if TRACK_LSRA_STATS
    for (int statIndex = 0; statIndex < LsraStat::COUNT; statIndex++)
    {
        blockInfo[0].stats[statIndex] = 0;
    }
#endif // TRACK_LSRA_STATS

    for (BasicBlock* block = compiler->fgFirstBB; block != nullptr; block = nextBlock)
    {
        blockSequence[bbSeqCount] = block;
        markBlockVisited(block);
        bbSeqCount++;
        nextBlock = nullptr;

        // Initialize the blockInfo.
        // predBBNum will be set later.
        // 0 is never used as a bbNum, but is used in blockInfo to designate an exception entry block.
        blockInfo[block->bbNum].predBBNum = 0;
        // We check for critical edges below, but initialize to false.
        blockInfo[block->bbNum].hasCriticalInEdge  = false;
        blockInfo[block->bbNum].hasCriticalOutEdge = false;
        blockInfo[block->bbNum].weight             = block->getBBWeight(compiler);
        blockInfo[block->bbNum].hasEHBoundaryIn    = block->hasEHBoundaryIn();
        blockInfo[block->bbNum].hasEHBoundaryOut   = block->hasEHBoundaryOut();
        blockInfo[block->bbNum].hasEHPred          = false;

#if TRACK_LSRA_STATS
        for (int statIndex = 0; statIndex < LsraStat::COUNT; statIndex++)
        {
            blockInfo[block->bbNum].stats[statIndex] = 0;
        }
#endif // TRACK_LSRA_STATS

        // We treat BBCallAlwaysPairTail blocks as having EH flow, since we can't
        // insert resolution moves into those blocks.
        if (block->isBBCallAlwaysPairTail())
        {
            blockInfo[block->bbNum].hasEHBoundaryIn  = true;
            blockInfo[block->bbNum].hasEHBoundaryOut = true;
        }

        bool hasUniquePred = (block->GetUniquePred(compiler) != nullptr);
        for (flowList* pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
        {
            BasicBlock* predBlock = pred->getBlock();
            if (!hasUniquePred)
            {
                if (predBlock->NumSucc(compiler) > 1)
                {
                    blockInfo[block->bbNum].hasCriticalInEdge = true;
                    hasCriticalEdges                          = true;
                }
                else if (predBlock->bbJumpKind == BBJ_SWITCH)
                {
                    assert(!"Switch with single successor");
                }
            }

            if (!block->isBBCallAlwaysPairTail() &&
                (predBlock->hasEHBoundaryOut() || predBlock->isBBCallAlwaysPairTail()))
            {
                assert(!block->isBBCallAlwaysPairTail());

                if (hasUniquePred)
                {
                    // A unique pred with an EH out edge won't allow us to keep any variables enregistered.
                    blockInfo[block->bbNum].hasEHBoundaryIn = true;
                }
                else
                {
                    blockInfo[block->bbNum].hasEHPred = true;
                }
            }
        }

        // Determine which block to schedule next.

        // First, update the NORMAL successors of the current block, adding them to the worklist
        // according to the desired order.  We will handle the EH successors below.
        bool checkForCriticalOutEdge = (block->NumSucc(compiler) > 1);
        if (!checkForCriticalOutEdge && block->bbJumpKind == BBJ_SWITCH)
        {
            assert(!"Switch with single successor");
        }

        const unsigned numSuccs = block->NumSucc(compiler);
        for (unsigned succIndex = 0; succIndex < numSuccs; succIndex++)
        {
            BasicBlock* succ = block->GetSucc(succIndex, compiler);
            if (checkForCriticalOutEdge && succ->GetUniquePred(compiler) == nullptr)
            {
                blockInfo[block->bbNum].hasCriticalOutEdge = true;
                hasCriticalEdges                           = true;
                // We can stop checking now.
                checkForCriticalOutEdge = false;
            }

            if (isTraversalLayoutOrder() || isBlockVisited(succ))
            {
                continue;
            }

            // We've now seen a predecessor, so add it to the work list and the "readySet".
            // It will be inserted in the worklist according to the specified traversal order
            // (i.e. pred-first or random, since layout order is handled above).
            if (!BlockSetOps::IsMember(compiler, readySet, succ->bbNum))
            {
                addToBlockSequenceWorkList(readySet, succ, predSet);
                BlockSetOps::AddElemD(compiler, readySet, succ->bbNum);
            }
        }

        // For layout order, simply use bbNext
        if (isTraversalLayoutOrder())
        {
            nextBlock = block->bbNext;
            continue;
        }

        while (nextBlock == nullptr)
        {
            nextBlock = getNextCandidateFromWorkList();

            // TODO-Throughput: We would like to bypass this traversal if we know we've handled all
            // the blocks - but fgBBcount does not appear to be updated when blocks are removed.
            if (nextBlock == nullptr /* && bbSeqCount != compiler->fgBBcount*/ && !verifiedAllBBs)
            {
                // If we don't encounter all blocks by traversing the regular successor links, do a full
                // traversal of all the blocks, and add them in layout order.
                // This may include:
                //   - internal-only blocks (in the fgAddCodeList) which may not be in the flow graph
                //     (these are not even in the bbNext links).
                //   - blocks that have become unreachable due to optimizations, but that are strongly
                //     connected (these are not removed)
                //   - EH blocks

                for (Compiler::AddCodeDsc* desc = compiler->fgAddCodeList; desc != nullptr; desc = desc->acdNext)
                {
                    if (!isBlockVisited(block))
                    {
                        addToBlockSequenceWorkList(readySet, block, predSet);
                        BlockSetOps::AddElemD(compiler, readySet, block->bbNum);
                    }
                }

                for (BasicBlock* block = compiler->fgFirstBB; block; block = block->bbNext)
                {
                    if (!isBlockVisited(block))
                    {
                        addToBlockSequenceWorkList(readySet, block, predSet);
                        BlockSetOps::AddElemD(compiler, readySet, block->bbNum);
                    }
                }
                verifiedAllBBs = true;
            }
            else
            {
                break;
            }
        }
    }
    blockSequencingDone = true;

#ifdef DEBUG
    // Make sure that we've visited all the blocks.
    for (BasicBlock* block = compiler->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        assert(isBlockVisited(block));
    }

    JITDUMP("LSRA Block Sequence: ");
    int i = 1;
    for (BasicBlock *block = startBlockSequence(); block != nullptr; ++i, block = moveToNextBlock())
    {
        JITDUMP(FMT_BB, block->bbNum);

        if (block->isMaxBBWeight())
        {
            JITDUMP("(MAX) ");
        }
        else
        {
            JITDUMP("(%6s) ", refCntWtd2str(block->getBBWeight(compiler)));
        }

        if (blockInfo[block->bbNum].hasEHBoundaryIn)
        {
            JITDUMP(" EH-in");
        }
        if (blockInfo[block->bbNum].hasEHBoundaryOut)
        {
            JITDUMP(" EH-out");
        }
        if (blockInfo[block->bbNum].hasEHPred)
        {
            JITDUMP(" has EH pred");
        }
        JITDUMP("\n");
    }
    JITDUMP("\n");
#endif
}

//------------------------------------------------------------------------
// compareBlocksForSequencing: Compare two basic blocks for sequencing order.
//
// Arguments:
//    block1            - the first block for comparison
//    block2            - the second block for comparison
//    useBlockWeights   - whether to use block weights for comparison
//
// Return Value:
//    -1 if block1 is preferred.
//     0 if the blocks are equivalent.
//     1 if block2 is preferred.
//
// Notes:
//    See addToBlockSequenceWorkList.
int LinearScan::compareBlocksForSequencing(BasicBlock* block1, BasicBlock* block2, bool useBlockWeights)
{
    if (useBlockWeights)
    {
        BasicBlock::weight_t weight1 = block1->getBBWeight(compiler);
        BasicBlock::weight_t weight2 = block2->getBBWeight(compiler);

        if (weight1 > weight2)
        {
            return -1;
        }
        else if (weight1 < weight2)
        {
            return 1;
        }
    }

    // If weights are the same prefer LOWER bbnum
    if (block1->bbNum < block2->bbNum)
    {
        return -1;
    }
    else if (block1->bbNum == block2->bbNum)
    {
        return 0;
    }
    else
    {
        return 1;
    }
}

//------------------------------------------------------------------------
// addToBlockSequenceWorkList: Add a BasicBlock to the work list for sequencing.
//
// Arguments:
//    sequencedBlockSet - the set of blocks that are already sequenced
//    block             - the new block to be added
//    predSet           - the buffer to save predecessors set. A block set allocated by the caller used here as a
//    temporary block set for constructing a predecessor set. Allocated by the caller to avoid reallocating a new block
//    set with every call to this function
//
// Return Value:
//    None.
//
// Notes:
//    The first block in the list will be the next one to be sequenced, as soon
//    as we encounter a block whose successors have all been sequenced, in pred-first
//    order, or the very next block if we are traversing in random order (once implemented).
//    This method uses a comparison method to determine the order in which to place
//    the blocks in the list.  This method queries whether all predecessors of the
//    block are sequenced at the time it is added to the list and if so uses block weights
//    for inserting the block.  A block is never inserted ahead of its predecessors.
//    A block at the time of insertion may not have all its predecessors sequenced, in
//    which case it will be sequenced based on its block number. Once a block is inserted,
//    its priority\order will not be changed later once its remaining predecessors are
//    sequenced.  This would mean that work list may not be sorted entirely based on
//    block weights alone.
//
//    Note also that, when random traversal order is implemented, this method
//    should insert the blocks into the list in random order, so that we can always
//    simply select the first block in the list.
void LinearScan::addToBlockSequenceWorkList(BlockSet sequencedBlockSet, BasicBlock* block, BlockSet& predSet)
{
    // The block that is being added is not already sequenced
    assert(!BlockSetOps::IsMember(compiler, sequencedBlockSet, block->bbNum));

    // Get predSet of block
    BlockSetOps::ClearD(compiler, predSet);
    flowList* pred;
    for (pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
    {
        BlockSetOps::AddElemD(compiler, predSet, pred->getBlock()->bbNum);
    }

    // If either a rarely run block or all its preds are already sequenced, use block's weight to sequence
    bool useBlockWeight = block->isRunRarely() || BlockSetOps::IsSubset(compiler, sequencedBlockSet, predSet);

    BasicBlockList* prevNode = nullptr;
    BasicBlockList* nextNode = blockSequenceWorkList;

    while (nextNode != nullptr)
    {
        int seqResult;

        if (nextNode->block->isRunRarely())
        {
            // If the block that is yet to be sequenced is a rarely run block, always use block weights for sequencing
            seqResult = compareBlocksForSequencing(nextNode->block, block, true);
        }
        else if (BlockSetOps::IsMember(compiler, predSet, nextNode->block->bbNum))
        {
            // always prefer unsequenced pred blocks
            seqResult = -1;
        }
        else
        {
            seqResult = compareBlocksForSequencing(nextNode->block, block, useBlockWeight);
        }

        if (seqResult > 0)
        {
            break;
        }

        prevNode = nextNode;
        nextNode = nextNode->next;
    }

    BasicBlockList* newListNode = new (compiler, CMK_LSRA) BasicBlockList(block, nextNode);
    if (prevNode == nullptr)
    {
        blockSequenceWorkList = newListNode;
    }
    else
    {
        prevNode->next = newListNode;
    }
}

void LinearScan::removeFromBlockSequenceWorkList(BasicBlockList* listNode, BasicBlockList* prevNode)
{
    if (listNode == blockSequenceWorkList)
    {
        assert(prevNode == nullptr);
        blockSequenceWorkList = listNode->next;
    }
    else
    {
        assert(prevNode != nullptr && prevNode->next == listNode);
        prevNode->next = listNode->next;
    }
    // TODO-Cleanup: consider merging Compiler::BlockListNode and BasicBlockList
    // compiler->FreeBlockListNode(listNode);
}

// Initialize the block order for allocation (called each time a new traversal begins).
BasicBlock* LinearScan::startBlockSequence()
{
    if (!blockSequencingDone)
    {
        setBlockSequence();
    }
    else
    {
        clearVisitedBlocks();
    }

    BasicBlock* curBB = compiler->fgFirstBB;
    curBBSeqNum       = 0;
    curBBNum          = curBB->bbNum;
    assert(blockSequence[0] == compiler->fgFirstBB);
    markBlockVisited(curBB);
    return curBB;
}

//------------------------------------------------------------------------
// moveToNextBlock: Move to the next block in order for allocation or resolution.
//
// Arguments:
//    None
//
// Return Value:
//    The next block.
//
// Notes:
//    This method is used when the next block is actually going to be handled.
//    It changes curBBNum.

BasicBlock* LinearScan::moveToNextBlock()
{
    BasicBlock* nextBlock = getNextBlock();
    curBBSeqNum++;
    if (nextBlock != nullptr)
    {
        curBBNum = nextBlock->bbNum;
    }
    return nextBlock;
}

//------------------------------------------------------------------------
// getNextBlock: Get the next block in order for allocation or resolution.
//
// Arguments:
//    None
//
// Return Value:
//    The next block.
//
// Notes:
//    This method does not actually change the current block - it is used simply
//    to determine which block will be next.

BasicBlock* LinearScan::getNextBlock()
{
    assert(blockSequencingDone);
    unsigned int nextBBSeqNum = curBBSeqNum + 1;
    if (nextBBSeqNum < bbSeqCount)
    {
        return blockSequence[nextBBSeqNum];
    }
    return nullptr;
}

//------------------------------------------------------------------------
// doLinearScan: The main method for register allocation.
//
// Arguments:
//    None
//
// Return Value:
//    None.
//

void LinearScan::doLinearScan()
{
    // Check to see whether we have any local variables to enregister.
    // We initialize this in the constructor based on opt settings,
    // but we don't want to spend time on the lclVar parts of LinearScan
    // if we have no tracked locals.
    if (enregisterLocalVars && (compiler->lvaTrackedCount == 0))
    {
        enregisterLocalVars = false;
    }

    splitBBNumToTargetBBNumMap = nullptr;

    // This is complicated by the fact that physical registers have refs associated
    // with locations where they are killed (e.g. calls), but we don't want to
    // count these as being touched.

    compiler->codeGen->regSet.rsClearRegsModified();

    initMaxSpill();
    buildIntervals();
    DBEXEC(VERBOSE, TupleStyleDump(LSRA_DUMP_REFPOS));
    compiler->EndPhase(PHASE_LINEAR_SCAN_BUILD);

    DBEXEC(VERBOSE, lsraDumpIntervals("after buildIntervals"));

    initVarRegMaps();
    allocateRegisters();
    allocationPassComplete = true;
    compiler->EndPhase(PHASE_LINEAR_SCAN_ALLOC);
    resolveRegisters();
    compiler->EndPhase(PHASE_LINEAR_SCAN_RESOLVE);

    assert(blockSequencingDone); // Should do at least one traversal.
    assert(blockEpoch == compiler->GetCurBasicBlockEpoch());

#if TRACK_LSRA_STATS
    if ((JitConfig.DisplayLsraStats() == 1)
#ifdef DEBUG
        || VERBOSE
#endif
        )
    {
        dumpLsraStats(jitstdout);
    }
#endif // TRACK_LSRA_STATS

    DBEXEC(VERBOSE, TupleStyleDump(LSRA_DUMP_POST));

    compiler->compLSRADone = true;
}

//------------------------------------------------------------------------
// recordVarLocationsAtStartOfBB: Update live-in LclVarDscs with the appropriate
//    register location at the start of a block, during codegen.
//
// Arguments:
//    bb - the block for which code is about to be generated.
//
// Return Value:
//    None.
//
// Assumptions:
//    CodeGen will take care of updating the reg masks and the current var liveness,
//    after calling this method.
//    This is because we need to kill off the dead registers before setting the newly live ones.

void LinearScan::recordVarLocationsAtStartOfBB(BasicBlock* bb)
{
    if (!enregisterLocalVars)
    {
        return;
    }
    JITDUMP("Recording Var Locations at start of " FMT_BB "\n", bb->bbNum);
    VarToRegMap map   = getInVarToRegMap(bb->bbNum);
    unsigned    count = 0;

    VarSetOps::AssignNoCopy(compiler, currentLiveVars,
                            VarSetOps::Intersection(compiler, registerCandidateVars, bb->bbLiveIn));
    VarSetOps::Iter iter(compiler, currentLiveVars);
    unsigned        varIndex = 0;
    while (iter.NextElem(&varIndex))
    {
        unsigned   varNum = compiler->lvaTrackedIndexToLclNum(varIndex);
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);

        regNumber oldRegNum = varDsc->GetRegNum();
        regNumber newRegNum = getVarReg(map, varIndex);

        if (oldRegNum != newRegNum)
        {
            JITDUMP("  V%02u(%s->%s)", varNum, compiler->compRegVarName(oldRegNum),
                    compiler->compRegVarName(newRegNum));
            varDsc->SetRegNum(newRegNum);
            count++;

#ifdef USING_VARIABLE_LIVE_RANGE
            if (bb->bbPrev != nullptr && VarSetOps::IsMember(compiler, bb->bbPrev->bbLiveOut, varIndex))
            {
                // varDsc was alive on previous block end ("bb->bbPrev->bbLiveOut"), so it has an open
                // "VariableLiveRange" which should change to be according "getInVarToRegMap"
                compiler->codeGen->getVariableLiveKeeper()->siUpdateVariableLiveRange(varDsc, varNum);
            }
#endif // USING_VARIABLE_LIVE_RANGE
        }
        else if (newRegNum != REG_STK)
        {
            JITDUMP("  V%02u(%s)", varNum, compiler->compRegVarName(newRegNum));
            count++;
        }
    }

    if (count == 0)
    {
        JITDUMP("  <none>\n");
    }

    JITDUMP("\n");
}

void Interval::setLocalNumber(Compiler* compiler, unsigned lclNum, LinearScan* linScan)
{
    LclVarDsc* varDsc = &compiler->lvaTable[lclNum];
    assert(varDsc->lvTracked);
    assert(varDsc->lvVarIndex < compiler->lvaTrackedCount);

    linScan->localVarIntervals[varDsc->lvVarIndex] = this;

    assert(linScan->getIntervalForLocalVar(varDsc->lvVarIndex) == this);
    this->isLocalVar = true;
    this->varNum     = lclNum;
}

//------------------------------------------------------------------------
// LinearScan:identifyCandidatesExceptionDataflow: Build the set of variables exposed on EH flow edges
//
// Notes:
//    This logic was originally cloned from fgInterBlockLocalVarLiveness.
//
void LinearScan::identifyCandidatesExceptionDataflow()
{
    BasicBlock* block;

    foreach_block(compiler, block)
    {
        if (block->hasEHBoundaryIn())
        {
            // live on entry to handler
            VarSetOps::UnionD(compiler, exceptVars, block->bbLiveIn);
        }

        if (block->hasEHBoundaryOut())
        {
            VarSetOps::UnionD(compiler, exceptVars, block->bbLiveOut);
            if (block->bbJumpKind == BBJ_EHFINALLYRET)
            {
                // Live on exit from finally.
                // We track these separately because, in addition to having EH live-out semantics,
                // we need to mark them must-init.
                VarSetOps::UnionD(compiler, finallyVars, block->bbLiveOut);
            }
        }
    }

#ifdef DEBUG
    if (VERBOSE)
    {
        JITDUMP("EH Vars: ");
        INDEBUG(dumpConvertedVarSet(compiler, exceptVars));
        JITDUMP("\nFinally Vars: ");
        INDEBUG(dumpConvertedVarSet(compiler, finallyVars));
        JITDUMP("\n\n");
    }

    // All variables live on exit from a 'finally' block should be marked lvLiveInOutOfHndlr.
    // and as 'explicitly initialized' (must-init) for GC-ref types.
    VarSetOps::Iter iter(compiler, exceptVars);
    unsigned        varIndex = 0;
    while (iter.NextElem(&varIndex))
    {
        unsigned   varNum = compiler->lvaTrackedIndexToLclNum(varIndex);
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);

        assert(varDsc->lvLiveInOutOfHndlr);

        if (varTypeIsGC(varDsc) && VarSetOps::IsMember(compiler, finallyVars, varIndex) && !varDsc->lvIsParam)
        {
            assert(varDsc->lvMustInit);
        }
    }
#endif
}

bool LinearScan::isRegCandidate(LclVarDsc* varDsc)
{
    if (!enregisterLocalVars)
    {
        return false;
    }
    assert((compiler->opts.compFlags & CLFLG_REGVAR) != 0);

    if (!varDsc->lvTracked)
    {
        return false;
    }

#if !defined(TARGET_64BIT)
    if (varDsc->lvType == TYP_LONG)
    {
        // Long variables should not be register candidates.
        // Lowering will have split any candidate lclVars into lo/hi vars.
        return false;
    }
#endif // !defined(TARGET_64BIT)

    // If we have JMP, reg args must be put on the stack

    if (compiler->compJmpOpUsed && varDsc->lvIsRegArg)
    {
        return false;
    }

    // Don't allocate registers for dependently promoted struct fields
    if (compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc))
    {
        return false;
    }

    // Don't enregister if the ref count is zero.
    if (varDsc->lvRefCnt() == 0)
    {
        varDsc->setLvRefCntWtd(0);
        return false;
    }

    // Variables that are address-exposed are never enregistered, or tracked.
    // A struct may be promoted, and a struct that fits in a register may be fully enregistered.
    // Pinned variables may not be tracked (a condition of the GCInfo representation)
    // or enregistered, on x86 -- it is believed that we can enregister pinned (more properly, "pinning")
    // references when using the general GC encoding.
    unsigned lclNum = (unsigned)(varDsc - compiler->lvaTable);
    if (varDsc->lvAddrExposed || !varDsc->IsEnregisterable() ||
        (!compiler->compEnregStructLocals() && (varDsc->lvType == TYP_STRUCT)))
    {
#ifdef DEBUG
        Compiler::DoNotEnregisterReason dner;
        if (varDsc->lvAddrExposed)
        {
            dner = Compiler::DNER_AddrExposed;
        }
        else
        {
            dner = Compiler::DNER_IsStruct;
        }
#endif // DEBUG
        compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(dner));
        return false;
    }
    else if (varDsc->lvPinned)
    {
        varDsc->lvTracked = 0;
#ifdef JIT32_GCENCODER
        compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(Compiler::DNER_PinningRef));
#endif // JIT32_GCENCODER
        return false;
    }

    //  Are we not optimizing and we have exception handlers?
    //   if so mark all args and locals as volatile, so that they
    //   won't ever get enregistered.
    //
    if (compiler->opts.MinOpts() && compiler->compHndBBtabCount > 0)
    {
        compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(Compiler::DNER_LiveInOutOfHandler));
    }

    if (varDsc->lvDoNotEnregister)
    {
        return false;
    }

    switch (genActualType(varDsc->TypeGet()))
    {
        case TYP_FLOAT:
        case TYP_DOUBLE:
            return !compiler->opts.compDbgCode;

        case TYP_INT:
        case TYP_LONG:
        case TYP_REF:
        case TYP_BYREF:
            break;

#ifdef FEATURE_SIMD
        case TYP_SIMD8:
        case TYP_SIMD12:
        case TYP_SIMD16:
        case TYP_SIMD32:
            return !varDsc->lvPromoted;
#endif // FEATURE_SIMD

        case TYP_STRUCT:
            // TODO-1stClassStructs: support vars with GC pointers. The issue is that such
            // vars will have `lvMustInit` set, because emitter has poor support for struct liveness,
            // but if the variable is tracked the prolog generator would expect it to be in liveIn set,
            // so an assert in `genFnProlog` will fire.
            return compiler->compEnregStructLocals() && !varDsc->HasGCPtr();

        case TYP_UNDEF:
        case TYP_UNKNOWN:
            noway_assert(!"lvType not set correctly");
            varDsc->lvType = TYP_INT;
            return false;

        default:
            return false;
    }

    return true;
}

// Identify locals & compiler temps that are register candidates
// TODO-Cleanup: This was cloned from Compiler::lvaSortByRefCount() in lclvars.cpp in order
// to avoid perturbation, but should be merged.

void LinearScan::identifyCandidates()
{
    if (enregisterLocalVars)
    {
        // Initialize the set of lclVars that are candidates for register allocation.
        VarSetOps::AssignNoCopy(compiler, registerCandidateVars, VarSetOps::MakeEmpty(compiler));

        // Initialize the sets of lclVars that are used to determine whether, and for which lclVars,
        // we need to perform resolution across basic blocks.
        // Note that we can't do this in the constructor because the number of tracked lclVars may
        // change between the constructor and the actual allocation.
        VarSetOps::AssignNoCopy(compiler, resolutionCandidateVars, VarSetOps::MakeEmpty(compiler));
        VarSetOps::AssignNoCopy(compiler, splitOrSpilledVars, VarSetOps::MakeEmpty(compiler));

        // We set enregisterLocalVars to true only if there are tracked lclVars
        assert(compiler->lvaCount != 0);
    }
    else if (compiler->lvaCount == 0)
    {
        // Nothing to do. Note that even if enregisterLocalVars is false, we still need to set the
        // lvLRACandidate field on all the lclVars to false if we have any.
        return;
    }

    VarSetOps::AssignNoCopy(compiler, exceptVars, VarSetOps::MakeEmpty(compiler));
    VarSetOps::AssignNoCopy(compiler, finallyVars, VarSetOps::MakeEmpty(compiler));
    if (compiler->compHndBBtabCount > 0)
    {
        identifyCandidatesExceptionDataflow();
    }

    unsigned   lclNum;
    LclVarDsc* varDsc;

    // While we build intervals for the candidate lclVars, we will determine the floating point
    // lclVars, if any, to consider for callee-save register preferencing.
    // We maintain two sets of FP vars - those that meet the first threshold of weighted ref Count,
    // and those that meet the second.
    // The first threshold is used for methods that are heuristically deemed either to have light
    // fp usage, or other factors that encourage conservative use of callee-save registers, such
    // as multiple exits (where there might be an early exit that woudl be excessively penalized by
    // lots of prolog/epilog saves & restores).
    // The second threshold is used where there are factors deemed to make it more likely that fp
    // fp callee save registers will be needed, such as loops or many fp vars.
    // We keep two sets of vars, since we collect some of the information to determine which set to
    // use as we iterate over the vars.
    // When we are generating AVX code on non-Unix (FEATURE_PARTIAL_SIMD_CALLEE_SAVE), we maintain an
    // additional set of LargeVectorType vars, and there is a separate threshold defined for those.
    // It is assumed that if we encounter these, that we should consider this a "high use" scenario,
    // so we don't maintain two sets of these vars.
    // This is defined as thresholdLargeVectorRefCntWtd, as we are likely to use the same mechanism
    // for vectors on Arm64, though the actual value may differ.

    unsigned int         floatVarCount        = 0;
    BasicBlock::weight_t thresholdFPRefCntWtd = 4 * BB_UNITY_WEIGHT;
    BasicBlock::weight_t maybeFPRefCntWtd     = 2 * BB_UNITY_WEIGHT;
    VARSET_TP            fpMaybeCandidateVars(VarSetOps::UninitVal());
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    unsigned int         largeVectorVarCount           = 0;
    BasicBlock::weight_t thresholdLargeVectorRefCntWtd = 4 * BB_UNITY_WEIGHT;
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    if (enregisterLocalVars)
    {
        VarSetOps::AssignNoCopy(compiler, fpCalleeSaveCandidateVars, VarSetOps::MakeEmpty(compiler));
        VarSetOps::AssignNoCopy(compiler, fpMaybeCandidateVars, VarSetOps::MakeEmpty(compiler));
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        VarSetOps::AssignNoCopy(compiler, largeVectorVars, VarSetOps::MakeEmpty(compiler));
        VarSetOps::AssignNoCopy(compiler, largeVectorCalleeSaveCandidateVars, VarSetOps::MakeEmpty(compiler));
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    }
#if DOUBLE_ALIGN
    unsigned             refCntStk       = 0;
    unsigned             refCntReg       = 0;
    BasicBlock::weight_t refCntWtdReg    = 0;
    unsigned             refCntStkParam  = 0; // sum of     ref counts for all stack based parameters
    BasicBlock::weight_t refCntWtdStkDbl = 0; // sum of wtd ref counts for stack based doubles
    doDoubleAlign                        = false;
    bool checkDoubleAlign                = true;
    if (compiler->codeGen->isFramePointerRequired() || compiler->opts.MinOpts())
    {
        checkDoubleAlign = false;
    }
    else
    {
        switch (compiler->getCanDoubleAlign())
        {
            case MUST_DOUBLE_ALIGN:
                doDoubleAlign    = true;
                checkDoubleAlign = false;
                break;
            case CAN_DOUBLE_ALIGN:
                break;
            case CANT_DOUBLE_ALIGN:
                doDoubleAlign    = false;
                checkDoubleAlign = false;
                break;
            default:
                unreached();
        }
    }
#endif // DOUBLE_ALIGN

    // Check whether register variables are permitted.
    if (!enregisterLocalVars)
    {
        localVarIntervals = nullptr;
    }
    else if (compiler->lvaTrackedCount > 0)
    {
        // initialize mapping from tracked local to interval
        localVarIntervals = new (compiler, CMK_LSRA) Interval*[compiler->lvaTrackedCount];
    }

    INTRACK_STATS(regCandidateVarCount = 0);
    for (lclNum = 0, varDsc = compiler->lvaTable; lclNum < compiler->lvaCount; lclNum++, varDsc++)
    {
        // Initialize all variables to REG_STK
        varDsc->SetRegNum(REG_STK);
#ifndef TARGET_64BIT
        varDsc->SetOtherReg(REG_STK);
#endif // TARGET_64BIT

        if (!enregisterLocalVars)
        {
            varDsc->lvLRACandidate = false;
            continue;
        }

#if DOUBLE_ALIGN
        if (checkDoubleAlign)
        {
            if (varDsc->lvIsParam && !varDsc->lvIsRegArg)
            {
                refCntStkParam += varDsc->lvRefCnt();
            }
            else if (!isRegCandidate(varDsc) || varDsc->lvDoNotEnregister)
            {
                refCntStk += varDsc->lvRefCnt();
                if ((varDsc->lvType == TYP_DOUBLE) ||
                    ((varTypeIsStruct(varDsc) && varDsc->lvStructDoubleAlign &&
                      (compiler->lvaGetPromotionType(varDsc) != Compiler::PROMOTION_TYPE_INDEPENDENT))))
                {
                    refCntWtdStkDbl += varDsc->lvRefCntWtd();
                }
            }
            else
            {
                refCntReg += varDsc->lvRefCnt();
                refCntWtdReg += varDsc->lvRefCntWtd();
            }
        }
#endif // DOUBLE_ALIGN

        // Start with the assumption that it's a candidate.

        varDsc->lvLRACandidate = 1;

        // Start with lvRegister as false - set it true only if the variable gets
        // the same register assignment throughout
        varDsc->lvRegister = false;

        if (!isRegCandidate(varDsc))
        {
            varDsc->lvLRACandidate = 0;
            if (varDsc->lvTracked)
            {
                localVarIntervals[varDsc->lvVarIndex] = nullptr;
            }
            // The current implementation of multi-reg structs that are referenced collectively
            // (i.e. by refering to the parent lclVar rather than each field separately) relies
            // on all or none of the fields being candidates.
            if (varDsc->lvIsStructField)
            {
                LclVarDsc* parentVarDsc = compiler->lvaGetDesc(varDsc->lvParentLcl);
                if (parentVarDsc->lvIsMultiRegRet && !parentVarDsc->lvDoNotEnregister)
                {
                    JITDUMP("Setting multi-reg struct V%02u as not enregisterable:", varDsc->lvParentLcl);
                    compiler->lvaSetVarDoNotEnregister(varDsc->lvParentLcl DEBUGARG(Compiler::DNER_BlockOp));
                    for (unsigned int i = 0; i < parentVarDsc->lvFieldCnt; i++)
                    {
                        LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(parentVarDsc->lvFieldLclStart + i);
                        JITDUMP(" V%02u", parentVarDsc->lvFieldLclStart + i);
                        if (fieldVarDsc->lvTracked)
                        {
                            fieldVarDsc->lvLRACandidate                = 0;
                            localVarIntervals[fieldVarDsc->lvVarIndex] = nullptr;
                            VarSetOps::RemoveElemD(compiler, registerCandidateVars, fieldVarDsc->lvVarIndex);
                            JITDUMP("*");
                        }
                        // This is not accurate, but we need a non-zero refCnt for the parent so that it will
                        // be allocated to the stack.
                        parentVarDsc->setLvRefCnt(parentVarDsc->lvRefCnt() + fieldVarDsc->lvRefCnt());
                    }
                    JITDUMP("\n");
                }
            }
            continue;
        }

        if (varDsc->lvLRACandidate)
        {
            var_types type = varDsc->GetActualRegisterType();
            if (varTypeUsesFloatReg(type))
            {
                compiler->compFloatingPointUsed = true;
            }
            Interval* newInt = newInterval(type);
            newInt->setLocalNumber(compiler, lclNum, this);
            VarSetOps::AddElemD(compiler, registerCandidateVars, varDsc->lvVarIndex);

            // we will set this later when we have determined liveness
            varDsc->lvMustInit = false;

            if (varDsc->lvIsStructField)
            {
                newInt->isStructField = true;
            }

            if (varDsc->lvLiveInOutOfHndlr)
            {
                newInt->isWriteThru = varDsc->lvEhWriteThruCandidate;
                setIntervalAsSpilled(newInt);
            }

            INTRACK_STATS(regCandidateVarCount++);

            // We maintain two sets of FP vars - those that meet the first threshold of weighted ref Count,
            // and those that meet the second (see the definitions of thresholdFPRefCntWtd and maybeFPRefCntWtd
            // above).
            CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            // Additionally, when we are generating code for a target with partial SIMD callee-save
            // (AVX on non-UNIX amd64 and 16-byte vectors on arm64), we keep a separate set of the
            // LargeVectorType vars.
            if (Compiler::varTypeNeedsPartialCalleeSave(varDsc->lvType))
            {
                largeVectorVarCount++;
                VarSetOps::AddElemD(compiler, largeVectorVars, varDsc->lvVarIndex);
                BasicBlock::weight_t refCntWtd = varDsc->lvRefCntWtd();
                if (refCntWtd >= thresholdLargeVectorRefCntWtd)
                {
                    VarSetOps::AddElemD(compiler, largeVectorCalleeSaveCandidateVars, varDsc->lvVarIndex);
                }
            }
            else
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                if (regType(type) == FloatRegisterType)
            {
                floatVarCount++;
                BasicBlock::weight_t refCntWtd = varDsc->lvRefCntWtd();
                if (varDsc->lvIsRegArg)
                {
                    // Don't count the initial reference for register params.  In those cases,
                    // using a callee-save causes an extra copy.
                    refCntWtd -= BB_UNITY_WEIGHT;
                }
                if (refCntWtd >= thresholdFPRefCntWtd)
                {
                    VarSetOps::AddElemD(compiler, fpCalleeSaveCandidateVars, varDsc->lvVarIndex);
                }
                else if (refCntWtd >= maybeFPRefCntWtd)
                {
                    VarSetOps::AddElemD(compiler, fpMaybeCandidateVars, varDsc->lvVarIndex);
                }
            }
            JITDUMP("  ");
            DBEXEC(VERBOSE, newInt->dump());
        }
        else
        {
            localVarIntervals[varDsc->lvVarIndex] = nullptr;
        }
    }

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    // Create Intervals to use for the save & restore of the upper halves of large vector lclVars.
    if (enregisterLocalVars)
    {
        VarSetOps::Iter largeVectorVarsIter(compiler, largeVectorVars);
        unsigned        largeVectorVarIndex = 0;
        while (largeVectorVarsIter.NextElem(&largeVectorVarIndex))
        {
            makeUpperVectorInterval(largeVectorVarIndex);
        }
    }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

#if DOUBLE_ALIGN
    if (checkDoubleAlign)
    {
        // TODO-CQ: Fine-tune this:
        // In the legacy reg predictor, this runs after allocation, and then demotes any lclVars
        // allocated to the frame pointer, which is probably the wrong order.
        // However, because it runs after allocation, it can determine the impact of demoting
        // the lclVars allocated to the frame pointer.
        // => Here, estimate of the EBP refCnt and weighted refCnt is a wild guess.
        //
        unsigned             refCntEBP    = refCntReg / 8;
        BasicBlock::weight_t refCntWtdEBP = refCntWtdReg / 8;

        doDoubleAlign =
            compiler->shouldDoubleAlign(refCntStk, refCntEBP, refCntWtdEBP, refCntStkParam, refCntWtdStkDbl);
    }
#endif // DOUBLE_ALIGN

    // The factors we consider to determine which set of fp vars to use as candidates for callee save
    // registers current include the number of fp vars, whether there are loops, and whether there are
    // multiple exits.  These have been selected somewhat empirically, but there is probably room for
    // more tuning.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    if (VERBOSE)
    {
        printf("\nFP callee save candidate vars: ");
        if (enregisterLocalVars && !VarSetOps::IsEmpty(compiler, fpCalleeSaveCandidateVars))
        {
            dumpConvertedVarSet(compiler, fpCalleeSaveCandidateVars);
            printf("\n");
        }
        else
        {
            printf("None\n\n");
        }
    }
#endif

    JITDUMP("floatVarCount = %d; hasLoops = %d, singleExit = %d\n", floatVarCount, compiler->fgHasLoops,
            (compiler->fgReturnBlocks == nullptr || compiler->fgReturnBlocks->next == nullptr));

    // Determine whether to use the 2nd, more aggressive, threshold for fp callee saves.
    if (floatVarCount > 6 && compiler->fgHasLoops &&
        (compiler->fgReturnBlocks == nullptr || compiler->fgReturnBlocks->next == nullptr))
    {
        assert(enregisterLocalVars);
#ifdef DEBUG
        if (VERBOSE)
        {
            printf("Adding additional fp callee save candidates: \n");
            if (!VarSetOps::IsEmpty(compiler, fpMaybeCandidateVars))
            {
                dumpConvertedVarSet(compiler, fpMaybeCandidateVars);
                printf("\n");
            }
            else
            {
                printf("None\n\n");
            }
        }
#endif
        VarSetOps::UnionD(compiler, fpCalleeSaveCandidateVars, fpMaybeCandidateVars);
    }

    // From here on, we're only interested in the exceptVars that are candidates.
    if (enregisterLocalVars && (compiler->compHndBBtabCount > 0))
    {
        VarSetOps::IntersectionD(compiler, exceptVars, registerCandidateVars);
    }

#ifdef TARGET_ARM
#ifdef DEBUG
    if (VERBOSE)
    {
        // Frame layout is only pre-computed for ARM
        printf("\nlvaTable after IdentifyCandidates\n");
        compiler->lvaTableDump(Compiler::FrameLayoutState::PRE_REGALLOC_FRAME_LAYOUT);
    }
#endif // DEBUG
#endif // TARGET_ARM
}

// TODO-Throughput: This mapping can surely be more efficiently done
void LinearScan::initVarRegMaps()
{
    if (!enregisterLocalVars)
    {
        inVarToRegMaps  = nullptr;
        outVarToRegMaps = nullptr;
        return;
    }
    assert(compiler->lvaTrackedFixed); // We should have already set this to prevent us from adding any new tracked
                                       // variables.

    // The compiler memory allocator requires that the allocation be an
    // even multiple of int-sized objects
    unsigned int varCount = compiler->lvaTrackedCount;
    regMapCount           = roundUp(varCount, (unsigned)sizeof(int));

    // Not sure why blocks aren't numbered from zero, but they don't appear to be.
    // So, if we want to index by bbNum we have to know the maximum value.
    unsigned int bbCount = compiler->fgBBNumMax + 1;

    inVarToRegMaps  = new (compiler, CMK_LSRA) regNumberSmall*[bbCount];
    outVarToRegMaps = new (compiler, CMK_LSRA) regNumberSmall*[bbCount];

    if (varCount > 0)
    {
        // This VarToRegMap is used during the resolution of critical edges.
        sharedCriticalVarToRegMap = new (compiler, CMK_LSRA) regNumberSmall[regMapCount];

        for (unsigned int i = 0; i < bbCount; i++)
        {
            VarToRegMap inVarToRegMap  = new (compiler, CMK_LSRA) regNumberSmall[regMapCount];
            VarToRegMap outVarToRegMap = new (compiler, CMK_LSRA) regNumberSmall[regMapCount];

            for (unsigned int j = 0; j < regMapCount; j++)
            {
                inVarToRegMap[j]  = REG_STK;
                outVarToRegMap[j] = REG_STK;
            }
            inVarToRegMaps[i]  = inVarToRegMap;
            outVarToRegMaps[i] = outVarToRegMap;
        }
    }
    else
    {
        sharedCriticalVarToRegMap = nullptr;
        for (unsigned int i = 0; i < bbCount; i++)
        {
            inVarToRegMaps[i]  = nullptr;
            outVarToRegMaps[i] = nullptr;
        }
    }
}

void LinearScan::setInVarRegForBB(unsigned int bbNum, unsigned int varNum, regNumber reg)
{
    assert(enregisterLocalVars);
    assert(reg < UCHAR_MAX && varNum < compiler->lvaCount);
    inVarToRegMaps[bbNum][compiler->lvaTable[varNum].lvVarIndex] = (regNumberSmall)reg;
}

void LinearScan::setOutVarRegForBB(unsigned int bbNum, unsigned int varNum, regNumber reg)
{
    assert(enregisterLocalVars);
    assert(reg < UCHAR_MAX && varNum < compiler->lvaCount);
    outVarToRegMaps[bbNum][compiler->lvaTable[varNum].lvVarIndex] = (regNumberSmall)reg;
}

LinearScan::SplitEdgeInfo LinearScan::getSplitEdgeInfo(unsigned int bbNum)
{
    assert(enregisterLocalVars);
    SplitEdgeInfo splitEdgeInfo;
    assert(bbNum <= compiler->fgBBNumMax);
    assert(bbNum > bbNumMaxBeforeResolution);
    assert(splitBBNumToTargetBBNumMap != nullptr);
    splitBBNumToTargetBBNumMap->Lookup(bbNum, &splitEdgeInfo);
    assert(splitEdgeInfo.toBBNum <= bbNumMaxBeforeResolution);
    assert(splitEdgeInfo.fromBBNum <= bbNumMaxBeforeResolution);
    return splitEdgeInfo;
}

VarToRegMap LinearScan::getInVarToRegMap(unsigned int bbNum)
{
    assert(enregisterLocalVars);
    assert(bbNum <= compiler->fgBBNumMax);
    // For the blocks inserted to split critical edges, the inVarToRegMap is
    // equal to the outVarToRegMap at the "from" block.
    if (bbNum > bbNumMaxBeforeResolution)
    {
        SplitEdgeInfo splitEdgeInfo = getSplitEdgeInfo(bbNum);
        unsigned      fromBBNum     = splitEdgeInfo.fromBBNum;
        if (fromBBNum == 0)
        {
            assert(splitEdgeInfo.toBBNum != 0);
            return inVarToRegMaps[splitEdgeInfo.toBBNum];
        }
        else
        {
            return outVarToRegMaps[fromBBNum];
        }
    }

    return inVarToRegMaps[bbNum];
}

VarToRegMap LinearScan::getOutVarToRegMap(unsigned int bbNum)
{
    assert(enregisterLocalVars);
    assert(bbNum <= compiler->fgBBNumMax);
    if (bbNum == 0)
    {
        return nullptr;
    }
    // For the blocks inserted to split critical edges, the outVarToRegMap is
    // equal to the inVarToRegMap at the target.
    if (bbNum > bbNumMaxBeforeResolution)
    {
        // If this is an empty block, its in and out maps are both the same.
        // We identify this case by setting fromBBNum or toBBNum to 0, and using only the other.
        SplitEdgeInfo splitEdgeInfo = getSplitEdgeInfo(bbNum);
        unsigned      toBBNum       = splitEdgeInfo.toBBNum;
        if (toBBNum == 0)
        {
            assert(splitEdgeInfo.fromBBNum != 0);
            return outVarToRegMaps[splitEdgeInfo.fromBBNum];
        }
        else
        {
            return inVarToRegMaps[toBBNum];
        }
    }
    return outVarToRegMaps[bbNum];
}

//------------------------------------------------------------------------
// setVarReg: Set the register associated with a variable in the given 'bbVarToRegMap'.
//
// Arguments:
//    bbVarToRegMap   - the map of interest
//    trackedVarIndex - the lvVarIndex for the variable
//    reg             - the register to which it is being mapped
//
// Return Value:
//    None
//
void LinearScan::setVarReg(VarToRegMap bbVarToRegMap, unsigned int trackedVarIndex, regNumber reg)
{
    assert(trackedVarIndex < compiler->lvaTrackedCount);
    regNumberSmall regSmall = (regNumberSmall)reg;
    assert((regNumber)regSmall == reg);
    bbVarToRegMap[trackedVarIndex] = regSmall;
}

//------------------------------------------------------------------------
// getVarReg: Get the register associated with a variable in the given 'bbVarToRegMap'.
//
// Arguments:
//    bbVarToRegMap   - the map of interest
//    trackedVarIndex - the lvVarIndex for the variable
//
// Return Value:
//    The register to which 'trackedVarIndex' is mapped
//
regNumber LinearScan::getVarReg(VarToRegMap bbVarToRegMap, unsigned int trackedVarIndex)
{
    assert(enregisterLocalVars);
    assert(trackedVarIndex < compiler->lvaTrackedCount);
    return (regNumber)bbVarToRegMap[trackedVarIndex];
}

// Initialize the incoming VarToRegMap to the given map values (generally a predecessor of
// the block)
VarToRegMap LinearScan::setInVarToRegMap(unsigned int bbNum, VarToRegMap srcVarToRegMap)
{
    assert(enregisterLocalVars);
    VarToRegMap inVarToRegMap = inVarToRegMaps[bbNum];
    memcpy(inVarToRegMap, srcVarToRegMap, (regMapCount * sizeof(regNumber)));
    return inVarToRegMap;
}

//------------------------------------------------------------------------
// checkLastUses: Check correctness of last use flags
//
// Arguments:
//    The block for which we are checking last uses.
//
// Notes:
//    This does a backward walk of the RefPositions, starting from the liveOut set.
//    This method was previously used to set the last uses, which were computed by
//    liveness, but were not create in some cases of multiple lclVar references in the
//    same tree. However, now that last uses are computed as RefPositions are created,
//    that is no longer necessary, and this method is simply retained as a check.
//    The exception to the check-only behavior is when LSRA_EXTEND_LIFETIMES if set via
//    COMPlus_JitStressRegs. In that case, this method is required, because even though
//    the RefPositions will not be marked lastUse in that case, we still need to correclty
//    mark the last uses on the tree nodes, which is done by this method.
//
#ifdef DEBUG
void LinearScan::checkLastUses(BasicBlock* block)
{
    if (VERBOSE)
    {
        JITDUMP("\n\nCHECKING LAST USES for " FMT_BB ", liveout=", block->bbNum);
        dumpConvertedVarSet(compiler, block->bbLiveOut);
        JITDUMP("\n==============================\n");
    }

    unsigned keepAliveVarNum = BAD_VAR_NUM;
    if (compiler->lvaKeepAliveAndReportThis())
    {
        keepAliveVarNum = compiler->info.compThisArg;
        assert(compiler->info.compIsStatic == false);
    }

    // find which uses are lastUses

    // Work backwards starting with live out.
    // 'computedLive' is updated to include any exposed use (including those in this
    // block that we've already seen).  When we encounter a use, if it's
    // not in that set, then it's a last use.

    VARSET_TP computedLive(VarSetOps::MakeCopy(compiler, block->bbLiveOut));

    bool                       foundDiff       = false;
    RefPositionReverseIterator reverseIterator = refPositions.rbegin();
    RefPosition*               currentRefPosition;
    for (currentRefPosition = &reverseIterator; currentRefPosition->refType != RefTypeBB;
         reverseIterator++, currentRefPosition = &reverseIterator)
    {
        // We should never see ParamDefs or ZeroInits within a basic block.
        assert(currentRefPosition->refType != RefTypeParamDef && currentRefPosition->refType != RefTypeZeroInit);
        if (currentRefPosition->isIntervalRef() && currentRefPosition->getInterval()->isLocalVar)
        {
            unsigned varNum   = currentRefPosition->getInterval()->varNum;
            unsigned varIndex = currentRefPosition->getInterval()->getVarIndex(compiler);

            LsraLocation loc = currentRefPosition->nodeLocation;

            // We should always have a tree node for a localVar, except for the "special" RefPositions.
            GenTree* tree = currentRefPosition->treeNode;
            assert(tree != nullptr || currentRefPosition->refType == RefTypeExpUse ||
                   currentRefPosition->refType == RefTypeDummyDef);

            if (!VarSetOps::IsMember(compiler, computedLive, varIndex) && varNum != keepAliveVarNum)
            {
                // There was no exposed use, so this is a "last use" (and we mark it thus even if it's a def)

                if (extendLifetimes())
                {
                    // NOTE: this is a bit of a hack. When extending lifetimes, the "last use" bit will be clear.
                    // This bit, however, would normally be used during resolveLocalRef to set the value of
                    // LastUse on the node for a ref position. If this bit is not set correctly even when
                    // extending lifetimes, the code generator will assert as it expects to have accurate last
                    // use information. To avoid these asserts, set the LastUse bit here.
                    // Note also that extendLifetimes() is an LSRA stress mode, so it will only be true for
                    // Checked or Debug builds, for which this method will be executed.
                    if (tree != nullptr)
                    {
                        tree->AsLclVar()->SetLastUse(currentRefPosition->multiRegIdx);
                    }
                }
                else if (!currentRefPosition->lastUse)
                {
                    JITDUMP("missing expected last use of V%02u @%u\n", compiler->lvaTrackedIndexToLclNum(varIndex),
                            loc);
                    foundDiff = true;
                }
                VarSetOps::AddElemD(compiler, computedLive, varIndex);
            }
            else if (currentRefPosition->lastUse)
            {
                JITDUMP("unexpected last use of V%02u @%u\n", compiler->lvaTrackedIndexToLclNum(varIndex), loc);
                foundDiff = true;
            }
            else if (extendLifetimes() && tree != nullptr)
            {
                // NOTE: see the comment above re: the extendLifetimes hack.
                tree->AsLclVar()->ClearLastUse(currentRefPosition->multiRegIdx);
            }

            if (currentRefPosition->refType == RefTypeDef || currentRefPosition->refType == RefTypeDummyDef)
            {
                VarSetOps::RemoveElemD(compiler, computedLive, varIndex);
            }
        }

        assert(reverseIterator != refPositions.rend());
    }

    VARSET_TP liveInNotComputedLive(VarSetOps::Diff(compiler, block->bbLiveIn, computedLive));

    // We may have exception vars in the liveIn set of exception blocks that are not computed live.
    if (compiler->ehBlockHasExnFlowDsc(block))
    {
        VarSetOps::DiffD(compiler, liveInNotComputedLive, compiler->fgGetHandlerLiveVars(block));
    }
    VarSetOps::Iter liveInNotComputedLiveIter(compiler, liveInNotComputedLive);
    unsigned        liveInNotComputedLiveIndex = 0;
    while (liveInNotComputedLiveIter.NextElem(&liveInNotComputedLiveIndex))
    {
        LclVarDsc* varDesc = compiler->lvaGetDescByTrackedIndex(liveInNotComputedLiveIndex);
        if (varDesc->lvLRACandidate)
        {
            JITDUMP(FMT_BB ": V%02u is in LiveIn set, but not computed live.\n", block->bbNum,
                    compiler->lvaTrackedIndexToLclNum(liveInNotComputedLiveIndex));
            foundDiff = true;
        }
    }

    VarSetOps::DiffD(compiler, computedLive, block->bbLiveIn);
    const VARSET_TP& computedLiveNotLiveIn(computedLive); // reuse the buffer.
    VarSetOps::Iter  computedLiveNotLiveInIter(compiler, computedLiveNotLiveIn);
    unsigned         computedLiveNotLiveInIndex = 0;
    while (computedLiveNotLiveInIter.NextElem(&computedLiveNotLiveInIndex))
    {
        LclVarDsc* varDesc = compiler->lvaGetDescByTrackedIndex(computedLiveNotLiveInIndex);
        if (varDesc->lvLRACandidate)
        {
            JITDUMP(FMT_BB ": V%02u is computed live, but not in LiveIn set.\n", block->bbNum,
                    compiler->lvaTrackedIndexToLclNum(computedLiveNotLiveInIndex));
            foundDiff = true;
        }
    }

    assert(!foundDiff);
}
#endif // DEBUG

//------------------------------------------------------------------------
// findPredBlockForLiveIn: Determine which block should be used for the register locations of the live-in variables.
//
// Arguments:
//    block                 - The block for which we're selecting a predecesor.
//    prevBlock             - The previous block in in allocation order.
//    pPredBlockIsAllocated - A debug-only argument that indicates whether any of the predecessors have been seen
//                            in allocation order.
//
// Return Value:
//    The selected predecessor.
//
// Assumptions:
//    in DEBUG, caller initializes *pPredBlockIsAllocated to false, and it will be set to true if the block
//    returned is in fact a predecessor.
//
// Notes:
//    This will select a predecessor based on the heuristics obtained by getLsraBlockBoundaryLocations(), which can be
//    one of:
//      LSRA_BLOCK_BOUNDARY_PRED    - Use the register locations of a predecessor block (default)
//      LSRA_BLOCK_BOUNDARY_LAYOUT  - Use the register locations of the previous block in layout order.
//                                    This is the only case where this actually returns a different block.
//      LSRA_BLOCK_BOUNDARY_ROTATE  - Rotate the register locations from a predecessor.
//                                    For this case, the block returned is the same as for LSRA_BLOCK_BOUNDARY_PRED, but
//                                    the register locations will be "rotated" to stress the resolution and allocation
//                                    code.

BasicBlock* LinearScan::findPredBlockForLiveIn(BasicBlock* block,
                                               BasicBlock* prevBlock DEBUGARG(bool* pPredBlockIsAllocated))
{
    BasicBlock* predBlock = nullptr;
    assert(*pPredBlockIsAllocated == false);

    // Blocks with exception flow on entry use no predecessor blocks, as all incoming vars
    // are on the stack.
    if (blockInfo[block->bbNum].hasEHBoundaryIn)
    {
        JITDUMP("\n\nIncoming EH boundary; ");
        return nullptr;
    }

    if (block == compiler->fgFirstBB)
    {
        return nullptr;
    }

    if (block->bbPreds == nullptr)
    {
        assert((block != compiler->fgFirstBB) || (prevBlock != nullptr));
        JITDUMP("\n\nNo predecessor; ");

        // Some throw blocks do not have predecessor. For such blocks, we want to return the fact
        // that predecessor is indeed null instead of returning the prevBlock. Returning prevBlock
        // will be wrong, because LSRA would think that the variable is live in registers based on
        // the lexical flow, but that won't be true according to the control flow.
        // Example:
        //
        // IG05:
        //      ...         ; V01 is in 'rdi'
        //      JNE IG07
        //      ...
        // IG06:
        //      ...
        //      ...         ; V01 is in 'rbx'
        //      JMP IG08
        // IG07:
        //      ...         ; LSRA thinks V01 is in 'rbx' if IG06 is set as previous block of IG07.
        //      ....
        //      CALL CORINFO_HELP_RNGCHKFAIL
        //      ...
        // IG08:
        //      ...
        //      ...
        if (block->bbJumpKind == BBJ_THROW)
        {
            JITDUMP(" - throw block; ");
            return nullptr;
        }

        // We may have unreachable blocks, due to optimization.
        // We don't want to set the predecessor as null in this case, since that will result in
        // unnecessary DummyDefs, and possibly result in inconsistencies requiring resolution
        // (since these unreachable blocks can have reachable successors).
        return prevBlock;
    }

#ifdef DEBUG
    if (getLsraBlockBoundaryLocations() == LSRA_BLOCK_BOUNDARY_LAYOUT)
    {
        if (prevBlock != nullptr)
        {
            predBlock = prevBlock;
        }
    }
    else
#endif // DEBUG
    {
        predBlock = block->GetUniquePred(compiler);
        if (predBlock != nullptr)
        {
            // We should already have returned null if this block has a single incoming EH boundary edge.
            assert(!predBlock->hasEHBoundaryOut());
            if (isBlockVisited(predBlock))
            {
                if (predBlock->bbJumpKind == BBJ_COND)
                {
                    // Special handling to improve matching on backedges.
                    BasicBlock* otherBlock = (block == predBlock->bbNext) ? predBlock->bbJumpDest : predBlock->bbNext;
                    noway_assert(otherBlock != nullptr);
                    if (isBlockVisited(otherBlock) && !blockInfo[otherBlock->bbNum].hasEHBoundaryIn)
                    {
                        // This is the case when we have a conditional branch where one target has already
                        // been visited.  It would be best to use the same incoming regs as that block,
                        // so that we have less likelihood of having to move registers.
                        // For example, in determining the block to use for the starting register locations for
                        // "block" in the following example, we'd like to use the same predecessor for "block"
                        // as for "otherBlock", so that both successors of predBlock have the same locations, reducing
                        // the likelihood of needing a split block on a backedge:
                        //
                        //   otherPred
                        //       |
                        //   otherBlock <-+
                        //     . . .      |
                        //                |
                        //   predBlock----+
                        //       |
                        //     block
                        //
                        if (blockInfo[otherBlock->bbNum].hasEHBoundaryIn)
                        {
                            return nullptr;
                        }
                        else
                        {
                            for (flowList* pred = otherBlock->bbPreds; pred != nullptr; pred = pred->flNext)
                            {
                                BasicBlock* otherPred = pred->getBlock();
                                if (otherPred->bbNum == blockInfo[otherBlock->bbNum].predBBNum)
                                {
                                    predBlock = otherPred;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                predBlock = nullptr;
            }
        }
        else
        {
            for (flowList* pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
            {
                BasicBlock* candidatePredBlock = pred->getBlock();

                if (isBlockVisited(candidatePredBlock))
                {
                    if ((predBlock == nullptr) || (predBlock->bbWeight < candidatePredBlock->bbWeight))
                    {
                        predBlock = candidatePredBlock;
                        INDEBUG(*pPredBlockIsAllocated = true;)
                    }
                }
            }
        }
        if (predBlock == nullptr)
        {
            predBlock = prevBlock;
            assert(predBlock != nullptr);
            JITDUMP("\n\nNo allocated predecessor; ");
        }
    }
    return predBlock;
}

#ifdef DEBUG
void LinearScan::dumpVarRefPositions(const char* title)
{
    if (enregisterLocalVars)
    {
        printf("\nVAR REFPOSITIONS %s\n", title);

        for (unsigned i = 0; i < compiler->lvaCount; i++)
        {
            printf("--- V%02u", i);

            LclVarDsc* varDsc = compiler->lvaTable + i;
            if (varDsc->lvIsRegCandidate())
            {
                Interval* interval = getIntervalForLocalVar(varDsc->lvVarIndex);
                printf("  (Interval %d)\n", interval->intervalIndex);
                for (RefPosition* ref = interval->firstRefPosition; ref != nullptr; ref = ref->nextRefPosition)
                {
                    ref->dump();
                }
            }
            else
            {
                printf("\n");
            }
        }
        printf("\n");
    }
}

#endif // DEBUG

// Set the default rpFrameType based upon codeGen->isFramePointerRequired()
// This was lifted from the register predictor
//
void LinearScan::setFrameType()
{
    FrameType frameType = FT_NOT_SET;
#if DOUBLE_ALIGN
    compiler->codeGen->setDoubleAlign(false);
    if (doDoubleAlign)
    {
        frameType = FT_DOUBLE_ALIGN_FRAME;
        compiler->codeGen->setDoubleAlign(true);
    }
    else
#endif // DOUBLE_ALIGN
        if (compiler->codeGen->isFramePointerRequired())
    {
        frameType = FT_EBP_FRAME;
    }
    else
    {
        if (compiler->rpMustCreateEBPCalled == false)
        {
#ifdef DEBUG
            const char* reason;
#endif // DEBUG
            compiler->rpMustCreateEBPCalled = true;
            if (compiler->rpMustCreateEBPFrame(INDEBUG(&reason)))
            {
                JITDUMP("; Decided to create an EBP based frame for ETW stackwalking (%s)\n", reason);
                compiler->codeGen->setFrameRequired(true);
            }
        }

        if (compiler->codeGen->isFrameRequired())
        {
            frameType = FT_EBP_FRAME;
        }
        else
        {
            frameType = FT_ESP_FRAME;
        }
    }

    switch (frameType)
    {
        case FT_ESP_FRAME:
            noway_assert(!compiler->codeGen->isFramePointerRequired());
            noway_assert(!compiler->codeGen->isFrameRequired());
            compiler->codeGen->setFramePointerUsed(false);
            break;
        case FT_EBP_FRAME:
            compiler->codeGen->setFramePointerUsed(true);
            break;
#if DOUBLE_ALIGN
        case FT_DOUBLE_ALIGN_FRAME:
            noway_assert(!compiler->codeGen->isFramePointerRequired());
            compiler->codeGen->setFramePointerUsed(false);
            break;
#endif // DOUBLE_ALIGN
        default:
            noway_assert(!"rpFrameType not set correctly!");
            break;
    }

    // If we are using FPBASE as the frame register, we cannot also use it for
    // a local var.
    regMaskTP removeMask = RBM_NONE;
    if (frameType == FT_EBP_FRAME)
    {
        removeMask |= RBM_FPBASE;
    }

    compiler->rpFrameType = frameType;

#ifdef TARGET_ARMARCH
    // Determine whether we need to reserve a register for large lclVar offsets.
    if (compiler->compRsvdRegCheck(Compiler::REGALLOC_FRAME_LAYOUT))
    {
        // We reserve R10/IP1 in this case to hold the offsets in load/store instructions
        compiler->codeGen->regSet.rsMaskResvd |= RBM_OPT_RSVD;
        assert(REG_OPT_RSVD != REG_FP);
        JITDUMP("  Reserved REG_OPT_RSVD (%s) due to large frame\n", getRegName(REG_OPT_RSVD));
        removeMask |= RBM_OPT_RSVD;
    }
#endif // TARGET_ARMARCH

    if ((removeMask != RBM_NONE) && ((availableIntRegs & removeMask) != 0))
    {
        // We know that we're already in "read mode" for availableIntRegs. However,
        // we need to remove these registers, so subsequent users (like callers
        // to allRegs()) get the right thing. The RemoveRegistersFromMasks() code
        // fixes up everything that already took a dependency on the value that was
        // previously read, so this completes the picture.
        availableIntRegs.OverrideAssign(availableIntRegs & ~removeMask);
    }
}

//------------------------------------------------------------------------
// copyOrMoveRegInUse: Is 'ref' a copyReg/moveReg that is still busy at the given location?
//
// Arguments:
//    ref: The RefPosition of interest
//    loc: The LsraLocation at which we're determining whether it's busy.
//
// Return Value:
//    true iff 'ref' is active at the given location
//
bool copyOrMoveRegInUse(RefPosition* ref, LsraLocation loc)
{
    if (!ref->copyReg && !ref->moveReg)
    {
        return false;
    }
    if (ref->getRefEndLocation() >= loc)
    {
        return true;
    }
    Interval*    interval = ref->getInterval();
    RefPosition* nextRef  = interval->getNextRefPosition();
    if (nextRef != nullptr && nextRef->treeNode == ref->treeNode && nextRef->getRefEndLocation() >= loc)
    {
        return true;
    }
    return false;
}

//------------------------------------------------------------------------
// getRegisterType: Get the RegisterType to use for the given RefPosition
//
// Arguments:
//    currentInterval: The interval for the current allocation
//    refPosition:     The RefPosition of the current Interval for which a register is being allocated
//
// Return Value:
//    The RegisterType that should be allocated for this RefPosition
//
// Notes:
//    This will nearly always be identical to the registerType of the interval, except in the case
//    of SIMD types of 8 bytes (currently only Vector2) when they are passed and returned in integer
//    registers, or copied to a return temp.
//    This method need only be called in situations where we may be dealing with the register requirements
//    of a RefTypeUse RefPosition (i.e. not when we are only looking at the type of an interval, nor when
//    we are interested in the "defining" type of the interval).  This is because the situation of interest
//    only happens at the use (where it must be copied to an integer register).

RegisterType LinearScan::getRegisterType(Interval* currentInterval, RefPosition* refPosition)
{
    assert(refPosition->getInterval() == currentInterval);
    RegisterType regType    = currentInterval->registerType;
    regMaskTP    candidates = refPosition->registerAssignment;

    assert((candidates & allRegs(regType)) != RBM_NONE);
    return regType;
}

//------------------------------------------------------------------------
// isMatchingConstant: Check to see whether a given register contains the constant referenced
//                     by the given RefPosition
//
// Arguments:
//    physRegRecord:   The RegRecord for the register we're interested in.
//    refPosition:     The RefPosition for a constant interval.
//
// Return Value:
//    True iff the register was defined by an identical constant node as the current interval.
//
bool LinearScan::isMatchingConstant(RegRecord* physRegRecord, RefPosition* refPosition)
{
    if ((physRegRecord->assignedInterval == nullptr) || !physRegRecord->assignedInterval->isConstant ||
        (refPosition->refType != RefTypeDef))
    {
        return false;
    }
    Interval* interval = refPosition->getInterval();
    if (!interval->isConstant || !isRegConstant(physRegRecord->regNum, interval->registerType))
    {
        return false;
    }
    noway_assert(refPosition->treeNode != nullptr);
    GenTree* otherTreeNode = physRegRecord->assignedInterval->firstRefPosition->treeNode;
    noway_assert(otherTreeNode != nullptr);

    if (refPosition->treeNode->OperGet() != otherTreeNode->OperGet())
    {
        return false;
    }

    switch (otherTreeNode->OperGet())
    {
        case GT_CNS_INT:
        {
            ssize_t v1 = refPosition->treeNode->AsIntCon()->IconValue();
            ssize_t v2 = otherTreeNode->AsIntCon()->IconValue();
            if ((v1 == v2) && (varTypeGCtype(refPosition->treeNode) == varTypeGCtype(otherTreeNode) || v1 == 0))
            {
#ifdef TARGET_64BIT
                // If the constant is negative, only reuse registers of the same type.
                // This is because, on a 64-bit system, we do not sign-extend immediates in registers to
                // 64-bits unless they are actually longs, as this requires a longer instruction.
                // This doesn't apply to a 32-bit system, on which long values occupy multiple registers.
                // (We could sign-extend, but we would have to always sign-extend, because if we reuse more
                // than once, we won't have access to the instruction that originally defines the constant).
                if ((refPosition->treeNode->TypeGet() == otherTreeNode->TypeGet()) || (v1 >= 0))
#endif // TARGET_64BIT
                {
                    return true;
                }
            }
            break;
        }
        case GT_CNS_DBL:
        {
            // For floating point constants, the values must be identical, not simply compare
            // equal.  So we compare the bits.
            if (refPosition->treeNode->AsDblCon()->isBitwiseEqual(otherTreeNode->AsDblCon()) &&
                (refPosition->treeNode->TypeGet() == otherTreeNode->TypeGet()))
            {
                return true;
            }
            break;
        }
        default:
            break;
    }

    return false;
}

//------------------------------------------------------------------------
// allocateReg: Find a register that satisfies the requirements for refPosition,
//              taking into account the preferences for the given Interval,
//              and possibly spilling a lower weight Interval.
//
// Arguments:
//    currentInterval: The interval for the current allocation
//    refPosition:     The RefPosition of the current Interval for which a register is being allocated
// Return Value:
//    The regNumber, if any, allocated to the RefPosition.
//    Returns REG_NA only if 'refPosition->RegOptional()' is true, and there are
//    no free registers and no registers containing lower-weight Intervals that can be spilled.
//
// Notes:
//    This method will prefer to allocate a free register, but if none are available,
//    it will look for a lower-weight Interval to spill.
//    Weight and farthest distance of next reference are used to determine whether an Interval
//    currently occupying a register should be spilled. It will be spilled either:
//    - At its most recent RefPosition, if that is within the current block, OR
//    - At the boundary between the previous block and this one
//
// To select a ref position for spilling.
// - If refPosition->RegOptional() == false
//        The RefPosition chosen for spilling will be the lowest weight
//        of all and if there is is more than one ref position with the
//        same lowest weight, among them choses the one with farthest
//        distance to its next reference.
//
// - If refPosition->RegOptional() == true
//        The ref position chosen for spilling will not only be lowest weight
//        of all but also has a weight lower than 'refPosition'.  If there is
//        no such ref position, no register will be allocated.
//

regNumber LinearScan::allocateReg(Interval*    currentInterval,
                                  RefPosition* refPosition DEBUG_ARG(RegisterScore* registerScore))
{
    regMaskTP foundRegBit = regSelector->select(currentInterval, refPosition DEBUG_ARG(registerScore));
    if (foundRegBit == RBM_NONE)
    {
        return REG_NA;
    }

    regNumber  foundReg               = genRegNumFromMask(foundRegBit);
    RegRecord* availablePhysRegRecord = getRegisterRecord(foundReg);
    Interval*  assignedInterval       = availablePhysRegRecord->assignedInterval;
    if ((assignedInterval != currentInterval) &&
        isAssigned(availablePhysRegRecord ARM_ARG(getRegisterType(currentInterval, refPosition))))
    {
        if (regSelector->isSpilling())
        {
            // We're spilling.
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_ARM
            if (currentInterval->registerType == TYP_DOUBLE)
            {
                assert(genIsValidDoubleReg(availablePhysRegRecord->regNum));
                unassignDoublePhysReg(availablePhysRegRecord);
            }
            else if (assignedInterval->registerType == TYP_DOUBLE)
            {
                // Make sure we spill both halves of the double register.
                assert(genIsValidDoubleReg(assignedInterval->assignedReg->regNum));
                unassignPhysReg(assignedInterval->assignedReg, assignedInterval->recentRefPosition);
            }
            else
#endif
            {
                unassignPhysReg(availablePhysRegRecord, assignedInterval->recentRefPosition);
            }
        }
        else
        {
            // If we considered this "unassigned" because this interval's lifetime ends before
            // the next ref, remember it.
            // For historical reasons (due to former short-circuiting of this case), if we're reassigning
            // the current interval to a previous assignment, we don't remember the previous interval.
            // Note that we need to compute this condition before calling unassignPhysReg, which wil reset
            // assignedInterval->physReg.
            bool wasAssigned = regSelector->foundUnassignedReg() && (assignedInterval != nullptr) &&
                               (assignedInterval->physReg == foundReg);
            unassignPhysReg(availablePhysRegRecord ARM_ARG(currentInterval->registerType));
            if (regSelector->isMatchingConstant())
            {
                assert(assignedInterval->isConstant);
                refPosition->treeNode->SetReuseRegVal();
            }
            else if (wasAssigned)
            {
                updatePreviousInterval(availablePhysRegRecord, assignedInterval, assignedInterval->registerType);
            }
            else
            {
                assert(!regSelector->isConstAvailable());
            }
        }
    }
    assignPhysReg(availablePhysRegRecord, currentInterval);
    refPosition->registerAssignment = foundRegBit;
    return foundReg;
}

//------------------------------------------------------------------------
// canSpillReg: Determine whether we can spill physRegRecord
//
// Arguments:
//    physRegRecord             - reg to spill
//    refLocation               - Location of RefPosition where this register will be spilled
//
// Return Value:
//    True  - if we can spill physRegRecord
//    False - otherwise
//
bool LinearScan::canSpillReg(RegRecord* physRegRecord, LsraLocation refLocation)
{
    assert(physRegRecord->assignedInterval != nullptr);
    RefPosition* recentAssignedRef = physRegRecord->assignedInterval->recentRefPosition;

    if (recentAssignedRef != nullptr)
    {
        // We can't spill a register that's active at the current location.
        // We should already have determined this with isRegBusy before calling this method.
        assert(!isRefPositionActive(recentAssignedRef, refLocation));
        return true;
    }
    // recentAssignedRef can only be null if this is a parameter that has not yet been
    // moved to a register (or stack), in which case we can't spill it yet.
    assert(physRegRecord->assignedInterval->getLocalVar(compiler)->lvIsParam);
    return false;
}

//------------------------------------------------------------------------
// getSpillWeight: Get the weight associated with spilling the given register
//
// Arguments:
//    physRegRecord - reg to spill
//
// Return Value:
//    The weight associated with the location at which we will spill.
//
// Note: This helper is designed to be used only from allocateReg() and getDoubleSpillWeight()
//
float LinearScan::getSpillWeight(RegRecord* physRegRecord)
{
    assert(physRegRecord->assignedInterval != nullptr);
    RefPosition* recentAssignedRef = physRegRecord->assignedInterval->recentRefPosition;
    float        weight            = BB_ZERO_WEIGHT;

    // We shouldn't call this method if there is no recentAssignedRef.
    assert(recentAssignedRef != nullptr);
    // We shouldn't call this method if the register is active at this location.
    assert(!isRefPositionActive(recentAssignedRef, currentLoc));
    weight = getWeight(recentAssignedRef);
    return weight;
}

#ifdef TARGET_ARM
//------------------------------------------------------------------------
// canSpillDoubleReg: Determine whether we can spill physRegRecord
//
// Arguments:
//    physRegRecord             - reg to spill (must be a valid double register)
//    refLocation               - Location of RefPosition where this register will be spilled
//
// Return Value:
//    True  - if we can spill physRegRecord
//    False - otherwise
//
bool LinearScan::canSpillDoubleReg(RegRecord* physRegRecord, LsraLocation refLocation)
{
    assert(genIsValidDoubleReg(physRegRecord->regNum));
    RegRecord* physRegRecord2 = getSecondHalfRegRec(physRegRecord);

    if ((physRegRecord->assignedInterval != nullptr) && !canSpillReg(physRegRecord, refLocation))
    {
        return false;
    }
    if ((physRegRecord2->assignedInterval != nullptr) && !canSpillReg(physRegRecord2, refLocation))
    {
        return false;
    }
    return true;
}

//------------------------------------------------------------------------
// unassignDoublePhysReg: unassign a double register (pair)
//
// Arguments:
//    doubleRegRecord - reg to unassign
//
// Note:
//    The given RegRecord must be a valid (even numbered) double register.
//
void LinearScan::unassignDoublePhysReg(RegRecord* doubleRegRecord)
{
    assert(genIsValidDoubleReg(doubleRegRecord->regNum));

    RegRecord* doubleRegRecordLo = doubleRegRecord;
    RegRecord* doubleRegRecordHi = getSecondHalfRegRec(doubleRegRecordLo);
    // For a double register, we has following four cases.
    // Case 1: doubleRegRecLo is assigned to TYP_DOUBLE interval
    // Case 2: doubleRegRecLo and doubleRegRecHi are assigned to different TYP_FLOAT intervals
    // Case 3: doubelRegRecLo is assgined to TYP_FLOAT interval and doubleRegRecHi is nullptr
    // Case 4: doubleRegRecordLo is nullptr, and doubleRegRecordHi is assigned to a TYP_FLOAT interval
    if (doubleRegRecordLo->assignedInterval != nullptr)
    {
        if (doubleRegRecordLo->assignedInterval->registerType == TYP_DOUBLE)
        {
            // Case 1: doubleRegRecLo is assigned to TYP_DOUBLE interval
            unassignPhysReg(doubleRegRecordLo, doubleRegRecordLo->assignedInterval->recentRefPosition);
        }
        else
        {
            // Case 2: doubleRegRecLo and doubleRegRecHi are assigned to different TYP_FLOAT intervals
            // Case 3: doubelRegRecLo is assgined to TYP_FLOAT interval and doubleRegRecHi is nullptr
            assert(doubleRegRecordLo->assignedInterval->registerType == TYP_FLOAT);
            unassignPhysReg(doubleRegRecordLo, doubleRegRecordLo->assignedInterval->recentRefPosition);

            if (doubleRegRecordHi != nullptr)
            {
                if (doubleRegRecordHi->assignedInterval != nullptr)
                {
                    assert(doubleRegRecordHi->assignedInterval->registerType == TYP_FLOAT);
                    unassignPhysReg(doubleRegRecordHi, doubleRegRecordHi->assignedInterval->recentRefPosition);
                }
            }
        }
    }
    else
    {
        // Case 4: doubleRegRecordLo is nullptr, and doubleRegRecordHi is assigned to a TYP_FLOAT interval
        assert(doubleRegRecordHi->assignedInterval != nullptr);
        assert(doubleRegRecordHi->assignedInterval->registerType == TYP_FLOAT);
        unassignPhysReg(doubleRegRecordHi, doubleRegRecordHi->assignedInterval->recentRefPosition);
    }
}

#endif // TARGET_ARM

//------------------------------------------------------------------------
// isRefPositionActive: Determine whether a given RefPosition is active at the given location
//
// Arguments:
//    refPosition - the RefPosition of interest
//    refLocation - the LsraLocation at which we want to know if it is active
//
// Return Value:
//    True  - if this RefPosition occurs at the given location, OR
//            if it occurs at the previous location and is marked delayRegFree.
//    False - otherwise
//
bool LinearScan::isRefPositionActive(RefPosition* refPosition, LsraLocation refLocation)
{
    return (refPosition->nodeLocation == refLocation ||
            ((refPosition->nodeLocation + 1 == refLocation) && refPosition->delayRegFree));
}

//------------------------------------------------------------------------
// isSpillCandidate: Determine if a register is a spill candidate for a given RefPosition.
//
// Arguments:
//    current               The interval for the current allocation
//    refPosition           The RefPosition of the current Interval for which a register is being allocated
//    physRegRecord         The RegRecord for the register we're considering for spill
//
// Return Value:
//    True iff the given register can be spilled to accommodate the given RefPosition.
//
bool LinearScan::isSpillCandidate(Interval* current, RefPosition* refPosition, RegRecord* physRegRecord)
{
    regMaskTP    candidateBit = genRegMask(physRegRecord->regNum);
    LsraLocation refLocation  = refPosition->nodeLocation;
    // We shouldn't be calling this if we haven't already determined that the register is not
    // busy until the next kill.
    assert(!isRegBusy(physRegRecord->regNum, current->registerType));
    // We should already have determined that the register isn't actively in use.
    assert(!isRegInUse(physRegRecord->regNum, current->registerType));
    // We shouldn't be calling this if 'refPosition' is a fixed reference to this register.
    assert(!refPosition->isFixedRefOfRegMask(candidateBit));
    // We shouldn't be calling this if there is a fixed reference at the same location
    // (and it's not due to this reference), as checked above.
    assert(!conflictingFixedRegReference(physRegRecord->regNum, refPosition));

    bool canSpill;
#ifdef TARGET_ARM
    if (current->registerType == TYP_DOUBLE)
    {
        canSpill = canSpillDoubleReg(physRegRecord, refLocation);
    }
    else
#endif // TARGET_ARM
    {
        canSpill = canSpillReg(physRegRecord, refLocation);
    }
    if (!canSpill)
    {
        return false;
    }
    return true;
}

// Grab a register to use to copy and then immediately use.
// This is called only for localVar intervals that already have a register
// assignment that is not compatible with the current RefPosition.
// This is not like regular assignment, because we don't want to change
// any preferences or existing register assignments.
// Prefer a free register that's got the earliest next use.
// Otherwise, spill something with the farthest next use
//
regNumber LinearScan::assignCopyReg(RefPosition* refPosition)
{
    Interval* currentInterval = refPosition->getInterval();
    assert(currentInterval != nullptr);
    assert(currentInterval->isActive);

    // Save the relatedInterval, if any, so that it doesn't get modified during allocation.
    Interval* savedRelatedInterval   = currentInterval->relatedInterval;
    currentInterval->relatedInterval = nullptr;

    // We don't want really want to change the default assignment,
    // so 1) pretend this isn't active, and 2) remember the old reg
    regNumber  oldPhysReg   = currentInterval->physReg;
    RegRecord* oldRegRecord = currentInterval->assignedReg;
    assert(oldRegRecord->regNum == oldPhysReg);
    currentInterval->isActive = false;

    // We *must* allocate a register, and it will be a copyReg. Set that field now, so that
    // refPosition->RegOptional() will return false.
    refPosition->copyReg = true;

    RegisterScore registerScore = NONE;
    regNumber allocatedReg      = allocateReg(currentInterval, refPosition DEBUG_ARG(&registerScore));
    assert(allocatedReg != REG_NA);

    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_COPY_REG, currentInterval, allocatedReg, nullptr, registerScore));

    // Now restore the old info
    currentInterval->relatedInterval = savedRelatedInterval;
    currentInterval->physReg         = oldPhysReg;
    currentInterval->assignedReg     = oldRegRecord;
    currentInterval->isActive        = true;

    return allocatedReg;
}

//------------------------------------------------------------------------
// isAssigned: This is the function to check if the given RegRecord has an assignedInterval.
//
// Arguments:
//    regRec       - The RegRecord to check that it is assigned.
//    newRegType   - There are elements to judge according to the upcoming register type.
//
// Return Value:
//    Returns true if the given RegRecord has an assignedInterval.
//
bool LinearScan::isAssigned(RegRecord* regRec ARM_ARG(RegisterType newRegType))
{
    if (regRec->assignedInterval != nullptr)
    {
        return true;
    }
#ifdef TARGET_ARM
    if (newRegType == TYP_DOUBLE)
    {
        RegRecord* otherRegRecord = getSecondHalfRegRec(regRec);

        if (otherRegRecord->assignedInterval != nullptr)
        {
            return true;
        }
    }
#endif
    return false;
}

//------------------------------------------------------------------------
// checkAndAssignInterval: Check if the interval is already assigned and
//                         if it is then unassign the physical record
//                         and set the assignedInterval to 'interval'
//
// Arguments:
//    regRec       - The RegRecord of interest
//    interval     - The Interval that we're going to assign to 'regRec'
//
void LinearScan::checkAndAssignInterval(RegRecord* regRec, Interval* interval)
{
    Interval* assignedInterval = regRec->assignedInterval;
    if (assignedInterval != nullptr && assignedInterval != interval)
    {
        // This is allocated to another interval.  Either it is inactive, or it was allocated as a
        // copyReg and is therefore not the "assignedReg" of the other interval.  In the latter case,
        // we simply unassign it - in the former case we need to set the physReg on the interval to
        // REG_NA to indicate that it is no longer in that register.
        // The lack of checking for this case resulted in an assert in the retail version of System.dll,
        // in method SerialStream.GetDcbFlag.
        // Note that we can't check for the copyReg case, because we may have seen a more recent
        // RefPosition for the Interval that was NOT a copyReg.
        if (assignedInterval->assignedReg == regRec)
        {
            assert(assignedInterval->isActive == false);
            assignedInterval->physReg = REG_NA;
        }
        unassignPhysReg(regRec->regNum);
    }
#ifdef TARGET_ARM
    // If 'interval' and 'assignedInterval' were both TYP_DOUBLE, then we have unassigned 'assignedInterval'
    // from both halves. Otherwise, if 'interval' is TYP_DOUBLE, we now need to unassign the other half.
    if ((interval->registerType == TYP_DOUBLE) &&
        ((assignedInterval == nullptr) || (assignedInterval->registerType == TYP_FLOAT)))
    {
        RegRecord* otherRegRecord = getSecondHalfRegRec(regRec);
        assignedInterval          = otherRegRecord->assignedInterval;
        if (assignedInterval != nullptr && assignedInterval != interval)
        {
            if (assignedInterval->assignedReg == otherRegRecord)
            {
                assert(assignedInterval->isActive == false);
                assignedInterval->physReg = REG_NA;
            }
            unassignPhysReg(otherRegRecord->regNum);
        }
    }
#endif

    updateAssignedInterval(regRec, interval, interval->registerType);
}

// Assign the given physical register interval to the given interval
void LinearScan::assignPhysReg(RegRecord* regRec, Interval* interval)
{
    regMaskTP assignedRegMask = genRegMask(regRec->regNum);
    compiler->codeGen->regSet.rsSetRegsModified(assignedRegMask DEBUGARG(true));

    interval->assignedReg = regRec;
    checkAndAssignInterval(regRec, interval);

    interval->physReg  = regRec->regNum;
    interval->isActive = true;
    if (interval->isLocalVar)
    {
        // Prefer this register for future references
        interval->updateRegisterPreferences(assignedRegMask);
    }
}

//------------------------------------------------------------------------
// setIntervalAsSplit: Set this Interval as being split
//
// Arguments:
//    interval - The Interval which is being split
//
// Return Value:
//    None.
//
// Notes:
//    The given Interval will be marked as split, and it will be added to the
//    set of splitOrSpilledVars.
//
// Assumptions:
//    "interval" must be a lclVar interval, as tree temps are never split.
//    This is asserted in the call to getVarIndex().
//
void LinearScan::setIntervalAsSplit(Interval* interval)
{
    if (interval->isLocalVar)
    {
        unsigned varIndex = interval->getVarIndex(compiler);
        if (!interval->isSplit)
        {
            VarSetOps::AddElemD(compiler, splitOrSpilledVars, varIndex);
        }
        else
        {
            assert(VarSetOps::IsMember(compiler, splitOrSpilledVars, varIndex));
        }
    }
    interval->isSplit = true;
}

//------------------------------------------------------------------------
// setIntervalAsSpilled: Set this Interval as being spilled
//
// Arguments:
//    interval - The Interval which is being spilled
//
// Return Value:
//    None.
//
// Notes:
//    The given Interval will be marked as spilled, and it will be added
//    to the set of splitOrSpilledVars.
//
void LinearScan::setIntervalAsSpilled(Interval* interval)
{
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    if (interval->isUpperVector)
    {
        assert(interval->relatedInterval->isLocalVar);
        interval->isSpilled = true;
        // Now we need to mark the local as spilled also, even if the lower half is never spilled,
        // as this will use the upper part of its home location.
        interval = interval->relatedInterval;
        // We'll now mark this as spilled, so it changes the spillCost.
        RefPosition* recentRefPos = interval->recentRefPosition;
        if (!interval->isSpilled && interval->isActive && (recentRefPos != nullptr))
        {
            VarSetOps::AddElemD(compiler, splitOrSpilledVars, interval->getVarIndex(compiler));
            interval->isSpilled = true;
            regNumber reg       = interval->physReg;
            spillCost[reg]      = getSpillWeight(getRegisterRecord(reg));
        }
    }
#endif
    if (interval->isLocalVar)
    {
        unsigned varIndex = interval->getVarIndex(compiler);
        if (!interval->isSpilled)
        {
            VarSetOps::AddElemD(compiler, splitOrSpilledVars, varIndex);
        }
        else
        {
            assert(VarSetOps::IsMember(compiler, splitOrSpilledVars, varIndex));
        }
    }
    interval->isSpilled = true;
}

//------------------------------------------------------------------------
// spill: Spill the "interval" starting from "fromRefPosition" (upto "toRefPosition")
//
// Arguments:
//    interval        - The interval that contains the RefPosition to be spilled
//    fromRefPosition - The RefPosition at which the Interval is to be spilled
//    toRefPosition   - The RefPosition at which it must be reloaded (debug only arg)
//
// Return Value:
//    None.
//
// Assumptions:
//    fromRefPosition and toRefPosition must not be null
//
void LinearScan::spillInterval(Interval* interval, RefPosition* fromRefPosition DEBUGARG(RefPosition* toRefPosition))
{
    assert(fromRefPosition != nullptr && toRefPosition != nullptr);
    assert(fromRefPosition->getInterval() == interval && toRefPosition->getInterval() == interval);
    assert(fromRefPosition->nextRefPosition == toRefPosition);

    if (!fromRefPosition->lastUse)
    {
        // If not allocated a register, Lcl var def/use ref positions even if reg optional
        // should be marked as spillAfter. Note that if it is a WriteThru interval, the value is always
        // written to the stack, but the WriteThru indicates that the register is no longer live.
        if (fromRefPosition->RegOptional() && !(interval->isLocalVar && fromRefPosition->IsActualRef()))
        {
            fromRefPosition->registerAssignment = RBM_NONE;
        }
        else
        {
            fromRefPosition->spillAfter = true;
        }
    }
    assert(toRefPosition != nullptr);

#ifdef DEBUG
    if (VERBOSE)
    {
        dumpLsraAllocationEvent(LSRA_EVENT_SPILL, interval);
    }
#endif // DEBUG

    INTRACK_STATS(updateLsraStat(STAT_SPILL, fromRefPosition->bbNum));

    interval->isActive = false;
    setIntervalAsSpilled(interval);

    // If fromRefPosition occurs before the beginning of this block, mark this as living in the stack
    // on entry to this block.
    if (fromRefPosition->nodeLocation <= curBBStartLocation)
    {
        // This must be a lclVar interval
        assert(interval->isLocalVar);
        setInVarRegForBB(curBBNum, interval->varNum, REG_STK);
    }
}

//------------------------------------------------------------------------
// unassignPhysRegNoSpill: Unassign the given physical register record from
//                         an active interval, without spilling.
//
// Arguments:
//    regRec           - the RegRecord to be unassigned
//
// Return Value:
//    None.
//
// Assumptions:
//    The assignedInterval must not be null, and must be active.
//
// Notes:
//    This method is used to unassign a register when an interval needs to be moved to a
//    different register, but not (yet) spilled.

void LinearScan::unassignPhysRegNoSpill(RegRecord* regRec)
{
    Interval* assignedInterval = regRec->assignedInterval;
    assert(assignedInterval != nullptr && assignedInterval->isActive);
    assignedInterval->isActive = false;
    unassignPhysReg(regRec, nullptr);
    assignedInterval->isActive = true;
}

//------------------------------------------------------------------------
// checkAndClearInterval: Clear the assignedInterval for the given
//                        physical register record
//
// Arguments:
//    regRec           - the physical RegRecord to be unassigned
//    spillRefPosition - The RefPosition at which the assignedInterval is to be spilled
//                       or nullptr if we aren't spilling
//
// Return Value:
//    None.
//
// Assumptions:
//    see unassignPhysReg
//
void LinearScan::checkAndClearInterval(RegRecord* regRec, RefPosition* spillRefPosition)
{
    Interval* assignedInterval = regRec->assignedInterval;
    assert(assignedInterval != nullptr);
    regNumber thisRegNum = regRec->regNum;

    if (spillRefPosition == nullptr)
    {
        // Note that we can't assert  for the copyReg case
        //
        if (assignedInterval->physReg == thisRegNum)
        {
            assert(assignedInterval->isActive == false);
        }
    }
    else
    {
        assert(spillRefPosition->getInterval() == assignedInterval);
    }

    updateAssignedInterval(regRec, nullptr, assignedInterval->registerType);
}

//------------------------------------------------------------------------
// unassignPhysReg: Unassign the given physical register record, and spill the
//                  assignedInterval at the given spillRefPosition, if any.
//
// Arguments:
//    regRec           - The RegRecord to be unassigned
//    newRegType       - The RegisterType of interval that would be assigned
//
// Return Value:
//    None.
//
// Notes:
//    On ARM architecture, Intervals have to be unassigned considering
//    with the register type of interval that would be assigned.
//
void LinearScan::unassignPhysReg(RegRecord* regRec ARM_ARG(RegisterType newRegType))
{
    RegRecord* regRecToUnassign = regRec;
#ifdef TARGET_ARM
    RegRecord* anotherRegRec = nullptr;

    if ((regRecToUnassign->assignedInterval != nullptr) &&
        (regRecToUnassign->assignedInterval->registerType == TYP_DOUBLE))
    {
        // If the register type of interval(being unassigned or new) is TYP_DOUBLE,
        // It should have to be valid double register (even register)
        if (!genIsValidDoubleReg(regRecToUnassign->regNum))
        {
            regRecToUnassign = findAnotherHalfRegRec(regRec);
        }
    }
    else
    {
        if (newRegType == TYP_DOUBLE)
        {
            anotherRegRec = getSecondHalfRegRec(regRecToUnassign);
        }
    }
#endif

    if (regRecToUnassign->assignedInterval != nullptr)
    {
        unassignPhysReg(regRecToUnassign, regRecToUnassign->assignedInterval->recentRefPosition);
    }
#ifdef TARGET_ARM
    if ((anotherRegRec != nullptr) && (anotherRegRec->assignedInterval != nullptr))
    {
        unassignPhysReg(anotherRegRec, anotherRegRec->assignedInterval->recentRefPosition);
    }
#endif
}

//------------------------------------------------------------------------
// unassignPhysReg: Unassign the given physical register record, and spill the
//                  assignedInterval at the given spillRefPosition, if any.
//
// Arguments:
//    regRec           - the RegRecord to be unassigned
//    spillRefPosition - The RefPosition at which the assignedInterval is to be spilled
//
// Return Value:
//    None.
//
// Assumptions:
//    The assignedInterval must not be null.
//    If spillRefPosition is null, the assignedInterval must be inactive, or not currently
//    assigned to this register (e.g. this is a copyReg for that Interval).
//    Otherwise, spillRefPosition must be associated with the assignedInterval.
//
void LinearScan::unassignPhysReg(RegRecord* regRec, RefPosition* spillRefPosition)
{
    Interval* assignedInterval = regRec->assignedInterval;
    assert(assignedInterval != nullptr);
    assert(spillRefPosition == nullptr || spillRefPosition->getInterval() == assignedInterval);
    regNumber thisRegNum = regRec->regNum;

    // Is assignedInterval actually still assigned to this register?
    bool      intervalIsAssigned = (assignedInterval->physReg == thisRegNum);
    regNumber regToUnassign      = thisRegNum;

#ifdef TARGET_ARM
    RegRecord* anotherRegRec = nullptr;

    // Prepare second half RegRecord of a double register for TYP_DOUBLE
    if (assignedInterval->registerType == TYP_DOUBLE)
    {
        assert(isFloatRegType(regRec->registerType));
        RegRecord* doubleRegRec;
        if (genIsValidDoubleReg(thisRegNum))
        {
            anotherRegRec = getSecondHalfRegRec(regRec);
            doubleRegRec  = regRec;
        }
        else
        {
            regToUnassign = REG_PREV(thisRegNum);
            anotherRegRec = getRegisterRecord(regToUnassign);
            doubleRegRec  = anotherRegRec;
        }

        // Both RegRecords should have been assigned to the same interval.
        assert(assignedInterval == anotherRegRec->assignedInterval);
        if (!intervalIsAssigned && (assignedInterval->physReg == anotherRegRec->regNum))
        {
            intervalIsAssigned = true;
        }

        clearNextIntervalRef(regToUnassign, TYP_DOUBLE);
        clearSpillCost(regToUnassign, TYP_DOUBLE);
        checkAndClearInterval(doubleRegRec, spillRefPosition);

        // Both RegRecords should have been unassigned together.
        assert(regRec->assignedInterval == nullptr);
        assert(anotherRegRec->assignedInterval == nullptr);
    }
    else
#endif // TARGET_ARM
    {
        clearNextIntervalRef(thisRegNum, assignedInterval->registerType);
        clearSpillCost(thisRegNum, assignedInterval->registerType);
        checkAndClearInterval(regRec, spillRefPosition);
    }
    makeRegAvailable(regToUnassign, assignedInterval->registerType);

    RefPosition* nextRefPosition = nullptr;
    if (spillRefPosition != nullptr)
    {
        nextRefPosition = spillRefPosition->nextRefPosition;
    }

    if (!intervalIsAssigned && assignedInterval->physReg != REG_NA)
    {
        // This must have been a temporary copy reg, but we can't assert that because there
        // may have been intervening RefPositions that were not copyRegs.

        // reg->assignedInterval has already been set to nullptr by checkAndClearInterval()
        assert(regRec->assignedInterval == nullptr);
        return;
    }

    // regNumber victimAssignedReg = assignedInterval->physReg;
    assignedInterval->physReg = REG_NA;

    bool spill = assignedInterval->isActive && nextRefPosition != nullptr;
    if (spill)
    {
        // If this is an active interval, it must have a recentRefPosition,
        // otherwise it would not be active
        assert(spillRefPosition != nullptr);

#if 0
        // TODO-CQ: Enable this and insert an explicit GT_COPY (otherwise there's no way to communicate
        // to codegen that we want the copyReg to be the new home location).
        // If the last reference was a copyReg, and we're spilling the register
        // it was copied from, then make the copyReg the new primary location
        // if possible
        if (spillRefPosition->copyReg)
        {
            regNumber copyFromRegNum = victimAssignedReg;
            regNumber copyRegNum = genRegNumFromMask(spillRefPosition->registerAssignment);
            if (copyFromRegNum == thisRegNum &&
                getRegisterRecord(copyRegNum)->assignedInterval == assignedInterval)
            {
                assert(copyRegNum != thisRegNum);
                assignedInterval->physReg = copyRegNum;
                assignedInterval->assignedReg = this->getRegisterRecord(copyRegNum);
                return;
            }
        }
#endif // 0
#ifdef DEBUG
        // With JitStressRegs == 0x80 (LSRA_EXTEND_LIFETIMES), we may have a RefPosition
        // that is not marked lastUse even though the treeNode is a lastUse.  In that case
        // we must not mark it for spill because the register will have been immediately freed
        // after use.  While we could conceivably add special handling for this case in codegen,
        // it would be messy and undesirably cause the "bleeding" of LSRA stress modes outside
        // of LSRA.
        if (extendLifetimes() && assignedInterval->isLocalVar && RefTypeIsUse(spillRefPosition->refType) &&
            spillRefPosition->treeNode != nullptr &&
            spillRefPosition->treeNode->AsLclVar()->IsLastUse(spillRefPosition->multiRegIdx))
        {
            dumpLsraAllocationEvent(LSRA_EVENT_SPILL_EXTENDED_LIFETIME, assignedInterval);
            assignedInterval->isActive = false;
            spill                      = false;
            // If the spillRefPosition occurs before the beginning of this block, it will have
            // been marked as living in this register on entry to this block, but we now need
            // to mark this as living on the stack.
            if (spillRefPosition->nodeLocation <= curBBStartLocation)
            {
                setInVarRegForBB(curBBNum, assignedInterval->varNum, REG_STK);
                if (spillRefPosition->nextRefPosition != nullptr)
                {
                    setIntervalAsSpilled(assignedInterval);
                }
            }
            else
            {
                // Otherwise, we need to mark spillRefPosition as lastUse, or the interval
                // will remain active beyond its allocated range during the resolution phase.
                spillRefPosition->lastUse = true;
            }
        }
        else
#endif // DEBUG
        {
            spillInterval(assignedInterval, spillRefPosition DEBUGARG(nextRefPosition));
        }
    }
    // Maintain the association with the interval, if it has more references.
    // Or, if we "remembered" an interval assigned to this register, restore it.
    if (nextRefPosition != nullptr)
    {
        assignedInterval->assignedReg = regRec;
    }
    else if (canRestorePreviousInterval(regRec, assignedInterval))
    {
        regRec->assignedInterval = regRec->previousInterval;
        regRec->previousInterval = nullptr;
        if (regRec->assignedInterval->physReg != thisRegNum)
        {
            clearNextIntervalRef(thisRegNum, regRec->assignedInterval->registerType);
        }
        else
        {
            updateNextIntervalRef(thisRegNum, regRec->assignedInterval);
        }

#ifdef TARGET_ARM
        // Note:
        //   We can not use updateAssignedInterval() and updatePreviousInterval() here,
        //   because regRec may not be a even-numbered float register.

        // Update second half RegRecord of a double register for TYP_DOUBLE
        if (regRec->assignedInterval->registerType == TYP_DOUBLE)
        {
            RegRecord* anotherHalfRegRec = findAnotherHalfRegRec(regRec);

            anotherHalfRegRec->assignedInterval = regRec->assignedInterval;
            anotherHalfRegRec->previousInterval = nullptr;
        }
#endif // TARGET_ARM

#ifdef DEBUG
        if (spill)
        {
            dumpLsraAllocationEvent(LSRA_EVENT_RESTORE_PREVIOUS_INTERVAL_AFTER_SPILL, regRec->assignedInterval,
                                    thisRegNum);
        }
        else
        {
            dumpLsraAllocationEvent(LSRA_EVENT_RESTORE_PREVIOUS_INTERVAL, regRec->assignedInterval, thisRegNum);
        }
#endif // DEBUG
    }
    else
    {
        updateAssignedInterval(regRec, nullptr, assignedInterval->registerType);
        updatePreviousInterval(regRec, nullptr, assignedInterval->registerType);
    }
}

//------------------------------------------------------------------------
// spillGCRefs: Spill any GC-type intervals that are currently in registers.
//
// Arguments:
//    killRefPosition - The RefPosition for the kill
//
// Return Value:
//    None.
//
// Notes:
//    This is used to ensure that we have no live GC refs in registers at an
//    unmanaged call.
//
void LinearScan::spillGCRefs(RefPosition* killRefPosition)
{
    // For each physical register that can hold a GC type,
    // if it is occupied by an interval of a GC type, spill that interval.
    regMaskTP candidateRegs = killRefPosition->registerAssignment;
    INDEBUG(bool killedRegs = false);
    while (candidateRegs != RBM_NONE)
    {
        regMaskTP nextRegBit = genFindLowestBit(candidateRegs);
        candidateRegs &= ~nextRegBit;
        regNumber  nextReg          = genRegNumFromMask(nextRegBit);
        RegRecord* regRecord        = getRegisterRecord(nextReg);
        Interval*  assignedInterval = regRecord->assignedInterval;
        if (assignedInterval == nullptr || (assignedInterval->isActive == false))
        {
            continue;
        }
        bool needsKill = varTypeIsGC(assignedInterval->registerType);
        if (!needsKill)
        {
            // The importer will assign a GC type to the rhs of an assignment if the lhs type is a GC type,
            // even if the rhs is not. See the CEE_STLOC* case in impImportBlockCode(). As a result,
            // we can have a 'GT_LCL_VAR' node with a GC type, when the lclVar itself is an integer type.
            // The emitter will mark this register as holding a GC type. Therfore we must spill this value.
            // This was exposed on Arm32 with EH write-thru.
            if ((assignedInterval->recentRefPosition != nullptr) &&
                (assignedInterval->recentRefPosition->treeNode != nullptr))
            {
                needsKill = varTypeIsGC(assignedInterval->recentRefPosition->treeNode);
            }
        }
        if (needsKill)
        {
            INDEBUG(killedRegs = true);
            unassignPhysReg(regRecord, assignedInterval->recentRefPosition);
            makeRegAvailable(nextReg, assignedInterval->registerType);
        }
    }
    INDEBUG(dumpLsraAllocationEvent(killedRegs ? LSRA_EVENT_DONE_KILL_GC_REFS : LSRA_EVENT_NO_GC_KILLS, nullptr, REG_NA,
                                    nullptr));
}

//------------------------------------------------------------------------
// processBlockEndAllocation: Update var locations after 'currentBlock' has been allocated
//
// Arguments:
//    currentBlock - the BasicBlock we have just finished allocating registers for
//
// Return Value:
//    None
//
// Notes:
//    Calls processBlockEndLocations() to set the outVarToRegMap, then gets the next block,
//    and sets the inVarToRegMap appropriately.

void LinearScan::processBlockEndAllocation(BasicBlock* currentBlock)
{
    assert(currentBlock != nullptr);
    if (enregisterLocalVars)
    {
        processBlockEndLocations(currentBlock);
    }
    markBlockVisited(currentBlock);

    // Get the next block to allocate.
    // When the last block in the method has successors, there will be a final "RefTypeBB" to
    // ensure that we get the varToRegMap set appropriately, but in that case we don't need
    // to worry about "nextBlock".
    BasicBlock* nextBlock = getNextBlock();
    if (nextBlock != nullptr)
    {
        processBlockStartLocations(nextBlock);
    }
}

//------------------------------------------------------------------------
// rotateBlockStartLocation: When in the LSRA_BLOCK_BOUNDARY_ROTATE stress mode, attempt to
//                           "rotate" the register assignment for a localVar to the next higher
//                           register that is available.
//
// Arguments:
//    interval      - the Interval for the variable whose register is getting rotated
//    targetReg     - its register assignment from the predecessor block being used for live-in
//    availableRegs - registers available for use
//
// Return Value:
//    The new register to use.

#ifdef DEBUG
regNumber LinearScan::rotateBlockStartLocation(Interval* interval, regNumber targetReg, regMaskTP availableRegs)
{
    if (targetReg != REG_STK && getLsraBlockBoundaryLocations() == LSRA_BLOCK_BOUNDARY_ROTATE)
    {
        // If we're rotating the register locations at block boundaries, try to use
        // the next higher register number of the appropriate register type.
        regMaskTP candidateRegs = allRegs(interval->registerType) & availableRegs;
        regNumber firstReg      = REG_NA;
        regNumber newReg        = REG_NA;
        while (candidateRegs != RBM_NONE)
        {
            regMaskTP nextRegBit = genFindLowestBit(candidateRegs);
            candidateRegs &= ~nextRegBit;
            regNumber nextReg = genRegNumFromMask(nextRegBit);
            if (nextReg > targetReg)
            {
                newReg = nextReg;
                break;
            }
            else if (firstReg == REG_NA)
            {
                firstReg = nextReg;
            }
        }
        if (newReg == REG_NA)
        {
            assert(firstReg != REG_NA);
            newReg = firstReg;
        }
        targetReg = newReg;
    }
    return targetReg;
}
#endif // DEBUG

#ifdef TARGET_ARM
//--------------------------------------------------------------------------------------
// isSecondHalfReg: Test if recRec is second half of double register
//                  which is assigned to an interval.
//
// Arguments:
//    regRec - a register to be tested
//    interval - an interval which is assigned to some register
//
// Assumptions:
//    None
//
// Return Value:
//    True only if regRec is second half of assignedReg in interval
//
bool LinearScan::isSecondHalfReg(RegRecord* regRec, Interval* interval)
{
    RegRecord* assignedReg = interval->assignedReg;

    if (assignedReg != nullptr && interval->registerType == TYP_DOUBLE)
    {
        // interval should have been allocated to a valid double register
        assert(genIsValidDoubleReg(assignedReg->regNum));

        // Find a second half RegRecord of double register
        regNumber firstRegNum  = assignedReg->regNum;
        regNumber secondRegNum = REG_NEXT(firstRegNum);

        assert(genIsValidFloatReg(secondRegNum) && !genIsValidDoubleReg(secondRegNum));

        RegRecord* secondRegRec = getRegisterRecord(secondRegNum);

        return secondRegRec == regRec;
    }

    return false;
}

//------------------------------------------------------------------------------------------
// getSecondHalfRegRec: Get the second (odd) half of an ARM32 double register
//
// Arguments:
//    regRec - A float RegRecord
//
// Assumptions:
//    regRec must be a valid double register (i.e. even)
//
// Return Value:
//    The RegRecord for the second half of the double register
//
RegRecord* LinearScan::getSecondHalfRegRec(RegRecord* regRec)
{
    regNumber  secondHalfRegNum;
    RegRecord* secondHalfRegRec;

    assert(genIsValidDoubleReg(regRec->regNum));

    secondHalfRegNum = REG_NEXT(regRec->regNum);
    secondHalfRegRec = getRegisterRecord(secondHalfRegNum);

    return secondHalfRegRec;
}
//------------------------------------------------------------------------------------------
// findAnotherHalfRegRec: Find another half RegRecord which forms same ARM32 double register
//
// Arguments:
//    regRec - A float RegRecord
//
// Assumptions:
//    None
//
// Return Value:
//    A RegRecord which forms same double register with regRec
//
RegRecord* LinearScan::findAnotherHalfRegRec(RegRecord* regRec)
{
    regNumber anotherHalfRegNum = findAnotherHalfRegNum(regRec->regNum);
    return getRegisterRecord(anotherHalfRegNum);
}
//------------------------------------------------------------------------------------------
// findAnotherHalfRegNum: Find another half register's number which forms same ARM32 double register
//
// Arguments:
//    regNumber - A float regNumber
//
// Assumptions:
//    None
//
// Return Value:
//    A register number which forms same double register with regNum.
//
regNumber LinearScan::findAnotherHalfRegNum(regNumber regNum)
{
    regNumber anotherHalfRegNum;

    assert(genIsValidFloatReg(regNum));

    // Find another half register for TYP_DOUBLE interval,
    // following same logic in canRestorePreviousInterval().
    if (genIsValidDoubleReg(regNum))
    {
        anotherHalfRegNum = REG_NEXT(regNum);
        assert(!genIsValidDoubleReg(anotherHalfRegNum));
    }
    else
    {
        anotherHalfRegNum = REG_PREV(regNum);
        assert(genIsValidDoubleReg(anotherHalfRegNum));
    }

    return anotherHalfRegNum;
}
#endif

//--------------------------------------------------------------------------------------
// canRestorePreviousInterval: Test if we can restore previous interval
//
// Arguments:
//    regRec - a register which contains previous interval to be restored
//    assignedInterval - an interval just unassigned
//
// Assumptions:
//    None
//
// Return Value:
//    True only if previous interval of regRec can be restored
//
bool LinearScan::canRestorePreviousInterval(RegRecord* regRec, Interval* assignedInterval)
{
    bool retVal =
        (regRec->previousInterval != nullptr && regRec->previousInterval != assignedInterval &&
         regRec->previousInterval->assignedReg == regRec && regRec->previousInterval->getNextRefPosition() != nullptr);

#ifdef TARGET_ARM
    if (retVal && regRec->previousInterval->registerType == TYP_DOUBLE)
    {
        RegRecord* anotherHalfRegRec = findAnotherHalfRegRec(regRec);

        retVal = retVal && anotherHalfRegRec->assignedInterval == nullptr;
    }
#endif

    return retVal;
}

bool LinearScan::isAssignedToInterval(Interval* interval, RegRecord* regRec)
{
    bool isAssigned = (interval->assignedReg == regRec);
#ifdef TARGET_ARM
    isAssigned |= isSecondHalfReg(regRec, interval);
#endif
    return isAssigned;
}

void LinearScan::unassignIntervalBlockStart(RegRecord* regRecord, VarToRegMap inVarToRegMap)
{
    // Is there another interval currently assigned to this register?  If so unassign it.
    Interval* assignedInterval = regRecord->assignedInterval;
    if (assignedInterval != nullptr)
    {
        if (isAssignedToInterval(assignedInterval, regRecord))
        {
            // Only localVars, constants or vector upper halves should be assigned to registers at block boundaries.
            if (!assignedInterval->isLocalVar)
            {
                assert(assignedInterval->isConstant || assignedInterval->IsUpperVector());
                // Don't need to update the VarToRegMap.
                inVarToRegMap = nullptr;
            }

            regNumber assignedRegNum = assignedInterval->assignedReg->regNum;

            // If the interval is active, it will be set to active when we reach its new
            // register assignment (which we must not yet have done, or it wouldn't still be
            // assigned to this register).
            assignedInterval->isActive = false;
            unassignPhysReg(assignedInterval->assignedReg, nullptr);
            if ((inVarToRegMap != nullptr) && inVarToRegMap[assignedInterval->getVarIndex(compiler)] == assignedRegNum)
            {
                inVarToRegMap[assignedInterval->getVarIndex(compiler)] = REG_STK;
            }
        }
        else
        {
            // This interval is no longer assigned to this register.
            updateAssignedInterval(regRecord, nullptr, assignedInterval->registerType);
        }
    }
}

//------------------------------------------------------------------------
// processBlockStartLocations: Update var locations on entry to 'currentBlock' and clear constant
//                             registers.
//
// Arguments:
//    currentBlock   - the BasicBlock we are about to allocate registers for
//    allocationPass - true if we are currently allocating registers (versus writing them back)
//
// Return Value:
//    None
//
// Notes:
//    During the allocation pass, we use the outVarToRegMap of the selected predecessor to
//    determine the lclVar locations for the inVarToRegMap.
//    During the resolution (write-back) pass, we only modify the inVarToRegMap in cases where
//    a lclVar was spilled after the block had been completed.
void LinearScan::processBlockStartLocations(BasicBlock* currentBlock)
{
    // If we have no register candidates we should only call this method during allocation.

    assert(enregisterLocalVars || !allocationPassComplete);

    if (!enregisterLocalVars)
    {
        // Just clear any constant registers and return.
        resetAvailableRegs();
        for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
        {
            RegRecord* physRegRecord    = getRegisterRecord(reg);
            Interval*  assignedInterval = physRegRecord->assignedInterval;
            clearNextIntervalRef(reg, physRegRecord->registerType);
            clearSpillCost(reg, physRegRecord->registerType);
            if (assignedInterval != nullptr)
            {
                assert(assignedInterval->isConstant);
                physRegRecord->assignedInterval = nullptr;
            }
        }
        return;
    }

    unsigned    predBBNum       = blockInfo[currentBlock->bbNum].predBBNum;
    VarToRegMap predVarToRegMap = getOutVarToRegMap(predBBNum);
    VarToRegMap inVarToRegMap   = getInVarToRegMap(currentBlock->bbNum);

    // If this block enters an exception region, all incoming vars are on the stack.
    if (predBBNum == 0)
    {
#if DEBUG
        if (blockInfo[currentBlock->bbNum].hasEHBoundaryIn || !allocationPassComplete)
        {
            // This should still be in its initialized empty state.
            for (unsigned varIndex = 0; varIndex < compiler->lvaTrackedCount; varIndex++)
            {
                // In the case where we're extending lifetimes for stress, we are intentionally modeling variables
                // as live when they really aren't to create extra register pressure & constraints.
                // However, this means that non-EH-vars will be live into EH regions. We can and should ignore the
                // locations of these. Note that they aren't reported to codegen anyway.
                if (!getLsraExtendLifeTimes() || VarSetOps::IsMember(compiler, currentBlock->bbLiveIn, varIndex))
                {
                    assert(inVarToRegMap[varIndex] == REG_STK);
                }
            }
        }
#endif // DEBUG
        predVarToRegMap = inVarToRegMap;
    }

    VarSetOps::AssignNoCopy(compiler, currentLiveVars,
                            VarSetOps::Intersection(compiler, registerCandidateVars, currentBlock->bbLiveIn));
#ifdef DEBUG
    if (getLsraExtendLifeTimes())
    {
        VarSetOps::AssignNoCopy(compiler, currentLiveVars, registerCandidateVars);
    }
    // If we are rotating register assignments at block boundaries, we want to make the
    // inactive registers available for the rotation.
    regMaskTP inactiveRegs = RBM_NONE;
#endif // DEBUG
    regMaskTP       liveRegs = RBM_NONE;
    VarSetOps::Iter iter(compiler, currentLiveVars);
    unsigned        varIndex = 0;
    while (iter.NextElem(&varIndex))
    {
        if (!compiler->lvaGetDescByTrackedIndex(varIndex)->lvLRACandidate)
        {
            continue;
        }
        regNumber    targetReg;
        Interval*    interval        = getIntervalForLocalVar(varIndex);
        RefPosition* nextRefPosition = interval->getNextRefPosition();
        assert((nextRefPosition != nullptr) || (interval->isWriteThru));

        bool leaveOnStack = false;

        // Special handling for variables live in/out of exception handlers.
        if (interval->isWriteThru)
        {
            // There are 3 cases where we will leave writethru lclVars on the stack:
            // 1) There is no predecessor.
            // 2) It is conservatively or artificially live - that is, it has no next use,
            //    so there is no place for codegen to record that the register is no longer occupied.
            // 3) This block has a predecessor with an outgoing EH edge. We won't be able to add "join"
            //    resolution to load the EH var into a register along that edge, so it must be on stack.
            if ((predBBNum == 0) || (nextRefPosition == nullptr) || (RefTypeIsDef(nextRefPosition->refType)) ||
                blockInfo[currentBlock->bbNum].hasEHPred)
            {
                leaveOnStack = true;
            }
        }

        if (!allocationPassComplete)
        {
            targetReg = getVarReg(predVarToRegMap, varIndex);
            if (leaveOnStack)
            {
                targetReg = REG_STK;
            }
#ifdef DEBUG
            regNumber newTargetReg = rotateBlockStartLocation(interval, targetReg, (~liveRegs | inactiveRegs));
            if (newTargetReg != targetReg)
            {
                targetReg = newTargetReg;
                setIntervalAsSplit(interval);
            }
#endif // DEBUG
            setVarReg(inVarToRegMap, varIndex, targetReg);
        }
        else // allocationPassComplete (i.e. resolution/write-back pass)
        {
            targetReg = getVarReg(inVarToRegMap, varIndex);
            // There are four cases that we need to consider during the resolution pass:
            // 1. This variable had a register allocated initially, and it was not spilled in the RefPosition
            //    that feeds this block.  In this case, both targetReg and predVarToRegMap[varIndex] will be targetReg.
            // 2. This variable had not been spilled prior to the end of predBB, but was later spilled, so
            //    predVarToRegMap[varIndex] will be REG_STK, but targetReg is its former allocated value.
            //    In this case, we will normally change it to REG_STK.  We will update its "spilled" status when we
            //    encounter it in resolveLocalRef().
            // 2a. If the next RefPosition is marked as a copyReg, we need to retain the allocated register.  This is
            //     because the copyReg RefPosition will not have recorded the "home" register, yet downstream
            //     RefPositions rely on the correct "home" register.
            // 3. This variable was spilled before we reached the end of predBB.  In this case, both targetReg and
            //    predVarToRegMap[varIndex] will be REG_STK, and the next RefPosition will have been marked
            //    as reload during allocation time if necessary (note that by the time we actually reach the next
            //    RefPosition, we may be using a different predecessor, at which it is still in a register).
            // 4. This variable was spilled during the allocation of this block, so targetReg is REG_STK
            //    (because we set inVarToRegMap at the time we spilled it), but predVarToRegMap[varIndex]
            //    is not REG_STK.  We retain the REG_STK value in the inVarToRegMap.
            if (targetReg != REG_STK)
            {
                if (getVarReg(predVarToRegMap, varIndex) != REG_STK)
                {
                    // Case #1 above.
                    assert(getVarReg(predVarToRegMap, varIndex) == targetReg ||
                           getLsraBlockBoundaryLocations() == LSRA_BLOCK_BOUNDARY_ROTATE);
                }
                else if (!nextRefPosition->copyReg)
                {
                    // case #2 above.
                    setVarReg(inVarToRegMap, varIndex, REG_STK);
                    targetReg = REG_STK;
                }
                // Else case 2a. - retain targetReg.
            }
            // Else case #3 or #4, we retain targetReg and nothing further to do or assert.
        }

        if (interval->physReg == targetReg)
        {
            if (interval->isActive)
            {
                assert(targetReg != REG_STK);
                assert(interval->assignedReg != nullptr && interval->assignedReg->regNum == targetReg &&
                       interval->assignedReg->assignedInterval == interval);
                liveRegs |= getRegMask(targetReg, interval->registerType);
                continue;
            }
        }
        else if (interval->physReg != REG_NA)
        {
            // This can happen if we are using the locations from a basic block other than the
            // immediately preceding one - where the variable was in a different location.
            if ((targetReg != REG_STK) || leaveOnStack)
            {
                // Unassign it from the register (it may get a new register below).
                if (interval->assignedReg != nullptr && interval->assignedReg->assignedInterval == interval)
                {
                    interval->isActive = false;
                    unassignPhysReg(getRegisterRecord(interval->physReg), nullptr);
                }
                else
                {
                    // This interval was live in this register the last time we saw a reference to it,
                    // but has since been displaced.
                    interval->physReg = REG_NA;
                }
            }
            else if (!allocationPassComplete)
            {
                // Keep the register assignment - if another var has it, it will get unassigned.
                // Otherwise, resolution will fix it up later, and it will be more
                // likely to match other assignments this way.
                targetReg          = interval->physReg;
                interval->isActive = true;
                liveRegs |= getRegMask(targetReg, interval->registerType);
                INDEBUG(inactiveRegs |= genRegMask(targetReg));
                setVarReg(inVarToRegMap, varIndex, targetReg);
            }
            else
            {
                interval->physReg = REG_NA;
            }
        }
        if (targetReg != REG_STK)
        {
            RegRecord* targetRegRecord = getRegisterRecord(targetReg);
            liveRegs |= getRegMask(targetReg, interval->registerType);
            if (!allocationPassComplete)
            {
                updateNextIntervalRef(targetReg, interval);
                updateSpillCost(targetReg, interval);
            }
            if (!interval->isActive)
            {
                interval->isActive    = true;
                interval->physReg     = targetReg;
                interval->assignedReg = targetRegRecord;
            }
            if (targetRegRecord->assignedInterval != interval)
            {
#ifdef TARGET_ARM
                // If this is a TYP_DOUBLE interval, and the assigned interval is either null or is TYP_FLOAT,
                // we also need to unassign the other half of the register.
                // Note that if the assigned interval is TYP_DOUBLE, it will be unassigned below.
                if ((interval->registerType == TYP_DOUBLE) &&
                    ((targetRegRecord->assignedInterval == nullptr) ||
                     (targetRegRecord->assignedInterval->registerType == TYP_FLOAT)))
                {
                    assert(genIsValidDoubleReg(targetReg));
                    unassignIntervalBlockStart(getSecondHalfRegRec(targetRegRecord),
                                               allocationPassComplete ? nullptr : inVarToRegMap);
                }

                // If this is a TYP_FLOAT interval, and the assigned interval was TYP_DOUBLE, we also
                // need to update the liveRegs to specify that the other half is not live anymore.
                // As mentioned above, for TYP_DOUBLE, the other half will be unassigned further below.
                if ((interval->registerType == TYP_FLOAT) &&
                    ((targetRegRecord->assignedInterval != nullptr) &&
                     (targetRegRecord->assignedInterval->registerType == TYP_DOUBLE)))
                {
                    RegRecord* anotherHalfRegRec = findAnotherHalfRegRec(targetRegRecord);

                    // Use TYP_FLOAT to get the regmask of just the half reg.
                    liveRegs &= ~getRegMask(anotherHalfRegRec->regNum, TYP_FLOAT);
                }

#endif // TARGET_ARM
                unassignIntervalBlockStart(targetRegRecord, allocationPassComplete ? nullptr : inVarToRegMap);
                assignPhysReg(targetRegRecord, interval);
            }
            if (interval->recentRefPosition != nullptr && !interval->recentRefPosition->copyReg &&
                interval->recentRefPosition->registerAssignment != genRegMask(targetReg))
            {
                interval->getNextRefPosition()->outOfOrder = true;
            }
        }
    }

    // Unassign any registers that are no longer live, and set register state, if allocating.
    if (!allocationPassComplete)
    {
        resetRegState();
        setRegsInUse(liveRegs);
    }
    for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
    {
        RegRecord* physRegRecord = getRegisterRecord(reg);
        if ((liveRegs & genRegMask(reg)) == 0)
        {
            makeRegAvailable(reg, physRegRecord->registerType);
            Interval* assignedInterval = physRegRecord->assignedInterval;

            if (assignedInterval != nullptr)
            {
                assert(assignedInterval->isLocalVar || assignedInterval->isConstant ||
                       assignedInterval->IsUpperVector());

                if (!assignedInterval->isConstant && assignedInterval->assignedReg == physRegRecord)
                {
                    assignedInterval->isActive = false;
                    if (assignedInterval->getNextRefPosition() == nullptr)
                    {
                        unassignPhysReg(physRegRecord, nullptr);
                    }
                    if (!assignedInterval->IsUpperVector())
                    {
                        inVarToRegMap[assignedInterval->getVarIndex(compiler)] = REG_STK;
                    }
                }
                else
                {
                    // This interval may still be active, but was in another register in an
                    // intervening block.
                    updateAssignedInterval(physRegRecord, nullptr, assignedInterval->registerType);
                }

#ifdef TARGET_ARM
                // unassignPhysReg, above, may have restored a 'previousInterval', in which case we need to
                // get the value of 'physRegRecord->assignedInterval' rather than using 'assignedInterval'.
                if (physRegRecord->assignedInterval != nullptr)
                {
                    assignedInterval = physRegRecord->assignedInterval;
                }
                if (assignedInterval->registerType == TYP_DOUBLE)
                {
                    // Skip next float register, because we already addressed a double register
                    assert(genIsValidDoubleReg(reg));
                    reg = REG_NEXT(reg);
                    makeRegAvailable(reg, physRegRecord->registerType);
                }
#endif // TARGET_ARM
            }
        }
#ifdef TARGET_ARM
        else
        {
            Interval* assignedInterval = physRegRecord->assignedInterval;

            if (assignedInterval != nullptr && assignedInterval->registerType == TYP_DOUBLE)
            {
                // Skip next float register, because we already addressed a double register
                assert(genIsValidDoubleReg(reg));
                reg = REG_NEXT(reg);
            }
        }
#endif // TARGET_ARM
    }
}

//------------------------------------------------------------------------
// processBlockEndLocations: Record the variables occupying registers after completing the current block.
//
// Arguments:
//    currentBlock - the block we have just completed.
//
// Return Value:
//    None
//
// Notes:
//    This must be called both during the allocation and resolution (write-back) phases.
//    This is because we need to have the outVarToRegMap locations in order to set the locations
//    at successor blocks during allocation time, but if lclVars are spilled after a block has been
//    completed, we need to record the REG_STK location for those variables at resolution time.

void LinearScan::processBlockEndLocations(BasicBlock* currentBlock)
{
    assert(currentBlock != nullptr && currentBlock->bbNum == curBBNum);
    VarToRegMap outVarToRegMap = getOutVarToRegMap(curBBNum);

    VarSetOps::AssignNoCopy(compiler, currentLiveVars,
                            VarSetOps::Intersection(compiler, registerCandidateVars, currentBlock->bbLiveOut));
#ifdef DEBUG
    if (getLsraExtendLifeTimes())
    {
        VarSetOps::Assign(compiler, currentLiveVars, registerCandidateVars);
    }
#endif // DEBUG
    VarSetOps::Iter iter(compiler, currentLiveVars);
    unsigned        varIndex = 0;
    while (iter.NextElem(&varIndex))
    {
        Interval* interval = getIntervalForLocalVar(varIndex);
        if (interval->isActive)
        {
            assert(interval->physReg != REG_NA && interval->physReg != REG_STK);
            setVarReg(outVarToRegMap, varIndex, interval->physReg);
        }
        else
        {
            outVarToRegMap[varIndex] = REG_STK;
        }
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        // Ensure that we have no partially-spilled large vector locals.
        assert(!Compiler::varTypeNeedsPartialCalleeSave(interval->registerType) || !interval->isPartiallySpilled);
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    }
    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_END_BB));
}

#ifdef DEBUG
void LinearScan::dumpRefPositions(const char* str)
{
    printf("------------\n");
    printf("REFPOSITIONS %s: \n", str);
    printf("------------\n");
    for (RefPosition& refPos : refPositions)
    {
        refPos.dump();
    }
}
#endif // DEBUG

//------------------------------------------------------------------------
// LinearScan::makeRegisterInactive: Make the interval currently assigned to
//                                   a register inactive.
//
// Arguments:
//    physRegRecord - the RegRecord for the register
//
// Return Value:
//    None.
//
// Notes:
//    It may be that the RegRecord has already been freed, e.g. due to a kill,
//    or it may be that the register was a copyReg, so is not the assigned register
//    of the Interval currently occupying the register, in which case this method has no effect.
//
void LinearScan::makeRegisterInactive(RegRecord* physRegRecord)
{
    Interval* assignedInterval = physRegRecord->assignedInterval;
    // It may have already been freed by a "Kill"
    if ((assignedInterval != nullptr) && (assignedInterval->physReg == physRegRecord->regNum))
    {
        assignedInterval->isActive = false;
        if (assignedInterval->isConstant)
        {
            clearNextIntervalRef(physRegRecord->regNum, assignedInterval->registerType);
        }
    }
}

//------------------------------------------------------------------------
// LinearScan::freeRegister: Make a register available for use
//
// Arguments:
//    physRegRecord - the RegRecord for the register to be freed.
//
// Return Value:
//    None.
//
// Assumptions:
//    None.
//    It may be that the RegRecord has already been freed, e.g. due to a kill,
//    in which case this method has no effect.
//
// Notes:
//    If there is currently an Interval assigned to this register, and it has
//    more references (i.e. this is a local last-use, but more uses and/or
//    defs remain), it will remain assigned to the physRegRecord.  However, since
//    it is marked inactive, the register will be available, albeit less desirable
//    to allocate.
//
void LinearScan::freeRegister(RegRecord* physRegRecord)
{
    Interval* assignedInterval = physRegRecord->assignedInterval;
    makeRegAvailable(physRegRecord->regNum, physRegRecord->registerType);
    clearSpillCost(physRegRecord->regNum, physRegRecord->registerType);
    makeRegisterInactive(physRegRecord);

    if (assignedInterval != nullptr)
    {
        // TODO: Under the following conditions we should be just putting it in regsToMakeInactive
        // not regsToFree.
        //
        // We don't unassign in the following conditions:
        // - If this is a constant node, that we may encounter again, OR
        // - If its recent RefPosition is not a last-use and its next RefPosition is non-null.
        // - If there are no more RefPositions, or the next
        //   one is a def.  Note that the latter condition doesn't actually ensure that
        //   there aren't subsequent uses that could be reached by a value in the assigned
        //   register, but is merely a heuristic to avoid tying up the register (or using
        //   it when it's non-optimal).  A better alternative would be to use SSA, so that
        //   we wouldn't unnecessarily link separate live ranges to the same register.
        //
        RefPosition* nextRefPosition = assignedInterval->getNextRefPosition();
        if (!assignedInterval->isConstant && (nextRefPosition == nullptr || RefTypeIsDef(nextRefPosition->refType)))
        {
#ifdef TARGET_ARM
            assert((assignedInterval->registerType != TYP_DOUBLE) || genIsValidDoubleReg(physRegRecord->regNum));
#endif // TARGET_ARM
            unassignPhysReg(physRegRecord, nullptr);
        }
    }
}

//------------------------------------------------------------------------
// LinearScan::freeRegisters: Free the registers in 'regsToFree'
//
// Arguments:
//    regsToFree         - the mask of registers to free
//
void LinearScan::freeRegisters(regMaskTP regsToFree)
{
    if (regsToFree == RBM_NONE)
    {
        return;
    }

    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_FREE_REGS));
    makeRegsAvailable(regsToFree);
    while (regsToFree != RBM_NONE)
    {
        regMaskTP nextRegBit = genFindLowestBit(regsToFree);
        regsToFree &= ~nextRegBit;
        regNumber  nextReg   = genRegNumFromMask(nextRegBit);
        RegRecord* regRecord = getRegisterRecord(nextReg);
#ifdef TARGET_ARM
        if (regRecord->assignedInterval != nullptr && (regRecord->assignedInterval->registerType == TYP_DOUBLE))
        {
            assert(genIsValidDoubleReg(nextReg));
            regsToFree &= ~(nextRegBit << 1);
        }
#endif
        freeRegister(regRecord);
    }
}

//------------------------------------------------------------------------
// LinearScan::allocateRegisters: Perform the actual register allocation by iterating over
//                                all of the previously constructed Intervals
//
void LinearScan::allocateRegisters()
{
    JITDUMP("*************** In LinearScan::allocateRegisters()\n");
    DBEXEC(VERBOSE, lsraDumpIntervals("before allocateRegisters"));

    // at start, nothing is active except for register args
    for (Interval& interval : intervals)
    {
        Interval* currentInterval          = &interval;
        currentInterval->recentRefPosition = nullptr;
        currentInterval->isActive          = false;
        if (currentInterval->isLocalVar)
        {
            LclVarDsc* varDsc = currentInterval->getLocalVar(compiler);
            if (varDsc->lvIsRegArg && currentInterval->firstRefPosition != nullptr)
            {
                currentInterval->isActive = true;
            }
        }
    }

    if (enregisterLocalVars)
    {
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        VarSetOps::Iter largeVectorVarsIter(compiler, largeVectorVars);
        unsigned        largeVectorVarIndex = 0;
        while (largeVectorVarsIter.NextElem(&largeVectorVarIndex))
        {
            Interval* lclVarInterval           = getIntervalForLocalVar(largeVectorVarIndex);
            lclVarInterval->isPartiallySpilled = false;
        }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    }

    resetRegState();
    for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
    {
        RegRecord* physRegRecord         = getRegisterRecord(reg);
        physRegRecord->recentRefPosition = nullptr;
        updateNextFixedRef(physRegRecord, physRegRecord->firstRefPosition);

        // Is this an incoming arg register? (Note that we don't, currently, consider reassigning
        // an incoming arg register as having spill cost.)
        Interval* interval = physRegRecord->assignedInterval;
        if (interval != nullptr)
        {
#ifdef TARGET_ARM
            if ((interval->registerType != TYP_DOUBLE) || genIsValidDoubleReg(reg))
#endif // TARGET_ARM
            {
                updateNextIntervalRef(reg, interval);
                updateSpillCost(reg, interval);
                setRegInUse(reg, interval->registerType);
                INDEBUG(registersToDump |= getRegMask(reg, interval->registerType));
            }
        }
        else
        {
            clearNextIntervalRef(reg, physRegRecord->registerType);
            clearSpillCost(reg, physRegRecord->registerType);
        }
    }

#ifdef DEBUG
    if (VERBOSE)
    {
        dumpRefPositions("BEFORE ALLOCATION");
        dumpVarRefPositions("BEFORE ALLOCATION");

        printf("\n\nAllocating Registers\n"
               "--------------------\n");
        // Start with a small set of commonly used registers, so that we don't keep having to print a new title.
        // Include all the arg regs, as they may already have values assigned to them.
        registersToDump = LsraLimitSmallIntSet | LsraLimitSmallFPSet | RBM_ARG_REGS;
        dumpRegRecordHeader();
        // Now print an empty "RefPosition", since we complete the dump of the regs at the beginning of the loop.
        printf(indentFormat, "");
    }
#endif // DEBUG

    BasicBlock* currentBlock = nullptr;

    LsraLocation prevLocation            = MinLocation;
    regMaskTP    regsToFree              = RBM_NONE;
    regMaskTP    delayRegsToFree         = RBM_NONE;
    regMaskTP    regsToMakeInactive      = RBM_NONE;
    regMaskTP    delayRegsToMakeInactive = RBM_NONE;
    regMaskTP    copyRegsToFree          = RBM_NONE;
    regsInUseThisLocation                = RBM_NONE;
    regsInUseNextLocation                = RBM_NONE;

    // This is the most recent RefPosition for which a register was allocated
    // - currently only used for DEBUG but maintained in non-debug, for clarity of code
    //   (and will be optimized away because in non-debug spillAlways() unconditionally returns false)
    RefPosition* lastAllocatedRefPosition = nullptr;

    bool handledBlockEnd = false;

    for (RefPosition& refPositionIterator : refPositions)
    {
        RefPosition* currentRefPosition = &refPositionIterator;
        RefPosition* nextRefPosition    = currentRefPosition->nextRefPosition;

        // TODO: Can we combine this with the freeing of registers below? It might
        // mess with the dump, since this was previously being done before the call below
        // to dumpRegRecords.
        regMaskTP tempRegsToMakeInactive = (regsToMakeInactive | delayRegsToMakeInactive);
        while (tempRegsToMakeInactive != RBM_NONE)
        {
            regMaskTP nextRegBit = genFindLowestBit(tempRegsToMakeInactive);
            tempRegsToMakeInactive &= ~nextRegBit;
            regNumber  nextReg   = genRegNumFromMask(nextRegBit);
            RegRecord* regRecord = getRegisterRecord(nextReg);
            clearSpillCost(regRecord->regNum, regRecord->registerType);
            makeRegisterInactive(regRecord);
        }
        if (currentRefPosition->nodeLocation > prevLocation)
        {
            makeRegsAvailable(regsToMakeInactive);
            // TODO: Clean this up. We need to make the delayRegs inactive as well, but don't want
            // to mark them as free yet.
            regsToMakeInactive |= delayRegsToMakeInactive;
            regsToMakeInactive      = delayRegsToMakeInactive;
            delayRegsToMakeInactive = RBM_NONE;
        }

#ifdef DEBUG
        // Set the activeRefPosition to null until we're done with any boundary handling.
        activeRefPosition = nullptr;
        if (VERBOSE)
        {
            // We're really dumping the RegRecords "after" the previous RefPosition, but it's more convenient
            // to do this here, since there are a number of "continue"s in this loop.
            dumpRegRecords();
        }
#endif // DEBUG

        // This is the previousRefPosition of the current Referent, if any
        RefPosition* previousRefPosition = nullptr;

        Interval*      currentInterval = nullptr;
        Referenceable* currentReferent = nullptr;
        RefType        refType         = currentRefPosition->refType;

        currentReferent = currentRefPosition->referent;

        if (spillAlways() && lastAllocatedRefPosition != nullptr && !lastAllocatedRefPosition->IsPhysRegRef() &&
            !lastAllocatedRefPosition->getInterval()->isInternal &&
            (RefTypeIsDef(lastAllocatedRefPosition->refType) || lastAllocatedRefPosition->getInterval()->isLocalVar))
        {
            assert(lastAllocatedRefPosition->registerAssignment != RBM_NONE);
            RegRecord* regRecord = lastAllocatedRefPosition->getInterval()->assignedReg;
            unassignPhysReg(regRecord, lastAllocatedRefPosition);
            // Now set lastAllocatedRefPosition to null, so that we don't try to spill it again
            lastAllocatedRefPosition = nullptr;
        }

        // We wait to free any registers until we've completed all the
        // uses for the current node.
        // This avoids reusing registers too soon.
        // We free before the last true def (after all the uses & internal
        // registers), and then again at the beginning of the next node.
        // This is made easier by assigning two LsraLocations per node - one
        // for all the uses, internal registers & all but the last def, and
        // another for the final def (if any).

        LsraLocation currentLocation = currentRefPosition->nodeLocation;

        // Free at a new location.
        if (currentLocation > prevLocation)
        {
            // CopyRegs are simply made available - we don't want to make the associated interval inactive.
            makeRegsAvailable(copyRegsToFree);
            copyRegsToFree        = RBM_NONE;
            regsInUseThisLocation = regsInUseNextLocation;
            regsInUseNextLocation = RBM_NONE;
            if ((regsToFree | delayRegsToFree) != RBM_NONE)
            {
                freeRegisters(regsToFree);
                if ((currentLocation > (prevLocation + 1)) && (delayRegsToFree != RBM_NONE))
                {
                    // We should never see a delayReg that is delayed until a Location that has no RefPosition
                    // (that would be the RefPosition that it was supposed to interfere with).
                    assert(!"Found a delayRegFree associated with Location with no reference");
                    // However, to be cautious for the Release build case, we will free them.
                    freeRegisters(delayRegsToFree);
                    delayRegsToFree       = RBM_NONE;
                    regsInUseThisLocation = RBM_NONE;
                }
                regsToFree      = delayRegsToFree;
                delayRegsToFree = RBM_NONE;

#ifdef DEBUG
                // Validate the current state just after we've freed the registers. This ensures that any pending
                // freed registers will have had their state updated to reflect the intervals they were holding.
                for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
                {
                    regMaskTP regMask = genRegMask(reg);
                    // If this isn't available or if it's still waiting to be freed (i.e. it was in
                    // delayRegsToFree and so now it's in regsToFree), then skip it.
                    if ((regMask & (availableIntRegs | availableFloatRegs) & ~regsToFree) == RBM_NONE)
                    {
                        continue;
                    }
                    RegRecord* physRegRecord    = getRegisterRecord(reg);
                    Interval*  assignedInterval = physRegRecord->assignedInterval;
                    if (assignedInterval != nullptr)
                    {
                        bool         isAssignedReg     = (assignedInterval->physReg == reg);
                        RefPosition* recentRefPosition = assignedInterval->recentRefPosition;
                        // If we have a copyReg or a moveReg, we might have assigned this register to an Interval,
                        // but that isn't considered its assignedReg.
                        if (recentRefPosition != nullptr)
                        {
                            if (recentRefPosition->refType == RefTypeExpUse)
                            {
                                // We don't update anything on these, as they're just placeholders to extend the
                                // lifetime.
                                continue;
                            }
                            // For copyReg or moveReg, we don't have anything further to assert.
                            if (recentRefPosition->copyReg || recentRefPosition->moveReg)
                            {
                                continue;
                            }
                            assert(assignedInterval->isConstant == isRegConstant(reg, assignedInterval->registerType));
                            if (assignedInterval->isActive)
                            {
                                // If this is not the register most recently allocated, it must be from a copyReg,
                                // or it was placed there by the inVarToRegMap. In either case it must be a lclVar.

                                if (!isAssignedToInterval(assignedInterval, physRegRecord))
                                {
                                    assert(assignedInterval->isLocalVar);
                                    // We'd like to assert that this was either set by the inVarToRegMap, or by
                                    // a copyReg, but we can't traverse backward to check for a copyReg, because
                                    // we only have recentRefPosition, and there may be a previous RefPosition
                                    // at the same Location with a copyReg.
                                }
                                if (isAssignedReg)
                                {
                                    assert(nextIntervalRef[reg] == assignedInterval->getNextRefLocation());
                                    assert(!isRegAvailable(reg, assignedInterval->registerType));
                                    assert((recentRefPosition == nullptr) ||
                                           (spillCost[reg] == getSpillWeight(physRegRecord)));
                                }
                                else
                                {
                                    assert((nextIntervalRef[reg] == MaxLocation) ||
                                           isRegBusy(reg, assignedInterval->registerType));
                                }
                            }
                            else
                            {
                                if ((assignedInterval->physReg == reg) && !assignedInterval->isConstant)
                                {
                                    assert(nextIntervalRef[reg] == assignedInterval->getNextRefLocation());
                                }
                                else
                                {
                                    assert(nextIntervalRef[reg] == MaxLocation);
                                    assert(isRegAvailable(reg, assignedInterval->registerType));
                                    assert(spillCost[reg] == 0);
                                }
                            }
                        }
                    }
                    else
                    {
                        assert(isRegAvailable(reg, physRegRecord->registerType));
                        assert(!isRegConstant(reg, physRegRecord->registerType));
                        assert(nextIntervalRef[reg] == MaxLocation);
                        assert(spillCost[reg] == 0);
                    }
                    LsraLocation thisNextFixedRef = physRegRecord->getNextRefLocation();
                    assert(nextFixedRef[reg] == thisNextFixedRef);
#ifdef TARGET_ARM
                    // If this is occupied by a double interval, skip the corresponding float reg.
                    if ((assignedInterval != nullptr) && (assignedInterval->registerType == TYP_DOUBLE))
                    {
                        reg = REG_NEXT(reg);
                    }
#endif
                }
#endif // DEBUG
            }
        }
        prevLocation = currentLocation;

        // get previous refposition, then current refpos is the new previous
        if (currentReferent != nullptr)
        {
            previousRefPosition                = currentReferent->recentRefPosition;
            currentReferent->recentRefPosition = currentRefPosition;
        }
        else
        {
            assert((refType == RefTypeBB) || (refType == RefTypeKillGCRefs));
        }

#ifdef DEBUG
        activeRefPosition = currentRefPosition;

        // For the purposes of register resolution, we handle the DummyDefs before
        // the block boundary - so the RefTypeBB is after all the DummyDefs.
        // However, for the purposes of allocation, we want to handle the block
        // boundary first, so that we can free any registers occupied by lclVars
        // that aren't live in the next block and make them available for the
        // DummyDefs.

        // If we've already handled the BlockEnd, but now we're seeing the RefTypeBB,
        // dump it now.
        if ((refType == RefTypeBB) && handledBlockEnd)
        {
            dumpNewBlock(currentBlock, currentRefPosition->nodeLocation);
        }
#endif // DEBUG

        if (!handledBlockEnd && (refType == RefTypeBB || refType == RefTypeDummyDef))
        {
            // Free any delayed regs (now in regsToFree) before processing the block boundary
            freeRegisters(regsToFree);
            regsToFree            = RBM_NONE;
            regsInUseThisLocation = RBM_NONE;
            regsInUseNextLocation = RBM_NONE;
            handledBlockEnd       = true;
            curBBStartLocation    = currentRefPosition->nodeLocation;
            if (currentBlock == nullptr)
            {
                currentBlock = startBlockSequence();
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_START_BB, nullptr, REG_NA, compiler->fgFirstBB));
            }
            else
            {
                processBlockEndAllocation(currentBlock);
                currentBlock = moveToNextBlock();
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_START_BB, nullptr, REG_NA, currentBlock));
            }
        }

        if (refType == RefTypeBB)
        {
            handledBlockEnd = false;
            continue;
        }

        if (refType == RefTypeKillGCRefs)
        {
            spillGCRefs(currentRefPosition);
            continue;
        }

        if (currentRefPosition->isPhysRegRef)
        {
            RegRecord* regRecord        = currentRefPosition->getReg();
            Interval*  assignedInterval = regRecord->assignedInterval;

            updateNextFixedRef(regRecord, currentRefPosition->nextRefPosition);

            // If this is a FixedReg, disassociate any inactive constant interval from this register.
            // Otherwise, do nothing.
            if (refType == RefTypeFixedReg)
            {
                if (assignedInterval != nullptr && !assignedInterval->isActive && assignedInterval->isConstant)
                {
                    clearConstantReg(regRecord->regNum, assignedInterval->registerType);
                    regRecord->assignedInterval  = nullptr;
                    spillCost[regRecord->regNum] = 0;

#ifdef TARGET_ARM
                    // Update overlapping floating point register for TYP_DOUBLE
                    if (assignedInterval->registerType == TYP_DOUBLE)
                    {
                        RegRecord* otherRegRecord = findAnotherHalfRegRec(regRecord);
                        assert(otherRegRecord->assignedInterval == assignedInterval);
                        otherRegRecord->assignedInterval  = nullptr;
                        spillCost[otherRegRecord->regNum] = 0;
                    }
#endif // TARGET_ARM
                }
                regsInUseThisLocation |= currentRefPosition->registerAssignment;
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_FIXED_REG, nullptr, currentRefPosition->assignedReg()));
                continue;
            }
            if (refType == RefTypeKill)
            {
                if (assignedInterval != nullptr)
                {
                    unassignPhysReg(regRecord, assignedInterval->recentRefPosition);
                    clearConstantReg(regRecord->regNum, assignedInterval->registerType);
                    makeRegAvailable(regRecord->regNum, assignedInterval->registerType);
                }
                clearRegBusyUntilKill(regRecord->regNum);
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_KEPT_ALLOCATION, nullptr, regRecord->regNum));
                continue;
            }
        }

        // If this is an exposed use, do nothing - this is merely a placeholder to attempt to
        // ensure that a register is allocated for the full lifetime.  The resolution logic
        // will take care of moving to the appropriate register if needed.

        if (refType == RefTypeExpUse)
        {
            INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_EXP_USE));
            currentInterval = currentRefPosition->getInterval();
            if (currentInterval->physReg != REG_NA)
            {
                updateNextIntervalRef(currentInterval->physReg, currentInterval);
            }
            continue;
        }

        regNumber assignedRegister = REG_NA;

        assert(currentRefPosition->isIntervalRef());
        currentInterval = currentRefPosition->getInterval();
        assert(currentInterval != nullptr);
        assignedRegister = currentInterval->physReg;

        // Identify the special cases where we decide up-front not to allocate
        bool allocate = true;
        bool didDump  = false;

        if (refType == RefTypeParamDef || refType == RefTypeZeroInit)
        {
            if (nextRefPosition == nullptr)
            {
                // If it has no actual references, mark it as "lastUse"; since they're not actually part
                // of any flow they won't have been marked during dataflow.  Otherwise, if we allocate a
                // register we won't unassign it.
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_ZERO_REF, currentInterval));
                currentRefPosition->lastUse = true;
            }
            LclVarDsc* varDsc = currentInterval->getLocalVar(compiler);
            assert(varDsc != nullptr);
            assert(!blockInfo[compiler->fgFirstBB->bbNum].hasEHBoundaryIn || currentInterval->isWriteThru);
            if (blockInfo[compiler->fgFirstBB->bbNum].hasEHBoundaryIn ||
                blockInfo[compiler->fgFirstBB->bbNum].hasEHPred)
            {
                allocate = false;
            }
            else if (refType == RefTypeParamDef && (varDsc->lvRefCntWtd() <= BB_UNITY_WEIGHT) &&
                     (!currentRefPosition->lastUse || (currentInterval->physReg == REG_STK)))
            {
                // If this is a low ref-count parameter, and either it is used (def is not the last use) or it's
                // passed on the stack, don't allocate a register.
                // Note that if this is an unused register parameter we don't want to set allocate to false because that
                // will cause us to allocate stack space to spill it.
                allocate = false;
            }
            else if ((currentInterval->physReg == REG_STK) && nextRefPosition->treeNode->OperIs(GT_BITCAST))
            {
                // In the case of ABI mismatches, avoid allocating a register only to have to immediately move
                // it to a different register file.
                allocate = false;
            }
            if (!allocate)
            {
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_NO_ENTRY_REG_ALLOCATED, currentInterval));
                didDump = true;
                setIntervalAsSpilled(currentInterval);
                if (assignedRegister != REG_NA)
                {
                    clearNextIntervalRef(assignedRegister, currentInterval->registerType);
                    clearSpillCost(assignedRegister, currentInterval->registerType);
                    makeRegAvailable(assignedRegister, currentInterval->registerType);
                }
            }
        }
#ifdef FEATURE_SIMD
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        else if (currentInterval->isUpperVector)
        {
            // This is a save or restore of the upper half of a large vector lclVar.
            Interval* lclVarInterval = currentInterval->relatedInterval;
            assert(lclVarInterval->isLocalVar);
            if (refType == RefTypeUpperVectorSave)
            {
                if ((lclVarInterval->physReg == REG_NA) ||
                    (lclVarInterval->isPartiallySpilled && (currentInterval->physReg == REG_STK)))
                {
                    allocate = false;
                }
                else
                {
                    lclVarInterval->isPartiallySpilled = true;
                }
            }
            else if (refType == RefTypeUpperVectorRestore)
            {
                assert(currentInterval->isUpperVector);
                if (lclVarInterval->isPartiallySpilled)
                {
                    lclVarInterval->isPartiallySpilled = false;
                }
                else
                {
                    allocate = false;
                }
            }
        }
        else if (refType == RefTypeUpperVectorSave)
        {
            assert(!currentInterval->isLocalVar);
            // Note that this case looks a lot like the case below, but in this case we need to spill
            // at the previous RefPosition.
            // We may want to consider allocating two callee-save registers for this case, but it happens rarely
            // enough that it may not warrant the additional complexity.
            if (assignedRegister != REG_NA)
            {
                unassignPhysReg(getRegisterRecord(assignedRegister), currentInterval->firstRefPosition);
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_NO_REG_ALLOCATED, currentInterval));
            }
            currentRefPosition->registerAssignment = RBM_NONE;
            continue;
        }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
#endif // FEATURE_SIMD

        if (allocate == false)
        {
            if (assignedRegister != REG_NA)
            {
                unassignPhysReg(getRegisterRecord(assignedRegister), currentRefPosition);
            }
            else if (!didDump)
            {
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_NO_REG_ALLOCATED, currentInterval));
                didDump = true;
            }
            currentRefPosition->registerAssignment = RBM_NONE;
            continue;
        }

        if (currentInterval->isSpecialPutArg)
        {
            assert(!currentInterval->isLocalVar);
            Interval* srcInterval = currentInterval->relatedInterval;
            assert(srcInterval != nullptr && srcInterval->isLocalVar);
            if (refType == RefTypeDef)
            {
                assert(srcInterval->recentRefPosition->nodeLocation == currentLocation - 1);
                RegRecord* physRegRecord = srcInterval->assignedReg;

                // For a putarg_reg to be special, its next use location has to be the same
                // as fixed reg's next kill location. Otherwise, if source lcl var's next use
                // is after the kill of fixed reg but before putarg_reg's next use, fixed reg's
                // kill would lead to spill of source but not the putarg_reg if it were treated
                // as special.
                if (srcInterval->isActive &&
                    genRegMask(srcInterval->physReg) == currentRefPosition->registerAssignment &&
                    currentInterval->getNextRefLocation() == nextFixedRef[srcInterval->physReg])
                {
                    assert(physRegRecord->regNum == srcInterval->physReg);

                    // Special putarg_reg acts as a pass-thru since both source lcl var
                    // and putarg_reg have the same register allocated.  Physical reg
                    // record of reg continue to point to source lcl var's interval
                    // instead of to putarg_reg's interval.  So if a spill of reg
                    // allocated to source lcl var happens, to reallocate to another
                    // tree node, before its use at call node it will lead to spill of
                    // lcl var instead of putarg_reg since physical reg record is pointing
                    // to lcl var's interval. As a result, arg reg would get trashed leading
                    // to bad codegen. The assumption here is that source lcl var of a
                    // special putarg_reg doesn't get spilled and re-allocated prior to
                    // its use at the call node.  This is ensured by marking physical reg
                    // record as busy until next kill.
                    setRegBusyUntilKill(srcInterval->physReg, srcInterval->registerType);
                }
                else
                {
                    currentInterval->isSpecialPutArg = false;
                }
            }
            // If this is still a SpecialPutArg, continue;
            if (currentInterval->isSpecialPutArg)
            {
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_SPECIAL_PUTARG, currentInterval,
                                                currentRefPosition->assignedReg()));
                continue;
            }
        }

        if (assignedRegister == REG_NA && RefTypeIsUse(refType))
        {
            currentRefPosition->reload = true;
            INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_RELOAD, currentInterval, assignedRegister));
        }

        regMaskTP assignedRegBit = RBM_NONE;
        bool      isInRegister   = false;
        if (assignedRegister != REG_NA)
        {
            isInRegister   = true;
            assignedRegBit = genRegMask(assignedRegister);
            if (!currentInterval->isActive)
            {
                // If this is a use, it must have started the block on the stack, but the register
                // was available for use so we kept the association.
                if (RefTypeIsUse(refType))
                {
                    assert(enregisterLocalVars);
                    assert(inVarToRegMaps[curBBNum][currentInterval->getVarIndex(compiler)] == REG_STK &&
                           previousRefPosition->nodeLocation <= curBBStartLocation);
                    isInRegister = false;
                }
                else
                {
                    currentInterval->isActive = true;
                    setRegInUse(assignedRegister, currentInterval->registerType);
                    updateSpillCost(assignedRegister, currentInterval);
                }
                updateNextIntervalRef(assignedRegister, currentInterval);
            }
            assert(currentInterval->assignedReg != nullptr &&
                   currentInterval->assignedReg->regNum == assignedRegister &&
                   currentInterval->assignedReg->assignedInterval == currentInterval);
        }

        if (previousRefPosition != nullptr)
        {
            assert(previousRefPosition->nextRefPosition == currentRefPosition);
            assert(assignedRegister == REG_NA || assignedRegBit == previousRefPosition->registerAssignment ||
                   currentRefPosition->outOfOrder || previousRefPosition->copyReg ||
                   previousRefPosition->refType == RefTypeExpUse || currentRefPosition->refType == RefTypeDummyDef);
        }
        else if (assignedRegister != REG_NA)
        {
            // Handle the case where this is a preassigned register (i.e. parameter).
            // We don't want to actually use the preassigned register if it's not
            // going to cover the lifetime - but we had to preallocate it to ensure
            // that it remained live.
            // TODO-CQ: At some point we may want to refine the analysis here, in case
            // it might be beneficial to keep it in this reg for PART of the lifetime
            if (currentInterval->isLocalVar)
            {
                regMaskTP preferences        = currentInterval->registerPreferences;
                bool      keepAssignment     = true;
                bool      matchesPreferences = (preferences & genRegMask(assignedRegister)) != RBM_NONE;

                // Will the assigned register cover the lifetime?  If not, does it at least
                // meet the preferences for the next RefPosition?
                LsraLocation nextPhysRegLocation = nextFixedRef[assignedRegister];
                if (nextPhysRegLocation <= currentInterval->lastRefPosition->nodeLocation)
                {
                    // Check to see if the existing assignment matches the preferences (e.g. callee save registers)
                    // and ensure that the next use of this localVar does not occur after the nextPhysRegRefPos
                    // There must be a next RefPosition, because we know that the Interval extends beyond the
                    // nextPhysRegRefPos.
                    assert(nextRefPosition != nullptr);
                    if (!matchesPreferences || nextPhysRegLocation < nextRefPosition->nodeLocation)
                    {
                        keepAssignment = false;
                    }
                    else if ((nextRefPosition->registerAssignment != assignedRegBit) &&
                             (nextPhysRegLocation <= nextRefPosition->getRefEndLocation()))
                    {
                        keepAssignment = false;
                    }
                }
                else if (refType == RefTypeParamDef && !matchesPreferences)
                {
                    // Don't use the register, even if available, if it doesn't match the preferences.
                    // Note that this case is only for ParamDefs, for which we haven't yet taken preferences
                    // into account (we've just automatically got the initial location).  In other cases,
                    // we would already have put it in a preferenced register, if it was available.
                    // TODO-CQ: Consider expanding this to check availability - that would duplicate
                    // code here, but otherwise we may wind up in this register anyway.
                    keepAssignment = false;
                }

                if (keepAssignment == false)
                {
                    RegRecord* physRegRecord               = getRegisterRecord(currentInterval->physReg);
                    currentRefPosition->registerAssignment = allRegs(currentInterval->registerType);
                    currentRefPosition->isFixedRegRef      = false;
                    unassignPhysRegNoSpill(physRegRecord);

                    // If the preferences are currently set to just this register, reset them to allRegs
                    // of the appropriate type (just as we just reset the registerAssignment for this
                    // RefPosition.
                    // Otherwise, simply remove this register from the preferences, if it's there.

                    if (currentInterval->registerPreferences == assignedRegBit)
                    {
                        currentInterval->registerPreferences = currentRefPosition->registerAssignment;
                    }
                    else
                    {
                        currentInterval->registerPreferences &= ~assignedRegBit;
                    }

                    assignedRegister = REG_NA;
                    assignedRegBit   = RBM_NONE;
                }
            }
        }

        if (assignedRegister != REG_NA)
        {
            RegRecord* physRegRecord = getRegisterRecord(assignedRegister);
            assert((assignedRegBit == currentRefPosition->registerAssignment) ||
                   (physRegRecord->assignedInterval == currentInterval) ||
                   !isRegInUse(assignedRegister, currentInterval->registerType));
            if (conflictingFixedRegReference(assignedRegister, currentRefPosition))
            {
                // We may have already reassigned the register to the conflicting reference.
                // If not, we need to unassign this interval.
                if (physRegRecord->assignedInterval == currentInterval)
                {
                    unassignPhysRegNoSpill(physRegRecord);
                    physRegRecord->assignedInterval = nullptr;
                    clearConstantReg(assignedRegister, currentInterval->registerType);
                }
                currentRefPosition->moveReg = true;
                assignedRegister            = REG_NA;
                currentRefPosition->registerAssignment &= ~assignedRegBit;
                setIntervalAsSplit(currentInterval);
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_MOVE_REG, currentInterval, assignedRegister));
            }
            else if ((genRegMask(assignedRegister) & currentRefPosition->registerAssignment) != 0)
            {
                currentRefPosition->registerAssignment = assignedRegBit;
                if (!currentInterval->isActive)
                {
                    // If we've got an exposed use at the top of a block, the
                    // interval might not have been active.  Otherwise if it's a use,
                    // the interval must be active.
                    if (refType == RefTypeDummyDef)
                    {
                        currentInterval->isActive = true;
                        assert(getRegisterRecord(assignedRegister)->assignedInterval == currentInterval);
                    }
                    else
                    {
                        currentRefPosition->reload = true;
                    }
                }
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_KEPT_ALLOCATION, currentInterval, assignedRegister));
            }
            else
            {
                // It's already in a register, but not one we need.
                if (!RefTypeIsDef(currentRefPosition->refType))
                {
                    regNumber copyReg        = assignCopyReg(currentRefPosition);
                    lastAllocatedRefPosition = currentRefPosition;
                    bool unassign            = false;
                    if (currentInterval->isWriteThru)
                    {
                        if (currentRefPosition->refType == RefTypeDef)
                        {
                            currentRefPosition->writeThru = true;
                        }
                        if (!currentRefPosition->lastUse)
                        {
                            if (currentRefPosition->spillAfter)
                            {
                                unassign = true;
                            }
                        }
                    }
                    regMaskTP copyRegMask     = getRegMask(copyReg, currentInterval->registerType);
                    regMaskTP assignedRegMask = getRegMask(assignedRegister, currentInterval->registerType);
                    regsInUseThisLocation |= copyRegMask | assignedRegMask;
                    if (currentRefPosition->lastUse)
                    {
                        if (currentRefPosition->delayRegFree)
                        {
                            INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_LAST_USE_DELAYED, currentInterval,
                                                            assignedRegister));
                            delayRegsToFree |= copyRegMask | assignedRegMask;
                            regsInUseNextLocation |= copyRegMask | assignedRegMask;
                        }
                        else
                        {
                            INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_LAST_USE, currentInterval, assignedRegister));
                            regsToFree |= copyRegMask | assignedRegMask;
                        }
                    }
                    else
                    {
                        copyRegsToFree |= copyRegMask;
                        if (currentRefPosition->delayRegFree)
                        {
                            regsInUseNextLocation |= copyRegMask | assignedRegMask;
                        }
                    }

                    // If this is a tree temp (non-localVar) interval, we will need an explicit move.
                    // Note: In theory a moveReg should cause the Interval to now have the new reg as its
                    // assigned register. However, that's not currently how this works.
                    // If we ever actually move lclVar intervals instead of copying, this will need to change.
                    if (!currentInterval->isLocalVar)
                    {
                        currentRefPosition->moveReg = true;
                        currentRefPosition->copyReg = false;
                    }
                    clearNextIntervalRef(copyReg, currentInterval->registerType);
                    clearSpillCost(copyReg, currentInterval->registerType);
                    updateNextIntervalRef(assignedRegister, currentInterval);
                    updateSpillCost(assignedRegister, currentInterval);
                    continue;
                }
                else
                {
                    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_NEEDS_NEW_REG, nullptr, assignedRegister));
                    regsToFree |= getRegMask(assignedRegister, currentInterval->registerType);
                    // We want a new register, but we don't want this to be considered a spill.
                    assignedRegister = REG_NA;
                    if (physRegRecord->assignedInterval == currentInterval)
                    {
                        unassignPhysRegNoSpill(physRegRecord);
                    }
                }
            }
        }

        if (assignedRegister == REG_NA)
        {
            if (currentRefPosition->RegOptional())
            {
                // We can avoid allocating a register if it is a last use requiring a reload.
                if (currentRefPosition->lastUse && currentRefPosition->reload)
                {
                    allocate = false;
                }
                else if (currentInterval->isWriteThru)
                {
                    // Don't allocate if the next reference is in a cold block.
                    if (nextRefPosition == nullptr || (nextRefPosition->nodeLocation >= firstColdLoc))
                    {
                        allocate = false;
                    }
                }

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE && defined(TARGET_XARCH)
                // We can also avoid allocating a register (in fact we don't want to) if we have
                // an UpperVectorRestore on xarch where the value is on the stack.
                if ((currentRefPosition->refType == RefTypeUpperVectorRestore) && (currentInterval->physReg == REG_NA))
                {
                    assert(currentRefPosition->regOptional);
                    allocate = false;
                }
#endif

#ifdef DEBUG
                // Under stress mode, don't allocate registers to RegOptional RefPositions.
                if (allocate && regOptionalNoAlloc())
                {
                    allocate = false;
                }
#endif
            }

            RegisterScore registerScore = NONE;
            if (allocate)
            {
                // Allocate a register, if we must, or if it is profitable to do so.
                // If we have a fixed reg requirement, and the interval is inactive in another register,
                // unassign that register.
                if (currentRefPosition->isFixedRegRef && !currentInterval->isActive &&
                    (currentInterval->assignedReg != nullptr) &&
                    (currentInterval->assignedReg->assignedInterval == currentInterval) &&
                    (genRegMask(currentInterval->assignedReg->regNum) != currentRefPosition->registerAssignment))
                {
                    unassignPhysReg(currentInterval->assignedReg, nullptr);
                }
                assignedRegister = allocateReg(currentInterval, currentRefPosition DEBUG_ARG(&registerScore));
            }

            // If no register was found, this RefPosition must not require a register.
            if (assignedRegister == REG_NA)
            {
                assert(currentRefPosition->RegOptional());
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_NO_REG_ALLOCATED, currentInterval));
                currentRefPosition->registerAssignment = RBM_NONE;
                currentRefPosition->reload             = false;
                currentInterval->isActive              = false;
                setIntervalAsSpilled(currentInterval);
            }
#ifdef DEBUG
            else
            {
                if (VERBOSE)
                {
                    if (currentInterval->isConstant && (currentRefPosition->treeNode != nullptr) &&
                        currentRefPosition->treeNode->IsReuseRegVal())
                    {
                        dumpLsraAllocationEvent(LSRA_EVENT_REUSE_REG, currentInterval, assignedRegister, currentBlock,
                                                registerScore);
                    }
                    else
                    {
                        dumpLsraAllocationEvent(LSRA_EVENT_ALLOC_REG, currentInterval, assignedRegister, currentBlock,
                                                registerScore);
                    }
                }
            }
#endif // DEBUG

            if (refType == RefTypeDummyDef && assignedRegister != REG_NA)
            {
                setInVarRegForBB(curBBNum, currentInterval->varNum, assignedRegister);
            }

            // If we allocated a register, and this is a use of a spilled value,
            // it should have been marked for reload above.
            if (assignedRegister != REG_NA && RefTypeIsUse(refType) && !isInRegister)
            {
                assert(currentRefPosition->reload);
            }
        }

        // If we allocated a register, record it
        if (assignedRegister != REG_NA)
        {
            assignedRegBit    = genRegMask(assignedRegister);
            regMaskTP regMask = getRegMask(assignedRegister, currentInterval->registerType);
            regsInUseThisLocation |= regMask;
            if (currentRefPosition->delayRegFree)
            {
                regsInUseNextLocation |= regMask;
            }
            currentRefPosition->registerAssignment = assignedRegBit;

            currentInterval->physReg = assignedRegister;
            regsToFree &= ~regMask; // we'll set it again later if it's dead

            // If this interval is dead, free the register.
            // The interval could be dead if this is a user variable, or if the
            // node is being evaluated for side effects, or a call whose result
            // is not used, etc.
            // If this is an UpperVector we'll neither free it nor preference it
            // (it will be freed when it is used).
            bool unassign = false;
            if (!currentInterval->IsUpperVector())
            {
                if (currentInterval->isWriteThru)
                {
                    if (currentRefPosition->refType == RefTypeDef)
                    {
                        currentRefPosition->writeThru = true;
                    }
                    if (!currentRefPosition->lastUse)
                    {
                        if (currentRefPosition->spillAfter)
                        {
                            unassign = true;
                        }
                    }
                }
                if (currentRefPosition->lastUse || currentRefPosition->nextRefPosition == nullptr)
                {
                    assert(currentRefPosition->isIntervalRef());
                    // If this isn't a final use, we'll mark the register as available, but keep the association.
                    if ((refType != RefTypeExpUse) && (currentRefPosition->nextRefPosition == nullptr))
                    {
                        unassign = true;
                    }
                    else
                    {
                        if (currentRefPosition->delayRegFree)
                        {
                            delayRegsToMakeInactive |= regMask;
                        }
                        else
                        {
                            regsToMakeInactive |= regMask;
                        }
                        // TODO-Cleanup: this makes things consistent with previous, and will enable preferences
                        // to be propagated, but it seems less than ideal.
                        currentInterval->isActive = false;
                    }
                    // Update the register preferences for the relatedInterval, if this is 'preferencedToDef'.
                    // Don't propagate to subsequent relatedIntervals; that will happen as they are allocated, and we
                    // don't know yet whether the register will be retained.
                    if (currentInterval->relatedInterval != nullptr)
                    {
                        currentInterval->relatedInterval->updateRegisterPreferences(assignedRegBit);
                    }
                }

                if (unassign)
                {
                    if (currentRefPosition->delayRegFree)
                    {
                        delayRegsToFree |= regMask;

                        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_LAST_USE_DELAYED));
                    }
                    else
                    {
                        regsToFree |= regMask;

                        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_LAST_USE));
                    }
                }
            }
            if (!unassign)
            {
                updateNextIntervalRef(assignedRegister, currentInterval);
                updateSpillCost(assignedRegister, currentInterval);
            }
        }
        lastAllocatedRefPosition = currentRefPosition;
    }

#ifdef JIT32_GCENCODER
    // For the JIT32_GCENCODER, when lvaKeepAliveAndReportThis is true, we must either keep the "this" pointer
    // in the same register for the entire method, or keep it on the stack. Rather than imposing this constraint
    // as we allocate, we will force all refs to the stack if it is split or spilled.
    if (enregisterLocalVars && compiler->lvaKeepAliveAndReportThis())
    {
        LclVarDsc* thisVarDsc = compiler->lvaGetDesc(compiler->info.compThisArg);
        if (thisVarDsc->lvLRACandidate)
        {
            Interval* interval = getIntervalForLocalVar(thisVarDsc->lvVarIndex);
            if (interval->isSplit)
            {
                // We'll have to spill this.
                setIntervalAsSpilled(interval);
            }
            if (interval->isSpilled)
            {
                unsigned prevBBNum = 0;
                for (RefPosition* ref = interval->firstRefPosition; ref != nullptr; ref = ref->nextRefPosition)
                {
                    // For the resolution phase, we need to ensure that any block with exposed uses has the
                    // incoming reg for 'this' as REG_STK.
                    if (RefTypeIsUse(ref->refType) && (ref->bbNum != prevBBNum))
                    {
                        VarToRegMap inVarToRegMap = getInVarToRegMap(ref->bbNum);
                        setVarReg(inVarToRegMap, thisVarDsc->lvVarIndex, REG_STK);
                    }
                    if (ref->RegOptional())
                    {
                        ref->registerAssignment = RBM_NONE;
                        ref->reload             = false;
                        ref->spillAfter         = false;
                    }
                    switch (ref->refType)
                    {
                        case RefTypeDef:
                            if (ref->registerAssignment != RBM_NONE)
                            {
                                ref->spillAfter = true;
                            }
                            break;
                        case RefTypeUse:
                            if (ref->registerAssignment != RBM_NONE)
                            {
                                ref->reload     = true;
                                ref->spillAfter = true;
                                ref->copyReg    = false;
                                ref->moveReg    = false;
                            }
                            break;
                        default:
                            break;
                    }
                    prevBBNum = ref->bbNum;
                }
            }
        }
    }
#endif // JIT32_GCENCODER

    // Free registers to clear associated intervals for resolution phase
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    if (getLsraExtendLifeTimes())
    {
        // If we have extended lifetimes, we need to make sure all the registers are freed.
        for (size_t regNumIndex = 0; regNumIndex <= REG_FP_LAST; regNumIndex++)
        {
            RegRecord& regRecord = physRegs[regNumIndex];
            Interval*  interval  = regRecord.assignedInterval;
            if (interval != nullptr)
            {
                interval->isActive = false;
                unassignPhysReg(&regRecord, nullptr);
            }
        }
    }
    else
#endif // DEBUG
    {
        freeRegisters(regsToFree | delayRegsToFree);
    }

#ifdef DEBUG
    if (VERBOSE)
    {
        // Dump the RegRecords after the last RefPosition is handled.
        dumpRegRecords();
        printf("\n");

        dumpRefPositions("AFTER ALLOCATION");
        dumpVarRefPositions("AFTER ALLOCATION");

        // Dump the intervals that remain active
        printf("Active intervals at end of allocation:\n");

        // We COULD just reuse the intervalIter from above, but ArrayListIterator doesn't
        // provide a Reset function (!) - we'll probably replace this so don't bother
        // adding it

        for (Interval& interval : intervals)
        {
            if (interval.isActive)
            {
                printf("Active ");
                interval.dump();
            }
        }

        printf("\n");
    }
#endif // DEBUG
}

//-----------------------------------------------------------------------------
// updateAssignedInterval: Update assigned interval of register.
//
// Arguments:
//    reg      -    register to be updated
//    interval -    interval to be assigned
//    regType  -    register type
//
// Return Value:
//    None
//
// Assumptions:
//    For ARM32, when "regType" is TYP_DOUBLE, "reg" should be a even-numbered
//    float register, i.e. lower half of double register.
//
// Note:
//    For ARM32, two float registers consisting a double register are updated
//    together when "regType" is TYP_DOUBLE.
//
void LinearScan::updateAssignedInterval(RegRecord* reg, Interval* interval, RegisterType regType)
{
#ifdef TARGET_ARM
    // Update overlapping floating point register for TYP_DOUBLE.
    Interval* oldAssignedInterval = reg->assignedInterval;
    regNumber doubleReg           = REG_NA;
    if (regType == TYP_DOUBLE)
    {
        doubleReg                        = reg->regNum;
        RegRecord* anotherHalfReg        = getSecondHalfRegRec(reg);
        anotherHalfReg->assignedInterval = interval;
    }
    else if ((oldAssignedInterval != nullptr) && (oldAssignedInterval->registerType == TYP_DOUBLE))
    {
        RegRecord* anotherHalfReg        = findAnotherHalfRegRec(reg);
        doubleReg                        = genIsValidDoubleReg(reg->regNum) ? reg->regNum : anotherHalfReg->regNum;
        anotherHalfReg->assignedInterval = nullptr;
    }
    if (doubleReg != REG_NA)
    {
        clearNextIntervalRef(doubleReg, TYP_DOUBLE);
        clearSpillCost(doubleReg, TYP_DOUBLE);
        clearConstantReg(doubleReg, TYP_DOUBLE);
    }
#endif
    reg->assignedInterval = interval;
    if (interval != nullptr)
    {
        setRegInUse(reg->regNum, interval->registerType);
        if (interval->isConstant)
        {
            setConstantReg(reg->regNum, interval->registerType);
        }
        else
        {
            clearConstantReg(reg->regNum, interval->registerType);
        }
        updateNextIntervalRef(reg->regNum, interval);
        updateSpillCost(reg->regNum, interval);
    }
    else
    {
        clearNextIntervalRef(reg->regNum, reg->registerType);
        clearSpillCost(reg->regNum, reg->registerType);
    }
}

//-----------------------------------------------------------------------------
// updatePreviousInterval: Update previous interval of register.
//
// Arguments:
//    reg      -    register to be updated
//    interval -    interval to be assigned
//    regType  -    register type
//
// Return Value:
//    None
//
// Assumptions:
//    For ARM32, when "regType" is TYP_DOUBLE, "reg" should be a even-numbered
//    float register, i.e. lower half of double register.
//
// Note:
//    For ARM32, two float registers consisting a double register are updated
//    together when "regType" is TYP_DOUBLE.
//
void LinearScan::updatePreviousInterval(RegRecord* reg, Interval* interval, RegisterType regType)
{
    reg->previousInterval = interval;

#ifdef TARGET_ARM
    // Update overlapping floating point register for TYP_DOUBLE
    if (regType == TYP_DOUBLE)
    {
        RegRecord* anotherHalfReg = findAnotherHalfRegRec(reg);

        anotherHalfReg->previousInterval = interval;
    }
#endif
}

//-----------------------------------------------------------------------------
// writeLocalReg: Write the register assignment for a GT_LCL_VAR node.
//
// Arguments:
//    lclNode  - The GT_LCL_VAR node
//    varNum   - The variable number for the register
//    reg      - The assigned register
//
// Return Value:
//    None
//
// Note:
//    For a multireg node, 'varNum' will be the field local for the given register.
//
void LinearScan::writeLocalReg(GenTreeLclVar* lclNode, unsigned varNum, regNumber reg)
{
    assert((lclNode->GetLclNum() == varNum) == !lclNode->IsMultiReg());
    if (lclNode->GetLclNum() == varNum)
    {
        lclNode->SetRegNum(reg);
    }
    else
    {
        assert(compiler->lvaEnregMultiRegVars);
        LclVarDsc* parentVarDsc = compiler->lvaGetDesc(lclNode->GetLclNum());
        assert(parentVarDsc->lvPromoted);
        unsigned regIndex = varNum - parentVarDsc->lvFieldLclStart;
        assert(regIndex < MAX_MULTIREG_COUNT);
        lclNode->SetRegNumByIdx(reg, regIndex);
    }
}

//-----------------------------------------------------------------------------
// LinearScan::resolveLocalRef
// Description:
//      Update the graph for a local reference.
//      Also, track the register (if any) that is currently occupied.
// Arguments:
//      treeNode: The lclVar that's being resolved
//      currentRefPosition: the RefPosition associated with the treeNode
//
// Details:
// This method is called for each local reference, during the resolveRegisters
// phase of LSRA.  It is responsible for keeping the following in sync:
//   - varDsc->GetRegNum() (and GetOtherReg()) contain the unique register location.
//     If it is not in the same register through its lifetime, it is set to REG_STK.
//   - interval->physReg is set to the assigned register
//     (i.e. at the code location which is currently being handled by resolveRegisters())
//     - interval->isActive is true iff the interval is live and occupying a register
//     - interval->isSpilled should have already been set to true if the interval is EVER spilled
//     - interval->isSplit is set to true if the interval does not occupy the same
//       register throughout the method
//   - RegRecord->assignedInterval points to the interval which currently occupies
//     the register
//   - For each lclVar node:
//     - GetRegNum()/gtRegPair is set to the currently allocated register(s).
//     - GTF_SPILLED is set on a use if it must be reloaded prior to use.
//     - GTF_SPILL is set if it must be spilled after use.
//
// A copyReg is an ugly case where the variable must be in a specific (fixed) register,
// but it currently resides elsewhere.  The register allocator must track the use of the
// fixed register, but it marks the lclVar node with the register it currently lives in
// and the code generator does the necessary move.
//
// Before beginning, the varDsc for each parameter must be set to its initial location.
//
// NICE: Consider tracking whether an Interval is always in the same location (register/stack)
// in which case it will require no resolution.
//
void LinearScan::resolveLocalRef(BasicBlock* block, GenTreeLclVar* treeNode, RefPosition* currentRefPosition)
{
    assert((block == nullptr) == (treeNode == nullptr));
    assert(enregisterLocalVars);

    // Is this a tracked local?  Or just a register allocated for loading
    // a non-tracked one?
    Interval* interval = currentRefPosition->getInterval();
    assert(interval->isLocalVar);

    interval->recentRefPosition = currentRefPosition;
    LclVarDsc* varDsc           = interval->getLocalVar(compiler);

    // NOTE: we set the LastUse flag here unless we are extending lifetimes, in which case we write
    // this bit in checkLastUses. This is a bit of a hack, but is necessary because codegen requires
    // accurate last use info that is not reflected in the lastUse bit on ref positions when we are extending
    // lifetimes. See also the comments in checkLastUses.
    if ((treeNode != nullptr) && !extendLifetimes())
    {
        if (currentRefPosition->lastUse)
        {
            treeNode->SetLastUse(currentRefPosition->getMultiRegIdx());
        }
        else
        {
            treeNode->ClearLastUse(currentRefPosition->getMultiRegIdx());
        }

        if ((currentRefPosition->registerAssignment != RBM_NONE) && (interval->physReg == REG_NA) &&
            currentRefPosition->RegOptional() && currentRefPosition->lastUse &&
            (currentRefPosition->refType == RefTypeUse))
        {
            // This can happen if the incoming location for the block was changed from a register to the stack
            // during resolution. In this case we're better off making it contained.
            assert(inVarToRegMaps[curBBNum][varDsc->lvVarIndex] == REG_STK);
            currentRefPosition->registerAssignment = RBM_NONE;
            writeLocalReg(treeNode->AsLclVar(), interval->varNum, REG_NA);
        }
    }

    if (currentRefPosition->registerAssignment == RBM_NONE)
    {
        assert(currentRefPosition->RegOptional());
        assert(interval->isSpilled);

        varDsc->SetRegNum(REG_STK);
        if (interval->assignedReg != nullptr && interval->assignedReg->assignedInterval == interval)
        {
            updateAssignedInterval(interval->assignedReg, nullptr, interval->registerType);
        }
        interval->assignedReg = nullptr;
        interval->physReg     = REG_NA;
        interval->isActive    = false;

        // Set this as contained if it is not a multi-reg (we could potentially mark it s contained
        // if all uses are from spill, but that adds complexity.
        if ((currentRefPosition->refType == RefTypeUse) && !treeNode->IsMultiReg())
        {
            assert(treeNode != nullptr);
            treeNode->SetContained();
        }

        return;
    }

    // In most cases, assigned and home registers will be the same
    // The exception is the copyReg case, where we've assigned a register
    // for a specific purpose, but will be keeping the register assignment
    regNumber assignedReg = currentRefPosition->assignedReg();
    regNumber homeReg     = assignedReg;

    // Undo any previous association with a physical register, UNLESS this
    // is a copyReg
    if (!currentRefPosition->copyReg)
    {
        regNumber oldAssignedReg = interval->physReg;
        if (oldAssignedReg != REG_NA && assignedReg != oldAssignedReg)
        {
            RegRecord* oldRegRecord = getRegisterRecord(oldAssignedReg);
            if (oldRegRecord->assignedInterval == interval)
            {
                updateAssignedInterval(oldRegRecord, nullptr, interval->registerType);
            }
        }
    }

    if (currentRefPosition->refType == RefTypeUse && !currentRefPosition->reload)
    {
        // Was this spilled after our predecessor was scheduled?
        if (interval->physReg == REG_NA)
        {
            assert(inVarToRegMaps[curBBNum][varDsc->lvVarIndex] == REG_STK);
            currentRefPosition->reload = true;
        }
    }

    bool reload     = currentRefPosition->reload;
    bool spillAfter = currentRefPosition->spillAfter;
    bool writeThru  = currentRefPosition->writeThru;

    // In the reload case we either:
    // - Set the register to REG_STK if it will be referenced only from the home location, or
    // - Set the register to the assigned register and set GTF_SPILLED if it must be loaded into a register.
    if (reload)
    {
        assert(currentRefPosition->refType != RefTypeDef);
        assert(interval->isSpilled);
        varDsc->SetRegNum(REG_STK);
        if (!spillAfter)
        {
            interval->physReg = assignedReg;
        }

        // If there is no treeNode, this must be a RefTypeExpUse, in
        // which case we did the reload already
        if (treeNode != nullptr)
        {
            treeNode->gtFlags |= GTF_SPILLED;
            if (treeNode->IsMultiReg())
            {
                treeNode->SetRegSpillFlagByIdx(GTF_SPILLED, currentRefPosition->getMultiRegIdx());
            }
            if (spillAfter)
            {
                if (currentRefPosition->RegOptional())
                {
                    // This is a use of lclVar that is flagged as reg-optional
                    // by lower/codegen and marked for both reload and spillAfter.
                    // In this case we can avoid unnecessary reload and spill
                    // by setting reg on lclVar to REG_STK and reg on tree node
                    // to REG_NA.  Codegen will generate the code by considering
                    // it as a contained memory operand.
                    //
                    // Note that varDsc->GetRegNum() is already to REG_STK above.
                    interval->physReg = REG_NA;
                    writeLocalReg(treeNode->AsLclVar(), interval->varNum, REG_NA);
                    treeNode->gtFlags &= ~GTF_SPILLED;
                    treeNode->SetContained();
                    // We don't support RegOptional for multi-reg localvars.
                    assert(!treeNode->IsMultiReg());
                }
                else
                {
                    treeNode->gtFlags |= GTF_SPILL;
                    if (treeNode->IsMultiReg())
                    {
                        treeNode->SetRegSpillFlagByIdx(GTF_SPILL, currentRefPosition->getMultiRegIdx());
                    }
                }
            }
        }
        else
        {
            assert(currentRefPosition->refType == RefTypeExpUse);
        }
    }
    else if (spillAfter && !RefTypeIsUse(currentRefPosition->refType) &&
             (!treeNode->IsMultiReg() || treeNode->gtGetOp1()->IsMultiRegNode()))
    {
        // In the case of a pure def, don't bother spilling - just assign it to the
        // stack.  However, we need to remember that it was spilled.
        // We can't do this in the case of a multi-reg node with a non-multireg source as
        // we need the register to extract into.

        assert(interval->isSpilled);
        varDsc->SetRegNum(REG_STK);
        interval->physReg = REG_NA;
        if (treeNode != nullptr)
        {
            writeLocalReg(treeNode->AsLclVar(), interval->varNum, REG_NA);
        }
    }
    else // Not reload and Not pure-def that's spillAfter
    {
        if (currentRefPosition->copyReg || currentRefPosition->moveReg)
        {
            // For a copyReg or moveReg, we have two cases:
            //  - In the first case, we have a fixedReg - i.e. a register which the code
            //    generator is constrained to use.
            //    The code generator will generate the appropriate move to meet the requirement.
            //  - In the second case, we were forced to use a different register because of
            //    interference (or JitStressRegs).
            //    In this case, we generate a GT_COPY.
            // In either case, we annotate the treeNode with the register in which the value
            // currently lives.  For moveReg, the homeReg is the new register (as assigned above).
            // But for copyReg, the homeReg remains unchanged.

            assert(treeNode != nullptr);
            writeLocalReg(treeNode->AsLclVar(), interval->varNum, interval->physReg);

            if (currentRefPosition->copyReg)
            {
                homeReg = interval->physReg;
            }
            else
            {
                assert(interval->isSplit);
                interval->physReg = assignedReg;
            }

            if (!currentRefPosition->isFixedRegRef || currentRefPosition->moveReg)
            {
                // This is the second case, where we need to generate a copy
                insertCopyOrReload(block, treeNode, currentRefPosition->getMultiRegIdx(), currentRefPosition);
            }
        }
        else
        {
            interval->physReg = assignedReg;

            if (!interval->isSpilled && !interval->isSplit)
            {
                if (varDsc->GetRegNum() != REG_STK)
                {
                    // If the register assignments don't match, then this interval is split.
                    if (varDsc->GetRegNum() != assignedReg)
                    {
                        setIntervalAsSplit(interval);
                        varDsc->SetRegNum(REG_STK);
                    }
                }
                else
                {
                    varDsc->SetRegNum(assignedReg);
                }
            }
        }
        if (spillAfter)
        {
            if (treeNode != nullptr)
            {
                treeNode->gtFlags |= GTF_SPILL;
                if (treeNode->IsMultiReg())
                {
                    treeNode->SetRegSpillFlagByIdx(GTF_SPILL, currentRefPosition->getMultiRegIdx());
                }
            }
            assert(interval->isSpilled);
            interval->physReg = REG_NA;
            varDsc->SetRegNum(REG_STK);
        }
        if (writeThru && (treeNode != nullptr))
        {
            // This is a def of a write-thru EH var (only defs are marked 'writeThru').
            treeNode->gtFlags |= GTF_SPILL;
            // We also mark writeThru defs that are not last-use with GTF_SPILLED to indicate that they are conceptually
            // spilled and immediately "reloaded", i.e. the register remains live.
            // Note that we can have a "last use" write that has no exposed uses in the standard
            // (non-eh) control flow, but that may be used on an exception path. Hence the need
            // to retain these defs, and to ensure that they write.
            if (!currentRefPosition->lastUse)
            {
                treeNode->gtFlags |= GTF_SPILLED;
                if (treeNode->IsMultiReg())
                {
                    treeNode->SetRegSpillFlagByIdx(GTF_SPILLED, currentRefPosition->getMultiRegIdx());
                }
            }
        }
    }

    // Update the physRegRecord for the register, so that we know what vars are in
    // regs at the block boundaries
    RegRecord* physRegRecord = getRegisterRecord(homeReg);
    if (spillAfter || currentRefPosition->lastUse)
    {
        interval->isActive    = false;
        interval->assignedReg = nullptr;
        interval->physReg     = REG_NA;

        updateAssignedInterval(physRegRecord, nullptr, interval->registerType);
    }
    else
    {
        interval->isActive    = true;
        interval->assignedReg = physRegRecord;

        updateAssignedInterval(physRegRecord, interval, interval->registerType);
    }
}

void LinearScan::writeRegisters(RefPosition* currentRefPosition, GenTree* tree)
{
    lsraAssignRegToTree(tree, currentRefPosition->assignedReg(), currentRefPosition->getMultiRegIdx());
}

//------------------------------------------------------------------------
// insertCopyOrReload: Insert a copy in the case where a tree node value must be moved
//   to a different register at the point of use (GT_COPY), or it is reloaded to a different register
//   than the one it was spilled from (GT_RELOAD).
//
// Arguments:
//    block             - basic block in which GT_COPY/GT_RELOAD is inserted.
//    tree              - This is the node to copy or reload.
//                        Insert copy or reload node between this node and its parent.
//    multiRegIdx       - register position of tree node for which copy or reload is needed.
//    refPosition       - The RefPosition at which copy or reload will take place.
//
// Notes:
//    The GT_COPY or GT_RELOAD will be inserted in the proper spot in execution order where the reload is to occur.
//
// For example, for this tree (numbers are execution order, lower is earlier and higher is later):
//
//                                   +---------+----------+
//                                   |       GT_ADD (3)   |
//                                   +---------+----------+
//                                             |
//                                           /   '\'
//                                         /       '\'
//                                       /           '\'
//                   +-------------------+           +----------------------+
//                   |         x (1)     | "tree"    |         y (2)        |
//                   +-------------------+           +----------------------+
//
// generate this tree:
//
//                                   +---------+----------+
//                                   |       GT_ADD (4)   |
//                                   +---------+----------+
//                                             |
//                                           /   '\'
//                                         /       '\'
//                                       /           '\'
//                   +-------------------+           +----------------------+
//                   |  GT_RELOAD (3)    |           |         y (2)        |
//                   +-------------------+           +----------------------+
//                             |
//                   +-------------------+
//                   |         x (1)     | "tree"
//                   +-------------------+
//
// Note in particular that the GT_RELOAD node gets inserted in execution order immediately before the parent of "tree",
// which seems a bit weird since normally a node's parent (in this case, the parent of "x", GT_RELOAD in the "after"
// picture) immediately follows all of its children (that is, normally the execution ordering is postorder).
// The ordering must be this weird "out of normal order" way because the "x" node is being spilled, probably
// because the expression in the tree represented above by "y" has high register requirements. We don't want
// to reload immediately, of course. So we put GT_RELOAD where the reload should actually happen.
//
// Note that GT_RELOAD is required when we reload to a different register than the one we spilled to. It can also be
// used if we reload to the same register. Normally, though, in that case we just mark the node with GTF_SPILLED,
// and the unspilling code automatically reuses the same register, and does the reload when it notices that flag
// when considering a node's operands.
//
void LinearScan::insertCopyOrReload(BasicBlock* block, GenTree* tree, unsigned multiRegIdx, RefPosition* refPosition)
{
    LIR::Range& blockRange = LIR::AsRange(block);

    LIR::Use treeUse;
    bool     foundUse = blockRange.TryGetUse(tree, &treeUse);
    assert(foundUse);

    GenTree* parent = treeUse.User();

    genTreeOps oper;
    if (refPosition->reload)
    {
        oper = GT_RELOAD;
    }
    else
    {
        oper = GT_COPY;

        INTRACK_STATS(updateLsraStat(STAT_COPY_REG, block->bbNum));
    }

    // If the parent is a reload/copy node, then tree must be a multi-reg node
    // that has already had one of its registers spilled.
    // It is possible that one of its RefTypeDef positions got spilled and the next
    // use of it requires it to be in a different register.
    //
    // In this case set the i'th position reg of reload/copy node to the reg allocated
    // for copy/reload refPosition.  Essentially a copy/reload node will have a reg
    // for each multi-reg position of its child. If there is a valid reg in i'th
    // position of GT_COPY or GT_RELOAD node then the corresponding result of its
    // child needs to be copied or reloaded to that reg.
    if (parent->IsCopyOrReload())
    {
        noway_assert(parent->OperGet() == oper);
        noway_assert(tree->IsMultiRegNode());
        GenTreeCopyOrReload* copyOrReload = parent->AsCopyOrReload();
        noway_assert(copyOrReload->GetRegNumByIdx(multiRegIdx) == REG_NA);
        copyOrReload->SetRegNumByIdx(refPosition->assignedReg(), multiRegIdx);
    }
    else
    {
        // Create the new node, with "tree" as its only child.
        var_types treeType = tree->TypeGet();

        GenTreeCopyOrReload* newNode = new (compiler, oper) GenTreeCopyOrReload(oper, treeType, tree);
        assert(refPosition->registerAssignment != RBM_NONE);
        SetLsraAdded(newNode);
        newNode->SetRegNumByIdx(refPosition->assignedReg(), multiRegIdx);
        if (refPosition->copyReg)
        {
            // This is a TEMPORARY copy
            assert(isCandidateLocalRef(tree) || tree->IsMultiRegLclVar());
            newNode->SetLastUse(multiRegIdx);
        }

        // Insert the copy/reload after the spilled node and replace the use of the original node with a use
        // of the copy/reload.
        blockRange.InsertAfter(tree, newNode);
        treeUse.ReplaceWith(compiler, newNode);
    }
}

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
//------------------------------------------------------------------------
// insertUpperVectorSave: Insert code to save the upper half of a vector that lives
//                        in a callee-save register at the point of a kill (the upper half is
//                        not preserved).
//
// Arguments:
//    tree              - This is the node before which we will insert the Save.
//                        It will be a call or some node that turns into a call.
//    refPosition       - The RefTypeUpperVectorSave RefPosition.
//    upperInterval     - The Interval for the upper half of the large vector lclVar.
//    block             - the BasicBlock containing the call.
//
void LinearScan::insertUpperVectorSave(GenTree*     tree,
                                       RefPosition* refPosition,
                                       Interval*    upperVectorInterval,
                                       BasicBlock*  block)
{
    JITDUMP("Inserting UpperVectorSave for RP #%d before %d.%s:\n", refPosition->rpNum, tree->gtTreeID,
            GenTree::OpName(tree->gtOper));
    Interval* lclVarInterval = upperVectorInterval->relatedInterval;
    assert(lclVarInterval->isLocalVar == true);
    assert(refPosition->getInterval() == upperVectorInterval);
    regNumber lclVarReg = lclVarInterval->physReg;
    if (lclVarReg == REG_NA)
    {
        return;
    }

    LclVarDsc* varDsc = compiler->lvaTable + lclVarInterval->varNum;
    assert(Compiler::varTypeNeedsPartialCalleeSave(varDsc->lvType));

    // On Arm64, we must always have a register to save the upper half,
    // while on x86 we can spill directly to memory.
    regNumber spillReg = refPosition->assignedReg();
#ifdef TARGET_ARM64
    bool spillToMem = refPosition->spillAfter;
    assert(spillReg != REG_NA);
#else
    bool spillToMem = (spillReg == REG_NA);
    assert(!refPosition->spillAfter);
#endif

    LIR::Range& blockRange = LIR::AsRange(block);

    // Insert the save before the call.

    GenTree* saveLcl = compiler->gtNewLclvNode(lclVarInterval->varNum, varDsc->lvType);
    saveLcl->SetRegNum(lclVarReg);
    SetLsraAdded(saveLcl);

    GenTreeSIMD* simdNode =
        new (compiler, GT_SIMD) GenTreeSIMD(LargeVectorSaveType, saveLcl, nullptr, SIMDIntrinsicUpperSave,
                                            varDsc->GetSimdBaseJitType(), genTypeSize(varDsc->lvType));

    if (simdNode->GetSimdBaseJitType() == CORINFO_TYPE_UNDEF)
    {
        // There are a few scenarios where we can get a LCL_VAR which
        // doesn't know the underlying baseType. In that scenario, we
        // will just lie and say it is a float. Codegen doesn't actually
        // care what the type is but this avoids an assert that would
        // otherwise be fired from the more general checks that happen.
        simdNode->SetSimdBaseJitType(CORINFO_TYPE_FLOAT);
    }

    SetLsraAdded(simdNode);
    simdNode->SetRegNum(spillReg);
    if (spillToMem)
    {
        simdNode->gtFlags |= GTF_SPILL;
        upperVectorInterval->physReg = REG_NA;
    }
    else
    {
        assert((genRegMask(spillReg) & RBM_FLT_CALLEE_SAVED) != RBM_NONE);
        upperVectorInterval->physReg = spillReg;
    }

    blockRange.InsertBefore(tree, LIR::SeqTree(compiler, simdNode));
    DISPTREE(simdNode);
    JITDUMP("\n");
}

//------------------------------------------------------------------------
// insertUpperVectorRestore: Insert code to restore the upper half of a vector that has been partially spilled.
//
// Arguments:
//    tree                - This is the node for which we will insert the Restore.
//                          If non-null, it will be a use of the large vector lclVar.
//                          If null, the Restore will be added to the end of the block.
//    upperVectorInterval - The Interval for the upper vector for the lclVar.
//    block               - the BasicBlock into which we will be inserting the code.
//
// Notes:
//    In the case where 'tree' is non-null, we will insert the restore just prior to
//    its use, in order to ensure the proper ordering.
//
void LinearScan::insertUpperVectorRestore(GenTree*     tree,
                                          RefPosition* refPosition,
                                          Interval*    upperVectorInterval,
                                          BasicBlock*  block)
{
    JITDUMP("Adding UpperVectorRestore for RP #%d ", refPosition->rpNum);
    Interval* lclVarInterval = upperVectorInterval->relatedInterval;
    assert(lclVarInterval->isLocalVar == true);
    regNumber lclVarReg = lclVarInterval->physReg;

    // We should not call this method if the lclVar is not in a register (we should have simply marked the entire
    // lclVar as spilled).
    assert(lclVarReg != REG_NA);
    LclVarDsc* varDsc = compiler->lvaTable + lclVarInterval->varNum;
    assert(Compiler::varTypeNeedsPartialCalleeSave(varDsc->lvType));

    GenTree* restoreLcl = nullptr;
    restoreLcl          = compiler->gtNewLclvNode(lclVarInterval->varNum, varDsc->lvType);
    restoreLcl->SetRegNum(lclVarReg);
    SetLsraAdded(restoreLcl);

    GenTreeSIMD* simdNode =
        new (compiler, GT_SIMD) GenTreeSIMD(varDsc->lvType, restoreLcl, nullptr, SIMDIntrinsicUpperRestore,
                                            varDsc->GetSimdBaseJitType(), genTypeSize(varDsc->lvType));

    if (simdNode->GetSimdBaseJitType() == CORINFO_TYPE_UNDEF)
    {
        // There are a few scenarios where we can get a LCL_VAR which
        // doesn't know the underlying baseType. In that scenario, we
        // will just lie and say it is a float. Codegen doesn't actually
        // care what the type is but this avoids an assert that would
        // otherwise be fired from the more general checks that happen.
        simdNode->SetSimdBaseJitType(CORINFO_TYPE_FLOAT);
    }

    regNumber restoreReg = upperVectorInterval->physReg;
    SetLsraAdded(simdNode);

    if (restoreReg == REG_NA)
    {
        // We need a stack location for this.
        assert(lclVarInterval->isSpilled);
#ifdef TARGET_AMD64
        assert(refPosition->assignedReg() == REG_NA);
        simdNode->gtFlags |= GTF_NOREG_AT_USE;
#else
        simdNode->gtFlags |= GTF_SPILLED;
        assert(refPosition->assignedReg() != REG_NA);
        restoreReg = refPosition->assignedReg();
#endif
    }
    simdNode->SetRegNum(restoreReg);

    LIR::Range& blockRange = LIR::AsRange(block);
    JITDUMP("Adding UpperVectorRestore ");
    if (tree != nullptr)
    {
        JITDUMP("before %d.%s:\n", tree->gtTreeID, GenTree::OpName(tree->gtOper));
        LIR::Use treeUse;
        bool     foundUse = blockRange.TryGetUse(tree, &treeUse);
        assert(foundUse);
        // We need to insert the restore prior to the use, not (necessarily) immediately after the lclVar.
        blockRange.InsertBefore(treeUse.User(), LIR::SeqTree(compiler, simdNode));
    }
    else
    {
        JITDUMP("at end of " FMT_BB ":\n", block->bbNum);
        if (block->bbJumpKind == BBJ_COND || block->bbJumpKind == BBJ_SWITCH)
        {
            noway_assert(!blockRange.IsEmpty());

            GenTree* branch = blockRange.LastNode();
            assert(branch->OperIsConditionalJump() || branch->OperGet() == GT_SWITCH_TABLE ||
                   branch->OperGet() == GT_SWITCH);

            blockRange.InsertBefore(branch, LIR::SeqTree(compiler, simdNode));
        }
        else
        {
            assert(block->bbJumpKind == BBJ_NONE || block->bbJumpKind == BBJ_ALWAYS);
            blockRange.InsertAtEnd(LIR::SeqTree(compiler, simdNode));
        }
    }
    DISPTREE(simdNode);
    JITDUMP("\n");
}
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

//------------------------------------------------------------------------
// initMaxSpill: Initializes the LinearScan members used to track the max number
//               of concurrent spills.  This is needed so that we can set the
//               fields in Compiler, so that the code generator, in turn can
//               allocate the right number of spill locations.
//
// Arguments:
//    None.
//
// Return Value:
//    None.
//
// Assumptions:
//    This is called before any calls to updateMaxSpill().

void LinearScan::initMaxSpill()
{
    needDoubleTmpForFPCall = false;
    needFloatTmpForFPCall  = false;
    for (int i = 0; i < TYP_COUNT; i++)
    {
        maxSpill[i]     = 0;
        currentSpill[i] = 0;
    }
}

//------------------------------------------------------------------------
// recordMaxSpill: Sets the fields in Compiler for the max number of concurrent spills.
//                 (See the comment on initMaxSpill.)
//
// Arguments:
//    None.
//
// Return Value:
//    None.
//
// Assumptions:
//    This is called after updateMaxSpill() has been called for all "real"
//    RefPositions.

void LinearScan::recordMaxSpill()
{
    // Note: due to the temp normalization process (see tmpNormalizeType)
    // only a few types should actually be seen here.
    JITDUMP("Recording the maximum number of concurrent spills:\n");
#ifdef TARGET_X86
    var_types returnType = RegSet::tmpNormalizeType(compiler->info.compRetType);
    if (needDoubleTmpForFPCall || (returnType == TYP_DOUBLE))
    {
        JITDUMP("Adding a spill temp for moving a double call/return value between xmm reg and x87 stack.\n");
        maxSpill[TYP_DOUBLE] += 1;
    }
    if (needFloatTmpForFPCall || (returnType == TYP_FLOAT))
    {
        JITDUMP("Adding a spill temp for moving a float call/return value between xmm reg and x87 stack.\n");
        maxSpill[TYP_FLOAT] += 1;
    }
#endif // TARGET_X86
    for (int i = 0; i < TYP_COUNT; i++)
    {
        if (var_types(i) != RegSet::tmpNormalizeType(var_types(i)))
        {
            // Only normalized types should have anything in the maxSpill array.
            // We assume here that if type 'i' does not normalize to itself, then
            // nothing else normalizes to 'i', either.
            assert(maxSpill[i] == 0);
        }
        if (maxSpill[i] != 0)
        {
            JITDUMP("  %s: %d\n", varTypeName(var_types(i)), maxSpill[i]);
            compiler->codeGen->regSet.tmpPreAllocateTemps(var_types(i), maxSpill[i]);
        }
    }
    JITDUMP("\n");
}

//------------------------------------------------------------------------
// updateMaxSpill: Update the maximum number of concurrent spills
//
// Arguments:
//    refPosition - the current RefPosition being handled
//
// Return Value:
//    None.
//
// Assumptions:
//    The RefPosition has an associated interval (getInterval() will
//    otherwise assert).
//
// Notes:
//    This is called for each "real" RefPosition during the writeback
//    phase of LSRA.  It keeps track of how many concurrently-live
//    spills there are, and the largest number seen so far.

void LinearScan::updateMaxSpill(RefPosition* refPosition)
{
    RefType refType = refPosition->refType;

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    if ((refType == RefTypeUpperVectorSave) || (refType == RefTypeUpperVectorRestore))
    {
        Interval* interval = refPosition->getInterval();
        // If this is not an 'upperVector', it must be a tree temp that has been already
        // (fully) spilled.
        if (!interval->isUpperVector)
        {
            assert(interval->firstRefPosition->spillAfter);
        }
        else
        {
            // The UpperVector RefPositions spill to the localVar's home location.
            Interval* lclVarInterval = interval->relatedInterval;
            assert(lclVarInterval->isSpilled || (!refPosition->spillAfter && !refPosition->reload));
        }
        return;
    }
#endif // !FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    if (refPosition->spillAfter || refPosition->reload ||
        (refPosition->RegOptional() && refPosition->assignedReg() == REG_NA))
    {
        Interval* interval = refPosition->getInterval();
        if (!interval->isLocalVar)
        {
            GenTree* treeNode = refPosition->treeNode;
            if (treeNode == nullptr)
            {
                assert(RefTypeIsUse(refType));
                treeNode = interval->firstRefPosition->treeNode;
            }
            assert(treeNode != nullptr);

            // The tmp allocation logic 'normalizes' types to a small number of
            // types that need distinct stack locations from each other.
            // Those types are currently gc refs, byrefs, <= 4 byte non-GC items,
            // 8-byte non-GC items, and 16-byte or 32-byte SIMD vectors.
            // LSRA is agnostic to those choices but needs
            // to know what they are here.
            var_types type;
            if (!treeNode->IsMultiRegNode())
            {
                type = getDefType(treeNode);
            }
            else
            {
                type = treeNode->GetRegTypeByIndex(refPosition->getMultiRegIdx());
            }

            type = RegSet::tmpNormalizeType(type);

            if (refPosition->spillAfter && !refPosition->reload)
            {
                currentSpill[type]++;
                if (currentSpill[type] > maxSpill[type])
                {
                    maxSpill[type] = currentSpill[type];
                }
            }
            else if (refPosition->reload)
            {
                assert(currentSpill[type] > 0);
                currentSpill[type]--;
            }
            else if (refPosition->RegOptional() && refPosition->assignedReg() == REG_NA)
            {
                // A spill temp not getting reloaded into a reg because it is
                // marked as allocate if profitable and getting used from its
                // memory location.  To properly account max spill for typ we
                // decrement spill count.
                assert(RefTypeIsUse(refType));
                assert(currentSpill[type] > 0);
                currentSpill[type]--;
            }
            JITDUMP("  Max spill for %s is %d\n", varTypeName(type), maxSpill[type]);
        }
    }
}

// This is the final phase of register allocation.  It writes the register assignments to
// the tree, and performs resolution across joins and backedges.
//
void LinearScan::resolveRegisters()
{
    // Iterate over the tree and the RefPositions in lockstep
    //  - annotate the tree with register assignments by setting GetRegNum() or gtRegPair (for longs)
    //    on the tree node
    //  - track globally-live var locations
    //  - add resolution points at split/merge/critical points as needed

    // Need to use the same traversal order as the one that assigns the location numbers.

    // Dummy RefPositions have been added at any split, join or critical edge, at the
    // point where resolution may be required.  These are located:
    //  - for a split, at the top of the non-adjacent block
    //  - for a join, at the bottom of the non-adjacent joining block
    //  - for a critical edge, at the top of the target block of each critical
    //    edge.
    // Note that a target block may have multiple incoming critical or split edges
    //
    // These RefPositions record the expected location of the Interval at that point.
    // At each branch, we identify the location of each liveOut interval, and check
    // against the RefPositions at the target.

    BasicBlock*  block;
    LsraLocation currentLocation = MinLocation;

    // Clear register assignments - these will be reestablished as lclVar defs (including RefTypeParamDefs)
    // are encountered.
    if (enregisterLocalVars)
    {
        for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
        {
            RegRecord* physRegRecord    = getRegisterRecord(reg);
            Interval*  assignedInterval = physRegRecord->assignedInterval;
            if (assignedInterval != nullptr)
            {
                assignedInterval->assignedReg = nullptr;
                assignedInterval->physReg     = REG_NA;
            }
            physRegRecord->assignedInterval  = nullptr;
            physRegRecord->recentRefPosition = nullptr;
        }

        // Clear "recentRefPosition" for lclVar intervals
        for (unsigned varIndex = 0; varIndex < compiler->lvaTrackedCount; varIndex++)
        {
            if (localVarIntervals[varIndex] != nullptr)
            {
                localVarIntervals[varIndex]->recentRefPosition = nullptr;
                localVarIntervals[varIndex]->isActive          = false;
            }
            else
            {
                assert(!compiler->lvaGetDescByTrackedIndex(varIndex)->lvLRACandidate);
            }
        }
    }

    // handle incoming arguments and special temps
    RefPositionIterator refPosIterator     = refPositions.begin();
    RefPosition*        currentRefPosition = &refPosIterator;

    if (enregisterLocalVars)
    {
        VarToRegMap entryVarToRegMap = inVarToRegMaps[compiler->fgFirstBB->bbNum];
        for (; refPosIterator != refPositions.end() &&
               (currentRefPosition->refType == RefTypeParamDef || currentRefPosition->refType == RefTypeZeroInit);
             ++refPosIterator, currentRefPosition = &refPosIterator)
        {
            Interval* interval = currentRefPosition->getInterval();
            assert(interval != nullptr && interval->isLocalVar);
            resolveLocalRef(nullptr, nullptr, currentRefPosition);
            regNumber reg      = REG_STK;
            int       varIndex = interval->getVarIndex(compiler);

            if (!currentRefPosition->spillAfter && currentRefPosition->registerAssignment != RBM_NONE)
            {
                reg = currentRefPosition->assignedReg();
            }
            else
            {
                reg                = REG_STK;
                interval->isActive = false;
            }
            setVarReg(entryVarToRegMap, varIndex, reg);
        }
    }
    else
    {
        assert(refPosIterator == refPositions.end() ||
               (refPosIterator->refType != RefTypeParamDef && refPosIterator->refType != RefTypeZeroInit));
    }

    // write back assignments
    for (block = startBlockSequence(); block != nullptr; block = moveToNextBlock())
    {
        assert(curBBNum == block->bbNum);

        if (enregisterLocalVars)
        {
            // Record the var locations at the start of this block.
            // (If it's fgFirstBB, we've already done that above, see entryVarToRegMap)

            curBBStartLocation = currentRefPosition->nodeLocation;
            if (block != compiler->fgFirstBB)
            {
                processBlockStartLocations(block);
            }

            // Handle the DummyDefs, updating the incoming var location.
            for (; refPosIterator != refPositions.end() && currentRefPosition->refType == RefTypeDummyDef;
                 ++refPosIterator, currentRefPosition = &refPosIterator)
            {
                assert(currentRefPosition->isIntervalRef());
                // Don't mark dummy defs as reload
                currentRefPosition->reload = false;
                resolveLocalRef(nullptr, nullptr, currentRefPosition);
                regNumber reg;
                if (currentRefPosition->registerAssignment != RBM_NONE)
                {
                    reg = currentRefPosition->assignedReg();
                }
                else
                {
                    reg                                         = REG_STK;
                    currentRefPosition->getInterval()->isActive = false;
                }
                setInVarRegForBB(curBBNum, currentRefPosition->getInterval()->varNum, reg);
            }
        }

        // The next RefPosition should be for the block.  Move past it.
        assert(refPosIterator != refPositions.end());
        assert(currentRefPosition->refType == RefTypeBB);
        ++refPosIterator;
        currentRefPosition = &refPosIterator;

        // Handle the RefPositions for the block
        for (; refPosIterator != refPositions.end() && currentRefPosition->refType != RefTypeBB &&
               currentRefPosition->refType != RefTypeDummyDef;
             ++refPosIterator, currentRefPosition = &refPosIterator)
        {
            currentLocation = currentRefPosition->nodeLocation;

            // Ensure that the spill & copy info is valid.
            // First, if it's reload, it must not be copyReg or moveReg
            assert(!currentRefPosition->reload || (!currentRefPosition->copyReg && !currentRefPosition->moveReg));
            // If it's copyReg it must not be moveReg, and vice-versa
            assert(!currentRefPosition->copyReg || !currentRefPosition->moveReg);

            switch (currentRefPosition->refType)
            {
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                case RefTypeUpperVectorSave:
                case RefTypeUpperVectorRestore:
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                case RefTypeUse:
                case RefTypeDef:
                    // These are the ones we're interested in
                    break;
                case RefTypeKill:
                case RefTypeFixedReg:
                    // These require no handling at resolution time
                    assert(currentRefPosition->referent != nullptr);
                    currentRefPosition->referent->recentRefPosition = currentRefPosition;
                    continue;
                case RefTypeExpUse:
                    // Ignore the ExpUse cases - a RefTypeExpUse would only exist if the
                    // variable is dead at the entry to the next block.  So we'll mark
                    // it as in its current location and resolution will take care of any
                    // mismatch.
                    assert(getNextBlock() == nullptr ||
                           !VarSetOps::IsMember(compiler, getNextBlock()->bbLiveIn,
                                                currentRefPosition->getInterval()->getVarIndex(compiler)));
                    currentRefPosition->referent->recentRefPosition = currentRefPosition;
                    continue;
                case RefTypeKillGCRefs:
                    // No action to take at resolution time, and no interval to update recentRefPosition for.
                    continue;
                case RefTypeDummyDef:
                case RefTypeParamDef:
                case RefTypeZeroInit:
                // Should have handled all of these already
                default:
                    unreached();
                    break;
            }
            updateMaxSpill(currentRefPosition);
            GenTree* treeNode = currentRefPosition->treeNode;

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            if (currentRefPosition->refType == RefTypeUpperVectorSave)
            {
                // The treeNode is a call or something that might become one.
                noway_assert(treeNode != nullptr);
                // If the associated interval is an UpperVector, this must be a RefPosition for a LargeVectorType
                // LocalVar.
                // Otherwise, this  is a non-lclVar interval that has been spilled, and we don't need to do anything.
                Interval* interval = currentRefPosition->getInterval();
                if (interval->isUpperVector)
                {
                    Interval* localVarInterval = interval->relatedInterval;
                    if ((localVarInterval->physReg != REG_NA) && !localVarInterval->isPartiallySpilled)
                    {
                        // If the localVar is in a register, it must be in a register that is not trashed by
                        // the current node (otherwise it would have already been spilled).
                        assert((genRegMask(localVarInterval->physReg) & getKillSetForNode(treeNode)) == RBM_NONE);
                        // If we have allocated a register to spill it to, we will use that; otherwise, we will spill it
                        // to the stack.  We can use as a temp register any non-arg caller-save register.
                        currentRefPosition->referent->recentRefPosition = currentRefPosition;
                        insertUpperVectorSave(treeNode, currentRefPosition, currentRefPosition->getInterval(), block);
                        localVarInterval->isPartiallySpilled = true;
                    }
                }
                else
                {
                    // This is a non-lclVar interval that must have been spilled.
                    assert(!currentRefPosition->getInterval()->isLocalVar);
                    assert(currentRefPosition->getInterval()->firstRefPosition->spillAfter);
                }
                continue;
            }
            else if (currentRefPosition->refType == RefTypeUpperVectorRestore)
            {
                // Since we don't do partial restores of tree temp intervals, this must be an upperVector.
                Interval* interval         = currentRefPosition->getInterval();
                Interval* localVarInterval = interval->relatedInterval;
                assert(interval->isUpperVector && (localVarInterval != nullptr));
                if (localVarInterval->physReg != REG_NA)
                {
                    assert(localVarInterval->isPartiallySpilled);
                    assert((localVarInterval->assignedReg != nullptr) &&
                           (localVarInterval->assignedReg->regNum == localVarInterval->physReg) &&
                           (localVarInterval->assignedReg->assignedInterval == localVarInterval));
                    insertUpperVectorRestore(treeNode, currentRefPosition, interval, block);
                }
                localVarInterval->isPartiallySpilled = false;
            }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

            // Most uses won't actually need to be recorded (they're on the def).
            // In those cases, treeNode will be nullptr.
            if (treeNode == nullptr)
            {
                // This is either a use, a dead def, or a field of a struct
                Interval* interval = currentRefPosition->getInterval();
                assert(currentRefPosition->refType == RefTypeUse ||
                       currentRefPosition->registerAssignment == RBM_NONE || interval->isStructField ||
                       interval->IsUpperVector());

                // TODO-Review: Need to handle the case where any of the struct fields
                // are reloaded/spilled at this use
                assert(!interval->isStructField ||
                       (currentRefPosition->reload == false && currentRefPosition->spillAfter == false));

                if (interval->isLocalVar && !interval->isStructField)
                {
                    LclVarDsc* varDsc = interval->getLocalVar(compiler);

                    // This must be a dead definition.  We need to mark the lclVar
                    // so that it's not considered a candidate for lvRegister, as
                    // this dead def will have to go to the stack.
                    assert(currentRefPosition->refType == RefTypeDef);
                    varDsc->SetRegNum(REG_STK);
                }
                continue;
            }

            assert(currentRefPosition->isIntervalRef());
            if (currentRefPosition->getInterval()->isInternal)
            {
                treeNode->gtRsvdRegs |= currentRefPosition->registerAssignment;
            }
            else
            {
                writeRegisters(currentRefPosition, treeNode);

                if (treeNode->OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR) && currentRefPosition->getInterval()->isLocalVar)
                {
                    resolveLocalRef(block, treeNode->AsLclVar(), currentRefPosition);
                }

                // Mark spill locations on temps
                // (local vars are handled in resolveLocalRef, above)
                // Note that the tree node will be changed from GTF_SPILL to GTF_SPILLED
                // in codegen, taking care of the "reload" case for temps
                else if (currentRefPosition->spillAfter || (currentRefPosition->nextRefPosition != nullptr &&
                                                            currentRefPosition->nextRefPosition->moveReg))
                {
                    if (treeNode != nullptr)
                    {
                        if (currentRefPosition->spillAfter)
                        {
                            treeNode->gtFlags |= GTF_SPILL;

                            // If this is a constant interval that is reusing a pre-existing value, we actually need
                            // to generate the value at this point in order to spill it.
                            if (treeNode->IsReuseRegVal())
                            {
                                treeNode->ResetReuseRegVal();
                            }

                            // In case of multi-reg node, also set spill flag on the
                            // register specified by multi-reg index of current RefPosition.
                            // Note that the spill flag on treeNode indicates that one or
                            // more its allocated registers are in that state.
                            if (treeNode->IsMultiRegCall())
                            {
                                GenTreeCall* call = treeNode->AsCall();
                                call->SetRegSpillFlagByIdx(GTF_SPILL, currentRefPosition->getMultiRegIdx());
                            }
#if FEATURE_ARG_SPLIT
                            else if (treeNode->OperIsPutArgSplit())
                            {
                                GenTreePutArgSplit* splitArg = treeNode->AsPutArgSplit();
                                splitArg->SetRegSpillFlagByIdx(GTF_SPILL, currentRefPosition->getMultiRegIdx());
                            }
#ifdef TARGET_ARM
                            else if (treeNode->OperIsMultiRegOp())
                            {
                                GenTreeMultiRegOp* multiReg = treeNode->AsMultiRegOp();
                                multiReg->SetRegSpillFlagByIdx(GTF_SPILL, currentRefPosition->getMultiRegIdx());
                            }
#endif // TARGET_ARM
#endif // FEATURE_ARG_SPLIT
                        }

                        // If the value is reloaded or moved to a different register, we need to insert
                        // a node to hold the register to which it should be reloaded
                        RefPosition* nextRefPosition = currentRefPosition->nextRefPosition;
                        noway_assert(nextRefPosition != nullptr);
                        if (INDEBUG(alwaysInsertReload() ||)
                                nextRefPosition->assignedReg() != currentRefPosition->assignedReg())
                        {
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                            // Note that we asserted above that this is an Interval RefPosition.
                            Interval* currentInterval = currentRefPosition->getInterval();
                            if (!currentInterval->isUpperVector && nextRefPosition->refType == RefTypeUpperVectorSave)
                            {
                                // The currentRefPosition is a spill of a tree temp.
                                // These have no associated Restore, as we always spill if the vector is
                                // in a register when this is encountered.
                                // The nextRefPosition we're interested in (where we may need to insert a
                                // reload or flag as GTF_NOREG_AT_USE) is the subsequent RefPosition.
                                assert(!currentInterval->isLocalVar);
                                nextRefPosition = nextRefPosition->nextRefPosition;
                                assert(nextRefPosition->refType != RefTypeUpperVectorSave);
                            }
                            // UpperVector intervals may have unique assignments at each reference.
                            if (!currentInterval->isUpperVector)
#endif
                            {
                                if (nextRefPosition->assignedReg() != REG_NA)
                                {
                                    insertCopyOrReload(block, treeNode, currentRefPosition->getMultiRegIdx(),
                                                       nextRefPosition);
                                }
                                else
                                {
                                    assert(nextRefPosition->RegOptional());

                                    // In case of tree temps, if def is spilled and use didn't
                                    // get a register, set a flag on tree node to be treated as
                                    // contained at the point of its use.
                                    if (currentRefPosition->spillAfter && currentRefPosition->refType == RefTypeDef &&
                                        nextRefPosition->refType == RefTypeUse)
                                    {
                                        assert(nextRefPosition->treeNode == nullptr);
                                        treeNode->gtFlags |= GTF_NOREG_AT_USE;
                                    }
                                }
                            }
                        }
                    }

                    // We should never have to "spill after" a temp use, since
                    // they're single use
                    else
                    {
                        unreached();
                    }
                }
            }
        }

        if (enregisterLocalVars)
        {
            processBlockEndLocations(block);
        }
    }

    if (enregisterLocalVars)
    {
#ifdef DEBUG
        if (VERBOSE)
        {
            printf("-----------------------\n");
            printf("RESOLVING BB BOUNDARIES\n");
            printf("-----------------------\n");

            printf("Resolution Candidates: ");
            dumpConvertedVarSet(compiler, resolutionCandidateVars);
            printf("\n");
            printf("Has %sCritical Edges\n\n", hasCriticalEdges ? "" : "No ");

            printf("Prior to Resolution\n");
            foreach_block(compiler, block)
            {
                printf("\n" FMT_BB, block->bbNum);
                if (block->hasEHBoundaryIn())
                {
                    JITDUMP("  EH flow in");
                }
                if (block->hasEHBoundaryOut())
                {
                    JITDUMP("  EH flow out");
                }

                printf("\nuse def in out\n");
                dumpConvertedVarSet(compiler, block->bbVarUse);
                printf("\n");
                dumpConvertedVarSet(compiler, block->bbVarDef);
                printf("\n");
                dumpConvertedVarSet(compiler, block->bbLiveIn);
                printf("\n");
                dumpConvertedVarSet(compiler, block->bbLiveOut);
                printf("\n");

                dumpInVarToRegMap(block);
                dumpOutVarToRegMap(block);
            }

            printf("\n\n");
        }
#endif // DEBUG

        resolveEdges();

        // Verify register assignments on variables
        unsigned   lclNum;
        LclVarDsc* varDsc;
        for (lclNum = 0, varDsc = compiler->lvaTable; lclNum < compiler->lvaCount; lclNum++, varDsc++)
        {
            if (!isCandidateVar(varDsc))
            {
                varDsc->SetRegNum(REG_STK);
            }
            else
            {
                Interval* interval = getIntervalForLocalVar(varDsc->lvVarIndex);

                // Determine initial position for parameters

                if (varDsc->lvIsParam)
                {
                    regMaskTP initialRegMask = interval->firstRefPosition->registerAssignment;
                    regNumber initialReg     = (initialRegMask == RBM_NONE || interval->firstRefPosition->spillAfter)
                                               ? REG_STK
                                               : genRegNumFromMask(initialRegMask);

#ifdef TARGET_ARM
                    if (varTypeIsMultiReg(varDsc))
                    {
                        // TODO-ARM-NYI: Map the hi/lo intervals back to lvRegNum and GetOtherReg() (these should NYI
                        // before this)
                        assert(!"Multi-reg types not yet supported");
                    }
                    else
#endif // TARGET_ARM
                    {
                        varDsc->SetArgInitReg(initialReg);
                        JITDUMP("  Set V%02u argument initial register to %s\n", lclNum, getRegName(initialReg));
                    }

                    // Stack args that are part of dependently-promoted structs should never be register candidates (see
                    // LinearScan::isRegCandidate).
                    assert(varDsc->lvIsRegArg || !compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc));
                }

                // If lvRegNum is REG_STK, that means that either no register
                // was assigned, or (more likely) that the same register was not
                // used for all references.  In that case, codegen gets the register
                // from the tree node.
                if (varDsc->GetRegNum() == REG_STK || interval->isSpilled || interval->isSplit)
                {
                    // For codegen purposes, we'll set lvRegNum to whatever register
                    // it's currently in as we go.
                    // However, we never mark an interval as lvRegister if it has either been spilled
                    // or split.
                    varDsc->lvRegister = false;

                    // Skip any dead defs or exposed uses
                    // (first use exposed will only occur when there is no explicit initialization)
                    RefPosition* firstRefPosition = interval->firstRefPosition;
                    while ((firstRefPosition != nullptr) && (firstRefPosition->refType == RefTypeExpUse))
                    {
                        firstRefPosition = firstRefPosition->nextRefPosition;
                    }
                    if (firstRefPosition == nullptr)
                    {
                        // Dead interval
                        varDsc->lvLRACandidate = false;
                        if (varDsc->lvRefCnt() == 0)
                        {
                            varDsc->lvOnFrame = false;
                        }
                        else
                        {
                            // We may encounter cases where a lclVar actually has no references, but
                            // a non-zero refCnt.  For safety (in case this is some "hidden" lclVar that we're
                            // not correctly recognizing), we'll mark those as needing a stack location.
                            // TODO-Cleanup: Make this an assert if/when we correct the refCnt
                            // updating.
                            varDsc->lvOnFrame = true;
                        }
                    }
                    else
                    {
                        // If the interval was not spilled, it doesn't need a stack location.
                        if (!interval->isSpilled)
                        {
                            varDsc->lvOnFrame = false;
                        }
                        if (firstRefPosition->registerAssignment == RBM_NONE || firstRefPosition->spillAfter)
                        {
                            // Either this RefPosition is spilled, or regOptional or it is not a "real" def or use
                            assert(
                                firstRefPosition->spillAfter || firstRefPosition->RegOptional() ||
                                (firstRefPosition->refType != RefTypeDef && firstRefPosition->refType != RefTypeUse));
                            varDsc->SetRegNum(REG_STK);
                        }
                        else
                        {
                            varDsc->SetRegNum(firstRefPosition->assignedReg());
                        }
                    }
                }
                else
                {
                    {
                        varDsc->lvRegister = true;
                        varDsc->lvOnFrame  = false;
                    }
#ifdef DEBUG
                    regMaskTP registerAssignment = genRegMask(varDsc->GetRegNum());
                    assert(!interval->isSpilled && !interval->isSplit);
                    RefPosition* refPosition = interval->firstRefPosition;
                    assert(refPosition != nullptr);

                    while (refPosition != nullptr)
                    {
                        // All RefPositions must match, except for dead definitions,
                        // copyReg/moveReg and RefTypeExpUse positions
                        if (refPosition->registerAssignment != RBM_NONE && !refPosition->copyReg &&
                            !refPosition->moveReg && refPosition->refType != RefTypeExpUse)
                        {
                            assert(refPosition->registerAssignment == registerAssignment);
                        }
                        refPosition = refPosition->nextRefPosition;
                    }
#endif // DEBUG
                }
            }
        }
    }

#ifdef DEBUG
    if (VERBOSE)
    {
        printf("Trees after linear scan register allocator (LSRA)\n");
        compiler->fgDispBasicBlocks(true);
    }

    verifyFinalAllocation();
#endif // DEBUG

    compiler->raMarkStkVars();
    recordMaxSpill();

    // TODO-CQ: Review this comment and address as needed.
    // Change all unused promoted non-argument struct locals to a non-GC type (in this case TYP_INT)
    // so that the gc tracking logic and lvMustInit logic will ignore them.
    // Extract the code that does this from raAssignVars, and call it here.
    // PRECONDITIONS: Ensure that lvPromoted is set on promoted structs, if and
    // only if it is promoted on all paths.
    // Call might be something like:
    // compiler->BashUnusedStructLocals();
}

//
//------------------------------------------------------------------------
// insertMove: Insert a move of a lclVar with the given lclNum into the given block.
//
// Arguments:
//    block          - the BasicBlock into which the move will be inserted.
//    insertionPoint - the instruction before which to insert the move
//    lclNum         - the lclNum of the var to be moved
//    fromReg        - the register from which the var is moving
//    toReg          - the register to which the var is moving
//
// Return Value:
//    None.
//
// Notes:
//    If insertionPoint is non-NULL, insert before that instruction;
//    otherwise, insert "near" the end (prior to the branch, if any).
//    If fromReg or toReg is REG_STK, then move from/to memory, respectively.

void LinearScan::insertMove(
    BasicBlock* block, GenTree* insertionPoint, unsigned lclNum, regNumber fromReg, regNumber toReg)
{
    LclVarDsc* varDsc = compiler->lvaTable + lclNum;
    // the lclVar must be a register candidate
    assert(isRegCandidate(varDsc));
    // One or both MUST be a register
    assert(fromReg != REG_STK || toReg != REG_STK);
    // They must not be the same register.
    assert(fromReg != toReg);

    // This var can't be marked lvRegister now
    varDsc->SetRegNum(REG_STK);

    GenTree* src = compiler->gtNewLclvNode(lclNum, varDsc->TypeGet());
    SetLsraAdded(src);

    // There are three cases we need to handle:
    // - We are loading a lclVar from the stack.
    // - We are storing a lclVar to the stack.
    // - We are copying a lclVar between registers.
    //
    // In the first and second cases, the lclVar node will be marked with GTF_SPILLED and GTF_SPILL, respectively.
    // It is up to the code generator to ensure that any necessary normalization is done when loading or storing the
    // lclVar's value.
    //
    // In the third case, we generate GT_COPY(GT_LCL_VAR) and type each node with the normalized type of the lclVar.
    // This is safe because a lclVar is always normalized once it is in a register.

    GenTree* dst = src;
    if (fromReg == REG_STK)
    {
        src->gtFlags |= GTF_SPILLED;
        src->SetRegNum(toReg);
    }
    else if (toReg == REG_STK)
    {
        src->gtFlags |= GTF_SPILL;
        src->SetRegNum(fromReg);
    }
    else
    {
        var_types movType = varDsc->GetRegisterType();
        src->gtType       = movType;

        dst = new (compiler, GT_COPY) GenTreeCopyOrReload(GT_COPY, movType, src);
        // This is the new home of the lclVar - indicate that by clearing the GTF_VAR_DEATH flag.
        // Note that if src is itself a lastUse, this will have no effect.
        dst->gtFlags &= ~(GTF_VAR_DEATH);
        src->SetRegNum(fromReg);
        dst->SetRegNum(toReg);
        SetLsraAdded(dst);
    }
    dst->SetUnusedValue();

    LIR::Range  treeRange  = LIR::SeqTree(compiler, dst);
    LIR::Range& blockRange = LIR::AsRange(block);

    if (insertionPoint != nullptr)
    {
        blockRange.InsertBefore(insertionPoint, std::move(treeRange));
    }
    else
    {
        // Put the copy at the bottom
        GenTree* lastNode = blockRange.LastNode();
        if (block->bbJumpKind == BBJ_COND || block->bbJumpKind == BBJ_SWITCH)
        {
            noway_assert(!blockRange.IsEmpty());

            GenTree* branch = lastNode;
            assert(branch->OperIsConditionalJump() || branch->OperGet() == GT_SWITCH_TABLE ||
                   branch->OperGet() == GT_SWITCH);

            blockRange.InsertBefore(branch, std::move(treeRange));
        }
        else
        {
            // These block kinds don't have a branch at the end.
            assert((lastNode == nullptr) || (!lastNode->OperIsConditionalJump() &&
                                             !lastNode->OperIs(GT_SWITCH_TABLE, GT_SWITCH, GT_RETURN, GT_RETFILT)));
            blockRange.InsertAtEnd(std::move(treeRange));
        }
    }
}

void LinearScan::insertSwap(
    BasicBlock* block, GenTree* insertionPoint, unsigned lclNum1, regNumber reg1, unsigned lclNum2, regNumber reg2)
{
#ifdef DEBUG
    if (VERBOSE)
    {
        const char* insertionPointString = "top";
        if (insertionPoint == nullptr)
        {
            insertionPointString = "bottom";
        }
        printf("   " FMT_BB " %s: swap V%02u in %s with V%02u in %s\n", block->bbNum, insertionPointString, lclNum1,
               getRegName(reg1), lclNum2, getRegName(reg2));
    }
#endif // DEBUG

    LclVarDsc* varDsc1 = compiler->lvaTable + lclNum1;
    LclVarDsc* varDsc2 = compiler->lvaTable + lclNum2;
    assert(reg1 != REG_STK && reg1 != REG_NA && reg2 != REG_STK && reg2 != REG_NA);

    GenTree* lcl1 = compiler->gtNewLclvNode(lclNum1, varDsc1->TypeGet());
    lcl1->SetRegNum(reg1);
    SetLsraAdded(lcl1);

    GenTree* lcl2 = compiler->gtNewLclvNode(lclNum2, varDsc2->TypeGet());
    lcl2->SetRegNum(reg2);
    SetLsraAdded(lcl2);

    GenTree* swap = compiler->gtNewOperNode(GT_SWAP, TYP_VOID, lcl1, lcl2);
    swap->SetRegNum(REG_NA);
    SetLsraAdded(swap);

    lcl1->gtNext = lcl2;
    lcl2->gtPrev = lcl1;
    lcl2->gtNext = swap;
    swap->gtPrev = lcl2;

    LIR::Range  swapRange  = LIR::SeqTree(compiler, swap);
    LIR::Range& blockRange = LIR::AsRange(block);

    if (insertionPoint != nullptr)
    {
        blockRange.InsertBefore(insertionPoint, std::move(swapRange));
    }
    else
    {
        // Put the copy at the bottom
        // If there's a branch, make an embedded statement that executes just prior to the branch
        if (block->bbJumpKind == BBJ_COND || block->bbJumpKind == BBJ_SWITCH)
        {
            noway_assert(!blockRange.IsEmpty());

            GenTree* branch = blockRange.LastNode();
            assert(branch->OperIsConditionalJump() || branch->OperGet() == GT_SWITCH_TABLE ||
                   branch->OperGet() == GT_SWITCH);

            blockRange.InsertBefore(branch, std::move(swapRange));
        }
        else
        {
            assert(block->bbJumpKind == BBJ_NONE || block->bbJumpKind == BBJ_ALWAYS);
            blockRange.InsertAtEnd(std::move(swapRange));
        }
    }
}

//------------------------------------------------------------------------
// getTempRegForResolution: Get a free register to use for resolution code.
//
// Arguments:
//    fromBlock - The "from" block on the edge being resolved.
//    toBlock   - The "to"block on the edge
//    type      - the type of register required
//
// Return Value:
//    Returns a register that is free on the given edge, or REG_NA if none is available.
//
// Notes:
//    It is up to the caller to check the return value, and to determine whether a register is
//    available, and to handle that case appropriately.
//    It is also up to the caller to cache the return value, as this is not cheap to compute.

regNumber LinearScan::getTempRegForResolution(BasicBlock* fromBlock, BasicBlock* toBlock, var_types type)
{
    // TODO-Throughput: This would be much more efficient if we add RegToVarMaps instead of VarToRegMaps
    // and they would be more space-efficient as well.
    VarToRegMap fromVarToRegMap = getOutVarToRegMap(fromBlock->bbNum);
    VarToRegMap toVarToRegMap   = getInVarToRegMap(toBlock->bbNum);

#ifdef TARGET_ARM
    regMaskTP freeRegs;
    if (type == TYP_DOUBLE)
    {
        // We have to consider all float registers for TYP_DOUBLE
        freeRegs = allRegs(TYP_FLOAT);
    }
    else
    {
        freeRegs = allRegs(type);
    }
#else  // !TARGET_ARM
    regMaskTP freeRegs = allRegs(type);
#endif // !TARGET_ARM

#ifdef DEBUG
    if (getStressLimitRegs() == LSRA_LIMIT_SMALL_SET)
    {
        return REG_NA;
    }
#endif // DEBUG
    INDEBUG(freeRegs = stressLimitRegs(nullptr, freeRegs));

    // We are only interested in the variables that are live-in to the "to" block.
    VarSetOps::Iter iter(compiler, toBlock->bbLiveIn);
    unsigned        varIndex = 0;
    while (iter.NextElem(&varIndex) && freeRegs != RBM_NONE)
    {
        regNumber fromReg = getVarReg(fromVarToRegMap, varIndex);
        regNumber toReg   = getVarReg(toVarToRegMap, varIndex);
        assert(fromReg != REG_NA && toReg != REG_NA);
        if (fromReg != REG_STK)
        {
            freeRegs &= ~genRegMask(fromReg, getIntervalForLocalVar(varIndex)->registerType);
        }
        if (toReg != REG_STK)
        {
            freeRegs &= ~genRegMask(toReg, getIntervalForLocalVar(varIndex)->registerType);
        }
    }

#ifdef TARGET_ARM
    if (type == TYP_DOUBLE)
    {
        // Exclude any doubles for which the odd half isn't in freeRegs.
        freeRegs = freeRegs & ((freeRegs << 1) & RBM_ALLDOUBLE);
    }
#endif

    if (freeRegs == RBM_NONE)
    {
        return REG_NA;
    }
    else
    {
        regNumber tempReg = genRegNumFromMask(genFindLowestBit(freeRegs));
        return tempReg;
    }
}

#ifdef TARGET_ARM
//------------------------------------------------------------------------
// addResolutionForDouble: Add resolution move(s) for TYP_DOUBLE interval
//                         and update location.
//
// Arguments:
//    block           - the BasicBlock into which the move will be inserted.
//    insertionPoint  - the instruction before which to insert the move
//    sourceIntervals - maintains sourceIntervals[reg] which each 'reg' is associated with
//    location        - maintains location[reg] which is the location of the var that was originally in 'reg'.
//    toReg           - the register to which the var is moving
//    fromReg         - the register from which the var is moving
//    resolveType     - the type of resolution to be performed
//
// Return Value:
//    None.
//
// Notes:
//    It inserts at least one move and updates incoming parameter 'location'.
//
void LinearScan::addResolutionForDouble(BasicBlock*     block,
                                        GenTree*        insertionPoint,
                                        Interval**      sourceIntervals,
                                        regNumberSmall* location,
                                        regNumber       toReg,
                                        regNumber       fromReg,
                                        ResolveType     resolveType)
{
    regNumber secondHalfTargetReg = REG_NEXT(fromReg);
    Interval* intervalToBeMoved1  = sourceIntervals[fromReg];
    Interval* intervalToBeMoved2  = sourceIntervals[secondHalfTargetReg];

    assert(!(intervalToBeMoved1 == nullptr && intervalToBeMoved2 == nullptr));

    if (intervalToBeMoved1 != nullptr)
    {
        if (intervalToBeMoved1->registerType == TYP_DOUBLE)
        {
            // TYP_DOUBLE interval occupies a double register, i.e. two float registers.
            assert(intervalToBeMoved2 == nullptr);
            assert(genIsValidDoubleReg(toReg));
        }
        else
        {
            // TYP_FLOAT interval occupies 1st half of double register, i.e. 1st float register
            assert(genIsValidFloatReg(toReg));
        }
        addResolution(block, insertionPoint, intervalToBeMoved1, toReg, fromReg);
        JITDUMP(" (%s)\n", resolveTypeName[resolveType]);
        location[fromReg] = (regNumberSmall)toReg;
    }

    if (intervalToBeMoved2 != nullptr)
    {
        // TYP_FLOAT interval occupies 2nd half of double register.
        assert(intervalToBeMoved2->registerType == TYP_FLOAT);
        regNumber secondHalfTempReg = REG_NEXT(toReg);

        addResolution(block, insertionPoint, intervalToBeMoved2, secondHalfTempReg, secondHalfTargetReg);
        JITDUMP(" (%s)\n", resolveTypeName[resolveType]);
        location[secondHalfTargetReg] = (regNumberSmall)secondHalfTempReg;
    }

    return;
}
#endif // TARGET_ARM

//------------------------------------------------------------------------
// addResolution: Add a resolution move of the given interval
//
// Arguments:
//    block          - the BasicBlock into which the move will be inserted.
//    insertionPoint - the instruction before which to insert the move
//    interval       - the interval of the var to be moved
//    toReg          - the register to which the var is moving
//    fromReg        - the register from which the var is moving
//
// Return Value:
//    None.
//
// Notes:
//    For joins, we insert at the bottom (indicated by an insertionPoint
//    of nullptr), while for splits we insert at the top.
//    This is because for joins 'block' is a pred of the join, while for splits it is a succ.
//    For critical edges, this function may be called twice - once to move from
//    the source (fromReg), if any, to the stack, in which case toReg will be
//    REG_STK, and we insert at the bottom (leave insertionPoint as nullptr).
//    The next time, we want to move from the stack to the destination (toReg),
//    in which case fromReg will be REG_STK, and we insert at the top.

void LinearScan::addResolution(
    BasicBlock* block, GenTree* insertionPoint, Interval* interval, regNumber toReg, regNumber fromReg)
{
#ifdef DEBUG
    const char* insertionPointString;
    if (insertionPoint == nullptr)
    {
        // We can't add resolution to a register at the bottom of a block that has an EHBoundaryOut,
        // except in the case of the "EH Dummy" resolution from the stack.
        assert((block->bbNum > bbNumMaxBeforeResolution) || (fromReg == REG_STK) ||
               !blockInfo[block->bbNum].hasEHBoundaryOut);
        insertionPointString = "bottom";
    }
    else
    {
        // We can't add resolution at the top of a block that has an EHBoundaryIn,
        // except in the case of the "EH Dummy" resolution to the stack.
        assert((block->bbNum > bbNumMaxBeforeResolution) || (toReg == REG_STK) ||
               !blockInfo[block->bbNum].hasEHBoundaryIn);
        insertionPointString = "top";
    }

    // We should never add resolution move inside BBCallAlwaysPairTail.
    noway_assert(!block->isBBCallAlwaysPairTail());

#endif // DEBUG

    JITDUMP("   " FMT_BB " %s: move V%02u from ", block->bbNum, insertionPointString, interval->varNum);
    JITDUMP("%s to %s", getRegName(fromReg), getRegName(toReg));

    insertMove(block, insertionPoint, interval->varNum, fromReg, toReg);
    if (fromReg == REG_STK || toReg == REG_STK)
    {
        assert(interval->isSpilled);
    }
    else
    {
        // We should have already marked this as spilled or split.
        assert((interval->isSpilled) || (interval->isSplit));
    }

    INTRACK_STATS(updateLsraStat(STAT_RESOLUTION_MOV, block->bbNum));
}

//------------------------------------------------------------------------
// handleOutgoingCriticalEdges: Performs the necessary resolution on all critical edges that feed out of 'block'
//
// Arguments:
//    block     - the block with outgoing critical edges.
//
// Return Value:
//    None..
//
// Notes:
//    For all outgoing critical edges (i.e. any successor of this block which is
//    a join edge), if there are any conflicts, split the edge by adding a new block,
//    and generate the resolution code into that block.

void LinearScan::handleOutgoingCriticalEdges(BasicBlock* block)
{
    VARSET_TP outResolutionSet(VarSetOps::Intersection(compiler, block->bbLiveOut, resolutionCandidateVars));
    if (VarSetOps::IsEmpty(compiler, outResolutionSet))
    {
        return;
    }
    VARSET_TP sameResolutionSet(VarSetOps::MakeEmpty(compiler));
    VARSET_TP diffResolutionSet(VarSetOps::MakeEmpty(compiler));

    // Get the outVarToRegMap for this block
    VarToRegMap outVarToRegMap = getOutVarToRegMap(block->bbNum);
    unsigned    succCount      = block->NumSucc(compiler);
    assert(succCount > 1);

    // First, determine the live regs at the end of this block so that we know what regs are
    // available to copy into.
    // Note that for this purpose we use the full live-out set, because we must ensure that
    // even the registers that remain the same across the edge are preserved correctly.
    regMaskTP       liveOutRegs = RBM_NONE;
    VarSetOps::Iter liveOutIter(compiler, block->bbLiveOut);
    unsigned        liveOutVarIndex = 0;
    while (liveOutIter.NextElem(&liveOutVarIndex))
    {
        regNumber fromReg = getVarReg(outVarToRegMap, liveOutVarIndex);
        if (fromReg != REG_STK)
        {
            regMaskTP fromRegMask = genRegMask(fromReg, getIntervalForLocalVar(liveOutVarIndex)->registerType);
            liveOutRegs |= fromRegMask;
        }
    }

    // Next, if this blocks ends with a switch table, or for Arm64, ends with JCMP instruction,
    // make sure to not copy into the registers that are consumed at the end of this block.
    //
    // Note: Only switches and JCMP (for Arm4) have input regs (and so can be fed by copies), so those
    // are the only block-ending branches that need special handling.
    regMaskTP consumedRegs = RBM_NONE;
    if (block->bbJumpKind == BBJ_SWITCH)
    {
        // At this point, Lowering has transformed any non-switch-table blocks into
        // cascading ifs.
        GenTree* switchTable = LIR::AsRange(block).LastNode();
        assert(switchTable != nullptr && switchTable->OperGet() == GT_SWITCH_TABLE);

        consumedRegs = switchTable->gtRsvdRegs;
        GenTree* op1 = switchTable->gtGetOp1();
        GenTree* op2 = switchTable->gtGetOp2();
        noway_assert(op1 != nullptr && op2 != nullptr);
        assert(op1->GetRegNum() != REG_NA && op2->GetRegNum() != REG_NA);
        // No floating point values, so no need to worry about the register type
        // (i.e. for ARM32, where we used the genRegMask overload with a type).
        assert(varTypeIsIntegralOrI(op1) && varTypeIsIntegralOrI(op2));
        consumedRegs |= genRegMask(op1->GetRegNum());
        consumedRegs |= genRegMask(op2->GetRegNum());
    }

#ifdef TARGET_ARM64
    // Next, if this blocks ends with a JCMP, we have to make sure:
    // 1. Not to copy into the register that JCMP uses
    //    e.g. JCMP w21, BRANCH
    // 2. Not to copy into the source of JCMP's operand before it is consumed
    //    e.g. Should not use w0 since it will contain wrong value after resolution
    //          call METHOD
    //          ; mov w0, w19  <-- should not resolve in w0 here.
    //          mov w21, w0
    //          JCMP w21, BRANCH
    // 3. Not to modify the local variable it must consume

    // Note: GT_COPY has special handling in codegen and its generation is merged with the
    // node that consumes its result. So both, the input and output regs of GT_COPY must be
    // excluded from the set available for resolution.
    LclVarDsc* jcmpLocalVarDsc = nullptr;
    if (block->bbJumpKind == BBJ_COND)
    {
        GenTree* lastNode = LIR::AsRange(block).LastNode();

        if (lastNode->OperIs(GT_JCMP))
        {
            GenTree* op1 = lastNode->gtGetOp1();
            consumedRegs |= genRegMask(op1->GetRegNum());

            if (op1->OperIs(GT_COPY))
            {
                GenTree* srcOp1 = op1->gtGetOp1();
                consumedRegs |= genRegMask(srcOp1->GetRegNum());
            }

            if (op1->IsLocal())
            {
                GenTreeLclVarCommon* lcl = op1->AsLclVarCommon();
                jcmpLocalVarDsc          = &compiler->lvaTable[lcl->GetLclNum()];
            }
        }
    }
#endif

    VarToRegMap sameVarToRegMap = sharedCriticalVarToRegMap;
    regMaskTP   sameWriteRegs   = RBM_NONE;
    regMaskTP   diffReadRegs    = RBM_NONE;

    // For each var that may require resolution, classify them as:
    // - in the same register at the end of this block and at each target (no resolution needed)
    // - in different registers at different targets (resolve separately):
    //     diffResolutionSet
    // - in the same register at each target at which it's live, but different from the end of
    //   this block.  We may be able to resolve these as if it is "join", but only if they do not
    //   write to any registers that are read by those in the diffResolutionSet:
    //     sameResolutionSet

    VarSetOps::Iter outResolutionSetIter(compiler, outResolutionSet);
    unsigned        outResolutionSetVarIndex = 0;
    while (outResolutionSetIter.NextElem(&outResolutionSetVarIndex))
    {
        regNumber fromReg             = getVarReg(outVarToRegMap, outResolutionSetVarIndex);
        bool      maybeSameLivePaths  = false;
        bool      liveOnlyAtSplitEdge = true;
        regNumber sameToReg           = REG_NA;
        for (unsigned succIndex = 0; succIndex < succCount; succIndex++)
        {
            BasicBlock* succBlock = block->GetSucc(succIndex, compiler);
            if (!VarSetOps::IsMember(compiler, succBlock->bbLiveIn, outResolutionSetVarIndex))
            {
                maybeSameLivePaths = true;
                continue;
            }
            else if (liveOnlyAtSplitEdge)
            {
                // Is the var live only at those target blocks which are connected by a split edge to this block
                liveOnlyAtSplitEdge = ((succBlock->bbPreds->flNext == nullptr) && (succBlock != compiler->fgFirstBB));
            }

            regNumber toReg = getVarReg(getInVarToRegMap(succBlock->bbNum), outResolutionSetVarIndex);
            if (sameToReg == REG_NA)
            {
                sameToReg = toReg;
                continue;
            }
            if (toReg == sameToReg)
            {
                continue;
            }
            sameToReg = REG_NA;
            break;
        }

        // Check for the cases where we can't write to a register.
        // We only need to check for these cases if sameToReg is an actual register (not REG_STK).
        if (sameToReg != REG_NA && sameToReg != REG_STK)
        {
            // If there's a path on which this var isn't live, it may use the original value in sameToReg.
            // In this case, sameToReg will be in the liveOutRegs of this block.
            // Similarly, if sameToReg is in sameWriteRegs, it has already been used (i.e. for a lclVar that's
            // live only at another target), and we can't copy another lclVar into that reg in this block.
            regMaskTP sameToRegMask =
                genRegMask(sameToReg, getIntervalForLocalVar(outResolutionSetVarIndex)->registerType);
            if (maybeSameLivePaths &&
                (((sameToRegMask & liveOutRegs) != RBM_NONE) || ((sameToRegMask & sameWriteRegs) != RBM_NONE)))
            {
                sameToReg = REG_NA;
            }
            // If this register is busy because it is used by a switch table at the end of the block
            // (or for Arm64, it is consumed by JCMP), we can't do the copy in this block since we can't
            // insert it after the switch (or for Arm64, can't insert and overwrite the operand/source
            // of operand of JCMP).
            if ((sameToRegMask & consumedRegs) != RBM_NONE)
            {
                sameToReg = REG_NA;
            }

#ifdef TARGET_ARM64
            if (jcmpLocalVarDsc && (jcmpLocalVarDsc->lvVarIndex == outResolutionSetVarIndex))
            {
                sameToReg = REG_NA;
            }
#endif

            // If the var is live only at those blocks connected by a split edge and not live-in at some of the
            // target blocks, we will resolve it the same way as if it were in diffResolutionSet and resolution
            // will be deferred to the handling of split edges, which means copy will only be at those target(s).
            //
            // Another way to achieve similar resolution for vars live only at split edges is by removing them
            // from consideration up-front but it requires that we traverse those edges anyway to account for
            // the registers that must note be overwritten.
            if (liveOnlyAtSplitEdge && maybeSameLivePaths)
            {
                sameToReg = REG_NA;
            }
        }

        if (sameToReg == REG_NA)
        {
            VarSetOps::AddElemD(compiler, diffResolutionSet, outResolutionSetVarIndex);
            if (fromReg != REG_STK)
            {
                diffReadRegs |= genRegMask(fromReg, getIntervalForLocalVar(outResolutionSetVarIndex)->registerType);
            }
        }
        else if (sameToReg != fromReg)
        {
            VarSetOps::AddElemD(compiler, sameResolutionSet, outResolutionSetVarIndex);
            setVarReg(sameVarToRegMap, outResolutionSetVarIndex, sameToReg);
            if (sameToReg != REG_STK)
            {
                sameWriteRegs |= genRegMask(sameToReg, getIntervalForLocalVar(outResolutionSetVarIndex)->registerType);
            }
        }
    }

    if (!VarSetOps::IsEmpty(compiler, sameResolutionSet))
    {
        if ((sameWriteRegs & diffReadRegs) != RBM_NONE)
        {
            // We cannot split the "same" and "diff" regs if the "same" set writes registers
            // that must be read by the "diff" set.  (Note that when these are done as a "batch"
            // we carefully order them to ensure all the input regs are read before they are
            // overwritten.)
            VarSetOps::UnionD(compiler, diffResolutionSet, sameResolutionSet);
            VarSetOps::ClearD(compiler, sameResolutionSet);
        }
        else
        {
            // For any vars in the sameResolutionSet, we can simply add the move at the end of "block".
            resolveEdge(block, nullptr, ResolveSharedCritical, sameResolutionSet);
        }
    }
    if (!VarSetOps::IsEmpty(compiler, diffResolutionSet))
    {
        for (unsigned succIndex = 0; succIndex < succCount; succIndex++)
        {
            BasicBlock* succBlock = block->GetSucc(succIndex, compiler);

            // Any "diffResolutionSet" resolution for a block with no other predecessors will be handled later
            // as split resolution.
            if ((succBlock->bbPreds->flNext == nullptr) && (succBlock != compiler->fgFirstBB))
            {
                continue;
            }

            // Now collect the resolution set for just this edge, if any.
            // Check only the vars in diffResolutionSet that are live-in to this successor.
            VarToRegMap succInVarToRegMap = getInVarToRegMap(succBlock->bbNum);
            VARSET_TP   edgeResolutionSet(VarSetOps::Intersection(compiler, diffResolutionSet, succBlock->bbLiveIn));
            VarSetOps::Iter iter(compiler, edgeResolutionSet);
            unsigned        varIndex = 0;
            while (iter.NextElem(&varIndex))
            {
                regNumber fromReg = getVarReg(outVarToRegMap, varIndex);
                regNumber toReg   = getVarReg(succInVarToRegMap, varIndex);

                if (fromReg == toReg)
                {
                    VarSetOps::RemoveElemD(compiler, edgeResolutionSet, varIndex);
                }
            }
            if (!VarSetOps::IsEmpty(compiler, edgeResolutionSet))
            {
                // For EH vars, we can always safely load them from the stack into the target for this block,
                // so if we have only EH vars, we'll do that instead of splitting the edge.
                if ((compiler->compHndBBtabCount > 0) && VarSetOps::IsSubset(compiler, edgeResolutionSet, exceptVars))
                {
                    GenTree*        insertionPoint = LIR::AsRange(succBlock).FirstNonPhiNode();
                    VarSetOps::Iter edgeSetIter(compiler, edgeResolutionSet);
                    unsigned        edgeVarIndex = 0;
                    while (edgeSetIter.NextElem(&edgeVarIndex))
                    {
                        regNumber toReg = getVarReg(succInVarToRegMap, edgeVarIndex);
                        setVarReg(succInVarToRegMap, edgeVarIndex, REG_STK);
                        if (toReg != REG_STK)
                        {
                            Interval* interval = getIntervalForLocalVar(edgeVarIndex);
                            assert(interval->isWriteThru);
                            addResolution(succBlock, insertionPoint, interval, toReg, REG_STK);
                            JITDUMP(" (EHvar)\n");
                        }
                    }
                }
                else
                {
                    resolveEdge(block, succBlock, ResolveCritical, edgeResolutionSet);
                }
            }
        }
    }
}

//------------------------------------------------------------------------
// resolveEdges: Perform resolution across basic block edges
//
// Arguments:
//    None.
//
// Return Value:
//    None.
//
// Notes:
//    Traverse the basic blocks.
//    - If this block has a single predecessor that is not the immediately
//      preceding block, perform any needed 'split' resolution at the beginning of this block
//    - Otherwise if this block has critical incoming edges, handle them.
//    - If this block has a single successor that has multiple predecesors, perform any needed
//      'join' resolution at the end of this block.
//    Note that a block may have both 'split' or 'critical' incoming edge(s) and 'join' outgoing
//    edges.

void LinearScan::resolveEdges()
{
    JITDUMP("RESOLVING EDGES\n");

    // The resolutionCandidateVars set was initialized with all the lclVars that are live-in to
    // any block. We now intersect that set with any lclVars that ever spilled or split.
    // If there are no candidates for resoultion, simply return.

    VarSetOps::IntersectionD(compiler, resolutionCandidateVars, splitOrSpilledVars);
    if (VarSetOps::IsEmpty(compiler, resolutionCandidateVars))
    {
        return;
    }

    BasicBlock *block, *prevBlock = nullptr;

    // Handle all the critical edges first.
    // We will try to avoid resolution across critical edges in cases where all the critical-edge
    // targets of a block have the same home.  We will then split the edges only for the
    // remaining mismatches.  We visit the out-edges, as that allows us to share the moves that are
    // common among all the targets.

    if (hasCriticalEdges)
    {
        foreach_block(compiler, block)
        {
            if (block->bbNum > bbNumMaxBeforeResolution)
            {
                // This is a new block added during resolution - we don't need to visit these now.
                continue;
            }
            if (blockInfo[block->bbNum].hasCriticalOutEdge)
            {
                handleOutgoingCriticalEdges(block);
            }
            prevBlock = block;
        }
    }

    prevBlock = nullptr;
    foreach_block(compiler, block)
    {
        if (block->bbNum > bbNumMaxBeforeResolution)
        {
            // This is a new block added during resolution - we don't need to visit these now.
            continue;
        }

        unsigned    succCount       = block->NumSucc(compiler);
        BasicBlock* uniquePredBlock = block->GetUniquePred(compiler);

        // First, if this block has a single predecessor,
        // we may need resolution at the beginning of this block.
        // This may be true even if it's the block we used for starting locations,
        // if a variable was spilled.
        VARSET_TP inResolutionSet(VarSetOps::Intersection(compiler, block->bbLiveIn, resolutionCandidateVars));
        if (!VarSetOps::IsEmpty(compiler, inResolutionSet))
        {
            if (uniquePredBlock != nullptr)
            {
                // We may have split edges during critical edge resolution, and in the process split
                // a non-critical edge as well.
                // It is unlikely that we would ever have more than one of these in sequence (indeed,
                // I don't think it's possible), but there's no need to assume that it can't.
                while (uniquePredBlock->bbNum > bbNumMaxBeforeResolution)
                {
                    uniquePredBlock = uniquePredBlock->GetUniquePred(compiler);
                    noway_assert(uniquePredBlock != nullptr);
                }
                resolveEdge(uniquePredBlock, block, ResolveSplit, inResolutionSet);
            }
        }

        // Finally, if this block has a single successor:
        //  - and that has at least one other predecessor (otherwise we will do the resolution at the
        //    top of the successor),
        //  - and that is not the target of a critical edge (otherwise we've already handled it)
        // we may need resolution at the end of this block.

        if (succCount == 1)
        {
            BasicBlock* succBlock = block->GetSucc(0, compiler);
            if (succBlock->GetUniquePred(compiler) == nullptr)
            {
                VARSET_TP outResolutionSet(
                    VarSetOps::Intersection(compiler, succBlock->bbLiveIn, resolutionCandidateVars));
                if (!VarSetOps::IsEmpty(compiler, outResolutionSet))
                {
                    resolveEdge(block, succBlock, ResolveJoin, outResolutionSet);
                }
            }
        }
    }

    // Now, fixup the mapping for any blocks that were adding for edge splitting.
    // See the comment prior to the call to fgSplitEdge() in resolveEdge().
    // Note that we could fold this loop in with the checking code below, but that
    // would only improve the debug case, and would clutter up the code somewhat.
    if (compiler->fgBBNumMax > bbNumMaxBeforeResolution)
    {
        foreach_block(compiler, block)
        {
            if (block->bbNum > bbNumMaxBeforeResolution)
            {
                // There may be multiple blocks inserted when we split.  But we must always have exactly
                // one path (i.e. all blocks must be single-successor and single-predecessor),
                // and only one block along the path may be non-empty.
                // Note that we may have a newly-inserted block that is empty, but which connects
                // two non-resolution blocks. This happens when an edge is split that requires it.

                BasicBlock* succBlock = block;
                do
                {
                    succBlock = succBlock->GetUniqueSucc();
                    noway_assert(succBlock != nullptr);
                } while ((succBlock->bbNum > bbNumMaxBeforeResolution) && succBlock->isEmpty());

                BasicBlock* predBlock = block;
                do
                {
                    predBlock = predBlock->GetUniquePred(compiler);
                    noway_assert(predBlock != nullptr);
                } while ((predBlock->bbNum > bbNumMaxBeforeResolution) && predBlock->isEmpty());

                unsigned succBBNum = succBlock->bbNum;
                unsigned predBBNum = predBlock->bbNum;
                if (block->isEmpty())
                {
                    // For the case of the empty block, find the non-resolution block (succ or pred).
                    if (predBBNum > bbNumMaxBeforeResolution)
                    {
                        assert(succBBNum <= bbNumMaxBeforeResolution);
                        predBBNum = 0;
                    }
                    else
                    {
                        succBBNum = 0;
                    }
                }
                else
                {
                    assert((succBBNum <= bbNumMaxBeforeResolution) && (predBBNum <= bbNumMaxBeforeResolution));
                }
                SplitEdgeInfo info = {predBBNum, succBBNum};
                getSplitBBNumToTargetBBNumMap()->Set(block->bbNum, info);

                // Set both the live-in and live-out to the live-in of the successor (by construction liveness
                // doesn't change in a split block).
                VarSetOps::Assign(compiler, block->bbLiveIn, succBlock->bbLiveIn);
                VarSetOps::Assign(compiler, block->bbLiveOut, succBlock->bbLiveIn);
            }
        }
    }

#ifdef DEBUG
    // Make sure the varToRegMaps match up on all edges.
    bool foundMismatch = false;
    foreach_block(compiler, block)
    {
        if (block->isEmpty() && block->bbNum > bbNumMaxBeforeResolution)
        {
            continue;
        }
        VarToRegMap toVarToRegMap = getInVarToRegMap(block->bbNum);
        for (flowList* pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
        {
            BasicBlock*     predBlock       = pred->getBlock();
            VarToRegMap     fromVarToRegMap = getOutVarToRegMap(predBlock->bbNum);
            VarSetOps::Iter iter(compiler, block->bbLiveIn);
            unsigned        varIndex = 0;
            while (iter.NextElem(&varIndex))
            {
                regNumber fromReg = getVarReg(fromVarToRegMap, varIndex);
                regNumber toReg   = getVarReg(toVarToRegMap, varIndex);
                if (fromReg != toReg)
                {
                    Interval* interval = getIntervalForLocalVar(varIndex);
                    // The fromReg and toReg may not match for a write-thru interval where the toReg is
                    // REG_STK, since the stack value is always valid for that case (so no move is needed).
                    if (!interval->isWriteThru || (toReg != REG_STK))
                    {
                        if (!foundMismatch)
                        {
                            foundMismatch = true;
                            printf("Found mismatched var locations after resolution!\n");
                        }
                        printf(" V%02u: " FMT_BB " to " FMT_BB ": %s to %s\n", interval->varNum, predBlock->bbNum,
                               block->bbNum, getRegName(fromReg), getRegName(toReg));
                    }
                }
            }
        }
    }
    assert(!foundMismatch);
#endif
    JITDUMP("\n");
}

//------------------------------------------------------------------------
// resolveEdge: Perform the specified type of resolution between two blocks.
//
// Arguments:
//    fromBlock     - the block from which the edge originates
//    toBlock       - the block at which the edge terminates
//    resolveType   - the type of resolution to be performed
//    liveSet       - the set of tracked lclVar indices which may require resolution
//
// Return Value:
//    None.
//
// Assumptions:
//    The caller must have performed the analysis to determine the type of the edge.
//
// Notes:
//    This method emits the correctly ordered moves necessary to place variables in the
//    correct registers across a Split, Join or Critical edge.
//    In order to avoid overwriting register values before they have been moved to their
//    new home (register/stack), it first does the register-to-stack moves (to free those
//    registers), then the register to register moves, ensuring that the target register
//    is free before the move, and then finally the stack to register moves.

void LinearScan::resolveEdge(BasicBlock*      fromBlock,
                             BasicBlock*      toBlock,
                             ResolveType      resolveType,
                             VARSET_VALARG_TP liveSet)
{
    VarToRegMap fromVarToRegMap = getOutVarToRegMap(fromBlock->bbNum);
    VarToRegMap toVarToRegMap;
    if (resolveType == ResolveSharedCritical)
    {
        toVarToRegMap = sharedCriticalVarToRegMap;
    }
    else
    {
        toVarToRegMap = getInVarToRegMap(toBlock->bbNum);
    }

    // The block to which we add the resolution moves depends on the resolveType
    BasicBlock* block;
    switch (resolveType)
    {
        case ResolveJoin:
        case ResolveSharedCritical:
            block = fromBlock;
            break;
        case ResolveSplit:
            block = toBlock;
            break;
        case ResolveCritical:
            // fgSplitEdge may add one or two BasicBlocks.  It returns the block that splits
            // the edge from 'fromBlock' and 'toBlock', but if it inserts that block right after
            // a block with a fall-through it will have to create another block to handle that edge.
            // These new blocks can be mapped to existing blocks in order to correctly handle
            // the calls to recordVarLocationsAtStartOfBB() from codegen.  That mapping is handled
            // in resolveEdges(), after all the edge resolution has been done (by calling this
            // method for each edge).
            block = compiler->fgSplitEdge(fromBlock, toBlock);

            // Split edges are counted against fromBlock.
            INTRACK_STATS(updateLsraStat(STAT_SPLIT_EDGE, fromBlock->bbNum));
            break;
        default:
            unreached();
            break;
    }

#ifndef TARGET_XARCH
    // We record tempregs for beginning and end of each block.
    // For amd64/x86 we only need a tempReg for float - we'll use xchg for int.
    // TODO-Throughput: It would be better to determine the tempRegs on demand, but the code below
    // modifies the varToRegMaps so we don't have all the correct registers at the time
    // we need to get the tempReg.
    regNumber tempRegInt =
        (resolveType == ResolveSharedCritical) ? REG_NA : getTempRegForResolution(fromBlock, toBlock, TYP_INT);
#endif // !TARGET_XARCH
    regNumber tempRegFlt = REG_NA;
#ifdef TARGET_ARM
    regNumber tempRegDbl = REG_NA;
#endif
    if ((compiler->compFloatingPointUsed) && (resolveType != ResolveSharedCritical))
    {
#ifdef TARGET_ARM
        // Try to reserve a double register for TYP_DOUBLE and use it for TYP_FLOAT too if available.
        tempRegDbl = getTempRegForResolution(fromBlock, toBlock, TYP_DOUBLE);
        if (tempRegDbl != REG_NA)
        {
            tempRegFlt = tempRegDbl;
        }
        else
#endif // TARGET_ARM
        {
            tempRegFlt = getTempRegForResolution(fromBlock, toBlock, TYP_FLOAT);
        }
    }

    regMaskTP targetRegsToDo      = RBM_NONE;
    regMaskTP targetRegsReady     = RBM_NONE;
    regMaskTP targetRegsFromStack = RBM_NONE;

    // The following arrays capture the location of the registers as they are moved:
    // - location[reg] gives the current location of the var that was originally in 'reg'.
    //   (Note that a var may be moved more than once.)
    // - source[reg] gives the original location of the var that needs to be moved to 'reg'.
    // For example, if a var is in rax and needs to be moved to rsi, then we would start with:
    //   location[rax] == rax
    //   source[rsi] == rax     -- this doesn't change
    // Then, if for some reason we need to move it temporary to rbx, we would have:
    //   location[rax] == rbx
    // Once we have completed the move, we will have:
    //   location[rax] == REG_NA
    // This indicates that the var originally in rax is now in its target register.

    regNumberSmall location[REG_COUNT];
    C_ASSERT(sizeof(char) == sizeof(regNumberSmall)); // for memset to work
    memset(location, REG_NA, REG_COUNT);
    regNumberSmall source[REG_COUNT];
    memset(source, REG_NA, REG_COUNT);

    // What interval is this register associated with?
    // (associated with incoming reg)
    Interval* sourceIntervals[REG_COUNT];
    memset(&sourceIntervals, 0, sizeof(sourceIntervals));

    // Intervals for vars that need to be loaded from the stack
    Interval* stackToRegIntervals[REG_COUNT];
    memset(&stackToRegIntervals, 0, sizeof(stackToRegIntervals));

    // Get the starting insertion point for the "to" resolution
    GenTree* insertionPoint = nullptr;
    if (resolveType == ResolveSplit || resolveType == ResolveCritical)
    {
        insertionPoint = LIR::AsRange(block).FirstNonPhiNode();
    }

    // If this is an edge between EH regions, we may have "extra" live-out EH vars.
    // If we are adding resolution at the end of the block, we need to create "virtual" moves
    // for these so that their registers are freed and can be reused.
    if ((resolveType == ResolveJoin) && (compiler->compHndBBtabCount > 0))
    {
        VARSET_TP extraLiveSet(VarSetOps::Diff(compiler, block->bbLiveOut, toBlock->bbLiveIn));
        VarSetOps::IntersectionD(compiler, extraLiveSet, registerCandidateVars);
        VarSetOps::Iter iter(compiler, extraLiveSet);
        unsigned        extraVarIndex = 0;
        while (iter.NextElem(&extraVarIndex))
        {
            Interval* interval = getIntervalForLocalVar(extraVarIndex);
            assert(interval->isWriteThru);
            regNumber fromReg = getVarReg(fromVarToRegMap, extraVarIndex);
            if (fromReg != REG_STK)
            {
                addResolution(block, insertionPoint, interval, REG_STK, fromReg);
                JITDUMP(" (EH DUMMY)\n");
                setVarReg(fromVarToRegMap, extraVarIndex, REG_STK);
            }
        }
    }

    // First:
    //   - Perform all moves from reg to stack (no ordering needed on these)
    //   - For reg to reg moves, record the current location, associating their
    //     source location with the target register they need to go into
    //   - For stack to reg moves (done last, no ordering needed between them)
    //     record the interval associated with the target reg
    // TODO-Throughput: We should be looping over the liveIn and liveOut registers, since
    // that will scale better than the live variables

    VarSetOps::Iter iter(compiler, liveSet);
    unsigned        varIndex = 0;
    while (iter.NextElem(&varIndex))
    {
        Interval* interval = getIntervalForLocalVar(varIndex);
        regNumber fromReg  = getVarReg(fromVarToRegMap, varIndex);
        regNumber toReg    = getVarReg(toVarToRegMap, varIndex);
        if (fromReg == toReg)
        {
            continue;
        }
        if (interval->isWriteThru && (toReg == REG_STK))
        {
            // We don't actually move a writeThru var back to the stack, as its stack value is always valid.
            // However, if this is a Join edge (i.e. the move is happening at the bottom of the block),
            // and it is a "normal" flow edge, we will go ahead and generate a mov instruction, which will be
            // a NOP but will cause the variable to be removed from being live in the register.
            if ((resolveType == ResolveSplit) || block->hasEHBoundaryOut())
            {
                continue;
            }
        }
        // For Critical edges, the location will not change on either side of the edge,
        // since we'll add a new block to do the move.
        if (resolveType == ResolveSplit)
        {
            setVarReg(toVarToRegMap, varIndex, fromReg);
        }
        else if (resolveType == ResolveJoin || resolveType == ResolveSharedCritical)
        {
            setVarReg(fromVarToRegMap, varIndex, toReg);
        }

        assert(fromReg < UCHAR_MAX && toReg < UCHAR_MAX);

        if (fromReg == REG_STK)
        {
            stackToRegIntervals[toReg] = interval;
            targetRegsFromStack |= genRegMask(toReg);
        }
        else if (toReg == REG_STK)
        {
            // Do the reg to stack moves now
            addResolution(block, insertionPoint, interval, REG_STK, fromReg);
            JITDUMP(" (%s)\n",
                    (interval->isWriteThru && (toReg == REG_STK)) ? "EH DUMMY" : resolveTypeName[resolveType]);
        }
        else
        {
            location[fromReg]        = (regNumberSmall)fromReg;
            source[toReg]            = (regNumberSmall)fromReg;
            sourceIntervals[fromReg] = interval;
            targetRegsToDo |= genRegMask(toReg);
        }
    }

    // REGISTER to REGISTER MOVES

    // First, find all the ones that are ready to move now
    regMaskTP targetCandidates = targetRegsToDo;
    while (targetCandidates != RBM_NONE)
    {
        regMaskTP targetRegMask = genFindLowestBit(targetCandidates);
        targetCandidates &= ~targetRegMask;
        regNumber targetReg = genRegNumFromMask(targetRegMask);
        if (location[targetReg] == REG_NA)
        {
#ifdef TARGET_ARM
            regNumber sourceReg = (regNumber)source[targetReg];
            Interval* interval  = sourceIntervals[sourceReg];
            if (interval->registerType == TYP_DOUBLE)
            {
                // For ARM32, make sure that both of the float halves of the double register are available.
                assert(genIsValidDoubleReg(targetReg));
                regNumber anotherHalfRegNum = REG_NEXT(targetReg);
                if (location[anotherHalfRegNum] == REG_NA)
                {
                    targetRegsReady |= targetRegMask;
                }
            }
            else
#endif // TARGET_ARM
            {
                targetRegsReady |= targetRegMask;
            }
        }
    }

    // Perform reg to reg moves
    while (targetRegsToDo != RBM_NONE)
    {
        while (targetRegsReady != RBM_NONE)
        {
            regMaskTP targetRegMask = genFindLowestBit(targetRegsReady);
            targetRegsToDo &= ~targetRegMask;
            targetRegsReady &= ~targetRegMask;
            regNumber targetReg = genRegNumFromMask(targetRegMask);
            assert(location[targetReg] != targetReg);
            assert(targetReg < REG_COUNT);
            regNumber sourceReg = (regNumber)source[targetReg];
            assert(sourceReg < REG_COUNT);
            regNumber fromReg = (regNumber)location[sourceReg];
            // stack to reg movs should be done last as part of "targetRegsFromStack"
            assert(fromReg < REG_STK);
            Interval* interval = sourceIntervals[sourceReg];
            assert(interval != nullptr);
            addResolution(block, insertionPoint, interval, targetReg, fromReg);
            JITDUMP(" (%s)\n", resolveTypeName[resolveType]);
            sourceIntervals[sourceReg] = nullptr;
            location[sourceReg]        = REG_NA;
            regMaskTP fromRegMask      = genRegMask(fromReg);

            // Do we have a free targetReg?
            if (fromReg == sourceReg)
            {
                if (source[fromReg] != REG_NA && ((targetRegsFromStack & fromRegMask) != fromRegMask))
                {
                    targetRegsReady |= fromRegMask;
#ifdef TARGET_ARM
                    if (genIsValidDoubleReg(fromReg))
                    {
                        // Ensure that either:
                        // - the Interval targeting fromReg is not double, or
                        // - the other half of the double is free.
                        Interval* otherInterval = sourceIntervals[source[fromReg]];
                        regNumber upperHalfReg  = REG_NEXT(fromReg);
                        if ((otherInterval->registerType == TYP_DOUBLE) && (location[upperHalfReg] != REG_NA))
                        {
                            targetRegsReady &= ~fromRegMask;
                        }
                    }
                }
                else if (genIsValidFloatReg(fromReg) && !genIsValidDoubleReg(fromReg))
                {
                    // We may have freed up the other half of a double where the lower half
                    // was already free.
                    regNumber lowerHalfReg     = REG_PREV(fromReg);
                    regNumber lowerHalfSrcReg  = (regNumber)source[lowerHalfReg];
                    regNumber lowerHalfSrcLoc  = (regNumber)location[lowerHalfReg];
                    regMaskTP lowerHalfRegMask = genRegMask(lowerHalfReg);
                    // Necessary conditions:
                    // - There is a source register for this reg (lowerHalfSrcReg != REG_NA)
                    // - It is currently free                    (lowerHalfSrcLoc == REG_NA)
                    // - The source interval isn't yet completed (sourceIntervals[lowerHalfSrcReg] != nullptr)
                    // - It's not in the ready set               ((targetRegsReady & lowerHalfRegMask) ==
                    //                                            RBM_NONE)
                    // - It's not resolved from stack            ((targetRegsFromStack & lowerHalfRegMask) !=
                    //                                            lowerHalfRegMask)
                    if ((lowerHalfSrcReg != REG_NA) && (lowerHalfSrcLoc == REG_NA) &&
                        (sourceIntervals[lowerHalfSrcReg] != nullptr) &&
                        ((targetRegsReady & lowerHalfRegMask) == RBM_NONE) &&
                        ((targetRegsFromStack & lowerHalfRegMask) != lowerHalfRegMask))
                    {
                        // This must be a double interval, otherwise it would be in targetRegsReady, or already
                        // completed.
                        assert(sourceIntervals[lowerHalfSrcReg]->registerType == TYP_DOUBLE);
                        targetRegsReady |= lowerHalfRegMask;
                    }
#endif // TARGET_ARM
                }
            }
        }
        if (targetRegsToDo != RBM_NONE)
        {
            regMaskTP targetRegMask = genFindLowestBit(targetRegsToDo);
            regNumber targetReg     = genRegNumFromMask(targetRegMask);

            // Is it already there due to other moves?
            // If not, move it to the temp reg, OR swap it with another register
            regNumber sourceReg = (regNumber)source[targetReg];
            regNumber fromReg   = (regNumber)location[sourceReg];
            if (targetReg == fromReg)
            {
                targetRegsToDo &= ~targetRegMask;
            }
            else
            {
                regNumber tempReg = REG_NA;
                bool      useSwap = false;
                if (emitter::isFloatReg(targetReg))
                {
#ifdef TARGET_ARM
                    if (sourceIntervals[fromReg]->registerType == TYP_DOUBLE)
                    {
                        // ARM32 requires a double temp register for TYP_DOUBLE.
                        tempReg = tempRegDbl;
                    }
                    else
#endif // TARGET_ARM
                        tempReg = tempRegFlt;
                }
#ifdef TARGET_XARCH
                else
                {
                    useSwap = true;
                }
#else // !TARGET_XARCH

                else
                {
                    tempReg = tempRegInt;
                }

#endif // !TARGET_XARCH
                if (useSwap || tempReg == REG_NA)
                {
                    // First, we have to figure out the destination register for what's currently in fromReg,
                    // so that we can find its sourceInterval.
                    regNumber otherTargetReg = REG_NA;

                    // By chance, is fromReg going where it belongs?
                    if (location[source[fromReg]] == targetReg)
                    {
                        otherTargetReg = fromReg;
                        // If we can swap, we will be done with otherTargetReg as well.
                        // Otherwise, we'll spill it to the stack and reload it later.
                        if (useSwap)
                        {
                            regMaskTP fromRegMask = genRegMask(fromReg);
                            targetRegsToDo &= ~fromRegMask;
                        }
                    }
                    else
                    {
                        // Look at the remaining registers from targetRegsToDo (which we expect to be relatively
                        // small at this point) to find out what's currently in targetReg.
                        regMaskTP mask = targetRegsToDo;
                        while (mask != RBM_NONE && otherTargetReg == REG_NA)
                        {
                            regMaskTP nextRegMask = genFindLowestBit(mask);
                            regNumber nextReg     = genRegNumFromMask(nextRegMask);
                            mask &= ~nextRegMask;
                            if (location[source[nextReg]] == targetReg)
                            {
                                otherTargetReg = nextReg;
                            }
                        }
                    }
                    assert(otherTargetReg != REG_NA);

                    if (useSwap)
                    {
                        // Generate a "swap" of fromReg and targetReg
                        insertSwap(block, insertionPoint, sourceIntervals[source[otherTargetReg]]->varNum, targetReg,
                                   sourceIntervals[sourceReg]->varNum, fromReg);
                        location[sourceReg]              = REG_NA;
                        location[source[otherTargetReg]] = (regNumberSmall)fromReg;

                        INTRACK_STATS(updateLsraStat(STAT_RESOLUTION_MOV, block->bbNum));
                    }
                    else
                    {
                        // Spill "targetReg" to the stack and add its eventual target (otherTargetReg)
                        // to "targetRegsFromStack", which will be handled below.
                        // NOTE: This condition is very rare.  Setting COMPlus_JitStressRegs=0x203
                        // has been known to trigger it in JIT SH.

                        // First, spill "otherInterval" from targetReg to the stack.
                        Interval* otherInterval = sourceIntervals[source[otherTargetReg]];
                        setIntervalAsSpilled(otherInterval);
                        addResolution(block, insertionPoint, otherInterval, REG_STK, targetReg);
                        JITDUMP(" (%s)\n", resolveTypeName[resolveType]);
                        location[source[otherTargetReg]] = REG_STK;

                        regMaskTP otherTargetRegMask = genRegMask(otherTargetReg);
                        targetRegsFromStack |= otherTargetRegMask;
                        stackToRegIntervals[otherTargetReg] = otherInterval;
                        targetRegsToDo &= ~otherTargetRegMask;

                        // Now, move the interval that is going to targetReg.
                        addResolution(block, insertionPoint, sourceIntervals[sourceReg], targetReg, fromReg);
                        JITDUMP(" (%s)\n", resolveTypeName[resolveType]);
                        location[sourceReg] = REG_NA;

                        // Add its "fromReg" to "targetRegsReady", only if:
                        // - It was one of the target register we originally determined.
                        // - It is not the eventual target (otherTargetReg) because its
                        //   value will be retrieved from STK.
                        if (source[fromReg] != REG_NA && fromReg != otherTargetReg)
                        {
                            regMaskTP fromRegMask = genRegMask(fromReg);
                            targetRegsReady |= fromRegMask;
#ifdef TARGET_ARM
                            if (genIsValidDoubleReg(fromReg))
                            {
                                // Ensure that either:
                                // - the Interval targeting fromReg is not double, or
                                // - the other half of the double is free.
                                Interval* otherInterval = sourceIntervals[source[fromReg]];
                                regNumber upperHalfReg  = REG_NEXT(fromReg);
                                if ((otherInterval->registerType == TYP_DOUBLE) && (location[upperHalfReg] != REG_NA))
                                {
                                    targetRegsReady &= ~fromRegMask;
                                }
                            }
#endif // TARGET_ARM
                        }
                    }
                    targetRegsToDo &= ~targetRegMask;
                }
                else
                {
                    compiler->codeGen->regSet.rsSetRegsModified(genRegMask(tempReg) DEBUGARG(true));
#ifdef TARGET_ARM
                    if (sourceIntervals[fromReg]->registerType == TYP_DOUBLE)
                    {
                        assert(genIsValidDoubleReg(targetReg));
                        assert(genIsValidDoubleReg(tempReg));

                        addResolutionForDouble(block, insertionPoint, sourceIntervals, location, tempReg, targetReg,
                                               resolveType);
                    }
                    else
#endif // TARGET_ARM
                    {
                        assert(sourceIntervals[targetReg] != nullptr);

                        addResolution(block, insertionPoint, sourceIntervals[targetReg], tempReg, targetReg);
                        JITDUMP(" (%s)\n", resolveTypeName[resolveType]);
                        location[targetReg] = (regNumberSmall)tempReg;
                    }
                    targetRegsReady |= targetRegMask;
                }
            }
        }
    }

    // Finally, perform stack to reg moves
    // All the target regs will be empty at this point
    while (targetRegsFromStack != RBM_NONE)
    {
        regMaskTP targetRegMask = genFindLowestBit(targetRegsFromStack);
        targetRegsFromStack &= ~targetRegMask;
        regNumber targetReg = genRegNumFromMask(targetRegMask);

        Interval* interval = stackToRegIntervals[targetReg];
        assert(interval != nullptr);

        addResolution(block, insertionPoint, interval, targetReg, REG_STK);
        JITDUMP(" (%s)\n", resolveTypeName[resolveType]);
    }
}

#if TRACK_LSRA_STATS

const char* LinearScan::getStatName(unsigned stat)
{
    LsraStat lsraStat = (LsraStat)stat;
    assert(lsraStat != LsraStat::COUNT);

    static const char* const lsraStatNames[] = {
#define LSRA_STAT_DEF(stat, name) name,
#include "lsra_stats.h"
#undef LSRA_STAT_DEF
#define REG_SEL_DEF(stat, value, shortname, orderSeqId) #stat,
#include "lsra_score.h"
#undef REG_SEL_DEF
    };

    assert(stat < ArrLen(lsraStatNames));
    return lsraStatNames[lsraStat];
}

LsraStat LinearScan::getLsraStatFromScore(RegisterScore registerScore)
{
    switch (registerScore)
    {
#define REG_SEL_DEF(stat, value, shortname, orderSeqId)                                                                \
    case RegisterScore::stat:                                                                                          \
        return LsraStat::STAT_##stat;
#include "lsra_score.h"
#undef REG_SEL_DEF
        default:
            return LsraStat::STAT_FREE;
    }
}

// ----------------------------------------------------------
// updateLsraStat: Increment LSRA stat counter.
//
// Arguments:
//    stat      -   LSRA stat enum
//    bbNum     -   Basic block to which LSRA stat needs to be
//                  associated with.
//
void LinearScan::updateLsraStat(LsraStat stat, unsigned bbNum)
{
    if (bbNum > bbNumMaxBeforeResolution)
    {
        // This is a newly created basic block as part of resolution.
        // These blocks contain resolution moves that are already accounted.
        return;
    }

    ++(blockInfo[bbNum].stats[(unsigned)stat]);
}

// -----------------------------------------------------------
// dumpLsraStats - dumps Lsra stats to given file.
//
// Arguments:
//    file    -  file to which stats are to be written.
//
void LinearScan::dumpLsraStats(FILE* file)
{
    unsigned             sumStats[LsraStat::COUNT] = {0};
    BasicBlock::weight_t wtdStats[LsraStat::COUNT] = {0};

    fprintf(file, "----------\n");
    fprintf(file, "LSRA Stats");
#ifdef DEBUG
    if (!VERBOSE)
    {
        fprintf(file, " : %s\n", compiler->info.compFullName);
    }
    else
    {
        // In verbose mode no need to print full name
        // while printing lsra stats.
        fprintf(file, "\n");
    }
#else
    fprintf(file, " : %s\n", compiler->eeGetMethodFullName(compiler->info.compCompHnd));
#endif

    fprintf(file, "----------\n");
#ifdef DEBUG
    fprintf(file, "Register selection order: %S\n", JitConfig.JitLsraOrdering());
#endif
    fprintf(file, "Total Tracked Vars:  %d\n", compiler->lvaTrackedCount);
    fprintf(file, "Total Reg Cand Vars: %d\n", regCandidateVarCount);
    fprintf(file, "Total number of Intervals: %d\n",
            static_cast<unsigned>((intervals.size() == 0 ? 0 : (intervals.size() - 1))));
    fprintf(file, "Total number of RefPositions: %d\n", static_cast<unsigned>(refPositions.size() - 1));

    // compute total number of spill temps created
    unsigned numSpillTemps = 0;
    for (int i = 0; i < TYP_COUNT; i++)
    {
        numSpillTemps += maxSpill[i];
    }
    fprintf(file, "Total Number of spill temps created: %d\n", numSpillTemps);
    fprintf(file, "..........\n");
    bool addedBlockHeader = false;
    bool anyNonZeroStat   = false;

    for (int statIndex = 0; statIndex < LsraStat::COUNT; statIndex++)
    {
        unsigned lsraStat = blockInfo[0].stats[statIndex];

        if (lsraStat != 0)
        {
            if (!addedBlockHeader)
            {
                addedBlockHeader = true;
                fprintf(file, FMT_BB " [%8.2f]: ", 0, blockInfo[0].weight);
                fprintf(file, "%s = %d", getStatName(statIndex), lsraStat);
            }
            else
            {
                fprintf(file, ", %s = %d", getStatName(statIndex), lsraStat);
            }

            sumStats[statIndex] += lsraStat;
            wtdStats[statIndex] += (lsraStat * blockInfo[0].weight);
            anyNonZeroStat = true;
        }
    }
    if (anyNonZeroStat)
    {
        fprintf(file, "\n");
    }

    for (BasicBlock* block = compiler->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (block->bbNum > bbNumMaxBeforeResolution)
        {
            continue;
        }

        addedBlockHeader = false;
        anyNonZeroStat   = false;
        for (int statIndex = 0; statIndex < LsraStat::COUNT; statIndex++)
        {
            unsigned lsraStat = blockInfo[block->bbNum].stats[statIndex];

            if (lsraStat != 0)
            {
                if (!addedBlockHeader)
                {
                    addedBlockHeader = true;
                    fprintf(file, FMT_BB " [%8.2f]: ", block->bbNum, block->bbWeight);
                    fprintf(file, "%s = %d", getStatName(statIndex), lsraStat);
                }
                else
                {
                    fprintf(file, ", %s = %d", getStatName(statIndex), lsraStat);
                }

                sumStats[statIndex] += lsraStat;
                wtdStats[statIndex] += (lsraStat * block->bbWeight);
                anyNonZeroStat = true;
            }
        }

        if (anyNonZeroStat)
        {
            fprintf(file, "\n");
        }
    }

    fprintf(file, "..........\n");
    for (int regSelectI = 0; regSelectI < LsraStat::COUNT; regSelectI++)
    {
        if (regSelectI == firstRegSelStat)
        {
            fprintf(file, "..........\n");
        }
        // TODO-review: I don't see a point of displaying Stats (SpillCount, etc.) if they are zero. Thoughts?
        if ((regSelectI < firstRegSelStat) || (sumStats[regSelectI] != 0))
        {
            // Print register selection stats
            if (regSelectI >= firstRegSelStat)
            {
                fprintf(file, "Total %s [#%2d] : %d   Weighted: %f\n", getStatName(regSelectI),
                        (regSelectI - firstRegSelStat + 1), sumStats[regSelectI], wtdStats[regSelectI]);
            }
            else
            {
                fprintf(file, "Total %s : %d   Weighted: %f\n", getStatName(regSelectI), sumStats[regSelectI],
                        wtdStats[regSelectI]);
            }
        }
    }
    printf("\n");
}

// -----------------------------------------------------------
// dumpLsraStatsCsvFormat - dumps Lsra stats to given file in csv format.
//
// Arguments:
//    file    -  file to which stats are to be written.
//
void LinearScan::dumpLsraStatsCsv(FILE* file)
{
    unsigned sumStats[LsraStat::COUNT] = {0};

    // Write the header if the file is empty
    if (ftell(file) == 0)
    {
        // header
        fprintf(file, "\"Method Name\"");
        for (int statIndex = 0; statIndex < LsraStat::COUNT; statIndex++)
        {
            fprintf(file, ",\"%s\"", LinearScan::getStatName(statIndex));
        }
        fprintf(file, ",\"PerfScore\"\n");
    }

    // bbNum == 0
    for (int statIndex = 0; statIndex < LsraStat::COUNT; statIndex++)
    {
        sumStats[statIndex] += blockInfo[0].stats[statIndex];
    }

    // blocks
    for (BasicBlock* block = compiler->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (block->bbNum > bbNumMaxBeforeResolution)
        {
            continue;
        }

        for (int statIndex = 0; statIndex < LsraStat::COUNT; statIndex++)
        {
            sumStats[statIndex] += blockInfo[block->bbNum].stats[statIndex];
        }
    }

    fprintf(file, "\"%s\"", compiler->info.compFullName);
    for (int statIndex = 0; statIndex < LsraStat::COUNT; statIndex++)
    {
        fprintf(file, ",%u", sumStats[statIndex]);
    }
    fprintf(file, ",%.2f\n", compiler->info.compPerfScore);
}
#endif // TRACK_LSRA_STATS

#ifdef DEBUG
void dumpRegMask(regMaskTP regs)
{
    if (regs == RBM_ALLINT)
    {
        printf("[allInt]");
    }
    else if (regs == (RBM_ALLINT & ~RBM_FPBASE))
    {
        printf("[allIntButFP]");
    }
    else if (regs == RBM_ALLFLOAT)
    {
        printf("[allFloat]");
    }
    else if (regs == RBM_ALLDOUBLE)
    {
        printf("[allDouble]");
    }
    else
    {
        dspRegMask(regs);
    }
}

static const char* getRefTypeName(RefType refType)
{
    switch (refType)
    {
#define DEF_REFTYPE(memberName, memberValue, shortName)                                                                \
    case memberName:                                                                                                   \
        return #memberName;
#include "lsra_reftypes.h"
#undef DEF_REFTYPE
        default:
            return nullptr;
    }
}

static const char* getRefTypeShortName(RefType refType)
{
    switch (refType)
    {
#define DEF_REFTYPE(memberName, memberValue, shortName)                                                                \
    case memberName:                                                                                                   \
        return shortName;
#include "lsra_reftypes.h"
#undef DEF_REFTYPE
        default:
            return nullptr;
    }
}

//------------------------------------------------------------------------
// getScoreName: Returns the texual name of register score
const char* LinearScan::getScoreName(RegisterScore score)
{
    switch (score)
    {
#define REG_SEL_DEF(stat, value, shortname, orderSeqId)                                                                \
    case stat:                                                                                                         \
        return shortname;
#include "lsra_score.h"
#undef REG_SEL_DEF
        default:
            return "  -  ";
    }
}

void RefPosition::dump()
{
    printf("<RefPosition #%-3u @%-3u", rpNum, nodeLocation);

    printf(" %s ", getRefTypeName(refType));

    if (this->IsPhysRegRef())
    {
        this->getReg()->tinyDump();
    }
    else if (getInterval())
    {
        this->getInterval()->tinyDump();
    }
    if (this->treeNode)
    {
        printf("%s", treeNode->OpName(treeNode->OperGet()));
        if (this->treeNode->IsMultiRegNode())
        {
            printf("[%d]", this->multiRegIdx);
        }
    }
    printf(" " FMT_BB " ", this->bbNum);

    printf("regmask=");
    dumpRegMask(registerAssignment);

    printf(" minReg=%d", minRegCandidateCount);

    if (this->lastUse)
    {
        printf(" last");
    }
    if (this->reload)
    {
        printf(" reload");
    }
    if (this->spillAfter)
    {
        printf(" spillAfter");
    }
    if (this->writeThru)
    {
        printf(" writeThru");
    }
    if (this->moveReg)
    {
        printf(" move");
    }
    if (this->copyReg)
    {
        printf(" copy");
    }
    if (this->isFixedRegRef)
    {
        printf(" fixed");
    }
    if (this->isLocalDefUse)
    {
        printf(" local");
    }
    if (this->delayRegFree)
    {
        printf(" delay");
    }
    if (this->outOfOrder)
    {
        printf(" outOfOrder");
    }

    if (this->RegOptional())
    {
        printf(" regOptional");
    }
    printf(">\n");
}

void RegRecord::dump()
{
    tinyDump();
}

void Interval::dump()
{
    printf("Interval %2u:", intervalIndex);

    if (isLocalVar)
    {
        printf(" (V%02u)", varNum);
    }
    else if (IsUpperVector())
    {
        assert(relatedInterval != nullptr);
        printf(" (U%02u)", relatedInterval->varNum);
    }
    printf(" %s", varTypeName(registerType));
    if (isInternal)
    {
        printf(" (INTERNAL)");
    }
    if (isSpilled)
    {
        printf(" (SPILLED)");
    }
    if (isSplit)
    {
        printf(" (SPLIT)");
    }
    if (isStructField)
    {
        printf(" (field)");
    }
    if (isPromotedStruct)
    {
        printf(" (promoted struct)");
    }
    if (hasConflictingDefUse)
    {
        printf(" (def-use conflict)");
    }
    if (hasInterferingUses)
    {
        printf(" (interfering uses)");
    }
    if (isSpecialPutArg)
    {
        printf(" (specialPutArg)");
    }
    if (isConstant)
    {
        printf(" (constant)");
    }
    if (isWriteThru)
    {
        printf(" (writeThru)");
    }

    printf(" RefPositions {");
    for (RefPosition* refPosition = this->firstRefPosition; refPosition != nullptr;
         refPosition              = refPosition->nextRefPosition)
    {
        printf("#%u@%u", refPosition->rpNum, refPosition->nodeLocation);
        if (refPosition->nextRefPosition)
        {
            printf(" ");
        }
    }
    printf("}");

    // this is not used (yet?)
    // printf(" SpillOffset %d", this->spillOffset);

    printf(" physReg:%s", getRegName(physReg));

    printf(" Preferences=");
    dumpRegMask(this->registerPreferences);

    if (relatedInterval)
    {
        printf(" RelatedInterval ");
        relatedInterval->microDump();
    }

    printf("\n");
}

// print out very concise representation
void Interval::tinyDump()
{
    printf("<Ivl:%u", intervalIndex);
    if (isLocalVar)
    {
        printf(" V%02u", varNum);
    }
    else if (IsUpperVector())
    {
        assert(relatedInterval != nullptr);
        printf(" (U%02u)", relatedInterval->varNum);
    }
    else if (isInternal)
    {
        printf(" internal");
    }
    printf("> ");
}

// print out extremely concise representation
void Interval::microDump()
{
    if (isLocalVar)
    {
        printf("<V%02u/L%u>", varNum, intervalIndex);
        return;
    }
    else if (IsUpperVector())
    {
        assert(relatedInterval != nullptr);
        printf(" (U%02u)", relatedInterval->varNum);
    }
    char intervalTypeChar = 'I';
    if (isInternal)
    {
        intervalTypeChar = 'T';
    }
    printf("<%c%u>", intervalTypeChar, intervalIndex);
}

void RegRecord::tinyDump()
{
    printf("<Reg:%-3s> ", getRegName(regNum));
}

void LinearScan::dumpDefList()
{
    if (!VERBOSE)
    {
        return;
    }
    JITDUMP("DefList: { ");
    bool first = true;
    for (RefInfoListNode *listNode = defList.Begin(), *end = defList.End(); listNode != end;
         listNode = listNode->Next())
    {
        GenTree* node = listNode->treeNode;
        JITDUMP("%sN%03u.t%d. %s", first ? "" : "; ", node->gtSeqNum, node->gtTreeID, GenTree::OpName(node->OperGet()));
        first = false;
    }
    JITDUMP(" }\n");
}

void LinearScan::lsraDumpIntervals(const char* msg)
{
    printf("\nLinear scan intervals %s:\n", msg);
    for (Interval& interval : intervals)
    {
        // only dump something if it has references
        // if (interval->firstRefPosition)
        interval.dump();
    }

    printf("\n");
}

// Dumps a tree node as a destination or source operand, with the style
// of dump dependent on the mode
void LinearScan::lsraGetOperandString(GenTree*          tree,
                                      LsraTupleDumpMode mode,
                                      char*             operandString,
                                      unsigned          operandStringLength)
{
    const char* lastUseChar = "";
    if (tree->OperIsScalarLocal() && ((tree->gtFlags & GTF_VAR_DEATH) != 0))
    {
        lastUseChar = "*";
    }
    switch (mode)
    {
        case LinearScan::LSRA_DUMP_PRE:
        case LinearScan::LSRA_DUMP_REFPOS:
            _snprintf_s(operandString, operandStringLength, operandStringLength, "t%d%s", tree->gtTreeID, lastUseChar);
            break;
        case LinearScan::LSRA_DUMP_POST:
        {
            Compiler* compiler = JitTls::GetCompiler();

            if (!tree->gtHasReg())
            {
                _snprintf_s(operandString, operandStringLength, operandStringLength, "STK%s", lastUseChar);
            }
            else
            {
                regNumber reg       = tree->GetRegNum();
                int       charCount = _snprintf_s(operandString, operandStringLength, operandStringLength, "%s%s",
                                            getRegName(reg, genIsValidFloatReg(reg)), lastUseChar);
                operandString += charCount;
                operandStringLength -= charCount;

                if (tree->IsMultiRegNode())
                {
                    unsigned regCount = tree->IsMultiRegLclVar()
                                            ? compiler->lvaGetDesc(tree->AsLclVar()->GetLclNum())->lvFieldCnt
                                            : tree->GetMultiRegCount();
                    for (unsigned regIndex = 1; regIndex < regCount; regIndex++)
                    {
                        regNumber reg = tree->GetRegByIndex(regIndex);
                        charCount     = _snprintf_s(operandString, operandStringLength, operandStringLength, ",%s%s",
                                                getRegName(reg, genIsValidFloatReg(reg)), lastUseChar);
                        operandString += charCount;
                        operandStringLength -= charCount;
                    }
                }
            }
        }
        break;
        default:
            printf("ERROR: INVALID TUPLE DUMP MODE\n");
            break;
    }
}
void LinearScan::lsraDispNode(GenTree* tree, LsraTupleDumpMode mode, bool hasDest)
{
    Compiler*      compiler            = JitTls::GetCompiler();
    const unsigned operandStringLength = 6 * MAX_MULTIREG_COUNT + 1;
    char           operandString[operandStringLength];
    const char*    emptyDestOperand = "               ";
    char           spillChar        = ' ';

    if (mode == LinearScan::LSRA_DUMP_POST)
    {
        if ((tree->gtFlags & GTF_SPILL) != 0)
        {
            spillChar = 'S';
        }
        if (!hasDest && tree->gtHasReg())
        {
            // A node can define a register, but not produce a value for a parent to consume,
            // i.e. in the "localDefUse" case.
            // There used to be an assert here that we wouldn't spill such a node.
            // However, we can have unused lclVars that wind up being the node at which
            // it is spilled. This probably indicates a bug, but we don't realy want to
            // assert during a dump.
            if (spillChar == 'S')
            {
                spillChar = '$';
            }
            else
            {
                spillChar = '*';
            }
            hasDest = true;
        }
    }
    printf("%c N%03u. ", spillChar, tree->gtSeqNum);

    LclVarDsc* varDsc = nullptr;
    unsigned   varNum = UINT_MAX;
    if (tree->IsLocal())
    {
        varNum = tree->AsLclVarCommon()->GetLclNum();
        varDsc = &(compiler->lvaTable[varNum]);
        if (varDsc->lvLRACandidate)
        {
            hasDest = false;
        }
    }
    if (hasDest)
    {
        if (mode == LinearScan::LSRA_DUMP_POST && tree->gtFlags & GTF_SPILLED)
        {
            assert(tree->gtHasReg());
        }
        lsraGetOperandString(tree, mode, operandString, operandStringLength);
        printf("%-15s =", operandString);
    }
    else
    {
        printf("%-15s  ", emptyDestOperand);
    }
    if (varDsc != nullptr)
    {
        if (varDsc->lvLRACandidate)
        {
            if (mode == LSRA_DUMP_REFPOS)
            {
                printf("  V%02u(L%d)", varNum, getIntervalForLocalVar(varDsc->lvVarIndex)->intervalIndex);
            }
            else
            {
                lsraGetOperandString(tree, mode, operandString, operandStringLength);
                printf("  V%02u(%s)", varNum, operandString);
                if (mode == LinearScan::LSRA_DUMP_POST && tree->gtFlags & GTF_SPILLED)
                {
                    printf("R");
                }
            }
        }
        else
        {
            printf("  V%02u MEM", varNum);
        }
    }
    else if (tree->OperIs(GT_ASG))
    {
        assert(!tree->gtHasReg());
        printf("  asg%s  ", GenTree::OpName(tree->OperGet()));
    }
    else
    {
        compiler->gtDispNodeName(tree);
        if (tree->OperKind() & GTK_LEAF)
        {
            compiler->gtDispLeaf(tree, nullptr);
        }
    }
}

//------------------------------------------------------------------------
// DumpOperandDefs: dumps the registers defined by a node.
//
// Arguments:
//    operand - The operand for which to compute a register count.
//
// Returns:
//    The number of registers defined by `operand`.
//
void LinearScan::DumpOperandDefs(
    GenTree* operand, bool& first, LsraTupleDumpMode mode, char* operandString, const unsigned operandStringLength)
{
    assert(operand != nullptr);
    assert(operandString != nullptr);
    if (!operand->IsLIR())
    {
        return;
    }

    int dstCount = ComputeOperandDstCount(operand);

    if (dstCount != 0)
    {
        // This operand directly produces registers; print it.
        if (!first)
        {
            printf(",");
        }
        lsraGetOperandString(operand, mode, operandString, operandStringLength);
        printf("%s", operandString);
        first = false;
    }
    else if (operand->isContained())
    {
        // This is a contained node. Dump the defs produced by its operands.
        for (GenTree* op : operand->Operands())
        {
            DumpOperandDefs(op, first, mode, operandString, operandStringLength);
        }
    }
}

void LinearScan::TupleStyleDump(LsraTupleDumpMode mode)
{
    BasicBlock*    block;
    LsraLocation   currentLoc          = 1; // 0 is the entry
    const unsigned operandStringLength = 6 * MAX_MULTIREG_COUNT + 1;
    char           operandString[operandStringLength];

    // currentRefPosition is not used for LSRA_DUMP_PRE
    // We keep separate iterators for defs, so that we can print them
    // on the lhs of the dump
    RefPositionIterator refPosIterator     = refPositions.begin();
    RefPosition*        currentRefPosition = &refPosIterator;

    switch (mode)
    {
        case LSRA_DUMP_PRE:
            printf("TUPLE STYLE DUMP BEFORE LSRA\n");
            break;
        case LSRA_DUMP_REFPOS:
            printf("TUPLE STYLE DUMP WITH REF POSITIONS\n");
            break;
        case LSRA_DUMP_POST:
            printf("TUPLE STYLE DUMP WITH REGISTER ASSIGNMENTS\n");
            break;
        default:
            printf("ERROR: INVALID TUPLE DUMP MODE\n");
            return;
    }

    if (mode != LSRA_DUMP_PRE)
    {
        printf("Incoming Parameters: ");
        for (; refPosIterator != refPositions.end() && currentRefPosition->refType != RefTypeBB;
             ++refPosIterator, currentRefPosition = &refPosIterator)
        {
            Interval* interval = currentRefPosition->getInterval();
            assert(interval != nullptr && interval->isLocalVar);
            printf(" V%02d", interval->varNum);
            if (mode == LSRA_DUMP_POST)
            {
                regNumber reg;
                if (currentRefPosition->registerAssignment == RBM_NONE)
                {
                    reg = REG_STK;
                }
                else
                {
                    reg = currentRefPosition->assignedReg();
                }
                LclVarDsc* varDsc = &(compiler->lvaTable[interval->varNum]);
                printf("(");
                regNumber assignedReg = varDsc->GetRegNum();
                regNumber argReg      = (varDsc->lvIsRegArg) ? varDsc->GetArgReg() : REG_STK;

                assert(reg == assignedReg || varDsc->lvRegister == false);
                if (reg != argReg)
                {
                    printf(getRegName(argReg, isFloatRegType(interval->registerType)));
                    printf("=>");
                }
                printf("%s)", getRegName(reg, isFloatRegType(interval->registerType)));
            }
        }
        printf("\n");
    }

    for (block = startBlockSequence(); block != nullptr; block = moveToNextBlock())
    {
        currentLoc += 2;

        if (mode == LSRA_DUMP_REFPOS)
        {
            bool printedBlockHeader = false;
            // We should find the boundary RefPositions in the order of exposed uses, dummy defs, and the blocks
            for (; refPosIterator != refPositions.end() &&
                   (currentRefPosition->refType == RefTypeExpUse || currentRefPosition->refType == RefTypeDummyDef ||
                    (currentRefPosition->refType == RefTypeBB && !printedBlockHeader));
                 ++refPosIterator, currentRefPosition = &refPosIterator)
            {
                Interval* interval = nullptr;
                if (currentRefPosition->isIntervalRef())
                {
                    interval = currentRefPosition->getInterval();
                }
                switch (currentRefPosition->refType)
                {
                    case RefTypeExpUse:
                        assert(interval != nullptr);
                        assert(interval->isLocalVar);
                        printf("  Exposed use of V%02u at #%d\n", interval->varNum, currentRefPosition->rpNum);
                        break;
                    case RefTypeDummyDef:
                        assert(interval != nullptr);
                        assert(interval->isLocalVar);
                        printf("  Dummy def of V%02u at #%d\n", interval->varNum, currentRefPosition->rpNum);
                        break;
                    case RefTypeBB:
                        block->dspBlockHeader(compiler);
                        printedBlockHeader = true;
                        printf("=====\n");
                        break;
                    default:
                        printf("Unexpected RefPosition type at #%d\n", currentRefPosition->rpNum);
                        break;
                }
            }
        }
        else
        {
            block->dspBlockHeader(compiler);
            printf("=====\n");
        }
        if (enregisterLocalVars && mode == LSRA_DUMP_POST && block != compiler->fgFirstBB &&
            block->bbNum <= bbNumMaxBeforeResolution)
        {
            printf("Predecessor for variable locations: " FMT_BB "\n", blockInfo[block->bbNum].predBBNum);
            dumpInVarToRegMap(block);
        }
        if (block->bbNum > bbNumMaxBeforeResolution)
        {
            SplitEdgeInfo splitEdgeInfo;
            splitBBNumToTargetBBNumMap->Lookup(block->bbNum, &splitEdgeInfo);
            assert(splitEdgeInfo.toBBNum <= bbNumMaxBeforeResolution);
            assert(splitEdgeInfo.fromBBNum <= bbNumMaxBeforeResolution);
            printf("New block introduced for resolution from " FMT_BB " to " FMT_BB "\n", splitEdgeInfo.fromBBNum,
                   splitEdgeInfo.toBBNum);
        }

        for (GenTree* node : LIR::AsRange(block).NonPhiNodes())
        {
            GenTree* tree = node;

            int produce = tree->IsValue() ? ComputeOperandDstCount(tree) : 0;
            int consume = ComputeAvailableSrcCount(tree);

            lsraDispNode(tree, mode, produce != 0 && mode != LSRA_DUMP_REFPOS);

            if (mode != LSRA_DUMP_REFPOS)
            {
                if (consume > 0)
                {
                    printf("; ");

                    bool first = true;
                    for (GenTree* operand : tree->Operands())
                    {
                        DumpOperandDefs(operand, first, mode, operandString, operandStringLength);
                    }
                }
            }
            else
            {
                // Print each RefPosition on a new line, but
                // printing all the kills for each node on a single line
                // and combining the fixed regs with their associated def or use
                bool         killPrinted        = false;
                RefPosition* lastFixedRegRefPos = nullptr;
                for (; refPosIterator != refPositions.end() &&
                       (currentRefPosition->refType == RefTypeUse || currentRefPosition->refType == RefTypeFixedReg ||
                        currentRefPosition->refType == RefTypeKill || currentRefPosition->refType == RefTypeDef) &&
                       (currentRefPosition->nodeLocation == tree->gtSeqNum ||
                        currentRefPosition->nodeLocation == tree->gtSeqNum + 1);
                     ++refPosIterator, currentRefPosition = &refPosIterator)
                {
                    Interval* interval = nullptr;
                    if (currentRefPosition->isIntervalRef())
                    {
                        interval = currentRefPosition->getInterval();
                    }
                    switch (currentRefPosition->refType)
                    {
                        case RefTypeUse:
                            if (currentRefPosition->IsPhysRegRef())
                            {
                                printf("\n                               Use:R%d(#%d)",
                                       currentRefPosition->getReg()->regNum, currentRefPosition->rpNum);
                            }
                            else
                            {
                                assert(interval != nullptr);
                                printf("\n                               Use:");
                                interval->microDump();
                                printf("(#%d)", currentRefPosition->rpNum);
                                if (currentRefPosition->isFixedRegRef && !interval->isInternal)
                                {
                                    assert(genMaxOneBit(currentRefPosition->registerAssignment));
                                    assert(lastFixedRegRefPos != nullptr);
                                    printf(" Fixed:%s(#%d)", getRegName(currentRefPosition->assignedReg(),
                                                                        isFloatRegType(interval->registerType)),
                                           lastFixedRegRefPos->rpNum);
                                    lastFixedRegRefPos = nullptr;
                                }
                                if (currentRefPosition->isLocalDefUse)
                                {
                                    printf(" LocalDefUse");
                                }
                                if (currentRefPosition->lastUse)
                                {
                                    printf(" *");
                                }
                            }
                            break;
                        case RefTypeDef:
                        {
                            // Print each def on a new line
                            assert(interval != nullptr);
                            printf("\n        Def:");
                            interval->microDump();
                            printf("(#%d)", currentRefPosition->rpNum);
                            if (currentRefPosition->isFixedRegRef)
                            {
                                assert(genMaxOneBit(currentRefPosition->registerAssignment));
                                printf(" %s", getRegName(currentRefPosition->assignedReg(),
                                                         isFloatRegType(interval->registerType)));
                            }
                            if (currentRefPosition->isLocalDefUse)
                            {
                                printf(" LocalDefUse");
                            }
                            if (currentRefPosition->lastUse)
                            {
                                printf(" *");
                            }
                            if (interval->relatedInterval != nullptr)
                            {
                                printf(" Pref:");
                                interval->relatedInterval->microDump();
                            }
                        }
                        break;
                        case RefTypeKill:
                            if (!killPrinted)
                            {
                                printf("\n        Kill: ");
                                killPrinted = true;
                            }
                            printf(getRegName(currentRefPosition->assignedReg(),
                                              isFloatRegType(currentRefPosition->getReg()->registerType)));
                            printf(" ");
                            break;
                        case RefTypeFixedReg:
                            lastFixedRegRefPos = currentRefPosition;
                            break;
                        default:
                            printf("Unexpected RefPosition type at #%d\n", currentRefPosition->rpNum);
                            break;
                    }
                }
            }
            printf("\n");
        }
        if (enregisterLocalVars && mode == LSRA_DUMP_POST)
        {
            dumpOutVarToRegMap(block);
        }
        printf("\n");
    }
    printf("\n\n");
}

void LinearScan::dumpLsraAllocationEvent(
    LsraDumpEvent event, Interval* interval, regNumber reg, BasicBlock* currentBlock, RegisterScore registerScore)
{
    if (!(VERBOSE))
    {
        return;
    }
    if ((interval != nullptr) && (reg != REG_NA) && (reg != REG_STK))
    {
        registersToDump |= getRegMask(reg, interval->registerType);
        dumpRegRecordTitleIfNeeded();
    }

    switch (event)
    {
        // Conflicting def/use
        case LSRA_EVENT_DEFUSE_CONFLICT:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("DUconflict ");
            dumpRegRecords();
            break;
        case LSRA_EVENT_DEFUSE_CASE1:
            printf(indentFormat, "  Case #1 use defRegAssignment");
            dumpRegRecords();
            break;
        case LSRA_EVENT_DEFUSE_CASE2:
            printf(indentFormat, "  Case #2 use useRegAssignment");
            dumpRegRecords();
            break;
        case LSRA_EVENT_DEFUSE_CASE3:
            printf(indentFormat, "  Case #3 use useRegAssignment");
            dumpRegRecords();
            dumpRegRecords();
            break;
        case LSRA_EVENT_DEFUSE_CASE4:
            printf(indentFormat, "  Case #4 use defRegAssignment");
            dumpRegRecords();
            break;
        case LSRA_EVENT_DEFUSE_CASE5:
            printf(indentFormat, "  Case #5 set def to all regs");
            dumpRegRecords();
            break;
        case LSRA_EVENT_DEFUSE_CASE6:
            printf(indentFormat, "  Case #6 need a copy");
            dumpRegRecords();
            if (interval == nullptr)
            {
                printf(indentFormat, "    NULL interval");
                dumpRegRecords();
            }
            else if (interval->firstRefPosition->multiRegIdx != 0)
            {
                printf(indentFormat, "    (multiReg)");
                dumpRegRecords();
            }
            break;

        case LSRA_EVENT_SPILL:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            assert(interval != nullptr && interval->assignedReg != nullptr);
            printf("Spill    %-4s ", getRegName(interval->assignedReg->regNum));
            dumpRegRecords();
            break;

        // Restoring the previous register
        case LSRA_EVENT_RESTORE_PREVIOUS_INTERVAL:
        case LSRA_EVENT_RESTORE_PREVIOUS_INTERVAL_AFTER_SPILL:
            assert(interval != nullptr);
            if ((activeRefPosition == nullptr) || (activeRefPosition->refType == RefTypeBB))
            {
                printf(emptyRefPositionFormat, "");
            }
            else
            {
                dumpRefPositionShort(activeRefPosition, currentBlock);
            }
            printf((event == LSRA_EVENT_RESTORE_PREVIOUS_INTERVAL) ? "Restr    %-4s " : "SRstr    %-4s ",
                   getRegName(reg));
            dumpRegRecords();
            break;

        case LSRA_EVENT_DONE_KILL_GC_REFS:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("Done       ");
            break;

        case LSRA_EVENT_NO_GC_KILLS:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("None       ");
            break;

        // Block boundaries
        case LSRA_EVENT_START_BB:
            // The RefTypeBB comes after the RefTypeDummyDefs associated with that block,
            // so we may have a RefTypeDummyDef at the time we dump this event.
            // In that case we'll have another "EVENT" associated with it, so we need to
            // print the full line now.
            if (activeRefPosition->refType != RefTypeBB)
            {
                dumpNewBlock(currentBlock, activeRefPosition->nodeLocation);
                dumpRegRecords();
            }
            else
            {
                dumpRefPositionShort(activeRefPosition, currentBlock);
            }
            break;

        // Allocation decisions
        case LSRA_EVENT_NEEDS_NEW_REG:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("Free  %-4s ", getRegName(reg));
            dumpRegRecords();
            break;

        case LSRA_EVENT_ZERO_REF:
            assert(interval != nullptr && interval->isLocalVar);
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("NoRef      ");
            dumpRegRecords();
            break;

        case LSRA_EVENT_FIXED_REG:
        case LSRA_EVENT_EXP_USE:
        case LSRA_EVENT_KEPT_ALLOCATION:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("Keep     %-4s ", getRegName(reg));
            break;

        case LSRA_EVENT_COPY_REG:
            assert(interval != nullptr && interval->recentRefPosition != nullptr);
            dumpRefPositionShort(activeRefPosition, currentBlock);
            if (allocationPassComplete || (registerScore == 0))
            {
                printf("Copy     %-4s ", getRegName(reg));
            }
            else
            {
                printf("%-5s(C) %-4s ", getScoreName(registerScore), getRegName(reg));
            }
            break;

        case LSRA_EVENT_MOVE_REG:
            assert(interval != nullptr && interval->recentRefPosition != nullptr);
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("Move     %-4s ", getRegName(reg));
            dumpRegRecords();
            break;

        case LSRA_EVENT_ALLOC_REG:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            if (allocationPassComplete || (registerScore == 0))
            {
                printf("Alloc    %-4s ", getRegName(reg));
            }
            else
            {
                printf("%-5s(A) %-4s ", getScoreName(registerScore), getRegName(reg));
            }

            break;

        case LSRA_EVENT_REUSE_REG:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            if (allocationPassComplete || (registerScore == 0))
            {
                printf("Reuse     %-4s ", getRegName(reg));
            }
            else
            {
                printf("%-5s(A) %-4s ", getScoreName(registerScore), getRegName(reg));
            }
            break;

        case LSRA_EVENT_NO_ENTRY_REG_ALLOCATED:
            assert(interval != nullptr && interval->isLocalVar);
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("LoRef         ");
            break;

        case LSRA_EVENT_NO_REG_ALLOCATED:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("NoReg         ");
            break;

        case LSRA_EVENT_RELOAD:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("ReLod    %-4s ", getRegName(reg));
            dumpRegRecords();
            break;

        case LSRA_EVENT_SPECIAL_PUTARG:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("PtArg    %-4s ", getRegName(reg));
            break;

        case LSRA_EVENT_UPPER_VECTOR_SAVE:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("UVSav    %-4s ", getRegName(reg));
            break;

        case LSRA_EVENT_UPPER_VECTOR_RESTORE:
            dumpRefPositionShort(activeRefPosition, currentBlock);
            printf("UVRes    %-4s ", getRegName(reg));
            break;

        // We currently don't dump anything for these events.
        case LSRA_EVENT_DEFUSE_FIXED_DELAY_USE:
        case LSRA_EVENT_SPILL_EXTENDED_LIFETIME:
        case LSRA_EVENT_END_BB:
        case LSRA_EVENT_FREE_REGS:
        case LSRA_EVENT_INCREMENT_RANGE_END:
        case LSRA_EVENT_LAST_USE:
        case LSRA_EVENT_LAST_USE_DELAYED:
            break;

        default:
            printf("?????    %-4s ", getRegName(reg));
            dumpRegRecords();
            break;
    }
}

//------------------------------------------------------------------------
// dumpRegRecordHeader: Dump the header for a column-based dump of the register state.
//
// Arguments:
//    None.
//
// Return Value:
//    None.
//
// Assumptions:
//    Reg names fit in 4 characters (minimum width of the columns)
//
// Notes:
//    In order to make the table as dense as possible (for ease of reading the dumps),
//    we determine the minimum regColumnWidth width required to represent:
//      regs, by name (e.g. eax or xmm0) - this is fixed at 4 characters.
//      intervals, as Vnn for lclVar intervals, or as I<num> for other intervals.
//    The table is indented by the amount needed for dumpRefPositionShort, which is
//    captured in shortRefPositionDumpWidth.
//
void LinearScan::dumpRegRecordHeader()
{
    printf("The following table has one or more rows for each RefPosition that is handled during allocation.\n"
           "The first column provides the basic information about the RefPosition, with its type (e.g. Def,\n"
           "Use, Fixd) followed by a '*' if it is a last use, and a 'D' if it is delayRegFree, and then the\n"
           "action taken during allocation (e.g. Alloc a new register, or Keep an existing one).\n"
           "The subsequent columns show the Interval occupying each register, if any, followed by 'a' if it is\n"
           "active, a 'p' if it is a large vector that has been partially spilled, and 'i'if it is inactive.\n"
           "Columns are only printed up to the last modifed register, which may increase during allocation,\n"
           "in which case additional columns will appear.  \n"
           "Registers which are not marked modified have ---- in their column.\n\n");

    // First, determine the width of each register column (which holds a reg name in the
    // header, and an interval name in each subsequent row).
    int intervalNumberWidth = (int)log10((double)intervals.size()) + 1;
    // The regColumnWidth includes the identifying character (I or V) and an 'i', 'p' or 'a' (inactive,
    // partially-spilled or active)
    regColumnWidth = intervalNumberWidth + 2;
    if (regColumnWidth < 4)
    {
        regColumnWidth = 4;
    }
    sprintf_s(intervalNameFormat, MAX_FORMAT_CHARS, "%%c%%-%dd", regColumnWidth - 2);
    sprintf_s(regNameFormat, MAX_FORMAT_CHARS, "%%-%ds", regColumnWidth);

    // Next, determine the width of the short RefPosition (see dumpRefPositionShort()).
    // This is in the form:
    // nnn.#mmm NAME TYPEld
    // Where:
    //    nnn is the Location, right-justified to the width needed for the highest location.
    //    mmm is the RefPosition rpNum, left-justified to the width needed for the highest rpNum.
    //    NAME is dumped by dumpReferentName(), and is "regColumnWidth".
    //    TYPE is RefTypeNameShort, and is 4 characters
    //    l is either '*' (if a last use) or ' ' (otherwise)
    //    d is either 'D' (if a delayed use) or ' ' (otherwise)

    maxNodeLocation = (maxNodeLocation == 0)
                          ? 1
                          : maxNodeLocation; // corner case of a method with an infinite loop without any gentree nodes
    assert(maxNodeLocation >= 1);
    assert(refPositions.size() >= 1);
    int nodeLocationWidth         = (int)log10((double)maxNodeLocation) + 1;
    int refPositionWidth          = (int)log10((double)refPositions.size()) + 1;
    int refTypeInfoWidth          = 4 /*TYPE*/ + 2 /* last-use and delayed */ + 1 /* space */;
    int locationAndRPNumWidth     = nodeLocationWidth + 2 /* .# */ + refPositionWidth + 1 /* space */;
    int shortRefPositionDumpWidth = locationAndRPNumWidth + regColumnWidth + 1 /* space */ + refTypeInfoWidth;
    sprintf_s(shortRefPositionFormat, MAX_FORMAT_CHARS, "%%%dd.#%%-%dd ", nodeLocationWidth, refPositionWidth);
    sprintf_s(emptyRefPositionFormat, MAX_FORMAT_CHARS, "%%-%ds", shortRefPositionDumpWidth);

    // The width of the "allocation info"
    //  - a 8-character allocation decision
    //  - a space
    //  - a 4-character register
    //  - a space
    int allocationInfoWidth = 8 + 1 + 4 + 1;

    // Next, determine the width of the legend for each row.  This includes:
    //  - a short RefPosition dump (shortRefPositionDumpWidth), which includes a space
    //  - the allocation info (allocationInfoWidth), which also includes a space

    regTableIndent = shortRefPositionDumpWidth + allocationInfoWidth;

    // BBnn printed left-justified in the NAME Typeld and allocationInfo space.
    int bbNumWidth = (int)log10((double)compiler->fgBBNumMax) + 1;
    // In the unlikely event that BB numbers overflow the space, we'll simply omit the predBB
    int predBBNumDumpSpace = regTableIndent - locationAndRPNumWidth - bbNumWidth - 9; // 'BB' + ' PredBB'
    if (predBBNumDumpSpace < bbNumWidth)
    {
        sprintf_s(bbRefPosFormat, MAX_LEGEND_FORMAT_CHARS, "BB%%-%dd", shortRefPositionDumpWidth - 2);
    }
    else
    {
        sprintf_s(bbRefPosFormat, MAX_LEGEND_FORMAT_CHARS, "BB%%-%dd PredBB%%-%dd", bbNumWidth, predBBNumDumpSpace);
    }

    if (compiler->shouldDumpASCIITrees())
    {
        columnSeparator = "|";
        line            = "-";
        leftBox         = "+";
        middleBox       = "+";
        rightBox        = "+";
    }
    else
    {
        columnSeparator = "\xe2\x94\x82";
        line            = "\xe2\x94\x80";
        leftBox         = "\xe2\x94\x9c";
        middleBox       = "\xe2\x94\xbc";
        rightBox        = "\xe2\x94\xa4";
    }
    sprintf_s(indentFormat, MAX_FORMAT_CHARS, "%%-%ds", regTableIndent);

    // Now, set up the legend format for the RefPosition info
    sprintf_s(legendFormat, MAX_LEGEND_FORMAT_CHARS, "%%-%d.%ds%%-%d.%ds%%-%ds%%s", nodeLocationWidth + 1,
              nodeLocationWidth + 1, refPositionWidth + 2, refPositionWidth + 2, regColumnWidth + 1);

    // Print a "title row" including the legend and the reg names.
    lastDumpedRegisters = RBM_NONE;
    dumpRegRecordTitleIfNeeded();
}

void LinearScan::dumpRegRecordTitleIfNeeded()
{
    if ((lastDumpedRegisters != registersToDump) || (rowCountSinceLastTitle > MAX_ROWS_BETWEEN_TITLES))
    {
        lastUsedRegNumIndex = 0;
        int lastRegNumIndex = compiler->compFloatingPointUsed ? REG_FP_LAST : REG_INT_LAST;
        for (int regNumIndex = 0; regNumIndex <= lastRegNumIndex; regNumIndex++)
        {
            if ((registersToDump & genRegMask((regNumber)regNumIndex)) != 0)
            {
                lastUsedRegNumIndex = regNumIndex;
            }
        }
        dumpRegRecordTitle();
        lastDumpedRegisters = registersToDump;
    }
}

void LinearScan::dumpRegRecordTitleLines()
{
    for (int i = 0; i < regTableIndent; i++)
    {
        printf("%s", line);
    }
    for (int regNumIndex = 0; regNumIndex <= lastUsedRegNumIndex; regNumIndex++)
    {
        regNumber regNum = (regNumber)regNumIndex;
        if (shouldDumpReg(regNum))
        {
            printf("%s", middleBox);
            for (int i = 0; i < regColumnWidth; i++)
            {
                printf("%s", line);
            }
        }
    }
    printf("%s\n", rightBox);
}
void LinearScan::dumpRegRecordTitle()
{
    dumpRegRecordTitleLines();

    // Print out the legend for the RefPosition info
    printf(legendFormat, "Loc ", "RP# ", "Name ", "Type  Action    Reg  ");

    // Print out the register name column headers
    char columnFormatArray[MAX_FORMAT_CHARS];
    sprintf_s(columnFormatArray, MAX_FORMAT_CHARS, "%s%%-%d.%ds", columnSeparator, regColumnWidth, regColumnWidth);
    for (int regNumIndex = 0; regNumIndex <= lastUsedRegNumIndex; regNumIndex++)
    {
        regNumber regNum = (regNumber)regNumIndex;
        if (shouldDumpReg(regNum))
        {
            const char* regName = getRegName(regNum);
            printf(columnFormatArray, regName);
        }
    }
    printf("%s\n", columnSeparator);

    rowCountSinceLastTitle = 0;

    dumpRegRecordTitleLines();
}

void LinearScan::dumpRegRecords()
{
    static char columnFormatArray[18];

    for (regNumber regNum = REG_FIRST; regNum <= (regNumber)lastUsedRegNumIndex; regNum = REG_NEXT(regNum))
    {
        if (shouldDumpReg(regNum))
        {
            printf("%s", columnSeparator);
            RegRecord& regRecord = physRegs[regNum];
            Interval*  interval  = regRecord.assignedInterval;
            if (interval != nullptr)
            {
                dumpIntervalName(interval);
                char activeChar = interval->isActive ? 'a' : 'i';
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                if (interval->isPartiallySpilled)
                {
                    activeChar = 'p';
                }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                printf("%c", activeChar);
            }
            else if ((genRegMask(regNum) & regsBusyUntilKill) != RBM_NONE)
            {
                printf(columnFormatArray, "Busy");
            }
            else
            {
                sprintf_s(columnFormatArray, MAX_FORMAT_CHARS, "%%-%ds", regColumnWidth);
                printf(columnFormatArray, "");
            }
        }
    }
    printf("%s\n", columnSeparator);
    rowCountSinceLastTitle++;
}

void LinearScan::dumpIntervalName(Interval* interval)
{
    if (interval->isLocalVar)
    {
        printf(intervalNameFormat, 'V', interval->varNum);
    }
    else if (interval->IsUpperVector())
    {
        printf(intervalNameFormat, 'U', interval->relatedInterval->varNum);
    }
    else if (interval->isConstant)
    {
        printf(intervalNameFormat, 'C', interval->intervalIndex);
    }
    else
    {
        printf(intervalNameFormat, 'I', interval->intervalIndex);
    }
}

void LinearScan::dumpEmptyRefPosition()
{
    printf(emptyRefPositionFormat, "");
}

//------------------------------------------------------------------------
// dumpNewBlock: Dump a line for a new block in a column-based dump of the register state.
//
// Arguments:
//    currentBlock - the new block to be dumped
//
void LinearScan::dumpNewBlock(BasicBlock* currentBlock, LsraLocation location)
{
    if (!VERBOSE)
    {
        return;
    }

    // Always print a title row before a RefTypeBB (except for the first, because we
    // will already have printed it before the parameters)
    if ((currentBlock != compiler->fgFirstBB) && (currentBlock != nullptr))
    {
        dumpRegRecordTitle();
    }
    // If the activeRefPosition is a DummyDef, then don't print anything further (printing the
    // title line makes it clearer that we're "about to" start the next block).
    if (activeRefPosition->refType == RefTypeDummyDef)
    {
        dumpEmptyRefPosition();
        printf("DDefs ");
        printf(regNameFormat, "");
        return;
    }
    printf(shortRefPositionFormat, location, activeRefPosition->rpNum);
    if (currentBlock == nullptr)
    {
        printf(regNameFormat, "END");
        printf("                 ");
        printf(regNameFormat, "");
    }
    else
    {
        printf(bbRefPosFormat, currentBlock->bbNum,
               currentBlock == compiler->fgFirstBB ? 0 : blockInfo[currentBlock->bbNum].predBBNum);
    }
}

// Note that the size of this dump is computed in dumpRegRecordHeader().
//
void LinearScan::dumpRefPositionShort(RefPosition* refPosition, BasicBlock* currentBlock)
{
    static RefPosition* lastPrintedRefPosition = nullptr;
    if (refPosition == lastPrintedRefPosition)
    {
        dumpEmptyRefPosition();
        return;
    }
    lastPrintedRefPosition = refPosition;
    if (refPosition->refType == RefTypeBB)
    {
        dumpNewBlock(currentBlock, refPosition->nodeLocation);
        return;
    }
    printf(shortRefPositionFormat, refPosition->nodeLocation, refPosition->rpNum);
    if (refPosition->isIntervalRef())
    {
        Interval* interval = refPosition->getInterval();
        dumpIntervalName(interval);
        char lastUseChar = ' ';
        char delayChar   = ' ';
        if (refPosition->lastUse)
        {
            lastUseChar = '*';
            if (refPosition->delayRegFree)
            {
                delayChar = 'D';
            }
        }
        printf("  %s%c%c ", getRefTypeShortName(refPosition->refType), lastUseChar, delayChar);
    }
    else if (refPosition->IsPhysRegRef())
    {
        RegRecord* regRecord = refPosition->getReg();
        printf(regNameFormat, getRegName(regRecord->regNum));
        printf(" %s   ", getRefTypeShortName(refPosition->refType));
    }
    else
    {
        assert(refPosition->refType == RefTypeKillGCRefs);
        // There's no interval or reg name associated with this.
        printf(regNameFormat, "   ");
        printf(" %s   ", getRefTypeShortName(refPosition->refType));
    }
}

//------------------------------------------------------------------------
// LinearScan::IsResolutionMove:
//     Returns true if the given node is a move inserted by LSRA
//     resolution.
//
// Arguments:
//     node - the node to check.
//
bool LinearScan::IsResolutionMove(GenTree* node)
{
    if (!IsLsraAdded(node))
    {
        return false;
    }

    switch (node->OperGet())
    {
        case GT_LCL_VAR:
        case GT_COPY:
            return node->IsUnusedValue();

        case GT_SWAP:
            return true;

        default:
            return false;
    }
}

//------------------------------------------------------------------------
// LinearScan::IsResolutionNode:
//     Returns true if the given node is either a move inserted by LSRA
//     resolution or an operand to such a move.
//
// Arguments:
//     containingRange - the range that contains the node to check.
//     node - the node to check.
//
bool LinearScan::IsResolutionNode(LIR::Range& containingRange, GenTree* node)
{
    for (;;)
    {
        if (IsResolutionMove(node))
        {
            return true;
        }

        if (!IsLsraAdded(node) || (node->OperGet() != GT_LCL_VAR))
        {
            return false;
        }

        LIR::Use use;
        bool     foundUse = containingRange.TryGetUse(node, &use);
        assert(foundUse);

        node = use.User();
    }
}

//------------------------------------------------------------------------
// verifyFinalAllocation: Traverse the RefPositions and verify various invariants.
//
// Arguments:
//    None.
//
// Return Value:
//    None.
//
// Notes:
//    If verbose is set, this will also dump a table of the final allocations.
void LinearScan::verifyFinalAllocation()
{
    if (VERBOSE)
    {
        printf("\nFinal allocation\n");
    }

    // Clear register assignments.
    for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
    {
        RegRecord* physRegRecord        = getRegisterRecord(reg);
        physRegRecord->assignedInterval = nullptr;
    }

    for (Interval& interval : intervals)
    {
        interval.assignedReg = nullptr;
        interval.physReg     = REG_NA;
    }

    DBEXEC(VERBOSE, dumpRegRecordTitle());

    BasicBlock*  currentBlock                = nullptr;
    GenTree*     firstBlockEndResolutionNode = nullptr;
    LsraLocation currentLocation             = MinLocation;
    for (RefPosition& refPosition : refPositions)
    {
        RefPosition* currentRefPosition = &refPosition;
        Interval*    interval           = nullptr;
        RegRecord*   regRecord          = nullptr;
        regNumber    regNum             = REG_NA;
        activeRefPosition               = currentRefPosition;

        if (currentRefPosition->refType != RefTypeBB)
        {
            if (currentRefPosition->IsPhysRegRef())
            {
                regRecord                    = currentRefPosition->getReg();
                regRecord->recentRefPosition = currentRefPosition;
                regNum                       = regRecord->regNum;
            }
            else if (currentRefPosition->isIntervalRef())
            {
                interval                    = currentRefPosition->getInterval();
                interval->recentRefPosition = currentRefPosition;
                if (currentRefPosition->registerAssignment != RBM_NONE)
                {
                    if (!genMaxOneBit(currentRefPosition->registerAssignment))
                    {
                        assert(currentRefPosition->refType == RefTypeExpUse ||
                               currentRefPosition->refType == RefTypeDummyDef);
                    }
                    else
                    {
                        regNum    = currentRefPosition->assignedReg();
                        regRecord = getRegisterRecord(regNum);
                    }
                }
            }
        }

        LsraLocation newLocation = currentRefPosition->nodeLocation;
        currentLocation          = newLocation;

        switch (currentRefPosition->refType)
        {
            case RefTypeBB:
            {
                if (currentBlock == nullptr)
                {
                    currentBlock = startBlockSequence();
                }
                else
                {
                    // Verify the resolution moves at the end of the previous block.
                    for (GenTree* node = firstBlockEndResolutionNode; node != nullptr; node = node->gtNext)
                    {
                        assert(enregisterLocalVars);
                        // Only verify nodes that are actually moves; don't bother with the nodes that are
                        // operands to moves.
                        if (IsResolutionMove(node))
                        {
                            verifyResolutionMove(node, currentLocation);
                        }
                    }

                    // Validate the locations at the end of the previous block.
                    if (enregisterLocalVars)
                    {
                        VarToRegMap     outVarToRegMap = outVarToRegMaps[currentBlock->bbNum];
                        VarSetOps::Iter iter(compiler, currentBlock->bbLiveOut);
                        unsigned        varIndex = 0;
                        while (iter.NextElem(&varIndex))
                        {
                            if (localVarIntervals[varIndex] == nullptr)
                            {
                                assert(!compiler->lvaGetDescByTrackedIndex(varIndex)->lvLRACandidate);
                                continue;
                            }
                            regNumber regNum = getVarReg(outVarToRegMap, varIndex);
                            interval         = getIntervalForLocalVar(varIndex);
                            if (interval->physReg != regNum)
                            {
                                assert(regNum == REG_STK);
                                assert((interval->physReg == REG_NA) || interval->isWriteThru);
                            }
                            interval->physReg     = REG_NA;
                            interval->assignedReg = nullptr;
                            interval->isActive    = false;
                        }
                    }

                    // Clear register assignments.
                    for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
                    {
                        RegRecord* physRegRecord        = getRegisterRecord(reg);
                        physRegRecord->assignedInterval = nullptr;
                    }

                    // Now, record the locations at the beginning of this block.
                    currentBlock = moveToNextBlock();
                }

                if (currentBlock != nullptr)
                {
                    if (enregisterLocalVars)
                    {
                        VarToRegMap     inVarToRegMap = inVarToRegMaps[currentBlock->bbNum];
                        VarSetOps::Iter iter(compiler, currentBlock->bbLiveIn);
                        unsigned        varIndex = 0;
                        while (iter.NextElem(&varIndex))
                        {
                            if (localVarIntervals[varIndex] == nullptr)
                            {
                                assert(!compiler->lvaGetDescByTrackedIndex(varIndex)->lvLRACandidate);
                                continue;
                            }
                            regNumber regNum                  = getVarReg(inVarToRegMap, varIndex);
                            interval                          = getIntervalForLocalVar(varIndex);
                            interval->physReg                 = regNum;
                            interval->assignedReg             = &(physRegs[regNum]);
                            interval->isActive                = true;
                            physRegs[regNum].assignedInterval = interval;
                        }
                    }

                    if (VERBOSE)
                    {
                        dumpRefPositionShort(currentRefPosition, currentBlock);
                        dumpRegRecords();
                    }

                    // Finally, handle the resolution moves, if any, at the beginning of the next block.
                    firstBlockEndResolutionNode = nullptr;
                    bool foundNonResolutionNode = false;

                    LIR::Range& currentBlockRange = LIR::AsRange(currentBlock);
                    for (GenTree* node : currentBlockRange.NonPhiNodes())
                    {
                        if (IsResolutionNode(currentBlockRange, node))
                        {
                            assert(enregisterLocalVars);
                            if (foundNonResolutionNode)
                            {
                                firstBlockEndResolutionNode = node;
                                break;
                            }
                            else if (IsResolutionMove(node))
                            {
                                // Only verify nodes that are actually moves; don't bother with the nodes that are
                                // operands to moves.
                                verifyResolutionMove(node, currentLocation);
                            }
                        }
                        else
                        {
                            foundNonResolutionNode = true;
                        }
                    }
                }
            }

            break;

            case RefTypeKill:
                assert(regRecord != nullptr);
                assert(regRecord->assignedInterval == nullptr);
                dumpLsraAllocationEvent(LSRA_EVENT_KEPT_ALLOCATION, nullptr, regRecord->regNum, currentBlock);
                break;
            case RefTypeFixedReg:
                assert(regRecord != nullptr);
                dumpLsraAllocationEvent(LSRA_EVENT_KEPT_ALLOCATION, nullptr, regRecord->regNum, currentBlock);
                break;

            case RefTypeUpperVectorSave:
                dumpLsraAllocationEvent(LSRA_EVENT_UPPER_VECTOR_SAVE, nullptr, REG_NA, currentBlock);
                break;

            case RefTypeUpperVectorRestore:
                dumpLsraAllocationEvent(LSRA_EVENT_UPPER_VECTOR_RESTORE, nullptr, REG_NA, currentBlock);
                break;

            case RefTypeDef:
            case RefTypeUse:
            case RefTypeParamDef:
            case RefTypeZeroInit:
                assert(interval != nullptr);

                if (interval->isSpecialPutArg)
                {
                    dumpLsraAllocationEvent(LSRA_EVENT_SPECIAL_PUTARG, interval, regNum);
                    break;
                }
                if (currentRefPosition->reload)
                {
                    interval->isActive = true;
                    assert(regNum != REG_NA);
                    interval->physReg           = regNum;
                    interval->assignedReg       = regRecord;
                    regRecord->assignedInterval = interval;
                    dumpLsraAllocationEvent(LSRA_EVENT_RELOAD, nullptr, regRecord->regNum, currentBlock);
                }
                if (regNum == REG_NA)
                {
                    // If this interval is still assigned to a register
                    if (interval->physReg != REG_NA)
                    {
                        // then unassign it if no new register was assigned to the RefTypeDef
                        if (RefTypeIsDef(currentRefPosition->refType))
                        {
                            assert(interval->assignedReg != nullptr);
                            if (interval->assignedReg->assignedInterval == interval)
                            {
                                interval->assignedReg->assignedInterval = nullptr;
                            }
                            interval->physReg     = REG_NA;
                            interval->assignedReg = nullptr;
                        }
                    }

                    dumpLsraAllocationEvent(LSRA_EVENT_NO_REG_ALLOCATED, interval);
                }
                else if (RefTypeIsDef(currentRefPosition->refType))
                {
                    interval->isActive = true;

                    if (VERBOSE)
                    {
                        if (interval->isConstant && (currentRefPosition->treeNode != nullptr) &&
                            currentRefPosition->treeNode->IsReuseRegVal())
                        {
                            dumpLsraAllocationEvent(LSRA_EVENT_REUSE_REG, nullptr, regRecord->regNum, currentBlock);
                        }
                        else
                        {
                            dumpLsraAllocationEvent(LSRA_EVENT_ALLOC_REG, nullptr, regRecord->regNum, currentBlock);
                        }
                    }
                }
                else if (currentRefPosition->copyReg)
                {
                    dumpLsraAllocationEvent(LSRA_EVENT_COPY_REG, interval, regRecord->regNum, currentBlock);
                }
                else if (currentRefPosition->moveReg)
                {
                    assert(interval->assignedReg != nullptr);
                    interval->assignedReg->assignedInterval = nullptr;
                    interval->physReg                       = regNum;
                    interval->assignedReg                   = regRecord;
                    regRecord->assignedInterval             = interval;
                    if (VERBOSE)
                    {
                        dumpEmptyRefPosition();
                        printf("Move     %-4s ", getRegName(regRecord->regNum));
                    }
                }
                else
                {
                    dumpLsraAllocationEvent(LSRA_EVENT_KEPT_ALLOCATION, nullptr, regRecord->regNum, currentBlock);
                }
                if (currentRefPosition->lastUse || (currentRefPosition->spillAfter && !currentRefPosition->writeThru))
                {
                    interval->isActive = false;
                }
                if (regNum != REG_NA)
                {
                    if (currentRefPosition->spillAfter)
                    {
                        if (VERBOSE)
                        {
                            // If refPos is marked as copyReg, then the reg that is spilled
                            // is the homeReg of the interval not the reg currently assigned
                            // to refPos.
                            regNumber spillReg = regNum;
                            if (currentRefPosition->copyReg)
                            {
                                assert(interval != nullptr);
                                spillReg = interval->physReg;
                            }
                            dumpRegRecords();
                            dumpEmptyRefPosition();
                            if (currentRefPosition->writeThru)
                            {
                                printf("WThru    %-4s ", getRegName(spillReg));
                            }
                            else
                            {
                                printf("Spill    %-4s ", getRegName(spillReg));
                            }
                        }
                    }
                    else if (currentRefPosition->copyReg)
                    {
                        regRecord->assignedInterval = interval;
                    }
                    else
                    {
                        if (RefTypeIsDef(currentRefPosition->refType))
                        {
                            // Interval was assigned to a different register.
                            // Clear the assigned interval of current register.
                            if (interval->physReg != REG_NA && interval->physReg != regNum)
                            {
                                interval->assignedReg->assignedInterval = nullptr;
                            }
                        }
                        interval->physReg           = regNum;
                        interval->assignedReg       = regRecord;
                        regRecord->assignedInterval = interval;
                    }
                }
                break;
            case RefTypeKillGCRefs:
                // No action to take.
                // However, we will assert that, at resolution time, no registers contain GC refs.
                {
                    DBEXEC(VERBOSE, printf("           "));
                    regMaskTP candidateRegs = currentRefPosition->registerAssignment;
                    while (candidateRegs != RBM_NONE)
                    {
                        regMaskTP nextRegBit = genFindLowestBit(candidateRegs);
                        candidateRegs &= ~nextRegBit;
                        regNumber  nextReg          = genRegNumFromMask(nextRegBit);
                        RegRecord* regRecord        = getRegisterRecord(nextReg);
                        Interval*  assignedInterval = regRecord->assignedInterval;
                        assert(assignedInterval == nullptr || !varTypeIsGC(assignedInterval->registerType));
                    }
                }
                break;

            case RefTypeExpUse:
            case RefTypeDummyDef:
                // Do nothing; these will be handled by the RefTypeBB.
                DBEXEC(VERBOSE, dumpRefPositionShort(currentRefPosition, currentBlock));
                DBEXEC(VERBOSE, printf("              "));
                break;

            case RefTypeInvalid:
                // for these 'currentRefPosition->refType' values, No action to take
                break;
        }

        if (currentRefPosition->refType != RefTypeBB)
        {
            DBEXEC(VERBOSE, dumpRegRecords());
            if (interval != nullptr)
            {
                if (currentRefPosition->copyReg)
                {
                    assert(interval->physReg != regNum);
                    regRecord->assignedInterval = nullptr;
                    assert(interval->assignedReg != nullptr);
                    regRecord = interval->assignedReg;
                }
                if (currentRefPosition->spillAfter || currentRefPosition->lastUse)
                {
                    assert(!currentRefPosition->spillAfter || currentRefPosition->IsActualRef());

                    if (RefTypeIsDef(currentRefPosition->refType))
                    {
                        // If an interval got assigned to a different register (while the different
                        // register got spilled), then clear the assigned interval of current register.
                        if (interval->physReg != REG_NA && interval->physReg != regNum)
                        {
                            interval->assignedReg->assignedInterval = nullptr;
                        }
                    }

                    interval->physReg     = REG_NA;
                    interval->assignedReg = nullptr;

                    // regRegcord could be null if the RefPosition does not require a register.
                    if (regRecord != nullptr)
                    {
                        regRecord->assignedInterval = nullptr;
                    }
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                    else if (interval->isUpperVector && !currentRefPosition->RegOptional())
                    {
                        // These only require a register if they are not RegOptional, and their lclVar
                        // interval is living in a register and not already partially spilled.
                        if ((currentRefPosition->refType == RefTypeUpperVectorSave) ||
                            (currentRefPosition->refType == RefTypeUpperVectorRestore))
                        {
                            Interval* lclVarInterval = interval->relatedInterval;
                            assert((lclVarInterval->physReg == REG_NA) || lclVarInterval->isPartiallySpilled);
                        }
                    }
#endif
                    else
                    {
                        assert(currentRefPosition->RegOptional());
                    }
                }
            }
        }
    }

    // Now, verify the resolution blocks.
    // Currently these are nearly always at the end of the method, but that may not always be the case.
    // So, we'll go through all the BBs looking for blocks whose bbNum is greater than bbNumMaxBeforeResolution.
    for (BasicBlock* currentBlock = compiler->fgFirstBB; currentBlock != nullptr; currentBlock = currentBlock->bbNext)
    {
        if (currentBlock->bbNum > bbNumMaxBeforeResolution)
        {
            // If we haven't enregistered an lclVars, we have no resolution blocks.
            assert(enregisterLocalVars);

            if (VERBOSE)
            {
                dumpRegRecordTitle();
                printf(shortRefPositionFormat, 0, 0);
                assert(currentBlock->bbPreds != nullptr && currentBlock->bbPreds->getBlock() != nullptr);
                printf(bbRefPosFormat, currentBlock->bbNum, currentBlock->bbPreds->getBlock()->bbNum);
                dumpRegRecords();
            }

            // Clear register assignments.
            for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
            {
                RegRecord* physRegRecord        = getRegisterRecord(reg);
                physRegRecord->assignedInterval = nullptr;
            }

            // Set the incoming register assignments
            VarToRegMap     inVarToRegMap = getInVarToRegMap(currentBlock->bbNum);
            VarSetOps::Iter iter(compiler, currentBlock->bbLiveIn);
            unsigned        varIndex = 0;
            while (iter.NextElem(&varIndex))
            {
                if (localVarIntervals[varIndex] == nullptr)
                {
                    assert(!compiler->lvaGetDescByTrackedIndex(varIndex)->lvLRACandidate);
                    continue;
                }
                regNumber regNum                  = getVarReg(inVarToRegMap, varIndex);
                Interval* interval                = getIntervalForLocalVar(varIndex);
                interval->physReg                 = regNum;
                interval->assignedReg             = &(physRegs[regNum]);
                interval->isActive                = true;
                physRegs[regNum].assignedInterval = interval;
            }

            // Verify the moves in this block
            LIR::Range& currentBlockRange = LIR::AsRange(currentBlock);
            for (GenTree* node : currentBlockRange.NonPhiNodes())
            {
                assert(IsResolutionNode(currentBlockRange, node));
                if (IsResolutionMove(node))
                {
                    // Only verify nodes that are actually moves; don't bother with the nodes that are
                    // operands to moves.
                    verifyResolutionMove(node, currentLocation);
                }
            }

            // Verify the outgoing register assignments
            {
                VarToRegMap     outVarToRegMap = getOutVarToRegMap(currentBlock->bbNum);
                VarSetOps::Iter iter(compiler, currentBlock->bbLiveOut);
                unsigned        varIndex = 0;
                while (iter.NextElem(&varIndex))
                {
                    if (localVarIntervals[varIndex] == nullptr)
                    {
                        assert(!compiler->lvaGetDescByTrackedIndex(varIndex)->lvLRACandidate);
                        continue;
                    }
                    regNumber regNum   = getVarReg(outVarToRegMap, varIndex);
                    Interval* interval = getIntervalForLocalVar(varIndex);
                    // Either the register assignments match, or the outgoing assignment is on the stack
                    // and this is a write-thru interval.
                    assert(interval->physReg == regNum || (interval->physReg == REG_NA && regNum == REG_STK) ||
                           (interval->isWriteThru && regNum == REG_STK));
                    interval->physReg     = REG_NA;
                    interval->assignedReg = nullptr;
                    interval->isActive    = false;
                }
            }
        }
    }

    DBEXEC(VERBOSE, printf("\n"));
}

//------------------------------------------------------------------------
// verifyResolutionMove: Verify a resolution statement.  Called by verifyFinalAllocation()
//
// Arguments:
//    resolutionMove    - A GenTree* that must be a resolution move.
//    currentLocation   - The LsraLocation of the most recent RefPosition that has been verified.
//
// Return Value:
//    None.
//
// Notes:
//    If verbose is set, this will also dump the moves into the table of final allocations.
void LinearScan::verifyResolutionMove(GenTree* resolutionMove, LsraLocation currentLocation)
{
    GenTree* dst = resolutionMove;
    assert(IsResolutionMove(dst));

    if (dst->OperGet() == GT_SWAP)
    {
        GenTreeLclVarCommon* left          = dst->gtGetOp1()->AsLclVarCommon();
        GenTreeLclVarCommon* right         = dst->gtGetOp2()->AsLclVarCommon();
        regNumber            leftRegNum    = left->GetRegNum();
        regNumber            rightRegNum   = right->GetRegNum();
        LclVarDsc*           leftVarDsc    = compiler->lvaTable + left->GetLclNum();
        LclVarDsc*           rightVarDsc   = compiler->lvaTable + right->GetLclNum();
        Interval*            leftInterval  = getIntervalForLocalVar(leftVarDsc->lvVarIndex);
        Interval*            rightInterval = getIntervalForLocalVar(rightVarDsc->lvVarIndex);
        assert(leftInterval->physReg == leftRegNum && rightInterval->physReg == rightRegNum);
        leftInterval->physReg                  = rightRegNum;
        rightInterval->physReg                 = leftRegNum;
        leftInterval->assignedReg              = &physRegs[rightRegNum];
        rightInterval->assignedReg             = &physRegs[leftRegNum];
        physRegs[rightRegNum].assignedInterval = leftInterval;
        physRegs[leftRegNum].assignedInterval  = rightInterval;
        if (VERBOSE)
        {
            printf(shortRefPositionFormat, currentLocation, 0);
            dumpIntervalName(leftInterval);
            printf("  Swap   ");
            printf("      %-4s ", getRegName(rightRegNum));
            dumpRegRecords();
            printf(shortRefPositionFormat, currentLocation, 0);
            dumpIntervalName(rightInterval);
            printf("  \"      ");
            printf("      %-4s ", getRegName(leftRegNum));
            dumpRegRecords();
        }
        return;
    }
    regNumber            dstRegNum = dst->GetRegNum();
    regNumber            srcRegNum;
    GenTreeLclVarCommon* lcl;
    if (dst->OperGet() == GT_COPY)
    {
        lcl       = dst->gtGetOp1()->AsLclVarCommon();
        srcRegNum = lcl->GetRegNum();
    }
    else
    {
        lcl = dst->AsLclVarCommon();
        if ((lcl->gtFlags & GTF_SPILLED) != 0)
        {
            srcRegNum = REG_STK;
        }
        else
        {
            assert((lcl->gtFlags & GTF_SPILL) != 0);
            srcRegNum = dstRegNum;
            dstRegNum = REG_STK;
        }
    }

    Interval* interval = getIntervalForLocalVarNode(lcl);
    assert(interval->physReg == srcRegNum || (srcRegNum == REG_STK && interval->physReg == REG_NA));
    if (srcRegNum != REG_STK)
    {
        physRegs[srcRegNum].assignedInterval = nullptr;
    }
    if (dstRegNum != REG_STK)
    {
        interval->physReg                    = dstRegNum;
        interval->assignedReg                = &(physRegs[dstRegNum]);
        physRegs[dstRegNum].assignedInterval = interval;
        interval->isActive                   = true;
    }
    else
    {
        interval->physReg     = REG_NA;
        interval->assignedReg = nullptr;
        interval->isActive    = false;
    }
    if (VERBOSE)
    {
        printf(shortRefPositionFormat, currentLocation, 0);
        dumpIntervalName(interval);
        printf("  Move      ");
        printf("      %-4s ", getRegName(dstRegNum));
        dumpRegRecords();
    }
}
#endif // DEBUG

LinearScan::RegisterSelection::RegisterSelection(LinearScan* linearScan)
{
    this->linearScan = linearScan;

#ifdef DEBUG
    mappingTable = new ScoreMappingTable(linearScan->compiler->getAllocator(CMK_LSRA));

#define REG_SEL_DEF(stat, value, shortname, orderSeqId)                                                                \
    mappingTable->Set(stat, &LinearScan::RegisterSelection::try_##stat);
#include "lsra_score.h"
#undef REG_SEL_DEF

    LPCWSTR ordering = JitConfig.JitLsraOrdering();
    if (ordering == nullptr)
    {
        ordering = W("ABCDEFGHIJKLMNOPQ");
    }

    for (int orderId = 0; orderId < REGSELECT_HEURISTIC_COUNT; orderId++)
    {
        // Make sure we do not set repeated entries
        assert(RegSelectionOrder[orderId] == NONE);

        switch (ordering[orderId])
        {
#define REG_SEL_DEF(enum_name, value, shortname, orderSeqId)                                                           \
    case orderSeqId:                                                                                                   \
        RegSelectionOrder[orderId] = enum_name;                                                                        \
        break;
#include "lsra_score.h"
#undef REG_SEL_DEF
            default:
                assert(!"Invalid lsraOrdering value.");
        }
    }
#endif // DEBUG
}

// ----------------------------------------------------------
//  reset: Resets the values of all the fields used for register selection.
//
void LinearScan::RegisterSelection::reset(Interval* interval, RefPosition* refPos)
{
    currentInterval = interval;
    refPosition     = refPos;
    score           = 0;

    regType         = linearScan->getRegisterType(currentInterval, refPosition);
    currentLocation = refPosition->nodeLocation;
    nextRefPos      = refPosition->nextRefPosition;
    candidates      = refPosition->registerAssignment;
    preferences     = currentInterval->registerPreferences;

    // This is not actually a preference, it's merely to track the lclVar that this
    // "specialPutArg" is using.
    relatedInterval    = currentInterval->isSpecialPutArg ? nullptr : currentInterval->relatedInterval;
    relatedPreferences = (relatedInterval == nullptr) ? RBM_NONE : relatedInterval->getCurrentPreferences();

    rangeEndLocation    = refPosition->getRangeEndLocation();
    relatedLastLocation = rangeEndLocation;
    preferCalleeSave    = currentInterval->preferCalleeSave;
    rangeEndRefPosition = nullptr;
    lastRefPosition     = currentInterval->lastRefPosition;
    lastLocation        = MinLocation;
    prevRegRec          = currentInterval->assignedReg;

    // These are used in the post-selection updates, and must be set for any selection.
    freeCandidates    = RBM_NONE;
    matchingConstants = RBM_NONE;
    unassignedSet     = RBM_NONE;

    coversSet        = RBM_NONE;
    preferenceSet    = RBM_NONE;
    coversRelatedSet = RBM_NONE;
    coversFullSet    = RBM_NONE;

    foundRegBit          = REG_NA;
    found                = false;
    skipAllocation       = false;
    coversSetsCalculated = false;
}

// ----------------------------------------------------------
//  applySelection: Apply the heuristic to the candidates.
//
// Arguments:
//  selectionScore:         The score corresponding to the heuristics we apply.
//  selectionCandidates:    The possible candidates for the heuristic to apply.
//
//  Return Values:
//      'true' if there was a single register candidate available after the heuristic is applied.
//
bool LinearScan::RegisterSelection::applySelection(int selectionScore, regMaskTP selectionCandidates)
{
    regMaskTP newCandidates = candidates & selectionCandidates;
    if (newCandidates != RBM_NONE)
    {
        score += selectionScore;
        candidates = newCandidates;
        return LinearScan::isSingleRegister(candidates);
    }
    return false;
}

// ----------------------------------------------------------
//  applySingleRegSelection: Select a single register, if it is in the candidate set.
//
// Arguments:
//  selectionScore:         The score corresponding to the heuristics we apply.
//  selectionCandidates:    The possible candidates for the heuristic to apply.
//
//  Return Values:
//      'true' if there was a single register candidate available after the heuristic is applied.
//
bool LinearScan::RegisterSelection::applySingleRegSelection(int selectionScore, regMaskTP selectionCandidate)
{
    assert(LinearScan::isSingleRegister(selectionCandidate));
    regMaskTP newCandidates = candidates & selectionCandidate;
    if (newCandidates != RBM_NONE)
    {
        candidates = newCandidates;
        return true;
    }
    return false;
}

// ----------------------------------------------------------
//  try_FREE: Apply the FREE heuristic.
//
void LinearScan::RegisterSelection::try_FREE()
{
    assert(!found);
    if (freeCandidates == RBM_NONE)
    {
        return;
    }

    found = applySelection(FREE, freeCandidates);
}

// ----------------------------------------------------------
//  try_CONST_AVAILABLE: Apply the CONST_AVAILABLE (matching constant) heuristic.
//
//  Note: we always need to define the 'matchingConstants' set.
//
void LinearScan::RegisterSelection::try_CONST_AVAILABLE()
{
    assert(!found);
    if (freeCandidates == RBM_NONE)
    {
        return;
    }

    if (currentInterval->isConstant && RefTypeIsDef(refPosition->refType))
    {
        found = applySelection(CONST_AVAILABLE, matchingConstants);
    }
}

// ----------------------------------------------------------
//  try_THIS_ASSIGNED: Apply the THIS_ASSIGNED heuristic.
//
void LinearScan::RegisterSelection::try_THIS_ASSIGNED()
{
    assert(!found);
    if (freeCandidates == RBM_NONE)
    {
        return;
    }

    if (prevRegRec != nullptr)
    {
        found = applySelection(THIS_ASSIGNED, freeCandidates & preferences & prevRegBit);
    }
}

// ----------------------------------------------------------
//  try_COVERS: Apply the COVERS heuristic.
//
void LinearScan::RegisterSelection::try_COVERS()
{
    assert(!found);

    calculateCoversSets();

    found = applySelection(COVERS, coversSet & preferenceSet);
}

// ----------------------------------------------------------
//  try_OWN_PREFERENCE: Apply the OWN_PREFERENCE heuristic.
//
//  Note: 'preferenceSet' already includes only freeCandidates.
//
void LinearScan::RegisterSelection::try_OWN_PREFERENCE()
{
    assert(!found);

#ifdef DEBUG
    calculateCoversSets();
#endif

    found = applySelection(OWN_PREFERENCE, (preferenceSet & freeCandidates));
}

// ----------------------------------------------------------
//  try_COVERS_RELATED: Apply the COVERS_RELATED heuristic.
//
void LinearScan::RegisterSelection::try_COVERS_RELATED()
{
    assert(!found);

#ifdef DEBUG
    calculateCoversSets();
#endif

    found = applySelection(COVERS_RELATED, (coversRelatedSet & freeCandidates));
}

// ----------------------------------------------------------
//  try_RELATED_PREFERENCE: Apply the RELATED_PREFERENCE heuristic.
//
void LinearScan::RegisterSelection::try_RELATED_PREFERENCE()
{
    assert(!found);

    found = applySelection(RELATED_PREFERENCE, relatedPreferences & freeCandidates);
}

// ----------------------------------------------------------
//  try_CALLER_CALLEE: Apply the CALLER_CALLEE heuristic.
//
void LinearScan::RegisterSelection::try_CALLER_CALLEE()
{
    assert(!found);

    found = applySelection(CALLER_CALLEE, callerCalleePrefs & freeCandidates);
}

// ----------------------------------------------------------
//  try_UNASSIGNED: Apply the UNASSIGNED heuristic.
//
void LinearScan::RegisterSelection::try_UNASSIGNED()
{
    assert(!found);

#ifdef DEBUG
    calculateCoversSets();
#endif

    found = applySelection(UNASSIGNED, unassignedSet);
}

// ----------------------------------------------------------
//  try_COVERS_FULL: Apply the COVERS_FULL heuristic.
//
void LinearScan::RegisterSelection::try_COVERS_FULL()
{
    assert(!found);

#ifdef DEBUG
    calculateCoversSets();
#endif

    found = applySelection(COVERS_FULL, (coversFullSet & freeCandidates));
}

// ----------------------------------------------------------
//  try_BEST_FIT: Apply the BEST_FIT heuristic.
//
void LinearScan::RegisterSelection::try_BEST_FIT()
{
    assert(!found);

    if (freeCandidates == RBM_NONE)
    {
        return;
    }

    regMaskTP bestFitSet = RBM_NONE;
    // If the best score includes COVERS_FULL, pick the one that's killed soonest.
    // If none cover the full range, the BEST_FIT is the one that's killed later.
    bool         earliestIsBest  = ((score & COVERS_FULL) != 0);
    LsraLocation bestFitLocation = earliestIsBest ? MaxLocation : MinLocation;
    for (regMaskTP bestFitCandidates = candidates; bestFitCandidates != RBM_NONE;)
    {
        regMaskTP bestFitCandidateBit = genFindLowestBit(bestFitCandidates);
        bestFitCandidates &= ~bestFitCandidateBit;
        regNumber bestFitCandidateRegNum = genRegNumFromMask(bestFitCandidateBit);

        // Find the next RefPosition of the register.
        LsraLocation nextIntervalLocation = linearScan->getNextIntervalRef(bestFitCandidateRegNum, regType);
        LsraLocation nextPhysRefLocation  = linearScan->getNextFixedRef(bestFitCandidateRegNum, regType);
        nextPhysRefLocation               = Min(nextPhysRefLocation, nextIntervalLocation);
        // If the nextPhysRefLocation is a fixedRef for the rangeEndRefPosition, increment it so that
        // we don't think it isn't covering the live range.
        // This doesn't handle the case where earlier RefPositions for this Interval are also
        // FixedRefs of this regNum, but at least those are only interesting in the case where those
        // are "local last uses" of the Interval - otherwise the liveRange would interfere with the reg.
        // TODO: This duplicates code in an earlier loop, and is basically here to duplicate previous
        // behavior; see if we can avoid this.
        if (nextPhysRefLocation == rangeEndLocation && rangeEndRefPosition->isFixedRefOfReg(bestFitCandidateRegNum))
        {
            INDEBUG(linearScan->dumpLsraAllocationEvent(LSRA_EVENT_INCREMENT_RANGE_END, currentInterval));
            nextPhysRefLocation++;
        }

        if (nextPhysRefLocation == bestFitLocation)
        {
            bestFitSet |= bestFitCandidateBit;
        }
        else
        {
            bool isBetter = false;
            if (nextPhysRefLocation > lastLocation)
            {
                // This covers the full range; favor it if the other doesn't, or if it's a closer match.
                if ((bestFitLocation <= lastLocation) || (nextPhysRefLocation < bestFitLocation))
                {
                    isBetter = true;
                }
            }
            else
            {
                // This doesn't cover the full range; favor it if the other doesn't either, but this ends later.
                if ((bestFitLocation <= lastLocation) && (nextPhysRefLocation > bestFitLocation))
                {
                    isBetter = true;
                }
            }
            if (isBetter)
            {
                bestFitSet      = bestFitCandidateBit;
                bestFitLocation = nextPhysRefLocation;
            }
        }
    }
    assert(bestFitSet != RBM_NONE);
    found = applySelection(BEST_FIT, bestFitSet);
}

// ----------------------------------------------------------
//  try_IS_PREV_REG: Apply the IS_PREV_REG heuristic.
//
//  Note:  Oddly, the previous heuristics only considered this if it covered the range.
//  TODO: Check if Only applies if we have freeCandidates.
//
void LinearScan::RegisterSelection::try_IS_PREV_REG()
{
    // TODO: We do not check found here.
    if ((prevRegRec != nullptr) && ((score & COVERS_FULL) != 0))
    {
        found = applySingleRegSelection(IS_PREV_REG, prevRegBit);
    }
}

// ----------------------------------------------------------
//  try_REG_ORDER: Apply the REG_ORDER heuristic. Only applies if we have freeCandidates.
//
void LinearScan::RegisterSelection::try_REG_ORDER()
{
    assert(!found);

    if (freeCandidates == RBM_NONE)
    {
        return;
    }

    // This will always result in a single candidate. That is, it is the tie-breaker
    // for free candidates, and doesn't make sense as anything other than the last
    // heuristic for free registers.
    unsigned  lowestRegOrder    = UINT_MAX;
    regMaskTP lowestRegOrderBit = RBM_NONE;
    for (regMaskTP regOrderCandidates = candidates; regOrderCandidates != RBM_NONE;)
    {
        regMaskTP regOrderCandidateBit = genFindLowestBit(regOrderCandidates);
        regOrderCandidates &= ~regOrderCandidateBit;
        regNumber regOrderCandidateRegNum = genRegNumFromMask(regOrderCandidateBit);
        unsigned  thisRegOrder            = linearScan->getRegisterRecord(regOrderCandidateRegNum)->regOrder;
        if (thisRegOrder < lowestRegOrder)
        {
            lowestRegOrder    = thisRegOrder;
            lowestRegOrderBit = regOrderCandidateBit;
        }
    }
    assert(lowestRegOrderBit != RBM_NONE);
    found = applySingleRegSelection(REG_ORDER, lowestRegOrderBit);
}

// ----------------------------------------------------------
//  try_SPILL_COST: Apply the SPILL_COST heuristic.
//
void LinearScan::RegisterSelection::try_SPILL_COST()
{
    assert(!found);

    // The set of registers with the lowest spill weight.
    regMaskTP lowestCostSpillSet = RBM_NONE;
    // Apply the SPILL_COST heuristic and eliminate regs that can't be spilled.

    // The spill weight for 'refPosition' (the one we're allocating now).
    float thisSpillWeight = linearScan->getWeight(refPosition);
    // The  spill weight for the best candidate we've found so far.
    float bestSpillWeight = FloatingPointUtils::infinite_float();
    // True if we found registers with lower spill weight than this refPosition.
    bool foundLowerSpillWeight = false;

    for (regMaskTP spillCandidates = candidates; spillCandidates != RBM_NONE;)
    {
        regMaskTP spillCandidateBit = genFindLowestBit(spillCandidates);
        spillCandidates &= ~spillCandidateBit;
        regNumber  spillCandidateRegNum    = genRegNumFromMask(spillCandidateBit);
        RegRecord* spillCandidateRegRecord = &linearScan->physRegs[spillCandidateRegNum];

        // Can and should the interval in this register be spilled for this one,
        // if we don't find a better alternative?
        if ((linearScan->getNextIntervalRef(spillCandidateRegNum, regType) == currentLocation) &&
            !spillCandidateRegRecord->assignedInterval->getNextRefPosition()->RegOptional())
        {
            continue;
        }
        if (!linearScan->isSpillCandidate(currentInterval, refPosition, spillCandidateRegRecord))
        {
            continue;
        }

        float currentSpillWeight = linearScan->spillCost[spillCandidateRegNum];
#ifdef TARGET_ARM
        if (currentInterval->registerType == TYP_DOUBLE)
        {
            currentSpillWeight = max(currentSpillWeight, linearScan->spillCost[REG_NEXT(spillCandidateRegNum)]);
        }
#endif
        if (currentSpillWeight < bestSpillWeight)
        {
            bestSpillWeight    = currentSpillWeight;
            lowestCostSpillSet = spillCandidateBit;
        }
        else if (currentSpillWeight == bestSpillWeight)
        {
            lowestCostSpillSet |= spillCandidateBit;
        }
    }

    if (lowestCostSpillSet == RBM_NONE)
    {
        return;
    }

    // We won't spill if this refPosition is RegOptional() and we have no candidates
    // with a lower spill cost.
    if ((bestSpillWeight >= thisSpillWeight) && refPosition->RegOptional())
    {
        currentInterval->assignedReg = nullptr;
        skipAllocation               = true;
        found                        = true;
    }

    // We must have at least one with the lowest spill cost.
    assert(lowestCostSpillSet != RBM_NONE);
    found = applySelection(SPILL_COST, lowestCostSpillSet);
}

// ----------------------------------------------------------
//  try_FAR_NEXT_REF: Apply the FAR_NEXT_REF heuristic.
//
void LinearScan::RegisterSelection::try_FAR_NEXT_REF()
{
    assert(!found);

    LsraLocation farthestLocation = MinLocation;
    regMaskTP    farthestSet      = RBM_NONE;
    for (regMaskTP farthestCandidates = candidates; farthestCandidates != RBM_NONE;)
    {
        regMaskTP farthestCandidateBit = genFindLowestBit(farthestCandidates);
        farthestCandidates &= ~farthestCandidateBit;
        regNumber farthestCandidateRegNum = genRegNumFromMask(farthestCandidateBit);

        // Find the next RefPosition of the register.
        LsraLocation nextIntervalLocation =
            linearScan->getNextIntervalRef(farthestCandidateRegNum, currentInterval->registerType);
        LsraLocation nextPhysRefLocation = Min(linearScan->nextFixedRef[farthestCandidateRegNum], nextIntervalLocation);
        if (nextPhysRefLocation == farthestLocation)
        {
            farthestSet |= farthestCandidateBit;
        }
        else if (nextPhysRefLocation > farthestLocation)
        {
            farthestSet      = farthestCandidateBit;
            farthestLocation = nextPhysRefLocation;
        }
    }
    // We must have at least one with the lowest spill cost.
    assert(farthestSet != RBM_NONE);
    found = applySelection(FAR_NEXT_REF, farthestSet);
}

// ----------------------------------------------------------
//  try_PREV_REG_OPT: Apply the PREV_REG_OPT heuristic.
//
void LinearScan::RegisterSelection::try_PREV_REG_OPT()
{
    assert(!found);

    regMaskTP prevRegOptSet = RBM_NONE;
    for (regMaskTP prevRegOptCandidates = candidates; prevRegOptCandidates != RBM_NONE;)
    {
        regMaskTP prevRegOptCandidateBit = genFindLowestBit(prevRegOptCandidates);
        prevRegOptCandidates &= ~prevRegOptCandidateBit;
        regNumber prevRegOptCandidateRegNum = genRegNumFromMask(prevRegOptCandidateBit);
        Interval* assignedInterval          = linearScan->physRegs[prevRegOptCandidateRegNum].assignedInterval;
        bool      foundPrevRegOptReg        = true;
#ifdef DEBUG
        bool hasAssignedInterval = false;
#endif

        if ((assignedInterval != nullptr) && (assignedInterval->recentRefPosition != nullptr))
        {
            foundPrevRegOptReg &=
                (assignedInterval->recentRefPosition->reload && assignedInterval->recentRefPosition->RegOptional());
#ifdef DEBUG
            hasAssignedInterval = true;
#endif
        }
#ifndef TARGET_ARM
        else
        {
            foundPrevRegOptReg = false;
        }
#endif

#ifdef TARGET_ARM
        // If current interval is TYP_DOUBLE, verify if the other half register matches the heuristics.
        // We have three cases:
        // 1. One of the register of the pair have an assigned interval: Check if that register's refPosition
        // matches the heuristics. If yes, add it to the set.
        // 2. Both registers of the pair have an assigned interval: Conservatively "and" conditions for
        // heuristics of their corresponding refPositions. If both register's heuristic matches, add them
        // to the set. TODO-CQ-ARM: We may implement a better condition later.
        // 3. None of the register have an assigned interval: Skip adding register and assert.
        if (currentInterval->registerType == TYP_DOUBLE)
        {
            regNumber anotherHalfRegNum = linearScan->findAnotherHalfRegNum(prevRegOptCandidateRegNum);
            assignedInterval            = linearScan->physRegs[anotherHalfRegNum].assignedInterval;
            if ((assignedInterval != nullptr) && (assignedInterval->recentRefPosition != nullptr))
            {
                if (assignedInterval->recentRefPosition->reload && assignedInterval->recentRefPosition->RegOptional())
                {
                    foundPrevRegOptReg &= (assignedInterval->recentRefPosition->reload &&
                                           assignedInterval->recentRefPosition->RegOptional());
                }
#ifdef DEBUG
                hasAssignedInterval = true;
#endif
            }
        }
#endif

        if (foundPrevRegOptReg)
        {
            // TODO-Cleanup: Previously, we always used the highest regNum with a previous regOptional
            // RefPosition, which is not really consistent with the way other selection criteria are
            // applied. should probably be: prevRegOptSet |= prevRegOptCandidateBit;
            prevRegOptSet = prevRegOptCandidateBit;
        }

#ifdef DEBUG
        // The assigned should be non-null, and should have a recentRefPosition, however since
        // this is a heuristic, we don't want a fatal error, so we just assert (not noway_assert).
        if (!hasAssignedInterval)
        {
            assert(!"Spill candidate has no assignedInterval recentRefPosition");
        }
#endif
    }
    found = applySelection(PREV_REG_OPT, prevRegOptSet);
}

// ----------------------------------------------------------
//  try_REG_NUM: Apply the REG_NUM heuristic.
//
void LinearScan::RegisterSelection::try_REG_NUM()
{
    assert(!found);

    found = applySingleRegSelection(REG_NUM, genFindLowestBit(candidates));
}

// ----------------------------------------------------------
//  calculateCoversSets: Calculate the necessary covers set registers to be used
//      for heuristics lke COVERS, COVERS_RELATED, COVERS_FULL.
//
void LinearScan::RegisterSelection::calculateCoversSets()
{
    if (freeCandidates == RBM_NONE || coversSetsCalculated)
    {
        return;
    }

    preferenceSet              = (candidates & preferences);
    regMaskTP coversCandidates = (preferenceSet == RBM_NONE) ? candidates : preferenceSet;
    for (; coversCandidates != RBM_NONE;)
    {
        regMaskTP coversCandidateBit = genFindLowestBit(coversCandidates);
        coversCandidates &= ~coversCandidateBit;
        regNumber coversCandidateRegNum = genRegNumFromMask(coversCandidateBit);

        // If we have a single candidate we don't need to compute the preference-related sets, but we
        // do need to compute the unassignedSet.
        if (!found)
        {
            // Find the next RefPosition of the register.
            LsraLocation nextIntervalLocation    = linearScan->getNextIntervalRef(coversCandidateRegNum, regType);
            LsraLocation nextPhysRefLocation     = linearScan->getNextFixedRef(coversCandidateRegNum, regType);
            LsraLocation coversCandidateLocation = Min(nextPhysRefLocation, nextIntervalLocation);

            // If the nextPhysRefLocation is a fixedRef for the rangeEndRefPosition, increment it so that
            // we don't think it isn't covering the live range.
            // This doesn't handle the case where earlier RefPositions for this Interval are also
            // FixedRefs of this regNum, but at least those are only interesting in the case where those
            // are "local last uses" of the Interval - otherwise the liveRange would interfere with the reg.
            if (coversCandidateLocation == rangeEndLocation &&
                rangeEndRefPosition->isFixedRefOfReg(coversCandidateRegNum))
            {
                INDEBUG(linearScan->dumpLsraAllocationEvent(LSRA_EVENT_INCREMENT_RANGE_END, currentInterval));
                coversCandidateLocation++;
            }
            if (coversCandidateLocation > rangeEndLocation)
            {
                coversSet |= coversCandidateBit;
            }
            if ((coversCandidateBit & relatedPreferences) != RBM_NONE)
            {
                if (coversCandidateLocation > relatedLastLocation)
                {
                    coversRelatedSet |= coversCandidateBit;
                }
            }
            else if (coversCandidateBit == refPosition->registerAssignment)
            {
                // If we had a fixed-reg def of a reg that will be killed before the use, prefer it to any other
                // registers with the same score.  (Note that we haven't changed the original registerAssignment
                // on the RefPosition).
                // Overload the RELATED_PREFERENCE value.
                // TODO-CQ: Consider if this should be split out.
                coversRelatedSet |= coversCandidateBit;
            }
            // Does this cover the full range of the interval?
            if (coversCandidateLocation > lastLocation)
            {
                coversFullSet |= coversCandidateBit;
            }
        }
        // The register is considered unassigned if it has no assignedInterval, OR
        // if its next reference is beyond the range of this interval.
        if (linearScan->nextIntervalRef[coversCandidateRegNum] > lastLocation)
        {
            unassignedSet |= coversCandidateBit;
        }
    }

    coversSetsCalculated = true;
}

// ----------------------------------------------------------
//  select: For given `currentInterval` and `refPosition`, selects a register to be assigned.
//
// Arguments:
//   currentInterval - Current interval for which register needs to be selected.
//   refPosition     - Refposition within the interval for which register needs to be selected.
//
//  Return Values:
//      Register bit selected (a single register) and REG_NA if no register was selected.
//
regMaskTP LinearScan::RegisterSelection::select(Interval*    currentInterval,
                                                RefPosition* refPosition DEBUG_ARG(RegisterScore* registerScore))
{
#ifdef DEBUG
    *registerScore = NONE;
#endif

    reset(currentInterval, refPosition);

    // process data-structures
    if (RefTypeIsDef(refPosition->refType))
    {
        if (currentInterval->hasConflictingDefUse)
        {
            linearScan->resolveConflictingDefAndUse(currentInterval, refPosition);
            candidates = refPosition->registerAssignment;
        }
        // Otherwise, check for the case of a fixed-reg def of a reg that will be killed before the
        // use, or interferes at the point of use (which shouldn't happen, but Lower doesn't mark
        // the contained nodes as interfering).
        // Note that we may have a ParamDef RefPosition that is marked isFixedRegRef, but which
        // has had its registerAssignment changed to no longer be a single register.
        else if (refPosition->isFixedRegRef && nextRefPos != nullptr && RefTypeIsUse(nextRefPos->refType) &&
                 !nextRefPos->isFixedRegRef && genMaxOneBit(refPosition->registerAssignment))
        {
            regNumber  defReg       = refPosition->assignedReg();
            RegRecord* defRegRecord = linearScan->getRegisterRecord(defReg);

            RefPosition* currFixedRegRefPosition = defRegRecord->recentRefPosition;
            assert(currFixedRegRefPosition != nullptr &&
                   currFixedRegRefPosition->nodeLocation == refPosition->nodeLocation);

            // If there is another fixed reference to this register before the use, change the candidates
            // on this RefPosition to include that of nextRefPos.
            RefPosition* nextFixedRegRefPosition = defRegRecord->getNextRefPosition();
            if (nextFixedRegRefPosition != nullptr &&
                nextFixedRegRefPosition->nodeLocation <= nextRefPos->getRefEndLocation())
            {
                candidates |= nextRefPos->registerAssignment;
                if (preferences == refPosition->registerAssignment)
                {
                    preferences = candidates;
                }
            }
        }
    }

    preferences &= candidates;
    if (preferences == RBM_NONE)
    {
        preferences = candidates;
    }

#ifdef DEBUG
    candidates = linearScan->stressLimitRegs(refPosition, candidates);
#endif
    assert(candidates != RBM_NONE);

    Interval* nextRelatedInterval  = relatedInterval;
    Interval* finalRelatedInterval = relatedInterval;
    Interval* rangeEndInterval     = relatedInterval;

    bool avoidByteRegs = false;
#ifdef TARGET_X86
    if ((relatedPreferences & ~RBM_BYTE_REGS) != RBM_NONE)
    {
        avoidByteRegs = true;
    }
#endif

    // Follow the chain of related intervals, as long as:
    // - The next reference is a def. We don't want to use the relatedInterval for preferencing if its next reference
    //   is not a new definition (as it either is or will become live).
    // - The next (def) reference is downstream. Otherwise we could iterate indefinitely because the preferences can be
    // circular.
    // - The intersection of preferenced registers is non-empty.
    //
    while (nextRelatedInterval != nullptr)
    {
        RefPosition* nextRelatedRefPosition = nextRelatedInterval->getNextRefPosition();

        // Only use the relatedInterval for preferencing if the related interval's next reference
        // is a new definition.
        if ((nextRelatedRefPosition != nullptr) && RefTypeIsDef(nextRelatedRefPosition->refType))
        {
            finalRelatedInterval = nextRelatedInterval;
            nextRelatedInterval  = nullptr;

            // First, get the preferences for this interval
            regMaskTP thisRelatedPreferences = finalRelatedInterval->getCurrentPreferences();
            // Now, determine if they are compatible and update the relatedPreferences that we'll consider.
            regMaskTP newRelatedPreferences = thisRelatedPreferences & relatedPreferences;
            if (newRelatedPreferences != RBM_NONE && (!avoidByteRegs || thisRelatedPreferences != RBM_BYTE_REGS))
            {
                // TODO-CQ: The following isFree() check doesn't account for the possibility that there's an
                // assignedInterval whose recentRefPosition was delayFree. It also fails to account for
                // the TYP_DOUBLE case on ARM. It would be better to replace the call to isFree with
                //   isRegAvailable(genRegNumFromMask(newRelatedPreferences), regType)), but this is retained
                //   to achieve zero diffs.
                //
                bool thisIsSingleReg = isSingleRegister(newRelatedPreferences);
                if (!thisIsSingleReg ||
                    (finalRelatedInterval->isLocalVar &&
                     linearScan->isFree(linearScan->getRegisterRecord(genRegNumFromMask(newRelatedPreferences)))))
                {
                    relatedPreferences = newRelatedPreferences;
                    // If this Interval has a downstream def without a single-register preference, continue to iterate.
                    if (nextRelatedRefPosition->nodeLocation > rangeEndLocation)
                    {
                        preferCalleeSave    = (preferCalleeSave || finalRelatedInterval->preferCalleeSave);
                        rangeEndLocation    = nextRelatedRefPosition->getRangeEndLocation();
                        rangeEndInterval    = finalRelatedInterval;
                        nextRelatedInterval = finalRelatedInterval->relatedInterval;
                    }
                }
            }
        }
        else
        {
            if (nextRelatedInterval == relatedInterval)
            {
                relatedInterval    = nullptr;
                relatedPreferences = RBM_NONE;
            }
            nextRelatedInterval = nullptr;
        }
    }

    // For floating point, we want to be less aggressive about using callee-save registers.
    // So in that case, we just need to ensure that the current RefPosition is covered.
    if (useFloatReg(currentInterval->registerType))
    {
        rangeEndRefPosition = refPosition;
        preferCalleeSave    = currentInterval->preferCalleeSave;
    }
    else if (currentInterval->isWriteThru && refPosition->spillAfter)
    {
        // This is treated as a last use of the register, as there is an upcoming EH boundary.
        rangeEndRefPosition = refPosition;
    }
    else
    {
        rangeEndRefPosition = refPosition->getRangeEndRef();
        // If we have a chain of related intervals, and a finalRelatedInterval that
        // is not currently occupying a register, and whose lifetime begins after this one,
        // we want to try to select a register that will cover its lifetime.
        if ((rangeEndInterval != nullptr) && (rangeEndInterval->assignedReg == nullptr) &&
            !rangeEndInterval->isWriteThru &&
            (rangeEndInterval->getNextRefLocation() >= rangeEndRefPosition->nodeLocation))
        {
            lastRefPosition = rangeEndInterval->lastRefPosition;
        }
    }
    if ((relatedInterval != nullptr) && !relatedInterval->isWriteThru)
    {
        relatedLastLocation = relatedInterval->lastRefPosition->nodeLocation;
    }

    if (preferCalleeSave)
    {
        regMaskTP calleeSaveCandidates = calleeSaveRegs(currentInterval->registerType);
        if (currentInterval->isWriteThru)
        {
            // We'll only prefer a callee-save register if it's already been used.
            regMaskTP unusedCalleeSaves =
                calleeSaveCandidates & ~(linearScan->compiler->codeGen->regSet.rsGetModifiedRegsMask());
            callerCalleePrefs = calleeSaveCandidates & ~unusedCalleeSaves;
            preferences &= ~unusedCalleeSaves;
        }
        else
        {
            callerCalleePrefs = calleeSaveCandidates;
        }
    }
    else
    {
        callerCalleePrefs = callerSaveRegs(currentInterval->registerType);
    }

    // If this has a delayed use (due to being used in a rmw position of a
    // non-commutative operator), its endLocation is delayed until the "def"
    // position, which is one location past the use (getRefEndLocation() takes care of this).
    rangeEndLocation = rangeEndRefPosition->getRefEndLocation();
    lastLocation     = lastRefPosition->getRefEndLocation();

    // We'll set this to short-circuit remaining heuristics when we have a single candidate.
    found = false;

    // Is this a fixedReg?
    regMaskTP fixedRegMask = RBM_NONE;
    if (refPosition->isFixedRegRef)
    {
        assert(genMaxOneBit(refPosition->registerAssignment));
        fixedRegMask = refPosition->registerAssignment;
        if (candidates == refPosition->registerAssignment)
        {
            found = true;
            if (linearScan->nextIntervalRef[genRegNumFromMask(candidates)] > lastLocation)
            {
                unassignedSet = candidates;
            }
        }
    }

    // Eliminate candidates that are in-use or busy.
    if (!found)
    {
        regMaskTP busyRegs = linearScan->regsBusyUntilKill | linearScan->regsInUseThisLocation;
        candidates &= ~busyRegs;

        // Also eliminate as busy any register with a conflicting fixed reference at this or
        // the next location.
        // Note that this will eliminate the fixedReg, if any, but we'll add it back below.
        regMaskTP checkConflictMask = candidates & linearScan->fixedRegs;
        while (checkConflictMask != RBM_NONE)
        {
            regMaskTP checkConflictBit = genFindLowestBit(checkConflictMask);
            checkConflictMask &= ~checkConflictBit;
            regNumber    checkConflictReg      = genRegNumFromMask(checkConflictBit);
            LsraLocation checkConflictLocation = linearScan->nextFixedRef[checkConflictReg];

            if ((checkConflictLocation == currentLocation) ||
                (refPosition->delayRegFree && (checkConflictLocation == (currentLocation + 1))))
            {
                candidates &= ~checkConflictBit;
            }
        }
        candidates |= fixedRegMask;
        found = isSingleRegister(candidates);
    }

    // By chance, is prevRegRec already holding this interval, as a copyReg or having
    // been restored as inactive after a kill?
    // NOTE: this is not currently considered one of the selection criteria - it always wins
    // if it is the assignedInterval of 'prevRegRec'.
    if (!found && (prevRegRec != nullptr))
    {
        prevRegBit = genRegMask(prevRegRec->regNum);
        if ((prevRegRec->assignedInterval == currentInterval) && ((candidates & prevRegBit) != RBM_NONE))
        {
            candidates = prevRegBit;
            found      = true;
#ifdef DEBUG
            *registerScore = THIS_ASSIGNED;
#endif
        }
    }
    else
    {
        prevRegBit = RBM_NONE;
    }

    if (!found && (candidates == RBM_NONE))
    {
        assert(refPosition->RegOptional());
        currentInterval->assignedReg = nullptr;
        return RBM_NONE;
    }

    // TODO-Cleanup: Previously, the "reverseSelect" stress mode reversed the order of the heuristics.
    // It needs to be re-engineered with this refactoring.
    // In non-debug builds, this will simply get optimized away
    bool reverseSelect = false;
#ifdef DEBUG
    reverseSelect = linearScan->doReverseSelect();
#endif // DEBUG

    freeCandidates = linearScan->getFreeCandidates(candidates, regType);

    // If no free candidates, then double check if refPosition is an actual ref.
    if (freeCandidates == RBM_NONE)
    {
        // We won't spill if this refPosition is not an actual ref.
        if (!refPosition->IsActualRef())
        {
            currentInterval->assignedReg = nullptr;
            return RBM_NONE;
        }
    }
    else
    {
        // Set the 'matchingConstants' set.
        if (currentInterval->isConstant && RefTypeIsDef(refPosition->refType))
        {
            matchingConstants = linearScan->getMatchingConstants(candidates, currentInterval, refPosition);
        }
    }

#define IF_FOUND_GOTO_DONE                                                                                             \
    if (found)                                                                                                         \
        goto Selection_Done;

#ifdef DEBUG
    HeuristicFn fn;
    for (int orderId = 0; orderId < REGSELECT_HEURISTIC_COUNT; orderId++)
    {
        IF_FOUND_GOTO_DONE

        RegisterScore heuristicToApply = RegSelectionOrder[orderId];
        if (mappingTable->Lookup(heuristicToApply, &fn))
        {
            (this->*fn)();
            if (found)
            {
                *registerScore = heuristicToApply;
            }

#if TRACK_LSRA_STATS
            INTRACK_STATS_IF(found, linearScan->updateLsraStat(linearScan->getLsraStatFromScore(heuristicToApply),
                                                               refPosition->bbNum));
#endif // TRACK_LSRA_STATS
        }
        else
        {
            assert(!"Unexpected heuristic value!");
        }
    }
#else // RELEASE
// In release, just invoke the default order

#define REG_SEL_DEF(stat, value, shortname, orderSeqId)                                                                \
    try_##stat();                                                                                                      \
    IF_FOUND_GOTO_DONE
#include "lsra_score.h"
#undef REG_SEL_DEF
#endif // DEBUG
#undef IF_FOUND_GOTO_DONE

Selection_Done:
    if (skipAllocation)
    {
        return RBM_NONE;
    }

    calculateCoversSets();

    assert(found && isSingleRegister(candidates));
    foundRegBit = candidates;
    return candidates;
}
