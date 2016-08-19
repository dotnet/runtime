// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

/*****************************************************************************/

const BYTE genTypeSizes[] = {
#define DEF_TP(tn, nm, jitType, verType, sz, sze, asze, st, al, tf, howUsed) sz,
#include "typelist.h"
#undef DEF_TP
};

const BYTE genTypeAlignments[] = {
#define DEF_TP(tn, nm, jitType, verType, sz, sze, asze, st, al, tf, howUsed) al,
#include "typelist.h"
#undef DEF_TP
};

const BYTE genTypeStSzs[] = {
#define DEF_TP(tn, nm, jitType, verType, sz, sze, asze, st, al, tf, howUsed) st,
#include "typelist.h"
#undef DEF_TP
};

const BYTE genActualTypes[] = {
#define DEF_TP(tn, nm, jitType, verType, sz, sze, asze, st, al, tf, howUsed) jitType,
#include "typelist.h"
#undef DEF_TP
};

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
    : gcInfo(theCompiler), regSet(theCompiler, gcInfo), compiler(theCompiler)
{
}

/*****************************************************************************/

CodeGen::CodeGen(Compiler* theCompiler) : CodeGenInterface(theCompiler)
{
#if defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87
    negBitmaskFlt  = nullptr;
    negBitmaskDbl  = nullptr;
    absBitmaskFlt  = nullptr;
    absBitmaskDbl  = nullptr;
    u8ToDblBitmask = nullptr;
#endif // defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87

    regTracker.rsTrackInit(compiler, &regSet);
    gcInfo.regSet        = &regSet;
    m_cgEmitter          = new (compiler->getAllocator()) emitter();
    m_cgEmitter->codeGen = this;
    m_cgEmitter->gcInfo  = &gcInfo;

#ifdef DEBUG
    setVerbose(compiler->verbose);
#endif // DEBUG

    compiler->tmpInit();

#ifdef DEBUG
#if defined(_TARGET_X86_) && defined(LEGACY_BACKEND)
    // This appears to be x86-specific. It's attempting to make sure all offsets to temps
    // are large. For ARM, this doesn't interact well with our decision about whether to use
    // R10 or not as a reserved register.
    if (regSet.rsStressRegs())
        compiler->tmpIntSpillMax = (SCHAR_MAX / sizeof(int));
#endif // defined(_TARGET_X86_) && defined(LEGACY_BACKEND)
#endif // DEBUG

    instInit();

#ifdef LEGACY_BACKEND
    // TODO-Cleanup: These used to be set in rsInit() - should they be moved to RegSet??
    // They are also accessed by the register allocators and fgMorphLclVar().
    intRegState.rsCurRegArgNum   = 0;
    floatRegState.rsCurRegArgNum = 0;
#endif // LEGACY_BACKEND

#ifdef LATE_DISASM
    getDisAssembler().disInit(compiler);
#endif

#ifdef DEBUG
    genTempLiveChg        = true;
    genTrnslLocalVarCount = 0;

    // Shouldn't be used before it is set in genFnProlog()
    compiler->compCalleeRegsPushed = UninitializedWord<unsigned>();

#if defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87
    // Shouldn't be used before it is set in genFnProlog()
    compiler->compCalleeFPRegsSavedMask = (regMaskTP)-1;
#endif // defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87
#endif // DEBUG

#ifdef _TARGET_AMD64_
    // This will be set before final frame layout.
    compiler->compVSQuirkStackPaddingNeeded = 0;

    // Set to true if we perform the Quirk that fixes the PPP issue
    compiler->compQuirkForPPPflag = false;
#endif // _TARGET_AMD64_

#ifdef LEGACY_BACKEND
    genFlagsEqualToNone();
#endif // LEGACY_BACKEND

#ifdef DEBUGGING_SUPPORT
    //  Initialize the IP-mapping logic.
    compiler->genIPmappingList        = nullptr;
    compiler->genIPmappingLast        = nullptr;
    compiler->genCallSite2ILOffsetMap = nullptr;
#endif

    /* Assume that we not fully interruptible */

    genInterruptible = false;
#ifdef DEBUG
    genInterruptibleUsed = false;
    genCurDispOffset     = (unsigned)-1;
#endif
}

void CodeGenInterface::genMarkTreeInReg(GenTreePtr tree, regNumber reg)
{
    tree->gtRegNum = reg;
    tree->gtFlags |= GTF_REG_VAL;
}

#if CPU_LONG_USES_REGPAIR
void CodeGenInterface::genMarkTreeInRegPair(GenTreePtr tree, regPairNo regPair)
{
    tree->gtRegPair = regPair;
    tree->gtFlags |= GTF_REG_VAL;
}
#endif

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)

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

int CodeGenInterface::genTotalFrameSize()
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

int CodeGenInterface::genSPtoFPdelta()
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

int CodeGenInterface::genCallerSPtoFPdelta()
{
    assert(isFramePointerUsed());
    int callerSPtoFPdelta = 0;

#if defined(_TARGET_ARM_)
    // On ARM, we first push the prespill registers, then store LR, then R11 (FP), and point R11 at the saved R11.
    callerSPtoFPdelta -= genCountBits(regSet.rsMaskPreSpillRegs(true)) * REGSIZE_BYTES;
    callerSPtoFPdelta -= 2 * REGSIZE_BYTES;
#elif defined(_TARGET_X86_)
    // Thanks to ebp chaining, the difference between ebp-based addresses
    // and caller-SP-relative addresses is just the 2 pointers:
    //     return address
    //     pushed ebp
    callerSPtoFPdelta -= 2 * REGSIZE_BYTES;
#else
#error "Unknown _TARGET_"
#endif // _TARGET_*

    assert(callerSPtoFPdelta <= 0);
    return callerSPtoFPdelta;
}

//---------------------------------------------------------------------
// genCallerSPtoInitialSPdelta - return the offset from Caller-SP to Initial SP.
//
// This number will be negative.

int CodeGenInterface::genCallerSPtoInitialSPdelta()
{
    int callerSPtoSPdelta = 0;

#if defined(_TARGET_ARM_)
    callerSPtoSPdelta -= genCountBits(regSet.rsMaskPreSpillRegs(true)) * REGSIZE_BYTES;
    callerSPtoSPdelta -= genTotalFrameSize();
#elif defined(_TARGET_X86_)
    callerSPtoSPdelta -= genTotalFrameSize();
    callerSPtoSPdelta -= REGSIZE_BYTES; // caller-pushed return address

    // compCalleeRegsPushed does not account for the frame pointer
    // TODO-Cleanup: shouldn't this be part of genTotalFrameSize?
    if (isFramePointerUsed())
    {
        callerSPtoSPdelta -= REGSIZE_BYTES;
    }
#else
#error "Unknown _TARGET_"
#endif // _TARGET_*

    assert(callerSPtoSPdelta <= 0);
    return callerSPtoSPdelta;
}

#endif // defined(_TARGET_X86_) || defined(_TARGET_ARM_)

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
    unsigned   varNum;
    LclVarDsc* varDsc;

    /* Figure out which non-register variables hold pointers */

    VarSetOps::AssignNoCopy(compiler, gcInfo.gcTrkStkPtrLcls, VarSetOps::MakeEmpty(compiler));

    // Figure out which variables live in registers.
    // Also, initialize gcTrkStkPtrLcls to include all tracked variables that do not fully live
    // in a register (i.e. they live on the stack for all or part of their lifetime).
    // Note that lvRegister indicates that a lclVar is in a register for its entire lifetime.

    VarSetOps::AssignNoCopy(compiler, compiler->raRegVarsMask, VarSetOps::MakeEmpty(compiler));

    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        if (varDsc->lvTracked
#ifndef LEGACY_BACKEND
            || varDsc->lvIsRegCandidate()
#endif // !LEGACY_BACKEND
                )
        {
            if (varDsc->lvRegister
#if FEATURE_STACK_FP_X87
                && !varDsc->IsFloatRegType()
#endif
                    )
            {
                VarSetOps::AddElemD(compiler, compiler->raRegVarsMask, varDsc->lvVarIndex);
            }
            else if (compiler->lvaIsGCTracked(varDsc) && (!varDsc->lvIsParam || varDsc->lvIsRegArg))
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

/*****************************************************************************
 *  To report exception handling information to the VM, we need the size of the exception
 *  handling regions. To compute that, we need to emit labels for the beginning block of
 *  an EH region, and the block that immediately follows a region. Go through the EH
 *  table and mark all these blocks with BBF_HAS_LABEL to make this happen.
 *
 *  The beginning blocks of the EH regions already should have this flag set.
 *
 *  No blocks should be added or removed after this.
 *
 *  This code is closely couple with genReportEH() in the sense that any block
 *  that this procedure has determined it needs to have a label has to be selected
 *  using the same logic both here and in genReportEH(), so basically any time there is
 *  a change in the way we handle EH reporting, we have to keep the logic of these two
 *  methods 'in sync'.
 */

void CodeGen::genPrepForEHCodegen()
{
    assert(!compiler->fgSafeBasicBlockCreation);

    EHblkDsc* HBtab;
    EHblkDsc* HBtabEnd;

    bool anyFinallys = false;

    for (HBtab = compiler->compHndBBtab, HBtabEnd = compiler->compHndBBtab + compiler->compHndBBtabCount;
         HBtab < HBtabEnd; HBtab++)
    {
        assert(HBtab->ebdTryBeg->bbFlags & BBF_HAS_LABEL);
        assert(HBtab->ebdHndBeg->bbFlags & BBF_HAS_LABEL);

        if (HBtab->ebdTryLast->bbNext != nullptr)
        {
            HBtab->ebdTryLast->bbNext->bbFlags |= BBF_HAS_LABEL;
        }

        if (HBtab->ebdHndLast->bbNext != nullptr)
        {
            HBtab->ebdHndLast->bbNext->bbFlags |= BBF_HAS_LABEL;
        }

        if (HBtab->HasFilter())
        {
            assert(HBtab->ebdFilter->bbFlags & BBF_HAS_LABEL);
            // The block after the last block of the filter is
            // the handler begin block, which we already asserted
            // has BBF_HAS_LABEL set.
        }

#ifdef _TARGET_AMD64_
        if (HBtab->HasFinallyHandler())
        {
            anyFinallys = true;
        }
#endif // _TARGET_AMD64_
    }

#ifdef _TARGET_AMD64_
    if (anyFinallys)
    {
        for (BasicBlock* block = compiler->fgFirstBB; block != nullptr; block = block->bbNext)
        {
            if (block->bbJumpKind == BBJ_CALLFINALLY)
            {
                BasicBlock* bbToLabel = block->bbNext;
                if (block->isBBCallAlwaysPair())
                {
                    bbToLabel = bbToLabel->bbNext; // skip the BBJ_ALWAYS
                }
                if (bbToLabel != nullptr)
                {
                    bbToLabel->bbFlags |= BBF_HAS_LABEL;
                }
            } // block is BBJ_CALLFINALLY
        }     // for each block
    }         // if (anyFinallys)
#endif        // _TARGET_AMD64_
}

void CodeGenInterface::genUpdateLife(GenTreePtr tree)
{
    compiler->compUpdateLife</*ForCodeGen*/ true>(tree);
}

void CodeGenInterface::genUpdateLife(VARSET_VALARG_TP newLife)
{
    compiler->compUpdateLife</*ForCodeGen*/ true>(newLife);
}

#ifdef LEGACY_BACKEND
// Returns the liveSet after tree has executed.
// "tree" MUST occur in the current statement, AFTER the most recent
// update of compiler->compCurLifeTree and compiler->compCurLife.
//
VARSET_VALRET_TP CodeGen::genUpdateLiveSetForward(GenTreePtr tree)
{
    VARSET_TP  VARSET_INIT(compiler, startLiveSet, compiler->compCurLife);
    GenTreePtr startNode;
    assert(tree != compiler->compCurLifeTree);
    if (compiler->compCurLifeTree == nullptr)
    {
        assert(compiler->compCurStmt != nullptr);
        startNode = compiler->compCurStmt->gtStmt.gtStmtList;
    }
    else
    {
        startNode = compiler->compCurLifeTree->gtNext;
    }
    return compiler->fgUpdateLiveSet(startLiveSet, startNode, tree);
}

// Determine the registers that are live after "second" has been evaluated,
// but which are not live after "first".
// PRECONDITIONS:
// 1. "first" must occur after compiler->compCurLifeTree in execution order for the current statement
// 2. "second" must occur after "first" in the current statement
//
regMaskTP CodeGen::genNewLiveRegMask(GenTreePtr first, GenTreePtr second)
{
    // First, compute the liveset after "first"
    VARSET_TP firstLiveSet = genUpdateLiveSetForward(first);
    // Now, update the set forward from "first" to "second"
    VARSET_TP secondLiveSet = compiler->fgUpdateLiveSet(firstLiveSet, first->gtNext, second);
    regMaskTP newLiveMask   = genLiveMask(VarSetOps::Diff(compiler, secondLiveSet, firstLiveSet));
    return newLiveMask;
}
#endif

// Return the register mask for the given register variable
// inline
regMaskTP CodeGenInterface::genGetRegMask(const LclVarDsc* varDsc)
{
    regMaskTP regMask = RBM_NONE;

    assert(varDsc->lvIsInReg());

    if (varTypeIsFloating(varDsc->TypeGet()))
    {
        regMask = genRegMaskFloat(varDsc->lvRegNum, varDsc->TypeGet());
    }
    else
    {
        regMask = genRegMask(varDsc->lvRegNum);
        if (isRegPairType(varDsc->lvType))
        {
            regMask |= genRegMask(varDsc->lvOtherReg);
        }
    }
    return regMask;
}

// Return the register mask for the given lclVar or regVar tree node
// inline
regMaskTP CodeGenInterface::genGetRegMask(GenTreePtr tree)
{
    assert(tree->gtOper == GT_LCL_VAR || tree->gtOper == GT_REG_VAR);

    regMaskTP        regMask = RBM_NONE;
    const LclVarDsc* varDsc  = compiler->lvaTable + tree->gtLclVarCommon.gtLclNum;
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

//------------------------------------------------------------------------
// getRegistersFromMask: Given a register mask return the two registers
//                       specified by the mask.
//
// Arguments:
//    regPairMask:  a register mask that has exactly two bits set
// Return values:
//    pLoReg:       the address of where to write the first register
//    pHiReg:       the address of where to write the second register
//
void CodeGenInterface::genGetRegPairFromMask(regMaskTP regPairMask, regNumber* pLoReg, regNumber* pHiReg)
{
    assert(genCountBits(regPairMask) == 2);

    regMaskTP loMask = genFindLowestBit(regPairMask); // set loMask to a one-bit mask
    regMaskTP hiMask = regPairMask - loMask;          // set hiMask to the other bit that was in tmpRegMask

    regNumber loReg = genRegNumFromMask(loMask); // set loReg from loMask
    regNumber hiReg = genRegNumFromMask(hiMask); // set hiReg from hiMask

    *pLoReg = loReg;
    *pHiReg = hiReg;
}

// The given lclVar is either going live (being born) or dying.
// It might be both going live and dying (that is, it is a dead store) under MinOpts.
// Update regSet.rsMaskVars accordingly.
// inline
void CodeGenInterface::genUpdateRegLife(const LclVarDsc* varDsc, bool isBorn, bool isDying DEBUGARG(GenTreePtr tree))
{
#if FEATURE_STACK_FP_X87
    // The stack fp reg vars are handled elsewhere
    if (varTypeIsFloating(varDsc->TypeGet()))
        return;
#endif

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
        // assert((regSet.rsMaskVars & regMask) == regMask);
        regSet.RemoveMaskVars(regMask);
    }
    else
    {
        assert((regSet.rsMaskVars & regMask) == 0);
        regSet.AddMaskVars(regMask);
    }
}

// Gets a register mask that represent the kill set for a helper call since
// not all JIT Helper calls follow the standard ABI on the target architecture.
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
regMaskTP Compiler::compHelperCallKillSet(CorInfoHelpFunc helper)
{
    switch (helper)
    {
        case CORINFO_HELP_ASSIGN_BYREF:
#if defined(_TARGET_AMD64_)
            return RBM_RSI | RBM_RDI | RBM_CALLEE_TRASH;
#elif defined(_TARGET_ARM64_)
            return RBM_CALLEE_TRASH_NOGC;
#else
            NYI("Model kill set for CORINFO_HELP_ASSIGN_BYREF on target arch");
            return RBM_CALLEE_TRASH;
#endif

        case CORINFO_HELP_PROF_FCN_ENTER:
#ifdef _TARGET_AMD64_
            return RBM_PROFILER_ENTER_TRASH;
#else
            unreached();
#endif
        case CORINFO_HELP_PROF_FCN_LEAVE:
        case CORINFO_HELP_PROF_FCN_TAILCALL:
#ifdef _TARGET_AMD64_
            return RBM_PROFILER_LEAVE_TRASH;
#else
            unreached();
#endif

        case CORINFO_HELP_STOP_FOR_GC:
            return RBM_STOP_FOR_GC_TRASH;

        case CORINFO_HELP_INIT_PINVOKE_FRAME:
            return RBM_INIT_PINVOKE_FRAME_TRASH;

        default:
            return RBM_CALLEE_TRASH;
    }
}

//
// Gets a register mask that represents the kill set for "NO GC" helper calls since
// not all JIT Helper calls follow the standard ABI on the target architecture.
//
// Note: This list may not be complete and defaults to the default NOGC registers.
//
regMaskTP Compiler::compNoGCHelperCallKillSet(CorInfoHelpFunc helper)
{
    assert(emitter::emitNoGChelper(helper));
#ifdef _TARGET_AMD64_
    switch (helper)
    {
        case CORINFO_HELP_PROF_FCN_ENTER:
            return RBM_PROFILER_ENTER_TRASH;

        case CORINFO_HELP_PROF_FCN_LEAVE:
        case CORINFO_HELP_PROF_FCN_TAILCALL:
            return RBM_PROFILER_LEAVE_TRASH;

        case CORINFO_HELP_ASSIGN_BYREF:
            // this helper doesn't trash RSI and RDI
            return RBM_CALLEE_TRASH_NOGC & ~(RBM_RSI | RBM_RDI);

        default:
            return RBM_CALLEE_TRASH_NOGC;
    }
#else
    return RBM_CALLEE_TRASH_NOGC;
#endif
}

// Update liveness (always var liveness, i.e., compCurLife, and also, if "ForCodeGen" is true, reg liveness, i.e.,
// regSet.rsMaskVars as well)
// if the given lclVar (or indir(addr(local)))/regVar node is going live (being born) or dying.
template <bool ForCodeGen>
void Compiler::compUpdateLifeVar(GenTreePtr tree, VARSET_TP* pLastUseVars)
{
    GenTreePtr indirAddrLocal = fgIsIndirOfAddrOfLocal(tree);
    assert(tree->OperIsNonPhiLocal() || indirAddrLocal != nullptr);

    // Get the local var tree -- if "tree" is "Ldobj(addr(x))", or "ind(addr(x))" this is "x", else it's "tree".
    GenTreePtr lclVarTree = indirAddrLocal;
    if (lclVarTree == nullptr)
    {
        lclVarTree = tree;
    }
    unsigned int lclNum = lclVarTree->gtLclVarCommon.gtLclNum;
    LclVarDsc*   varDsc = lvaTable + lclNum;

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
#if 0   
        // TODO-ARM64-Bug?: These asserts don't seem right for ARM64: I don't understand why we have to assert 
        // two consecutive lclvars (in execution order) can only be observed if the first one is a struct field.
        // It seems to me this is code only applicable to the legacy JIT and not RyuJIT (and therefore why it was 
        // ifdef'ed out for AMD64).
        else if (!varDsc->lvIsStructField)
        {
            GenTreePtr prevTree;
            for (prevTree = tree->gtPrev;
                 prevTree != NULL && prevTree != compCurLifeTree;
                 prevTree = prevTree->gtPrev)
            {
                if ((prevTree->gtOper == GT_LCL_VAR) || (prevTree->gtOper == GT_REG_VAR))
                {
                    LclVarDsc * prevVarDsc = lvaTable + prevTree->gtLclVarCommon.gtLclNum;

                    // These are the only things for which this method MUST be called
                    assert(prevVarDsc->lvIsStructField);
                }
            }
            assert(prevTree == compCurLifeTree);
        }
#endif // 0
    }
#endif // !_TARGET_AMD64_
#endif // DEBUG

    compCurLifeTree = tree;
    VARSET_TP VARSET_INIT(this, newLife, compCurLife);

    // By codegen, a struct may not be TYP_STRUCT, so we have to
    // check lvPromoted, for the case where the fields are being
    // tracked.
    if (!varDsc->lvTracked && !varDsc->lvPromoted)
    {
        return;
    }

    bool isBorn = ((tree->gtFlags & GTF_VAR_DEF) != 0 && (tree->gtFlags & GTF_VAR_USEASG) == 0); // if it's "x <op>=
                                                                                                 // ..." then variable
                                                                                                 // "x" must have had a
                                                                                                 // previous, original,
                                                                                                 // site to be born.
    bool isDying = ((tree->gtFlags & GTF_VAR_DEATH) != 0);
#ifndef LEGACY_BACKEND
    bool spill = ((tree->gtFlags & GTF_SPILL) != 0);
#endif // !LEGACY_BACKEND

#ifndef LEGACY_BACKEND
    // For RyuJIT backend, since all tracked vars are register candidates, but not all are in registers at all times,
    // we maintain two separate sets of variables - the total set of variables that are either
    // born or dying here, and the subset of those that are on the stack
    VARSET_TP VARSET_INIT_NOCOPY(stackVarDeltaSet, VarSetOps::MakeEmpty(this));
#endif // !LEGACY_BACKEND

    if (isBorn || isDying)
    {
        bool hasDeadTrackedFieldVars = false; // If this is true, then, for a LDOBJ(ADDR(<promoted struct local>)),
        VARSET_TP* deadTrackedFieldVars =
            nullptr; // *deadTrackedFieldVars indicates which tracked field vars are dying.
        VARSET_TP VARSET_INIT_NOCOPY(varDeltaSet, VarSetOps::MakeEmpty(this));

        if (varDsc->lvTracked)
        {
            VarSetOps::AddElemD(this, varDeltaSet, varDsc->lvVarIndex);
            if (ForCodeGen)
            {
#ifndef LEGACY_BACKEND
                if (isBorn && varDsc->lvIsRegCandidate() && tree->gtHasReg())
                {
                    codeGen->genUpdateVarReg(varDsc, tree);
                }
#endif // !LEGACY_BACKEND
                if (varDsc->lvIsInReg()
#ifndef LEGACY_BACKEND
                    && tree->gtRegNum != REG_NA
#endif // !LEGACY_BACKEND
                    )
                {
                    codeGen->genUpdateRegLife(varDsc, isBorn, isDying DEBUGARG(tree));
                }
#ifndef LEGACY_BACKEND
                else
                {
                    VarSetOps::AddElemD(this, stackVarDeltaSet, varDsc->lvVarIndex);
                }
#endif // !LEGACY_BACKEND
            }
        }
        else if (varDsc->lvPromoted)
        {
            if (indirAddrLocal != nullptr && isDying)
            {
                assert(!isBorn); // GTF_VAR_DEATH only set for LDOBJ last use.
                hasDeadTrackedFieldVars = GetPromotedStructDeathVars()->Lookup(indirAddrLocal, &deadTrackedFieldVars);
                if (hasDeadTrackedFieldVars)
                {
                    VarSetOps::Assign(this, varDeltaSet, *deadTrackedFieldVars);
                }
            }

            for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
            {
                LclVarDsc* fldVarDsc = &(lvaTable[i]);
                noway_assert(fldVarDsc->lvIsStructField);
                if (fldVarDsc->lvTracked)
                {
                    unsigned fldVarIndex = fldVarDsc->lvVarIndex;
                    noway_assert(fldVarIndex < lvaTrackedCount);
                    if (!hasDeadTrackedFieldVars)
                    {
                        VarSetOps::AddElemD(this, varDeltaSet, fldVarIndex);
                        if (ForCodeGen)
                        {
                            // We repeat this call here and below to avoid the VarSetOps::IsMember
                            // test in this, the common case, where we have no deadTrackedFieldVars.
                            if (fldVarDsc->lvIsInReg())
                            {
#ifndef LEGACY_BACKEND
                                if (isBorn)
                                {
                                    codeGen->genUpdateVarReg(fldVarDsc, tree);
                                }
#endif // !LEGACY_BACKEND
                                codeGen->genUpdateRegLife(fldVarDsc, isBorn, isDying DEBUGARG(tree));
                            }
#ifndef LEGACY_BACKEND
                            else
                            {
                                VarSetOps::AddElemD(this, stackVarDeltaSet, fldVarIndex);
                            }
#endif // !LEGACY_BACKEND
                        }
                    }
                    else if (ForCodeGen && VarSetOps::IsMember(this, varDeltaSet, fldVarIndex))
                    {
                        if (lvaTable[i].lvIsInReg())
                        {
#ifndef LEGACY_BACKEND
                            if (isBorn)
                            {
                                codeGen->genUpdateVarReg(fldVarDsc, tree);
                            }
#endif // !LEGACY_BACKEND
                            codeGen->genUpdateRegLife(fldVarDsc, isBorn, isDying DEBUGARG(tree));
                        }
#ifndef LEGACY_BACKEND
                        else
                        {
                            VarSetOps::AddElemD(this, stackVarDeltaSet, fldVarIndex);
                        }
#endif // !LEGACY_BACKEND
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
            VarSetOps::DiffD(this, newLife, varDeltaSet);
            if (pLastUseVars != nullptr)
            {
                VarSetOps::Assign(this, *pLastUseVars, varDeltaSet);
            }
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
            VarSetOps::UnionD(this, newLife, varDeltaSet);
        }
    }

    if (!VarSetOps::Equal(this, compCurLife, newLife))
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\t\t\t\t\t\t\tLive vars: ");
            dumpConvertedVarSet(this, compCurLife);
            printf(" => ");
            dumpConvertedVarSet(this, newLife);
            printf("\n");
        }
#endif // DEBUG

        VarSetOps::Assign(this, compCurLife, newLife);

        if (ForCodeGen)
        {
#ifndef LEGACY_BACKEND

            // Only add vars to the gcInfo.gcVarPtrSetCur if they are currently on stack, since the
            // gcInfo.gcTrkStkPtrLcls
            // includes all TRACKED vars that EVER live on the stack (i.e. are not always in a register).
            VARSET_TP VARSET_INIT_NOCOPY(gcTrkStkDeltaSet,
                                         VarSetOps::Intersection(this, codeGen->gcInfo.gcTrkStkPtrLcls,
                                                                 stackVarDeltaSet));
            if (!VarSetOps::IsEmpty(this, gcTrkStkDeltaSet))
            {
#ifdef DEBUG
                if (verbose)
                {
                    printf("\t\t\t\t\t\t\tGCvars: ");
                    dumpConvertedVarSet(this, codeGen->gcInfo.gcVarPtrSetCur);
                    printf(" => ");
                }
#endif // DEBUG

                if (isBorn)
                {
                    VarSetOps::UnionD(this, codeGen->gcInfo.gcVarPtrSetCur, gcTrkStkDeltaSet);
                }
                else
                {
                    VarSetOps::DiffD(this, codeGen->gcInfo.gcVarPtrSetCur, gcTrkStkDeltaSet);
                }

#ifdef DEBUG
                if (verbose)
                {
                    dumpConvertedVarSet(this, codeGen->gcInfo.gcVarPtrSetCur);
                    printf("\n");
                }
#endif // DEBUG
            }

#else // LEGACY_BACKEND

#ifdef DEBUG
            if (verbose)
            {
                VARSET_TP VARSET_INIT_NOCOPY(gcVarPtrSetNew,
                                             VarSetOps::Intersection(this, newLife, codeGen->gcInfo.gcTrkStkPtrLcls));
                if (!VarSetOps::Equal(this, codeGen->gcInfo.gcVarPtrSetCur, gcVarPtrSetNew))
                {
                    printf("\t\t\t\t\t\t\tGCvars: ");
                    dumpConvertedVarSet(this, codeGen->gcInfo.gcVarPtrSetCur);
                    printf(" => ");
                    dumpConvertedVarSet(this, gcVarPtrSetNew);
                    printf("\n");
                }
            }
#endif // DEBUG

            VarSetOps::AssignNoCopy(this, codeGen->gcInfo.gcVarPtrSetCur,
                                    VarSetOps::Intersection(this, newLife, codeGen->gcInfo.gcTrkStkPtrLcls));

#endif // LEGACY_BACKEND

#ifdef DEBUGGING_SUPPORT
            codeGen->siUpdate();
#endif
        }
    }

#ifndef LEGACY_BACKEND
    if (ForCodeGen && spill)
    {
        assert(!varDsc->lvPromoted);
        codeGen->genSpillVar(tree);
        if (VarSetOps::IsMember(this, codeGen->gcInfo.gcTrkStkPtrLcls, varDsc->lvVarIndex))
        {
            if (!VarSetOps::IsMember(this, codeGen->gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex))
            {
                VarSetOps::AddElemD(this, codeGen->gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);
#ifdef DEBUG
                if (verbose)
                {
                    printf("\t\t\t\t\t\t\tVar V%02u becoming live\n", varDsc - lvaTable);
                }
#endif // DEBUG
            }
        }
    }
#endif // !LEGACY_BACKEND
}

// Need an explicit instantiation.
template void Compiler::compUpdateLifeVar<false>(GenTreePtr tree, VARSET_TP* pLastUseVars);

template <bool ForCodeGen>
void Compiler::compChangeLife(VARSET_VALARG_TP newLife DEBUGARG(GenTreePtr tree))
{
    LclVarDsc* varDsc;

#ifdef DEBUG
    if (verbose)
    {
        if (tree != nullptr)
        {
            Compiler::printTreeID(tree);
        }
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
    VARSET_TP VARSET_INIT(this, deadSet, compCurLife);
    VarSetOps::DiffD(this, deadSet, newLife);

    // bornSet = newLife - compCurLife
    VARSET_TP VARSET_INIT(this, bornSet, newLife);
    VarSetOps::DiffD(this, bornSet, compCurLife);

    /* Can't simultaneously become live and dead at the same time */

    // (deadSet UNION bornSet) != EMPTY
    noway_assert(!VarSetOps::IsEmpty(this, VarSetOps::Union(this, deadSet, bornSet)));
    // (deadSet INTERSECTION bornSet) == EMPTY
    noway_assert(VarSetOps::IsEmpty(this, VarSetOps::Intersection(this, deadSet, bornSet)));

#ifdef LEGACY_BACKEND
    // In the LEGACY_BACKEND case, we only consider variables that are fully enregisterd
    // and there may be none.
    VarSetOps::IntersectionD(this, deadSet, raRegVarsMask);
    VarSetOps::IntersectionD(this, bornSet, raRegVarsMask);
    // And all gcTrkStkPtrLcls that are now live will be on the stack
    VarSetOps::AssignNoCopy(this, codeGen->gcInfo.gcVarPtrSetCur,
                            VarSetOps::Intersection(this, newLife, codeGen->gcInfo.gcTrkStkPtrLcls));
#endif // LEGACY_BACKEND

    VarSetOps::Assign(this, compCurLife, newLife);

    // Handle the dying vars first, then the newly live vars.
    // This is because, in the RyuJIT backend case, they may occupy registers that
    // will be occupied by another var that is newly live.
    VARSET_ITER_INIT(this, deadIter, deadSet, deadVarIndex);
    while (deadIter.NextElem(this, &deadVarIndex))
    {
        unsigned varNum = lvaTrackedToVarNum[deadVarIndex];
        varDsc          = lvaTable + varNum;
        bool isGCRef    = (varDsc->TypeGet() == TYP_REF);
        bool isByRef    = (varDsc->TypeGet() == TYP_BYREF);

        if (varDsc->lvIsInReg())
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
            codeGen->genUpdateRegLife(varDsc, false /*isBorn*/, true /*isDying*/ DEBUGARG(tree));
        }
#ifndef LEGACY_BACKEND
        // This isn't in a register, so update the gcVarPtrSetCur.
        // (Note that in the LEGACY_BACKEND case gcVarPtrSetCur is updated above unconditionally
        // for all gcTrkStkPtrLcls in newLife, because none of them ever live in a register.)
        else if (isGCRef || isByRef)
        {
            VarSetOps::RemoveElemD(this, codeGen->gcInfo.gcVarPtrSetCur, deadVarIndex);
            JITDUMP("\t\t\t\t\t\t\tV%02u becoming dead\n", varNum);
        }
#endif // !LEGACY_BACKEND
    }

    VARSET_ITER_INIT(this, bornIter, bornSet, bornVarIndex);
    while (bornIter.NextElem(this, &bornVarIndex))
    {
        unsigned varNum = lvaTrackedToVarNum[bornVarIndex];
        varDsc          = lvaTable + varNum;
        bool isGCRef    = (varDsc->TypeGet() == TYP_REF);
        bool isByRef    = (varDsc->TypeGet() == TYP_BYREF);

        if (varDsc->lvIsInReg())
        {
#ifndef LEGACY_BACKEND
#ifdef DEBUG
            if (VarSetOps::IsMember(this, codeGen->gcInfo.gcVarPtrSetCur, bornVarIndex))
            {
                JITDUMP("\t\t\t\t\t\t\tRemoving V%02u from gcVarPtrSetCur\n", varNum);
            }
#endif // DEBUG
            VarSetOps::RemoveElemD(this, codeGen->gcInfo.gcVarPtrSetCur, bornVarIndex);
#endif // !LEGACY_BACKEND
            codeGen->genUpdateRegLife(varDsc, true /*isBorn*/, false /*isDying*/ DEBUGARG(tree));
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
#ifndef LEGACY_BACKEND
        // This isn't in a register, so update the gcVarPtrSetCur
        else if (lvaIsGCTracked(varDsc))
        {
            VarSetOps::AddElemD(this, codeGen->gcInfo.gcVarPtrSetCur, bornVarIndex);
            JITDUMP("\t\t\t\t\t\t\tV%02u becoming live\n", varNum);
        }
#endif // !LEGACY_BACKEND
    }

#ifdef DEBUGGING_SUPPORT
    codeGen->siUpdate();
#endif
}

// Need an explicit instantiation.
template void Compiler::compChangeLife<true>(VARSET_VALARG_TP newLife DEBUGARG(GenTreePtr tree));

#ifdef LEGACY_BACKEND

/*****************************************************************************
 *
 *  Get the mask of integer registers that contain 'live' enregistered
 *  local variables after "tree".
 *
 *  The output is the mask of integer registers that are currently
 *  alive and holding the enregistered local variables.
 */
regMaskTP CodeGenInterface::genLiveMask(GenTreePtr tree)
{
    regMaskTP liveMask = regSet.rsMaskVars;

    GenTreePtr nextNode;
    if (compiler->compCurLifeTree == nullptr)
    {
        assert(compiler->compCurStmt != nullptr);
        nextNode = compiler->compCurStmt->gtStmt.gtStmtList;
    }
    else
    {
        nextNode = compiler->compCurLifeTree->gtNext;
    }

    // Theoretically, we should always be able to find "tree" by walking
    // forward in execution order.  But unfortunately, there is at least
    // one case (addressing) where a node may be evaluated out of order
    // So, we have to handle that case
    bool outOfOrder = false;
    for (; nextNode != tree->gtNext; nextNode = nextNode->gtNext)
    {
        if (nextNode == nullptr)
        {
            outOfOrder = true;
            break;
        }
        if (nextNode->gtOper == GT_LCL_VAR || nextNode->gtOper == GT_REG_VAR)
        {
            bool isBorn  = ((tree->gtFlags & GTF_VAR_DEF) != 0 && (tree->gtFlags & GTF_VAR_USEASG) == 0);
            bool isDying = ((nextNode->gtFlags & GTF_VAR_DEATH) != 0);
            if (isBorn || isDying)
            {
                regMaskTP regMask = genGetRegMask(nextNode);
                if (regMask != RBM_NONE)
                {
                    if (isBorn)
                    {
                        liveMask |= regMask;
                    }
                    else
                    {
                        liveMask &= ~(regMask);
                    }
                }
            }
        }
    }
    if (outOfOrder)
    {
        assert(compiler->compCurLifeTree != nullptr);
        liveMask = regSet.rsMaskVars;
        // We were unable to find "tree" by traversing forward.  We must now go
        // backward from compiler->compCurLifeTree instead.  We have to start with compiler->compCurLifeTree,
        // since regSet.rsMaskVars reflects its completed execution
        for (nextNode = compiler->compCurLifeTree; nextNode != tree; nextNode = nextNode->gtPrev)
        {
            assert(nextNode != nullptr);

            if (nextNode->gtOper == GT_LCL_VAR || nextNode->gtOper == GT_REG_VAR)
            {
                bool isBorn  = ((tree->gtFlags & GTF_VAR_DEF) != 0 && (tree->gtFlags & GTF_VAR_USEASG) == 0);
                bool isDying = ((nextNode->gtFlags & GTF_VAR_DEATH) != 0);
                if (isBorn || isDying)
                {
                    regMaskTP regMask = genGetRegMask(nextNode);
                    if (regMask != RBM_NONE)
                    {
                        // We're going backward - so things born are removed
                        // and vice versa
                        if (isBorn)
                        {
                            liveMask &= ~(regMask);
                        }
                        else
                        {
                            liveMask |= regMask;
                        }
                    }
                }
            }
        }
    }
    return liveMask;
}

/*****************************************************************************
 *
 *  Get the mask of integer registers that contain 'live' enregistered
 *  local variables.

 *  The input is a liveSet which contains a set of local
 *  variables that are currently alive
 *
 *  The output is the mask of x86 integer registers that are currently
 *  alive and holding the enregistered local variables
 */

regMaskTP CodeGenInterface::genLiveMask(VARSET_VALARG_TP liveSet)
{
    // Check for the zero LiveSet mask
    if (VarSetOps::IsEmpty(compiler, liveSet))
    {
        return RBM_NONE;
    }

    // set if our liveSet matches the one we have cached: genLastLiveSet -> genLastLiveMask
    if (VarSetOps::Equal(compiler, liveSet, genLastLiveSet))
    {
        return genLastLiveMask;
    }

    regMaskTP liveMask = 0;

    VARSET_ITER_INIT(compiler, iter, liveSet, varIndex);
    while (iter.NextElem(compiler, &varIndex))
    {

        // If the variable is not enregistered, then it can't contribute to the liveMask
        if (!VarSetOps::IsMember(compiler, compiler->raRegVarsMask, varIndex))
        {
            continue;
        }

        // Find the variable in compiler->lvaTable
        unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
        LclVarDsc* varDsc = compiler->lvaTable + varNum;

#if !FEATURE_FP_REGALLOC
        // If the variable is a floating point type, then it can't contribute to the liveMask
        if (varDsc->IsFloatRegType())
        {
            continue;
        }
#endif

        noway_assert(compiler->lvaTable[varNum].lvRegister);
        regMaskTP regBit;

        if (varTypeIsFloating(varDsc->TypeGet()))
        {
            regBit = genRegMaskFloat(varDsc->lvRegNum, varDsc->TypeGet());
        }
        else
        {
            regBit = genRegMask(varDsc->lvRegNum);

            // For longs we may have two regs
            if (isRegPairType(varDsc->lvType) && varDsc->lvOtherReg != REG_STK)
            {
                regBit |= genRegMask(varDsc->lvOtherReg);
            }
        }

        noway_assert(regBit != 0);

        // We should not already have any of these bits set
        noway_assert((liveMask & regBit) == 0);

        // Update the liveMask with the register bits that are live
        liveMask |= regBit;
    }

    // cache the last mapping between gtLiveSet -> liveMask
    VarSetOps::Assign(compiler, genLastLiveSet, liveSet);
    genLastLiveMask = liveMask;

    return liveMask;
}

#endif

/*****************************************************************************
 *
 *  Generate a spill.
 */
void CodeGenInterface::spillReg(var_types type, TempDsc* tmp, regNumber reg)
{
    getEmitter()->emitIns_S_R(ins_Store(type), emitActualTypeSize(type), reg, tmp->tdTempNum(), 0);
}

/*****************************************************************************
 *
 *  Generate a reload.
 */
void CodeGenInterface::reloadReg(var_types type, TempDsc* tmp, regNumber reg)
{
    getEmitter()->emitIns_R_S(ins_Load(type), emitActualTypeSize(type), reg, tmp->tdTempNum(), 0);
}

#ifdef LEGACY_BACKEND
#if defined(_TARGET_ARM_) || defined(_TARGET_AMD64_)
void CodeGenInterface::reloadFloatReg(var_types type, TempDsc* tmp, regNumber reg)
{
    var_types tmpType = tmp->tdTempType();
    getEmitter()->emitIns_R_S(ins_FloatLoad(type), emitActualTypeSize(tmpType), reg, tmp->tdTempNum(), 0);
}
#endif
#endif // LEGACY_BACKEND

// inline
regNumber CodeGenInterface::genGetThisArgReg(GenTreePtr call)
{
    noway_assert(call->IsCall());
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
    RegSet::SpillDsc* spillDsc = regSet.rsGetSpillInfo(tree, tree->gtRegNum, &prevDsc);
    assert(spillDsc != nullptr);

    // Get the temp desc.
    TempDsc* temp = regSet.rsGetSpillTempWord(tree->gtRegNum, spillDsc, prevDsc);
    return temp;
}

#ifdef _TARGET_XARCH_

#ifdef _TARGET_AMD64_
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
#endif //_TARGET_AMD64_

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
#ifdef _TARGET_AMD64_
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
#ifdef _TARGET_AMD64_
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

#ifdef _TARGET_AMD64_
    // If code addr could be encoded as 32-bit offset relative to IP, we need to record a relocation.
    if (genCodeIndirAddrCanBeEncodedAsPCRelOffset(addr))
    {
        return true;
    }

    // It could be possible that the code indir addr could be encoded as 32-bit displacement relative
    // to zero.  But we don't need to emit a relocation in that case.
    return false;
#else  //_TARGET_X86_
    // On x86 there is need for recording relocations during jitting,
    // because all addrs fit within 32-bits.
    return false;
#endif //_TARGET_X86_
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

#ifdef _TARGET_AMD64_
    // By default all direct code addresses go through relocation so that VM will setup
    // a jump stub if addr cannot be encoded as pc-relative offset.
    return true;
#else  //_TARGET_X86_
    // On x86 there is no need for recording relocations during jitting,
    // because all addrs fit within 32-bits.
    return false;
#endif //_TARGET_X86_
}
#endif //_TARGET_XARCH_

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

    block->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;

    // Use coldness of current block, as this label will
    // be contained in it.
    block->bbFlags |= (compiler->compCurBB->bbFlags & BBF_COLD);

#ifdef DEBUG
    block->bbTgtStkDepth = genStackLevel / sizeof(int);
#endif
    return block;
}

