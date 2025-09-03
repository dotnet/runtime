// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// We mainly rely on TryLowerSwitchToBitTest in these heuristics, but jump tables can be useful
// even without conversion to a bitmap test.
#define SWITCH_MAX_DISTANCE ((TARGET_POINTER_SIZE * BITS_PER_BYTE) - 1)

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
        if (block->KindIs(BBJ_COND) && optSwitchDetectAndConvert(block))
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

    BitVecTraits traits(comp->fgBBNumMax + 1, comp);
    BitVec       visited = BitVecOps::MakeEmpty(&traits);
    BitVecOps::AddElemD(&traits, visited, block->bbNum);
    while (block->KindIs(BBJ_ALWAYS) && (block->firstStmt() == nullptr) &&
           BasicBlock::sameEHRegion(block, block->GetTarget()))
    {
        block = block->GetTarget();
        if (BitVecOps::IsMember(&traits, visited, block->bbNum))
        {
            // A cycle detected, bail out.
            return origBlock;
        }
        BitVecOps::AddElemD(&traits, visited, block->bbNum);
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
//    nodeToTest - Variable node that is tested against the constants
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
                                GenTree*    nodeToTest,
                                bool        testingForConversion,
                                BitVec*     ccmpVec)
{
    assert(firstBlock->KindIs(BBJ_COND));
    assert(!varTypeIsSmall(nodeToTest));

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

    // if MaxValue is less than SWITCH_MAX_DISTANCE then don't bother with SUB(val, minValue)
    if (maxValue <= SWITCH_MAX_DISTANCE)
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
    FlowEdge* const trueEdge  = firstBlock->GetTrueEdge();
    FlowEdge* const falseEdge = firstBlock->GetFalseEdge();

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
    // Convert firstBlock to a switch block
    const unsigned jumpCount = static_cast<unsigned>(maxValue - minValue + 1);
    assert((jumpCount > 0) && (jumpCount <= SWITCH_MAX_DISTANCE + 1));
    FlowEdge** const jmpTab =
        new (this, CMK_FlowEdge) FlowEdge*[2 + jumpCount + 1 /* true/false edges | cases | default case */];

    firstBlock->SetSwitch(new (this, CMK_BasicBlock) BBswtDesc(jmpTab, 2, jmpTab + 2, jumpCount + 1, true));
    firstBlock->bbCodeOffsEnd = lastBlock->bbCodeOffsEnd;
    firstBlock->lastStmt()->GetRootNode()->ChangeOper(GT_SWITCH);

    // The root node is now SUB(nodeToTest, minValue) if minValue != 0
    GenTree* switchValue = nodeToTest;
    if (minValue != 0)
    {
        switchValue =
            gtNewOperNode(GT_SUB, nodeToTest->TypeGet(), switchValue, gtNewIconNode(minValue, nodeToTest->TypeGet()));
    }

    firstBlock->lastStmt()->GetRootNode()->AsOp()->gtOp1 = switchValue;
    gtSetStmtInfo(firstBlock->lastStmt());
    fgSetStmtSeq(firstBlock->lastStmt());
    gtUpdateStmtSideEffects(firstBlock->lastStmt());

    // Unlink and remove the whole chain of conditional blocks
    fgRemoveRefPred(falseEdge);
    BasicBlock* blockToRemove = falseEdge->getDestinationBlock();
    for (int i = 0; i < (testsCount - 1); i++)
    {
        // We always follow the false target because reversed tests are only supported for the last block.
        assert(blockToRemove->KindIs(BBJ_COND));
        BasicBlock* const nextBlockToRemove = blockToRemove->GetFalseTarget();
        fgRemoveBlock(blockToRemove, true);
        blockToRemove = nextBlockToRemove;
    }

    fgHasSwitch = true;

    // Splitting doesn't work well with jump-tables currently
    opts.compProcedureSplitting = false;

    // Compose a bit vector of all the values we have in the testValues array
    // to quickly check if a value is in the array
    ssize_t bitVector = 0;
    for (testIdx = 0; testIdx < testsCount; testIdx++)
    {
        assert(testIdx <= (int)((sizeof(ssize_t) * BITS_PER_BYTE) - 1));
        bitVector |= (ssize_t)(1ULL << static_cast<unsigned>((testValues[testIdx] - minValue)));
    }

    // Unlink blockIfTrue from firstBlock, we're going to link it again in the loop below.
    fgRemoveRefPred(trueEdge);

    FlowEdge*        switchTrueEdge = nullptr;
    FlowEdge** const cases          = jmpTab + 2;

    for (unsigned i = 0; i < jumpCount; i++)
    {
        // value exists in the testValues array (via bitVector) - 'true' case.
        const bool isTrue = (bitVector & static_cast<ssize_t>(1ULL << i)) != 0;

        FlowEdge* const newEdge = fgAddRefPred((isTrue ? blockIfTrue : blockIfFalse), firstBlock);
        cases[i]                = newEdge;

        if ((switchTrueEdge == nullptr) && isTrue)
        {
            switchTrueEdge = newEdge;
        }
    }

    assert(switchTrueEdge != nullptr);

    // Link the 'default' case
    FlowEdge* const switchDefaultEdge = fgAddRefPred(blockIfFalse, firstBlock);
    cases[jumpCount]                  = switchDefaultEdge;

    // Fix likelihoods
    switchDefaultEdge->setLikelihood(falseLikelihood);
    switchTrueEdge->setLikelihood(1.0 - falseLikelihood);

    // Initialize unique successor table
    firstBlock->GetSwitchTargets()->GetSuccs()[0] = cases[0];
    firstBlock->GetSwitchTargets()->GetSuccs()[1] = (cases[0] == switchTrueEdge) ? switchDefaultEdge : switchTrueEdge;

    return true;
}
