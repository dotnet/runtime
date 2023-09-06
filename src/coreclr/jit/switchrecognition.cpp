// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

//-----------------------------------------------------------------------------
// OptRangePatternDsc:     Descriptor used for `if` range pattern optimization
//
class OptRangePatternDsc
{
public:
    static const int m_sizePatterns = 64; // The size of the patterns array

private:
    Compiler*     m_comp = nullptr; // The pointer to the Compiler instance
    CompAllocator m_allocator;      // The memory allocator
    typedef JitHashTable<ssize_t, JitLargePrimitiveKeyFuncs<ssize_t>, BasicBlock*> PatternToBlockMap;
    PatternToBlockMap m_patternToBlockMap;

    CompAllocator m_allocatorTarget; // The memory allocator
    typedef JitHashTable<BasicBlock*, JitPtrKeyFuncs<BasicBlock>, int> BlockToIntMap;
    BlockToIntMap m_blockToIntMap;

    BasicBlock* switchBBdesc   = nullptr; // The switch basic block descriptor
    BBswtDesc*  swtDsc         = nullptr; // The switch descriptor
    GenTree*    m_minOp        = nullptr; // The CNS_INT node with the minimum pattern
    BasicBlock* m_defaultJmpBB = nullptr; // The Switch default jump target
    BasicBlock* optGetJumpTargetBB(BasicBlock* block, bool reversed = false);
    bool optMakeSwitchDesc();

public:
    OptRangePatternDsc(Compiler* comp)
        : m_comp(comp)
        , m_allocator(comp->getAllocator(CMK_Generic))
        , m_patternToBlockMap(m_allocator)
        , m_allocatorTarget(comp->getAllocator(CMK_Generic))
        , m_blockToIntMap(m_allocatorTarget)
    {
    }

    BasicBlock* m_optFirstBB    = nullptr; // The first BB of the range pattern
    BasicBlock* m_optLastBB     = nullptr; // The last BB of the range pattern
    ssize_t     m_minPattern    = 0;       // The minimum pattern
    ssize_t     m_maxPattern    = 0;       // The maximum pattern
    int         m_rangePattern  = 0;       // The range of values in patterns[]
    unsigned    m_bbCodeOffs    = 0;       // IL code offset of the switch basic block
    unsigned    m_bbCodeOffsEnd = 0;       // IL code offset end of the switch basic block

    bool optSetPattern(int idxPattern, ssize_t patternVal, BasicBlock* block);
    int  optGetPatternCount();
    void optPrintPatterns();
    bool optJumpsToPatternBlock();
    bool optChangeToSwitch();
};

//-----------------------------------------------------------------------------
// optGetJumpTargetBB:  Get jumpTargetBB for the pattern
//
// Arguments:
//    block - the basic block to get its jump target.
//
// Return Value:
//    The jump target BB for the pattern
//
// Notes:
//   If compare operator is GT_EQ, the switch jump target (true case) is bbJumpDest.
//   If compare operator is GT_NE,it is bbNext.
//
BasicBlock* OptRangePatternDsc::optGetJumpTargetBB(BasicBlock* block, bool reversed)
{
    assert(block != nullptr && (block->KindIs(BBJ_SWITCH, BBJ_COND)));
    assert(block->lastStmt()->GetRootNode()->gtGetOp1() != nullptr);

    const GenTree* rootNode = block->lastStmt()->GetRootNode()->gtGetOp1();
    assert(rootNode->OperIs(GT_EQ, GT_NE));
    if (reversed)
    {
        return rootNode->OperIs(GT_EQ) ? block->bbNext : block->bbJumpDest;
    }
    return rootNode->OperIs(GT_EQ) ? block->bbJumpDest : block->bbNext;
}

