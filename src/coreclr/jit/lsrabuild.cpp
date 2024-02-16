// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                    Interval and RefPosition Building                      XX
XX                                                                           XX
XX  This contains the logic for constructing Intervals and RefPositions that XX
XX  is common across architectures. See lsra{arch}.cpp for the architecture- XX
XX  specific methods for building.                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "lsra.h"

//------------------------------------------------------------------------
// RefInfoList
//------------------------------------------------------------------------
// removeListNode - retrieve the RefInfoListNode for the given GenTree node
//
// Notes:
//     The BuildNode methods use this helper to retrieve the RefPositions for child nodes
//     from the useList being constructed. Note that, if the user knows the order of the operands,
//     it is expected that they should just retrieve them directly.
//
RefInfoListNode* RefInfoList::removeListNode(GenTree* node)
{
    RefInfoListNode* prevListNode = nullptr;
    for (RefInfoListNode *listNode = Begin(), *end = End(); listNode != end; listNode = listNode->Next())
    {
        if (listNode->treeNode == node)
        {
            assert(listNode->ref->getMultiRegIdx() == 0);
            return removeListNode(listNode, prevListNode);
        }
        prevListNode = listNode;
    }
    assert(!"removeListNode didn't find the node");
    unreached();
}

//------------------------------------------------------------------------
// removeListNode - retrieve the RefInfoListNode for one reg of the given multireg GenTree node
//
// Notes:
//     The BuildNode methods use this helper to retrieve the RefPositions for child nodes
//     from the useList being constructed. Note that, if the user knows the order of the operands,
//     it is expected that they should just retrieve them directly.
//
RefInfoListNode* RefInfoList::removeListNode(GenTree* node, unsigned multiRegIdx)
{
    RefInfoListNode* prevListNode = nullptr;
    for (RefInfoListNode *listNode = Begin(), *end = End(); listNode != end; listNode = listNode->Next())
    {
        if ((listNode->treeNode == node) && (listNode->ref->getMultiRegIdx() == multiRegIdx))
        {
            return removeListNode(listNode, prevListNode);
        }
        prevListNode = listNode;
    }
    assert(!"removeListNode didn't find the node");
    unreached();
}

//------------------------------------------------------------------------
// RefInfoListNodePool::RefInfoListNodePool:
//    Creates a pool of `RefInfoListNode` values.
//
// Arguments:
//    compiler    - The compiler context.
//    preallocate - The number of nodes to preallocate.
//
RefInfoListNodePool::RefInfoListNodePool(Compiler* compiler, unsigned preallocate) : m_compiler(compiler)
{
    if (preallocate > 0)
    {
        RefInfoListNode* preallocatedNodes = compiler->getAllocator(CMK_LSRA).allocate<RefInfoListNode>(preallocate);

        RefInfoListNode* head = preallocatedNodes;
        head->m_next          = nullptr;

        for (unsigned i = 1; i < preallocate; i++)
        {
            RefInfoListNode* node = &preallocatedNodes[i];
            node->m_next          = head;
            head                  = node;
        }

        m_freeList = head;
    }
}

//------------------------------------------------------------------------
// RefInfoListNodePool::GetNode: Fetches an unused node from the
//                                    pool.
//
// Arguments:
//    r - The `RefPosition` for the `RefInfo` value.
//    t - The IR node for the `RefInfo` value
//
// Returns:
//    A pooled or newly-allocated `RefInfoListNode`, depending on the
//    contents of the pool.
RefInfoListNode* RefInfoListNodePool::GetNode(RefPosition* r, GenTree* t)
{
    RefInfoListNode* head = m_freeList;
    if (head == nullptr)
    {
        head = m_compiler->getAllocator(CMK_LSRA).allocate<RefInfoListNode>(1);
    }
    else
    {
        m_freeList = head->m_next;
    }

    head->ref      = r;
    head->treeNode = t;
    head->m_next   = nullptr;

    return head;
}

//------------------------------------------------------------------------
// RefInfoListNodePool::ReturnNode: Returns a list of nodes to the node
//                                   pool and clears the given list.
//
// Arguments:
//    list - The list to return.
//
void RefInfoListNodePool::ReturnNode(RefInfoListNode* listNode)
{
    listNode->m_next = m_freeList;
    m_freeList       = listNode;
}

//------------------------------------------------------------------------
// newInterval: Create a new Interval of the given RegisterType.
//
// Arguments:
//    theRegisterType - The type of Interval to create.
//
// TODO-Cleanup: Consider adding an overload that takes a varDsc, and can appropriately
// set such fields as isStructField
//
Interval* LinearScan::newInterval(RegisterType theRegisterType)
{
    intervals.emplace_back(theRegisterType, allRegs(theRegisterType));
    Interval* newInt = &intervals.back();

#ifdef DEBUG
    newInt->intervalIndex = static_cast<unsigned>(intervals.size() - 1);
#endif // DEBUG

    DBEXEC(VERBOSE, newInt->dump(this->compiler));
    return newInt;
}

//------------------------------------------------------------------------
// newRefPositionRaw: Create a new RefPosition
//
// Arguments:
//    nodeLocation - The location of the reference.
//    treeNode     - The GenTree of the reference.
//    refType      - The type of reference
//
// Notes:
//    This is used to create RefPositions for both RegRecords and Intervals,
//    so it does only the common initialization.
//
RefPosition* LinearScan::newRefPositionRaw(LsraLocation nodeLocation, GenTree* treeNode, RefType refType)
{
    refPositions.emplace_back(curBBNum, nodeLocation, treeNode, refType DEBUG_ARG(currBuildNode));
    RefPosition* newRP = &refPositions.back();
#ifdef DEBUG
    // Reset currBuildNode so we do not set it for subsequent refpositions belonging
    // to the same treeNode and hence, avoid printing it for every refposition inside
    // the allocation table.
    currBuildNode = nullptr;
    newRP->rpNum  = static_cast<unsigned>(refPositions.size() - 1);
    if (!enregisterLocalVars)
    {
        assert(!((refType == RefTypeParamDef) || (refType == RefTypeZeroInit) || (refType == RefTypeDummyDef) ||
                 (refType == RefTypeExpUse)));
    }
#endif // DEBUG
    return newRP;
}

//------------------------------------------------------------------------
// resolveConflictingDefAndUse: Resolve the situation where we have conflicting def and use
//    register requirements on a single-def, single-use interval.
//
// Arguments:
//    defRefPosition - The interval definition
//    useRefPosition - The (sole) interval use
//
// Return Value:
//    None.
//
// Assumptions:
//    The two RefPositions are for the same interval, which is a tree-temp.
//
// Notes:
//    We require some special handling for the case where the use is a "delayRegFree" case of a fixedReg.
//    In that case, if we change the registerAssignment on the useRefPosition, we will lose the fact that,
//    even if we assign a different register (and rely on codegen to do the copy), that fixedReg also needs
//    to remain busy until the Def register has been allocated.  In that case, we don't allow Case 1 or Case 4
//    below.
//    Here are the cases we consider (in this order):
//    1. If The defRefPosition specifies a single register, and there are no conflicting
//       FixedReg uses of it between the def and use, we use that register, and the code generator
//       will insert the copy.  Note that it cannot be in use because there is a FixedRegRef for the def.
//    2. If the useRefPosition specifies a single register, and it is not in use, and there are no
//       conflicting FixedReg uses of it between the def and use, we use that register, and the code generator
//       will insert the copy.
//    3. If the defRefPosition specifies a single register (but there are conflicts, as determined
//       in 1.), and there are no conflicts with the useRefPosition register (if it's a single register),
///      we set the register requirements on the defRefPosition to the use registers, and the
//       code generator will insert a copy on the def.  We can't rely on the code generator to put a copy
//       on the use if it has multiple possible candidates, as it won't know which one has been allocated.
//    4. If the useRefPosition specifies a single register, and there are no conflicts with the register
//       on the defRefPosition, we leave the register requirements on the defRefPosition as-is, and set
//       the useRefPosition to the def registers, for similar reasons to case #3.
//    5. If both the defRefPosition and the useRefPosition specify single registers, but both have conflicts,
//       We set the candidates on defRefPosition to be all regs of the appropriate type, and since they are
//       single registers, codegen can insert the copy.
//    6. Finally, if the RefPositions specify disjoint subsets of the registers (or the use is fixed but
//       has a conflict), we must insert a copy.  The copy will be inserted before the use if the
//       use is not fixed (in the fixed case, the code generator will insert the use).
//
// TODO-CQ: We get bad register allocation in case #3 in the situation where no register is
// available for the lifetime.  We end up allocating a register that must be spilled, and it probably
// won't be the register that is actually defined by the target instruction.  So, we have to copy it
// and THEN spill it.  In this case, we should be using the def requirement.  But we need to change
// the interface to this method a bit to make that work (e.g. returning a candidate set to use, but
// leaving the registerAssignment as-is on the def, so that if we find that we need to spill anyway
// we can use the fixed-reg on the def.
//

void LinearScan::resolveConflictingDefAndUse(Interval* interval, RefPosition* defRefPosition)
{
    assert(!interval->isLocalVar);

    RefPosition* useRefPosition   = defRefPosition->nextRefPosition;
    regMaskTP    defRegAssignment = defRefPosition->registerAssignment;
    regMaskTP    useRegAssignment = useRefPosition->registerAssignment;
    RegRecord*   defRegRecord     = nullptr;
    RegRecord*   useRegRecord     = nullptr;
    regNumber    defReg           = REG_NA;
    regNumber    useReg           = REG_NA;
    bool         defRegConflict   = ((defRegAssignment & useRegAssignment) == RBM_NONE);
    bool         useRegConflict   = defRegConflict;

    // If the useRefPosition is a "delayRegFree", we can't change the registerAssignment
    // on it, or we will fail to ensure that the fixedReg is busy at the time the target
    // (of the node that uses this interval) is allocated.
    bool canChangeUseAssignment = !useRefPosition->isFixedRegRef || !useRefPosition->delayRegFree;

    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CONFLICT));
    if (!canChangeUseAssignment)
    {
        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_FIXED_DELAY_USE));
    }
    if (defRefPosition->isFixedRegRef && !defRegConflict)
    {
        defReg       = defRefPosition->assignedReg();
        defRegRecord = getRegisterRecord(defReg);
        if (canChangeUseAssignment)
        {
            RefPosition* currFixedRegRefPosition = defRegRecord->recentRefPosition;
            assert(currFixedRegRefPosition != nullptr &&
                   currFixedRegRefPosition->nodeLocation == defRefPosition->nodeLocation);

            if (currFixedRegRefPosition->nextRefPosition == nullptr ||
                currFixedRegRefPosition->nextRefPosition->nodeLocation > useRefPosition->getRefEndLocation())
            {
                // This is case #1.  Use the defRegAssignment
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CASE1));
                useRefPosition->registerAssignment = defRegAssignment;
                return;
            }
            else
            {
                defRegConflict = true;
            }
        }
    }
    if (useRefPosition->isFixedRegRef && !useRegConflict)
    {
        useReg       = useRefPosition->assignedReg();
        useRegRecord = getRegisterRecord(useReg);

        // We know that useRefPosition is a fixed use, so the nextRefPosition must not be null.
        RefPosition* nextFixedRegRefPosition = useRegRecord->getNextRefPosition();
        assert(nextFixedRegRefPosition != nullptr &&
               nextFixedRegRefPosition->nodeLocation <= useRefPosition->nodeLocation);

        // First, check to see if there are any conflicting FixedReg references between the def and use.
        if (nextFixedRegRefPosition->nodeLocation == useRefPosition->nodeLocation)
        {
            // OK, no conflicting FixedReg references.
            // Now, check to see whether it is currently in use.
            if (useRegRecord->assignedInterval != nullptr)
            {
                RefPosition* possiblyConflictingRef         = useRegRecord->assignedInterval->recentRefPosition;
                LsraLocation possiblyConflictingRefLocation = possiblyConflictingRef->getRefEndLocation();
                if (possiblyConflictingRefLocation >= defRefPosition->nodeLocation)
                {
                    useRegConflict = true;
                }
            }
            if (!useRegConflict)
            {
                // This is case #2.  Use the useRegAssignment
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CASE2, interval));
                defRefPosition->registerAssignment = useRegAssignment;
                return;
            }
        }
        else
        {
            useRegConflict = true;
        }
    }
    if (defRegRecord != nullptr && !useRegConflict)
    {
        // This is case #3.
        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CASE3, interval));
        defRefPosition->registerAssignment = useRegAssignment;
        return;
    }
    if (useRegRecord != nullptr && !defRegConflict && canChangeUseAssignment)
    {
        // This is case #4.
        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CASE4, interval));
        useRefPosition->registerAssignment = defRegAssignment;
        return;
    }
    if (defRegRecord != nullptr && useRegRecord != nullptr)
    {
        // This is case #5.
        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CASE5, interval));
        RegisterType regType = interval->registerType;
        assert((getRegisterType(interval, defRefPosition) == regType) &&
               (getRegisterType(interval, useRefPosition) == regType));
        regMaskTP candidates               = allRegs(regType);
        defRefPosition->registerAssignment = candidates;
        defRefPosition->isFixedRegRef      = false;
        return;
    }
    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CASE6, interval));
    return;
}

//------------------------------------------------------------------------
// applyCalleeSaveHeuristics: Set register preferences for an interval based on the given RefPosition
//
// Arguments:
//    rp - The RefPosition of interest
//
// Notes:
//    This is slightly more general than its name applies, and updates preferences not just
//    for callee-save registers.
//
void LinearScan::applyCalleeSaveHeuristics(RefPosition* rp)
{
#ifdef TARGET_AMD64
    if (compiler->opts.compDbgEnC)
    {
        // We only use RSI and RDI for EnC code, so we don't want to favor callee-save regs.
        return;
    }
#endif // TARGET_AMD64

    Interval* theInterval = rp->getInterval();

#ifdef DEBUG
    if (!doReverseCallerCallee())
#endif // DEBUG
    {
        // Set preferences so that this register set will be preferred for earlier refs
        theInterval->mergeRegisterPreferences(rp->registerAssignment);
    }
}

//------------------------------------------------------------------------
// checkConflictingDefUse: Ensure that we have consistent def/use on SDSU temps.
//
// Arguments:
//    useRP - The use RefPosition of a tree temp (SDSU Interval)
//
// Notes:
//    There are a couple of cases where this may over-constrain allocation:
//    1. In the case of a non-commutative rmw def (in which the rmw source must be delay-free), or
//    2. In the case where the defining node requires a temp distinct from the target (also a
//       delay-free case).
//    In those cases, if we propagate a single-register restriction from the consumer to the producer
//    the delayed uses will not see a fixed reference in the PhysReg at that position, and may
//    incorrectly allocate that register.
//    TODO-CQ: This means that we may often require a copy at the use of this node's result.
//    This case could be moved to BuildRefPositionsForNode, at the point where the def RefPosition is
//    created, causing a RefTypeFixedReg to be added at that location. This, however, results in
//    more PhysReg RefPositions (a throughput impact), and a large number of diffs that require
//    further analysis to determine benefit.
//    See Issue #11274.
//
void LinearScan::checkConflictingDefUse(RefPosition* useRP)
{
    assert(useRP->refType == RefTypeUse);
    Interval* theInterval = useRP->getInterval();
    assert(!theInterval->isLocalVar);

    RefPosition* defRP = theInterval->firstRefPosition;

    // All defs must have a valid treeNode, but we check it below to be conservative.
    assert(defRP->treeNode != nullptr);
    regMaskTP prevAssignment = defRP->registerAssignment;
    regMaskTP newAssignment  = (prevAssignment & useRP->registerAssignment);
    if (newAssignment != RBM_NONE)
    {
        if (!isSingleRegister(newAssignment) || !theInterval->hasInterferingUses)
        {
            defRP->registerAssignment = newAssignment;
        }
    }
    else
    {
        theInterval->hasConflictingDefUse = true;
    }
}

//------------------------------------------------------------------------
// associateRefPosWithInterval: Update the Interval based on the given RefPosition.
//
// Arguments:
//    rp - The RefPosition of interest
//
// Notes:
//    This is called at the time when 'rp' has just been created, so it becomes
//    the nextRefPosition of the recentRefPosition, and both the recentRefPosition
//    and lastRefPosition of its referent.
//
void LinearScan::associateRefPosWithInterval(RefPosition* rp)
{
    Referenceable* theReferent = rp->referent;

    if (theReferent != nullptr)
    {
        // All RefPositions except the dummy ones at the beginning of blocks

        if (rp->isIntervalRef())
        {
            Interval* theInterval = rp->getInterval();

            applyCalleeSaveHeuristics(rp);

            if (theInterval->isLocalVar)
            {
                if (RefTypeIsUse(rp->refType))
                {
                    RefPosition* const prevRP = theInterval->recentRefPosition;
                    if ((prevRP != nullptr) && (prevRP->bbNum == rp->bbNum))
                    {
                        prevRP->lastUse = false;
                    }
                }

                rp->lastUse = (rp->refType != RefTypeExpUse) && (rp->refType != RefTypeParamDef) &&
                              (rp->refType != RefTypeZeroInit) && !extendLifetimes();
            }
            else if (rp->refType == RefTypeUse)
            {
                checkConflictingDefUse(rp);
                rp->lastUse = true;
            }
        }

        RefPosition* prevRP = theReferent->recentRefPosition;
        if (prevRP != nullptr)
        {
            prevRP->nextRefPosition = rp;
        }
        else
        {
            theReferent->firstRefPosition = rp;
        }
        theReferent->recentRefPosition = rp;
        theReferent->lastRefPosition   = rp;
    }
    else
    {
        assert((rp->refType == RefTypeBB) || (rp->refType == RefTypeKillGCRefs));
    }
}

//---------------------------------------------------------------------------
// newRefPosition: allocate and initialize a new RefPosition.
//
// Arguments:
//     reg             -  reg number that identifies RegRecord to be associated
//                        with this RefPosition
//     theLocation     -  LSRA location of RefPosition
//     theRefType      -  RefPosition type
//     theTreeNode     -  GenTree node for which this RefPosition is created
//     mask            -  Set of valid registers for this RefPosition
//     multiRegIdx     -  register position if this RefPosition corresponds to a
//                        multi-reg call node.
//
// Return Value:
//     a new RefPosition
//
RefPosition* LinearScan::newRefPosition(
    regNumber reg, LsraLocation theLocation, RefType theRefType, GenTree* theTreeNode, regMaskTP mask)
{
    RefPosition* newRP = newRefPositionRaw(theLocation, theTreeNode, theRefType);

    RegRecord* regRecord = getRegisterRecord(reg);
    newRP->setReg(regRecord);
    newRP->registerAssignment = mask;

    newRP->setMultiRegIdx(0);
    newRP->setRegOptional(false);

    // We can't have two RefPositions on a RegRecord at the same location, unless they are different types.
    assert((regRecord->lastRefPosition == nullptr) || (regRecord->lastRefPosition->nodeLocation < theLocation) ||
           (regRecord->lastRefPosition->refType != theRefType));
    associateRefPosWithInterval(newRP);

    DBEXEC(VERBOSE, newRP->dump(this));
    return newRP;
}

