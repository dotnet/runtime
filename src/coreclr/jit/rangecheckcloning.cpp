// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "rangecheckcloning.h"

// This file contains the definition of the "Range check cloning" phase.
//
// The goal of this phase is to pick up range checks which were not optimized by the
// range check optimization phase and clone them to have "fast" and "slow" paths.
// This is similar to what the "Loop Cloning" phase does for loops. Example:
//
//   arr[i + 1] = x;
//   arr[i + 3] = y;
//   arr[i + 5] = z;
//   arr[i + 8] = w;
//
// assertprop/rangecheck phases give up on the above bounds checks because of the
// increasing offsets and there are no assertions that they can rely on.
// This phase handles such cases by cloning the entire block (only the affected statements
// to be precise) into "fast" and "slow" under a "cloned" condition:
//
//   if (i >= 0 && i < arr.Length - 8)
//   {
//       // Fast path
//       arr[i + 1] = x; // no bounds check
//       arr[i + 3] = y; // no bounds check
//       arr[i + 5] = z; // no bounds check
//       arr[i + 8] = w; // no bounds check
//   }
//   else
//   {
//       // Slow path
//       arr[i + 1] = x; // bounds check
//       arr[i + 3] = y; // bounds check
//       arr[i + 5] = w; // bounds check
//       arr[i + 8] = w; // bounds check
//   }
//
// The phase scans all statements in a block and groups the bounds checks based on
// "Base Index and Length" pairs (VNs). Then the phase takes the largest group and
// clones the block to have fast and slow paths.

//------------------------------------------------------------------------------------
// Initialize: Initialize the BoundsCheckInfo with the given bounds check node.
//    and perform some basic legality checks.
//
// Arguments:
//    comp             - The compiler instance
//    statement        - The statement containing the bounds check
//    statementIdx     - The index of the statement in the block
//    bndChk           - The bounds check node (its use edge)
//
// Return Value:
//    true if the initialization was successful, false otherwise.
//
bool BoundsCheckInfo::Initialize(const Compiler* comp, Statement* statement, int statementIdx, GenTree** bndChk)
{
    assert((bndChk != nullptr) && ((*bndChk) != nullptr));

    stmt      = statement;
    stmtIdx   = statementIdx;
    bndChkUse = bndChk;
    idxVN     = comp->vnStore->VNConservativeNormalValue(BndChk()->GetIndex()->gtVNPair);
    lenVN     = comp->vnStore->VNConservativeNormalValue(BndChk()->GetArrayLength()->gtVNPair);
    if ((idxVN == ValueNumStore::NoVN) || (lenVN == ValueNumStore::NoVN))
    {
        return false;
    }

    if (BndChk()->GetIndex()->IsIntCnsFitsInI32())
    {
        // Index being a constant means we have index=0 and cns offset
        offset = static_cast<int>(BndChk()->GetIndex()->AsIntCon()->IconValue());
        idxVN  = comp->vnStore->VNZeroForType(TYP_INT);
    }
    else
    {
        if (comp->vnStore->TypeOfVN(idxVN) != TYP_INT)
        {
            return false;
        }

        // Otherwise, peel the offset from the index using VN
        comp->vnStore->PeelOffsetsI32(&idxVN, &offset);
        assert(idxVN != ValueNumStore::NoVN);
    }
    assert(comp->vnStore->TypeOfVN(idxVN) == TYP_INT);

    if (offset < 0)
    {
        // Not supported yet.
        return false;
    }
    return true;
}

//------------------------------------------------------------------------------------
// RemoveBoundsChk - Remove the given bounds check from the statement and the block.
//
// Arguments:
//    comp    - compiler instance
//    treeUse - the bounds check node to remove (its use edge)
//    stmt    - the statement containing the bounds check
//
static void RemoveBoundsChk(Compiler* comp, GenTree** treeUse, Statement* stmt)
{
    JITDUMP("Before RemoveBoundsChk:\n");
    DISPTREE(*treeUse);

    GenTree* sideEffList = nullptr;
    comp->gtExtractSideEffList(*treeUse, &sideEffList, GTF_SIDE_EFFECT, /*ignoreRoot*/ true);
    *treeUse = (sideEffList != nullptr) ? sideEffList : comp->gtNewNothingNode();

    comp->gtUpdateStmtSideEffects(stmt);
    comp->gtSetStmtInfo(stmt);
    comp->fgSetStmtSeq(stmt);

    JITDUMP("After RemoveBoundsChk:\n");
    DISPTREE(stmt->GetRootNode());
}

