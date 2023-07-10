// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "fgprofilesynthesis.h"

// TODO
//
// * faster way of doing fgGetPredForBlock
// * vet against some real data
// * IR based heuristics (perhaps)
// * During Cp, avoid repeatedly propagating through nested loops
// * Fake BB0 or always force scratch BB
// * Reconcile with our other loop finding stuff
// * Stop the upweight/downweight of loops in rest of jit
// * Durable edge properties (exit, back)
// * Tweak RunRarely to be at or near zero
// * OSR entry weight
// * Special handling for deep nests?
// * Plan for irreducible cases -- MoveNext's

//------------------------------------------------------------------------
// fgProfileSynthesis: update edge likelihoods and block counts based
//   on various strategies
//
// Arguments:
//   options - options to control synthesis
//
void ProfileSynthesis::Run(ProfileSynthesisOption option)
{
    BuildReversePostorder();
    FindLoops();

    // Retain or compute edge likelihood information
    //
    switch (option)
    {
        case ProfileSynthesisOption::AssignLikelihoods:
            AssignLikelihoods();
            break;

        case ProfileSynthesisOption::RetainLikelihoods:
            break;

        case ProfileSynthesisOption::BlendLikelihoods:
            BlendLikelihoods();
            break;

        case ProfileSynthesisOption::ResetAndSynthesize:
            ClearLikelihoods();
            AssignLikelihoods();
            break;

        case ProfileSynthesisOption::ReverseLikelihoods:
            ReverseLikelihoods();
            break;

        case ProfileSynthesisOption::RandomLikelihoods:
            RandomizeLikelihoods();
            break;

        case ProfileSynthesisOption::RepairLikelihoods:
            RepairLikelihoods();
            break;

        default:
            assert(!"unexpected profile synthesis option");
            break;
    }

    // Determine cyclic probabilities
    //
    ComputeCyclicProbabilities();

    // Assign weights to entry points in the flow graph
    //
    AssignInputWeights(option);

    // Compute the block weights given the inputs and edge likelihoods
    //
    ComputeBlockWeights();

    // Update pgo info
    //
    const bool             hadPgoWeights = m_comp->fgPgoHaveWeights;
    ICorJitInfo::PgoSource newSource     = ICorJitInfo::PgoSource::Synthesis;

    if (option == ProfileSynthesisOption::RepairLikelihoods)
    {
        newSource = m_comp->fgPgoSource;
    }
    else if (hadPgoWeights && (option == ProfileSynthesisOption::BlendLikelihoods))
    {
        newSource = ICorJitInfo::PgoSource::Blend;
    }

    m_comp->fgPgoHaveWeights = true;
    m_comp->fgPgoSource      = newSource;

#ifdef DEBUG
    if (JitConfig.JitCheckSynthesizedCounts() > 0)
    {
        // Verify consistency, provided we didn't see any improper headers
        // or cap any Cp values.
        //
        if ((m_improperLoopHeaders == 0) && (m_cappedCyclicProbabilities == 0))
        {
            // verify likely weights, assert on failure, check all blocks
            m_comp->fgDebugCheckProfileWeights(ProfileChecks::CHECK_LIKELY | ProfileChecks::RAISE_ASSERT |
                                               ProfileChecks::CHECK_ALL_BLOCKS);
        }
    }
#endif
}

//------------------------------------------------------------------------
// AssignLikelihoods: update edge likelihoods and block counts based
//   entirely on heuristics.
//
// Notes:
//   any existing likelihoods are removed in favor of heuristic
//   likelihoods
//
void ProfileSynthesis::AssignLikelihoods()
{
    JITDUMP("Assigning edge likelihoods based on heuristics\n");

    for (BasicBlock* const block : m_comp->Blocks())
    {
        switch (block->bbJumpKind)
        {
            case BBJ_THROW:
            case BBJ_RETURN:
            case BBJ_EHFINALLYRET:
            case BBJ_EHFAULTRET:
                // No successor cases
                // (todo: finally ret may have succs)
                break;

            case BBJ_NONE:
            case BBJ_CALLFINALLY:
                // Single successor next cases
                //
                // Note we handle flow to the finally
                // specially; this represents return
                // from the finally.
                AssignLikelihoodNext(block);
                break;

            case BBJ_ALWAYS:
            case BBJ_LEAVE:
            case BBJ_EHCATCHRET:
            case BBJ_EHFILTERRET:
                // Single successor jump cases
                AssignLikelihoodJump(block);
                break;

            case BBJ_COND:
                // Two successor cases
                AssignLikelihoodCond(block);
                break;

            case BBJ_SWITCH:
                // N successor cases
                AssignLikelihoodSwitch(block);
                break;

            default:
                unreached();
        }
    }
}

//------------------------------------------------------------------------
// IsDfsAncestor: see if block `x` is ancestor of block `y` in the depth
//   first spanning tree
//
// Arguments:
//   x -- block that is possible ancestor
//   y -- block that is possible descendant
//
// Returns:
//   True if x is ancestor of y in the depth first spanning tree.
//
// Notes:
//   If return value is false, then x does not dominate y.
//
bool ProfileSynthesis::IsDfsAncestor(BasicBlock* x, BasicBlock* y)
{
    return ((x->bbPreorderNum <= y->bbPreorderNum) && (y->bbPostorderNum <= x->bbPostorderNum));
}

//------------------------------------------------------------------------
// GetLoopFromHeader: see if a block is a loop header, and if so return
//   the associated loop.
//
// Arguments:
//    block - block in question
//
// Returns:
//    loop headed by block, or nullptr
//
SimpleLoop* ProfileSynthesis::GetLoopFromHeader(BasicBlock* block)
{
    for (SimpleLoop* loop : *m_loops)
    {
        if (loop->m_head == block)
        {
            return loop;
        }
    }

    return nullptr;
}