//-----------------------------------------------------------------------------
// optSetPattern: Save pattern value and basic block
//
// Arguments:
//    idxPattern - the index of the pattern to set
//    patternVal - the value of the pattern
//    block - the basic block to set its jump target.
//
// Return Value:
//    true if the pattern is set, false otherwise.
//
bool OptRangePatternDsc::optSetPattern(int idxPattern, ssize_t patternVal, BasicBlock* block)
{
    if (idxPattern < 0 || idxPattern >= m_sizePatterns)
    {
        JITDUMP("idxPattern out of range")
        return false;
    }

    // Set the pattern value if it does not already exists in the map
    if (m_patternToBlockMap.Lookup(patternVal, nullptr)) // Pattern already exists
    {
        return false;
    }

    m_patternToBlockMap.Set(patternVal, block);

    // Set min pattern
    if (patternVal < m_minPattern)
    {
        m_minPattern = patternVal;

        // Update minOp to the tree with the min pattern
        GenTree* minOpNode = block->lastStmt()->GetRootNode()->gtGetOp1()->gtGetOp2();
        assert(minOpNode != nullptr && minOpNode->IsIntegralConst());
        m_minOp = minOpNode;
    }
    // Set max pattern
    if (patternVal > m_maxPattern)
    {
        m_maxPattern = patternVal;
    }

    // Set default jump target of the Switch basic block:
    //   For GT_EQ, Switch Default jump target (for false case) is set to bbNext.
    //   For GT_NE, Switch Default jump target (for false case) is set to bbJumpDest.
    assert((block != nullptr) && block->KindIs(BBJ_COND));
    assert(block->lastStmt()->GetRootNode()->gtGetOp1() != nullptr);

    m_defaultJmpBB = optGetJumpTargetBB(block, true);

    // Update the code offset end range
    assert(block->bbCodeOffsEnd <= UINT_MAX);
    m_bbCodeOffsEnd = block->bbCodeOffsEnd;

    // Set the last basic block of the range pattern
    m_optLastBB = block;

    return true;
}

//-----------------------------------------------------------------------------
// optGetPatternCount: Get the number of patterns
//
// Return Value:
//    The number of reserved patterns
//
int OptRangePatternDsc::optGetPatternCount()
{
    return m_patternToBlockMap.GetCount();
}

//-----------------------------------------------------------------------------
// optPrintPatterns:     Prints the patterns from m_patternToBlockMap
//
void OptRangePatternDsc::optPrintPatterns()
{
    // print patterns from m_patternToBlockMap using key iterator
    PatternToBlockMap* patternToBlockMap = &m_patternToBlockMap;
    for (ssize_t patternVal : PatternToBlockMap::KeyIteration(patternToBlockMap))
    {
        BasicBlock* mappedBlock;
        if (patternToBlockMap->Lookup(patternVal, &mappedBlock))
        {
            printf("patternVal = %d, block = %d\n", patternVal, mappedBlock->bbNum);
        }
    }
}

//-----------------------------------------------------------------------------
// optJumpsToPatternBlock:     Checks if any pattern block jumps to one of the blocks
//                             within m_patternToBlockMap
//
// Arguments:
//    None
//
// Return Value:
//   True if any of m_patternToBlockMap block jumps to one of the blocks within m_patternToBlockMap
//   False otherwise.
//
bool OptRangePatternDsc::optJumpsToPatternBlock()
{
    PatternToBlockMap* patternToBlockMap = &m_patternToBlockMap;
    const unsigned int maxPatternBbNum   = m_optFirstBB->bbNum + optGetPatternCount() - 1;

    for (ssize_t patternVal : PatternToBlockMap::KeyIteration(patternToBlockMap))
    {
        BasicBlock* sourceBlock;
        if (patternToBlockMap->Lookup(patternVal, &sourceBlock))
        {
            BasicBlock* destBlock = sourceBlock->bbJumpDest;
            assert(destBlock != nullptr);

            if (destBlock->bbNum >= m_optFirstBB->bbNum && destBlock->bbNum <= maxPatternBbNum)
            {
                return true;
            }
        }
    }

    return false;
}

//-----------------------------------------------------------------------------
// optMakeSwitchDesc: Make a Switch descriptor with a switch jump table
//
// Returns:
//    true if the switch descriptor is created successfully with the switch jump tables. False otherwise.
//
// Notes:
//    It creates switch descriptor with a jump table for each range pattern and default case.
//    If the value is part of the reserved patterns, the switch jump table is set to the jump target
//    for true case
//          If GT_EQ, jump target is block's bbJumpDest. If GT_NE, jump target is block's bbNext.
//    Otherwise, the switch jump table is set to the default jump target.
//    No tree is updated in this method.
//
bool OptRangePatternDsc::optMakeSwitchDesc()
{
#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("\n*************** In optMakeSwitchDesc()\n");
    }
