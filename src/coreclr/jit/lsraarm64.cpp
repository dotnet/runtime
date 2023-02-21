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
// getNextConsecutiveRefPosition: Get the next subsequent refPosition.
//
// Arguments:
//    refPosition   - The refposition for which we need to find next refposition
//
// Return Value:
//    The next refPosition or nullptr if there is not one.
//
RefPosition* LinearScan::getNextConsecutiveRefPosition(RefPosition* refPosition)
{
    RefPosition* nextRefPosition;
    assert(refPosition->needsConsecutive);
    nextConsecutiveRefPositionMap->Lookup(refPosition, &nextRefPosition);
    assert((nextRefPosition == nullptr) || nextRefPosition->needsConsecutive);
    return nextRefPosition;
}

//------------------------------------------------------------------------
// setNextConsecutiveRegisterAssignment: For subsequent refPositions, set the register
//   requirement to be the consecutive register(s) of the register that is assigned to
//   the firstRefPosition.
//
// Arguments:
//    firstRefPosition  - First refPosition of the series of consecutive registers.
//    firstReg          - Register assigned to the first refposition.
//
//  Returns:
//      True if all the consecutive registers starting from `firstRegAssigned` were free. Even if one
//      of them is busy, returns false and does not change the registerAssignment of a subsequent
//      refPosition.
//
bool LinearScan::setNextConsecutiveRegisterAssignment(RefPosition* firstRefPosition, regNumber firstRegAssigned)
{
    assert(firstRefPosition->assignedReg() == firstRegAssigned);
    assert(isSingleRegister(genRegMask(firstRegAssigned)));
    assert(firstRefPosition->isFirstRefPositionOfConsecutiveRegisters());
    assert(emitter::isVectorRegister(firstRegAssigned));

    // Verify that all the consecutive registers needed are free, if not, return false.
    // Need to do this before we set registerAssignment of any of the refPositions that
    // are part of the range.

    if (!areNextConsecutiveRegistersFree(firstRegAssigned, firstRefPosition->regCount,
                                         firstRefPosition->getInterval()->registerType))
    {
        return false;
    }

    RefPosition* consecutiveRefPosition = getNextConsecutiveRefPosition(firstRefPosition);
    regNumber    regToAssign            = firstRegAssigned == REG_FP_LAST ? REG_FP_FIRST : REG_NEXT(firstRegAssigned);

    // First refposition should always start with RefTypeUse
    assert(firstRefPosition->refType != RefTypeUpperVectorRestore);

    INDEBUG(int refPosCount = 1);
    regMaskTP busyConsecutiveRegMask = ~(((1ULL << firstRefPosition->regCount) - 1) << firstRegAssigned);

    while (consecutiveRefPosition != nullptr)
    {
        assert(consecutiveRefPosition->regCount == 0);
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        if (consecutiveRefPosition->refType == RefTypeUpperVectorRestore)
        {
            if (consecutiveRefPosition->getInterval()->isPartiallySpilled)
            {
                // Make sure that restore doesn't get one of the registers that are part of series we are trying to set
                // currently.
                // TODO-CQ: We could technically assign RefTypeUpperVectorRestore and its RefTypeUse same register, but
                // during register selection, it might get tricky to know which of the busy registers are assigned to
                // RefTypeUpperVectorRestore positions of corresponding variables for which (another criteria)
                // we are trying to find consecutive registers.

                consecutiveRefPosition->registerAssignment &= ~busyConsecutiveRegMask;
            }
            consecutiveRefPosition = getNextConsecutiveRefPosition(consecutiveRefPosition);
        }
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
        INDEBUG(refPosCount++);
        assert(consecutiveRefPosition->refType == RefTypeUse);
        consecutiveRefPosition->registerAssignment = genRegMask(regToAssign);
        consecutiveRefPosition                     = getNextConsecutiveRefPosition(consecutiveRefPosition);
        regToAssign                                = regToAssign == REG_FP_LAST ? REG_FP_FIRST : REG_NEXT(regToAssign);
    }

    assert(refPosCount == firstRefPosition->regCount);

    return true;
}

