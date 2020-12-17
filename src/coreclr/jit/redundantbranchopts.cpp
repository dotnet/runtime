// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

//------------------------------------------------------------------------
// optRedundantBranches: try and optimize redundant branches in the method
//
// Returns:
//   PhaseStatus indicating if anything changed.
//
PhaseStatus Compiler::optRedundantBranches()
{
    // We attempt this "bottom up" so walk the flow graph in postorder.
    //
    bool madeChanges = false;

    for (unsigned i = fgDomBBcount; i > 0; --i)
    {
        BasicBlock* const block = fgBBInvPostOrder[i];

        // Upstream phases like optOptimizeBools may remove blocks
        // that are referenced in bbInvPosOrder.
        //
        if ((block->bbFlags & BBF_REMOVED) != 0)
        {
            continue;
        }

        // We currently can optimize some BBJ_CONDs.
        //
        if (block->bbJumpKind == BBJ_COND)
        {
            madeChanges |= optRedundantBranch(block);
        }
    }

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// optRedundantBranch: try and optimize a possibly redundant branch
//
// Arguments:
//   block - block with branch to optimize
//
// Returns:
//   True if the branch was optimized.
//
bool Compiler::optRedundantBranch(BasicBlock* const block)
{
    Statement* const stmt = block->lastStmt();

    if (stmt == nullptr)
    {
        return false;
    }

    GenTree* const jumpTree = stmt->GetRootNode();

    if (!jumpTree->OperIs(GT_JTRUE))
    {
        return false;
    }

    GenTree* const tree = jumpTree->AsOp()->gtOp1;

    if (!(tree->OperKind() & GTK_RELOP))
    {
        return false;
    }

    // Walk up the dom tree and see if any dominating block has branched on
    // exactly this tree's VN...
    //
    BasicBlock* prevBlock  = block;
    BasicBlock* domBlock   = block->bbIDom;
    int         relopValue = -1;

    if (domBlock == nullptr)
    {
        return false;
    }

    while (domBlock != nullptr)
    {
        if (prevBlock == block)
        {
            JITDUMP("\nChecking " FMT_BB " for redundancy\n", block->bbNum);
        }

        // Check the current dominator
        //
        JITDUMP(" ... checking dom " FMT_BB "\n", domBlock->bbNum);

        if (domBlock->bbJumpKind == BBJ_COND)
        {
            Statement* const domJumpStmt = domBlock->lastStmt();
            GenTree* const   domJumpTree = domJumpStmt->GetRootNode();
            assert(domJumpTree->OperIs(GT_JTRUE));
            GenTree* const domCmpTree = domJumpTree->AsOp()->gtGetOp1();

            if (domCmpTree->OperKind() & GTK_RELOP)
            {
                ValueNum domCmpVN = domCmpTree->GetVN(VNK_Conservative);

                // Note we could also infer the tree relop's value from similar relops higher in the dom tree.
                // For example, (x >= 0) dominating (x > 0), or (x < 0) dominating (x > 0).
                //
                // That is left as a future enhancement.
                //
                if (domCmpVN == tree->GetVN(VNK_Conservative))
                {
                    // Thes compare in "tree" is redundant.
                    // Is there a unique path from the dominating compare?
                    JITDUMP(" Redundant compare; current relop:\n");
                    DISPTREE(tree);
                    JITDUMP(" dominating relop in " FMT_BB " with same VN:\n", domBlock->bbNum);
                    DISPTREE(domCmpTree);

                    BasicBlock* trueSuccessor  = domBlock->bbJumpDest;
                    BasicBlock* falseSuccessor = domBlock->bbNext;

                    const bool trueReaches  = fgReachable(trueSuccessor, block);
                    const bool falseReaches = fgReachable(falseSuccessor, block);

                    if (trueReaches && falseReaches)
                    {
                        // Both dominating compare outcomes reach the current block so we can't infer the
                        // value of the relop.
                        //
                        // If the dominating compare is close to the current compare, this may be a missed
                        // opportunity to tail duplicate.
                        //
                        JITDUMP("Both successors of " FMT_BB " reach, can't optimize\n", domBlock->bbNum);

                        if ((trueSuccessor->GetUniqueSucc() == block) || (falseSuccessor->GetUniqueSucc() == block))
                        {
                            JITDUMP("Perhaps we should have tail duplicated " FMT_BB "\n", block->bbNum);
                        }
                    }
                    else if (trueReaches)
                    {
                        // Taken jump in dominator reaches, fall through doesn't; relop must be true.
                        //
                        JITDUMP("Jump successor " FMT_BB " of " FMT_BB " reaches, relop must be true\n",
                                domBlock->bbJumpDest->bbNum, domBlock->bbNum);
                        relopValue = 1;
                        break;
                    }
                    else if (falseReaches)
                    {
                        // Fall through from dominator reaches, taken jump doesn't; relop must be false.
                        //
                        JITDUMP("Fall through successor " FMT_BB " of " FMT_BB " reaches, relop must be false\n",
                                domBlock->bbNext->bbNum, domBlock->bbNum);
                        relopValue = 0;
                        break;
                    }
                    else
                    {
                        // No apparent path from the dominating BB.
                        //
                        // If domBlock or block is in an EH handler we may fail to find a path.
                        // Just ignore those cases.
                        //
                        // No point in looking further up the tree.
                        //
                        break;
                    }
                }
            }
        }

        // Keep looking higher up in the tree
        //
        prevBlock = domBlock;
        domBlock  = domBlock->bbIDom;
    }

    // Did we determine the relop value via dominance checks? If so, optimize.
    //
    if (relopValue == -1)
    {
        JITDUMP("Failed to find a suitable dominating compare, so we won't optimize\n");
        return false;
    }

    // Bail out if tree is has certain side effects
    //
    // Note we really shouldn't get here if the tree has non-exception effects,
    // as they should have impacted the value number.
    //
    if ((tree->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        // Bail if there is a non-exception effect.
        //
        if ((tree->gtFlags & GTF_SIDE_EFFECT) != GTF_EXCEPT)
        {
            JITDUMP("Current relop has non-exception side effects, so we won't optimize\n");
            return false;
        }

        // Be conservative if there is an exception effect and we're in an EH region
        // as we might not model the full extent of EH flow.
        //
        if (block->hasTryIndex())
        {
            JITDUMP("Current relop has exception side effect and is in a try, so we won't optimize\n");
            return false;
        }
    }

    JITDUMP("\nRedundant branch opt in " FMT_BB ":\n", block->bbNum);

    tree->ChangeOperConst(GT_CNS_INT);
    tree->AsIntCon()->gtIconVal = relopValue;

    fgMorphBlockStmt(block, stmt DEBUGARG(__FUNCTION__));
    return true;
}