//---------------------------------------------------------------------------
// newRefPosition: allocate and initialize a new RefPosition.
//
// Arguments:
//     theInterval     -  interval to which RefPosition is associated with.
//     theLocation     -  LSRA location of RefPosition
//     theRefType      -  RefPosition type
//     theTreeNode     -  GenTree node for which this RefPosition is created
//     mask            -  Set of valid registers for this RefPosition
//     multiRegIdx     -  register position if this RefPosition corresponds to a
//                        multi-reg call node.
//
// Return Value:
//     a new RefPosition
//
RefPosition* LinearScan::newRefPosition(Interval*    theInterval,
                                        LsraLocation theLocation,
                                        RefType      theRefType,
                                        GenTree*     theTreeNode,
                                        regMaskTP    mask,
                                        unsigned     multiRegIdx /* = 0 */)
{
    if (theInterval != nullptr)
    {
        if (mask == RBM_NONE)
        {
            mask = allRegs(theInterval->registerType);
        }
    }
    else
    {
        assert(theRefType == RefTypeBB || theRefType == RefTypeKillGCRefs);
    }
#ifdef DEBUG
    if (theInterval != nullptr && regType(theInterval->registerType) == FloatRegisterType)
    {
        // In the case we're using floating point registers we must make sure
        // this flag was set previously in the compiler since this will mandate
        // whether LSRA will take into consideration FP reg killsets.
        assert(compiler->compFloatingPointUsed || ((mask & RBM_FLT_CALLEE_SAVED) == 0));
    }
#endif // DEBUG

    // If this reference is constrained to a single register (and it's not a dummy
    // or Kill reftype already), add a RefTypeFixedReg at this location so that its
    // availability can be more accurately determined

    bool isFixedRegister = isSingleRegister(mask);
    bool insertFixedRef  = false;
    if (isFixedRegister)
    {
        // Insert a RefTypeFixedReg for any normal def or use (not ParamDef or BB),
        // but not an internal use (it will already have a FixedRef for the def).
        if ((theRefType == RefTypeDef) || ((theRefType == RefTypeUse) && !theInterval->isInternal))
        {
            insertFixedRef = true;
        }
    }

    if (insertFixedRef)
    {
        regNumber    physicalReg = genRegNumFromMask(mask);
        RefPosition* pos         = newRefPosition(physicalReg, theLocation, RefTypeFixedReg, nullptr, mask);
        assert(theInterval != nullptr);
#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        // The LoongArch64's ABI which the float args maybe passed by integer register
        // when no float register left but free integer register.
        assert((regType(theInterval->registerType) == FloatRegisterType) ||
               (allRegs(theInterval->registerType) & mask) != 0);
#else
        assert((allRegs(theInterval->registerType) & mask) != 0);
#endif
    }

    RefPosition* newRP = newRefPositionRaw(theLocation, theTreeNode, theRefType);

    newRP->setInterval(theInterval);

    // Spill info
    newRP->isFixedRegRef = isFixedRegister;

#ifndef TARGET_AMD64
    // We don't need this for AMD because the PInvoke method epilog code is explicit
    // at register allocation time.
    if (theInterval != nullptr && theInterval->isLocalVar && compiler->compMethodRequiresPInvokeFrame() &&
        theInterval->varNum == compiler->genReturnLocal)
    {
        mask &= ~(RBM_PINVOKE_TCB | RBM_PINVOKE_FRAME);
        noway_assert(mask != RBM_NONE);
    }
#endif // !TARGET_AMD64
    newRP->registerAssignment = mask;

    newRP->setMultiRegIdx(multiRegIdx);
    newRP->setRegOptional(false);

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    newRP->skipSaveRestore = false;
#endif

    associateRefPosWithInterval(newRP);

    if (RefTypeIsDef(newRP->refType))
    {
        assert(theInterval != nullptr);
        theInterval->isSingleDef = theInterval->firstRefPosition == newRP;
    }

    DBEXEC(VERBOSE, newRP->dump(this));
    return newRP;
}

//------------------------------------------------------------------------
// IsContainableMemoryOp: Checks whether this is a memory op that can be contained.
//
// Arguments:
//    node        - the node of interest.
//
// Return value:
//    True if this will definitely be a memory reference that could be contained.
//
// Notes:
//    This differs from the isMemoryOp() method on GenTree because it checks for
//    the case of doNotEnregister local. This won't include locals that
//    for some other reason do not become register candidates, nor those that get
//    spilled.
//    Also, because we usually call this before we redo dataflow, any new lclVars
//    introduced after the last dataflow analysis will not yet be marked lvTracked,
//    so we don't use that.
//
bool LinearScan::isContainableMemoryOp(GenTree* node)
{
    if (node->isMemoryOp())
    {
        return true;
    }
    if (node->IsLocal())
    {
        if (!enregisterLocalVars)
        {
            return true;
        }
        const LclVarDsc* varDsc = compiler->lvaGetDesc(node->AsLclVar());
        return varDsc->lvDoNotEnregister;
    }
    return false;
}

//------------------------------------------------------------------------
// addRefsForPhysRegMask: Adds RefPositions of the given type for all the registers in 'mask'.
//
// Arguments:
//    mask        - the mask (set) of registers.
//    currentLoc  - the location at which they should be added
//    refType     - the type of refposition
//    isLastUse   - true IFF this is a last use of the register
//
void LinearScan::addRefsForPhysRegMask(regMaskTP mask, LsraLocation currentLoc, RefType refType, bool isLastUse)
{
    assert(refType == RefTypeKill);

    // The mask identifies a set of registers that will be used during
    // codegen. Mark these as modified here, so when we do final frame
    // layout, we'll know about all these registers. This is especially
    // important if mask contains callee-saved registers, which affect the
    // frame size since we need to save/restore them. In the case where we
    // have a copyBlk with GC pointers, can need to call the
    // CORINFO_HELP_ASSIGN_BYREF helper, which kills callee-saved RSI and
    // RDI, if LSRA doesn't assign RSI/RDI, they wouldn't get marked as
    // modified until codegen, which is too late.
    compiler->codeGen->regSet.rsSetRegsModified(mask DEBUGARG(true));

    for (regMaskTP candidates = mask; candidates != RBM_NONE;)
    {
        regNumber reg = genFirstRegNumFromMaskAndToggle(candidates);
        // This assumes that these are all "special" RefTypes that
        // don't need to be recorded on the tree (hence treeNode is nullptr)
        RefPosition* pos = newRefPosition(reg, currentLoc, refType, nullptr,
                                          genRegMask(reg)); // This MUST occupy the physical register (obviously)

        if (isLastUse)
        {
            pos->lastUse = true;
        }
    }
}

//------------------------------------------------------------------------
// getKillSetForStoreInd: Determine the liveness kill set for a GT_STOREIND node.
// If the GT_STOREIND will generate a write barrier, determine the specific kill
// set required by the case-specific, platform-specific write barrier. If no
// write barrier is required, the kill set will be RBM_NONE.
//
// Arguments:
//    tree - the GT_STOREIND node
//
// Return Value:    a register mask of the registers killed
//
regMaskTP LinearScan::getKillSetForStoreInd(GenTreeStoreInd* tree)
{
    assert(tree->OperIs(GT_STOREIND));

    regMaskTP killMask = RBM_NONE;

    GCInfo::WriteBarrierForm writeBarrierForm = compiler->codeGen->gcInfo.gcIsWriteBarrierCandidate(tree);
    if (writeBarrierForm != GCInfo::WBF_NoBarrier)
    {
        if (compiler->codeGen->genUseOptimizedWriteBarriers(writeBarrierForm))
        {
            // We can't determine the exact helper to be used at this point, because it depends on
            // the allocated register for the `data` operand. However, all the (x86) optimized
            // helpers have the same kill set: EDX. And note that currently, only x86 can return
            // `true` for genUseOptimizedWriteBarriers().
            killMask = RBM_CALLEE_TRASH_NOGC;
        }
        else
        {
            // Figure out which helper we're going to use, and then get the kill set for that helper.
            CorInfoHelpFunc helper = compiler->codeGen->genWriteBarrierHelperForWriteBarrierForm(writeBarrierForm);
            killMask               = compiler->compHelperCallKillSet(helper);
        }
    }
    return killMask;
}

//------------------------------------------------------------------------
// getKillSetForShiftRotate: Determine the liveness kill set for a shift or rotate node.
//
// Arguments:
//    shiftNode - the shift or rotate node
//
// Return Value:    a register mask of the registers killed
//
regMaskTP LinearScan::getKillSetForShiftRotate(GenTreeOp* shiftNode)
{
    regMaskTP killMask = RBM_NONE;
#ifdef TARGET_XARCH
    assert(shiftNode->OperIsShiftOrRotate());
    GenTree* shiftBy = shiftNode->gtGetOp2();
    if (!shiftBy->isContained())
    {
        killMask = RBM_RCX;
    }
#endif // TARGET_XARCH
    return killMask;
}

//------------------------------------------------------------------------
// getKillSetForMul: Determine the liveness kill set for a multiply node.
//
// Arguments:
//    tree - the multiply node
//
// Return Value:    a register mask of the registers killed
//
regMaskTP LinearScan::getKillSetForMul(GenTreeOp* mulNode)
{
    regMaskTP killMask = RBM_NONE;
#ifdef TARGET_XARCH
    assert(mulNode->OperIsMul());
    if (!mulNode->OperIs(GT_MUL) || (((mulNode->gtFlags & GTF_UNSIGNED) != 0) && mulNode->gtOverflowEx()))
    {
        killMask = RBM_RAX | RBM_RDX;
    }
#endif // TARGET_XARCH
    return killMask;
}

//------------------------------------------------------------------------
// getKillSetForModDiv: Determine the liveness kill set for a mod or div node.
//
// Arguments:
//    tree - the mod or div node as a GenTreeOp
//
// Return Value:    a register mask of the registers killed
//
regMaskTP LinearScan::getKillSetForModDiv(GenTreeOp* node)
{
    regMaskTP killMask = RBM_NONE;
#ifdef TARGET_XARCH
    assert(node->OperIs(GT_MOD, GT_DIV, GT_UMOD, GT_UDIV));
    if (varTypeUsesIntReg(node->TypeGet()))
    {
        // Both RAX and RDX are killed by the operation
        killMask = RBM_RAX | RBM_RDX;
    }
#endif // TARGET_XARCH
    return killMask;
}

//------------------------------------------------------------------------
// getKillSetForCall: Determine the liveness kill set for a call node.
//
// Arguments:
//    tree - the GenTreeCall node
//
// Return Value:    a register mask of the registers killed
//
regMaskTP LinearScan::getKillSetForCall(GenTreeCall* call)
{
    regMaskTP killMask = RBM_CALLEE_TRASH;
#ifdef TARGET_X86
    if (compiler->compFloatingPointUsed)
    {
        if (call->TypeGet() == TYP_DOUBLE)
        {
            needDoubleTmpForFPCall = true;
        }
        else if (call->TypeGet() == TYP_FLOAT)
        {
            needFloatTmpForFPCall = true;
        }
    }
#endif // TARGET_X86
    if (call->IsHelperCall())
    {
        CorInfoHelpFunc helpFunc = compiler->eeGetHelperNum(call->gtCallMethHnd);
        killMask                 = compiler->compHelperCallKillSet(helpFunc);
    }

    // if there is no FP used, we can ignore the FP kills
    if (!compiler->compFloatingPointUsed)
    {
#if defined(TARGET_XARCH)
        killMask &= ~(RBM_FLT_CALLEE_TRASH | RBM_MSK_CALLEE_TRASH);
#else
        killMask &= ~RBM_FLT_CALLEE_TRASH;
#endif // TARGET_XARCH
    }
#ifdef TARGET_ARM
    if (call->IsVirtualStub())
    {
        killMask |= compiler->virtualStubParamInfo->GetRegMask();
    }
#else  // !TARGET_ARM
    // Verify that the special virtual stub call registers are in the kill mask.
    // We don't just add them unconditionally to the killMask because for most architectures
    // they are already in the RBM_CALLEE_TRASH set,
    // and we don't want to introduce extra checks and calls in this hot function.
    assert(!call->IsVirtualStub() ||
           ((killMask & compiler->virtualStubParamInfo->GetRegMask()) == compiler->virtualStubParamInfo->GetRegMask()));
#endif // !TARGET_ARM
    return killMask;
}

//------------------------------------------------------------------------
// getKillSetForBlockStore: Determine the liveness kill set for a block store node.
//
// Arguments:
//    tree - the block store node as a GenTreeBlk
//
// Return Value:    a register mask of the registers killed
//
regMaskTP LinearScan::getKillSetForBlockStore(GenTreeBlk* blkNode)
{
    assert(blkNode->OperIsStoreBlk());
    regMaskTP killMask = RBM_NONE;

    bool isCopyBlk = varTypeIsStruct(blkNode->Data());
    switch (blkNode->gtBlkOpKind)
    {
        case GenTreeBlk::BlkOpKindCpObjUnroll:
#ifdef TARGET_XARCH
        case GenTreeBlk::BlkOpKindCpObjRepInstr:
#endif // TARGET_XARCH
            assert(isCopyBlk && blkNode->AsBlk()->GetLayout()->HasGCPtr());
            killMask = compiler->compHelperCallKillSet(CORINFO_HELP_ASSIGN_BYREF);
            break;

#ifndef TARGET_X86
        case GenTreeBlk::BlkOpKindHelper:
            if (isCopyBlk)
            {
                killMask = compiler->compHelperCallKillSet(CORINFO_HELP_MEMCPY);
            }
            else
            {
                killMask = compiler->compHelperCallKillSet(CORINFO_HELP_MEMSET);
            }
            break;
#endif
#ifdef TARGET_XARCH
        case GenTreeBlk::BlkOpKindRepInstr:
            if (isCopyBlk)
            {
                // rep movs kills RCX, RDI and RSI
                killMask = RBM_RCX | RBM_RDI | RBM_RSI;
            }
            else
            {
                // rep stos kills RCX and RDI.
                // (Note that the Data() node, if not constant, will be assigned to
                // RCX, but it's find that this kills it, as the value is not available
                // after this node in any case.)
                killMask = RBM_RDI | RBM_RCX;
            }
            break;
#endif
        case GenTreeBlk::BlkOpKindUnrollMemmove:
        case GenTreeBlk::BlkOpKindUnroll:
        case GenTreeBlk::BlkOpKindLoop:
        case GenTreeBlk::BlkOpKindInvalid:
            // for these 'gtBlkOpKind' kinds, we leave 'killMask' = RBM_NONE
            break;
    }

    return killMask;
}

#ifdef FEATURE_HW_INTRINSICS
//------------------------------------------------------------------------
// getKillSetForHWIntrinsic: Determine the liveness kill set for a GT_STOREIND node.
// If the GT_STOREIND will generate a write barrier, determine the specific kill
// set required by the case-specific, platform-specific write barrier. If no
// write barrier is required, the kill set will be RBM_NONE.
//
// Arguments:
//    tree - the GT_STOREIND node
//
// Return Value:    a register mask of the registers killed
//
regMaskTP LinearScan::getKillSetForHWIntrinsic(GenTreeHWIntrinsic* node)
{
    regMaskTP killMask = RBM_NONE;
#ifdef TARGET_XARCH
    switch (node->GetHWIntrinsicId())
    {
        case NI_SSE2_MaskMove:
            // maskmovdqu uses edi as the implicit address register.
            // Although it is set as the srcCandidate on the address, if there is also a fixed
            // assignment for the definition of the address, resolveConflictingDefAndUse() may
            // change the register assignment on the def or use of a tree temp (SDSU) when there
            // is a conflict, and the FixedRef on edi won't be sufficient to ensure that another
            // Interval will not be allocated there.
            // Issue #17674 tracks this.
            killMask = RBM_EDI;
            break;

        default:
            // Leave killMask as RBM_NONE
            break;
    }
#endif // TARGET_XARCH
    return killMask;
}
#endif // FEATURE_HW_INTRINSICS

//------------------------------------------------------------------------
// getKillSetForReturn: Determine the liveness kill set for a return node.
//
// Arguments:
//    NONE (this kill set is independent of the details of the specific return.)
//
// Return Value:    a register mask of the registers killed
//
regMaskTP LinearScan::getKillSetForReturn()
{
    return compiler->compIsProfilerHookNeeded() ? compiler->compHelperCallKillSet(CORINFO_HELP_PROF_FCN_LEAVE)
                                                : RBM_NONE;
}

//------------------------------------------------------------------------
// getKillSetForProfilerHook: Determine the liveness kill set for a profiler hook.
//
// Arguments:
//    NONE (this kill set is independent of the details of the specific node.)
//
// Return Value:    a register mask of the registers killed
//
regMaskTP LinearScan::getKillSetForProfilerHook()
{
    return compiler->compIsProfilerHookNeeded() ? compiler->compHelperCallKillSet(CORINFO_HELP_PROF_FCN_TAILCALL)
                                                : RBM_NONE;
}

#ifdef DEBUG
//------------------------------------------------------------------------
// getKillSetForNode:   Return the registers killed by the given tree node.
//
// Arguments:
//    tree       - the tree for which the kill set is needed.
//
// Return Value:    a register mask of the registers killed
//
regMaskTP LinearScan::getKillSetForNode(GenTree* tree)
{
    regMaskTP killMask = RBM_NONE;
    switch (tree->OperGet())
    {
        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROL:
        case GT_ROR:
#ifdef TARGET_X86
        case GT_LSH_HI:
        case GT_RSH_LO:
#endif
            killMask = getKillSetForShiftRotate(tree->AsOp());
            break;

        case GT_MUL:
        case GT_MULHI:
#if !defined(TARGET_64BIT) || defined(TARGET_ARM64)
        case GT_MUL_LONG:
#endif
            killMask = getKillSetForMul(tree->AsOp());
            break;

        case GT_MOD:
        case GT_DIV:
        case GT_UMOD:
        case GT_UDIV:
            killMask = getKillSetForModDiv(tree->AsOp());
            break;

        case GT_STORE_BLK:
        case GT_STORE_DYN_BLK:
            killMask = getKillSetForBlockStore(tree->AsBlk());
            break;

        case GT_RETURNTRAP:
            killMask = compiler->compHelperCallKillSet(CORINFO_HELP_STOP_FOR_GC);
            break;

        case GT_CALL:
            killMask = getKillSetForCall(tree->AsCall());

            break;
        case GT_STOREIND:
            killMask = getKillSetForStoreInd(tree->AsStoreInd());
            break;

#if defined(PROFILING_SUPPORTED)
        // If this method requires profiler ELT hook then mark these nodes as killing
        // callee trash registers (excluding RAX and XMM0). The reason for this is that
        // profiler callback would trash these registers. See vm\amd64\asmhelpers.asm for
        // more details.
        case GT_RETURN:
            killMask = getKillSetForReturn();
            break;

        case GT_PROF_HOOK:
            killMask = getKillSetForProfilerHook();
            break;
#endif // PROFILING_SUPPORTED

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            killMask = getKillSetForHWIntrinsic(tree->AsHWIntrinsic());
            break;
#endif // FEATURE_HW_INTRINSICS

        default:
            // for all other 'tree->OperGet()' kinds, leave 'killMask' = RBM_NONE
            break;
    }
    return killMask;
}
#endif // DEBUG

//------------------------------------------------------------------------
// buildKillPositionsForNode:
// Given some tree node add refpositions for all the registers this node kills
//
// Arguments:
//    tree       - the tree for which kill positions should be generated
//    currentLoc - the location at which the kills should be added
//    killMask   - The mask of registers killed by this node
//
// Return Value:
//    true       - kills were inserted
//    false      - no kills were inserted
//
// Notes:
//    The return value is needed because if we have any kills, we need to make sure that
//    all defs are located AFTER the kills.  On the other hand, if there aren't kills,
//    the multiple defs for a regPair are in different locations.
//    If we generate any kills, we will mark all currentLiveVars as being preferenced
//    to avoid the killed registers.  This is somewhat conservative.
//
//    This method can add kills even if killMask is RBM_NONE, if this tree is one of the
//    special cases that signals that we can't permit callee save registers to hold GC refs.

