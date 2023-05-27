// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "promotion.h"
#include "jitstd/algorithm.h"

//------------------------------------------------------------------------
// PhysicalPromotion: Promote structs based on primitive access patterns.
//
// Returns:
//    Suitable phase status.
//
PhaseStatus Compiler::PhysicalPromotion()
{
    if (!opts.OptEnabled(CLFLG_STRUCTPROMOTE))
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (fgNoStructPromotion)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if ((JitConfig.JitEnablePhysicalPromotion() == 0) && !compStressCompile(STRESS_PHYSICAL_PROMOTION, 25))
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

#ifdef DEBUG
    static ConfigMethodRange s_range;
    s_range.EnsureInit(JitConfig.JitEnablePhysicalPromotionRange());

    if (!s_range.Contains(info.compMethodHash()))
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif

    Promotion prom(this);
    return prom.Run();
}

// Represents an access into a struct local.
struct Access
{
    ClassLayout* Layout;
    unsigned     Offset;
    var_types    AccessType;

    // Number of times we saw this access.
    unsigned Count = 0;
    // Number of times this access is on the RHS of an assignment.
    unsigned CountAssignmentSource = 0;
    // Number of times this access is on the LHS of an assignment.
    unsigned CountAssignmentDestination = 0;
    unsigned CountCallArgs              = 0;
    unsigned CountReturns               = 0;
    unsigned CountPassedAsRetbuf        = 0;

    weight_t CountWtd                      = 0;
    weight_t CountAssignmentSourceWtd      = 0;
    weight_t CountAssignmentDestinationWtd = 0;
    weight_t CountAssignedFromCallWtd      = 0;
    weight_t CountCallArgsWtd              = 0;
    weight_t CountReturnsWtd               = 0;
    weight_t CountPassedAsRetbufWtd        = 0;

    Access(unsigned offset, var_types accessType, ClassLayout* layout)
        : Layout(layout), Offset(offset), AccessType(accessType)
    {
    }

    unsigned GetAccessSize() const
    {
        return AccessType == TYP_STRUCT ? Layout->GetSize() : genTypeSize(AccessType);
    }

    bool Overlaps(unsigned otherStart, unsigned otherSize) const
    {
        unsigned end = Offset + GetAccessSize();
        if (end <= otherStart)
        {
            return false;
        }

        unsigned otherEnd = otherStart + otherSize;
        if (otherEnd <= Offset)
        {
            return false;
        }

        return true;
    }
};

enum class AccessKindFlags : uint32_t
{
    None                    = 0,
    IsCallArg               = 1,
    IsAssignmentSource      = 2,
    IsAssignmentDestination = 4,
    IsCallRetBuf            = 8,
    IsAssignedFromCall      = 16,
    IsReturned              = 32,
};

inline constexpr AccessKindFlags operator~(AccessKindFlags a)
{
    return (AccessKindFlags)(~(uint32_t)a);
}

inline constexpr AccessKindFlags operator|(AccessKindFlags a, AccessKindFlags b)
{
    return (AccessKindFlags)((uint32_t)a | (uint32_t)b);
}

inline constexpr AccessKindFlags operator&(AccessKindFlags a, AccessKindFlags b)
{
    return (AccessKindFlags)((uint32_t)a & (uint32_t)b);
}

inline AccessKindFlags& operator|=(AccessKindFlags& a, AccessKindFlags b)
{
    return a = (AccessKindFlags)((uint32_t)a | (uint32_t)b);
}

inline AccessKindFlags& operator&=(AccessKindFlags& a, AccessKindFlags b)
{
    return a = (AccessKindFlags)((uint32_t)a & (uint32_t)b);
}

//------------------------------------------------------------------------
// OverlappingReplacements:
//   Find replacements that overlap the specified [offset..offset+size) interval.
//
// Parameters:
//   offset           - Starting offset of interval
//   size             - Size of interval
//   firstReplacement - [out] The first replacement that overlaps
//   endReplacement   - [out, optional] One past the last replacement that overlaps
//
// Returns:
//   True if any replacement overlaps; otherwise false.
//
bool AggregateInfo::OverlappingReplacements(unsigned      offset,
                                            unsigned      size,
                                            Replacement** firstReplacement,
                                            Replacement** endReplacement)
{
    size_t firstIndex = Promotion::BinarySearch<Replacement, &Replacement::Offset>(Replacements, offset);
    if ((ssize_t)firstIndex < 0)
    {
        firstIndex = ~firstIndex;
        if (firstIndex > 0)
        {
            Replacement& lastRepBefore = Replacements[firstIndex - 1];
            if ((lastRepBefore.Offset + genTypeSize(lastRepBefore.AccessType)) > offset)
            {
                // Overlap with last entry starting before offs.
                firstIndex--;
            }
            else if (firstIndex >= Replacements.size())
            {
                // Starts after last replacement ends.
                return false;
            }
        }

        const Replacement& first = Replacements[firstIndex];
        if (first.Offset >= (offset + size))
        {
            // First candidate starts after this ends.
            return false;
        }
    }

    assert((firstIndex < Replacements.size()) && Replacements[firstIndex].Overlaps(offset, size));
    *firstReplacement = &Replacements[firstIndex];

    if (endReplacement != nullptr)
    {
        size_t lastIndex = Promotion::BinarySearch<Replacement, &Replacement::Offset>(Replacements, offset + size);
        if ((ssize_t)lastIndex < 0)
        {
            lastIndex = ~lastIndex;
        }

        // Since we verified above that there is an overlapping replacement
        // we know that lastIndex exists and is the next one that does not
        // overlap.
        assert(lastIndex > 0);
        *endReplacement = Replacements.data() + lastIndex;
    }

    return true;
}

// Tracks all the accesses into one particular struct local.
class LocalUses
{
    jitstd::vector<Access> m_accesses;

public:
    LocalUses(Compiler* comp) : m_accesses(comp->getAllocator(CMK_Promotion))
    {
    }

    //------------------------------------------------------------------------
    // RecordAccess:
    //   Record an access into this local with the specified offset and access type.
    //
    // Parameters:
    //   offs         - The offset being accessed
    //   accessType   - The type of the access
    //   accessLayout - The layout of the access, for accessType == TYP_STRUCT
    //   flags        - Flags classifying the access
    //   weight       - Weight of the block containing the access
    //
    void RecordAccess(
        unsigned offs, var_types accessType, ClassLayout* accessLayout, AccessKindFlags flags, weight_t weight)
    {
        Access* access = nullptr;

        size_t index = 0;
        if (m_accesses.size() > 0)
        {
            index = Promotion::BinarySearch<Access, &Access::Offset>(m_accesses, offs);
            if ((ssize_t)index >= 0)
            {
                do
                {
                    Access& candidateAccess = m_accesses[index];
                    if ((candidateAccess.AccessType == accessType) && (candidateAccess.Layout == accessLayout))
                    {
                        access = &candidateAccess;
                        break;
                    }

                    index++;
                } while (index < m_accesses.size() && m_accesses[index].Offset == offs);
            }
            else
            {
                index = ~index;
            }
        }

        if (access == nullptr)
        {
            access = &*m_accesses.insert(m_accesses.begin() + index, Access(offs, accessType, accessLayout));
        }

        access->Count++;
        access->CountWtd += weight;

        if ((flags & AccessKindFlags::IsAssignmentSource) != AccessKindFlags::None)
        {
            access->CountAssignmentSource++;
            access->CountAssignmentSourceWtd += weight;
        }

        if ((flags & AccessKindFlags::IsAssignmentDestination) != AccessKindFlags::None)
        {
            access->CountAssignmentDestination++;
            access->CountAssignmentDestinationWtd += weight;

            if ((flags & AccessKindFlags::IsAssignedFromCall) != AccessKindFlags::None)
            {
                access->CountAssignedFromCallWtd += weight;
            }
        }

        if ((flags & AccessKindFlags::IsCallArg) != AccessKindFlags::None)
        {
            access->CountCallArgs++;
            access->CountCallArgsWtd += weight;
        }

        if ((flags & AccessKindFlags::IsCallRetBuf) != AccessKindFlags::None)
        {
            access->CountPassedAsRetbuf++;
            access->CountPassedAsRetbufWtd += weight;
        }

        if ((flags & AccessKindFlags::IsReturned) != AccessKindFlags::None)
        {
            access->CountReturns++;
            access->CountReturnsWtd += weight;
        }
    }

