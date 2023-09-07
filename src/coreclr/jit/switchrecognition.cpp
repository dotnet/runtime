// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#define SWITCH_MAX_DISTANCE (TARGET_POINTER_SIZE * BITS_IN_BYTE - 1)
#define SWITCH_MIN_TESTS 4

// Does given block represent JTRUE(X ==/!= CNS) construct?
bool IsConstantTestBlock(const BasicBlock* block,
                         BasicBlock**      blockIfTrue,
                         BasicBlock**      blockIfFalse,
                         bool*             isReversed,
                         GenTree**         variableNode = nullptr,
                         ssize_t*          cns          = nullptr)
{
    if (block->KindIs(BBJ_COND) && block->hasSingleStmt() && ((block->bbFlags & BBF_DONT_REMOVE) == 0))
    {
        const GenTree* rootNode = block->lastStmt()->GetRootNode();
        assert(rootNode->OperIs(GT_JTRUE));

        // It has to be JTRUE(GT_EQ or GT_NE)
        if (rootNode->gtGetOp1()->OperIs(GT_EQ, GT_NE))
        {
            GenTree* op1 = rootNode->gtGetOp1()->gtGetOp1();
            GenTree* op2 = rootNode->gtGetOp1()->gtGetOp2();

            if (!varTypeIsIntegral(op1) || !varTypeIsIntegral(op2))
            {
                // Only integral types are supported
                return false;
            }

            // We're looking for "X EQ/NE CNS" or "CNS EQ/NE X" pattern
            if (op1->IsCnsIntOrI() ^ op2->IsCnsIntOrI())
            {
                if (!op1->OperIs(GT_LCL_VAR) && !op2->OperIs(GT_LCL_VAR))
                {
                    // We can slightly relax this in future, for now we only expect "LCL_VAR EQ/NE CNS" (or reversed)
                    return false;
                }

                *isReversed   = rootNode->gtGetOp1()->OperIs(GT_NE);
                *blockIfTrue  = *isReversed ? block->bbNext : block->bbJumpDest;
                *blockIfFalse = *isReversed ? block->bbJumpDest : block->bbNext;

                if ((block->bbNext == block->bbJumpDest) || (block->bbJumpDest == block))
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
// optSwitchConvert : Convert a series of conditional blocks into a switch block
//    conditional blocks are blocks that have a single statement that is a GT_EQ
//    or GT_NE node. The blocks are expected jump into the same target and test
//    the same variable against different constants
//
// Arguments:
//    firstBlock - First conditional block in the chain
//    testsCount - Number of conditional blocks in the chain
//    testValues - Array of constants that are tested against the variable
//    nodeToTest - Variable node that is tested against the constants
//
// Return Value:
//    True if the conversion was successful, false otherwise
//
bool Compiler::optSwitchConvert(BasicBlock* firstBlock, int testsCount, ssize_t* testValues, GenTree* nodeToTest)
{
    assert(firstBlock->KindIs(BBJ_COND));

    if (testsCount < SWITCH_MIN_TESTS)
    {
        // Early out - short chains.
        return false;
    }

    // Find max and min values in the testValues array
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
        if ((newMaxValue - newMinValue) > SWITCH_MAX_DISTANCE)
        {
            // Stop here, the distance between min and max is too big
            break;
        }

        // TODO: currently we mainly focus on creating the shape that is then expanded into a bit test
        // Ideally we should create jump tables for other cases as well and we need some cost-benefit analysis.

        minValue = newMinValue;
        maxValue = newMaxValue;
    }

    if (testIdx < SWITCH_MIN_TESTS)
    {
        // Make sure we still have at least SWITCH_MIN_TESTS values after we filtered out some of them
        return false;
    }

    // Find the last block in the chain
    const BasicBlock* lastBlock = firstBlock;
    for (int i = 0; i < testIdx - 1; i++)
    {
        lastBlock = lastBlock->bbNext;
    }

    BasicBlock* blockIfTrue  = nullptr;
    BasicBlock* blockIfFalse = nullptr;
    bool        isReversed   = false;
    const bool  isTest       = IsConstantTestBlock(lastBlock, &blockIfTrue, &blockIfFalse, &isReversed);
    assert(isTest);

    // Convert firstBlock to a switch block
    firstBlock->bbJumpKind    = BBJ_SWITCH;
    firstBlock->bbJumpDest    = nullptr;
    firstBlock->bbCodeOffsEnd = lastBlock->bbCodeOffsEnd;
    firstBlock->lastStmt()->GetRootNode()->ChangeOper(GT_SWITCH);

    // The root node is now SUB(nodeToTest, minValue) if minValue != 0
    GenTree* switchValue = gtCloneExpr(nodeToTest);
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
    BasicBlock* blockToRemove = firstBlock->bbNext;
    fgRemoveRefPred(blockToRemove, firstBlock);
    while (blockToRemove != lastBlock->bbNext)
    {
        BasicBlock* nextBlock = blockToRemove->bbNext;
        fgRemoveBlock(blockToRemove, true);
        blockToRemove = nextBlock;
    }

    const auto jumpCount = static_cast<unsigned>(maxValue - minValue);
    assert(jumpCount > 0 && jumpCount <= SWITCH_MAX_DISTANCE);
    const auto jmpTab = new (this, CMK_BasicBlock) BasicBlock*[jumpCount + 1 /*default case*/];

    firstBlock->bbJumpSwt                = new (this, CMK_BasicBlock) BBswtDesc;
    firstBlock->bbJumpSwt->bbsCount      = jumpCount + 1;
    firstBlock->bbJumpSwt->bbsHasDefault = true;
    firstBlock->bbJumpSwt->bbsDstTab     = jmpTab;
    firstBlock->bbNext                   = isReversed ? blockIfTrue : blockIfFalse;
    fgHasSwitch                          = true;

    // Compose a bit vector of all the values we have in the testValues array
    // to quickly check if a value is in the array
    ssize_t bitVector = 0;
    for (testIdx = 0; testIdx < testsCount; testIdx++)
    {
        bitVector |= (1ULL << static_cast<unsigned>((testValues[testIdx] - minValue)));
    }

    for (unsigned i = 0; i < jumpCount; i++)
    {
        // value exists in the testValues array (via bitVector) - 'true' case.
        const bool isTrue = (bitVector & static_cast<unsigned>(1 << i));
        jmpTab[i]         = isTrue ? blockIfTrue : blockIfFalse;

        // firstBlock already has a link to blockIfTrue so skip the first iteration
        if (i > 0)
        {
            fgAddRefPred(jmpTab[i], firstBlock);
        }
        else
        {
            assert(isTrue);
        }
    }

    // Link the 'default' case
    jmpTab[jumpCount] = blockIfFalse;
    fgAddRefPred(blockIfFalse, firstBlock);

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
bool Compiler::optSwitchDetectAndConvert(BasicBlock* block)
{
    GenTree*    variableNode = nullptr;
    ssize_t     cns          = 0;
    BasicBlock* blockIfTrue  = nullptr;
    BasicBlock* blockIfFalse = nullptr;

    bool isReversed = false;
    if (IsConstantTestBlock(block, &blockIfTrue, &blockIfFalse, &isReversed, &variableNode, &cns))
    {
        if (isReversed)
        {
            // First block uses NE - we don't support this yet. We currently expect all blocks to use EQ
            // and allow NE for the last one (because it's what Roslyn usually emits)
            return false;
        }

        int     testValueIndex                  = 0;
        ssize_t testValues[SWITCH_MAX_DISTANCE] = {};
        testValues[testValueIndex++]            = cns;

        const BasicBlock* prevBlock = block;

        // Now walk the next blocks and see if they are basically the same type of test
        for (const BasicBlock* currBb = block->bbNext; currBb != nullptr; currBb = currBb->bbNext)
        {
            GenTree*    currVariableNode = nullptr;
            ssize_t     currCns          = 0;
            BasicBlock* currBlockIfTrue  = nullptr;
            BasicBlock* currBlockIfFalse = nullptr;

            if (IsConstantTestBlock(currBb, &currBlockIfTrue, &currBlockIfFalse, &isReversed, &currVariableNode,
                                    &currCns))
            {
                if (currBlockIfTrue != blockIfTrue)
                {
                    // Target blocks don't match, stop searching and process what we already have
                    return optSwitchConvert(block, testValueIndex, testValues, variableNode);
                }

                if (!GenTree::Compare(currVariableNode, variableNode))
                {
                    // Variable nodes don't match, stop searching and process what we already have
                    return optSwitchConvert(block, testValueIndex, testValues, variableNode);
                }

                if (currBb->GetUniquePred(this) != prevBlock)
                {
                    // Current block has multiple preds, stop searching and process what we already have
                    return optSwitchConvert(block, testValueIndex, testValues, variableNode);
                }

                if (!BasicBlock::sameEHRegion(prevBlock, currBb))
                {
                    // Current block is in a different EH region, stop searching and process what we already have
                    return optSwitchConvert(block, testValueIndex, testValues, variableNode);
                }

                testValues[testValueIndex++] = currCns;
                if (testValueIndex == SWITCH_MAX_DISTANCE)
                {
                    // Too many suitable tests found - stop and check what we already have
                    return optSwitchConvert(block, testValueIndex, testValues, variableNode);
                }

                if (isReversed)
                {
                    // We only support reversed test (GT_NE) for the last block - stop and check what we already have
                    return optSwitchConvert(block, testValueIndex, testValues, variableNode);
                }

                prevBlock = currBb;
            }
            else
            {
                // Current block is not a suitable test, stop searching and process what we already have
                return optSwitchConvert(block, testValueIndex, testValues, variableNode);
            }
        }
    }

    return false;
}

//-----------------------------------------------------------------------------
//  optSwitchRecognition: Optimize range check for if (A || B || C || D) pattern and convert it to Switch block
//
//  Returns:
//      MODIFIED_NOTHING if no optimization is performed.
//      MODIFIED_EVERYTHING otherwise.
//
//  Notes:
//      Detect if (a == val1 || a == val2 || a == val3 || ...) pattern and change it to switch tree
//      to reduce compares and jumps, and perform bit operation instead in Lowering phase.
//      This optimization is performed only for integer types. Limit the Switch
//      conversion to only for 3 or more conditional patterns to reduce code size regressions.
//
PhaseStatus Compiler::optSwitchRecognition()
{
    bool modified = false;
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        // block->KindIs(BBJ_COND) check is for better throughput.
        if (block->KindIs(BBJ_COND) && optSwitchDetectAndConvert(block))
        {
            JITDUMP("Converted block " FMT_BB " to switch\n", block->bbNum)
            modified = true;
        }
    }

    if (modified)
    {
        fgReorderBlocks(/* useProfileData */ false);
        fgUpdateChangedFlowGraph(FlowGraphUpdates::COMPUTE_BASICS);
        return PhaseStatus::MODIFIED_EVERYTHING;
    }

    return PhaseStatus::MODIFIED_NOTHING;
}