bool LinearScan::buildKillPositionsForNode(GenTree* tree, LsraLocation currentLoc, regMaskTP killMask)
{
    bool insertedKills = false;

    if (killMask != RBM_NONE)
    {
        addRefsForPhysRegMask(killMask, currentLoc, RefTypeKill, true);

        // TODO-CQ: It appears to be valuable for both fp and int registers to avoid killing the callee
        // save regs on infrequently executed paths.  However, it results in a large number of asmDiffs,
        // many of which appear to be regressions (because there is more spill on the infrequently path),
        // but are not really because the frequent path becomes smaller.  Validating these diffs will need
        // to be done before making this change.
        // Also note that we avoid setting callee-save preferences for floating point. This may need
        // revisiting, and note that it doesn't currently apply to SIMD types, only float or double.
        // if (!blockSequence[curBBSeqNum]->isRunRarely())
        if (enregisterLocalVars)
        {
            VarSetOps::Iter iter(compiler, currentLiveVars);
            unsigned        varIndex = 0;
            while (iter.NextElem(&varIndex))
            {
                LclVarDsc* varDsc = compiler->lvaGetDescByTrackedIndex(varIndex);
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                if (Compiler::varTypeNeedsPartialCalleeSave(varDsc->GetRegisterType()))
                {
                    if (!VarSetOps::IsMember(compiler, largeVectorCalleeSaveCandidateVars, varIndex))
                    {
                        continue;
                    }
                }
                else
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                    if (varTypeIsFloating(varDsc) &&
                        !VarSetOps::IsMember(compiler, fpCalleeSaveCandidateVars, varIndex))
                {
                    continue;
                }
                Interval*  interval   = getIntervalForLocalVar(varIndex);
                const bool isCallKill = ((killMask == RBM_INT_CALLEE_TRASH) || (killMask == RBM_CALLEE_TRASH));

                if (isCallKill)
                {
                    interval->preferCalleeSave = true;
                }

                // We are more conservative about allocating callee-saves registers to write-thru vars, since
                // a call only requires reloading after (not spilling before). So we record (above) the fact
                // that we'd prefer a callee-save register, but we don't update the preferences at this point.
                // See the "heuristics for writeThru intervals" in 'buildIntervals()'.
                if (!interval->isWriteThru || !isCallKill)
                {
                    regMaskTP newPreferences = allRegs(interval->registerType) & (~killMask);

                    if (newPreferences != RBM_NONE)
                    {
                        if (!interval->isWriteThru)
                        {
                            // Update the register aversion as long as this is not write-thru vars for
                            // reason mentioned above.
                            interval->registerAversion |= killMask;
                        }
                        interval->updateRegisterPreferences(newPreferences);
                    }
                    else
                    {
                        // If there are no callee-saved registers, the call could kill all the registers.
                        // This is a valid state, so in that case assert should not trigger. The RA will spill in order
                        // to free a register later.
                        assert(compiler->opts.compDbgEnC || (calleeSaveRegs(varDsc->lvType) == RBM_NONE) ||
                               varTypeIsStruct(varDsc->lvType));
                    }
                }
            }
        }

        insertedKills = true;
    }

    if (compiler->killGCRefs(tree))
    {
        RefPosition* pos =
            newRefPosition((Interval*)nullptr, currentLoc, RefTypeKillGCRefs, tree, (availableIntRegs & ~RBM_ARG_REGS));
        insertedKills = true;
    }

    return insertedKills;
}

//------------------------------------------------------------------------
// LinearScan::isCandidateMultiRegLclVar: Check whether a MultiReg node should
//                                        remain a candidate MultiReg
//
// Arguments:
//    lclNode - the GT_LCL_VAR or GT_STORE_LCL_VAR of interest
//
// Return Value:
//    true iff it remains a MultiReg lclVar.
//
// Notes:
//    When identifying candidates, the register allocator will only retain
//    promoted fields of a multi-reg local as candidates if all of its fields
//    are candidates. This is because of the added complexity of dealing with a
//    def or use of a multi-reg lclVar when only some of the fields have liveness
//    info.
//    At the time we determine whether a multi-reg lclVar can still be handled
//    as such, we've already completed Lowering, so during the build phase of
//    LSRA we have to reset the GTF_VAR_MULTIREG flag if necessary as we visit
//    each node.
//
bool LinearScan::isCandidateMultiRegLclVar(GenTreeLclVar* lclNode)
{
    assert(compiler->lvaEnregMultiRegVars && lclNode->IsMultiReg());
    LclVarDsc* varDsc = compiler->lvaGetDesc(lclNode);
    assert(varDsc->lvPromoted);
    bool isMultiReg = (compiler->lvaGetPromotionType(varDsc) == Compiler::PROMOTION_TYPE_INDEPENDENT);
    if (!isMultiReg)
    {
        lclNode->ClearMultiReg();
    }
#ifdef DEBUG
    for (unsigned int i = 0; i < varDsc->lvFieldCnt; i++)
    {
        LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(varDsc->lvFieldLclStart + i);
        assert(isCandidateVar(fieldVarDsc) == isMultiReg);
    }
#endif // DEBUG
    return isMultiReg;
}

//------------------------------------------------------------------------
// checkContainedOrCandidateLclVar: Check whether a GT_LCL_VAR node is a
//                                  candidate or contained.
//
// Arguments:
//    lclNode - the GT_LCL_VAR or GT_STORE_LCL_VAR of interest
//
// Return Value:
//    true if the node remains a candidate or is contained
//    false otherwise (i.e. if it will define a register)
//
// Notes:
//    We handle candidate variables differently from non-candidate ones.
//    If it is a candidate, we will simply add a use of it at its parent/consumer.
//    Otherwise, for a use we need to actually add the appropriate references for loading
//    or storing the variable.
//
//    A candidate lclVar won't actually get used until the appropriate ancestor node
//    is processed, unless this is marked "isLocalDefUse" because it is a stack-based argument
//    to a call or an orphaned dead node.
//
//    Also, because we do containment analysis before we redo dataflow and identify register
//    candidates, the containment analysis only uses !lvDoNotEnregister to estimate register
//    candidates.
//    If there is a lclVar that is estimated during Lowering to be register candidate but turns
//    out not to be, if a use was marked regOptional it should now be marked contained instead.
//
bool LinearScan::checkContainedOrCandidateLclVar(GenTreeLclVar* lclNode)
{
    bool isCandidate;
    bool makeContained = false;
    // We shouldn't be calling this if this node was already contained.
    assert(!lclNode->isContained());
    // If we have a multireg local, verify that its fields are still register candidates.
    if (lclNode->IsMultiReg())
    {
        // Multi-reg uses must support containment, but if we have an actual multi-reg local
        // we don't want it to be RegOptional in fixed-use cases, so that we can ensure proper
        // liveness modeling (e.g. if one field is in a register required by another field, in
        // a RegOptional case we won't handle the conflict properly if we decide not to allocate).
        isCandidate = isCandidateMultiRegLclVar(lclNode);
        if (isCandidate)
        {
            assert(!lclNode->IsRegOptional());
        }
        else
        {
            makeContained = true;
        }
    }
    else
    {
        isCandidate   = compiler->lvaGetDesc(lclNode)->lvLRACandidate;
        makeContained = !isCandidate && lclNode->IsRegOptional();
    }
    if (makeContained)
    {
        lclNode->ClearRegOptional();
        lclNode->SetContained();
        return true;
    }
    return isCandidate;
}

//----------------------------------------------------------------------------
// defineNewInternalTemp: Defines a ref position for an internal temp.
//
// Arguments:
//     tree                  -   GenTree node requiring an internal register
//     regType               -   Register type
//     currentLoc            -   Location of the temp Def position
//     regMask               -   register mask of candidates for temp
//
RefPosition* LinearScan::defineNewInternalTemp(GenTree* tree, RegisterType regType, regMaskTP regMask)
{
    Interval* current   = newInterval(regType);
    current->isInternal = true;
    RefPosition* newDef = newRefPosition(current, currentLoc, RefTypeDef, tree, regMask, 0);
    assert(internalCount < MaxInternalCount);
    internalDefs[internalCount++] = newDef;
    return newDef;
}

//------------------------------------------------------------------------
// buildInternalRegisterDefForNode - Create an Interval for an internal int register, and a def RefPosition
//
// Arguments:
//   tree                  - GenTree node that needs internal registers
//   internalCands         - The mask of valid registers
//
// Returns:
//   The def RefPosition created for this internal temp.
//
RefPosition* LinearScan::buildInternalIntRegisterDefForNode(GenTree* tree, regMaskTP internalCands)
{
    // The candidate set should contain only integer registers.
    assert((internalCands & ~availableIntRegs) == RBM_NONE);

    RefPosition* defRefPosition = defineNewInternalTemp(tree, IntRegisterType, internalCands);
    return defRefPosition;
}

//------------------------------------------------------------------------
// buildInternalFloatRegisterDefForNode - Create an Interval for an internal fp register, and a def RefPosition
//
// Arguments:
//   tree                  - GenTree node that needs internal registers
//   internalCands         - The mask of valid registers
//
// Returns:
//   The def RefPosition created for this internal temp.
//
RefPosition* LinearScan::buildInternalFloatRegisterDefForNode(GenTree* tree, regMaskTP internalCands)
{
    // The candidate set should contain only float registers.
    assert((internalCands & ~availableFloatRegs) == RBM_NONE);

    RefPosition* defRefPosition = defineNewInternalTemp(tree, FloatRegisterType, internalCands);
    return defRefPosition;
}

#if defined(FEATURE_SIMD) && defined(TARGET_XARCH)
RefPosition* LinearScan::buildInternalMaskRegisterDefForNode(GenTree* tree, regMaskTP internalCands)
{
    // The candidate set should contain only float registers.
    assert((internalCands & ~availableMaskRegs) == RBM_NONE);

    return defineNewInternalTemp(tree, MaskRegisterType, internalCands);
}
#endif

//------------------------------------------------------------------------
// buildInternalRegisterUses - adds use positions for internal
// registers required for tree node.
//
// Notes:
//   During the BuildNode process, calls to buildInternalIntRegisterDefForNode and
//   buildInternalFloatRegisterDefForNode put new RefPositions in the 'internalDefs'
//   array, and increment 'internalCount'. This method must be called to add corresponding
//   uses. It then resets the 'internalCount' for the handling of the next node.
//
//   If the internal registers must differ from the target register, 'setInternalRegsDelayFree'
//   must be set to true, so that the uses may be marked 'delayRegFree'.
//   Note that if a node has both float and int temps, generally the target with either be
//   int *or* float, and it is not really necessary to set this on the other type, but it does
//   no harm as it won't restrict the register selection.
//
void LinearScan::buildInternalRegisterUses()
{
    assert(internalCount <= MaxInternalCount);
    for (int i = 0; i < internalCount; i++)
    {
        RefPosition* def  = internalDefs[i];
        regMaskTP    mask = def->registerAssignment;
        RefPosition* use  = newRefPosition(def->getInterval(), currentLoc, RefTypeUse, def->treeNode, mask, 0);
        if (setInternalRegsDelayFree)
        {
            use->delayRegFree = true;
            pendingDelayFree  = true;
        }
    }
    // internalCount = 0;
}

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
//------------------------------------------------------------------------
// makeUpperVectorInterval - Create an Interval for saving and restoring
//                           the upper half of a large vector.
//
// Arguments:
//    varIndex - The tracked index for a large vector lclVar.
//
void LinearScan::makeUpperVectorInterval(unsigned varIndex)
{
    Interval* lclVarInterval = getIntervalForLocalVar(varIndex);
    assert(Compiler::varTypeNeedsPartialCalleeSave(lclVarInterval->registerType));
    Interval* newInt        = newInterval(LargeVectorSaveType);
    newInt->relatedInterval = lclVarInterval;
    newInt->isUpperVector   = true;
}

//------------------------------------------------------------------------
// getUpperVectorInterval - Get the Interval for saving and restoring
//                          the upper half of a large vector.
//
// Arguments:
//    varIndex - The tracked index for a large vector lclVar.
//
Interval* LinearScan::getUpperVectorInterval(unsigned varIndex)
{
    // TODO-Throughput: Consider creating a map from varIndex to upperVector interval.
    for (Interval& interval : intervals)
    {
        if (interval.isLocalVar)
        {
            continue;
        }
        noway_assert(interval.isUpperVector);
        if (interval.relatedInterval->getVarIndex(compiler) == varIndex)
        {
            return &interval;
        }
    }
    unreached();
}

//------------------------------------------------------------------------
// buildUpperVectorSaveRefPositions - Create special RefPositions for saving
//                                    the upper half of a set of large vectors.
//
// Arguments:
//    tree       - The current node being handled
//    currentLoc - The location of the current node
//    fpCalleeKillSet - The set of registers killed by this node.
//
// Notes: This is called by BuildDefsWithKills for any node that kills registers in the
//        RBM_FLT_CALLEE_TRASH set. We actually need to find any calls that kill the upper-half
//        of the callee-save vector registers.
//        But we will use as a proxy any node that kills floating point registers.
//        (Note that some calls are masquerading as other nodes at this point so we can't just check for calls.)
//
void LinearScan::buildUpperVectorSaveRefPositions(GenTree* tree, LsraLocation currentLoc, regMaskTP fpCalleeKillSet)
{
    if ((tree != nullptr) && tree->IsCall())
    {
        if (tree->AsCall()->IsNoReturn() || compiler->fgIsThrow(tree))
        {
            // No point in having vector save/restore if the call will not return.
            return;
        }
    }

    if (enregisterLocalVars && !VarSetOps::IsEmpty(compiler, largeVectorVars))
    {
        // We assume that the kill set includes at least some callee-trash registers, but
        // that it doesn't include any callee-save registers.
        assert((fpCalleeKillSet & RBM_FLT_CALLEE_TRASH) != RBM_NONE);
        assert((fpCalleeKillSet & RBM_FLT_CALLEE_SAVED) == RBM_NONE);

        // We only need to save the upper half of any large vector vars that are currently live.
        VARSET_TP       liveLargeVectors(VarSetOps::Intersection(compiler, currentLiveVars, largeVectorVars));
        VarSetOps::Iter iter(compiler, liveLargeVectors);
        unsigned        varIndex = 0;
        bool            blockAlwaysReturn =
            compiler->compCurBB->KindIs(BBJ_THROW, BBJ_EHFINALLYRET, BBJ_EHFAULTRET, BBJ_EHFILTERRET, BBJ_EHCATCHRET);

        while (iter.NextElem(&varIndex))
        {
            Interval* varInterval = getIntervalForLocalVar(varIndex);
            if (!varInterval->isPartiallySpilled)
            {
                Interval*    upperVectorInterval = getUpperVectorInterval(varIndex);
                RefPosition* pos =
                    newRefPosition(upperVectorInterval, currentLoc, RefTypeUpperVectorSave, tree, RBM_FLT_CALLEE_SAVED);
                varInterval->isPartiallySpilled = true;
                pos->skipSaveRestore            = blockAlwaysReturn;
#ifdef TARGET_XARCH
                pos->regOptional = true;
#endif
            }
        }
    }
    // For any non-lclVar intervals that are live at this point (i.e. in the DefList), we will also create
    // a RefTypeUpperVectorSave. For now these will all be spilled at this point, as we don't currently
    // have a mechanism to communicate any non-lclVar intervals that need to be restored.
    // TODO-CQ: We could consider adding such a mechanism, but it's unclear whether this rare
    // case of a large vector temp live across a call is worth the added complexity.
    for (RefInfoListNode *listNode = defList.Begin(), *end = defList.End(); listNode != end;
         listNode = listNode->Next())
    {
        const GenTree* defNode = listNode->treeNode;
        var_types      regType = defNode->TypeGet();
        if (regType == TYP_STRUCT)
        {
            assert(defNode->OperIs(GT_LCL_VAR, GT_CALL));
            if (defNode->OperIs(GT_LCL_VAR))
            {
                const GenTreeLclVar* lcl    = defNode->AsLclVar();
                const LclVarDsc*     varDsc = compiler->lvaGetDesc(lcl);
                regType                     = varDsc->GetRegisterType();
            }
            else
            {
                const GenTreeCall*          call      = defNode->AsCall();
                const CORINFO_CLASS_HANDLE  retClsHnd = call->gtRetClsHnd;
                Compiler::structPassingKind howToReturnStruct;
                regType = compiler->getReturnTypeForStruct(retClsHnd, call->GetUnmanagedCallConv(), &howToReturnStruct);
                if (howToReturnStruct == Compiler::SPK_ByValueAsHfa)
                {
                    regType = compiler->GetHfaType(retClsHnd);
                }
#if defined(TARGET_ARM64)
                else if (howToReturnStruct == Compiler::SPK_ByValue)
                {
                    // TODO-Cleanup: add a new Compiler::SPK for this case.
                    // This is the case when 16-byte struct is returned as [x0, x1].
                    // We don't need a partial callee save.
                    regType = TYP_LONG;
                }
#endif // TARGET_ARM64
            }
            assert((regType != TYP_STRUCT) && (regType != TYP_UNDEF));
        }
        if (Compiler::varTypeNeedsPartialCalleeSave(regType))
        {
            // In the rare case where such an interval is live across nested calls, we don't need to insert another.
            if (listNode->ref->getInterval()->recentRefPosition->refType != RefTypeUpperVectorSave)
            {
                RefPosition* pos = newRefPosition(listNode->ref->getInterval(), currentLoc, RefTypeUpperVectorSave,
                                                  tree, RBM_FLT_CALLEE_SAVED);
            }
        }
    }
}

//------------------------------------------------------------------------
// buildUpperVectorRestoreRefPosition - Create a RefPosition for restoring
//                                      the upper half of a large vector.
//
// Arguments:
//    lclVarInterval - A lclVarInterval that is live at 'currentLoc'
//    currentLoc     - The current location for which we're building RefPositions
//    node           - The node, if any, that the restore would be inserted before.
//                     If null, the restore will be inserted at the end of the block.
//    isUse          - If the refPosition that is about to be created represents a use or not.
//                   - If not, it would be the one at the end of the block.
//    multiRegIdx    - Register position if this restore corresponds to a field of a multi reg node.
//
void LinearScan::buildUpperVectorRestoreRefPosition(
    Interval* lclVarInterval, LsraLocation currentLoc, GenTree* node, bool isUse, unsigned multiRegIdx)
{
    if (lclVarInterval->isPartiallySpilled)
    {
        unsigned     varIndex            = lclVarInterval->getVarIndex(compiler);
        Interval*    upperVectorInterval = getUpperVectorInterval(varIndex);
        RefPosition* savePos             = upperVectorInterval->recentRefPosition;
        RefPosition* restorePos =
            newRefPosition(upperVectorInterval, currentLoc, RefTypeUpperVectorRestore, node, RBM_NONE);
        lclVarInterval->isPartiallySpilled = false;

        restorePos->setMultiRegIdx(multiRegIdx);

        if (isUse)
        {
            // If there was a use of the restore before end of the block restore,
            // then it is needed and cannot be eliminated
            savePos->skipSaveRestore = false;
        }
        else
        {
            // otherwise, just do the whatever was decided for save position
            restorePos->skipSaveRestore = savePos->skipSaveRestore;
        }

#ifdef TARGET_XARCH
        restorePos->regOptional = true;
#endif
    }
}

#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

