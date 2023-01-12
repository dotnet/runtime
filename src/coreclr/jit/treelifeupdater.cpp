#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "treelifeupdater.h"

template <bool ForCodeGen>
TreeLifeUpdater<ForCodeGen>::TreeLifeUpdater(Compiler* compiler)
    : compiler(compiler)
    , newLife(VarSetOps::MakeEmpty(compiler))
    , stackVarDeltaSet(VarSetOps::MakeEmpty(compiler))
    , varDeltaSet(VarSetOps::MakeEmpty(compiler))
    , gcTrkStkDeltaSet(VarSetOps::MakeEmpty(compiler))
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
    unsigned fldVarIndex = fldVarDsc->lvVarIndex;
    assert((lclNode->gtFlags & GTF_VAR_USEASG) == 0);

    VarSetOps::Assign(compiler, newLife, compiler->compCurLife);
    bool isBorn  = ((lclNode->gtFlags & GTF_VAR_DEF) != 0);
    bool isDying = !isBorn && lclNode->IsLastUse(multiRegIndex);
    // GTF_SPILL will be set if any registers need to be spilled.
    GenTreeFlags spillFlags = (lclNode->gtFlags & lclNode->GetRegSpillFlagByIdx(multiRegIndex));
    bool         spill      = ((spillFlags & GTF_SPILL) != 0);
    bool         isInMemory = false;

    if (isBorn || isDying)
    {
        if (ForCodeGen)
        {
            regNumber reg     = lclNode->GetRegNumByIdx(multiRegIndex);
            bool      isInReg = fldVarDsc->lvIsInReg() && reg != REG_NA;
            isInMemory        = !isInReg || fldVarDsc->IsAlwaysAliveInMemory();
            if (isInReg)
            {
                if (isBorn)
                {
                    compiler->codeGen->genUpdateVarReg(fldVarDsc, lclNode, multiRegIndex);
                }
                compiler->codeGen->genUpdateRegLife(fldVarDsc, isBorn, isDying DEBUGARG(lclNode));
            }
        }
        // First, update the live set
        if (isDying)
        {
            VarSetOps::RemoveElemD(compiler, newLife, fldVarIndex);
        }
        else
        {
            VarSetOps::AddElemD(compiler, newLife, fldVarIndex);
        }
    }

    if (!VarSetOps::Equal(compiler, compiler->compCurLife, newLife))
    {
#ifdef DEBUG
        if (compiler->verbose)
        {
            printf("\t\t\t\t\t\t\tLive vars: ");
            dumpConvertedVarSet(compiler, compiler->compCurLife);
            printf(" => ");
            dumpConvertedVarSet(compiler, newLife);
            printf("\n");
        }
#endif // DEBUG

        VarSetOps::Assign(compiler, compiler->compCurLife, newLife);

        if (ForCodeGen)
        {
            // Only add vars to the gcInfo.gcVarPtrSetCur if they are currently on stack, since the
            // gcInfo.gcTrkStkPtrLcls
            // includes all TRACKED vars that EVER live on the stack (i.e. are not always in a register).
            VarSetOps::Assign(compiler, gcTrkStkDeltaSet, compiler->codeGen->gcInfo.gcTrkStkPtrLcls);
            if (isInMemory && VarSetOps::IsMember(compiler, gcTrkStkDeltaSet, fldVarIndex))
            {
#ifdef DEBUG
                if (compiler->verbose)
                {
                    printf("\t\t\t\t\t\t\tGCvars: ");
                    dumpConvertedVarSet(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur);
                    printf(" => ");
                }
#endif // DEBUG

                if (isBorn)
                {
                    VarSetOps::AddElemD(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur, fldVarIndex);
                }
                else
                {
                    VarSetOps::RemoveElemD(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur, fldVarIndex);
                }

#ifdef DEBUG
                if (compiler->verbose)
                {
                    dumpConvertedVarSet(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur);
                    printf("\n");
                }
#endif // DEBUG
            }

#ifdef USING_VARIABLE_LIVE_RANGE
            // For each of the LclVarDsc that are reporting change, variable or fields
            compiler->codeGen->getVariableLiveKeeper()->siStartOrCloseVariableLiveRange(fldVarDsc, fieldVarNum, isBorn,
                                                                                        isDying);
#endif // USING_VARIABLE_LIVE_RANGE

#ifdef USING_SCOPE_INFO
            compiler->codeGen->siUpdate();
#endif // USING_SCOPE_INFO
        }
    }

    if (ForCodeGen && spill)
    {
        if (VarSetOps::IsMember(compiler, compiler->codeGen->gcInfo.gcTrkStkPtrLcls, fldVarIndex))
        {
            if (!VarSetOps::IsMember(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur, fldVarIndex))
            {
                VarSetOps::AddElemD(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur, fldVarIndex);
#ifdef DEBUG
                if (compiler->verbose)
                {
                    printf("\t\t\t\t\t\t\tVar V%02u becoming live\n", fieldVarNum);
                }
#endif // DEBUG
            }
        }
        return true;
    }
    return false;
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
    if (!varDsc->lvTracked && !varDsc->lvPromoted)
    {
        return;
    }

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

    bool isBorn = ((lclVarTree->gtFlags & GTF_VAR_DEF) != 0) && ((lclVarTree->gtFlags & GTF_VAR_USEASG) == 0);

    if (varDsc->lvTracked)
    {
        assert(!varDsc->lvPromoted && !lclVarTree->IsMultiRegLclVar());

        bool isDying = (lclVarTree->gtFlags & GTF_VAR_DEATH) != 0;

        if (isBorn || isDying)
        {
            bool previouslyLive = VarSetOps::IsMember(compiler, compiler->compCurLife, varDsc->lvVarIndex);
            UpdateBit(compiler->compCurLife, varDsc, isBorn, isDying);

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
                    UpdateBit(compiler->codeGen->gcInfo.gcVarPtrSetCur, varDsc, isBorn, isDying);
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
#ifdef DEBUG
        if (lclVarTree->IsMultiRegLclVar())
        {
            // We should never have an indirect reference for a multi-reg.
            assert(lclVarTree == tree);
            assert((lclVarTree->gtFlags & GTF_VAR_USEASG) == 0);
            assert(varDsc->lvPromoted && !varDsc->lvTracked);
        }
#endif

        bool isAnyFieldDying = lclVarTree->HasLastUse();

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
                    assert(!lclVarTree->IsMultiRegLclVar());
                    continue;
                }

                bool previouslyLive = VarSetOps::IsMember(compiler, compiler->compCurLife, fldVarDsc->lvVarIndex);
                bool isDying        = lclVarTree->IsLastUse(i);
                UpdateBit(compiler->compCurLife, fldVarDsc, isBorn, isDying);

                if (ForCodeGen)
                {
                    // Only multireg locals can have enregistered fields.
                    assert(lclVarTree->IsMultiRegLclVar() || !fldVarDsc->lvIsInReg());

                    bool isInReg    = fldVarDsc->lvIsInReg() && (lclVarTree->AsLclVar()->GetRegNumByIdx(i) != REG_NA);
                    bool isInMemory = !isInReg || fldVarDsc->IsAlwaysAliveInMemory();

                    if (isInReg)
                    {
                        if (isBorn)
                        {
                            compiler->codeGen->genUpdateVarReg(fldVarDsc, tree, i);
                        }

                        compiler->codeGen->genUpdateRegLife(fldVarDsc, isBorn, isDying DEBUGARG(tree));
                        // If this was marked for spill genProduceReg should already have spilled it.
                        bool fieldNeedsSpill = ((lclVarTree->gtFlags & GTF_SPILL) != 0) &&
                                               ((lclVarTree->GetRegSpillFlagByIdx(i) & GTF_SPILL) != 0);
                        assert(!fieldNeedsSpill);
                    }

                    if (isInMemory &&
                        VarSetOps::IsMember(compiler, compiler->codeGen->gcInfo.gcTrkStkPtrLcls, fldVarDsc->lvVarIndex))
                    {
                        UpdateBit(compiler->codeGen->gcInfo.gcVarPtrSetCur, fldVarDsc, isBorn, isDying);
                    }

                    if ((isDying != isBorn) && (isBorn != previouslyLive))
                    {
                        compiler->codeGen->getVariableLiveKeeper()->siStartOrCloseVariableLiveRange(fldVarDsc,
                                                                                                    fldLclNum, isBorn,
                                                                                                    isDying);
                    }
                }
            }
        }
    }

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

template <bool ForCodeGen>
void TreeLifeUpdater<ForCodeGen>::UpdateBit(VARSET_TP& set, LclVarDsc* dsc, bool isBorn, bool isDying)
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

template class TreeLifeUpdater<true>;
template class TreeLifeUpdater<false>;
