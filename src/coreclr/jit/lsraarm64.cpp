// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                    Register Requirements for ARM64                        XX
XX                                                                           XX
XX  This encapsulates all the logic for setting register requirements for    XX
XX  the ARM64 architecture.                                                  XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_ARM64

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"

//------------------------------------------------------------------------
// getNextConsecutiveRefPosition: Get the next subsequent RefPosition.
//
// Arguments:
//    refPosition   - The RefPosition for which we need to find the next RefPosition.
//
// Return Value:
//    The next RefPosition or nullptr if there is not one.
//
RefPosition* LinearScan::getNextConsecutiveRefPosition(RefPosition* refPosition)
{
    assert(compiler->info.compNeedsConsecutiveRegisters);
    RefPosition* nextRefPosition = nullptr;
    assert(refPosition->needsConsecutive);
    nextConsecutiveRefPositionMap->Lookup(refPosition, &nextRefPosition);
    assert((nextRefPosition == nullptr) || nextRefPosition->needsConsecutive);
    return nextRefPosition;
}

//------------------------------------------------------------------------
// assignConsecutiveRegisters: For subsequent RefPositions, set the register
//   requirement to be the consecutive register(s) of the register that is assigned to
//   the firstRefPosition.
//   If one of the subsequent RefPosition is RefTypeUpperVectorRestore, sets the
//   registerAssignment to not include any of the consecutive registers that are being
//   assigned to the RefTypeUse RefPositions.
//
// Arguments:
//    firstRefPosition  - First RefPosition of the series of consecutive registers.
//    firstRegAssigned  - Register assigned to the first RefPosition.
//
//  Note:
//      This method will set the registerAssignment of subsequent RefPositions with consecutive registers.
//      Some of the registers could be busy, and they will be spilled. We would end up with busy registers if
//      we did not find free consecutive registers.
//
void LinearScan::assignConsecutiveRegisters(RefPosition* firstRefPosition, regNumber firstRegAssigned)
{
    assert(compiler->info.compNeedsConsecutiveRegisters);
    assert(firstRefPosition->assignedReg() == firstRegAssigned);
    assert(firstRefPosition->isFirstRefPositionOfConsecutiveRegisters());
    assert(emitter::isVectorRegister(firstRegAssigned));
    assert(consecutiveRegsInUseThisLocation == RBM_NONE);

    RefPosition* consecutiveRefPosition = getNextConsecutiveRefPosition(firstRefPosition);
    regNumber    regToAssign            = firstRegAssigned == REG_FP_LAST ? REG_FP_FIRST : REG_NEXT(firstRegAssigned);

    // First RefPosition should always start with RefTypeUse
    assert(firstRefPosition->refType != RefTypeUpperVectorRestore);

    INDEBUG(int refPosCount = 1);
    consecutiveRegsInUseThisLocation = (((1ULL << firstRefPosition->regCount) - 1) << firstRegAssigned);

    while (consecutiveRefPosition != nullptr)
    {
        assert(consecutiveRefPosition->regCount == 0);
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        if (consecutiveRefPosition->refType == RefTypeUpperVectorRestore)
        {
            Interval* srcInterval = consecutiveRefPosition->getInterval();
            assert(srcInterval->isUpperVector);
            assert(srcInterval->relatedInterval != nullptr);
            if (srcInterval->relatedInterval->isPartiallySpilled)
            {
                // Make sure that restore doesn't get one of the registers that are part of series we are trying to set
                // currently.
                // TODO-CQ: We could technically assign RefTypeUpperVectorRestore and its RefTypeUse same register, but
                // during register selection, it might get tricky to know which of the busy registers are assigned to
                // RefTypeUpperVectorRestore positions of corresponding variables for which (another criteria)
                // we are trying to find consecutive registers.

                consecutiveRefPosition->registerAssignment &= ~consecutiveRegsInUseThisLocation;
            }
            consecutiveRefPosition = getNextConsecutiveRefPosition(consecutiveRefPosition);
        }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        INDEBUG(refPosCount++);
        assert((consecutiveRefPosition->refType == RefTypeDef) || (consecutiveRefPosition->refType == RefTypeUse));
        consecutiveRefPosition->registerAssignment = genSingleTypeRegMask(regToAssign);
        consecutiveRefPosition                     = getNextConsecutiveRefPosition(consecutiveRefPosition);
        regToAssign                                = regToAssign == REG_FP_LAST ? REG_FP_FIRST : REG_NEXT(regToAssign);
    }

    assert(refPosCount == firstRefPosition->regCount);
}

//------------------------------------------------------------------------
// canAssignNextConsecutiveRegisters: Starting with `firstRegAssigned`, check if next
//   consecutive registers are free or are already assigned to the subsequent RefPositions.
//
// Arguments:
//    firstRefPosition  - First RefPosition of the series of consecutive registers.
//    firstRegAssigned  - Register assigned to the first RefPosition.
//
//  Returns:
//      True if all the consecutive registers starting from `firstRegAssigned` are assignable.
//      Even if one of them is busy, returns false.
//
bool LinearScan::canAssignNextConsecutiveRegisters(RefPosition* firstRefPosition, regNumber firstRegAssigned)
{
    int          registersCount  = firstRefPosition->regCount;
    RefPosition* nextRefPosition = firstRefPosition;
    regNumber    regToAssign     = firstRegAssigned;
    assert(compiler->info.compNeedsConsecutiveRegisters);
    assert(registersCount > 1);
    assert(emitter::isVectorRegister(firstRegAssigned));

    int i = 1;
    do
    {
        nextRefPosition = getNextConsecutiveRefPosition(nextRefPosition);
        regToAssign     = regToAssign == REG_FP_LAST ? REG_FP_FIRST : REG_NEXT(regToAssign);
        if (!isFree(getRegisterRecord(regToAssign)))
        {
            if (nextRefPosition->refType == RefTypeUpperVectorRestore)
            {
                nextRefPosition = getNextConsecutiveRefPosition(nextRefPosition);
            }

            Interval* interval = nextRefPosition->getInterval();

            // If regToAssign is not free, make sure it is not in use at current location.
            // If not, then check if it is already assigned to the interval corresponding
            // to the subsequent nextRefPosition.
            // If yes, it would just use regToAssign for that nextRefPosition.
            if ((interval != nullptr) && !isRegInUse(regToAssign, interval->registerType) &&
                (interval->assignedReg != nullptr) && ((interval->assignedReg->regNum == regToAssign)))
            {
                continue;
            }

            return false;
        }
    } while (++i != registersCount);

    return true;
}

//------------------------------------------------------------------------
// filterConsecutiveCandidates: Given `candidates`, check if `registersNeeded` consecutive
//   registers are available in it, and if yes, returns first bit set of every possible series.
//
// Arguments:
//    candidates                - Set of available candidates.
//    registersNeeded           - Number of consecutive registers needed.
//    allConsecutiveCandidates  - Mask returned containing all bits set for possible consecutive register candidates.
//
//  Returns:
//      From `candidates`, the mask of series of consecutive registers of `registersNeeded` size with just the first-bit
//      set.
//
SingleTypeRegSet LinearScan::filterConsecutiveCandidates(SingleTypeRegSet  floatCandidates,
                                                         unsigned int      registersNeeded,
                                                         SingleTypeRegSet* allConsecutiveCandidates)
{
    assert((floatCandidates == RBM_NONE) || (floatCandidates & availableFloatRegs) != RBM_NONE);

    if (PopCount(floatCandidates) < registersNeeded)
    {
        // There is no way the register demanded can be satisfied for this RefPosition
        // based on the candidates from which it can allocate a register.
        return RBM_NONE;
    }

    SingleTypeRegSet currAvailableRegs = floatCandidates;
    SingleTypeRegSet overallResult     = RBM_NONE;
    SingleTypeRegSet consecutiveResult = RBM_NONE;

// At this point, for 'n' registers requirement, if Rm, Rm+1, Rm+2, ..., Rm+k-1 are
// available, create the mask only for Rm, Rm+1, ..., Rm+(k-n) to convey that it
// is safe to assign any of those registers, but not beyond that.
#define AppendConsecutiveMask(startIndex, endIndex, availableRegistersMask)                                            \
    SingleTypeRegSet selectionStartMask = (1ULL << regAvailableStartIndex) - 1;                                        \
    SingleTypeRegSet selectionEndMask   = (1ULL << (regAvailableEndIndex - registersNeeded + 1)) - 1;                  \
    consecutiveResult |= availableRegistersMask & (selectionEndMask & ~selectionStartMask);                            \
    overallResult |= availableRegistersMask;

    unsigned regAvailableStartIndex = 0, regAvailableEndIndex = 0;

    do
    {
        // From LSB, find the first available register (bit `1`)
        regAvailableStartIndex     = BitScanForward(currAvailableRegs);
        SingleTypeRegSet startMask = (1ULL << regAvailableStartIndex) - 1;

        // Mask all the bits that are processed from LSB thru regAvailableStart until the last `1`.
        SingleTypeRegSet maskProcessed = ~(currAvailableRegs | startMask);

        // From regAvailableStart, find the first unavailable register (bit `0`).
        if (maskProcessed == RBM_NONE)
        {
            regAvailableEndIndex = 64;
            if ((regAvailableEndIndex - regAvailableStartIndex) >= registersNeeded)
            {
                AppendConsecutiveMask(regAvailableStartIndex, regAvailableEndIndex, currAvailableRegs);
            }
            break;
        }
        else
        {
            regAvailableEndIndex = BitScanForward(maskProcessed);
        }
        SingleTypeRegSet endMask = (1ULL << regAvailableEndIndex) - 1;

        // Anything between regAvailableStart and regAvailableEnd is the range of consecutive registers available.
        // If they are equal to or greater than our register requirements, then add all of them to the result.
        if ((regAvailableEndIndex - regAvailableStartIndex) >= registersNeeded)
        {
            AppendConsecutiveMask(regAvailableStartIndex, regAvailableEndIndex, (endMask & ~startMask));
        }
        currAvailableRegs &= ~endMask;
    } while (currAvailableRegs != RBM_NONE);

    SingleTypeRegSet v0_v31_mask = SRBM_V0 | SRBM_V31;
    if ((floatCandidates & v0_v31_mask) == v0_v31_mask)
    {
        // Finally, check for round robin case where sequence of last register
        // round to first register is available.
        // For n registers needed, it checks if MSB (n-1) + LSB (1) or
        // MSB (n - 2) + LSB (2) registers are available and if yes,
        // set the least bit of such MSB.
        //
        // This could have done using bit-twiddling, but is simpler when the
        // checks are done with these hardcoded values.
        switch (registersNeeded)
        {
            case 2:
            {
                if ((floatCandidates & v0_v31_mask) != RBM_NONE)
                {
                    consecutiveResult |= SRBM_V31;
                    overallResult |= v0_v31_mask;
                }
                break;
            }
            case 3:
            {
                SingleTypeRegSet v0_v30_v31_mask = SRBM_V0 | SRBM_V30 | SRBM_V31;
                if ((floatCandidates & v0_v30_v31_mask) != RBM_NONE)
                {
                    consecutiveResult |= SRBM_V30;
                    overallResult |= v0_v30_v31_mask;
                }

                SingleTypeRegSet v0_v1_v31_mask = SRBM_V0 | SRBM_V1 | SRBM_V31;
                if ((floatCandidates & v0_v1_v31_mask) != RBM_NONE)
                {
                    consecutiveResult |= SRBM_V31;
                    overallResult |= v0_v1_v31_mask;
                }
                break;
            }
            case 4:
            {
                SingleTypeRegSet v0_v29_v30_v31_mask = SRBM_V0 | SRBM_V29 | SRBM_V30 | SRBM_V31;
                if ((floatCandidates & v0_v29_v30_v31_mask) != RBM_NONE)
                {
                    consecutiveResult |= SRBM_V29;
                    overallResult |= v0_v29_v30_v31_mask;
                }

                SingleTypeRegSet v0_v1_v30_v31_mask = SRBM_V0 | SRBM_V29 | SRBM_V30 | SRBM_V31;
                if ((floatCandidates & v0_v1_v30_v31_mask) != RBM_NONE)
                {
                    consecutiveResult |= SRBM_V30;
                    overallResult |= v0_v1_v30_v31_mask;
                }

                SingleTypeRegSet v0_v1_v2_v31_mask = SRBM_V0 | SRBM_V29 | SRBM_V30 | SRBM_V31;
                if ((floatCandidates & v0_v1_v2_v31_mask) != RBM_NONE)
                {
                    consecutiveResult |= SRBM_V31;
                    overallResult |= v0_v1_v2_v31_mask;
                }
                break;
            }
            default:
                assert(!"Unexpected registersNeeded\n");
        }
    }

    // consecutiveResult should always be a subset of overallResult
    assert((overallResult & consecutiveResult) == consecutiveResult);
    *allConsecutiveCandidates = overallResult;
    return consecutiveResult;
}

