// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "promotion.h"

struct BasicBlockLiveness
{
    // Variables used before a full definition.
    BitVec VarUse;
    // Variables fully defined before a use.
    // Note that this differs from our normal liveness: partial definitions are
    // NOT marked but they are also not considered uses.
    BitVec VarDef;
    // Variables live-in to this basic block.
    BitVec LiveIn;
    // Variables live-out of this basic block.
    BitVec LiveOut;
};

//------------------------------------------------------------------------
// Run:
//   Compute liveness information pertaining the promoted structs.
//
// Remarks:
//   For each promoted aggregate we compute the liveness for its remainder and
//   all of its fields. Unlike regular liveness we currently do not do any DCE
//   here and so only do the dataflow computation once.
//
//   The liveness information is written into the IR using the normal
//   GTF_VAR_DEATH flag. Note that the semantics of GTF_VAR_DEATH differs from
//   the rest of the JIT for a short while between the liveness is computed and
//   the replacement phase has run: in particular, after this liveness pass you
//   may see a node like:
//
//       LCL_FLD   int    V16 tmp9         [+8] (last use)
//
//   that indicates that this particular field (or the remainder if it wasn't
//   promoted) is dying, not that V16 itself is dying. After replacement has
//   run the semantics align with the rest of the JIT: in the promoted case V16
//   [+8] would be replaced by its promoted field local, and in the remainder
//   case all non-remainder uses of V16 would also be.
//
//   There is one catch which is struct uses of the local. These can indicate
//   deaths of multiple fields and also the remainder, so this information is
//   stored on the side. PromotionLiveness::GetDeathsForStructLocal is used to
//   query this information.
//
//   The liveness information is used by decomposition to avoid creating dead
//   stores, and also to mark the replacement field uses/defs with proper
//   up-to-date liveness information to be used by future phases (forward sub
//   and morph, as of writing this). It is also used to avoid creating
//   unnecessary read-backs; this is mostly just a TP optimization as future
//   liveness passes would be expected to DCE these anyway.
//
//   Avoiding the creation of dead stores to the remainder is especially
//   important as these otherwise would often end up looking like partial
//   definitions, and the other liveness passes handle partial definitions very
//   conservatively and are not able to DCE them.
//
//   Unlike the other liveness passes we keep the per-block liveness
//   information on the side and we do not update BasicBlock::bbLiveIn et al.
//   This relies on downstream phases not requiring/wanting to use per-basic
//   block live-in/live-out/var-use/var-def sets. To be able to update these we
//   would need to give the new locals "regular" tracked indices (i.e. allocate
//   a lvVarIndex).
//
//   The indices allocated and used internally within the liveness computation
//   are "dense" in the sense that the bit vectors only have indices for
//   remainders and the replacement fields introduced by this pass. In other
//   words, we allocate 1 + num_fields indices for each promoted struct local).
//
void PromotionLiveness::Run()
{
    m_structLclToTrackedIndex = new (m_compiler, CMK_Promotion) unsigned[m_aggregates.size()]{};
    unsigned trackedIndex     = 0;
    for (size_t lclNum = 0; lclNum < m_aggregates.size(); lclNum++)
    {
        AggregateInfo* agg = m_aggregates[lclNum];
        if (agg == nullptr)
        {
            continue;
        }

        m_structLclToTrackedIndex[lclNum] = trackedIndex;
        // TODO: We need a scalability limit on these, we cannot always track
        // the remainder and all fields.
        // Remainder.
        trackedIndex++;
        // Fields.
        trackedIndex += (unsigned)agg->Replacements.size();

#ifdef DEBUG
        // Mark the struct local (remainder) and fields as tracked for DISPTREE to properly
        // show last use information.
        m_compiler->lvaGetDesc((unsigned)lclNum)->lvTrackedWithoutIndex = true;
        for (size_t i = 0; i < agg->Replacements.size(); i++)
        {
            m_compiler->lvaGetDesc(agg->Replacements[i].LclNum)->lvTrackedWithoutIndex = true;
        }
#endif
    }

    m_numVars = trackedIndex;

    m_bvTraits = new (m_compiler, CMK_Promotion) BitVecTraits(m_numVars, m_compiler);
    m_bbInfo   = m_compiler->fgAllocateTypeForEachBlk<BasicBlockLiveness>(CMK_Promotion);
    BitVecOps::AssignNoCopy(m_bvTraits, m_liveIn, BitVecOps::MakeEmpty(m_bvTraits));
    BitVecOps::AssignNoCopy(m_bvTraits, m_ehLiveVars, BitVecOps::MakeEmpty(m_bvTraits));

    JITDUMP("Computing liveness for %u remainders/fields\n\n", m_numVars);

    ComputeUseDefSets();

    InterBlockLiveness();

    FillInLiveness();
}

