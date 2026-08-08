// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                    Fast register allocator for MinOpts                    XX
XX                                                                           XX
XX  When optimizations are disabled no local variable is enregistered, so     XX
XX  every value that needs a register is a "tree temp": defined by a LIR      XX
XX  node and consumed by exactly one later node in the same block. Nothing    XX
XX  is live across a block boundary, so no dataflow, block sequencing         XX
XX  heuristics, interval splitting or edge resolution is required. That lets  XX
XX  the three LSRA passes (build, allocate, resolve) fuse into a single walk  XX
XX  over the LIR: for each node we build its RefPositions (reusing the        XX
XX  target-specific BuildNode logic), immediately assign registers and        XX
XX  immediately write the results back into the IR.                          XX
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
// canUseMinOptsRegAlloc: Determine whether the fast MinOpts allocator can be used.
//
bool LinearScan::canUseMinOptsRegAlloc()
{
#ifdef TARGET_ARM
    // On arm32 a TYP_DOUBLE occupies a register pair, which this allocator does not model.
    return false;
#else
    // The fast allocator relies on no value being live across a block boundary,
    // which is only guaranteed when no local variable is enregistered.
    if (enregisterLocalVars || m_compiler->opts.OptimizationEnabled())
    {
        return false;
    }

#ifdef TARGET_ARM64
    // Consecutive register requirements need the general allocator.
    if (m_compiler->info.compNeedsConsecutiveRegisters)
    {
        return false;
    }
#endif

#ifdef DEBUG
    // The register allocation stress modes are implemented in terms of the data structures
    // that the general allocator builds, so let it handle those.
    if ((getStressLimitRegs() != LSRA_LIMIT_NONE) || (getSelectionHeuristics() != LSRA_SELECT_DEFAULT) ||
        getLsraExtendLifeTimes() || spillAlways() || alwaysInsertReload())
    {
        return false;
    }
#endif // DEBUG

    return true;
#endif // !TARGET_ARM
}

//------------------------------------------------------------------------
// minOptsRecordNodeRefPosition: Remember a RefPosition that belongs to the node
//    currently being processed.
//
void LinearScan::minOptsRecordNodeRefPosition(RefPosition* refPosition)
{
    if (minOptsNodeRefPositionCount == minOptsNodeRefPositionCapacity)
    {
        unsigned      newCapacity = (minOptsNodeRefPositionCapacity == 0) ? 64 : minOptsNodeRefPositionCapacity * 2;
        RefPosition** newArray    = getAllocator(m_compiler).allocate<RefPosition*>(newCapacity);
        if (minOptsNodeRefPositionCount != 0)
        {
            memcpy(newArray, minOptsNodeRefPositions, minOptsNodeRefPositionCount * sizeof(RefPosition*));
        }
        minOptsNodeRefPositions        = newArray;
        minOptsNodeRefPositionCapacity = newCapacity;
    }

    minOptsNodeRefPositions[minOptsNodeRefPositionCount++] = refPosition;
}

//------------------------------------------------------------------------
// minOptsBuildRegOrder: Set up the per-register-file allocation order.
//
void LinearScan::minOptsBuildRegOrder()
{
    static const regNumber intOrder[] = {REG_VAR_ORDER};
    static const regNumber fltOrder[] = {REG_VAR_ORDER_FLT};

    minOptsRegOrder[0]     = intOrder;
    minOptsRegOrderSize[0] = ArrLen(intOrder);
    minOptsRegOrder[1]     = fltOrder;
    minOptsRegOrderSize[1] = ArrLen(fltOrder);
    minOptsRegOrder[2]     = nullptr;
    minOptsRegOrderSize[2] = 0;

#if defined(TARGET_AMD64)
    // x64 has additional float registers available when EVEX is supported, which changes
    // the preference order.
    static const regNumber fltOrderEvex[] = {REG_VAR_ORDER_FLT_EVEX};
    if (getEvexIsSupported())
    {
        minOptsRegOrder[1]     = fltOrderEvex;
        minOptsRegOrderSize[1] = ArrLen(fltOrderEvex);
    }
#endif

#if defined(TARGET_XARCH)
    static const regNumber mskOrder[] = {REG_VAR_ORDER_MSK};
    minOptsRegOrder[2]                = mskOrder;
    minOptsRegOrderSize[2]            = ArrLen(mskOrder);
#endif
}

//------------------------------------------------------------------------
// minOptsAssignReg: Record that 'interval' now occupies 'reg'.
//
void LinearScan::minOptsAssignReg(Interval* interval, regNumber reg)
{
    RegisterType     regType = interval->registerType;
    SingleTypeRegSet regMask = getSingleTypeRegMask(reg, regType);

    interval->physReg = reg;
    minOptsLiveRegs.AddRegsetForType(regMask, regType);
    minOptsRegsInUse.AddRegsetForType(regMask, regType);
    minOptsRegToInterval[reg] = interval;
}