#ifdef DEBUG
//------------------------------------------------------------------------
// ComputeOperandDstCount: computes the number of registers defined by a
//                         node.
//
// For most nodes, this is simple:
// - Nodes that do not produce values (e.g. stores and other void-typed
//   nodes) and nodes that immediately use the registers they define
//   produce no registers
// - Nodes that are marked as defining N registers define N registers.
//
// For contained nodes, however, things are more complicated: for purposes
// of bookkeeping, a contained node is treated as producing the transitive
// closure of the registers produced by its sources.
//
// Arguments:
//    operand - The operand for which to compute a register count.
//
// Returns:
//    The number of registers defined by `operand`.
//
int LinearScan::ComputeOperandDstCount(GenTree* operand)
{
    if (operand->isContained())
    {
        int dstCount = 0;
        for (GenTree* op : operand->Operands())
        {
            dstCount += ComputeOperandDstCount(op);
        }

        return dstCount;
    }
    if (operand->IsUnusedValue())
    {
        // Operands that define an unused value do not produce any registers.
        return 0;
    }
    if (operand->IsValue())
    {
        // Operands that are values and are not contained, consume all of their operands
        // and produce one or more registers.
        return operand->GetRegisterDstCount(compiler);
    }
    else
    {
        // This must be one of the operand types that are neither contained nor produce a value.
        // Stores and void-typed operands may be encountered when processing call nodes, which contain
        // pointers to argument setup stores.
        assert(operand->OperIsStore() || operand->OperIsPutArgStk() || operand->TypeIs(TYP_VOID));
        return 0;
    }
}

//------------------------------------------------------------------------
// ComputeAvailableSrcCount: computes the number of registers available as
//                           sources for a node.
//
// This is simply the sum of the number of registers produced by each
// operand to the node.
//
// Arguments:
//    node - The node for which to compute a source count.
//
// Return Value:
//    The number of registers available as sources for `node`.
//
int LinearScan::ComputeAvailableSrcCount(GenTree* node)
{
    int numSources = 0;
    for (GenTree* operand : node->Operands())
    {
        numSources += ComputeOperandDstCount(operand);
    }

    return numSources;
}
#endif // DEBUG

//------------------------------------------------------------------------
// buildRefPositionsForNode: The main entry point for building the RefPositions
//                           and "tree temp" Intervals for a given node.
//
// Arguments:
//    tree       - The node for which we are building RefPositions
//    currentLoc - The LsraLocation of the given node
//
void LinearScan::buildRefPositionsForNode(GenTree* tree, LsraLocation currentLoc)
{
    // The set of internal temporary registers used by this node are stored in the
    // gtRsvdRegs register mask. Clear it out.
    tree->gtRsvdRegs = RBM_NONE;

#ifdef DEBUG
    if (VERBOSE)
    {
        dumpDefList();
        compiler->gtDispTree(tree, nullptr, nullptr, true);
    }
#endif // DEBUG

    if (tree->isContained())
    {
#ifdef TARGET_XARCH
        // On XArch we can have contained candidate lclVars if they are part of a RMW
        // address computation. In this case we need to check whether it is a last use.
        if (tree->IsLocal() && ((tree->gtFlags & GTF_VAR_DEATH) != 0))
        {
            LclVarDsc* const varDsc = compiler->lvaGetDesc(tree->AsLclVarCommon());
            if (isCandidateVar(varDsc))
            {
                assert(varDsc->lvTracked);
                unsigned varIndex = varDsc->lvVarIndex;
                VarSetOps::RemoveElemD(compiler, currentLiveVars, varIndex);

                UpdatePreferencesOfDyingLocal(getIntervalForLocalVar(varIndex));
            }
        }
#else  // TARGET_XARCH
        assert(!isCandidateLocalRef(tree));
#endif // TARGET_XARCH
        JITDUMP("Contained\n");
        return;
    }

#ifdef DEBUG
    // If we are constraining the registers for allocation, we will modify all the RefPositions
    // we've built for this node after we've created them. In order to do that, we'll remember
    // the last RefPosition prior to those created for this node.
    RefPositionIterator refPositionMark = refPositions.backPosition();
    int                 oldDefListCount = defList.Count();
    currBuildNode                       = tree;
#endif // DEBUG

    int consume = BuildNode(tree);

#ifdef DEBUG
    int newDefListCount = defList.Count();
    // Currently produce is unused, but need to strengthen an assert to check if produce is
    // as expected. See https://github.com/dotnet/runtime/issues/8678
    int produce = newDefListCount - oldDefListCount;
    assert((consume == 0) || (ComputeAvailableSrcCount(tree) == consume));

    // If we are constraining registers, modify all the RefPositions we've just built to specify the
    // minimum reg count required.
    if ((getStressLimitRegs() != LSRA_LIMIT_NONE) || (getSelectionHeuristics() != LSRA_SELECT_DEFAULT))
    {
        // The number of registers required for a tree node is the sum of
        //   { RefTypeUses } + { RefTypeDef for the node itself } + specialPutArgCount
        // This is the minimum set of registers that needs to be ensured in the candidate set of ref positions created.
        //
        // First, we count them.
        unsigned minRegCount = 0;

        RefPositionIterator iter = refPositionMark;
        for (iter++; iter != refPositions.end(); iter++)
        {
            RefPosition* newRefPosition = &(*iter);
            if (newRefPosition->isIntervalRef())
            {
                if ((newRefPosition->refType == RefTypeUse) ||
                    ((newRefPosition->refType == RefTypeDef) && !newRefPosition->getInterval()->isInternal))
                {
                    minRegCount++;
                }
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                else if (newRefPosition->refType == RefTypeUpperVectorSave)
                {
                    minRegCount++;
                }
#ifdef TARGET_ARM64
                else if (newRefPosition->needsConsecutive)
                {
                    assert(newRefPosition->refType == RefTypeUpperVectorRestore);
                    minRegCount++;
                }
#endif // TARGET_ARM64
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

#ifdef TARGET_ARM64
                if (newRefPosition->needsConsecutive)
                {
                    consecutiveRegistersLocation = newRefPosition->nodeLocation;
                }
#endif // TARGET_ARM64
                if (newRefPosition->getInterval()->isSpecialPutArg)
                {
                    minRegCount++;
                }
            }
        }

        if (tree->OperIsPutArgSplit())
        {
            // While we have attempted to account for any "specialPutArg" defs above, we're only looking at RefPositions
            // created for this node. We must be defining at least one register in the PutArgSplit, so conservatively
            // add one less than the maximum number of registers args to 'minRegCount'.
            minRegCount += MAX_REG_ARG - 1;
        }
        for (refPositionMark++; refPositionMark != refPositions.end(); refPositionMark++)
        {
            RefPosition* newRefPosition    = &(*refPositionMark);
            unsigned     minRegCountForRef = minRegCount;
            if (RefTypeIsUse(newRefPosition->refType) && newRefPosition->delayRegFree)
            {
                // If delayRegFree, then Use will interfere with the destination of the consuming node.
                // Therefore, we also need add the kill set of the consuming node to minRegCount.
                //
                // For example consider the following IR on x86, where v01 and v02
                // are method args coming in ecx and edx respectively.
                //   GT_DIV(v01, v02)
                //
                // For GT_DIV, the minRegCount will be 3 without adding kill set of GT_DIV node.
                //
                // Assume further JitStressRegs=2, which would constrain candidates to callee trashable
                // regs { eax, ecx, edx } on use positions of v01 and v02.  LSRA allocates ecx for v01.
                // The use position of v02 cannot be allocated a reg since it is marked delay-reg free and
                // {eax,edx} are getting killed before the def of GT_DIV.  For this reason, minRegCount for
                // the use position of v02 also needs to take into account the kill set of its consuming node.
                regMaskTP killMask = getKillSetForNode(tree);
                if (killMask != RBM_NONE)
                {
                    minRegCountForRef += genCountBits(killMask);
                }
            }
            else if ((newRefPosition->refType) == RefTypeDef && (newRefPosition->getInterval()->isSpecialPutArg))
            {
                minRegCountForRef++;
            }

            newRefPosition->minRegCandidateCount = minRegCountForRef;
            if (newRefPosition->IsActualRef() && doReverseCallerCallee())
            {
                Interval* interval       = newRefPosition->getInterval();
                regMaskTP oldAssignment  = newRefPosition->registerAssignment;
                regMaskTP calleeSaveMask = calleeSaveRegs(interval->registerType);
#ifdef TARGET_ARM64
                if (newRefPosition->isLiveAtConsecutiveRegistersLoc(consecutiveRegistersLocation))
                {
                    // If we are assigning to refPositions that has consecutive registers requirements, skip the
                    // limit stress for them, because there are high chances that many registers are busy for
                    // consecutive requirements and
                    // we do not have enough remaining for other refpositions (like operands). Likewise, skip for the
                    // definition node that comes after that, for which, all the registers are in "delayRegFree" state.
                }
                else
#endif // TARGET_ARM64
                {
                    newRefPosition->registerAssignment =
                        getConstrainedRegMask(newRefPosition, oldAssignment, calleeSaveMask, minRegCountForRef);
                }

                if ((newRefPosition->registerAssignment != oldAssignment) && (newRefPosition->refType == RefTypeUse) &&
                    !interval->isLocalVar)
                {
#ifdef TARGET_ARM64
                    RefPosition* defRefPos = interval->firstRefPosition;
                    assert(defRefPos->treeNode != nullptr);
                    if (defRefPos->isLiveAtConsecutiveRegistersLoc(consecutiveRegistersLocation))
                    {
                        // If a method has consecutive registers and we are assigning to use refPosition whose
                        // definition was from a location that has consecutive registers, skip the limit stress for
                        // them, because there are high chances that many registers are busy for consecutive
                        // requirements and marked as "delayRegFree" state. We do not have enough remaining for other
                        // refpositions.
                    }
                    else
#endif // TARGET_ARM64
                    {
                        checkConflictingDefUse(newRefPosition);
                    }
                }
            }
        }
        consecutiveRegistersLocation = MinLocation;
    }
#endif // DEBUG
    JITDUMP("\n");
}

static const regNumber lsraRegOrder[]   = {REG_VAR_ORDER};
const unsigned         lsraRegOrderSize = ArrLen(lsraRegOrder);

static const regNumber lsraRegOrderFlt[]   = {REG_VAR_ORDER_FLT};
const unsigned         lsraRegOrderFltSize = ArrLen(lsraRegOrderFlt);

#if defined(TARGET_AMD64)
static const regNumber lsraRegOrderFltEvex[]   = {REG_VAR_ORDER_FLT_EVEX};
const unsigned         lsraRegOrderFltEvexSize = ArrLen(lsraRegOrderFltEvex);
#endif //  TARGET_AMD64

#if defined(TARGET_XARCH)
static const regNumber lsraRegOrderMsk[]   = {REG_VAR_ORDER_MSK};
const unsigned         lsraRegOrderMskSize = ArrLen(lsraRegOrderMsk);
#endif // TARGET_XARCH

//------------------------------------------------------------------------
// buildPhysRegRecords: Make an interval for each physical register
//
void LinearScan::buildPhysRegRecords()
{
    for (regNumber reg = REG_FIRST; reg < AVAILABLE_REG_COUNT; reg = REG_NEXT(reg))
    {
        RegRecord* curr = &physRegs[reg];
        curr->init(reg);
    }
    for (unsigned int i = 0; i < lsraRegOrderSize; i++)
    {
        regNumber  reg  = lsraRegOrder[i];
        RegRecord* curr = &physRegs[reg];
        curr->regOrder  = (unsigned char)i;
    }

    // TODO-CQ: We build physRegRecords before building intervals
    // and refpositions. During building intervals/refposition, we
    // would know if there are floating points used. If we can know
    // that information before we build intervals, we can skip
    // initializing the floating registers.
    // For that `compFloatingPointUsed` should be set accurately
    // before invoking allocator.

    const regNumber* regOrderFlt;
    unsigned         regOrderFltSize;

#if defined(TARGET_AMD64)
    // x64 has additional registers available when EVEX is supported
    // and that causes a different ordering to be used since they are
    // callee trash and should appear at the end up the existing callee
    // trash set

    if (compiler->canUseEvexEncoding())
    {
        regOrderFlt     = &lsraRegOrderFltEvex[0];
        regOrderFltSize = lsraRegOrderFltEvexSize;
    }
    else
    {
        regOrderFlt     = &lsraRegOrderFlt[0];
        regOrderFltSize = lsraRegOrderFltSize;
    }
#else
    regOrderFlt     = &lsraRegOrderFlt[0];
    regOrderFltSize = lsraRegOrderFltSize;
#endif

    for (unsigned int i = 0; i < regOrderFltSize; i++)
    {
        regNumber  reg  = regOrderFlt[i];
        RegRecord* curr = &physRegs[reg];
        curr->regOrder  = (unsigned char)i;
    }

#if defined(TARGET_XARCH)
    // xarch has mask registers available when EVEX is supported

    if (compiler->canUseEvexEncoding())
    {
        for (unsigned int i = 0; i < lsraRegOrderMskSize; i++)
        {
            regNumber  reg  = lsraRegOrderMsk[i];
            RegRecord* curr = &physRegs[reg];
            curr->regOrder  = (unsigned char)i;
        }
    }
#endif // TARGET_XARCH
}

//------------------------------------------------------------------------
// insertZeroInitRefPositions: Handle lclVars that are live-in to the first block
//
// Notes:
//    Prior to calling this method, 'currentLiveVars' must be set to the set of register
//    candidate variables that are liveIn to the first block.
//    For each register candidate that is live-in to the first block:
//    - If it is a GC ref, or if compInitMem is set, a ZeroInit RefPosition will be created.
//    - Otherwise, it will be marked as spilled, since it will not be assigned a register
//      on entry and will be loaded from memory on the undefined path.
//      Note that, when the compInitMem option is not set, we may encounter these on
//      paths that are protected by the same condition as an earlier def. However, since
//      we don't do the analysis to determine this - and couldn't rely on always identifying
//      such cases even if we tried - we must conservatively treat the undefined path as
//      being possible. This is a relatively rare case, so the introduced conservatism is
//      not expected to warrant the analysis required to determine the best placement of
//      an initialization.
//
void LinearScan::insertZeroInitRefPositions()
{
    assert(enregisterLocalVars);
#ifdef DEBUG
    VARSET_TP expectedLiveVars(VarSetOps::Intersection(compiler, registerCandidateVars, compiler->fgFirstBB->bbLiveIn));
    assert(VarSetOps::Equal(compiler, currentLiveVars, expectedLiveVars));
#endif //  DEBUG

    // insert defs for this, then a block boundary

    VarSetOps::Iter iter(compiler, currentLiveVars);
    unsigned        varIndex = 0;
    while (iter.NextElem(&varIndex))
    {
        LclVarDsc* varDsc = compiler->lvaGetDescByTrackedIndex(varIndex);
        if (!varDsc->lvIsParam && isCandidateVar(varDsc))
        {
            JITDUMP("V%02u was live in to first block:", compiler->lvaTrackedIndexToLclNum(varIndex));
            Interval* interval = getIntervalForLocalVar(varIndex);
            if (compiler->info.compInitMem || varTypeIsGC(varDsc->TypeGet()))
            {
                varDsc->lvMustInit = true;

                // OSR will handle init of locals and promoted fields thereof
                if (compiler->lvaIsOSRLocal(compiler->lvaTrackedIndexToLclNum(varIndex)))
                {
                    JITDUMP(" will be initialized by OSR\n");
                    // setIntervalAsSpilled(interval);
                    varDsc->lvMustInit = false;
                }

                JITDUMP(" creating ZeroInit\n");
                RefPosition* pos = newRefPosition(interval, MinLocation, RefTypeZeroInit, nullptr /* theTreeNode */,
                                                  allRegs(interval->registerType));
                pos->setRegOptional(true);
            }
            else
            {
                setIntervalAsSpilled(interval);
                JITDUMP(" marking as spilled\n");
            }
        }
    }

    // We must also insert zero-inits for any finallyVars if they are refs or if compInitMem is true.
    if (compiler->lvaEnregEHVars)
    {
        VarSetOps::Iter iter(compiler, finallyVars);
        unsigned        varIndex = 0;
        while (iter.NextElem(&varIndex))
        {
            LclVarDsc* varDsc = compiler->lvaGetDescByTrackedIndex(varIndex);
            if (!varDsc->lvIsParam && isCandidateVar(varDsc))
            {
                JITDUMP("V%02u is a finally var:", compiler->lvaTrackedIndexToLclNum(varIndex));
                Interval* interval = getIntervalForLocalVar(varIndex);
                if (compiler->info.compInitMem || varTypeIsGC(varDsc->TypeGet()))
                {
                    if (interval->recentRefPosition == nullptr)
                    {
                        JITDUMP(" creating ZeroInit\n");
                        RefPosition* pos = newRefPosition(interval, MinLocation, RefTypeZeroInit,
                                                          nullptr /* theTreeNode */, allRegs(interval->registerType));
                        pos->setRegOptional(true);
                        varDsc->lvMustInit = true;
                    }
                    else
                    {
                        // We must only generate one entry RefPosition for each Interval. Since this is not
                        // a parameter, it can't be RefTypeParamDef, so it must be RefTypeZeroInit, which
                        // we must have generated for the live-in case above.
                        assert(interval->recentRefPosition->refType == RefTypeZeroInit);
                        JITDUMP(" already ZeroInited\n");
                    }
                }
            }
        }
    }
}

#if defined(UNIX_AMD64_ABI) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
//------------------------------------------------------------------------
// UpdateRegStateForStructArg:
//    Sets the register state for an argument of type STRUCT.
//    This is shared between with AMD64's SystemV systems and LoongArch64-ABI.
//
// Arguments:
//    argDsc - the LclVarDsc for the argument of interest
//
// Notes:
//     See Compiler::raUpdateRegStateForArg(RegState *regState, LclVarDsc *argDsc) in regalloc.cpp
//         for how state for argument is updated for unix non-structs and Windows AMD64 structs.
//
void LinearScan::UpdateRegStateForStructArg(LclVarDsc* argDsc)
{
    assert(varTypeIsStruct(argDsc));
    RegState* intRegState   = &compiler->codeGen->intRegState;
    RegState* floatRegState = &compiler->codeGen->floatRegState;

    if ((argDsc->GetArgReg() != REG_STK) && (argDsc->GetArgReg() != REG_NA))
    {
        if (genRegMask(argDsc->GetArgReg()) & (RBM_ALLFLOAT))
        {
            assert(genRegMask(argDsc->GetArgReg()) & (RBM_FLTARG_REGS));
            floatRegState->rsCalleeRegArgMaskLiveIn |= genRegMask(argDsc->GetArgReg());
        }
        else
        {
            assert(genRegMask(argDsc->GetArgReg()) & (RBM_ARG_REGS));
            intRegState->rsCalleeRegArgMaskLiveIn |= genRegMask(argDsc->GetArgReg());
        }
    }

    if ((argDsc->GetOtherArgReg() != REG_STK) && (argDsc->GetOtherArgReg() != REG_NA))
    {
        if (genRegMask(argDsc->GetOtherArgReg()) & (RBM_ALLFLOAT))
        {
            assert(genRegMask(argDsc->GetOtherArgReg()) & (RBM_FLTARG_REGS));
            floatRegState->rsCalleeRegArgMaskLiveIn |= genRegMask(argDsc->GetOtherArgReg());
        }
        else
        {
            assert(genRegMask(argDsc->GetOtherArgReg()) & (RBM_ARG_REGS));
            intRegState->rsCalleeRegArgMaskLiveIn |= genRegMask(argDsc->GetOtherArgReg());
        }
    }
}

#endif // defined(UNIX_AMD64_ABI) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

