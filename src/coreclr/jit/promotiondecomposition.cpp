// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "promotion.h"
#include "jitstd/algorithm.h"

// Represents a list of statements; this is the result of assignment decomposition.
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

// Represents a plan for decomposing a block operation into direct treatment of
// replacement fields and the remainder.
class DecompositionPlan
{
    struct Entry
    {
        unsigned     ToLclNum;
        Replacement* ToReplacement;
        unsigned     FromLclNum;
        Replacement* FromReplacement;
        unsigned     Offset;
        var_types    Type;
    };

    Compiler*                       m_compiler;
    jitstd::vector<AggregateInfo*>& m_aggregates;
    PromotionLiveness*              m_liveness;
    GenTree*                        m_dst;
    GenTree*                        m_src;
    bool                            m_dstInvolvesReplacements;
    bool                            m_srcInvolvesReplacements;
    ArrayStack<Entry>               m_entries;
    bool                            m_hasNonRemainderUseOfStructLocal = false;

public:
    DecompositionPlan(Compiler*                       comp,
                      jitstd::vector<AggregateInfo*>& aggregates,
                      PromotionLiveness*              liveness,
                      GenTree*                        dst,
                      GenTree*                        src,
                      bool                            dstInvolvesReplacements,
                      bool                            srcInvolvesReplacements)
        : m_compiler(comp)
        , m_aggregates(aggregates)
        , m_liveness(liveness)
        , m_dst(dst)
        , m_src(src)
        , m_dstInvolvesReplacements(dstInvolvesReplacements)
        , m_srcInvolvesReplacements(srcInvolvesReplacements)
        , m_entries(comp->getAllocator(CMK_Promotion))
    {
    }

    //------------------------------------------------------------------------
    // CopyBetweenReplacements:
    //   Add an entry specifying to copy from a replacement into another replacement.
    //
    // Parameters:
    //   dstRep - The destination replacement.
    //   srcRep - The source replacement.
    //   offset - The offset this covers in the struct copy.
    //   type   - The type of copy.
    //
    void CopyBetweenReplacements(Replacement* dstRep, Replacement* srcRep, unsigned offset)
    {
        m_entries.Push(Entry{dstRep->LclNum, dstRep, srcRep->LclNum, srcRep, offset, dstRep->AccessType});
    }

    //------------------------------------------------------------------------
    // CopyBetweenReplacements:
    //   Add an entry specifying to copy from a promoted field into a replacement.
    //
    // Parameters:
    //   dstRep - The destination replacement.
    //   srcLcl - Local number of regularly promoted source field.
    //   offset - The offset this covers in the struct copy.
    //   type   - The type of copy.
    //
    // Remarks:
    //   Used when the source local is a regular promoted field.
    //
    void CopyBetweenReplacements(Replacement* dstRep, unsigned srcLcl, unsigned offset)
    {
        m_entries.Push(Entry{dstRep->LclNum, dstRep, srcLcl, nullptr, offset, dstRep->AccessType});
    }