// inline
void CodeGen::genDefineTempLabel(BasicBlock* label)
{
#ifdef DEBUG
    if (compiler->opts.dspCode)
    {
        printf("\n      L_M%03u_BB%02u:\n", Compiler::s_compMethodsCount, label->bbNum);
    }
#endif

    label->bbEmitCookie =
        getEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur);

    /* gcInfo.gcRegGCrefSetCur does not account for redundant load-suppression
       of GC vars, and the emitter will not know about */

    regTracker.rsTrackRegClrPtr();
}

/*****************************************************************************
 *
 *  Adjust the stack pointer by the given value; assumes that this follows
 *  a call so only callee-saved registers (and registers that may hold a
 *  return value) are used at this point.
 */

void CodeGen::genAdjustSP(ssize_t delta)
{
#ifdef _TARGET_X86_
    if (delta == sizeof(int))
        inst_RV(INS_pop, REG_ECX, TYP_INT);
    else
#endif
        inst_RV_IV(INS_add, REG_SPBASE, delta, EA_PTRSIZE);
}

#ifdef _TARGET_ARM_
// return size
// alignmentWB is out param
unsigned CodeGenInterface::InferOpSizeAlign(GenTreePtr op, unsigned* alignmentWB)
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
unsigned CodeGenInterface::InferStructOpSizeAlign(GenTreePtr op, unsigned* alignmentWB)
{
    unsigned alignment = 0;
    unsigned opSize    = 0;

    while (op->gtOper == GT_COMMA)
    {
        op = op->gtOp.gtOp2;
    }

    if (op->gtOper == GT_OBJ)
    {
        CORINFO_CLASS_HANDLE clsHnd = op->AsObj()->gtClass;
        opSize                      = compiler->info.compCompHnd->getClassSize(clsHnd);
        alignment = roundUp(compiler->info.compCompHnd->getClassAlignmentRequirement(clsHnd), TARGET_POINTER_SIZE);
    }
    else if (op->gtOper == GT_LCL_VAR)
    {
        unsigned   varNum = op->gtLclVarCommon.gtLclNum;
        LclVarDsc* varDsc = compiler->lvaTable + varNum;
        assert(varDsc->lvType == TYP_STRUCT);
        opSize = varDsc->lvSize();
        if (varDsc->lvStructDoubleAlign)
        {
            alignment = TARGET_POINTER_SIZE * 2;
        }
        else
        {
            alignment = TARGET_POINTER_SIZE;
        }
    }
    else if (op->OperIsCopyBlkOp())
    {
        GenTreePtr op2 = op->gtOp.gtOp2;

        if (op2->OperGet() == GT_CNS_INT)
        {
            if (op2->IsIconHandle(GTF_ICON_CLASS_HDL))
            {
                CORINFO_CLASS_HANDLE clsHnd = (CORINFO_CLASS_HANDLE)op2->gtIntCon.gtIconVal;
                opSize = roundUp(compiler->info.compCompHnd->getClassSize(clsHnd), TARGET_POINTER_SIZE);
                alignment =
                    roundUp(compiler->info.compCompHnd->getClassAlignmentRequirement(clsHnd), TARGET_POINTER_SIZE);
            }
            else
            {
                opSize         = op2->gtIntCon.gtIconVal;
                GenTreePtr op1 = op->gtOp.gtOp1;
                assert(op1->OperGet() == GT_LIST);
                GenTreePtr dstAddr = op1->gtOp.gtOp1;
                if (dstAddr->OperGet() == GT_ADDR)
                {
                    InferStructOpSizeAlign(dstAddr->gtOp.gtOp1, &alignment);
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
        CORINFO_CLASS_HANDLE clsHnd = op->gtArgPlace.gtArgPlaceClsHnd;
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

#endif // _TARGET_ARM_

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
 *  The 'mode' parameter may have one of the following values:
 *
 *  #if LEA_AVAILABLE
 *         +1       ...     we're trying to compute a value via 'LEA'
 *  #endif
 *
 *          0       ...     we're trying to form an address mode
 *
 *         -1       ...     we're generating code for an address mode,
 *                          and thus the address must already form an
 *                          address mode (without any further work)
 *
 *  IMPORTANT NOTE: This routine doesn't generate any code, it merely
 *                  identifies the components that might be used to
 *                  form an address mode later on.
 */

bool CodeGen::genCreateAddrMode(GenTreePtr  addr,
                                int         mode,
                                bool        fold,
                                regMaskTP   regMask,
                                bool*       revPtr,
                                GenTreePtr* rv1Ptr,
                                GenTreePtr* rv2Ptr,
#if SCALED_ADDR_MODES
                                unsigned* mulPtr,
#endif
                                unsigned* cnsPtr,
                                bool      nogen)
{
#ifndef LEGACY_BACKEND
    assert(nogen == true);
#endif // !LEGACY_BACKEND

    /*
        The following indirections are valid address modes on x86/x64:

            [                  icon]      * not handled here
            [reg                   ]      * not handled here
            [reg             + icon]
            [reg2 +     reg1       ]
            [reg2 +     reg1 + icon]
            [reg2 + 2 * reg1       ]
            [reg2 + 4 * reg1       ]
            [reg2 + 8 * reg1       ]
            [       2 * reg1 + icon]
            [       4 * reg1 + icon]
            [       8 * reg1 + icon]
            [reg2 + 2 * reg1 + icon]
            [reg2 + 4 * reg1 + icon]
            [reg2 + 8 * reg1 + icon]

        The following indirections are valid address modes on arm64:

            [reg]
            [reg  + icon]
            [reg2 + reg1]
            [reg2 + reg1 * natural-scale]

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

    GenTreePtr rv1 = nullptr;
    GenTreePtr rv2 = nullptr;

    GenTreePtr op1;
    GenTreePtr op2;

    ssize_t cns;
#if SCALED_ADDR_MODES
    unsigned mul;
#endif

    GenTreePtr tmp;

    /* What order are the sub-operands to be evaluated */

    if (addr->gtFlags & GTF_REVERSE_OPS)
    {
        op1 = addr->gtOp.gtOp2;
        op2 = addr->gtOp.gtOp1;
    }
    else
    {
        op1 = addr->gtOp.gtOp1;
        op2 = addr->gtOp.gtOp2;
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
#endif

AGAIN:
    /* We come back to 'AGAIN' if we have an add of a constant, and we are folding that
       constant, or we have gone through a GT_NOP or GT_COMMA node. We never come back
       here if we find a scaled index.
    */
    CLANG_FORMAT_COMMENT_ANCHOR;

#if SCALED_ADDR_MODES
    assert(mul == 0);
#endif

#ifdef LEGACY_BACKEND
    /* Check both operands as far as being register variables */

    if (mode != -1)
    {
        if (op1->gtOper == GT_LCL_VAR)
            genMarkLclVar(op1);
        if (op2->gtOper == GT_LCL_VAR)
            genMarkLclVar(op2);
    }
#endif // LEGACY_BACKEND

    /* Special case: keep constants as 'op2' */

    if (op1->IsCnsIntOrI())
    {
        // Presumably op2 is assumed to not be a constant (shouldn't happen if we've done constant folding)?
        tmp = op1;
        op1 = op2;
        op2 = tmp;
    }

    /* Check for an addition of a constant */

    if (op2->IsIntCnsFitsInI32() && (op2->gtType != TYP_REF) && FitsIn<INT32>(cns + op2->gtIntConCommon.IconValue()))
    {
        /* We're adding a constant */

        cns += op2->gtIntConCommon.IconValue();

#ifdef LEGACY_BACKEND
        /* Can (and should) we use "add reg, icon" ? */

        if ((op1->gtFlags & GTF_REG_VAL) && mode == 1 && !nogen)
        {
            regNumber reg1 = op1->gtRegNum;

            if ((regMask == 0 || (regMask & genRegMask(reg1))) && genRegTrashable(reg1, addr))
            {
                // In case genMarkLclVar(op1) bashed it above and it is
                // the last use of the variable.

                genUpdateLife(op1);

                /* 'reg1' is trashable, so add "icon" into it */

                genIncRegBy(reg1, cns, addr, addr->TypeGet());

                genUpdateLife(addr);
                return true;
            }
        }
#endif // LEGACY_BACKEND

#ifdef _TARGET_ARM64_
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

                    op2 = op1->gtOp.gtOp2;
                    op1 = op1->gtOp.gtOp1;

                    goto AGAIN;

#if SCALED_ADDR_MODES && !defined(_TARGET_ARM64_)
                // TODO-ARM64-CQ: For now we don't try to create a scaled index on ARM64.
                case GT_MUL:
                    if (op1->gtOverflow())
                    {
                        return false; // Need overflow check
                    }

                    __fallthrough;

                case GT_LSH:

                    mul = op1->GetScaledIndex();
                    if (mul)
                    {
                        /* We can use "[mul*rv2 + icon]" */

                        rv1 = nullptr;
                        rv2 = op1->gtOp.gtOp1;

                        goto FOUND_AM;
                    }
                    break;
#endif

                default:
                    break;
            }
        }

        /* The best we can do is "[rv1 + icon]" */

        rv1 = op1;
        rv2 = nullptr;

        goto FOUND_AM;
    }

    /* op2 is not a constant. So keep on trying.
       Does op1 or op2 already sit in a register? */

    if (op1->gtFlags & GTF_REG_VAL)
    {
        /* op1 is sitting in a register */
    }
    else if (op2->gtFlags & GTF_REG_VAL)
    {
        /* op2 is sitting in a register. Keep the enregistered value as op1 */

        tmp = op1;
        op1 = op2;
        op2 = tmp;

        noway_assert(rev == false);
        rev = true;
    }
    else
    {
        /* Neither op1 nor op2 are sitting in a register right now */

        switch (op1->gtOper)
        {
#ifndef _TARGET_ARM64_
            // TODO-ARM64-CQ: For now we don't try to create a scaled index on ARM64.
            case GT_ADD:

                if (op1->gtOverflow())
                {
                    break;
                }

                if (op1->gtOp.gtOp2->IsIntCnsFitsInI32() && FitsIn<INT32>(cns + op1->gtOp.gtOp2->gtIntCon.gtIconVal))
                {
                    cns += op1->gtOp.gtOp2->gtIntCon.gtIconVal;
                    op1 = op1->gtOp.gtOp1;

                    goto AGAIN;
                }

                break;

#if SCALED_ADDR_MODES

            case GT_MUL:

                if (op1->gtOverflow())
                {
                    break;
                }

                __fallthrough;

            case GT_LSH:

                mul = op1->GetScaledIndex();
                if (mul)
                {
                    /* 'op1' is a scaled value */

                    rv1 = op2;
                    rv2 = op1->gtOp.gtOp1;

                    int argScale;
                    while ((rv2->gtOper == GT_MUL || rv2->gtOper == GT_LSH) && (argScale = rv2->GetScaledIndex()) != 0)
                    {
                        if (jitIsScaleIndexMul(argScale * mul))
                        {
                            mul = mul * argScale;
                            rv2 = rv2->gtOp.gtOp1;
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
#endif // !_TARGET_ARM64_

            case GT_NOP:

                if (!nogen)
                {
                    break;
                }

                op1 = op1->gtOp.gtOp1;
                goto AGAIN;

            case GT_COMMA:

                if (!nogen)
                {
                    break;
                }

                op1 = op1->gtOp.gtOp2;
                goto AGAIN;

            default:
                break;
        }

        noway_assert(op2);
        switch (op2->gtOper)
        {
#ifndef _TARGET_ARM64_
            // TODO-ARM64-CQ: For now we don't try to create a scaled index on ARM64.
            case GT_ADD:

                if (op2->gtOverflow())
                {
                    break;
                }

                if (op2->gtOp.gtOp2->IsIntCnsFitsInI32() && FitsIn<INT32>(cns + op2->gtOp.gtOp2->gtIntCon.gtIconVal))
                {
                    cns += op2->gtOp.gtOp2->gtIntCon.gtIconVal;
                    op2 = op2->gtOp.gtOp1;

                    goto AGAIN;
                }

                break;

#if SCALED_ADDR_MODES

            case GT_MUL:

                if (op2->gtOverflow())
                {
                    break;
                }

                __fallthrough;

            case GT_LSH:

                mul = op2->GetScaledIndex();
                if (mul)
                {
                    // 'op2' is a scaled value...is it's argument also scaled?
                    int argScale;
                    rv2 = op2->gtOp.gtOp1;
                    while ((rv2->gtOper == GT_MUL || rv2->gtOper == GT_LSH) && (argScale = rv2->GetScaledIndex()) != 0)
                    {
                        if (jitIsScaleIndexMul(argScale * mul))
                        {
                            mul = mul * argScale;
                            rv2 = rv2->gtOp.gtOp1;
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
#endif // !_TARGET_ARM64_

            case GT_NOP:

                if (!nogen)
                {
                    break;
                }

                op2 = op2->gtOp.gtOp1;
                goto AGAIN;

            case GT_COMMA:

                if (!nogen)
                {
                    break;
                }

                op2 = op2->gtOp.gtOp2;
                goto AGAIN;

            default:
                break;
        }

        goto ADD_OP12;
    }

    /* op1 is in a register.
       Is op2 an addition or a scaled value? */

    noway_assert(op2);

#ifndef _TARGET_ARM64_
    // TODO-ARM64-CQ: For now we don't try to create a scaled index on ARM64.
    switch (op2->gtOper)
    {
        case GT_ADD:

            if (op2->gtOverflow())
            {
                break;
            }

            if (op2->gtOp.gtOp2->IsIntCnsFitsInI32() && FitsIn<INT32>(cns + op2->gtOp.gtOp2->gtIntCon.gtIconVal))
            {
                cns += op2->gtOp.gtOp2->gtIntCon.gtIconVal;
                op2 = op2->gtOp.gtOp1;
                goto AGAIN;
            }

            break;

#if SCALED_ADDR_MODES

        case GT_MUL:

            if (op2->gtOverflow())
            {
                break;
            }

            __fallthrough;

        case GT_LSH:

            mul = op2->GetScaledIndex();
            if (mul)
            {
                rv1 = op1;
                rv2 = op2->gtOp.gtOp1;
                int argScale;
                while ((rv2->gtOper == GT_MUL || rv2->gtOper == GT_LSH) && (argScale = rv2->GetScaledIndex()) != 0)
                {
                    if (jitIsScaleIndexMul(argScale * mul))
                    {
                        mul = mul * argScale;
                        rv2 = rv2->gtOp.gtOp1;
                    }
                    else
                    {
                        break;
                    }
                }

                goto FOUND_AM;
            }
            break;

#endif // SCALED_ADDR_MODES

        default:
            break;
    }
#endif // !_TARGET_ARM64_

ADD_OP12:

    /* The best we can do "[rv1 + rv2]" or "[rv1 + rv2 + cns]" */

    rv1 = op1;
    rv2 = op2;
#ifdef _TARGET_ARM64_
    assert(cns == 0);
#endif

FOUND_AM:

#ifdef LEGACY_BACKEND
    /* Check for register variables */

    if (mode != -1)
    {
        if (rv1 && rv1->gtOper == GT_LCL_VAR)
            genMarkLclVar(rv1);
        if (rv2 && rv2->gtOper == GT_LCL_VAR)
            genMarkLclVar(rv2);
    }
#endif // LEGACY_BACKEND

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
            ssize_t    tmpMul;
            GenTreePtr index;

            if ((rv2->gtOper == GT_MUL || rv2->gtOper == GT_LSH) && (rv2->gtOp.gtOp2->IsCnsIntOrI()))
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

    /* Success - return the various components to the caller */

    *revPtr = rev;
    *rv1Ptr = rv1;
    *rv2Ptr = rv2;
#if SCALED_ADDR_MODES
    *mulPtr = mul;
#endif
    *cnsPtr = (unsigned)cns;

    return true;
}

/*****************************************************************************
*  The condition to use for (the jmp/set for) the given type of operation
*
*  In case of amd64, this routine should be used when there is no gentree available
*  and one needs to generate jumps based on integer comparisons.  When gentree is
*  available always use its overloaded version.
*
*/

// static
emitJumpKind CodeGen::genJumpKindForOper(genTreeOps cmp, CompareKind compareKind)
{
    const static BYTE genJCCinsSigned[] = {
#if defined(_TARGET_XARCH_)
        EJ_je,  // GT_EQ
        EJ_jne, // GT_NE
        EJ_jl,  // GT_LT
        EJ_jle, // GT_LE
        EJ_jge, // GT_GE
        EJ_jg,  // GT_GT
#elif defined(_TARGET_ARMARCH_)
        EJ_eq,   // GT_EQ
        EJ_ne,   // GT_NE
        EJ_lt,   // GT_LT
        EJ_le,   // GT_LE
        EJ_ge,   // GT_GE
        EJ_gt,   // GT_GT
#endif
    };

    const static BYTE genJCCinsUnsigned[] = /* unsigned comparison */
    {
#if defined(_TARGET_XARCH_)
        EJ_je,  // GT_EQ
        EJ_jne, // GT_NE
        EJ_jb,  // GT_LT
        EJ_jbe, // GT_LE
        EJ_jae, // GT_GE
        EJ_ja,  // GT_GT
#elif defined(_TARGET_ARMARCH_)
        EJ_eq,   // GT_EQ
        EJ_ne,   // GT_NE
        EJ_lo,   // GT_LT
        EJ_ls,   // GT_LE
        EJ_hs,   // GT_GE
        EJ_hi,   // GT_GT
#endif
    };

    const static BYTE genJCCinsLogical[] = /* logical operation */
    {
#if defined(_TARGET_XARCH_)
        EJ_je,   // GT_EQ   (Z == 1)
        EJ_jne,  // GT_NE   (Z == 0)
        EJ_js,   // GT_LT   (S == 1)
        EJ_NONE, // GT_LE
        EJ_jns,  // GT_GE   (S == 0)
        EJ_NONE, // GT_GT
#elif defined(_TARGET_ARMARCH_)
        EJ_eq,   // GT_EQ   (Z == 1)
        EJ_ne,   // GT_NE   (Z == 0)
        EJ_mi,   // GT_LT   (N == 1)
        EJ_NONE, // GT_LE
        EJ_pl,   // GT_GE   (N == 0)
        EJ_NONE, // GT_GT
#endif
    };

#if defined(_TARGET_XARCH_)
    assert(genJCCinsSigned[GT_EQ - GT_EQ] == EJ_je);
    assert(genJCCinsSigned[GT_NE - GT_EQ] == EJ_jne);
    assert(genJCCinsSigned[GT_LT - GT_EQ] == EJ_jl);
    assert(genJCCinsSigned[GT_LE - GT_EQ] == EJ_jle);
    assert(genJCCinsSigned[GT_GE - GT_EQ] == EJ_jge);
    assert(genJCCinsSigned[GT_GT - GT_EQ] == EJ_jg);

    assert(genJCCinsUnsigned[GT_EQ - GT_EQ] == EJ_je);
    assert(genJCCinsUnsigned[GT_NE - GT_EQ] == EJ_jne);
    assert(genJCCinsUnsigned[GT_LT - GT_EQ] == EJ_jb);
    assert(genJCCinsUnsigned[GT_LE - GT_EQ] == EJ_jbe);
    assert(genJCCinsUnsigned[GT_GE - GT_EQ] == EJ_jae);
    assert(genJCCinsUnsigned[GT_GT - GT_EQ] == EJ_ja);

    assert(genJCCinsLogical[GT_EQ - GT_EQ] == EJ_je);
    assert(genJCCinsLogical[GT_NE - GT_EQ] == EJ_jne);
    assert(genJCCinsLogical[GT_LT - GT_EQ] == EJ_js);
    assert(genJCCinsLogical[GT_GE - GT_EQ] == EJ_jns);
#elif defined(_TARGET_ARMARCH_)
    assert(genJCCinsSigned[GT_EQ - GT_EQ] == EJ_eq);
    assert(genJCCinsSigned[GT_NE - GT_EQ] == EJ_ne);
    assert(genJCCinsSigned[GT_LT - GT_EQ] == EJ_lt);
    assert(genJCCinsSigned[GT_LE - GT_EQ] == EJ_le);
    assert(genJCCinsSigned[GT_GE - GT_EQ] == EJ_ge);
    assert(genJCCinsSigned[GT_GT - GT_EQ] == EJ_gt);

    assert(genJCCinsUnsigned[GT_EQ - GT_EQ] == EJ_eq);
    assert(genJCCinsUnsigned[GT_NE - GT_EQ] == EJ_ne);
    assert(genJCCinsUnsigned[GT_LT - GT_EQ] == EJ_lo);
    assert(genJCCinsUnsigned[GT_LE - GT_EQ] == EJ_ls);
    assert(genJCCinsUnsigned[GT_GE - GT_EQ] == EJ_hs);
    assert(genJCCinsUnsigned[GT_GT - GT_EQ] == EJ_hi);

    assert(genJCCinsLogical[GT_EQ - GT_EQ] == EJ_eq);
    assert(genJCCinsLogical[GT_NE - GT_EQ] == EJ_ne);
    assert(genJCCinsLogical[GT_LT - GT_EQ] == EJ_mi);
    assert(genJCCinsLogical[GT_GE - GT_EQ] == EJ_pl);
#else
    assert(!"unknown arch");
#endif
    assert(GenTree::OperIsCompare(cmp));

    emitJumpKind result = EJ_COUNT;

    if (compareKind == CK_UNSIGNED)
    {
        result = (emitJumpKind)genJCCinsUnsigned[cmp - GT_EQ];
    }
    else if (compareKind == CK_SIGNED)
    {
        result = (emitJumpKind)genJCCinsSigned[cmp - GT_EQ];
    }
    else if (compareKind == CK_LOGICAL)
    {
        result = (emitJumpKind)genJCCinsLogical[cmp - GT_EQ];
    }
    assert(result != EJ_COUNT);
    return result;
}

/*****************************************************************************
 *
 *  Generate an exit sequence for a return from a method (note: when compiling
 *  for speed there might be multiple exit points).
 */

void CodeGen::genExitCode(BasicBlock* block)
{
#ifdef DEBUGGING_SUPPORT
    /* Just wrote the first instruction of the epilog - inform debugger
       Note that this may result in a duplicate IPmapping entry, and
       that this is ok  */

    // For non-optimized debuggable code, there is only one epilog.
    genIPmappingAdd((IL_OFFSETX)ICorDebugInfo::EPILOG, true);
#endif // DEBUGGING_SUPPORT

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

                gcInfo.gcMarkRegPtrVal(varDsc->lvArgReg, varDsc->TypeGet());
            }

            getEmitter()->emitThisGCrefRegs = getEmitter()->emitInitGCrefRegs = gcInfo.gcRegGCrefSetCur;
            getEmitter()->emitThisByrefRegs = getEmitter()->emitInitByrefRegs = gcInfo.gcRegByrefSetCur;
        }
    }

    genReserveEpilog(block);
}

/*****************************************************************************
 *
 * Generate code for an out-of-line exception.
 * For debuggable code, we generate the 'throw' inline.
 * For non-dbg code, we share the helper blocks created by fgAddCodeRef().
 */

void CodeGen::genJumpToThrowHlpBlk(emitJumpKind jumpKind, SpecialCodeKind codeKind, GenTreePtr failBlk)
{
    if (!compiler->opts.compDbgCode)
    {
        /* For non-debuggable code, find and use the helper block for
           raising the exception. The block may be shared by other trees too. */

        BasicBlock* tgtBlk;

        if (failBlk)
        {
            /* We already know which block to jump to. Use that. */

            noway_assert(failBlk->gtOper == GT_LABEL);
            tgtBlk = failBlk->gtLabel.gtLabBB;
            noway_assert(
                tgtBlk ==
                compiler->fgFindExcptnTarget(codeKind, compiler->bbThrowIndex(compiler->compCurBB))->acdDstBlk);
        }
        else
        {
            /* Find the helper-block which raises the exception. */

            Compiler::AddCodeDsc* add =
                compiler->fgFindExcptnTarget(codeKind, compiler->bbThrowIndex(compiler->compCurBB));
            PREFIX_ASSUME_MSG((add != nullptr), ("ERROR: failed to find exception throw block"));
            tgtBlk = add->acdDstBlk;
        }

        noway_assert(tgtBlk);

        // Jump to the excption-throwing block on error.

        inst_JMP(jumpKind, tgtBlk);
    }
    else
    {
        /* The code to throw the exception will be generated inline, and
           we will jump around it in the normal non-exception case */

        BasicBlock*  tgtBlk          = nullptr;
        emitJumpKind reverseJumpKind = emitter::emitReverseJumpKind(jumpKind);
        if (reverseJumpKind != jumpKind)
        {
            tgtBlk = genCreateTempLabel();
            inst_JMP(reverseJumpKind, tgtBlk);
        }

        genEmitHelperCall(compiler->acdHelper(codeKind), 0, EA_UNKNOWN);

        /* Define the spot for the normal non-exception case to jump to */
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
void CodeGen::genCheckOverflow(GenTreePtr tree)
{
    // Overflow-check should be asked for this tree
    noway_assert(tree->gtOverflow());

    const var_types type = tree->TypeGet();

    // Overflow checks can only occur for the non-small types: (i.e. TYP_INT,TYP_LONG)
    noway_assert(!varTypeIsSmall(type));

    emitJumpKind jumpKind;

#ifdef _TARGET_ARM64_
    if (tree->OperGet() == GT_MUL)
    {
        jumpKind = EJ_ne;
    }
    else
#endif
    {
        bool isUnsignedOverflow = ((tree->gtFlags & GTF_UNSIGNED) != 0);

#if defined(_TARGET_XARCH_)

        jumpKind = isUnsignedOverflow ? EJ_jb : EJ_jo;

#elif defined(_TARGET_ARMARCH_)

        jumpKind = isUnsignedOverflow ? EJ_lo : EJ_vs;

        if (jumpKind == EJ_lo)
        {
            if ((tree->OperGet() != GT_SUB) && (tree->gtOper != GT_ASG_SUB))
            {
                jumpKind = EJ_hs;
            }
        }

#endif // defined(_TARGET_ARMARCH_)
    }

    // Jump to the block which will throw the expection

    genJumpToThrowHlpBlk(jumpKind, SCK_OVERFLOW);
}

#if FEATURE_EH_FUNCLETS

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
#endif // FEATURE_EH_FUNCLETS

/*****************************************************************************
 *
 *  Generate code for the function.
 */

void CodeGen::genGenerateCode(void** codePtr, ULONG* nativeSizeOfCode)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genGenerateCode()\n");
        compiler->fgDispBasicBlocks(compiler->verboseTrees);
    }
#endif

    unsigned codeSize;
    unsigned prologSize;
    unsigned epilogSize;

    void* consPtr;

#ifdef DEBUG
    genInterruptibleUsed = true;

#if STACK_PROBES
    genNeedPrologStackProbe = false;
#endif

    compiler->fgDebugCheckBBlist();
#endif // DEBUG

    /* This is the real thing */

    genPrepForCompiler();

    /* Prepare the emitter */
    getEmitter()->Init();
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
            if (compiler->canUseAVX())
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

        printf("\n");

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
            printf("; compiler->opts.MinOpts() is true\n");
        }
        else
        {
            printf("; unknown optimization flags\n");
        }

#if DOUBLE_ALIGN
        if (compiler->genDoubleAlign())
            printf("; double-aligned frame\n");
        else
#endif
            printf("; %s based frame\n", isFramePointerUsed() ? STR_FPBASE : STR_SPBASE);

        if (genInterruptible)
        {
            printf("; fully interruptible\n");
        }
        else
        {
            printf("; partially interruptible\n");
        }

        if (compiler->fgHaveProfileData())
        {
            printf("; with IBC profile data\n");
        }

        if (compiler->fgProfileData_ILSizeMismatch)
        {
            printf("; discarded IBC profile data due to mismatch in ILSize\n");
        }
    }
#endif // DEBUG

#ifndef LEGACY_BACKEND

    // For RyuJIT backend, we compute the final frame layout before code generation. This is because LSRA
    // has already computed exactly the maximum concurrent number of spill temps of each type that are
    // required during code generation. So, there is nothing left to estimate: we can be precise in the frame
    // layout. This helps us generate smaller code, and allocate, after code generation, a smaller amount of
    // memory from the VM.

    genFinalizeFrame();

    unsigned maxTmpSize = compiler->tmpSize; // This is precise after LSRA has pre-allocated the temps.

#else // LEGACY_BACKEND

    // Estimate the frame size: first, estimate the number of spill temps needed by taking the register
    // predictor spill temp estimates and stress levels into consideration. Then, compute the tentative
    // frame layout using conservative callee-save register estimation (namely, guess they'll all be used
    // and thus saved on the frame).

    // Compute the maximum estimated spill temp size.
    unsigned maxTmpSize = sizeof(double) + sizeof(float) + sizeof(__int64) + sizeof(void*);

    maxTmpSize += (compiler->tmpDoubleSpillMax * sizeof(double)) + (compiler->tmpIntSpillMax * sizeof(int));

#ifdef DEBUG

    /* When StressRegs is >=1, there will be a bunch of spills not predicted by
       the predictor (see logic in rsPickReg).  It will be very hard to teach
       the predictor about the behavior of rsPickReg for StressRegs >= 1, so
       instead let's make maxTmpSize large enough so that we won't be wrong.
       This means that at StressRegs >= 1, we will not be testing the logic
       that sets the maxTmpSize size.
    */

    if (regSet.rsStressRegs() >= 1)
    {
        maxTmpSize += (REG_TMP_ORDER_COUNT * REGSIZE_BYTES);
    }

    // JIT uses 2 passes when assigning stack variable (i.e. args, temps, and locals) locations in varDsc->lvStkOffs.
    // During the 1st pass (in genGenerateCode), it estimates the maximum possible size for stack temps
    // and put it in maxTmpSize. Then it calculates the varDsc->lvStkOffs for each variable based on this estimation.
    // However during stress mode, we might spill more temps on the stack, which might grow the
    // size of the temp area.
    // This might cause varDsc->lvStkOffs to change during the 2nd pass (in emitEndCodeGen).
    // If the change of varDsc->lvStkOffs crosses the threshold for the instruction size,
    // we will then have a mismatched estimated code size (during the 1st pass) and the actual emitted code size
    // (during the 2nd pass).
    // Also, if STRESS_UNSAFE_BUFFER_CHECKS is turned on, we might reorder the stack variable locations,
    // which could cause the mismatch too.
    //
    // The following code is simply bump the maxTmpSize up to at least BYTE_MAX+1 during the stress mode, so that
    // we don't run into code size problem during stress.

    if (getJitStressLevel() != 0)
    {
        if (maxTmpSize < BYTE_MAX + 1)
        {
            maxTmpSize = BYTE_MAX + 1;
        }
    }
#endif // DEBUG

    /* Estimate the offsets of locals/arguments and size of frame */

    unsigned lclSize = compiler->lvaFrameSize(Compiler::TENTATIVE_FRAME_LAYOUT);

#ifdef DEBUG
    //
    // Display the local frame offsets that we have tentatively decided upon
    //
    if (verbose)
    {
        compiler->lvaTableDump();
    }
#endif // DEBUG

#endif // LEGACY_BACKEND

    getEmitter()->emitBegFN(isFramePointerUsed()
#if defined(DEBUG)
                                ,
                            (compiler->compCodeOpt() != Compiler::SMALL_CODE) &&
                                !(compiler->opts.eeFlags & CORJIT_FLG_PREJIT)
#endif
#ifdef LEGACY_BACKEND
                                ,
                            lclSize
#endif // LEGACY_BACKEND
                            ,
                            maxTmpSize);

    /* Now generate code for the function */
    genCodeForBBlist();

#ifndef LEGACY_BACKEND
#ifdef DEBUG
    // After code generation, dump the frame layout again. It should be the same as before code generation, if code
    // generation hasn't touched it (it shouldn't!).
    if (verbose)
    {
        compiler->lvaTableDump();
    }
#endif // DEBUG
#endif // !LEGACY_BACKEND

    /* We can now generate the function prolog and epilog */

    genGeneratePrologsAndEpilogs();

    /* Bind jump distances */

    getEmitter()->emitJumpDistBind();

    /* The code is now complete and final; it should not change after this. */

    /* Compute the size of the code sections that we are going to ask the VM
       to allocate. Note that this might not be precisely the size of the
       code we emit, though it's fatal if we emit more code than the size we
       compute here.
       (Note: an example of a case where we emit less code would be useful.)
    */

    getEmitter()->emitComputeCodeSizes();

#ifdef DEBUG

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
                NO_WAY_NOASSERT("Stress failure");
            }
        }
    }

#endif // DEBUG

    /* We've finished collecting all the unwind information for the function. Now reserve
       space for it from the VM.
    */

    compiler->unwindReserve();

#if DISPLAY_SIZES

    size_t dataSize = getEmitter()->emitDataSize();

#endif // DISPLAY_SIZES

    void* coldCodePtr;

    bool trackedStackPtrsContig; // are tracked stk-ptrs contiguous ?

#ifdef _TARGET_AMD64_
    trackedStackPtrsContig = false;
#elif defined(_TARGET_ARM_)
    // On arm due to prespilling of arguments, tracked stk-ptrs may not be contiguous
    trackedStackPtrsContig = !compiler->opts.compDbgEnC && !compiler->compIsProfilerHookNeeded();
#elif defined(_TARGET_ARM64_)
    // Incoming vararg registers are homed on the top of the stack. Tracked var may not be contiguous.
    trackedStackPtrsContig = !compiler->opts.compDbgEnC && !compiler->info.compIsVarArgs;
#else
    trackedStackPtrsContig = !compiler->opts.compDbgEnC;
#endif

#ifdef DEBUG
    /* We're done generating code for this function */
    compiler->compCodeGenDone = true;
#endif

    compiler->EndPhase(PHASE_GENERATE_CODE);

    codeSize = getEmitter()->emitEndCodeGen(compiler, trackedStackPtrsContig, genInterruptible, genFullPtrRegMap,
                                            (compiler->info.compRetType == TYP_REF), compiler->compHndBBtabCount,
                                            &prologSize, &epilogSize, codePtr, &coldCodePtr, &consPtr);

    compiler->EndPhase(PHASE_EMIT_CODE);

#ifdef DEBUG
    if (compiler->opts.disAsm)
    {
        printf("; Total bytes of code %d, prolog size %d for method %s\n", codeSize, prologSize,
               compiler->info.compFullName);
        printf("; ============================================================\n");
        printf(""); // in our logic this causes a flush
    }

    if (verbose)
    {
        printf("*************** After end code gen, before unwindEmit()\n");
        getEmitter()->emitDispIGlist(true);
    }
#endif

#if EMIT_TRACK_STACK_DEPTH
    /* Check our max stack level. Needed for fgAddCodeRef().
       We need to relax the assert as our estimation won't include code-gen
       stack changes (which we know don't affect fgAddCodeRef()) */
    noway_assert(getEmitter()->emitMaxStackDepth <=
                 (compiler->fgPtrArgCntMax + compiler->compHndBBtabCount + // Return address for locally-called finallys
                  genTypeStSz(TYP_LONG) +                 // longs/doubles may be transferred via stack, etc
                  (compiler->compTailCallUsed ? 4 : 0))); // CORINFO_HELP_TAILCALL args
#endif

    *nativeSizeOfCode                 = codeSize;
    compiler->info.compNativeCodeSize = (UNATIVE_OFFSET)codeSize;

    // printf("%6u bytes of code generated for %s.%s\n", codeSize, compiler->info.compFullName);

    // Make sure that the x86 alignment and cache prefetch optimization rules
    // were obeyed.

    // Don't start a method in the last 7 bytes of a 16-byte alignment area
    //   unless we are generating SMALL_CODE
    // noway_assert( (((unsigned)(*codePtr) % 16) <= 8) || (compiler->compCodeOpt() == SMALL_CODE));

    /* Now that the code is issued, we can finalize and emit the unwind data */

    compiler->unwindEmit(*codePtr, coldCodePtr);

#ifdef DEBUGGING_SUPPORT

    /* Finalize the line # tracking logic after we know the exact block sizes/offsets */

    genIPmappingGen();

    /* Finalize the Local Var info in terms of generated code */

    genSetScopeInfo();

#endif // DEBUGGING_SUPPORT

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
        size_t consSize = getEmitter()->emitDataSize();
        size_t infoSize = compiler->compInfoBlkSize;

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

    getEmitter()->emitEndFN();

    /* Shut down the spill logic */

    regSet.rsSpillDone();

    /* Shut down the temp logic */

    compiler->tmpDone();

#if DISPLAY_SIZES

    grossVMsize += compiler->info.compILCodeSize;
    totalNCsize += codeSize + dataSize + compiler->compInfoBlkSize;
    grossNCsize += codeSize + dataSize;

#endif // DISPLAY_SIZES

    compiler->EndPhase(PHASE_EMIT_GCEH);
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

    unsigned EHCount = compiler->compHndBBtabCount;

#if FEATURE_EH_FUNCLETS
    // Count duplicated clauses. This uses the same logic as below, where we actually generate them for reporting to the
    // VM.
    unsigned duplicateClauseCount = 0;
    unsigned enclosingTryIndex;
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

#if FEATURE_EH_CALLFINALLY_THUNKS
    unsigned clonedFinallyCount = 0;

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
#endif // FEATURE_EH_CALLFINALLY_THUNKS

#endif // FEATURE_EH_FUNCLETS

#ifdef DEBUG
    if (compiler->opts.dspEHTable)
    {
#if FEATURE_EH_FUNCLETS
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

#if FEATURE_EH_FUNCLETS
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
                // It seems weird this is from the CorExceptionFlag enum in corhdr.h,
                // not the CORINFO_EH_CLAUSE_FLAGS enum in corinfo.h.
                flags = (CORINFO_EH_CLAUSE_FLAGS)(flags | COR_ILEXCEPTION_CLAUSE_DUPLICATED);

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
    if (anyFinallys)
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
                clause.Flags = (CORINFO_EH_CLAUSE_FLAGS)(CORINFO_EH_CLAUSE_FINALLY | COR_ILEXCEPTION_CLAUSE_DUPLICATED);
                clause.TryOffset     = hndBeg;
                clause.TryLength     = hndBeg;
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
    }  // if (anyFinallys)
#endif // FEATURE_EH_CALLFINALLY_THUNKS

#endif // FEATURE_EH_FUNCLETS

    assert(XTnum == EHCount);
}

void CodeGen::genGCWriteBarrier(GenTreePtr tgt, GCInfo::WriteBarrierForm wbf)
{
#ifndef LEGACY_BACKEND
    noway_assert(tgt->gtOper == GT_STOREIND);
#else  // LEGACY_BACKEND
    noway_assert(tgt->gtOper == GT_IND || tgt->gtOper == GT_CLS_VAR); // enforced by gcIsWriteBarrierCandidate
#endif // LEGACY_BACKEND

    /* Call the proper vm helper */
    int helper = CORINFO_HELP_ASSIGN_REF;
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
            else if (tgt->gtOp.gtOp1->TypeGet() == TYP_I_IMPL)
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

#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
    // We classify the "tgt" trees as follows:
    // If "tgt" is of the form (where [ x ] indicates an optional x, and { x1, ..., xn } means "one of the x_i forms"):
    //    IND [-> ADDR -> IND] -> { GT_LCL_VAR, GT_REG_VAR, ADD({GT_LCL_VAR, GT_REG_VAR}, X), ADD(X, (GT_LCL_VAR,
    //    GT_REG_VAR)) }
    // then let "v" be the GT_LCL_VAR or GT_REG_VAR.
    //   * If "v" is the return buffer argument, classify as CWBKind_RetBuf.
    //   * If "v" is another by-ref argument, classify as CWBKind_ByRefArg.
    //   * Otherwise, classify as CWBKind_OtherByRefLocal.
    // If "tgt" is of the form IND -> ADDR -> GT_LCL_VAR, clasify as CWBKind_AddrOfLocal.
    // Otherwise, classify as CWBKind_Unclassified.

    CheckedWriteBarrierKinds wbKind = CWBKind_Unclassified;
    if (tgt->gtOper == GT_IND)
    {
        GenTreePtr lcl = NULL;

        GenTreePtr indArg = tgt->gtOp.gtOp1;
        if (indArg->gtOper == GT_ADDR && indArg->gtOp.gtOp1->gtOper == GT_IND)
        {
            indArg = indArg->gtOp.gtOp1->gtOp.gtOp1;
        }
        if (indArg->gtOper == GT_LCL_VAR || indArg->gtOper == GT_REG_VAR)
        {
            lcl = indArg;
        }
        else if (indArg->gtOper == GT_ADD)
        {
            if (indArg->gtOp.gtOp1->gtOper == GT_LCL_VAR || indArg->gtOp.gtOp1->gtOper == GT_REG_VAR)
            {
                lcl = indArg->gtOp.gtOp1;
            }
            else if (indArg->gtOp.gtOp2->gtOper == GT_LCL_VAR || indArg->gtOp.gtOp2->gtOper == GT_REG_VAR)
            {
                lcl = indArg->gtOp.gtOp2;
            }
        }
        if (lcl != NULL)
        {
            wbKind          = CWBKind_OtherByRefLocal; // Unclassified local variable.
            unsigned lclNum = 0;
            if (lcl->gtOper == GT_LCL_VAR)
                lclNum = lcl->gtLclVarCommon.gtLclNum;
            else
            {
                assert(lcl->gtOper == GT_REG_VAR);
                lclNum = lcl->gtRegVar.gtLclNum;
            }
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
            assert(!(indArg->gtOper == GT_ADDR && indArg->gtOp.gtOp1->gtOper == GT_LCL_VAR));
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
        genStackLevel += 4;
        inst_IV(INS_push, wbKind);
        genEmitHelperCall(helper,
                          4,           // argSize
                          EA_PTRSIZE); // retSize
        genStackLevel -= 4;
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

#ifdef _TARGET_ARM64_
    if (compiler->info.compIsVarArgs)
    {
        // We've already saved all int registers at the top of stack in the prolog.
        // No need further action.
        return;
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
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        var_types type;   // the Jit type of this regArgTab entry
#endif                    // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        unsigned trashBy; // index into this regArgTab[] table of the register that will be copied to this register.
                          // That is, for regArgTab[x].trashBy = y, argument register number 'y' will be copied to
                          // argument register number 'x'. Only used when circular = true.
        char slot;        // 0 means the register is not used for a register argument
                          // 1 means the first part of a register argument
                          // 2, 3 or 4  means the second,third or fourth part of a multireg argument
        bool stackArg;    // true if the argument gets homed to the stack
        bool processed;   // true after we've processed the argument (and it is in its final location)
        bool circular;    // true if this register participates in a circular dependency loop.

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING

        // For UNIX AMD64 struct passing, the type of the register argument slot can differ from
        // the type of the lclVar in ways that are not ascertainable from lvType.
        // So, for that case we retain the type of the register in the regArgTab.

        var_types getRegType(Compiler* compiler)
        {
            return type; // UNIX_AMD64 implementation
        }

#else // !FEATURE_UNIX_AMD64_STRUCT_PASSING

        // In other cases, we simply use the type of the lclVar to determine the type of the register.
        var_types getRegType(Compiler* compiler)
        {
            LclVarDsc varDsc = compiler->lvaTable[varNum];
            // Check if this is an HFA register arg and return the HFA type
            if (varDsc.lvIsHfaRegArg())
            {
                return varDsc.GetHfaType();
            }
            return varDsc.lvType;
        }

#endif // !FEATURE_UNIX_AMD64_STRUCT_PASSING
    } regArgTab[max(MAX_REG_ARG + 1, MAX_FLOAT_REG_ARG)] = {};

    unsigned   varNum;
    LclVarDsc* varDsc;
    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
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
                noway_assert(parentVarDsc->lvFieldCnt == 1); // We only handle one field here

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

        var_types regType = varDsc->TypeGet();
        // Change regType to the HFA type when we have a HFA argument
        if (varDsc->lvIsHfaRegArg())
        {
            regType = varDsc->GetHfaType();
        }

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        if (!varTypeIsStruct(regType))
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        {
            // A struct might be passed  partially in XMM register for System V calls.
            // So a single arg might use both register files.
            if (isFloatRegType(regType) != doingFloat)
            {
                continue;
            }
        }

        int slots = 0;

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        if (varTypeIsStruct(varDsc))
        {
            CORINFO_CLASS_HANDLE typeHnd = varDsc->lvVerTypeInfo.GetClassHandle();
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
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        {
            // Bingo - add it to our table
            regArgNum = genMapRegNumToRegArgNum(varDsc->lvArgReg, regType);

            noway_assert(regArgNum < argMax);
            // We better not have added it already (there better not be multiple vars representing this argument
            // register)
            noway_assert(regArgTab[regArgNum].slot == 0);

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            // Set the register type.
            regArgTab[regArgNum].type = regType;
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

            regArgTab[regArgNum].varNum = varNum;
            regArgTab[regArgNum].slot   = 1;

            slots = 1;

#if FEATURE_MULTIREG_ARGS
            if (compiler->lvaIsMultiregStruct(varDsc))
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

#ifdef _TARGET_ARM_
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
#endif // _TARGET_ARM_

        for (int i = 0; i < slots; i++)
        {
            regType          = regArgTab[regArgNum + i].getRegType(compiler);
            regNumber regNum = genMapRegArgNumToRegNum(regArgNum + i, regType);

#if !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            // lvArgReg could be INT or FLOAT reg. So the following assertion doesn't hold.
            // The type of the register depends on the classification of the first eightbyte
            // of the struct. For information on classification refer to the System V x86_64 ABI at:
            // http://www.x86-64.org/documentation/abi.pdf

            assert((i > 0) || (regNum == varDsc->lvArgReg));
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            // Is the arg dead on entry to the method ?

            if ((regArgMaskLive & genRegMask(regNum)) == 0)
            {
                if (varDsc->lvTrackedNonStruct())
                {
                    noway_assert(!VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex));
                }
                else
                {
#ifdef _TARGET_X86_
                    noway_assert(varDsc->lvType == TYP_STRUCT);
#else // !_TARGET_X86_
#ifndef LEGACY_BACKEND
                    // For LSRA, it may not be in regArgMaskLive if it has a zero
                    // refcnt.  This is in contrast with the non-LSRA case in which all
                    // non-tracked args are assumed live on entry.
                    noway_assert((varDsc->lvRefCnt == 0) || (varDsc->lvType == TYP_STRUCT) ||
                                 (varDsc->lvAddrExposed && compiler->info.compIsVarArgs));
#else  // LEGACY_BACKEND
                    noway_assert(
                        varDsc->lvType == TYP_STRUCT ||
                        (varDsc->lvAddrExposed && (compiler->info.compIsVarArgs || compiler->opts.compUseSoftFP)));
#endif // LEGACY_BACKEND
#endif // !_TARGET_X86_
                }
                // Mark it as processed and be done with it
                regArgTab[regArgNum + i].processed = true;
                goto NON_DEP;
            }

#ifdef _TARGET_ARM_
            // On the ARM when the varDsc is a struct arg (or pre-spilled due to varargs) the initReg/xtraReg
            // could be equal to lvArgReg. The pre-spilled registers are also not considered live either since
            // they've already been spilled.
            //
            if ((regSet.rsMaskPreSpillRegs(false) & genRegMask(regNum)) == 0)
#endif // _TARGET_ARM_
            {
                noway_assert(xtraReg != varDsc->lvArgReg + i);
                noway_assert(regArgMaskLive & genRegMask(regNum));
            }

            regArgTab[regArgNum + i].processed = false;

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

                if ((i == 0) && (varDsc->lvRegNum == regNum))
                {
                    goto NON_DEP;
                }

#if !defined(_TARGET_64BIT_)
                if ((i == 1) && varTypeIsStruct(varDsc) && (varDsc->lvOtherReg == regNum))
                {
                    goto NON_DEP;
                }
                if ((i == 1) && (genActualType(varDsc->TypeGet()) == TYP_LONG) && (varDsc->lvOtherReg == regNum))
                {
                    goto NON_DEP;
                }

                if ((i == 1) && (genActualType(varDsc->TypeGet()) == TYP_DOUBLE) &&
                    (REG_NEXT(varDsc->lvRegNum) == regNum))
                {
                    goto NON_DEP;
                }
#endif // !defined(_TARGET_64BIT_)
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
                if (regArgTab[argNum].slot == 1)
                {
                    destRegNum = varDsc->lvRegNum;
                }
#if FEATURE_MULTIREG_ARGS && defined(FEATURE_SIMD) && defined(_TARGET_AMD64_)
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
#elif !defined(_TARGET_64BIT_)
                else if (regArgTab[argNum].slot == 2 && genActualType(varDsc->TypeGet()) == TYP_LONG)
                {
                    destRegNum = varDsc->lvOtherReg;
                }
                else
                {
                    assert(regArgTab[argNum].slot == 2);
                    assert(varDsc->TypeGet() == TYP_DOUBLE);
                    destRegNum = REG_NEXT(varDsc->lvRegNum);
                }
#endif // !defined(_TARGET_64BIT_)
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
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef LEGACY_BACKEND
    noway_assert(((regArgMaskLive & RBM_FLTARG_REGS) == 0) &&
                 "Homing of float argument registers with circular dependencies not implemented.");
#endif // LEGACY_BACKEND

    /* Now move the arguments to their locations.
     * First consider ones that go on the stack since they may
     * free some registers. */

    regArgMaskLive = regState->rsCalleeRegArgMaskLiveIn; // reset the live in to what it was at the start
    for (argNum = 0; argNum < argMax; argNum++)
    {
        emitAttr size;

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        // If this is the wrong register file, just continue.
        if (regArgTab[argNum].type == TYP_UNDEF)
        {
            // This could happen if the reg in regArgTab[argNum] is of the other register file -
            //     for System V register passed structs where the first reg is GPR and the second an XMM reg.
            // The next register file processing will process it.
            continue;
        }
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
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

#ifndef _TARGET_64BIT_
        // If not a stack arg go to the next one
        if (varDsc->lvType == TYP_LONG)
        {
            if (regArgTab[argNum].slot == 1 && !regArgTab[argNum].stackArg)
            {
                continue;
            }
            else if (varDsc->lvOtherReg != REG_STK)
            {
                continue;
            }
        }
        else
#endif // !_TARGET_64BIT_
        {
            // If not a stack arg go to the next one
            if (!regArgTab[argNum].stackArg)
            {
                continue;
            }
        }

#if defined(_TARGET_ARM_)
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
        noway_assert(varDsc->lvIsInReg() == false ||
                     (varDsc->lvType == TYP_LONG && varDsc->lvOtherReg == REG_STK && regArgTab[argNum].slot == 2));

        var_types storeType = TYP_UNDEF;
        unsigned  slotSize  = TARGET_POINTER_SIZE;

        if (varTypeIsStruct(varDsc))
        {
            storeType = TYP_I_IMPL; // Default store type for a struct type is a pointer sized integer
#if FEATURE_MULTIREG_ARGS
            // Must be <= MAX_PASS_MULTIREG_BYTES or else it wouldn't be passed in registers
            noway_assert(varDsc->lvSize() <= MAX_PASS_MULTIREG_BYTES);
#endif // FEATURE_MULTIREG_ARGS
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
            storeType = regArgTab[argNum].type;
#endif // !FEATURE_UNIX_AMD64_STRUCT_PASSING
            if (varDsc->lvIsHfaRegArg())
            {
#ifdef _TARGET_ARM_
                // On ARM32 the storeType for HFA args is always TYP_FLOAT
                storeType = TYP_FLOAT;
                slotSize  = (unsigned)emitActualTypeSize(storeType);
#else  // _TARGET_ARM64_
                storeType = genActualType(varDsc->GetHfaType());
                slotSize  = (unsigned)emitActualTypeSize(storeType);
#endif // _TARGET_ARM64_
            }
        }
        else // Not a struct type
        {
            storeType = genActualType(varDsc->TypeGet());
        }
        size = emitActualTypeSize(storeType);
#ifdef _TARGET_X86_
        noway_assert(genTypeSize(storeType) == TARGET_POINTER_SIZE);
#endif //_TARGET_X86_

        regNumber srcRegNum = genMapRegArgNumToRegNum(argNum, storeType);

        // Stack argument - if the ref count is 0 don't care about it

        if (!varDsc->lvOnFrame)
        {
            noway_assert(varDsc->lvRefCnt == 0);
        }
        else
        {
            // Since slot is typically 1, baseOffset is typically 0
            int baseOffset = (regArgTab[argNum].slot - 1) * slotSize;

            getEmitter()->emitIns_S_R(ins_Store(storeType), size, srcRegNum, varNum, baseOffset);

#ifndef FEATURE_UNIX_AMD64_STRUCT_PASSING
            // Check if we are writing past the end of the struct
            if (varTypeIsStruct(varDsc))
            {
                assert(varDsc->lvSize() >= baseOffset + (unsigned)size);
            }
#endif // !FEATURE_UNIX_AMD64_STRUCT_PASSING

            if (regArgTab[argNum].slot == 1)
            {
                psiMoveToStack(varNum);
            }
        }

        /* mark the argument as processed */

        regArgTab[argNum].processed = true;
        regArgMaskLive &= ~genRegMask(srcRegNum);

#if defined(_TARGET_ARM_)
        if (storeType == TYP_DOUBLE)
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
#if defined(FEATURE_HFA) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            insCopy = ins_Copy(TYP_DOUBLE);
            // Compute xtraReg here when we have a float argument
            assert(xtraReg == REG_NA);

            regMaskTP fpAvailMask;

            fpAvailMask = RBM_FLT_CALLEE_TRASH & ~regArgMaskLive;
#if defined(FEATURE_HFA)
            fpAvailMask &= RBM_ALLDOUBLE;
#else
#if !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
#error Error. Wrong architecture.
#endif // !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
#endif // defined(FEATURE_HFA)

            if (fpAvailMask == RBM_NONE)
            {
                fpAvailMask = RBM_ALLFLOAT & ~regArgMaskLive;
#if defined(FEATURE_HFA)
                fpAvailMask &= RBM_ALLDOUBLE;
#else
#if !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
#error Error. Wrong architecture.
#endif // !defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
#endif // defined(FEATURE_HFA)
            }

            assert(fpAvailMask != RBM_NONE);

            // We pick the lowest avail register number
            regMaskTP tempMask = genFindLowestBit(fpAvailMask);
            xtraReg            = genRegNumFromMask(tempMask);
#elif defined(_TARGET_X86_)
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

#ifdef _TARGET_XARCH_
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

                noway_assert(varDscDest->lvArgReg == varDscSrc->lvRegNum);

                getEmitter()->emitIns_R_R(INS_xchg, size, varDscSrc->lvRegNum, varDscSrc->lvArgReg);
                regTracker.rsTrackRegTrash(varDscSrc->lvRegNum);
                regTracker.rsTrackRegTrash(varDscSrc->lvArgReg);

                /* mark both arguments as processed */
                regArgTab[destReg].processed = true;
                regArgTab[srcReg].processed  = true;

                regArgMaskLive &= ~genRegMask(varDscSrc->lvArgReg);
                regArgMaskLive &= ~genRegMask(varDscDest->lvArgReg);

                psiMoveToReg(varNumSrc);
                psiMoveToReg(varNumDest);
            }
            else
#endif // _TARGET_XARCH_
            {
                var_types destMemType = varDscDest->TypeGet();

#ifdef _TARGET_ARM_
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
#endif // _TARGET_ARM_

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

                getEmitter()->emitIns_R_R(insCopy, size, xtraReg, begRegNum);

                regTracker.rsTrackRegCopy(xtraReg, begRegNum);

                *pXtraRegClobbered = true;

                psiMoveToReg(varNumDest, xtraReg);

                /* start moving everything to its right place */

                while (srcReg != begReg)
                {
                    /* mov dest, src */

                    regNumber destRegNum = genMapRegArgNumToRegNum(destReg, destMemType);
                    regNumber srcRegNum  = genMapRegArgNumToRegNum(srcReg, destMemType);

                    getEmitter()->emitIns_R_R(insCopy, size, destRegNum, srcRegNum);

                    regTracker.rsTrackRegCopy(destRegNum, srcRegNum);

                    /* mark 'src' as processed */
                    noway_assert(srcReg < argMax);
                    regArgTab[srcReg].processed = true;
#ifdef _TARGET_ARM_
                    if (size == EA_8BYTE)
                        regArgTab[srcReg + 1].processed = true;
#endif
                    regArgMaskLive &= ~genMapArgNumToRegMask(srcReg, destMemType);

                    /* move to the next pair */
                    destReg = srcReg;
                    srcReg  = regArgTab[srcReg].trashBy;

                    varDscDest  = varDscSrc;
                    destMemType = varDscDest->TypeGet();
#ifdef _TARGET_ARM_
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

                getEmitter()->emitIns_R_R(insCopy, size, destRegNum, xtraReg);

                regTracker.rsTrackRegCopy(destRegNum, xtraReg);

                psiMoveToReg(varNumSrc);

                /* mark the beginning register as processed */

                regArgTab[srcReg].processed = true;
#ifdef _TARGET_ARM_
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

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            if (regType == TYP_UNDEF)
            {
                // This could happen if the reg in regArgTab[argNum] is of the other register file -
                // for System V register passed structs where the first reg is GPR and the second an XMM reg.
                // The next register file processing will process it.
                regArgMaskLive &= ~genRegMask(regNum);
                continue;
            }
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

            noway_assert(varDsc->lvIsParam && varDsc->lvIsRegArg);
#ifndef _TARGET_64BIT_
#ifndef _TARGET_ARM_
            // Right now we think that incoming arguments are not pointer sized.  When we eventually
            // understand the calling convention, this still won't be true. But maybe we'll have a better
            // idea of how to ignore it.

            // On Arm, a long can be passed in register
            noway_assert(genTypeSize(genActualType(varDsc->TypeGet())) == sizeof(void*));
#endif
#endif //_TARGET_64BIT_

            noway_assert(varDsc->lvIsInReg() && !regArgTab[argNum].circular);

            /* Register argument - hopefully it stays in the same register */
            regNumber destRegNum  = REG_NA;
            var_types destMemType = varDsc->TypeGet();

            if (regArgTab[argNum].slot == 1)
            {
                destRegNum = varDsc->lvRegNum;

#ifdef _TARGET_ARM_
                if (genActualType(destMemType) == TYP_DOUBLE && regArgTab[argNum + 1].processed)
                {
                    // The second half of the double has already been processed! Treat this as a single.
                    destMemType = TYP_FLOAT;
                }
#endif // _TARGET_ARM_
            }
#ifndef _TARGET_64BIT_
            else if (regArgTab[argNum].slot == 2 && genActualType(destMemType) == TYP_LONG)
            {
#ifndef LEGACY_BACKEND
                assert(genActualType(varDsc->TypeGet()) == TYP_LONG || genActualType(varDsc->TypeGet()) == TYP_DOUBLE);
                if (genActualType(varDsc->TypeGet()) == TYP_DOUBLE)
                {
                    destRegNum = regNum;
                }
                else
#endif // !LEGACY_BACKEND
                    destRegNum = varDsc->lvOtherReg;

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
                destRegNum  = REG_NEXT(varDsc->lvRegNum);
            }
#endif // !_TARGET_64BIT_
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING) && defined(FEATURE_SIMD)
            else
            {
                assert(regArgTab[argNum].slot == 2);
                assert(argNum > 0);
                assert(regArgTab[argNum - 1].slot == 1);
                assert((varDsc->lvType == TYP_SIMD12) || (varDsc->lvType == TYP_SIMD16));
                destRegNum = varDsc->lvRegNum;
                noway_assert(regNum != destRegNum);
                continue;
            }
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING) && defined(FEATURE_SIMD)
            noway_assert(destRegNum != REG_NA);
            if (destRegNum != regNum)
            {
                /* Cannot trash a currently live register argument.
                 * Skip this one until its target will be free
                 * which is guaranteed to happen since we have no circular dependencies. */

                regMaskTP destMask = genRegMask(destRegNum);
#ifdef _TARGET_ARM_
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

                getEmitter()->emitIns_R_R(ins_Copy(destMemType), size, destRegNum, regNum);

                psiMoveToReg(varNum);
            }

            /* mark the argument as processed */

            assert(!regArgTab[argNum].processed);
            regArgTab[argNum].processed = true;
            regArgMaskLive &= ~genRegMask(regNum);
#if FEATURE_MULTIREG_ARGS
            int argRegCount = 1;
#ifdef _TARGET_ARM_
            if (genActualType(destMemType) == TYP_DOUBLE)
            {
                argRegCount = 2;
            }
#endif
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING) && defined(FEATURE_SIMD)
            if (varTypeIsStruct(varDsc) && argNum < (argMax - 1) && regArgTab[argNum + 1].slot == 2)
            {
                argRegCount          = 2;
                int       nextArgNum = argNum + 1;
                regNumber nextRegNum = genMapRegArgNumToRegNum(nextArgNum, regArgTab[nextArgNum].getRegType(compiler));
                noway_assert(regArgTab[nextArgNum].varNum == varNum);
                // Emit a shufpd with a 0 immediate, which preserves the 0th element of the dest reg
                // and moves the 0th element of the src reg into the 1st element of the dest reg.
                getEmitter()->emitIns_R_R_I(INS_shufpd, emitActualTypeSize(varDsc->lvType), destRegNum, nextRegNum, 0);
                // Set destRegNum to regNum so that we skip the setting of the register below,
                // but mark argNum as processed and clear regNum from the live mask.
                destRegNum = regNum;
            }
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING) && defined(FEATURE_SIMD)
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
#if defined(_TARGET_ARM_) && defined(PROFILING_SUPPORTED)
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

        var_types type = genActualType(varDsc->TypeGet());

#if FEATURE_STACK_FP_X87
        // Floating point locals are loaded onto the x86-FPU in the next section
        if (varTypeIsFloating(type))
            continue;
#endif

        /* Is the variable dead on entry */

        if (!VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
        {
            continue;
        }

        /* Load the incoming parameter into the register */

        /* Figure out the home offset of the incoming argument */

        regNumber regNum;
        regNumber otherReg;

#ifndef LEGACY_BACKEND
#ifdef _TARGET_ARM_
        if (type == TYP_LONG)
        {
            regPairNo regPair = varDsc->lvArgInitRegPair;
            regNum            = genRegPairLo(regPair);
            otherReg          = genRegPairHi(regPair);
        }
        else
#endif // _TARGET_ARM
        {
            regNum   = varDsc->lvArgInitReg;
            otherReg = REG_NA;
        }
#else  // LEGACY_BACKEND
        regNum = varDsc->lvRegNum;
        if (type == TYP_LONG)
        {
            otherReg = varDsc->lvOtherReg;
        }
        else
        {
            otherReg = REG_NA;
        }
#endif // LEGACY_BACKEND

        assert(regNum != REG_STK);

#ifndef _TARGET_64BIT_
        if (type == TYP_LONG)
        {
            /* long - at least the low half must be enregistered */

            getEmitter()->emitIns_R_S(ins_Load(TYP_INT), EA_4BYTE, regNum, varNum, 0);
            regTracker.rsTrackRegTrash(regNum);

            /* Is the upper half also enregistered? */

            if (otherReg != REG_STK)
            {
                getEmitter()->emitIns_R_S(ins_Load(TYP_INT), EA_4BYTE, otherReg, varNum, sizeof(int));
                regTracker.rsTrackRegTrash(otherReg);
            }
        }
        else
#endif // _TARGET_64BIT_
        {
            /* Loading a single register - this is the easy/common case */

            getEmitter()->emitIns_R_S(ins_Load(type), emitTypeSize(type), regNum, varNum, 0);
            regTracker.rsTrackRegTrash(regNum);
        }

        psiMoveToReg(varNum);
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
#ifndef LEGACY_BACKEND // this is called before codegen in RyuJIT backend
    assert(!compiler->compGeneratingProlog);
#else  // LEGACY_BACKEND
    assert(compiler->compGeneratingProlog);
#endif // LEGACY_BACKEND

    unsigned initStkLclCnt = 0;  // The number of int-sized stack local variables that need to be initialized (variables
                                 // larger than int count for more than 1).
    unsigned largeGcStructs = 0; // The number of "large" structs with GC pointers. Used as part of the heuristic to
                                 // determine whether to use block init.

    unsigned   varNum;
    LclVarDsc* varDsc;

    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        if (varDsc->lvIsParam)
        {
            continue;
        }

        if (!varDsc->lvIsInReg() && !varDsc->lvOnFrame)
        {
            noway_assert(varDsc->lvRefCnt == 0);
            continue;
        }

        if (varNum == compiler->lvaInlinedPInvokeFrameVar || varNum == compiler->lvaStubArgumentVar)
        {
            continue;
        }

#if FEATURE_FIXED_OUT_ARGS
        if (varNum == compiler->lvaPInvokeFrameRegSaveVar)
        {
            continue;
        }
        if (varNum == compiler->lvaOutgoingArgSpaceVar)
        {
            continue;
        }
#endif

#if FEATURE_EH_FUNCLETS
        // There's no need to force 0-initialization of the PSPSym, it will be
        // initialized with a real value in the prolog
        if (varNum == compiler->lvaPSPSym)
        {
            continue;
        }
#endif

        if (compiler->lvaIsFieldOfDependentlyPromotedStruct(varDsc))
        {
            // For Compiler::PROMOTION_TYPE_DEPENDENT type of promotion, the whole struct should have been
            // initialized by the parent struct. No need to set the lvMustInit bit in the
            // field locals.
            continue;
        }

        if (compiler->info.compInitMem || varTypeIsGC(varDsc->TypeGet()) || (varDsc->lvStructGcCount > 0) ||
            varDsc->lvMustInit)
        {
            if (varDsc->lvTracked)
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
#ifndef LEGACY_BACKEND
                            if (!varDsc->lvIsInReg())
#endif // !LEGACY_BACKEND
                            {
                                // Var is completely on the stack, in the legacy JIT case, or
                                // on the stack at entry, in the RyuJIT case.
                                initStkLclCnt += (unsigned)roundUp(compiler->lvaLclSize(varNum)) / sizeof(int);
                            }
                        }
                        else
                        {
                            // Var is partially enregistered
                            noway_assert(genTypeSize(varDsc->TypeGet()) > sizeof(int) && varDsc->lvOtherReg == REG_STK);
                            initStkLclCnt += genTypeStSz(TYP_INT);
                        }
                    }
                }
            }

            /* With compInitMem, all untracked vars will have to be init'ed */
            /* VSW 102460 - Do not force initialization of compiler generated temps,
                unless they are untracked GC type or structs that contain GC pointers */
            CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_SIMD
            // TODO-1stClassStructs
            // This is here to duplicate previous behavior, where TYP_SIMD8 locals
            // were not being re-typed correctly.
            if ((!varDsc->lvTracked || (varDsc->lvType == TYP_STRUCT) || (varDsc->lvType == TYP_SIMD8)) &&
#else  // !FEATURE_SIMD
            if ((!varDsc->lvTracked || (varDsc->lvType == TYP_STRUCT)) &&
#endif // !FEATURE_SIMD
                varDsc->lvOnFrame &&
                (!varDsc->lvIsTemp || varTypeIsGC(varDsc->TypeGet()) || (varDsc->lvStructGcCount > 0)))
            {
                varDsc->lvMustInit = true;

                initStkLclCnt += (unsigned)roundUp(compiler->lvaLclSize(varNum)) / sizeof(int);
            }

            continue;
        }

        /* Ignore if not a pointer variable or value class with a GC field */

        if (!compiler->lvaTypeIsGC(varNum))
        {
            continue;
        }

#if CAN_DISABLE_DFA
        /* If we don't know lifetimes of variables, must be conservative */

        if (compiler->opts.MinOpts())
        {
            varDsc->lvMustInit = true;
            noway_assert(!varDsc->lvRegister);
        }
        else
#endif // CAN_DISABLE_DFA
        {
            if (!varDsc->lvTracked)
            {
                varDsc->lvMustInit = true;
            }
        }

        /* Is this a 'must-init' stack pointer local? */

        if (varDsc->lvMustInit && varDsc->lvOnFrame)
        {
            initStkLclCnt += varDsc->lvStructGcCount;
        }

        if ((compiler->lvaLclSize(varNum) > (3 * sizeof(void*))) && (largeGcStructs <= 4))
        {
            largeGcStructs++;
        }
    }

    /* Don't forget about spill temps that hold pointers */

    if (!TRACK_GC_TEMP_LIFETIMES)
    {
        assert(compiler->tmpAllFree());
        for (TempDsc* tempThis = compiler->tmpListBeg(); tempThis != nullptr; tempThis = compiler->tmpListNxt(tempThis))
        {
            if (varTypeIsGC(tempThis->tdTempType()))
            {
                initStkLclCnt++;
            }
        }
    }

    // After debugging this further it was found that this logic is incorrect:
    // it incorrectly assumes the stack slots are always 4 bytes (not necessarily the case)
    // and this also double counts variables (we saw this in the debugger) around line 4829.
    // Even though this doesn't pose a problem with correctness it will improperly decide to
    // zero init the stack using a block operation instead of a 'case by case' basis.
    genInitStkLclCnt = initStkLclCnt;

    /* If we have more than 4 untracked locals, use block initialization */
    /* TODO-Review: If we have large structs, bias toward not using block initialization since
       we waste all the other slots.  Really need to compute the correct
       and compare that against zeroing the slots individually */

    genUseBlockInit = (genInitStkLclCnt > (largeGcStructs + 4));

    if (genUseBlockInit)
    {
        regMaskTP maskCalleeRegArgMask = intRegState.rsCalleeRegArgMaskLiveIn;

        // If there is a secret stub param, don't count it, as it will no longer
        // be live when we do block init.
        if (compiler->info.compPublishStubParam)
        {
            maskCalleeRegArgMask &= ~RBM_SECRET_STUB_PARAM;
        }

#ifdef _TARGET_XARCH_
        // If we're going to use "REP STOS", remember that we will trash EDI
        // For fastcall we will have to save ECX, EAX
        // so reserve two extra callee saved
        // This is better than pushing eax, ecx, because we in the later
        // we will mess up already computed offsets on the stack (for ESP frames)
        regSet.rsSetRegsModified(RBM_EDI);

#ifdef UNIX_AMD64_ABI
        // For register arguments we may have to save ECX (and RDI on Amd64 System V OSes.)
        // In such case use R12 and R13 registers.
        if (maskCalleeRegArgMask & RBM_RCX)
        {
            regSet.rsSetRegsModified(RBM_R12);
        }

        if (maskCalleeRegArgMask & RBM_RDI)
        {
            regSet.rsSetRegsModified(RBM_R13);
        }
#else  // !UNIX_AMD64_ABI
        if (maskCalleeRegArgMask & RBM_ECX)
        {
            regSet.rsSetRegsModified(RBM_ESI);
        }
#endif // !UNIX_AMD64_ABI

        if (maskCalleeRegArgMask & RBM_EAX)
        {
            regSet.rsSetRegsModified(RBM_EBX);
        }

#endif // _TARGET_XARCH_
#ifdef _TARGET_ARM_
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
#endif // _TARGET_ARM_
    }
}

/*-----------------------------------------------------------------------------
 *
 *  Push any callee-saved registers we have used
 */

#if defined(_TARGET_ARM64_)
void CodeGen::genPushCalleeSavedRegisters(regNumber initReg, bool* pInitRegZeroed)
#else
void          CodeGen::genPushCalleeSavedRegisters()
#endif
{
    assert(compiler->compGeneratingProlog);

#if defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87
    // x86/x64 doesn't support push of xmm/ymm regs, therefore consider only integer registers for pushing onto stack
    // here. Space for float registers to be preserved is stack allocated and saved as part of prolog sequence and not
    // here.
    regMaskTP rsPushRegs = regSet.rsGetModifiedRegsMask() & RBM_INT_CALLEE_SAVED;
#else // !defined(_TARGET_XARCH_) || FEATURE_STACK_FP_X87
    regMaskTP rsPushRegs = regSet.rsGetModifiedRegsMask() & RBM_CALLEE_SAVED;
#endif

#if ETW_EBP_FRAMED
    if (!isFramePointerUsed() && regSet.rsRegsModified(RBM_FPBASE))
    {
        noway_assert(!"Used register RBM_FPBASE as a scratch register!");
    }
#endif

#ifdef _TARGET_XARCH_
    // On X86/X64 we have already pushed the FP (frame-pointer) prior to calling this method
    if (isFramePointerUsed())
    {
        rsPushRegs &= ~RBM_FPBASE;
    }
#endif

#ifdef _TARGET_ARMARCH_
    // On ARM we push the FP (frame-pointer) here along with all other callee saved registers
    if (isFramePointerUsed())
        rsPushRegs |= RBM_FPBASE;

    //
    // It may be possible to skip pushing/popping lr for leaf methods. However, such optimization would require
    // changes in GC suspension architecture.
    //
    // We would need to guarantee that a tight loop calling a virtual leaf method can be suspended for GC. Today, we
    // generate partially interruptible code for both the method that contains the tight loop with the call and the leaf
    // method. GC suspension depends on return address hijacking in this case. Return address hijacking depends
    // on the return address to be saved on the stack. If we skipped pushing/popping lr, the return address would never
    // be saved on the stack and the GC suspension would time out.
    //
    // So if we wanted to skip pushing pushing/popping lr for leaf frames, we would also need to do one of
    // the following to make GC suspension work in the above scenario:
    // - Make return address hijacking work even when lr is not saved on the stack.
    // - Generate fully interruptible code for loops that contains calls
    // - Generate fully interruptible code for leaf methods
    //
    // Given the limited benefit from this optimization (<10k for mscorlib NGen image), the extra complexity
    // is not worth it.
    //
    rsPushRegs |= RBM_LR; // We must save the return address (in the LR register)

    regSet.rsMaskCalleeSaved = rsPushRegs;
#endif // _TARGET_ARMARCH_

#ifdef DEBUG
    if (compiler->compCalleeRegsPushed != genCountBits(rsPushRegs))
    {
        printf("Error: unexpected number of callee-saved registers to push. Expected: %d. Got: %d ",
               compiler->compCalleeRegsPushed, genCountBits(rsPushRegs));
        dspRegMask(rsPushRegs);
        printf("\n");
        assert(compiler->compCalleeRegsPushed == genCountBits(rsPushRegs));
    }
#endif // DEBUG

#if defined(_TARGET_ARM_)
    regMaskTP maskPushRegsFloat = rsPushRegs & RBM_ALLFLOAT;
    regMaskTP maskPushRegsInt   = rsPushRegs & ~maskPushRegsFloat;

    maskPushRegsInt |= genStackAllocRegisterMask(compiler->compLclFrameSize, maskPushRegsFloat);

    assert(FitsIn<int>(maskPushRegsInt));
    inst_IV(INS_push, (int)maskPushRegsInt);
    compiler->unwindPushMaskInt(maskPushRegsInt);

    if (maskPushRegsFloat != 0)
    {
        genPushFltRegs(maskPushRegsFloat);
        compiler->unwindPushMaskFloat(maskPushRegsFloat);
    }
#elif defined(_TARGET_ARM64_)
    // See the document "ARM64 JIT Frame Layout" and/or "ARM64 Exception Data" for more details or requirements and
    // options. Case numbers in comments here refer to this document.
    //
    // For most frames, generate, e.g.:
    //      stp fp,  lr,  [sp,-0x80]!   // predecrement SP with full frame size, and store FP/LR pair. Store pair
    //                                  // ensures stack stays aligned.
    //      stp r19, r20, [sp, 0x60]    // store at positive offset from SP established above, into callee-saved area
    //                                  // at top of frame (highest addresses).
    //      stp r21, r22, [sp, 0x70]
    //
    // Notes:
    // 1. We don't always need to save FP. If FP isn't saved, then LR is saved with the other callee-saved registers
    //    at the top of the frame.
    // 2. If we save FP, then the first store is FP, LR.
    // 3. General-purpose registers are 8 bytes, floating-point registers are 16 bytes, but FP/SIMD registers only
    //    preserve their lower 8 bytes, by calling convention.
    // 4. For frames with varargs, we spill the integer register arguments to the stack, so all the arguments are
    //    consecutive.
    // 5. We allocate the frame here; no further changes to SP are allowed (except in the body, for localloc).

    int totalFrameSize = genTotalFrameSize();

    int offset; // This will be the starting place for saving the callee-saved registers, in increasing order.

    regMaskTP maskSaveRegsFloat = rsPushRegs & RBM_ALLFLOAT;
    regMaskTP maskSaveRegsInt   = rsPushRegs & ~maskSaveRegsFloat;

    if (compiler->info.compIsVarArgs)
    {
        assert(maskSaveRegsFloat == RBM_NONE);
    }

    int frameType = 0; // This number is arbitrary, is defined below, and corresponds to one of the frame styles we
                       // generate based on various sizes.
    int calleeSaveSPDelta          = 0;
    int calleeSaveSPDeltaUnaligned = 0;

    if (isFramePointerUsed())
    {
        // We need to save both FP and LR.

        assert((maskSaveRegsInt & RBM_FP) != 0);
        assert((maskSaveRegsInt & RBM_LR) != 0);

        if ((compiler->lvaOutgoingArgSpaceSize == 0) && (totalFrameSize < 512))
        {
            // Case #1.
            //
            // Generate:
            //      stp fp,lr,[sp,#-framesz]!
            //
            // The (totalFrameSize < 512) condition ensures that both the predecrement
            //  and the postincrement of SP can occur with STP.
            //
            // After saving callee-saved registers, we establish the frame pointer with:
            //      mov fp,sp
            // We do this *after* saving callee-saved registers, so the prolog/epilog unwind codes mostly match.

            frameType = 1;

            getEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, -totalFrameSize,
                                          INS_OPTS_PRE_INDEX);
            compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, -totalFrameSize);

            maskSaveRegsInt &= ~(RBM_FP | RBM_LR);                        // We've already saved FP/LR
            offset = (int)compiler->compLclFrameSize + 2 * REGSIZE_BYTES; // 2 for FP/LR
        }
        else if (totalFrameSize <= 512)
        {
            // Case #2.
            //
            // Generate:
            //      sub sp,sp,#framesz
            //      stp fp,lr,[sp,#outsz]   // note that by necessity, #outsz <= #framesz - 16, so #outsz <= 496.
            //
            // The (totalFrameSize <= 512) condition ensures the callee-saved registers can all be saved using STP with
            // signed offset encoding.
            //
            // After saving callee-saved registers, we establish the frame pointer with:
            //      add fp,sp,#outsz
            // We do this *after* saving callee-saved registers, so the prolog/epilog unwind codes mostly match.

            frameType = 2;

            assert(compiler->lvaOutgoingArgSpaceSize + 2 * REGSIZE_BYTES <= (unsigned)totalFrameSize);

            getEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, totalFrameSize);
            compiler->unwindAllocStack(totalFrameSize);

            getEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE,
                                          compiler->lvaOutgoingArgSpaceSize);
            compiler->unwindSaveRegPair(REG_FP, REG_LR, compiler->lvaOutgoingArgSpaceSize);

            maskSaveRegsInt &= ~(RBM_FP | RBM_LR);                        // We've already saved FP/LR
            offset = (int)compiler->compLclFrameSize + 2 * REGSIZE_BYTES; // 2 for FP/LR
        }
        else
        {
            // Case 5 or 6.
            //
            // First, the callee-saved registers will be saved, and the callee-saved register code must use pre-index
            // to subtract from SP as the first instruction. It must also leave space for varargs registers to be
            // stored. For example:
            //      stp r19,r20,[sp,#-96]!
            //      stp d8,d9,[sp,#16]
            //      ... save varargs incoming integer registers ...
            // Note that all SP alterations must be 16-byte aligned. We have already calculated any alignment to be
            // lower on the stack than the callee-saved registers (see lvaAlignFrame() for how we calculate alignment).
            // So, if there is an odd number of callee-saved registers, we use (for example, with just one saved
            // register):
            //      sub sp,sp,#16
            //      str r19,[sp,#8]
            // This is one additional instruction, but it centralizes the aligned space. Otherwise, it might be
            // possible to have two 8-byte alignment padding words, one below the callee-saved registers, and one
            // above them. If that is preferable, we could implement it.
            // Note that any varargs saved space will always be 16-byte aligned, since there are 8 argument registers.
            //
            // Then, define #remainingFrameSz = #framesz - (callee-saved size + varargs space + possible alignment
            // padding from above).
            // Note that #remainingFrameSz must not be zero, since we still need to save FP,SP.
            //
            // Generate:
            //      sub sp,sp,#remainingFrameSz
            // or, for large frames:
            //      mov rX, #remainingFrameSz // maybe multiple instructions
            //      sub sp,sp,rX
            //
            // followed by:
            //      stp fp,lr,[sp,#outsz]
            //      add fp,sp,#outsz
            //
            // However, we need to handle the case where #outsz is larger than the constant signed offset encoding can
            // handle. And, once again, we might need to deal with #outsz that is not aligned to 16-bytes (i.e.,
            // STACK_ALIGN). So, in the case of large #outsz we will have an additional SP adjustment, using one of the
            // following sequences:
            //
            // Define #remainingFrameSz2 = #remainingFrameSz - #outsz.
            //
            //      sub sp,sp,#remainingFrameSz2  // if #remainingFrameSz2 is 16-byte aligned
            //      stp fp,lr,[sp]
            //      mov fp,sp
            //      sub sp,sp,#outsz    // in this case, #outsz must also be 16-byte aligned
            //
            // Or:
            //
            //      sub sp,sp,roundUp(#remainingFrameSz2,16)    // if #remainingFrameSz2 is not 16-byte aligned (it is
            //                                                  // always guaranteed to be 8 byte aligned).
            //      stp fp,lr,[sp,#8]                           // it will always be #8 in the unaligned case
            //      add fp,sp,#8
            //      sub sp,sp,#outsz - #8
            //
            // (As usual, for a large constant "#outsz - #8", we might need multiple instructions:
            //      mov rX, #outsz - #8 // maybe multiple instructions
            //      sub sp,sp,rX
            // )

            frameType = 3;

            calleeSaveSPDeltaUnaligned =
                totalFrameSize - compiler->compLclFrameSize - 2 * REGSIZE_BYTES; // 2 for FP, LR which we'll save later.
            assert(calleeSaveSPDeltaUnaligned >= 0);
            assert((calleeSaveSPDeltaUnaligned % 8) == 0); // It better at least be 8 byte aligned.
            calleeSaveSPDelta = AlignUp((UINT)calleeSaveSPDeltaUnaligned, STACK_ALIGN);

            offset = calleeSaveSPDelta - calleeSaveSPDeltaUnaligned;
            assert((offset == 0) || (offset == REGSIZE_BYTES)); // At most one alignment slot between SP and where we
                                                                // store the callee-saved registers.

            // We'll take care of these later, but callee-saved regs code shouldn't see them.
            maskSaveRegsInt &= ~(RBM_FP | RBM_LR);
        }
    }
    else
    {
        // No frame pointer (no chaining).
        assert((maskSaveRegsInt & RBM_FP) == 0);
        assert((maskSaveRegsInt & RBM_LR) != 0);

        // Note that there is no pre-indexed save_lrpair unwind code variant, so we can't allocate the frame using 'stp'
        // if we only have one callee-saved register plus LR to save.

        NYI("Frame without frame pointer");
        offset = 0;
    }

    assert(frameType != 0);

    genSaveCalleeSavedRegistersHelp(maskSaveRegsInt | maskSaveRegsFloat, offset, -calleeSaveSPDelta);

    offset += genCountBits(maskSaveRegsInt | maskSaveRegsFloat) * REGSIZE_BYTES;

    // For varargs, home the incoming arg registers last. Note that there is nothing to unwind here,
    // so we just report "NOP" unwind codes. If there's no more frame setup after this, we don't
    // need to add codes at all.

    if (compiler->info.compIsVarArgs)
    {
        // There are 8 general-purpose registers to home, thus 'offset' must be 16-byte aligned here.
        assert((offset % 16) == 0);
        for (regNumber reg1 = REG_ARG_FIRST; reg1 < REG_ARG_LAST; reg1 = REG_NEXT(REG_NEXT(reg1)))
        {
            regNumber reg2 = REG_NEXT(reg1);
            // stp REG, REG + 1, [SP, #offset]
            getEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, reg1, reg2, REG_SPBASE, offset);
            compiler->unwindNop();
            offset += 2 * REGSIZE_BYTES;
        }
    }

    if (frameType == 1)
    {
        getEmitter()->emitIns_R_R(INS_mov, EA_PTRSIZE, REG_FPBASE, REG_SPBASE);
        compiler->unwindSetFrameReg(REG_FPBASE, 0);
    }
    else if (frameType == 2)
    {
        getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, compiler->lvaOutgoingArgSpaceSize);
        compiler->unwindSetFrameReg(REG_FPBASE, compiler->lvaOutgoingArgSpaceSize);
    }
    else if (frameType == 3)
    {
        int remainingFrameSz = totalFrameSize - calleeSaveSPDelta;
        assert(remainingFrameSz > 0);
        assert((remainingFrameSz % 16) == 0); // this is guaranteed to be 16-byte aligned because each component --
                                              // totalFrameSize and calleeSaveSPDelta -- is 16-byte aligned.

        if (compiler->lvaOutgoingArgSpaceSize >= 504)
        {
            // We can't do "stp fp,lr,[sp,#outsz]" because #outsz is too big.
            // If compiler->lvaOutgoingArgSpaceSize is not aligned, we need to align the SP adjustment.
            assert(remainingFrameSz > (int)compiler->lvaOutgoingArgSpaceSize);
            int spAdjustment2Unaligned = remainingFrameSz - compiler->lvaOutgoingArgSpaceSize;
            int spAdjustment2          = (int)roundUp((size_t)spAdjustment2Unaligned, STACK_ALIGN);
            int alignmentAdjustment2   = spAdjustment2 - spAdjustment2Unaligned;
            assert((alignmentAdjustment2 == 0) || (alignmentAdjustment2 == 8));

            genPrologSaveRegPair(REG_FP, REG_LR, alignmentAdjustment2, -spAdjustment2, false, initReg, pInitRegZeroed);
            offset += spAdjustment2;

            // Now subtract off the #outsz (or the rest of the #outsz if it was unaligned, and the above "sub" included
            // some of it)

            int spAdjustment3 = compiler->lvaOutgoingArgSpaceSize - alignmentAdjustment2;
            assert(spAdjustment3 > 0);
            assert((spAdjustment3 % 16) == 0);

            getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, alignmentAdjustment2);
            compiler->unwindSetFrameReg(REG_FPBASE, alignmentAdjustment2);

            genStackPointerAdjustment(-spAdjustment3, initReg, pInitRegZeroed);
            offset += spAdjustment3;
        }
        else
        {
            genPrologSaveRegPair(REG_FP, REG_LR, compiler->lvaOutgoingArgSpaceSize, -remainingFrameSz, false, initReg,
                                 pInitRegZeroed);
            offset += remainingFrameSz;

            getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, compiler->lvaOutgoingArgSpaceSize);
            compiler->unwindSetFrameReg(REG_FPBASE, compiler->lvaOutgoingArgSpaceSize);
        }
    }

    assert(offset == totalFrameSize);

