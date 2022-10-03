// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//                                    CopyProp
//
// This stage performs value numbering based copy propagation. Since copy propagation
// is about data flow, we cannot find them in assertion prop phase. In assertion prop
// we can identify copies, like so: if (a == b) else, i.e., control flow assertions.
//
// To identify data flow copies, we'll follow a similar approach to SSA renaming.
// We would walk each path in the graph keeping track of every live definition. Thus
// when we see a variable that shares the VN with a live definition, we'd replace this
// variable with the variable in the live definition, if suitable.
//
///////////////////////////////////////////////////////////////////////////////////////

#include "jitpch.h"
#include "ssabuilder.h"
#include "treelifeupdater.h"

/**************************************************************************************
 *
 * Corresponding to the live definition pushes, pop the stack as we finish a sub-paths
 * of the graph originating from the block. Refer SSA renaming for any additional info.
 * "curSsaName" tracks the currently live definitions.
 */
void Compiler::optBlockCopyPropPopStacks(BasicBlock* block, LclNumToLiveDefsMap* curSsaName)
{
    for (Statement* const stmt : block->Statements())
    {
        for (GenTree* const tree : stmt->TreeList())
        {
            GenTreeLclVarCommon* lclDefNode = nullptr;
            if (tree->OperIsSsaDef() && tree->DefinesLocal(this, &lclDefNode))
            {
                const unsigned lclNum = optIsSsaLocal(lclDefNode);

                if (lclNum == BAD_VAR_NUM)
                {
                    continue;
                }

                CopyPropSsaDefStack* stack = nullptr;
                if (curSsaName->Lookup(lclNum, &stack))
                {
                    stack->Pop();
                    if (stack->Empty())
                    {
                        curSsaName->Remove(lclNum);
                    }
                }
            }
        }
    }
}

#ifdef DEBUG
void Compiler::optDumpCopyPropStack(LclNumToLiveDefsMap* curSsaName)
{
    JITDUMP("{ ");
    for (LclNumToLiveDefsMap::KeyIterator iter = curSsaName->Begin(); !iter.Equal(curSsaName->End()); ++iter)
    {
        GenTreeLclVarCommon* lclVar    = iter.GetValue()->Top().GetDefNode()->AsLclVarCommon();
        unsigned             ssaLclNum = optIsSsaLocal(lclVar);
        assert(ssaLclNum != BAD_VAR_NUM);

        if (ssaLclNum == lclVar->GetLclNum())
        {
            JITDUMP("%d-[%06d]:V%02u ", iter.Get(), dspTreeID(lclVar), ssaLclNum);
        }
        else
        {
            // A promoted field was assigned using the parent struct, print `ssa field lclNum(parent lclNum)`.
            JITDUMP("%d-[%06d]:V%02u(V%02u) ", iter.Get(), dspTreeID(lclVar), ssaLclNum, lclVar->GetLclNum());
        }
    }
    JITDUMP("}\n\n");
}
#endif
/*******************************************************************************************************
 *
 * Given the "lclVar" and "copyVar" compute if the copy prop will be beneficial.
 *
 */
int Compiler::optCopyProp_LclVarScore(const LclVarDsc* lclVarDsc, const LclVarDsc* copyVarDsc, bool preferOp2)
{
    int score = 0;

    if (lclVarDsc->lvVolatileHint)
    {
        score += 4;
    }

    if (copyVarDsc->lvVolatileHint)
    {
        score -= 4;
    }

#ifdef TARGET_X86
    // For doubles we also prefer to change parameters into non-parameter local variables
    if (lclVarDsc->lvType == TYP_DOUBLE)
    {
        if (lclVarDsc->lvIsParam)
        {
            score += 2;
        }

        if (copyVarDsc->lvIsParam)
        {
            score -= 2;
        }
    }
#endif

    // Otherwise we prefer to use the op2LclNum
    return score + ((preferOp2) ? 1 : -1);
}

