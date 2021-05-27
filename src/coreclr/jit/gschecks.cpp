// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                               GSChecks                                    XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

/*****************************************************************************
 * gsGSChecksInitCookie
 * Grabs the cookie for detecting overflow of unsafe buffers.
 */
void Compiler::gsGSChecksInitCookie()
{
    var_types type = TYP_I_IMPL;

    lvaGSSecurityCookie = lvaGrabTempWithImplicitUse(false DEBUGARG("GSSecurityCookie"));

    // Prevent cookie init/check from being optimized
    lvaSetVarAddrExposed(lvaGSSecurityCookie);
    lvaTable[lvaGSSecurityCookie].lvType = type;

    info.compCompHnd->getGSCookie(&gsGlobalSecurityCookieVal, &gsGlobalSecurityCookieAddr);
}

const unsigned NO_SHADOW_COPY = UINT_MAX;

/*****************************************************************************
 * gsCopyShadowParams
 * The current function has an unsafe buffer on the stack.  Search for vulnerable
 * parameters which could be used to modify a code address and take over the process
 * in the case of a buffer overrun. Create a safe local copy for each vulnerable parameter,
 * which will be allocated bellow the unsafe buffer.  Change uses of the param to the
 * shadow copy.
 *
 * A pointer under indirection is considered vulnerable. A malicious user could read from
 * protected memory or write to it. If a parameter is assigned/computed into another variable,
 * and is a pointer (i.e., under indirection), then we consider the variable to be part of the
 * equivalence class with the parameter. All parameters in the equivalence class are shadowed.
 */
void Compiler::gsCopyShadowParams()
{
    if (info.compIsVarArgs)
    {
        return;
    }

    // Allocate array for shadow param info
    gsShadowVarInfo = new (this, CMK_Unknown) ShadowParamVarInfo[lvaCount]();

    // Find groups of variables assigned to each other, and also
    // tracks variables which are dereferenced and marks them as ptrs.
    // Look for assignments to *p, and ptrs passed to functions
    if (gsFindVulnerableParams())
    {
        // Replace vulnerable params by shadow copies.
        gsParamsToShadows();
    }
}

// This struct tracks how a tree is being used

struct MarkPtrsInfo
{
    Compiler* comp;
    unsigned  lvAssignDef;  // Which local variable is the tree being assigned to?
    bool      isAssignSrc;  // Is this the source value for an assignment?
    bool      isUnderIndir; // Is this a pointer value tree that is being dereferenced?
    bool      skipNextNode; // Skip a single node during the tree-walk

#ifdef DEBUG
    void Print()
    {
        printf(
            "[MarkPtrsInfo] = {comp = %p, lvAssignDef = %d, isAssignSrc = %d, isUnderIndir = %d, skipNextNode = %d}\n",
            comp, lvAssignDef, isAssignSrc, isUnderIndir, skipNextNode);
    }
#endif
};

/*****************************************************************************
 * gsMarkPtrsAndAssignGroups
 * Walk a tree looking for assignment groups, variables whose value is used
 * in a *p store or use, and variable passed to calls.  This info is then used
 * to determine parameters which are vulnerable.
 * This function carries a state to know if it is under an assign node, call node
 * or indirection node.  It starts a new tree walk for it's subtrees when the state
 * changes.
 */
