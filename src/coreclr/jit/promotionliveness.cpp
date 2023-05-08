#include "jitpch.h"
#include "promotion.h"

struct BasicBlockLiveness
{
    BitVec VarUse;
    BitVec VarDef;
    BitVec LiveIn;
    BitVec LiveOut;
};

void PromotionLiveness::Run()
{
    m_structLclToTrackedIndex = new (m_compiler, CMK_Promotion) unsigned[m_compiler->lvaTableCnt] {};
    unsigned trackedIndex = 0;
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

    m_bbInfo = m_compiler->fgAllocateTypeForEachBlk<BasicBlockLiveness>(CMK_Promotion);
    for (BasicBlock* block : m_compiler->Blocks())
    {
        BasicBlockLiveness& bb = m_bbInfo[block->bbNum];
        BitVecOps::AssignNoCopy(m_bvTraits, bb.VarUse, BitVecOps::MakeEmpty(m_bvTraits));
        BitVecOps::AssignNoCopy(m_bvTraits, bb.VarDef, BitVecOps::MakeEmpty(m_bvTraits));
        BitVecOps::AssignNoCopy(m_bvTraits, bb.LiveIn, BitVecOps::MakeEmpty(m_bvTraits));
        BitVecOps::AssignNoCopy(m_bvTraits, bb.LiveOut, BitVecOps::MakeEmpty(m_bvTraits));
    }

    while (true)
    {
        ComputeUseDefSets();

        InterBlockLiveness();

        FillInLiveness();
    }
}

void PromotionLiveness::ComputeUseDefSets()
{
    for (BasicBlock* block : m_compiler->Blocks())
    {
        BasicBlockLiveness& bb = m_bbInfo[block->bbNum];
        BitVecOps::ClearD(m_bvTraits, bb.VarUse);
        BitVecOps::ClearD(m_bvTraits, bb.VarDef);
        BitVecOps::ClearD(m_bvTraits, bb.LiveOut);

        for (Statement* stmt : block->Statements())
        {
            for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
            {
                MarkUseDef(lcl, bb.VarUse, bb.VarDef);
            }
        }
    }
}

static bool Intersects(unsigned segment1Start, unsigned segment1End, unsigned segment2Start, unsigned segment2End)
{
    if (segment1End <= segment2Start)
    {
        return false;
    }

    if (segment2End <= segment1Start)
    {
        return false;
    }

    return true;
}

void PromotionLiveness::MarkUseDef(GenTreeLclVarCommon* lcl, BitSetShortLongRep& useSet, BitSetShortLongRep& defSet)
{
    AggregateInfo* agg = m_aggregates[lcl->GetLclNum()];
    if (agg == nullptr)
    {
        return;
    }

    jitstd::vector<Replacement>& reps = agg->Replacements;
    bool isUse = (lcl->gtFlags & GTF_VAR_DEF) == 0;
    bool isDef = !isUse;

    unsigned baseIndex = m_structLclToTrackedIndex[lcl->GetLclNum()];
    unsigned offs = lcl->GetLclOffs();
    var_types accessType = lcl->TypeGet();
    size_t index = Promotion::BinarySearch<Replacement, &Replacement::Offset>(reps, offs);

    if (accessType == TYP_STRUCT)
    {
        unsigned size;
        if (lcl->OperIs(GT_LCL_ADDR))
        {
            size = m_compiler->lvaGetDesc(lcl->GetLclNum())->lvExactSize() - offs;
        }
        else
        {
            size = lcl->GetLayout(m_compiler)->GetSize();
        }

        if ((ssize_t)index < 0)
        {
            index = ~index;
            if ((index > 0) && reps[index - 1].Overlaps(offs, size))
            {
                index--;
            }
        }

        unsigned lastEnd = offs;
        bool usesRemainder = false;
        while ((index < reps.size()) && (reps[index].Offset < offs + size))
        {
            Replacement& rep = reps[index];
            bool isFullFieldDef = isDef && (offs <= rep.Offset) && (offs + size >= rep.Offset + genTypeSize(rep.AccessType));
            MarkIndex(baseIndex + (unsigned)index, isUse, isFullFieldDef, useSet, defSet);

            // Check if [lastEnd..rep.Offset) intersects with [UnpromotedMin..UnpromotedMax) to determine if this is a use of the remainder.
            if (isUse && (rep.Offset > lastEnd))
            {
                // TODO-CQ: This doesn't take padding inside the struct into account.
                // We can compute a cumulative indicator that indicates from
                // replacement to replacement whether there is any remainder
                // between them.
                usesRemainder |= Intersects(lastEnd, rep.Offset, agg->UnpromotedMin, agg->UnpromotedMax);
            }
        }

        if (isUse && (lastEnd < offs + size))
        {
            usesRemainder |= Intersects(lastEnd, offs + size, agg->UnpromotedMin, agg->UnpromotedMax);
        }

        bool isFullDefOfRemainder = isDef && (agg->UnpromotedMin >= offs) && (agg->UnpromotedMax <= (offs + size));
        bool isUseOfRemainder = isUse && usesRemainder;
        MarkIndex(baseIndex, isUseOfRemainder, isFullDefOfRemainder, useSet, defSet);
    }
    else
    {
        if ((ssize_t)index < 0)
        {
            unsigned size = genTypeSize(accessType);
            bool isFullDefOfRemainder = isDef && (agg->UnpromotedMin >= offs) && (agg->UnpromotedMax <= (offs + size));
            bool isUseOfRemainder = isUse && Intersects(agg->UnpromotedMin, agg->UnpromotedMax, offs, offs + size);
            MarkIndex(baseIndex, isUseOfRemainder, isFullDefOfRemainder, useSet, defSet);
        }
        else
        {
            // Accessing element.
            MarkIndex(baseIndex + (unsigned)index, isUse, isDef, useSet, defSet);
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

    unsigned baseIndex = m_structLclToTrackedIndex[lcl->GetLclNum()];
    unsigned offs = lcl->GetLclOffs();
    var_types accessType = lcl->TypeGet();
    size_t index = Promotion::BinarySearch<Replacement, &Replacement::Offset>(agg->Replacements, offs);

    if (accessType == TYP_STRUCT)
    {
        if (!BitVecOps::IsMember(m_bvTraits, volatileVars, baseIndex))
        {
            unsigned size;
            if (lcl->OperIs(GT_LCL_ADDR))
            {
                size = m_compiler->lvaGetDesc(lcl)->lvExactSize();
            }
            else
            {
                size = lcl->GetLayout(m_compiler)->GetSize();
            }
            // TODO-TP: GTF_VAR_DEATH annotation here currently means the remainder. We need
            // a side table to attach field liveness information to.
            bool isFullDefOfRemainder = isDef && (agg->UnpromotedMin >= offs) && (agg->UnpromotedMax <= (offs + size));
            if (isFullDefOfRemainder)
            {
                BitVecOps::RemoveElemD(m_bvTraits, life, baseIndex);
            }
        }
    }
    else
    {

    }
}
