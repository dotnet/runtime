//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
    // Used to ask the dataflow object to restart analysis.
    enum UpdateResult
    {
        RestartAnalysis,
        ContinueAnalysis
    };

    // The callback interface that needs to be implemented by anyone
    // needing updates by the dataflow object.
    class Callback
    {
    public:
        Callback(Compiler* pCompiler);

        void StartMerge(BasicBlock* block);
        void Merge(BasicBlock* block, BasicBlock* pred, flowList* preds);
        void EndMerge(BasicBlock* block);
        bool Changed(BasicBlock* block);
        DataFlow::UpdateResult Update(BasicBlock* block);

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
        callback.EndMerge(block);

        if (callback.Changed(block))
        {
            UpdateResult result = callback.Update(block);

            assert(result == DataFlow::ContinueAnalysis);

            AllSuccessorIter succsBegin = block->GetAllSuccs(m_pCompiler).begin();
            AllSuccessorIter succsEnd = block->GetAllSuccs(m_pCompiler).end(); 
            for (AllSuccessorIter succ = succsBegin; succ != succsEnd; ++succ)
            {
                worklist.insert(worklist.end(), *succ);
            }
        }
    }
}