//------------------------------------------------------------------------
// minOptsMarkModified: Record that codegen will write to 'reg'.
//
// Notes:
//    This is deliberately only done once a register assignment is final. A definition
//    may still be retargeted to a different register while it is pending its use, and
//    in that case the register it was tentatively placed in is never written to.
//
void LinearScan::minOptsMarkModified(regNumber reg)
{
    m_compiler->codeGen->regSet.rsSetRegsModified(genRegMask(reg) DEBUGARG(true));
}

//------------------------------------------------------------------------
// minOptsMoveInterval: Retarget a definition that has not been spilled to a different
//    register, rewriting the register recorded on the defining node.
//
// Notes:
//    This rewrites history: the value is treated as if it had been defined into 'newReg'
//    in the first place, so no move instruction is required. The caller must have verified
//    that 'newReg' was free for the whole lifetime of the interval.
//
void LinearScan::minOptsMoveInterval(Interval* interval, regNumber newReg)
{
    RefPosition* defRefPosition = interval->firstRefPosition;
    regNumber    oldReg         = interval->physReg;
    RegisterType regType        = interval->registerType;

    assert(oldReg != REG_NA);
    assert(!defRefPosition->spillAfter);

    // Note that we deliberately do not update minOptsRegFreeSince[oldReg]: as far as the
    // rest of the allocation is concerned, the value was never in 'oldReg'.
    minOptsLiveRegs.RemoveRegsetForType(getSingleTypeRegMask(oldReg, regType), regType);
    minOptsFreeReg(interval);

    minOptsAssignReg(interval, newReg);
    defRefPosition->registerAssignment = genSingleTypeRegMask(newReg);
    lsraAssignRegToTree(defRefPosition->treeNode, newReg, defRefPosition->getMultiRegIdx());

    JITDUMP("      Retargeted def [%06u] from %s to %s\n", Compiler::dspTreeID(defRefPosition->treeNode),
            getRegName(oldReg), getRegName(newReg));
}

//------------------------------------------------------------------------
// minOptsCanRetarget: Can the definition of 'interval' be moved to another register?
//
static bool minOptsCanRetarget(Interval* interval)
{
    if (interval->isInternal || (interval->physReg == REG_NA))
    {
        return false;
    }

    RefPosition* defRefPosition = interval->firstRefPosition;
    if ((defRefPosition == nullptr) || !RefTypeIsDef(defRefPosition->refType) || defRefPosition->spillAfter)
    {
        return false;
    }

    // A fixed definition has to stay where it is, and the registers of a multi-reg node
    // can't be changed independently (that could require a cyclic parallel assignment).
    if (defRefPosition->isFixedRegRef || (defRefPosition->treeNode == nullptr) ||
        defRefPosition->treeNode->IsMultiRegNode())
    {
        return false;
    }

    return true;
}

//------------------------------------------------------------------------
// minOptsTryRetarget: Try to make the value of 'interval' available in one of 'candidates'
//    without emitting a move, by retargeting its definition.
//
// Notes:
//    A definition can be retargeted to a register that has been free for the whole
//    lifetime of the value. If the wanted register is occupied by another pending
//    definition, we also try to relocate that one out of the way; this is what makes
//    patterns like a GC write barrier (which needs two specific registers that were
//    filled in the "wrong" order) come out without any moves.
//
bool LinearScan::minOptsTryRetarget(Interval* interval, SingleTypeRegSet candidates, regMaskTP conflicting)
{
    if (!minOptsCanRetarget(interval))
    {
        return false;
    }

    const LsraLocation defLoc  = interval->firstRefPosition->nodeLocation;
    const RegisterType regType = interval->registerType;
    const regNumber    oldReg  = interval->physReg;

    // The definition can only be moved to a register it could have been produced into.
    candidates &= interval->registerPreferences;
    if (candidates == RBM_NONE)
    {
        return false;
    }

    regMaskTP        blockedMask = minOptsRegsInUse | conflicting;
    SingleTypeRegSet blocked     = blockedMask.GetRegSetForType(regType);
    SingleTypeRegSet occupied    = minOptsLiveRegs.GetRegSetForType(regType);
    SingleTypeRegSet ownMask     = getSingleTypeRegMask(oldReg, regType);

    const int        typeIndex = minOptsRegOrderIndex(regType);
    const regNumber* order     = minOptsRegOrder[typeIndex];
    const unsigned   orderSize = minOptsRegOrderSize[typeIndex];

    SingleTypeRegSet freeCandidates = candidates & ~(blocked | occupied);

    for (unsigned i = 0; (i < orderSize) && (freeCandidates != RBM_NONE); i++)
    {
        regNumber candidateReg = order[i];
        if ((freeCandidates & genSingleTypeRegMask(candidateReg)) == RBM_NONE)
        {
            continue;
        }

        if (minOptsRegFreeSince[candidateReg] <= defLoc)
        {
            minOptsMoveInterval(interval, candidateReg);
            return true;
        }
    }

    // Nothing was free for long enough. See whether we can relocate the value that is
    // sitting in one of the candidate registers, which would then free it up all the way
    // back to our definition.
    SingleTypeRegSet takenCandidates = candidates & occupied & ~(blocked | ownMask);
    while (takenCandidates != RBM_NONE)
    {
        regNumber        candidateReg = genFirstRegNumFromMask(takenCandidates, regType);
        SingleTypeRegSet candidateBit = genSingleTypeRegMask(candidateReg);
        takenCandidates ^= candidateBit;

        if (minOptsRegFreeSince[candidateReg] > defLoc)
        {
            continue;
        }

        Interval* occupant = minOptsRegToInterval[candidateReg];
        if ((occupant == nullptr) || !minOptsCanRetarget(occupant))
        {
            continue;
        }

        // The occupant has to be relocatable within the same register file.
        const RegisterType occRegType = occupant->registerType;
        if (minOptsRegOrderIndex(occRegType) != minOptsRegOrderIndex(regType))
        {
            continue;
        }

        const LsraLocation occupantDefLoc = occupant->firstRefPosition->nodeLocation;
        SingleTypeRegSet   occBlocked     = blockedMask.GetRegSetForType(occRegType);
        SingleTypeRegSet   occOccupied    = minOptsLiveRegs.GetRegSetForType(occRegType);
        SingleTypeRegSet   relocTargets   = occupant->registerPreferences & ~(occBlocked | occOccupied);

        for (unsigned i = 0; (i < orderSize) && (relocTargets != RBM_NONE); i++)
        {
            regNumber relocReg = order[i];
            if ((relocTargets & genSingleTypeRegMask(relocReg)) == RBM_NONE)
            {
                continue;
            }

            if (minOptsRegFreeSince[relocReg] > occupantDefLoc)
            {
                continue;
            }

            minOptsMoveInterval(occupant, relocReg);
            minOptsMoveInterval(interval, candidateReg);
            return true;
        }
    }

    return false;
}