//------------------------------------------------------------------------
// ComputeUseDefSets:
//   Compute the use and def sets for all blocks.
//
void PromotionLiveness::ComputeUseDefSets()
{
    for (BasicBlock* block : m_compiler->Blocks())
    {
        BasicBlockLiveness& bb = m_bbInfo[block->bbNum];
        BitVecOps::AssignNoCopy(m_bvTraits, bb.VarUse, BitVecOps::MakeEmpty(m_bvTraits));
        BitVecOps::AssignNoCopy(m_bvTraits, bb.VarDef, BitVecOps::MakeEmpty(m_bvTraits));
        BitVecOps::AssignNoCopy(m_bvTraits, bb.LiveIn, BitVecOps::MakeEmpty(m_bvTraits));
        BitVecOps::AssignNoCopy(m_bvTraits, bb.LiveOut, BitVecOps::MakeEmpty(m_bvTraits));

        if (m_compiler->compQmarkUsed)
        {
            for (Statement* stmt : block->Statements())
            {
                GenTree* dst;
                GenTree* qmark = m_compiler->fgGetTopLevelQmark(stmt->GetRootNode(), &dst);
                if (qmark == nullptr)
                {
                    for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
                    {
                        MarkUseDef(lcl, bb.VarUse, bb.VarDef);
                    }
                }
                else
                {
                    for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
                    {
                        // Skip liveness updates/marking for defs; they may be conditionally executed.
                        if ((lcl->gtFlags & GTF_VAR_DEF) == 0)
                        {
                            MarkUseDef(lcl, bb.VarUse, bb.VarDef);
                        }
                    }
                }
            }
        }
        else
        {
            for (Statement* stmt : block->Statements())
            {
                for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
                {
                    MarkUseDef(lcl, bb.VarUse, bb.VarDef);
                }
            }
        }

#ifdef DEBUG
        if (m_compiler->verbose)
        {
            BitVec allVars(BitVecOps::Union(m_bvTraits, bb.VarUse, bb.VarDef));
            printf(FMT_BB " USE(%u)=", block->bbNum, BitVecOps::Count(m_bvTraits, bb.VarUse));
            DumpVarSet(bb.VarUse, allVars);
            printf("\n" FMT_BB " DEF(%u)=", block->bbNum, BitVecOps::Count(m_bvTraits, bb.VarDef));
            DumpVarSet(bb.VarDef, allVars);
            printf("\n\n");
        }
#endif
    }
}

