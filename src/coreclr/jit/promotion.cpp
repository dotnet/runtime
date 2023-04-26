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
    // Note that the invariant is that this is always false at the entrance to
    // a basic block, i.e. all predecessors would have read the replacement
    // back before transferring control if necessary.
    bool NeedsReadBack = false;
#ifdef DEBUG
    const char* Name;
#endif

    Replacement(unsigned offset, var_types accessType, unsigned lclNum DEBUGARG(const char* name))
        : Offset(offset)
        , AccessType(accessType)
        , LclNum(lclNum)
#ifdef DEBUG
        , Name(name)
#endif
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

            (*replacements)->push_back(Replacement(access.Offset, access.AccessType, newLcl DEBUGARG(bufp)));
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

class DecompositionStatementList
{
    GenTree* m_head = nullptr;

public:
    void AddStatement(GenTree* stmt)
    {
        stmt->gtNext = m_head;
        m_head       = stmt;
    }

    GenTree* ToCommaTree(Compiler* comp)
    {
        if (m_head == nullptr)
        {
            return comp->gtNewNothingNode();
        }

        GenTree* tree = m_head;

        for (GenTree* cur = m_head->gtNext; cur != nullptr; cur = cur->gtNext)
        {
            tree = comp->gtNewOperNode(GT_COMMA, TYP_VOID, cur, tree);
        }

        return tree;
    }
};

class DecompositionPlan
{
    struct Entry
    {
        unsigned  ToLclNum;
        unsigned  FromLclNum;
        unsigned  Offset;
        var_types Type;
    };

    Compiler*         m_compiler;
    ArrayStack<Entry> m_entries;
    GenTree*          m_dst;
    GenTree*          m_src;

public:
    DecompositionPlan(Compiler* comp, GenTree* dst, GenTree* src)
        : m_compiler(comp), m_entries(comp->getAllocator(CMK_Promotion)), m_dst(dst), m_src(src)
    {
    }

    //------------------------------------------------------------------------
    // CopyBetweenReplacements:
    //   Add an entry specifying to copy from a local into another local.
    //
    // Parameters:
    //   dstLcl - The destination local to write.
    //   srcLcl - The source local.
    //   offset - The offset this covers in the struct copy.
    //   type   - The type of copy.
    //
    // Remarks:
    //   This may be used for cases where the destination or source is a regularly promoted field.
    //
    void CopyBetweenReplacements(unsigned dstLcl, unsigned srcLcl, unsigned offset, var_types type)
    {
        m_entries.Push(Entry{dstLcl, srcLcl, offset, type});
    }

    //------------------------------------------------------------------------
    // CopyToReplacement:
    //   Add an entry specifying to copy from the source into a replacement local.
    //
    // Parameters:
    //   dstLcl - The destination local to write.
    //   offset - The relative offset into the source.
    //   type   - The type of copy.
    //
    void CopyToReplacement(unsigned dstLcl, unsigned offset, var_types type)
    {
        m_entries.Push(Entry{dstLcl, BAD_VAR_NUM, offset, type});
    }

    //------------------------------------------------------------------------
    // CopyFromReplacement:
    //   Add an entry specifying to copy from a replacement local into the destination.
    //
    // Parameters:
    //   srcLcl - The source local to copy from.
    //   offset - The relative offset into the destination to write.
    //   type   - The type of copy.
    //
    void CopyFromReplacement(unsigned srcLcl, unsigned offset, var_types type)
    {
        m_entries.Push(Entry{BAD_VAR_NUM, srcLcl, offset, type});
    }

    //------------------------------------------------------------------------
    // InitReplacement:
    //   Add an entry specifying that a specified replacement local should be
    //   constant initialized.
    //
    // Parameters:
    //   dstLcl - The destination local.
    //   offset - The offset covered by this initialization.
    //   type   - The type to initialize.
    //
    void InitReplacement(unsigned dstLcl, unsigned offset, var_types type)
    {
        m_entries.Push(Entry{dstLcl, BAD_VAR_NUM, offset, type});
    }

    //------------------------------------------------------------------------
    // Finalize:
    //   Create IR to perform the full decomposed struct copy as specified by
    //   the entries that were added to the decomposition plan. Add the
    //   statements to the specified list.
    //
    // Parameters:
    //   statements - The list of statements to add to.
    //
    void Finalize(DecompositionStatementList* statements)
    {
        if (m_src->IsConstInitVal())
        {
            FinalizeInit(statements);
        }
        else
        {
            FinalizeCopy(statements);
        }
    }

private:
    //------------------------------------------------------------------------
    // CoversDestination:
    //   Check if the destination is fully defined by the entries that have
    //   been added to the current plan.
    //
    // Returns:
    //   True if so.
    //
    // Remarks:
    //   When the plan fully covers the destination the remaining struct
    //   copy/initialization can be omitted.
    //
    bool CoversDestination()
    {
        unsigned prevEnd    = 0;
        unsigned dstLclOffs = 0;
        if (m_dst->OperIs(GT_LCL_VAR, GT_LCL_FLD))
        {
            dstLclOffs = m_dst->AsLclVarCommon()->GetLclOffs();
        }

        for (int i = 0; i < m_entries.Height(); i++)
        {
            const Entry& entry = m_entries.BottomRef(i);

            unsigned writeBegin = entry.Offset;
            unsigned writeSize  = genTypeSize(entry.Type);

            assert(writeBegin >= prevEnd);

            if (writeBegin > prevEnd)
            {
                // Uncovered hole from [prevEnd..writeBegin).
                // TODO-CQ: In many cases it's more efficient to "plug" the holes. However,
                // it is made more complicated by the fact that the holes can contain GC pointers in them and
                // we cannot (yet) represent custom class layouts with GC pointers in them.
                // TODO-CQ: Many of these cases are just padding. We should handle structs with insignificant
                // padding here.
                return false;
            }

            prevEnd = writeBegin + writeSize;
        }

        return prevEnd == m_dst->GetLayout(m_compiler)->GetSize();
    }

