// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           RegSet                                          XX
XX                                                                           XX
XX  Represents the register set, and their states during code generation     XX
XX  Can select an unused register, keeps track of the contents of the        XX
XX  registers, and can spill registers                                       XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "emit.h"

/*****************************************************************************/

#ifdef _TARGET_ARM64_
const regMaskSmall regMasks[] = {
#define REGDEF(name, rnum, mask, xname, wname) mask,
#include "register.h"
};
#else // !_TARGET_ARM64_
const regMaskSmall regMasks[] = {
#define REGDEF(name, rnum, mask, sname) mask,
#include "register.h"
};
#endif

#ifdef _TARGET_X86_
const regMaskSmall regFPMasks[] = {
#define REGDEF(name, rnum, mask, sname) mask,
#include "registerfp.h"
};
#endif // _TARGET_X86_

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                          RegSet                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

void RegSet::rsClearRegsModified()
{
#ifndef LEGACY_BACKEND
    assert(m_rsCompiler->lvaDoneFrameLayout < Compiler::FINAL_FRAME_LAYOUT);
#endif // !LEGACY_BACKEND

#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("Clearing modified regs.\n");
    }
    rsModifiedRegsMaskInitialized = true;
#endif // DEBUG

    rsModifiedRegsMask = RBM_NONE;
}

void RegSet::rsSetRegsModified(regMaskTP mask DEBUGARG(bool suppressDump))
{
    assert(mask != RBM_NONE);
    assert(rsModifiedRegsMaskInitialized);

#ifndef LEGACY_BACKEND
    // We can't update the modified registers set after final frame layout (that is, during code
    // generation and after). Ignore prolog and epilog generation: they call register tracking to
    // modify rbp, for example, even in functions that use rbp as a frame pointer. Make sure normal
    // code generation isn't actually adding to set of modified registers.
    // Frame layout is only affected by callee-saved registers, so only ensure that callee-saved
    // registers aren't modified after final frame layout.
    assert((m_rsCompiler->lvaDoneFrameLayout < Compiler::FINAL_FRAME_LAYOUT) || m_rsCompiler->compGeneratingProlog ||
           m_rsCompiler->compGeneratingEpilog ||
           (((rsModifiedRegsMask | mask) & RBM_CALLEE_SAVED) == (rsModifiedRegsMask & RBM_CALLEE_SAVED)));
#endif // !LEGACY_BACKEND

#ifdef DEBUG
    if (m_rsCompiler->verbose && !suppressDump)
    {
        if (rsModifiedRegsMask != (rsModifiedRegsMask | mask))
        {
            printf("Marking regs modified: ");
            dspRegMask(mask);
            printf(" (");
            dspRegMask(rsModifiedRegsMask);
            printf(" => ");
            dspRegMask(rsModifiedRegsMask | mask);
            printf(")\n");
        }
    }
#endif // DEBUG

    rsModifiedRegsMask |= mask;
}

void RegSet::rsRemoveRegsModified(regMaskTP mask)
{
    assert(mask != RBM_NONE);
    assert(rsModifiedRegsMaskInitialized);

#ifndef LEGACY_BACKEND
    // See comment in rsSetRegsModified().
    assert((m_rsCompiler->lvaDoneFrameLayout < Compiler::FINAL_FRAME_LAYOUT) || m_rsCompiler->compGeneratingProlog ||
           m_rsCompiler->compGeneratingEpilog ||
           (((rsModifiedRegsMask & ~mask) & RBM_CALLEE_SAVED) == (rsModifiedRegsMask & RBM_CALLEE_SAVED)));
#endif // !LEGACY_BACKEND

#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("Removing modified regs: ");
        dspRegMask(mask);
        if (rsModifiedRegsMask == (rsModifiedRegsMask & ~mask))
        {
            printf(" (unchanged)");
        }
        else
        {
            printf(" (");
            dspRegMask(rsModifiedRegsMask);
            printf(" => ");
            dspRegMask(rsModifiedRegsMask & ~mask);
            printf(")");
        }
        printf("\n");
    }
#endif // DEBUG

    rsModifiedRegsMask &= ~mask;
}

void RegSet::SetMaskVars(regMaskTP newMaskVars)
{
#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("\t\t\t\t\t\t\tLive regs: ");
        if (_rsMaskVars == newMaskVars)
        {
            printf("(unchanged) ");
        }
        else
        {
            printRegMaskInt(_rsMaskVars);
            m_rsCompiler->getEmitter()->emitDispRegSet(_rsMaskVars);
            printf(" => ");
        }
        printRegMaskInt(newMaskVars);
        m_rsCompiler->getEmitter()->emitDispRegSet(newMaskVars);
        printf("\n");
    }
#endif // DEBUG

    _rsMaskVars = newMaskVars;
}

#ifdef DEBUG

RegSet::rsStressRegsType RegSet::rsStressRegs()
{
#ifndef LEGACY_BACKEND
    return RS_STRESS_NONE;
#else  // LEGACY_BACKEND
    rsStressRegsType val = (rsStressRegsType)JitConfig.JitStressRegs();
    if (val == RS_STRESS_NONE && m_rsCompiler->compStressCompile(Compiler::STRESS_REGS, 15))
        val = RS_PICK_BAD_REG;
    return val;
#endif // LEGACY_BACKEND
}
#endif // DEBUG

#ifdef LEGACY_BACKEND
/*****************************************************************************
 *  Includes 'includeHint' if 'regs' is empty
 */

regMaskTP RegSet::rsUseIfZero(regMaskTP regs, regMaskTP includeHint)
{
    return regs ? regs : includeHint;
}

/*****************************************************************************
 *  Excludes 'excludeHint' if it results in a non-empty mask
 */

regMaskTP RegSet::rsExcludeHint(regMaskTP regs, regMaskTP excludeHint)
{
    regMaskTP OKmask = regs & ~excludeHint;
    return OKmask ? OKmask : regs;
}

/*****************************************************************************
 *  Narrows choice by 'narrowHint' if it results in a non-empty mask
 */

regMaskTP RegSet::rsNarrowHint(regMaskTP regs, regMaskTP narrowHint)
{
    regMaskTP narrowed = regs & narrowHint;
    return narrowed ? narrowed : regs;
}

/*****************************************************************************
 *  Excludes 'exclude' from regs if non-zero, or from RBM_ALLINT
 */

regMaskTP RegSet::rsMustExclude(regMaskTP regs, regMaskTP exclude)
{
    // Try to exclude from current set
    regMaskTP OKmask = regs & ~exclude;

    // If current set wont work, exclude from RBM_ALLINT
    if (OKmask == RBM_NONE)
        OKmask = (RBM_ALLINT & ~exclude);

    assert(OKmask);

    return OKmask;
}

/*****************************************************************************
 *
 *  The following returns a mask that yields all free registers.
 */

// inline
regMaskTP RegSet::rsRegMaskFree()
{
    /* Any register that is locked must also be marked as 'used' */

    assert((rsMaskUsed & rsMaskLock) == rsMaskLock);

    /* Any register that isn't used and doesn't hold a variable is free */

    return RBM_ALLINT & ~(rsMaskUsed | rsMaskVars | rsMaskResvd);
}

/*****************************************************************************
 *
 *  The following returns a mask of registers that may be grabbed.
 */

// inline
regMaskTP RegSet::rsRegMaskCanGrab()
{
    /* Any register that is locked must also be marked as 'used' */

    assert((rsMaskUsed & rsMaskLock) == rsMaskLock);

    /* Any register that isn't locked and doesn't hold a var can be grabbed */

    regMaskTP result = (RBM_ALLINT & ~(rsMaskLock | rsMaskVars));

#ifdef _TARGET_ARM_

    // On the ARM when we pass structs in registers we set the rsUsedTree[]
    // to be the full TYP_STRUCT tree, which doesn't allow us to spill/unspill
    // these argument registers.  To fix JitStress issues that can occur
    // when rsPickReg tries to spill one of these registers we just remove them
    // from the set of registers that we can grab
    //
    regMaskTP structArgMask = RBM_NONE;
    // Load all the variable arguments in registers back to their registers.
    for (regNumber reg = REG_ARG_FIRST; reg <= REG_ARG_LAST; reg = REG_NEXT(reg))
    {
        GenTreePtr regHolds = rsUsedTree[reg];
        if ((regHolds != NULL) && (regHolds->TypeGet() == TYP_STRUCT))
        {
            structArgMask |= genRegMask(reg);
        }
    }
    result &= ~structArgMask;
#endif

    return result;
}

/*****************************************************************************
 *
 *  Pick a free register. It is guaranteed that a register is available.
 *  Note that rsPickReg() can spill a register, whereas rsPickFreeReg() will not.
 */

// inline
regNumber RegSet::rsPickFreeReg(regMaskTP regMaskHint)
{
    regMaskTP freeRegs = rsRegMaskFree();
    assert(freeRegs != RBM_NONE);

    regMaskTP regs = rsNarrowHint(freeRegs, regMaskHint);

    return rsGrabReg(regs);
}

/*****************************************************************************
 *
 *  Mark the given set of registers as used and locked.
 */

// inline
void RegSet::rsLockReg(regMaskTP regMask)
{
    /* Must not be already marked as either used or locked */

    assert((rsMaskUsed & regMask) == 0);
    rsMaskUsed |= regMask;
    assert((rsMaskLock & regMask) == 0);
    rsMaskLock |= regMask;
}

/*****************************************************************************
 *
 *  Mark an already used set of registers as locked.
 */

// inline
void RegSet::rsLockUsedReg(regMaskTP regMask)
{
    /* Must not be already marked as locked. Must be already marked as used. */

    assert((rsMaskLock & regMask) == 0);
    assert((rsMaskUsed & regMask) == regMask);

    rsMaskLock |= regMask;
}

/*****************************************************************************
 *
 *  Mark the given set of registers as no longer used/locked.
 */

// inline
void RegSet::rsUnlockReg(regMaskTP regMask)
{
    /* Must be currently marked as both used and locked */

    assert((rsMaskUsed & regMask) == regMask);
    rsMaskUsed -= regMask;
    assert((rsMaskLock & regMask) == regMask);
    rsMaskLock -= regMask;
}

/*****************************************************************************
 *
 *  Mark the given set of registers as no longer locked.
 */

// inline
void RegSet::rsUnlockUsedReg(regMaskTP regMask)
{
    /* Must be currently marked as both used and locked */

    assert((rsMaskUsed & regMask) == regMask);
    assert((rsMaskLock & regMask) == regMask);
    rsMaskLock -= regMask;
}

/*****************************************************************************
 *
 *  Mark the given set of registers as used and locked. It may already have
 *  been marked as used.
 */

// inline
void RegSet::rsLockReg(regMaskTP regMask, regMaskTP* usedMask)
{
    /* Is it already marked as used? */

    regMaskTP used   = (rsMaskUsed & regMask);
    regMaskTP unused = (regMask & ~used);

    if (used)
        rsLockUsedReg(used);

    if (unused)
        rsLockReg(unused);

    *usedMask = used;
}

/*****************************************************************************
 *
 *  Mark the given set of registers as no longer
 */

// inline
void RegSet::rsUnlockReg(regMaskTP regMask, regMaskTP usedMask)
{
    regMaskTP unused = (regMask & ~usedMask);

    if (usedMask)
        rsUnlockUsedReg(usedMask);

    if (unused)
        rsUnlockReg(unused);
}
#endif // LEGACY_BACKEND

/*****************************************************************************
 *
 *  Assume all registers contain garbage (called at start of codegen and when
 *  we encounter a code label).
 */

// inline
void RegTracker::rsTrackRegClr()
{
    assert(RV_TRASH == 0);
    memset(rsRegValues, 0, sizeof(rsRegValues));
}

/*****************************************************************************
 *
 *  Trash the rsRegValues associated with a register
 */

// inline
void RegTracker::rsTrackRegTrash(regNumber reg)
{
    /* Keep track of which registers we ever touch */

    regSet->rsSetRegsModified(genRegMask(reg));

    /* Record the new value for the register */

    rsRegValues[reg].rvdKind = RV_TRASH;
}

/*****************************************************************************
 *
 *  calls rsTrackRegTrash on the set of registers in regmask
 */

// inline
void RegTracker::rsTrackRegMaskTrash(regMaskTP regMask)
{
    regMaskTP regBit = 1;

    for (regNumber regNum = REG_FIRST; regNum < REG_COUNT; regNum = REG_NEXT(regNum), regBit <<= 1)
    {
        if (regBit > regMask)
        {
            break;
        }

        if (regBit & regMask)
        {
            rsTrackRegTrash(regNum);
        }
    }
}

/*****************************************************************************/

// inline
void RegTracker::rsTrackRegIntCns(regNumber reg, ssize_t val)
{
    assert(genIsValidIntReg(reg));

    /* Keep track of which registers we ever touch */

    regSet->rsSetRegsModified(genRegMask(reg));

    /* Record the new value for the register */

    rsRegValues[reg].rvdKind      = RV_INT_CNS;
    rsRegValues[reg].rvdIntCnsVal = val;
}

/*****************************************************************************/

