// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// We mainly rely on TryLowerSwitchToBitTest in these heuristics, but jump tables can be useful
// even without conversion to a bitmap test.
#define SWITCH_MAX_DISTANCE ((TARGET_POINTER_SIZE * BITS_PER_BYTE) - 1)

// Sets that span up to two bit tables are tested with a bit test for the first table and a range check for the rest.
#define SWITCH_MAX_WIDE_DISTANCE ((2 * (SWITCH_MAX_DISTANCE + 1)) - 1)

#if TARGET_ARM64
// ARM64 has conditional instructions which can be more efficient than a switch->bittest (optOptimizeBools)
#define SWITCH_MIN_TESTS 5
#else
#define SWITCH_MIN_TESTS 3
#endif

// This is a heuristics based value tuned for optimal performance
#define CONVERT_SWITCH_TO_CCMP_MIN_TEST 5

//-----------------------------------------------------------------------------
//  optRecognizeAndOptimizeSwitchJumps: Optimize range check for `x == cns1 || x == cns2 || x == cns3 ...`
//      pattern and convert it to a BBJ_SWITCH block (jump table), which then *might* be converted
//      to a bitmap test via TryLowerSwitchToBitTest.
//      If we have PGO data, try peeling switches with dominant cases.
//      TODO: recognize general jump table patterns.
//
//  Return Value:
//      MODIFIED_EVERYTHING if any switches were newly identified and/or optimized, false otherwise
//
PhaseStatus Compiler::optRecognizeAndOptimizeSwitchJumps()
{
    bool modified = false;

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->Next())
    {
        if (block->isRunRarely())
        {
            continue;
        }

        // block->KindIs(BBJ_COND) check is for better throughput.
        if (block->KindIs(BBJ_COND) && (optSwitchDetectAndConvert(block) || optSwitchConvertRangeTests(block)))
        {
            JITDUMP("Converted block " FMT_BB " to switch\n", block->bbNum)
            modified = true;

            // Converted switches won't have dominant cases, so we can skip the switch peeling check.
            assert(!block->GetSwitchTargets()->HasDominantCase());
        }
        else if (block->KindIs(BBJ_SWITCH) && block->GetSwitchTargets()->HasDominantCase())
        {
            fgPeelSwitch(block);
            modified = true;

            // Switch peeling will convert this block into a check for the dominant case,
            // and insert the updated switch block after, which doesn't have a dominant case.
            // Skip over the switch block in the loop iteration.
            assert(block->Next()->KindIs(BBJ_SWITCH));
            block = block->Next();
        }
    }

    return modified ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------------
// SkipFallthroughBlocks : Skip all fallthrough blocks (empty BBJ_ALWAYS blocks)
//
// Arguments:
//    compiler - The compiler instance.
//    block    - the block to process
//
// Return Value:
//    block that is not an empty fallthrough block
//
static BasicBlock* SkipFallthroughBlocks(Compiler* comp, BasicBlock* block)
{
    BasicBlock* origBlock = block;
    if (!block->KindIs(BBJ_ALWAYS) || (block->firstStmt() != nullptr))
    {
        // Fast path
        return block;
    }

    // We might consider ignoring statements with only NOPs if that is profitable.

    BitVecTraits traits(comp->compBasicBlockID, comp);
    BitVec       visited = BitVecOps::MakeEmpty(&traits);
    BitVecOps::AddElemD(&traits, visited, block->bbID);
    while (block->KindIs(BBJ_ALWAYS) && (block->firstStmt() == nullptr) &&
           BasicBlock::sameEHRegion(block, block->GetTarget()))
    {
        block = block->GetTarget();
        if (!BitVecOps::TryAddElemD(&traits, visited, block->bbID))
        {
            // A cycle detected, bail out.
            return origBlock;
        }
    }
    return block;
}

//------------------------------------------------------------------------------
// IsConstantTestCondBlock : Does the given block represent a simple BBJ_COND
//    constant test? e.g. JTRUE(EQ/NE(X, CNS)).
//
// Arguments:
//    compiler         - The compiler instance.
//    block            - The block to check
//    allowSideEffects - is variableNode allowed to have side-effects (COMMA)?
//    trueTarget       - [out] The successor visited if X == CNS
//    falseTarget      - [out] The successor visited if X != CNS
//    isReversed       - [out] True if the condition is reversed (GT_NE)
//    variableNode     - [out] The variable node (X in the example above)
//    cns              - [out] The constant value (CNS in the example above)
//
// Return Value:
//    True if the block represents a constant test, false otherwise
//
bool IsConstantTestCondBlock(Compiler*         comp,
                             const BasicBlock* block,
                             bool              allowSideEffects,
                             BasicBlock**      trueTarget,
                             BasicBlock**      falseTarget,
                             bool*             isReversed,
                             GenTree**         variableNode = nullptr,
                             ssize_t*          cns          = nullptr)
{
    // NOTE: caller is expected to check that a block has multiple statements or not
    if (block->KindIs(BBJ_COND) && (block->lastStmt() != nullptr) && !block->HasFlag(BBF_DONT_REMOVE))
    {
        const GenTree* rootNode = block->lastStmt()->GetRootNode();
        assert(rootNode->OperIs(GT_JTRUE));

        // It has to be JTRUE(GT_EQ or GT_NE)
        if (rootNode->gtGetOp1()->OperIs(GT_EQ, GT_NE))
        {
            GenTree* op1 = rootNode->gtGetOp1()->gtGetOp1();
            GenTree* op2 = rootNode->gtGetOp1()->gtGetOp2();

            if (!varTypeIsIntOrI(op1) || !varTypeIsIntOrI(op2))
            {
                // Only TYP_INT and TYP_LONG are supported
                return false;
            }

            // We're looking for "X EQ/NE CNS" or "CNS EQ/NE X" pattern
            if ((op1->IsCnsIntOrI() && !op1->IsIconHandle()) ^ (op2->IsCnsIntOrI() && !op2->IsIconHandle()))
            {
                if (allowSideEffects)
                {
                    if (!op1->gtEffectiveVal()->OperIs(GT_LCL_VAR) && !op2->gtEffectiveVal()->OperIs(GT_LCL_VAR))
                    {
                        return false;
                    }
                }
                else if (!op1->OperIs(GT_LCL_VAR) && !op2->OperIs(GT_LCL_VAR))
                {
                    return false;
                }

                *isReversed = rootNode->gtGetOp1()->OperIs(GT_NE);
                *trueTarget =
                    SkipFallthroughBlocks(comp, *isReversed ? block->GetFalseTarget() : block->GetTrueTarget());
                *falseTarget =
                    SkipFallthroughBlocks(comp, *isReversed ? block->GetTrueTarget() : block->GetFalseTarget());

                if (block->FalseTargetIs(block) || block->TrueTargetIs(block))
                {
                    // Ignoring weird cases like a condition jumping to itself
                    return false;
                }

                if ((variableNode != nullptr) && (cns != nullptr))
                {
                    if (op1->IsCnsIntOrI())
                    {
                        *cns          = op1->AsIntCon()->IconValue();
                        *variableNode = op2;
                    }
                    else
                    {
                        *cns          = op2->AsIntCon()->IconValue();
                        *variableNode = op1;
                    }
                }
                return true;
            }
        }
    }
    return false;
}

