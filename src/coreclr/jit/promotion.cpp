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

//------------------------------------------------------------------------
// BinarySearch:
//   Find first entry with an equal offset, or bitwise complement of first
//   entry with a higher offset.
//
// Parameters:
//   vec    - The vector to binary search in
//   offset - The offset to search for
//
// Returns:
//    Index of the first entry with an equal offset, or bitwise complement of
//    first entry with a higher offset.
//
template <typename T, unsigned(T::*field)>
static size_t BinarySearch(const jitstd::vector<T>& vec, unsigned offset)
{
    size_t min = 0;
    size_t max = vec.size();
    while (min < max)
    {
        size_t mid = min + (max - min) / 2;
        if (vec[mid].*field == offset)
        {
            while (mid > 0 && vec[mid - 1].*field == offset)
            {
                mid--;
            }

            return mid;
        }
        if (vec[mid].*field < offset)
        {
            min = mid + 1;
        }
        else
        {
            max = mid;
        }
    }

    return ~min;
}

// Represents a single replacement of a (field) access into a struct local.
struct Replacement
{
    unsigned  Offset;
    var_types AccessType;
    unsigned  LclNum;
    // Is the replacement local (given by LclNum) fresher than the value in the struct local?
    bool NeedsWriteBack = true;
    // Is the value in the struct local fresher than the replacement local?
    bool NeedsReadBack = false;

    Replacement(unsigned offset, var_types accessType, unsigned lclNum)
        : Offset(offset), AccessType(accessType), LclNum(lclNum)
    {
    }

    bool Overlaps(unsigned otherStart, unsigned otherSize) const
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
};

enum class AccessKindFlags : uint32_t
{
    None                    = 0,
    IsCallArg               = 1,
    IsAssignmentSource      = 2,
    IsAssignmentDestination = 4,
    IsCallRetBuf            = 8,
    IsReturned              = 16,
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
            index = BinarySearch<Access, &Access::Offset>(m_accesses, offs);
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
    //   replacements - [out] Pointer to vector to create and insert replacements into
    //
    void PickPromotions(Compiler* comp, unsigned lclNum, jitstd::vector<Replacement>** replacements)
    {
        if (m_accesses.size() <= 0)
        {
            return;
        }

        assert(*replacements == nullptr);
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

            if (*replacements == nullptr)
            {
                *replacements =
                    new (comp, CMK_Promotion) jitstd::vector<Replacement>(comp->getAllocator(CMK_Promotion));
            }

            (*replacements)->push_back(Replacement(access.Offset, access.AccessType, newLcl));
        }
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
        weight_t countOverlappedCallsWtd                 = 0;
        weight_t countOverlappedReturnsWtd               = 0;
        weight_t countOverlappedRetbufsWtd               = 0;
        weight_t countOverlappedAssignmentDestinationWtd = 0;
        weight_t countOverlappedAssignmentSourceWtd      = 0;

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