// inline
void RegTracker::rsTrackRegLclVarLng(regNumber reg, unsigned var, bool low)
{
    assert(genIsValidIntReg(reg));

    if (compiler->lvaTable[var].lvAddrExposed)
    {
        return;
    }

    /* Keep track of which registers we ever touch */

    regSet->rsSetRegsModified(genRegMask(reg));

    /* Record the new value for the register */

    rsRegValues[reg].rvdKind      = (low ? RV_LCL_VAR_LNG_LO : RV_LCL_VAR_LNG_HI);
    rsRegValues[reg].rvdLclVarNum = var;
}

/*****************************************************************************/

// inline
bool RegTracker::rsTrackIsLclVarLng(regValKind rvKind)
{
    if (compiler->opts.MinOpts() || compiler->opts.compDbgCode)
    {
        return false;
    }

    if (rvKind == RV_LCL_VAR_LNG_LO || rvKind == RV_LCL_VAR_LNG_HI)
    {
        return true;
    }
    else
    {
        return false;
    }
}

/*****************************************************************************/

// inline
void RegTracker::rsTrackRegClsVar(regNumber reg, GenTreePtr clsVar)
{
    rsTrackRegTrash(reg);
}

/*****************************************************************************/

// inline
void RegTracker::rsTrackRegAssign(GenTree* op1, GenTree* op2)
{
    /* Constant/bitvalue has precedence over local */
    switch (rsRegValues[op2->gtRegNum].rvdKind)
    {
        case RV_INT_CNS:
            break;

        default:

            /* Mark RHS register as containing the value */

            switch (op1->gtOper)
            {
                case GT_LCL_VAR:
                    rsTrackRegLclVar(op2->gtRegNum, op1->gtLclVarCommon.gtLclNum);
                    break;
                case GT_CLS_VAR:
                    rsTrackRegClsVar(op2->gtRegNum, op1);
                    break;
                default:
                    break;
            }
    }
}

#ifdef LEGACY_BACKEND

/*****************************************************************************
 *
 *  Given a regmask, find the best regPairNo that can be formed
 *  or return REG_PAIR_NONE if no register pair can be formed
 */

regPairNo RegSet::rsFindRegPairNo(regMaskTP regAllowedMask)
{
    regPairNo regPair;

    // Remove any special purpose registers such as SP, EBP, etc...
    regMaskTP specialUseMask = (rsMaskResvd | RBM_SPBASE);
#if ETW_EBP_FRAMED
    specialUseMask |= RBM_FPBASE;
#else
    if (m_rsCompiler->codeGen->isFramePointerUsed())
        specialUseMask |= RBM_FPBASE;
#endif

    regAllowedMask &= ~specialUseMask;

    /* Check if regAllowedMask has zero or one bits set */
    if ((regAllowedMask & (regAllowedMask - 1)) == 0)
    {
        /* If so we won't be able to find a reg pair */
        return REG_PAIR_NONE;
    }

#ifdef _TARGET_X86_
    if (regAllowedMask & RBM_EAX)
    {
        /* EAX is available, see if we can pair it with another reg */

        if (regAllowedMask & RBM_EDX)
        {
            regPair = REG_PAIR_EAXEDX;
            goto RET;
        }
        if (regAllowedMask & RBM_ECX)
        {
            regPair = REG_PAIR_EAXECX;
            goto RET;
        }
        if (regAllowedMask & RBM_EBX)
        {
            regPair = REG_PAIR_EAXEBX;
            goto RET;
        }
        if (regAllowedMask & RBM_ESI)
        {
            regPair = REG_PAIR_EAXESI;
            goto RET;
        }
        if (regAllowedMask & RBM_EDI)
        {
            regPair = REG_PAIR_EAXEDI;
            goto RET;
        }
        if (regAllowedMask & RBM_EBP)
        {
            regPair = REG_PAIR_EAXEBP;
            goto RET;
        }
    }

    if (regAllowedMask & RBM_ECX)
    {
        /* ECX is available, see if we can pair it with another reg */

        if (regAllowedMask & RBM_EDX)
        {
            regPair = REG_PAIR_ECXEDX;
            goto RET;
        }
        if (regAllowedMask & RBM_EBX)
        {
            regPair = REG_PAIR_ECXEBX;
            goto RET;
        }
        if (regAllowedMask & RBM_ESI)
        {
            regPair = REG_PAIR_ECXESI;
            goto RET;
        }
        if (regAllowedMask & RBM_EDI)
        {
            regPair = REG_PAIR_ECXEDI;
            goto RET;
        }
        if (regAllowedMask & RBM_EBP)
        {
            regPair = REG_PAIR_ECXEBP;
            goto RET;
        }
    }

    if (regAllowedMask & RBM_EDX)
    {
        /* EDX is available, see if we can pair it with another reg */

        if (regAllowedMask & RBM_EBX)
        {
            regPair = REG_PAIR_EDXEBX;
            goto RET;
        }
        if (regAllowedMask & RBM_ESI)
        {
            regPair = REG_PAIR_EDXESI;
            goto RET;
        }
        if (regAllowedMask & RBM_EDI)
        {
            regPair = REG_PAIR_EDXEDI;
            goto RET;
        }
        if (regAllowedMask & RBM_EBP)
        {
            regPair = REG_PAIR_EDXEBP;
            goto RET;
        }
    }

    if (regAllowedMask & RBM_EBX)
    {
        /* EBX is available, see if we can pair it with another reg */

        if (regAllowedMask & RBM_ESI)
        {
            regPair = REG_PAIR_EBXESI;
            goto RET;
        }
        if (regAllowedMask & RBM_EDI)
        {
            regPair = REG_PAIR_EBXEDI;
            goto RET;
        }
        if (regAllowedMask & RBM_EBP)
        {
            regPair = REG_PAIR_EBXEBP;
            goto RET;
        }
    }

    if (regAllowedMask & RBM_ESI)
    {
        /* ESI is available, see if we can pair it with another reg */

        if (regAllowedMask & RBM_EDI)
        {
            regPair = REG_PAIR_ESIEDI;
            goto RET;
        }
        if (regAllowedMask & RBM_EBP)
        {
            regPair = REG_PAIR_EBPESI;
            goto RET;
        }
    }

    if (regAllowedMask & RBM_EDI)
    {
        /* EDI is available, see if we can pair it with another reg */

        if (regAllowedMask & RBM_EBP)
        {
            regPair = REG_PAIR_EBPEDI;
            goto RET;
        }
    }
#endif

#ifdef _TARGET_ARM_
    // ARM is symmetric, so don't bother to prefer some pairs to others
    //
    // Iterate the registers in the order specified by rpRegTmpOrder/raRegTmpOrder

    for (unsigned index1 = 0; index1 < REG_TMP_ORDER_COUNT; index1++)
    {
        regNumber reg1;
        if (m_rsCompiler->rpRegAllocDone)
            reg1 = raRegTmpOrder[index1];
        else
            reg1 = rpRegTmpOrder[index1];

        regMaskTP reg1Mask = genRegMask(reg1);

        if ((regAllowedMask & reg1Mask) == 0)
            continue;

        for (unsigned index2 = index1 + 1; index2 < REG_TMP_ORDER_COUNT; index2++)
        {
            regNumber reg2;
            if (m_rsCompiler->rpRegAllocDone)
                reg2 = raRegTmpOrder[index2];
            else
                reg2 = rpRegTmpOrder[index2];

            regMaskTP reg2Mask = genRegMask(reg2);

            if ((regAllowedMask & reg2Mask) == 0)
                continue;

            regMaskTP pairMask = genRegMask(reg1) | genRegMask(reg2);

            // if reg1 is larger than reg2 then swap the registers
            if (reg1 > reg2)
            {
                regNumber regT = reg1;
                reg1           = reg2;
                reg2           = regT;
            }

            regPair = gen2regs2pair(reg1, reg2);
            return regPair;
        }
    }
#endif

    assert(!"Unreachable code");
    regPair = REG_PAIR_NONE;

#ifdef _TARGET_X86_
RET:
#endif

    return regPair;
}

#endif // LEGACY_BACKEND

/*****************************************************************************/

RegSet::RegSet(Compiler* compiler, GCInfo& gcInfo) : m_rsCompiler(compiler), m_rsGCInfo(gcInfo)
{
    /* Initialize the spill logic */

    rsSpillInit();

    /* Initialize the argument register count */
    // TODO-Cleanup: Consider moving intRegState and floatRegState to RegSet.  They used
    // to be initialized here, but are now initialized in the CodeGen constructor.
    // intRegState.rsCurRegArgNum   = 0;
    // loatRegState.rsCurRegArgNum = 0;

    rsMaskResvd = RBM_NONE;

#ifdef LEGACY_BACKEND
    rsMaskMult = RBM_NONE;
    rsMaskUsed = RBM_NONE;
    rsMaskLock = RBM_NONE;
#endif // LEGACY_BACKEND

#ifdef _TARGET_ARMARCH_
    rsMaskCalleeSaved = RBM_NONE;
#endif // _TARGET_ARMARCH_

#ifdef _TARGET_ARM_
    rsMaskPreSpillRegArg = RBM_NONE;
    rsMaskPreSpillAlign  = RBM_NONE;
#endif

#ifdef DEBUG
    rsModifiedRegsMaskInitialized = false;
#endif // DEBUG
}

#ifdef LEGACY_BACKEND
/*****************************************************************************
 *
 *  Marks the register that holds the given operand value as 'used'. If 'addr'
 *  is non-zero, the register is part of a complex address mode that needs to
 *  be marked if the register is ever spilled.
 */

void RegSet::rsMarkRegUsed(GenTreePtr tree, GenTreePtr addr)
{
    var_types type;
    regNumber regNum;
    regMaskTP regMask;

    /* The value must be sitting in a register */

    assert(tree);
    assert(tree->gtFlags & GTF_REG_VAL);

    type   = tree->TypeGet();
    regNum = tree->gtRegNum;

    if (isFloatRegType(type))
        regMask = genRegMaskFloat(regNum, type);
    else
        regMask = genRegMask(regNum);

#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("\t\t\t\t\t\t\tThe register %s currently holds ", m_rsCompiler->compRegVarName(regNum));
        Compiler::printTreeID(tree);
        if (addr != NULL)
        {
            printf("/");
            Compiler::printTreeID(addr);
        }
        else if (tree->gtOper == GT_CNS_INT)
        {
            if (tree->IsIconHandle())
                printf(" / Handle(0x%08p)", dspPtr(tree->gtIntCon.gtIconVal));
            else
                printf(" / Constant(0x%X)", tree->gtIntCon.gtIconVal);
        }
        printf("]\n");
    }
#endif // DEBUG

    /* Remember whether the register holds a pointer */

    m_rsGCInfo.gcMarkRegPtrVal(regNum, type);

    /* No locked register may ever be marked as free */

    assert((rsMaskLock & rsRegMaskFree()) == 0);

    /* Is the register used by two different values simultaneously? */

    if (regMask & rsMaskUsed)
    {
        /* Save the preceding use information */

        rsRecMultiReg(regNum, type);
    }

    /* Set the register's bit in the 'used' bitset */

    rsMaskUsed |= regMask;

    /* Remember what values are in what registers, in case we have to spill */
    assert(regNum != REG_SPBASE);
    assert(rsUsedTree[regNum] == NULL);
    rsUsedTree[regNum] = tree;
    assert(rsUsedAddr[regNum] == NULL);
    rsUsedAddr[regNum] = addr;
}

void RegSet::rsMarkArgRegUsedByPromotedFieldArg(GenTreePtr promotedStructArg, regNumber regNum, bool isGCRef)
{
    regMaskTP regMask;

    /* The value must be sitting in a register */

    assert(promotedStructArg);
    assert(promotedStructArg->TypeGet() == TYP_STRUCT);

    assert(regNum < MAX_REG_ARG);
    regMask = genRegMask(regNum);
    assert((regMask & RBM_ARG_REGS) != RBM_NONE);

#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("\t\t\t\t\t\t\tThe register %s currently holds ", m_rsCompiler->compRegVarName(regNum));
        Compiler::printTreeID(promotedStructArg);
        if (promotedStructArg->gtOper == GT_CNS_INT)
        {
            if (promotedStructArg->IsIconHandle())
                printf(" / Handle(0x%08p)", dspPtr(promotedStructArg->gtIntCon.gtIconVal));
            else
                printf(" / Constant(0x%X)", promotedStructArg->gtIntCon.gtIconVal);
        }
        printf("]\n");
    }
#endif

    /* Remember whether the register holds a pointer */

    m_rsGCInfo.gcMarkRegPtrVal(regNum, (isGCRef ? TYP_REF : TYP_INT));

    /* No locked register may ever be marked as free */

    assert((rsMaskLock & rsRegMaskFree()) == 0);

    /* Is the register used by two different values simultaneously? */

    if (regMask & rsMaskUsed)
    {
        /* Save the preceding use information */

        assert(isValidIntArgReg(regNum)); // We are expecting only integer argument registers here
        rsRecMultiReg(regNum, TYP_I_IMPL);
    }

    /* Set the register's bit in the 'used' bitset */

    rsMaskUsed |= regMask;

    /* Remember what values are in what registers, in case we have to spill */
    assert(regNum != REG_SPBASE);
    assert(rsUsedTree[regNum] == 0);
    rsUsedTree[regNum] = promotedStructArg;
}