    //------------------------------------------------------------------------
    // FinalizeInit:
    //   Create IR to perform the decomposed initialization.
    //
    // Parameters:
    //   statements - List to add statements to.
    //
    void FinalizeInit(DecompositionStatementList* statements)
    {
        GenTree* cns         = m_src->OperIsInitVal() ? m_src->gtGetOp1() : m_src;
        int64_t  initPattern = int64_t(cns->AsIntCon()->IconValue() & 0xFF) * 0x0101010101010101LL;

        for (int i = 0; i < m_entries.Height(); i++)
        {
            const Entry& entry = m_entries.BottomRef(i);
            GenTree*     srcVal;
            switch (entry.Type)
            {
                case TYP_BOOL:
                case TYP_BYTE:
                case TYP_UBYTE:
                case TYP_SHORT:
                case TYP_USHORT:
                case TYP_INT:
                {
                    int64_t mask = (int64_t(1) << (genTypeSize(entry.Type) * 8)) - 1;
                    srcVal       = m_compiler->gtNewIconNode(static_cast<int32_t>(initPattern & mask));
                    break;
                }
                case TYP_LONG:
                    srcVal = m_compiler->gtNewLconNode(initPattern);
                    break;
                case TYP_FLOAT:
                    float floatPattern;
                    memcpy(&floatPattern, &initPattern, sizeof(floatPattern));
                    srcVal = m_compiler->gtNewDconNode(floatPattern, TYP_FLOAT);
                    break;
                case TYP_DOUBLE:
                    double doublePattern;
                    memcpy(&doublePattern, &initPattern, sizeof(doublePattern));
                    srcVal = m_compiler->gtNewDconNode(doublePattern);
                    break;
                case TYP_REF:
                case TYP_BYREF:
#ifdef FEATURE_SIMD
                case TYP_SIMD8:
                case TYP_SIMD12:
                case TYP_SIMD16:
#if defined(TARGET_XARCH)
                case TYP_SIMD32:
                case TYP_SIMD64:
#endif // TARGET_XARCH
#endif // FEATURE_SIMD
                {
                    assert(initPattern == 0);
                    srcVal = m_compiler->gtNewZeroConNode(entry.Type);
                    break;
                }
                default:
                    unreached();
            }

            assert(entry.ToLclNum != BAD_VAR_NUM);
            GenTree* dst = m_compiler->gtNewLclvNode(entry.ToLclNum, entry.Type);
            statements->AddStatement(m_compiler->gtNewAssignNode(dst, srcVal));
        }

        if (!CoversDestination())
        {
            GenTree* asg = m_compiler->gtNewBlkOpNode(m_dst, cns);
            statements->AddStatement(asg);
        }
    }

