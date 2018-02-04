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

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#include "lsra.h"

//------------------------------------------------------------------------
// LocationInfoListNodePool::LocationInfoListNodePool:
//    Creates a pool of `LocationInfoListNode` values.
//
// Arguments:
//    compiler    - The compiler context.
//    preallocate - The number of nodes to preallocate.
//
LocationInfoListNodePool::LocationInfoListNodePool(Compiler* compiler, unsigned preallocate) : m_compiler(compiler)
{
    if (preallocate > 0)
    {
        size_t                preallocateSize = sizeof(LocationInfoListNode) * preallocate;
        LocationInfoListNode* preallocatedNodes =
            reinterpret_cast<LocationInfoListNode*>(compiler->compGetMem(preallocateSize, CMK_LSRA));

        LocationInfoListNode* head = preallocatedNodes;
        head->m_next               = nullptr;

        for (unsigned i = 1; i < preallocate; i++)
        {
            LocationInfoListNode* node = &preallocatedNodes[i];
            node->m_next               = head;
            head                       = node;
        }

        m_freeList = head;
    }
}

//------------------------------------------------------------------------
// LocationInfoListNodePool::GetNode: Fetches an unused node from the
//                                    pool.
//
// Arguments:
//    l -    - The `LsraLocation` for the `LocationInfo` value.
//    i      - The interval for the `LocationInfo` value.
//    t      - The IR node for the `LocationInfo` value
//    regIdx - The register index for the `LocationInfo` value.
//
// Returns:
//    A pooled or newly-allocated `LocationInfoListNode`, depending on the
//    contents of the pool.
LocationInfoListNode* LocationInfoListNodePool::GetNode(LsraLocation l, Interval* i, GenTree* t, unsigned regIdx)
{
    LocationInfoListNode* head = m_freeList;
    if (head == nullptr)
    {
        head = reinterpret_cast<LocationInfoListNode*>(m_compiler->compGetMem(sizeof(LocationInfoListNode)));
    }
    else
    {
        m_freeList = head->m_next;
    }

    head->loc      = l;
    head->interval = i;
    head->treeNode = t;
    head->m_next   = nullptr;

    return head;
}

//------------------------------------------------------------------------
// LocationInfoListNodePool::ReturnNodes: Returns a list of nodes to the node
//                                        pool and clears the given list.
//
// Arguments:
//    list - The list to return.
//
void LocationInfoListNodePool::ReturnNodes(LocationInfoList& list)
{
    assert(list.m_head != nullptr);
    assert(list.m_tail != nullptr);

    LocationInfoListNode* head = m_freeList;
    list.m_tail->m_next        = head;
    m_freeList                 = list.m_head;

    list.m_head = nullptr;
    list.m_tail = nullptr;
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
    bool         defRegConflict   = false;
    bool         useRegConflict   = false;

    // If the useRefPosition is a "delayRegFree", we can't change the registerAssignment
    // on it, or we will fail to ensure that the fixedReg is busy at the time the target
    // (of the node that uses this interval) is allocated.
    bool canChangeUseAssignment = !useRefPosition->isFixedRegRef || !useRefPosition->delayRegFree;

    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CONFLICT));
    if (!canChangeUseAssignment)
    {
        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_FIXED_DELAY_USE));
    }
    if (defRefPosition->isFixedRegRef)
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
    if (useRefPosition->isFixedRegRef)
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
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CASE2));
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
        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CASE3));
        defRefPosition->registerAssignment = useRegAssignment;
        return;
    }
    if (useRegRecord != nullptr && !defRegConflict && canChangeUseAssignment)
    {
        // This is case #4.
        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CASE4));
        useRefPosition->registerAssignment = defRegAssignment;
        return;
    }
    if (defRegRecord != nullptr && useRegRecord != nullptr)
    {
        // This is case #5.
        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CASE5));
        RegisterType regType = interval->registerType;
        assert((getRegisterType(interval, defRefPosition) == regType) &&
               (getRegisterType(interval, useRefPosition) == regType));
        regMaskTP candidates               = allRegs(regType);
        defRefPosition->registerAssignment = candidates;
        return;
    }
    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DEFUSE_CASE6));
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
        theInterval->updateRegisterPreferences(rp->registerAssignment);
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
//    created, causing a RefTypeFixedRef to be added at that location. This, however, results in
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

    newRP->setReg(getRegisterRecord(reg));
    newRP->registerAssignment = mask;

    newRP->setMultiRegIdx(0);
    newRP->setAllocateIfProfitable(false);

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
        // Insert a RefTypeFixedReg for any normal def or use (not ParamDef or BB)
        if (theRefType == RefTypeUse || theRefType == RefTypeDef)
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
    newRP->setAllocateIfProfitable(false);

    associateRefPosWithInterval(newRP);

    DBEXEC(VERBOSE, newRP->dump());
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
            // helpers have the same kill set: EDX.
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
// getKillSetForNode:   Return the registers killed by the given tree node.
//
// Arguments:
//    compiler   - the compiler context to use
//    tree       - the tree for which the kill set is needed.
//
// Return Value:    a register mask of the registers killed
//
regMaskTP LinearScan::getKillSetForNode(GenTree* tree)
{
    regMaskTP killMask = RBM_NONE;
    switch (tree->OperGet())
    {
#ifdef _TARGET_XARCH_
        case GT_MUL:
            // We use the 128-bit multiply when performing an overflow checking unsigned multiply
            //
            if (((tree->gtFlags & GTF_UNSIGNED) != 0) && tree->gtOverflowEx())
            {
                // Both RAX and RDX are killed by the operation
                killMask = RBM_RAX | RBM_RDX;
            }
            break;

        case GT_MULHI:
#if defined(_TARGET_X86_) && !defined(LEGACY_BACKEND)
        case GT_MUL_LONG:
#endif
            killMask = RBM_RAX | RBM_RDX;
            break;

        case GT_MOD:
        case GT_DIV:
        case GT_UMOD:
        case GT_UDIV:
            if (!varTypeIsFloating(tree->TypeGet()))
            {
                // Both RAX and RDX are killed by the operation
                killMask = RBM_RAX | RBM_RDX;
            }
            break;
#endif // _TARGET_XARCH_

        case GT_STORE_OBJ:
            if (tree->OperIsCopyBlkOp())
            {
                assert(tree->AsObj()->gtGcPtrCount != 0);
                killMask = compiler->compHelperCallKillSet(CORINFO_HELP_ASSIGN_BYREF);
                break;
            }
            __fallthrough;

        case GT_STORE_BLK:
        case GT_STORE_DYN_BLK:
        {
            GenTreeBlk* blkNode   = tree->AsBlk();
            bool        isCopyBlk = varTypeIsStruct(blkNode->Data());
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
        break;

        case GT_RETURNTRAP:
            killMask = compiler->compHelperCallKillSet(CORINFO_HELP_STOP_FOR_GC);
            break;
        case GT_CALL:
#ifdef _TARGET_X86_
            if (compiler->compFloatingPointUsed)
            {
                if (tree->TypeGet() == TYP_DOUBLE)
                {
                    needDoubleTmpForFPCall = true;
                }
                else if (tree->TypeGet() == TYP_FLOAT)
                {
                    needFloatTmpForFPCall = true;
                }
            }
#endif // _TARGET_X86_
#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
            if (tree->IsHelperCall())
            {
                GenTreeCall*    call     = tree->AsCall();
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
                if (tree->AsCall()->IsVirtualStub())
                {
                    killMask |= compiler->virtualStubParamInfo->GetRegMask();
                }
#else // !_TARGET_ARM_
            // Verify that the special virtual stub call registers are in the kill mask.
            // We don't just add them unconditionally to the killMask because for most architectures
            // they are already in the RBM_CALLEE_TRASH set,
            // and we don't want to introduce extra checks and calls in this hot function.
            assert(!tree->AsCall()->IsVirtualStub() || ((killMask & compiler->virtualStubParamInfo->GetRegMask()) ==
                                                        compiler->virtualStubParamInfo->GetRegMask()));
#endif
            }
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
            if (compiler->compIsProfilerHookNeeded())
            {
                killMask = compiler->compHelperCallKillSet(CORINFO_HELP_PROF_FCN_LEAVE);
            }
            break;

        case GT_PROF_HOOK:
            if (compiler->compIsProfilerHookNeeded())
            {
                killMask = compiler->compHelperCallKillSet(CORINFO_HELP_PROF_FCN_TAILCALL);
            }
            break;
#endif // PROFILING_SUPPORTED

        default:
            // for all other 'tree->OperGet()' kinds, leave 'killMask' = RBM_NONE
            break;
    }
    return killMask;
}

//------------------------------------------------------------------------
// buildKillPositionsForNode:
// Given some tree node add refpositions for all the registers this node kills
//
// Arguments:
//    tree       - the tree for which kill positions should be generated
//    currentLoc - the location at which the kills should be added
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

bool LinearScan::buildKillPositionsForNode(GenTree* tree, LsraLocation currentLoc)
{
    regMaskTP killMask   = getKillSetForNode(tree);
    bool      isCallKill = ((killMask == RBM_INT_CALLEE_TRASH) || (killMask == RBM_CALLEE_TRASH));
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
        // save regs on infrequently exectued paths.  However, it results in a large number of asmDiffs,
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
                Interval* interval = getIntervalForLocalVar(varIndex);
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

        if (compiler->killGCRefs(tree))
        {
            RefPosition* pos = newRefPosition((Interval*)nullptr, currentLoc, RefTypeKillGCRefs, tree,
                                              (allRegs(TYP_REF) & ~RBM_ARG_REGS));
        }
        return true;
    }

    return false;
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
    return newRefPosition(current, currentLoc, RefTypeDef, tree, regMask, 0);
}