//------------------------------------------------------------------------
// filterConsecutiveCandidatesForSpill: Amoung the selected consecutiveCandidates,
//   check if there are any ranges that would require fewer registers to spill
//   and returns such mask. The return result would always be a subset of
//   consecutiveCandidates.
//
// Arguments:
//    consecutiveCandidates   - Consecutive candidates to filter  on.
//    registersNeeded         - Number of registers needed.
//
//  Returns:
//      Filtered candidates that needs fewer spilling.
//
SingleTypeRegSet LinearScan::filterConsecutiveCandidatesForSpill(SingleTypeRegSet consecutiveCandidates,
                                                                 unsigned int     registersNeeded)
{
    assert(consecutiveCandidates != RBM_NONE);
    assert((registersNeeded >= 2) && (registersNeeded <= 4));
    SingleTypeRegSet consecutiveResultForBusy = RBM_NONE;
    SingleTypeRegSet unprocessedRegs          = consecutiveCandidates;
    unsigned         regAvailableStartIndex = 0, regAvailableEndIndex = 0;
    int              maxSpillRegs         = registersNeeded;
    SingleTypeRegSet registersNeededMask  = (1ULL << registersNeeded) - 1;
    SingleTypeRegSet availableFloatRegSet = m_AvailableRegs.GetFloatRegSet();
    do
    {
        // From LSB, find the first available register (bit `1`)
        regAvailableStartIndex = BitScanForward(unprocessedRegs);

        // For the current range, find how many registers are free vs. busy
        SingleTypeRegSet maskForCurRange        = RBM_NONE;
        bool             shouldCheckForRounding = false;
        switch (registersNeeded)
        {
            case 2:
                shouldCheckForRounding = (regAvailableStartIndex == 63);
                break;
            case 3:
                shouldCheckForRounding = (regAvailableStartIndex >= 62);
                break;
            case 4:
                shouldCheckForRounding = (regAvailableStartIndex >= 61);
                break;
            default:
                assert("Unsupported registersNeeded\n");
                break;
        }

        if (shouldCheckForRounding)
        {
            unsigned int roundedRegistersNeeded = registersNeeded - (63 - regAvailableStartIndex + 1);
            maskForCurRange                     = (1ULL << roundedRegistersNeeded) - 1;
        }

        maskForCurRange |= (registersNeededMask << regAvailableStartIndex);
        maskForCurRange &= availableFloatRegSet;

        if (maskForCurRange != RBM_NONE)
        {
            // In the given range, there are some free registers available. Calculate how many registers
            // will need spilling if this range is picked.

            int curSpillRegs = registersNeeded - PopCount(maskForCurRange);
            if (curSpillRegs < maxSpillRegs)
            {
                consecutiveResultForBusy = 1ULL << regAvailableStartIndex;
                maxSpillRegs             = curSpillRegs;
            }
            else if (curSpillRegs == maxSpillRegs)
            {
                consecutiveResultForBusy |= 1ULL << regAvailableStartIndex;
            }
        }
        unprocessedRegs &= ~(1ULL << regAvailableStartIndex);
    } while (unprocessedRegs != RBM_NONE);

    // consecutiveResultForBusy should always be a subset of consecutiveCandidates.
    assert((consecutiveCandidates & consecutiveResultForBusy) == consecutiveResultForBusy);
    return consecutiveResultForBusy;
}