//------------------------------------------------------------------------
// MarkUseDef:
//   Mark use/def information for a single appearence of a local.
//
// Parameters:
//   lcl    - The local node
//   useSet - The use set to mark in.
//   defSet - The def set to mark in.
//
void PromotionLiveness::MarkUseDef(GenTreeLclVarCommon* lcl, BitSetShortLongRep& useSet, BitSetShortLongRep& defSet)
{
    AggregateInfo* agg = m_aggregates[lcl->GetLclNum()];
    if (agg == nullptr)
    {
        return;
    }

    jitstd::vector<Replacement>& reps  = agg->Replacements;
    bool                         isDef = (lcl->gtFlags & GTF_VAR_DEF) != 0;
    bool                         isUse = !isDef;

    unsigned  baseIndex  = m_structLclToTrackedIndex[lcl->GetLclNum()];
    var_types accessType = lcl->TypeGet();

    if (accessType == TYP_STRUCT)
    {
        if (lcl->OperIs(GT_LCL_ADDR))
        {
            // For LCL_ADDR this is a retbuf and we expect it to be a def. We
            // don't know the exact size here so we cannot mark anything as
            // being fully defined, thus we can just return.
            assert(isDef);
            return;
        }

        if (lcl->OperIsScalarLocal())
        {
            // Mark remainder and all fields.
            for (size_t i = 0; i <= reps.size(); i++)
            {
                MarkIndex(baseIndex + (unsigned)i, isUse, isDef, useSet, defSet);
            }
        }
        else
        {
            unsigned offs  = lcl->GetLclOffs();
            unsigned size  = lcl->GetLayout(m_compiler)->GetSize();
            size_t   index = Promotion::BinarySearch<Replacement, &Replacement::Offset>(reps, offs);

            if ((ssize_t)index < 0)
            {
                index = ~index;
                if ((index > 0) && reps[index - 1].Overlaps(offs, size))
                {
                    index--;
                }
            }

            while ((index < reps.size()) && (reps[index].Offset < offs + size))
            {
                Replacement& rep = reps[index];
                bool         isFullFieldDef =
                    isDef && (offs <= rep.Offset) && (offs + size >= rep.Offset + genTypeSize(rep.AccessType));
                MarkIndex(baseIndex + 1 + (unsigned)index, isUse, isFullFieldDef, useSet, defSet);
                index++;
            }

            bool isFullDefOfRemainder = isDef && (agg->UnpromotedMin >= offs) && (agg->UnpromotedMax <= (offs + size));
            // TODO-CQ: We could also try to figure out if a use actually touches the remainder, e.g. in some cases
            // a struct use may consist only of promoted fields and does not actually use the remainder.
            MarkIndex(baseIndex, isUse, isFullDefOfRemainder, useSet, defSet);
        }
    }
    else
    {
        unsigned offs  = lcl->GetLclOffs();
        size_t   index = Promotion::BinarySearch<Replacement, &Replacement::Offset>(reps, offs);
        if ((ssize_t)index < 0)
        {
            unsigned size             = genTypeSize(accessType);
            bool isFullDefOfRemainder = isDef && (agg->UnpromotedMin >= offs) && (agg->UnpromotedMax <= (offs + size));
            MarkIndex(baseIndex, isUse, isFullDefOfRemainder, useSet, defSet);
        }
        else
        {
            // Accessing element.
            MarkIndex(baseIndex + 1 + (unsigned)index, isUse, isDef, useSet, defSet);
        }
    }
}

//------------------------------------------------------------------------
// MarkIndex:
//   Mark specific bits in use/def bit vectors depending on whether this is a use def.
//
// Parameters:
//   index  - The index of the bit to set.
//   isUse  - Whether this is a use
//   isDef  - Whether this is a def
//   useSet - The set of uses
//   defSet - The set of defs
//
void PromotionLiveness::MarkIndex(unsigned index, bool isUse, bool isDef, BitVec& useSet, BitVec& defSet)
{
    if (isUse && !BitVecOps::IsMember(m_bvTraits, defSet, index))
    {
        BitVecOps::AddElemD(m_bvTraits, useSet, index);
    }

    if (isDef)
    {
        BitVecOps::AddElemD(m_bvTraits, defSet, index);
    }
}

