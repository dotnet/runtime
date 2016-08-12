// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//   This class is used to perform data flow optimizations.
//   An example usage would be:
//
//     DataFlow flow(m_pCompiler);
//     flow.ForwardAnalysis(callback);
//
//  The "callback" object needs to implement the necessary callback
//  functions that  the "flow" object will call as the data flow
//  analysis progresses.
//
#pragma once

#include "compiler.h"
#include "jitstd.h"


class DataFlow
{
private:
    DataFlow();

public:
    // The callback interface that needs to be implemented by anyone
    // needing updates by the dataflow object.
    class Callback
    {
    public:
        Callback(Compiler* pCompiler);

        void StartMerge(BasicBlock* block);
        void Merge(BasicBlock* block, BasicBlock* pred, flowList* preds);
        bool EndMerge(BasicBlock* block);

    private:
        Compiler* m_pCompiler;
    };

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
        {
            flowList* preds = m_pCompiler->BlockPredsWithEH(block);
            for (flowList* pred = preds; pred; pred = pred->flNext)
            {
                callback.Merge(block, pred->flBlock, preds);
            }
        }

        if (callback.EndMerge(block))
        {
            AllSuccessorIter succsBegin = block->GetAllSuccs(m_pCompiler).begin();
            AllSuccessorIter succsEnd = block->GetAllSuccs(m_pCompiler).end(); 
            for (AllSuccessorIter succ = succsBegin; succ != succsEnd; ++succ)
            {
                worklist.insert(worklist.end(), *succ);
            }
        }
    }
}

