// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                                  ScopeInfo                                XX
XX                                                                           XX
XX   Classes to gather the Scope information from the local variable info.   XX
XX   Translates the given LocalVarTab from IL instruction offsets into       XX
XX   native code offsets.                                                    XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/******************************************************************************
 *                                  Debuggable code
 *
 *  We break up blocks at the start and end IL ranges of the local variables.
 *  This is because IL offsets do not correspond exactly to native offsets
 *  except at block boundaries. No basic-blocks are deleted (not even
 *  unreachable), so there will not be any missing address-ranges, though the
 *  blocks themselves may not be ordered. (Also, internal blocks may be added).
 *  o At the start of each basic block, siBeginBlock() checks if any variables
 *    are coming in scope, and adds an open scope to siOpenScopeList if needed.
 *  o At the end of each basic block, siEndBlock() checks if any variables
 *    are going out of scope and moves the open scope from siOpenScopeLast
 *    to siScopeList.
 *
 *                                  Optimized code
 *
 *  We cannot break up the blocks as this will produce different code under
 *  the debugger. Instead we try to do a best effort.
 *  o At the start of each basic block, siBeginBlock() adds open scopes
 *    corresponding to block->bbLiveIn to siOpenScopeList. Also siUpdate()
 *    is called to close scopes for variables which are not live anymore.
 *  o siEndBlock() closes scopes for any variables which go out of range
 *    before bbCodeOffsEnd.
 *  o siCloseAllOpenScopes() closes any open scopes after all the blocks.
 *    This should only be needed if some basic block are deleted/out of order,
 *    etc.
 *  Also,
 *  o At every assignment to a variable, siCheckVarScope() adds an open scope
 *    for the variable being assigned to.
 *  o genChangeLife() calls siUpdate() which closes scopes for variables which
 *    are not live anymore.
 *
 ******************************************************************************
 */

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "emit.h"
#include "codegen.h"

/*****************************************************************************/
#ifdef DEBUGGING_SUPPORT
/*****************************************************************************/

bool Compiler::siVarLoc::vlIsInReg(regNumber reg)
{
    switch (vlType)
    {
        case VLT_REG:
            return (vlReg.vlrReg == reg);
        case VLT_REG_REG:
            return ((vlRegReg.vlrrReg1 == reg) || (vlRegReg.vlrrReg2 == reg));
        case VLT_REG_STK:
            return (vlRegStk.vlrsReg == reg);
        case VLT_STK_REG:
            return (vlStkReg.vlsrReg == reg);

        case VLT_STK:
        case VLT_STK2:
        case VLT_FPSTK:
            return false;

        default:
            assert(!"Bad locType");
            return false;
    }
}