#endif // DEBUG

    if (optGetPatternCount() > m_rangePattern || m_rangePattern < 2 || optGetPatternCount() > m_rangePattern ||
        m_rangePattern > m_sizePatterns)
    {
        return false;
    }

    BasicBlock* prevBb       = nullptr;
    int         patternIndex = 0;

    BasicBlockFlags bbFlags   = BBF_EMPTY;
    unsigned        curBBoffs = 0;
    unsigned        nxtBBoffs = 0;
    unsigned        jmpCnt    = 0; // # of switch cases (including default case)

    BasicBlock** jmpTab          = nullptr;
    BasicBlock** jmpPtr          = nullptr;
    unsigned     uniqueTargetCnt = 0; // # of unique jump targets
    bool         tailCall        = false;
    ssize_t      minVal          = m_minPattern;

    // Allocate the jump table
    jmpCnt = m_rangePattern + 1;
    jmpPtr = jmpTab = new (m_comp, CMK_BasicBlock) BasicBlock*[jmpCnt];

    // Make a jump table for the range of patterns
    // If reserved pattern, jump to jump target of source block. If not, jump to default target.
    for (int idxRng = 0; idxRng < m_rangePattern; idxRng++)
    {
        // Find a mapped block from a pattern
        ssize_t     key         = minVal + idxRng;
        BasicBlock* mappedBlock = nullptr;
        if (m_patternToBlockMap.Lookup(key, &mappedBlock)) // A mapped block is found
        {
            BasicBlock* jumpTargetBb = optGetJumpTargetBB(mappedBlock);
            *jmpPtr                  = (BasicBlock*)(size_t)(jumpTargetBb->bbCodeOffs);
            *(jmpPtr)                = jumpTargetBb;
            // Update the unique target basic block to count map
            if (!m_blockToIntMap.Lookup(jumpTargetBb, nullptr))
            {
                m_blockToIntMap.Set(jumpTargetBb, 1);
            }
        }
        else
        {
            BasicBlock* defaultJmpBb = m_defaultJmpBB;
            *jmpPtr                  = (BasicBlock*)(size_t)(defaultJmpBb->bbCodeOffs);
            *(jmpPtr)                = defaultJmpBb;
            // Update the unique target basic block to count map
            if (!m_blockToIntMap.Lookup(defaultJmpBb, nullptr))
            {
                m_blockToIntMap.Set(defaultJmpBb, 1);
            }
        }
        jmpPtr++;
    }

    // Append the default label to the jump table
    *jmpPtr   = (BasicBlock*)(size_t)(m_defaultJmpBB->bbCodeOffs);
    *(jmpPtr) = m_defaultJmpBB;
    // Update the unique target basic block to count map
    if (!m_blockToIntMap.Lookup(m_defaultJmpBB, nullptr))
    {
        m_blockToIntMap.Set(m_defaultJmpBB, 1);
    }
    jmpPtr++;

    // Make sure we found the right number of labels
    noway_assert(jmpPtr == jmpTab + jmpCnt);

    //
    // Check if it is profitable to use a switch instead of a series of conditional branches
    //

    // If the number of unique target counts is 1, it has only default case. Not profitable.
    // If it is >= 3, it it not converted to a bit test in Lowering. So, skip it.
    uniqueTargetCnt = m_blockToIntMap.GetCount();
    if (uniqueTargetCnt != 2)
    {
        return false;
    }

    noway_assert(jmpCnt >= 2);

    // If all jumps to the same target block, BBJ_NONE or BBJ_ALWAYS is better.
    BasicBlock* uniqueSucc = nullptr;
    if (uniqueTargetCnt == 2)
    {
        uniqueSucc = jmpTab[0];
        noway_assert(jmpCnt >= 2);
        for (unsigned i = 1; i < jmpCnt - 1; i++)
        {
            if (jmpTab[i] != uniqueSucc)
            {
                uniqueSucc = nullptr;
                break;
            }
        }
    }
    if (uniqueSucc != nullptr)
    {
        return false;
    }

    // Check if it is better to use Jmp instead of Switch
    BasicBlock* defaultBB   = jmpTab[jmpCnt - 1];
    BasicBlock* followingBB = m_optLastBB->bbNext;

    // Is the number of cases right for a jump switch?
    const bool firstCaseFollows = (followingBB == jmpTab[0]);
    const bool defaultFollows   = (followingBB == defaultBB);

    unsigned minSwitchTabJumpCnt = 2; // table is better than just 2 cmp/jcc

    // This means really just a single cmp/jcc (aka a simple if/else)
    if (firstCaseFollows || defaultFollows)
    {
        minSwitchTabJumpCnt++;
    }