//------------------------------------------------------------------------
// InterBlockLiveness:
//   Compute the fixpoint.
//
void PromotionLiveness::InterBlockLiveness()
{
    bool changed;
    do
    {
        changed = false;

        for (BasicBlock* block = m_compiler->fgLastBB; block != nullptr; block = block->bbPrev)
        {
            m_hasPossibleBackEdge |= block->bbNext && (block->bbNext->bbNum <= block->bbNum);
            changed |= PerBlockLiveness(block);
        }

        if (!m_hasPossibleBackEdge)
        {
            break;
        }
    } while (changed);

#ifdef DEBUG
    if (m_compiler->verbose)
    {
        for (BasicBlock* block : m_compiler->Blocks())
        {
            BasicBlockLiveness& bbInfo = m_bbInfo[block->bbNum];
            BitVec              allVars(BitVecOps::Union(m_bvTraits, bbInfo.LiveIn, bbInfo.LiveOut));
            printf(FMT_BB " IN (%u)=", block->bbNum, BitVecOps::Count(m_bvTraits, bbInfo.LiveIn));
            DumpVarSet(bbInfo.LiveIn, allVars);
            printf("\n" FMT_BB " OUT(%u)=", block->bbNum, BitVecOps::Count(m_bvTraits, bbInfo.LiveOut));
            DumpVarSet(bbInfo.LiveOut, allVars);
            printf("\n\n");
        }
    }
#endif
}

//------------------------------------------------------------------------
// PerBlockLiveness:
//   Compute liveness for a single block during a single iteration of the
//   fixpoint computation.
//
// Parameters:
//   block - The block
//
bool PromotionLiveness::PerBlockLiveness(BasicBlock* block)
{
    // We disable promotion for GT_JMP methods.
    assert(!block->endsWithJmpMethod(m_compiler));

    BasicBlockLiveness& bbInfo = m_bbInfo[block->bbNum];
    BitVecOps::ClearD(m_bvTraits, bbInfo.LiveOut);
    for (BasicBlock* succ : block->GetAllSuccs(m_compiler))
    {
        BitVecOps::UnionD(m_bvTraits, bbInfo.LiveOut, m_bbInfo[succ->bbNum].LiveIn);
        m_hasPossibleBackEdge |= succ->bbNum <= block->bbNum;
    }

    BitVecOps::LivenessD(m_bvTraits, m_liveIn, bbInfo.VarDef, bbInfo.VarUse, bbInfo.LiveOut);

    if (m_compiler->ehBlockHasExnFlowDsc(block))
    {
        BitVecOps::ClearD(m_bvTraits, m_ehLiveVars);
        AddHandlerLiveVars(block, m_ehLiveVars);
        BitVecOps::UnionD(m_bvTraits, m_liveIn, m_ehLiveVars);
        BitVecOps::UnionD(m_bvTraits, bbInfo.LiveOut, m_ehLiveVars);
        m_hasPossibleBackEdge = true;
    }

    bool liveInChanged = !BitVecOps::Equal(m_bvTraits, bbInfo.LiveIn, m_liveIn);

    if (liveInChanged)
    {
        BitVecOps::Assign(m_bvTraits, bbInfo.LiveIn, m_liveIn);
    }

    return liveInChanged;
}