            countOverlappedCallsWtd += otherAccess.CountCallArgsWtd;
            countOverlappedReturnsWtd += otherAccess.CountReturnsWtd;
            countOverlappedRetbufsWtd += otherAccess.CountPassedAsRetbufWtd;
            countOverlappedAssignmentDestinationWtd += otherAccess.CountAssignmentDestinationWtd;
            countOverlappedAssignmentSourceWtd += otherAccess.CountAssignmentSourceWtd;
        }

        // TODO-CQ: Tune the following heuristics. Currently they are based on
        // x64 code size although using BB weights when available. This mixing
        // does not make sense.
        weight_t costWithout = 0;

        // A normal access without promotion looks like:
        // mov reg, [reg+offs]
        // It may also be contained. Overall we are going to cost each use of
        // an unpromoted local at 6.5 bytes.
        // TODO-CQ: We can make much better guesses on what will and won't be contained.
        costWithout += access.CountWtd * 6.5;

        weight_t costWith = 0;

        // For any use we expect to just use the register directly. We will cost this at 3.5 bytes.
        costWith += access.CountWtd * 3.5;

        weight_t   countReadBacksWtd = 0;
        LclVarDsc* lcl               = comp->lvaGetDesc(lclNum);
        // For parameters or OSR locals we need an initial read back
        if (lcl->lvIsParam || lcl->lvIsOSRLocal)
        {
            countReadBacksWtd += comp->fgFirstBB->getBBWeight(comp);
        }

        countReadBacksWtd += countOverlappedRetbufsWtd;
        countReadBacksWtd += countOverlappedAssignmentDestinationWtd;

        // A read back puts the value from stack back to (hopefully) register. We cost it at 5 bytes.
        costWith += countReadBacksWtd * 5;

        // Write backs with TYP_REFs when the base local is an implicit byref
        // involves checked write barriers, so they are very expensive.
        // TODO-CQ: This should be adjusted once we type implicit byrefs as TYP_I_IMPL.
        weight_t writeBackCost = comp->lvaIsImplicitByRefLocal(lclNum) && (access.AccessType == TYP_REF) ? 15 : 5;
        weight_t countWriteBacksWtd =
            countOverlappedCallsWtd + countOverlappedReturnsWtd + countOverlappedAssignmentSourceWtd;
        costWith += countWriteBacksWtd * writeBackCost;

        JITDUMP("Evaluating access %s @ %03u\n", varTypeName(access.AccessType), access.Offset);
        JITDUMP("  Single write-back cost: " FMT_WT "\n", writeBackCost);
        JITDUMP("  Write backs: " FMT_WT "\n", countWriteBacksWtd);
        JITDUMP("  Read backs: " FMT_WT "\n", countReadBacksWtd);
        JITDUMP("  Cost with: " FMT_WT "\n", costWith);
        JITDUMP("  Cost without: " FMT_WT "\n", costWithout);

        if (costWith < costWithout)
        {
            JITDUMP("  Promoting replacement\n");
            return true;
        }

#ifdef DEBUG
        if (comp->compStressCompile(Compiler::STRESS_PHYSICAL_PROMOTION_COST, 25))
        {
            JITDUMP("  Promoting replacement due to stress\n");
            return true;
        }
#endif

        JITDUMP("  Disqualifying replacement\n");
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
                printf("  [%03u..%03u)\n", access.Offset, access.Offset + access.Layout->GetSize());
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

        if (tree->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_LCL_ADDR))
        {
            GenTreeLclVarCommon* lcl = tree->AsLclVarCommon();
            LclVarDsc*           dsc = m_compiler->lvaGetDesc(lcl);
            if (!dsc->lvPromoted && (dsc->TypeGet() == TYP_STRUCT) && !dsc->IsAddressExposed())
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
                    accessFlags  = ClassifyLocalRead(lcl, user);
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
    //   Given a local use and its user, classify information about it.
    //
    // Parameters:
    //   lcl - The local
    //   user - The user of the local.
    //
    // Returns:
    //   Flags classifying the access.
    //
    AccessKindFlags ClassifyLocalRead(GenTreeLclVarCommon* lcl, GenTree* user)
    {
        assert(lcl->OperIsLocalRead());

        AccessKindFlags flags = AccessKindFlags::None;
        if (user->IsCall())
        {
            GenTreeCall* call     = user->AsCall();
            unsigned     argIndex = 0;
            for (CallArg& arg : call->gtArgs.Args())
            {
                if (arg.GetNode() != lcl)
                {
                    argIndex++;
                    continue;
                }

                flags |= AccessKindFlags::IsCallArg;

                unsigned argSize = 0;
                if (arg.GetSignatureType() != TYP_STRUCT)
                {
                    argSize = genTypeSize(arg.GetSignatureType());
                }
                else
                {
                    argSize = m_compiler->typGetObjLayout(arg.GetSignatureClassHandle())->GetSize();
                }

                break;
            }
        }

        if (user->OperIs(GT_ASG))
        {
            if (user->gtGetOp1() == lcl)
            {
                flags |= AccessKindFlags::IsAssignmentDestination;
            }

            if (user->gtGetOp2() == lcl)
            {
                flags |= AccessKindFlags::IsAssignmentSource;
            }
        }

        if (user->OperIs(GT_RETURN))
        {
            assert(user->gtGetOp1() == lcl);
            flags |= AccessKindFlags::IsReturned;
        }

        return flags;
    }
};