//------------------------------------------------------------------------
// minOptsFreeReg: Release the register held by 'interval'. The interval must be dead.
//
void LinearScan::minOptsFreeReg(Interval* interval)
{
    regNumber reg = interval->physReg;
    assert(reg != REG_NA);
    assert(minOptsRegToInterval[reg] == interval);

    minOptsRegToInterval[reg] = nullptr;
    interval->physReg         = REG_NA;
}

//------------------------------------------------------------------------
// minOptsSpillInterval: Spill the value held by 'interval' to its home stack location.
//
// Notes:
//    This marks the defining node with GTF_SPILL. The consuming use will either
//    reload it into a register or (if it is RegOptional) consume it from memory.
//
void LinearScan::minOptsSpillInterval(Interval* interval)
{
    assert(interval->physReg != REG_NA);
    assert(!interval->isInternal);

    RefPosition* defRefPosition = interval->firstRefPosition;
    assert((defRefPosition != nullptr) && RefTypeIsDef(defRefPosition->refType));

    GenTree* defNode = defRefPosition->treeNode;
    assert(defNode != nullptr);

    if (!defRefPosition->spillAfter)
    {
        defRefPosition->spillAfter = true;
        interval->isSpilled        = true;
        minOptsAnySpill            = true;

        defNode->gtFlags |= GTF_SPILL;
        if (defNode->IsMultiRegNode())
        {
            defNode->SetRegSpillFlagByIdx(GTF_SPILL, defRefPosition->getMultiRegIdx());
        }

        JITDUMP("      Spilling [%06u] from %s\n", Compiler::dspTreeID(defNode), getRegName(interval->physReg));
    }

    RegisterType     regType = interval->registerType;
    SingleTypeRegSet regMask = getSingleTypeRegMask(interval->physReg, regType);
    minOptsLiveRegs.RemoveRegsetForType(regMask, regType);
    minOptsRegsToFree.RemoveRegsetForType(regMask, regType);
    minOptsDelayRegsToFree.RemoveRegsetForType(regMask, regType);
    minOptsRegFreeSince[interval->physReg] = minOptsCurLoc;

    minOptsFreeReg(interval);
}

//------------------------------------------------------------------------
// minOptsProcessKill: Handle the registers killed by the current node.
//
void LinearScan::minOptsProcessKill(regMaskTP killedRegs)
{
    regMaskTP liveKilled = killedRegs & minOptsLiveRegs;

    for (regMaskTP remaining = liveKilled; remaining.IsNonEmpty();)
    {
        regNumber reg      = genFirstRegNumFromMaskAndToggle(remaining);
        Interval* interval = minOptsRegToInterval[reg];
        if (interval != nullptr)
        {
            minOptsSpillInterval(interval);
        }
    }

    // The killed registers are available again after the kill.
    for (regMaskTP remaining = killedRegs; remaining.IsNonEmpty();)
    {
        regNumber reg            = genFirstRegNumFromMaskAndToggle(remaining);
        minOptsRegFreeSince[reg] = minOptsCurLoc;
    }

    // Definitions of this node are produced after the kill, so they are free to use
    // these registers.
    minOptsKilledRegs &= ~killedRegs;
}

