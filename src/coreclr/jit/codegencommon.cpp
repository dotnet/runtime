// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX Code Generator Common:                                                    XX
XX   Methods common to all architectures and register allocation strategies  XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

// TODO-Cleanup: There are additional methods in CodeGen*.cpp that are almost
// identical, and which should probably be moved here.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif
#include "codegen.h"

#include "gcinfo.h"
#include "emit.h"

#ifndef JIT32_GCENCODER
#include "gcinfoencoder.h"
#endif

#include "patchpointinfo.h"

/*****************************************************************************/

void CodeGenInterface::setFramePointerRequiredEH(bool value)
{
    m_cgFramePointerRequired = value;

#ifndef JIT32_GCENCODER
    if (value)
    {
        // EnumGcRefs will only enumerate slots in aborted frames
        // if they are fully-interruptible.  So if we have a catch
        // or finally that will keep frame-vars alive, we need to
        // force fully-interruptible.
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
        if (verbose)
        {
            printf("Method has EH, marking method as fully interruptible\n");
        }
#endif

        m_cgInterruptible = true;
    }
#endif // JIT32_GCENCODER
}

/*****************************************************************************/
CodeGenInterface* getCodeGenerator(Compiler* comp)
{
    return new (comp, CMK_Codegen) CodeGen(comp);
}

// CodeGen constructor
CodeGenInterface::CodeGenInterface(Compiler* theCompiler)
    : gcInfo(theCompiler), regSet(theCompiler, gcInfo), compiler(theCompiler), treeLifeUpdater(nullptr)
{
}

/*****************************************************************************/

CodeGen::CodeGen(Compiler* theCompiler) : CodeGenInterface(theCompiler)
{
#if defined(TARGET_XARCH)
    negBitmaskFlt  = nullptr;
    negBitmaskDbl  = nullptr;
    absBitmaskFlt  = nullptr;
    absBitmaskDbl  = nullptr;
    u8ToDblBitmask = nullptr;
#endif // defined(TARGET_XARCH)

#if defined(FEATURE_PUT_STRUCT_ARG_STK) && !defined(TARGET_X86)
    m_stkArgVarNum = BAD_VAR_NUM;
#endif

#if defined(UNIX_X86_ABI)
    curNestedAlignment = 0;
    maxNestedAlignment = 0;
#endif

    gcInfo.regSet        = &regSet;
    m_cgEmitter          = new (compiler->getAllocator()) emitter();
    m_cgEmitter->codeGen = this;
    m_cgEmitter->gcInfo  = &gcInfo;

#ifdef DEBUG
    setVerbose(compiler->verbose);
#endif // DEBUG

    regSet.tmpInit();

    instInit();

#ifdef LATE_DISASM
    getDisAssembler().disInit(compiler);
#endif

#ifdef DEBUG
    genTempLiveChg        = true;
    genTrnslLocalVarCount = 0;

    // Shouldn't be used before it is set in genFnProlog()
    compiler->compCalleeRegsPushed = UninitializedWord<unsigned>(compiler);

#if defined(TARGET_XARCH)
    // Shouldn't be used before it is set in genFnProlog()
    compiler->compCalleeFPRegsSavedMask = (regMaskTP)-1;
#endif // defined(TARGET_XARCH)
#endif // DEBUG

#ifdef TARGET_AMD64
    // This will be set before final frame layout.
    compiler->compVSQuirkStackPaddingNeeded = 0;

    // Set to true if we perform the Quirk that fixes the PPP issue
    compiler->compQuirkForPPPflag = false;
#endif // TARGET_AMD64

    //  Initialize the IP-mapping logic.
    compiler->genIPmappingList        = nullptr;
    compiler->genIPmappingLast        = nullptr;
    compiler->genCallSite2ILOffsetMap = nullptr;

    /* Assume that we not fully interruptible */

    SetInterruptible(false);
#ifdef TARGET_ARMARCH
    SetHasTailCalls(false);
#endif // TARGET_ARMARCH
#ifdef DEBUG
    genInterruptibleUsed = false;
    genCurDispOffset     = (unsigned)-1;
#endif

#ifdef TARGET_ARM64
    genSaveFpLrWithAllCalleeSavedRegisters = false;
#endif // TARGET_ARM64
}

void CodeGenInterface::genMarkTreeInReg(GenTree* tree, regNumber reg)
{
    tree->SetRegNum(reg);
}

#if defined(TARGET_X86) || defined(TARGET_ARM)

//---------------------------------------------------------------------
// genTotalFrameSize - return the "total" size of the stack frame, including local size
// and callee-saved register size. There are a few things "missing" depending on the
// platform. The function genCallerSPtoInitialSPdelta() includes those things.
//
// For ARM, this doesn't include the prespilled registers.
//
// For x86, this doesn't include the frame pointer if codeGen->isFramePointerUsed() is true.
// It also doesn't include the pushed return address.
//
// Return value:
//    Frame size

int CodeGenInterface::genTotalFrameSize() const
{
    assert(!IsUninitialized(compiler->compCalleeRegsPushed));

    int totalFrameSize = compiler->compCalleeRegsPushed * REGSIZE_BYTES + compiler->compLclFrameSize;

    assert(totalFrameSize >= 0);
    return totalFrameSize;
}

//---------------------------------------------------------------------
// genSPtoFPdelta - return the offset from SP to the frame pointer.
// This number is going to be positive, since SP must be at the lowest
// address.
//
// There must be a frame pointer to call this function!

int CodeGenInterface::genSPtoFPdelta() const
{
    assert(isFramePointerUsed());

    int delta;

    delta = -genCallerSPtoInitialSPdelta() + genCallerSPtoFPdelta();

    assert(delta >= 0);
    return delta;
}

//---------------------------------------------------------------------
// genCallerSPtoFPdelta - return the offset from Caller-SP to the frame pointer.
// This number is going to be negative, since the Caller-SP is at a higher
// address than the frame pointer.
//
// There must be a frame pointer to call this function!

int CodeGenInterface::genCallerSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    int callerSPtoFPdelta = 0;

#if defined(TARGET_ARM)
    // On ARM, we first push the prespill registers, then store LR, then R11 (FP), and point R11 at the saved R11.
    callerSPtoFPdelta -= genCountBits(regSet.rsMaskPreSpillRegs(true)) * REGSIZE_BYTES;
    callerSPtoFPdelta -= 2 * REGSIZE_BYTES;
#elif defined(TARGET_X86)
    // Thanks to ebp chaining, the difference between ebp-based addresses
    // and caller-SP-relative addresses is just the 2 pointers:
    //     return address
    //     pushed ebp
    callerSPtoFPdelta -= 2 * REGSIZE_BYTES;
#else
#error "Unknown TARGET"
#endif // TARGET*

    assert(callerSPtoFPdelta <= 0);
    return callerSPtoFPdelta;
}

//---------------------------------------------------------------------
// genCallerSPtoInitialSPdelta - return the offset from Caller-SP to Initial SP.
//
// This number will be negative.

int CodeGenInterface::genCallerSPtoInitialSPdelta() const
{
    int callerSPtoSPdelta = 0;

#if defined(TARGET_ARM)
    callerSPtoSPdelta -= genCountBits(regSet.rsMaskPreSpillRegs(true)) * REGSIZE_BYTES;
    callerSPtoSPdelta -= genTotalFrameSize();
#elif defined(TARGET_X86)
    callerSPtoSPdelta -= genTotalFrameSize();
    callerSPtoSPdelta -= REGSIZE_BYTES; // caller-pushed return address

    // compCalleeRegsPushed does not account for the frame pointer
    // TODO-Cleanup: shouldn't this be part of genTotalFrameSize?
    if (isFramePointerUsed())
    {
        callerSPtoSPdelta -= REGSIZE_BYTES;
    }
#else
#error "Unknown TARGET"
#endif // TARGET*

    assert(callerSPtoSPdelta <= 0);
    return callerSPtoSPdelta;
}

#endif // defined(TARGET_X86) || defined(TARGET_ARM)

/*****************************************************************************
 * Should we round simple operations (assignments, arithmetic operations, etc.)
 */

// inline
// static
bool CodeGen::genShouldRoundFP()
{
    RoundLevel roundLevel = getRoundFloatLevel();

    switch (roundLevel)
    {
        case ROUND_NEVER:
        case ROUND_CMP_CONST:
        case ROUND_CMP:
            return false;

        default:
            assert(roundLevel == ROUND_ALWAYS);
            return true;
    }
}

/*****************************************************************************
 *
 *  Initialize some global variables.
 */

void CodeGen::genPrepForCompiler()
{
    treeLifeUpdater = new (compiler, CMK_bitset) TreeLifeUpdater<true>(compiler);

    /* Figure out which non-register variables hold pointers */

    VarSetOps::AssignNoCopy(compiler, gcInfo.gcTrkStkPtrLcls, VarSetOps::MakeEmpty(compiler));

    // Also, initialize gcTrkStkPtrLcls to include all tracked variables that do not fully live
    // in a register (i.e. they live on the stack for all or part of their lifetime).
    // Note that lvRegister indicates that a lclVar is in a register for its entire lifetime.

    unsigned   varNum;
    LclVarDsc* varDsc;
    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        if (varDsc->lvTracked || varDsc->lvIsRegCandidate())
        {
            if (!varDsc->lvRegister && compiler->lvaIsGCTracked(varDsc))
            {
                VarSetOps::AddElemD(compiler, gcInfo.gcTrkStkPtrLcls, varDsc->lvVarIndex);
            }
        }
    }
    VarSetOps::AssignNoCopy(compiler, genLastLiveSet, VarSetOps::MakeEmpty(compiler));
    genLastLiveMask = RBM_NONE;
#ifdef DEBUG
    compiler->fgBBcountAtCodegen = compiler->fgBBcount;
#endif
}

//------------------------------------------------------------------------
// genMarkLabelsForCodegen: Mark labels required for codegen.
//
// Mark all blocks that require a label with BBF_HAS_LABEL. These are either blocks that are:
// 1. the target of jumps (fall-through flow doesn't require a label),
// 2. referenced labels such as for "switch" codegen,
// 3. needed to denote the range of EH regions to the VM.
// 4. needed to denote the range of code for alignment processing.
//
// No labels will be in the IR before now, but future codegen might annotate additional blocks
// with this flag, such as "switch" codegen, or codegen-created blocks from genCreateTempLabel().
// Also, the alignment processing code marks BBJ_COND fall-through labels elsewhere.
//
// To report exception handling information to the VM, we need the size of the exception
// handling regions. To compute that, we need to emit labels for the beginning block of
// an EH region, and the block that immediately follows a region. Go through the EH
// table and mark all these blocks with BBF_HAS_LABEL to make this happen.
//
// This code is closely couple with genReportEH() in the sense that any block
// that this procedure has determined it needs to have a label has to be selected
// using the same logic both here and in genReportEH(), so basically any time there is
// a change in the way we handle EH reporting, we have to keep the logic of these two
// methods 'in sync'.
//
// No blocks should be added or removed after this.
//
void CodeGen::genMarkLabelsForCodegen()
{
    assert(!compiler->fgSafeBasicBlockCreation);

    JITDUMP("Mark labels for codegen\n");

#ifdef DEBUG
    // No label flags should be set before this.
    for (BasicBlock* block = compiler->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        assert((block->bbFlags & BBF_HAS_LABEL) == 0);
    }
#endif // DEBUG

    // The first block is special; it always needs a label. This is to properly set up GC info.
    JITDUMP("  " FMT_BB " : first block\n", compiler->fgFirstBB->bbNum);
    compiler->fgFirstBB->bbFlags |= BBF_HAS_LABEL;

    // The current implementation of switch tables requires the first block to have a label so it
    // can generate offsets to the switch label targets.
    // (This is duplicative with the fact we always set the first block with a label above.)
    // TODO-CQ: remove this when switches have been re-implemented to not use this.
    if (compiler->fgHasSwitch)
    {
        JITDUMP("  " FMT_BB " : function has switch; mark first block\n", compiler->fgFirstBB->bbNum);
        compiler->fgFirstBB->bbFlags |= BBF_HAS_LABEL;
    }

    for (BasicBlock* block = compiler->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        switch (block->bbJumpKind)
        {
            case BBJ_ALWAYS: // This will also handle the BBJ_ALWAYS of a BBJ_CALLFINALLY/BBJ_ALWAYS pair.
            case BBJ_COND:
            case BBJ_EHCATCHRET:
                JITDUMP("  " FMT_BB " : branch target\n", block->bbJumpDest->bbNum);
                block->bbJumpDest->bbFlags |= BBF_HAS_LABEL;
                break;

            case BBJ_SWITCH:
                unsigned jumpCnt;
                jumpCnt = block->bbJumpSwt->bbsCount;
                BasicBlock** jumpTab;
                jumpTab = block->bbJumpSwt->bbsDstTab;
                do
                {
                    JITDUMP("  " FMT_BB " : branch target\n", (*jumpTab)->bbNum);
                    (*jumpTab)->bbFlags |= BBF_HAS_LABEL;
                } while (++jumpTab, --jumpCnt);
                break;

            case BBJ_CALLFINALLY:
                // The finally target itself will get marked by walking the EH table, below, and marking
                // all handler begins.
                CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_EH_CALLFINALLY_THUNKS
                {
                    // For callfinally thunks, we need to mark the block following the callfinally/always pair,
                    // as that's needed for identifying the range of the "duplicate finally" region in EH data.
                    BasicBlock* bbToLabel = block->bbNext;
                    if (block->isBBCallAlwaysPair())
                    {
                        bbToLabel = bbToLabel->bbNext; // skip the BBJ_ALWAYS
                    }
                    if (bbToLabel != nullptr)
                    {
                        JITDUMP("  " FMT_BB " : callfinally thunk region end\n", bbToLabel->bbNum);
                        bbToLabel->bbFlags |= BBF_HAS_LABEL;
                    }
                }
#endif // FEATURE_EH_CALLFINALLY_THUNKS

                break;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
            case BBJ_RETURN:
            case BBJ_THROW:
            case BBJ_NONE:
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }
    }

    // Walk all the exceptional code blocks and mark them, since they don't appear in the normal flow graph.
    for (Compiler::AddCodeDsc* add = compiler->fgAddCodeList; add; add = add->acdNext)
    {
        JITDUMP("  " FMT_BB " : throw helper block\n", add->acdDstBlk->bbNum);
        add->acdDstBlk->bbFlags |= BBF_HAS_LABEL;
    }

    EHblkDsc* HBtab;
    EHblkDsc* HBtabEnd;

    for (HBtab = compiler->compHndBBtab, HBtabEnd = compiler->compHndBBtab + compiler->compHndBBtabCount;
         HBtab < HBtabEnd; HBtab++)
    {
        HBtab->ebdTryBeg->bbFlags |= BBF_HAS_LABEL;
        HBtab->ebdHndBeg->bbFlags |= BBF_HAS_LABEL;

        JITDUMP("  " FMT_BB " : try begin\n", HBtab->ebdTryBeg->bbNum);
        JITDUMP("  " FMT_BB " : hnd begin\n", HBtab->ebdHndBeg->bbNum);

        if (HBtab->ebdTryLast->bbNext != nullptr)
        {
            HBtab->ebdTryLast->bbNext->bbFlags |= BBF_HAS_LABEL;
            JITDUMP("  " FMT_BB " : try end\n", HBtab->ebdTryLast->bbNext->bbNum);
        }

        if (HBtab->ebdHndLast->bbNext != nullptr)
        {
            HBtab->ebdHndLast->bbNext->bbFlags |= BBF_HAS_LABEL;
            JITDUMP("  " FMT_BB " : hnd end\n", HBtab->ebdHndLast->bbNext->bbNum);
        }

        if (HBtab->HasFilter())
        {
            HBtab->ebdFilter->bbFlags |= BBF_HAS_LABEL;
            JITDUMP("  " FMT_BB " : filter begin\n", HBtab->ebdFilter->bbNum);
        }
    }

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("*************** After genMarkLabelsForCodegen()\n");
        compiler->fgDispBasicBlocks();
    }
#endif // DEBUG
}

void CodeGenInterface::genUpdateLife(GenTree* tree)
{
    treeLifeUpdater->UpdateLife(tree);
}

void CodeGenInterface::genUpdateLife(VARSET_VALARG_TP newLife)
{
    compiler->compUpdateLife</*ForCodeGen*/ true>(newLife);
}

// Return the register mask for the given register variable
// inline
regMaskTP CodeGenInterface::genGetRegMask(const LclVarDsc* varDsc)
{
    regMaskTP regMask = RBM_NONE;

    assert(varDsc->lvIsInReg());

    if (varTypeUsesFloatReg(varDsc->TypeGet()))
    {
        regMask = genRegMaskFloat(varDsc->GetRegNum(), varDsc->TypeGet());
    }
    else
    {
        regMask = genRegMask(varDsc->GetRegNum());
    }
    return regMask;
}

// Return the register mask for the given lclVar or regVar tree node
// inline
regMaskTP CodeGenInterface::genGetRegMask(GenTree* tree)
{
    assert(tree->gtOper == GT_LCL_VAR);

    regMaskTP        regMask = RBM_NONE;
    const LclVarDsc* varDsc  = compiler->lvaTable + tree->AsLclVarCommon()->GetLclNum();
    if (varDsc->lvPromoted)
    {
        for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
        {
            noway_assert(compiler->lvaTable[i].lvIsStructField);
            if (compiler->lvaTable[i].lvIsInReg())
            {
                regMask |= genGetRegMask(&compiler->lvaTable[i]);
            }
        }
    }
    else if (varDsc->lvIsInReg())
    {
        regMask = genGetRegMask(varDsc);
    }
    return regMask;
}

// The given lclVar is either going live (being born) or dying.
// It might be both going live and dying (that is, it is a dead store) under MinOpts.
// Update regSet.GetMaskVars() accordingly.
// inline
void CodeGenInterface::genUpdateRegLife(const LclVarDsc* varDsc, bool isBorn, bool isDying DEBUGARG(GenTree* tree))
{
    regMaskTP regMask = genGetRegMask(varDsc);

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("\t\t\t\t\t\t\tV%02u in reg ", (varDsc - compiler->lvaTable));
        varDsc->PrintVarReg();
        printf(" is becoming %s  ", (isDying) ? "dead" : "live");
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    if (isDying)
    {
        // We'd like to be able to assert the following, however if we are walking
        // through a qmark/colon tree, we may encounter multiple last-use nodes.
        // assert((regSet.GetMaskVars() & regMask) == regMask);
        regSet.RemoveMaskVars(regMask);
    }
    else
    {
        // If this is going live, the register must not have a variable in it, except
        // in the case of an exception variable, which may be already treated as live
        // in the register.
        assert(varDsc->lvLiveInOutOfHndlr || ((regSet.GetMaskVars() & regMask) == 0));
        regSet.AddMaskVars(regMask);
    }
}

//----------------------------------------------------------------------
// compHelperCallKillSet: Gets a register mask that represents the kill set for a helper call.
// Not all JIT Helper calls follow the standard ABI on the target architecture.
//
// TODO-CQ: Currently this list is incomplete (not all helpers calls are
//          enumerated) and not 100% accurate (some killsets are bigger than
//          what they really are).
//          There's some work to be done in several places in the JIT to
//          accurately track the registers that are getting killed by
//          helper calls:
//              a) LSRA needs several changes to accomodate more precise killsets
//                 for every helper call it sees (both explicitly [easy] and
//                 implicitly [hard])
//              b) Currently for AMD64, when we generate code for a helper call
//                 we're independently over-pessimizing the killsets of the call
//                 (independently from LSRA) and this needs changes
//                 both in CodeGenAmd64.cpp and emitx86.cpp.
//
//                 The best solution for this problem would be to try to centralize
//                 the killset information in a single place but then make the
//                 corresponding changes so every code generation phase is in sync
//                 about this.
//
//         The interim solution is to only add known helper calls that don't
//         follow the AMD64 ABI and actually trash registers that are supposed to be non-volatile.
//
// Arguments:
//   helper - The helper being inquired about
//
// Return Value:
//   Mask of register kills -- registers whose values are no longer guaranteed to be the same.
//
regMaskTP Compiler::compHelperCallKillSet(CorInfoHelpFunc helper)
{
    switch (helper)
    {
        case CORINFO_HELP_ASSIGN_BYREF:
#if defined(TARGET_AMD64)
            return RBM_RSI | RBM_RDI | RBM_CALLEE_TRASH_NOGC;
#elif defined(TARGET_ARMARCH)
            return RBM_CALLEE_TRASH_WRITEBARRIER_BYREF;
#elif defined(TARGET_X86)
            return RBM_ESI | RBM_EDI | RBM_ECX;
#else
            NYI("Model kill set for CORINFO_HELP_ASSIGN_BYREF on target arch");
            return RBM_CALLEE_TRASH;
#endif

#if defined(TARGET_ARMARCH)
        case CORINFO_HELP_ASSIGN_REF:
        case CORINFO_HELP_CHECKED_ASSIGN_REF:
            return RBM_CALLEE_TRASH_WRITEBARRIER;
#endif

        case CORINFO_HELP_PROF_FCN_ENTER:
#ifdef RBM_PROFILER_ENTER_TRASH
            return RBM_PROFILER_ENTER_TRASH;
#else
            NYI("Model kill set for CORINFO_HELP_PROF_FCN_ENTER on target arch");
#endif

        case CORINFO_HELP_PROF_FCN_LEAVE:
#ifdef RBM_PROFILER_LEAVE_TRASH
            return RBM_PROFILER_LEAVE_TRASH;
#else
            NYI("Model kill set for CORINFO_HELP_PROF_FCN_LEAVE on target arch");
#endif

        case CORINFO_HELP_PROF_FCN_TAILCALL:
#ifdef RBM_PROFILER_TAILCALL_TRASH
            return RBM_PROFILER_TAILCALL_TRASH;
#else
            NYI("Model kill set for CORINFO_HELP_PROF_FCN_TAILCALL on target arch");
#endif

#ifdef TARGET_X86
        case CORINFO_HELP_ASSIGN_REF_EAX:
        case CORINFO_HELP_ASSIGN_REF_ECX:
        case CORINFO_HELP_ASSIGN_REF_EBX:
        case CORINFO_HELP_ASSIGN_REF_EBP:
        case CORINFO_HELP_ASSIGN_REF_ESI:
        case CORINFO_HELP_ASSIGN_REF_EDI:

        case CORINFO_HELP_CHECKED_ASSIGN_REF_EAX:
        case CORINFO_HELP_CHECKED_ASSIGN_REF_ECX:
        case CORINFO_HELP_CHECKED_ASSIGN_REF_EBX:
        case CORINFO_HELP_CHECKED_ASSIGN_REF_EBP:
        case CORINFO_HELP_CHECKED_ASSIGN_REF_ESI:
        case CORINFO_HELP_CHECKED_ASSIGN_REF_EDI:
            return RBM_EDX;

#ifdef FEATURE_USE_ASM_GC_WRITE_BARRIERS
        case CORINFO_HELP_ASSIGN_REF:
        case CORINFO_HELP_CHECKED_ASSIGN_REF:
            return RBM_EAX | RBM_EDX;
#endif // FEATURE_USE_ASM_GC_WRITE_BARRIERS
#endif

        case CORINFO_HELP_STOP_FOR_GC:
            return RBM_STOP_FOR_GC_TRASH;

        case CORINFO_HELP_INIT_PINVOKE_FRAME:
            return RBM_INIT_PINVOKE_FRAME_TRASH;

        default:
            return RBM_CALLEE_TRASH;
    }
}

//------------------------------------------------------------------------
// compChangeLife: Compare the given "newLife" with last set of live variables and update
//  codeGen "gcInfo", siScopes, "regSet" with the new variable's homes/liveness.
//
// Arguments:
//    newLife - the new set of variables that are alive.
//
// Assumptions:
//    The set of live variables reflects the result of only emitted code, it should not be considering the becoming
//    live/dead of instructions that has not been emitted yet. This is used to ensure [) "VariableLiveRange"
//    intervals when calling "siStartVariableLiveRange" and "siEndVariableLiveRange".
//
// Notes:
//    If "ForCodeGen" is false, only "compCurLife" set (and no mask) will be setted.
//
template <bool ForCodeGen>
void Compiler::compChangeLife(VARSET_VALARG_TP newLife)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("Change life %s ", VarSetOps::ToString(this, compCurLife));
        dumpConvertedVarSet(this, compCurLife);
        printf(" -> %s ", VarSetOps::ToString(this, newLife));
        dumpConvertedVarSet(this, newLife);
        printf("\n");
    }
#endif // DEBUG

    /* We should only be called when the live set has actually changed */

    noway_assert(!VarSetOps::Equal(this, compCurLife, newLife));

    if (!ForCodeGen)
    {
        VarSetOps::Assign(this, compCurLife, newLife);
        return;
    }

    /* Figure out which variables are becoming live/dead at this point */

    // deadSet = compCurLife - newLife
    VARSET_TP deadSet(VarSetOps::Diff(this, compCurLife, newLife));

    // bornSet = newLife - compCurLife
    VARSET_TP bornSet(VarSetOps::Diff(this, newLife, compCurLife));

    /* Can't simultaneously become live and dead at the same time */

    // (deadSet UNION bornSet) != EMPTY
    noway_assert(!VarSetOps::IsEmptyUnion(this, deadSet, bornSet));
    // (deadSet INTERSECTION bornSet) == EMPTY
    noway_assert(VarSetOps::IsEmptyIntersection(this, deadSet, bornSet));

    VarSetOps::Assign(this, compCurLife, newLife);

    // Handle the dying vars first, then the newly live vars.
    // This is because, in the RyuJIT backend case, they may occupy registers that
    // will be occupied by another var that is newly live.
    VarSetOps::Iter deadIter(this, deadSet);
    unsigned        deadVarIndex = 0;
    while (deadIter.NextElem(&deadVarIndex))
    {
        unsigned   varNum     = lvaTrackedIndexToLclNum(deadVarIndex);
        LclVarDsc* varDsc     = lvaGetDesc(varNum);
        bool       isGCRef    = (varDsc->TypeGet() == TYP_REF);
        bool       isByRef    = (varDsc->TypeGet() == TYP_BYREF);
        bool       isInReg    = varDsc->lvIsInReg();
        bool       isInMemory = !isInReg || varDsc->lvLiveInOutOfHndlr;

        if (isInReg)
        {
            // TODO-Cleanup: Move the code from compUpdateLifeVar to genUpdateRegLife that updates the
            // gc sets
            regMaskTP regMask = varDsc->lvRegMask();
            if (isGCRef)
            {
                codeGen->gcInfo.gcRegGCrefSetCur &= ~regMask;
            }
            else if (isByRef)
            {
                codeGen->gcInfo.gcRegByrefSetCur &= ~regMask;
            }
            codeGen->genUpdateRegLife(varDsc, false /*isBorn*/, true /*isDying*/ DEBUGARG(nullptr));
        }
        // Update the gcVarPtrSetCur if it is in memory.
        if (isInMemory && (isGCRef || isByRef))
        {
            VarSetOps::RemoveElemD(this, codeGen->gcInfo.gcVarPtrSetCur, deadVarIndex);
            JITDUMP("\t\t\t\t\t\t\tV%02u becoming dead\n", varNum);
        }

#ifdef USING_VARIABLE_LIVE_RANGE
        codeGen->getVariableLiveKeeper()->siEndVariableLiveRange(varNum);
#endif // USING_VARIABLE_LIVE_RANGE
    }

    VarSetOps::Iter bornIter(this, bornSet);
    unsigned        bornVarIndex = 0;
    while (bornIter.NextElem(&bornVarIndex))
    {
        unsigned   varNum  = lvaTrackedIndexToLclNum(bornVarIndex);
        LclVarDsc* varDsc  = lvaGetDesc(varNum);
        bool       isGCRef = (varDsc->TypeGet() == TYP_REF);
        bool       isByRef = (varDsc->TypeGet() == TYP_BYREF);

        if (varDsc->lvIsInReg())
        {
            // If this variable is going live in a register, it is no longer live on the stack,
            // unless it is an EH var, which always remains live on the stack.
            if (!varDsc->lvLiveInOutOfHndlr)
            {
#ifdef DEBUG
                if (VarSetOps::IsMember(this, codeGen->gcInfo.gcVarPtrSetCur, bornVarIndex))
                {
                    JITDUMP("\t\t\t\t\t\t\tRemoving V%02u from gcVarPtrSetCur\n", varNum);
                }
#endif // DEBUG
                VarSetOps::RemoveElemD(this, codeGen->gcInfo.gcVarPtrSetCur, bornVarIndex);
            }
            codeGen->genUpdateRegLife(varDsc, true /*isBorn*/, false /*isDying*/ DEBUGARG(nullptr));
            regMaskTP regMask = varDsc->lvRegMask();
            if (isGCRef)
            {
                codeGen->gcInfo.gcRegGCrefSetCur |= regMask;
            }
            else if (isByRef)
            {
                codeGen->gcInfo.gcRegByrefSetCur |= regMask;
            }
        }
        else if (lvaIsGCTracked(varDsc))
        {
            // This isn't in a register, so update the gcVarPtrSetCur to show that it's live on the stack.
            VarSetOps::AddElemD(this, codeGen->gcInfo.gcVarPtrSetCur, bornVarIndex);
            JITDUMP("\t\t\t\t\t\t\tV%02u becoming live\n", varNum);
        }

#ifdef USING_VARIABLE_LIVE_RANGE
        codeGen->getVariableLiveKeeper()->siStartVariableLiveRange(varDsc, varNum);
#endif // USING_VARIABLE_LIVE_RANGE
    }

#ifdef USING_SCOPE_INFO
    codeGen->siUpdate();
#endif // USING_SCOPE_INFO
}

// Need an explicit instantiation.
template void Compiler::compChangeLife<true>(VARSET_VALARG_TP newLife);

/*****************************************************************************
 *
 *  Generate a spill.
 */
void CodeGenInterface::spillReg(var_types type, TempDsc* tmp, regNumber reg)
{
    GetEmitter()->emitIns_S_R(ins_Store(type), emitActualTypeSize(type), reg, tmp->tdTempNum(), 0);
}

/*****************************************************************************
 *
 *  Generate a reload.
 */
void CodeGenInterface::reloadReg(var_types type, TempDsc* tmp, regNumber reg)
{
    GetEmitter()->emitIns_R_S(ins_Load(type), emitActualTypeSize(type), reg, tmp->tdTempNum(), 0);
}

// inline
regNumber CodeGenInterface::genGetThisArgReg(GenTreeCall* call) const
{
    return REG_ARG_0;
}

//----------------------------------------------------------------------
// getSpillTempDsc: get the TempDsc corresponding to a spilled tree.
//
// Arguments:
//   tree  -  spilled GenTree node
//
// Return Value:
//   TempDsc corresponding to tree
TempDsc* CodeGenInterface::getSpillTempDsc(GenTree* tree)
{
    // tree must be in spilled state.
    assert((tree->gtFlags & GTF_SPILLED) != 0);

    // Get the tree's SpillDsc.
    RegSet::SpillDsc* prevDsc;
    RegSet::SpillDsc* spillDsc = regSet.rsGetSpillInfo(tree, tree->GetRegNum(), &prevDsc);
    assert(spillDsc != nullptr);

    // Get the temp desc.
    TempDsc* temp = regSet.rsGetSpillTempWord(tree->GetRegNum(), spillDsc, prevDsc);
    return temp;
}

#ifdef TARGET_XARCH

#ifdef TARGET_AMD64
// Returns relocation type hint for an addr.
// Note that there are no reloc hints on x86.
//
// Arguments
//    addr  -  data address
//
// Returns
//    relocation type hint
//
unsigned short CodeGenInterface::genAddrRelocTypeHint(size_t addr)
{
    return compiler->eeGetRelocTypeHint((void*)addr);
}
#endif // TARGET_AMD64

// Return true if an absolute indirect data address can be encoded as IP-relative.
// offset. Note that this method should be used only when the caller knows that
// the address is an icon value that VM has given and there is no GenTree node
// representing it. Otherwise, one should always use FitsInAddrBase().
//
// Arguments
//    addr  -  an absolute indirect data address
//
// Returns
//    true if indir data addr could be encoded as IP-relative offset.
//
bool CodeGenInterface::genDataIndirAddrCanBeEncodedAsPCRelOffset(size_t addr)
{
#ifdef TARGET_AMD64
    return genAddrRelocTypeHint(addr) == IMAGE_REL_BASED_REL32;
#else
    // x86: PC-relative addressing is available only for control flow instructions (jmp and call)
    return false;
#endif
}

// Return true if an indirect code address can be encoded as IP-relative offset.
// Note that this method should be used only when the caller knows that the
// address is an icon value that VM has given and there is no GenTree node
// representing it. Otherwise, one should always use FitsInAddrBase().
//
// Arguments
//    addr  -  an absolute indirect code address
//
// Returns
//    true if indir code addr could be encoded as IP-relative offset.
//
bool CodeGenInterface::genCodeIndirAddrCanBeEncodedAsPCRelOffset(size_t addr)
{
#ifdef TARGET_AMD64
    return genAddrRelocTypeHint(addr) == IMAGE_REL_BASED_REL32;
#else
    // x86: PC-relative addressing is available only for control flow instructions (jmp and call)
    return true;
#endif
}

// Return true if an indirect code address can be encoded as 32-bit displacement
// relative to zero. Note that this method should be used only when the caller
// knows that the address is an icon value that VM has given and there is no
// GenTree node representing it. Otherwise, one should always use FitsInAddrBase().
//
// Arguments
//    addr  -  absolute indirect code address
//
// Returns
//    true if absolute indir code addr could be encoded as 32-bit displacement relative to zero.
//
bool CodeGenInterface::genCodeIndirAddrCanBeEncodedAsZeroRelOffset(size_t addr)
{
    return GenTreeIntConCommon::FitsInI32((ssize_t)addr);
}

// Return true if an absolute indirect code address needs a relocation recorded with VM.
//
// Arguments
//    addr  -  an absolute indirect code address
//
// Returns
//    true if indir code addr needs a relocation recorded with VM
//
bool CodeGenInterface::genCodeIndirAddrNeedsReloc(size_t addr)
{
    // If generating relocatable ngen code, then all code addr should go through relocation
    if (compiler->opts.compReloc)
    {
        return true;
    }

#ifdef TARGET_AMD64
    // See if the code indir addr can be encoded as 32-bit displacement relative to zero.
    // We don't need a relocation in that case.
    if (genCodeIndirAddrCanBeEncodedAsZeroRelOffset(addr))
    {
        return false;
    }

    // Else we need a relocation.
    return true;
#else  // TARGET_X86
    // On x86 there is no need to record or ask for relocations during jitting,
    // because all addrs fit within 32-bits.
    return false;
#endif // TARGET_X86
}

// Return true if a direct code address needs to be marked as relocatable.
//
// Arguments
//    addr  -  absolute direct code address
//
// Returns
//    true if direct code addr needs a relocation recorded with VM
//
bool CodeGenInterface::genCodeAddrNeedsReloc(size_t addr)
{
    // If generating relocatable ngen code, then all code addr should go through relocation
    if (compiler->opts.compReloc)
    {
        return true;
    }

#ifdef TARGET_AMD64
    // By default all direct code addresses go through relocation so that VM will setup
    // a jump stub if addr cannot be encoded as pc-relative offset.
    return true;
#else  // TARGET_X86
    // On x86 there is no need for recording relocations during jitting,
    // because all addrs fit within 32-bits.
    return false;
#endif // TARGET_X86
}
#endif // TARGET_XARCH

/*****************************************************************************
 *
 *  The following can be used to create basic blocks that serve as labels for
 *  the emitter. Use with caution - these are not real basic blocks!
 *
 */

// inline
BasicBlock* CodeGen::genCreateTempLabel()
{
#ifdef DEBUG
    // These blocks don't affect FP
    compiler->fgSafeBasicBlockCreation = true;
#endif

    BasicBlock* block = compiler->bbNewBasicBlock(BBJ_NONE);

#ifdef DEBUG
    compiler->fgSafeBasicBlockCreation = false;
#endif

    JITDUMP("Mark " FMT_BB " as label: codegen temp block\n", block->bbNum);
    block->bbFlags |= BBF_HAS_LABEL;

    // Use coldness of current block, as this label will
    // be contained in it.
    block->bbFlags |= (compiler->compCurBB->bbFlags & BBF_COLD);

#ifdef DEBUG
#ifdef UNIX_X86_ABI
    block->bbTgtStkDepth = (genStackLevel - curNestedAlignment) / sizeof(int);
#else
    block->bbTgtStkDepth = genStackLevel / sizeof(int);
#endif
#endif
    return block;
}

void CodeGen::genLogLabel(BasicBlock* bb)
{
#ifdef DEBUG
    if (compiler->opts.dspCode)
    {
        printf("\n      L_M%03u_" FMT_BB ":\n", compiler->compMethodID, bb->bbNum);
    }
#endif
}

// genDefineTempLabel: Define a label based on the current GC info tracked by
// the code generator.
//
// Arguments:
//     label - A label represented as a basic block. These are created with
//     genCreateTempLabel and are not normal basic blocks.
//
// Notes:
//     The label will be defined with the current GC info tracked by the code
//     generator. When the emitter sees this label it will thus remove any temporary
//     GC refs it is tracking in registers. For example, a call might produce a ref
//     in RAX which the emitter would track but which would not be tracked in
//     codegen's GC info since codegen would immediately copy it from RAX into its
//     home.
//
void CodeGen::genDefineTempLabel(BasicBlock* label)
{
    genLogLabel(label);
    label->bbEmitCookie = GetEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                     gcInfo.gcRegByrefSetCur, false DEBUG_ARG(label->bbNum));
}

// genDefineInlineTempLabel: Define an inline label that does not affect the GC
// info.
//
// Arguments:
//     label - A label represented as a basic block. These are created with
//     genCreateTempLabel and are not normal basic blocks.
//
// Notes:
//     The emitter will continue to track GC info as if there was no label.
//
void CodeGen::genDefineInlineTempLabel(BasicBlock* label)
{
    genLogLabel(label);
    label->bbEmitCookie = GetEmitter()->emitAddInlineLabel();
}

/*****************************************************************************
 *
 *  Adjust the stack pointer by the given value; assumes that this follows
 *  a call so only callee-saved registers (and registers that may hold a
 *  return value) are used at this point.
 */

void CodeGen::genAdjustSP(target_ssize_t delta)
{
#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
    if (delta == sizeof(int))
        inst_RV(INS_pop, REG_ECX, TYP_INT);
    else
#endif
        inst_RV_IV(INS_add, REG_SPBASE, delta, EA_PTRSIZE);
}

//------------------------------------------------------------------------
// genAdjustStackLevel: Adjust the stack level, if required, for a throw helper block
//
// Arguments:
//    block - The BasicBlock for which we are about to generate code.
//
// Assumptions:
//    Must be called just prior to generating code for 'block'.
//
// Notes:
//    This only makes an adjustment if !FEATURE_FIXED_OUT_ARGS, if there is no frame pointer,
//    and if 'block' is a throw helper block with a non-zero stack level.

void CodeGen::genAdjustStackLevel(BasicBlock* block)
{
#if !FEATURE_FIXED_OUT_ARGS
    // Check for inserted throw blocks and adjust genStackLevel.
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(UNIX_X86_ABI)
    if (isFramePointerUsed() && compiler->fgIsThrowHlpBlk(block))
    {
        // x86/Linux requires stack frames to be 16-byte aligned, but SP may be unaligned
        // at this point if a jump to this block is made in the middle of pushing arugments.
        //
        // Here we restore SP to prevent potential stack alignment issues.
        GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, -genSPtoFPdelta());
    }
#endif

    if (!isFramePointerUsed() && compiler->fgIsThrowHlpBlk(block))
    {
        noway_assert(block->bbFlags & BBF_HAS_LABEL);

        SetStackLevel(compiler->fgThrowHlpBlkStkLevel(block) * sizeof(int));

        if (genStackLevel != 0)
        {
#ifdef TARGET_X86
            GetEmitter()->emitMarkStackLvl(genStackLevel);
            inst_RV_IV(INS_add, REG_SPBASE, genStackLevel, EA_PTRSIZE);
            SetStackLevel(0);
#else  // TARGET_X86
            NYI("Need emitMarkStackLvl()");
#endif // TARGET_X86
        }
    }
#endif // !FEATURE_FIXED_OUT_ARGS
}

#ifdef TARGET_ARMARCH
// return size
// alignmentWB is out param
unsigned CodeGenInterface::InferOpSizeAlign(GenTree* op, unsigned* alignmentWB)
{
    unsigned alignment = 0;
    unsigned opSize    = 0;

    if (op->gtType == TYP_STRUCT || op->OperIsCopyBlkOp())
    {
        opSize = InferStructOpSizeAlign(op, &alignment);
    }
    else
    {
        alignment = genTypeAlignments[op->TypeGet()];
        opSize    = genTypeSizes[op->TypeGet()];
    }

    assert(opSize != 0);
    assert(alignment != 0);

    (*alignmentWB) = alignment;
    return opSize;
}
// return size
// alignmentWB is out param
unsigned CodeGenInterface::InferStructOpSizeAlign(GenTree* op, unsigned* alignmentWB)
{
    unsigned alignment = 0;
    unsigned opSize    = 0;

    while (op->gtOper == GT_COMMA)
    {
        op = op->AsOp()->gtOp2;
    }

    if (op->gtOper == GT_OBJ)
    {
        CORINFO_CLASS_HANDLE clsHnd = op->AsObj()->GetLayout()->GetClassHandle();
        opSize                      = op->AsObj()->GetLayout()->GetSize();
        alignment = roundUp(compiler->info.compCompHnd->getClassAlignmentRequirement(clsHnd), TARGET_POINTER_SIZE);
    }
    else if (op->gtOper == GT_LCL_VAR)
    {
        unsigned   varNum = op->AsLclVarCommon()->GetLclNum();
        LclVarDsc* varDsc = compiler->lvaTable + varNum;
        assert(varDsc->lvType == TYP_STRUCT);
        opSize = varDsc->lvSize();
#ifndef TARGET_64BIT
        if (varDsc->lvStructDoubleAlign)
        {
            alignment = TARGET_POINTER_SIZE * 2;
        }
        else
#endif // !TARGET_64BIT
        {
            alignment = TARGET_POINTER_SIZE;
        }
    }
    else if (op->OperIsCopyBlkOp())
    {
        GenTree* op2 = op->AsOp()->gtOp2;

        if (op2->OperGet() == GT_CNS_INT)
        {
            if (op2->IsIconHandle(GTF_ICON_CLASS_HDL))
            {
                CORINFO_CLASS_HANDLE clsHnd = (CORINFO_CLASS_HANDLE)op2->AsIntCon()->gtIconVal;
                opSize = roundUp(compiler->info.compCompHnd->getClassSize(clsHnd), TARGET_POINTER_SIZE);
                alignment =
                    roundUp(compiler->info.compCompHnd->getClassAlignmentRequirement(clsHnd), TARGET_POINTER_SIZE);
            }
            else
            {
                opSize       = (unsigned)op2->AsIntCon()->gtIconVal;
                GenTree* op1 = op->AsOp()->gtOp1;
                assert(op1->OperGet() == GT_LIST);
                GenTree* dstAddr = op1->AsOp()->gtOp1;
                if (dstAddr->OperGet() == GT_ADDR)
                {
                    InferStructOpSizeAlign(dstAddr->AsOp()->gtOp1, &alignment);
                }
                else
                {
                    assert(!"Unhandle dstAddr node");
                    alignment = TARGET_POINTER_SIZE;
                }
            }
        }
        else
        {
            noway_assert(!"Variable sized COPYBLK register arg!");
            opSize    = 0;
            alignment = TARGET_POINTER_SIZE;
        }
    }
    else if (op->gtOper == GT_MKREFANY)
    {
        opSize    = TARGET_POINTER_SIZE * 2;
        alignment = TARGET_POINTER_SIZE;
    }
    else if (op->IsArgPlaceHolderNode())
    {
        CORINFO_CLASS_HANDLE clsHnd = op->AsArgPlace()->gtArgPlaceClsHnd;
        assert(clsHnd != 0);
        opSize    = roundUp(compiler->info.compCompHnd->getClassSize(clsHnd), TARGET_POINTER_SIZE);
        alignment = roundUp(compiler->info.compCompHnd->getClassAlignmentRequirement(clsHnd), TARGET_POINTER_SIZE);
    }
    else
    {
        assert(!"Unhandled gtOper");
        opSize    = TARGET_POINTER_SIZE;
        alignment = TARGET_POINTER_SIZE;
    }

    assert(opSize != 0);
    assert(alignment != 0);

    (*alignmentWB) = alignment;
    return opSize;
}

#endif // TARGET_ARMARCH

/*****************************************************************************
 *
 *  Take an address expression and try to find the best set of components to
 *  form an address mode; returns non-zero if this is successful.
 *
 *  TODO-Cleanup: The RyuJIT backend never uses this to actually generate code.
 *  Refactor this code so that the underlying analysis can be used in
 *  the RyuJIT Backend to do lowering, instead of having to call this method with the
 *  option to not generate the code.
 *
 *  'fold' specifies if it is OK to fold the array index which hangs off
 *  a GT_NOP node.
 *
 *  If successful, the parameters will be set to the following values:
 *
 *      *rv1Ptr     ...     base operand
 *      *rv2Ptr     ...     optional operand
 *      *revPtr     ...     true if rv2 is before rv1 in the evaluation order
 *  #if SCALED_ADDR_MODES
 *      *mulPtr     ...     optional multiplier (2/4/8) for rv2
 *                          Note that for [reg1 + reg2] and [reg1 + reg2 + icon], *mulPtr == 0.
 *  #endif
 *      *cnsPtr     ...     integer constant [optional]
 *
 *  IMPORTANT NOTE: This routine doesn't generate any code, it merely
 *                  identifies the components that might be used to
 *                  form an address mode later on.
 */

bool CodeGen::genCreateAddrMode(GenTree*  addr,
                                bool      fold,
                                bool*     revPtr,
                                GenTree** rv1Ptr,
                                GenTree** rv2Ptr,
#if SCALED_ADDR_MODES
                                unsigned* mulPtr,
#endif // SCALED_ADDR_MODES
                                ssize_t* cnsPtr)
{
    /*
        The following indirections are valid address modes on x86/x64:

            [                  icon]      * not handled here
            [reg                   ]
            [reg             + icon]
            [reg1 +     reg2       ]
            [reg1 +     reg2 + icon]
            [reg1 + 2 * reg2       ]
            [reg1 + 4 * reg2       ]
            [reg1 + 8 * reg2       ]
            [       2 * reg2 + icon]
            [       4 * reg2 + icon]
            [       8 * reg2 + icon]
            [reg1 + 2 * reg2 + icon]
            [reg1 + 4 * reg2 + icon]
            [reg1 + 8 * reg2 + icon]

        The following indirections are valid address modes on arm64:

            [reg]
            [reg  + icon]
            [reg1 + reg2]
            [reg1 + reg2 * natural-scale]

     */

    /* All indirect address modes require the address to be an addition */

    if (addr->gtOper != GT_ADD)
    {
        return false;
    }

    // Can't use indirect addressing mode as we need to check for overflow.
    // Also, can't use 'lea' as it doesn't set the flags.

    if (addr->gtOverflow())
    {
        return false;
    }

    GenTree* rv1 = nullptr;
    GenTree* rv2 = nullptr;

    GenTree* op1;
    GenTree* op2;

    ssize_t cns;
#if SCALED_ADDR_MODES
    unsigned mul;
#endif // SCALED_ADDR_MODES

    GenTree* tmp;

    /* What order are the sub-operands to be evaluated */

    if (addr->gtFlags & GTF_REVERSE_OPS)
    {
        op1 = addr->AsOp()->gtOp2;
        op2 = addr->AsOp()->gtOp1;
    }
    else
    {
        op1 = addr->AsOp()->gtOp1;
        op2 = addr->AsOp()->gtOp2;
    }

    bool rev = false; // Is op2 first in the evaluation order?

    /*
        A complex address mode can combine the following operands:

            op1     ...     base address
            op2     ...     optional scaled index
#if SCALED_ADDR_MODES
            mul     ...     optional multiplier (2/4/8) for op2
#endif
            cns     ...     optional displacement

        Here we try to find such a set of operands and arrange for these
        to sit in registers.
     */

    cns = 0;
#if SCALED_ADDR_MODES
    mul = 0;
#endif // SCALED_ADDR_MODES

AGAIN:
    /* We come back to 'AGAIN' if we have an add of a constant, and we are folding that
       constant, or we have gone through a GT_NOP or GT_COMMA node. We never come back
       here if we find a scaled index.
    */
    CLANG_FORMAT_COMMENT_ANCHOR;

#if SCALED_ADDR_MODES
    assert(mul == 0);
#endif // SCALED_ADDR_MODES

    /* Special case: keep constants as 'op2' */

    if (op1->IsCnsIntOrI())
    {
        // Presumably op2 is assumed to not be a constant (shouldn't happen if we've done constant folding)?
        tmp = op1;
        op1 = op2;
        op2 = tmp;
    }

    /* Check for an addition of a constant */

    if (op2->IsIntCnsFitsInI32() && (op2->gtType != TYP_REF) && FitsIn<INT32>(cns + op2->AsIntConCommon()->IconValue()))
    {
        // We should not be building address modes out of non-foldable constants
        assert(op2->AsIntConCommon()->ImmedValCanBeFolded(compiler, addr->OperGet()));

        /* We're adding a constant */

        cns += op2->AsIntConCommon()->IconValue();

#if defined(TARGET_ARMARCH)
        if (cns == 0)
#endif
        {
            /* Inspect the operand the constant is being added to */

            switch (op1->gtOper)
            {
                case GT_ADD:

                    if (op1->gtOverflow())
                    {
                        break;
                    }

                    op2 = op1->AsOp()->gtOp2;
                    op1 = op1->AsOp()->gtOp1;

                    goto AGAIN;

#if SCALED_ADDR_MODES && !defined(TARGET_ARMARCH)
                // TODO-ARM64-CQ, TODO-ARM-CQ: For now we don't try to create a scaled index.
                case GT_MUL:
                    if (op1->gtOverflow())
                    {
                        return false; // Need overflow check
                    }

                    FALLTHROUGH;

                case GT_LSH:

                    mul = op1->GetScaledIndex();
                    if (mul)
                    {
                        /* We can use "[mul*rv2 + icon]" */

                        rv1 = nullptr;
                        rv2 = op1->AsOp()->gtOp1;

                        goto FOUND_AM;
                    }
                    break;
#endif // SCALED_ADDR_MODES && !defined(TARGET_ARMARCH)

                default:
                    break;
            }
        }

        /* The best we can do is "[rv1 + icon]" */

        rv1 = op1;
        rv2 = nullptr;

        goto FOUND_AM;
    }

    // op2 is not a constant. So keep on trying.

    /* Neither op1 nor op2 are sitting in a register right now */

    switch (op1->gtOper)
    {
#if !defined(TARGET_ARMARCH)
        // TODO-ARM64-CQ, TODO-ARM-CQ: For now we don't try to create a scaled index.
        case GT_ADD:

            if (op1->gtOverflow())
            {
                break;
            }

            if (op1->AsOp()->gtOp2->IsIntCnsFitsInI32() &&
                FitsIn<INT32>(cns + op1->AsOp()->gtOp2->AsIntCon()->gtIconVal))
            {
                cns += op1->AsOp()->gtOp2->AsIntCon()->gtIconVal;
                op1 = op1->AsOp()->gtOp1;

                goto AGAIN;
            }

            break;

#if SCALED_ADDR_MODES

        case GT_MUL:

            if (op1->gtOverflow())
            {
                break;
            }

            FALLTHROUGH;

        case GT_LSH:

            mul = op1->GetScaledIndex();
            if (mul)
            {
                /* 'op1' is a scaled value */

                rv1 = op2;
                rv2 = op1->AsOp()->gtOp1;

                int argScale;
                while ((rv2->gtOper == GT_MUL || rv2->gtOper == GT_LSH) && (argScale = rv2->GetScaledIndex()) != 0)
                {
                    if (jitIsScaleIndexMul(argScale * mul))
                    {
                        mul = mul * argScale;
                        rv2 = rv2->AsOp()->gtOp1;
                    }
                    else
                    {
                        break;
                    }
                }

                noway_assert(rev == false);
                rev = true;

                goto FOUND_AM;
            }
            break;

#endif // SCALED_ADDR_MODES
#endif // !TARGET_ARMARCH

        case GT_NOP:

            op1 = op1->AsOp()->gtOp1;
            goto AGAIN;

        case GT_COMMA:

            op1 = op1->AsOp()->gtOp2;
            goto AGAIN;

        default:
            break;
    }

    noway_assert(op2);
    switch (op2->gtOper)
    {
#if !defined(TARGET_ARMARCH)
        // TODO-ARM64-CQ, TODO-ARM-CQ: For now we don't try to create a scaled index.
        case GT_ADD:

            if (op2->gtOverflow())
            {
                break;
            }

            if (op2->AsOp()->gtOp2->IsIntCnsFitsInI32() &&
                FitsIn<INT32>(cns + op2->AsOp()->gtOp2->AsIntCon()->gtIconVal))
            {
                cns += op2->AsOp()->gtOp2->AsIntCon()->gtIconVal;
                op2 = op2->AsOp()->gtOp1;

                goto AGAIN;
            }

            break;

#if SCALED_ADDR_MODES

        case GT_MUL:

            if (op2->gtOverflow())
            {
                break;
            }

            FALLTHROUGH;

        case GT_LSH:

            mul = op2->GetScaledIndex();
            if (mul)
            {
                // 'op2' is a scaled value...is it's argument also scaled?
                int argScale;
                rv2 = op2->AsOp()->gtOp1;
                while ((rv2->gtOper == GT_MUL || rv2->gtOper == GT_LSH) && (argScale = rv2->GetScaledIndex()) != 0)
                {
                    if (jitIsScaleIndexMul(argScale * mul))
                    {
                        mul = mul * argScale;
                        rv2 = rv2->AsOp()->gtOp1;
                    }
                    else
                    {
                        break;
                    }
                }

                rv1 = op1;

                goto FOUND_AM;
            }
            break;

#endif // SCALED_ADDR_MODES
#endif // !TARGET_ARMARCH

        case GT_NOP:

            op2 = op2->AsOp()->gtOp1;
            goto AGAIN;

        case GT_COMMA:

            op2 = op2->AsOp()->gtOp2;
            goto AGAIN;

        default:
            break;
    }

    /* The best we can do "[rv1 + rv2]" or "[rv1 + rv2 + cns]" */

    rv1 = op1;
    rv2 = op2;
#ifdef TARGET_ARM64
    assert(cns == 0);
#endif

FOUND_AM:

    if (rv2)
    {
        /* Make sure a GC address doesn't end up in 'rv2' */

        if (varTypeIsGC(rv2->TypeGet()))
        {
            noway_assert(rv1 && !varTypeIsGC(rv1->TypeGet()));

            tmp = rv1;
            rv1 = rv2;
            rv2 = tmp;

            rev = !rev;
        }

        /* Special case: constant array index (that is range-checked) */

        if (fold)
        {
            ssize_t  tmpMul;
            GenTree* index;

            if ((rv2->gtOper == GT_MUL || rv2->gtOper == GT_LSH) && (rv2->AsOp()->gtOp2->IsCnsIntOrI()))
            {
                /* For valuetype arrays where we can't use the scaled address
                   mode, rv2 will point to the scaled index. So we have to do
                   more work */

                tmpMul = compiler->optGetArrayRefScaleAndIndex(rv2, &index DEBUGARG(false));
                if (mul)
                {
                    tmpMul *= mul;
                }
            }
            else
            {
                /* May be a simple array. rv2 will points to the actual index */

                index  = rv2;
                tmpMul = mul;
            }

            /* Get hold of the array index and see if it's a constant */
            if (index->IsIntCnsFitsInI32())
            {
                /* Get hold of the index value */
                ssize_t ixv = index->AsIntConCommon()->IconValue();

#if SCALED_ADDR_MODES
                /* Scale the index if necessary */
                if (tmpMul)
                {
                    ixv *= tmpMul;
                }
#endif

                if (FitsIn<INT32>(cns + ixv))
                {
                    /* Add the scaled index to the offset value */

                    cns += ixv;

#if SCALED_ADDR_MODES
                    /* There is no scaled operand any more */
                    mul = 0;
#endif
                    rv2 = nullptr;
                }
            }
        }
    }

    // We shouldn't have [rv2*1 + cns] - this is equivalent to [rv1 + cns]
    noway_assert(rv1 || mul != 1);

    noway_assert(FitsIn<INT32>(cns));

    if (rv1 == nullptr && rv2 == nullptr)
    {
        return false;
    }

    /* Success - return the various components to the caller */

    *revPtr = rev;
    *rv1Ptr = rv1;
    *rv2Ptr = rv2;
#if SCALED_ADDR_MODES
    *mulPtr = mul;
#endif
    *cnsPtr = cns;

    return true;
}

#ifdef TARGET_ARMARCH
//------------------------------------------------------------------------
// genEmitGSCookieCheck: Generate code to check that the GS cookie
// wasn't thrashed by a buffer overrun. Common code for ARM32 and ARM64.
//
void CodeGen::genEmitGSCookieCheck(bool pushReg)
{
    noway_assert(compiler->gsGlobalSecurityCookieAddr || compiler->gsGlobalSecurityCookieVal);

    // Make sure that the return register is reported as live GC-ref so that any GC that kicks in while
    // executing GS cookie check will not collect the object pointed to by REG_INTRET (R0).
    if (!pushReg && (compiler->info.compRetNativeType == TYP_REF))
        gcInfo.gcRegGCrefSetCur |= RBM_INTRET;

    // We need two temporary registers, to load the GS cookie values and compare them. We can't use
    // any argument registers if 'pushReg' is true (meaning we have a JMP call). They should be
    // callee-trash registers, which should not contain anything interesting at this point.
    // We don't have any IR node representing this check, so LSRA can't communicate registers
    // for us to use.

    regNumber regGSConst = REG_GSCOOKIE_TMP_0;
    regNumber regGSValue = REG_GSCOOKIE_TMP_1;

    if (compiler->gsGlobalSecurityCookieAddr == nullptr)
    {
        // load the GS cookie constant into a reg
        //
        genSetRegToIcon(regGSConst, compiler->gsGlobalSecurityCookieVal, TYP_I_IMPL);
    }
    else
    {
        // Ngen case - GS cookie constant needs to be accessed through an indirection.
        instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, regGSConst, (ssize_t)compiler->gsGlobalSecurityCookieAddr,
                               INS_FLAGS_DONT_CARE DEBUGARG((size_t)THT_GSCookieCheck) DEBUGARG(0));
        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, regGSConst, regGSConst, 0);
    }
    // Load this method's GS value from the stack frame
    GetEmitter()->emitIns_R_S(INS_ldr, EA_PTRSIZE, regGSValue, compiler->lvaGSSecurityCookie, 0);
    // Compare with the GC cookie constant
    GetEmitter()->emitIns_R_R(INS_cmp, EA_PTRSIZE, regGSConst, regGSValue);

    BasicBlock* gsCheckBlk = genCreateTempLabel();
    inst_JMP(EJ_eq, gsCheckBlk);
    // regGSConst and regGSValue aren't needed anymore, we can use them for helper call
    genEmitHelperCall(CORINFO_HELP_FAIL_FAST, 0, EA_UNKNOWN, regGSConst);
    genDefineTempLabel(gsCheckBlk);
}
#endif // TARGET_ARMARCH

/*****************************************************************************
 *
 *  Generate an exit sequence for a return from a method (note: when compiling
 *  for speed there might be multiple exit points).
 */

void CodeGen::genExitCode(BasicBlock* block)
{
    /* Just wrote the first instruction of the epilog - inform debugger
       Note that this may result in a duplicate IPmapping entry, and
       that this is ok  */

    // For non-optimized debuggable code, there is only one epilog.
    genIPmappingAdd((IL_OFFSETX)ICorDebugInfo::EPILOG, true);

    bool jmpEpilog = ((block->bbFlags & BBF_HAS_JMP) != 0);
    if (compiler->getNeedsGSSecurityCookie())
    {
        genEmitGSCookieCheck(jmpEpilog);

        if (jmpEpilog)
        {
            // Dev10 642944 -
            // The GS cookie check created a temp label that has no live
            // incoming GC registers, we need to fix that

            unsigned   varNum;
            LclVarDsc* varDsc;

            /* Figure out which register parameters hold pointers */

            for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount && varDsc->lvIsRegArg;
                 varNum++, varDsc++)
            {
                noway_assert(varDsc->lvIsParam);

                gcInfo.gcMarkRegPtrVal(varDsc->GetArgReg(), varDsc->TypeGet());
            }

            GetEmitter()->emitThisGCrefRegs = GetEmitter()->emitInitGCrefRegs = gcInfo.gcRegGCrefSetCur;
            GetEmitter()->emitThisByrefRegs = GetEmitter()->emitInitByrefRegs = gcInfo.gcRegByrefSetCur;
        }
    }

    genReserveEpilog(block);
}

//------------------------------------------------------------------------
// genJumpToThrowHlpBlk: Generate code for an out-of-line exception.
//
// Notes:
//   For code that uses throw helper blocks, we share the helper blocks created by fgAddCodeRef().
//   Otherwise, we generate the 'throw' inline.
//
// Arguments:
//   jumpKind - jump kind to generate;
//   codeKind - the special throw-helper kind;
//   failBlk  - optional fail target block, if it is already known;
//
void CodeGen::genJumpToThrowHlpBlk(emitJumpKind jumpKind, SpecialCodeKind codeKind, BasicBlock* failBlk)
{
    bool useThrowHlpBlk = compiler->fgUseThrowHelperBlocks();
#if defined(UNIX_X86_ABI) && defined(FEATURE_EH_FUNCLETS)
    // Inline exception-throwing code in funclet to make it possible to unwind funclet frames.
    useThrowHlpBlk = useThrowHlpBlk && (compiler->funCurrentFunc()->funKind == FUNC_ROOT);
#endif // UNIX_X86_ABI && FEATURE_EH_FUNCLETS

    if (useThrowHlpBlk)
    {
        // For code with throw helper blocks, find and use the helper block for
        // raising the exception. The block may be shared by other trees too.

        BasicBlock* excpRaisingBlock;

        if (failBlk != nullptr)
        {
            // We already know which block to jump to. Use that.
            excpRaisingBlock = failBlk;

#ifdef DEBUG
            Compiler::AddCodeDsc* add =
                compiler->fgFindExcptnTarget(codeKind, compiler->bbThrowIndex(compiler->compCurBB));
            assert(excpRaisingBlock == add->acdDstBlk);
#if !FEATURE_FIXED_OUT_ARGS
            assert(add->acdStkLvlInit || isFramePointerUsed());
#endif // !FEATURE_FIXED_OUT_ARGS
#endif // DEBUG
        }
        else
        {
            // Find the helper-block which raises the exception.
            Compiler::AddCodeDsc* add =
                compiler->fgFindExcptnTarget(codeKind, compiler->bbThrowIndex(compiler->compCurBB));
            PREFIX_ASSUME_MSG((add != nullptr), ("ERROR: failed to find exception throw block"));
            excpRaisingBlock = add->acdDstBlk;
#if !FEATURE_FIXED_OUT_ARGS
            assert(add->acdStkLvlInit || isFramePointerUsed());
#endif // !FEATURE_FIXED_OUT_ARGS
        }

        noway_assert(excpRaisingBlock != nullptr);

        // Jump to the exception-throwing block on error.
        inst_JMP(jumpKind, excpRaisingBlock);
    }
    else
    {
        // The code to throw the exception will be generated inline, and
        //  we will jump around it in the normal non-exception case.

        BasicBlock*  tgtBlk          = nullptr;
        emitJumpKind reverseJumpKind = emitter::emitReverseJumpKind(jumpKind);
        if (reverseJumpKind != jumpKind)
        {
            tgtBlk = genCreateTempLabel();
            inst_JMP(reverseJumpKind, tgtBlk);
        }

        genEmitHelperCall(compiler->acdHelper(codeKind), 0, EA_UNKNOWN);

        // Define the spot for the normal non-exception case to jump to.
        if (tgtBlk != nullptr)
        {
            assert(reverseJumpKind != jumpKind);
            genDefineTempLabel(tgtBlk);
        }
    }
}

/*****************************************************************************
 *
 * The last operation done was generating code for "tree" and that would
 * have set the flags. Check if the operation caused an overflow.
 */

// inline
void CodeGen::genCheckOverflow(GenTree* tree)
{
    // Overflow-check should be asked for this tree
    noway_assert(tree->gtOverflow());

    const var_types type = tree->TypeGet();

    // Overflow checks can only occur for the non-small types: (i.e. TYP_INT,TYP_LONG)
    noway_assert(!varTypeIsSmall(type));

    emitJumpKind jumpKind;

#ifdef TARGET_ARM64
    if (tree->OperGet() == GT_MUL)
    {
        jumpKind = EJ_ne;
    }
    else
#endif
    {
        bool isUnsignedOverflow = ((tree->gtFlags & GTF_UNSIGNED) != 0);

#if defined(TARGET_XARCH)

        jumpKind = isUnsignedOverflow ? EJ_jb : EJ_jo;

#elif defined(TARGET_ARMARCH)

        jumpKind = isUnsignedOverflow ? EJ_lo : EJ_vs;

        if (jumpKind == EJ_lo)
        {
            if (tree->OperGet() != GT_SUB)
            {
                jumpKind = EJ_hs;
            }
        }

#endif // defined(TARGET_ARMARCH)
    }

    // Jump to the block which will throw the expection

    genJumpToThrowHlpBlk(jumpKind, SCK_OVERFLOW);
}

#if defined(FEATURE_EH_FUNCLETS)

/*****************************************************************************
 *
 *  Update the current funclet as needed by calling genUpdateCurrentFunclet().
 *  For non-BBF_FUNCLET_BEG blocks, it asserts that the current funclet
 *  is up-to-date.
 *
 */

void CodeGen::genUpdateCurrentFunclet(BasicBlock* block)
{
    if (block->bbFlags & BBF_FUNCLET_BEG)
    {
        compiler->funSetCurrentFunc(compiler->funGetFuncIdx(block));
        if (compiler->funCurrentFunc()->funKind == FUNC_FILTER)
        {
            assert(compiler->ehGetDsc(compiler->funCurrentFunc()->funEHIndex)->ebdFilter == block);
        }
        else
        {
            // We shouldn't see FUNC_ROOT
            assert(compiler->funCurrentFunc()->funKind == FUNC_HANDLER);
            assert(compiler->ehGetDsc(compiler->funCurrentFunc()->funEHIndex)->ebdHndBeg == block);
        }
    }
    else
    {
        assert(compiler->compCurrFuncIdx <= compiler->compFuncInfoCount);
        if (compiler->funCurrentFunc()->funKind == FUNC_FILTER)
        {
            assert(compiler->ehGetDsc(compiler->funCurrentFunc()->funEHIndex)->InFilterRegionBBRange(block));
        }
        else if (compiler->funCurrentFunc()->funKind == FUNC_ROOT)
        {
            assert(!block->hasHndIndex());
        }
        else
        {
            assert(compiler->funCurrentFunc()->funKind == FUNC_HANDLER);
            assert(compiler->ehGetDsc(compiler->funCurrentFunc()->funEHIndex)->InHndRegionBBRange(block));
        }
    }
}

#if defined(TARGET_ARM)
void CodeGen::genInsertNopForUnwinder(BasicBlock* block)
{
    // If this block is the target of a finally return, we need to add a preceding NOP, in the same EH region,
    // so the unwinder doesn't get confused by our "movw lr, xxx; movt lr, xxx; b Lyyy" calling convention that
    // calls the funclet during non-exceptional control flow.
    if (block->bbFlags & BBF_FINALLY_TARGET)
    {
        assert(block->bbFlags & BBF_HAS_LABEL);

#ifdef DEBUG
        if (compiler->verbose)
        {
            printf("\nEmitting finally target NOP predecessor for " FMT_BB "\n", block->bbNum);
        }
#endif
        // Create a label that we'll use for computing the start of an EH region, if this block is
        // at the beginning of such a region. If we used the existing bbEmitCookie as is for
        // determining the EH regions, then this NOP would end up outside of the region, if this
        // block starts an EH region. If we pointed the existing bbEmitCookie here, then the NOP
        // would be executed, which we would prefer not to do.

        block->bbUnwindNopEmitCookie =
            GetEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur,
                                       false DEBUG_ARG(block->bbNum));

        instGen(INS_nop);
    }
}
#endif

#endif // FEATURE_EH_FUNCLETS

//----------------------------------------------------------------------
// genGenerateCode: Generate code for the function.
//
// Arguments:
//     codePtr [OUT] - address of generated code
//     nativeSizeOfCode [OUT] - length of generated code in bytes
//
void CodeGen::genGenerateCode(void** codePtr, uint32_t* nativeSizeOfCode)
{

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genGenerateCode()\n");
        compiler->fgDispBasicBlocks(compiler->verboseTrees);
    }
#endif

    this->codePtr          = codePtr;
    this->nativeSizeOfCode = nativeSizeOfCode;

    DoPhase(this, PHASE_GENERATE_CODE, &CodeGen::genGenerateMachineCode);
    DoPhase(this, PHASE_EMIT_CODE, &CodeGen::genEmitMachineCode);
    DoPhase(this, PHASE_EMIT_GCEH, &CodeGen::genEmitUnwindDebugGCandEH);
}

//----------------------------------------------------------------------
// genGenerateMachineCode -- determine which machine instructions to emit
//
void CodeGen::genGenerateMachineCode()
{
#ifdef DEBUG
    genInterruptibleUsed = true;

    compiler->fgDebugCheckBBlist();
#endif // DEBUG

    /* This is the real thing */

    genPrepForCompiler();

    /* Prepare the emitter */
    GetEmitter()->Init();
#ifdef DEBUG
    VarSetOps::AssignNoCopy(compiler, genTempOldLife, VarSetOps::MakeEmpty(compiler));
#endif

#ifdef DEBUG
    if (compiler->opts.disAsmSpilled && regSet.rsNeededSpillReg)
    {
        compiler->opts.disAsm = true;
    }

    if (compiler->opts.disAsm)
    {
        printf("; Assembly listing for method %s\n", compiler->info.compFullName);

        printf("; Emitting ");

        if (compiler->compCodeOpt() == Compiler::SMALL_CODE)
        {
            printf("SMALL_CODE");
        }
        else if (compiler->compCodeOpt() == Compiler::FAST_CODE)
        {
            printf("FAST_CODE");
        }
        else
        {
            printf("BLENDED_CODE");
        }

        printf(" for ");

        if (compiler->info.genCPU == CPU_X86)
        {
            printf("generic X86 CPU");
        }
        else if (compiler->info.genCPU == CPU_X86_PENTIUM_4)
        {
            printf("Pentium 4");
        }
        else if (compiler->info.genCPU == CPU_X64)
        {
            if (compiler->canUseVexEncoding())
            {
                printf("X64 CPU with AVX");
            }
            else
            {
                printf("X64 CPU with SSE2");
            }
        }
        else if (compiler->info.genCPU == CPU_ARM)
        {
            printf("generic ARM CPU");
        }
        else if (compiler->info.genCPU == CPU_ARM64)
        {
            printf("generic ARM64 CPU");
        }
        else
        {
            printf("unknown architecture");
        }

#if defined(TARGET_WINDOWS)
        printf(" - Windows");
#elif defined(TARGET_UNIX)
        printf(" - Unix");
#endif

        printf("\n");

        if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0))
        {
            printf("; Tier-0 compilation\n");
        }
        else if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TIER1))
        {
            printf("; Tier-1 compilation\n");
        }
        else if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_READYTORUN))
        {
            printf("; ReadyToRun compilation\n");
        }

        if (compiler->opts.IsOSR())
        {
            printf("; OSR variant for entry point 0x%x\n", compiler->info.compILEntry);
        }

        if ((compiler->opts.compFlags & CLFLG_MAXOPT) == CLFLG_MAXOPT)
        {
            printf("; optimized code\n");
        }
        else if (compiler->opts.compDbgCode)
        {
            printf("; debuggable code\n");
        }
        else if (compiler->opts.MinOpts())
        {
            printf("; MinOpts code\n");
        }
        else
        {
            printf("; unknown optimization flags\n");
        }

        if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR))
        {
            printf("; instrumented for collecting profile data\n");
        }
        else if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_BBOPT) && compiler->fgHaveProfileData())
        {
            printf("; optimized using profile data\n");
        }

#if DOUBLE_ALIGN
        if (compiler->genDoubleAlign())
            printf("; double-aligned frame\n");
        else
#endif
            printf("; %s based frame\n", isFramePointerUsed() ? STR_FPBASE : STR_SPBASE);

        if (GetInterruptible())
        {
            printf("; fully interruptible\n");
        }
        else
        {
            printf("; partially interruptible\n");
        }

        if (compiler->fgHaveProfileData())
        {
            printf("; with PGO: edge weights are %s, and fgCalledCount is " FMT_WT "\n",
                   compiler->fgHaveValidEdgeWeights ? "valid" : "invalid", compiler->fgCalledCount);
        }

        if (compiler->fgPgoFailReason != nullptr)
        {
            printf("; %s\n", compiler->fgPgoFailReason);
        }

        if ((compiler->fgPgoInlineePgo + compiler->fgPgoInlineeNoPgo + compiler->fgPgoInlineeNoPgoSingleBlock) > 0)
        {
            printf("; %u inlinees with PGO data; %u single block inlinees; %u inlinees without PGO data\n",
                   compiler->fgPgoInlineePgo, compiler->fgPgoInlineeNoPgoSingleBlock, compiler->fgPgoInlineeNoPgo);
        }

        if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_ALT_JIT))
        {
            printf("; invoked as altjit\n");
        }
    }
#endif // DEBUG

    // We compute the final frame layout before code generation. This is because LSRA
    // has already computed exactly the maximum concurrent number of spill temps of each type that are
    // required during code generation. So, there is nothing left to estimate: we can be precise in the frame
    // layout. This helps us generate smaller code, and allocate, after code generation, a smaller amount of
    // memory from the VM.

    genFinalizeFrame();

    unsigned maxTmpSize = regSet.tmpGetTotalSize(); // This is precise after LSRA has pre-allocated the temps.

    GetEmitter()->emitBegFN(isFramePointerUsed()
#if defined(DEBUG)
                                ,
                            (compiler->compCodeOpt() != Compiler::SMALL_CODE) &&
                                !compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT)
#endif
                                ,
                            maxTmpSize);

    /* Now generate code for the function */
    genCodeForBBlist();

#ifdef DEBUG
    // After code generation, dump the frame layout again. It should be the same as before code generation, if code
    // generation hasn't touched it (it shouldn't!).
    if (verbose)
    {
        compiler->lvaTableDump();
    }
#endif // DEBUG

    /* We can now generate the function prolog and epilog */

    genGeneratePrologsAndEpilogs();

    /* Bind jump distances */

    GetEmitter()->emitJumpDistBind();

#if FEATURE_LOOP_ALIGN
    /* Perform alignment adjustments */

    GetEmitter()->emitLoopAlignAdjustments();
#endif

    /* The code is now complete and final; it should not change after this. */
}

//----------------------------------------------------------------------
// genEmitMachineCode -- emit the actual machine instruction code
//
void CodeGen::genEmitMachineCode()
{
    /* Compute the size of the code sections that we are going to ask the VM
       to allocate. Note that this might not be precisely the size of the
       code we emit, though it's fatal if we emit more code than the size we
       compute here.
       (Note: an example of a case where we emit less code would be useful.)
    */

    GetEmitter()->emitComputeCodeSizes();

#ifdef DEBUG
    unsigned instrCount;

    // Code to test or stress our ability to run a fallback compile.
    // We trigger the fallback here, before asking the VM for any memory,
    // because if not, we will leak mem, as the current codebase can't free
    // the mem after the emitter asks the VM for it. As this is only a stress
    // mode, we only want the functionality, and don't care about the relative
    // ugliness of having the failure here.
    if (!compiler->jitFallbackCompile)
    {
        // Use COMPlus_JitNoForceFallback=1 to prevent NOWAY assert testing from happening,
        // especially that caused by enabling JIT stress.
        if (!JitConfig.JitNoForceFallback())
        {
            if (JitConfig.JitForceFallback() || compiler->compStressCompile(Compiler::STRESS_GENERIC_VARN, 5))
            {
                JITDUMP("\n\n*** forcing no-way fallback -- current jit request will be abandoned ***\n\n");
                NO_WAY_NOASSERT("Stress failure");
            }
        }
    }

#endif // DEBUG

    /* We've finished collecting all the unwind information for the function. Now reserve
       space for it from the VM.
    */

    compiler->unwindReserve();

    bool trackedStackPtrsContig; // are tracked stk-ptrs contiguous ?

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    trackedStackPtrsContig = false;
#elif defined(TARGET_ARM)
    // On arm due to prespilling of arguments, tracked stk-ptrs may not be contiguous
    trackedStackPtrsContig = !compiler->opts.compDbgEnC && !compiler->compIsProfilerHookNeeded();
#else
    trackedStackPtrsContig = !compiler->opts.compDbgEnC;
#endif

    codeSize = GetEmitter()->emitEndCodeGen(compiler, trackedStackPtrsContig, GetInterruptible(),
                                            IsFullPtrRegMapRequired(), compiler->compHndBBtabCount, &prologSize,
                                            &epilogSize, codePtr, &coldCodePtr, &consPtr DEBUGARG(&instrCount));

#ifdef DEBUG
    assert(compiler->compCodeGenDone == false);

    /* We're done generating code for this function */
    compiler->compCodeGenDone = true;
#endif

#if defined(DEBUG) || defined(LATE_DISASM)
    // Add code size information into the Perf Score
    // All compPerfScore calculations must be performed using doubles
    compiler->info.compPerfScore += ((double)compiler->info.compTotalHotCodeSize * (double)PERFSCORE_CODESIZE_COST_HOT);
    compiler->info.compPerfScore +=
        ((double)compiler->info.compTotalColdCodeSize * (double)PERFSCORE_CODESIZE_COST_COLD);
#endif // DEBUG || LATE_DISASM

#ifdef DEBUG
    if (compiler->opts.disAsm || verbose)
    {
        printf("\n; Total bytes of code %d, prolog size %d, PerfScore %.2f, instruction count %d, allocated bytes for "
               "code %d (MethodHash=%08x) for "
               "method %s\n",
               codeSize, prologSize, compiler->info.compPerfScore, instrCount,
               GetEmitter()->emitTotalHotCodeSize + GetEmitter()->emitTotalColdCodeSize,
               compiler->info.compMethodHash(), compiler->info.compFullName);
        printf("; ============================================================\n\n");
        printf(""); // in our logic this causes a flush
    }

    if (verbose)
    {
        printf("*************** After end code gen, before unwindEmit()\n");
        GetEmitter()->emitDispIGlist(true);
    }
#endif

#if EMIT_TRACK_STACK_DEPTH && defined(DEBUG_ARG_SLOTS)
    // Check our max stack level. Needed for fgAddCodeRef().
    // We need to relax the assert as our estimation won't include code-gen
    // stack changes (which we know don't affect fgAddCodeRef()).
    // NOTE: after emitEndCodeGen (including here), emitMaxStackDepth is a
    // count of DWORD-sized arguments, NOT argument size in bytes.
    {
        unsigned maxAllowedStackDepth = compiler->fgGetPtrArgCntMax() + // Max number of pointer-sized stack arguments.
                                        compiler->compHndBBtabCount +   // Return address for locally-called finallys
                                        genTypeStSz(TYP_LONG) + // longs/doubles may be transferred via stack, etc
                                        (compiler->compTailCallUsed ? 4 : 0); // CORINFO_HELP_TAILCALL args
#if defined(UNIX_X86_ABI)
        // Convert maxNestedAlignment to DWORD count before adding to maxAllowedStackDepth.
        assert(maxNestedAlignment % sizeof(int) == 0);
        maxAllowedStackDepth += maxNestedAlignment / sizeof(int);
#endif
        assert(GetEmitter()->emitMaxStackDepth <= maxAllowedStackDepth);
    }
#endif // EMIT_TRACK_STACK_DEPTH && DEBUG

    *nativeSizeOfCode                 = codeSize;
    compiler->info.compNativeCodeSize = (UNATIVE_OFFSET)codeSize;

    // printf("%6u bytes of code generated for %s.%s\n", codeSize, compiler->info.compFullName);

    // Make sure that the x86 alignment and cache prefetch optimization rules
    // were obeyed.

    // Don't start a method in the last 7 bytes of a 16-byte alignment area
    //   unless we are generating SMALL_CODE
    // noway_assert( (((unsigned)(*codePtr) % 16) <= 8) || (compiler->compCodeOpt() == SMALL_CODE));
}

//----------------------------------------------------------------------
// genEmitUnwindDebugGCandEH: emit unwind, debug, gc, and EH info
//
void CodeGen::genEmitUnwindDebugGCandEH()
{
    /* Now that the code is issued, we can finalize and emit the unwind data */

    compiler->unwindEmit(*codePtr, coldCodePtr);

    /* Finalize the line # tracking logic after we know the exact block sizes/offsets */

    genIPmappingGen();

    /* Finalize the Local Var info in terms of generated code */

    genSetScopeInfo();

#if defined(USING_VARIABLE_LIVE_RANGE) && defined(DEBUG)
    if (compiler->verbose)
    {
        varLiveKeeper->dumpLvaVariableLiveRanges();
    }
#endif // defined(USING_VARIABLE_LIVE_RANGE) && defined(DEBUG)

#ifdef LATE_DISASM
    unsigned finalHotCodeSize;
    unsigned finalColdCodeSize;
    if (compiler->fgFirstColdBlock != nullptr)
    {
        // We did some hot/cold splitting. The hot section is always padded out to the
        // size we thought it would be, but the cold section is not.
        assert(codeSize <= compiler->info.compTotalHotCodeSize + compiler->info.compTotalColdCodeSize);
        assert(compiler->info.compTotalHotCodeSize > 0);
        assert(compiler->info.compTotalColdCodeSize > 0);
        finalHotCodeSize  = compiler->info.compTotalHotCodeSize;
        finalColdCodeSize = codeSize - finalHotCodeSize;
    }
    else
    {
        // No hot/cold splitting
        assert(codeSize <= compiler->info.compTotalHotCodeSize);
        assert(compiler->info.compTotalHotCodeSize > 0);
        assert(compiler->info.compTotalColdCodeSize == 0);
        finalHotCodeSize  = codeSize;
        finalColdCodeSize = 0;
    }
    getDisAssembler().disAsmCode((BYTE*)*codePtr, finalHotCodeSize, (BYTE*)coldCodePtr, finalColdCodeSize);
#endif // LATE_DISASM

    /* Report any exception handlers to the VM */

    genReportEH();

#ifdef JIT32_GCENCODER
#ifdef DEBUG
    void* infoPtr =
#endif // DEBUG
#endif
        // Create and store the GC info for this method.
        genCreateAndStoreGCInfo(codeSize, prologSize, epilogSize DEBUGARG(codePtr));

#ifdef DEBUG
    FILE* dmpf = jitstdout;

    compiler->opts.dmpHex = false;
    if (!strcmp(compiler->info.compMethodName, "<name of method you want the hex dump for"))
    {
        FILE*   codf;
        errno_t ec = fopen_s(&codf, "C:\\JIT.COD", "at"); // NOTE: file append mode
        if (ec != 0)
        {
            assert(codf);
            dmpf                  = codf;
            compiler->opts.dmpHex = true;
        }
    }
    if (compiler->opts.dmpHex)
    {
        size_t consSize = GetEmitter()->emitDataSize();

        fprintf(dmpf, "Generated code for %s:\n", compiler->info.compFullName);
        fprintf(dmpf, "\n");

        if (codeSize)
        {
            fprintf(dmpf, "    Code  at %p [%04X bytes]\n", dspPtr(*codePtr), codeSize);
        }
        if (consSize)
        {
            fprintf(dmpf, "    Const at %p [%04X bytes]\n", dspPtr(consPtr), consSize);
        }
#ifdef JIT32_GCENCODER
        size_t infoSize = compiler->compInfoBlkSize;
        if (infoSize)
            fprintf(dmpf, "    Info  at %p [%04X bytes]\n", dspPtr(infoPtr), infoSize);
#endif // JIT32_GCENCODER

        fprintf(dmpf, "\n");

        if (codeSize)
        {
            hexDump(dmpf, "Code", (BYTE*)*codePtr, codeSize);
        }
        if (consSize)
        {
            hexDump(dmpf, "Const", (BYTE*)consPtr, consSize);
        }
#ifdef JIT32_GCENCODER
        if (infoSize)
            hexDump(dmpf, "Info", (BYTE*)infoPtr, infoSize);
#endif // JIT32_GCENCODER

        fflush(dmpf);
    }

    if (dmpf != jitstdout)
    {
        fclose(dmpf);
    }

#endif // DEBUG

    /* Tell the emitter that we're done with this function */

    GetEmitter()->emitEndFN();

    /* Shut down the spill logic */

    regSet.rsSpillDone();

    /* Shut down the temp logic */

    regSet.tmpDone();

#if DISPLAY_SIZES

    size_t dataSize = GetEmitter()->emitDataSize();
    grossVMsize += compiler->info.compILCodeSize;
    totalNCsize += codeSize + dataSize + compiler->compInfoBlkSize;
    grossNCsize += codeSize + dataSize;

#endif // DISPLAY_SIZES
}

/*****************************************************************************
 *
 *  Report EH clauses to the VM
 */

void CodeGen::genReportEH()
{
    if (compiler->compHndBBtabCount == 0)
    {
        return;
    }

#ifdef DEBUG
    if (compiler->opts.dspEHTable)
    {
        printf("*************** EH table for %s\n", compiler->info.compFullName);
    }
#endif // DEBUG

    unsigned  XTnum;
    EHblkDsc* HBtab;
    EHblkDsc* HBtabEnd;

    bool isCoreRTABI = compiler->IsTargetAbi(CORINFO_CORERT_ABI);

    unsigned EHCount = compiler->compHndBBtabCount;

#if defined(FEATURE_EH_FUNCLETS)
    // Count duplicated clauses. This uses the same logic as below, where we actually generate them for reporting to the
    // VM.
    unsigned duplicateClauseCount = 0;
    unsigned enclosingTryIndex;

    // Duplicate clauses are not used by CoreRT ABI
    if (!isCoreRTABI)
    {
        for (XTnum = 0; XTnum < compiler->compHndBBtabCount; XTnum++)
        {
            for (enclosingTryIndex = compiler->ehTrueEnclosingTryIndexIL(XTnum); // find the true enclosing try index,
                                                                                 // ignoring 'mutual protect' trys
                 enclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX;
                 enclosingTryIndex = compiler->ehGetEnclosingTryIndex(enclosingTryIndex))
            {
                ++duplicateClauseCount;
            }
        }
        EHCount += duplicateClauseCount;
    }

#if FEATURE_EH_CALLFINALLY_THUNKS
    unsigned clonedFinallyCount = 0;

    // Duplicate clauses are not used by CoreRT ABI
    if (!isCoreRTABI)
    {
        // We don't keep track of how many cloned finally there are. So, go through and count.
        // We do a quick pass first through the EH table to see if there are any try/finally
        // clauses. If there aren't, we don't need to look for BBJ_CALLFINALLY.

        bool anyFinallys = false;
        for (HBtab = compiler->compHndBBtab, HBtabEnd = compiler->compHndBBtab + compiler->compHndBBtabCount;
             HBtab < HBtabEnd; HBtab++)
        {
            if (HBtab->HasFinallyHandler())
            {
                anyFinallys = true;
                break;
            }
        }
        if (anyFinallys)
        {
            for (BasicBlock* block = compiler->fgFirstBB; block != nullptr; block = block->bbNext)
            {
                if (block->bbJumpKind == BBJ_CALLFINALLY)
                {
                    ++clonedFinallyCount;
                }
            }

            EHCount += clonedFinallyCount;
        }
    }
#endif // FEATURE_EH_CALLFINALLY_THUNKS

#endif // FEATURE_EH_FUNCLETS

#ifdef DEBUG
    if (compiler->opts.dspEHTable)
    {
#if defined(FEATURE_EH_FUNCLETS)
#if FEATURE_EH_CALLFINALLY_THUNKS
        printf("%d EH table entries, %d duplicate clauses, %d cloned finallys, %d total EH entries reported to VM\n",
               compiler->compHndBBtabCount, duplicateClauseCount, clonedFinallyCount, EHCount);
        assert(compiler->compHndBBtabCount + duplicateClauseCount + clonedFinallyCount == EHCount);
#else  // !FEATURE_EH_CALLFINALLY_THUNKS
        printf("%d EH table entries, %d duplicate clauses, %d total EH entries reported to VM\n",
               compiler->compHndBBtabCount, duplicateClauseCount, EHCount);
        assert(compiler->compHndBBtabCount + duplicateClauseCount == EHCount);
#endif // !FEATURE_EH_CALLFINALLY_THUNKS
#else  // !FEATURE_EH_FUNCLETS
        printf("%d EH table entries, %d total EH entries reported to VM\n", compiler->compHndBBtabCount, EHCount);
        assert(compiler->compHndBBtabCount == EHCount);
#endif // !FEATURE_EH_FUNCLETS
    }
#endif // DEBUG

    // Tell the VM how many EH clauses to expect.
    compiler->eeSetEHcount(EHCount);

    XTnum = 0; // This is the index we pass to the VM

    for (HBtab = compiler->compHndBBtab, HBtabEnd = compiler->compHndBBtab + compiler->compHndBBtabCount;
         HBtab < HBtabEnd; HBtab++)
    {
        UNATIVE_OFFSET tryBeg, tryEnd, hndBeg, hndEnd, hndTyp;

        tryBeg = compiler->ehCodeOffset(HBtab->ebdTryBeg);
        hndBeg = compiler->ehCodeOffset(HBtab->ebdHndBeg);

        tryEnd = (HBtab->ebdTryLast == compiler->fgLastBB) ? compiler->info.compNativeCodeSize
                                                           : compiler->ehCodeOffset(HBtab->ebdTryLast->bbNext);
        hndEnd = (HBtab->ebdHndLast == compiler->fgLastBB) ? compiler->info.compNativeCodeSize
                                                           : compiler->ehCodeOffset(HBtab->ebdHndLast->bbNext);

        if (HBtab->HasFilter())
        {
            hndTyp = compiler->ehCodeOffset(HBtab->ebdFilter);
        }
        else
        {
            hndTyp = HBtab->ebdTyp;
        }

        CORINFO_EH_CLAUSE_FLAGS flags = ToCORINFO_EH_CLAUSE_FLAGS(HBtab->ebdHandlerType);

        if (isCoreRTABI && (XTnum > 0))
        {
            // For CoreRT, CORINFO_EH_CLAUSE_SAMETRY flag means that the current clause covers same
            // try block as the previous one. The runtime cannot reliably infer this information from
            // native code offsets because of different try blocks can have same offsets. Alternative
            // solution to this problem would be inserting extra nops to ensure that different try
            // blocks have different offsets.
            if (EHblkDsc::ebdIsSameTry(HBtab, HBtab - 1))
            {
                // The SAMETRY bit should only be set on catch clauses. This is ensured in IL, where only 'catch' is
                // allowed to be mutually-protect. E.g., the C# "try {} catch {} catch {} finally {}" actually exists in
                // IL as "try { try {} catch {} catch {} } finally {}".
                assert(HBtab->HasCatchHandler());
                flags = (CORINFO_EH_CLAUSE_FLAGS)(flags | CORINFO_EH_CLAUSE_SAMETRY);
            }
        }

        // Note that we reuse the CORINFO_EH_CLAUSE type, even though the names of
        // the fields aren't accurate.

        CORINFO_EH_CLAUSE clause;
        clause.ClassToken    = hndTyp; /* filter offset is passed back here for filter-based exception handlers */
        clause.Flags         = flags;
        clause.TryOffset     = tryBeg;
        clause.TryLength     = tryEnd;
        clause.HandlerOffset = hndBeg;
        clause.HandlerLength = hndEnd;

        assert(XTnum < EHCount);

        // Tell the VM about this EH clause.
        compiler->eeSetEHinfo(XTnum, &clause);

        ++XTnum;
    }

#if defined(FEATURE_EH_FUNCLETS)
    // Now output duplicated clauses.
    //
    // If a funclet has been created by moving a handler out of a try region that it was originally nested
    // within, then we need to report a "duplicate" clause representing the fact that an exception in that
    // handler can be caught by the 'try' it has been moved out of. This is because the original 'try' region
    // descriptor can only specify a single, contiguous protected range, but the funclet we've moved out is
    // no longer contiguous with the original 'try' region. The new EH descriptor will have the same handler
    // region as the enclosing try region's handler region. This is the sense in which it is duplicated:
    // there is now a "duplicate" clause with the same handler region as another, but a different 'try'
    // region.
    //
    // For example, consider this (capital letters represent an unknown code sequence, numbers identify a
    // try or handler region):
    //
    // A
    // try (1) {
    //   B
    //   try (2) {
    //     C
    //   } catch (3) {
    //     D
    //   } catch (4) {
    //     E
    //   }
    //   F
    // } catch (5) {
    //   G
    // }
    // H
    //
    // Here, we have try region (1) BCDEF protected by catch (5) G, and region (2) C protected
    // by catch (3) D and catch (4) E. Note that catch (4) E does *NOT* protect the code "D".
    // This is an example of 'mutually protect' regions. First, we move handlers (3) and (4)
    // to the end of the code. However, (3) and (4) are nested inside, and protected by, try (1). Again
    // note that (3) is not nested inside (4), despite ebdEnclosingTryIndex indicating that.
    // The code "D" and "E" won't be contiguous with the protected region for try (1) (which
    // will, after moving catch (3) AND (4), be BCF). Thus, we need to add a new EH descriptor
    // representing try (1) protecting the new funclets catch (3) and (4).
    // The code will be generated as follows:
    //
    // ABCFH // "main" code
    // D // funclet
    // E // funclet
    // G // funclet
    //
    // The EH regions are:
    //
    //  C -> D
    //  C -> E
    //  BCF -> G
    //  D -> G // "duplicate" clause
    //  E -> G // "duplicate" clause
    //
    // Note that we actually need to generate one of these additional "duplicate" clauses for every
    // region the funclet is nested in. Take this example:
    //
    //  A
    //  try (1) {
    //      B
    //      try (2,3) {
    //          C
    //          try (4) {
    //              D
    //              try (5,6) {
    //                  E
    //              } catch {
    //                  F
    //              } catch {
    //                  G
    //              }
    //              H
    //          } catch {
    //              I
    //          }
    //          J
    //      } catch {
    //          K
    //      } catch {
    //          L
    //      }
    //      M
    //  } catch {
    //      N
    //  }
    //  O
    //
    // When we pull out funclets, we get the following generated code:
    //
    // ABCDEHJMO // "main" function
    // F // funclet
    // G // funclet
    // I // funclet
    // K // funclet
    // L // funclet
    // N // funclet
    //
    // And the EH regions we report to the VM are (in order; main clauses
    // first in most-to-least nested order, funclets ("duplicated clauses")
    // last, in most-to-least nested) are:
    //
    //  E -> F
    //  E -> G
    //  DEH -> I
    //  CDEHJ -> K
    //  CDEHJ -> L
    //  BCDEHJM -> N
    //  F -> I // funclet clause #1 for F
    //  F -> K // funclet clause #2 for F
    //  F -> L // funclet clause #3 for F
    //  F -> N // funclet clause #4 for F
    //  G -> I // funclet clause #1 for G
    //  G -> K // funclet clause #2 for G
    //  G -> L // funclet clause #3 for G
    //  G -> N // funclet clause #4 for G
    //  I -> K // funclet clause #1 for I
    //  I -> L // funclet clause #2 for I
    //  I -> N // funclet clause #3 for I
    //  K -> N // funclet clause #1 for K
    //  L -> N // funclet clause #1 for L
    //
    // So whereas the IL had 6 EH clauses, we need to report 19 EH clauses to the VM.
    // Note that due to the nature of 'mutually protect' clauses, it would be incorrect
    // to add a clause "F -> G" because F is NOT protected by G, but we still have
    // both "F -> K" and "F -> L" because F IS protected by both of those handlers.
    //
    // The overall ordering of the clauses is still the same most-to-least nesting
    // after front-to-back start offset. Because we place the funclets at the end
    // these new clauses should also go at the end by this ordering.
    //

    if (duplicateClauseCount > 0)
    {
        unsigned reportedDuplicateClauseCount = 0; // How many duplicated clauses have we reported?
        unsigned XTnum2;
        for (XTnum2 = 0, HBtab = compiler->compHndBBtab; XTnum2 < compiler->compHndBBtabCount; XTnum2++, HBtab++)
        {
            unsigned enclosingTryIndex;

            EHblkDsc* fletTab = compiler->ehGetDsc(XTnum2);

            for (enclosingTryIndex = compiler->ehTrueEnclosingTryIndexIL(XTnum2); // find the true enclosing try index,
                                                                                  // ignoring 'mutual protect' trys
                 enclosingTryIndex != EHblkDsc::NO_ENCLOSING_INDEX;
                 enclosingTryIndex = compiler->ehGetEnclosingTryIndex(enclosingTryIndex))
            {
                // The funclet we moved out is nested in a try region, so create a new EH descriptor for the funclet
                // that will have the enclosing try protecting the funclet.

                noway_assert(XTnum2 < enclosingTryIndex); // the enclosing region must be less nested, and hence have a
                                                          // greater EH table index

                EHblkDsc* encTab = compiler->ehGetDsc(enclosingTryIndex);

                // The try region is the handler of the funclet. Note that for filters, we don't protect the
                // filter region, only the filter handler region. This is because exceptions in filters never
                // escape; the VM swallows them.

                BasicBlock* bbTryBeg  = fletTab->ebdHndBeg;
                BasicBlock* bbTryLast = fletTab->ebdHndLast;

                BasicBlock* bbHndBeg  = encTab->ebdHndBeg; // The handler region is the same as the enclosing try
                BasicBlock* bbHndLast = encTab->ebdHndLast;

                UNATIVE_OFFSET tryBeg, tryEnd, hndBeg, hndEnd, hndTyp;

                tryBeg = compiler->ehCodeOffset(bbTryBeg);
                hndBeg = compiler->ehCodeOffset(bbHndBeg);

                tryEnd = (bbTryLast == compiler->fgLastBB) ? compiler->info.compNativeCodeSize
                                                           : compiler->ehCodeOffset(bbTryLast->bbNext);
                hndEnd = (bbHndLast == compiler->fgLastBB) ? compiler->info.compNativeCodeSize
                                                           : compiler->ehCodeOffset(bbHndLast->bbNext);

                if (encTab->HasFilter())
                {
                    hndTyp = compiler->ehCodeOffset(encTab->ebdFilter);
                }
                else
                {
                    hndTyp = encTab->ebdTyp;
                }

                CORINFO_EH_CLAUSE_FLAGS flags = ToCORINFO_EH_CLAUSE_FLAGS(encTab->ebdHandlerType);

                // Tell the VM this is an extra clause caused by moving funclets out of line.
                flags = (CORINFO_EH_CLAUSE_FLAGS)(flags | CORINFO_EH_CLAUSE_DUPLICATE);

                // Note that the JIT-EE interface reuses the CORINFO_EH_CLAUSE type, even though the names of
                // the fields aren't really accurate. For example, we set "TryLength" to the offset of the
                // instruction immediately after the 'try' body. So, it really could be more accurately named
                // "TryEndOffset".

                CORINFO_EH_CLAUSE clause;
                clause.ClassToken = hndTyp; /* filter offset is passed back here for filter-based exception handlers */
                clause.Flags      = flags;
                clause.TryOffset  = tryBeg;
                clause.TryLength  = tryEnd;
                clause.HandlerOffset = hndBeg;
                clause.HandlerLength = hndEnd;

                assert(XTnum < EHCount);

                // Tell the VM about this EH clause (a duplicated clause).
                compiler->eeSetEHinfo(XTnum, &clause);

                ++XTnum;
                ++reportedDuplicateClauseCount;

#ifndef DEBUG
                if (duplicateClauseCount == reportedDuplicateClauseCount)
                {
                    break; // we've reported all of them; no need to continue looking
                }
#endif // !DEBUG

            } // for each 'true' enclosing 'try'
        }     // for each EH table entry

        assert(duplicateClauseCount == reportedDuplicateClauseCount);
    } // if (duplicateClauseCount > 0)

#if FEATURE_EH_CALLFINALLY_THUNKS
    if (clonedFinallyCount > 0)
    {
        unsigned reportedClonedFinallyCount = 0;
        for (BasicBlock* block = compiler->fgFirstBB; block != nullptr; block = block->bbNext)
        {
            if (block->bbJumpKind == BBJ_CALLFINALLY)
            {
                UNATIVE_OFFSET hndBeg, hndEnd;

                hndBeg = compiler->ehCodeOffset(block);

                // How big is it? The BBJ_ALWAYS has a null bbEmitCookie! Look for the block after, which must be
                // a label or jump target, since the BBJ_CALLFINALLY doesn't fall through.
                BasicBlock* bbLabel = block->bbNext;
                if (block->isBBCallAlwaysPair())
                {
                    bbLabel = bbLabel->bbNext; // skip the BBJ_ALWAYS
                }
                if (bbLabel == nullptr)
                {
                    hndEnd = compiler->info.compNativeCodeSize;
                }
                else
                {
                    assert(bbLabel->bbEmitCookie != nullptr);
                    hndEnd = compiler->ehCodeOffset(bbLabel);
                }

                CORINFO_EH_CLAUSE clause;
                clause.ClassToken = 0; // unused
                clause.Flags      = (CORINFO_EH_CLAUSE_FLAGS)(CORINFO_EH_CLAUSE_FINALLY | CORINFO_EH_CLAUSE_DUPLICATE);
                clause.TryOffset  = hndBeg;
                clause.TryLength  = hndBeg;
                clause.HandlerOffset = hndBeg;
                clause.HandlerLength = hndEnd;

                assert(XTnum < EHCount);

                // Tell the VM about this EH clause (a cloned finally clause).
                compiler->eeSetEHinfo(XTnum, &clause);

                ++XTnum;
                ++reportedClonedFinallyCount;

#ifndef DEBUG
                if (clonedFinallyCount == reportedClonedFinallyCount)
                {
                    break; // we're done; no need to keep looking
                }
#endif        // !DEBUG
            } // block is BBJ_CALLFINALLY
        }     // for each block

        assert(clonedFinallyCount == reportedClonedFinallyCount);
    }  // if (clonedFinallyCount > 0)
#endif // FEATURE_EH_CALLFINALLY_THUNKS

#endif // FEATURE_EH_FUNCLETS

    assert(XTnum == EHCount);
}

//----------------------------------------------------------------------
// genUseOptimizedWriteBarriers: Determine if an optimized write barrier
// helper should be used.
//
// Arguments:
//   wbf - The WriteBarrierForm of the write (GT_STOREIND) that is happening.
//
// Return Value:
//   true if an optimized write barrier helper should be used, false otherwise.
//   Note: only x86 implements register-specific source optimized write
//   barriers currently.
//
bool CodeGenInterface::genUseOptimizedWriteBarriers(GCInfo::WriteBarrierForm wbf)
{
#if defined(TARGET_X86) && NOGC_WRITE_BARRIERS
#ifdef DEBUG
    return (wbf != GCInfo::WBF_NoBarrier_CheckNotHeapInDebug); // This one is always a call to a C++ method.
#else
    return true;
#endif
#else
    return false;
#endif
}

//----------------------------------------------------------------------
// genUseOptimizedWriteBarriers: Determine if an optimized write barrier
// helper should be used.
//
// This has the same functionality as the version of
// genUseOptimizedWriteBarriers that takes a WriteBarrierForm, but avoids
// determining what the required write barrier form is, if possible.
//
// Arguments:
//   tgt - target tree of write (e.g., GT_STOREIND)
//   assignVal - tree with value to write
//
// Return Value:
//   true if an optimized write barrier helper should be used, false otherwise.
//   Note: only x86 implements register-specific source optimized write
//   barriers currently.
//
bool CodeGenInterface::genUseOptimizedWriteBarriers(GenTree* tgt, GenTree* assignVal)
{
#if defined(TARGET_X86) && NOGC_WRITE_BARRIERS
#ifdef DEBUG
    GCInfo::WriteBarrierForm wbf = compiler->codeGen->gcInfo.gcIsWriteBarrierCandidate(tgt, assignVal);
    return (wbf != GCInfo::WBF_NoBarrier_CheckNotHeapInDebug); // This one is always a call to a C++ method.
#else
    return true;
#endif
#else
    return false;
#endif
}

//----------------------------------------------------------------------
// genWriteBarrierHelperForWriteBarrierForm: Given a write node requiring a write
// barrier, and the write barrier form required, determine the helper to call.
//
// Arguments:
//   tgt - target tree of write (e.g., GT_STOREIND)
//   wbf - already computed write barrier form to use
//
// Return Value:
//   Write barrier helper to use.
//
// Note: do not call this function to get an optimized write barrier helper (e.g.,
// for x86).
//
CorInfoHelpFunc CodeGenInterface::genWriteBarrierHelperForWriteBarrierForm(GenTree* tgt, GCInfo::WriteBarrierForm wbf)
{
    noway_assert(tgt->gtOper == GT_STOREIND);

    CorInfoHelpFunc helper = CORINFO_HELP_ASSIGN_REF;

#ifdef DEBUG
    if (wbf == GCInfo::WBF_NoBarrier_CheckNotHeapInDebug)
    {
        helper = CORINFO_HELP_ASSIGN_REF_ENSURE_NONHEAP;
    }
    else
#endif
        if (tgt->gtOper != GT_CLS_VAR)
    {
        if (wbf != GCInfo::WBF_BarrierUnchecked) // This overrides the tests below.
        {
            if (tgt->gtFlags & GTF_IND_TGTANYWHERE)
            {
                helper = CORINFO_HELP_CHECKED_ASSIGN_REF;
            }
            else if (tgt->AsOp()->gtOp1->TypeGet() == TYP_I_IMPL)
            {
                helper = CORINFO_HELP_CHECKED_ASSIGN_REF;
            }
        }
    }
    assert(((helper == CORINFO_HELP_ASSIGN_REF_ENSURE_NONHEAP) && (wbf == GCInfo::WBF_NoBarrier_CheckNotHeapInDebug)) ||
           ((helper == CORINFO_HELP_CHECKED_ASSIGN_REF) &&
            (wbf == GCInfo::WBF_BarrierChecked || wbf == GCInfo::WBF_BarrierUnknown)) ||
           ((helper == CORINFO_HELP_ASSIGN_REF) &&
            (wbf == GCInfo::WBF_BarrierUnchecked || wbf == GCInfo::WBF_BarrierUnknown)));

    return helper;
}

//----------------------------------------------------------------------
// genGCWriteBarrier: Generate a write barrier for a node.
//
// Arguments:
//   tgt - target tree of write (e.g., GT_STOREIND)
//   wbf - already computed write barrier form to use
//
void CodeGen::genGCWriteBarrier(GenTree* tgt, GCInfo::WriteBarrierForm wbf)
{
    CorInfoHelpFunc helper = genWriteBarrierHelperForWriteBarrierForm(tgt, wbf);

#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
    // We classify the "tgt" trees as follows:
    // If "tgt" is of the form (where [ x ] indicates an optional x, and { x1, ..., xn } means "one of the x_i forms"):
    //    IND [-> ADDR -> IND] -> { GT_LCL_VAR, ADD({GT_LCL_VAR}, X), ADD(X, (GT_LCL_VAR)) }
    // then let "v" be the GT_LCL_VAR.
    //   * If "v" is the return buffer argument, classify as CWBKind_RetBuf.
    //   * If "v" is another by-ref argument, classify as CWBKind_ByRefArg.
    //   * Otherwise, classify as CWBKind_OtherByRefLocal.
    // If "tgt" is of the form IND -> ADDR -> GT_LCL_VAR, clasify as CWBKind_AddrOfLocal.
    // Otherwise, classify as CWBKind_Unclassified.

    CheckedWriteBarrierKinds wbKind = CWBKind_Unclassified;
    if (tgt->gtOper == GT_IND)
    {
        GenTree* lcl = NULL;

        GenTree* indArg = tgt->AsOp()->gtOp1;
        if (indArg->gtOper == GT_ADDR && indArg->AsOp()->gtOp1->gtOper == GT_IND)
        {
            indArg = indArg->AsOp()->gtOp1->AsOp()->gtOp1;
        }
        if (indArg->gtOper == GT_LCL_VAR)
        {
            lcl = indArg;
        }
        else if (indArg->gtOper == GT_ADD)
        {
            if (indArg->AsOp()->gtOp1->gtOper == GT_LCL_VAR)
            {
                lcl = indArg->AsOp()->gtOp1;
            }
            else if (indArg->AsOp()->gtOp2->gtOper == GT_LCL_VAR)
            {
                lcl = indArg->AsOp()->gtOp2;
            }
        }
        if (lcl != NULL)
        {
            wbKind          = CWBKind_OtherByRefLocal; // Unclassified local variable.
            unsigned lclNum = lcl->AsLclVar()->GetLclNum();
            if (lclNum == compiler->info.compRetBuffArg)
            {
                wbKind = CWBKind_RetBuf; // Ret buff.  Can happen if the struct exceeds the size limit.
            }
            else
            {
                LclVarDsc* varDsc = &compiler->lvaTable[lclNum];
                if (varDsc->lvIsParam && varDsc->lvType == TYP_BYREF)
                {
                    wbKind = CWBKind_ByRefArg; // Out (or in/out) arg
                }
            }
        }
        else
        {
            // We should have eliminated the barrier for this case.
            assert(!(indArg->gtOper == GT_ADDR && indArg->AsOp()->gtOp1->gtOper == GT_LCL_VAR));
        }
    }

    if (helper == CORINFO_HELP_CHECKED_ASSIGN_REF)
    {
#if 0
#ifdef DEBUG
        // Enable this to sample the unclassified trees.
        static int unclassifiedBarrierSite = 0;
        if (wbKind == CWBKind_Unclassified)
        {
            unclassifiedBarrierSite++;
            printf("unclassifiedBarrierSite = %d:\n", unclassifiedBarrierSite); compiler->gtDispTree(tgt); printf(""); printf("\n");
        }
#endif // DEBUG
#endif // 0
        AddStackLevel(4);
        inst_IV(INS_push, wbKind);
        genEmitHelperCall(helper,
                          4,           // argSize
                          EA_PTRSIZE); // retSize
        SubtractStackLevel(4);
    }
    else
    {
        genEmitHelperCall(helper,
                          0,           // argSize
                          EA_PTRSIZE); // retSize
    }

#else  // !FEATURE_COUNT_GC_WRITE_BARRIERS
    genEmitHelperCall(helper,
                      0,           // argSize
                      EA_PTRSIZE); // retSize
#endif // !FEATURE_COUNT_GC_WRITE_BARRIERS
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Prolog / Epilog                                 XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************
 *
 *  Generates code for moving incoming register arguments to their
 *  assigned location, in the function prolog.
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
void CodeGen::genFnPrologCalleeRegArgs(regNumber xtraReg, bool* pXtraRegClobbered, RegState* regState)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFnPrologCalleeRegArgs() for %s regs\n", regState->rsIsFloat ? "float" : "int");
    }
#endif

    unsigned  argMax;           // maximum argNum value plus 1, (including the RetBuffArg)
    unsigned  argNum;           // current argNum, always in [0..argMax-1]
    unsigned  fixedRetBufIndex; // argNum value used by the fixed return buffer argument (ARM64)
    unsigned  regArgNum;        // index into the regArgTab[] table
    regMaskTP regArgMaskLive = regState->rsCalleeRegArgMaskLiveIn;
    bool      doingFloat     = regState->rsIsFloat;

    // We should be generating the prolog block when we are called
    assert(compiler->compGeneratingProlog);

    // We expect to have some registers of the type we are doing, that are LiveIn, otherwise we don't need to be called.
    noway_assert(regArgMaskLive != 0);

    // If a method has 3 args (and no fixed return buffer) then argMax is 3 and valid indexes are 0,1,2
    // If a method has a fixed return buffer (on ARM64) then argMax gets set to 9 and valid index are 0-8
    //
    // The regArgTab can always have unused entries,
    //    for example if an architecture always increments the arg register number but uses either
    //    an integer register or a floating point register to hold the next argument
    //    then with a mix of float and integer args you could have:
    //
    //    sampleMethod(int i, float x, int j, float y, int k, float z);
    //          r0, r2 and r4 as valid integer arguments with argMax as 5
    //      and f1, f3 and f5 and valid floating point arguments with argMax as 6
    //    The first one is doingFloat==false and the second one is doingFloat==true
    //
    //    If a fixed return buffer (in r8) was also present then the first one would become:
    //          r0, r2, r4 and r8 as valid integer arguments with argMax as 9
    //

    argMax           = regState->rsCalleeRegArgCount;
    fixedRetBufIndex = (unsigned)-1; // Invalid value

    // If necessary we will select a correct xtraReg for circular floating point args later.
    if (doingFloat)
    {
        xtraReg = REG_NA;
        noway_assert(argMax <= MAX_FLOAT_REG_ARG);
    }
    else // we are doing the integer registers
    {
        noway_assert(argMax <= MAX_REG_ARG);
        if (hasFixedRetBuffReg())
        {
            fixedRetBufIndex = theFixedRetBuffArgNum();
            // We have an additional integer register argument when hasFixedRetBuffReg() is true
            argMax = fixedRetBufIndex + 1;
            assert(argMax == (MAX_REG_ARG + 1));
        }
    }

    //
    // Construct a table with the register arguments, for detecting circular and
    // non-circular dependencies between the register arguments. A dependency is when
    // an argument register Rn needs to be moved to register Rm that is also an argument
    // register. The table is constructed in the order the arguments are passed in
    // registers: the first register argument is in regArgTab[0], the second in
    // regArgTab[1], etc. Note that on ARM, a TYP_DOUBLE takes two entries, starting
    // at an even index. The regArgTab is indexed from 0 to argMax - 1.
    // Note that due to an extra argument register for ARM64 (i.e  theFixedRetBuffReg())
    // we have increased the allocated size of the regArgTab[] by one.
    //
    struct regArgElem
    {
        unsigned varNum; // index into compiler->lvaTable[] for this register argument
#if defined(UNIX_AMD64_ABI)
        var_types type;   // the Jit type of this regArgTab entry
#endif                    // defined(UNIX_AMD64_ABI)
        unsigned trashBy; // index into this regArgTab[] table of the register that will be copied to this register.
                          // That is, for regArgTab[x].trashBy = y, argument register number 'y' will be copied to
                          // argument register number 'x'. Only used when circular = true.
        char slot;        // 0 means the register is not used for a register argument
                          // 1 means the first part of a register argument
                          // 2, 3 or 4  means the second,third or fourth part of a multireg argument
        bool stackArg;    // true if the argument gets homed to the stack
        bool writeThru;   // true if the argument gets homed to both stack and register
        bool processed;   // true after we've processed the argument (and it is in its final location)
        bool circular;    // true if this register participates in a circular dependency loop.

#ifdef UNIX_AMD64_ABI

        // For UNIX AMD64 struct passing, the type of the register argument slot can differ from
        // the type of the lclVar in ways that are not ascertainable from lvType.
        // So, for that case we retain the type of the register in the regArgTab.

        var_types getRegType(Compiler* compiler)
        {
            return type; // UNIX_AMD64 implementation
        }

#else // !UNIX_AMD64_ABI

        // In other cases, we simply use the type of the lclVar to determine the type of the register.
        var_types getRegType(Compiler* compiler)
        {
            const LclVarDsc& varDsc = compiler->lvaTable[varNum];
            // Check if this is an HFA register arg and return the HFA type
            if (varDsc.lvIsHfaRegArg())
            {
#if defined(TARGET_WINDOWS)
                // Cannot have hfa types on windows arm targets
                // in vararg methods.
                assert(!compiler->info.compIsVarArgs);
#endif // defined(TARGET_WINDOWS)
                return varDsc.GetHfaType();
            }
            return compiler->mangleVarArgsType(varDsc.lvType);
        }

#endif // !UNIX_AMD64_ABI
    } regArgTab[max(MAX_REG_ARG + 1, MAX_FLOAT_REG_ARG)] = {};

    unsigned   varNum;
    LclVarDsc* varDsc;

    for (varNum = 0; varNum < compiler->lvaCount; ++varNum)
    {
        varDsc = compiler->lvaTable + varNum;

        // Is this variable a register arg?
        if (!varDsc->lvIsParam)
        {
            continue;
        }

        if (!varDsc->lvIsRegArg)
        {
            continue;
        }

        // When we have a promoted struct we have two possible LclVars that can represent the incoming argument
        // in the regArgTab[], either the original TYP_STRUCT argument or the introduced lvStructField.
        // We will use the lvStructField if we have a TYPE_INDEPENDENT promoted struct field otherwise
        // use the the original TYP_STRUCT argument.
        //
        if (varDsc->lvPromoted || varDsc->lvIsStructField)
        {
            LclVarDsc* parentVarDsc = varDsc;
            if (varDsc->lvIsStructField)
            {
                assert(!varDsc->lvPromoted);
                parentVarDsc = &compiler->lvaTable[varDsc->lvParentLcl];
            }

            Compiler::lvaPromotionType promotionType = compiler->lvaGetPromotionType(parentVarDsc);

            if (promotionType == Compiler::PROMOTION_TYPE_INDEPENDENT)
            {
                // For register arguments that are independent promoted structs we put the promoted field varNum in the
                // regArgTab[]
                if (varDsc->lvPromoted)
                {
                    continue;
                }
            }
            else
            {
                // For register arguments that are not independent promoted structs we put the parent struct varNum in
                // the regArgTab[]
                if (varDsc->lvIsStructField)
                {
                    continue;
                }
            }
        }

        var_types regType = compiler->mangleVarArgsType(varDsc->TypeGet());
        // Change regType to the HFA type when we have a HFA argument
        if (varDsc->lvIsHfaRegArg())
        {
#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
            if (compiler->info.compIsVarArgs)
            {
                assert(!"Illegal incoming HFA arg encountered in Vararg method.");
            }
#endif // defined(TARGET_WINDOWS) && defined(TARGET_ARM64)
            regType = varDsc->GetHfaType();
        }

#if defined(UNIX_AMD64_ABI)
        if (!varTypeIsStruct(regType))
#endif // defined(UNIX_AMD64_ABI)
        {
            // A struct might be passed  partially in XMM register for System V calls.
            // So a single arg might use both register files.
            if (emitter::isFloatReg(varDsc->GetArgReg()) != doingFloat)
            {
                continue;
            }
        }

        int slots = 0;

#if defined(UNIX_AMD64_ABI)
        if (varTypeIsStruct(varDsc))
        {
            CORINFO_CLASS_HANDLE typeHnd = varDsc->GetStructHnd();
            assert(typeHnd != nullptr);
            SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
            compiler->eeGetSystemVAmd64PassStructInRegisterDescriptor(typeHnd, &structDesc);
            if (!structDesc.passedInRegisters)
            {
                // The var is not passed in registers.
                continue;
            }

            unsigned firstRegSlot = 0;
            for (unsigned slotCounter = 0; slotCounter < structDesc.eightByteCount; slotCounter++)
            {
                regNumber regNum = varDsc->lvRegNumForSlot(slotCounter);
                var_types regType;

#ifdef FEATURE_SIMD
                // Assumption 1:
                // RyuJit backend depends on the assumption that on 64-Bit targets Vector3 size is rounded off
                // to TARGET_POINTER_SIZE and hence Vector3 locals on stack can be treated as TYP_SIMD16 for
                // reading and writing purposes.  Hence while homing a Vector3 type arg on stack we should
                // home entire 16-bytes so that the upper-most 4-bytes will be zeroed when written to stack.
                //
                // Assumption 2:
                // RyuJit backend is making another implicit assumption that Vector3 type args when passed in
                // registers or on stack, the upper most 4-bytes will be zero.
                //
                // For P/Invoke return and Reverse P/Invoke argument passing, native compiler doesn't guarantee
                // that upper 4-bytes of a Vector3 type struct is zero initialized and hence assumption 2 is
                // invalid.
                //
                // RyuJIT x64 Windows: arguments are treated as passed by ref and hence read/written just 12
                // bytes. In case of Vector3 returns, Caller allocates a zero initialized Vector3 local and
                // passes it retBuf arg and Callee method writes only 12 bytes to retBuf. For this reason,
                // there is no need to clear upper 4-bytes of Vector3 type args.
                //
                // RyuJIT x64 Unix: arguments are treated as passed by value and read/writen as if TYP_SIMD16.
                // Vector3 return values are returned two return registers and Caller assembles them into a
                // single xmm reg. Hence RyuJIT explicitly generates code to clears upper 4-bytes of Vector3
                // type args in prolog and Vector3 type return value of a call

                if (varDsc->lvType == TYP_SIMD12)
                {
                    regType = TYP_DOUBLE;
                }
                else
#endif
                {
                    regType = compiler->GetEightByteType(structDesc, slotCounter);
                }

                regArgNum = genMapRegNumToRegArgNum(regNum, regType);

                if ((!doingFloat && (structDesc.IsIntegralSlot(slotCounter))) ||
                    (doingFloat && (structDesc.IsSseSlot(slotCounter))))
                {
                    // Store the reg for the first slot.
                    if (slots == 0)
                    {
                        firstRegSlot = regArgNum;
                    }

                    // Bingo - add it to our table
                    noway_assert(regArgNum < argMax);
                    noway_assert(regArgTab[regArgNum].slot == 0); // we better not have added it already (there better
                                                                  // not be multiple vars representing this argument
                                                                  // register)
                    regArgTab[regArgNum].varNum = varNum;
                    regArgTab[regArgNum].slot   = (char)(slotCounter + 1);
                    regArgTab[regArgNum].type   = regType;
                    slots++;
                }
            }

            if (slots == 0)
            {
                continue; // Nothing to do for this regState set.
            }

            regArgNum = firstRegSlot;
        }
        else
#endif // defined(UNIX_AMD64_ABI)
        {
            // Bingo - add it to our table
            regArgNum = genMapRegNumToRegArgNum(varDsc->GetArgReg(), regType);

            noway_assert(regArgNum < argMax);
            // We better not have added it already (there better not be multiple vars representing this argument
            // register)
            noway_assert(regArgTab[regArgNum].slot == 0);

#if defined(UNIX_AMD64_ABI)
            // Set the register type.
            regArgTab[regArgNum].type = regType;
#endif // defined(UNIX_AMD64_ABI)

            regArgTab[regArgNum].varNum = varNum;
            regArgTab[regArgNum].slot   = 1;

            slots = 1;

#if FEATURE_MULTIREG_ARGS
            if (compiler->lvaIsMultiregStruct(varDsc, compiler->info.compIsVarArgs))
            {
                if (varDsc->lvIsHfaRegArg())
                {
                    // We have an HFA argument, set slots to the number of registers used
                    slots = varDsc->lvHfaSlots();
                }
                else
                {
                    // Currently all non-HFA multireg structs are two registers in size (i.e. two slots)
                    assert(varDsc->lvSize() == (2 * TARGET_POINTER_SIZE));
                    // We have a non-HFA multireg argument, set slots to two
                    slots = 2;
                }

                // Note that regArgNum+1 represents an argument index not an actual argument register.
                // see genMapRegArgNumToRegNum(unsigned argNum, var_types type)

                // This is the setup for the rest of a multireg struct arg

                for (int i = 1; i < slots; i++)
                {
                    noway_assert((regArgNum + i) < argMax);

                    // We better not have added it already (there better not be multiple vars representing this argument
                    // register)
                    noway_assert(regArgTab[regArgNum + i].slot == 0);

                    regArgTab[regArgNum + i].varNum = varNum;
                    regArgTab[regArgNum + i].slot   = (char)(i + 1);
                }
            }
#endif // FEATURE_MULTIREG_ARGS
        }

#ifdef TARGET_ARM
        int lclSize = compiler->lvaLclSize(varNum);

        if (lclSize > REGSIZE_BYTES)
        {
            unsigned maxRegArgNum = doingFloat ? MAX_FLOAT_REG_ARG : MAX_REG_ARG;
            slots                 = lclSize / REGSIZE_BYTES;
            if (regArgNum + slots > maxRegArgNum)
            {
                slots = maxRegArgNum - regArgNum;
            }
        }
        C_ASSERT((char)MAX_REG_ARG == MAX_REG_ARG);
        assert(slots < INT8_MAX);
        for (char i = 1; i < slots; i++)
        {
            regArgTab[regArgNum + i].varNum = varNum;
            regArgTab[regArgNum + i].slot   = i + 1;
        }
#endif // TARGET_ARM

        for (int i = 0; i < slots; i++)
        {
            regType          = regArgTab[regArgNum + i].getRegType(compiler);
            regNumber regNum = genMapRegArgNumToRegNum(regArgNum + i, regType);

#if !defined(UNIX_AMD64_ABI)
            assert((i > 0) || (regNum == varDsc->GetArgReg()));
#endif // defined(UNIX_AMD64_ABI)

            // Is the arg dead on entry to the method ?

            if ((regArgMaskLive & genRegMask(regNum)) == 0)
            {
                if (varDsc->lvTrackedNonStruct())
                {
                    // We may now see some tracked locals with zero refs.
                    // See Lowering::DoPhase. Tolerate these.
                    if (varDsc->lvRefCnt() > 0)
                    {
                        noway_assert(!VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex));
                    }
                }
                else
                {
#ifdef TARGET_X86
                    noway_assert(varDsc->lvType == TYP_STRUCT);
#else  // !TARGET_X86
                    // For LSRA, it may not be in regArgMaskLive if it has a zero
                    // refcnt.  This is in contrast with the non-LSRA case in which all
                    // non-tracked args are assumed live on entry.
                    noway_assert((varDsc->lvRefCnt() == 0) || (varDsc->lvType == TYP_STRUCT) ||
                                 (varDsc->lvAddrExposed && compiler->info.compIsVarArgs) ||
                                 (varDsc->lvAddrExposed && compiler->opts.compUseSoftFP));
#endif // !TARGET_X86
                }
                // Mark it as processed and be done with it
                regArgTab[regArgNum + i].processed = true;
                goto NON_DEP;
            }

#ifdef TARGET_ARM
            // On the ARM when the varDsc is a struct arg (or pre-spilled due to varargs) the initReg/xtraReg
            // could be equal to GetArgReg(). The pre-spilled registers are also not considered live either since
            // they've already been spilled.
            //
            if ((regSet.rsMaskPreSpillRegs(false) & genRegMask(regNum)) == 0)
#endif // TARGET_ARM
            {
#if !defined(UNIX_AMD64_ABI)
                noway_assert(xtraReg != (varDsc->GetArgReg() + i));
#endif
                noway_assert(regArgMaskLive & genRegMask(regNum));
            }

            regArgTab[regArgNum + i].processed = false;
            regArgTab[regArgNum + i].writeThru = (varDsc->lvIsInReg() && varDsc->lvLiveInOutOfHndlr);

            /* mark stack arguments since we will take care of those first */
            regArgTab[regArgNum + i].stackArg = (varDsc->lvIsInReg()) ? false : true;

            /* If it goes on the stack or in a register that doesn't hold
             * an argument anymore -> CANNOT form a circular dependency */

            if (varDsc->lvIsInReg() && (genRegMask(regNum) & regArgMaskLive))
            {
                /* will trash another argument -> possible dependency
                 * We may need several passes after the table is constructed
                 * to decide on that */

                /* Maybe the argument stays in the register (IDEAL) */

                if ((i == 0) && (varDsc->GetRegNum() == regNum))
                {
                    goto NON_DEP;
                }

#if !defined(TARGET_64BIT)
                if ((i == 1) && varTypeIsStruct(varDsc) && (varDsc->GetOtherReg() == regNum))
                {
                    goto NON_DEP;
                }
                if ((i == 1) && (genActualType(varDsc->TypeGet()) == TYP_LONG) && (varDsc->GetOtherReg() == regNum))
                {
                    goto NON_DEP;
                }

                if ((i == 1) && (genActualType(varDsc->TypeGet()) == TYP_DOUBLE) &&
                    (REG_NEXT(varDsc->GetRegNum()) == regNum))
                {
                    goto NON_DEP;
                }
#endif // !defined(TARGET_64BIT)
                regArgTab[regArgNum + i].circular = true;
            }
            else
            {
            NON_DEP:
                regArgTab[regArgNum + i].circular = false;

                /* mark the argument register as free */
                regArgMaskLive &= ~genRegMask(regNum);
            }
        }
    }

    /* Find the circular dependencies for the argument registers, if any.
     * A circular dependency is a set of registers R1, R2, ..., Rn
     * such that R1->R2 (that is, R1 needs to be moved to R2), R2->R3, ..., Rn->R1 */

    bool change = true;
    if (regArgMaskLive)
    {
        /* Possible circular dependencies still exist; the previous pass was not enough
         * to filter them out. Use a "sieve" strategy to find all circular dependencies. */

        while (change)
        {
            change = false;

            for (argNum = 0; argNum < argMax; argNum++)
            {
                // If we already marked the argument as non-circular then continue

                if (!regArgTab[argNum].circular)
                {
                    continue;
                }

                if (regArgTab[argNum].slot == 0) // Not a register argument
                {
                    continue;
                }

                varNum = regArgTab[argNum].varNum;
                noway_assert(varNum < compiler->lvaCount);
                varDsc = compiler->lvaTable + varNum;
                noway_assert(varDsc->lvIsParam && varDsc->lvIsRegArg);

                /* cannot possibly have stack arguments */
                noway_assert(varDsc->lvIsInReg());
                noway_assert(!regArgTab[argNum].stackArg);

                var_types regType = regArgTab[argNum].getRegType(compiler);
                regNumber regNum  = genMapRegArgNumToRegNum(argNum, regType);

                regNumber destRegNum = REG_NA;
                if (varTypeIsStruct(varDsc) &&
                    (compiler->lvaGetPromotionType(varDsc) == Compiler::PROMOTION_TYPE_INDEPENDENT))
                {
                    assert(regArgTab[argNum].slot <= varDsc->lvFieldCnt);
                    LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(varDsc->lvFieldLclStart + regArgTab[argNum].slot - 1);
                    destRegNum             = fieldVarDsc->GetRegNum();
                }
                else if (regArgTab[argNum].slot == 1)
                {
                    destRegNum = varDsc->GetRegNum();
                }
#if defined(TARGET_ARM64) && defined(FEATURE_SIMD)
                else if (varDsc->lvIsHfa())
                {
                    // This must be a SIMD type that's fully enregistered, but is passed as an HFA.
                    // Each field will be inserted into the same destination register.
                    assert(varTypeIsSIMD(varDsc) &&
                           !compiler->isOpaqueSIMDType(varDsc->lvVerTypeInfo.GetClassHandle()));
                    assert(regArgTab[argNum].slot <= (int)varDsc->lvHfaSlots());
                    assert(argNum > 0);
                    assert(regArgTab[argNum - 1].varNum == varNum);
                    regArgMaskLive &= ~genRegMask(regNum);
                    regArgTab[argNum].circular = false;
                    change                     = true;
                    continue;
                }
#elif defined(UNIX_AMD64_ABI) && defined(FEATURE_SIMD)
                else
                {
                    assert(regArgTab[argNum].slot == 2);
                    assert(argNum > 0);
                    assert(regArgTab[argNum - 1].slot == 1);
                    assert(regArgTab[argNum - 1].varNum == varNum);
                    assert((varDsc->lvType == TYP_SIMD12) || (varDsc->lvType == TYP_SIMD16));
                    regArgMaskLive &= ~genRegMask(regNum);
                    regArgTab[argNum].circular = false;
                    change                     = true;
                    continue;
                }
#endif // defined(UNIX_AMD64_ABI) && defined(FEATURE_SIMD)
#if !defined(TARGET_64BIT)
                else if (regArgTab[argNum].slot == 2 && genActualType(varDsc->TypeGet()) == TYP_LONG)
                {
                    destRegNum = varDsc->GetOtherReg();
                }
                else
                {
                    assert(regArgTab[argNum].slot == 2);
                    assert(varDsc->TypeGet() == TYP_DOUBLE);
                    destRegNum = REG_NEXT(varDsc->GetRegNum());
                }
#endif // !defined(TARGET_64BIT)
                noway_assert(destRegNum != REG_NA);
                if (genRegMask(destRegNum) & regArgMaskLive)
                {
                    /* we are trashing a live argument register - record it */
                    unsigned destRegArgNum = genMapRegNumToRegArgNum(destRegNum, regType);
                    noway_assert(destRegArgNum < argMax);
                    regArgTab[destRegArgNum].trashBy = argNum;
                }
                else
                {
                    /* argument goes to a free register */
                    regArgTab[argNum].circular = false;
                    change                     = true;

                    /* mark the argument register as free */
                    regArgMaskLive &= ~genRegMask(regNum);
                }
            }
        }
    }

    /* At this point, everything that has the "circular" flag
     * set to "true" forms a circular dependency */
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    if (regArgMaskLive)
    {
        if (verbose)
        {
            printf("Circular dependencies found while home-ing the incoming arguments.\n");
        }
    }
#endif

    // LSRA allocates registers to incoming parameters in order and will not overwrite
    // a register still holding a live parameter.

    noway_assert(((regArgMaskLive & RBM_FLTARG_REGS) == 0) &&
                 "Homing of float argument registers with circular dependencies not implemented.");

    // Now move the arguments to their locations.
    // First consider ones that go on the stack since they may free some registers.
    // Also home writeThru args, since they're also homed to the stack.

    regArgMaskLive = regState->rsCalleeRegArgMaskLiveIn; // reset the live in to what it was at the start
    for (argNum = 0; argNum < argMax; argNum++)
    {
        emitAttr size;

#if defined(UNIX_AMD64_ABI)
        // If this is the wrong register file, just continue.
        if (regArgTab[argNum].type == TYP_UNDEF)
        {
            // This could happen if the reg in regArgTab[argNum] is of the other register file -
            //     for System V register passed structs where the first reg is GPR and the second an XMM reg.
            // The next register file processing will process it.
            continue;
        }
#endif // defined(UNIX_AMD64_ABI)

        // If the arg is dead on entry to the method, skip it

        if (regArgTab[argNum].processed)
        {
            continue;
        }

        if (regArgTab[argNum].slot == 0) // Not a register argument
        {
            continue;
        }

        varNum = regArgTab[argNum].varNum;
        noway_assert(varNum < compiler->lvaCount);
        varDsc = compiler->lvaTable + varNum;

#ifndef TARGET_64BIT
        // If this arg is never on the stack, go to the next one.
        if (varDsc->lvType == TYP_LONG)
        {
            if (regArgTab[argNum].slot == 1 && !regArgTab[argNum].stackArg && !regArgTab[argNum].writeThru)
            {
                continue;
            }
            else if (varDsc->GetOtherReg() != REG_STK)
            {
                continue;
            }
        }
        else
#endif // !TARGET_64BIT
        {
            // If this arg is never on the stack, go to the next one.
            if (!regArgTab[argNum].stackArg && !regArgTab[argNum].writeThru)
            {
                continue;
            }
        }

#if defined(TARGET_ARM)
        if (varDsc->lvType == TYP_DOUBLE)
        {
            if (regArgTab[argNum].slot == 2)
            {
                // We handled the entire double when processing the first half (slot == 1)
                continue;
            }
        }
#endif

        noway_assert(regArgTab[argNum].circular == false);

        noway_assert(varDsc->lvIsParam);
        noway_assert(varDsc->lvIsRegArg);
        noway_assert(varDsc->lvIsInReg() == false || varDsc->lvLiveInOutOfHndlr ||
                     (varDsc->lvType == TYP_LONG && varDsc->GetOtherReg() == REG_STK && regArgTab[argNum].slot == 2));

        var_types storeType = TYP_UNDEF;
        unsigned  slotSize  = TARGET_POINTER_SIZE;

        if (varTypeIsStruct(varDsc))
        {
            storeType = TYP_I_IMPL; // Default store type for a struct type is a pointer sized integer
#if FEATURE_MULTIREG_ARGS
            // Must be <= MAX_PASS_MULTIREG_BYTES or else it wouldn't be passed in registers
            noway_assert(varDsc->lvSize() <= MAX_PASS_MULTIREG_BYTES);
#endif // FEATURE_MULTIREG_ARGS
#ifdef UNIX_AMD64_ABI
            storeType = regArgTab[argNum].type;
#endif // !UNIX_AMD64_ABI
            if (varDsc->lvIsHfaRegArg())
            {
#ifdef TARGET_ARM
                // On ARM32 the storeType for HFA args is always TYP_FLOAT
                storeType = TYP_FLOAT;
                slotSize  = (unsigned)emitActualTypeSize(storeType);
#else  // TARGET_ARM64
                storeType = genActualType(varDsc->GetHfaType());
                slotSize  = (unsigned)emitActualTypeSize(storeType);
#endif // TARGET_ARM64
            }
        }
        else // Not a struct type
        {
            storeType = compiler->mangleVarArgsType(genActualType(varDsc->TypeGet()));
        }
        size = emitActualTypeSize(storeType);
#ifdef TARGET_X86
        noway_assert(genTypeSize(storeType) == TARGET_POINTER_SIZE);
#endif // TARGET_X86

        regNumber srcRegNum = genMapRegArgNumToRegNum(argNum, storeType);

        // Stack argument - if the ref count is 0 don't care about it

        if (!varDsc->lvOnFrame)
        {
            noway_assert(varDsc->lvRefCnt() == 0);
        }
        else
        {
            // Since slot is typically 1, baseOffset is typically 0
            int baseOffset = (regArgTab[argNum].slot - 1) * slotSize;

            GetEmitter()->emitIns_S_R(ins_Store(storeType), size, srcRegNum, varNum, baseOffset);

#ifndef UNIX_AMD64_ABI
            // Check if we are writing past the end of the struct
            if (varTypeIsStruct(varDsc))
            {
                assert(varDsc->lvSize() >= baseOffset + (unsigned)size);
            }
#endif // !UNIX_AMD64_ABI
#ifdef USING_SCOPE_INFO
            if (regArgTab[argNum].slot == 1)
            {
                psiMoveToStack(varNum);
            }
#endif // USING_SCOPE_INFO
        }

        // Mark the argument as processed, and set it as no longer live in srcRegNum,
        // unless it is a writeThru var, in which case we home it to the stack, but
        // don't mark it as processed until below.
        if (!regArgTab[argNum].writeThru)
        {
            regArgTab[argNum].processed = true;
            regArgMaskLive &= ~genRegMask(srcRegNum);
        }

#if defined(TARGET_ARM)
        if ((storeType == TYP_DOUBLE) && !regArgTab[argNum].writeThru)
        {
            regArgTab[argNum + 1].processed = true;
            regArgMaskLive &= ~genRegMask(REG_NEXT(srcRegNum));
        }
#endif
    }

    /* Process any circular dependencies */
    if (regArgMaskLive)
    {
        unsigned    begReg, destReg, srcReg;
        unsigned    varNumDest, varNumSrc;
        LclVarDsc*  varDscDest;
        LclVarDsc*  varDscSrc;
        instruction insCopy = INS_mov;

        if (doingFloat)
        {
#ifndef UNIX_AMD64_ABI
            if (GlobalJitOptions::compFeatureHfa)
#endif // !UNIX_AMD64_ABI
            {
                insCopy = ins_Copy(TYP_DOUBLE);
                // Compute xtraReg here when we have a float argument
                assert(xtraReg == REG_NA);

                regMaskTP fpAvailMask;

                fpAvailMask = RBM_FLT_CALLEE_TRASH & ~regArgMaskLive;
                if (GlobalJitOptions::compFeatureHfa)
                {
                    fpAvailMask &= RBM_ALLDOUBLE;
                }

                if (fpAvailMask == RBM_NONE)
                {
                    fpAvailMask = RBM_ALLFLOAT & ~regArgMaskLive;
                    if (GlobalJitOptions::compFeatureHfa)
                    {
                        fpAvailMask &= RBM_ALLDOUBLE;
                    }
                }

                assert(fpAvailMask != RBM_NONE);

                // We pick the lowest avail register number
                regMaskTP tempMask = genFindLowestBit(fpAvailMask);
                xtraReg            = genRegNumFromMask(tempMask);
            }
#if defined(TARGET_X86)
            // This case shouldn't occur on x86 since NYI gets converted to an assert
            NYI("Homing circular FP registers via xtraReg");
#endif
        }

        for (argNum = 0; argNum < argMax; argNum++)
        {
            // If not a circular dependency then continue
            if (!regArgTab[argNum].circular)
            {
                continue;
            }

            // If already processed the dependency then continue

            if (regArgTab[argNum].processed)
            {
                continue;
            }

            if (regArgTab[argNum].slot == 0) // Not a register argument
            {
                continue;
            }

            destReg = begReg = argNum;
            srcReg           = regArgTab[argNum].trashBy;

            varNumDest = regArgTab[destReg].varNum;
            noway_assert(varNumDest < compiler->lvaCount);
            varDscDest = compiler->lvaTable + varNumDest;
            noway_assert(varDscDest->lvIsParam && varDscDest->lvIsRegArg);

            noway_assert(srcReg < argMax);
            varNumSrc = regArgTab[srcReg].varNum;
            noway_assert(varNumSrc < compiler->lvaCount);
            varDscSrc = compiler->lvaTable + varNumSrc;
            noway_assert(varDscSrc->lvIsParam && varDscSrc->lvIsRegArg);

            emitAttr size = EA_PTRSIZE;

#ifdef TARGET_XARCH
            //
            // The following code relies upon the target architecture having an
            // 'xchg' instruction which directly swaps the values held in two registers.
            // On the ARM architecture we do not have such an instruction.
            //
            if (destReg == regArgTab[srcReg].trashBy)
            {
                /* only 2 registers form the circular dependency - use "xchg" */

                varNum = regArgTab[argNum].varNum;
                noway_assert(varNum < compiler->lvaCount);
                varDsc = compiler->lvaTable + varNum;
                noway_assert(varDsc->lvIsParam && varDsc->lvIsRegArg);

                noway_assert(genTypeSize(genActualType(varDscSrc->TypeGet())) <= REGSIZE_BYTES);

                /* Set "size" to indicate GC if one and only one of
                 * the operands is a pointer
                 * RATIONALE: If both are pointers, nothing changes in
                 * the GC pointer tracking. If only one is a pointer we
                 * have to "swap" the registers in the GC reg pointer mask
                 */

                if (varTypeGCtype(varDscSrc->TypeGet()) != varTypeGCtype(varDscDest->TypeGet()))
                {
                    size = EA_GCREF;
                }

                noway_assert(varDscDest->GetArgReg() == varDscSrc->GetRegNum());

                GetEmitter()->emitIns_R_R(INS_xchg, size, varDscSrc->GetRegNum(), varDscSrc->GetArgReg());
                regSet.verifyRegUsed(varDscSrc->GetRegNum());
                regSet.verifyRegUsed(varDscSrc->GetArgReg());

                /* mark both arguments as processed */
                regArgTab[destReg].processed = true;
                regArgTab[srcReg].processed  = true;

                regArgMaskLive &= ~genRegMask(varDscSrc->GetArgReg());
                regArgMaskLive &= ~genRegMask(varDscDest->GetArgReg());
#ifdef USING_SCOPE_INFO
                psiMoveToReg(varNumSrc);
                psiMoveToReg(varNumDest);
#endif // USING_SCOPE_INFO
            }
            else
#endif // TARGET_XARCH
            {
                var_types destMemType = varDscDest->TypeGet();

#ifdef TARGET_ARM
                bool cycleAllDouble = true; // assume the best

                unsigned iter = begReg;
                do
                {
                    if (compiler->lvaTable[regArgTab[iter].varNum].TypeGet() != TYP_DOUBLE)
                    {
                        cycleAllDouble = false;
                        break;
                    }
                    iter = regArgTab[iter].trashBy;
                } while (iter != begReg);

                // We may treat doubles as floats for ARM because we could have partial circular
                // dependencies of a float with a lo/hi part of the double. We mark the
                // trashBy values for each slot of the double, so let the circular dependency
                // logic work its way out for floats rather than doubles. If a cycle has all
                // doubles, then optimize so that instead of two vmov.f32's to move a double,
                // we can use one vmov.f64.
                //
                if (!cycleAllDouble && destMemType == TYP_DOUBLE)
                {
                    destMemType = TYP_FLOAT;
                }
#endif // TARGET_ARM

                if (destMemType == TYP_REF)
                {
                    size = EA_GCREF;
                }
                else if (destMemType == TYP_BYREF)
                {
                    size = EA_BYREF;
                }
                else if (destMemType == TYP_DOUBLE)
                {
                    size = EA_8BYTE;
                }
                else if (destMemType == TYP_FLOAT)
                {
                    size = EA_4BYTE;
                }

                /* move the dest reg (begReg) in the extra reg */

                assert(xtraReg != REG_NA);

                regNumber begRegNum = genMapRegArgNumToRegNum(begReg, destMemType);

                GetEmitter()->emitIns_R_R(insCopy, size, xtraReg, begRegNum);

                regSet.verifyRegUsed(xtraReg);

                *pXtraRegClobbered = true;
#ifdef USING_SCOPE_INFO
                psiMoveToReg(varNumDest, xtraReg);
#endif // USING_SCOPE_INFO
                /* start moving everything to its right place */

                while (srcReg != begReg)
                {
                    /* mov dest, src */

                    regNumber destRegNum = genMapRegArgNumToRegNum(destReg, destMemType);
                    regNumber srcRegNum  = genMapRegArgNumToRegNum(srcReg, destMemType);

                    GetEmitter()->emitIns_R_R(insCopy, size, destRegNum, srcRegNum);

                    regSet.verifyRegUsed(destRegNum);

                    /* mark 'src' as processed */
                    noway_assert(srcReg < argMax);
                    regArgTab[srcReg].processed = true;
#ifdef TARGET_ARM
                    if (size == EA_8BYTE)
                        regArgTab[srcReg + 1].processed = true;
#endif
                    regArgMaskLive &= ~genMapArgNumToRegMask(srcReg, destMemType);

                    /* move to the next pair */
                    destReg = srcReg;
                    srcReg  = regArgTab[srcReg].trashBy;

                    varDscDest  = varDscSrc;
                    destMemType = varDscDest->TypeGet();
#ifdef TARGET_ARM
                    if (!cycleAllDouble && destMemType == TYP_DOUBLE)
                    {
                        destMemType = TYP_FLOAT;
                    }
#endif
                    varNumSrc = regArgTab[srcReg].varNum;
                    noway_assert(varNumSrc < compiler->lvaCount);
                    varDscSrc = compiler->lvaTable + varNumSrc;
                    noway_assert(varDscSrc->lvIsParam && varDscSrc->lvIsRegArg);

                    if (destMemType == TYP_REF)
                    {
                        size = EA_GCREF;
                    }
                    else if (destMemType == TYP_DOUBLE)
                    {
                        size = EA_8BYTE;
                    }
                    else
                    {
                        size = EA_4BYTE;
                    }
                }

                /* take care of the beginning register */

                noway_assert(srcReg == begReg);

                /* move the dest reg (begReg) in the extra reg */

                regNumber destRegNum = genMapRegArgNumToRegNum(destReg, destMemType);

                GetEmitter()->emitIns_R_R(insCopy, size, destRegNum, xtraReg);

                regSet.verifyRegUsed(destRegNum);
#ifdef USING_SCOPE_INFO
                psiMoveToReg(varNumSrc);
#endif // USING_SCOPE_INFO
                /* mark the beginning register as processed */

                regArgTab[srcReg].processed = true;
#ifdef TARGET_ARM
                if (size == EA_8BYTE)
                    regArgTab[srcReg + 1].processed = true;
#endif
                regArgMaskLive &= ~genMapArgNumToRegMask(srcReg, destMemType);
            }
        }
    }

    /* Finally take care of the remaining arguments that must be enregistered */
    while (regArgMaskLive)
    {
        regMaskTP regArgMaskLiveSave = regArgMaskLive;

        for (argNum = 0; argNum < argMax; argNum++)
        {
            /* If already processed go to the next one */
            if (regArgTab[argNum].processed)
            {
                continue;
            }

            if (regArgTab[argNum].slot == 0)
            { // Not a register argument
                continue;
            }

            varNum = regArgTab[argNum].varNum;
            noway_assert(varNum < compiler->lvaCount);
            varDsc            = compiler->lvaTable + varNum;
            var_types regType = regArgTab[argNum].getRegType(compiler);
            regNumber regNum  = genMapRegArgNumToRegNum(argNum, regType);

#if defined(UNIX_AMD64_ABI)
            if (regType == TYP_UNDEF)
            {
                // This could happen if the reg in regArgTab[argNum] is of the other register file -
                // for System V register passed structs where the first reg is GPR and the second an XMM reg.
                // The next register file processing will process it.
                regArgMaskLive &= ~genRegMask(regNum);
                continue;
            }
#endif // defined(UNIX_AMD64_ABI)

            noway_assert(varDsc->lvIsParam && varDsc->lvIsRegArg);
#ifndef TARGET_64BIT
#ifndef TARGET_ARM
            // Right now we think that incoming arguments are not pointer sized.  When we eventually
            // understand the calling convention, this still won't be true. But maybe we'll have a better
            // idea of how to ignore it.

            // On Arm, a long can be passed in register
            noway_assert(genTypeSize(genActualType(varDsc->TypeGet())) == TARGET_POINTER_SIZE);
#endif
#endif // TARGET_64BIT

            noway_assert(varDsc->lvIsInReg() && !regArgTab[argNum].circular);

            /* Register argument - hopefully it stays in the same register */
            regNumber destRegNum  = REG_NA;
            var_types destMemType = varDsc->GetRegisterType();

            if (regArgTab[argNum].slot == 1)
            {
                destRegNum = varDsc->GetRegNum();

#ifdef TARGET_ARM
                if (genActualType(destMemType) == TYP_DOUBLE && regArgTab[argNum + 1].processed)
                {
                    // The second half of the double has already been processed! Treat this as a single.
                    destMemType = TYP_FLOAT;
                }
#endif // TARGET_ARM
            }
#ifndef TARGET_64BIT
            else if (regArgTab[argNum].slot == 2 && genActualType(destMemType) == TYP_LONG)
            {
                assert(genActualType(varDsc->TypeGet()) == TYP_LONG || genActualType(varDsc->TypeGet()) == TYP_DOUBLE);
                if (genActualType(varDsc->TypeGet()) == TYP_DOUBLE)
                {
                    destRegNum = regNum;
                }
                else
                {
                    destRegNum = varDsc->GetOtherReg();
                }

                assert(destRegNum != REG_STK);
            }
            else
            {
                assert(regArgTab[argNum].slot == 2);
                assert(destMemType == TYP_DOUBLE);

                // For doubles, we move the entire double using the argNum representing
                // the first half of the double. There are two things we won't do:
                // (1) move the double when the 1st half of the destination is free but the
                // 2nd half is occupied, and (2) move the double when the 2nd half of the
                // destination is free but the 1st half is occupied. Here we consider the
                // case where the first half can't be moved initially because its target is
                // still busy, but the second half can be moved. We wait until the entire
                // double can be moved, if possible. For example, we have F0/F1 double moving to F2/F3,
                // and F2 single moving to F16. When we process F0, its target F2 is busy,
                // so we skip it on the first pass. When we process F1, its target F3 is
                // available. However, we want to move F0/F1 all at once, so we skip it here.
                // We process F2, which frees up F2. The next pass through, we process F0 and
                // F2/F3 are empty, so we move it. Note that if half of a double is involved
                // in a circularity with a single, then we will have already moved that half
                // above, so we go ahead and move the remaining half as a single.
                // Because there are no circularities left, we are guaranteed to terminate.

                assert(argNum > 0);
                assert(regArgTab[argNum - 1].slot == 1);

                if (!regArgTab[argNum - 1].processed)
                {
                    // The first half of the double hasn't been processed; try to be processed at the same time
                    continue;
                }

                // The first half of the double has been processed but the second half hasn't!
                // This could happen for double F2/F3 moving to F0/F1, and single F0 moving to F2.
                // In that case, there is a F0/F2 loop that is not a double-only loop. The circular
                // dependency logic above will move them as singles, leaving just F3 to move. Treat
                // it as a single to finish the shuffling.

                destMemType = TYP_FLOAT;
                destRegNum  = REG_NEXT(varDsc->GetRegNum());
            }
#endif // !TARGET_64BIT
#if (defined(UNIX_AMD64_ABI) || defined(TARGET_ARM64)) && defined(FEATURE_SIMD)
            else
            {
                assert(regArgTab[argNum].slot == 2);
                assert(argNum > 0);
                assert(regArgTab[argNum - 1].slot == 1);
                assert((varDsc->lvType == TYP_SIMD12) || (varDsc->lvType == TYP_SIMD16));
                destRegNum = varDsc->GetRegNum();
                noway_assert(regNum != destRegNum);
                continue;
            }
#endif // (defined(UNIX_AMD64_ABI) || defined(TARGET_ARM64)) && defined(FEATURE_SIMD)
            noway_assert(destRegNum != REG_NA);
            if (destRegNum != regNum)
            {
                /* Cannot trash a currently live register argument.
                 * Skip this one until its target will be free
                 * which is guaranteed to happen since we have no circular dependencies. */

                regMaskTP destMask = genRegMask(destRegNum);
#ifdef TARGET_ARM
                // Don't process the double until both halves of the destination are clear.
                if (genActualType(destMemType) == TYP_DOUBLE)
                {
                    assert((destMask & RBM_DBL_REGS) != 0);
                    destMask |= genRegMask(REG_NEXT(destRegNum));
                }
#endif

                if (destMask & regArgMaskLive)
                {
                    continue;
                }

                /* Move it to the new register */

                emitAttr size = emitActualTypeSize(destMemType);

#if defined(TARGET_ARM64)
                if (varTypeIsSIMD(varDsc) && argNum < (argMax - 1) && regArgTab[argNum + 1].slot == 2)
                {
                    // For a SIMD type that is passed in two integer registers,
                    // Limit the copy below to the first 8 bytes from the first integer register.
                    // Handle the remaining 8 bytes from the second slot in the code further below
                    assert(EA_SIZE(size) >= 8);
                    size = EA_8BYTE;
                }
#endif
                instruction copyIns = ins_Copy(regNum, destMemType);
                GetEmitter()->emitIns_R_R(copyIns, size, destRegNum, regNum);
#ifdef USING_SCOPE_INFO
                psiMoveToReg(varNum);
#endif // USING_SCOPE_INFO
            }

            /* mark the argument as processed */

            assert(!regArgTab[argNum].processed);
            regArgTab[argNum].processed = true;
            regArgMaskLive &= ~genRegMask(regNum);
#if FEATURE_MULTIREG_ARGS
            int argRegCount = 1;
#ifdef TARGET_ARM
            if (genActualType(destMemType) == TYP_DOUBLE)
            {
                argRegCount = 2;
            }
#endif
#if defined(UNIX_AMD64_ABI) && defined(FEATURE_SIMD)
            if (varTypeIsStruct(varDsc) && argNum < (argMax - 1) && regArgTab[argNum + 1].slot == 2)
            {
                argRegCount          = 2;
                int       nextArgNum = argNum + 1;
                regNumber nextRegNum = genMapRegArgNumToRegNum(nextArgNum, regArgTab[nextArgNum].getRegType(compiler));
                noway_assert(regArgTab[nextArgNum].varNum == varNum);
                // Emit a shufpd with a 0 immediate, which preserves the 0th element of the dest reg
                // and moves the 0th element of the src reg into the 1st element of the dest reg.
                GetEmitter()->emitIns_R_R_I(INS_shufpd, emitActualTypeSize(varDsc->lvType), destRegNum, nextRegNum, 0);
                // Set destRegNum to regNum so that we skip the setting of the register below,
                // but mark argNum as processed and clear regNum from the live mask.
                destRegNum = regNum;
            }
#endif // defined(UNIX_AMD64_ABI) && defined(FEATURE_SIMD)
#ifdef TARGET_ARMARCH
            if (varDsc->lvIsHfa())
            {
                // This includes both fixed-size SIMD types that are independently promoted, as well
                // as other HFA structs.
                argRegCount = varDsc->lvHfaSlots();
                if (argNum < (argMax - argRegCount + 1))
                {
                    if (compiler->lvaGetPromotionType(varDsc) == Compiler::PROMOTION_TYPE_INDEPENDENT)
                    {
                        // For an HFA type that is passed in multiple registers and promoted, we copy each field to its
                        // destination register.
                        for (int i = 0; i < argRegCount; i++)
                        {
                            int        nextArgNum  = argNum + i;
                            LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(varDsc->lvFieldLclStart + i);
                            regNumber  nextRegNum =
                                genMapRegArgNumToRegNum(nextArgNum, regArgTab[nextArgNum].getRegType(compiler));
                            destRegNum = fieldVarDsc->GetRegNum();
                            noway_assert(regArgTab[nextArgNum].varNum == varNum);
                            noway_assert(genIsValidFloatReg(nextRegNum));
                            noway_assert(genIsValidFloatReg(destRegNum));
                            GetEmitter()->emitIns_R_R(INS_mov, EA_8BYTE, destRegNum, nextRegNum);
                        }
                    }
#if defined(TARGET_ARM64) && defined(FEATURE_SIMD)
                    else
                    {
                        // For a SIMD type that is passed in multiple registers but enregistered as a vector,
                        // the code above copies the first argument register into the lower 4 or 8 bytes
                        // of the target register. Here we must handle the subsequent fields by
                        // inserting them into the upper bytes of the target SIMD floating point register.
                        argRegCount = varDsc->lvHfaSlots();
                        for (int i = 1; i < argRegCount; i++)
                        {
                            int         nextArgNum  = argNum + i;
                            regArgElem* nextArgElem = &regArgTab[nextArgNum];
                            var_types   nextArgType = nextArgElem->getRegType(compiler);
                            regNumber   nextRegNum  = genMapRegArgNumToRegNum(nextArgNum, nextArgType);
                            noway_assert(nextArgElem->varNum == varNum);
                            noway_assert(genIsValidFloatReg(nextRegNum));
                            noway_assert(genIsValidFloatReg(destRegNum));
                            GetEmitter()->emitIns_R_R_I_I(INS_mov, EA_4BYTE, destRegNum, nextRegNum, i, 0);
                        }
                    }
#endif // defined(TARGET_ARM64) && defined(FEATURE_SIMD)
                }
            }
#endif // TARGET_ARMARCH

            // Mark the rest of the argument registers corresponding to this multi-reg type as
            // being processed and no longer live.
            for (int regSlot = 1; regSlot < argRegCount; regSlot++)
            {
                int nextArgNum = argNum + regSlot;
                assert(!regArgTab[nextArgNum].processed);
                regArgTab[nextArgNum].processed = true;
                regNumber nextRegNum = genMapRegArgNumToRegNum(nextArgNum, regArgTab[nextArgNum].getRegType(compiler));
                regArgMaskLive &= ~genRegMask(nextRegNum);
            }
#endif // FEATURE_MULTIREG_ARGS
        }

        noway_assert(regArgMaskLiveSave != regArgMaskLive); // if it doesn't change, we have an infinite loop
    }
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************
 * If any incoming stack arguments live in registers, load them.
 */
void CodeGen::genEnregisterIncomingStackArgs()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genEnregisterIncomingStackArgs()\n");
    }
#endif

    // OSR handles this specially
    if (compiler->opts.IsOSR())
    {
        return;
    }

    assert(compiler->compGeneratingProlog);

    unsigned varNum = 0;

    for (LclVarDsc *varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        /* Is this variable a parameter? */

        if (!varDsc->lvIsParam)
        {
            continue;
        }

        /* If it's a register argument then it's already been taken care of.
           But, on Arm when under a profiler, we would have prespilled a register argument
           and hence here we need to load it from its prespilled location.
        */
        bool isPrespilledForProfiling = false;
#if defined(TARGET_ARM) && defined(PROFILING_SUPPORTED)
        isPrespilledForProfiling =
            compiler->compIsProfilerHookNeeded() && compiler->lvaIsPreSpilled(varNum, regSet.rsMaskPreSpillRegs(false));
#endif

        if (varDsc->lvIsRegArg && !isPrespilledForProfiling)
        {
            continue;
        }

        /* Has the parameter been assigned to a register? */

        if (!varDsc->lvIsInReg())
        {
            continue;
        }

        /* Is the variable dead on entry */

        if (!VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
        {
            continue;
        }

        /* Load the incoming parameter into the register */

        /* Figure out the home offset of the incoming argument */

        regNumber regNum = varDsc->GetArgInitReg();
        assert(regNum != REG_STK);

        var_types regType = varDsc->GetActualRegisterType();

        GetEmitter()->emitIns_R_S(ins_Load(regType), emitTypeSize(regType), regNum, varNum, 0);
        regSet.verifyRegUsed(regNum);
#ifdef USING_SCOPE_INFO
        psiMoveToReg(varNum);
#endif // USING_SCOPE_INFO
    }
}

/*-------------------------------------------------------------------------
 *
 *  We have to decide whether we're going to use block initialization
 *  in the prolog before we assign final stack offsets. This is because
 *  when using block initialization we may need additional callee-saved
 *  registers which need to be saved on the frame, thus increasing the
 *  frame size.
 *
 *  We'll count the number of locals we have to initialize,
 *  and if there are lots of them we'll use block initialization.
 *  Thus, the local variable table must have accurate register location
 *  information for enregistered locals for their register state on entry
 *  to the function.
 *
 *  At the same time we set lvMustInit for locals (enregistered or on stack)
 *  that must be initialized (e.g. initialize memory (comInitMem),
 *  untracked pointers or disable DFA)
 */
void CodeGen::genCheckUseBlockInit()
{
    assert(!compiler->compGeneratingProlog);

    unsigned initStkLclCnt = 0; // The number of int-sized stack local variables that need to be initialized (variables
                                // larger than int count for more than 1).

    unsigned   varNum;
    LclVarDsc* varDsc;

    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        // The logic below is complex. Make sure we are not
        // double-counting the initialization impact of any locals.
        bool counted = false;

        if (!varDsc->lvIsInReg() && !varDsc->lvOnFrame)
        {
            noway_assert(varDsc->lvRefCnt() == 0);
            continue;
        }

        // Initialization of OSR locals must be handled specially
        if (compiler->lvaIsOSRLocal(varNum))
        {
            varDsc->lvMustInit = 0;
            continue;
        }

        if (compiler->fgVarIsNeverZeroInitializedInProlog(varNum))
        {
            continue;
        }

        if (compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc))
        {
            // For Compiler::PROMOTION_TYPE_DEPENDENT type of promotion, the whole struct should have been
            // initialized by the parent struct. No need to set the lvMustInit bit in the
            // field locals.
            continue;
        }

        if (varDsc->lvHasExplicitInit)
        {
            varDsc->lvMustInit = 0;
            continue;
        }

        const bool isTemp      = varDsc->lvIsTemp;
        const bool hasGCPtr    = varDsc->HasGCPtr();
        const bool isTracked   = varDsc->lvTracked;
        const bool isStruct    = varTypeIsStruct(varDsc);
        const bool compInitMem = compiler->info.compInitMem;

        if (isTemp && !hasGCPtr)
        {
            varDsc->lvMustInit = 0;
            continue;
        }

        if (compInitMem || hasGCPtr || varDsc->lvMustInit)
        {
            if (isTracked)
            {
                /* For uninitialized use of tracked variables, the liveness
                 * will bubble to the top (compiler->fgFirstBB) in fgInterBlockLocalVarLiveness()
                 */
                if (varDsc->lvMustInit ||
                    VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
                {
                    /* This var must be initialized */

                    varDsc->lvMustInit = 1;

                    /* See if the variable is on the stack will be initialized
                     * using rep stos - compute the total size to be zero-ed */

                    if (varDsc->lvOnFrame)
                    {
                        if (!varDsc->lvRegister)
                        {
                            if (!varDsc->lvIsInReg() || varDsc->lvLiveInOutOfHndlr)
                            {
                                // Var is on the stack at entry.
                                initStkLclCnt +=
                                    roundUp(compiler->lvaLclSize(varNum), TARGET_POINTER_SIZE) / sizeof(int);
                                counted = true;
                            }
                        }
                        else
                        {
                            // Var is partially enregistered
                            noway_assert(genTypeSize(varDsc->TypeGet()) > sizeof(int) &&
                                         varDsc->GetOtherReg() == REG_STK);
                            initStkLclCnt += genTypeStSz(TYP_INT);
                            counted = true;
                        }
                    }
                }
            }

            if (varDsc->lvOnFrame)
            {
                bool mustInitThisVar = false;
                if (hasGCPtr && !isTracked)
                {
                    JITDUMP("must init V%02u because it has a GC ref\n", varNum);
                    mustInitThisVar = true;
                }
                else if (hasGCPtr && isStruct)
                {
                    // TODO-1stClassStructs: support precise liveness reporting for such structs.
                    JITDUMP("must init a tracked V%02u because it a struct with a GC ref\n", varNum);
                    mustInitThisVar = true;
                }
                else
                {
                    // We are done with tracked or GC vars, now look at untracked vars without GC refs.
                    if (!isTracked)
                    {
                        assert(!hasGCPtr && !isTemp);
                        if (compInitMem)
                        {
                            JITDUMP("must init V%02u because compInitMem is set and it is not a temp\n", varNum);
                            mustInitThisVar = true;
                        }
                    }
                }
                if (mustInitThisVar)
                {
                    varDsc->lvMustInit = true;

                    if (!counted)
                    {
                        initStkLclCnt += roundUp(compiler->lvaLclSize(varNum), TARGET_POINTER_SIZE) / sizeof(int);
                        counted = true;
                    }
                }
            }
        }
    }

    /* Don't forget about spill temps that hold pointers */
    assert(regSet.tmpAllFree());
    for (TempDsc* tempThis = regSet.tmpListBeg(); tempThis != nullptr; tempThis = regSet.tmpListNxt(tempThis))
    {
        if (varTypeIsGC(tempThis->tdTempType()))
        {
            initStkLclCnt++;
        }
    }

    // Record number of 4 byte slots that need zeroing.
    genInitStkLclCnt = initStkLclCnt;

    // Decide if we will do block initialization in the prolog, or use
    // a series of individual stores.
    //
    // Primary factor is the number of slots that need zeroing. We've
    // been counting by sizeof(int) above. We assume for now we can
    // only zero register width bytes per store.
    //
    // Current heuristic is to use block init when more than 4 stores
    // are required.
    //
    // TODO: Consider taking into account the presence of large structs that
    // potentially only need some fields set to zero.
    //
    // Compiler::fgVarNeedsExplicitZeroInit relies on this logic to
    // find structs that are guaranteed to be block initialized.
    // If this logic changes, Compiler::fgVarNeedsExplicitZeroInit needs
    // to be modified.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_64BIT
#if defined(TARGET_AMD64)

    // We can clear using aligned SIMD so the threshold is lower,
    // and clears in order which is better for auto-prefetching
    genUseBlockInit = (genInitStkLclCnt > 4);

#else // !defined(TARGET_AMD64)

    genUseBlockInit = (genInitStkLclCnt > 8);
#endif
#else

    genUseBlockInit = (genInitStkLclCnt > 4);

#endif // TARGET_64BIT

    if (genUseBlockInit)
    {
        regMaskTP maskCalleeRegArgMask = intRegState.rsCalleeRegArgMaskLiveIn;

        // If there is a secret stub param, don't count it, as it will no longer
        // be live when we do block init.
        if (compiler->info.compPublishStubParam)
        {
            maskCalleeRegArgMask &= ~RBM_SECRET_STUB_PARAM;
        }

#ifdef TARGET_ARM
        //
        // On the Arm if we are using a block init to initialize, then we
        // must force spill R4/R5/R6 so that we can use them during
        // zero-initialization process.
        //
        int forceSpillRegCount = genCountBits(maskCalleeRegArgMask & ~regSet.rsMaskPreSpillRegs(false)) - 1;
        if (forceSpillRegCount > 0)
            regSet.rsSetRegsModified(RBM_R4);
        if (forceSpillRegCount > 1)
            regSet.rsSetRegsModified(RBM_R5);
        if (forceSpillRegCount > 2)
            regSet.rsSetRegsModified(RBM_R6);
#endif // TARGET_ARM
    }
}

#if defined(TARGET_ARM)

void CodeGen::genPushFltRegs(regMaskTP regMask)
{
    assert(regMask != 0);                        // Don't call uness we have some registers to push
    assert((regMask & RBM_ALLFLOAT) == regMask); // Only floasting point registers should be in regMask

    regNumber lowReg = genRegNumFromMask(genFindLowestBit(regMask));
    int       slots  = genCountBits(regMask);
    // regMask should be contiguously set
    regMaskTP tmpMask = ((regMask >> lowReg) + 1); // tmpMask should have a single bit set
    assert((tmpMask & (tmpMask - 1)) == 0);
    assert(lowReg == REG_F16); // Currently we expect to start at F16 in the unwind codes

    // Our calling convention requires that we only use vpush for TYP_DOUBLE registers
    noway_assert(floatRegCanHoldType(lowReg, TYP_DOUBLE));
    noway_assert((slots % 2) == 0);

    GetEmitter()->emitIns_R_I(INS_vpush, EA_8BYTE, lowReg, slots / 2);
}

void CodeGen::genPopFltRegs(regMaskTP regMask)
{
    assert(regMask != 0);                        // Don't call uness we have some registers to pop
    assert((regMask & RBM_ALLFLOAT) == regMask); // Only floasting point registers should be in regMask

    regNumber lowReg = genRegNumFromMask(genFindLowestBit(regMask));
    int       slots  = genCountBits(regMask);
    // regMask should be contiguously set
    regMaskTP tmpMask = ((regMask >> lowReg) + 1); // tmpMask should have a single bit set
    assert((tmpMask & (tmpMask - 1)) == 0);

    // Our calling convention requires that we only use vpop for TYP_DOUBLE registers
    noway_assert(floatRegCanHoldType(lowReg, TYP_DOUBLE));
    noway_assert((slots % 2) == 0);

    GetEmitter()->emitIns_R_I(INS_vpop, EA_8BYTE, lowReg, slots / 2);
}

//------------------------------------------------------------------------
// genFreeLclFrame: free the local stack frame by adding `frameSize` to SP.
//
// Arguments:
//   frameSize - the frame size to free;
//   pUnwindStarted - was epilog unwind started or not.
//
// Notes:
//   If epilog unwind hasn't been started, and we generate code, we start unwind
//    and set* pUnwindStarted = true.
//
void CodeGen::genFreeLclFrame(unsigned frameSize, /* IN OUT */ bool* pUnwindStarted)
{
    assert(compiler->compGeneratingEpilog);

    if (frameSize == 0)
        return;

    // Add 'frameSize' to SP.
    //
    // Unfortunately, we can't just use:
    //
    //      inst_RV_IV(INS_add, REG_SPBASE, frameSize, EA_PTRSIZE);
    //
    // because we need to generate proper unwind codes for each instruction generated,
    // and large frame sizes might generate a temp register load which might
    // need an unwind code. We don't want to generate a "NOP" code for this
    // temp register load; we want the unwind codes to start after that.

    if (arm_Valid_Imm_For_Instr(INS_add, frameSize, INS_FLAGS_DONT_CARE))
    {
        if (!*pUnwindStarted)
        {
            compiler->unwindBegEpilog();
            *pUnwindStarted = true;
        }

        GetEmitter()->emitIns_R_I(INS_add, EA_PTRSIZE, REG_SPBASE, frameSize, INS_FLAGS_DONT_CARE);
    }
    else
    {
        // R12 doesn't hold arguments or return values, so can be used as temp.
        regNumber tmpReg = REG_R12;
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, frameSize);
        if (*pUnwindStarted)
        {
            compiler->unwindPadding();
        }

        // We're going to generate an unwindable instruction, so check again if
        // we need to start the unwind codes.

        if (!*pUnwindStarted)
        {
            compiler->unwindBegEpilog();
            *pUnwindStarted = true;
        }

        GetEmitter()->emitIns_R_R(INS_add, EA_PTRSIZE, REG_SPBASE, tmpReg, INS_FLAGS_DONT_CARE);
    }

    compiler->unwindAllocStack(frameSize);
}

/*-----------------------------------------------------------------------------
 *
 *  Move of relocatable displacement value to register
 */
void CodeGen::genMov32RelocatableDisplacement(BasicBlock* block, regNumber reg)
{
    GetEmitter()->emitIns_R_L(INS_movw, EA_4BYTE_DSP_RELOC, block, reg);
    GetEmitter()->emitIns_R_L(INS_movt, EA_4BYTE_DSP_RELOC, block, reg);

    if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_RELATIVE_CODE_RELOCS))
    {
        GetEmitter()->emitIns_R_R_R(INS_add, EA_4BYTE_DSP_RELOC, reg, reg, REG_PC);
    }
}

/*-----------------------------------------------------------------------------
 *
 *  Move of relocatable data-label to register
 */
void CodeGen::genMov32RelocatableDataLabel(unsigned value, regNumber reg)
{
    GetEmitter()->emitIns_R_D(INS_movw, EA_HANDLE_CNS_RELOC, value, reg);
    GetEmitter()->emitIns_R_D(INS_movt, EA_HANDLE_CNS_RELOC, value, reg);

    if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_RELATIVE_CODE_RELOCS))
    {
        GetEmitter()->emitIns_R_R_R(INS_add, EA_HANDLE_CNS_RELOC, reg, reg, REG_PC);
    }
}

/*-----------------------------------------------------------------------------
 *
 * Move of relocatable immediate to register
 */
void CodeGen::genMov32RelocatableImmediate(emitAttr size, BYTE* addr, regNumber reg)
{
    _ASSERTE(EA_IS_RELOC(size));

    GetEmitter()->emitIns_MovRelocatableImmediate(INS_movw, size, reg, addr);
    GetEmitter()->emitIns_MovRelocatableImmediate(INS_movt, size, reg, addr);

    if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_RELATIVE_CODE_RELOCS))
    {
        GetEmitter()->emitIns_R_R_R(INS_add, size, reg, reg, REG_PC);
    }
}

/*-----------------------------------------------------------------------------
 *
 *  Returns register mask to push/pop to allocate a small stack frame,
 *  instead of using "sub sp" / "add sp". Returns RBM_NONE if either frame size
 *  is zero, or if we should use "sub sp" / "add sp" instead of push/pop.
 */
regMaskTP CodeGen::genStackAllocRegisterMask(unsigned frameSize, regMaskTP maskCalleeSavedFloat)
{
    assert(compiler->compGeneratingProlog || compiler->compGeneratingEpilog);

    // We can't do this optimization with callee saved floating point registers because
    // the stack would be allocated in a wrong spot.
    if (maskCalleeSavedFloat != RBM_NONE)
        return RBM_NONE;

    // Allocate space for small frames by pushing extra registers. It generates smaller and faster code
    // that extra sub sp,XXX/add sp,XXX.
    // R0 and R1 may be used by return value. Keep things simple and just skip the optimization
    // for the 3*REGSIZE_BYTES and 4*REGSIZE_BYTES cases. They are less common and they have more
    // significant negative side-effects (more memory bus traffic).
    switch (frameSize)
    {
        case REGSIZE_BYTES:
            return RBM_R3;
        case 2 * REGSIZE_BYTES:
            return RBM_R2 | RBM_R3;
        default:
            return RBM_NONE;
    }
}

#endif // TARGET_ARM

/*****************************************************************************
 *
 *  initFltRegs -- The mask of float regs to be zeroed.
 *  initDblRegs -- The mask of double regs to be zeroed.
 *  initReg -- A zero initialized integer reg to copy from.
 *
 *  Does best effort to move between VFP/xmm regs if one is already
 *  initialized to 0. (Arm Only) Else copies from the integer register which
 *  is slower.
 */
void CodeGen::genZeroInitFltRegs(const regMaskTP& initFltRegs, const regMaskTP& initDblRegs, const regNumber& initReg)
{
    assert(compiler->compGeneratingProlog);

    // The first float/double reg that is initialized to 0. So they can be used to
    // initialize the remaining registers.
    regNumber fltInitReg = REG_NA;
    regNumber dblInitReg = REG_NA;

    // Iterate through float/double registers and initialize them to 0 or
    // copy from already initialized register of the same type.
    regMaskTP regMask = genRegMask(REG_FP_FIRST);
    for (regNumber reg = REG_FP_FIRST; reg <= REG_FP_LAST; reg = REG_NEXT(reg), regMask <<= 1)
    {
        if (regMask & initFltRegs)
        {
            // Do we have a float register already set to 0?
            if (fltInitReg != REG_NA)
            {
                // Copy from float.
                inst_RV_RV(ins_Copy(TYP_FLOAT), reg, fltInitReg, TYP_FLOAT);
            }
            else
            {
#ifdef TARGET_ARM
                // Do we have a double register initialized to 0?
                if (dblInitReg != REG_NA)
                {
                    // Copy from double.
                    inst_RV_RV(INS_vcvt_d2f, reg, dblInitReg, TYP_FLOAT);
                }
                else
                {
                    // Copy from int.
                    inst_RV_RV(INS_vmov_i2f, reg, initReg, TYP_FLOAT, EA_4BYTE);
                }
#elif defined(TARGET_XARCH)
                // XORPS is the fastest and smallest way to initialize a XMM register to zero.
                inst_RV_RV(INS_xorps, reg, reg, TYP_DOUBLE);
                dblInitReg = reg;
#elif defined(TARGET_ARM64)
                // We will just zero out the entire vector register. This sets it to a double/float zero value
                GetEmitter()->emitIns_R_I(INS_movi, EA_16BYTE, reg, 0x00, INS_OPTS_16B);
#else // TARGET*
#error Unsupported or unset target architecture
#endif
                fltInitReg = reg;
            }
        }
        else if (regMask & initDblRegs)
        {
            // Do we have a double register already set to 0?
            if (dblInitReg != REG_NA)
            {
                // Copy from double.
                inst_RV_RV(ins_Copy(TYP_DOUBLE), reg, dblInitReg, TYP_DOUBLE);
            }
            else
            {
#ifdef TARGET_ARM
                // Do we have a float register initialized to 0?
                if (fltInitReg != REG_NA)
                {
                    // Copy from float.
                    inst_RV_RV(INS_vcvt_f2d, reg, fltInitReg, TYP_DOUBLE);
                }
                else
                {
                    // Copy from int.
                    inst_RV_RV_RV(INS_vmov_i2d, reg, initReg, initReg, EA_8BYTE);
                }
#elif defined(TARGET_XARCH)
                // XORPS is the fastest and smallest way to initialize a XMM register to zero.
                inst_RV_RV(INS_xorps, reg, reg, TYP_DOUBLE);
                fltInitReg = reg;
#elif defined(TARGET_ARM64)
                // We will just zero out the entire vector register. This sets it to a double/float zero value
                GetEmitter()->emitIns_R_I(INS_movi, EA_16BYTE, reg, 0x00, INS_OPTS_16B);
#else // TARGET*
#error Unsupported or unset target architecture
#endif
                dblInitReg = reg;
            }
        }
    }
}

/*-----------------------------------------------------------------------------
 *
 *  Restore any callee-saved registers we have used
 */

#if defined(TARGET_ARM)

bool CodeGen::genCanUsePopToReturn(regMaskTP maskPopRegsInt, bool jmpEpilog)
{
    assert(compiler->compGeneratingEpilog);

    if (!jmpEpilog && regSet.rsMaskPreSpillRegs(true) == RBM_NONE)
        return true;
    else
        return false;
}

void CodeGen::genPopCalleeSavedRegisters(bool jmpEpilog)
{
    assert(compiler->compGeneratingEpilog);

    regMaskTP maskPopRegs      = regSet.rsGetModifiedRegsMask() & RBM_CALLEE_SAVED;
    regMaskTP maskPopRegsFloat = maskPopRegs & RBM_ALLFLOAT;
    regMaskTP maskPopRegsInt   = maskPopRegs & ~maskPopRegsFloat;

    // First, pop float registers

    if (maskPopRegsFloat != RBM_NONE)
    {
        genPopFltRegs(maskPopRegsFloat);
        compiler->unwindPopMaskFloat(maskPopRegsFloat);
    }

    // Next, pop integer registers

    if (!jmpEpilog)
    {
        regMaskTP maskStackAlloc = genStackAllocRegisterMask(compiler->compLclFrameSize, maskPopRegsFloat);
        maskPopRegsInt |= maskStackAlloc;
    }

    if (isFramePointerUsed())
    {
        assert(!regSet.rsRegsModified(RBM_FPBASE));
        maskPopRegsInt |= RBM_FPBASE;
    }

    if (genCanUsePopToReturn(maskPopRegsInt, jmpEpilog))
    {
        maskPopRegsInt |= RBM_PC;
        // Record the fact that we use a pop to the PC to perform the return
        genUsedPopToReturn = true;
    }
    else
    {
        maskPopRegsInt |= RBM_LR;
        // Record the fact that we did not use a pop to the PC to perform the return
        genUsedPopToReturn = false;
    }

    assert(FitsIn<int>(maskPopRegsInt));
    inst_IV(INS_pop, (int)maskPopRegsInt);
    compiler->unwindPopMaskInt(maskPopRegsInt);
}

#elif defined(TARGET_ARM64)

void CodeGen::genPopCalleeSavedRegistersAndFreeLclFrame(bool jmpEpilog)
{
    assert(compiler->compGeneratingEpilog);

    regMaskTP rsRestoreRegs = regSet.rsGetModifiedRegsMask() & RBM_CALLEE_SAVED;

    if (isFramePointerUsed())
    {
        rsRestoreRegs |= RBM_FPBASE;
    }

    rsRestoreRegs |= RBM_LR; // We must save/restore the return address (in the LR register)

    regMaskTP regsToRestoreMask = rsRestoreRegs;

    int totalFrameSize = genTotalFrameSize();

    int calleeSaveSPOffset = 0; // This will be the starting place for restoring the callee-saved registers, in
                                // decreasing order.
    int frameType         = 0;  // An indicator of what type of frame we are popping.
    int calleeSaveSPDelta = 0;  // Amount to add to SP after callee-saved registers have been restored.

    if (isFramePointerUsed())
    {
        if ((compiler->lvaOutgoingArgSpaceSize == 0) && (totalFrameSize <= 504) &&
            !genSaveFpLrWithAllCalleeSavedRegisters)
        {
            JITDUMP("Frame type 1. #outsz=0; #framesz=%d; localloc? %s\n", totalFrameSize,
                    dspBool(compiler->compLocallocUsed));

            frameType = 1;
            if (compiler->compLocallocUsed)
            {
                // Restore sp from fp
                //      mov sp, fp
                inst_RV_RV(INS_mov, REG_SPBASE, REG_FPBASE);
                compiler->unwindSetFrameReg(REG_FPBASE, 0);
            }

            regsToRestoreMask &= ~(RBM_FP | RBM_LR); // We'll restore FP/LR at the end, and post-index SP.

            // Compute callee save SP offset which is at the top of local frame while the FP/LR is saved at the
            // bottom of stack.
            calleeSaveSPOffset = compiler->compLclFrameSize + 2 * REGSIZE_BYTES;
        }
        else if (totalFrameSize <= 512)
        {
            if (compiler->compLocallocUsed)
            {
                // Restore sp from fp
                //      sub sp, fp, #outsz // Uses #outsz if FP/LR stored at bottom
                int SPtoFPdelta = genSPtoFPdelta();
                GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, SPtoFPdelta);
                compiler->unwindSetFrameReg(REG_FPBASE, SPtoFPdelta);
            }

            if (genSaveFpLrWithAllCalleeSavedRegisters)
            {
                JITDUMP("Frame type 4 (save FP/LR at top). #outsz=%d; #framesz=%d; localloc? %s\n",
                        unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize,
                        dspBool(compiler->compLocallocUsed));

                frameType = 4;

                calleeSaveSPOffset = compiler->compLclFrameSize;

                // Remove the frame after we're done restoring the callee-saved registers.
                calleeSaveSPDelta = totalFrameSize;
            }
            else
            {
                JITDUMP("Frame type 2 (save FP/LR at bottom). #outsz=%d; #framesz=%d; localloc? %s\n",
                        unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize,
                        dspBool(compiler->compLocallocUsed));

                frameType = 2;

                regsToRestoreMask &= ~(RBM_FP | RBM_LR); // We'll restore FP/LR at the end, and post-index SP.

                // Compute callee save SP offset which is at the top of local frame while the FP/LR is saved at the
                // bottom of stack.
                calleeSaveSPOffset = compiler->compLclFrameSize + 2 * REGSIZE_BYTES;
            }
        }
        else if (!genSaveFpLrWithAllCalleeSavedRegisters)
        {
            JITDUMP("Frame type 3 (save FP/LR at bottom). #outsz=%d; #framesz=%d; localloc? %s\n",
                    unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize, dspBool(compiler->compLocallocUsed));

            frameType = 3;

            int calleeSaveSPDeltaUnaligned = totalFrameSize - compiler->compLclFrameSize -
                                             2 * REGSIZE_BYTES; // 2 for FP, LR which we'll restore later.
            assert(calleeSaveSPDeltaUnaligned >= 0);
            assert((calleeSaveSPDeltaUnaligned % 8) == 0); // It better at least be 8 byte aligned.
            calleeSaveSPDelta = AlignUp((UINT)calleeSaveSPDeltaUnaligned, STACK_ALIGN);

            JITDUMP("    calleeSaveSPDelta=%d\n", calleeSaveSPDelta);

            regsToRestoreMask &= ~(RBM_FP | RBM_LR); // We'll restore FP/LR at the end, and (hopefully) post-index SP.

            int remainingFrameSz = totalFrameSize - calleeSaveSPDelta;
            assert(remainingFrameSz > 0);

            if (compiler->lvaOutgoingArgSpaceSize > 504)
            {
                // We can't do "ldp fp,lr,[sp,#outsz]" because #outsz is too big.
                // If compiler->lvaOutgoingArgSpaceSize is not aligned, we need to align the SP adjustment.
                assert(remainingFrameSz > (int)compiler->lvaOutgoingArgSpaceSize);
                int spAdjustment2Unaligned = remainingFrameSz - compiler->lvaOutgoingArgSpaceSize;
                int spAdjustment2          = (int)roundUp((unsigned)spAdjustment2Unaligned, STACK_ALIGN);
                int alignmentAdjustment2   = spAdjustment2 - spAdjustment2Unaligned;
                assert((alignmentAdjustment2 == 0) || (alignmentAdjustment2 == REGSIZE_BYTES));

                // Restore sp from fp. No need to update sp after this since we've set up fp before adjusting sp
                // in prolog.
                //      sub sp, fp, #alignmentAdjustment2
                GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, alignmentAdjustment2);
                compiler->unwindSetFrameReg(REG_FPBASE, alignmentAdjustment2);

                // Generate:
                //      ldp fp,lr,[sp]
                //      add sp,sp,#remainingFrameSz

                JITDUMP("    alignmentAdjustment2=%d\n", alignmentAdjustment2);
                genEpilogRestoreRegPair(REG_FP, REG_LR, alignmentAdjustment2, spAdjustment2, false, REG_IP1, nullptr);
            }
            else
            {
                if (compiler->compLocallocUsed)
                {
                    // Restore sp from fp; here that's #outsz from SP
                    //      sub sp, fp, #outsz
                    int SPtoFPdelta = genSPtoFPdelta();
                    assert(SPtoFPdelta == (int)compiler->lvaOutgoingArgSpaceSize);
                    GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, SPtoFPdelta);
                    compiler->unwindSetFrameReg(REG_FPBASE, SPtoFPdelta);
                }

                // Generate:
                //      ldp fp,lr,[sp,#outsz]
                //      add sp,sp,#remainingFrameSz     ; might need to load this constant in a scratch register if
                //                                      ; it's large

                JITDUMP("    remainingFrameSz=%d\n", remainingFrameSz);

                genEpilogRestoreRegPair(REG_FP, REG_LR, compiler->lvaOutgoingArgSpaceSize, remainingFrameSz, false,
                                        REG_IP1, nullptr);
            }

            // Unlike frameType=1 or frameType=2 that restore SP at the end,
            // frameType=3 already adjusted SP above to delete local frame.
            // There is at most one alignment slot between SP and where we store the callee-saved registers.
            calleeSaveSPOffset = calleeSaveSPDelta - calleeSaveSPDeltaUnaligned;
            assert((calleeSaveSPOffset == 0) || (calleeSaveSPOffset == REGSIZE_BYTES));
        }
        else
        {
            JITDUMP("Frame type 5 (save FP/LR at top). #outsz=%d; #framesz=%d; localloc? %s\n",
                    unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize, dspBool(compiler->compLocallocUsed));

            frameType = 5;

            int calleeSaveSPDeltaUnaligned = totalFrameSize - compiler->compLclFrameSize;
            assert(calleeSaveSPDeltaUnaligned >= 0);
            assert((calleeSaveSPDeltaUnaligned % 8) == 0); // It better at least be 8 byte aligned.
            calleeSaveSPDelta = AlignUp((UINT)calleeSaveSPDeltaUnaligned, STACK_ALIGN);

            calleeSaveSPOffset = calleeSaveSPDelta - calleeSaveSPDeltaUnaligned;
            assert((calleeSaveSPOffset == 0) || (calleeSaveSPOffset == REGSIZE_BYTES));

            // Restore sp from fp:
            //      sub sp, fp, #sp-to-fp-delta
            // This is the same whether there is localloc or not. Note that we don't need to do anything to remove the
            // "remainingFrameSz" to reverse the SUB of that amount in the prolog.

            int offsetSpToSavedFp = calleeSaveSPDelta -
                                    (compiler->info.compIsVarArgs ? MAX_REG_ARG * REGSIZE_BYTES : 0) -
                                    2 * REGSIZE_BYTES; // -2 for FP, LR
            GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, offsetSpToSavedFp);
            compiler->unwindSetFrameReg(REG_FPBASE, offsetSpToSavedFp);
        }
    }
    else
    {
        // No frame pointer (no chaining).
        NYI("Frame without frame pointer");
        calleeSaveSPOffset = 0;
    }

    JITDUMP("    calleeSaveSPOffset=%d, calleeSaveSPDelta=%d\n", calleeSaveSPOffset, calleeSaveSPDelta);
    genRestoreCalleeSavedRegistersHelp(regsToRestoreMask, calleeSaveSPOffset, calleeSaveSPDelta);

    if (frameType == 1)
    {
        // Generate:
        //      ldp fp,lr,[sp],#framesz

        GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, totalFrameSize,
                                      INS_OPTS_POST_INDEX);
        compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, -totalFrameSize);
    }
    else if (frameType == 2)
    {
        // Generate:
        //      ldr fp,lr,[sp,#outsz]
        //      add sp,sp,#framesz

        GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE,
                                      compiler->lvaOutgoingArgSpaceSize);
        compiler->unwindSaveRegPair(REG_FP, REG_LR, compiler->lvaOutgoingArgSpaceSize);

        GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, totalFrameSize);
        compiler->unwindAllocStack(totalFrameSize);
    }
    else if (frameType == 3)
    {
        // Nothing to do after restoring callee-saved registers.
    }
    else if (frameType == 4)
    {
        // Nothing to do after restoring callee-saved registers.
    }
    else if (frameType == 5)
    {
        // Nothing to do after restoring callee-saved registers.
    }
    else
    {
        unreached();
    }
}

#elif defined(TARGET_XARCH)

void CodeGen::genPopCalleeSavedRegisters(bool jmpEpilog)
{
    assert(compiler->compGeneratingEpilog);

    unsigned popCount = 0;
    if (regSet.rsRegsModified(RBM_EBX))
    {
        popCount++;
        inst_RV(INS_pop, REG_EBX, TYP_I_IMPL);
    }
    if (regSet.rsRegsModified(RBM_FPBASE))
    {
        // EBP cannot be directly modified for EBP frame and double-aligned frames
        assert(!doubleAlignOrFramePointerUsed());

        popCount++;
        inst_RV(INS_pop, REG_EBP, TYP_I_IMPL);
    }

#ifndef UNIX_AMD64_ABI
    // For System V AMD64 calling convention ESI and EDI are volatile registers.
    if (regSet.rsRegsModified(RBM_ESI))
    {
        popCount++;
        inst_RV(INS_pop, REG_ESI, TYP_I_IMPL);
    }
    if (regSet.rsRegsModified(RBM_EDI))
    {
        popCount++;
        inst_RV(INS_pop, REG_EDI, TYP_I_IMPL);
    }
#endif // !defined(UNIX_AMD64_ABI)

#ifdef TARGET_AMD64
    if (regSet.rsRegsModified(RBM_R12))
    {
        popCount++;
        inst_RV(INS_pop, REG_R12, TYP_I_IMPL);
    }
    if (regSet.rsRegsModified(RBM_R13))
    {
        popCount++;
        inst_RV(INS_pop, REG_R13, TYP_I_IMPL);
    }
    if (regSet.rsRegsModified(RBM_R14))
    {
        popCount++;
        inst_RV(INS_pop, REG_R14, TYP_I_IMPL);
    }
    if (regSet.rsRegsModified(RBM_R15))
    {
        popCount++;
        inst_RV(INS_pop, REG_R15, TYP_I_IMPL);
    }
#endif // TARGET_AMD64

    // Amd64/x86 doesn't support push/pop of xmm registers.
    // These will get saved to stack separately after allocating
    // space on stack in prolog sequence.  PopCount is essentially
    // tracking the count of integer registers pushed.

    noway_assert(compiler->compCalleeRegsPushed == popCount);
}

#elif defined(TARGET_X86)

void CodeGen::genPopCalleeSavedRegisters(bool jmpEpilog)
{
    assert(compiler->compGeneratingEpilog);

    unsigned popCount = 0;

    /*  NOTE:   The EBP-less frame code below depends on the fact that
                all of the pops are generated right at the start and
                each takes one byte of machine code.
     */

    if (regSet.rsRegsModified(RBM_FPBASE))
    {
        // EBP cannot be directly modified for EBP frame and double-aligned frames
        noway_assert(!doubleAlignOrFramePointerUsed());

        inst_RV(INS_pop, REG_EBP, TYP_I_IMPL);
        popCount++;
    }
    if (regSet.rsRegsModified(RBM_EBX))
    {
        popCount++;
        inst_RV(INS_pop, REG_EBX, TYP_I_IMPL);
    }
    if (regSet.rsRegsModified(RBM_ESI))
    {
        popCount++;
        inst_RV(INS_pop, REG_ESI, TYP_I_IMPL);
    }
    if (regSet.rsRegsModified(RBM_EDI))
    {
        popCount++;
        inst_RV(INS_pop, REG_EDI, TYP_I_IMPL);
    }
    noway_assert(compiler->compCalleeRegsPushed == popCount);
}

#endif // TARGET*

// We need a register with value zero. Zero the initReg, if necessary, and set *pInitRegZeroed if so.
// Return the register to use. On ARM64, we never touch the initReg, and always just return REG_ZR.
regNumber CodeGen::genGetZeroReg(regNumber initReg, bool* pInitRegZeroed)
{
#ifdef TARGET_ARM64
    return REG_ZR;
#else  // !TARGET_ARM64
    if (*pInitRegZeroed == false)
    {
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, initReg);
        *pInitRegZeroed = true;
    }
    return initReg;
#endif // !TARGET_ARM64
}

//-----------------------------------------------------------------------------
// genZeroInitFrame: Zero any untracked pointer locals and/or initialize memory for locspace
//
// Arguments:
//    untrLclHi      - (Untracked locals High-Offset)  The upper bound offset at which the zero init
//                                                     code will end initializing memory (not inclusive).
//    untrLclLo      - (Untracked locals Low-Offset)   The lower bound at which the zero init code will
//                                                     start zero initializing memory.
//    initReg        - A scratch register (that gets set to zero on some platforms).
//    pInitRegZeroed - OUT parameter. *pInitRegZeroed is set to 'true' if this method sets initReg register to zero,
//                     'false' if initReg was set to a non-zero value, and left unchanged if initReg was not touched.
void CodeGen::genZeroInitFrame(int untrLclHi, int untrLclLo, regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    if (genUseBlockInit)
    {
        assert(untrLclHi > untrLclLo);
#ifdef TARGET_ARM
        // Generate the following code:
        //
        // For cnt less than 10
        //
        //            mov     rZero1, 0
        //            mov     rZero2, 0
        //            mov     rCnt,  <cnt>
        //            stm     <rZero1,rZero2>,[rAddr!]
        // <optional> stm     <rZero1,rZero2>,[rAddr!]
        // <optional> stm     <rZero1,rZero2>,[rAddr!]
        // <optional> stm     <rZero1,rZero2>,[rAddr!]
        // <optional> str     rZero1,[rAddr]
        //
        // For rCnt greater than or equal to 10
        //
        //            mov     rZero1, 0
        //            mov     rZero2, 0
        //            mov     rCnt,  <cnt/2>
        //            sub     rAddr, sp, OFFS
        //
        //        loop:
        //            stm     <rZero1,rZero2>,[rAddr!]
        //            sub     rCnt,rCnt,1
        //            jnz     loop
        //
        // <optional> str     rZero1,[rAddr]   // When cnt is odd

        regNumber rAddr;
        regNumber rCnt = REG_NA; // Invalid
        regMaskTP regMask;

        regMaskTP availMask = regSet.rsGetModifiedRegsMask() | RBM_INT_CALLEE_TRASH; // Set of available registers
        availMask &= ~intRegState.rsCalleeRegArgMaskLiveIn; // Remove all of the incoming argument registers as they are
                                                            // currently live
        availMask &= ~genRegMask(initReg); // Remove the pre-calculated initReg as we will zero it and maybe use it for
                                           // a large constant.

        if (compiler->compLocallocUsed)
        {
            availMask &= ~RBM_SAVED_LOCALLOC_SP; // Remove the register reserved when we have a localloc frame
        }

        regNumber rZero1; // We're going to use initReg for rZero1
        regNumber rZero2;

        // We pick the next lowest register number for rZero2
        noway_assert(availMask != RBM_NONE);
        regMask = genFindLowestBit(availMask);
        rZero2  = genRegNumFromMask(regMask);
        availMask &= ~regMask;
        assert((genRegMask(rZero2) & intRegState.rsCalleeRegArgMaskLiveIn) == 0); // rZero2 is not a live incoming
                                                                                  // argument reg

        // We pick the next lowest register number for rAddr
        noway_assert(availMask != RBM_NONE);
        regMask = genFindLowestBit(availMask);
        rAddr   = genRegNumFromMask(regMask);
        availMask &= ~regMask;

        bool     useLoop   = false;
        unsigned uCntBytes = untrLclHi - untrLclLo;
        assert((uCntBytes % sizeof(int)) == 0);         // The smallest stack slot is always 4 bytes.
        unsigned uCntSlots = uCntBytes / REGSIZE_BYTES; // How many register sized stack slots we're going to use.

        // When uCntSlots is 9 or less, we will emit a sequence of stm/stp instructions inline.
        // When it is 10 or greater, we will emit a loop containing a stm/stp instruction.
        // In both of these cases the stm/stp instruction will write two zeros to memory
        // and we will use a single str instruction at the end whenever we have an odd count.
        if (uCntSlots >= 10)
            useLoop = true;

        if (useLoop)
        {
            // We pick the next lowest register number for rCnt
            noway_assert(availMask != RBM_NONE);
            regMask = genFindLowestBit(availMask);
            rCnt    = genRegNumFromMask(regMask);
            availMask &= ~regMask;
        }

        // rAddr is not a live incoming argument reg
        assert((genRegMask(rAddr) & intRegState.rsCalleeRegArgMaskLiveIn) == 0);

        if (arm_Valid_Imm_For_Add(untrLclLo, INS_FLAGS_DONT_CARE))
        {
            GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, rAddr, genFramePointerReg(), untrLclLo);
        }
        else
        {
            // Load immediate into the InitReg register
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, (ssize_t)untrLclLo);
            GetEmitter()->emitIns_R_R_R(INS_add, EA_PTRSIZE, rAddr, genFramePointerReg(), initReg);
            *pInitRegZeroed = false;
        }

        if (useLoop)
        {
            noway_assert(uCntSlots >= 2);
            assert((genRegMask(rCnt) & intRegState.rsCalleeRegArgMaskLiveIn) == 0); // rCnt is not a live incoming
                                                                                    // argument reg
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, rCnt, (ssize_t)uCntSlots / 2);
        }

        rZero1 = genGetZeroReg(initReg, pInitRegZeroed);
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, rZero2);
        target_ssize_t stmImm = (target_ssize_t)(genRegMask(rZero1) | genRegMask(rZero2));

        if (!useLoop)
        {
            while (uCntBytes >= REGSIZE_BYTES * 2)
            {
                GetEmitter()->emitIns_R_I(INS_stm, EA_PTRSIZE, rAddr, stmImm);
                uCntBytes -= REGSIZE_BYTES * 2;
            }
        }
        else
        {
            GetEmitter()->emitIns_R_I(INS_stm, EA_PTRSIZE, rAddr, stmImm); // zero stack slots
            GetEmitter()->emitIns_R_I(INS_sub, EA_PTRSIZE, rCnt, 1, INS_FLAGS_SET);
            GetEmitter()->emitIns_J(INS_bhi, NULL, -3);
            uCntBytes %= REGSIZE_BYTES * 2;
        }

        if (uCntBytes >= REGSIZE_BYTES) // check and zero the last register-sized stack slot (odd number)
        {
            GetEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, rZero1, rAddr, 0);
            uCntBytes -= REGSIZE_BYTES;
        }

        noway_assert(uCntBytes == 0);

#elif defined(TARGET_ARM64)
        int bytesToWrite = untrLclHi - untrLclLo;

        const regNumber zeroSimdReg          = REG_ZERO_INIT_FRAME_SIMD;
        bool            simdRegZeroed        = false;
        const int       simdRegPairSizeBytes = 2 * FP_REGSIZE_BYTES;

        regNumber addrReg = REG_ZERO_INIT_FRAME_REG1;

        if (addrReg == initReg)
        {
            *pInitRegZeroed = false;
        }

        int addrOffset = 0;

        // The following invariants are held below:
        //
        //   1) [addrReg, #addrOffset] points at a location where next chunk of zero bytes will be written;
        //   2) bytesToWrite specifies the number of bytes on the frame to initialize;
        //   3) if simdRegZeroed is true then 128-bit wide zeroSimdReg contains zeroes.

        const int bytesUseZeroingLoop = 192;

        if (bytesToWrite >= bytesUseZeroingLoop)
        {
            // Generates the following code:
            //
            // When the size of the region is greater than or equal to 256 bytes
            // **and** DC ZVA instruction use is permitted
            // **and** the instruction block size is configured to 64 bytes:
            //
            //    movi    v16.16b, #0
            //    add     x9, fp, #(untrLclLo+64)
            //    add     x10, fp, #(untrLclHi-64)
            //    stp     q16, q16, [x9, #-64]
            //    stp     q16, q16, [x9, #-32]
            //    bfm     x9, xzr, #0, #5
            //
            // loop:
            //    dc      zva, x9
            //    add     x9, x9, #64
            //    cmp     x9, x10
            //    blo     loop
            //
            //    stp     q16, q16, [x10]
            //    stp     q16, q16, [x10, #32]
            //
            // Otherwise:
            //
            //     movi    v16.16b, #0
            //     add     x9, fp, #(untrLclLo-32)
            //     mov     x10, #(bytesToWrite-64)
            //
            // loop:
            //     stp     q16, q16, [x9, #32]
            //     stp     q16, q16, [x9, #64]!
            //     subs    x10, x10, #64
            //     bge     loop

            const int bytesUseDataCacheZeroInstruction = 256;

            GetEmitter()->emitIns_R_I(INS_movi, EA_16BYTE, zeroSimdReg, 0, INS_OPTS_16B);
            simdRegZeroed = true;

            if ((bytesToWrite >= bytesUseDataCacheZeroInstruction) &&
                compiler->compOpportunisticallyDependsOn(InstructionSet_Dczva))
            {
                // The first and the last 64 bytes should be written with two stp q-reg instructions.
                // This is in order to avoid **unintended** zeroing of the data by dc zva
                // outside of [fp+untrLclLo, fp+untrLclHi) memory region.

                genInstrWithConstant(INS_add, EA_PTRSIZE, addrReg, genFramePointerReg(), untrLclLo + 64, addrReg);
                addrOffset = -64;

                const regNumber endAddrReg = REG_ZERO_INIT_FRAME_REG2;

                if (endAddrReg == initReg)
                {
                    *pInitRegZeroed = false;
                }

                genInstrWithConstant(INS_add, EA_PTRSIZE, endAddrReg, genFramePointerReg(), untrLclHi - 64, endAddrReg);

                GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_16BYTE, zeroSimdReg, zeroSimdReg, addrReg, addrOffset);
                addrOffset += simdRegPairSizeBytes;

                GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_16BYTE, zeroSimdReg, zeroSimdReg, addrReg, addrOffset);
                addrOffset += simdRegPairSizeBytes;

                assert(addrOffset == 0);

                GetEmitter()->emitIns_R_R_I_I(INS_bfm, EA_PTRSIZE, addrReg, REG_ZR, 0, 5);
                // addrReg points at the beginning of a cache line.

                GetEmitter()->emitIns_R(INS_dczva, EA_PTRSIZE, addrReg);
                GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, addrReg, addrReg, 64);
                GetEmitter()->emitIns_R_R(INS_cmp, EA_PTRSIZE, addrReg, endAddrReg);
                GetEmitter()->emitIns_J(INS_blo, NULL, -4);

                addrReg      = endAddrReg;
                bytesToWrite = 64;
            }
            else
            {
                genInstrWithConstant(INS_add, EA_PTRSIZE, addrReg, genFramePointerReg(), untrLclLo - 32, addrReg);
                addrOffset = 32;

                const regNumber countReg = REG_ZERO_INIT_FRAME_REG2;

                if (countReg == initReg)
                {
                    *pInitRegZeroed = false;
                }

                instGen_Set_Reg_To_Imm(EA_PTRSIZE, countReg, bytesToWrite - 64);

                GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_16BYTE, zeroSimdReg, zeroSimdReg, addrReg, 32);
                GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_16BYTE, zeroSimdReg, zeroSimdReg, addrReg, 64,
                                              INS_OPTS_PRE_INDEX);

                GetEmitter()->emitIns_R_R_I(INS_subs, EA_PTRSIZE, countReg, countReg, 64);
                GetEmitter()->emitIns_J(INS_bge, NULL, -4);

                bytesToWrite %= 64;
            }
        }
        else
        {
            genInstrWithConstant(INS_add, EA_PTRSIZE, addrReg, genFramePointerReg(), untrLclLo, addrReg);
        }

        if (bytesToWrite >= simdRegPairSizeBytes)
        {
            // Generates the following code:
            //
            //     movi    v16.16b, #0
            //     stp     q16, q16, [x9, #addrOffset]
            //     stp     q16, q16, [x9, #(addrOffset+32)]
            // ...
            //     stp     q16, q16, [x9, #(addrOffset+roundDown(bytesToWrite, 32))]

            if (!simdRegZeroed)
            {
                GetEmitter()->emitIns_R_I(INS_movi, EA_16BYTE, zeroSimdReg, 0, INS_OPTS_16B);
                simdRegZeroed = true;
            }

            for (; bytesToWrite >= simdRegPairSizeBytes; bytesToWrite -= simdRegPairSizeBytes)
            {
                GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_16BYTE, zeroSimdReg, zeroSimdReg, addrReg, addrOffset);
                addrOffset += simdRegPairSizeBytes;
            }
        }

        const int regPairSizeBytes = 2 * REGSIZE_BYTES;

        if (bytesToWrite >= regPairSizeBytes)
        {
            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_ZR, REG_ZR, addrReg, addrOffset);
            addrOffset += regPairSizeBytes;
            bytesToWrite -= regPairSizeBytes;
        }

        if (bytesToWrite >= REGSIZE_BYTES)
        {
            GetEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, REG_ZR, addrReg, addrOffset);
            addrOffset += REGSIZE_BYTES;
            bytesToWrite -= REGSIZE_BYTES;
        }

        if (bytesToWrite == sizeof(int))
        {
            GetEmitter()->emitIns_R_R_I(INS_str, EA_4BYTE, REG_ZR, addrReg, addrOffset);
            bytesToWrite = 0;
        }

        assert(bytesToWrite == 0);
#elif defined(TARGET_XARCH)
        assert(compiler->getSIMDSupportLevel() >= SIMD_SSE2_Supported);
        emitter*  emit        = GetEmitter();
        regNumber frameReg    = genFramePointerReg();
        regNumber zeroReg     = REG_NA;
        int       blkSize     = untrLclHi - untrLclLo;
        int       minSimdSize = XMM_REGSIZE_BYTES;

        assert(blkSize >= 0);
        noway_assert((blkSize % sizeof(int)) == 0);
        // initReg is not a live incoming argument reg
        assert((genRegMask(initReg) & intRegState.rsCalleeRegArgMaskLiveIn) == 0);
#if defined(TARGET_AMD64)
        // We will align on x64 so can use the aligned mov
        instruction simdMov = simdAlignedMovIns();
        // Aligning low we want to move up to next boundary
        int alignedLclLo = (untrLclLo + (XMM_REGSIZE_BYTES - 1)) & -XMM_REGSIZE_BYTES;

        if ((untrLclLo != alignedLclLo) && (blkSize < 2 * XMM_REGSIZE_BYTES))
        {
            // If unaligned and smaller then 2 x SIMD size we won't bother trying to align
            assert((alignedLclLo - untrLclLo) < XMM_REGSIZE_BYTES);
            simdMov = simdUnalignedMovIns();
        }
#else // !defined(TARGET_AMD64)
        // We aren't going to try and align on x86
        instruction simdMov      = simdUnalignedMovIns();
        int         alignedLclLo = untrLclLo;
#endif
        if (blkSize < minSimdSize)
        {
            zeroReg = genGetZeroReg(initReg, pInitRegZeroed);

            int i = 0;
            for (; i + REGSIZE_BYTES <= blkSize; i += REGSIZE_BYTES)
            {
                emit->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, zeroReg, frameReg, untrLclLo + i);
            }
#if defined(TARGET_AMD64)
            assert((i == blkSize) || (i + (int)sizeof(int) == blkSize));
            if (i != blkSize)
            {
                emit->emitIns_AR_R(ins_Store(TYP_INT), EA_4BYTE, zeroReg, frameReg, untrLclLo + i);
                i += sizeof(int);
            }
#endif // defined(TARGET_AMD64)
            assert(i == blkSize);
        }
        else
        {
            // Grab a non-argument, non-callee saved XMM reg
            CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef UNIX_AMD64_ABI
            // System V x64 first temp reg is xmm8
            regNumber zeroSIMDReg = genRegNumFromMask(RBM_XMM8);
#else
            // Windows first temp reg is xmm4
            regNumber zeroSIMDReg = genRegNumFromMask(RBM_XMM4);
#endif // UNIX_AMD64_ABI

#if defined(TARGET_AMD64)
            int       alignedLclHi;
            int       alignmentHiBlkSize;

            if ((blkSize < 2 * XMM_REGSIZE_BYTES) || (untrLclLo == alignedLclLo))
            {
                // Either aligned or smaller then 2 x SIMD size so we won't try to align
                // However, we still want to zero anything that is not in a 16 byte chunk at end
                int alignmentBlkSize = blkSize & -XMM_REGSIZE_BYTES;
                alignmentHiBlkSize   = blkSize - alignmentBlkSize;
                alignedLclHi         = untrLclLo + alignmentBlkSize;
                alignedLclLo         = untrLclLo;
                blkSize              = alignmentBlkSize;

                assert((blkSize + alignmentHiBlkSize) == (untrLclHi - untrLclLo));
            }
            else
            {
                // We are going to align

                // Aligning high we want to move down to previous boundary
                alignedLclHi = untrLclHi & -XMM_REGSIZE_BYTES;
                // Zero out the unaligned portions
                alignmentHiBlkSize     = untrLclHi - alignedLclHi;
                int alignmentLoBlkSize = alignedLclLo - untrLclLo;
                blkSize                = alignedLclHi - alignedLclLo;

                assert((blkSize + alignmentLoBlkSize + alignmentHiBlkSize) == (untrLclHi - untrLclLo));

                assert(alignmentLoBlkSize > 0);
                assert(alignmentLoBlkSize < XMM_REGSIZE_BYTES);
                assert((alignedLclLo - alignmentLoBlkSize) == untrLclLo);

                zeroReg = genGetZeroReg(initReg, pInitRegZeroed);

                int i = 0;
                for (; i + REGSIZE_BYTES <= alignmentLoBlkSize; i += REGSIZE_BYTES)
                {
                    emit->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, zeroReg, frameReg, untrLclLo + i);
                }
                assert((i == alignmentLoBlkSize) || (i + (int)sizeof(int) == alignmentLoBlkSize));
                if (i != alignmentLoBlkSize)
                {
                    emit->emitIns_AR_R(ins_Store(TYP_INT), EA_4BYTE, zeroReg, frameReg, untrLclLo + i);
                    i += sizeof(int);
                }

                assert(i == alignmentLoBlkSize);
            }
#else // !defined(TARGET_AMD64)
            // While we aren't aligning the start, we still want to
            // zero anything that is not in a 16 byte chunk at end
            int alignmentBlkSize   = blkSize & -XMM_REGSIZE_BYTES;
            int alignmentHiBlkSize = blkSize - alignmentBlkSize;
            int alignedLclHi       = untrLclLo + alignmentBlkSize;
            blkSize                = alignmentBlkSize;

            assert((blkSize + alignmentHiBlkSize) == (untrLclHi - untrLclLo));
#endif
            // The loop is unrolled 3 times so we do not move to the loop block until it
            // will loop at least once so the threshold is 6.
            if (blkSize < (6 * XMM_REGSIZE_BYTES))
            {
                // Generate the following code:
                //
                //   xorps   xmm4, xmm4
                //   movups  xmmword ptr [ebp/esp-OFFS], xmm4
                //   ...
                //   movups  xmmword ptr [ebp/esp-OFFS], xmm4
                //   mov      qword ptr [ebp/esp-OFFS], rax

                emit->emitIns_R_R(INS_xorps, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, zeroSIMDReg);

                int i = 0;
                for (; i < blkSize; i += XMM_REGSIZE_BYTES)
                {
                    emit->emitIns_AR_R(simdMov, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, frameReg, alignedLclLo + i);
                }

                assert(i == blkSize);
            }
            else
            {
                // Generate the following code:
                //
                //    xorps    xmm4, xmm4
                //    ;movaps xmmword ptr[ebp/esp-loOFFS], xmm4          ; alignment to 3x
                //    ;movaps xmmword ptr[ebp/esp-loOFFS + 10H], xmm4    ;
                //    mov rax, - <size>                                  ; start offset from hi
                //    movaps xmmword ptr[rbp + rax + hiOFFS      ], xmm4 ; <--+
                //    movaps xmmword ptr[rbp + rax + hiOFFS + 10H], xmm4 ;    |
                //    movaps xmmword ptr[rbp + rax + hiOFFS + 20H], xmm4 ;    | Loop
                //    add rax, 48                                        ;    |
                //    jne SHORT  -5 instr                                ; ---+

                emit->emitIns_R_R(INS_xorps, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, zeroSIMDReg);

                // How many extra don't fit into the 3x unroll
                int extraSimd = (blkSize % (XMM_REGSIZE_BYTES * 3)) / XMM_REGSIZE_BYTES;
                if (extraSimd != 0)
                {
                    blkSize -= XMM_REGSIZE_BYTES;
                    // Not a multiple of 3 so add stores at low end of block
                    emit->emitIns_AR_R(simdMov, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, frameReg, alignedLclLo);
                    if (extraSimd == 2)
                    {
                        blkSize -= XMM_REGSIZE_BYTES;
                        // one more store needed
                        emit->emitIns_AR_R(simdMov, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, frameReg,
                                           alignedLclLo + XMM_REGSIZE_BYTES);
                    }
                }

                // Exact multiple of 3 simd lengths (or loop end condition will not be met)
                noway_assert((blkSize % (3 * XMM_REGSIZE_BYTES)) == 0);

                // At least 3 simd lengths remain (as loop is 3x unrolled and we want it to loop at least once)
                assert(blkSize >= (3 * XMM_REGSIZE_BYTES));
                // In range at start of loop
                assert((alignedLclHi - blkSize) >= untrLclLo);
                assert(((alignedLclHi - blkSize) + (XMM_REGSIZE_BYTES * 2)) < (untrLclHi - XMM_REGSIZE_BYTES));
                // In range at end of loop
                assert((alignedLclHi - (3 * XMM_REGSIZE_BYTES) + (2 * XMM_REGSIZE_BYTES)) <=
                       (untrLclHi - XMM_REGSIZE_BYTES));
                assert((alignedLclHi - (blkSize + extraSimd * XMM_REGSIZE_BYTES)) == alignedLclLo);

                // Set loop counter
                emit->emitIns_R_I(INS_mov, EA_PTRSIZE, initReg, -(ssize_t)blkSize);
                // Loop start
                emit->emitIns_ARX_R(simdMov, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, frameReg, initReg, 1,
                                    alignedLclHi);
                emit->emitIns_ARX_R(simdMov, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, frameReg, initReg, 1,
                                    alignedLclHi + XMM_REGSIZE_BYTES);
                emit->emitIns_ARX_R(simdMov, EA_ATTR(XMM_REGSIZE_BYTES), zeroSIMDReg, frameReg, initReg, 1,
                                    alignedLclHi + 2 * XMM_REGSIZE_BYTES);

                emit->emitIns_R_I(INS_add, EA_PTRSIZE, initReg, XMM_REGSIZE_BYTES * 3);
                // Loop until counter is 0
                emit->emitIns_J(INS_jne, nullptr, -5);

                // initReg will be zero at end of the loop
                *pInitRegZeroed = true;
            }

            if (untrLclHi != alignedLclHi)
            {
                assert(alignmentHiBlkSize > 0);
                assert(alignmentHiBlkSize < XMM_REGSIZE_BYTES);
                assert((alignedLclHi + alignmentHiBlkSize) == untrLclHi);

                zeroReg = genGetZeroReg(initReg, pInitRegZeroed);

                int i = 0;
                for (; i + REGSIZE_BYTES <= alignmentHiBlkSize; i += REGSIZE_BYTES)
                {
                    emit->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, zeroReg, frameReg, alignedLclHi + i);
                }
#if defined(TARGET_AMD64)
                assert((i == alignmentHiBlkSize) || (i + (int)sizeof(int) == alignmentHiBlkSize));
                if (i != alignmentHiBlkSize)
                {
                    emit->emitIns_AR_R(ins_Store(TYP_INT), EA_4BYTE, zeroReg, frameReg, alignedLclHi + i);
                    i += sizeof(int);
                }
#endif // defined(TARGET_AMD64)
                assert(i == alignmentHiBlkSize);
            }
        }
#else  // TARGET*
#error Unsupported or unset target architecture
#endif // TARGET*
    }
    else if (genInitStkLclCnt > 0)
    {
        assert((genRegMask(initReg) & intRegState.rsCalleeRegArgMaskLiveIn) == 0); // initReg is not a live incoming
                                                                                   // argument reg

        /* Initialize any lvMustInit vars on the stack */

        LclVarDsc* varDsc;
        unsigned   varNum;

        for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
        {
            if (!varDsc->lvMustInit)
            {
                continue;
            }

            // TODO-Review: I'm not sure that we're correctly handling the mustInit case for
            // partially-enregistered vars in the case where we don't use a block init.
            noway_assert(varDsc->lvIsInReg() || varDsc->lvOnFrame);

            // lvMustInit can only be set for GC types or TYP_STRUCT types
            // or when compInitMem is true
            // or when in debug code

            noway_assert(varTypeIsGC(varDsc->TypeGet()) || (varDsc->TypeGet() == TYP_STRUCT) ||
                         compiler->info.compInitMem || compiler->opts.compDbgCode);

            if (!varDsc->lvOnFrame)
            {
                continue;
            }

            if ((varDsc->TypeGet() == TYP_STRUCT) && !compiler->info.compInitMem &&
                (varDsc->lvExactSize >= TARGET_POINTER_SIZE))
            {
                // We only initialize the GC variables in the TYP_STRUCT
                const unsigned slots  = (unsigned)compiler->lvaLclSize(varNum) / REGSIZE_BYTES;
                ClassLayout*   layout = varDsc->GetLayout();

                for (unsigned i = 0; i < slots; i++)
                {
                    if (layout->IsGCPtr(i))
                    {
                        GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE,
                                                  genGetZeroReg(initReg, pInitRegZeroed), varNum, i * REGSIZE_BYTES);
                    }
                }
            }
            else
            {
                regNumber zeroReg = genGetZeroReg(initReg, pInitRegZeroed);

                // zero out the whole thing rounded up to a single stack slot size
                unsigned lclSize = roundUp(compiler->lvaLclSize(varNum), (unsigned)sizeof(int));
                unsigned i;
                for (i = 0; i + REGSIZE_BYTES <= lclSize; i += REGSIZE_BYTES)
                {
                    GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, zeroReg, varNum, i);
                }

#ifdef TARGET_64BIT
                assert(i == lclSize || (i + sizeof(int) == lclSize));
                if (i != lclSize)
                {
                    GetEmitter()->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, zeroReg, varNum, i);
                    i += sizeof(int);
                }
#endif // TARGET_64BIT
                assert(i == lclSize);
            }
        }

        assert(regSet.tmpAllFree());
        for (TempDsc* tempThis = regSet.tmpListBeg(); tempThis != nullptr; tempThis = regSet.tmpListNxt(tempThis))
        {
            if (!varTypeIsGC(tempThis->tdTempType()))
            {
                continue;
            }

            // printf("initialize untracked spillTmp [EBP-%04X]\n", stkOffs);

            inst_ST_RV(ins_Store(TYP_I_IMPL), tempThis, 0, genGetZeroReg(initReg, pInitRegZeroed), TYP_I_IMPL);
        }
    }

    // Initialize args and locals for OSR. Note this may include promoted fields.
    if (compiler->opts.IsOSR())
    {
        PatchpointInfo* patchpointInfo = compiler->info.compPatchpointInfo;

        // basic sanity checks (make sure we're OSRing the right method)
        assert(patchpointInfo->NumberOfLocals() == compiler->info.compLocalsCount);

        const int      originalFrameSize = patchpointInfo->FpToSpDelta();
        const unsigned patchpointInfoLen = patchpointInfo->NumberOfLocals();

        for (unsigned varNum = 0; varNum < compiler->lvaCount; varNum++)
        {
            if (!compiler->lvaIsOSRLocal(varNum))
            {
                continue;
            }

            LclVarDsc* const varDsc = compiler->lvaGetDesc(varNum);

            if (!varDsc->lvIsInReg())
            {
                JITDUMP("---OSR--- V%02u in memory\n", varNum);
                continue;
            }

            if (!VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
            {
                JITDUMP("---OSR--- V%02u (reg) not live at entry\n", varNum);
                continue;
            }

            int      fieldOffset = 0;
            unsigned lclNum      = varNum;

            if (varDsc->lvIsStructField)
            {
                lclNum = varDsc->lvParentLcl;
                assert(lclNum < patchpointInfoLen);

                fieldOffset = varDsc->lvFldOffset;
                JITDUMP("---OSR--- V%02u is promoted field of V%02u at offset %d\n", varNum, lclNum, fieldOffset);
            }

            // Note we are always reading from the original frame here
            const var_types lclTyp  = genActualType(varDsc->lvType);
            const emitAttr  size    = emitTypeSize(lclTyp);
            const int       stkOffs = patchpointInfo->Offset(lclNum) + fieldOffset;

            // Original frames always use frame pointers, so
            // stkOffs is the original frame-relative offset
            // to the variable.
            //
            // We need to determine the stack or frame-pointer relative
            // offset for this variable in the current frame.
            //
            // If current frame does not use a frame pointer, we need to
            // add the SP-to-FP delta of this frame and the SP-to-FP delta
            // of the original frame; that translates from this frame's
            // stack pointer the old frame frame pointer.
            //
            // We then add the original frame's frame-pointer relative
            // offset (note this offset is usually negative -- the stack
            // grows down, so locals are below the frame pointer).
            //
            // /-----original frame-----/
            // / return address         /
            // / saved RBP   --+        /  <--- Original frame ptr   --+
            // / ...           |        /                              |
            // / ...       (stkOffs)    /                              |
            // / ...           |        /                              |
            // / variable    --+        /                              |
            // / ...                    /                (original frame sp-fp delta)
            // / ...                    /                              |
            // /-----OSR frame ---------/                              |
            // / pseudo return address  /                            --+
            // / ...                    /                              |
            // / ...                    /                    (this frame sp-fp delta)
            // / ...                    /                              |
            // /------------------------/  <--- Stack ptr            --+
            //
            // If the current frame is using a frame pointer, we need to
            // add the SP-to-FP delta of/ the original frame and then add
            // the original frame's frame-pointer relative offset.
            //
            // /-----original frame-----/
            // / return address         /
            // / saved RBP   --+        /  <--- Original frame ptr   --+
            // / ...           |        /                              |
            // / ...       (stkOffs)    /                              |
            // / ...           |        /                              |
            // / variable    --+        /                              |
            // / ...                    /                (original frame sp-fp delta)
            // / ...                    /                              |
            // /-----OSR frame ---------/                              |
            // / pseudo return address  /                            --+
            // / saved RBP              /  <--- Frame ptr            --+
            // / ...                    /
            // / ...                    /
            // / ...                    /
            // /------------------------/

            int offset = originalFrameSize + stkOffs;

            if (isFramePointerUsed())
            {
                // also adjust for saved RPB on this frame
                offset += TARGET_POINTER_SIZE;
            }
            else
            {
                offset += genSPtoFPdelta();
            }

            JITDUMP("---OSR--- V%02u (reg) old rbp offset %d old frame %d this frame sp-fp %d new offset %d (%02xH)\n",
                    varNum, stkOffs, originalFrameSize, genSPtoFPdelta(), offset, offset);

            GetEmitter()->emitIns_R_AR(ins_Load(lclTyp), size, varDsc->GetRegNum(), genFramePointerReg(), offset);
        }
    }
}

/*-----------------------------------------------------------------------------
 *
 *  Save the generic context argument.
 *
 *  We need to do this within the "prolog" in case anyone tries to inspect
 *  the param-type-arg/this (which can be done after the prolog) using
 *  ICodeManager::GetParamTypeArg().
 */

void CodeGen::genReportGenericContextArg(regNumber initReg, bool* pInitRegZeroed)
{
    // For OSR the original method has set this up for us.
    if (compiler->opts.IsOSR())
    {
        return;
    }

    assert(compiler->compGeneratingProlog);

    bool reportArg = compiler->lvaReportParamTypeArg();

    // We should report either generic context arg or "this" when used so.
    if (!reportArg)
    {
#ifndef JIT32_GCENCODER
        if (!compiler->lvaKeepAliveAndReportThis())
#endif
        {
            return;
        }
    }

    // For JIT32_GCENCODER, we won't be here if reportArg is false.
    unsigned contextArg = reportArg ? compiler->info.compTypeCtxtArg : compiler->info.compThisArg;

    noway_assert(contextArg != BAD_VAR_NUM);
    LclVarDsc* varDsc = &compiler->lvaTable[contextArg];

    // We are still in the prolog and compiler->info.compTypeCtxtArg has not been
    // moved to its final home location. So we need to use it from the
    // incoming location.

    regNumber reg;

    bool isPrespilledForProfiling = false;
#if defined(TARGET_ARM) && defined(PROFILING_SUPPORTED)
    isPrespilledForProfiling =
        compiler->compIsProfilerHookNeeded() && compiler->lvaIsPreSpilled(contextArg, regSet.rsMaskPreSpillRegs(false));
#endif

    // Load from the argument register only if it is not prespilled.
    if (compiler->lvaIsRegArgument(contextArg) && !isPrespilledForProfiling)
    {
        reg = varDsc->GetArgReg();
    }
    else
    {
        if (isFramePointerUsed())
        {
#if defined(TARGET_ARM)
            // GetStackOffset() is always valid for incoming stack-arguments, even if the argument
            // will become enregistered.
            // On Arm compiler->compArgSize doesn't include r11 and lr sizes and hence we need to add 2*REGSIZE_BYTES
            noway_assert((2 * REGSIZE_BYTES <= varDsc->GetStackOffset()) &&
                         (size_t(varDsc->GetStackOffset()) < compiler->compArgSize + 2 * REGSIZE_BYTES));
#else
            // GetStackOffset() is always valid for incoming stack-arguments, even if the argument
            // will become enregistered.
            noway_assert((0 < varDsc->GetStackOffset()) && (size_t(varDsc->GetStackOffset()) < compiler->compArgSize));
#endif
        }

        // We will just use the initReg since it is an available register
        // and we are probably done using it anyway...
        reg             = initReg;
        *pInitRegZeroed = false;

        // mov reg, [compiler->info.compTypeCtxtArg]
        GetEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, reg, genFramePointerReg(),
                                   varDsc->GetStackOffset());
        regSet.verifyRegUsed(reg);
    }

#if defined(TARGET_ARM64)
    genInstrWithConstant(ins_Store(TYP_I_IMPL), EA_PTRSIZE, reg, genFramePointerReg(),
                         compiler->lvaCachedGenericContextArgOffset(), rsGetRsvdReg());
#elif defined(TARGET_ARM)
    // ARM's emitIns_R_R_I automatically uses the reserved register if necessary.
    GetEmitter()->emitIns_R_R_I(ins_Store(TYP_I_IMPL), EA_PTRSIZE, reg, genFramePointerReg(),
                                compiler->lvaCachedGenericContextArgOffset());
#else  // !ARM64 !ARM
    // mov [ebp-lvaCachedGenericContextArgOffset()], reg
    GetEmitter()->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, reg, genFramePointerReg(),
                               compiler->lvaCachedGenericContextArgOffset());
#endif // !ARM64 !ARM
}

/*****************************************************************************

Esp frames :
----------

These instructions are just a reordering of the instructions used today.

push ebp
push esi
push edi
push ebx
sub esp, LOCALS_SIZE / push dummyReg if LOCALS_SIZE=sizeof(void*)
...
add esp, LOCALS_SIZE / pop dummyReg
pop ebx
pop edi
pop esi
pop ebp
ret

Ebp frames :
----------

The epilog does "add esp, LOCALS_SIZE" instead of "mov ebp, esp".
Everything else is similar, though in a different order.

The security object will no longer be at a fixed offset. However, the
offset can still be determined by looking up the GC-info and determining
how many callee-saved registers are pushed.

push ebp
mov ebp, esp
push esi
push edi
push ebx
sub esp, LOCALS_SIZE / push dummyReg if LOCALS_SIZE=sizeof(void*)
...
add esp, LOCALS_SIZE / pop dummyReg
pop ebx
pop edi
pop esi
(mov esp, ebp if there are no callee-saved registers)
pop ebp
ret

Double-aligned frame :
--------------------

LOCALS_SIZE_ADJUSTED needs to include an unused DWORD if an odd number
of callee-saved registers are pushed on the stack so that the locals
themselves are qword-aligned. The instructions are the same as today,
just in a different order.

push ebp
mov ebp, esp
and esp, 0xFFFFFFFC
push esi
push edi
push ebx
sub esp, LOCALS_SIZE_ADJUSTED / push dummyReg if LOCALS_SIZE=sizeof(void*)
...
add esp, LOCALS_SIZE_ADJUSTED / pop dummyReg
pop ebx
pop edi
pop esi
pop ebp
mov esp, ebp
pop ebp
ret

localloc (with ebp) frames :
--------------------------

The instructions are the same as today, just in a different order.
Also, today the epilog does "lea esp, [ebp-LOCALS_SIZE-calleeSavedRegsPushedSize]"
which will change to "lea esp, [ebp-calleeSavedRegsPushedSize]".

push ebp
mov ebp, esp
push esi
push edi
push ebx
sub esp, LOCALS_SIZE / push dummyReg if LOCALS_SIZE=sizeof(void*)
...
lea esp, [ebp-calleeSavedRegsPushedSize]
pop ebx
pop edi
pop esi
(mov esp, ebp if there are no callee-saved registers)
pop ebp
ret

*****************************************************************************/

/*****************************************************************************
 *
 *  Reserve space for a function prolog.
 */

void CodeGen::genReserveProlog(BasicBlock* block)
{
    assert(block != nullptr);

    JITDUMP("Reserving prolog IG for block " FMT_BB "\n", block->bbNum);

    /* Nothing is live on entry to the prolog */

    GetEmitter()->emitCreatePlaceholderIG(IGPT_PROLOG, block, VarSetOps::MakeEmpty(compiler), 0, 0, false);
}

/*****************************************************************************
 *
 *  Reserve space for a function epilog.
 */

void CodeGen::genReserveEpilog(BasicBlock* block)
{
    regMaskTP gcrefRegsArg = gcInfo.gcRegGCrefSetCur;
    regMaskTP byrefRegsArg = gcInfo.gcRegByrefSetCur;

    /* The return value is special-cased: make sure it goes live for the epilog */

    bool jmpEpilog = ((block->bbFlags & BBF_HAS_JMP) != 0);

    if (IsFullPtrRegMapRequired() && !jmpEpilog)
    {
        if (varTypeIsGC(compiler->info.compRetNativeType))
        {
            noway_assert(genTypeStSz(compiler->info.compRetNativeType) == genTypeStSz(TYP_I_IMPL));

            gcInfo.gcMarkRegPtrVal(REG_INTRET, compiler->info.compRetNativeType);

            switch (compiler->info.compRetNativeType)
            {
                case TYP_REF:
                    gcrefRegsArg |= RBM_INTRET;
                    break;
                case TYP_BYREF:
                    byrefRegsArg |= RBM_INTRET;
                    break;
                default:
                    break;
            }

            JITDUMP("Extending return value GC liveness to epilog\n");
        }
    }

    JITDUMP("Reserving epilog IG for block " FMT_BB "\n", block->bbNum);

    assert(block != nullptr);
    const VARSET_TP& gcrefVarsArg(GetEmitter()->emitThisGCrefVars);
    bool             last = (block->bbNext == nullptr);
    GetEmitter()->emitCreatePlaceholderIG(IGPT_EPILOG, block, gcrefVarsArg, gcrefRegsArg, byrefRegsArg, last);
}

#if defined(FEATURE_EH_FUNCLETS)

/*****************************************************************************
 *
 *  Reserve space for a funclet prolog.
 */

void CodeGen::genReserveFuncletProlog(BasicBlock* block)
{
    assert(block != nullptr);

    /* Currently, no registers are live on entry to the prolog, except maybe
       the exception object. There might be some live stack vars, but they
       cannot be accessed until after the frame pointer is re-established.
       In order to potentially prevent emitting a death before the prolog
       and a birth right after it, we just report it as live during the
       prolog, and rely on the prolog being non-interruptible. Trust
       genCodeForBBlist to correctly initialize all the sets.

       We might need to relax these asserts if the VM ever starts
       restoring any registers, then we could have live-in reg vars...
    */

    noway_assert((gcInfo.gcRegGCrefSetCur & RBM_EXCEPTION_OBJECT) == gcInfo.gcRegGCrefSetCur);
    noway_assert(gcInfo.gcRegByrefSetCur == 0);

    JITDUMP("Reserving funclet prolog IG for block " FMT_BB "\n", block->bbNum);

    GetEmitter()->emitCreatePlaceholderIG(IGPT_FUNCLET_PROLOG, block, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                          gcInfo.gcRegByrefSetCur, false);
}

/*****************************************************************************
 *
 *  Reserve space for a funclet epilog.
 */

void CodeGen::genReserveFuncletEpilog(BasicBlock* block)
{
    assert(block != nullptr);

    JITDUMP("Reserving funclet epilog IG for block " FMT_BB "\n", block->bbNum);

    bool last = (block->bbNext == nullptr);
    GetEmitter()->emitCreatePlaceholderIG(IGPT_FUNCLET_EPILOG, block, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                          gcInfo.gcRegByrefSetCur, last);
}

#endif // FEATURE_EH_FUNCLETS

/*****************************************************************************
 *  Finalize the frame size and offset assignments.
 *
 *  No changes can be made to the modified register set after this, since that can affect how many
 *  callee-saved registers get saved.
 */
void CodeGen::genFinalizeFrame()
{
    JITDUMP("Finalizing stack frame\n");

    // Initializations need to happen based on the var locations at the start
    // of the first basic block, so load those up. In particular, the determination
    // of whether or not to use block init in the prolog is dependent on the variable
    // locations on entry to the function.
    compiler->m_pLinearScan->recordVarLocationsAtStartOfBB(compiler->fgFirstBB);

    genCheckUseBlockInit();

    // Set various registers as "modified" for special code generation scenarios: Edit & Continue, P/Invoke calls, etc.
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(TARGET_X86)

    if (compiler->compTailCallUsed)
    {
        // If we are generating a helper-based tailcall, we've set the tailcall helper "flags"
        // argument to "1", indicating to the tailcall helper that we've saved the callee-saved
        // registers (ebx, esi, edi). So, we need to make sure all the callee-saved registers
        // actually get saved.

        regSet.rsSetRegsModified(RBM_INT_CALLEE_SAVED);
    }
#endif // TARGET_X86

#ifdef TARGET_ARM
    // Make sure that callee-saved registers used by call to a stack probing helper generated are pushed on stack.
    if (compiler->compLclFrameSize >= compiler->eeGetPageSize())
    {
        regSet.rsSetRegsModified(RBM_STACK_PROBE_HELPER_ARG | RBM_STACK_PROBE_HELPER_CALL_TARGET |
                                 RBM_STACK_PROBE_HELPER_TRASH);
    }

    // If there are any reserved registers, add them to the modified set.
    if (regSet.rsMaskResvd != RBM_NONE)
    {
        regSet.rsSetRegsModified(regSet.rsMaskResvd);
    }
#endif // TARGET_ARM

#ifdef DEBUG
    if (verbose)
    {
        printf("Modified regs: ");
        dspRegMask(regSet.rsGetModifiedRegsMask());
        printf("\n");
    }
#endif // DEBUG

    // Set various registers as "modified" for special code generation scenarios: Edit & Continue, P/Invoke calls, etc.
    if (compiler->opts.compDbgEnC)
    {
        // We always save FP.
        noway_assert(isFramePointerUsed());
#ifdef TARGET_AMD64
        // On x64 we always save exactly RBP, RSI and RDI for EnC.
        regMaskTP okRegs = (RBM_CALLEE_TRASH | RBM_FPBASE | RBM_RSI | RBM_RDI);
        regSet.rsSetRegsModified(RBM_RSI | RBM_RDI);
        noway_assert((regSet.rsGetModifiedRegsMask() & ~okRegs) == 0);
#else  // !TARGET_AMD64
        // On x86 we save all callee saved regs so the saved reg area size is consistent
        regSet.rsSetRegsModified(RBM_INT_CALLEE_SAVED & ~RBM_FPBASE);
#endif // !TARGET_AMD64
    }

    /* If we have any pinvoke calls, we might potentially trash everything */
    if (compiler->compMethodRequiresPInvokeFrame())
    {
        noway_assert(isFramePointerUsed()); // Setup of Pinvoke frame currently requires an EBP style frame
        regSet.rsSetRegsModified(RBM_INT_CALLEE_SAVED & ~RBM_FPBASE);
    }

#ifdef UNIX_AMD64_ABI
    // On Unix x64 we also save R14 and R15 for ELT profiler hook generation.
    if (compiler->compIsProfilerHookNeeded())
    {
        regSet.rsSetRegsModified(RBM_PROFILER_ENTER_ARG_0 | RBM_PROFILER_ENTER_ARG_1);
    }
#endif

    /* Count how many callee-saved registers will actually be saved (pushed) */

    // EBP cannot be (directly) modified for EBP frame and double-aligned frames
    noway_assert(!doubleAlignOrFramePointerUsed() || !regSet.rsRegsModified(RBM_FPBASE));

#if ETW_EBP_FRAMED
    // EBP cannot be (directly) modified
    noway_assert(!regSet.rsRegsModified(RBM_FPBASE));
#endif

    regMaskTP maskCalleeRegsPushed = regSet.rsGetModifiedRegsMask() & RBM_CALLEE_SAVED;

#ifdef TARGET_ARMARCH
    if (isFramePointerUsed())
    {
        // For a FP based frame we have to push/pop the FP register
        //
        maskCalleeRegsPushed |= RBM_FPBASE;

        // This assert check that we are not using REG_FP
        // as both the frame pointer and as a codegen register
        //
        assert(!regSet.rsRegsModified(RBM_FPBASE));
    }

    // we always push LR.  See genPushCalleeSavedRegisters
    //
    maskCalleeRegsPushed |= RBM_LR;

#if defined(TARGET_ARM)
    // TODO-ARM64-Bug?: enable some variant of this for FP on ARM64?
    regMaskTP maskPushRegsFloat = maskCalleeRegsPushed & RBM_ALLFLOAT;
    regMaskTP maskPushRegsInt   = maskCalleeRegsPushed & ~maskPushRegsFloat;

    if ((maskPushRegsFloat != RBM_NONE) ||
        (compiler->opts.MinOpts() && (regSet.rsMaskResvd & maskCalleeRegsPushed & RBM_OPT_RSVD)))
    {
        // Here we try to keep stack double-aligned before the vpush
        if ((genCountBits(regSet.rsMaskPreSpillRegs(true) | maskPushRegsInt) % 2) != 0)
        {
            regNumber extraPushedReg = REG_R4;
            while (maskPushRegsInt & genRegMask(extraPushedReg))
            {
                extraPushedReg = REG_NEXT(extraPushedReg);
            }
            if (extraPushedReg < REG_R11)
            {
                maskPushRegsInt |= genRegMask(extraPushedReg);
                regSet.rsSetRegsModified(genRegMask(extraPushedReg));
            }
        }
        maskCalleeRegsPushed = maskPushRegsInt | maskPushRegsFloat;
    }

    // We currently only expect to push/pop consecutive FP registers
    // and these have to be double-sized registers as well.
    // Here we will insure that maskPushRegsFloat obeys these requirements.
    //
    if (maskPushRegsFloat != RBM_NONE)
    {
        regMaskTP contiguousMask = genRegMaskFloat(REG_F16, TYP_DOUBLE);
        while (maskPushRegsFloat > contiguousMask)
        {
            contiguousMask <<= 2;
            contiguousMask |= genRegMaskFloat(REG_F16, TYP_DOUBLE);
        }
        if (maskPushRegsFloat != contiguousMask)
        {
            regMaskTP maskExtraRegs = contiguousMask - maskPushRegsFloat;
            maskPushRegsFloat |= maskExtraRegs;
            regSet.rsSetRegsModified(maskExtraRegs);
            maskCalleeRegsPushed |= maskExtraRegs;
        }
    }
#endif // TARGET_ARM
#endif // TARGET_ARMARCH

#if defined(TARGET_XARCH)
    // Compute the count of callee saved float regs saved on stack.
    // On Amd64 we push only integer regs. Callee saved float (xmm6-xmm15)
    // regs are stack allocated and preserved in their stack locations.
    compiler->compCalleeFPRegsSavedMask = maskCalleeRegsPushed & RBM_FLT_CALLEE_SAVED;
    maskCalleeRegsPushed &= ~RBM_FLT_CALLEE_SAVED;
#endif // defined(TARGET_XARCH)

    compiler->compCalleeRegsPushed = genCountBits(maskCalleeRegsPushed);

#ifdef DEBUG
    if (verbose)
    {
        printf("Callee-saved registers pushed: %d ", compiler->compCalleeRegsPushed);
        dspRegMask(maskCalleeRegsPushed);
        printf("\n");
    }
#endif // DEBUG

    /* Assign the final offsets to things living on the stack frame */

    compiler->lvaAssignFrameOffsets(Compiler::FINAL_FRAME_LAYOUT);

    /* We want to make sure that the prolog size calculated here is accurate
       (that is instructions will not shrink because of conservative stack
       frame approximations).  We do this by filling in the correct size
       here (where we have committed to the final numbers for the frame offsets)
       This will ensure that the prolog size is always correct
    */
    GetEmitter()->emitMaxTmpSize = regSet.tmpGetTotalSize();

#ifdef DEBUG
    if (compiler->opts.dspCode || compiler->opts.disAsm || compiler->opts.disAsm2 || verbose)
    {
        compiler->lvaTableDump();
    }
#endif
}

//------------------------------------------------------------------------
// genEstablishFramePointer: Set up the frame pointer by adding an offset to the stack pointer.
//
// Arguments:
//    delta - the offset to add to the current stack pointer to establish the frame pointer
//    reportUnwindData - true if establishing the frame pointer should be reported in the OS unwind data.

void CodeGen::genEstablishFramePointer(int delta, bool reportUnwindData)
{
    assert(compiler->compGeneratingProlog);

#if defined(TARGET_XARCH)

    if (delta == 0)
    {
        GetEmitter()->emitIns_R_R(INS_mov, EA_PTRSIZE, REG_FPBASE, REG_SPBASE);
#ifdef USING_SCOPE_INFO
        psiMoveESPtoEBP();
#endif // USING_SCOPE_INFO
    }
    else
    {
        GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, delta);
        // We don't update prolog scope info (there is no function to handle lea), but that is currently dead code
        // anyway.
    }

    if (reportUnwindData)
    {
        compiler->unwindSetFrameReg(REG_FPBASE, delta);
    }

#elif defined(TARGET_ARM)

    assert(arm_Valid_Imm_For_Add_SP(delta));
    GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, delta);

    if (reportUnwindData)
    {
        compiler->unwindPadding();
    }

#elif defined(TARGET_ARM64)

    if (delta == 0)
    {
        GetEmitter()->emitIns_R_R(INS_mov, EA_PTRSIZE, REG_FPBASE, REG_SPBASE);
    }
    else
    {
        GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, delta);
    }

    if (reportUnwindData)
    {
        compiler->unwindSetFrameReg(REG_FPBASE, delta);
    }

#else
    NYI("establish frame pointer");
#endif
}

/*****************************************************************************
 *
 *  Generates code for a function prolog.
 *
 *  NOTE REGARDING CHANGES THAT IMPACT THE DEBUGGER:
 *
 *  The debugger relies on decoding ARM instructions to be able to successfully step through code. It does not
 *  implement decoding all ARM instructions. It only implements decoding the instructions which the JIT emits, and
 *  only instructions which result in control not going to the next instruction. Basically, any time execution would
 *  not continue at the next instruction (such as B, BL, BX, BLX, POP{pc}, etc.), the debugger has to be able to
 *  decode that instruction. If any of this is changed on ARM, the debugger team needs to be notified so that it
 *  can ensure stepping isn't broken. This is also a requirement for x86 and amd64.
 *
 *  If any changes are made in the prolog, epilog, calls, returns, and branches, it is a good idea to notify the
 *  debugger team to ensure that stepping still works.
 *
 *  ARM stepping code is here: debug\ee\arm\armwalker.cpp, vm\arm\armsinglestepper.cpp.
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
void CodeGen::genFnProlog()
{
    ScopedSetVariable<bool> _setGeneratingProlog(&compiler->compGeneratingProlog, true);

    compiler->funSetCurrentFunc(0);

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFnProlog()\n");
    }
#endif

#ifdef DEBUG
    genInterruptibleUsed = true;
#endif

    assert(compiler->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT);

    /* Ready to start on the prolog proper */

    GetEmitter()->emitBegProlog();
    compiler->unwindBegProlog();

    // Do this so we can put the prolog instruction group ahead of
    // other instruction groups
    genIPmappingAddToFront((IL_OFFSETX)ICorDebugInfo::PROLOG);

#ifdef DEBUG
    if (compiler->opts.dspCode)
    {
        printf("\n__prolog:\n");
    }
#endif

    if (compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0))
    {
        // Create new scopes for the method-parameters for the prolog-block.
        psiBegProlog();
    }

#if defined(TARGET_XARCH)
    // For OSR there is a "phantom prolog" to account for the actions taken
    // in the original frame that impact RBP and RSP on entry to the OSR method.
    if (compiler->opts.IsOSR())
    {
        PatchpointInfo* patchpointInfo    = compiler->info.compPatchpointInfo;
        const int       originalFrameSize = patchpointInfo->FpToSpDelta();

        compiler->unwindPush(REG_FPBASE);
        compiler->unwindAllocStack(originalFrameSize);
    }
#endif

#ifdef DEBUG

    if (compiler->compJitHaltMethod())
    {
        /* put a nop first because the debugger and other tools are likely to
           put an int3 at the beginning and we don't want to confuse them */

        instGen(INS_nop);
        instGen(INS_BREAKPOINT);

#ifdef TARGET_ARMARCH
        // Avoid asserts in the unwind info because these instructions aren't accounted for.
        compiler->unwindPadding();
#endif // TARGET_ARMARCH
    }
#endif // DEBUG

#if defined(FEATURE_EH_FUNCLETS) && defined(DEBUG)

    // We cannot force 0-initialization of the PSPSym
    // as it will overwrite the real value
    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        LclVarDsc* varDsc = &compiler->lvaTable[compiler->lvaPSPSym];
        assert(!varDsc->lvMustInit);
    }

#endif // FEATURE_EH_FUNCLETS && DEBUG

    /*-------------------------------------------------------------------------
     *
     *  Record the stack frame ranges that will cover all of the tracked
     *  and untracked pointer variables.
     *  Also find which registers will need to be zero-initialized.
     *
     *  'initRegs': - Generally, enregistered variables should not need to be
     *                zero-inited. They only need to be zero-inited when they
     *                have a possibly uninitialized read on some control
     *                flow path. Apparently some of the IL_STUBs that we
     *                generate have this property.
     */

    int untrLclLo = +INT_MAX;
    int untrLclHi = -INT_MAX;
    // 'hasUntrLcl' is true if there are any stack locals which must be init'ed.
    // Note that they may be tracked, but simply not allocated to a register.
    bool hasUntrLcl = false;

    int  GCrefLo  = +INT_MAX;
    int  GCrefHi  = -INT_MAX;
    bool hasGCRef = false;

    regMaskTP initRegs    = RBM_NONE; // Registers which must be init'ed.
    regMaskTP initFltRegs = RBM_NONE; // FP registers which must be init'ed.
    regMaskTP initDblRegs = RBM_NONE;

    unsigned   varNum;
    LclVarDsc* varDsc;

    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        if (varDsc->lvIsParam && !varDsc->lvIsRegArg)
        {
            continue;
        }

        if (!varDsc->lvIsInReg() && !varDsc->lvOnFrame)
        {
            noway_assert(varDsc->lvRefCnt() == 0);
            continue;
        }

        signed int loOffs = varDsc->GetStackOffset();
        signed int hiOffs = varDsc->GetStackOffset() + compiler->lvaLclSize(varNum);

        /* We need to know the offset range of tracked stack GC refs */
        /* We assume that the GC reference can be anywhere in the TYP_STRUCT */

        if (varDsc->HasGCPtr() && varDsc->lvTrackedNonStruct() && varDsc->lvOnFrame)
        {
            // For fields of PROMOTION_TYPE_DEPENDENT type of promotion, they should have been
            // taken care of by the parent struct.
            if (!compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc))
            {
                hasGCRef = true;

                if (loOffs < GCrefLo)
                {
                    GCrefLo = loOffs;
                }
                if (hiOffs > GCrefHi)
                {
                    GCrefHi = hiOffs;
                }
            }
        }

        /* For lvMustInit vars, gather pertinent info */

        if (!varDsc->lvMustInit)
        {
            continue;
        }

        bool isInReg    = varDsc->lvIsInReg();
        bool isInMemory = !isInReg || varDsc->lvLiveInOutOfHndlr;

        // Note that 'lvIsInReg()' will only be accurate for variables that are actually live-in to
        // the first block. This will include all possibly-uninitialized locals, whose liveness
        // will naturally propagate up to the entry block. However, we also set 'lvMustInit' for
        // locals that are live-in to a finally block, and those may not be live-in to the first
        // block. For those, we don't want to initialize the register, as it will not actually be
        // occupying it on entry.
        if (isInReg)
        {
            if (compiler->lvaEnregEHVars && varDsc->lvLiveInOutOfHndlr)
            {
                isInReg = VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex);
            }
            else
            {
                assert(VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex));
            }
        }

        if (isInReg)
        {
            regMaskTP regMask = genRegMask(varDsc->GetRegNum());
            if (!varDsc->IsFloatRegType())
            {
                initRegs |= regMask;

                if (varTypeIsMultiReg(varDsc))
                {
                    if (varDsc->GetOtherReg() != REG_STK)
                    {
                        initRegs |= genRegMask(varDsc->GetOtherReg());
                    }
                    else
                    {
                        /* Upper DWORD is on the stack, and needs to be inited */

                        loOffs += sizeof(int);
                        goto INIT_STK;
                    }
                }
            }
            else if (varDsc->TypeGet() == TYP_DOUBLE)
            {
                initDblRegs |= regMask;
            }
            else
            {
                initFltRegs |= regMask;
            }
        }
        if (isInMemory)
        {
        INIT_STK:

            hasUntrLcl = true;

            if (loOffs < untrLclLo)
            {
                untrLclLo = loOffs;
            }
            if (hiOffs > untrLclHi)
            {
                untrLclHi = hiOffs;
            }
        }
    }

    /* Don't forget about spill temps that hold pointers */

    assert(regSet.tmpAllFree());
    for (TempDsc* tempThis = regSet.tmpListBeg(); tempThis != nullptr; tempThis = regSet.tmpListNxt(tempThis))
    {
        if (!varTypeIsGC(tempThis->tdTempType()))
        {
            continue;
        }

        signed int loOffs = tempThis->tdTempOffs();
        signed int hiOffs = loOffs + TARGET_POINTER_SIZE;

        // If there is a frame pointer used, due to frame pointer chaining it will point to the stored value of the
        // previous frame pointer. Thus, stkOffs can't be zero.
        CLANG_FORMAT_COMMENT_ANCHOR;

#if !defined(TARGET_AMD64)
        // However, on amd64 there is no requirement to chain frame pointers.

        noway_assert(!isFramePointerUsed() || loOffs != 0);
#endif // !defined(TARGET_AMD64)

        // printf("    Untracked tmp at [EBP-%04X]\n", -stkOffs);

        hasUntrLcl = true;

        if (loOffs < untrLclLo)
        {
            untrLclLo = loOffs;
        }
        if (hiOffs > untrLclHi)
        {
            untrLclHi = hiOffs;
        }
    }

    // TODO-Cleanup: Add suitable assert for the OSR case.
    assert(compiler->opts.IsOSR() || ((genInitStkLclCnt > 0) == hasUntrLcl));

#ifdef DEBUG
    if (verbose)
    {
        if (genInitStkLclCnt > 0)
        {
            printf("Found %u lvMustInit int-sized stack slots, frame offsets %d through %d\n", genInitStkLclCnt,
                   -untrLclLo, -untrLclHi);
        }
    }
#endif

#ifdef TARGET_ARM
    // On the ARM we will spill any incoming struct args in the first instruction in the prolog
    // Ditto for all enregistered user arguments in a varargs method.
    // These registers will be available to use for the initReg.  We just remove
    // all of these registers from the rsCalleeRegArgMaskLiveIn.
    //
    intRegState.rsCalleeRegArgMaskLiveIn &= ~regSet.rsMaskPreSpillRegs(false);
#endif

    /* Choose the register to use for zero initialization */

    regNumber initReg = REG_SCRATCH; // Unless we find a better register below

    // Track if initReg holds non-zero value. Start conservative and assume it has non-zero value.
    // If initReg is ever set to zero, this variable is set to true and zero initializing initReg
    // will be skipped.
    bool      initRegZeroed = false;
    regMaskTP excludeMask   = intRegState.rsCalleeRegArgMaskLiveIn;
    regMaskTP tempMask;

    // We should not use the special PINVOKE registers as the initReg
    // since they are trashed by the jithelper call to setup the PINVOKE frame
    if (compiler->compMethodRequiresPInvokeFrame())
    {
        excludeMask |= RBM_PINVOKE_FRAME;

        assert((!compiler->opts.ShouldUsePInvokeHelpers()) || (compiler->info.compLvFrameListRoot == BAD_VAR_NUM));
        if (!compiler->opts.ShouldUsePInvokeHelpers())
        {
            noway_assert(compiler->info.compLvFrameListRoot < compiler->lvaCount);

            excludeMask |= (RBM_PINVOKE_TCB | RBM_PINVOKE_SCRATCH);

            // We also must exclude the register used by compLvFrameListRoot when it is enregistered
            //
            LclVarDsc* varDsc = &compiler->lvaTable[compiler->info.compLvFrameListRoot];
            if (varDsc->lvRegister)
            {
                excludeMask |= genRegMask(varDsc->GetRegNum());
            }
        }
    }

#ifdef TARGET_ARM
    // If we have a variable sized frame (compLocallocUsed is true)
    // then using REG_SAVED_LOCALLOC_SP in the prolog is not allowed
    if (compiler->compLocallocUsed)
    {
        excludeMask |= RBM_SAVED_LOCALLOC_SP;
    }
#endif // TARGET_ARM

    tempMask = initRegs & ~excludeMask & ~regSet.rsMaskResvd;

    if (tempMask != RBM_NONE)
    {
        // We will use one of the registers that we were planning to zero init anyway.
        // We pick the lowest register number.
        tempMask = genFindLowestBit(tempMask);
        initReg  = genRegNumFromMask(tempMask);
    }
    // Next we prefer to use one of the unused argument registers.
    // If they aren't available we use one of the caller-saved integer registers.
    else
    {
        tempMask = regSet.rsGetModifiedRegsMask() & RBM_ALLINT & ~excludeMask & ~regSet.rsMaskResvd;
        if (tempMask != RBM_NONE)
        {
            // We pick the lowest register number
            tempMask = genFindLowestBit(tempMask);
            initReg  = genRegNumFromMask(tempMask);
        }
    }

    noway_assert(!compiler->compMethodRequiresPInvokeFrame() || (initReg != REG_PINVOKE_FRAME));

#if defined(TARGET_AMD64)
    // If we are a varargs call, in order to set up the arguments correctly this
    // must be done in a 2 step process. As per the x64 ABI:
    // a) The caller sets up the argument shadow space (just before the return
    //    address, 4 pointer sized slots).
    // b) The callee is responsible to home the arguments on the shadow space
    //    provided by the caller.
    // This way, the varargs iterator will be able to retrieve the
    // call arguments properly since both the arg regs and the stack allocated
    // args will be contiguous.
    //
    // OSR methods can skip this, as the setup is done by the orignal method.
    if (compiler->info.compIsVarArgs && !compiler->opts.IsOSR())
    {
        GetEmitter()->spillIntArgRegsToShadowSlots();
    }

#endif // TARGET_AMD64

#ifdef TARGET_ARM
    /*-------------------------------------------------------------------------
     *
     * Now start emitting the part of the prolog which sets up the frame
     */

    if (regSet.rsMaskPreSpillRegs(true) != RBM_NONE)
    {
        inst_IV(INS_push, (int)regSet.rsMaskPreSpillRegs(true));
        compiler->unwindPushMaskInt(regSet.rsMaskPreSpillRegs(true));
    }
#endif // TARGET_ARM

#ifdef TARGET_XARCH
    if (doubleAlignOrFramePointerUsed())
    {
        inst_RV(INS_push, REG_FPBASE, TYP_REF);
        compiler->unwindPush(REG_FPBASE);
#ifdef USING_SCOPE_INFO
        psiAdjustStackLevel(REGSIZE_BYTES);
#endif               // USING_SCOPE_INFO
#ifndef TARGET_AMD64 // On AMD64, establish the frame pointer after the "sub rsp"
        genEstablishFramePointer(0, /*reportUnwindData*/ true);
#endif // !TARGET_AMD64

#if DOUBLE_ALIGN
        if (compiler->genDoubleAlign())
        {
            noway_assert(isFramePointerUsed() == false);
            noway_assert(!regSet.rsRegsModified(RBM_FPBASE)); /* Trashing EBP is out.    */

            inst_RV_IV(INS_AND, REG_SPBASE, -8, EA_PTRSIZE);
        }
#endif // DOUBLE_ALIGN
    }
#endif // TARGET_XARCH

#ifdef TARGET_ARM64
    genPushCalleeSavedRegisters(initReg, &initRegZeroed);
#else  // !TARGET_ARM64
    genPushCalleeSavedRegisters();
#endif // !TARGET_ARM64

#ifdef TARGET_ARM
    bool needToEstablishFP        = false;
    int  afterLclFrameSPtoFPdelta = 0;
    if (doubleAlignOrFramePointerUsed())
    {
        needToEstablishFP = true;

        // If the local frame is small enough, we establish the frame pointer after the OS-reported prolog.
        // This makes the prolog and epilog match, giving us smaller unwind data. If the frame size is
        // too big, we go ahead and do it here.

        int SPtoFPdelta          = (compiler->compCalleeRegsPushed - 2) * REGSIZE_BYTES;
        afterLclFrameSPtoFPdelta = SPtoFPdelta + compiler->compLclFrameSize;
        if (!arm_Valid_Imm_For_Add_SP(afterLclFrameSPtoFPdelta))
        {
            // Oh well, it looks too big. Go ahead and establish the frame pointer here.
            genEstablishFramePointer(SPtoFPdelta, /*reportUnwindData*/ true);
            needToEstablishFP = false;
        }
    }
#endif // TARGET_ARM

    //-------------------------------------------------------------------------
    //
    // Subtract the local frame size from SP.
    //
    //-------------------------------------------------------------------------
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef TARGET_ARM64
    regMaskTP maskStackAlloc = RBM_NONE;

#ifdef TARGET_ARM
    maskStackAlloc =
        genStackAllocRegisterMask(compiler->compLclFrameSize, regSet.rsGetModifiedRegsMask() & RBM_FLT_CALLEE_SAVED);
#endif // TARGET_ARM

    if (maskStackAlloc == RBM_NONE)
    {
        genAllocLclFrame(compiler->compLclFrameSize, initReg, &initRegZeroed, intRegState.rsCalleeRegArgMaskLiveIn);
    }
#endif // !TARGET_ARM64

//-------------------------------------------------------------------------

#ifdef TARGET_ARM
    if (compiler->compLocallocUsed)
    {
        GetEmitter()->emitIns_R_R(INS_mov, EA_4BYTE, REG_SAVED_LOCALLOC_SP, REG_SPBASE);
        regSet.verifyRegUsed(REG_SAVED_LOCALLOC_SP);
        compiler->unwindSetFrameReg(REG_SAVED_LOCALLOC_SP, 0);
    }
#endif // TARGET_ARMARCH

#if defined(TARGET_XARCH)
    // Preserve callee saved float regs to stack.
    genPreserveCalleeSavedFltRegs(compiler->compLclFrameSize);
#endif // defined(TARGET_XARCH)

#ifdef TARGET_AMD64
    // Establish the AMD64 frame pointer after the OS-reported prolog.
    if (doubleAlignOrFramePointerUsed())
    {
        bool reportUnwindData = compiler->compLocallocUsed || compiler->opts.compDbgEnC;
        genEstablishFramePointer(compiler->codeGen->genSPtoFPdelta(), reportUnwindData);
    }
#endif // TARGET_AMD64

//-------------------------------------------------------------------------
//
// This is the end of the OS-reported prolog for purposes of unwinding
//
//-------------------------------------------------------------------------

#ifdef TARGET_ARM
    if (needToEstablishFP)
    {
        genEstablishFramePointer(afterLclFrameSPtoFPdelta, /*reportUnwindData*/ false);
        needToEstablishFP = false; // nobody uses this later, but set it anyway, just to be explicit
    }
#endif // TARGET_ARM

    if (compiler->info.compPublishStubParam)
    {
#if CPU_LOAD_STORE_ARCH
        GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SECRET_STUB_PARAM,
                                  compiler->lvaStubArgumentVar, 0);
#else
        // mov [lvaStubArgumentVar], EAX
        GetEmitter()->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SECRET_STUB_PARAM, genFramePointerReg(),
                                   compiler->lvaTable[compiler->lvaStubArgumentVar].GetStackOffset());
#endif
        assert(intRegState.rsCalleeRegArgMaskLiveIn & RBM_SECRET_STUB_PARAM);

        // It's no longer live; clear it out so it can be used after this in the prolog
        intRegState.rsCalleeRegArgMaskLiveIn &= ~RBM_SECRET_STUB_PARAM;
    }

    //
    // Zero out the frame as needed
    //

    genZeroInitFrame(untrLclHi, untrLclLo, initReg, &initRegZeroed);

#if defined(FEATURE_EH_FUNCLETS)

    genSetPSPSym(initReg, &initRegZeroed);

#else // !FEATURE_EH_FUNCLETS

    // when compInitMem is true the genZeroInitFrame will zero out the shadow SP slots
    if (compiler->ehNeedsShadowSPslots() && !compiler->info.compInitMem)
    {
        // The last slot is reserved for ICodeManager::FixContext(ppEndRegion)
        unsigned filterEndOffsetSlotOffs = compiler->lvaLclSize(compiler->lvaShadowSPslotsVar) - TARGET_POINTER_SIZE;

        // Zero out the slot for nesting level 0
        unsigned firstSlotOffs = filterEndOffsetSlotOffs - TARGET_POINTER_SIZE;

        if (!initRegZeroed)
        {
            instGen_Set_Reg_To_Zero(EA_PTRSIZE, initReg);
            initRegZeroed = true;
        }

        GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, initReg, compiler->lvaShadowSPslotsVar,
                                  firstSlotOffs);
    }

#endif // !FEATURE_EH_FUNCLETS

    genReportGenericContextArg(initReg, &initRegZeroed);

#ifdef JIT32_GCENCODER
    // Initialize the LocalAllocSP slot if there is localloc in the function.
    if (compiler->lvaLocAllocSPvar != BAD_VAR_NUM)
    {
        GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaLocAllocSPvar, 0);
    }
#endif // JIT32_GCENCODER

    // Set up the GS security cookie

    genSetGSSecurityCookie(initReg, &initRegZeroed);

#ifdef PROFILING_SUPPORTED

    // Insert a function entry callback for profiling, if requested.
    // OSR methods aren't called, so don't have enter hooks.
    if (!compiler->opts.IsOSR())
    {
        genProfilingEnterCallback(initReg, &initRegZeroed);
    }

#endif // PROFILING_SUPPORTED

    if (!GetInterruptible())
    {
        // The 'real' prolog ends here for non-interruptible methods.
        // For fully-interruptible methods, we extend the prolog so that
        // we do not need to track GC inforation while shuffling the
        // arguments.
        GetEmitter()->emitMarkPrologEnd();
    }

#if defined(UNIX_AMD64_ABI) && defined(FEATURE_SIMD)
    // The unused bits of Vector3 arguments must be cleared
    // since native compiler doesn't initize the upper bits to zeros.
    //
    // TODO-Cleanup: This logic can be implemented in
    // genFnPrologCalleeRegArgs() for argument registers and
    // genEnregisterIncomingStackArgs() for stack arguments.
    genClearStackVec3ArgUpperBits();
#endif // UNIX_AMD64_ABI && FEATURE_SIMD

    /*-----------------------------------------------------------------------------
     * Take care of register arguments first
     */

    RegState* regState;

    // Update the arg initial register locations.
    compiler->lvaUpdateArgsWithInitialReg();

    // Home incoming arguments and generate any required inits.
    // OSR handles this by moving the values from the original frame.
    //
    if (!compiler->opts.IsOSR())
    {
        FOREACH_REGISTER_FILE(regState)
        {
            if (regState->rsCalleeRegArgMaskLiveIn)
            {
                // If we need an extra register to shuffle around the incoming registers
                // we will use xtraReg (initReg) and set the xtraRegClobbered flag,
                // if we don't need to use the xtraReg then this flag will stay false
                //
                regNumber xtraReg;
                bool      xtraRegClobbered = false;

                if (genRegMask(initReg) & RBM_ARG_REGS)
                {
                    xtraReg = initReg;
                }
                else
                {
                    xtraReg       = REG_SCRATCH;
                    initRegZeroed = false;
                }

                genFnPrologCalleeRegArgs(xtraReg, &xtraRegClobbered, regState);

                if (xtraRegClobbered)
                {
                    initRegZeroed = false;
                }
            }
        }
    }

    // Home the incoming arguments.
    genEnregisterIncomingStackArgs();

    /* Initialize any must-init registers variables now */

    if (initRegs)
    {
        regMaskTP regMask = 0x1;

        for (regNumber reg = REG_INT_FIRST; reg <= REG_INT_LAST; reg = REG_NEXT(reg), regMask <<= 1)
        {
            if (regMask & initRegs)
            {
                // Check if we have already zeroed this register
                if ((reg == initReg) && initRegZeroed)
                {
                    continue;
                }
                else
                {
                    instGen_Set_Reg_To_Zero(EA_PTRSIZE, reg);
                    if (reg == initReg)
                    {
                        initRegZeroed = true;
                    }
                }
            }
        }
    }

    if (initFltRegs | initDblRegs)
    {
        // If initReg is not in initRegs then we will use REG_SCRATCH
        if ((genRegMask(initReg) & initRegs) == 0)
        {
            initReg       = REG_SCRATCH;
            initRegZeroed = false;
        }

#ifdef TARGET_ARM
        // This is needed only for Arm since it can use a zero initialized int register
        // to initialize vfp registers.
        if (!initRegZeroed)
        {
            instGen_Set_Reg_To_Zero(EA_PTRSIZE, initReg);
            initRegZeroed = true;
        }
#endif // TARGET_ARM

        genZeroInitFltRegs(initFltRegs, initDblRegs, initReg);
    }

    //-----------------------------------------------------------------------------

    //
    // Increase the prolog size here only if fully interruptible.
    //

    if (GetInterruptible())
    {
        GetEmitter()->emitMarkPrologEnd();
    }
    if (compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0))
    {
        psiEndProlog();
    }

    if (hasGCRef)
    {
        GetEmitter()->emitSetFrameRangeGCRs(GCrefLo, GCrefHi);
    }
    else
    {
        noway_assert(GCrefLo == +INT_MAX);
        noway_assert(GCrefHi == -INT_MAX);
    }

#ifdef DEBUG
    if (compiler->opts.dspCode)
    {
        printf("\n");
    }
#endif

#ifdef TARGET_X86
    // On non-x86 the VARARG cookie does not need any special treatment.

    // Load up the VARARG argument pointer register so it doesn't get clobbered.
    // only do this if we actually access any statically declared args
    // (our argument pointer register has a refcount > 0).
    unsigned argsStartVar = compiler->lvaVarargsBaseOfStkArgs;

    if (compiler->info.compIsVarArgs && compiler->lvaTable[argsStartVar].lvRefCnt() > 0)
    {
        varDsc = &compiler->lvaTable[argsStartVar];

        noway_assert(compiler->info.compArgsCount > 0);

        // MOV EAX, <VARARGS HANDLE>
        GetEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_EAX, compiler->info.compArgsCount - 1, 0);
        regSet.verifyRegUsed(REG_EAX);

        // MOV EAX, [EAX]
        GetEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_EAX, REG_EAX, 0);

        // EDX might actually be holding something here.  So make sure to only use EAX for this code
        // sequence.

        LclVarDsc* lastArg = &compiler->lvaTable[compiler->info.compArgsCount - 1];
        noway_assert(!lastArg->lvRegister);
        signed offset = lastArg->GetStackOffset();
        assert(offset != BAD_STK_OFFS);
        noway_assert(lastArg->lvFramePointerBased);

        // LEA EAX, &<VARARGS HANDLE> + EAX
        GetEmitter()->emitIns_R_ARR(INS_lea, EA_PTRSIZE, REG_EAX, genFramePointerReg(), REG_EAX, offset);

        if (varDsc->lvIsInReg())
        {
            if (varDsc->GetRegNum() != REG_EAX)
            {
                GetEmitter()->emitIns_R_R(INS_mov, EA_PTRSIZE, varDsc->GetRegNum(), REG_EAX);
                regSet.verifyRegUsed(varDsc->GetRegNum());
            }
        }
        else
        {
            GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_EAX, argsStartVar, 0);
        }
    }

#endif // TARGET_X86

#if defined(DEBUG) && defined(TARGET_XARCH)
    if (compiler->opts.compStackCheckOnRet)
    {
        noway_assert(compiler->lvaReturnSpCheck != 0xCCCCCCCC &&
                     compiler->lvaTable[compiler->lvaReturnSpCheck].lvDoNotEnregister &&
                     compiler->lvaTable[compiler->lvaReturnSpCheck].lvOnFrame);
        GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaReturnSpCheck, 0);
    }
#endif // defined(DEBUG) && defined(TARGET_XARCH)

    GetEmitter()->emitEndProlog();
    compiler->unwindEndProlog();

    noway_assert(GetEmitter()->emitMaxTmpSize == regSet.tmpGetTotalSize());
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************
 *
 *  Generates code for a function epilog.
 *
 *  Please consult the "debugger team notification" comment in genFnProlog().
 */

#if defined(TARGET_ARMARCH)

void CodeGen::genFnEpilog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
        printf("*************** In genFnEpilog()\n");
#endif // DEBUG

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    VarSetOps::Assign(compiler, gcInfo.gcVarPtrSetCur, GetEmitter()->emitInitGCrefVars);
    gcInfo.gcRegGCrefSetCur = GetEmitter()->emitInitGCrefRegs;
    gcInfo.gcRegByrefSetCur = GetEmitter()->emitInitByrefRegs;

#ifdef DEBUG
    if (compiler->opts.dspCode)
        printf("\n__epilog:\n");

    if (verbose)
    {
        printf("gcVarPtrSetCur=%s ", VarSetOps::ToString(compiler, gcInfo.gcVarPtrSetCur));
        dumpConvertedVarSet(compiler, gcInfo.gcVarPtrSetCur);
        printf(", gcRegGCrefSetCur=");
        printRegMaskInt(gcInfo.gcRegGCrefSetCur);
        GetEmitter()->emitDispRegSet(gcInfo.gcRegGCrefSetCur);
        printf(", gcRegByrefSetCur=");
        printRegMaskInt(gcInfo.gcRegByrefSetCur);
        GetEmitter()->emitDispRegSet(gcInfo.gcRegByrefSetCur);
        printf("\n");
    }
#endif // DEBUG

    bool jmpEpilog = ((block->bbFlags & BBF_HAS_JMP) != 0);

    GenTree* lastNode = block->lastNode();

    // Method handle and address info used in case of jump epilog
    CORINFO_METHOD_HANDLE methHnd = nullptr;
    CORINFO_CONST_LOOKUP  addrInfo;
    addrInfo.addr       = nullptr;
    addrInfo.accessType = IAT_VALUE;

    if (jmpEpilog && lastNode->gtOper == GT_JMP)
    {
        methHnd = (CORINFO_METHOD_HANDLE)lastNode->AsVal()->gtVal1;
        compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &addrInfo);
    }

#ifdef TARGET_ARM
    // We delay starting the unwind codes until we have an instruction which we know
    // needs an unwind code. In particular, for large stack frames in methods without
    // localloc, the sequence might look something like this:
    //      movw    r3, 0x38e0
    //      add     sp, r3
    //      pop     {r4,r5,r6,r10,r11,pc}
    // In this case, the "movw" should not be part of the unwind codes, since it will
    // be a NOP, and it is a waste to start with a NOP. Note that calling unwindBegEpilog()
    // also sets the current location as the beginning offset of the epilog, so every
    // instruction afterwards needs an unwind code. In the case above, if you call
    // unwindBegEpilog() before the "movw", then you must generate a NOP for the "movw".

    bool unwindStarted = false;

    // Tear down the stack frame

    if (compiler->compLocallocUsed)
    {
        if (!unwindStarted)
        {
            compiler->unwindBegEpilog();
            unwindStarted = true;
        }

        // mov R9 into SP
        inst_RV_RV(INS_mov, REG_SP, REG_SAVED_LOCALLOC_SP);
        compiler->unwindSetFrameReg(REG_SAVED_LOCALLOC_SP, 0);
    }

    if (jmpEpilog ||
        genStackAllocRegisterMask(compiler->compLclFrameSize, regSet.rsGetModifiedRegsMask() & RBM_FLT_CALLEE_SAVED) ==
            RBM_NONE)
    {
        genFreeLclFrame(compiler->compLclFrameSize, &unwindStarted);
    }

    if (!unwindStarted)
    {
        // If we haven't generated anything yet, we're certainly going to generate a "pop" next.
        compiler->unwindBegEpilog();
        unwindStarted = true;
    }

    if (jmpEpilog && lastNode->gtOper == GT_JMP && addrInfo.accessType == IAT_RELPVALUE)
    {
        // IAT_RELPVALUE jump at the end is done using relative indirection, so,
        // additional helper register is required.
        // We use LR just before it is going to be restored from stack, i.e.
        //
        //     movw r12, laddr
        //     movt r12, haddr
        //     mov lr, r12
        //     ldr r12, [r12]
        //     add r12, r12, lr
        //     pop {lr}
        //     ...
        //     bx r12

        regNumber indCallReg = REG_R12;
        regNumber vptrReg1   = REG_LR;

        instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indCallReg, (ssize_t)addrInfo.addr);
        GetEmitter()->emitIns_R_R(INS_mov, EA_PTRSIZE, vptrReg1, indCallReg);
        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, indCallReg, indCallReg, 0);
        GetEmitter()->emitIns_R_R(INS_add, EA_PTRSIZE, indCallReg, vptrReg1);
    }

    genPopCalleeSavedRegisters(jmpEpilog);

    if (regSet.rsMaskPreSpillRegs(true) != RBM_NONE)
    {
        // We better not have used a pop PC to return otherwise this will be unreachable code
        noway_assert(!genUsedPopToReturn);

        int preSpillRegArgSize = genCountBits(regSet.rsMaskPreSpillRegs(true)) * REGSIZE_BYTES;
        inst_RV_IV(INS_add, REG_SPBASE, preSpillRegArgSize, EA_PTRSIZE);
        compiler->unwindAllocStack(preSpillRegArgSize);
    }

    if (jmpEpilog)
    {
        // We better not have used a pop PC to return otherwise this will be unreachable code
        noway_assert(!genUsedPopToReturn);
    }

#else  // TARGET_ARM64
    compiler->unwindBegEpilog();

    genPopCalleeSavedRegistersAndFreeLclFrame(jmpEpilog);
#endif // TARGET_ARM64

    if (jmpEpilog)
    {
        SetHasTailCalls(true);

        noway_assert(block->bbJumpKind == BBJ_RETURN);
        noway_assert(block->GetFirstLIRNode() != nullptr);

        /* figure out what jump we have */
        GenTree* jmpNode = lastNode;
#if !FEATURE_FASTTAILCALL
        noway_assert(jmpNode->gtOper == GT_JMP);
#else  // FEATURE_FASTTAILCALL
        // armarch
        // If jmpNode is GT_JMP then gtNext must be null.
        // If jmpNode is a fast tail call, gtNext need not be null since it could have embedded stmts.
        noway_assert((jmpNode->gtOper != GT_JMP) || (jmpNode->gtNext == nullptr));

        // Could either be a "jmp method" or "fast tail call" implemented as epilog+jmp
        noway_assert((jmpNode->gtOper == GT_JMP) ||
                     ((jmpNode->gtOper == GT_CALL) && jmpNode->AsCall()->IsFastTailCall()));

        // The next block is associated with this "if" stmt
        if (jmpNode->gtOper == GT_JMP)
#endif // FEATURE_FASTTAILCALL
        {
            // Simply emit a jump to the methodHnd. This is similar to a call so we can use
            // the same descriptor with some minor adjustments.
            assert(methHnd != nullptr);
            assert(addrInfo.addr != nullptr);

#ifdef TARGET_ARMARCH
            emitter::EmitCallType callType;
            void*                 addr;
            regNumber             indCallReg;
            switch (addrInfo.accessType)
            {
                case IAT_VALUE:
                    if (validImmForBL((ssize_t)addrInfo.addr))
                    {
                        // Simple direct call
                        callType   = emitter::EC_FUNC_TOKEN;
                        addr       = addrInfo.addr;
                        indCallReg = REG_NA;
                        break;
                    }

                    // otherwise the target address doesn't fit in an immediate
                    // so we have to burn a register...
                    FALLTHROUGH;

                case IAT_PVALUE:
                    // Load the address into a register, load indirect and call  through a register
                    // We have to use R12 since we assume the argument registers are in use
                    callType   = emitter::EC_INDIR_R;
                    indCallReg = REG_INDIRECT_CALL_TARGET_REG;
                    addr       = NULL;
                    instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indCallReg, (ssize_t)addrInfo.addr);
                    if (addrInfo.accessType == IAT_PVALUE)
                    {
                        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, indCallReg, indCallReg, 0);
                        regSet.verifyRegUsed(indCallReg);
                    }
                    break;

                case IAT_RELPVALUE:
                {
                    // Load the address into a register, load relative indirect and call through a register
                    // We have to use R12 since we assume the argument registers are in use
                    // LR is used as helper register right before it is restored from stack, thus,
                    // all relative address calculations are performed before LR is restored.
                    callType   = emitter::EC_INDIR_R;
                    indCallReg = REG_R12;
                    addr       = NULL;

                    regSet.verifyRegUsed(indCallReg);
                    break;
                }

                case IAT_PPVALUE:
                default:
                    NO_WAY("Unsupported JMP indirection");
            }

            /* Simply emit a jump to the methodHnd. This is similar to a call so we can use
             * the same descriptor with some minor adjustments.
             */

            // clang-format off
            GetEmitter()->emitIns_Call(callType,
                                       methHnd,
                                       INDEBUG_LDISASM_COMMA(nullptr)
                                       addr,
                                       0,          // argSize
                                       EA_UNKNOWN, // retSize
#if defined(TARGET_ARM64)
                                       EA_UNKNOWN, // secondRetSize
#endif
                                       gcInfo.gcVarPtrSetCur,
                                       gcInfo.gcRegGCrefSetCur,
                                       gcInfo.gcRegByrefSetCur,
                                       BAD_IL_OFFSET, // IL offset
                                       indCallReg,    // ireg
                                       REG_NA,        // xreg
                                       0,             // xmul
                                       0,             // disp
                                       true);         // isJump
            // clang-format on
            CLANG_FORMAT_COMMENT_ANCHOR;
#endif // TARGET_ARMARCH
        }
#if FEATURE_FASTTAILCALL
        else
        {
            // Fast tail call.
            GenTreeCall* call     = jmpNode->AsCall();
            gtCallTypes  callType = (gtCallTypes)call->gtCallType;

            // Fast tail calls cannot happen to helpers.
            assert((callType == CT_INDIRECT) || (callType == CT_USER_FUNC));

            // Try to dispatch this as a direct branch; this is possible when the call is
            // truly direct. In this case, the control expression will be null and the direct
            // target address will be in gtDirectCallAddress. It is still possible that calls
            // to user funcs require indirection, in which case the control expression will
            // be non-null.
            if ((callType == CT_USER_FUNC) && (call->gtControlExpr == nullptr))
            {
                assert(call->gtCallMethHnd != nullptr);
                // clang-format off
                GetEmitter()->emitIns_Call(emitter::EC_FUNC_TOKEN,
                                           call->gtCallMethHnd,
                                           INDEBUG_LDISASM_COMMA(nullptr)
                                           call->gtDirectCallAddress,
                                           0,          // argSize
                                           EA_UNKNOWN  // retSize
                                           ARM64_ARG(EA_UNKNOWN), // secondRetSize
                                           gcInfo.gcVarPtrSetCur,
                                           gcInfo.gcRegGCrefSetCur,
                                           gcInfo.gcRegByrefSetCur,
                                           BAD_IL_OFFSET, // IL offset
                                           REG_NA,        // ireg
                                           REG_NA,        // xreg
                                           0,             // xmul
                                           0,             // disp
                                           true);         // isJump
                // clang-format on
            }
            else
            {
                // Target requires indirection to obtain. genCallInstruction will have materialized
                // it into REG_FASTTAILCALL_TARGET already, so just branch to it.
                GetEmitter()->emitIns_R(INS_br, emitTypeSize(TYP_I_IMPL), REG_FASTTAILCALL_TARGET);
            }
        }
#endif // FEATURE_FASTTAILCALL
    }
    else
    {
#ifdef TARGET_ARM
        if (!genUsedPopToReturn)
        {
            // If we did not use a pop to return, then we did a "pop {..., lr}" instead of "pop {..., pc}",
            // so we need a "bx lr" instruction to return from the function.
            inst_RV(INS_bx, REG_LR, TYP_I_IMPL);
            compiler->unwindBranch16();
        }
#else  // TARGET_ARM64
        inst_RV(INS_ret, REG_LR, TYP_I_IMPL);
        compiler->unwindReturn(REG_LR);
#endif // TARGET_ARM64
    }

    compiler->unwindEndEpilog();
}

#elif defined(TARGET_XARCH)

void CodeGen::genFnEpilog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFnEpilog()\n");
    }
#endif

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    VarSetOps::Assign(compiler, gcInfo.gcVarPtrSetCur, GetEmitter()->emitInitGCrefVars);
    gcInfo.gcRegGCrefSetCur = GetEmitter()->emitInitGCrefRegs;
    gcInfo.gcRegByrefSetCur = GetEmitter()->emitInitByrefRegs;

    noway_assert(!compiler->opts.MinOpts() || isFramePointerUsed()); // FPO not allowed with minOpts

#ifdef DEBUG
    genInterruptibleUsed = true;
#endif

    bool jmpEpilog = ((block->bbFlags & BBF_HAS_JMP) != 0);

#ifdef DEBUG
    if (compiler->opts.dspCode)
    {
        printf("\n__epilog:\n");
    }

    if (verbose)
    {
        printf("gcVarPtrSetCur=%s ", VarSetOps::ToString(compiler, gcInfo.gcVarPtrSetCur));
        dumpConvertedVarSet(compiler, gcInfo.gcVarPtrSetCur);
        printf(", gcRegGCrefSetCur=");
        printRegMaskInt(gcInfo.gcRegGCrefSetCur);
        GetEmitter()->emitDispRegSet(gcInfo.gcRegGCrefSetCur);
        printf(", gcRegByrefSetCur=");
        printRegMaskInt(gcInfo.gcRegByrefSetCur);
        GetEmitter()->emitDispRegSet(gcInfo.gcRegByrefSetCur);
        printf("\n");
    }
#endif

    // Restore float registers that were saved to stack before SP is modified.
    genRestoreCalleeSavedFltRegs(compiler->compLclFrameSize);

#ifdef JIT32_GCENCODER
    // When using the JIT32 GC encoder, we do not start the OS-reported portion of the epilog until after
    // the above call to `genRestoreCalleeSavedFltRegs` because that function
    //   a) does not actually restore any registers: there are none when targeting the Windows x86 ABI,
    //      which is the only target that uses the JIT32 GC encoder
    //   b) may issue a `vzeroupper` instruction to eliminate AVX -> SSE transition penalties.
    // Because the `vzeroupper` instruction is not recognized by the VM's unwinder and there are no
    // callee-save FP restores that the unwinder would need to see, we can avoid the need to change the
    // unwinder (and break binary compat with older versions of the runtime) by starting the epilog
    // after any `vzeroupper` instruction has been emitted. If either of the above conditions changes,
    // we will need to rethink this.
    GetEmitter()->emitStartEpilog();
#endif

    /* Compute the size in bytes we've pushed/popped */

    if (!doubleAlignOrFramePointerUsed())
    {
        // We have an ESP frame */

        noway_assert(compiler->compLocallocUsed == false); // Only used with frame-pointer

        /* Get rid of our local variables */

        if (compiler->compLclFrameSize)
        {
#ifdef TARGET_X86
            /* Add 'compiler->compLclFrameSize' to ESP */
            /* Use pop ECX to increment ESP by 4, unless compiler->compJmpOpUsed is true */

            if ((compiler->compLclFrameSize == TARGET_POINTER_SIZE) && !compiler->compJmpOpUsed)
            {
                inst_RV(INS_pop, REG_ECX, TYP_I_IMPL);
                regSet.verifyRegUsed(REG_ECX);
            }
            else
#endif // TARGET_X86
            {
                /* Add 'compiler->compLclFrameSize' to ESP */
                /* Generate "add esp, <stack-size>" */
                inst_RV_IV(INS_add, REG_SPBASE, compiler->compLclFrameSize, EA_PTRSIZE);
            }
        }

        genPopCalleeSavedRegisters();

        // Extra OSR adjust to get to where RBP was saved by the original frame, and
        // restore RBP.
        //
        // Note the other callee saves made in that frame are dead, the OSR method
        // will save and restore what it needs.
        if (compiler->opts.IsOSR())
        {
            PatchpointInfo* patchpointInfo    = compiler->info.compPatchpointInfo;
            const int       originalFrameSize = patchpointInfo->FpToSpDelta();

            // Use add since we know the SP-to-FP delta of the original method.
            //
            // If we ever allow the original method to have localloc this will
            // need to change.
            inst_RV_IV(INS_add, REG_SPBASE, originalFrameSize, EA_PTRSIZE);
            inst_RV(INS_pop, REG_EBP, TYP_I_IMPL);
        }
    }
    else
    {
        noway_assert(doubleAlignOrFramePointerUsed());

        /* Tear down the stack frame */

        bool needMovEspEbp = false;

#if DOUBLE_ALIGN
        if (compiler->genDoubleAlign())
        {
            //
            // add esp, compLclFrameSize
            //
            // We need not do anything (except the "mov esp, ebp") if
            // compiler->compCalleeRegsPushed==0. However, this is unlikely, and it
            // also complicates the code manager. Hence, we ignore that case.

            noway_assert(compiler->compLclFrameSize != 0);
            inst_RV_IV(INS_add, REG_SPBASE, compiler->compLclFrameSize, EA_PTRSIZE);

            needMovEspEbp = true;
        }
        else
#endif // DOUBLE_ALIGN
        {
            bool needLea = false;

            if (compiler->compLocallocUsed)
            {
                // OSR not yet ready for localloc
                assert(!compiler->opts.IsOSR());

                // ESP may be variable if a localloc was actually executed. Reset it.
                //    lea esp, [ebp - compiler->compCalleeRegsPushed * REGSIZE_BYTES]
                needLea = true;
            }
            else if (!regSet.rsRegsModified(RBM_CALLEE_SAVED))
            {
                if (compiler->compLclFrameSize != 0)
                {
#ifdef TARGET_AMD64
                    // AMD64 can't use "mov esp, ebp", according to the ABI specification describing epilogs. So,
                    // do an LEA to "pop off" the frame allocation.
                    needLea = true;
#else  // !TARGET_AMD64
                    // We will just generate "mov esp, ebp" and be done with it.
                    needMovEspEbp = true;
#endif // !TARGET_AMD64
                }
            }
            else if (compiler->compLclFrameSize == 0)
            {
                // do nothing before popping the callee-saved registers
            }
#ifdef TARGET_X86
            else if (compiler->compLclFrameSize == REGSIZE_BYTES)
            {
                // "pop ecx" will make ESP point to the callee-saved registers
                inst_RV(INS_pop, REG_ECX, TYP_I_IMPL);
                regSet.verifyRegUsed(REG_ECX);
            }
#endif // TARGET_X86
            else
            {
                // We need to make ESP point to the callee-saved registers
                needLea = true;
            }

            if (needLea)
            {
                int offset;

#ifdef TARGET_AMD64
                // lea esp, [ebp + compiler->compLclFrameSize - genSPtoFPdelta]
                //
                // Case 1: localloc not used.
                // genSPToFPDelta = compiler->compCalleeRegsPushed * REGSIZE_BYTES + compiler->compLclFrameSize
                // offset = compiler->compCalleeRegsPushed * REGSIZE_BYTES;
                // The amount to be subtracted from RBP to point at callee saved int regs.
                //
                // Case 2: localloc used
                // genSPToFPDelta = Min(240, (int)compiler->lvaOutgoingArgSpaceSize)
                // Offset = Amount to be added to RBP to point at callee saved int regs.
                offset = genSPtoFPdelta() - compiler->compLclFrameSize;

                // Offset should fit within a byte if localloc is not used.
                if (!compiler->compLocallocUsed)
                {
                    noway_assert(offset < UCHAR_MAX);
                }
#else
                // lea esp, [ebp - compiler->compCalleeRegsPushed * REGSIZE_BYTES]
                offset = compiler->compCalleeRegsPushed * REGSIZE_BYTES;
                noway_assert(offset < UCHAR_MAX); // the offset fits in a byte
#endif

                GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, -offset);
            }
        }

        //
        // Pop the callee-saved registers (if any)
        //
        genPopCalleeSavedRegisters();

#ifdef TARGET_AMD64
        // Extra OSR adjust to get to where RBP was saved by the original frame.
        //
        // Note the other callee saves made in that frame are dead, the current method
        // will save and restore what it needs.
        if (compiler->opts.IsOSR())
        {
            PatchpointInfo* patchpointInfo    = compiler->info.compPatchpointInfo;
            const int       originalFrameSize = patchpointInfo->FpToSpDelta();

            // Use add since we know the SP-to-FP delta of the original method.
            // We also need to skip over the slot where we pushed RBP.
            //
            // If we ever allow the original method to have localloc this will
            // need to change.
            inst_RV_IV(INS_add, REG_SPBASE, originalFrameSize + TARGET_POINTER_SIZE, EA_PTRSIZE);
        }

        assert(!needMovEspEbp); // "mov esp, ebp" is not allowed in AMD64 epilogs
#else  // !TARGET_AMD64
        if (needMovEspEbp)
        {
            // mov esp, ebp
            inst_RV_RV(INS_mov, REG_SPBASE, REG_FPBASE);
        }
#endif // !TARGET_AMD64

        // pop ebp
        inst_RV(INS_pop, REG_EBP, TYP_I_IMPL);
    }

    GetEmitter()->emitStartExitSeq(); // Mark the start of the "return" sequence

    /* Check if this a special return block i.e.
     * CEE_JMP instruction */

    if (jmpEpilog)
    {
        noway_assert(block->bbJumpKind == BBJ_RETURN);
        noway_assert(block->GetFirstLIRNode());

        // figure out what jump we have
        GenTree* jmpNode = block->lastNode();
#if !FEATURE_FASTTAILCALL
        // x86
        noway_assert(jmpNode->gtOper == GT_JMP);
#else
        // amd64
        // If jmpNode is GT_JMP then gtNext must be null.
        // If jmpNode is a fast tail call, gtNext need not be null since it could have embedded stmts.
        noway_assert((jmpNode->gtOper != GT_JMP) || (jmpNode->gtNext == nullptr));

        // Could either be a "jmp method" or "fast tail call" implemented as epilog+jmp
        noway_assert((jmpNode->gtOper == GT_JMP) ||
                     ((jmpNode->gtOper == GT_CALL) && jmpNode->AsCall()->IsFastTailCall()));

        // The next block is associated with this "if" stmt
        if (jmpNode->gtOper == GT_JMP)
#endif
        {
            // Simply emit a jump to the methodHnd. This is similar to a call so we can use
            // the same descriptor with some minor adjustments.
            CORINFO_METHOD_HANDLE methHnd = (CORINFO_METHOD_HANDLE)jmpNode->AsVal()->gtVal1;

            CORINFO_CONST_LOOKUP addrInfo;
            compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &addrInfo);
            if (addrInfo.accessType != IAT_VALUE && addrInfo.accessType != IAT_PVALUE)
            {
                NO_WAY("Unsupported JMP indirection");
            }

            // If we have IAT_PVALUE we might need to jump via register indirect, as sometimes the
            // indirection cell can't be reached by the jump.
            emitter::EmitCallType callType;
            void*                 addr;
            regNumber             indCallReg;

            if (addrInfo.accessType == IAT_PVALUE)
            {
                if (genCodeIndirAddrCanBeEncodedAsPCRelOffset((size_t)addrInfo.addr))
                {
                    // 32 bit displacement will work
                    callType   = emitter::EC_FUNC_TOKEN_INDIR;
                    addr       = addrInfo.addr;
                    indCallReg = REG_NA;
                }
                else
                {
                    // 32 bit displacement won't work
                    callType   = emitter::EC_INDIR_ARD;
                    indCallReg = REG_RAX;
                    addr       = nullptr;
                    instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indCallReg, (ssize_t)addrInfo.addr);
                    regSet.verifyRegUsed(indCallReg);
                }
            }
            else
            {
                callType   = emitter::EC_FUNC_TOKEN;
                addr       = addrInfo.addr;
                indCallReg = REG_NA;
            }

            // clang-format off
            GetEmitter()->emitIns_Call(callType,
                                       methHnd,
                                       INDEBUG_LDISASM_COMMA(nullptr)
                                       addr,
                                       0,                                                      // argSize
                                       EA_UNKNOWN                                              // retSize
                                       MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(EA_UNKNOWN),        // secondRetSize
                                       gcInfo.gcVarPtrSetCur,
                                       gcInfo.gcRegGCrefSetCur,
                                       gcInfo.gcRegByrefSetCur,
                                       BAD_IL_OFFSET, indCallReg, REG_NA, 0, 0,  /* iloffset, ireg, xreg, xmul, disp */
                                       true /* isJump */
            );
            // clang-format on
        }
#if FEATURE_FASTTAILCALL
        else
        {
#ifdef TARGET_AMD64
            // Fast tail call.
            GenTreeCall* call     = jmpNode->AsCall();
            gtCallTypes  callType = (gtCallTypes)call->gtCallType;

            // Fast tail calls cannot happen to helpers.
            assert((callType == CT_INDIRECT) || (callType == CT_USER_FUNC));

            // Calls to a user func can be dispatched as an RIP-relative jump when they are
            // truly direct; in this case, the control expression will be null and the direct
            // target address will be in gtDirectCallAddress. It is still possible that calls
            // to user funcs require indirection, in which case the control expression will
            // be non-null.
            if ((callType == CT_USER_FUNC) && (call->gtControlExpr == nullptr))
            {
                assert(call->gtCallMethHnd != nullptr);
                // clang-format off
                GetEmitter()->emitIns_Call(
                        emitter::EC_FUNC_TOKEN,
                        call->gtCallMethHnd,
                        INDEBUG_LDISASM_COMMA(nullptr)
                        call->gtDirectCallAddress,
                        0,                                              // argSize
                        EA_UNKNOWN                                      // retSize
                        MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(EA_UNKNOWN),// secondRetSize
                        gcInfo.gcVarPtrSetCur,
                        gcInfo.gcRegGCrefSetCur,
                        gcInfo.gcRegByrefSetCur,
                        BAD_IL_OFFSET, REG_NA, REG_NA, 0, 0,  /* iloffset, ireg, xreg, xmul, disp */
                        true /* isJump */
                );
                // clang-format on
            }
            else
            {
                // Target requires indirection to obtain. genCallInstruction will have materialized
                // it into RAX already, so just jump to it. The stack walker requires that a register
                // indirect tail call be rex.w prefixed.
                GetEmitter()->emitIns_R(INS_rex_jmp, emitTypeSize(TYP_I_IMPL), REG_RAX);
            }

#else
            assert(!"Fast tail call as epilog+jmp");
            unreached();
#endif // TARGET_AMD64
        }
#endif // FEATURE_FASTTAILCALL
    }
    else
    {
        unsigned stkArgSize = 0; // Zero on all platforms except x86

#if defined(TARGET_X86)
        bool     fCalleePop = true;

        // varargs has caller pop
        if (compiler->info.compIsVarArgs)
            fCalleePop = false;

        if (IsCallerPop(compiler->info.compCallConv))
            fCalleePop = false;

        if (fCalleePop)
        {
            noway_assert(compiler->compArgSize >= intRegState.rsCalleeRegArgCount * REGSIZE_BYTES);
            stkArgSize = compiler->compArgSize - intRegState.rsCalleeRegArgCount * REGSIZE_BYTES;

            noway_assert(compiler->compArgSize < 0x10000); // "ret" only has 2 byte operand
        }
#endif // TARGET_X86

        /* Return, popping our arguments (if any) */
        instGen_Return(stkArgSize);
    }
}

#else // TARGET*
#error Unsupported or unset target architecture
#endif // TARGET*

#if defined(FEATURE_EH_FUNCLETS)

#ifdef TARGET_ARM

/*****************************************************************************
 *
 *  Generates code for an EH funclet prolog.
 *
 *  Funclets have the following incoming arguments:
 *
 *      catch:          r0 = the exception object that was caught (see GT_CATCH_ARG)
 *      filter:         r0 = the exception object to filter (see GT_CATCH_ARG), r1 = CallerSP of the containing function
 *      finally/fault:  none
 *
 *  Funclets set the following registers on exit:
 *
 *      catch:          r0 = the address at which execution should resume (see BBJ_EHCATCHRET)
 *      filter:         r0 = non-zero if the handler should handle the exception, zero otherwise (see GT_RETFILT)
 *      finally/fault:  none
 *
 *  The ARM funclet prolog sequence is:
 *
 *     push {regs,lr}   ; We push the callee-saved regs and 'lr'.
 *                      ;   TODO-ARM-CQ: We probably only need to save lr, plus any callee-save registers that we
 *                      ;         actually use in the funclet. Currently, we save the same set of callee-saved regs
 *                      ;         calculated for the entire function.
 *     sub sp, XXX      ; Establish the rest of the frame.
 *                      ;   XXX is determined by lvaOutgoingArgSpaceSize plus space for the PSP slot, aligned
 *                      ;   up to preserve stack alignment. If we push an odd number of registers, we also
 *                      ;   generate this, to keep the stack aligned.
 *
 *     ; Fill the PSP slot, for use by the VM (it gets reported with the GC info), or by code generation of nested
 *     ;     filters.
 *     ; This is not part of the "OS prolog"; it has no associated unwind data, and is not reversed in the funclet
 *     ;     epilog.
 *
 *     if (this is a filter funclet)
 *     {
 *          // r1 on entry to a filter funclet is CallerSP of the containing function:
 *          // either the main function, or the funclet for a handler that this filter is dynamically nested within.
 *          // Note that a filter can be dynamically nested within a funclet even if it is not statically within
 *          // a funclet. Consider:
 *          //
 *          //    try {
 *          //        try {
 *          //            throw new Exception();
 *          //        } catch(Exception) {
 *          //            throw new Exception();     // The exception thrown here ...
 *          //        }
 *          //    } filter {                         // ... will be processed here, while the "catch" funclet frame is
 *          //                                       // still on the stack
 *          //    } filter-handler {
 *          //    }
 *          //
 *          // Because of this, we need a PSP in the main function anytime a filter funclet doesn't know whether the
 *          // enclosing frame will be a funclet or main function. We won't know any time there is a filter protecting
 *          // nested EH. To simplify, we just always create a main function PSP for any function with a filter.
 *
 *          ldr r1, [r1 - PSP_slot_CallerSP_offset]     ; Load the CallerSP of the main function (stored in the PSP of
 *                                                      ; the dynamically containing funclet or function)
 *          str r1, [sp + PSP_slot_SP_offset]           ; store the PSP
 *          sub r11, r1, Function_CallerSP_to_FP_delta  ; re-establish the frame pointer
 *     }
 *     else
 *     {
 *          // This is NOT a filter funclet. The VM re-establishes the frame pointer on entry.
 *          // TODO-ARM-CQ: if VM set r1 to CallerSP on entry, like for filters, we could save an instruction.
 *
 *          add r3, r11, Function_CallerSP_to_FP_delta  ; compute the CallerSP, given the frame pointer. r3 is scratch.
 *          str r3, [sp + PSP_slot_SP_offset]           ; store the PSP
 *     }
 *
 *  The epilog sequence is then:
 *
 *     add sp, XXX      ; if necessary
 *     pop {regs,pc}
 *
 *  If it is worth it, we could push r0, r1, r2, r3 instead of using an additional add/sub instruction.
 *  Code size would be smaller, but we would be writing to / reading from the stack, which might be slow.
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |Callee saved registers |
 *      |-----------------------|
 *      |Pre-spill regs space   |   // This is only necessary to keep the PSP slot at the same offset
 *      |                       |   // in function and funclet
 *      |-----------------------|
 *      |        PSP slot       |   // Omitted in CoreRT ABI
 *      |-----------------------|
 *      ~  possible 4 byte pad  ~
 *      ~     for alignment     ~
 *      |-----------------------|
 *      |   Outgoing arg space  |
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 */

void CodeGen::genFuncletProlog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
        printf("*************** In genFuncletProlog()\n");
#endif

    assert(block != NULL);
    assert(block->bbFlags & BBF_FUNCLET_BEG);

    ScopedSetVariable<bool> _setGeneratingProlog(&compiler->compGeneratingProlog, true);

    gcInfo.gcResetForBB();

    compiler->unwindBegProlog();

    regMaskTP maskPushRegsFloat = genFuncletInfo.fiSaveRegs & RBM_ALLFLOAT;
    regMaskTP maskPushRegsInt   = genFuncletInfo.fiSaveRegs & ~maskPushRegsFloat;

    regMaskTP maskStackAlloc = genStackAllocRegisterMask(genFuncletInfo.fiSpDelta, maskPushRegsFloat);
    maskPushRegsInt |= maskStackAlloc;

    assert(FitsIn<int>(maskPushRegsInt));
    inst_IV(INS_push, (int)maskPushRegsInt);
    compiler->unwindPushMaskInt(maskPushRegsInt);

    if (maskPushRegsFloat != RBM_NONE)
    {
        genPushFltRegs(maskPushRegsFloat);
        compiler->unwindPushMaskFloat(maskPushRegsFloat);
    }

    bool isFilter = (block->bbCatchTyp == BBCT_FILTER);

    regMaskTP maskArgRegsLiveIn;
    if (isFilter)
    {
        maskArgRegsLiveIn = RBM_R0 | RBM_R1;
    }
    else if ((block->bbCatchTyp == BBCT_FINALLY) || (block->bbCatchTyp == BBCT_FAULT))
    {
        maskArgRegsLiveIn = RBM_NONE;
    }
    else
    {
        maskArgRegsLiveIn = RBM_R0;
    }

    regNumber initReg       = REG_R3; // R3 is never live on entry to a funclet, so it can be trashed
    bool      initRegZeroed = false;

    if (maskStackAlloc == RBM_NONE)
    {
        genAllocLclFrame(genFuncletInfo.fiSpDelta, initReg, &initRegZeroed, maskArgRegsLiveIn);
    }

    // This is the end of the OS-reported prolog for purposes of unwinding
    compiler->unwindEndProlog();

    // If there is no PSPSym (CoreRT ABI), we are done.
    if (compiler->lvaPSPSym == BAD_VAR_NUM)
    {
        return;
    }

    if (isFilter)
    {
        // This is the first block of a filter

        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, REG_R1, REG_R1, genFuncletInfo.fiPSP_slot_CallerSP_offset);
        regSet.verifyRegUsed(REG_R1);
        GetEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, REG_R1, REG_SPBASE, genFuncletInfo.fiPSP_slot_SP_offset);
        GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_FPBASE, REG_R1,
                                    genFuncletInfo.fiFunctionCallerSPtoFPdelta);
    }
    else
    {
        // This is a non-filter funclet
        GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_R3, REG_FPBASE,
                                    genFuncletInfo.fiFunctionCallerSPtoFPdelta);
        regSet.verifyRegUsed(REG_R3);
        GetEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, REG_R3, REG_SPBASE, genFuncletInfo.fiPSP_slot_SP_offset);
    }
}

/*****************************************************************************
 *
 *  Generates code for an EH funclet epilog.
 */

void CodeGen::genFuncletEpilog()
{
#ifdef DEBUG
    if (verbose)
        printf("*************** In genFuncletEpilog()\n");
#endif

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    // Just as for the main function, we delay starting the unwind codes until we have
    // an instruction which we know needs an unwind code. This is to support code like
    // this:
    //      movw    r3, 0x38e0
    //      add     sp, r3
    //      pop     {r4,r5,r6,r10,r11,pc}
    // where the "movw" shouldn't be part of the unwind codes. See genFnEpilog() for more details.

    bool unwindStarted = false;

    /* The saved regs info saves the LR register. We need to pop the PC register to return */
    assert(genFuncletInfo.fiSaveRegs & RBM_LR);

    regMaskTP maskPopRegsFloat = genFuncletInfo.fiSaveRegs & RBM_ALLFLOAT;
    regMaskTP maskPopRegsInt   = genFuncletInfo.fiSaveRegs & ~maskPopRegsFloat;

    regMaskTP maskStackAlloc = genStackAllocRegisterMask(genFuncletInfo.fiSpDelta, maskPopRegsFloat);
    maskPopRegsInt |= maskStackAlloc;

    if (maskStackAlloc == RBM_NONE)
    {
        genFreeLclFrame(genFuncletInfo.fiSpDelta, &unwindStarted);
    }

    if (!unwindStarted)
    {
        // We'll definitely generate an unwindable instruction next
        compiler->unwindBegEpilog();
        unwindStarted = true;
    }

    maskPopRegsInt &= ~RBM_LR;
    maskPopRegsInt |= RBM_PC;

    if (maskPopRegsFloat != RBM_NONE)
    {
        genPopFltRegs(maskPopRegsFloat);
        compiler->unwindPopMaskFloat(maskPopRegsFloat);
    }

    assert(FitsIn<int>(maskPopRegsInt));
    inst_IV(INS_pop, (int)maskPopRegsInt);
    compiler->unwindPopMaskInt(maskPopRegsInt);

    compiler->unwindEndEpilog();
}

/*****************************************************************************
 *
 *  Capture the information used to generate the funclet prologs and epilogs.
 *  Note that all funclet prologs are identical, and all funclet epilogs are
 *  identical (per type: filters are identical, and non-filters are identical).
 *  Thus, we compute the data used for these just once.
 *
 *  See genFuncletProlog() for more information about the prolog/epilog sequences.
 */

void CodeGen::genCaptureFuncletPrologEpilogInfo()
{
    if (compiler->ehAnyFunclets())
    {
        assert(isFramePointerUsed());
        assert(compiler->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT); // The frame size and offsets must be
                                                                              // finalized

        // Frame pointer doesn't point at the end, it points at the pushed r11. So, instead
        // of adding the number of callee-saved regs to CallerSP, we add 1 for lr and 1 for r11
        // (plus the "pre spill regs"). Note that we assume r12 and r13 aren't saved
        // (also assumed in genFnProlog()).
        assert((regSet.rsMaskCalleeSaved & (RBM_R12 | RBM_R13)) == 0);
        unsigned preSpillRegArgSize                = genCountBits(regSet.rsMaskPreSpillRegs(true)) * REGSIZE_BYTES;
        genFuncletInfo.fiFunctionCallerSPtoFPdelta = preSpillRegArgSize + 2 * REGSIZE_BYTES;

        regMaskTP rsMaskSaveRegs = regSet.rsMaskCalleeSaved;
        unsigned  saveRegsCount  = genCountBits(rsMaskSaveRegs);
        unsigned  saveRegsSize   = saveRegsCount * REGSIZE_BYTES; // bytes of regs we're saving
        assert(compiler->lvaOutgoingArgSpaceSize % REGSIZE_BYTES == 0);
        unsigned funcletFrameSize =
            preSpillRegArgSize + saveRegsSize + REGSIZE_BYTES /* PSP slot */ + compiler->lvaOutgoingArgSpaceSize;

        unsigned funcletFrameSizeAligned  = roundUp(funcletFrameSize, STACK_ALIGN);
        unsigned funcletFrameAlignmentPad = funcletFrameSizeAligned - funcletFrameSize;
        unsigned spDelta                  = funcletFrameSizeAligned - saveRegsSize;

        unsigned PSP_slot_SP_offset = compiler->lvaOutgoingArgSpaceSize + funcletFrameAlignmentPad;
        int      PSP_slot_CallerSP_offset =
            -(int)(funcletFrameSize - compiler->lvaOutgoingArgSpaceSize); // NOTE: it's negative!

        /* Now save it for future use */

        genFuncletInfo.fiSaveRegs                 = rsMaskSaveRegs;
        genFuncletInfo.fiSpDelta                  = spDelta;
        genFuncletInfo.fiPSP_slot_SP_offset       = PSP_slot_SP_offset;
        genFuncletInfo.fiPSP_slot_CallerSP_offset = PSP_slot_CallerSP_offset;

#ifdef DEBUG
        if (verbose)
        {
            printf("\n");
            printf("Funclet prolog / epilog info\n");
            printf("    Function CallerSP-to-FP delta: %d\n", genFuncletInfo.fiFunctionCallerSPtoFPdelta);
            printf("                        Save regs: ");
            dspRegMask(rsMaskSaveRegs);
            printf("\n");
            printf("                         SP delta: %d\n", genFuncletInfo.fiSpDelta);
            printf("               PSP slot SP offset: %d\n", genFuncletInfo.fiPSP_slot_SP_offset);
            printf("        PSP slot Caller SP offset: %d\n", genFuncletInfo.fiPSP_slot_CallerSP_offset);

            if (PSP_slot_CallerSP_offset != compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym))
            {
                printf("lvaGetCallerSPRelativeOffset(lvaPSPSym): %d\n",
                       compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym));
            }
        }
#endif // DEBUG

        assert(PSP_slot_CallerSP_offset < 0);
        if (compiler->lvaPSPSym != BAD_VAR_NUM)
        {
            assert(PSP_slot_CallerSP_offset ==
                   compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym)); // same offset used in main
                                                                                 // function and funclet!
        }
    }
}

#elif defined(TARGET_AMD64)

/*****************************************************************************
 *
 *  Generates code for an EH funclet prolog.
 *
 *  Funclets have the following incoming arguments:
 *
 *      catch/filter-handler: rcx = InitialSP, rdx = the exception object that was caught (see GT_CATCH_ARG)
 *      filter:               rcx = InitialSP, rdx = the exception object to filter (see GT_CATCH_ARG)
 *      finally/fault:        rcx = InitialSP
 *
 *  Funclets set the following registers on exit:
 *
 *      catch/filter-handler: rax = the address at which execution should resume (see BBJ_EHCATCHRET)
 *      filter:               rax = non-zero if the handler should handle the exception, zero otherwise (see GT_RETFILT)
 *      finally/fault:        none
 *
 *  The AMD64 funclet prolog sequence is:
 *
 *     push ebp
 *     push callee-saved regs
 *                      ; TODO-AMD64-CQ: We probably only need to save any callee-save registers that we actually use
 *                      ;         in the funclet. Currently, we save the same set of callee-saved regs calculated for
 *                      ;         the entire function.
 *     sub sp, XXX      ; Establish the rest of the frame.
 *                      ;   XXX is determined by lvaOutgoingArgSpaceSize plus space for the PSP slot, aligned
 *                      ;   up to preserve stack alignment. If we push an odd number of registers, we also
 *                      ;   generate this, to keep the stack aligned.
 *
 *     ; Fill the PSP slot, for use by the VM (it gets reported with the GC info), or by code generation of nested
 *     ;    filters.
 *     ; This is not part of the "OS prolog"; it has no associated unwind data, and is not reversed in the funclet
 *     ;    epilog.
 *     ; Also, re-establish the frame pointer from the PSP.
 *
 *     mov rbp, [rcx + PSP_slot_InitialSP_offset]       ; Load the PSP (InitialSP of the main function stored in the
 *                                                      ; PSP of the dynamically containing funclet or function)
 *     mov [rsp + PSP_slot_InitialSP_offset], rbp       ; store the PSP in our frame
 *     lea ebp, [rbp + Function_InitialSP_to_FP_delta]  ; re-establish the frame pointer of the parent frame. If
 *                                                      ; Function_InitialSP_to_FP_delta==0, we don't need this
 *                                                      ; instruction.
 *
 *  The epilog sequence is then:
 *
 *     add rsp, XXX
 *     pop callee-saved regs    ; if necessary
 *     pop rbp
 *     ret
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |    Return address     |
 *      |-----------------------|
 *      |      Saved EBP        |
 *      |-----------------------|
 *      |Callee saved registers |
 *      |-----------------------|
 *      ~  possible 8 byte pad  ~
 *      ~     for alignment     ~
 *      |-----------------------|
 *      |        PSP slot       | // Omitted in CoreRT ABI
 *      |-----------------------|
 *      |   Outgoing arg space  | // this only exists if the function makes a call
 *      |-----------------------| <---- Initial SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 * TODO-AMD64-Bug?: the frame pointer should really point to the PSP slot (the debugger seems to assume this
 * in DacDbiInterfaceImpl::InitParentFrameInfo()), or someplace above Initial-SP. There is an AMD64
 * UNWIND_INFO restriction that it must be within 240 bytes of Initial-SP. See jit64\amd64\inc\md.h
 * "FRAMEPTR OFFSETS" for details.
 */

void CodeGen::genFuncletProlog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletProlog()\n");
    }
#endif

    assert(!regSet.rsRegsModified(RBM_FPBASE));
    assert(block != nullptr);
    assert(block->bbFlags & BBF_FUNCLET_BEG);
    assert(isFramePointerUsed());

    ScopedSetVariable<bool> _setGeneratingProlog(&compiler->compGeneratingProlog, true);

    gcInfo.gcResetForBB();

    compiler->unwindBegProlog();

    // We need to push ebp, since it's callee-saved.
    // We need to push the callee-saved registers. We only need to push the ones that we need, but we don't
    // keep track of that on a per-funclet basis, so we push the same set as in the main function.
    // The only fixed-size frame we need to allocate is whatever is big enough for the PSPSym, since nothing else
    // is stored here (all temps are allocated in the parent frame).
    // We do need to allocate the outgoing argument space, in case there are calls here. This must be the same
    // size as the parent frame's outgoing argument space, to keep the PSPSym offset the same.

    inst_RV(INS_push, REG_FPBASE, TYP_REF);
    compiler->unwindPush(REG_FPBASE);

    // Callee saved int registers are pushed to stack.
    genPushCalleeSavedRegisters();

    regMaskTP maskArgRegsLiveIn;
    if ((block->bbCatchTyp == BBCT_FINALLY) || (block->bbCatchTyp == BBCT_FAULT))
    {
        maskArgRegsLiveIn = RBM_ARG_0;
    }
    else
    {
        maskArgRegsLiveIn = RBM_ARG_0 | RBM_ARG_2;
    }

    regNumber initReg       = REG_EBP; // We already saved EBP, so it can be trashed
    bool      initRegZeroed = false;

    genAllocLclFrame(genFuncletInfo.fiSpDelta, initReg, &initRegZeroed, maskArgRegsLiveIn);

    // Callee saved float registers are copied to stack in their assigned stack slots
    // after allocating space for them as part of funclet frame.
    genPreserveCalleeSavedFltRegs(genFuncletInfo.fiSpDelta);

    // This is the end of the OS-reported prolog for purposes of unwinding
    compiler->unwindEndProlog();

    // If there is no PSPSym (CoreRT ABI), we are done.
    if (compiler->lvaPSPSym == BAD_VAR_NUM)
    {
        return;
    }

    GetEmitter()->emitIns_R_AR(INS_mov, EA_PTRSIZE, REG_FPBASE, REG_ARG_0, genFuncletInfo.fiPSP_slot_InitialSP_offset);

    regSet.verifyRegUsed(REG_FPBASE);

    GetEmitter()->emitIns_AR_R(INS_mov, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, genFuncletInfo.fiPSP_slot_InitialSP_offset);

    if (genFuncletInfo.fiFunction_InitialSP_to_FP_delta != 0)
    {
        GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_FPBASE, REG_FPBASE,
                                   genFuncletInfo.fiFunction_InitialSP_to_FP_delta);
    }

    // We've modified EBP, but not really. Say that we haven't...
    regSet.rsRemoveRegsModified(RBM_FPBASE);
}

/*****************************************************************************
 *
 *  Generates code for an EH funclet epilog.
 *
 *  Note that we don't do anything with unwind codes, because AMD64 only cares about unwind codes for the prolog.
 */

void CodeGen::genFuncletEpilog()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletEpilog()\n");
    }
#endif

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    // Restore callee saved XMM regs from their stack slots before modifying SP
    // to position at callee saved int regs.
    genRestoreCalleeSavedFltRegs(genFuncletInfo.fiSpDelta);
    inst_RV_IV(INS_add, REG_SPBASE, genFuncletInfo.fiSpDelta, EA_PTRSIZE);
    genPopCalleeSavedRegisters();
    inst_RV(INS_pop, REG_EBP, TYP_I_IMPL);
    instGen_Return(0);
}

/*****************************************************************************
 *
 *  Capture the information used to generate the funclet prologs and epilogs.
 */

void CodeGen::genCaptureFuncletPrologEpilogInfo()
{
    if (!compiler->ehAnyFunclets())
    {
        return;
    }

    // Note that compLclFrameSize can't be used (for can we call functions that depend on it),
    // because we're not going to allocate the same size frame as the parent.

    assert(isFramePointerUsed());
    assert(compiler->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT); // The frame size and offsets must be
                                                                          // finalized
    assert(compiler->compCalleeFPRegsSavedMask != (regMaskTP)-1); // The float registers to be preserved is finalized

    // Even though lvaToInitialSPRelativeOffset() depends on compLclFrameSize,
    // that's ok, because we're figuring out an offset in the parent frame.
    genFuncletInfo.fiFunction_InitialSP_to_FP_delta =
        compiler->lvaToInitialSPRelativeOffset(0, true); // trick to find the Initial-SP-relative offset of the frame
                                                         // pointer.

    assert(compiler->lvaOutgoingArgSpaceSize % REGSIZE_BYTES == 0);
#ifndef UNIX_AMD64_ABI
    // No 4 slots for outgoing params on the stack for System V systems.
    assert((compiler->lvaOutgoingArgSpaceSize == 0) ||
           (compiler->lvaOutgoingArgSpaceSize >= (4 * REGSIZE_BYTES))); // On AMD64, we always have 4 outgoing argument
// slots if there are any calls in the function.
#endif // UNIX_AMD64_ABI
    unsigned offset = compiler->lvaOutgoingArgSpaceSize;

    genFuncletInfo.fiPSP_slot_InitialSP_offset = offset;

    // How much stack do we allocate in the funclet?
    // We need to 16-byte align the stack.

    unsigned totalFrameSize =
        REGSIZE_BYTES                                       // return address
        + REGSIZE_BYTES                                     // pushed EBP
        + (compiler->compCalleeRegsPushed * REGSIZE_BYTES); // pushed callee-saved int regs, not including EBP

    // Entire 128-bits of XMM register is saved to stack due to ABI encoding requirement.
    // Copying entire XMM register to/from memory will be performant if SP is aligned at XMM_REGSIZE_BYTES boundary.
    unsigned calleeFPRegsSavedSize = genCountBits(compiler->compCalleeFPRegsSavedMask) * XMM_REGSIZE_BYTES;
    unsigned FPRegsPad             = (calleeFPRegsSavedSize > 0) ? AlignmentPad(totalFrameSize, XMM_REGSIZE_BYTES) : 0;

    unsigned PSPSymSize = (compiler->lvaPSPSym != BAD_VAR_NUM) ? REGSIZE_BYTES : 0;

    totalFrameSize += FPRegsPad               // Padding before pushing entire xmm regs
                      + calleeFPRegsSavedSize // pushed callee-saved float regs
                      // below calculated 'pad' will go here
                      + PSPSymSize                        // PSPSym
                      + compiler->lvaOutgoingArgSpaceSize // outgoing arg space
        ;

    unsigned pad = AlignmentPad(totalFrameSize, 16);

    genFuncletInfo.fiSpDelta = FPRegsPad                           // Padding to align SP on XMM_REGSIZE_BYTES boundary
                               + calleeFPRegsSavedSize             // Callee saved xmm regs
                               + pad + PSPSymSize                  // PSPSym
                               + compiler->lvaOutgoingArgSpaceSize // outgoing arg space
        ;

#ifdef DEBUG
    if (verbose)
    {
        printf("\n");
        printf("Funclet prolog / epilog info\n");
        printf("   Function InitialSP-to-FP delta: %d\n", genFuncletInfo.fiFunction_InitialSP_to_FP_delta);
        printf("                         SP delta: %d\n", genFuncletInfo.fiSpDelta);
        printf("       PSP slot Initial SP offset: %d\n", genFuncletInfo.fiPSP_slot_InitialSP_offset);
    }

    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        assert(genFuncletInfo.fiPSP_slot_InitialSP_offset ==
               compiler->lvaGetInitialSPRelativeOffset(compiler->lvaPSPSym)); // same offset used in main function and
                                                                              // funclet!
    }
#endif // DEBUG
}

#elif defined(TARGET_ARM64)

// Look in CodeGenArm64.cpp

#elif defined(TARGET_X86)

/*****************************************************************************
 *
 *  Generates code for an EH funclet prolog.
 *
 *
 *  Funclets have the following incoming arguments:
 *
 *      catch/filter-handler: eax = the exception object that was caught (see GT_CATCH_ARG)
 *      filter:               eax = the exception object that was caught (see GT_CATCH_ARG)
 *      finally/fault:        none
 *
 *  Funclets set the following registers on exit:
 *
 *      catch/filter-handler: eax = the address at which execution should resume (see BBJ_EHCATCHRET)
 *      filter:               eax = non-zero if the handler should handle the exception, zero otherwise (see GT_RETFILT)
 *      finally/fault:        none
 *
 *  Funclet prolog/epilog sequence and funclet frame layout are TBD.
 *
 */

void CodeGen::genFuncletProlog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletProlog()\n");
    }
#endif

    ScopedSetVariable<bool> _setGeneratingProlog(&compiler->compGeneratingProlog, true);

    gcInfo.gcResetForBB();

    compiler->unwindBegProlog();

    // This is the end of the OS-reported prolog for purposes of unwinding
    compiler->unwindEndProlog();

    // TODO We may need EBP restore sequence here if we introduce PSPSym

    // Add a padding for 16-byte alignment
    inst_RV_IV(INS_sub, REG_SPBASE, 12, EA_PTRSIZE);
}

/*****************************************************************************
 *
 *  Generates code for an EH funclet epilog.
 */

void CodeGen::genFuncletEpilog()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletEpilog()\n");
    }
#endif

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    // Revert a padding that was added for 16-byte alignment
    inst_RV_IV(INS_add, REG_SPBASE, 12, EA_PTRSIZE);

    instGen_Return(0);
}

/*****************************************************************************
 *
 *  Capture the information used to generate the funclet prologs and epilogs.
 */

void CodeGen::genCaptureFuncletPrologEpilogInfo()
{
    if (!compiler->ehAnyFunclets())
    {
        return;
    }
}

#else // TARGET*

/*****************************************************************************
 *
 *  Generates code for an EH funclet prolog.
 */

void CodeGen::genFuncletProlog(BasicBlock* block)
{
    NYI("Funclet prolog");
}

/*****************************************************************************
 *
 *  Generates code for an EH funclet epilog.
 */

void CodeGen::genFuncletEpilog()
{
    NYI("Funclet epilog");
}

/*****************************************************************************
 *
 *  Capture the information used to generate the funclet prologs and epilogs.
 */

void CodeGen::genCaptureFuncletPrologEpilogInfo()
{
    if (compiler->ehAnyFunclets())
    {
        NYI("genCaptureFuncletPrologEpilogInfo()");
    }
}

#endif // TARGET*

/*-----------------------------------------------------------------------------
 *
 *  Set the main function PSPSym value in the frame.
 *  Funclets use different code to load the PSP sym and save it in their frame.
 *  See the document "X64 and ARM ABIs.docx" for a full description of the PSPSym.
 *  The PSPSym section of that document is copied here.
 *
 ***********************************
 *  The name PSPSym stands for Previous Stack Pointer Symbol.  It is how a funclet
 *  accesses locals from the main function body.
 *
 *  First, two definitions.
 *
 *  Caller-SP is the value of the stack pointer in a function's caller before the call
 *  instruction is executed. That is, when function A calls function B, Caller-SP for B
 *  is the value of the stack pointer immediately before the call instruction in A
 *  (calling B) was executed. Note that this definition holds for both AMD64, which
 *  pushes the return value when a call instruction is executed, and for ARM, which
 *  doesn't. For AMD64, Caller-SP is the address above the call return address.
 *
 *  Initial-SP is the initial value of the stack pointer after the fixed-size portion of
 *  the frame has been allocated. That is, before any "alloca"-type allocations.
 *
 *  The PSPSym is a pointer-sized local variable in the frame of the main function and
 *  of each funclet. The value stored in PSPSym is the value of Initial-SP/Caller-SP
 *  for the main function.  The stack offset of the PSPSym is reported to the VM in the
 *  GC information header.  The value reported in the GC information is the offset of the
 *  PSPSym from Initial-SP/Caller-SP. (Note that both the value stored, and the way the
 *  value is reported to the VM, differs between architectures. In particular, note that
 *  most things in the GC information header are reported as offsets relative to Caller-SP,
 *  but PSPSym on AMD64 is one (maybe the only) exception.)
 *
 *  The VM uses the PSPSym to find other locals it cares about (such as the generics context
 *  in a funclet frame). The JIT uses it to re-establish the frame pointer register, so that
 *  the frame pointer is the same value in a funclet as it is in the main function body.
 *
 *  When a funclet is called, it is passed the Establisher Frame Pointer. For AMD64 this is
 *  true for all funclets and it is passed as the first argument in RCX, but for ARM this is
 *  only true for first pass funclets (currently just filters) and it is passed as the second
 *  argument in R1. The Establisher Frame Pointer is a stack pointer of an interesting "parent"
 *  frame in the exception processing system. For the CLR, it points either to the main function
 *  frame or a dynamically enclosing funclet frame from the same function, for the funclet being
 *  invoked. The value of the Establisher Frame Pointer is Initial-SP on AMD64, Caller-SP on ARM.
 *
 *  Using the establisher frame, the funclet wants to load the value of the PSPSym. Since we
 *  don't know if the Establisher Frame is from the main function or a funclet, we design the
 *  main function and funclet frame layouts to place the PSPSym at an identical, small, constant
 *  offset from the Establisher Frame in each case. (This is also required because we only report
 *  a single offset to the PSPSym in the GC information, and that offset must be valid for the main
 *  function and all of its funclets). Then, the funclet uses this known offset to compute the
 *  PSPSym address and read its value. From this, it can compute the value of the frame pointer
 *  (which is a constant offset from the PSPSym value) and set the frame register to be the same
 *  as the parent function. Also, the funclet writes the value of the PSPSym to its own frame's
 *  PSPSym. This "copying" of the PSPSym happens for every funclet invocation, in particular,
 *  for every nested funclet invocation.
 *
 *  On ARM, for all second pass funclets (finally, fault, catch, and filter-handler) the VM
 *  restores all non-volatile registers to their values within the parent frame. This includes
 *  the frame register (R11). Thus, the PSPSym is not used to recompute the frame pointer register
 *  in this case, though the PSPSym is copied to the funclet's frame, as for all funclets.
 *
 *  Catch, Filter, and Filter-handlers also get an Exception object (GC ref) as an argument
 *  (REG_EXCEPTION_OBJECT).  On AMD64 it is the second argument and thus passed in RDX.  On
 *  ARM this is the first argument and passed in R0.
 *
 *  (Note that the JIT64 source code contains a comment that says, "The current CLR doesn't always
 *  pass the correct establisher frame to the funclet. Funclet may receive establisher frame of
 *  funclet when expecting that of original routine." It indicates this is the reason that a PSPSym
 *  is required in all funclets as well as the main function, whereas if the establisher frame was
 *  correctly reported, the PSPSym could be omitted in some cases.)
 ***********************************
 */
void CodeGen::genSetPSPSym(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    if (compiler->lvaPSPSym == BAD_VAR_NUM)
    {
        return;
    }

    noway_assert(isFramePointerUsed()); // We need an explicit frame pointer

#if defined(TARGET_ARM)

    // We either generate:
    //     add     r1, r11, 8
    //     str     r1, [reg + PSPSymOffset]
    // or:
    //     add     r1, sp, 76
    //     str     r1, [reg + PSPSymOffset]
    // depending on the smallest encoding

    int SPtoCallerSPdelta = -genCallerSPtoInitialSPdelta();

    int       callerSPOffs;
    regNumber regBase;

    if (arm_Valid_Imm_For_Add_SP(SPtoCallerSPdelta))
    {
        // use the "add <reg>, sp, imm" form

        callerSPOffs = SPtoCallerSPdelta;
        regBase      = REG_SPBASE;
    }
    else
    {
        // use the "add <reg>, r11, imm" form

        int FPtoCallerSPdelta = -genCallerSPtoFPdelta();
        noway_assert(arm_Valid_Imm_For_Add(FPtoCallerSPdelta, INS_FLAGS_DONT_CARE));

        callerSPOffs = FPtoCallerSPdelta;
        regBase      = REG_FPBASE;
    }

    // We will just use the initReg since it is an available register
    // and we are probably done using it anyway...
    regNumber regTmp = initReg;
    *pInitRegZeroed  = false;

    GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, regTmp, regBase, callerSPOffs);
    GetEmitter()->emitIns_S_R(INS_str, EA_PTRSIZE, regTmp, compiler->lvaPSPSym, 0);

#elif defined(TARGET_ARM64)

    int SPtoCallerSPdelta = -genCallerSPtoInitialSPdelta();

    // We will just use the initReg since it is an available register
    // and we are probably done using it anyway...
    regNumber regTmp = initReg;
    *pInitRegZeroed  = false;

    GetEmitter()->emitIns_R_R_Imm(INS_add, EA_PTRSIZE, regTmp, REG_SPBASE, SPtoCallerSPdelta);
    GetEmitter()->emitIns_S_R(INS_str, EA_PTRSIZE, regTmp, compiler->lvaPSPSym, 0);

#elif defined(TARGET_AMD64)

    // The PSP sym value is Initial-SP, not Caller-SP!
    // We assume that RSP is Initial-SP when this function is called. That is, the stack frame
    // has been established.
    //
    // We generate:
    //     mov     [rbp-20h], rsp       // store the Initial-SP (our current rsp) in the PSPsym

    GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaPSPSym, 0);

#else // TARGET*

    NYI("Set function PSP sym");

#endif // TARGET*
}

#endif // FEATURE_EH_FUNCLETS

/*****************************************************************************
 *
 *  Generates code for all the function and funclet prologs and epilogs.
 */

void CodeGen::genGeneratePrologsAndEpilogs()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** Before prolog / epilog generation\n");
        GetEmitter()->emitDispIGlist(false);
    }
#endif

    // Before generating the prolog, we need to reset the variable locations to what they will be on entry.
    // This affects our code that determines which untracked locals need to be zero initialized.
    compiler->m_pLinearScan->recordVarLocationsAtStartOfBB(compiler->fgFirstBB);

    // Tell the emitter we're done with main code generation, and are going to start prolog and epilog generation.

    GetEmitter()->emitStartPrologEpilogGeneration();

    gcInfo.gcResetForBB();
    genFnProlog();

    // Generate all the prologs and epilogs.
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(FEATURE_EH_FUNCLETS)

    // Capture the data we're going to use in the funclet prolog and epilog generation. This is
    // information computed during codegen, or during function prolog generation, like
    // frame offsets. It must run after main function prolog generation.

    genCaptureFuncletPrologEpilogInfo();

#endif // FEATURE_EH_FUNCLETS

    // Walk the list of prologs and epilogs and generate them.
    // We maintain a list of prolog and epilog basic blocks in
    // the insGroup structure in the emitter. This list was created
    // during code generation by the genReserve*() functions.
    //
    // TODO: it seems like better design would be to create a list of prologs/epilogs
    // in the code generator (not the emitter), and then walk that list. But we already
    // have the insGroup list, which serves well, so we don't need the extra allocations
    // for a prolog/epilog list in the code generator.

    GetEmitter()->emitGeneratePrologEpilog();

    // Tell the emitter we're done with all prolog and epilog generation.

    GetEmitter()->emitFinishPrologEpilogGeneration();

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** After prolog / epilog generation\n");
        GetEmitter()->emitDispIGlist(false);
    }
#endif
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           End Prolog / Epilog                             XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#if defined(TARGET_XARCH)
// Save compCalleeFPRegsPushed with the smallest register number saved at [RSP+offset], working
// down the stack to the largest register number stored at [RSP+offset-(genCountBits(regMask)-1)*XMM_REG_SIZE]
// Here offset = 16-byte aligned offset after pushing integer registers.
//
// Params
//   lclFrameSize - Fixed frame size excluding callee pushed int regs.
//             non-funclet: this will be compLclFrameSize.
//             funclet frames: this will be FuncletInfo.fiSpDelta.
void CodeGen::genPreserveCalleeSavedFltRegs(unsigned lclFrameSize)
{
    genVzeroupperIfNeeded(false);
    regMaskTP regMask = compiler->compCalleeFPRegsSavedMask;

    // Only callee saved floating point registers should be in regMask
    assert((regMask & RBM_FLT_CALLEE_SAVED) == regMask);

    // fast path return
    if (regMask == RBM_NONE)
    {
        return;
    }

#ifdef TARGET_AMD64
    unsigned firstFPRegPadding = compiler->lvaIsCalleeSavedIntRegCountEven() ? REGSIZE_BYTES : 0;
    unsigned offset            = lclFrameSize - firstFPRegPadding - XMM_REGSIZE_BYTES;

    // Offset is 16-byte aligned since we use movaps for preserving xmm regs.
    assert((offset % 16) == 0);
    instruction copyIns = ins_Copy(TYP_FLOAT);
#else  // !TARGET_AMD64
    unsigned    offset            = lclFrameSize - XMM_REGSIZE_BYTES;
    instruction copyIns           = INS_movupd;
#endif // !TARGET_AMD64

    for (regNumber reg = REG_FLT_CALLEE_SAVED_FIRST; regMask != RBM_NONE; reg = REG_NEXT(reg))
    {
        regMaskTP regBit = genRegMask(reg);
        if ((regBit & regMask) != 0)
        {
            // ABI requires us to preserve lower 128-bits of YMM register.
            GetEmitter()->emitIns_AR_R(copyIns,
                                       EA_8BYTE, // TODO-XArch-Cleanup: size specified here doesn't matter but should be
                                                 // EA_16BYTE
                                       reg, REG_SPBASE, offset);
            compiler->unwindSaveReg(reg, offset);
            regMask &= ~regBit;
            offset -= XMM_REGSIZE_BYTES;
        }
    }
}

// Save/Restore compCalleeFPRegsPushed with the smallest register number saved at [RSP+offset], working
// down the stack to the largest register number stored at [RSP+offset-(genCountBits(regMask)-1)*XMM_REG_SIZE]
// Here offset = 16-byte aligned offset after pushing integer registers.
//
// Params
//   lclFrameSize - Fixed frame size excluding callee pushed int regs.
//             non-funclet: this will be compLclFrameSize.
//             funclet frames: this will be FuncletInfo.fiSpDelta.
void CodeGen::genRestoreCalleeSavedFltRegs(unsigned lclFrameSize)
{
    regMaskTP regMask = compiler->compCalleeFPRegsSavedMask;

    // Only callee saved floating point registers should be in regMask
    assert((regMask & RBM_FLT_CALLEE_SAVED) == regMask);

    // fast path return
    if (regMask == RBM_NONE)
    {
        genVzeroupperIfNeeded();
        return;
    }

#ifdef TARGET_AMD64
    unsigned    firstFPRegPadding = compiler->lvaIsCalleeSavedIntRegCountEven() ? REGSIZE_BYTES : 0;
    instruction copyIns           = ins_Copy(TYP_FLOAT);
#else  // !TARGET_AMD64
    unsigned    firstFPRegPadding = 0;
    instruction copyIns           = INS_movupd;
#endif // !TARGET_AMD64

    unsigned  offset;
    regNumber regBase;
    if (compiler->compLocallocUsed)
    {
        // localloc frame: use frame pointer relative offset
        assert(isFramePointerUsed());
        regBase = REG_FPBASE;
        offset  = lclFrameSize - genSPtoFPdelta() - firstFPRegPadding - XMM_REGSIZE_BYTES;
    }
    else
    {
        regBase = REG_SPBASE;
        offset  = lclFrameSize - firstFPRegPadding - XMM_REGSIZE_BYTES;
    }

#ifdef TARGET_AMD64
    // Offset is 16-byte aligned since we use movaps for restoring xmm regs
    assert((offset % 16) == 0);
#endif // TARGET_AMD64

    for (regNumber reg = REG_FLT_CALLEE_SAVED_FIRST; regMask != RBM_NONE; reg = REG_NEXT(reg))
    {
        regMaskTP regBit = genRegMask(reg);
        if ((regBit & regMask) != 0)
        {
            // ABI requires us to restore lower 128-bits of YMM register.
            GetEmitter()->emitIns_R_AR(copyIns,
                                       EA_8BYTE, // TODO-XArch-Cleanup: size specified here doesn't matter but should be
                                                 // EA_16BYTE
                                       reg, regBase, offset);
            regMask &= ~regBit;
            offset -= XMM_REGSIZE_BYTES;
        }
    }
    genVzeroupperIfNeeded();
}

// Generate Vzeroupper instruction as needed to zero out upper 128b-bit of all YMM registers so that the
// AVX/Legacy SSE transition penalties can be avoided. This function is been used in genPreserveCalleeSavedFltRegs
// (prolog) and genRestoreCalleeSavedFltRegs (epilog). Issue VZEROUPPER in Prolog if the method contains
// 128-bit or 256-bit AVX code, to avoid legacy SSE to AVX transition penalty, which could happen when native
// code contains legacy SSE code calling into JIT AVX code (e.g. reverse pinvoke). Issue VZEROUPPER in Epilog
// if the method contains 256-bit AVX code, to avoid AVX to legacy SSE transition penalty.
//
// Params
//   check256bitOnly  - true to check if the function contains 256-bit AVX instruction and generate Vzeroupper
//      instruction, false to check if the function contains AVX instruciton (either 128-bit or 256-bit).
//
void CodeGen::genVzeroupperIfNeeded(bool check256bitOnly /* = true*/)
{
    bool emitVzeroUpper = false;
    if (check256bitOnly)
    {
        emitVzeroUpper = GetEmitter()->Contains256bitAVX();
    }
    else
    {
        emitVzeroUpper = GetEmitter()->ContainsAVX();
    }

    if (emitVzeroUpper)
    {
        assert(compiler->canUseVexEncoding());
        instGen(INS_vzeroupper);
    }
}

#endif // defined(TARGET_XARCH)

//-----------------------------------------------------------------------------------
// IsMultiRegReturnedType: Returns true if the type is returned in multiple registers
//
// Arguments:
//     hClass   -  type handle
//
// Return Value:
//     true if type is returned in multiple registers, false otherwise.
//
bool Compiler::IsMultiRegReturnedType(CORINFO_CLASS_HANDLE hClass, CorInfoCallConvExtension callConv)
{
    if (hClass == NO_CLASS_HANDLE)
    {
        return false;
    }

    structPassingKind howToReturnStruct;
    var_types         returnType = getReturnTypeForStruct(hClass, callConv, &howToReturnStruct);

#ifdef TARGET_ARM64
    return (varTypeIsStruct(returnType) && (howToReturnStruct != SPK_PrimitiveType));
#else
    return (varTypeIsStruct(returnType));
#endif
}

//----------------------------------------------
// Methods that support HFA's for ARM32/ARM64
//----------------------------------------------

bool Compiler::IsHfa(CORINFO_CLASS_HANDLE hClass)
{
    return varTypeIsValidHfaType(GetHfaType(hClass));
}

bool Compiler::IsHfa(GenTree* tree)
{
    if (GlobalJitOptions::compFeatureHfa)
    {
        return IsHfa(gtGetStructHandleIfPresent(tree));
    }
    else
    {
        return false;
    }
}

var_types Compiler::GetHfaType(GenTree* tree)
{
    if (GlobalJitOptions::compFeatureHfa)
    {
        return GetHfaType(gtGetStructHandleIfPresent(tree));
    }
    else
    {
        return TYP_UNDEF;
    }
}

unsigned Compiler::GetHfaCount(GenTree* tree)
{
    return GetHfaCount(gtGetStructHandle(tree));
}

var_types Compiler::GetHfaType(CORINFO_CLASS_HANDLE hClass)
{
    if (GlobalJitOptions::compFeatureHfa)
    {
        if (hClass != NO_CLASS_HANDLE)
        {
            CorInfoHFAElemType elemKind = info.compCompHnd->getHFAType(hClass);
            if (elemKind != CORINFO_HFA_ELEM_NONE)
            {
                // This type may not appear elsewhere, but it will occupy a floating point register.
                compFloatingPointUsed = true;
            }
            return HfaTypeFromElemKind(elemKind);
        }
    }
    return TYP_UNDEF;
}

//------------------------------------------------------------------------
// GetHfaCount: Given a  class handle for an HFA struct
//    return the number of registers needed to hold the HFA
//
//    Note that on ARM32 the single precision registers overlap with
//        the double precision registers and for that reason each
//        double register is considered to be two single registers.
//        Thus for ARM32 an HFA of 4 doubles this function will return 8.
//    On ARM64 given an HFA of 4 singles or 4 doubles this function will
//         will return 4 for both.
// Arguments:
//    hClass: the class handle of a HFA struct
//
unsigned Compiler::GetHfaCount(CORINFO_CLASS_HANDLE hClass)
{
    assert(IsHfa(hClass));
#ifdef TARGET_ARM
    // A HFA of doubles is twice as large as an HFA of singles for ARM32
    // (i.e. uses twice the number of single precison registers)
    return info.compCompHnd->getClassSize(hClass) / REGSIZE_BYTES;
#else  // TARGET_ARM64
    var_types hfaType   = GetHfaType(hClass);
    unsigned  classSize = info.compCompHnd->getClassSize(hClass);
    // Note that the retail build issues a warning about a potential divsion by zero without the Max function
    unsigned elemSize = Max((unsigned)1, EA_SIZE_IN_BYTES(emitActualTypeSize(hfaType)));
    return classSize / elemSize;
#endif // TARGET_ARM64
}

#ifdef TARGET_XARCH

//------------------------------------------------------------------------
// genMapShiftInsToShiftByConstantIns: Given a general shift/rotate instruction,
// map it to the specific x86/x64 shift opcode for a shift/rotate by a constant.
// X86/x64 has a special encoding for shift/rotate-by-constant-1.
//
// Arguments:
//    ins: the base shift/rotate instruction
//    shiftByValue: the constant value by which we are shifting/rotating
//
instruction CodeGen::genMapShiftInsToShiftByConstantIns(instruction ins, int shiftByValue)
{
    assert(ins == INS_rcl || ins == INS_rcr || ins == INS_rol || ins == INS_ror || ins == INS_shl || ins == INS_shr ||
           ins == INS_sar);

    // Which format should we use?

    instruction shiftByConstantIns;

    if (shiftByValue == 1)
    {
        // Use the shift-by-one format.

        assert(INS_rcl + 1 == INS_rcl_1);
        assert(INS_rcr + 1 == INS_rcr_1);
        assert(INS_rol + 1 == INS_rol_1);
        assert(INS_ror + 1 == INS_ror_1);
        assert(INS_shl + 1 == INS_shl_1);
        assert(INS_shr + 1 == INS_shr_1);
        assert(INS_sar + 1 == INS_sar_1);

        shiftByConstantIns = (instruction)(ins + 1);
    }
    else
    {
        // Use the shift-by-NNN format.

        assert(INS_rcl + 2 == INS_rcl_N);
        assert(INS_rcr + 2 == INS_rcr_N);
        assert(INS_rol + 2 == INS_rol_N);
        assert(INS_ror + 2 == INS_ror_N);
        assert(INS_shl + 2 == INS_shl_N);
        assert(INS_shr + 2 == INS_shr_N);
        assert(INS_sar + 2 == INS_sar_N);

        shiftByConstantIns = (instruction)(ins + 2);
    }

    return shiftByConstantIns;
}

#endif // TARGET_XARCH

//------------------------------------------------------------------------------------------------ //
// getFirstArgWithStackSlot - returns the first argument with stack slot on the caller's frame.
//
// Return value:
//    The number of the first argument with stack slot on the caller's frame.
//
// Note:
//    On x64 Windows the caller always creates slots (homing space) in its frame for the
//    first 4 arguments of a callee (register passed args). So, the the variable number
//    (lclNum) for the first argument with a stack slot is always 0.
//    For System V systems or armarch, there is no such calling convention requirement, and the code
//    needs to find the first stack passed argument from the caller. This is done by iterating over
//    all the lvParam variables and finding the first with GetArgReg() equals to REG_STK.
//
unsigned CodeGen::getFirstArgWithStackSlot()
{
#if defined(UNIX_AMD64_ABI) || defined(TARGET_ARMARCH)
    unsigned baseVarNum = 0;
    // Iterate over all the lvParam variables in the Lcl var table until we find the first one
    // that's passed on the stack.
    LclVarDsc* varDsc = nullptr;
    for (unsigned i = 0; i < compiler->info.compArgsCount; i++)
    {
        varDsc = &(compiler->lvaTable[i]);

        // We should have found a stack parameter (and broken out of this loop) before
        // we find any non-parameters.
        assert(varDsc->lvIsParam);

        if (varDsc->GetArgReg() == REG_STK)
        {
            baseVarNum = i;
            break;
        }
    }
    assert(varDsc != nullptr);

    return baseVarNum;
#elif defined(TARGET_AMD64)
    return 0;
#else  // TARGET_X86
    // Not implemented for x86.
    NYI_X86("getFirstArgWithStackSlot not yet implemented for x86.");
    return BAD_VAR_NUM;
#endif // TARGET_X86
}

//------------------------------------------------------------------------
// genSinglePush: Report a change in stack level caused by a single word-sized push instruction
//
void CodeGen::genSinglePush()
{
    AddStackLevel(REGSIZE_BYTES);
}

//------------------------------------------------------------------------
// genSinglePop: Report a change in stack level caused by a single word-sized pop instruction
//
void CodeGen::genSinglePop()
{
    SubtractStackLevel(REGSIZE_BYTES);
}

//------------------------------------------------------------------------
// genPushRegs: Push the given registers.
//
// Arguments:
//    regs - mask or registers to push
//    byrefRegs - OUT arg. Set to byref registers that were pushed.
//    noRefRegs - OUT arg. Set to non-GC ref registers that were pushed.
//
// Return Value:
//    Mask of registers pushed.
//
// Notes:
//    This function does not check if the register is marked as used, etc.
//
regMaskTP CodeGen::genPushRegs(regMaskTP regs, regMaskTP* byrefRegs, regMaskTP* noRefRegs)
{
    *byrefRegs = RBM_NONE;
    *noRefRegs = RBM_NONE;

    if (regs == RBM_NONE)
    {
        return RBM_NONE;
    }

#if FEATURE_FIXED_OUT_ARGS

    NYI("Don't call genPushRegs with real regs!");
    return RBM_NONE;

#else // FEATURE_FIXED_OUT_ARGS

    noway_assert(genTypeStSz(TYP_REF) == genTypeStSz(TYP_I_IMPL));
    noway_assert(genTypeStSz(TYP_BYREF) == genTypeStSz(TYP_I_IMPL));

    regMaskTP pushedRegs = regs;

    for (regNumber reg = REG_INT_FIRST; regs != RBM_NONE; reg = REG_NEXT(reg))
    {
        regMaskTP regBit = regMaskTP(1) << reg;

        if ((regBit & regs) == RBM_NONE)
            continue;

        var_types type;
        if (regBit & gcInfo.gcRegGCrefSetCur)
        {
            type = TYP_REF;
        }
        else if (regBit & gcInfo.gcRegByrefSetCur)
        {
            *byrefRegs |= regBit;
            type = TYP_BYREF;
        }
        else if (noRefRegs != NULL)
        {
            *noRefRegs |= regBit;
            type = TYP_I_IMPL;
        }
        else
        {
            continue;
        }

        inst_RV(INS_push, reg, type);

        genSinglePush();
        gcInfo.gcMarkRegSetNpt(regBit);

        regs &= ~regBit;
    }

    return pushedRegs;

#endif // FEATURE_FIXED_OUT_ARGS
}

//------------------------------------------------------------------------
// genPopRegs: Pop the registers that were pushed by genPushRegs().
//
// Arguments:
//    regs - mask of registers to pop
//    byrefRegs - The byref registers that were pushed by genPushRegs().
//    noRefRegs - The non-GC ref registers that were pushed by genPushRegs().
//
// Return Value:
//    None
//
void CodeGen::genPopRegs(regMaskTP regs, regMaskTP byrefRegs, regMaskTP noRefRegs)
{
    if (regs == RBM_NONE)
    {
        return;
    }

#if FEATURE_FIXED_OUT_ARGS

    NYI("Don't call genPopRegs with real regs!");

#else // FEATURE_FIXED_OUT_ARGS

    noway_assert((regs & byrefRegs) == byrefRegs);
    noway_assert((regs & noRefRegs) == noRefRegs);
    noway_assert((regs & (gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur)) == RBM_NONE);

    noway_assert(genTypeStSz(TYP_REF) == genTypeStSz(TYP_INT));
    noway_assert(genTypeStSz(TYP_BYREF) == genTypeStSz(TYP_INT));

    // Walk the registers in the reverse order as genPushRegs()
    for (regNumber reg = REG_INT_LAST; regs != RBM_NONE; reg = REG_PREV(reg))
    {
        regMaskTP regBit = regMaskTP(1) << reg;

        if ((regBit & regs) == RBM_NONE)
            continue;

        var_types type;
        if (regBit & byrefRegs)
        {
            type = TYP_BYREF;
        }
        else if (regBit & noRefRegs)
        {
            type = TYP_INT;
        }
        else
        {
            type = TYP_REF;
        }

        inst_RV(INS_pop, reg, type);
        genSinglePop();

        if (type != TYP_INT)
            gcInfo.gcMarkRegPtrVal(reg, type);

        regs &= ~regBit;
    }

#endif // FEATURE_FIXED_OUT_ARGS
}

/*****************************************************************************
 *                          genSetScopeInfo
 *
 * This function should be called only after the sizes of the emitter blocks
 * have been finalized.
 */

void CodeGen::genSetScopeInfo()
{
    if (!compiler->opts.compScopeInfo)
    {
        return;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genSetScopeInfo()\n");
    }
#endif

    unsigned varsLocationsCount = 0;

#ifdef USING_SCOPE_INFO
    if (compiler->info.compVarScopesCount > 0)
    {
        varsLocationsCount = siScopeCnt + psiScopeCnt;
    }
#else // USING_SCOPE_INFO

#ifdef USING_VARIABLE_LIVE_RANGE
    varsLocationsCount = (unsigned int)varLiveKeeper->getLiveRangesCount();
#endif // USING_VARIABLE_LIVE_RANGE

#endif // USING_SCOPE_INFO

    if (varsLocationsCount == 0)
    {
        // No variable home to report
        compiler->eeSetLVcount(0);
        compiler->eeSetLVdone();
        return;
    }

    noway_assert(compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0));

    // Initialize the table where the reported variables' home will be placed.
    compiler->eeSetLVcount(varsLocationsCount);

#ifdef DEBUG
    genTrnslLocalVarCount = varsLocationsCount;
    if (varsLocationsCount)
    {
        genTrnslLocalVarInfo = new (compiler, CMK_DebugOnly) TrnslLocalVarInfo[varsLocationsCount];
    }
#endif

#ifdef USING_SCOPE_INFO
    genSetScopeInfoUsingsiScope();
#else // USING_SCOPE_INFO
#ifdef USING_VARIABLE_LIVE_RANGE
    // We can have one of both flags defined, both, or none. Specially if we need to compare both
    // both results. But we cannot report both to the debugger, since there would be overlapping
    // intervals, and may not indicate the same variable location.

    genSetScopeInfoUsingVariableRanges();

#endif // USING_VARIABLE_LIVE_RANGE
#endif // USING_SCOPE_INFO

    compiler->eeSetLVdone();
}

#ifdef USING_SCOPE_INFO
void CodeGen::genSetScopeInfoUsingsiScope()
{
    noway_assert(psiOpenScopeList.scNext == nullptr);

    // Record the scopes found for the parameters over the prolog.
    // The prolog needs to be treated differently as a variable may not
    // have the same info in the prolog block as is given by compiler->lvaTable.
    // eg. A register parameter is actually on the stack, before it is loaded to reg.

    CodeGen::psiScope* scopeP;
    unsigned           i;

    for (i = 0, scopeP = psiScopeList.scNext; i < psiScopeCnt; i++, scopeP = scopeP->scNext)
    {
        noway_assert(scopeP != nullptr);
        noway_assert(scopeP->scStartLoc.Valid());
        noway_assert(scopeP->scEndLoc.Valid());

        UNATIVE_OFFSET startOffs = scopeP->scStartLoc.CodeOffset(GetEmitter());
        UNATIVE_OFFSET endOffs   = scopeP->scEndLoc.CodeOffset(GetEmitter());

        unsigned varNum = scopeP->scSlotNum;
        noway_assert(startOffs <= endOffs);

        // The range may be 0 if the prolog is empty. For such a case,
        // report the liveness of arguments to span at least the first
        // instruction in the method. This will be incorrect (except on
        // entry to the method) if the very first instruction of the method
        // is part of a loop. However, this should happen
        // very rarely, and the incorrectness is worth being able to look
        // at the argument on entry to the method.
        if (startOffs == endOffs)
        {
            noway_assert(startOffs == 0);
            endOffs++;
        }

        siVarLoc varLoc = scopeP->getSiVarLoc();

        genSetScopeInfo(i, startOffs, endOffs - startOffs, varNum, scopeP->scLVnum, true, &varLoc);
    }

    // Record the scopes for the rest of the method.
    // Check that the LocalVarInfo scopes look OK
    noway_assert(siOpenScopeList.scNext == nullptr);

    CodeGen::siScope* scopeL;

    for (i = 0, scopeL = siScopeList.scNext; i < siScopeCnt; i++, scopeL = scopeL->scNext)
    {
        noway_assert(scopeL != nullptr);
        noway_assert(scopeL->scStartLoc.Valid());
        noway_assert(scopeL->scEndLoc.Valid());

        // Find the start and end IP

        UNATIVE_OFFSET startOffs = scopeL->scStartLoc.CodeOffset(GetEmitter());
        UNATIVE_OFFSET endOffs   = scopeL->scEndLoc.CodeOffset(GetEmitter());

        noway_assert(scopeL->scStartLoc != scopeL->scEndLoc);

        LclVarDsc* varDsc = compiler->lvaGetDesc(scopeL->scVarNum);
        siVarLoc   varLoc = getSiVarLoc(varDsc, scopeL);

        genSetScopeInfo(psiScopeCnt + i, startOffs, endOffs - startOffs, scopeL->scVarNum, scopeL->scLVnum, false,
                        &varLoc);
    }
}
#endif // USING_SCOPE_INFO

#ifdef USING_VARIABLE_LIVE_RANGE
//------------------------------------------------------------------------
// genSetScopeInfoUsingVariableRanges: Call "genSetScopeInfo" with the
//  "VariableLiveRanges" created for the arguments, special arguments and
//  IL local variables.
//
// Notes:
//  This function is called from "genSetScopeInfo" once the code is generated
//  and we want to send debug info to the debugger.
//
void CodeGen::genSetScopeInfoUsingVariableRanges()
{
    unsigned int liveRangeIndex = 0;

    for (unsigned int varNum = 0; varNum < compiler->info.compLocalsCount; varNum++)
    {
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);

        if (compiler->compMap2ILvarNum(varNum) != (unsigned int)ICorDebugInfo::UNKNOWN_ILNUM)
        {
            VariableLiveKeeper::LiveRangeList* liveRanges = nullptr;

            for (int rangeIndex = 0; rangeIndex < 2; rangeIndex++)
            {
                if (rangeIndex == 0)
                {
                    liveRanges = varLiveKeeper->getLiveRangesForVarForProlog(varNum);
                }
                else
                {
                    liveRanges = varLiveKeeper->getLiveRangesForVarForBody(varNum);
                }
                for (VariableLiveKeeper::VariableLiveRange& liveRange : *liveRanges)
                {
                    UNATIVE_OFFSET startOffs = liveRange.m_StartEmitLocation.CodeOffset(GetEmitter());
                    UNATIVE_OFFSET endOffs   = liveRange.m_EndEmitLocation.CodeOffset(GetEmitter());

                    if (varDsc->lvIsParam && (startOffs == endOffs))
                    {
                        // If the length is zero, it means that the prolog is empty. In that case,
                        // CodeGen::genSetScopeInfo will report the liveness of all arguments
                        // as spanning the first instruction in the method, so that they can
                        // at least be inspected on entry to the method.
                        endOffs++;
                    }

                    genSetScopeInfo(liveRangeIndex, startOffs, endOffs - startOffs, varNum,
                                    varNum /* I dont know what is the which in eeGetLvInfo */, true,
                                    &liveRange.m_VarLocation);
                    liveRangeIndex++;
                }
            }
        }
    }
}
#endif // USING_VARIABLE_LIVE_RANGE

//------------------------------------------------------------------------
// genSetScopeInfo: Record scope information for debug info
//
// Arguments:
//    which
//    startOffs - the starting offset for this scope
//    length    - the length of this scope
//    varNum    - the lclVar for this scope info
//    LVnum
//    avail     - a bool indicating if it has a home
//    varLoc    - the position (reg or stack) of the variable
//
// Notes:
//    Called for every scope info piece to record by the main genSetScopeInfo()

void CodeGen::genSetScopeInfo(unsigned       which,
                              UNATIVE_OFFSET startOffs,
                              UNATIVE_OFFSET length,
                              unsigned       varNum,
                              unsigned       LVnum,
                              bool           avail,
                              siVarLoc*      varLoc)
{
    // We need to do some mapping while reporting back these variables.

    unsigned ilVarNum = compiler->compMap2ILvarNum(varNum);
    noway_assert((int)ilVarNum != ICorDebugInfo::UNKNOWN_ILNUM);

#ifdef TARGET_X86
    // Non-x86 platforms are allowed to access all arguments directly
    // so we don't need this code.

    // Is this a varargs function?
    if (compiler->info.compIsVarArgs && varNum != compiler->lvaVarargsHandleArg &&
        varNum < compiler->info.compArgsCount && !compiler->lvaTable[varNum].lvIsRegArg)
    {
        noway_assert(varLoc->vlType == VLT_STK || varLoc->vlType == VLT_STK2);

        // All stack arguments (except the varargs handle) have to be
        // accessed via the varargs cookie. Discard generated info,
        // and just find its position relative to the varargs handle

        PREFIX_ASSUME(compiler->lvaVarargsHandleArg < compiler->info.compArgsCount);
        if (!compiler->lvaTable[compiler->lvaVarargsHandleArg].lvOnFrame)
        {
            noway_assert(!compiler->opts.compDbgCode);
            return;
        }

        // Can't check compiler->lvaTable[varNum].lvOnFrame as we don't set it for
        // arguments of vararg functions to avoid reporting them to GC.
        noway_assert(!compiler->lvaTable[varNum].lvRegister);
        unsigned cookieOffset = compiler->lvaTable[compiler->lvaVarargsHandleArg].GetStackOffset();
        unsigned varOffset    = compiler->lvaTable[varNum].GetStackOffset();

        noway_assert(cookieOffset < varOffset);
        unsigned offset     = varOffset - cookieOffset;
        unsigned stkArgSize = compiler->compArgSize - intRegState.rsCalleeRegArgCount * REGSIZE_BYTES;
        noway_assert(offset < stkArgSize);
        offset = stkArgSize - offset;

        varLoc->vlType                   = VLT_FIXED_VA;
        varLoc->vlFixedVarArg.vlfvOffset = offset;
    }

#endif // TARGET_X86

    VarName name = nullptr;

#ifdef DEBUG

    for (unsigned scopeNum = 0; scopeNum < compiler->info.compVarScopesCount; scopeNum++)
    {
        if (LVnum == compiler->info.compVarScopes[scopeNum].vsdLVnum)
        {
            name = compiler->info.compVarScopes[scopeNum].vsdName;
        }
    }

    // Hang on to this compiler->info.

    TrnslLocalVarInfo& tlvi = genTrnslLocalVarInfo[which];

    tlvi.tlviVarNum    = ilVarNum;
    tlvi.tlviLVnum     = LVnum;
    tlvi.tlviName      = name;
    tlvi.tlviStartPC   = startOffs;
    tlvi.tlviLength    = length;
    tlvi.tlviAvailable = avail;
    tlvi.tlviVarLoc    = *varLoc;

#endif // DEBUG

    compiler->eeSetLVinfo(which, startOffs, length, ilVarNum, *varLoc);
}

/*****************************************************************************/
#ifdef LATE_DISASM
#if defined(DEBUG)
/*****************************************************************************
 *                          CompilerRegName
 *
 * Can be called only after lviSetLocalVarInfo() has been called
 */

/* virtual */
const char* CodeGen::siRegVarName(size_t offs, size_t size, unsigned reg)
{
    if (!compiler->opts.compScopeInfo)
        return nullptr;

    if (compiler->info.compVarScopesCount == 0)
        return nullptr;

    noway_assert(genTrnslLocalVarCount == 0 || genTrnslLocalVarInfo);

    for (unsigned i = 0; i < genTrnslLocalVarCount; i++)
    {
        if ((genTrnslLocalVarInfo[i].tlviVarLoc.vlIsInReg((regNumber)reg)) &&
            (genTrnslLocalVarInfo[i].tlviAvailable == true) && (genTrnslLocalVarInfo[i].tlviStartPC <= offs + size) &&
            (genTrnslLocalVarInfo[i].tlviStartPC + genTrnslLocalVarInfo[i].tlviLength > offs))
        {
            return genTrnslLocalVarInfo[i].tlviName ? compiler->VarNameToStr(genTrnslLocalVarInfo[i].tlviName) : NULL;
        }
    }

    return NULL;
}

/*****************************************************************************
 *                          CompilerStkName
 *
 * Can be called only after lviSetLocalVarInfo() has been called
 */

/* virtual */
const char* CodeGen::siStackVarName(size_t offs, size_t size, unsigned reg, unsigned stkOffs)
{
    if (!compiler->opts.compScopeInfo)
        return nullptr;

    if (compiler->info.compVarScopesCount == 0)
        return nullptr;

    noway_assert(genTrnslLocalVarCount == 0 || genTrnslLocalVarInfo);

    for (unsigned i = 0; i < genTrnslLocalVarCount; i++)
    {
        if ((genTrnslLocalVarInfo[i].tlviVarLoc.vlIsOnStack((regNumber)reg, stkOffs)) &&
            (genTrnslLocalVarInfo[i].tlviAvailable == true) && (genTrnslLocalVarInfo[i].tlviStartPC <= offs + size) &&
            (genTrnslLocalVarInfo[i].tlviStartPC + genTrnslLocalVarInfo[i].tlviLength > offs))
        {
            return genTrnslLocalVarInfo[i].tlviName ? compiler->VarNameToStr(genTrnslLocalVarInfo[i].tlviName) : NULL;
        }
    }

    return NULL;
}

/*****************************************************************************/
#endif // defined(DEBUG)
#endif // LATE_DISASM

#ifdef DEBUG

/*****************************************************************************
 *  Display a IPmappingDsc. Pass -1 as mappingNum to not display a mapping number.
 */

void CodeGen::genIPmappingDisp(unsigned mappingNum, Compiler::IPmappingDsc* ipMapping)
{
    if (mappingNum != unsigned(-1))
    {
        printf("%d: ", mappingNum);
    }

    IL_OFFSETX offsx = ipMapping->ipmdILoffsx;

    if (offsx == BAD_IL_OFFSET)
    {
        printf("???");
    }
    else
    {
        Compiler::eeDispILOffs(jitGetILoffsAny(offsx));

        if (jitIsStackEmpty(offsx))
        {
            printf(" STACK_EMPTY");
        }

        if (jitIsCallInstruction(offsx))
        {
            printf(" CALL_INSTRUCTION");
        }
    }

    printf(" ");
    ipMapping->ipmdNativeLoc.Print(compiler->compMethodID);
    // We can only call this after code generation. Is there any way to tell when it's legal to call?
    // printf(" [%x]", ipMapping->ipmdNativeLoc.CodeOffset(GetEmitter()));

    if (ipMapping->ipmdIsLabel)
    {
        printf(" label");
    }

    printf("\n");
}

void CodeGen::genIPmappingListDisp()
{
    unsigned                mappingNum = 0;
    Compiler::IPmappingDsc* ipMapping;

    for (ipMapping = compiler->genIPmappingList; ipMapping != nullptr; ipMapping = ipMapping->ipmdNext)
    {
        genIPmappingDisp(mappingNum, ipMapping);
        ++mappingNum;
    }
}

#endif // DEBUG

/*****************************************************************************
 *
 *  Append an IPmappingDsc struct to the list that we're maintaining
 *  for the debugger.
 *  Record the instr offset as being at the current code gen position.
 */

void CodeGen::genIPmappingAdd(IL_OFFSETX offsx, bool isLabel)
{
    if (!compiler->opts.compDbgInfo)
    {
        return;
    }

    assert(offsx != BAD_IL_OFFSET);

    switch ((int)offsx) // Need the cast since offs is unsigned and the case statements are comparing to signed.
    {
        case ICorDebugInfo::PROLOG:
        case ICorDebugInfo::EPILOG:
            break;

        default:

            if (offsx != (IL_OFFSETX)ICorDebugInfo::NO_MAPPING)
            {
                noway_assert(jitGetILoffs(offsx) <= compiler->info.compILCodeSize);
            }

            // Ignore this one if it's the same IL offset as the last one we saw.
            // Note that we'll let through two identical IL offsets if the flag bits
            // differ, or two identical "special" mappings (e.g., PROLOG).
            if ((compiler->genIPmappingLast != nullptr) && (offsx == compiler->genIPmappingLast->ipmdILoffsx))
            {
                JITDUMP("genIPmappingAdd: ignoring duplicate IL offset 0x%x\n", offsx);
                return;
            }
            break;
    }

    /* Create a mapping entry and append it to the list */

    Compiler::IPmappingDsc* addMapping = compiler->getAllocator(CMK_DebugInfo).allocate<Compiler::IPmappingDsc>(1);
    addMapping->ipmdNativeLoc.CaptureLocation(GetEmitter());
    addMapping->ipmdILoffsx = offsx;
    addMapping->ipmdIsLabel = isLabel;
    addMapping->ipmdNext    = nullptr;

    if (compiler->genIPmappingList != nullptr)
    {
        assert(compiler->genIPmappingLast != nullptr);
        assert(compiler->genIPmappingLast->ipmdNext == nullptr);
        compiler->genIPmappingLast->ipmdNext = addMapping;
    }
    else
    {
        assert(compiler->genIPmappingLast == nullptr);
        compiler->genIPmappingList = addMapping;
    }

    compiler->genIPmappingLast = addMapping;

#ifdef DEBUG
    if (verbose)
    {
        printf("Added IP mapping: ");
        genIPmappingDisp(unsigned(-1), addMapping);
    }
#endif // DEBUG
}

/*****************************************************************************
 *
 *  Prepend an IPmappingDsc struct to the list that we're maintaining
 *  for the debugger.
 *  Record the instr offset as being at the current code gen position.
 */
void CodeGen::genIPmappingAddToFront(IL_OFFSETX offsx)
{
    if (!compiler->opts.compDbgInfo)
    {
        return;
    }

    assert(offsx != BAD_IL_OFFSET);
    assert(compiler->compGeneratingProlog); // We only ever do this during prolog generation.

    switch ((int)offsx) // Need the cast since offs is unsigned and the case statements are comparing to signed.
    {
        case ICorDebugInfo::NO_MAPPING:
        case ICorDebugInfo::PROLOG:
        case ICorDebugInfo::EPILOG:
            break;

        default:
            noway_assert(jitGetILoffs(offsx) <= compiler->info.compILCodeSize);
            break;
    }

    /* Create a mapping entry and prepend it to the list */

    Compiler::IPmappingDsc* addMapping = compiler->getAllocator(CMK_DebugInfo).allocate<Compiler::IPmappingDsc>(1);
    addMapping->ipmdNativeLoc.CaptureLocation(GetEmitter());
    addMapping->ipmdILoffsx = offsx;
    addMapping->ipmdIsLabel = true;
    addMapping->ipmdNext    = nullptr;

    addMapping->ipmdNext       = compiler->genIPmappingList;
    compiler->genIPmappingList = addMapping;

    if (compiler->genIPmappingLast == nullptr)
    {
        compiler->genIPmappingLast = addMapping;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("Added IP mapping to front: ");
        genIPmappingDisp(unsigned(-1), addMapping);
    }
#endif // DEBUG
}

/*****************************************************************************/

C_ASSERT(IL_OFFSETX(ICorDebugInfo::NO_MAPPING) != IL_OFFSETX(BAD_IL_OFFSET));
C_ASSERT(IL_OFFSETX(ICorDebugInfo::PROLOG) != IL_OFFSETX(BAD_IL_OFFSET));
C_ASSERT(IL_OFFSETX(ICorDebugInfo::EPILOG) != IL_OFFSETX(BAD_IL_OFFSET));

C_ASSERT(IL_OFFSETX(BAD_IL_OFFSET) > MAX_IL_OFFSET);
C_ASSERT(IL_OFFSETX(ICorDebugInfo::NO_MAPPING) > MAX_IL_OFFSET);
C_ASSERT(IL_OFFSETX(ICorDebugInfo::PROLOG) > MAX_IL_OFFSET);
C_ASSERT(IL_OFFSETX(ICorDebugInfo::EPILOG) > MAX_IL_OFFSET);

//------------------------------------------------------------------------
// jitGetILoffs: Returns the IL offset portion of the IL_OFFSETX type.
//      Asserts if any ICorDebugInfo distinguished value (like ICorDebugInfo::NO_MAPPING)
//      is seen; these are unexpected here. Also asserts if passed BAD_IL_OFFSET.
//
// Arguments:
//    offsx - the IL_OFFSETX value with the IL offset to extract.
//
// Return Value:
//    The IL offset.

IL_OFFSET jitGetILoffs(IL_OFFSETX offsx)
{
    assert(offsx != BAD_IL_OFFSET);

    switch ((int)offsx) // Need the cast since offs is unsigned and the case statements are comparing to signed.
    {
        case ICorDebugInfo::NO_MAPPING:
        case ICorDebugInfo::PROLOG:
        case ICorDebugInfo::EPILOG:
            unreached();

        default:
            return IL_OFFSET(offsx & ~IL_OFFSETX_BITS);
    }
}

//------------------------------------------------------------------------
// jitGetILoffsAny: Similar to jitGetILoffs(), but passes through ICorDebugInfo
//      distinguished values. Asserts if passed BAD_IL_OFFSET.
//
// Arguments:
//    offsx - the IL_OFFSETX value with the IL offset to extract.
//
// Return Value:
//    The IL offset.

IL_OFFSET jitGetILoffsAny(IL_OFFSETX offsx)
{
    assert(offsx != BAD_IL_OFFSET);

    switch ((int)offsx) // Need the cast since offs is unsigned and the case statements are comparing to signed.
    {
        case ICorDebugInfo::NO_MAPPING:
        case ICorDebugInfo::PROLOG:
        case ICorDebugInfo::EPILOG:
            return IL_OFFSET(offsx);

        default:
            return IL_OFFSET(offsx & ~IL_OFFSETX_BITS);
    }
}

//------------------------------------------------------------------------
// jitIsStackEmpty: Does the IL offset have the stack empty bit set?
//      Asserts if passed BAD_IL_OFFSET.
//
// Arguments:
//    offsx - the IL_OFFSETX value to check
//
// Return Value:
//    'true' if the stack empty bit is set; 'false' otherwise.

bool jitIsStackEmpty(IL_OFFSETX offsx)
{
    assert(offsx != BAD_IL_OFFSET);

    switch ((int)offsx) // Need the cast since offs is unsigned and the case statements are comparing to signed.
    {
        case ICorDebugInfo::NO_MAPPING:
        case ICorDebugInfo::PROLOG:
        case ICorDebugInfo::EPILOG:
            return true;

        default:
            return (offsx & IL_OFFSETX_STKBIT) == 0;
    }
}

//------------------------------------------------------------------------
// jitIsCallInstruction: Does the IL offset have the call instruction bit set?
//      Asserts if passed BAD_IL_OFFSET.
//
// Arguments:
//    offsx - the IL_OFFSETX value to check
//
// Return Value:
//    'true' if the call instruction bit is set; 'false' otherwise.

bool jitIsCallInstruction(IL_OFFSETX offsx)
{
    assert(offsx != BAD_IL_OFFSET);

    switch ((int)offsx) // Need the cast since offs is unsigned and the case statements are comparing to signed.
    {
        case ICorDebugInfo::NO_MAPPING:
        case ICorDebugInfo::PROLOG:
        case ICorDebugInfo::EPILOG:
            return false;

        default:
            return (offsx & IL_OFFSETX_CALLINSTRUCTIONBIT) != 0;
    }
}

/*****************************************************************************/

void CodeGen::genEnsureCodeEmitted(IL_OFFSETX offsx)
{
    if (!compiler->opts.compDbgCode)
    {
        return;
    }

    if (offsx == BAD_IL_OFFSET)
    {
        return;
    }

    /* If other IL were offsets reported, skip */

    if (compiler->genIPmappingLast == nullptr)
    {
        return;
    }

    if (compiler->genIPmappingLast->ipmdILoffsx != offsx)
    {
        return;
    }

    /* offsx was the last reported offset. Make sure that we generated native code */

    if (compiler->genIPmappingLast->ipmdNativeLoc.IsCurrentLocation(GetEmitter()))
    {
        instGen(INS_nop);
    }
}

/*****************************************************************************
 *
 *  Shut down the IP-mapping logic, report the info to the EE.
 */

void CodeGen::genIPmappingGen()
{
    if (!compiler->opts.compDbgInfo)
    {
        return;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genIPmappingGen()\n");
    }
#endif

    if (compiler->genIPmappingList == nullptr)
    {
        compiler->eeSetLIcount(0);
        compiler->eeSetLIdone();
        return;
    }

    Compiler::IPmappingDsc* tmpMapping;
    Compiler::IPmappingDsc* prevMapping;
    unsigned                mappingCnt;
    UNATIVE_OFFSET          lastNativeOfs;

    /* First count the number of distinct mapping records */

    mappingCnt    = 0;
    lastNativeOfs = UNATIVE_OFFSET(~0);

    for (prevMapping = nullptr, tmpMapping = compiler->genIPmappingList; tmpMapping != nullptr;
         tmpMapping = tmpMapping->ipmdNext)
    {
        IL_OFFSETX srcIP = tmpMapping->ipmdILoffsx;

        // Managed RetVal - since new sequence points are emitted to identify IL calls,
        // make sure that those are not filtered and do not interfere with filtering of
        // other sequence points.
        if (jitIsCallInstruction(srcIP))
        {
            mappingCnt++;
            continue;
        }

        UNATIVE_OFFSET nextNativeOfs = tmpMapping->ipmdNativeLoc.CodeOffset(GetEmitter());

        if (nextNativeOfs != lastNativeOfs)
        {
            mappingCnt++;
            lastNativeOfs = nextNativeOfs;
            prevMapping   = tmpMapping;
            continue;
        }

        /* If there are mappings with the same native offset, then:
           o If one of them is NO_MAPPING, ignore it
           o If one of them is a label, report that and ignore the other one
           o Else report the higher IL offset
         */

        PREFIX_ASSUME(prevMapping != nullptr); // We would exit before if this was true
        if (prevMapping->ipmdILoffsx == (IL_OFFSETX)ICorDebugInfo::NO_MAPPING)
        {
            // If the previous entry was NO_MAPPING, ignore it
            prevMapping->ipmdNativeLoc.Init();
            prevMapping = tmpMapping;
        }
        else if (srcIP == (IL_OFFSETX)ICorDebugInfo::NO_MAPPING)
        {
            // If the current entry is NO_MAPPING, ignore it
            // Leave prevMapping unchanged as tmpMapping is no longer valid
            tmpMapping->ipmdNativeLoc.Init();
        }
        else if (srcIP == (IL_OFFSETX)ICorDebugInfo::EPILOG || srcIP == 0)
        {
            // counting for special cases: see below
            mappingCnt++;
            prevMapping = tmpMapping;
        }
        else
        {
            noway_assert(prevMapping != nullptr);
            noway_assert(!prevMapping->ipmdNativeLoc.Valid() ||
                         lastNativeOfs == prevMapping->ipmdNativeLoc.CodeOffset(GetEmitter()));

            /* The previous block had the same native offset. We have to
               discard one of the mappings. Simply reinitialize ipmdNativeLoc
               and prevMapping will be ignored later. */

            if (prevMapping->ipmdIsLabel)
            {
                // Leave prevMapping unchanged as tmpMapping is no longer valid
                tmpMapping->ipmdNativeLoc.Init();
            }
            else
            {
                prevMapping->ipmdNativeLoc.Init();
                prevMapping = tmpMapping;
            }
        }
    }

    /* Tell them how many mapping records we've got */

    compiler->eeSetLIcount(mappingCnt);

    /* Now tell them about the mappings */

    mappingCnt    = 0;
    lastNativeOfs = UNATIVE_OFFSET(~0);

    for (tmpMapping = compiler->genIPmappingList; tmpMapping != nullptr; tmpMapping = tmpMapping->ipmdNext)
    {
        // Do we have to skip this record ?
        if (!tmpMapping->ipmdNativeLoc.Valid())
        {
            continue;
        }

        UNATIVE_OFFSET nextNativeOfs = tmpMapping->ipmdNativeLoc.CodeOffset(GetEmitter());
        IL_OFFSETX     srcIP         = tmpMapping->ipmdILoffsx;

        if (jitIsCallInstruction(srcIP))
        {
            compiler->eeSetLIinfo(mappingCnt++, nextNativeOfs, jitGetILoffs(srcIP), jitIsStackEmpty(srcIP), true);
        }
        else if (nextNativeOfs != lastNativeOfs)
        {
            compiler->eeSetLIinfo(mappingCnt++, nextNativeOfs, jitGetILoffsAny(srcIP), jitIsStackEmpty(srcIP), false);
            lastNativeOfs = nextNativeOfs;
        }
        else if (srcIP == (IL_OFFSETX)ICorDebugInfo::EPILOG || srcIP == 0)
        {
            // For the special case of an IL instruction with no body
            // followed by the epilog (say ret void immediately preceding
            // the method end), we put two entries in, so that we'll stop
            // at the (empty) ret statement if the user tries to put a
            // breakpoint there, and then have the option of seeing the
            // epilog or not based on SetUnmappedStopMask for the stepper.
            compiler->eeSetLIinfo(mappingCnt++, nextNativeOfs, jitGetILoffsAny(srcIP), jitIsStackEmpty(srcIP), false);
        }
    }

#if 0
    // TODO-Review:
    //This check is disabled.  It is always true that any time this check asserts, the debugger would have a
    //problem with IL source level debugging.  However, for a C# file, it only matters if things are on
    //different source lines.  As a result, we have all sorts of latent problems with how we emit debug
    //info, but very few actual ones.  Whenever someone wants to tackle that problem in general, turn this
    //assert back on.
    if (compiler->opts.compDbgCode)
    {
        //Assert that the first instruction of every basic block with more than one incoming edge has a
        //different sequence point from each incoming block.
        //
        //It turns out that the only thing we really have to assert is that the first statement in each basic
        //block has an IL offset and appears in eeBoundaries.
        for (BasicBlock * block = compiler->fgFirstBB; block != nullptr; block = block->bbNext)
        {
            Statement* stmt = block->firstStmt();
            if ((block->bbRefs > 1) && (stmt != nullptr))
            {
                bool found = false;
                if (stmt->GetILOffsetX() != BAD_IL_OFFSET)
                {
                    IL_OFFSET ilOffs = jitGetILoffs(stmt->GetILOffsetX());
                    for (unsigned i = 0; i < eeBoundariesCount; ++i)
                    {
                        if (eeBoundaries[i].ilOffset == ilOffs)
                        {
                            found = true;
                            break;
                        }
                    }
                }
                noway_assert(found && "A basic block that is a jump target did not start a new sequence point.");
            }
        }
    }
#endif // 0

    compiler->eeSetLIdone();
}

/*============================================================================
 *
 *   These are empty stubs to help the late dis-assembler to compile
 *   if the late disassembler is being built into a non-DEBUG build.
 *
 *============================================================================
 */

#if defined(LATE_DISASM)
#if !defined(DEBUG)

/* virtual */
const char* CodeGen::siRegVarName(size_t offs, size_t size, unsigned reg)
{
    return NULL;
}

/* virtual */
const char* CodeGen::siStackVarName(size_t offs, size_t size, unsigned reg, unsigned stkOffs)
{
    return NULL;
}

/*****************************************************************************/
#endif // !defined(DEBUG)
#endif // defined(LATE_DISASM)
/*****************************************************************************/

//------------------------------------------------------------------------
// indirForm: Make a temporary indir we can feed to pattern matching routines
//    in cases where we don't want to instantiate all the indirs that happen.
//
GenTreeIndir CodeGen::indirForm(var_types type, GenTree* base)
{
    GenTreeIndir i(GT_IND, type, base, nullptr);
    i.SetRegNum(REG_NA);
    i.SetContained();
    return i;
}

//------------------------------------------------------------------------
// indirForm: Make a temporary indir we can feed to pattern matching routines
//    in cases where we don't want to instantiate all the indirs that happen.
//
GenTreeStoreInd CodeGen::storeIndirForm(var_types type, GenTree* base, GenTree* data)
{
    GenTreeStoreInd i(type, base, data);
    i.SetRegNum(REG_NA);
    return i;
}

//------------------------------------------------------------------------
// intForm: Make a temporary int we can feed to pattern matching routines
//    in cases where we don't want to instantiate.
//
GenTreeIntCon CodeGen::intForm(var_types type, ssize_t value)
{
    GenTreeIntCon i(type, value);
    i.SetRegNum(REG_NA);
    return i;
}

#if defined(TARGET_X86) || defined(TARGET_ARM)
//------------------------------------------------------------------------
// genLongReturn: Generates code for long return statement for x86 and arm.
//
// Note: treeNode's and op1's registers are already consumed.
//
// Arguments:
//    treeNode - The GT_RETURN or GT_RETFILT tree node with LONG return type.
//
// Return Value:
//    None
//
void CodeGen::genLongReturn(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_RETURN || treeNode->OperGet() == GT_RETFILT);
    assert(treeNode->TypeGet() == TYP_LONG);
    GenTree*  op1        = treeNode->gtGetOp1();
    var_types targetType = treeNode->TypeGet();

    assert(op1 != nullptr);
    assert(op1->OperGet() == GT_LONG);
    GenTree* loRetVal = op1->gtGetOp1();
    GenTree* hiRetVal = op1->gtGetOp2();
    assert((loRetVal->GetRegNum() != REG_NA) && (hiRetVal->GetRegNum() != REG_NA));

    genConsumeReg(loRetVal);
    genConsumeReg(hiRetVal);
    if (loRetVal->GetRegNum() != REG_LNGRET_LO)
    {
        inst_RV_RV(ins_Copy(targetType), REG_LNGRET_LO, loRetVal->GetRegNum(), TYP_INT);
    }
    if (hiRetVal->GetRegNum() != REG_LNGRET_HI)
    {
        inst_RV_RV(ins_Copy(targetType), REG_LNGRET_HI, hiRetVal->GetRegNum(), TYP_INT);
    }
}
#endif // TARGET_X86 || TARGET_ARM

//------------------------------------------------------------------------
// genReturn: Generates code for return statement.
//            In case of struct return, delegates to the genStructReturn method.
//
// Arguments:
//    treeNode - The GT_RETURN or GT_RETFILT tree node.
//
// Return Value:
//    None
//
void CodeGen::genReturn(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_RETURN || treeNode->OperGet() == GT_RETFILT);
    GenTree*  op1        = treeNode->gtGetOp1();
    var_types targetType = treeNode->TypeGet();

    // A void GT_RETFILT is the end of a finally. For non-void filter returns we need to load the result in the return
    // register, if it's not already there. The processing is the same as GT_RETURN. For filters, the IL spec says the
    // result is type int32. Further, the only legal values are 0 or 1; the use of other values is "undefined".
    assert(!treeNode->OperIs(GT_RETFILT) || (targetType == TYP_VOID) || (targetType == TYP_INT));

#ifdef DEBUG
    if (targetType == TYP_VOID)
    {
        assert(op1 == nullptr);
    }
#endif // DEBUG

#if defined(TARGET_X86) || defined(TARGET_ARM)
    if (targetType == TYP_LONG)
    {
        genLongReturn(treeNode);
    }
    else
#endif // TARGET_X86 || TARGET_ARM
    {
        if (isStructReturn(treeNode))
        {
            genStructReturn(treeNode);
        }
        else if (targetType != TYP_VOID)
        {
            assert(op1 != nullptr);
            noway_assert(op1->GetRegNum() != REG_NA);

            // !! NOTE !! genConsumeReg will clear op1 as GC ref after it has
            // consumed a reg for the operand. This is because the variable
            // is dead after return. But we are issuing more instructions
            // like "profiler leave callback" after this consumption. So
            // if you are issuing more instructions after this point,
            // remember to keep the variable live up until the new method
            // exit point where it is actually dead.
            genConsumeReg(op1);

#if defined(TARGET_ARM64)
            genSimpleReturn(treeNode);
#else // !TARGET_ARM64
#if defined(TARGET_X86)
            if (varTypeUsesFloatReg(treeNode))
            {
                genFloatReturn(treeNode);
            }
            else
#elif defined(TARGET_ARM)
            if (varTypeUsesFloatReg(treeNode) && (compiler->opts.compUseSoftFP || compiler->info.compIsVarArgs))
            {
                if (targetType == TYP_FLOAT)
                {
                    GetEmitter()->emitIns_R_R(INS_vmov_f2i, EA_4BYTE, REG_INTRET, op1->GetRegNum());
                }
                else
                {
                    assert(targetType == TYP_DOUBLE);
                    GetEmitter()->emitIns_R_R_R(INS_vmov_d2i, EA_8BYTE, REG_INTRET, REG_NEXT(REG_INTRET),
                                                op1->GetRegNum());
                }
            }
            else
#endif // TARGET_ARM
            {
                regNumber retReg = varTypeUsesFloatReg(treeNode) ? REG_FLOATRET : REG_INTRET;
                if (op1->GetRegNum() != retReg)
                {
                    inst_RV_RV(ins_Move_Extend(targetType, true), retReg, op1->GetRegNum(), targetType);
                }
            }
#endif // !TARGET_ARM64
        }
    }

#ifdef PROFILING_SUPPORTED
    // !! Note !!
    // TODO-AMD64-Unix: If the profiler hook is implemented on *nix, make sure for 2 register returned structs
    //                  the RAX and RDX needs to be kept alive. Make the necessary changes in lowerxarch.cpp
    //                  in the handling of the GT_RETURN statement.
    //                  Such structs containing GC pointers need to be handled by calling gcInfo.gcMarkRegSetNpt
    //                  for the return registers containing GC refs.
    //
    // Reason for not materializing Leave callback as a GT_PROF_HOOK node after GT_RETURN:
    // In flowgraph and other places assert that the last node of a block marked as
    // BBJ_RETURN is either a GT_RETURN or GT_JMP or a tail call.  It would be nice to
    // maintain such an invariant irrespective of whether profiler hook needed or not.
    // Also, there is not much to be gained by materializing it as an explicit node.
    //
    // There should be a single return block while generating profiler ELT callbacks,
    // so we just look for that block to trigger insertion of the profile hook.
    if ((compiler->compCurBB == compiler->genReturnBB) && compiler->compIsProfilerHookNeeded())
    {
        // !! NOTE !!
        // Since we are invalidating the assumption that we would slip into the epilog
        // right after the "return", we need to preserve the return reg's GC state
        // across the call until actual method return.
        ReturnTypeDesc retTypeDesc;
        unsigned       regCount = 0;
        if (compiler->compMethodReturnsMultiRegRetType())
        {
            if (varTypeIsLong(compiler->info.compRetNativeType))
            {
                retTypeDesc.InitializeLongReturnType();
            }
            else // we must have a struct return type
            {
                CorInfoCallConvExtension callConv = compiler->info.compCallConv;

                retTypeDesc.InitializeStructReturnType(compiler, compiler->info.compMethodInfo->args.retTypeClass,
                                                       callConv);
            }
            regCount = retTypeDesc.GetReturnRegCount();
        }

        if (varTypeIsGC(compiler->info.compRetNativeType))
        {
            gcInfo.gcMarkRegPtrVal(REG_INTRET, compiler->info.compRetNativeType);
        }
        else if (compiler->compMethodReturnsMultiRegRetType())
        {
            for (unsigned i = 0; i < regCount; ++i)
            {
                if (varTypeIsGC(retTypeDesc.GetReturnRegType(i)))
                {
                    gcInfo.gcMarkRegPtrVal(retTypeDesc.GetABIReturnReg(i), retTypeDesc.GetReturnRegType(i));
                }
            }
        }
        else if (compiler->compMethodReturnsRetBufAddr())
        {
            gcInfo.gcMarkRegPtrVal(REG_INTRET, TYP_BYREF);
        }

        genProfilingLeaveCallback(CORINFO_HELP_PROF_FCN_LEAVE);

        if (varTypeIsGC(compiler->info.compRetNativeType))
        {
            gcInfo.gcMarkRegSetNpt(genRegMask(REG_INTRET));
        }
        else if (compiler->compMethodReturnsMultiRegRetType())
        {
            for (unsigned i = 0; i < regCount; ++i)
            {
                if (varTypeIsGC(retTypeDesc.GetReturnRegType(i)))
                {
                    gcInfo.gcMarkRegSetNpt(genRegMask(retTypeDesc.GetABIReturnReg(i)));
                }
            }
        }
        else if (compiler->compMethodReturnsRetBufAddr())
        {
            gcInfo.gcMarkRegSetNpt(genRegMask(REG_INTRET));
        }
    }
#endif // PROFILING_SUPPORTED

#if defined(DEBUG) && defined(TARGET_XARCH)
    bool doStackPointerCheck = compiler->opts.compStackCheckOnRet;

#if defined(FEATURE_EH_FUNCLETS)
    // Don't do stack pointer check at the return from a funclet; only for the main function.
    if (compiler->funCurrentFunc()->funKind != FUNC_ROOT)
    {
        doStackPointerCheck = false;
    }
#else  // !FEATURE_EH_FUNCLETS
    // Don't generate stack checks for x86 finally/filter EH returns: these are not invoked
    // with the same SP as the main function. See also CodeGen::genEHFinallyOrFilterRet().
    if ((compiler->compCurBB->bbJumpKind == BBJ_EHFINALLYRET) || (compiler->compCurBB->bbJumpKind == BBJ_EHFILTERRET))
    {
        doStackPointerCheck = false;
    }
#endif // !FEATURE_EH_FUNCLETS

    genStackPointerCheck(doStackPointerCheck, compiler->lvaReturnSpCheck);
#endif // defined(DEBUG) && defined(TARGET_XARCH)
}

//------------------------------------------------------------------------
// isStructReturn: Returns whether the 'treeNode' is returning a struct.
//
// Arguments:
//    treeNode - The tree node to evaluate whether is a struct return.
//
// Return Value:
//    Returns true if the 'treeNode" is a GT_RETURN node of type struct.
//    Otherwise returns false.
//
bool CodeGen::isStructReturn(GenTree* treeNode)
{
    // This method could be called for 'treeNode' of GT_RET_FILT or GT_RETURN.
    // For the GT_RET_FILT, the return is always a bool or a void, for the end of a finally block.
    noway_assert(treeNode->OperGet() == GT_RETURN || treeNode->OperGet() == GT_RETFILT);
    if (treeNode->OperGet() != GT_RETURN)
    {
        return false;
    }

#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)
    assert(!varTypeIsStruct(treeNode));
    return false;
#else
    return varTypeIsStruct(treeNode) && (compiler->info.compRetNativeType == TYP_STRUCT);
#endif
}

//------------------------------------------------------------------------
// genStructReturn: Generates code for returning a struct.
//
// Arguments:
//    treeNode - The GT_RETURN tree node.
//
// Return Value:
//    None
//
// Assumption:
//    op1 of GT_RETURN node is either GT_LCL_VAR or multi-reg GT_CALL
//
void CodeGen::genStructReturn(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_RETURN);
    GenTree* op1 = treeNode->gtGetOp1();
    genConsumeRegs(op1);
    GenTree* actualOp1 = op1;
    if (op1->IsCopyOrReload())
    {
        actualOp1 = op1->gtGetOp1();
    }

    ReturnTypeDesc retTypeDesc;
    LclVarDsc*     varDsc = nullptr;
    if (actualOp1->OperIs(GT_LCL_VAR))
    {
        varDsc = compiler->lvaGetDesc(actualOp1->AsLclVar()->GetLclNum());
        retTypeDesc.InitializeStructReturnType(compiler, varDsc->GetStructHnd(), compiler->info.compCallConv);
        assert(varDsc->lvIsMultiRegRet);
    }
    else
    {
        assert(actualOp1->OperIs(GT_CALL));
        retTypeDesc = *(actualOp1->AsCall()->GetReturnTypeDesc());
    }
    unsigned regCount = retTypeDesc.GetReturnRegCount();
    assert(regCount <= MAX_RET_REG_COUNT);

#if FEATURE_MULTIREG_RET
    if (genIsRegCandidateLocal(actualOp1))
    {
        // Right now the only enregisterable structs supported are SIMD vector types.
        assert(varTypeIsSIMD(op1));
        assert(!actualOp1->AsLclVar()->IsMultiReg());
#ifdef FEATURE_SIMD
        genSIMDSplitReturn(op1, &retTypeDesc);
#endif // FEATURE_SIMD
    }
    else if (actualOp1->OperIs(GT_LCL_VAR) && !actualOp1->AsLclVar()->IsMultiReg())
    {
        GenTreeLclVar* lclNode = actualOp1->AsLclVar();
        LclVarDsc*     varDsc  = compiler->lvaGetDesc(lclNode->GetLclNum());
        assert(varDsc->lvIsMultiRegRet);
        int offset = 0;
        for (unsigned i = 0; i < regCount; ++i)
        {
            var_types type  = retTypeDesc.GetReturnRegType(i);
            regNumber toReg = retTypeDesc.GetABIReturnReg(i);
            GetEmitter()->emitIns_R_S(ins_Load(type), emitTypeSize(type), toReg, lclNode->GetLclNum(), offset);
            offset += genTypeSize(type);
        }
    }
    else
    {
        for (unsigned i = 0; i < regCount; ++i)
        {
            var_types type    = retTypeDesc.GetReturnRegType(i);
            regNumber toReg   = retTypeDesc.GetABIReturnReg(i);
            regNumber fromReg = op1->GetRegByIndex(i);
            if ((fromReg == REG_NA) && op1->OperIs(GT_COPY))
            {
                // A copy that doesn't copy this field will have REG_NA.
                // TODO-Cleanup: It would probably be better to always have a valid reg
                // on a GT_COPY, unless the operand is actually spilled. Then we wouldn't have
                // to check for this case (though we'd have to check in the genRegCopy that the
                // reg is valid).
                fromReg = actualOp1->GetRegByIndex(i);
            }
            if (fromReg == REG_NA)
            {
                // This is a spilled field of a multi-reg lclVar.
                // We currently only mark a lclVar operand as RegOptional, since we don't have a way
                // to mark a multi-reg tree node as used from spill (GTF_NOREG_AT_USE) on a per-reg basis.
                assert(varDsc != nullptr);
                assert(varDsc->lvPromoted);
                unsigned fieldVarNum = varDsc->lvFieldLclStart + i;
                assert(compiler->lvaGetDesc(fieldVarNum)->lvOnFrame);
                GetEmitter()->emitIns_R_S(ins_Load(type), emitTypeSize(type), toReg, fieldVarNum, 0);
            }
            else if (fromReg != toReg)
            {
                // Note that ins_Copy(fromReg, type) will return the appropriate register to copy
                // between register files if needed.
                inst_RV_RV(ins_Copy(fromReg, type), toReg, fromReg, type);
            }
        }
    }
#else // !FEATURE_MULTIREG_RET
    unreached();
#endif
}

//----------------------------------------------------------------------------------
// genMultiRegStoreToLocal: store multi-reg value to a local
//
// Arguments:
//    lclNode  -  Gentree of GT_STORE_LCL_VAR
//
// Return Value:
//    None
//
// Assumption:
//    The child of store is a multi-reg node.
//
void CodeGen::genMultiRegStoreToLocal(GenTreeLclVar* lclNode)
{
    assert(lclNode->OperIs(GT_STORE_LCL_VAR));
    assert(varTypeIsStruct(lclNode) || varTypeIsMultiReg(lclNode));
    GenTree* op1       = lclNode->gtGetOp1();
    GenTree* actualOp1 = op1->gtSkipReloadOrCopy();
    assert(op1->IsMultiRegNode());
    unsigned regCount =
        actualOp1->IsMultiRegLclVar() ? actualOp1->AsLclVar()->GetFieldCount(compiler) : actualOp1->GetMultiRegCount();

    // Assumption: current implementation requires that a multi-reg
    // var in 'var = call' is flagged as lvIsMultiRegRet to prevent it from
    // being promoted, unless compiler->lvaEnregMultiRegVars is true.

    unsigned   lclNum = lclNode->AsLclVarCommon()->GetLclNum();
    LclVarDsc* varDsc = compiler->lvaGetDesc(lclNum);
    if (op1->OperIs(GT_CALL))
    {
        assert(regCount <= MAX_RET_REG_COUNT);
        noway_assert(varDsc->lvIsMultiRegRet);
    }

#ifdef FEATURE_SIMD
    // Check for the case of an enregistered SIMD type that's returned in multiple registers.
    if (varDsc->lvIsRegCandidate() && lclNode->GetRegNum() != REG_NA)
    {
        assert(varTypeIsSIMD(lclNode));
        genMultiRegStoreToSIMDLocal(lclNode);
        return;
    }
#endif // FEATURE_SIMD

    // We have either a multi-reg local or a local with multiple fields in memory.
    //
    // The liveness model is as follows:
    //    use reg #0 from src, including any reload or copy
    //    define reg #0
    //    use reg #1 from src, including any reload or copy
    //    define reg #1
    //    etc.
    // Imagine the following scenario:
    //    There are 3 registers used. Prior to this node, they occupy registers r3, r2 and r1.
    //    There are 3 registers defined by this node. They need to be placed in r1, r2 and r3,
    //    in that order.
    //
    // If we defined the as using all the source registers at once, we'd have to adopt one
    // of the following models:
    //  - All (or all but one) of the incoming sources are marked "delayFree" so that they won't
    //    get the same register as any of the registers being defined. This would result in copies for
    //    the common case where the source and destination registers are the same (e.g. when a CALL
    //    result is assigned to a lclVar, which is then returned).
    //    - For our example (and for many/most cases) we would have to copy or spill all sources.
    //  - We allow circular dependencies between source and destination registers. This would require
    //    the code generator to determine the order in which the copies must be generated, and would
    //    require a temp register in case a swap is required. This complexity would have to be handled
    //    in both the normal code generation case, as well as for copies & reloads, as they are currently
    //    modeled by the register allocator to happen just prior to the use.
    //    - For our example, a temp would be required to swap r1 and r3, unless a swap instruction is
    //      available on the target.
    //
    // By having a multi-reg local use and define each field in order, we avoid these issues, and the
    // register allocator will ensure that any conflicts are resolved via spill or inserted COPYs.
    // For our example, the register allocator would simple spill r1 because the first def requires it.
    // The code generator would move r3  to r1, leave r2 alone, and then load the spilled value into r3.

    unsigned offset        = 0;
    bool     isMultiRegVar = lclNode->IsMultiRegLclVar();
    bool     hasRegs       = false;

    if (isMultiRegVar)
    {
        assert(compiler->lvaEnregMultiRegVars);
        assert(regCount == varDsc->lvFieldCnt);
    }
    for (unsigned i = 0; i < regCount; ++i)
    {
        regNumber reg     = genConsumeReg(op1, i);
        var_types srcType = actualOp1->GetRegTypeByIndex(i);
        // genConsumeReg will return the valid register, either from the COPY
        // or from the original source.
        assert(reg != REG_NA);
        if (isMultiRegVar)
        {
            // Each field is passed in its own register, use the field types.
            regNumber  varReg      = lclNode->GetRegByIndex(i);
            unsigned   fieldLclNum = varDsc->lvFieldLclStart + i;
            LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(fieldLclNum);
            var_types  destType    = fieldVarDsc->TypeGet();
            if (varReg != REG_NA)
            {
                hasRegs = true;
                if (varReg != reg)
                {
                    // We may need a cross register-file copy here.
                    inst_RV_RV(ins_Copy(reg, destType), varReg, reg, destType);
                }
                fieldVarDsc->SetRegNum(varReg);
            }
            else
            {
                varReg = REG_STK;
            }
            if ((varReg == REG_STK) || fieldVarDsc->lvLiveInOutOfHndlr)
            {
                if (!lclNode->AsLclVar()->IsLastUse(i))
                {
                    // A byte field passed in a long register should be written on the stack as a byte.
                    instruction storeIns = ins_StoreFromSrc(reg, destType);
                    GetEmitter()->emitIns_S_R(storeIns, emitTypeSize(destType), reg, fieldLclNum, 0);
                }
            }
            fieldVarDsc->SetRegNum(varReg);
        }
        else
        {
            // Several fields could be passed in one register, copy using the register type.
            // It could rewrite memory outside of the fields but local on the stack are rounded to POINTER_SIZE so
            // it is safe to store a long register into a byte field as it is known that we have enough padding after.
            GetEmitter()->emitIns_S_R(ins_Store(srcType), emitTypeSize(srcType), reg, lclNum, offset);
            offset += genTypeSize(srcType);
#ifdef DEBUG
#ifdef TARGET_64BIT
            assert(offset <= varDsc->lvSize());
#else  // !TARGET_64BIT
            if (varTypeIsStruct(varDsc))
            {
                assert(offset <= varDsc->lvSize());
            }
            else
            {
                assert(varDsc->TypeGet() == TYP_LONG);
                assert(offset <= genTypeSize(TYP_LONG));
            }
#endif // !TARGET_64BIT
#endif // DEBUG
        }
    }

    // Update variable liveness.
    if (isMultiRegVar)
    {
        if (hasRegs)
        {
            genProduceReg(lclNode);
        }
        else
        {
            genUpdateLife(lclNode);
        }
    }
    else
    {
        genUpdateLife(lclNode);
        varDsc->SetRegNum(REG_STK);
    }
}

//------------------------------------------------------------------------
// genRegCopy: Produce code for a GT_COPY node.
//
// Arguments:
//    tree - the GT_COPY node
//
// Notes:
//    This will copy the register produced by this node's source, to
//    the register allocated to this GT_COPY node.
//    It has some special handling for these cases:
//    - when the source and target registers are in different register files
//      (note that this is *not* a conversion).
//    - when the source is a lclVar whose home location is being moved to a new
//      register (rather than just being copied for temporary use).
//
void CodeGen::genRegCopy(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_COPY);
    GenTree* op1 = treeNode->AsOp()->gtOp1;

    if (op1->IsMultiRegNode())
    {
        // Register allocation assumes that any reload and copy are done in operand order.
        // That is, we can have:
        //    (reg0, reg1) = COPY(V0,V1) where V0 is in reg1 and V1 is in memory
        // The register allocation model assumes:
        //     First, V0 is moved to reg0 (v1 can't be in reg0 because it is still live, which would be a conflict).
        //     Then, V1 is moved to reg1
        // However, if we call genConsumeRegs on op1, it will do the reload of V1 before we do the copy of V0.
        // So we need to handle that case first.
        //
        // There should never be any circular dependencies, and we will check that here.

        // GenTreeCopyOrReload only reports the highest index that has a valid register.
        // However, we need to ensure that we consume all the registers of the child node,
        // so we use its regCount.
        unsigned regCount =
            op1->IsMultiRegLclVar() ? op1->AsLclVar()->GetFieldCount(compiler) : op1->GetMultiRegCount();
        assert(regCount <= MAX_MULTIREG_COUNT);

        // First set the source registers as busy if they haven't been spilled.
        // (Note that this is just for verification that we don't have circular dependencies.)
        regMaskTP busyRegs = RBM_NONE;
        for (unsigned i = 0; i < regCount; ++i)
        {
            if ((op1->GetRegSpillFlagByIdx(i) & GTF_SPILLED) == 0)
            {
                busyRegs |= genRegMask(op1->GetRegByIndex(i));
            }
        }
        for (unsigned i = 0; i < regCount; ++i)
        {
            regNumber sourceReg = op1->GetRegByIndex(i);
            // genRegCopy will consume the source register, perform any required reloads,
            // and will return either the register copied to, or the original register if there's no copy.
            regNumber targetReg = genRegCopy(treeNode, i);
            if (targetReg != sourceReg)
            {
                regMaskTP targetRegMask = genRegMask(targetReg);
                assert((busyRegs & targetRegMask) == 0);
                // Clear sourceReg from the busyRegs, and add targetReg.
                busyRegs &= ~genRegMask(sourceReg);
            }
            busyRegs |= genRegMask(targetReg);
        }
        return;
    }

    regNumber srcReg     = genConsumeReg(op1);
    var_types targetType = treeNode->TypeGet();
    regNumber targetReg  = treeNode->GetRegNum();
    assert(srcReg != REG_NA);
    assert(targetReg != REG_NA);
    assert(targetType != TYP_STRUCT);

    inst_RV_RV(ins_Copy(srcReg, targetType), targetReg, srcReg, targetType);

    if (op1->IsLocal())
    {
        // The lclVar will never be a def.
        // If it is a last use, the lclVar will be killed by genConsumeReg(), as usual, and genProduceReg will
        // appropriately set the gcInfo for the copied value.
        // If not, there are two cases we need to handle:
        // - If this is a TEMPORARY copy (indicated by the GTF_VAR_DEATH flag) the variable
        //   will remain live in its original register.
        //   genProduceReg() will appropriately set the gcInfo for the copied value,
        //   and genConsumeReg will reset it.
        // - Otherwise, we need to update register info for the lclVar.

        GenTreeLclVarCommon* lcl = op1->AsLclVarCommon();
        assert((lcl->gtFlags & GTF_VAR_DEF) == 0);

        if ((lcl->gtFlags & GTF_VAR_DEATH) == 0 && (treeNode->gtFlags & GTF_VAR_DEATH) == 0)
        {
            LclVarDsc* varDsc = compiler->lvaGetDesc(lcl);

            // If we didn't just spill it (in genConsumeReg, above), then update the register info
            if (varDsc->GetRegNum() != REG_STK)
            {
                // The old location is dying
                genUpdateRegLife(varDsc, /*isBorn*/ false, /*isDying*/ true DEBUGARG(op1));

                gcInfo.gcMarkRegSetNpt(genRegMask(op1->GetRegNum()));

                genUpdateVarReg(varDsc, treeNode);

#ifdef USING_VARIABLE_LIVE_RANGE
                // Report the home change for this variable
                varLiveKeeper->siUpdateVariableLiveRange(varDsc, lcl->GetLclNum());
#endif // USING_VARIABLE_LIVE_RANGE

                // The new location is going live
                genUpdateRegLife(varDsc, /*isBorn*/ true, /*isDying*/ false DEBUGARG(treeNode));
            }
        }
    }

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genRegCopy: Produce code for a single register of a multireg copy node.
//
// Arguments:
//    tree          - The GT_COPY node
//    multiRegIndex - The index of the register to be copied
//
// Notes:
//    This will copy the corresponding register produced by this node's source, to
//    the register allocated to the register specified by this GT_COPY node.
//    A multireg copy doesn't support moving between register files, as the GT_COPY
//    node does not retain separate types for each index.
//    - when the source is a lclVar whose home location is being moved to a new
//      register (rather than just being copied for temporary use).
//
// Return Value:
//    Either the register copied to, or the original register if there's no copy.
//
regNumber CodeGen::genRegCopy(GenTree* treeNode, unsigned multiRegIndex)
{
    assert(treeNode->OperGet() == GT_COPY);
    GenTree* op1 = treeNode->gtGetOp1();
    assert(op1->IsMultiRegNode());

    GenTreeCopyOrReload* copyNode = treeNode->AsCopyOrReload();
    assert(copyNode->GetRegCount() <= MAX_MULTIREG_COUNT);

    // Consume op1's register, which will perform any necessary reloads.
    genConsumeReg(op1, multiRegIndex);

    regNumber sourceReg = op1->GetRegByIndex(multiRegIndex);
    regNumber targetReg = copyNode->GetRegNumByIdx(multiRegIndex);
    // GenTreeCopyOrReload only reports the highest index that has a valid register.
    // However there may be lower indices that have no valid register (i.e. the register
    // on the source is still valid at the consumer).
    if (targetReg != REG_NA)
    {
        // We shouldn't specify a no-op move.
        assert(sourceReg != targetReg);
        var_types type;
        if (op1->IsMultiRegLclVar())
        {
            LclVarDsc* parentVarDsc = compiler->lvaGetDesc(op1->AsLclVar()->GetLclNum());
            unsigned   fieldVarNum  = parentVarDsc->lvFieldLclStart + multiRegIndex;
            LclVarDsc* fieldVarDsc  = compiler->lvaGetDesc(fieldVarNum);
            type                    = fieldVarDsc->TypeGet();
            inst_RV_RV(ins_Copy(type), targetReg, sourceReg, type);
            if (!op1->AsLclVar()->IsLastUse(multiRegIndex) && fieldVarDsc->GetRegNum() != REG_STK)
            {
                // The old location is dying
                genUpdateRegLife(fieldVarDsc, /*isBorn*/ false, /*isDying*/ true DEBUGARG(op1));
                gcInfo.gcMarkRegSetNpt(genRegMask(sourceReg));
                genUpdateVarReg(fieldVarDsc, treeNode);

#ifdef USING_VARIABLE_LIVE_RANGE
                // Report the home change for this variable
                varLiveKeeper->siUpdateVariableLiveRange(fieldVarDsc, fieldVarNum);
#endif // USING_VARIABLE_LIVE_RANGE

                // The new location is going live
                genUpdateRegLife(fieldVarDsc, /*isBorn*/ true, /*isDying*/ false DEBUGARG(treeNode));
            }
        }
        else
        {
            type = op1->GetRegTypeByIndex(multiRegIndex);
            inst_RV_RV(ins_Copy(type), targetReg, sourceReg, type);
            // We never spill after a copy, so to produce the single register, we simply need to
            // update the GC info for the defined register.
            gcInfo.gcMarkRegPtrVal(targetReg, type);
        }
        return targetReg;
    }
    else
    {
        return sourceReg;
    }
}

#if defined(DEBUG) && defined(TARGET_XARCH)

//------------------------------------------------------------------------
// genStackPointerCheck: Generate code to check the stack pointer against a saved value.
// This is a debug check.
//
// Arguments:
//    doStackPointerCheck - If true, do the stack pointer check, otherwise do nothing.
//    lvaStackPointerVar  - The local variable number that holds the value of the stack pointer
//                          we are comparing against.
//
// Return Value:
//    None
//
void CodeGen::genStackPointerCheck(bool doStackPointerCheck, unsigned lvaStackPointerVar)
{
    if (doStackPointerCheck)
    {
        noway_assert(lvaStackPointerVar != 0xCCCCCCCC && compiler->lvaTable[lvaStackPointerVar].lvDoNotEnregister &&
                     compiler->lvaTable[lvaStackPointerVar].lvOnFrame);
        GetEmitter()->emitIns_S_R(INS_cmp, EA_PTRSIZE, REG_SPBASE, lvaStackPointerVar, 0);

        BasicBlock* sp_check = genCreateTempLabel();
        GetEmitter()->emitIns_J(INS_je, sp_check);
        instGen(INS_BREAKPOINT);
        genDefineTempLabel(sp_check);
    }
}

#endif // defined(DEBUG) && defined(TARGET_XARCH)

unsigned CodeGenInterface::getCurrentStackLevel() const
{
    return genStackLevel;
}

#ifdef USING_VARIABLE_LIVE_RANGE
#ifdef DEBUG
//------------------------------------------------------------------------
//                      VariableLiveRanges dumpers
//------------------------------------------------------------------------

// Dump "VariableLiveRange" when code has not been generated and we don't have so the assembly native offset
// but at least "emitLocation"s and "siVarLoc"
void CodeGenInterface::VariableLiveKeeper::VariableLiveRange::dumpVariableLiveRange(
    const CodeGenInterface* codeGen) const
{
    codeGen->dumpSiVarLoc(&m_VarLocation);
    printf(" [ ");
    m_StartEmitLocation.Print(codeGen->GetCompiler()->compMethodID);
    printf(", ");
    if (m_EndEmitLocation.Valid())
    {
        m_EndEmitLocation.Print(codeGen->GetCompiler()->compMethodID);
    }
    else
    {
        printf("NON_CLOSED_RANGE");
    }
    printf(" ]; ");
}

// Dump "VariableLiveRange" when code has been generated and we have the assembly native offset of each "emitLocation"
void CodeGenInterface::VariableLiveKeeper::VariableLiveRange::dumpVariableLiveRange(
    emitter* emit, const CodeGenInterface* codeGen) const
{
    assert(emit != nullptr);

    // "VariableLiveRanges" are created setting its location ("m_VarLocation") and the initial native offset
    // ("m_StartEmitLocation")
    codeGen->dumpSiVarLoc(&m_VarLocation);

    // If this is an open "VariableLiveRange", "m_EndEmitLocation" is non-valid and print -1
    UNATIVE_OFFSET endAssemblyOffset = m_EndEmitLocation.Valid() ? m_EndEmitLocation.CodeOffset(emit) : -1;

    printf(" [%X , %X )", m_StartEmitLocation.CodeOffset(emit), m_EndEmitLocation.CodeOffset(emit));
}

//------------------------------------------------------------------------
//                      LiveRangeDumper
//------------------------------------------------------------------------
//------------------------------------------------------------------------
// resetDumper: If the the "liveRange" has its last "VariableLiveRange" closed, it makes
//  the "LiveRangeDumper" points to end of "liveRange" (nullptr). In other case,
//  it makes the "LiveRangeDumper" points to the last "VariableLiveRange" of
//  "liveRange", which is opened.
//
// Arguments:
//  liveRanges - the "LiveRangeList" of the "VariableLiveDescriptor" we want to
//      udpate its "LiveRangeDumper".
//
// Notes:
//  This method is expected to be called once a the code for a BasicBlock has been
//  generated and all the new "VariableLiveRange"s of the variable during this block
//  has been dumped.
void CodeGenInterface::VariableLiveKeeper::LiveRangeDumper::resetDumper(const LiveRangeList* liveRanges)
{
    // There must have reported something in order to reset
    assert(m_hasLiveRangestoDump);

    if (liveRanges->back().m_EndEmitLocation.Valid())
    {
        // the last "VariableLiveRange" is closed and the variable
        // is no longer alive
        m_hasLiveRangestoDump = false;
    }
    else
    {
        // the last "VariableLiveRange" remains opened because it is
        // live at "BasicBlock"s "bbLiveOut".
        m_StartingLiveRange = liveRanges->backPosition();
    }
}

//------------------------------------------------------------------------
// setDumperStartAt: Make "LiveRangeDumper" instance points the last "VariableLiveRange"
// added so we can starts dumping from there after the actual "BasicBlock"s code is generated.
//
// Arguments:
//  liveRangeIt - an iterator to a position in "VariableLiveDescriptor::m_VariableLiveRanges"
//
// Return Value:
//  A const pointer to the "LiveRangeList" containing all the "VariableLiveRange"s
//  of the variable with index "varNum".
//
// Notes:
//  "varNum" should be always a valid inde ("varnum" < "m_LiveDscCount")
void CodeGenInterface::VariableLiveKeeper::LiveRangeDumper::setDumperStartAt(const LiveRangeListIterator liveRangeIt)
{
    m_hasLiveRangestoDump = true;
    m_StartingLiveRange   = liveRangeIt;
}

//------------------------------------------------------------------------
// getStartForDump: Return an iterator to the first "VariableLiveRange" edited/added
//  during the current "BasicBlock"
//
// Return Value:
//  A LiveRangeListIterator to the first "VariableLiveRange" in "LiveRangeList" which
//  was used during last "BasicBlock".
//
CodeGenInterface::VariableLiveKeeper::LiveRangeListIterator CodeGenInterface::VariableLiveKeeper::LiveRangeDumper::
    getStartForDump() const
{
    return m_StartingLiveRange;
}

//------------------------------------------------------------------------
// hasLiveRangesToDump: Retutn wheter at least a "VariableLiveRange" was alive during
//  the current "BasicBlock"'s code generation
//
// Return Value:
//  A boolean indicating indicating if there is at least a "VariableLiveRange"
//  that has been used for the variable during last "BasicBlock".
//
bool CodeGenInterface::VariableLiveKeeper::LiveRangeDumper::hasLiveRangesToDump() const
{
    return m_hasLiveRangestoDump;
}
#endif // DEBUG

//------------------------------------------------------------------------
//                      VariableLiveDescriptor
//------------------------------------------------------------------------

CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::VariableLiveDescriptor(CompAllocator allocator)
{
    // Initialize an empty list
    m_VariableLiveRanges = new (allocator) LiveRangeList(allocator);

    INDEBUG(m_VariableLifeBarrier = new (allocator) LiveRangeDumper(m_VariableLiveRanges));
}

//------------------------------------------------------------------------
// hasVariableLiveRangeOpen: Return true if the variable is still alive,
//  false in other case.
//
bool CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::hasVariableLiveRangeOpen() const
{
    return !m_VariableLiveRanges->empty() && !m_VariableLiveRanges->back().m_EndEmitLocation.Valid();
}

//------------------------------------------------------------------------
// getLiveRanges: Return the list of variable locations for this variable.
//
// Return Value:
//  A const LiveRangeList* pointing to the first variable location if it has
//  any or the end of the list in other case.
//
CodeGenInterface::VariableLiveKeeper::LiveRangeList* CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::
    getLiveRanges() const
{
    return m_VariableLiveRanges;
}

//------------------------------------------------------------------------
// startLiveRangeFromEmitter: Report this variable as being born in "varLocation"
//  since the instruction where "emit" is located.
//
// Arguments:
//  varLocation  - the home of the variable.
//  emit - an emitter* instance located at the first instruction from
//  where "varLocation" becomes valid.
//
// Assumptions:
//  This variable is being born so it should be dead.
//
// Notes:
//  The position of "emit" matters to ensure intervals inclusive of the
//  beginning and exclusive of the end.
//
void CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::startLiveRangeFromEmitter(
    CodeGenInterface::siVarLoc varLocation, emitter* emit) const
{
    noway_assert(emit != nullptr);

    // Is the first "VariableLiveRange" or the previous one has been closed so its "m_EndEmitLocation" is valid
    noway_assert(m_VariableLiveRanges->empty() || m_VariableLiveRanges->back().m_EndEmitLocation.Valid());

    if (!m_VariableLiveRanges->empty() &&
        siVarLoc::Equals(&varLocation, &(m_VariableLiveRanges->back().m_VarLocation)) &&
        m_VariableLiveRanges->back().m_EndEmitLocation.IsPreviousInsNum(emit))
    {
        JITDUMP("Extending debug range...\n");

        // The variable is being born just after the instruction at which it died.
        // In this case, i.e. an update of the variable's value, we coalesce the live ranges.
        m_VariableLiveRanges->back().m_EndEmitLocation.Init();
    }
    else
    {
        JITDUMP("New debug range: %s\n",
                m_VariableLiveRanges->empty()
                    ? "first"
                    : siVarLoc::Equals(&varLocation, &(m_VariableLiveRanges->back().m_VarLocation))
                          ? "new var or location"
                          : "not adjacent");
        // Creates new live range with invalid end
        m_VariableLiveRanges->emplace_back(varLocation, emitLocation(), emitLocation());
        m_VariableLiveRanges->back().m_StartEmitLocation.CaptureLocation(emit);
    }

#ifdef DEBUG
    if (!m_VariableLifeBarrier->hasLiveRangesToDump())
    {
        m_VariableLifeBarrier->setDumperStartAt(m_VariableLiveRanges->backPosition());
    }
#endif // DEBUG

    // startEmitLocationendEmitLocation has to be Valid and endEmitLocationendEmitLocation  not
    noway_assert(m_VariableLiveRanges->back().m_StartEmitLocation.Valid());
    noway_assert(!m_VariableLiveRanges->back().m_EndEmitLocation.Valid());
}

//------------------------------------------------------------------------
// endLiveRangeAtEmitter: Report this variable as becoming dead since the
//  instruction where "emit" is located.
//
// Arguments:
//  emit - an emitter* instance located at the first instruction from
//   this variable becomes dead.
//
// Assumptions:
//  This variable is becoming dead so it should be alive.
//
// Notes:
//  The position of "emit" matters to ensure intervals inclusive of the
//  beginning and exclusive of the end.
//
void CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::endLiveRangeAtEmitter(emitter* emit) const
{
    noway_assert(emit != nullptr);
    noway_assert(hasVariableLiveRangeOpen());

    // Using [close, open) ranges so as to not compute the size of the last instruction
    m_VariableLiveRanges->back().m_EndEmitLocation.CaptureLocation(emit);

    // No m_EndEmitLocation has to be Valid
    noway_assert(m_VariableLiveRanges->back().m_EndEmitLocation.Valid());
}

//------------------------------------------------------------------------
// UpdateLiveRangeAtEmitter: Report this variable as changing its variable
//  home to "varLocation" since the instruction where "emit" is located.
//
// Arguments:
//  varLocation  - the new variable location.
//  emit - an emitter* instance located at the first instruction from
//   where "varLocation" becomes valid.
//
// Assumptions:
//  This variable is being born so it should be dead.
//
// Notes:
//  The position of "emit" matters to ensure intervals inclusive of the
//  beginning and exclusive of the end.
//
void CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::updateLiveRangeAtEmitter(
    CodeGenInterface::siVarLoc varLocation, emitter* emit) const
{
    // This variable is changing home so it has been started before during this block
    noway_assert(m_VariableLiveRanges != nullptr && !m_VariableLiveRanges->empty());

    // And its last m_EndEmitLocation has to be invalid
    noway_assert(!m_VariableLiveRanges->back().m_EndEmitLocation.Valid());

    // If we are reporting again the same home, that means we are doing something twice?
    // noway_assert(! CodeGenInterface::siVarLoc::Equals(&m_VariableLiveRanges->back().m_VarLocation, varLocation));

    // Close previous live range
    endLiveRangeAtEmitter(emit);

    startLiveRangeFromEmitter(varLocation, emit);
}

#ifdef DEBUG
void CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::dumpAllRegisterLiveRangesForBlock(
    emitter* emit, const CodeGenInterface* codeGen) const
{
    printf("[");
    for (LiveRangeListIterator it = m_VariableLiveRanges->begin(); it != m_VariableLiveRanges->end(); it++)
    {
        it->dumpVariableLiveRange(emit, codeGen);
    }
    printf("]\n");
}

void CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::dumpRegisterLiveRangesForBlockBeforeCodeGenerated(
    const CodeGenInterface* codeGen) const
{
    noway_assert(codeGen != nullptr);

    printf("[");
    for (LiveRangeListIterator it = m_VariableLifeBarrier->getStartForDump(); it != m_VariableLiveRanges->end(); it++)
    {
        it->dumpVariableLiveRange(codeGen);
    }
    printf("]\n");
}

// Returns true if a live range for this variable has been recorded
bool CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::hasVarLiveRangesToDump() const
{
    return !m_VariableLiveRanges->empty();
}

// Returns true if a live range for this variable has been recorded from last call to EndBlock
bool CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::hasVarLiveRangesFromLastBlockToDump() const
{
    return m_VariableLifeBarrier->hasLiveRangesToDump();
}

// Reset the barrier so as to dump only next block changes on next block
void CodeGenInterface::VariableLiveKeeper::VariableLiveDescriptor::endBlockLiveRanges()
{
    // make "m_VariableLifeBarrier->m_StartingLiveRange" now points to nullptr for printing purposes
    m_VariableLifeBarrier->resetDumper(m_VariableLiveRanges);
}
#endif // DEBUG

//------------------------------------------------------------------------
//                      VariableLiveKeeper
//------------------------------------------------------------------------
// Initialize structures for VariableLiveRanges
void CodeGenInterface::initializeVariableLiveKeeper()
{
    CompAllocator allocator = compiler->getAllocator(CMK_VariableLiveRanges);

    int amountTrackedVariables = compiler->opts.compDbgInfo ? compiler->info.compLocalsCount : 0;
    int amountTrackedArgs      = compiler->opts.compDbgInfo ? compiler->info.compArgsCount : 0;

    varLiveKeeper = new (allocator) VariableLiveKeeper(amountTrackedVariables, amountTrackedArgs, compiler, allocator);
}

CodeGenInterface::VariableLiveKeeper* CodeGenInterface::getVariableLiveKeeper() const
{
    return varLiveKeeper;
};

//------------------------------------------------------------------------
// VariableLiveKeeper: Create an instance of the object in charge of managing
//  VariableLiveRanges and intialize the array "m_vlrLiveDsc".
//
// Arguments:
//    totalLocalCount   - the count of args, special args and IL Local
//      variables in the method.
//    argsCount         - the count of args and special args in the method.
//    compiler          - a compiler instance
//
CodeGenInterface::VariableLiveKeeper::VariableLiveKeeper(unsigned int  totalLocalCount,
                                                         unsigned int  argsCount,
                                                         Compiler*     comp,
                                                         CompAllocator allocator)
    : m_LiveDscCount(totalLocalCount)
    , m_LiveArgsCount(argsCount)
    , m_Compiler(comp)
    , m_LastBasicBlockHasBeenEmited(false)
{
    if (m_LiveDscCount > 0)
    {
        // Allocate memory for "m_vlrLiveDsc" and initialize each "VariableLiveDescriptor"
        m_vlrLiveDsc          = allocator.allocate<VariableLiveDescriptor>(m_LiveDscCount);
        m_vlrLiveDscForProlog = allocator.allocate<VariableLiveDescriptor>(m_LiveDscCount);

        for (unsigned int varNum = 0; varNum < m_LiveDscCount; varNum++)
        {
            new (m_vlrLiveDsc + varNum, jitstd::placement_t()) VariableLiveDescriptor(allocator);
            new (m_vlrLiveDscForProlog + varNum, jitstd::placement_t()) VariableLiveDescriptor(allocator);
        }
    }
}

//------------------------------------------------------------------------
// siStartOrCloseVariableLiveRange: Reports the given variable as beign born
//  or becoming dead.
//
// Arguments:
//    varDsc    - the variable for which a location changed will be reported
//    varNum    - the index of the variable in the "compiler->lvaTable"
//    isBorn    - whether the variable is being born from where the emitter is located.
//    isDying   - whether the variable is dying from where the emitter is located.
//
// Assumptions:
//    The emitter should be located on the first instruction from where is true that
//    the variable becoming valid (when isBorn is true) or invalid (when isDying is true).
//
// Notes:
//    This method is being called from treeLifeUpdater when the variable is being born,
//    becoming dead, or both.
//
void CodeGenInterface::VariableLiveKeeper::siStartOrCloseVariableLiveRange(const LclVarDsc* varDsc,
                                                                           unsigned int     varNum,
                                                                           bool             isBorn,
                                                                           bool             isDying)
{
    noway_assert(varDsc != nullptr);

    // Only the variables that exists in the IL, "this", and special arguments
    // are reported.
    if (m_Compiler->opts.compDbgInfo && varNum < m_LiveDscCount)
    {
        if (isBorn && !isDying)
        {
            // "varDsc" is valid from this point
            siStartVariableLiveRange(varDsc, varNum);
        }
        if (isDying && !isBorn)
        {
            // this variable live range is no longer valid from this point
            siEndVariableLiveRange(varNum);
        }
    }
}

//------------------------------------------------------------------------
// siStartOrCloseVariableLiveRanges: Iterates the given set of variables
//  calling "siStartOrCloseVariableLiveRange" with each one.
//
// Arguments:
//    varsIndexSet    - the set of variables to report start/end "VariableLiveRange"
//    isBorn    - whether the set is being born from where the emitter is located.
//    isDying   - whether the set is dying from where the emitter is located.
//
// Assumptions:
//    The emitter should be located on the first instruction from where is true that
//    the variable becoming valid (when isBorn is true) or invalid (when isDying is true).
//
// Notes:
//    This method is being called from treeLifeUpdater when a set of variables
//    is being born, becoming dead, or both.
//
void CodeGenInterface::VariableLiveKeeper::siStartOrCloseVariableLiveRanges(VARSET_VALARG_TP varsIndexSet,
                                                                            bool             isBorn,
                                                                            bool             isDying)
{
    if (m_Compiler->opts.compDbgInfo)
    {
        VarSetOps::Iter iter(m_Compiler, varsIndexSet);
        unsigned        varIndex = 0;
        while (iter.NextElem(&varIndex))
        {
            unsigned int     varNum = m_Compiler->lvaTrackedIndexToLclNum(varIndex);
            const LclVarDsc* varDsc = m_Compiler->lvaGetDesc(varNum);
            siStartOrCloseVariableLiveRange(varDsc, varNum, isBorn, isDying);
        }
    }
}

//------------------------------------------------------------------------
// siStartVariableLiveRange: Reports the given variable as being born.
//
// Arguments:
//    varDsc    - the variable for which a location changed will be reported
//    varNum    - the index of the variable to report home in lvLiveDsc
//
// Assumptions:
//    The emitter should be pointing to the first instruction from where the VariableLiveRange is
//    becoming valid.
//    The given "varDsc" should have its VariableRangeLists initialized.
//
// Notes:
//    This method should be called on every place a Variable is becoming alive.
void CodeGenInterface::VariableLiveKeeper::siStartVariableLiveRange(const LclVarDsc* varDsc, unsigned int varNum)
{
    noway_assert(varDsc != nullptr);

    // Only the variables that exists in the IL, "this", and special arguments
    // are reported.
    if (m_Compiler->opts.compDbgInfo && varNum < m_LiveDscCount)
    {
        // Build siVarLoc for this born "varDsc"
        CodeGenInterface::siVarLoc varLocation =
            m_Compiler->codeGen->getSiVarLoc(varDsc, m_Compiler->codeGen->getCurrentStackLevel());

        VariableLiveDescriptor* varLiveDsc = &m_vlrLiveDsc[varNum];
        // this variable live range is valid from this point
        varLiveDsc->startLiveRangeFromEmitter(varLocation, m_Compiler->GetEmitter());
    }
}

//------------------------------------------------------------------------
// siEndVariableLiveRange: Reports the variable as becoming dead.
//
// Arguments:
//    varNum    - the index of the variable at m_vlrLiveDsc or lvaTable in that
//       is becoming dead.
//
// Assumptions:
//    The given variable should be alive.
//    The emitter should be pointing to the first instruction from where the VariableLiveRange is
//    becoming invalid.
//
// Notes:
//    This method should be called on every place a Variable is becoming dead.
void CodeGenInterface::VariableLiveKeeper::siEndVariableLiveRange(unsigned int varNum)
{
    // Only the variables that exists in the IL, "this", and special arguments
    // will be reported.

    // This method is being called from genUpdateLife, and that one is called after
    // code for BasicBlock have been generated, but the emitter has no longer
    // a valid IG so we don't report the close of a "VariableLiveRange" after code is
    // emitted.

    if (m_Compiler->opts.compDbgInfo && varNum < m_LiveDscCount && !m_LastBasicBlockHasBeenEmited)
    {
        // this variable live range is no longer valid from this point
        m_vlrLiveDsc[varNum].endLiveRangeAtEmitter(m_Compiler->GetEmitter());
    }
}

//------------------------------------------------------------------------
// siUpdateVariableLiveRange: Reports the change of variable location for the
//  given variable.
//
// Arguments:
//    varDsc    - the variable for which tis home has changed.
//    varNum    - the index of the variable to report home in lvLiveDsc
//
// Assumptions:
//    The given variable should be alive.
//    The emitter should be pointing to the first instruction from where
//    the new variable location is becoming valid.
//
void CodeGenInterface::VariableLiveKeeper::siUpdateVariableLiveRange(const LclVarDsc* varDsc, unsigned int varNum)
{
    noway_assert(varDsc != nullptr);

    // Only the variables that exists in the IL, "this", and special arguments
    // will be reported. This are locals and arguments, and are counted in
    // "info.compLocalsCount".

    // This method is being called when the prolog is being generated, and
    // the emitter has no longer a valid IG so we don't report the close of
    //  a "VariableLiveRange" after code is emitted.
    if (m_Compiler->opts.compDbgInfo && varNum < m_LiveDscCount && !m_LastBasicBlockHasBeenEmited)
    {
        // Build the location of the variable
        CodeGenInterface::siVarLoc siVarLoc =
            m_Compiler->codeGen->getSiVarLoc(varDsc, m_Compiler->codeGen->getCurrentStackLevel());

        // Report the home change for this variable
        VariableLiveDescriptor* varLiveDsc = &m_vlrLiveDsc[varNum];
        varLiveDsc->updateLiveRangeAtEmitter(siVarLoc, m_Compiler->GetEmitter());
    }
}

//------------------------------------------------------------------------
// siEndAllVariableLiveRange: Reports the set of variables as becoming dead.
//
// Arguments:
//    newLife    - the set of variables that are becoming dead.
//
// Assumptions:
//    All the variables in the set are alive.
//
// Notes:
//    This method is called when the last block being generated to killed all
//    the live variables and set a flag to avoid reporting variable locations for
//    on next calls to method that update variable liveness.
void CodeGenInterface::VariableLiveKeeper::siEndAllVariableLiveRange(VARSET_VALARG_TP varsToClose)
{
    if (m_Compiler->opts.compDbgInfo)
    {
        if (m_Compiler->lvaTrackedCount > 0 || !m_Compiler->opts.OptimizationDisabled())
        {
            VarSetOps::Iter iter(m_Compiler, varsToClose);
            unsigned        varIndex = 0;
            while (iter.NextElem(&varIndex))
            {
                unsigned int varNum = m_Compiler->lvaTrackedIndexToLclNum(varIndex);
                siEndVariableLiveRange(varNum);
            }
        }
        else
        {
            // It seems we are jitting debug code, so we don't have variable
            //  liveness info
            siEndAllVariableLiveRange();
        }
    }

    m_LastBasicBlockHasBeenEmited = true;
}

//------------------------------------------------------------------------
// siEndAllVariableLiveRange: Reports all live variables as dead.
//
// Notes:
//    This overload exists for the case we are jitting code compiled in
//    debug mode. When that happen we don't have variable liveness info
//    as "BaiscBlock::bbLiveIn" or "BaiscBlock::bbLiveOut" and there is no
//    tracked variable.
//
void CodeGenInterface::VariableLiveKeeper::siEndAllVariableLiveRange()
{
    // TODO: we can improve this keeping a set for the variables with
    // open VariableLiveRanges

    for (unsigned int varNum = 0; varNum < m_LiveDscCount; varNum++)
    {
        const VariableLiveDescriptor* varLiveDsc = m_vlrLiveDsc + varNum;
        if (varLiveDsc->hasVariableLiveRangeOpen())
        {
            siEndVariableLiveRange(varNum);
        }
    }
}

//------------------------------------------------------------------------
// getLiveRangesForVarForBody: Return the "VariableLiveRange" that correspond to
//  the given "varNum".
//
// Arguments:
//  varNum  - the index of the variable in m_vlrLiveDsc, which is the same as
//      in lvaTable.
//
// Return Value:
//  A const pointer to the list of variable locations reported for the variable.
//
// Assumptions:
//  This variable should be an argument, a special argument or an IL local
//  variable.
CodeGenInterface::VariableLiveKeeper::LiveRangeList* CodeGenInterface::VariableLiveKeeper::getLiveRangesForVarForBody(
    unsigned int varNum) const
{
    // There should be at least one variable for which its liveness is tracked
    noway_assert(varNum < m_LiveDscCount);

    return m_vlrLiveDsc[varNum].getLiveRanges();
}

//------------------------------------------------------------------------
// getLiveRangesForVarForProlog: Return the "VariableLiveRange" that correspond to
//  the given "varNum".
//
// Arguments:
//  varNum  - the index of the variable in m_vlrLiveDsc, which is the same as
//      in lvaTable.
//
// Return Value:
//  A const pointer to the list of variable locations reported for the variable.
//
// Assumptions:
//  This variable should be an argument, a special argument or an IL local
//  variable.
CodeGenInterface::VariableLiveKeeper::LiveRangeList* CodeGenInterface::VariableLiveKeeper::getLiveRangesForVarForProlog(
    unsigned int varNum) const
{
    // There should be at least one variable for which its liveness is tracked
    noway_assert(varNum < m_LiveDscCount);

    return m_vlrLiveDscForProlog[varNum].getLiveRanges();
}

//------------------------------------------------------------------------
// getLiveRangesCount: Returns the count of variable locations reported for the tracked
//  variables, which are arguments, special arguments, and local IL variables.
//
// Return Value:
//    size_t - the count of variable locations
//
// Notes:
//    This method is being called from "genSetScopeInfo" to know the count of
//    "varResultInfo" that should be created on eeSetLVcount.
//
size_t CodeGenInterface::VariableLiveKeeper::getLiveRangesCount() const
{
    size_t liveRangesCount = 0;

    if (m_Compiler->opts.compDbgInfo)
    {
        for (unsigned int varNum = 0; varNum < m_LiveDscCount; varNum++)
        {
            for (int i = 0; i < 2; i++)
            {
                VariableLiveDescriptor* varLiveDsc = (i == 0 ? m_vlrLiveDscForProlog : m_vlrLiveDsc) + varNum;

                if (m_Compiler->compMap2ILvarNum(varNum) != (unsigned int)ICorDebugInfo::UNKNOWN_ILNUM)
                {
                    liveRangesCount += varLiveDsc->getLiveRanges()->size();
                }
            }
        }
    }
    return liveRangesCount;
}

//------------------------------------------------------------------------
// psiStartVariableLiveRange: Reports the given variable as being born.
//
// Arguments:
//  varLcation  - the variable location
//  varNum      - the index of the variable in "compiler->lvaTable" or
//      "VariableLivekeeper->m_vlrLiveDsc"
//
// Notes:
//  This function is expected to be called from "psiBegProlog" during
//  prolog code generation.
//
void CodeGenInterface::VariableLiveKeeper::psiStartVariableLiveRange(CodeGenInterface::siVarLoc varLocation,
                                                                     unsigned int               varNum)
{
    // This descriptor has to correspond to a parameter. The first slots in lvaTable
    // are arguments and special arguments.
    noway_assert(varNum < m_LiveArgsCount);

    VariableLiveDescriptor* varLiveDsc = &m_vlrLiveDscForProlog[varNum];
    varLiveDsc->startLiveRangeFromEmitter(varLocation, m_Compiler->GetEmitter());
}

//------------------------------------------------------------------------
// psiClosePrologVariableRanges: Report all the parameters as becoming dead.
//
// Notes:
//  This function is expected to be called from preffix "psiEndProlog" after
//  code for prolog has been generated.
//
void CodeGenInterface::VariableLiveKeeper::psiClosePrologVariableRanges()
{
    noway_assert(m_LiveArgsCount <= m_LiveDscCount);

    for (unsigned int varNum = 0; varNum < m_LiveArgsCount; varNum++)
    {
        VariableLiveDescriptor* varLiveDsc = m_vlrLiveDscForProlog + varNum;

        if (varLiveDsc->hasVariableLiveRangeOpen())
        {
            varLiveDsc->endLiveRangeAtEmitter(m_Compiler->GetEmitter());
        }
    }
}

#ifdef DEBUG
void CodeGenInterface::VariableLiveKeeper::dumpBlockVariableLiveRanges(const BasicBlock* block)
{
    // "block" will be dereferenced
    noway_assert(block != nullptr);

    bool hasDumpedHistory = false;

    if (m_Compiler->verbose)
    {
        printf("////////////////////////////////////////\n");
        printf("////////////////////////////////////////\n");
        printf("Variable Live Range History Dump for Block %d \n", block->bbNum);

        if (m_Compiler->opts.compDbgInfo)
        {
            for (unsigned int varNum = 0; varNum < m_LiveDscCount; varNum++)
            {
                VariableLiveDescriptor* varLiveDsc = m_vlrLiveDsc + varNum;

                if (varLiveDsc->hasVarLiveRangesFromLastBlockToDump())
                {
                    hasDumpedHistory = true;
                    printf("IL Var Num %d:\n", m_Compiler->compMap2ILvarNum(varNum));
                    varLiveDsc->dumpRegisterLiveRangesForBlockBeforeCodeGenerated(m_Compiler->codeGen);
                    varLiveDsc->endBlockLiveRanges();
                }
            }
        }

        if (!hasDumpedHistory)
        {
            printf("..None..\n");
        }

        printf("////////////////////////////////////////\n");
        printf("////////////////////////////////////////\n");
        printf("End Generating code for Block %d \n", block->bbNum);
    }
}

void CodeGenInterface::VariableLiveKeeper::dumpLvaVariableLiveRanges() const
{
    bool hasDumpedHistory = false;

    if (m_Compiler->verbose)
    {
        printf("////////////////////////////////////////\n");
        printf("////////////////////////////////////////\n");
        printf("PRINTING VARIABLE LIVE RANGES:\n");

        if (m_Compiler->opts.compDbgInfo)
        {
            for (unsigned int varNum = 0; varNum < m_LiveDscCount; varNum++)
            {
                VariableLiveDescriptor* varLiveDsc = m_vlrLiveDsc + varNum;

                if (varLiveDsc->hasVarLiveRangesToDump())
                {
                    hasDumpedHistory = true;
                    printf("IL Var Num %d:\n", m_Compiler->compMap2ILvarNum(varNum));
                    varLiveDsc->dumpAllRegisterLiveRangesForBlock(m_Compiler->GetEmitter(), m_Compiler->codeGen);
                }
            }
        }

        if (!hasDumpedHistory)
        {
            printf("..None..\n");
        }

        printf("////////////////////////////////////////\n");
        printf("////////////////////////////////////////\n");
    }
}
#endif // DEBUG
#endif // USING_VARIABLE_LIVE_RANGE