#elif defined(_TARGET_XARCH_)
    // Push backwards so we match the order we will pop them in the epilog
    // and all the other code that expects it to be in this order.
    for (regNumber reg = REG_INT_LAST; rsPushRegs != RBM_NONE; reg = REG_PREV(reg))
    {
        regMaskTP regBit = genRegMask(reg);

        if ((regBit & rsPushRegs) != 0)
        {
            inst_RV(INS_push, reg, TYP_REF);
            compiler->unwindPush(reg);

            if (!doubleAlignOrFramePointerUsed())
            {
                psiAdjustStackLevel(REGSIZE_BYTES);
            }

            rsPushRegs &= ~regBit;
        }
    }

#else
    assert(!"Unknown TARGET");
#endif // _TARGET_*
}

/*-----------------------------------------------------------------------------
 *
 *  Probe the stack and allocate the local stack frame: subtract from SP.
 *  On ARM64, this only does the probing; allocating the frame is done when callee-saved registers are saved.
 */

void CodeGen::genAllocLclFrame(unsigned frameSize, regNumber initReg, bool* pInitRegZeroed, regMaskTP maskArgRegsLiveIn)
{
    assert(compiler->compGeneratingProlog);

    if (frameSize == 0)
    {
        return;
    }

    const size_t pageSize = compiler->eeGetPageSize();

#ifdef _TARGET_ARM_
    assert(!compiler->info.compPublishStubParam || (REG_SECRET_STUB_PARAM != initReg));
#endif // _TARGET_ARM_

#ifdef _TARGET_XARCH_
    if (frameSize == REGSIZE_BYTES)
    {
        // Frame size is the same as register size.
        inst_RV(INS_push, REG_EAX, TYP_I_IMPL);
    }
    else
#endif // _TARGET_XARCH_
        if (frameSize < pageSize)
    {
#ifndef _TARGET_ARM64_
        // Frame size is (0x0008..0x1000)
        inst_RV_IV(INS_sub, REG_SPBASE, frameSize, EA_PTRSIZE);
#endif // !_TARGET_ARM64_
    }
    else if (frameSize < compiler->getVeryLargeFrameSize())
    {
        // Frame size is (0x1000..0x3000)
        CLANG_FORMAT_COMMENT_ANCHOR;

#if CPU_LOAD_STORE_ARCH
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, -(ssize_t)pageSize);
        getEmitter()->emitIns_R_R_R(INS_ldr, EA_4BYTE, initReg, REG_SPBASE, initReg);
        regTracker.rsTrackRegTrash(initReg);
        *pInitRegZeroed = false; // The initReg does not contain zero
#else
        getEmitter()->emitIns_AR_R(INS_TEST, EA_PTRSIZE, REG_EAX, REG_SPBASE, -(int)pageSize);
#endif

        if (frameSize >= 0x2000)
        {
#if CPU_LOAD_STORE_ARCH
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, -2 * (ssize_t)pageSize);
            getEmitter()->emitIns_R_R_R(INS_ldr, EA_4BYTE, initReg, REG_SPBASE, initReg);
            regTracker.rsTrackRegTrash(initReg);
#else
            getEmitter()->emitIns_AR_R(INS_TEST, EA_PTRSIZE, REG_EAX, REG_SPBASE, -2 * (int)pageSize);
#endif
        }

