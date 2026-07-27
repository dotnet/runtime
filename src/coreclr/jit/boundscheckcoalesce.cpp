// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Bounds Check Coalescing
//
// Within a single block, when multiple GT_BOUNDS_CHECK nodes share the same
// length VN and use constant indices, only the bounds check with the largest
// constant index is actually needed. This pass finds such groups and
// strengthens the first bounds check in the group by replacing its constant
// index with the maximum constant index in the group. Forward assertion prop
// then drops the now-redundant later bounds checks.
//
// Example: `a[0] + a[1] + a[2] + a[3]` produces four bounds checks with
// indices 0, 1, 2, 3 and the same length. We rewrite the first BC's index
// to 3; forward assertion prop then drops the other three as redundant.
//
// We ensure no observable side effects (other than a bounds check exception)
// can occur between the original check and the subsequent checks.
//
// This phase runs before assertion prop, which then optimizes away
// the trailing, now-redundant checks.
//

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

namespace
{
struct BoundsCheckCandidate
{
    // leading bounds check in a candidate set
    GenTreeBoundsChk* m_bc;

    // array length being checked
    ValueNum m_lenVN;

    // Max index being checked
    int m_offset;

    BoundsCheckCandidate(GenTreeBoundsChk* bc, ValueNum lenVN, int offset)
        : m_bc(bc)
        , m_lenVN(lenVN)
        , m_offset(offset)
    {
    }
};

//------------------------------------------------------------------------
// IsSideEffectBarrier: check if a node blocks bounds check coalescing
//
// Arguments:
//    comp - the compiler instance
//    node - the node to check
//    blockHasEHSuccs - whether the block containing the node has reachable EH successors
//
// Returns:
//    true if a node may have a side effect that should prevent us from
//    coalescing bounds checks across it. Uses the per-node
//    (non-summary) effect flags from GenTree::OperEffects.
//
bool IsSideEffectBarrier(Compiler* comp, GenTree* node, bool blockHasEHSuccs)
{
    // A node that lowers to a helper call requires the call flag but is not a
    // GT_CALL (for example, a variable-distance long shift on 32-bit targets).
    // Treat such nodes as barriers up front: they are effectively calls, and
    // OperEffects/OperRequiresGlobRefFlag do not expect to be queried for them.
    //
    if (node->OperRequiresCallFlag(comp))
    {
        return true;
    }

    ExceptionSetFlags  exSet;
    GenTreeFlags const effects = node->OperEffects(comp, &exSet);

    // Calls are barriers, as are nodes whose evaluation order is explicitly
    // constrained (GTF_ORDER_SIDEEFF): coalescing must not move a strengthened
    // check across an operation the IR says cannot be reordered.
    //
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
        return !dsc->lvTracked || dsc->IsLiveInOutOfHandler();
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

    // Track the current maximum offset seen for a given length VN
    // optimization barrier count.
    //
    struct Key
    {
        int      m_barrierCount;
        ValueNum m_lengthVN;

        Key(int barrierCount, ValueNum lengthVN)
            : m_barrierCount(barrierCount)
            , m_lengthVN(lengthVN)
        {
        }

        bool operator==(const Key& other) const
        {
            return (m_barrierCount == other.m_barrierCount) && (m_lengthVN == other.m_lengthVN);
        }

        static bool Equals(const Key& x, const Key& y)
        {
            return (x.m_barrierCount == y.m_barrierCount) && (x.m_lengthVN == y.m_lengthVN);
        }

        static unsigned GetHashCode(const Key& x)
        {
            return (unsigned)x.m_lengthVN ^ (unsigned)x.m_barrierCount;
        }
    };

    typedef JitHashTable<Key, Key, int> GroupMap;

    bool          modified = false;
    CompAllocator alloc(getAllocator(CMK_AssertionProp));

    ArrayStack<BoundsCheckCandidate> candidates(alloc);
    GroupMap                         groupMap(alloc);

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
                if (node->OperIs(GT_BOUNDS_CHECK))
                {
                    GenTreeBoundsChk* const bc = node->AsBoundsChk();
                    if (bc->gtThrowKind != SCK_RNGCHK_FAIL)
                    {
                        // A bounds check with a different throw kind throws a
                        // different exception. Treat it as a barrier so we do not
                        // reorder a strengthened range check across it and change
                        // which exception is observed.
                        //
                        barrierCount++;
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

                    // Look through comma-wrapped length nodes.
                    //
                    GenTree* const lenNode = bc->GetArrayLength()->gtEffectiveVal();
                    ValueNum const lenVN   = vnStore->VNConservativeNormalValue(lenNode->gtVNPair);
                    if (lenVN == ValueNumStore::NoVN)
                    {
                        continue;
                    }

                    Key key(barrierCount, lenVN);
                    int headIndex;
                    if (!groupMap.Lookup(key, &headIndex))
                    {
                        // First constant-index bounds check with this length VN and this barrier count.
                        // Start a new group.
                        //
                        groupMap.Set(key, candidates.Height());
                        candidates.Emplace(bc, lenVN, offset);
                        continue;
                    }

                    // Following bounds check. See if this is a new max index.
                    //
                    BoundsCheckCandidate& head = candidates.BottomRef(headIndex);
                    JITDUMP("BC coalesce in " FMT_BB ": [%06u] (offset %d) is redundant given [%06u]\n", block->bbNum,
                            dspTreeID(bc), offset, dspTreeID(head.m_bc));
                    if (offset > head.m_offset)
                    {
                        head.m_offset = offset;
                    }
                    continue;
                }

                if (IsSideEffectBarrier(this, node, blockHasEHSuccs))
                {
                    barrierCount++;
                    continue;
                }
            }
        }

        // Revise the check made by the first entry in each group, if we
        // found a subsequent check at a higher constant index.
        //
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
