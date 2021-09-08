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

#if DEBUG
    if (verbose)
    {
        fgDispBasicBlocks(verboseTrees);
    }
#endif // DEBUG

    class OptRedundantBranchesDomTreeVisitor : public DomTreeVisitor<OptRedundantBranchesDomTreeVisitor>
    {
    public:
        bool madeChanges;

        OptRedundantBranchesDomTreeVisitor(Compiler* compiler)
            : DomTreeVisitor(compiler, compiler->fgSsaDomTree), madeChanges(false)
        {
        }

        void PreOrderVisit(BasicBlock* block)
        {
        }

        void PostOrderVisit(BasicBlock* block)
        {
            // Skip over any removed blocks.
            //
            if ((block->bbFlags & BBF_REMOVED) != 0)
            {
                return;
            }

            // We currently can optimize some BBJ_CONDs.
            //
            if (block->bbJumpKind == BBJ_COND)
            {
                madeChanges |= m_compiler->optRedundantBranch(block);
            }
        }
    };

    OptRedundantBranchesDomTreeVisitor visitor(this);
    visitor.WalkTree();

    // Reset visited flags, in case we set any.
    //
    for (BasicBlock* const block : Blocks())
    {
        block->bbFlags &= ~BBF_VISITED;
    }

#if DEBUG
    if (verbose && visitor.madeChanges)
    {
        fgDispBasicBlocks(verboseTrees);
    }