/*****************************************************************************
 *
 *  Marks the register pair that holds the given operand value as 'used'.
 */

void RegSet::rsMarkRegPairUsed(GenTreePtr tree)
{
    regNumber regLo;
    regNumber regHi;
    regPairNo regPair;
    regMaskTP regMask;

    /* The value must be sitting in a register */

    assert(tree);
#if CPU_HAS_FP_SUPPORT
    assert(tree->gtType == TYP_LONG);
#else
    assert(tree->gtType == TYP_LONG || tree->gtType == TYP_DOUBLE);
#endif
    assert(tree->gtFlags & GTF_REG_VAL);

    regPair = tree->gtRegPair;
    regMask = genRegPairMask(regPair);

    regLo = genRegPairLo(regPair);
    regHi = genRegPairHi(regPair);

#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("\t\t\t\t\t\t\tThe register %s currently holds \n", m_rsCompiler->compRegVarName(regLo));
        Compiler::printTreeID(tree);
        printf("/lo32\n");
        printf("\t\t\t\t\t\t\tThe register %s currently holds \n", m_rsCompiler->compRegVarName(regHi));
        Compiler::printTreeID(tree);
        printf("/hi32\n");
    }
#endif

    /* Neither register obviously holds a pointer value */

    m_rsGCInfo.gcMarkRegSetNpt(regMask);

    /* No locked register may ever be marked as free */

    assert((rsMaskLock & rsRegMaskFree()) == 0);

    /* Are the registers used by two different values simultaneously? */

    if (rsMaskUsed & genRegMask(regLo))
    {
        /* Save the preceding use information */

        rsRecMultiReg(regLo, TYP_INT);
    }

    if (rsMaskUsed & genRegMask(regHi))
    {
        /* Save the preceding use information */

        rsRecMultiReg(regHi, TYP_INT);
    }

    /* Can't mark a register pair more than once as used */

    // assert((regMask & rsMaskUsed) == 0);

    /* Mark the registers as 'used' */

    rsMaskUsed |= regMask;

    /* Remember what values are in what registers, in case we have to spill */

    if (regLo != REG_STK)
    {
        assert(rsUsedTree[regLo] == 0);
        assert(regLo != REG_SPBASE);
        rsUsedTree[regLo] = tree;
    }

    if (regHi != REG_STK)
    {
        assert(rsUsedTree[regHi] == 0);
        assert(regHi != REG_SPBASE);
        rsUsedTree[regHi] = tree;
    }
}

/*****************************************************************************
 *
 *  Returns true if the given tree is currently held in reg.
 *  Note that reg may by used by multiple trees, in which case we have
 *  to search rsMultiDesc[reg].
 */

bool RegSet::rsIsTreeInReg(regNumber reg, GenTreePtr tree)
{
    /* First do the trivial check */

    if (rsUsedTree[reg] == tree)
        return true;

    /* If the register is used by multiple trees, we have to search the list
       in rsMultiDesc[reg] */

    if (genRegMask(reg) & rsMaskMult)
    {
        SpillDsc* multiDesc = rsMultiDesc[reg];
        assert(multiDesc);

        for (/**/; multiDesc; multiDesc = multiDesc->spillNext)
        {
            if (multiDesc->spillTree == tree)
                return true;

            assert((!multiDesc->spillNext) == (!multiDesc->spillMoreMultis));
        }
    }

    /* Not found. It must be spilled */

    return false;
}
#endif // LEGACY_BACKEND

/*****************************************************************************
 *
 *  Finds the SpillDsc corresponding to 'tree' assuming it was spilled from 'reg'.
 */

RegSet::SpillDsc* RegSet::rsGetSpillInfo(GenTreePtr tree,
                                         regNumber  reg,
                                         SpillDsc** pPrevDsc
#ifdef LEGACY_BACKEND
                                         ,
                                         SpillDsc** pMultiDsc
#endif // LEGACY_BACKEND
                                         )
{
    /* Normally, trees are unspilled in the order of being spilled due to
       the post-order walking of trees during code-gen. However, this will
       not be true for something like a GT_ARR_ELEM node */
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef LEGACY_BACKEND
    SpillDsc* multi = rsSpillDesc[reg];
#endif // LEGACY_BACKEND

    SpillDsc* prev;
    SpillDsc* dsc;
    for (prev = nullptr, dsc = rsSpillDesc[reg]; dsc != nullptr; prev = dsc, dsc = dsc->spillNext)
    {
#ifdef LEGACY_BACKEND
        if (prev && !prev->spillMoreMultis)
            multi = dsc;
#endif // LEGACY_BACKEND

        if (dsc->spillTree == tree)
        {
            break;
        }
    }

    if (pPrevDsc)
    {
        *pPrevDsc = prev;
    }
#ifdef LEGACY_BACKEND
    if (pMultiDsc)
        *pMultiDsc = multi;
#endif // LEGACY_BACKEND

    return dsc;
}

#ifdef LEGACY_BACKEND
/*****************************************************************************
 *
 *  Mark the register set given by the register mask as not used.
 */

void RegSet::rsMarkRegFree(regMaskTP regMask)
{
    /* Are we freeing any multi-use registers? */

    if (regMask & rsMaskMult)
    {
        rsMultRegFree(regMask);
        return;
    }

    m_rsGCInfo.gcMarkRegSetNpt(regMask);

    regMaskTP regBit = 1;

    for (regNumber regNum = REG_FIRST; regNum < REG_COUNT; regNum = REG_NEXT(regNum), regBit <<= 1)
    {
        if (regBit > regMask)
            break;

        if (regBit & regMask)
        {
#ifdef DEBUG
            if (m_rsCompiler->verbose)
            {
                printf("\t\t\t\t\t\t\tThe register %s no longer holds ", m_rsCompiler->compRegVarName(regNum));
                Compiler::printTreeID(rsUsedTree[regNum]);
                Compiler::printTreeID(rsUsedAddr[regNum]);
                printf("\n");
            }
#endif
            GenTreePtr usedTree = rsUsedTree[regNum];
            assert(usedTree != NULL);
            rsUsedTree[regNum] = NULL;
            rsUsedAddr[regNum] = NULL;
#ifdef _TARGET_ARM_
            if (usedTree->TypeGet() == TYP_DOUBLE)
            {
                regNum = REG_NEXT(regNum);
                regBit <<= 1;

                assert(regBit & regMask);
                assert(rsUsedTree[regNum] == NULL);
                assert(rsUsedAddr[regNum] == NULL);
            }
#endif
        }
    }

    /* Remove the register set from the 'used' set */

    assert((regMask & rsMaskUsed) == regMask);
    rsMaskUsed -= regMask;

    /* No locked register may ever be marked as free */

    assert((rsMaskLock & rsRegMaskFree()) == 0);
}

/*****************************************************************************
 *
 *  Free the register from the given tree. If the register holds other tree,
 *  it will still be marked as used, else it will be completely free.
 */

void RegSet::rsMarkRegFree(regNumber reg, GenTreePtr tree)
{
    assert(rsIsTreeInReg(reg, tree));
    regMaskTP regMask = genRegMask(reg);

    /* If the register is not multi-used, it's easy. Just do the default work */

    if (!(regMask & rsMaskMult))
    {
        rsMarkRegFree(regMask);
        return;
    }

    /* The tree is multi-used. We just have to free it off the given tree but
       leave other trees which use the register as they are. The register may
       not be multi-used after freeing it from the given tree */

    /* Is the tree in rsUsedTree[] or in rsMultiDesc[]?
       If it is in rsUsedTree[], update rsUsedTree[] */

    if (rsUsedTree[reg] == tree)
    {
        rsRmvMultiReg(reg);
        return;
    }

    /* The tree is in rsMultiDesc[] instead of in rsUsedTree[]. Find the desc
       corresponding to the tree and just remove it from there */

    for (SpillDsc *multiDesc = rsMultiDesc[reg], *prevDesc = NULL; multiDesc;
         prevDesc = multiDesc, multiDesc = multiDesc->spillNext)
    {
        /* If we find the descriptor with the tree we are looking for,
           discard it */

        if (multiDesc->spillTree != tree)
            continue;

        if (prevDesc == NULL)
        {
            /* The very first desc in rsMultiDesc[] matched. If there are
               no further descs, then the register is no longer multi-used */

            if (!multiDesc->spillMoreMultis)
                rsMaskMult -= regMask;

            rsMultiDesc[reg] = multiDesc->spillNext;
        }
        else
        {
            /* There are a couple of other descs before the match. So the
               register is still multi-used. However, we may have to
               update spillMoreMultis for the previous desc. */

            if (!multiDesc->spillMoreMultis)
                prevDesc->spillMoreMultis = false;

            prevDesc->spillNext = multiDesc->spillNext;
        }

        SpillDsc::freeDsc(this, multiDesc);

#ifdef DEBUG
        if (m_rsCompiler->verbose)
        {
            printf("\t\t\t\t\t\t\tRegister %s multi-use dec for ", m_rsCompiler->compRegVarName(reg));
            Compiler::printTreeID(tree);
            printf(" - now ");
            Compiler::printTreeID(rsUsedTree[reg]);
            printf(" multMask=" REG_MASK_ALL_FMT "\n", rsMaskMult);
        }
#endif

        return;
    }

    assert(!"Didn't find the spilled tree in rsMultiDesc[]");
}

/*****************************************************************************
 *
 *  Mark the register set given by the register mask as not used; there may
 *  be some 'multiple-use' registers in the set.
 */

void RegSet::rsMultRegFree(regMaskTP regMask)
{
    /* Free any multiple-use registers first */
    regMaskTP nonMultMask = regMask & ~rsMaskMult;
    regMaskTP myMultMask  = regMask & rsMaskMult;

    if (myMultMask)
    {
        regNumber regNum;
        regMaskTP regBit;

        for (regNum = REG_FIRST, regBit = 1; regNum < REG_COUNT; regNum = REG_NEXT(regNum), regBit <<= 1)
        {
            if (regBit > myMultMask)
                break;

            if (regBit & myMultMask)
            {
                /* Free the multi-use register 'regNum' */
                var_types type = rsRmvMultiReg(regNum);
#ifdef _TARGET_ARM_
                if (genIsValidFloatReg(regNum) && (type == TYP_DOUBLE))
                {
                    // On ARM32, We skip the second register for a TYP_DOUBLE
                    regNum = REG_NEXT(regNum);
                    regBit <<= 1;
                }
#endif // _TARGET_ARM_
            }
        }
    }

    /* If there are any single-use registers, free them */

    if (nonMultMask)
        rsMarkRegFree(nonMultMask);
}

/*****************************************************************************
 *
 *  Returns the number of registers that are currently free which appear in needReg.
 */

unsigned RegSet::rsFreeNeededRegCount(regMaskTP needReg)
{
    regMaskTP regNeededFree = rsRegMaskFree() & needReg;
    unsigned  cntFree       = 0;

    /* While some registers are free ... */

    while (regNeededFree)
    {
        /* Remove the next register bit and bump the count */

        regNeededFree -= genFindLowestBit(regNeededFree);
        cntFree += 1;
    }

    return cntFree;
}
#endif // LEGACY_BACKEND

/*****************************************************************************
 *
 *  Record the fact that the given register now contains the given local
 *  variable. Pointers are handled specially since reusing the register
 *  will extend the lifetime of a pointer register which is not a register
 *  variable.
 */

void RegTracker::rsTrackRegLclVar(regNumber reg, unsigned var)
{
    LclVarDsc* varDsc = &compiler->lvaTable[var];
    assert(reg != REG_STK);
#if CPU_HAS_FP_SUPPORT
    assert(varTypeIsFloating(varDsc->TypeGet()) == false);
#endif
    // Kill the register before doing anything in case we take a
    // shortcut out of here
    rsRegValues[reg].rvdKind = RV_TRASH;

    if (compiler->lvaTable[var].lvAddrExposed)
    {
        return;
    }

    /* Keep track of which registers we ever touch */

    regSet->rsSetRegsModified(genRegMask(reg));

#if REDUNDANT_LOAD

    /* Is the variable a pointer? */

    if (varTypeIsGC(varDsc->TypeGet()))
    {
        /* Don't track pointer register vars */

        if (varDsc->lvRegister)
        {
            return;
        }

        /* Don't track when fully interruptible */

        if (compiler->genInterruptible)
        {
            return;
        }
    }
    else if (varDsc->lvNormalizeOnLoad())
    {
        return;
    }

#endif

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("\t\t\t\t\t\t\tThe register %s now holds V%02u\n", compiler->compRegVarName(reg), var);
    }
#endif

    /* Record the new value for the register. ptr var needed for
     * lifetime extension
     */

    rsRegValues[reg].rvdKind = RV_LCL_VAR;

    // If this is a cast of a 64 bit int, then we must have the low 32 bits.
    if (genActualType(varDsc->TypeGet()) == TYP_LONG)
    {
        rsRegValues[reg].rvdKind = RV_LCL_VAR_LNG_LO;
    }

    rsRegValues[reg].rvdLclVarNum = var;
}

/*****************************************************************************/