    //------------------------------------------------------------------------
    // FinalizeCopy:
    //   Create IR to perform the decomposed copy.
    //
    // Parameters:
    //   statements - List to add statements to.
    //
    void FinalizeCopy(DecompositionStatementList* statements)
    {
        assert(m_dst->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK, GT_FIELD) &&
               m_src->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK, GT_FIELD));
        bool coversDestination = CoversDestination();

        GenTree*     addr         = nullptr;
        unsigned     addrBaseOffs = 0;
        GenTreeFlags indirFlags   = GTF_EMPTY;

        if (m_dst->OperIs(GT_BLK, GT_FIELD))
        {
            addr = m_dst->gtGetOp1();

            if (m_dst->OperIs(GT_FIELD))
            {
                addrBaseOffs = m_dst->AsField()->gtFldOffset;
            }

            indirFlags = GetPropagatedIndirFlags(m_dst);
        }
        else if (m_src->OperIs(GT_BLK, GT_FIELD))
        {
            addr = m_src->gtGetOp1();

            if (m_src->OperIs(GT_FIELD))
            {
                addrBaseOffs = m_src->AsField()->gtFldOffset;
            }

            indirFlags = GetPropagatedIndirFlags(m_src);
        }

        int numAddrUses = addr == nullptr ? 0 : (m_entries.Height() + (coversDestination ? 0 : 1));

        // If the destination is fully covered we may need a null check for the GT_FIELD case.
        // If the destination is not covered then the initial struct copy is enough.
        bool needsNullCheck = coversDestination && (addr != nullptr) && m_compiler->fgAddrCouldBeNull(addr);

        if (needsNullCheck)
        {
            // See if our first indirection will subsume the null check (usual case).
            assert(m_entries.Height() > 0);
            const Entry& entry = m_entries.BottomRef(0);

            assert((entry.FromLclNum == BAD_VAR_NUM) || (entry.ToLclNum == BAD_VAR_NUM));
            needsNullCheck = m_compiler->fgIsBigOffset(addrBaseOffs + entry.Offset);
        }

        if (needsNullCheck)
        {
            numAddrUses++;
        }

        if ((addr != nullptr) && (numAddrUses > 1))
        {
            if (addr->OperIsLocal() && (!m_dst->OperIs(GT_LCL_VAR, GT_LCL_FLD) ||
                                        (addr->AsLclVarCommon()->GetLclNum() != m_dst->AsLclVarCommon()->GetLclNum())))
            {
                // We will introduce more uses of the address local, so it is
                // no longer dying here.
                addr->gtFlags &= ~GTF_VAR_DEATH;
            }
            else if (addr->IsInvariant())
            {
                // Fall through
            }
            else
            {
                unsigned addrLcl = m_compiler->lvaGrabTemp(true DEBUGARG("Spilling address for field-by-field copy"));
                statements->AddStatement(m_compiler->gtNewTempAssign(addrLcl, addr));
                addr = m_compiler->gtNewLclvNode(addrLcl, addr->TypeGet());
                UpdateEarlyRefCount(m_compiler, addr);
            }
        }

        auto grabAddr = [&numAddrUses, addr, this](unsigned offs) {
            assert(numAddrUses > 0);
            numAddrUses--;

            GenTree* addrUse;
            if (numAddrUses == 0)
            {
                // Last use of the address, reuse the node.
                addrUse = addr;
            }
            else
            {
                addrUse = m_compiler->gtCloneExpr(addr);
                UpdateEarlyRefCount(m_compiler, addrUse);
            }

            if (offs != 0)
            {
                var_types addrType = varTypeIsGC(addrUse) ? TYP_BYREF : TYP_I_IMPL;
                addrUse            = m_compiler->gtNewOperNode(GT_ADD, addrType, addrUse,
                                                    m_compiler->gtNewIconNode((ssize_t)offs, TYP_I_IMPL));
            }

            return addrUse;
        };

        if (!coversDestination)
        {
            // Note that this does not handle partially overlapping copies,
            // but that is left undefined (and normal block copies do not
            // handle this either).
            if (m_src->OperIs(GT_BLK, GT_FIELD))
            {
                // Note that we should use 0 instead of addrBaseOffs here
                // since this ends up as the address of the GT_FIELD node
                // that already has the field offset.
                m_src->AsUnOp()->gtOp1 = grabAddr(0);
            }
            else if (m_dst->OperIs(GT_BLK, GT_FIELD))
            {
                // Like above, use 0 intentionally here.
                m_dst->AsUnOp()->gtOp1 = grabAddr(0);
            }

            statements->AddStatement(m_compiler->gtNewBlkOpNode(m_dst, m_src));

            if (m_src->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                // We will introduce uses of the source below so this struct
                // copy is no longer the last use if it was before.
                m_src->gtFlags &= ~GTF_VAR_DEATH;
            }
        }

        if (needsNullCheck)
        {
            GenTreeIndir* indir = m_compiler->gtNewIndir(TYP_BYTE, grabAddr(addrBaseOffs));
            PropagateIndirFlags(indir, indirFlags);
            statements->AddStatement(indir);
        }

        for (int i = 0; i < m_entries.Height(); i++)
        {
            const Entry& entry = m_entries.BottomRef(i);

            GenTree* dst;
            if (entry.ToLclNum != BAD_VAR_NUM)
            {
                dst = m_compiler->gtNewLclvNode(entry.ToLclNum, entry.Type);

                if (m_compiler->lvaGetDesc(entry.ToLclNum)->lvIsStructField)
                    UpdateEarlyRefCount(m_compiler, dst);
            }
            else
            {
                assert(entry.FromLclNum != BAD_VAR_NUM);

                if (m_dst->OperIs(GT_LCL_VAR, GT_LCL_FLD))
                {
                    unsigned offs = m_dst->AsLclVarCommon()->GetLclOffs() + entry.Offset;
                    // Local morph ensures we do not see local indirs here that dereference beyond UINT16_MAX.
                    noway_assert(FitsIn<uint16_t>(offs));
                    dst = m_compiler->gtNewLclFldNode(m_dst->AsLclVarCommon()->GetLclNum(), entry.Type, offs);
                    m_compiler->lvaSetVarDoNotEnregister(m_dst->AsLclVarCommon()->GetLclNum()
                                                             DEBUGARG(DoNotEnregisterReason::LocalField));
                    UpdateEarlyRefCount(m_compiler, dst);
                }
                else
                {
                    GenTree* addr = grabAddr(addrBaseOffs + entry.Offset);
                    dst           = m_compiler->gtNewIndir(entry.Type, addr);
                    PropagateIndirFlags(dst, indirFlags);
                }
            }

            GenTree* src;
            if (entry.FromLclNum != BAD_VAR_NUM)
            {
                src = m_compiler->gtNewLclvNode(entry.FromLclNum, entry.Type);

                if (m_compiler->lvaGetDesc(entry.FromLclNum)->lvIsStructField)
                    UpdateEarlyRefCount(m_compiler, src);
            }
            else
            {
                assert(entry.ToLclNum != BAD_VAR_NUM);
                if (m_src->OperIs(GT_LCL_VAR, GT_LCL_FLD))
                {
                    unsigned offs = m_src->AsLclVarCommon()->GetLclOffs() + entry.Offset;
                    noway_assert(FitsIn<uint16_t>(offs));
                    src = m_compiler->gtNewLclFldNode(m_src->AsLclVarCommon()->GetLclNum(), entry.Type, offs);
                    m_compiler->lvaSetVarDoNotEnregister(m_src->AsLclVarCommon()->GetLclNum()
                                                             DEBUGARG(DoNotEnregisterReason::LocalField));
                    UpdateEarlyRefCount(m_compiler, src);
                }
                else
                {
                    GenTree* addr = grabAddr(addrBaseOffs + entry.Offset);
                    src           = m_compiler->gtNewIndir(entry.Type, addr);
                    PropagateIndirFlags(src, indirFlags);
                }
            }

            statements->AddStatement(m_compiler->gtNewAssignNode(dst, src));
        }

        assert(numAddrUses == 0);
    }

    GenTreeFlags GetPropagatedIndirFlags(GenTree* indir)
    {
        assert(indir->OperIs(GT_BLK, GT_FIELD));
        if (indir->OperIs(GT_BLK))
        {
            return indir->gtFlags & (GTF_IND_VOLATILE | GTF_IND_NONFAULTING | GTF_IND_UNALIGNED | GTF_IND_INITCLASS);
        }

        static_assert_no_msg(GTF_FLD_VOLATILE == GTF_IND_VOLATILE);
        return indir->gtFlags & GTF_IND_VOLATILE;
    }

    void PropagateIndirFlags(GenTree* indir, GenTreeFlags flags)
    {
        if (genTypeSize(indir) == 1)
        {
            flags &= ~GTF_IND_UNALIGNED;
        }

        indir->gtFlags |= flags;
    }

    //------------------------------------------------------------------------
    // UpdateEarlyRefCount:
    //   Update early ref counts if necessary for the specified IR node.
    //
    // Parameters:
    //   comp      - compiler instance
    //   candidate - the IR node that may be a local that should have its early
    //               ref counts updated.
    //
    static void UpdateEarlyRefCount(Compiler* comp, GenTree* candidate)
    {
        if (!candidate->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_LCL_ADDR))
        {
            return;
        }

        IncrementRefCount(comp, candidate->AsLclVarCommon()->GetLclNum());

        LclVarDsc* varDsc = comp->lvaGetDesc(candidate->AsLclVarCommon());
        if (varDsc->lvIsStructField)
        {
            IncrementRefCount(comp, varDsc->lvParentLcl);
        }

        if (varDsc->lvPromoted)
        {
            for (unsigned fldLclNum = varDsc->lvFieldLclStart; fldLclNum < varDsc->lvFieldLclStart + varDsc->lvFieldCnt;
                 fldLclNum++)
            {
                IncrementRefCount(comp, fldLclNum);
            }
        }
    }

    //------------------------------------------------------------------------
    // IncrementRefCount:
    //   Increment the ref count for the specified local.
    //
    // Parameters:
    //   comp   - compiler instance
    //   lclNum - the local
    //
    static void IncrementRefCount(Compiler* comp, unsigned lclNum)
    {
        LclVarDsc* varDsc = comp->lvaGetDesc(lclNum);
        varDsc->incLvRefCntSaturating(1, RCS_EARLY);
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
            DecomposeAssignment(use, user);
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
    void DecomposeAssignment(GenTree** use, GenTree* user)
    {
        GenTreeOp* asg = (*use)->AsOp();

        if (!asg->gtGetOp1()->TypeIs(TYP_STRUCT))
        {
            return;
        }

        GenTree* dst = asg->gtGetOp1();
        assert(!dst->OperIs(GT_COMMA));
        GenTree* src = asg->gtGetOp2()->gtEffectiveVal();

        GenTreeLclVarCommon* dstLcl = dst->OperIs(GT_LCL_VAR, GT_LCL_FLD) ? dst->AsLclVarCommon() : nullptr;
        GenTreeLclVarCommon* srcLcl = src->OperIs(GT_LCL_VAR, GT_LCL_FLD) ? src->AsLclVarCommon() : nullptr;

        Replacement* dstFirstRep     = nullptr;
        Replacement* dstEndRep       = nullptr;
        bool dstInvolvesReplacements = (dstLcl != nullptr) && OverlappingReplacements(dstLcl, &dstFirstRep, &dstEndRep);
        Replacement* srcFirstRep     = nullptr;
        Replacement* srcEndRep       = nullptr;
        bool srcInvolvesReplacements = (srcLcl != nullptr) && OverlappingReplacements(srcLcl, &srcFirstRep, &srcEndRep);

        if (!dstInvolvesReplacements && !srcInvolvesReplacements)
        {
            return;
        }

        JITDUMP("Processing block operation [%06u] that involves replacements\n", Compiler::dspTreeID(asg));

        if (src->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK, GT_FIELD) || src->IsConstInitVal())
        {
            DecompositionStatementList result;
            EliminateCommasInBlockOp(asg, &result);

            if (dstInvolvesReplacements)
            {
                unsigned dstLclOffs = dstLcl->GetLclOffs();
                unsigned dstLclSize = dstLcl->GetLayout(m_compiler)->GetSize();
                if (dstFirstRep->Offset < dstLclOffs)
                {
                    if (dstFirstRep->NeedsWriteBack)
                    {
                        JITDUMP("*** Block operation partially overlaps with destination %s. Write and read-backs are "
                                "necessary.\n",
                                dstFirstRep->Name);
                        // The value of the replacement will be partially assembled from its old value and this struct
                        // operation.
                        // We accomplish this by an initial write back, the struct copy, followed by a later read back.
                        // TODO-CQ: This is very expensive and unreflected in heuristics, but it is also very rare.
                        result.AddStatement(CreateWriteBack(dstLcl->GetLclNum(), *dstFirstRep));

                        dstFirstRep->NeedsWriteBack = false;
                    }

                    dstFirstRep->NeedsReadBack = true;
                    dstFirstRep++;
                }

                if (dstEndRep > dstFirstRep)
                {
                    Replacement* dstLastRep = dstEndRep - 1;
                    if (dstLastRep->Offset + genTypeSize(dstLastRep->AccessType) > dstLclOffs + dstLclSize)
                    {
                        if (dstLastRep->NeedsWriteBack)
                        {
                            JITDUMP(
                                "*** Block operation partially overlaps with destination %s. Write and read-backs are "
                                "necessary.\n",
                                dstLastRep->Name);
                            result.AddStatement(CreateWriteBack(dstLcl->GetLclNum(), *dstLastRep));

                            dstLastRep->NeedsWriteBack = false;
                        }

                        dstLastRep->NeedsReadBack = true;
                        dstEndRep--;
                    }
                }
            }

            if (srcInvolvesReplacements)
            {
                unsigned srcLclOffs = srcLcl->GetLclOffs();
                unsigned srcLclSize = srcLcl->GetLayout(m_compiler)->GetSize();

                if (srcFirstRep->Offset < srcLclOffs)
                {
                    if (srcFirstRep->NeedsWriteBack)
                    {
                        JITDUMP("*** Block operation partially overlaps with source %s. Write back is necessary.\n",
                                srcFirstRep->Name);

                        result.AddStatement(CreateWriteBack(srcLcl->GetLclNum(), *srcFirstRep));

                        srcFirstRep->NeedsWriteBack = false;
                    }

                    srcFirstRep++;
                }

                if (srcEndRep > srcFirstRep)
                {
                    Replacement* srcLastRep = srcEndRep - 1;
                    if (srcLastRep->Offset + genTypeSize(srcLastRep->AccessType) > srcLclOffs + srcLclSize)
                    {
                        if (srcLastRep->NeedsWriteBack)
                        {
                            JITDUMP("*** Block operation partially overlaps with source %s. Write back is necessary.\n",
                                    srcLastRep->Name);

                            result.AddStatement(CreateWriteBack(srcLcl->GetLclNum(), *srcLastRep));
                            srcLastRep->NeedsWriteBack = false;
                        }

                        srcEndRep--;
                    }
                }
            }

            DecompositionPlan plan(m_compiler, dst, src);

            if (src->IsConstInitVal())
            {
                GenTree* cns = src->OperIsInitVal() ? src->gtGetOp1() : src;
                InitFields(dst->AsLclVarCommon(), dstFirstRep, dstEndRep,
                           static_cast<unsigned char>(cns->AsIntCon()->IconValue()), &plan);
            }
            else
            {
                CopyBetweenFields(dst, dstFirstRep, dstEndRep, src, srcFirstRep, srcEndRep, &result, &plan);
            }

            plan.Finalize(&result);

            *use          = result.ToCommaTree(m_compiler);
            m_madeChanges = true;
        }
        else
        {
            if (asg->gtGetOp2()->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                GenTreeLclVarCommon* rhsLcl = asg->gtGetOp2()->AsLclVarCommon();
                unsigned             size   = rhsLcl->GetLayout(m_compiler)->GetSize();
                WriteBackBefore(&asg->gtOp2, rhsLcl->GetLclNum(), rhsLcl->GetLclOffs(), size);
            }

            if (asg->gtGetOp1()->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                GenTreeLclVarCommon* lhsLcl = asg->gtGetOp1()->AsLclVarCommon();
                unsigned             size   = lhsLcl->GetLayout(m_compiler)->GetSize();
                MarkForReadBack(lhsLcl->GetLclNum(), lhsLcl->GetLclOffs(), size);
            }
        }
    }

    //------------------------------------------------------------------------
    // InitFields:
    //   Add entries into the plan specifying which replacements can be
    //   directly inited, and mark the other ones as requiring read back.
    //
    // Parameters:
    //   firstRep - The first replacement.
    //   endRep   - End of the replacements.
    //   initVal  - byte pattern to init with
    //   result   - Statement list to add resulting statements to.
    //
    // Remarks:
    //   Sets Replacement::Handled if the replacement was handled and IR was
    //   created to initialize it with the correct value.
    //
    void InitFields(GenTreeLclVarCommon* dst,
                    Replacement*         firstRep,
                    Replacement*         endRep,
                    unsigned char        initVal,
                    DecompositionPlan*   plan)
    {
        for (Replacement* rep = firstRep; rep < endRep; rep++)
        {
            if ((initVal != 0) && (varTypeIsSIMD(rep->AccessType) || varTypeIsGC(rep->AccessType)))
            {
                // We will need to read this one back after initing the struct.
                rep->NeedsWriteBack = false;
                rep->NeedsReadBack  = true;
                continue;
            }

            plan->InitReplacement(rep->LclNum, rep->Offset - dst->GetLclOffs(), rep->AccessType);
            rep->NeedsWriteBack = true;
            rep->NeedsReadBack  = false;
        }
    }

    //------------------------------------------------------------------------
    // CopyBetweenFields:
    //   Copy between two struct locals that may involve replacements.
    //
    // Parameters:
    //   dst         - Destination node
    //   dstFirstRep - First replacement of the destination or nullptr if destination is not a promoted local.
    //   dstEndRep   - One past last replacement of the destination.
    //   src         - Source node
    //   srcFirstRep - First replacement of the source or nullptr if source is not a promoted local.
    //   srcEndRep   - One past last replacement of the source.
    //   statements  - Statement list to add potential "init" statements to.
    //   plan        - Data structure that tracks the specific copies to be done.
    //
    void CopyBetweenFields(GenTree*                    dst,
                           Replacement*                dstFirstRep,
                           Replacement*                dstEndRep,
                           GenTree*                    src,
                           Replacement*                srcFirstRep,
                           Replacement*                srcEndRep,
                           DecompositionStatementList* statements,
                           DecompositionPlan*          plan)
    {
        assert(src->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK, GT_FIELD));

        GenTreeLclVarCommon* dstLcl      = dst->OperIs(GT_LCL_VAR, GT_LCL_FLD) ? dst->AsLclVarCommon() : nullptr;
        GenTreeLclVarCommon* srcLcl      = src->OperIs(GT_LCL_VAR, GT_LCL_FLD) ? src->AsLclVarCommon() : nullptr;
        unsigned             dstBaseOffs = dstLcl != nullptr ? dstLcl->GetLclOffs() : 0;
        unsigned             srcBaseOffs = srcLcl != nullptr ? srcLcl->GetLclOffs() : 0;

        LclVarDsc* dstDsc = dstLcl != nullptr ? m_compiler->lvaGetDesc(dstLcl) : nullptr;
        LclVarDsc* srcDsc = srcLcl != nullptr ? m_compiler->lvaGetDesc(srcLcl) : nullptr;

        Replacement* dstRep = dstFirstRep;
        Replacement* srcRep = srcFirstRep;

        while ((dstRep < dstEndRep) || (srcRep < srcEndRep))
        {
            if ((srcRep < srcEndRep) && srcRep->NeedsReadBack)
            {
                assert(srcLcl != nullptr);
                statements->AddStatement(CreateReadBack(srcLcl->GetLclNum(), *srcRep));
                srcRep->NeedsReadBack = false;
            }

            if ((dstRep < dstEndRep) && (srcRep < srcEndRep))
            {
                if (srcRep->Offset - srcBaseOffs + genTypeSize(srcRep->AccessType) < dstRep->Offset - dstBaseOffs)
                {
                    // This source replacement ends before the next destination replacement starts.
                    // Write it directly to the destination struct local.
                    plan->CopyFromReplacement(srcRep->LclNum, srcRep->Offset - srcBaseOffs, srcRep->AccessType);
                    srcRep++;
                    continue;
                }

                if (dstRep->Offset - dstBaseOffs + genTypeSize(dstRep->AccessType) < srcRep->Offset - srcBaseOffs)
                {
                    // Destination replacement ends before the next source replacement starts.
                    // Read it directly from the source struct local.
                    plan->CopyToReplacement(dstRep->LclNum, dstRep->Offset - dstBaseOffs, dstRep->AccessType);
                    dstRep->NeedsWriteBack = true;
                    dstRep->NeedsReadBack  = false;
                    dstRep++;
                    continue;
                }

                // Overlap. Check for exact match of replacements.
                // TODO-CQ: Allow copies between small types of different signs, and between TYP_I_IMPL/TYP_BYREF?
                if (((dstRep->Offset - dstBaseOffs) == (srcRep->Offset - srcBaseOffs)) &&
                    (dstRep->AccessType == srcRep->AccessType))
                {
                    plan->CopyBetweenReplacements(dstRep->LclNum, srcRep->LclNum, dstRep->Offset - dstBaseOffs,
                                                  dstRep->AccessType);
                    dstRep->NeedsWriteBack = true;
                    dstRep->NeedsReadBack  = false;
                    dstRep++;
                    srcRep++;
                    continue;
                }

                // Partial overlap. Write source back to the struct local. We
                // will handle the destination replacement in a future
                // iteration of the loop.
                statements->AddStatement(CreateWriteBack(srcLcl->GetLclNum(), *srcRep));
                srcRep++;
                continue;
            }

            if (dstRep < dstEndRep)
            {
                if ((srcDsc != nullptr) && srcDsc->lvPromoted)
                {
                    unsigned srcOffs  = srcLcl->GetLclOffs() + (dstRep->Offset - dstBaseOffs);
                    unsigned fieldLcl = m_compiler->lvaGetFieldLocal(srcDsc, srcOffs);

                    if ((fieldLcl != BAD_VAR_NUM) && (m_compiler->lvaGetDesc(fieldLcl)->lvType == dstRep->AccessType))
                    {
                        plan->CopyBetweenReplacements(dstRep->LclNum, fieldLcl, dstRep->Offset - dstBaseOffs,
                                                      dstRep->AccessType);
                        dstRep->NeedsWriteBack = true;
                        dstRep->NeedsReadBack  = false;
                        dstRep++;
                        continue;
                    }
                }

                // TODO-CQ: If the source is promoted then this will result in
                // DNER'ing it. Alternatively we could copy the promoted field
                // directly the destination's struct local and the overlapping
                // fields as needing read back to avoid this DNER.
                plan->CopyToReplacement(dstRep->LclNum, dstRep->Offset - dstBaseOffs, dstRep->AccessType);
                dstRep->NeedsWriteBack = true;
                dstRep->NeedsReadBack  = false;
                dstRep++;
            }
            else
            {
                assert(srcRep < srcEndRep);
                if ((dstDsc != nullptr) && dstDsc->lvPromoted)
                {
                    unsigned dstOffs  = dstLcl->GetLclOffs() + (srcRep->Offset - srcBaseOffs);
                    unsigned fieldLcl = m_compiler->lvaGetFieldLocal(dstDsc, dstOffs);

                    if ((fieldLcl != BAD_VAR_NUM) && (m_compiler->lvaGetDesc(fieldLcl)->lvType == srcRep->AccessType))
                    {
                        plan->CopyBetweenReplacements(fieldLcl, srcRep->LclNum, srcRep->Offset - srcBaseOffs,
                                                      srcRep->AccessType);
                        srcRep++;
                        continue;
                    }
                }

                plan->CopyFromReplacement(srcRep->LclNum, srcRep->Offset - srcBaseOffs, srcRep->AccessType);
                srcRep++;
            }
        }
    }

    //------------------------------------------------------------------------
    // EliminateCommasInBlockOp:
    //   Ensure that the sources of a block op are not commas by extracting side effects.
    //
    // Parameters:
    //   asg    - The block op
    //   result   - Statement list to add resulting statements to.
    //
    // Remarks:
    //   Works similarly to MorphInitBlockHelper::EliminateCommas.
    //
    void EliminateCommasInBlockOp(GenTreeOp* asg, DecompositionStatementList* result)
    {
        bool     any = false;
        GenTree* lhs = asg->gtGetOp1();
        assert(lhs->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_FIELD, GT_IND, GT_BLK));

        GenTree* rhs = asg->gtGetOp2();

        if (asg->IsReverseOp())
        {
            while (rhs->OperIs(GT_COMMA))
            {
                result->AddStatement(rhs->gtGetOp1());
                rhs = rhs->gtGetOp2();
                any = true;
            }
        }
        else
        {
            if (lhs->OperIsUnary() && rhs->OperIs(GT_COMMA))
            {
                GenTree* addr = lhs->gtGetOp1();
                // Note that GTF_GLOB_REF is not up to date here, hence we need
                // a tree walk to find address exposed locals.
                if (((addr->gtFlags & GTF_ALL_EFFECT) != 0) ||
                    (((rhs->gtFlags & GTF_ASG) != 0) && !addr->IsInvariant()) ||
                    m_compiler->gtHasAddressExposedLocals(addr))
                {
                    unsigned lhsAddrLclNum = m_compiler->lvaGrabTemp(true DEBUGARG("Block morph LHS addr"));

                    result->AddStatement(m_compiler->gtNewTempAssign(lhsAddrLclNum, addr));
                    lhs->AsUnOp()->gtOp1 = m_compiler->gtNewLclvNode(lhsAddrLclNum, genActualType(addr));
                    m_compiler->gtUpdateNodeSideEffects(lhs);
                    m_madeChanges = true;
                    any           = true;
                }
            }

            while (rhs->OperIs(GT_COMMA))
            {
                result->AddStatement(rhs->gtGetOp1());
                rhs = rhs->gtGetOp2();
                any = true;
            }
        }

        if (any)
        {
            asg->gtOp2 = rhs;
            m_compiler->gtUpdateNodeSideEffects(asg);
            m_madeChanges = true;
        }
    }

    //------------------------------------------------------------------------
    // OverlappingReplacements:
    //   Find replacements that overlap the specified struct local.
    //
    // Parameters:
    //   lcl              - A struct local
    //   firstReplacement - [out] The first replacement that overlaps
    //   endReplacement   - [out, optional] One past the last replacement that overlaps
    //
    // Returns:
    //   True if any replacement overlaps; otherwise false.
    //
    bool OverlappingReplacements(GenTreeLclVarCommon* lcl,
                                 Replacement**        firstReplacement,
                                 Replacement**        endReplacement = nullptr)
    {
        if (m_replacements[lcl->GetLclNum()] == nullptr)
        {
            return false;
        }

        jitstd::vector<Replacement>& replacements = *m_replacements[lcl->GetLclNum()];

        unsigned offs       = lcl->GetLclOffs();
        unsigned size       = lcl->GetLayout(m_compiler)->GetSize();
        size_t   firstIndex = BinarySearch<Replacement, &Replacement::Offset>(replacements, offs);
        if ((ssize_t)firstIndex < 0)
        {
            firstIndex = ~firstIndex;
            if (firstIndex > 0)
            {
                Replacement& lastRepBefore = replacements[firstIndex - 1];
                if ((lastRepBefore.Offset + genTypeSize(lastRepBefore.AccessType)) > offs)
                {
                    // Overlap with last entry starting before offs.
                    firstIndex--;
                }
                else if (firstIndex >= replacements.size())
                {
                    // Starts after last replacement ends.
                    return false;
                }
            }

            const Replacement& first = replacements[firstIndex];
            if (first.Offset >= (offs + size))
            {
                // First candidate starts after this ends.
                return false;
            }
        }

        assert((firstIndex < replacements.size()) && replacements[firstIndex].Overlaps(offs, size));
        *firstReplacement = &replacements[firstIndex];

        if (endReplacement != nullptr)
        {
            size_t lastIndex = BinarySearch<Replacement, &Replacement::Offset>(replacements, offs + size);
            if ((ssize_t)lastIndex < 0)
            {
                lastIndex = ~lastIndex;
            }

            // Since we verified above that there is an overlapping replacement
            // we know that lastIndex exists and is the next one that does not
            // overlap.
            assert(lastIndex > 0);
            *endReplacement = replacements.data() + lastIndex;
        }

        return true;
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
            *use = m_compiler->gtNewOperNode(GT_COMMA, (*use)->TypeGet(), CreateReadBack(lclNum, rep), *use);
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
                GenTreeOp* comma =
                    m_compiler->gtNewOperNode(GT_COMMA, (*use)->TypeGet(), CreateWriteBack(lcl, rep), *use);
                *use = comma;
                use  = &comma->gtOp2;

                rep.NeedsWriteBack = false;
                m_madeChanges      = true;
            }

            index++;
        }
    }

    //------------------------------------------------------------------------
    // CreateWriteBack:
    //   Create IR that writes a replacement local's value back to its struct local:
    //
    //     ASG
    //       LCL_FLD int V00 [+4]
    //       LCL_VAR int V01
    //
    // Parameters:
    //   structLclNum - Struct local
    //   replacement  - Information about the replacement
    //
    // Returns:
    //   IR nodes.
    //
    GenTree* CreateWriteBack(unsigned structLclNum, const Replacement& replacement)
    {
        GenTree* dst = m_compiler->gtNewLclFldNode(structLclNum, replacement.AccessType, replacement.Offset);
        GenTree* src = m_compiler->gtNewLclvNode(replacement.LclNum, genActualType(replacement.AccessType));
        GenTree* asg = m_compiler->gtNewAssignNode(dst, src);
        return asg;
    }

    //------------------------------------------------------------------------
    // CreateReadBack:
    //   Create IR that reads a replacement local's value back from its struct local:
    //
    //     ASG
    //       LCL_VAR int V01
    //       LCL_FLD int V00 [+4]
    //
    // Parameters:
    //   structLclNum - Struct local
    //   replacement  - Information about the replacement
    //
    // Returns:
    //   IR nodes.
    //
    GenTree* CreateReadBack(unsigned structLclNum, const Replacement& replacement)
    {
        GenTree* dst = m_compiler->gtNewLclvNode(replacement.LclNum, genActualType(replacement.AccessType));
        GenTree* src = m_compiler->gtNewLclFldNode(structLclNum, replacement.AccessType, replacement.Offset);
        GenTree* asg = m_compiler->gtNewAssignNode(dst, src);
        return asg;
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
    void MarkForReadBack(unsigned lcl, unsigned offs, unsigned size)
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

        bool     result = false;
        unsigned end    = offs + size;
        while ((index < replacements.size()) && (replacements[index].Offset < end))
        {
            result           = true;
            Replacement& rep = replacements[index];
            assert(rep.Overlaps(offs, size));
            rep.NeedsReadBack  = true;
            rep.NeedsWriteBack = false;
            index++;
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

                    GenTree* readBack = replacer.CreateReadBack(i, rep);
                    m_compiler->fgInsertStmtNearEnd(bb, m_compiler->fgNewStmtFromTree(readBack));
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