#endif // DEBUG

    return visitor.madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
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
        // Check the current dominator
        //
        if (domBlock->bbJumpKind == BBJ_COND)
        {
            Statement* const domJumpStmt = domBlock->lastStmt();
            GenTree* const   domJumpTree = domJumpStmt->GetRootNode();
            assert(domJumpTree->OperIs(GT_JTRUE));
            GenTree* const domCmpTree = domJumpTree->AsOp()->gtGetOp1();

            if (domCmpTree->OperKind() & GTK_RELOP)
            {
                // We can use liberal VNs as bounds checks are not yet
                // manifest explicitly as relops.
                //
                ValueNum domCmpVN = domCmpTree->GetVN(VNK_Liberal);

                // Note we could also infer the tree relop's value from similar relops higher in the dom tree.
                // For example, (x >= 0) dominating (x > 0), or (x < 0) dominating (x > 0).
                //
                // That is left as a future enhancement.
                //
                if (domCmpVN == tree->GetVN(VNK_Liberal))
                {
                    // The compare in "tree" is redundant.
                    // Is there a unique path from the dominating compare?
                    //
                    JITDUMP("\nDominator " FMT_BB " of " FMT_BB " has relop with same liberal VN:\n", domBlock->bbNum,
                            block->bbNum);
                    DISPTREE(domCmpTree);
                    JITDUMP(" Redundant compare; current relop:\n");
                    DISPTREE(tree);

                    BasicBlock* const trueSuccessor  = domBlock->bbJumpDest;
                    BasicBlock* const falseSuccessor = domBlock->bbNext;
                    const bool        trueReaches    = optReachable(trueSuccessor, block, domBlock);
                    const bool        falseReaches   = optReachable(falseSuccessor, block, domBlock);

                    if (trueReaches && falseReaches)
                    {
                        // Both dominating compare outcomes reach the current block so we can't infer the
                        // value of the relop.
                        //
                        // However we may be able to update the flow from block's predecessors so they
                        // bypass block and instead transfer control to jump's successors (aka jump threading).
                        //
                        const bool wasThreaded = optJumpThread(block, domBlock);

                        if (wasThreaded)
                        {
                            return true;
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
                        // We should rarely see this given that optReachable is returning
                        // up to date results, but as we optimize we create unreachable blocks,
                        // and that can lead to cases where we can't find paths. That means we may be
                        // optimizing code that is now unreachable, but attempts to fix or avoid
                        // doing that lead to more complications, and it isn't that common.
                        // So we just tolerate it.
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

//------------------------------------------------------------------------
// optJumpThread: try and bypass the current block by rerouting
//   flow from predecessors directly to successors.
//
// Arguments:
//   block - block with branch to optimize
//   domBlock - a dominating block that has an equivalent branch
//
// Returns:
//   True if the branch was optimized.
//
// Notes:
//
//    A       B          A     B
//     \     /           |     |
//      \   /            |     |
//      block     ==>    |     |
//      /   \            |     |
//     /     \           |     |
//    C       D          C     D
//
bool Compiler::optJumpThread(BasicBlock* const block, BasicBlock* const domBlock)
{
    assert(block->bbJumpKind == BBJ_COND);
    assert(domBlock->bbJumpKind == BBJ_COND);

    // If the dominating block is not the immediate dominator
    // we might need to duplicate a lot of code to thread
    // the jumps. See if that's the case.
    //
    const bool isIDom = domBlock == block->bbIDom;
    if (!isIDom)
    {
        // Walk up the dom tree until we hit dom block.
        //
        // If none of the doms in the stretch are BBJ_COND,
        // then we must have already optimized them, and
        // so should not have to duplicate code to thread.
        //
        BasicBlock* idomBlock = block->bbIDom;
        while ((idomBlock != nullptr) && (idomBlock != domBlock))
        {
            if (idomBlock->bbJumpKind == BBJ_COND)
            {
                JITDUMP(" -- " FMT_BB " not closest branching dom, so no threading\n", idomBlock->bbNum);
                return false;
            }
            JITDUMP(" -- bypassing %sdom " FMT_BB " as it was already optimized\n",
                    (idomBlock == block->bbIDom) ? "i" : "", idomBlock->bbNum);
            idomBlock = idomBlock->bbIDom;
        }

        // If we didn't bail out above, we should have reached domBlock.
        //
        assert(idomBlock == domBlock);
    }

    JITDUMP("Both successors of %sdom " FMT_BB " reach " FMT_BB " -- attempting jump threading\n", isIDom ? "i" : "",
            domBlock->bbNum, block->bbNum);

    // If the block is the first block of try-region, then skip jump threading
    if (bbIsTryBeg(block))
    {
        JITDUMP(FMT_BB " is first block of try-region; no threading\n", block->bbNum);
        return false;
    }

    // Since flow is going to bypass block, make sure there
    // is nothing in block that can cause a side effect.
    //
    // Note we neglect PHI assignments. This reflects a general lack of
    // SSA update abilities in the jit. We really should update any uses
    // of PHIs defined here with the corresponding PHI input operand.
    //
    // TODO: if block has side effects, for those predecessors that are
    // favorable (ones that don't reach block via a critical edge), consider
    // duplicating block's IR into the predecessor. This is the jump threading
    // analog of the optimization we encourage via fgOptimizeUncondBranchToSimpleCond.
    //
    Statement* const lastStmt = block->lastStmt();

    for (Statement* const stmt : block->NonPhiStatements())
    {
        GenTree* const tree = stmt->GetRootNode();

        // We can ignore exception side effects in the jump tree.
        //
        // They are covered by the exception effects in the dominating compare.
        // We know this because the VNs match and they encode exception states.
        //
        if ((tree->gtFlags & GTF_SIDE_EFFECT) != 0)
        {
            if (stmt == lastStmt)
            {
                assert(tree->OperIs(GT_JTRUE));
                if ((tree->gtFlags & GTF_SIDE_EFFECT) == GTF_EXCEPT)
                {
                    // However, be conservative if block is in a try as we might not
                    // have a full picture of EH flow.
                    //
                    if (!block->hasTryIndex())
                    {
                        // We will ignore the side effect on this tree.
                        //
                        continue;
                    }
                }
            }

            JITDUMP(FMT_BB " has side effects; no threading\n", block->bbNum);
            return false;
        }
    }

    // In order to optimize we have to be able to determine which predecessors
    // are correlated exclusively with a true value for block's relop, and which
    // are correlated exclusively with a false value (aka true preds and false preds).
    //
    // To do this we try and follow the flow from domBlock to block; any block pred
    // reachable from domBlock's true edge is a true pred, and vice versa.
    //
    // However, there are some exceptions:
    //
    // * It's possible for a pred to be reachable from both paths out of domBlock;
    // if so, we can't jump thread that pred.
    //
    // * It's also possible that a pred can't branch directly to a successor as
    // it might violate EH region constraints. Since this causes the same issues
    // as an ambiguous pred we'll just classify these as ambiguous too.
    //
    // * It's also possible to have preds with implied eh flow to the current
    // block, eg a catch return, and so we won't see either path reachable.
    // We'll handle those as ambiguous as well.
    //
    // * It's also possible that the pred is a switch; we will treat switch
    // preds as ambiguous as well.
    //
    // For true preds and false preds we can reroute flow. It may turn out that
    // one of the preds falls through to block. We would prefer not to introduce
    // a new block to allow changing that fall through to a jump, so if we have
    // both a pred that is not a true pred, and a fall through, we defer optimizing
    // the fall through pred as well.
    //
    int               numPreds          = 0;
    int               numAmbiguousPreds = 0;
    int               numTruePreds      = 0;
    int               numFalsePreds     = 0;
    BasicBlock*       fallThroughPred   = nullptr;
    BasicBlock* const trueSuccessor     = domBlock->bbJumpDest;
    BasicBlock* const falseSuccessor    = domBlock->bbNext;
    BasicBlock* const trueTarget        = block->bbJumpDest;
    BasicBlock* const falseTarget       = block->bbNext;
    BlockSet          truePreds         = BlockSetOps::MakeEmpty(this);
    BlockSet          ambiguousPreds    = BlockSetOps::MakeEmpty(this);

    for (BasicBlock* const predBlock : block->PredBlocks())
    {
        numPreds++;

        // Treat switch preds as ambiguous for now.
        //
        if (predBlock->bbJumpKind == BBJ_SWITCH)
        {
            JITDUMP(FMT_BB " is a switch pred\n", predBlock->bbNum);
            BlockSetOps::AddElemD(this, ambiguousPreds, predBlock->bbNum);
            numAmbiguousPreds++;
            continue;
        }

        const bool isTruePred =
            ((predBlock == domBlock) && (trueSuccessor == block)) || optReachable(trueSuccessor, predBlock, domBlock);
        const bool isFalsePred =
            ((predBlock == domBlock) && (falseSuccessor == block)) || optReachable(falseSuccessor, predBlock, domBlock);

        if (isTruePred == isFalsePred)
        {
            // Either both reach, or neither reaches.
            //
            // We should rarely see (false,false) given that optReachable is returning
            // up to date results, but as we optimize we create unreachable blocks,
            // and that can lead to cases where we can't find paths. That means we may be
            // optimizing code that is now unreachable, but attempts to fix or avoid doing that
            // lead to more complications, and it isn't that common. So we tolerate it.
            //
            JITDUMP(FMT_BB " is an ambiguous pred\n", predBlock->bbNum);
            BlockSetOps::AddElemD(this, ambiguousPreds, predBlock->bbNum);
            numAmbiguousPreds++;
            continue;
        }

        if (isTruePred)
        {
            if (!BasicBlock::sameEHRegion(predBlock, trueTarget))
            {
                JITDUMP(FMT_BB " is an eh constrained pred\n", predBlock->bbNum);
                numAmbiguousPreds++;
                BlockSetOps::AddElemD(this, ambiguousPreds, predBlock->bbNum);
                continue;
            }

            numTruePreds++;
            BlockSetOps::AddElemD(this, truePreds, predBlock->bbNum);
            JITDUMP(FMT_BB " is a true pred\n", predBlock->bbNum);
        }
        else
        {
            assert(isFalsePred);

            if (!BasicBlock::sameEHRegion(predBlock, falseTarget))
            {
                JITDUMP(FMT_BB " is an eh constrained pred\n", predBlock->bbNum);
                BlockSetOps::AddElemD(this, ambiguousPreds, predBlock->bbNum);
                numAmbiguousPreds++;
                continue;
            }

            numFalsePreds++;
            JITDUMP(FMT_BB " is a false pred\n", predBlock->bbNum);
        }

        // Note if the true or false pred is the fall through pred.
        //
        if (predBlock->bbNext == block)
        {
            JITDUMP(FMT_BB " is the fall-through pred\n", predBlock->bbNum);
            assert(fallThroughPred == nullptr);
            fallThroughPred = predBlock;
        }
    }

    // All preds should have been classified.
    //
    assert(numPreds == numTruePreds + numFalsePreds + numAmbiguousPreds);

    if ((numTruePreds == 0) && (numFalsePreds == 0))
    {
        // This is possible, but should be rare.
        //
        JITDUMP(FMT_BB " only has ambiguous preds, not optimizing\n", block->bbNum);
        return false;
    }

    if ((numAmbiguousPreds > 0) && (fallThroughPred != nullptr))
    {
        JITDUMP(FMT_BB " has both ambiguous preds and a fall through pred, not optimizing\n", block->bbNum);
        return false;
    }

    // We should be good to go
    //
    JITDUMP("Optimizing via jump threading\n");

    // Now reroute the flow from the predecessors.
    //
    // If there is a fall through pred, modify block by deleting the terminal
    // jump statement, and update it to jump or fall through to the appropriate successor.
    // Note this is just a refinement of pre-existing flow so no EH check is needed.
    //
    // All other predecessors must reach block via a jump. So we can update their
    // flow directly by changing their jump targets to the appropriate successor,
    // provided it's a permissable flow in our EH model.
    //
    for (BasicBlock* const predBlock : block->PredBlocks())
    {
        // If this was an ambiguous pred, skip.
        //
        if (BlockSetOps::IsMember(this, ambiguousPreds, predBlock->bbNum))
        {
            continue;
        }

        const bool isTruePred = BlockSetOps::IsMember(this, truePreds, predBlock->bbNum);

        // Is this the one and only unambiguous fall through pred?
        //
        if (predBlock->bbNext == block)
        {
            assert(predBlock == fallThroughPred);

            // No other pred can safely pass control through block.
            //
            assert(numAmbiguousPreds == 0);

            // Clean out the terminal branch statement; we are going to repurpose this block
            //
            Statement* lastStmt = block->lastStmt();
            fgRemoveStmt(block, lastStmt);

            if (isTruePred)
            {
                JITDUMP("Fall through flow from pred " FMT_BB " -> " FMT_BB " implies predicate true\n",
                        predBlock->bbNum, block->bbNum);
                JITDUMP("  repurposing " FMT_BB " to always jump to " FMT_BB "\n", block->bbNum, trueTarget->bbNum);
                fgRemoveRefPred(block->bbNext, block);
                block->bbJumpKind = BBJ_ALWAYS;
            }
            else
            {
                JITDUMP("Fall through flow from pred " FMT_BB " -> " FMT_BB " implies predicate false\n",
                        predBlock->bbNum, block->bbNum);
                JITDUMP("  repurposing " FMT_BB " to always fall through to " FMT_BB "\n", block->bbNum,
                        falseTarget->bbNum);
                fgRemoveRefPred(block->bbJumpDest, block);
                block->bbJumpKind = BBJ_NONE;
            }
        }
        else
        {
            assert(predBlock->bbNext != block);
            if (isTruePred)
            {
                assert(!optReachable(falseSuccessor, predBlock, domBlock));
                JITDUMP("Jump flow from pred " FMT_BB " -> " FMT_BB
                        " implies predicate true; we can safely redirect flow to be " FMT_BB " -> " FMT_BB "\n",
                        predBlock->bbNum, block->bbNum, predBlock->bbNum, trueTarget->bbNum);

                fgRemoveRefPred(block, predBlock);
                fgReplaceJumpTarget(predBlock, trueTarget, block);
                fgAddRefPred(trueTarget, predBlock);
            }
            else
            {
                JITDUMP("Jump flow from pred " FMT_BB " -> " FMT_BB
                        " implies predicate false; we can safely redirect flow to be " FMT_BB " -> " FMT_BB "\n",
                        predBlock->bbNum, block->bbNum, predBlock->bbNum, falseTarget->bbNum);

                fgRemoveRefPred(block, predBlock);
                fgReplaceJumpTarget(predBlock, falseTarget, block);
                fgAddRefPred(falseTarget, predBlock);
            }
        }
    }

    // We optimized.
    //
    fgModified = true;
    return true;
}

//------------------------------------------------------------------------
// optReachable: see if there's a path from one block to another,
//   including paths involving EH flow.
//
// Arguments:
//    fromBlock - staring block
//    toBlock   - ending block
//    excludedBlock - ignore paths that flow through this block
//
// Returns:
//    true if there is a path, false if there is no path
//
// Notes:
//    Like fgReachable, but computed on demand (and so accurate given
//    the current flow graph), and also considers paths involving EH.
//
//    This may overstate "true" reachability in methods where there are
//    finallies with multiple continuations.
//
bool Compiler::optReachable(BasicBlock* const fromBlock, BasicBlock* const toBlock, BasicBlock* const excludedBlock)
{
    if (fromBlock == toBlock)
    {
        return true;
    }

    for (BasicBlock* const block : Blocks())
    {
        block->bbFlags &= ~BBF_VISITED;
    }

    ArrayStack<BasicBlock*> stack(getAllocator(CMK_Reachability));
    stack.Push(fromBlock);

    while (!stack.Empty())
    {
        BasicBlock* const nextBlock = stack.Pop();
        nextBlock->bbFlags |= BBF_VISITED;
        assert(nextBlock != toBlock);

        if (nextBlock == excludedBlock)
        {
            continue;
        }

        for (BasicBlock* succ : nextBlock->GetAllSuccs(this))
        {
            if (succ == toBlock)
            {
                return true;
            }

            if ((succ->bbFlags & BBF_VISITED) != 0)
            {
                continue;
            }

            stack.Push(succ);
        }
    }

    return false;
}