//------------------------------------------------------------------------
// updateRegStateForArg: Updates rsCalleeRegArgMaskLiveIn for the appropriate
//    regState (either compiler->intRegState or compiler->floatRegState),
//    with the lvArgReg on "argDsc"
//
// Arguments:
//    argDsc - the argument for which the state is to be updated.
//
// Return Value: None
//
// Assumptions:
//    The argument is live on entry to the function
//    (or is untracked and therefore assumed live)
//
void LinearScan::updateRegStateForArg(LclVarDsc* argDsc)
{
#if defined(UNIX_AMD64_ABI) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    // For SystemV-AMD64 and LoongArch64 calls the argDsc
    // can have 2 registers (for structs.). Handle them here.
    if (varTypeIsStruct(argDsc))
    {
        UpdateRegStateForStructArg(argDsc);
    }
    else
#endif // defined(UNIX_AMD64_ABI) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    {
        RegState* intRegState   = &compiler->codeGen->intRegState;
        RegState* floatRegState = &compiler->codeGen->floatRegState;
        bool      isFloat       = emitter::isFloatReg(argDsc->GetArgReg());

        if (argDsc->lvIsHfaRegArg())
        {
            isFloat = true;
        }

        if (isFloat)
        {
            JITDUMP("Float arg V%02u in reg %s\n", compiler->lvaGetLclNum(argDsc), getRegName(argDsc->GetArgReg()));
            compiler->raUpdateRegStateForArg(floatRegState, argDsc);
        }
        else
        {
            JITDUMP("Int arg V%02u in reg %s\n", compiler->lvaGetLclNum(argDsc), getRegName(argDsc->GetArgReg()));
#if FEATURE_MULTIREG_ARGS
            if (argDsc->GetOtherArgReg() != REG_NA)
            {
                JITDUMP("(second half) in reg %s\n", getRegName(argDsc->GetOtherArgReg()));
            }
#endif // FEATURE_MULTIREG_ARGS
            compiler->raUpdateRegStateForArg(intRegState, argDsc);
        }
    }
}

template void LinearScan::buildIntervals<true>();
template void LinearScan::buildIntervals<false>();

//------------------------------------------------------------------------
// buildIntervals: The main entry point for building the data structures over
//                 which we will do register allocation.
//
template <bool localVarsEnregistered>
void           LinearScan::buildIntervals()
{
    BasicBlock* block;

    JITDUMP("\nbuildIntervals ========\n");

    // Build (empty) records for all of the physical registers
    buildPhysRegRecords();

#ifdef DEBUG
    if (VERBOSE)
    {
        printf("\n-----------------\n");
        printf("LIVENESS:\n");
        printf("-----------------\n");
        for (BasicBlock* const block : compiler->Blocks())
        {
            printf(FMT_BB "\nuse: ", block->bbNum);
            dumpConvertedVarSet(compiler, block->bbVarUse);
            printf("\ndef: ");
            dumpConvertedVarSet(compiler, block->bbVarDef);
            printf("\n in: ");
            dumpConvertedVarSet(compiler, block->bbLiveIn);
            printf("\nout: ");
            dumpConvertedVarSet(compiler, block->bbLiveOut);
            printf("\n");
        }
    }
#endif // DEBUG

    resetRegState();

#if DOUBLE_ALIGN
    // We will determine whether we should double align the frame during
    // identifyCandidates(), but we initially assume that we will not.
    doDoubleAlign = false;
#endif

    identifyCandidates<localVarsEnregistered>();

    // Figure out if we're going to use a frame pointer. We need to do this before building
    // the ref positions, because those objects will embed the frame register in various register masks
    // if the frame pointer is not reserved. If we decide to have a frame pointer, setFrameType() will
    // remove the frame pointer from the masks.
    setFrameType();

    DBEXEC(VERBOSE, TupleStyleDump(LSRA_DUMP_PRE));

    // second part:
    JITDUMP("\nbuildIntervals second part ========\n");
    currentLoc = 0;
    // TODO-Cleanup: This duplicates prior behavior where entry (ParamDef) RefPositions were
    // being assigned the bbNum of the last block traversed in the 2nd phase of Lowering.
    // Previously, the block sequencing was done for the (formerly separate) Build pass,
    // and the curBBNum was left as the last block sequenced. This block was then used to set the
    // weight for the entry (ParamDef) RefPositions. It would be logical to set this to the
    // normalized entry weight (compiler->fgCalledCount), but that results in a net regression.
    if (!blockSequencingDone)
    {
        setBlockSequence();
    }

    // Next, create ParamDef RefPositions for all the tracked parameters, in order of their varIndex.
    // Assign these RefPositions to the (nonexistent) BB0.
    curBBNum = 0;

    RegState* intRegState                   = &compiler->codeGen->intRegState;
    RegState* floatRegState                 = &compiler->codeGen->floatRegState;
    intRegState->rsCalleeRegArgMaskLiveIn   = RBM_NONE;
    floatRegState->rsCalleeRegArgMaskLiveIn = RBM_NONE;
    regsInUseThisLocation                   = RBM_NONE;
    regsInUseNextLocation                   = RBM_NONE;

    for (unsigned int varIndex = 0; varIndex < compiler->lvaTrackedCount; varIndex++)
    {
        LclVarDsc* argDsc = compiler->lvaGetDescByTrackedIndex(varIndex);

        if (!argDsc->lvIsParam)
        {
            continue;
        }

        // Only reserve a register if the argument is actually used.
        // Is it dead on entry? If compJmpOpUsed is true, then the arguments
        // have to be kept alive, so we have to consider it as live on entry.
        // Use lvRefCnt instead of checking bbLiveIn because if it's volatile we
        // won't have done dataflow on it, but it needs to be marked as live-in so
        // it will get saved in the prolog.
        if (!compiler->compJmpOpUsed && argDsc->lvRefCnt() == 0 && !compiler->opts.compDbgCode)
        {
            continue;
        }

        if (argDsc->lvIsRegArg)
        {
            updateRegStateForArg(argDsc);
        }

        if (isCandidateVar(argDsc))
        {
            Interval*       interval = getIntervalForLocalVar(varIndex);
            const var_types regType  = argDsc->GetRegisterType();
            regMaskTP       mask     = allRegs(regType);
            if (argDsc->lvIsRegArg)
            {
                // Set this interval as currently assigned to that register
                regNumber inArgReg = argDsc->GetArgReg();
                assert(inArgReg < REG_COUNT);
                mask = genRegMask(inArgReg);
                assignPhysReg(inArgReg, interval);
                INDEBUG(registersToDump |= getRegMask(inArgReg, interval->registerType));
            }
            RefPosition* pos = newRefPosition(interval, MinLocation, RefTypeParamDef, nullptr, mask);
            pos->setRegOptional(true);
        }
        else if (argDsc->lvPromoted)
        {
            for (unsigned fieldVarNum = argDsc->lvFieldLclStart;
                 fieldVarNum < argDsc->lvFieldLclStart + argDsc->lvFieldCnt; ++fieldVarNum)
            {
                const LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(fieldVarNum);
                if (fieldVarDsc->lvLRACandidate)
                {
                    assert(fieldVarDsc->lvTracked);
                    Interval*    interval = getIntervalForLocalVar(fieldVarDsc->lvVarIndex);
                    RefPosition* pos =
                        newRefPosition(interval, MinLocation, RefTypeParamDef, nullptr, allRegs(TypeGet(fieldVarDsc)));
                    pos->setRegOptional(true);
                }
            }
        }
        else
        {
            // We can overwrite the register (i.e. codegen saves it on entry)
            assert(argDsc->lvRefCnt() == 0 || !argDsc->lvIsRegArg || argDsc->lvDoNotEnregister ||
                   !argDsc->lvLRACandidate || (varTypeIsFloating(argDsc->TypeGet()) && compiler->opts.compDbgCode));
        }
    }

    // Now set up the reg state for the non-tracked args
    // (We do this here because we want to generate the ParamDef RefPositions in tracked
    // order, so that loop doesn't hit the non-tracked args)

    for (unsigned argNum = 0; argNum < compiler->info.compArgsCount; argNum++)
    {
        LclVarDsc* argDsc = compiler->lvaGetDesc(argNum);

        if (argDsc->lvPromoted)
        {
            for (unsigned fieldVarNum = argDsc->lvFieldLclStart;
                 fieldVarNum < argDsc->lvFieldLclStart + argDsc->lvFieldCnt; ++fieldVarNum)
            {
                LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(fieldVarNum);
                noway_assert(fieldVarDsc->lvIsParam);
                if (!fieldVarDsc->lvTracked && fieldVarDsc->lvIsRegArg)
                {
                    updateRegStateForArg(fieldVarDsc);
                }
            }
        }
        else
        {
            noway_assert(argDsc->lvIsParam);
            if (!argDsc->lvTracked && argDsc->lvIsRegArg)
            {
                updateRegStateForArg(argDsc);
            }
        }
    }

    // If there is a secret stub param, it is also live in
    if (compiler->info.compPublishStubParam)
    {
        intRegState->rsCalleeRegArgMaskLiveIn |= RBM_SECRET_STUB_PARAM;
    }

    numPlacedArgLocals = 0;
    placedArgRegs      = RBM_NONE;

    BasicBlock* predBlock = nullptr;
    BasicBlock* prevBlock = nullptr;

    // Initialize currentLiveVars to the empty set.  We will set it to the current
    // live-in at the entry to each block (this will include the incoming args on
    // the first block).
    VarSetOps::AssignNoCopy(compiler, currentLiveVars, VarSetOps::MakeEmpty(compiler));

    for (block = startBlockSequence(); block != nullptr; block = moveToNextBlock())
    {
        JITDUMP("\nNEW BLOCK " FMT_BB "\n", block->bbNum);
        compiler->compCurBB = block;

        bool predBlockIsAllocated = false;
        predBlock                 = findPredBlockForLiveIn(block, prevBlock DEBUGARG(&predBlockIsAllocated));
        if (predBlock != nullptr)
        {
            JITDUMP("\n\nSetting " FMT_BB " as the predecessor for determining incoming variable registers of " FMT_BB
                    "\n",
                    predBlock->bbNum, block->bbNum);
            assert(predBlock->bbNum <= bbNumMaxBeforeResolution);
            blockInfo[block->bbNum].predBBNum = predBlock->bbNum;
        }

        if (localVarsEnregistered)
        {
            VarSetOps::AssignNoCopy(compiler, currentLiveVars,
                                    VarSetOps::Intersection(compiler, registerCandidateVars, block->bbLiveIn));

            if (block == compiler->fgFirstBB)
            {
                insertZeroInitRefPositions();
                // The first real location is at 1; 0 is for the entry.
                currentLoc = 1;
            }

            // For blocks that don't have EHBoundaryIn, we need DummyDefs for cases where "predBlock" isn't
            // really a predecessor.
            // Note that it's possible to have uses of uninitialized variables, in which case even the first
            // block may require DummyDefs, which we are not currently adding - this means that these variables
            // will always be considered to be in memory on entry (and reloaded when the use is encountered).
            // TODO-CQ: Consider how best to tune this.  Currently, if we create DummyDefs for uninitialized
            // variables (which may actually be initialized along the dynamically executed paths, but not
            // on all static paths), we wind up with excessive live ranges for some of these variables.

            if (!blockInfo[block->bbNum].hasEHBoundaryIn)
            {
                // Any lclVars live-in on a non-EH boundary edge are resolution candidates.
                VarSetOps::UnionD(compiler, resolutionCandidateVars, currentLiveVars);

                if (block != compiler->fgFirstBB)
                {
                    VARSET_TP newLiveIn(VarSetOps::MakeCopy(compiler, currentLiveVars));
                    if (predBlock != nullptr)
                    {
                        // Compute set difference: newLiveIn = currentLiveVars - predBlock->bbLiveOut
                        VarSetOps::DiffD(compiler, newLiveIn, predBlock->bbLiveOut);
                    }
                    // Don't create dummy defs for EH vars; we'll load them from the stack as/when needed.
                    VarSetOps::DiffD(compiler, newLiveIn, exceptVars);

                    // Create dummy def RefPositions

                    if (!VarSetOps::IsEmpty(compiler, newLiveIn))
                    {
                        // If we are using locations from a predecessor, we should never require DummyDefs.
                        assert(!predBlockIsAllocated);
                        JITDUMP("Creating dummy definitions\n");
                        VarSetOps::Iter iter(compiler, newLiveIn);
                        unsigned        varIndex = 0;
                        while (iter.NextElem(&varIndex))
                        {
                            // Add a dummyDef for any candidate vars that are in the "newLiveIn" set.
                            LclVarDsc* varDsc = compiler->lvaGetDescByTrackedIndex(varIndex);
                            assert(isCandidateVar(varDsc));
                            Interval*    interval = getIntervalForLocalVar(varIndex);
                            RefPosition* pos      = newRefPosition(interval, currentLoc, RefTypeDummyDef, nullptr,
                                                              allRegs(interval->registerType));
                            pos->setRegOptional(true);
                        }
                        JITDUMP("Finished creating dummy definitions\n\n");
                    }
                }
            }
        }

        // Add a dummy RefPosition to mark the block boundary.
        // Note that we do this AFTER adding the exposed uses above, because the
        // register positions for those exposed uses need to be recorded at
        // this point.

        RefPosition* pos = newRefPosition((Interval*)nullptr, currentLoc, RefTypeBB, nullptr, RBM_NONE);
        currentLoc += 2;
        JITDUMP("\n");

        if (firstColdLoc == MaxLocation)
        {
            if (block->isRunRarely())
            {
                firstColdLoc = currentLoc;
                JITDUMP("firstColdLoc = %d\n", firstColdLoc);
            }
        }
        else
        {
            // TODO: We'd like to assert the following but we don't currently ensure that only
            // "RunRarely" blocks are contiguous.
            // (The funclets will generally be last, but we don't follow layout order, so we
            // don't have to preserve that in the block sequence.)
            // assert(block->isRunRarely());
        }

        // For frame poisoning we generate code into scratch BB right after prolog since
        // otherwise the prolog might become too large. In this case we will put the poison immediate
        // into the scratch register, so it will be killed here.
        if (compiler->compShouldPoisonFrame() && compiler->fgFirstBBisScratch() && block == compiler->fgFirstBB)
        {
            regMaskTP killed;
#if defined(TARGET_XARCH)
            // Poisoning uses EAX for small vars and rep stosd that kills edi, ecx and eax for large vars.
            killed = RBM_EDI | RBM_ECX | RBM_EAX;
#else
            // Poisoning uses REG_SCRATCH for small vars and memset helper for big vars.
            killed = genRegMask(REG_SCRATCH) | compiler->compHelperCallKillSet(CORINFO_HELP_MEMSET);
#endif
            addRefsForPhysRegMask(killed, currentLoc + 1, RefTypeKill, true);
            currentLoc += 2;
        }

        LIR::Range& blockRange = LIR::AsRange(block);
        for (GenTree* node : blockRange)
        {
            // We increment the location of each tree node by 2 so that the node definition, if any,
            // is at a new location and doesn't interfere with the uses.
            // For multi-reg local stores, the 'BuildMultiRegStoreLoc' method will further increment the
            // location by 2 for each destination register beyond the first.
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
            node->gtSeqNum = currentLoc;
            // In DEBUG, we want to set the gtRegTag to GT_REGTAG_REG, so that subsequent dumps will show the register
            // value.
            // Although this looks like a no-op it sets the tag.
            node->SetRegNum(node->GetRegNum());
#endif

            buildRefPositionsForNode(node, currentLoc);

#ifdef DEBUG
            if (currentLoc > maxNodeLocation)
            {
                maxNodeLocation = currentLoc;
            }
#endif // DEBUG
            currentLoc += 2;
        }

        if (localVarsEnregistered)
        {
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            // At the end of each block, create upperVectorRestores for any largeVectorVars that may be
            // partiallySpilled (during the build phase all intervals will be marked isPartiallySpilled if
            // they *may) be partially spilled at any point.
            VarSetOps::Iter largeVectorVarsIter(compiler, largeVectorVars);
            unsigned        largeVectorVarIndex = 0;
            while (largeVectorVarsIter.NextElem(&largeVectorVarIndex))
            {
                Interval* lclVarInterval = getIntervalForLocalVar(largeVectorVarIndex);
                buildUpperVectorRestoreRefPosition(lclVarInterval, currentLoc, nullptr, false, 0);
            }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

            // Note: the visited set is cleared in LinearScan::doLinearScan()
            markBlockVisited(block);
            if (!defList.IsEmpty())
            {
                INDEBUG(dumpDefList());
                assert(!"Expected empty defList at end of block");
            }

            // Insert exposed uses for a lclVar that is live-out of 'block' but not live-in to the
            // next block, or any unvisited successors.
            // This will address lclVars that are live on a backedge, as well as those that are kept
            // live at a GT_JMP.
            //
            // Blocks ending with "jmp method" are marked as BBJ_HAS_JMP,
            // and jmp call is represented using GT_JMP node which is a leaf node.
            // Liveness phase keeps all the arguments of the method live till the end of
            // block by adding them to liveout set of the block containing GT_JMP.
            //
            // The target of a GT_JMP implicitly uses all the current method arguments, however
            // there are no actual references to them.  This can cause LSRA to assert, because
            // the variables are live but it sees no references.  In order to correctly model the
            // liveness of these arguments, we add dummy exposed uses, in the same manner as for
            // backward branches.  This will happen automatically via expUseSet.
            //
            // Note that a block ending with GT_JMP has no successors and hence the variables
            // for which dummy use ref positions are added are arguments of the method.

            VARSET_TP expUseSet(VarSetOps::MakeCopy(compiler, block->bbLiveOut));
            VarSetOps::IntersectionD(compiler, expUseSet, registerCandidateVars);
            BasicBlock* nextBlock = getNextBlock();
            if (nextBlock != nullptr)
            {
                VarSetOps::DiffD(compiler, expUseSet, nextBlock->bbLiveIn);
            }

            block->VisitAllSuccs(compiler, [=, &expUseSet](BasicBlock* succ) {
                if (VarSetOps::IsEmpty(compiler, expUseSet))
                {
                    return BasicBlockVisit::Abort;
                }

                if (!isBlockVisited(succ))
                {
                    VarSetOps::DiffD(compiler, expUseSet, succ->bbLiveIn);
                }

                return BasicBlockVisit::Continue;
            });

            if (!VarSetOps::IsEmpty(compiler, expUseSet))
            {
                JITDUMP("Exposed uses:\n");
                VarSetOps::Iter iter(compiler, expUseSet);
                unsigned        varIndex = 0;
                while (iter.NextElem(&varIndex))
                {
                    unsigned         varNum = compiler->lvaTrackedToVarNum[varIndex];
                    const LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
                    assert(isCandidateVar(varDsc));
                    Interval*    interval = getIntervalForLocalVar(varIndex);
                    RefPosition* pos =
                        newRefPosition(interval, currentLoc, RefTypeExpUse, nullptr, allRegs(interval->registerType));
                    pos->setRegOptional(true);
                }
            }

            // Clear the "last use" flag on any vars that are live-out from this block.
            VARSET_TP       bbLiveDefs(VarSetOps::Intersection(compiler, registerCandidateVars, block->bbLiveOut));
            VarSetOps::Iter iter(compiler, bbLiveDefs);
            unsigned        varIndex = 0;
            while (iter.NextElem(&varIndex))
            {
                unsigned         varNum = compiler->lvaTrackedToVarNum[varIndex];
                LclVarDsc* const varDsc = compiler->lvaGetDesc(varNum);
                assert(isCandidateVar(varDsc));
                RefPosition* const lastRP = getIntervalForLocalVar(varIndex)->lastRefPosition;
                // We should be able to assert that lastRP is non-null if it is live-out, but sometimes liveness
                // lies.
                if ((lastRP != nullptr) && (lastRP->bbNum == block->bbNum))
                {
                    lastRP->lastUse = false;
                }
            }

#ifdef DEBUG
            checkLastUses(block);

            if (VERBOSE)
            {
                printf("use: ");
                dumpConvertedVarSet(compiler, block->bbVarUse);
                printf("\ndef: ");
                dumpConvertedVarSet(compiler, block->bbVarDef);
                printf("\n");
            }
#endif // DEBUG
        }
        else
        {
            // Note: the visited set is cleared in LinearScan::doLinearScan()
            markBlockVisited(block);
            if (!defList.IsEmpty())
            {
                INDEBUG(dumpDefList());
                assert(!"Expected empty defList at end of block");
            }
        }

        prevBlock = block;
    }

    if (localVarsEnregistered)
    {
        if (compiler->lvaKeepAliveAndReportThis())
        {
            // If we need to KeepAliveAndReportThis, add a dummy exposed use of it at the end
            unsigned keepAliveVarNum = compiler->info.compThisArg;
            assert(compiler->info.compIsStatic == false);
            const LclVarDsc* varDsc = compiler->lvaGetDesc(keepAliveVarNum);
            if (isCandidateVar(varDsc))
            {
                JITDUMP("Adding exposed use of this, for lvaKeepAliveAndReportThis\n");
                Interval*    interval = getIntervalForLocalVar(varDsc->lvVarIndex);
                RefPosition* pos =
                    newRefPosition(interval, currentLoc, RefTypeExpUse, nullptr, allRegs(interval->registerType));
                pos->setRegOptional(true);
            }
        }
        // Adjust heuristics for writeThru intervals.
        if (compiler->compHndBBtabCount > 0)
        {
            VarSetOps::Iter iter(compiler, exceptVars);
            unsigned        varIndex = 0;
            while (iter.NextElem(&varIndex))
            {
                unsigned   varNum   = compiler->lvaTrackedToVarNum[varIndex];
                LclVarDsc* varDsc   = compiler->lvaGetDesc(varNum);
                Interval*  interval = getIntervalForLocalVar(varIndex);
                assert(interval->isWriteThru);
                weight_t weight = varDsc->lvRefCntWtd();

                // We'd like to only allocate registers for EH vars that have enough uses
                // to compensate for the additional registers being live (and for the possibility
                // that we may have to insert an additional copy).
                // However, we don't currently have that information available. Instead, we'll
                // aggressively assume that these vars are defined once, at their first RefPosition.
                //
                RefPosition* firstRefPosition = interval->firstRefPosition;

                // Incoming reg args are given an initial weight of 2 * BB_UNITY_WEIGHT
                // (see lvaComputeRefCounts(); this may be reviewed/changed in future).
                //
                weight_t initialWeight = (firstRefPosition->refType == RefTypeParamDef)
                                             ? (2 * BB_UNITY_WEIGHT)
                                             : blockInfo[firstRefPosition->bbNum].weight;
                weight -= initialWeight;

                // If the remaining weight is less than the initial weight, we'd like to allocate it only
                // opportunistically, but we don't currently have a mechanism to do so.
                // For now, we'll just avoid using callee-save registers if the weight is too low.
                if (interval->preferCalleeSave)
                {
                    // The benefit of a callee-save register isn't as high as it would be for a normal arg.
                    // We'll have at least the cost of saving & restoring the callee-save register,
                    // so we won't break even until we have at least 4 * BB_UNITY_WEIGHT.
                    // Given that we also don't have a good way to tell whether the variable is live
                    // across a call in the non-EH code, we'll be extra conservative about this.
                    // Note that for writeThru intervals we don't update the preferences to be only callee-save.
                    unsigned calleeSaveCount;

                    if (varTypeUsesIntReg(interval->registerType))
                    {
                        calleeSaveCount = CNT_CALLEE_ENREG;
                    }
#if defined(TARGET_XARCH) && defined(FEATURE_SIMD)
                    else if (varTypeUsesMaskReg(interval->registerType))
                    {
                        calleeSaveCount = CNT_CALLEE_SAVED_MASK;
                    }
#endif // TARGET_XARCH && FEATURE_SIMD
                    else
                    {
                        assert(varTypeUsesFloatReg(interval->registerType));
                        calleeSaveCount = CNT_CALLEE_SAVED_FLOAT;
                    }

                    if ((weight <= (BB_UNITY_WEIGHT * 7)) || varDsc->lvVarIndex >= calleeSaveCount)
                    {
                        // If this is relatively low weight, don't prefer callee-save at all.
                        interval->preferCalleeSave = false;
                    }
                    else
                    {
                        // In other cases, we'll add in the callee-save regs to the preferences, but not clear
                        // the non-callee-save regs . We also handle this case specially in tryAllocateFreeReg().
                        interval->registerPreferences |= calleeSaveRegs(interval->registerType);
                    }
                }
            }
        }

#ifdef DEBUG
        if (getLsraExtendLifeTimes())
        {
            for (unsigned lclNum = 0; lclNum < compiler->lvaCount; lclNum++)
            {
                LclVarDsc* varDsc = compiler->lvaGetDesc(lclNum);
                if (varDsc->lvLRACandidate)
                {
                    JITDUMP("Adding exposed use of V%02u for LsraExtendLifetimes\n", lclNum);
                    Interval*    interval = getIntervalForLocalVar(varDsc->lvVarIndex);
                    RefPosition* pos =
                        newRefPosition(interval, currentLoc, RefTypeExpUse, nullptr, allRegs(interval->registerType));
                    pos->setRegOptional(true);
                }
            }
        }
#endif // DEBUG
    }

    // If the last block has successors, create a RefTypeBB to record
    // what's live

    if (prevBlock->NumSucc(compiler) > 0)
    {
        RefPosition* pos = newRefPosition((Interval*)nullptr, currentLoc, RefTypeBB, nullptr, RBM_NONE);
    }

    needNonIntegerRegisters |= compiler->compFloatingPointUsed;
    if (!needNonIntegerRegisters)
    {
        availableRegCount = REG_INT_COUNT;
    }

    if (availableRegCount < (sizeof(regMaskTP) * 8))
    {
        // Mask out the bits that are between 64 ~ availableRegCount
        actualRegistersMask = (1ULL << availableRegCount) - 1;
    }
    else
    {
        actualRegistersMask = ~RBM_NONE;
    }

#ifdef DEBUG
    // Make sure we don't have any blocks that were not visited
    for (BasicBlock* const block : compiler->Blocks())
    {
        assert(isBlockVisited(block));
    }

    if (VERBOSE)
    {
        lsraDumpIntervals("BEFORE VALIDATING INTERVALS");
        dumpRefPositions("BEFORE VALIDATING INTERVALS");
    }
    validateIntervals();

#endif // DEBUG
}

