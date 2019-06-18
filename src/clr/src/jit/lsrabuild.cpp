// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
//    l -    - The `LsraLocation` for the `RefInfo` value.
//    i      - The interval for the `RefInfo` value.
//    t      - The IR node for the `RefInfo` value
//    regIdx - The register index for the `RefInfo` value.
//
// Returns:
//    A pooled or newly-allocated `RefInfoListNode`, depending on the
//    contents of the pool.
RefInfoListNode* RefInfoListNodePool::GetNode(RefPosition* r, GenTree* t, unsigned regIdx)
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

    DBEXEC(VERBOSE, newInt->dump());
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
    refPositions.emplace_back(curBBNum, nodeLocation, treeNode, refType);
    RefPosition* newRP = &refPositions.back();
#ifdef DEBUG
    newRP->rpNum = static_cast<unsigned>(refPositions.size() - 1);
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
//       We set the candiates on defRefPosition to be all regs of the appropriate type, and since they are
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
        useReg                               = useRefPosition->assignedReg();
        useRegRecord                         = getRegisterRecord(useReg);
        RefPosition* currFixedRegRefPosition = useRegRecord->recentRefPosition;

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
#ifdef _TARGET_AMD64_
    if (compiler->opts.compDbgEnC)
    {
        // We only use RSI and RDI for EnC code, so we don't want to favor callee-save regs.
        return;
    }
#endif // _TARGET_AMD64_

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

    DBEXEC(VERBOSE, newRP->dump());
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
        assert((allRegs(theInterval->registerType) & mask) != 0);
    }

    RefPosition* newRP = newRefPositionRaw(theLocation, theTreeNode, theRefType);

    newRP->setInterval(theInterval);

    // Spill info
    newRP->isFixedRegRef = isFixedRegister;

#ifndef _TARGET_AMD64_
    // We don't need this for AMD because the PInvoke method epilog code is explicit
    // at register allocation time.
    if (theInterval != nullptr && theInterval->isLocalVar && compiler->info.compCallUnmanaged &&
        theInterval->varNum == compiler->genReturnLocal)
    {
        mask &= ~(RBM_PINVOKE_TCB | RBM_PINVOKE_FRAME);
        noway_assert(mask != RBM_NONE);
    }
#endif // !_TARGET_AMD64_
    newRP->registerAssignment = mask;

    newRP->setMultiRegIdx(multiRegIdx);
    newRP->setRegOptional(false);

    associateRefPosWithInterval(newRP);

    DBEXEC(VERBOSE, newRP->dump());
    return newRP;
}