    //------------------------------------------------------------------------
    // PickPromotions:
    //   Pick specific replacements to make for this struct local after a set
    //   of accesses have been recorded.
    //
    // Parameters:
    //   comp   - Compiler instance
    //   lclNum - Local num for this struct local
    //   aggregateInfo - [out] Pointer to aggregate info to create and insert replacements into.
    //
    void PickPromotions(Compiler* comp, unsigned lclNum, AggregateInfo** aggregateInfo)
    {
        if (m_accesses.size() <= 0)
        {
            return;
        }

        JITDUMP("Picking promotions for V%02u\n", lclNum);

        assert(*aggregateInfo == nullptr);
        for (size_t i = 0; i < m_accesses.size(); i++)
        {
            const Access& access = m_accesses[i];

            if (access.AccessType == TYP_STRUCT)
            {
                continue;
            }

            if (!EvaluateReplacement(comp, lclNum, access))
            {
                continue;
            }

#ifdef DEBUG
            char buf[32];
            sprintf_s(buf, sizeof(buf), "V%02u.[%03u..%03u)", lclNum, access.Offset,
                      access.Offset + genTypeSize(access.AccessType));
            size_t len  = strlen(buf) + 1;
            char*  bufp = new (comp, CMK_DebugOnly) char[len];
            strcpy_s(bufp, len, buf);
#endif
            unsigned   newLcl = comp->lvaGrabTemp(false DEBUGARG(bufp));
            LclVarDsc* dsc    = comp->lvaGetDesc(newLcl);
            dsc->lvType       = access.AccessType;

            if (*aggregateInfo == nullptr)
            {
                *aggregateInfo = new (comp, CMK_Promotion) AggregateInfo(comp->getAllocator(CMK_Promotion), lclNum);
            }

            (*aggregateInfo)
                ->Replacements.push_back(Replacement(access.Offset, access.AccessType, newLcl DEBUGARG(bufp)));
        }

        JITDUMP("\n");
    }

    //------------------------------------------------------------------------
    // EvaluateReplacement:
    //   Evaluate legality and profitability of a single replacement candidate.
    //
    // Parameters:
    //   comp   - Compiler instance
    //   lclNum - Local num for this struct local
    //   access - Access information for the candidate.
    //
    // Returns:
    //   True if we should promote this access and create a replacement; otherwise false.
    //
    bool EvaluateReplacement(Compiler* comp, unsigned lclNum, const Access& access)
    {
        weight_t countOverlappedCallArgWtd                = 0;
        weight_t countOverlappedReturnsWtd                = 0;
        weight_t countOverlappedRetbufsWtd                = 0;
        weight_t countOverlappedAssignedFromCallWtd       = 0;
        weight_t countOverlappedDecomposableAssignmentWtd = 0;

        bool overlap = false;
        for (const Access& otherAccess : m_accesses)
        {
            if (&otherAccess == &access)
                continue;

            if (!otherAccess.Overlaps(access.Offset, genTypeSize(access.AccessType)))
            {
                continue;
            }

            if (otherAccess.AccessType != TYP_STRUCT)
            {
                return false;
            }

            countOverlappedCallArgWtd += otherAccess.CountCallArgsWtd;
            countOverlappedReturnsWtd += otherAccess.CountReturnsWtd;
            countOverlappedRetbufsWtd += otherAccess.CountPassedAsRetbufWtd;
            countOverlappedAssignedFromCallWtd += otherAccess.CountAssignedFromCallWtd;
            countOverlappedDecomposableAssignmentWtd +=
                (otherAccess.CountAssignmentDestinationWtd + otherAccess.CountAssignmentSourceWtd -
                 otherAccess.CountAssignedFromCallWtd);
        }

        weight_t costWithout = 0;

        // We cost any normal access (which is a struct load or store) without promotion at 3 cycles.
        costWithout += access.CountWtd * 3;

        weight_t costWith = 0;

        // For promoted accesses we expect these to turn into reg-reg movs (and in many cases be fully contained in the
        // parent).
        // We cost these at 0.5 cycles.
        costWith += access.CountWtd * 0.5;

        // Now look at the overlapping struct uses that promotion will make more expensive.

        weight_t   countReadBacksWtd = 0;
        LclVarDsc* lcl               = comp->lvaGetDesc(lclNum);
        // For parameters or OSR locals we always need one read back.
        if (lcl->lvIsParam || lcl->lvIsOSRLocal)
        {
            countReadBacksWtd += comp->fgFirstBB->getBBWeight(comp);
        }

        // If used as a retbuf we need a readback after.
        countReadBacksWtd += countOverlappedRetbufsWtd;

        // The same if the struct was assigned from a call, since we don't
        // currently have any "forwarding" optimization for this case.
        countReadBacksWtd += countOverlappedAssignedFromCallWtd;

        // A readback turns into a stack load that we costed at 3 above.
        costWith += countReadBacksWtd * 3;

        // Write backs with TYP_REFs when the base local is an implicit byref
        // involves checked write barriers, so they are very expensive. We cost that at 10 cycles.
        // TODO-CQ: This should be adjusted once we type implicit byrefs as TYP_I_IMPL.
        // Otherwise we cost it like a store to stack at 3 cycles.
        weight_t writeBackCost = comp->lvaIsImplicitByRefLocal(lclNum) && (access.AccessType == TYP_REF) ? 10 : 3;

        // We write back before an overlapping struct use passed as an arg.
        // TODO-CQ: A store-forwarding optimization in lowering could get rid
        // of these copies; however, it requires lowering to be able to prove
        // that not writing the fields into the struct local is ok.
        weight_t countWriteBacksWtd = countOverlappedCallArgWtd;
        costWith += countWriteBacksWtd * writeBackCost;

        // Overlapping assignments are decomposable so we don't cost them as
        // being more expensive than their unpromoted counterparts (i.e. we
        // don't consider them at all). However, we should do something more
        // clever here, since:
        // * We may still end up writing the full remainder as part of the
        //   decomposed assignment, in which case all the field writes are just
        //   added code size/perf cost.
        // * Even if we don't, decomposing a single struct write into many
        //   field writes is not necessarily profitable (e.g. 16 byte field
        //   stores vs 1 XMM load/store).
        //
        // TODO-CQ: This ends up being a combinatorial optimization problem. We
        // need to take a more "whole-struct" view here and look at sets of
        // fields we are promoting together, evaluating all of them at once in
        // comparison with the covering struct uses. This will also allow us to
        // give a bonus to promoting remainders that may not have scalar uses
        // but will allow fully decomposing assignments away.

        JITDUMP("  Evaluating access %s @ %03u\n", varTypeName(access.AccessType), access.Offset);
        JITDUMP("    Single write-back cost: " FMT_WT "\n", writeBackCost);
        JITDUMP("    Write backs: " FMT_WT "\n", countWriteBacksWtd);
        JITDUMP("    Read backs: " FMT_WT "\n", countReadBacksWtd);
        JITDUMP("    Cost with: " FMT_WT "\n", costWith);
        JITDUMP("    Cost without: " FMT_WT "\n", costWithout);

        if (costWith < costWithout)
        {
            JITDUMP("  Promoting replacement\n\n");
            return true;
        }

#ifdef DEBUG
        if (comp->compStressCompile(Compiler::STRESS_PHYSICAL_PROMOTION_COST, 25))
        {
            JITDUMP("  Promoting replacement due to stress\n\n");
            return true;
        }
#endif

        JITDUMP("  Disqualifying replacement\n\n");
        return false;
    }

#ifdef DEBUG
    void Dump(unsigned lclNum)
    {
        if (m_accesses.size() <= 0)
        {
            return;
        }

        printf("Accesses for V%02u\n", lclNum);
        for (Access& access : m_accesses)
        {
            if (access.AccessType == TYP_STRUCT)
            {
                printf("  [%03u..%03u) as %s\n", access.Offset, access.Offset + access.Layout->GetSize(),
                       access.Layout->GetClassName());
            }
            else
            {
                printf("  %s @ %03u\n", varTypeName(access.AccessType), access.Offset);
            }

            printf("    #:                             (%u, " FMT_WT ")\n", access.Count, access.CountWtd);
            printf("    # assigned from:               (%u, " FMT_WT ")\n", access.CountAssignmentSource,
                   access.CountAssignmentSourceWtd);
            printf("    # assigned to:                 (%u, " FMT_WT ")\n", access.CountAssignmentDestination,
                   access.CountAssignmentDestinationWtd);
            printf("    # as call arg:                 (%u, " FMT_WT ")\n", access.CountCallArgs,
                   access.CountCallArgsWtd);
            printf("    # as retbuf:                   (%u, " FMT_WT ")\n", access.CountPassedAsRetbuf,
                   access.CountPassedAsRetbufWtd);
            printf("    # as returned value:           (%u, " FMT_WT ")\n\n", access.CountReturns,
                   access.CountReturnsWtd);
        }
    }
#endif
};