void RegTracker::rsTrackRegSwap(regNumber reg1, regNumber reg2)
{
    RegValDsc tmp;

    tmp               = rsRegValues[reg1];
    rsRegValues[reg1] = rsRegValues[reg2];
    rsRegValues[reg2] = tmp;
}

void RegTracker::rsTrackRegCopy(regNumber reg1, regNumber reg2)
{
    /* Keep track of which registers we ever touch */

    assert(reg1 < REG_COUNT);
    assert(reg2 < REG_COUNT);

    regSet->rsSetRegsModified(genRegMask(reg1));

    rsRegValues[reg1] = rsRegValues[reg2];
}

#ifdef LEGACY_BACKEND

/*****************************************************************************
 *  One of the operands of this complex address mode has been spilled
 */

void rsAddrSpillOper(GenTreePtr addr)
{
    if (addr)
    {
        assert(addr->gtOper == GT_IND || addr->gtOper == GT_ARR_ELEM || addr->gtOper == GT_LEA ||
               addr->gtOper == GT_CMPXCHG);

        // GTF_SPILLED_OP2 says "both operands have been spilled"
        assert((addr->gtFlags & GTF_SPILLED_OP2) == 0);

        if ((addr->gtFlags & GTF_SPILLED_OPER) == 0)
            addr->gtFlags |= GTF_SPILLED_OPER;
        else
            addr->gtFlags |= GTF_SPILLED_OP2;
    }
}

void rsAddrUnspillOper(GenTreePtr addr)
{
    if (addr)
    {
        assert(addr->gtOper == GT_IND || addr->gtOper == GT_ARR_ELEM || addr->gtOper == GT_LEA ||
               addr->gtOper == GT_CMPXCHG);

        assert((addr->gtFlags & GTF_SPILLED_OPER) != 0);

        // Both operands spilled? */
        if ((addr->gtFlags & GTF_SPILLED_OP2) != 0)
            addr->gtFlags &= ~GTF_SPILLED_OP2;
        else
            addr->gtFlags &= ~GTF_SPILLED_OPER;
    }
}

void RegSet::rsSpillRegIfUsed(regNumber reg)
{
    if (rsMaskUsed & genRegMask(reg))
    {
        rsSpillReg(reg);
    }
}

#endif // LEGACY_BACKEND

//------------------------------------------------------------
// rsSpillTree: Spill the tree held in 'reg'.
//
// Arguments:
//   reg     -   Register of tree node that is to be spilled
//   tree    -   GenTree node that is being spilled
//   regIdx  -   Register index identifying the specific result
//               register of a multi-reg call node. For single-reg
//               producing tree nodes its value is zero.
//
// Return Value:
//   None.
//
// Assumption:
//    RyuJIT backend specific: in case of multi-reg call nodes, GTF_SPILL
//    flag associated with the reg that is being spilled is cleared.  The
//    caller of this method is expected to clear GTF_SPILL flag on call
//    node after all of its registers marked for spilling are spilled.
//
void RegSet::rsSpillTree(regNumber reg, GenTreePtr tree, unsigned regIdx /* =0 */)
{
    assert(tree != nullptr);

    GenTreeCall* call = nullptr;
    var_types    treeType;

#ifndef LEGACY_BACKEND
    if (tree->IsMultiRegCall())
    {
        call                        = tree->AsCall();
        ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
        treeType                    = retTypeDesc->GetReturnRegType(regIdx);
    }
    else
#endif
    {
        treeType = tree->TypeGet();
    }

    var_types tempType = Compiler::tmpNormalizeType(treeType);
    regMaskTP mask;
    bool      floatSpill = false;

    if (isFloatRegType(treeType))
    {
        floatSpill = true;
        mask       = genRegMaskFloat(reg, treeType);
    }
    else
    {
        mask = genRegMask(reg);
    }

    rsNeededSpillReg = true;

#ifdef LEGACY_BACKEND
    // The register we're spilling must be used but not locked
    // or an enregistered variable.

    assert((mask & rsMaskUsed) == mask);
    assert((mask & rsMaskLock) == 0);
    assert((mask & rsMaskVars) == 0);
#endif // LEGACY_BACKEND

#ifndef LEGACY_BACKEND
    // We should only be spilling nodes marked for spill,
    // vars should be handled elsewhere, and to prevent
    // spilling twice clear GTF_SPILL flag on tree node.
    //
    // In case of multi-reg call nodes only the spill flag
    // associated with the reg is cleared. Spill flag on
    // call node should be cleared by the caller of this method.
    assert(tree->gtOper != GT_REG_VAR);
    assert((tree->gtFlags & GTF_SPILL) != 0);

    unsigned regFlags = 0;
    if (call != nullptr)
    {
        regFlags = call->GetRegSpillFlagByIdx(regIdx);
        assert((regFlags & GTF_SPILL) != 0);
        regFlags &= ~GTF_SPILL;
    }
    else
    {
        assert(!varTypeIsMultiReg(tree));
        tree->gtFlags &= ~GTF_SPILL;
    }
#endif // !LEGACY_BACKEND

#if CPU_LONG_USES_REGPAIR
    // Are we spilling a part of a register pair?
    if (treeType == TYP_LONG)
    {
        tempType = TYP_I_IMPL;
        assert(genRegPairLo(tree->gtRegPair) == reg || genRegPairHi(tree->gtRegPair) == reg);
    }
    else
    {
        assert(tree->gtFlags & GTF_REG_VAL);
        assert(tree->gtRegNum == reg);
    }
#else
    assert(tree->InReg());
    assert(tree->gtRegNum == reg || (call != nullptr && call->GetRegNumByIdx(regIdx) == reg));
#endif // CPU_LONG_USES_REGPAIR

    // Are any registers free for spillage?
    SpillDsc* spill = SpillDsc::alloc(m_rsCompiler, this, tempType);

    // Grab a temp to store the spilled value
    TempDsc* temp    = m_rsCompiler->tmpGetTemp(tempType);
    spill->spillTemp = temp;
    tempType         = temp->tdTempType();

    // Remember what it is we have spilled
    spill->spillTree = tree;
#ifdef LEGACY_BACKEND
    spill->spillAddr = rsUsedAddr[reg];
#endif // LEGACY_BACKEND

#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("\t\t\t\t\t\t\tThe register %s spilled with    ", m_rsCompiler->compRegVarName(reg));
        Compiler::printTreeID(spill->spillTree);
#ifdef LEGACY_BACKEND
        printf("/");
        Compiler::printTreeID(spill->spillAddr);
#endif // LEGACY_BACKEND
    }
#endif

#ifdef LEGACY_BACKEND
    // Is the register part of a complex address mode?
    rsAddrSpillOper(rsUsedAddr[reg]);
#endif // LEGACY_BACKEND

    // 'lastDsc' is 'spill' for simple cases, and will point to the last
    // multi-use descriptor if 'reg' is being multi-used
    SpillDsc* lastDsc = spill;

#ifdef LEGACY_BACKEND
    if ((rsMaskMult & mask) == 0)
    {
        spill->spillMoreMultis = false;
    }
    else
    {
        // The register is being multi-used and will have entries in
        // rsMultiDesc[reg]. Spill all of them (ie. move them to
        // rsSpillDesc[reg]).
        // When we unspill the reg, they will all be moved back to
        // rsMultiDesc[].

        spill->spillMoreMultis = true;

        SpillDsc* nextDsc = rsMultiDesc[reg];

        do
        {
            assert(nextDsc != nullptr);

            // Is this multi-use part of a complex address mode?
            rsAddrSpillOper(nextDsc->spillAddr);

            // Mark the tree node as having been spilled
            rsMarkSpill(nextDsc->spillTree, reg);

            // lastDsc points to the last of the multi-spill descrs for 'reg'
            nextDsc->spillTemp = temp;

#ifdef DEBUG
            if (m_rsCompiler->verbose)
            {
                printf(", ");
                Compiler::printTreeID(nextDsc->spillTree);
                printf("/");
                Compiler::printTreeID(nextDsc->spillAddr);
            }
#endif

            lastDsc->spillNext = nextDsc;
            lastDsc            = nextDsc;

            nextDsc = nextDsc->spillNext;
        } while (lastDsc->spillMoreMultis);

        rsMultiDesc[reg] = nextDsc;

        // 'reg' is no longer considered to be multi-used. We will set this
        // mask again when this value gets unspilled
        rsMaskMult &= ~mask;
    }
#endif // LEGACY_BACKEND

    // Insert the spill descriptor(s) in the list
    lastDsc->spillNext = rsSpillDesc[reg];
    rsSpillDesc[reg]   = spill;

#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("\n");
    }
#endif

    // Generate the code to spill the register
    var_types storeType = floatSpill ? treeType : tempType;

    m_rsCompiler->codeGen->spillReg(storeType, temp, reg);

    // Mark the tree node as having been spilled
    rsMarkSpill(tree, reg);

#ifdef LEGACY_BACKEND
    // The register is now free
    rsMarkRegFree(mask);
#else
    // In case of multi-reg call node also mark the specific
    // result reg as spilled.
    if (call != nullptr)
    {
        regFlags |= GTF_SPILLED;
        call->SetRegSpillFlagByIdx(regFlags, regIdx);
    }
#endif //! LEGACY_BACKEND
}

#if defined(_TARGET_X86_) && !FEATURE_STACK_FP_X87
/*****************************************************************************
*
*  Spill the top of the FP x87 stack.
*/
void RegSet::rsSpillFPStack(GenTreePtr tree)
{
    SpillDsc* spill;
    TempDsc*  temp;
    var_types treeType = tree->TypeGet();

    assert(tree->OperGet() == GT_CALL);
    spill = SpillDsc::alloc(m_rsCompiler, this, treeType);

    /* Grab a temp to store the spilled value */

    spill->spillTemp = temp = m_rsCompiler->tmpGetTemp(treeType);

    /* Remember what it is we have spilled */

    spill->spillTree  = tree;
    SpillDsc* lastDsc = spill;

    regNumber reg      = tree->gtRegNum;
    lastDsc->spillNext = rsSpillDesc[reg];
    rsSpillDesc[reg]   = spill;

#ifdef DEBUG
    if (m_rsCompiler->verbose)
        printf("\n");
#endif
    // m_rsCompiler->codeGen->inst_FS_ST(INS_fstp, emitActualTypeSize(treeType), temp, 0);
    m_rsCompiler->codeGen->getEmitter()->emitIns_S(INS_fstp, emitActualTypeSize(treeType), temp->tdTempNum(), 0);

    /* Mark the tree node as having been spilled */

    rsMarkSpill(tree, reg);
}
#endif // defined(_TARGET_X86_) && !FEATURE_STACK_FP_X87

#ifdef LEGACY_BACKEND

/*****************************************************************************
 *
 *  Spill the given register (which we assume to be currently marked as used).
 */

void RegSet::rsSpillReg(regNumber reg)
{
    /* We must know the value in the register that we are spilling */
    GenTreePtr tree = rsUsedTree[reg];

#ifdef _TARGET_ARM_
    if (tree == NULL && genIsValidFloatReg(reg) && !genIsValidDoubleReg(reg))
    {
        reg = REG_PREV(reg);
        assert(rsUsedTree[reg]);
        assert(rsUsedTree[reg]->TypeGet() == TYP_DOUBLE);
        tree = rsUsedTree[reg];
    }
#endif

    rsSpillTree(reg, tree);

    /* The register no longer holds its original value */

    rsUsedTree[reg] = NULL;
}

/*****************************************************************************
 *
 *  Spill all registers in 'regMask' that are currently marked as used.
 */

void RegSet::rsSpillRegs(regMaskTP regMask)
{
    /* The registers we're spilling must not be locked,
       or enregistered variables */

    assert((regMask & rsMaskLock) == 0);
    assert((regMask & rsMaskVars) == 0);

    /* Only spill what's currently marked as used */

    regMask &= rsMaskUsed;
    assert(regMask);

    regNumber regNum;
    regMaskTP regBit;

    for (regNum = REG_FIRST, regBit = 1; regNum < REG_COUNT; regNum = REG_NEXT(regNum), regBit <<= 1)
    {
        if (regMask & regBit)
        {
            rsSpillReg(regNum);

            regMask &= rsMaskUsed;

            if (!regMask)
                break;
        }
    }
}

/*****************************************************************************
 *
 *  The following table determines the order in which registers are considered
 *  for internal tree temps to live in
 */

extern const regNumber raRegTmpOrder[] = {REG_TMP_ORDER};
extern const regNumber rpRegTmpOrder[] = {REG_PREDICT_ORDER};
#if FEATURE_FP_REGALLOC
extern const regNumber raRegFltTmpOrder[] = {REG_FLT_TMP_ORDER};
#endif

/*****************************************************************************
 *
 *  Choose a register from the given set in the preferred order (see above);
 *  if no registers are in the set return REG_STK.
 */