//---------------------------------------------------------------------------
// newUseRefPosition: allocate and initialize a RefTypeUse RefPosition at currentLoc.
//
// Arguments:
//     theInterval     -  interval to which RefPosition is associated with.
//     theTreeNode     -  GenTree node for which this RefPosition is created
//     mask            -  Set of valid registers for this RefPosition
//     multiRegIdx     -  register position if this RefPosition corresponds to a
//                        multi-reg call node.
//     minRegCount     -  Minimum number registers that needs to be ensured while
//                        constraining candidates for this ref position under
//                        LSRA stress. This is a DEBUG only arg.
//
// Return Value:
//     a new RefPosition
//
// Notes:
//     If the caller knows that 'theTreeNode' is NOT a candidate local, newRefPosition
//     can/should be called directly.
//
RefPosition* LinearScan::newUseRefPosition(Interval* theInterval,
                                           GenTree*  theTreeNode,
                                           regMaskTP mask,
                                           unsigned  multiRegIdx)
{
    GenTree* treeNode = isCandidateLocalRef(theTreeNode) ? theTreeNode : nullptr;

    RefPosition* pos = newRefPosition(theInterval, currentLoc, RefTypeUse, treeNode, mask, multiRegIdx);
    if (theTreeNode->IsRegOptional())
    {
        pos->setRegOptional(true);
    }
    return pos;
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
        LclVarDsc* varDsc = &compiler->lvaTable[node->AsLclVar()->gtLclNum];
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
    for (regNumber reg = REG_FIRST; mask; reg = REG_NEXT(reg), mask >>= 1)
    {
        if (mask & 1)
        {
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

    GenTree* data = tree->Data();

    GCInfo::WriteBarrierForm writeBarrierForm = compiler->codeGen->gcInfo.gcIsWriteBarrierCandidate(tree, data);
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
            CorInfoHelpFunc helper =
                compiler->codeGen->genWriteBarrierHelperForWriteBarrierForm(tree, writeBarrierForm);
            killMask = compiler->compHelperCallKillSet(helper);
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
#ifdef _TARGET_XARCH_
    assert(shiftNode->OperIsShiftOrRotate());
    GenTree* shiftBy = shiftNode->gtGetOp2();
    if (!shiftBy->isContained())
    {
        killMask = RBM_RCX;
    }
#endif // _TARGET_XARCH_
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
#ifdef _TARGET_XARCH_
    assert(mulNode->OperIsMul());
    if (!mulNode->OperIs(GT_MUL) || (((mulNode->gtFlags & GTF_UNSIGNED) != 0) && mulNode->gtOverflowEx()))
    {
        killMask = RBM_RAX | RBM_RDX;
    }
#endif // _TARGET_XARCH_
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
#ifdef _TARGET_XARCH_
    assert(node->OperIs(GT_MOD, GT_DIV, GT_UMOD, GT_UDIV));
    if (!varTypeIsFloating(node->TypeGet()))
    {
        // Both RAX and RDX are killed by the operation
        killMask = RBM_RAX | RBM_RDX;
    }
#endif // _TARGET_XARCH_
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
    regMaskTP killMask = RBM_NONE;
#ifdef _TARGET_X86_
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
#endif // _TARGET_X86_
#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
    if (call->IsHelperCall())
    {
        CorInfoHelpFunc helpFunc = compiler->eeGetHelperNum(call->gtCallMethHnd);
        killMask                 = compiler->compHelperCallKillSet(helpFunc);
    }
    else
#endif // defined(_TARGET_X86_) || defined(_TARGET_ARM_)
    {
        // if there is no FP used, we can ignore the FP kills
        if (compiler->compFloatingPointUsed)
        {
            killMask = RBM_CALLEE_TRASH;
        }
        else
        {
            killMask = RBM_INT_CALLEE_TRASH;
        }
#ifdef _TARGET_ARM_
        if (call->IsVirtualStub())
        {
            killMask |= compiler->virtualStubParamInfo->GetRegMask();
        }
#else  // !_TARGET_ARM_
        // Verify that the special virtual stub call registers are in the kill mask.
        // We don't just add them unconditionally to the killMask because for most architectures
        // they are already in the RBM_CALLEE_TRASH set,
        // and we don't want to introduce extra checks and calls in this hot function.
        assert(!call->IsVirtualStub() || ((killMask & compiler->virtualStubParamInfo->GetRegMask()) ==
                                          compiler->virtualStubParamInfo->GetRegMask()));
#endif // !_TARGET_ARM_
    }
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
    assert(blkNode->OperIsStore());
    regMaskTP killMask = RBM_NONE;

    if ((blkNode->OperGet() == GT_STORE_OBJ) && blkNode->OperIsCopyBlkOp())
    {
        assert(blkNode->AsObj()->gtGcPtrCount != 0);
        killMask = compiler->compHelperCallKillSet(CORINFO_HELP_ASSIGN_BYREF);
    }
    else
    {
        bool isCopyBlk = varTypeIsStruct(blkNode->Data());
        switch (blkNode->gtBlkOpKind)
        {
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

#ifdef _TARGET_XARCH_
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
#else
            case GenTreeBlk::BlkOpKindRepInstr:
#endif
            case GenTreeBlk::BlkOpKindUnroll:
            case GenTreeBlk::BlkOpKindInvalid:
                // for these 'gtBlkOpKind' kinds, we leave 'killMask' = RBM_NONE
                break;
        }
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
#ifdef _TARGET_XARCH_
    switch (node->gtHWIntrinsicId)
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
#endif // _TARGET_XARCH_
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
#ifdef _TARGET_X86_
        case GT_LSH_HI:
        case GT_RSH_LO:
#endif
            killMask = getKillSetForShiftRotate(tree->AsOp());
            break;

        case GT_MUL:
        case GT_MULHI:
#if !defined(_TARGET_64BIT_)
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

        case GT_STORE_OBJ:
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
        case GT_HWIntrinsic:
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
        // The killMask identifies a set of registers that will be used during codegen.
        // Mark these as modified here, so when we do final frame layout, we'll know about
        // all these registers. This is especially important if killMask contains
        // callee-saved registers, which affect the frame size since we need to save/restore them.
        // In the case where we have a copyBlk with GC pointers, can need to call the
        // CORINFO_HELP_ASSIGN_BYREF helper, which kills callee-saved RSI and RDI, if
        // LSRA doesn't assign RSI/RDI, they wouldn't get marked as modified until codegen,
        // which is too late.
        compiler->codeGen->regSet.rsSetRegsModified(killMask DEBUGARG(true));

        addRefsForPhysRegMask(killMask, currentLoc, RefTypeKill, true);

        // TODO-CQ: It appears to be valuable for both fp and int registers to avoid killing the callee
        // save regs on infrequently executed paths.  However, it results in a large number of asmDiffs,
        // many of which appear to be regressions (because there is more spill on the infrequently path),
        // but are not really because the frequent path becomes smaller.  Validating these diffs will need
        // to be done before making this change.
        // if (!blockSequence[curBBSeqNum]->isRunRarely())
        if (enregisterLocalVars)
        {
            VarSetOps::Iter iter(compiler, currentLiveVars);
            unsigned        varIndex = 0;
            while (iter.NextElem(&varIndex))
            {
                unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
                LclVarDsc* varDsc = compiler->lvaTable + varNum;
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                if (varTypeNeedsPartialCalleeSave(varDsc->lvType))
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
                regMaskTP newPreferences = allRegs(interval->registerType) & (~killMask);

                if (newPreferences != RBM_NONE)
                {
                    interval->updateRegisterPreferences(newPreferences);
                }
                else
                {
                    // If there are no callee-saved registers, the call could kill all the registers.
                    // This is a valid state, so in that case assert should not trigger. The RA will spill in order to
                    // free a register later.
                    assert(compiler->opts.compDbgEnC || (calleeSaveRegs(varDsc->lvType)) == RBM_NONE);
                }
            }
        }

        insertedKills = true;
    }

    if (compiler->killGCRefs(tree))
    {
        RefPosition* pos =
            newRefPosition((Interval*)nullptr, currentLoc, RefTypeKillGCRefs, tree, (allRegs(TYP_REF) & ~RBM_ARG_REGS));
        insertedKills = true;
    }

    return insertedKills;
}

//----------------------------------------------------------------------------
// defineNewInternalTemp: Defines a ref position for an internal temp.
//
// Arguments:
//     tree                  -   Gentree node requiring an internal register
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
//   tree                  - Gentree node that needs internal registers
//   internalCands         - The mask of valid registers
//
// Returns:
//   The def RefPosition created for this internal temp.
//
RefPosition* LinearScan::buildInternalIntRegisterDefForNode(GenTree* tree, regMaskTP internalCands)
{
    bool fixedReg = false;
    // The candidate set should contain only integer registers.
    assert((internalCands & ~allRegs(TYP_INT)) == RBM_NONE);
    if (genMaxOneBit(internalCands))
    {
        fixedReg = true;
    }

    RefPosition* defRefPosition = defineNewInternalTemp(tree, IntRegisterType, internalCands);
    return defRefPosition;
}

//------------------------------------------------------------------------
// buildInternalFloatRegisterDefForNode - Create an Interval for an internal fp register, and a def RefPosition
//
// Arguments:
//   tree                  - Gentree node that needs internal registers
//   internalCands         - The mask of valid registers
//
// Returns:
//   The def RefPosition created for this internal temp.
//
RefPosition* LinearScan::buildInternalFloatRegisterDefForNode(GenTree* tree, regMaskTP internalCands)
{
    bool fixedReg = false;
    // The candidate set should contain only float registers.
    assert((internalCands & ~allRegs(TYP_FLOAT)) == RBM_NONE);
    if (genMaxOneBit(internalCands))
    {
        fixedReg = true;
    }

    RefPosition* defRefPosition = defineNewInternalTemp(tree, FloatRegisterType, internalCands);
    return defRefPosition;
}

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
    assert(varTypeNeedsPartialCalleeSave(lclVarInterval->registerType));
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
    if (enregisterLocalVars && !VarSetOps::IsEmpty(compiler, largeVectorVars))
    {
        assert((fpCalleeKillSet & RBM_FLT_CALLEE_TRASH) != RBM_NONE);

        // We only need to save the upper half of any large vector vars that are currently live.
        VARSET_TP       liveLargeVectors(VarSetOps::Intersection(compiler, currentLiveVars, largeVectorVars));
        VarSetOps::Iter iter(compiler, liveLargeVectors);
        unsigned        varIndex = 0;
        while (iter.NextElem(&varIndex))
        {
            Interval* varInterval = getIntervalForLocalVar(varIndex);
            if (!varInterval->isPartiallySpilled)
            {
                Interval*    upperVectorInterval = getUpperVectorInterval(varIndex);
                RefPosition* pos =
                    newRefPosition(upperVectorInterval, currentLoc, RefTypeUpperVectorSave, tree, RBM_FLT_CALLEE_SAVED);
                varInterval->isPartiallySpilled = true;
#ifdef _TARGET_XARCH_
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
        if (varTypeNeedsPartialCalleeSave(listNode->treeNode->TypeGet()))
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
//
void LinearScan::buildUpperVectorRestoreRefPosition(Interval* lclVarInterval, LsraLocation currentLoc, GenTree* node)
{
    if (lclVarInterval->isPartiallySpilled)
    {
        unsigned     varIndex            = lclVarInterval->getVarIndex(compiler);
        Interval*    upperVectorInterval = getUpperVectorInterval(varIndex);
        RefPosition* pos = newRefPosition(upperVectorInterval, currentLoc, RefTypeUpperVectorRestore, node, RBM_NONE);
        lclVarInterval->isPartiallySpilled = false;
#ifdef _TARGET_XARCH_
        pos->regOptional = true;
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
// static
int LinearScan::ComputeOperandDstCount(GenTree* operand)
{
    // GT_ARGPLACE is the only non-LIR node that is currently in the trees at this stage, though
    // note that it is not in the linear order. It seems best to check for !IsLIR() rather than
    // GT_ARGPLACE directly, since it's that characteristic that makes it irrelevant for this method.
    if (!operand->IsLIR())
    {
        return 0;
    }
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
        // Operands that are values and are not contained consume all of their operands
        // and produce one or more registers.
        return operand->GetRegisterDstCount();
    }
    else
    {
        // This must be one of the operand types that are neither contained nor produce a value.
        // Stores and void-typed operands may be encountered when processing call nodes, which contain
        // pointers to argument setup stores.
        assert(operand->OperIsStore() || operand->OperIsBlkOp() || operand->OperIsPutArgStk() ||
               operand->OperIsCompare() || operand->OperIs(GT_CMP) || operand->IsSIMDEqualityOrInequality() ||
               operand->TypeGet() == TYP_VOID);
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
// static
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
//    block      - The BasicBlock in which the node resides
//    currentLoc - The LsraLocation of the given node
//
void LinearScan::buildRefPositionsForNode(GenTree* tree, BasicBlock* block, LsraLocation currentLoc)
{
    // The LIR traversal doesn't visit GT_LIST or GT_ARGPLACE nodes.
    // GT_CLS_VAR nodes should have been eliminated by rationalizer.
    assert(tree->OperGet() != GT_ARGPLACE);
    assert(tree->OperGet() != GT_LIST);
    assert(tree->OperGet() != GT_CLS_VAR);

    // The LIR traversal visits only the first node in a GT_FIELD_LIST.
    assert((tree->OperGet() != GT_FIELD_LIST) || tree->AsFieldList()->IsFieldListHead());

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
#ifdef _TARGET_XARCH_
        // On XArch we can have contained candidate lclVars if they are part of a RMW
        // address computation. In this case we need to check whether it is a last use.
        if (tree->IsLocal() && ((tree->gtFlags & GTF_VAR_DEATH) != 0))
        {
            LclVarDsc* const varDsc = &compiler->lvaTable[tree->AsLclVarCommon()->gtLclNum];
            if (isCandidateVar(varDsc))
            {
                assert(varDsc->lvTracked);
                unsigned varIndex = varDsc->lvVarIndex;
                VarSetOps::RemoveElemD(compiler, currentLiveVars, varIndex);
            }
        }
#else  // _TARGET_XARCH_
        assert(!isCandidateLocalRef(tree));
#endif // _TARGET_XARCH_
        JITDUMP("Contained\n");
        return;
    }

#ifdef DEBUG
    // If we are constraining the registers for allocation, we will modify all the RefPositions
    // we've built for this node after we've created them. In order to do that, we'll remember
    // the last RefPosition prior to those created for this node.
    RefPositionIterator refPositionMark = refPositions.backPosition();
    int                 oldDefListCount = defList.Count();
#endif // DEBUG

    int consume = BuildNode(tree);

#ifdef DEBUG
    int newDefListCount = defList.Count();
    int produce         = newDefListCount - oldDefListCount;
    assert((consume == 0) || (ComputeAvailableSrcCount(tree) == consume));

#if defined(_TARGET_AMD64_)
    // Multi-reg call node is the only node that could produce multi-reg value
    assert(produce <= 1 || (tree->IsMultiRegCall() && produce == MAX_RET_REG_COUNT));
#endif // _TARGET_AMD64_

#endif // DEBUG

#ifdef DEBUG
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
                newRefPosition->registerAssignment =
                    getConstrainedRegMask(oldAssignment, calleeSaveMask, minRegCountForRef);
                if ((newRefPosition->registerAssignment != oldAssignment) && (newRefPosition->refType == RefTypeUse) &&
                    !interval->isLocalVar)
                {
                    checkConflictingDefUse(newRefPosition);
                }
            }
        }
    }
#endif // DEBUG
    JITDUMP("\n");
}

//------------------------------------------------------------------------
// buildPhysRegRecords: Make an interval for each physical register
//
void LinearScan::buildPhysRegRecords()
{
    RegisterType regType = IntRegisterType;
    for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
    {
        RegRecord* curr = &physRegs[reg];
        curr->init(reg);
    }
}

//------------------------------------------------------------------------
// getNonEmptyBlock: Return the first non-empty block starting with 'block'
//
// Arguments:
//    block - the BasicBlock from which we start looking
//
// Return Value:
//    The first non-empty BasicBlock we find.
//
BasicBlock* getNonEmptyBlock(BasicBlock* block)
{
    while (block != nullptr && block->bbTreeList == nullptr)
    {
        BasicBlock* nextBlock = block->bbNext;
        // Note that here we use the version of NumSucc that does not take a compiler.
        // That way this doesn't have to take a compiler, or be an instance method, e.g. of LinearScan.
        // If we have an empty block, it must have jump type BBJ_NONE or BBJ_ALWAYS, in which
        // case we don't need the version that takes a compiler.
        assert(block->NumSucc() == 1 && ((block->bbJumpKind == BBJ_ALWAYS) || (block->bbJumpKind == BBJ_NONE)));
        // sometimes the first block is empty and ends with an uncond branch
        // assert( block->GetSucc(0) == nextBlock);
        block = nextBlock;
    }
    assert(block != nullptr && block->bbTreeList != nullptr);
    return block;
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
        unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
        LclVarDsc* varDsc = compiler->lvaTable + varNum;
        if (!varDsc->lvIsParam && isCandidateVar(varDsc))
        {
            JITDUMP("V%02u was live in to first block:", varNum);
            Interval* interval = getIntervalForLocalVar(varIndex);
            if (compiler->info.compInitMem || varTypeIsGC(varDsc->TypeGet()))
            {
                JITDUMP(" creating ZeroInit\n");
                GenTree*     firstNode = getNonEmptyBlock(compiler->fgFirstBB)->firstNode();
                RefPosition* pos =
                    newRefPosition(interval, MinLocation, RefTypeZeroInit, firstNode, allRegs(interval->registerType));
                pos->setRegOptional(true);
                varDsc->lvMustInit = true;
            }
            else
            {
                setIntervalAsSpilled(interval);
                JITDUMP(" marking as spilled\n");
            }
        }
    }
}

#if defined(UNIX_AMD64_ABI)
//------------------------------------------------------------------------
// unixAmd64UpdateRegStateForArg: Sets the register state for an argument of type STRUCT for System V systems.
//
// Arguments:
//    argDsc - the LclVarDsc for the argument of interest
//
// Notes:
//     See Compiler::raUpdateRegStateForArg(RegState *regState, LclVarDsc *argDsc) in regalloc.cpp
//         for how state for argument is updated for unix non-structs and Windows AMD64 structs.
//
void LinearScan::unixAmd64UpdateRegStateForArg(LclVarDsc* argDsc)
{
    assert(varTypeIsStruct(argDsc));
    RegState* intRegState   = &compiler->codeGen->intRegState;
    RegState* floatRegState = &compiler->codeGen->floatRegState;

    if ((argDsc->lvArgReg != REG_STK) && (argDsc->lvArgReg != REG_NA))
    {
        if (genRegMask(argDsc->lvArgReg) & (RBM_ALLFLOAT))
        {
            assert(genRegMask(argDsc->lvArgReg) & (RBM_FLTARG_REGS));
            floatRegState->rsCalleeRegArgMaskLiveIn |= genRegMask(argDsc->lvArgReg);
        }
        else
        {
            assert(genRegMask(argDsc->lvArgReg) & (RBM_ARG_REGS));
            intRegState->rsCalleeRegArgMaskLiveIn |= genRegMask(argDsc->lvArgReg);
        }
    }

    if ((argDsc->lvOtherArgReg != REG_STK) && (argDsc->lvOtherArgReg != REG_NA))
    {
        if (genRegMask(argDsc->lvOtherArgReg) & (RBM_ALLFLOAT))
        {
            assert(genRegMask(argDsc->lvOtherArgReg) & (RBM_FLTARG_REGS));
            floatRegState->rsCalleeRegArgMaskLiveIn |= genRegMask(argDsc->lvOtherArgReg);
        }
        else
        {
            assert(genRegMask(argDsc->lvOtherArgReg) & (RBM_ARG_REGS));
            intRegState->rsCalleeRegArgMaskLiveIn |= genRegMask(argDsc->lvOtherArgReg);
        }
    }
}

#endif // defined(UNIX_AMD64_ABI)

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
// Notes:
//    This relies on a method in regAlloc.cpp that is shared between LSRA
//    and regAlloc.  It is further abstracted here because regState is updated
//    separately for tracked and untracked variables in LSRA.
//
void LinearScan::updateRegStateForArg(LclVarDsc* argDsc)
{
#if defined(UNIX_AMD64_ABI)
    // For System V AMD64 calls the argDsc can have 2 registers (for structs.)
    // Handle them here.
    if (varTypeIsStruct(argDsc))
    {
        unixAmd64UpdateRegStateForArg(argDsc);
    }
    else
#endif // defined(UNIX_AMD64_ABI)
    {
        RegState* intRegState   = &compiler->codeGen->intRegState;
        RegState* floatRegState = &compiler->codeGen->floatRegState;
        bool      isFloat       = emitter::isFloatReg(argDsc->lvArgReg);

        if (argDsc->lvIsHfaRegArg())
        {
            isFloat = true;
        }

        if (isFloat)
        {
            JITDUMP("Float arg V%02u in reg %s\n", (argDsc - compiler->lvaTable), getRegName(argDsc->lvArgReg));
            compiler->raUpdateRegStateForArg(floatRegState, argDsc);
        }
        else
        {
            JITDUMP("Int arg V%02u in reg %s\n", (argDsc - compiler->lvaTable), getRegName(argDsc->lvArgReg));
#if FEATURE_MULTIREG_ARGS
            if (argDsc->lvOtherArgReg != REG_NA)
            {
                JITDUMP("(second half) in reg %s\n", getRegName(argDsc->lvOtherArgReg));
            }
#endif // FEATURE_MULTIREG_ARGS
            compiler->raUpdateRegStateForArg(intRegState, argDsc);
        }
    }
}

//------------------------------------------------------------------------
// buildIntervals: The main entry point for building the data structures over
//                 which we will do register allocation.
//
void LinearScan::buildIntervals()
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
        foreach_block(compiler, block)
        {
            printf(FMT_BB " use def in out\n", block->bbNum);
            dumpConvertedVarSet(compiler, block->bbVarUse);
            printf("\n");
            dumpConvertedVarSet(compiler, block->bbVarDef);
            printf("\n");
            dumpConvertedVarSet(compiler, block->bbLiveIn);
            printf("\n");
            dumpConvertedVarSet(compiler, block->bbLiveOut);
            printf("\n");
        }
    }
#endif // DEBUG

#if DOUBLE_ALIGN
    // We will determine whether we should double align the frame during
    // identifyCandidates(), but we initially assume that we will not.
    doDoubleAlign = false;
#endif

    identifyCandidates();

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
    curBBNum = blockSequence[bbSeqCount - 1]->bbNum;

    // Next, create ParamDef RefPositions for all the tracked parameters, in order of their varIndex.
    // Assign these RefPositions to the (nonexistent) BB0.
    curBBNum = 0;

    LclVarDsc*   argDsc;
    unsigned int lclNum;

    RegState* intRegState                   = &compiler->codeGen->intRegState;
    RegState* floatRegState                 = &compiler->codeGen->floatRegState;
    intRegState->rsCalleeRegArgMaskLiveIn   = RBM_NONE;
    floatRegState->rsCalleeRegArgMaskLiveIn = RBM_NONE;

    for (unsigned int varIndex = 0; varIndex < compiler->lvaTrackedCount; varIndex++)
    {
        lclNum = compiler->lvaTrackedToVarNum[varIndex];
        argDsc = &(compiler->lvaTable[lclNum]);

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
            Interval* interval = getIntervalForLocalVar(varIndex);
            regMaskTP mask     = allRegs(TypeGet(argDsc));
            if (argDsc->lvIsRegArg)
            {
                // Set this interval as currently assigned to that register
                regNumber inArgReg = argDsc->lvArgReg;
                assert(inArgReg < REG_COUNT);
                mask = genRegMask(inArgReg);
                assignPhysReg(inArgReg, interval);
            }
            RefPosition* pos = newRefPosition(interval, MinLocation, RefTypeParamDef, nullptr, mask);
            pos->setRegOptional(true);
        }
        else if (varTypeIsStruct(argDsc->lvType))
        {
            for (unsigned fieldVarNum = argDsc->lvFieldLclStart;
                 fieldVarNum < argDsc->lvFieldLclStart + argDsc->lvFieldCnt; ++fieldVarNum)
            {
                LclVarDsc* fieldVarDsc = &(compiler->lvaTable[fieldVarNum]);
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

    for (unsigned argNum = 0; argNum < compiler->info.compArgsCount; argNum++, argDsc++)
    {
        argDsc = &(compiler->lvaTable[argNum]);

        if (argDsc->lvPromotedStruct())
        {
            noway_assert(argDsc->lvFieldCnt == 1); // We only handle one field here

            unsigned fieldVarNum = argDsc->lvFieldLclStart;
            argDsc               = &(compiler->lvaTable[fieldVarNum]);
        }
        noway_assert(argDsc->lvIsParam);
        if (!argDsc->lvTracked && argDsc->lvIsRegArg)
        {
            updateRegStateForArg(argDsc);
        }
    }

    // If there is a secret stub param, it is also live in
    if (compiler->info.compPublishStubParam)
    {
        intRegState->rsCalleeRegArgMaskLiveIn |= RBM_SECRET_STUB_PARAM;
    }

    BasicBlock* predBlock = nullptr;
    BasicBlock* prevBlock = nullptr;

    // Initialize currentLiveVars to the empty set.  We will set it to the current
    // live-in at the entry to each block (this will include the incoming args on
    // the first block).
    VarSetOps::AssignNoCopy(compiler, currentLiveVars, VarSetOps::MakeEmpty(compiler));

    for (block = startBlockSequence(); block != nullptr; block = moveToNextBlock())
    {
        JITDUMP("\nNEW BLOCK " FMT_BB "\n", block->bbNum);

        bool predBlockIsAllocated = false;
        predBlock                 = findPredBlockForLiveIn(block, prevBlock DEBUGARG(&predBlockIsAllocated));
        if (predBlock)
        {
            JITDUMP("\n\nSetting " FMT_BB " as the predecessor for determining incoming variable registers of " FMT_BB
                    "\n",
                    block->bbNum, predBlock->bbNum);
            assert(predBlock->bbNum <= bbNumMaxBeforeResolution);
            blockInfo[block->bbNum].predBBNum = predBlock->bbNum;
        }

        if (enregisterLocalVars)
        {
            VarSetOps::AssignNoCopy(compiler, currentLiveVars,
                                    VarSetOps::Intersection(compiler, registerCandidateVars, block->bbLiveIn));

            if (block == compiler->fgFirstBB)
            {
                insertZeroInitRefPositions();
                // The first real location is at 1; 0 is for the entry.
                currentLoc = 1;
            }

            // Any lclVars live-in to a block are resolution candidates.
            VarSetOps::UnionD(compiler, resolutionCandidateVars, currentLiveVars);

            // Determine if we need any DummyDefs.
            // We need DummyDefs for cases where "predBlock" isn't really a predecessor.
            // Note that it's possible to have uses of unitialized variables, in which case even the first
            // block may require DummyDefs, which we are not currently adding - this means that these variables
            // will always be considered to be in memory on entry (and reloaded when the use is encountered).
            // TODO-CQ: Consider how best to tune this.  Currently, if we create DummyDefs for uninitialized
            // variables (which may actually be initialized along the dynamically executed paths, but not
            // on all static paths), we wind up with excessive liveranges for some of these variables.
            VARSET_TP newLiveIn(VarSetOps::MakeCopy(compiler, currentLiveVars));
            if (predBlock)
            {
                // Compute set difference: newLiveIn = currentLiveVars - predBlock->bbLiveOut
                VarSetOps::DiffD(compiler, newLiveIn, predBlock->bbLiveOut);
            }
            bool needsDummyDefs = (!VarSetOps::IsEmpty(compiler, newLiveIn) && block != compiler->fgFirstBB);

            // Create dummy def RefPositions

            if (needsDummyDefs)
            {
                // If we are using locations from a predecessor, we should never require DummyDefs.
                assert(!predBlockIsAllocated);

                JITDUMP("Creating dummy definitions\n");
                VarSetOps::Iter iter(compiler, newLiveIn);
                unsigned        varIndex = 0;
                while (iter.NextElem(&varIndex))
                {
                    unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
                    LclVarDsc* varDsc = compiler->lvaTable + varNum;
                    // Add a dummyDef for any candidate vars that are in the "newLiveIn" set.
                    // If this is the entry block, don't add any incoming parameters (they're handled with ParamDefs).
                    if (isCandidateVar(varDsc) && (predBlock != nullptr || !varDsc->lvIsParam))
                    {
                        Interval*    interval = getIntervalForLocalVar(varIndex);
                        RefPosition* pos      = newRefPosition(interval, currentLoc, RefTypeDummyDef, nullptr,
                                                          allRegs(interval->registerType));
                        pos->setRegOptional(true);
                    }
                }
                JITDUMP("Finished creating dummy definitions\n\n");
            }
        }

        // Add a dummy RefPosition to mark the block boundary.
        // Note that we do this AFTER adding the exposed uses above, because the
        // register positions for those exposed uses need to be recorded at
        // this point.

        RefPosition* pos = newRefPosition((Interval*)nullptr, currentLoc, RefTypeBB, nullptr, RBM_NONE);
        currentLoc += 2;
        JITDUMP("\n");

        LIR::Range& blockRange = LIR::AsRange(block);
        for (GenTree* node : blockRange.NonPhiNodes())
        {
            // We increment the number position of each tree node by 2 to simplify the logic when there's the case of
            // a tree that implicitly does a dual-definition of temps (the long case).  In this case it is easier to
            // already have an idle spot to handle a dual-def instead of making some messy adjustments if we only
            // increment the number position by one.
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
            node->gtSeqNum = currentLoc;
            // In DEBUG, we want to set the gtRegTag to GT_REGTAG_REG, so that subsequent dumps will so the register
            // value.
            // Although this looks like a no-op it sets the tag.
            node->gtRegNum = node->gtRegNum;
#endif

            buildRefPositionsForNode(node, block, currentLoc);

#ifdef DEBUG
            if (currentLoc > maxNodeLocation)
            {
                maxNodeLocation = currentLoc;
            }
#endif // DEBUG
            currentLoc += 2;
        }

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        // At the end of each block, create upperVectorRestores for any largeVectorVars that may be
        // partiallySpilled (during the build phase all intervals will be marked isPartiallySpilled if
        // they *may) be partially spilled at any point.
        if (enregisterLocalVars)
        {
            VarSetOps::Iter largeVectorVarsIter(compiler, largeVectorVars);
            unsigned        largeVectorVarIndex = 0;
            while (largeVectorVarsIter.NextElem(&largeVectorVarIndex))
            {
                Interval* lclVarInterval = getIntervalForLocalVar(largeVectorVarIndex);
                buildUpperVectorRestoreRefPosition(lclVarInterval, currentLoc, nullptr);
            }
        }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

        // Note: the visited set is cleared in LinearScan::doLinearScan()
        markBlockVisited(block);
        if (!defList.IsEmpty())
        {
            INDEBUG(dumpDefList());
            assert(!"Expected empty defList at end of block");
        }

        if (enregisterLocalVars)
        {
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
            for (BasicBlock* succ : block->GetAllSuccs(compiler))
            {
                if (VarSetOps::IsEmpty(compiler, expUseSet))
                {
                    break;
                }

                if (isBlockVisited(succ))
                {
                    continue;
                }
                VarSetOps::DiffD(compiler, expUseSet, succ->bbLiveIn);
            }

            if (!VarSetOps::IsEmpty(compiler, expUseSet))
            {
                JITDUMP("Exposed uses:");
                VarSetOps::Iter iter(compiler, expUseSet);
                unsigned        varIndex = 0;
                while (iter.NextElem(&varIndex))
                {
                    unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
                    LclVarDsc* varDsc = compiler->lvaTable + varNum;
                    assert(isCandidateVar(varDsc));
                    Interval*    interval = getIntervalForLocalVar(varIndex);
                    RefPosition* pos =
                        newRefPosition(interval, currentLoc, RefTypeExpUse, nullptr, allRegs(interval->registerType));
                    pos->setRegOptional(true);
                    JITDUMP(" V%02u", varNum);
                }
                JITDUMP("\n");
            }

            // Clear the "last use" flag on any vars that are live-out from this block.
            {
                VARSET_TP       bbLiveDefs(VarSetOps::Intersection(compiler, registerCandidateVars, block->bbLiveOut));
                VarSetOps::Iter iter(compiler, bbLiveDefs);
                unsigned        varIndex = 0;
                while (iter.NextElem(&varIndex))
                {
                    unsigned         varNum = compiler->lvaTrackedToVarNum[varIndex];
                    LclVarDsc* const varDsc = &compiler->lvaTable[varNum];
                    assert(isCandidateVar(varDsc));
                    RefPosition* const lastRP = getIntervalForLocalVar(varIndex)->lastRefPosition;
                    // We should be able to assert that lastRP is non-null if it is live-out, but sometimes liveness
                    // lies.
                    if ((lastRP != nullptr) && (lastRP->bbNum == block->bbNum))
                    {
                        lastRP->lastUse = false;
                    }
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

        prevBlock = block;
    }

    if (enregisterLocalVars)
    {
        if (compiler->lvaKeepAliveAndReportThis())
        {
            // If we need to KeepAliveAndReportThis, add a dummy exposed use of it at the end
            unsigned keepAliveVarNum = compiler->info.compThisArg;
            assert(compiler->info.compIsStatic == false);
            LclVarDsc* varDsc = compiler->lvaTable + keepAliveVarNum;
            if (isCandidateVar(varDsc))
            {
                JITDUMP("Adding exposed use of this, for lvaKeepAliveAndReportThis\n");
                Interval*    interval = getIntervalForLocalVar(varDsc->lvVarIndex);
                RefPosition* pos =
                    newRefPosition(interval, currentLoc, RefTypeExpUse, nullptr, allRegs(interval->registerType));
                pos->setRegOptional(true);
            }
        }

#ifdef DEBUG
        if (getLsraExtendLifeTimes())
        {
            LclVarDsc* varDsc;
            for (lclNum = 0, varDsc = compiler->lvaTable; lclNum < compiler->lvaCount; lclNum++, varDsc++)
            {
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

#ifdef DEBUG
    // Make sure we don't have any blocks that were not visited
    foreach_block(compiler, block)
    {
        assert(isBlockVisited(block));
    }

    if (VERBOSE)
    {
        lsraDumpIntervals("BEFORE VALIDATING INTERVALS");
        dumpRefPositions("BEFORE VALIDATING INTERVALS");
        validateIntervals();
    }
#endif // DEBUG
}

#ifdef DEBUG
//------------------------------------------------------------------------
// validateIntervals: A DEBUG-only method that checks that the lclVar RefPositions
//                    do not reflect uses of undefined values
//
// Notes: If an undefined use is encountered, it merely prints a message.
//
// TODO-Cleanup: This should probably assert, or at least print the message only
//               when doing a JITDUMP.
//
void LinearScan::validateIntervals()
{
    if (enregisterLocalVars)
    {
        for (unsigned i = 0; i < compiler->lvaTrackedCount; i++)
        {
            if (!compiler->lvaTable[compiler->lvaTrackedToVarNum[i]].lvLRACandidate)
            {
                continue;
            }
            Interval* interval = getIntervalForLocalVar(i);

            bool defined = false;
            printf("-----------------\n");
            for (RefPosition* ref = interval->firstRefPosition; ref != nullptr; ref = ref->nextRefPosition)
            {
                ref->dump();
                RefType refType = ref->refType;
                if (!defined && RefTypeIsUse(refType))
                {
                    if (compiler->info.compMethodName != nullptr)
                    {
                        printf("%s: ", compiler->info.compMethodName);
                    }
                    printf("LocalVar V%02u: undefined use at %u\n", interval->varNum, ref->nodeLocation);
                }
                // Note that there can be multiple last uses if they are on disjoint paths,
                // so we can't really check the lastUse flag
                if (ref->lastUse)
                {
                    defined = false;
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

#ifdef _TARGET_XARCH_
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
#endif // _TARGET_XARCH_
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
    RegisterType type = getDefType(tree);

    if (dstCandidates != RBM_NONE)
    {
        assert((tree->gtRegNum == REG_NA) || (dstCandidates == genRegMask(tree->GetRegByIndex(multiRegIdx))));
    }

    if (tree->IsMultiRegNode())
    {
        type = tree->GetRegTypeByIndex(multiRegIdx);
    }

    Interval* interval = newInterval(type);
    if (tree->gtRegNum != REG_NA)
    {
        if (!tree->IsMultiRegNode() || (multiRegIdx == 0))
        {
            assert((dstCandidates == RBM_NONE) || (dstCandidates == genRegMask(tree->gtRegNum)));
            dstCandidates = genRegMask(tree->gtRegNum);
        }
        else
        {
            assert(isSingleRegister(dstCandidates));
        }
    }
#ifdef _TARGET_X86_
    else if (varTypeIsByte(tree))
    {
        if (dstCandidates == RBM_NONE)
        {
            dstCandidates = allRegs(TYP_INT);
        }
        dstCandidates &= ~RBM_NON_BYTE_REGS;
        assert(dstCandidates != RBM_NONE);
    }
#endif // _TARGET_X86_
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
#ifdef _TARGET_XARCH_
    setTgtPref(interval, tgtPrefUse);
    setTgtPref(interval, tgtPrefUse2);
#endif // _TARGET_XARCH_
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
    ReturnTypeDesc* retTypeDesc = nullptr;
    if (tree->IsMultiRegCall())
    {
        retTypeDesc = tree->AsCall()->GetReturnTypeDesc();
    }
    for (int i = 0; i < dstCount; i++)
    {
        regMaskTP thisDstCandidates;
        if (fixedReg)
        {
            // In case of multi-reg call node, we have to query the ith position return register.
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
// BuildUse: Remove the RefInfoListNode for the given multi-reg index of the given node from
//           the defList, and build a use RefPosition for the associated Interval.
//
// Arguments:
//    operand      - The node of interest
//    candidates   - The register candidates for the use
//    multiRegIdx  - The index of the multireg def/use
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
        }
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        buildUpperVectorRestoreRefPosition(interval, currentLoc, operand);
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
    GenTree* const addr = indirTree->gtOp1;
    return BuildAddrUses(addr, candidates);
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
    if ((addrMode->Base() != nullptr) && !addrMode->Base()->isContained())
    {
        BuildUse(addrMode->Base(), candidates);
        srcCount++;
    }
    if ((addrMode->Index() != nullptr) && !addrMode->Index()->isContained())
    {
        BuildUse(addrMode->Index(), candidates);
        srcCount++;
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildOperandUses: Build Use RefPositions for an operand that might be contained.
//
// Arguments:
//    node      - The node of interest
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

#if !defined(_TARGET_64BIT_)
    if (node->OperIs(GT_LONG))
    {
        return BuildBinaryUses(node->AsOp(), candidates);
    }
#endif // !defined(_TARGET_64BIT_)
    if (node->OperIsIndir())
    {
        return BuildIndirUses(node->AsIndir(), candidates);
    }
    if (node->OperIs(GT_LEA))
    {
        return BuildAddrUses(node, candidates);
    }
#ifdef FEATURE_HW_INTRINSICS
    if (node->OperIsHWIntrinsic())
    {
        if (node->AsHWIntrinsic()->OperIsMemoryLoad())
        {
            return BuildAddrUses(node->gtGetOp1());
        }
        BuildUse(node->gtGetOp1(), candidates);
        return 1;
    }
#endif // FEATURE_HW_INTRINSICS

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
// BuildDelayFreeUses: Build Use RefPositions for an operand that might be contained,
//                     and which need to be marked delayRegFree
//
// Arguments:
//    node      - The node of interest
//
// Return Value:
//    The number of source registers used by the *parent* of this node.
//
int LinearScan::BuildDelayFreeUses(GenTree* node, regMaskTP candidates)
{
    RefPosition* use;
    if (!node->isContained())
    {
        use = BuildUse(node, candidates);
        setDelayFree(use);
        return 1;
    }
    if (node->OperIsHWIntrinsic())
    {
        use = BuildUse(node->gtGetOp1(), candidates);
        setDelayFree(use);
        return 1;
    }
    if (!node->OperIsIndir())
    {
        return 0;
    }
    GenTreeIndir* indirTree = node->AsIndir();
    GenTree*      addr      = indirTree->gtOp1;
    if (!addr->isContained())
    {
        use = BuildUse(addr, candidates);
        setDelayFree(use);
        return 1;
    }
    if (!addr->OperIs(GT_LEA))
    {
        return 0;
    }

    GenTreeAddrMode* const addrMode = addr->AsAddrMode();

    unsigned srcCount = 0;
    if ((addrMode->Base() != nullptr) && !addrMode->Base()->isContained())
    {
        use = BuildUse(addrMode->Base(), candidates);
        setDelayFree(use);
        srcCount++;
    }
    if ((addrMode->Index() != nullptr) && !addrMode->Index()->isContained())
    {
        use = BuildUse(addrMode->Index(), candidates);
        setDelayFree(use);
        srcCount++;
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
#ifdef _TARGET_XARCH_
    if (node->OperIsBinary() && isRMWRegOper(node))
    {
        return BuildRMWUses(node, candidates);
    }
#endif // _TARGET_XARCH_
    int      srcCount = 0;
    GenTree* op1      = node->gtOp1;
    GenTree* op2      = node->gtGetOp2IfPresent();
    if (node->IsReverseOp() && (op2 != nullptr))
    {
        srcCount += BuildOperandUses(op2, candidates);
        op2 = nullptr;
    }
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
// BuildStoreLoc: Set register requirements for a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Setting the appropriate candidates for a store of a multi-reg call return value.
//    - Handling of contained immediates.
//    - Requesting an internal register for SIMD12 stores.
//
int LinearScan::BuildStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    GenTree*     op1 = storeLoc->gtGetOp1();
    int          srcCount;
    RefPosition* singleUseRef = nullptr;
    LclVarDsc*   varDsc       = &compiler->lvaTable[storeLoc->gtLclNum];

// First, define internal registers.
#ifdef FEATURE_SIMD
    RefPosition* internalFloatDef = nullptr;
    if (varTypeIsSIMD(storeLoc) && !op1->IsCnsIntOrI() && (storeLoc->TypeGet() == TYP_SIMD12))
    {
        // Need an additional register to extract upper 4 bytes of Vector3.
        internalFloatDef = buildInternalFloatRegisterDefForNode(storeLoc, allSIMDRegs());
    }
#endif // FEATURE_SIMD

    // Second, use source registers.

    if (op1->IsMultiRegCall())
    {
        // This is the case of var = call where call is returning
        // a value in multiple return registers.
        // Must be a store lclvar.
        assert(storeLoc->OperGet() == GT_STORE_LCL_VAR);

        // srcCount = number of registers in which the value is returned by call
        GenTreeCall*    call        = op1->AsCall();
        ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
        unsigned        regCount    = retTypeDesc->GetReturnRegCount();
        srcCount                    = retTypeDesc->GetReturnRegCount();

        for (int i = 0; i < srcCount; ++i)
        {
            BuildUse(op1, RBM_NONE, i);
        }
    }
#ifndef _TARGET_64BIT_
    else if (varTypeIsLong(op1))
    {
        if (op1->OperIs(GT_MUL_LONG))
        {
            srcCount = 2;
            BuildUse(op1, allRegs(TYP_INT), 0);
            BuildUse(op1, allRegs(TYP_INT), 1);
        }
        else
        {
            assert(op1->OperIs(GT_LONG));
            assert(op1->isContained() && !op1->gtGetOp1()->isContained() && !op1->gtGetOp2()->isContained());
            srcCount = BuildBinaryUses(op1->AsOp());
            assert(srcCount == 2);
        }
    }
#endif // !_TARGET_64BIT_
    else if (op1->isContained())
    {
        srcCount = 0;
    }
    else
    {
        srcCount                = 1;
        regMaskTP srcCandidates = RBM_NONE;
#ifdef _TARGET_X86_
        if (varTypeIsByte(storeLoc))
        {
            srcCandidates = allByteRegs();
        }
#endif // _TARGET_X86_
        singleUseRef = BuildUse(op1, srcCandidates);
    }

// Third, use internal registers.
#ifdef FEATURE_SIMD
    buildInternalRegisterUses();
#endif // FEATURE_SIMD

    // Fourth, define destination registers.

    // Add the lclVar to currentLiveVars (if it will remain live)
    if (isCandidateVar(varDsc))
    {
        assert(varDsc->lvTracked);
        unsigned  varIndex       = varDsc->lvVarIndex;
        Interval* varDefInterval = getIntervalForLocalVar(varIndex);
        if ((storeLoc->gtFlags & GTF_VAR_DEATH) == 0)
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
        newRefPosition(varDefInterval, currentLoc + 1, RefTypeDef, storeLoc, allRegs(storeLoc->TypeGet()));
    }
    else
    {
        if (storeLoc->gtOp1->OperIs(GT_BITCAST))
        {
            storeLoc->gtType = storeLoc->gtOp1->gtType = storeLoc->gtOp1->AsUnOp()->gtOp1->TypeGet();
            RegisterType registerType                  = regType(storeLoc->TypeGet());
            noway_assert(singleUseRef != nullptr);

            Interval* srcInterval     = singleUseRef->getInterval();
            srcInterval->registerType = registerType;

            RefPosition* srcDefPosition = srcInterval->firstRefPosition;
            assert(srcDefPosition != nullptr);
            assert(srcDefPosition->refType == RefTypeDef);
            assert(srcDefPosition->treeNode == storeLoc->gtOp1);

            srcDefPosition->registerAssignment = allRegs(registerType);
            singleUseRef->registerAssignment   = allRegs(registerType);
        }
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
    if ((kind & (GTK_CONST | GTK_LEAF)) == 0)
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

#if !defined(_TARGET_64BIT_)
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
#endif // !defined(_TARGET_64BIT_)
        if ((tree->TypeGet() != TYP_VOID) && !op1->isContained())
    {
        regMaskTP useCandidates = RBM_NONE;

#if FEATURE_MULTIREG_RET
#ifdef _TARGET_ARM64_
        if (varTypeIsSIMD(tree))
        {
            useCandidates = allSIMDRegs();
            BuildUse(op1, useCandidates);
            return 1;
        }
#endif // !_TARGET_ARM64_

        if (varTypeIsStruct(tree))
        {
            // op1 has to be either an lclvar or a multi-reg returning call
            if (op1->OperGet() == GT_LCL_VAR)
            {
                BuildUse(op1, useCandidates);
            }
            else
            {
                noway_assert(op1->IsMultiRegCall());

                ReturnTypeDesc* retTypeDesc = op1->AsCall()->GetReturnTypeDesc();
                int             srcCount    = retTypeDesc->GetReturnRegCount();
                useCandidates               = retTypeDesc->GetABIReturnRegs();
                for (int i = 0; i < srcCount; i++)
                {
                    BuildUse(op1, useCandidates, i);
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
#if defined(DEBUG) && defined(_TARGET_X86_)
    // On x86, `LSRA_LIMIT_CALLER` is too restrictive to allow the use of special put args: this stress mode
    // leaves only three registers allocatable--eax, ecx, and edx--of which the latter two are also used for the
    // first two integral arguments to a call. This can leave us with too few registers to succesfully allocate in
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
    regNumber argReg = node->gtRegNum;
    assert(argReg != REG_NA);
    bool     isSpecialPutArg = false;
    int      srcCount        = 1;
    GenTree* op1             = node->gtGetOp1();

    // First, handle the GT_OBJ case, which loads into the arg register
    // (so we don't set the use to prefer that register for the source address).
    if (op1->OperIs(GT_OBJ))
    {
        GenTreeObj* obj  = op1->AsObj();
        GenTree*    addr = obj->Addr();
        unsigned    size = obj->gtBlkSize;
        assert(size <= MAX_PASS_SINGLEREG_BYTES);
        if (addr->OperIsLocalAddr())
        {
            // We don't need a source register.
            assert(addr->isContained());
            srcCount = 0;
        }
        else if (!isPow2(size))
        {
            // We'll need an internal register to do the odd-size load.
            // This can only happen with integer registers.
            assert(genIsValidIntReg(argReg));
            buildInternalIntRegisterDefForNode(node);
            BuildUse(addr);
            buildInternalRegisterUses();
        }
        return srcCount;
    }

    // To avoid redundant moves, have the argument operand computed in the
    // register in which the argument is passed to the call.
    regMaskTP    argMask = genRegMask(argReg);
    RefPosition* use     = BuildUse(op1, argMask);

    if (supportsSpecialPutArg() && isCandidateLocalRef(op1) && ((op1->gtFlags & GTF_VAR_DEATH) == 0))
    {
        // This is the case for a "pass-through" copy of a lclVar.  In the case where it is a non-last-use,
        // we don't want the def of the copy to kill the lclVar register, if it is assigned the same register
        // (which is actually what we hope will happen).
        JITDUMP("Setting putarg_reg as a pass-through of a non-last use lclVar\n");

        // Preference the destination to the interval of the first register defined by the first operand.
        assert(use->getInterval()->isLocalVar);
        isSpecialPutArg = true;
    }

#ifdef _TARGET_ARM_
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
#endif // _TARGET_ARM_
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
#if FEATURE_VARARG
    if (call->IsVarargs() && varTypeIsFloating(argNode))
    {
        *callHasFloatRegArgs = true;

        // We'll have to return the internal def and then later create a use for it.
        regNumber argReg    = argNode->gtRegNum;
        regNumber targetReg = compiler->getCallArgIntRegister(argReg);

        buildInternalIntRegisterDefForNode(call, genRegMask(targetReg));
    }
#endif // FEATURE_VARARG
}

//------------------------------------------------------------------------
// BuildGCWriteBarrier: Handle additional register requirements for a GC write barrier
//
// Arguments:
//    tree    - The STORE_IND for which a write barrier is required
//
int LinearScan::BuildGCWriteBarrier(GenTree* tree)
{
    GenTree* dst  = tree;
    GenTree* addr = tree->gtGetOp1();
    GenTree* src  = tree->gtGetOp2();

    // In the case where we are doing a helper assignment, even if the dst
    // is an indir through an lea, we need to actually instantiate the
    // lea in a register
    assert(!addr->isContained() && !src->isContained());
    int       srcCount       = 2;
    regMaskTP addrCandidates = RBM_ARG_0;
    regMaskTP srcCandidates  = RBM_ARG_1;

#if defined(_TARGET_ARM64_)

    // the 'addr' goes into x14 (REG_WRITE_BARRIER_DST)
    // the 'src'  goes into x15 (REG_WRITE_BARRIER_SRC)
    //
    addrCandidates = RBM_WRITE_BARRIER_DST;
    srcCandidates  = RBM_WRITE_BARRIER_SRC;

#elif defined(_TARGET_X86_) && NOGC_WRITE_BARRIERS

    bool useOptimizedWriteBarrierHelper = compiler->codeGen->genUseOptimizedWriteBarriers(tree, src);
    if (useOptimizedWriteBarrierHelper)
    {
        // Special write barrier:
        // op1 (addr) goes into REG_WRITE_BARRIER (rdx) and
        // op2 (src) goes into any int register.
        addrCandidates = RBM_WRITE_BARRIER;
        srcCandidates  = RBM_WRITE_BARRIER_SRC;
    }

#endif // defined(_TARGET_X86_) && NOGC_WRITE_BARRIERS

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
//    None.
//
int LinearScan::BuildCmp(GenTree* tree)
{
    assert(tree->OperIsCompare() || tree->OperIs(GT_CMP) || tree->OperIs(GT_JCMP));
    regMaskTP dstCandidates = RBM_NONE;
    regMaskTP op1Candidates = RBM_NONE;
    regMaskTP op2Candidates = RBM_NONE;
    GenTree*  op1           = tree->gtGetOp1();
    GenTree*  op2           = tree->gtGetOp2();

#ifdef _TARGET_X86_
    // If the compare is used by a jump, we just need to set the condition codes. If not, then we need
    // to store the result into the low byte of a register, which requires the dst be a byteable register.
    if (tree->TypeGet() != TYP_VOID)
    {
        dstCandidates = allByteRegs();
    }
    bool needByteRegs = false;
    if (varTypeIsByte(tree))
    {
        if (!varTypeIsFloating(op1))
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
#endif // _TARGET_X86_

    int srcCount = BuildOperandUses(op1, op1Candidates);
    srcCount += BuildOperandUses(op2, op2Candidates);
    if (tree->TypeGet() != TYP_VOID)
    {
        BuildDef(tree, dstCandidates);
    }
    return srcCount;
}