    //------------------------------------------------------------------------
    // CopyBetweenReplacements:
    //   Add an entry specifying to copy from a replacement into a promoted field.
    //
    // Parameters:
    //   dstRep - The destination replacement.
    //   srcLcl - Local number of regularly promoted source field.
    //   offset - The offset this covers in the struct copy.
    //   type   - The type of copy.
    //
    // Remarks:
    //   Used when the destination local is a regular promoted field.
    //
    void CopyBetweenReplacements(unsigned dstLcl, Replacement* srcRep, unsigned offset)
    {
        m_entries.Push(Entry{dstLcl, nullptr, srcRep->LclNum, srcRep, offset, srcRep->AccessType});
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
    void CopyToReplacement(Replacement* dstRep, unsigned offset)
    {
        m_entries.Push(Entry{dstRep->LclNum, dstRep, BAD_VAR_NUM, nullptr, offset, dstRep->AccessType});
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
    void CopyFromReplacement(Replacement* srcRep, unsigned offset)
    {
        m_entries.Push(Entry{BAD_VAR_NUM, nullptr, srcRep->LclNum, srcRep, offset, srcRep->AccessType});
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
    void InitReplacement(Replacement* dstRep, unsigned offset)
    {
        m_entries.Push(Entry{dstRep->LclNum, dstRep, BAD_VAR_NUM, nullptr, offset, dstRep->AccessType});
    }

    //------------------------------------------------------------------------
    // MarkNonRemainderUseOfStructLocal:
    //   Mark that some of the destination replacements are being handled via a
    //   readback. This invalidates liveness information for the remainder
    //   because the struct local will now also be used for the readback.
    //
    void MarkNonRemainderUseOfStructLocal()
    {
        m_hasNonRemainderUseOfStructLocal = true;
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
        if (IsInit())
        {
            FinalizeInit(statements);
        }
        else
        {
            FinalizeCopy(statements);
        }
    }

    //------------------------------------------------------------------------
    // CanInitPrimitive:
    //   Check if we can handle initializing a primitive of the specified type.
    //   For example, we cannot directly initialize SIMD types to non-zero
    //   constants.
    //
    // Parameters:
    //   type - The primitive type
    //
    // Returns:
    //   True if so.
    //
    bool CanInitPrimitive(var_types type)
    {
        assert(IsInit());
        if (varTypeIsGC(type) || varTypeIsSIMD(type))
        {
            return GetInitPattern() == 0;
        }

        return true;
    }

private:
    //------------------------------------------------------------------------
    // IsInit:
    //   Check if this is an init block operation.
    //
    // Returns:
    //   True if so.
    //
    bool IsInit()
    {
        return m_src->IsConstInitVal();
    }

    //------------------------------------------------------------------------
    // GetInitPattern:
    //   For an init block operation, get the pattern to init with.
    //
    // Returns:
    //   Byte pattern.
    //
    uint8_t GetInitPattern()
    {
        assert(IsInit());
        GenTree* cns = m_src->OperIsInitVal() ? m_src->gtGetOp1() : m_src;
        return uint8_t(cns->AsIntCon()->IconValue() & 0xFF);
    }

    //------------------------------------------------------------------------
    // ComputeRemainder:
    //   Compute the remainder of the block operation that needs to be inited
    //   or copied after the replacements stored in the plan have been handled.
    //
    // Returns:
    //   Segments representing the remainder.
    //
    // Remarks:
    //   This function takes into account that insignificant padding does not
    //   need to be considered part of the remainder. For example, the last 4
    //   bytes of Span<T> on 64-bit are not returned as the remainder.
    //
    StructSegments ComputeRemainder()
    {
        ClassLayout* dstLayout = m_dst->GetLayout(m_compiler);

        // Validate with "obviously correct" but less scalable fixed bit vector implementation.
        INDEBUG(FixedBitVect * segmentBitVect);
        StructSegments segments = Promotion::SignificantSegments(m_compiler, dstLayout DEBUGARG(&segmentBitVect));

        for (int i = 0; i < m_entries.Height(); i++)
        {
            const Entry& entry = m_entries.BottomRef(i);

            segments.Subtract(StructSegments::Segment(entry.Offset, entry.Offset + genTypeSize(entry.Type)));

#ifdef DEBUG
            for (unsigned i = 0; i < genTypeSize(entry.Type); i++)
                segmentBitVect->bitVectClear(entry.Offset + i);
#endif
        }

#ifdef DEBUG
        segments.Check(segmentBitVect);

        if (m_compiler->verbose)
        {
            printf("  Remainder: ");
            segments.Dump();
            printf("\n");
        }
#endif

        return segments;
    }

    // Represents the strategy for handling the remainder part of the block
    // operation.
    struct RemainderStrategy
    {
        enum
        {
            NoRemainder,
            Primitive,
            FullBlock,
        };

        int       Type;
        unsigned  PrimitiveOffset;
        var_types PrimitiveType;

        RemainderStrategy(int type, unsigned primitiveOffset = 0, var_types primitiveType = TYP_UNDEF)
            : Type(type), PrimitiveOffset(primitiveOffset), PrimitiveType(primitiveType)
        {
        }
    };

    //------------------------------------------------------------------------
    // DetermineRemainderStrategy:
    //   Determine the strategy to use to handle the remaining parts of the struct
    //   once replacements have been handled.
    //
    // Returns:
    //   Type describing how it should be handled; for example, by a full block
    //   copy (that may be redundant with some of the replacements, but covers
    //   the rest of the remainder); or by handling a specific 'hole' as a
    //   primitive.
    //
    RemainderStrategy DetermineRemainderStrategy(const StructDeaths& dstDeaths)
    {
        if (m_dstInvolvesReplacements && !m_hasNonRemainderUseOfStructLocal && dstDeaths.IsRemainderDying())
        {
            JITDUMP("  => Remainder strategy: do nothing (remainder dying)\n");
            return RemainderStrategy(RemainderStrategy::NoRemainder);
        }

        StructSegments remainder = ComputeRemainder();
        if (remainder.IsEmpty())
        {
            JITDUMP("  => Remainder strategy: do nothing (no remainder)\n");
            return RemainderStrategy(RemainderStrategy::NoRemainder);
        }

        StructSegments::Segment segment;
        // See if we can "plug the hole" with a single primitive.
        if (remainder.IsSingleSegment(&segment))
        {
            var_types primitiveType = TYP_UNDEF;
            unsigned  size          = segment.End - segment.Start;
            if ((size == TARGET_POINTER_SIZE) && ((segment.Start % TARGET_POINTER_SIZE) == 0))
            {
                ClassLayout* dstLayout = m_dst->GetLayout(m_compiler);
                primitiveType          = dstLayout->GetGCPtrType(segment.Start / TARGET_POINTER_SIZE);
            }
            else
            {
                switch (size)
                {
                    case 1:
                        primitiveType = TYP_UBYTE;
                        break;
                    case 2:
                        primitiveType = TYP_USHORT;
                        break;
                    case 4:
                        primitiveType = TYP_INT;
                        break;
#ifdef TARGET_64BIT
                    case 8:
                        primitiveType = TYP_LONG;
                        break;
#endif

                        // TODO-CQ: SIMD sizes
                }
            }

            if (primitiveType != TYP_UNDEF)
            {
                if (!IsInit() || CanInitPrimitive(primitiveType))
                {
                    JITDUMP("  => Remainder strategy: %s at +%03u\n", varTypeName(primitiveType), segment.Start);
                    return RemainderStrategy(RemainderStrategy::Primitive, segment.Start, primitiveType);
                }
                else
                {
                    JITDUMP("  Cannot handle initing remainder as primitive of type %s\n", varTypeName(primitiveType));
                }
            }
        }

        JITDUMP("  => Remainder strategy: retain a full block op\n");
        return RemainderStrategy(RemainderStrategy::FullBlock);
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
        uint8_t      initPattern = GetInitPattern();
        StructDeaths deaths      = m_liveness->GetDeathsForStructLocal(m_dst->AsLclVarCommon());

        AggregateInfo* agg = m_aggregates[m_dst->AsLclVarCommon()->GetLclNum()];
        assert((agg != nullptr) && (agg->Replacements.size() > 0));
        Replacement* firstRep = agg->Replacements.data();

        for (int i = 0; i < m_entries.Height(); i++)
        {
            const Entry& entry = m_entries.BottomRef(i);

            assert((entry.ToLclNum != BAD_VAR_NUM) && (entry.ToReplacement != nullptr));
            assert((entry.ToReplacement >= firstRep) && (entry.ToReplacement < firstRep + agg->Replacements.size()));
            size_t replacementIndex = entry.ToReplacement - firstRep;

            if (!deaths.IsReplacementDying((unsigned)replacementIndex))
            {
                GenTree* src = m_compiler->gtNewConWithPattern(entry.Type, initPattern);
                GenTree* dst = m_compiler->gtNewLclvNode(entry.ToLclNum, entry.Type);
                statements->AddStatement(m_compiler->gtNewAssignNode(dst, src));
            }

            entry.ToReplacement->NeedsWriteBack = true;
            entry.ToReplacement->NeedsReadBack  = false;
        }

        RemainderStrategy remainderStrategy = DetermineRemainderStrategy(deaths);
        if (remainderStrategy.Type == RemainderStrategy::FullBlock)
        {
            GenTree* asg = m_compiler->gtNewAssignNode(m_dst, m_src);
            statements->AddStatement(asg);
        }
        else if (remainderStrategy.Type == RemainderStrategy::Primitive)
        {
            GenTree*             src    = m_compiler->gtNewConWithPattern(remainderStrategy.PrimitiveType, initPattern);
            GenTreeLclVarCommon* dstLcl = m_dst->AsLclVarCommon();
            GenTree*             dst = m_compiler->gtNewLclFldNode(dstLcl->GetLclNum(), remainderStrategy.PrimitiveType,
                                                       dstLcl->GetLclOffs() + remainderStrategy.PrimitiveOffset);
            m_compiler->lvaSetVarDoNotEnregister(dstLcl->GetLclNum() DEBUGARG(DoNotEnregisterReason::LocalField));
            statements->AddStatement(m_compiler->gtNewAssignNode(dst, src));
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
        assert(m_dst->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK) && m_src->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK));

        StructDeaths dstDeaths;
        if (m_dstInvolvesReplacements)
        {
            dstDeaths = m_liveness->GetDeathsForStructLocal(m_dst->AsLclVarCommon());
        }

        RemainderStrategy remainderStrategy = DetermineRemainderStrategy(dstDeaths);

        // If the remainder is a full block and is going to incur write barrier
        // then avoid incurring multiple write barriers for each source
        // replacement that is a GC pointer -- write them back to the struct
        // first instead. That is, instead of:
        //
        //   ▌  COMMA     void
        //   ├──▌  ASG       struct (copy)                      <- write barrier
        //   │  ├──▌  BLK       struct<Program+S, 32>
        //   │  │  └──▌  LCL_VAR   byref  V01 arg1
        //   │  └──▌  LCL_VAR   struct<Program+S, 32> V00 arg0
        //   └──▌  COMMA     void
        //      ├──▌  ASG       ref                             <- write barrier
        //      │  ├──▌  IND       ref
        //      │  │  └──▌  ADD       byref
        //      │  │     ├──▌  LCL_VAR   byref  V01 arg1
        //      │  │     └──▌  CNS_INT   long   8
        //      │  └──▌  LCL_VAR   ref    V05 tmp3
        //      └──▌  ASG       ref                             <- write barrier
        //         ├──▌  IND       ref
        //         │  └──▌  ADD       byref
        //         │     ├──▌  LCL_VAR   byref  V01 arg1
        //         │     └──▌  CNS_INT   long   24
        //         └──▌  LCL_VAR   ref    V06 tmp4
        //
        // Produce:
        //
        //   ▌  COMMA     void
        //   ├──▌  ASG       ref                                <- no write barrier
        //   │  ├──▌  LCL_FLD   ref    V00 arg0         [+8]
        //   │  └──▌  LCL_VAR   ref    V05 tmp3
        //   └──▌  COMMA     void
        //      ├──▌  ASG       ref                             <- no write barrier
        //      │  ├──▌  LCL_FLD   ref    V00 arg0         [+24]
        //      │  └──▌  LCL_VAR   ref    V06 tmp4
        //      └──▌  ASG       struct (copy)                   <- write barrier
        //         ├──▌  BLK       struct<Program+S, 32>
        //         │  └──▌  LCL_VAR   byref  V01 arg1          (last use)
        //         └──▌  LCL_VAR   struct<Program+S, 32> V00 arg0
        //
        if ((remainderStrategy.Type == RemainderStrategy::FullBlock) && m_dst->OperIs(GT_BLK) &&
            m_dst->GetLayout(m_compiler)->HasGCPtr())
        {
            for (int i = 0; i < m_entries.Height(); i++)
            {
                const Entry& entry = m_entries.BottomRef(i);
                if ((entry.FromReplacement != nullptr) && (entry.Type == TYP_REF))
                {
                    Replacement* rep = entry.FromReplacement;
                    if (rep->NeedsWriteBack)
                    {
                        statements->AddStatement(
                            Promotion::CreateWriteBack(m_compiler, m_src->AsLclVarCommon()->GetLclNum(), *rep));
                        JITDUMP("  Will write back V%02u (%s) to avoid an additional write barrier\n", rep->LclNum,
                                rep->Description);

                        // The loop below will skip these replacements as an
                        // optimization if it is going to copy the struct
                        // anyway.
                        rep->NeedsWriteBack = false;
                    }
                }
            }
        }

        GenTree*       addr               = nullptr;
        target_ssize_t addrBaseOffs       = 0;
        FieldSeq*      addrBaseOffsFldSeq = nullptr;
        GenTreeFlags   indirFlags         = GTF_EMPTY;

        if (m_dst->OperIs(GT_BLK))
        {
            addr = m_dst->gtGetOp1();
            indirFlags =
                m_dst->gtFlags & (GTF_IND_VOLATILE | GTF_IND_NONFAULTING | GTF_IND_UNALIGNED | GTF_IND_INITCLASS);
        }
        else if (m_src->OperIs(GT_BLK))
        {
            addr = m_src->gtGetOp1();
            indirFlags =
                m_src->gtFlags & (GTF_IND_VOLATILE | GTF_IND_NONFAULTING | GTF_IND_UNALIGNED | GTF_IND_INITCLASS);
        }

        int numAddrUses = 0;

        if (addr != nullptr)
        {
            for (int i = 0; i < m_entries.Height(); i++)
            {
                if (!CanSkipEntry(m_entries.BottomRef(i), dstDeaths, remainderStrategy))
                {
                    numAddrUses++;
                }
            }

            if (remainderStrategy.Type != RemainderStrategy::NoRemainder)
            {
                numAddrUses++;
            }
        }

        bool needsNullCheck = false;
        if ((addr != nullptr) && m_compiler->fgAddrCouldBeNull(addr))
        {
            switch (remainderStrategy.Type)
            {
                case RemainderStrategy::NoRemainder:
                case RemainderStrategy::Primitive:
                    needsNullCheck = true;
                    // See if our first indirection will subsume the null check (usual case).
                    for (int i = 0; i < m_entries.Height(); i++)
                    {
                        if (CanSkipEntry(m_entries.BottomRef(i), dstDeaths, remainderStrategy))
                        {
                            continue;
                        }

                        const Entry& entry = m_entries.BottomRef(i);

                        assert((entry.FromLclNum == BAD_VAR_NUM) || (entry.ToLclNum == BAD_VAR_NUM));
                        needsNullCheck = m_compiler->fgIsBigOffset(entry.Offset);
                        break;
                    }
                    break;
            }
        }

        if (needsNullCheck)
        {
            numAddrUses++;
        }

        if ((addr != nullptr) && (numAddrUses > 1))
        {
            m_compiler->gtPeelOffsets(&addr, &addrBaseOffs, &addrBaseOffsFldSeq);

            if (CanReuseAddressForDecomposedStore(addr))
            {
                if (addr->OperIsLocalRead())
                {
                    // We will introduce more uses of the address local, so it is
                    // no longer dying here.
                    addr->gtFlags &= ~GTF_VAR_DEATH;
                }
            }
            else
            {
                unsigned addrLcl = m_compiler->lvaGrabTemp(true DEBUGARG("Spilling address for field-by-field copy"));
                statements->AddStatement(m_compiler->gtNewTempAssign(addrLcl, addr));
                addr = m_compiler->gtNewLclvNode(addrLcl, addr->TypeGet());
            }
        }

        auto grabAddr = [=, &numAddrUses](unsigned offs) {
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
            }

            target_ssize_t fullOffs = addrBaseOffs + (target_ssize_t)offs;
            if ((fullOffs != 0) || (addrBaseOffsFldSeq != nullptr))
            {
                GenTreeIntCon* offsetNode = m_compiler->gtNewIconNode(fullOffs, TYP_I_IMPL);
                offsetNode->gtFieldSeq    = addrBaseOffsFldSeq;

                var_types addrType = varTypeIsGC(addrUse) ? TYP_BYREF : TYP_I_IMPL;
                addrUse            = m_compiler->gtNewOperNode(GT_ADD, addrType, addrUse, offsetNode);
            }

            return addrUse;
        };

        if (remainderStrategy.Type == RemainderStrategy::FullBlock)
        {
            // We will reuse the existing block op's operands. Rebase the
            // address off of the new local we created.
            if (m_src->OperIs(GT_BLK))
            {
                m_src->AsUnOp()->gtOp1 = grabAddr(0);
            }
            else if (m_dst->OperIs(GT_BLK))
            {
                m_dst->AsUnOp()->gtOp1 = grabAddr(0);
            }
        }

        // If the source involves replacements then do the struct op first --
        // we would overwrite the destination with stale bits if we did it last.
        // If the source does not involve replacements then CQ analysis shows
        // that it's best to do it last.
        if ((remainderStrategy.Type == RemainderStrategy::FullBlock) && m_srcInvolvesReplacements)
        {
            statements->AddStatement(m_compiler->gtNewAssignNode(m_dst, m_src));

            if (m_src->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                // We will introduce uses of the source below so this struct
                // copy is no longer the last use if it was before.
                m_src->gtFlags &= ~GTF_VAR_DEATH;
            }
        }

        if (needsNullCheck)
        {
            GenTreeIndir* indir = m_compiler->gtNewIndir(TYP_BYTE, grabAddr(0));
            PropagateIndirFlags(indir, indirFlags);
            statements->AddStatement(indir);
        }

        StructDeaths srcDeaths;
        if (m_srcInvolvesReplacements)
        {
            srcDeaths = m_liveness->GetDeathsForStructLocal(m_src->AsLclVarCommon());
        }

        for (int i = 0; i < m_entries.Height(); i++)
        {
            const Entry& entry = m_entries.BottomRef(i);

            if (entry.ToReplacement != nullptr)
            {
                entry.ToReplacement->NeedsWriteBack = true;
                entry.ToReplacement->NeedsReadBack  = false;
            }

            if (CanSkipEntry(entry, dstDeaths, remainderStrategy))
            {
                if (entry.FromReplacement != nullptr)
                {
                    JITDUMP(
                        "  Skipping dst+%03u <- V%02u (%s); it is up-to-date in its struct local and will be handled "
                        "as part of the remainder\n",
                        entry.Offset, entry.FromReplacement->LclNum, entry.FromReplacement->Description);
                }
                else if (entry.ToReplacement != nullptr)
                {
                    JITDUMP("  Skipping def of V%02u (%s); it is dying", entry.ToReplacement->LclNum,
                            entry.ToReplacement->Description);
                }

                continue;
            }

            GenTree* dst;
            if (entry.ToLclNum != BAD_VAR_NUM)
            {
                dst = m_compiler->gtNewLclvNode(entry.ToLclNum, entry.Type);
            }
            else if (m_dst->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                unsigned offs = m_dst->AsLclVarCommon()->GetLclOffs() + entry.Offset;
                // Local morph ensures we do not see local indirs here that dereference beyond UINT16_MAX.
                noway_assert(FitsIn<uint16_t>(offs));
                dst = m_compiler->gtNewLclFldNode(m_dst->AsLclVarCommon()->GetLclNum(), entry.Type, offs);
                m_compiler->lvaSetVarDoNotEnregister(m_dst->AsLclVarCommon()->GetLclNum()
                                                         DEBUGARG(DoNotEnregisterReason::LocalField));
            }
            else
            {
                GenTree* addr = grabAddr(entry.Offset);
                dst           = m_compiler->gtNewIndir(entry.Type, addr);
                PropagateIndirFlags(dst, indirFlags);
            }

            GenTree* src;
            if (entry.FromLclNum != BAD_VAR_NUM)
            {
                src = m_compiler->gtNewLclvNode(entry.FromLclNum, entry.Type);

                if (entry.FromReplacement != nullptr)
                {
                    AggregateInfo* srcAgg   = m_aggregates[m_src->AsLclVarCommon()->GetLclNum()];
                    Replacement*   firstRep = srcAgg->Replacements.data();
                    assert((entry.FromReplacement >= firstRep) &&
                           (entry.FromReplacement < (firstRep + srcAgg->Replacements.size())));
                    size_t replacementIndex = entry.FromReplacement - firstRep;
                    if (srcDeaths.IsReplacementDying((unsigned)replacementIndex))
                    {
                        src->gtFlags |= GTF_VAR_DEATH;
                    }
                }
            }
            else if (m_src->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                unsigned offs = m_src->AsLclVarCommon()->GetLclOffs() + entry.Offset;
                noway_assert(FitsIn<uint16_t>(offs));
                src = m_compiler->gtNewLclFldNode(m_src->AsLclVarCommon()->GetLclNum(), entry.Type, offs);
                m_compiler->lvaSetVarDoNotEnregister(m_src->AsLclVarCommon()->GetLclNum()
                                                         DEBUGARG(DoNotEnregisterReason::LocalField));
            }
            else
            {
                GenTree* addr = grabAddr(entry.Offset);
                src           = m_compiler->gtNewIndir(entry.Type, addr);
                PropagateIndirFlags(src, indirFlags);
            }

            statements->AddStatement(m_compiler->gtNewAssignNode(dst, src));
        }

        if ((remainderStrategy.Type == RemainderStrategy::FullBlock) && !m_srcInvolvesReplacements)
        {
            statements->AddStatement(m_compiler->gtNewAssignNode(m_dst, m_src));
        }

        if (remainderStrategy.Type == RemainderStrategy::Primitive)
        {
            GenTree* dst;
            if (m_dst->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                GenTreeLclVarCommon* dstLcl = m_dst->AsLclVarCommon();
                dst = m_compiler->gtNewLclFldNode(dstLcl->GetLclNum(), remainderStrategy.PrimitiveType,
                                                  dstLcl->GetLclOffs() + remainderStrategy.PrimitiveOffset);
                m_compiler->lvaSetVarDoNotEnregister(dstLcl->GetLclNum() DEBUGARG(DoNotEnregisterReason::LocalField));
            }
            else
            {
                dst = m_compiler->gtNewIndir(remainderStrategy.PrimitiveType,
                                             grabAddr(remainderStrategy.PrimitiveOffset));
                PropagateIndirFlags(dst, indirFlags);
            }

            GenTree* src;
            if (m_src->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                GenTreeLclVarCommon* srcLcl = m_src->AsLclVarCommon();
                src = m_compiler->gtNewLclFldNode(srcLcl->GetLclNum(), remainderStrategy.PrimitiveType,
                                                  srcLcl->GetLclOffs() + remainderStrategy.PrimitiveOffset);
                m_compiler->lvaSetVarDoNotEnregister(srcLcl->GetLclNum() DEBUGARG(DoNotEnregisterReason::LocalField));
            }
            else
            {
                src = m_compiler->gtNewIndir(remainderStrategy.PrimitiveType,
                                             grabAddr(remainderStrategy.PrimitiveOffset));
                PropagateIndirFlags(src, indirFlags);
            }

            statements->AddStatement(m_compiler->gtNewAssignNode(dst, src));
        }

        assert(numAddrUses == 0);
    }

    //------------------------------------------------------------------------
    // CanSkipEntry:
    //   Check if the specified entry can be skipped because it is writing to a
    //   death replacement or because the remainder would handle it anyway.
    //
    // Parameters:
    //   entry             - The init/copy entry
    //   remainderStrategy - The strategy we are using for the remainder
    //
    bool CanSkipEntry(const Entry& entry, const StructDeaths& deaths, const RemainderStrategy& remainderStrategy)
    {
        if (entry.ToReplacement != nullptr)
        {
            // Check if this entry is dying anyway.
            assert(m_dstInvolvesReplacements);

            AggregateInfo* agg = m_aggregates[m_dst->AsLclVarCommon()->GetLclNum()];
            assert((agg != nullptr) && (agg->Replacements.size() > 0));
            Replacement* firstRep = agg->Replacements.data();
            assert((entry.ToReplacement >= firstRep) && (entry.ToReplacement < (firstRep + agg->Replacements.size())));

            size_t replacementIndex = entry.ToReplacement - firstRep;
            if (deaths.IsReplacementDying((unsigned)replacementIndex))
            {
                return true;
            }
        }

        if (entry.FromReplacement != nullptr)
        {
            // Check if the remainder is going to handle it.
            return (remainderStrategy.Type == RemainderStrategy::FullBlock) && !entry.FromReplacement->NeedsWriteBack &&
                   (entry.ToLclNum == BAD_VAR_NUM);
        }

        return false;
    }

    //------------------------------------------------------------------------
    // CanReuseAddressForDecomposedStore: Check if it is safe to reuse the
    // specified address node for each decomposed store of a block copy.
    //
    // Arguments:
    //   addrNode - The address node
    //
    // Return Value:
    //   True if the caller can reuse the address by cloning.
    //
    bool CanReuseAddressForDecomposedStore(GenTree* addrNode)
    {
        if (addrNode->OperIsLocalRead())
        {
            GenTreeLclVarCommon* lcl    = addrNode->AsLclVarCommon();
            unsigned             lclNum = lcl->GetLclNum();
            if (m_compiler->lvaGetDesc(lclNum)->IsAddressExposed())
            {
                // Address could be pointing to itself
                return false;
            }

            // If we aren't writing a local here then since the address is not
            // exposed it cannot change.
            if (!m_dst->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                return true;
            }

            // Otherwise it could still be possible that the address is part of
            // the struct we're writing.
            unsigned dstLclNum = m_dst->AsLclVarCommon()->GetLclNum();
            if (lclNum == dstLclNum)
            {
                return false;
            }

            // It could also be one of the replacement locals we're going to write.
            for (int i = 0; i < m_entries.Height(); i++)
            {
                if (m_entries.BottomRef(i).ToLclNum == lclNum)
                {
                    return false;
                }
            }

            return true;
        }

        return addrNode->IsInvariant();
    }

    //------------------------------------------------------------------------
    // PropagateIndirFlags:
    //   Propagate the specified flags to a GT_IND node.
    //
    // Parameters:
    //   indir - The indirection to apply flags to
    //   flags - The specified indirection flags.
    //
    void PropagateIndirFlags(GenTree* indir, GenTreeFlags flags)
    {
        if (genTypeSize(indir) == 1)
        {
            flags &= ~GTF_IND_UNALIGNED;
        }

        indir->gtFlags |= flags;
    }
};

//------------------------------------------------------------------------
// gtPeelOffsets: Peel all ADD(addr, CNS_INT(x)) nodes off the specified address
// node and return the base node and sum of offsets peeled.
//
// Arguments:
//   addr   - [in, out] The address node.
//   offset - [out] The sum of offset peeled such that ADD(addr, offset) is equivalent to the original addr.
//   fldSeq - [out] The combined field sequence for all the peeled offsets.
//
void Compiler::gtPeelOffsets(GenTree** addr, target_ssize_t* offset, FieldSeq** fldSeq)
{
    assert((*addr)->TypeIs(TYP_I_IMPL, TYP_BYREF, TYP_REF));
    *offset = 0;
    *fldSeq = nullptr;
    while ((*addr)->OperIs(GT_ADD) && !(*addr)->gtOverflow())
    {
        GenTree* op1 = (*addr)->gtGetOp1();
        GenTree* op2 = (*addr)->gtGetOp2();

        if (op2->IsCnsIntOrI() && !op2->AsIntCon()->IsIconHandle())
        {
            assert(op2->TypeIs(TYP_I_IMPL));
            GenTreeIntCon* intCon = op2->AsIntCon();
            *offset += (target_ssize_t)intCon->IconValue();
            *fldSeq = m_fieldSeqStore->Append(*fldSeq, intCon->gtFieldSeq);
            *addr   = op1;
        }
        else if (op1->IsCnsIntOrI() && !op1->AsIntCon()->IsIconHandle())
        {
            assert(op1->TypeIs(TYP_I_IMPL));
            GenTreeIntCon* intCon = op1->AsIntCon();
            *offset += (target_ssize_t)intCon->IconValue();
            *fldSeq = m_fieldSeqStore->Append(intCon->gtFieldSeq, *fldSeq);
            *addr   = op2;
        }
        else
        {
            break;
        }
    }
}

//------------------------------------------------------------------------
// HandleAssignment:
//   Handle an assignment that may be between struct locals with replacements.
//
// Parameters:
//   asg - The assignment
//   user - The user of the assignment.
//
void ReplaceVisitor::HandleAssignment(GenTree** use, GenTree* user)
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
        // TODO-CQ: If the destination is an aggregate we can still use liveness
        // information for the remainder to DCE this.
        return;
    }

    JITDUMP("Processing block operation [%06u] that involves replacements\n", Compiler::dspTreeID(asg));

    if (src->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK) || src->IsConstInitVal())
    {
        DecompositionStatementList result;
        EliminateCommasInBlockOp(asg, &result);

        DecompositionPlan plan(m_compiler, m_aggregates, m_liveness, dst, src, dstInvolvesReplacements,
                               srcInvolvesReplacements);

        if (dstInvolvesReplacements)
        {
            unsigned dstLclOffs = dstLcl->GetLclOffs();
            unsigned dstLclSize = dstLcl->GetLayout(m_compiler)->GetSize();
            if (dstFirstRep->Offset < dstLclOffs)
            {
                JITDUMP("*** Block operation partially overlaps with start replacement of destination V%02u (%s)\n",
                        dstFirstRep->LclNum, dstFirstRep->Description);

                if (dstFirstRep->NeedsWriteBack)
                {
                    // The value of the replacement will be partially assembled from its old value and this struct
                    // operation.
                    // We accomplish this by an initial write back, the struct copy, followed by a later read back.
                    // TODO-CQ: This is expensive and unreflected in heuristics, but it is also very rare.
                    result.AddStatement(Promotion::CreateWriteBack(m_compiler, dstLcl->GetLclNum(), *dstFirstRep));
                    dstFirstRep->NeedsWriteBack = false;
                }

                plan.MarkNonRemainderUseOfStructLocal();
                dstFirstRep->NeedsReadBack = true;
                dstFirstRep++;
            }

            if (dstEndRep > dstFirstRep)
            {
                Replacement* dstLastRep = dstEndRep - 1;
                if (dstLastRep->Offset + genTypeSize(dstLastRep->AccessType) > dstLclOffs + dstLclSize)
                {
                    JITDUMP("*** Block operation partially overlaps with end replacement of destination V%02u (%s)\n",
                            dstLastRep->LclNum, dstLastRep->Description);

                    if (dstLastRep->NeedsWriteBack)
                    {
                        result.AddStatement(Promotion::CreateWriteBack(m_compiler, dstLcl->GetLclNum(), *dstLastRep));
                        dstLastRep->NeedsWriteBack = false;
                    }

                    plan.MarkNonRemainderUseOfStructLocal();
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
                JITDUMP("*** Block operation partially overlaps with start replacement of source V%02u (%s)\n",
                        srcFirstRep->LclNum, srcFirstRep->Description);

                if (srcFirstRep->NeedsWriteBack)
                {
                    result.AddStatement(Promotion::CreateWriteBack(m_compiler, srcLcl->GetLclNum(), *srcFirstRep));
                    srcFirstRep->NeedsWriteBack = false;
                }

                srcFirstRep++;
            }

            if (srcEndRep > srcFirstRep)
            {
                Replacement* srcLastRep = srcEndRep - 1;
                if (srcLastRep->Offset + genTypeSize(srcLastRep->AccessType) > srcLclOffs + srcLclSize)
                {
                    JITDUMP("*** Block operation partially overlaps with end replacement of source V%02u (%s)\n",
                            srcLastRep->LclNum, srcLastRep->Description);

                    if (srcLastRep->NeedsWriteBack)
                    {
                        result.AddStatement(Promotion::CreateWriteBack(m_compiler, srcLcl->GetLclNum(), *srcLastRep));
                        srcLastRep->NeedsWriteBack = false;
                    }

                    srcEndRep--;
                }
            }
        }

        if (src->IsConstInitVal())
        {
            InitFields(dst->AsLclVarCommon(), dstFirstRep, dstEndRep, &plan);
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
bool ReplaceVisitor::OverlappingReplacements(GenTreeLclVarCommon* lcl,
                                             Replacement**        firstReplacement,
                                             Replacement**        endReplacement)
{
    AggregateInfo* agg = m_aggregates[lcl->GetLclNum()];
    if (agg == nullptr)
    {
        return false;
    }

    unsigned offs = lcl->GetLclOffs();
    unsigned size = lcl->GetLayout(m_compiler)->GetSize();
    return agg->OverlappingReplacements(offs, size, firstReplacement, endReplacement);
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
void ReplaceVisitor::EliminateCommasInBlockOp(GenTreeOp* asg, DecompositionStatementList* result)
{
    bool     any = false;
    GenTree* lhs = asg->gtGetOp1();
    assert(lhs->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_IND, GT_BLK));

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
        if (lhs->OperIsIndir() && rhs->OperIs(GT_COMMA))
        {
            GenTree* addr = lhs->gtGetOp1();
            // Note that GTF_GLOB_REF is not up to date here, hence we need
            // a tree walk to find address exposed locals.
            if (((addr->gtFlags & GTF_ALL_EFFECT) != 0) || (((rhs->gtFlags & GTF_ASG) != 0) && !addr->IsInvariant()) ||
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
// InitFields:
//   Add entries into the plan specifying which replacements can be
//   directly inited, and mark the other ones as requiring read back.
//
// Parameters:
//   dst      - Destination local that involves replacement.
//   firstRep - The first replacement.
//   endRep   - End of the replacements.
//   plan     - Decomposition plan to add initialization entries into.
//
void ReplaceVisitor::InitFields(GenTreeLclVarCommon* dst,
                                Replacement*         firstRep,
                                Replacement*         endRep,
                                DecompositionPlan*   plan)
{
    for (Replacement* rep = firstRep; rep < endRep; rep++)
    {
        if (!plan->CanInitPrimitive(rep->AccessType))
        {
            JITDUMP("  Unsupported init of %s %s. Will init as struct and read back.\n", varTypeName(rep->AccessType),
                    rep->Description);

            // We will need to read this one back after initing the struct.
            rep->NeedsWriteBack = false;
            rep->NeedsReadBack  = true;
            plan->MarkNonRemainderUseOfStructLocal();
            continue;
        }

        JITDUMP("  Init V%02u (%s)\n", rep->LclNum, rep->Description);
        plan->InitReplacement(rep, rep->Offset - dst->GetLclOffs());
    }
}

#ifdef DEBUG
//------------------------------------------------------------------------
// LastUseString:
//   Return a string indicating whether a replacement is a last use, for
//   JITDUMP purposes.
//
// Parameters:
//   lcl - A struct local
//   rep - A replacement of that struct local
//
// Returns:
//   " (last use)" if it is, and otherwise "".
//
const char* ReplaceVisitor::LastUseString(GenTreeLclVarCommon* lcl, Replacement* rep)
{
    StructDeaths   deaths = m_liveness->GetDeathsForStructLocal(lcl);
    AggregateInfo* agg    = m_aggregates[lcl->GetLclNum()];
    assert(agg != nullptr);
    Replacement* firstRep = agg->Replacements.data();
    assert((rep >= firstRep) && (rep < firstRep + agg->Replacements.size()));

    size_t replacementIndex = rep - firstRep;
    if (deaths.IsReplacementDying((unsigned)replacementIndex))
    {
        return " (last use)";
    }

    return "";
}
#endif

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
void ReplaceVisitor::CopyBetweenFields(GenTree*                    dst,
                                       Replacement*                dstFirstRep,
                                       Replacement*                dstEndRep,
                                       GenTree*                    src,
                                       Replacement*                srcFirstRep,
                                       Replacement*                srcEndRep,
                                       DecompositionStatementList* statements,
                                       DecompositionPlan*          plan)
{
    assert(src->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK));

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
            JITDUMP("  Source replacement V%02u (%s) is stale. Will read it back before copy.\n", srcRep->LclNum,
                    srcRep->Description);

            assert(srcLcl != nullptr);
            statements->AddStatement(Promotion::CreateReadBack(m_compiler, srcLcl->GetLclNum(), *srcRep));
            srcRep->NeedsReadBack = false;
            assert(!srcRep->NeedsWriteBack);
        }

        if ((dstRep < dstEndRep) && (srcRep < srcEndRep))
        {
            if (srcRep->Offset - srcBaseOffs + genTypeSize(srcRep->AccessType) < dstRep->Offset - dstBaseOffs)
            {
                // This source replacement ends before the next destination replacement starts.
                // Write it directly to the destination struct local.
                unsigned offs = srcRep->Offset - srcBaseOffs;
                plan->CopyFromReplacement(srcRep, offs);
                JITDUMP("  dst+%03u <- V%02u (%s)%s\n", offs, srcRep->LclNum, srcRep->Description,
                        LastUseString(srcLcl, srcRep));
                srcRep++;
                continue;
            }

            if (dstRep->Offset - dstBaseOffs + genTypeSize(dstRep->AccessType) < srcRep->Offset - srcBaseOffs)
            {
                // Destination replacement ends before the next source replacement starts.
                // Read it directly from the source struct local.
                unsigned offs = dstRep->Offset - dstBaseOffs;
                plan->CopyToReplacement(dstRep, offs);
                JITDUMP("  V%02u (%s)%s <- src+%03u\n", dstRep->LclNum, dstRep->Description,
                        LastUseString(dstLcl, dstRep), offs);
                dstRep++;
                continue;
            }

            // Overlap. Check for exact match of replacements.
            // TODO-CQ: Allow copies between small types of different signs, and between TYP_I_IMPL/TYP_BYREF?
            if (((dstRep->Offset - dstBaseOffs) == (srcRep->Offset - srcBaseOffs)) &&
                (dstRep->AccessType == srcRep->AccessType))
            {
                plan->CopyBetweenReplacements(dstRep, srcRep, dstRep->Offset - dstBaseOffs);
                JITDUMP("  V%02u (%s)%s <- V%02u (%s)%s\n", dstRep->LclNum, dstRep->Description,
                        LastUseString(dstLcl, dstRep), srcRep->LclNum, srcRep->Description,
                        LastUseString(srcLcl, srcRep));

                dstRep++;
                srcRep++;
                continue;
            }

            // Partial overlap. Write source back to the struct local. We
            // will handle the destination replacement in a future
            // iteration of the loop.
            statements->AddStatement(Promotion::CreateWriteBack(m_compiler, srcLcl->GetLclNum(), *srcRep));
            JITDUMP("  Partial overlap of V%02u (%s)%s <- V%02u (%s)%s. Will read source back before copy\n",
                    dstRep->LclNum, dstRep->Description, LastUseString(dstLcl, dstRep), srcRep->LclNum,
                    srcRep->Description, LastUseString(srcLcl, srcRep));
            srcRep++;
            continue;
        }

        if (dstRep < dstEndRep)
        {
            unsigned offs = dstRep->Offset - dstBaseOffs;

            if ((srcDsc != nullptr) && srcDsc->lvPromoted)
            {
                unsigned srcOffs  = srcLcl->GetLclOffs() + offs;
                unsigned fieldLcl = m_compiler->lvaGetFieldLocal(srcDsc, srcOffs);

                if (fieldLcl != BAD_VAR_NUM)
                {
                    LclVarDsc* dsc = m_compiler->lvaGetDesc(fieldLcl);
                    if (dsc->lvType == dstRep->AccessType)
                    {
                        plan->CopyBetweenReplacements(dstRep, fieldLcl, offs);
                        JITDUMP("  V%02u (%s)%s <- V%02u (%s)\n", dstRep->LclNum, dstRep->Description,
                                LastUseString(dstLcl, dstRep), dsc->lvReason);
                        dstRep++;
                        continue;
                    }
                }
            }

            // TODO-CQ: If the source is promoted then this will result in
            // DNER'ing it. Alternatively we could copy the promoted field
            // directly to the destination's struct local and mark the
            // overlapping fields as needing read back to avoid this DNER.
            plan->CopyToReplacement(dstRep, offs);
            JITDUMP("  V%02u (%s)%s <- src+%03u\n", dstRep->LclNum, dstRep->Description, LastUseString(dstLcl, dstRep),
                    offs);
            dstRep++;
        }
        else
        {
            assert(srcRep < srcEndRep);
            unsigned offs = srcRep->Offset - srcBaseOffs;
            if ((dstDsc != nullptr) && dstDsc->lvPromoted)
            {
                unsigned dstOffs  = dstLcl->GetLclOffs() + offs;
                unsigned fieldLcl = m_compiler->lvaGetFieldLocal(dstDsc, dstOffs);

                if (fieldLcl != BAD_VAR_NUM)
                {
                    LclVarDsc* dsc = m_compiler->lvaGetDesc(fieldLcl);
                    if (dsc->lvType == srcRep->AccessType)
                    {
                        plan->CopyBetweenReplacements(fieldLcl, srcRep, offs);
                        JITDUMP("  V%02u (%s) <- V%02u (%s)%s\n", fieldLcl, dsc->lvReason, srcRep->LclNum,
                                srcRep->Description, LastUseString(srcLcl, srcRep));
                        srcRep++;
                        continue;
                    }
                }
            }

            plan->CopyFromReplacement(srcRep, offs);
            JITDUMP("  dst+%03u <- V%02u (%s)%s\n", offs, srcRep->LclNum, srcRep->Description,
                    LastUseString(srcLcl, srcRep));
            srcRep++;
        }
    }
}
