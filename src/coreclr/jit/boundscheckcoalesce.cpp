// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Bounds Check Coalescing
//
// Within a single block, when multiple GT_BOUNDS_CHECK nodes share the same
// length VN and use constant indices, only the bounds check with the largest
// constant index is actually needed. This pass finds such groups and
// strengthens the FIRST bounds check in the group by replacing its constant
// index with the maximum constant index in the group. Forward assertion prop
// then drops the now-redundant later bounds checks.
//
// Example: `a[0] + a[1] + a[2] + a[3]` produces four bounds checks with
// indices 0, 1, 2, 3 and the same length. We rewrite the first BC's index
// to 3; forward assertion prop then drops the other three as redundant.
//
// Safety:
//   * Strengthening is sound: if the new (stronger) check passes, all the
//     original (weaker) checks would have passed too. If it fails, one of
//     the original checks would have failed too -- both throw the same
//     IndexOutOfRangeException.
//   * We only coalesce bounds checks that are not separated by side effects
//     that could change observable exception ordering. We use per-node
//     effect flags from GenTree::OperEffects: calls and ordering-side-effect
//     nodes (e.g. volatile loads) are barriers; nodes that may throw are
//     barriers unless their only exception is IndexOutOfRange (so other
//     bounds checks fall through naturally); heap-visible stores are
//     barriers, as are local stores whose destination is live across an
//     exception handler reachable from this block.
//   * We require all candidates in the group to have the same length VN
//     and constant non-negative indices. The first BC's index must itself
//     be a constant so it can be mutated in place.
//
// This phase runs before PHASE_ASSERTION_PROP_MAIN so that the existing
// forward direction of assertion prop sees the strengthened first BC and
// drops the redundant followers.
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
// reordering an earlier bounds-check failure across it. Uses the per-node
// (non-summary) effect flags from GenTree::OperEffects.
//
// Calls and ordering-side-effect nodes (e.g. volatile loads) are barriers.
//
// A node that may throw is a barrier unless its only possible exception is
// IndexOutOfRange (the same exception our strengthened check throws); this
// is what lets a sibling GT_BOUNDS_CHECK fall through as a non-barrier.
//
// A heap-visible store is a barrier; a store to a tracked local that is not
// live across any exception handler reachable from this block is not.
//
bool IsSideEffectBarrier(Compiler* comp, GenTree* node, bool blockHasEHSuccs)
{
    ExceptionSetFlags  exSet;
    GenTreeFlags const effects = node->OperEffects(comp, &exSet);

    if ((effects & (GTF_CALL | GTF_ORDER_SIDEEFF)) != 0)
    {
        return true;
    }

    if ((effects & GTF_EXCEPT) != 0)
    {
        if ((exSet & ~ExceptionSetFlags::IndexOutOfRangeException) != ExceptionSetFlags::None)
        {
            return true;
        }
    }

    if ((effects & GTF_ASG) != 0)
    {
        if (!node->OperIsLocalStore())
        {
            return true;
        }
        if (!blockHasEHSuccs)
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
    // followers only update the head's running max offset and are not retained.
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
        int        barrierCount    = 0;
        bool const blockHasEHSuccs = block->HasPotentialEHSuccs(this);

        for (Statement* const stmt : block->Statements())
        {
            for (GenTree* const node : stmt->TreeList())
            {
                if (IsSideEffectBarrier(this, node, blockHasEHSuccs))
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

                // Follower: bump the head's running max offset. Once we
                // strengthen the head, forward assertion prop will drop us.
                BoundsCheckCandidate& head = candidates.BottomRef(headIndex);
                JITDUMP("BC coalesce in " FMT_BB ": [%06u] (offset %d) is redundant given [%06u]\n", block->bbNum,
                        dspTreeID(bc), offset, dspTreeID(head.m_bc));
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
