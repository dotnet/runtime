#include "jitpch.h"
#include "promotion.h"
#include "jitstd/algorithm.h"

PhaseStatus Compiler::PromoteStructsNew()
{
    if (!opts.OptEnabled(CLFLG_STRUCTPROMOTE))
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

#ifdef DEBUG
    if (JitConfig.JitNewStructPromotion() == 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif

    Promotion prom(this);
    return prom.Run();
}

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
    // Number of times this access is on the RHS of an assignment where the LHS is a probable register candidate.
    unsigned CountAssignmentsToRegisterCandidate = 0;
    // Number of times this access is on the LHS of an assignment where the RHS is a probable register candidate.
    unsigned CountAssignmentsFromRegisterCandidate = 0;
    unsigned CountCallArgs                         = 0;
    unsigned CountCallArgsByImplicitRef            = 0;
    unsigned CountCallArgsOnStack                  = 0;
    unsigned CountReturns                          = 0;
    unsigned CountPassedAsRetbuf                   = 0;

    weight_t CountWtd = 0;
    weight_t CountAssignmentSourceWtd = 0;
    weight_t CountAssignmentDestinationWtd = 0;
    weight_t CountAssignmentsToRegisterCandidateWtd = 0;
    weight_t CountAssignmentsFromRegisterCandidateWtd = 0;
    weight_t CountCallArgsWtd                         = 0;
    weight_t CountCallArgsByImplicitRefWtd            = 0;
    weight_t CountCallArgsOnStackWtd                  = 0;
    weight_t CountReturnsWtd                          = 0;
    weight_t CountPassedAsRetbufWtd                   = 0;

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

// Find first entry with an equal offset, or bitwise complement of first
// entry with a higher offset.
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

struct Replacement
{
    unsigned     Offset;
    var_types    AccessType;
    unsigned     LclNum;
    bool         NeedsWriteBack = true;
    bool         NeedsReadBack  = false;
    Replacement* Next           = nullptr;

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
    None                              = 0,
    IsCallArg                         = 1,
    IsAssignmentSource                = 2,
    IsAssignmentDestination           = 4,
    IsAssignmentToRegisterCandidate   = 8,
    IsAssignmentFromRegisterCandidate = 16,
    IsCallArgByImplicitRef            = 32,
    IsCallArgOnStack                  = 64,
    IsCallRetBuf                      = 128,
    IsReturned                        = 256,
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

class LocalsUses
{
    jitstd::vector<Access> m_accesses;

public:
    LocalsUses(Compiler* comp) : m_accesses(comp->getAllocator(CMK_Promotion))
    {
    }

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
                        access         = &candidateAccess;
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

            if ((flags & AccessKindFlags::IsAssignmentToRegisterCandidate) != AccessKindFlags::None)
            {
                access->CountAssignmentsToRegisterCandidate++;
                access->CountAssignmentsToRegisterCandidateWtd += weight;
            }
        }

        if ((flags & AccessKindFlags::IsAssignmentDestination) != AccessKindFlags::None)
        {
            access->CountAssignmentDestination++;
            access->CountAssignmentDestinationWtd += weight;

            if ((flags & AccessKindFlags::IsAssignmentFromRegisterCandidate) != AccessKindFlags::None)
            {
                access->CountAssignmentsFromRegisterCandidate++;
                access->CountAssignmentsFromRegisterCandidateWtd += weight;
            }
        }

        if ((flags & AccessKindFlags::IsCallArg) != AccessKindFlags::None)
        {
            access->CountCallArgs++;
            access->CountCallArgsWtd += weight;

            if ((flags & AccessKindFlags::IsCallArgByImplicitRef) != AccessKindFlags::None)
            {
                access->CountCallArgsByImplicitRef++;
                access->CountCallArgsByImplicitRefWtd += weight;
            }

            if ((flags & AccessKindFlags::IsCallArgOnStack) != AccessKindFlags::None)
            {
                access->CountCallArgsOnStack++;
                access->CountCallArgsOnStackWtd += weight;
            }
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

    void PickPromotions(Compiler* comp, unsigned lclNum, jitstd::vector<Replacement>& replacements)
    {
        if (m_accesses.size() <= 0)
        {
            return;
        }

        // jitstd::sort(
        //    m_accesses.begin(), m_accesses.end(),
        //    [](const Access& l, const Access& r)
        //    {
        //        if (l.Offset < r.Offset)
        //        {
        //            return true;
        //        }

        //        if (l.Offset > r.Offset)
        //        {
        //            return false;
        //        }

        //        if (l.Count > r.Count)
        //        {
        //            return true;
        //        }

        //        if (l.Count < r.Count)
        //        {
        //            return false;
        //        }

        //        // TODO: Better heuristics, use info about struct uses to decrease benefit of promotions
        //        if (r.AccessType == TYP_STRUCT)
        //        {
        //            if (l.AccessType == TYP_STRUCT)
        //            {
        //                return l.Layout < r.Layout;
        //            }

        //            return true;
        //        }

        //        if (l.AccessType == TYP_STRUCT)
        //        {
        //            return false;
        //        }

        //        assert(l.AccessType != r.AccessType);
        //        return l.AccessType < r.AccessType;
        //    });

        assert(replacements.size() == 0);
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
            unsigned newLcl = comp->lvaGrabTemp(false DEBUGARG(bufp));
            comp->lvaGetDesc(newLcl)->lvType = access.AccessType;
            replacements.push_back(Replacement(access.Offset, access.AccessType, newLcl));
        }
    }

    bool EvaluateReplacement(Compiler* comp, unsigned lclNum, const Access& access)
    {
        unsigned countOverlappedCalls                    = 0;
        unsigned countOverlappedReturns                  = 0;
        unsigned countOverlappedRetbufs                  = 0;
        unsigned countOverlappedAssignmentDestination    = 0;
        unsigned countOverlappedAssignmentSource         = 0;
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

            countOverlappedCalls += otherAccess.CountCallArgs;
            countOverlappedReturns += otherAccess.CountReturns;
            countOverlappedRetbufs += otherAccess.CountPassedAsRetbuf;
            countOverlappedAssignmentDestination += otherAccess.CountAssignmentDestination;
            countOverlappedAssignmentSource += otherAccess.CountAssignmentSource;

            countOverlappedCallsWtd += otherAccess.CountCallArgsWtd;
            countOverlappedReturnsWtd += otherAccess.CountReturnsWtd;
            countOverlappedRetbufsWtd += otherAccess.CountPassedAsRetbufWtd;
            countOverlappedAssignmentDestinationWtd += otherAccess.CountAssignmentDestinationWtd;
            countOverlappedAssignmentSourceWtd += otherAccess.CountAssignmentSourceWtd;
        }

        weight_t costWithout = 0;
        // A normal access without promotion looks like:
        // mov reg, [reg+offs]
        // It may also be contained. Overall we are going to cost each use of
        // an unpromoted local at 6.5 bytes.
        // TODO: We can make much better guesses on what will and won't be contained.

        costWithout += access.CountWtd * 6.5;

        weight_t costWith = 0;

        // For any use we expect to just use the register directly. We will cost this at 3.5 bytes.
        costWith += access.CountWtd * 3.5;

        weight_t   countReadBacksWtd = 0;
        LclVarDsc* lcl               = comp->lvaGetDesc(lclNum);
        // For parameters we need an initial read back
        if (lcl->lvIsParam)
        {
            countReadBacksWtd += comp->fgFirstBB->getBBWeight(comp);
        }

        countReadBacksWtd += countOverlappedRetbufsWtd;
        countReadBacksWtd += countOverlappedAssignmentDestinationWtd;

        // A read back puts the value from stack back to register. We cost it at 5 bytes.
        costWith += countReadBacksWtd * 5;

        // Write backs with TYP_REFs when the base local is an implicit byref
        // involves checked write barriers, so they are very expensive.
        // TODO-CQ: This should be adjusted once we type implicit byrefs as TYP_I_IMPL.
        weight_t writeBackCost = comp->lvaIsImplicitByRefLocal(lclNum) && (access.AccessType == TYP_REF) ? 15 : 5;
        weight_t countWriteBacksWtd =
            countOverlappedCallsWtd + countOverlappedReturnsWtd + countOverlappedAssignmentSourceWtd;
        costWith += countWriteBacksWtd * writeBackCost;

        JITDUMP("Evaluating access %s @ %03u\n", varTypeName(access.AccessType), access.Offset);
        JITDUMP("  Write-back cost: %d\n", writeBackCost);
        JITDUMP("  # write backs: " FMT_WT "\n", countWriteBacksWtd);
        JITDUMP("  # read backs: " FMT_WT "\n", countReadBacksWtd);
        JITDUMP("  Cost with: " FMT_WT "\n", costWith);
        JITDUMP("  Cost without: " FMT_WT "\n", costWithout);

        if (costWith < costWithout)
        {
            JITDUMP("  Promoting replacement\n");
            return true;
        }

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
            printf("    # assigned from:               (%u, " FMT_WT ")\n", access.CountAssignmentSource, access.CountAssignmentSourceWtd);
            printf("    # assigned to:                 (%u, " FMT_WT ")\n", access.CountAssignmentDestination, access.CountAssignmentDestinationWtd);
            printf("    # as call arg:                 (%u, " FMT_WT ")\n", access.CountCallArgs, access.CountCallArgsWtd);
            printf("    # as implicit by-ref call arg: (%u, " FMT_WT ")\n", access.CountCallArgsByImplicitRef, access.CountCallArgsByImplicitRefWtd);
            printf("    # as on-stack call arg:        (%u, " FMT_WT ")\n", access.CountCallArgsOnStack, access.CountCallArgsOnStackWtd);
            printf("    # as retbuf:                   (%u, " FMT_WT ")\n", access.CountPassedAsRetbuf, access.CountPassedAsRetbufWtd);
            printf("    # as returned value:           (%u, " FMT_WT ")\n\n", access.CountReturns, access.CountReturnsWtd);
        }
    }
