// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//   This class is used to perform data flow optimizations.
//   An example usage would be:
//
//     DataFlow flow(m_compiler);
//     flow.ForwardAnalysis(callback);
//
//  The "callback" object needs to implement the following member
//  functions that the "flow" object will call as the data flow
//  analysis progresses:
//
//  class Callback
//  {
//  public:
//      void StartMerge(BasicBlock* block);
//      void Merge(BasicBlock* block, BasicBlock* pred, unsigned dupCount);
//      bool EndMerge(BasicBlock* block);
//  };
#pragma once

#include "compiler.h"
#include "jitstd.h"

class DataFlow
{
private:
    DataFlow();

public:
    DataFlow(Compiler* pCompiler);

    template <typename TCallback>
    void ForwardAnalysis(TCallback& callback);

private:
    Compiler* m_compiler;
};

template <typename TCallback>
void DataFlow::ForwardAnalysis(TCallback& callback)
{
    if (m_compiler->m_dfsTree == nullptr)
    {
        m_compiler->m_dfsTree = m_compiler->fgComputeDfs();
    }

    bool changed;
    do
    {
        changed = false;
        for (unsigned i = m_compiler->m_dfsTree->GetPostOrderCount(); i > 0; i--)
        {
            BasicBlock* block = m_compiler->m_dfsTree->GetPostOrder(i - 1);

            callback.StartMerge(block);
            if (m_compiler->bbIsHandlerBeg(block))
            {
                EHblkDsc* ehDsc = m_compiler->ehGetBlockHndDsc(block);
                callback.MergeHandler(block, ehDsc->ebdTryBeg, ehDsc->ebdTryLast);
            }
            else
            {
                for (FlowEdge* pred : block->PredEdges())
                {
                    callback.Merge(block, pred->getSourceBlock(), pred->getDupCount());
                }
            }

            changed |= callback.EndMerge(block);
        }
    } while (changed && m_compiler->m_dfsTree->HasCycle());
}