//------------------------------------------------------------------------
// AddHandlerLiveVars:
//   Find variables that are live-in to handlers reachable by implicit control
//   flow and add them to a specified bit vector.
//
// Parameters:
//   block      - The block
//   ehLiveVars - The bit vector to mark in
//
// Remarks:
//   Similar to Compiler::fgGetHandlerLiveVars used by regular liveness.
//
void PromotionLiveness::AddHandlerLiveVars(BasicBlock* block, BitVec& ehLiveVars)
{
    assert(m_compiler->ehBlockHasExnFlowDsc(block));
    EHblkDsc* HBtab = m_compiler->ehGetBlockExnFlowDsc(block);

    do
    {
        // Either we enter the filter first or the catch/finally
        if (HBtab->HasFilter())
        {
            BitVecOps::UnionD(m_bvTraits, ehLiveVars, m_bbInfo[HBtab->ebdFilter->bbNum].LiveIn);
#if defined(FEATURE_EH_FUNCLETS)
            // The EH subsystem can trigger a stack walk after the filter
            // has returned, but before invoking the handler, and the only
            // IP address reported from this method will be the original
            // faulting instruction, thus everything in the try body
            // must report as live any variables live-out of the filter
            // (which is the same as those live-in to the handler)
            BitVecOps::UnionD(m_bvTraits, ehLiveVars, m_bbInfo[HBtab->ebdHndBeg->bbNum].LiveIn);
#endif // FEATURE_EH_FUNCLETS
        }
        else
        {
            BitVecOps::UnionD(m_bvTraits, ehLiveVars, m_bbInfo[HBtab->ebdHndBeg->bbNum].LiveIn);
        }

        // If we have nested try's edbEnclosing will provide them
        assert((HBtab->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX) ||
               (HBtab->ebdEnclosingTryIndex > m_compiler->ehGetIndex(HBtab)));

        unsigned outerIndex = HBtab->ebdEnclosingTryIndex;
        if (outerIndex == EHblkDsc::NO_ENCLOSING_INDEX)
        {
            break;
        }
        HBtab = m_compiler->ehGetDsc(outerIndex);

    } while (true);

    // If this block is within a filter, we also need to report as live
    // any vars live into enclosed finally or fault handlers, since the
    // filter will run during the first EH pass, and enclosed or enclosing
    // handlers will run during the second EH pass. So all these handlers
    // are "exception flow" successors of the filter.
    //
    // Note we are relying on ehBlockHasExnFlowDsc to return true
    // for any filter block that we should examine here.
    if (block->hasHndIndex())
    {
        const unsigned thisHndIndex   = block->getHndIndex();
        EHblkDsc*      enclosingHBtab = m_compiler->ehGetDsc(thisHndIndex);

        if (enclosingHBtab->InFilterRegionBBRange(block))
        {
            assert(enclosingHBtab->HasFilter());

            // Search the EH table for enclosed regions.
            //
            // All the enclosed regions will be lower numbered and
            // immediately prior to and contiguous with the enclosing
            // region in the EH tab.
            unsigned index = thisHndIndex;

            while (index > 0)
            {
                index--;
                unsigned enclosingIndex = m_compiler->ehGetEnclosingTryIndex(index);
                bool     isEnclosed     = false;

                // To verify this is an enclosed region, search up
                // through the enclosing regions until we find the
                // region associated with the filter.
                while (enclosingIndex != EHblkDsc::NO_ENCLOSING_INDEX)
                {
                    if (enclosingIndex == thisHndIndex)
                    {
                        isEnclosed = true;
                        break;
                    }

                    enclosingIndex = m_compiler->ehGetEnclosingTryIndex(enclosingIndex);
                }

                // If we found an enclosed region, check if the region
                // is a try fault or try finally, and if so, add any
                // locals live into the enclosed region's handler into this
                // block's live-in set.
                if (isEnclosed)
                {
                    EHblkDsc* enclosedHBtab = m_compiler->ehGetDsc(index);

                    if (enclosedHBtab->HasFinallyOrFaultHandler())
                    {
                        BitVecOps::UnionD(m_bvTraits, ehLiveVars, m_bbInfo[enclosedHBtab->ebdHndBeg->bbNum].LiveIn);
                    }
                }
                // Once we run across a non-enclosed region, we can stop searching.
                else
                {
                    break;
                }
            }
        }
    }
}

