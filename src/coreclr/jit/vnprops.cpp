// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Contextual VN facts, their provenance, and the IR properties they establish.
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif
#include "valuenum.h"
#include "ssaconfig.h"

//--------------------------------------------------------------------------------
// fgVNDropSurvivedCastException: Drop a cast helper call's InvalidCastExc when a dominating cast of
//    the same object to the same class has been survived, and record this one.
//
// Arguments:
//    call - the CastClass helper call, already value numbered
//
// Notes:
//    The cast's value number is memoized per object and class, so the exception is removed from the
//    call's set here rather than left out when the value number is first built.
//
void Compiler::fgVNDropSurvivedCastException(GenTreeCall* call)
{
    GenTree* const castTo = call->gtArgs.GetUserArgByIndex(0)->GetNode();
    GenTree* const obj    = call->gtArgs.GetUserArgByIndex(1)->GetNode();

    ValueNum const itemL =
        vnStore->VNForFuncNoFolding(TYP_REF, VNF_InvalidCastExc, vnStore->VNNormalValue(obj->gtVNPair.GetLiberal()),
                                    vnStore->VNNormalValue(castTo->gtVNPair.GetLiberal()));
    ValueNum const itemC     = vnStore->VNForFuncNoFolding(TYP_REF, VNF_InvalidCastExc,
                                                           vnStore->VNNormalValue(obj->gtVNPair.GetConservative()),
                                                           vnStore->VNNormalValue(castTo->gtVNPair.GetConservative()));
    bool const     survivedL = fgVNSurvivedException(itemL);
    bool const     survivedC = (itemC == itemL) && survivedL;

    if (survivedL)
    {
        ValueNum norm;
        ValueNum exc;
        vnStore->VNUnpackExc(call->gtVNPair.GetLiberal(), &norm, &exc);
        call->gtVNPair.SetLiberal(vnStore->VNWithExc(norm, vnStore->VNExcSetRemove(exc, itemL)));
    }
    if (survivedC)
    {
        ValueNum norm;
        ValueNum exc;
        vnStore->VNUnpackExc(call->gtVNPair.GetConservative(), &norm, &exc);
        call->gtVNPair.SetConservative(vnStore->VNWithExc(norm, vnStore->VNExcSetRemove(exc, itemC)));
    }
}

// Large witness sets use a sparse Fenwick tree over dominator preorder. Each bucket
// holds the witness with the greatest dominator postorder. A prefix query therefore
// finds a witness dominating the query exactly when one exists: dominance requires
// preorder(witness) <= preorder(query) and postorder(witness) >= postorder(query).
// Both lookup and insertion take O(log(block count)) hash operations. Small sets
// retain their inline/list representation, avoiding an index for ordinary methods.
BasicBlock* Compiler::SurvivedBlocks::FindDominating(FlowGraphDominatorTree* domTree, BasicBlock* block)
{
    if ((more != nullptr) && (more->index != nullptr))
    {
        for (unsigned i = domTree->GetPreorderNum(block) + 1; i != 0; i &= i - 1)
        {
            BasicBlock* witness;
            if (more->index->Lookup(i, &witness) && domTree->Dominates(witness, block))
            {
                return witness;
            }
        }
        return nullptr;
    }

    if (domTree->Dominates(first, block))
    {
        return first;
    }
    if (more != nullptr)
    {
        for (int i = 0; i < more->blocks.Height(); i++)
        {
            BasicBlock* witness = more->blocks.BottomRef(i);
            if (domTree->Dominates(witness, block))
            {
                return witness;
            }
        }
    }
    return nullptr;
}

void Compiler::SurvivedBlocks::IndexBlock(FlowGraphDominatorTree* domTree, BasicBlock* block)
{
    const unsigned count = domTree->GetDfsTree()->GetPostOrderCount();
    for (unsigned i = domTree->GetPreorderNum(block) + 1; i <= count; i += i & (0u - i))
    {
        BasicBlock** slot = more->index->LookupPointerOrAdd(i, nullptr);
        if ((*slot != nullptr) && (domTree->GetPostorderNum(*slot) >= domTree->GetPostorderNum(block)))
        {
            // Every containing bucket already includes this witness as well.
            break;
        }
        *slot = block;
    }
}

//--------------------------------------------------------------------------------
// fgVNSurvivedException: Whether this exception item is ruled out at the current point, and record
//    that the current block survives it. A block one of the recorded blocks dominates adds nothing; a
//    block dominating a recorded one takes its place; any other is a further way of reaching the item
//    and is added. One hash lookup serves both.
//
// Arguments:
//    excItemVN - the exception item
//
// Return Value:
//    The establishing block or covered join, or nullptr when the item has not been survived.
//
BasicBlock* Compiler::fgVNSurvivedException(ValueNum excItemVN)
{
    if ((vnSurvivedExceptions == nullptr) || (compCurBB == nullptr) || (m_domTree == nullptr))
    {
        return nullptr;
    }

    SurvivedBlocks* const blocks = vnSurvivedExceptions->LookupPointerOrAdd(excItemVN, SurvivedBlocks());
    if (blocks->first == nullptr)
    {
        blocks->first = compCurBB;
        return nullptr;
    }

    if (BasicBlock* anchor = blocks->FindDominating(m_domTree, compCurBB))
    {
        return anchor;
    }

    // Not dominated by a survivor. A join reached only through survivors counts as one and is recorded
    // as a survivor itself. Any other block is a further way of reaching the item. The join is judged on
    // its dominance predecessors, so a handler counts every way the try can be left before completing.
    FlowEdge* const domPreds    = BlockDominancePreds(compCurBB);
    bool            joinCovered = domPreds != nullptr;
    for (FlowEdge* edge = domPreds; edge != nullptr; edge = edge->getNextPredEdge())
    {
        BasicBlock* const pred = edge->getSourceBlock();
        if (!m_dfsTree->Contains(pred))
        {
            continue;
        }

        if (blocks->FindDominating(m_domTree, pred) == nullptr)
        {
            joinCovered = false;
            break;
        }
    }

    if ((blocks->more != nullptr) && (blocks->more->index != nullptr))
    {
        blocks->IndexBlock(m_domTree, compCurBB);
        return (joinCovered ? compCurBB : nullptr);
    }

    const int count = 1 + ((blocks->more == nullptr) ? 0 : blocks->more->blocks.Height());
    for (int i = 0; i < count; i++)
    {
        BasicBlock*& witness = (i == 0) ? blocks->first : blocks->more->blocks.BottomRef(i - 1);
        if (m_domTree->Dominates(compCurBB, witness))
        {
            witness = compCurBB;
            return (joinCovered ? compCurBB : nullptr);
        }
    }
    if (blocks->more == nullptr)
    {
        blocks->more = new (getAllocator(CMK_ValueNumber)) SurvivedBlocks::Additional(getAllocator(CMK_ValueNumber));
    }

    // This is a representation threshold, not a limit on the facts we retain.
    if (count == 16)
    {
        blocks->more->index =
            new (getAllocator(CMK_ValueNumber)) SurvivedBlocks::DominanceIndex(getAllocator(CMK_ValueNumber));
        blocks->IndexBlock(m_domTree, blocks->first);
        for (int i = 0; i < blocks->more->blocks.Height(); i++)
        {
            blocks->IndexBlock(m_domTree, blocks->more->blocks.BottomRef(i));
        }
        blocks->IndexBlock(m_domTree, compCurBB);
    }
    else
    {
        blocks->more->blocks.Push(compCurBB);
    }
    return (joinCovered ? compCurBB : nullptr);
}
