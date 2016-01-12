//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

    lvaGSSecurityCookie = lvaGrabTemp(false DEBUGARG("GSSecurityCookie"));

    // Prevent cookie init/check from being optimized
    lvaSetVarAddrExposed(lvaGSSecurityCookie);
    lvaTable[lvaGSSecurityCookie].lvType = type;

    info.compCompHnd->getGSCookie(&gsGlobalSecurityCookieVal, 
                                  &gsGlobalSecurityCookieAddr);
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
    Compiler *  comp;
    unsigned    lvAssignDef; // Which local variable is the tree being assigned to?
    bool        isAssignSrc; // Is this the source value for an assignment?
    bool        isUnderIndir; // Is this a pointer value tree that is being dereferenced?
    bool        skipNextNode; // Skip a single node during the tree-walk

#ifdef DEBUG
    void Print()
    {
        printf("[MarkPtrsInfo] = {comp = %p, lvAssignDef = %d, isAssignSrc = %d, isUnderIndir = %d, skipNextNode = %d}\n",
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
Compiler::fgWalkResult Compiler::gsMarkPtrsAndAssignGroups(GenTreePtr *pTree, fgWalkData *data)
{
    struct MarkPtrsInfo *pState= (MarkPtrsInfo *)data->pCallbackData;
    struct MarkPtrsInfo newState = *pState;
    Compiler *comp = data->compiler;
    GenTreePtr tree = *pTree;
    ShadowParamVarInfo *shadowVarInfo = pState->comp->gsShadowVarInfo;
    assert(shadowVarInfo);
    bool fIsBlk = false;
    unsigned lclNum;

    assert(!pState->isAssignSrc || pState->lvAssignDef != (unsigned)-1);

    if (pState->skipNextNode)
    {
        pState->skipNextNode = false;
        return WALK_CONTINUE;
    }

    switch (tree->OperGet())
    {
    // Indirections - look for *p uses and defs
    case GT_INITBLK:
    case GT_COPYOBJ:
    case GT_COPYBLK:
        fIsBlk = true;
        // fallthrough
    case GT_IND:
    case GT_LDOBJ:
    case GT_ARR_ELEM:
    case GT_ARR_INDEX:
    case GT_ARR_OFFSET:
    case GT_FIELD:

        newState.isUnderIndir = true;
        {
            if (fIsBlk)
            {
                // Blk nodes have implicit indirections.
                comp->fgWalkTreePre(&tree->gtOp.gtOp1, comp->gsMarkPtrsAndAssignGroups, (void *)&newState);

                if (tree->OperGet() == GT_INITBLK)
                {
                    newState.isUnderIndir = false;
                }
                comp->fgWalkTreePre(&tree->gtOp.gtOp2, comp->gsMarkPtrsAndAssignGroups, (void *)&newState);
            }
            else
            {
                newState.skipNextNode = true;  // Don't have to worry about which kind of node we're dealing with
                comp->fgWalkTreePre(&tree, comp->gsMarkPtrsAndAssignGroups, (void *)&newState);
            }
        }

        return WALK_SKIP_SUBTREES;

    // local vars and param uses
    case GT_LCL_VAR:
    case GT_LCL_FLD:
        lclNum = tree->gtLclVarCommon.gtLclNum;

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
                FixedBitVect *bv = FixedBitVect::bitVectInit(pState->comp->lvaCount, pState->comp);

                // (shadowVarInfo[pState->lvAssignDef] == NULL && shadowVarInfo[lclNew] == NULL);
                // Neither of them has an assign group yet.  Make a new one.
                shadowVarInfo[pState->lvAssignDef].assignGroup = bv;
                shadowVarInfo[lclNum].assignGroup = bv;
                bv->bitVectSet(pState->lvAssignDef);
                bv->bitVectSet(lclNum);
            }

        }
        return WALK_CONTINUE;
    
    // Calls - Mark arg variables
    case GT_CALL:

        newState.isUnderIndir = false;
        newState.isAssignSrc = false;
        {
            if (tree->gtCall.gtCallObjp)
            {
                newState.isUnderIndir = true;
                comp->fgWalkTreePre(&tree->gtCall.gtCallObjp, gsMarkPtrsAndAssignGroups, (void *)&newState);
            }

            for (GenTreeArgList* args = tree->gtCall.gtCallArgs; args; args = args->Rest())
            {
                comp->fgWalkTreePre(&args->Current(), gsMarkPtrsAndAssignGroups, (void *)&newState);
            }
            for (GenTreeArgList* args = tree->gtCall.gtCallLateArgs; args; args = args->Rest())
            {
                comp->fgWalkTreePre(&args->Current(), gsMarkPtrsAndAssignGroups, (void *)&newState);
            }

            if (tree->gtCall.gtCallType == CT_INDIRECT)
            {
                newState.isUnderIndir = true;

                // A function pointer is treated like a write-through pointer since
                // it controls what code gets executed, and so indirectly can cause
                // a write to memory.
                comp->fgWalkTreePre(&tree->gtCall.gtCallAddr, gsMarkPtrsAndAssignGroups, (void *)&newState);
            }
        }
        return WALK_SKIP_SUBTREES;


    case GT_ADDR:
        newState.isUnderIndir = false;
        // We'll assume p in "**p = " can be vulnerable because by changing 'p', someone
        // could control where **p stores to.
        {
            comp->fgWalkTreePre(&tree->gtOp.gtOp1, comp->gsMarkPtrsAndAssignGroups, (void *)&newState);
        }
        return WALK_SKIP_SUBTREES;


    default:
        // Assignments - track assign groups and *p defs.
        if (tree->OperIsAssignment())
        {
            bool isLocVar;
            bool isLocFld;

            // Walk dst side
            comp->fgWalkTreePre(&tree->gtOp.gtOp1, comp->gsMarkPtrsAndAssignGroups, (void *)&newState);
            
            // Now handle src side
            isLocVar = tree->gtOp.gtOp1->OperGet() == GT_LCL_VAR;
            isLocFld = tree->gtOp.gtOp1->OperGet() == GT_LCL_FLD;

            if ((isLocVar || isLocFld) && tree->gtOp.gtOp2)
            {
                lclNum = tree->gtOp.gtOp1->gtLclVarCommon.gtLclNum;
                newState.lvAssignDef = lclNum;
                newState.isAssignSrc = true;
            }

            comp->fgWalkTreePre(&tree->gtOp.gtOp2, comp->gsMarkPtrsAndAssignGroups, (void *)&newState);

            return WALK_SKIP_SUBTREES;
        }
    }

    return WALK_CONTINUE;
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

    info.comp = this;
    info.lvAssignDef = (unsigned)-1;
    info.isUnderIndir = false;
    info.isAssignSrc = false;
    info.skipNextNode = false;

    // Walk all the trees setting lvIsWritePtr, lvIsOutgoingArg, lvIsPtr and assignGroup.
    fgWalkAllTreesPre(gsMarkPtrsAndAssignGroups, &info);

    // Compute has vulnerable at the end of the loop.
    bool hasOneVulnerable = false;

    // Initialize propagated[v0...vn] = {0}^n, so we can skip the ones propagated through
    // some assign group.
    FixedBitVect* propagated = (lvaCount > 0) ? FixedBitVect::bitVectInit(lvaCount, this) : NULL;

    for (UINT lclNum = 0; lclNum < lvaCount; lclNum++)
    {
        LclVarDsc *varDsc = &lvaTable[lclNum];
        ShadowParamVarInfo *shadowInfo = &gsShadowVarInfo[lclNum];

        // If there was an indirection or if unsafe buffer, then we'd call it vulnerable.
        if (varDsc->lvIsPtr || varDsc->lvIsUnsafeBuffer)
        {
            hasOneVulnerable = true;
        }

        // Now, propagate the info through the assign group (an equivalence class of vars transitively assigned.)
        if (shadowInfo->assignGroup == NULL || propagated->bitVectTest(lclNum))
        {
            continue;
        }

        // Propagate lvIsPtr, so that:
        //   1. Any parameter in the equivalence class can be identified as lvIsPtr and hence shadowed.
        //   2. Buffers with pointers are placed at lower memory addresses than buffers without pointers.
        UINT isUnderIndir = varDsc->lvIsPtr;

        // First pass -- find if any variable is vulnerable.
        FixedBitVect *assignGroup = shadowInfo->assignGroup;      
        for (UINT lclNum = assignGroup->bitVectGetFirst();
             lclNum != (unsigned) -1 && !isUnderIndir;
             lclNum = assignGroup->bitVectGetNext(lclNum))
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
        for (UINT lclNum = assignGroup->bitVectGetFirst();
             lclNum != (unsigned) -1;
             lclNum = assignGroup->bitVectGetNext(lclNum))
        {
            lvaTable[lclNum].lvIsPtr = TRUE;
            propagated->bitVectSet(lclNum);
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("Equivalence assign group %s: ",  isUnderIndir ? "isPtr " : "");
            for (UINT lclNum = assignGroup->bitVectGetFirst();
                 lclNum != (unsigned) -1;
                 lclNum = assignGroup->bitVectGetNext(lclNum))
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


/*****************************************************************************
 * gsParamsToShadows
 * Copy each vulnerable param ptr or buffer to a local shadow copy and replace
 * uses of the param by the shadow copy
 */
void Compiler::gsParamsToShadows()
{
    // Cache old count since we'll add new variables, and  
    // gsShadowVarInfo will not grow to accomodate the new ones.
    UINT lvaOldCount = lvaCount;

    // Create shadow copy for each param candidate
    for (UINT lclNum = 0; lclNum < lvaOldCount; lclNum++)
    {
        LclVarDsc *varDsc = &lvaTable[lclNum];
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


        int shadowVar = lvaGrabTemp(false DEBUGARG("shadowVar"));
        // Copy some info

        var_types type = varTypeIsSmall(varDsc->TypeGet()) ? TYP_INT : varDsc->TypeGet();
        lvaTable[shadowVar].lvType        = type;
        lvaTable[shadowVar].lvAddrExposed = varDsc->lvAddrExposed;
        lvaTable[shadowVar].lvDoNotEnregister = varDsc->lvDoNotEnregister;
#ifdef DEBUG
        lvaTable[shadowVar].lvVMNeedsStackAddr = varDsc->lvVMNeedsStackAddr;
        lvaTable[shadowVar].lvLiveInOutOfHndlr = varDsc->lvLiveInOutOfHndlr;
        lvaTable[shadowVar].lvLclFieldExpr = varDsc->lvLclFieldExpr;
        lvaTable[shadowVar].lvLiveAcrossUCall = varDsc->lvLiveAcrossUCall;
#endif
        lvaTable[shadowVar].lvVerTypeInfo = varDsc->lvVerTypeInfo;
        lvaTable[shadowVar].lvGcLayout    = varDsc->lvGcLayout;
        lvaTable[shadowVar].lvIsUnsafeBuffer = varDsc->lvIsUnsafeBuffer;
        lvaTable[shadowVar].lvIsPtr       = varDsc->lvIsPtr;

#ifdef  DEBUG
        if (verbose)
        {
            printf("Var V%02u is shadow param candidate. Shadow copy is V%02u.\n", lclNum, shadowVar);
        }
#endif

        gsShadowVarInfo[lclNum].shadowCopy = shadowVar;
    }

    // Replace param uses with shadow copy
    fgWalkAllTreesPre(gsReplaceShadowParams, (void *)this);

    // Now insert code to copy the params to their shadow copy.
    for (UINT lclNum = 0; lclNum < lvaOldCount; lclNum++)
    {
        LclVarDsc *varDsc = &lvaTable[lclNum];

        unsigned shadowVar = gsShadowVarInfo[lclNum].shadowCopy;
        if (shadowVar == NO_SHADOW_COPY)
        {
            continue;
        }

        var_types type = lvaTable[shadowVar].TypeGet();

        GenTreePtr src = gtNewLclvNode(lclNum, varDsc->TypeGet());
        GenTreePtr dst = gtNewLclvNode(shadowVar, type);

        src->gtFlags |= GTF_DONT_CSE;
        dst->gtFlags |= GTF_DONT_CSE;

        GenTreePtr opAssign = NULL;
        if (type == TYP_STRUCT)
        {
            CORINFO_CLASS_HANDLE clsHnd = varDsc->lvVerTypeInfo.GetClassHandle();

            // We don't need unsafe value cls check here since we are copying the params and this flag
            // would have been set on the original param before reaching here.
            lvaSetStruct(shadowVar, clsHnd, false);

            src = gtNewOperNode(GT_ADDR, TYP_BYREF, src);
            dst = gtNewOperNode(GT_ADDR, TYP_BYREF, dst);

            opAssign = gtNewCpObjNode(dst, src, clsHnd, false);
#if FEATURE_MULTIREG_STRUCTS
            lvaTable[shadowVar].lvDontPromote = lvaTable[lclNum].lvDontPromote;
#endif // FEATURE_MULTIREG_STRUCTS
        }
        else
        {
            opAssign = gtNewAssignNode(dst, src);
        }
        fgEnsureFirstBBisScratch();
        (void) fgInsertStmtAtBeg(fgFirstBB, fgMorphTree(opAssign));
    }

    // If the method has "Jmp CalleeMethod", then we need to copy shadow params back to original
    // params before "jmp" to CalleeMethod.
    if (compJmpOpUsed)
    {
        // There could be more than one basic block ending with a "Jmp" type tail call.
        // We would have to insert assignments in all such blocks, just before GT_JMP stmnt.
        for (BasicBlock * block = fgFirstBB; block; block = block->bbNext)
        {
            if (block->bbJumpKind != BBJ_RETURN)
            {
                continue;
            }

            if  ((block->bbFlags & BBF_HAS_JMP) == 0) 
            {
                continue;
            }

            for (UINT lclNum = 0; lclNum < info.compArgsCount; lclNum++)
            {
                LclVarDsc *varDsc = &lvaTable[lclNum];

                unsigned shadowVar = gsShadowVarInfo[lclNum].shadowCopy;
                if (shadowVar == NO_SHADOW_COPY)
                {
                    continue;
                }

                GenTreePtr src = gtNewLclvNode(shadowVar, lvaTable[shadowVar].TypeGet());
                GenTreePtr dst = gtNewLclvNode(lclNum, varDsc->TypeGet());
                
                src->gtFlags |= GTF_DONT_CSE;
                dst->gtFlags |= GTF_DONT_CSE;

                GenTreePtr opAssign = nullptr;
                if (varDsc->TypeGet() == TYP_STRUCT)
                {
                    CORINFO_CLASS_HANDLE clsHnd = varDsc->lvVerTypeInfo.GetClassHandle();
                    src = gtNewOperNode(GT_ADDR, TYP_BYREF, src);
                    dst = gtNewOperNode(GT_ADDR, TYP_BYREF, dst);

                    opAssign = gtNewCpObjNode(dst, src, clsHnd, false);
                }
                else
                {
                    opAssign = gtNewAssignNode(dst, src);
                }
                
                (void) fgInsertStmtNearEnd(block, fgMorphTree(opAssign));
            }

        }
    }
}


/*****************************************************************************
 * gsReplaceShadowParams (tree-walk call-back)
 * Replace all vulnerable param uses by it's shadow copy.
 */

Compiler::fgWalkResult      Compiler::gsReplaceShadowParams(GenTreePtr * pTree, fgWalkData *data)
{
    Compiler *      comp = data->compiler;
    GenTreePtr      tree = *pTree;
    GenTreePtr      asg  = NULL; 

    if (tree->gtOper == GT_ASG)
    {
        asg  = tree;              // "asg" is the assignment tree.
        tree = tree->gtOp.gtOp1;  // "tree" is the local var tree at the left-hand size of the assignment.
    }        
    
    if (tree->gtOper == GT_LCL_VAR || tree->gtOper == GT_LCL_FLD)
    {
        UINT paramNum = tree->gtLclVarCommon.gtLclNum;

        if (!ShadowParamVarInfo::mayNeedShadowCopy(&comp->lvaTable[paramNum]) ||
            comp->gsShadowVarInfo[paramNum].shadowCopy == NO_SHADOW_COPY)
        {
            return WALK_CONTINUE;
        }

        tree->gtLclVarCommon.SetLclNum(comp->gsShadowVarInfo[paramNum].shadowCopy);

        // In gsParamsToShadows(), we create a shadow var of TYP_INT for every small type param.
        // Make sure we update the type of the local var tree as well.
        if (varTypeIsSmall(comp->lvaTable[paramNum].TypeGet()))
        {
            tree->gtType = TYP_INT;
            if (asg) 
            {
                // If this is an assignment tree, propagate the type to it as well.
                asg->gtType = TYP_INT;
            }
        }
    }

    return WALK_CONTINUE;
}

