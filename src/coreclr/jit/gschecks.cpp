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

//------------------------------------------------------------------------
// gsPhase: modify IR and symbols to implement stack security checks
//
// Returns:
//    Suitable phase status
//
PhaseStatus Compiler::gsPhase()
{
    bool madeChanges = false;

    if (getNeedsGSSecurityCookie())
    {
        unsigned const prevBBCount = fgBBcount;
        gsGSChecksInitCookie();

        if (compGSReorderStackLayout)
        {
            gsCopyShadowParams();
        }

        madeChanges = true;
    }
    else
    {
        JITDUMP("No GS security needed\n");
    }

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// gsGSChecksInitCookie:
//   Initialize the GS cookie local and value.
//
void Compiler::gsGSChecksInitCookie()
{
    var_types type = TYP_I_IMPL;

    lvaGSSecurityCookie = lvaGrabTempWithImplicitUse(false DEBUGARG("GSSecurityCookie"));

    // Prevent cookie init/check from being optimized
    lvaSetVarAddrExposed(lvaGSSecurityCookie DEBUGARG(AddressExposedReason::TOO_CONSERVATIVE));
    lvaTable[lvaGSSecurityCookie].lvType = type;

    info.compCompHnd->getGSCookie(&gsGlobalSecurityCookieVal, &gsGlobalSecurityCookieAddr);
}

//------------------------------------------------------------------------
// gsCopyShadowParams:
//   The current function has an unsafe buffer on the stack. Search for vulnerable
//   parameters which could be used to modify a code address and take over the process
//   in the case of a buffer overrun. Create a safe local copy for each vulnerable parameter,
//   which will be allocated below the unsafe buffer. Change uses of the param to the
//   shadow copy.
//
//   A pointer under indirection is considered vulnerable. A malicious user could read from
//   protected memory or write to it. If a parameter is assigned/computed into another variable,
//   and is a pointer (i.e., under indirection), then we consider the variable to be part of the
//   equivalence class with the parameter. All parameters in the equivalence class are shadowed.
//
void Compiler::gsCopyShadowParams()
{
    if (info.compIsVarArgs)
    {
        return;
    }

    // Allocate array for shadow param info
    //
    gsShadowVarInfo      = new (this, CMK_Unknown) ShadowParamVarInfo[lvaCount]();
    gsShadowVarInfoCount = lvaCount;

    // Find groups of variables assigned to each other, and also
    // tracks variables which are dereferenced and marks them as ptrs.
    // Look for stores to *p, and ptrs passed to functions
    //
    if (gsFindVulnerableParams())
    {
        // Replace vulnerable params by shadow copies.
        //
        gsParamsToShadows();
    }
    else
    {
        // There are no vulnerable params.
        // Clear out the info to avoid looking at stale data.
        //
        gsShadowVarInfo      = nullptr;
        gsShadowVarInfoCount = 0;
    }
}

//------------------------------------------------------------------------
// gsUnionAssignGroups:
//   Unify the assign groups of lclNum1 and lclNum2, indicating that these may
//   take the same value during the function's execution.
//
// Parameters:
//   lclNum1 - The first local
//   lclNum2 - The second local
//   reason  - The tree that is the reason for the union (for debugging only)
//
void Compiler::gsUnionAssignGroups(unsigned lclNum1, unsigned lclNum2, GenTree* reason)
{
    ShadowParamVarInfo& info1 = gsShadowVarInfo[lclNum1];
    ShadowParamVarInfo& info2 = gsShadowVarInfo[lclNum2];

#ifdef DEBUG
    if (info1.assignGroup != info2.assignGroup)
    {
        JITDUMP("Unifying assign groups of V%02u and V%02u because of [%06u]\n", lclNum1, lclNum2, dspTreeID(reason));
    }
#endif

    if (info1.assignGroup != nullptr)
    {
        if (info2.assignGroup != nullptr)
        {
            info1.assignGroup->bitVectOr(info2.assignGroup);
        }
        else
        {
            info1.assignGroup->bitVectSet(lclNum2);
        }

        // Point both to the same bit vector
        info2.assignGroup = info1.assignGroup;
    }
    else if (info2.assignGroup != nullptr)
    {
        info2.assignGroup->bitVectSet(lclNum1);

        // Point both to the same bit vector
        info1.assignGroup = info2.assignGroup;
    }
    else
    {
        FixedBitVect* bv = FixedBitVect::bitVectInit(lvaCount, this);

        // Neither of them has an assign group yet. Make a new one.
        info1.assignGroup = bv;
        info2.assignGroup = bv;
        bv->bitVectSet(lclNum1);
        bv->bitVectSet(lclNum2);
    }
}

//------------------------------------------------------------------------
// gsVisitDependentLocals:
//   Visit the local number of all locals that a specified node's value may
//   depend on.
//
// Parameters:
//   node - The node
//   visit - A functor void(unsigned lclNum) called for each local number that
//           directly contributes to node's value.
//
template <typename TVisit>
void Compiler::gsVisitDependentLocals(GenTree* node, TVisit visit)
{
    class Visitor : public GenTreeVisitor<Visitor>
    {
        TVisit& m_visit;
    public:
        enum
        {
            DoPreOrder = true,
        };

        Visitor(Compiler* compiler, TVisit& visit)
            : GenTreeVisitor<Visitor>(compiler)
            , m_visit(visit)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* node = *use;
            if (node->OperIs(GT_IND, GT_BLK, GT_MDARR_LENGTH, GT_MDARR_LOWER_BOUND, GT_CALL))
            {
                return WALK_SKIP_SUBTREES;
            }

            if ((user != nullptr) && user->OperIs(GT_SELECT) && (node == user->AsConditional()->gtCond))
            {
                // The condition of a select does not contribute to the value
                return WALK_SKIP_SUBTREES;
            }

            if (node->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                m_visit(node->AsLclVarCommon()->GetLclNum());
            }

            return WALK_CONTINUE;
        }
    };

    Visitor visitor(this, visit);
    visitor.WalkTree(&node, nullptr);
}