Compiler::fgWalkResult Compiler::gsMarkPtrsAndAssignGroups(GenTree** pTree, fgWalkData* data)
{
    struct MarkPtrsInfo* pState        = (MarkPtrsInfo*)data->pCallbackData;
    struct MarkPtrsInfo  newState      = *pState;
    Compiler*            comp          = data->compiler;
    GenTree*             tree          = *pTree;
    ShadowParamVarInfo*  shadowVarInfo = pState->comp->gsShadowVarInfo;
    assert(shadowVarInfo);

    assert(!pState->isAssignSrc || pState->lvAssignDef != (unsigned)-1);

    if (pState->skipNextNode)
    {
        pState->skipNextNode = false;
        return WALK_CONTINUE;
    }

    switch (tree->OperGet())
    {
        // Indirections - look for *p uses and defs
        case GT_IND:
        case GT_BLK:
        case GT_OBJ:
        case GT_ARR_ELEM:
        case GT_ARR_INDEX:
        case GT_ARR_OFFSET:
        case GT_FIELD:

            newState.isUnderIndir = true;
            {
                newState.skipNextNode = true; // Don't have to worry about which kind of node we're dealing with
                comp->fgWalkTreePre(&tree, comp->gsMarkPtrsAndAssignGroups, (void*)&newState);
            }

            return WALK_SKIP_SUBTREES;

        // local vars and param uses
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        {
            unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();

            if (pState->isUnderIndir)
            {
                // The variable is being dereferenced for a read or a write.
                comp->lvaTable[lclNum].lvIsPtr = 1;
            }

            if (pState->isAssignSrc)
            {
                //
                // Add lvAssignDef and lclNum to a common assign group
                if (shadowVarInfo[pState->lvAssignDef].assignGroup)
                {
                    if (shadowVarInfo[lclNum].assignGroup)
                    {
                        // OR both bit vector
                        shadowVarInfo[pState->lvAssignDef].assignGroup->bitVectOr(shadowVarInfo[lclNum].assignGroup);
                    }
                    else
                    {
                        shadowVarInfo[pState->lvAssignDef].assignGroup->bitVectSet(lclNum);
                    }

                    // Point both to the same bit vector
                    shadowVarInfo[lclNum].assignGroup = shadowVarInfo[pState->lvAssignDef].assignGroup;
                }
                else if (shadowVarInfo[lclNum].assignGroup)
                {
                    shadowVarInfo[lclNum].assignGroup->bitVectSet(pState->lvAssignDef);

                    // Point both to the same bit vector
                    shadowVarInfo[pState->lvAssignDef].assignGroup = shadowVarInfo[lclNum].assignGroup;
                }
                else
                {
                    FixedBitVect* bv = FixedBitVect::bitVectInit(pState->comp->lvaCount, pState->comp);

                    // (shadowVarInfo[pState->lvAssignDef] == NULL && shadowVarInfo[lclNew] == NULL);
                    // Neither of them has an assign group yet.  Make a new one.
                    shadowVarInfo[pState->lvAssignDef].assignGroup = bv;
                    shadowVarInfo[lclNum].assignGroup              = bv;
                    bv->bitVectSet(pState->lvAssignDef);
                    bv->bitVectSet(lclNum);
                }
            }
            return WALK_CONTINUE;
        }

        // Calls - Mark arg variables
        case GT_CALL:

            newState.isUnderIndir = false;
            newState.isAssignSrc  = false;
            {
                if (tree->AsCall()->gtCallThisArg != nullptr)
                {
                    newState.isUnderIndir = true;
                    comp->fgWalkTreePre(&tree->AsCall()->gtCallThisArg->NodeRef(), gsMarkPtrsAndAssignGroups,
                                        (void*)&newState);
                }

                for (GenTreeCall::Use& use : tree->AsCall()->Args())
                {
                    comp->fgWalkTreePre(&use.NodeRef(), gsMarkPtrsAndAssignGroups, (void*)&newState);
                }
                for (GenTreeCall::Use& use : tree->AsCall()->LateArgs())
                {
                    comp->fgWalkTreePre(&use.NodeRef(), gsMarkPtrsAndAssignGroups, (void*)&newState);
                }

                if (tree->AsCall()->gtCallType == CT_INDIRECT)
                {
                    newState.isUnderIndir = true;

                    // A function pointer is treated like a write-through pointer since
                    // it controls what code gets executed, and so indirectly can cause
                    // a write to memory.
                    comp->fgWalkTreePre(&tree->AsCall()->gtCallAddr, gsMarkPtrsAndAssignGroups, (void*)&newState);
                }
            }
            return WALK_SKIP_SUBTREES;

        case GT_ADDR:
            newState.isUnderIndir = false;
            // We'll assume p in "**p = " can be vulnerable because by changing 'p', someone
            // could control where **p stores to.
            {
                comp->fgWalkTreePre(&tree->AsOp()->gtOp1, comp->gsMarkPtrsAndAssignGroups, (void*)&newState);
            }
            return WALK_SKIP_SUBTREES;

        case GT_ASG:
        {
            GenTreeOp* asg = tree->AsOp();
            GenTree*   dst = asg->gtGetOp1();
            GenTree*   src = asg->gtGetOp2();
            // Assignments - track assign groups and *p defs.

            // Walk dst side
            comp->fgWalkTreePre(&dst, comp->gsMarkPtrsAndAssignGroups, (void*)&newState);

            // Now handle src side
            if (dst->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                unsigned lclNum      = dst->AsLclVarCommon()->GetLclNum();
                newState.lvAssignDef = lclNum;
                newState.isAssignSrc = true;
            }

            comp->fgWalkTreePre(&src, comp->gsMarkPtrsAndAssignGroups, (void*)&newState);
            assert(dst == asg->gtGetOp1());
            assert(src == asg->gtGetOp2());

            return WALK_SKIP_SUBTREES;
        }

        default:
            return WALK_CONTINUE;
    }
}

