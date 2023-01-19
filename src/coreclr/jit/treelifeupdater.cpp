// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "treelifeupdater.h"

template <bool ForCodeGen>
TreeLifeUpdater<ForCodeGen>::TreeLifeUpdater(Compiler* compiler)
    : compiler(compiler)
#ifdef DEBUG
    , epoch(compiler->GetCurLVEpoch())
    , oldLife(VarSetOps::MakeEmpty(compiler))
    , oldStackPtrsLife(VarSetOps::MakeEmpty(compiler))
#endif // DEBUG
{
}

//------------------------------------------------------------------------
// UpdateLifeFieldVar: Update live sets for only the given field of a multi-reg LclVar node.
//
// Arguments:
//    lclNode - the GT_LCL_VAR node.
//    multiRegIndex - the index of the field being updated.
//
// Return Value:
//    Returns true iff the variable needs to be spilled.
//
// Notes:
//    This method need only be used when the fields are dying or going live at different times,
//    e.g. when I ready the 0th field/reg of one node and define the 0th field/reg of another
//    before reading the subsequent fields/regs.
//
template <bool ForCodeGen>
bool TreeLifeUpdater<ForCodeGen>::UpdateLifeFieldVar(GenTreeLclVar* lclNode, unsigned multiRegIndex)
{
    LclVarDsc* parentVarDsc = compiler->lvaGetDesc(lclNode);
    assert(parentVarDsc->lvPromoted && (multiRegIndex < parentVarDsc->lvFieldCnt) && lclNode->IsMultiReg() &&
           compiler->lvaEnregMultiRegVars);
    unsigned   fieldVarNum = parentVarDsc->lvFieldLclStart + multiRegIndex;
    LclVarDsc* fldVarDsc   = compiler->lvaGetDesc(fieldVarNum);
    assert(fldVarDsc->lvTracked);
    assert((lclNode->gtFlags & GTF_VAR_USEASG) == 0);

    StoreCurrentLifeForDump();
    bool isBorn  = ((lclNode->gtFlags & GTF_VAR_DEF) != 0);
    bool isDying = !isBorn && lclNode->IsLastUse(multiRegIndex);

    if (isBorn || isDying)
    {
        bool previouslyLive = VarSetOps::IsMember(compiler, compiler->compCurLife, fldVarDsc->lvVarIndex);
        UpdateLifeBit(compiler->compCurLife, fldVarDsc, isBorn, isDying);

        if (ForCodeGen)
        {
            regNumber reg        = lclNode->GetRegNumByIdx(multiRegIndex);
            bool      isInReg    = fldVarDsc->lvIsInReg() && reg != REG_NA;
            bool      isInMemory = !isInReg || fldVarDsc->IsAlwaysAliveInMemory();
            if (isInReg)
            {
                if (isBorn)
                {
                    compiler->codeGen->genUpdateVarReg(fldVarDsc, lclNode, multiRegIndex);
                }
                compiler->codeGen->genUpdateRegLife(fldVarDsc, isBorn, isDying DEBUGARG(lclNode));
            }

            if (isInMemory &&
                VarSetOps::IsMember(compiler, compiler->codeGen->gcInfo.gcTrkStkPtrLcls, fldVarDsc->lvVarIndex))
            {
                UpdateLifeBit(compiler->codeGen->gcInfo.gcVarPtrSetCur, fldVarDsc, isBorn, isDying);
            }

            if (previouslyLive != isBorn)
            {
                compiler->codeGen->getVariableLiveKeeper()->siStartOrCloseVariableLiveRange(fldVarDsc, fieldVarNum,
                                                                                            isBorn, isDying);
            }
        }
    }

    bool spill = false;
    // GTF_SPILL will be set if any registers need to be spilled.
    if (ForCodeGen && ((lclNode->gtFlags & lclNode->GetRegSpillFlagByIdx(multiRegIndex) & GTF_SPILL) != 0))
    {
        if (VarSetOps::IsMember(compiler, compiler->codeGen->gcInfo.gcTrkStkPtrLcls, fldVarDsc->lvVarIndex))
        {
            if (!VarSetOps::IsMember(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur, fldVarDsc->lvVarIndex))
            {
                VarSetOps::AddElemD(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur, fldVarDsc->lvVarIndex);
#ifdef DEBUG
                if (compiler->verbose)
                {
                    printf("\t\t\t\t\t\t\tVar V%02u becoming live\n", fieldVarNum);
                }
#endif // DEBUG
            }
        }

        spill = true;
    }

    DumpLifeDelta();

    return spill;
}