//------------------------------------------------------------------------------
// optCopyProp : Perform copy propagation on a given tree as we walk the graph and if it is a local
//               variable, then look up all currently live definitions and check if any of those
//               definitions share the same value number. If so, then we can make the replacement.
//
// Arguments:
//    stmt        -  Statement the tree belongs to
//    tree        -  The local tree to perform copy propagation on
//    lclNum      -  The local number of said tree
//    curSsaName  -  The map from lclNum to its recently live definitions as a stack
//
void Compiler::optCopyProp(Statement* stmt, GenTreeLclVarCommon* tree, unsigned lclNum, LclNumToLiveDefsMap* curSsaName)
{
    assert((lclNum != BAD_VAR_NUM) && (optIsSsaLocal(tree) == lclNum) && ((tree->gtFlags & GTF_VAR_DEF) == 0));
    assert(tree->gtVNPair.BothDefined());

    LclVarDsc* varDsc   = lvaGetDesc(lclNum);
    ValueNum   lclDefVN = varDsc->GetPerSsaData(tree->GetSsaNum())->m_vnPair.GetConservative();
    assert(lclDefVN != ValueNumStore::NoVN);

    for (LclNumToLiveDefsMap::KeyIterator iter = curSsaName->Begin(); !iter.Equal(curSsaName->End()); ++iter)
    {
        unsigned newLclNum = iter.Get();

        // Nothing to do if same.
        if (lclNum == newLclNum)
        {
            continue;
        }

        CopyPropSsaDef newLclDef    = iter.GetValue()->Top();
        LclSsaVarDsc*  newLclSsaDef = newLclDef.GetSsaDef();

        // Likewise, nothing to do if the most recent def is not available.
        if (newLclSsaDef == nullptr)
        {
            continue;
        }

        ValueNum newLclDefVN = newLclSsaDef->m_vnPair.GetConservative();
        assert(newLclDefVN != ValueNumStore::NoVN);

        if (newLclDefVN != lclDefVN)
        {
            continue;
        }

        // Do not copy propagate if the old and new lclVar have different 'doNotEnregister' settings.
        // This is primarily to avoid copy propagating to IND(ADDR(LCL_VAR)) where the replacement lclVar
        // is not marked 'lvDoNotEnregister'.
        // However, in addition, it may not be profitable to propagate a 'doNotEnregister' lclVar to an
        // existing use of an enregisterable lclVar.
        LclVarDsc* newLclVarDsc = lvaGetDesc(newLclNum);
        if (varDsc->lvDoNotEnregister != newLclVarDsc->lvDoNotEnregister)
        {
            continue;
        }

        if (optCopyProp_LclVarScore(varDsc, newLclVarDsc, true) <= 0)
        {
            continue;
        }

        // Check whether the newLclNum is live before being substituted. Otherwise, we could end
        // up in a situation where there must've been a phi node that got pruned because the variable
        // is not live anymore. For example,
        //  if
        //     x0 = 1
        //  else
        //     x1 = 2
        //  print(c) <-- x is not live here. Let's say 'c' shares the value number with "x0."
        //
        // If we simply substituted 'c' with "x0", we would be wrong. Ideally, there would be a phi
        // node x2 = phi(x0, x1) which can then be used to substitute 'c' with. But because of pruning
        // there would be no such phi node. To solve this we'll check if 'x' is live, before replacing
        // 'c' with 'x.'

        // We compute liveness only on tracked variables. And all SSA locals are tracked.
        assert(newLclVarDsc->lvTracked);

        // Because of this dependence on live variable analysis, CopyProp phase is immediately
        // after Liveness, SSA and VN.
        if ((newLclNum != info.compThisArg) && !VarSetOps::IsMember(this, compCurLife, newLclVarDsc->lvVarIndex))
        {
            continue;
        }

        if (tree->OperIs(GT_LCL_VAR))
        {
            var_types newLclType = newLclVarDsc->TypeGet();
            if (!newLclVarDsc->lvNormalizeOnLoad())
            {
                newLclType = genActualType(newLclType);
            }

            if (newLclType != tree->TypeGet())
            {
                continue;
            }
        }

#ifdef DEBUG
        if (verbose)
        {
            JITDUMP("VN based copy assertion for ");
            printTreeID(tree);
            printf(" V%02d " FMT_VN " by ", lclNum, lclDefVN);
            printTreeID(newLclDef.GetDefNode());
            printf(" V%02d " FMT_VN ".\n", newLclNum, newLclDefVN);
            DISPNODE(tree);
        }
#endif

        unsigned newSsaNum = newLclVarDsc->GetSsaNumForSsaDef(newLclSsaDef);
        assert(newSsaNum != SsaConfig::RESERVED_SSA_NUM);

        tree->AsLclVarCommon()->SetLclNum(newLclNum);
        tree->AsLclVarCommon()->SetSsaNum(newSsaNum);
        gtUpdateSideEffects(stmt, tree);

#ifdef DEBUG
        if (verbose)
        {
            printf("copy propagated to:\n");
            DISPNODE(tree);
        }
#endif
        break;
    }
}

