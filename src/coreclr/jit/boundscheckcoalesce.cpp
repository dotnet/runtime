// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Bounds Check Coalescing
//
// Within a single block, when multiple GT_BOUNDS_CHECK nodes share the same
// length VN and use constant indices, only the bounds check with the largest
// constant index is actually needed. This pass finds such groups and:
//
//   1. Strengthens the FIRST bounds check in the group by replacing its
//      constant index with the maximum constant index in the group.
//   2. Marks all other bounds checks in the group with GTF_CHK_INDEX_INBND
//      so the existing assertion-prop COMMA handler removes them.
//
// Example: `a[0] + a[1] + a[2] + a[3]` produces four bounds checks with
// indices 0, 1, 2, 3 and the same length. We rewrite the first BC's index
// to 3 and tag the other three for removal. Forward assertion prop then
// drops them as redundant.
//
// Safety:
//   * Strengthening is sound: if the new (stronger) check passes, all the
//     original (weaker) checks would have passed too. If it fails, one of
//     the original checks would have failed too -- both throw the same
//     IndexOutOfRangeException.
//   * We only coalesce bounds checks that are not separated by side effects
//     (calls, indirect/heap stores, atomic ops, memory barriers, or stores
//     to locals that are live in/out of an exception handler in a containing
//     try region). Other bounds checks between members of the group are not
//     barriers (they only throw IOOB, which is the same exception type our
//     strengthened check throws).
//   * We require all candidates in the group to have the same length VN
//     and constant non-negative indices. The first BC's index must itself
//     be a constant so it can be mutated in place.
//
// This phase runs before PHASE_ASSERTION_PROP_MAIN so that the existing
// forward direction of assertion prop sees the strengthened first BC and
// removes the marked-redundant later BCs.
//

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

namespace
{
struct BoundsCheckCandidate
{
    GenTreeBoundsChk* m_bc;
    Statement*        m_stmt;
    ValueNum          m_lenVN;
    int               m_offset;
    int               m_barrierCount;

    BoundsCheckCandidate(GenTreeBoundsChk* bc, Statement* stmt, ValueNum lenVN, int offset, int barrierCount)
        : m_bc(bc)
        , m_stmt(stmt)
        , m_lenVN(lenVN)
        , m_offset(offset)
        , m_barrierCount(barrierCount)
    {
    }
};

//------------------------------------------------------------------------
// IsSideEffectBarrier: check if a node blocks bounds check coalescing
//
// Returns true if a node may have a side effect that should prevent us from
// reordering an earlier bounds-check failure across it.
//
// Stores to tracked locals that are not live in/out of any exception handler
// are not barriers: they cannot be observed if a bounds-check failure is
// reordered to before them.
//
bool IsSideEffectBarrier(Compiler* comp, GenTree* node, bool blockIsInsideTry)
{
    if (node->IsCall())
    {
        return true;
    }
    if (node->OperIs(GT_MEMORYBARRIER))
    {
        return true;
    }
    if (node->OperIsAtomicOp())
    {
        return true;
    }
    if (node->OperIsStore())
    {
        if (!node->OperIsLocalStore())
        {
            return true;
        }
        if (!blockIsInsideTry)
        {
            return false;
        }
        LclVarDsc const* const dsc = comp->lvaGetDesc(node->AsLclVarCommon());
        return !dsc->lvTracked || dsc->lvLiveInOutOfHndlr;
    }
    return false;
}
} // namespace