//------------------------------------------------------------------------------
// optSwitchDetectAndConvert : Try to detect a series of conditional blocks which
//    can be converted into a switch (jump-table) construct. See optSwitchConvert
//    for more details.
//
// Arguments:
//    firstBlock - A block to start the search from
//    testingForConversion - Test if its likely a switch conversion will happen.
//    Used to prevent a pessimization when optimizing for conditional chaining.
//    Done in this function to prevent maintaining the check in two places.
//    ccmpVec - BitVec to use to track all the nodes participating in a single switch
//
// Return Value:
//    True if the conversion was successful, false otherwise
//
bool Compiler::optSwitchDetectAndConvert(BasicBlock* firstBlock, bool testingForConversion, BitVec* ccmpVec)
{
    assert(firstBlock->KindIs(BBJ_COND));

#ifdef TARGET_ARM
    // Bitmap test (TryLowerSwitchToBitTest) is quite verbose on ARM32 (~6 instructions), it results in massive size
    // regressions.
    return false;
#endif

    GenTree*    variableNode                    = nullptr;
    ssize_t     cns                             = 0;
    BasicBlock* trueTarget                      = nullptr;
    BasicBlock* falseTarget                     = nullptr;
    int         testValueIndex                  = 0;
    ssize_t     testValues[SWITCH_MAX_DISTANCE] = {};
    weight_t    falseLikelihood                 = firstBlock->GetFalseEdge()->getLikelihood();

    // The algorithm is simple - we check that the given block is a constant test block
    // and then try to accumulate as many constant test blocks as possible. Once we hit
    // a block that doesn't match the pattern, we start processing the accumulated blocks.
    bool isReversed = false;
    if (IsConstantTestCondBlock(this, firstBlock, true, &trueTarget, &falseTarget, &isReversed, &variableNode, &cns))
    {
        if (isReversed)
        {
            // First block uses NE - we don't support this yet. We currently expect all blocks to use EQ
            // and allow NE for the last one (because it's what Roslyn usually emits).
            // TODO: make it more flexible and support cases like "x != cns1 && x != cns2 && ..."
            return false;
        }
        if (testingForConversion)
        {
            assert(ccmpVec != nullptr);
            BitVecTraits ccmpTraits(fgBBNumMax + 1, this);
            if (BitVecOps::IsMember(&ccmpTraits, *ccmpVec, firstBlock->bbNum))
            {
                BitVecOps::RemoveElemD(&ccmpTraits, *ccmpVec, firstBlock->bbNum);
                return true;
            }
            else
            {
                BitVecOps::ClearD(&ccmpTraits, *ccmpVec);
            }
        }

        // No more than SWITCH_MAX_TABLE_SIZE blocks are allowed (arbitrary limit in this context)
        testValueIndex             = 0;
        testValues[testValueIndex] = cns;
        testValueIndex++;

        // Track likelihood of reaching the false block
        //
        falseLikelihood             = firstBlock->GetFalseEdge()->getLikelihood();
        const BasicBlock* prevBlock = firstBlock;

        // Now walk the chain of test blocks, and see if they are basically the same type of test
        for (BasicBlock *currBb = falseTarget, *currFalseTarget; currBb != nullptr; currBb = currFalseTarget)
        {
            GenTree*    currVariableNode = nullptr;
            ssize_t     currCns          = 0;
            BasicBlock* currTrueTarget   = nullptr;

            if (!currBb->hasSingleStmt())
            {
                // Only the first conditional block can have multiple statements.
                // Stop searching and process what we already have.
                return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode,
                                        testingForConversion, ccmpVec);
            }

            // Inspect secondary blocks
            if (IsConstantTestCondBlock(this, currBb, false, &currTrueTarget, &currFalseTarget, &isReversed,
                                        &currVariableNode, &currCns))
            {
                if (currTrueTarget != trueTarget)
                {
                    // This blocks jumps to a different target, stop searching and process what we already have.
                    return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode,
                                            testingForConversion, ccmpVec);
                }

                if (!GenTree::Compare(currVariableNode, variableNode->gtEffectiveVal()))
                {
                    // A different variable node is used, stop searching and process what we already have.
                    return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode,
                                            testingForConversion, ccmpVec);
                }

                if (currBb->GetUniquePred(this) != prevBlock)
                {
                    // Multiple preds in a secondary block, stop searching and process what we already have.
                    return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode,
                                            testingForConversion, ccmpVec);
                }

                if (!BasicBlock::sameEHRegion(prevBlock, currBb))
                {
                    // Current block is in a different EH region, stop searching and process what we already have.
                    return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode,
                                            testingForConversion, ccmpVec);
                }

                // Ok we can work with that, add the test value to the list
                testValues[testValueIndex++] = currCns;
                falseLikelihood *= currBb->GetFalseEdge()->getLikelihood();

                if (testValueIndex == SWITCH_MAX_DISTANCE)
                {
                    // Too many suitable tests found - stop and process what we already have.
                    return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode,
                                            testingForConversion, ccmpVec);
                }

                if (isReversed)
                {
                    // We only support reversed test (GT_NE) for the last block.
                    return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode,
                                            testingForConversion, ccmpVec);
                }
                prevBlock = currBb;
            }
            else
            {
                // Current block is not a suitable test, stop searching and process what we already have.
                return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode,
                                        testingForConversion, ccmpVec);
            }
        }
    }

    return false;
}