regNumber RegSet::rsPickRegInTmpOrder(regMaskTP regMask)
{
    if (regMask == RBM_NONE)
        return REG_STK;

    bool      firstPass = true;
    regMaskTP avoidMask =
        ~rsGetModifiedRegsMask() & RBM_CALLEE_SAVED; // We want to avoid using any new callee saved register

    while (true)
    {
        /* Iterate the registers in the order specified by raRegTmpOrder */

        for (unsigned index = 0; index < REG_TMP_ORDER_COUNT; index++)
        {
            regNumber candidateReg  = raRegTmpOrder[index];
            regMaskTP candidateMask = genRegMask(candidateReg);

            // For a FP base frame, don't use FP register.
            if (m_rsCompiler->codeGen->isFramePointerUsed() && (candidateMask == RBM_FPBASE))
                continue;

            // For the first pass avoid selecting a never used register when there are other registers available
            if (firstPass && ((candidateMask & avoidMask) != 0))
                continue;

            if (regMask & candidateMask)
                return candidateReg;
        }

        if (firstPass == true)
            firstPass = false; // OK, now we are willing to select a never used register
        else
            break;
    }

    return REG_STK;
}

/*****************************************************************************
 *  Choose a register from the 'regMask' set and return it. If no registers in
 *  the set are currently free, one of them will be spilled (even if other
 *  registers - not in the set - are currently free).
 *
 *  If you don't require a register from a particular set, you should use rsPickReg() instead.
 *
 *  rsModifiedRegsMask is modified to include the returned register.
 */

regNumber RegSet::rsGrabReg(regMaskTP regMask)
{
    regMaskTP OKmask;
    regNumber regNum;
    regMaskTP regBit;

    assert(regMask);
    regMask &= ~rsMaskLock;
    assert(regMask);

    /* See if one of the desired registers happens to be free */

    OKmask = regMask & rsRegMaskFree();

    regNum = rsPickRegInTmpOrder(OKmask);
    if (REG_STK != regNum)
    {
        goto RET;
    }

    /* We'll have to spill one of the registers in 'regMask' */

    OKmask = regMask & rsRegMaskCanGrab();
    assert(OKmask);

    for (regNum = REG_FIRST, regBit = 1; (regBit & OKmask) == 0; regNum = REG_NEXT(regNum), regBit <<= 1)
    {
        if (regNum >= REG_COUNT)
        {
            assert(!"no register to grab!");
            NO_WAY("Could not grab a register, Predictor should have prevented this!");
        }
    }

    /* This will be the victim -- spill it */
    rsSpillReg(regNum);

    /* Make sure we did find a register to spill */
    assert(genIsValidReg(regNum));

RET:
    /* Keep track of which registers we ever touch */
    rsSetRegsModified(genRegMask(regNum));
    return regNum;
}

/*****************************************************************************
 *  Find a register to use and return it, spilling if necessary.
 *
 *  Look for a register in the following order: First, try and find a free register
 *  in 'regBest' (if 'regBest' is RBM_NONE, skip this step). Second, try to find a
 *  free register in 'regMask' (if 'regMask' is RBM_NONE, skip this step). Note that
 *  'regBest' doesn't need to be a subset of 'regMask'. Third, find any free
 *  register. Fourth, spill a register. The register to spill will be in 'regMask',
 *  if 'regMask' is not RBM_NONE.
 *
 *  Note that 'regMask' and 'regBest' are purely recommendations, and can be ignored;
 *  the caller can't expect that the returned register will be in those sets. In
 *  particular, under register stress, we specifically will pick registers not in
 *  these sets to ensure that callers don't require a register from those sets
 *  (and to ensure callers can handle the spilling that might ensue).
 *
 *  Calling rsPickReg() with the default arguments (which sets 'regMask' and 'regBest' to RBM_NONE)
 *  is equivalent to calling rsGrabReg(rsRegMaskFree()).
 *
 *  rsModifiedRegsMask is modified to include the returned register.
 */

regNumber RegSet::rsPickReg(regMaskTP regMask, regMaskTP regBest)
{
    regNumber regNum;
    regMaskTP spillMask;
    regMaskTP canGrabMask;

#ifdef DEBUG
    if (rsStressRegs() >= 1)
    {
        /* 'regMask' is purely a recommendation, and callers should be
           able to handle the case where it is not satisfied.
           The logic here tries to return ~regMask to check that all callers
           are prepared to handle such a case */

        regMaskTP badRegs = rsMaskMult & rsRegMaskCanGrab();

        badRegs = rsUseIfZero(badRegs, rsMaskUsed & rsRegMaskCanGrab());
        badRegs = rsUseIfZero(badRegs, rsRegMaskCanGrab());
        badRegs = rsExcludeHint(badRegs, regMask);

        assert(badRegs != RBM_NONE);

        return rsGrabReg(badRegs);
    }

#endif

    regMaskTP freeMask = rsRegMaskFree();

AGAIN:

    /* By default we'd prefer to accept all available registers */

    regMaskTP OKmask = freeMask;

    // OKmask = rsNarrowHint(OKmask, rsUselessRegs());

    /* Is there a 'best' register set? */

    if (regBest)
    {
        OKmask &= regBest;
        if (OKmask)
            goto TRY_REG;
        else
            goto TRY_ALL;
    }

    /* Was a register set recommended by the caller? */

    if (regMask)
    {
        OKmask &= regMask;
        if (!OKmask)
            goto TRY_ALL;
    }

TRY_REG:

    /* Iterate the registers in the order specified by raRegTmpOrder */

    regNum = rsPickRegInTmpOrder(OKmask);
    if (REG_STK != regNum)
    {
        goto RET;
    }

TRY_ALL:

    /* Were we considering 'regBest' ? */

    if (regBest)
    {
        /* 'regBest' is no good -- ignore it and try 'regMask' instead */

        regBest = RBM_NONE;
        goto AGAIN;
    }

    /* Now let's consider all available registers */

    /* Were we limited in our consideration? */

    if (!regMask)
    {
        /* We need to spill one of the free registers */

        spillMask = freeMask;
    }
    else
    {
        /* Did we not consider all free registers? */

        if ((regMask & freeMask) != freeMask)
        {
            /* The recommended regset didn't work, so try all available regs */

            regNum = rsPickRegInTmpOrder(freeMask);
            if (REG_STK != regNum)
                goto RET;
        }

        /* If we're going to spill, might as well go for the right one */

        spillMask = regMask;
    }

    /* Make sure we can spill some register. */

    canGrabMask = rsRegMaskCanGrab();
    if ((spillMask & canGrabMask) == 0)
        spillMask = canGrabMask;

    assert(spillMask);

    /* We have no choice but to spill one of the regs */

    return rsGrabReg(spillMask);

RET:

    rsSetRegsModified(genRegMask(regNum));
    return regNum;
}

#endif // LEGACY_BACKEND

/*****************************************************************************
 *
 *  Get the temp that was spilled from the given register (and free its
 *  spill descriptor while we're at it). Returns the temp (i.e. local var)
 */

TempDsc* RegSet::rsGetSpillTempWord(regNumber reg, SpillDsc* dsc, SpillDsc* prevDsc)
{
    assert((prevDsc == nullptr) || (prevDsc->spillNext == dsc));

#ifdef LEGACY_BACKEND
    /* Is dsc the last of a set of multi-used values */

    if (prevDsc && prevDsc->spillMoreMultis && !dsc->spillMoreMultis)
        prevDsc->spillMoreMultis = false;
#endif // LEGACY_BACKEND

    /* Remove this spill entry from the register's list */

    (prevDsc ? prevDsc->spillNext : rsSpillDesc[reg]) = dsc->spillNext;

    /* Remember which temp the value is in */

    TempDsc* temp = dsc->spillTemp;

    SpillDsc::freeDsc(this, dsc);

    /* return the temp variable */

    return temp;
}

#ifdef LEGACY_BACKEND
/*****************************************************************************
 *
 *  Reload the value that was spilled from the given register (and free its
 *  spill descriptor while we're at it). Returns the new register (which will
 *  be a member of 'needReg' if that value is non-zero).
 *
 *  'willKeepNewReg' indicates if the caller intends to mark newReg as used.
 *      If not, then we can't unspill the other multi-used descriptor (if any).
 *      Instead, we will just hold on to the temp and unspill them
 *      again as needed.
 */

regNumber RegSet::rsUnspillOneReg(GenTreePtr tree, regNumber oldReg, KeepReg willKeepNewReg, regMaskTP needReg)
{
    /* Was oldReg multi-used when it was spilled? */

    SpillDsc *prevDsc, *multiDsc;
    SpillDsc* spillDsc = rsGetSpillInfo(tree, oldReg, &prevDsc, &multiDsc);
    noway_assert((spillDsc != NULL) && (multiDsc != NULL));

    bool multiUsed = multiDsc->spillMoreMultis;

    /* We will use multiDsc to walk the rest of the spill list (if it's
       multiUsed). As we're going to remove spillDsc from the multiDsc
       list in the rsGetSpillTempWord() call we have to take care of the
       case where multiDsc==spillDsc. We will set multiDsc as spillDsc->spillNext */
    if (multiUsed && multiDsc == spillDsc)
    {
        assert(spillDsc->spillNext);
        multiDsc = spillDsc->spillNext;
    }

    /* Get the temp and free the spill-descriptor */

    TempDsc* temp = rsGetSpillTempWord(oldReg, spillDsc, prevDsc);

    //  Pick a new home for the value:
    //    This must be a register matching the 'needReg' mask, if it is non-zero.
    //    Additionally, if 'oldReg' is in 'needMask' and it is free we will select oldReg.
    //    Also note that the rsGrabReg() call below may cause the chosen register to be spilled.
    //
    regMaskTP prefMask;
    regMaskTP freeMask;
    regNumber newReg;
    var_types regType;
    var_types loadType;

    bool floatUnspill = false;

#if FEATURE_FP_REGALLOC
    floatUnspill = genIsValidFloatReg(oldReg);
#endif

    if (floatUnspill)
    {
        if (temp->tdTempType() == TYP_DOUBLE)
            regType = TYP_DOUBLE;
        else
            regType = TYP_FLOAT;
        loadType    = regType;
        prefMask    = genRegMaskFloat(oldReg, regType);
        freeMask    = RegFreeFloat();
    }
    else
    {
        regType  = TYP_I_IMPL;
        loadType = temp->tdTempType();
        prefMask = genRegMask(oldReg);
        freeMask = rsRegMaskFree();
    }

    if ((((prefMask & needReg) != 0) || (needReg == 0)) && ((prefMask & freeMask) != 0))
    {
        needReg = prefMask;
    }

    if (floatUnspill)
    {
        RegisterPreference pref(RBM_ALLFLOAT, needReg);
        newReg = PickRegFloat(regType, &pref, true);
    }
    else
    {
        newReg = rsGrabReg(rsUseIfZero(needReg, RBM_ALLINT));
    }

    m_rsCompiler->codeGen->trashReg(newReg);

    /* Reload the value from the saved location into the new register */

    m_rsCompiler->codeGen->reloadReg(loadType, temp, newReg);

    if (multiUsed && (willKeepNewReg == KEEP_REG))
    {
        /* We will unspill all the other multi-use trees if the register
           is going to be marked as used. If it is not going to be marked
           as used, we will have a problem if the new register gets spilled
           again.
         */

        /* We don't do the extra unspilling for complex address modes,
           since someone up the call chain may have a different idea about
           what registers are used to form the complex address mode (the
           addrReg return value from genMakeAddressable).

           Also, it is not safe to unspill all the multi-uses with a TYP_LONG.

           Finally, it is not safe to unspill into a different register, because
           the caller of genMakeAddressable caches the addrReg return value
           (register mask), but when unspilling into a different register it's
           not possible to inform the caller that addrReg is now different.
           See bug #89946 for an example of this.  There is an assert for this
           in rsMarkRegFree via genDoneAddressable.
         */

        for (SpillDsc* dsc = multiDsc; /**/; dsc = dsc->spillNext)
        {
            if ((oldReg != newReg) || (dsc->spillAddr != NULL) || (dsc->spillTree->gtType == TYP_LONG))
            {
                return newReg;
            }

            if (!dsc->spillMoreMultis)
            {
                /* All the remaining multi-uses are fine. We will now
                   unspill them all */
                break;
            }
        }

        bool       bFound = false;
        SpillDsc*  pDsc;
        SpillDsc** ppPrev;

        for (pDsc = rsSpillDesc[oldReg], ppPrev = &rsSpillDesc[oldReg];; pDsc = pDsc->spillNext)
        {
            if (pDsc == multiDsc)
            {
                // We've found the sequence we were searching for
                bFound = true;
            }

            if (bFound)
            {
                rsAddrUnspillOper(pDsc->spillAddr);

                // Mark the tree node as having been unspilled into newReg
                rsMarkUnspill(pDsc->spillTree, newReg);
            }

            if (!pDsc->spillMoreMultis)
            {
                if (bFound)
                {
                    // End of sequence

                    // We link remaining sides of list
                    *ppPrev = pDsc->spillNext;

                    // Exit walk
                    break;
                }
                else
                {
                    ppPrev = &(pDsc->spillNext);
                }
            }
        }

        /* pDsc points to the last multi-used descriptor from the spill-list
           for the current value (pDsc->spillMoreMultis == false) */

        pDsc->spillNext     = rsMultiDesc[newReg];
        rsMultiDesc[newReg] = multiDsc;

        if (floatUnspill)
            rsMaskMult |= genRegMaskFloat(newReg, regType);
        else
            rsMaskMult |= genRegMask(newReg);
    }

    /* Free the temp, it's no longer used */

    m_rsCompiler->tmpRlsTemp(temp);

    return newReg;
}
#endif // LEGACY_BACKEND