//------------------------------------------------------------------------
// minOptsSpillGCRefs: Spill any GC values that are live in the given register set.
//
void LinearScan::minOptsSpillGCRefs(RefPosition* refPosition)
{
    regMaskTP candidates = regMaskTP(refPosition->registerAssignment) & minOptsLiveRegs;

    for (regMaskTP remaining = candidates; remaining.IsNonEmpty();)
    {
        regNumber reg      = genFirstRegNumFromMaskAndToggle(remaining);
        Interval* interval = minOptsRegToInterval[reg];
        if (interval == nullptr)
        {
            continue;
        }

        bool needsKill = varTypeIsGC(interval->registerType);
        if (!needsKill)
        {
            // We can have a node with a GC type whose interval type is an integer type;
            // the emitter will report the register as holding a GC value, so it must be spilled.
            GenTree* defNode = interval->firstRefPosition->treeNode;
            needsKill        = (defNode != nullptr) && varTypeIsGC(defNode);
        }

        if (needsKill)
        {
            minOptsSpillInterval(interval);
        }
    }
}

//------------------------------------------------------------------------
// minOptsVacateReg: Make 'reg' available, preferring to relocate the value that is in it
//    over spilling it.
//
void LinearScan::minOptsVacateReg(regNumber reg)
{
    Interval* occupant = minOptsRegToInterval[reg];
    if (occupant == nullptr)
    {
        return;
    }

    // Anything this node needs at a specific register (or clobbers) is not a valid new
    // home for the value, and neither is where it is now.
    const RegisterType occRegType  = occupant->registerType;
    regMaskTP          conflicting = minOptsFixedRegsThisLoc | minOptsFixedRegsNextLoc | minOptsKilledRegs;
    conflicting.AddRegsetForType(getSingleTypeRegMask(occupant->physReg, occRegType), occRegType);

    if (!minOptsTryRetarget(occupant, occupant->registerPreferences, conflicting))
    {
        minOptsSpillInterval(occupant);
    }
}

//------------------------------------------------------------------------
// minOptsSelectReg: Choose a register for the given RefPosition.
//
// Notes:
//    A free register is always preferred. Otherwise a currently live value is
//    spilled to make room.
//
regNumber LinearScan::minOptsSelectReg(RefPosition* refPosition, SingleTypeRegSet candidates, RegisterType regType)
{
    assert(candidates != RBM_NONE);

    if (refPosition->isFixedRegRef && genMaxOneBit(candidates))
    {
        // This RefPosition requires one specific register; take it, moving or spilling
        // whatever is in it.
        regNumber fixedReg = genRegNumFromMask(candidates, regType);
        minOptsVacateReg(fixedReg);
        return fixedReg;
    }

    // Registers that must not be handed out to this RefPosition:
    //  - anything referenced at this location
    //  - anything that a later RefPosition of this node requires at a fixed register
    regMaskTP avoidMask = minOptsRegsInUse;
    if (refPosition->nodeLocation == currentLoc)
    {
        avoidMask |= minOptsFixedRegsThisLoc;
        if (refPosition->delayRegFree)
        {
            // The register has to stay alive through the def location.
            avoidMask |= minOptsFixedRegsNextLoc | minOptsKilledRegs;
        }
    }
    else
    {
        // A definition must not land in a register that the node itself clobbers, nor in one
        // that another definition of this node requires.
        avoidMask |= minOptsFixedRegsNextLoc | minOptsKilledRegs;
    }

    SingleTypeRegSet avoid = avoidMask.GetRegSetForType(regType);
    SingleTypeRegSet busy  = avoid | minOptsLiveRegs.GetRegSetForType(regType);
    SingleTypeRegSet free  = candidates & ~busy;

    if (free != RBM_NONE)
    {
        const int        typeIndex = minOptsRegOrderIndex(regType);
        const regNumber* order     = minOptsRegOrder[typeIndex];
        const unsigned   count     = minOptsRegOrderSize[typeIndex];
        for (unsigned i = 0; i < count; i++)
        {
            if ((free & genSingleTypeRegMask(order[i])) != RBM_NONE)
            {
                return order[i];
            }
        }

        // Not covered by the preference order (should not normally happen); just take the first one.
        return genRegNumFromMask(genFindLowestBit(free), regType);
    }

    if (refPosition->RegOptional())
    {
        return REG_NA;
    }

    // Nothing is free; spill a live value to make room.
    SingleTypeRegSet spillable = candidates & ~avoid;

    noway_assert(spillable != RBM_NONE);

    regNumber spillReg = genRegNumFromMask(genFindLowestBit(spillable), regType);
    Interval* toSpill  = minOptsRegToInterval[spillReg];
    if (toSpill != nullptr)
    {
        minOptsSpillInterval(toSpill);
    }

    return spillReg;
}