// -----------------------------------------------------------------------------
// optRangeCheckCloning_DoClone: Perform the actual range check cloning for the given range
//    of bounds checks. All the legality checks are done before calling this function.
//    This function effectively converts a single block (containing bounds checks) into:
//
//  prevBb:
//    goto lowerBndBb
//
//  lowerBndBb:
//    if (idx < 0)
//        goto fallbackBb
//    else
//        goto upperBndBb
//
//  upperBndBb:
//    if (idx < len - maxConstOffset)
//        goto fastpathBb
//    else
//        goto fallbackBb
//
//  fallbackBb:
//    [Original block with bounds checks]
//    goto nextBb
//
//  fastpathBb:
//    [Cloned block with no bounds checks]
//    goto nextBb
//
//  nextBb:
//    ...
//
// Arguments:
//    comp        - The compiler instance
//    block       - The block to clone
//    bndChkStack - The stack of bounds checks to clone
//    lastStmt    - The last statement in the block (the block is split after this statement)
//
// Return Value:
//    The next block to visit after the cloning.
//
static BasicBlock* optRangeCheckCloning_DoClone(Compiler*             comp,
                                                BasicBlock*           block,
                                                BoundsCheckInfoStack* bndChkStack,
                                                Statement*            lastStmt)
{
    assert(block != nullptr);
    assert(bndChkStack->Height() > 0);

    // The bound checks are in the execution order (top of the stack is the last check)
    BoundsCheckInfo firstCheck = bndChkStack->Bottom();
    BasicBlock*     prevBb     = block;

    // First, split the block at the first bounds check using gtSplitTree (via fgSplitBlockBeforeTree):
    GenTree**   bndChkUse;
    Statement*  newFirstStmt;
    BasicBlock* fastpathBb =
        comp->fgSplitBlockBeforeTree(block, firstCheck.stmt, firstCheck.BndChk(), &newFirstStmt, &bndChkUse);

    // Perform the usual routine after gtSplitTree:
    while ((newFirstStmt != nullptr) && (newFirstStmt != firstCheck.stmt))
    {
        comp->fgMorphStmtBlockOps(fastpathBb, newFirstStmt);
        newFirstStmt = newFirstStmt->GetNextStmt();
    }
    comp->fgMorphStmtBlockOps(fastpathBb, firstCheck.stmt);
    comp->gtUpdateStmtSideEffects(firstCheck.stmt);

    // Now split the block at the last bounds check using fgSplitBlockAfterStatement:
    // TODO-RangeCheckCloning: call gtSplitTree for lastBndChkStmt as well, to cut off
    // the stuff we don't have to clone.
    BasicBlock* lastBb = comp->fgSplitBlockAfterStatement(fastpathBb, lastStmt);

    DebugInfo debugInfo = fastpathBb->firstStmt()->GetDebugInfo();

    // Find the maximum offset
    int offset = 0;
    for (int i = 0; i < bndChkStack->Height(); i++)
    {
        offset = max(offset, bndChkStack->Top(i).offset);
    }
    assert(offset >= 0);

    GenTree* idx    = comp->gtCloneExpr(firstCheck.BndChk()->GetIndex());
    GenTree* arrLen = comp->gtCloneExpr(firstCheck.BndChk()->GetArrayLength());

    // gtSplitTree is expected to spill the side effects of the index and array length expressions
    assert((idx->gtFlags & GTF_ALL_EFFECT) == 0);
    assert((arrLen->gtFlags & GTF_ALL_EFFECT) == 0);

    // Since we're re-using the index node from the first bounds check and its value was spilled
    // by the tree split, we need to restore the base index by subtracting the offset.
    // Hopefully, someone will fold this back into the index expression.
    //
    GenTree* idxClone;
    if (firstCheck.offset > 0)
    {
        GenTree* offsetNode = comp->gtNewIconNode(-firstCheck.offset); // never overflows
        idx                 = comp->gtNewOperNode(GT_ADD, TYP_INT, idx, offsetNode);
        idxClone            = comp->fgInsertCommaFormTemp(&idx);
    }
    else
    {
        idxClone = comp->gtCloneExpr(idx);
    }

    // 1) lowerBndBb:
    //
    // if (i < 0)
    //     goto fallbackBb
    // else
    //     goto upperBndBb
    //
    GenTreeOp* idxLowerBoundTree = comp->gtNewOperNode(GT_LT, TYP_INT, comp->gtCloneExpr(idx), comp->gtNewIconNode(0));
    idxLowerBoundTree->gtFlags |= GTF_RELOP_JMP_USED;
    GenTree*    jtrue      = comp->gtNewOperNode(GT_JTRUE, TYP_VOID, idxLowerBoundTree);
    BasicBlock* lowerBndBb = comp->fgNewBBFromTreeAfter(BBJ_COND, prevBb, jtrue, debugInfo);

    JITDUMP("\nLower bound check:\n");
    DISPTREE(jtrue);

    // 2) upperBndBb:
    //
    // if (i < arrLen - indexOffset)
    //     goto fastpathBb
    // else
    //     goto fallbackBb
    //
    GenTreeOp* idxUpperBoundTree;
    if (idx->IsIntegralConst(0))
    {
        // if the index is just 0, then we can simplify the condition to "arrLen > indexOffset"
        idxUpperBoundTree = comp->gtNewOperNode(GT_GT, TYP_INT, arrLen, comp->gtNewIconNode(offset));
    }
    else
    {
        // "i < arrLen + (-indexOffset)"
        GenTree*   negOffset = comp->gtNewIconNode(-offset); // never overflows
        GenTreeOp* subNode   = comp->gtNewOperNode(GT_ADD, TYP_INT, arrLen, negOffset);
        idxUpperBoundTree    = comp->gtNewOperNode(GT_LT, TYP_INT, idxClone, subNode);
    }
    idxUpperBoundTree->gtFlags |= GTF_RELOP_JMP_USED;
    jtrue                  = comp->gtNewOperNode(GT_JTRUE, TYP_VOID, idxUpperBoundTree);
    BasicBlock* upperBndBb = comp->fgNewBBFromTreeAfter(BBJ_COND, lowerBndBb, jtrue, debugInfo);

    JITDUMP("\nUpper bound check:\n");
    DISPTREE(jtrue);

    // 3) fallbackBb:
    //
    // For the fallback (slow path), we just entirely clone the fast path.
    //
    BasicBlock* fallbackBb = comp->fgNewBBafter(BBJ_ALWAYS, upperBndBb, false);
    BasicBlock::CloneBlockState(comp, fallbackBb, fastpathBb);

    // 4) fastBlockBb:
    //
    // No actions needed - it's our current block as is.

    // Wire up the edges
    //
    comp->fgRedirectTargetEdge(prevBb, lowerBndBb);
    FlowEdge* fallbackToNextBb       = comp->fgAddRefPred(lastBb, fallbackBb);
    FlowEdge* lowerBndToUpperBndEdge = comp->fgAddRefPred(upperBndBb, lowerBndBb);
    FlowEdge* lowerBndToFallbackEdge = comp->fgAddRefPred(fallbackBb, lowerBndBb);
    FlowEdge* upperBndToFastPathEdge = comp->fgAddRefPred(fastpathBb, upperBndBb);
    FlowEdge* upperBndToFallbackEdge = comp->fgAddRefPred(fallbackBb, upperBndBb);
    fallbackBb->SetTargetEdge(fallbackToNextBb);
    lowerBndBb->SetTrueEdge(lowerBndToFallbackEdge);
    lowerBndBb->SetFalseEdge(lowerBndToUpperBndEdge);
    upperBndBb->SetTrueEdge(upperBndToFastPathEdge);
    upperBndBb->SetFalseEdge(upperBndToFallbackEdge);

    // Set the weights. We assume that the fallback is rarely taken.
    //
    lowerBndBb->inheritWeight(prevBb);
    upperBndBb->inheritWeight(prevBb);
    fastpathBb->inheritWeight(prevBb);
    fallbackBb->bbSetRunRarely();
    fallbackToNextBb->setLikelihood(1.0f);
    lowerBndToUpperBndEdge->setLikelihood(1.0f);
    lowerBndToFallbackEdge->setLikelihood(0.0f);
    upperBndToFastPathEdge->setLikelihood(1.0f);
    upperBndToFallbackEdge->setLikelihood(0.0f);

    lowerBndBb->SetFlags(BBF_INTERNAL);
    upperBndBb->SetFlags(BBF_INTERNAL | BBF_HAS_IDX_LEN);

    // Now drop the bounds check from the fast path
    while (!bndChkStack->Empty())
    {
        BoundsCheckInfo info = bndChkStack->Pop();
#if DEBUG
        // Ensure that the bounds check that we're removing is in the fast path:
        bool statementFound = false;
        for (Statement* const stmt : fastpathBb->Statements())
        {
            if (stmt == info.stmt)
            {
                statementFound = true;

                // Find the bndChk in the statement
                Compiler::fgWalkResult result = comp->fgWalkTreePre(
                    stmt->GetRootNodePointer(),
                    [](GenTree** pTree, Compiler::fgWalkData* data) -> Compiler::fgWalkResult {
                    return (*pTree == (GenTree*)data->pCallbackData) ? Compiler::WALK_ABORT : Compiler::WALK_CONTINUE;
                },
                    info.BndChk());
                // We don't need to validate bndChkParent - RemoveBoundsChk will do it for us
                assert(result == Compiler::WALK_ABORT);
                break;
            }
        }
        assert(statementFound);
#endif
        RemoveBoundsChk(comp, info.bndChkUse, info.stmt);
    }

    comp->fgMorphBlockStmt(lowerBndBb, lowerBndBb->lastStmt() DEBUGARG("Morph lowerBnd"));
    comp->fgMorphBlockStmt(upperBndBb, upperBndBb->lastStmt() DEBUGARG("Morph upperBnd"));
    if (lowerBndBb->lastStmt() != nullptr)
    {
        // lowerBndBb might be converted into no-op by fgMorphBlockStmt(lowerBndBb)
        // it happens when we emit BBJ_COND(0 >= 0) fake block (for simplicity)
        comp->gtUpdateStmtSideEffects(lowerBndBb->lastStmt());
    }
    comp->gtUpdateStmtSideEffects(upperBndBb->lastStmt());

    // All blocks must be in the same EH region
    assert(BasicBlock::sameEHRegion(prevBb, lowerBndBb));
    assert(BasicBlock::sameEHRegion(prevBb, upperBndBb));
    assert(BasicBlock::sameEHRegion(prevBb, fastpathBb));
    assert(BasicBlock::sameEHRegion(prevBb, fallbackBb));
    assert(BasicBlock::sameEHRegion(prevBb, lastBb));

    return fastpathBb;
}

