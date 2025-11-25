// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef FGWASM_H
#define FGWASM_H

// Forward declarations
//
class Scc;

//------------------------------------------------------------------------------
// WasmSuccessorEnumerator: adapter for visiting Wasm successors in a DFS
//
class WasmSuccessorEnumerator
{
    BasicBlock* m_block;
    union
    {
        // We store up to 4 successors inline in the enumerator. For ASP.NET
        // and libraries.pmi this is enough in 99.7% of cases.
        BasicBlock*  m_successors[4];
        BasicBlock** m_pSuccessors;
    };

    unsigned m_numSuccs;
    unsigned m_curSucc = UINT_MAX;

public:
    // Constructs an enumerator of all `block`'s successors.
    WasmSuccessorEnumerator(Compiler* comp, BasicBlock* block, const bool useProfile = false);

    // Gets the block whose successors are enumerated.
    BasicBlock* Block()
    {
        return m_block;
    }

    // Returns the next available successor or `nullptr` if there are no more successors.
    BasicBlock* NextSuccessor()
    {
        m_curSucc++;
        if (m_curSucc >= m_numSuccs)
        {
            return nullptr;
        }

        if (m_numSuccs <= ArrLen(m_successors))
        {
            return m_successors[m_curSucc];
        }

        return m_pSuccessors[m_curSucc];
    }

    // iterator support
    // for (BasicBlock* const succ : WasmSuccessorEnumerator(...))

    class Iterator
    {
    private:
        WasmSuccessorEnumerator* m_enumerator;
        BasicBlock*              m_block;

        void advance()
        {
            if (m_enumerator != nullptr)
            {
                m_block = m_enumerator->NextSuccessor();
            }
        }

    public:

        Iterator(WasmSuccessorEnumerator* enumerator)
            : m_enumerator(enumerator)
            , m_block(nullptr)
        {
            advance();
        }

        BasicBlock* operator*() const
        {
            assert(m_block != nullptr);
            return m_block;
        }

        Iterator& operator++()
        {
            advance();
            return *this;
        }

        bool operator!=(const Iterator& other) const
        {
            return m_block != other.m_block;
        }
    };

    Iterator begin()
    {
        return Iterator(this);
    }

    Iterator end()
    {
        return Iterator(nullptr);
    }
};

//------------------------------------------------------------------------
// WasmInterval
//
// Represents a Wasm BLOCK/END or LOOP/END span in the linearized
// basic block list.
//
class WasmInterval
{
private:

    // m_chain refers to the conflict set member with the lowest m_start.
    // (for "trivial" singleton conflict sets m_chain will be `this`)
    WasmInterval* m_chain;

    // True index of start
    unsigned m_start;

    // True index of end; interval ends just before this block
    unsigned m_end;

    // Largest end index of any chained interval
    unsigned m_chainEnd;

    // true if this is a loop interval (extents cannot change)
    bool m_isLoop;

public:

    WasmInterval(unsigned start, unsigned end, bool isLoop)
        : m_chain(nullptr)
        , m_start(start)
        , m_end(end)
        , m_chainEnd(end)
        , m_isLoop(isLoop)
    {
        m_chain = this;
    }

    unsigned Start() const
    {
        return m_start;
    }

    unsigned End() const
    {
        return m_end;
    }

    unsigned ChainEnd() const
    {
        return m_chainEnd;
    }

    // Call while resolving intervals when building chains.
    WasmInterval* FetchAndUpdateChain()
    {
        if (m_chain == this)
        {
            return this;
        }

        WasmInterval* chain = m_chain->FetchAndUpdateChain();
        m_chain             = chain;
        return chain;
    }

    // Call after intervals are resolved and chains are fixed.
    WasmInterval* Chain() const
    {
        assert((m_chain == this) || (m_chain == m_chain->Chain()));
        return m_chain;
    }

    bool IsLoop() const
    {
        return m_isLoop;
    }

    void SetChain(WasmInterval* c)
    {
        m_chain       = c;
        c->m_chainEnd = max(c->m_chainEnd, m_chainEnd);
    }

    static WasmInterval* NewBlock(Compiler* comp, BasicBlock* start, BasicBlock* end)
    {
        WasmInterval* result =
            new (comp, CMK_WasmCfgLowering) WasmInterval(start->bbPreorderNum, end->bbPreorderNum, /* isLoop */ false);
        return result;
    }

    static WasmInterval* NewLoop(Compiler* comp, BasicBlock* start, BasicBlock* end)
    {
        WasmInterval* result =
            new (comp, CMK_WasmCfgLowering) WasmInterval(start->bbPreorderNum, end->bbPreorderNum, /* isLoop */ true);
        return result;
    }

#ifdef DEBUG
    void Dump(bool chainExtent = false)
    {
        printf("[%03u,%03u]%s", m_start, chainExtent ? m_chainEnd : m_end, m_isLoop && !chainExtent ? " L" : "");

        if (m_chain != this)
        {
            printf(" --> ");
            m_chain->Dump(true);
        }
        else
        {
            printf("\n");
        }
    }
#endif
};