#ifdef _TARGET_ARM64_
        compiler->unwindPadding();
#else // !_TARGET_ARM64_
#if CPU_LOAD_STORE_ARCH
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, frameSize);
        compiler->unwindPadding();
        getEmitter()->emitIns_R_R_R(INS_sub, EA_4BYTE, REG_SPBASE, REG_SPBASE, initReg);
#else
        inst_RV_IV(INS_sub, REG_SPBASE, frameSize, EA_PTRSIZE);
#endif
#endif // !_TARGET_ARM64_
    }
    else
    {
        // Frame size >= 0x3000
        assert(frameSize >= compiler->getVeryLargeFrameSize());

        // Emit the following sequence to 'tickle' the pages.
        // Note it is important that stack pointer not change until this is
        // complete since the tickles could cause a stack overflow, and we
        // need to be able to crawl the stack afterward (which means the
        // stack pointer needs to be known).
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_XARCH_
        bool pushedStubParam = false;
        if (compiler->info.compPublishStubParam && (REG_SECRET_STUB_PARAM == initReg))
        {
            // push register containing the StubParam
            inst_RV(INS_push, REG_SECRET_STUB_PARAM, TYP_I_IMPL);
            pushedStubParam = true;
        }
#endif // !_TARGET_XARCH_

        instGen_Set_Reg_To_Zero(EA_PTRSIZE, initReg);

        //
        // Can't have a label inside the ReJIT padding area
        //
        genPrologPadForReJit();

#if CPU_LOAD_STORE_ARCH

        // TODO-ARM64-Bug?: set the availMask properly!
        regMaskTP availMask =
            (regSet.rsGetModifiedRegsMask() & RBM_ALLINT) | RBM_R12 | RBM_LR; // Set of available registers
        availMask &= ~maskArgRegsLiveIn;   // Remove all of the incoming argument registers as they are currently live
        availMask &= ~genRegMask(initReg); // Remove the pre-calculated initReg

        regNumber rOffset = initReg;
        regNumber rLimit;
        regNumber rTemp;
        regMaskTP tempMask;

        // We pick the next lowest register number for rTemp
        noway_assert(availMask != RBM_NONE);
        tempMask = genFindLowestBit(availMask);
        rTemp    = genRegNumFromMask(tempMask);
        availMask &= ~tempMask;

        // We pick the next lowest register number for rLimit
        noway_assert(availMask != RBM_NONE);
        tempMask = genFindLowestBit(availMask);
        rLimit   = genRegNumFromMask(tempMask);
        availMask &= ~tempMask;

        // TODO-LdStArch-Bug?: review this. The first time we load from [sp+0] which will always succeed. That doesn't
        // make sense.
        // TODO-ARM64-CQ: we could probably use ZR on ARM64 instead of rTemp.
        //
        //      mov rLimit, -frameSize
        // loop:
        //      ldr rTemp, [sp+rOffset]
        //      sub rOffset, 0x1000     // Note that 0x1000 on ARM32 uses the funky Thumb immediate encoding
        //      cmp rOffset, rLimit
        //      jge loop
        noway_assert((ssize_t)(int)frameSize == (ssize_t)frameSize); // make sure framesize safely fits within an int
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, rLimit, -(int)frameSize);
        getEmitter()->emitIns_R_R_R(INS_ldr, EA_4BYTE, rTemp, REG_SPBASE, rOffset);
        regTracker.rsTrackRegTrash(rTemp);