// A visitor to record all the bounds checks in a statement in the execution order
class BoundsChecksVisitor final : public GenTreeVisitor<BoundsChecksVisitor>
{
    Statement*                      m_stmt;
    ArrayStack<BoundCheckLocation>* m_boundsChks;
    int                             m_stmtIdx;

public:
    enum
    {
        DoPostOrder       = true,
        DoPreOrder        = true,
        UseExecutionOrder = true
    };

    BoundsChecksVisitor(Compiler*                       compiler,
                        Statement*                      stmt,
                        int                             stmtIdx,
                        ArrayStack<BoundCheckLocation>* bndChkLocations)
        : GenTreeVisitor(compiler)
        , m_stmt(stmt)
        , m_boundsChks(bndChkLocations)
        , m_stmtIdx(stmtIdx)
    {
    }

    fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
    {
        // No GTF_EXCEPT - no bounds check down the tree
        if (((*use)->gtFlags & GTF_EXCEPT) == 0)
        {
            return fgWalkResult::WALK_SKIP_SUBTREES;
        }
        return fgWalkResult::WALK_CONTINUE;
    }

    fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        if ((*use)->OperIs(GT_BOUNDS_CHECK))
        {
            m_boundsChks->Push(BoundCheckLocation(m_stmt, use, m_stmtIdx));
        }
        return fgWalkResult::WALK_CONTINUE;
    }
};

