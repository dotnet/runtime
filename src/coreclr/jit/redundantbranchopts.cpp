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
                madeChanges |= m_compiler->optRedundantRelop(block);

                BasicBlock* bbNext = block->bbNext;
                BasicBlock* bbJump = block->bbJumpDest;

                madeChanges |= m_compiler->optRedundantBranch(block);

                // It's possible that either bbNext or bbJump were unlinked and it's proven
                // to be profitable to pay special attention to their successors.
                if (madeChanges && (bbNext->countOfInEdges() == 0))
                {
                    for (BasicBlock* succ : bbNext->Succs())
                    {
                        m_compiler->optRedundantBranch(succ);
                    }
                }

                if (madeChanges && (bbJump->countOfInEdges() == 0))
                {
                    for (BasicBlock* succ : bbJump->Succs())
                    {
                        m_compiler->optRedundantBranch(succ);
                    }
                }
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

static const ValueNumStore::VN_RELATION_KIND s_vnRelations[] = {ValueNumStore::VN_RELATION_KIND::VRK_Same,
                                                                ValueNumStore::VN_RELATION_KIND::VRK_Reverse,
                                                                ValueNumStore::VN_RELATION_KIND::VRK_Swap,
                                                                ValueNumStore::VN_RELATION_KIND::VRK_SwapReverse};

//------------------------------------------------------------------------
// RelopImplicationInfo
//
// Describes information needed to check for and describe the
// inferences between two relops.
//
struct RelopImplicationInfo
{
    // Dominating relop, whose value may be determined by control flow
    ValueNum domCmpNormVN = ValueNumStore::NoVN;
    // Dominated relop, whose value we would like to determine
    ValueNum treeNormVN = ValueNumStore::NoVN;
    // Relationship between the two relops, if any
    ValueNumStore::VN_RELATION_KIND vnRelation = ValueNumStore::VN_RELATION_KIND::VRK_Same;
    // Can we draw an inference?
    bool canInfer = false;
    // If canInfer and ominating relop is true, can we infer value of dominated relop?
    bool canInferFromTrue = true;
    // If canInfer and dominating relop is false, can we infer value of dominated relop?
    bool canInferFromFalse = true;
    // Reverse the sense of the inference
    bool reverseSense = false;
};

