#include "jitpch.h"
#include "promotion.h"
#include "jitstd/algorithm.h"

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
            tree = comp->gtNewOperNode(GT_COMMA, tree->TypeGet(), cur, tree);
        }

        return tree;
    }
};

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

    Replacement* dstFirstRep             = nullptr;
    Replacement* dstEndRep               = nullptr;
    bool         dstInvolvesReplacements = asg->gtGetOp1()->OperIs(GT_LCL_VAR, GT_LCL_FLD) &&
                                   OverlappingReplacements(dst->AsLclVarCommon(), &dstFirstRep, &dstEndRep);
    Replacement* srcFirstRep             = nullptr;
    Replacement* srcEndRep               = nullptr;
    bool         srcInvolvesReplacements = asg->gtGetOp2()->OperIs(GT_LCL_VAR, GT_LCL_FLD) &&
                                   OverlappingReplacements(src->AsLclVarCommon(), &srcFirstRep, &srcEndRep);

    if (!dstInvolvesReplacements && !srcInvolvesReplacements)
    {
        return;
    }

    JITDUMP("Processing block operation [%06u] that involves replacements\n", Compiler::dspTreeID(asg));

    if (dstInvolvesReplacements && (src->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK) || src->IsConstInitVal()))
    {
        DecompositionStatementList result;
        EliminateCommasInBlockOp(asg, &result);

        if (dstInvolvesReplacements && srcInvolvesReplacements)
        {
            JITDUMP("Copy [%06u] is between two physically promoted locals with replacements\n",
                    Compiler::dspTreeID(asg));
            JITDUMP("*** Conservative: Phys<->phys copies not yet supported; inserting conservative write-back\n");
            for (Replacement* rep = srcFirstRep; rep < srcEndRep; rep++)
            {
                if (rep->NeedsWriteBack)
                {
                    result.AddStatement(
                        Promotion::CreateWriteBack(m_compiler, src->AsLclVarCommon()->GetLclNum(), *rep));
                    rep->NeedsWriteBack = false;
                }
            }

            srcInvolvesReplacements = false;
        }

        if (dstInvolvesReplacements)
        {
            GenTreeLclVarCommon* dstLcl     = dst->AsLclVarCommon();
            unsigned             dstLclOffs = dstLcl->GetLclOffs();
            unsigned             dstLclSize = dstLcl->GetLayout(m_compiler)->GetSize();

            if (dstFirstRep->Offset < dstLclOffs)
            {
                JITDUMP("*** Block operation partially overlaps with %s. Write and read-backs are necessary.\n",
                        dstFirstRep->Description);
                // The value of the replacement will be partially assembled from its old value and this struct
                // operation.
                // We accomplish this by an initial write back, the struct copy, followed by a later read back.
                // TODO-CQ: This is very expensive and unreflected in heuristics, but it is also very rare.
                result.AddStatement(Promotion::CreateWriteBack(m_compiler, dstLcl->GetLclNum(), *dstFirstRep));

                dstFirstRep->NeedsWriteBack = false;
                dstFirstRep->NeedsReadBack  = true;
                dstFirstRep++;
            }

            if (dstEndRep > dstFirstRep)
            {
                Replacement* dstLastRep = dstEndRep - 1;
                if (dstLastRep->Offset + genTypeSize(dstLastRep->AccessType) > dstLclOffs + dstLclSize)
                {
                    JITDUMP("*** Block operation partially overlaps with %s. Write and read-backs are necessary.\n",
                            dstLastRep->Description);
                    result.AddStatement(Promotion::CreateWriteBack(m_compiler, dstLcl->GetLclNum(), *dstLastRep));

                    dstLastRep->NeedsWriteBack = false;
                    dstLastRep->NeedsReadBack  = true;
                    dstEndRep--;
                }
            }

            if (src->IsConstInitVal())
            {
                GenTree* cns = src->OperIsInitVal() ? src->gtGetOp1() : src;
                InitFieldByField(dstFirstRep, dstEndRep, static_cast<unsigned char>(cns->AsIntCon()->IconValue()),
                                 &result);
            }
            else
            {
                CopyIntoFields(dstFirstRep, dstEndRep, dstLcl, src, &result);
            }

            // At this point all replacements that have Handled = true contain their correct value.
            // Check if these cover the entire block operation.
            unsigned prevEnd = dstLclOffs;
            bool     covered = true;

            for (Replacement* rep = dstFirstRep; rep < dstEndRep; rep++)
            {
                if (!rep->Handled)
                {
                    covered = false;
                    break;
                }

                assert(rep->Offset >= prevEnd);
                if (rep->Offset != prevEnd)
                {
                    // Uncovered hole from [lastEnd..rep->Offset).
                    // TODO-CQ: In many cases it's more efficient to "plug" the holes. However,
                    // it is made more complicated by the fact that the holes can contain GC pointers in them and
                    // we cannot (yet) represent custom class layouts with GC pointers in them.
                    // TODO-CQ: Many of these cases are just padding. We should handle structs with insignificant
                    // padding here.
                    covered = false;
                    break;
                }

                prevEnd = rep->Offset + genTypeSize(rep->AccessType);
            }

            covered &= prevEnd == dstLclOffs + dstLclSize;

            if (!covered)
            {
                JITDUMP("Struct operation is not fully covered by replaced fields. Keeping struct operation.\n");
                result.AddStatement(asg);
            }

            // For unhandled replacements, mark that they will require a read back before their next access.
            // Conversely, the replacements we handled above are now up to date and should not be read back.
            // We also keep the invariant that Replacement::Handled == false, so reset it here as well.

            for (Replacement* rep = dstFirstRep; rep < dstEndRep; rep++)
            {
                rep->NeedsReadBack  = !rep->Handled;
                rep->NeedsWriteBack = rep->Handled;
                rep->Handled        = false;
            }
        }
        else
        {
            assert(srcInvolvesReplacements);
        }

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
    if (m_replacements[lcl->GetLclNum()] == nullptr)
    {
        return false;
    }

    jitstd::vector<Replacement>& replacements = *m_replacements[lcl->GetLclNum()];

    unsigned offs       = lcl->GetLclOffs();
    unsigned size       = lcl->GetLayout(m_compiler)->GetSize();
    size_t   firstIndex = Promotion::BinarySearch<Replacement, &Replacement::Offset>(replacements, offs);
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

    assert(replacements[firstIndex].Overlaps(offs, size));
    *firstReplacement = &replacements[firstIndex];

    if (endReplacement != nullptr)
    {
        size_t lastIndex = Promotion::BinarySearch<Replacement, &Replacement::Offset>(replacements, offs + size);
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
        if (lhs->OperIsUnary() && rhs->OperIs(GT_COMMA))
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
// InitFieldByField:
//   Initialize the specified replacements with a specified pattern.
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
void ReplaceVisitor::InitFieldByField(Replacement*                firstRep,
                                      Replacement*                endRep,
                                      unsigned char               initVal,
                                      DecompositionStatementList* result)
{
    int64_t initPattern = int64_t(initVal) * 0x0101010101010101LL;

    for (Replacement* rep = firstRep; rep < endRep; rep++)
    {
        assert(!rep->Handled);

        GenTree* srcVal;
        if ((initPattern != 0) && (varTypeIsSIMD(rep->AccessType) || varTypeIsGC(rep->AccessType)))
        {
            // Leave unhandled, we will do this via a read back on the next access.
            continue;
        }

        switch (rep->AccessType)
        {
            case TYP_BOOL:
            case TYP_BYTE:
            case TYP_UBYTE:
            case TYP_SHORT:
            case TYP_USHORT:
            case TYP_INT:
            {
                int64_t mask = (int64_t(1) << (genTypeSize(rep->AccessType) * 8)) - 1;
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
                srcVal = m_compiler->gtNewZeroConNode(rep->AccessType);
                break;
            }
            default:
                unreached();
        }

        GenTree* lcl = m_compiler->gtNewLclvNode(rep->LclNum, rep->AccessType);
        GenTree* asg = m_compiler->gtNewAssignNode(lcl, srcVal);
        result->AddStatement(asg);
        rep->Handled = true;
    }
}

//------------------------------------------------------------------------
// CopyIntoFields:
//   Copy from a specified block source into the specified replacements.
//
// Parameters:
//   firstRep - The first replacement.
//   endRep   - End of the replacements.
//   dst      - Local containing the replacements.
//   src      - The block source.
//   result   - Statement list to add resulting statements to.
//
void ReplaceVisitor::CopyIntoFields(Replacement*                firstRep,
                                    Replacement*                endRep,
                                    GenTreeLclVarCommon*        dst,
                                    GenTree*                    src,
                                    DecompositionStatementList* result)
{
    assert(src->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_BLK));

    GenTreeFlags indirFlags = GTF_EMPTY;
    if (src->OperIs(GT_BLK))
    {
        GenTree* addr = src->AsIndir()->Addr();

        if (addr->OperIsLocal() && (addr->AsLclVarCommon()->GetLclNum() != dst->GetLclNum()))
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
            // TODO-CQ: Avoid this local if we only use the address once? A
            // bit complicated since our caller may use the address too.
            unsigned addrLcl = m_compiler->lvaGrabTemp(true DEBUGARG("Spilling address for field-by-field copy"));
            result->AddStatement(m_compiler->gtNewTempAssign(addrLcl, addr));
            src->AsUnOp()->gtOp1 = m_compiler->gtNewLclvNode(addrLcl, addr->TypeGet());
        }

        indirFlags = src->gtFlags & (GTF_IND_VOLATILE | GTF_IND_NONFAULTING | GTF_IND_UNALIGNED | GTF_IND_INITCLASS);
    }

    LclVarDsc* srcDsc = src->OperIs(GT_LCL_VAR, GT_LCL_FLD) ? m_compiler->lvaGetDesc(src->AsLclVarCommon()) : nullptr;

    for (Replacement* rep = firstRep; rep < endRep; rep++)
    {
        assert(!rep->Handled);
        assert(rep->Offset >= dst->GetLclOffs());

        unsigned srcOffs = rep->Offset - dst->GetLclOffs();

        GenTree* dstLcl = m_compiler->gtNewLclvNode(rep->LclNum, rep->AccessType);
        GenTree* srcFld = nullptr;
        if (srcDsc != nullptr)
        {
            srcOffs += src->AsLclVarCommon()->GetLclOffs();

            if (srcDsc->lvPromoted)
            {
                unsigned fieldLcl = m_compiler->lvaGetFieldLocal(srcDsc, srcOffs);

                if ((fieldLcl != BAD_VAR_NUM) && (m_compiler->lvaGetDesc(fieldLcl)->lvType == rep->AccessType))
                {
                    srcFld = m_compiler->gtNewLclvNode(fieldLcl, rep->AccessType);
                }
            }

            if (srcFld == nullptr)
            {
                srcFld = m_compiler->gtNewLclFldNode(src->AsLclVarCommon()->GetLclNum(), rep->AccessType, srcOffs);
                // TODO-CQ: This may be better left as a read back if the
                // source is non-physically promoted.
                m_compiler->lvaSetVarDoNotEnregister(src->AsLclVarCommon()->GetLclNum()
                                                         DEBUGARG(DoNotEnregisterReason::LocalField));
            }

            UpdateEarlyRefCount(srcFld);
        }
        else
        {
            if ((rep == firstRep) && m_compiler->fgIsBigOffset(srcOffs) &&
                m_compiler->fgAddrCouldBeNull(src->AsIndir()->Addr()))
            {
                GenTree*      addrForNullCheck = m_compiler->gtCloneExpr(src->AsIndir()->Addr());
                GenTreeIndir* indir            = m_compiler->gtNewIndir(TYP_BYTE, addrForNullCheck);
                indir->gtFlags |= indirFlags;
                result->AddStatement(indir);
                UpdateEarlyRefCount(addrForNullCheck);
            }

            GenTree* addr = m_compiler->gtCloneExpr(src->AsIndir()->Addr());
            UpdateEarlyRefCount(addr);
            if (srcOffs != 0)
            {
                var_types addrType = varTypeIsGC(addr) ? TYP_BYREF : TYP_I_IMPL;
                addr =
                    m_compiler->gtNewOperNode(GT_ADD, addrType, addr, m_compiler->gtNewIconNode(srcOffs, TYP_I_IMPL));
            }

            GenTree* dstLcl = m_compiler->gtNewLclvNode(rep->LclNum, rep->AccessType);
            srcFld          = m_compiler->gtNewIndir(rep->AccessType, addr, indirFlags);
        }

        result->AddStatement(m_compiler->gtNewAssignNode(dstLcl, srcFld));
        rep->Handled = true;
    }
}

