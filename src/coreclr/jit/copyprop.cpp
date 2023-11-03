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

//------------------------------------------------------------------------------
// optBlockCopyPropPopStacks: pop copy prop stack
//
// Notes:
//    Corresponding to the live definition pushes, pop the stack as we finish a sub-paths
//    of the graph originating from the block. Refer SSA renaming for any additional info.
//    "curSsaName" tracks the currently live definitions.
//
void Compiler::optBlockCopyPropPopStacks(BasicBlock* block, LclNumToLiveDefsMap* curSsaName)
{
    auto popDef = [=](unsigned defLclNum, unsigned defSsaNum) {
        CopyPropSsaDefStack* stack = nullptr;
        if ((defSsaNum != SsaConfig::RESERVED_SSA_NUM) && curSsaName->Lookup(defLclNum, &stack))
        {
            stack->Pop();
            if (stack->Empty())
            {
                curSsaName->Remove(defLclNum);
            }
        }
    };

    for (Statement* const stmt : block->Statements())
    {
        for (GenTree* const tree : stmt->TreeList())
        {
            GenTreeLclVarCommon* lclDefNode = nullptr;
            if (tree->OperIsSsaDef() && tree->DefinesLocal(this, &lclDefNode))
            {
                if (lclDefNode->HasCompositeSsaName())
                {
                    LclVarDsc* varDsc = lvaGetDesc(lclDefNode);
                    assert(varDsc->lvPromoted);

                    for (unsigned index = 0; index < varDsc->lvFieldCnt; index++)
                    {
                        popDef(varDsc->lvFieldLclStart + index, lclDefNode->GetSsaNum(this, index));
                    }
                }
                else
                {
                    popDef(lclDefNode->GetLclNum(), lclDefNode->GetSsaNum());
                }
            }
        }
    }
}

#ifdef DEBUG
//------------------------------------------------------------------------------
// optDumpCopyPropStacks: dump copy prop stack
//
void Compiler::optDumpCopyPropStack(LclNumToLiveDefsMap* curSsaName)
{
    JITDUMP("{ ");
    for (LclNumToLiveDefsMap::Node* const iter : LclNumToLiveDefsMap::KeyValueIteration(curSsaName))
    {
        unsigned             defLclNum  = iter->GetKey();
        GenTreeLclVarCommon* lclDefNode = iter->GetValue()->Top().GetDefNode()->AsLclVarCommon();
        LclSsaVarDsc*        ssaDef     = iter->GetValue()->Top().GetSsaDef();

        if (ssaDef != nullptr)
        {
            unsigned defSsaNum = lvaGetDesc(defLclNum)->GetSsaNumForSsaDef(ssaDef);
            JITDUMP("[%06d]:V%02u/%u ", dspTreeID(lclDefNode), defLclNum, defSsaNum);
        }
        else
        {
            JITDUMP("[%06d]:V%02u/NA ", dspTreeID(lclDefNode), defLclNum);
        }
    }
    JITDUMP("}\n\n");
}
#endif
//------------------------------------------------------------------------------
// optCopyProp_LclVarScore: compute if the copy prop will be beneficial
//
// Arguments:
//    lclVarDsc  - variable that is target of a potential copy prop
//    copyVarDsc - variable that is source of a potential copy prop
//    preferOp2  - true if ...??
//
// Returns:
//    "score" indicating relative profitability of the copy
//      (non-negative: favorable)
//
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
//    block       -  BasicBlock containing stmt
//    stmt        -  Statement the tree belongs to
//    tree        -  The local tree to perform copy propagation on
//    lclNum      -  Number of the local "tree" refers to
//    curSsaName  -  The map from lclNum to its recently live definitions as a stack
//
// Returns:
//    Whether any changes were made.
//
bool Compiler::optCopyProp(
    BasicBlock* block, Statement* stmt, GenTreeLclVarCommon* tree, unsigned lclNum, LclNumToLiveDefsMap* curSsaName)
{
    assert((tree->gtFlags & GTF_VAR_DEF) == 0);
    assert(tree->GetLclNum() == lclNum);

    bool       madeChanges = false;
    LclVarDsc* varDsc      = lvaGetDesc(lclNum);
    ValueNum   lclDefVN    = varDsc->GetPerSsaData(tree->GetSsaNum())->m_vnPair.GetConservative();
    assert(lclDefVN != ValueNumStore::NoVN);

    for (LclNumToLiveDefsMap::Node* const iter : LclNumToLiveDefsMap::KeyValueIteration(curSsaName))
    {
        unsigned newLclNum = iter->GetKey();

        // Nothing to do if same.
        if (lclNum == newLclNum)
        {
            continue;
        }

        CopyPropSsaDef      newLclDef    = iter->GetValue()->Top();
        LclSsaVarDsc* const newLclSsaDef = newLclDef.GetSsaDef();

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

        // It may not be profitable to propagate a 'doNotEnregister' lclVar to an existing use of an
        // enregisterable lclVar.
        LclVarDsc* const newLclVarDsc = lvaGetDesc(newLclNum);
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
        newLclSsaDef->AddUse(block);

#ifdef DEBUG
        if (verbose)
        {
            printf("copy propagated to:\n");
            DISPNODE(tree);
        }
#endif

        madeChanges = true;
        break;
    }

    return madeChanges;
}