//------------------------------------------------------------------------
// getConsecutiveCandidates: Returns the mask of all the consecutive candidates
//   for given RefPosition. For first RefPosition of a series of RefPositions that needs
//   consecutive registers, then returns only the mask such that it satisfies the need
//   of having free consecutive registers. If free consecutive registers are not available
//   it finds such a series that needs fewer registers spilling.
//
// Arguments:
//    allCandidates   - Register assigned to the first RefPosition.
//    refPosition     - Number of registers to check.
//    busyCandidates  - Register mask of free/busy registers.
//
//  Returns:
//      Register mask of free consecutive registers. If there are not enough free registers,
//      or the free registers are not consecutive, then return RBM_NONE. In that case,
//      `busyCandidates` will contain the register mask that can be assigned and will include
//      both free and busy registers.
//
//  Notes:
//      The consecutive registers mask includes just the bits of first registers or
//      (n - k) registers. For example, if we need 3 consecutive registers and
//      allCandidates = 0x1C080D0F00000000, the consecutive register mask returned
//      will be 0x400000300000000.
//
SingleTypeRegSet LinearScan::getConsecutiveCandidates(SingleTypeRegSet  allCandidates,
                                                      RefPosition*      refPosition,
                                                      SingleTypeRegSet* busyCandidates)
{
    assert(compiler->info.compNeedsConsecutiveRegisters);
    assert(refPosition->isFirstRefPositionOfConsecutiveRegisters());
    SingleTypeRegSet floatFreeCandidates = allCandidates & m_AvailableRegs.GetFloatRegSet();

#ifdef DEBUG
    if (getStressLimitRegs() != LSRA_LIMIT_NONE)
    {
        // For stress, make only alternate registers available so we can stress the selection of free/busy registers.
        floatFreeCandidates &= SRBM_V0 | SRBM_V2 | SRBM_V4 | SRBM_V6 | SRBM_V8 | SRBM_V10 | SRBM_V12 | SRBM_V14 |
                               SRBM_V16 | SRBM_V18 | SRBM_V20 | SRBM_V22 | SRBM_V24 | SRBM_V26 | SRBM_V28 | SRBM_V30;
    }
#endif

    *busyCandidates = RBM_NONE;
    SingleTypeRegSet overallResult;
    unsigned int     registersNeeded = refPosition->regCount;

    if (floatFreeCandidates != RBM_NONE)
    {
        SingleTypeRegSet consecutiveResultForFree =
            filterConsecutiveCandidates(floatFreeCandidates, registersNeeded, &overallResult);

        if (consecutiveResultForFree != RBM_NONE)
        {
            // One last time, check if subsequent RefPositions (all RefPositions except the first for which
            // we assigned above) already have consecutive registers assigned. If yes, and if one of the
            // register out of the `consecutiveResult` is available for the first RefPosition, then just use
            // that. This will avoid unnecessary copies.

            regNumber firstRegNum = REG_NA;
            regNumber prevRegNum  = REG_NA;
            int       foundCount  = 0;

            RefPosition* consecutiveRefPosition = getNextConsecutiveRefPosition(refPosition);
            assert(consecutiveRefPosition != nullptr);

            for (unsigned int i = 1; i < registersNeeded; i++)
            {
                Interval* interval     = consecutiveRefPosition->getInterval();
                consecutiveRefPosition = getNextConsecutiveRefPosition(consecutiveRefPosition);

                if (!interval->isActive)
                {
                    foundCount = 0;
                    continue;
                }

                regNumber currRegNum = interval->assignedReg->regNum;
                if ((prevRegNum == REG_NA) || (prevRegNum == REG_PREV(currRegNum)) ||
                    ((prevRegNum == REG_FP_LAST) && (currRegNum == REG_FP_FIRST)))
                {
                    if (prevRegNum == REG_NA)
                    {
                        firstRegNum = currRegNum;
                    }
                    prevRegNum = currRegNum;
                    foundCount++;
                    continue;
                }

                foundCount = 0;
                break;
            }

            if (foundCount != 0)
            {
                assert(firstRegNum != REG_NA);
                SingleTypeRegSet remainingRegsMask = ((1ULL << (registersNeeded - foundCount)) - 1)
                                                     << (firstRegNum - 1);

                if ((overallResult & remainingRegsMask) != RBM_NONE)
                {
                    // If remaining registers are available, then just set the firstRegister mask
                    consecutiveResultForFree = 1ULL << (firstRegNum - 1);
                }
            }

            return consecutiveResultForFree;
        }
    }
    // There are registers available but they are not consecutive.
    // Here are some options to address them:
    //
    //  1.  Scan once again the available registers and find a set which has maximum register available.
    //      In other words, try to find register sequence that needs fewer registers to be spilled. This
    //      will give optimal CQ.
    //
    //  2.  Check if some of the RefPositions in the series are already in *somewhat* consecutive registers
    //      and if yes, assign that register sequence. That way, we will avoid copying values of
    //      RefPositions that are already positioned in the desired registers. Checking this is beneficial
    //      only if it can happen frequently. So for RefPositions <RP# 5, RP# 6, RP# 7, RP# 8>, it should
    //      be that, RP# 6 is already in V14 and RP# 8 is already in V16. But this can be rare (not tested).
    //      In future, if we see such cases being hit, we could use this heuristics.
    //
    //  3.  Give one of the free register to the first position and the algorithm will
    //      give the subsequent consecutive registers (free or busy) to the remaining RefPositions
    //      of the series. This may not give optimal CQ however.
    //
    //  4.  Return the set of available registers and let selection heuristics pick one of them to get
    //      assigned to the first RefPosition. Remaining RefPositions will be assigned to the subsequent
    //      registers (if busy, they will be spilled), similar to #3 above and will not give optimal CQ.
    //
    //
    // Among `consecutiveResultForBusy`, we could shortlist the registers that are beneficial from "busy register
    // selection" heuristics perspective. However, we would need to add logic of try_SPILL_COST(),
    // try_FAR_NEXT_REF(), etc. here which would complicate things. Instead, we just go with option# 1 and select
    // registers based on fewer number of registers that has to be spilled.
    //
    SingleTypeRegSet overallResultForBusy;
    SingleTypeRegSet consecutiveResultForBusy =
        filterConsecutiveCandidates(allCandidates, registersNeeded, &overallResultForBusy);

    *busyCandidates = consecutiveResultForBusy;

    // Check if we can further check better registers amoung consecutiveResultForBusy.
    if ((m_AvailableRegs.GetFloatRegSet() & overallResultForBusy) != RBM_NONE)
    {
        // `overallResultForBusy` contains the mask of entire series that can be the consecutive candidates.
        // If there is an overlap of that with free registers, then try to find a series that will need least
        // registers spilling as mentioned in #1 above.

        SingleTypeRegSet optimalConsecutiveResultForBusy =
            filterConsecutiveCandidatesForSpill(consecutiveResultForBusy, registersNeeded);

        if (optimalConsecutiveResultForBusy != RBM_NONE)
        {
            *busyCandidates = optimalConsecutiveResultForBusy;
        }
        else if ((m_AvailableRegs.GetFloatRegSet() & consecutiveResultForBusy) != RBM_NONE)
        {
            // We did not find free consecutive candidates, however we found some registers among the
            // `allCandidates` that are mix of free and busy. Since `busyCandidates` just has bit set for first
            // register of such series, return the mask that starts with free register, if possible. The busy
            // registers will be spilled during assignment of subsequent RefPosition.
            *busyCandidates = (m_AvailableRegs.GetFloatRegSet() & consecutiveResultForBusy);
        }
    }

    // Return RBM_NONE because there was no free candidates.
    return RBM_NONE;
}
//------------------------------------------------------------------------
// BuildNode: Build the RefPositions for a node
//
// Arguments:
//    treeNode - the node of interest
//
// Return Value:
//    The number of sources consumed by this node.
//
// Notes:
// Preconditions:
//    LSRA Has been initialized.
//
// Postconditions:
//    RefPositions have been built for all the register defs and uses required
//    for this node.
//
int LinearScan::BuildNode(GenTree* tree)
{
    assert(!tree->isContained());
    int       srcCount;
    int       dstCount;
    regMaskTP killMask      = RBM_NONE;
    bool      isLocalDefUse = false;

    // Reset the build-related members of LinearScan.
    clearBuildState();

    // Set the default dstCount. This may be modified below.
    if (tree->IsValue())
    {
        dstCount = 1;
        if (tree->IsUnusedValue())
        {
            isLocalDefUse = true;
        }
    }
    else
    {
        dstCount = 0;
    }

    switch (tree->OperGet())
    {
        default:
        {
            srcCount = BuildSimple(tree);
            break;
        }
        case GT_PHYSREG:
        {
            srcCount = 0;
            if (varTypeIsMask(tree))
            {
                assert(tree->AsPhysReg()->gtSrcReg == REG_FFR);
                BuildDef(tree, getSingleTypeRegMask(tree->AsPhysReg()->gtSrcReg, TYP_MASK));
            }
            else
            {
                BuildSimple(tree);
            }
            break;
        }
        case GT_LCL_VAR:
            // We make a final determination about whether a GT_LCL_VAR is a candidate or contained
            // after liveness. In either case we don't build any uses or defs. Otherwise, this is a
            // load of a stack-based local into a register and we'll fall through to the general
            // local case below.
            if (checkContainedOrCandidateLclVar(tree->AsLclVar()))
            {
                return 0;
            }
            FALLTHROUGH;

        case GT_LCL_FLD:
        {
            srcCount = 0;
#ifdef FEATURE_SIMD
            // Need an additional register to read upper 4 bytes of Vector3.
            if (tree->TypeGet() == TYP_SIMD12)
            {
                // We need an internal register different from targetReg in which 'tree' produces its result
                // because both targetReg and internal reg will be in use at the same time.
                buildInternalIntRegisterDefForNode(tree);
                setInternalRegsDelayFree = true;
                buildInternalRegisterUses();
            }
#endif
            BuildDef(tree);
        }
        break;

        case GT_STORE_LCL_VAR:
            if (tree->IsMultiRegLclVar() && isCandidateMultiRegLclVar(tree->AsLclVar()))
            {
                dstCount = compiler->lvaGetDesc(tree->AsLclVar())->lvFieldCnt;
            }
            FALLTHROUGH;

        case GT_STORE_LCL_FLD:
            srcCount = BuildStoreLoc(tree->AsLclVarCommon());
            break;

        case GT_FIELD_LIST:
            // These should always be contained. We don't correctly allocate or
            // generate code for a non-contained GT_FIELD_LIST.
            noway_assert(!"Non-contained GT_FIELD_LIST");
            srcCount = 0;
            break;

        case GT_NO_OP:
        case GT_START_NONGC:
            srcCount = 0;
            assert(dstCount == 0);
            break;

        case GT_PROF_HOOK:
            srcCount = 0;
            assert(dstCount == 0);
            killMask = getKillSetForProfilerHook();
            BuildKills(tree, killMask);
            break;

        case GT_START_PREEMPTGC:
            // This kills GC refs in callee save regs
            srcCount = 0;
            assert(dstCount == 0);
            BuildKills(tree, RBM_NONE);
            break;

        case GT_CNS_DBL:
        {
            GenTreeDblCon* dblConst   = tree->AsDblCon();
            double         constValue = dblConst->AsDblCon()->DconValue();

            if (emitter::emitIns_valid_imm_for_fmov(constValue))
            {
                // Directly encode constant to instructions.
            }
            else
            {
                // Reserve int to load constant from memory (IF_LARGELDC)
                buildInternalIntRegisterDefForNode(tree);
                buildInternalRegisterUses();
            }
        }
            FALLTHROUGH;

        case GT_CNS_INT:
        {
            srcCount = 0;
            assert(dstCount == 1);
            RefPosition* def               = BuildDef(tree);
            def->getInterval()->isConstant = true;
        }
        break;

#if defined(FEATURE_SIMD)
        case GT_CNS_VEC:
        {
            GenTreeVecCon* vecCon = tree->AsVecCon();

            if (vecCon->IsAllBitsSet() || vecCon->IsZero())
            {
                // Directly encode constant to instructions.
            }
            else
            {
                // Reserve int to load constant from memory (IF_LARGELDC)
                buildInternalIntRegisterDefForNode(tree);
                buildInternalRegisterUses();
            }

            srcCount = 0;
            assert(dstCount == 1);

            RefPosition* def               = BuildDef(tree);
            def->getInterval()->isConstant = true;
            break;
        }
#endif // FEATURE_SIMD

#if defined(FEATURE_MASKED_HW_INTRINSICS)
        case GT_CNS_MSK:
        {
            GenTreeMskCon* mskCon = tree->AsMskCon();

            if (mskCon->IsAllBitsSet() || mskCon->IsZero())
            {
                // Directly encode constant to instructions.
            }
            else
            {
                // Reserve int to load constant from memory (IF_LARGELDC)
                buildInternalIntRegisterDefForNode(tree);
                buildInternalRegisterUses();
            }

            srcCount = 0;
            assert(dstCount == 1);

            RefPosition* def               = BuildDef(tree);
            def->getInterval()->isConstant = true;
            break;
        }
#endif // FEATURE_MASKED_HW_INTRINSICS

        case GT_BOX:
        case GT_COMMA:
        case GT_QMARK:
        case GT_COLON:
            srcCount = 0;
            assert(dstCount == 0);
            unreached();
            break;

        case GT_RETURN:
            srcCount = BuildReturn(tree);
            killMask = getKillSetForReturn();
            BuildKills(tree, killMask);
            break;

#ifdef SWIFT_SUPPORT
        case GT_SWIFT_ERROR_RET:
            BuildUse(tree->gtGetOp1(), RBM_SWIFT_ERROR.GetIntRegSet());
            // Plus one for error register
            srcCount = BuildReturn(tree) + 1;
            killMask = getKillSetForReturn();
            BuildKills(tree, killMask);
            break;
#endif // SWIFT_SUPPORT

        case GT_RETFILT:
            assert(dstCount == 0);
            if (tree->TypeGet() == TYP_VOID)
            {
                srcCount = 0;
            }
            else
            {
                assert(tree->TypeGet() == TYP_INT);
                srcCount = 1;
                BuildUse(tree->gtGetOp1(), RBM_INTRET.GetIntRegSet());
            }
            break;

        case GT_NOP:
            srcCount = 0;
            assert(tree->TypeIs(TYP_VOID));
            assert(dstCount == 0);
            break;

        case GT_KEEPALIVE:
            assert(dstCount == 0);
            srcCount = BuildOperandUses(tree->gtGetOp1());
            break;

        case GT_JMP:
            srcCount = 0;
            assert(dstCount == 0);
            break;

        case GT_SWITCH:
            // This should never occur since switch nodes must not be visible at this
            // point in the JIT.
            srcCount = 0;
            noway_assert(!"Switch must be lowered at this point");
            break;

        case GT_JMPTABLE:
            srcCount = 0;
            assert(dstCount == 1);
            BuildDef(tree);
            break;

        case GT_SWITCH_TABLE:
            buildInternalIntRegisterDefForNode(tree);
            srcCount = BuildBinaryUses(tree->AsOp());
            assert(dstCount == 0);
            break;

        case GT_ADD:
        case GT_SUB:
            if (varTypeIsFloating(tree->TypeGet()))
            {
                // overflow operations aren't supported on float/double types.
                assert(!tree->gtOverflow());

                // No implicit conversions at this stage as the expectation is that
                // everything is made explicit by adding casts.
                assert(tree->gtGetOp1()->TypeGet() == tree->gtGetOp2()->TypeGet());
            }
            FALLTHROUGH;

        case GT_AND:
        case GT_AND_NOT:
        case GT_OR:
        case GT_OR_NOT:
        case GT_XOR:
        case GT_XOR_NOT:
        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROR:
            srcCount = BuildBinaryUses(tree->AsOp());
            assert(dstCount == 1);
            BuildDef(tree);
            break;

        case GT_BFIZ:
            assert(tree->gtGetOp1()->OperIs(GT_CAST));
            srcCount = BuildOperandUses(tree->gtGetOp1()->gtGetOp1());
            BuildDef(tree);
            break;

        case GT_RETURNTRAP:
            // this just turns into a compare of its child with an int
            // + a conditional call
            BuildUse(tree->gtGetOp1());
            srcCount = 1;
            assert(dstCount == 0);
            killMask = compiler->compHelperCallKillSet(CORINFO_HELP_STOP_FOR_GC);
            BuildKills(tree, killMask);
            break;

        case GT_MOD:
        case GT_UMOD:
            NYI_IF(varTypeIsFloating(tree->TypeGet()), "FP Remainder in ARM64");
            assert(!"Shouldn't see an integer typed GT_MOD node in ARM64");
            srcCount = 0;
            break;

        case GT_MUL:
            if (tree->gtOverflow())
            {
                // Need a register different from target reg to check for overflow.
                buildInternalIntRegisterDefForNode(tree);
                setInternalRegsDelayFree = true;
            }
            FALLTHROUGH;

        case GT_DIV:
        case GT_MULHI:
        case GT_MUL_LONG:
        case GT_UDIV:
        {
            srcCount = BuildBinaryUses(tree->AsOp());
            buildInternalRegisterUses();
            assert(dstCount == 1);
            BuildDef(tree);
        }
        break;

        case GT_INTRINSIC:
        {
            switch (tree->AsIntrinsic()->gtIntrinsicName)
            {
                case NI_System_Math_Max:
                case NI_System_Math_Min:
                case NI_System_Math_MaxNumber:
                case NI_System_Math_MinNumber:
                {
                    assert(varTypeIsFloating(tree->gtGetOp1()));
                    assert(varTypeIsFloating(tree->gtGetOp2()));
                    assert(tree->gtGetOp1()->TypeIs(tree->TypeGet()));

                    srcCount = BuildBinaryUses(tree->AsOp());
                    assert(dstCount == 1);
                    BuildDef(tree);
                    break;
                }

                case NI_System_Math_Abs:
                case NI_System_Math_Ceiling:
                case NI_System_Math_Floor:
                case NI_System_Math_Truncate:
                case NI_System_Math_Round:
                case NI_System_Math_Sqrt:
                {
                    assert(varTypeIsFloating(tree->gtGetOp1()));
                    assert(tree->gtGetOp1()->TypeIs(tree->TypeGet()));

                    BuildUse(tree->gtGetOp1());
                    srcCount = 1;
                    assert(dstCount == 1);
                    BuildDef(tree);
                    break;
                }

                default:
                    unreached();
            }
        }
        break;

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            srcCount = BuildHWIntrinsic(tree->AsHWIntrinsic(), &dstCount);
            break;
#endif // FEATURE_HW_INTRINSICS

        case GT_CAST:
            assert(dstCount == 1);
            srcCount = BuildCast(tree->AsCast());
            break;

        case GT_NEG:
        case GT_NOT:
            srcCount = BuildOperandUses(tree->gtGetOp1(), RBM_NONE);
            assert(dstCount == 1);
            BuildDef(tree);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_TEST_EQ:
        case GT_TEST_NE:
        case GT_CMP:
        case GT_TEST:
        case GT_CCMP:
        case GT_JCMP:
        case GT_JTEST:
            srcCount = BuildCmp(tree);
            break;

        case GT_JTRUE:
            BuildOperandUses(tree->gtGetOp1(), RBM_NONE);
            srcCount = 1;
            break;

        case GT_CKFINITE:
            srcCount = 1;
            assert(dstCount == 1);
            buildInternalIntRegisterDefForNode(tree);
            BuildUse(tree->gtGetOp1());
            BuildDef(tree);
            buildInternalRegisterUses();
            break;

        case GT_CMPXCHG:
        {
            GenTreeCmpXchg* cmpXchgNode = tree->AsCmpXchg();
            srcCount                    = cmpXchgNode->Comparand()->isContained() ? 2 : 3;
            assert(dstCount == 1);

            if (!compiler->compOpportunisticallyDependsOn(InstructionSet_Atomics))
            {
                // For ARMv8 exclusives requires a single internal register
                buildInternalIntRegisterDefForNode(tree);
            }

            // For ARMv8 exclusives the lifetime of the addr and data must be extended because
            // it may be used used multiple during retries

            // For ARMv8.1 atomic cas the lifetime of the addr and data must be extended to prevent
            // them being reused as the target register which must be destroyed early

            RefPosition* locationUse = BuildUse(tree->AsCmpXchg()->Addr());
            setDelayFree(locationUse);
            RefPosition* valueUse = BuildUse(tree->AsCmpXchg()->Data());
            setDelayFree(valueUse);
            if (!cmpXchgNode->Comparand()->isContained())
            {
                RefPosition* comparandUse = BuildUse(tree->AsCmpXchg()->Comparand());

                // For ARMv8 exclusives the lifetime of the comparand must be extended because
                // it may be used used multiple during retries
                if (!compiler->compOpportunisticallyDependsOn(InstructionSet_Atomics))
                {
                    setDelayFree(comparandUse);
                }
            }

            // Internals may not collide with target
            setInternalRegsDelayFree = true;
            buildInternalRegisterUses();
            BuildDef(tree);
        }
        break;

        case GT_LOCKADD:
        case GT_XORR:
        case GT_XAND:
        case GT_XADD:
        case GT_XCHG:
        {
            assert(dstCount == (tree->TypeIs(TYP_VOID) ? 0 : 1));
            srcCount = tree->gtGetOp2()->isContained() ? 1 : 2;

            if (!compiler->compOpportunisticallyDependsOn(InstructionSet_Atomics))
            {
                // GT_XCHG requires a single internal register; the others require two.
                buildInternalIntRegisterDefForNode(tree);
                if (tree->OperGet() != GT_XCHG)
                {
                    buildInternalIntRegisterDefForNode(tree);
                }
            }
            else if (tree->OperIs(GT_XAND))
            {
                // for ldclral we need an internal register.
                buildInternalIntRegisterDefForNode(tree);
            }

            assert(!tree->gtGetOp1()->isContained());
            RefPosition* op1Use = BuildUse(tree->gtGetOp1());
            RefPosition* op2Use = nullptr;
            if (!tree->gtGetOp2()->isContained())
            {
                op2Use = BuildUse(tree->gtGetOp2());
            }

            // For ARMv8 exclusives the lifetime of the addr and data must be extended because
            // it may be used used multiple during retries
            if (!compiler->compOpportunisticallyDependsOn(InstructionSet_Atomics))
            {
                // Internals may not collide with target
                if (dstCount == 1)
                {
                    setDelayFree(op1Use);
                    if (op2Use != nullptr)
                    {
                        setDelayFree(op2Use);
                    }
                    setInternalRegsDelayFree = true;
                }
                buildInternalRegisterUses();
                if (dstCount == 1)
                {
                    BuildDef(tree);
                }
            }
            else
            {
                // We always need the target reg for LSE, even if
                // return value is unused, see genLockedInstructions
                buildInternalRegisterUses();
                BuildDef(tree);
            }
        }
        break;

#if FEATURE_ARG_SPLIT
        case GT_PUTARG_SPLIT:
            srcCount = BuildPutArgSplit(tree->AsPutArgSplit());
            dstCount = tree->AsPutArgSplit()->gtNumRegs;
            break;
#endif // FEATURE_ARG_SPLIT

        case GT_PUTARG_STK:
            srcCount = BuildPutArgStk(tree->AsPutArgStk());
            break;

        case GT_PUTARG_REG:
            srcCount = BuildPutArgReg(tree->AsUnOp());
            break;

        case GT_CALL:
            srcCount = BuildCall(tree->AsCall());
            if (tree->AsCall()->HasMultiRegRetVal())
            {
                dstCount = tree->AsCall()->GetReturnTypeDesc()->GetReturnRegCount();
            }
            break;

        case GT_BLK:
            // These should all be eliminated prior to Lowering.
            assert(!"Non-store block node in Lowering");
            srcCount = 0;
            break;

        case GT_STORE_BLK:
            srcCount = BuildBlockStore(tree->AsBlk());
            break;

        case GT_INIT_VAL:
            // Always a passthrough of its child's value.
            assert(!"INIT_VAL should always be contained");
            srcCount = 0;
            break;

        case GT_LCLHEAP:
        {
            assert(dstCount == 1);

            // Need a variable number of temp regs (see genLclHeap() in codegenarm64.cpp):
            // Here '-' means don't care.
            //
            //  Size?                   Init Memory?    # temp regs
            //   0                          -               0
            //   const and <=UnrollLimit    -               0
            //   const and <PageSize        No              0
            //   >UnrollLimit               Yes             0
            //   Non-const                  Yes             0
            //   Non-const                  No              2
            //

            GenTree* size = tree->gtGetOp1();
            if (size->IsCnsIntOrI())
            {
                assert(size->isContained());
                srcCount = 0;

                size_t sizeVal = size->AsIntCon()->gtIconVal;

                if (sizeVal != 0)
                {
                    // Compute the amount of memory to properly STACK_ALIGN.
                    // Note: The GenTree node is not updated here as it is cheap to recompute stack aligned size.
                    // This should also help in debugging as we can examine the original size specified with
                    // localloc.
                    sizeVal = AlignUp(sizeVal, STACK_ALIGN);

                    if (sizeVal <= compiler->getUnrollThreshold(Compiler::UnrollKind::Memset))
                    {
                        // Need no internal registers
                    }
                    else if (!compiler->info.compInitMem)
                    {
                        // No need to initialize allocated stack space.
                        if (sizeVal < compiler->eeGetPageSize())
                        {
                            // Need no internal registers
                        }
                        else
                        {
                            // We need two registers: regCnt and RegTmp
                            buildInternalIntRegisterDefForNode(tree);
                            buildInternalIntRegisterDefForNode(tree);
                        }
                    }
                }
            }
            else
            {
                srcCount = 1;
                if (!compiler->info.compInitMem)
                {
                    buildInternalIntRegisterDefForNode(tree);
                    buildInternalIntRegisterDefForNode(tree);
                }
            }

            if (!size->isContained())
            {
                BuildUse(size);
            }
            buildInternalRegisterUses();
            BuildDef(tree);
        }
        break;

        case GT_BOUNDS_CHECK:
        {
            GenTreeBoundsChk* node = tree->AsBoundsChk();
            // Consumes arrLen & index - has no result
            assert(dstCount == 0);
            srcCount = BuildOperandUses(node->GetIndex());
            srcCount += BuildOperandUses(node->GetArrayLength());
        }
        break;

        case GT_ARR_ELEM:
            // These must have been lowered
            noway_assert(!"We should never see a GT_ARR_ELEM in lowering");
            srcCount = 0;
            assert(dstCount == 0);
            break;

        case GT_LEA:
        {
            GenTreeAddrMode* lea = tree->AsAddrMode();

            GenTree* base  = lea->Base();
            GenTree* index = lea->Index();
            int      cns   = lea->Offset();

            // This LEA is instantiating an address, so we set up the srcCount here.
            srcCount = 0;
            if (base != nullptr)
            {
                srcCount++;
                BuildUse(base);
            }
            if (index != nullptr)
            {
                srcCount++;
                if (index->OperIs(GT_BFIZ) && index->isContained())
                {
                    GenTreeCast* cast = index->gtGetOp1()->AsCast();
                    assert(cast->isContained() && (cns == 0));
                    BuildUse(cast->CastOp());
                }
                else if (index->OperIs(GT_CAST) && index->isContained())
                {
                    GenTreeCast* cast = index->AsCast();
                    assert(cast->isContained() && (cns == 0));
                    BuildUse(cast->CastOp());
                }
                else
                {
                    BuildUse(index);
                }
            }
            assert(dstCount == 1);

            // On ARM64 we may need a single internal register
            // (when both conditions are true then we still only need a single internal register)
            if ((index != nullptr) && (cns != 0))
            {
                // ARM64 does not support both Index and offset so we need an internal register
                buildInternalIntRegisterDefForNode(tree);
            }
            else if (!emitter::emitIns_valid_imm_for_add(cns, EA_8BYTE))
            {
                // This offset can't be contained in the add instruction, so we need an internal register
                buildInternalIntRegisterDefForNode(tree);
            }
            buildInternalRegisterUses();
            BuildDef(tree);
        }
        break;

        case GT_STOREIND:
        {
            assert(dstCount == 0);

            if (compiler->codeGen->gcInfo.gcIsWriteBarrierStoreIndNode(tree->AsStoreInd()))
            {
                srcCount = BuildGCWriteBarrier(tree);
                break;
            }

            srcCount = BuildIndir(tree->AsIndir());
            if (!tree->gtGetOp2()->isContained())
            {
                BuildUse(tree->gtGetOp2());
                srcCount++;
            }
        }
        break;

        case GT_NULLCHECK:
        case GT_IND:
            assert(dstCount == (tree->OperIs(GT_NULLCHECK) ? 0 : 1));
            srcCount = BuildIndir(tree->AsIndir());
            break;

        case GT_CATCH_ARG:
            srcCount = 0;
            assert(dstCount == 1);
            BuildDef(tree, RBM_EXCEPTION_OBJECT.GetIntRegSet());
            break;

        case GT_INDEX_ADDR:
            assert(dstCount == 1);
            srcCount = BuildBinaryUses(tree->AsOp());
            buildInternalIntRegisterDefForNode(tree);
            if (!tree->AsIndexAddr()->Index()->TypeIs(TYP_I_IMPL) &&
                !(isPow2(tree->AsIndexAddr()->gtElemSize) && (tree->AsIndexAddr()->gtElemSize <= 32768)))
            {
                // We're going to need a temp reg to widen the index.
                buildInternalIntRegisterDefForNode(tree);
            }
            buildInternalRegisterUses();
            BuildDef(tree);
            break;

        case GT_SELECT:
            assert(dstCount == 1);
            srcCount = BuildSelect(tree->AsConditional());
            break;
        case GT_SELECTCC:
            assert(dstCount == 1);
            srcCount = BuildSelect(tree->AsOp());
            break;

#ifdef SWIFT_SUPPORT
        case GT_SWIFT_ERROR:
            srcCount = 0;
            assert(dstCount == 1);

            // Any register should do here, but the error register value should immediately
            // be moved from GT_SWIFT_ERROR's destination register to the SwiftError struct,
            // and we know REG_SWIFT_ERROR should be busy up to this point, anyway.
            // By forcing LSRA to use REG_SWIFT_ERROR as both the source and destination register,
            // we can ensure the redundant move is elided.
            BuildDef(tree, RBM_SWIFT_ERROR.GetIntRegSet());
            break;
#endif // SWIFT_SUPPORT

    } // end switch (tree->OperGet())

    if (tree->IsUnusedValue() && (dstCount != 0))
    {
        isLocalDefUse = true;
    }
    // We need to be sure that we've set srcCount and dstCount appropriately
    assert((dstCount < 2) || tree->IsMultiRegNode());
    assert(isLocalDefUse == (tree->IsValue() && tree->IsUnusedValue()));
    assert(!tree->IsValue() || (dstCount != 0));
    assert(dstCount == tree->GetRegisterDstCount(compiler));
    return srcCount;
}