//---------------------------------------------------------------------
//  rsUnspillInPlace: The given tree operand has been spilled; just mark
//  it as unspilled so that we can use it as "normal" local.
//
//  Arguments:
//     tree    -  GenTree that needs to be marked as unspilled.
//     oldReg  -  reg of tree that was spilled.
//
//  Return Value:
//     None.
//
//  Assumptions:
//  1. It is the responsibility of the caller to free the spill temp.
//  2. RyuJIT backend specific: In case of multi-reg call node
//     GTF_SPILLED flag associated with reg is cleared.  It is the
//     responsibility of caller to clear GTF_SPILLED flag on call node
//     itself after ensuring there are no outstanding regs in GTF_SPILLED
//     state.
//
TempDsc* RegSet::rsUnspillInPlace(GenTreePtr tree, regNumber oldReg, unsigned regIdx /* =0 */)
{
    assert(!isRegPairType(tree->gtType));

    // Get the tree's SpillDsc
    SpillDsc* prevDsc;
    SpillDsc* spillDsc = rsGetSpillInfo(tree, oldReg, &prevDsc);
    PREFIX_ASSUME(spillDsc != nullptr);

    // Get the temp
    TempDsc* temp = rsGetSpillTempWord(oldReg, spillDsc, prevDsc);

    // The value is now unspilled
    if (tree->IsMultiRegCall())
    {
        GenTreeCall* call  = tree->AsCall();
        unsigned     flags = call->GetRegSpillFlagByIdx(regIdx);
        flags &= ~GTF_SPILLED;
        call->SetRegSpillFlagByIdx(flags, regIdx);
    }
    else
    {
        tree->gtFlags &= ~GTF_SPILLED;
    }

#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("\t\t\t\t\t\t\tTree-Node marked unspilled from  ");
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif

    return temp;
}

#ifdef LEGACY_BACKEND

/*****************************************************************************
 *
 *  The given tree operand has been spilled; reload it into a register that
 *  is in 'needReg' (if 'needReg' is RBM_NONE, any register will do). If 'keepReg'
 *  is set to KEEP_REG, we'll mark the new register as used.
 */

void RegSet::rsUnspillReg(GenTreePtr tree, regMaskTP needReg, KeepReg keepReg)
{
    assert(!isRegPairType(tree->gtType)); // use rsUnspillRegPair()
    regNumber oldReg = tree->gtRegNum;

    /* Get the SpillDsc for the tree */

    SpillDsc* spillDsc = rsGetSpillInfo(tree, oldReg);
    PREFIX_ASSUME(spillDsc != NULL);

    /* Before spillDsc is stomped on by rsUnspillOneReg(), note whether
     * the reg was part of an address mode
     */

    GenTreePtr unspillAddr = spillDsc->spillAddr;

    /* Pick a new home for the value */

    regNumber newReg = rsUnspillOneReg(tree, oldReg, keepReg, needReg);

    /* Mark the tree node as having been unspilled into newReg */

    rsMarkUnspill(tree, newReg);

    // If this reg was part of a complex address mode, need to clear this flag which
    // tells address mode building that a component has been spilled

    rsAddrUnspillOper(unspillAddr);

#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("\t\t\t\t\t\t\tThe register %s unspilled from  ", m_rsCompiler->compRegVarName(newReg));
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif

    /* Mark the new value as used, if the caller desires so */

    if (keepReg == KEEP_REG)
        rsMarkRegUsed(tree, unspillAddr);
}
#endif // LEGACY_BACKEND

void RegSet::rsMarkSpill(GenTreePtr tree, regNumber reg)
{
    tree->gtFlags &= ~GTF_REG_VAL;
    tree->gtFlags |= GTF_SPILLED;
}

#ifdef LEGACY_BACKEND

void RegSet::rsMarkUnspill(GenTreePtr tree, regNumber reg)
{
#ifndef _TARGET_AMD64_
    assert(tree->gtType != TYP_LONG);
#endif // _TARGET_AMD64_

    tree->gtFlags |= GTF_REG_VAL;
    tree->gtFlags &= ~GTF_SPILLED;
    tree->gtRegNum = reg;
}

/*****************************************************************************
 *
 *  Choose a register pair from the given set (note: only registers in the
 *  given set will be considered).
 */

regPairNo RegSet::rsGrabRegPair(regMaskTP regMask)
{
    regPairNo regPair;
    regMaskTP OKmask;
    regNumber reg1;
    regNumber reg2;

    assert(regMask);
    regMask &= ~rsMaskLock;
    assert(regMask);

    /* We'd prefer to choose a free register pair if possible */

    OKmask = regMask & rsRegMaskFree();

    /* Any takers in the recommended/free set? */

    regPair = rsFindRegPairNo(OKmask);

    if (regPair != REG_PAIR_NONE)
    {
        // The normal early exit

        /* Keep track of which registers we ever touch */
        rsSetRegsModified(genRegPairMask(regPair));

        return regPair;
    }

    /* We have no choice but to spill one or two used regs */

    if (OKmask)
    {
        /* One (and only one) register is free and acceptable - grab it */

        assert(genMaxOneBit(OKmask));

        for (reg1 = REG_INT_FIRST; reg1 <= REG_INT_LAST; reg1 = REG_NEXT(reg1))
        {
            if (OKmask & genRegMask(reg1))
                break;
        }
        assert(OKmask & genRegMask(reg1));
    }
    else
    {
        /* No register is free and acceptable - we'll have to spill two */

        reg1 = rsGrabReg(regMask);
    }

    /* Temporarily lock the first register so it doesn't go away */

    rsLockReg(genRegMask(reg1));

    /* Now grab another register */

    reg2 = rsGrabReg(regMask);

    /* We can unlock the first register now */

    rsUnlockReg(genRegMask(reg1));

    /* Convert the two register numbers into a pair */

    if (reg1 < reg2)
        regPair = gen2regs2pair(reg1, reg2);
    else
        regPair = gen2regs2pair(reg2, reg1);

    return regPair;
}

/*****************************************************************************
 *
 *  Choose a register pair from the given set (if non-zero) or from the set of
 *  currently available registers (if 'regMask' is zero).
 */

regPairNo RegSet::rsPickRegPair(regMaskTP regMask)
{
    regMaskTP OKmask;
    regPairNo regPair;

    int repeat = 0;

    /* By default we'd prefer to accept all available registers */

    OKmask = rsRegMaskFree();

    if (regMask)
    {
        /* A register set was recommended by the caller */

        OKmask &= regMask;
    }

AGAIN:

    regPair = rsFindRegPairNo(OKmask);

    if (regPair != REG_PAIR_NONE)
    {
        return regPair; // Normal early exit
    }

    regMaskTP freeMask;
    regMaskTP spillMask;

    /* Now let's consider all available registers */

    freeMask = rsRegMaskFree();

    /* Were we limited in our consideration? */

    if (!regMask)
    {
        /* We need to spill two of the free registers */

        spillMask = freeMask;
    }
    else
    {
        /* Did we not consider all free registers? */

        if ((regMask & freeMask) != freeMask && repeat == 0)
        {
            /* The recommended regset didn't work, so try all available regs */

            OKmask = freeMask;
            repeat++;
            goto AGAIN;
        }

        /* If we're going to spill, might as well go for the right one */

        spillMask = regMask;
    }

    /* Make sure that we have at least two bits set */

    if (genMaxOneBit(spillMask & rsRegMaskCanGrab()))
        spillMask = rsRegMaskCanGrab();

    assert(!genMaxOneBit(spillMask));

    /* We have no choice but to spill 1/2 of the regs */

    return rsGrabRegPair(spillMask);
}

/*****************************************************************************
 *
 *  The given tree operand has been spilled; reload it into a register pair
 *  that is in 'needReg' (if 'needReg' is RBM_NONE, any register pair will do). If
 *  'keepReg' is KEEP_REG, we'll mark the new register pair as used. It is
 *  assumed that the current register pair has been marked as used (modulo
 *  any spillage, of course).
 */

void RegSet::rsUnspillRegPair(GenTreePtr tree, regMaskTP needReg, KeepReg keepReg)
{
    assert(isRegPairType(tree->gtType));

    regPairNo regPair = tree->gtRegPair;
    regNumber regLo   = genRegPairLo(regPair);
    regNumber regHi   = genRegPairHi(regPair);

    /* Has the register holding the lower half been spilled? */

    if (!rsIsTreeInReg(regLo, tree))
    {
        /* Is the upper half already in the right place? */

        if (rsIsTreeInReg(regHi, tree))
        {
            /* Temporarily lock the high part */

            rsLockUsedReg(genRegMask(regHi));

            /* Pick a new home for the lower half */

            regLo = rsUnspillOneReg(tree, regLo, keepReg, needReg);

            /* We can unlock the high part now */

            rsUnlockUsedReg(genRegMask(regHi));
        }
        else
        {
            /* Pick a new home for the lower half */

            regLo = rsUnspillOneReg(tree, regLo, keepReg, needReg);
        }
    }
    else
    {
        /* Free the register holding the lower half */

        rsMarkRegFree(genRegMask(regLo));
    }

    if (regHi != REG_STK)
    {
        /* Has the register holding the upper half been spilled? */

        if (!rsIsTreeInReg(regHi, tree))
        {
            regMaskTP regLoUsed;

            /* Temporarily lock the low part so it doesnt get spilled */

            rsLockReg(genRegMask(regLo), &regLoUsed);

            /* Pick a new home for the upper half */

            regHi = rsUnspillOneReg(tree, regHi, keepReg, needReg);

            /* We can unlock the low register now */

            rsUnlockReg(genRegMask(regLo), regLoUsed);
        }
        else
        {
            /* Free the register holding the upper half */

            rsMarkRegFree(genRegMask(regHi));
        }
    }

    /* The value is now residing in the new register */

    tree->gtFlags |= GTF_REG_VAL;
    tree->gtFlags &= ~GTF_SPILLED;
    tree->gtRegPair = gen2regs2pair(regLo, regHi);

    /* Mark the new value as used, if the caller desires so */

    if (keepReg == KEEP_REG)
        rsMarkRegPairUsed(tree);
}

/*****************************************************************************
 *
 *  The given register is being used by multiple trees (all of which represent
 *  the same logical value). Happens mainly because of REDUNDANT_LOAD;
 *  We don't want to really spill the register as it actually holds the
 *  value we want. But the multiple trees may be part of different
 *  addressing modes.
 *  Save the previous 'use' info so that when we return the register will
 *  appear unused.
 */

void RegSet::rsRecMultiReg(regNumber reg, var_types type)
{
    SpillDsc* spill;
    regMaskTP regMask;

    if (genIsValidFloatReg(reg) && isFloatRegType(type))
        regMask = genRegMaskFloat(reg, type);
    else
        regMask = genRegMask(reg);

#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("\t\t\t\t\t\t\tRegister %s multi-use inc for   ", m_rsCompiler->compRegVarName(reg));
        Compiler::printTreeID(rsUsedTree[reg]);
        printf(" multMask=" REG_MASK_ALL_FMT "\n", rsMaskMult | regMask);
    }
#endif

    /* The register is supposed to be already used */

    assert(regMask & rsMaskUsed);

    assert(rsUsedTree[reg]);

    /* Allocate/reuse a spill descriptor */

    spill = SpillDsc::alloc(m_rsCompiler, this, rsUsedTree[reg]->TypeGet());

    /* Record the current 'use' info in the spill descriptor */

    spill->spillTree = rsUsedTree[reg];
    rsUsedTree[reg]  = 0;
    spill->spillAddr = rsUsedAddr[reg];
    rsUsedAddr[reg]  = 0;

    /* Remember whether the register is already 'multi-use' */

    spill->spillMoreMultis = ((rsMaskMult & regMask) != 0);

    /* Insert the new multi-use record in the list for the register */

    spill->spillNext = rsMultiDesc[reg];
    rsMultiDesc[reg] = spill;

    /* This register is now 'multi-use' */

    rsMaskMult |= regMask;
}

/*****************************************************************************
 *
 *  Free the given register, which is known to have multiple uses.
 */

var_types RegSet::rsRmvMultiReg(regNumber reg)
{
    SpillDsc* dsc;

    assert(rsMaskMult & genRegMask(reg));

#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("\t\t\t\t\t\t\tRegister %s multi-use dec for   ", m_rsCompiler->compRegVarName(reg));
        Compiler::printTreeID(rsUsedTree[reg]);
        printf(" multMask=" REG_MASK_ALL_FMT "\n", rsMaskMult);
    }
