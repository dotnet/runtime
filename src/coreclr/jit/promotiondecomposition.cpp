// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "promotion.h"
#include "jitstd/algorithm.h"

// Represents a list of statements; this is the result of store decomposition.
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
        Replacement* ToReplacement;
        Replacement* FromReplacement;
        unsigned     Offset;
        var_types    Type;
    };

    Promotion*         m_promotion;
    Compiler*          m_compiler;
    ReplaceVisitor*    m_replacer;
    AggregateInfoMap&  m_aggregates;
    PromotionLiveness* m_liveness;
    GenTree*           m_store;
    GenTree*           m_src;
    bool               m_dstInvolvesReplacements;
    bool               m_srcInvolvesReplacements;
    ArrayStack<Entry>  m_entries;
    bool               m_hasNonRemainderUseOfStructLocal = false;

public:
    DecompositionPlan(Promotion*         prom,
                      ReplaceVisitor*    replacer,
                      AggregateInfoMap&  aggregates,
                      PromotionLiveness* liveness,
                      GenTree*           store,
                      GenTree*           src,
                      bool               dstInvolvesReplacements,
                      bool               srcInvolvesReplacements)
        : m_promotion(prom)
        , m_compiler(prom->m_compiler)
        , m_replacer(replacer)
        , m_aggregates(aggregates)
        , m_liveness(liveness)
        , m_store(store)
        , m_src(src)
        , m_dstInvolvesReplacements(dstInvolvesReplacements)
        , m_srcInvolvesReplacements(srcInvolvesReplacements)
        , m_entries(prom->m_compiler->getAllocator(CMK_Promotion))
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
        m_entries.Push(Entry{dstRep, srcRep, offset, dstRep->AccessType});
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
        m_entries.Push(Entry{dstRep, nullptr, offset, dstRep->AccessType});
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
        m_entries.Push(Entry{nullptr, srcRep, offset, srcRep->AccessType});
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
        m_entries.Push(Entry{dstRep, nullptr, offset, dstRep->AccessType});
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
        ClassLayout* dstLayout = m_store->GetLayout(m_compiler);

        StructSegments segments = m_promotion->SignificantSegments(dstLayout);

        for (int i = 0; i < m_entries.Height(); i++)
        {
            const Entry& entry = m_entries.BottomRef(i);

            segments.Subtract(StructSegments::Segment(entry.Offset, entry.Offset + genTypeSize(entry.Type)));
        }