//------------------------------------------------------------------------------
// optIsSsaLocal : helper to check if the tree is a local that participates in SSA numbering.
//
// Arguments:
//    lclNode - The local tree to perform the check on;
//
// Returns:
//    - lclNum if the local is participating in SSA;
//    - fieldLclNum if the parent local can be replaced by its only field;
//    - BAD_VAR_NUM otherwise.
//
unsigned Compiler::optIsSsaLocal(GenTreeLclVarCommon* lclNode)
{
    unsigned   lclNum = lclNode->GetLclNum();
    LclVarDsc* varDsc = lvaGetDesc(lclNum);

    if (!lvaInSsa(lclNum) && varDsc->CanBeReplacedWithItsField(this))
    {
        lclNum = varDsc->lvFieldLclStart;
    }

    if (!lvaInSsa(lclNum))
    {
        return BAD_VAR_NUM;
    }

    return lclNum;
}

//------------------------------------------------------------------------------
// optCopyPropPushDef: Push the new live SSA def on the stack for "lclNode".
//
// Arguments:
//    defNode    - The definition node for this def (GT_ASG/GT_CALL) (will be "nullptr" for "use" defs)
//    lclNode    - The local tree representing "the def" (that can actually be a use)
//    lclNum     - The local's number (see "optIsSsaLocal")
//    curSsaName - The map of local numbers to stacks of their defs
//
void Compiler::optCopyPropPushDef(GenTree*             defNode,
                                  GenTreeLclVarCommon* lclNode,
                                  unsigned             lclNum,
                                  LclNumToLiveDefsMap* curSsaName)
{
    assert((lclNum != BAD_VAR_NUM) && (lclNum == optIsSsaLocal(lclNode)));

    // Shadowed parameters are special: they will (at most) have one use, that is one on the RHS of an
    // assignment to their shadow, and we must not substitute them anywhere. So we'll not push any defs.
    if ((gsShadowVarInfo != nullptr) && lvaGetDesc(lclNum)->lvIsParam &&
        (gsShadowVarInfo[lclNum].shadowCopy != BAD_VAR_NUM))
    {
        assert(!curSsaName->Lookup(lclNum));
        return;
    }

    unsigned ssaDefNum = SsaConfig::RESERVED_SSA_NUM;
    if (defNode == nullptr)
    {
        // Parameters, this pointer etc.
        assert((lclNode->gtFlags & GTF_VAR_DEF) == 0);
        assert(lclNode->GetSsaNum() == SsaConfig::FIRST_SSA_NUM);
        ssaDefNum = lclNode->GetSsaNum();
    }
    else
    {
        assert((lclNode->gtFlags & GTF_VAR_DEF) != 0);

        // TODO-CQ: design better heuristics for propagation and remove this condition.
        if (!defNode->IsPhiDefn())
        {
            ssaDefNum = GetSsaNumForLocalVarDef(lclNode);

            // This will be "RESERVED_SSA_NUM" for promoted struct fields assigned using the parent struct.
            // TODO-CQ: fix this.
            assert((ssaDefNum != SsaConfig::RESERVED_SSA_NUM) || lvaGetDesc(lclNode)->CanBeReplacedWithItsField(this));
        }
    }

    // The default is "not available".
    LclSsaVarDsc* ssaDef = nullptr;

    if (ssaDefNum != SsaConfig::RESERVED_SSA_NUM)
    {
        ssaDef = lvaGetDesc(lclNum)->GetPerSsaData(ssaDefNum);
    }

    CopyPropSsaDefStack* defStack;
    if (!curSsaName->Lookup(lclNum, &defStack))
    {
        defStack = new (curSsaName->GetAllocator()) CopyPropSsaDefStack(curSsaName->GetAllocator());
        curSsaName->Set(lclNum, defStack);
    }

    defStack->Push(CopyPropSsaDef(ssaDef, lclNode));
}

