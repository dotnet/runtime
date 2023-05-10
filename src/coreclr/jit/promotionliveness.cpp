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
    BitVec LiveIn;
    BitVec LiveOut;
};

bool PromotionLiveness::IsReplacementLiveOut(BasicBlock* bb, unsigned structLcl, unsigned replacementIndex)
{
    BitVec   liveOut   = m_bbInfo[bb->bbNum].LiveOut;
    unsigned baseIndex = m_structLclToTrackedIndex[structLcl];
    return BitVecOps::IsMember(m_bvTraits, liveOut, baseIndex + 1 + replacementIndex);
}

StructUseDeaths PromotionLiveness::GetDeathsForStructLocal(GenTreeLclVarCommon* lcl)
{
    assert(lcl->OperIsLocal());
    BitVec aggDeaths;
    bool   found = m_aggDeaths.Lookup(lcl, &aggDeaths);
    assert(found);

    unsigned       lclNum  = lcl->GetLclNum();
    AggregateInfo* aggInfo = m_aggregates[lclNum];
    return StructUseDeaths(aggDeaths, (unsigned)aggInfo->Replacements.size());
}

bool StructUseDeaths::IsRemainderDying() const
{
    BitVecTraits traits(1 + m_numFields, nullptr);
    return BitVecOps::IsMember(&traits, m_deaths, 0);
}

bool StructUseDeaths::IsReplacementDying(unsigned index) const
{
    BitVecTraits traits(1 + m_numFields, nullptr);
    return BitVecOps::IsMember(&traits, m_deaths, 1 + index);
}

void PromotionLiveness::Run()
{
    m_structLclToTrackedIndex = new (m_compiler, CMK_Promotion) unsigned[m_compiler->lvaTableCnt]{};
    unsigned trackedIndex     = 0;
    for (unsigned lclNum = 0; lclNum < m_compiler->lvaTableCnt; lclNum++)
    {
        AggregateInfo* agg = m_aggregates[lclNum];
        if (agg == nullptr)
        {
            continue;
        }

        m_structLclToTrackedIndex[lclNum] = trackedIndex;
        // Remainder.
        // TODO-TP: We can avoid allocating an index for this when agg->UnpromotedMin == agg->UnpromotedMax,
        // but it makes liveness use/def marking less uniform.
        trackedIndex++;
        // Fields
        trackedIndex += (unsigned)agg->Replacements.size();
    }

    m_bvTraits = new (m_compiler, CMK_Promotion) BitVecTraits(trackedIndex, m_compiler);
    m_bbInfo   = m_compiler->fgAllocateTypeForEachBlk<BasicBlockLiveness>(CMK_Promotion);
    BitVecOps::AssignNoCopy(m_bvTraits, m_liveIn, BitVecOps::MakeEmpty(m_bvTraits));
    BitVecOps::AssignNoCopy(m_bvTraits, m_ehLiveVars, BitVecOps::MakeEmpty(m_bvTraits));

    ComputeUseDefSets();

    InterBlockLiveness();

    FillInLiveness();
}

void PromotionLiveness::ComputeUseDefSets()
{
    for (BasicBlock* block : m_compiler->Blocks())
    {
        BasicBlockLiveness& bb = m_bbInfo[block->bbNum];
        BitVecOps::AssignNoCopy(m_bvTraits, bb.VarUse, BitVecOps::MakeEmpty(m_bvTraits));
        BitVecOps::AssignNoCopy(m_bvTraits, bb.VarDef, BitVecOps::MakeEmpty(m_bvTraits));
        BitVecOps::AssignNoCopy(m_bvTraits, bb.LiveIn, BitVecOps::MakeEmpty(m_bvTraits));
        BitVecOps::AssignNoCopy(m_bvTraits, bb.LiveOut, BitVecOps::MakeEmpty(m_bvTraits));

        for (Statement* stmt : block->Statements())
        {
            for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
            {
                MarkUseDef(lcl, bb.VarUse, bb.VarDef);
            }
        }
    }
}

void PromotionLiveness::MarkUseDef(GenTreeLclVarCommon* lcl, BitSetShortLongRep& useSet, BitSetShortLongRep& defSet)
{
    AggregateInfo* agg = m_aggregates[lcl->GetLclNum()];
    if (agg == nullptr)
    {
        return;
    }

    jitstd::vector<Replacement>& reps  = agg->Replacements;
    bool                         isUse = (lcl->gtFlags & GTF_VAR_DEF) == 0;
    bool                         isDef = !isUse;

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
                MarkIndex(baseIndex + (unsigned)i, isUse, isDef, useSet, defSet);
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
}

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

    bool liveInChanged = BitVecOps::Equal(m_bvTraits, bbInfo.LiveIn, m_liveIn);

    if (liveInChanged)
    {
        BitVecOps::Assign(m_bvTraits, bbInfo.LiveIn, m_liveIn);
    }

    return liveInChanged;
}

void PromotionLiveness::AddHandlerLiveVars(BasicBlock* block, BitVec& ehLiveVars)
{
    assert(m_compiler->ehBlockHasExnFlowDsc(block));
    EHblkDsc* HBtab = m_compiler->ehGetBlockExnFlowDsc(block);

    do
    {
        /* Either we enter the filter first or the catch/finally */
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

        /* If we have nested try's edbEnclosing will provide them */
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

void PromotionLiveness::FillInLiveness()
{
    BitVec life(BitVecOps::MakeEmpty(m_bvTraits));
    BitVec volatileVars(BitVecOps::MakeEmpty(m_bvTraits));

    for (BasicBlock* block : m_compiler->Blocks())
    {
        BasicBlockLiveness& bbInfo = m_bbInfo[block->bbNum];

        BitVecOps::ClearD(m_bvTraits, volatileVars);

        if (m_compiler->ehBlockHasExnFlowDsc(block))
        {
            AddHandlerLiveVars(block, volatileVars);
        }

        BitVecOps::Assign(m_bvTraits, life, bbInfo.LiveOut);

        Statement* firstStmt = block->firstStmt();

        if (firstStmt == nullptr)
        {
            continue;
        }

        Statement* stmt = block->lastStmt();

        while (true)
        {
            Statement* prevStmt = stmt->GetPrevStmt();

            for (GenTree* cur = stmt->GetTreeListEnd(); cur != nullptr; cur = cur->gtPrev)
            {
                FillInLiveness(life, volatileVars, cur->AsLclVarCommon());
            }

            if (stmt == firstStmt)
            {
                break;
            }

            stmt = prevStmt;
        }
    }
}

void PromotionLiveness::FillInLiveness(BitVec& life, BitVec volatileVars, GenTreeLclVarCommon* lcl)
{
    AggregateInfo* agg = m_aggregates[lcl->GetLclNum()];
    if (agg == nullptr)
    {
        return;
    }

    bool isUse = (lcl->gtFlags & GTF_VAR_DEF) == 0;
    bool isDef = !isUse;

    unsigned  baseIndex  = m_structLclToTrackedIndex[lcl->GetLclNum()];
    var_types accessType = lcl->TypeGet();

    if (accessType == TYP_STRUCT)
    {
        if (lcl->OperIs(GT_LCL_ADDR))
        {
            // Retbuf -- these are definitions but we do not know of how much.
            // We never mark them as dead and we never treat them as killing anything.
            assert(isDef);
            return;
        }

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
            }

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
        unsigned offs  = lcl->GetLclOffs();
        size_t   index = Promotion::BinarySearch<Replacement, &Replacement::Offset>(agg->Replacements, offs);
        if ((ssize_t)index < 0)
        {
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
            index             = ~index;
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