//------------------------------------------------------------------------------
// optSwitchConvert : Convert a series of conditional blocks into a switch block
//    conditional blocks are blocks that have a single statement that is a GT_EQ
//    or GT_NE node. The blocks are expected jump into the same target and test
//    the same variable against different constants
//
// Arguments:
//    firstBlock - First conditional block in the chain
//    testsCount - Number of conditional blocks in the chain
//    testValues - Array of constants that are tested against the variable
//    falseLikelihood - Likelihood of control flow reaching the false block
//    switchValue - Variable node that is tested against the constants
//    testingForConversion - Test if its likely a switch conversion will happen.
//    Used to prevent a pessimization when optimizing for conditional chaining.
//    Done in this function to prevent maintaining the check in two places.
//    ccmpVec - BitVec to use to track all the nodes participating in a single switch
//
// Return Value:
//    True if the conversion was successful, false otherwise
//
bool Compiler::optSwitchConvert(BasicBlock* firstBlock,
                                int         testsCount,
                                ssize_t*    testValues,
                                weight_t    falseLikelihood,
                                GenTree*    switchValue,
                                bool        testingForConversion,
                                BitVec*     ccmpVec)
{
    assert(firstBlock->KindIs(BBJ_COND));
    assert(!varTypeIsSmall(switchValue));

    if (testingForConversion && (testsCount < CONVERT_SWITCH_TO_CCMP_MIN_TEST))
    {
        return false;
    }

    if (testsCount < SWITCH_MIN_TESTS)
    {
        // Early out - short chains.
        return false;
    }

    static_assert(SWITCH_MIN_TESTS > 0);

    // Find max and min values in the testValues array
    // At this point we have at least SWITCH_MIN_TESTS values in the array
    ssize_t minValue = testValues[0];
    ssize_t maxValue = testValues[0];

    int testIdx = 0;
    for (; testIdx < testsCount; testIdx++)
    {
        ssize_t testValue = testValues[testIdx];
        if (testValue < 0)
        {
            // We don't support negative values
            break;
        }

        const ssize_t newMinValue = min(minValue, testValue);
        const ssize_t newMaxValue = max(maxValue, testValue);
        assert(newMaxValue >= newMinValue);
        if ((newMaxValue - newMinValue) > SWITCH_MAX_DISTANCE)
        {
            // Stop here, the distance between min and max is too big
            break;
        }
        minValue = newMinValue;
        maxValue = newMaxValue;
    }

    // testIdx is now representing the index of last good test value,
    // Update testsCount as it's now potentially smaller than initially.
    testsCount = testIdx;

    if (testsCount < SWITCH_MIN_TESTS)
    {
        // Make sure we still have at least SWITCH_MIN_TESTS values after we filtered out some of them
        return false;
    }

    // Normalize switch whenever it's a requirement to fit the table or as an optimization to collapse into range check
    bool doesntFitTable  = maxValue > SWITCH_MAX_DISTANCE;
    bool continuousRange = testsCount == (maxValue - minValue + 1);
    if (doesntFitTable || continuousRange)
    {
        if (minValue != 0)
        {
            switchValue = gtNewOperNode(GT_ADD, switchValue->TypeGet(), switchValue,
                                        gtNewIconNode(-minValue, switchValue->TypeGet()));
        }
    }
    else
    {
        minValue = 0;
    }

    // Find the last block in the chain
    const BasicBlock* lastBlock = firstBlock;
    for (int i = 0; i < (testsCount - 1); i++)
    {
        const GenTree* rootNode = lastBlock->lastStmt()->GetRootNode();
        assert(lastBlock->KindIs(BBJ_COND));
        assert(rootNode->OperIs(GT_JTRUE));

        // We only support reversed tests (GT_NE) in the last block of the chain.
        assert(rootNode->gtGetOp1()->OperIs(GT_EQ));
        lastBlock = lastBlock->GetFalseTarget();
    }

    BasicBlock* blockIfTrue  = nullptr;
    BasicBlock* blockIfFalse = nullptr;
    bool        isReversed   = false;
    const bool  isTest = IsConstantTestCondBlock(this, lastBlock, false, &blockIfTrue, &blockIfFalse, &isReversed);
    assert(isTest);

    assert(SkipFallthroughBlocks(this, firstBlock->GetTrueTarget()) == SkipFallthroughBlocks(this, blockIfTrue));

    if (testingForConversion)
    {
        assert(ccmpVec != nullptr);
        BitVecTraits ccmpTraits(fgBBNumMax + 1, this);
        // Return if we are just checking for a possibility of a switch convert and not actually making the conversion
        // to switch here.
        BasicBlock* iterBlock = firstBlock;
        for (int i = 0; i < testsCount; i++)
        {
            BitVecOps::AddElemD(&ccmpTraits, *ccmpVec, iterBlock->bbNum);
            iterBlock = iterBlock->GetFalseTarget();
        }
        return true;
    }

    // Compose a bit vector of all the values we have in the testValues array
    // to quickly check if a value is in the array
    ssize_t bitVector = 0;
    for (testIdx = 0; testIdx < testsCount; testIdx++)
    {
        assert(testIdx <= (int)((sizeof(ssize_t) * BITS_PER_BYTE) - 1));
        bitVector |= (ssize_t)(1ULL << static_cast<unsigned>((testValues[testIdx] - minValue)));
    }

    // All the conditional blocks that follow firstBlock in the chain are going to be removed.
    // We always follow the false target because reversed tests are only supported for the last block.
    BasicBlock* chainBlocks[SWITCH_MAX_DISTANCE];
    assert(testsCount <= SWITCH_MAX_DISTANCE);
    BasicBlock* chainBlock = firstBlock;
    for (int i = 0; i < (testsCount - 1); i++)
    {
        assert(chainBlock->KindIs(BBJ_COND));
        chainBlock     = chainBlock->GetFalseTarget();
        chainBlocks[i] = chainBlock;
    }

    optConvertToBitmapSwitch(firstBlock, switchValue, minValue, maxValue, bitVector, blockIfTrue, blockIfFalse,
                             blockIfFalse, 1.0 - falseLikelihood, falseLikelihood, lastBlock->bbCodeOffsEnd,
                             chainBlocks, testsCount - 1);
    return true;
}