//------------------------------------------------------------------------------
// FgWasm: Wasm-specific flow graph methods
//
class FgWasm
{
private:

    Compiler*         m_comp;
    unsigned          m_sccNum;
    FlowGraphDfsTree* m_dfsTree;
    BitVecTraits      m_traits;

public:

    FgWasm(Compiler* comp)
        : m_comp(comp)
        , m_sccNum(0)
        , m_dfsTree(nullptr)
        , m_traits(0, nullptr)
    {
    }

    Compiler* Comp() const
    {
        return m_comp;
    }

    unsigned GetNextSccNum()
    {
        return m_sccNum++;
    }

    FlowGraphDfsTree* GetDfsTree() const
    {
        return m_dfsTree;
    }

    void SetDfsAndTraits(FlowGraphDfsTree* dfsTree)
    {
        assert(m_dfsTree == nullptr);
        m_dfsTree = dfsTree;
        m_traits  = m_dfsTree->PostOrderTraits();
    }

    BitVecTraits* GetTraits()
    {
        return &m_traits;
    }

    typedef JitHashTable<BasicBlock*, JitPtrKeyFuncs<BasicBlock>, Scc*> SccMap;

    template <typename TFunc>
    static BasicBlockVisit VisitWasmSuccs(Compiler* comp, BasicBlock* block, TFunc func, bool useProfile = false);

    template <typename VisitPreorder, typename VisitPostorder, typename VisitEdge, const bool useProfile = false>
    unsigned WasmRunSubgraphDfs(VisitPreorder  visitPreorder,
                                VisitPostorder visitPostorder,
                                VisitEdge      visitEdge,
                                BitVec&        subgraph);

    FlowGraphDfsTree* WasmDfs(bool& hasBlocksOnlyReachableByEH);

    void WasmFindSccs(ArrayStack<Scc*>& sccs);

    void WasmFindSccsCore(BitVec& subset, ArrayStack<Scc*>& sccs, BasicBlock** postorder, unsigned postorderCount);

    void AssignBlockToScc(BasicBlock* block, BasicBlock* root, BitVec& subset, ArrayStack<Scc*>& sccs, SccMap& map);

    bool WasmTransformSccs(ArrayStack<Scc*>& sccs);
};

#define RETURN_ON_ABORT(expr)                                                                                          \
    if (expr == BasicBlockVisit::Abort)                                                                                \
    {                                                                                                                  \
        return BasicBlockVisit::Abort;                                                                                 \
    }

//------------------------------------------------------------------------------
// VisitWasmSuccs: Visit Wasm successors of this block.
//
// Arguments:
//   comp       - Compiler instance
//   block      - BasicBlock to visit
//   func       - Callback
//   useProfile - visit BBJ_COND successors in increasing likelihood order
//
// Returns:
//   Whether or not the visiting was aborted.
//
// Notes:
//
//  An enumerator of a block's successors for Wasm control flow code gen. Does not
//  consider exceptional successors or successors that require runtime intervention
//  (eg funclet returns).
//
template <typename TFunc>
BasicBlockVisit FgWasm::VisitWasmSuccs(Compiler* comp, BasicBlock* block, TFunc func, bool useProfile)
{
    switch (block->GetKind())
    {
        // Funclet returns have no successors
        //
        case BBJ_EHFINALLYRET:
        case BBJ_EHCATCHRET:
        case BBJ_EHFILTERRET:
        case BBJ_LEAVE:
        case BBJ_THROW:
        case BBJ_RETURN:
        case BBJ_EHFAULTRET:
            return BasicBlockVisit::Continue;

        case BBJ_CALLFINALLY:
            if (block->isBBCallFinallyPair())
            {
                RETURN_ON_ABORT(func(block->Next()));
            }

            return BasicBlockVisit::Continue;

        case BBJ_CALLFINALLYRET:
        case BBJ_ALWAYS:
            return func(block->GetTarget());

        case BBJ_COND:
            if (block->TrueEdgeIs(block->GetFalseEdge()))
            {
                RETURN_ON_ABORT(func(block->GetFalseTarget()));
            }
            else if (useProfile && (block->GetTrueEdge()->getLikelihood() < block->GetFalseEdge()->getLikelihood()))
            {
                // When building an RPO-based block layout, we want to visit the unlikely successor first
                // so that in the DFS computation, the likely successor will be processed right before this block,
                // meaning the RPO-based layout will enable fall-through into the likely successor.
                //
                RETURN_ON_ABORT(func(block->GetTrueTarget()));
                RETURN_ON_ABORT(func(block->GetFalseTarget()));
            }
            else
            {
                RETURN_ON_ABORT(func(block->GetFalseTarget()));
                RETURN_ON_ABORT(func(block->GetTrueTarget()));
            }

            return BasicBlockVisit::Continue;

        case BBJ_SWITCH:
        {
            BBswtDesc* const desc      = block->GetSwitchTargets();
            unsigned const   succCount = desc->GetSuccCount();

            for (unsigned i = 0; i < succCount; i++)
            {
                RETURN_ON_ABORT(func(desc->GetSucc(i)->getDestinationBlock()));
            }

            return BasicBlockVisit::Continue;
        }

        default:
            unreached();
    }
}