class ReplaceVisitor : public GenTreeVisitor<ReplaceVisitor>
{
    Promotion*                    m_prom;
    jitstd::vector<Replacement>** m_replacements;
    bool                          m_madeChanges = false;

public:
    enum
    {
        DoPostOrder       = true,
        UseExecutionOrder = true,
    };

    ReplaceVisitor(Promotion* prom, jitstd::vector<Replacement>** replacements)
        : GenTreeVisitor(prom->m_compiler), m_prom(prom), m_replacements(replacements)
    {
    }

    bool MadeChanges()
    {
        return m_madeChanges;
    }

    void Reset()
    {
        m_madeChanges = false;
    }

    fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* tree = *use;

        if (tree->OperIs(GT_ASG))
        {
            // If LHS of the ASG was a local then we skipped it as we don't
            // want to see it until after the RHS.
            if (tree->gtGetOp1()->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                ReplaceLocal(&tree->AsOp()->gtOp1, tree);
            }

            // Assignments can be decomposed directly into accesses of the replacements.
            DecomposeAssignment((*use)->AsOp(), user);
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

        // Skip the local on the LHS of ASGs when we see it in the normal tree
        // visit; we handle it as part of the parent ASG instead.
        if (tree->OperIs(GT_LCL_VAR, GT_LCL_FLD) &&
            ((user == nullptr) || !user->OperIs(GT_ASG) || (user->gtGetOp1() != tree)))
        {
            ReplaceLocal(use, user);
            return fgWalkResult::WALK_CONTINUE;
        }

        return fgWalkResult::WALK_CONTINUE;
    }