//------------------------------------------------------------------------
// areNextConsecutiveRegistersBusy: Starting with `regToAssign`, check if next
//   `registersToCheck` are free or not.
//
// Arguments:
//      - First refPosition of the series of consecutive registers.
//    regToAssign     - Register assigned to the first refposition.
//    registersCount  - Number of registers to check.
//    registerType    - Type of register.
//
//  Returns:
//      True if all the consecutive registers starting from `regToAssign` were free. Even if one
//      of them is busy, returns false.
//
bool LinearScan::areNextConsecutiveRegistersFree(regNumber regToAssign, int registersCount, var_types registerType)
{
    for (int i = 0; i < registersCount; i++)
    {
        if (isRegInUse(regToAssign, registerType))
        {
            return false;
        }
        regToAssign = regToAssign == REG_FP_LAST ? REG_FP_FIRST : REG_NEXT(regToAssign);
    }

    return true;
}

regMaskTP LinearScan::getFreeCandidates(regMaskTP candidates, RefPosition* refPosition)
{
    regMaskTP result = candidates & m_AvailableRegs;
    if (!refPosition->isFirstRefPositionOfConsecutiveRegisters())
    {
        return result;
    }

    unsigned int registersNeeded   = refPosition->regCount;
    regMaskTP    currAvailableRegs = result;
    if (BitOperations::PopCount(currAvailableRegs) < registersNeeded)
    {
        // If number of free registers are less than what we need, no point in scanning
        // for them.
        return RBM_NONE;
    }

// At this point, for 'n' registers requirement, if Rm+1, Rm+2, Rm+3, ..., Rm+k are
// available, create the mask only for Rm+1, Rm+2, ..., Rm+(k-n+1) to convey that it
// is safe to assign any of those registers, but not beyond that.
#define AppendConsecutiveMask(startIndex, endIndex, availableRegistersMask)                                            \
    regMaskTP selectionStartMask = (1ULL << regAvailableStartIndex) - 1;                                               \
    regMaskTP selectionEndMask   = (1ULL << (regAvailableEndIndex - registersNeeded + 1)) - 1;                         \
    consecutiveResult |= availableRegistersMask & (selectionEndMask & ~selectionStartMask);                            \
    overallResult |= availableRegistersMask;

    regMaskTP overallResult          = RBM_NONE;
    regMaskTP consecutiveResult      = RBM_NONE;
    DWORD  regAvailableStartIndex = 0, regAvailableEndIndex = 0;
    do
    {
        // From LSB, find the first available register (bit `1`)
        BitScanForward64(&regAvailableStartIndex, static_cast<DWORD64>(currAvailableRegs));
        regMaskTP startMask    = (1ULL << regAvailableStartIndex) - 1;

        // Mask all the bits that are processed from LSB thru regAvailableStart until the last `1`.
        regMaskTP maskProcessed = ~(currAvailableRegs | startMask);

        // From regAvailableStart, find the first unavailable register (bit `0`).
        if (maskProcessed == 0)
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
            BitScanForward64(&regAvailableEndIndex, static_cast<DWORD64>(maskProcessed));
        }
        regMaskTP endMask = (1ULL << regAvailableEndIndex) - 1;

        // Anything between regAvailableStart and regAvailableEnd is the range of consecutive registers available
        // If they are equal to or greater than our register requirements, then add all of them to the result.
        if ((regAvailableEndIndex - regAvailableStartIndex) >= registersNeeded)
        {
            AppendConsecutiveMask(regAvailableStartIndex, regAvailableEndIndex, (endMask & ~startMask));
        }
        currAvailableRegs &= ~endMask;
    } while (currAvailableRegs != RBM_NONE);

    if (compiler->opts.OptimizationEnabled())
    {
        // One last time, check if subsequent refpositions already have consecutive registers assigned
        // and if yes, and if one of the register out of consecutiveResult is available for the first
        // refposition, then just use that. This will avoid unnecessary copies.

        regNumber firstRegNum  = REG_NA;
        regNumber prevRegNum   = REG_NA;
        int       foundCount   = 0;
        regMaskTP foundRegMask = RBM_NONE;

        RefPosition* consecutiveRefPosition = getNextConsecutiveRefPosition(refPosition);
        assert(consecutiveRefPosition != nullptr);

        for (unsigned int i = 1; i < registersNeeded; i++)
        {
            Interval* interval     = consecutiveRefPosition->getInterval();
            consecutiveRefPosition = getNextConsecutiveRefPosition(consecutiveRefPosition);

            if (!interval->isActive)
            {
                foundRegMask = RBM_NONE;
                foundCount   = 0;
                continue;
            }

            regNumber currRegNum = interval->assignedReg->regNum;
            if ((prevRegNum == REG_NA) || (prevRegNum == REG_PREV(currRegNum)) ||
                ((prevRegNum == REG_FP_LAST) && (currRegNum == REG_FP_FIRST)))
            {
                foundRegMask |= genRegMask(currRegNum);
                if (prevRegNum == REG_NA)
                {
                    firstRegNum = currRegNum;
                }
                prevRegNum = currRegNum;
                foundCount++;
                continue;
            }

            foundRegMask = RBM_NONE;
            foundCount   = 0;
            break;
        }

        if (foundCount != 0)
        {
            assert(firstRegNum != REG_NA);
            regMaskTP remainingRegsMask = ((1ULL << (registersNeeded - foundCount)) - 1) << (firstRegNum - 1);

            if ((overallResult & remainingRegsMask) != RBM_NONE)
            {
                // If remaining registers are available, then just set the firstRegister mask
                consecutiveResult = 1ULL << (firstRegNum - 1);
            }
        }
    }

    return consecutiveResult;
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
    int       dstCount      = 0;
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
            srcCount = BuildSimple(tree);
            break;

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
                buildInternalFloatRegisterDefForNode(tree, allSIMDRegs());
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
            BuildDefsWithKills(tree, 0, RBM_NONE, killMask);
            break;

        case GT_START_PREEMPTGC:
            // This kills GC refs in callee save regs
            srcCount = 0;
            assert(dstCount == 0);
            BuildDefsWithKills(tree, 0, RBM_NONE, RBM_NONE);
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
            BuildDefsWithKills(tree, 0, RBM_NONE, killMask);
            break;

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
                BuildUse(tree->gtGetOp1(), RBM_INTRET);
            }
            break;

        case GT_NOP:
            // A GT_NOP is either a passthrough (if it is void, or if it has
            // a child), but must be considered to produce a dummy value if it
            // has a type but no child.
            srcCount = 0;
            if (tree->TypeGet() != TYP_VOID && tree->gtGetOp1() == nullptr)
            {
                assert(dstCount == 1);
                BuildDef(tree);
            }
            else
            {
                assert(dstCount == 0);
            }
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

        case GT_ASG:
            noway_assert(!"We should never hit any assignment operator in lowering");
            srcCount = 0;
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
        case GT_XOR:
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
            BuildDefsWithKills(tree, 0, RBM_NONE, killMask);
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
                    assert(varTypeIsFloating(tree->gtGetOp1()));
                    assert(varTypeIsFloating(tree->gtGetOp2()));
                    assert(tree->gtGetOp1()->TypeIs(tree->TypeGet()));

                    srcCount = BuildBinaryUses(tree->AsOp());
                    assert(dstCount == 1);
                    BuildDef(tree);
                    break;

                case NI_System_Math_Abs:
                case NI_System_Math_Ceiling:
                case NI_System_Math_Floor:
                case NI_System_Math_Truncate:
                case NI_System_Math_Round:
                case NI_System_Math_Sqrt:
                    assert(varTypeIsFloating(tree->gtGetOp1()));
                    assert(tree->gtGetOp1()->TypeIs(tree->TypeGet()));

                    BuildUse(tree->gtGetOp1());
                    srcCount = 1;
                    assert(dstCount == 1);
                    BuildDef(tree);
                    break;

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
        case GT_JCMP:
            srcCount = BuildCmp(tree);
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
            srcCount                    = cmpXchgNode->gtOpComparand->isContained() ? 2 : 3;
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

            RefPosition* locationUse = BuildUse(tree->AsCmpXchg()->gtOpLocation);
            setDelayFree(locationUse);
            RefPosition* valueUse = BuildUse(tree->AsCmpXchg()->gtOpValue);
            setDelayFree(valueUse);
            if (!cmpXchgNode->gtOpComparand->isContained())
            {
                RefPosition* comparandUse = BuildUse(tree->AsCmpXchg()->gtOpComparand);

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
            assert(dstCount == (tree->TypeGet() == TYP_VOID) ? 0 : 1);
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
            }
            if (dstCount == 1)
            {
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
        case GT_STORE_OBJ:
        case GT_STORE_DYN_BLK:
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
                    // Note: The Gentree node is not updated here as it is cheap to recompute stack aligned size.
                    // This should also help in debugging as we can examine the original size specified with
                    // localloc.
                    sizeVal = AlignUp(sizeVal, STACK_ALIGN);

                    if (sizeVal <= LCLHEAP_UNROLL_LIMIT)
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
            // These must have been lowered to GT_ARR_INDEX
            noway_assert(!"We should never see a GT_ARR_ELEM in lowering");
            srcCount = 0;
            assert(dstCount == 0);
            break;

        case GT_ARR_INDEX:
        {
            srcCount = 2;
            assert(dstCount == 1);
            buildInternalIntRegisterDefForNode(tree);
            setInternalRegsDelayFree = true;

            // For GT_ARR_INDEX, the lifetime of the arrObj must be extended because it is actually used multiple
            // times while the result is being computed.
            RefPosition* arrObjUse = BuildUse(tree->AsArrIndex()->ArrObj());
            setDelayFree(arrObjUse);
            BuildUse(tree->AsArrIndex()->IndexExpr());
            buildInternalRegisterUses();
            BuildDef(tree);
        }
        break;

        case GT_ARR_OFFSET:
            // This consumes the offset, if any, the arrObj and the effective index,
            // and produces the flattened offset for this dimension.
            srcCount = 2;
            if (!tree->AsArrOffs()->gtOffset->isContained())
            {
                BuildUse(tree->AsArrOffs()->gtOffset);
                srcCount++;
            }
            BuildUse(tree->AsArrOffs()->gtIndex);
            BuildUse(tree->AsArrOffs()->gtArrObj);
            assert(dstCount == 1);
            buildInternalIntRegisterDefForNode(tree);
            buildInternalRegisterUses();
            BuildDef(tree);
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
            BuildDef(tree, RBM_EXCEPTION_OBJECT);
            break;

        case GT_INDEX_ADDR:
            assert(dstCount == 1);
            srcCount = BuildBinaryUses(tree->AsOp());
            buildInternalIntRegisterDefForNode(tree);
            buildInternalRegisterUses();
            BuildDef(tree);
            break;

        case GT_SELECT:
            assert(dstCount == 1);
            srcCount = BuildSelect(tree->AsConditional());
            break;

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
    int dstCount = 0;

    if (HWIntrinsicInfo::IsMultiReg(intrin.id))
    {
        dstCount = intrinsicTree->GetMultiRegCount(compiler);
    }
    else if (intrinsicTree->IsValue())
    {
        dstCount = 1;
    }

    const bool hasImmediateOperand = HWIntrinsicInfo::HasImmediateOperand(intrin.id);

    if (hasImmediateOperand && !HWIntrinsicInfo::NoJmpTableImm(intrin.id))
    {
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

            if (intrin.numOperands == 3)
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
            HWIntrinsicInfo::lookupImmBounds(intrin.id, indexedElementSimdSize, intrin.baseType, &immLowerBound,
                                             &immUpperBound);
        }
        else
        {
            HWIntrinsicInfo::lookupImmBounds(intrin.id, intrinsicTree->GetSimdSize(), intrin.baseType, &immLowerBound,
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
                    case NI_AdvSimd_Arm64_DuplicateSelectedScalarToVector128:
                        needBranchTargetReg = !intrin.op2->isContainedIntOrIImmed();
                        break;

                    case NI_AdvSimd_ExtractVector64:
                    case NI_AdvSimd_ExtractVector128:
                    case NI_AdvSimd_StoreSelectedScalar:
                        needBranchTargetReg = !intrin.op3->isContainedIntOrIImmed();
                        break;

                    case NI_AdvSimd_Arm64_InsertSelectedScalar:
                        assert(intrin.op2->isContainedIntOrIImmed());
                        assert(intrin.op4->isContainedIntOrIImmed());
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

    // Determine whether this is an RMW operation where op2+ must be marked delayFree so that it
    // is not allocated the same register as the target.
    const bool isRMW = intrinsicTree->isRMWHWIntrinsic(compiler);

    bool tgtPrefOp1 = false;

    if (intrin.op1 != nullptr)
    {
        bool simdRegToSimdRegMove = false;

        switch (intrin.id)
        {
            case NI_Vector64_CreateScalarUnsafe:
            case NI_Vector128_CreateScalarUnsafe:
            {
                simdRegToSimdRegMove = varTypeIsFloating(intrin.op1);
                break;
            }

            case NI_AdvSimd_Arm64_DuplicateToVector64:
            {
                simdRegToSimdRegMove = (intrin.op1->TypeGet() == TYP_DOUBLE);
                break;
            }

            case NI_Vector64_ToScalar:
            case NI_Vector128_ToScalar:
            {
                simdRegToSimdRegMove = varTypeIsFloating(intrinsicTree);
                break;
            }

            case NI_Vector64_ToVector128Unsafe:
            case NI_Vector128_AsVector3:
            case NI_Vector128_GetLower:
            {
                simdRegToSimdRegMove = true;
                break;
            }

            default:
            {
                break;
            }
        }

        // If we have an RMW intrinsic or an intrinsic with simple move semantic between two SIMD registers,
        // we want to preference op1Reg to the target if op1 is not contained.
        if (isRMW || simdRegToSimdRegMove)
        {
            tgtPrefOp1 = !intrin.op1->isContained();
        }

        if (intrinsicTree->OperIsMemoryLoadOrStore())
        {
            srcCount += BuildAddrUses(intrin.op1);
        }
        else if (tgtPrefOp1)
        {
            tgtPrefUse = BuildUse(intrin.op1);
            srcCount++;
        }
        else
        {
            if ((intrin.id == NI_AdvSimd_VectorTableLookup) || (intrin.id == NI_AdvSimd_Arm64_VectorTableLookup))
            {
                srcCount += BuildConsecutiveRegisters(intrin.op1);
            }
            else
            {
                srcCount += BuildOperandUses(intrin.op1);
            }
        }
    }

    if ((intrin.id == NI_AdvSimd_VectorTableLookup) || (intrin.id == NI_AdvSimd_Arm64_VectorTableLookup) ||
        (intrin.id == NI_AdvSimd_VectorTableLookupExtension) ||
        (intrin.id == NI_AdvSimd_Arm64_VectorTableLookupExtension))
    {
        if ((intrin.id == NI_AdvSimd_VectorTableLookup) || (intrin.id == NI_AdvSimd_Arm64_VectorTableLookup))
        {
            assert(intrin.op2 != nullptr);
            srcCount += BuildOperandUses(intrin.op2);
        }
        else
        {
            assert(intrin.op2 != nullptr);
            assert(intrin.op3 != nullptr);
            srcCount += BuildConsecutiveRegisters(intrin.op2, intrin.op1);
            srcCount += isRMW ? BuildDelayFreeUses(intrin.op3, intrin.op1) : BuildOperandUses(intrin.op3);
        }
        assert(dstCount == 1);
        buildInternalRegisterUses();
        BuildDef(intrinsicTree);
        *pDstCount = 1;

        return srcCount;
    }
    else if ((intrin.category == HW_Category_SIMDByIndexedElement) && (genTypeSize(intrin.baseType) == 2))
    {
        // Some "Advanced SIMD scalar x indexed element" and "Advanced SIMD vector x indexed element" instructions (e.g.
        // "MLA (by element)") have encoding that restricts what registers that can be used for the indexed element when
        // the element size is H (i.e. 2 bytes).
        assert(intrin.op2 != nullptr);

        if ((intrin.op4 != nullptr) || ((intrin.op3 != nullptr) && !hasImmediateOperand))
        {
            if (isRMW)
            {
                srcCount += BuildDelayFreeUses(intrin.op2, nullptr);
                srcCount += BuildDelayFreeUses(intrin.op3, nullptr, RBM_ASIMD_INDEXED_H_ELEMENT_ALLOWED_REGS);
            }
            else
            {
                srcCount += BuildOperandUses(intrin.op2);
                srcCount += BuildOperandUses(intrin.op3, RBM_ASIMD_INDEXED_H_ELEMENT_ALLOWED_REGS);
            }

            if (intrin.op4 != nullptr)
            {
                assert(hasImmediateOperand);
                assert(varTypeIsIntegral(intrin.op4));

                srcCount += BuildOperandUses(intrin.op4);
            }
        }
        else
        {
            assert(!isRMW);

            srcCount += BuildOperandUses(intrin.op2, RBM_ASIMD_INDEXED_H_ELEMENT_ALLOWED_REGS);

            if (intrin.op3 != nullptr)
            {
                assert(hasImmediateOperand);
                assert(varTypeIsIntegral(intrin.op3));

                srcCount += BuildOperandUses(intrin.op3);
            }
        }
    }
    else if (intrin.op2 != nullptr)
    {
        // RMW intrinsic operands doesn't have to be delayFree when they can be assigned the same register as op1Reg
        // (i.e. a register that corresponds to read-modify-write operand) and one of them is the last use.

        assert(intrin.op1 != nullptr);

        bool forceOp2DelayFree = false;
        if ((intrin.id == NI_Vector64_GetElement) || (intrin.id == NI_Vector128_GetElement))
        {
            if (!intrin.op2->IsCnsIntOrI() && (!intrin.op1->isContained() || intrin.op1->OperIsLocal()))
            {
                // If the index is not a constant and the object is not contained or is a local
                // we will need a general purpose register to calculate the address
                // internal register must not clobber input index
                // TODO-Cleanup: An internal register will never clobber a source; this code actually
                // ensures that the index (op2) doesn't interfere with the target.
                buildInternalIntRegisterDefForNode(intrinsicTree);
                forceOp2DelayFree = true;
            }

            if (!intrin.op2->IsCnsIntOrI() && !intrin.op1->isContained())
            {
                // If the index is not a constant or op1 is in register,
                // we will use the SIMD temp location to store the vector.
                var_types requiredSimdTempType = (intrin.id == NI_Vector64_GetElement) ? TYP_SIMD8 : TYP_SIMD16;
                compiler->getSIMDInitTempVarNum(requiredSimdTempType);
            }
        }

        if (forceOp2DelayFree)
        {
            srcCount += BuildDelayFreeUses(intrin.op2);
        }
        else
        {
            srcCount += isRMW ? BuildDelayFreeUses(intrin.op2, intrin.op1) : BuildOperandUses(intrin.op2);
        }

        if (intrin.op3 != nullptr)
        {
            srcCount += isRMW ? BuildDelayFreeUses(intrin.op3, intrin.op1) : BuildOperandUses(intrin.op3);

            if (intrin.op4 != nullptr)
            {
                srcCount += isRMW ? BuildDelayFreeUses(intrin.op4, intrin.op1) : BuildOperandUses(intrin.op4);
            }
        }
    }

    buildInternalRegisterUses();

    if ((dstCount == 1) || (dstCount == 2))
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

int LinearScan::BuildConsecutiveRegisters(GenTree* treeNode, GenTree* rmwNode)
{
    int       srcCount     = 0;
    Interval* rmwInterval  = nullptr;
    bool      rmwIsLastUse = false;
    if ((rmwNode != nullptr))
    {
        if (isCandidateLocalRef(rmwNode))
        {
            rmwInterval  = getIntervalForLocalVarNode(rmwNode->AsLclVar());
            rmwIsLastUse = rmwNode->AsLclVar()->IsLastUse(0);
        }
    }
    if (treeNode->OperIsFieldList())
    {
        unsigned     regCount    = 0;
        RefPosition* firstRefPos = nullptr;
        RefPosition* currRefPos  = nullptr;
        RefPosition* lastRefPos  = nullptr;

        NextConsecutiveRefPositionsMap* refPositionMap = getNextConsecutiveRefPositionsMap();
        for (GenTreeFieldList::Use& use : treeNode->AsFieldList()->Uses())
        {
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            RefPosition* restoreRefPos = nullptr;
            currRefPos                 = BuildUse(use.GetNode(), RBM_NONE, 0, &restoreRefPos);
#else
            currRefPos = BuildUse(use.GetNode());
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            currRefPos->needsConsecutive = true;
            currRefPos->regCount         = 0;
#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
            if (restoreRefPos != nullptr)
            {
                // If there was a restoreRefPosition created, make sure
                // to link it as well so it gets same registerAssignment
                restoreRefPos->needsConsecutive = true;
                restoreRefPos->regCount         = 0;
                if (firstRefPos == nullptr)
                {
                    // Always set the non  UpperVectorRestore. UpperVectorRestore can be assigned
                    // different independent register.
                    // See TODO-CQ in setNextConsecutiveRegisterAssignment().
                    firstRefPos = currRefPos;
                }
                refPositionMap->Set(lastRefPos, restoreRefPos, LinearScan::NextConsecutiveRefPositionsMap::Overwrite);
                refPositionMap->Set(restoreRefPos, currRefPos, LinearScan::NextConsecutiveRefPositionsMap::Overwrite);
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
        }

        // Just `regCount` to actual registers count for first ref-position.
        // For others, set 0 so we can identify that this is non-first refposition.
        firstRefPos->regCount = regCount;
        srcCount += regCount;
    }
    else
    {
        srcCount += BuildOperandUses(treeNode);
    }

    return srcCount;
}
#endif

#endif // TARGET_ARM64