#undef RETURN_ON_ABORT

//------------------------------------------------------------------------
// WasmRunSubgraphDfs: Run DFS over a subgraph of the flow graph.
//
// Type parameters:
//   VisitPreorder  - Functor type that takes a BasicBlock* and its preorder number
//   VisitPostorder - Functor type that takes a BasicBlock* and its postorder number
//   VisitEdge      - Functor type that takes two BasicBlock*.
//   useProfile     - If true, determines order of successors visited using profile data
//
// Parameters:
//   comp           - Compiler instance
//   visitPreorder  - Functor to visit block in its preorder
//   visitPostorder - Functor to visit block in its postorder
//   visitEdge      - Functor to visit an edge. Called after visitPreorder (if
//                    this is the first time the successor is seen).
//   subgraphBlocks - bitvector (in postorder num space) identifying the subgraph
//
// Returns:
//   Number of blocks visited.
//
// Notes:
//   Uses block post order numbers.
//   So, visitors must not clobber these numbers.
//
// TODO:
//   Encapsulate subgraph as functor...?
//
template <typename VisitPreorder, typename VisitPostorder, typename VisitEdge, const bool useProfile /* = false */>
unsigned FgWasm::WasmRunSubgraphDfs(VisitPreorder  visitPreorder,
                                    VisitPostorder visitPostorder,
                                    VisitEdge      visitEdge,
                                    BitVec&        subgraph)
{
    JITDUMP("Running Wasm subgraph DFS on %u blocks\n", BitVecOps::Count(&m_traits, subgraph));

    // We should have a wasm DFS for the entire method
    //
    assert(m_dfsTree->IsForWasm());

    BitVec          visited(BitVecOps::MakeEmpty(&m_traits));
    unsigned        preOrderIndex  = 0;
    unsigned        postOrderIndex = 0;
    Compiler* const comp           = Comp();

    ArrayStack<WasmSuccessorEnumerator> blocks(comp->getAllocator(CMK_WasmSccTransform));

    auto dfsFrom = [&](BasicBlock* firstBB) {
        BitVecOps::AddElemD(&m_traits, visited, firstBB->bbPostorderNum);
        blocks.Emplace(comp, firstBB, useProfile);
        JITDUMP(" visiting " FMT_BB "\n", firstBB->bbNum);
        visitPreorder(firstBB, preOrderIndex++);

        while (!blocks.Empty())
        {
            BasicBlock* const block = blocks.TopRef().Block();
            BasicBlock* const succ  = blocks.TopRef().NextSuccessor();

            if (succ != nullptr)
            {
                if (BitVecOps::IsMember(&m_traits, subgraph, succ->bbPostorderNum))
                {
                    if (BitVecOps::TryAddElemD(&m_traits, visited, succ->bbPostorderNum))
                    {
                        blocks.Emplace(comp, succ, useProfile);
                        visitPreorder(succ, preOrderIndex++);
                    }

                    visitEdge(block, succ);
                }

                continue;
            }

            blocks.Pop();
            visitPostorder(block, postOrderIndex++);
        }
    };

    // Find the subgraph entry blocks (blocks that have no pred, or a pred not in the subgraph).
    //
    ArrayStack<BasicBlock*> entries(comp->getAllocator(CMK_WasmSccTransform));

    unsigned        poNum = 0;
    BitVecOps::Iter iterator(&m_traits, subgraph);
    while (iterator.NextElem(&poNum))
    {
        BasicBlock* const block   = m_dfsTree->GetPostOrder(poNum);
        bool              hasPred = false;
        for (BasicBlock* const pred : block->PredBlocks())
        {
            hasPred = true;
            if (!BitVecOps::IsMember(&m_traits, subgraph, pred->bbPostorderNum))
            {
                JITDUMP(FMT_BB " is subgraph entry\n", block->bbNum);
                entries.Emplace(block);
            }
        }

        if (!hasPred)
        {
            JITDUMP(FMT_BB " is an isolated subgraph entry\n", block->bbNum);
            entries.Emplace(block);
        }
    }

    // Kick off a DFS from each unvisited entry
    //
    while (entries.Height() > 0)
    {
        BasicBlock* const block = entries.Pop();

        if (!BitVecOps::IsMember(&m_traits, visited, block->bbPostorderNum))
        {
            dfsFrom(block);
        }
    }

    assert(preOrderIndex == postOrderIndex);
    return preOrderIndex;
}

#endif // FGWASM_H