#ifdef DEBUG
//------------------------------------------------------------------------
// validateIntervals: A DEBUG-only method that checks that:
//      - the lclVar RefPositions do not reflect uses of undefined values
//      - A singleDef interval should have just first RefPosition as RefTypeDef.
//
// TODO-Cleanup: If an undefined use is encountered, it merely prints a message
// but probably assert.
//
void LinearScan::validateIntervals()
{
    if (enregisterLocalVars)
    {
        JITDUMP("\n------------\n");
        JITDUMP("REFPOSITIONS DURING VALIDATE INTERVALS (RefPositions per interval)\n");
        JITDUMP("------------\n\n");

        for (unsigned i = 0; i < compiler->lvaTrackedCount; i++)
        {
            if (!compiler->lvaGetDescByTrackedIndex(i)->lvLRACandidate)
            {
                continue;
            }
            Interval* interval = getIntervalForLocalVar(i);

            bool     defined      = false;
            unsigned lastUseBBNum = 0;
            JITDUMP("-----------------\n");
            for (RefPosition* ref = interval->firstRefPosition; ref != nullptr; ref = ref->nextRefPosition)
            {
                if (VERBOSE)
                {
                    ref->dump(this);
                }
                RefType refType = ref->refType;
                if (!defined && RefTypeIsUse(refType) && (lastUseBBNum == ref->bbNum))
                {
                    if (!ref->lastUse)
                    {
                        if (compiler->info.compMethodName != nullptr)
                        {
                            JITDUMP("%s: ", compiler->info.compMethodName);
                        }
                        JITDUMP("LocalVar V%02u: undefined use at %u\n", interval->varNum, ref->nodeLocation);
                        assert(false);
                    }
                }

                // For single-def intervals, the only the first refposition should be a RefTypeDef
                if (interval->isSingleDef && RefTypeIsDef(refType))
                {
                    assert(ref == interval->firstRefPosition);
                }

                // Note that there can be multiple last uses if they are on disjoint paths,
                // so we can't really check the lastUse flag
                if (ref->lastUse)
                {
                    defined      = false;
                    lastUseBBNum = ref->bbNum;
                }
                if (RefTypeIsDef(refType))
                {
                    defined = true;
                }
            }
        }
    }
}
#endif // DEBUG

#ifndef TARGET_ARM
//------------------------------------------------------------------------
// setTgtPref: Set a  preference relationship between the given Interval
//             and a Use RefPosition.
//
// Arguments:
//    interval   - An interval whose defining instruction has tgtPrefUse as a use
//    tgtPrefUse - The use RefPosition
//
// Notes:
//    This is called when we would like tgtPrefUse and this def to get the same register.
//    This is only desirable if the use is a last use, which it is if it is a non-local,
//    *or* if it is a lastUse.
//     Note that we don't yet have valid lastUse information in the RefPositions that we're building
//    (every RefPosition is set as a lastUse until we encounter a new use), so we have to rely on the treeNode.
//    This may be called for multiple uses, in which case 'interval' will only get preferenced at most
//    to the first one (if it didn't already have a 'relatedInterval'.
//
void setTgtPref(Interval* interval, RefPosition* tgtPrefUse)
{
    if (tgtPrefUse != nullptr)
    {
        Interval* useInterval = tgtPrefUse->getInterval();
        if (!useInterval->isLocalVar || (tgtPrefUse->treeNode == nullptr) ||
            ((tgtPrefUse->treeNode->gtFlags & GTF_VAR_DEATH) != 0))
        {
            // Set the use interval as related to the interval we're defining.
            useInterval->assignRelatedIntervalIfUnassigned(interval);
        }
    }
}
#endif // !TARGET_ARM

//------------------------------------------------------------------------
// BuildDef: Build a RefTypeDef RefPosition for the given node
//
// Arguments:
//    tree          - The node that defines a register
//    dstCandidates - The candidate registers for the definition
//    multiRegIdx   - The index of the definition, defaults to zero.
//                    Only non-zero for multi-reg nodes.
//
// Return Value:
//    The newly created RefPosition.
//
// Notes:
//    Adds the RefInfo for the definition to the defList.
//
RefPosition* LinearScan::BuildDef(GenTree* tree, regMaskTP dstCandidates, int multiRegIdx)
{
    assert(!tree->isContained());

    if (dstCandidates != RBM_NONE)
    {
        assert((tree->GetRegNum() == REG_NA) || (dstCandidates == genRegMask(tree->GetRegByIndex(multiRegIdx))));
    }

    RegisterType type;
    if (!tree->IsMultiRegNode())
    {
        type = getDefType(tree);
    }
    else
    {
        type = tree->GetRegTypeByIndex(multiRegIdx);
    }

    if (!varTypeUsesIntReg(type))
    {
        compiler->compFloatingPointUsed = true;
    }

    Interval* interval = newInterval(type);
    if (tree->GetRegNum() != REG_NA)
    {
        if (!tree->IsMultiRegNode() || (multiRegIdx == 0))
        {
            assert((dstCandidates == RBM_NONE) || (dstCandidates == genRegMask(tree->GetRegNum())));
            dstCandidates = genRegMask(tree->GetRegNum());
        }
        else
        {
            assert(isSingleRegister(dstCandidates));
        }
    }
#ifdef TARGET_X86
    else if (varTypeIsByte(tree))
    {
        if (dstCandidates == RBM_NONE)
        {
            dstCandidates = availableIntRegs;
        }
        dstCandidates &= ~RBM_NON_BYTE_REGS;
        assert(dstCandidates != RBM_NONE);
    }
#endif // TARGET_X86
    if (pendingDelayFree)
    {
        interval->hasInterferingUses = true;
        // pendingDelayFree = false;
    }
    RefPosition* defRefPosition =
        newRefPosition(interval, currentLoc + 1, RefTypeDef, tree, dstCandidates, multiRegIdx);
    if (tree->IsUnusedValue())
    {
        defRefPosition->isLocalDefUse = true;
        defRefPosition->lastUse       = true;
    }
    else
    {
        RefInfoListNode* refInfo = listNodePool.GetNode(defRefPosition, tree);
        defList.Append(refInfo);
    }

#ifndef TARGET_ARM
    setTgtPref(interval, tgtPrefUse);
    setTgtPref(interval, tgtPrefUse2);
#endif // !TARGET_ARM

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    assert(!interval->isPartiallySpilled);
#endif

    return defRefPosition;
}

//------------------------------------------------------------------------
// BuildDef: Build one or more RefTypeDef RefPositions for the given node
//
// Arguments:
//    tree          - The node that defines a register
//    dstCount      - The number of registers defined by the node
//    dstCandidates - the candidate registers for the definition
//
// Notes:
//    Adds the RefInfo for the definitions to the defList.
//
void LinearScan::BuildDefs(GenTree* tree, int dstCount, regMaskTP dstCandidates)
{
    bool fixedReg = false;
    if ((dstCount > 1) && (dstCandidates != RBM_NONE) && ((int)genCountBits(dstCandidates) == dstCount))
    {
        fixedReg = true;
    }
    const ReturnTypeDesc* retTypeDesc = nullptr;
    if (tree->IsMultiRegCall())
    {
        retTypeDesc = tree->AsCall()->GetReturnTypeDesc();
    }
    for (int i = 0; i < dstCount; i++)
    {
        regMaskTP thisDstCandidates;
        if (fixedReg)
        {
            // In case of multi-reg call node, we have to query the i'th position return register.
            // For all other cases of multi-reg definitions, the registers must be in sequential order.
            if (retTypeDesc != nullptr)
            {
                thisDstCandidates = genRegMask(tree->AsCall()->GetReturnTypeDesc()->GetABIReturnReg(i));
                assert((dstCandidates & thisDstCandidates) != RBM_NONE);
            }
            else
            {
                thisDstCandidates = genFindLowestBit(dstCandidates);
            }
            dstCandidates &= ~thisDstCandidates;
        }
        else
        {
            thisDstCandidates = dstCandidates;
        }
        BuildDef(tree, thisDstCandidates, i);
    }
}

//------------------------------------------------------------------------
// BuildDef: Build one or more RefTypeDef RefPositions for the given node,
//           as well as kills as specified by the given mask.
//
// Arguments:
//    tree          - The node that defines a register
//    dstCount      - The number of registers defined by the node
//    dstCandidates - The candidate registers for the definition
//    killMask      - The mask of registers killed by this node
//
// Notes:
//    Adds the RefInfo for the definitions to the defList.
//    The def and kill functionality is folded into a single method so that the
//    save and restores of upper vector registers can be bracketed around the def.
//
void LinearScan::BuildDefsWithKills(GenTree* tree, int dstCount, regMaskTP dstCandidates, regMaskTP killMask)
{
    assert(killMask == getKillSetForNode(tree));

    // Call this even when killMask is RBM_NONE, as we have to check for some special cases
    buildKillPositionsForNode(tree, currentLoc + 1, killMask);

    if (killMask != RBM_NONE)
    {
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        // Build RefPositions to account for the fact that, even in a callee-save register, the upper half of any large
        // vector will be killed by a call.
        // We actually need to find any calls that kill the upper-half of the callee-save vector registers.
        // But we will use as a proxy any node that kills floating point registers.
        // (Note that some calls are masquerading as other nodes at this point so we can't just check for calls.)
        // We call this unconditionally for such nodes, as we will create RefPositions for any large vector tree temps
        // even if 'enregisterLocalVars' is false, or 'liveLargeVectors' is empty, though currently the allocation
        // phase will fully (rather than partially) spill those, so we don't need to build the UpperVectorRestore
        // RefPositions in that case.
        // This must be done after the kills, so that we know which large vectors are still live.
        //
        if ((killMask & RBM_FLT_CALLEE_TRASH) != RBM_NONE)
        {
            buildUpperVectorSaveRefPositions(tree, currentLoc + 1, killMask);
        }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    }

    // Now, create the Def(s)
    BuildDefs(tree, dstCount, dstCandidates);
}

//------------------------------------------------------------------------
// UpdatePreferencesOfDyingLocal: Update the preference of a dying local.
//
// Arguments:
//    interval - the interval for the local
//
// Notes:
//    The "dying" information here is approximate, see the comment in BuildUse.
//
void LinearScan::UpdatePreferencesOfDyingLocal(Interval* interval)
{
    assert(!VarSetOps::IsMember(compiler, currentLiveVars, interval->getVarIndex(compiler)));

    // If we see a use of a local between placing a register and a call then we
    // want to update that local's preferences to exclude the "placed" register.
    // Picking the "placed" register is otherwise going to force a spill.
    //
    // We only need to do this on liveness updates because if the local is live
    // _after_ the call, then we are going to prefer callee-saved registers for
    // such local anyway, so there is no need to look at such local uses.
    //
    if (placedArgRegs == RBM_NONE)
    {
        return;
    }

    // Write-thru locals are "free" to spill and we are quite conservative
    // about allocating them to callee-saved registers, so leave them alone
    // here.
    if (interval->isWriteThru)
    {
        return;
    }

    // Find the registers that we should remove from the preference set because
    // they are occupied with argument values.
    regMaskTP unpref   = placedArgRegs;
    unsigned  varIndex = interval->getVarIndex(compiler);
    for (size_t i = 0; i < numPlacedArgLocals; i++)
    {
        if (placedArgLocals[i].VarIndex == varIndex)
        {
            // This local's value is going to be available in this register so
            // keep it in the preferences.
            unpref &= ~genRegMask(placedArgLocals[i].Reg);
        }
    }

    if (unpref != RBM_NONE)
    {
#ifdef DEBUG
        if (VERBOSE)
        {
            printf("Last use of V%02u between PUTARG and CALL. Removing occupied arg regs from preferences: ",
                   compiler->lvaTrackedIndexToLclNum(varIndex));
            compiler->dumpRegMask(unpref);
            printf("\n");
        }
#endif

        interval->registerAversion |= unpref;
        regMaskTP newPreferences = allRegs(interval->registerType) & ~unpref;
        interval->updateRegisterPreferences(newPreferences);
    }
}

