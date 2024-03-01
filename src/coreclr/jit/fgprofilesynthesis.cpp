// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "fgprofilesynthesis.h"

// TODO
//
// * vet against some real data
// * IR based heuristics (perhaps)
// * During Cp, avoid repeatedly propagating through nested loops
// * Fake BB0 or always force scratch BB
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
    m_dfsTree = m_comp->fgComputeDfs();
    m_loops   = FlowGraphNaturalLoops::Find(m_dfsTree);

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
        switch (block->GetKind())
        {
            case BBJ_THROW:
            case BBJ_RETURN:
            case BBJ_EHFINALLYRET:
            case BBJ_EHFAULTRET:
                // No successor cases
                // (todo: finally ret may have succs)
                break;

            case BBJ_CALLFINALLY:
            case BBJ_ALWAYS:
            case BBJ_CALLFINALLYRET:
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
// AssignLikelihoodJump: update edge likelihood for a block that always
//   transfers control to its target block
//
// Arguments;
//   block -- block in question
//
void ProfileSynthesis::AssignLikelihoodJump(BasicBlock* block)
{
    FlowEdge* const edge = block->GetTargetEdge();
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
    FlowEdge* const trueEdge  = block->GetTrueEdge();
    FlowEdge* const falseEdge = block->GetFalseEdge();

    // Watch for degenerate case
    //
    if (trueEdge == falseEdge)
    {
        assert(trueEdge->getDupCount() == 2);
        trueEdge->setLikelihood(1.0);
        return;
    }

    BasicBlock* trueTarget  = trueEdge->getDestinationBlock();
    BasicBlock* falseTarget = falseEdge->getDestinationBlock();

    // THROW heuristic
    //
    bool const isTrueThrow  = trueTarget->KindIs(BBJ_THROW);
    bool const isFalseThrow = falseTarget->KindIs(BBJ_THROW);

    if (isTrueThrow != isFalseThrow)
    {
        if (isTrueThrow)
        {
            trueEdge->setLikelihood(0.0);
            falseEdge->setLikelihood(1.0);
        }
        else
        {
            trueEdge->setLikelihood(1.0);
            falseEdge->setLikelihood(0.0);
        }

        return;
    }

    // LOOP BACK EDGE heuristic
    //
    bool const isTrueEdgeBackEdge  = m_loops->IsLoopBackEdge(trueEdge);
    bool const isFalseEdgeBackEdge = m_loops->IsLoopBackEdge(falseEdge);

    if (isTrueEdgeBackEdge != isFalseEdgeBackEdge)
    {
        if (isTrueEdgeBackEdge)
        {
            JITDUMP(FMT_BB "->" FMT_BB " is loop back edge\n", block->bbNum, trueTarget->bbNum);
            trueEdge->setLikelihood(loopBackLikelihood);
            falseEdge->setLikelihood(1.0 - loopBackLikelihood);
        }
        else
        {
            JITDUMP(FMT_BB "->" FMT_BB " is loop back edge\n", block->bbNum, falseTarget->bbNum);
            trueEdge->setLikelihood(1.0 - loopBackLikelihood);
            falseEdge->setLikelihood(loopBackLikelihood);
        }

        return;
    }

    // LOOP EXIT EDGE heuristic
    //
    // Consider: adjust probability if loop has multiple exit edges, so that
    // overall exit probability is around 0.1.
    //
    bool const isTrueEdgeExitEdge  = m_loops->IsLoopExitEdge(trueEdge);
    bool const isFalseEdgeExitEdge = m_loops->IsLoopExitEdge(falseEdge);

    if (isTrueEdgeExitEdge != isFalseEdgeExitEdge)
    {
        if (isTrueEdgeExitEdge)
        {
            JITDUMP(FMT_BB "->" FMT_BB " is loop exit edge\n", block->bbNum, trueTarget->bbNum);
            trueEdge->setLikelihood(1.0 - loopExitLikelihood);
            falseEdge->setLikelihood(loopExitLikelihood);
        }
        else
        {
            JITDUMP(FMT_BB "->" FMT_BB " is loop exit edge\n", block->bbNum, falseTarget->bbNum);
            trueEdge->setLikelihood(loopExitLikelihood);
            falseEdge->setLikelihood(1.0 - loopExitLikelihood);
        }

        return;
    }

    // RETURN heuristic
    //
    bool const isJumpReturn = trueTarget->KindIs(BBJ_RETURN);
    bool const isNextReturn = falseTarget->KindIs(BBJ_RETURN);

    if (isJumpReturn != isNextReturn)
    {
        if (isJumpReturn)
        {
            trueEdge->setLikelihood(returnLikelihood);
            falseEdge->setLikelihood(1.0 - returnLikelihood);
        }
        else
        {
            trueEdge->setLikelihood(1.0 - returnLikelihood);
            falseEdge->setLikelihood(returnLikelihood);
        }

        return;
    }

    // IL OFFSET heuristic
    //
    // Give slight preference to bbNext
    //
    trueEdge->setLikelihood(1.0 - ilNextLikelihood);
    falseEdge->setLikelihood(ilNextLikelihood);
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

    // Check for divide by zero to avoid compiler warnings for Release builds (above assert is removed)
    const weight_t p = (n != 0) ? (1 / (weight_t)n) : 0;

    // Each unique edge gets some multiple of that basic probability
    //
    for (FlowEdge* const succEdge : block->SuccEdges(m_comp))
    {
        succEdge->setLikelihood(p * succEdge->getDupCount());
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

    for (FlowEdge* const succEdge : block->SuccEdges(m_comp))
    {
        weight_t likelihood = succEdge->getLikelihood();
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
        switch (block->GetKind())
        {
            case BBJ_THROW:
            case BBJ_RETURN:
            case BBJ_EHFINALLYRET:
            case BBJ_EHFAULTRET:
                // No successor cases
                // Nothing to do.
                break;

            case BBJ_CALLFINALLY:
            case BBJ_ALWAYS:
            case BBJ_CALLFINALLYRET:
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

                if (block->KindIs(BBJ_COND))
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

        switch (block->GetKind())
        {
            case BBJ_THROW:
            case BBJ_RETURN:
            case BBJ_EHFINALLYRET:
            case BBJ_EHFAULTRET:
                // No successor cases
                // Nothing to do.
                break;

            case BBJ_CALLFINALLY:
            case BBJ_ALWAYS:
            case BBJ_CALLFINALLYRET:
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

                if (block->KindIs(BBJ_COND))
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
                for (FlowEdge* const succEdge : block->SuccEdges(m_comp))
                {
                    weight_t newLikelihood = succEdge->getLikelihood();
                    weight_t oldLikelihood = *iter;

                    succEdge->setLikelihood((blendFactor * oldLikelihood) + ((1.0 - blendFactor) * newLikelihood));
                    BasicBlock* const succBlock = succEdge->getDestinationBlock();
                    JITDUMP(FMT_BB " -> " FMT_BB " was " FMT_WT " now " FMT_WT "\n", block->bbNum, succBlock->bbNum,
                            oldLikelihood, succEdge->getLikelihood());

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
        for (FlowEdge* const succEdge : block->SuccEdges(m_comp))
        {
            succEdge->clearLikelihood();
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
        for (FlowEdge* const succEdge : block->SuccEdges(m_comp))
        {
            succEdge->setLikelihood(likelihoods[i++] / sum);
        }
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// FindCyclicProbabilities: for each loop, compute how much flow returns
//   to the loop head given one external count.
//
void ProfileSynthesis::ComputeCyclicProbabilities()
{
    m_cyclicProbabilities = nullptr;
    if (m_loops->NumLoops() == 0)
    {
        return;
    }

    m_cyclicProbabilities = new (m_comp, CMK_Pgo) weight_t[m_loops->NumLoops()];
    // Walk loops in post order to visit inner loops before outer loops.
    for (FlowGraphNaturalLoop* loop : m_loops->InPostOrder())
    {
        ComputeCyclicProbabilities(loop);
    }
}

//------------------------------------------------------------------------
// FindCyclicProbabilities: for a given loop, compute how much flow returns
//   to the loop head given one external count.
//
void ProfileSynthesis::ComputeCyclicProbabilities(FlowGraphNaturalLoop* loop)
{
    // Initialize
    //
    loop->VisitLoopBlocks([](BasicBlock* loopBlock) {
        loopBlock->bbWeight = 0.0;
        return BasicBlockVisit::Continue;
    });

    // Process loop blocks in RPO. Just takes one pass through the loop blocks
    // as any cyclic contributions are handled by cyclic probabilities.
    //
    loop->VisitLoopBlocksReversePostOrder([=](BasicBlock* block) {
        // Loop head gets external count of 1
        //
        if (block == loop->GetHeader())
        {
            JITDUMP("ccp: " FMT_BB " :: 1.0\n", block->bbNum);
            block->bbWeight = 1.0;
        }
        else
        {
            FlowGraphNaturalLoop* const nestedLoop = m_loops->GetLoopByHeader(block);

            if (nestedLoop != nullptr)
            {
                // We should have figured this out already.
                //
                assert(m_cyclicProbabilities[nestedLoop->GetIndex()] != 0);

                // Sum entry edges, multply by Cp
                //
                weight_t newWeight = 0.0;

                for (FlowEdge* const edge : nestedLoop->EntryEdges())
                {
                    if (BasicBlock::sameHndRegion(block, edge->getSourceBlock()))
                    {
                        newWeight += edge->getLikelyWeight();
                    }
                }

                newWeight *= m_cyclicProbabilities[nestedLoop->GetIndex()];
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

        return BasicBlockVisit::Continue;
    });

    // Now look at cyclic flow back to the head block.
    //
    weight_t cyclicWeight = 0;
    bool     capped       = false;

    for (FlowEdge* const edge : loop->BackEdges())
    {
        JITDUMP("ccp backedge " FMT_BB " (" FMT_WT ") -> " FMT_BB " likelihood " FMT_WT "\n",
                edge->getSourceBlock()->bbNum, edge->getSourceBlock()->bbWeight, loop->GetHeader()->bbNum,
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
            loop->GetHeader()->bbNum, cyclicWeight, cyclicProbability, capped ? " [capped]" : "");

    m_cyclicProbabilities[loop->GetIndex()] = cyclicProbability;

    // Try and adjust loop exit likelihood to reflect capping.
    // If there are multiple exits we just adjust the first one we can. This is somewhat arbitrary.
    // If there are no exits, there's nothing we can do.
    //
    if (capped && (loop->ExitEdges().size() > 0))
    {
        // Figure out how much flow exits the loop with the capped probability
        // and current block frequencies and exit likelihoods.
        //
        weight_t cappedExitWeight = 0.0;

        for (FlowEdge* const exitEdge : loop->ExitEdges())
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

            for (FlowEdge* const exitEdge : loop->ExitEdges())
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
                if (exitBlock->KindIs(BBJ_COND) && (exitBlockWeight > (missingExitWeight + currentExitWeight)))
                {
                    JITDUMP("Will adjust likelihood of the exit edge from loop exit block " FMT_BB
                            " to reflect capping; current likelihood is " FMT_WT "\n",
                            exitBlock->bbNum, exitEdge->getLikelihood());

                    FlowEdge* const trueEdge           = exitBlock->GetTrueEdge();
                    FlowEdge* const falseEdge          = exitBlock->GetFalseEdge();
                    weight_t const  exitLikelihood     = (missingExitWeight + currentExitWeight) / exitBlockWeight;
                    weight_t const  continueLikelihood = 1.0 - exitLikelihood;

                    // We are making it more likely that the loop exits, so the new exit likelihood
                    // should be greater than the old.
                    //
                    assert(exitLikelihood > exitEdge->getLikelihood());

                    if (trueEdge == exitEdge)
                    {
                        trueEdge->setLikelihood(exitLikelihood);
                        falseEdge->setLikelihood(continueLikelihood);
                    }
                    else
                    {
                        assert(falseEdge == exitEdge);
                        trueEdge->setLikelihood(continueLikelihood);
                        falseEdge->setLikelihood(exitLikelihood);
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
void ProfileSynthesis::ComputeBlockWeights()
{
    JITDUMP("Computing block weights\n");

    for (unsigned i = m_dfsTree->GetPostOrderCount(); i != 0; i--)
    {
        BasicBlock* block = m_dfsTree->GetPostOrder(i - 1);
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
    FlowGraphNaturalLoop* const loop      = m_loops->GetLoopByHeader(block);
    weight_t                    newWeight = block->bbWeight;
    const char*                 kind      = "";

    if (loop != nullptr)
    {
        // Sum all entry edges that aren't EH flow
        //
        for (FlowEdge* const edge : loop->EntryEdges())
        {
            if (BasicBlock::sameHndRegion(block, edge->getSourceBlock()))
            {
                newWeight += edge->getLikelyWeight();
            }
        }

        // Scale by cyclic probability
        //
        newWeight *= m_cyclicProbabilities[loop->GetIndex()];
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