#if defined(TARGET_ARM)
    // On ARM for small switch tables we will
    // generate a sequence of compare and branch instructions
    // because the code to load the base of the switch
    // table is huge and hideous due to the relocation... :(
    minSwitchTabJumpCnt += 2;
#endif // TARGET_ARM

    bool useJumpSequence = jmpCnt < minSwitchTabJumpCnt;

    if (TargetOS::IsUnix && TargetArchitecture::IsArm32)
    {
        useJumpSequence = useJumpSequence || m_comp->IsTargetAbi(CORINFO_NATIVEAOT_ABI);
    }

    if (useJumpSequence) // It is better to use a series of compare and branch IR trees.
    {
        return false;
    }

    // One of the case blocks has to follow the switch block. All the pattern blocks except for the first pattern block
    // will be removed from Switch conversion. So, we need to check if jump target follows the last pattern block.
    BasicBlock* bbCase0           = nullptr;
    BasicBlock* bbCase1           = jmpTab[0];
    BasicBlock* nextBbAfterSwitch = m_optLastBB->bbNext;

    for (unsigned tabIdx = 1; tabIdx < (jmpCnt - 1); tabIdx++)
    {
        if (jmpTab[tabIdx] != bbCase1 && bbCase0 == nullptr)
        {
            bbCase0 = jmpTab[tabIdx];
            break;
        }
    }
    if ((nextBbAfterSwitch != bbCase0) && (nextBbAfterSwitch != bbCase1))
    {
        return false;
    }

    // If the next basic block after Switch is an empty block with an unconditional jump, skip it.
    if (nextBbAfterSwitch->isEmpty() && (nextBbAfterSwitch->bbJumpKind == BBJ_ALWAYS) &&
        (nextBbAfterSwitch != nextBbAfterSwitch->bbJumpDest))
    {
        JITDUMP("\nSkip converting to Switch block if Switch jumps to an empty block with an unconditional "
            "jump (" FMT_BB " -> " FMT_BB ")\n", m_optFirstBB->bbNum, nextBbAfterSwitch->bbNum)

        return false;
    }

    // Allocate the switch descriptor
    swtDsc = new (m_comp, CMK_BasicBlock) BBswtDesc;

    // Fill in the remaining fields of the switch descriptor
    swtDsc->bbsCount  = jmpCnt;
    swtDsc->bbsDstTab = jmpTab;

    m_comp->fgHasSwitch = true;

    if (m_comp->opts.compProcedureSplitting)
    {
        m_comp->opts.compProcedureSplitting = false;
        JITDUMP("Turning off procedure splitting for this method, as it might need switch tables; "
                "implementation limitation.\n");
    }

    tailCall = false;

#ifdef DEBUG
    if (m_comp->verbose)
    {
        //  Print bbNum of each jmpTab
        printf("------Switch " FMT_BB " jumps to:    ", m_optFirstBB->bbNum);
        for (unsigned i = 0; i < swtDsc->bbsCount; i++)
        {
            printf("%c" FMT_BB, (i == 0) ? ' ' : ',', jmpTab[i]->bbNum);
        }
        printf("\n\n");
    }
#endif // DEBUG

    return true;
}