#if defined(_TARGET_ARM_)
        getEmitter()->emitIns_R_I(INS_sub, EA_PTRSIZE, rOffset, pageSize);
#elif defined(_TARGET_ARM64_)
        getEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, rOffset, rOffset, pageSize);
#endif // _TARGET_ARM64_
        getEmitter()->emitIns_R_R(INS_cmp, EA_PTRSIZE, rOffset, rLimit);
        getEmitter()->emitIns_J(INS_bhi, NULL, -4);

#else // !CPU_LOAD_STORE_ARCH

        // Code size for each instruction. We need this because the
        // backward branch is hard-coded with the number of bytes to branch.
        // The encoding differs based on the architecture and what register is
        // used (namely, using RAX has a smaller encoding).
        //
        // loop:
        // For x86
        //      test [esp + eax], eax       3
        //      sub eax, 0x1000             5
        //      cmp EAX, -frameSize         5
        //      jge loop                    2
        //
        // For AMD64 using RAX
        //      test [rsp + rax], rax       4
        //      sub rax, 0x1000             6
        //      cmp rax, -frameSize         6
        //      jge loop                    2
        //
        // For AMD64 using RBP
        //      test [rsp + rbp], rbp       4
        //      sub rbp, 0x1000             7
        //      cmp rbp, -frameSize         7
        //      jge loop                    2

        getEmitter()->emitIns_R_ARR(INS_TEST, EA_PTRSIZE, initReg, REG_SPBASE, initReg, 0);
        inst_RV_IV(INS_sub, initReg, pageSize, EA_PTRSIZE);
        inst_RV_IV(INS_cmp, initReg, -((ssize_t)frameSize), EA_PTRSIZE);

        int bytesForBackwardJump;
#ifdef _TARGET_AMD64_
        assert((initReg == REG_EAX) || (initReg == REG_EBP)); // We use RBP as initReg for EH funclets.
        bytesForBackwardJump = ((initReg == REG_EAX) ? -18 : -20);
#else  // !_TARGET_AMD64_
        assert(initReg == REG_EAX);
        bytesForBackwardJump = -15;
#endif // !_TARGET_AMD64_

        inst_IV(INS_jge, bytesForBackwardJump); // Branch backwards to start of loop

#endif // !CPU_LOAD_STORE_ARCH

        *pInitRegZeroed = false; // The initReg does not contain zero

#ifdef _TARGET_XARCH_
        if (pushedStubParam)
        {
            // pop eax
            inst_RV(INS_pop, REG_SECRET_STUB_PARAM, TYP_I_IMPL);
            regTracker.rsTrackRegTrash(REG_SECRET_STUB_PARAM);
        }
#endif // _TARGET_XARCH_

#if CPU_LOAD_STORE_ARCH
        compiler->unwindPadding();
#endif

#if CPU_LOAD_STORE_ARCH
#ifndef _TARGET_ARM64_
        inst_RV_RV(INS_add, REG_SPBASE, rLimit, TYP_I_IMPL);
#endif // !_TARGET_ARM64_
#else
        //      sub esp, frameSize   6
        inst_RV_IV(INS_sub, REG_SPBASE, frameSize, EA_PTRSIZE);
#endif
    }

#ifndef _TARGET_ARM64_
    compiler->unwindAllocStack(frameSize);

    if (!doubleAlignOrFramePointerUsed())
    {
        psiAdjustStackLevel(frameSize);
    }
#endif // !_TARGET_ARM64_
}

#if defined(_TARGET_ARM_)

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

    getEmitter()->emitIns_R_I(INS_vpush, EA_8BYTE, lowReg, slots / 2);
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

    getEmitter()->emitIns_R_I(INS_vpop, EA_8BYTE, lowReg, slots / 2);
}

/*-----------------------------------------------------------------------------
 *
 *  If we have a jmp call, then the argument registers cannot be used in the
 *  epilog. So return the current call's argument registers as the argument
 *  registers for the jmp call.
 */
regMaskTP CodeGen::genJmpCallArgMask()
{
    assert(compiler->compGeneratingEpilog);

    regMaskTP argMask = RBM_NONE;
    for (unsigned varNum = 0; varNum < compiler->info.compArgsCount; ++varNum)
    {
        const LclVarDsc& desc = compiler->lvaTable[varNum];
        if (desc.lvIsRegArg)
        {
            argMask |= genRegMask(desc.lvArgReg);
        }
    }
    return argMask;
}

/*-----------------------------------------------------------------------------
 *
 *  Free the local stack frame: add to SP.
 *  If epilog unwind hasn't been started, and we generate code, we start unwind
 *  and set *pUnwindStarted = true.
 */

void CodeGen::genFreeLclFrame(unsigned frameSize, /* IN OUT */ bool* pUnwindStarted, bool jmpEpilog)
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

        getEmitter()->emitIns_R_I(INS_add, EA_PTRSIZE, REG_SPBASE, frameSize, INS_FLAGS_DONT_CARE);
    }
    else
    {
        regMaskTP grabMask = RBM_INT_CALLEE_TRASH;
        if (jmpEpilog)
        {
            // Do not use argument registers as scratch registers in the jmp epilog.
            grabMask &= ~genJmpCallArgMask();
        }
#ifndef LEGACY_BACKEND
        regNumber tmpReg;
        tmpReg = REG_TMP_0;
#else  // LEGACY_BACKEND
        regNumber tmpReg = regSet.rsGrabReg(grabMask);
#endif // LEGACY_BACKEND
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

        getEmitter()->emitIns_R_R(INS_add, EA_PTRSIZE, REG_SPBASE, tmpReg, INS_FLAGS_DONT_CARE);
    }

    compiler->unwindAllocStack(frameSize);
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

#endif // _TARGET_ARM_

#if !FEATURE_STACK_FP_X87

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
#ifdef _TARGET_ARM_
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
#elif defined(_TARGET_XARCH_)
                // Xorpd xmmreg, xmmreg is the fastest way to initialize a float register to
                // zero instead of moving constant 0.0f.  Though we just need to initialize just the 32-bits
                // we will use xorpd to initialize 64-bits of the xmm register so that it can be
                // used to zero initialize xmm registers that hold double values.
                inst_RV_RV(INS_xorpd, reg, reg, TYP_DOUBLE);
                dblInitReg = reg;
#elif defined(_TARGET_ARM64_)
                NYI("Initialize floating-point register to zero");
#else // _TARGET_*
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
#ifdef _TARGET_ARM_
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
#elif defined(_TARGET_XARCH_)
                // Xorpd xmmreg, xmmreg is the fastest way to initialize a double register to
                // zero than moving constant 0.0d.  We can also use lower 32-bits of 'reg'
                // for zero initializing xmm registers subsequently that contain float values.
                inst_RV_RV(INS_xorpd, reg, reg, TYP_DOUBLE);
                fltInitReg = reg;
#elif defined(_TARGET_ARM64_)
                // We will just zero out the entire vector register. This sets it to a double zero value
                getEmitter()->emitIns_R_I(INS_movi, EA_16BYTE, reg, 0x00, INS_OPTS_16B);
#else // _TARGET_*
#error Unsupported or unset target architecture
#endif
                dblInitReg = reg;
            }
        }
    }
}
#endif // !FEATURE_STACK_FP_X87

/*-----------------------------------------------------------------------------
 *
 *  Restore any callee-saved registers we have used
 */

#if defined(_TARGET_ARM_)

bool CodeGen::genCanUsePopToReturn(regMaskTP maskPopRegsInt, bool jmpEpilog)
{
    assert(compiler->compGeneratingEpilog);

#ifdef ARM_HAZARD_AVOIDANCE
    // Only need to handle the Krait Hazard when we are Jitting
    //
    if ((compiler->opts.eeFlags & CORJIT_FLG_PREJIT) == 0)
    {
        // We will never generate the T2 encoding of pop when we have a Krait Errata
        if ((maskPopRegsInt & RBM_HIGH_REGS) != 0)
            return false;
    }
#endif

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

#elif defined(_TARGET_ARM64_)

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

    int calleeSaveSPOffset; // This will be the starting place for restoring the callee-saved registers, in decreasing
                            // order.
    int frameType                  = 0; // An indicator of what type of frame we are popping.
    int calleeSaveSPDelta          = 0;
    int calleeSaveSPDeltaUnaligned = 0;

    if (isFramePointerUsed())
    {
        if ((compiler->lvaOutgoingArgSpaceSize == 0) && (totalFrameSize < 512))
        {
            frameType = 1;
            if (compiler->compLocallocUsed)
            {
                // Restore sp from fp
                //      mov sp, fp
                inst_RV_RV(INS_mov, REG_SPBASE, REG_FPBASE);
                compiler->unwindSetFrameReg(REG_FPBASE, 0);
            }

            regsToRestoreMask &= ~(RBM_FP | RBM_LR); // We'll restore FP/LR at the end, and post-index SP.

            // Compute callee save SP offset which is at the top of local frame while the FP/LR is saved at the bottom
            // of stack.
            calleeSaveSPOffset = compiler->compLclFrameSize + 2 * REGSIZE_BYTES;
        }
        else if (totalFrameSize <= 512)
        {
            frameType = 2;
            if (compiler->compLocallocUsed)
            {
                // Restore sp from fp
                //      sub sp, fp, #outsz
                getEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_FPBASE,
                                            compiler->lvaOutgoingArgSpaceSize);
                compiler->unwindSetFrameReg(REG_FPBASE, compiler->lvaOutgoingArgSpaceSize);
            }

            regsToRestoreMask &= ~(RBM_FP | RBM_LR); // We'll restore FP/LR at the end, and post-index SP.

            // Compute callee save SP offset which is at the top of local frame while the FP/LR is saved at the bottom
            // of stack.
            calleeSaveSPOffset = compiler->compLclFrameSize + 2 * REGSIZE_BYTES;
        }
        else
        {
            frameType = 3;

            calleeSaveSPDeltaUnaligned = totalFrameSize - compiler->compLclFrameSize -
                                         2 * REGSIZE_BYTES; // 2 for FP, LR which we'll restore later.
            assert(calleeSaveSPDeltaUnaligned >= 0);
            assert((calleeSaveSPDeltaUnaligned % 8) == 0); // It better at least be 8 byte aligned.
            calleeSaveSPDelta = AlignUp((UINT)calleeSaveSPDeltaUnaligned, STACK_ALIGN);

            regsToRestoreMask &= ~(RBM_FP | RBM_LR); // We'll restore FP/LR at the end, and (hopefully) post-index SP.

            int remainingFrameSz = totalFrameSize - calleeSaveSPDelta;
            assert(remainingFrameSz > 0);

            if (compiler->lvaOutgoingArgSpaceSize >= 504)
            {
                // We can't do "ldp fp,lr,[sp,#outsz]" because #outsz is too big.
                // If compiler->lvaOutgoingArgSpaceSize is not aligned, we need to align the SP adjustment.
                assert(remainingFrameSz > (int)compiler->lvaOutgoingArgSpaceSize);
                int spAdjustment2Unaligned = remainingFrameSz - compiler->lvaOutgoingArgSpaceSize;
                int spAdjustment2          = (int)roundUp((size_t)spAdjustment2Unaligned, STACK_ALIGN);
                int alignmentAdjustment2   = spAdjustment2 - spAdjustment2Unaligned;
                assert((alignmentAdjustment2 == 0) || (alignmentAdjustment2 == REGSIZE_BYTES));

                if (compiler->compLocallocUsed)
                {
                    // Restore sp from fp. No need to update sp after this since we've set up fp before adjusting sp in
                    // prolog.
                    //      sub sp, fp, #alignmentAdjustment2
                    getEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, alignmentAdjustment2);
                    compiler->unwindSetFrameReg(REG_FPBASE, alignmentAdjustment2);
                }
                else
                {
                    // Generate:
                    //      add sp,sp,#outsz                ; if #outsz is not 16-byte aligned, we need to be more
                    //                                      ; careful
                    int spAdjustment3 = compiler->lvaOutgoingArgSpaceSize - alignmentAdjustment2;
                    assert(spAdjustment3 > 0);
                    assert((spAdjustment3 % 16) == 0);
                    genStackPointerAdjustment(spAdjustment3, REG_IP0, nullptr);
                }

                // Generate:
                //      ldp fp,lr,[sp]
                //      add sp,sp,#remainingFrameSz
                genEpilogRestoreRegPair(REG_FP, REG_LR, alignmentAdjustment2, spAdjustment2, REG_IP0, nullptr);
            }
            else
            {
                if (compiler->compLocallocUsed)
                {
                    // Restore sp from fp
                    //      sub sp, fp, #outsz
                    getEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_FPBASE,
                                                compiler->lvaOutgoingArgSpaceSize);
                    compiler->unwindSetFrameReg(REG_FPBASE, compiler->lvaOutgoingArgSpaceSize);
                }

                // Generate:
                //      ldp fp,lr,[sp,#outsz]
                //      add sp,sp,#remainingFrameSz     ; might need to load this constant in a scratch register if
                //                                      ; it's large

                genEpilogRestoreRegPair(REG_FP, REG_LR, compiler->lvaOutgoingArgSpaceSize, remainingFrameSz, REG_IP0,
                                        nullptr);
            }

            // Unlike frameType=1 or frameType=2 that restore SP at the end,
            // frameType=3 already adjusted SP above to delete local frame.
            // There is at most one alignment slot between SP and where we store the callee-saved registers.
            calleeSaveSPOffset = calleeSaveSPDelta - calleeSaveSPDeltaUnaligned;
            assert((calleeSaveSPOffset == 0) || (calleeSaveSPOffset == REGSIZE_BYTES));
        }
    }
    else
    {
        // No frame pointer (no chaining).
        NYI("Frame without frame pointer");
        calleeSaveSPOffset = 0;
    }

    genRestoreCalleeSavedRegistersHelp(regsToRestoreMask, calleeSaveSPOffset, calleeSaveSPDelta);

    if (frameType == 1)
    {
        // Generate:
        //      ldp fp,lr,[sp],#framesz

        getEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, totalFrameSize,
                                      INS_OPTS_POST_INDEX);
        compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, -totalFrameSize);
    }
    else if (frameType == 2)
    {
        // Generate:
        //      ldr fp,lr,[sp,#outsz]
        //      add sp,sp,#framesz

        getEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE,
                                      compiler->lvaOutgoingArgSpaceSize);
        compiler->unwindSaveRegPair(REG_FP, REG_LR, compiler->lvaOutgoingArgSpaceSize);

        getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, totalFrameSize);
        compiler->unwindAllocStack(totalFrameSize);
    }
    else if (frameType == 3)
    {
        // Nothing to do after restoring callee-saved registers.
    }
    else
    {
        unreached();
    }
}

#elif defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87

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

#ifdef _TARGET_AMD64_
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
#endif // _TARGET_AMD64_

    // Amd64/x86 doesn't support push/pop of xmm registers.
    // These will get saved to stack separately after allocating
    // space on stack in prolog sequence.  PopCount is essentially
    // tracking the count of integer registers pushed.

    noway_assert(compiler->compCalleeRegsPushed == popCount);
}

#elif defined(_TARGET_X86_)

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

#endif // _TARGET_*

// We need a register with value zero. Zero the initReg, if necessary, and set *pInitRegZeroed if so.
// Return the register to use. On ARM64, we never touch the initReg, and always just return REG_ZR.
regNumber CodeGen::genGetZeroReg(regNumber initReg, bool* pInitRegZeroed)
{
#ifdef _TARGET_ARM64_
    return REG_ZR;
#else  // !_TARGET_ARM64_
    if (*pInitRegZeroed == false)
    {
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, initReg);
        *pInitRegZeroed = true;
    }
    return initReg;
#endif // !_TARGET_ARM64_
}

/*-----------------------------------------------------------------------------
 *
 * Do we have any untracked pointer locals at all,
 * or do we need to initialize memory for locspace?
 *
 * untrLclHi      - (Untracked locals High-Offset)   The upper bound offset at which the zero init code will end
 * initializing memory (not inclusive).
 * untrLclLo      - (Untracked locals Low-Offset)    The lower bound at which the zero init code will start zero
 * initializing memory.
 * initReg        - A scratch register (that gets set to zero on some platforms).
 * pInitRegZeroed - Sets a flag that tells the callee whether or not the initReg register got zeroed.
 */
void CodeGen::genZeroInitFrame(int untrLclHi, int untrLclLo, regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    if (genUseBlockInit)
    {
        assert(untrLclHi > untrLclLo);
#ifdef _TARGET_ARMARCH_
        /*
            Generate the following code:

            For cnt less than 10

                mov     rZero1, 0
                mov     rZero2, 0
                mov     rCnt,  <cnt>
                stm     <rZero1,rZero2>,[rAddr!]
    <optional>  stm     <rZero1,rZero2>,[rAddr!]
    <optional>  stm     <rZero1,rZero2>,[rAddr!]
    <optional>  stm     <rZero1,rZero2>,[rAddr!]
    <optional>  str     rZero1,[rAddr]

            For rCnt greater than or equal to 10

                mov     rZero1, 0
                mov     rZero2, 0
                mov     rCnt,  <cnt/2>
                sub     rAddr, sp, OFFS

            loop:
                stm     <rZero1,rZero2>,[rAddr!]
                sub     rCnt,rCnt,1
                jnz     loop

    <optional>  str     rZero1,[rAddr]   // When cnt is odd

            NOTE: for ARM64, the instruction is stp, not stm. And we can use ZR instead of allocating registers.
         */

        regNumber rAddr;
        regNumber rCnt = REG_NA; // Invalid
        regMaskTP regMask;

        regMaskTP availMask = regSet.rsGetModifiedRegsMask() | RBM_INT_CALLEE_TRASH; // Set of available registers
        availMask &= ~intRegState.rsCalleeRegArgMaskLiveIn; // Remove all of the incoming argument registers as they are
                                                            // currently live
        availMask &= ~genRegMask(initReg); // Remove the pre-calculated initReg as we will zero it and maybe use it for
                                           // a large constant.

#if defined(_TARGET_ARM_)

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
        assert((genRegMask(rZero2) & intRegState.rsCalleeRegArgMaskLiveIn) ==
               0); // rZero2 is not a live incoming argument reg

        // We pick the next lowest register number for rAddr
        noway_assert(availMask != RBM_NONE);
        regMask = genFindLowestBit(availMask);
        rAddr   = genRegNumFromMask(regMask);
        availMask &= ~regMask;

#else // !define(_TARGET_ARM_)

        regNumber rZero1 = REG_ZR;
        rAddr            = initReg;
        *pInitRegZeroed  = false;

#endif // !defined(_TARGET_ARM_)

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

        assert((genRegMask(rAddr) & intRegState.rsCalleeRegArgMaskLiveIn) ==
               0); // rAddr is not a live incoming argument reg
#if defined(_TARGET_ARM_)
        if (arm_Valid_Imm_For_Add(untrLclLo, INS_FLAGS_DONT_CARE))
#else  // !_TARGET_ARM_
        if (emitter::emitIns_valid_imm_for_add(untrLclLo, EA_PTRSIZE))
#endif // !_TARGET_ARM_
        {
            getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, rAddr, genFramePointerReg(), untrLclLo);
        }
        else
        {
            // Load immediate into the InitReg register
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, (ssize_t)untrLclLo);
            getEmitter()->emitIns_R_R_R(INS_add, EA_PTRSIZE, rAddr, genFramePointerReg(), initReg);
            *pInitRegZeroed = false;
        }

        if (useLoop)
        {
            noway_assert(uCntSlots >= 2);
            assert((genRegMask(rCnt) & intRegState.rsCalleeRegArgMaskLiveIn) ==
                   0); // rCnt is not a live incoming argument reg
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, rCnt, (ssize_t)uCntSlots / 2);
        }

#if defined(_TARGET_ARM_)
        rZero1 = genGetZeroReg(initReg, pInitRegZeroed);
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, rZero2);
        ssize_t stmImm = (ssize_t)(genRegMask(rZero1) | genRegMask(rZero2));
#endif // _TARGET_ARM_

        if (!useLoop)
        {
            while (uCntBytes >= REGSIZE_BYTES * 2)
            {
#ifdef _TARGET_ARM_
                getEmitter()->emitIns_R_I(INS_stm, EA_PTRSIZE, rAddr, stmImm);
#else  // !_TARGET_ARM_
                getEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_ZR, REG_ZR, rAddr, 2 * REGSIZE_BYTES,
                                              INS_OPTS_POST_INDEX);
#endif // !_TARGET_ARM_
                uCntBytes -= REGSIZE_BYTES * 2;
            }
        }
        else // useLoop is true
        {
#ifdef _TARGET_ARM_
            getEmitter()->emitIns_R_I(INS_stm, EA_PTRSIZE, rAddr, stmImm); // zero stack slots
            getEmitter()->emitIns_R_I(INS_sub, EA_PTRSIZE, rCnt, 1, INS_FLAGS_SET);
#else  // !_TARGET_ARM_
            getEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_ZR, REG_ZR, rAddr, 2 * REGSIZE_BYTES,
                                          INS_OPTS_POST_INDEX); // zero stack slots
            getEmitter()->emitIns_R_R_I(INS_subs, EA_PTRSIZE, rCnt, rCnt, 1);
#endif // !_TARGET_ARM_
            getEmitter()->emitIns_J(INS_bhi, NULL, -3);
            uCntBytes %= REGSIZE_BYTES * 2;
        }

        if (uCntBytes >= REGSIZE_BYTES) // check and zero the last register-sized stack slot (odd number)
        {
#ifdef _TARGET_ARM_
            getEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, rZero1, rAddr, 0);
#else  // _TARGET_ARM_
            if ((uCntBytes - REGSIZE_BYTES) == 0)
            {
                getEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, REG_ZR, rAddr, 0);
            }
            else
            {
                getEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, REG_ZR, rAddr, REGSIZE_BYTES, INS_OPTS_POST_INDEX);
            }
#endif // !_TARGET_ARM_
            uCntBytes -= REGSIZE_BYTES;
        }
#ifdef _TARGET_ARM64_
        if (uCntBytes > 0)
        {
            assert(uCntBytes == sizeof(int));
            getEmitter()->emitIns_R_R_I(INS_str, EA_4BYTE, REG_ZR, rAddr, 0);
            uCntBytes -= sizeof(int);
        }
#endif // _TARGET_ARM64_
        noway_assert(uCntBytes == 0);

#elif defined(_TARGET_XARCH_)
        /*
            Generate the following code:

                lea     edi, [ebp/esp-OFFS]
                mov     ecx, <size>
                xor     eax, eax
                rep     stosd
         */

        noway_assert(regSet.rsRegsModified(RBM_EDI));

#ifdef UNIX_AMD64_ABI
        // For register arguments we may have to save ECX and RDI on Amd64 System V OSes
        if (intRegState.rsCalleeRegArgMaskLiveIn & RBM_RCX)
        {
            noway_assert(regSet.rsRegsModified(RBM_R12));
            inst_RV_RV(INS_mov, REG_R12, REG_RCX);
            regTracker.rsTrackRegTrash(REG_R12);
        }

        if (intRegState.rsCalleeRegArgMaskLiveIn & RBM_RDI)
        {
            noway_assert(regSet.rsRegsModified(RBM_R13));
            inst_RV_RV(INS_mov, REG_R13, REG_RDI);
            regTracker.rsTrackRegTrash(REG_R13);
        }
#else  // !UNIX_AMD64_ABI
        // For register arguments we may have to save ECX
        if (intRegState.rsCalleeRegArgMaskLiveIn & RBM_ECX)
        {
            noway_assert(regSet.rsRegsModified(RBM_ESI));
            inst_RV_RV(INS_mov, REG_ESI, REG_ECX);
            regTracker.rsTrackRegTrash(REG_ESI);
        }
#endif // !UNIX_AMD64_ABI

        noway_assert((intRegState.rsCalleeRegArgMaskLiveIn & RBM_EAX) == 0);

        getEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_EDI, genFramePointerReg(), untrLclLo);
        regTracker.rsTrackRegTrash(REG_EDI);

        inst_RV_IV(INS_mov, REG_ECX, (untrLclHi - untrLclLo) / sizeof(int), EA_4BYTE);
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, REG_EAX);
        instGen(INS_r_stosd);