//------------------------------------------------------------------------
// FillInLiveness:
//   Starting with the live-out set for each basic block do a backwards traversal
//   marking liveness into the IR.
//
void PromotionLiveness::FillInLiveness()
{
    BitVec life(BitVecOps::MakeEmpty(m_bvTraits));
    BitVec volatileVars(BitVecOps::MakeEmpty(m_bvTraits));

    for (BasicBlock* block : m_compiler->Blocks())
    {
        if (block->firstStmt() == nullptr)
        {
            continue;
        }

        BasicBlockLiveness& bbInfo = m_bbInfo[block->bbNum];

        BitVecOps::ClearD(m_bvTraits, volatileVars);

        if (m_compiler->ehBlockHasExnFlowDsc(block))
        {
            AddHandlerLiveVars(block, volatileVars);
        }

        BitVecOps::Assign(m_bvTraits, life, bbInfo.LiveOut);

        Statement* stmt = block->lastStmt();

        while (true)
        {
            GenTree* qmark = nullptr;
            if (m_compiler->compQmarkUsed)
            {
                GenTree* dst;
                qmark = m_compiler->fgGetTopLevelQmark(stmt->GetRootNode(), &dst);
            }

            if (qmark == nullptr)
            {
                for (GenTree* cur = stmt->GetTreeListEnd(); cur != nullptr; cur = cur->gtPrev)
                {
                    FillInLiveness(life, volatileVars, cur->AsLclVarCommon());
                }
            }
            else
            {
                for (GenTree* cur = stmt->GetTreeListEnd(); cur != nullptr; cur = cur->gtPrev)
                {
                    // Skip liveness updates/marking for defs; they may be conditionally executed.
                    if ((cur->gtFlags & GTF_VAR_DEF) == 0)
                    {
                        FillInLiveness(life, volatileVars, cur->AsLclVarCommon());
                    }
                }
            }

            if (stmt == block->firstStmt())
            {
                break;
            }

            stmt = stmt->GetPrevStmt();
        }
    }
}