#ifdef FEATURE_HW_INTRINSICS

#include "hwintrinsic.h"

//------------------------------------------------------------------------
// BuildHWIntrinsic: Set the NodeInfo for a GT_HWINTRINSIC tree.
//
// Arguments:
//    tree       - The GT_HWINTRINSIC node of interest
//    pDstCount  - OUT parameter - the number of registers defined for the given node
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildHWIntrinsic(GenTreeHWIntrinsic* intrinsicTree, int* pDstCount)
{
    assert(pDstCount != nullptr);

    const HWIntrinsic intrin(intrinsicTree);

    int srcCount = 0;

    // Determine whether this is an operation where an op must be marked delayFree so that it
    // is not allocated the same register as the target.
    GenTree* delayFreeOp = getDelayFreeOperand(intrinsicTree);

    // Determine whether this is an operation where one of the ops is an address
    GenTree* addrOp = getVectorAddrOperand(intrinsicTree);

    // Determine whether this is an operation where one of the ops has consecutive registers
    bool     destIsConsecutive = false;
    GenTree* consecutiveOp     = getConsecutiveRegistersOperand(intrinsicTree, &destIsConsecutive);

    // Determine whether this is an operation where one of the ops is an embedded mask
    GenTreeHWIntrinsic* embeddedOp = getEmbeddedMaskOperand(intrin);

    // Determine whether this is an operation where one of the ops is a contained conditional select
    GenTreeHWIntrinsic* containedCselOp = getContainedCselOperand(intrinsicTree);

    // If there is an embedded op, then determine if that has an op that must be marked delayFree
    if (embeddedOp != nullptr)
    {
        assert(delayFreeOp == nullptr);
        delayFreeOp = getDelayFreeOperand(embeddedOp, /* embedded */ true);
    }

    // Build any immediates
    BuildHWIntrinsicImmediate(intrinsicTree, intrin);

    // Build all Operands
    for (size_t opNum = 1; opNum <= intrin.numOperands; opNum++)
    {
        GenTree* operand = intrinsicTree->Op(opNum);

        assert(operand != nullptr);

        SingleTypeRegSet candidates = getOperandCandidates(intrinsicTree, intrin, opNum);

        if (embeddedOp == operand)
        {
            assert(addrOp != operand);
            assert(consecutiveOp != operand);
            assert(delayFreeOp != operand);

            srcCount += BuildEmbeddedOperandUses(embeddedOp, delayFreeOp);
        }
        else if (addrOp == operand)
        {
            assert(consecutiveOp != operand);
            assert(delayFreeOp != operand);

            srcCount += BuildAddrUses(operand, candidates);
        }
        else if (consecutiveOp == operand)
        {
            assert(candidates == RBM_NONE);

            // Some operands have consective op which is also a delay free op
            srcCount += BuildConsecutiveRegistersForUse(operand, delayFreeOp);
        }
        else if (delayFreeOp == operand)
        {
            if (delayFreeOp->isContained())
            {
                srcCount += BuildOperandUses(operand);
            }
            else
            {
                RefPosition* delayUse = BuildUse(operand, candidates);
                srcCount += 1;

                if (opNum == 1)
                {
                    assert(tgtPrefUse == nullptr);
                    assert(tgtPrefUse2 == nullptr);
                    tgtPrefUse = delayUse;
                }
                else
                {
                    assert(opNum == 2);
                    assert(tgtPrefUse == nullptr);
                    assert(tgtPrefUse2 == nullptr);
                    tgtPrefUse2 = delayUse;
                }
            }
        }
        else if (containedCselOp == operand)
        {
            srcCount += BuildContainedCselUses(containedCselOp, delayFreeOp, candidates);
        }
        // Only build as delay free use if register types match
        else if ((delayFreeOp != nullptr) &&
                 (varTypeUsesSameRegType(delayFreeOp->TypeGet(), operand->TypeGet()) ||
                  (delayFreeOp->IsMultiRegNode() && varTypeUsesFloatReg(operand->TypeGet()))))
        {
            srcCount += BuildDelayFreeUses(operand, delayFreeOp, candidates);
        }
        else
        {
            srcCount += BuildOperandUses(operand, candidates);
        }
    }

    buildInternalRegisterUses();

    // Build Destination

    int dstCount = 0;

    if (HWIntrinsicInfo::IsMultiReg(intrin.id))
    {
        dstCount = intrinsicTree->GetMultiRegCount(compiler);
    }
    else if (intrinsicTree->IsValue())
    {
        dstCount = 1;
    }

    if (destIsConsecutive)
    {
        BuildConsecutiveRegistersForDef(intrinsicTree, dstCount);
    }
    else if ((dstCount == 1) || (dstCount == 2))
    {
        BuildDef(intrinsicTree);

        if (dstCount == 2)
        {
            BuildDef(intrinsicTree, RBM_NONE, 1);
        }
    }
    else
    {
        assert(dstCount == 0);
    }

    *pDstCount = dstCount;
    return srcCount;
}