#endif
};

class LocalsUseVisitor : public GenTreeVisitor<LocalsUseVisitor>
{
    Promotion*  m_prom;
    LocalsUses* m_uses;
    BasicBlock* m_curBB;

public:
    enum
    {
        DoPreOrder = true,
    };

    LocalsUseVisitor(Promotion* prom) : GenTreeVisitor(prom->m_compiler), m_prom(prom)
    {
        m_uses = reinterpret_cast<LocalsUses*>(
            new (prom->m_compiler, CMK_Promotion) char[prom->m_compiler->lvaCount * sizeof(LocalsUses)]);
        for (size_t i = 0; i < prom->m_compiler->lvaCount; i++)
            new (&m_uses[i], jitstd::placement_t()) LocalsUses(prom->m_compiler);
    }

    void SetBB(BasicBlock* bb)
    {
        m_curBB = bb;
    }

    LocalsUses* GetUsesByLocal(unsigned lcl)
    {
        return &m_uses[lcl];
    }

    fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* tree = *use;

        if (tree->OperIsLocal())
        {
            GenTreeLclVarCommon* lcl = tree->AsLclVarCommon();
            LclVarDsc*           dsc = m_compiler->lvaGetDesc(lcl);
            if (!dsc->lvPromoted && (dsc->TypeGet() == TYP_STRUCT) && !dsc->IsAddressExposed())
            {
                if (lcl->OperIsLocalAddr())
                {
                    assert(user->OperIs(GT_CALL) && dsc->IsHiddenBufferStructArg() &&
                           (user->AsCall()->gtArgs.GetRetBufferArg()->GetNode() == lcl));
                    // TODO: We should record that this is used as the address
                    // of a retbuf -- it makes promotion less desirable as we
                    // have to reload fields back from the retbuf.
                }
                else
                {
                    unsigned        offs         = lcl->GetLclOffs();
                    var_types       accessType   = lcl->TypeGet();
                    ClassLayout*    accessLayout = accessType == TYP_STRUCT ? lcl->GetLayout(m_compiler) : nullptr;
                    AccessKindFlags accessFlags  = ClassifyLocalAccess(lcl, user);
                    m_uses[lcl->GetLclNum()].RecordAccess(offs, accessType, accessLayout, accessFlags,
                                                          m_curBB->getBBWeight(m_compiler));
                }
            }
        }

