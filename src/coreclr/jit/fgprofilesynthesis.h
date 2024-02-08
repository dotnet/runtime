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
    AssignLikelihoodsOnly,
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

    static constexpr weight_t epsilon = 0.001;

private:
    ProfileSynthesis(Compiler* compiler)
        : m_comp(compiler), m_loops(nullptr), m_improperLoopHeaders(0), m_cappedCyclicProbabilities(0)
    {
    }

    static constexpr weight_t exceptionScale     = 0.001;
    static constexpr weight_t blendFactor        = 0.99;
    static constexpr weight_t cappedLikelihood   = 0.999;
    static constexpr weight_t returnLikelihood   = 0.2;
    static constexpr weight_t ilNextLikelihood   = 0.52;
    static constexpr weight_t loopBackLikelihood = 0.9;
    static constexpr weight_t loopExitLikelihood = 0.9;

    void Run(ProfileSynthesisOption option);

    weight_t SumOutgoingLikelihoods(BasicBlock* block, WeightVector* likelihoods = nullptr);

    void AssignLikelihoods();
    void AssignLikelihoodNext(BasicBlock* block);
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

    void AssignInputWeights(ProfileSynthesisOption option);

    void ComputeBlockWeights();
    void ComputeBlockWeight(BasicBlock* block);

private:
    Compiler* const        m_comp;
    FlowGraphDfsTree*      m_dfsTree;
    FlowGraphNaturalLoops* m_loops;
    weight_t*              m_cyclicProbabilities;
    unsigned               m_improperLoopHeaders;
    unsigned               m_cappedCyclicProbabilities;
};

#endif // !_FGPROFILESYNTHESIS_H_