//------------------------------------------------------------------------
// buildInternalRegisterDefsForNode - build Def positions for internal
// registers required for tree node.
//
// Arguments:
//   tree                  -   Gentree node that needs internal registers
//   temps                 -   in-out array which is populated with ref positions
//                             created for Def of internal registers
//
// Returns:
//   The total number of Def positions created for internal registers of tree no.
int LinearScan::buildInternalRegisterDefsForNode(GenTree* tree, TreeNodeInfo* info, RefPosition* temps[])
{
    int       count;
    int       internalIntCount = info->internalIntCount;
    regMaskTP internalCands    = info->getInternalCandidates(this);

    // If the number of internal integer registers required is the same as the number of candidate integer registers in
    // the candidate set, then they must be handled as fixed registers.
    // (E.g. for the integer registers that floating point arguments must be copied into for a varargs call.)
    bool      fixedRegs             = false;
    regMaskTP internalIntCandidates = (internalCands & allRegs(TYP_INT));
    if (((int)genCountBits(internalIntCandidates)) == internalIntCount)
    {
        fixedRegs = true;
    }

    for (count = 0; count < internalIntCount; count++)
    {
        regMaskTP internalIntCands = (internalCands & allRegs(TYP_INT));
        if (fixedRegs)
        {
            internalIntCands = genFindLowestBit(internalIntCands);
            internalCands &= ~internalIntCands;
        }
        temps[count] = defineNewInternalTemp(tree, IntRegisterType, internalIntCands);
    }

    int internalFloatCount = info->internalFloatCount;
    for (int i = 0; i < internalFloatCount; i++)
    {
        regMaskTP internalFPCands = (internalCands & internalFloatRegCandidates());
        temps[count++]            = defineNewInternalTemp(tree, FloatRegisterType, internalFPCands);
    }

    assert(count < MaxInternalRegisters);
    assert(count == (internalIntCount + internalFloatCount));
    return count;
}

//------------------------------------------------------------------------
// buildInternalRegisterUsesForNode - adds Use positions for internal
// registers required for tree node.
//
// Arguments:
//   tree                  -   Gentree node that needs internal registers
//   defs                  -   int array containing Def positions of internal
//                             registers.
//   total                 -   Total number of Def positions in 'defs' array.
//
// Returns:
//   Void.
void LinearScan::buildInternalRegisterUsesForNode(GenTree* tree, TreeNodeInfo* info, RefPosition* defs[], int total)
{
    assert(total < MaxInternalRegisters);

    // defs[] has been populated by buildInternalRegisterDefsForNode
    // now just add uses to the defs previously added.
    for (int i = 0; i < total; i++)
    {
        RefPosition* prevRefPosition = defs[i];
        assert(prevRefPosition != nullptr);
        regMaskTP mask = prevRefPosition->registerAssignment;
        if (prevRefPosition->isPhysRegRef)
        {
            newRefPosition(defs[i]->getReg()->regNum, currentLoc, RefTypeUse, tree, mask);
        }
        else
        {
            RefPosition* newest = newRefPosition(defs[i]->getInterval(), currentLoc, RefTypeUse, tree, mask, 0);

            if (info->isInternalRegDelayFree)
            {
                newest->delayRegFree = true;
            }
        }
    }
}

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
//------------------------------------------------------------------------
// buildUpperVectorSaveRefPositions - Create special RefPositions for saving
//                                    the upper half of a set of large vector.
//
// Arguments:
//    tree       - The current node being handled
//    currentLoc - The location of the current node
//
// Return Value: Returns the set of lclVars that are killed by this node, and therefore
//               required RefTypeUpperVectorSaveDef RefPositions.
//
// Notes: The returned set is used by buildUpperVectorRestoreRefPositions.
//
VARSET_VALRET_TP
LinearScan::buildUpperVectorSaveRefPositions(GenTree* tree, LsraLocation currentLoc)
{
    assert(enregisterLocalVars);
    VARSET_TP liveLargeVectors(VarSetOps::MakeEmpty(compiler));
    regMaskTP fpCalleeKillSet = RBM_NONE;
    if (!VarSetOps::IsEmpty(compiler, largeVectorVars))
    {
        // We actually need to find any calls that kill the upper-half of the callee-save vector registers.
        // But we will use as a proxy any node that kills floating point registers.
        // (Note that some calls are masquerading as other nodes at this point so we can't just check for calls.)
        fpCalleeKillSet = getKillSetForNode(tree);
        if ((fpCalleeKillSet & RBM_FLT_CALLEE_TRASH) != RBM_NONE)
        {
            VarSetOps::AssignNoCopy(compiler, liveLargeVectors,
                                    VarSetOps::Intersection(compiler, currentLiveVars, largeVectorVars));
            VarSetOps::Iter iter(compiler, liveLargeVectors);
            unsigned        varIndex = 0;
            while (iter.NextElem(&varIndex))
            {
                Interval* varInterval    = getIntervalForLocalVar(varIndex);
                Interval* tempInterval   = newInterval(varInterval->registerType);
                tempInterval->isInternal = true;
                RefPosition* pos =
                    newRefPosition(tempInterval, currentLoc, RefTypeUpperVectorSaveDef, tree, RBM_FLT_CALLEE_SAVED);
                // We are going to save the existing relatedInterval of varInterval on tempInterval, so that we can set
                // the tempInterval as the relatedInterval of varInterval, so that we can build the corresponding
                // RefTypeUpperVectorSaveUse RefPosition.  We will then restore the relatedInterval onto varInterval,
                // and set varInterval as the relatedInterval of tempInterval.
                tempInterval->relatedInterval = varInterval->relatedInterval;
                varInterval->relatedInterval  = tempInterval;
            }
        }
    }
    return liveLargeVectors;
}