        return fgWalkResult::WALK_CONTINUE;
    }

    AccessKindFlags ClassifyLocalAccess(GenTreeLclVarCommon* lcl, GenTree* user)
    {
        AccessKindFlags flags = AccessKindFlags::None;
        if (user->IsCall())
        {
            GenTreeCall* call = user->AsCall();
            if (call->IsOptimizingRetBufAsLocal() && (m_compiler->gtCallGetDefinedRetBufLclAddr(call) == lcl))
            {
                flags |= AccessKindFlags::IsCallRetBuf;
            }
            else
            {
                unsigned argIndex = 0;
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

#ifdef WINDOWS_AMD64_ABI
                    if ((argSize != 1) && (argSize != 2) && (argSize != 4) && (argSize != 8))
                    {
                        flags |= AccessKindFlags::IsCallArgByImplicitRef;
                    }

                    if (argIndex >= 4)
                    {
                        flags |= AccessKindFlags::IsCallArgOnStack;
                    }
#endif
                    break;
                }
            }
        }

        if (user->OperIs(GT_ASG))
        {
            if (user->gtGetOp1() == lcl)
            {
                flags |= AccessKindFlags::IsAssignmentDestination;

                if (user->gtGetOp2()->OperIs(GT_LCL_VAR))
                {
                    LclVarDsc* dsc = m_compiler->lvaGetDesc(user->gtGetOp2()->AsLclVarCommon());
                    if ((dsc->TypeGet() != TYP_STRUCT) && !dsc->IsAddressExposed())
                    {
                        flags |= AccessKindFlags::IsAssignmentFromRegisterCandidate;
                    }
                }
            }

            if (user->gtGetOp2() == lcl)
            {
                flags |= AccessKindFlags::IsAssignmentSource;

                if (user->gtGetOp1()->OperIs(GT_LCL_VAR))
                {
                    LclVarDsc* dsc = m_compiler->lvaGetDesc(user->gtGetOp1()->AsLclVarCommon());
                    if ((dsc->TypeGet() != TYP_STRUCT) && !dsc->IsAddressExposed())
                    {
                        flags |= AccessKindFlags::IsAssignmentToRegisterCandidate;
                    }
                }
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
    Promotion*                   m_prom;
    jitstd::vector<Replacement>* m_replacements;
    BasicBlock*                  m_bb;
    Statement*                   m_stmt;
    unsigned                     m_seenAsgs;
    unsigned                     m_seenAsgToLcl;
    bool                         m_madeChanges;
    Statement*                   m_lastStmt;

public:
    enum
    {
        DoPostOrder       = true,
        UseExecutionOrder = true,
    };

    ReplaceVisitor(Promotion* prom, jitstd::vector<Replacement>* replacements)
        : GenTreeVisitor(prom->m_compiler), m_prom(prom), m_replacements(replacements)
    {
    }

    bool MadeChanges()
    {
        return m_madeChanges;
    }
    Statement* GetNextStatement()
    {
        return m_lastStmt->GetNextStmt();
    }

    void Reset(BasicBlock* bb, Statement* stmt)
    {
        m_bb          = bb;
        m_stmt        = stmt;
        m_seenAsgs    = 0;
        m_madeChanges = false;
        m_lastStmt    = stmt;
    }

    fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* tree = *use;

        // We handle all cases where we can see struct uses up front.
        if (tree->OperIs(GT_ASG))
        {
            // Assignments can be decomposed directly into accesses of the replacements.
            return DecomposeAssignment(use, user);
        }

        if (tree->OperIs(GT_CALL))
        {
            // Calls need to store replacements back into the struct local for args
            // and need to restore replacements from the result (for
            // retbufs/returns). Lowering handles optimizing out the unnecessary copies.
            return LoadStoreAroundCall(use, user);
        }

        if (tree->OperIs(GT_RETURN))
        {
            // Returns need to store replacements back into the struct local.
            // Lowering will optimize away these copies when possible.
            return StoreBeforeReturn((*use)->AsUnOp());
        }

        if (tree->OperIs(GT_LCL_VAR, GT_LCL_FLD))
        {
            ReplaceLocal(use, user);
            return fgWalkResult::WALK_CONTINUE;
        }

        return fgWalkResult::WALK_CONTINUE;
    }

    fgWalkResult DecomposeAssignment(GenTree** use, GenTree* user)
    {
        GenTreeOp* asg = (*use)->AsOp();

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

        return fgWalkResult::WALK_CONTINUE;
    }

    fgWalkResult LoadStoreAroundCall(GenTree** use, GenTree* user)
    {
        GenTreeCall* call = (*use)->AsCall();

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
            assert(retBufArg->GetNode()->OperIsLocalAddr());
            GenTreeLclVarCommon* retBufLcl = retBufArg->GetNode()->AsLclVarCommon();
            unsigned             size      = m_compiler->typGetObjLayout(call->gtRetClsHnd)->GetSize();

            MarkForReadBack(retBufLcl->GetLclNum(), retBufLcl->GetLclOffs(), size);
        }

        return fgWalkResult::WALK_CONTINUE;
    }

    void ReplaceLocal(GenTree** use, GenTree* user)
    {
        GenTreeLclVarCommon*         lcl          = (*use)->AsLclVarCommon();
        unsigned                     lclNum       = lcl->GetLclNum();
        jitstd::vector<Replacement>& replacements = m_replacements[lclNum];

        if (replacements.size() <= 0)
        {
            return;
        }

        unsigned  offs       = lcl->GetLclOffs();
        var_types accessType = lcl->TypeGet();

#ifdef DEBUG
        if (accessType == TYP_STRUCT)
        {
            assert((user == nullptr) || user->OperIs(GT_ASG, GT_CALL, GT_RETURN));
        }
        else
        {
            ClassLayout* accessLayout = varTypeIsStruct(accessType) ? lcl->GetLayout(m_compiler) : nullptr;
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

        if (accessType != TYP_STRUCT)
        {
            size_t index = BinarySearch<Replacement, &Replacement::Offset>(replacements, offs);
            if ((ssize_t)index >= 0)
            {
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
                    *use = m_compiler->gtNewOperNode(GT_COMMA, (*use)->TypeGet(), m_compiler->gtNewAssignNode(dst, src),
                                                     *use);
                    rep.NeedsReadBack = false;
                }

                m_madeChanges = true;
            }
        }
    }

    fgWalkResult StoreBeforeReturn(GenTreeUnOp* ret)
    {
        if (ret->TypeIs(TYP_VOID) || !ret->gtGetOp1()->OperIs(GT_LCL_VAR, GT_LCL_FLD))
        {
            return fgWalkResult::WALK_CONTINUE;
        }

        GenTreeLclVarCommon* retLcl = ret->gtGetOp1()->AsLclVarCommon();
        if (retLcl->TypeIs(TYP_STRUCT))
        {
            unsigned size = retLcl->GetLayout(m_compiler)->GetSize();
            WriteBackBefore(&ret->gtOp1, retLcl->GetLclNum(), retLcl->GetLclOffs(), size);
        }

        return fgWalkResult::WALK_CONTINUE;
    }

    // Write back all overlapping replacement fields in the specified range.
    void WriteBackBefore(GenTree** use, unsigned lcl, unsigned offs, unsigned size)
    {
        jitstd::vector<Replacement> replacements = m_replacements[lcl];
        size_t                      index        = BinarySearch<Replacement, &Replacement::Offset>(replacements, offs);

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

    void MarkForReadBack(unsigned lcl, unsigned offs, unsigned size, bool conservative = false)
    {
        jitstd::vector<Replacement>& replacements = m_replacements[lcl];
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
            assert((rep.Offset >= offs) && (rep.Offset + genTypeSize(rep.AccessType) <= end));
            rep.NeedsReadBack  = true;
            rep.NeedsWriteBack = false;
            index++;

            if (conservative)
            {
                JITDUMP("*** NYI: Conservatively marking as read-back\n");
                conservative = false;
            }
        }
    }
};