#ifdef UNIX_AMD64_ABI
        // Move back the argument registers
        if (intRegState.rsCalleeRegArgMaskLiveIn & RBM_RCX)
        {
            inst_RV_RV(INS_mov, REG_RCX, REG_R12);
        }

        if (intRegState.rsCalleeRegArgMaskLiveIn & RBM_RDI)
        {
            inst_RV_RV(INS_mov, REG_RDI, REG_R13);
        }
#else  // !UNIX_AMD64_ABI
        // Move back the argument registers
        if (intRegState.rsCalleeRegArgMaskLiveIn & RBM_ECX)
        {
            inst_RV_RV(INS_mov, REG_ECX, REG_ESI);
        }
#endif // !UNIX_AMD64_ABI

#else // _TARGET_*
#error Unsupported or unset target architecture
#endif // _TARGET_*
    }
    else if (genInitStkLclCnt > 0)
    {
        assert((genRegMask(initReg) & intRegState.rsCalleeRegArgMaskLiveIn) ==
               0); // initReg is not a live incoming argument reg

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

#ifdef _TARGET_64BIT_
            if (!varDsc->lvOnFrame)
            {
                continue;
            }
#else  // !_TARGET_64BIT_
            if (varDsc->lvRegister)
            {
                if (varDsc->lvOnFrame)
                {
                    /* This is a partially enregistered TYP_LONG var */
                    noway_assert(varDsc->lvOtherReg == REG_STK);
                    noway_assert(varDsc->lvType == TYP_LONG);

                    noway_assert(compiler->info.compInitMem);

                    getEmitter()->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, genGetZeroReg(initReg, pInitRegZeroed),
                                              varNum, sizeof(int));
                }
                continue;
            }
#endif // !_TARGET_64BIT_

            if ((varDsc->TypeGet() == TYP_STRUCT) && !compiler->info.compInitMem &&
                (varDsc->lvExactSize >= TARGET_POINTER_SIZE))
            {
                // We only initialize the GC variables in the TYP_STRUCT
                const unsigned slots  = (unsigned)compiler->lvaLclSize(varNum) / REGSIZE_BYTES;
                const BYTE*    gcPtrs = compiler->lvaGetGcLayout(varNum);

                for (unsigned i = 0; i < slots; i++)
                {
                    if (gcPtrs[i] != TYPE_GC_NONE)
                    {
                        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE,
                                                  genGetZeroReg(initReg, pInitRegZeroed), varNum, i * REGSIZE_BYTES);
                    }
                }
            }
            else
            {
                regNumber zeroReg = genGetZeroReg(initReg, pInitRegZeroed);

                // zero out the whole thing rounded up to a single stack slot size
                unsigned lclSize = (unsigned)roundUp(compiler->lvaLclSize(varNum), sizeof(int));
                unsigned i;
                for (i = 0; i + REGSIZE_BYTES <= lclSize; i += REGSIZE_BYTES)
                {
                    getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, zeroReg, varNum, i);
                }

#ifdef _TARGET_64BIT_
                assert(i == lclSize || (i + sizeof(int) == lclSize));
                if (i != lclSize)
                {
                    getEmitter()->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, zeroReg, varNum, i);
                    i += sizeof(int);
                }
#endif // _TARGET_64BIT_
                assert(i == lclSize);
            }
        }

        if (!TRACK_GC_TEMP_LIFETIMES)
        {
            assert(compiler->tmpAllFree());
            for (TempDsc* tempThis = compiler->tmpListBeg(); tempThis != nullptr;
                 tempThis          = compiler->tmpListNxt(tempThis))
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
#if defined(_TARGET_ARM_) && defined(PROFILING_SUPPORTED)
    isPrespilledForProfiling =
        compiler->compIsProfilerHookNeeded() && compiler->lvaIsPreSpilled(contextArg, regSet.rsMaskPreSpillRegs(false));
#endif

    // Load from the argument register only if it is not prespilled.
    if (compiler->lvaIsRegArgument(contextArg) && !isPrespilledForProfiling)
    {
        reg = varDsc->lvArgReg;
    }
    else
    {
        if (isFramePointerUsed())
        {
#if defined(_TARGET_ARM_)
            // lvStkOffs is always valid for incoming stack-arguments, even if the argument
            // will become enregistered.
            // On Arm compiler->compArgSize doesn't include r11 and lr sizes and hence we need to add 2*REGSIZE_BYTES
            noway_assert((2 * REGSIZE_BYTES <= varDsc->lvStkOffs) &&
                         (size_t(varDsc->lvStkOffs) < compiler->compArgSize + 2 * REGSIZE_BYTES));
#else
            // lvStkOffs is always valid for incoming stack-arguments, even if the argument
            // will become enregistered.
            noway_assert((0 < varDsc->lvStkOffs) && (size_t(varDsc->lvStkOffs) < compiler->compArgSize));
#endif
        }

        // We will just use the initReg since it is an available register
        // and we are probably done using it anyway...
        reg             = initReg;
        *pInitRegZeroed = false;

        // mov reg, [compiler->info.compTypeCtxtArg]
        getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, reg, genFramePointerReg(), varDsc->lvStkOffs);
        regTracker.rsTrackRegTrash(reg);
    }

#if CPU_LOAD_STORE_ARCH
    getEmitter()->emitIns_R_R_I(ins_Store(TYP_I_IMPL), EA_PTRSIZE, reg, genFramePointerReg(),
                                compiler->lvaCachedGenericContextArgOffset());
#else  // CPU_LOAD_STORE_ARCH
    // mov [ebp-lvaCachedGenericContextArgOffset()], reg
    getEmitter()->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, reg, genFramePointerReg(),
                               compiler->lvaCachedGenericContextArgOffset());
#endif // !CPU_LOAD_STORE_ARCH
}

/*-----------------------------------------------------------------------------
 *
 *  Set the "GS" security cookie in the prolog.
 */

void CodeGen::genSetGSSecurityCookie(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    if (!compiler->getNeedsGSSecurityCookie())
    {
        return;
    }

    noway_assert(compiler->gsGlobalSecurityCookieAddr || compiler->gsGlobalSecurityCookieVal);

    if (compiler->gsGlobalSecurityCookieAddr == nullptr)
    {
#ifdef _TARGET_AMD64_
        // eax = #GlobalSecurityCookieVal64; [frame.GSSecurityCookie] = eax
        getEmitter()->emitIns_R_I(INS_mov, EA_PTRSIZE, REG_RAX, compiler->gsGlobalSecurityCookieVal);
        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_RAX, compiler->lvaGSSecurityCookie, 0);
#else
        //  mov   dword ptr [frame.GSSecurityCookie], #GlobalSecurityCookieVal
        instGen_Store_Imm_Into_Lcl(TYP_I_IMPL, EA_PTRSIZE, compiler->gsGlobalSecurityCookieVal,
                                   compiler->lvaGSSecurityCookie, 0, initReg);
#endif
    }
    else
    {
        regNumber reg;
#ifdef _TARGET_XARCH_
        // Always use EAX on x86 and x64
        // On x64, if we're not moving into RAX, and the address isn't RIP relative, we can't encode it.
        reg = REG_EAX;
#else
        // We will just use the initReg since it is an available register
        reg = initReg;
#endif

        *pInitRegZeroed = false;

#if CPU_LOAD_STORE_ARCH
        instGen_Set_Reg_To_Imm(EA_PTR_DSP_RELOC, reg, (ssize_t)compiler->gsGlobalSecurityCookieAddr);
        getEmitter()->emitIns_R_R_I(ins_Load(TYP_I_IMPL), EA_PTRSIZE, reg, reg, 0);
        regTracker.rsTrackRegTrash(reg);
#else
        //  mov   reg, dword ptr [compiler->gsGlobalSecurityCookieAddr]
        //  mov   dword ptr [frame.GSSecurityCookie], reg
        getEmitter()->emitIns_R_AI(INS_mov, EA_PTR_DSP_RELOC, reg, (ssize_t)compiler->gsGlobalSecurityCookieAddr);
        regTracker.rsTrackRegTrash(reg);
#endif
        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, reg, compiler->lvaGSSecurityCookie, 0);
    }
}

#ifdef PROFILING_SUPPORTED

/*-----------------------------------------------------------------------------
 *
 *  Generate the profiling function enter callback.
 */

void CodeGen::genProfilingEnterCallback(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    // Give profiler a chance to back out of hooking this method
    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

#ifndef LEGACY_BACKEND
#if defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI) // No profiling for System V systems yet.
    unsigned   varNum;
    LclVarDsc* varDsc;

    // Since the method needs to make a profiler callback, it should have out-going arg space allocated.
    noway_assert(compiler->lvaOutgoingArgSpaceVar != BAD_VAR_NUM);
    noway_assert(compiler->lvaOutgoingArgSpaceSize >= (4 * REGSIZE_BYTES));

    // Home all arguments passed in arg registers (RCX, RDX, R8 and R9).
    // In case of vararg methods, arg regs are already homed.
    //
    // Note: Here we don't need to worry about updating gc'info since enter
    // callback is generated as part of prolog which is non-gc interruptible.
    // Moreover GC cannot kick while executing inside profiler callback which is a
    // profiler requirement so it can examine arguments which could be obj refs.
    if (!compiler->info.compIsVarArgs)
    {
        for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->info.compArgsCount; varNum++, varDsc++)
        {
            noway_assert(varDsc->lvIsParam);

            if (!varDsc->lvIsRegArg)
            {
                continue;
            }

            var_types storeType = varDsc->lvaArgType();
            regNumber argReg    = varDsc->lvArgReg;
            getEmitter()->emitIns_S_R(ins_Store(storeType), emitTypeSize(storeType), argReg, varNum, 0);
        }
    }

    // Emit profiler EnterCallback(ProfilerMethHnd, caller's SP)
    // RCX = ProfilerMethHnd
    if (compiler->compProfilerMethHndIndirected)
    {
        // Profiler hooks enabled during Ngen time.
        // Profiler handle needs to be accessed through an indirection of a pointer.
        getEmitter()->emitIns_R_AI(INS_mov, EA_PTR_DSP_RELOC, REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
    }
    else
    {
        // No need to record relocations, if we are generating ELT hooks under the influence
        // of complus_JitELtHookEnabled=1
        if (compiler->opts.compJitELTHookEnabled)
        {
            genSetRegToIcon(REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd, TYP_I_IMPL);
        }
        else
        {
            instGen_Set_Reg_To_Imm(EA_8BYTE, REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
        }
    }

    // RDX = caller's SP
    // Notes
    //   1) Here we can query caller's SP offset since prolog will be generated after final frame layout.
    //   2) caller's SP relative offset to FramePointer will be negative.  We need to add absolute value
    //      of that offset to FramePointer to obtain caller's SP value.
    assert(compiler->lvaOutgoingArgSpaceVar != BAD_VAR_NUM);
    int callerSPOffset = compiler->lvaToCallerSPRelativeOffset(0, isFramePointerUsed());
    getEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_ARG_1, genFramePointerReg(), -callerSPOffset);

    // Can't have a call until we have enough padding for rejit
    genPrologPadForReJit();

    // This will emit either
    // "call ip-relative 32-bit offset" or
    // "mov rax, helper addr; call rax"
    genEmitHelperCall(CORINFO_HELP_PROF_FCN_ENTER, 0, EA_UNKNOWN);

    // TODO-AMD64-CQ: Rather than reloading, see if this could be optimized by combining with prolog
    // generation logic that moves args around as required by first BB entry point conditions
    // computed by LSRA.  Code pointers for investigating this further: genFnPrologCalleeRegArgs()
    // and genEnregisterIncomingStackArgs().
    //
    // Now reload arg registers from home locations.
    // Vararg methods:
    //   - we need to reload only known (i.e. fixed) reg args.
    //   - if floating point type, also reload it into corresponding integer reg
    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->info.compArgsCount; varNum++, varDsc++)
    {
        noway_assert(varDsc->lvIsParam);

        if (!varDsc->lvIsRegArg)
        {
            continue;
        }

        var_types loadType = varDsc->lvaArgType();
        regNumber argReg   = varDsc->lvArgReg;
        getEmitter()->emitIns_R_S(ins_Load(loadType), emitTypeSize(loadType), argReg, varNum, 0);

#if FEATURE_VARARG
        if (compiler->info.compIsVarArgs && varTypeIsFloating(loadType))
        {
            regNumber   intArgReg = compiler->getCallArgIntRegister(argReg);
            instruction ins       = ins_CopyFloatToInt(loadType, TYP_LONG);
            inst_RV_RV(ins, argReg, intArgReg, loadType);
        }
#endif //  FEATURE_VARARG
    }

    // If initReg is one of RBM_CALLEE_TRASH, then it needs to be zero'ed before using.
    if ((RBM_CALLEE_TRASH & genRegMask(initReg)) != 0)
    {
        *pInitRegZeroed = false;
    }

#else //!_TARGET_AMD64_
    NYI("RyuJIT: Emit Profiler Enter callback");
#endif

#else // LEGACY_BACKEND

    unsigned saveStackLvl2 = genStackLevel;

#if defined(_TARGET_X86_)
    // Important note: when you change enter probe layout, you must also update SKIP_ENTER_PROF_CALLBACK()
    // for x86 stack unwinding

    // Push the profilerHandle
    if (compiler->compProfilerMethHndIndirected)
    {
        getEmitter()->emitIns_AR_R(INS_push, EA_PTR_DSP_RELOC, REG_NA, REG_NA, (ssize_t)compiler->compProfilerMethHnd);
    }
    else
    {
        inst_IV(INS_push, (size_t)compiler->compProfilerMethHnd);
    }
#elif defined(_TARGET_ARM_)
    // On Arm arguments are prespilled on stack, which frees r0-r3.
    // For generating Enter callout we would need two registers and one of them has to be r0 to pass profiler handle.
    // The call target register could be any free register.
    regNumber argReg = regSet.rsGrabReg(RBM_PROFILER_ENTER_ARG);
    noway_assert(argReg == REG_PROFILER_ENTER_ARG);
    regSet.rsLockReg(RBM_PROFILER_ENTER_ARG);

    if (compiler->compProfilerMethHndIndirected)
    {
        getEmitter()->emitIns_R_AI(INS_ldr, EA_PTR_DSP_RELOC, argReg, (ssize_t)compiler->compProfilerMethHnd);
        regTracker.rsTrackRegTrash(argReg);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_4BYTE, argReg, (ssize_t)compiler->compProfilerMethHnd);
    }
#else  // _TARGET_*
    NYI("Pushing the profilerHandle & caller's sp for the profiler callout and locking registers");
#endif // _TARGET_*

    //
    // Can't have a call until we have enough padding for rejit
    //
    genPrologPadForReJit();

    // This will emit either
    // "call ip-relative 32-bit offset" or
    // "mov rax, helper addr; call rax"
    genEmitHelperCall(CORINFO_HELP_PROF_FCN_ENTER,
                      0,           // argSize. Again, we have to lie about it
                      EA_UNKNOWN); // retSize

#if defined(_TARGET_X86_)
    //
    // Adjust the number of stack slots used by this managed method if necessary.
    //
    if (compiler->fgPtrArgCntMax < 1)
    {
        compiler->fgPtrArgCntMax = 1;
    }
#elif defined(_TARGET_ARM_)
    // Unlock registers
    regSet.rsUnlockReg(RBM_PROFILER_ENTER_ARG);

    if (initReg == argReg)
    {
        *pInitRegZeroed = false;
    }
#else  // _TARGET_*
    NYI("Pushing the profilerHandle & caller's sp for the profiler callout and locking registers");
#endif // _TARGET_*

    /* Restore the stack level */

    genStackLevel = saveStackLvl2;
#endif // LEGACY_BACKEND
}

/*****************************************************************************
 *
 *  Generates Leave profiler hook.
 *  Technically, this is not part of the epilog; it is called when we are generating code for a GT_RETURN node.
 */

void CodeGen::genProfilingLeaveCallback(unsigned helper /*= CORINFO_HELP_PROF_FCN_LEAVE*/)
{
    // Only hook if profiler says it's okay.
    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

    compiler->info.compProfilerCallback = true;

    // Need to save on to the stack level, since the callee will pop the argument
    unsigned saveStackLvl2 = genStackLevel;

#ifndef LEGACY_BACKEND

#if defined(_TARGET_AMD64_) && !defined(UNIX_AMD64_ABI) // No profiling for System V systems yet.
    // Since the method needs to make a profiler callback, it should have out-going arg space allocated.
    noway_assert(compiler->lvaOutgoingArgSpaceVar != BAD_VAR_NUM);
    noway_assert(compiler->lvaOutgoingArgSpaceSize >= (4 * REGSIZE_BYTES));

    // If thisPtr needs to be kept alive and reported, it cannot be one of the callee trash
    // registers that profiler callback kills.
    if (compiler->lvaKeepAliveAndReportThis() && compiler->lvaTable[compiler->info.compThisArg].lvIsInReg())
    {
        regMaskTP thisPtrMask = genRegMask(compiler->lvaTable[compiler->info.compThisArg].lvRegNum);
        noway_assert((RBM_PROFILER_LEAVE_TRASH & thisPtrMask) == 0);
    }

    // At this point return value is computed and stored in RAX or XMM0.
    // On Amd64, Leave callback preserves the return register.  We keep
    // RAX alive by not reporting as trashed by helper call.  Also note
    // that GC cannot kick-in while executing inside profiler callback,
    // which is a requirement of profiler as well since it needs to examine
    // return value which could be an obj ref.

    // RCX = ProfilerMethHnd
    if (compiler->compProfilerMethHndIndirected)
    {
        // Profiler hooks enabled during Ngen time.
        // Profiler handle needs to be accessed through an indirection of an address.
        getEmitter()->emitIns_R_AI(INS_mov, EA_PTR_DSP_RELOC, REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
    }
    else
    {
        // Don't record relocations, if we are generating ELT hooks under the influence
        // of complus_JitELtHookEnabled=1
        if (compiler->opts.compJitELTHookEnabled)
        {
            genSetRegToIcon(REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd, TYP_I_IMPL);
        }
        else
        {
            instGen_Set_Reg_To_Imm(EA_8BYTE, REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
        }
    }

    // RDX = caller's SP
    // TODO-AMD64-Cleanup: Once we start doing codegen after final frame layout, retain the "if" portion
    // of the stmnts to execute unconditionally and clean-up rest.
    if (compiler->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT)
    {
        // Caller's SP relative offset to FramePointer will be negative.  We need to add absolute
        // value of that offset to FramePointer to obtain caller's SP value.
        int callerSPOffset = compiler->lvaToCallerSPRelativeOffset(0, isFramePointerUsed());
        getEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_ARG_1, genFramePointerReg(), -callerSPOffset);
    }
    else
    {
        // If we are here means that it is a tentative frame layout during which we
        // cannot use caller's SP offset since it is an estimate.  For now we require the
        // method to have at least a single arg so that we can use it to obtain caller's
        // SP.
        LclVarDsc* varDsc = compiler->lvaTable;
        NYI_IF((varDsc == nullptr) || !varDsc->lvIsParam, "Profiler ELT callback for a method without any params");

        // lea rdx, [FramePointer + Arg0's offset]
        getEmitter()->emitIns_R_S(INS_lea, EA_PTRSIZE, REG_ARG_1, 0, 0);
    }

    // We can use any callee trash register (other than RAX, RCX, RDX) for call target.
    // We use R8 here. This will emit either
    // "call ip-relative 32-bit offset" or
    // "mov r8, helper addr; call r8"
    genEmitHelperCall(helper, 0, EA_UNKNOWN, REG_ARG_2);

#else  //!_TARGET_AMD64_
    NYI("RyuJIT: Emit Profiler Leave callback");
#endif // _TARGET_*

#else // LEGACY_BACKEND

#if defined(_TARGET_X86_)
    //
    // Push the profilerHandle
    //

    if (compiler->compProfilerMethHndIndirected)
    {
        getEmitter()->emitIns_AR_R(INS_push, EA_PTR_DSP_RELOC, REG_NA, REG_NA, (ssize_t)compiler->compProfilerMethHnd);
    }
    else
    {
        inst_IV(INS_push, (size_t)compiler->compProfilerMethHnd);
    }
    genSinglePush();

    genEmitHelperCall(CORINFO_HELP_PROF_FCN_LEAVE,
                      sizeof(int) * 1, // argSize
                      EA_UNKNOWN);     // retSize

    //
    // Adjust the number of stack slots used by this managed method if necessary.
    //
    if (compiler->fgPtrArgCntMax < 1)
    {
        compiler->fgPtrArgCntMax = 1;
    }
#elif defined(_TARGET_ARM_)
    //
    // Push the profilerHandle
    //

    // We could optimize register usage based on return value is int/long/void. But to keep it simple we will lock
    // RBM_PROFILER_RET_USED always.
    regNumber scratchReg = regSet.rsGrabReg(RBM_PROFILER_RET_SCRATCH);
    noway_assert(scratchReg == REG_PROFILER_RET_SCRATCH);
    regSet.rsLockReg(RBM_PROFILER_RET_USED);

    // Contract between JIT and Profiler Leave callout on arm:
    // Return size <= 4 bytes: REG_PROFILER_RET_SCRATCH will contain return value
    // Return size > 4 and <= 8: <REG_PROFILER_RET_SCRATCH,r1> will contain return value.
    // Floating point or double or HFA return values will be in s0-s15 in case of non-vararg methods.
    // It is assumed that profiler Leave callback doesn't trash registers r1,REG_PROFILER_RET_SCRATCH and s0-s15.
    //
    // In the following cases r0 doesn't contain a return value and hence need not be preserved before emitting Leave
    // callback.
    bool     r0Trashed;
    emitAttr attr = EA_UNKNOWN;

    if (compiler->info.compRetType == TYP_VOID ||
        (!compiler->info.compIsVarArgs && (varTypeIsFloating(compiler->info.compRetType) ||
                                           compiler->IsHfa(compiler->info.compMethodInfo->args.retTypeClass))))
    {
        r0Trashed = false;
    }
    else
    {
        // Has a return value and r0 is in use. For emitting Leave profiler callout we would need r0 for passing
        // profiler handle. Therefore, r0 is moved to REG_PROFILER_RETURN_SCRATCH as per contract.
        if (RBM_ARG_0 & gcInfo.gcRegGCrefSetCur)
        {
            attr = EA_GCREF;
            gcInfo.gcMarkRegSetGCref(RBM_PROFILER_RET_SCRATCH);
        }
        else if (RBM_ARG_0 & gcInfo.gcRegByrefSetCur)
        {
            attr = EA_BYREF;
            gcInfo.gcMarkRegSetByref(RBM_PROFILER_RET_SCRATCH);
        }
        else
        {
            attr = EA_4BYTE;
        }

        getEmitter()->emitIns_R_R(INS_mov, attr, REG_PROFILER_RET_SCRATCH, REG_ARG_0);
        regTracker.rsTrackRegTrash(REG_PROFILER_RET_SCRATCH);
        gcInfo.gcMarkRegSetNpt(RBM_ARG_0);
        r0Trashed = true;
    }

    if (compiler->compProfilerMethHndIndirected)
    {
        getEmitter()->emitIns_R_AI(INS_ldr, EA_PTR_DSP_RELOC, REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
        regTracker.rsTrackRegTrash(REG_ARG_0);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_4BYTE, REG_ARG_0, (ssize_t)compiler->compProfilerMethHnd);
    }

    genEmitHelperCall(CORINFO_HELP_PROF_FCN_LEAVE,
                      0,           // argSize
                      EA_UNKNOWN); // retSize

    // Restore state that existed before profiler callback
    if (r0Trashed)
    {
        getEmitter()->emitIns_R_R(INS_mov, attr, REG_ARG_0, REG_PROFILER_RET_SCRATCH);
        regTracker.rsTrackRegTrash(REG_ARG_0);
        gcInfo.gcMarkRegSetNpt(RBM_PROFILER_RET_SCRATCH);
    }

    regSet.rsUnlockReg(RBM_PROFILER_RET_USED);
#else  // _TARGET_*
    NYI("Pushing the profilerHandle & caller's sp for the profiler callout and locking them");
#endif // _TARGET_*

#endif // LEGACY_BACKEND

    /* Restore the stack level */
    genStackLevel = saveStackLvl2;
}

#endif // PROFILING_SUPPORTED

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
 *  Generates appropriate NOP padding for a function prolog to support ReJIT.
 */

void CodeGen::genPrologPadForReJit()
{
    assert(compiler->compGeneratingProlog);

#ifdef _TARGET_XARCH_
    if (!(compiler->opts.eeFlags & CORJIT_FLG_PROF_REJIT_NOPS))
    {
        return;
    }

#if FEATURE_EH_FUNCLETS

    // No need to generate pad (nops) for funclets.
    // When compiling the main function (and not a funclet)
    // the value of funCurrentFunc->funKind is equal to FUNC_ROOT.
    if (compiler->funCurrentFunc()->funKind != FUNC_ROOT)
    {
        return;
    }

#endif // FEATURE_EH_FUNCLETS

    unsigned size = getEmitter()->emitGetPrologOffsetEstimate();
    if (size < 5)
    {
        instNop(5 - size);
    }
#endif
}

/*****************************************************************************
 *
 *  Reserve space for a function prolog.
 */

void CodeGen::genReserveProlog(BasicBlock* block)
{
    assert(block != nullptr);

    JITDUMP("Reserving prolog IG for block BB%02u\n", block->bbNum);

    /* Nothing is live on entry to the prolog */

    getEmitter()->emitCreatePlaceholderIG(IGPT_PROLOG, block, VarSetOps::MakeEmpty(compiler), 0, 0, false);
}

/*****************************************************************************
 *
 *  Reserve space for a function epilog.
 */

void CodeGen::genReserveEpilog(BasicBlock* block)
{
    VARSET_TP VARSET_INIT(compiler, gcrefVarsArg, getEmitter()->emitThisGCrefVars);
    regMaskTP gcrefRegsArg = gcInfo.gcRegGCrefSetCur;
    regMaskTP byrefRegsArg = gcInfo.gcRegByrefSetCur;

    /* The return value is special-cased: make sure it goes live for the epilog */

    bool jmpEpilog = ((block->bbFlags & BBF_HAS_JMP) != 0);

    if (genFullPtrRegMap && !jmpEpilog)
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
        }
    }

    JITDUMP("Reserving epilog IG for block BB%02u\n", block->bbNum);

    assert(block != nullptr);
    bool last = (block->bbNext == nullptr);
    getEmitter()->emitCreatePlaceholderIG(IGPT_EPILOG, block, gcrefVarsArg, gcrefRegsArg, byrefRegsArg, last);
}

#if FEATURE_EH_FUNCLETS

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

    JITDUMP("Reserving funclet prolog IG for block BB%02u\n", block->bbNum);

    getEmitter()->emitCreatePlaceholderIG(IGPT_FUNCLET_PROLOG, block, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                          gcInfo.gcRegByrefSetCur, false);
}

/*****************************************************************************
 *
 *  Reserve space for a funclet epilog.
 */

void CodeGen::genReserveFuncletEpilog(BasicBlock* block)
{
    assert(block != nullptr);

    JITDUMP("Reserving funclet epilog IG for block BB%02u\n", block->bbNum);

    bool last = (block->bbNext == nullptr);
    getEmitter()->emitCreatePlaceholderIG(IGPT_FUNCLET_EPILOG, block, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
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

#ifndef LEGACY_BACKEND
    // Initializations need to happen based on the var locations at the start
    // of the first basic block, so load those up. In particular, the determination
    // of whether or not to use block init in the prolog is dependent on the variable
    // locations on entry to the function.
    compiler->m_pLinearScan->recordVarLocationsAtStartOfBB(compiler->fgFirstBB);
#endif // !LEGACY_BACKEND

    genCheckUseBlockInit();

    // Set various registers as "modified" for special code generation scenarios: Edit & Continue, P/Invoke calls, etc.
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_X86_)

    if (compiler->compTailCallUsed)
    {
        // If we are generating a helper-based tailcall, we've set the tailcall helper "flags"
        // argument to "1", indicating to the tailcall helper that we've saved the callee-saved
        // registers (ebx, esi, edi). So, we need to make sure all the callee-saved registers
        // actually get saved.

        regSet.rsSetRegsModified(RBM_INT_CALLEE_SAVED);
    }
#endif // _TARGET_X86_

#if defined(_TARGET_ARMARCH_)
    // We need to determine if we will change SP larger than a specific amount to determine if we want to use a loop
    // to touch stack pages, that will require multiple registers. See genAllocLclFrame() for details.
    if (compiler->compLclFrameSize >= compiler->getVeryLargeFrameSize())
    {
        regSet.rsSetRegsModified(VERY_LARGE_FRAME_SIZE_REG_MASK);
    }
#endif // defined(_TARGET_ARMARCH_)

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
#ifdef _TARGET_AMD64_
        // On x64 we always save exactly RBP, RSI and RDI for EnC.
        regMaskTP okRegs = (RBM_CALLEE_TRASH | RBM_FPBASE | RBM_RSI | RBM_RDI);
        regSet.rsSetRegsModified(RBM_RSI | RBM_RDI);
        noway_assert((regSet.rsGetModifiedRegsMask() & ~okRegs) == 0);
#else  // !_TARGET_AMD64_
        // On x86 we save all callee saved regs so the saved reg area size is consistent
        regSet.rsSetRegsModified(RBM_INT_CALLEE_SAVED & ~RBM_FPBASE);
#endif // !_TARGET_AMD64_
    }

    /* If we have any pinvoke calls, we might potentially trash everything */
    if (compiler->info.compCallUnmanaged)
    {
        noway_assert(isFramePointerUsed()); // Setup of Pinvoke frame currently requires an EBP style frame
        regSet.rsSetRegsModified(RBM_INT_CALLEE_SAVED & ~RBM_FPBASE);
    }

    /* Count how many callee-saved registers will actually be saved (pushed) */

    // EBP cannot be (directly) modified for EBP frame and double-aligned frames
    noway_assert(!doubleAlignOrFramePointerUsed() || !regSet.rsRegsModified(RBM_FPBASE));

#if ETW_EBP_FRAMED
    // EBP cannot be (directly) modified
    noway_assert(!regSet.rsRegsModified(RBM_FPBASE));
#endif

    regMaskTP maskCalleeRegsPushed = regSet.rsGetModifiedRegsMask() & RBM_CALLEE_SAVED;

#ifdef _TARGET_ARMARCH_
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

#if defined(_TARGET_ARM_)
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
#endif // _TARGET_ARM_
#endif // _TARGET_ARMARCH_

#if defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87
    // Compute the count of callee saved float regs saved on stack.
    // On Amd64 we push only integer regs. Callee saved float (xmm6-xmm15)
    // regs are stack allocated and preserved in their stack locations.
    compiler->compCalleeFPRegsSavedMask = maskCalleeRegsPushed & RBM_FLT_CALLEE_SAVED;
    maskCalleeRegsPushed &= ~RBM_FLT_CALLEE_SAVED;
#endif // defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87

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
    getEmitter()->emitMaxTmpSize = compiler->tmpSize;

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

#if defined(_TARGET_XARCH_)

    if (delta == 0)
    {
        getEmitter()->emitIns_R_R(INS_mov, EA_PTRSIZE, REG_FPBASE, REG_SPBASE);
        psiMoveESPtoEBP();
    }
    else
    {
        getEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, delta);
        // We don't update prolog scope info (there is no function to handle lea), but that is currently dead code
        // anyway.
    }

    if (reportUnwindData)
    {
        compiler->unwindSetFrameReg(REG_FPBASE, delta);
    }

#elif defined(_TARGET_ARM_)

    assert(arm_Valid_Imm_For_Add_SP(delta));
    getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, delta);

    if (reportUnwindData)
    {
        compiler->unwindPadding();
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

#ifdef LEGACY_BACKEND
    genFinalizeFrame();
#endif // LEGACY_BACKEND

    assert(compiler->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT);

    /* Ready to start on the prolog proper */

    getEmitter()->emitBegProlog();
    compiler->unwindBegProlog();

#ifdef DEBUGGING_SUPPORT
    // Do this so we can put the prolog instruction group ahead of
    // other instruction groups
    genIPmappingAddToFront((IL_OFFSETX)ICorDebugInfo::PROLOG);
#endif // DEBUGGING_SUPPORT

#ifdef DEBUG
    if (compiler->opts.dspCode)
    {
        printf("\n__prolog:\n");
    }
#endif

#ifdef DEBUGGING_SUPPORT
    if (compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0))
    {
        // Create new scopes for the method-parameters for the prolog-block.
        psiBegProlog();
    }