//------------------------------------------------------------------------
// gsMarkPointers:
//   Mark that dependent locals of the specified tree are pointers.
//
// Parameters:
//   tree - The tree, typically an indirection
//
void Compiler::gsMarkPointers(GenTree* tree)
{
    gsVisitDependentLocals(tree, [=](unsigned lclNum) {
        LclVarDsc* varDsc = lvaGetDesc(lclNum);

#ifdef DEBUG
        if (!varDsc->lvIsPtr)
        {
            JITDUMP("Marking V%02u as a pointer because of [%06u]\n", lclNum, dspTreeID(tree));
        }
#endif

        varDsc->lvIsPtr = 1;
    });
}

//------------------------------------------------------------------------
// gsFindVulnerableParams:
//   Walk all the trees looking for ptrs, args, assign groups, *p stores, etc.
//   Then use that info to figure out vulnerable pointers.
//
// Returns:
//   Returns true if it found atleast one vulnerable pointer parameter that
//   needs to be shadow-copied.
//
bool Compiler::gsFindVulnerableParams()
{
    for (BasicBlock* block : Blocks())
    {
        for (GenTree* node : LIR::AsRange(block))
        {
            switch (node->OperGet())
            {
                case GT_IND:
                case GT_BLK:
                case GT_MDARR_LENGTH:
                case GT_MDARR_LOWER_BOUND:
                case GT_STOREIND:
                case GT_STORE_BLK:
                {
                    gsMarkPointers(node->gtGetOp1());
                    break;
                }
                case GT_STORE_LCL_VAR:
                case GT_STORE_LCL_FLD:
                {
                    GenTreeLclVarCommon* lcl = node->AsLclVarCommon();
                    gsVisitDependentLocals(lcl->Data(), [=](unsigned lclNum) {
                        gsUnionAssignGroups(lcl->GetLclNum(), lclNum, lcl);
                    });

                    break;
                }
                case GT_CALL:
                {
                    CallArg* thisArg = node->AsCall()->gtArgs.GetThisArg();
                    if (thisArg != nullptr)
                    {
                        gsMarkPointers(thisArg->GetNode());
                    }

                    if (node->AsCall()->gtCallType == CT_INDIRECT)
                    {
                        // A function pointer is treated like a write-through pointer since
                        // it controls what code gets executed, and so indirectly can cause
                        // a write to memory.
                        gsMarkPointers(node->AsCall()->gtCallAddr);
                    }

                    break;
                }
                default:
                    break;
            }
        }
    }

    // Compute has vulnerable at the end of the loop.
    bool hasOneVulnerable = false;

    // Initialize propagated[v0...vn] = {0}^n, so we can skip the ones propagated through
    // some assign group.
    FixedBitVect* propagated = (lvaCount > 0) ? FixedBitVect::bitVectInit(lvaCount, this) : nullptr;

    for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++)
    {
        LclVarDsc*          varDsc     = lvaGetDesc(lclNum);
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
        bool isUnderIndir = varDsc->lvIsPtr;

        // First pass -- find if any variable is vulnerable.
        FixedBitVect* assignGroup = shadowInfo->assignGroup;
        for (unsigned lclNum = assignGroup->bitVectGetFirst(); lclNum != (unsigned)-1 && !isUnderIndir;
             lclNum          = assignGroup->bitVectGetNext(lclNum))
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
        for (unsigned lclNum = assignGroup->bitVectGetFirst(); lclNum != (unsigned)-1;
             lclNum          = assignGroup->bitVectGetNext(lclNum))
        {
            lvaTable[lclNum].lvIsPtr = true;
            propagated->bitVectSet(lclNum);
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("Equivalence assign group %s: ", isUnderIndir ? "isPtr " : "");
            for (unsigned lclNum = assignGroup->bitVectGetFirst(); lclNum != (unsigned)-1;
                 lclNum          = assignGroup->bitVectGetNext(lclNum))
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
// gsParamsToShadows:
//   Copy each vulnerable param ptr or buffer to a local shadow copy and
//   replace uses of the param by the shadow copy.
//
void Compiler::gsParamsToShadows()
{
    // Create the locals that shadow the parameters
    if (!gsCreateShadowingLocals())
    {
        return;
    }

    // Redirect uses in the IR to the shadowed versions.
    for (BasicBlock* block : Blocks())
    {
        for (GenTree* tree : LIR::AsRange(block))
        {
            gsRewriteTreeForShadowParam(tree);
        }
    }

    // Now insert code to copy the params to their shadow copy at the beginning of the function.
    for (unsigned lclNum = 0; lclNum < gsShadowVarInfoCount; lclNum++)
    {
        const LclVarDsc* varDsc = lvaGetDesc(lclNum);

        const unsigned shadowLclNum = gsShadowVarInfo[lclNum].shadowCopy;
        if (shadowLclNum == BAD_VAR_NUM)
        {
            continue;
        }

        gsCopyIntoShadow(lclNum, shadowLclNum);
    }

    // If the method has "Jmp CalleeMethod", then we need to copy shadow params back to original
    // params before "jmp" to CalleeMethod.
    if (compJmpOpUsed)
    {
        // There could be more than one basic block ending with a "Jmp" type tail call.
        // We have to insert stores in all such blocks, just before GT_JMP stmnt.
        for (BasicBlock* const block : Blocks())
        {
            if (!block->KindIs(BBJ_RETURN))
            {
                continue;
            }

            GenTree* lastNode = LIR::AsRange(block).LastNode();
            if ((lastNode == nullptr) || !lastNode->OperIs(GT_JMP))
            {
                continue;
            }

            for (unsigned lclNum = 0; lclNum < info.compArgsCount; lclNum++)
            {
                const LclVarDsc* varDsc = lvaGetDesc(lclNum);

                const unsigned shadowVarNum = gsShadowVarInfo[lclNum].shadowCopy;
                if (shadowVarNum == BAD_VAR_NUM)
                {
                    continue;
                }

                GenTree* src   = gtNewLclVarNode(shadowVarNum);
                GenTree* store = gtNewStoreLclVarNode(lclNum, src);

                LIR::AsRange(block).InsertBefore(lastNode, LIR::SeqTree(this, store));
            }
        }
    }
}

//-------------------------------------------------------------------------------
// gsCreateShadowingLocals:
//   For each parameter that should be shadowed, create a local variable to hold its copy.
//
// Returns:
//   True if any shadowing locals were created.
//
bool Compiler::gsCreateShadowingLocals()
{
    bool createdAny = false;
    // Create shadow copy for each param candidate
    for (unsigned lclNum = 0; lclNum < gsShadowVarInfoCount; lclNum++)
    {
        LclVarDsc* varDsc                  = lvaGetDesc(lclNum);
        gsShadowVarInfo[lclNum].shadowCopy = BAD_VAR_NUM;

        // Only care about params whose values are on the stack
        if (!ShadowParamVarInfo::mayNeedShadowCopy(varDsc))
        {
            continue;
        }

        if (!varDsc->lvIsPtr && !varDsc->lvIsUnsafeBuffer)
        {
            continue;
        }

        int shadowVarNum = lvaGrabTemp(false DEBUGARG(printfAlloc("V%02u shadow", lclNum)));
        // reload varDsc as lvaGrabTemp may realloc the lvaTable[]
        varDsc                  = lvaGetDesc(lclNum);
        LclVarDsc* shadowVarDsc = lvaGetDesc(shadowVarNum);

        // Copy some info
        var_types type            = varTypeIsSmall(varDsc->TypeGet()) ? TYP_INT : varDsc->TypeGet();
        shadowVarDsc->lvType      = type;
        shadowVarDsc->lvRegStruct = varDsc->lvRegStruct;
        shadowVarDsc->SetAddressExposed(varDsc->IsAddressExposed() DEBUGARG(varDsc->GetAddrExposedReason()));
        shadowVarDsc->lvDoNotEnregister       = varDsc->lvDoNotEnregister;
        shadowVarDsc->lvSingleDefRegCandidate = varDsc->lvSingleDefRegCandidate;
        // The old variable will not be used in handlers anymore, allow it to stay enregistered
        varDsc->lvLiveInOutOfHndlr = false;
#ifdef DEBUG
        shadowVarDsc->SetDoNotEnregReason(varDsc->GetDoNotEnregReason());
        shadowVarDsc->SetDefinedViaAddress(varDsc->IsDefinedViaAddress());
#endif

        if (varTypeIsStruct(type))
        {
            // We don't need unsafe value cls check here since we are copying the params and this flag
            // would have been set on the original param before reaching here.
            lvaSetStruct(shadowVarNum, varDsc->GetLayout(), false);
            shadowVarDsc->lvIsMultiRegArg  = varDsc->lvIsMultiRegArg;
            shadowVarDsc->lvIsMultiRegRet  = varDsc->lvIsMultiRegRet;
            shadowVarDsc->lvIsMultiRegDest = varDsc->lvIsMultiRegDest;
        }
        shadowVarDsc->lvIsUnsafeBuffer = varDsc->lvIsUnsafeBuffer;
        shadowVarDsc->lvIsPtr          = varDsc->lvIsPtr;

        if (varDsc->IsNeverNegative())
        {
            shadowVarDsc->SetIsNeverNegative(true);
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("Var V%02u is shadow param candidate. Shadow copy is V%02u.\n", lclNum, shadowVarNum);
        }
#endif

        gsShadowVarInfo[lclNum].shadowCopy = shadowVarNum;
        createdAny                         = true;
    }

    return createdAny;
}

//-------------------------------------------------------------------------------
// gsRewriteTreeForShadowParam:
//   If necessary, rewrite the given tree to act on the shadowed version of a local.
//
// Parameters:
//   tree - The tree
//
void Compiler::gsRewriteTreeForShadowParam(GenTree* tree)
{
    if (!tree->OperIsAnyLocal())
    {
        return;
    }

    unsigned int lclNum = tree->AsLclVarCommon()->GetLclNum();
    if (lclNum >= gsShadowVarInfoCount)
    {
        return;
    }

    unsigned int shadowLclNum = gsShadowVarInfo[lclNum].shadowCopy;
    if (shadowLclNum == BAD_VAR_NUM)
    {
        return;
    }

    LclVarDsc* varDsc = lvaGetDesc(lclNum);
    assert(ShadowParamVarInfo::mayNeedShadowCopy(varDsc));

    tree->AsLclVarCommon()->SetLclNum(shadowLclNum);

    if (varTypeIsSmall(varDsc))
    {
        if (tree->OperIsScalarLocal())
        {
            tree->gtType = TYP_INT;
        }
        else if (tree->OperIs(GT_STORE_LCL_FLD) && tree->IsPartialLclFld(this))
        {
            tree->gtFlags |= GTF_VAR_USEASG;
        }
    }
}

//-------------------------------------------------------------------------------
// gsCopyIntoShadow:
//   Insert IR to copy the value of "lclNum" into "shadowLclNum" at the
//   beginning of the function.
//
// Parameters:
//   lclNum         - The original vulnerable local
//   shadowLclNum   - The shadowing local
//
void Compiler::gsCopyIntoShadow(unsigned lclNum, unsigned shadowLclNum)
{
    LclVarDsc* varDsc = lvaGetDesc(lclNum);
    if (varDsc->lvPromoted && !varDsc->lvDoNotEnregister)
    {
        lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::BlockOp));
    }

#if defined(TARGET_X86) && defined(FEATURE_IJW)
    if (lclNum < info.compArgsCount && argRequiresSpecialCopy(lclNum) && varDsc->TypeIs(TYP_STRUCT))
    {
        JITDUMP("arg%02u requires special copy, using special copy helper to copy to shadow var V%02u\n", lclNum,
                shadowLclNum);
        CORINFO_METHOD_HANDLE copyHelper =
            info.compCompHnd->getSpecialCopyHelper(varDsc->GetLayout()->GetClassHandle());
        GenTreeCall* call = gtNewCallNode(CT_USER_FUNC, copyHelper, TYP_VOID);

        GenTree* src = gtNewLclVarAddrNode(lclNum);
        GenTree* dst = gtNewLclVarAddrNode(shadowLclNum);

        call->gtArgs.PushBack(this, NewCallArg::Primitive(dst));
        call->gtArgs.PushBack(this, NewCallArg::Primitive(src));

        compCurBB = fgFirstBB; // Needed by some morphing
        fgMorphTree(call);
        compCurBB = nullptr;

        // Insert the IR
        if (opts.IsReversePInvoke())
        {
            GenTree* insertAfter = nullptr;
            // If we are in a reverse P/Invoke then insert after the GC transition.
            //
            // TODO-Cleanup: We should be inserting reverse pinvoke transitions way
            // later in the JIT to avoid having to search like this.

            for (GenTree* node : LIR::AsRange(fgFirstBB))
            {
                if (node->IsHelperCall(this, CORINFO_HELP_JIT_REVERSE_PINVOKE_ENTER) ||
                    node->IsHelperCall(this, CORINFO_HELP_JIT_REVERSE_PINVOKE_ENTER_TRACK_TRANSITIONS))
                {
                    insertAfter = node;
                    break;
                }
            }

            noway_assert(insertAfter != nullptr);

            JITDUMP("Inserting IR after Reverse P/Invoke transition [%06u]\n", dspTreeID(insertAfter));
            LIR::AsRange(fgFirstBB).InsertAfter(insertAfter, LIR::SeqTree(this, call));
            DISPTREERANGE(LIR::AsRange(fgFirstBB), call);
        }
        else
        {
            LIR::AsRange(fgFirstBB).InsertAtBeginning(LIR::SeqTree(this, call));
            DISPTREERANGE(LIR::AsRange(fgFirstBB), call);
        }

        return;
    }
#endif

    GenTree* src   = gtNewLclvNode(lclNum, varDsc->TypeGet());
    GenTree* store = gtNewStoreLclVarNode(shadowLclNum, src);

    LIR::AsRange(fgFirstBB).InsertAtBeginning(src, store);
    JITDUMP("Created shadow param copy for V%02u to V%02u\n", lclNum, shadowLclNum);
    DISPTREERANGE(LIR::AsRange(fgFirstBB), store);
}
