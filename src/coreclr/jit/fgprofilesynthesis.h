// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _FGPROFILESYNTHESIS_H_
#define _FGPROFILESYNTHESIS_H_

#include "compiler.h"
#include "jitstd.h"

// Flowgraph Profile Synthesis

typedef jitstd::vector<FlowEdge*> EdgeVector;
typedef jitstd::vector<weight_t>  WeightVector;

struct SimpleLoop
{
    SimpleLoop(BasicBlock* head, CompAllocator allocator)
        : m_head(head)
        , m_parent(nullptr)
        , m_blocks(BlockSetOps::UninitVal())
        , m_entryEdges(allocator)
        , m_backEdges(allocator)
        , m_exitEdges(allocator)
        , m_cyclicProbability(0)
        , m_depth(0)
    {
    }

    BasicBlock* m_head;
    SimpleLoop* m_parent;
    BlockSet    m_blocks;
    EdgeVector  m_entryEdges;
    EdgeVector  m_backEdges;
    EdgeVector  m_exitEdges;
    weight_t    m_cyclicProbability;
    unsigned    m_depth;
};

typedef jitstd::vector<SimpleLoop*> LoopVector;

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

private:
    ProfileSynthesis(Compiler* compiler)
        : m_comp(compiler)
        , m_loops(nullptr)
        , m_bbNumToBlockMap(nullptr)
        , m_improperLoopHeaders(0)
        , m_cappedCyclicProbabilities(0)
    {
    }

    static constexpr weight_t exceptionScale = 0.001;
    static constexpr weight_t blendFactor    = 0.99;
    static constexpr weight_t epsilon        = 0.001;

    static constexpr weight_t cappedLikelihood   = 0.999;
    static constexpr weight_t returnLikelihood   = 0.2;
    static constexpr weight_t ilNextLikelihood   = 0.52;
    static constexpr weight_t loopBackLikelihood = 0.9;
    static constexpr weight_t loopExitLikelihood = 0.9;

    void Run(ProfileSynthesisOption option);

    void        BuildReversePostorder();
    static bool IsDfsAncestor(BasicBlock* x, BasicBlock* y);
    bool IsLoopBackEdge(FlowEdge* edge);
    bool IsLoopExitEdge(FlowEdge* edge);

    void        FindLoops();
    SimpleLoop* GetLoopFromHeader(BasicBlock* block);

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
    void ComputeCyclicProbabilities(SimpleLoop* loop);

    void AssignInputWeights(ProfileSynthesisOption option);

    void ComputeBlockWeights();
    void ComputeBlockWeightsSubgraph(BasicBlock* block);
    void ComputeBlockWeight(BasicBlock* block);

private:
    Compiler* const m_comp;
    LoopVector*     m_loops;
    BasicBlock**    m_bbNumToBlockMap;
    unsigned        m_improperLoopHeaders;
    unsigned        m_cappedCyclicProbabilities;
};

#endif // !_FGPROFILESYNTHESIS_H_