//------------------------------------------------------------------------
// allocateNodeMinOpts: Assign registers to the RefPositions that were just built
//    for 'node', and write the assignments back into the IR.
//
void LinearScan::allocateNodeMinOpts(BasicBlock* block, GenTree* node)
{
    const unsigned     count = minOptsNodeRefPositionCount;
    const LsraLocation loc   = currentLoc;

    // Collect the single-register requirements and kills of this node up front, so that
    // we know which registers a later RefPosition of the same node is going to need.
    minOptsFixedRegsThisLoc = RBM_NONE;
    minOptsFixedRegsNextLoc = RBM_NONE;
    minOptsKilledRegs       = RBM_NONE;

    for (unsigned i = 0; i < count; i++)
    {
        RefPosition* refPosition = minOptsNodeRefPositions[i];

        if (refPosition->refType == RefTypeKill)
        {
            minOptsKilledRegs |= refPosition->getKilledRegisters();

            if (refPosition->delayRegFree)
            {
                // This kill reserves its registers for the node that follows this one (the
                // async continuation and the Swift error register), so nothing this node
                // defines may land there.
                minOptsFixedRegsNextLoc |= refPosition->getKilledRegisters();
            }
        }
        else if (refPosition->refType == RefTypeFixedReg)
        {
            // A RefPosition on a physical register: it reserves that register for the whole
            // node. (NativeAOT arm64 TLS access reserves the registers the linker patches.)
            minOptsFixedRegsThisLoc |= genRegMask(refPosition->getReg()->regNum);
        }
        else if (refPosition->isFixedRegRef)
        {
            assert(RefTypeIsDef(refPosition->refType) || RefTypeIsUse(refPosition->refType));
            regMaskTP* fixedRegs =
                (refPosition->nodeLocation == loc) ? &minOptsFixedRegsThisLoc : &minOptsFixedRegsNextLoc;
            fixedRegs->AddRegsetForType(refPosition->registerAssignment, refPosition->getRegisterType());
        }
    }

    minOptsRegsInUse       = RBM_NONE;
    minOptsRegsToFree      = RBM_NONE;
    minOptsDelayRegsToFree = RBM_NONE;
    minOptsCurLoc          = loc;

    for (unsigned i = 0; i < count; i++)
    {
        RefPosition* refPosition = minOptsNodeRefPositions[i];

        if (refPosition->nodeLocation > minOptsCurLoc)
        {
            // Move on to the def location of this node. Registers whose last use was at the
            // previous location become available, except for the delay-free ones.
            assert(refPosition->nodeLocation == loc + 1);
            minOptsCurLoc = refPosition->nodeLocation;

            for (regMaskTP freeing = minOptsRegsToFree; freeing.IsNonEmpty();)
            {
                regNumber freeReg            = genFirstRegNumFromMaskAndToggle(freeing);
                minOptsRegFreeSince[freeReg] = minOptsCurLoc;
            }

            minOptsLiveRegs &= ~minOptsRegsToFree;
            minOptsRegsToFree = RBM_NONE;
            minOptsRegsInUse  = minOptsDelayRegsToFree;
        }

        switch (refPosition->refType)
        {
            case RefTypeKill:
                minOptsProcessKill(refPosition->getKilledRegisters());
                break;

            case RefTypeKillGCRefs:
                minOptsSpillGCRefs(refPosition);
                break;

            case RefTypeFixedReg:
                // The register is reserved for the whole node (collected above); make sure
                // nothing is left living in it.
                minOptsVacateReg(refPosition->getReg()->regNum);
                break;

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            case RefTypeUpperVectorSave:
            {
                // In MinOpts these only ever occur for tree temps of a large vector type that are
                // live across a call. We simply spill the whole value.
                Interval* interval = refPosition->getInterval();
                assert(!interval->isLocalVar);
                if (interval->physReg != REG_NA)
                {
                    minOptsSpillInterval(interval);
                }
                refPosition->registerAssignment = RBM_NONE;

                // Keep the interval's recent RefPosition pointing at a RefPosition that stays
                // alive, since this one is about to be recycled.
                interval->recentRefPosition = interval->firstRefPosition;
                break;
            }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

            case RefTypeDef:
                allocateDefMinOpts(refPosition);
                break;

            case RefTypeUse:
                allocateUseMinOpts(block, refPosition);
                break;

            default:
                noway_assert(!"Unexpected RefType in the MinOpts allocator");
                break;
        }
    }

    // Release everything this node reserved.
    regMaskTP freed = minOptsRegsToFree | minOptsDelayRegsToFree;
    minOptsLiveRegs &= ~freed;

    for (regMaskTP freeing = freed; freeing.IsNonEmpty();)
    {
        regNumber freeReg            = genFirstRegNumFromMaskAndToggle(freeing);
        minOptsRegFreeSince[freeReg] = loc + 2;
    }

    minOptsNodeRefPositionCount = 0;
}