//------------------------------------------------------------------------
// IsLoopBackEdge: see if an edge is a loop back edge
//
// Arguments:
//   edge - edge in question
//
// Returns:
//   True if edge is a backedge in some recognized loop.
//
// Notes:
//   Different than asking IsDfsAncestor since we disqualify some
//   natural backedges for complex loop strctures.
//
// Todo:
//   Annotate the edge directly
//
bool ProfileSynthesis::IsLoopBackEdge(FlowEdge* edge)
{
    for (SimpleLoop* loop : *m_loops)
    {
        for (FlowEdge* loopBackEdge : loop->m_backEdges)
        {
            if (loopBackEdge == edge)
            {
                return true;
            }
        }
    }

    return false;
}

//------------------------------------------------------------------------
// IsLoopExitEdge: see if a flow edge is a loop exit edge
//
// Arguments:
//   edge - edge in question
//
// Returns:
//   True if edge is an exit edge in some recognized loop
//
// Todo:
//   Annotate the edge directly
//
//   Decide if we want to report that the edge exits
//   multiple loops.

bool ProfileSynthesis::IsLoopExitEdge(FlowEdge* edge)
{
    for (SimpleLoop* loop : *m_loops)
    {
        for (FlowEdge* loopExitEdge : loop->m_exitEdges)
        {
            if (loopExitEdge == edge)
            {
                return true;
            }
        }
    }

    return false;
}

//------------------------------------------------------------------------
// AssignLikelihoodNext: update edge likelihood for block that always
//   transfers control to bbNext
//
// Arguments;
//   block -- block in question
//
void ProfileSynthesis::AssignLikelihoodNext(BasicBlock* block)
{
    FlowEdge* const edge = m_comp->fgGetPredForBlock(block->bbNext, block);
    edge->setLikelihood(1.0);
}

//------------------------------------------------------------------------
// AssignLikelihoodJump: update edge likelihood for a block that always
//   transfers control to bbJumpDest
//
// Arguments;
//   block -- block in question
//
void ProfileSynthesis::AssignLikelihoodJump(BasicBlock* block)
{
    FlowEdge* const edge = m_comp->fgGetPredForBlock(block->bbJumpDest, block);
    edge->setLikelihood(1.0);
}

//------------------------------------------------------------------------
// AssignLikelihoodCond: update edge likelihood for a block that
//   ends in a conditional branch
//
// Arguments;
//   block -- block in question (BBJ_COND)
//
void ProfileSynthesis::AssignLikelihoodCond(BasicBlock* block)
{
    BasicBlock* const jump = block->bbJumpDest;
    BasicBlock* const next = block->bbNext;

    // Watch for degenerate case
    //
    if (jump == next)
    {
        AssignLikelihoodNext(block);
        return;
    }

    FlowEdge* const jumpEdge = m_comp->fgGetPredForBlock(jump, block);
    FlowEdge* const nextEdge = m_comp->fgGetPredForBlock(next, block);

    // THROW heuristic
    //
    bool const isJumpThrow = (jump->bbJumpKind == BBJ_THROW);
    bool const isNextThrow = (next->bbJumpKind == BBJ_THROW);

    if (isJumpThrow != isNextThrow)
    {
        if (isJumpThrow)
        {
            jumpEdge->setLikelihood(0.0);
            nextEdge->setLikelihood(1.0);
        }
        else
        {
            jumpEdge->setLikelihood(1.0);
            nextEdge->setLikelihood(0.0);
        }

        return;
    }

    // LOOP BACK EDGE heuristic
    //
    bool const isJumpEdgeBackEdge = IsLoopBackEdge(jumpEdge);
    bool const isNextEdgeBackEdge = IsLoopBackEdge(nextEdge);

    if (isJumpEdgeBackEdge != isNextEdgeBackEdge)
    {
        if (isJumpEdgeBackEdge)
        {
            JITDUMP(FMT_BB "->" FMT_BB " is loop back edge\n", block->bbNum, jump->bbNum);
            jumpEdge->setLikelihood(loopBackLikelihood);
            nextEdge->setLikelihood(1.0 - loopBackLikelihood);
        }
        else
        {
            JITDUMP(FMT_BB "->" FMT_BB " is loop back edge\n", block->bbNum, next->bbNum);
            jumpEdge->setLikelihood(1.0 - loopBackLikelihood);
            nextEdge->setLikelihood(loopBackLikelihood);
        }

        return;
    }

    // LOOP EXIT EDGE heuristic
    //
    // Consider: adjust probability if loop has multiple exit edges, so that
    // overall exit probability is around 0.1.
    //
    bool const isJumpEdgeExitEdge = IsLoopExitEdge(jumpEdge);
    bool const isNextEdgeExitEdge = IsLoopExitEdge(nextEdge);

    if (isJumpEdgeExitEdge != isNextEdgeExitEdge)
    {
        if (isJumpEdgeExitEdge)
        {
            JITDUMP(FMT_BB "->" FMT_BB " is loop exit edge\n", block->bbNum, jump->bbNum);
            jumpEdge->setLikelihood(1.0 - loopExitLikelihood);
            nextEdge->setLikelihood(loopExitLikelihood);
        }
        else
        {
            JITDUMP(FMT_BB "->" FMT_BB " is loop exit edge\n", block->bbNum, next->bbNum);
            jumpEdge->setLikelihood(loopExitLikelihood);
            nextEdge->setLikelihood(1.0 - loopExitLikelihood);
        }

        return;
    }

    // RETURN heuristic
    //
    bool const isJumpReturn = (jump->bbJumpKind == BBJ_RETURN);
    bool const isNextReturn = (next->bbJumpKind == BBJ_RETURN);

    if (isJumpReturn != isNextReturn)
    {
        if (isJumpReturn)
        {
            jumpEdge->setLikelihood(returnLikelihood);
            nextEdge->setLikelihood(1.0 - returnLikelihood);
        }
        else
        {
            jumpEdge->setLikelihood(1.0 - returnLikelihood);
            nextEdge->setLikelihood(returnLikelihood);
        }

        return;
    }

    // IL OFFSET heuristic
    //
    // Give slight preference to bbNext
    //
    jumpEdge->setLikelihood(1.0 - ilNextLikelihood);
    nextEdge->setLikelihood(ilNextLikelihood);
}