//------------------------------------------------------------------------
// BuildHWIntrinsicImmediate: Build immediate operands of intrinsics, if any.
//
// Arguments:
//    intrinsicTree - Intrinsic tree node for which need to build RefPositions for
//    intrin        - Underlying intrinsic
//
void LinearScan::BuildHWIntrinsicImmediate(GenTreeHWIntrinsic* intrinsicTree, const HWIntrinsic intrin)
{
    if (!HWIntrinsicInfo::HasImmediateOperand(intrin.id) || HWIntrinsicInfo::NoJmpTableImm(intrin.id))
    {
        return;
    }

    // We may need to allocate an additional general-purpose register when an intrinsic has a non-const immediate
    // operand and the intrinsic does not have an alternative non-const fallback form.
    // However, for a case when the operand can take only two possible values - zero and one
    // the codegen can use cbnz to do conditional branch, so such register is not needed.

    bool needBranchTargetReg = false;

    int immLowerBound = 0;
    int immUpperBound = 0;

    if (intrin.category == HW_Category_SIMDByIndexedElement)
    {
        var_types indexedElementOpType;

        if (intrin.numOperands == 2)
        {
            indexedElementOpType = intrin.op1->TypeGet();
        }
        else if (intrin.numOperands == 3)
        {
            indexedElementOpType = intrin.op2->TypeGet();
        }
        else
        {
            assert(intrin.numOperands == 4);
            indexedElementOpType = intrin.op3->TypeGet();
        }

        assert(varTypeIsSIMD(indexedElementOpType));

        const unsigned int indexedElementSimdSize = genTypeSize(indexedElementOpType);
        HWIntrinsicInfo::lookupImmBounds(intrin.id, indexedElementSimdSize, intrin.baseType, 1, &immLowerBound,
                                         &immUpperBound);
    }
    else
    {
        HWIntrinsicInfo::lookupImmBounds(intrin.id, intrinsicTree->GetSimdSize(), intrin.baseType, 1, &immLowerBound,
                                         &immUpperBound);
    }

    if ((immLowerBound != 0) || (immUpperBound != 1))
    {
        if ((intrin.category == HW_Category_SIMDByIndexedElement) ||
            (intrin.category == HW_Category_ShiftLeftByImmediate) ||
            (intrin.category == HW_Category_ShiftRightByImmediate))
        {
            switch (intrin.numOperands)
            {
                case 4:
                    needBranchTargetReg = !intrin.op4->isContainedIntOrIImmed();
                    break;

                case 3:
                    needBranchTargetReg = !intrin.op3->isContainedIntOrIImmed();
                    break;

                case 2:
                    needBranchTargetReg = !intrin.op2->isContainedIntOrIImmed();
                    break;

                default:
                    unreached();
            }
        }
        else
        {
            switch (intrin.id)
            {
                case NI_AdvSimd_DuplicateSelectedScalarToVector64:
                case NI_AdvSimd_DuplicateSelectedScalarToVector128:
                case NI_AdvSimd_Extract:
                case NI_AdvSimd_Insert:
                case NI_AdvSimd_InsertScalar:
                case NI_AdvSimd_LoadAndInsertScalar:
                case NI_AdvSimd_LoadAndInsertScalarVector64x2:
                case NI_AdvSimd_LoadAndInsertScalarVector64x3:
                case NI_AdvSimd_LoadAndInsertScalarVector64x4:
                case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x2:
                case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x3:
                case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x4:
                case NI_AdvSimd_Arm64_DuplicateSelectedScalarToVector128:
                    needBranchTargetReg = !intrin.op2->isContainedIntOrIImmed();
                    break;

                case NI_AdvSimd_ExtractVector64:
                case NI_AdvSimd_ExtractVector128:
                case NI_AdvSimd_StoreSelectedScalar:
                case NI_AdvSimd_Arm64_StoreSelectedScalar:
                case NI_Sve_PrefetchBytes:
                case NI_Sve_PrefetchInt16:
                case NI_Sve_PrefetchInt32:
                case NI_Sve_PrefetchInt64:
                case NI_Sve_ExtractVector:
                case NI_Sve_TrigonometricMultiplyAddCoefficient:
                    needBranchTargetReg = !intrin.op3->isContainedIntOrIImmed();
                    break;

                case NI_AdvSimd_Arm64_InsertSelectedScalar:
                    assert(intrin.op2->isContainedIntOrIImmed());
                    assert(intrin.op4->isContainedIntOrIImmed());
                    break;

                case NI_Sve_CreateTrueMaskByte:
                case NI_Sve_CreateTrueMaskDouble:
                case NI_Sve_CreateTrueMaskInt16:
                case NI_Sve_CreateTrueMaskInt32:
                case NI_Sve_CreateTrueMaskInt64:
                case NI_Sve_CreateTrueMaskSByte:
                case NI_Sve_CreateTrueMaskSingle:
                case NI_Sve_CreateTrueMaskUInt16:
                case NI_Sve_CreateTrueMaskUInt32:
                case NI_Sve_CreateTrueMaskUInt64:
                case NI_Sve_Count16BitElements:
                case NI_Sve_Count32BitElements:
                case NI_Sve_Count64BitElements:
                case NI_Sve_Count8BitElements:
                    needBranchTargetReg = !intrin.op1->isContainedIntOrIImmed();
                    break;

                case NI_Sve_GatherPrefetch8Bit:
                case NI_Sve_GatherPrefetch16Bit:
                case NI_Sve_GatherPrefetch32Bit:
                case NI_Sve_GatherPrefetch64Bit:
                    if (!varTypeIsSIMD(intrin.op2->gtType))
                    {
                        needBranchTargetReg = !intrin.op4->isContainedIntOrIImmed();
                    }
                    else
                    {
                        needBranchTargetReg = !intrin.op3->isContainedIntOrIImmed();
                    }
                    break;

                case NI_Sve_SaturatingDecrementBy16BitElementCount:
                case NI_Sve_SaturatingDecrementBy32BitElementCount:
                case NI_Sve_SaturatingDecrementBy64BitElementCount:
                case NI_Sve_SaturatingDecrementBy8BitElementCount:
                case NI_Sve_SaturatingIncrementBy16BitElementCount:
                case NI_Sve_SaturatingIncrementBy32BitElementCount:
                case NI_Sve_SaturatingIncrementBy64BitElementCount:
                case NI_Sve_SaturatingIncrementBy8BitElementCount:
                case NI_Sve_SaturatingDecrementBy16BitElementCountScalar:
                case NI_Sve_SaturatingDecrementBy32BitElementCountScalar:
                case NI_Sve_SaturatingDecrementBy64BitElementCountScalar:
                case NI_Sve_SaturatingIncrementBy16BitElementCountScalar:
                case NI_Sve_SaturatingIncrementBy32BitElementCountScalar:
                case NI_Sve_SaturatingIncrementBy64BitElementCountScalar:
                    // Can only avoid generating a table if both immediates are constant.
                    assert(intrin.op2->isContainedIntOrIImmed() == intrin.op3->isContainedIntOrIImmed());
                    needBranchTargetReg = !intrin.op2->isContainedIntOrIImmed();
                    // Ensure that internal does not collide with destination.
                    setInternalRegsDelayFree = true;
                    break;

                case NI_Sve_MultiplyAddRotateComplexBySelectedScalar:
                    // This API has two immediates, one of which is used to index pairs of floats in a vector.
                    // For a vector width of 128 bits, this means the index's range is [0, 1],
                    // which means we will skip the above jump table register check,
                    // even though we might need a jump table for the second immediate.
                    // Thus, this API is special-cased, and does not use the HW_Category_SIMDByIndexedElement path.
                    // Also, only one internal register is needed for the jump table;
                    // we will combine the two immediates into one jump table.

                    // Can only avoid generating a table if both immediates are constant.
                    assert(intrin.op4->isContainedIntOrIImmed() == intrin.op5->isContainedIntOrIImmed());
                    needBranchTargetReg = !intrin.op4->isContainedIntOrIImmed();
                    // Ensure that internal does not collide with destination.
                    setInternalRegsDelayFree = true;
                    break;

                case NI_Sve_ShiftRightArithmeticForDivide:
                    needBranchTargetReg = !intrin.op2->isContainedIntOrIImmed();
                    break;

                case NI_Sve_AddRotateComplex:
                    needBranchTargetReg = !intrin.op3->isContainedIntOrIImmed();
                    break;

                case NI_Sve_MultiplyAddRotateComplex:
                    needBranchTargetReg = !intrin.op4->isContainedIntOrIImmed();
                    break;

                default:
                    unreached();
            }
        }
    }

    if (needBranchTargetReg)
    {
        buildInternalIntRegisterDefForNode(intrinsicTree);
    }
}