#endif

#ifdef DEBUG

    if (compiler->compJitHaltMethod())
    {
        /* put a nop first because the debugger and other tools are likely to
           put an int3 at the begining and we don't want to confuse them */

        instGen(INS_nop);
        instGen(INS_BREAKPOINT);

#ifdef _TARGET_ARMARCH_
        // Avoid asserts in the unwind info because these instructions aren't accounted for.
        compiler->unwindPadding();
#endif // _TARGET_ARMARCH_
    }
#endif // DEBUG

#if FEATURE_EH_FUNCLETS && defined(DEBUG)

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
            noway_assert(varDsc->lvRefCnt == 0);
            continue;
        }

        signed int loOffs = varDsc->lvStkOffs;
        signed int hiOffs = varDsc->lvStkOffs + compiler->lvaLclSize(varNum);

        /* We need to know the offset range of tracked stack GC refs */
        /* We assume that the GC reference can be anywhere in the TYP_STRUCT */

        if (compiler->lvaTypeIsGC(varNum) && varDsc->lvTrackedNonStruct() && varDsc->lvOnFrame)
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

        if (varDsc->lvIsInReg())
        {
            regMaskTP regMask = genRegMask(varDsc->lvRegNum);
            if (!varDsc->IsFloatRegType())
            {
                initRegs |= regMask;

                if (varTypeIsMultiReg(varDsc))
                {
                    if (varDsc->lvOtherReg != REG_STK)
                    {
                        initRegs |= genRegMask(varDsc->lvOtherReg);
                    }
                    else
                    {
                        /* Upper DWORD is on the stack, and needs to be inited */

                        loOffs += sizeof(int);
                        goto INIT_STK;
                    }
                }
            }
#if !FEATURE_STACK_FP_X87
            else if (varDsc->TypeGet() == TYP_DOUBLE)
            {
                initDblRegs |= regMask;
            }
            else
            {
                initFltRegs |= regMask;
            }
#endif // !FEATURE_STACK_FP_X87
        }
        else
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

    if (!TRACK_GC_TEMP_LIFETIMES)
    {
        assert(compiler->tmpAllFree());
        for (TempDsc* tempThis = compiler->tmpListBeg(); tempThis != nullptr; tempThis = compiler->tmpListNxt(tempThis))
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

#if !defined(_TARGET_AMD64_)
            // However, on amd64 there is no requirement to chain frame pointers.

            noway_assert(!isFramePointerUsed() || loOffs != 0);
#endif // !defined(_TARGET_AMD64_)
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
    }

    assert((genInitStkLclCnt > 0) == hasUntrLcl);

#ifdef DEBUG
    if (verbose)
    {
        if (genInitStkLclCnt > 0)
        {
            printf("Found %u lvMustInit stk vars, frame offsets %d through %d\n", genInitStkLclCnt, -untrLclLo,
                   -untrLclHi);
        }
    }
#endif

#ifdef _TARGET_ARM_
    // On the ARM we will spill any incoming struct args in the first instruction in the prolog
    // Ditto for all enregistered user arguments in a varargs method.
    // These registers will be available to use for the initReg.  We just remove
    // all of these registers from the rsCalleeRegArgMaskLiveIn.
    //
    intRegState.rsCalleeRegArgMaskLiveIn &= ~regSet.rsMaskPreSpillRegs(false);
#endif

    /* Choose the register to use for zero initialization */

    regNumber initReg       = REG_SCRATCH; // Unless we find a better register below
    bool      initRegZeroed = false;
    regMaskTP excludeMask   = intRegState.rsCalleeRegArgMaskLiveIn;
    regMaskTP tempMask;

    // We should not use the special PINVOKE registers as the initReg
    // since they are trashed by the jithelper call to setup the PINVOKE frame
    if (compiler->info.compCallUnmanaged)
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
                excludeMask |= genRegMask(varDsc->lvRegNum);
            }
        }
    }

#ifdef _TARGET_ARM_
    // If we have a variable sized frame (compLocallocUsed is true)
    // then using REG_SAVED_LOCALLOC_SP in the prolog is not allowed
    if (compiler->compLocallocUsed)
    {
        excludeMask |= RBM_SAVED_LOCALLOC_SP;
    }
#endif // _TARGET_ARM_

#if defined(_TARGET_XARCH_)
    if (compiler->compLclFrameSize >= compiler->getVeryLargeFrameSize())
    {
        // We currently must use REG_EAX on x86 here
        // because the loop's backwards branch depends upon the size of EAX encodings
        assert(initReg == REG_EAX);
    }
    else
#endif // _TARGET_XARCH_
    {
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
    }

    noway_assert(!compiler->info.compCallUnmanaged || (initReg != REG_PINVOKE_FRAME));

#if defined(_TARGET_AMD64_)
    // If we are a varargs call, in order to set up the arguments correctly this
    // must be done in a 2 step process. As per the x64 ABI:
    // a) The caller sets up the argument shadow space (just before the return
    //    address, 4 pointer sized slots).
    // b) The callee is responsible to home the arguments on the shadow space
    //    provided by the caller.
    // This way, the varargs iterator will be able to retrieve the
    // call arguments properly since both the arg regs and the stack allocated
    // args will be contiguous.
    if (compiler->info.compIsVarArgs)
    {
        getEmitter()->spillIntArgRegsToShadowSlots();
    }

#endif // _TARGET_AMD64_

#ifdef _TARGET_ARM_
    /*-------------------------------------------------------------------------
     *
     * Now start emitting the part of the prolog which sets up the frame
     */

    if (regSet.rsMaskPreSpillRegs(true) != RBM_NONE)
    {
        inst_IV(INS_push, (int)regSet.rsMaskPreSpillRegs(true));
        compiler->unwindPushMaskInt(regSet.rsMaskPreSpillRegs(true));
    }
#endif // _TARGET_ARM_

#ifdef _TARGET_XARCH_
    if (doubleAlignOrFramePointerUsed())
    {
        inst_RV(INS_push, REG_FPBASE, TYP_REF);
        compiler->unwindPush(REG_FPBASE);
        psiAdjustStackLevel(REGSIZE_BYTES);

#ifndef _TARGET_AMD64_ // On AMD64, establish the frame pointer after the "sub rsp"
        genEstablishFramePointer(0, /*reportUnwindData*/ true);
#endif // !_TARGET_AMD64_

#if DOUBLE_ALIGN
        if (compiler->genDoubleAlign())
        {
            noway_assert(isFramePointerUsed() == false);
            noway_assert(!regSet.rsRegsModified(RBM_FPBASE)); /* Trashing EBP is out.    */

            inst_RV_IV(INS_AND, REG_SPBASE, -8, EA_PTRSIZE);
        }
#endif // DOUBLE_ALIGN
    }
#endif // _TARGET_XARCH_

#ifdef _TARGET_ARM64_
    // Probe large frames now, if necessary, since genPushCalleeSavedRegisters() will allocate the frame.
    genAllocLclFrame(compiler->compLclFrameSize, initReg, &initRegZeroed, intRegState.rsCalleeRegArgMaskLiveIn);
    genPushCalleeSavedRegisters(initReg, &initRegZeroed);
#else  // !_TARGET_ARM64_
    genPushCalleeSavedRegisters();
#endif // !_TARGET_ARM64_

#ifdef _TARGET_ARM_
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
#endif // _TARGET_ARM_

    //-------------------------------------------------------------------------
    //
    // Subtract the local frame size from SP.
    //
    //-------------------------------------------------------------------------
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef _TARGET_ARM64_
    regMaskTP maskStackAlloc = RBM_NONE;

#ifdef _TARGET_ARM_
    maskStackAlloc =
        genStackAllocRegisterMask(compiler->compLclFrameSize, regSet.rsGetModifiedRegsMask() & RBM_FLT_CALLEE_SAVED);
#endif // _TARGET_ARM_

    if (maskStackAlloc == RBM_NONE)
    {
        genAllocLclFrame(compiler->compLclFrameSize, initReg, &initRegZeroed, intRegState.rsCalleeRegArgMaskLiveIn);
    }
#endif // !_TARGET_ARM64_

//-------------------------------------------------------------------------

#ifdef _TARGET_ARM_
    if (compiler->compLocallocUsed)
    {
        getEmitter()->emitIns_R_R(INS_mov, EA_4BYTE, REG_SAVED_LOCALLOC_SP, REG_SPBASE);
        regTracker.rsTrackRegTrash(REG_SAVED_LOCALLOC_SP);
        compiler->unwindSetFrameReg(REG_SAVED_LOCALLOC_SP, 0);
    }
#endif // _TARGET_ARMARCH_

#if defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87
    // Preserve callee saved float regs to stack.
    genPreserveCalleeSavedFltRegs(compiler->compLclFrameSize);
#endif // defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87

#ifdef _TARGET_AMD64_
    // Establish the AMD64 frame pointer after the OS-reported prolog.
    if (doubleAlignOrFramePointerUsed())
    {
        bool reportUnwindData = compiler->compLocallocUsed || compiler->opts.compDbgEnC;
        genEstablishFramePointer(compiler->codeGen->genSPtoFPdelta(), reportUnwindData);
    }
#endif //_TARGET_AMD64_

//-------------------------------------------------------------------------
//
// This is the end of the OS-reported prolog for purposes of unwinding
//
//-------------------------------------------------------------------------

#ifdef _TARGET_ARM_
    if (needToEstablishFP)
    {
        genEstablishFramePointer(afterLclFrameSPtoFPdelta, /*reportUnwindData*/ false);
        needToEstablishFP = false; // nobody uses this later, but set it anyway, just to be explicit
    }
#endif // _TARGET_ARM_

    if (compiler->info.compPublishStubParam)
    {
#if CPU_LOAD_STORE_ARCH
        getEmitter()->emitIns_R_R_I(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SECRET_STUB_PARAM, genFramePointerReg(),
                                    compiler->lvaTable[compiler->lvaStubArgumentVar].lvStkOffs);
#else
        // mov [lvaStubArgumentVar], EAX
        getEmitter()->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SECRET_STUB_PARAM, genFramePointerReg(),
                                   compiler->lvaTable[compiler->lvaStubArgumentVar].lvStkOffs);
#endif
        assert(intRegState.rsCalleeRegArgMaskLiveIn & RBM_SECRET_STUB_PARAM);

        // It's no longer live; clear it out so it can be used after this in the prolog
        intRegState.rsCalleeRegArgMaskLiveIn &= ~RBM_SECRET_STUB_PARAM;
    }

#if STACK_PROBES
    // We could probably fold this into the loop for the FrameSize >= 0x3000 probing
    // when creating the stack frame. Don't think it's worth it, though.
    if (genNeedPrologStackProbe)
    {
        //
        // Can't have a call until we have enough padding for rejit
        //
        genPrologPadForReJit();
        noway_assert(compiler->opts.compNeedStackProbes);
        genGenerateStackProbe();
        compiler->compStackProbePrologDone = true;
    }
#endif // STACK_PROBES

    //
    // Zero out the frame as needed
    //

    genZeroInitFrame(untrLclHi, untrLclLo, initReg, &initRegZeroed);

#if FEATURE_EH_FUNCLETS

    genSetPSPSym(initReg, &initRegZeroed);

#else // !FEATURE_EH_FUNCLETS

    // when compInitMem is true the genZeroInitFrame will zero out the shadow SP slots
    if (compiler->ehNeedsShadowSPslots() && !compiler->info.compInitMem)
    {
        /*
        // size/speed option?
        getEmitter()->emitIns_I_ARR(INS_mov, EA_PTRSIZE, 0,
                                REG_EBP, REG_NA, -compiler->lvaShadowSPfirstOffs);
        */

        // The last slot is reserved for ICodeManager::FixContext(ppEndRegion)
        unsigned filterEndOffsetSlotOffs = compiler->lvaLclSize(compiler->lvaShadowSPslotsVar) - (sizeof(void*));

        // Zero out the slot for nesting level 0
        unsigned firstSlotOffs = filterEndOffsetSlotOffs - (sizeof(void*));

        if (!initRegZeroed)
        {
            instGen_Set_Reg_To_Zero(EA_PTRSIZE, initReg);
            initRegZeroed = true;
        }

        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, initReg, compiler->lvaShadowSPslotsVar,
                                  firstSlotOffs);
    }

#endif // !FEATURE_EH_FUNCLETS

    genReportGenericContextArg(initReg, &initRegZeroed);

#if defined(LEGACY_BACKEND) // in RyuJIT backend this has already been expanded into trees
    if (compiler->info.compCallUnmanaged)
    {
        getEmitter()->emitDisableRandomNops();
        initRegs = genPInvokeMethodProlog(initRegs);
        getEmitter()->emitEnableRandomNops();
    }
#endif // defined(LEGACY_BACKEND)

    // The local variable representing the security object must be on the stack frame
    // and must be 0 initialized.
    noway_assert((compiler->lvaSecurityObject == BAD_VAR_NUM) ||
                 (compiler->lvaTable[compiler->lvaSecurityObject].lvOnFrame &&
                  compiler->lvaTable[compiler->lvaSecurityObject].lvMustInit));

    // Initialize any "hidden" slots/locals

    if (compiler->compLocallocUsed)
    {
        noway_assert(compiler->lvaLocAllocSPvar != BAD_VAR_NUM);
#ifdef _TARGET_ARM64_
        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_FPBASE, compiler->lvaLocAllocSPvar, 0);
#else
        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaLocAllocSPvar, 0);
#endif
    }

    // Set up the GS security cookie

    genSetGSSecurityCookie(initReg, &initRegZeroed);

#ifdef PROFILING_SUPPORTED

    // Insert a function entry callback for profiling, if requested.
    genProfilingEnterCallback(initReg, &initRegZeroed);

#endif // PROFILING_SUPPORTED

    if (!genInterruptible)
    {
        /*-------------------------------------------------------------------------
         *
         * The 'real' prolog ends here for non-interruptible methods.
         * For fully-interruptible methods, we extend the prolog so that
         * we do not need to track GC inforation while shuffling the
         * arguments.
         *
         * Make sure there's enough padding for ReJIT.
         *
         */
        genPrologPadForReJit();
        getEmitter()->emitMarkPrologEnd();
    }

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING) && defined(FEATURE_SIMD)
    // The unused bits of Vector3 arguments must be cleared
    // since native compiler doesn't initize the upper bits to zeros.
    //
    // TODO-Cleanup: This logic can be implemented in
    // genFnPrologCalleeRegArgs() for argument registers and
    // genEnregisterIncomingStackArgs() for stack arguments.
    genClearStackVec3ArgUpperBits();
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING && FEATURE_SIMD

    /*-----------------------------------------------------------------------------
     * Take care of register arguments first
     */

    RegState* regState;

#ifndef LEGACY_BACKEND
    // Update the arg initial register locations.
    compiler->lvaUpdateArgsWithInitialReg();
#endif // !LEGACY_BACKEND

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

    // Home the incoming arguments
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

#if !FEATURE_STACK_FP_X87
    if (initFltRegs | initDblRegs)
    {
        // If initReg is not in initRegs then we will use REG_SCRATCH
        if ((genRegMask(initReg) & initRegs) == 0)
        {
            initReg       = REG_SCRATCH;
            initRegZeroed = false;
        }

#ifdef _TARGET_ARM_
        // This is needed only for Arm since it can use a zero initialized int register
        // to initialize vfp registers.
        if (!initRegZeroed)
        {
            instGen_Set_Reg_To_Zero(EA_PTRSIZE, initReg);
            initRegZeroed = true;
        }
#endif // _TARGET_ARM_

        genZeroInitFltRegs(initFltRegs, initDblRegs, initReg);
    }
#endif // !FEATURE_STACK_FP_X87

#if FEATURE_STACK_FP_X87
    //
    // Here is where we load the enregistered floating point arguments
    //   and locals onto the x86-FPU.
    //
    genCodeForPrologStackFP();
#endif

    //-----------------------------------------------------------------------------

    //
    // Increase the prolog size here only if fully interruptible.
    // And again make sure it's big enough for ReJIT
    //

    if (genInterruptible)
    {
        genPrologPadForReJit();
        getEmitter()->emitMarkPrologEnd();
    }

#ifdef DEBUGGING_SUPPORT
    if (compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0))
    {
        psiEndProlog();
    }
#endif

    if (hasGCRef)
    {
        getEmitter()->emitSetFrameRangeGCRs(GCrefLo, GCrefHi);
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

#ifdef _TARGET_X86_
    // On non-x86 the VARARG cookie does not need any special treatment.

    // Load up the VARARG argument pointer register so it doesn't get clobbered.
    // only do this if we actually access any statically declared args
    // (our argument pointer register has a refcount > 0).
    unsigned argsStartVar = compiler->lvaVarargsBaseOfStkArgs;

    if (compiler->info.compIsVarArgs && compiler->lvaTable[argsStartVar].lvRefCnt > 0)
    {
        varDsc = &compiler->lvaTable[argsStartVar];

        noway_assert(compiler->info.compArgsCount > 0);

        // MOV EAX, <VARARGS HANDLE>
        getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_EAX, compiler->info.compArgsCount - 1, 0);
        regTracker.rsTrackRegTrash(REG_EAX);

        // MOV EAX, [EAX]
        getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_EAX, REG_EAX, 0);

        // EDX might actually be holding something here.  So make sure to only use EAX for this code
        // sequence.

        LclVarDsc* lastArg = &compiler->lvaTable[compiler->info.compArgsCount - 1];
        noway_assert(!lastArg->lvRegister);
        signed offset = lastArg->lvStkOffs;
        assert(offset != BAD_STK_OFFS);
        noway_assert(lastArg->lvFramePointerBased);

        // LEA EAX, &<VARARGS HANDLE> + EAX
        getEmitter()->emitIns_R_ARR(INS_lea, EA_PTRSIZE, REG_EAX, genFramePointerReg(), REG_EAX, offset);

        if (varDsc->lvRegister)
        {
            if (varDsc->lvRegNum != REG_EAX)
            {
                getEmitter()->emitIns_R_R(INS_mov, EA_PTRSIZE, varDsc->lvRegNum, REG_EAX);
                regTracker.rsTrackRegTrash(varDsc->lvRegNum);
            }
        }
        else
        {
            getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_EAX, argsStartVar, 0);
        }
    }

#endif // _TARGET_X86_

#ifdef DEBUG
    if (compiler->opts.compStackCheckOnRet)
    {
        noway_assert(compiler->lvaReturnEspCheck != 0xCCCCCCCC &&
                     compiler->lvaTable[compiler->lvaReturnEspCheck].lvDoNotEnregister &&
                     compiler->lvaTable[compiler->lvaReturnEspCheck].lvOnFrame);
        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaReturnEspCheck, 0);
    }
#endif

    getEmitter()->emitEndProlog();
    compiler->unwindEndProlog();

    noway_assert(getEmitter()->emitMaxTmpSize == compiler->tmpSize);
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

#if defined(_TARGET_ARM_)

void CodeGen::genFnEpilog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
        printf("*************** In genFnEpilog()\n");
#endif

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    VarSetOps::Assign(compiler, gcInfo.gcVarPtrSetCur, getEmitter()->emitInitGCrefVars);
    gcInfo.gcRegGCrefSetCur = getEmitter()->emitInitGCrefRegs;
    gcInfo.gcRegByrefSetCur = getEmitter()->emitInitByrefRegs;

#ifdef DEBUG
    if (compiler->opts.dspCode)
        printf("\n__epilog:\n");

    if (verbose)
    {
        printf("gcVarPtrSetCur=%s ", VarSetOps::ToString(compiler, gcInfo.gcVarPtrSetCur));
        dumpConvertedVarSet(compiler, gcInfo.gcVarPtrSetCur);
        printf(", gcRegGCrefSetCur=");
        printRegMaskInt(gcInfo.gcRegGCrefSetCur);
        getEmitter()->emitDispRegSet(gcInfo.gcRegGCrefSetCur);
        printf(", gcRegByrefSetCur=");
        printRegMaskInt(gcInfo.gcRegByrefSetCur);
        getEmitter()->emitDispRegSet(gcInfo.gcRegByrefSetCur);
        printf("\n");
    }
#endif

    bool jmpEpilog = ((block->bbFlags & BBF_HAS_JMP) != 0);

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
        genFreeLclFrame(compiler->compLclFrameSize, &unwindStarted, jmpEpilog);
    }

    if (!unwindStarted)
    {
        // If we haven't generated anything yet, we're certainly going to generate a "pop" next.
        compiler->unwindBegEpilog();
        unwindStarted = true;
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
        noway_assert(block->bbJumpKind == BBJ_RETURN);
        noway_assert(block->bbTreeList);

        // We better not have used a pop PC to return otherwise this will be unreachable code
        noway_assert(!genUsedPopToReturn);

        /* figure out what jump we have */

        GenTree* jmpNode = block->lastNode();
        noway_assert(jmpNode->gtOper == GT_JMP);

        CORINFO_METHOD_HANDLE methHnd = (CORINFO_METHOD_HANDLE)jmpNode->gtVal.gtVal1;

        CORINFO_CONST_LOOKUP  addrInfo;
        void*                 addr;
        regNumber             indCallReg;
        emitter::EmitCallType callType;

        compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &addrInfo);
        switch (addrInfo.accessType)
        {
            case IAT_VALUE:
                if (arm_Valid_Imm_For_BL((ssize_t)addrInfo.addr))
                {
                    // Simple direct call
                    callType   = emitter::EC_FUNC_TOKEN;
                    addr       = addrInfo.addr;
                    indCallReg = REG_NA;
                    break;
                }

                // otherwise the target address doesn't fit in an immediate
                // so we have to burn a register...
                __fallthrough;

            case IAT_PVALUE:
                // Load the address into a register, load indirect and call  through a register
                // We have to use R12 since we assume the argument registers are in use
                callType   = emitter::EC_INDIR_R;
                indCallReg = REG_R12;
                addr       = NULL;
                instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indCallReg, (ssize_t)addrInfo.addr);
                if (addrInfo.accessType == IAT_PVALUE)
                {
                    getEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, indCallReg, indCallReg, 0);
                    regTracker.rsTrackRegTrash(indCallReg);
                }
                break;

            case IAT_PPVALUE:
            default:
                NO_WAY("Unsupported JMP indirection");
        }

        /* Simply emit a jump to the methodHnd. This is similar to a call so we can use
         * the same descriptor with some minor adjustments.
         */

        getEmitter()->emitIns_Call(callType, methHnd, INDEBUG_LDISASM_COMMA(nullptr) addr,
                                   0,          // argSize
                                   EA_UNKNOWN, // retSize
                                   gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur,
                                   BAD_IL_OFFSET, // IL offset
                                   indCallReg,    // ireg
                                   REG_NA,        // xreg
                                   0,             // xmul
                                   0,             // disp
                                   true);         // isJump
    }
    else
    {
        if (!genUsedPopToReturn)
        {
            // If we did not use a pop to return, then we did a "pop {..., lr}" instead of "pop {..., pc}",
            // so we need a "bx lr" instruction to return from the function.
            inst_RV(INS_bx, REG_LR, TYP_I_IMPL);
            compiler->unwindBranch16();
        }
    }

    compiler->unwindEndEpilog();
}

#elif defined(_TARGET_ARM64_)

void CodeGen::genFnEpilog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
        printf("*************** In genFnEpilog()\n");
#endif

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    VarSetOps::Assign(compiler, gcInfo.gcVarPtrSetCur, getEmitter()->emitInitGCrefVars);
    gcInfo.gcRegGCrefSetCur = getEmitter()->emitInitGCrefRegs;
    gcInfo.gcRegByrefSetCur = getEmitter()->emitInitByrefRegs;

#ifdef DEBUG
    if (compiler->opts.dspCode)
        printf("\n__epilog:\n");

    if (verbose)
    {
        printf("gcVarPtrSetCur=%s ", VarSetOps::ToString(compiler, gcInfo.gcVarPtrSetCur));
        dumpConvertedVarSet(compiler, gcInfo.gcVarPtrSetCur);
        printf(", gcRegGCrefSetCur=");
        printRegMaskInt(gcInfo.gcRegGCrefSetCur);
        getEmitter()->emitDispRegSet(gcInfo.gcRegGCrefSetCur);
        printf(", gcRegByrefSetCur=");
        printRegMaskInt(gcInfo.gcRegByrefSetCur);
        getEmitter()->emitDispRegSet(gcInfo.gcRegByrefSetCur);
        printf("\n");
    }
#endif

    bool jmpEpilog = ((block->bbFlags & BBF_HAS_JMP) != 0);

    compiler->unwindBegEpilog();

    genPopCalleeSavedRegistersAndFreeLclFrame(jmpEpilog);

    if (jmpEpilog)
    {
        noway_assert(block->bbJumpKind == BBJ_RETURN);
        noway_assert(block->bbTreeList != nullptr);

        // figure out what jump we have
        GenTree* jmpNode = block->lastNode();
#if !FEATURE_FASTTAILCALL
        noway_assert(jmpNode->gtOper == GT_JMP);
#else
        // arm64
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
            CORINFO_METHOD_HANDLE methHnd = (CORINFO_METHOD_HANDLE)jmpNode->gtVal.gtVal1;

            CORINFO_CONST_LOOKUP addrInfo;
            compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &addrInfo);
            if (addrInfo.accessType != IAT_VALUE)
            {
                NYI_ARM64("Unsupported JMP indirection");
            }

            emitter::EmitCallType callType = emitter::EC_FUNC_TOKEN;

            // Simply emit a jump to the methodHnd. This is similar to a call so we can use
            // the same descriptor with some minor adjustments.
            getEmitter()->emitIns_Call(callType, methHnd, INDEBUG_LDISASM_COMMA(nullptr) addrInfo.addr,
                                       0,          // argSize
                                       EA_UNKNOWN, // retSize
                                       EA_UNKNOWN, // secondRetSize
                                       gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur,
                                       BAD_IL_OFFSET, REG_NA, REG_NA, 0, 0, /* iloffset, ireg, xreg, xmul, disp */
                                       true);                               /* isJump */
        }
#if FEATURE_FASTTAILCALL
        else
        {
            // Fast tail call.
            // Call target = REG_IP0.
            // https://github.com/dotnet/coreclr/issues/4827
            // Do we need a special encoding for stack walker like rex.w prefix for x64?
            getEmitter()->emitIns_R(INS_br, emitTypeSize(TYP_I_IMPL), REG_IP0);
        }
#endif // FEATURE_FASTTAILCALL
    }
    else
    {
        inst_RV(INS_ret, REG_LR, TYP_I_IMPL);
        compiler->unwindReturn(REG_LR);
    }

    compiler->unwindEndEpilog();
}

#elif defined(_TARGET_XARCH_)

void CodeGen::genFnEpilog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFnEpilog()\n");
    }
#endif

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    VarSetOps::Assign(compiler, gcInfo.gcVarPtrSetCur, getEmitter()->emitInitGCrefVars);
    gcInfo.gcRegGCrefSetCur = getEmitter()->emitInitGCrefRegs;
    gcInfo.gcRegByrefSetCur = getEmitter()->emitInitByrefRegs;

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
        getEmitter()->emitDispRegSet(gcInfo.gcRegGCrefSetCur);
        printf(", gcRegByrefSetCur=");
        printRegMaskInt(gcInfo.gcRegByrefSetCur);
        getEmitter()->emitDispRegSet(gcInfo.gcRegByrefSetCur);
        printf("\n");
    }
#endif

#if !FEATURE_STACK_FP_X87
    // Restore float registers that were saved to stack before SP is modified.
    genRestoreCalleeSavedFltRegs(compiler->compLclFrameSize);
#endif // !FEATURE_STACK_FP_X87

    /* Compute the size in bytes we've pushed/popped */

    if (!doubleAlignOrFramePointerUsed())
    {
        // We have an ESP frame */

        noway_assert(compiler->compLocallocUsed == false); // Only used with frame-pointer

        /* Get rid of our local variables */

        if (compiler->compLclFrameSize)
        {
#ifdef _TARGET_X86_
            /* Add 'compiler->compLclFrameSize' to ESP */
            /* Use pop ECX to increment ESP by 4, unless compiler->compJmpOpUsed is true */

            if ((compiler->compLclFrameSize == sizeof(void*)) && !compiler->compJmpOpUsed)
            {
                inst_RV(INS_pop, REG_ECX, TYP_I_IMPL);
                regTracker.rsTrackRegTrash(REG_ECX);
            }
            else
#endif // _TARGET_X86
            {
                /* Add 'compiler->compLclFrameSize' to ESP */
                /* Generate "add esp, <stack-size>" */
                inst_RV_IV(INS_add, REG_SPBASE, compiler->compLclFrameSize, EA_PTRSIZE);
            }
        }

        genPopCalleeSavedRegisters();
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
                // ESP may be variable if a localloc was actually executed. Reset it.
                //    lea esp, [ebp - compiler->compCalleeRegsPushed * REGSIZE_BYTES]

                needLea = true;
            }
            else if (!regSet.rsRegsModified(RBM_CALLEE_SAVED))
            {
                if (compiler->compLclFrameSize != 0)
                {
#ifdef _TARGET_AMD64_
                    // AMD64 can't use "mov esp, ebp", according to the ABI specification describing epilogs. So,
                    // do an LEA to "pop off" the frame allocation.
                    needLea = true;
#else  // !_TARGET_AMD64_
                    // We will just generate "mov esp, ebp" and be done with it.
                    needMovEspEbp = true;
#endif // !_TARGET_AMD64_
                }
            }
            else if (compiler->compLclFrameSize == 0)
            {
                // do nothing before popping the callee-saved registers
            }
#ifdef _TARGET_X86_
            else if (compiler->compLclFrameSize == REGSIZE_BYTES)
            {
                // "pop ecx" will make ESP point to the callee-saved registers
                inst_RV(INS_pop, REG_ECX, TYP_I_IMPL);
                regTracker.rsTrackRegTrash(REG_ECX);
            }
#endif // _TARGET_X86
            else
            {
                // We need to make ESP point to the callee-saved registers
                needLea = true;
            }

            if (needLea)
            {
                int offset;

#ifdef _TARGET_AMD64_
                // lea esp, [ebp + compiler->compLclFrameSize - genSPtoFPdelta]
                //
                // Case 1: localloc not used.
                // genSPToFPDelta = compiler->compCalleeRegsPushed * REGSIZE_BYTES + compiler->compLclFrameSize
                // offset = compiler->compCalleeRegsPushed * REGSIZE_BYTES;
                // The amount to be subtracted from RBP to point at callee saved int regs.
                //
                // Case 2: localloc used
                // genSPToFPDelta = Min(240, (int)compiler->lvaOutgoingArgSpaceSize)
                // Offset = Amount to be aded to RBP to point at callee saved int regs.
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

                getEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, -offset);
            }
        }

        //
        // Pop the callee-saved registers (if any)
        //

        genPopCalleeSavedRegisters();

#ifdef _TARGET_AMD64_
        assert(!needMovEspEbp); // "mov esp, ebp" is not allowed in AMD64 epilogs
#else  // !_TARGET_AMD64_
        if (needMovEspEbp)
        {
            // mov esp, ebp
            inst_RV_RV(INS_mov, REG_SPBASE, REG_FPBASE);
        }
#endif // !_TARGET_AMD64_

        // pop ebp
        inst_RV(INS_pop, REG_EBP, TYP_I_IMPL);
    }

    getEmitter()->emitStartExitSeq(); // Mark the start of the "return" sequence

    /* Check if this a special return block i.e.
     * CEE_JMP instruction */

    if (jmpEpilog)
    {
        noway_assert(block->bbJumpKind == BBJ_RETURN);
        noway_assert(block->bbTreeList);

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
            CORINFO_METHOD_HANDLE methHnd = (CORINFO_METHOD_HANDLE)jmpNode->gtVal.gtVal1;

            CORINFO_CONST_LOOKUP addrInfo;
            compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &addrInfo);
            if (addrInfo.accessType != IAT_VALUE && addrInfo.accessType != IAT_PVALUE)
            {
                NO_WAY("Unsupported JMP indirection");
            }

            const emitter::EmitCallType callType =
                (addrInfo.accessType == IAT_VALUE) ? emitter::EC_FUNC_TOKEN : emitter::EC_FUNC_TOKEN_INDIR;

            // Simply emit a jump to the methodHnd. This is similar to a call so we can use
            // the same descriptor with some minor adjustments.
            getEmitter()->emitIns_Call(callType, methHnd, INDEBUG_LDISASM_COMMA(nullptr) addrInfo.addr,
                                       0,                                                      // argSize
                                       EA_UNKNOWN                                              // retSize
                                       FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(EA_UNKNOWN), // secondRetSize
                                       gcInfo.gcVarPtrSetCur,
                                       gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur, BAD_IL_OFFSET, REG_NA, REG_NA,
                                       0, 0,  /* iloffset, ireg, xreg, xmul, disp */
                                       true); /* isJump */
        }