//------------------------------------------------------------------------------
// optBlockCopyProp : Perform copy propagation using currently live definitions on the current block's
//                    variables. Also as new definitions are encountered update the "curSsaName" which
//                    tracks the currently live definitions.
//
// Arguments:
//    block       -  Block the tree belongs to
//    curSsaName  -  The map from lclNum to its recently live definitions as a stack
//
void Compiler::optBlockCopyProp(BasicBlock* block, LclNumToLiveDefsMap* curSsaName)
{
#ifdef DEBUG
    JITDUMP("Copy Assertion for " FMT_BB "\n", block->bbNum);
    if (verbose)
    {
        printf("  curSsaName stack: ");
        optDumpCopyPropStack(curSsaName);
    }
#endif

    // We are not generating code so we don't need to deal with liveness change
    TreeLifeUpdater<false> treeLifeUpdater(this);

    // There are no definitions at the start of the block. So clear it.
    compCurLifeTree = nullptr;
    VarSetOps::Assign(this, compCurLife, block->bbLiveIn);
    for (Statement* const stmt : block->Statements())
    {
        // Walk the tree to find if any local variable can be replaced with current live definitions.
        // Simultaneously, push live definitions on the stack - that logic must be in sync with the
        // SSA renaming process.
        for (GenTree* const tree : stmt->TreeList())
        {
            treeLifeUpdater.UpdateLife(tree);

            GenTreeLclVarCommon* lclDefNode = nullptr;
            if (tree->OperIsSsaDef() && tree->DefinesLocal(this, &lclDefNode))
            {
                const unsigned lclNum = optIsSsaLocal(lclDefNode);

                if (lclNum == BAD_VAR_NUM)
                {
                    continue;
                }

                optCopyPropPushDef(tree, lclDefNode, lclNum, curSsaName);
            }
            else if (tree->OperIs(GT_LCL_VAR, GT_LCL_FLD) && ((tree->gtFlags & GTF_VAR_DEF) == 0))
            {
                const unsigned lclNum = optIsSsaLocal(tree->AsLclVarCommon());

                if (lclNum == BAD_VAR_NUM)
                {
                    continue;
                }

                // If we encounter first use of a param or this pointer add it as a
                // live definition. Since they are always live, we'll do it only once.
                if ((lvaGetDesc(lclNum)->lvIsParam || (lclNum == info.compThisArg)) && !curSsaName->Lookup(lclNum))
                {
                    optCopyPropPushDef(nullptr, tree->AsLclVarCommon(), lclNum, curSsaName);
                }

                // TODO-Review: EH successor/predecessor iteration seems broken.
                if ((block->bbCatchTyp == BBCT_FINALLY) || (block->bbCatchTyp == BBCT_FAULT))
                {
                    continue;
                }

                optCopyProp(stmt, tree->AsLclVarCommon(), lclNum, curSsaName);
            }
        }
    }
}

/**************************************************************************************
 *
 * This stage performs value numbering based copy propagation. Since copy propagation
 * is about data flow, we cannot find them in assertion prop phase. In assertion prop
 * we can identify copies that like so: if (a == b) else, i.e., control flow assertions.
 *
 * To identify data flow copies, we follow a similar approach to SSA renaming. We walk
 * each path in the graph keeping track of every live definition. Thus when we see a
 * variable that shares the VN with a live definition, we'd replace this variable with
 * the variable in the live definition.
 *
 * We do this to be in conventional SSA form. This can very well be changed later.
 *
 * For example, on some path in the graph:
 *    a0 = x0
 *    :            <- other blocks
 *    :
 *    a1 = y0
 *    :
 *    :            <- other blocks
 *    b0 = x0, we cannot substitute x0 with a0, because currently our backend doesn't
 * treat lclNum and ssaNum together as a variable, but just looks at lclNum. If we
 * substituted x0 with a0, then we'd be in general SSA form.
 *
 */
void Compiler::optVnCopyProp()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optVnCopyProp()\n");
    }
#endif

    if (fgSsaPassesCompleted == 0)
    {
        return;
    }

    VarSetOps::AssignNoCopy(this, compCurLife, VarSetOps::MakeEmpty(this));

    class CopyPropDomTreeVisitor : public DomTreeVisitor<CopyPropDomTreeVisitor>
    {
        // The map from lclNum to its recently live definitions as a stack.
        LclNumToLiveDefsMap m_curSsaName;

    public:
        CopyPropDomTreeVisitor(Compiler* compiler)
            : DomTreeVisitor(compiler, compiler->fgSsaDomTree), m_curSsaName(compiler->getAllocator(CMK_CopyProp))
        {
        }

        void PreOrderVisit(BasicBlock* block)
        {
            // TODO-Cleanup: Move this function from Compiler to this class.
            m_compiler->optBlockCopyProp(block, &m_curSsaName);
        }

        void PostOrderVisit(BasicBlock* block)
        {
            // TODO-Cleanup: Move this function from Compiler to this class.
            m_compiler->optBlockCopyPropPopStacks(block, &m_curSsaName);
        }

        void PropagateCopies()
        {
            WalkTree();

#ifdef DEBUG
            // Verify the definitions remaining are only those we pushed for parameters.
            for (LclNumToLiveDefsMap::KeyIterator iter = m_curSsaName.Begin(); !iter.Equal(m_curSsaName.End()); ++iter)
            {
                unsigned lclNum = iter.Get();
                assert(m_compiler->lvaGetDesc(lclNum)->lvIsParam || (lclNum == m_compiler->info.compThisArg));

                CopyPropSsaDefStack* defStack = iter.GetValue();
                assert(defStack->Height() == 1);
            }
#endif // DEBUG
        }
    };

    CopyPropDomTreeVisitor visitor(this);
    visitor.PropagateCopies();

    // Tracked variable count increases after CopyProp, so don't keep a shorter array around.
    // Destroy (release) the varset.
    VarSetOps::AssignNoCopy(this, compCurLife, VarSetOps::UninitVal());
}