// Visitor that records information about uses of struct locals.
class LocalsUseVisitor : public GenTreeVisitor<LocalsUseVisitor>
{
    Promotion*  m_prom;
    LocalUses** m_uses;
    BasicBlock* m_curBB = nullptr;

public:
    enum
    {
        DoPreOrder = true,
    };

    LocalsUseVisitor(Promotion* prom) : GenTreeVisitor(prom->m_compiler), m_prom(prom)
    {
        m_uses = new (prom->m_compiler, CMK_Promotion) LocalUses*[prom->m_compiler->lvaCount]{};
    }

    //------------------------------------------------------------------------
    // SetBB:
    //   Set current BB we are visiting. Used to get BB weights for access costing.
    //
    // Parameters:
    //   bb - The current basic block.
    //
    void SetBB(BasicBlock* bb)
    {
        m_curBB = bb;
    }

    //------------------------------------------------------------------------
    // GetUsesByLocal:
    //   Get the uses information for a specified local.
    //
    // Parameters:
    //   bb - The current basic block.
    //
    // Returns:
    //   Information about uses, or null if this local has no uses information
    //   associated with it.
    //
    LocalUses* GetUsesByLocal(unsigned lcl)
    {
        return m_uses[lcl];
    }

    fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* tree = *use;

        if (tree->OperIsAnyLocal())
        {
            GenTreeLclVarCommon* lcl = tree->AsLclVarCommon();
            LclVarDsc*           dsc = m_compiler->lvaGetDesc(lcl);
            if (Promotion::IsCandidateForPhysicalPromotion(dsc))
            {
                var_types       accessType;
                ClassLayout*    accessLayout;
                AccessKindFlags accessFlags;

                if (lcl->OperIs(GT_LCL_ADDR))
                {
                    assert(user->OperIs(GT_CALL) && dsc->IsHiddenBufferStructArg() &&
                           (user->AsCall()->gtArgs.GetRetBufferArg()->GetNode() == lcl));

                    accessType   = TYP_STRUCT;
                    accessLayout = m_compiler->typGetObjLayout(user->AsCall()->gtRetClsHnd);
                    accessFlags  = AccessKindFlags::IsCallRetBuf;
                }
                else
                {
                    accessType   = lcl->TypeGet();
                    accessLayout = accessType == TYP_STRUCT ? lcl->GetLayout(m_compiler) : nullptr;
                    accessFlags  = ClassifyLocalAccess(lcl, user);
                }

                LocalUses* uses = GetOrCreateUses(lcl->GetLclNum());
                unsigned   offs = lcl->GetLclOffs();
                uses->RecordAccess(offs, accessType, accessLayout, accessFlags, m_curBB->getBBWeight(m_compiler));
            }
        }

        return fgWalkResult::WALK_CONTINUE;
    }

private:
    //------------------------------------------------------------------------
    // GetOrCreateUses:
    //   Get the uses information for a local. Create it if it does not already exist.
    //
    // Parameters:
    //   lclNum - The local
    //
    // Returns:
    //   Uses information.
    //
    LocalUses* GetOrCreateUses(unsigned lclNum)
    {
        if (m_uses[lclNum] == nullptr)
        {
            m_uses[lclNum] = new (m_compiler, CMK_Promotion) LocalUses(m_compiler);
        }

        return m_uses[lclNum];
    }

    //------------------------------------------------------------------------
    // ClassifyLocalAccess:
    //   Given a local node and its user, classify information about it.
    //
    // Parameters:
    //   lcl - The local
    //   user - The user of the local.
    //
    // Returns:
    //   Flags classifying the access.
    //
    AccessKindFlags ClassifyLocalAccess(GenTreeLclVarCommon* lcl, GenTree* user)
    {
        assert(lcl->OperIsLocalRead() || lcl->OperIsLocalStore());

        AccessKindFlags flags = AccessKindFlags::None;
        if (lcl->OperIsLocalStore())
        {
            flags |= AccessKindFlags::IsAssignmentDestination;

            if (lcl->AsLclVarCommon()->Data()->gtEffectiveVal()->IsCall())
            {
                flags |= AccessKindFlags::IsAssignedFromCall;
            }
        }

        if (user == nullptr)
        {
            return flags;
        }

        if (user->IsCall())
        {
            for (CallArg& arg : user->AsCall()->gtArgs.Args())
            {
                if (arg.GetNode() == lcl)
                {
                    flags |= AccessKindFlags::IsCallArg;
                    break;
                }
            }
        }

        if (user->OperIsStore() && (user->Data() == lcl))
        {
            flags |= AccessKindFlags::IsAssignmentSource;
        }

        if (user->OperIs(GT_RETURN))
        {
            assert(user->gtGetOp1() == lcl);
            flags |= AccessKindFlags::IsReturned;
        }

        return flags;
    }
};

//------------------------------------------------------------------------
// Replacement::Overlaps:
//   Check if this replacement overlaps the specified range.
//
// Parameters:
//   otherStart - Start of the other range.
//   otherSize  - Size of the other range.
//
// Returns:
//    True if they overlap.
//
bool Replacement::Overlaps(unsigned otherStart, unsigned otherSize) const
{
    unsigned end = Offset + genTypeSize(AccessType);
    if (end <= otherStart)
    {
        return false;
    }

    unsigned otherEnd = otherStart + otherSize;
    if (otherEnd <= Offset)
    {
        return false;
    }

    return true;
}