//------------------------------------------------------------------------
// allocateDefMinOpts: Allocate a register for a definition.
//
void LinearScan::allocateDefMinOpts(RefPosition* refPosition)
{
    Interval*    interval = refPosition->getInterval();
    RegisterType regType  = interval->registerType;

    assert(interval->physReg == REG_NA);
    assert(!interval->isLocalVar);

    SingleTypeRegSet candidates = getAvailableGPRsForType(refPosition->registerAssignment, regType);
    if (refPosition->isFixedRegRef)
    {
        // This requirement is being satisfied now, so later RefPositions of this node
        // no longer have to avoid the register.
        regMaskTP* fixedRegs =
            (refPosition->nodeLocation == currentLoc) ? &minOptsFixedRegsThisLoc : &minOptsFixedRegsNextLoc;
        fixedRegs->RemoveRegsetForType(refPosition->registerAssignment, regType);
    }

    regNumber reg = minOptsSelectReg(refPosition, candidates, regType);
    noway_assert(reg != REG_NA);

    // Remember the full candidate set: if a later use wants the value in a specific
    // register we may retarget this definition, but only within these candidates.
    interval->registerPreferences = candidates;

    refPosition->registerAssignment = genSingleTypeRegMask(reg);
    minOptsAssignReg(interval, reg);

    if (interval->isInternal)
    {
        m_compiler->codeGen->internalRegisters.Add(refPosition->treeNode, refPosition->registerAssignment);
        minOptsMarkModified(reg);
    }
    else
    {
        lsraAssignRegToTree(refPosition->treeNode, reg, refPosition->getMultiRegIdx());
    }

    JITDUMP("      Def [%06u] -> %s\n", Compiler::dspTreeID(refPosition->treeNode), getRegName(reg));

    if (refPosition->isLocalDefUse)
    {
        // A dead definition: the register is free again immediately.
        minOptsMarkModified(reg);
        minOptsFreeReg(interval);
        minOptsRegsToFree.AddRegsetForType(getSingleTypeRegMask(reg, regType), regType);
    }
}

//------------------------------------------------------------------------
// allocateUseMinOpts: Allocate a register for a use.
//
void LinearScan::allocateUseMinOpts(BasicBlock* block, RefPosition* refPosition)
{
    Interval*    interval = refPosition->getInterval();
    RegisterType regType  = interval->registerType;

    assert(!interval->isLocalVar);
    assert(refPosition->lastUse);

    SingleTypeRegSet candidates = getAvailableGPRsForType(refPosition->registerAssignment, regType);
    if (refPosition->isFixedRegRef)
    {
        minOptsFixedRegsThisLoc.RemoveRegsetForType(refPosition->registerAssignment, regType);
    }

    RefPosition* defRefPosition = interval->firstRefPosition;
    regNumber    assignedReg    = interval->physReg;

    if (assignedReg != REG_NA)
    {
        SingleTypeRegSet assignedRegMask = genSingleTypeRegMask(assignedReg);

        // Does the register the value is in satisfy this use, and is it free of any
        // conflicting fixed requirement of this node?
        regMaskTP conflicting = minOptsFixedRegsThisLoc;
        if (refPosition->delayRegFree)
        {
            conflicting |= minOptsFixedRegsNextLoc | minOptsKilledRegs;
        }

        bool keepAssignment;
        if (refPosition->isFixedRegRef && genMaxOneBit(candidates))
        {
            // A fixed use always gets its register, and it never conflicts with itself.
            keepAssignment = (candidates == assignedRegMask);
        }
        else
        {
            keepAssignment =
                ((candidates & assignedRegMask) != RBM_NONE) && !conflicting.IsRegNumPresent(assignedReg, regType);
        }

        if (keepAssignment)
        {
            // Keep the current assignment.
            refPosition->registerAssignment = assignedRegMask;
            minOptsUseDone(refPosition, interval, assignedReg);
            return;
        }

        // The value is in the wrong register. If the register this use requires could have
        // held the value all along, retarget the definition and avoid a move altogether.
        if (minOptsTryRetarget(interval, candidates, conflicting))
        {
            refPosition->registerAssignment = genSingleTypeRegMask(interval->physReg);
            minOptsUseDone(refPosition, interval, interval->physReg);
            return;
        }

        // We have to insert an explicit move.
        SingleTypeRegSet copyCandidates = candidates & ~assignedRegMask;
        if (copyCandidates == RBM_NONE)
        {
            copyCandidates = candidates;
        }

        regNumber copyReg = minOptsSelectReg(refPosition, copyCandidates, regType);
        if (copyReg == REG_NA)
        {
            // Nothing is available for the move, but this use is able to consume the value
            // from memory, so spill the definition instead. (minOptsSelectReg only declines
            // to spill something else when the use is reg-optional.)
            assert(refPosition->RegOptional());
            minOptsMarkModified(assignedReg);
            minOptsSpillInterval(interval);

            defRefPosition->treeNode->gtFlags |= GTF_NOREG_AT_USE;
            refPosition->registerAssignment = RBM_NONE;

            JITDUMP("      Use of [%06u] from memory\n", Compiler::dspTreeID(defRefPosition->treeNode));
            return;
        }

        assert(copyReg != assignedReg);

        refPosition->registerAssignment = genSingleTypeRegMask(copyReg);
        refPosition->moveReg            = true;

        // Release the old register and take the new one.
        minOptsLiveRegs.RemoveRegsetForType(getSingleTypeRegMask(assignedReg, regType), regType);
        minOptsRegFreeSince[assignedReg] = minOptsCurLoc;
        minOptsFreeReg(interval);

        minOptsAssignReg(interval, copyReg);
        insertCopyOrReload(block, defRefPosition->treeNode, refPosition->getMultiRegIdx(), refPosition);

        JITDUMP("      Copy [%06u] from %s to %s\n", Compiler::dspTreeID(defRefPosition->treeNode),
                getRegName(assignedReg), getRegName(copyReg));

        minOptsUseDone(refPosition, interval, copyReg);
        return;
    }

    // The value has been spilled and has to be reloaded (or, if it is optional, used
    // directly from memory).
    assert(defRefPosition->spillAfter);
    assert(defRefPosition->treeNode != nullptr);

    if (refPosition->RegOptional())
    {
        // We could allocate a register here, but since the value is already in memory and
        // this is its only use, it is cheaper to consume it directly from memory.
        defRefPosition->treeNode->gtFlags |= GTF_NOREG_AT_USE;
        refPosition->registerAssignment = RBM_NONE;
        minOptsMarkModified(defRefPosition->assignedReg());

        JITDUMP("      Use of [%06u] from memory\n", Compiler::dspTreeID(defRefPosition->treeNode));
        return;
    }

    regNumber reloadReg = minOptsSelectReg(refPosition, candidates, regType);
    noway_assert(reloadReg != REG_NA);

    refPosition->registerAssignment = genSingleTypeRegMask(reloadReg);
    refPosition->reload             = true;
    minOptsAssignReg(interval, reloadReg);

    if (defRefPosition->assignedReg() != reloadReg)
    {
        // The value has to come back in a different register than it was spilled from,
        // so an explicit reload node is required.
        insertCopyOrReload(block, defRefPosition->treeNode, refPosition->getMultiRegIdx(), refPosition);
    }

    JITDUMP("      Reload [%06u] into %s\n", Compiler::dspTreeID(defRefPosition->treeNode), getRegName(reloadReg));

    minOptsUseDone(refPosition, interval, reloadReg);
}

