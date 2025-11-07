// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef FGWASM_H
#define FGWASM_H

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
BasicBlockVisit VisitWasmSuccs(Compiler* comp, BasicBlock* block, TFunc func, bool useProfile = false)
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

#endif // FGWASM_H
