// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// We mainly rely on TryLowerSwitchToBitTest in these heuristics, but jump tables can be useful
// even without conversion to a bitmap test.
#define SWITCH_MAX_DISTANCE ((TARGET_POINTER_SIZE * BITS_PER_BYTE) - 1)
#define SWITCH_MIN_TESTS    3

//-----------------------------------------------------------------------------
//  optSwitchRecognition: Optimize range check for `x == cns1 || x == cns2 || x == cns3 ...`
//      pattern and convert it to Switch block (jump table) which is then *might* be converted
//      to a bitmap test via TryLowerSwitchToBitTest.
//      TODO: recognize general jump table patterns.
//
//  Return Value:
//      MODIFIED_EVERYTHING if the optimization was applied.
//
PhaseStatus Compiler::optSwitchRecognition()
{
    bool modified = false;
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->Next())
    {
#ifdef TARGET_XARCH
        // Limit to XARCH, ARM is already doing a great job with such comparisons using
        // a series of ccmp instruction (see ifConvert phase).
        if (block->KindIs(BBJ_COND) && !block->isRunRarely() && optSwitchDetectAndConvert(block))
        {
            JITDUMP("Converted block " FMT_BB " to switch\n", block->bbNum)
            modified = true;
        }
#endif

        // See if we can merge BBJ_COND into an existing switch block
        while (block->KindIs(BBJ_SWITCH) && optExtendSwitch(block))
        {
            modified = true;
        }
    }

    if (modified)
    {
        // Remove unreachable blocks
        fgUpdateFlowGraph();
        fgRenumberBlocks();
        return PhaseStatus::MODIFIED_EVERYTHING;
    }
    return PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------------