//------------------------------------------------------------------------
// FillInLiveness:
//   Fill liveness information into the specified IR node.
//
// Parameters:
//   life         - The current life set. Will be read and updated depending on 'lcl'.
//   volatileVars - Bit vector of variables that are live always.
//   lcl          - The IR node to process liveness for and to mark with liveness information.
//
void PromotionLiveness::FillInLiveness(BitVec& life, BitVec volatileVars, GenTreeLclVarCommon* lcl)
{
    AggregateInfo* agg = m_aggregates[lcl->GetLclNum()];
    if (agg == nullptr)
    {
        return;
    }

    bool isDef = (lcl->gtFlags & GTF_VAR_DEF) != 0;
    bool isUse = !isDef;

    unsigned  baseIndex  = m_structLclToTrackedIndex[lcl->GetLclNum()];
    var_types accessType = lcl->TypeGet();

    if (accessType == TYP_STRUCT)
    {
        // We need an external bit set to represent dying fields/remainder on a struct use.
        BitVecTraits aggTraits(1 + (unsigned)agg->Replacements.size(), m_compiler);
        BitVec       aggDeaths(BitVecOps::MakeEmpty(&aggTraits));
        if (lcl->OperIsScalarLocal())
        {
            // Handle remainder and all fields.
            for (size_t i = 0; i <= agg->Replacements.size(); i++)
            {
                unsigned varIndex = baseIndex + (unsigned)i;
                if (BitVecOps::IsMember(m_bvTraits, life, varIndex))
                {
                    if (isDef && !BitVecOps::IsMember(m_bvTraits, volatileVars, varIndex))
                    {
                        BitVecOps::RemoveElemD(m_bvTraits, life, varIndex);
                    }
                }
                else
                {
                    BitVecOps::AddElemD(&aggTraits, aggDeaths, (unsigned)i);

                    if (isUse)
                    {
                        BitVecOps::AddElemD(m_bvTraits, life, varIndex);
                    }
                }
            }
        }
        else
        {
            unsigned offs  = lcl->GetLclOffs();
            unsigned size  = lcl->GetLayout(m_compiler)->GetSize();
            size_t   index = Promotion::BinarySearch<Replacement, &Replacement::Offset>(agg->Replacements, offs);

            if ((ssize_t)index < 0)
            {
                index = ~index;
                if ((index > 0) && agg->Replacements[index - 1].Overlaps(offs, size))
                {
                    index--;
                }
            }

            // Handle fields.
            while ((index < agg->Replacements.size()) && (agg->Replacements[index].Offset < offs + size))
            {
                unsigned     varIndex = baseIndex + 1 + (unsigned)index;
                Replacement& rep      = agg->Replacements[index];
                if (BitVecOps::IsMember(m_bvTraits, life, varIndex))
                {
                    bool isFullFieldDef =
                        isDef && (offs <= rep.Offset) && (offs + size >= rep.Offset + genTypeSize(rep.AccessType));
                    if (isFullFieldDef && !BitVecOps::IsMember(m_bvTraits, volatileVars, varIndex))
                    {
                        BitVecOps::RemoveElemD(m_bvTraits, life, varIndex);
                    }
                }
                else
                {
                    BitVecOps::AddElemD(&aggTraits, aggDeaths, 1 + (unsigned)index);

                    if (isUse)
                    {
                        BitVecOps::AddElemD(m_bvTraits, life, varIndex);
                    }
                }

                index++;
            }

            // Handle remainder.
            if (BitVecOps::IsMember(m_bvTraits, life, baseIndex))
            {
                bool isFullDefOfRemainder =
                    isDef && (agg->UnpromotedMin >= offs) && (agg->UnpromotedMax <= (offs + size));
                if (isFullDefOfRemainder && !BitVecOps::IsMember(m_bvTraits, volatileVars, baseIndex))
                {
                    BitVecOps::RemoveElemD(m_bvTraits, life, baseIndex);
                }
            }
            else
            {
                // TODO-CQ: We could also try to figure out if a use actually touches the remainder, e.g. in some cases
                // a struct use may consist only of promoted fields and does not actually use the remainder.
                BitVecOps::AddElemD(&aggTraits, aggDeaths, 0);

                if (isUse)
                {
                    BitVecOps::AddElemD(m_bvTraits, life, baseIndex);
                }
            }
        }

        m_aggDeaths.Set(lcl, aggDeaths);
    }
    else
    {
        if (lcl->OperIs(GT_LCL_ADDR))
        {
            // Retbuf -- these are definitions but we do not know of how much.
            // We never mark them as dead and we never treat them as killing anything.
            assert(isDef);
            return;
        }

        unsigned offs  = lcl->GetLclOffs();
        size_t   index = Promotion::BinarySearch<Replacement, &Replacement::Offset>(agg->Replacements, offs);
        if ((ssize_t)index < 0)
        {
            // No replacement found, this is a use of the remainder.
            unsigned size = genTypeSize(accessType);
            if (BitVecOps::IsMember(m_bvTraits, life, baseIndex))
            {
                lcl->gtFlags &= ~GTF_VAR_DEATH;

                bool isFullDefOfRemainder =
                    isDef && (agg->UnpromotedMin >= offs) && (agg->UnpromotedMax <= (offs + size));
                if (isFullDefOfRemainder && !BitVecOps::IsMember(m_bvTraits, volatileVars, baseIndex))
                {
                    BitVecOps::RemoveElemD(m_bvTraits, life, baseIndex);
                }
            }
            else
            {
                lcl->gtFlags |= GTF_VAR_DEATH;

                if (isUse)
                {
                    BitVecOps::AddElemD(m_bvTraits, life, baseIndex);
                }
            }
        }
        else
        {
            // Use of a field.
            unsigned varIndex = baseIndex + 1 + (unsigned)index;

            if (BitVecOps::IsMember(m_bvTraits, life, varIndex))
            {
                lcl->gtFlags &= ~GTF_VAR_DEATH;

                if (isDef && !BitVecOps::IsMember(m_bvTraits, volatileVars, varIndex))
                {
                    BitVecOps::RemoveElemD(m_bvTraits, life, varIndex);
                }
            }
            else
            {
                lcl->gtFlags |= GTF_VAR_DEATH;

                if (isUse)
                {
                    BitVecOps::AddElemD(m_bvTraits, life, varIndex);
                }
            }
        }
    }
}