//------------------------------------------------------------------------------
// optConvertToBitmapSwitch : Convert a BBJ_COND block into a BBJ_SWITCH block that has only two distinct
//    targets (plus the default case) and remove the other conditional blocks that were folded into it.
//    The resulting switch is later lowered to a bit test by TryLowerSwitchToBitTest when possible.
//
// Arguments:
//    firstBlock        - The BBJ_COND block to convert
//    switchValue       - The value to switch on. It must already be normalized to start at 'minValue'
//                        (i.e. 'minValue' has been subtracted from it) unless 'minValue' is zero.
//    minValue          - The value that is subtracted from the tested variable to get the case index
//    maxValue          - The largest tested value, (maxValue - minValue) is the last case index
//    bitVector         - A bit vector, bit N is set if case N goes to 'blockIfTrue'
//    blockIfTrue       - The target for the cases that are set in 'bitVector'
//    blockIfFalse      - The target for the cases that are not set in 'bitVector'
//    blockIfDefault    - The target for the default case (values outside of the cases). This is 'blockIfFalse'
//                        unless the switch is followed by a range check that tests the remaining values.
//    trueLikelihood    - The likelihood of control flow reaching 'blockIfTrue'
//    defaultLikelihood - The likelihood of control flow reaching 'blockIfDefault'. Only used if it differs
//                        from 'blockIfFalse'; otherwise it is 1 - 'trueLikelihood'.
//    codeOffsEnd       - The IL offset end of the converted block
//    blocksToRemove    - The conditional blocks that were folded into 'firstBlock'. The blocks are expected
//                        to be ordered so that a block comes after all of its predecessors.
//    blocksToRemoveCnt - The number of blocks in 'blocksToRemove'
//
void Compiler::optConvertToBitmapSwitch(BasicBlock* firstBlock,
                                        GenTree*    switchValue,
                                        ssize_t     minValue,
                                        ssize_t     maxValue,
                                        ssize_t     bitVector,
                                        BasicBlock* blockIfTrue,
                                        BasicBlock* blockIfFalse,
                                        BasicBlock* blockIfDefault,
                                        weight_t    trueLikelihood,
                                        weight_t    defaultLikelihood,
                                        IL_OFFSET   codeOffsEnd,
                                        BasicBlock* blocksToRemove[],
                                        unsigned    blocksToRemoveCnt)
{
    assert(firstBlock->KindIs(BBJ_COND));
    assert(blockIfTrue != blockIfFalse);

    // The default case usually goes to the same block as the false cases. It can be different when the switch is
    // the switch that is followed by a range check for the values that do not fit in the bit table.
    const bool separateDefault = (blockIfDefault != blockIfFalse);
    assert(!separateDefault || ((blockIfDefault != blockIfTrue)));

    FlowEdge* const trueEdge  = firstBlock->GetTrueEdge();
    FlowEdge* const falseEdge = firstBlock->GetFalseEdge();

    // Convert firstBlock to a switch block
    const unsigned jumpCount = static_cast<unsigned>(maxValue - minValue + 1);
    assert((jumpCount > 0) && (jumpCount <= SWITCH_MAX_DISTANCE + 1));
    FlowEdge** const jmpTab =
        new (this, CMK_FlowEdge) FlowEdge*[3 + jumpCount + 1 /* unique edges | cases | default */];

    const unsigned numSuccs = separateDefault ? 3 : 2;
    firstBlock->SetSwitch(new (this, CMK_BasicBlock) BBswtDesc(jmpTab, numSuccs, jmpTab + 3, jumpCount + 1, true));
    firstBlock->bbCodeOffsEnd = codeOffsEnd;
    firstBlock->lastStmt()->GetRootNode()->ChangeOper(GT_SWITCH);

    firstBlock->lastStmt()->GetRootNode()->AsOp()->gtOp1 = switchValue;
    gtSetStmtInfo(firstBlock->lastStmt());
    fgSetStmtSeq(firstBlock->lastStmt());
    gtUpdateStmtSideEffects(firstBlock->lastStmt());

    // Unlink the old edges of firstBlock, we're going to link the blocks that we still need again in the loop below.
    // Removing them first makes sure that the blocks we're about to remove do not have any references left
    // once all of their predecessors have been removed.
    fgRemoveRefPred(falseEdge);
    fgRemoveRefPred(trueEdge);

    // Remove the whole set of conditional blocks that were folded into the switch
    for (unsigned i = 0; i < blocksToRemoveCnt; i++)
    {
        assert(blocksToRemove[i]->KindIs(BBJ_COND, BBJ_SWITCH));
        fgRemoveBlock(blocksToRemove[i], true);
    }

    fgHasSwitch = true;

    // Splitting doesn't work well with jump-tables currently
    opts.compProcedureSplitting = false;

    FlowEdge*        switchTrueEdge  = nullptr;
    FlowEdge*        switchFalseEdge = nullptr;
    FlowEdge** const cases           = jmpTab + 3;

    for (unsigned i = 0; i < jumpCount; i++)
    {
        // value exists in the testValues array (via bitVector) - 'true' case.
        const bool isTrue = (bitVector & static_cast<ssize_t>(1ULL << i)) != 0;

        FlowEdge* const newEdge = fgAddRefPred((isTrue ? blockIfTrue : blockIfFalse), firstBlock);
        cases[i]                = newEdge;

        FlowEdge** const edgeToRemember = isTrue ? &switchTrueEdge : &switchFalseEdge;
        if (*edgeToRemember == nullptr)
        {
            *edgeToRemember = newEdge;
        }
    }

    assert(switchTrueEdge != nullptr);

    // Link the 'default' case
    FlowEdge* const switchDefaultEdge = fgAddRefPred(blockIfDefault, firstBlock);
    cases[jumpCount]                  = switchDefaultEdge;

    // Fix likelihoods
    switchTrueEdge->setLikelihood(trueLikelihood);
    if (separateDefault)
    {
        // Both the false cases and the default case need to be reachable, see the callers.
        assert(switchFalseEdge != nullptr);
        switchDefaultEdge->setLikelihood(defaultLikelihood);
        switchFalseEdge->setLikelihood(max(0.0, 1.0 - trueLikelihood - defaultLikelihood));
    }
    else
    {
        switchDefaultEdge->setLikelihood(1.0 - trueLikelihood);
    }

    // Initialize unique successor table
    FlowEdge** const uniqueSuccs = jmpTab;
    unsigned         uniqueCount = 0;
    uniqueSuccs[uniqueCount++]   = cases[0];
    for (FlowEdge* const edge : {switchTrueEdge, switchDefaultEdge, switchFalseEdge})
    {
        bool known = (edge == nullptr);
        for (unsigned i = 0; (i < uniqueCount) && !known; i++)
        {
            known = (uniqueSuccs[i] == edge);
        }

        if (!known)
        {
            uniqueSuccs[uniqueCount++] = edge;
        }
    }
    assert(uniqueCount == numSuccs);

    // Update profile. The removed blocks no longer contribute to the weight of the targets and the new edges do.
    if (firstBlock->hasProfileWeight())
    {
        blockIfTrue->setBBProfileWeight(blockIfTrue->computeIncomingWeight());
        blockIfFalse->setBBProfileWeight(blockIfFalse->computeIncomingWeight());

        if ((blockIfTrue->NumSucc() > 0) || (blockIfFalse->NumSucc() > 0))
        {
            JITDUMP("optConvertToBitmapSwitch: Profile needs to be propagated through " FMT_BB "'s successors. Data %s "
                    "inconsistent.\n",
                    firstBlock->bbNum, fgPgoConsistent ? "is now" : "was already");
            fgPgoConsistent = false;
        }
    }
}

// The maximum number of conditional blocks that are considered as a single constant set membership test.
#define RANGE_SET_MAX_TESTS 64

// The maximum number of intervals that the values of the variable are split into.
#define RANGE_SET_MAX_INTERVALS 512

// The maximum number of jump table entries of a switch block that is part of a constant set membership test.
#define RANGE_SET_MAX_SWITCH_CASES 64

//------------------------------------------------------------------------------
// RangeSetTest : A block that tests a local against constants and then jumps to one of its successors. It is either
//    a conditional block of the form JTRUE(relop(X + offset, cns)) or a switch block of the form SWITCH(X + offset).
//
struct RangeSetTest
{
    BasicBlock* block;        // The block that contains the test
    GenTree*    valueNode;    // The tested local (can be wrapped in a COMMA in the first block), without the offset
    bool        isSwitch;     // Is this a switch block (otherwise it's a conditional block)?
    genTreeOps  oper;         // Conditional blocks: EQ, NE, LT, LE, GT or GE; the constant is the second operand
    bool        isUnsigned;   // Conditional blocks: is it an unsigned comparison?
    bool        is64Bit;      // Is the tested variable TYP_LONG (otherwise TYP_INT)?
    int64_t     offset;       // Value that is added to X before the comparison
    int64_t     cns;          // Conditional blocks: the constant that is tested against
    unsigned    succCount;    // The number of successors. For conditional blocks: true, false. For switch blocks: the
                              // jump table entries, the last one is the default case.
    BasicBlock** switchSuccs; // Switch blocks: the successors (skipping empty blocks)
    weight_t*    switchLikelihoods;  // Switch blocks: the likelihood of every successor
    BasicBlock*  condSuccs[2];       // Conditional blocks: the successors (skipping empty blocks)
    weight_t     condLikelihoods[2]; // Conditional blocks: the likelihood of every successor