// -----------------------------------------------------------------------------
// DoesComplexityExceed: Check if the complexity of the bounds checks exceeds the budget.
//    We want to avoid cloning blocks with too many unrelated trees/statements between
//    the bounds checks.
//
// Arguments:
//    comp    - The compiler instance
//    bndChks - The stack of bounds checks
//
// Return Value:
//    true if the complexity exceeds the budget, false otherwise.
//
static bool DoesComplexityExceed(Compiler* comp, ArrayStack<BoundsCheckInfo>* bndChks)
{
    Statement* firstBndChkStmt = bndChks->Bottom().stmt;
    Statement* lastBndChkStmt  = bndChks->Top().stmt;

    JITDUMP("Checking complexity from " FMT_STMT " to " FMT_STMT "\n", firstBndChkStmt->GetID(),
            lastBndChkStmt->GetID());

    assert(bndChks->Height() <= MAX_CHECKS_PER_GROUP);

    // An average statement with a bounds check is ~20 nodes. There can be statements
    // between the bounds checks (i.e. bounds checks from another groups). So let's say
    // our budget is 40 nodes per bounds check.
    unsigned budget = bndChks->Height() * BUDGET_MULTIPLIER;
    JITDUMP("\tBudget: %d nodes.\n", budget);

    Statement* currentStmt = firstBndChkStmt;
    while (currentStmt != lastBndChkStmt)
    {
        GenTree* rootNode = currentStmt->GetRootNode();
        if (rootNode != nullptr)
        {
            unsigned actual = 0;
            if (comp->gtComplexityExceeds(rootNode, budget, &actual))
            {
                JITDUMP("\tExceeded budget!");
                return true;
            }
            JITDUMP("\t\tSubtracting %d from budget in " FMT_STMT " statement\n", actual, currentStmt->GetID());
            budget -= actual;
        }
        currentStmt = currentStmt->GetNextStmt();
    }

    JITDUMP("Complexity is within budget: %d\n", budget);
    return false;
}