//------------------------------------------------------------------------
// BuildEmbeddedOperandUses: Build the uses for an Embedded Mask operand
//
// These need special handling as the ConditionalSelect and embedded op will be generated as a single instruction,
// and possibly prefixed with a MOVPRFX
//
//
// Arguments:
//    embeddedOpNode      - The embedded node of interest
//    embeddedDelayFreeOp - The delay free operand corresponding to the embedded node
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildEmbeddedOperandUses(GenTreeHWIntrinsic* embeddedOpNode, GenTree* embeddedDelayFreeOp)
{
    assert(embeddedOpNode->IsEmbMaskOp());
    assert(!embeddedOpNode->OperIsHWIntrinsic(NI_Sve_ConvertVectorToMask));

    int               srcCount = 0;
    const HWIntrinsic intrinEmbedded(embeddedOpNode);

    // Build any immediates
    BuildHWIntrinsicImmediate(embeddedOpNode, intrinEmbedded);

    if (embeddedDelayFreeOp == nullptr)
    {
        srcCount += BuildOperandUses(embeddedOpNode);
        return srcCount;
    }

    // If the embedded operation has RMW semantics then record delay-free for the "merge" value

    if (HWIntrinsicInfo::IsFmaIntrinsic(intrinEmbedded.id))
    {
        assert(intrinEmbedded.numOperands == 3);

        // For FMA intrinsics, the arguments may get switched around during codegen.
        // Figure out where the delay free op will be.

        LIR::Use use;
        GenTree* user = nullptr;

        if (LIR::AsRange(blockSequence[curBBSeqNum]).TryGetUse(embeddedOpNode, &use))
        {
            user = use.User();
        }
        unsigned resultOpNum = embeddedOpNode->GetResultOpNumForRmwIntrinsic(user, intrinEmbedded.op1,
                                                                             intrinEmbedded.op2, intrinEmbedded.op3);

        if (resultOpNum != 0)
        {
            embeddedDelayFreeOp = embeddedOpNode->Op(resultOpNum);
        }
    }

    for (size_t argNum = 1; argNum <= intrinEmbedded.numOperands; argNum++)
    {
        GenTree* node = embeddedOpNode->Op(argNum);

        if (node == embeddedDelayFreeOp)
        {
            tgtPrefUse = BuildUse(node);
            srcCount++;
        }
        else
        {
            // TODO-CQ : The requirements here may not hold depending on the usage of movprfx in codegen,
            // and if so then the uses of delay free can be reduced.

            RefPosition* useRefPosition = nullptr;

            int uses = BuildDelayFreeUses(node, nullptr, RBM_NONE, &useRefPosition);
            srcCount += uses;

            // It is a hard requirement that these are not allocated to the same register as the destination,
            // so verify no optimizations kicked in to skip setting the delay-free.
            assert((useRefPosition != nullptr && useRefPosition->delayRegFree) || (uses == 0));
        }
    }

    return srcCount;
}

//------------------------------------------------------------------------
// BuildContainedCselUses: Build the uses for a contained conditional select operand
//
// Arguments:
//    containedCselOpNode - The contained conditional select node of interest
//    delayFreeOp         - The delay free operand corresponding to the conditional select node
//    candidates          - The set of candidates for the uses
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildContainedCselUses(GenTreeHWIntrinsic* containedCselOpNode,
                                       GenTree*            delayFreeOp,
                                       SingleTypeRegSet    candidates)
{
    int srcCount = 0;

    assert(containedCselOpNode->isContained());

    for (size_t opNum = 1; opNum <= containedCselOpNode->GetOperandCount(); opNum++)
    {
        GenTree* currentOp = containedCselOpNode->Op(opNum);
        if (currentOp->TypeGet() == TYP_MASK)
        {
            srcCount += BuildOperandUses(currentOp, candidates);
        }
        else
        {
            srcCount += BuildDelayFreeUses(currentOp, delayFreeOp, candidates);
        }
    }

    return srcCount;
}