#endif

    /* Get hold of the spill descriptor for the register */

    dsc = rsMultiDesc[reg];
    assert(dsc);
    rsMultiDesc[reg] = dsc->spillNext;

    /* Copy the previous 'use' info from the descriptor */

    assert(reg != REG_SPBASE);
    rsUsedTree[reg] = dsc->spillTree;
    rsUsedAddr[reg] = dsc->spillAddr;

    if (!(dsc->spillTree->gtFlags & GTF_SPILLED))
        m_rsGCInfo.gcMarkRegPtrVal(reg, dsc->spillTree->TypeGet());

    var_types type = dsc->spillTree->TypeGet();
    regMaskTP regMask;

    if (genIsValidFloatReg(reg) && isFloatRegType(type))
        regMask = genRegMaskFloat(reg, type);
    else
        regMask = genRegMask(reg);

    /* Is only one use of the register left? */

    if (!dsc->spillMoreMultis)
    {
        rsMaskMult -= regMask;
    }

#ifdef DEBUG
    if (m_rsCompiler->verbose)
    {
        printf("\t\t\t\t\t\t\tRegister %s multi-use dec - now ", m_rsCompiler->compRegVarName(reg));
        Compiler::printTreeID(rsUsedTree[reg]);
        printf(" multMask=" REG_MASK_ALL_FMT "\n", rsMaskMult);
    }
#endif

    SpillDsc::freeDsc(this, dsc);
    return type;
}
#endif // LEGACY_BACKEND

/*****************************************************************************/
#if REDUNDANT_LOAD
/*****************************************************************************
 *
 *  Search for a register which contains the given constant value.
 *  Return success/failure and set the register if success.
 *  If the closeDelta argument is non-NULL then look for a
 *  register that has a close constant value. For ARM, find
 *  the closest register value, independent of constant delta.
 *  For non-ARM, only consider values that are within -128..+127.
 *  If one is found, *closeDelta is set to the difference that needs
 *  to be added to the register returned. On x86/amd64, an lea instruction
 *  is used to set the target register using the register that
 *  contains the close integer constant.
 */

regNumber RegTracker::rsIconIsInReg(ssize_t val, ssize_t* closeDelta /* = NULL */)
{
    regNumber closeReg = REG_NA;

    if (compiler->opts.MinOpts() || compiler->opts.compDbgCode)
    {
        return REG_NA;
    }

    for (regNumber reg = REG_INT_FIRST; reg <= REG_INT_LAST; reg = REG_NEXT(reg))
    {
        if (rsRegValues[reg].rvdKind == RV_INT_CNS)
        {
            ssize_t regCnsVal = rsRegValues[reg].rvdIntCnsVal;
            if (regCnsVal == val)
            {
                if (closeDelta)
                {
                    *closeDelta = 0;
                }
                return reg;
            }
            if (closeDelta)
            {
#ifdef _TARGET_ARM_
                // Find the smallest delta; the caller checks the size
                // TODO-CQ: find the smallest delta from a low register?
                //       That is, is it better to return a high register with a
                //       small constant delta, or a low register with
                //       a larger offset? It's better to have a low register with an offset within the low register
                //       range, or a high register otherwise...

                ssize_t regCnsDelta = val - regCnsVal;
                if ((closeReg == REG_NA) || (unsigned_abs(regCnsDelta) < unsigned_abs(*closeDelta)))
                {
                    closeReg    = reg;
                    *closeDelta = regCnsDelta;
                }
#else
                if (closeReg == REG_NA)
                {
                    ssize_t regCnsDelta = val - regCnsVal;
                    /* Does delta fit inside a byte [-128..127] */
                    if (regCnsDelta == (signed char)regCnsDelta)
                    {
                        closeReg    = reg;
                        *closeDelta = (int)regCnsDelta;
                    }
                }
#endif
            }
        }
    }

    /* There was not an exact match */

    return closeReg; /* will always be REG_NA when closeDelta is NULL */
}

/*****************************************************************************
 *
 *  Assume all non-integer registers contain garbage (this is called when
 *  we encounter a code label that isn't jumped by any block; we need to
 *  clear pointer values out of the table lest the GC pointer tables get
 *  out of date).
 */

void RegTracker::rsTrackRegClrPtr()
{
    for (regNumber reg = REG_FIRST; reg < REG_COUNT; reg = REG_NEXT(reg))
    {
        /* Preserve constant values */

        if (rsRegValues[reg].rvdKind == RV_INT_CNS)
        {
            /* Make sure we don't preserve NULL (it's a pointer) */

            if (rsRegValues[reg].rvdIntCnsVal != NULL)
            {
                continue;
            }
        }

        /* Preserve variables known to not be pointers */

        if (rsRegValues[reg].rvdKind == RV_LCL_VAR)
        {
            if (!varTypeIsGC(compiler->lvaTable[rsRegValues[reg].rvdLclVarNum].TypeGet()))
            {
                continue;
            }
        }

        rsRegValues[reg].rvdKind = RV_TRASH;
    }
}

/*****************************************************************************
 *
 *  This routine trashes the registers that hold stack GCRef/ByRef variables. (VSW: 561129)
 *  It should be called at each gc-safe point.
 *
 *  It returns a mask of the registers that used to contain tracked stack variables that
 *  were trashed.
 *
 */

regMaskTP RegTracker::rsTrashRegsForGCInterruptability()
{
    regMaskTP result = RBM_NONE;
    for (regNumber reg = REG_FIRST; reg < REG_COUNT; reg = REG_NEXT(reg))
    {
        if (rsRegValues[reg].rvdKind == RV_LCL_VAR)
        {
            LclVarDsc* varDsc = &compiler->lvaTable[rsRegValues[reg].rvdLclVarNum];

            if (!varTypeIsGC(varDsc->TypeGet()))
            {
                continue;
            }

            // Only stack locals got tracked.
            assert(!varDsc->lvRegister);

            rsRegValues[reg].rvdKind = RV_TRASH;

            result |= genRegMask(reg);
        }
    }

    return result;
}

/*****************************************************************************
 *
 *  Search for a register which contains the given local var.
 *  Return success/failure and set the register if success.
 *  Return FALSE on register variables, because otherwise their lifetimes
 *  can get bungled with respect to pointer tracking.
 */

regNumber RegTracker::rsLclIsInReg(unsigned var)
{
    assert(var < compiler->lvaCount);

    if (compiler->opts.MinOpts() || compiler->opts.compDbgCode)
    {
        return REG_NA;
    }

    /* return false if register var so genMarkLclVar can do its job */

    if (compiler->lvaTable[var].lvRegister)
    {
        return REG_NA;
    }

    for (regNumber reg = REG_FIRST; reg < REG_COUNT; reg = REG_NEXT(reg))
    {
        if (rsRegValues[reg].rvdLclVarNum == var && rsRegValues[reg].rvdKind == RV_LCL_VAR)
        {
            return reg;
        }
    }

    return REG_NA;
}

/*****************************************************************************/

regPairNo RegTracker::rsLclIsInRegPair(unsigned var)
{
    assert(var < compiler->lvaCount);

    if (compiler->opts.MinOpts() || compiler->opts.compDbgCode)
    {
        return REG_PAIR_NONE;
    }

    regValKind rvKind = RV_TRASH;
    regNumber  regNo  = DUMMY_INIT(REG_NA);

    for (regNumber reg = REG_FIRST; reg < REG_COUNT; reg = REG_NEXT(reg))
    {
        if (rvKind != rsRegValues[reg].rvdKind && rsTrackIsLclVarLng(rsRegValues[reg].rvdKind) &&
            rsRegValues[reg].rvdLclVarNum == var)
        {
            /* first occurrence of this variable ? */

            if (rvKind == RV_TRASH)
            {
                regNo  = reg;
                rvKind = rsRegValues[reg].rvdKind;
            }
            else if (rvKind == RV_LCL_VAR_LNG_HI)
            {
                /* We found the lower half of the long */

                return gen2regs2pair(reg, regNo);
            }
            else
            {
                /* We found the upper half of the long */

                assert(rvKind == RV_LCL_VAR_LNG_LO);
                return gen2regs2pair(regNo, reg);
            }
        }
    }

    return REG_PAIR_NONE;
}

/*****************************************************************************/

void RegTracker::rsTrashLclLong(unsigned var)
{
    if (compiler->opts.MinOpts() || compiler->opts.compDbgCode)
    {
        return;
    }

    for (regNumber reg = REG_FIRST; reg < REG_COUNT; reg = REG_NEXT(reg))
    {
        if (rsTrackIsLclVarLng(rsRegValues[reg].rvdKind) && rsRegValues[reg].rvdLclVarNum == var)
        {
            rsRegValues[reg].rvdKind = RV_TRASH;
        }
    }
}

/*****************************************************************************
 *
 *  Local's value has changed, mark all regs which contained it as trash.
 */

void RegTracker::rsTrashLcl(unsigned var)
{
    if (compiler->opts.MinOpts() || compiler->opts.compDbgCode)
    {
        return;
    }

    for (regNumber reg = REG_FIRST; reg < REG_COUNT; reg = REG_NEXT(reg))
    {
        if (rsRegValues[reg].rvdKind == RV_LCL_VAR && rsRegValues[reg].rvdLclVarNum == var)
        {
            rsRegValues[reg].rvdKind = RV_TRASH;
        }
    }
}

/*****************************************************************************
 *
 *  A little helper to trash the given set of registers.
 *  Usually used after a call has been generated.
 */

void RegTracker::rsTrashRegSet(regMaskTP regMask)
{
    if (compiler->opts.MinOpts() || compiler->opts.compDbgCode)
    {
        return;
    }
    regMaskTP regBit = 1;
    for (regNumber regNum = REG_FIRST; regMask != 0; regNum = REG_NEXT(regNum), regBit <<= 1)
    {
        if (regBit & regMask)
        {
            rsTrackRegTrash(regNum);
            regMask -= regBit;
        }
    }
}

/*****************************************************************************
 *
 *  Return a mask of registers that hold no useful value.
 */

regMaskTP RegTracker::rsUselessRegs()
{
    if (compiler->opts.MinOpts() || compiler->opts.compDbgCode)
    {
        return RBM_ALLINT;
    }

    regMaskTP mask = RBM_NONE;
    for (regNumber reg = REG_FIRST; reg < REG_COUNT; reg = REG_NEXT(reg))
    {
        if (rsRegValues[reg].rvdKind == RV_TRASH)
        {
            mask |= genRegMask(reg);
        }
    }

    return mask;
}

/*****************************************************************************/
#endif // REDUNDANT_LOAD
/*****************************************************************************/

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           TempsInfo                                       XX
XX                                                                           XX
XX  The temporary lclVars allocated by the compiler for code generation      XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

void Compiler::tmpInit()
{
#ifdef LEGACY_BACKEND
    tmpDoubleSpillMax = 0;
    tmpIntSpillMax    = 0;
#endif // LEGACY_BACKEND

    tmpCount = 0;
    tmpSize  = 0;
#ifdef DEBUG
    tmpGetCount = 0;
#endif

    memset(tmpFree, 0, sizeof(tmpFree));
    memset(tmpUsed, 0, sizeof(tmpUsed));
}

/* static */
var_types Compiler::tmpNormalizeType(var_types type)
{
#ifndef LEGACY_BACKEND

    type = genActualType(type);

#else  // LEGACY_BACKEND
    if (!varTypeIsGC(type))
    {
        switch (genTypeStSz(type))
        {
            case 1:
                type = TYP_INT; // Maps all 4-byte non-GC types to TYP_INT temps
                break;
            case 2:
                type = TYP_DOUBLE; // Maps all 8-byte types to TYP_DOUBLE temps
                break;
            default:
                assert(!"unexpected type");
        }
    }
#endif // LEGACY_BACKEND

    return type;
}

/*****************************************************************************
 *
 *  Allocate a temp of the given size (and type, if tracking pointers for
 *  the garbage collector).
 */

TempDsc* Compiler::tmpGetTemp(var_types type)
{
    type          = tmpNormalizeType(type);
    unsigned size = genTypeSize(type);

    // If TYP_STRUCT ever gets in here we do bad things (tmpSlot returns -1)
    noway_assert(size >= sizeof(int));

    /* Find the slot to search for a free temp of the right size */

    unsigned slot = tmpSlot(size);

    /* Look for a temp with a matching type */

    TempDsc** last = &tmpFree[slot];
    TempDsc*  temp;

    for (temp = *last; temp; last = &temp->tdNext, temp = *last)
    {
        /* Does the type match? */

        if (temp->tdTempType() == type)
        {
            /* We have a match -- remove it from the free list */

            *last = temp->tdNext;
            break;
        }
    }

#ifdef DEBUG
    /* Do we need to allocate a new temp */
    bool isNewTemp = false;
#endif // DEBUG

#ifndef LEGACY_BACKEND

    noway_assert(temp != nullptr);

#else // LEGACY_BACKEND

    if (temp == nullptr)
    {
#ifdef DEBUG
        isNewTemp = true;
#endif // DEBUG
        tmpCount++;
        tmpSize += (unsigned)size;

#ifdef _TARGET_ARM_
        if (type == TYP_DOUBLE)
        {
            // Adjust tmpSize in case it needs alignment
            tmpSize += TARGET_POINTER_SIZE;
        }
#endif // _TARGET_ARM_

        genEmitter->emitTmpSizeChanged(tmpSize);

        temp = new (this, CMK_Unknown) TempDsc(-((int)tmpCount), size, type);
    }

#endif // LEGACY_BACKEND

#ifdef DEBUG
    if (verbose)
    {
        printf("%s temp #%u, slot %u, size = %u\n", isNewTemp ? "created" : "reused", -temp->tdTempNum(), slot,
               temp->tdTempSize());
    }
    tmpGetCount++;
#endif // DEBUG

    temp->tdNext  = tmpUsed[slot];
    tmpUsed[slot] = temp;

    return temp;
}

