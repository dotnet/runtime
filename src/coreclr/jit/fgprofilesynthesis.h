// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FGPROFILESYNTHESIS_H_
#define _FGPROFILESYNTHESIS_H_

#include "compiler.h"
#include "jitstd.h"

// Flowgraph Profile Synthesis

typedef jitstd::vector<weight_t> WeightVector;

//------------------------------------------------------------------------
// ProfileSynthesisOption: specify behavior of profile synthesis
//
enum class ProfileSynthesisOption
{
    AssignLikelihoods,
    RetainLikelihoods,
    RepairLikelihoods,
    BlendLikelihoods,
    ResetAndSynthesize,
    ReverseLikelihoods,
    RandomLikelihoods,
};

//------------------------------------------------------------------------
// ProfileSynthesis: synthesize, repair, or alter existing profile information
//
class ProfileSynthesis
{
public:
    static void Run(Compiler* compiler, ProfileSynthesisOption option)
    {
        ProfileSynthesis p(compiler);
        p.Run(option);
    }

    static PhaseStatus AdjustThrowEdgeLikelihoods(Compiler* compiler);

    static constexpr weight_t epsilon = 0.001;

private:
    ProfileSynthesis(Compiler* compiler)
        : m_comp(compiler)
        , m_dfsTree(compiler->m_dfsTree)
        , m_loops(compiler->m_loops)
        // Profile synthesis can be run before or after morph, so tolerate (non-)canonical method entries
        , m_entryBlock((compiler->opts.IsOSR() && (compiler->fgEntryBB != nullptr)) ? compiler->fgEntryBB
                                                                                    : compiler->fgFirstBB)
    {
        // If the Compiler object didn't give us flowgraph annotations to use, re-compute them
        if (m_dfsTree == nullptr)
        {
            m_dfsTree = compiler->fgComputeDfs();
            m_loops   = FlowGraphNaturalLoops::Find(m_dfsTree);
        }
        else
        {
            assert(m_loops != nullptr);
        }

        m_improperLoopHeaders = m_loops->ImproperLoopHeaders();

        if (m_loops->NumLoops() > 0)
        {
            m_cyclicProbabilities = new (compiler, CMK_Pgo) weight_t[m_loops->NumLoops()];
        }
    }

    static constexpr weight_t exceptionWeight       = 0.00001;
    static constexpr weight_t initialBlendFactor    = 0.05;
    static constexpr weight_t blendFactorGrowthRate = 3;
    static constexpr weight_t cappedLikelihood      = 0.999;
    static constexpr weight_t returnLikelihood      = 0.2;
    static constexpr weight_t ilNextLikelihood      = 0.52;
    static constexpr weight_t loopBackLikelihood    = 0.9;
    static constexpr weight_t loopExitLikelihood    = 0.9;
    static constexpr weight_t throwLikelihood       = 0;
    static constexpr unsigned maxRepairRetries      = 4;
    static constexpr unsigned maxSolverIterations   = 50;
    static constexpr weight_t maxCount              = 1e12;

    void Run(ProfileSynthesisOption option);

    weight_t SumOutgoingLikelihoods(BasicBlock* block, WeightVector* likelihoods = nullptr);

    void AssignLikelihoods();
    void AssignLikelihoodJump(BasicBlock* block);
    void AssignLikelihoodCond(BasicBlock* block);
    void AssignLikelihoodSwitch(BasicBlock* block);
    void RepairLikelihoods();
    void BlendLikelihoods();
    void ClearLikelihoods();
    void ReverseLikelihoods();
    void RandomizeLikelihoods();

    void ComputeCyclicProbabilities();
    void ComputeCyclicProbabilities(FlowGraphNaturalLoop* loop);

    void AssignInputWeights(weight_t entryBlockWeight);

    void ComputeBlockWeights();
    void ComputeBlockWeight(BasicBlock* block);

    void GaussSeidelSolver();

private:
    Compiler* const        m_comp;
    FlowGraphDfsTree*      m_dfsTree                   = nullptr;
    FlowGraphNaturalLoops* m_loops                     = nullptr;
    BasicBlock*            m_entryBlock                = nullptr;
    weight_t*              m_cyclicProbabilities       = nullptr;
    weight_t               m_blendFactor               = initialBlendFactor;
    weight_t               m_loopExitLikelihood        = loopExitLikelihood;
    weight_t               m_loopBackLikelihood        = loopBackLikelihood;
    weight_t               m_returnLikelihood          = returnLikelihood;
    unsigned               m_improperLoopHeaders       = 0;
    unsigned               m_cappedCyclicProbabilities = 0;
    bool                   m_approximate               = false;
    bool                   m_overflow                  = false;
    bool                   m_hasInfiniteLoop           = false;
};

#endif // !_FGPROFILESYNTHESIS_H_