#if FEATURE_FASTTAILCALL
        else
        {
#ifdef _TARGET_AMD64_
            // Fast tail call.
            // Call target = RAX.
            // Stack walker requires that a register indirect tail call be rex.w prefixed.
            getEmitter()->emitIns_R(INS_rex_jmp, emitTypeSize(TYP_I_IMPL), REG_RAX);
#else
            assert(!"Fast tail call as epilog+jmp");
            unreached();
#endif //_TARGET_AMD64_
        }
#endif // FEATURE_FASTTAILCALL
    }
    else
    {
        unsigned stkArgSize = 0; // Zero on all platforms except x86

#if defined(_TARGET_X86_)

        noway_assert(compiler->compArgSize >= intRegState.rsCalleeRegArgCount * sizeof(void*));
        stkArgSize = compiler->compArgSize - intRegState.rsCalleeRegArgCount * sizeof(void*);

        noway_assert(compiler->compArgSize < 0x10000); // "ret" only has 2 byte operand

        // varargs has caller pop
        if (compiler->info.compIsVarArgs)
            stkArgSize = 0;

#endif // defined(_TARGET_X86_)

        /* Return, popping our arguments (if any) */
        instGen_Return(stkArgSize);
    }
}

#else // _TARGET_*
#error Unsupported or unset target architecture
#endif // _TARGET_*

#if FEATURE_EH_FUNCLETS

#ifdef _TARGET_ARM_

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
 *      |        PSP slot       |
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
    assert(block->bbFlags && BBF_FUNCLET_BEG);

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

    if (isFilter)
    {
        // This is the first block of a filter

        getEmitter()->emitIns_R_R_I(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_R1, REG_R1,
                                    genFuncletInfo.fiPSP_slot_CallerSP_offset);
        regTracker.rsTrackRegTrash(REG_R1);
        getEmitter()->emitIns_R_R_I(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_R1, REG_SPBASE,
                                    genFuncletInfo.fiPSP_slot_SP_offset);
        getEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_FPBASE, REG_R1,
                                    genFuncletInfo.fiFunctionCallerSPtoFPdelta);
    }
    else
    {
        // This is a non-filter funclet
        getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_R3, REG_FPBASE,
                                    genFuncletInfo.fiFunctionCallerSPtoFPdelta);
        regTracker.rsTrackRegTrash(REG_R3);
        getEmitter()->emitIns_R_R_I(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_R3, REG_SPBASE,
                                    genFuncletInfo.fiPSP_slot_SP_offset);
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
        genFreeLclFrame(genFuncletInfo.fiSpDelta, &unwindStarted, false);
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
        assert(compiler->lvaDoneFrameLayout ==
               Compiler::FINAL_FRAME_LAYOUT); // The frame size and offsets must be finalized

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

            if (PSP_slot_CallerSP_offset !=
                compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym)) // for debugging
                printf("lvaGetCallerSPRelativeOffset(lvaPSPSym): %d\n",
                       compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym));
        }
#endif // DEBUG

        assert(PSP_slot_CallerSP_offset < 0);
        assert(compiler->lvaPSPSym != BAD_VAR_NUM);
        assert(PSP_slot_CallerSP_offset == compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym)); // same offset
                                                                                                         // used in main
                                                                                                         // function and
                                                                                                         // funclet!
    }
}

#elif defined(_TARGET_AMD64_)

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
 *      |        PSP slot       |
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

    getEmitter()->emitIns_R_AR(INS_mov, EA_PTRSIZE, REG_FPBASE, REG_ARG_0, genFuncletInfo.fiPSP_slot_InitialSP_offset);

    regTracker.rsTrackRegTrash(REG_FPBASE);

    getEmitter()->emitIns_AR_R(INS_mov, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, genFuncletInfo.fiPSP_slot_InitialSP_offset);

    if (genFuncletInfo.fiFunction_InitialSP_to_FP_delta != 0)
    {
        getEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_FPBASE, REG_FPBASE,
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
    assert(compiler->lvaDoneFrameLayout ==
           Compiler::FINAL_FRAME_LAYOUT);                         // The frame size and offsets must be finalized
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

    totalFrameSize += FPRegsPad               // Padding before pushing entire xmm regs
                      + calleeFPRegsSavedSize // pushed callee-saved float regs
                      // below calculated 'pad' will go here
                      + REGSIZE_BYTES                     // PSPSym
                      + compiler->lvaOutgoingArgSpaceSize // outgoing arg space
        ;

    unsigned pad = AlignmentPad(totalFrameSize, 16);

    genFuncletInfo.fiSpDelta = FPRegsPad                           // Padding to align SP on XMM_REGSIZE_BYTES boundary
                               + calleeFPRegsSavedSize             // Callee saved xmm regs
                               + pad + REGSIZE_BYTES               // PSPSym
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
#endif // DEBUG

    assert(compiler->lvaPSPSym != BAD_VAR_NUM);
    assert(genFuncletInfo.fiPSP_slot_InitialSP_offset ==
           compiler->lvaGetInitialSPRelativeOffset(compiler->lvaPSPSym)); // same offset used in main function and
                                                                          // funclet!
}

#elif defined(_TARGET_ARM64_)

// Look in CodeGenArm64.cpp

#else // _TARGET_*

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

#endif // _TARGET_*

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

    if (!compiler->ehNeedsPSPSym())
    {
        return;
    }

    noway_assert(isFramePointerUsed());         // We need an explicit frame pointer
    assert(compiler->lvaPSPSym != BAD_VAR_NUM); // We should have created the PSPSym variable

#if defined(_TARGET_ARM_)

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

    getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, regTmp, regBase, callerSPOffs);
    getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, regTmp, compiler->lvaPSPSym, 0);

#elif defined(_TARGET_ARM64_)

    int SPtoCallerSPdelta = -genCallerSPtoInitialSPdelta();

    // We will just use the initReg since it is an available register
    // and we are probably done using it anyway...
    regNumber regTmp = initReg;
    *pInitRegZeroed  = false;

    getEmitter()->emitIns_R_R_Imm(INS_add, EA_PTRSIZE, regTmp, REG_SPBASE, SPtoCallerSPdelta);
    getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, regTmp, compiler->lvaPSPSym, 0);

#elif defined(_TARGET_AMD64_)

    // The PSP sym value is Initial-SP, not Caller-SP!
    // We assume that RSP is Initial-SP when this function is called. That is, the stack frame
    // has been established.
    //
    // We generate:
    //     mov     [rbp-20h], rsp       // store the Initial-SP (our current rsp) in the PSPsym

    getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaPSPSym, 0);

#else // _TARGET_*

    NYI("Set function PSP sym");

#endif // _TARGET_*
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
        getEmitter()->emitDispIGlist(false);
    }
#endif

#ifndef LEGACY_BACKEND
    // Before generating the prolog, we need to reset the variable locations to what they will be on entry.
    // This affects our code that determines which untracked locals need to be zero initialized.
    compiler->m_pLinearScan->recordVarLocationsAtStartOfBB(compiler->fgFirstBB);
#endif // !LEGACY_BACKEND

    // Tell the emitter we're done with main code generation, and are going to start prolog and epilog generation.

    getEmitter()->emitStartPrologEpilogGeneration();

    gcInfo.gcResetForBB();
    genFnProlog();

    // Generate all the prologs and epilogs.
    CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_EH_FUNCLETS

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

    getEmitter()->emitGeneratePrologEpilog();

    // Tell the emitter we're done with all prolog and epilog generation.

    getEmitter()->emitFinishPrologEpilogGeneration();

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** After prolog / epilog generation\n");
        getEmitter()->emitDispIGlist(false);
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

#if STACK_PROBES
void CodeGen::genGenerateStackProbe()
{
    noway_assert(compiler->opts.compNeedStackProbes);

    // If this assert fires, it means somebody has changed the value
    // CORINFO_STACKPROBE_DEPTH.
    // Why does the EE need such a deep probe? It should just need a couple
    // of bytes, to set up a frame in the unmanaged code..

    static_assert_no_msg(CORINFO_STACKPROBE_DEPTH + JIT_RESERVED_STACK < compiler->eeGetPageSize());

    JITDUMP("Emitting stack probe:\n");
    getEmitter()->emitIns_AR_R(INS_TEST, EA_PTRSIZE, REG_EAX, REG_SPBASE,
                               -(CORINFO_STACKPROBE_DEPTH + JIT_RESERVED_STACK));
}
#endif // STACK_PROBES

/*****************************************************************************
 *
 *  Record the constant and return a tree node that yields its address.
 */

GenTreePtr CodeGen::genMakeConst(const void* cnsAddr, var_types cnsType, GenTreePtr cnsTree, bool dblAlign)
{
    // Assign the constant an offset in the data section
    UNATIVE_OFFSET cnsSize = genTypeSize(cnsType);
    UNATIVE_OFFSET cnum    = getEmitter()->emitDataConst(cnsAddr, cnsSize, dblAlign);

#ifdef DEBUG
    if (compiler->opts.dspCode)
    {
        printf("   @%s%02u   ", "CNS", cnum);

        switch (cnsType)
        {
            case TYP_INT:
                printf("DD      %d \n", *(int*)cnsAddr);
                break;
            case TYP_LONG:
                printf("DQ      %lld\n", *(__int64*)cnsAddr);
                break;
            case TYP_FLOAT:
                printf("DF      %f \n", *(float*)cnsAddr);
                break;
            case TYP_DOUBLE:
                printf("DQ      %lf\n", *(double*)cnsAddr);
                break;

            default:
                noway_assert(!"unexpected constant type");
        }
    }
#endif

    // Access to inline data is 'abstracted' by a special type of static member
    // (produced by eeFindJitDataOffs) which the emitter recognizes as being a reference
    // to constant data, not a real static field.

    return new (compiler, GT_CLS_VAR) GenTreeClsVar(cnsType, compiler->eeFindJitDataOffs(cnum), nullptr);
}

#if defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87
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
    regMaskTP regMask = compiler->compCalleeFPRegsSavedMask;

    // Only callee saved floating point registers should be in regMask
    assert((regMask & RBM_FLT_CALLEE_SAVED) == regMask);

    // fast path return
    if (regMask == RBM_NONE)
    {
        return;
    }

#ifdef _TARGET_AMD64_
    unsigned firstFPRegPadding = compiler->lvaIsCalleeSavedIntRegCountEven() ? REGSIZE_BYTES : 0;
    unsigned offset            = lclFrameSize - firstFPRegPadding - XMM_REGSIZE_BYTES;

    // Offset is 16-byte aligned since we use movaps for preserving xmm regs.
    assert((offset % 16) == 0);
    instruction copyIns = ins_Copy(TYP_FLOAT);
#else  // !_TARGET_AMD64_
    unsigned    offset            = lclFrameSize - XMM_REGSIZE_BYTES;
    instruction copyIns           = INS_movupd;
#endif // !_TARGET_AMD64_

    for (regNumber reg = REG_FLT_CALLEE_SAVED_FIRST; regMask != RBM_NONE; reg = REG_NEXT(reg))
    {
        regMaskTP regBit = genRegMask(reg);
        if ((regBit & regMask) != 0)
        {
            // ABI requires us to preserve lower 128-bits of YMM register.
            getEmitter()->emitIns_AR_R(copyIns,
                                       EA_8BYTE, // TODO-XArch-Cleanup: size specified here doesn't matter but should be
                                                 // EA_16BYTE
                                       reg, REG_SPBASE, offset);
            compiler->unwindSaveReg(reg, offset);
            regMask &= ~regBit;
            offset -= XMM_REGSIZE_BYTES;
        }
    }

#ifdef FEATURE_AVX_SUPPORT
    // Just before restoring float registers issue a Vzeroupper to zero out upper 128-bits of all YMM regs.
    // This is to avoid penalty if this routine is using AVX-256 and now returning to a routine that is
    // using SSE2.
    if (compiler->getFloatingPointInstructionSet() == InstructionSet_AVX)
    {
        instGen(INS_vzeroupper);
    }
#endif
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
        return;
    }

#ifdef _TARGET_AMD64_
    unsigned    firstFPRegPadding = compiler->lvaIsCalleeSavedIntRegCountEven() ? REGSIZE_BYTES : 0;
    instruction copyIns           = ins_Copy(TYP_FLOAT);
#else  // !_TARGET_AMD64_
    unsigned    firstFPRegPadding = 0;
    instruction copyIns           = INS_movupd;
#endif // !_TARGET_AMD64_

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

#ifdef _TARGET_AMD64_
    // Offset is 16-byte aligned since we use movaps for restoring xmm regs
    assert((offset % 16) == 0);
#endif // _TARGET_AMD64_

#ifdef FEATURE_AVX_SUPPORT
    // Just before restoring float registers issue a Vzeroupper to zero out upper 128-bits of all YMM regs.
    // This is to avoid penalty if this routine is using AVX-256 and now returning to a routine that is
    // using SSE2.
    if (compiler->getFloatingPointInstructionSet() == InstructionSet_AVX)
    {
        instGen(INS_vzeroupper);
    }
#endif

    for (regNumber reg = REG_FLT_CALLEE_SAVED_FIRST; regMask != RBM_NONE; reg = REG_NEXT(reg))
    {
        regMaskTP regBit = genRegMask(reg);
        if ((regBit & regMask) != 0)
        {
            // ABI requires us to restore lower 128-bits of YMM register.
            getEmitter()->emitIns_R_AR(copyIns,
                                       EA_8BYTE, // TODO-XArch-Cleanup: size specified here doesn't matter but should be
                                                 // EA_16BYTE
                                       reg, regBase, offset);
            regMask &= ~regBit;
            offset -= XMM_REGSIZE_BYTES;
        }
    }
}
#endif // defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87

//-----------------------------------------------------------------------------------
// IsMultiRegPassedType: Returns true if the type is returned in multiple registers
//
// Arguments:
//     hClass   -  type handle
//
// Return Value:
//     true if type is passed in multiple registers, false otherwise.
//
bool Compiler::IsMultiRegPassedType(CORINFO_CLASS_HANDLE hClass)
{
    if (hClass == NO_CLASS_HANDLE)
    {
        return false;
    }

    structPassingKind howToPassStruct;
    var_types         returnType = getArgTypeForStruct(hClass, &howToPassStruct);

    return (returnType == TYP_STRUCT);
}

//-----------------------------------------------------------------------------------
// IsMultiRegReturnedType: Returns true if the type is returned in multiple registers
//
// Arguments:
//     hClass   -  type handle
//
// Return Value:
//     true if type is returned in multiple registers, false otherwise.
//
bool Compiler::IsMultiRegReturnedType(CORINFO_CLASS_HANDLE hClass)
{
    if (hClass == NO_CLASS_HANDLE)
    {
        return false;
    }

    structPassingKind howToReturnStruct;
    var_types         returnType = getReturnTypeForStruct(hClass, &howToReturnStruct);

    return (returnType == TYP_STRUCT);
}

//----------------------------------------------
// Methods that support HFA's for ARM32/ARM64
//----------------------------------------------

bool Compiler::IsHfa(CORINFO_CLASS_HANDLE hClass)
{
#ifdef FEATURE_HFA
    return varTypeIsFloating(GetHfaType(hClass));
#else
    return false;
#endif
}

bool Compiler::IsHfa(GenTreePtr tree)
{
#ifdef FEATURE_HFA
    return IsHfa(gtGetStructHandleIfPresent(tree));
#else
    return false;
#endif
}

var_types Compiler::GetHfaType(GenTreePtr tree)
{
#ifdef FEATURE_HFA
    if (tree->TypeGet() == TYP_STRUCT)
    {
        return GetHfaType(gtGetStructHandleIfPresent(tree));
    }
#endif
    return TYP_UNDEF;
}

unsigned Compiler::GetHfaCount(GenTreePtr tree)
{
    return GetHfaCount(gtGetStructHandleIfPresent(tree));
}

var_types Compiler::GetHfaType(CORINFO_CLASS_HANDLE hClass)
{
    var_types result = TYP_UNDEF;
    if (hClass != NO_CLASS_HANDLE)
    {
#ifdef FEATURE_HFA
        CorInfoType corType = info.compCompHnd->getHFAType(hClass);
        if (corType != CORINFO_TYPE_UNDEF)
        {
            result = JITtype2varType(corType);
        }
#endif // FEATURE_HFA
    }
    return result;
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
#ifdef _TARGET_ARM_
    // A HFA of doubles is twice as large as an HFA of singles for ARM32
    // (i.e. uses twice the number of single precison registers)
    return info.compCompHnd->getClassSize(hClass) / REGSIZE_BYTES;
#else  // _TARGET_ARM64_
    var_types hfaType   = GetHfaType(hClass);
    unsigned  classSize = info.compCompHnd->getClassSize(hClass);
    // Note that the retail build issues a warning about a potential divsion by zero without the Max function
    unsigned elemSize = Max((unsigned)1, EA_SIZE_IN_BYTES(emitActualTypeSize(hfaType)));
    return classSize / elemSize;
#endif // _TARGET_ARM64_
}

#ifdef _TARGET_XARCH_

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

#endif // _TARGET_XARCH_

#if !defined(LEGACY_BACKEND) && (defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_))

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
//    For System V systems or arm64, there is no such calling convention requirement, and the code needs to find
//    the first stack passed argument from the caller. This is done by iterating over
//    all the lvParam variables and finding the first with lvArgReg equals to REG_STK.
//
unsigned CodeGen::getFirstArgWithStackSlot()
{
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING) || defined(_TARGET_ARM64_)
    unsigned baseVarNum = 0;
#if defined(FEATURE_UNIX_AMR64_STRUCT_PASSING)
    baseVarNum = compiler->lvaFirstStackIncomingArgNum;

    if (compiler->lvaFirstStackIncomingArgNum != BAD_VAR_NUM)
    {
        baseVarNum = compiler->lvaFirstStackIncomingArgNum;
    }
    else
#endif // FEATURE_UNIX_ARM64_STRUCT_PASSING
    {
        // Iterate over all the local variables in the Lcl var table.
        // They contain all the implicit arguments - thisPtr, retBuf,
        // generic context, PInvoke cookie, var arg cookie,no-standard args, etc.
        LclVarDsc* varDsc = nullptr;
        for (unsigned i = 0; i < compiler->info.compArgsCount; i++)
        {
            varDsc = &(compiler->lvaTable[i]);

            // We are iterating over the arguments only.
            assert(varDsc->lvIsParam);

            if (varDsc->lvArgReg == REG_STK)
            {
                baseVarNum = i;
#if defined(FEATURE_UNIX_AMR64_STRUCT_PASSING)
                compiler->lvaFirstStackIncomingArgNum = baseVarNum;
#endif // FEATURE_UNIX_ARM64_STRUCT_PASSING
                break;
            }
        }
        assert(varDsc != nullptr);
    }

    return baseVarNum;
#elif defined(_TARGET_AMD64_)
    return 0;
#else
    // Not implemented for x86.
    NYI_X86("getFirstArgWithStackSlot not yet implemented for x86.");
    return BAD_VAR_NUM;
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING || _TARGET_ARM64_
}

#endif // !LEGACY_BACKEND && (_TARGET_XARCH_ || _TARGET_ARM64_)

/*****************************************************************************/
#ifdef DEBUGGING_SUPPORT

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

    if (compiler->info.compVarScopesCount == 0)
    {
        compiler->eeSetLVcount(0);
        compiler->eeSetLVdone();
        return;
    }

    noway_assert(compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0));
    noway_assert(psiOpenScopeList.scNext == nullptr);

    unsigned i;
    unsigned scopeCnt = siScopeCnt + psiScopeCnt;

    compiler->eeSetLVcount(scopeCnt);

#ifdef DEBUG
    genTrnslLocalVarCount = scopeCnt;
    if (scopeCnt)
    {
        genTrnslLocalVarInfo = new (compiler, CMK_DebugOnly) TrnslLocalVarInfo[scopeCnt];
    }
#endif

    // Record the scopes found for the parameters over the prolog.
    // The prolog needs to be treated differently as a variable may not
    // have the same info in the prolog block as is given by compiler->lvaTable.
    // eg. A register parameter is actually on the stack, before it is loaded to reg.

    CodeGen::psiScope* scopeP;

    for (i = 0, scopeP = psiScopeList.scNext; i < psiScopeCnt; i++, scopeP = scopeP->scNext)
    {
        noway_assert(scopeP != nullptr);
        noway_assert(scopeP->scStartLoc.Valid());
        noway_assert(scopeP->scEndLoc.Valid());

        UNATIVE_OFFSET startOffs = scopeP->scStartLoc.CodeOffset(getEmitter());
        UNATIVE_OFFSET endOffs   = scopeP->scEndLoc.CodeOffset(getEmitter());

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

        Compiler::siVarLoc varLoc;

        if (scopeP->scRegister)
        {
            varLoc.vlType       = Compiler::VLT_REG;
            varLoc.vlReg.vlrReg = (regNumber)scopeP->u1.scRegNum;
        }
        else
        {
            varLoc.vlType           = Compiler::VLT_STK;
            varLoc.vlStk.vlsBaseReg = (regNumber)scopeP->u2.scBaseReg;
            varLoc.vlStk.vlsOffset  = scopeP->u2.scOffset;
        }

        genSetScopeInfo(i, startOffs, endOffs - startOffs, varNum, scopeP->scLVnum, true, varLoc);
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

        UNATIVE_OFFSET startOffs = scopeL->scStartLoc.CodeOffset(getEmitter());
        UNATIVE_OFFSET endOffs   = scopeL->scEndLoc.CodeOffset(getEmitter());

        noway_assert(scopeL->scStartLoc != scopeL->scEndLoc);

        // For stack vars, find the base register, and offset

        regNumber baseReg;
        signed    offset = compiler->lvaTable[scopeL->scVarNum].lvStkOffs;

        if (!compiler->lvaTable[scopeL->scVarNum].lvFramePointerBased)
        {
            baseReg = REG_SPBASE;
            offset += scopeL->scStackLevel;
        }
        else
        {
            baseReg = REG_FPBASE;
        }

        // Now fill in the varLoc

        Compiler::siVarLoc varLoc;

        // TODO-Review: This only works for always-enregistered variables. With LSRA, a variable might be in a register
        // for part of its lifetime, or in different registers for different parts of its lifetime.
        // This should only matter for non-debug code, where we do variable enregistration.
        // We should store the ranges of variable enregistration in the scope table.
        if (compiler->lvaTable[scopeL->scVarNum].lvIsInReg())
        {
            var_types type = genActualType(compiler->lvaTable[scopeL->scVarNum].TypeGet());
            switch (type)
            {
                case TYP_INT:
                case TYP_REF:
                case TYP_BYREF:
#ifdef _TARGET_64BIT_
                case TYP_LONG:
#endif // _TARGET_64BIT_

                    varLoc.vlType       = Compiler::VLT_REG;
                    varLoc.vlReg.vlrReg = compiler->lvaTable[scopeL->scVarNum].lvRegNum;
                    break;

#ifndef _TARGET_64BIT_
                case TYP_LONG:
#if !CPU_HAS_FP_SUPPORT
                case TYP_DOUBLE:
#endif

                    if (compiler->lvaTable[scopeL->scVarNum].lvOtherReg != REG_STK)
                    {
                        varLoc.vlType            = Compiler::VLT_REG_REG;
                        varLoc.vlRegReg.vlrrReg1 = compiler->lvaTable[scopeL->scVarNum].lvRegNum;
                        varLoc.vlRegReg.vlrrReg2 = compiler->lvaTable[scopeL->scVarNum].lvOtherReg;
                    }
                    else
                    {
                        varLoc.vlType                        = Compiler::VLT_REG_STK;
                        varLoc.vlRegStk.vlrsReg              = compiler->lvaTable[scopeL->scVarNum].lvRegNum;
                        varLoc.vlRegStk.vlrsStk.vlrssBaseReg = baseReg;
                        if (!isFramePointerUsed() && varLoc.vlRegStk.vlrsStk.vlrssBaseReg == REG_SPBASE)
                        {
                            varLoc.vlRegStk.vlrsStk.vlrssBaseReg = (regNumber)ICorDebugInfo::REGNUM_AMBIENT_SP;
                        }
                        varLoc.vlRegStk.vlrsStk.vlrssOffset = offset + sizeof(int);
                    }
                    break;
#endif // !_TARGET_64BIT_

#ifdef _TARGET_64BIT_

                case TYP_FLOAT:
                case TYP_DOUBLE:
                    // TODO-AMD64-Bug: ndp\clr\src\inc\corinfo.h has a definition of RegNum that only goes up to R15,
                    // so no XMM registers can get debug information.
                    varLoc.vlType       = Compiler::VLT_REG_FP;
                    varLoc.vlReg.vlrReg = compiler->lvaTable[scopeL->scVarNum].lvRegNum;
                    break;

#else // !_TARGET_64BIT_

#if CPU_HAS_FP_SUPPORT
                case TYP_FLOAT:
                case TYP_DOUBLE:
                    if (isFloatRegType(type))
                    {
                        varLoc.vlType         = Compiler::VLT_FPSTK;
                        varLoc.vlFPstk.vlfReg = compiler->lvaTable[scopeL->scVarNum].lvRegNum;
                    }
                    break;
#endif // CPU_HAS_FP_SUPPORT

#endif // !_TARGET_64BIT_

#ifdef FEATURE_SIMD
                case TYP_SIMD8:
                case TYP_SIMD12:
                case TYP_SIMD16:
                case TYP_SIMD32:
                    varLoc.vlType = Compiler::VLT_REG_FP;

                    // TODO-AMD64-Bug: ndp\clr\src\inc\corinfo.h has a definition of RegNum that only goes up to R15,
                    // so no XMM registers can get debug information.
                    //
                    // Note: Need to initialize vlrReg field, otherwise during jit dump hitting an assert
                    // in eeDispVar() --> getRegName() that regNumber is valid.
                    varLoc.vlReg.vlrReg = compiler->lvaTable[scopeL->scVarNum].lvRegNum;
                    break;
#endif // FEATURE_SIMD

                default:
                    noway_assert(!"Invalid type");
            }
        }
        else
        {
            assert(offset != BAD_STK_OFFS);
            LclVarDsc* varDsc = compiler->lvaTable + scopeL->scVarNum;
            switch (genActualType(varDsc->TypeGet()))
            {
                case TYP_INT:
                case TYP_REF:
                case TYP_BYREF:
                case TYP_FLOAT:
                case TYP_STRUCT:
                case TYP_BLK: // Needed because of the TYP_BLK stress mode
#ifdef FEATURE_SIMD
                case TYP_SIMD8:
                case TYP_SIMD12:
                case TYP_SIMD16:
                case TYP_SIMD32:
#endif
#ifdef _TARGET_64BIT_
                case TYP_LONG:
                case TYP_DOUBLE:
#endif // _TARGET_64BIT_
#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
                    // In the AMD64 ABI we are supposed to pass a struct by reference when its
                    // size is not 1, 2, 4 or 8 bytes in size. During fgMorph, the compiler modifies
                    // the IR to comply with the ABI and therefore changes the type of the lclVar
                    // that holds the struct from TYP_STRUCT to TYP_BYREF but it gives us a hint that
                    // this is still a struct by setting the lvIsTemp flag.
                    // The same is true for ARM64 and structs > 16 bytes.
                    // (See Compiler::fgMarkImplicitByRefArgs in Morph.cpp for further detail)
                    // Now, the VM expects a special enum for these type of local vars: VLT_STK_BYREF
                    // to accomodate for this situation.
                    if (varDsc->lvType == TYP_BYREF && varDsc->lvIsTemp)
                    {
                        assert(varDsc->lvIsParam);
                        varLoc.vlType = Compiler::VLT_STK_BYREF;
                    }
                    else
#endif // defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
                    {
                        varLoc.vlType = Compiler::VLT_STK;
                    }
                    varLoc.vlStk.vlsBaseReg = baseReg;
                    varLoc.vlStk.vlsOffset  = offset;
                    if (!isFramePointerUsed() && varLoc.vlStk.vlsBaseReg == REG_SPBASE)
                    {
                        varLoc.vlStk.vlsBaseReg = (regNumber)ICorDebugInfo::REGNUM_AMBIENT_SP;
                    }
                    break;

#ifndef _TARGET_64BIT_
                case TYP_LONG:
                case TYP_DOUBLE:
                    varLoc.vlType             = Compiler::VLT_STK2;
                    varLoc.vlStk2.vls2BaseReg = baseReg;
                    varLoc.vlStk2.vls2Offset  = offset;
                    if (!isFramePointerUsed() && varLoc.vlStk2.vls2BaseReg == REG_SPBASE)
                    {
                        varLoc.vlStk2.vls2BaseReg = (regNumber)ICorDebugInfo::REGNUM_AMBIENT_SP;
                    }
                    break;
#endif // !_TARGET_64BIT_

                default:
                    noway_assert(!"Invalid type");
            }
        }

        genSetScopeInfo(psiScopeCnt + i, startOffs, endOffs - startOffs, scopeL->scVarNum, scopeL->scLVnum,
                        scopeL->scAvailable, varLoc);
    }

    compiler->eeSetLVdone();
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
        if ((genTrnslLocalVarInfo[i].tlviVarLoc.vlIsOnStk((regNumber)reg, stkOffs)) &&
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
    ipMapping->ipmdNativeLoc.Print();
    // We can only call this after code generation. Is there any way to tell when it's legal to call?
    // printf(" [%x]", ipMapping->ipmdNativeLoc.CodeOffset(getEmitter()));

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

            if (offsx != ICorDebugInfo::NO_MAPPING)
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

    Compiler::IPmappingDsc* addMapping =
        (Compiler::IPmappingDsc*)compiler->compGetMem(sizeof(*addMapping), CMK_DebugInfo);

    addMapping->ipmdNativeLoc.CaptureLocation(getEmitter());
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

    Compiler::IPmappingDsc* addMapping =
        (Compiler::IPmappingDsc*)compiler->compGetMem(sizeof(*addMapping), CMK_DebugInfo);

    addMapping->ipmdNativeLoc.CaptureLocation(getEmitter());
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

    if (compiler->genIPmappingLast->ipmdNativeLoc.IsCurrentLocation(getEmitter()))
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

        UNATIVE_OFFSET nextNativeOfs = tmpMapping->ipmdNativeLoc.CodeOffset(getEmitter());

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
                         lastNativeOfs == prevMapping->ipmdNativeLoc.CodeOffset(getEmitter()));

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

        UNATIVE_OFFSET nextNativeOfs = tmpMapping->ipmdNativeLoc.CodeOffset(getEmitter());
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
            if ((block->bbRefs > 1) && (block->bbTreeList != nullptr))
            {
                noway_assert(block->bbTreeList->gtOper == GT_STMT);
                bool found = false;
                if (block->bbTreeList->gtStmt.gtStmtILoffsx != BAD_IL_OFFSET)
                {
                    IL_OFFSET ilOffs = jitGetILoffs(block->bbTreeList->gtStmt.gtStmtILoffsx);
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

#endif // DEBUGGING_SUPPORT

/*============================================================================
 *
 *   These are empty stubs to help the late dis-assembler to compile
 *   if DEBUGGING_SUPPORT is not enabled, or the late disassembler is being
 *   built into a non-DEBUG build.
 *
 *============================================================================
 */

#if defined(LATE_DISASM)
#if !defined(DEBUGGING_SUPPORT) || !defined(DEBUG)

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
#endif // !defined(DEBUGGING_SUPPORT) || !defined(DEBUG)
#endif // defined(LATE_DISASM)
/*****************************************************************************/