//------------------------------------------------------------------------
// optBoundsCheckCoalesce: Coalesce bounds checks within each block.
//
// Returns:
//    Suitable phase status.
//
PhaseStatus Compiler::optBoundsCheckCoalesce()
{
    if (!doesMethodHaveBoundsChecks())
    {
        JITDUMP("Method has no bounds checks\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (fgSsaPassesCompleted == 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    bool          modified = false;
    CompAllocator alloc(getAllocator(CMK_AssertionProp));

    // Per-block scratch state, reused across blocks. The candidates stack
    // holds the "head" (first) candidate in each (barrierCount, lenVN) group;
    // followers are tagged GTF_CHK_INDEX_INBND immediately and not retained.
    // groupMap maps a packed (barrierCount, lenVN) key to the candidate index
    // of the group head.
    typedef JitHashTable<UINT64, JitLargePrimitiveKeyFuncs<UINT64>, int> GroupMap;
    ArrayStack<BoundsCheckCandidate>                                     candidates(alloc);
    GroupMap                                                             groupMap(alloc);

    auto const makeKey = [](int barrierCount, ValueNum lenVN) -> UINT64 {
        return (static_cast<UINT64>(static_cast<UINT32>(barrierCount)) << 32) | static_cast<UINT32>(lenVN);
    };

    for (BasicBlock* const block : Blocks())
    {
        candidates.Reset();
        groupMap.RemoveAll();
        int        barrierCount     = 0;
        bool const blockIsInsideTry = block->hasTryIndex();

        for (Statement* const stmt : block->Statements())
        {
            for (GenTree* const node : stmt->TreeList())
            {
                if (IsSideEffectBarrier(this, node, blockIsInsideTry))
                {
                    barrierCount++;
                    continue;
                }

                if (!node->OperIs(GT_BOUNDS_CHECK))
                {
                    continue;
                }

                GenTreeBoundsChk* const bc = node->AsBoundsChk();
                if (bc->gtThrowKind != SCK_RNGCHK_FAIL)
                {
                    continue;
                }

                GenTree* const idx = bc->GetIndex();
                if (!idx->IsIntCnsFitsInI32())
                {
                    continue;
                }

                int const offset = static_cast<int>(idx->AsIntCon()->IconValue());
                if (offset < 0)
                {
                    continue;
                }

                ValueNum const lenVN = vnStore->VNConservativeNormalValue(bc->GetArrayLength()->gtVNPair);
                if (lenVN == ValueNumStore::NoVN)
                {
                    continue;
                }

                UINT64 const key = makeKey(barrierCount, lenVN);
                int          headIndex;
                if (!groupMap.Lookup(key, &headIndex))
                {
                    // First member of this group: record it as the head and keep it
                    // in the candidates stack so we can strengthen it later.
                    groupMap.Set(key, candidates.Height());
                    candidates.Emplace(bc, stmt, lenVN, offset, barrierCount);
                    continue;
                }

                // Follower: tag for forward assertion prop to splice out, and
                // bump the head's running max offset.
                BoundsCheckCandidate& head = candidates.BottomRef(headIndex);
                JITDUMP("BC coalesce in " FMT_BB ": marking [%06u] (offset %d) as redundant of [%06u]\n", block->bbNum,
                        dspTreeID(bc), offset, dspTreeID(head.m_bc));
                bc->gtFlags |= GTF_CHK_INDEX_INBND;
                if (offset > head.m_offset)
                {
                    head.m_offset = offset;
                }
            }
        }

        // Strengthen each group head whose recorded max exceeds its original
        // index. Heads with no stronger follower are left alone -- existing
        // forward assertion prop already handles equal-or-weaker followers.
        for (int i = 0; i < candidates.Height(); i++)
        {
            BoundsCheckCandidate& head     = candidates.BottomRef(i);
            GenTreeIntCon* const  idxCns   = head.m_bc->GetIndex()->AsIntCon();
            int const             original = static_cast<int>(idxCns->IconValue());
            if (head.m_offset == original)
            {
                continue;
            }

            JITDUMP("BC coalesce in " FMT_BB ": strengthen [%06u] offset %d -> %d (lenVN " FMT_VN ")\n", block->bbNum,
                    dspTreeID(head.m_bc), original, head.m_offset, head.m_lenVN);

            idxCns->SetIconValue(head.m_offset);
            idxCns->gtVNPair.SetBoth(vnStore->VNForIntCon(head.m_offset));
            modified = true;
        }
    }

    return modified ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}