//------------------------------------------------------------------------
// BuildUse: Remove the RefInfoListNode for the given multi-reg index of the given node from
//           the defList, and build a use RefPosition for the associated Interval.
//
// Arguments:
//    operand             - The node of interest
//    candidates          - The register candidates for the use
//    multiRegIdx         - The index of the multireg def/use
//
// Return Value:
//    The newly created use RefPosition
//
// Notes:
//    The node must not be contained, and must have been processed by buildRefPositionsForNode().
//
RefPosition* LinearScan::BuildUse(GenTree* operand, regMaskTP candidates, int multiRegIdx)
{
    assert(!operand->isContained());
    Interval* interval;
    bool      regOptional = operand->IsRegOptional();

    if (isCandidateLocalRef(operand))
    {
        interval = getIntervalForLocalVarNode(operand->AsLclVarCommon());

        // We have only approximate last-use information at this point.  This is because the
        // execution order doesn't actually reflect the true order in which the localVars
        // are referenced - but the order of the RefPositions will, so we recompute it after
        // RefPositions are built.
        // Use the old value for setting currentLiveVars - note that we do this with the
        // not-quite-correct setting of lastUse.  However, this is OK because
        // 1) this is only for preferencing, which doesn't require strict correctness, and
        // 2) the cases where these out-of-order uses occur should not overlap a kill.
        // TODO-Throughput: clean this up once we have the execution order correct.  At that point
        // we can update currentLiveVars at the same place that we create the RefPosition.
        if ((operand->gtFlags & GTF_VAR_DEATH) != 0)
        {
            unsigned varIndex = interval->getVarIndex(compiler);
            VarSetOps::RemoveElemD(compiler, currentLiveVars, varIndex);
            UpdatePreferencesOfDyingLocal(interval);
        }
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        buildUpperVectorRestoreRefPosition(interval, currentLoc, operand, true, (unsigned)multiRegIdx);
#endif
    }
    else if (operand->IsMultiRegLclVar())
    {
        assert(compiler->lvaEnregMultiRegVars);
        LclVarDsc* varDsc      = compiler->lvaGetDesc(operand->AsLclVar());
        LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(varDsc->lvFieldLclStart + multiRegIdx);
        interval               = getIntervalForLocalVar(fieldVarDsc->lvVarIndex);
        if (operand->AsLclVar()->IsLastUse(multiRegIdx))
        {
            VarSetOps::RemoveElemD(compiler, currentLiveVars, fieldVarDsc->lvVarIndex);
        }
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        buildUpperVectorRestoreRefPosition(interval, currentLoc, operand, true, (unsigned)multiRegIdx);
#endif
    }
    else
    {
        RefInfoListNode* refInfo   = defList.removeListNode(operand, multiRegIdx);
        RefPosition*     defRefPos = refInfo->ref;
        assert(defRefPos->multiRegIdx == multiRegIdx);
        interval = defRefPos->getInterval();
        listNodePool.ReturnNode(refInfo);
        operand = nullptr;
    }
    RefPosition* useRefPos = newRefPosition(interval, currentLoc, RefTypeUse, operand, candidates, multiRegIdx);
    useRefPos->setRegOptional(regOptional);
    return useRefPos;
}

//------------------------------------------------------------------------
// BuildIndirUses: Build Use RefPositions for an indirection that might be contained
//
// Arguments:
//    indirTree      - The indirection node of interest
//
// Return Value:
//    The number of source registers used by the *parent* of this node.
//
// Notes:
//    This method may only be used if the candidates are the same for all sources.
//
int LinearScan::BuildIndirUses(GenTreeIndir* indirTree, regMaskTP candidates)
{
    return BuildAddrUses(indirTree->Addr(), candidates);
}