    BasicBlock* Succ(unsigned index) const
    {
        return isSwitch ? switchSuccs[index] : condSuccs[index];
    }

    weight_t Likelihood(unsigned index) const
    {
        return isSwitch ? switchLikelihoods[index] : condLikelihoods[index];
    }

    //------------------------------------------------------------------------------
    // Evaluate: Compute which successor is taken for a concrete value of the variable.
    //
    unsigned Evaluate(int64_t x) const
    {
        if (isSwitch)
        {
            const uint32_t index = static_cast<uint32_t>(x) + static_cast<uint32_t>(offset);
            return (index < (succCount - 1)) ? index : (succCount - 1);
        }

        return (is64Bit ? EvaluateAs<int64_t, uint64_t>(x) : EvaluateAs<int32_t, uint32_t>(x)) ? 0 : 1;
    }

private:
    template <typename TSigned, typename TUnsigned>
    bool EvaluateAs(int64_t x) const
    {
        const TSigned v = static_cast<TSigned>(static_cast<TUnsigned>(x) + static_cast<TUnsigned>(offset));
        const TSigned c = static_cast<TSigned>(cns);
        if (isUnsigned)
        {
            return Compare(static_cast<TUnsigned>(v), static_cast<TUnsigned>(c));
        }
        return Compare(v, c);
    }

    template <typename T>
    bool Compare(T a, T b) const
    {
        switch (oper)
        {
            case GT_EQ:
                return a == b;
            case GT_NE:
                return a != b;
            case GT_LT:
                return a < b;
            case GT_LE:
                return a <= b;
            case GT_GT:
                return a > b;
            default:
                assert(oper == GT_GE);
                return a >= b;
        }
    }
};

//------------------------------------------------------------------------------
// ParseRangeSetTestBlock : Does the given block represent a constant test, i.e. JTRUE(relop(X [+/- cns], cns))
//    or SWITCH(X [+/- cns]) where X is a TYP_INT (or TYP_LONG for conditional blocks on 64 bit targets) local?
//
// Arguments:
//    comp    - The compiler instance.
//    block   - The block to check
//    isFirst - Is this the first block of the whole test? Only the first block can have more than one
//              statement and have side effects (via COMMA) in the tested value.
//    test    - [out] The parsed test
//
// Return Value:
//    True if the block represents a constant test, false otherwise
//
static bool ParseRangeSetTestBlock(Compiler* comp, BasicBlock* block, bool isFirst, RangeSetTest* test)
{
    if (!block->KindIs(BBJ_COND, BBJ_SWITCH) || block->HasFlag(BBF_DONT_REMOVE) || (block->lastStmt() == nullptr))
    {
        return false;
    }

    if (!isFirst && !block->hasSingleStmt())
    {
        return false;
    }

    GenTree* const rootNode = block->lastStmt()->GetRootNode();

    // Parses "X", "X + CNS" and "X - CNS" (the latter two are what the range check folding produces).
    auto parseValue = [&](GenTree* node, var_types type, int64_t* offset, GenTree** value) -> bool {
        *offset = 0;
        *value  = node;
        if (node->OperIs(GT_ADD, GT_SUB) && !node->gtOverflow() && node->gtGetOp2()->IsCnsIntOrI() &&
            !node->gtGetOp2()->IsIconHandle() && node->gtGetOp2()->TypeIs(type))
        {
            const int64_t cns = static_cast<int64_t>(node->gtGetOp2()->AsIntCon()->IconValue());
            *offset           = node->OperIs(GT_ADD) ? cns : static_cast<int64_t>(0ULL - static_cast<uint64_t>(cns));
            *value            = node->gtGetOp1();
        }

        GenTree* const local = isFirst ? (*value)->gtEffectiveVal() : *value;
        return local->OperIs(GT_LCL_VAR) && local->TypeIs(type);
    };

    if (block->KindIs(BBJ_SWITCH))
    {
        BBswtDesc* const desc = block->GetSwitchTargets();
        if (!desc->HasDefaultCase() || (desc->GetCaseCount() > RANGE_SET_MAX_SWITCH_CASES) ||
            !rootNode->OperIs(GT_SWITCH) || !rootNode->gtGetOp1()->TypeIs(TYP_INT))
        {
            return false;
        }

        if (!parseValue(rootNode->gtGetOp1(), TYP_INT, &test->offset, &test->valueNode))
        {
            return false;
        }

        test->block             = block;
        test->isSwitch          = true;
        test->oper              = GT_NONE;
        test->isUnsigned        = false;
        test->is64Bit           = false;
        test->cns               = 0;
        test->succCount         = desc->GetCaseCount();
        test->switchSuccs       = new (comp, CMK_Generic) BasicBlock*[test->succCount];
        test->switchLikelihoods = new (comp, CMK_Generic) weight_t[test->succCount];
        for (unsigned i = 0; i < test->succCount; i++)
        {
            FlowEdge* const edge       = desc->GetCase(i);
            test->switchSuccs[i]       = SkipFallthroughBlocks(comp, edge->getDestinationBlock());
            test->switchLikelihoods[i] = edge->getLikelihood() / edge->getDupCount();
        }
        return true;
    }

    if (block->FalseTargetIs(block) || block->TrueTargetIs(block))
    {
        return false;
    }

    assert(rootNode->OperIs(GT_JTRUE));

    GenTree* const cond = rootNode->gtGetOp1();
    if (!cond->OperIs(GT_EQ, GT_NE, GT_LT, GT_LE, GT_GT, GT_GE))
    {
        return false;
    }

    GenTree*   op1  = cond->gtGetOp1();
    GenTree*   op2  = cond->gtGetOp2();
    genTreeOps oper = cond->OperGet();

    const var_types type = op1->TypeGet();
    if (!op2->TypeIs(type) || !((type == TYP_INT) || (TARGET_POINTER_SIZE == 8 && (type == TYP_LONG))))
    {
        return false;
    }

    if (op1->IsCnsIntOrI())
    {
        std::swap(op1, op2);
        oper = GenTree::SwapRelop(oper);
    }

    if (!op2->IsCnsIntOrI() || op2->IsIconHandle() || op1->IsCnsIntOrI())
    {
        return false;
    }

    if (!parseValue(op1, type, &test->offset, &test->valueNode))
    {
        return false;
    }

    test->block              = block;
    test->isSwitch           = false;
    test->oper               = oper;
    test->isUnsigned         = cond->IsUnsigned();
    test->is64Bit            = (type == TYP_LONG);
    test->cns                = static_cast<int64_t>(op2->AsIntCon()->IconValue());
    test->succCount          = 2;
    test->condSuccs[0]       = SkipFallthroughBlocks(comp, block->GetTrueTarget());
    test->condSuccs[1]       = SkipFallthroughBlocks(comp, block->GetFalseTarget());
    test->condLikelihoods[0] = block->GetTrueEdge()->getLikelihood();
    test->condLikelihoods[1] = block->GetFalseEdge()->getLikelihood();
    return true;
}