#ifdef DEBUG
        if (m_compiler->verbose)
        {
            printf("  Block op remainder: ");
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
        if (remainder.CoveringSegment(&segment))
        {
            var_types    primitiveType = TYP_UNDEF;
            unsigned     size          = segment.End - segment.Start;
            ClassLayout* dstLayout     = m_store->GetLayout(m_compiler);

            if ((size == TARGET_POINTER_SIZE) && ((segment.Start % TARGET_POINTER_SIZE) == 0))
            {
                primitiveType = dstLayout->GetGCPtrType(segment.Start / TARGET_POINTER_SIZE);
            }
            else if (!dstLayout->IntersectsGCPtr(segment.Start, size))
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

#ifdef FEATURE_SIMD
                    case 16:
                        if (m_compiler->getPreferredVectorByteLength() >= 16)
                        {
                            primitiveType = TYP_SIMD16;
                        }
                        break;
#ifdef TARGET_XARCH
                    case 32:
                        if (m_compiler->getPreferredVectorByteLength() >= 32)
                        {
                            primitiveType = TYP_SIMD32;
                        }
                        break;

                    case 64:
                        if (m_compiler->getPreferredVectorByteLength() >= 64)
                        {
                            primitiveType = TYP_SIMD64;
                        }
                        break;
#endif
#endif
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
        StructDeaths deaths      = m_liveness->GetDeathsForStructLocal(m_store->AsLclVarCommon());

        AggregateInfo* agg = m_aggregates.Lookup(m_store->AsLclVarCommon()->GetLclNum());
        assert((agg != nullptr) && (agg->Replacements.size() > 0));
        Replacement* firstRep = agg->Replacements.data();

        for (int i = 0; i < m_entries.Height(); i++)
        {
            const Entry& entry = m_entries.BottomRef(i);

            assert(entry.ToReplacement != nullptr);
            assert((entry.ToReplacement >= firstRep) && (entry.ToReplacement < firstRep + agg->Replacements.size()));
            size_t replacementIndex = entry.ToReplacement - firstRep;

            if (!deaths.IsReplacementDying((unsigned)replacementIndex))
            {
                GenTree* value = m_compiler->gtNewConWithPattern(entry.Type, initPattern);
                GenTree* store = m_compiler->gtNewStoreLclVarNode(entry.ToReplacement->LclNum, value);
                statements->AddStatement(store);
            }

            m_replacer->ClearNeedsReadBack(*entry.ToReplacement);
            m_replacer->SetNeedsWriteBack(*entry.ToReplacement);
        }

        RemainderStrategy remainderStrategy = DetermineRemainderStrategy(deaths);
        if (remainderStrategy.Type == RemainderStrategy::FullBlock)
        {
            statements->AddStatement(m_store);
        }
        else if (remainderStrategy.Type == RemainderStrategy::Primitive)
        {
            GenTree*       value = m_compiler->gtNewConWithPattern(remainderStrategy.PrimitiveType, initPattern);
            LocationAccess storeAccess;
            storeAccess.InitializeLocal(m_store->AsLclVarCommon());
            GenTree* store = storeAccess.CreateStore(remainderStrategy.PrimitiveOffset, remainderStrategy.PrimitiveType,
                                                     value, m_compiler);
            statements->AddStatement(store);
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
        assert(m_store->OperIs(GT_STORE_LCL_VAR, GT_STORE_LCL_FLD, GT_STORE_BLK) &&
               m_src->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK));

        StructDeaths dstDeaths;
        if (m_dstInvolvesReplacements)
        {
            dstDeaths = m_liveness->GetDeathsForStructLocal(m_store->AsLclVarCommon());
        }

        RemainderStrategy remainderStrategy = DetermineRemainderStrategy(dstDeaths);

        // If the remainder is a full block and is going to incur write barrier
        // then avoid incurring multiple write barriers for each source
        // replacement that is a GC pointer -- write them back to the struct
        // first instead. That is, instead of:
        //
        //   ▌  COMMA     void
        //   ├──▌  STORE_BLK struct<Program+S, 32>        <- write barrier
        //   │  ├──▌  LCL_VAR   byref  V01 arg1
        //   │  └──▌  LCL_VAR   struct<Program+S, 32> V00 arg0
        //   └──▌  COMMA     void
        //      ├──▌  STOREIND ref                        <- write barrier
        //      │  ├───▌  ADD       byref
        //      │  │   ├──▌  LCL_VAR   byref  V01 arg1
        //      │  │   └──▌  CNS_INT   long   8
        //      │  └──▌  LCL_VAR   ref    V05 tmp3
        //      └──▌  STOREIND   ref                      <- write barrier
        //         ├──▌  ADD       byref
        //         │  ├──▌  LCL_VAR   byref  V01 arg1
        //         │  └──▌  CNS_INT   long   24
        //         └──▌  LCL_VAR   ref    V06 tmp4
        //
        // Produce:
        //
        //   ▌  COMMA     void
        //   ├──▌  STORE_LCL_FLD ref    V00 arg0         [+8]   <- no write barrier
        //   │  └──▌  LCL_VAR   ref    V05 tmp3
        //   └──▌  COMMA     void
        //      ├──▌  STORE_LCL_FLD ref    V00 arg0      [+24]  <- no write barrier
        //      │  └──▌  LCL_VAR   ref    V06 tmp4
        //      └──▌  STORE_BLK struct<Program+S, 32>           <- write barrier
        //         ├──▌  LCL_VAR   byref  V01 arg1          (last use)
        //         └──▌  LCL_VAR   struct<Program+S, 32> V00 arg0
        //
        if ((remainderStrategy.Type == RemainderStrategy::FullBlock) && m_store->OperIs(GT_STORE_BLK) &&
            m_store->AsBlk()->GetLayout()->HasGCPtr())
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
                        m_replacer->ClearNeedsWriteBack(*rep);
                    }
                }
            }
        }

        // We prefer to do the remainder at the end, if possible, since CQ
        // analysis shows that this is best. However, handling the remainder
        // may overwrite the destination with stale bits if the source has
        // replacements (since handling the remainder copies from the struct,
        // and the fresh values are usually in the replacement locals).
        bool handleRemainderFirst = RemainderOverwritesDestinationWithStaleBits(remainderStrategy, dstDeaths);

        GenTree*       addr               = nullptr;
        target_ssize_t addrBaseOffs       = 0;
        FieldSeq*      addrBaseOffsFldSeq = nullptr;
        GenTreeFlags   indirFlags         = GTF_EMPTY;

        if (m_store->OperIs(GT_STORE_BLK))
        {
            addr       = m_store->AsIndir()->Addr();
            indirFlags = m_store->gtFlags & GTF_IND_COPYABLE_FLAGS;
        }
        else if (m_src->OperIs(GT_BLK))
        {
            addr       = m_src->AsIndir()->Addr();
            indirFlags = m_src->gtFlags & GTF_IND_COPYABLE_FLAGS;
        }

        int numAddrUses = 0;

        bool needsNullCheck = false;

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

            if (m_compiler->fgAddrCouldBeNull(addr))
            {
                if (handleRemainderFirst)
                {
                    needsNullCheck = (remainderStrategy.Type == RemainderStrategy::Primitive) &&
                                     m_compiler->fgIsBigOffset(remainderStrategy.PrimitiveOffset);
                }
                else
                {
                    needsNullCheck = true;
                    // See if our first indirection will subsume the null check (usual case).
                    for (int i = 0; i < m_entries.Height(); i++)
                    {
                        if (CanSkipEntry(m_entries.BottomRef(i), dstDeaths, remainderStrategy))
                        {
                            continue;
                        }
                        const Entry& entry = m_entries.BottomRef(i);
                        assert((entry.FromReplacement == nullptr) || (entry.ToReplacement == nullptr));
                        needsNullCheck = m_compiler->fgIsBigOffset(entry.Offset);
                        break;
                    }
                }
            }

            if (needsNullCheck)
            {
                numAddrUses++;
            }

            if (numAddrUses > 1)
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
                    unsigned addrLcl =
                        m_compiler->lvaGrabTemp(true DEBUGARG("Spilling address for field-by-field copy"));
                    statements->AddStatement(m_compiler->gtNewTempStore(addrLcl, addr));
                    addr = m_compiler->gtNewLclvNode(addrLcl, addr->TypeGet());
                }
            }
        }

        // Create helper types to create accesses.
        LocationAccess* indirAccess = nullptr;
        LocationAccess  storeAccess;

        if (m_store->OperIs(GT_STORE_BLK))
        {
            storeAccess.InitializeIndir(addr, addrBaseOffs, addrBaseOffsFldSeq, indirFlags, numAddrUses);
            indirAccess = &storeAccess;
        }
        else
        {
            storeAccess.InitializeLocal(m_store->AsLclVarCommon());
        }

        LocationAccess srcAccess;

        if (m_src->OperIs(GT_BLK))
        {
            srcAccess.InitializeIndir(addr, addrBaseOffs, addrBaseOffsFldSeq, indirFlags, numAddrUses);
            indirAccess = &srcAccess;
        }
        else
        {
            srcAccess.InitializeLocal(m_src->AsLclVarCommon());
        }

        if (needsNullCheck)
        {
            assert(indirAccess != nullptr);
            GenTree* nullCheck = indirAccess->CreateRead(0, TYP_BYTE, m_compiler);
            statements->AddStatement(nullCheck);
        }

        if (handleRemainderFirst)
        {
            CopyRemainder(storeAccess, srcAccess, remainderStrategy, statements);

            if (m_src->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                // We will introduce uses of the source below so this struct
                // copy is no longer the last use if it was before.
                m_src->gtFlags &= ~GTF_VAR_DEATH;
            }
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
                m_replacer->ClearNeedsReadBack(*entry.ToReplacement);
                m_replacer->SetNeedsWriteBack(*entry.ToReplacement);
            }

            if (CanSkipEntry(entry, dstDeaths, remainderStrategy DEBUGARG(/* dump */ true)))
            {
                continue;
            }

            GenTree* src;
            if (entry.FromReplacement != nullptr)
            {
                src = m_compiler->gtNewLclvNode(entry.FromReplacement->LclNum, entry.Type);

                if (entry.FromReplacement != nullptr)
                {
                    AggregateInfo* srcAgg   = m_aggregates.Lookup(m_src->AsLclVarCommon()->GetLclNum());
                    Replacement*   firstRep = srcAgg->Replacements.data();
                    assert((entry.FromReplacement >= firstRep) &&
                           (entry.FromReplacement < (firstRep + srcAgg->Replacements.size())));
                    size_t replacementIndex = entry.FromReplacement - firstRep;
                    if (srcDeaths.IsReplacementDying((unsigned)replacementIndex))
                    {
                        src->gtFlags |= GTF_VAR_DEATH;
                        m_replacer->CheckForwardSubForLastUse(entry.FromReplacement->LclNum);
                    }
                }
            }
            else
            {
                src = srcAccess.CreateRead(entry.Offset, entry.Type, m_compiler);
            }

            GenTree* store;
            if (entry.ToReplacement != nullptr)
            {
                store = m_compiler->gtNewStoreLclVarNode(entry.ToReplacement->LclNum, src);
            }
            else
            {
                store = storeAccess.CreateStore(entry.Offset, entry.Type, src, m_compiler);
            }

            statements->AddStatement(store);
        }

        if (!handleRemainderFirst)
        {
            CopyRemainder(storeAccess, srcAccess, remainderStrategy, statements);
        }

        INDEBUG(storeAccess.CheckFullyUsed());
        INDEBUG(srcAccess.CheckFullyUsed());
    }

    //------------------------------------------------------------------------
    // CanSkipEntry:
    //   Check if the specified entry can be skipped because it is writing to a
    //   dead replacement or because the remainder would handle it anyway.
    //
    // Parameters:
    //   entry             - The init/copy entry
    //   deaths            - Liveness information for the destination; only valid if m_dstInvolvedReplacements is true.
    //   remainderStrategy - The strategy we are using for the remainder
    //   dump              - Whether to JITDUMP decisions made
    //
    bool CanSkipEntry(const Entry&             entry,
                      const StructDeaths&      deaths,
                      const RemainderStrategy& remainderStrategy DEBUGARG(bool dump = false))
    {
        if (entry.ToReplacement != nullptr)
        {
            // Check if this entry is dying anyway.
            assert(m_dstInvolvesReplacements);

            AggregateInfo* agg = m_aggregates.Lookup(m_store->AsLclVarCommon()->GetLclNum());
            assert((agg != nullptr) && (agg->Replacements.size() > 0));
            Replacement* firstRep = agg->Replacements.data();
            assert((entry.ToReplacement >= firstRep) && (entry.ToReplacement < (firstRep + agg->Replacements.size())));

            size_t replacementIndex = entry.ToReplacement - firstRep;
            if (deaths.IsReplacementDying((unsigned)replacementIndex))
            {
#ifdef DEBUG
                if (dump)
                {
                    JITDUMP("  Skipping def of V%02u (%s); it is dying\n", entry.ToReplacement->LclNum,
                            entry.ToReplacement->Description);
                }
#endif

                return true;
            }
        }
        else
        {
            // If the destination has replacements we still have usable
            // liveness information for the remainder. This case happens if the
            // source was also promoted.
            if (m_dstInvolvesReplacements && deaths.IsRemainderDying())
            {
#ifdef DEBUG
                if (dump)
                {
                    JITDUMP("  Skipping write to dst+%03u; it is the remainder and the remainder is dying\n",
                            entry.Offset);
                }
#endif

                return true;
            }
        }

        if (entry.FromReplacement != nullptr)
        {
            // Check if the remainder is going to handle it.
            if ((remainderStrategy.Type == RemainderStrategy::FullBlock) && !entry.FromReplacement->NeedsWriteBack &&
                (entry.ToReplacement == nullptr))
            {
#ifdef DEBUG
                if (dump)
                {
                    JITDUMP("  Skipping dst+%03u <- V%02u (%s); it is up-to-date in its struct local and will be "
                            "handled as part of the remainder\n",
                            entry.Offset, entry.FromReplacement->LclNum, entry.FromReplacement->Description);
                }
#endif

                return true;
            }
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
            LclVarDsc*           dsc    = m_compiler->lvaGetDesc(lclNum);
            if (dsc->IsAddressExposed())
            {
                // Address could be pointing to itself
                return false;
            }

            // If we aren't writing a local here then since the address is not
            // exposed it cannot change.
            if (!m_store->OperIsLocalStore())
            {
                return true;
            }

            // Otherwise it could still be possible that the address is part of
            // the struct we're writing.
            unsigned dstLclNum = m_store->AsLclVarCommon()->GetLclNum();
            if ((lclNum == dstLclNum) || (dsc->lvIsStructField && (dsc->lvParentLcl == dstLclNum)))
            {
                return false;
            }

            // It could also be one of the replacement locals we're going to write.
            for (int i = 0; i < m_entries.Height(); i++)
            {
                const Entry& entry = m_entries.BottomRef(i);
                if ((entry.ToReplacement != nullptr) && (entry.ToReplacement->LclNum == lclNum))
                {
                    return false;
                }
            }

            return true;
        }

        return addrNode->IsInvariant();
    }

    //------------------------------------------------------------------------
    // RemainderOverwritesDestinationWithStaleBits:
    //   Check if handling the remainder is going to write stale bits to the
    //   destination.
    //
    // Parameters:
    //   remainderStrategy - The remainder strategy
    //   dstDeaths         - Destination liveness
    //
    // Returns:
    //   True if so.
    //
    // Remarks:
    //   We usually prefer to write the remainder last as CQ analysis shows
    //   that to be most beneficial. However, if we do that we may overwrite
    //   the destination with stale bits. This occurs if the source has
    //   replacements. Handling the remainder copies from the source struct
    //   local, but the up-to-date values may be in its replacement locals. So
    //   we must take care to write the replacement locals _after_ the
    //   remainder has been written.
    //
    bool RemainderOverwritesDestinationWithStaleBits(const RemainderStrategy& remainderStrategy,
                                                     const StructDeaths&      dstDeaths)
    {
        if (!m_srcInvolvesReplacements)
        {
            return false;
        }

        switch (remainderStrategy.Type)
        {
            case RemainderStrategy::FullBlock:
                return true;
            case RemainderStrategy::Primitive:
                for (int i = 0; i < m_entries.Height(); i++)
                {
                    const Entry& entry = m_entries.BottomRef(i);
                    if (entry.Offset + genTypeSize(entry.Type) <= remainderStrategy.PrimitiveOffset)
                    {
                        // Entry ends before remainder starts
                        continue;
                    }

                    // Remainder ends before entry starts
                    if (remainderStrategy.PrimitiveOffset + genTypeSize(remainderStrategy.PrimitiveType) <=
                        entry.Offset)
                    {
                        continue;
                    }

                    // Are we even going to write the entry?
                    if (!CanSkipEntry(entry, dstDeaths, remainderStrategy))
                    {
                        // Yep, so we need to be careful.
                        return true;
                    }
                }

                // No entry overlaps.
                return false;
            default:
                return false;
        }
    }

    // Helper class to create derived accesses off of a location: either a
    // local, or as indirections off of an address.
    class LocationAccess
    {
        GenTreeLclVarCommon* m_local              = nullptr;
        GenTree*             m_addr               = nullptr;
        target_ssize_t       m_addrBaseOffs       = 0;
        FieldSeq*            m_addrBaseOffsFldSeq = nullptr;
        GenTreeFlags         m_indirFlags         = GTF_EMPTY;
        int                  m_numUsesLeft        = -1;

    public:
        //------------------------------------------------------------------------
        // InitializeIndir:
        //   Initialize this to represent an indirection.
        //
        // Parameters:
        //   addr               - The address of the indirection
        //   addrBaseOffs       - Base offset to add on top of the address.
        //   addrBaseOffsFldSeq - Field sequence for the base offset
        //   indirFlags         - Indirection flags to add to created accesses
        //   numExpectedUses    - Number of derived indirections that are expected to be created.
        //
        void InitializeIndir(GenTree*       addr,
                             target_ssize_t addrBaseOffs,
                             FieldSeq*      addrBaseOffsFldSeq,
                             GenTreeFlags   indirFlags,
                             int            numExpectedUses)
        {
            m_addr               = addr;
            m_addrBaseOffs       = addrBaseOffs;
            m_addrBaseOffsFldSeq = addrBaseOffsFldSeq;
            m_indirFlags         = indirFlags;
            m_numUsesLeft        = numExpectedUses;
        }

        //------------------------------------------------------------------------
        // InitializeLocal:
        //   Initialize this to represent a local.
        //
        // Parameters:
        //   local - The local
        //
        void InitializeLocal(GenTreeLclVarCommon* local)
        {
            m_local = local;
        }

        //------------------------------------------------------------------------
        // CreateRead:
        //   Create a read from this location.
        //
        // Parameters:
        //   offs - Offset
        //   type - Type of store
        //   comp - Compiler instance
        //
        // Returns:
        //   IR node to perform the read.
        //
        GenTree* CreateRead(unsigned offs, var_types type, Compiler* comp)
        {
            if (m_addr != nullptr)
            {
                GenTreeIndir* indir = comp->gtNewIndir(type, GrabAddress(offs, comp), GetIndirFlags(type));
                return indir;
            }

            // Check if the source has a regularly promoted field at this offset.
            unsigned fieldLclNum = FindRegularlyPromotedField(offs, comp);
            if ((fieldLclNum != BAD_VAR_NUM) && (comp->lvaGetDesc(fieldLclNum)->TypeGet() == type))
            {
                return comp->gtNewLclvNode(fieldLclNum, type);
            }

            GenTreeLclFld* fld = comp->gtNewLclFldNode(m_local->GetLclNum(), type, m_local->GetLclOffs() + offs);
            comp->lvaSetVarDoNotEnregister(m_local->GetLclNum() DEBUGARG(DoNotEnregisterReason::LocalField));
            return fld;
        }

        //------------------------------------------------------------------------
        // CreateStore:
        //   Create a store to this location.
        //
        // Parameters:
        //   offs - Offset
        //   type - Type of store
        //   src  - Source value
        //   comp - Compiler instance
        //
        // Returns:
        //   IR node to perform the store.
        //
        GenTree* CreateStore(unsigned offs, var_types type, GenTree* src, Compiler* comp)
        {
            if (m_addr != nullptr)
            {
                GenTreeIndir* indir = comp->gtNewStoreIndNode(type, GrabAddress(offs, comp), src, GetIndirFlags(type));
                return indir;
            }

            unsigned fieldLclNum = FindRegularlyPromotedField(offs, comp);
            if ((fieldLclNum != BAD_VAR_NUM) && (comp->lvaGetDesc(fieldLclNum)->TypeGet() == type))
            {
                return comp->gtNewStoreLclVarNode(fieldLclNum, src);
            }

            GenTreeLclFld* fld =
                comp->gtNewStoreLclFldNode(m_local->GetLclNum(), type, m_local->GetLclOffs() + offs, src);
            comp->lvaSetVarDoNotEnregister(m_local->GetLclNum() DEBUGARG(DoNotEnregisterReason::LocalField));
            return fld;
        }

        //------------------------------------------------------------------------
        // FindRegularlyPromotedField:
        //   Find the local number of a regularly promoted field at a specified offset.
        //
        // Parameters:
        //   offs - offset
        //   comp - Compiler instance
        //
        // Returns:
        //   Local number, or BAD_VAR_NUM if this is not a local or it doesn't
        //   have a regularly promoted field at the specified offset.
        //
        unsigned FindRegularlyPromotedField(unsigned offs, Compiler* comp)
        {
            if (m_local == nullptr)
            {
                return BAD_VAR_NUM;
            }

            LclVarDsc* lclDsc  = comp->lvaGetDesc(m_local);
            unsigned   lclOffs = m_local->GetLclOffs() + offs;

            if (!lclDsc->lvPromoted)
            {
                return BAD_VAR_NUM;
            }

            return comp->lvaGetFieldLocal(lclDsc, lclOffs);
        }

        //------------------------------------------------------------------------
        // GrabAddress:
        //   Create a derived access of the address at a specified offset. Only
        //   valid if this represents an indirection. This will decrement the
        //   number of expected derived accesses that can be created.
        //
        // Parameters:
        //   offs - offset
        //   comp - Compiler instance
        //
        // Returns:
        //   Node computing the address.
        //
        GenTree* GrabAddress(unsigned offs, Compiler* comp)
        {
            assert((m_addr != nullptr) && (m_numUsesLeft > 0));
            m_numUsesLeft--;

            GenTree* addrUse;
            if (m_numUsesLeft == 0)
            {
                // Last use of the address, reuse the node.
                addrUse = m_addr;
            }
            else
            {
                addrUse = comp->gtCloneExpr(m_addr);
            }

            target_ssize_t fullOffs = m_addrBaseOffs + (target_ssize_t)offs;
            if ((fullOffs != 0) || (m_addrBaseOffsFldSeq != nullptr))
            {
                GenTreeIntCon* offsetNode = comp->gtNewIconNode(fullOffs, TYP_I_IMPL);
                offsetNode->gtFieldSeq    = m_addrBaseOffsFldSeq;

                var_types addrType = varTypeIsGC(addrUse) ? TYP_BYREF : TYP_I_IMPL;
                addrUse            = comp->gtNewOperNode(GT_ADD, addrType, addrUse, offsetNode);
            }

            return addrUse;
        }

#ifdef DEBUG
        //------------------------------------------------------------------------
        // CheckFullyUsed:
        //   If this is an indirection, verify that it was accessed exactly the
        //   expected number of times.
        //
        void CheckFullyUsed()
        {
            assert((m_addr == nullptr) || (m_numUsesLeft == 0));
        }
#endif

    private:
        //------------------------------------------------------------------------
        // GetIndirFlags:
        //   Get the flags to set on a new indir.
        //
        // Parameters:
        //   type - Type of the indirection
        //
        // Returns:
        //   Flags to set.
        //
        GenTreeFlags GetIndirFlags(var_types type)
        {
            if (genTypeSize(type) == 1)
            {
                return m_indirFlags & ~GTF_IND_UNALIGNED;
            }

            return m_indirFlags;
        }
    };

    //------------------------------------------------------------------------
    // CopyRemainder:
    //   Create IR to copy the remainder.
    //
    // Parameters:
    //   storeAccess       - Helper class to create derived stores
    //   srcAccess         - Helper class to create derived source accesses
    //   remainderStrategy - The strategy to generate IR for
    //   statements        - List to add IR to.
    //
    void CopyRemainder(LocationAccess&             storeAccess,
                       LocationAccess&             srcAccess,
                       const RemainderStrategy&    remainderStrategy,
                       DecompositionStatementList* statements)
    {
        if (remainderStrategy.Type == RemainderStrategy::FullBlock)
        {
            // We will reuse the existing block op. Rebase the address off of the new local we created.
            if (m_src->OperIs(GT_BLK))
            {
                m_src->AsIndir()->Addr() = srcAccess.GrabAddress(0, m_compiler);
            }
            else if (m_store->OperIs(GT_STORE_BLK))
            {
                m_store->AsIndir()->Addr() = storeAccess.GrabAddress(0, m_compiler);
            }

            statements->AddStatement(m_store);
        }
        else if (remainderStrategy.Type == RemainderStrategy::Primitive)
        {
            var_types primitiveType = remainderStrategy.PrimitiveType;
            // The remainder might match a regularly promoted field exactly. If
            // it does then use the promoted field's type so we can create a
            // direct access.
            unsigned srcPromField = srcAccess.FindRegularlyPromotedField(remainderStrategy.PrimitiveOffset, m_compiler);
            unsigned storePromField =
                storeAccess.FindRegularlyPromotedField(remainderStrategy.PrimitiveOffset, m_compiler);

            if ((srcPromField != BAD_VAR_NUM) || (storePromField != BAD_VAR_NUM))
            {
                var_types regPromFieldType =
                    m_compiler->lvaGetDesc(srcPromField != BAD_VAR_NUM ? srcPromField : storePromField)->TypeGet();
                if (genTypeSize(regPromFieldType) == genTypeSize(primitiveType))
                {
                    primitiveType = regPromFieldType;
                }
            }

            GenTree* src   = srcAccess.CreateRead(remainderStrategy.PrimitiveOffset, primitiveType, m_compiler);
            GenTree* store = storeAccess.CreateStore(remainderStrategy.PrimitiveOffset, primitiveType, src, m_compiler);
            statements->AddStatement(store);
        }
    }
};