//------------------------------------------------------------------------
// optRedundantBranch: try and optimize a possibly redundant branch
//
// Arguments:
//   rii - struct with relop implication information
//
// Returns:
//   No return value.
//   Sets rii->canInfer and other fields, if inference is possible.
//
// Notes:
//
// First looks for exact or similar relations.
//
// If that fails, then looks for cases where the user or optOptimizeBools
// has combined two distinct predicates with a boolean AND, OR, or has wrapped
// a predicate in NOT.
//
// This will be expressed as  {NE/EQ}({AND/OR/NOT}(...), 0).
// If the operator is EQ then a true {AND/OR} result implies
// a false taken branch, so we need to invert the sense of our
// inferences.
//
// Note we could also infer the tree relop's value from other
// dominating relops, for example, (x >= 0) dominating (x > 0).
// That is left as a future enhancement.
//
void Compiler::optRelopImpliesRelop(RelopImplicationInfo* rii)
{
    assert(!rii->canInfer);

    // Look for related VNs
    //
    for (auto vnRelation : s_vnRelations)
    {
        const ValueNum relatedVN = vnStore->GetRelatedRelop(rii->domCmpNormVN, vnRelation);
        if ((relatedVN != ValueNumStore::NoVN) && (relatedVN == rii->treeNormVN))
        {
            rii->canInfer   = true;
            rii->vnRelation = vnRelation;
            return;
        }
    }

    // VNs are not directly related. See if dominating
    // compare encompasses a related VN.
    //
    VNFuncApp funcApp;
    if (!vnStore->GetVNFunc(rii->domCmpNormVN, &funcApp))
    {
        return;
    }

    genTreeOps const oper = genTreeOps(funcApp.m_func);

    // Look for {EQ,NE}({AND,OR,NOT}, 0)
    //
    if (!GenTree::StaticOperIs(oper, GT_EQ, GT_NE))
    {
        return;
    }

    const ValueNum constantVN = funcApp.m_args[1];
    if (constantVN != vnStore->VNZeroForType(TYP_INT))
    {
        return;
    }

    const ValueNum predVN = funcApp.m_args[0];
    VNFuncApp      predFuncApp;
    if (!vnStore->GetVNFunc(predVN, &predFuncApp))
    {
        return;
    }

    genTreeOps const predOper = genTreeOps(predFuncApp.m_func);

    if (!GenTree::StaticOperIs(predOper, GT_AND, GT_OR, GT_NOT))
    {
        return;
    }

    // Dominating compare is {EQ,NE}({AND,OR,NOT}, 0).
    //
    // See if one of {AND,OR,NOT} operands is related.
    //
    for (unsigned int i = 0; (i < predFuncApp.m_arity) && !rii->canInfer; i++)
    {
        ValueNum pVN = predFuncApp.m_args[i];

        for (auto vnRelation : s_vnRelations)
        {
            const ValueNum relatedVN = vnStore->GetRelatedRelop(pVN, vnRelation);

            if ((relatedVN != ValueNumStore::NoVN) && (relatedVN == rii->treeNormVN))
            {
                rii->vnRelation = vnRelation;
                rii->canInfer   = true;

                // If dom predicate is wrapped in EQ(*,0) then a true dom
                // predicate implies a false branch outcome, and vice versa.
                //
                // And if the dom predicate is GT_NOT we reverse yet again.
                //
                rii->reverseSense = (oper == GT_EQ) ^ (predOper == GT_NOT);

                // We only get partial knowledge in these cases.
                //
                //   AND(p1,p2) = true  ==> both p1 and p2 must be true
                //   AND(p1,p2) = false ==> don't know p1 or p2
                //    OR(p1,p2) = true  ==> don't know p1 or p2
                //    OR(p1,p2) = false ==> both p1 and p2 must be false
                //
                if (predOper != GT_NOT)
                {
                    rii->canInferFromFalse = rii->reverseSense ^ (predOper == GT_OR);
                    rii->canInferFromTrue  = rii->reverseSense ^ (predOper == GT_AND);
                }

                JITDUMP("Inferring predicate value from %s\n", GenTree::OpName(predOper));
                return;
            }
        }
    }
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

    if (!tree->OperIsCompare())
    {
        return false;
    }

    // Walk up the dom tree and see if any dominating block has branched on
    // exactly this tree's VN...
    //
    BasicBlock*    prevBlock   = block;
    BasicBlock*    domBlock    = block->bbIDom;
    int            relopValue  = -1;
    ValueNum       treeExcVN   = ValueNumStore::NoVN;
    ValueNum       domCmpExcVN = ValueNumStore::NoVN;
    unsigned       matchCount  = 0;
    const unsigned matchLimit  = 4;

    if (domBlock == nullptr)
    {
        return false;
    }

    // Unpack the tree's VN
    //
    ValueNum treeNormVN;
    vnStore->VNUnpackExc(tree->GetVN(VNK_Liberal), &treeNormVN, &treeExcVN);

    // If the treeVN is a constant, we optimize directly.
    //
    // Note the inferencing we do below is not valid for constant VNs,
    // so handling/avoiding this case up front is a correctness requirement.
    //
    if (vnStore->IsVNConstant(treeNormVN))
    {

        relopValue = (treeNormVN == vnStore->VNZeroForType(TYP_INT)) ? 0 : 1;
        JITDUMP("Relop [%06u] " FMT_BB " has known value %s\n ", dspTreeID(tree), block->bbNum,
                relopValue == 0 ? "false" : "true");
    }

    bool trySpeculativeDom = false;
    while ((relopValue == -1) && !trySpeculativeDom)
    {
        if (domBlock == nullptr)
        {
            // It's possible that bbIDom is not up to date at this point due to recent BB modifications
            // so let's try to quickly calculate new one
            domBlock = fgGetDomSpeculatively(block);
            if (domBlock == block->bbIDom)
            {
                // We already checked this one
                break;
            }
            trySpeculativeDom = true;
        }

        if (domBlock == nullptr)
        {
            break;
        }

        // Check the current dominator
        //
        if (domBlock->bbJumpKind == BBJ_COND)
        {
            Statement* const domJumpStmt = domBlock->lastStmt();
            GenTree* const   domJumpTree = domJumpStmt->GetRootNode();
            assert(domJumpTree->OperIs(GT_JTRUE));
            GenTree* const domCmpTree = domJumpTree->AsOp()->gtGetOp1();

            if (domCmpTree->OperIsCompare())
            {
                // We can use liberal VNs here, as bounds checks are not yet
                // manifest explicitly as relops.
                //
                RelopImplicationInfo rii;
                rii.treeNormVN = treeNormVN;
                vnStore->VNUnpackExc(domCmpTree->GetVN(VNK_Liberal), &rii.domCmpNormVN, &domCmpExcVN);

                // See if knowing the value of domCmpNormVN implies knowing the value of treeNormVN.
                //
                optRelopImpliesRelop(&rii);

                if (rii.canInfer)
                {
                    // If we have a long skinny dominator tree we may scale poorly,
                    // and in particular reachability (below) is costly. Give up if
                    // we've matched a few times and failed to optimize.
                    //
                    if (++matchCount > matchLimit)
                    {
                        JITDUMP("Bailing out; %d matches found w/o optimizing\n", matchCount);
                        return false;
                    }

                    // The compare in "tree" is redundant.
                    // Is there a unique path from the dominating compare?
                    //
                    JITDUMP("\nDominator " FMT_BB " of " FMT_BB " has relop with %s liberal VN\n", domBlock->bbNum,
                            block->bbNum, ValueNumStore::VNRelationString(rii.vnRelation));
                    DISPTREE(domCmpTree);
                    JITDUMP(" Redundant compare; current relop:\n");
                    DISPTREE(tree);

                    const bool domIsSameRelop = (rii.vnRelation == ValueNumStore::VN_RELATION_KIND::VRK_Same) ||
                                                (rii.vnRelation == ValueNumStore::VN_RELATION_KIND::VRK_Swap);

                    BasicBlock* const trueSuccessor  = domBlock->bbJumpDest;
                    BasicBlock* const falseSuccessor = domBlock->bbNext;

                    // If we can trace the flow from the dominating relop, we can infer its value.
                    //
                    const bool trueReaches  = optReachable(trueSuccessor, block, domBlock);
                    const bool falseReaches = optReachable(falseSuccessor, block, domBlock);

                    if (trueReaches && falseReaches && rii.canInferFromTrue && rii.canInferFromFalse)
                    {
                        // JIT-TP: it didn't produce diffs so let's skip it
                        if (trySpeculativeDom)
                        {
                            break;
                        }

                        // Both dominating compare outcomes reach the current block so we can't infer the
                        // value of the relop.
                        //
                        // However we may be able to update the flow from block's predecessors so they
                        // bypass block and instead transfer control to jump's successors (aka jump threading).
                        //
                        const bool wasThreaded = optJumpThread(block, domBlock, domIsSameRelop);

                        if (wasThreaded)
                        {
                            return true;
                        }
                    }
                    else if (trueReaches && !falseReaches && rii.canInferFromTrue)
                    {
                        // Taken jump in dominator reaches, fall through doesn't; relop must be true/false.
                        //
                        const bool relopIsTrue = rii.reverseSense ^ domIsSameRelop;
                        JITDUMP("Jump successor " FMT_BB " of " FMT_BB " reaches, relop [%06u] must be %s\n",
                                domBlock->bbJumpDest->bbNum, domBlock->bbNum, dspTreeID(tree),
                                relopIsTrue ? "true" : "false");
                        relopValue = relopIsTrue ? 1 : 0;
                        break;
                    }
                    else if (falseReaches && !trueReaches && rii.canInferFromFalse)
                    {
                        // Fall through from dominator reaches, taken jump doesn't; relop must be false/true.
                        //
                        const bool relopIsFalse = rii.reverseSense ^ domIsSameRelop;
                        JITDUMP("Fall through successor " FMT_BB " of " FMT_BB " reaches, relop [%06u] must be %s\n",
                                domBlock->bbNext->bbNum, domBlock->bbNum, dspTreeID(tree),
                                relopIsFalse ? "false" : "true");
                        relopValue = relopIsFalse ? 0 : 1;
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

    // Be conservative if there is an exception effect and we're in an EH region
    // as we might not model the full extent of EH flow.
    //
    if (((tree->gtFlags & GTF_EXCEPT) != 0) && block->hasTryIndex())
    {
        JITDUMP("Current relop has exception side effect and is in a try, so we won't optimize\n");
        return false;
    }

    // Handle the side effects: for exceptions we can know whether we can drop them using the exception sets.
    // Other side effects we always leave around (the unused tree will be appropriately transformed by morph).
    //
    bool keepTreeForSideEffects = false;
    if ((tree->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        keepTreeForSideEffects = true;

        if (((tree->gtFlags & GTF_SIDE_EFFECT) == GTF_EXCEPT) && vnStore->VNExcIsSubset(domCmpExcVN, treeExcVN))
        {
            keepTreeForSideEffects = false;
        }
    }

    if (keepTreeForSideEffects)
    {
        JITDUMP("Current relop has side effects, keeping it, unused\n");
        GenTree* relopComma       = gtNewOperNode(GT_COMMA, TYP_INT, tree, gtNewIconNode(relopValue));
        jumpTree->AsUnOp()->gtOp1 = relopComma;
    }
    else
    {
        tree->BashToConst(relopValue);
    }

    JITDUMP("\nRedundant branch opt in " FMT_BB ":\n", block->bbNum);

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
//   domIsSameRelop - if true, dominating block does the same compare;
//                    if false, dominating block does a reverse compare
//
// Returns:
//   True if the branch was optimized.
//
// Notes:
//
// Conceptually this just transforms flow as follows:
//
//     domBlock           domBlock
//    /       |          /       |
//    Ts      Fs         Ts      Fs    True/False successor
//   ....    ....       ....    ....
//    Tp      Fp         Tp      Fp    True/False pred
//     \     /           |       |
//      \   /            |       |
//      block     ==>    |       |
//      /   \            |       |
//     /     \           |       |
//    Tt     Ft          Tt      Ft    True/false target
//
// However we may try to re-purpose block, and so end up producing flow more like this:
//
//     domBlock           domBlock
//    /       |          /       |
//    Ts      Fs         Ts      Fs    True/False successor
//   ....    ....       ....    ....
//    Tp      Fp         Tp      Fp    True/False pred
//     \     /           |       |
//      \   /            |       |
//      block     ==>    |      block   (repurposed)
//      /   \            |       |
//     /     \           |       |
//    Tt     Ft          Tt      Ft    True/false target
//
bool Compiler::optJumpThread(BasicBlock* const block, BasicBlock* const domBlock, bool domIsSameRelop)
{
    assert(block->bbJumpKind == BBJ_COND);
    assert(domBlock->bbJumpKind == BBJ_COND);

    if (fgCurBBEpochSize != (fgBBNumMax + 1))
    {
        JITDUMP("Looks like we've added a new block (e.g. during optLoopHoist) since last renumber, so no threading\n");
        return false;
    }

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
                    // However, be conservative if the blocks are not in the
                    // same EH region, as we might not be able to fully
                    // describe control flow between them.
                    //
                    if (BasicBlock::sameEHRegion(block, domBlock))
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
    // To do this we try and follow the flow from domBlock to block. When domIsSameRelop
    // is true, any block pred reachable from domBlock's true edge is a true pred of block,
    // and any block pred reachable from domBlock's false edge is a false pred of block.
    //
    // If domIsSameRelop is false, then the roles of the of the paths from domBlock swap:
    // any block pred reachable from domBlock's true edge is a false pred of block,
    // and any block pred reachable from domBlock's false edge is a true pred of block.
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
    // * We note if there is an un-ambiguous pred that falls through to block.
    // This is the "fall through pred", and the (true/false) pred set it belongs to
    // is the "fall through set".
    //
    // Now for some case analysis.
    //
    // (1) If we have both an ambiguous pred and a fall through pred, we treat
    // the fall through pred as an ambiguous pred (we can't reroute its flow to
    // avoid block, and we need to keep block intact), and jump thread the other
    // preds per (2) below.
    //
    // (2) If we have an ambiguous pred and no fall through, we reroute the true and
    // false preds to branch to the true and false successors, respectively.
    //
    // (3) If we don't have an ambiguous pred and don't have a fall through pred,
    // we choose one of the pred sets to be treated as if it was the fall through set.
    // For now the choice is arbitrary, so we chose the true preds, and proceed
    // per (4) below.
    //
    // (4) If we don't have an ambiguous pred, and we have a fall through, we leave
    // all preds in the fall through set alone -- they continue branching to block.
    // We modify block to branch to the appropriate successor for the fall through set.
    // Note block will be empty other than phis and the branch, so this is ok.
    // The preds in the other set target the other successor.
    //
    // The goal of the above is to maximize the number of cases where we jump thread,
    // and to maximize the number of jump threads that reuse the original block. This
    // latter should prove useful in subsequent work, where we aim to enable jump
    // threading in cases where block has side effects.
    //
    int               numPreds          = 0;
    int               numAmbiguousPreds = 0;
    int               numTruePreds      = 0;
    int               numFalsePreds     = 0;
    BasicBlock*       fallThroughPred   = nullptr;
    BasicBlock* const trueSuccessor     = domIsSameRelop ? domBlock->bbJumpDest : domBlock->bbNext;
    BasicBlock* const falseSuccessor    = domIsSameRelop ? domBlock->bbNext : domBlock->bbJumpDest;
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
        // Treat the fall through pred as an ambiguous pred.
        JITDUMP(FMT_BB " has both ambiguous preds and a fall through pred\n", block->bbNum);
        JITDUMP("Treating fall through pred " FMT_BB " as an ambiguous pred\n", fallThroughPred->bbNum);

        if (BlockSetOps::IsMember(this, truePreds, fallThroughPred->bbNum))
        {
            BlockSetOps::RemoveElemD(this, truePreds, fallThroughPred->bbNum);
            assert(numTruePreds > 0);
            numTruePreds--;
        }
        else
        {
            assert(numFalsePreds > 0);
            numFalsePreds--;
        }

        assert(!(BlockSetOps::IsMember(this, ambiguousPreds, fallThroughPred->bbNum)));
        BlockSetOps::AddElemD(this, ambiguousPreds, fallThroughPred->bbNum);
        numAmbiguousPreds++;
        fallThroughPred = nullptr;
    }

    // Determine if either set of preds will route via block.
    //
    bool truePredsWillReuseBlock  = false;
    bool falsePredsWillReuseBlock = false;

    if (fallThroughPred != nullptr)
    {
        assert(numAmbiguousPreds == 0);
        truePredsWillReuseBlock  = BlockSetOps::IsMember(this, truePreds, fallThroughPred->bbNum);
        falsePredsWillReuseBlock = !truePredsWillReuseBlock;
    }
    else if (numAmbiguousPreds == 0)
    {
        truePredsWillReuseBlock  = true;
        falsePredsWillReuseBlock = !truePredsWillReuseBlock;
    }

    assert(!(truePredsWillReuseBlock && falsePredsWillReuseBlock));

    // We should be good to go
    //
    JITDUMP("Optimizing via jump threading\n");

    // Fix block, if we're reusing it.
    //
    if (truePredsWillReuseBlock)
    {
        Statement* lastStmt = block->lastStmt();
        fgRemoveStmt(block, lastStmt);
        JITDUMP("  repurposing " FMT_BB " to always jump to " FMT_BB "\n", block->bbNum, trueTarget->bbNum);
        fgRemoveRefPred(block->bbNext, block);
        block->bbJumpKind = BBJ_ALWAYS;
    }
    else if (falsePredsWillReuseBlock)
    {
        Statement* lastStmt = block->lastStmt();
        fgRemoveStmt(block, lastStmt);
        JITDUMP("  repurposing " FMT_BB " to always fall through to " FMT_BB "\n", block->bbNum, falseTarget->bbNum);
        fgRemoveRefPred(block->bbJumpDest, block);
        block->bbJumpKind = BBJ_NONE;
    }

    // Now reroute the flow from the predecessors.
    // If this pred is in the set that will reuse block, do nothing.
    // Else revise pred to branch directly to the appropriate successor of block.
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

        // Do we need to alter flow from this pred?
        //
        if ((isTruePred && truePredsWillReuseBlock) || (!isTruePred && falsePredsWillReuseBlock))
        {
            // No, we can leave as is.
            //
            JITDUMP("%s pred " FMT_BB " will continue to target " FMT_BB "\n", isTruePred ? "true" : "false",
                    predBlock->bbNum, block->bbNum);
            continue;
        }

        // Yes, we need to jump to the appropriate successor.
        // Note we should not be altering flow for the fall-through pred.
        //
        assert(predBlock != fallThroughPred);
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

    // We optimized.
    //
    fgModified = true;
    return true;
}

//------------------------------------------------------------------------
// optRedundantRelop: see if the value of tree is redundant given earlier
//   relops in this block.
//
// Arguments:
//    block - block of interest (BBJ_COND)
//
// Returns:
//    true, if changes were made.
//
// Notes:
//
// Here's a walkthrough of how this operates. Given a block like
//
// STMT00388 (IL 0x30D...  ???)
//  *  ASG       ref    <l:$9d3, c:$9d4>
//  +--*  LCL_VAR   ref    V121 tmp97       d:1 <l:$2c8, c:$99f>
//   \--*  IND       ref    <l:$9d3, c:$9d4>
//       \--*  LCL_VAR   byref  V116 tmp92       u:1 (last use) Zero Fseq[m_task] $18c
//
// STMT00390 (IL 0x30D...  ???)
//  *  ASG       bool   <l:$8ff, c:$a02>
//  +--*  LCL_VAR   int    V123 tmp99       d:1 <l:$8ff, c:$a02>
//  \--*  NE        int    <l:$8ff, c:$a02>
//     +--*  LCL_VAR   ref    V121 tmp97       u:1 <l:$2c8, c:$99f>
//     \--*  CNS_INT   ref    null $VN.Null
//
// STMT00391
//  *  ASG       ref    $133
//  +--*  LCL_VAR   ref    V124 tmp100      d:1 $133
//  \--*  IND       ref    $133
//     \--*  CNS_INT(h) long   0x31BD3020 [ICON_STR_HDL] $34f
//
// STMT00392
//  *  JTRUE     void
//  \--*  NE        int    <l:$8ff, c:$a02>
//     +--*  LCL_VAR   int    V123 tmp99       u:1 (last use) <l:$8ff, c:$a02>
//     \--*  CNS_INT   int    0 $40
//
// We will first consider STMT00391. It is a local assign but the RHS value number
// isn't related to $8ff. So we continue searching and add V124 to the array
// of defined locals.
//
// Next we consider STMT00390. It is a local assign and the RHS value number is
// the same, $8ff. So this compare is a fwd-sub candidate. We check if any local
// on the RHS is in the defined locals array. The answer is no. So the RHS tree
// can be safely forwarded in place of the compare in STMT00392. We check if V123 is
// live out of the block. The answer is no. So This RHS tree becomes the candidate tree.
// We add V123 to the array of defined locals and keep searching.
//
// Next we consider STMT00388, It is a local assign but the RHS value number
// isn't related to $8ff. So we continue searching and add V121 to the array
// of defined locals.
//
// We reach the end of the block and stop searching.
//
// Since we found a viable candidate, we clone it and substitute into the jump:
//
// STMT00388 (IL 0x30D...  ???)
//  *  ASG       ref    <l:$9d3, c:$9d4>
//  +--*  LCL_VAR   ref    V121 tmp97       d:1 <l:$2c8, c:$99f>
//   \--*  IND       ref    <l:$9d3, c:$9d4>
//       \--*  LCL_VAR   byref  V116 tmp92       u:1 (last use) Zero Fseq[m_task] $18c
//
// STMT00390 (IL 0x30D...  ???)
//  *  ASG       bool   <l:$8ff, c:$a02>
//  +--*  LCL_VAR   int    V123 tmp99       d:1 <l:$8ff, c:$a02>
//  \--*  NE        int    <l:$8ff, c:$a02>
//     +--*  LCL_VAR   ref    V121 tmp97       u:1 <l:$2c8, c:$99f>
//     \--*  CNS_INT   ref    null $VN.Null
//
// STMT00391
//  *  ASG       ref    $133
//  +--*  LCL_VAR   ref    V124 tmp100      d:1 $133
//  \--*  IND       ref    $133
//     \--*  CNS_INT(h) long   0x31BD3020 [ICON_STR_HDL] $34f
//
// STMT00392
//  *  JTRUE     void
//  \--*  NE        int    <l:$8ff, c:$a02>
//     +--*  LCL_VAR   ref    V121 tmp97       u:1 <l:$2c8, c:$99f>
//     \--*  CNS_INT   ref    null $VN.Null
//
// We anticipate that STMT00390 will become dead code, and if and so we've
// eliminated one of the two compares in the block.
//
bool Compiler::optRedundantRelop(BasicBlock* const block)
{
    Statement* const stmt = block->lastStmt();

    if (stmt == nullptr)
    {
        return false;
    }

    // If there's just one statement, bail.
    //
    if (stmt == block->firstStmt())
    {
        return false;
    }

    GenTree* const jumpTree = stmt->GetRootNode();

    if (!jumpTree->OperIs(GT_JTRUE))
    {
        return false;
    }

    GenTree* const tree = jumpTree->AsOp()->gtOp1;

    if (!tree->OperIsCompare())
    {
        return false;
    }

    // If tree has side effects other than GTF_EXCEPT, bail.
    //
    if ((tree->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        if ((tree->gtFlags & GTF_SIDE_EFFECT) != GTF_EXCEPT)
        {
            return false;
        }
    }

    // If relop's value is known, bail.
    //
    const ValueNum treeVN = vnStore->VNNormalValue(tree->GetVN(VNK_Liberal));

    if (vnStore->IsVNConstant(treeVN))
    {
        JITDUMP(" -- no, jump tree cond is constant\n");
        return false;
    }

    // Save off the jump tree's liberal exceptional VN.
    //
    const ValueNum treeExcVN = vnStore->VNExceptionSet(tree->GetVN(VNK_Liberal));

    JITDUMP("\noptRedundantRelop in " FMT_BB "; jump tree is\n", block->bbNum);
    DISPTREE(jumpTree);

    // We're going to search back to find the earliest tree in block that
    //  * makes the current relop redundant;
    //  * can safely and profitably forward substituted to the jump.
    //
    Statement*                      prevStmt            = stmt;
    GenTree*                        candidateTree       = nullptr;
    Statement*                      candidateStmt       = nullptr;
    ValueNumStore::VN_RELATION_KIND candidateVnRelation = ValueNumStore::VN_RELATION_KIND::VRK_Same;
    bool                            sideEffect          = false;

    // We need to keep track of which locals might be killed by
    // the trees between the expression we want to forward substitute
    // and the jump.
    //
    // We don't use a varset here because we are indexing by local ID,
    // not by tracked index.
    //
    // The table size here also implicitly limits how far back we'll search.
    //
    enum
    {
        DEFINED_LOCALS_SIZE = 10
    };
    unsigned definedLocals[DEFINED_LOCALS_SIZE];
    unsigned definedLocalsCount = 0;

    while (true)
    {
        // If we've run a cross a side effecting pred tree, stop looking.
        //
        if (sideEffect)
        {
            break;
        }

        prevStmt = prevStmt->GetPrevStmt();

        // Backwards statement walks wrap around, so if we get
        // back to stmt we've seen everything there is to see.
        //
        if (prevStmt == stmt)
        {
            break;
        }

        // We are looking for ASG(lcl, ...)
        //
        GenTree* const prevTree = prevStmt->GetRootNode();

        JITDUMP(" ... checking previous tree\n");
        DISPTREE(prevTree);

        // Ignore nops.
        //
        if (prevTree->OperIs(GT_NOP))
        {
            continue;
        }

        // If prevTree has side effects, bail,
        // unless it is in the immediately preceding statement.
        //
        // (we'll later show that any exception must come from the RHS as the LHS
        // will be a simple local).
        //
        if ((prevTree->gtFlags & (GTF_CALL | GTF_ORDER_SIDEEFF)) != 0)
        {
            if (prevStmt->GetNextStmt() != stmt)
            {
                JITDUMP(" -- prev tree has side effects and is not next to jumpTree\n");
                break;
            }

            JITDUMP(" -- prev tree has side effects, allowing as prev tree is immediately before jumpTree\n");
            sideEffect = true;
        }

        if (!prevTree->OperIs(GT_ASG))
        {
            JITDUMP(" -- prev tree not ASG\n");
            break;
        }

        GenTree* const prevTreeLHS = prevTree->AsOp()->gtOp1;
        GenTree* const prevTreeRHS = prevTree->AsOp()->gtOp2;

        if (!prevTreeLHS->OperIs(GT_LCL_VAR))
        {
            JITDUMP(" -- prev tree not ASG(LCL...)\n");
            break;
        }

        // If we are seeing PHIs we have run out of interesting stmts.
        //
        if (prevTreeRHS->OperIs(GT_PHI))
        {
            JITDUMP(" -- prev tree is a phi\n");
            break;
        }

        // Bail if RHS has an embedded assignment. We could handle this
        // if we generalized the interference check we run below.
        //
        if ((prevTreeRHS->gtFlags & GTF_ASG) != 0)
        {
            JITDUMP(" -- prev tree RHS has embedded assignment\n");
            break;
        }

        // Figure out what local is assigned here.
        //
        const unsigned   prevTreeLcl    = prevTreeLHS->AsLclVarCommon()->GetLclNum();
        LclVarDsc* const prevTreeLclDsc = lvaGetDesc(prevTreeLcl);

        // If local is not tracked, assume we can't safely reason about interference
        // or liveness.
        //
        if (!prevTreeLclDsc->lvTracked)
        {
            JITDUMP(" -- prev tree defs untracked V%02u\n", prevTreeLcl);
            break;
        }

        // If we've run out of room to keep track of defined locals, bail.
        //
        if (definedLocalsCount >= DEFINED_LOCALS_SIZE)
        {
            JITDUMP(" -- ran out of space for tracking kills\n");
            break;
        }

        definedLocals[definedLocalsCount++] = prevTreeLcl;

        // If the normal liberal VN of RHS is the normal liberal VN of the current tree, or is "related",
        // consider forward sub.
        //
        const ValueNum                  domCmpVN        = vnStore->VNNormalValue(prevTreeRHS->GetVN(VNK_Liberal));
        bool                            matched         = false;
        ValueNumStore::VN_RELATION_KIND vnRelationMatch = ValueNumStore::VN_RELATION_KIND::VRK_Same;

        for (auto vnRelation : s_vnRelations)
        {
            const ValueNum relatedVN = vnStore->GetRelatedRelop(domCmpVN, vnRelation);
            if ((relatedVN != ValueNumStore::NoVN) && (relatedVN == treeVN))
            {
                vnRelationMatch = vnRelation;
                matched         = true;
                break;
            }
        }

        if (!matched)
        {
            JITDUMP(" -- prev tree VN is not related\n");
            continue;
        }

        JITDUMP("  -- prev tree has relop with %s liberal VN\n", ValueNumStore::VNRelationString(vnRelationMatch));

        // If the jump tree VN has exceptions, verify that the RHS tree has a superset.
        //
        if (treeExcVN != vnStore->VNForEmptyExcSet())
        {
            const ValueNum prevTreeExcVN = vnStore->VNExceptionSet(prevTreeRHS->GetVN(VNK_Liberal));

            if (!vnStore->VNExcIsSubset(prevTreeExcVN, treeExcVN))
            {
                JITDUMP(" -- prev tree does not anticipate all jump tree exceptions\n");
                break;
            }
        }

        // See if we can safely move a copy of prevTreeRHS later, to replace tree.
        // We can, if none of its lcls are killed.
        //
        bool interferes = false;

        for (unsigned int i = 0; i < definedLocalsCount; i++)
        {
            if (gtHasRef(prevTreeRHS, definedLocals[i]))
            {
                JITDUMP(" -- prev tree ref to V%02u interferes\n", definedLocals[i]);
                interferes = true;
                break;
            }
        }

        if (interferes)
        {
            break;
        }

        // Heuristic: only forward sub a relop
        //
        if (!prevTreeRHS->OperIsCompare())
        {
            JITDUMP(" -- prev tree is not relop\n");
            continue;
        }

        // If the lcl defined here is live out, forward sub is problematic.
        // We'll either create a redundant tree (as the original won't be dead)
        // or lose the def (if we actually move the RHS tree).
        //
        if (VarSetOps::IsMember(this, block->bbLiveOut, prevTreeLclDsc->lvVarIndex))
        {
            JITDUMP(" -- prev tree lcl V%02u is live-out\n", prevTreeLcl);
            continue;
        }

        JITDUMP(" -- prev tree is viable candidate for relop fwd sub!\n");
        candidateTree       = prevTreeRHS;
        candidateStmt       = prevStmt;
        candidateVnRelation = vnRelationMatch;
    }

    if (candidateTree == nullptr)
    {
        return false;
    }

    GenTree* substituteTree = nullptr;
    bool     usedCopy       = false;

    if (candidateStmt->GetNextStmt() == stmt)
    {
        // We are going forward-sub candidateTree
        //
        substituteTree = candidateTree;
    }
    else
    {
        // We going to forward-sub a copy of candidateTree
        //
        assert(!sideEffect);
        substituteTree = gtCloneExpr(candidateTree);
        usedCopy       = true;
    }

    // If we need the reverse compare, make it so.
    // We also need to set a proper VN.
    //
    if ((candidateVnRelation == ValueNumStore::VN_RELATION_KIND::VRK_Reverse) ||
        (candidateVnRelation == ValueNumStore::VN_RELATION_KIND::VRK_SwapReverse))
    {
        // Copy the vn info as it will be trashed when we change the oper.
        //
        ValueNumPair origVNP = substituteTree->gtVNPair;

        // Update the tree. Note we don't actually swap operands...?
        //
        substituteTree->SetOper(GenTree::ReverseRelop(substituteTree->OperGet()));

        // Compute the right set of VNs for this new tree.
        //
        ValueNum origNormConVN = vnStore->VNConservativeNormalValue(origVNP);
        ValueNum origNormLibVN = vnStore->VNLiberalNormalValue(origVNP);
        ValueNum newNormConVN  = vnStore->GetRelatedRelop(origNormConVN, ValueNumStore::VN_RELATION_KIND::VRK_Reverse);
        ValueNum newNormLibVN  = vnStore->GetRelatedRelop(origNormLibVN, ValueNumStore::VN_RELATION_KIND::VRK_Reverse);
        ValueNumPair newNormalVNP(newNormLibVN, newNormConVN);
        ValueNumPair origExcVNP = vnStore->VNPExceptionSet(origVNP);
        ValueNumPair newVNP     = vnStore->VNPWithExc(newNormalVNP, origExcVNP);

        substituteTree->SetVNs(newVNP);
    }

    // This relop is now a subtree of a jump.
    //
    substituteTree->gtFlags |= (GTF_RELOP_JMP_USED | GTF_DONT_CSE);

    // Swap in the new tree.
    //
    GenTree** const treeUse = &(jumpTree->AsOp()->gtOp1);
    jumpTree->ReplaceOperand(treeUse, substituteTree);
    fgSetStmtSeq(stmt);
    gtUpdateStmtSideEffects(stmt);

    DEBUG_DESTROY_NODE(tree);

    // If we didn't forward sub a copy, the candidateStmt must be removed.
    //
    if (!usedCopy)
    {
        fgRemoveStmt(block, candidateStmt);
    }

    JITDUMP(" -- done! new jump tree is\n");
    DISPTREE(jumpTree);

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