int LinearScan::BuildAddrUses(GenTree* addr, regMaskTP candidates)
{
    if (!addr->isContained())
    {
        BuildUse(addr, candidates);
        return 1;
    }
    if (!addr->OperIs(GT_LEA))
    {
        return 0;
    }

    GenTreeAddrMode* const addrMode = addr->AsAddrMode();

    unsigned srcCount = 0;
    if (addrMode->HasBase() && !addrMode->Base()->isContained())
    {
        BuildUse(addrMode->Base(), candidates);
        srcCount++;
    }
    if (addrMode->HasIndex())
    {
        if (!addrMode->Index()->isContained())
        {
            BuildUse(addrMode->Index(), candidates);
            srcCount++;
        }
#ifdef TARGET_ARM64
        else if (addrMode->Index()->OperIs(GT_BFIZ))
        {
            GenTreeCast* cast = addrMode->Index()->gtGetOp1()->AsCast();
            assert(cast->isContained());
            BuildUse(cast->CastOp(), candidates);
            srcCount++;
        }
        else if (addrMode->Index()->OperIs(GT_CAST))
        {
            GenTreeCast* cast = addrMode->Index()->AsCast();
            assert(cast->isContained());
            BuildUse(cast->CastOp(), candidates);
            srcCount++;
        }
#endif
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildOperandUses: Build Use RefPositions for an operand that might be contained.
//
// Arguments:
//    node              - The node of interest
//    candidates        - The set of candidates for the uses
//
// Return Value:
//    The number of source registers used by the *parent* of this node.
//
int LinearScan::BuildOperandUses(GenTree* node, regMaskTP candidates)
{
    if (!node->isContained())
    {
        BuildUse(node, candidates);
        return 1;
    }

#ifdef TARGET_ARM64
    // Must happen before OperIsHWIntrinsic case,
    // but this occurs when a vector zero node is marked as contained.
    if (node->IsVectorZero())
    {
        return 0;
    }
#endif

#if !defined(TARGET_64BIT)
    if (node->OperIs(GT_LONG))
    {
        return BuildBinaryUses(node->AsOp(), candidates);
    }
#endif // !defined(TARGET_64BIT)
    if (node->OperIsIndir())
    {
        return BuildIndirUses(node->AsIndir(), candidates);
    }
    if (node->OperIs(GT_LEA))
    {
        return BuildAddrUses(node, candidates);
    }
    if (node->OperIs(GT_BSWAP, GT_BSWAP16))
    {
        return BuildOperandUses(node->gtGetOp1(), candidates);
    }
#ifdef FEATURE_HW_INTRINSICS
    if (node->OperIsHWIntrinsic())
    {
        GenTreeHWIntrinsic* hwintrinsic = node->AsHWIntrinsic();

        if (hwintrinsic->OperIsMemoryLoad())
        {
            return BuildAddrUses(hwintrinsic->Op(1));
        }

        size_t numArgs = hwintrinsic->GetOperandCount();

        if (numArgs != 1)
        {
            assert(numArgs == 2);
            assert(hwintrinsic->Op(2)->isContained());
            assert(hwintrinsic->Op(2)->IsCnsIntOrI());
        }

        return BuildOperandUses(hwintrinsic->Op(1), candidates);
    }
#endif // FEATURE_HW_INTRINSICS
#if defined(TARGET_XARCH) || defined(TARGET_ARM64)
    if (node->OperIsCompare())
    {
        // Compares can be contained by a SELECT/compare chains.
        return BuildBinaryUses(node->AsOp(), candidates);
    }
#endif
#ifdef TARGET_ARM64
    if (node->OperIs(GT_MUL) || node->OperIs(GT_AND))
    {
        // MUL can be contained for madd or msub on arm64.
        // ANDs may be contained in a chain.
        return BuildBinaryUses(node->AsOp(), candidates);
    }
    if (node->OperIs(GT_NEG, GT_CAST, GT_LSH, GT_RSH, GT_RSZ))
    {
        // NEG can be contained for mneg on arm64
        // CAST and LSH for ADD with sign/zero extension
        // LSH, RSH, and RSZ for various "shifted register" instructions on arm64
        return BuildOperandUses(node->gtGetOp1(), candidates);
    }
#endif

    return 0;
}

//------------------------------------------------------------------------
// setDelayFree: Mark a RefPosition as delayRegFree, and set pendingDelayFree
//
// Arguments:
//    use      - The use RefPosition to mark
//
void LinearScan::setDelayFree(RefPosition* use)
{
    use->delayRegFree = true;
    pendingDelayFree  = true;
}

//------------------------------------------------------------------------
// AddDelayFreeUses: Mark useRefPosition as delay-free, if applicable, for the
//                   rmw node.
//
// Arguments:
//    useRefPosition -    The use refposition that need to be delay-freed.
//    rmwNode        - The node that has RMW semantics (if applicable)
//
void LinearScan::AddDelayFreeUses(RefPosition* useRefPosition, GenTree* rmwNode)
{
    assert(useRefPosition != nullptr);

    Interval* rmwInterval  = nullptr;
    bool      rmwIsLastUse = false;
    GenTree*  addr         = nullptr;
    if ((rmwNode != nullptr) && isCandidateLocalRef(rmwNode))
    {
        rmwInterval = getIntervalForLocalVarNode(rmwNode->AsLclVar());
        // Note: we don't handle multi-reg vars here. It's not clear that there are any cases
        // where we'd encounter a multi-reg var in an RMW context.
        assert(!rmwNode->AsLclVar()->IsMultiReg());
        rmwIsLastUse = rmwNode->AsLclVar()->IsLastUse(0);
    }
    // If node != rmwNode, then definitely node should be marked as "delayFree".
    // However, if node == rmwNode, then we can mark node as "delayFree" only if
    // none of the node/rmwNode are the last uses. If either of them are last use,
    // we can safely reuse the rmwNode as destination.
    if ((useRefPosition->getInterval() != rmwInterval) || (!rmwIsLastUse && !useRefPosition->lastUse))
    {
        setDelayFree(useRefPosition);
    }
}

//------------------------------------------------------------------------
// BuildDelayFreeUses: Build Use RefPositions for an operand that might be contained,
//                     and which may need to be marked delayRegFree
//
// Arguments:
//    node              - The node of interest
//    rmwNode           - The node that has RMW semantics (if applicable)
//    candidates        - The set of candidates for the uses
//    useRefPositionRef - If a use RefPosition is created, returns it. If none created, sets it to nullptr.
//
// REVIEW: useRefPositionRef is not consistently set. Also, sometimes this function creates multiple RefPositions
// but can only return one. Does it matter which one gets returned?
//
// Return Value:
//    The number of source registers used by the *parent* of this node.
//
int LinearScan::BuildDelayFreeUses(GenTree*      node,
                                   GenTree*      rmwNode,
                                   regMaskTP     candidates,
                                   RefPosition** useRefPositionRef)
{
    RefPosition* use  = nullptr;
    GenTree*     addr = nullptr;
    if (useRefPositionRef != nullptr)
    {
        *useRefPositionRef = nullptr;
    }

    if (!node->isContained())
    {
        use = BuildUse(node, candidates);
    }
#ifdef TARGET_ARM64
    // Must happen before OperIsHWIntrinsic case,
    // but this occurs when a vector zero node is marked as contained.
    else if (node->IsVectorZero())
    {
        return 0;
    }
#endif
#ifdef FEATURE_HW_INTRINSICS
    else if (node->OperIsHWIntrinsic())
    {
        assert(node->AsHWIntrinsic()->GetOperandCount() == 1);
        use = BuildUse(node->AsHWIntrinsic()->Op(1), candidates);
    }
#endif
    else if (!node->OperIsIndir())
    {
        return 0;
    }
    else
    {
        GenTreeIndir* indirTree = node->AsIndir();
        addr                    = indirTree->gtOp1;
        if (!addr->isContained())
        {
            use = BuildUse(addr, candidates);
        }
        else if (!addr->OperIs(GT_LEA))
        {
            return 0;
        }
    }
    if (use != nullptr)
    {
        AddDelayFreeUses(use, rmwNode);
        if (useRefPositionRef != nullptr)
        {
            *useRefPositionRef = use;
        }
        return 1;
    }

    // If we reach here we have a contained LEA in 'addr'.

    GenTreeAddrMode* const addrMode = addr->AsAddrMode();

    unsigned srcCount = 0;
    if (addrMode->HasBase() && !addrMode->Base()->isContained())
    {
        use = BuildUse(addrMode->Base(), candidates);
        AddDelayFreeUses(use, rmwNode);

        srcCount++;
    }
    if (addrMode->HasIndex() && !addrMode->Index()->isContained())
    {
        use = BuildUse(addrMode->Index(), candidates);
        AddDelayFreeUses(use, rmwNode);

        srcCount++;
    }

    if (useRefPositionRef != nullptr)
    {
        *useRefPositionRef = use;
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildBinaryUses: Get the RefInfoListNodes for the operands of the
//                  given node, and build uses for them.
//
// Arguments:
//    node - a GenTreeOp
//
// Return Value:
//    The number of actual register operands.
//
// Notes:
//    The operands must already have been processed by buildRefPositionsForNode, and their
//    RefInfoListNodes placed in the defList.
//
int LinearScan::BuildBinaryUses(GenTreeOp* node, regMaskTP candidates)
{
    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2IfPresent();

#ifdef TARGET_XARCH
    if (node->OperIsBinary() && isRMWRegOper(node))
    {
        assert(op2 != nullptr);
        return BuildRMWUses(node, op1, op2, candidates);
    }
#endif // TARGET_XARCH
    int srcCount = 0;
    if (op1 != nullptr)
    {
        srcCount += BuildOperandUses(op1, candidates);
    }
    if (op2 != nullptr)
    {
        srcCount += BuildOperandUses(op2, candidates);
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildCastUses: Build uses for a cast's source, preferencing it as appropriate.
//
// Arguments:
//    cast       - The cast node to build uses for
//    candidates - The candidate registers for the uses
//
// Return Value:
//    The number of actual register operands.
//
int LinearScan::BuildCastUses(GenTreeCast* cast, regMaskTP candidates)
{
    GenTree* src = cast->CastOp();

    // Casts can have contained memory operands.
    if (src->isContained())
    {
        return BuildOperandUses(src, candidates);
    }

    RefPosition* srcUse = BuildUse(src, candidates);

#ifdef TARGET_64BIT
    // A long -> int cast is a copy - the code generator will elide
    // it if the source and destination registers are the same.
    if (src->TypeIs(TYP_LONG) && cast->TypeIs(TYP_INT))
    {
        tgtPrefUse = srcUse;
    }
#endif // TARGET_64BIT

    return 1;
}

//------------------------------------------------------------------------
// BuildStoreLocDef: Build a definition RefPosition for a local store
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This takes an index to enable building multiple defs for a multi-reg local.
//
void LinearScan::BuildStoreLocDef(GenTreeLclVarCommon* storeLoc,
                                  LclVarDsc*           varDsc,
                                  RefPosition*         singleUseRef,
                                  int                  index)
{
    assert(varDsc->lvTracked);
    unsigned  varIndex       = varDsc->lvVarIndex;
    Interval* varDefInterval = getIntervalForLocalVar(varIndex);
    if (!storeLoc->IsLastUse(index))
    {
        VarSetOps::AddElemD(compiler, currentLiveVars, varIndex);
    }
    if (singleUseRef != nullptr)
    {
        Interval* srcInterval = singleUseRef->getInterval();
        if (srcInterval->relatedInterval == nullptr)
        {
            // Preference the source to the dest, unless this is a non-last-use localVar.
            // Note that the last-use info is not correct, but it is a better approximation than preferencing
            // the source to the dest, if the source's lifetime extends beyond the dest.
            if (!srcInterval->isLocalVar || (singleUseRef->treeNode->gtFlags & GTF_VAR_DEATH) != 0)
            {
                srcInterval->assignRelatedInterval(varDefInterval);
            }
        }
        else if (!srcInterval->isLocalVar)
        {
            // Preference the source to dest, if src is not a local var.
            srcInterval->assignRelatedInterval(varDefInterval);
        }
    }

    regMaskTP defCandidates = RBM_NONE;
    var_types type          = varDsc->GetRegisterType();

#ifdef TARGET_X86
    if (varTypeIsByte(type))
    {
        defCandidates = allByteRegs();
    }
    else
    {
        defCandidates = allRegs(type);
    }
#else
    defCandidates  = allRegs(type);
#endif // TARGET_X86

    RefPosition* def = newRefPosition(varDefInterval, currentLoc + 1, RefTypeDef, storeLoc, defCandidates, index);
    if (varDefInterval->isWriteThru)
    {
        // We always make write-thru defs reg-optional, as we can store them if they don't
        // get a register.
        def->regOptional = true;
    }
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    if (Compiler::varTypeNeedsPartialCalleeSave(varDefInterval->registerType))
    {
        varDefInterval->isPartiallySpilled = false;
    }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
}

//------------------------------------------------------------------------
// BuildMultiRegStoreLoc: Set register requirements for a store of a lclVar
//
// Arguments:
//    storeLoc - the multireg local store (GT_STORE_LCL_VAR)
//
// Returns:
//    The number of source registers read.
//
int LinearScan::BuildMultiRegStoreLoc(GenTreeLclVar* storeLoc)
{
    GenTree*     op1      = storeLoc->gtGetOp1();
    unsigned int dstCount = storeLoc->GetFieldCount(compiler);
    unsigned int srcCount = dstCount;
    LclVarDsc*   varDsc   = compiler->lvaGetDesc(storeLoc);

    assert(compiler->lvaEnregMultiRegVars);
    assert(storeLoc->OperGet() == GT_STORE_LCL_VAR);
    bool isMultiRegSrc = op1->IsMultiRegNode();
    // The source must be:
    // - a multi-reg source
    // - an enregisterable SIMD type, or
    // - in-memory local
    //
    if (isMultiRegSrc)
    {
        assert(op1->GetMultiRegCount(compiler) == srcCount);
    }
    else if (varTypeIsEnregisterable(op1))
    {
        // Create a delay free use, as we'll have to use it to create each field
        RefPosition* use = BuildUse(op1, RBM_NONE);
        setDelayFree(use);
        srcCount = 1;
    }
    else
    {
        // Otherwise we must have an in-memory struct lclVar.
        // We will just load directly into the register allocated for this lclVar,
        // so we don't need to build any uses.
        assert(op1->OperIs(GT_LCL_VAR) && op1->isContained() && op1->TypeIs(TYP_STRUCT));
        srcCount = 0;
    }
    // For multi-reg local stores of multi-reg sources, the code generator will read each source
    // register, and then move it, if needed, to the destination register. These nodes have
    // 2*N locations where N is the number of registers, so that the liveness can
    // be reflected accordingly.
    //
    for (unsigned int i = 0; i < dstCount; ++i)
    {
        LclVarDsc*   fieldVarDsc  = compiler->lvaGetDesc(varDsc->lvFieldLclStart + i);
        RefPosition* singleUseRef = nullptr;

        if (isMultiRegSrc)
        {
            regMaskTP srcCandidates = RBM_NONE;
#ifdef TARGET_X86
            var_types type = fieldVarDsc->TypeGet();
            if (varTypeIsByte(type))
            {
                srcCandidates = allByteRegs();
            }
#endif // TARGET_X86
            singleUseRef = BuildUse(op1, srcCandidates, i);
        }
        assert(isCandidateVar(fieldVarDsc));
        BuildStoreLocDef(storeLoc, fieldVarDsc, singleUseRef, i);
        if (isMultiRegSrc && (i < (dstCount - 1)))
        {
            currentLoc += 2;
        }
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildStoreLoc: Set register requirements for a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Setting the appropriate candidates.
//    - Handling of contained immediates.
//    - Requesting an internal register for SIMD12 stores.
//
int LinearScan::BuildStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    GenTree*     op1 = storeLoc->gtGetOp1();
    int          srcCount;
    RefPosition* singleUseRef = nullptr;
    LclVarDsc*   varDsc       = compiler->lvaGetDesc(storeLoc);

    if (storeLoc->IsMultiRegLclVar())
    {
        return BuildMultiRegStoreLoc(storeLoc->AsLclVar());
    }

// First, define internal registers.
#ifdef FEATURE_SIMD
    if (varTypeIsSIMD(storeLoc) && !op1->IsVectorZero() && (storeLoc->TypeGet() == TYP_SIMD12))
    {
        // Need an additional register to extract upper 4 bytes of Vector3,
        // it has to be float for x86.
        buildInternalFloatRegisterDefForNode(storeLoc, allSIMDRegs());
    }
#endif // FEATURE_SIMD

    // Second, use source registers.

    if (op1->IsMultiRegNode())
    {
        // This is the case where the source produces multiple registers.
        // This must be a store lclvar.
        assert(storeLoc->OperGet() == GT_STORE_LCL_VAR);
        srcCount = op1->GetMultiRegCount(compiler);

        for (int i = 0; i < srcCount; ++i)
        {
            BuildUse(op1, RBM_NONE, i);
        }
#if defined(FEATURE_SIMD) && defined(TARGET_X86)
        if (TargetOS::IsWindows && !compiler->compOpportunisticallyDependsOn(InstructionSet_SSE41))
        {
            if (varTypeIsSIMD(storeLoc) && op1->IsCall())
            {
                // Need an additional register to create a SIMD8 from EAX/EDX without SSE4.1.
                buildInternalFloatRegisterDefForNode(storeLoc, allSIMDRegs());

                if (isCandidateVar(varDsc))
                {
                    // This internal register must be different from the target register.
                    setInternalRegsDelayFree = true;
                }
            }
        }
#endif // FEATURE_SIMD && TARGET_X86
    }
    else if (op1->isContained() && op1->OperIs(GT_BITCAST))
    {
        GenTree*     bitCastSrc   = op1->gtGetOp1();
        RegisterType registerType = regType(bitCastSrc->TypeGet());
        singleUseRef              = BuildUse(bitCastSrc, allRegs(registerType));

        Interval* srcInterval = singleUseRef->getInterval();
        assert(regType(srcInterval->registerType) == registerType);
        srcCount = 1;
    }
#ifndef TARGET_64BIT
    else if (varTypeIsLong(op1))
    {
        // GT_MUL_LONG is handled by the IsMultiRegNode case above.
        assert(op1->OperIs(GT_LONG));
        assert(op1->isContained() && !op1->gtGetOp1()->isContained() && !op1->gtGetOp2()->isContained());
        srcCount = BuildBinaryUses(op1->AsOp());
        assert(srcCount == 2);
    }
#endif // !TARGET_64BIT
    else if (op1->isContained())
    {
        srcCount = 0;
    }
    else
    {
        srcCount                = 1;
        regMaskTP srcCandidates = RBM_NONE;
#ifdef TARGET_X86
        var_types type = varDsc->GetRegisterType(storeLoc);
        if (varTypeIsByte(type))
        {
            srcCandidates = allByteRegs();
        }
#endif // TARGET_X86
        singleUseRef = BuildUse(op1, srcCandidates);
    }

// Third, use internal registers.
#ifdef TARGET_ARM
    if (storeLoc->OperIs(GT_STORE_LCL_FLD) && storeLoc->AsLclFld()->IsOffsetMisaligned())
    {
        buildInternalIntRegisterDefForNode(storeLoc); // to generate address.
        buildInternalIntRegisterDefForNode(storeLoc); // to move float into an int reg.
        if (storeLoc->TypeIs(TYP_DOUBLE))
        {
            buildInternalIntRegisterDefForNode(storeLoc); // to move the second half into an int reg.
        }
    }
#endif // TARGET_ARM

#if defined(FEATURE_SIMD) || defined(TARGET_ARM)
    buildInternalRegisterUses();
#endif // FEATURE_SIMD || TARGET_ARM

    // Fourth, define destination registers.

    // Add the lclVar to currentLiveVars (if it will remain live)
    if (isCandidateVar(varDsc))
    {
        BuildStoreLocDef(storeLoc, varDsc, singleUseRef, 0);
    }

    return srcCount;
}

//------------------------------------------------------------------------
// BuildSimple: Builds use RefPositions for trees requiring no special handling
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    The number of use RefPositions created
//
int LinearScan::BuildSimple(GenTree* tree)
{
    unsigned kind     = tree->OperKind();
    int      srcCount = 0;
    if ((kind & GTK_LEAF) == 0)
    {
        assert((kind & GTK_SMPOP) != 0);
        srcCount = BuildBinaryUses(tree->AsOp());
    }
    if (tree->IsValue())
    {
        BuildDef(tree);
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildReturn: Set the NodeInfo for a GT_RETURN.
//
// Arguments:
//    tree - The node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildReturn(GenTree* tree)
{
    GenTree* op1 = tree->gtGetOp1();

#if !defined(TARGET_64BIT)
    if (tree->TypeGet() == TYP_LONG)
    {
        assert((op1->OperGet() == GT_LONG) && op1->isContained());
        GenTree* loVal = op1->gtGetOp1();
        GenTree* hiVal = op1->gtGetOp2();
        BuildUse(loVal, RBM_LNGRET_LO);
        BuildUse(hiVal, RBM_LNGRET_HI);
        return 2;
    }
    else
#endif // !defined(TARGET_64BIT)
        if ((tree->TypeGet() != TYP_VOID) && !op1->isContained())
    {
        regMaskTP useCandidates = RBM_NONE;

#if FEATURE_MULTIREG_RET
#ifdef TARGET_ARM64
        if (varTypeIsSIMD(tree) && !op1->IsMultiRegLclVar())
        {
            BuildUse(op1, RBM_DOUBLERET);
            return 1;
        }
#endif // TARGET_ARM64

        if (varTypeIsStruct(tree))
        {
            // op1 has to be either a lclvar or a multi-reg returning call
            if ((op1->OperGet() == GT_LCL_VAR) && !op1->IsMultiRegLclVar())
            {
                BuildUse(op1, useCandidates);
            }
            else
            {
                noway_assert(op1->IsMultiRegCall() || (op1->IsMultiRegLclVar() && compiler->lvaEnregMultiRegVars));

                ReturnTypeDesc retTypeDesc = compiler->compRetTypeDesc;
                const int      srcCount    = retTypeDesc.GetReturnRegCount();
                assert(op1->GetMultiRegCount(compiler) == static_cast<unsigned>(srcCount));

                // For any source that's coming from a different register file, we need to ensure that
                // we reserve the specific ABI register we need.
                bool hasMismatchedRegTypes = false;
                if (op1->IsMultiRegLclVar())
                {
                    for (int i = 0; i < srcCount; i++)
                    {
                        RegisterType srcType = regType(op1->AsLclVar()->GetFieldTypeByIndex(compiler, i));
                        RegisterType dstType = regType(retTypeDesc.GetReturnRegType(i));
                        if (srcType != dstType)
                        {
                            hasMismatchedRegTypes = true;
                            regMaskTP dstRegMask  = genRegMask(retTypeDesc.GetABIReturnReg(i));

                            if (varTypeUsesIntReg(dstType))
                            {
                                buildInternalIntRegisterDefForNode(tree, dstRegMask);
                            }
#if defined(TARGET_XARCH) && defined(FEATURE_SIMD)
                            else if (varTypeUsesMaskReg(dstType))
                            {
                                buildInternalMaskRegisterDefForNode(tree, dstRegMask);
                            }
#endif // TARGET_XARCH && FEATURE_SIMD
                            else
                            {
                                assert(varTypeUsesFloatReg(dstType));
                                buildInternalFloatRegisterDefForNode(tree, dstRegMask);
                            }
                        }
                    }
                }
                for (int i = 0; i < srcCount; i++)
                {
                    // We will build uses of the type of the operand registers/fields, and the codegen
                    // for return will move as needed.
                    if (!hasMismatchedRegTypes || (regType(op1->AsLclVar()->GetFieldTypeByIndex(compiler, i)) ==
                                                   regType(retTypeDesc.GetReturnRegType(i))))
                    {
                        BuildUse(op1, genRegMask(retTypeDesc.GetABIReturnReg(i)), i);
                    }
                    else
                    {
                        BuildUse(op1, RBM_NONE, i);
                    }
                }
                if (hasMismatchedRegTypes)
                {
                    buildInternalRegisterUses();
                }
                return srcCount;
            }
        }
        else
#endif // FEATURE_MULTIREG_RET
        {
            // Non-struct type return - determine useCandidates
            switch (tree->TypeGet())
            {
                case TYP_VOID:
                    useCandidates = RBM_NONE;
                    break;
                case TYP_FLOAT:
                    useCandidates = RBM_FLOATRET;
                    break;
                case TYP_DOUBLE:
                    // We ONLY want the valid double register in the RBM_DOUBLERET mask.
                    useCandidates = (RBM_DOUBLERET & RBM_ALLDOUBLE);
                    break;
                case TYP_LONG:
                    useCandidates = RBM_LNGRET;
                    break;
                default:
                    useCandidates = RBM_INTRET;
                    break;
            }
            BuildUse(op1, useCandidates);
            return 1;
        }
    }

    // No kills or defs.
    return 0;
}

//------------------------------------------------------------------------
// supportsSpecialPutArg: Determine if we can support specialPutArgs
//
// Return Value:
//    True iff specialPutArg intervals can be supported.
//
// Notes:
//    See below.
//

bool LinearScan::supportsSpecialPutArg()
{
#if defined(DEBUG) && defined(TARGET_X86)
    // On x86, `LSRA_LIMIT_CALLER` is too restrictive to allow the use of special put args: this stress mode
    // leaves only three registers allocatable--eax, ecx, and edx--of which the latter two are also used for the
    // first two integral arguments to a call. This can leave us with too few registers to successfully allocate in
    // situations like the following:
    //
    //     t1026 =    lclVar    ref    V52 tmp35        u:3 REG NA <l:$3a1, c:$98d>
    //
    //             /--*  t1026  ref
    //     t1352 = *  putarg_reg ref    REG NA
    //
    //      t342 =    lclVar    int    V14 loc6         u:4 REG NA $50c
    //
    //      t343 =    const     int    1 REG NA $41
    //
    //             /--*  t342   int
    //             +--*  t343   int
    //      t344 = *  +         int    REG NA $495
    //
    //      t345 =    lclVar    int    V04 arg4         u:2 REG NA $100
    //
    //             /--*  t344   int
    //             +--*  t345   int
    //      t346 = *  %         int    REG NA $496
    //
    //             /--*  t346   int
    //     t1353 = *  putarg_reg int    REG NA
    //
    //     t1354 =    lclVar    ref    V52 tmp35         (last use) REG NA
    //
    //             /--*  t1354  ref
    //     t1355 = *  lea(b+0)  byref  REG NA
    //
    // Here, the first `putarg_reg` would normally be considered a special put arg, which would remove `ecx` from the
    // set of allocatable registers, leaving only `eax` and `edx`. The allocator will then fail to allocate a register
    // for the def of `t345` if arg4 is not a register candidate: the corresponding ref position will be constrained to
    // { `ecx`, `ebx`, `esi`, `edi` }, which `LSRA_LIMIT_CALLER` will further constrain to `ecx`, which will not be
    // available due to the special put arg.
    return getStressLimitRegs() != LSRA_LIMIT_CALLER;
#else
    return true;
#endif
}

//------------------------------------------------------------------------
// BuildPutArgReg: Set the NodeInfo for a PUTARG_REG.
//
// Arguments:
//    node                - The PUTARG_REG node.
//    argReg              - The register in which to pass the argument.
//    info                - The info for the node's using call.
//    isVarArgs           - True if the call uses a varargs calling convention.
//    callHasFloatRegArgs - Set to true if this PUTARG_REG uses an FP register.
//
// Return Value:
//    None.
//
int LinearScan::BuildPutArgReg(GenTreeUnOp* node)
{
    assert(node != nullptr);
    assert(node->OperIsPutArgReg());
    regNumber argReg = node->GetRegNum();
    assert(argReg != REG_NA);
    bool     isSpecialPutArg = false;
    int      srcCount        = 1;
    GenTree* op1             = node->gtGetOp1();

    // To avoid redundant moves, have the argument operand computed in the
    // register in which the argument is passed to the call.
    regMaskTP    argMask = genRegMask(argReg);
    RefPosition* use     = BuildUse(op1, argMask);

    // Record that this register is occupied by a register now.
    placedArgRegs |= argMask;

    if (supportsSpecialPutArg() && isCandidateLocalRef(op1) && ((op1->gtFlags & GTF_VAR_DEATH) == 0))
    {
        // This is the case for a "pass-through" copy of a lclVar.  In the case where it is a non-last-use,
        // we don't want the def of the copy to kill the lclVar register, if it is assigned the same register
        // (which is actually what we hope will happen).
        JITDUMP("Setting putarg_reg as a pass-through of a non-last use lclVar\n");

        // Preference the destination to the interval of the first register defined by the first operand.
        assert(use->getInterval()->isLocalVar);
        isSpecialPutArg = true;

        // Record that this local is available in the register to ensure we
        // keep the register in its local set if we see it die before the call
        // (see UpdatePreferencesOfDyingLocal).
        assert(numPlacedArgLocals < ArrLen(placedArgLocals));
        placedArgLocals[numPlacedArgLocals].VarIndex = use->getInterval()->getVarIndex(compiler);
        placedArgLocals[numPlacedArgLocals].Reg      = argReg;
        numPlacedArgLocals++;
    }

#ifdef TARGET_ARM
    // If type of node is `long` then it is actually `double`.
    // The actual `long` types must have been transformed as a field list with two fields.
    if (node->TypeGet() == TYP_LONG)
    {
        srcCount++;
        regMaskTP argMaskHi = genRegMask(REG_NEXT(argReg));
        assert(genRegArgNext(argReg) == REG_NEXT(argReg));
        use = BuildUse(op1, argMaskHi, 1);
        BuildDef(node, argMask, 0);
        BuildDef(node, argMaskHi, 1);
    }
    else
#endif // TARGET_ARM
    {
        RefPosition* def = BuildDef(node, argMask);
        if (isSpecialPutArg)
        {
            def->getInterval()->isSpecialPutArg = true;
            def->getInterval()->assignRelatedInterval(use->getInterval());
        }
    }

    return srcCount;
}

//------------------------------------------------------------------------
// HandleFloatVarArgs: Handle additional register requirements for a varargs call
//
// Arguments:
//    call    - The call node of interest
//    argNode - The current argument
//
// Return Value:
//    None.
//
// Notes:
//    In the case of a varargs call, the ABI dictates that if we have floating point args,
//    we must pass the enregistered arguments in both the integer and floating point registers.
//    Since the integer register is not associated with the arg node, we will reserve it as
//    an internal register on the call so that it is not used during the evaluation of the call node
//    (e.g. for the target).
void LinearScan::HandleFloatVarArgs(GenTreeCall* call, GenTree* argNode, bool* callHasFloatRegArgs)
{
    if (compFeatureVarArg() && call->IsVarargs() && varTypeIsFloating(argNode))
    {
        *callHasFloatRegArgs = true;

        // We'll have to return the internal def and then later create a use for it.
        regNumber argReg    = argNode->GetRegNum();
        regNumber targetReg = compiler->getCallArgIntRegister(argReg);

        buildInternalIntRegisterDefForNode(call, genRegMask(targetReg));
    }
}

//------------------------------------------------------------------------
// BuildGCWriteBarrier: Handle additional register requirements for a GC write barrier
//
// Arguments:
//    tree    - The STORE_IND for which a write barrier is required
//
int LinearScan::BuildGCWriteBarrier(GenTree* tree)
{
    GenTree* addr = tree->gtGetOp1();
    GenTree* src  = tree->gtGetOp2();

    // In the case where we are doing a helper assignment, even if the dst
    // is an indir through an lea, we need to actually instantiate the
    // lea in a register
    assert(!addr->isContained() && !src->isContained());
    regMaskTP addrCandidates = RBM_WRITE_BARRIER_DST;
    regMaskTP srcCandidates  = RBM_WRITE_BARRIER_SRC;

#if defined(TARGET_X86) && NOGC_WRITE_BARRIERS

    bool useOptimizedWriteBarrierHelper = compiler->codeGen->genUseOptimizedWriteBarriers(tree->AsStoreInd());
    if (useOptimizedWriteBarrierHelper)
    {
        // Special write barrier:
        // op1 (addr) goes into REG_OPTIMIZED_WRITE_BARRIER_DST (rdx) and
        // op2 (src) goes into any int register.
        addrCandidates = RBM_OPTIMIZED_WRITE_BARRIER_DST;
        srcCandidates  = RBM_OPTIMIZED_WRITE_BARRIER_SRC;
    }

#endif // defined(TARGET_X86) && NOGC_WRITE_BARRIERS

    BuildUse(addr, addrCandidates);
    BuildUse(src, srcCandidates);

    regMaskTP killMask = getKillSetForStoreInd(tree->AsStoreInd());
    buildKillPositionsForNode(tree, currentLoc + 1, killMask);
    return 2;
}

//------------------------------------------------------------------------
// BuildCmp: Set the register requirements for a compare.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    Number of sources.
//
int LinearScan::BuildCmp(GenTree* tree)
{
#if defined(TARGET_XARCH)
    assert(tree->OperIsCompare() || tree->OperIs(GT_CMP, GT_TEST, GT_BT));
#elif defined(TARGET_ARM64)
    assert(tree->OperIsCompare() || tree->OperIs(GT_CMP, GT_TEST, GT_JCMP, GT_JTEST, GT_CCMP));
#else
    assert(tree->OperIsCompare() || tree->OperIs(GT_CMP, GT_TEST, GT_JCMP));
#endif

    int srcCount = BuildCmpOperands(tree);

    if (!tree->TypeIs(TYP_VOID))
    {
        regMaskTP dstCandidates = RBM_NONE;

#ifdef TARGET_X86
        // If the compare is used by a jump, we just need to set the condition codes. If not, then we need
        // to store the result into the low byte of a register, which requires the dst be a byteable register.
        dstCandidates = allByteRegs();
#endif

        BuildDef(tree, dstCandidates);
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildCmpOperands: Set the register requirements for a compare's operands.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    Number of sources.
//
int LinearScan::BuildCmpOperands(GenTree* tree)
{
    regMaskTP op1Candidates = RBM_NONE;
    regMaskTP op2Candidates = RBM_NONE;
    GenTree*  op1           = tree->gtGetOp1();
    GenTree*  op2           = tree->gtGetOp2();

#ifdef TARGET_X86
    bool needByteRegs = false;
    if (varTypeIsByte(tree))
    {
        if (varTypeUsesIntReg(op1))
        {
            needByteRegs = true;
        }
    }
    // Example1: GT_EQ(int, op1 of type ubyte, op2 of type ubyte) - in this case codegen uses
    // ubyte as the result of comparison and if the result needs to be materialized into a reg
    // simply zero extend it to TYP_INT size.  Here is an example of generated code:
    //         cmp dl, byte ptr[addr mode]
    //         movzx edx, dl
    else if (varTypeIsByte(op1) && varTypeIsByte(op2))
    {
        needByteRegs = true;
    }
    // Example2: GT_EQ(int, op1 of type ubyte, op2 is GT_CNS_INT) - in this case codegen uses
    // ubyte as the result of the comparison and if the result needs to be materialized into a reg
    // simply zero extend it to TYP_INT size.
    else if (varTypeIsByte(op1) && op2->IsCnsIntOrI())
    {
        needByteRegs = true;
    }
    // Example3: GT_EQ(int, op1 is GT_CNS_INT, op2 of type ubyte) - in this case codegen uses
    // ubyte as the result of the comparison and if the result needs to be materialized into a reg
    // simply zero extend it to TYP_INT size.
    else if (op1->IsCnsIntOrI() && varTypeIsByte(op2))
    {
        needByteRegs = true;
    }
    if (needByteRegs)
    {
        if (!op1->isContained())
        {
            op1Candidates = allByteRegs();
        }
        if (!op2->isContained())
        {
            op2Candidates = allByteRegs();
        }
    }
#endif // TARGET_X86

    int srcCount = BuildOperandUses(op1, op1Candidates);
    srcCount += BuildOperandUses(op2, op2Candidates);
    return srcCount;
}