//------------------------------------------------------------------------
//  BuildConsecutiveRegistersForUse: Build ref position(s) for `treeNode` that has a
//  requirement of allocating consecutive registers. It will create the RefTypeUse
//  RefPositions for as many consecutive registers are needed for `treeNode` and in
//  between, it might contain RefTypeUpperVectorRestore RefPositions.
//
//  For the first RefPosition of the series, it sets the `regCount` field equal to
//  the number of subsequent RefPositions (including the first one) involved for this
//  treeNode. For the subsequent RefPositions, it sets the `regCount` to 0. For all
//  the RefPositions created, it sets the `needsConsecutive` flag so it can be used to
//  identify these RefPositions during allocation.
//
//  It also populates a `RefPositionMap` to access the subsequent RefPositions from
//  a given RefPosition. This was preferred rather than adding a field in RefPosition
//  for this purpose.
//
// Arguments:
//    treeNode       - The GT_HWINTRINSIC node of interest
//    rmwNode        - Read-modify-write node.
//
// Return Value:
//    The number of sources consumed by this node.
//
int LinearScan::BuildConsecutiveRegistersForUse(GenTree* treeNode, GenTree* rmwNode)
{
    int       srcCount     = 0;
    Interval* rmwInterval  = nullptr;
    bool      rmwIsLastUse = false;
    if (rmwNode != nullptr)
    {
        if (isCandidateLocalRef(rmwNode))
        {
            rmwInterval  = getIntervalForLocalVarNode(rmwNode->AsLclVar());
            rmwIsLastUse = rmwNode->AsLclVar()->IsLastUse(0);
        }
    }
    if (treeNode->OperIsFieldList())
    {
        assert(compiler->info.compNeedsConsecutiveRegisters);

        unsigned     regCount    = 0;
        RefPosition* firstRefPos = nullptr;
        RefPosition* currRefPos  = nullptr;
        RefPosition* lastRefPos  = nullptr;

        NextConsecutiveRefPositionsMap* refPositionMap = getNextConsecutiveRefPositionsMap();
        for (GenTreeFieldList::Use& use : treeNode->AsFieldList()->Uses())
        {
            RefPosition*        restoreRefPos = nullptr;
            RefPositionIterator prevRefPos    = refPositions.backPosition();

            currRefPos = BuildUse(use.GetNode(), RBM_NONE, 0);

            // Check if restore RefPositions were created
            RefPositionIterator tailRefPos = refPositions.backPosition();
            assert(tailRefPos == currRefPos);
            prevRefPos++;
            if (prevRefPos != tailRefPos)
            {
                restoreRefPos = prevRefPos;
                assert(restoreRefPos->refType == RefTypeUpperVectorRestore);
            }

            currRefPos->needsConsecutive = true;
            currRefPos->regCount         = 0;
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            if (restoreRefPos != nullptr)
            {
                // If there was a restoreRefPosition created, make sure to link it
                // as well so during register assignment, we could visit it and
                // make sure that it doesn't get assigned one of register that is part
                // of consecutive registers we are allocating for this treeNode.
                // See assignConsecutiveRegisters().
                restoreRefPos->needsConsecutive = true;
                restoreRefPos->regCount         = 0;
                if (firstRefPos == nullptr)
                {
                    // Always set the non UpperVectorRestore as the firstRefPos.
                    // UpperVectorRestore can be assigned to a different independent
                    // register.
                    // See TODO-CQ in assignConsecutiveRegisters().
                    firstRefPos = currRefPos;
                }
                refPositionMap->Set(lastRefPos, restoreRefPos, LinearScan::NextConsecutiveRefPositionsMap::Overwrite);
                refPositionMap->Set(restoreRefPos, currRefPos, LinearScan::NextConsecutiveRefPositionsMap::Overwrite);

                if (rmwNode != nullptr)
                {
                    // If we have rmwNode, determine if the restoreRefPos should be set to delay-free.
                    if ((restoreRefPos->getInterval() != rmwInterval) || (!rmwIsLastUse && !restoreRefPos->lastUse))
                    {
                        setDelayFree(restoreRefPos);
                    }
                }
            }
            else
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            {
                if (firstRefPos == nullptr)
                {
                    firstRefPos = currRefPos;
                }
                refPositionMap->Set(lastRefPos, currRefPos, LinearScan::NextConsecutiveRefPositionsMap::Overwrite);
            }

            refPositionMap->Set(currRefPos, nullptr);

            lastRefPos = currRefPos;
            regCount++;
            if (rmwNode != nullptr)
            {
                // If we have rmwNode, determine if the currRefPos should be set to delay-free.
                if ((currRefPos->getInterval() != rmwInterval) || (!rmwIsLastUse && !currRefPos->lastUse))
                {
                    setDelayFree(currRefPos);
                }
            }
        }

        // Set `regCount` to actual consecutive registers count for first ref-position.
        // For others, set 0 so we can identify that this is non-first RefPosition.
        firstRefPos->regCount = regCount;

#ifdef DEBUG
        // Set the minimum register candidates needed for stress to work.
        currRefPos = firstRefPos;
        while (currRefPos != nullptr)
        {
            currRefPos->minRegCandidateCount = regCount;
            currRefPos                       = getNextConsecutiveRefPosition(currRefPos);
        }
#endif
        srcCount += regCount;
    }
    else
    {
        RefPositionIterator refPositionMark   = refPositions.backPosition();
        int                 refPositionsAdded = BuildOperandUses(treeNode);

        if (rmwNode != nullptr)
        {
            // Check all the newly created RefPositions for delay free
            RefPositionIterator iter = refPositionMark;

            for (iter++; iter != refPositions.end(); iter++)
            {
                RefPosition* refPositionAdded = &(*iter);

                // If we have rmwNode, determine if the refPositionAdded should be set to delay-free.
                if ((refPositionAdded->getInterval() != rmwInterval) || (!rmwIsLastUse && !refPositionAdded->lastUse))
                {
                    setDelayFree(refPositionAdded);
                }
            }
        }

        srcCount += refPositionsAdded;
    }

    return srcCount;
}

//------------------------------------------------------------------------
//  BuildConsecutiveRegistersForDef: Build RefTypeDef ref position(s) for
//  `treeNode` that produces `registerCount` consecutive registers.
//
//  For the first RefPosition of the series, it sets the `regCount` field equal to
//  the total number of RefPositions (including the first one) involved for this
//  treeNode. For the subsequent RefPositions, it sets the `regCount` to 0. For all
//  the RefPositions created, it sets the `needsConsecutive` flag so it can be used to
//  identify these RefPositions during allocation.
//
//  It also populates a `RefPositionMap` to access the subsequent RefPositions from
//  a given RefPosition. This was preferred rather than adding a field in RefPosition
//  for this purpose.
//
// Arguments:
//    treeNode       - The GT_HWINTRINSIC node of interest
//    registerCount  - Number of registers the treeNode produces
//
void LinearScan::BuildConsecutiveRegistersForDef(GenTree* treeNode, int registerCount)
{
    assert(registerCount > 1);
    assert(compiler->info.compNeedsConsecutiveRegisters);

    RefPosition* currRefPos = nullptr;
    RefPosition* lastRefPos = nullptr;

    NextConsecutiveRefPositionsMap* refPositionMap = getNextConsecutiveRefPositionsMap();
    for (int fieldIdx = 0; fieldIdx < registerCount; fieldIdx++)
    {
        currRefPos                   = BuildDef(treeNode, RBM_NONE, fieldIdx);
        currRefPos->needsConsecutive = true;
        currRefPos->regCount         = 0;
#ifdef DEBUG
        // Set the minimum register candidates needed for stress to work.
        currRefPos->minRegCandidateCount = registerCount;
#endif
        if (fieldIdx == 0)
        {
            // Set `regCount` to actual consecutive registers count for first ref-position.
            // For others, set 0 so we can identify that this is non-first RefPosition.

            currRefPos->regCount = registerCount;
        }

        refPositionMap->Set(lastRefPos, currRefPos, LinearScan::NextConsecutiveRefPositionsMap::Overwrite);
        refPositionMap->Set(currRefPos, nullptr);

        lastRefPos = currRefPos;
    }
}

#ifdef DEBUG
//------------------------------------------------------------------------
// isLiveAtConsecutiveRegistersLoc: Check if the refPosition is live at the location
//    where consecutive registers are needed. This is used during JitStressRegs to
//    not constrain the register requirements for such refpositions, because a lot
//    of registers will be busy. For RefTypeUse, it will just see if the nodeLocation
//    matches with the tracking `consecutiveRegistersLocation`. For Def, it will check
//    the underlying `GenTree*` to see if the tree that produced it had consecutive
//    registers requirement.
//
//
// Arguments:
//    consecutiveRegistersLocation - The most recent location where consecutive
//     registers were needed.
//
// Returns: If the refposition is live at same location which has the requirement of
//    consecutive registers.
//
bool RefPosition::isLiveAtConsecutiveRegistersLoc(LsraLocation consecutiveRegistersLocation)
{
    if (needsConsecutive)
    {
        return true;
    }

    bool atConsecutiveRegsLoc          = consecutiveRegistersLocation == nodeLocation;
    bool treeNeedsConsecutiveRegisters = false;

    if ((treeNode != nullptr) && treeNode->OperIsHWIntrinsic())
    {
        const HWIntrinsic intrin(treeNode->AsHWIntrinsic());
        treeNeedsConsecutiveRegisters = HWIntrinsicInfo::NeedsConsecutiveRegisters(intrin.id);
    }

    if (refType == RefTypeDef)
    {
        return treeNeedsConsecutiveRegisters;
    }
    else if (refType == RefTypeUse)
    {
        if (isIntervalRef() && getInterval()->isInternal)
        {
            return treeNeedsConsecutiveRegisters;
        }
        return atConsecutiveRegsLoc;
    }
    else if (refType == RefTypeUpperVectorRestore)
    {
        return atConsecutiveRegsLoc;
    }
    return false;
}
#endif // DEBUG

//------------------------------------------------------------------------
// getOperandCandidates: Get the register candidates for a given operand number
//
// Arguments:
//    intrinsicTree - Tree to check
//    intrin        - The tree as a HWIntrinsic
//    opNum         - The Operand number in the intrinsic tree
//
// Return Value:
//    The candidates for the operand number
//
SingleTypeRegSet LinearScan::getOperandCandidates(GenTreeHWIntrinsic* intrinsicTree, HWIntrinsic intrin, size_t opNum)
{
    SingleTypeRegSet opCandidates = RBM_NONE;

    assert(opNum <= intrinsicTree->GetOperandCount());

    if (HWIntrinsicInfo::IsLowVectorOperation(intrin.id))
    {
        assert(!varTypeIsMask(intrinsicTree->Op(opNum)->TypeGet()));

        bool isLowVectorOpNum = false;

        switch (intrin.id)
        {
            case NI_Sve_DotProductBySelectedScalar:
            case NI_Sve_FusedMultiplyAddBySelectedScalar:
            case NI_Sve_FusedMultiplySubtractBySelectedScalar:
            case NI_Sve_MultiplyAddRotateComplexBySelectedScalar:
                isLowVectorOpNum = (opNum == 3);
                break;
            case NI_Sve_MultiplyBySelectedScalar:
                isLowVectorOpNum = (opNum == 2);
                break;
            default:
                unreached();
        }

        if (isLowVectorOpNum)
        {
            unsigned baseElementSize = genTypeSize(intrin.baseType);

            if (baseElementSize == 8)
            {
                opCandidates = RBM_SVE_INDEXED_D_ELEMENT_ALLOWED_REGS.GetFloatRegSet();
            }
            else
            {
                assert(baseElementSize == 4);
                opCandidates = RBM_SVE_INDEXED_S_ELEMENT_ALLOWED_REGS.GetFloatRegSet();
            }
        }
    }
    else if ((intrin.category == HW_Category_SIMDByIndexedElement) && (genTypeSize(intrin.baseType) == 2))
    {
        // Some "Advanced SIMD scalar x indexed element" and "Advanced SIMD vector x indexed element" instructions (e.g.
        // "MLA (by element)") have encoding that restricts what registers that can be used for the indexed element when
        // the element size is H (i.e. 2 bytes).

        if ((intrin.numOperands == 4) || (intrin.numOperands == 3 && !HWIntrinsicInfo::HasImmediateOperand(intrin.id)))
        {
            opCandidates = (opNum == 3) ? RBM_ASIMD_INDEXED_H_ELEMENT_ALLOWED_REGS.GetFloatRegSet() : opCandidates;
        }
        else if (intrin.id == NI_Sve_DuplicateSelectedScalarToVector)
        {
            // Do nothing
        }
        else
        {
            opCandidates = (opNum == 2) ? RBM_ASIMD_INDEXED_H_ELEMENT_ALLOWED_REGS.GetFloatRegSet() : opCandidates;
        }
    }
    else if (varTypeIsMask(intrinsicTree->Op(opNum)->TypeGet()))
    {
        opCandidates = RBM_ALLMASK.GetPredicateRegSet();

        // Any low mask operands are always in op1
        if (opNum == 1 && HWIntrinsicInfo::IsLowMaskedOperation(intrin.id))
        {
            opCandidates = RBM_LOWMASK.GetPredicateRegSet();
        }
    }

    return opCandidates;
}