//------------------------------------------------------------------------
// gtPeelOffsets: Peel all ADD(addr, CNS_INT(x)) nodes off the specified address
// node and return the base node and sum of offsets peeled.
//
// Arguments:
//   addr   - [in, out] The address node.
//   offset - [out] The sum of offset peeled such that ADD(addr, offset) is equivalent to the original addr.
//   fldSeq - [out, optional] The combined field sequence for all the peeled offsets.
//
void Compiler::gtPeelOffsets(GenTree** addr, target_ssize_t* offset, FieldSeq** fldSeq)
{
    assert((*addr)->TypeIs(TYP_I_IMPL, TYP_BYREF, TYP_REF));
    *offset = 0;

    if (fldSeq != nullptr)
    {
        *fldSeq = nullptr;
    }

    while (true)
    {
        if ((*addr)->OperIs(GT_ADD) && !(*addr)->gtOverflow())
        {
            GenTree* op1 = (*addr)->gtGetOp1();
            GenTree* op2 = (*addr)->gtGetOp2();

            if (op2->IsCnsIntOrI() && !op2->AsIntCon()->IsIconHandle())
            {
                assert(op2->TypeIs(TYP_I_IMPL));
                GenTreeIntCon* intCon = op2->AsIntCon();
                *offset += (target_ssize_t)intCon->IconValue();

                if (fldSeq != nullptr)
                {
                    *fldSeq = m_fieldSeqStore->Append(*fldSeq, intCon->gtFieldSeq);
                }

                *addr = op1;
            }
            else if (op1->IsCnsIntOrI() && !op1->AsIntCon()->IsIconHandle())
            {
                assert(op1->TypeIs(TYP_I_IMPL));
                GenTreeIntCon* intCon = op1->AsIntCon();
                *offset += (target_ssize_t)intCon->IconValue();

                if (fldSeq != nullptr)
                {
                    *fldSeq = m_fieldSeqStore->Append(intCon->gtFieldSeq, *fldSeq);
                }

                *addr = op2;
            }
            else
            {
                break;
            }
        }
        else if ((*addr)->OperIs(GT_LEA))
        {
            GenTreeAddrMode* addrMode = (*addr)->AsAddrMode();
            if (addrMode->HasIndex())
            {
                break;
            }

            *offset += (target_ssize_t)addrMode->Offset();
            *addr = addrMode->Base();
        }
        else
        {
            break;
        }
    }
}

