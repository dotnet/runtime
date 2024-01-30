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
#include "optcse.h" // for cse metrics

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

#if defined(TARGET_XARCH)
void CodeGenInterface::CopyRegisterInfo()
{
#if defined(TARGET_AMD64)
    rbmAllFloat       = compiler->rbmAllFloat;
    rbmFltCalleeTrash = compiler->rbmFltCalleeTrash;
#endif // TARGET_AMD64

    rbmAllMask        = compiler->rbmAllMask;
    rbmMskCalleeTrash = compiler->rbmMskCalleeTrash;
}
#endif // TARGET_XARCH

/*****************************************************************************/

CodeGen::CodeGen(Compiler* theCompiler) : CodeGenInterface(theCompiler)
{
#if defined(TARGET_XARCH)
    negBitmaskFlt  = nullptr;
    negBitmaskDbl  = nullptr;
    absBitmaskFlt  = nullptr;
    absBitmaskDbl  = nullptr;
    zroSimd12Elm3  = nullptr;
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
#endif // TARGET_AMD64

    compiler->genCallSite2DebugInfoMap = nullptr;

    /* Assume that we not fully interruptible */

    SetInterruptible(false);
#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    SetHasTailCalls(false);
#endif // TARGET_ARMARCH || TARGET_LOONGARCH64 || TARGET_RISCV64
#ifdef DEBUG
    genInterruptibleUsed = false;
    genCurDispOffset     = (unsigned)-1;
#endif

#ifdef TARGET_ARM64
    genSaveFpLrWithAllCalleeSavedRegisters = false;
    genForceFuncletFrameType5              = false;
#endif // TARGET_ARM64
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
//
// No labels will be in the IR before now, but future codegen might annotate additional blocks
// with this flag, such as "switch" codegen, or codegen-created blocks from genCreateTempLabel().
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
    for (BasicBlock* const block : compiler->Blocks())
    {
        assert(!block->HasFlag(BBF_HAS_LABEL));
    }
#endif // DEBUG

    // The first block is special; it always needs a label. This is to properly set up GC info.
    JITDUMP("  " FMT_BB " : first block\n", compiler->fgFirstBB->bbNum);
    compiler->fgFirstBB->SetFlags(BBF_HAS_LABEL);

    // The current implementation of switch tables requires the first block to have a label so it
    // can generate offsets to the switch label targets.
    // (This is duplicative with the fact we always set the first block with a label above.)
    // TODO-CQ: remove this when switches have been re-implemented to not use this.
    if (compiler->fgHasSwitch)
    {
        JITDUMP("  " FMT_BB " : function has switch; mark first block\n", compiler->fgFirstBB->bbNum);
        compiler->fgFirstBB->SetFlags(BBF_HAS_LABEL);
    }

    for (BasicBlock* const block : compiler->Blocks())
    {
        switch (block->GetKind())
        {
            case BBJ_ALWAYS:
                // If we can skip this jump, don't create a label for the target
                if (block->CanRemoveJumpToNext(compiler))
                {
                    break;
                }

                FALLTHROUGH;

            case BBJ_EHCATCHRET:
                JITDUMP("  " FMT_BB " : branch target\n", block->GetTarget()->bbNum);
                block->GetTarget()->SetFlags(BBF_HAS_LABEL);
                break;

            case BBJ_COND:
                JITDUMP("  " FMT_BB " : branch target\n", block->GetTrueTarget()->bbNum);
                block->GetTrueTarget()->SetFlags(BBF_HAS_LABEL);

                // If we need a jump to the false target, give it a label
                if (!block->CanRemoveJumpToTarget(block->GetFalseTarget(), compiler))
                {
                    JITDUMP("  " FMT_BB " : branch target\n", block->GetFalseTarget()->bbNum);
                    block->GetFalseTarget()->SetFlags(BBF_HAS_LABEL);
                }
                break;

            case BBJ_SWITCH:
                for (BasicBlock* const bTarget : block->SwitchTargets())
                {
                    JITDUMP("  " FMT_BB " : switch target\n", bTarget->bbNum);
                    bTarget->SetFlags(BBF_HAS_LABEL);
                }
                break;

            case BBJ_CALLFINALLY:
                // The finally target itself will get marked by walking the EH table, below, and marking
                // all handler begins.
                CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_EH_CALLFINALLY_THUNKS
                {
                    // For callfinally thunks, we need to mark the block following the callfinally/callfinallyret pair,
                    // as that's needed for identifying the range of the "duplicate finally" region in EH data.
                    BasicBlock* bbToLabel = block->Next();
                    if (block->isBBCallFinallyPair())
                    {
                        bbToLabel = bbToLabel->Next(); // skip the BBJ_CALLFINALLYRET
                    }
                    if (bbToLabel != nullptr)
                    {
                        JITDUMP("  " FMT_BB " : callfinally thunk region end\n", bbToLabel->bbNum);
                        bbToLabel->SetFlags(BBF_HAS_LABEL);
                    }
                }
#endif // FEATURE_EH_CALLFINALLY_THUNKS

                break;

            case BBJ_CALLFINALLYRET:
                JITDUMP("  " FMT_BB " : finally continuation\n", block->GetFinallyContinuation()->bbNum);
                block->GetFinallyContinuation()->SetFlags(BBF_HAS_LABEL);
                break;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFAULTRET:
            case BBJ_EHFILTERRET: // The filter-handler will get marked when processing the EH handlers, below.
            case BBJ_RETURN:
            case BBJ_THROW:
                break;

            default:
                noway_assert(!"Unexpected bbKind");
                break;
        }
    }

    // Walk all the exceptional code blocks and mark them, since they don't appear in the normal flow graph.
    for (Compiler::AddCodeDsc* add = compiler->fgAddCodeList; add != nullptr; add = add->acdNext)
    {
        if (add->acdUsed)
        {
            JITDUMP("  " FMT_BB " : throw helper block\n", add->acdDstBlk->bbNum);
            add->acdDstBlk->SetFlags(BBF_HAS_LABEL);
        }
    }

    for (EHblkDsc* const HBtab : EHClauses(compiler))
    {
        HBtab->ebdTryBeg->SetFlags(BBF_HAS_LABEL);
        HBtab->ebdHndBeg->SetFlags(BBF_HAS_LABEL);

        JITDUMP("  " FMT_BB " : try begin\n", HBtab->ebdTryBeg->bbNum);
        JITDUMP("  " FMT_BB " : hnd begin\n", HBtab->ebdHndBeg->bbNum);

        if (!HBtab->ebdTryLast->IsLast())
        {
            HBtab->ebdTryLast->Next()->SetFlags(BBF_HAS_LABEL);
            JITDUMP("  " FMT_BB " : try end\n", HBtab->ebdTryLast->Next()->bbNum);
        }

        if (!HBtab->ebdHndLast->IsLast())
        {
            HBtab->ebdHndLast->Next()->SetFlags(BBF_HAS_LABEL);
            JITDUMP("  " FMT_BB " : hnd end\n", HBtab->ebdHndLast->Next()->bbNum);
        }

        if (HBtab->HasFilter())
        {
            HBtab->ebdFilter->SetFlags(BBF_HAS_LABEL);
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

    regNumber reg = varDsc->GetRegNum();
    if (genIsValidFloatReg(reg))
    {
        regMask = genRegMaskFloat(reg ARM_ARG(varDsc->GetRegisterType()));
    }
    else
    {
        regMask = genRegMask(reg);
    }
    return regMask;
}

// Return the register mask for the given lclVar or regVar tree node
// inline
regMaskTP CodeGenInterface::genGetRegMask(GenTree* tree)
{
    assert(tree->gtOper == GT_LCL_VAR);

    regMaskTP        regMask = RBM_NONE;
    const LclVarDsc* varDsc  = compiler->lvaGetDesc(tree->AsLclVarCommon());
    if (varDsc->lvPromoted)
    {
        for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
        {
            const LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(i);
            noway_assert(fieldVarDsc->lvIsStructField);
            if (fieldVarDsc->lvIsInReg())
            {
                regMask |= genGetRegMask(fieldVarDsc);
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
        printf("\t\t\t\t\t\t\tV%02u in reg ", compiler->lvaGetLclNum(varDsc));

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
        // in the case of an exception or "spill at single-def" variable, which may be already treated
        // as live in the register.
        assert(varDsc->IsAlwaysAliveInMemory() || ((regSet.GetMaskVars() & regMask) == 0));
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
//              a) LSRA needs several changes to accommodate more precise killsets
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
        case CORINFO_HELP_ASSIGN_REF:
        case CORINFO_HELP_CHECKED_ASSIGN_REF:
            return RBM_CALLEE_TRASH_WRITEBARRIER;

        case CORINFO_HELP_ASSIGN_BYREF:
            return RBM_CALLEE_TRASH_WRITEBARRIER_BYREF;

        case CORINFO_HELP_PROF_FCN_ENTER:
            return RBM_PROFILER_ENTER_TRASH;

        case CORINFO_HELP_PROF_FCN_LEAVE:
            return RBM_PROFILER_LEAVE_TRASH;

        case CORINFO_HELP_PROF_FCN_TAILCALL:
            return RBM_PROFILER_TAILCALL_TRASH;

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
#endif

        case CORINFO_HELP_STOP_FOR_GC:
            return RBM_STOP_FOR_GC_TRASH;

        case CORINFO_HELP_INIT_PINVOKE_FRAME:
            return RBM_INIT_PINVOKE_FRAME_TRASH;

        case CORINFO_HELP_VALIDATE_INDIRECT_CALL:
            return RBM_VALIDATE_INDIRECT_CALL_TRASH;

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
//    If "ForCodeGen" is false, only "compCurLife" set (and no mask) will be updated.
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
        bool       isInMemory = !isInReg || varDsc->IsAlwaysAliveInMemory();

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

        codeGen->getVariableLiveKeeper()->siEndVariableLiveRange(varNum);
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
            // unless it is an EH/"spill at single-def" var, which always remains live on the stack.
            if (!varDsc->IsAlwaysAliveInMemory())
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

        codeGen->getVariableLiveKeeper()->siStartVariableLiveRange(varDsc, varNum);
    }
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

    // Label doesn't need a jump kind
    BasicBlock* block = BasicBlock::New(compiler);

#ifdef DEBUG
    compiler->fgSafeBasicBlockCreation = false;
#endif

    JITDUMP("Mark " FMT_BB " as label: codegen temp block\n", block->bbNum);
    block->SetFlags(BBF_HAS_LABEL);

    // Use coldness of current block, as this label will
    // be contained in it.
    block->CopyFlags(compiler->compCurBB, BBF_COLD);

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
                                                     gcInfo.gcRegByrefSetCur DEBUG_ARG(label));
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
        // at this point if a jump to this block is made in the middle of pushing arguments.
        //
        // Here we restore SP to prevent potential stack alignment issues.
        GetEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, -genSPtoFPdelta());
    }
#endif

    if (!isFramePointerUsed() && compiler->fgIsThrowHlpBlk(block))
    {
        noway_assert(block->HasFlag(BBF_HAS_LABEL));

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
 *      *mulPtr     ...     optional multiplier (2/4/8) for rv2
 *                          Note that for [reg1 + reg2] and [reg1 + reg2 + icon], *mulPtr == 0.
 *      *cnsPtr     ...     integer constant [optional]
 *
 *  IMPORTANT NOTE: This routine doesn't generate any code, it merely
 *                  identifies the components that might be used to
 *                  form an address mode later on.
 */

bool CodeGen::genCreateAddrMode(
    GenTree* addr, bool fold, bool* revPtr, GenTree** rv1Ptr, GenTree** rv2Ptr, unsigned* mulPtr, ssize_t* cnsPtr)
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

    if (!addr->OperIs(GT_ADD))
    {
        return false;
    }

    GenTree* rv1 = nullptr;
    GenTree* rv2 = nullptr;

    GenTree* op1;
    GenTree* op2;

    ssize_t  cns;
    unsigned mul;

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

    // Can't use indirect addressing mode as we need to check for overflow.
    // Also, can't use 'lea' as it doesn't set the flags.

    if (addr->gtOverflow())
    {
        return false;
    }

    bool rev = false; // Is op2 first in the evaluation order?

    /*
        A complex address mode can combine the following operands:

            op1     ...     base address
            op2     ...     optional scaled index
            mul     ...     optional multiplier (2/4/8) for op2
            cns     ...     optional displacement

        Here we try to find such a set of operands and arrange for these
        to sit in registers.
     */

    cns = 0;
    mul = 0;

AGAIN:
    /* We come back to 'AGAIN' if we have an add of a constant, and we are folding that
       constant, or we have gone through a GT_NOP or GT_COMMA node. We never come back
       here if we find a scaled index.
    */
    CLANG_FORMAT_COMMENT_ANCHOR;

    assert(mul == 0);

    /* Special case: keep constants as 'op2', but don't do this for constant handles
       because they don't fit I32 that we're going to check for below anyway. */

    if (op1->IsCnsIntOrI() && !op1->IsIconHandle())
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
        if (!op2->AsIntConCommon()->ImmedValCanBeFolded(compiler, addr->OperGet()))
        {
            assert(compiler->opts.compReloc);
            return false;
        }

        /* We're adding a constant */

        cns += op2->AsIntConCommon()->IconValue();

#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
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

#if !defined(TARGET_ARMARCH) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
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
#endif // !defined(TARGET_ARMARCH) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)

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
#if !defined(TARGET_ARMARCH) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
        // TODO-ARM64-CQ, TODO-ARM-CQ: For now we don't try to create a scaled index.
        case GT_ADD:

            if (op1->gtOverflow())
            {
                break;
            }

            if (op1->AsOp()->gtOp2->IsIntCnsFitsInI32())
            {
                GenTreeIntCon* addConst = op1->AsOp()->gtOp2->AsIntCon();

                if (addConst->ImmedValCanBeFolded(compiler, GT_ADD) && FitsIn<INT32>(cns + addConst->IconValue()))
                {
                    cns += addConst->IconValue();
                    op1 = op1->AsOp()->gtOp1;

                    goto AGAIN;
                }
            }
            break;

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
#endif // !TARGET_ARMARCH && !TARGET_LOONGARCH64 && !TARGET_RISCV64

        case GT_COMMA:

            op1 = op1->AsOp()->gtOp2;
            goto AGAIN;

        default:
            break;
    }

    noway_assert(op2);
    switch (op2->gtOper)
    {
#if !defined(TARGET_ARMARCH) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
        // TODO-ARM64-CQ, TODO-ARM-CQ: For now we only handle MUL and LSH because
        // arm doesn't support both scale and offset at the same. Offset is handled
        // at the emitter as a peephole optimization.
        case GT_ADD:

            if (op2->gtOverflow())
            {
                break;
            }

            if (op2->AsOp()->gtOp2->IsIntCnsFitsInI32())
            {
                GenTreeIntCon* addConst = op2->AsOp()->gtOp2->AsIntCon();

                if (addConst->ImmedValCanBeFolded(compiler, GT_ADD) && FitsIn<INT32>(cns + addConst->IconValue()))
                {
                    cns += addConst->IconValue();
                    op2 = op2->AsOp()->gtOp1;
                }

                goto AGAIN;
            }
            break;

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
#endif // TARGET_ARMARCH || TARGET_LOONGARCH64 || TARGET_RISCV64

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
        // Make sure a GC address doesn't end up in 'rv2'
        if (varTypeIsGC(rv2->TypeGet()))
        {
            std::swap(rv1, rv2);
            rev = !rev;
        }

        // Special case: constant array index (that is range-checked)
        if (fold)
        {
            // By default, assume index is rv2 and indexScale is mul (or 1 if mul is zero)
            GenTree* index      = rv2;
            ssize_t  indexScale = mul == 0 ? 1 : mul;

            if (rv2->OperIs(GT_MUL, GT_LSH) && (rv2->gtGetOp2()->IsCnsIntOrI()))
            {
                indexScale *= compiler->optGetArrayRefScaleAndIndex(rv2, &index DEBUGARG(false));
            }

            // "index * 0" means index is zero
            if (indexScale == 0)
            {
                mul = 0;
                rv2 = nullptr;
            }
            else if (index->IsIntCnsFitsInI32())
            {
                ssize_t constantIndex = index->AsIntConCommon()->IconValue() * indexScale;
                if (constantIndex == 0)
                {
                    // while scale is a non-zero constant, the actual index is zero so drop it
                    mul = 0;
                    rv2 = nullptr;
                }
                else if (FitsIn<INT32>(cns + constantIndex))
                {
                    // Add the constant index to the accumulated offset value
                    cns += constantIndex;
                    // and get rid of index
                    mul = 0;
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
    *mulPtr = mul;
    *cnsPtr = cns;

    return true;
}

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
    genIPmappingAdd(IPmappingDscKind::Epilog, DebugInfo(), true);

    bool jmpEpilog = block->HasFlag(BBF_HAS_JMP);
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
            assert(add->acdUsed);
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
            assert(add->acdUsed);
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
        // we will jump around it in the normal non-exception case.

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

#if !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
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

    // Jump to the block which will throw the exception

    genJumpToThrowHlpBlk(jumpKind, SCK_OVERFLOW);
}
#endif

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
    if (block->HasFlag(BBF_FUNCLET_BEG))
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

    genWriteBarrierUsed = false;
#endif

    this->codePtr          = codePtr;
    this->nativeSizeOfCode = nativeSizeOfCode;

    DoPhase(this, PHASE_GENERATE_CODE, &CodeGen::genGenerateMachineCode);
    DoPhase(this, PHASE_EMIT_CODE, &CodeGen::genEmitMachineCode);
    DoPhase(this, PHASE_EMIT_GCEH, &CodeGen::genEmitUnwindDebugGCandEH);

#ifdef DEBUG
    // For R2R/NAOT not all these helpers are implemented. So don't ask for them.
    //
    if (genWriteBarrierUsed && JitConfig.EnableExtraSuperPmiQueries() && !compiler->opts.IsReadyToRun())
    {
        void* ignored;
        for (int i = CORINFO_HELP_ASSIGN_REF; i <= CORINFO_HELP_ASSIGN_STRUCT; i++)
        {
            compiler->compGetHelperFtn((CorInfoHelpFunc)i, &ignored);
        }
    }
#endif
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
#endif
    compiler->compCurBB = compiler->fgFirstBB;

    if (compiler->opts.disAsm)
    {
#ifdef DEBUG
        const char* fullName = compiler->info.compFullName;
#else
        const char* fullName = compiler->eeGetMethodFullName(compiler->info.compMethodHnd);
#endif

        printf("; Assembly listing for method %s (%s)\n", fullName, compiler->compGetTieringName(true));

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

#if defined(TARGET_X86)
        if (compiler->canUseEvexEncoding())
        {
            printf("X86 with AVX512");
        }
        else if (compiler->canUseVexEncoding())
        {
            printf("X86 with AVX");
        }
        else
        {
            printf("generic X86");
        }
#elif defined(TARGET_AMD64)
        if (compiler->canUseEvexEncoding())
        {
            printf("X64 with AVX512");
        }
        else if (compiler->canUseVexEncoding())
        {
            printf("X64 with AVX");
        }
        else
        {
            printf("generic X64");
        }
#elif defined(TARGET_ARM)
        printf("generic ARM");
#elif defined(TARGET_ARM64)
        printf("generic ARM64");
#elif defined(TARGET_LOONGARCH64)
        printf("generic LOONGARCH64");
#elif defined(TARGET_RISCV64)
        printf("generic RISCV64");
#else
        printf("unknown architecture");
#endif

        if (TargetOS::IsWindows)
        {
            printf(" - Windows");
        }
        else if (TargetOS::IsApplePlatform)
        {
            printf(" - Apple");
        }
        else if (TargetOS::IsUnix)
        {
            printf(" - Unix");
        }

        printf("\n");

        printf("; %s code\n", compiler->compGetTieringName(false));

        if (compiler->IsTargetAbi(CORINFO_NATIVEAOT_ABI))
        {
            printf("; NativeAOT compilation\n");
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
        else if (compiler->opts.compDbgEnC)
        {
            printf("; EnC code\n");
        }
        else if (compiler->opts.compDbgCode)
        {
            printf("; debuggable code\n");
        }

        if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_BBOPT) && compiler->fgHaveProfileWeights())
        {
            printf("; optimized using %s\n", compiler->compGetPgoSourceName());
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

        if (compiler->fgHaveProfileWeights())
        {
            printf("; with %s: edge weights are %s, and fgCalledCount is " FMT_WT "\n",
                   compiler->compGetPgoSourceName(), compiler->fgHaveValidEdgeWeights ? "valid" : "invalid",
                   compiler->fgCalledCount);
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

        if (compiler->opts.IsCFGEnabled())
        {
            printf("; control-flow guard enabled\n");
        }

        if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_ALT_JIT))
        {
            printf("; invoked as altjit\n");
        }
    }

    // We compute the final frame layout before code generation. This is because LSRA
    // has already computed exactly the maximum concurrent number of spill temps of each type that are
    // required during code generation. So, there is nothing left to estimate: we can be precise in the frame
    // layout. This helps us generate smaller code, and allocate, after code generation, a smaller amount of
    // memory from the VM.

    genFinalizeFrame();

    GetEmitter()->emitBegFN(isFramePointerUsed()
#if defined(DEBUG)
                                ,
                            (compiler->compCodeOpt() != Compiler::SMALL_CODE) &&
                                !compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT)
#endif
                                );

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

    // check to see if any jumps can be removed
    GetEmitter()->emitRemoveJumpToNextInst();

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
        // Use DOTNET_JitNoForceFallback=1 to prevent NOWAY assert testing from happening,
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

#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    trackedStackPtrsContig = false;
#elif defined(TARGET_ARM)
    // On arm due to prespilling of arguments, tracked stk-ptrs may not be contiguous
    trackedStackPtrsContig = !compiler->opts.compDbgEnC && !compiler->compIsProfilerHookNeeded();
#else
    trackedStackPtrsContig = !compiler->opts.compDbgEnC;
#endif

    if (compiler->opts.disAsm && compiler->opts.disTesting)
    {
        printf("; BEGIN METHOD %s\n", compiler->eeGetMethodFullName(compiler->info.compMethodHnd));
    }

    codeSize =
        GetEmitter()->emitEndCodeGen(compiler, trackedStackPtrsContig, GetInterruptible(), IsFullPtrRegMapRequired(),
                                     compiler->compHndBBtabCount, &prologSize, &epilogSize, codePtr, &codePtrRW,
                                     &coldCodePtr, &coldCodePtrRW, &consPtr, &consPtrRW DEBUGARG(&instrCount));

#ifdef DEBUG
    assert(compiler->compCodeGenDone == false);

    /* We're done generating code for this function */
    compiler->compCodeGenDone = true;
#endif

    if (compiler->opts.disAsm && compiler->opts.disTesting)
    {
        printf("; END METHOD %s\n", compiler->eeGetMethodFullName(compiler->info.compMethodHnd));
    }

#ifdef DEBUG
    const bool dspMetrics     = compiler->opts.dspMetrics;
    const bool dspSummary     = compiler->opts.disAsm || verbose;
    const bool dspMetricsOnly = dspMetrics && !dspSummary;

    if (dspSummary || dspMetrics)
    {
        if (!dspMetricsOnly)
        {
            printf("\n");
        }

        printf("; Total bytes of code %d, prolog size %d, PerfScore %.2f, instruction count %d, allocated bytes for "
               "code %d",
               codeSize, prologSize, compiler->info.compPerfScore, instrCount,
               GetEmitter()->emitTotalHotCodeSize + GetEmitter()->emitTotalColdCodeSize);

        if (dspMetrics)
        {
            printf(", num cse %d num cand %d", compiler->optCSEcount, compiler->optCSECandidateCount);

            CSE_HeuristicCommon* const cseHeuristic = compiler->optGetCSEheuristic();
            if (cseHeuristic != nullptr)
            {
                cseHeuristic->DumpMetrics();
            }

            if (compiler->info.compMethodSuperPMIIndex >= 0)
            {
                printf(" spmi index %d", compiler->info.compMethodSuperPMIIndex);
            }
        }

#if TRACK_LSRA_STATS
        if (JitConfig.DisplayLsraStats() == 3)
        {
            compiler->m_pLinearScan->dumpLsraStatsSummary(jitstdout());
        }
#endif // TRACK_LSRA_STATS

        printf(" (MethodHash=%08x) for method %s (%s)\n", compiler->info.compMethodHash(), compiler->info.compFullName,
               compiler->compGetTieringName(true));

        if (!dspMetricsOnly)
        {
            printf("; ============================================================\n\n");
        }
        printf(""); // in our logic this causes a flush
    }

    if (verbose)
    {
        printf("*************** After end code gen, before unwindEmit()\n");
        GetEmitter()->emitDispIGlist(/* displayInstructions */ true);
    }
#else
    if (compiler->opts.disAsm)
    {
        printf("\n; Total bytes of code %d\n\n", codeSize);
    }
#endif

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

    genReportRichDebugInfo();

    /* Finalize the Local Var info in terms of generated code */

    genSetScopeInfo();

#if defined(LATE_DISASM) || defined(DEBUG)
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
#endif // defined(LATE_DISASM) || defined(DEBUG)

#ifdef LATE_DISASM
    getDisAssembler().disAsmCode((BYTE*)*codePtr, (BYTE*)codePtrRW, finalHotCodeSize, (BYTE*)coldCodePtr,
                                 (BYTE*)coldCodePtrRW, finalColdCodeSize);
#endif // LATE_DISASM

#ifdef DEBUG
    if (JitConfig.JitRawHexCode().contains(compiler->info.compMethodHnd, compiler->info.compClassHnd,
                                           &compiler->info.compMethodInfo->args))
    {
        // NOTE: code in cold region is not supported.

        BYTE*  dumpAddr = (BYTE*)codePtrRW;
        size_t dumpSize = finalHotCodeSize;

        const WCHAR* rawHexCodeFilePath = JitConfig.JitRawHexCodeFile();
        if (rawHexCodeFilePath)
        {
            FILE*   hexDmpf;
            errno_t ec = _wfopen_s(&hexDmpf, rawHexCodeFilePath, W("at")); // NOTE: file append mode
            if (ec == 0)
            {
                assert(hexDmpf);
                hexDump(hexDmpf, dumpAddr, dumpSize);
                fclose(hexDmpf);
            }
        }
        else
        {
            FILE* dmpf = jitstdout();

            fprintf(dmpf, "Generated native code for %s:\n", compiler->info.compFullName);
            hexDump(dmpf, dumpAddr, dumpSize);
            fprintf(dmpf, "\n\n");
        }
    }
#endif // DEBUG

    /* Report any exception handlers to the VM */

    genReportEH();

    // Create and store the GC info for this method.
    genCreateAndStoreGCInfo(codeSize, prologSize, epilogSize DEBUGARG(codePtr));

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

    unsigned XTnum;

    bool isNativeAOT = compiler->IsTargetAbi(CORINFO_NATIVEAOT_ABI);

    unsigned EHCount = compiler->compHndBBtabCount;

#if defined(FEATURE_EH_FUNCLETS)
    // Count duplicated clauses. This uses the same logic as below, where we actually generate them for reporting to the
    // VM.
    unsigned duplicateClauseCount = 0;
    unsigned enclosingTryIndex;

    // Duplicate clauses are not used by NativeAOT ABI
    if (!isNativeAOT)
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

    // Duplicate clauses are not used by NativeAOT ABI
    if (!isNativeAOT)
    {
        // We don't keep track of how many cloned finally there are. So, go through and count.
        // We do a quick pass first through the EH table to see if there are any try/finally
        // clauses. If there aren't, we don't need to look for BBJ_CALLFINALLY.

        bool anyFinallys = false;
        for (EHblkDsc* const HBtab : EHClauses(compiler))
        {
            if (HBtab->HasFinallyHandler())
            {
                anyFinallys = true;
                break;
            }
        }
        if (anyFinallys)
        {
            for (BasicBlock* const block : compiler->Blocks())
            {
                if (block->KindIs(BBJ_CALLFINALLY))
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

    for (EHblkDsc* const HBtab : EHClauses(compiler))
    {
        UNATIVE_OFFSET tryBeg, tryEnd, hndBeg, hndEnd, hndTyp;

        tryBeg = compiler->ehCodeOffset(HBtab->ebdTryBeg);
        hndBeg = compiler->ehCodeOffset(HBtab->ebdHndBeg);

        tryEnd = (HBtab->ebdTryLast == compiler->fgLastBB) ? compiler->info.compNativeCodeSize
                                                           : compiler->ehCodeOffset(HBtab->ebdTryLast->Next());
        hndEnd = (HBtab->ebdHndLast == compiler->fgLastBB) ? compiler->info.compNativeCodeSize
                                                           : compiler->ehCodeOffset(HBtab->ebdHndLast->Next());

        if (HBtab->HasFilter())
        {
            hndTyp = compiler->ehCodeOffset(HBtab->ebdFilter);
        }
        else
        {
            hndTyp = HBtab->ebdTyp;
        }

        CORINFO_EH_CLAUSE_FLAGS flags = ToCORINFO_EH_CLAUSE_FLAGS(HBtab->ebdHandlerType);

        if (XTnum > 0)
        {
            // CORINFO_EH_CLAUSE_SAMETRY flag means that the current clause covers same
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
        unsigned  reportedDuplicateClauseCount = 0; // How many duplicated clauses have we reported?
        unsigned  XTnum2;
        EHblkDsc* HBtab;
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
                                                           : compiler->ehCodeOffset(bbTryLast->Next());
                hndEnd = (bbHndLast == compiler->fgLastBB) ? compiler->info.compNativeCodeSize
                                                           : compiler->ehCodeOffset(bbHndLast->Next());

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
        for (BasicBlock* const block : compiler->Blocks())
        {
            if (block->KindIs(BBJ_CALLFINALLY))
            {
                UNATIVE_OFFSET hndBeg, hndEnd;

                hndBeg = compiler->ehCodeOffset(block);

                // How big is it? The BBJ_CALLFINALLYRET has a null bbEmitCookie! Look for the block after, which must
                // be a label or jump target, since the BBJ_CALLFINALLY doesn't fall through.
                BasicBlock* bbLabel = block->Next();
                if (block->isBBCallFinallyPair())
                {
                    bbLabel = bbLabel->Next(); // skip the BBJ_CALLFINALLYRET
                }
                if (bbLabel == nullptr)
                {
                    hndEnd = compiler->info.compNativeCodeSize;
                }
                else
                {
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
//   store - the GT_STOREIND node
//
// Return Value:
//   true if an optimized write barrier helper should be used, false otherwise.
//   Note: only x86 implements register-specific source optimized write
//   barriers currently.
//
bool CodeGenInterface::genUseOptimizedWriteBarriers(GenTreeStoreInd* store)
{
#if defined(TARGET_X86) && NOGC_WRITE_BARRIERS
#ifdef DEBUG
    GCInfo::WriteBarrierForm wbf = compiler->codeGen->gcInfo.gcIsWriteBarrierCandidate(store);
    return (wbf != GCInfo::WBF_NoBarrier_CheckNotHeapInDebug); // This one is always a call to a C++ method.
#else
    return true;
#endif
#else
    return false;
#endif
}

//----------------------------------------------------------------------
// genWriteBarrierHelperForWriteBarrierForm: Given a write barrier form
// return the corresponding helper.
//
// Arguments:
//   wbf - the write barrier form
//
// Return Value:
//   Write barrier helper to use.
//
// Note: do not call this function to get an optimized write barrier helper (e.g.,
// for x86).
//
CorInfoHelpFunc CodeGenInterface::genWriteBarrierHelperForWriteBarrierForm(GCInfo::WriteBarrierForm wbf)
{
    INDEBUG(genWriteBarrierUsed = true);

    switch (wbf)
    {
        case GCInfo::WBF_BarrierChecked:
            return CORINFO_HELP_CHECKED_ASSIGN_REF;

        case GCInfo::WBF_BarrierUnchecked:
            return CORINFO_HELP_ASSIGN_REF;

#ifdef DEBUG
        case GCInfo::WBF_NoBarrier_CheckNotHeapInDebug:
            return CORINFO_HELP_ASSIGN_REF_ENSURE_NONHEAP;
#endif // DEBUG

        default:
            unreached();
    }
}

//----------------------------------------------------------------------
// genGCWriteBarrier: Generate a write barrier for a node.
//
// Arguments:
//   store - the GT_STOREIND node
//   wbf   - already computed write barrier form to use
//
void CodeGen::genGCWriteBarrier(GenTreeStoreInd* store, GCInfo::WriteBarrierForm wbf)
{
    CorInfoHelpFunc helper = genWriteBarrierHelperForWriteBarrierForm(wbf);

#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
    // Under FEATURE_COUNT_GC_WRITE_BARRIERS, we will add an extra argument to the
    // checked write barrier call denoting the kind of address being written to.
    //
    if (helper == CORINFO_HELP_CHECKED_ASSIGN_REF)
    {
        CheckedWriteBarrierKinds wbKind  = CWBKind_Unclassified;
        GenTree*                 tgtAddr = store->Addr();

        while (tgtAddr->OperIs(GT_ADD, GT_LEA))
        {
            if (tgtAddr->OperIs(GT_LEA) && tgtAddr->AsAddrMode()->HasBase())
            {
                tgtAddr = tgtAddr->AsAddrMode()->Base();
            }
            else if (tgtAddr->OperIs(GT_ADD) && tgtAddr->AsOp()->gtGetOp2()->IsCnsIntOrI())
            {
                tgtAddr = tgtAddr->AsOp()->gtGetOp1();
            }
            else
            {
                break;
            }
        }

        if (tgtAddr->OperIs(GT_LCL_VAR))
        {
            unsigned   lclNum = tgtAddr->AsLclVar()->GetLclNum();
            LclVarDsc* varDsc = compiler->lvaGetDesc(lclNum);
            if (lclNum == compiler->info.compRetBuffArg)
            {
                wbKind = CWBKind_RetBuf
            }
            else if (varDsc->TypeGet() == TYP_BYREF)
            {
                wbKind = varDsc->lvIsParam ? CWBKind_ByRefArg : CWBKind_OtherByRefLocal;
            }
        }
        else if (tgtAddr->OperIs(GT_LCL_ADDR))
        {
            // Ideally, we should have eliminated the barrier for this case.
            wbKind = CWBKind_AddrOfLocal;
        }

#if 0
#ifdef DEBUG
        // Enable this to sample the unclassified trees.
        static int unclassifiedBarrierSite = 0;
        if (wbKind == CWBKind_Unclassified)
        {
            unclassifiedBarrierSite++;
            printf("unclassifiedBarrierSite = %d:\n", unclassifiedBarrierSite);
            compiler->gtDispTree(store);
            printf(""); // Flush.
            printf("\n");
        }
#endif // DEBUG
#endif // 0

        AddStackLevel(4);
        inst_IV(INS_push, wbKind);
        genEmitHelperCall(helper,
                          4,           // argSize
                          EA_PTRSIZE); // retSize
        SubtractStackLevel(4);
        return;
    }
#endif // FEATURE_COUNT_GC_WRITE_BARRIERS

    genEmitHelperCall(helper,
                      0,           // argSize
                      EA_PTRSIZE); // retSize
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

#if !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
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
        unsigned  varNum;  // index into compiler->lvaTable[] for this register argument
        var_types type;    // the Jit type of this regArgTab entry
        unsigned  trashBy; // index into this regArgTab[] table of the register that will be copied to this register.
                           // That is, for regArgTab[x].trashBy = y, argument register number 'y' will be copied to
                           // argument register number 'x'. Only used when circular = true.
        char slot;         // 0 means the register is not used for a register argument
                           // 1 means the first part of a register argument
                           // 2, 3 or 4  means the second,third or fourth part of a multireg argument
        bool stackArg;     // true if the argument gets homed to the stack
        bool writeThru;    // true if the argument gets homed to both stack and register
        bool processed;    // true after we've processed the argument (and it is in its final location)
        bool circular;     // true if this register participates in a circular dependency loop.
        bool hfaConflict;  // arg is part of an HFA that will end up in the same register
                           // but in a different slot (eg arg in s3 = v3.s[0], needs to end up in v3.s[3])
    } regArgTab[max(MAX_REG_ARG + 1, MAX_FLOAT_REG_ARG)] = {};

    unsigned   varNum;
    LclVarDsc* varDsc;

    for (varNum = 0; varNum < compiler->lvaCount; ++varNum)
    {
        varDsc = compiler->lvaGetDesc(varNum);

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
        // use the original TYP_STRUCT argument.
        //
        if (varDsc->lvPromoted || varDsc->lvIsStructField)
        {
            LclVarDsc* parentVarDsc = varDsc;
            if (varDsc->lvIsStructField)
            {
                assert(!varDsc->lvPromoted);
                parentVarDsc = compiler->lvaGetDesc(varDsc->lvParentLcl);
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
            if (TargetOS::IsWindows && TargetArchitecture::IsArm64 && compiler->info.compIsVarArgs)
            {
                assert(!"Illegal incoming HFA arg encountered in Vararg method.");
            }
            regType = varDsc->GetHfaType();
        }

#if defined(UNIX_AMD64_ABI)
        if (!varTypeIsStruct(regType))
#endif // defined(UNIX_AMD64_ABI)
        {
            bool isFloatReg = emitter::isFloatReg(varDsc->GetArgReg());

            if (isFloatReg != doingFloat)
            {
                // A struct might be passed  partially in XMM register for System V calls.
                // So a single arg might use both register files.
                continue;
            }
            else if (isFloatReg != varTypeUsesFloatArgReg(regType))
            {
                if (regType == TYP_FLOAT)
                {
                    regType = TYP_INT;
                }
                else
                {
                    assert(regType == TYP_DOUBLE);
                    regType = TYP_LONG;
                }
            }
        }

        int slots = 0;

#if defined(UNIX_AMD64_ABI)
        if (varTypeIsStruct(varDsc))
        {
            CORINFO_CLASS_HANDLE typeHnd;
            if (varDsc->lvIsStructField)
            {
                // The only case we currently permit is a wrapped SIMD field,
                // where we won't have the class handle available, so get it
                // from the parent struct -- they will agree on ABI details.
                LclVarDsc* parentDsc = compiler->lvaGetDesc(varDsc->lvParentLcl);
                assert(varTypeIsSIMD(varDsc) && (parentDsc->lvFieldCnt == 1));
                typeHnd = parentDsc->GetLayout()->GetClassHandle();
            }
            else
            {
                typeHnd = varDsc->GetLayout()->GetClassHandle();
            }
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
                var_types slotRegType;

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
                    slotRegType = TYP_DOUBLE;
                }
                else
#endif
                {
                    slotRegType = compiler->GetEightByteType(structDesc, slotCounter);
                }

                regArgNum = genMapRegNumToRegArgNum(regNum, slotRegType);

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
                    regArgTab[regArgNum].type   = slotRegType;
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
            slots     = 1;

            if (TargetArchitecture::IsArm32 ||
                (TargetOS::IsWindows && TargetArchitecture::IsArm64 && compiler->info.compIsVarArgs))
            {
                int lclSize = compiler->lvaLclSize(varNum);
                if (lclSize > REGSIZE_BYTES)
                {
                    slots = lclSize / REGSIZE_BYTES;
                }
            }
#if FEATURE_MULTIREG_ARGS
            else if (varDsc->lvIsMultiRegArg)
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
            }
#endif // FEATURE_MULTIREG_ARGS

            // Handle args split between registers and stack. The arm64 fixed ret buf arg is never split.
            if (compFeatureArgSplit() && (fixedRetBufIndex != regArgNum))
            {
                unsigned maxRegArgNum = doingFloat ? MAX_FLOAT_REG_ARG : MAX_REG_ARG;
                if (regArgNum + slots > maxRegArgNum)
                {
                    JITDUMP("Splitting V%02u: %u registers, %u stack slots\n", varNum, maxRegArgNum - regArgNum,
                            regArgNum + slots - maxRegArgNum);
                    slots = maxRegArgNum - regArgNum;
                }
            }

            // Note that regArgNum + 1 represents an argument index not an actual argument register;
            // see genMapRegArgNumToRegNum().

            for (int i = 0; i < slots; i++)
            {
                noway_assert((regArgNum + i) < argMax);

                // We better not have added it already (there better not be multiple vars representing this argument
                // register)
                noway_assert(regArgTab[regArgNum + i].slot == 0);

                regArgTab[regArgNum + i].varNum = varNum;
                regArgTab[regArgNum + i].slot   = static_cast<char>(i + 1);

                regArgTab[regArgNum + i].type = regType; // Set the register type.
            }
        }

        for (int i = 0; i < slots; i++)
        {
            regType          = regArgTab[regArgNum + i].type;
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
                                 (varDsc->IsAddressExposed() && compiler->info.compIsVarArgs) ||
                                 (varDsc->IsAddressExposed() && compiler->opts.compUseSoftFP));
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
            regArgTab[regArgNum + i].stackArg = varDsc->lvIsInReg() ? false : true;

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

    bool change         = true;
    bool hasHfaConflict = false;
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

                varNum                     = regArgTab[argNum].varNum;
                varDsc                     = compiler->lvaGetDesc(varNum);
                const var_types varRegType = varDsc->GetRegisterType();
                noway_assert(varDsc->lvIsParam && varDsc->lvIsRegArg);

                /* cannot possibly have stack arguments */
                noway_assert(varDsc->lvIsInReg());
                noway_assert(!regArgTab[argNum].stackArg);

                var_types regType = regArgTab[argNum].type;
                regNumber regNum  = genMapRegArgNumToRegNum(argNum, regType);

                regNumber destRegNum = REG_NA;
                if (varTypeIsPromotable(varDsc) &&
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
                    //
                    assert(varTypeIsSIMD(varDsc));
                    assert(regArgTab[argNum].slot <= (int)varDsc->lvHfaSlots());
                    assert(argNum > 0);
                    assert(regArgTab[argNum - 1].varNum == varNum);

                    // If the field is passed in the same register as the destination,
                    // but is in the wrong part of the register, mark it specially so later
                    // we make sure to move it to the right spot before "freeing" the destination.
                    //
                    destRegNum = varDsc->GetRegNum();
                    if (regNum == destRegNum)
                    {
                        // We only get here if the HFA part is not already in the right slot in
                        // the destination. That is, it is not slot-1.
                        //
                        const int slot = regArgTab[argNum].slot;
                        assert(slot != 1);
                        JITDUMP("HFA conflict; arg num %u needs to move from %s[%u] to %s[%u]\n", argNum,
                                getRegName(regNum), 0, getRegName(destRegNum), slot - 1);
                        regArgTab[argNum].hfaConflict = true;

                        // We'll need to do a special pass later to resolve these
                        //
                        hasHfaConflict = true;
                    }
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
                    assert((varRegType == TYP_SIMD12) || (varRegType == TYP_SIMD16));
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
        varDsc = compiler->lvaGetDesc(varNum);

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
            storeType = genActualType(regArgTab[argNum].type);
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
            varDscDest = compiler->lvaGetDesc(varNumDest);
            noway_assert(varDscDest->lvIsParam && varDscDest->lvIsRegArg);

            noway_assert(srcReg < argMax);
            varNumSrc = regArgTab[srcReg].varNum;
            varDscSrc = compiler->lvaGetDesc(varNumSrc);
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
                varDsc = compiler->lvaGetDesc(varNum);
                noway_assert(varDsc->lvIsParam && varDsc->lvIsRegArg);

                noway_assert(genTypeSize(genActualType(varDscSrc->TypeGet())) <= REGSIZE_BYTES);

                /* Set "size" to indicate GC if one and only one of
                 * the operands is a pointer
                 * RATIONALE: If both are pointers, nothing changes in
                 * the GC pointer tracking. If only one is a pointer we
                 * have to "swap" the registers in the GC reg pointer mask
                 */

                if (varTypeIsGC(varDscSrc) != varTypeIsGC(varDscDest))
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
                    if (compiler->lvaGetDesc(regArgTab[iter].varNum)->TypeGet() != TYP_DOUBLE)
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
                // HVA types...?

                /* move the dest reg (begReg) in the extra reg */

                assert(xtraReg != REG_NA);

                regNumber begRegNum = genMapRegArgNumToRegNum(begReg, destMemType);
                GetEmitter()->emitIns_Mov(insCopy, size, xtraReg, begRegNum, /* canSkip */ false);

                regSet.verifyRegUsed(xtraReg);

                *pXtraRegClobbered = true;
                /* start moving everything to its right place */

                while (srcReg != begReg)
                {
                    /* mov dest, src */

                    regNumber destRegNum = genMapRegArgNumToRegNum(destReg, destMemType);
                    regNumber srcRegNum  = genMapRegArgNumToRegNum(srcReg, destMemType);

                    GetEmitter()->emitIns_Mov(insCopy, size, destRegNum, srcRegNum, /* canSkip */ false);

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
                    varDscSrc = compiler->lvaGetDesc(varNumSrc);
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

                GetEmitter()->emitIns_Mov(insCopy, size, destRegNum, xtraReg, /* canSkip */ false);

                regSet.verifyRegUsed(destRegNum);
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

#if defined(TARGET_ARM64) && defined(FEATURE_SIMD)
    // If we saw any hfa conflicts, handle those now.
    //
    if (hasHfaConflict)
    {
        // Up above we noticed that there was at least one non-slot-1 HFA arg whose
        // destination register was the same as the arg register.
        //
        // For example, say an HFA was passed as s0-s3 and the destination was v3.
        // s3 is in the right register, but not in the right slot in the register.
        //
        // We handle this by first moving the conflicting part to the right slot
        // in the destination (via pass 0 below), and then moving the remaining parts
        // to their respective slots (via pass 1).
        //
        // Note the slot index in the register is one less than value of
        // regArgTab[argNum].slot, so a slot-1 hfa arg goes into slot 0 of the destination).
        //
        // So for the above example, we'd first move the "slot-4" s3 (== v3.s[0]) to v3.s[3].
        // Then we can insert s0 to v3.s[0]) and so on.
        //
        // We can exempt slot-1 cases as the conflicting part is already in the
        // right slot, and code lower down correctly handles populating the remaining slots.
        //
        for (argNum = 0; argNum < argMax; argNum++)
        {
            if (!regArgTab[argNum].hfaConflict)
            {
                continue;
            }

            varNum                      = regArgTab[argNum].varNum;
            varDsc                      = compiler->lvaGetDesc(varNum);
            const regNumber destRegNum  = varDsc->GetRegNum();
            const var_types regType     = regArgTab[argNum].type;
            const unsigned  firstArgNum = argNum - (regArgTab[argNum].slot - 1);
            const unsigned  lastArgNum  = firstArgNum + varDsc->lvHfaSlots() - 1;

            assert(varDsc->lvIsHfa());
            assert((argNum >= firstArgNum) && (argNum <= lastArgNum));
            assert(destRegNum == genMapRegArgNumToRegNum(argNum, regType));

            // Pass 0: move the conflicting part; Pass1: insert everything else
            //
            for (int pass = 0; pass <= 1; pass++)
            {
                for (unsigned currentArgNum = firstArgNum; currentArgNum <= lastArgNum; currentArgNum++)
                {
                    const regNumber regNum = genMapRegArgNumToRegNum(currentArgNum, regType);
                    bool            insertArg =
                        ((pass == 0) && (currentArgNum == argNum)) || ((pass == 1) && (currentArgNum != argNum));

                    if (insertArg)
                    {
                        assert(!regArgTab[currentArgNum].processed);

                        // EA_4BYTE is probably wrong here (and below)
                        // todo -- suppress self move
                        GetEmitter()->emitIns_R_R_I_I(INS_mov, EA_4BYTE, destRegNum, regNum,
                                                      regArgTab[currentArgNum].slot - 1, 0);
                        regArgTab[currentArgNum].processed = true;
                        regArgMaskLive &= ~genRegMask(regNum);
                    }
                }
            }
        }
    }
#endif // defined(TARGET_ARM64) && defined(FEATURE_SIMD)

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

            varNum                     = regArgTab[argNum].varNum;
            varDsc                     = compiler->lvaGetDesc(varNum);
            const var_types regType    = regArgTab[argNum].type;
            const regNumber regNum     = genMapRegArgNumToRegNum(argNum, regType);
            const var_types varRegType = varDsc->GetRegisterType();

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
#ifdef TARGET_X86
            // On x86 we don't enregister args that are not pointer sized.
            noway_assert(genTypeSize(varDsc->GetStackSlotHomeType()) == TARGET_POINTER_SIZE);
#endif // TARGET_X86

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
                assert((varRegType == TYP_SIMD12) || (varRegType == TYP_SIMD16));
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
                    assert((destMask & RBM_ALLDOUBLE) != 0);
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
                inst_Mov(destMemType, destRegNum, regNum, /* canSkip */ false, size);
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
                regNumber nextRegNum = genMapRegArgNumToRegNum(nextArgNum, regArgTab[nextArgNum].type);
                noway_assert(regArgTab[nextArgNum].varNum == varNum);
                // Emit a shufpd with a 0 immediate, which preserves the 0th element of the dest reg
                // and moves the 0th element of the src reg into the 1st element of the dest reg.
                GetEmitter()->emitIns_R_R_I(INS_shufpd, emitActualTypeSize(varRegType), destRegNum, nextRegNum, 0);
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
                            regNumber  nextRegNum  = genMapRegArgNumToRegNum(nextArgNum, regArgTab[nextArgNum].type);
                            destRegNum             = fieldVarDsc->GetRegNum();
                            noway_assert(regArgTab[nextArgNum].varNum == varNum);
                            noway_assert(genIsValidFloatReg(nextRegNum));
                            noway_assert(genIsValidFloatReg(destRegNum));
                            GetEmitter()->emitIns_Mov(INS_mov, EA_8BYTE, destRegNum, nextRegNum, /* canSkip */ false);
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
                            var_types   nextArgType = nextArgElem->type;
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
                regNumber nextRegNum            = genMapRegArgNumToRegNum(nextArgNum, regArgTab[nextArgNum].type);
                regArgMaskLive &= ~genRegMask(nextRegNum);
            }
#endif // FEATURE_MULTIREG_ARGS
        }

        noway_assert(regArgMaskLiveSave != regArgMaskLive); // if it doesn't change, we have an infinite loop
    }
}
#endif // !TARGET_LOONGARCH64 && !TARGET_RISCV64

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

    // OSR handles this specially -- see genEnregisterOSRArgsAndLocals
    //
    assert(!compiler->opts.IsOSR());

    assert(compiler->compGeneratingProlog);

    unsigned varNum = 0;

#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    int       tmp_offset = 0;
    regNumber tmp_reg    = REG_NA;
#endif

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

        var_types regType = varDsc->GetStackSlotHomeType();
#ifdef TARGET_LOONGARCH64
        {
            bool FPbased;
            int  base = compiler->lvaFrameAddress(varNum, &FPbased);

            if (emitter::isValidSimm12(base))
            {
                GetEmitter()->emitIns_R_S(ins_Load(regType), emitTypeSize(regType), regNum, varNum, 0);
            }
            else
            {
                if (tmp_reg == REG_NA)
                {
                    regNumber reg2 = FPbased ? REG_FPBASE : REG_SPBASE;
                    tmp_offset     = base;
                    tmp_reg        = REG_R21;

                    GetEmitter()->emitIns_I_la(EA_PTRSIZE, REG_R21, base);
                    GetEmitter()->emitIns_R_R_R(INS_add_d, EA_PTRSIZE, REG_R21, REG_R21, reg2);
                    GetEmitter()->emitIns_R_S(ins_Load(regType), emitTypeSize(regType), regNum, varNum, -8);
                }
                else
                {
                    int baseOffset = -(base - tmp_offset) - 8;
                    GetEmitter()->emitIns_R_S(ins_Load(regType), emitTypeSize(regType), regNum, varNum, baseOffset);
                }
            }
        }
#else // !TARGET_LOONGARCH64
        GetEmitter()->emitIns_R_S(ins_Load(regType), emitTypeSize(regType), regNum, varNum, 0);
#endif // !TARGET_LOONGARCH64

        regSet.verifyRegUsed(regNum);
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
            varDsc->lvMustInit = 0;
            continue;
        }

        if (compiler->fgVarIsNeverZeroInitializedInProlog(varNum))
        {
            varDsc->lvMustInit = 0;
            continue;
        }

        if (compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc))
        {
            // For Compiler::PROMOTION_TYPE_DEPENDENT type of promotion, the whole struct should have been
            // initialized by the parent struct. No need to set the lvMustInit bit in the
            // field locals.
            varDsc->lvMustInit = 0;
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
                inst_Mov(TYP_FLOAT, reg, fltInitReg, /* canSkip */ false);
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
                    inst_Mov(TYP_FLOAT, reg, initReg, /* canSkip */ false);
                }
#elif defined(TARGET_XARCH)
                // XORPS is the fastest and smallest way to initialize a XMM register to zero.
                GetEmitter()->emitIns_SIMD_R_R_R(INS_xorps, EA_16BYTE, reg, reg, reg);
                dblInitReg = reg;
#elif defined(TARGET_ARM64)
                // We will just zero out the entire vector register. This sets it to a double/float zero value
                GetEmitter()->emitIns_R_I(INS_movi, EA_16BYTE, reg, 0x00, INS_OPTS_16B);
#elif defined(TARGET_LOONGARCH64)
                // We will just zero out the entire vector register. This sets it to a double/float zero value
                GetEmitter()->emitIns_R_R(INS_movgr2fr_d, EA_8BYTE, reg, REG_R0);
#elif defined(TARGET_RISCV64)
                GetEmitter()->emitIns_R_R(INS_fmv_w_x, EA_4BYTE, reg, REG_R0);
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
                inst_Mov(TYP_DOUBLE, reg, dblInitReg, /* canSkip */ false);
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
                GetEmitter()->emitIns_SIMD_R_R_R(INS_xorps, EA_16BYTE, reg, reg, reg);
                fltInitReg = reg;
#elif defined(TARGET_ARM64)
                // We will just zero out the entire vector register. This sets it to a double/float zero value
                GetEmitter()->emitIns_R_I(INS_movi, EA_16BYTE, reg, 0x00, INS_OPTS_16B);
#elif defined(TARGET_LOONGARCH64)
                GetEmitter()->emitIns_R_R(INS_movgr2fr_d, EA_8BYTE, reg, REG_R0);
#elif defined(TARGET_RISCV64)
                GetEmitter()->emitIns_R_R(INS_fmv_d_x, EA_8BYTE, reg, REG_R0);
#else // TARGET*
#error Unsupported or unset target architecture
#endif
                dblInitReg = reg;
            }
        }
    }
}

// We need a register with value zero. Zero the initReg, if necessary, and set *pInitRegZeroed if so.
// Return the register to use. On ARM64, we never touch the initReg, and always just return REG_ZR.
regNumber CodeGen::genGetZeroReg(regNumber initReg, bool* pInitRegZeroed)
{
#ifdef TARGET_ARM64
    return REG_ZR;
#elif defined(TARGET_LOONGARCH64)
    return REG_R0;
#elif defined(TARGET_RISCV64)
    return REG_R0;
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
        genZeroInitFrameUsingBlockInit(untrLclHi, untrLclLo, initReg, pInitRegZeroed);
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
                (varDsc->lvExactSize() >= TARGET_POINTER_SIZE))
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
}

//-----------------------------------------------------------------------------
// genEnregisterOSRArgsAndLocals: Initialize any enregistered args or locals
//   that get values from the tier0 frame.
//
// Arguments:
//    initReg -- scratch register to use if needed
//    pInitRegZeroed -- [IN,OUT] if init reg is zero (on entry/exit)
//
#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
void CodeGen::genEnregisterOSRArgsAndLocals(regNumber initReg, bool* pInitRegZeroed)
#else
void CodeGen::genEnregisterOSRArgsAndLocals()
#endif
{
    assert(compiler->opts.IsOSR());
    PatchpointInfo* const patchpointInfo = compiler->info.compPatchpointInfo;

    // basic sanity checks (make sure we're OSRing the right method)
    assert(patchpointInfo->NumberOfLocals() == compiler->info.compLocalsCount);

    const int      originalFrameSize = patchpointInfo->TotalFrameSize();
    const unsigned patchpointInfoLen = patchpointInfo->NumberOfLocals();

    for (unsigned varNum = 0; varNum < compiler->lvaCount; varNum++)
    {
        if (!compiler->lvaIsOSRLocal(varNum))
        {
            // This local was not part of the tier0 method's state.
            // No work required.
            //
            continue;
        }

        LclVarDsc* const varDsc = compiler->lvaGetDesc(varNum);

        if (!varDsc->lvIsInReg())
        {
            // For args/locals in memory, the OSR frame will continue to access
            // that memory location. No work required.
            //
            JITDUMP("---OSR--- V%02u in memory\n", varNum);
            continue;
        }

        // This local was part of the live tier0 state and is enregistered in the
        // OSR method. Initialize the register from the right frame slot.
        //
        // If we ever enable promotion we'll need to generalize what follows to copy each
        // field from the tier0 frame to its OSR home.
        //
        if (!VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
        {
            // This arg or local is not live at entry to the OSR method.
            // No work required.
            //
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

        // Note we are always reading from the tier0 frame here
        //
        const var_types lclTyp  = varDsc->GetStackSlotHomeType();
        const emitAttr  size    = emitActualTypeSize(lclTyp);
        const int       stkOffs = patchpointInfo->Offset(lclNum) + fieldOffset;

#if defined(TARGET_AMD64)

        // Original frames always use frame pointers, so
        // stkOffs is the tier0 frame's frame-relative offset
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
        //
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

        JITDUMP("---OSR--- V%02u (reg) old rbp offset %d old frame %d this frame sp-fp %d new offset %d (0x%02x)\n",
                varNum, stkOffs, originalFrameSize, genSPtoFPdelta(), offset, offset);

        GetEmitter()->emitIns_R_AR(ins_Load(lclTyp), size, varDsc->GetRegNum(), genFramePointerReg(), offset);

#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

        // Patchpoint offset is from top of Tier0 frame
        //
        // We need to determine the frame-pointer relative
        // offset for this variable in the osr frame.
        //
        // First add the Tier0 frame size
        //
        const int tier0FrameSize = compiler->info.compPatchpointInfo->TotalFrameSize();

        // then add the OSR frame size
        //
        const int osrFrameSize = genTotalFrameSize();

        // then subtract OSR SP-FP delta
        //
        const int osrSpToFpDelta = genSPtoFPdelta();

        //               | => tier0 top of frame relative
        //               |         + => tier0 bottom of frame relative
        //               |         |                + => osr bottom of frame (sp) relative
        //               |         |                |              - => osr fp relative
        //               |         |                |              |
        const int offset = stkOffs + tier0FrameSize + osrFrameSize - osrSpToFpDelta;

        JITDUMP("---OSR--- V%02u (reg) Tier0 virtual offset %d OSR frame size %d OSR sp-fp "
                "delta %d total offset %d (0x%x)\n",
                varNum, stkOffs, osrFrameSize, osrSpToFpDelta, offset, offset);

        genInstrWithConstant(ins_Load(lclTyp), size, varDsc->GetRegNum(), genFramePointerReg(), offset, initReg);
        *pInitRegZeroed = false;
#endif // TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64
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
    assert(compiler->compGeneratingProlog);

    const bool reportArg = compiler->lvaReportParamTypeArg();

    if (compiler->opts.IsOSR())
    {
        PatchpointInfo* const ppInfo = compiler->info.compPatchpointInfo;
        if (reportArg)
        {
            // OSR method will use Tier0 slot to report context arg.
            //
            assert(ppInfo->HasGenericContextArgOffset());
            JITDUMP("OSR method will use Tier0 frame slot for generics context arg.\n");
        }
        else if (compiler->lvaKeepAliveAndReportThis())
        {
            // OSR method will use Tier0 slot to report `this` as context.
            //
            assert(ppInfo->HasKeptAliveThis());
            JITDUMP("OSR method will use Tier0 frame slot for generics context `this`.\n");
        }

        return;
    }

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
    LclVarDsc* varDsc = compiler->lvaGetDesc(contextArg);

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
#elif defined(TARGET_LOONGARCH64)
    genInstrWithConstant(ins_Store(TYP_I_IMPL), EA_PTRSIZE, reg, genFramePointerReg(),
                         compiler->lvaCachedGenericContextArgOffset(), REG_R21);
#elif defined(TARGET_RISCV64)
    genInstrWithConstant(ins_Store(TYP_I_IMPL), EA_PTRSIZE, reg, genFramePointerReg(),
                         compiler->lvaCachedGenericContextArgOffset(), rsGetRsvdReg());
#else  // !ARM64 !ARM !LOONGARCH64 !RISCV64
    // mov [ebp-lvaCachedGenericContextArgOffset()], reg
    GetEmitter()->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, reg, genFramePointerReg(),
                               compiler->lvaCachedGenericContextArgOffset());
#endif // !ARM64 !ARM !LOONGARCH64 !RISCV64
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

    bool jmpEpilog = block->HasFlag(BBF_HAS_JMP);

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
    GetEmitter()->emitCreatePlaceholderIG(IGPT_EPILOG, block, gcrefVarsArg, gcrefRegsArg, byrefRegsArg,
                                          block->IsLast());
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

    GetEmitter()->emitCreatePlaceholderIG(IGPT_FUNCLET_EPILOG, block, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                          gcInfo.gcRegByrefSetCur, block->IsLast());
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
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
        regMaskTP okRegs = (RBM_CALLEE_TRASH | RBM_FPBASE | RBM_ENC_CALLEE_SAVED);
        if (RBM_ENC_CALLEE_SAVED != 0)
        {
            regSet.rsSetRegsModified(RBM_ENC_CALLEE_SAVED);
        }
        noway_assert((regSet.rsGetModifiedRegsMask() & ~okRegs) == 0);
#else  // !TARGET_AMD64 && !TARGET_ARM64
        // On x86 we save all callee saved regs so the saved reg area size is consistent
        regSet.rsSetRegsModified(RBM_INT_CALLEE_SAVED & ~RBM_FPBASE);
#endif // !TARGET_AMD64 && !TARGET_ARM64
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
        regMaskTP contiguousMask = genRegMaskFloat(REG_F16);
        while (maskPushRegsFloat > contiguousMask)
        {
            contiguousMask <<= 2;
            contiguousMask |= genRegMaskFloat(REG_F16);
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
    // On Amd64 we push only integer regs. Callee saved float (xmm6-xmm31)
    // regs are stack allocated and preserved in their stack locations.
    compiler->compCalleeFPRegsSavedMask = maskCalleeRegsPushed & RBM_FLT_CALLEE_SAVED;
    maskCalleeRegsPushed &= ~RBM_FLT_CALLEE_SAVED;
#endif // defined(TARGET_XARCH)

#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
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

    // we always push RA.  See genPushCalleeSavedRegisters
    maskCalleeRegsPushed |= RBM_RA;
#endif // TARGET_LOONGARCH64 || TARGET_RISCV64

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

#ifdef DEBUG
    if (compiler->opts.dspCode || compiler->opts.disAsm || compiler->opts.disAsm2 || verbose)
    {
        compiler->lvaTableDump();
    }
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
    genIPmappingAddToFront(IPmappingDscKind::Prolog, DebugInfo(), true);

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

#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    // For arm64 OSR, emit a "phantom prolog" to account for the actions taken
    // in the tier0 frame that impact FP and SP on entry to the OSR method.
    //
    // x64 handles this differently; the phantom prolog unwind is emitted in
    // genOSRRecordTier0CalleeSavedRegistersAndFrame.
    //
    if (compiler->opts.IsOSR())
    {
        PatchpointInfo* patchpointInfo = compiler->info.compPatchpointInfo;
        const int       tier0FrameSize = patchpointInfo->TotalFrameSize();

        // SP is tier0 method's SP.
        compiler->unwindAllocStack(tier0FrameSize);
    }
#endif // defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

#ifdef DEBUG

    if (compiler->compJitHaltMethod())
    {
        /* put a nop first because the debugger and other tools are likely to
           put an int3 at the beginning and we don't want to confuse them */

        instGen(INS_nop);
        instGen(INS_BREAKPOINT);

#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        // Avoid asserts in the unwind info because these instructions aren't accounted for.
        compiler->unwindPadding();
#endif // TARGET_ARMARCH || TARGET_LOONGARCH64 || TARGET_RISCV64
    }
#endif // DEBUG

#if defined(FEATURE_EH_FUNCLETS) && defined(DEBUG)

    // We cannot force 0-initialization of the PSPSym
    // as it will overwrite the real value
    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        const LclVarDsc* varDsc = compiler->lvaGetDesc(compiler->lvaPSPSym);
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
            regNumber regForVar = varDsc->GetRegNum();
            regMaskTP regMask   = genRegMask(regForVar);
            if (!genIsValidFloatReg(regForVar))
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

        assert(!compiler->opts.ShouldUsePInvokeHelpers() || (compiler->info.compLvFrameListRoot == BAD_VAR_NUM));
        if (!compiler->opts.ShouldUsePInvokeHelpers())
        {
            excludeMask |= (RBM_PINVOKE_TCB | RBM_PINVOKE_SCRATCH);

            // We also must exclude the register used by compLvFrameListRoot when it is enregistered
            //
            const LclVarDsc* varDsc = compiler->lvaGetDesc(compiler->info.compLvFrameListRoot);
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

    const bool isRoot = (compiler->funCurrentFunc()->funKind == FuncKind::FUNC_ROOT);

#ifdef TARGET_AMD64
    const bool isOSRx64Root = isRoot && compiler->opts.IsOSR();
#else
    const bool isOSRx64Root = false;
#endif // TARGET_AMD64

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

#if defined(TARGET_AMD64)
    // For x64 OSR root frames, we can't use any as of yet unsaved
    // callee save as initReg, as we defer saving these until later in
    // the prolog, and we don't have normal arg regs.
    if (isOSRx64Root)
    {
        initReg = REG_SCRATCH; // REG_EAX
    }
#elif defined(TARGET_ARM64)
    // For arm64 OSR root frames, we may need a scratch register for large
    // offset addresses. Use a register that won't be allocated.
    //
    if (isRoot && compiler->opts.IsOSR())
    {
        initReg = REG_IP1;
    }
#elif defined(TARGET_LOONGARCH64)
    // For LoongArch64 OSR root frames, we may need a scratch register for large
    // offset addresses. Use a register that won't be allocated.
    if (isRoot && compiler->opts.IsOSR())
    {
        initReg = REG_SCRATCH;
    }
#elif defined(TARGET_RISCV64)
    // For RISC-V64 OSR root frames, we may need a scratch register for large
    // offset addresses. Use a register that won't be allocated.
    if (isRoot && compiler->opts.IsOSR())
    {
        initReg = REG_SCRATCH; // REG_T0
    }
#endif

#if !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
    // For LoongArch64's OSR root frames, we may need a scratch register for large
    // offset addresses. But this does not conflict with the REG_PINVOKE_FRAME.
    //
    // RISC-V64's OSR root frames are similar to LoongArch64's. In this case
    // REG_SCRATCH also shouldn't conflict with REG_PINVOKE_FRAME, even if
    // technically they are the same register - REG_T0.
    //
    noway_assert(!compiler->compMethodRequiresPInvokeFrame() || (initReg != REG_PINVOKE_FRAME));
#endif // !TARGET_LOONGARCH64 && !TARGET_RISCV64

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
    // OSR methods can skip this, as the setup is done by the original method.
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

    unsigned extraFrameSize = 0;

#ifdef TARGET_XARCH

#ifdef TARGET_AMD64
    if (isOSRx64Root)
    {
        // Account for the Tier0 callee saves
        //
        genOSRRecordTier0CalleeSavedRegistersAndFrame();

        // We don't actually push any callee saves on the OSR frame,
        // but we still reserve space, so account for this when
        // allocating the local frame.
        //
        extraFrameSize = compiler->compCalleeRegsPushed * REGSIZE_BYTES;
    }
#endif // TARGET_AMD64

    if (doubleAlignOrFramePointerUsed())
    {
        // OSR methods handle "saving" FP specially.
        //
        // For epilog and unwind, we restore the RBP saved by the
        // Tier0 method. The save we do here is just to set up a
        // proper RBP-based frame chain link.
        //
        if (isOSRx64Root && isFramePointerUsed())
        {
            GetEmitter()->emitIns_R_AR(INS_mov, EA_8BYTE, initReg, REG_FPBASE, 0);
            inst_RV(INS_push, initReg, TYP_REF);
            initRegZeroed = false;

            // We account for the SP movement in unwind, but not for
            // the "save" of RBP.
            //
            compiler->unwindAllocStack(REGSIZE_BYTES);
        }
        else
        {
            inst_RV(INS_push, REG_FPBASE, TYP_REF);
            compiler->unwindPush(REG_FPBASE);
        }
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

#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    genPushCalleeSavedRegisters(initReg, &initRegZeroed);

#else  // !TARGET_ARM64 && !TARGET_LOONGARCH64 && !TARGET_RISCV64

    if (!isOSRx64Root)
    {
        genPushCalleeSavedRegisters();
    }
#endif // !TARGET_ARM64 && !TARGET_LOONGARCH64 && !TARGET_RISCV64

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

#if !defined(TARGET_ARM64) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
    regMaskTP maskStackAlloc = RBM_NONE;

#ifdef TARGET_ARM
    maskStackAlloc = genStackAllocRegisterMask(compiler->compLclFrameSize + extraFrameSize,
                                               regSet.rsGetModifiedRegsMask() & RBM_FLT_CALLEE_SAVED);
#endif // TARGET_ARM

    if (maskStackAlloc == RBM_NONE)
    {
        genAllocLclFrame(compiler->compLclFrameSize + extraFrameSize, initReg, &initRegZeroed,
                         intRegState.rsCalleeRegArgMaskLiveIn);
    }
#endif // !TARGET_ARM64 && !TARGET_LOONGARCH64 && !TARGET_RISCV64

#ifdef TARGET_AMD64
    // For x64 OSR we have to finish saving int callee saves.
    //
    if (isOSRx64Root)
    {
        genOSRSaveRemainingCalleeSavedRegisters();
    }
#endif // TARGET_AMD64

//-------------------------------------------------------------------------

#ifdef TARGET_ARM
    if (compiler->compLocallocUsed)
    {
        GetEmitter()->emitIns_Mov(INS_mov, EA_4BYTE, REG_SAVED_LOCALLOC_SP, REG_SPBASE, /* canSkip */ false);
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
        const bool reportUnwindData = compiler->compLocallocUsed || compiler->opts.compDbgEnC;
        genEstablishFramePointer(compiler->codeGen->genSPtoFPdelta(), reportUnwindData);
    }
#endif // TARGET_AMD64
    compiler->unwindEndProlog();

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
        GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SECRET_STUB_PARAM,
                                  compiler->lvaStubArgumentVar, 0);
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

    // For OSR we may have a zero-length prolog. That's not supported
    // when the method must report a generics context,/ so add a nop if so.
    //
    if (compiler->opts.IsOSR() && (GetEmitter()->emitGetPrologOffsetEstimate() == 0) &&
        (compiler->lvaReportParamTypeArg() || compiler->lvaKeepAliveAndReportThis()))
    {
        JITDUMP("OSR: prolog was zero length and has generic context to report: adding nop to pad prolog.\n");
        instGen(INS_nop);
    }

    if (!GetInterruptible())
    {
        // The 'real' prolog ends here for non-interruptible methods.
        // For fully-interruptible methods, we extend the prolog so that
        // we do not need to track GC information while shuffling the
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

    // Home incoming arguments and generate any required inits.
    // OSR handles this by moving the values from the original frame.
    //
    // Update the arg initial register locations.
    //
    if (compiler->opts.IsOSR())
    {
        // For OSR  we defer updating "initial reg" for args until
        // we've set the live-in regs with values from the Tier0 frame.
        //
        // Otherwise we'll do some of these fetches twice.
        //
        CLANG_FORMAT_COMMENT_ANCHOR;
#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        genEnregisterOSRArgsAndLocals(initReg, &initRegZeroed);
#else
        genEnregisterOSRArgsAndLocals();
#endif
        compiler->lvaUpdateArgsWithInitialReg();
    }
    else
    {
        compiler->lvaUpdateArgsWithInitialReg();

#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        if (intRegState.rsCalleeRegArgMaskLiveIn || floatRegState.rsCalleeRegArgMaskLiveIn)
        {
            initRegZeroed = false;
            genFnPrologCalleeRegArgs();
        }
#else
        auto assignIncomingRegisterArgs = [this, initReg, &initRegZeroed](RegState* regState) {
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
        };

#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_ARM)
        // Handle float parameters first; in the presence of struct promotion
        // we can have parameters that are homed into float registers but
        // passed in integer registers. So make sure we get those out of the
        // integer registers before we potentially override those as part of
        // handling integer parameters.

        assignIncomingRegisterArgs(&floatRegState);
        assignIncomingRegisterArgs(&intRegState);
#else
        assignIncomingRegisterArgs(&intRegState);
#endif // TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64

#endif // TARGET_LOONGARCH64 || TARGET_RISCV64

        // Home the incoming arguments.
        genEnregisterIncomingStackArgs();
    }

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

    if (compiler->info.compIsVarArgs && compiler->lvaGetDesc(argsStartVar)->lvRefCnt() > 0)
    {
        varDsc = compiler->lvaGetDesc(argsStartVar);

        noway_assert(compiler->info.compArgsCount > 0);

        // MOV EAX, <VARARGS HANDLE>
        GetEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_EAX, compiler->info.compArgsCount - 1, 0);
        regSet.verifyRegUsed(REG_EAX);

        // MOV EAX, [EAX]
        GetEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_EAX, REG_EAX, 0);

        // EDX might actually be holding something here.  So make sure to only use EAX for this code
        // sequence.

        const LclVarDsc* lastArg = compiler->lvaGetDesc(compiler->info.compArgsCount - 1);
        noway_assert(!lastArg->lvRegister);
        signed offset = lastArg->GetStackOffset();
        assert(offset != BAD_STK_OFFS);
        noway_assert(lastArg->lvFramePointerBased);

        // LEA EAX, &<VARARGS HANDLE> + EAX
        GetEmitter()->emitIns_R_ARR(INS_lea, EA_PTRSIZE, REG_EAX, genFramePointerReg(), REG_EAX, offset);

        if (varDsc->lvIsInReg())
        {
            GetEmitter()->emitIns_Mov(INS_mov, EA_PTRSIZE, varDsc->GetRegNum(), REG_EAX, /* canSkip */ true);
            regSet.verifyRegUsed(varDsc->GetRegNum());
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
        assert(compiler->lvaReturnSpCheck != BAD_VAR_NUM);
        assert(compiler->lvaGetDesc(compiler->lvaReturnSpCheck)->lvDoNotEnregister);
        assert(compiler->lvaGetDesc(compiler->lvaReturnSpCheck)->lvOnFrame);
        GetEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaReturnSpCheck, 0);
    }
#endif // defined(DEBUG) && defined(TARGET_XARCH)

    GetEmitter()->emitEndProlog();
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//------------------------------------------------------------------------
// getCallTarget - Get the node that evaluates to the call target
//
// Arguments:
//    call - the GT_CALL node
//
// Returns:
//   The node. Note that for direct calls this may still return non-null if the direct call
//   requires a 'complex' tree to load the target (e.g. in R2R or because we go through a stub).
//
GenTree* CodeGen::getCallTarget(const GenTreeCall* call, CORINFO_METHOD_HANDLE* methHnd)
{
    // all virtuals should have been expanded into a control expression by this point.
    assert(!call->IsVirtual() || call->gtControlExpr || call->gtCallAddr);

    if (call->gtCallType == CT_INDIRECT)
    {
        assert(call->gtControlExpr == nullptr);

        if (methHnd != nullptr)
        {
            *methHnd = nullptr;
        }

        return call->gtCallAddr;
    }

    if (methHnd != nullptr)
    {
        *methHnd = call->gtCallMethHnd;
    }

    return call->gtControlExpr;
}

//------------------------------------------------------------------------
// getCallIndirectionCellReg - Get the register containing the indirection cell for a call
//
// Arguments:
//    call - the node
//
// Returns:
//   The register containing the indirection cell, or REG_NA if this call does not use an indirection cell argument.
//
// Notes:
//   We currently use indirection cells for VSD on all platforms and for R2R calls on ARM architectures.
//
regNumber CodeGen::getCallIndirectionCellReg(GenTreeCall* call)
{
    regNumber result = REG_NA;
    switch (call->GetIndirectionCellArgKind())
    {
        case WellKnownArg::None:
            break;
        case WellKnownArg::R2RIndirectionCell:
            result = REG_R2R_INDIRECT_PARAM;
            break;
        case WellKnownArg::VirtualStubCell:
            result = compiler->virtualStubParamInfo->GetReg();
            break;
        default:
            unreached();
    }

#ifdef DEBUG
    if (call->GetIndirectionCellArgKind() != WellKnownArg::None)
    {
        CallArg* indirCellArg = call->gtArgs.FindWellKnownArg(call->GetIndirectionCellArgKind());
        assert((indirCellArg != nullptr) && (indirCellArg->AbiInfo.GetRegNum() == result));
    }
#endif

    return result;
}

//------------------------------------------------------------------------
// genDefinePendingLabel - If necessary, define the pending call label after a
// call instruction was emitted.
//
// Arguments:
//    call - the call node
//
void CodeGen::genDefinePendingCallLabel(GenTreeCall* call)
{
    // for pinvoke/intrinsic/tailcalls we may have needed to get the address of
    // a label.
    if (!genPendingCallLabel)
    {
        return;
    }

    // For certain indirect calls we may introduce helper calls before that we need to skip:
    // - CFG may introduce a call to the validator first
    // - Generic virtual methods may compute the target dynamically through a separate helper call
    if (call->IsHelperCall(compiler, CORINFO_HELP_VALIDATE_INDIRECT_CALL) ||
        call->IsHelperCall(compiler, CORINFO_HELP_VIRTUAL_FUNC_PTR))
    {
        return;
    }

    genDefineInlineTempLabel(genPendingCallLabel);
    genPendingCallLabel = nullptr;
}

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
        GetEmitter()->emitDispIGlist(/* displayInstructions */ false);
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
        GetEmitter()->emitDispIGlist(/* displayInstructions */ false);
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

#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
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
    // (i.e. uses twice the number of single precision registers)
    return info.compCompHnd->getClassSize(hClass) / REGSIZE_BYTES;
#else  // TARGET_ARM64
    var_types hfaType   = GetHfaType(hClass);
    unsigned  classSize = info.compCompHnd->getClassSize(hClass);
    // Note that the retail build issues a warning about a potential division by zero without the Max function
    unsigned elemSize = Max((unsigned)1, EA_SIZE_IN_BYTES(emitActualTypeSize(hfaType)));
    return classSize / elemSize;
#endif // TARGET_ARM64
}

//------------------------------------------------------------------------------------------------ //
// getFirstArgWithStackSlot - returns the first argument with stack slot on the caller's frame.
//
// Return value:
//    The number of the first argument with stack slot on the caller's frame.
//
// Note:
//    On x64 Windows the caller always creates slots (homing space) in its frame for the
//    first 4 arguments of a callee (register passed args). So, the variable number
//    (lclNum) for the first argument with a stack slot is always 0.
//    For System V systems or armarch, there is no such calling convention requirement, and the code
//    needs to find the first stack passed argument from the caller. This is done by iterating over
//    all the lvParam variables and finding the first with GetArgReg() equals to REG_STK.
//
unsigned CodeGen::getFirstArgWithStackSlot()
{
#if defined(UNIX_AMD64_ABI) || defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    unsigned baseVarNum = 0;
    // Iterate over all the lvParam variables in the Lcl var table until we find the first one
    // that's passed on the stack.
    LclVarDsc* varDsc = nullptr;
    for (unsigned i = 0; i < compiler->info.compArgsCount; i++)
    {
        varDsc = compiler->lvaGetDesc(i);

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

#ifdef DEBUG

/*****************************************************************************
 *  Display a IPmappingDsc. Pass -1 as mappingNum to not display a mapping number.
 */

void CodeGen::genIPmappingDisp(unsigned mappingNum, const IPmappingDsc* ipMapping)
{
    if (mappingNum != unsigned(-1))
    {
        printf("%d: ", mappingNum);
    }

    switch (ipMapping->ipmdKind)
    {
        case IPmappingDscKind::Prolog:
            printf("PROLOG");
            break;
        case IPmappingDscKind::Epilog:
            printf("EPILOG");
            break;
        case IPmappingDscKind::NoMapping:
            printf("NO_MAP");
            break;
        case IPmappingDscKind::Normal:
            const ILLocation& loc = ipMapping->ipmdLoc;
            Compiler::eeDispILOffs(loc.GetOffset());
            if (loc.IsStackEmpty())
            {
                printf(" STACK_EMPTY");
            }

            if (loc.IsCall())
            {
                printf(" CALL_INSTRUCTION");
            }

            break;
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
    unsigned mappingNum = 0;

    for (IPmappingDsc& dsc : compiler->genIPmappings)
    {
        genIPmappingDisp(mappingNum, &dsc);
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

void CodeGen::genIPmappingAdd(IPmappingDscKind kind, const DebugInfo& di, bool isLabel)
{
    if (!compiler->opts.compDbgInfo)
    {
        return;
    }

    assert((kind == IPmappingDscKind::Normal) == di.IsValid());

    switch (kind)
    {
        case IPmappingDscKind::Prolog:
        case IPmappingDscKind::Epilog:
            break;

        default:

            if (kind == IPmappingDscKind::Normal)
            {
                noway_assert(di.GetLocation().GetOffset() <= compiler->info.compILCodeSize);
            }

            // Ignore this one if it's the same IL location as the last one we saw.
            // Note that we'll let through two identical IL offsets if the flag bits
            // differ, or two identical "special" mappings (e.g., PROLOG).
            if ((compiler->genIPmappings.size() > 0) && (kind == compiler->genIPmappings.back().ipmdKind) &&
                (di.GetLocation() == compiler->genIPmappings.back().ipmdLoc))
            {
                JITDUMP("genIPmappingAdd: ignoring duplicate IL offset 0x%x\n", di.GetLocation().GetOffset());
                return;
            }
            break;
    }

    IPmappingDsc addMapping;
    addMapping.ipmdNativeLoc.CaptureLocation(GetEmitter());
    addMapping.ipmdKind    = kind;
    addMapping.ipmdLoc     = di.GetLocation();
    addMapping.ipmdIsLabel = isLabel;

    assert((kind == IPmappingDscKind::Normal) == addMapping.ipmdLoc.IsValid());
    compiler->genIPmappings.push_back(addMapping);

#ifdef DEBUG
    if (verbose)
    {
        printf("Added IP mapping: ");
        genIPmappingDisp(unsigned(-1), &addMapping);
    }
#endif // DEBUG
}

/*****************************************************************************
 *
 *  Prepend an IPmappingDsc struct to the list that we're maintaining
 *  for the debugger.
 */
void CodeGen::genIPmappingAddToFront(IPmappingDscKind kind, const DebugInfo& di, bool isLabel)
{
    if (!compiler->opts.compDbgInfo)
    {
        return;
    }

    noway_assert((kind != IPmappingDscKind::Normal) ||
                 (di.IsValid() && (di.GetLocation().GetOffset() <= compiler->info.compILCodeSize)));

    /* Create a mapping entry and prepend it to the list */

    IPmappingDsc addMapping;
    addMapping.ipmdNativeLoc.CaptureLocation(GetEmitter());
    addMapping.ipmdKind    = kind;
    addMapping.ipmdLoc     = di.GetLocation();
    addMapping.ipmdIsLabel = isLabel;
    compiler->genIPmappings.push_front(addMapping);

#ifdef DEBUG
    if (verbose)
    {
        printf("Added IP mapping to front: ");
        genIPmappingDisp(unsigned(-1), &addMapping);
    }
#endif // DEBUG
}

/*****************************************************************************/

void CodeGen::genEnsureCodeEmitted(const DebugInfo& di)
{
    if (!compiler->opts.compDbgCode)
    {
        return;
    }

    if (!di.IsValid())
    {
        return;
    }

    // If other IL were offsets reported, skip

    if (compiler->genIPmappings.size() <= 0)
    {
        return;
    }

    const IPmappingDsc& prev = compiler->genIPmappings.back();
    if (prev.ipmdLoc != di.GetLocation())
    {
        return;
    }

    // di represents the last reported offset. Make sure that we generated native code

    if (prev.ipmdNativeLoc.IsCurrentLocation(GetEmitter()))
    {
        instGen(INS_nop);
    }
}

//------------------------------------------------------------------------
// genIPmappingGen: Shut down the IP-mapping logic, report the info to the EE.
//
void CodeGen::genIPmappingGen()
{
    if (!compiler->opts.compDbgInfo)
    {
        return;
    }

    JITDUMP("*************** In genIPmappingGen()\n");

    if (compiler->genIPmappings.size() <= 0)
    {
        compiler->eeSetLIcount(0);
        compiler->eeSetLIdone();
        return;
    }

    UNATIVE_OFFSET prevNativeOfs = UNATIVE_OFFSET(~0);
    for (jitstd::list<IPmappingDsc>::iterator it = compiler->genIPmappings.begin();
         it != compiler->genIPmappings.end();)
    {
        UNATIVE_OFFSET dscNativeOfs = it->ipmdNativeLoc.CodeOffset(GetEmitter());
        if (dscNativeOfs != prevNativeOfs)
        {
            prevNativeOfs = dscNativeOfs;
            ++it;
            continue;
        }

        // If we have a previous offset we should have a previous mapping.
        assert(it != compiler->genIPmappings.begin());
        jitstd::list<IPmappingDsc>::iterator prev = it;
        --prev;

        // Prev and current mappings have same native offset.
        // If one does not map to IL then remove that one.
        if (prev->ipmdKind == IPmappingDscKind::NoMapping)
        {
            compiler->genIPmappings.erase(prev);
            ++it;
            continue;
        }

        if (it->ipmdKind == IPmappingDscKind::NoMapping)
        {
            it = compiler->genIPmappings.erase(it);
            continue;
        }

        // Both have mappings.
        // If previous is the prolog, keep both if this one is at IL offset 0.
        // (TODO: Why? Debugger has no problem breaking on the prolog mapping
        // it seems.)
        if ((prev->ipmdKind == IPmappingDscKind::Prolog) && (it->ipmdKind == IPmappingDscKind::Normal) &&
            (it->ipmdLoc.GetOffset() == 0))
        {
            ++it;
            continue;
        }

        // For the special case of an IL instruction with no body followed by
        // the epilog (say ret void immediately preceding the method end), we
        // leave both entries in, so that we'll stop at the (empty) ret
        // statement if the user tries to put a breakpoint there, and then have
        // the option of seeing the epilog or not based on SetUnmappedStopMask
        // for the stepper.
        if (it->ipmdKind == IPmappingDscKind::Epilog)
        {
            ++it;
            continue;
        }

        // For managed return values we store all calls. Keep both in this case
        // too.
        if (((prev->ipmdKind == IPmappingDscKind::Normal) && (prev->ipmdLoc.IsCall())) ||
            ((it->ipmdKind == IPmappingDscKind::Normal) && (it->ipmdLoc.IsCall())))
        {
            ++it;
            continue;
        }

        // Otherwise report the higher offset unless the previous mapping is a
        // label.
        if (prev->ipmdIsLabel)
        {
            it = compiler->genIPmappings.erase(it);
        }
        else
        {
            compiler->genIPmappings.erase(prev);
            ++it;
        }
    }

    // Tell them how many mapping records we've got

    compiler->eeSetLIcount(static_cast<unsigned int>(compiler->genIPmappings.size()));

    // Now tell them about the mappings
    unsigned int mappingIdx = 0;
    for (const IPmappingDsc& dsc : compiler->genIPmappings)
    {
        compiler->eeSetLIinfo(mappingIdx++, dsc.ipmdNativeLoc.CodeOffset(GetEmitter()), dsc.ipmdKind, dsc.ipmdLoc);
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
        for (BasicBlock* const block : compiler->Blocks())
        {
            Statement* stmt = block->firstStmt();
            if ((block->bbRefs > 1) && (stmt != nullptr))
            {
                bool found = false;
                DebugInfo rootInfo = stmt->GetDebugInfo().GetRoot();
                if (rootInfo.IsValid())
                {
                    for (unsigned i = 0; i < compiler->eeBoundariesCount; ++i)
                    {
                        if (compiler->eeBoundaries[i].ilOffset == rootInfo.GetLocation().GetOffset())
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

#ifdef DEBUG
//------------------------------------------------------------------------
// genReportRichDebugInfoInlineTreeToFile:
//   Recursively process a context in the inline tree and write information about it to a file.
//
// Parameters:
//   file - the file
//   context - the context
//   first - whether this is the first of the siblings being written out
//
void CodeGen::genReportRichDebugInfoInlineTreeToFile(FILE* file, InlineContext* context, bool* first)
{
    if (context->GetSibling() != nullptr)
    {
        genReportRichDebugInfoInlineTreeToFile(file, context->GetSibling(), first);
    }

    if (context->IsSuccess())
    {
        if (!*first)
        {
            fprintf(file, ",");
        }

        *first = false;

        fprintf(file, "{\"Ordinal\":%u,", context->GetOrdinal());
        fprintf(file, "\"MethodID\":%lld,", (int64_t)context->GetCallee());
        fprintf(file, "\"ILOffset\":%u,", context->GetLocation().GetOffset());
        fprintf(file, "\"LocationFlags\":%u,", (uint32_t)context->GetLocation().EncodeSourceTypes());
        fprintf(file, "\"ExactILOffset\":%u,", context->GetActualCallOffset());
        auto append = [&]() {
            char        buffer[256];
            const char* methodName = compiler->eeGetMethodName(context->GetCallee(), buffer, sizeof(buffer));
            fprintf(file, "\"MethodName\":\"%s\",", methodName);
        };
        append();
        fprintf(file, "\"Inlinees\":[");
        if (context->GetChild() != nullptr)
        {
            bool childFirst = true;
            genReportRichDebugInfoInlineTreeToFile(file, context->GetChild(), &childFirst);
        }
        fprintf(file, "]}");
    }
}

//------------------------------------------------------------------------
// genReportRichDebugInfoToFile:
//   Write rich debug info in JSON format to file specified by environment variable.
//
void CodeGen::genReportRichDebugInfoToFile()
{
    if (JitConfig.WriteRichDebugInfoFile() == nullptr)
    {
        return;
    }

    static CritSecObject s_critSect;
    CritSecHolder        holder(s_critSect);

    FILE* file = _wfopen(JitConfig.WriteRichDebugInfoFile(), W("a"));
    if (file == nullptr)
    {
        return;
    }

    // MethodID in ETW events are the method handles.
    fprintf(file, "{\"MethodID\":%lld,", (INT64)compiler->info.compMethodHnd);
    // Print inline tree.
    fprintf(file, "\"InlineTree\":");

    bool first = true;
    genReportRichDebugInfoInlineTreeToFile(file, compiler->compInlineContext, &first);
    fprintf(file, ",\"Mappings\":[");
    first = true;
    for (RichIPMapping& mapping : compiler->genRichIPmappings)
    {
        if (!first)
        {
            fprintf(file, ",");
        }

        first = false;

        fprintf(file, "{\"NativeOffset\":%u,\"InlineContext\":%u,\"ILOffset\":%u}",
                mapping.nativeLoc.CodeOffset(GetEmitter()), mapping.debugInfo.GetInlineContext()->GetOrdinal(),
                mapping.debugInfo.GetLocation().GetOffset());
    }

    fprintf(file, "]}\n");

    fclose(file);
}

#endif

//------------------------------------------------------------------------
// genRecordRichDebugInfoInlineTree:
//   Recursively process a context in the inline tree and record information
//   about it.
//
// Parameters:
//   context - the inline context
//   nodes   - the array to record into
//
void CodeGen::genRecordRichDebugInfoInlineTree(InlineContext* context, ICorDebugInfo::InlineTreeNode* nodes)
{
    if (context->IsSuccess())
    {
        // We expect 1 + NumInlines unique ordinals
        assert(context->GetOrdinal() <= compiler->m_inlineStrategy->GetInlineCount());

        ICorDebugInfo::InlineTreeNode* node = &nodes[context->GetOrdinal()];
        node->Method                        = context->GetCallee();
        node->ILOffset                      = context->GetActualCallOffset();
        node->Child                         = context->GetChild() == nullptr ? 0 : context->GetChild()->GetOrdinal();
        node->Sibling = context->GetSibling() == nullptr ? 0 : context->GetSibling()->GetOrdinal();
    }

    if (context->GetSibling() != nullptr)
    {
        genRecordRichDebugInfoInlineTree(context->GetSibling(), nodes);
    }

    if (context->GetChild() != nullptr)
    {
        genRecordRichDebugInfoInlineTree(context->GetChild(), nodes);
    }
}

//------------------------------------------------------------------------
// genReportRichDebugInfo:
//   If enabled, report rich debugging information to file and/or EE.
//
void CodeGen::genReportRichDebugInfo()
{
    INDEBUG(genReportRichDebugInfoToFile());

    if (JitConfig.RichDebugInfo() == 0)
    {
        return;
    }

    unsigned numContexts     = 1 + compiler->m_inlineStrategy->GetInlineCount();
    unsigned numRichMappings = static_cast<unsigned>(compiler->genRichIPmappings.size());

    ICorDebugInfo::InlineTreeNode* inlineTree = static_cast<ICorDebugInfo::InlineTreeNode*>(
        compiler->info.compCompHnd->allocateArray(numContexts * sizeof(ICorDebugInfo::InlineTreeNode)));
    ICorDebugInfo::RichOffsetMapping* mappings = static_cast<ICorDebugInfo::RichOffsetMapping*>(
        compiler->info.compCompHnd->allocateArray(numRichMappings * sizeof(ICorDebugInfo::RichOffsetMapping)));

    memset(inlineTree, 0, numContexts * sizeof(ICorDebugInfo::InlineTreeNode));
    memset(mappings, 0, numRichMappings * sizeof(ICorDebugInfo::RichOffsetMapping));

    genRecordRichDebugInfoInlineTree(compiler->compInlineContext, inlineTree);

#ifdef DEBUG
    for (unsigned i = 0; i < numContexts; i++)
    {
        assert(inlineTree[i].Method != NO_METHOD_HANDLE);
    }
#endif

    size_t mappingIndex = 0;
    for (const RichIPMapping& richMapping : compiler->genRichIPmappings)
    {
        ICorDebugInfo::RichOffsetMapping* mapping = &mappings[mappingIndex];
        assert(richMapping.debugInfo.IsValid());
        mapping->NativeOffset = richMapping.nativeLoc.CodeOffset(GetEmitter());
        mapping->Inlinee      = richMapping.debugInfo.GetInlineContext()->GetOrdinal();
        mapping->ILOffset     = richMapping.debugInfo.GetLocation().GetOffset();
        mapping->Source       = richMapping.debugInfo.GetLocation().EncodeSourceTypes();

        mappingIndex++;
    }

    compiler->info.compCompHnd->reportRichMappings(inlineTree, numContexts, mappings, numRichMappings);
}

//------------------------------------------------------------------------
// genAddRichIPMappingHere:
//   Create a rich IP mapping at the current emit location using the specified
//   debug information.
//
// Parameters:
//   di - the debug information
//
void CodeGen::genAddRichIPMappingHere(const DebugInfo& di)
{
    RichIPMapping mapping;
    mapping.nativeLoc.CaptureLocation(GetEmitter());
    mapping.debugInfo = di;
    compiler->genRichIPmappings.push_back(mapping);
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

//------------------------------------------------------------------------
// indirForm: Make a temporary indir we can feed to pattern matching routines
//    in cases where we don't want to instantiate all the indirs that happen.
//
/* static */ GenTreeIndir CodeGen::indirForm(var_types type, GenTree* base)
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
/* static */ GenTreeStoreInd CodeGen::storeIndirForm(var_types type, GenTree* base, GenTree* data)
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

    inst_Mov(targetType, REG_LNGRET_LO, loRetVal->GetRegNum(), /* canSkip */ true, emitActualTypeSize(TYP_INT));
    inst_Mov(targetType, REG_LNGRET_HI, hiRetVal->GetRegNum(), /* canSkip */ true, emitActualTypeSize(TYP_INT));
}
#endif // TARGET_X86 || TARGET_ARM

//------------------------------------------------------------------------
// genReturn: Generates code for return statement.
//            In case of struct return, delegates to the genStructReturn method.
//            In case of LONG return on 32-bit, delegates to the genLongReturn method.
//
// Arguments:
//    treeNode - The GT_RETURN or GT_RETFILT tree node.
//
// Return Value:
//    None
//
void CodeGen::genReturn(GenTree* treeNode)
{
    assert(treeNode->OperIs(GT_RETURN, GT_RETFILT));

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

#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
            genSimpleReturn(treeNode);
#else // !TARGET_ARM64 || !TARGET_LOONGARCH64 || !TARGET_RISCV64
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
                    GetEmitter()->emitIns_Mov(INS_vmov_f2i, EA_4BYTE, REG_INTRET, op1->GetRegNum(),
                                              /* canSkip */ false);
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
                regNumber retReg;

                if (varTypeUsesIntReg(treeNode))
                {
                    retReg = REG_INTRET;
                }
                else
                {
                    assert(varTypeUsesFloatReg(treeNode));
                    retReg = REG_FLOATRET;
                }

                inst_Mov_Extend(targetType, /* srcInReg */ true, retReg, op1->GetRegNum(), /* canSkip */ true);
            }
#endif // !TARGET_ARM64 || !TARGET_LOONGARCH64 || !TARGET_RISCV64
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
    // There should be a single GT_RETURN while generating profiler ELT callbacks.
    //
    if (treeNode->OperIs(GT_RETURN) && compiler->compIsProfilerHookNeeded())
    {
        // !! NOTE !!
        // Since we are invalidating the assumption that we would slip into the epilog
        // right after the "return", we need to preserve the return reg's GC state
        // across the call until actual method return.

        ReturnTypeDesc retTypeDesc = compiler->compRetTypeDesc;
        unsigned       retRegCount = retTypeDesc.GetReturnRegCount();

        if (compiler->compMethodReturnsRetBufAddr())
        {
            gcInfo.gcMarkRegPtrVal(REG_INTRET, TYP_BYREF);
        }
        else
        {
            for (unsigned i = 0; i < retRegCount; ++i)
            {
                if (varTypeIsGC(retTypeDesc.GetReturnRegType(i)))
                {
                    gcInfo.gcMarkRegPtrVal(retTypeDesc.GetABIReturnReg(i), retTypeDesc.GetReturnRegType(i));
                }
            }
        }

        genProfilingLeaveCallback(CORINFO_HELP_PROF_FCN_LEAVE);

        if (compiler->compMethodReturnsRetBufAddr())
        {
            gcInfo.gcMarkRegSetNpt(genRegMask(REG_INTRET));
        }
        else
        {
            for (unsigned i = 0; i < retRegCount; ++i)
            {
                if (varTypeIsGC(retTypeDesc.GetReturnRegType(i)))
                {
                    gcInfo.gcMarkRegSetNpt(genRegMask(retTypeDesc.GetABIReturnReg(i)));
                }
            }
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
    if (compiler->compCurBB->KindIs(BBJ_EHFINALLYRET, BBJ_EHFAULTRET, BBJ_EHFILTERRET))
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

    genConsumeRegs(treeNode->gtGetOp1());

    GenTree* op1       = treeNode->gtGetOp1();
    GenTree* actualOp1 = op1->gtSkipReloadOrCopy();

    ReturnTypeDesc retTypeDesc = compiler->compRetTypeDesc;
    const unsigned regCount    = retTypeDesc.GetReturnRegCount();
    assert(regCount <= MAX_RET_REG_COUNT);

#if FEATURE_MULTIREG_RET
    // Right now the only enregisterable structs supported are SIMD vector types.
    if (genIsRegCandidateLocal(actualOp1))
    {
#if defined(DEBUG)
        const GenTreeLclVar* lclVar = actualOp1->AsLclVar();
        const LclVarDsc*     varDsc = compiler->lvaGetDesc(lclVar);
        assert(varTypeIsSIMD(varDsc->GetRegisterType()));
        assert(!lclVar->IsMultiReg());
#endif // DEBUG

#ifdef FEATURE_SIMD
        genSIMDSplitReturn(op1, &retTypeDesc);
#endif // FEATURE_SIMD
    }
    else if (actualOp1->OperIs(GT_LCL_VAR) && !actualOp1->AsLclVar()->IsMultiReg())
    {
        GenTreeLclVar* lclNode = actualOp1->AsLclVar();
        LclVarDsc*     varDsc  = compiler->lvaGetDesc(lclNode);
        assert(varDsc->lvIsMultiRegRet);

#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        // On LoongArch64, for a struct like "{ int, double }", "retTypeDesc" will be "{ TYP_INT, TYP_DOUBLE }",
        // i. e. not include the padding for the first field, and so the general loop below won't work.
        var_types type  = retTypeDesc.GetReturnRegType(0);
        regNumber toReg = retTypeDesc.GetABIReturnReg(0);
        GetEmitter()->emitIns_R_S(ins_Load(type), emitTypeSize(type), toReg, lclNode->GetLclNum(), 0);
        if (regCount > 1)
        {
            assert(regCount == 2);
            int offset = genTypeSize(type);
            type       = retTypeDesc.GetReturnRegType(1);
            offset     = (int)((unsigned int)offset < genTypeSize(type) ? genTypeSize(type) : offset);
            toReg      = retTypeDesc.GetABIReturnReg(1);
            GetEmitter()->emitIns_R_S(ins_Load(type), emitTypeSize(type), toReg, lclNode->GetLclNum(), offset);
        }
#else  // !TARGET_LOONGARCH64 && !TARGET_RISCV64
        int offset = 0;
        for (unsigned i = 0; i < regCount; ++i)
        {
            var_types type  = retTypeDesc.GetReturnRegType(i);
            regNumber toReg = retTypeDesc.GetABIReturnReg(i);
            GetEmitter()->emitIns_R_S(ins_Load(type), emitTypeSize(type), toReg, lclNode->GetLclNum(), offset);
            offset += genTypeSize(type);
        }
#endif // !TARGET_LOONGARCH64 && !TARGET_RISCV64
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
                LclVarDsc* varDsc = compiler->lvaGetDesc(actualOp1->AsLclVar());
                assert(varDsc->lvPromoted);
                unsigned fieldVarNum = varDsc->lvFieldLclStart + i;
                assert(compiler->lvaGetDesc(fieldVarNum)->lvOnFrame);

                GetEmitter()->emitIns_R_S(ins_Load(type), emitTypeSize(type), toReg, fieldVarNum, 0);
            }
            else
            {
                // Note that ins_Copy(fromReg, type) will return the appropriate register to copy
                // between register files if needed.
                inst_Mov(type, toReg, fromReg, /* canSkip */ true);
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
//    lclNode  -  GenTree of GT_STORE_LCL_VAR
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

    GenTree* op1 = lclNode->gtGetOp1();
    assert(op1->IsMultiRegNode());
    GenTree* actualOp1 = op1->gtSkipReloadOrCopy();
    unsigned regCount  = actualOp1->GetMultiRegCount(compiler);
    assert(regCount > 1);

    // Assumption: current implementation requires that a multi-reg
    // var in 'var = call' is flagged as lvIsMultiRegRet to prevent it from
    // being promoted, unless compiler->lvaEnregMultiRegVars is true.

    unsigned   lclNum = lclNode->GetLclNum();
    LclVarDsc* varDsc = compiler->lvaGetDesc(lclNum);
    if (op1->OperIs(GT_CALL))
    {
        assert(regCount <= MAX_RET_REG_COUNT);
        noway_assert(varDsc->lvIsMultiRegRet);
    }

#ifdef FEATURE_SIMD
    // Check for the case of an enregistered SIMD type that's returned in multiple registers.
    if (varDsc->lvIsRegCandidate() && (lclNode->GetRegNum() != REG_NA))
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

                // We may need a cross register-file copy here.
                inst_Mov(destType, varReg, reg, /* canSkip */ true);
            }
            else
            {
                varReg = REG_STK;
            }
            if ((varReg == REG_STK) || fieldVarDsc->IsAlwaysAliveInMemory())
            {
                if (!lclNode->IsLastUse(i))
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
#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
            // should consider the padding field within a struct.
            offset = (offset % genTypeSize(srcType)) ? AlignUp(offset, genTypeSize(srcType)) : offset;
#endif
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
        unsigned regCount = op1->GetMultiRegCount(compiler);
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

    inst_Mov(targetType, targetReg, srcReg, /* canSkip */ false);

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

                // Report the home change for this variable
                varLiveKeeper->siUpdateVariableLiveRange(varDsc, lcl->GetLclNum());

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
            LclVarDsc* parentVarDsc = compiler->lvaGetDesc(op1->AsLclVar());
            unsigned   fieldVarNum  = parentVarDsc->lvFieldLclStart + multiRegIndex;
            LclVarDsc* fieldVarDsc  = compiler->lvaGetDesc(fieldVarNum);
            type                    = fieldVarDsc->TypeGet();
            inst_Mov(type, targetReg, sourceReg, /* canSkip */ false);
            if (!op1->AsLclVar()->IsLastUse(multiRegIndex) && fieldVarDsc->GetRegNum() != REG_STK)
            {
                // The old location is dying
                genUpdateRegLife(fieldVarDsc, /*isBorn*/ false, /*isDying*/ true DEBUGARG(op1));
                gcInfo.gcMarkRegSetNpt(genRegMask(sourceReg));
                genUpdateVarReg(fieldVarDsc, treeNode);

                // Report the home change for this variable
                varLiveKeeper->siUpdateVariableLiveRange(fieldVarDsc, fieldVarNum);

                // The new location is going live
                genUpdateRegLife(fieldVarDsc, /*isBorn*/ true, /*isDying*/ false DEBUGARG(treeNode));
            }
        }
        else
        {
            type = op1->GetRegTypeByIndex(multiRegIndex);
            inst_Mov(type, targetReg, sourceReg, /* canSkip */ false);
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
//    offset              - the offset from the stack pointer to expect
//    regTmp              - register we can use for computation if `offset` != 0
//
// Return Value:
//    None
//
void CodeGen::genStackPointerCheck(bool      doStackPointerCheck,
                                   unsigned  lvaStackPointerVar,
                                   ssize_t   offset,
                                   regNumber regTmp)
{
    if (doStackPointerCheck)
    {
        assert(lvaStackPointerVar != BAD_VAR_NUM);
        assert(compiler->lvaGetDesc(lvaStackPointerVar)->lvDoNotEnregister);
        assert(compiler->lvaGetDesc(lvaStackPointerVar)->lvOnFrame);

        if (offset != 0)
        {
            assert(regTmp != REG_NA);
            GetEmitter()->emitIns_Mov(INS_mov, EA_PTRSIZE, regTmp, REG_SPBASE, /* canSkip */ false);
            GetEmitter()->emitIns_R_I(INS_sub, EA_PTRSIZE, regTmp, offset);
            GetEmitter()->emitIns_S_R(INS_cmp, EA_PTRSIZE, regTmp, lvaStackPointerVar, 0);
        }
        else
        {
            GetEmitter()->emitIns_S_R(INS_cmp, EA_PTRSIZE, REG_SPBASE, lvaStackPointerVar, 0);
        }

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

//-----------------------------------------------------------------------------
// genPoisonFrame: Generate code that places a recognizable value into address exposed variables.
//
// Remarks:
//   This function emits code to poison address exposed non-zero-inited local variables. We expect this function
//   to be called when emitting code for the scratch BB that comes right after the prolog.
//   The variables are poisoned using 0xcdcdcdcd.
void CodeGen::genPoisonFrame(regMaskTP regLiveIn)
{
    assert(compiler->compShouldPoisonFrame());
#if defined(TARGET_XARCH)
    regNumber poisonValReg = REG_EAX;
    assert((regLiveIn & (RBM_EDI | RBM_ECX | RBM_EAX)) == 0);
#else
    regNumber poisonValReg = REG_SCRATCH;
    assert((regLiveIn & (genRegMask(REG_SCRATCH) | RBM_ARG_0 | RBM_ARG_1 | RBM_ARG_2)) == 0);
#endif

#ifdef TARGET_64BIT
    const ssize_t poisonVal = (ssize_t)0xcdcdcdcdcdcdcdcd;
#else
    const ssize_t poisonVal = (ssize_t)0xcdcdcdcd;
#endif

    // The first time we need to poison something we will initialize a register to the largest immediate cccccccc that
    // we can fit.
    bool hasPoisonImm = false;
    for (unsigned varNum = 0; varNum < compiler->info.compLocalsCount; varNum++)
    {
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
        if (varDsc->lvIsParam || varDsc->lvMustInit || !varDsc->IsAddressExposed())
        {
            continue;
        }

        assert(varDsc->lvOnFrame);

        unsigned int size = compiler->lvaLclSize(varNum);
        if ((size / TARGET_POINTER_SIZE) > 16)
        {
            // This will require more than 16 instructions, switch to rep stosd/memset call.
            CLANG_FORMAT_COMMENT_ANCHOR;
#if defined(TARGET_XARCH)
            GetEmitter()->emitIns_R_S(INS_lea, EA_PTRSIZE, REG_EDI, (int)varNum, 0);
            assert(size % 4 == 0);
            instGen_Set_Reg_To_Imm(EA_4BYTE, REG_ECX, size / 4);
            // On xarch we can leave the value in eax and only set eax once
            // since rep stosd does not kill eax.
            if (!hasPoisonImm)
            {
                instGen_Set_Reg_To_Imm(EA_PTRSIZE, REG_EAX, poisonVal);
                hasPoisonImm = true;
            }
            instGen(INS_r_stosd);
#else
            GetEmitter()->emitIns_R_S(INS_lea, EA_PTRSIZE, REG_ARG_0, (int)varNum, 0);
            instGen_Set_Reg_To_Imm(EA_4BYTE, REG_ARG_1, static_cast<char>(poisonVal));
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, REG_ARG_2, size);
            genEmitHelperCall(CORINFO_HELP_MEMSET, 0, EA_UNKNOWN);
            // May kill REG_SCRATCH, so we need to reload it.
            hasPoisonImm = false;
#endif
        }
        else
        {
            if (!hasPoisonImm)
            {
                instGen_Set_Reg_To_Imm(EA_PTRSIZE, poisonValReg, poisonVal);
                hasPoisonImm = true;
            }

// For 64-bit we check if the local is 8-byte aligned. For 32-bit, we assume everything is always 4-byte aligned.
#ifdef TARGET_64BIT
            bool fpBased;
            int  addr = compiler->lvaFrameAddress((int)varNum, &fpBased);
#else
            int addr     = 0;
#endif
            int end = addr + (int)size;
            for (int offs = addr; offs < end;)
            {
#ifdef TARGET_64BIT
                if ((offs % 8) == 0 && end - offs >= 8)
                {
                    GetEmitter()->emitIns_S_R(ins_Store(TYP_LONG), EA_8BYTE, REG_SCRATCH, (int)varNum, offs - addr);
                    offs += 8;
                    continue;
                }
#endif

                assert((offs % 4) == 0 && end - offs >= 4);
                GetEmitter()->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, REG_SCRATCH, (int)varNum, offs - addr);
                offs += 4;
            }
        }
    }
}

//----------------------------------------------------------------------
// genBitCast - Generate the instruction to move a value between register files
//
// Arguments
//    targetType - the destination type
//    targetReg  - the destination register
//    srcType    - the source type
//    srcReg     - the source register
//
void CodeGen::genBitCast(var_types targetType, regNumber targetReg, var_types srcType, regNumber srcReg)
{
    const bool srcFltReg = varTypeUsesFloatReg(srcType);
    assert(srcFltReg == genIsValidFloatReg(srcReg));

    const bool dstFltReg = varTypeUsesFloatReg(targetType);
    assert(dstFltReg == genIsValidFloatReg(targetReg));

    inst_Mov(targetType, targetReg, srcReg, /* canSkip */ true);
}

//----------------------------------------------------------------------
// genCodeForBitCast - Generate code for a GT_BITCAST that is not contained
//
// Arguments
//    treeNode - the GT_BITCAST for which we're generating code
//
void CodeGen::genCodeForBitCast(GenTreeOp* treeNode)
{
    assert(treeNode->TypeGet() == genActualType(treeNode));
    regNumber targetReg  = treeNode->GetRegNum();
    var_types targetType = treeNode->TypeGet();
    GenTree*  op1        = treeNode->gtGetOp1();
    genConsumeRegs(op1);

    if (op1->isContained())
    {
        assert(op1->OperIs(GT_LCL_VAR));
        unsigned    lclNum  = op1->AsLclVarCommon()->GetLclNum();
        instruction loadIns = ins_Load(targetType, compiler->isSIMDTypeLocalAligned(lclNum));
        GetEmitter()->emitIns_R_S(loadIns, emitTypeSize(targetType), targetReg, lclNum, 0);
    }
    else
    {
#ifdef TARGET_ARM
        if (compiler->opts.compUseSoftFP && (targetType == TYP_LONG))
        {
            // This is a special arm-softFP case when a TYP_LONG node was introduced during lowering
            // for a call argument,  so it was not handled by decomposelongs phase as all other TYP_LONG nodes.
            // Example foo(double LclVar V01), LclVar V01 has to be passed in general registers r0, r1,
            // so lowering will add `BITCAST long(LclVar double V01)` and codegen has to support it here.
            const regNumber srcReg   = op1->GetRegNum();
            const regNumber otherReg = treeNode->AsMultiRegOp()->gtOtherReg;
            assert(otherReg != REG_NA);
            inst_RV_RV_RV(INS_vmov_d2i, targetReg, otherReg, srcReg, EA_8BYTE);
        }
        else
#endif // TARGET_ARM
        {
            genBitCast(targetType, targetReg, op1->TypeGet(), op1->GetRegNum());
        }
    }
    genProduceReg(treeNode);
}

//----------------------------------------------------------------------
// genCanOmitNormalizationForBswap16:
//   Small peephole to check if a bswap16 node can omit normalization.
//
// Arguments:
//   tree - The BSWAP16 node
//
// Remarks:
//   BSWAP16 nodes are required to zero extend the upper 16 bits, but since the
//   importer always inserts a normalizing cast (either sign or zero extending)
//   we almost never need to actually do this.
//
bool CodeGen::genCanOmitNormalizationForBswap16(GenTree* tree)
{
    if (compiler->opts.OptimizationDisabled())
    {
        return false;
    }

    assert(tree->OperIs(GT_BSWAP16));
    if ((tree->gtNext == nullptr) || !tree->gtNext->OperIs(GT_CAST))
    {
        return false;
    }

    GenTreeCast* cast = tree->gtNext->AsCast();
    if (cast->gtOverflow() || (cast->CastOp() != tree))
    {
        return false;
    }

    return (cast->gtCastType == TYP_USHORT) || (cast->gtCastType == TYP_SHORT);
}

//----------------------------------------------------------------------
// genCodeForReuseVal: Generate code for a node marked with re-using a register.
//
// Arguments:
//   tree - The node marked with re-using a register
//
// Remarks:
//   Generates nothing, except for when the node is a CNS_INT(0) where
//   we will define a new label to propagate GC info. We want to do this
//   because if the node is a CNS_INT(0) and is re-using a register,
//   that register could have been used for a CNS_INT(ref null) that is GC
//   tracked.
//
void CodeGen::genCodeForReuseVal(GenTree* treeNode)
{
    assert(treeNode->IsReuseRegVal());

    // For now, this is only used for constant nodes.
    assert(treeNode->OperIs(GT_CNS_INT, GT_CNS_DBL, GT_CNS_VEC));
    JITDUMP("  TreeNode is marked ReuseReg\n");

    if (treeNode->IsIntegralConst(0) && GetEmitter()->emitCurIGnonEmpty())
    {
        genDefineTempLabel(genCreateTempLabel());
    }
}