//------------------------------------------------------------------------
// AssignLikelihoodSwitch: update edge likelihood for a block that
//   ends in a switch
//
// Arguments;
//   block -- block in question (BBJ_SWITCH)
//
void ProfileSynthesis::AssignLikelihoodSwitch(BasicBlock* block)
{
    // Assume each switch case is equally probable
    //
    const unsigned n = block->NumSucc();
    assert(n != 0);
    const weight_t p = 1 / (weight_t)n;

    // Each unique edge gets some multiple of that basic probability
    //
    for (BasicBlock* const succ : block->Succs(m_comp))
    {
        FlowEdge* const edge = m_comp->fgGetPredForBlock(succ, block);
        edge->setLikelihood(p * edge->getDupCount());
    }
}

//------------------------------------------------------------------------
// SumOutgoingLikelihoods: sum existing likelihoods for exiting a block
//
// Arguments:
//   block -- block in question
//   likelihoods -- [optional, out] vector to fill in with the outgoing likelihoods
//
// Returns:
//   Sum of likelihoods of each successor
//
weight_t ProfileSynthesis::SumOutgoingLikelihoods(BasicBlock* block, WeightVector* likelihoods)
{
    weight_t sum = 0;

    if (likelihoods != nullptr)
    {
        likelihoods->clear();
    }

    for (BasicBlock* const succ : block->Succs(m_comp))
    {
        FlowEdge* const edge       = m_comp->fgGetPredForBlock(succ, block);
        weight_t        likelihood = edge->getLikelihood();
        if (likelihoods != nullptr)
        {
            likelihoods->push_back(likelihood);
        }
        sum += likelihood;
    }

    return sum;
}

//------------------------------------------------------------------------
// RepairLikelihoods: find nodes with inconsistent or missing likelihoods
//   and update them with heuristics.
//
// Notes:
//   Existing likelihoods are retained, if consistent.
//
void ProfileSynthesis::RepairLikelihoods()
{
    JITDUMP("Repairing inconsistent or missing edge likelihoods\n");

    for (BasicBlock* const block : m_comp->Blocks())
    {
        switch (block->bbJumpKind)
        {
            case BBJ_THROW:
            case BBJ_RETURN:
            case BBJ_EHFINALLYRET:
            case BBJ_EHFAULTRET:
                // No successor cases
                // Nothing to do.
                break;

            case BBJ_NONE:
            case BBJ_CALLFINALLY:
                // Single successor next cases.
                // Just assign 1.0
                AssignLikelihoodNext(block);
                break;

            case BBJ_ALWAYS:
            case BBJ_LEAVE:
            case BBJ_EHCATCHRET:
            case BBJ_EHFILTERRET:
                // Single successor jump cases
                // Just assign 1.0
                AssignLikelihoodJump(block);
                break;

            case BBJ_COND:
            case BBJ_SWITCH:
            {
                // Repair if either likelihoods are inconsistent or block weight is zero.
                //
                weight_t const sum        = SumOutgoingLikelihoods(block);
                bool const     consistent = Compiler::fgProfileWeightsEqual(sum, 1.0, epsilon);
                bool const     zero       = Compiler::fgProfileWeightsEqual(block->bbWeight, 0.0, epsilon);

                if (consistent && !zero)
                {
                    // Leave as is.
                    break;
                }

                JITDUMP("Repairing likelihoods in " FMT_BB, block->bbNum);
                if (!consistent)
                {
                    JITDUMP("; existing likelihood sum: " FMT_WT, sum);
                }
                if (zero)
                {
                    JITDUMP("; zero weight block");
                }
                JITDUMP("\n");

                if (block->bbJumpKind == BBJ_COND)
                {
                    AssignLikelihoodCond(block);
                }
                else
                {
                    AssignLikelihoodSwitch(block);
                }

                break;
            }

            default:
                unreached();
        }
    }
}