//------------------------------------------------------------------------
// UpdateEarlyRefCount:
//   Update early ref counts if necessary for the specified IR node.
//
// Parameters:
//   candidate - the IR node that may be a local that should have its early ref counts updated.
//
void ReplaceVisitor::UpdateEarlyRefCount(GenTree* candidate)
{
    if (!candidate->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_LCL_ADDR))
    {
        return;
    }

    IncrementRefCount(candidate->AsLclVarCommon()->GetLclNum());

    LclVarDsc* varDsc = m_compiler->lvaGetDesc(candidate->AsLclVarCommon());
    if (varDsc->lvIsStructField)
    {
        IncrementRefCount(varDsc->lvParentLcl);
    }

    if (varDsc->lvPromoted)
    {
        for (unsigned fldLclNum = varDsc->lvFieldLclStart; fldLclNum < varDsc->lvFieldLclStart + varDsc->lvFieldCnt;
             fldLclNum++)
        {
            IncrementRefCount(fldLclNum);
        }
    }
}

//------------------------------------------------------------------------
// IncrementRefCount:
//   Increment the ref count for the specified local.
//
// Parameters:
//   lclNum - the local
//
void ReplaceVisitor::IncrementRefCount(unsigned lclNum)
{
    LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
    varDsc->incLvRefCntSaturating(1, RCS_EARLY);
}