//------------------------------------------------------------------------------
// optCopyPropPushDef: Push the new live SSA def on the stack for "lclNode".
//
// Arguments:
//    defNode    - The definition node for this def (store/GT_CALL) (will be "nullptr" for "use" defs)
//    lclNode    - The local tree representing "the def"
//    curSsaName - The map of local numbers to stacks of their defs
//
void Compiler::optCopyPropPushDef(GenTree* defNode, GenTreeLclVarCommon* lclNode, LclNumToLiveDefsMap* curSsaName)
{
    unsigned lclNum = lclNode->GetLclNum();

    // Shadowed parameters are special: they will (at most) have one use, that is one on the RHS of an
    // assignment to their shadow, and we must not substitute them anywhere. So we'll not push any defs.
    if ((gsShadowVarInfo != nullptr) && lvaGetDesc(lclNum)->lvIsParam &&
        (gsShadowVarInfo[lclNum].shadowCopy != BAD_VAR_NUM))
    {
        assert(!curSsaName->Lookup(lclNum));
        return;
    }

    auto pushDef = [=](unsigned defLclNum, unsigned defSsaNum) {
        // The default is "not available".
        LclSsaVarDsc* ssaDef = nullptr;

        if (defSsaNum != SsaConfig::RESERVED_SSA_NUM)
        {
            ssaDef = lvaGetDesc(defLclNum)->GetPerSsaData(defSsaNum);
        }

        CopyPropSsaDefStack* defStack;
        if (!curSsaName->Lookup(defLclNum, &defStack))
        {
            defStack = new (curSsaName->GetAllocator()) CopyPropSsaDefStack(curSsaName->GetAllocator());
            curSsaName->Set(defLclNum, defStack);
        }

        defStack->Push(CopyPropSsaDef(ssaDef, lclNode));
    };

    if (lclNode->HasCompositeSsaName())
    {
        LclVarDsc* varDsc = lvaGetDesc(lclNum);
        assert(varDsc->lvPromoted);

        for (unsigned index = 0; index < varDsc->lvFieldCnt; index++)
        {
            unsigned ssaNum = lclNode->GetSsaNum(this, index);
            if (ssaNum != SsaConfig::RESERVED_SSA_NUM)
            {
                pushDef(varDsc->lvFieldLclStart + index, ssaNum);
            }
        }
    }
    else if (lclNode->HasSsaName())
    {
        unsigned ssaNum = lclNode->GetSsaNum();
        if ((defNode != nullptr) && defNode->IsPhiDefn())
        {
            // TODO-CQ: design better heuristics for propagation and remove this.
            ssaNum = SsaConfig::RESERVED_SSA_NUM;
        }

        pushDef(lclNum, ssaNum);
    }
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
// Returns:
//    true if any copy prop was done
//
bool Compiler::optBlockCopyProp(BasicBlock* block, LclNumToLiveDefsMap* curSsaName)
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
    bool                   madeChanges = false;

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
                optCopyPropPushDef(tree, lclDefNode, curSsaName);
            }
            else if (tree->OperIs(GT_LCL_VAR, GT_LCL_FLD) && tree->AsLclVarCommon()->HasSsaName())
            {
                unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();

                // If we encounter first use of a param or this pointer add it as a
                // live definition. Since they are always live, we'll do it only once.
                if ((lvaGetDesc(lclNum)->lvIsParam || (lclNum == info.compThisArg)) && !curSsaName->Lookup(lclNum))
                {
                    optCopyPropPushDef(nullptr, tree->AsLclVarCommon(), curSsaName);
                }

                // TODO-Review: EH successor/predecessor iteration seems broken.
                if ((block->bbCatchTyp == BBCT_FINALLY) || (block->bbCatchTyp == BBCT_FAULT))
                {
                    continue;
                }

                madeChanges |= optCopyProp(block, stmt, tree->AsLclVarCommon(), lclNum, curSsaName);
            }
        }
    }

    return madeChanges;
}