//------------------------------------------------------------------------
// BlendLikelihoods: if a node has existing likelihoods that differ from
//   the heuristic likelihoods, blend the heuristic prediction with the
//   current prediction
//
// Notes:
//
//   Blend is weighted 95% existing / 5% heuristics
//
//   If a node's existing likelihoods don't sum to 1.0:
//     * if sum is zero, just run heuristics
//     * if sum is nonzero, scale that up and then blend
//
void ProfileSynthesis::BlendLikelihoods()
{
    JITDUMP("Blending existing likelihoods with heuristics\n");
    WeightVector likelihoods(m_comp->getAllocator(CMK_Pgo));

    for (BasicBlock* const block : m_comp->Blocks())
    {
        weight_t sum = SumOutgoingLikelihoods(block, &likelihoods);

        switch (block->bbJumpKind)
        {
            case BBJ_THROW:
            case BBJ_RETURN:
            case BBJ_EHFINALLYRET:
            case BBJ_EHFAULTRET:
                // No successor cases
                // Nothing to do.
                break;

            case BBJ_NONE:
            case BBJ_CALLFINALLY:
                // Single successor next cases.
                // Just assign 1.0
                AssignLikelihoodNext(block);
                break;

            case BBJ_ALWAYS:
            case BBJ_LEAVE:
            case BBJ_EHCATCHRET:
            case BBJ_EHFILTERRET:
                // Single successor jump cases
                // Just assign 1.0
                AssignLikelihoodJump(block);
                break;

            case BBJ_COND:
            case BBJ_SWITCH:
            {
                // Capture the existing weights and assign new likelihoods based on synthesis.
                //
                weight_t const sum        = SumOutgoingLikelihoods(block, &likelihoods);
                bool const     unlikely   = Compiler::fgProfileWeightsEqual(sum, 0.0, epsilon);
                bool const     consistent = Compiler::fgProfileWeightsEqual(sum, 1.0, epsilon);
                bool const     zero       = Compiler::fgProfileWeightsEqual(block->bbWeight, 0.0, epsilon);

                if (block->bbJumpKind == BBJ_COND)
                {
                    AssignLikelihoodCond(block);
                }
                else
                {
                    AssignLikelihoodSwitch(block);
                }

                if (unlikely || zero)
                {
                    // Existing likelihood was zero, or profile weight was zero. Just use synthesis likelihoods.
                    //
                    JITDUMP("%s in " FMT_BB " was zero, using synthesized likelihoods\n",
                            unlikely ? "Existing likelihood" : "Block weight", block->bbNum);
                    break;
                }

                WeightVector::iterator iter;

                if (!Compiler::fgProfileWeightsEqual(sum, 1.0, epsilon))
                {
                    // Existing likelihood was too low or too high. Scale.
                    //
                    weight_t scale = 1.0 / sum;
                    JITDUMP("Scaling old likelihoods in " FMT_BB " by " FMT_WT "\n", block->bbNum, scale);
                    for (iter = likelihoods.begin(); iter != likelihoods.end(); iter++)
                    {
                        *iter *= scale;
                    }
                }

                // Blend
                //
                JITDUMP("Blending likelihoods in " FMT_BB " with blend factor " FMT_WT " \n", block->bbNum,
                        blendFactor);
                iter = likelihoods.begin();
                for (BasicBlock* const succ : block->Succs(m_comp))
                {
                    FlowEdge* const edge          = m_comp->fgGetPredForBlock(succ, block);
                    weight_t        newLikelihood = edge->getLikelihood();
                    weight_t        oldLikelihood = *iter;

                    edge->setLikelihood((blendFactor * oldLikelihood) + ((1.0 - blendFactor) * newLikelihood));
                    JITDUMP(FMT_BB " -> " FMT_BB " was " FMT_WT " now " FMT_WT "\n", block->bbNum, succ->bbNum,
                            oldLikelihood, edge->getLikelihood());

                    iter++;
                }
                break;
            }

            default:
                unreached();
        }
    }
}

//------------------------------------------------------------------------
// ClearLikelihoods: unset likelihoods on all edges
//
void ProfileSynthesis::ClearLikelihoods()
{
    for (BasicBlock* const block : m_comp->Blocks())
    {
        for (BasicBlock* const succ : block->Succs(m_comp))
        {
            FlowEdge* const edge = m_comp->fgGetPredForBlock(succ, block);
            edge->clearLikelihood();
        }
    }
}