//-----------------------------------------------------------------------------
// optChangeToSwitch: Change the first pattern block to SWITCH block and remove other pattern blocks
//
// Return Value:
//    true if the blocks were successfully updated, false otherwise.
//
// Notes:
//    Leave Switch basic block only and remove all other blocks in the range pattern.
//    Update reference count of jump target blocks.
//    If the CNS_INT node does not have a min pattern, replace it with the CNS_INT node with the min value.
//
//    Tree before the optimization:
//    ```
//      JTRUE
//        \EQ
//          \LCL_VAR
//          \CNS_INT
//    ```
//
//    Tree after the optimization:
//    ```
//      SWITCH
//        \SUB
//          \LCL_VAR
//          \CNS_INT (min pattern)
//     ```
bool OptRangePatternDsc::optChangeToSwitch()
{
    JITDUMP("\n*************** In optChangeToSwitch()\n")

    assert(m_optFirstBB != nullptr && m_optFirstBB->KindIs(BBJ_COND) && m_optFirstBB->bbNext != nullptr);
    assert(optGetPatternCount() <= m_rangePattern && m_rangePattern >= 2 && optGetPatternCount() <= m_rangePattern &&
           m_rangePattern <= m_sizePatterns);

    // Make Switch descriptor with a jump table
    if (!optMakeSwitchDesc())
    {
        return false;
    }

    bool updatedBlocks = false;
    switchBBdesc       = m_optFirstBB;
    int patternCount   = optGetPatternCount();

    // Update the Switch basic block

    // Change `JTRUE` basic block to `Switch` basic block
    switchBBdesc = m_optFirstBB;

    // Change BBJ_COND to BBJ_SWITCH
    switchBBdesc->bbJumpKind = BBJ_SWITCH;
    switchBBdesc->bbJumpDest = nullptr;

    switchBBdesc->bbCodeOffs    = m_bbCodeOffs;
    switchBBdesc->bbCodeOffsEnd = m_bbCodeOffsEnd;
    switchBBdesc->bbJumpSwt     = swtDsc;
    GenTree* rootTree           = switchBBdesc->lastStmt()->GetRootNode();
    assert(rootTree->OperIs(GT_JTRUE));
    assert(!(rootTree->gtFlags & GTF_SIDE_EFFECT)); // JTRUE node should not have side effects

    // Change from GT_JTRUE to GT_SWITCH
    rootTree->ChangeOper(GT_SWITCH);

    // Special args to fgAddRefPred
    FlowEdge* const oldEdge = nullptr;

    // Remove non-switch pattern blocks. Update the reference count of jump target block from the removed block.
    BasicBlock* currBb = switchBBdesc->bbNext;
    for (int idxPattern = 1; idxPattern < patternCount && currBb != nullptr; idxPattern++)
    {
        assert(!(currBb->bbFlags & BBF_DONT_REMOVE));

        BasicBlock* prevBlock = currBb->bbPrev;
        BasicBlock* nextBlock = currBb->bbNext;
        BasicBlock* jumpBlock = optGetJumpTargetBB(currBb);

        // Unlink the current block and its pred block

        // Check if prevBlock is the predecessor of currBb
        assert(currBb != nullptr && prevBlock != nullptr);
        FlowEdge* pred = m_comp->fgGetPredForBlock(currBb, prevBlock);
        assert(pred != nullptr);

        m_comp->fgRemoveRefPred(currBb, prevBlock);

        // Link Switch block and current block's jump target block
        m_comp->fgAddRefPred(jumpBlock, switchBBdesc, oldEdge);

        // Link Switch block and the next block:
        //      if GT_EQ and currBb is the last pattern block, skip because bbNext is already linked as default jump
        //      target if GT_NE, it is already linked when linking its jump target block to Switch block
        if (currBb->lastStmt()->GetRootNode()->gtGetOp1()->OperIs(GT_EQ) && idxPattern != (patternCount - 1))
        {
            m_comp->fgAddRefPred(nextBlock, switchBBdesc, oldEdge);
        }

        m_comp->fgRemoveBlock(currBb, /* unreachable */ true);

        updatedBlocks = true;
        currBb        = nextBlock;
    }

    // Update the reference count of the default jump block
    int numNotFound = m_rangePattern - patternCount + 1; // +1 for switch default case
    for (int idxFalse = 0; idxFalse < numNotFound; idxFalse++)
    {
        m_comp->fgAddRefPred(m_defaultJmpBB, switchBBdesc, oldEdge);
    }

    // Continue to transform Switch node

    Statement* stmt = switchBBdesc->lastStmt();

    // Change from GT_EQ or GT_NE to GT_SUB
    //      tree: SUB
    //      op1: LCL_VAR
    //      op2: GT_CNS_INT or GT_CNS_LNG
    GenTree* tree = rootTree->gtGetOp1(); // GT_EQ or GT_NE node to chnage to GT_SUB
    tree->ChangeOper(GT_SUB);
    assert(tree->gtGetOp1() != nullptr && tree->gtGetOp1()->OperIs(GT_LCL_VAR));

    // Change constant node if siwtch tree does not have the mininum pattern
    if (tree->gtGetOp2() != nullptr && tree->gtGetOp2()->AsIntCon()->IconValue() != m_minPattern)
    {
        GenTree* op2        = tree->gtGetOp2(); // GT_CNS_INT or GT_CNS_LNG node
        tree->AsOp()->gtOp2 = m_minOp;

        m_comp->gtSetStmtInfo(stmt);
        m_comp->fgSetStmtSeq(stmt);

        DEBUG_DESTROY_NODE(op2);
    }

    m_comp->gtUpdateStmtSideEffects(stmt);

    return updatedBlocks;
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
//      This optimization is performed only for integer types.
//      Run this phase after "Optimize Layout" phase to avoid Switch block being reordered and separated from its bbNext
//      target block. If they are not adjacent, Lowering does not convert them to a bit test. Limit the Switch
//      conversion to only for 3 or more conditional patterns to reduce code size regressions.
//
PhaseStatus Compiler::optSwitchRecognition()
{
    JITDUMP("\n*************** In optSwitchRecognition()\n");

    OptRangePatternDsc optRngPattern(this);

    int          patternIndex      = 0; // The index of the pattern in the array
    bool         foundPattern      = false;
    unsigned int firstPatternBBNum = 0; // Basic block number of the first pattern found
    BasicBlock*  prevBb            = fgFirstBB;

    if (fgFirstBB->bbNext == nullptr)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    for (BasicBlock* const currBb : Blocks())
    {
        if (currBb->KindIs(BBJ_COND) && prevBb != nullptr && prevBb->KindIs(BBJ_COND))
        {
            // Check if prevBb is the predecessor of currBb and currBb has only one predecessor
            if ((currBb->GetUniquePred(this) != prevBb) || ((currBb->bbFlags & BBF_DONT_REMOVE) != 0))
            {
                if (foundPattern)
                {
                    break; // Stop searching if patterns are not from consecutive basic blocks
                }
                continue;
            }

            // Basic block must have only one statement
            if (currBb->hasSingleStmt() && prevBb->hasSingleStmt())
            {
                // Skip if there is any side effect
                assert(currBb->lastStmt()->GetRootNode()->OperIs(GT_JTRUE)); // JTRUE node
                if (currBb->lastStmt()->GetRootNode()->gtFlags & GTF_SIDE_EFFECT)
                {
                    // Stop searching if patterns are not from consecutive basic blocks
                    if (foundPattern)
                    {
                        break;
                    }
                    continue;
                }

                GenTree* currCmpOp = currBb->lastStmt()->GetRootNode()->gtGetOp1(); // GT_EQ or GT_NE node
                GenTree* prevCmpOp = prevBb->lastStmt()->GetRootNode()->gtGetOp1();
                assert(currCmpOp != nullptr && prevCmpOp != nullptr);

                // Compare operator is GT_EQ. If it is GT_NE, it is the end of the pattern check.
                if ((prevCmpOp->OperIs(GT_EQ) && currCmpOp->OperIs(GT_EQ)) ||
                    (prevCmpOp->OperIs(GT_EQ) && currCmpOp->OperIs(GT_NE)))
                {
                    if (prevBb->bbJumpDest == currBb)
                    {
                        if (foundPattern)
                        {
                            break;
                        }
                        continue;
                    }

                    // Check both conditions to have constant on the right side (optimize GT_CNS_INT only)
                    if (currCmpOp->gtGetOp2()->IsCnsIntOrI() && prevCmpOp->gtGetOp2()->IsCnsIntOrI())
                    {
                        // Check both conditions to have the same local variable number
                        if (prevCmpOp->gtGetOp1()->OperIs(GT_LCL_VAR) && currCmpOp->gtGetOp1()->OperIs(GT_LCL_VAR) &&
                            prevCmpOp->gtGetOp1()->AsLclVar()->GetLclNum() ==
                                currCmpOp->gtGetOp1()->AsLclVar()->GetLclNum())
                        {
#ifdef DEBUG
                            if (this->verbose)
                            {
                                printf("\nFound pattern (Prev vs Curr):\n");
                                gtDispTree(prevCmpOp);
                                printf("\n");
                                gtDispTree(currCmpOp);
                                printf("\n\n");
                            }
#endif // DEBUG

                            // No optimization if the number of patterns is greater than 64.
                            if (patternIndex >= optRngPattern.m_sizePatterns)
                            {
                                JITDUMP("Too many patterns found (> 64), no optimization done.\n")
                                return PhaseStatus::MODIFIED_NOTHING;
                            }

                            // First pattern found
                            if (!foundPattern)
                            {
                                assert(patternIndex == 0 && prevCmpOp->OperIs(GT_EQ));
                                // If the first pattern block is rarely run, skip it.
                                if (prevBb->isRunRarely())
                                {
                                    JITDUMP(FMT_BB " is run rarely. Skip optimizing this block.\n", prevBb->bbNum)
                                    prevBb = currBb;
                                    continue;
                                }
                                ssize_t firstPatternVal = prevCmpOp->gtGetOp2()->AsIntCon()->IconValue();

                                // Initialize the pattern range
                                //
                                assert(prevBb != nullptr && prevBb->bbJumpKind == BBJ_COND &&
                                       prevBb->lastStmt()->GetRootNode()->OperIs(GT_JTRUE));

                                optRngPattern.m_optFirstBB = prevBb;
                                optRngPattern.m_optLastBB  = prevBb;

                                // Initialize min pattern and max pattern to the first pattern value
                                optRngPattern.m_minPattern = firstPatternVal;
                                optRngPattern.m_maxPattern = firstPatternVal;

                                // Initialize the code offset range from the first block
                                assert(prevBb->bbCodeOffs >= 0 && prevBb->bbCodeOffs <= UINT_MAX);
                                if (optRngPattern.optGetPatternCount() == 0)
                                {
                                    optRngPattern.m_bbCodeOffs = prevBb->bbCodeOffs;
                                }

                                // Save the first pattern
                                if (!optRngPattern.optSetPattern(patternIndex, firstPatternVal, prevBb))
                                {
                                    break;
                                }

                                firstPatternBBNum = prevBb->bbNum;
                                patternIndex++;
                            }

                            // Current pattern

                            // Save the pattern and Switch default jump target for the pattern (false case)
                            ssize_t currentPatternVal = currCmpOp->gtGetOp2()->AsIntCon()->IconValue();
                            if (!optRngPattern.optSetPattern(patternIndex, currentPatternVal, currBb))
                            {
                                break;
                            }

                            patternIndex++;
                            foundPattern = true;

                            // Stop searching if the current BB is GT_NE. It is the last pattern.
                            if (currCmpOp->OperIs(GT_NE))
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        // Optimize only when patterns are found in consecutive BBs.
        // Stop searching if patterns have been found in previous BBs, but the current BB does not have a pattern
        if (foundPattern && patternIndex < (int)(currBb->bbNum - firstPatternBBNum + 1))
        {
            break;
        }

        prevBb = currBb;
    }

    if (foundPattern)
    {
        int patternCount = optRngPattern.optGetPatternCount();
        // If there are less than 3 patterns, no optimization is done. It is not profitable.
        if (patternCount <= 2 || patternCount > optRngPattern.m_sizePatterns)
        {
            return PhaseStatus::MODIFIED_NOTHING;
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("Reserved patterns:\n");
            optRngPattern.optPrintPatterns();
        }
#endif // DEBUG

        // Check if blocks jump to any of the pattern block
        if (optRngPattern.optJumpsToPatternBlock())
        {
            JITDUMP("A pattern block jumps to another pattern block, no optimization done.\n");
            return PhaseStatus::MODIFIED_NOTHING;
        }

        // Find range of pattern values
        ssize_t minPattern   = optRngPattern.m_minPattern;
        ssize_t maxPattern   = optRngPattern.m_maxPattern;
        int     rangePattern = (int)(maxPattern - minPattern + 1);
        if (patternCount > rangePattern || rangePattern < 2 || rangePattern > optRngPattern.m_sizePatterns)
        {
            JITDUMP("Range of pattern values is too small (< 2) or too big (> %d): %d\n",
                       optRngPattern.m_sizePatterns, rangePattern);

            return PhaseStatus::MODIFIED_NOTHING;
        }
        assert(rangePattern >= 0 && rangePattern <= optRngPattern.m_sizePatterns);
        optRngPattern.m_rangePattern = rangePattern;

        JITDUMP("Min Max Range: %lld, %lld, %d\n", (int64_t)minPattern, (int64_t)maxPattern, rangePattern);

        // Replace "JTRUE" block with a "Switch" block and remove other pattern blocks
        if (optRngPattern.optChangeToSwitch())
        {
            return PhaseStatus::MODIFIED_EVERYTHING;
        }
    }

    return PhaseStatus::MODIFIED_NOTHING;
}