// -----------------------------------------------------------------------------
// optRangeCheckCloning: The main entry point for the range check cloning phase.
//    This phase scans all the blocks in the method and groups the bounds checks
//    in each block by the "Base Index and Length" pairs (VNs). Then it picks up
//    the largest group and clones the block to have fast and slow paths in order
//    to optimize the bounds checks in the fast path.
//    See the overview at the top of the file and the comments in the optRangeCheckCloning_DoClone
//    function for more details.
//
// Return Value:
//    The status of the phase after the transformation.
//
PhaseStatus Compiler::optRangeCheckCloning()
{
    if (!doesMethodHaveBoundsChecks())
    {
        JITDUMP("Current method has no bounds checks\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    const bool preferSize = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_SIZE_OPT);
    if (preferSize)
    {
        // The optimization comes with a codegen size increase
        JITDUMP("Optimized for size - bail out.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    bool modified = false;

    // An array to keep all the bounds checks in the block
    // Strictly speaking, we don't need this array and can group the bounds checks
    // right as we walk them, but this helps to improve the TP/Memory usage
    // as many blocks don't have enough bounds checks to clone anyway.
    ArrayStack<BoundCheckLocation> bndChkLocations(getAllocator(CMK_RangeCheckCloning));

    // A map to group the bounds checks by the base index and length VNs
    BoundsCheckInfoMap bndChkMap(getAllocator(CMK_RangeCheckCloning));

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->Next())
    {
        if (!block->HasFlag(BBF_MAY_HAVE_BOUNDS_CHECKS))
        {
            // TP optimization - skip blocks that *likely* don't have bounds checks
            continue;
        }

        if (block->isRunRarely() || block->KindIs(BBJ_THROW))
        {
            continue;
        }

        bndChkLocations.Reset();
        bndChkMap.RemoveAll();

        int stmtIdx = -1;
        for (Statement* const stmt : block->Statements())
        {
            stmtIdx++;
            if (block->HasTerminator() && (stmt == block->lastStmt()))
            {
                // TODO-RangeCheckCloning: Splitting these blocks at the last statements
                // require using gtSplitTree for the last bounds check.
                break;
            }

            // Now just record all the bounds checks in the block (in the execution order)
            //
            BoundsChecksVisitor visitor(this, stmt, stmtIdx, &bndChkLocations);
            visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
        }

        if (bndChkLocations.Height() < MIN_CHECKS_PER_GROUP)
        {
            JITDUMP("Not enough bounds checks in the block - bail out.\n");
            continue;
        }

        // Now we need to group the bounds checks by the base index and length VNs.
        // We could do it directly in the visitor above and avoid this O(n) pass,
        // but it's more TP/Memory wise to use stack-allocated ArrayStack first and
        // bail out on <MIN_CHECKS_PER_GROUP cases.
        for (int i = 0; i < bndChkLocations.Height(); i++)
        {
            BoundCheckLocation loc = bndChkLocations.Bottom(i);
            BoundsCheckInfo    bci{};
            if (bci.Initialize(this, loc.stmt, loc.stmtIdx, loc.bndChkUse))
            {
                IdxLenPair             key(bci.idxVN, bci.lenVN);
                BoundsCheckInfoStack** value = bndChkMap.LookupPointerOrAdd(key, nullptr);
                if (*value == nullptr)
                {
                    CompAllocator allocator = getAllocator(CMK_RangeCheckCloning);
                    *value                  = new (allocator) BoundsCheckInfoStack(allocator);
                }

                if ((*value)->Height() < MAX_CHECKS_PER_GROUP)
                {
                    (*value)->Push(bci);
                }
            }
        }

        if (bndChkMap.GetCount() == 0)
        {
            JITDUMP("No bounds checks in the block - bail out.\n");
            continue;
        }

        // Now choose the largest group of bounds checks (the one with the most checks)
        ArrayStack<BoundsCheckInfoStack*> groups(getAllocator(CMK_RangeCheckCloning));

        for (BoundsCheckInfoMap::Node* keyValuePair : BoundsCheckInfoMap::KeyValueIteration(&bndChkMap))
        {
            ArrayStack<BoundsCheckInfo>* value = keyValuePair->GetValue();
            if ((value->Height() >= MIN_CHECKS_PER_GROUP) && !DoesComplexityExceed(this, value))
            {
                groups.Push(value);
            }
        }

        if (groups.Height() == 0)
        {
            JITDUMP("No suitable group of bounds checks in the block - bail out.\n");
            continue;
        }

        // We have multiple groups of bounds checks in the block.
        // let's pick a group that appears first in the block and the one whose last bounds check
        // appears last in the block.
        //
        BoundsCheckInfoStack* firstGroup = groups.Top();
        BoundsCheckInfoStack* lastGroup  = groups.Top();
        for (int i = 0; i < groups.Height(); i++)
        {
            BoundsCheckInfoStack* group      = groups.Bottom(i);
            int                   firstStmt  = group->Bottom().stmtIdx;
            int                   secondStmt = group->Top().stmtIdx;
            if (firstStmt < firstGroup->Bottom().stmtIdx)
            {
                firstGroup = group;
            }
            if (secondStmt > lastGroup->Top().stmtIdx)
            {
                lastGroup = group;
            }
        }

        // We're going to clone for the first group.
        // But let's see if we can extend the end of the group so future iterations
        // can fit more groups in the same block.
        //
        Statement* lastStmt = firstGroup->Top().stmt;

        int firstGroupStarts = firstGroup->Bottom().stmtIdx;
        int firstGroupEnds   = firstGroup->Top().stmtIdx;
        int lastGroupStarts  = lastGroup->Bottom().stmtIdx;
        int lastGroupEnds    = lastGroup->Top().stmtIdx;

        // The only requirement is that both groups must overlap - we don't want to
        // end up cloning unrelated statements between them (not a correctness issue,
        // just a heuristic to avoid cloning too much).
        //
        if (firstGroupEnds < lastGroupEnds && firstGroupEnds >= lastGroupStarts)
        {
            lastStmt = lastGroup->Top().stmt;
        }

        JITDUMP("Cloning bounds checks in " FMT_BB " from " FMT_STMT " to " FMT_STMT "\n", block->bbNum,
                firstGroup->Bottom().stmt->GetID(), lastStmt->GetID());

        BasicBlock* nextBbToVisit = optRangeCheckCloning_DoClone(this, block, firstGroup, lastStmt);
        assert(nextBbToVisit != nullptr);
        // optRangeCheckCloning_DoClone wants us to visit nextBbToVisit next
        block = nextBbToVisit->Prev();
        assert(block != nullptr);
        modified = true;
    }

    if (modified)
    {
        return PhaseStatus::MODIFIED_EVERYTHING;
    }

    return PhaseStatus::MODIFIED_NOTHING;
}