//------------------------------------------------------------------------
// minOptsUseDone: Common bookkeeping once a use has been given a register.
//
void LinearScan::minOptsUseDone(RefPosition* refPosition, Interval* interval, regNumber reg)
{
    RegisterType     regType = interval->registerType;
    SingleTypeRegSet regMask = getSingleTypeRegMask(reg, regType);

    minOptsRegsInUse.AddRegsetForType(regMask, regType);

    // The definition can no longer be retargeted, so its register assignment (and the one
    // for this use, if a copy or reload was needed) is now final.
    minOptsMarkModified(interval->firstRefPosition->assignedReg());
    minOptsMarkModified(reg);

    // The value dies here, but the register itself stays reserved until the end of the
    // current location (or, for delay-free uses, until the end of the node).
    minOptsFreeReg(interval);

    if (refPosition->delayRegFree)
    {
        minOptsDelayRegsToFree.AddRegsetForType(regMask, regType);
    }
    else
    {
        minOptsRegsToFree.AddRegsetForType(regMask, regType);
    }
}

//------------------------------------------------------------------------
// allocateBlockMinOpts: Build RefPositions and allocate registers for one block.
//
void LinearScan::allocateBlockMinOpts(BasicBlock* block)
{
    JITDUMP("\n" FMT_BB ":\n", block->bbNum);

    m_compiler->compCurBB = block;

    // No value is live across a block boundary, so everything starts out free.
    minOptsLiveRegs        = RBM_NONE;
    minOptsRegsInUse       = RBM_NONE;
    minOptsRegsToFree      = RBM_NONE;
    minOptsDelayRegsToFree = RBM_NONE;

    for (regNumber reg = REG_FIRST; reg < REG_COUNT; reg = REG_NEXT(reg))
    {
        minOptsRegToInterval[reg] = nullptr;
        minOptsRegFreeSince[reg]  = MinLocation;
    }

    // State is not live across blocks, so the FP register kill switch is per block.
    needToKillFloatRegs = false;

    if ((block == m_compiler->fgFirstBB) && m_compiler->lvaHasAnySwiftStackParamToReassemble())
    {
        m_compiler->codeGen->regSet.rsSetRegsModified(genRegMask(REG_SCRATCH) DEBUGARG(true));
    }
#ifdef TARGET_X86
    if ((block == m_compiler->fgFirstBB) && m_compiler->info.compIsVarArgs)
    {
        m_compiler->codeGen->regSet.rsSetRegsModified(genRegMask(REG_SCRATCH) DEBUGARG(true));
    }
#endif

    if (m_compiler->compShouldPoisonFrame() && (block == m_compiler->fgFirstBB))
    {
        regMaskTP killed;
#if defined(TARGET_XARCH)
        killed = RBM_EDI | RBM_ECX | RBM_EAX;
#else
        killed = m_compiler->compHelperCallKillSet(CORINFO_HELP_NATIVE_MEMSET);
        killed.AddRegNumInMask(REG_SCRATCH);
#endif
        m_compiler->codeGen->regSet.rsSetRegsModified(killed DEBUGARG(true));
    }

    // Mark the block boundary, mirroring what buildIntervals does. Nothing is live across
    // it, but some Build code assumes the RefPosition list is never empty.
    newRefPosition((Interval*)nullptr, currentLoc, RefTypeBB, nullptr, RBM_NONE);
    minOptsNodeRefPositionCount = 0;
    currentLoc += 2;

    LIR::Range& blockRange = LIR::AsRange(block);
    for (GenTree* node : blockRange)
    {
#ifdef DEBUG
        node->gtSeqNum = currentLoc;
        // Although this looks like a no-op it sets the gtRegTag so that dumps show the register.
        node->SetRegNum(node->GetRegNum());
#endif

        assert(minOptsNodeRefPositionCount == 0);

        buildRefPositionsForNode(node, currentLoc);

        if (minOptsNodeRefPositionCount != 0)
        {
            allocateNodeMinOpts(block, node);
        }

        currentLoc += 2;
    }

    if (m_compiler->getNeedsGSSecurityCookie() && block->KindIs(BBJ_RETURN))
    {
        // The cookie check will kill some registers that it uses; make sure they are
        // reported as modified.
        bool isTailCall = block->HasFlag(BBF_HAS_JMP);
        m_compiler->codeGen->regSet.rsSetRegsModified(m_compiler->codeGen->genGetGSCookieTempRegs(isTailCall)
                                                          DEBUGARG(true));
    }

    assert(defList.IsEmpty());
    markBlockVisited(block);
}