#ifndef LEGACY_BACKEND

/*****************************************************************************
 * Preallocate 'count' temps of type 'type'. This type must be a normalized
 * type (by the definition of tmpNormalizeType()).
 *
 * This is used at the end of LSRA, which knows precisely the maximum concurrent
 * number of each type of spill temp needed, before code generation. Code generation
 * then uses these preallocated temp. If code generation ever asks for more than
 * has been preallocated, it is a fatal error.
 */

void Compiler::tmpPreAllocateTemps(var_types type, unsigned count)
{
    assert(type == tmpNormalizeType(type));
    unsigned size = genTypeSize(type);

    // If TYP_STRUCT ever gets in here we do bad things (tmpSlot returns -1)
    noway_assert(size >= sizeof(int));

    // Find the slot to search for a free temp of the right size.
    // Note that slots are shared by types of the identical size (e.g., TYP_REF and TYP_LONG on AMD64),
    // so we can't assert that the slot is empty when we get here.

    unsigned slot = tmpSlot(size);

    for (unsigned i = 0; i < count; i++)
    {
        tmpCount++;
        tmpSize += size;

        TempDsc* temp = new (this, CMK_Unknown) TempDsc(-((int)tmpCount), size, type);

#ifdef DEBUG
        if (verbose)
        {
            printf("pre-allocated temp #%u, slot %u, size = %u\n", -temp->tdTempNum(), slot, temp->tdTempSize());
        }
#endif // DEBUG

        // Add it to the front of the appropriate slot list.
        temp->tdNext  = tmpFree[slot];
        tmpFree[slot] = temp;
    }
}

#endif // !LEGACY_BACKEND

/*****************************************************************************
 *
 *  Release the given temp.
 */

void Compiler::tmpRlsTemp(TempDsc* temp)
{
    assert(temp != nullptr);

    unsigned slot;

    /* Add the temp to the 'free' list */

    slot = tmpSlot(temp->tdTempSize());

#ifdef DEBUG
    if (verbose)
    {
        printf("release temp #%u, slot %u, size = %u\n", -temp->tdTempNum(), slot, temp->tdTempSize());
    }
    assert(tmpGetCount);
    tmpGetCount--;
#endif

    // Remove it from the 'used' list.

    TempDsc** last = &tmpUsed[slot];
    TempDsc*  t;
    for (t = *last; t != nullptr; last = &t->tdNext, t = *last)
    {
        if (t == temp)
        {
            /* Found it! -- remove it from the 'used' list */

            *last = t->tdNext;
            break;
        }
    }
    assert(t != nullptr); // We better have found it!

    // Add it to the free list.

    temp->tdNext  = tmpFree[slot];
    tmpFree[slot] = temp;
}

/*****************************************************************************
 *  Given a temp number, find the corresponding temp.
 *
 *  When looking for temps on the "free" list, this can only be used after code generation. (This is
 *  simply because we have an assert to that effect in tmpListBeg(); we could relax that, or hoist
 *  the assert to the appropriate callers.)
 *
 *  When looking for temps on the "used" list, this can be used any time.
 */
TempDsc* Compiler::tmpFindNum(int tnum, TEMP_USAGE_TYPE usageType /* = TEMP_USAGE_FREE */) const
{
    assert(tnum < 0); // temp numbers are negative

    for (TempDsc* temp = tmpListBeg(usageType); temp != nullptr; temp = tmpListNxt(temp, usageType))
    {
        if (temp->tdTempNum() == tnum)
        {
            return temp;
        }
    }

    return nullptr;
}

/*****************************************************************************
 *
 *  A helper function is used to iterate over all the temps.
 */

TempDsc* Compiler::tmpListBeg(TEMP_USAGE_TYPE usageType /* = TEMP_USAGE_FREE */) const
{
    TempDsc* const* tmpLists;
    if (usageType == TEMP_USAGE_FREE)
    {
        tmpLists = tmpFree;
    }
    else
    {
        tmpLists = tmpUsed;
    }

    // Return the first temp in the slot for the smallest size
    unsigned slot = 0;
    while (slot < (TEMP_SLOT_COUNT - 1) && tmpLists[slot] == nullptr)
    {
        slot++;
    }
    TempDsc* temp = tmpLists[slot];

    return temp;
}

/*****************************************************************************
 * Used with tmpListBeg() to iterate over the list of temps.
 */

TempDsc* Compiler::tmpListNxt(TempDsc* curTemp, TEMP_USAGE_TYPE usageType /* = TEMP_USAGE_FREE */) const
{
    assert(curTemp != nullptr);

    TempDsc* temp = curTemp->tdNext;
    if (temp == nullptr)
    {
        unsigned size = curTemp->tdTempSize();

        // If there are no more temps in the list, check if there are more
        // slots (for bigger sized temps) to walk.

        TempDsc* const* tmpLists;
        if (usageType == TEMP_USAGE_FREE)
        {
            tmpLists = tmpFree;
        }
        else
        {
            tmpLists = tmpUsed;
        }

        while (size < TEMP_MAX_SIZE && temp == nullptr)
        {
            size += sizeof(int);
            unsigned slot = tmpSlot(size);
            temp          = tmpLists[slot];
        }

        assert((temp == nullptr) || (temp->tdTempSize() == size));
    }

    return temp;
}

#ifdef DEBUG
/*****************************************************************************
 * Return 'true' if all allocated temps are free (not in use).
 */
bool Compiler::tmpAllFree() const
{
    // The 'tmpGetCount' should equal the number of things in the 'tmpUsed' lists. This is a convenient place
    // to assert that.
    unsigned usedCount = 0;
    for (TempDsc* temp = tmpListBeg(TEMP_USAGE_USED); temp != nullptr; temp = tmpListNxt(temp, TEMP_USAGE_USED))
    {
        ++usedCount;
    }
    assert(usedCount == tmpGetCount);

    if (tmpGetCount != 0)
    {
        return false;
    }

    for (unsigned i = 0; i < sizeof(tmpUsed) / sizeof(tmpUsed[0]); i++)
    {
        if (tmpUsed[i] != nullptr)
        {
            return false;
        }
    }

    return true;
}

#endif // DEBUG

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX  Register-related utility functions                                       XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************
 *
 *  Returns whether regPair is a combination of two x86 registers or
 *  contains a pseudo register.
 *  In debug it also asserts that reg1 and reg2 are not the same.
 */

bool genIsProperRegPair(regPairNo regPair)
{
    regNumber rlo = genRegPairLo(regPair);
    regNumber rhi = genRegPairHi(regPair);

    assert(regPair >= REG_PAIR_FIRST && regPair <= REG_PAIR_LAST);

    if (rlo == rhi)
    {
        return false;
    }

    if (rlo == REG_L_STK || rhi == REG_L_STK)
    {
        return false;
    }

    if (rlo >= REG_COUNT || rhi >= REG_COUNT)
    {
        return false;
    }

    return (rlo != REG_STK && rhi != REG_STK);
}

/*****************************************************************************
 *
 *  Given a register that is an argument register
 *   returns the next argument register
 *
 *  Note: that this method will return a non arg register
 *   when given REG_ARG_LAST
 *
 */

regNumber genRegArgNext(regNumber argReg)
{
    regNumber result = REG_NA;

    if (isValidFloatArgReg(argReg))
    {
        // We can iterate the floating point argument registers by using +1
        result = REG_NEXT(argReg);
    }
    else
    {
        assert(isValidIntArgReg(argReg));

#ifdef _TARGET_AMD64_
#ifdef UNIX_AMD64_ABI
        // Windows X64 ABI:
        //     REG_EDI, REG_ESI, REG_ECX, REG_EDX, REG_R8, REG_R9
        //
        if (argReg == REG_ARG_1) // REG_ESI
        {
            result = REG_ARG_2; // REG_ECX
        }
        else if (argReg == REG_ARG_3) // REG_EDX
        {
            result = REG_ARG_4; // REG_R8
        }
#else  // Windows ABI
        // Windows X64 ABI:
        //     REG_ECX, REG_EDX, REG_R8, REG_R9
        //
        if (argReg == REG_ARG_1) // REG_EDX
        {
            result = REG_ARG_2; // REG_R8
        }
#endif // UNIX or Windows ABI
#endif // _TARGET_AMD64_

        // If we didn't set 'result' to valid register above
        // then we will just iterate 'argReg' using REG_NEXT
        //
        if (result == REG_NA)
        {
            // Otherwise we just iterate the argument registers by using REG_NEXT
            result = REG_NEXT(argReg);
        }
    }

    return result;
}

/*****************************************************************************
 *
 *  The following table determines the order in which callee-saved registers
 *  are encoded in GC information at call sites (perhaps among other things).
 *  In any case, they establish a mapping from ordinal callee-save reg "indices" to
 *  register numbers and corresponding bitmaps.
 */

const regNumber raRegCalleeSaveOrder[] = {REG_CALLEE_SAVED_ORDER};
const regMaskTP raRbmCalleeSaveOrder[] = {RBM_CALLEE_SAVED_ORDER};

regMaskSmall genRegMaskFromCalleeSavedMask(unsigned short calleeSaveMask)
{
    regMaskSmall res = 0;
    for (int i = 0; i < CNT_CALLEE_SAVED; i++)
    {
        if ((calleeSaveMask & ((regMaskTP)1 << i)) != 0)
        {
            res |= raRbmCalleeSaveOrder[i];
        }
    }
    return res;
}

/*****************************************************************************
 *
 *  Initializes the spill code. Should be called once per function compiled.
 */

// inline
void RegSet::rsSpillInit()
{
    /* Clear out the spill and multi-use tables */

    memset(rsSpillDesc, 0, sizeof(rsSpillDesc));

#ifdef LEGACY_BACKEND
    memset(rsUsedTree, 0, sizeof(rsUsedTree));
    memset(rsUsedAddr, 0, sizeof(rsUsedAddr));
    memset(rsMultiDesc, 0, sizeof(rsMultiDesc));
    rsSpillFloat = nullptr;
#endif // LEGACY_BACKEND

    rsNeededSpillReg = false;

    /* We don't have any descriptors allocated */

    rsSpillFree = nullptr;
}

/*****************************************************************************
 *
 *  Shuts down the spill code. Should be called once per function compiled.
 */

// inline
void RegSet::rsSpillDone()
{
    rsSpillChk();
}

/*****************************************************************************
 *
 *  Begin tracking spills - should be called each time before a pass is made
 *  over a function body.
 */

// inline
void RegSet::rsSpillBeg()
{
    rsSpillChk();
}

/*****************************************************************************
 *
 *  Finish tracking spills - should be called each time after a pass is made
 *  over a function body.
 */

// inline
void RegSet::rsSpillEnd()
{
    rsSpillChk();
}

//****************************************************************************
//  Create a new SpillDsc or get one off the free list
//

// inline
RegSet::SpillDsc* RegSet::SpillDsc::alloc(Compiler* pComp, RegSet* regSet, var_types type)
{
    RegSet::SpillDsc*  spill;
    RegSet::SpillDsc** pSpill;

    pSpill = &(regSet->rsSpillFree);

    // Allocate spill structure
    if (*pSpill)
    {
        spill   = *pSpill;
        *pSpill = spill->spillNext;
    }
    else
    {
        spill = (RegSet::SpillDsc*)pComp->compGetMem(sizeof(SpillDsc));
    }
    return spill;
}

//****************************************************************************
//  Free a SpillDsc and return it to the rsSpillFree list
//

// inline
void RegSet::SpillDsc::freeDsc(RegSet* regSet, RegSet::SpillDsc* spillDsc)
{
    spillDsc->spillNext = regSet->rsSpillFree;
    regSet->rsSpillFree = spillDsc;
}

/*****************************************************************************
 *
 *  Make sure no spills are currently active - used for debugging of the code
 *  generator.
 */

#ifdef DEBUG

// inline
void RegSet::rsSpillChk()
{
    // All grabbed temps should have been released
    assert(m_rsCompiler->tmpGetCount == 0);

    for (regNumber reg = REG_FIRST; reg < REG_COUNT; reg = REG_NEXT(reg))
    {
        assert(rsSpillDesc[reg] == nullptr);

#ifdef LEGACY_BACKEND
        assert(rsUsedTree[reg] == NULL);
        assert(rsMultiDesc[reg] == NULL);
#endif // LEGACY_BACKEND
    }
}

#else

// inline
void RegSet::rsSpillChk()
{
}

#endif

/*****************************************************************************/
#if REDUNDANT_LOAD

// inline
bool RegTracker::rsIconIsInReg(ssize_t val, regNumber reg)
{
    if (compiler->opts.MinOpts() || compiler->opts.compDbgCode)
    {
        return false;
    }

    if (rsRegValues[reg].rvdKind == RV_INT_CNS && rsRegValues[reg].rvdIntCnsVal == val)
    {
        return true;
    }
    return false;
}

#endif // REDUNDANT_LOAD
/*****************************************************************************/