//------------------------------------------------------------------------
// ReverseLikelihoods: for all blocks, reverse likelihoods on all edges
//   from the block
//
void ProfileSynthesis::ReverseLikelihoods()
{
#ifdef DEBUG
    JITDUMP("Reversing likelihoods\n");
    WeightVector likelihoods(m_comp->getAllocator(CMK_Pgo));
    for (BasicBlock* const block : m_comp->Blocks())
    {
        for (BasicBlock* const succ : block->Succs(m_comp))
        {
            weight_t sum = SumOutgoingLikelihoods(block, &likelihoods);

            if (likelihoods.size() < 2)
            {
                continue;
            }

            for (size_t i = 0; i < likelihoods.size() / 2; i++)
            {
                size_t   j     = likelihoods.size() - i - 1;
                weight_t t     = likelihoods[i];
                likelihoods[i] = likelihoods[j];
                likelihoods[j] = t;
            }
        }
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// RandomizeLikelihoods: for all blocks, randomize likelihoods on all edges
//   from the block
//
// Notes:
//   total outgoing likelihood for each block remains at 1.0
//
void ProfileSynthesis::RandomizeLikelihoods()
{
#ifdef DEBUG
    // todo: external seed
    JITDUMP("Randomizing likelihoods\n");
    WeightVector likelihoods(m_comp->getAllocator(CMK_Pgo));
    CLRRandom    random;

    random.Init(m_comp->info.compMethodHash());

    for (BasicBlock* const block : m_comp->Blocks())
    {
        unsigned const N = block->NumSucc(m_comp);

        if (N < 2)
        {
            continue;
        }

        likelihoods.clear();
        likelihoods.reserve(N);

        weight_t sum = 0;
        unsigned i   = 0;

        // Consider: something other than uniform distribution.
        // As is, this will rarely set likelihoods to zero.
        //
        for (i = 0; i < N; i++)
        {
            likelihoods[i] = (weight_t)random.NextDouble();
            sum += likelihoods[i];
        }

        i = 0;
        for (BasicBlock* const succ : block->Succs(m_comp))
        {
            FlowEdge* const edge = m_comp->fgGetPredForBlock(succ, block);
            edge->setLikelihood(likelihoods[i++] / sum);
        }
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// fgBuildReversePostorder: compute depth first spanning tree and pre
//   and post numbers for the blocks
//
void ProfileSynthesis::BuildReversePostorder()
{
    m_comp->EnsureBasicBlockEpoch();
    m_comp->fgComputeEnterBlocksSet();
    m_comp->fgDfsReversePostorder();

    // Build map from bbNum to Block*.
    //
    m_bbNumToBlockMap = new (m_comp, CMK_Pgo) BasicBlock*[m_comp->fgBBNumMax + 1]{};
    for (BasicBlock* const block : m_comp->Blocks())
    {
        m_bbNumToBlockMap[block->bbNum] = block;
    }

#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("\nAfter doing a post order traversal of the BB graph, this is the ordering:\n");
        for (unsigned i = 1; i <= m_comp->fgBBNumMax; ++i)
        {
            BasicBlock* const block = m_comp->fgBBReversePostorder[i];
            printf("%02u -> " FMT_BB "[%u, %u]\n", i, block->bbNum, block->bbPreorderNum, block->bbPostorderNum);
        }
        printf("\n");
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// FindLoops: locate and classify loops
//
void ProfileSynthesis::FindLoops()
{
    CompAllocator allocator = m_comp->getAllocator(CMK_Pgo);
    m_loops                 = new (allocator) LoopVector(allocator);

    // Identify loops
    //
    for (unsigned i = 1; i <= m_comp->fgBBNumMax; i++)
    {
        BasicBlock* const block = m_comp->fgBBReversePostorder[i];

        // If a block is a DFS ancestor of one if its predecessors then the block is a loop header.
        //
        SimpleLoop* loop = nullptr;

        for (FlowEdge* predEdge : block->PredEdges())
        {
            if (IsDfsAncestor(block, predEdge->getSourceBlock()))
            {
                if (loop == nullptr)
                {
                    loop = new (allocator) SimpleLoop(block, allocator);
                    JITDUMP("\n");
                }

                JITDUMP(FMT_BB " -> " FMT_BB " is a backedge\n", predEdge->getSourceBlock()->bbNum, block->bbNum);
                loop->m_backEdges.push_back(predEdge);
            }
        }

        if (loop == nullptr)
        {
            continue;
        }

        JITDUMP(FMT_BB " is head of a DFS loop with %d back edges\n", block->bbNum, loop->m_backEdges.size());

        // Now walk back in flow along the back edges from block to determine if
        // this is a natural loop and to find all the blocks in the loop.
        //
        loop->m_blocks = BlockSetOps::MakeEmpty(m_comp);
        BlockSetOps::AddElemD(m_comp, loop->m_blocks, block->bbNum);

        // todo: hoist this out and just do a reset here
        jitstd::list<BasicBlock*> worklist(allocator);

        // Seed the worklist
        //
        for (FlowEdge* backEdge : loop->m_backEdges)
        {
            BasicBlock* const backEdgeSource = backEdge->getSourceBlock();

            if (BlockSetOps::IsMember(m_comp, loop->m_blocks, backEdgeSource->bbNum))
            {
                continue;
            }

            worklist.push_back(backEdgeSource);
        }

        bool isNaturalLoop = true;

        // Work back through flow to loop head or to another pred
        // that is clearly outside the loop.
        //
        // TODO: verify that we can indeed get back to the loop head
        // and not get stopped somewhere (eg loop through EH).
        //
        while (!worklist.empty() & isNaturalLoop)
        {
            BasicBlock* const loopBlock = worklist.back();
            worklist.pop_back();
            BlockSetOps::AddElemD(m_comp, loop->m_blocks, loopBlock->bbNum);

            for (FlowEdge* const predEdge : loopBlock->PredEdges())
            {
                BasicBlock* const predBlock = predEdge->getSourceBlock();

                // `block` cannot dominate `predBlock` unless it is a DFS ancestor.
                //
                if (!IsDfsAncestor(block, predBlock))
                {
                    // Does this represent flow out of some handler?
                    // If so we will ignore it.
                    //
                    // Might want to vet that handler's try region entry
                    // is a dfs ancestor...?
                    //
                    if (!BasicBlock::sameHndRegion(block, predBlock))
                    {
                        continue;
                    }

                    JITDUMP("Loop is not natural; witness " FMT_BB " -> " FMT_BB "\n", predBlock->bbNum,
                            loopBlock->bbNum);

                    isNaturalLoop = false;
                    m_improperLoopHeaders++;
                    break;
                }

                if (BlockSetOps::IsMember(m_comp, loop->m_blocks, predBlock->bbNum))
                {
                    continue;
                }

                worklist.push_back(predBlock);
            }
        }

        if (!isNaturalLoop)
        {
            continue;
        }

        JITDUMP("Loop has %d blocks\n", BlockSetOps::Count(m_comp, loop->m_blocks));

        // Find the exit edges
        //
        BlockSetOps::Iter iter(m_comp, loop->m_blocks);
        unsigned          bbNum = 0;
        while (iter.NextElem(&bbNum))
        {
            BasicBlock* const loopBlock = m_bbNumToBlockMap[bbNum];

            for (BasicBlock* const succBlock : loopBlock->Succs(m_comp))
            {
                if (!BlockSetOps::IsMember(m_comp, loop->m_blocks, succBlock->bbNum))
                {
                    FlowEdge* const exitEdge = m_comp->fgGetPredForBlock(succBlock, loopBlock);
                    JITDUMP(FMT_BB " -> " FMT_BB " is an exit edge\n", loopBlock->bbNum, succBlock->bbNum);
                    loop->m_exitEdges.push_back(exitEdge);
                }
            }
        }

        // Find the entry edges
        //
        // Note if fgEntryBB is a loop head we won't have an entry edge.
        // So it needs to be special cased later on when processing
        // entry edges.
        //
        for (FlowEdge* const predEdge : loop->m_head->PredEdges())
        {
            if (!IsDfsAncestor(block, predEdge->getSourceBlock()))
            {
                JITDUMP(FMT_BB " -> " FMT_BB " is an entry edge\n", predEdge->getSourceBlock()->bbNum,
                        loop->m_head->bbNum);
                loop->m_entryEdges.push_back(predEdge);
            }
        }

        // Search for parent loop, validate proper nesting.
        //
        // Since loops record in outer->inner order the parent will be the
        // most recently recorded loop that contains this loop's header.
        //
        for (auto it = m_loops->rbegin(), itEnd = m_loops->rend(); it != itEnd; ++it)
        {
            SimpleLoop* const otherLoop = *it;

            if (BlockSetOps::IsMember(m_comp, otherLoop->m_blocks, block->bbNum))
            {
                // Ancestor loop; should contain all blocks of this loop
                //
                assert(BlockSetOps::IsSubset(m_comp, loop->m_blocks, otherLoop->m_blocks));

                if (loop->m_parent == nullptr)
                {
                    loop->m_parent = otherLoop;
                    loop->m_depth  = otherLoop->m_depth + 1;
                    JITDUMP("at depth %u, nested within loop starting at " FMT_BB "\n", loop->m_depth,
                            otherLoop->m_head->bbNum);

                    // Note we could break here but that would bypass the non-overlap check
                    // just below, so for now we check against all known loops.
                }
            }
            else
            {
                // Non-ancestor loop; should have no blocks in common with current loop
                //
                assert(BlockSetOps::IsEmptyIntersection(m_comp, loop->m_blocks, otherLoop->m_blocks));
            }
        }

        if (loop->m_parent == nullptr)
        {
            JITDUMP("top-level loop\n");
            loop->m_depth = 1;
        }

        // Record this loop
        //
        m_loops->push_back(loop);
    }

    if (m_loops->size() > 0)
    {
        JITDUMP("\nFound %d loops\n", m_loops->size());
    }

    if (m_improperLoopHeaders > 0)
    {
        JITDUMP("Rejected %d loop headers\n", m_improperLoopHeaders);
    }
}

//------------------------------------------------------------------------
// FindCyclicProbabilities: for each loop, compute how much flow returns
//   to the loop head given one external count.
//
void ProfileSynthesis::ComputeCyclicProbabilities()
{
    // We found loop walking in reverse postorder, so the loop vector
    // is naturally organized with outer loops before inner.
    //
    // Walk it backwards here so we compute inner loop cyclic probabilities
    // first. We rely on that when processing outer loops.
    //
    for (auto it = m_loops->rbegin(), itEnd = m_loops->rend(); it != itEnd; ++it)
    {
        SimpleLoop* const loop = *it;
        ComputeCyclicProbabilities(loop);
    }
}

//------------------------------------------------------------------------
// FindCyclicProbabilities: for a given loop, compute how much flow returns
//   to the loop head given one external count.
//
void ProfileSynthesis::ComputeCyclicProbabilities(SimpleLoop* loop)
{
    // Initialize
    //
    BlockSetOps::Iter iter(m_comp, loop->m_blocks);
    unsigned          bbNum = 0;
    while (iter.NextElem(&bbNum))
    {
        BasicBlock* const loopBlock = m_bbNumToBlockMap[bbNum];
        loopBlock->bbWeight         = 0.0;
    }

    // Process loop blocks in RPO. Just takes one pass through the loop blocks
    // as any cyclic contributions are handled by cyclic probabilities.
    //
    for (unsigned int i = 1; i <= m_comp->fgBBNumMax; i++)
    {
        BasicBlock* const block = m_comp->fgBBReversePostorder[i];

        if (!BlockSetOps::IsMember(m_comp, loop->m_blocks, block->bbNum))
        {
            continue;
        }

        // Loop head gets external count of 1
        //
        if (block == loop->m_head)
        {
            JITDUMP("ccp: " FMT_BB " :: 1.0\n", block->bbNum);
            block->bbWeight = 1.0;
        }
        else
        {
            SimpleLoop* const nestedLoop = GetLoopFromHeader(block);

            if (nestedLoop != nullptr)
            {
                // We should have figured this out already.
                //
                assert(nestedLoop->m_cyclicProbability != 0);

                // Sum entry edges, multply by Cp
                //
                weight_t newWeight = 0.0;

                for (FlowEdge* const edge : nestedLoop->m_entryEdges)
                {
                    if (BasicBlock::sameHndRegion(block, edge->getSourceBlock()))
                    {
                        newWeight += edge->getLikelyWeight();
                    }
                }

                newWeight *= nestedLoop->m_cyclicProbability;
                block->bbWeight = newWeight;

                JITDUMP("ccp (nested header): " FMT_BB " :: " FMT_WT "\n", block->bbNum, newWeight);
            }
            else
            {
                weight_t newWeight = 0.0;

                for (FlowEdge* const edge : block->PredEdges())
                {
                    if (BasicBlock::sameHndRegion(block, edge->getSourceBlock()))
                    {
                        newWeight += edge->getLikelyWeight();
                    }
                }

                block->bbWeight = newWeight;

                JITDUMP("ccp: " FMT_BB " :: " FMT_WT "\n", block->bbNum, newWeight);
            }
        }
    }

    // Now look at cyclic flow back to the head block.
    //
    weight_t cyclicWeight = 0;
    bool     capped       = false;

    for (FlowEdge* const edge : loop->m_backEdges)
    {
        JITDUMP("ccp backedge " FMT_BB " (" FMT_WT ") -> " FMT_BB " likelihood " FMT_WT "\n",
                edge->getSourceBlock()->bbNum, edge->getSourceBlock()->bbWeight, loop->m_head->bbNum,
                edge->getLikelihood());

        cyclicWeight += edge->getLikelyWeight();
    }

    // Allow for a bit of rounding error, but not too much.
    // (todo: decrease loop gain if we are in a deep nest?)
    // assert(cyclicWeight <= 1.01);
    //
    if (cyclicWeight > cappedLikelihood)
    {
        JITDUMP("Cyclic weight " FMT_WT " > " FMT_WT "(cap) -- will reduce to cap\n", cyclicWeight, cappedLikelihood);
        capped       = true;
        cyclicWeight = cappedLikelihood;
        m_cappedCyclicProbabilities++;
    }

    // Note this value is not actually a probability; it is the expected
    // iteration count of the loop.
    //
    weight_t const cyclicProbability = 1.0 / (1.0 - cyclicWeight);

    JITDUMP("For loop at " FMT_BB " cyclic weight is " FMT_WT " cyclic probability is " FMT_WT "%s\n",
            loop->m_head->bbNum, cyclicWeight, cyclicProbability, capped ? " [capped]" : "");

    loop->m_cyclicProbability = cyclicProbability;

    // Try and adjust loop exit likelihood to reflect capping.
    // If there are multiple exits we just adjust the first one we can. This is somewhat arbitrary.
    // If there are no exits, there's nothing we can do.
    //
    if (capped && (loop->m_exitEdges.size() > 0))
    {
        // Figure out how much flow exits the loop with the capped probablility
        // and current block frequencies and exit likelihoods.
        //
        weight_t cappedExitWeight = 0.0;

        for (FlowEdge* const exitEdge : loop->m_exitEdges)
        {
            BasicBlock* const exitBlock          = exitEdge->getSourceBlock();
            weight_t const    exitBlockFrequency = exitBlock->bbWeight;
            weight_t const    exitBlockWeight    = exitBlockFrequency * cyclicProbability;
            weight_t const    exitWeight         = exitEdge->getLikelihood() * exitBlockWeight;
            cappedExitWeight += exitWeight;
            JITDUMP("Exit from " FMT_BB " has weight " FMT_WT "\n", exitBlock->bbNum, exitWeight);
        }

        JITDUMP("Total exit weight " FMT_WT "\n", cappedExitWeight);

        // We should end up with a value less than one since we input one unit of flow into the
        // loop and are artificially capping the iteration count of the loop, so less weight is
        // now flowing out than in. However because of rounding we might end up near or a bit over 1.0.
        //
        if ((cappedExitWeight + epsilon) < 1.0)
        {
            // We want to increase the exit likelihood of one exit block to create
            // additional flow out of the loop. Figure out how much we need.
            //
            weight_t const missingExitWeight = 1.0 - cappedExitWeight;
            JITDUMP("Loop exit flow deficit from capping is " FMT_WT "\n", missingExitWeight);

            bool adjustedExit = false;

            for (FlowEdge* const exitEdge : loop->m_exitEdges)
            {
                // Does this block have enough weight that it can supply all the missing weight?
                //
                BasicBlock* const exitBlock          = exitEdge->getSourceBlock();
                weight_t const    exitBlockFrequency = exitBlock->bbWeight;
                weight_t const    exitBlockWeight    = exitBlockFrequency * cyclicProbability;
                weight_t const    currentExitWeight  = exitEdge->getLikelihood() * exitBlockWeight;

                // TODO: we might also want to exclude edges that are exiting from child loops here,
                // or think harder about what might be appropriate in those cases. Seems like we ought
                // to adjust an edge's likelihoods at most once.
                //
                // Currently we don't know which edges do this.
                //
                if ((exitBlock->bbJumpKind == BBJ_COND) && (exitBlockWeight > (missingExitWeight + currentExitWeight)))
                {
                    JITDUMP("Will adjust likelihood of the exit edge from loop exit block " FMT_BB
                            " to reflect capping; current likelihood is " FMT_WT "\n",
                            exitBlock->bbNum, exitEdge->getLikelihood());

                    BasicBlock* const jump               = exitBlock->bbJumpDest;
                    BasicBlock* const next               = exitBlock->bbNext;
                    FlowEdge* const   jumpEdge           = m_comp->fgGetPredForBlock(jump, exitBlock);
                    FlowEdge* const   nextEdge           = m_comp->fgGetPredForBlock(next, exitBlock);
                    weight_t const    exitLikelihood     = (missingExitWeight + currentExitWeight) / exitBlockWeight;
                    weight_t const    continueLikelihood = 1.0 - exitLikelihood;

                    // We are making it more likely that the loop exits, so the new exit likelihood
                    // should be greater than the old.
                    //
                    assert(exitLikelihood > exitEdge->getLikelihood());

                    if (jumpEdge == exitEdge)
                    {
                        jumpEdge->setLikelihood(exitLikelihood);
                        nextEdge->setLikelihood(continueLikelihood);
                    }
                    else
                    {
                        assert(nextEdge == exitEdge);
                        jumpEdge->setLikelihood(continueLikelihood);
                        nextEdge->setLikelihood(exitLikelihood);
                    }
                    adjustedExit = true;

                    JITDUMP("New likelihood is  " FMT_WT "\n", exitEdge->getLikelihood());
                    break;
                }
            }

            if (!adjustedExit)
            {
                // Possibly we could have fixed things up by adjusting more than one exit?
                //
                JITDUMP("Unable to find suitable exit to carry off capped flow\n");
            }
        }
        else
        {
            JITDUMP("Exit weight comparable or above 1.0, leaving as is\n");
        }
    }
}

//------------------------------------------------------------------------
// fgAssignInputWeights: provide initial profile weights for all blocks
//
// Arguments:
//   option - profile synthesis option
//
// Notes:
//   For finallys we will pick up new entry weights when we process
//   the subtree that can invoke them normally.
//
//   Option is used to determine the entry weight, so that the
//   absolute values of weights does not change dramatically.
//
//   Some parts of the jit are sensitive to the absolute weights.
//
void ProfileSynthesis::AssignInputWeights(ProfileSynthesisOption option)
{
    // Determine input weight for entire method.
    //
    BasicBlock* const entryBlock  = m_comp->fgFirstBB;
    weight_t          entryWeight = BB_UNITY_WEIGHT;

    switch (option)
    {
        case ProfileSynthesisOption::BlendLikelihoods:
        case ProfileSynthesisOption::RepairLikelihoods:
        {
            // Try and retain fgEntryBB's weight.
            // Easiest to do when the block has no preds.
            //
            if (entryBlock->hasProfileWeight())
            {
                weight_t currentEntryWeight = entryBlock->bbWeight;

                if (!Compiler::fgProfileWeightsEqual(currentEntryWeight, 0.0, epsilon))
                {
                    if (entryBlock->bbPreds == nullptr)
                    {
                        entryWeight = currentEntryWeight;
                    }
                    else
                    {
                        // TODO: something similar to how we compute fgCalledCount;
                        // try and sum return weights?
                    }
                }
                else
                {
                    // Entry weight was zero or nearly zero, just use default
                }
            }
            else
            {
                // Entry was unprofiled, just use default
            }
            break;
        }

        default:
            break;
    }

    // Determine input weight for EH regions.
    //
    const weight_t ehWeight = entryWeight * exceptionScale;

    for (BasicBlock* block : m_comp->Blocks())
    {
        block->setBBProfileWeight(0.0);
    }

    entryBlock->setBBProfileWeight(entryWeight);

    if (!m_comp->compIsForInlining())
    {
        for (EHblkDsc* const HBtab : EHClauses(m_comp))
        {
            if (HBtab->HasFilter())
            {
                HBtab->ebdFilter->setBBProfileWeight(ehWeight);
            }

            HBtab->ebdHndBeg->setBBProfileWeight(ehWeight);
        }
    }
}

//------------------------------------------------------------------------
// ComputeBlockWeights: compute weights for all blocks
//   based on input weights, edge likelihoods, and cyclic probabilities
//
// Notes:
//   We want to first walk the main method body, then any finally
//   handers from outermost to innermost.
//
//   The depth first walk we did to kick off synthesis has split the
//   graph into a forest of depth first spanning trees. We leverage
//   this and the EH table structure to accomplish the visiting order above.
//
//   We might be able to avoid all this if during the DFS walk we
//   walked from try entries to filter or handlers, so that a
//   single DFST encompassed all the reachable blocks in the right order.
//
void ProfileSynthesis::ComputeBlockWeights()
{
    JITDUMP("Computing block weights\n");

    // Main method body
    //
    ComputeBlockWeightsSubgraph(m_comp->fgFirstBB);

    // All finally and fault handlers from outer->inner
    // (walk EH table backwards)
    //
    if (!m_comp->compIsForInlining())
    {
        for (unsigned i = 0; i < m_comp->compHndBBtabCount; i++)
        {
            unsigned const  XTnum = m_comp->compHndBBtabCount - i - 1;
            EHblkDsc* const HBtab = &m_comp->compHndBBtab[XTnum];
            if (HBtab->HasFilter())
            {
                // Filter subtree includes handler
                //
                ComputeBlockWeightsSubgraph(HBtab->ebdFilter);
            }
            else
            {
                ComputeBlockWeightsSubgraph(HBtab->ebdHndBeg);
            }
        }
    }

    // Anything else is unreachable and will have zero count
}

//------------------------------------------------------------------------
// ComputeBlockWeights: compute weights for all blocks in a particular DFST
//
// Arguments:
//   entry - root node of a DFST
//
void ProfileSynthesis::ComputeBlockWeightsSubgraph(BasicBlock* entry)
{
    // Determine the range of indices for this DFST in the overall RPO.
    //
    const unsigned firstIndex = m_comp->fgBBNumMax - entry->bbPostorderNum + 1;
    assert(m_comp->fgBBReversePostorder[firstIndex] == entry);

    assert(entry->bbPostorderNum >= entry->bbPreorderNum);
    const unsigned lastIndex = firstIndex + entry->bbPostorderNum - entry->bbPreorderNum;

    for (unsigned int i = firstIndex; i <= lastIndex; i++)
    {
        BasicBlock* const block = m_comp->fgBBReversePostorder[i];
        ComputeBlockWeight(block);
    }
}

//------------------------------------------------------------------------
// ComputeBlockWeight: compute weight for a given block
//
// Arguments:
//    block: block in question
//
void ProfileSynthesis::ComputeBlockWeight(BasicBlock* block)
{
    SimpleLoop* const loop      = GetLoopFromHeader(block);
    weight_t          newWeight = block->bbWeight;
    const char*       kind      = "";

    if (loop != nullptr)
    {
        // Sum all entry edges that aren't EH flow
        //
        for (FlowEdge* const edge : loop->m_entryEdges)
        {
            if (BasicBlock::sameHndRegion(block, edge->getSourceBlock()))
            {
                newWeight += edge->getLikelyWeight();
            }
        }

        // Scale by cyclic probability
        //
        newWeight *= loop->m_cyclicProbability;
        kind = " (loop head)";
    }
    else
    {
        // Sum all incoming edges that aren't EH flow
        //
        for (FlowEdge* const edge : block->PredEdges())
        {
            if (BasicBlock::sameHndRegion(block, edge->getSourceBlock()))
            {
                newWeight += edge->getLikelyWeight();
            }
        }
    }

    block->setBBProfileWeight(newWeight);
    JITDUMP("cbw%s: " FMT_BB " :: " FMT_WT "\n", kind, block->bbNum, block->bbWeight);

    // If we're at the start of try in a try/finally, update the finally
    // entry to reflect the proper weight.
    //
    if (m_comp->bbIsTryBeg(block))
    {
        EHblkDsc* const HBtab = m_comp->ehGetBlockTryDsc(block);

        if (HBtab->HasFinallyHandler())
        {
            BasicBlock* const finallyEntry = HBtab->ebdHndBeg;
            finallyEntry->setBBProfileWeight(newWeight);
            kind = " (finally)";
            JITDUMP("cbw%s: " FMT_BB " :: " FMT_WT "\n", kind, finallyEntry->bbNum, finallyEntry->bbWeight);
        }
    }
}