//------------------------------------------------------------------------
// doRegisterAllocationMinOpts: The MinOpts register allocation phase.
//
PhaseStatus LinearScan::doRegisterAllocationMinOpts()
{
    assert(!enregisterLocalVars);

    minOptsRegAlloc = true;
    minOptsAnySpill = false;

    m_compiler->codeGen->regSet.rsClearRegsModified();
    initMaxSpill();

    // No local variable is enregistered, so this just clears the maps.
    initVarRegMaps();

#ifdef TARGET_ARM64
    nextConsecutiveRefPositionMap = nullptr;
#endif

    buildPhysRegRecords();

#if DOUBLE_ALIGN
    // identifyCandidates() determines this, but it can return early without setting it.
    doDoubleAlign = false;
#endif

    identifyCandidates<false>();

    // Figure out if we're going to use a frame pointer. This has to happen before we build
    // any RefPositions, since they embed register masks that depend on it.
    setFrameType();

#if defined(TARGET_XARCH)
#if defined(TARGET_AMD64)
    lowGprRegs = (availableIntRegs & RBM_LOWINT.GetIntRegSet());
#else
    lowGprRegs = availableIntRegs;
#endif // TARGET_AMD64
#endif // TARGET_XARCH

    minOptsBuildRegOrder();

    if (!blockSequencingDone)
    {
        setBlockSequence();
    }

    curBBNum = 0;

    computeCalleeRegArgMaskLiveIn();

    numPlacedArgLocals = 0;
    placedArgRegs      = RBM_NONE;

    regsBusyUntilKill     = RBM_NONE;
    regsInUseThisLocation = RBM_NONE;
    regsInUseNextLocation = RBM_NONE;

    currentLoc = 1;

    JITDUMP("\n*************** In LinearScan::doRegisterAllocationMinOpts()\n");

    for (BasicBlock* block = startBlockSequence(); block != nullptr; block = moveToNextBlock())
    {
        allocateBlockMinOpts(block);
    }

    allocationPassComplete = true;

    // Compute the number of concurrently live spill temps. A temp is acquired by codegen
    // where the spilled value is defined (that is where GTF_SPILL is set) and released
    // where it is reloaded, so this cannot be derived from the order in which we decided
    // to spill. Walk the RefPositions and reuse the shared accounting instead.
    if (minOptsAnySpill)
    {
        for (RefPosition& refPosition : refPositions)
        {
            if ((refPosition.refType == RefTypeDef) || (refPosition.refType == RefTypeUse)
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
                || (refPosition.refType == RefTypeUpperVectorSave)
#endif
            )
            {
                updateMaxSpill(&refPosition);
            }
        }
    }

    needNonIntegerRegisters |= m_compiler->compFloatingPointUsed;
    if (!needNonIntegerRegisters)
    {
        availableRegCount = REG_INT_COUNT;
    }

    if (availableRegCount < (sizeof(regMaskSmall) * 8))
    {
        // Mask out the bits that are between (8 * regMaskSmall) ~ availableRegCount
        actualRegistersMask = regMaskTP((1ULL << availableRegCount) - 1);
    }
#ifdef HAS_MORE_THAN_64_REGISTERS
    else if (availableRegCount < (sizeof(regMaskTP) * 8))
    {
        actualRegistersMask = regMaskTP(~RBM_NONE, availableMaskRegs);
    }
#endif
    else
    {
        actualRegistersMask = regMaskTP(~RBM_NONE, ~0);
    }

    m_compiler->EndPhase(PHASE_LINEAR_SCAN_BUILD);
    m_compiler->EndPhase(PHASE_LINEAR_SCAN_ALLOC);

    m_compiler->raMarkStkVars();
    recordMaxSpill();

    m_compiler->EndPhase(PHASE_LINEAR_SCAN_RESOLVE);

#ifdef DEBUG
    if (VERBOSE)
    {
        printf("Trees after linear scan register allocator (LSRA)\n");
        m_compiler->fgDispBasicBlocks(true);
    }

    m_compiler->fgDebugCheckLinks();
#endif

    m_compiler->compRegAllocDone = true;

    // We never create new blocks, so the flowgraph annotations are still valid.
    assert(m_compiler->fgBBcount == bbSeqCount);

    return PhaseStatus::MODIFIED_EVERYTHING;
}