//------------------------------------------------------------------------
// IsReplacementLiveOut:
//   Check if a replacement field is live at the end of a basic block.
//
// Parameters:
//   structLcl        - The struct (base) local
//   replacementIndex - Index of the replacement
//
// Returns:
//   True if the field is in the live-out set.
//
bool PromotionLiveness::IsReplacementLiveOut(BasicBlock* bb, unsigned structLcl, unsigned replacementIndex)
{
    BitVec   liveOut   = m_bbInfo[bb->bbNum].LiveOut;
    unsigned baseIndex = m_structLclToTrackedIndex[structLcl];
    return BitVecOps::IsMember(m_bvTraits, liveOut, baseIndex + 1 + replacementIndex);
}

//------------------------------------------------------------------------
// GetDeathsForStructLocal:
//   Get a data structure that can be used to query liveness information
//   for a specified local node at its position.
//
// Parameters:
//   lcl - The node
//
// Returns:
//   Liveness information.
//
StructDeaths PromotionLiveness::GetDeathsForStructLocal(GenTreeLclVarCommon* lcl)
{
    assert(lcl->OperIsLocal() && lcl->TypeIs(TYP_STRUCT) && (m_aggregates[lcl->GetLclNum()] != nullptr));
    BitVec aggDeaths;
    bool   found = m_aggDeaths.Lookup(lcl, &aggDeaths);
    assert(found);

    unsigned       lclNum  = lcl->GetLclNum();
    AggregateInfo* aggInfo = m_aggregates[lclNum];
    return StructDeaths(aggDeaths, (unsigned)aggInfo->Replacements.size());
}

//------------------------------------------------------------------------
// IsRemainderDying:
//   Check if the remainder is dying.
//
// Returns:
//   True if so.
//
bool StructDeaths::IsRemainderDying() const
{
    BitVecTraits traits(1 + m_numFields, nullptr);
    return BitVecOps::IsMember(&traits, m_deaths, 0);
}

//------------------------------------------------------------------------
// IsReplacementDying:
//   Check if a specific replacement is dying.
//
// Returns:
//   True if so.
//
bool StructDeaths::IsReplacementDying(unsigned index) const
{
    BitVecTraits traits(1 + m_numFields, nullptr);
    return BitVecOps::IsMember(&traits, m_deaths, 1 + index);
}

#ifdef DEBUG
//------------------------------------------------------------------------
// DumpVarSet:
//   Dump a var set to jitstdout.
//
// Parameters:
//   set     - The set to dump
//   allVars - Set of all variables to print whitespace for if not in 'set'.
//             Used for alignment.
//
void PromotionLiveness::DumpVarSet(BitVec set, BitVec allVars)
{
    printf("{");

    const char* sep = "";
    for (size_t i = 0; i < m_aggregates.size(); i++)
    {
        AggregateInfo* agg = m_aggregates[i];
        if (agg == nullptr)
        {
            continue;
        }

        for (size_t j = 0; j <= agg->Replacements.size(); j++)
        {
            unsigned index = (unsigned)(m_structLclToTrackedIndex[i] + j);

            if (BitVecOps::IsMember(m_bvTraits, set, index))
            {
                if (j == 0)
                {
                    printf("%sV%02u(remainder)", sep, (unsigned)i);
                }
                else
                {
                    const Replacement& rep = agg->Replacements[j - 1];
                    printf("%sV%02u.[%03u..%03u)", sep, (unsigned)i, rep.Offset,
                           rep.Offset + genTypeSize(rep.AccessType));
                }
                sep = " ";
            }
            else if (BitVecOps::IsMember(m_bvTraits, allVars, index))
            {
                printf("%s              ", sep);
                sep = " ";
            }
        }
    }

    printf("}");
}
#endif