//------------------------------------------------------------------------------
// optVnCopyProp: value numbering based copy propagation
//
// Returns:
//    Suitable phase status
//
// Notes:
//
//   This phase performs value numbering based copy propagation. Since copy propagation
//   is about data flow, we cannot find them in assertion prop phase. In assertion prop
//   we can identify copies that like so: if (a == b) else, i.e., control flow assertions.
//
//   To identify data flow copies, we follow a similar approach to SSA renaming. We walk
//   each path in the graph keeping track of every live definition. Thus when we see a
//   variable that shares the VN with a live definition, we'd replace this variable with
//   the variable in the live definition.
//
//   We do this to be in conventional SSA form. This can very well be changed later.
//
//   For example, on some path in the graph:
//      a0 = x0
//      :            <- other blocks
//      :
//      a1 = y0
//      :
//      :            <- other blocks
//      b0 = x0, we cannot substitute x0 with a0, because currently our backend doesn't
//   treat lclNum and ssaNum together as a variable, but just looks at lclNum. If we
//   substituted x0 with a0, then we'd be in general SSA form.
//
PhaseStatus Compiler::optVnCopyProp()
{
    if (fgSsaPassesCompleted == 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    VarSetOps::AssignNoCopy(this, compCurLife, VarSetOps::MakeEmpty(this));

    class CopyPropDomTreeVisitor : public DomTreeVisitor<CopyPropDomTreeVisitor>
    {
        // The map from lclNum to its recently live definitions as a stack.
        LclNumToLiveDefsMap m_curSsaName;
        bool                m_madeChanges = false;

    public:
        CopyPropDomTreeVisitor(Compiler* compiler)
            : DomTreeVisitor(compiler, compiler->fgSsaDomTree)
            , m_curSsaName(compiler->getAllocator(CMK_CopyProp))
            , m_madeChanges(false)
        {
        }

        bool MadeChanges() const
        {
            return m_madeChanges;
        }

        void PreOrderVisit(BasicBlock* block)
        {
            // TODO-Cleanup: Move this function from Compiler to this class.
            m_madeChanges |= m_compiler->optBlockCopyProp(block, &m_curSsaName);
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
            for (LclNumToLiveDefsMap::Node* const iter : LclNumToLiveDefsMap::KeyValueIteration(&m_curSsaName))
            {
                unsigned lclNum = iter->GetKey();
                assert(m_compiler->lvaGetDesc(lclNum)->lvIsParam || (lclNum == m_compiler->info.compThisArg));

                CopyPropSsaDefStack* defStack = iter->GetValue();
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

    return visitor.MadeChanges() ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}