    //------------------------------------------------------------------------
    // DecomposeAssignment:
    //   Handle an assignment that may be between struct locals with replacements.
    //
    // Parameters:
    //   asg - The assignment
    //   user - The user of the assignment.
    //
    void DecomposeAssignment(GenTreeOp* asg, GenTree* user)
    {
        // TODO-CQ: field-by-field copies and inits.

        if (asg->gtGetOp2()->OperIs(GT_LCL_VAR, GT_LCL_FLD))
        {
            GenTreeLclVarCommon* rhsLcl = asg->gtGetOp2()->AsLclVarCommon();
            if (rhsLcl->TypeIs(TYP_STRUCT))
            {
                unsigned size = rhsLcl->GetLayout(m_compiler)->GetSize();
                WriteBackBefore(&asg->gtOp2, rhsLcl->GetLclNum(), rhsLcl->GetLclOffs(), size);
            }
        }

        if (asg->gtGetOp1()->OperIs(GT_LCL_VAR, GT_LCL_FLD))
        {
            GenTreeLclVarCommon* lhsLcl = asg->gtGetOp1()->AsLclVarCommon();
            if (lhsLcl->TypeIs(TYP_STRUCT))
            {
                unsigned size = lhsLcl->GetLayout(m_compiler)->GetSize();
                MarkForReadBack(lhsLcl->GetLclNum(), lhsLcl->GetLclOffs(), size, true);
            }
        }
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
    void LoadStoreAroundCall(GenTreeCall* call, GenTree* user)
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
            }
        }

        if (call->IsOptimizingRetBufAsLocal())
        {
            assert(retBufArg != nullptr);
            assert(retBufArg->GetNode()->OperIs(GT_LCL_ADDR));
            GenTreeLclVarCommon* retBufLcl = retBufArg->GetNode()->AsLclVarCommon();
            unsigned             size      = m_compiler->typGetObjLayout(call->gtRetClsHnd)->GetSize();

            MarkForReadBack(retBufLcl->GetLclNum(), retBufLcl->GetLclOffs(), size);
        }
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
    void ReplaceLocal(GenTree** use, GenTree* user)
    {
        GenTreeLclVarCommon* lcl    = (*use)->AsLclVarCommon();
        unsigned             lclNum = lcl->GetLclNum();
        if (m_replacements[lclNum] == nullptr)
        {
            return;
        }

        jitstd::vector<Replacement>& replacements = *m_replacements[lclNum];

        unsigned  offs       = lcl->GetLclOffs();
        var_types accessType = lcl->TypeGet();

#ifdef DEBUG
        if (accessType == TYP_STRUCT)
        {
            assert((user == nullptr) || user->OperIs(GT_ASG, GT_CALL, GT_RETURN));
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

        size_t index = BinarySearch<Replacement, &Replacement::Offset>(replacements, offs);
        if ((ssize_t)index < 0)
        {
            // Access that we don't have a replacement for.
            return;
        }

        Replacement& rep = replacements[index];
        assert(accessType == rep.AccessType);
        JITDUMP("  ..replaced with promoted lcl V%02u\n", rep.LclNum);
        *use = m_compiler->gtNewLclvNode(rep.LclNum, accessType);

        if ((lcl->gtFlags & GTF_VAR_DEF) != 0)
        {
            rep.NeedsWriteBack = true;
            rep.NeedsReadBack  = false;
        }
        else if (rep.NeedsReadBack)
        {
            GenTree* dst = m_compiler->gtNewLclvNode(rep.LclNum, rep.AccessType);
            GenTree* src = m_compiler->gtNewLclFldNode(lclNum, rep.AccessType, rep.Offset);
            *use = m_compiler->gtNewOperNode(GT_COMMA, (*use)->TypeGet(), m_compiler->gtNewAssignNode(dst, src), *use);
            rep.NeedsReadBack = false;

            // TODO-CQ: Local copy prop does not take into account that the
            // uses of LCL_VAR occur at the user, which means it may introduce
            // illegally overlapping lifetimes, such as:
            //
            // └──▌  ADD       int
            //    ├──▌  LCL_VAR   int    V10 tmp6        -> copy propagated to [V35 tmp31]
            //    └──▌  COMMA     int
            //       ├──▌  ASG       int
            //       │  ├──▌  LCL_VAR   int    V35 tmp31
            //       │  └──▌  LCL_FLD   int    V03 loc1         [+4]
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
    void StoreBeforeReturn(GenTreeUnOp* ret)
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
    void WriteBackBefore(GenTree** use, unsigned lcl, unsigned offs, unsigned size)
    {
        if (m_replacements[lcl] == nullptr)
        {
            return;
        }

        jitstd::vector<Replacement>& replacements = *m_replacements[lcl];
        size_t                       index        = BinarySearch<Replacement, &Replacement::Offset>(replacements, offs);

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
                GenTree*   dst = m_compiler->gtNewLclFldNode(lcl, rep.AccessType, rep.Offset);
                GenTree*   src = m_compiler->gtNewLclvNode(rep.LclNum, rep.AccessType);
                GenTreeOp* comma =
                    m_compiler->gtNewOperNode(GT_COMMA, (*use)->TypeGet(), m_compiler->gtNewAssignNode(dst, src), *use);
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
    //   conservative - Whether this is a potentially conservative read back
    //                  that we can handle more efficiently in the future (only used for
    //                  logging purposes)
    //
    void MarkForReadBack(unsigned lcl, unsigned offs, unsigned size, bool conservative = false)
    {
        if (m_replacements[lcl] == nullptr)
        {
            return;
        }

        jitstd::vector<Replacement>& replacements = *m_replacements[lcl];
        size_t                       index        = BinarySearch<Replacement, &Replacement::Offset>(replacements, offs);

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
            assert(rep.Overlaps(offs, size));
            rep.NeedsReadBack  = true;
            rep.NeedsWriteBack = false;
            index++;

            if (conservative)
            {
                JITDUMP("*** NYI: Conservatively marked as read-back\n");
                conservative = false;
            }
        }
    }
};

//------------------------------------------------------------------------
// Promotion::Run:
//   Run the promotion phase.
//
// Returns:
//   Suitable phase status.
//
PhaseStatus Promotion::Run()
{
    if (m_compiler->lvaCount <= 0)
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
            localsUse.WalkTree(stmt->GetRootNodePointer(), nullptr);
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

    // Pick promotion based on the use information we just collected.
    bool                          anyReplacements = false;
    jitstd::vector<Replacement>** replacements =
        new (m_compiler, CMK_Promotion) jitstd::vector<Replacement>*[m_compiler->lvaCount]{};
    for (unsigned i = 0; i < numLocals; i++)
    {
        LocalUses* uses = localsUse.GetUsesByLocal(i);
        if (uses == nullptr)
        {
            continue;
        }

        uses->PickPromotions(m_compiler, i, &replacements[i]);

        if (replacements[i] != nullptr)
        {
            assert(replacements[i]->size() > 0);
            anyReplacements = true;
#ifdef DEBUG
            JITDUMP("V%02u promoted with %d replacements\n", i, (int)replacements[i]->size());
            for (const Replacement& rep : *replacements[i])
            {
                JITDUMP("  [%03u..%03u) promoted as %s V%02u\n", rep.Offset, rep.Offset + genTypeSize(rep.AccessType),
                        varTypeName(rep.AccessType), rep.LclNum);
            }
#endif
        }
    }

    if (!anyReplacements)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // Make all replacements we decided on.
    ReplaceVisitor replacer(this, replacements);
    for (BasicBlock* bb : m_compiler->Blocks())
    {
        for (Statement* stmt : bb->Statements())
        {
            DISPSTMT(stmt);
            replacer.Reset();
            replacer.WalkTree(stmt->GetRootNodePointer(), nullptr);

            if (replacer.MadeChanges())
            {
                m_compiler->fgSequenceLocals(stmt);
                m_compiler->gtUpdateStmtSideEffects(stmt);
                JITDUMP("New statement:\n");
                DISPSTMT(stmt);
            }
        }

        for (unsigned i = 0; i < numLocals; i++)
        {
            if (replacements[i] == nullptr)
            {
                continue;
            }

            for (Replacement& rep : *replacements[i])
            {
                assert(!rep.NeedsReadBack || !rep.NeedsWriteBack);
                if (rep.NeedsReadBack)
                {
                    JITDUMP("Reading back replacement V%02u.[%03u..%03u) -> V%02u at the end of " FMT_BB "\n", i,
                            rep.Offset, rep.Offset + genTypeSize(rep.AccessType), rep.LclNum, bb->bbNum);

                    GenTree* dst = m_compiler->gtNewLclvNode(rep.LclNum, rep.AccessType);
                    GenTree* src = m_compiler->gtNewLclFldNode(i, rep.AccessType, rep.Offset);
                    GenTree* asg = m_compiler->gtNewAssignNode(dst, src);
                    m_compiler->fgInsertStmtNearEnd(bb, m_compiler->fgNewStmtFromTree(asg));
                    rep.NeedsReadBack = false;
                }

                rep.NeedsWriteBack = true;
            }
        }
    }

    // Insert initial IR to read arguments/OSR locals into replacement locals,
    // and add necessary explicit zeroing.
    Statement* prevStmt = nullptr;
    for (unsigned lclNum = 0; lclNum < numLocals; lclNum++)
    {
        if (replacements[lclNum] == nullptr)
        {
            continue;
        }

        LclVarDsc* dsc = m_compiler->lvaGetDesc(lclNum);
        if (dsc->lvIsParam || dsc->lvIsOSRLocal)
        {
            InsertInitialReadBack(lclNum, *replacements[lclNum], &prevStmt);
        }
        else if (dsc->lvSuppressedZeroInit)
        {
            // We may have suppressed inserting an explicit zero init based on the
            // assumption that the entire local will be zero inited in the prolog.
            // Now that we are promoting some fields that assumption may be
            // invalidated for those fields, and we may need to insert explicit
            // zero inits again.
            ExplicitlyZeroInitReplacementLocals(lclNum, *replacements[lclNum], &prevStmt);
        }
    }

    return PhaseStatus::MODIFIED_EVERYTHING;
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
        const Replacement& rep = replacements[i];

        GenTree* dst = m_compiler->gtNewLclvNode(rep.LclNum, rep.AccessType);
        GenTree* src = m_compiler->gtNewLclFldNode(lclNum, rep.AccessType, rep.Offset);
        GenTree* asg = m_compiler->gtNewAssignNode(dst, src);
        InsertInitStatement(prevStmt, asg);
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

        GenTree* dst = m_compiler->gtNewLclvNode(rep.LclNum, rep.AccessType);
        GenTree* src = m_compiler->gtNewZeroConNode(rep.AccessType);
        GenTree* asg = m_compiler->gtNewAssignNode(dst, src);
        InsertInitStatement(prevStmt, asg);
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