//------------------------------------------------------------------------
// getDelayFreeOperand: Get the delay free characteristics of the HWIntrinsic
//
// For a RMW intrinsic, prefer the RMW operand to the target.
// For a simple move semantic between two SIMD registers, then prefer the source operand.
//
// Arguments:
//    intrinsicTree - Tree to check
//    embedded - If this is an embedded operand
//
// Return Value:
//    The operand that needs to be delay freed
//
GenTree* LinearScan::getDelayFreeOperand(GenTreeHWIntrinsic* intrinsicTree, bool embedded)
{
    bool isRMW = intrinsicTree->isRMWHWIntrinsic(compiler);

    const NamedIntrinsic intrinsicId = intrinsicTree->GetHWIntrinsicId();
    GenTree*             delayFreeOp = nullptr;

    switch (intrinsicId)
    {
        case NI_Vector64_CreateScalarUnsafe:
        case NI_Vector128_CreateScalarUnsafe:
            if (varTypeIsFloating(intrinsicTree->Op(1)))
            {
                delayFreeOp = intrinsicTree->Op(1);
                assert(delayFreeOp != nullptr);
            }
            break;

        case NI_AdvSimd_Arm64_DuplicateToVector64:
            if (intrinsicTree->Op(1)->TypeGet() == TYP_DOUBLE)
            {
                delayFreeOp = intrinsicTree->Op(1);
                assert(delayFreeOp != nullptr);
            }
            break;

        case NI_Vector64_ToScalar:
        case NI_Vector128_ToScalar:
            if (varTypeIsFloating(intrinsicTree))
            {
                delayFreeOp = intrinsicTree->Op(1);
                assert(delayFreeOp != nullptr);
            }
            break;

        case NI_Vector64_ToVector128Unsafe:
        case NI_Vector128_AsVector128Unsafe:
        case NI_Vector128_AsVector3:
        case NI_Vector128_GetLower:
            delayFreeOp = intrinsicTree->Op(1);
            assert(delayFreeOp != nullptr);
            break;

        case NI_Sve_CreateBreakPropagateMask:
            // RMW operates on the second op.
            assert(isRMW);
            delayFreeOp = intrinsicTree->Op(2);
            assert(delayFreeOp != nullptr);
            break;

        default:
            if (isRMW)
            {
                if (HWIntrinsicInfo::IsExplicitMaskedOperation(intrinsicId))
                {
                    // First operand contains the mask, RMW op is the second one.
                    delayFreeOp = intrinsicTree->Op(2);
                    assert(delayFreeOp != nullptr);
                }
                else if (HWIntrinsicInfo::IsOptionalEmbeddedMaskedOperation(intrinsicId))
                {
                    // For optional embedded masks, RMW behavior only holds when the operand is embedded.
                    // The non-embedded variant is always not RMW.
                    if (embedded)
                    {
                        delayFreeOp = intrinsicTree->Op(1);
                        assert(delayFreeOp != nullptr);
                    }
                }
                else
                {
                    delayFreeOp = intrinsicTree->Op(1);
                    assert(delayFreeOp != nullptr);
                }
            }
            break;
    }

    return delayFreeOp;
}
//------------------------------------------------------------------------
// getVectorAddrOperand: Get the address operand of the HWIntrinsic, if any
//
// Arguments:
//    intrinsicTree - Tree to check
//
// Return Value:
//    The operand that is an address
//
GenTree* LinearScan::getVectorAddrOperand(GenTreeHWIntrinsic* intrinsicTree)
{
    GenTree* pAddr = nullptr;

    if (intrinsicTree->OperIsMemoryLoad(&pAddr))
    {
        assert(pAddr != nullptr);
        return pAddr;
    }
    if (intrinsicTree->OperIsMemoryStore(&pAddr))
    {
        assert(pAddr != nullptr);
        return pAddr;
    }

    // Operands that are not loads or stores but do require an address
    switch (intrinsicTree->GetHWIntrinsicId())
    {
        case NI_Sve_PrefetchBytes:
        case NI_Sve_PrefetchInt16:
        case NI_Sve_PrefetchInt32:
        case NI_Sve_PrefetchInt64:
        case NI_Sve_GatherPrefetch8Bit:
        case NI_Sve_GatherPrefetch16Bit:
        case NI_Sve_GatherPrefetch32Bit:
        case NI_Sve_GatherPrefetch64Bit:
            if (!varTypeIsSIMD(intrinsicTree->Op(2)->gtType))
            {
                return intrinsicTree->Op(2);
            }
            break;

        default:
            break;
    }

    return nullptr;
}

//------------------------------------------------------------------------
// getConsecutiveRegistersOperand: Get the consecutive operand of the HWIntrinsic, if any
//
// Arguments:
//    intrinsicTree           - Tree to check
//    destIsConsecutive (out) - if the destination requires consective registers
//
// Return Value:
//    The operand that requires consecutive registers.
//
//    If only the destination requires consecutive registers then destIsConsecutive will be
//    true and the function will return nullptr.
//
GenTree* LinearScan::getConsecutiveRegistersOperand(const HWIntrinsic intrin, bool* destIsConsecutive)
{
    *destIsConsecutive     = false;
    GenTree* consecutiveOp = nullptr;

    if (!HWIntrinsicInfo::NeedsConsecutiveRegisters(intrin.id))
    {
        return nullptr;
    }

    switch (intrin.id)
    {
        case NI_AdvSimd_Arm64_VectorTableLookup:
        case NI_AdvSimd_VectorTableLookup:
            consecutiveOp = intrin.op1;
            assert(consecutiveOp != nullptr);
            break;

        case NI_AdvSimd_Arm64_Store:
        case NI_AdvSimd_Arm64_StoreVectorAndZip:
        case NI_AdvSimd_Arm64_VectorTableLookupExtension:
        case NI_AdvSimd_Store:
        case NI_AdvSimd_StoreVectorAndZip:
        case NI_AdvSimd_VectorTableLookupExtension:
            consecutiveOp = intrin.op2;
            assert(consecutiveOp != nullptr);
            break;

        case NI_AdvSimd_StoreSelectedScalar:
        case NI_AdvSimd_Arm64_StoreSelectedScalar:
            if (intrin.op2->gtType == TYP_STRUCT)
            {
                consecutiveOp = intrin.op2;
                assert(consecutiveOp != nullptr);
            }
            break;

        case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x2:
        case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x3:
        case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x4:
        case NI_AdvSimd_LoadAndInsertScalarVector64x2:
        case NI_AdvSimd_LoadAndInsertScalarVector64x3:
        case NI_AdvSimd_LoadAndInsertScalarVector64x4:
            consecutiveOp = intrin.op1;
            assert(consecutiveOp != nullptr);
            *destIsConsecutive = true;
            break;

        case NI_Sve_StoreAndZipx2:
        case NI_Sve_StoreAndZipx3:
        case NI_Sve_StoreAndZipx4:
            consecutiveOp = intrin.op3;
            assert(consecutiveOp != nullptr);
            break;

        case NI_AdvSimd_Load2xVector64AndUnzip:
        case NI_AdvSimd_Load3xVector64AndUnzip:
        case NI_AdvSimd_Load4xVector64AndUnzip:
        case NI_AdvSimd_Arm64_Load2xVector128AndUnzip:
        case NI_AdvSimd_Arm64_Load3xVector128AndUnzip:
        case NI_AdvSimd_Arm64_Load4xVector128AndUnzip:
        case NI_AdvSimd_Load2xVector64:
        case NI_AdvSimd_Load3xVector64:
        case NI_AdvSimd_Load4xVector64:
        case NI_AdvSimd_Arm64_Load2xVector128:
        case NI_AdvSimd_Arm64_Load3xVector128:
        case NI_AdvSimd_Arm64_Load4xVector128:
        case NI_AdvSimd_LoadAndReplicateToVector64x2:
        case NI_AdvSimd_LoadAndReplicateToVector64x3:
        case NI_AdvSimd_LoadAndReplicateToVector64x4:
        case NI_AdvSimd_Arm64_LoadAndReplicateToVector128x2:
        case NI_AdvSimd_Arm64_LoadAndReplicateToVector128x3:
        case NI_AdvSimd_Arm64_LoadAndReplicateToVector128x4:
        case NI_Sve_Load2xVectorAndUnzip:
        case NI_Sve_Load3xVectorAndUnzip:
        case NI_Sve_Load4xVectorAndUnzip:
            *destIsConsecutive = true;
            break;

        default:
            noway_assert(!"Not a supported as multiple consecutive register intrinsic");
    }

    return consecutiveOp;
}

//------------------------------------------------------------------------
//
// Arguments:
//    intrin - Tree to check
//
// Return Value:
//    The operand that is an embedded mask
//
GenTreeHWIntrinsic* LinearScan::getEmbeddedMaskOperand(const HWIntrinsic intrin)
{
    if ((intrin.id == NI_Sve_ConditionalSelect) && (intrin.op2->IsEmbMaskOp()))
    {
        assert(intrin.op2->OperIsHWIntrinsic());
        return intrin.op2->AsHWIntrinsic();
    }
    return nullptr;
}

//------------------------------------------------------------------------
// getContainedCselOperand: Get the contained conditional select operand of the HWIntrinsic, if any
//
// Arguments:
//    intrin - Tree to check
//
// Return Value:
//    The operand that is a contained conditional select node
//
GenTreeHWIntrinsic* LinearScan::getContainedCselOperand(GenTreeHWIntrinsic* intrinsicTree)
{
    for (size_t opNum = 1; opNum <= intrinsicTree->GetOperandCount(); opNum++)
    {
        GenTree* currentOp = intrinsicTree->Op(opNum);

        if ((currentOp->OperGet() == GT_HWINTRINSIC) &&
            (currentOp->AsHWIntrinsic()->GetHWIntrinsicId() == NI_Sve_ConditionalSelect) && currentOp->isContained())
        {
            return currentOp->AsHWIntrinsic();
        }
    }
    return nullptr;
}

#endif // FEATURE_HW_INTRINSICS

#endif // TARGET_ARM64
