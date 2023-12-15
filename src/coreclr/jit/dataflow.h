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
            for (FlowEdge* pred : block->PredEdges())
            {
                callback.Merge(block, pred->getSourceBlock(), pred->getDupCount());
            }
        }

        if (callback.EndMerge(block))
        {
            // The clients using DataFlow (CSE, assertion prop) currently do
            // not need EH successors here:
            //
            // 1. CSE does not CSE into handlers, so it considers no
            // expressions available at the beginning of handlers;
            //
            // 2. Facts in global assertion prop are VN-based and can only
            // become false because of control flow, so it is sufficient to
            // propagate facts available into the 'try' head block, since that
            // block dominates all other blocks in the 'try'. That will happen
            // as part of processing handlers below.
            //
            block->VisitRegularSuccs(m_pCompiler, [&worklist](BasicBlock* succ) {
                worklist.insert(worklist.end(), succ);
                return BasicBlockVisit::Continue;
            });
        }

        if (m_pCompiler->bbIsTryBeg(block))
        {
            // Handlers of the try are reachable (and may require special
            // handling compared to the normal "at-the-end" propagation above).
            EHblkDsc* eh = m_pCompiler->ehGetDsc(block->getTryIndex());
            do
            {
                worklist.insert(worklist.end(), eh->ExFlowBlock());

                if (eh->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
                {
                    break;
                }

                eh = m_pCompiler->ehGetDsc(eh->ebdEnclosingTryIndex);
            } while (eh->ebdTryBeg == block);
        }
    }
}