//------------------------------------------------------------------------
// UpdateLifeVar: Update live sets for a given tree.
//
// Arguments:
//    tree       - the tree which affects liveness
//    lclVarTree - the local tree
//
// Notes:
//    Most commonly "tree" and "lclVarTree" will be the same, however,
//    that will not be true for indirect defs ("STOREIND(LCL_ADDR, ...)")
//    and uses ("OBJ(LCL_ADDR)")
//
template <bool ForCodeGen>
void TreeLifeUpdater<ForCodeGen>::UpdateLifeVar(GenTree* tree, GenTreeLclVarCommon* lclVarTree)
{
    assert(lclVarTree->OperIsNonPhiLocal() || lclVarTree->OperIsLocalAddr());

    unsigned int lclNum = lclVarTree->GetLclNum();
    LclVarDsc*   varDsc = compiler->lvaGetDesc(lclNum);

    compiler->compCurLifeTree = tree;

    // By codegen, a struct may not be TYP_STRUCT, so we have to
    // check lvPromoted, for the case where the fields are being
    // tracked.
    if (!varDsc->lvTracked && !varDsc->lvPromoted)
    {
        return;
    }

    StoreCurrentLifeForDump();

    bool isBorn = ((lclVarTree->gtFlags & GTF_VAR_DEF) != 0) && ((lclVarTree->gtFlags & GTF_VAR_USEASG) == 0);

    if (varDsc->lvTracked)
    {
        assert(!varDsc->lvPromoted && !lclVarTree->IsMultiRegLclVar());

        bool isDying = (lclVarTree->gtFlags & GTF_VAR_DEATH) != 0;

        if (isBorn || isDying)
        {
            bool previouslyLive = VarSetOps::IsMember(compiler, compiler->compCurLife, varDsc->lvVarIndex);
            UpdateLifeBit(compiler->compCurLife, varDsc, isBorn, isDying);

            if (ForCodeGen)
            {
                if (isBorn && varDsc->lvIsRegCandidate() && tree->gtHasReg(compiler))
                {
                    compiler->codeGen->genUpdateVarReg(varDsc, tree);
                }

                bool isInReg    = varDsc->lvIsInReg() && (tree->GetRegNum() != REG_NA);
                bool isInMemory = !isInReg || varDsc->IsAlwaysAliveInMemory();
                if (isInReg)
                {
                    compiler->codeGen->genUpdateRegLife(varDsc, isBorn, isDying DEBUGARG(tree));
                }

                if (isInMemory &&
                    VarSetOps::IsMember(compiler, compiler->codeGen->gcInfo.gcTrkStkPtrLcls, varDsc->lvVarIndex))
                {
                    UpdateLifeBit(compiler->codeGen->gcInfo.gcVarPtrSetCur, varDsc, isBorn, isDying);
                }

                if ((isDying != isBorn) && (isBorn != previouslyLive))
                {
                    compiler->codeGen->getVariableLiveKeeper()->siStartOrCloseVariableLiveRange(varDsc, lclNum, isBorn,
                                                                                                isDying);
                }
            }
        }

        if (ForCodeGen && ((lclVarTree->gtFlags & GTF_SPILL) != 0))
        {
            compiler->codeGen->genSpillVar(tree);

            if (VarSetOps::IsMember(compiler, compiler->codeGen->gcInfo.gcTrkStkPtrLcls, varDsc->lvVarIndex))
            {
                if (!VarSetOps::IsMember(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex))
                {
                    VarSetOps::AddElemD(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);
                    JITDUMP("\t\t\t\t\t\t\tVar V%02u becoming live\n", lclNum);
                }
            }
        }
    }
    else if (varDsc->lvPromoted)
    {
        bool isMultiRegLocal = lclVarTree->IsMultiRegLclVar();
#ifdef DEBUG
        if (isMultiRegLocal)
        {
            // We should never have an indirect reference for a multi-reg.
            assert(lclVarTree == tree);
            assert((lclVarTree->gtFlags & GTF_VAR_USEASG) == 0);
        }
#endif

        // TODO: Fields can die even at a def.
        bool isAnyFieldDying =
            isMultiRegLocal ? !isBorn && lclVarTree->HasLastUse() : ((lclVarTree->gtFlags & GTF_VAR_DEATH) != 0);
        if (isBorn || isAnyFieldDying)
        {
            VARSET_TP* deadTrackedFieldVars    = nullptr;
            bool       hasDeadTrackedFieldVars = false;

            // TODO-Review: the code below does not look right. We can have last uses for simple LCL_VARs
            // as well as indirect uses.
            if (!isMultiRegLocal && (tree != lclVarTree) && isAnyFieldDying)
            {
                assert(!isBorn); // GTF_VAR_DEATH only set for non-partial last use.
                hasDeadTrackedFieldVars = compiler->LookupPromotedStructDeathVars(lclVarTree, &deadTrackedFieldVars);
            }

        if (isBorn || isAnyFieldDying)
        {
            unsigned firstFieldVarNum = varDsc->lvFieldLclStart;
            for (unsigned i = 0; i < varDsc->lvFieldCnt; ++i)
            {
                unsigned   fldLclNum = firstFieldVarNum + i;
                LclVarDsc* fldVarDsc = compiler->lvaGetDesc(fldLclNum);
                assert(fldVarDsc->lvIsStructField);
                if (!fldVarDsc->lvTracked)
                {
                    // multi-reg locals are expected to have all fields tracked so that they are register candidates.
                    assert(!isMultiRegLocal);
                    continue;
                }

                // We should never see enregistered fields in a struct local unless
                // IsMultiRegLclVar() returns true.
                assert(isMultiRegLocal || !fldVarDsc->lvIsInReg());

                bool isInReg        = fldVarDsc->lvIsInReg() && (lclVarTree->AsLclVar()->GetRegNumByIdx(i) != REG_NA);
                bool isInMemory     = !isInReg || fldVarDsc->IsAlwaysAliveInMemory();
                bool previouslyLive = VarSetOps::IsMember(compiler, compiler->compCurLife, fldVarDsc->lvVarIndex);

                bool isFieldDying;

                if (isMultiRegLocal)
                {
                    isFieldDying = lclVarTree->IsLastUse(i);
                    // TODO: Remove this condition which disallows marking
                    // some fields as dead even though they are dying when other
                    // fields are defined.
                    if ((isBorn && !isFieldDying) || (!isBorn && isFieldDying))
                    {
                        UpdateLifeBit(compiler->compCurLife, fldVarDsc, isBorn, isFieldDying);
                    }
                }
                else
                {
                    isFieldDying = isAnyFieldDying && (!hasDeadTrackedFieldVars ||
                                                       VarSetOps::IsMember(compiler, *deadTrackedFieldVars, i));
                    UpdateLifeBit(compiler->compCurLife, fldVarDsc, isBorn, isFieldDying);
                }

                if (ForCodeGen)
                {
                    if (isInReg)
                    {
                        if (isBorn)
                        {
                            compiler->codeGen->genUpdateVarReg(fldVarDsc, tree, i);
                        }

                        compiler->codeGen->genUpdateRegLife(fldVarDsc, isBorn, isFieldDying DEBUGARG(tree));
                        // If this was marked for spill genProduceReg should already have spilled it.
                        bool fieldNeedsSpill = ((lclVarTree->gtFlags & GTF_SPILL) != 0) &&
                                               ((lclVarTree->GetRegSpillFlagByIdx(i) & GTF_SPILL) != 0);
                        assert(!fieldNeedsSpill);
                    }

                    if (isInMemory &&
                        VarSetOps::IsMember(compiler, compiler->codeGen->gcInfo.gcTrkStkPtrLcls, fldVarDsc->lvVarIndex))
                    {
                        UpdateLifeBit(compiler->codeGen->gcInfo.gcVarPtrSetCur, fldVarDsc, isBorn, isFieldDying);
                    }

                    if ((isFieldDying != isBorn) && (isBorn != previouslyLive))
                    {
                        compiler->codeGen->getVariableLiveKeeper()->siStartOrCloseVariableLiveRange(fldVarDsc,
                                                                                                    fldLclNum, isBorn,
                                                                                                    isFieldDying);
                    }
                }
            }
        }
    }

    DumpLifeDelta();
}

//------------------------------------------------------------------------
// UpdateLife: Determine whether the tree affects liveness, and update liveness sets accordingly.
//
// Arguments:
//    tree - the tree which effect on liveness is processed.
//
template <bool ForCodeGen>
void TreeLifeUpdater<ForCodeGen>::UpdateLife(GenTree* tree)
{
    assert(compiler->GetCurLVEpoch() == epoch);
    // TODO-Cleanup: We shouldn't really be calling this more than once
    if (tree == compiler->compCurLifeTree)
    {
        return;
    }

    // Note that after lowering, we can see indirect uses and definitions of tracked variables.
    // TODO-Bug: we're not handling calls with return buffers here properly.
    GenTreeLclVarCommon* lclVarTree = nullptr;
    if (tree->OperIsNonPhiLocal())
    {
        lclVarTree = tree->AsLclVarCommon();
    }
    else if (tree->OperIsIndir() && tree->AsIndir()->Addr()->OperIsLocalAddr())
    {
        lclVarTree = tree->AsIndir()->Addr()->AsLclVarCommon();
    }

    if (lclVarTree != nullptr)
    {
        UpdateLifeVar(tree, lclVarTree);
    }
}

//------------------------------------------------------------------------
// UpdateLifeBit: Update a liveness set for a specific local depending on whether it is being born or dying.
//
// Arguments:
//    set - The life set
//    dsc - The local's description
//    isBorn - Whether the local is being born now
//    isDying - Whether the local is dying now
//
template <bool ForCodeGen>
void TreeLifeUpdater<ForCodeGen>::UpdateLifeBit(VARSET_TP& set, LclVarDsc* dsc, bool isBorn, bool isDying)
{
    if (isDying)
    {
        VarSetOps::RemoveElemD(compiler, set, dsc->lvVarIndex);
    }
    else if (isBorn)
    {
        VarSetOps::AddElemD(compiler, set, dsc->lvVarIndex);
    }
}

//------------------------------------------------------------------------
// StoreCurrentLifeForDump: Store current liveness information so that deltas
// can be dumped after potential updates.
//
template <bool ForCodeGen>
void           TreeLifeUpdater<ForCodeGen>::StoreCurrentLifeForDump()
{
#ifdef DEBUG
    if (compiler->verbose)
    {
        VarSetOps::Assign(compiler, oldLife, compiler->compCurLife);

        if (ForCodeGen)
        {
            VarSetOps::Assign(compiler, oldStackPtrsLife, compiler->codeGen->gcInfo.gcVarPtrSetCur);
        }
    }
#endif
}

//------------------------------------------------------------------------
// DumpLifeDelta: Dump the delta of liveness changes that happened since
// StoreCurrentLifeForDump was called.
//
template <bool ForCodeGen>
void           TreeLifeUpdater<ForCodeGen>::DumpLifeDelta()
{
#ifdef DEBUG
    if (compiler->verbose && !VarSetOps::Equal(compiler, oldLife, compiler->compCurLife))
    {
        printf("\t\t\t\t\t\t\tLive vars: ");
        dumpConvertedVarSet(compiler, oldLife);
        printf(" => ");
        dumpConvertedVarSet(compiler, compiler->compCurLife);
        printf("\n");
    }

    if (ForCodeGen && compiler->verbose &&
        !VarSetOps::Equal(compiler, oldStackPtrsLife, compiler->codeGen->gcInfo.gcVarPtrSetCur))
    {
        printf("\t\t\t\t\t\t\tGC vars: ");
        dumpConvertedVarSet(compiler, oldStackPtrsLife);
        printf(" => ");
        dumpConvertedVarSet(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur);
        printf("\n");
    }
#endif // DEBUG
}

template class TreeLifeUpdater<true>;
template class TreeLifeUpdater<false>;