//------------------------------------------------------------------------------
// optSwitchConvertRangeTests : Try to convert a tree of conditional (and switch) blocks that tests the same local
//    against constants (using any relational operator) and that only has two possible outcomes into a bitmap test,
//    e.g.
//
//      c is >= '0' and <= '9' or >= 'A' and <= 'Z'
//
//    which the C# compiler emits as a decision tree such as:
//
//      if (c < 'A') { return c >= '0' && c <= '9'; } else { return c <= 'Z'; }
//
//    Neither the range check folding nor the "x == cns1 || x == cns2" switch recognition can handle these.
//    Because there are only two outcomes and all tests are on the same value, we can compute which values of the
//    variable lead to which outcome. If one of the outcomes is only reachable for a small window of values
//    (up to the number of bits in a pointer), we convert the tree into a switch that only has two distinct
//    targets, which then is lowered into a range check and a bit test.
//
// Notes:
//    Only tests that directly use the variable (optionally with a constant offset) are recognized. For a switch on
//    a long Roslyn first stores "tmp = x - c" in a local and then tests/switches on that temp (using a truncating
//    cast for the switch). Those trees are not recognized: supporting them requires rewriting the uses of the temp
//    and removing its definition, which is not worth the extra complexity.
//
// Arguments:
//    firstBlock - The root of the decision tree.
//
// Return Value:
//    True if the conversion was successful, false otherwise
//
bool Compiler::optSwitchConvertRangeTests(BasicBlock* firstBlock)
{
    assert(firstBlock->KindIs(BBJ_COND));

#ifdef TARGET_ARM
    // Bitmap test (TryLowerSwitchToBitTest) is quite verbose on ARM32 (~6 instructions), it results in massive size
    // regressions.
    return false;
#endif

    // Cheap check first, this runs for every conditional block: a set of at least SWITCH_MIN_TESTS tests needs
    // another test after the first block.
    auto mayBeTest = [](const BasicBlock* block) {
        return block->KindIs(BBJ_COND, BBJ_SWITCH) || (block->KindIs(BBJ_ALWAYS) && block->isEmpty());
    };

    if (!mayBeTest(firstBlock->GetTrueTarget()) && !mayBeTest(firstBlock->GetFalseTarget()))
    {
        return false;
    }

    RangeSetTest tests[RANGE_SET_MAX_TESTS];
    unsigned     testCount = 0;

    if (!ParseRangeSetTestBlock(this, firstBlock, /* isFirst */ true, &tests[0]) || tests[0].isSwitch)
    {
        return false;
    }
    testCount = 1;

    GenTree* const variable = tests[0].valueNode->gtEffectiveVal();

    auto findTest = [&](const BasicBlock* block) -> int {
        for (unsigned i = 0; i < testCount; i++)
        {
            if (tests[i].block == block)
            {
                return static_cast<int>(i);
            }
        }
        return -1;
    };

    // Collect all blocks that can be reached from the first block through constant tests of the same variable.
    // Blocks that are not such a test are an outcome of the whole test. There can only be two of them, so give up
    // as soon as a third one is found. This keeps the cost low for the (common) large chains of tests with many
    // different targets.
    BasicBlock* knownOutcomes[2]  = {nullptr, nullptr};
    unsigned    knownOutcomeCount = 0;

    for (unsigned i = 0; i < testCount; i++)
    {
        for (unsigned s = 0; s < tests[i].succCount; s++)
        {
            BasicBlock* const succ = tests[i].Succ(s);
            if (succ == firstBlock)
            {
                // A loop
                return false;
            }

            if (findTest(succ) >= 0)
            {
                continue;
            }

            if (testCount < RANGE_SET_MAX_TESTS)
            {
                RangeSetTest test;
                if (BasicBlock::sameEHRegion(firstBlock, succ) &&
                    ParseRangeSetTestBlock(this, succ, /* isFirst */ false, &test) &&
                    (test.is64Bit == tests[0].is64Bit) && GenTree::Compare(test.valueNode, variable))
                {
                    tests[testCount++] = test;
                    continue;
                }
            }

            if ((succ != knownOutcomes[0]) && (succ != knownOutcomes[1]))
            {
                if (knownOutcomeCount == 2)
                {
                    return false;
                }
                knownOutcomes[knownOutcomeCount++] = succ;
            }
        }
    }

    // We're going to remove all the blocks except the first one, so all their predecessors have to be removed too.
    // Blocks that fail this check are treated as the outcome of the test instead.
    bool changed;
    do
    {
        changed = false;
        for (unsigned i = 1; i < testCount; i++)
        {
            bool onlyTestPreds = true;
            for (BasicBlock* const pred : tests[i].block->PredBlocks())
            {
                if (findTest(pred) < 0)
                {
                    onlyTestPreds = false;
                    break;
                }
            }

            if (!onlyTestPreds)
            {
                tests[i] = tests[--testCount];
                changed  = true;
                break;
            }
        }
    } while (changed);

    if (testCount < SWITCH_MIN_TESTS)
    {
        return false;
    }

    // Now find the (only) two outcomes of the tests. For every successor of a test remember where it goes:
    // either to another test (index >= 0) or to one of the outcomes (-1 or -2).
    BasicBlock* outcomes[2] = {nullptr, nullptr};
    int*        next[RANGE_SET_MAX_TESTS];
    unsigned    inDegree[RANGE_SET_MAX_TESTS] = {};

    for (unsigned i = 0; i < testCount; i++)
    {
        next[i] = new (this, CMK_Generic) int[tests[i].succCount];
        for (unsigned s = 0; s < tests[i].succCount; s++)
        {
            BasicBlock* const succ  = tests[i].Succ(s);
            const int         index = findTest(succ);
            if (index >= 0)
            {
                next[i][s] = index;
                inDegree[index]++;
                continue;
            }

            if (outcomes[0] == nullptr)
            {
                outcomes[0] = succ;
            }
            else if ((outcomes[1] == nullptr) && (outcomes[0] != succ))
            {
                outcomes[1] = succ;
            }

            if (succ == outcomes[0])
            {
                next[i][s] = -1;
            }
            else if (succ == outcomes[1])
            {
                next[i][s] = -2;
            }
            else
            {
                // More than two outcomes
                return false;
            }
        }
    }

    if ((outcomes[1] == nullptr) || (inDegree[0] != 0))
    {
        return false;
    }

    // Order the tests so that every test comes after all the tests that can jump to it (and make sure there
    // are no cycles).
    unsigned order[RANGE_SET_MAX_TESTS];
    unsigned orderCount = 0;
    order[orderCount++] = 0;
    for (unsigned i = 0; i < orderCount; i++)
    {
        for (unsigned s = 0; s < tests[order[i]].succCount; s++)
        {
            const int index = next[order[i]][s];
            if ((index >= 0) && (--inDegree[index] == 0))
            {
                order[orderCount++] = static_cast<unsigned>(index);
            }
        }
    }

    if (orderCount != testCount)
    {
        return false;
    }

    // Split the range of all the possible values of the variable into intervals in which the result of every test
    // is constant. Every test can only change its result when the (offset) variable is equal to the constant or the
    // constant plus one, or at the places where the comparison wraps around. For switch blocks these are the
    // values that select a jump table entry.
    const bool    is64Bit   = tests[0].is64Bit;
    const int64_t domainMin = is64Bit ? INT64_MIN : INT32_MIN;
    int64_t       starts[RANGE_SET_MAX_INTERVALS];
    unsigned      intervalCount = 0;
    starts[intervalCount++]     = domainMin;

    for (unsigned i = 0; i < testCount; i++)
    {
        uint64_t changePoints[RANGE_SET_MAX_SWITCH_CASES + 2];
        unsigned changePointCount = 0;
        if (tests[i].isSwitch)
        {
            for (unsigned c = 0; c < tests[i].succCount; c++)
            {
                changePoints[changePointCount++] = c;
            }
            changePoints[changePointCount++] = static_cast<uint64_t>(domainMin);
        }
        else
        {
            const uint64_t cns               = static_cast<uint64_t>(tests[i].cns);
            changePoints[changePointCount++] = cns;
            changePoints[changePointCount++] = cns + 1;
            changePoints[changePointCount++] = 0;
            changePoints[changePointCount++] = static_cast<uint64_t>(domainMin);
        }

        if ((intervalCount + changePointCount) > RANGE_SET_MAX_INTERVALS)
        {
            return false;
        }

        for (unsigned p = 0; p < changePointCount; p++)
        {
            const uint64_t x = changePoints[p] - static_cast<uint64_t>(tests[i].offset);
            starts[intervalCount++] =
                is64Bit ? static_cast<int64_t>(x) : static_cast<int32_t>(static_cast<uint32_t>(x));
        }
    }

    for (unsigned i = 1; i < intervalCount; i++)
    {
        const int64_t value = starts[i];
        unsigned      j     = i;
        for (; (j > 0) && (starts[j - 1] > value); j--)
        {
            starts[j] = starts[j - 1];
        }
        starts[j] = value;
    }

    unsigned uniqueCount = 1;
    for (unsigned i = 1; i < intervalCount; i++)
    {
        if (starts[i] != starts[uniqueCount - 1])
        {
            starts[uniqueCount++] = starts[i];
        }
    }
    intervalCount = uniqueCount;

    // Find the outcome of every interval by evaluating the tests with the first value of the interval
    int8_t outcomeOfInterval[RANGE_SET_MAX_INTERVALS];
    for (unsigned i = 0; i < intervalCount; i++)
    {
        const int64_t x       = starts[i];
        unsigned      current = 0;
        int           outcome = 0;
        for (unsigned steps = 0;; steps++)
        {
            if (steps > testCount)
            {
                return false;
            }

            const int target = next[current][tests[current].Evaluate(x)];
            if (target < 0)
            {
                outcome = -target - 1;
                break;
            }
            current = static_cast<unsigned>(target);
        }
        outcomeOfInterval[i] = static_cast<int8_t>(outcome);
    }

    // The outcome that is reachable for a small window of values is the one that we can test with a bitmap.
    // Only one of the outcomes can have this property because the other one has to contain all the other values.
    int bitmapOutcome = -1;
    for (int o = 0; o < 2; o++)
    {
        if ((outcomeOfInterval[0] != o) && (outcomeOfInterval[intervalCount - 1] != o))
        {
            bitmapOutcome = o;
            break;
        }
    }

    if (bitmapOutcome < 0)
    {
        return false;
    }

    int64_t  minValue = INT64_MAX;
    int64_t  maxValue = INT64_MIN;
    unsigned runCount = 0;
    for (unsigned i = 0; i < intervalCount; i++)
    {
        if (outcomeOfInterval[i] != bitmapOutcome)
        {
            continue;
        }

        minValue = min(minValue, starts[i]);
        maxValue = max(maxValue, starts[i + 1] - 1); // This is never the last interval, see above.

        if ((i == 0) || (outcomeOfInterval[i - 1] != bitmapOutcome))
        {
            runCount++;
        }
    }

    if (runCount == 0)
    {
        // The test is always false
        return false;
    }

    if (runCount < 2)
    {
        // A single range, leave it for the range check folding.
        return false;
    }

    const uint64_t span = static_cast<uint64_t>(maxValue) - static_cast<uint64_t>(minValue);
    if (span > SWITCH_MAX_WIDE_DISTANCE)
    {
        return false;
    }

    // A set that does not fit in a single bit table is tested with a bit test for the lowest values, whose default
    // case jumps to a range check for the remaining values (see below).
    const bool wide = (span > SWITCH_MAX_DISTANCE);

    // The bit test replaces one compare per single value and two per range, so it only pays off if the compares are
    // expensive enough. The wide variant needs an extra range check, and ARM64 needs up to four instructions to
    // materialize the bit table and can already test a chain of ranges efficiently with conditional compares.
    unsigned compareCost = 0;
    for (unsigned i = 0; i < intervalCount; i++)
    {
        if ((outcomeOfInterval[i] == bitmapOutcome) && ((i == 0) || (outcomeOfInterval[i - 1] != bitmapOutcome)))
        {
            unsigned end = i;
            while (((end + 1) < intervalCount) && (outcomeOfInterval[end + 1] == bitmapOutcome))
            {
                end++;
            }
            compareCost += ((starts[end + 1] - starts[i]) == 1) ? 1 : 2;
        }
    }

#ifdef TARGET_ARM64
    const unsigned minCompareCost = 7;
#else
    const unsigned minCompareCost = wide ? 6 : 0;
#endif
    if (compareCost < minCompareCost)
    {
        return false;
    }

    // Likelihood of reaching the other outcome (the default case of the switch)
    weight_t reachDefault[RANGE_SET_MAX_TESTS];
    for (unsigned i = testCount; i-- > 0;)
    {
        const unsigned t   = order[i];
        weight_t       sum = 0;
        for (unsigned s = 0; s < tests[t].succCount; s++)
        {
            const int      n = next[t][s];
            const weight_t p = (n >= 0) ? reachDefault[n] : (((-n - 1) != bitmapOutcome) ? 1.0 : 0.0);
            sum += tests[t].Likelihood(s) * p;
        }
        reachDefault[t] = sum;
    }

    BasicBlock* const blockIfTrue  = outcomes[bitmapOutcome];
    BasicBlock* const blockIfFalse = outcomes[1 - bitmapOutcome];

    JITDUMP("Converting %u conditional blocks starting at " FMT_BB " to a bitmap test of values [%lld..%lld]\n",
            testCount, firstBlock->bbNum, static_cast<long long>(minValue), static_cast<long long>(maxValue));

    // Normalize the switch so that it starts at zero if needed
    const var_types type = is64Bit ? TYP_LONG : TYP_INT;
    if (!wide && (minValue >= 0) && (maxValue <= SWITCH_MAX_DISTANCE))
    {
        minValue = 0;
    }

    // The bit tables, the second one is only used for sets that do not fit in a single bit table.
    const unsigned bitsPerTable = SWITCH_MAX_DISTANCE + 1;
    size_t         bitTables[2] = {0, 0};
    for (unsigned i = 0; i < intervalCount; i++)
    {
        if (outcomeOfInterval[i] != bitmapOutcome)
        {
            continue;
        }

        for (int64_t v = starts[i]; v < starts[i + 1]; v++)
        {
            const unsigned bit = static_cast<unsigned>(v - minValue);
            bitTables[bit / bitsPerTable] |= static_cast<size_t>(1) << (bit % bitsPerTable);
        }
    }

    // A set that does not fit in a single bit table is only converted if the values that do not fit are one range
    // (e.g. "[a-zA-Z0-9_]"). That is cheaper than a second bit test, which is not a win over the compares.
    const unsigned highBits = wide ? (static_cast<unsigned>(span - bitsPerTable) + 1) : 0;
    if (wide)
    {
        const size_t highMask = (highBits == bitsPerTable) ? SIZE_MAX : ((static_cast<size_t>(1) << highBits) - 1);

        // The first table has to contain both outcomes (like a bit table in a regular switch).
        if ((bitTables[1] != highMask) || (bitTables[0] == 0) || (bitTables[0] == SIZE_MAX))
        {
            return false;
        }
    }

    // From here on we're going to convert the tests, so it is fine to create new nodes.
    GenTree* switchValue = tests[0].valueNode;
    if (minValue != 0)
    {
        switchValue = gtNewOperNode(GT_ADD, type, switchValue, gtNewIconNode(static_cast<ssize_t>(-minValue), type));
    }

    BasicBlock* blocksToRemove[RANGE_SET_MAX_TESTS];
    unsigned    removeCount = 0;
    IL_OFFSET   codeOffsEnd = firstBlock->bbCodeOffsEnd;
    for (unsigned i = 0; i < testCount; i++)
    {
        const BasicBlock* const block = tests[order[i]].block;
        if ((block->bbCodeOffsEnd != BAD_IL_OFFSET) &&
            ((codeOffsEnd == BAD_IL_OFFSET) || (block->bbCodeOffsEnd > codeOffsEnd)))
        {
            codeOffsEnd = block->bbCodeOffsEnd;
        }

        if (order[i] != 0)
        {
            blocksToRemove[removeCount++] = tests[order[i]].block;
        }
    }

    if (!wide)
    {
        optConvertToBitmapSwitch(firstBlock, switchValue, static_cast<ssize_t>(minValue),
                                 static_cast<ssize_t>(maxValue), static_cast<ssize_t>(bitTables[0]), blockIfTrue,
                                 blockIfFalse, blockIfFalse, 1.0 - reachDefault[0], reachDefault[0], codeOffsEnd,
                                 blocksToRemove, removeCount);
        return true;
    }

    // Create a second block that tests the values that do not fit in the bit table with a range check. It is the
    // default case of the switch, so it also sees the values that are below the first table. These wrap around
    // in the (unsigned) range check and end up as not in the set.
    BasicBlock* const secondBlock = fgNewBBafter(BBJ_ALWAYS, firstBlock, /* extendRegion */ true);
    secondBlock->bbCodeOffs       = codeOffsEnd;
    secondBlock->bbCodeOffsEnd    = codeOffsEnd;
    secondBlock->CopyFlags(firstBlock, BBF_COPY_PROPAGATE);

    FlowEdge* const secondTrueEdge  = fgAddRefPred(blockIfTrue, secondBlock);
    FlowEdge* const secondFalseEdge = fgAddRefPred(blockIfFalse, secondBlock);
    secondBlock->SetCond(secondTrueEdge, secondFalseEdge);

    GenTree* const secondValue     = gtNewOperNode(GT_ADD, type, gtCloneExpr(variable),
                                                   gtNewIconNode(-static_cast<ssize_t>(minValue + bitsPerTable), type));
    GenTree* const secondCondition = gtNewOperNode(GT_LE, TYP_INT, secondValue, gtNewIconNode(highBits - 1, type));
    secondCondition->SetUnsigned();
    secondCondition->gtFlags |= GTF_RELOP_JMP_USED | GTF_DONT_CSE;
    GenTree* const secondJump = gtNewOperNode(GT_JTRUE, TYP_VOID, secondCondition);
    fgInsertStmtAtEnd(secondBlock, fgNewStmtFromTree(secondJump, firstBlock->lastStmt()->GetDebugInfo()));

    // Estimate the likelihoods: the true values are spread over the bit table and the range, and the false values
    // are assumed to be evenly spread over "outside of the bit table" (the second block) and "inside it".
    unsigned lowCount = 0;
    for (unsigned bit = 0; bit < bitsPerTable; bit++)
    {
        lowCount += ((bitTables[0] >> bit) & 1);
    }

    const weight_t trueLikelihood   = 1.0 - reachDefault[0];
    const weight_t highShare        = static_cast<weight_t>(highBits) / static_cast<weight_t>(lowCount + highBits);
    const weight_t firstTrue        = trueLikelihood * (1.0 - highShare);
    const weight_t firstDefault     = (trueLikelihood * highShare) + (reachDefault[0] / 2);
    const weight_t secondTrueChance = (firstDefault > 0) ? ((trueLikelihood * highShare) / firstDefault) : 0.5;

    secondBlock->inheritWeight(firstBlock);
    secondBlock->scaleBBWeight(firstDefault);
    secondTrueEdge->setLikelihood(secondTrueChance);
    secondFalseEdge->setLikelihood(1.0 - secondTrueChance);
    gtSetStmtInfo(secondBlock->lastStmt());
    fgSetStmtSeq(secondBlock->lastStmt());
    gtUpdateStmtSideEffects(secondBlock->lastStmt());

    optConvertToBitmapSwitch(firstBlock, switchValue, 0, SWITCH_MAX_DISTANCE, static_cast<ssize_t>(bitTables[0]),
                             blockIfTrue, blockIfFalse, secondBlock, firstTrue, firstDefault, codeOffsEnd,
                             blocksToRemove, removeCount);
    return true;
}