/*****************************************************************************
 * gsFindVulnerableParams
 * Walk all the trees looking for ptrs, args, assign groups, *p stores, etc.
 * Then use that info to figure out vulnerable pointers.
 *
 * It returns true if it found atleast one vulnerable pointer parameter that
 * needs to be shadow-copied.
 */

bool Compiler::gsFindVulnerableParams()
{
    MarkPtrsInfo info;

    info.comp         = this;
    info.lvAssignDef  = (unsigned)-1;
    info.isUnderIndir = false;
    info.isAssignSrc  = false;
    info.skipNextNode = false;

    // Walk all the trees setting lvIsWritePtr, lvIsOutgoingArg, lvIsPtr and assignGroup.
    fgWalkAllTreesPre(gsMarkPtrsAndAssignGroups, &info);

    // Compute has vulnerable at the end of the loop.
    bool hasOneVulnerable = false;

    // Initialize propagated[v0...vn] = {0}^n, so we can skip the ones propagated through
    // some assign group.
    FixedBitVect* propagated = (lvaCount > 0) ? FixedBitVect::bitVectInit(lvaCount, this) : nullptr;

    for (UINT lclNum = 0; lclNum < lvaCount; lclNum++)
    {
        LclVarDsc*          varDsc     = &lvaTable[lclNum];
        ShadowParamVarInfo* shadowInfo = &gsShadowVarInfo[lclNum];

        // If there was an indirection or if unsafe buffer, then we'd call it vulnerable.
        if (varDsc->lvIsPtr || varDsc->lvIsUnsafeBuffer)
        {
            hasOneVulnerable = true;
        }

        // Now, propagate the info through the assign group (an equivalence class of vars transitively assigned.)
        if (shadowInfo->assignGroup == nullptr || propagated->bitVectTest(lclNum))
        {
            continue;
        }

        // Propagate lvIsPtr, so that:
        //   1. Any parameter in the equivalence class can be identified as lvIsPtr and hence shadowed.
        //   2. Buffers with pointers are placed at lower memory addresses than buffers without pointers.
        UINT isUnderIndir = varDsc->lvIsPtr;

        // First pass -- find if any variable is vulnerable.
        FixedBitVect* assignGroup = shadowInfo->assignGroup;
        for (UINT lclNum = assignGroup->bitVectGetFirst(); lclNum != (unsigned)-1 && !isUnderIndir;
             lclNum      = assignGroup->bitVectGetNext(lclNum))
        {
            isUnderIndir |= lvaTable[lclNum].lvIsPtr;
        }

        // Vulnerable, so propagate to all members of the equivalence class.
        if (isUnderIndir)
        {
            hasOneVulnerable = true;
        }
        // Nothing to propagate.
        else
        {
            continue;
        }

        // Second pass -- mark all are vulnerable.
        assert(isUnderIndir);
        for (UINT lclNum = assignGroup->bitVectGetFirst(); lclNum != (unsigned)-1;
             lclNum      = assignGroup->bitVectGetNext(lclNum))
        {
            lvaTable[lclNum].lvIsPtr = true;
            propagated->bitVectSet(lclNum);
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("Equivalence assign group %s: ", isUnderIndir ? "isPtr " : "");
            for (UINT lclNum = assignGroup->bitVectGetFirst(); lclNum != (unsigned)-1;
                 lclNum      = assignGroup->bitVectGetNext(lclNum))
            {
                gtDispLclVar(lclNum, false);
                printf(" ");
            }
            printf("\n");
        }
#endif
    }

    return hasOneVulnerable;
}