// HandleStructStore:
//   Handle a store that may be between struct locals with replacements.
//
// Parameters:
//   use  - The store's use
//   user - The store's user
//
void ReplaceVisitor::HandleStructStore(GenTree** use, GenTree* user)
{
    GenTree* store = *use;

    assert(store->TypeIs(TYP_STRUCT));

    GenTree*             src    = store->Data()->gtEffectiveVal();
    GenTreeLclVarCommon* dstLcl = store->OperIsLocalStore() ? store->AsLclVarCommon() : nullptr;
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

    JITDUMP("Processing block operation [%06u] that involves replacements\n", Compiler::dspTreeID(store));

    if (src->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK) || src->IsConstInitVal())
    {
        DecompositionStatementList result;
        EliminateCommasInBlockOp(store, &result);

        DecompositionPlan plan(m_promotion, this, m_aggregates, m_liveness, store, src, dstInvolvesReplacements,
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
                    ClearNeedsWriteBack(*dstFirstRep);
                }

                SetNeedsReadBack(*dstFirstRep);

                plan.MarkNonRemainderUseOfStructLocal();
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
                        ClearNeedsWriteBack(*dstLastRep);
                    }

                    SetNeedsReadBack(*dstLastRep);

                    plan.MarkNonRemainderUseOfStructLocal();
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
                    ClearNeedsWriteBack(*srcFirstRep);
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
                        ClearNeedsWriteBack(*srcLastRep);
                    }

                    srcEndRep--;
                }
            }
        }

        if (src->IsConstInitVal())
        {
            InitFields(store->AsLclVarCommon(), dstFirstRep, dstEndRep, &plan);
        }
        else
        {
            CopyBetweenFields(store, dstFirstRep, dstEndRep, src, srcFirstRep, srcEndRep, &result, &plan);
        }

        plan.Finalize(&result);

        *use          = result.ToCommaTree(m_compiler);
        m_madeChanges = true;
    }
    else
    {
        if (store->Data()->OperIs(GT_LCL_VAR, GT_LCL_FLD))
        {
            GenTreeLclVarCommon* srcLcl = store->Data()->AsLclVarCommon();
            unsigned             size   = srcLcl->GetLayout(m_compiler)->GetSize();
            WriteBackBeforeUse(&store->Data(), srcLcl->GetLclNum(), srcLcl->GetLclOffs(), size);
        }

        if (store->OperIsLocalStore())
        {
            GenTreeLclVarCommon* lclStore = store->AsLclVarCommon();
            unsigned             size     = lclStore->GetLayout(m_compiler)->GetSize();
            MarkForReadBack(lclStore, size DEBUGARG("cannot decompose store"));
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
    AggregateInfo* agg = m_aggregates.Lookup(lcl->GetLclNum());
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
//   store  - The block op
//   result - Statement list to add resulting statements to.
//
// Remarks:
//   Works similarly to MorphInitBlockHelper::EliminateCommas.
//
void ReplaceVisitor::EliminateCommasInBlockOp(GenTree* store, DecompositionStatementList* result)
{
    bool     any = false;
    GenTree* src = store->Data();

    if (store->IsReverseOp())
    {
        while (src->OperIs(GT_COMMA))
        {
            result->AddStatement(src->gtGetOp1());
            src = src->gtGetOp2();
            any = true;
        }
    }
    else
    {
        if (store->OperIsIndir() && src->OperIs(GT_COMMA))
        {
            GenTree* addr = store->gtGetOp1();
            // Note that GTF_GLOB_REF is not up to date here, hence we need a tree walk to find address exposed locals.
            if (((addr->gtFlags & GTF_ALL_EFFECT) != 0) || (((src->gtFlags & GTF_ASG) != 0) && !addr->IsInvariant()) ||
                m_compiler->gtHasAddressExposedLocals(addr))
            {
                unsigned dstAddrLclNum = m_compiler->lvaGrabTemp(true DEBUGARG("Block morph store addr"));

                result->AddStatement(m_compiler->gtNewTempStore(dstAddrLclNum, addr));
                store->AsIndir()->Addr() = m_compiler->gtNewLclvNode(dstAddrLclNum, genActualType(addr));
                m_compiler->gtUpdateNodeSideEffects(store);
                m_madeChanges = true;
                any           = true;
            }
        }

        while (src->OperIs(GT_COMMA))
        {
            result->AddStatement(src->gtGetOp1());
            src = src->gtGetOp2();
            any = true;
        }
    }

    if (any)
    {
        store->Data() = src;
        m_compiler->gtUpdateNodeSideEffects(store);
        m_madeChanges = true;
    }
}

//------------------------------------------------------------------------
// InitFields:
//   Add entries into the plan specifying which replacements can be
//   directly inited, and mark the other ones as requiring read back.
//
// Parameters:
//   dstStore - Store into the destination local that involves replacement.
//   firstRep - The first replacement.
//   endRep   - End of the replacements.
//   plan     - Decomposition plan to add initialization entries into.
//
void ReplaceVisitor::InitFields(GenTreeLclVarCommon* dstStore,
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
            ClearNeedsWriteBack(*rep);
            SetNeedsReadBack(*rep);
            plan->MarkNonRemainderUseOfStructLocal();
            continue;
        }

        JITDUMP("  Init V%02u (%s)%s\n", rep->LclNum, rep->Description, LastUseString(dstStore, rep));
        plan->InitReplacement(rep, rep->Offset - dstStore->GetLclOffs());
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
    AggregateInfo* agg    = m_aggregates.Lookup(lcl->GetLclNum());
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
//   store       - Store node
//   dstFirstRep - First replacement of the destination or nullptr if destination is not a promoted local.
//   dstEndRep   - One past last replacement of the destination.
//   src         - Source node
//   srcFirstRep - First replacement of the source or nullptr if source is not a promoted local.
//   srcEndRep   - One past last replacement of the source.
//   statements  - Statement list to add potential "init" statements to.
//   plan        - Data structure that tracks the specific copies to be done.
//
void ReplaceVisitor::CopyBetweenFields(GenTree*                    store,
                                       Replacement*                dstFirstRep,
                                       Replacement*                dstEndRep,
                                       GenTree*                    src,
                                       Replacement*                srcFirstRep,
                                       Replacement*                srcEndRep,
                                       DecompositionStatementList* statements,
                                       DecompositionPlan*          plan)
{
    assert(src->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK));

    GenTreeLclVarCommon* dstLcl      = store->OperIsLocalStore() ? store->AsLclVarCommon() : nullptr;
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
            ClearNeedsReadBack(*srcRep);
            assert(!srcRep->NeedsWriteBack);
        }

        if ((dstRep < dstEndRep) && (srcRep < srcEndRep))
        {
            if (srcRep->Offset - srcBaseOffs + genTypeSize(srcRep->AccessType) <= dstRep->Offset - dstBaseOffs)
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

            if (dstRep->Offset - dstBaseOffs + genTypeSize(dstRep->AccessType) <= srcRep->Offset - srcBaseOffs)
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
            plan->CopyToReplacement(dstRep, offs);
            JITDUMP("  V%02u (%s)%s <- src+%03u\n", dstRep->LclNum, dstRep->Description, LastUseString(dstLcl, dstRep),
                    offs);
            dstRep++;
        }
        else
        {
            unsigned offs = srcRep->Offset - srcBaseOffs;
            plan->CopyFromReplacement(srcRep, offs);
            JITDUMP("  dst+%03u <- V%02u (%s)%s\n", offs, srcRep->LclNum, srcRep->Description,
                    LastUseString(srcLcl, srcRep));
            srcRep++;
        }
    }
}