//------------------------------------------------------------------------
// IntersectsOrAdjacent:
//   Check if this segment intersects or is adjacent to another segment.
//
// Parameters:
//   other - The other segment.
//
// Returns:
//    True if so.
//
bool StructSegments::Segment::IntersectsOrAdjacent(const Segment& other) const
{
    if (End < other.Start)
    {
        return false;
    }

    if (other.End < Start)
    {
        return false;
    }

    return true;
}

//------------------------------------------------------------------------
// Contains:
//   Check if this segment contains another segment.
//
// Parameters:
//   other - The other segment.
//
// Returns:
//    True if so.
//
bool StructSegments::Segment::Contains(const Segment& other) const
{
    return (other.Start >= Start) && (other.End <= End);
}

//------------------------------------------------------------------------
// Merge:
//   Update this segment to also contain another segment.
//
// Parameters:
//   other - The other segment.
//
void StructSegments::Segment::Merge(const Segment& other)
{
    Start = min(Start, other.Start);
    End   = max(End, other.End);
}

//------------------------------------------------------------------------
// Add:
//   Add a segment to the data structure.
//
// Parameters:
//   segment - The segment to add.
//
void StructSegments::Add(const Segment& segment)
{
    size_t index = Promotion::BinarySearch<Segment, &Segment::End>(m_segments, segment.Start);

    if ((ssize_t)index < 0)
    {
        index = ~index;
    }

    m_segments.insert(m_segments.begin() + index, segment);
    size_t endIndex;
    for (endIndex = index + 1; endIndex < m_segments.size(); endIndex++)
    {
        if (!m_segments[index].IntersectsOrAdjacent(m_segments[endIndex]))
        {
            break;
        }

        m_segments[index].Merge(m_segments[endIndex]);
    }

    m_segments.erase(m_segments.begin() + index + 1, m_segments.begin() + endIndex);
}

//------------------------------------------------------------------------
// Subtract:
//   Subtract a segment from the data structure.
//
// Parameters:
//   segment - The segment to subtract.
//
void StructSegments::Subtract(const Segment& segment)
{
    size_t index = Promotion::BinarySearch<Segment, &Segment::End>(m_segments, segment.Start);
    if ((ssize_t)index < 0)
    {
        index = ~index;
    }
    else
    {
        // Start == segment[index].End, which makes it non-interesting.
        index++;
    }

    if (index >= m_segments.size())
    {
        return;
    }

    // Here we know Start < segment[index].End. Do they not intersect at all?
    if (m_segments[index].Start >= segment.End)
    {
        // Does not intersect any segment.
        return;
    }

    assert(m_segments[index].IntersectsOrAdjacent(segment));

    if (m_segments[index].Contains(segment))
    {
        if (segment.Start > m_segments[index].Start)
        {
            // New segment (existing.Start, segment.Start)
            if (segment.End < m_segments[index].End)
            {
                m_segments.insert(m_segments.begin() + index, Segment(m_segments[index].Start, segment.Start));

                // And new segment (segment.End, existing.End)
                m_segments[index + 1].Start = segment.End;
                return;
            }

            m_segments[index].End = segment.Start;
            return;
        }
        if (segment.End < m_segments[index].End)
        {
            // New segment (segment.End, existing.End)
            m_segments[index].Start = segment.End;
            return;
        }

        // Full segment is being removed
        m_segments.erase(m_segments.begin() + index);
        return;
    }

    if (segment.Start > m_segments[index].Start)
    {
        m_segments[index].End = segment.Start;
        index++;
    }

    size_t endIndex = Promotion::BinarySearch<Segment, &Segment::End>(m_segments, segment.End);
    if ((ssize_t)endIndex >= 0)
    {
        m_segments.erase(m_segments.begin() + index, m_segments.begin() + endIndex + 1);
        return;
    }

    endIndex = ~endIndex;
    if (endIndex == m_segments.size())
    {
        m_segments.erase(m_segments.begin() + index, m_segments.end());
        return;
    }

    if (segment.End > m_segments[endIndex].Start)
    {
        m_segments[endIndex].Start = segment.End;
    }

    m_segments.erase(m_segments.begin() + index, m_segments.begin() + endIndex);
}

//------------------------------------------------------------------------
// IsEmpty:
//   Check if the segment tree is empty.
//
// Returns:
//   True if so.
//
bool StructSegments::IsEmpty()
{
    return m_segments.size() == 0;
}