PhaseStatus Promotion::Run()
{
    if (m_compiler->lvaCount <= 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

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
            LocalsUses* uses = localsUse.GetUsesByLocal(lcl);
            uses->Dump(lcl);
        }
    }
#endif

    bool                         anyReplacements = false;
    jitstd::vector<Replacement>* replacements    = reinterpret_cast<jitstd::vector<Replacement>*>(
        new (m_compiler, CMK_Promotion) char[sizeof(jitstd::vector<Replacement>) * m_compiler->lvaCount]);
    for (unsigned i = 0; i < numLocals; i++)
    {
        new (&replacements[i], jitstd::placement_t())
            jitstd::vector<Replacement>(m_compiler->getAllocator(CMK_Promotion));

        localsUse.GetUsesByLocal(i)->PickPromotions(m_compiler, i, replacements[i]);
        if (replacements[i].size() > 0)
        {
            JITDUMP("V%02u promoted with %d replacements\n", i, (int)replacements[i].size());
            for (const Replacement& rep : replacements[i])
            {
                JITDUMP("  [%03u..%03u) promoted as %s V%02u\n", rep.Offset, rep.Offset + genTypeSize(rep.AccessType),
                        varTypeName(rep.AccessType), rep.LclNum);
                anyReplacements = true;
            }
        }
    }

    if (!anyReplacements)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    ReplaceVisitor replacer(this, replacements);
    for (BasicBlock* bb : m_compiler->Blocks())
    {
        for (Statement* stmt = bb->firstStmt(); stmt != nullptr;)
        {
            DISPSTMT(stmt);
            replacer.Reset(bb, stmt);
            replacer.WalkTree(stmt->GetRootNodePointer(), nullptr);

            if (replacer.MadeChanges())
            {
                m_compiler->fgSequenceLocals(stmt);
                JITDUMP("New statement:\n");
                DISPSTMT(stmt);
            }

            stmt = replacer.GetNextStatement();
        }

        for (unsigned i = 0; i < numLocals; i++)
        {
            for (unsigned j = 0; j < replacements[i].size(); j++)
            {
                Replacement& rep = replacements[i][j];
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

    Statement* prevParamLoadStmt = nullptr;
    for (unsigned i = 0; i < m_compiler->info.compArgsCount; i++)
    {
        for (unsigned j = 0; j < replacements[i].size(); j++)
        {
            Replacement& rep = replacements[i][j];

            m_compiler->fgEnsureFirstBBisScratch();

            GenTree*   dst  = m_compiler->gtNewLclvNode(rep.LclNum, rep.AccessType);
            GenTree*   src  = m_compiler->gtNewLclFldNode(i, rep.AccessType, rep.Offset);
            GenTree*   asg  = m_compiler->gtNewAssignNode(dst, src);
            Statement* stmt = m_compiler->fgNewStmtFromTree(asg);
            if (prevParamLoadStmt != nullptr)
            {
                m_compiler->fgInsertStmtAfter(m_compiler->fgFirstBB, prevParamLoadStmt, stmt);
            }
            else
            {
                m_compiler->fgInsertStmtAtBeg(m_compiler->fgFirstBB, stmt);
            }

            prevParamLoadStmt = stmt;
        }
    }

    return PhaseStatus::MODIFIED_EVERYTHING;
}

bool Promotion::ParseLocation(
    GenTree* tree, unsigned* lcl, unsigned* offs, var_types* accessType, ClassLayout** accessLayout)
{
    *offs         = 0;
    *accessType   = TYP_UNDEF;
    *accessLayout = nullptr;

    while (true)
    {
        if (tree->OperIsLocalRead())
        {
            *lcl = tree->AsLclVarCommon()->GetLclNum();
            *offs += tree->AsLclVarCommon()->GetLclOffs();
            if (*accessType == TYP_UNDEF)
            {
                *accessType   = tree->TypeGet();
                *accessLayout = varTypeIsStruct(tree) ? tree->AsLclVarCommon()->GetLayout(m_compiler) : nullptr;
            }

            return true;
        }

        if (tree->OperIs(GT_FIELD) || tree->OperIsIndir())
        {
            if (tree->OperIs(GT_FIELD))
            {
                if (tree->AsField()->GetFldObj() == nullptr)
                {
                    return false;
                }

                if (*accessType == TYP_UNDEF)
                {
                    CORINFO_CLASS_HANDLE fieldClassHandle;
                    var_types fieldType = m_compiler->eeGetFieldType(tree->AsField()->gtFldHnd, &fieldClassHandle);

                    *accessType = fieldType == TYP_STRUCT ? m_compiler->impNormStructType(fieldClassHandle) : fieldType;
                    *accessLayout =
                        varTypeIsStruct(fieldType) ? m_compiler->typGetObjLayout(fieldClassHandle) : nullptr;
                }

                *offs += tree->AsField()->gtFldOffset;
                tree = tree->AsField()->GetFldObj();
            }
            else
            {
                if (*accessType == TYP_UNDEF)
                {
                    *accessType   = tree->TypeGet();
                    *accessLayout = tree->OperIsBlk() ? tree->AsBlk()->GetLayout() : nullptr;
                }

                tree = tree->AsIndir()->Addr();
            }

            switch (tree->gtOper)
            {
                case GT_ADDR:
                    tree = tree->gtGetOp1();
                    continue;
                case GT_LCL_VAR_ADDR:
                    *lcl = tree->AsLclVarCommon()->GetLclNum();
                    return true;
                case GT_LCL_FLD_ADDR:
                    *lcl = tree->AsLclVarCommon()->GetLclNum();
                    *offs += tree->AsLclVarCommon()->GetLclOffs();
                    return true;
                default:
                    return false;
            }
        }
        else
        {
            return false;
        }
    }
}