bool Compiler::siVarLoc::vlIsOnStk(regNumber reg, signed offset)
{
    regNumber actualReg;

    switch (vlType)
    {

        case VLT_REG_STK:
            actualReg = vlRegStk.vlrsStk.vlrssBaseReg;
            if ((int)actualReg == (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                actualReg = REG_SPBASE;
            }
            return ((actualReg == reg) && (vlRegStk.vlrsStk.vlrssOffset == offset));
        case VLT_STK_REG:
            actualReg = vlStkReg.vlsrStk.vlsrsBaseReg;
            if ((int)actualReg == (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                actualReg = REG_SPBASE;
            }
            return ((actualReg == reg) && (vlStkReg.vlsrStk.vlsrsOffset == offset));
        case VLT_STK:
            actualReg = vlStk.vlsBaseReg;
            if ((int)actualReg == (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                actualReg = REG_SPBASE;
            }
            return ((actualReg == reg) && (vlStk.vlsOffset == offset));
        case VLT_STK2:
            actualReg = vlStk2.vls2BaseReg;
            if ((int)actualReg == (int)ICorDebugInfo::REGNUM_AMBIENT_SP)
            {
                actualReg = REG_SPBASE;
            }
            return ((actualReg == reg) && ((vlStk2.vls2Offset == offset) || (vlStk2.vls2Offset == (offset - 4))));

        case VLT_REG:
        case VLT_REG_FP:
        case VLT_REG_REG:
        case VLT_FPSTK:
            return false;

        default:
            assert(!"Bad locType");
            return false;
    }
}

/*============================================================================
 *
 *              Implementation for ScopeInfo
 *
 *
 * Whenever a variable comes into scope, add it to the list.
 * When a varDsc goes dead, end its previous scope entry, and make a new one
 * which is unavailable.
 * When a varDsc goes live, end its previous un-available entry (if any) and
 * set its new entry as available.
 *
 *============================================================================
 */

/*****************************************************************************
 *                      siNewScope
 *
 * Creates a new scope and adds it to the Open scope list.
 */

CodeGen::siScope* CodeGen::siNewScope(unsigned LVnum, unsigned varNum)
{
    bool     tracked  = compiler->lvaTable[varNum].lvTracked;
    unsigned varIndex = compiler->lvaTable[varNum].lvVarIndex;

    if (tracked)
    {
        siEndTrackedScope(varIndex);
    }

    siScope* newScope = (siScope*)compiler->compGetMem(sizeof(*newScope), CMK_SiScope);

    newScope->scStartLoc.CaptureLocation(getEmitter());
    assert(newScope->scStartLoc.Valid());

    newScope->scEndLoc.Init();

    newScope->scLVnum      = LVnum;
    newScope->scVarNum     = varNum;
    newScope->scNext       = nullptr;
    newScope->scStackLevel = genStackLevel; // used only by stack vars

    siOpenScopeLast->scNext = newScope;
    newScope->scPrev        = siOpenScopeLast;
    siOpenScopeLast         = newScope;

    if (tracked)
    {
        siLatestTrackedScopes[varIndex] = newScope;
    }

    return newScope;
}

/*****************************************************************************
 *                          siRemoveFromOpenScopeList
 *
 * Removes a scope from the open-scope list and puts it into the done-scope list
 */

void CodeGen::siRemoveFromOpenScopeList(CodeGen::siScope* scope)
{
    assert(scope);
    assert(scope->scEndLoc.Valid());

    // Remove from open-scope list

    scope->scPrev->scNext = scope->scNext;
    if (scope->scNext)
    {
        scope->scNext->scPrev = scope->scPrev;
    }
    else
    {
        siOpenScopeLast = scope->scPrev;
    }

    // Add to the finished scope list. (Try to) filter out scopes of length 0.

    if (scope->scStartLoc != scope->scEndLoc)
    {
        siScopeLast->scNext = scope;
        siScopeLast         = scope;
        siScopeCnt++;
    }
}

/*----------------------------------------------------------------------------
 * These functions end scopes given different types of parameters
 *----------------------------------------------------------------------------
 */

/*****************************************************************************
 * For tracked vars, we don't need to search for the scope in the list as we
 * have a pointer to the open scopes of all tracked variables.
 */

void CodeGen::siEndTrackedScope(unsigned varIndex)
{
    siScope* scope = siLatestTrackedScopes[varIndex];
    if (!scope)
    {
        return;
    }

    scope->scEndLoc.CaptureLocation(getEmitter());
    assert(scope->scEndLoc.Valid());

    siRemoveFromOpenScopeList(scope);

    siLatestTrackedScopes[varIndex] = nullptr;
}

/*****************************************************************************
 * If we don't know that the variable is tracked, this function handles both
 * cases.
 */

void CodeGen::siEndScope(unsigned varNum)
{
    for (siScope* scope = siOpenScopeList.scNext; scope; scope = scope->scNext)
    {
        if (scope->scVarNum == varNum)
        {
            siEndScope(scope);
            return;
        }
    }

    // At this point, we probably have a bad LocalVarTab

    if (compiler->opts.compDbgCode)
    {
        // LocalVarTab is good?? If we reached here implies that we are in a
        // bad state, so pretend that we don't have any scope info.
        assert(!siVerifyLocalVarTab());

        compiler->opts.compScopeInfo = false;
    }
}

/*****************************************************************************
 * If we have a handle to the siScope structure, we handle ending this scope
 * differently than if we just had a variable number. This saves us searching
 * the open-scope list again.
 */

void CodeGen::siEndScope(siScope* scope)
{
    scope->scEndLoc.CaptureLocation(getEmitter());
    assert(scope->scEndLoc.Valid());

    siRemoveFromOpenScopeList(scope);

    LclVarDsc& lclVarDsc1 = compiler->lvaTable[scope->scVarNum];
    if (lclVarDsc1.lvTracked)
    {
        siLatestTrackedScopes[lclVarDsc1.lvVarIndex] = nullptr;
    }
}

/*****************************************************************************
 *                      siVerifyLocalVarTab
 *
 * Checks the LocalVarTab for consistency. The VM may not have properly
 * verified the LocalVariableTable.
 */

#ifdef DEBUG

bool CodeGen::siVerifyLocalVarTab()
{
    // No entries with overlapping lives should have the same slot.

    for (unsigned i = 0; i < compiler->info.compVarScopesCount; i++)
    {
        for (unsigned j = i + 1; j < compiler->info.compVarScopesCount; j++)
        {
            unsigned slot1 = compiler->info.compVarScopes[i].vsdVarNum;
            unsigned beg1  = compiler->info.compVarScopes[i].vsdLifeBeg;
            unsigned end1  = compiler->info.compVarScopes[i].vsdLifeEnd;

            unsigned slot2 = compiler->info.compVarScopes[j].vsdVarNum;
            unsigned beg2  = compiler->info.compVarScopes[j].vsdLifeBeg;
            unsigned end2  = compiler->info.compVarScopes[j].vsdLifeEnd;

            if (slot1 == slot2 && (end1 > beg2 && beg1 < end2))
            {
                return false;
            }
        }
    }

    return true;
}

#endif

/*============================================================================
 *           INTERFACE (public) Functions for ScopeInfo
 *============================================================================
 */

void CodeGen::siInit()
{
#ifdef _TARGET_X86_
    assert((unsigned)ICorDebugInfo::REGNUM_EAX == REG_EAX);
    assert((unsigned)ICorDebugInfo::REGNUM_ECX == REG_ECX);
    assert((unsigned)ICorDebugInfo::REGNUM_EDX == REG_EDX);
    assert((unsigned)ICorDebugInfo::REGNUM_EBX == REG_EBX);
    assert((unsigned)ICorDebugInfo::REGNUM_ESP == REG_ESP);
    assert((unsigned)ICorDebugInfo::REGNUM_EBP == REG_EBP);
    assert((unsigned)ICorDebugInfo::REGNUM_ESI == REG_ESI);
    assert((unsigned)ICorDebugInfo::REGNUM_EDI == REG_EDI);
#endif

    assert((unsigned)ICorDebugInfo::VLT_REG == Compiler::VLT_REG);
    assert((unsigned)ICorDebugInfo::VLT_STK == Compiler::VLT_STK);
    assert((unsigned)ICorDebugInfo::VLT_REG_REG == Compiler::VLT_REG_REG);
    assert((unsigned)ICorDebugInfo::VLT_REG_STK == Compiler::VLT_REG_STK);
    assert((unsigned)ICorDebugInfo::VLT_STK_REG == Compiler::VLT_STK_REG);
    assert((unsigned)ICorDebugInfo::VLT_STK2 == Compiler::VLT_STK2);
    assert((unsigned)ICorDebugInfo::VLT_FPSTK == Compiler::VLT_FPSTK);
    assert((unsigned)ICorDebugInfo::VLT_FIXED_VA == Compiler::VLT_FIXED_VA);
    assert((unsigned)ICorDebugInfo::VLT_COUNT == Compiler::VLT_COUNT);
    assert((unsigned)ICorDebugInfo::VLT_INVALID == Compiler::VLT_INVALID);

    /* ICorDebugInfo::VarLoc and siVarLoc should overlap exactly as we cast
     * one to the other in eeSetLVinfo()
     * Below is a "required but not sufficient" condition
     */

    assert(sizeof(ICorDebugInfo::VarLoc) == sizeof(Compiler::siVarLoc));

    assert(compiler->opts.compScopeInfo);

    siOpenScopeList.scNext = nullptr;
    siOpenScopeLast        = &siOpenScopeList;
    siScopeLast            = &siScopeList;

    siScopeCnt = 0;

    VarSetOps::AssignNoCopy(compiler, siLastLife, VarSetOps::MakeEmpty(compiler));
    siLastEndOffs = 0;

    if (compiler->info.compVarScopesCount == 0)
    {
        return;
    }

#if FEATURE_EH_FUNCLETS
    siInFuncletRegion = false;
#endif // FEATURE_EH_FUNCLETS

    for (unsigned i = 0; i < lclMAX_TRACKED; i++)
    {
        siLatestTrackedScopes[i] = nullptr;
    }

    compiler->compResetScopeLists();
}

/*****************************************************************************
 *                          siBeginBlock
 *
 * Called at the beginning of code-gen for a block. Checks if any scopes
 * need to be opened.
 */

void CodeGen::siBeginBlock(BasicBlock* block)
{
    assert(block != nullptr);

    if (!compiler->opts.compScopeInfo)
    {
        return;
    }

    if (compiler->info.compVarScopesCount == 0)
    {
        return;
    }

#if FEATURE_EH_FUNCLETS
    if (siInFuncletRegion)
    {
        return;
    }

    if (block->bbFlags & BBF_FUNCLET_BEG)
    {
        // For now, don't report any scopes in funclets. JIT64 doesn't.
        siInFuncletRegion = true;

        JITDUMP("Scope info: found beginning of funclet region at block BB%02u; ignoring following blocks\n",
                block->bbNum);

        return;
    }
#endif // FEATURE_EH_FUNCLETS

#ifdef DEBUG
    if (verbose)
    {
        printf("\nScope info: begin block BB%02u, IL range ", block->bbNum);
        block->dspBlockILRange();
        printf("\n");
    }
#endif // DEBUG

    unsigned beginOffs = block->bbCodeOffs;

    if (beginOffs == BAD_IL_OFFSET)
    {
        JITDUMP("Scope info: ignoring block beginning\n");
        return;
    }

    if (!compiler->opts.compDbgCode)
    {
        /* For non-debuggable code */

        // End scope of variables which are not live for this block

        siUpdate();

        // Check that vars which are live on entry have an open scope

        VARSET_ITER_INIT(compiler, iter, block->bbLiveIn, i);
        while (iter.NextElem(compiler, &i))
        {
            unsigned varNum = compiler->lvaTrackedToVarNum[i];
            // lvRefCnt may go down to 0 after liveness-analysis.
            // So we need to check if this tracked variable is actually used.
            if (!compiler->lvaTable[varNum].lvIsInReg() && !compiler->lvaTable[varNum].lvOnFrame)
            {
                assert(compiler->lvaTable[varNum].lvRefCnt == 0);
                continue;
            }

            siCheckVarScope(varNum, beginOffs);
        }
    }
    else
    {
        // For debuggable code, scopes can begin only on block boundaries.
        // Check if there are any scopes on the current block's start boundary.

        VarScopeDsc* varScope;

#if FEATURE_EH_FUNCLETS

        // If we find a spot where the code offset isn't what we expect, because
        // there is a gap, it might be because we've moved the funclets out of
        // line. Catch up with the enter and exit scopes of the current block.
        // Ignore the enter/exit scope changes of the missing scopes, which for
        // funclets must be matched.

        if (siLastEndOffs != beginOffs)
        {
            assert(beginOffs > 0);
            assert(siLastEndOffs < beginOffs);

            JITDUMP("Scope info: found offset hole. lastOffs=%u, currOffs=%u\n", siLastEndOffs, beginOffs);

            // Skip enter scopes
            while ((varScope = compiler->compGetNextEnterScope(beginOffs - 1, true)) != nullptr)
            {
                /* do nothing */
                JITDUMP("Scope info: skipping enter scope, LVnum=%u\n", varScope->vsdLVnum);
            }

            // Skip exit scopes
            while ((varScope = compiler->compGetNextExitScope(beginOffs - 1, true)) != nullptr)
            {
                /* do nothing */
                JITDUMP("Scope info: skipping exit scope, LVnum=%u\n", varScope->vsdLVnum);
            }
        }

#else // FEATURE_EH_FUNCLETS

        if (siLastEndOffs != beginOffs)
        {
            assert(siLastEndOffs < beginOffs);
            return;
        }

#endif // FEATURE_EH_FUNCLETS

        while ((varScope = compiler->compGetNextEnterScope(beginOffs)) != nullptr)
        {
            // brace-matching editor workaround for following line: (
            JITDUMP("Scope info: opening scope, LVnum=%u [%03X..%03X)\n", varScope->vsdLVnum, varScope->vsdLifeBeg,
                    varScope->vsdLifeEnd);

            siNewScope(varScope->vsdLVnum, varScope->vsdVarNum);

#ifdef DEBUG
            LclVarDsc* lclVarDsc1 = &compiler->lvaTable[varScope->vsdVarNum];
            if (VERBOSE)
            {
                printf("Scope info: >> new scope, VarNum=%u, tracked? %s, VarIndex=%u, bbLiveIn=%s ",
                       varScope->vsdVarNum, lclVarDsc1->lvTracked ? "yes" : "no", lclVarDsc1->lvVarIndex,
                       VarSetOps::ToString(compiler, block->bbLiveIn));
                dumpConvertedVarSet(compiler, block->bbLiveIn);
                printf("\n");
            }
            assert(!lclVarDsc1->lvTracked || VarSetOps::IsMember(compiler, block->bbLiveIn, lclVarDsc1->lvVarIndex));
#endif // DEBUG
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        siDispOpenScopes();
    }
#endif
}

/*****************************************************************************
 *                          siEndBlock
 *
 * Called at the end of code-gen for a block. Any closing scopes are marked
 * as such. Note that if we are collecting LocalVar info, scopes can
 * only begin or end at block boundaries for debuggable code.
 */

void CodeGen::siEndBlock(BasicBlock* block)
{
    assert(compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0));

#if FEATURE_EH_FUNCLETS
    if (siInFuncletRegion)
    {
        return;
    }
#endif // FEATURE_EH_FUNCLETS

#ifdef DEBUG
    if (verbose)
    {
        printf("\nScope info: end block BB%02u, IL range ", block->bbNum);
        block->dspBlockILRange();
        printf("\n");
    }
#endif // DEBUG

    unsigned endOffs = block->bbCodeOffsEnd;

    if (endOffs == BAD_IL_OFFSET)
    {
        JITDUMP("Scope info: ignoring block end\n");
        return;
    }

    // If non-debuggable code, find all scopes which end over this block
    // and close them. For debuggable code, scopes will only end on block
    // boundaries.

    VarScopeDsc* varScope;
    while ((varScope = compiler->compGetNextExitScope(endOffs, !compiler->opts.compDbgCode)) != nullptr)
    {
        // brace-matching editor workaround for following line: (
        JITDUMP("Scope info: ending scope, LVnum=%u [%03X..%03X)\n", varScope->vsdLVnum, varScope->vsdLifeBeg,
                varScope->vsdLifeEnd);

        unsigned   varNum     = varScope->vsdVarNum;
        LclVarDsc* lclVarDsc1 = &compiler->lvaTable[varNum];

        assert(lclVarDsc1);

        if (lclVarDsc1->lvTracked)
        {
            siEndTrackedScope(lclVarDsc1->lvVarIndex);
        }
        else
        {
            siEndScope(varNum);
        }
    }

    siLastEndOffs = endOffs;

#ifdef DEBUG
    if (verbose)
    {
        siDispOpenScopes();
    }
#endif
}

/*****************************************************************************
 *                          siUpdate
 *
 * Called at the start of basic blocks, and during code-gen of a block,
 * for non-debuggable code, whenever the life of any tracked variable changes
 * and the appropriate code has been generated. For debuggable code, variables are
 * live over their entire scope, and so they go live or dead only on
 * block boundaries.
 */
void CodeGen::siUpdate()
{
    if (!compiler->opts.compScopeInfo)
    {
        return;
    }

    if (compiler->opts.compDbgCode)
    {
        return;
    }

    if (compiler->info.compVarScopesCount == 0)
    {
        return;
    }

#if FEATURE_EH_FUNCLETS
    if (siInFuncletRegion)
    {
        return;
    }
#endif // FEATURE_EH_FUNCLETS

    VARSET_TP VARSET_INIT_NOCOPY(killed, VarSetOps::Diff(compiler, siLastLife, compiler->compCurLife));
    assert(VarSetOps::IsSubset(compiler, killed, compiler->lvaTrackedVars));

    VARSET_ITER_INIT(compiler, iter, killed, i);
    while (iter.NextElem(compiler, &i))
    {
#ifdef DEBUG
        unsigned   lclNum = compiler->lvaTrackedToVarNum[i];
        LclVarDsc* lclVar = &compiler->lvaTable[lclNum];
        assert(lclVar->lvTracked);
#endif

        siScope* scope = siLatestTrackedScopes[i];
        siEndTrackedScope(i);
    }

    VarSetOps::Assign(compiler, siLastLife, compiler->compCurLife);
}

/*****************************************************************************
 *  In optimized code, we may not have access to gtLclVar.gtLclILoffs.
 *  So there may be ambiguity as to which entry in compiler->info.compVarScopes
 *  to use. We search the entire table and find the entry whose life
 *  begins closest to the given offset.
 */

/*****************************************************************************
 *                          siCheckVarScope
 *
 * For non-debuggable code, whenever we come across a GenTree which is an
 * assignment to a local variable, this function is called to check if the
 * variable has an open scope. Also, check if it has the correct LVnum.
 */

void CodeGen::siCheckVarScope(unsigned varNum, IL_OFFSET offs)
{
    assert(compiler->opts.compScopeInfo && !compiler->opts.compDbgCode && (compiler->info.compVarScopesCount > 0));

#if FEATURE_EH_FUNCLETS
    if (siInFuncletRegion)
    {
        return;
    }
#endif // FEATURE_EH_FUNCLETS

    if (offs == BAD_IL_OFFSET)
    {
        return;
    }

    siScope*   scope;
    LclVarDsc* lclVarDsc1 = &compiler->lvaTable[varNum];

    // If there is an open scope corresponding to varNum, find it

    if (lclVarDsc1->lvTracked)
    {
        scope = siLatestTrackedScopes[lclVarDsc1->lvVarIndex];
    }
    else
    {
        for (scope = siOpenScopeList.scNext; scope; scope = scope->scNext)
        {
            if (scope->scVarNum == varNum)
            {
                break;
            }
        }
    }

    // Look up the compiler->info.compVarScopes[] to find the local var info for (varNum->lvSlotNum, offs)
    VarScopeDsc* varScope = compiler->compFindLocalVar(varNum, offs);
    if (varScope == nullptr)
    {
        return;
    }

    // If the currently open scope does not have the correct LVnum, close it
    // and create a new scope with this new LVnum

    if (scope)
    {
        if (scope->scLVnum != varScope->vsdLVnum)
        {
            siEndScope(scope);
            siNewScope(varScope->vsdLVnum, varScope->vsdVarNum);
        }
    }
    else
    {
        siNewScope(varScope->vsdLVnum, varScope->vsdVarNum);
    }
}

/*****************************************************************************
 *                          siCloseAllOpenScopes
 *
 * For unreachable code, or optimized code with blocks reordered, there may be
 * scopes left open at the end. Simply close them.
 */

void CodeGen::siCloseAllOpenScopes()
{
    assert(siOpenScopeList.scNext);

    while (siOpenScopeList.scNext)
    {
        siEndScope(siOpenScopeList.scNext);
    }
}

/*****************************************************************************
 *                          siDispOpenScopes
 *
 * Displays all the vars on the open-scope list
 */

#ifdef DEBUG

void CodeGen::siDispOpenScopes()
{
    assert(compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0));

    printf("Scope info: open scopes =\n");

    if (siOpenScopeList.scNext == nullptr)
    {
        printf("   <none>\n");
    }
    else
    {
        for (siScope* scope = siOpenScopeList.scNext; scope != nullptr; scope = scope->scNext)
        {
            VarScopeDsc* localVars = compiler->info.compVarScopes;

            for (unsigned i = 0; i < compiler->info.compVarScopesCount; i++, localVars++)
            {
                if (localVars->vsdLVnum == scope->scLVnum)
                {
                    const char* name = compiler->VarNameToStr(localVars->vsdName);
                    // brace-matching editor workaround for following line: (
                    printf("   %u (%s) [%03X..%03X)\n", localVars->vsdLVnum, name == nullptr ? "UNKNOWN" : name,
                           localVars->vsdLifeBeg, localVars->vsdLifeEnd);
                    break;
                }
            }
        }
    }
}

#endif // DEBUG

/*============================================================================
 *
 *              Implementation for PrologScopeInfo
 *
 *============================================================================
 */

/*****************************************************************************
 *                      psiNewPrologScope
 *
 * Creates a new scope and adds it to the Open scope list.
 */

CodeGen::psiScope* CodeGen::psiNewPrologScope(unsigned LVnum, unsigned slotNum)
{
    psiScope* newScope = (psiScope*)compiler->compGetMem(sizeof(*newScope), CMK_SiScope);

    newScope->scStartLoc.CaptureLocation(getEmitter());
    assert(newScope->scStartLoc.Valid());

    newScope->scEndLoc.Init();

    newScope->scLVnum   = LVnum;
    newScope->scSlotNum = slotNum;

    newScope->scNext         = nullptr;
    psiOpenScopeLast->scNext = newScope;
    newScope->scPrev         = psiOpenScopeLast;
    psiOpenScopeLast         = newScope;

    return newScope;
}

/*****************************************************************************
 *                          psiEndPrologScope
 *
 * Remove the scope from the Open-scope list and add it to the finished-scopes
 * list if its length is non-zero
 */

void CodeGen::psiEndPrologScope(psiScope* scope)
{
    scope->scEndLoc.CaptureLocation(getEmitter());
    assert(scope->scEndLoc.Valid());

    // Remove from open-scope list
    scope->scPrev->scNext = scope->scNext;
    if (scope->scNext)
    {
        scope->scNext->scPrev = scope->scPrev;
    }
    else
    {
        psiOpenScopeLast = scope->scPrev;
    }

    // Add to the finished scope list.
    // If the length is zero, it means that the prolog is empty. In that case,
    // CodeGen::genSetScopeInfo will report the liveness of all arguments
    // as spanning the first instruction in the method, so that they can
    // at least be inspected on entry to the method.
    if (scope->scStartLoc != scope->scEndLoc || scope->scStartLoc.IsOffsetZero())
    {
        psiScopeLast->scNext = scope;
        psiScopeLast         = scope;
        psiScopeCnt++;
    }
}

/*============================================================================
 *           INTERFACE (protected) Functions for PrologScopeInfo
 *============================================================================
 */

//------------------------------------------------------------------------
// psSetScopeOffset: Set the offset of the newScope to the offset of the LslVar
//
// Arguments:
//    'newScope'  the new scope object whose offset is to be set to the lclVarDsc offset.
//    'lclVarDsc' is an op that will now be contained by its parent.
//
//
void CodeGen::psSetScopeOffset(psiScope* newScope, LclVarDsc* lclVarDsc)
{
    newScope->scRegister   = false;
    newScope->u2.scBaseReg = REG_SPBASE;

#ifdef _TARGET_AMD64_
    // scOffset = offset from caller SP - REGSIZE_BYTES
    // TODO-Cleanup - scOffset needs to be understood.  For now just matching with the existing definition.
    newScope->u2.scOffset =
        compiler->lvaToCallerSPRelativeOffset(lclVarDsc->lvStkOffs, lclVarDsc->lvFramePointerBased) + REGSIZE_BYTES;
#else  // !_TARGET_AMD64_
    if (doubleAlignOrFramePointerUsed())
    {
        // REGSIZE_BYTES - for the pushed value of EBP
        newScope->u2.scOffset = lclVarDsc->lvStkOffs - REGSIZE_BYTES;
    }
    else
    {
        newScope->u2.scOffset = lclVarDsc->lvStkOffs - genTotalFrameSize();
    }
#endif // !_TARGET_AMD64_
}

/*============================================================================
*           INTERFACE (public) Functions for PrologScopeInfo
*============================================================================
*/

/*****************************************************************************
 *                          psiBegProlog
 *
 * Initializes the PrologScopeInfo, and creates open scopes for all the
 * parameters of the method.
 */

void CodeGen::psiBegProlog()
{
    assert(compiler->compGeneratingProlog);

    VarScopeDsc* varScope;

    psiOpenScopeList.scNext = nullptr;
    psiOpenScopeLast        = &psiOpenScopeList;
    psiScopeLast            = &psiScopeList;
    psiScopeCnt             = 0;

    compiler->compResetScopeLists();

    while ((varScope = compiler->compGetNextEnterScope(0)) != nullptr)
    {
        LclVarDsc* lclVarDsc1 = &compiler->lvaTable[varScope->vsdVarNum];

        if (!lclVarDsc1->lvIsParam)
        {
            continue;
        }

        psiScope* newScope = psiNewPrologScope(varScope->vsdLVnum, varScope->vsdVarNum);

        if (lclVarDsc1->lvIsRegArg)
        {
            bool isStructHandled = false;
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
            if (varTypeIsStruct(lclVarDsc1))
            {
                CORINFO_CLASS_HANDLE typeHnd = lclVarDsc1->lvVerTypeInfo.GetClassHandle();
                assert(typeHnd != nullptr);
                compiler->eeGetSystemVAmd64PassStructInRegisterDescriptor(typeHnd, &structDesc);
                if (structDesc.passedInRegisters)
                {
                    regNumber regNum      = REG_NA;
                    regNumber otherRegNum = REG_NA;
                    for (unsigned nCnt = 0; nCnt < structDesc.eightByteCount; nCnt++)
                    {
                        unsigned  len     = structDesc.eightByteSizes[nCnt];
                        var_types regType = TYP_UNDEF;

                        if (nCnt == 0)
                        {
                            regNum = lclVarDsc1->lvArgReg;
                        }
                        else if (nCnt == 1)
                        {
                            otherRegNum = lclVarDsc1->lvOtherArgReg;
                        }
                        else
                        {
                            assert(false && "Invalid eightbyte number.");
                        }

                        regType = compiler->GetEightByteType(structDesc, nCnt);
#ifdef DEBUG
                        regType = compiler->mangleVarArgsType(regType);
                        assert(genMapRegNumToRegArgNum((nCnt == 0 ? regNum : otherRegNum), regType) != (unsigned)-1);
#endif // DEBUG
                    }

                    newScope->scRegister    = true;
                    newScope->u1.scRegNum   = (regNumberSmall)regNum;
                    newScope->u1.scOtherReg = (regNumberSmall)otherRegNum;
                }
                else
                {
                    // Stack passed argument. Get the offset from the  caller's frame.
                    psSetScopeOffset(newScope, lclVarDsc1);
                }

                isStructHandled = true;
            }
#endif // !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            if (!isStructHandled)
            {
#ifdef DEBUG
                var_types regType = compiler->mangleVarArgsType(lclVarDsc1->TypeGet());
                if (lclVarDsc1->lvIsHfaRegArg())
                {
                    regType = lclVarDsc1->GetHfaType();
                }
                assert(genMapRegNumToRegArgNum(lclVarDsc1->lvArgReg, regType) != (unsigned)-1);
#endif // DEBUG

                newScope->scRegister  = true;
                newScope->u1.scRegNum = (regNumberSmall)lclVarDsc1->lvArgReg;
            }
        }
        else
        {
            psSetScopeOffset(newScope, lclVarDsc1);
        }
    }
}

/*****************************************************************************
 Enable this macro to get accurate prolog information for every instruction
 in the prolog. However, this is overkill as nobody steps through the
 disassembly of the prolog. Even if they do they will not expect rich debug info.

 We still report all the arguments at the very start of the method so that
 the user can see the arguments at the very start of the method (offset=0).

 Disabling this decreased the debug maps in mscorlib by 10% (01/2003)
 */

#if 0
#define ACCURATE_PROLOG_DEBUG_INFO
#endif

/*****************************************************************************
 *                          psiAdjustStackLevel
 *
 * When ESP changes, all scopes relative to ESP have to be updated.
 */

void CodeGen::psiAdjustStackLevel(unsigned size)
{
#ifdef DEBUGGING_SUPPORT
    if (!compiler->opts.compScopeInfo || (compiler->info.compVarScopesCount == 0))
    {
        return;
    }

    assert(compiler->compGeneratingProlog);

#ifdef ACCURATE_PROLOG_DEBUG_INFO

    psiScope* scope;

    // walk the list backwards
    // Works as psiEndPrologScope does not change scPrev
    for (scope = psiOpenScopeLast; scope != &psiOpenScopeList; scope = scope->scPrev)
    {
        if (scope->scRegister)
        {
            assert(compiler->lvaTable[scope->scSlotNum].lvIsRegArg);
            continue;
        }
        assert(scope->u2.scBaseReg == REG_SPBASE);

        psiScope* newScope     = psiNewPrologScope(scope->scLVnum, scope->scSlotNum);
        newScope->scRegister   = false;
        newScope->u2.scBaseReg = REG_SPBASE;
        newScope->u2.scOffset  = scope->u2.scOffset + size;

        psiEndPrologScope(scope);
    }

#endif // ACCURATE_PROLOG_DEBUG_INFO
#endif // DEBUGGING_SUPPORT
}

/*****************************************************************************
 *                          psiMoveESPtoEBP
 *
 * For EBP-frames, the parameters are accessed via ESP on entry to the function,
 * but via EBP right after a "mov ebp,esp" instruction
 */

void CodeGen::psiMoveESPtoEBP()
{
#ifdef DEBUGGING_SUPPORT
    if (!compiler->opts.compScopeInfo || (compiler->info.compVarScopesCount == 0))
    {
        return;
    }

    assert(compiler->compGeneratingProlog);
    assert(doubleAlignOrFramePointerUsed());

#ifdef ACCURATE_PROLOG_DEBUG_INFO

    psiScope* scope;

    // walk the list backwards
    // Works as psiEndPrologScope does not change scPrev
    for (scope = psiOpenScopeLast; scope != &psiOpenScopeList; scope = scope->scPrev)
    {
        if (scope->scRegister)
        {
            assert(compiler->lvaTable[scope->scSlotNum].lvIsRegArg);
            continue;
        }
        assert(scope->u2.scBaseReg == REG_SPBASE);

        psiScope* newScope     = psiNewPrologScope(scope->scLVnum, scope->scSlotNum);
        newScope->scRegister   = false;
        newScope->u2.scBaseReg = REG_FPBASE;
        newScope->u2.scOffset  = scope->u2.scOffset;

        psiEndPrologScope(scope);
    }

#endif // ACCURATE_PROLOG_DEBUG_INFO
#endif // DEBUGGING_SUPPORT
}

/*****************************************************************************
 *                          psiMoveToReg
 *
 * Called when a parameter is loaded into its assigned register from the stack,
 * or when parameters are moved around due to circular dependancy.
 * If reg != REG_NA, then the parameter is being moved into its assigned
 * register, else it may be being moved to a temp register.
 */

void CodeGen::psiMoveToReg(unsigned varNum, regNumber reg, regNumber otherReg)
{
#ifdef DEBUGGING_SUPPORT
    assert(compiler->compGeneratingProlog);

    if (!compiler->opts.compScopeInfo)
    {
        return;
    }

    if (compiler->info.compVarScopesCount == 0)
    {
        return;
    }

    assert((int)varNum >= 0); // It's not a spill temp number.
    assert(compiler->lvaTable[varNum].lvIsInReg());

#ifdef ACCURATE_PROLOG_DEBUG_INFO

    /* If reg!=REG_NA, the parameter is part of a cirular dependancy, and is
     * being moved through temp register "reg".
     * If reg==REG_NA, it is being moved to its assigned register.
     */
    if (reg == REG_NA)
    {
        // Grab the assigned registers.

        reg      = compiler->lvaTable[varNum].lvRegNum;
        otherReg = compiler->lvaTable[varNum].lvOtherReg;
    }

    psiScope* scope;

    // walk the list backwards
    // Works as psiEndPrologScope does not change scPrev
    for (scope = psiOpenScopeLast; scope != &psiOpenScopeList; scope = scope->scPrev)
    {
        if (scope->scSlotNum != compiler->lvaTable[varNum].lvSlotNum)
            continue;

        psiScope* newScope      = psiNewPrologScope(scope->scLVnum, scope->scSlotNum);
        newScope->scRegister    = true;
        newScope->u1.scRegNum   = reg;
        newScope->u1.scOtherReg = otherReg;

        psiEndPrologScope(scope);
        return;
    }

    // May happen if a parameter does not have an entry in the LocalVarTab
    // But assert() just in case it is because of something else.
    assert(varNum == compiler->info.compRetBuffArg ||
           !"Parameter scope not found (Assert doesnt always indicate error)");

#endif // ACCURATE_PROLOG_DEBUG_INFO
#endif // DEBUGGING_SUPPORT
}

/*****************************************************************************
 *                      CodeGen::psiMoveToStack
 *
 * A incoming register-argument is being moved to its final home on the stack
 * (ie. all adjustements to {F/S}PBASE have been made
 */

void CodeGen::psiMoveToStack(unsigned varNum)
{
#ifdef DEBUGGING_SUPPORT
    if (!compiler->opts.compScopeInfo || (compiler->info.compVarScopesCount == 0))
    {
        return;
    }

    assert(compiler->compGeneratingProlog);
    assert(compiler->lvaTable[varNum].lvIsRegArg);
    assert(!compiler->lvaTable[varNum].lvRegister);

#ifdef ACCURATE_PROLOG_DEBUG_INFO

    psiScope* scope;

    // walk the list backwards
    // Works as psiEndPrologScope does not change scPrev
    for (scope = psiOpenScopeLast; scope != &psiOpenScopeList; scope = scope->scPrev)
    {
        if (scope->scSlotNum != compiler->lvaTable[varNum].lvSlotNum)
            continue;

        /* The param must be currently sitting in the register in which it
           was passed in */
        assert(scope->scRegister);
        assert(scope->u1.scRegNum == compiler->lvaTable[varNum].lvArgReg);

        psiScope* newScope     = psiNewPrologScope(scope->scLVnum, scope->scSlotNum);
        newScope->scRegister   = false;
        newScope->u2.scBaseReg = (compiler->lvaTable[varNum].lvFramePointerBased) ? REG_FPBASE : REG_SPBASE;
        newScope->u2.scOffset  = compiler->lvaTable[varNum].lvStkOffs;

        psiEndPrologScope(scope);
        return;
    }

    // May happen if a parameter does not have an entry in the LocalVarTab
    // But assert() just in case it is because of something else.
    assert(varNum == compiler->info.compRetBuffArg ||
           !"Parameter scope not found (Assert doesnt always indicate error)");

#endif // ACCURATE_PROLOG_DEBUG_INFO
#endif // DEBUGGING_SUPPORT
}

/*****************************************************************************
 *                          psiEndProlog
 */

void CodeGen::psiEndProlog()
{
    assert(compiler->compGeneratingProlog);
    psiScope* scope;

    for (scope = psiOpenScopeList.scNext; scope; scope = psiOpenScopeList.scNext)
    {
        psiEndPrologScope(scope);
    }
}

/*****************************************************************************/
#endif // DEBUGGING_SUPPORT
/*****************************************************************************/
