// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

// Represents a description of how a tree's value changes as a loop iterates.
struct ScalarEvolution
{

#ifdef DEBUG
    void Dump()
    {

    }
#endif
};

typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, ScalarEvolution> ScalarEvolutionMap;

class ScalarEvolutionContext
{
    ScalarEvolutionMap m_map;

public:
    ScalarEvolutionContext(Compiler* comp)
        : m_map(comp->getAllocator(CMK_LoopScalarEvolution))
    {
    }

    ScalarEvolution* Analyze(GenTree* tree)
    {

    }
};

//------------------------------------------------------------------------
// optInductionVariables: Try and optimize induction variables in the method.
//
// Returns:
//   PhaseStatus indicating if anything changed.
//
PhaseStatus Compiler::optInductionVariables()
{
    JITDUMP("*************** In optInductionVariables()\n");

    JITDUMP("\n");
    fgDispBasicBlocks(true);

    ScalarEvolutionContext scevContext(this);

    for (FlowGraphNaturalLoop* loop : m_loops->InReversePostOrder())
    {
        JITDUMP("Analyzing scalar evolution in ");
        FlowGraphNaturalLoop::Dump(loop);

        for (Statement* stmt : loop->GetHeader()->Statements())
        {
            if (!stmt->IsPhiDefnStmt())
            {
                break;
            }

            GenTreeLclVarCommon* phiDef = stmt->GetRootNode()->AsLclVarCommon();
            unsigned lclNum = phiDef->GetLclNum();
            GenTreePhi* phi = phiDef->Data()->AsPhi();

            GenTree* backedgeDef = nullptr;
            GenTree* initDef = nullptr;

            for (GenTreePhi::Use& use : phi->Uses())
            {
                GenTreePhiArg* phiArg = use.GetNode()->AsPhiArg();

            }
        }
    }

    return PhaseStatus::MODIFIED_NOTHING;
}