//-------------------------------------------------------------------------------
// gsParamsToShadows: Copy each vulnerable param ptr or buffer to a local shadow
//                    copy and replace uses of the param by the shadow copy.
void Compiler::gsParamsToShadows()
{
    // Cache old count since we'll add new variables, and
    // gsShadowVarInfo will not grow to accomodate the new ones.
    UINT lvaOldCount = lvaCount;

    // Create shadow copy for each param candidate
    for (UINT lclNum = 0; lclNum < lvaOldCount; lclNum++)
    {
        LclVarDsc* varDsc                  = &lvaTable[lclNum];
        gsShadowVarInfo[lclNum].shadowCopy = NO_SHADOW_COPY;

        // Only care about params whose values are on the stack
        if (!ShadowParamVarInfo::mayNeedShadowCopy(varDsc))
        {
            continue;
        }

        if (!varDsc->lvIsPtr && !varDsc->lvIsUnsafeBuffer)
        {
            continue;
        }

        int shadowVarNum = lvaGrabTemp(false DEBUGARG("shadowVar"));
        // reload varDsc as lvaGrabTemp may realloc the lvaTable[]
        varDsc                  = &lvaTable[lclNum];
        LclVarDsc* shadowVarDsc = &lvaTable[shadowVarNum];

        // Copy some info

        var_types type       = varTypeIsSmall(varDsc->TypeGet()) ? TYP_INT : varDsc->TypeGet();
        shadowVarDsc->lvType = type;

#ifdef FEATURE_SIMD
        shadowVarDsc->lvSIMDType            = varDsc->lvSIMDType;
        shadowVarDsc->lvUsedInSIMDIntrinsic = varDsc->lvUsedInSIMDIntrinsic;
        if (varDsc->lvSIMDType)
        {
            CorInfoType simdBaseJitType = varDsc->GetSimdBaseJitType();
            shadowVarDsc->SetSimdBaseJitType(simdBaseJitType);
        }
#endif
        shadowVarDsc->lvRegStruct = varDsc->lvRegStruct;

        shadowVarDsc->lvAddrExposed     = varDsc->lvAddrExposed;
        shadowVarDsc->lvDoNotEnregister = varDsc->lvDoNotEnregister;
#ifdef DEBUG
        shadowVarDsc->lvVMNeedsStackAddr = varDsc->lvVMNeedsStackAddr;
        shadowVarDsc->lvLiveInOutOfHndlr = varDsc->lvLiveInOutOfHndlr;
        shadowVarDsc->lvLclFieldExpr     = varDsc->lvLclFieldExpr;
        shadowVarDsc->lvLiveAcrossUCall  = varDsc->lvLiveAcrossUCall;
#endif
        shadowVarDsc->lvVerTypeInfo = varDsc->lvVerTypeInfo;
        if (varTypeIsStruct(type))
        {
            // We don't need unsafe value cls check here since we are copying the params and this flag
            // would have been set on the original param before reaching here.
            lvaSetStruct(shadowVarNum, varDsc->GetStructHnd(), false);
            shadowVarDsc->lvIsMultiRegArg = varDsc->lvIsMultiRegArg;
            shadowVarDsc->lvIsMultiRegRet = varDsc->lvIsMultiRegRet;
        }
        shadowVarDsc->lvIsUnsafeBuffer = varDsc->lvIsUnsafeBuffer;
        shadowVarDsc->lvIsPtr          = varDsc->lvIsPtr;

#ifdef DEBUG
        if (verbose)
        {
            printf("Var V%02u is shadow param candidate. Shadow copy is V%02u.\n", lclNum, shadowVarNum);
        }
#endif

        gsShadowVarInfo[lclNum].shadowCopy = shadowVarNum;
    }

    class ReplaceShadowParamsVisitor final : public GenTreeVisitor<ReplaceShadowParamsVisitor>
    {
        // Walk the locals of the method (i.e. GT_LCL_FLD and GT_LCL_VAR nodes) and replace the ones that correspond to
        // "vulnerable" parameters with their shadow copies. If an original local variable has small type then replace
        // the GT_LCL_VAR node type with TYP_INT.
    public:
        enum
        {
            DoPreOrder    = true,
            DoLclVarsOnly = true
        };

        ReplaceShadowParamsVisitor(Compiler* compiler) : GenTreeVisitor<ReplaceShadowParamsVisitor>(compiler)
        {
        }

        Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* tree = *use;

            unsigned int lclNum       = tree->AsLclVarCommon()->GetLclNum();
            unsigned int shadowLclNum = m_compiler->gsShadowVarInfo[lclNum].shadowCopy;

            if (shadowLclNum != NO_SHADOW_COPY)
            {
                LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
                assert(ShadowParamVarInfo::mayNeedShadowCopy(varDsc));

                tree->AsLclVarCommon()->SetLclNum(shadowLclNum);

                if (varTypeIsSmall(varDsc->TypeGet()))
                {
                    if (tree->OperIs(GT_LCL_VAR))
                    {
                        tree->gtType = TYP_INT;

                        if (user->OperIs(GT_ASG) && user->gtGetOp1() == tree)
                        {
                            user->gtType = TYP_INT;
                        }
                    }
                }
            }

            return WALK_CONTINUE;
        }
    };

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        for (Statement* stmt : block->Statements())
        {
            ReplaceShadowParamsVisitor replaceShadowParamsVisitor(this);
            replaceShadowParamsVisitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
        }
    }

    // Now insert code to copy the params to their shadow copy.
    for (UINT lclNum = 0; lclNum < lvaOldCount; lclNum++)
    {
        const LclVarDsc* varDsc = &lvaTable[lclNum];

        const unsigned shadowVarNum = gsShadowVarInfo[lclNum].shadowCopy;
        if (shadowVarNum == NO_SHADOW_COPY)
        {
            continue;
        }

        const LclVarDsc* shadowVarDsc = &lvaTable[shadowVarNum];
        var_types        type         = shadowVarDsc->TypeGet();

        GenTree* src = gtNewLclvNode(lclNum, varDsc->TypeGet());
        GenTree* dst = gtNewLclvNode(shadowVarNum, type);

        src->gtFlags |= GTF_DONT_CSE;
        dst->gtFlags |= GTF_DONT_CSE;

        GenTree* opAssign = nullptr;
        if (type == TYP_STRUCT)
        {
            assert(shadowVarDsc->GetLayout() != nullptr);
            assert(shadowVarDsc->lvExactSize != 0);
            opAssign = gtNewBlkOpNode(dst, src, false, true);
        }
        else
        {
            opAssign = gtNewAssignNode(dst, src);
        }
        fgEnsureFirstBBisScratch();
        (void)fgNewStmtAtBeg(fgFirstBB, fgMorphTree(opAssign));
    }

    // If the method has "Jmp CalleeMethod", then we need to copy shadow params back to original
    // params before "jmp" to CalleeMethod.
    if (compJmpOpUsed)
    {
        // There could be more than one basic block ending with a "Jmp" type tail call.
        // We would have to insert assignments in all such blocks, just before GT_JMP stmnt.
        for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
        {
            if (block->bbJumpKind != BBJ_RETURN)
            {
                continue;
            }

            if ((block->bbFlags & BBF_HAS_JMP) == 0)
            {
                continue;
            }

            for (UINT lclNum = 0; lclNum < info.compArgsCount; lclNum++)
            {
                const LclVarDsc* varDsc = &lvaTable[lclNum];

                const unsigned shadowVarNum = gsShadowVarInfo[lclNum].shadowCopy;
                if (shadowVarNum == NO_SHADOW_COPY)
                {
                    continue;
                }

                const LclVarDsc* shadowVarDsc = &lvaTable[shadowVarNum];

                GenTree* src = gtNewLclvNode(shadowVarNum, shadowVarDsc->TypeGet());
                GenTree* dst = gtNewLclvNode(lclNum, varDsc->TypeGet());

                src->gtFlags |= GTF_DONT_CSE;
                dst->gtFlags |= GTF_DONT_CSE;

                GenTree* opAssign = nullptr;
                if (varDsc->TypeGet() == TYP_STRUCT)
                {
                    opAssign = gtNewBlkOpNode(dst, src, false, true);
                }
                else
                {
                    opAssign = gtNewAssignNode(dst, src);
                }

                (void)fgNewStmtNearEnd(block, fgMorphTree(opAssign));
            }
        }
    }
}
