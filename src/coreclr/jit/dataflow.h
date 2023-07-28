// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//   This class is used to perform data flow optimizations.
//   An example usage would be:
//
//     DataFlow flow(m_pCompiler);
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
    Compiler* m_pCompiler;
};

template <typename TCallback>
void DataFlow::ForwardAnalysis(TCallback& callback)
{
    jitstd::list<BasicBlock*> worklist(jitstd::allocator<void>(m_pCompiler->getAllocator()));

    worklist.insert(worklist.begin(), m_pCompiler->fgFirstBB);
    while (!worklist.empty())
    {
        BasicBlock* block = *(worklist.begin());
        worklist.erase(worklist.begin());

        callback.StartMerge(block);
        if (m_pCompiler->bbIsHandlerBeg(block))
        {
            EHblkDsc* ehDsc = m_pCompiler->ehGetBlockHndDsc(block);
            callback.MergeHandler(block, ehDsc->ebdTryBeg, ehDsc->ebdTryLast);
        }
        else
        {
            FlowEdge* preds = m_pCompiler->BlockPredsWithEH(block);
            for (FlowEdge* pred = preds; pred; pred = pred->getNextPredEdge())
            {
                callback.Merge(block, pred->getSourceBlock(), pred->getDupCount());
            }
        }

        if (callback.EndMerge(block))
        {
            block->VisitAllSuccs(m_pCompiler, [&worklist](BasicBlock* succ) {
                worklist.insert(worklist.end(), succ);
                return BasicBlockVisit::Continue;
            });
        }
    }
}