// IsConstantTestCondBlock : Does the given block represent a simple BBJ_COND
//    constant test? e.g. JTRUE(EQ/NE(X, CNS)).
//
// Arguments:
//    block            - The block to check
//    allowSideEffects - is variableNode allowed to have side-effects (COMMA)?
//    trueEdge         - [out] The successor edge taken if X == CNS
//    falseEdge        - [out] The successor edge taken if X != CNS
//    isReversed       - [out] True if the condition is reversed (GT_NE)
//    variableNode     - [out] The variable node (X in the example above)
//    cns              - [out] The constant value (CNS in the example above)
//
// Return Value:
//    True if the block represents a constant test, false otherwise
//
bool IsConstantTestCondBlock(const BasicBlock* block,
                             bool              allowSideEffects,
                             FlowEdge**        trueEdge,
                             FlowEdge**        falseEdge,
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
                *trueEdge   = *isReversed ? block->GetFalseEdge() : block->GetTrueEdge();
                *falseEdge  = *isReversed ? block->GetTrueEdge() : block->GetFalseEdge();

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
// GetSwitchValueOp: Try to extract BBJ_SWITCH's variable (and the offset)
//
// Arguments:
//    switchBlock  - BBJ_SWITCH block to get the switch value from
//    variableNode - [out] Switch value node
//    offset       - [out] Offset of the switch value
//
// Return Value:
//    True if switch value was successfully extracted, false otherwise
//
static bool GetSwitchValueOp(const BasicBlock* switchBlock, GenTree** variableNode, ssize_t* offset)
{
    assert(switchBlock->KindIs(BBJ_SWITCH));

    GenTree* switchNode = switchBlock->lastStmt()->GetRootNode()->gtEffectiveVal();
    assert(switchNode->OperIs(GT_SWITCH));

    GenTree* switchOp = switchNode->gtGetOp1();

    // In most cases the switch value is either ADD(LCL_VAR, CNS) or LCL_VAR
    if (switchOp->OperIs(GT_ADD))
    {
        // We use gtEffectiveVal() to skip COMMA nodes, those will be evaluated before
        // any other case node in the switch block anyway.
        GenTree* op1 = switchOp->gtGetOp1()->gtEffectiveVal();
        GenTree* op2 = switchOp->gtGetOp2();

        // TODO-JumpTable: We should be able to accept any side-effect-free op1 here
        if (op1->OperIs(GT_LCL_VAR) && op2->IsCnsIntOrI())
        {
            *variableNode = op1;
            *offset       = op2->AsIntCon()->IconValue();
            return true;
        }
    }
    else
    {
        // We use gtEffectiveVal() to skip COMMA nodes, those will be evaluated before
        // any other case node in the switch block anyway.
        GenTree* op1 = switchOp->gtEffectiveVal();
        if (op1->OperIs(GT_LCL_VAR))
        {
            *variableNode = op1;
            *offset       = 0;
            return true;
        }
    }
    return false;
}

//------------------------------------------------------------------------------
// optExtendSwitch: If one of the switch's targets is a value test block,
//    and the switch's variable is the same as the value test's variable - try to merge
//    that test (BBJ_COND) into the switch block. It's useful to assist Roslyn, which
//    uses fairly conservative heuristics to generate switch blocks and may give up on
//    some tests that could be merged into the switch.
//
// Arguments:
//    block - BBJ_SWITCH block to extend
//
// Return Value:
//    True if switch was successfully extended, false otherwise
//
bool Compiler::optExtendSwitch(BasicBlock* block)
{
    assert(block->KindIs(BBJ_SWITCH));
    JITDUMP("Considering expanding switch " FMT_BB "\n", block->bbNum);

    if (opts.IsInstrumented())
    {
        JITDUMP("Instrumentation is enabled - bail out.\n");
        return false;
    }

    assert(opts.OptimizationEnabled());

    GenTree* switchOp           = nullptr;
    ssize_t  switchTargetOffset = 0;
    if (!GetSwitchValueOp(block, &switchOp, &switchTargetOffset))
    {
        JITDUMP("Couldn't extract the switch's variable - bail out.\n");
        return false;
    }

    // TODO-JumpTable: Currently, we only expand the default target, but it should be possible
    // to traverse all non-default edges as well (if they point to similar value tests).
    // It shouldn't be too hard to implement (might be TP costly, though).

    BBswtDesc* switchTargets = block->GetSwitchTargets();
    if (!switchTargets->bbsHasDefault)
    {
        JITDUMP("Switch doesn't have a default target - bail out.\n");
        return false;
    }

    // Now let's see if we can merge the value test in the edgeToExpand's destination block
    // with the switch block.
    FlowEdge*   edgeToExpand    = switchTargets->getDefault();
    BasicBlock* edgeToExpandDst = edgeToExpand->getDestinationBlock();

    // IsConstantTestCondBlock will tell us if the default target is a value test block (e.g. "x == cns")
    // TODO-JumpTable: Add support for more complex tests like "x > 10 && x < 20"
    // Also, we should be able to merge two BBJ_SWITCH on the same variable. Roslyn may create such shapes.
    FlowEdge* testPasses;
    FlowEdge* testFails;
    bool      isReversed;
    GenTree*  variableNode;
    ssize_t   cns;
    if (!IsConstantTestCondBlock(edgeToExpandDst, false, &testPasses, &testFails, &isReversed, &variableNode, &cns) ||
        !edgeToExpandDst->hasSingleStmt())
    {
        JITDUMP("The default target is not performing a value test - bail out.\n");
        return false;
    }

    BasicBlock* testPassesBb = testPasses->getDestinationBlock();
    BasicBlock* testFailsBb  = testFails->getDestinationBlock();

    if (!GenTree::Compare(variableNode, switchOp))
    {
        JITDUMP("The default target is performing a value test on a different variable - bail out.\n");
        return false;
    }

    // We're less conservative than Roslyn, but we still have some limits (unless stress mode is enabled)

    if ((static_cast<size_t>(cns + switchTargetOffset) > SWITCH_MAX_DISTANCE) &&
        !compStressCompile(STRESS_DONT_LIMIT_JUMP_TABLE, 50))
    {
        JITDUMP("Switch value is out of range - bail out.\n");
        return false;
    }

    // We need to normalize the test value to the switch's range
    const ssize_t newTestValue = cns + switchTargetOffset;

    unsigned   oldJumpCnt = switchTargets->bbsCount;
    unsigned   newJumpCnt = max(oldJumpCnt, (unsigned)(newTestValue + 1 + (switchTargets->bbsHasDefault ? 1 : 0)));
    FlowEdge** oldJumpTab = switchTargets->bbsDstTab;
    FlowEdge** newJumpTab = new (this, CMK_FlowEdge) FlowEdge*[newJumpCnt];

    //
    // Update likelihoods
    //
    weight_t edgeToExpandLikelihood = edgeToExpand->getLikelihood();
    weight_t newTestLikelihood      = edgeToExpandLikelihood * testPasses->getLikelihood();
    weight_t newDefLikelihood       = edgeToExpandLikelihood * testFails->getLikelihood();

    // However, it is tricky if testPasses or testFails are already presented in the jump table.
    // Switch block currently doesn't support different likelihoods for edges pointing to the same block.
    // So we need to adjust the existing likelihoods.
    for (unsigned i = 0; i < oldJumpCnt; i++)
    {
        BasicBlock* edgeDstBb      = oldJumpTab[i]->getDestinationBlock();
        weight_t    edgeLikelihood = oldJumpTab[i]->getLikelihood();

        if (edgeDstBb == testPassesBb)
        {
            newTestLikelihood = edgeLikelihood + edgeToExpandLikelihood * testPasses->getLikelihood();
        }
        if (edgeDstBb == testFailsBb)
        {
            newDefLikelihood = edgeLikelihood + edgeToExpandLikelihood * testFails->getLikelihood();
        }
    }

    //
    // Create the new jump table
    //
    for (unsigned i = 0; i < newJumpCnt; i++)
    {
        if (i < oldJumpCnt)
        {
            // Not only the default case can point to edgeToExpandDst
            if (oldJumpTab[i]->getDestinationBlock() == edgeToExpandDst)
            {
                fgRemoveRefPred(oldJumpTab[i]);
            }
            else
            {
                // Just copy the old edge
                newJumpTab[i] = oldJumpTab[i];
                continue;
            }
        }

        if (i == (unsigned)newTestValue)
        {
            newJumpTab[i] = fgAddRefPred(testPassesBb, block);
            newJumpTab[i]->setLikelihood(newTestLikelihood);
        }
        else
        {
            newJumpTab[i] = fgAddRefPred(testFailsBb, block);
            newJumpTab[i]->setLikelihood(newDefLikelihood);
        }
    }

    switchTargets->bbsCount  = newJumpCnt;
    switchTargets->bbsDstTab = newJumpTab;

    // m_switchDescMap cache is invalidated by this change, so it has to be recomputed
    InvalidateUniqueSwitchSuccMap();

    JITDUMP("Successfully merged the default target's value test with the switch.\n");
    return true;
}

//------------------------------------------------------------------------------
// optSwitchDetectAndConvert : Try to detect a series of conditional blocks which
//    can be converted into a switch (jump-table) construct. See optSwitchConvert
//    for more details.
//
// Arguments:
//    firstBlock - A block to start the search from
//
// Return Value:
//    True if the conversion was successful, false otherwise
//
bool Compiler::optSwitchDetectAndConvert(BasicBlock* firstBlock)
{
    assert(firstBlock->KindIs(BBJ_COND));

    GenTree*  variableNode = nullptr;
    ssize_t   cns          = 0;
    FlowEdge* trueTarget   = nullptr;
    FlowEdge* falseTarget  = nullptr;

    // The algorithm is simple - we check that the given block is a constant test block
    // and then try to accumulate as many constant test blocks as possible. Once we hit
    // a block that doesn't match the pattern, we start processing the accumulated blocks.
    bool isReversed = false;
    if (IsConstantTestCondBlock(firstBlock, true, &trueTarget, &falseTarget, &isReversed, &variableNode, &cns))
    {
        if (isReversed)
        {
            // First block uses NE - we don't support this yet. We currently expect all blocks to use EQ
            // and allow NE for the last one (because it's what Roslyn usually emits).
            // TODO: make it more flexible and support cases like "x != cns1 && x != cns2 && ..."
            return false;
        }

        // No more than SWITCH_MAX_TABLE_SIZE blocks are allowed (arbitrary limit in this context)
        int     testValueIndex                  = 0;
        ssize_t testValues[SWITCH_MAX_DISTANCE] = {};
        testValues[testValueIndex]              = cns;
        testValueIndex++;

        // Track likelihood of reaching the false block
        //
        weight_t          falseLikelihood = firstBlock->GetFalseEdge()->getLikelihood();
        const BasicBlock* prevBlock       = firstBlock;

        // Now walk the next blocks and see if they are basically the same type of test
        for (const BasicBlock* currBb = firstBlock->Next(); currBb != nullptr; currBb = currBb->Next())
        {
            GenTree*  currVariableNode = nullptr;
            ssize_t   currCns          = 0;
            FlowEdge* currTrueTarget   = nullptr;
            FlowEdge* currFalseTarget  = nullptr;

            if (!currBb->hasSingleStmt())
            {
                // Only the first conditional block can have multiple statements.
                // Stop searching and process what we already have.
                return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode);
            }

            // Inspect secondary blocks
            if (IsConstantTestCondBlock(currBb, false, &currTrueTarget, &currFalseTarget, &isReversed,
                                        &currVariableNode, &currCns))
            {
                if (currTrueTarget->getDestinationBlock() != trueTarget->getDestinationBlock())
                {
                    // This blocks jumps to a different target, stop searching and process what we already have.
                    return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode);
                }

                if (!GenTree::Compare(currVariableNode, variableNode->gtEffectiveVal()))
                {
                    // A different variable node is used, stop searching and process what we already have.
                    return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode);
                }

                if (currBb->GetUniquePred(this) != prevBlock)
                {
                    // Multiple preds in a secondary block, stop searching and process what we already have.
                    return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode);
                }

                if (!BasicBlock::sameEHRegion(prevBlock, currBb))
                {
                    // Current block is in a different EH region, stop searching and process what we already have.
                    return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode);
                }

                // Ok we can work with that, add the test value to the list
                testValues[testValueIndex++] = currCns;
                falseLikelihood *= currBb->GetFalseEdge()->getLikelihood();

                if (testValueIndex == SWITCH_MAX_DISTANCE)
                {
                    // Too many suitable tests found - stop and process what we already have.
                    return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode);
                }

                if (isReversed)
                {
                    // We only support reversed test (GT_NE) for the last block.
                    return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode);
                }

                prevBlock = currBb;
            }
            else
            {
                // Current block is not a suitable test, stop searching and process what we already have.
                return optSwitchConvert(firstBlock, testValueIndex, testValues, falseLikelihood, variableNode);
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
//
// Return Value:
//    True if the conversion was successful, false otherwise
//
bool Compiler::optSwitchConvert(
    BasicBlock* firstBlock, int testsCount, ssize_t* testValues, weight_t falseLikelihood, GenTree* nodeToTest)
{
    assert(firstBlock->KindIs(BBJ_COND));
    assert(!varTypeIsSmall(nodeToTest));

    if (testsCount < SWITCH_MIN_TESTS)
    {
        // Early out - short chains.
        return false;
    }

    static_assert_no_msg(SWITCH_MIN_TESTS > 0);

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
    for (int i = 0; i < testsCount - 1; i++)
    {
        lastBlock = lastBlock->Next();
    }

    FlowEdge*  blockIfTrueEdge  = nullptr;
    FlowEdge*  blockIfFalseEdge = nullptr;
    bool       isReversed       = false;
    const bool isTest = IsConstantTestCondBlock(lastBlock, false, &blockIfTrueEdge, &blockIfFalseEdge, &isReversed);
    assert(isTest);

    BasicBlock* blockIfTrue  = blockIfTrueEdge->getDestinationBlock();
    BasicBlock* blockIfFalse = blockIfFalseEdge->getDestinationBlock();

    assert(firstBlock->TrueTargetIs(blockIfTrue));
    FlowEdge* const trueEdge  = firstBlock->GetTrueEdge();
    FlowEdge* const falseEdge = firstBlock->GetFalseEdge();

    // Convert firstBlock to a switch block
    firstBlock->SetSwitch(new (this, CMK_BasicBlock) BBswtDesc);
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
    assert(firstBlock->NextIs(blockToRemove));
    while (!lastBlock->NextIs(blockToRemove))
    {
        blockToRemove = fgRemoveBlock(blockToRemove, true);
    }

    const unsigned jumpCount = static_cast<unsigned>(maxValue - minValue + 1);
    assert((jumpCount > 0) && (jumpCount <= SWITCH_MAX_DISTANCE + 1));
    FlowEdge** jmpTab = new (this, CMK_FlowEdge) FlowEdge*[jumpCount + 1 /*default case*/];

    // Quirk: lastBlock's false target may have diverged from bbNext. If the false target is behind firstBlock,
    // we may create a cycle in the BasicBlock list by setting firstBlock->bbNext to it.
    // Add a new BBJ_ALWAYS to the false target to avoid this.
    // (We only need this if the false target is behind firstBlock,
    // but it's cheaper to just check if the false target has diverged)
    // TODO-NoFallThrough: Revisit this quirk?
    bool skipPredRemoval = false;
    if (!lastBlock->FalseTargetIs(lastBlock->Next()))
    {
        if (isReversed)
        {
            assert(lastBlock->FalseTargetIs(blockIfTrue));
            fgRemoveRefPred(trueEdge);
            BasicBlock* targetBlock = blockIfTrue;
            blockIfTrue             = fgNewBBafter(BBJ_ALWAYS, firstBlock, true);
            FlowEdge* const newEdge = fgAddRefPred(targetBlock, blockIfTrue);
            skipPredRemoval         = true;
            blockIfTrue->SetTargetEdge(newEdge);
        }
        else
        {
            assert(lastBlock->FalseTargetIs(blockIfFalse));
            BasicBlock* targetBlock = blockIfFalse;
            blockIfFalse            = fgNewBBafter(BBJ_ALWAYS, firstBlock, true);
            FlowEdge* const newEdge = fgAddRefPred(targetBlock, blockIfFalse);
            blockIfFalse->SetTargetEdge(newEdge);
        }
    }

    fgHasSwitch                                   = true;
    firstBlock->GetSwitchTargets()->bbsCount      = jumpCount + 1;
    firstBlock->GetSwitchTargets()->bbsHasDefault = true;
    firstBlock->GetSwitchTargets()->bbsDstTab     = jmpTab;
    firstBlock->SetNext(isReversed ? blockIfTrue : blockIfFalse);

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
    if (!skipPredRemoval)
    {
        fgRemoveRefPred(trueEdge);
    }

    FlowEdge* switchTrueEdge = nullptr;

    for (unsigned i = 0; i < jumpCount; i++)
    {
        // value exists in the testValues array (via bitVector) - 'true' case.
        const bool isTrue = (bitVector & static_cast<ssize_t>(1ULL << i)) != 0;

        FlowEdge* const newEdge = fgAddRefPred((isTrue ? blockIfTrue : blockIfFalse), firstBlock);
        jmpTab[i]               = newEdge;

        if ((switchTrueEdge == nullptr) && isTrue)
        {
            switchTrueEdge = newEdge;
        }
    }

    // Link the 'default' case
    FlowEdge* const switchDefaultEdge = fgAddRefPred(blockIfFalse, firstBlock);
    jmpTab[jumpCount]                 = switchDefaultEdge;

    // Fix likelihoods
    switchDefaultEdge->setLikelihood(falseLikelihood);
    switchTrueEdge->setLikelihood(1.0 - falseLikelihood);

    return true;
}