// buildUpperVectorRestoreRefPositions - Create special RefPositions for restoring
//                                       the upper half of a set of large vectors.
//
// Arguments:
//    tree       - The current node being handled
//    currentLoc - The location of the current node
//    liveLargeVectors - The set of lclVars needing restores (returned by buildUpperVectorSaveRefPositions)
//
void LinearScan::buildUpperVectorRestoreRefPositions(GenTree*         tree,
                                                     LsraLocation     currentLoc,
                                                     VARSET_VALARG_TP liveLargeVectors)
{
    assert(enregisterLocalVars);
    if (!VarSetOps::IsEmpty(compiler, liveLargeVectors))
    {
        VarSetOps::Iter iter(compiler, liveLargeVectors);
        unsigned        varIndex = 0;
        while (iter.NextElem(&varIndex))
        {
            Interval* varInterval  = getIntervalForLocalVar(varIndex);
            Interval* tempInterval = varInterval->relatedInterval;
            assert(tempInterval->isInternal == true);
            RefPosition* pos =
                newRefPosition(tempInterval, currentLoc, RefTypeUpperVectorSaveUse, tree, RBM_FLT_CALLEE_SAVED);
            // Restore the relatedInterval onto varInterval, and set varInterval as the relatedInterval
            // of tempInterval.
            varInterval->relatedInterval  = tempInterval->relatedInterval;
            tempInterval->relatedInterval = varInterval;
        }
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
#ifdef _TARGET_ARM_
    assert(!isRegPairType(tree->TypeGet()));
#endif // _TARGET_ARM_

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

    // If the node produces a value that will be consumed by a parent node, its TreeNodeInfo will
    // be allocated in the LocationInfoListNode. Otherwise, we'll just use a local value that will
    // be thrown away when we're done.
    LocationInfoListNode* locationInfo = nullptr;
    TreeNodeInfo          tempInfo;
    TreeNodeInfo*         info    = nullptr;
    int                   consume = 0;
    int                   produce = 0;
    if (!tree->isContained())
    {
        if (tree->IsValue())
        {
            locationInfo    = listNodePool.GetNode(currentLoc, nullptr, tree);
            currentNodeInfo = &locationInfo->info;
        }
        else
        {
            currentNodeInfo = &tempInfo;
        }
        info = currentNodeInfo;
        info->Initialize(this, tree);
        BuildNode(tree);
        assert(info->IsValid(this));
        consume = info->srcCount;
        produce = info->dstCount;
#ifdef DEBUG
        if (VERBOSE)
        {
            printf("    +");
            info->dump(this);
            tree->dumpLIRFlags();
            printf("\n");
        }
#endif // DEBUG
    }

#ifdef DEBUG
    if (VERBOSE)
    {
        if (tree->isContained())
        {
            JITDUMP("Contained\n");
        }
        else if (tree->OperIs(GT_LCL_VAR, GT_LCL_FLD) && tree->IsUnusedValue())
        {
            JITDUMP("Unused\n");
        }
        else
        {
            JITDUMP("  consume=%d produce=%d\n", consume, produce);
        }
    }
#endif // DEBUG

    assert(((consume == 0) && (produce == 0)) || (ComputeAvailableSrcCount(tree) == consume));

    if (tree->OperIs(GT_LCL_VAR, GT_LCL_FLD))
    {
        LclVarDsc* const varDsc = &compiler->lvaTable[tree->AsLclVarCommon()->gtLclNum];
        if (isCandidateVar(varDsc))
        {
            assert(consume == 0);

            // We handle tracked variables differently from non-tracked ones.  If it is tracked,
            // we simply add a use or def of the tracked variable.  Otherwise, for a use we need
            // to actually add the appropriate references for loading or storing the variable.
            //
            // It won't actually get used or defined until the appropriate ancestor tree node
            // is processed, unless this is marked "isLocalDefUse" because it is a stack-based argument
            // to a call

            assert(varDsc->lvTracked);
            unsigned varIndex = varDsc->lvVarIndex;

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
            if ((tree->gtFlags & GTF_VAR_DEATH) != 0)
            {
                VarSetOps::RemoveElemD(compiler, currentLiveVars, varIndex);
            }

            if (!tree->IsUnusedValue() && !tree->isContained())
            {
                assert(produce != 0);

                locationInfo->interval = getIntervalForLocalVar(varIndex);
                defList.Append(locationInfo);
            }
            return;
        }
    }
    if (tree->isContained())
    {
        return;
    }

    // Handle the case of local variable assignment
    Interval* varDefInterval = nullptr;

    GenTree* defNode = tree;

    // noAdd means the node creates a def but for purposes of map
    // management do not add it because data is not flowing up the
    // tree

    bool         noAdd   = info->isLocalDefUse;
    RefPosition* prevPos = nullptr;

    bool isSpecialPutArg = false;

    assert(!tree->OperIsAssignment());
    if (tree->OperIsLocalStore())
    {
        GenTreeLclVarCommon* const store = tree->AsLclVarCommon();
        assert((consume > 1) || (regType(store->gtOp1->TypeGet()) == regType(store->TypeGet())));

        LclVarDsc* varDsc = &compiler->lvaTable[store->gtLclNum];
        if (isCandidateVar(varDsc))
        {
            // We always push the tracked lclVar intervals
            assert(varDsc->lvTracked);
            unsigned varIndex = varDsc->lvVarIndex;
            varDefInterval    = getIntervalForLocalVar(varIndex);
            assert((store->gtFlags & GTF_VAR_DEF) != 0);
            defNode = tree;
            if (produce == 0)
            {
                produce = 1;
                noAdd   = true;
            }

            assert(consume <= MAX_RET_REG_COUNT);
            if (consume == 1)
            {
                // Get the location info for the register defined by the first operand.
                LocationInfoListNode& operandInfo = *(useList.Begin());
                assert(operandInfo.treeNode == tree->gtGetOp1());

                Interval* srcInterval = operandInfo.interval;
                if (srcInterval->relatedInterval == nullptr)
                {
                    // Preference the source to the dest, unless this is a non-last-use localVar.
                    // Note that the last-use info is not correct, but it is a better approximation than preferencing
                    // the source to the dest, if the source's lifetime extends beyond the dest.
                    if (!srcInterval->isLocalVar || (operandInfo.treeNode->gtFlags & GTF_VAR_DEATH) != 0)
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

            if ((tree->gtFlags & GTF_VAR_DEATH) == 0)
            {
                VarSetOps::AddElemD(compiler, currentLiveVars, varIndex);
            }
        }
        else if (store->gtOp1->OperIs(GT_BITCAST))
        {
            store->gtType = store->gtOp1->gtType = store->gtOp1->AsUnOp()->gtOp1->TypeGet();

            // Get the location info for the register defined by the first operand.
            LocationInfoListNode& operandInfo = *(useList.Begin());
            assert(operandInfo.treeNode == tree->gtGetOp1());

            Interval* srcInterval     = operandInfo.interval;
            srcInterval->registerType = regType(store->TypeGet());

            RefPosition* srcDefPosition = srcInterval->firstRefPosition;
            assert(srcDefPosition != nullptr);
            assert(srcDefPosition->refType == RefTypeDef);
            assert(srcDefPosition->treeNode == store->gtOp1);

            srcDefPosition->registerAssignment = allRegs(store->TypeGet());
            operandInfo.info.setSrcCandidates(this, allRegs(store->TypeGet()));
        }
    }
    else if (noAdd && produce == 0)
    {
        // Dead nodes may remain after tree rationalization, decomposition or lowering.
        // They should be marked as UnusedValue.
        // TODO-Cleanup: Identify and remove these dead nodes prior to register allocation.
        assert(!noAdd || (produce != 0));
    }

    Interval* prefSrcInterval = nullptr;

    // If this is a binary operator that will be encoded with 2 operand fields
    // (i.e. the target is read-modify-write), preference the dst to op1.

    bool hasDelayFreeSrc = info->hasDelayFreeSrc;

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
    const bool supportsSpecialPutArg = getStressLimitRegs() != LSRA_LIMIT_CALLER;
#else
    const bool supportsSpecialPutArg = true;
#endif

    if (supportsSpecialPutArg)
    {
        if ((tree->OperGet() == GT_PUTARG_REG) && isCandidateLocalRef(tree->gtGetOp1()) &&
            (tree->gtGetOp1()->gtFlags & GTF_VAR_DEATH) == 0)
        {
            // This is the case for a "pass-through" copy of a lclVar.  In the case where it is a non-last-use,
            // we don't want the def of the copy to kill the lclVar register, if it is assigned the same register
            // (which is actually what we hope will happen).
            JITDUMP("Setting putarg_reg as a pass-through of a non-last use lclVar\n");

            // Get the register information for the first operand of the node.
            LocationInfoListNode* operandDef = useList.Begin();
            assert(operandDef->treeNode == tree->gtGetOp1());

            // Preference the destination to the interval of the first register defined by the first operand.
            Interval* srcInterval = operandDef->interval;
            assert(srcInterval->isLocalVar);
            prefSrcInterval = srcInterval;
            isSpecialPutArg = true;
            INDEBUG(specialPutArgCount++);
        }
        else if (tree->IsCall())
        {
            INDEBUG(specialPutArgCount = 0);
        }
    }

    RefPosition* internalRefs[MaxInternalRegisters];

#ifdef DEBUG
    // If we are constraining the registers for allocation, we will modify all the RefPositions
    // we've built for this node after we've created them. In order to do that, we'll remember
    // the last RefPosition prior to those created for this node.
    RefPositionIterator refPositionMark = refPositions.backPosition();
#endif // DEBUG

    // Make intervals for all the 'internal' register requirements for this node,
    // where internal means additional registers required temporarily.
    // Create a RefTypeDef RefPosition for each such interval.
    int internalCount = buildInternalRegisterDefsForNode(tree, info, internalRefs);

    // Make use RefPositions for all used values.
    int consumed = 0;
    for (LocationInfoListNode *listNode = useList.Begin(), *end = useList.End(); listNode != end;
         listNode = listNode->Next())
    {
        LocationInfo& locInfo = *static_cast<LocationInfo*>(listNode);

        // For tree temps, a use is always a last use and the end of the range;
        // this is set by default in newRefPosition
        GenTree* const useNode = locInfo.treeNode;
        assert(useNode != nullptr);

        Interval*     srcInterval = locInfo.interval;
        TreeNodeInfo& useNodeInfo = locInfo.info;
        if (useNodeInfo.isTgtPref)
        {
            prefSrcInterval = srcInterval;
        }

        const bool delayRegFree = (hasDelayFreeSrc && useNodeInfo.isDelayFree);

        regMaskTP candidates = useNodeInfo.getSrcCandidates(this);
#ifdef _TARGET_ARM_
        regMaskTP allCandidates = candidates;

        if (useNode->OperIsPutArgSplit() || useNode->OperIsMultiRegOp())
        {
            // get i-th candidate, set bits in useCandidates must be in sequential order.
            candidates = genFindLowestReg(allCandidates);
            allCandidates &= ~candidates;
        }
#endif // _TARGET_ARM_

        assert((candidates & allRegs(srcInterval->registerType)) != 0);

        // For non-localVar uses we record nothing, as nothing needs to be written back to the tree.
        GenTree* const refPosNode = srcInterval->isLocalVar ? useNode : nullptr;
        RefPosition*   pos        = newRefPosition(srcInterval, currentLoc, RefTypeUse, refPosNode, candidates, 0);
        if (delayRegFree)
        {
            pos->delayRegFree = true;
        }

        if (useNode->IsRegOptional())
        {
            pos->setAllocateIfProfitable(true);
        }
        consumed++;

        // Create additional use RefPositions for multi-reg nodes.
        for (int idx = 1; idx < locInfo.info.dstCount; idx++)
        {
            noway_assert(srcInterval->relatedInterval != nullptr);
            srcInterval = srcInterval->relatedInterval;
#ifdef _TARGET_ARM_
            if (useNode->OperIsPutArgSplit() ||
                (compiler->opts.compUseSoftFP && (useNode->OperIsPutArgReg() || useNode->OperGet() == GT_BITCAST)))
            {
                // get first candidate, set bits in useCandidates must be in sequential order.
                candidates = genFindLowestReg(allCandidates);
                allCandidates &= ~candidates;
            }
#endif // _TARGET_ARM_
            RefPosition* pos = newRefPosition(srcInterval, currentLoc, RefTypeUse, refPosNode, candidates, idx);
            consumed++;
        }
    }

    assert(consumed == consume);
    if (consume != 0)
    {
        listNodePool.ReturnNodes(useList);
    }

    buildInternalRegisterUsesForNode(tree, info, internalRefs, internalCount);

    RegisterType registerType  = getDefType(tree);
    regMaskTP    candidates    = info->getDstCandidates(this);
    regMaskTP    useCandidates = info->getSrcCandidates(this);

#ifdef DEBUG
    if (VERBOSE && produce)
    {
        printf("Def candidates ");
        dumpRegMask(candidates);
        printf(", Use candidates ");
        dumpRegMask(useCandidates);
        printf("\n");
    }
#endif // DEBUG

#if defined(_TARGET_AMD64_)
    // Multi-reg call node is the only node that could produce multi-reg value
    assert(produce <= 1 || (tree->IsMultiRegCall() && produce == MAX_RET_REG_COUNT));
#endif // _TARGET_xxx_

    // Add kill positions before adding def positions
    buildKillPositionsForNode(tree, currentLoc + 1);

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    VARSET_TP liveLargeVectors(VarSetOps::UninitVal());
    if (enregisterLocalVars && (RBM_FLT_CALLEE_SAVED != RBM_NONE))
    {
        // Build RefPositions for saving any live large vectors.
        // This must be done after the kills, so that we know which large vectors are still live.
        VarSetOps::AssignNoCopy(compiler, liveLargeVectors, buildUpperVectorSaveRefPositions(tree, currentLoc + 1));
    }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

    ReturnTypeDesc* retTypeDesc    = nullptr;
    bool            isMultiRegCall = tree->IsMultiRegCall();
    if (isMultiRegCall)
    {
        retTypeDesc = tree->AsCall()->GetReturnTypeDesc();
        assert((int)genCountBits(candidates) == produce);
        assert(candidates == retTypeDesc->GetABIReturnRegs());
    }

    // push defs
    LocationInfoList locationInfoList;
    LsraLocation     defLocation = currentLoc + 1;
    Interval*        interval    = varDefInterval;
    // For nodes that define multiple registers, subsequent intervals will be linked using the 'relatedInterval' field.
    // Keep track of the previous interval allocated, for that purpose.
    Interval* prevInterval = nullptr;
    for (int i = 0; i < produce; i++)
    {
        regMaskTP currCandidates = candidates;

        // In case of multi-reg call node, registerType is given by
        // the type of ith position return register.
        if (isMultiRegCall)
        {
            registerType   = retTypeDesc->GetReturnRegType((unsigned)i);
            currCandidates = genRegMask(retTypeDesc->GetABIReturnReg(i));
            useCandidates  = allRegs(registerType);
        }

#ifdef _TARGET_ARM_
        // If oper is GT_PUTARG_REG, set bits in useCandidates must be in sequential order.
        if (tree->OperIsPutArgSplit() || tree->OperIsMultiRegOp())
        {
            // get i-th candidate
            currCandidates = genFindLowestReg(candidates);
            candidates &= ~currCandidates;
        }
#endif // _TARGET_ARM_

        if (interval == nullptr)
        {
            // Make a new interval
            interval = newInterval(registerType);
            if (hasDelayFreeSrc || info->isInternalRegDelayFree)
            {
                interval->hasInterferingUses = true;
            }
            else if (tree->OperIsConst())
            {
                assert(!tree->IsReuseRegVal());
                interval->isConstant = true;
            }

            if ((currCandidates & useCandidates) != RBM_NONE)
            {
                interval->updateRegisterPreferences(currCandidates & useCandidates);
            }

            if (isSpecialPutArg)
            {
                interval->isSpecialPutArg = true;
            }
        }
        else
        {
            assert(registerTypesEquivalent(interval->registerType, registerType));
        }

        if (prefSrcInterval != nullptr)
        {
            interval->assignRelatedIntervalIfUnassigned(prefSrcInterval);
        }

        // for assignments, we want to create a refposition for the def
        // but not push it
        if (!noAdd)
        {
            if (i == 0)
            {
                locationInfo->interval = interval;
                prevInterval           = interval;
                defList.Append(locationInfo);
            }
            else
            {
                // This is the 2nd or subsequent register defined by a multi-reg node.
                // Connect them using 'relatedInterval'.
                noway_assert(prevInterval != nullptr);
                prevInterval->relatedInterval = interval;
                prevInterval                  = interval;
                prevInterval->isMultiReg      = true;
                interval->isMultiReg          = true;
            }
        }

        RefPosition* pos = newRefPosition(interval, defLocation, RefTypeDef, defNode, currCandidates, (unsigned)i);
        if (info->isLocalDefUse)
        {
            // This must be an unused value, OR it is a special node for which we allocate
            // a target register even though it produces no value.
            assert(defNode->IsUnusedValue() || (defNode->gtOper == GT_LOCKADD));
            pos->isLocalDefUse = true;
            pos->lastUse       = true;
        }
        interval->updateRegisterPreferences(currCandidates);
        interval->updateRegisterPreferences(useCandidates);
        interval = nullptr;
    }

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    // SaveDef position must be at the same location as Def position of call node.
    if (enregisterLocalVars)
    {
        buildUpperVectorRestoreRefPositions(tree, defLocation, liveLargeVectors);
    }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

#ifdef DEBUG
    // If we are constraining registers, modify all the RefPositions we've just built to specify the
    // minimum reg count required.
    if ((getStressLimitRegs() != LSRA_LIMIT_NONE) || (getSelectionHeuristics() != LSRA_SELECT_DEFAULT))
    {
        // The number of registers required for a tree node is the sum of
        //   consume + produce + internalCount + specialPutArgCount.
        // This is the minimum set of registers that needs to be ensured in the candidate set of ref positions created.
        //
        unsigned minRegCount =
            consume + produce + info->internalIntCount + info->internalFloatCount + specialPutArgCount;

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

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
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

#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

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
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    // For System V AMD64 calls the argDsc can have 2 registers (for structs.)
    // Handle them here.
    if (varTypeIsStruct(argDsc))
    {
        unixAmd64UpdateRegStateForArg(argDsc);
    }
    else
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    {
        RegState* intRegState   = &compiler->codeGen->intRegState;
        RegState* floatRegState = &compiler->codeGen->floatRegState;
        // In the case of AMD64 we'll still use the floating point registers
        // to model the register usage for argument on vararg calls, so
        // we will ignore the varargs condition to determine whether we use
        // XMM registers or not for setting up the call.
        bool isFloat = (isFloatRegType(argDsc->lvType)
#ifndef _TARGET_AMD64_
                        && !compiler->info.compIsVarArgs
#endif
                        && !compiler->opts.compUseSoftFP);

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
            printf("BB%02u use def in out\n", block->bbNum);
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
        if (!compiler->compJmpOpUsed && argDsc->lvRefCnt == 0 && !compiler->opts.compDbgCode)
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
                }
            }
        }
        else
        {
            // We can overwrite the register (i.e. codegen saves it on entry)
            assert(argDsc->lvRefCnt == 0 || !argDsc->lvIsRegArg || argDsc->lvDoNotEnregister ||
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
        JITDUMP("\nNEW BLOCK BB%02u\n", block->bbNum);

        bool predBlockIsAllocated = false;
        predBlock                 = findPredBlockForLiveIn(block, prevBlock DEBUGARG(&predBlockIsAllocated));
        if (predBlock)
        {
            JITDUMP("\n\nSetting BB%02u as the predecessor for determining incoming variable registers of BB%02u\n",
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

        // Note: the visited set is cleared in LinearScan::doLinearScan()
        markBlockVisited(block);
        assert(defList.IsEmpty());

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
                    JITDUMP(" V%02u", varNum);
                }
                JITDUMP("\n");
            }

            // Clear the "last use" flag on any vars that are live-out from this block.
            {
                VarSetOps::Iter iter(compiler, block->bbLiveOut);
                unsigned        varIndex = 0;
                while (iter.NextElem(&varIndex))
                {
                    unsigned         varNum = compiler->lvaTrackedToVarNum[varIndex];
                    LclVarDsc* const varDsc = &compiler->lvaTable[varNum];
                    if (isCandidateVar(varDsc))
                    {
                        RefPosition* const lastRP = getIntervalForLocalVar(varIndex)->lastRefPosition;
                        if ((lastRP != nullptr) && (lastRP->bbNum == block->bbNum))
                        {
                            lastRP->lastUse = false;
                        }
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

//------------------------------------------------------------------------
// GetIndirInfo: Get the source registers for an indirection that might be contained.
//
// Arguments:
//    node      - The node of interest
//
// Return Value:
//    The number of source registers used by the *parent* of this node.
//
// Notes:
//    Adds the defining node for each register to the useList.
//
int LinearScan::GetIndirInfo(GenTreeIndir* indirTree)
{
    GenTree* const addr = indirTree->gtOp1;
    if (!addr->isContained())
    {
        appendLocationInfoToList(addr);
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
        appendLocationInfoToList(addrMode->Base());
        srcCount++;
    }
    if ((addrMode->Index() != nullptr) && !addrMode->Index()->isContained())
    {
        appendLocationInfoToList(addrMode->Index());
        srcCount++;
    }
    return srcCount;
}

//------------------------------------------------------------------------
// GetOperandInfo: Get the source registers for an operand that might be contained.
//
// Arguments:
//    node      - The node of interest
//    useList   - The list of uses for the node that we're currently processing
//
// Return Value:
//    The number of source registers used by the *parent* of this node.
//
// Notes:
//    Adds the defining node for each register to the given useList.
//
int LinearScan::GetOperandInfo(GenTree* node)
{
    if (!node->isContained())
    {
        appendLocationInfoToList(node);
        return 1;
    }

#if !defined(_TARGET_64BIT_)
    if (node->OperIs(GT_LONG))
    {
        return appendBinaryLocationInfoToList(node->AsOp());
    }
#endif // !defined(_TARGET_64BIT_)
    if (node->OperIsIndir())
    {
        const unsigned srcCount = GetIndirInfo(node->AsIndir());
        return srcCount;
    }
    if (node->OperIsHWIntrinsic())
    {
        appendLocationInfoToList(node->gtGetOp1());
        return 1;
    }

    return 0;
}

//------------------------------------------------------------------------
// GetOperandInfo: Get the source registers for an operand that might be contained.
//
// Arguments:
//    node      - The node of interest
//    useList   - The list of uses for the node that we're currently processing
//
// Return Value:
//    The number of source registers used by the *parent* of this node.
//
// Notes:
//    Adds the defining node for each register to the useList.
//
int LinearScan::GetOperandInfo(GenTree* node, LocationInfoListNode** pFirstInfo)
{
    LocationInfoListNode* prevLast = useList.Last();
    int                   srcCount = GetOperandInfo(node);
    if (prevLast == nullptr)
    {
        *pFirstInfo = useList.Begin();
    }
    else
    {
        *pFirstInfo = prevLast->Next();
    }
    return srcCount;
}

void TreeNodeInfo::Initialize(LinearScan* lsra, GenTree* node)
{
    _dstCount           = 0;
    _srcCount           = 0;
    _internalIntCount   = 0;
    _internalFloatCount = 0;

    isLocalDefUse          = false;
    isDelayFree            = false;
    hasDelayFreeSrc        = false;
    isTgtPref              = false;
    isInternalRegDelayFree = false;

    regMaskTP dstCandidates;

    // if there is a reg indicated on the tree node, use that for dstCandidates
    // the exception is the NOP, which sometimes show up around late args.
    // TODO-Cleanup: get rid of those NOPs.
    if (node->gtRegNum == REG_STK)
    {
        dstCandidates = RBM_NONE;
    }
    else if (node->gtRegNum == REG_NA || node->gtOper == GT_NOP)
    {
#ifdef ARM_SOFTFP
        if (node->OperGet() == GT_PUTARG_REG)
        {
            dstCandidates = lsra->allRegs(TYP_INT);
        }
        else
#endif
        {
            dstCandidates = lsra->allRegs(node->TypeGet());
        }
    }
    else
    {
        dstCandidates = genRegMask(node->gtRegNum);
    }

    setDstCandidates(lsra, dstCandidates);
    srcCandsIndex = dstCandsIndex;

    setInternalCandidates(lsra, lsra->allRegs(TYP_INT));

#ifdef DEBUG
    isInitialized = true;
#endif

    assert(IsValid(lsra));
}

//------------------------------------------------------------------------
// getSrcCandidates: Get the source candidates (candidates for the consumer
//                   of the node) from the TreeNodeInfo
//
// Arguments:
//    lsra - the LinearScan object
//
// Return Value:
//    The set of registers (as a register mask) that are candidates for the
//    consumer of the node
//
// Notes:
//    The LinearScan object maintains the mapping from the indices kept in the
//    TreeNodeInfo to the actual register masks.
//
regMaskTP TreeNodeInfo::getSrcCandidates(LinearScan* lsra)
{
    return lsra->GetRegMaskForIndex(srcCandsIndex);
}

//------------------------------------------------------------------------
// setSrcCandidates: Set the source candidates (candidates for the consumer
//                   of the node) on the TreeNodeInfo
//
// Arguments:
//    lsra - the LinearScan object
//
// Notes: see getSrcCandidates
//
void TreeNodeInfo::setSrcCandidates(LinearScan* lsra, regMaskTP mask)
{
    LinearScan::RegMaskIndex i = lsra->GetIndexForRegMask(mask);
    assert(FitsIn<unsigned char>(i));
    srcCandsIndex = (unsigned char)i;
}

//------------------------------------------------------------------------
// getDstCandidates: Get the dest candidates (candidates for the definition
//                   of the node) from the TreeNodeInfo
//
// Arguments:
//    lsra - the LinearScan object
//
// Return Value:
//    The set of registers (as a register mask) that are candidates for the
//    node itself
//
// Notes: see getSrcCandidates
//
regMaskTP TreeNodeInfo::getDstCandidates(LinearScan* lsra)
{
    return lsra->GetRegMaskForIndex(dstCandsIndex);
}

//------------------------------------------------------------------------
// setDstCandidates: Set the dest candidates (candidates for the definition
//                   of the node) on the TreeNodeInfo
//
// Arguments:
//    lsra - the LinearScan object
//
// Notes: see getSrcCandidates
//
void TreeNodeInfo::setDstCandidates(LinearScan* lsra, regMaskTP mask)
{
    LinearScan::RegMaskIndex i = lsra->GetIndexForRegMask(mask);
    assert(FitsIn<unsigned char>(i));
    dstCandsIndex = (unsigned char)i;
}

//------------------------------------------------------------------------
// getInternalCandidates: Get the internal candidates (candidates for the internal
//                        temporary registers used by a node) from the TreeNodeInfo
//
// Arguments:
//    lsra - the LinearScan object
//
// Return Value:
//    The set of registers (as a register mask) that are candidates for the
//    internal temporary registers.
//
// Notes: see getSrcCandidates
//
regMaskTP TreeNodeInfo::getInternalCandidates(LinearScan* lsra)
{
    return lsra->GetRegMaskForIndex(internalCandsIndex);
}

//------------------------------------------------------------------------
// getInternalCandidates: Set the internal candidates (candidates for the internal
//                        temporary registers used by a node) on the TreeNodeInfo
//
// Arguments:
//    lsra - the LinearScan object
//
// Notes: see getSrcCandidates
//
void TreeNodeInfo::setInternalCandidates(LinearScan* lsra, regMaskTP mask)
{
    LinearScan::RegMaskIndex i = lsra->GetIndexForRegMask(mask);
    assert(FitsIn<unsigned char>(i));
    internalCandsIndex = (unsigned char)i;
}

//------------------------------------------------------------------------
// addInternalCandidates: Add internal candidates (candidates for the internal
//                        temporary registers used by a node) on the TreeNodeInfo
//
// Arguments:
//    lsra - the LinearScan object
//
// Notes: see getSrcCandidates
//
void TreeNodeInfo::addInternalCandidates(LinearScan* lsra, regMaskTP mask)
{
    LinearScan::RegMaskIndex i = lsra->GetIndexForRegMask(lsra->GetRegMaskForIndex(internalCandsIndex) | mask);
    assert(FitsIn<unsigned char>(i));
    internalCandsIndex = (unsigned char)i;
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
void LinearScan::BuildStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    TreeNodeInfo* info = currentNodeInfo;
    GenTree*      op1  = storeLoc->gtGetOp1();

    assert(info->dstCount == 0);

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
        info->srcCount              = regCount;

        // Call node srcCandidates = Bitwise-OR(allregs(GetReturnRegType(i))) for all i=0..RetRegCount-1
        regMaskTP             srcCandidates = allMultiRegCallNodeRegs(call);
        LocationInfoListNode* locInfo       = getLocationInfo(op1);
        locInfo->info.setSrcCandidates(this, srcCandidates);
        useList.Append(locInfo);
    }
#ifndef _TARGET_64BIT_
    else if (varTypeIsLong(op1))
    {
        if (op1->OperIs(GT_MUL_LONG))
        {
#ifdef _TARGET_X86_
            // This is actually a bug. A GT_MUL_LONG produces two registers, but is modeled as only producing
            // eax (and killing edx). This only works because it always occurs as var = GT_MUL_LONG (ensured by
            // DecomposeMul), and therefore edx won't be reused before the store.
            // TODO-X86-Cleanup: GT_MUL_LONG should be a multireg node on x86, just as on ARM.
            info->srcCount = 1;
#else
            info->srcCount         = 2;
#endif
            appendLocationInfoToList(op1);
        }
        else
        {
            assert(op1->OperIs(GT_LONG));
            assert(op1->isContained() && !op1->gtOp.gtOp1->isContained() && !op1->gtOp.gtOp2->isContained());
            info->srcCount = appendBinaryLocationInfoToList(op1->AsOp());
            assert(info->srcCount == 2);
        }
    }
#endif // !_TARGET_64BIT_
    else if (op1->isContained())
    {
        info->srcCount = 0;
    }
    else
    {
        info->srcCount = 1;
        appendLocationInfoToList(op1);
    }

#ifdef FEATURE_SIMD
    if (varTypeIsSIMD(storeLoc))
    {
        if (!op1->isContained() && (storeLoc->TypeGet() == TYP_SIMD12))
        {
// Need an additional register to extract upper 4 bytes of Vector3.
#ifdef _TARGET_XARCH_
            info->internalFloatCount = 1;
            info->setInternalCandidates(this, allSIMDRegs());
#elif defined(_TARGET_ARM64_)
            info->internalIntCount = 1;
#else
#error "Unknown target architecture for STORE_LCL_VAR of SIMD12"
#endif
        }
    }
#endif // FEATURE_SIMD
}

//------------------------------------------------------------------------
// BuildSimple: Sets the srcCount for all the trees
// without special handling based on the tree node type.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildSimple(GenTree* tree)
{
    TreeNodeInfo* info = currentNodeInfo;
    unsigned      kind = tree->OperKind();
    assert(info->dstCount == (tree->IsValue() ? 1 : 0));
    if (kind & (GTK_CONST | GTK_LEAF))
    {
        info->srcCount = 0;
    }
    else if (kind & (GTK_SMPOP))
    {
        info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
    }
    else
    {
        unreached();
    }
}

//------------------------------------------------------------------------
// BuildReturn: Set the NodeInfo for a GT_RETURN.
//
// Arguments:
//    tree - The node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildReturn(GenTree* tree)
{
    TreeNodeInfo* info = currentNodeInfo;
    assert(info->dstCount == 0);
    GenTree* op1 = tree->gtGetOp1();

#if !defined(_TARGET_64BIT_)
    if (tree->TypeGet() == TYP_LONG)
    {
        assert((op1->OperGet() == GT_LONG) && op1->isContained());
        GenTree* loVal                  = op1->gtGetOp1();
        GenTree* hiVal                  = op1->gtGetOp2();
        info->srcCount                  = 2;
        LocationInfoListNode* loValInfo = getLocationInfo(loVal);
        LocationInfoListNode* hiValInfo = getLocationInfo(hiVal);
        loValInfo->info.setSrcCandidates(this, RBM_LNGRET_LO);
        hiValInfo->info.setSrcCandidates(this, RBM_LNGRET_HI);
        useList.Append(loValInfo);
        useList.Append(hiValInfo);
    }
    else
#endif // !defined(_TARGET_64BIT_)
        if ((tree->TypeGet() != TYP_VOID) && !op1->isContained())
    {
        regMaskTP useCandidates = RBM_NONE;

        info->srcCount = 1;

#if FEATURE_MULTIREG_RET
        if (varTypeIsStruct(tree))
        {
            // op1 has to be either an lclvar or a multi-reg returning call
            if (op1->OperGet() != GT_LCL_VAR)
            {
                noway_assert(op1->IsMultiRegCall());

                ReturnTypeDesc* retTypeDesc = op1->AsCall()->GetReturnTypeDesc();
                info->srcCount              = retTypeDesc->GetReturnRegCount();
                useCandidates               = retTypeDesc->GetABIReturnRegs();
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
        }

        LocationInfoListNode* locationInfo = getLocationInfo(op1);
        if (useCandidates != RBM_NONE)
        {
            locationInfo->info.setSrcCandidates(this, useCandidates);
        }
        useList.Append(locationInfo);
    }
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
void LinearScan::BuildPutArgReg(GenTreeUnOp* node)
{
    TreeNodeInfo* info = currentNodeInfo;
    assert(node != nullptr);
    assert(node->OperIsPutArgReg());
    info->srcCount   = 1;
    regNumber argReg = node->gtRegNum;
    assert(argReg != REG_NA);

    // Set the register requirements for the node.
    regMaskTP argMask = genRegMask(argReg);

#ifdef _TARGET_ARM_
    // If type of node is `long` then it is actually `double`.
    // The actual `long` types must have been transformed as a field list with two fields.
    if (node->TypeGet() == TYP_LONG)
    {
        info->srcCount++;
        info->dstCount = info->srcCount;
        assert(genRegArgNext(argReg) == REG_NEXT(argReg));
        argMask |= genRegMask(REG_NEXT(argReg));
    }
#endif // _TARGET_ARM_
    info->setDstCandidates(this, argMask);
    info->setSrcCandidates(this, argMask);

    // To avoid redundant moves, have the argument operand computed in the
    // register in which the argument is passed to the call.
    LocationInfoListNode* op1Info = getLocationInfo(node->gtOp.gtOp1);
    op1Info->info.setSrcCandidates(this, info->getSrcCandidates(this));
    op1Info->info.isDelayFree = true;
    useList.Append(op1Info);
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
    TreeNodeInfo* info = currentNodeInfo;
    if (call->IsVarargs() && varTypeIsFloating(argNode))
    {
        *callHasFloatRegArgs = true;

        regNumber argReg    = argNode->gtRegNum;
        regNumber targetReg = compiler->getCallArgIntRegister(argReg);
        info->setInternalIntCount(info->internalIntCount + 1);
        info->addInternalCandidates(this, genRegMask(targetReg));
    }
#endif // FEATURE_VARARG
}

//------------------------------------------------------------------------
// BuildGCWriteBarrier: Handle additional register requirements for a GC write barrier
//
// Arguments:
//    tree    - The STORE_IND for which a write barrier is required
//
void LinearScan::BuildGCWriteBarrier(GenTree* tree)
{
    TreeNodeInfo*         info     = currentNodeInfo;
    GenTree*              dst      = tree;
    GenTree*              addr     = tree->gtOp.gtOp1;
    GenTree*              src      = tree->gtOp.gtOp2;
    LocationInfoListNode* addrInfo = getLocationInfo(addr);
    LocationInfoListNode* srcInfo  = getLocationInfo(src);

    // In the case where we are doing a helper assignment, even if the dst
    // is an indir through an lea, we need to actually instantiate the
    // lea in a register
    assert(!addr->isContained() && !src->isContained());
    useList.Append(addrInfo);
    useList.Append(srcInfo);
    info->srcCount = 2;
    assert(info->dstCount == 0);
    bool customSourceRegs = false;

#if NOGC_WRITE_BARRIERS

#if defined(_TARGET_ARM64_)
    // For the NOGC JIT Helper calls
    //
    // the 'addr' goes into x14 (REG_WRITE_BARRIER_DST_BYREF)
    // the 'src'  goes into x15 (REG_WRITE_BARRIER)
    //
    addrInfo->info.setSrcCandidates(this, RBM_WRITE_BARRIER_DST_BYREF);
    srcInfo->info.setSrcCandidates(this, RBM_WRITE_BARRIER);
    customSourceRegs = true;

#elif defined(_TARGET_X86_)

    bool useOptimizedWriteBarrierHelper = compiler->codeGen->genUseOptimizedWriteBarriers(tree, src);

    if (useOptimizedWriteBarrierHelper)
    {
        // Special write barrier:
        // op1 (addr) goes into REG_WRITE_BARRIER (rdx) and
        // op2 (src) goes into any int register.
        addrInfo->info.setSrcCandidates(this, RBM_WRITE_BARRIER);
        srcInfo->info.setSrcCandidates(this, RBM_WRITE_BARRIER_SRC);
        customSourceRegs = true;
    }
#else // !defined(_TARGET_X86_) && !defined(_TARGET_ARM64_)
#error "NOGC_WRITE_BARRIERS is not supported"
#endif // !defined(_TARGET_X86_)

#endif // NOGC_WRITE_BARRIERS

    if (!customSourceRegs)
    {
        // For the standard JIT Helper calls:
        // op1 (addr) goes into REG_ARG_0 and
        // op2 (src) goes into REG_ARG_1
        addrInfo->info.setSrcCandidates(this, RBM_ARG_0);
        srcInfo->info.setSrcCandidates(this, RBM_ARG_1);
    }

    // Both src and dst must reside in a register, which they should since we haven't set
    // either of them as contained.
    assert(addrInfo->info.dstCount == 1);
    assert(srcInfo->info.dstCount == 1);
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
void LinearScan::BuildCmp(GenTree* tree)
{
    TreeNodeInfo* info = currentNodeInfo;
    assert(tree->OperIsCompare() || tree->OperIs(GT_CMP) || tree->OperIs(GT_JCMP));

    info->srcCount = 0;
    assert((info->dstCount == 1) || (tree->TypeGet() == TYP_VOID));

#ifdef _TARGET_X86_
    // If the compare is used by a jump, we just need to set the condition codes. If not, then we need
    // to store the result into the low byte of a register, which requires the dst be a byteable register.
    // We always set the dst candidates, though, because if this is compare is consumed by a jump, they
    // won't be used. We might be able to use GTF_RELOP_JMP_USED to determine this case, but it's not clear
    // that flag is maintained until this location (especially for decomposed long compares).
    info->setDstCandidates(this, RBM_BYTE_REGS);
#endif // _TARGET_X86_

    info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
}

#endif // !LEGACY_BACKEND
