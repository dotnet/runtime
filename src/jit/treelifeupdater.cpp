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
    , gcVarPtrSetNew(VarSetOps::MakeEmpty(compiler))
    , epoch(compiler->GetCurLVEpoch())
#endif // DEBUG
{
}

//------------------------------------------------------------------------
// UpdateLifeVar: Update live sets for a given tree.
//
// Arguments:
//    tree - the tree which affects liveness.
//
template <bool ForCodeGen>
void TreeLifeUpdater<ForCodeGen>::UpdateLifeVar(GenTree* tree)
{
    GenTree* indirAddrLocal = compiler->fgIsIndirOfAddrOfLocal(tree);
    assert(tree->OperIsNonPhiLocal() || indirAddrLocal != nullptr);

    // Get the local var tree -- if "tree" is "Ldobj(addr(x))", or "ind(addr(x))" this is "x", else it's "tree".
    GenTree* lclVarTree = indirAddrLocal;
    if (lclVarTree == nullptr)
    {
        lclVarTree = tree;
    }
    unsigned int lclNum = lclVarTree->gtLclVarCommon.gtLclNum;
    LclVarDsc*   varDsc = compiler->lvaTable + lclNum;

#ifdef DEBUG
#if !defined(_TARGET_AMD64_)
    // There are no addr nodes on ARM and we are experimenting with encountering vars in 'random' order.
    // Struct fields are not traversed in a consistent order, so ignore them when
    // verifying that we see the var nodes in execution order
    if (ForCodeGen)
    {
        if (tree->OperIsIndir())
        {
            assert(indirAddrLocal != NULL);
        }
        else if (tree->gtNext != NULL && tree->gtNext->gtOper == GT_ADDR &&
                 ((tree->gtNext->gtNext == NULL || !tree->gtNext->gtNext->OperIsIndir())))
        {
            assert(tree->IsLocal()); // Can only take the address of a local.
            // The ADDR might occur in a context where the address it contributes is eventually
            // dereferenced, so we can't say that this is not a use or def.
        }
    }
#endif // !_TARGET_AMD64_
#endif // DEBUG

    compiler->compCurLifeTree = tree;
    VarSetOps::Assign(compiler, newLife, compiler->compCurLife);

    // By codegen, a struct may not be TYP_STRUCT, so we have to
    // check lvPromoted, for the case where the fields are being
    // tracked.
    if (!varDsc->lvTracked && !varDsc->lvPromoted)
    {
        return;
    }

    // if it's a partial definition then variable "x" must have had a previous, original, site to be born.
    bool isBorn  = ((tree->gtFlags & GTF_VAR_DEF) != 0 && (tree->gtFlags & GTF_VAR_USEASG) == 0);
    bool isDying = ((tree->gtFlags & GTF_VAR_DEATH) != 0);
    bool spill   = ((tree->gtFlags & GTF_SPILL) != 0);

    // Since all tracked vars are register candidates, but not all are in registers at all times,
    // we maintain two separate sets of variables - the total set of variables that are either
    // born or dying here, and the subset of those that are on the stack
    VarSetOps::ClearD(compiler, stackVarDeltaSet);

    if (isBorn || isDying)
    {
        VarSetOps::ClearD(compiler, varDeltaSet);

        if (varDsc->lvTracked)
        {
            VarSetOps::AddElemD(compiler, varDeltaSet, varDsc->lvVarIndex);
            if (ForCodeGen)
            {
                if (isBorn && varDsc->lvIsRegCandidate() && tree->gtHasReg())
                {
                    compiler->codeGen->genUpdateVarReg(varDsc, tree);
                }
                if (varDsc->lvIsInReg() && tree->gtRegNum != REG_NA)
                {
                    compiler->codeGen->genUpdateRegLife(varDsc, isBorn, isDying DEBUGARG(tree));
                }
                else
                {
                    VarSetOps::AddElemD(compiler, stackVarDeltaSet, varDsc->lvVarIndex);
                }
            }
        }
        else if (varDsc->lvPromoted)
        {
            // If hasDeadTrackedFieldVars is true, then, for a LDOBJ(ADDR(<promoted struct local>)),
            // *deadTrackedFieldVars indicates which tracked field vars are dying.
            bool hasDeadTrackedFieldVars = false;

            if (indirAddrLocal != nullptr && isDying)
            {
                assert(!isBorn); // GTF_VAR_DEATH only set for LDOBJ last use.
                VARSET_TP* deadTrackedFieldVars = nullptr;
                hasDeadTrackedFieldVars =
                    compiler->LookupPromotedStructDeathVars(indirAddrLocal, &deadTrackedFieldVars);
                if (hasDeadTrackedFieldVars)
                {
                    VarSetOps::Assign(compiler, varDeltaSet, *deadTrackedFieldVars);
                }
            }

            for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
            {
                LclVarDsc* fldVarDsc = &(compiler->lvaTable[i]);
                noway_assert(fldVarDsc->lvIsStructField);
                if (fldVarDsc->lvTracked)
                {
                    unsigned fldVarIndex = fldVarDsc->lvVarIndex;
                    noway_assert(fldVarIndex < compiler->lvaTrackedCount);
                    if (!hasDeadTrackedFieldVars)
                    {
                        VarSetOps::AddElemD(compiler, varDeltaSet, fldVarIndex);
                        if (ForCodeGen)
                        {
                            // We repeat this call here and below to avoid the VarSetOps::IsMember
                            // test in this, the common case, where we have no deadTrackedFieldVars.
                            if (fldVarDsc->lvIsInReg())
                            {
                                if (isBorn)
                                {
                                    compiler->codeGen->genUpdateVarReg(fldVarDsc, tree);
                                }
                                compiler->codeGen->genUpdateRegLife(fldVarDsc, isBorn, isDying DEBUGARG(tree));
                            }
                            else
                            {
                                VarSetOps::AddElemD(compiler, stackVarDeltaSet, fldVarIndex);
                            }
                        }
                    }
                    else if (ForCodeGen && VarSetOps::IsMember(compiler, varDeltaSet, fldVarIndex))
                    {
                        if (compiler->lvaTable[i].lvIsInReg())
                        {
                            if (isBorn)
                            {
                                compiler->codeGen->genUpdateVarReg(fldVarDsc, tree);
                            }
                            compiler->codeGen->genUpdateRegLife(fldVarDsc, isBorn, isDying DEBUGARG(tree));
                        }
                        else
                        {
                            VarSetOps::AddElemD(compiler, stackVarDeltaSet, fldVarIndex);
                        }
                    }
                }
            }
        }

        // First, update the live set
        if (isDying)
        {
            // We'd like to be able to assert the following, however if we are walking
            // through a qmark/colon tree, we may encounter multiple last-use nodes.
            // assert (VarSetOps::IsSubset(compiler, regVarDeltaSet, newLife));
            VarSetOps::DiffD(compiler, newLife, varDeltaSet);
        }
        else
        {
            // This shouldn't be in newLife, unless this is debug code, in which
            // case we keep vars live everywhere, OR the variable is address-exposed,
            // OR this block is part of a try block, in which case it may be live at the handler
            // Could add a check that, if it's in newLife, that it's also in
            // fgGetHandlerLiveVars(compCurBB), but seems excessive
            //
            // For a dead store, it can be the case that we set both isBorn and isDying to true.
            // (We don't eliminate dead stores under MinOpts, so we can't assume they're always
            // eliminated.)  If it's both, we handled it above.
            VarSetOps::UnionD(compiler, newLife, varDeltaSet);
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
            VarSetOps::IntersectionD(compiler, gcTrkStkDeltaSet, stackVarDeltaSet);
            if (!VarSetOps::IsEmpty(compiler, gcTrkStkDeltaSet))
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
                    VarSetOps::UnionD(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur, gcTrkStkDeltaSet);
                }
                else
                {
                    VarSetOps::DiffD(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur, gcTrkStkDeltaSet);
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
            compiler->codeGen->getVariableLiveKeeper()->siStartOrCloseVariableLiveRanges(varDeltaSet, isBorn, isDying);
#endif // USING_VARIABLE_LIVE_RANGE

#ifdef USING_SCOPE_INFO
            compiler->codeGen->siUpdate();
#endif // USING_SCOPE_INFO
        }
    }

    if (ForCodeGen && spill)
    {
        assert(!varDsc->lvPromoted);
        compiler->codeGen->genSpillVar(tree);
        if (VarSetOps::IsMember(compiler, compiler->codeGen->gcInfo.gcTrkStkPtrLcls, varDsc->lvVarIndex))
        {
            if (!VarSetOps::IsMember(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex))
            {
                VarSetOps::AddElemD(compiler, compiler->codeGen->gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);
#ifdef DEBUG
                if (compiler->verbose)
                {
                    printf("\t\t\t\t\t\t\tVar V%02u becoming live\n", varDsc - compiler->lvaTable);
                }
#endif // DEBUG
            }
        }
    }
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

    if (!tree->OperIsNonPhiLocal() && compiler->fgIsIndirOfAddrOfLocal(tree) == nullptr)
    {
        return;
    }

    UpdateLifeVar(tree);
}

template class TreeLifeUpdater<true>;
template class TreeLifeUpdater<false>;
