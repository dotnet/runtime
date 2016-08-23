// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

                 Linear Scan Register Allocation

                         a.k.a. LSRA

  Preconditions
    - All register requirements are expressed in the code stream, either as destination
      registers of tree nodes, or as internal registers.  These requirements are
      expressed in the TreeNodeInfo (gtLsraInfo) on each node, which includes:
      - The number of register sources and destinations.
      - The register restrictions (candidates) of the target register, both from itself,
        as producer of the value (dstCandidates), and from its consuming node (srcCandidates).
        Note that the srcCandidates field of TreeNodeInfo refers to the destination register
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
    - Traverse the RefPositions, marking last uses (setLastUses)
      - Note that this is necessary because the execution order doesn't accurately reflect use order.
        There is a "TODO-Throughput" to eliminate this.
    - Allocate registers (allocateRegisters)
    - Annotate nodes with register assignments (resolveRegisters)
    - Add move nodes as needed to resolve conflicting register
      assignments across non-adjacent edges. (resolveEdges, called from resolveRegisters)

  Postconditions:

    Tree nodes (GenTree):
    - GenTree::gtRegNum (and gtRegPair for ARM) is annotated with the register
      assignment for a node. If the node does not require a register, it is
      annotated as such (for single registers, gtRegNum = REG_NA; for register
      pair type, gtRegPair = REG_PAIR_NONE). For a variable definition or interior
      tree node (an "implicit" definition), this is the register to put the result.
      For an expression use, this is the place to find the value that has previously
      been computed.
      - In most cases, this register must satisfy the constraints specified by the TreeNodeInfo.
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
        requirement, LSRA must insert a GT_COPY node between the node and its parent.  The gtRegNum on the GT_COPY node
        must satisfy the register requirement of the parent.
    - GenTree::gtRsvdRegs has a set of registers used for internal temps.
    - A tree node is marked GTF_SPILL if the tree node must be spilled by the code generator after it has been
      evaluated.
      - LSRA currently does not set GTF_SPILLED on such nodes, because it caused problems in the old code generator.
        In the new backend perhaps this should change (see also the note below under CodeGen).
    - A tree node is marked GTF_SPILLED if it is a lclVar that must be reloaded prior to use.
      - The register (gtRegNum) on the node indicates the register to which it must be reloaded.
      - For lclVar nodes, since the uses and defs are distinct tree nodes, it is always possible to annotate the node
        with the register to which the variable must be reloaded.
      - For other nodes, since they represent both the def and use, if the value must be reloaded to a different
        register, LSRA must insert a GT_RELOAD node in order to specify the register to which it should be reloaded.

    Local variable table (LclVarDsc):
    - LclVarDsc::lvRegister is set to true if a local variable has the
      same register assignment for its entire lifetime.
    - LclVarDsc::lvRegNum / lvOtherReg: these are initialized to their
      first value at the end of LSRA (it looks like lvOtherReg isn't?
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

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

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
void lsraAssignRegToTree(GenTreePtr tree, regNumber reg, unsigned regIdx)
{
    if (regIdx == 0)
    {
        tree->gtRegNum = reg;
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
unsigned LinearScan::getWeight(RefPosition* refPos)
{
    unsigned   weight;
    GenTreePtr treeNode = refPos->treeNode;

    if (treeNode != nullptr)
    {
        if (isCandidateLocalRef(treeNode))
        {
            // Tracked locals: use weighted ref cnt as the weight of the
            // ref position.
            GenTreeLclVarCommon* lclCommon = treeNode->AsLclVarCommon();
            LclVarDsc*           varDsc    = &(compiler->lvaTable[lclCommon->gtLclNum]);
            weight                         = varDsc->lvRefCntWtd;
        }
        else
        {
            // Non-candidate local ref or non-lcl tree node.
            // These are considered to have two references in the basic block:
            // a def and a use and hence weighted ref count is 2 times
            // the basic block weight in which they appear.
            weight = 2 * this->blockInfo[refPos->bbNum].weight;
        }
    }
    else
    {
        // Non-tree node ref positions.  These will have a single
        // reference in the basic block and hence their weighted
        // refcount is equal to the block weight in which they
        // appear.
        weight = this->blockInfo[refPos->bbNum].weight;
    }

    return weight;
}

// allRegs represents a set of registers that can
// be used to allocate the specified type in any point
// in time (more of a 'bank' of registers).
regMaskTP LinearScan::allRegs(RegisterType rt)
{
    if (rt == TYP_FLOAT)
    {
        return availableFloatRegs;
    }
    else if (rt == TYP_DOUBLE)
    {
        return availableDoubleRegs;
#ifdef FEATURE_SIMD
        // TODO-Cleanup: Add an RBM_ALLSIMD
    }
    else if (varTypeIsSIMD(rt))
    {
        return availableDoubleRegs;
#endif // FEATURE_SIMD
    }
    else
    {
        return availableIntRegs;
    }
}

//--------------------------------------------------------------------------
// allMultiRegCallNodeRegs: represents a set of registers that can be used
// to allocate a multi-reg call node.
//
// Arguments:
//    call   -  Multi-reg call node
//
// Return Value:
//    Mask representing the set of available registers for multi-reg call
//    node.
//
// Note:
// Multi-reg call node available regs = Bitwise-OR(allregs(GetReturnRegType(i)))
// for all i=0..RetRegCount-1.
regMaskTP LinearScan::allMultiRegCallNodeRegs(GenTreeCall* call)
{
    assert(call->HasMultiRegRetVal());

    ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
    regMaskTP       resultMask  = allRegs(retTypeDesc->GetReturnRegType(0));

    unsigned count = retTypeDesc->GetReturnRegCount();
    for (unsigned i = 1; i < count; ++i)
    {
        resultMask |= allRegs(retTypeDesc->GetReturnRegType(i));
    }

    return resultMask;
}

//--------------------------------------------------------------------------
// allRegs: returns the set of registers that can accomodate the type of
// given node.
//
// Arguments:
//    tree   -  GenTree node
//
// Return Value:
//    Mask representing the set of available registers for given tree
//
// Note: In case of multi-reg call node, the full set of registers must be
// determined by looking at types of individual return register types.
// In this case, the registers may include registers from different register
// sets and will not be limited to the actual ABI return registers.
regMaskTP LinearScan::allRegs(GenTree* tree)
{
    regMaskTP resultMask;

    // In case of multi-reg calls, allRegs is defined as
    // Bitwise-Or(allRegs(GetReturnRegType(i)) for i=0..ReturnRegCount-1
    if (tree->IsMultiRegCall())
    {
        resultMask = allMultiRegCallNodeRegs(tree->AsCall());
    }
    else
    {
        resultMask = allRegs(tree->TypeGet());
    }

    return resultMask;
}

regMaskTP LinearScan::allSIMDRegs()
{
    return availableFloatRegs;
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

/*****************************************************************************
 * Register types
 *****************************************************************************/
template <class T>
RegisterType regType(T type)
{
#ifdef FEATURE_SIMD
    if (varTypeIsSIMD(type))
    {
        return FloatRegisterType;
    }
#endif // FEATURE_SIMD
    return varTypeIsFloating(TypeGet(type)) ? FloatRegisterType : IntRegisterType;
}

bool useFloatReg(var_types type)
{
    return (regType(type) == FloatRegisterType);
}

bool registerTypesEquivalent(RegisterType a, RegisterType b)
{
    return varTypeIsIntegralOrI(a) == varTypeIsIntegralOrI(b);
}

bool isSingleRegister(regMaskTP regMask)
{
    return (regMask != RBM_NONE && genMaxOneBit(regMask));
}

/*****************************************************************************
 * Inline functions for RegRecord
 *****************************************************************************/

bool RegRecord::isFree()
{
    return ((assignedInterval == nullptr || !assignedInterval->isActive) && !isBusyUntilNextKill);
}

/*****************************************************************************
 * Inline functions for LinearScan
 *****************************************************************************/
RegRecord* LinearScan::getRegisterRecord(regNumber regNum)
{
    return &physRegs[regNum];
}

#ifdef DEBUG
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
        switch (getStressLimitRegs())
        {
            case LSRA_LIMIT_CALLEE:
                if (!compiler->opts.compDbgEnC && (mask & RBM_CALLEE_SAVED) != RBM_NONE)
                {
                    mask &= RBM_CALLEE_SAVED;
                }
                break;
            case LSRA_LIMIT_CALLER:
                if ((mask & RBM_CALLEE_TRASH) != RBM_NONE)
                {
                    mask &= RBM_CALLEE_TRASH;
                }
                break;
            case LSRA_LIMIT_SMALL_SET:
                if ((mask & LsraLimitSmallIntSet) != RBM_NONE)
                {
                    mask &= LsraLimitSmallIntSet;
                }
                else if ((mask & LsraLimitSmallFPSet) != RBM_NONE)
                {
                    mask &= LsraLimitSmallFPSet;
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

// TODO-Cleanup: Consider adding an overload that takes a varDsc, and can appropriately
// set such fields as isStructField

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
// conflictingFixedRegReference: Determine whether the current RegRecord has a
//                               fixed register use that conflicts with 'refPosition'
//
// Arguments:
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

bool RegRecord::conflictingFixedRegReference(RefPosition* refPosition)
{
    // Is this a fixed reference of this register?  If so, there is no conflict.
    if (refPosition->isFixedRefOfRegMask(genRegMask(regNum)))
    {
        return false;
    }
    // Otherwise, check for conflicts.
    // There is a conflict if:
    // 1. There is a recent RefPosition on this RegRecord that is at this location,
    //    except in the case where it is a special "putarg" that is associated with this interval, OR
    // 2. There is an upcoming RefPosition at this location, or at the next location
    //    if refPosition is a delayed use (i.e. must be kept live through the next/def location).

    LsraLocation refLocation = refPosition->nodeLocation;
    if (recentRefPosition != nullptr && recentRefPosition->refType != RefTypeKill &&
        recentRefPosition->nodeLocation == refLocation &&
        (!isBusyUntilNextKill || assignedInterval != refPosition->getInterval()))
    {
        return true;
    }
    LsraLocation nextPhysRefLocation = getNextRefLocation();
    if (nextPhysRefLocation == refLocation || (refPosition->delayRegFree && nextPhysRefLocation == (refLocation + 1)))
    {
        return true;
    }
    return false;
}

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
    regMaskTP calleeSaveMask = calleeSaveRegs(getRegisterType(theInterval, rp));
    if (doReverseCallerCallee())
    {
        regMaskTP newAssignment = rp->registerAssignment;
        newAssignment &= calleeSaveMask;
        if (newAssignment != RBM_NONE)
        {
            rp->registerAssignment = newAssignment;
        }
    }
    else
#endif // DEBUG
    {
        // Set preferences so that this register set will be preferred for earlier refs
        theInterval->updateRegisterPreferences(rp->registerAssignment);
    }
}

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

            // Ensure that we have consistent def/use on SDSU temps.
            // However, in the case of a non-commutative rmw def, we must avoid over-constraining
            // the def, so don't propagate a single-register restriction from the consumer to the producer

            if (RefTypeIsUse(rp->refType) && !theInterval->isLocalVar)
            {
                RefPosition* prevRefPosition = theInterval->recentRefPosition;
                assert(prevRefPosition != nullptr && theInterval->firstRefPosition == prevRefPosition);
                regMaskTP prevAssignment = prevRefPosition->registerAssignment;
                regMaskTP newAssignment  = (prevAssignment & rp->registerAssignment);
                if (newAssignment != RBM_NONE)
                {
                    if (!theInterval->hasNonCommutativeRMWDef || !isSingleRegister(newAssignment))
                    {
                        prevRefPosition->registerAssignment = newAssignment;
                    }
                }
                else
                {
                    theInterval->hasConflictingDefUse = true;
                }
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
    newRP->setAllocateIfProfitable(0);

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
    newRP->setAllocateIfProfitable(0);

    associateRefPosWithInterval(newRP);

    DBEXEC(VERBOSE, newRP->dump());
    return newRP;
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

// Iterate through all the registers of the given type
class RegisterIterator
{
    friend class Registers;

public:
    RegisterIterator(RegisterType type) : regType(type)
    {
        if (useFloatReg(regType))
        {
            currentRegNum = REG_FP_FIRST;
        }
        else
        {
            currentRegNum = REG_INT_FIRST;
        }
    }

protected:
    static RegisterIterator Begin(RegisterType regType)
    {
        return RegisterIterator(regType);
    }
    static RegisterIterator End(RegisterType regType)
    {
        RegisterIterator endIter = RegisterIterator(regType);
        // This assumes only integer and floating point register types
        // if we target a processor with additional register types,
        // this would have to change
        if (useFloatReg(regType))
        {
            // This just happens to work for both double & float
            endIter.currentRegNum = REG_NEXT(REG_FP_LAST);
        }
        else
        {
            endIter.currentRegNum = REG_NEXT(REG_INT_LAST);
        }
        return endIter;
    }

public:
    void operator++(int dummy) // int dummy is c++ for "this is postfix ++"
    {
        currentRegNum = REG_NEXT(currentRegNum);
#ifdef _TARGET_ARM_
        if (regType == TYP_DOUBLE)
            currentRegNum = REG_NEXT(currentRegNum);
#endif
    }
    void operator++() // prefix operator++
    {
        currentRegNum = REG_NEXT(currentRegNum);
#ifdef _TARGET_ARM_
        if (regType == TYP_DOUBLE)
            currentRegNum = REG_NEXT(currentRegNum);
#endif
    }
    regNumber operator*()
    {
        return currentRegNum;
    }
    bool operator!=(const RegisterIterator& other)
    {
        return other.currentRegNum != currentRegNum;
    }

private:
    regNumber    currentRegNum;
    RegisterType regType;
};

class Registers
{
public:
    friend class RegisterIterator;
    RegisterType type;
    Registers(RegisterType t)
    {
        type = t;
    }
    RegisterIterator begin()
    {
        return RegisterIterator::Begin(type);
    }
    RegisterIterator end()
    {
        return RegisterIterator::End(type);
    }
};

#ifdef DEBUG
void LinearScan::dumpVarToRegMap(VarToRegMap map)
{
    bool anyPrinted = false;
    for (unsigned varIndex = 0; varIndex < compiler->lvaTrackedCount; varIndex++)
    {
        unsigned varNum = compiler->lvaTrackedToVarNum[varIndex];
        if (map[varIndex] != REG_STK)
        {
            printf("V%02u=%s ", varNum, getRegName(map[varIndex]));
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
    printf("Var=Reg beg of BB%02u: ", block->bbNum);
    VarToRegMap map = getInVarToRegMap(block->bbNum);
    dumpVarToRegMap(map);
}

void LinearScan::dumpOutVarToRegMap(BasicBlock* block)
{
    printf("Var=Reg end of BB%02u: ", block->bbNum);
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
#if MEASURE_MEM_ALLOC
    , lsraIAllocator(nullptr)
#endif // MEASURE_MEM_ALLOC
    , intervals(LinearScanMemoryAllocatorInterval(theCompiler))
    , refPositions(LinearScanMemoryAllocatorRefPosition(theCompiler))
{
#ifdef DEBUG
    maxNodeLocation   = 0;
    activeRefPosition = nullptr;

    // Get the value of the environment variable that controls stress for register allocation
    lsraStressMask = JitConfig.JitStressRegs();
#if 0
#ifdef DEBUG
    if (lsraStressMask != 0)
    {
        // The code in this #if can be used to debug JitStressRegs issues according to
        // method hash.  To use, simply set environment variables JitStressRegsHashLo and JitStressRegsHashHi
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
        else if (dump == true)
        {
            printf("JitStressRegs = %x for method %s, hash = 0x%x.\n",
                   lsraStressMask, compiler->info.compFullName, compiler->info.compMethodHash());
            printf("");         // in our logic this causes a flush
        }
    }
#endif // DEBUG
#endif

    dumpTerse = (JitConfig.JitDumpTerseLsra() != 0);

#endif // DEBUG
    availableIntRegs = (RBM_ALLINT & ~compiler->codeGen->regSet.rsMaskResvd);
#if ETW_EBP_FRAMED
    availableIntRegs &= ~RBM_FPBASE;
#endif // ETW_EBP_FRAMED
    availableFloatRegs  = RBM_ALLFLOAT;
    availableDoubleRegs = RBM_ALLDOUBLE;

#ifdef _TARGET_AMD64_
    if (compiler->opts.compDbgEnC)
    {
        // On x64 when the EnC option is set, we always save exactly RBP, RSI and RDI.
        // RBP is not available to the register allocator, so RSI and RDI are the only
        // callee-save registers available.
        availableIntRegs &= ~RBM_CALLEE_SAVED | RBM_RSI | RBM_RDI;
        availableFloatRegs &= ~RBM_CALLEE_SAVED;
        availableDoubleRegs &= ~RBM_CALLEE_SAVED;
    }
#endif // _TARGET_AMD64_
    compiler->rpFrameType           = FT_NOT_SET;
    compiler->rpMustCreateEBPCalled = false;

    compiler->codeGen->intRegState.rsIsFloat   = false;
    compiler->codeGen->floatRegState.rsIsFloat = true;

    // Block sequencing (the order in which we schedule).
    // Note that we don't initialize the bbVisitedSet until we do the first traversal
    // (currently during Lowering's second phase, where it sets the TreeNodeInfo).
    // This is so that any blocks that are added during the first phase of Lowering
    // are accounted for (and we don't have BasicBlockEpoch issues).
    blockSequencingDone   = false;
    blockSequence         = nullptr;
    blockSequenceWorkList = nullptr;
    curBBSeqNum           = 0;
    bbSeqCount            = 0;

    // Information about each block, including predecessor blocks used for variable locations at block entry.
    blockInfo = nullptr;

    // Populate the register mask table.
    // The first two masks in the table are allint/allfloat
    // The next N are the masks for each single register.
    // After that are the dynamically added ones.
    regMaskTable               = new (compiler, CMK_LSRA) regMaskTP[numMasks];
    regMaskTable[ALLINT_IDX]   = allRegs(TYP_INT);
    regMaskTable[ALLFLOAT_IDX] = allRegs(TYP_DOUBLE);

    regNumber reg;
    for (reg = REG_FIRST; reg < REG_COUNT; reg = REG_NEXT(reg))
    {
        regMaskTable[FIRST_SINGLE_REG_IDX + reg - REG_FIRST] = (reg == REG_STK) ? RBM_NONE : genRegMask(reg);
    }
    nextFreeMask = FIRST_SINGLE_REG_IDX + REG_COUNT;
    noway_assert(nextFreeMask <= numMasks);
}

// Return the reg mask corresponding to the given index.
regMaskTP LinearScan::GetRegMaskForIndex(RegMaskIndex index)
{
    assert(index < numMasks);
    assert(index < nextFreeMask);
    return regMaskTable[index];
}

// Given a reg mask, return the index it corresponds to. If it is not a 'well known' reg mask,
// add it at the end. This method has linear behavior in the worst cases but that is fairly rare.
// Most methods never use any but the well-known masks, and when they do use more
// it is only one or two more.
LinearScan::RegMaskIndex LinearScan::GetIndexForRegMask(regMaskTP mask)
{
    RegMaskIndex result;
    if (isSingleRegister(mask))
    {
        result = genRegNumFromMask(mask) + FIRST_SINGLE_REG_IDX;
    }
    else if (mask == allRegs(TYP_INT))
    {
        result = ALLINT_IDX;
    }
    else if (mask == allRegs(TYP_DOUBLE))
    {
        result = ALLFLOAT_IDX;
    }
    else
    {
        for (int i = FIRST_SINGLE_REG_IDX + REG_COUNT; i < nextFreeMask; i++)
        {
            if (regMaskTable[i] == mask)
            {
                return i;
            }
        }

        // We only allocate a fixed number of masks. Since we don't reallocate, we will throw a
        // noway_assert if we exceed this limit.
        noway_assert(nextFreeMask < numMasks);

        regMaskTable[nextFreeMask] = mask;
        result                     = nextFreeMask;
        nextFreeMask++;
    }
    assert(mask == regMaskTable[result]);
    return result;
}

// We've decided that we can't use a register during register allocation (probably FPBASE),
// but we've already added it to the register masks. Go through the masks and remove it.
void LinearScan::RemoveRegisterFromMasks(regNumber reg)
{
    JITDUMP("Removing register %s from LSRA register masks\n", getRegName(reg));

    regMaskTP mask = ~genRegMask(reg);
    for (int i = 0; i < nextFreeMask; i++)
    {
        regMaskTable[i] &= mask;
    }

    JITDUMP("After removing register:\n");
    DBEXEC(VERBOSE, dspRegisterMaskTable());
}

#ifdef DEBUG
void LinearScan::dspRegisterMaskTable()
{
    printf("LSRA register masks. Total allocated: %d, total used: %d\n", numMasks, nextFreeMask);
    for (int i = 0; i < nextFreeMask; i++)
    {
        printf("%2u: ", i);
        dspRegMask(regMaskTable[i]);
        printf("\n");
    }
}
#endif // DEBUG

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
// setBlockSequence:Determine the block order for register allocation.
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
//    This requires a traversal of the BasicBlocks, and could potentially be
//    combined with the first traversal (currently the one in Lowering that sets the
//    TreeNodeInfo).

void LinearScan::setBlockSequence()
{
    // Reset the "visited" flag on each block.
    compiler->EnsureBasicBlockEpoch();
    bbVisitedSet = BlockSetOps::MakeEmpty(compiler);
    BlockSet BLOCKSET_INIT_NOCOPY(readySet, BlockSetOps::MakeEmpty(compiler));
    assert(blockSequence == nullptr && bbSeqCount == 0);
    blockSequence            = new (compiler, CMK_LSRA) BasicBlock*[compiler->fgBBcount];
    bbNumMaxBeforeResolution = compiler->fgBBNumMax;
    blockInfo                = new (compiler, CMK_LSRA) LsraBlockInfo[bbNumMaxBeforeResolution + 1];

    assert(blockSequenceWorkList == nullptr);

    bool addedInternalBlocks = false;
    verifiedAllBBs           = false;
    BasicBlock* nextBlock;
    for (BasicBlock* block = compiler->fgFirstBB; block != nullptr; block = nextBlock)
    {
        blockSequence[bbSeqCount] = block;
        markBlockVisited(block);
        bbSeqCount++;
        nextBlock = nullptr;

        // Initialize the blockInfo.
        // predBBNum will be set later.  0 is never used as a bbNum.
        blockInfo[block->bbNum].predBBNum = 0;
        // We check for critical edges below, but initialize to false.
        blockInfo[block->bbNum].hasCriticalInEdge  = false;
        blockInfo[block->bbNum].hasCriticalOutEdge = false;
        blockInfo[block->bbNum].weight             = block->bbWeight;

        if (block->GetUniquePred(compiler) == nullptr)
        {
            for (flowList* pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
            {
                BasicBlock* predBlock = pred->flBlock;
                if (predBlock->NumSucc(compiler) > 1)
                {
                    blockInfo[block->bbNum].hasCriticalInEdge = true;
                    break;
                }
                else if (predBlock->bbJumpKind == BBJ_SWITCH)
                {
                    assert(!"Switch with single successor");
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

        for (unsigned succIndex = 0; succIndex < block->NumSucc(compiler); succIndex++)
        {
            BasicBlock* succ = block->GetSucc(succIndex, compiler);
            if (checkForCriticalOutEdge && succ->GetUniquePred(compiler) == nullptr)
            {
                blockInfo[block->bbNum].hasCriticalOutEdge = true;
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
                addToBlockSequenceWorkList(readySet, succ);
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
                // If we don't encounter all blocks by traversing the regular sucessor links, do a full
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
                        addToBlockSequenceWorkList(readySet, block);
                        BlockSetOps::AddElemD(compiler, readySet, block->bbNum);
                    }
                }

                for (BasicBlock* block = compiler->fgFirstBB; block; block = block->bbNext)
                {
                    if (!isBlockVisited(block))
                    {
                        addToBlockSequenceWorkList(readySet, block);
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
        JITDUMP("BB%02u", block->bbNum);

        if (block->isMaxBBWeight())
        {
            JITDUMP("(MAX) ");
        }
        else
        {
            JITDUMP("(%6s) ", refCntWtd2str(block->getBBWeight(compiler)));
        }

        if (i % 10 == 0)
        {
            JITDUMP("\n                     ");
        }
    }
    JITDUMP("\n\n");
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
        unsigned weight1 = block1->getBBWeight(compiler);
        unsigned weight2 = block2->getBBWeight(compiler);

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
void LinearScan::addToBlockSequenceWorkList(BlockSet sequencedBlockSet, BasicBlock* block)
{
    // The block that is being added is not already sequenced
    assert(!BlockSetOps::IsMember(compiler, sequencedBlockSet, block->bbNum));

    // Get predSet of block
    BlockSet  BLOCKSET_INIT_NOCOPY(predSet, BlockSetOps::MakeEmpty(compiler));
    flowList* pred;
    for (pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
    {
        BlockSetOps::AddElemD(compiler, predSet, pred->flBlock->bbNum);
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
    BasicBlock* curBB = compiler->fgFirstBB;
    curBBSeqNum       = 0;
    curBBNum          = curBB->bbNum;
    clearVisitedBlocks();
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
// Assumptions:
//    Lowering must have set the NodeInfo (gtLsraInfo) on each node to communicate
//    the register requirements.

void LinearScan::doLinearScan()
{
#ifdef DEBUG
    if (VERBOSE)
    {
        printf("*************** In doLinearScan\n");
        printf("Trees before linear scan register allocator (LSRA)\n");
        compiler->fgDispBasicBlocks(true);
    }
#endif // DEBUG

    splitBBNumToTargetBBNumMap = nullptr;

    // This is complicated by the fact that physical registers have refs associated
    // with locations where they are killed (e.g. calls), but we don't want to
    // count these as being touched.

    compiler->codeGen->regSet.rsClearRegsModified();

    // Figure out if we're going to use an RSP frame or an RBP frame. We need to do this
    // before building the intervals and ref positions, because those objects will embed
    // RBP in various register masks (like preferences) if RBP is allowed to be allocated.
    setFrameType();

    initMaxSpill();
    buildIntervals();
    DBEXEC(VERBOSE, TupleStyleDump(LSRA_DUMP_REFPOS));
    compiler->EndPhase(PHASE_LINEAR_SCAN_BUILD);

    DBEXEC(VERBOSE, lsraDumpIntervals("after buildIntervals"));

    BlockSetOps::ClearD(compiler, bbVisitedSet);
    initVarRegMaps();
    allocateRegisters();
    compiler->EndPhase(PHASE_LINEAR_SCAN_ALLOC);
    resolveRegisters();
    compiler->EndPhase(PHASE_LINEAR_SCAN_RESOLVE);

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
    JITDUMP("Recording Var Locations at start of BB%02u\n", bb->bbNum);
    VarToRegMap map   = getInVarToRegMap(bb->bbNum);
    unsigned    count = 0;

    VARSET_ITER_INIT(compiler, iter, bb->bbLiveIn, varIndex);
    while (iter.NextElem(compiler, &varIndex))
    {
        unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
        LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);
        regNumber  regNum = getVarReg(map, varNum);

        regNumber oldRegNum = varDsc->lvRegNum;
        regNumber newRegNum = regNum;

        if (oldRegNum != newRegNum)
        {
            JITDUMP("  V%02u(%s->%s)", varNum, compiler->compRegVarName(oldRegNum),
                    compiler->compRegVarName(newRegNum));
            varDsc->lvRegNum = newRegNum;
            count++;
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

void Interval::setLocalNumber(unsigned lclNum, LinearScan* linScan)
{
    linScan->localVarIntervals[lclNum] = this;

    assert(linScan->getIntervalForLocalVar(lclNum) == this);
    this->isLocalVar = true;
    this->varNum     = lclNum;
}

// identify the candidates which we are not going to enregister due to
// being used in EH in a way we don't want to deal with
// this logic cloned from fgInterBlockLocalVarLiveness
void LinearScan::identifyCandidatesExceptionDataflow()
{
    VARSET_TP   VARSET_INIT_NOCOPY(exceptVars, VarSetOps::MakeEmpty(compiler));
    VARSET_TP   VARSET_INIT_NOCOPY(filterVars, VarSetOps::MakeEmpty(compiler));
    VARSET_TP   VARSET_INIT_NOCOPY(finallyVars, VarSetOps::MakeEmpty(compiler));
    BasicBlock* block;

    foreach_block(compiler, block)
    {
        if (block->bbCatchTyp != BBCT_NONE)
        {
            // live on entry to handler
            VarSetOps::UnionD(compiler, exceptVars, block->bbLiveIn);
        }

        if (block->bbJumpKind == BBJ_EHFILTERRET)
        {
            // live on exit from filter
            VarSetOps::UnionD(compiler, filterVars, block->bbLiveOut);
        }
        else if (block->bbJumpKind == BBJ_EHFINALLYRET)
        {
            // live on exit from finally
            VarSetOps::UnionD(compiler, finallyVars, block->bbLiveOut);
        }
#if FEATURE_EH_FUNCLETS
        // Funclets are called and returned from, as such we can only count on the frame
        // pointer being restored, and thus everything live in or live out must be on the
        // stack
        if (block->bbFlags & BBF_FUNCLET_BEG)
        {
            VarSetOps::UnionD(compiler, exceptVars, block->bbLiveIn);
        }
        if ((block->bbJumpKind == BBJ_EHFINALLYRET) || (block->bbJumpKind == BBJ_EHFILTERRET) ||
            (block->bbJumpKind == BBJ_EHCATCHRET))
        {
            VarSetOps::UnionD(compiler, exceptVars, block->bbLiveOut);
        }
#endif // FEATURE_EH_FUNCLETS
    }

    // slam them all together (there was really no need to use more than 2 bitvectors here)
    VarSetOps::UnionD(compiler, exceptVars, filterVars);
    VarSetOps::UnionD(compiler, exceptVars, finallyVars);

    /* Mark all pointer variables live on exit from a 'finally'
        block as either volatile for non-GC ref types or as
        'explicitly initialized' (volatile and must-init) for GC-ref types */

    VARSET_ITER_INIT(compiler, iter, exceptVars, varIndex);
    while (iter.NextElem(compiler, &varIndex))
    {
        unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
        LclVarDsc* varDsc = compiler->lvaTable + varNum;

        compiler->lvaSetVarDoNotEnregister(varNum DEBUGARG(Compiler::DNER_LiveInOutOfHandler));

        if (varTypeIsGC(varDsc))
        {
            if (VarSetOps::IsMember(compiler, finallyVars, varIndex) && !varDsc->lvIsParam)
            {
                varDsc->lvMustInit = true;
            }
        }
    }
}

bool LinearScan::isRegCandidate(LclVarDsc* varDsc)
{
    // Check to see if opt settings permit register variables
    if ((compiler->opts.compFlags & CLFLG_REGVAR) == 0)
    {
        return false;
    }

    // If we have JMP, reg args must be put on the stack

    if (compiler->compJmpOpUsed && varDsc->lvIsRegArg)
    {
        return false;
    }

    if (!varDsc->lvTracked)
    {
        return false;
    }

    // Don't allocate registers for dependently promoted struct fields
    if (compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc))
    {
        return false;
    }
    return true;
}

// Identify locals & compiler temps that are register candidates
// TODO-Cleanup: This was cloned from Compiler::lvaSortByRefCount() in lclvars.cpp in order
// to avoid perturbation, but should be merged.

void LinearScan::identifyCandidates()
{
    if (compiler->lvaCount == 0)
    {
        return;
    }

    if (compiler->compHndBBtabCount > 0)
    {
        identifyCandidatesExceptionDataflow();
    }

    // initialize mapping from local to interval
    localVarIntervals = new (compiler, CMK_LSRA) Interval*[compiler->lvaCount];

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

    VarSetOps::AssignNoCopy(compiler, fpCalleeSaveCandidateVars, VarSetOps::MakeEmpty(compiler));
    VARSET_TP    VARSET_INIT_NOCOPY(fpMaybeCandidateVars, VarSetOps::MakeEmpty(compiler));
    unsigned int floatVarCount        = 0;
    unsigned int thresholdFPRefCntWtd = 4 * BB_UNITY_WEIGHT;
    unsigned int maybeFPRefCntWtd     = 2 * BB_UNITY_WEIGHT;
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    VarSetOps::AssignNoCopy(compiler, largeVectorVars, VarSetOps::MakeEmpty(compiler));
    VarSetOps::AssignNoCopy(compiler, largeVectorCalleeSaveCandidateVars, VarSetOps::MakeEmpty(compiler));
    unsigned int largeVectorVarCount           = 0;
    unsigned int thresholdLargeVectorRefCntWtd = 4 * BB_UNITY_WEIGHT;
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

    for (lclNum = 0, varDsc = compiler->lvaTable; lclNum < compiler->lvaCount; lclNum++, varDsc++)
    {
        // Assign intervals to all the variables - this makes it easier to map
        // them back
        var_types intervalType = (var_types)varDsc->lvType;
        Interval* newInt       = newInterval(intervalType);

        newInt->setLocalNumber(lclNum, this);
        if (varDsc->lvIsStructField)
        {
            newInt->isStructField = true;
        }

        // Initialize all variables to REG_STK
        varDsc->lvRegNum = REG_STK;
#ifndef _TARGET_64BIT_
        varDsc->lvOtherReg = REG_STK;
#endif // _TARGET_64BIT_

#if !defined(_TARGET_64BIT_)
        if (intervalType == TYP_LONG)
        {
            // Long variables should not be register candidates.
            // Lowering will have split any candidate lclVars into lo/hi vars.
            varDsc->lvLRACandidate = 0;
            continue;
        }
#endif // !defined(_TARGET_64BIT)

        /* Track all locals that can be enregistered */

        varDsc->lvLRACandidate = 1;

        if (!isRegCandidate(varDsc))
        {
            varDsc->lvLRACandidate = 0;
            continue;
        }

        // Start with lvRegister as false - set it true only if the variable gets
        // the same register assignment throughout
        varDsc->lvRegister = false;

        /* If the ref count is zero */
        if (varDsc->lvRefCnt == 0)
        {
            /* Zero ref count, make this untracked */
            varDsc->lvRefCntWtd    = 0;
            varDsc->lvLRACandidate = 0;
        }

        // Variables that are address-exposed are never enregistered, or tracked.
        // A struct may be promoted, and a struct that fits in a register may be fully enregistered.
        // Pinned variables may not be tracked (a condition of the GCInfo representation)
        // or enregistered, on x86 -- it is believed that we can enregister pinned (more properly, "pinning")
        // references when using the general GC encoding.

        if (varDsc->lvAddrExposed || !varTypeIsEnregisterableStruct(varDsc))
        {
            varDsc->lvLRACandidate = 0;
#ifdef DEBUG
            Compiler::DoNotEnregisterReason dner = Compiler::DNER_AddrExposed;
            if (!varDsc->lvAddrExposed)
            {
                dner = Compiler::DNER_IsStruct;
            }
#endif // DEBUG
            compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(dner));
        }
        else if (varDsc->lvPinned)
        {
            varDsc->lvTracked = 0;
#ifdef JIT32_GCENCODER
            compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(Compiler::DNER_PinningRef));
#endif // JIT32_GCENCODER
        }

        //  Are we not optimizing and we have exception handlers?
        //   if so mark all args and locals as volatile, so that they
        //   won't ever get enregistered.
        //
        if (compiler->opts.MinOpts() && compiler->compHndBBtabCount > 0)
        {
            compiler->lvaSetVarDoNotEnregister(lclNum DEBUGARG(Compiler::DNER_LiveInOutOfHandler));
            varDsc->lvLRACandidate = 0;
            continue;
        }

        if (varDsc->lvDoNotEnregister)
        {
            varDsc->lvLRACandidate = 0;
            continue;
        }

        var_types type = genActualType(varDsc->TypeGet());

        switch (type)
        {
#if CPU_HAS_FP_SUPPORT
            case TYP_FLOAT:
            case TYP_DOUBLE:
                if (compiler->opts.compDbgCode)
                {
                    varDsc->lvLRACandidate = 0;
                }
                break;
#endif // CPU_HAS_FP_SUPPORT

            case TYP_INT:
            case TYP_LONG:
            case TYP_REF:
            case TYP_BYREF:
                break;

#ifdef FEATURE_SIMD
            case TYP_SIMD12:
            case TYP_SIMD16:
            case TYP_SIMD32:
                if (varDsc->lvPromoted)
                {
                    varDsc->lvLRACandidate = 0;
                }
                break;
            // TODO-1stClassStructs: Move TYP_SIMD8 up with the other SIMD types, after handling the param issue
            // (passing & returning as TYP_LONG).
            case TYP_SIMD8:
#endif // FEATURE_SIMD

            case TYP_STRUCT:
            {
                varDsc->lvLRACandidate = 0;
            }
            break;

            case TYP_UNDEF:
            case TYP_UNKNOWN:
                noway_assert(!"lvType not set correctly");
                varDsc->lvType = TYP_INT;

                __fallthrough;

            default:
                varDsc->lvLRACandidate = 0;
        }

        // we will set this later when we have determined liveness
        if (varDsc->lvLRACandidate)
        {
            varDsc->lvMustInit = false;
        }

        // We maintain two sets of FP vars - those that meet the first threshold of weighted ref Count,
        // and those that meet the second (see the definitions of thresholdFPRefCntWtd and maybeFPRefCntWtd
        // above).
        CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        // Additionally, when we are generating AVX on non-UNIX amd64, we keep a separate set of the LargeVectorType
        // vars.
        if (varDsc->lvType == LargeVectorType)
        {
            largeVectorVarCount++;
            VarSetOps::AddElemD(compiler, largeVectorVars, varDsc->lvVarIndex);
            unsigned refCntWtd = varDsc->lvRefCntWtd;
            if (refCntWtd >= thresholdLargeVectorRefCntWtd)
            {
                VarSetOps::AddElemD(compiler, largeVectorCalleeSaveCandidateVars, varDsc->lvVarIndex);
            }
        }
        else
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            if (regType(newInt->registerType) == FloatRegisterType)
        {
            floatVarCount++;
            unsigned refCntWtd = varDsc->lvRefCntWtd;
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
    }

    // The factors we consider to determine which set of fp vars to use as candidates for callee save
    // registers current include the number of fp vars, whether there are loops, and whether there are
    // multiple exits.  These have been selected somewhat empirically, but there is probably room for
    // more tuning.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    if (VERBOSE)
    {
        printf("\nFP callee save candidate vars: ");
        if (!VarSetOps::IsEmpty(compiler, fpCalleeSaveCandidateVars))
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

#ifdef _TARGET_ARM_
#ifdef DEBUG
    if (VERBOSE)
    {
        // Frame layout is only pre-computed for ARM
        printf("\nlvaTable after IdentifyCandidates\n");
        compiler->lvaTableDump();
    }
#endif // DEBUG
#endif // _TARGET_ARM_
}

// TODO-Throughput: This mapping can surely be more efficiently done
void LinearScan::initVarRegMaps()
{
    assert(compiler->lvaTrackedFixed); // We should have already set this to prevent us from adding any new tracked
                                       // variables.

    // The compiler memory allocator requires that the allocation be an
    // even multiple of int-sized objects
    unsigned int varCount = compiler->lvaTrackedCount;
    regMapCount           = (unsigned int)roundUp(varCount, sizeof(int));

    // Not sure why blocks aren't numbered from zero, but they don't appear to be.
    // So, if we want to index by bbNum we have to know the maximum value.
    unsigned int bbCount = compiler->fgBBNumMax + 1;

    inVarToRegMaps  = new (compiler, CMK_LSRA) regNumber*[bbCount];
    outVarToRegMaps = new (compiler, CMK_LSRA) regNumber*[bbCount];

    if (varCount > 0)
    {
        // This VarToRegMap is used during the resolution of critical edges.
        sharedCriticalVarToRegMap = new (compiler, CMK_LSRA) regNumber[regMapCount];

        for (unsigned int i = 0; i < bbCount; i++)
        {
            regNumber* inVarToRegMap  = new (compiler, CMK_LSRA) regNumber[regMapCount];
            regNumber* outVarToRegMap = new (compiler, CMK_LSRA) regNumber[regMapCount];

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
    assert(reg < UCHAR_MAX && varNum < compiler->lvaCount);
    inVarToRegMaps[bbNum][compiler->lvaTable[varNum].lvVarIndex] = reg;
}

void LinearScan::setOutVarRegForBB(unsigned int bbNum, unsigned int varNum, regNumber reg)
{
    assert(reg < UCHAR_MAX && varNum < compiler->lvaCount);
    outVarToRegMaps[bbNum][compiler->lvaTable[varNum].lvVarIndex] = reg;
}

LinearScan::SplitEdgeInfo LinearScan::getSplitEdgeInfo(unsigned int bbNum)
{
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
    assert(bbNum <= compiler->fgBBNumMax);
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

regNumber LinearScan::getVarReg(VarToRegMap bbVarToRegMap, unsigned int varNum)
{
    assert(compiler->lvaTable[varNum].lvTracked);
    return bbVarToRegMap[compiler->lvaTable[varNum].lvVarIndex];
}

// Initialize the incoming VarToRegMap to the given map values (generally a predecessor of
// the block)
VarToRegMap LinearScan::setInVarToRegMap(unsigned int bbNum, VarToRegMap srcVarToRegMap)
{
    VarToRegMap inVarToRegMap = inVarToRegMaps[bbNum];
    memcpy(inVarToRegMap, srcVarToRegMap, (regMapCount * sizeof(regNumber)));
    return inVarToRegMap;
}

// find the last node in the tree in execution order
// TODO-Throughput: this is inefficient!
GenTree* lastNodeInTree(GenTree* tree)
{
    // There is no gtprev on the top level tree node so
    // apparently the way to walk a tree backwards is to walk
    // it forward, find the last node, and walk back from there.

    GenTree* last = nullptr;
    if (tree->OperGet() == GT_STMT)
    {
        GenTree* statement = tree;

        foreach_treenode_execution_order(tree, statement)
        {
            last = tree;
        }
        return last;
    }
    else
    {
        while (tree)
        {
            last = tree;
            tree = tree->gtNext;
        }
        return last;
    }
}

// given a tree node
RefType refTypeForLocalRefNode(GenTree* node)
{
    assert(node->IsLocal());

    // We don't support updates
    assert((node->gtFlags & GTF_VAR_USEASG) == 0);

    if (node->gtFlags & GTF_VAR_DEF)
    {
        return RefTypeDef;
    }
    else
    {
        return RefTypeUse;
    }
}

// This function sets RefPosition last uses by walking the RefPositions, instead of walking the
// tree nodes in execution order (as was done in a previous version).
// This is because the execution order isn't strictly correct, specifically for
// references to local variables that occur in arg lists.
//
// TODO-Throughput: This function should eventually be eliminated, as we should be able to rely on last uses
// being set by dataflow analysis.  It is necessary to do it this way only because the execution
// order wasn't strictly correct.

void LinearScan::setLastUses(BasicBlock* block)
{
#ifdef DEBUG
    if (VERBOSE)
    {
        JITDUMP("\n\nCALCULATING LAST USES for block %u, liveout=", block->bbNum);
        dumpConvertedVarSet(compiler, block->bbLiveOut);
        JITDUMP("\n==============================\n");
    }
#endif // DEBUG

    unsigned keepAliveVarNum = BAD_VAR_NUM;
    if (compiler->lvaKeepAliveAndReportThis())
    {
        keepAliveVarNum = compiler->info.compThisArg;
        assert(compiler->info.compIsStatic == false);
    }

    // find which uses are lastUses

    // Work backwards starting with live out.
    // 'temp' is updated to include any exposed use (including those in this
    // block that we've already seen).  When we encounter a use, if it's
    // not in that set, then it's a last use.

    VARSET_TP VARSET_INIT(compiler, temp, block->bbLiveOut);

    auto currentRefPosition = refPositions.rbegin();

    while (currentRefPosition->refType != RefTypeBB)
    {
        // We should never see ParamDefs or ZeroInits within a basic block.
        assert(currentRefPosition->refType != RefTypeParamDef && currentRefPosition->refType != RefTypeZeroInit);
        if (currentRefPosition->isIntervalRef() && currentRefPosition->getInterval()->isLocalVar)
        {
            unsigned varNum   = currentRefPosition->getInterval()->varNum;
            unsigned varIndex = currentRefPosition->getInterval()->getVarIndex(compiler);
            // We should always have a tree node for a localVar, except for the "special" RefPositions.
            GenTreePtr tree = currentRefPosition->treeNode;
            assert(tree != nullptr || currentRefPosition->refType == RefTypeExpUse ||
                   currentRefPosition->refType == RefTypeDummyDef);
            if (!VarSetOps::IsMember(compiler, temp, varIndex) && varNum != keepAliveVarNum)
            {
                // There was no exposed use, so this is a
                // "last use" (and we mark it thus even if it's a def)

                if (tree != nullptr)
                {
                    tree->gtFlags |= GTF_VAR_DEATH;
                }
                LsraLocation loc = currentRefPosition->nodeLocation;
#ifdef DEBUG
                if (getLsraExtendLifeTimes())
                {
                    JITDUMP("last use of V%02u @%u (not marked as last use for LSRA due to extendLifetimes stress "
                            "option)\n",
                            compiler->lvaTrackedToVarNum[varIndex], loc);
                }
                else
#endif // DEBUG
                {
                    JITDUMP("last use of V%02u @%u\n", compiler->lvaTrackedToVarNum[varIndex], loc);
                    currentRefPosition->lastUse = true;
                }
                VarSetOps::AddElemD(compiler, temp, varIndex);
            }
            else
            {
                currentRefPosition->lastUse = false;
                if (tree != nullptr)
                {
                    tree->gtFlags &= ~GTF_VAR_DEATH;
                }
            }

            if (currentRefPosition->refType == RefTypeDef || currentRefPosition->refType == RefTypeDummyDef)
            {
                VarSetOps::RemoveElemD(compiler, temp, varIndex);
            }
        }
        assert(currentRefPosition != refPositions.rend());
        ++currentRefPosition;
    }

#ifdef DEBUG
    VARSET_TP VARSET_INIT(compiler, temp2, block->bbLiveIn);
    VarSetOps::DiffD(compiler, temp2, temp);
    VarSetOps::DiffD(compiler, temp, block->bbLiveIn);
    bool foundDiff = false;

    {
        VARSET_ITER_INIT(compiler, iter, temp, varIndex);
        while (iter.NextElem(compiler, &varIndex))
        {
            unsigned varNum = compiler->lvaTrackedToVarNum[varIndex];
            if (compiler->lvaTable[varNum].lvLRACandidate)
            {
                JITDUMP("BB%02u: V%02u is computed live, but not in LiveIn set.\n", block->bbNum, varNum);
                foundDiff = true;
            }
        }
    }

    {
        VARSET_ITER_INIT(compiler, iter, temp2, varIndex);
        while (iter.NextElem(compiler, &varIndex))
        {
            unsigned varNum = compiler->lvaTrackedToVarNum[varIndex];
            if (compiler->lvaTable[varNum].lvLRACandidate)
            {
                JITDUMP("BB%02u: V%02u is in LiveIn set, but not computed live.\n", block->bbNum, varNum);
                foundDiff = true;
            }
        }
    }

    assert(!foundDiff);
#endif // DEBUG
}

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
            killMask = RBM_RAX | RBM_RDX;
            break;

        case GT_MOD:
        case GT_DIV:
        case GT_UMOD:
        case GT_UDIV:
            if (!varTypeIsFloating(tree->TypeGet()))
            {
                // RDX needs to be killed early, because it must not be used as a source register
                // (unlike most cases, where the kill happens AFTER the uses).  So for this kill,
                // we add the RefPosition at the tree loc (where the uses are located) instead of the
                // usual kill location which is the same as the defs at tree loc+1.
                // Note that we don't have to add interference for the live vars, because that
                // will be done below, and is not sensitive to the precise location.
                LsraLocation currentLoc = tree->gtLsraInfo.loc;
                assert(currentLoc != 0);
                addRefsForPhysRegMask(RBM_RDX, currentLoc, RefTypeKill, true);
                // Both RAX and RDX are killed by the operation
                killMask = RBM_RAX | RBM_RDX;
            }
            break;
#endif // _TARGET_XARCH_
        case GT_COPYOBJ:
            killMask = compiler->compHelperCallKillSet(CORINFO_HELP_ASSIGN_BYREF);
            break;

        case GT_COPYBLK:
        {
            GenTreeCpBlk* cpBlkNode = tree->AsCpBlk();
            switch (cpBlkNode->gtBlkOpKind)
            {
                case GenTreeBlkOp::BlkOpKindHelper:
                    killMask = compiler->compHelperCallKillSet(CORINFO_HELP_MEMCPY);
                    break;
#ifdef _TARGET_XARCH_
                case GenTreeBlkOp::BlkOpKindRepInstr:
                    // rep movs kills RCX, RDI and RSI
                    killMask = RBM_RCX | RBM_RDI | RBM_RSI;
                    break;
#else
                case GenTreeBlkOp::BlkOpKindRepInstr:
#endif
                case GenTreeBlkOp::BlkOpKindUnroll:
                case GenTreeBlkOp::BlkOpKindInvalid:
                    // for these 'cpBlkNode->gtBlkOpKind' kinds, we leave 'killMask' = RBM_NONE
                    break;
            }
        }
        break;

        case GT_INITBLK:
        {
            GenTreeInitBlk* initBlkNode = tree->AsInitBlk();
            switch (initBlkNode->gtBlkOpKind)
            {
                case GenTreeBlkOp::BlkOpKindHelper:
                    killMask = compiler->compHelperCallKillSet(CORINFO_HELP_MEMSET);
                    break;
#ifdef _TARGET_XARCH_
                case GenTreeBlkOp::BlkOpKindRepInstr:
                    // rep stos kills RCX and RDI
                    killMask = RBM_RCX | RBM_RDI;
                    break;
#else
                case GenTreeBlkOp::BlkOpKindRepInstr:
#endif
                case GenTreeBlkOp::BlkOpKindUnroll:
                case GenTreeBlkOp::BlkOpKindInvalid:
                    // for these 'cpBlkNode->gtBlkOpKind' kinds, we leave 'killMask' = RBM_NONE
                    break;
            }
        }
        break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROL:
        case GT_ROR:
            if (tree->gtLsraInfo.isHelperCallWithKills)
            {
                killMask = RBM_CALLEE_TRASH;
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
            if (tree->IsHelperCall())
            {
                GenTreeCall*    call     = tree->AsCall();
                CorInfoHelpFunc helpFunc = compiler->eeGetHelperNum(call->gtCallMethHnd);
                killMask                 = compiler->compHelperCallKillSet(helpFunc);
            }
            else
#endif // _TARGET_X86_
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
            }
            break;
        case GT_STOREIND:
            if (compiler->codeGen->gcInfo.gcIsWriteBarrierAsgNode(tree))
            {
                killMask = RBM_CALLEE_TRASH_NOGC;
#if !NOGC_WRITE_BARRIERS && (defined(_TARGET_ARM_) || defined(_TARGET_AMD64_))
                killMask |= (RBM_ARG_0 | RBM_ARG_1);
#endif // !NOGC_WRITE_BARRIERS && (defined(_TARGET_ARM_) || defined(_TARGET_AMD64_))
            }
            break;

#if defined(PROFILING_SUPPORTED) && defined(_TARGET_AMD64_)
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
                ;
            }
            break;
#endif // PROFILING_SUPPORTED && _TARGET_AMD64_

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
        compiler->codeGen->regSet.rsSetRegsModified(killMask DEBUGARG(dumpTerse));

        addRefsForPhysRegMask(killMask, currentLoc, RefTypeKill, true);

        // TODO-CQ: It appears to be valuable for both fp and int registers to avoid killing the callee
        // save regs on infrequently exectued paths.  However, it results in a large number of asmDiffs,
        // many of which appear to be regressions (because there is more spill on the infrequently path),
        // but are not really because the frequent path becomes smaller.  Validating these diffs will need
        // to be done before making this change.
        // if (!blockSequence[curBBSeqNum]->isRunRarely())
        {

            VARSET_ITER_INIT(compiler, iter, currentLiveVars, varIndex);
            while (iter.NextElem(compiler, &varIndex))
            {
                unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
                LclVarDsc* varDsc = compiler->lvaTable + varNum;
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                if (varDsc->lvType == LargeVectorType)
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
                Interval* interval = getIntervalForLocalVar(varNum);
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

        if (tree->IsCall() && (tree->gtFlags & GTF_CALL_UNMANAGED) != 0)
        {
            RefPosition* pos = newRefPosition((Interval*)nullptr, currentLoc, RefTypeKillGCRefs, tree,
                                              (allRegs(TYP_REF) & ~RBM_ARG_REGS));
        }
        return true;
    }

    return false;
}

RefPosition* LinearScan::defineNewInternalTemp(GenTree*     tree,
                                               RegisterType regType,
                                               LsraLocation currentLoc,
                                               regMaskTP    regMask)
{
    Interval* current   = newInterval(regType);
    current->isInternal = true;
    return newRefPosition(current, currentLoc, RefTypeDef, tree, regMask);
}

int LinearScan::buildInternalRegisterDefsForNode(GenTree*     tree,
                                                 LsraLocation currentLoc,
                                                 RefPosition* temps[]) // populates
{
    int       count;
    int       internalIntCount = tree->gtLsraInfo.internalIntCount;
    regMaskTP internalCands    = tree->gtLsraInfo.getInternalCandidates(this);

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
        temps[count] = defineNewInternalTemp(tree, IntRegisterType, currentLoc, internalIntCands);
    }

    int internalFloatCount = tree->gtLsraInfo.internalFloatCount;
    for (int i = 0; i < internalFloatCount; i++)
    {
        regMaskTP internalFPCands = (internalCands & internalFloatRegCandidates());
        temps[count++]            = defineNewInternalTemp(tree, FloatRegisterType, currentLoc, internalFPCands);
    }

    noway_assert(count < MaxInternalRegisters);
    assert(count == (internalIntCount + internalFloatCount));
    return count;
}

void LinearScan::buildInternalRegisterUsesForNode(GenTree*     tree,
                                                  LsraLocation currentLoc,
                                                  RefPosition* defs[],
                                                  int          total)
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
            RefPosition* newest = newRefPosition(defs[i]->getInterval(), currentLoc, RefTypeUse, tree, mask);
            newest->lastUse     = true;
        }
    }
}

regMaskTP LinearScan::getUseCandidates(GenTree* useNode)
{
    TreeNodeInfo info = useNode->gtLsraInfo;
    return info.getSrcCandidates(this);
}

regMaskTP LinearScan::getDefCandidates(GenTree* tree)
{
    TreeNodeInfo info = tree->gtLsraInfo;
    return info.getDstCandidates(this);
}

RegisterType LinearScan::getDefType(GenTree* tree)
{
    return tree->TypeGet();
}

regMaskTP fixedCandidateMask(var_types type, regMaskTP candidates)
{
    if (genMaxOneBit(candidates))
    {
        return candidates;
    }
    return RBM_NONE;
}

//------------------------------------------------------------------------
// LocationInfoListNode: used to store a single `LocationInfo` value for a
//                       node during `buildIntervals`.
//
// This is the node type for `LocationInfoList` below.
//
class LocationInfoListNode final : public LocationInfo
{
    friend class LocationInfoList;
    friend class LocationInfoListNodePool;

    LocationInfoListNode* m_next; // The next node in the list

public:
    LocationInfoListNode(LsraLocation l, Interval* i, GenTree* t, unsigned regIdx = 0) : LocationInfo(l, i, t, regIdx)
    {
    }

    //------------------------------------------------------------------------
    // LocationInfoListNode::Next: Returns the next node in the list.
    LocationInfoListNode* Next() const
    {
        return m_next;
    }
};

//------------------------------------------------------------------------
// LocationInfoList: used to store a list of `LocationInfo` values for a
//                   node during `buildIntervals`.
//
// Given an IR node that either directly defines N registers or that is a
// contained node with uses that define a total of N registers, that node
// will map to N `LocationInfo` values. These values are stored as a
// linked list of `LocationInfoListNode` values.
//
class LocationInfoList final
{
    friend class LocationInfoListNodePool;

    LocationInfoListNode* m_head; // The head of the list
    LocationInfoListNode* m_tail; // The tail of the list

public:
    LocationInfoList() : m_head(nullptr), m_tail(nullptr)
    {
    }

    LocationInfoList(LocationInfoListNode* node) : m_head(node), m_tail(node)
    {
        assert(m_head->m_next == nullptr);
    }

    //------------------------------------------------------------------------
    // LocationInfoList::IsEmpty: Returns true if the list is empty.
    //
    bool IsEmpty() const
    {
        return m_head == nullptr;
    }

    //------------------------------------------------------------------------
    // LocationInfoList::Begin: Returns the first node in the list.
    //
    LocationInfoListNode* Begin() const
    {
        return m_head;
    }

    //------------------------------------------------------------------------
    // LocationInfoList::End: Returns the position after the last node in the
    //                        list. The returned value is suitable for use as
    //                        a sentinel for iteration.
    //
    LocationInfoListNode* End() const
    {
        return nullptr;
    }

    //------------------------------------------------------------------------
    // LocationInfoList::Append: Appends a node to the list.
    //
    // Arguments:
    //    node - The node to append. Must not be part of an existing list.
    //
    void Append(LocationInfoListNode* node)
    {
        assert(node->m_next == nullptr);

        if (m_tail == nullptr)
        {
            assert(m_head == nullptr);
            m_head = node;
        }
        else
        {
            m_tail->m_next = node;
        }

        m_tail = node;
    }

    //------------------------------------------------------------------------
    // LocationInfoList::Append: Appends another list to this list.
    //
    // Arguments:
    //    other - The list to append.
    //
    void Append(LocationInfoList other)
    {
        if (m_tail == nullptr)
        {
            assert(m_head == nullptr);
            m_head = other.m_head;
        }
        else
        {
            m_tail->m_next = other.m_head;
        }

        m_tail = other.m_tail;
    }
};

//------------------------------------------------------------------------
// LocationInfoListNodePool: manages a pool of `LocationInfoListNode`
//                           values to decrease overall memory usage
//                           during `buildIntervals`.
//
// `buildIntervals` involves creating a list of location info values per
// node that either directly produces a set of registers or that is a
// contained node with register-producing sources. However, these lists
// are short-lived: they are destroyed once the use of the corresponding
// node is processed. As such, there is typically only a small number of
// `LocationInfoListNode` values in use at any given time. Pooling these
// values avoids otherwise frequent allocations.
class LocationInfoListNodePool final
{
    LocationInfoListNode* m_freeList;
    Compiler*             m_compiler;

public:
    //------------------------------------------------------------------------
    // LocationInfoListNodePool::LocationInfoListNodePool:
    //    Creates a pool of `LocationInfoListNode` values.
    //
    // Arguments:
    //    compiler    - The compiler context.
    //    preallocate - The number of nodes to preallocate.
    //
    LocationInfoListNodePool(Compiler* compiler, unsigned preallocate = 0) : m_compiler(compiler)
    {
        if (preallocate > 0)
        {
            size_t preallocateSize   = sizeof(LocationInfoListNode) * preallocate;
            auto*  preallocatedNodes = reinterpret_cast<LocationInfoListNode*>(compiler->compGetMem(preallocateSize));

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
    LocationInfoListNode* GetNode(LsraLocation l, Interval* i, GenTree* t, unsigned regIdx = 0)
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

        head->loc         = l;
        head->interval    = i;
        head->treeNode    = t;
        head->multiRegIdx = regIdx;
        head->m_next      = nullptr;

        return head;
    }

    //------------------------------------------------------------------------
    // LocationInfoListNodePool::ReturnNodes: Returns a list of nodes to the
    //                                        pool.
    //
    // Arguments:
    //    list - The list to return.
    //
    void ReturnNodes(LocationInfoList& list)
    {
        assert(list.m_head != nullptr);
        assert(list.m_tail != nullptr);

        LocationInfoListNode* head = m_freeList;
        list.m_tail->m_next        = head;
        m_freeList                 = list.m_head;
    }
};

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
VARSET_VALRET_TP
LinearScan::buildUpperVectorSaveRefPositions(GenTree* tree, LsraLocation currentLoc)
{
    VARSET_TP VARSET_INIT_NOCOPY(liveLargeVectors, VarSetOps::MakeEmpty(compiler));
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
            VARSET_ITER_INIT(compiler, iter, liveLargeVectors, varIndex);
            while (iter.NextElem(compiler, &varIndex))
            {
                unsigned  varNum         = compiler->lvaTrackedToVarNum[varIndex];
                Interval* varInterval    = getIntervalForLocalVar(varNum);
                Interval* tempInterval   = newInterval(LargeVectorType);
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

void LinearScan::buildUpperVectorRestoreRefPositions(GenTree*         tree,
                                                     LsraLocation     currentLoc,
                                                     VARSET_VALARG_TP liveLargeVectors)
{
    if (!VarSetOps::IsEmpty(compiler, liveLargeVectors))
    {
        VARSET_ITER_INIT(compiler, iter, liveLargeVectors, varIndex);
        while (iter.NextElem(compiler, &varIndex))
        {
            unsigned  varNum       = compiler->lvaTrackedToVarNum[varIndex];
            Interval* varInterval  = getIntervalForLocalVar(varNum);
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
static int ComputeOperandDstCount(GenTree* operand)
{
    TreeNodeInfo& operandInfo = operand->gtLsraInfo;

    if (operandInfo.isLocalDefUse)
    {
        // Operands that define an unused value do not produce any registers.
        return 0;
    }
    else if (operandInfo.dstCount != 0)
    {
        // Operands that have a specified number of destination registers consume all of their operands
        // and therefore produce exactly that number of registers.
        return operandInfo.dstCount;
    }
    else if (operandInfo.srcCount != 0)
    {
        // If an operand has no destination registers but does have source registers, it must be a store
        // or a compare.
        assert(operand->OperIsStore() ||
               operand->OperIsBlkOp() ||
               operand->OperIsPutArgStk() ||
               operand->OperIsCompare());
        return 0;
    }
    else if (operand->OperIsStore() || operand->TypeGet() == TYP_VOID)
    {
        // Stores and void-typed operands may be encountered when processing call nodes, which contain
        // pointers to argument setup stores.
        return 0;
    }
    else
    {
        // If a non-void-types operand is not an unsued value and does not have source registers, that
        // argument is contained within its parent and produces `sum(operand_dst_count)` registers.
        int dstCount = 0;
        for (GenTree* op : operand->Operands(true))
        {
            dstCount += ComputeOperandDstCount(op);
        }

        return dstCount;
    }
}

//------------------------------------------------------------------------
// ComputeAvailableSrcCount: computes the number of registers available as
//                           sources for a node.
//
// This is simply the sum of the number of registers prduced by each
// operand to the node.
//
// Arguments:
//    node - The node for which to compute a source count.
//
// Retures:
//    The number of registers available as sources for `node`.
//
static int ComputeAvailableSrcCount(GenTree* node)
{
    int numSources = 0;
    for (GenTree* operand : node->Operands(true))
    {
        numSources += ComputeOperandDstCount(operand);
    }

    return numSources;
}
#endif

void LinearScan::buildRefPositionsForNode(GenTree*                  tree,
                                          BasicBlock*               block,
                                          LocationInfoListNodePool& listNodePool,
                                          HashTableBase<GenTree*, LocationInfoList>& operandToLocationInfoMap,
                                          LsraLocation currentLoc)
{
#ifdef _TARGET_ARM_
    assert(!isRegPairType(tree->TypeGet()));
#endif // _TARGET_ARM_

    // The tree traversal doesn't visit GT_LIST or GT_ARGPLACE nodes
    if (tree->OperGet() == GT_LIST || tree->OperGet() == GT_ARGPLACE)
    {
        return;
    }

    // These nodes are eliminated by the Rationalizer.
    if (tree->OperGet() == GT_CLS_VAR)
    {
        JITDUMP("Unexpected node %s in LSRA.\n", GenTree::NodeName(tree->OperGet()));
        assert(!"Unexpected node in LSRA.");
    }

    // The set of internal temporary registers used by this node are stored in the
    // gtRsvdRegs register mask. Clear it out.
    tree->gtRsvdRegs = RBM_NONE;

#ifdef DEBUG
    if (VERBOSE)
    {
        JITDUMP("at start of tree, map contains: { ");
        bool first = true;
        for (auto kvp : operandToLocationInfoMap)
        {
            GenTree*         node    = kvp.Key();
            LocationInfoList defList = kvp.Value();

            JITDUMP("%sN%03u. %s -> (", first ? "" : "; ", node->gtSeqNum, GenTree::NodeName(node->OperGet()));
            for (LocationInfoListNode *def = defList.Begin(), *end = defList.End(); def != end; def = def->Next())
            {
                JITDUMP("%s%d.N%03u", def == defList.Begin() ? "" : ", ", def->loc, def->treeNode->gtSeqNum);
            }
            JITDUMP(")");

            first = false;
        }
        JITDUMP(" }\n");
    }
#endif // DEBUG

    TreeNodeInfo info = tree->gtLsraInfo;
    assert(info.IsValid(this));
    int consume = info.srcCount;
    int produce = info.dstCount;

    assert(((consume == 0) && (produce == 0)) || (ComputeAvailableSrcCount(tree) == consume));

    if (isCandidateLocalRef(tree) && !tree->OperIsLocalStore())
    {
        assert(consume == 0);

        // We handle tracked variables differently from non-tracked ones.  If it is tracked,
        // we simply add a use or def of the tracked variable.  Otherwise, for a use we need
        // to actually add the appropriate references for loading or storing the variable.
        //
        // It won't actually get used or defined until the appropriate ancestor tree node
        // is processed, unless this is marked "isLocalDefUse" because it is a stack-based argument
        // to a call

        Interval* interval        = getIntervalForLocalVar(tree->gtLclVarCommon.gtLclNum);
        regMaskTP candidates      = getUseCandidates(tree);
        regMaskTP fixedAssignment = fixedCandidateMask(tree->TypeGet(), candidates);

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
            VarSetOps::RemoveElemD(compiler, currentLiveVars,
                                   compiler->lvaTable[tree->gtLclVarCommon.gtLclNum].lvVarIndex);
        }

        JITDUMP("t%u (i:%u)\n", currentLoc, interval->intervalIndex);

        if (!info.isLocalDefUse)
        {
            if (produce != 0)
            {
                LocationInfoList list(listNodePool.GetNode(currentLoc, interval, tree));
                bool             added = operandToLocationInfoMap.AddOrUpdate(tree, list);
                assert(added);

                tree->gtLsraInfo.definesAnyRegisters = true;
            }

            return;
        }
        else
        {
            JITDUMP("    Not added to map\n");
            regMaskTP candidates = getUseCandidates(tree);

            if (fixedAssignment != RBM_NONE)
            {
                candidates = fixedAssignment;
            }
            RefPosition* pos   = newRefPosition(interval, currentLoc, RefTypeUse, tree, candidates);
            pos->isLocalDefUse = true;
            bool isLastUse     = ((tree->gtFlags & GTF_VAR_DEATH) != 0);
            pos->lastUse       = isLastUse;
            pos->setAllocateIfProfitable(tree->IsRegOptional());
            DBEXEC(VERBOSE, pos->dump());
            return;
        }
    }

#ifdef DEBUG
    if (VERBOSE)
    {
        lsraDispNode(tree, LSRA_DUMP_REFPOS, (produce != 0));
        JITDUMP("\n");
        JITDUMP("  consume=%d produce=%d\n", consume, produce);
    }
#endif // DEBUG

    // Handle the case of local variable assignment
    Interval* varDefInterval = nullptr;
    RefType   defRefType     = RefTypeDef;

    GenTree* defNode = tree;

    // noAdd means the node creates a def but for purposes of map
    // management do not add it because data is not flowing up the
    // tree but over (as in ASG nodes)

    bool         noAdd   = info.isLocalDefUse;
    RefPosition* prevPos = nullptr;

    bool isSpecialPutArg = false;

    assert(!tree->OperIsAssignment());
    if (tree->OperIsLocalStore())
    {
        if (isCandidateLocalRef(tree))
        {
            // We always push the tracked lclVar intervals
            varDefInterval = getIntervalForLocalVar(tree->gtLclVarCommon.gtLclNum);
            defRefType     = refTypeForLocalRefNode(tree);
            defNode        = tree;
            if (produce == 0)
            {
                produce = 1;
                noAdd   = true;
            }

            assert(consume <= MAX_RET_REG_COUNT);
            if (consume == 1)
            {
                // Get the location info for the register defined by the first operand.
                LocationInfoList operandDefs;
                bool found = operandToLocationInfoMap.TryGetValue(*(tree->OperandsBegin(true)), &operandDefs);
                assert(found);

                // Since we only expect to consume one register, we should only have a single register to
                // consume.
                assert(operandDefs.Begin()->Next() == operandDefs.End());

                LocationInfo& operandInfo = *static_cast<LocationInfo*>(operandDefs.Begin());

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

                // We can have a case where the source of the store has a different register type,
                // e.g. when the store is of a return value temp, and op1 is a Vector2
                // (TYP_SIMD8).  We will need to set the
                // src candidates accordingly on op1 so that LSRA will generate a copy.
                // We could do this during Lowering, but at that point we don't know whether
                // this lclVar will be a register candidate, and if not, we would prefer to leave
                // the type alone.
                if (regType(tree->gtGetOp1()->TypeGet()) != regType(tree->TypeGet()))
                {
                    tree->gtGetOp1()->gtLsraInfo.setSrcCandidates(this, allRegs(tree->TypeGet()));
                }
            }

            if ((tree->gtFlags & GTF_VAR_DEATH) == 0)
            {
                VarSetOps::AddElemD(compiler, currentLiveVars,
                                    compiler->lvaTable[tree->gtLclVarCommon.gtLclNum].lvVarIndex);
            }
        }
    }
    else if (noAdd && produce == 0)
    {
        // This is the case for dead nodes that occur after
        // tree rationalization
        // TODO-Cleanup: Identify and remove these dead nodes prior to register allocation.
        if (tree->IsMultiRegCall())
        {
            // In case of multi-reg call node, produce = number of return registers
            produce = tree->AsCall()->GetReturnTypeDesc()->GetReturnRegCount();
        }
        else
        {
            produce = 1;
        }
    }

#ifdef DEBUG
    if (VERBOSE)
    {
        if (produce)
        {
            if (varDefInterval != nullptr)
            {
                printf("t%u (i:%u) = op ", currentLoc, varDefInterval->intervalIndex);
            }
            else
            {
                for (int i = 0; i < produce; i++)
                {
                    printf("t%u ", currentLoc);
                }
                printf("= op ");
            }
        }
        else
        {
            printf("     op ");
        }
        printf("\n");
    }
#endif // DEBUG

    Interval* prefSrcInterval = nullptr;

    // If this is a binary operator that will be encoded with 2 operand fields
    // (i.e. the target is read-modify-write), preference the dst to op1.

    bool hasDelayFreeSrc = tree->gtLsraInfo.hasDelayFreeSrc;
    if (tree->OperGet() == GT_PUTARG_REG && isCandidateLocalRef(tree->gtGetOp1()) &&
        (tree->gtGetOp1()->gtFlags & GTF_VAR_DEATH) == 0)
    {
        // This is the case for a "pass-through" copy of a lclVar.  In the case where it is a non-last-use,
        // we don't want the def of the copy to kill the lclVar register, if it is assigned the same register
        // (which is actually what we hope will happen).
        JITDUMP("Setting putarg_reg as a pass-through of a non-last use lclVar\n");

        // Get the register information for the first operand of the node.
        LocationInfoList operandDefs;
        bool             found = operandToLocationInfoMap.TryGetValue(*(tree->OperandsBegin(true)), &operandDefs);
        assert(found);

        // Preference the destination to the interval of the first register defined by the first operand.
        Interval* srcInterval = operandDefs.Begin()->interval;
        assert(srcInterval->isLocalVar);
        prefSrcInterval = srcInterval;
        isSpecialPutArg = true;
    }

    RefPosition* internalRefs[MaxInternalRegisters];

    // make intervals for all the 'internal' register requirements for this node
    // where internal means additional registers required temporarily
    int internalCount = buildInternalRegisterDefsForNode(tree, currentLoc, internalRefs);

    // pop all ref'd tree temps
    GenTreeOperandIterator iterator = tree->OperandsBegin(true);

    // `operandDefs` holds the list of `LocationInfo` values for the registers defined by the current
    // operand. `operandDefsIterator` points to the current `LocationInfo` value in `operandDefs`.
    LocationInfoList      operandDefs;
    LocationInfoListNode* operandDefsIterator = operandDefs.End();
    for (int useIndex = 0; useIndex < consume; useIndex++)
    {
        // If we've consumed all of the registers defined by the current operand, advance to the next
        // operand that defines any registers.
        if (operandDefsIterator == operandDefs.End())
        {
            // Skip operands that do not define any registers, whether directly or indirectly.
            GenTree* operand;
            do
            {
                assert(iterator != tree->OperandsEnd());
                operand = *iterator;

                ++iterator;
            } while (!operand->gtLsraInfo.definesAnyRegisters);

            // If we have already processed a previous operand, return its `LocationInfo` list to the
            // pool.
            if (useIndex > 0)
            {
                assert(!operandDefs.IsEmpty());
                listNodePool.ReturnNodes(operandDefs);
            }

            // Remove the list of registers defined by the current operand from the map. Note that this
            // is only correct because tree nodes are singly-used: if this property ever changes (e.g.
            // if tree nodes are eventually allowed to be multiply-used), then the removal is only
            // correct at the last use.
            bool removed = operandToLocationInfoMap.TryRemove(operand, &operandDefs);
            assert(removed);

            // Move the operand def iterator to the `LocationInfo` for the first register defined by the
            // current operand.
            operandDefsIterator = operandDefs.Begin();
            assert(operandDefsIterator != operandDefs.End());
        }

        LocationInfo& locInfo = *static_cast<LocationInfo*>(operandDefsIterator);
        operandDefsIterator   = operandDefsIterator->Next();

        JITDUMP("t%u ", locInfo.loc);

        // for interstitial tree temps, a use is always last and end;
        // this is  set by default in newRefPosition
        GenTree* useNode = locInfo.treeNode;
        assert(useNode != nullptr);
        var_types type        = useNode->TypeGet();
        regMaskTP candidates  = getUseCandidates(useNode);
        Interval* i           = locInfo.interval;
        unsigned  multiRegIdx = locInfo.multiRegIdx;

#ifdef FEATURE_SIMD
        // In case of multi-reg call store to a local, there won't be any mismatch of
        // use candidates with the type of the tree node.
        if (tree->OperIsLocalStore() && varDefInterval == nullptr && !useNode->IsMultiRegCall())
        {
            // This is a non-candidate store.  If this is a SIMD type, the use candidates
            // may not match the type of the tree node.  If that is the case, change the
            // type of the tree node to match, so that we do the right kind of store.
            if ((candidates & allRegs(tree->gtType)) == RBM_NONE)
            {
                noway_assert((candidates & allRegs(useNode->gtType)) != RBM_NONE);
                // Currently, the only case where this should happen is for a TYP_LONG
                // source and a TYP_SIMD8 target.
                assert((useNode->gtType == TYP_LONG && tree->gtType == TYP_SIMD8) ||
                       (useNode->gtType == TYP_SIMD8 && tree->gtType == TYP_LONG));
                tree->gtType = useNode->gtType;
            }
        }
#endif // FEATURE_SIMD

        bool delayRegFree = (hasDelayFreeSrc && useNode->gtLsraInfo.isDelayFree);
        if (useNode->gtLsraInfo.isTgtPref)
        {
            prefSrcInterval = i;
        }

        bool regOptionalAtUse = useNode->IsRegOptional();
        bool isLastUse        = true;
        if (isCandidateLocalRef(useNode))
        {
            isLastUse = ((useNode->gtFlags & GTF_VAR_DEATH) != 0);
        }
        else
        {
            // For non-localVar uses we record nothing,
            // as nothing needs to be written back to the tree.
            useNode = nullptr;
        }

        regMaskTP fixedAssignment = fixedCandidateMask(type, candidates);
        if (fixedAssignment != RBM_NONE)
        {
            candidates = fixedAssignment;
        }

        RefPosition* pos;
        if ((candidates & allRegs(i->registerType)) == 0)
        {
            // This should only occur where we've got a type mismatch due to SIMD
            // pointer-size types that are passed & returned as longs.
            i->hasConflictingDefUse = true;
            if (fixedAssignment != RBM_NONE)
            {
                // Explicitly insert a FixedRefPosition and fake the candidates, because otherwise newRefPosition
                // will complain about the types not matching.
                regNumber    physicalReg = genRegNumFromMask(fixedAssignment);
                RefPosition* pos = newRefPosition(physicalReg, currentLoc, RefTypeFixedReg, nullptr, fixedAssignment);
            }
            pos = newRefPosition(i, currentLoc, RefTypeUse, useNode, allRegs(i->registerType), multiRegIdx);
            pos->registerAssignment = candidates;
        }
        else
        {
            pos = newRefPosition(i, currentLoc, RefTypeUse, useNode, candidates, multiRegIdx);
        }
        if (delayRegFree)
        {
            hasDelayFreeSrc   = true;
            pos->delayRegFree = true;
        }

        if (isLastUse)
        {
            pos->lastUse = true;
        }

        if (regOptionalAtUse)
        {
            pos->setAllocateIfProfitable(1);
        }
    }
    JITDUMP("\n");

    if (!operandDefs.IsEmpty())
    {
        listNodePool.ReturnNodes(operandDefs);
    }

    buildInternalRegisterUsesForNode(tree, currentLoc, internalRefs, internalCount);

    RegisterType registerType  = getDefType(tree);
    regMaskTP    candidates    = getDefCandidates(tree);
    regMaskTP    useCandidates = getUseCandidates(tree);

#ifdef DEBUG
    if (VERBOSE)
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
#elif defined(_TARGET_ARM_)
    assert(!varTypeIsMultiReg(tree->TypeGet()));
#endif // _TARGET_xxx_

    // Add kill positions before adding def positions
    buildKillPositionsForNode(tree, currentLoc + 1);

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    VARSET_TP VARSET_INIT_NOCOPY(liveLargeVectors, VarSetOps::UninitVal());
    if (RBM_FLT_CALLEE_SAVED != RBM_NONE)
    {
        // Build RefPositions for saving any live large vectors.
        // This must be done after the kills, so that we know which large vectors are still live.
        VarSetOps::AssignNoCopy(compiler, liveLargeVectors, buildUpperVectorSaveRefPositions(tree, currentLoc));
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
    for (int i = 0; i < produce; i++)
    {
        regMaskTP currCandidates = candidates;
        Interval* interval       = varDefInterval;

        // In case of multi-reg call node, registerType is given by
        // the type of ith position return register.
        if (isMultiRegCall)
        {
            registerType   = retTypeDesc->GetReturnRegType((unsigned)i);
            currCandidates = genRegMask(retTypeDesc->GetABIReturnReg(i));
            useCandidates  = allRegs(registerType);
        }

        if (interval == nullptr)
        {
            // Make a new interval
            interval = newInterval(registerType);
            if (hasDelayFreeSrc)
            {
                interval->hasNonCommutativeRMWDef = true;
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
            locationInfoList.Append(listNodePool.GetNode(defLocation, interval, tree, (unsigned)i));
        }

        RefPosition* pos = newRefPosition(interval, defLocation, defRefType, defNode, currCandidates, (unsigned)i);
        if (info.isLocalDefUse)
        {
            pos->isLocalDefUse = true;
            pos->lastUse       = true;
        }
        DBEXEC(VERBOSE, pos->dump());
        interval->updateRegisterPreferences(currCandidates);
        interval->updateRegisterPreferences(useCandidates);
    }

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
    buildUpperVectorRestoreRefPositions(tree, currentLoc, liveLargeVectors);
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

    bool isContainedNode =
        !noAdd && consume == 0 && produce == 0 && tree->TypeGet() != TYP_VOID && !tree->OperIsStore();
    if (isContainedNode)
    {
        // Contained nodes map to the concatenated lists of their operands.
        for (GenTree* op : tree->Operands(true))
        {
            if (!op->gtLsraInfo.definesAnyRegisters)
            {
                assert(ComputeOperandDstCount(op) == 0);
                continue;
            }

            LocationInfoList operandList;
            bool             removed = operandToLocationInfoMap.TryRemove(op, &operandList);
            assert(removed);

            locationInfoList.Append(operandList);
        }
    }

    if (!locationInfoList.IsEmpty())
    {
        bool added = operandToLocationInfoMap.AddOrUpdate(tree, locationInfoList);
        assert(added);
        tree->gtLsraInfo.definesAnyRegisters = true;
    }
}

// make an interval for each physical register
void LinearScan::buildPhysRegRecords()
{
    RegisterType regType = IntRegisterType;
    for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
    {
        RegRecord* curr = &physRegs[reg];
        curr->init(reg);
    }
}

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

void LinearScan::insertZeroInitRefPositions()
{
    // insert defs for this, then a block boundary

    VARSET_ITER_INIT(compiler, iter, compiler->fgFirstBB->bbLiveIn, varIndex);
    while (iter.NextElem(compiler, &varIndex))
    {
        unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
        LclVarDsc* varDsc = compiler->lvaTable + varNum;
        if (!varDsc->lvIsParam && isCandidateVar(varDsc) &&
            (compiler->info.compInitMem || varTypeIsGC(varDsc->TypeGet())))
        {
            GenTree* firstNode = getNonEmptyBlock(compiler->fgFirstBB)->firstNode();
            JITDUMP("V%02u was live in\n", varNum);
            Interval*    interval = getIntervalForLocalVar(varNum);
            RefPosition* pos =
                newRefPosition(interval, MinLocation, RefTypeZeroInit, firstNode, allRegs(interval->registerType));
            varDsc->lvMustInit = true;
        }
    }
}

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
// -----------------------------------------------------------------------
// Sets the register state for an argument of type STRUCT for System V systems.
//     See Compiler::raUpdateRegStateForArg(RegState *regState, LclVarDsc *argDsc) in regalloc.cpp
//         for how state for argument is updated for unix non-structs and Windows AMD64 structs.
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
                        );

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
#ifdef DEBUG
    assert(*pPredBlockIsAllocated == false);
    if (getLsraBlockBoundaryLocations() == LSRA_BLOCK_BOUNDARY_LAYOUT)
    {
        if (prevBlock != nullptr)
        {
            predBlock = prevBlock;
        }
    }
    else
#endif // DEBUG
        if (block != compiler->fgFirstBB)
    {
        predBlock = block->GetUniquePred(compiler);
        if (predBlock != nullptr)
        {
            if (isBlockVisited(predBlock))
            {
                if (predBlock->bbJumpKind == BBJ_COND)
                {
                    // Special handling to improve matching on backedges.
                    BasicBlock* otherBlock = (block == predBlock->bbNext) ? predBlock->bbJumpDest : predBlock->bbNext;
                    noway_assert(otherBlock != nullptr);
                    if (isBlockVisited(otherBlock))
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
                        for (flowList* pred = otherBlock->bbPreds; pred != nullptr; pred = pred->flNext)
                        {
                            BasicBlock* otherPred = pred->flBlock;
                            if (otherPred->bbNum == blockInfo[otherBlock->bbNum].predBBNum)
                            {
                                predBlock = otherPred;
                                break;
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
                BasicBlock* candidatePredBlock = pred->flBlock;
                if (isBlockVisited(candidatePredBlock))
                {
                    if (predBlock == nullptr || predBlock->bbWeight < candidatePredBlock->bbWeight)
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

void LinearScan::buildIntervals()
{
    BasicBlock* block;

    // start numbering at 1; 0 is the entry
    LsraLocation currentLoc = 1;

    JITDUMP("\nbuildIntervals ========\n");

    // Now build (empty) records for all of the physical registers
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

    identifyCandidates();

    DBEXEC(VERBOSE, TupleStyleDump(LSRA_DUMP_PRE));

    // second part:
    JITDUMP("\nbuildIntervals second part ========\n");
    currentLoc = 0;

    // Next, create ParamDef RefPositions for all the tracked parameters,
    // in order of their varIndex

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
            Interval* interval = getIntervalForLocalVar(lclNum);
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
                    Interval*    interval = getIntervalForLocalVar(fieldVarNum);
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

    LocationInfoListNodePool listNodePool(compiler, 8);
    SmallHashTable<GenTree*, LocationInfoList, 32> operandToLocationInfoMap(compiler);

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

        if (block == compiler->fgFirstBB)
        {
            insertZeroInitRefPositions();
        }

        // Determine if we need any DummyDefs.
        // We need DummyDefs for cases where "predBlock" isn't really a predecessor.
        // Note that it's possible to have uses of unitialized variables, in which case even the first
        // block may require DummyDefs, which we are not currently adding - this means that these variables
        // will always be considered to be in memory on entry (and reloaded when the use is encountered).
        // TODO-CQ: Consider how best to tune this.  Currently, if we create DummyDefs for uninitialized
        // variables (which may actually be initialized along the dynamically executed paths, but not
        // on all static paths), we wind up with excessive liveranges for some of these variables.
        VARSET_TP VARSET_INIT(compiler, newLiveIn, block->bbLiveIn);
        if (predBlock)
        {
            JITDUMP("\n\nSetting incoming variable registers of BB%02u to outVarToRegMap of BB%02u\n", block->bbNum,
                    predBlock->bbNum);
            assert(predBlock->bbNum <= bbNumMaxBeforeResolution);
            blockInfo[block->bbNum].predBBNum = predBlock->bbNum;
            // Compute set difference: newLiveIn = block->bbLiveIn - predBlock->bbLiveOut
            VarSetOps::DiffD(compiler, newLiveIn, predBlock->bbLiveOut);
        }
        bool needsDummyDefs = (!VarSetOps::IsEmpty(compiler, newLiveIn) && block != compiler->fgFirstBB);

        // Create dummy def RefPositions

        if (needsDummyDefs)
        {
            // If we are using locations from a predecessor, we should never require DummyDefs.
            assert(!predBlockIsAllocated);

            JITDUMP("Creating dummy definitions\n");
            VARSET_ITER_INIT(compiler, iter, newLiveIn, varIndex);
            while (iter.NextElem(compiler, &varIndex))
            {
                unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
                LclVarDsc* varDsc = compiler->lvaTable + varNum;
                // Add a dummyDef for any candidate vars that are in the "newLiveIn" set.
                // If this is the entry block, don't add any incoming parameters (they're handled with ParamDefs).
                if (isCandidateVar(varDsc) && (predBlock != nullptr || !varDsc->lvIsParam))
                {
                    Interval*    interval = getIntervalForLocalVar(varNum);
                    RefPosition* pos =
                        newRefPosition(interval, currentLoc, RefTypeDummyDef, nullptr, allRegs(interval->registerType));
                }
            }
            JITDUMP("Finished creating dummy definitions\n\n");
        }

        // Add a dummy RefPosition to mark the block boundary.
        // Note that we do this AFTER adding the exposed uses above, because the
        // register positions for those exposed uses need to be recorded at
        // this point.

        RefPosition* pos = newRefPosition((Interval*)nullptr, currentLoc, RefTypeBB, nullptr, RBM_NONE);

        VarSetOps::Assign(compiler, currentLiveVars, block->bbLiveIn);

        LIR::Range& blockRange = LIR::AsRange(block);
        for (GenTree* node : blockRange.NonPhiNodes())
        {
            assert(node->gtLsraInfo.loc >= currentLoc);
            assert(((node->gtLIRFlags & LIR::Flags::IsUnusedValue) == 0) || node->gtLsraInfo.isLocalDefUse);

            currentLoc = node->gtLsraInfo.loc;
            buildRefPositionsForNode(node, block, listNodePool, operandToLocationInfoMap, currentLoc);

#ifdef DEBUG
            if (currentLoc > maxNodeLocation)
            {
                maxNodeLocation = currentLoc;
            }
#endif // DEBUG
        }

        // Increment the LsraLocation at this point, so that the dummy RefPositions
        // will not have the same LsraLocation as any "real" RefPosition.
        currentLoc += 2;

        // Note: the visited set is cleared in LinearScan::doLinearScan()
        markBlockVisited(block);

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

        VARSET_TP   VARSET_INIT(compiler, expUseSet, block->bbLiveOut);
        BasicBlock* nextBlock = getNextBlock();
        if (nextBlock != nullptr)
        {
            VarSetOps::DiffD(compiler, expUseSet, nextBlock->bbLiveIn);
        }
        AllSuccessorIter succsEnd = block->GetAllSuccs(compiler).end();
        for (AllSuccessorIter succs = block->GetAllSuccs(compiler).begin();
             succs != succsEnd && !VarSetOps::IsEmpty(compiler, expUseSet); ++succs)
        {
            BasicBlock* succ = (*succs);
            if (isBlockVisited(succ))
            {
                continue;
            }
            VarSetOps::DiffD(compiler, expUseSet, succ->bbLiveIn);
        }

        if (!VarSetOps::IsEmpty(compiler, expUseSet))
        {
            JITDUMP("Exposed uses:");
            VARSET_ITER_INIT(compiler, iter, expUseSet, varIndex);
            while (iter.NextElem(compiler, &varIndex))
            {
                unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
                LclVarDsc* varDsc = compiler->lvaTable + varNum;
                if (isCandidateVar(varDsc))
                {
                    Interval*    interval = getIntervalForLocalVar(varNum);
                    RefPosition* pos =
                        newRefPosition(interval, currentLoc, RefTypeExpUse, nullptr, allRegs(interval->registerType));
                    JITDUMP(" V%02u", varNum);
                }
            }
            JITDUMP("\n");
        }

        // Identify the last uses of each variable, except in the case of MinOpts, where all vars
        // are kept live everywhere.

        if (!compiler->opts.MinOpts())
        {
            setLastUses(block);
        }

#ifdef DEBUG
        if (VERBOSE)
        {
            printf("use: ");
            dumpConvertedVarSet(compiler, block->bbVarUse);
            printf("\ndef: ");
            dumpConvertedVarSet(compiler, block->bbVarDef);
            printf("\n");
        }
#endif // DEBUG

        prevBlock = block;
    }

    // If we need to KeepAliveAndReportThis, add a dummy exposed use of it at the end
    if (compiler->lvaKeepAliveAndReportThis())
    {
        unsigned keepAliveVarNum = compiler->info.compThisArg;
        assert(compiler->info.compIsStatic == false);
        if (isCandidateVar(&compiler->lvaTable[keepAliveVarNum]))
        {
            JITDUMP("Adding exposed use of this, for lvaKeepAliveAndReportThis\n");
            Interval*    interval = getIntervalForLocalVar(keepAliveVarNum);
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
                Interval*    interval = getIntervalForLocalVar(lclNum);
                RefPosition* pos =
                    newRefPosition(interval, currentLoc, RefTypeExpUse, nullptr, allRegs(interval->registerType));
            }
        }
    }
#endif // DEBUG

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
void LinearScan::dumpVarRefPositions(const char* title)
{
    printf("\nVAR REFPOSITIONS %s\n", title);

    for (unsigned i = 0; i < compiler->lvaCount; i++)
    {
        Interval* interval = getIntervalForLocalVar(i);
        printf("--- V%02u\n", i);

        for (RefPosition* ref = interval->firstRefPosition; ref != nullptr; ref = ref->nextRefPosition)
        {
            ref->dump();
        }
    }

    printf("\n");
}

void LinearScan::validateIntervals()
{
    for (unsigned i = 0; i < compiler->lvaCount; i++)
    {
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
                printf("LocalVar V%02u: undefined use at %u\n", i, ref->nodeLocation);
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
#endif // DEBUG

// Set the default rpFrameType based upon codeGen->isFramePointerRequired()
// This was lifted from the register predictor
//
void LinearScan::setFrameType()
{
    FrameType frameType = FT_NOT_SET;
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

#if DOUBLE_ALIGN
    // The DOUBLE_ALIGN feature indicates whether the JIT will attempt to double-align the
    // frame if needed.  Note that this feature isn't on for amd64, because the stack is
    // always double-aligned by default.
    compiler->codeGen->setDoubleAlign(false);

    // TODO-CQ: Tune this (see regalloc.cpp, in which raCntWtdStkDblStackFP is used to
    // determine whether to double-align). Note, though that there is at least one test
    // (jit\opt\Perf\DoubleAlign\Locals.exe) that depends on double-alignment being set
    // in certain situations.
    if (!compiler->opts.MinOpts() && !compiler->codeGen->isFramePointerRequired() && compiler->compFloatingPointUsed)
    {
        frameType = FT_DOUBLE_ALIGN_FRAME;
    }
#endif // DOUBLE_ALIGN

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
            compiler->codeGen->setDoubleAlign(true);
            break;
#endif // DOUBLE_ALIGN
        default:
            noway_assert(!"rpFrameType not set correctly!");
            break;
    }

    // If we are using FPBASE as the frame register, we cannot also use it for
    // a local var. Note that we may have already added it to the register masks,
    // which are computed when the LinearScan class constructor is created, and
    // used during lowering. Luckily, the TreeNodeInfo only stores an index to
    // the masks stored in the LinearScan class, so we only need to walk the
    // unique masks and remove FPBASE.
    if (frameType == FT_EBP_FRAME)
    {
        if ((availableIntRegs & RBM_FPBASE) != 0)
        {
            RemoveRegisterFromMasks(REG_FPBASE);

            // We know that we're already in "read mode" for availableIntRegs. However,
            // we need to remove the FPBASE register, so subsequent users (like callers
            // to allRegs()) get the right thing. The RemoveRegisterFromMasks() code
            // fixes up everything that already took a dependency on the value that was
            // previously read, so this completes the picture.
            availableIntRegs.OverrideAssign(availableIntRegs & ~RBM_FPBASE);
        }
    }

    compiler->rpFrameType = frameType;
}

// Is the copyReg given by this RefPosition still busy at the
// given location?
bool copyRegInUse(RefPosition* ref, LsraLocation loc)
{
    assert(ref->copyReg);
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

// Determine whether the register represented by "physRegRecord" is available at least
// at the "currentLoc", and if so, return the next location at which it is in use in
// "nextRefLocationPtr"
//
bool LinearScan::registerIsAvailable(RegRecord*    physRegRecord,
                                     LsraLocation  currentLoc,
                                     LsraLocation* nextRefLocationPtr,
                                     RegisterType  regType)
{
    *nextRefLocationPtr          = MaxLocation;
    LsraLocation nextRefLocation = MaxLocation;
    regMaskTP    regMask         = genRegMask(physRegRecord->regNum);
    if (physRegRecord->isBusyUntilNextKill)
    {
        return false;
    }

    RefPosition* nextPhysReference = physRegRecord->getNextRefPosition();
    if (nextPhysReference != nullptr)
    {
        nextRefLocation = nextPhysReference->nodeLocation;
        // if (nextPhysReference->refType == RefTypeFixedReg) nextRefLocation--;
    }
    else if (!physRegRecord->isCalleeSave)
    {
        nextRefLocation = MaxLocation - 1;
    }

    Interval* assignedInterval = physRegRecord->assignedInterval;

    if (assignedInterval != nullptr)
    {
        RefPosition* recentReference = assignedInterval->recentRefPosition;

        // The only case where we have an assignedInterval, but recentReference is null
        // is where this interval is live at procedure entry (i.e. an arg register), in which
        // case it's still live and its assigned register is not available
        // (Note that the ParamDef will be recorded as a recentReference when we encounter
        // it, but we will be allocating registers, potentially to other incoming parameters,
        // as we process the ParamDefs.)

        if (recentReference == nullptr)
        {
            return false;
        }

        // Is this a copyReg?  It is if the register assignment doesn't match.
        // (the recentReference may not be a copyReg, because we could have seen another
        // reference since the copyReg)

        if (!assignedInterval->isAssignedTo(physRegRecord->regNum))
        {
            // Don't reassign it if it's still in use
            if (recentReference->copyReg && copyRegInUse(recentReference, currentLoc))
            {
                return false;
            }
        }
        else if (!assignedInterval->isActive && assignedInterval->isConstant)
        {
            // Treat this as unassigned, i.e. do nothing.
            // TODO-CQ: Consider adjusting the heuristics (probably in the caller of this method)
            // to avoid reusing these registers.
        }
        // If this interval isn't active, it's available if it isn't referenced
        // at this location (or the previous location, if the recent RefPosition
        // is a delayRegFree).
        else if (!assignedInterval->isActive &&
                 (recentReference->refType == RefTypeExpUse || recentReference->getRefEndLocation() < currentLoc))
        {
            // This interval must have a next reference (otherwise it wouldn't be assigned to this register)
            RefPosition* nextReference = recentReference->nextRefPosition;
            if (nextReference != nullptr)
            {
                if (nextReference->nodeLocation < nextRefLocation)
                {
                    nextRefLocation = nextReference->nodeLocation;
                }
            }
            else
            {
                assert(recentReference->copyReg && recentReference->registerAssignment != regMask);
            }
        }
        else
        {
            return false;
        }
    }
    if (nextRefLocation < *nextRefLocationPtr)
    {
        *nextRefLocationPtr = nextRefLocation;
    }

#ifdef _TARGET_ARM_
    if (regType == TYP_DOUBLE)
    {
        // Recurse, but check the other half this time (TYP_FLOAT)
        if (!registerIsAvailable(getRegisterRecord(REG_NEXT(physRegRecord->regNum)), currentLoc, nextRefLocationPtr,
                                 TYP_FLOAT))
            return false;
        nextRefLocation = *nextRefLocationPtr;
    }
#endif // _TARGET_ARM_

    return (nextRefLocation >= currentLoc);
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
#if defined(FEATURE_SIMD) && defined(_TARGET_AMD64_)
    if ((candidates & allRegs(regType)) == RBM_NONE)
    {
        assert((regType == TYP_SIMD8) && (refPosition->refType == RefTypeUse) &&
               ((candidates & allRegs(TYP_INT)) != RBM_NONE));
        regType = TYP_INT;
    }
#else  // !(defined(FEATURE_SIMD) && defined(_TARGET_AMD64_))
    assert((candidates & allRegs(regType)) != RBM_NONE);
#endif // !(defined(FEATURE_SIMD) && defined(_TARGET_AMD64_))
    return regType;
}

//------------------------------------------------------------------------
// tryAllocateFreeReg: Find a free register that satisfies the requirements for refPosition,
//                     and takes into account the preferences for the given Interval
//
// Arguments:
//    currentInterval: The interval for the current allocation
//    refPosition:     The RefPosition of the current Interval for which a register is being allocated
//
// Return Value:
//    The regNumber, if any, allocated to the RefPositon.  Returns REG_NA if no free register is found.
//
// Notes:
//    TODO-CQ: Consider whether we need to use a different order for tree temps than for vars, as
//    reg predict does

static const regNumber lsraRegOrder[]      = {REG_VAR_ORDER};
const unsigned         lsraRegOrderSize    = ArrLen(lsraRegOrder);
static const regNumber lsraRegOrderFlt[]   = {REG_VAR_ORDER_FLT};
const unsigned         lsraRegOrderFltSize = ArrLen(lsraRegOrderFlt);

regNumber LinearScan::tryAllocateFreeReg(Interval* currentInterval, RefPosition* refPosition)
{
    regNumber foundReg = REG_NA;

    RegisterType     regType = getRegisterType(currentInterval, refPosition);
    const regNumber* regOrder;
    unsigned         regOrderSize;
    if (useFloatReg(regType))
    {
        regOrder     = lsraRegOrderFlt;
        regOrderSize = lsraRegOrderFltSize;
    }
    else
    {
        regOrder     = lsraRegOrder;
        regOrderSize = lsraRegOrderSize;
    }

    LsraLocation currentLocation = refPosition->nodeLocation;
    RefPosition* nextRefPos      = refPosition->nextRefPosition;
    LsraLocation nextLocation    = (nextRefPos == nullptr) ? currentLocation : nextRefPos->nodeLocation;
    regMaskTP    candidates      = refPosition->registerAssignment;
    regMaskTP    preferences     = currentInterval->registerPreferences;

    if (RefTypeIsDef(refPosition->refType))
    {
        if (currentInterval->hasConflictingDefUse)
        {
            resolveConflictingDefAndUse(currentInterval, refPosition);
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
            RegRecord* defRegRecord = getRegisterRecord(defReg);

            RefPosition* currFixedRegRefPosition = defRegRecord->recentRefPosition;
            assert(currFixedRegRefPosition != nullptr &&
                   currFixedRegRefPosition->nodeLocation == refPosition->nodeLocation);

            // If there is another fixed reference to this register before the use, change the candidates
            // on this RefPosition to include that of nextRefPos.
            if (currFixedRegRefPosition->nextRefPosition != nullptr &&
                currFixedRegRefPosition->nextRefPosition->nodeLocation <= nextRefPos->getRefEndLocation())
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
    regMaskTP relatedPreferences = RBM_NONE;

#ifdef DEBUG
    candidates = stressLimitRegs(refPosition, candidates);
#endif
    bool mustAssignARegister = true;
    assert(candidates != RBM_NONE);

    // If the related interval has no further references, it is possible that it is a source of the
    // node that produces this interval.  However, we don't want to use the relatedInterval for preferencing
    // if its next reference is not a new definition (as it either is or will become live).
    Interval* relatedInterval = currentInterval->relatedInterval;
    if (relatedInterval != nullptr)
    {
        RefPosition* nextRelatedRefPosition = relatedInterval->getNextRefPosition();
        if (nextRelatedRefPosition != nullptr)
        {
            // Don't use the relatedInterval for preferencing if its next reference is not a new definition.
            if (!RefTypeIsDef(nextRelatedRefPosition->refType))
            {
                relatedInterval = nullptr;
            }
            // Is the relatedInterval simply a copy to another relatedInterval?
            else if ((relatedInterval->relatedInterval != nullptr) &&
                     (nextRelatedRefPosition->nextRefPosition != nullptr) &&
                     (nextRelatedRefPosition->nextRefPosition->nextRefPosition == nullptr) &&
                     (nextRelatedRefPosition->nextRefPosition->nodeLocation <
                      relatedInterval->relatedInterval->getNextRefLocation()))
            {
                // The current relatedInterval has only two remaining RefPositions, both of which
                // occur prior to the next RefPosition for its relatedInterval.
                // It is likely a copy.
                relatedInterval = relatedInterval->relatedInterval;
            }
        }
    }

    if (relatedInterval != nullptr)
    {
        // If the related interval already has an assigned register, then use that
        // as the related preference.  We'll take the related
        // interval preferences into account in the loop over all the registers.

        if (relatedInterval->assignedReg != nullptr)
        {
            relatedPreferences = genRegMask(relatedInterval->assignedReg->regNum);
        }
        else
        {
            relatedPreferences = relatedInterval->registerPreferences;
        }
    }

    bool preferCalleeSave = currentInterval->preferCalleeSave;

    // For floating point, we want to be less aggressive about using callee-save registers.
    // So in that case, we just need to ensure that the current RefPosition is covered.
    RefPosition* rangeEndRefPosition;
    RefPosition* lastRefPosition = currentInterval->lastRefPosition;
    if (useFloatReg(currentInterval->registerType))
    {
        rangeEndRefPosition = refPosition;
    }
    else
    {
        rangeEndRefPosition = currentInterval->lastRefPosition;
        // If we have a relatedInterval that is not currently occupying a register,
        // and whose lifetime begins after this one ends,
        // we want to try to select a register that will cover its lifetime.
        if ((relatedInterval != nullptr) && (relatedInterval->assignedReg == nullptr) &&
            (relatedInterval->getNextRefLocation() >= rangeEndRefPosition->nodeLocation))
        {
            lastRefPosition  = relatedInterval->lastRefPosition;
            preferCalleeSave = relatedInterval->preferCalleeSave;
        }
    }

    // If this has a delayed use (due to being used in a rmw position of a
    // non-commutative operator), its endLocation is delayed until the "def"
    // position, which is one location past the use (getRefEndLocation() takes care of this).
    LsraLocation rangeEndLocation = rangeEndRefPosition->getRefEndLocation();
    LsraLocation lastLocation     = lastRefPosition->getRefEndLocation();
    regNumber    prevReg          = REG_NA;

    if (currentInterval->assignedReg)
    {
        bool useAssignedReg = false;
        // This was an interval that was previously allocated to the given
        // physical register, and we should try to allocate it to that register
        // again, if possible and reasonable.
        // Use it preemptively (i.e. before checking other available regs)
        // only if it is preferred and available.

        RegRecord* regRec    = currentInterval->assignedReg;
        prevReg              = regRec->regNum;
        regMaskTP prevRegBit = genRegMask(prevReg);

        // Is it in the preferred set of regs?
        if ((prevRegBit & preferences) != RBM_NONE)
        {
            // Is it currently available?
            LsraLocation nextPhysRefLoc;
            if (registerIsAvailable(regRec, currentLocation, &nextPhysRefLoc, currentInterval->registerType))
            {
                // If the register is next referenced at this location, only use it if
                // this has a fixed reg requirement (i.e. this is the reference that caused
                // the FixedReg ref to be created)

                if (!regRec->conflictingFixedRegReference(refPosition))
                {
                    useAssignedReg = true;
                }
            }
        }
        if (useAssignedReg)
        {
            regNumber foundReg = prevReg;
            assignPhysReg(regRec, currentInterval);
            refPosition->registerAssignment = genRegMask(foundReg);
            return foundReg;
        }
        else
        {
            // Don't keep trying to allocate to this register
            currentInterval->assignedReg = nullptr;
        }
    }

    RegRecord* availablePhysRegInterval = nullptr;
    Interval*  intervalToUnassign       = nullptr;

    // Each register will receive a score which is the sum of the scoring criteria below.
    // These were selected on the assumption that they will have an impact on the "goodness"
    // of a register selection, and have been tuned to a certain extent by observing the impact
    // of the ordering on asmDiffs.  However, there is probably much more room for tuning,
    // and perhaps additional criteria.
    //
    // These are FLAGS (bits) so that we can easily order them and add them together.
    // If the scores are equal, but one covers more of the current interval's range,
    // then it wins.  Otherwise, the one encountered earlier in the regOrder wins.

    enum RegisterScore
    {
        VALUE_AVAILABLE = 0x40, // It is a constant value that is already in an acceptable register.
        COVERS          = 0x20, // It is in the interval's preference set and it covers the entire lifetime.
        OWN_PREFERENCE  = 0x10, // It is in the preference set of this interval.
        COVERS_RELATED  = 0x08, // It is in the preference set of the related interval and covers the entire lifetime.
        RELATED_PREFERENCE = 0x04, // It is in the preference set of the related interval.
        CALLER_CALLEE      = 0x02, // It is in the right "set" for the interval (caller or callee-save).
        UNASSIGNED         = 0x01, // It is not currently assigned to an inactive interval.
    };

    int bestScore = 0;

    // Compute the best possible score so we can stop looping early if we find it.
    // TODO-Throughput: At some point we may want to short-circuit the computation of each score, but
    // probably not until we've tuned the order of these criteria.  At that point,
    // we'll need to avoid the short-circuit if we've got a stress option to reverse
    // the selection.
    int bestPossibleScore = COVERS + UNASSIGNED + OWN_PREFERENCE + CALLER_CALLEE;
    if (relatedPreferences != RBM_NONE)
    {
        bestPossibleScore |= RELATED_PREFERENCE + COVERS_RELATED;
    }

    LsraLocation bestLocation = MinLocation;

    // In non-debug builds, this will simply get optimized away
    bool reverseSelect = false;
#ifdef DEBUG
    reverseSelect = doReverseSelect();
#endif // DEBUG

    // An optimization for the common case where there is only one candidate -
    // avoid looping over all the other registers

    regNumber singleReg = REG_NA;

    if (genMaxOneBit(candidates))
    {
        regOrderSize = 1;
        singleReg    = genRegNumFromMask(candidates);
        regOrder     = &singleReg;
    }

    for (unsigned i = 0; i < regOrderSize && (candidates != RBM_NONE); i++)
    {
        regNumber regNum       = regOrder[i];
        regMaskTP candidateBit = genRegMask(regNum);

        if (!(candidates & candidateBit))
        {
            continue;
        }

        candidates &= ~candidateBit;

        RegRecord* physRegRecord = getRegisterRecord(regNum);

        int          score               = 0;
        LsraLocation nextPhysRefLocation = MaxLocation;

        // By chance, is this register already holding this interval, as a copyReg or having
        // been restored as inactive after a kill?
        if (physRegRecord->assignedInterval == currentInterval)
        {
            availablePhysRegInterval = physRegRecord;
            intervalToUnassign       = nullptr;
            break;
        }

        // Find the next RefPosition of the physical register
        if (!registerIsAvailable(physRegRecord, currentLocation, &nextPhysRefLocation, regType))
        {
            continue;
        }

        // If the register is next referenced at this location, only use it if
        // this has a fixed reg requirement (i.e. this is the reference that caused
        // the FixedReg ref to be created)

        if (physRegRecord->conflictingFixedRegReference(refPosition))
        {
            continue;
        }

        // If this is a definition of a constant interval, check to see if its value is already in this register.
        if (currentInterval->isConstant && RefTypeIsDef(refPosition->refType) &&
            (physRegRecord->assignedInterval != nullptr) && physRegRecord->assignedInterval->isConstant)
        {
            noway_assert(refPosition->treeNode != nullptr);
            GenTree* otherTreeNode = physRegRecord->assignedInterval->firstRefPosition->treeNode;
            noway_assert(otherTreeNode != nullptr);

            if (refPosition->treeNode->OperGet() == otherTreeNode->OperGet())
            {
                switch (otherTreeNode->OperGet())
                {
                    case GT_CNS_INT:
                        if ((refPosition->treeNode->AsIntCon()->IconValue() ==
                             otherTreeNode->AsIntCon()->IconValue()) &&
                            (varTypeGCtype(refPosition->treeNode) == varTypeGCtype(otherTreeNode)))
                        {
#ifdef _TARGET_64BIT_
                            // If the constant is negative, only reuse registers of the same type.
                            // This is because, on a 64-bit system, we do not sign-extend immediates in registers to
                            // 64-bits unless they are actually longs, as this requires a longer instruction.
                            // This doesn't apply to a 32-bit system, on which long values occupy multiple registers.
                            // (We could sign-extend, but we would have to always sign-extend, because if we reuse more
                            // than once, we won't have access to the instruction that originally defines the constant).
                            if ((refPosition->treeNode->TypeGet() == otherTreeNode->TypeGet()) ||
                                (refPosition->treeNode->AsIntCon()->IconValue() >= 0))
#endif // _TARGET_64BIT_
                            {
                                score |= VALUE_AVAILABLE;
                            }
                        }
                        break;
                    case GT_CNS_DBL:
                    {
                        // For floating point constants, the values must be identical, not simply compare
                        // equal.  So we compare the bits.
                        if (refPosition->treeNode->AsDblCon()->isBitwiseEqual(otherTreeNode->AsDblCon()) &&
                            (refPosition->treeNode->TypeGet() == otherTreeNode->TypeGet()))
                        {
                            score |= VALUE_AVAILABLE;
                        }
                        break;
                    }
                    default:
                        // for all other 'otherTreeNode->OperGet()' kinds, we leave 'score' unchanged
                        break;
                }
            }
        }

        // If the nextPhysRefLocation is a fixedRef for the rangeEndRefPosition, increment it so that
        // we don't think it isn't covering the live range.
        // This doesn't handle the case where earlier RefPositions for this Interval are also
        // FixedRefs of this regNum, but at least those are only interesting in the case where those
        // are "local last uses" of the Interval - otherwise the liveRange would interfere with the reg.
        if (nextPhysRefLocation == rangeEndLocation && rangeEndRefPosition->isFixedRefOfReg(regNum))
        {
            INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_INCREMENT_RANGE_END, currentInterval, regNum));
            nextPhysRefLocation++;
        }

        if ((candidateBit & preferences) != RBM_NONE)
        {
            score |= OWN_PREFERENCE;
            if (nextPhysRefLocation > rangeEndLocation)
            {
                score |= COVERS;
            }
        }
        if (relatedInterval != nullptr && (candidateBit & relatedPreferences) != RBM_NONE)
        {
            score |= RELATED_PREFERENCE;
            if (nextPhysRefLocation > relatedInterval->lastRefPosition->nodeLocation)
            {
                score |= COVERS_RELATED;
            }
        }

        // If we had a fixed-reg def of a reg that will be killed before the use, prefer it to any other registers
        // with the same score.  (Note that we haven't changed the original registerAssignment on the RefPosition).
        // Overload the RELATED_PREFERENCE value.
        else if (candidateBit == refPosition->registerAssignment)
        {
            score |= RELATED_PREFERENCE;
        }

        if ((preferCalleeSave && physRegRecord->isCalleeSave) || (!preferCalleeSave && !physRegRecord->isCalleeSave))
        {
            score |= CALLER_CALLEE;
        }

        // The register is considered unassigned if it has no assignedInterval, OR
        // if its next reference is beyond the range of this interval.
        if (physRegRecord->assignedInterval == nullptr ||
            physRegRecord->assignedInterval->getNextRefLocation() > lastLocation)
        {
            score |= UNASSIGNED;
        }

        bool foundBetterCandidate = false;

        if (score > bestScore)
        {
            foundBetterCandidate = true;
        }
        else if (score == bestScore)
        {
            // Prefer a register that covers the range.
            if (bestLocation <= lastLocation)
            {
                if (nextPhysRefLocation > bestLocation)
                {
                    foundBetterCandidate = true;
                }
            }
            // If both cover the range, prefer a register that is killed sooner (leaving the longer range register
            // available). If both cover the range and also getting killed at the same location, prefer the one which
            // is same as previous assignment.
            else if (nextPhysRefLocation > lastLocation)
            {
                if (nextPhysRefLocation < bestLocation)
                {
                    foundBetterCandidate = true;
                }
                else if (nextPhysRefLocation == bestLocation && prevReg == regNum)
                {
                    foundBetterCandidate = true;
                }
            }
        }

#ifdef DEBUG
        if (doReverseSelect() && bestScore != 0)
        {
            foundBetterCandidate = !foundBetterCandidate;
        }
#endif // DEBUG

        if (foundBetterCandidate)
        {
            bestLocation             = nextPhysRefLocation;
            availablePhysRegInterval = physRegRecord;
            intervalToUnassign       = physRegRecord->assignedInterval;
            bestScore                = score;
        }

        // there is no way we can get a better score so break out
        if (!reverseSelect && score == bestPossibleScore && bestLocation == rangeEndLocation + 1)
        {
            break;
        }
    }

    if (availablePhysRegInterval != nullptr)
    {
        if (intervalToUnassign != nullptr)
        {
            unassignPhysReg(availablePhysRegInterval, intervalToUnassign->recentRefPosition);
            if (bestScore & VALUE_AVAILABLE)
            {
                assert(intervalToUnassign->isConstant);
                refPosition->treeNode->SetReuseRegVal();
                refPosition->treeNode->SetInReg();
            }
            // If we considered this "unassigned" because this interval's lifetime ends before
            // the next ref, remember it.
            else if ((bestScore & UNASSIGNED) != 0 && intervalToUnassign != nullptr)
            {
                availablePhysRegInterval->previousInterval = intervalToUnassign;
            }
        }
        else
        {
            assert((bestScore & VALUE_AVAILABLE) == 0);
        }
        assignPhysReg(availablePhysRegInterval, currentInterval);
        foundReg                        = availablePhysRegInterval->regNum;
        regMaskTP foundRegMask          = genRegMask(foundReg);
        refPosition->registerAssignment = foundRegMask;
        if (relatedInterval != nullptr)
        {
            relatedInterval->updateRegisterPreferences(foundRegMask);
        }
    }

    return foundReg;
}

//------------------------------------------------------------------------
// allocateBusyReg: Find a busy register that satisfies the requirements for refPosition,
//                  and that can be spilled.
//
// Arguments:
//    current               The interval for the current allocation
//    refPosition           The RefPosition of the current Interval for which a register is being allocated
//    allocateIfProfitable  If true, a reg may not be allocated if all other ref positions currently
//                          occupying registers are more important than the 'refPosition'.
//
// Return Value:
//    The regNumber allocated to the RefPositon.  Returns REG_NA if no free register is found.
//
// Note:  Currently this routine uses weight and farthest distance of next reference
// to select a ref position for spilling.
// a) if allocateIfProfitable = false
//        The ref position chosen for spilling will be the lowest weight
//        of all and if there is is more than one ref position with the
//        same lowest weight, among them choses the one with farthest
//        distance to its next reference.
//
// b) if allocateIfProfitable = true
//        The ref position chosen for spilling will not only be lowest weight
//        of all but also has a weight lower than 'refPosition'.  If there is
//        no such ref position, reg will not be allocated.
regNumber LinearScan::allocateBusyReg(Interval* current, RefPosition* refPosition, bool allocateIfProfitable)
{
    regNumber foundReg = REG_NA;

    RegisterType regType     = getRegisterType(current, refPosition);
    regMaskTP    candidates  = refPosition->registerAssignment;
    regMaskTP    preferences = (current->registerPreferences & candidates);
    if (preferences == RBM_NONE)
    {
        preferences = candidates;
    }
    if (candidates == RBM_NONE)
    {
        // This assumes only integer and floating point register types
        // if we target a processor with additional register types,
        // this would have to change
        candidates = allRegs(regType);
    }

#ifdef DEBUG
    candidates = stressLimitRegs(refPosition, candidates);
#endif // DEBUG

    // TODO-CQ: Determine whether/how to take preferences into account in addition to
    // prefering the one with the furthest ref position when considering
    // a candidate to spill
    RegRecord*   farthestRefPhysRegRecord = nullptr;
    LsraLocation farthestLocation         = MinLocation;
    LsraLocation refLocation              = refPosition->nodeLocation;
    unsigned     farthestRefPosWeight;
    if (allocateIfProfitable)
    {
        // If allocating a reg is optional, we will consider those ref positions
        // whose weight is less than 'refPosition' for spilling.
        farthestRefPosWeight = getWeight(refPosition);
    }
    else
    {
        // If allocating a reg is a must, we start off with max weight so
        // that the first spill candidate will be selected based on
        // farthest distance alone.  Since we start off with farthestLocation
        // initialized to MinLocation, the first available ref position
        // will be selected as spill candidate and its weight as the
        // fathestRefPosWeight.
        farthestRefPosWeight = BB_MAX_WEIGHT;
    }

    for (regNumber regNum : Registers(regType))
    {
        regMaskTP candidateBit = genRegMask(regNum);
        if (!(candidates & candidateBit))
        {
            continue;
        }
        RegRecord* physRegRecord = getRegisterRecord(regNum);

        if (physRegRecord->isBusyUntilNextKill)
        {
            continue;
        }
        Interval* assignedInterval = physRegRecord->assignedInterval;

        // If there is a fixed reference at the same location (and it's not due to this reference),
        // don't use it.

        if (physRegRecord->conflictingFixedRegReference(refPosition))
        {
            assert(candidates != candidateBit);
            continue;
        }

        LsraLocation physRegNextLocation = MaxLocation;
        if (refPosition->isFixedRefOfRegMask(candidateBit))
        {
            // Either there is a fixed reference due to this node, or one associated with a
            // fixed use fed by a def at this node.
            // In either case, we must use this register as it's the only candidate
            // TODO-CQ: At the time we allocate a register to a fixed-reg def, if it's not going
            // to remain live until the use, we should set the candidates to allRegs(regType)
            // to avoid a spill - codegen can then insert the copy.
            assert(candidates == candidateBit);
            physRegNextLocation  = MaxLocation;
            farthestRefPosWeight = BB_MAX_WEIGHT;
        }
        else
        {
            physRegNextLocation = physRegRecord->getNextRefLocation();

            // If refPosition requires a fixed register, we should reject all others.
            // Otherwise, we will still evaluate all phyRegs though their next location is
            // not better than farthestLocation found so far.
            //
            // TODO: this method should be using an approach similar to tryAllocateFreeReg()
            // where it uses a regOrder array to avoid iterating over any but the single
            // fixed candidate.
            if (refPosition->isFixedRegRef && physRegNextLocation < farthestLocation)
            {
                continue;
            }
        }

        // If this register is not assigned to an interval, either
        // - it has a FixedReg reference at the current location that is not this reference, OR
        // - this is the special case of a fixed loReg, where this interval has a use at the same location
        // In either case, we cannot use it

        if (assignedInterval == nullptr)
        {
            RefPosition* nextPhysRegPosition = physRegRecord->getNextRefPosition();

#ifndef _TARGET_ARM64_
            // TODO-Cleanup: Revisit this after Issue #3524 is complete
            // On ARM64 the nodeLocation is not always == refLocation, Disabling this assert for now.
            assert(nextPhysRegPosition->nodeLocation == refLocation && candidateBit != candidates);
#endif
            continue;
        }

        RefPosition* recentAssignedRef = assignedInterval->recentRefPosition;

        if (!assignedInterval->isActive)
        {
            // The assigned interval has a reference at this location - otherwise, we would have found
            // this in tryAllocateFreeReg().
            // Note that we may or may not have actually handled the reference yet, so it could either
            // be recentAssigedRef, or the next reference.
            assert(recentAssignedRef != nullptr);
            if (recentAssignedRef->nodeLocation != refLocation)
            {
                if (recentAssignedRef->nodeLocation + 1 == refLocation)
                {
                    assert(recentAssignedRef->delayRegFree);
                }
                else
                {
                    RefPosition* nextAssignedRef = recentAssignedRef->nextRefPosition;
                    assert(nextAssignedRef != nullptr);
                    assert(nextAssignedRef->nodeLocation == refLocation ||
                           (nextAssignedRef->nodeLocation + 1 == refLocation && nextAssignedRef->delayRegFree));
                }
            }
            continue;
        }

        // If we have a recentAssignedRef, check that it is going to be OK to spill it
        //
        // TODO-Review: Under what conditions recentAssginedRef would be null?
        unsigned recentAssignedRefWeight = BB_ZERO_WEIGHT;
        if (recentAssignedRef != nullptr)
        {
            if (recentAssignedRef->nodeLocation == refLocation)
            {
                // We can't spill a register that's being used at the current location
                RefPosition* physRegRef = physRegRecord->recentRefPosition;
                continue;
            }

            // If the current position has the candidate register marked to be delayed,
            // check if the previous location is using this register, if that's the case we have to skip
            // since we can't spill this register.
            if (recentAssignedRef->delayRegFree && (refLocation == recentAssignedRef->nodeLocation + 1))
            {
                continue;
            }

            // We don't prefer to spill a register if the weight of recentAssignedRef > weight
            // of the spill candidate found so far.  We would consider spilling a greater weight
            // ref position only if the refPosition being allocated must need a reg.
            recentAssignedRefWeight = getWeight(recentAssignedRef);
            if (recentAssignedRefWeight > farthestRefPosWeight)
            {
                continue;
            }
        }

        LsraLocation nextLocation = assignedInterval->getNextRefLocation();

        // We should never spill a register that's occupied by an Interval with its next use at the current location.
        // Normally this won't occur (unless we actually had more uses in a single node than there are registers),
        // because we'll always find something with a later nextLocation, but it can happen in stress when
        // we have LSRA_SELECT_NEAREST.
        if ((nextLocation == refLocation) && !refPosition->isFixedRegRef)
        {
            continue;
        }

        if (nextLocation > physRegNextLocation)
        {
            nextLocation = physRegNextLocation;
        }

        bool isBetterLocation;

#ifdef DEBUG
        if (doSelectNearest() && farthestRefPhysRegRecord != nullptr)
        {
            isBetterLocation = (nextLocation <= farthestLocation);
        }
        else
#endif
            // This if-stmt is associated with the above else
            if (recentAssignedRefWeight < farthestRefPosWeight)
        {
            isBetterLocation = true;
        }
        else
        {
            // This would mean the weight of spill ref position we found so far is equal
            // to the weight of the ref position that is being evaluated.  In this case
            // we prefer to spill ref position whose distance to its next reference is
            // the farthest.
            assert(recentAssignedRefWeight == farthestRefPosWeight);

            // If allocateIfProfitable=true, the first spill candidate selected
            // will be based on weight alone. After we have found a spill
            // candidate whose weight is less than the 'refPosition', we will
            // consider farthest distance when there is a tie in weights.
            // This is to ensure that we don't spill a ref position whose
            // weight is equal to weight of 'refPosition'.
            if (allocateIfProfitable && farthestRefPhysRegRecord == nullptr)
            {
                isBetterLocation = false;
            }
            else
            {
                isBetterLocation = (nextLocation > farthestLocation);

                if (nextLocation > farthestLocation)
                {
                    isBetterLocation = true;
                }
                else if (nextLocation == farthestLocation)
                {
                    // Both weight and distance are equal.
                    // Prefer that ref position which is marked both reload and
                    // allocate if profitable.  These ref positions don't need
                    // need to be spilled as they are already in memory and
                    // codegen considers them as contained memory operands.
                    isBetterLocation = (recentAssignedRef != nullptr) && recentAssignedRef->reload &&
                                       recentAssignedRef->AllocateIfProfitable();
                }
                else
                {
                    isBetterLocation = false;
                }
            }
        }

        if (isBetterLocation)
        {
            farthestLocation         = nextLocation;
            farthestRefPhysRegRecord = physRegRecord;
            farthestRefPosWeight     = recentAssignedRefWeight;
        }
    }

#if DEBUG
    if (allocateIfProfitable)
    {
        // There may not be a spill candidate or if one is found
        // its weight must be less than the weight of 'refPosition'
        assert((farthestRefPhysRegRecord == nullptr) || (farthestRefPosWeight < getWeight(refPosition)));
    }
    else
    {
        // Must have found a spill candidate.
        assert((farthestRefPhysRegRecord != nullptr) && (farthestLocation > refLocation || refPosition->isFixedRegRef));
    }
#endif

    if (farthestRefPhysRegRecord != nullptr)
    {
        foundReg = farthestRefPhysRegRecord->regNum;
        unassignPhysReg(farthestRefPhysRegRecord, farthestRefPhysRegRecord->assignedInterval->recentRefPosition);
        assignPhysReg(farthestRefPhysRegRecord, current);
        refPosition->registerAssignment = genRegMask(foundReg);
    }
    else
    {
        foundReg                        = REG_NA;
        refPosition->registerAssignment = RBM_NONE;
    }

    return foundReg;
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

    bool         foundFreeReg = false;
    RegRecord*   bestPhysReg  = nullptr;
    LsraLocation bestLocation = MinLocation;
    regMaskTP    candidates   = refPosition->registerAssignment;

    // Save the relatedInterval, if any, so that it doesn't get modified during allocation.
    Interval* savedRelatedInterval   = currentInterval->relatedInterval;
    currentInterval->relatedInterval = nullptr;

    // We don't want really want to change the default assignment,
    // so 1) pretend this isn't active, and 2) remember the old reg
    regNumber  oldPhysReg   = currentInterval->physReg;
    RegRecord* oldRegRecord = currentInterval->assignedReg;
    assert(oldRegRecord->regNum == oldPhysReg);
    currentInterval->isActive = false;

    regNumber allocatedReg = tryAllocateFreeReg(currentInterval, refPosition);
    if (allocatedReg == REG_NA)
    {
        allocatedReg = allocateBusyReg(currentInterval, refPosition, false);
    }

    // Now restore the old info
    currentInterval->relatedInterval = savedRelatedInterval;
    currentInterval->physReg         = oldPhysReg;
    currentInterval->assignedReg     = oldRegRecord;
    currentInterval->isActive        = true;

    refPosition->copyReg = true;
    return allocatedReg;
}

// Check if the interval is already assigned and if it is then unassign the physical record
// then set the assignedInterval to 'interval'
//
void LinearScan::checkAndAssignInterval(RegRecord* regRec, Interval* interval)
{
    if (regRec->assignedInterval != nullptr && regRec->assignedInterval != interval)
    {
        // This is allocated to another interval.  Either it is inactive, or it was allocated as a
        // copyReg and is therefore not the "assignedReg" of the other interval.  In the latter case,
        // we simply unassign it - in the former case we need to set the physReg on the interval to
        // REG_NA to indicate that it is no longer in that register.
        // The lack of checking for this case resulted in an assert in the retail version of System.dll,
        // in method SerialStream.GetDcbFlag.
        // Note that we can't check for the copyReg case, because we may have seen a more recent
        // RefPosition for the Interval that was NOT a copyReg.
        if (regRec->assignedInterval->assignedReg == regRec)
        {
            assert(regRec->assignedInterval->isActive == false);
            regRec->assignedInterval->physReg = REG_NA;
        }
        unassignPhysReg(regRec->regNum);
    }

    regRec->assignedInterval = interval;
}

// Assign the given physical register interval to the given interval
void LinearScan::assignPhysReg(RegRecord* regRec, Interval* interval)
{
    regMaskTP assignedRegMask = genRegMask(regRec->regNum);
    compiler->codeGen->regSet.rsSetRegsModified(assignedRegMask DEBUGARG(dumpTerse));

    checkAndAssignInterval(regRec, interval);
    interval->assignedReg = regRec;

#ifdef _TARGET_ARM_
    if ((interval->registerType == TYP_DOUBLE) && isFloatRegType(regRec->registerType))
    {
        regNumber  nextRegNum = REG_NEXT(regRec->regNum);
        RegRecord* nextRegRec = getRegisterRecord(nextRegNum);

        checkAndAssignInterval(nextRegRec, interval);
    }
#endif // _TARGET_ARM_

    interval->physReg  = regRec->regNum;
    interval->isActive = true;
    if (interval->isLocalVar)
    {
        // Prefer this register for future references
        interval->updateRegisterPreferences(assignedRegMask);
    }
}

//------------------------------------------------------------------------
// spill: Spill this Interval between "fromRefPosition" and "toRefPosition"
//
// Arguments:
//    fromRefPosition - The RefPosition at which the Interval is to be spilled
//    toRefPosition   - The RefPosition at which it must be reloaded
//
// Return Value:
//    None.
//
// Assumptions:
//    fromRefPosition and toRefPosition must not be null
//
void LinearScan::spillInterval(Interval* interval, RefPosition* fromRefPosition, RefPosition* toRefPosition)
{
    assert(fromRefPosition != nullptr && toRefPosition != nullptr);
    assert(fromRefPosition->getInterval() == interval && toRefPosition->getInterval() == interval);
    assert(fromRefPosition->nextRefPosition == toRefPosition);

    if (!fromRefPosition->lastUse)
    {
        // If not allocated a register, Lcl var def/use ref positions even if reg optional
        // should be marked as spillAfter.
        if (!fromRefPosition->RequiresRegister() && !(interval->isLocalVar && fromRefPosition->IsActualRef()))
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

    interval->isActive  = false;
    interval->isSpilled = true;

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
//    regRec           - the RegRecord to be unasssigned
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
//    regRec           - the physical RegRecord to be unasssigned
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

    regRec->assignedInterval = nullptr;
}

//------------------------------------------------------------------------
// unassignPhysReg: Unassign the given physical register record, and spill the
//                  assignedInterval at the given spillRefPosition, if any.
//
// Arguments:
//    regRec           - the RegRecord to be unasssigned
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
    checkAndClearInterval(regRec, spillRefPosition);
    regNumber thisRegNum = regRec->regNum;

#ifdef _TARGET_ARM_
    if ((assignedInterval->registerType == TYP_DOUBLE) && isFloatRegType(regRec->registerType))
    {
        regNumber  nextRegNum = REG_NEXT(regRec->regNum);
        RegRecord* nextRegRec = getRegisterRecord(nextRegNum);
        checkAndClearInterval(nextRegRec, spillRefPosition);
    }
#endif // _TARGET_ARM_

#ifdef DEBUG
    if (VERBOSE && !dumpTerse)
    {
        printf("unassigning %s: ", getRegName(regRec->regNum));
        assignedInterval->dump();
        printf("\n");
    }
#endif // DEBUG

    RefPosition* nextRefPosition = nullptr;
    if (spillRefPosition != nullptr)
    {
        nextRefPosition = spillRefPosition->nextRefPosition;
    }

    if (assignedInterval->physReg != REG_NA && assignedInterval->physReg != thisRegNum)
    {
        // This must have been a temporary copy reg, but we can't assert that because there
        // may have been intervening RefPositions that were not copyRegs.
        regRec->assignedInterval = nullptr;
        return;
    }

    regNumber victimAssignedReg = assignedInterval->physReg;
    assignedInterval->physReg   = REG_NA;

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
            spillRefPosition->treeNode != nullptr && (spillRefPosition->treeNode->gtFlags & GTF_VAR_DEATH) != 0)
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
                    assignedInterval->isSpilled = true;
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
            spillInterval(assignedInterval, spillRefPosition, nextRefPosition);
        }
    }
    // Maintain the association with the interval, if it has more references.
    // Or, if we "remembered" an interval assigned to this register, restore it.
    if (nextRefPosition != nullptr)
    {
        assignedInterval->assignedReg = regRec;
    }
    else if (regRec->previousInterval != nullptr && regRec->previousInterval->assignedReg == regRec &&
             regRec->previousInterval->getNextRefPosition() != nullptr)
    {
        regRec->assignedInterval = regRec->previousInterval;
        regRec->previousInterval = nullptr;
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
        regRec->assignedInterval = nullptr;
        regRec->previousInterval = nullptr;
    }
}

//------------------------------------------------------------------------
// spillGCRefs: Spill any GC-type intervals that are currently in registers.a
//
// Arguments:
//    killRefPosition - The RefPosition for the kill
//
// Return Value:
//    None.
//
void LinearScan::spillGCRefs(RefPosition* killRefPosition)
{
    // For each physical register that can hold a GC type,
    // if it is occupied by an interval of a GC type, spill that interval.
    regMaskTP candidateRegs = killRefPosition->registerAssignment;
    while (candidateRegs != RBM_NONE)
    {
        regMaskTP nextRegBit = genFindLowestBit(candidateRegs);
        candidateRegs &= ~nextRegBit;
        regNumber  nextReg          = genRegNumFromMask(nextRegBit);
        RegRecord* regRecord        = getRegisterRecord(nextReg);
        Interval*  assignedInterval = regRecord->assignedInterval;
        if (assignedInterval == nullptr || (assignedInterval->isActive == false) ||
            !varTypeIsGC(assignedInterval->registerType))
        {
            continue;
        }
        unassignPhysReg(regRecord, assignedInterval->recentRefPosition);
    }
    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_DONE_KILL_GC_REFS, nullptr, REG_NA, nullptr));
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
//    Calls processBlockEndLocation() to set the outVarToRegMap, then gets the next block,
//    and sets the inVarToRegMap appropriately.

void LinearScan::processBlockEndAllocation(BasicBlock* currentBlock)
{
    assert(currentBlock != nullptr);
    processBlockEndLocations(currentBlock);
    markBlockVisited(currentBlock);

    // Get the next block to allocate.
    // When the last block in the method has successors, there will be a final "RefTypeBB" to
    // ensure that we get the varToRegMap set appropriately, but in that case we don't need
    // to worry about "nextBlock".
    BasicBlock* nextBlock = getNextBlock();
    if (nextBlock != nullptr)
    {
        processBlockStartLocations(nextBlock, true);
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

//------------------------------------------------------------------------
// processBlockStartLocations: Update var locations on entry to 'currentBlock'
//
// Arguments:
//    currentBlock   - the BasicBlock we have just finished allocating registers for
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
void LinearScan::processBlockStartLocations(BasicBlock* currentBlock, bool allocationPass)
{
    unsigned    predBBNum         = blockInfo[currentBlock->bbNum].predBBNum;
    VarToRegMap predVarToRegMap   = getOutVarToRegMap(predBBNum);
    VarToRegMap inVarToRegMap     = getInVarToRegMap(currentBlock->bbNum);
    bool        hasCriticalInEdge = blockInfo[currentBlock->bbNum].hasCriticalInEdge;

    VARSET_TP VARSET_INIT_NOCOPY(liveIn, currentBlock->bbLiveIn);
#ifdef DEBUG
    if (getLsraExtendLifeTimes())
    {
        VarSetOps::AssignNoCopy(compiler, liveIn, compiler->lvaTrackedVars);
    }
    // If we are rotating register assignments at block boundaries, we want to make the
    // inactive registers available for the rotation.
    regMaskTP inactiveRegs = RBM_NONE;
#endif // DEBUG
    regMaskTP liveRegs = RBM_NONE;
    VARSET_ITER_INIT(compiler, iter, liveIn, varIndex);
    while (iter.NextElem(compiler, &varIndex))
    {
        unsigned varNum = compiler->lvaTrackedToVarNum[varIndex];
        if (!compiler->lvaTable[varNum].lvLRACandidate)
        {
            continue;
        }
        regNumber    targetReg;
        Interval*    interval        = getIntervalForLocalVar(varNum);
        RefPosition* nextRefPosition = interval->getNextRefPosition();
        assert(nextRefPosition != nullptr);

        if (allocationPass)
        {
            targetReg = predVarToRegMap[varIndex];
            INDEBUG(targetReg       = rotateBlockStartLocation(interval, targetReg, (~liveRegs | inactiveRegs)));
            inVarToRegMap[varIndex] = targetReg;
        }
        else // !allocationPass (i.e. resolution/write-back pass)
        {
            targetReg = inVarToRegMap[varIndex];
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
                if (predVarToRegMap[varIndex] != REG_STK)
                {
                    // Case #1 above.
                    assert(predVarToRegMap[varIndex] == targetReg ||
                           getLsraBlockBoundaryLocations() == LSRA_BLOCK_BOUNDARY_ROTATE);
                }
                else if (!nextRefPosition->copyReg)
                {
                    // case #2 above.
                    inVarToRegMap[varIndex] = REG_STK;
                    targetReg               = REG_STK;
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
                liveRegs |= genRegMask(targetReg);
                continue;
            }
        }
        else if (interval->physReg != REG_NA)
        {
            // This can happen if we are using the locations from a basic block other than the
            // immediately preceding one - where the variable was in a different location.
            if (targetReg != REG_STK)
            {
                // Unassign it from the register (it will get a new register below).
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
            else if (allocationPass)
            {
                // Keep the register assignment - if another var has it, it will get unassigned.
                // Otherwise, resolution will fix it up later, and it will be more
                // likely to match other assignments this way.
                interval->isActive = true;
                liveRegs |= genRegMask(interval->physReg);
                INDEBUG(inactiveRegs |= genRegMask(interval->physReg));
                inVarToRegMap[varIndex] = interval->physReg;
            }
            else
            {
                interval->physReg = REG_NA;
            }
        }
        if (targetReg != REG_STK)
        {
            RegRecord* targetRegRecord = getRegisterRecord(targetReg);
            liveRegs |= genRegMask(targetReg);
            if (!interval->isActive)
            {
                interval->isActive    = true;
                interval->physReg     = targetReg;
                interval->assignedReg = targetRegRecord;
            }
            Interval* assignedInterval = targetRegRecord->assignedInterval;
            if (assignedInterval != interval)
            {
                // Is there another interval currently assigned to this register?  If so unassign it.
                if (assignedInterval != nullptr)
                {
                    if (assignedInterval->assignedReg == targetRegRecord)
                    {
                        // If the interval is active, it will be set to active when we reach its new
                        // register assignment (which we must not yet have done, or it wouldn't still be
                        // assigned to this register).
                        assignedInterval->isActive = false;
                        unassignPhysReg(targetRegRecord, nullptr);
                        if (allocationPass && assignedInterval->isLocalVar &&
                            inVarToRegMap[assignedInterval->getVarIndex(compiler)] == targetReg)
                        {
                            inVarToRegMap[assignedInterval->getVarIndex(compiler)] = REG_STK;
                        }
                    }
                    else
                    {
                        // This interval is no longer assigned to this register.
                        targetRegRecord->assignedInterval = nullptr;
                    }
                }
                assignPhysReg(targetRegRecord, interval);
            }
            if (interval->recentRefPosition != nullptr && !interval->recentRefPosition->copyReg &&
                interval->recentRefPosition->registerAssignment != genRegMask(targetReg))
            {
                interval->getNextRefPosition()->outOfOrder = true;
            }
        }
    }

    // Unassign any registers that are no longer live.
    for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
    {
        if ((liveRegs & genRegMask(reg)) == 0)
        {
            RegRecord* physRegRecord    = getRegisterRecord(reg);
            Interval*  assignedInterval = physRegRecord->assignedInterval;

            if (assignedInterval != nullptr)
            {
                assert(assignedInterval->isLocalVar || assignedInterval->isConstant);
                if (!assignedInterval->isConstant && assignedInterval->assignedReg == physRegRecord)
                {
                    assignedInterval->isActive = false;
                    if (assignedInterval->getNextRefPosition() == nullptr)
                    {
                        unassignPhysReg(physRegRecord, nullptr);
                    }
                    inVarToRegMap[assignedInterval->getVarIndex(compiler)] = REG_STK;
                }
                else
                {
                    // This interval may still be active, but was in another register in an
                    // intervening block.
                    physRegRecord->assignedInterval = nullptr;
                }
            }
        }
    }
    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_START_BB, nullptr, REG_NA, currentBlock));
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

    VARSET_TP VARSET_INIT_NOCOPY(liveOut, currentBlock->bbLiveOut);
#ifdef DEBUG
    if (getLsraExtendLifeTimes())
    {
        VarSetOps::AssignNoCopy(compiler, liveOut, compiler->lvaTrackedVars);
    }
#endif // DEBUG
    regMaskTP liveRegs = RBM_NONE;
    VARSET_ITER_INIT(compiler, iter, liveOut, varIndex);
    while (iter.NextElem(compiler, &varIndex))
    {
        unsigned  varNum   = compiler->lvaTrackedToVarNum[varIndex];
        Interval* interval = getIntervalForLocalVar(varNum);
        if (interval->isActive)
        {
            assert(interval->physReg != REG_NA && interval->physReg != REG_STK);
            outVarToRegMap[varIndex] = interval->physReg;
        }
        else
        {
            outVarToRegMap[varIndex] = REG_STK;
        }
    }
    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_END_BB));
}

#ifdef DEBUG
void LinearScan::dumpRefPositions(const char* str)
{
    printf("------------\n");
    printf("REFPOSITIONS %s: \n", str);
    printf("------------\n");
    for (auto& refPos : refPositions)
    {
        refPos.dump();
    }
}
#endif // DEBUG

bool LinearScan::registerIsFree(regNumber regNum, RegisterType regType)
{
    RegRecord* physRegRecord = getRegisterRecord(regNum);

    bool isFree = physRegRecord->isFree();

#ifdef _TARGET_ARM_
    if (isFree && regType == TYP_DOUBLE)
    {
        isFree = getRegisterRecord(REG_NEXT(regNum))->isFree();
    }
#endif // _TARGET_ARM_

    return isFree;
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
void LinearScan::freeRegister(RegRecord* physRegRecord)
{
    Interval* assignedInterval = physRegRecord->assignedInterval;
    // It may have already been freed by a "Kill"
    if (assignedInterval != nullptr)
    {
        assignedInterval->isActive = false;
        // If this is a constant node, that we may encounter again (e.g. constant),
        // don't unassign it until we need the register.
        if (!assignedInterval->isConstant)
        {
            RefPosition* nextRefPosition = assignedInterval->getNextRefPosition();
            // Unassign the register only if there are no more RefPositions, or the next
            // one is a def.  Note that the latter condition doesn't actually ensure that
            // there aren't subsequent uses that could be reached by a def in the assigned
            // register, but is merely a heuristic to avoid tying up the register (or using
            // it when it's non-optimal).  A better alternative would be to use SSA, so that
            // we wouldn't unnecessarily link separate live ranges to the same register.
            if (nextRefPosition == nullptr || RefTypeIsDef(nextRefPosition->refType))
            {
                unassignPhysReg(physRegRecord, nullptr);
            }
        }
    }
}

void LinearScan::freeRegisters(regMaskTP regsToFree)
{
    if (regsToFree == RBM_NONE)
    {
        return;
    }

    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_FREE_REGS));
    while (regsToFree != RBM_NONE)
    {
        regMaskTP nextRegBit = genFindLowestBit(regsToFree);
        regsToFree &= ~nextRegBit;
        regNumber nextReg = genRegNumFromMask(nextRegBit);
        freeRegister(getRegisterRecord(nextReg));
    }
}

// Actual register allocation, accomplished by iterating over all of the previously
// constructed Intervals
// Loosely based on raAssignVars()
//
void LinearScan::allocateRegisters()
{
    JITDUMP("*************** In LinearScan::allocateRegisters()\n");
    DBEXEC(VERBOSE, lsraDumpIntervals("before allocateRegisters"));

    // at start, nothing is active except for register args
    for (auto& interval : intervals)
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

    for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
    {
        getRegisterRecord(reg)->recentRefPosition = nullptr;
        getRegisterRecord(reg)->isActive          = false;
    }

#ifdef DEBUG
    regNumber lastAllocatedReg = REG_NA;
    if (VERBOSE)
    {
        dumpRefPositions("BEFORE ALLOCATION");
        dumpVarRefPositions("BEFORE ALLOCATION");

        printf("\n\nAllocating Registers\n"
               "--------------------\n");
        if (dumpTerse)
        {
            dumpRegRecordHeader();
            // Now print an empty indent
            printf(indentFormat, "");
        }
    }
#endif // DEBUG

    BasicBlock* currentBlock = nullptr;

    LsraLocation prevLocation    = MinLocation;
    regMaskTP    regsToFree      = RBM_NONE;
    regMaskTP    delayRegsToFree = RBM_NONE;

    // This is the most recent RefPosition for which a register was allocated
    // - currently only used for DEBUG but maintained in non-debug, for clarity of code
    //   (and will be optimized away because in non-debug spillAlways() unconditionally returns false)
    RefPosition* lastAllocatedRefPosition = nullptr;

    bool handledBlockEnd = false;

    for (auto& refPosition : refPositions)
    {
        RefPosition* currentRefPosition = &refPosition;

#ifdef DEBUG
        // Set the activeRefPosition to null until we're done with any boundary handling.
        activeRefPosition = nullptr;
        if (VERBOSE)
        {
            if (dumpTerse)
            {
                // We're really dumping the RegRecords "after" the previous RefPosition, but it's more convenient
                // to do this here, since there are a number of "continue"s in this loop.
                dumpRegRecords();
            }
            else
            {
                printf("\n");
            }
        }
#endif // DEBUG

        // This is the previousRefPosition of the current Referent, if any
        RefPosition* previousRefPosition = nullptr;

        Interval*      currentInterval = nullptr;
        Referenceable* currentReferent = nullptr;
        bool           isInternalRef   = false;
        RefType        refType         = currentRefPosition->refType;

        currentReferent = currentRefPosition->referent;

        if (spillAlways() && lastAllocatedRefPosition != nullptr && !lastAllocatedRefPosition->isPhysRegRef &&
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

        if ((regsToFree | delayRegsToFree) != RBM_NONE)
        {
            bool doFreeRegs = false;
            // Free at a new location, or at a basic block boundary
            if (currentLocation > prevLocation || refType == RefTypeBB)
            {
                doFreeRegs = true;
            }

            if (doFreeRegs)
            {
                freeRegisters(regsToFree);
                regsToFree      = delayRegsToFree;
                delayRegsToFree = RBM_NONE;
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

        // For the purposes of register resolution, we handle the DummyDefs before
        // the block boundary - so the RefTypeBB is after all the DummyDefs.
        // However, for the purposes of allocation, we want to handle the block
        // boundary first, so that we can free any registers occupied by lclVars
        // that aren't live in the next block and make them available for the
        // DummyDefs.

        if (!handledBlockEnd && (refType == RefTypeBB || refType == RefTypeDummyDef))
        {
            // Free any delayed regs (now in regsToFree) before processing the block boundary
            freeRegisters(regsToFree);
            regsToFree         = RBM_NONE;
            handledBlockEnd    = true;
            curBBStartLocation = currentRefPosition->nodeLocation;
            if (currentBlock == nullptr)
            {
                currentBlock = startBlockSequence();
            }
            else
            {
                processBlockEndAllocation(currentBlock);
                currentBlock = moveToNextBlock();
            }
#ifdef DEBUG
            if (VERBOSE && currentBlock != nullptr && !dumpTerse)
            {
                currentBlock->dspBlockHeader(compiler);
                printf("\n");
            }
#endif // DEBUG
        }

#ifdef DEBUG
        activeRefPosition = currentRefPosition;
        if (VERBOSE)
        {
            if (dumpTerse)
            {
                dumpRefPositionShort(currentRefPosition, currentBlock);
            }
            else
            {
                currentRefPosition->dump();
            }
        }
#endif // DEBUG

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

        // If this is a FixedReg, disassociate any inactive constant interval from this register.
        // Otherwise, do nothing.
        if (refType == RefTypeFixedReg)
        {
            RegRecord* regRecord = currentRefPosition->getReg();
            if (regRecord->assignedInterval != nullptr && !regRecord->assignedInterval->isActive &&
                regRecord->assignedInterval->isConstant)
            {
                regRecord->assignedInterval = nullptr;
            }
            INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_FIXED_REG, nullptr, currentRefPosition->assignedReg()));
            continue;
        }

        // If this is an exposed use, do nothing - this is merely a placeholder to attempt to
        // ensure that a register is allocated for the full lifetime.  The resolution logic
        // will take care of moving to the appropriate register if needed.

        if (refType == RefTypeExpUse)
        {
            INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_EXP_USE));
            continue;
        }

        regNumber assignedRegister = REG_NA;

        if (currentRefPosition->isIntervalRef())
        {
            currentInterval  = currentRefPosition->getInterval();
            assignedRegister = currentInterval->physReg;
#if DEBUG
            if (VERBOSE && !dumpTerse)
            {
                currentInterval->dump();
            }
#endif // DEBUG

            // Identify the special cases where we decide up-front not to allocate
            bool allocate = true;
            bool didDump  = false;

            if (refType == RefTypeParamDef || refType == RefTypeZeroInit)
            {
                // For a ParamDef with a weighted refCount less than unity, don't enregister it at entry.
                // TODO-CQ: Consider doing this only for stack parameters, since otherwise we may be needlessly
                // inserting a store.
                LclVarDsc* varDsc = currentInterval->getLocalVar(compiler);
                assert(varDsc != nullptr);
                if (refType == RefTypeParamDef && varDsc->lvRefCntWtd <= BB_UNITY_WEIGHT)
                {
                    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_NO_ENTRY_REG_ALLOCATED, currentInterval));
                    didDump  = true;
                    allocate = false;
                }
                // If it has no actual references, mark it as "lastUse"; since they're not actually part
                // of any flow they won't have been marked during dataflow.  Otherwise, if we allocate a
                // register we won't unassign it.
                else if (currentRefPosition->nextRefPosition == nullptr)
                {
                    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_ZERO_REF, currentInterval));
                    currentRefPosition->lastUse = true;
                }
            }
#ifdef FEATURE_SIMD
            else if (refType == RefTypeUpperVectorSaveDef || refType == RefTypeUpperVectorSaveUse)
            {
                Interval* lclVarInterval = currentInterval->relatedInterval;
                if (lclVarInterval->physReg == REG_NA)
                {
                    allocate = false;
                }
            }
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
                assert(srcInterval->isLocalVar);
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
                        currentInterval->getNextRefLocation() == physRegRecord->getNextRefLocation())
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
                        physRegRecord->isBusyUntilNextKill = true;
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
                    assert(inVarToRegMaps[curBBNum][currentInterval->getVarIndex(compiler)] == REG_STK &&
                           previousRefPosition->nodeLocation <= curBBStartLocation);
                    isInRegister = false;
                }
                else
                {
                    currentInterval->isActive = true;
                }
            }
            assert(currentInterval->assignedReg != nullptr &&
                   currentInterval->assignedReg->regNum == assignedRegister &&
                   currentInterval->assignedReg->assignedInterval == currentInterval);
        }

        // If this is a physical register, we unconditionally assign it to itself!
        if (currentRefPosition->isPhysRegRef)
        {
            RegRecord* currentReg       = currentRefPosition->getReg();
            Interval*  assignedInterval = currentReg->assignedInterval;

            if (assignedInterval != nullptr)
            {
                unassignPhysReg(currentReg, assignedInterval->recentRefPosition);
            }
            currentReg->isActive = true;
            assignedRegister     = currentReg->regNum;
            assignedRegBit       = genRegMask(assignedRegister);
            if (refType == RefTypeKill)
            {
                currentReg->isBusyUntilNextKill = false;
            }
        }
        else if (previousRefPosition != nullptr)
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
                RegRecord*   physRegRecord     = getRegisterRecord(currentInterval->physReg);
                RefPosition* nextPhysRegRefPos = physRegRecord->getNextRefPosition();
                if (nextPhysRegRefPos != nullptr &&
                    nextPhysRegRefPos->nodeLocation <= currentInterval->lastRefPosition->nodeLocation)
                {
                    // Check to see if the existing assignment matches the preferences (e.g. callee save registers)
                    // and ensure that the next use of this localVar does not occur after the nextPhysRegRefPos
                    // There must be a next RefPosition, because we know that the Interval extends beyond the
                    // nextPhysRegRefPos.
                    RefPosition* nextLclVarRefPos = currentRefPosition->nextRefPosition;
                    assert(nextLclVarRefPos != nullptr);
                    if (!matchesPreferences || nextPhysRegRefPos->nodeLocation < nextLclVarRefPos->nodeLocation ||
                        physRegRecord->conflictingFixedRegReference(nextLclVarRefPos))
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
                    currentRefPosition->registerAssignment = allRegs(currentInterval->registerType);
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
            // If there is a conflicting fixed reference, insert a copy.
            RegRecord* physRegRecord = getRegisterRecord(assignedRegister);
            if (physRegRecord->conflictingFixedRegReference(currentRefPosition))
            {
                // We may have already reassigned the register to the conflicting reference.
                // If not, we need to unassign this interval.
                if (physRegRecord->assignedInterval == currentInterval)
                {
                    unassignPhysRegNoSpill(physRegRecord);
                }
                currentRefPosition->moveReg = true;
                assignedRegister            = REG_NA;
                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_MOVE_REG, currentInterval, assignedRegister));
            }
            else if ((genRegMask(assignedRegister) & currentRefPosition->registerAssignment) != 0)
            {
                currentRefPosition->registerAssignment = assignedRegBit;
                if (!currentReferent->isActive)
                {
                    // If we've got an exposed use at the top of a block, the
                    // interval might not have been active.  Otherwise if it's a use,
                    // the interval must be active.
                    if (refType == RefTypeDummyDef)
                    {
                        currentReferent->isActive = true;
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
                // This must be a localVar or a single-reg fixed use or a tree temp with conflicting def & use.

                assert(currentInterval && (currentInterval->isLocalVar || currentRefPosition->isFixedRegRef ||
                                           currentInterval->hasConflictingDefUse));

                // It's already in a register, but not one we need.
                // If it is a fixed use that is not marked "delayRegFree", there is already a FixedReg to ensure that
                // the needed reg is not otherwise in use, so we can simply ignore it and codegen will do the copy.
                // The reason we need special handling for the "delayRegFree" case is that we need to mark the
                // fixed-reg as in-use and delayed (the FixedReg RefPosition doesn't handle the delay requirement).
                // Otherwise, if this is a pure use localVar or tree temp, we assign a copyReg, but must free both regs
                // if it is a last use.
                if (!currentRefPosition->isFixedRegRef || currentRefPosition->delayRegFree)
                {
                    if (!RefTypeIsDef(currentRefPosition->refType))
                    {
                        regNumber copyReg = assignCopyReg(currentRefPosition);
                        assert(copyReg != REG_NA);
                        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_COPY_REG, currentInterval, copyReg));
                        lastAllocatedRefPosition = currentRefPosition;
                        if (currentRefPosition->lastUse)
                        {
                            if (currentRefPosition->delayRegFree)
                            {
                                INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_LAST_USE_DELAYED, currentInterval,
                                                                assignedRegister));
                                delayRegsToFree |=
                                    (genRegMask(assignedRegister) | currentRefPosition->registerAssignment);
                            }
                            else
                            {
                                INDEBUG(
                                    dumpLsraAllocationEvent(LSRA_EVENT_LAST_USE, currentInterval, assignedRegister));
                                regsToFree |= (genRegMask(assignedRegister) | currentRefPosition->registerAssignment);
                            }
                        }
                        // If this is a tree temp (non-localVar) interval, we will need an explicit move.
                        if (!currentInterval->isLocalVar)
                        {
                            currentRefPosition->moveReg = true;
                            currentRefPosition->copyReg = false;
                        }
                        continue;
                    }
                    else
                    {
                        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_NEEDS_NEW_REG, nullptr, assignedRegister));
                        regsToFree |= genRegMask(assignedRegister);
                        // We want a new register, but we don't want this to be considered a spill.
                        assignedRegister = REG_NA;
                        if (physRegRecord->assignedInterval == currentInterval)
                        {
                            unassignPhysRegNoSpill(physRegRecord);
                        }
                    }
                }
                else
                {
                    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_KEPT_ALLOCATION, nullptr, assignedRegister));
                }
            }
        }

        if (assignedRegister == REG_NA)
        {
            bool allocateReg = true;

            if (currentRefPosition->AllocateIfProfitable())
            {
                // We can avoid allocating a register if it is a the last use requiring a reload.
                if (currentRefPosition->lastUse && currentRefPosition->reload)
                {
                    allocateReg = false;
                }

#ifdef DEBUG
                // Under stress mode, don't attempt to allocate a reg to
                // reg optional ref position.
                if (allocateReg && regOptionalNoAlloc())
                {
                    allocateReg = false;
                }
#endif
            }

            if (allocateReg)
            {
                // Try to allocate a register
                assignedRegister = tryAllocateFreeReg(currentInterval, currentRefPosition);
            }

            // If no register was found, and if the currentRefPosition must have a register,
            // then find a register to spill
            if (assignedRegister == REG_NA)
            {
#ifdef FEATURE_SIMD
                if (refType == RefTypeUpperVectorSaveDef)
                {
                    // TODO-CQ: Determine whether copying to two integer callee-save registers would be profitable.
                    currentRefPosition->registerAssignment = (allRegs(TYP_FLOAT) & RBM_FLT_CALLEE_TRASH);
                    assignedRegister                       = tryAllocateFreeReg(currentInterval, currentRefPosition);
                    // There MUST be caller-save registers available, because they have all just been killed.
                    assert(assignedRegister != REG_NA);
                    // Now, spill it.
                    // (These will look a bit backward in the dump, but it's a pain to dump the alloc before the spill).
                    unassignPhysReg(getRegisterRecord(assignedRegister), currentRefPosition);
                    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_ALLOC_REG, currentInterval, assignedRegister));
                    // Now set assignedRegister to REG_NA again so that we don't re-activate it.
                    assignedRegister = REG_NA;
                }
                else
#endif // FEATURE_SIMD
                    if (currentRefPosition->RequiresRegister() || currentRefPosition->AllocateIfProfitable())
                {
                    if (allocateReg)
                    {
                        assignedRegister = allocateBusyReg(currentInterval, currentRefPosition,
                                                           currentRefPosition->AllocateIfProfitable());
                    }

                    if (assignedRegister != REG_NA)
                    {
                        INDEBUG(
                            dumpLsraAllocationEvent(LSRA_EVENT_ALLOC_SPILLED_REG, currentInterval, assignedRegister));
                    }
                    else
                    {
                        // This can happen only for those ref positions that are to be allocated
                        // only if profitable.
                        noway_assert(currentRefPosition->AllocateIfProfitable());

                        currentRefPosition->registerAssignment = RBM_NONE;
                        currentRefPosition->reload             = false;

                        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_NO_REG_ALLOCATED, currentInterval));
                    }
                }
                else
                {
                    INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_NO_REG_ALLOCATED, currentInterval));
                    currentRefPosition->registerAssignment = RBM_NONE;
                    currentInterval->isActive              = false;
                }
            }
#ifdef DEBUG
            else
            {
                if (VERBOSE)
                {
                    if (currentInterval->isConstant && (currentRefPosition->treeNode != nullptr) &&
                        currentRefPosition->treeNode->IsReuseRegVal())
                    {
                        dumpLsraAllocationEvent(LSRA_EVENT_REUSE_REG, nullptr, assignedRegister, currentBlock);
                    }
                    else
                    {
                        dumpLsraAllocationEvent(LSRA_EVENT_ALLOC_REG, nullptr, assignedRegister, currentBlock);
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
        if (currentInterval != nullptr && assignedRegister != REG_NA)
        {
            assignedRegBit                         = genRegMask(assignedRegister);
            currentRefPosition->registerAssignment = assignedRegBit;
            currentInterval->physReg               = assignedRegister;
            regsToFree &= ~assignedRegBit; // we'll set it again later if it's dead

            // If this interval is dead, free the register.
            // The interval could be dead if this is a user variable, or if the
            // node is being evaluated for side effects, or a call whose result
            // is not used, etc.
            if (currentRefPosition->lastUse || currentRefPosition->nextRefPosition == nullptr)
            {
                assert(currentRefPosition->isIntervalRef());

                if (refType != RefTypeExpUse && currentRefPosition->nextRefPosition == nullptr)
                {
                    if (currentRefPosition->delayRegFree)
                    {
                        delayRegsToFree |= assignedRegBit;
                        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_LAST_USE_DELAYED));
                    }
                    else
                    {
                        regsToFree |= assignedRegBit;
                        INDEBUG(dumpLsraAllocationEvent(LSRA_EVENT_LAST_USE));
                    }
                }
                else
                {
                    currentInterval->isActive = false;
                }
            }

            lastAllocatedRefPosition = currentRefPosition;
        }
    }

    // Free registers to clear associated intervals for resolution phase
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    if (getLsraExtendLifeTimes())
    {
        // If we have extended lifetimes, we need to make sure all the registers are freed.
        for (int regNumIndex = 0; regNumIndex <= REG_FP_LAST; regNumIndex++)
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
        if (dumpTerse)
        {
            // Dump the RegRecords after the last RefPosition is handled.
            dumpRegRecords();
            printf("\n");
        }

        dumpRefPositions("AFTER ALLOCATION");
        dumpVarRefPositions("AFTER ALLOCATION");

        // Dump the intervals that remain active
        printf("Active intervals at end of allocation:\n");

        // We COULD just reuse the intervalIter from above, but ArrayListIterator doesn't
        // provide a Reset function (!) - we'll probably replace this so don't bother
        // adding it

        for (auto& interval : intervals)
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
//   - varDsc->lvRegNum (and lvOtherReg) contain the unique register location.
//     If it is not in the same register through its lifetime, it is set to REG_STK.
//   - interval->physReg is set to the assigned register
//     (i.e. at the code location which is currently being handled by resolveRegisters())
//     - interval->isActive is true iff the interval is live and occupying a register
//     - interval->isSpilled is set to true if the interval is EVER spilled
//     - interval->isSplit is set to true if the interval does not occupy the same
//       register throughout the method
//   - RegRecord->assignedInterval points to the interval which currently occupies
//     the register
//   - For each lclVar node:
//     - gtRegNum/gtRegPair is set to the currently allocated register(s)
//     - GTF_REG_VAL is set if it is a use, and is in a register
//     - GTF_SPILLED is set on a use if it must be reloaded prior to use (GTF_REG_VAL
//       must not be set)
//     - GTF_SPILL is set if it must be spilled after use (GTF_REG_VAL may or may not
//       be set)
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
void LinearScan::resolveLocalRef(BasicBlock* block, GenTreePtr treeNode, RefPosition* currentRefPosition)
{
    assert((block == nullptr) == (treeNode == nullptr));

    // Is this a tracked local?  Or just a register allocated for loading
    // a non-tracked one?
    Interval* interval = currentRefPosition->getInterval();
    if (!interval->isLocalVar)
    {
        return;
    }
    interval->recentRefPosition = currentRefPosition;
    LclVarDsc* varDsc           = interval->getLocalVar(compiler);

    if (currentRefPosition->registerAssignment == RBM_NONE)
    {
        assert(!currentRefPosition->RequiresRegister());

        interval->isSpilled = true;
        varDsc->lvRegNum    = REG_STK;
        if (interval->assignedReg != nullptr && interval->assignedReg->assignedInterval == interval)
        {
            interval->assignedReg->assignedInterval = nullptr;
        }
        interval->assignedReg = nullptr;
        interval->physReg     = REG_NA;

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
                oldRegRecord->assignedInterval = nullptr;
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

    // In the reload case we simply do not set GTF_REG_VAL, and it gets
    // referenced from the variable's home location.
    // This is also true for a pure def which is spilled.
    if (reload && currentRefPosition->refType != RefTypeDef)
    {
        varDsc->lvRegNum = REG_STK;
        if (!spillAfter)
        {
            interval->physReg = assignedReg;
        }

        // If there is no treeNode, this must be a RefTypeExpUse, in
        // which case we did the reload already
        if (treeNode != nullptr)
        {
            treeNode->gtFlags |= GTF_SPILLED;
            if (spillAfter)
            {
                if (currentRefPosition->AllocateIfProfitable())
                {
                    // This is a use of lclVar that is flagged as reg-optional
                    // by lower/codegen and marked for both reload and spillAfter.
                    // In this case we can avoid unnecessary reload and spill
                    // by setting reg on lclVar to REG_STK and reg on tree node
                    // to REG_NA.  Codegen will generate the code by considering
                    // it as a contained memory operand.
                    //
                    // Note that varDsc->lvRegNum is already to REG_STK above.
                    interval->physReg  = REG_NA;
                    treeNode->gtRegNum = REG_NA;
                    treeNode->gtFlags &= ~GTF_SPILLED;
                }
                else
                {
                    treeNode->gtFlags |= GTF_SPILL;
                }
            }
        }
        else
        {
            assert(currentRefPosition->refType == RefTypeExpUse);
        }

        // If we have an undefined use set it as non-reg
        if (!interval->isSpilled)
        {
            if (varDsc->lvIsParam && !varDsc->lvIsRegArg && currentRefPosition == interval->firstRefPosition)
            {
                // Parameters are the only thing that can be used before defined
            }
            else
            {
                // if we see a use before def of something else, the zero init flag better not be set.
                noway_assert(!compiler->info.compInitMem);
                // if it is not set, then the behavior is undefined but we don't want to crash or assert
                interval->isSpilled = true;
            }
        }
    }
    else if (spillAfter && !RefTypeIsUse(currentRefPosition->refType))
    {
        // In the case of a pure def, don't bother spilling - just assign it to the
        // stack.  However, we need to remember that it was spilled.

        interval->isSpilled = true;
        varDsc->lvRegNum    = REG_STK;
        interval->physReg   = REG_NA;
        if (treeNode != nullptr)
        {
            treeNode->gtRegNum = REG_NA;
        }
    }
    else
    {
        // Not reload and Not pure-def that's spillAfter

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
            treeNode->gtRegNum = interval->physReg;

            if (currentRefPosition->copyReg)
            {
                homeReg = interval->physReg;
            }
            else
            {
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
                if (varDsc->lvRegNum != REG_STK)
                {
                    // If the register assignments don't match, then this interval is spilt,
                    // but not spilled (yet)
                    // However, we don't have a single register assignment now
                    if (varDsc->lvRegNum != assignedReg)
                    {
                        interval->isSplit = TRUE;
                        varDsc->lvRegNum  = REG_STK;
                    }
                }
                else
                {
                    varDsc->lvRegNum = assignedReg;
                }
            }
        }
        if (spillAfter)
        {
            if (treeNode != nullptr)
            {
                treeNode->gtFlags |= GTF_SPILL;
            }
            interval->isSpilled = true;
            interval->physReg   = REG_NA;
            varDsc->lvRegNum    = REG_STK;
        }

        // This value is in a register, UNLESS we already saw this treeNode
        // and marked it for reload
        if (treeNode != nullptr && !(treeNode->gtFlags & GTF_SPILLED))
        {
            treeNode->gtFlags |= GTF_REG_VAL;
        }
    }

    // Update the physRegRecord for the register, so that we know what vars are in
    // regs at the block boundaries
    RegRecord* physRegRecord = getRegisterRecord(homeReg);
    if (spillAfter || currentRefPosition->lastUse)
    {
        physRegRecord->assignedInterval = nullptr;
        interval->assignedReg           = nullptr;
        interval->physReg               = REG_NA;
        interval->isActive              = false;
    }
    else
    {
        interval->isActive              = true;
        physRegRecord->assignedInterval = interval;
        interval->assignedReg           = physRegRecord;
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
//                                           /   \
//                                         /       \
//                                       /           \
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
//                                           /   \
//                                         /       \
//                                       /           \
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
void LinearScan::insertCopyOrReload(BasicBlock* block, GenTreePtr tree, unsigned multiRegIdx, RefPosition* refPosition)
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
    }

    // If the parent is a reload/copy node, then tree must be a multi-reg call node
    // that has already had one of its registers spilled. This is Because multi-reg
    // call node is the only node whose RefTypeDef positions get independently
    // spilled or reloaded.  It is possible that one of its RefTypeDef position got
    // spilled and the next use of it requires it to be in a different register.
    //
    // In this case set the ith position reg of reload/copy node to the reg allocated
    // for copy/reload refPosition.  Essentially a copy/reload node will have a reg
    // for each multi-reg position of its child. If there is a valid reg in ith
    // position of GT_COPY or GT_RELOAD node then the corresponding result of its
    // child needs to be copied or reloaded to that reg.
    if (parent->IsCopyOrReload())
    {
        noway_assert(parent->OperGet() == oper);
        noway_assert(tree->IsMultiRegCall());
        GenTreeCall*         call         = tree->AsCall();
        GenTreeCopyOrReload* copyOrReload = parent->AsCopyOrReload();
        noway_assert(copyOrReload->GetRegNumByIdx(multiRegIdx) == REG_NA);
        copyOrReload->SetRegNumByIdx(refPosition->assignedReg(), multiRegIdx);
    }
    else
    {
        // Create the new node, with "tree" as its only child.
        var_types treeType = tree->TypeGet();

#ifdef FEATURE_SIMD
        // Check to see whether we need to move to a different register set.
        // This currently only happens in the case of SIMD vector types that are small enough (pointer size)
        // that they must be passed & returned in integer registers.
        // 'treeType' is the type of the register we are moving FROM,
        // and refPosition->registerAssignment is the mask for the register we are moving TO.
        // If they don't match, we need to reverse the type for the "move" node.

        if ((allRegs(treeType) & refPosition->registerAssignment) == 0)
        {
            treeType = (useFloatReg(treeType)) ? TYP_I_IMPL : TYP_SIMD8;
        }
#endif // FEATURE_SIMD

        GenTreeCopyOrReload* newNode = new (compiler, oper) GenTreeCopyOrReload(oper, treeType, tree);
        assert(refPosition->registerAssignment != RBM_NONE);
        newNode->SetRegNumByIdx(refPosition->assignedReg(), multiRegIdx);
        newNode->gtLsraInfo.isLsraAdded   = true;
        newNode->gtLsraInfo.isLocalDefUse = false;
        if (refPosition->copyReg)
        {
            // This is a TEMPORARY copy
            assert(isCandidateLocalRef(tree));
            newNode->gtFlags |= GTF_VAR_DEATH;
        }

        // Insert the copy/reload after the spilled node and replace the use of the original node with a use
        // of the copy/reload.
        blockRange.InsertAfter(tree, newNode);
        treeUse.ReplaceWith(compiler, newNode);
    }
}

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
//------------------------------------------------------------------------
// insertUpperVectorSaveAndReload: Insert code to save and restore the upper half of a vector that lives
//                                 in a callee-save register at the point of a kill (the upper half is
//                                 not preserved).
//
// Arguments:
//    tree              - This is the node around which we will insert the Save & Reload.
//                        It will be a call or some node that turns into a call.
//    refPosition       - The RefTypeUpperVectorSaveDef RefPosition.
//
void LinearScan::insertUpperVectorSaveAndReload(GenTreePtr tree, RefPosition* refPosition, BasicBlock* block)
{
    Interval* lclVarInterval = refPosition->getInterval()->relatedInterval;
    assert(lclVarInterval->isLocalVar == true);
    LclVarDsc* varDsc = compiler->lvaTable + lclVarInterval->varNum;
    assert(varDsc->lvType == LargeVectorType);
    regNumber lclVarReg = lclVarInterval->physReg;
    if (lclVarReg == REG_NA)
    {
        return;
    }

    assert((genRegMask(lclVarReg) & RBM_FLT_CALLEE_SAVED) != RBM_NONE);

    regNumber spillReg   = refPosition->assignedReg();
    bool      spillToMem = refPosition->spillAfter;

    LIR::Range& blockRange = LIR::AsRange(block);

    // First, insert the save as an embedded statement before the call.

    GenTreePtr saveLcl              = compiler->gtNewLclvNode(lclVarInterval->varNum, LargeVectorType);
    saveLcl->gtLsraInfo.isLsraAdded = true;
    saveLcl->gtRegNum               = lclVarReg;
    saveLcl->gtFlags |= GTF_REG_VAL;
    saveLcl->gtLsraInfo.isLocalDefUse = false;

    GenTreeSIMD* simdNode =
        new (compiler, GT_SIMD) GenTreeSIMD(LargeVectorSaveType, saveLcl, nullptr, SIMDIntrinsicUpperSave,
                                            varDsc->lvBaseType, genTypeSize(LargeVectorType));
    simdNode->gtLsraInfo.isLsraAdded = true;
    simdNode->gtRegNum               = spillReg;
    if (spillToMem)
    {
        simdNode->gtFlags |= GTF_SPILL;
    }

    blockRange.InsertBefore(tree, LIR::SeqTree(compiler, simdNode));

    // Now insert the restore after the call.

    GenTreePtr restoreLcl              = compiler->gtNewLclvNode(lclVarInterval->varNum, LargeVectorType);
    restoreLcl->gtLsraInfo.isLsraAdded = true;
    restoreLcl->gtRegNum               = lclVarReg;
    restoreLcl->gtFlags |= GTF_REG_VAL;
    restoreLcl->gtLsraInfo.isLocalDefUse = false;

    simdNode = new (compiler, GT_SIMD)
        GenTreeSIMD(LargeVectorType, restoreLcl, nullptr, SIMDIntrinsicUpperRestore, varDsc->lvBaseType, 32);
    simdNode->gtLsraInfo.isLsraAdded = true;
    simdNode->gtRegNum               = spillReg;
    if (spillToMem)
    {
        simdNode->gtFlags |= GTF_SPILLED;
    }

    blockRange.InsertAfter(tree, LIR::SeqTree(compiler, simdNode));
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
#ifdef _TARGET_X86_
    var_types returnType = compiler->tmpNormalizeType(compiler->info.compRetType);
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
#endif // _TARGET_X86_
    for (int i = 0; i < TYP_COUNT; i++)
    {
        if (var_types(i) != compiler->tmpNormalizeType(var_types(i)))
        {
            // Only normalized types should have anything in the maxSpill array.
            // We assume here that if type 'i' does not normalize to itself, then
            // nothing else normalizes to 'i', either.
            assert(maxSpill[i] == 0);
        }
        JITDUMP("  %s: %d\n", varTypeName(var_types(i)), maxSpill[i]);
        if (maxSpill[i] != 0)
        {
            compiler->tmpPreAllocateTemps(var_types(i), maxSpill[i]);
        }
    }
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

    if (refPosition->spillAfter || refPosition->reload ||
        (refPosition->AllocateIfProfitable() && refPosition->assignedReg() == REG_NA))
    {
        Interval* interval = refPosition->getInterval();
        if (!interval->isLocalVar)
        {
            // The tmp allocation logic 'normalizes' types to a small number of
            // types that need distinct stack locations from each other.
            // Those types are currently gc refs, byrefs, <= 4 byte non-GC items,
            // 8-byte non-GC items, and 16-byte or 32-byte SIMD vectors.
            // LSRA is agnostic to those choices but needs
            // to know what they are here.
            var_types typ;

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            if ((refType == RefTypeUpperVectorSaveDef) || (refType == RefTypeUpperVectorSaveUse))
            {
                typ = LargeVectorSaveType;
            }
            else
#endif // !FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            {
                GenTreePtr treeNode = refPosition->treeNode;
                if (treeNode == nullptr)
                {
                    assert(RefTypeIsUse(refType));
                    treeNode = interval->firstRefPosition->treeNode;
                }
                assert(treeNode != nullptr);

                // In case of multi-reg call nodes, we need to use the type
                // of the return register given by multiRegIdx of the refposition.
                if (treeNode->IsMultiRegCall())
                {
                    ReturnTypeDesc* retTypeDesc = treeNode->AsCall()->GetReturnTypeDesc();
                    typ                         = retTypeDesc->GetReturnRegType(refPosition->getMultiRegIdx());
                }
                else
                {
                    typ = treeNode->TypeGet();
                }
                typ = compiler->tmpNormalizeType(typ);
            }

            if (refPosition->spillAfter && !refPosition->reload)
            {
                currentSpill[typ]++;
                if (currentSpill[typ] > maxSpill[typ])
                {
                    maxSpill[typ] = currentSpill[typ];
                }
            }
            else if (refPosition->reload)
            {
                assert(currentSpill[typ] > 0);
                currentSpill[typ]--;
            }
            else if (refPosition->AllocateIfProfitable() && refPosition->assignedReg() == REG_NA)
            {
                // A spill temp not getting reloaded into a reg because it is
                // marked as allocate if profitable and getting used from its
                // memory location.  To properly account max spill for typ we
                // decrement spill count.
                assert(RefTypeIsUse(refType));
                assert(currentSpill[typ] > 0);
                currentSpill[typ]--;
            }
            JITDUMP("  Max spill for %s is %d\n", varTypeName(typ), maxSpill[typ]);
        }
    }
}

// This is the final phase of register allocation.  It writes the register assignments to
// the tree, and performs resolution across joins and backedges.
//
void LinearScan::resolveRegisters()
{
    // Iterate over the tree and the RefPositions in lockstep
    //  - annotate the tree with register assignments by setting gtRegNum or gtRegPair (for longs)
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
    for (unsigned lclNum = 0; lclNum < compiler->lvaCount; lclNum++)
    {
        localVarIntervals[lclNum]->recentRefPosition = nullptr;
        localVarIntervals[lclNum]->isActive          = false;
    }

    // handle incoming arguments and special temps
    auto currentRefPosition = refPositions.begin();

    VarToRegMap entryVarToRegMap = inVarToRegMaps[compiler->fgFirstBB->bbNum];
    while (currentRefPosition != refPositions.end() &&
           (currentRefPosition->refType == RefTypeParamDef || currentRefPosition->refType == RefTypeZeroInit))
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
        entryVarToRegMap[varIndex] = reg;
        ++currentRefPosition;
    }

    JITDUMP("------------------------\n");
    JITDUMP("WRITING BACK ASSIGNMENTS\n");
    JITDUMP("------------------------\n");

    BasicBlock* insertionBlock = compiler->fgFirstBB;
    GenTreePtr  insertionPoint = LIR::AsRange(insertionBlock).FirstNonPhiNode();

    // write back assignments
    for (block = startBlockSequence(); block != nullptr; block = moveToNextBlock())
    {
        assert(curBBNum == block->bbNum);

#ifdef DEBUG
        if (VERBOSE)
        {
            block->dspBlockHeader(compiler);
            currentRefPosition->dump();
        }
#endif // DEBUG

        // Record the var locations at the start of this block.
        // (If it's fgFirstBB, we've already done that above, see entryVarToRegMap)

        curBBStartLocation = currentRefPosition->nodeLocation;
        if (block != compiler->fgFirstBB)
        {
            processBlockStartLocations(block, false);
        }

        // Handle the DummyDefs, updating the incoming var location.
        for (; currentRefPosition != refPositions.end() && currentRefPosition->refType == RefTypeDummyDef;
             ++currentRefPosition)
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

        // The next RefPosition should be for the block.  Move past it.
        assert(currentRefPosition != refPositions.end());
        assert(currentRefPosition->refType == RefTypeBB);
        ++currentRefPosition;

        // Handle the RefPositions for the block
        for (; currentRefPosition != refPositions.end() && currentRefPosition->refType != RefTypeBB &&
               currentRefPosition->refType != RefTypeDummyDef;
             ++currentRefPosition)
        {
            currentLocation = currentRefPosition->nodeLocation;
            JITDUMP("current : ");
            DBEXEC(VERBOSE, currentRefPosition->dump());

            // Ensure that the spill & copy info is valid.
            // First, if it's reload, it must not be copyReg or moveReg
            assert(!currentRefPosition->reload || (!currentRefPosition->copyReg && !currentRefPosition->moveReg));
            // If it's copyReg it must not be moveReg, and vice-versa
            assert(!currentRefPosition->copyReg || !currentRefPosition->moveReg);

            switch (currentRefPosition->refType)
            {
#ifdef FEATURE_SIMD
                case RefTypeUpperVectorSaveUse:
                case RefTypeUpperVectorSaveDef:
#endif // FEATURE_SIMD
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
            if (currentRefPosition->refType == RefTypeUpperVectorSaveDef)
            {
                // The treeNode must be a call, and this must be a RefPosition for a LargeVectorType LocalVar.
                // If the LocalVar is in a callee-save register, we are going to spill its upper half around the call.
                // If we have allocated a register to spill it to, we will use that; otherwise, we will spill it
                // to the stack.  We can use as a temp register any non-arg caller-save register.
                noway_assert(treeNode != nullptr);
                currentRefPosition->referent->recentRefPosition = currentRefPosition;
                insertUpperVectorSaveAndReload(treeNode, currentRefPosition, block);
            }
            else if (currentRefPosition->refType == RefTypeUpperVectorSaveUse)
            {
                continue;
            }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

            // Most uses won't actually need to be recorded (they're on the def).
            // In those cases, treeNode will be nullptr.
            if (treeNode == nullptr)
            {
                // This is either a use, a dead def, or a field of a struct
                Interval* interval = currentRefPosition->getInterval();
                assert(currentRefPosition->refType == RefTypeUse ||
                       currentRefPosition->registerAssignment == RBM_NONE || interval->isStructField);

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
                    varDsc->lvRegNum = REG_STK;
                }

                JITDUMP("No tree node to write back to\n");
                continue;
            }

            DBEXEC(VERBOSE, lsraDispNode(treeNode, LSRA_DUMP_REFPOS, true));
            JITDUMP("\n");

            LsraLocation loc = treeNode->gtLsraInfo.loc;
            JITDUMP("curr = %u mapped = %u", currentLocation, loc);
            assert(treeNode->IsLocal() || currentLocation == loc || currentLocation == loc + 1);

            if (currentRefPosition->isIntervalRef() && currentRefPosition->getInterval()->isInternal)
            {
                JITDUMP(" internal");
                GenTreePtr indNode = nullptr;
                if (treeNode->OperIsIndir())
                {
                    indNode = treeNode;
                    JITDUMP(" allocated at GT_IND");
                }
                if (indNode != nullptr)
                {
                    GenTreePtr addrNode = indNode->gtOp.gtOp1->gtEffectiveVal();
                    if (addrNode->OperGet() != GT_ARR_ELEM)
                    {
                        addrNode->gtRsvdRegs |= currentRefPosition->registerAssignment;
                        JITDUMP(", recorded on addr");
                    }
                }
                if (treeNode->OperGet() == GT_ARR_ELEM)
                {
                    // TODO-Review: See WORKAROUND ALERT in buildRefPositionsForNode()
                    GenTreePtr firstIndexTree = treeNode->gtArrElem.gtArrInds[0]->gtEffectiveVal();
                    assert(firstIndexTree != nullptr);
                    if (firstIndexTree->IsLocal() && (firstIndexTree->gtFlags & GTF_VAR_DEATH) == 0)
                    {
                        // Record the LAST internal interval
                        // (Yes, this naively just records each one, but the next will replace it;
                        // I'd fix this if it wasn't just a temporary fix)
                        if (currentRefPosition->refType == RefTypeDef)
                        {
                            JITDUMP(" allocated at GT_ARR_ELEM, recorded on firstIndex V%02u");
                            firstIndexTree->gtRsvdRegs = (regMaskSmall)currentRefPosition->registerAssignment;
                        }
                    }
                }
                treeNode->gtRsvdRegs |= currentRefPosition->registerAssignment;
            }
            else
            {
                writeRegisters(currentRefPosition, treeNode);

                if (treeNode->IsLocal() && currentRefPosition->getInterval()->isLocalVar)
                {
                    resolveLocalRef(block, treeNode, currentRefPosition);
                }

                // Mark spill locations on temps
                // (local vars are handled in resolveLocalRef, above)
                // Note that the tree node will be changed from GTF_SPILL to GTF_SPILLED
                // in codegen, taking care of the "reload" case for temps
                else if (currentRefPosition->spillAfter || (currentRefPosition->nextRefPosition != nullptr &&
                                                            currentRefPosition->nextRefPosition->moveReg))
                {
                    if (treeNode != nullptr && currentRefPosition->isIntervalRef())
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

                            // In case of multi-reg call node, also set spill flag on the
                            // register specified by multi-reg index of current RefPosition.
                            // Note that the spill flag on treeNode indicates that one or
                            // more its allocated registers are in that state.
                            if (treeNode->IsMultiRegCall())
                            {
                                GenTreeCall* call = treeNode->AsCall();
                                call->SetRegSpillFlagByIdx(GTF_SPILL, currentRefPosition->getMultiRegIdx());
                            }
                        }

                        // If the value is reloaded or moved to a different register, we need to insert
                        // a node to hold the register to which it should be reloaded
                        RefPosition* nextRefPosition = currentRefPosition->nextRefPosition;
                        assert(nextRefPosition != nullptr);
                        if (INDEBUG(alwaysInsertReload() ||)
                                nextRefPosition->assignedReg() != currentRefPosition->assignedReg())
                        {
                            if (nextRefPosition->assignedReg() != REG_NA)
                            {
                                insertCopyOrReload(block, treeNode, currentRefPosition->getMultiRegIdx(),
                                                   nextRefPosition);
                            }
                            else
                            {
                                assert(nextRefPosition->AllocateIfProfitable());

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

                    // We should never have to "spill after" a temp use, since
                    // they're single use
                    else
                    {
                        unreached();
                    }
                }
            }
            JITDUMP("\n");
        }

        processBlockEndLocations(block);
    }

#ifdef DEBUG
    if (VERBOSE)
    {
        printf("-----------------------\n");
        printf("RESOLVING BB BOUNDARIES\n");
        printf("-----------------------\n");

        printf("Prior to Resolution\n");
        foreach_block(compiler, block)
        {
            printf("\nBB%02u use def in out\n", block->bbNum);
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
            varDsc->lvRegNum = REG_STK;
        }
        else
        {
            Interval* interval = getIntervalForLocalVar(lclNum);

            // Determine initial position for parameters

            if (varDsc->lvIsParam)
            {
                regMaskTP initialRegMask = interval->firstRefPosition->registerAssignment;
                regNumber initialReg     = (initialRegMask == RBM_NONE || interval->firstRefPosition->spillAfter)
                                           ? REG_STK
                                           : genRegNumFromMask(initialRegMask);
                regNumber sourceReg = (varDsc->lvIsRegArg) ? varDsc->lvArgReg : REG_STK;

#ifdef _TARGET_ARM_
                if (varTypeIsMultiReg(varDsc))
                {
                    // TODO-ARM-NYI: Map the hi/lo intervals back to lvRegNum and lvOtherReg (these should NYI before
                    // this)
                    assert(!"Multi-reg types not yet supported");
                }
                else
#endif // _TARGET_ARM_
                {
                    varDsc->lvArgInitReg = initialReg;
                    JITDUMP("  Set V%02u argument initial register to %s\n", lclNum, getRegName(initialReg));
                }
                if (!varDsc->lvIsRegArg)
                {
                    // stack arg
                    if (compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc))
                    {
                        if (sourceReg != initialReg)
                        {
                            // The code generator won't initialize struct
                            // fields, so we have to do that if it's not already
                            // where it belongs.
                            assert(interval->isStructField);
                            JITDUMP("  Move struct field param V%02u from %s to %s\n", lclNum, getRegName(sourceReg),
                                    getRegName(initialReg));
                            insertMove(insertionBlock, insertionPoint, lclNum, sourceReg, initialReg);
                        }
                    }
                }
            }

            // If lvRegNum is REG_STK, that means that either no register
            // was assigned, or (more likely) that the same register was not
            // used for all references.  In that case, codegen gets the register
            // from the tree node.
            if (varDsc->lvRegNum == REG_STK || interval->isSpilled || interval->isSplit)
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
                    if (varDsc->lvRefCnt == 0)
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
                        // Either this RefPosition is spilled, or it is not a "real" def or use
                        assert(firstRefPosition->spillAfter ||
                               (firstRefPosition->refType != RefTypeDef && firstRefPosition->refType != RefTypeUse));
                        varDsc->lvRegNum = REG_STK;
                    }
                    else
                    {
                        varDsc->lvRegNum = firstRefPosition->assignedReg();
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
                regMaskTP registerAssignment = genRegMask(varDsc->lvRegNum);
                assert(!interval->isSpilled && !interval->isSplit);
                RefPosition* refPosition = interval->firstRefPosition;
                assert(refPosition != nullptr);

                while (refPosition != nullptr)
                {
                    // All RefPositions must match, except for dead definitions,
                    // copyReg/moveReg and RefTypeExpUse positions
                    if (refPosition->registerAssignment != RBM_NONE && !refPosition->copyReg && !refPosition->moveReg &&
                        refPosition->refType != RefTypeExpUse)
                    {
                        assert(refPosition->registerAssignment == registerAssignment);
                    }
                    refPosition = refPosition->nextRefPosition;
                }
#endif // DEBUG
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
    BasicBlock* block, GenTreePtr insertionPoint, unsigned lclNum, regNumber fromReg, regNumber toReg)
{
    LclVarDsc* varDsc = compiler->lvaTable + lclNum;
    // One or both MUST be a register
    assert(fromReg != REG_STK || toReg != REG_STK);
    // They must not be the same register.
    assert(fromReg != toReg);

    // This var can't be marked lvRegister now
    varDsc->lvRegNum = REG_STK;

    var_types lclTyp = varDsc->TypeGet();
    if (varDsc->lvNormalizeOnStore())
    {
        lclTyp = genActualType(lclTyp);
    }
    GenTreePtr src              = compiler->gtNewLclvNode(lclNum, lclTyp);
    src->gtLsraInfo.isLsraAdded = true;
    GenTreePtr top;

    // If we are moving from STK to reg, mark the lclVar nodes with GTF_SPILLED
    // Otherwise, if we are moving from reg to stack, mark it as GTF_SPILL
    // Finally, for a reg-to-reg move, generate a GT_COPY

    top = src;
    if (fromReg == REG_STK)
    {
        src->gtFlags |= GTF_SPILLED;
        src->gtRegNum = toReg;
    }
    else if (toReg == REG_STK)
    {
        src->gtFlags |= GTF_SPILL;
        src->SetInReg();
        src->gtRegNum = fromReg;
    }
    else
    {
        top = new (compiler, GT_COPY) GenTreeCopyOrReload(GT_COPY, varDsc->TypeGet(), src);
        // This is the new home of the lclVar - indicate that by clearing the GTF_VAR_DEATH flag.
        // Note that if src is itself a lastUse, this will have no effect.
        top->gtFlags &= ~(GTF_VAR_DEATH);
        src->gtRegNum = fromReg;
        src->SetInReg();
        top->gtRegNum                 = toReg;
        src->gtNext                   = top;
        top->gtPrev                   = src;
        src->gtLsraInfo.isLocalDefUse = false;
        top->gtLsraInfo.isLsraAdded   = true;
    }
    top->gtLsraInfo.isLocalDefUse = true;

    LIR::Range  treeRange  = LIR::SeqTree(compiler, top);
    LIR::Range& blockRange = LIR::AsRange(block);

    if (insertionPoint != nullptr)
    {
        blockRange.InsertBefore(insertionPoint, std::move(treeRange));
    }
    else
    {
        // Put the copy at the bottom
        // If there's a branch, make an embedded statement that executes just prior to the branch
        if (block->bbJumpKind == BBJ_COND || block->bbJumpKind == BBJ_SWITCH)
        {
            noway_assert(!blockRange.IsEmpty());

            GenTree* branch = blockRange.LastNode();
            assert(branch->OperGet() == GT_JTRUE || branch->OperGet() == GT_SWITCH_TABLE ||
                   branch->OperGet() == GT_SWITCH);

            blockRange.InsertBefore(branch, std::move(treeRange));
        }
        else
        {
            assert(block->bbJumpKind == BBJ_NONE || block->bbJumpKind == BBJ_ALWAYS);
            blockRange.InsertAtEnd(std::move(treeRange));
        }
    }
}

void LinearScan::insertSwap(
    BasicBlock* block, GenTreePtr insertionPoint, unsigned lclNum1, regNumber reg1, unsigned lclNum2, regNumber reg2)
{
#ifdef DEBUG
    if (VERBOSE)
    {
        const char* insertionPointString = "top";
        if (insertionPoint == nullptr)
        {
            insertionPointString = "bottom";
        }
        printf("   BB%02u %s: swap V%02u in %s with V%02u in %s\n", block->bbNum, insertionPointString, lclNum1,
               getRegName(reg1), lclNum2, getRegName(reg2));
    }
#endif // DEBUG

    LclVarDsc* varDsc1 = compiler->lvaTable + lclNum1;
    LclVarDsc* varDsc2 = compiler->lvaTable + lclNum2;
    assert(reg1 != REG_STK && reg1 != REG_NA && reg2 != REG_STK && reg2 != REG_NA);

    GenTreePtr lcl1                = compiler->gtNewLclvNode(lclNum1, varDsc1->TypeGet());
    lcl1->gtLsraInfo.isLsraAdded   = true;
    lcl1->gtLsraInfo.isLocalDefUse = false;
    lcl1->SetInReg();
    lcl1->gtRegNum = reg1;

    GenTreePtr lcl2                = compiler->gtNewLclvNode(lclNum2, varDsc2->TypeGet());
    lcl2->gtLsraInfo.isLsraAdded   = true;
    lcl2->gtLsraInfo.isLocalDefUse = false;
    lcl2->SetInReg();
    lcl2->gtRegNum = reg2;

    GenTreePtr swap                = compiler->gtNewOperNode(GT_SWAP, TYP_VOID, lcl1, lcl2);
    swap->gtLsraInfo.isLsraAdded   = true;
    swap->gtLsraInfo.isLocalDefUse = false;
    swap->gtRegNum                 = REG_NA;

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
            assert(branch->OperGet() == GT_JTRUE || branch->OperGet() == GT_SWITCH_TABLE ||
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

    regMaskTP freeRegs = allRegs(type);
#ifdef DEBUG
    if (getStressLimitRegs() == LSRA_LIMIT_SMALL_SET)
    {
        return REG_NA;
    }
#endif // DEBUG
    INDEBUG(freeRegs = stressLimitRegs(nullptr, freeRegs));

    // We are only interested in the variables that are live-in to the "to" block.
    VARSET_ITER_INIT(compiler, iter, toBlock->bbLiveIn, varIndex);
    while (iter.NextElem(compiler, &varIndex) && freeRegs != RBM_NONE)
    {
        regNumber fromReg = fromVarToRegMap[varIndex];
        regNumber toReg   = toVarToRegMap[varIndex];
        assert(fromReg != REG_NA && toReg != REG_NA);
        if (fromReg != REG_STK)
        {
            freeRegs &= ~genRegMask(fromReg);
        }
        if (toReg != REG_STK)
        {
            freeRegs &= ~genRegMask(toReg);
        }
    }
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
    BasicBlock* block, GenTreePtr insertionPoint, Interval* interval, regNumber toReg, regNumber fromReg)
{
#ifdef DEBUG
    const char* insertionPointString = "top";
#endif // DEBUG
    if (insertionPoint == nullptr)
    {
#ifdef DEBUG
        insertionPointString = "bottom";
#endif // DEBUG
    }

    JITDUMP("   BB%02u %s: move V%02u from ", block->bbNum, insertionPointString, interval->varNum);
    JITDUMP("%s to %s", getRegName(fromReg), getRegName(toReg));

    insertMove(block, insertionPoint, interval->varNum, fromReg, toReg);
    if (fromReg == REG_STK || toReg == REG_STK)
    {
        interval->isSpilled = true;
    }
    else
    {
        interval->isSplit = true;
    }
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
    VARSET_TP VARSET_INIT_NOCOPY(sameResolutionSet, VarSetOps::MakeEmpty(compiler));
    VARSET_TP VARSET_INIT_NOCOPY(sameLivePathsSet, VarSetOps::MakeEmpty(compiler));
    VARSET_TP VARSET_INIT_NOCOPY(singleTargetSet, VarSetOps::MakeEmpty(compiler));
    VARSET_TP VARSET_INIT_NOCOPY(diffResolutionSet, VarSetOps::MakeEmpty(compiler));

    // Get the outVarToRegMap for this block
    VarToRegMap outVarToRegMap = getOutVarToRegMap(block->bbNum);
    unsigned    succCount      = block->NumSucc(compiler);
    assert(succCount > 1);
    VarToRegMap firstSuccInVarToRegMap = nullptr;
    BasicBlock* firstSucc              = nullptr;

    // First, determine the live regs at the end of this block so that we know what regs are
    // available to copy into.
    regMaskTP liveOutRegs = RBM_NONE;
    VARSET_ITER_INIT(compiler, iter1, block->bbLiveOut, varIndex1);
    while (iter1.NextElem(compiler, &varIndex1))
    {
        unsigned  varNum  = compiler->lvaTrackedToVarNum[varIndex1];
        regNumber fromReg = getVarReg(outVarToRegMap, varNum);
        if (fromReg != REG_STK)
        {
            liveOutRegs |= genRegMask(fromReg);
        }
    }

    // Next, if this blocks ends with a switch table, we have to make sure not to copy
    // into the registers that it uses.
    regMaskTP switchRegs = RBM_NONE;
    if (block->bbJumpKind == BBJ_SWITCH)
    {
        // At this point, Lowering has transformed any non-switch-table blocks into
        // cascading ifs.
        GenTree* switchTable = LIR::AsRange(block).LastNode();
        assert(switchTable != nullptr && switchTable->OperGet() == GT_SWITCH_TABLE);

        switchRegs   = switchTable->gtRsvdRegs;
        GenTree* op1 = switchTable->gtGetOp1();
        GenTree* op2 = switchTable->gtGetOp2();
        noway_assert(op1 != nullptr && op2 != nullptr);
        assert(op1->gtRegNum != REG_NA && op2->gtRegNum != REG_NA);
        switchRegs |= genRegMask(op1->gtRegNum);
        switchRegs |= genRegMask(op2->gtRegNum);
    }

    VarToRegMap sameVarToRegMap = sharedCriticalVarToRegMap;
    regMaskTP   sameWriteRegs   = RBM_NONE;
    regMaskTP   diffReadRegs    = RBM_NONE;

    // For each var, classify them as:
    // - in the same register at the end of this block and at each target (no resolution needed)
    // - in different registers at different targets (resolve separately):
    //     diffResolutionSet
    // - in the same register at each target at which it's live, but different from the end of
    //   this block.  We may be able to resolve these as if it is "join", but only if they do not
    //   write to any registers that are read by those in the diffResolutionSet:
    //     sameResolutionSet

    VARSET_ITER_INIT(compiler, iter, block->bbLiveOut, varIndex);
    while (iter.NextElem(compiler, &varIndex))
    {
        unsigned  varNum              = compiler->lvaTrackedToVarNum[varIndex];
        regNumber fromReg             = getVarReg(outVarToRegMap, varNum);
        bool      isMatch             = true;
        bool      isSame              = false;
        bool      maybeSingleTarget   = false;
        bool      maybeSameLivePaths  = false;
        bool      liveOnlyAtSplitEdge = true;
        regNumber sameToReg           = REG_NA;
        for (unsigned succIndex = 0; succIndex < succCount; succIndex++)
        {
            BasicBlock* succBlock = block->GetSucc(succIndex, compiler);
            if (!VarSetOps::IsMember(compiler, succBlock->bbLiveIn, varIndex))
            {
                maybeSameLivePaths = true;
                continue;
            }
            else if (liveOnlyAtSplitEdge)
            {
                // Is the var live only at those target blocks which are connected by a split edge to this block
                liveOnlyAtSplitEdge = ((succBlock->bbPreds->flNext == nullptr) && (succBlock != compiler->fgFirstBB));
            }

            regNumber toReg = getVarReg(getInVarToRegMap(succBlock->bbNum), varNum);
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
            regMaskTP sameToRegMask = genRegMask(sameToReg);
            if (maybeSameLivePaths &&
                (((sameToRegMask & liveOutRegs) != RBM_NONE) || ((sameToRegMask & sameWriteRegs) != RBM_NONE)))
            {
                sameToReg = REG_NA;
            }
            // If this register is used by a switch table at the end of the block, we can't do the copy
            // in this block (since we can't insert it after the switch).
            if ((sameToRegMask & switchRegs) != RBM_NONE)
            {
                sameToReg = REG_NA;
            }

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
            VarSetOps::AddElemD(compiler, diffResolutionSet, varIndex);
            if (fromReg != REG_STK)
            {
                diffReadRegs |= genRegMask(fromReg);
            }
        }
        else if (sameToReg != fromReg)
        {
            VarSetOps::AddElemD(compiler, sameResolutionSet, varIndex);
            sameVarToRegMap[varIndex] = sameToReg;
            if (sameToReg != REG_STK)
            {
                sameWriteRegs |= genRegMask(sameToReg);
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
            bool        needsResolution   = false;
            VarToRegMap succInVarToRegMap = getInVarToRegMap(succBlock->bbNum);
            VARSET_TP   VARSET_INIT_NOCOPY(edgeResolutionSet,
                                         VarSetOps::Intersection(compiler, diffResolutionSet, succBlock->bbLiveIn));
            VARSET_ITER_INIT(compiler, iter, edgeResolutionSet, varIndex);
            while (iter.NextElem(compiler, &varIndex))
            {
                unsigned  varNum   = compiler->lvaTrackedToVarNum[varIndex];
                Interval* interval = getIntervalForLocalVar(varNum);
                regNumber fromReg  = getVarReg(outVarToRegMap, varNum);
                regNumber toReg    = getVarReg(succInVarToRegMap, varNum);

                if (fromReg == toReg)
                {
                    VarSetOps::RemoveElemD(compiler, edgeResolutionSet, varIndex);
                }
            }
            if (!VarSetOps::IsEmpty(compiler, edgeResolutionSet))
            {
                resolveEdge(block, succBlock, ResolveCritical, edgeResolutionSet);
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

    BasicBlock *block, *prevBlock = nullptr;

    // Handle all the critical edges first.
    // We will try to avoid resolution across critical edges in cases where all the critical-edge
    // targets of a block have the same home.  We will then split the edges only for the
    // remaining mismatches.  We visit the out-edges, as that allows us to share the moves that are
    // common among allt he targets.

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

    prevBlock = nullptr;
    foreach_block(compiler, block)
    {
        if (block->bbNum > bbNumMaxBeforeResolution)
        {
            // This is a new block added during resolution - we don't need to visit these now.
            continue;
        }

        unsigned    succCount       = block->NumSucc(compiler);
        flowList*   preds           = block->bbPreds;
        BasicBlock* uniquePredBlock = block->GetUniquePred(compiler);

        // First, if this block has a single predecessor,
        // we may need resolution at the beginning of this block.
        // This may be true even if it's the block we used for starting locations,
        // if a variable was spilled.
        if (!VarSetOps::IsEmpty(compiler, block->bbLiveIn))
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
                resolveEdge(uniquePredBlock, block, ResolveSplit, block->bbLiveIn);
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
                resolveEdge(block, succBlock, ResolveJoin, succBlock->bbLiveIn);
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
            BasicBlock* predBlock       = pred->flBlock;
            VarToRegMap fromVarToRegMap = getOutVarToRegMap(predBlock->bbNum);
            VARSET_ITER_INIT(compiler, iter, block->bbLiveIn, varIndex);
            while (iter.NextElem(compiler, &varIndex))
            {
                unsigned  varNum  = compiler->lvaTrackedToVarNum[varIndex];
                regNumber fromReg = getVarReg(fromVarToRegMap, varNum);
                regNumber toReg   = getVarReg(toVarToRegMap, varNum);
                if (fromReg != toReg)
                {
                    Interval* interval = getIntervalForLocalVar(varNum);
                    if (!foundMismatch)
                    {
                        foundMismatch = true;
                        printf("Found mismatched var locations after resolution!\n");
                    }
                    printf(" V%02u: BB%02u to BB%02u: ", varNum, predBlock->bbNum, block->bbNum);
                    printf("%s to %s\n", getRegName(fromReg), getRegName(toReg));
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
            break;
        default:
            unreached();
            break;
    }

#ifndef _TARGET_XARCH_
    // We record tempregs for beginning and end of each block.
    // For amd64/x86 we only need a tempReg for float - we'll use xchg for int.
    // TODO-Throughput: It would be better to determine the tempRegs on demand, but the code below
    // modifies the varToRegMaps so we don't have all the correct registers at the time
    // we need to get the tempReg.
    regNumber tempRegInt =
        (resolveType == ResolveSharedCritical) ? REG_NA : getTempRegForResolution(fromBlock, toBlock, TYP_INT);
#endif // !_TARGET_XARCH_
    regNumber tempRegFlt = REG_NA;
    if ((compiler->compFloatingPointUsed) && (resolveType != ResolveSharedCritical))
    {
        tempRegFlt = getTempRegForResolution(fromBlock, toBlock, TYP_FLOAT);
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
    Interval* sourceIntervals[REG_COUNT] = {nullptr};

    // Intervals for vars that need to be loaded from the stack
    Interval* stackToRegIntervals[REG_COUNT] = {nullptr};

    // Get the starting insertion point for the "to" resolution
    GenTreePtr insertionPoint = nullptr;
    if (resolveType == ResolveSplit || resolveType == ResolveCritical)
    {
        insertionPoint = LIR::AsRange(block).FirstNonPhiNode();
    }

    // First:
    //   - Perform all moves from reg to stack (no ordering needed on these)
    //   - For reg to reg moves, record the current location, associating their
    //     source location with the target register they need to go into
    //   - For stack to reg moves (done last, no ordering needed between them)
    //     record the interval associated with the target reg
    // TODO-Throughput: We should be looping over the liveIn and liveOut registers, since
    // that will scale better than the live variables

    VARSET_ITER_INIT(compiler, iter, liveSet, varIndex);
    while (iter.NextElem(compiler, &varIndex))
    {
        unsigned  varNum    = compiler->lvaTrackedToVarNum[varIndex];
        bool      isSpilled = false;
        Interval* interval  = getIntervalForLocalVar(varNum);
        regNumber fromReg   = getVarReg(fromVarToRegMap, varNum);
        regNumber toReg     = getVarReg(toVarToRegMap, varNum);
        if (fromReg == toReg)
        {
            continue;
        }

        // For Critical edges, the location will not change on either side of the edge,
        // since we'll add a new block to do the move.
        if (resolveType == ResolveSplit)
        {
            toVarToRegMap[varIndex] = fromReg;
        }
        else if (resolveType == ResolveJoin || resolveType == ResolveSharedCritical)
        {
            fromVarToRegMap[varIndex] = toReg;
        }

        assert(fromReg < UCHAR_MAX && toReg < UCHAR_MAX);

        bool done = false;

        if (fromReg != toReg)
        {
            if (fromReg == REG_STK)
            {
                stackToRegIntervals[toReg] = interval;
                targetRegsFromStack |= genRegMask(toReg);
            }
            else if (toReg == REG_STK)
            {
                // Do the reg to stack moves now
                addResolution(block, insertionPoint, interval, REG_STK, fromReg);
                JITDUMP(" (%s)\n", resolveTypeName[resolveType]);
            }
            else
            {
                location[fromReg]        = (regNumberSmall)fromReg;
                source[toReg]            = (regNumberSmall)fromReg;
                sourceIntervals[fromReg] = interval;
                targetRegsToDo |= genRegMask(toReg);
            }
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
            targetRegsReady |= targetRegMask;
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
            regNumber sourceReg = (regNumber)source[targetReg];
            regNumber fromReg   = (regNumber)location[sourceReg];
            assert(fromReg < UCHAR_MAX && sourceReg < UCHAR_MAX);
            Interval* interval = sourceIntervals[sourceReg];
            assert(interval != nullptr);
            addResolution(block, insertionPoint, interval, targetReg, fromReg);
            JITDUMP(" (%s)\n", resolveTypeName[resolveType]);
            sourceIntervals[sourceReg] = nullptr;
            location[sourceReg]        = REG_NA;

            // Do we have a free targetReg?
            if (fromReg == sourceReg && source[fromReg] != REG_NA)
            {
                regMaskTP fromRegMask = genRegMask(fromReg);
                targetRegsReady |= fromRegMask;
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
                    tempReg = tempRegFlt;
                }
#ifdef _TARGET_XARCH_
                else
                {
                    useSwap = true;
                }
#else  // !_TARGET_XARCH_
                else
                {
                    tempReg = tempRegInt;
                }
#endif // !_TARGET_XARCH_
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
                    }
                    else
                    {
                        // Spill "targetReg" to the stack and add its eventual target (otherTargetReg)
                        // to "targetRegsFromStack", which will be handled below.
                        // NOTE: This condition is very rare.  Setting COMPlus_JitStressRegs=0x203
                        // has been known to trigger it in JIT SH.

                        // First, spill "otherInterval" from targetReg to the stack.
                        Interval* otherInterval = sourceIntervals[source[otherTargetReg]];
                        addResolution(block, insertionPoint, otherInterval, REG_STK, targetReg);
                        JITDUMP(" (%s)\n", resolveTypeName[resolveType]);
                        location[source[otherTargetReg]] = REG_STK;

                        // Now, move the interval that is going to targetReg, and add its "fromReg" to
                        // "targetRegsReady".
                        addResolution(block, insertionPoint, sourceIntervals[sourceReg], targetReg, fromReg);
                        JITDUMP(" (%s)\n", resolveTypeName[resolveType]);
                        location[sourceReg] = REG_NA;
                        targetRegsReady |= genRegMask(fromReg);
                    }
                    targetRegsToDo &= ~targetRegMask;
                }
                else
                {
                    compiler->codeGen->regSet.rsSetRegsModified(genRegMask(tempReg) DEBUGARG(dumpTerse));
                    assert(sourceIntervals[targetReg] != nullptr);
                    addResolution(block, insertionPoint, sourceIntervals[targetReg], tempReg, targetReg);
                    JITDUMP(" (%s)\n", resolveTypeName[resolveType]);
                    location[targetReg] = (regNumberSmall)tempReg;
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

void TreeNodeInfo::Initialize(LinearScan* lsra, GenTree* node, LsraLocation location)
{
    regMaskTP dstCandidates;

    // if there is a reg indicated on the tree node, use that for dstCandidates
    // the exception is the NOP, which sometimes show up around late args.
    // TODO-Cleanup: get rid of those NOPs.
    if (node->gtRegNum == REG_NA || node->gtOper == GT_NOP)
    {
        dstCandidates = lsra->allRegs(node->TypeGet());
    }
    else
    {
        dstCandidates = genRegMask(node->gtRegNum);
    }

    internalIntCount      = 0;
    internalFloatCount    = 0;
    isLocalDefUse         = false;
    isHelperCallWithKills = false;
    isLsraAdded           = false;
    definesAnyRegisters   = false;

    setDstCandidates(lsra, dstCandidates);
    srcCandsIndex = dstCandsIndex;

    setInternalCandidates(lsra, lsra->allRegs(TYP_INT));

    loc = location;
#ifdef DEBUG
    isInitialized = true;
#endif

    assert(IsValid(lsra));
}

regMaskTP TreeNodeInfo::getSrcCandidates(LinearScan* lsra)
{
    return lsra->GetRegMaskForIndex(srcCandsIndex);
}

void TreeNodeInfo::setSrcCandidates(LinearScan* lsra, regMaskTP mask)
{
    LinearScan::RegMaskIndex i = lsra->GetIndexForRegMask(mask);
    assert(FitsIn<unsigned char>(i));
    srcCandsIndex = (unsigned char)i;
}

regMaskTP TreeNodeInfo::getDstCandidates(LinearScan* lsra)
{
    return lsra->GetRegMaskForIndex(dstCandsIndex);
}

void TreeNodeInfo::setDstCandidates(LinearScan* lsra, regMaskTP mask)
{
    LinearScan::RegMaskIndex i = lsra->GetIndexForRegMask(mask);
    assert(FitsIn<unsigned char>(i));
    dstCandsIndex = (unsigned char)i;
}

regMaskTP TreeNodeInfo::getInternalCandidates(LinearScan* lsra)
{
    return lsra->GetRegMaskForIndex(internalCandsIndex);
}

void TreeNodeInfo::setInternalCandidates(LinearScan* lsra, regMaskTP mask)
{
    LinearScan::RegMaskIndex i = lsra->GetIndexForRegMask(mask);
    assert(FitsIn<unsigned char>(i));
    internalCandsIndex = (unsigned char)i;
}

void TreeNodeInfo::addInternalCandidates(LinearScan* lsra, regMaskTP mask)
{
    LinearScan::RegMaskIndex i = lsra->GetIndexForRegMask(lsra->GetRegMaskForIndex(internalCandsIndex) | mask);
    assert(FitsIn<unsigned char>(i));
    internalCandsIndex = (unsigned char)i;
}

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

void RefPosition::dump()
{
    printf("<RefPosition #%-3u @%-3u", rpNum, nodeLocation);

    if (nextRefPosition)
    {
        printf(" ->#%-3u", nextRefPosition->rpNum);
    }

    printf(" %s ", getRefTypeName(refType));

    if (this->isPhysRegRef)
    {
        this->getReg()->tinyDump();
    }
    else if (getInterval())
    {
        this->getInterval()->tinyDump();
    }

    if (this->treeNode)
    {
        printf("%s ", treeNode->OpName(treeNode->OperGet()));
    }
    printf("BB%02u ", this->bbNum);

    printf("regmask=");
    dumpRegMask(registerAssignment);

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
        printf(" (struct)");
    }
    if (isSpecialPutArg)
    {
        printf(" (specialPutArg)");
    }
    if (isConstant)
    {
        printf(" (constant)");
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
        printf("[%p]", dspPtr(relatedInterval));
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
    if (isInternal)
    {
        printf(" internal");
    }
    printf("> ");
}

// print out extremely concise representation
void Interval::microDump()
{
    char intervalTypeChar = 'I';
    if (isInternal)
    {
        intervalTypeChar = 'T';
    }
    else if (isLocalVar)
    {
        intervalTypeChar = 'L';
    }

    printf("<%c%u>", intervalTypeChar, intervalIndex);
}

void RegRecord::tinyDump()
{
    printf("<Reg:%-3s> ", getRegName(regNum));
}

void TreeNodeInfo::dump(LinearScan* lsra)
{
    printf("<TreeNodeInfo @ %2u %d=%d %di %df", loc, dstCount, srcCount, internalIntCount, internalFloatCount);
    printf(" src=");
    dumpRegMask(getSrcCandidates(lsra));
    printf(" int=");
    dumpRegMask(getInternalCandidates(lsra));
    printf(" dst=");
    dumpRegMask(getDstCandidates(lsra));
    if (isLocalDefUse)
    {
        printf(" L");
    }
    if (isInitialized)
    {
        printf(" I");
    }
    if (isHelperCallWithKills)
    {
        printf(" H");
    }
    if (isLsraAdded)
    {
        printf(" A");
    }
    if (isDelayFree)
    {
        printf(" D");
    }
    if (isTgtPref)
    {
        printf(" P");
    }
    printf(">\n");
}

void LinearScan::lsraDumpIntervals(const char* msg)
{
    Interval* interval;

    printf("\nLinear scan intervals %s:\n", msg);
    for (auto& interval : intervals)
    {
        // only dump something if it has references
        // if (interval->firstRefPosition)
        interval.dump();
    }

    printf("\n");
}

// Dumps a tree node as a destination or source operand, with the style
// of dump dependent on the mode
void LinearScan::lsraGetOperandString(GenTreePtr        tree,
                                      LsraTupleDumpMode mode,
                                      char*             operandString,
                                      unsigned          operandStringLength)
{
    const char* lastUseChar = "";
    if ((tree->gtFlags & GTF_VAR_DEATH) != 0)
    {
        lastUseChar = "*";
    }
    switch (mode)
    {
        case LinearScan::LSRA_DUMP_PRE:
            _snprintf_s(operandString, operandStringLength, operandStringLength, "t%d%s", tree->gtSeqNum, lastUseChar);
            break;
        case LinearScan::LSRA_DUMP_REFPOS:
            _snprintf_s(operandString, operandStringLength, operandStringLength, "t%d%s", tree->gtSeqNum, lastUseChar);
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
                _snprintf_s(operandString, operandStringLength, operandStringLength, "%s%s",
                            getRegName(tree->gtRegNum, useFloatReg(tree->TypeGet())), lastUseChar);
            }
        }
        break;
        default:
            printf("ERROR: INVALID TUPLE DUMP MODE\n");
            break;
    }
}
void LinearScan::lsraDispNode(GenTreePtr tree, LsraTupleDumpMode mode, bool hasDest)
{
    Compiler*      compiler            = JitTls::GetCompiler();
    const unsigned operandStringLength = 16;
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
            // This can be true for the "localDefUse" case - defining a reg, but
            // pushing it on the stack
            assert(spillChar == ' ');
            spillChar = '*';
            hasDest   = true;
        }
    }
    printf("%c N%03u. ", spillChar, tree->gtSeqNum);

    LclVarDsc* varDsc = nullptr;
    unsigned   varNum = UINT_MAX;
    if (tree->IsLocal())
    {
        varNum = tree->gtLclVarCommon.gtLclNum;
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
                printf("  V%02u(L%d)", varNum, getIntervalForLocalVar(varNum)->intervalIndex);
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
    else if (tree->OperIsAssignment())
    {
        assert(!tree->gtHasReg());
        const char* isRev = "";
        if ((tree->gtFlags & GTF_REVERSE_OPS) != 0)
        {
            isRev = "(Rev)";
        }
        printf("  asg%s%s  ", GenTree::NodeName(tree->OperGet()), isRev);
    }
    else
    {
        compiler->gtDispNodeName(tree);
        if ((tree->gtFlags & GTF_REVERSE_OPS) != 0)
        {
            printf("(Rev)");
        }
        if (tree->OperKind() & GTK_LEAF)
        {
            compiler->gtDispLeaf(tree, nullptr);
        }
    }
}

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
void LinearScan::DumpOperandDefs(GenTree* operand,
                                 bool& first,
                                 LsraTupleDumpMode mode,
                                 char* operandString,
                                 const unsigned operandStringLength)
{
    assert(operand != nullptr);
    assert(operandString != nullptr);

    if (ComputeOperandDstCount(operand) == 0)
    {
        return;
    }

    if (operand->gtLsraInfo.dstCount != 0)
    {
        // This operand directly produces registers; print it.
        for (int i = 0; i < operand->gtLsraInfo.dstCount; i++)
        {
            if (!first)
            {
                printf(",");
            }

            lsraGetOperandString(operand, mode, operandString, operandStringLength);
            printf("%s", operandString);

            first = false;
        }
    }
    else
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
    BasicBlock*            block;
    LsraLocation           currentLoc          = 1; // 0 is the entry
    const unsigned         operandStringLength = 16;
    char                   operandString[operandStringLength];

    // currentRefPosition is not used for LSRA_DUMP_PRE
    // We keep separate iterators for defs, so that we can print them
    // on the lhs of the dump
    auto currentRefPosition = refPositions.begin();

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
        for (; currentRefPosition != refPositions.end() && currentRefPosition->refType != RefTypeBB;
             ++currentRefPosition)
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
                regNumber assignedReg = varDsc->lvRegNum;
                regNumber argReg      = (varDsc->lvIsRegArg) ? varDsc->lvArgReg : REG_STK;

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
            for (; currentRefPosition != refPositions.end() &&
                   (currentRefPosition->refType == RefTypeExpUse || currentRefPosition->refType == RefTypeDummyDef ||
                    (currentRefPosition->refType == RefTypeBB && !printedBlockHeader));
                 ++currentRefPosition)
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
        if (mode == LSRA_DUMP_POST && block != compiler->fgFirstBB && block->bbNum <= bbNumMaxBeforeResolution)
        {
            printf("Predecessor for variable locations: BB%02u\n", blockInfo[block->bbNum].predBBNum);
            dumpInVarToRegMap(block);
        }
        if (block->bbNum > bbNumMaxBeforeResolution)
        {
            SplitEdgeInfo splitEdgeInfo;
            splitBBNumToTargetBBNumMap->Lookup(block->bbNum, &splitEdgeInfo);
            assert(splitEdgeInfo.toBBNum <= bbNumMaxBeforeResolution);
            assert(splitEdgeInfo.fromBBNum <= bbNumMaxBeforeResolution);
            printf("New block introduced for resolution from BB%02u to BB%02u\n", splitEdgeInfo.fromBBNum,
                   splitEdgeInfo.toBBNum);
        }

        for (GenTree* node : LIR::AsRange(block).NonPhiNodes())
        {
            GenTree* tree = node;

            genTreeOps oper = tree->OperGet();
            TreeNodeInfo& info = tree->gtLsraInfo;
            if (tree->gtLsraInfo.isLsraAdded)
            {
                // This must be one of the nodes that we add during LSRA

                if (oper == GT_LCL_VAR)
                {
                    info.srcCount = 0;
                    info.dstCount = 1;
                }
                else if (oper == GT_RELOAD || oper == GT_COPY)
                {
                    info.srcCount = 1;
                    info.dstCount = 1;
                }
#ifdef FEATURE_SIMD
                else if (oper == GT_SIMD)
                {
                    if (tree->gtSIMD.gtSIMDIntrinsicID == SIMDIntrinsicUpperSave)
                    {
                        info.srcCount = 1;
                        info.dstCount = 1;
                    }
                    else
                    {
                        assert(tree->gtSIMD.gtSIMDIntrinsicID == SIMDIntrinsicUpperRestore);
                        info.srcCount = 2;
                        info.dstCount = 0;
                    }
                }
#endif // FEATURE_SIMD
                else
                {
                    assert(oper == GT_SWAP);
                    info.srcCount = 2;
                    info.dstCount = 0;
                }
                info.internalIntCount   = 0;
                info.internalFloatCount = 0;
            }

            int       consume   = info.srcCount;
            int       produce   = info.dstCount;
            regMaskTP killMask  = RBM_NONE;
            regMaskTP fixedMask = RBM_NONE;

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
                for (; currentRefPosition != refPositions.end() &&
                       (currentRefPosition->refType == RefTypeUse || currentRefPosition->refType == RefTypeFixedReg ||
                        currentRefPosition->refType == RefTypeKill || currentRefPosition->refType == RefTypeDef) &&
                       (currentRefPosition->nodeLocation == tree->gtSeqNum ||
                        currentRefPosition->nodeLocation == tree->gtSeqNum + 1);
                     ++currentRefPosition)
                {
                    Interval* interval = nullptr;
                    if (currentRefPosition->isIntervalRef())
                    {
                        interval = currentRefPosition->getInterval();
                    }
                    switch (currentRefPosition->refType)
                    {
                        case RefTypeUse:
                            if (currentRefPosition->isPhysRegRef)
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
                                if (currentRefPosition->isFixedRegRef)
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
            if (info.internalIntCount != 0 && mode != LSRA_DUMP_REFPOS)
            {
                printf("\tinternal (%d):\t", info.internalIntCount);
                if (mode == LSRA_DUMP_POST)
                {
                    dumpRegMask(tree->gtRsvdRegs);
                }
                else if ((info.getInternalCandidates(this) & allRegs(TYP_INT)) != allRegs(TYP_INT))
                {
                    dumpRegMask(info.getInternalCandidates(this) & allRegs(TYP_INT));
                }
                printf("\n");
            }
            if (info.internalFloatCount != 0 && mode != LSRA_DUMP_REFPOS)
            {
                printf("\tinternal (%d):\t", info.internalFloatCount);
                if (mode == LSRA_DUMP_POST)
                {
                    dumpRegMask(tree->gtRsvdRegs);
                }
                else if ((info.getInternalCandidates(this) & allRegs(TYP_INT)) != allRegs(TYP_INT))
                {
                    dumpRegMask(info.getInternalCandidates(this) & allRegs(TYP_INT));
                }
                printf("\n");
            }
        }
        if (mode == LSRA_DUMP_POST)
        {
            dumpOutVarToRegMap(block);
        }
        printf("\n");
    }
    printf("\n\n");
}

void LinearScan::dumpLsraAllocationEvent(LsraDumpEvent event,
                                         Interval*     interval,
                                         regNumber     reg,
                                         BasicBlock*   currentBlock)
{
    if (!(VERBOSE))
    {
        return;
    }
    switch (event)
    {
        // Conflicting def/use
        case LSRA_EVENT_DEFUSE_CONFLICT:
            if (!dumpTerse)
            {
                printf("  Def and Use have conflicting register requirements:");
            }
            else
            {
                printf("DUconflict ");
                dumpRegRecords();
            }
            break;
        case LSRA_EVENT_DEFUSE_FIXED_DELAY_USE:
            if (!dumpTerse)
            {
                printf(" Can't change useAssignment ");
            }
            break;
        case LSRA_EVENT_DEFUSE_CASE1:
            if (!dumpTerse)
            {
                printf(" case #1, use the defRegAssignment\n");
            }
            else
            {
                printf(indentFormat, " case #1 use defRegAssignment");
                dumpRegRecords();
                dumpEmptyRefPosition();
            }
            break;
        case LSRA_EVENT_DEFUSE_CASE2:
            if (!dumpTerse)
            {
                printf(" case #2, use the useRegAssignment\n");
            }
            else
            {
                printf(indentFormat, " case #2 use useRegAssignment");
                dumpRegRecords();
                dumpEmptyRefPosition();
            }
            break;
        case LSRA_EVENT_DEFUSE_CASE3:
            if (!dumpTerse)
            {
                printf(" case #3, change the defRegAssignment to the use regs\n");
            }
            else
            {
                printf(indentFormat, " case #3 use useRegAssignment");
                dumpRegRecords();
                dumpEmptyRefPosition();
            }
            break;
        case LSRA_EVENT_DEFUSE_CASE4:
            if (!dumpTerse)
            {
                printf(" case #4, change the useRegAssignment to the def regs\n");
            }
            else
            {
                printf(indentFormat, " case #4 use defRegAssignment");
                dumpRegRecords();
                dumpEmptyRefPosition();
            }
            break;
        case LSRA_EVENT_DEFUSE_CASE5:
            if (!dumpTerse)
            {
                printf(" case #5, Conflicting Def and Use single-register requirements require copies - set def to all "
                       "regs of the appropriate type\n");
            }
            else
            {
                printf(indentFormat, " case #5 set def to all regs");
                dumpRegRecords();
                dumpEmptyRefPosition();
            }
            break;
        case LSRA_EVENT_DEFUSE_CASE6:
            if (!dumpTerse)
            {
                printf(" case #6, Conflicting Def and Use register requirements require a copy\n");
            }
            else
            {
                printf(indentFormat, " case #6 need a copy");
                dumpRegRecords();
                dumpEmptyRefPosition();
            }
            break;

        case LSRA_EVENT_SPILL:
            if (!dumpTerse)
            {
                printf("Spilled:\n");
                interval->dump();
            }
            else
            {
                assert(interval != nullptr && interval->assignedReg != nullptr);
                printf("Spill %-4s ", getRegName(interval->assignedReg->regNum));
                dumpRegRecords();
                dumpEmptyRefPosition();
            }
            break;
        case LSRA_EVENT_SPILL_EXTENDED_LIFETIME:
            if (!dumpTerse)
            {
                printf("  Spilled extended lifetime var V%02u at last use; not marked for actual spill.",
                       interval->intervalIndex);
            }
            break;

        // Restoring the previous register
        case LSRA_EVENT_RESTORE_PREVIOUS_INTERVAL_AFTER_SPILL:
            assert(interval != nullptr);
            if (!dumpTerse)
            {
                printf("  Assign register %s to previous interval Ivl:%d after spill\n", getRegName(reg),
                       interval->intervalIndex);
            }
            else
            {
                // If we spilled, then the dump is already pre-indented, but we need to pre-indent for the subsequent
                // allocation
                // with a dumpEmptyRefPosition().
                printf("SRstr %-4s ", getRegName(reg));
                dumpRegRecords();
                dumpEmptyRefPosition();
            }
            break;
        case LSRA_EVENT_RESTORE_PREVIOUS_INTERVAL:
            assert(interval != nullptr);
            if (!dumpTerse)
            {
                printf("  Assign register %s to previous interval Ivl:%d\n", getRegName(reg), interval->intervalIndex);
            }
            else
            {
                if (activeRefPosition == nullptr)
                {
                    printf(emptyRefPositionFormat, "");
                }
                printf("Restr %-4s ", getRegName(reg));
                dumpRegRecords();
                if (activeRefPosition != nullptr)
                {
                    printf(emptyRefPositionFormat, "");
                }
            }
            break;

        // Done with GC Kills
        case LSRA_EVENT_DONE_KILL_GC_REFS:
            printf("DoneKillGC ");
            break;

        // Block boundaries
        case LSRA_EVENT_START_BB:
            assert(currentBlock != nullptr);
            if (!dumpTerse)
            {
                printf("\n\n  Live Vars(Regs) at start of BB%02u (from pred BB%02u):", currentBlock->bbNum,
                       blockInfo[currentBlock->bbNum].predBBNum);
                dumpVarToRegMap(inVarToRegMaps[currentBlock->bbNum]);
            }
            break;
        case LSRA_EVENT_END_BB:
            if (!dumpTerse)
            {
                printf("\n\n  Live Vars(Regs) after BB%02u:", currentBlock->bbNum);
                dumpVarToRegMap(outVarToRegMaps[currentBlock->bbNum]);
            }
            break;

        case LSRA_EVENT_FREE_REGS:
            if (!dumpTerse)
            {
                printf("Freeing registers:\n");
            }
            break;

        // Characteristics of the current RefPosition
        case LSRA_EVENT_INCREMENT_RANGE_END:
            if (!dumpTerse)
            {
                printf("  Incrementing nextPhysRegLocation for %s\n", getRegName(reg));
            }
            // else ???
            break;
        case LSRA_EVENT_LAST_USE:
            if (!dumpTerse)
            {
                printf("    Last use, marked to be freed\n");
            }
            break;
        case LSRA_EVENT_LAST_USE_DELAYED:
            if (!dumpTerse)
            {
                printf("    Last use, marked to be freed (delayed)\n");
            }
            break;
        case LSRA_EVENT_NEEDS_NEW_REG:
            if (!dumpTerse)
            {
                printf("    Needs new register; mark %s to be freed\n", getRegName(reg));
            }
            else
            {
                printf("Free  %-4s ", getRegName(reg));
                dumpRegRecords();
                dumpEmptyRefPosition();
            }
            break;

        // Allocation decisions
        case LSRA_EVENT_FIXED_REG:
        case LSRA_EVENT_EXP_USE:
            if (!dumpTerse)
            {
                printf("No allocation\n");
            }
            else
            {
                printf("Keep  %-4s ", getRegName(reg));
            }
            break;
        case LSRA_EVENT_ZERO_REF:
            assert(interval != nullptr && interval->isLocalVar);
            if (!dumpTerse)
            {
                printf("Marking V%02u as last use there are no actual references\n", interval->varNum);
            }
            else
            {
                printf("NoRef      ");
                dumpRegRecords();
                dumpEmptyRefPosition();
            }
            break;
        case LSRA_EVENT_KEPT_ALLOCATION:
            if (!dumpTerse)
            {
                printf("already allocated %4s\n", getRegName(reg));
            }
            else
            {
                printf("Keep  %-4s ", getRegName(reg));
            }
            break;
        case LSRA_EVENT_COPY_REG:
            assert(interval != nullptr && interval->recentRefPosition != nullptr);
            if (!dumpTerse)
            {
                printf("allocated %s as copyReg\n\n", getRegName(reg));
            }
            else
            {
                printf("Copy  %-4s ", getRegName(reg));
            }
            break;
        case LSRA_EVENT_MOVE_REG:
            assert(interval != nullptr && interval->recentRefPosition != nullptr);
            if (!dumpTerse)
            {
                printf("  needs a new register; marked as moveReg\n");
            }
            else
            {
                printf("Move  %-4s ", getRegName(reg));
                dumpRegRecords();
                dumpEmptyRefPosition();
            }
            break;
        case LSRA_EVENT_ALLOC_REG:
            if (!dumpTerse)
            {
                printf("allocated %s\n", getRegName(reg));
            }
            else
            {
                printf("Alloc %-4s ", getRegName(reg));
            }
            break;
        case LSRA_EVENT_REUSE_REG:
            if (!dumpTerse)
            {
                printf("reused constant in %s\n", getRegName(reg));
            }
            else
            {
                printf("Reuse %-4s ", getRegName(reg));
            }
            break;
        case LSRA_EVENT_ALLOC_SPILLED_REG:
            if (!dumpTerse)
            {
                printf("allocated spilled register %s\n", getRegName(reg));
            }
            else
            {
                printf("Steal %-4s ", getRegName(reg));
            }
            break;
        case LSRA_EVENT_NO_ENTRY_REG_ALLOCATED:
            assert(interval != nullptr && interval->isLocalVar);
            if (!dumpTerse)
            {
                printf("Not allocating an entry register for V%02u due to low ref count\n", interval->varNum);
            }
            else
            {
                printf("LoRef      ");
            }
            break;
        case LSRA_EVENT_NO_REG_ALLOCATED:
            if (!dumpTerse)
            {
                printf("no register allocated\n");
            }
            else
            {
                printf("NoReg      ");
            }
            break;
        case LSRA_EVENT_RELOAD:
            if (!dumpTerse)
            {
                printf("    Marked for reload\n");
            }
            else
            {
                printf("ReLod %-4s ", getRegName(reg));
                dumpRegRecords();
                dumpEmptyRefPosition();
            }
            break;
        case LSRA_EVENT_SPECIAL_PUTARG:
            if (!dumpTerse)
            {
                printf("    Special case of putArg - using lclVar that's in the expected reg\n");
            }
            else
            {
                printf("PtArg %-4s ", getRegName(reg));
            }
            break;
        default:
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
           "active, and 'i'if it is inactive.  Columns are only printed up to the last modifed register, which\n"
           "may increase during allocation, in which case additional columns will appear.  Registers which are\n"
           "not marked modified have ---- in their column.\n\n");

    // First, determine the width of each register column (which holds a reg name in the
    // header, and an interval name in each subsequent row).
    int intervalNumberWidth = (int)log10((double)intervals.size()) + 1;
    // The regColumnWidth includes the identifying character (I or V) and an 'i' or 'a' (inactive or active)
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
    //  - a 5-character allocation decision
    //  - a space
    //  - a 4-character register
    //  - a space
    int allocationInfoWidth = 5 + 1 + 4 + 1;

    // Next, determine the width of the legend for each row.  This includes:
    //  - a short RefPosition dump (shortRefPositionDumpWidth), which includes a space
    //  - the allocation info (allocationInfoWidth), which also includes a space

    regTableIndent = shortRefPositionDumpWidth + allocationInfoWidth;

    // BBnn printed left-justified in the NAME Typeld and allocationInfo space.
    int bbDumpWidth = regColumnWidth + 1 + refTypeInfoWidth + allocationInfoWidth;
    int bbNumWidth  = (int)log10((double)compiler->fgBBNumMax) + 1;
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

    // Finally, print a "title row" including the legend and the reg names
    dumpRegRecordTitle();
}

int LinearScan::getLastUsedRegNumIndex()
{
    int       lastUsedRegNumIndex = 0;
    regMaskTP usedRegsMask        = compiler->codeGen->regSet.rsGetModifiedRegsMask();
    int       lastRegNumIndex     = compiler->compFloatingPointUsed ? REG_FP_LAST : REG_INT_LAST;
    for (int regNumIndex = 0; regNumIndex <= lastRegNumIndex; regNumIndex++)
    {
        if ((usedRegsMask & genRegMask((regNumber)regNumIndex)) != 0)
        {
            lastUsedRegNumIndex = regNumIndex;
        }
    }
    return lastUsedRegNumIndex;
}

void LinearScan::dumpRegRecordTitleLines()
{
    for (int i = 0; i < regTableIndent; i++)
    {
        printf("%s", line);
    }
    int lastUsedRegNumIndex = getLastUsedRegNumIndex();
    for (int regNumIndex = 0; regNumIndex <= lastUsedRegNumIndex; regNumIndex++)
    {
        printf("%s", middleBox);
        for (int i = 0; i < regColumnWidth; i++)
        {
            printf("%s", line);
        }
    }
    printf("%s\n", rightBox);
}
void LinearScan::dumpRegRecordTitle()
{
    dumpRegRecordTitleLines();

    // Print out the legend for the RefPosition info
    printf(legendFormat, "Loc ", "RP# ", "Name ", "Type  Action Reg  ");

    // Print out the register name column headers
    char columnFormatArray[MAX_FORMAT_CHARS];
    sprintf_s(columnFormatArray, MAX_FORMAT_CHARS, "%s%%-%d.%ds", columnSeparator, regColumnWidth, regColumnWidth);
    int lastUsedRegNumIndex = getLastUsedRegNumIndex();
    for (int regNumIndex = 0; regNumIndex <= lastUsedRegNumIndex; regNumIndex++)
    {
        regNumber   regNum  = (regNumber)regNumIndex;
        const char* regName = getRegName(regNum);
        printf(columnFormatArray, regName);
    }
    printf("%s\n", columnSeparator);

    rowCountSinceLastTitle = 0;

    dumpRegRecordTitleLines();
}

void LinearScan::dumpRegRecords()
{
    static char columnFormatArray[18];
    int         lastUsedRegNumIndex = getLastUsedRegNumIndex();
    regMaskTP   usedRegsMask        = compiler->codeGen->regSet.rsGetModifiedRegsMask();

    for (int regNumIndex = 0; regNumIndex <= lastUsedRegNumIndex; regNumIndex++)
    {
        printf("%s", columnSeparator);
        RegRecord& regRecord = physRegs[regNumIndex];
        Interval*  interval  = regRecord.assignedInterval;
        if (interval != nullptr)
        {
            dumpIntervalName(interval);
            char activeChar = interval->isActive ? 'a' : 'i';
            printf("%c", activeChar);
        }
        else if (regRecord.isBusyUntilNextKill)
        {
            printf(columnFormatArray, "Busy");
        }
        else if ((usedRegsMask & genRegMask((regNumber)regNumIndex)) == 0)
        {
            sprintf_s(columnFormatArray, MAX_FORMAT_CHARS, "%%-%ds", regColumnWidth);
            printf(columnFormatArray, "----");
        }
        else
        {
            sprintf_s(columnFormatArray, MAX_FORMAT_CHARS, "%%-%ds", regColumnWidth);
            printf(columnFormatArray, "");
        }
    }
    printf("%s\n", columnSeparator);

    if (rowCountSinceLastTitle > MAX_ROWS_BETWEEN_TITLES)
    {
        dumpRegRecordTitle();
    }
    rowCountSinceLastTitle++;
}

void LinearScan::dumpIntervalName(Interval* interval)
{
    char intervalChar;
    if (interval->isLocalVar)
    {
        intervalChar = 'V';
    }
    else if (interval->isConstant)
    {
        intervalChar = 'C';
    }
    else
    {
        intervalChar = 'I';
    }
    printf(intervalNameFormat, intervalChar, interval->intervalIndex);
}

void LinearScan::dumpEmptyRefPosition()
{
    printf(emptyRefPositionFormat, "");
}

// Note that the size of this dump is computed in dumpRegRecordHeader().
//
void LinearScan::dumpRefPositionShort(RefPosition* refPosition, BasicBlock* currentBlock)
{
    BasicBlock* block = currentBlock;
    if (refPosition->refType == RefTypeBB)
    {
        // Always print a title row before a RefTypeBB (except for the first, because we
        // will already have printed it before the parameters)
        if (refPosition->refType == RefTypeBB && block != compiler->fgFirstBB && block != nullptr)
        {
            dumpRegRecordTitle();
        }
    }
    printf(shortRefPositionFormat, refPosition->nodeLocation, refPosition->rpNum);
    if (refPosition->refType == RefTypeBB)
    {
        if (block == nullptr)
        {
            printf(regNameFormat, "END");
            printf("               ");
            printf(regNameFormat, "");
        }
        else
        {
            printf(bbRefPosFormat, block->bbNum, block == compiler->fgFirstBB ? 0 : blockInfo[block->bbNum].predBBNum);
        }
    }
    else if (refPosition->isIntervalRef())
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
    else if (refPosition->isPhysRegRef)
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
    if (!node->gtLsraInfo.isLsraAdded)
    {
        return false;
    }

    switch (node->OperGet())
    {
    case GT_LCL_VAR:
    case GT_COPY:
        return node->gtLsraInfo.isLocalDefUse;

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

        if (!node->gtLsraInfo.isLsraAdded || (node->OperGet() != GT_LCL_VAR))
        {
            return false;
        }

        LIR::Use use;
        bool foundUse = containingRange.TryGetUse(node, &use);
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

    for (auto& interval : intervals)
    {
        interval.assignedReg = nullptr;
        interval.physReg     = REG_NA;
    }

    DBEXEC(VERBOSE, dumpRegRecordTitle());

    BasicBlock*  currentBlock                = nullptr;
    GenTree*     firstBlockEndResolutionNode = nullptr;
    regMaskTP    regsToFree                  = RBM_NONE;
    regMaskTP    delayRegsToFree             = RBM_NONE;
    LsraLocation currentLocation             = MinLocation;
    for (auto& refPosition : refPositions)
    {
        RefPosition* currentRefPosition = &refPosition;
        Interval*    interval           = nullptr;
        RegRecord*   regRecord          = nullptr;
        regNumber    regNum             = REG_NA;
        if (currentRefPosition->refType == RefTypeBB)
        {
            regsToFree |= delayRegsToFree;
            delayRegsToFree = RBM_NONE;
            // For BB RefPositions, wait until we dump the "end of block" info before dumping the basic RefPosition
            // info.
        }
        else
        {
            // For other RefPosition types, we can dump the basic RefPosition info now.
            DBEXEC(VERBOSE, dumpRefPositionShort(currentRefPosition, currentBlock));

            if (currentRefPosition->isPhysRegRef)
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

        if (newLocation > currentLocation)
        {
            // Free Registers.
            // We could use the freeRegisters() method, but we'd have to carefully manage the active intervals.
            for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
            {
                regMaskTP regMask = genRegMask(reg);
                if ((regsToFree & regMask) != RBM_NONE)
                {
                    RegRecord* physRegRecord        = getRegisterRecord(reg);
                    physRegRecord->assignedInterval = nullptr;
                }
            }
            regsToFree = delayRegsToFree;
            regsToFree = RBM_NONE;
        }
        currentLocation = newLocation;

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
                        // Only verify nodes that are actually moves; don't bother with the nodes that are
                        // operands to moves.
                        if (IsResolutionMove(node))
                        {
                            verifyResolutionMove(node, currentLocation);
                        }
                    }

                    // Validate the locations at the end of the previous block.
                    VarToRegMap outVarToRegMap = outVarToRegMaps[currentBlock->bbNum];
                    VARSET_ITER_INIT(compiler, iter, currentBlock->bbLiveOut, varIndex);
                    while (iter.NextElem(compiler, &varIndex))
                    {
                        unsigned  varNum = compiler->lvaTrackedToVarNum[varIndex];
                        regNumber regNum = getVarReg(outVarToRegMap, varNum);
                        interval         = getIntervalForLocalVar(varNum);
                        assert(interval->physReg == regNum || (interval->physReg == REG_NA && regNum == REG_STK));
                        interval->physReg     = REG_NA;
                        interval->assignedReg = nullptr;
                        interval->isActive    = false;
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
                    VarToRegMap inVarToRegMap = inVarToRegMaps[currentBlock->bbNum];
                    VARSET_ITER_INIT(compiler, iter, currentBlock->bbLiveIn, varIndex);
                    while (iter.NextElem(compiler, &varIndex))
                    {
                        unsigned  varNum                  = compiler->lvaTrackedToVarNum[varIndex];
                        regNumber regNum                  = getVarReg(inVarToRegMap, varNum);
                        interval                          = getIntervalForLocalVar(varNum);
                        interval->physReg                 = regNum;
                        interval->assignedReg             = &(physRegs[regNum]);
                        interval->isActive                = true;
                        physRegs[regNum].assignedInterval = interval;
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

            case RefTypeUpperVectorSaveDef:
            case RefTypeUpperVectorSaveUse:
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
                        printf("Move  %-4s ", getRegName(regRecord->regNum));
                    }
                }
                else
                {
                    dumpLsraAllocationEvent(LSRA_EVENT_KEPT_ALLOCATION, nullptr, regRecord->regNum, currentBlock);
                }
                if (currentRefPosition->lastUse || currentRefPosition->spillAfter)
                {
                    interval->isActive = false;
                }
                if (regNum != REG_NA)
                {
                    if (currentRefPosition->spillAfter)
                    {
                        if (VERBOSE)
                        {
                            dumpRegRecords();
                            dumpEmptyRefPosition();
                            printf("Spill %-4s ", getRegName(regNum));
                        }
                    }
                    else if (currentRefPosition->copyReg)
                    {
                        regRecord->assignedInterval = interval;
                    }
                    else
                    {
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
                DBEXEC(VERBOSE, printf("           "));
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
                    interval->physReg     = REG_NA;
                    interval->assignedReg = nullptr;

                    // regRegcord could be null if RefPosition is to be allocated a
                    // reg only if profitable.
                    if (regRecord != nullptr)
                    {
                        regRecord->assignedInterval = nullptr;
                    }
                    else
                    {
                        assert(currentRefPosition->AllocateIfProfitable());
                    }
                }
            }
        }
    }

    // Now, verify the resolution blocks.
    // Currently these are nearly always at the end of the method, but that may not alwyas be the case.
    // So, we'll go through all the BBs looking for blocks whose bbNum is greater than bbNumMaxBeforeResolution.
    for (BasicBlock* currentBlock = compiler->fgFirstBB; currentBlock != nullptr; currentBlock = currentBlock->bbNext)
    {
        if (currentBlock->bbNum > bbNumMaxBeforeResolution)
        {
            if (VERBOSE)
            {
                dumpRegRecordTitle();
                printf(shortRefPositionFormat, 0, 0);
                assert(currentBlock->bbPreds != nullptr && currentBlock->bbPreds->flBlock != nullptr);
                printf(bbRefPosFormat, currentBlock->bbNum, currentBlock->bbPreds->flBlock->bbNum);
                dumpRegRecords();
            }

            // Clear register assignments.
            for (regNumber reg = REG_FIRST; reg < ACTUAL_REG_COUNT; reg = REG_NEXT(reg))
            {
                RegRecord* physRegRecord        = getRegisterRecord(reg);
                physRegRecord->assignedInterval = nullptr;
            }

            // Set the incoming register assignments
            VarToRegMap inVarToRegMap = getInVarToRegMap(currentBlock->bbNum);
            VARSET_ITER_INIT(compiler, iter, currentBlock->bbLiveIn, varIndex);
            while (iter.NextElem(compiler, &varIndex))
            {
                unsigned  varNum                  = compiler->lvaTrackedToVarNum[varIndex];
                regNumber regNum                  = getVarReg(inVarToRegMap, varNum);
                Interval* interval                = getIntervalForLocalVar(varNum);
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
                VarToRegMap outVarToRegMap = getOutVarToRegMap(currentBlock->bbNum);
                VARSET_ITER_INIT(compiler, iter, currentBlock->bbLiveOut, varIndex);
                while (iter.NextElem(compiler, &varIndex))
                {
                    unsigned  varNum   = compiler->lvaTrackedToVarNum[varIndex];
                    regNumber regNum   = getVarReg(outVarToRegMap, varNum);
                    Interval* interval = getIntervalForLocalVar(varNum);
                    assert(interval->physReg == regNum || (interval->physReg == REG_NA && regNum == REG_STK));
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
        regNumber            leftRegNum    = left->gtRegNum;
        regNumber            rightRegNum   = right->gtRegNum;
        Interval*            leftInterval  = getIntervalForLocalVar(left->gtLclNum);
        Interval*            rightInterval = getIntervalForLocalVar(right->gtLclNum);
        assert(leftInterval->physReg == leftRegNum && rightInterval->physReg == rightRegNum);
        leftInterval->physReg                  = rightRegNum;
        rightInterval->physReg                 = leftRegNum;
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
    regNumber            dstRegNum = dst->gtRegNum;
    regNumber            srcRegNum;
    GenTreeLclVarCommon* lcl;
    if (dst->OperGet() == GT_COPY)
    {
        lcl       = dst->gtGetOp1()->AsLclVarCommon();
        srcRegNum = lcl->gtRegNum;
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
    Interval* interval = getIntervalForLocalVar(lcl->gtLclNum);
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
        printf("  Move   ");
        printf("      %-4s ", getRegName(dstRegNum));
        dumpRegRecords();
    }
}
#endif // DEBUG

#endif // !LEGACY_BACKEND