//------------------------------------------------------------------------
// IsSingleSegment:
//   Check if the segment tree contains only a single segment, and return
//   it if so.
//
// Parameters:
//   result - [out] The single segment. Only valid if the method returns true.
//
// Returns:
//   True if so.
//
bool StructSegments::IsSingleSegment(Segment* result)
{
    if (m_segments.size() == 1)
    {
        *result = m_segments[0];
        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// CoveringSegment:
//   Compute a segment that covers all contained segments in this segment tree.
//
// Parameters:
//   result - [out] The single segment. Only valid if the method returns true.
//
// Returns:
//   True if this segment tree was non-empty; otherwise false.
//
bool StructSegments::CoveringSegment(Segment* result)
{
    if (m_segments.size() == 0)
    {
        return false;
    }

    result->Start = m_segments[0].Start;
    result->End   = m_segments[m_segments.size() - 1].End;
    return true;
}

#ifdef DEBUG
//------------------------------------------------------------------------
// Check:
//   Validate that the data structure is normalized and that it equals a
//   specific fixed bit vector.
//
// Parameters:
//   vect - The bit vector
//
// Remarks:
//   This validates that the internal representation is normalized (i.e.
//   all adjacent intervals are merged) and that it contains an index iff
//   the specified vector contains that index.
//
void StructSegments::Check(FixedBitVect* vect)
{
    bool     first = true;
    unsigned last  = 0;
    for (const Segment& segment : m_segments)
    {
        assert(first || (last < segment.Start));
        assert(segment.End <= vect->bitVectGetSize());

        for (unsigned i = last; i < segment.Start; i++)
            assert(!vect->bitVectTest(i));

        for (unsigned i = segment.Start; i < segment.End; i++)
            assert(vect->bitVectTest(i));

        first = false;
        last  = segment.End;
    }

    for (unsigned i = last, size = vect->bitVectGetSize(); i < size; i++)
        assert(!vect->bitVectTest(i));
}

//------------------------------------------------------------------------
// Dump:
//   Dump a string representation of the segment tree to stdout.
//
void StructSegments::Dump()
{
    if (m_segments.size() == 0)
    {
        printf("<empty>");
    }
    else
    {
        const char* sep = "";
        for (const Segment& segment : m_segments)
        {
            printf("%s[%03u..%03u)", sep, segment.Start, segment.End);
            sep = " ";
        }
    }
}
#endif

//------------------------------------------------------------------------
// SignificantSegments:
//   Compute a segment tree containing all significant (non-padding) segments
//   for the specified class layout.
//
// Parameters:
//   compiler    - Compiler instance
//   layout      - The layout
//   bitVectRept - In debug, a bit vector that represents the same segments as the returned segment tree.
//                 Used for verification purposes.
//
// Returns:
//   Segment tree containing all significant parts of the layout.
//
StructSegments Promotion::SignificantSegments(Compiler*    compiler,
                                              ClassLayout* layout DEBUGARG(FixedBitVect** bitVectRepr))
{
    COMP_HANDLE compHnd = compiler->info.compCompHnd;

    bool significantPadding;
    if (layout->IsBlockLayout())
    {
        significantPadding = true;
        JITDUMP("  Block op has significant padding due to block layout\n");
    }
    else
    {
        uint32_t attribs = compHnd->getClassAttribs(layout->GetClassHandle());
        if ((attribs & CORINFO_FLG_INDEXABLE_FIELDS) != 0)
        {
            significantPadding = true;
            JITDUMP("  Block op has significant padding due to indexable fields\n");
        }
        else if ((attribs & CORINFO_FLG_DONT_DIG_FIELDS) != 0)
        {
            significantPadding = true;
            JITDUMP("  Block op has significant padding due to CORINFO_FLG_DONT_DIG_FIELDS\n");
        }
        else if (((attribs & CORINFO_FLG_CUSTOMLAYOUT) != 0) && ((attribs & CORINFO_FLG_CONTAINS_GC_PTR) == 0))
        {
            significantPadding = true;
            JITDUMP("  Block op has significant padding due to CUSTOMLAYOUT without GC pointers\n");
        }
        else
        {
            significantPadding = false;
        }
    }

    StructSegments segments(compiler->getAllocator(CMK_Promotion));

    // Validate with "obviously correct" but less scalable fixed bit vector implementation.
    INDEBUG(FixedBitVect* segmentBitVect = FixedBitVect::bitVectInit(layout->GetSize(), compiler));

    if (significantPadding)
    {
        segments.Add(StructSegments::Segment(0, layout->GetSize()));

#ifdef DEBUG
        for (unsigned i = 0; i < layout->GetSize(); i++)
            segmentBitVect->bitVectSet(i);
#endif
    }
    else
    {
        unsigned numFields = compHnd->getClassNumInstanceFields(layout->GetClassHandle());
        for (unsigned i = 0; i < numFields; i++)
        {
            CORINFO_FIELD_HANDLE fieldHnd  = compHnd->getFieldInClass(layout->GetClassHandle(), (int)i);
            unsigned             fldOffset = compHnd->getFieldOffset(fieldHnd);
            CORINFO_CLASS_HANDLE fieldClassHandle;
            CorInfoType          corType = compHnd->getFieldType(fieldHnd, &fieldClassHandle);
            var_types            varType = JITtype2varType(corType);
            unsigned             size    = genTypeSize(varType);
            if (size == 0)
            {
                // TODO-CQ: Recursively handle padding in sub structures
                // here. Might be better to introduce a single JIT-EE call
                // to query the significant segments -- that would also be
                // usable by R2R even outside the version bubble in many
                // cases.
                size = compHnd->getClassSize(fieldClassHandle);
                assert(size != 0);
            }

            segments.Add(StructSegments::Segment(fldOffset, fldOffset + size));
#ifdef DEBUG
            for (unsigned i = 0; i < size; i++)
                segmentBitVect->bitVectSet(fldOffset + i);
#endif
        }
    }

#ifdef DEBUG
    if (bitVectRepr != nullptr)
    {
        *bitVectRepr = segmentBitVect;
    }
#endif

    // TODO-TP: Cache this per class layout, we call this for every struct
    // operation on a promoted local.
    return segments;
}

//------------------------------------------------------------------------
// CreateWriteBack:
//   Create IR that writes a replacement local's value back to its struct local:
//
//     STORE_LCL_FLD int V00 [+4]
//       LCL_VAR int V01
//
// Parameters:
//   compiler - Compiler instance
//   structLclNum - Struct local
//   replacement  - Information about the replacement
//
// Returns:
//   IR node.
//
GenTree* Promotion::CreateWriteBack(Compiler* compiler, unsigned structLclNum, const Replacement& replacement)
{
    GenTree* value = compiler->gtNewLclVarNode(replacement.LclNum);
    GenTree* store = compiler->gtNewStoreLclFldNode(structLclNum, replacement.AccessType, replacement.Offset, value);
    return store;
}

//------------------------------------------------------------------------
// CreateReadBack:
//   Create IR that reads a replacement local's value back from its struct local:
//
//     STORE_LCL_VAR int V01
//       LCL_FLD int V00 [+4]
//
// Parameters:
//   compiler - Compiler instance
//   structLclNum - Struct local
//   replacement  - Information about the replacement
//
// Returns:
//   IR node.
//
GenTree* Promotion::CreateReadBack(Compiler* compiler, unsigned structLclNum, const Replacement& replacement)
{
    GenTree* value = compiler->gtNewLclFldNode(structLclNum, replacement.AccessType, replacement.Offset);
    GenTree* store = compiler->gtNewStoreLclVarNode(replacement.LclNum, value);
    return store;
}

//------------------------------------------------------------------------
// EndBlock:
//   Handle reaching the end of the currently started block by preparing
//   internal state for upcoming basic blocks, and inserting any necessary
//   readbacks.
//
// Remarks:
//   We currently expect all fields to be most up-to-date in their field locals
//   at the beginning of every basic block. That means all replacements should
//   have Replacement::NeedsReadBack == false and Replacement::NeedsWriteBack
//   == true at the beginning of every block. This function makes it so that is
//   the case.
//
void ReplaceVisitor::EndBlock()
{
    for (AggregateInfo* agg : m_aggregates)
    {
        if (agg == nullptr)
        {
            continue;
        }

        for (size_t i = 0; i < agg->Replacements.size(); i++)
        {
            Replacement& rep = agg->Replacements[i];
            assert(!rep.NeedsReadBack || !rep.NeedsWriteBack);
            if (rep.NeedsReadBack)
            {
                if (m_liveness->IsReplacementLiveOut(m_currentBlock, agg->LclNum, (unsigned)i))
                {
                    JITDUMP("Reading back replacement V%02u.[%03u..%03u) -> V%02u near the end of " FMT_BB ":\n",
                            agg->LclNum, rep.Offset, rep.Offset + genTypeSize(rep.AccessType), rep.LclNum,
                            m_currentBlock->bbNum);

                    GenTree*   readBack = Promotion::CreateReadBack(m_compiler, agg->LclNum, rep);
                    Statement* stmt     = m_compiler->fgNewStmtFromTree(readBack);
                    DISPSTMT(stmt);
                    m_compiler->fgInsertStmtNearEnd(m_currentBlock, stmt);
                }
                else
                {
                    JITDUMP("Skipping reading back dead replacement V%02u.[%03u..%03u) -> V%02u near the end of " FMT_BB
                            "\n",
                            agg->LclNum, rep.Offset, rep.Offset + genTypeSize(rep.AccessType), rep.LclNum,
                            m_currentBlock->bbNum);
                }
                rep.NeedsReadBack = false;
            }

            rep.NeedsWriteBack = true;
        }
    }

    m_hasPendingReadBacks = false;
}

Compiler::fgWalkResult ReplaceVisitor::PostOrderVisit(GenTree** use, GenTree* user)
{
    GenTree* tree = *use;

    use = InsertMidTreeReadBacksIfNecessary(use);

    if (tree->OperIsStore())
    {
        if (tree->OperIsLocalStore())
        {
            ReplaceLocal(use, user);
        }

        // Stores can be decomposed directly into accesses of the replacements.
        HandleStore(use, user);
        return fgWalkResult::WALK_CONTINUE;
    }

    if (tree->OperIs(GT_CALL))
    {
        // Calls need to store replacements back into the struct local for args
        // and need to restore replacements from the result (for
        // retbufs/returns).
        LoadStoreAroundCall((*use)->AsCall(), user);
        return fgWalkResult::WALK_CONTINUE;
    }

    if (tree->OperIs(GT_RETURN))
    {
        // Returns need to store replacements back into the struct local.
        StoreBeforeReturn((*use)->AsUnOp());
        return fgWalkResult::WALK_CONTINUE;
    }

    if (tree->OperIs(GT_LCL_VAR, GT_LCL_FLD))
    {
        ReplaceLocal(use, user);
        return fgWalkResult::WALK_CONTINUE;
    }

    return fgWalkResult::WALK_CONTINUE;
}

//------------------------------------------------------------------------
// InsertMidTreeReadBacksIfNecessary:
//   If necessary, insert IR to read back all replacements before the specified use.
//
// Parameters:
//   use - The use
//
// Returns:
//   New use pointing to the old tree.
//
// Remarks:
//   When a struct field is most up-to-date in its struct local it is marked to
//   need a read back. We then need to decide when to insert IR to read it back
//   to its field local.
//
//   We normally do this before the first use of the field we find, or before
//   we transfer control to any successor. This method handles the case of
//   implicit control flow related to EH; when this basic block is in a
//   try-region (or filter block) and we find a tree that may throw it eagerly
//   inserts pending readbacks.
//
GenTree** ReplaceVisitor::InsertMidTreeReadBacksIfNecessary(GenTree** use)
{
    if (!m_hasPendingReadBacks || !m_compiler->ehBlockHasExnFlowDsc(m_currentBlock))
    {
        return use;
    }

    if (((*use)->gtFlags & (GTF_EXCEPT | GTF_CALL)) == 0)
    {
        assert(!(*use)->OperMayThrow(m_compiler));
        return use;
    }

    if (!(*use)->OperMayThrow(m_compiler))
    {
        return use;
    }

    JITDUMP("Reading back pending replacements before tree with possible exception side effect inside block in try "
            "region\n");

    for (AggregateInfo* agg : m_aggregates)
    {
        if (agg == nullptr)
        {
            continue;
        }

        for (Replacement& rep : agg->Replacements)
        {
            // TODO-CQ: We should ensure we do not mark dead fields as
            // requiring readback. Currently it is handled by querying liveness
            // as part of end-of-block readback insertion, but for these
            // mid-tree readbacks we cannot query liveness information for
            // arbitrary locals.
            if (!rep.NeedsReadBack)
            {
                continue;
            }

            rep.NeedsReadBack = false;
            GenTree* readBack = Promotion::CreateReadBack(m_compiler, agg->LclNum, rep);
            *use =
                m_compiler->gtNewOperNode(GT_COMMA, (*use)->IsValue() ? (*use)->TypeGet() : TYP_VOID, readBack, *use);
            use           = &(*use)->AsOp()->gtOp2;
            m_madeChanges = true;
        }
    }

    m_hasPendingReadBacks = false;
    return use;
}

//------------------------------------------------------------------------
// LoadStoreAroundCall:
//   Handle a call that may involve struct local arguments and that may
//   pass a struct local with replacements as the retbuf.
//
// Parameters:
//   call - The call
//   user - The user of the call.
//
void ReplaceVisitor::LoadStoreAroundCall(GenTreeCall* call, GenTree* user)
{
    CallArg* retBufArg = nullptr;
    for (CallArg& arg : call->gtArgs.Args())
    {
        if (arg.GetWellKnownArg() == WellKnownArg::RetBuffer)
        {
            retBufArg = &arg;
            continue;
        }

        if (!arg.GetNode()->OperIs(GT_LCL_VAR, GT_LCL_FLD))
        {
            continue;
        }

        GenTreeLclVarCommon* argNodeLcl = arg.GetNode()->AsLclVarCommon();

        if (argNodeLcl->TypeIs(TYP_STRUCT))
        {
            unsigned size = argNodeLcl->GetLayout(m_compiler)->GetSize();
            WriteBackBefore(&arg.EarlyNodeRef(), argNodeLcl->GetLclNum(), argNodeLcl->GetLclOffs(), size);

            if ((m_aggregates[argNodeLcl->GetLclNum()] != nullptr) && IsPromotedStructLocalDying(argNodeLcl))
            {
                argNodeLcl->gtFlags |= GTF_VAR_DEATH;
            }
        }
    }

    if (call->IsOptimizingRetBufAsLocal())
    {
        assert(retBufArg != nullptr);
        assert(retBufArg->GetNode()->OperIs(GT_LCL_ADDR));
        GenTreeLclVarCommon* retBufLcl = retBufArg->GetNode()->AsLclVarCommon();
        unsigned             size      = m_compiler->typGetObjLayout(call->gtRetClsHnd)->GetSize();

        if (MarkForReadBack(retBufLcl->GetLclNum(), retBufLcl->GetLclOffs(), size))
        {
            JITDUMP("Retbuf has replacements that were marked for read back\n");
        }
    }
}

//------------------------------------------------------------------------
// IsPromotedStructLocalDying:
//   Check if a promoted struct local is dying at its current position.
//
// Parameters:
//   lcl - The local
//
// Returns:
//   True if so.
//
// Remarks:
//   This effectively translates our precise liveness information for struct
//   uses into the liveness information that the rest of the JIT expects.
//
//   If the remainder of the struct local is dying, then we expect that this
//   entire struct local is now dying, since all field accesses are going to be
//   replaced with other locals. The exception is if there is a queued read
//   back for any of the fields.
//
bool ReplaceVisitor::IsPromotedStructLocalDying(GenTreeLclVarCommon* lcl)
{
    StructDeaths deaths = m_liveness->GetDeathsForStructLocal(lcl);
    if (!deaths.IsRemainderDying())
    {
        return false;
    }

    AggregateInfo* agg = m_aggregates[lcl->GetLclNum()];
    for (Replacement& rep : agg->Replacements)
    {
        if (rep.NeedsReadBack)
        {
            return false;
        }
    }

    return true;
}

//------------------------------------------------------------------------
// ReplaceLocal:
//   Handle a local that may need to be replaced.
//
// Parameters:
//   use - The use of the local
//   user - The user of the local.
//
// Notes:
//   This usually amounts to making a replacement like
//
//       LCL_FLD int V00 [+8] -> LCL_VAR int V10.
//
//  In some cases we may have a pending read back, meaning that the
//  replacement local is out-of-date compared to the struct local.
//  In that case we also need to insert IR to read it back.
//  This happens for example if the struct local was just assigned from a
//  call or via a block copy.
//
void ReplaceVisitor::ReplaceLocal(GenTree** use, GenTree* user)
{
    GenTreeLclVarCommon* lcl    = (*use)->AsLclVarCommon();
    unsigned             lclNum = lcl->GetLclNum();
    if (m_aggregates[lclNum] == nullptr)
    {
        return;
    }

    jitstd::vector<Replacement>& replacements = m_aggregates[lclNum]->Replacements;

    unsigned  offs       = lcl->GetLclOffs();
    var_types accessType = lcl->TypeGet();

#ifdef DEBUG
    if (accessType == TYP_STRUCT)
    {
        if (lcl->OperIsLocalRead())
        {
            assert((user == nullptr) || user->OperIs(GT_CALL, GT_RETURN) || user->OperIsStore());
        }
    }
    else
    {
        ClassLayout* accessLayout = accessType == TYP_STRUCT ? lcl->GetLayout(m_compiler) : nullptr;
        unsigned     accessSize   = accessLayout != nullptr ? accessLayout->GetSize() : genTypeSize(accessType);
        for (const Replacement& rep : replacements)
        {
            assert(!rep.Overlaps(offs, accessSize) || ((rep.Offset == offs) && (rep.AccessType == accessType)));
        }

        assert((accessType != TYP_STRUCT) || (accessLayout != nullptr));
        JITDUMP("Processing use [%06u] of V%02u.[%03u..%03u)\n", Compiler::dspTreeID(lcl), lclNum, offs,
                offs + accessSize);
    }
#endif

    if (accessType == TYP_STRUCT)
    {
        // Will be handled once we get to the parent.
        return;
    }

    size_t index = Promotion::BinarySearch<Replacement, &Replacement::Offset>(replacements, offs);
    if ((ssize_t)index < 0)
    {
        // Access that we don't have a replacement for.
        return;
    }

    Replacement& rep = replacements[index];
    assert(accessType == rep.AccessType);
    JITDUMP("  ..replaced with promoted lcl V%02u\n", rep.LclNum);

    bool isDef = lcl->OperIsLocalStore();
    if (isDef)
    {
        *use = m_compiler->gtNewStoreLclVarNode(rep.LclNum, lcl->Data());
    }
    else
    {
        *use = m_compiler->gtNewLclvNode(rep.LclNum, accessType);
    }

    (*use)->gtFlags |= lcl->gtFlags & GTF_VAR_DEATH;

    if (isDef)
    {
        rep.NeedsWriteBack = true;
        rep.NeedsReadBack  = false;
    }
    else if (rep.NeedsReadBack)
    {
        *use = m_compiler->gtNewOperNode(GT_COMMA, (*use)->TypeGet(),
                                         Promotion::CreateReadBack(m_compiler, lclNum, rep), *use);
        rep.NeedsReadBack = false;

        // TODO-CQ: Local copy prop does not take into account that the
        // uses of LCL_VAR occur at the user, which means it may introduce
        // illegally overlapping lifetimes, such as:
        //
        // └──▌  ADD       int
        //    ├──▌  LCL_VAR   int    V10 tmp6        -> copy propagated to [V35 tmp31]
        //    └──▌  COMMA     int
        //       ├──▌  STORE_LCL_VAR int    V35 tmp31
        //       │  └──▌  LCL_FLD   int    V03 loc1         [+4]
        //
        // This really ought to be handled by local copy prop, but the way it works during
        // morph makes it hard to fix there.
        //
        // This is the short term fix. Long term fixes may be:
        // 1. Fix local copy prop
        // 2. Teach LSRA to allow the above cases, simplifying IR concepts (e.g.
        //    introduce something like GT_COPY on top of LCL_VAR when they
        //    need to be "defs")
        // 3. Change the pass here to avoid creating any embedded assignments by making use
        //    of gtSplitTree. We will only need to split in very edge cases since the point
        //    at which the replacement was marked as needing read back is practically always
        //    going to be in a previous statement, so this shouldn't be too bad for CQ.

        m_compiler->lvaGetDesc(rep.LclNum)->lvRedefinedInEmbeddedStatement = true;
    }

    m_madeChanges = true;
}

//------------------------------------------------------------------------
// StoreBeforeReturn:
//   Handle a return of a potential struct local.
//
// Parameters:
//   ret - The GT_RETURN node
//
void ReplaceVisitor::StoreBeforeReturn(GenTreeUnOp* ret)
{
    if (ret->TypeIs(TYP_VOID) || !ret->gtGetOp1()->OperIs(GT_LCL_VAR, GT_LCL_FLD))
    {
        return;
    }

    GenTreeLclVarCommon* retLcl = ret->gtGetOp1()->AsLclVarCommon();
    if (retLcl->TypeIs(TYP_STRUCT))
    {
        unsigned size = retLcl->GetLayout(m_compiler)->GetSize();
        WriteBackBefore(&ret->gtOp1, retLcl->GetLclNum(), retLcl->GetLclOffs(), size);
    }
}

//------------------------------------------------------------------------
// WriteBackBefore:
//   Update the use with IR that writes back all necessary overlapping
//   replacements into a struct local.
//
// Parameters:
//   use  - The use, which will be updated with a cascading comma trees of assignments
//   lcl  - The struct local
//   offs - The starting offset into the struct local of the overlapping range to write back to
//   size - The size of the overlapping range
//
void ReplaceVisitor::WriteBackBefore(GenTree** use, unsigned lcl, unsigned offs, unsigned size)
{
    if (m_aggregates[lcl] == nullptr)
    {
        return;
    }

    jitstd::vector<Replacement>& replacements = m_aggregates[lcl]->Replacements;
    size_t                       index = Promotion::BinarySearch<Replacement, &Replacement::Offset>(replacements, offs);

    if ((ssize_t)index < 0)
    {
        index = ~index;
        if ((index > 0) && replacements[index - 1].Overlaps(offs, size))
        {
            index--;
        }
    }

    unsigned end = offs + size;
    while ((index < replacements.size()) && (replacements[index].Offset < end))
    {
        Replacement& rep = replacements[index];
        if (rep.NeedsWriteBack)
        {
            GenTreeOp* comma = m_compiler->gtNewOperNode(GT_COMMA, (*use)->TypeGet(),
                                                         Promotion::CreateWriteBack(m_compiler, lcl, rep), *use);
            *use = comma;
            use  = &comma->gtOp2;

            rep.NeedsWriteBack = false;
            m_madeChanges      = true;
        }

        index++;
    }
}

//------------------------------------------------------------------------
// MarkForReadBack:
//   Mark that replacements in the specified struct local need to be read
//   back before their next use.
//
// Parameters:
//   lcl          - The struct local
//   offs         - The starting offset of the range in the struct local that needs to be read back from.
//   size         - The size of the range
//
bool ReplaceVisitor::MarkForReadBack(unsigned lcl, unsigned offs, unsigned size)
{
    if (m_aggregates[lcl] == nullptr)
    {
        return false;
    }

    jitstd::vector<Replacement>& replacements = m_aggregates[lcl]->Replacements;
    size_t                       index = Promotion::BinarySearch<Replacement, &Replacement::Offset>(replacements, offs);

    if ((ssize_t)index < 0)
    {
        index = ~index;
        if ((index > 0) && replacements[index - 1].Overlaps(offs, size))
        {
            index--;
        }
    }

    bool     any = false;
    unsigned end = offs + size;
    while ((index < replacements.size()) && (replacements[index].Offset < end))
    {
        any              = true;
        Replacement& rep = replacements[index];
        assert(rep.Overlaps(offs, size));
        rep.NeedsReadBack     = true;
        rep.NeedsWriteBack    = false;
        m_hasPendingReadBacks = true;
        index++;
    }

    return any;
}

//------------------------------------------------------------------------
// Promotion::Run:
//   Run the promotion phase.
//
// Returns:
//   Suitable phase status.
//
PhaseStatus Promotion::Run()
{
    if (!HaveCandidateLocals())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // First collect information about uses of locals
    LocalsUseVisitor localsUse(this);
    for (BasicBlock* bb : m_compiler->Blocks())
    {
        localsUse.SetBB(bb);

        for (Statement* stmt : bb->Statements())
        {
            for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
            {
                if (Promotion::IsCandidateForPhysicalPromotion(m_compiler->lvaGetDesc(lcl)))
                {
                    localsUse.WalkTree(stmt->GetRootNodePointer(), nullptr);
                    break;
                }
            }
        }
    }

    unsigned numLocals = m_compiler->lvaCount;

#ifdef DEBUG
    if (m_compiler->verbose)
    {
        for (unsigned lcl = 0; lcl < m_compiler->lvaCount; lcl++)
        {
            LocalUses* uses = localsUse.GetUsesByLocal(lcl);
            if (uses != nullptr)
            {
                uses->Dump(lcl);
            }
        }
    }
#endif

    // Pick promotions based on the use information we just collected.
    bool                           anyReplacements = false;
    jitstd::vector<AggregateInfo*> aggregates(m_compiler->lvaCount, nullptr, m_compiler->getAllocator(CMK_Promotion));
    for (unsigned i = 0; i < numLocals; i++)
    {
        LocalUses* uses = localsUse.GetUsesByLocal(i);
        if (uses == nullptr)
        {
            continue;
        }

        uses->PickPromotions(m_compiler, i, &aggregates[i]);

        if (aggregates[i] == nullptr)
        {
            continue;
        }

        jitstd::vector<Replacement>& reps = aggregates[i]->Replacements;

        assert(reps.size() > 0);
        anyReplacements = true;
#ifdef DEBUG
        JITDUMP("V%02u promoted with %d replacements\n", i, (int)reps.size());
        for (const Replacement& rep : reps)
        {
            JITDUMP("  [%03u..%03u) promoted as %s V%02u\n", rep.Offset, rep.Offset + genTypeSize(rep.AccessType),
                    varTypeName(rep.AccessType), rep.LclNum);
        }
#endif

        JITDUMP("Computing unpromoted remainder for V%02u\n", i);
        StructSegments unpromotedParts = SignificantSegments(m_compiler, m_compiler->lvaGetDesc(i)->GetLayout());
        for (size_t i = 0; i < reps.size(); i++)
        {
            unpromotedParts.Subtract(
                StructSegments::Segment(reps[i].Offset, reps[i].Offset + genTypeSize(reps[i].AccessType)));
        }

        JITDUMP("  Remainder: ");
        DBEXEC(m_compiler->verbose, unpromotedParts.Dump());
        JITDUMP("\n\n");

        StructSegments::Segment unpromotedSegment;
        if (unpromotedParts.CoveringSegment(&unpromotedSegment))
        {
            aggregates[i]->UnpromotedMin = unpromotedSegment.Start;
            aggregates[i]->UnpromotedMax = unpromotedSegment.End;
            assert(unpromotedSegment.Start < unpromotedSegment.End);
        }
        else
        {
            // Aggregate is fully promoted, leave UnpromotedMin == UnpromotedMax to indicate this.
        }
    }

    if (!anyReplacements)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // Compute liveness for the fields and remainders.
    PromotionLiveness liveness(m_compiler, aggregates);
    liveness.Run();

    JITDUMP("Making replacements\n\n");
    // Make all replacements we decided on.
    ReplaceVisitor replacer(this, aggregates, &liveness);
    for (BasicBlock* bb : m_compiler->Blocks())
    {
        replacer.StartBlock(bb);

        for (Statement* stmt : bb->Statements())
        {
            DISPSTMT(stmt);
            replacer.StartStatement();
            replacer.WalkTree(stmt->GetRootNodePointer(), nullptr);

            if (replacer.MadeChanges())
            {
                m_compiler->fgSequenceLocals(stmt);
                m_compiler->gtUpdateStmtSideEffects(stmt);
                JITDUMP("New statement:\n");
                DISPSTMT(stmt);
            }
        }

        replacer.EndBlock();
    }

    // Insert initial IR to read arguments/OSR locals into replacement locals,
    // and add necessary explicit zeroing.
    Statement* prevStmt = nullptr;
    for (unsigned lclNum = 0; lclNum < numLocals; lclNum++)
    {
        if (aggregates[lclNum] == nullptr)
        {
            continue;
        }

        LclVarDsc* dsc = m_compiler->lvaGetDesc(lclNum);
        if (dsc->lvIsParam || dsc->lvIsOSRLocal)
        {
            InsertInitialReadBack(lclNum, aggregates[lclNum]->Replacements, &prevStmt);
        }
        else if (dsc->lvSuppressedZeroInit)
        {
            // We may have suppressed inserting an explicit zero init based on the
            // assumption that the entire local will be zero inited in the prolog.
            // Now that we are promoting some fields that assumption may be
            // invalidated for those fields, and we may need to insert explicit
            // zero inits again.
            ExplicitlyZeroInitReplacementLocals(lclNum, aggregates[lclNum]->Replacements, &prevStmt);
        }
    }

    return PhaseStatus::MODIFIED_EVERYTHING;
}

//------------------------------------------------------------------------
// Promotion::HaveCandidateLocals:
//   Check if there are any locals that are candidates for physical promotion.
//
// Returns:
//   True if so.
//
bool Promotion::HaveCandidateLocals()
{
    for (unsigned lclNum = 0; lclNum < m_compiler->lvaCount; lclNum++)
    {
        if (IsCandidateForPhysicalPromotion(m_compiler->lvaGetDesc(lclNum)))
        {
            return true;
        }
    }

    return false;
}

//------------------------------------------------------------------------
// Promotion::IsCandidateForPhysicalPromotion:
//   Check if a specified local is a candidate for physical promotion.
//
// Returns:
//   True if so.
//
bool Promotion::IsCandidateForPhysicalPromotion(LclVarDsc* dsc)
{
    return (dsc->TypeGet() == TYP_STRUCT) && !dsc->lvPromoted && !dsc->IsAddressExposed();
}

//------------------------------------------------------------------------
// Promotion::InsertInitialReadBack:
//   Insert IR to initially read a struct local's value into its promoted field locals.
//
// Parameters:
//   lclNum       - The struct local
//   replacements - Replacements for the struct local
//   prevStmt     - [in, out] Previous statement to insert after
//
void Promotion::InsertInitialReadBack(unsigned                           lclNum,
                                      const jitstd::vector<Replacement>& replacements,
                                      Statement**                        prevStmt)
{
    for (unsigned i = 0; i < replacements.size(); i++)
    {
        const Replacement& rep      = replacements[i];
        GenTree*           readBack = CreateReadBack(m_compiler, lclNum, rep);
        InsertInitStatement(prevStmt, readBack);
    }
}

//------------------------------------------------------------------------
// Promotion::ExplicitlyZeroInitReplacementLocals:
//   Insert IR to zero out replacement locals if necessary.
//
// Parameters:
//   lclNum       - The struct local
//   replacements - Replacements for the struct local
//   prevStmt     - [in, out] Previous statement to insert after
//
void Promotion::ExplicitlyZeroInitReplacementLocals(unsigned                           lclNum,
                                                    const jitstd::vector<Replacement>& replacements,
                                                    Statement**                        prevStmt)
{
    for (unsigned i = 0; i < replacements.size(); i++)
    {
        const Replacement& rep = replacements[i];

        if (!m_compiler->fgVarNeedsExplicitZeroInit(rep.LclNum, false, false))
        {
            // Other downstream code (e.g. recursive-tailcalls-to-loops opt) may
            // still need to insert further explicit zero initing.
            m_compiler->lvaGetDesc(rep.LclNum)->lvSuppressedZeroInit = true;
            continue;
        }

        GenTree* value = m_compiler->gtNewZeroConNode(rep.AccessType);
        GenTree* store = m_compiler->gtNewStoreLclVarNode(rep.LclNum, value);
        InsertInitStatement(prevStmt, store);
    }
}

//------------------------------------------------------------------------
// Promotion::InsertInitStatement:
//   Insert a new statement after the specified statement in the scratch block,
//   or at the beginning of the scratch block if no other statements were
//   inserted yet.
//
// Parameters:
//   prevStmt - [in, out] Previous statement to insert after
//   tree     - Tree to create statement from
//
void Promotion::InsertInitStatement(Statement** prevStmt, GenTree* tree)
{
    m_compiler->fgEnsureFirstBBisScratch();
    Statement* stmt = m_compiler->fgNewStmtFromTree(tree);
    if (*prevStmt != nullptr)
    {
        m_compiler->fgInsertStmtAfter(m_compiler->fgFirstBB, *prevStmt, stmt);
    }
    else
    {
        m_compiler->fgInsertStmtAtBeg(m_compiler->fgFirstBB, stmt);
    }

    *prevStmt = stmt;
}
