// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           RegAlloc                                        XX
XX                                                                           XX
XX  Does the register allocation and puts the remaining lclVars on the stack XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif
#include "regalloc.h"

#if FEATURE_FP_REGALLOC
Compiler::enumConfigRegisterFP Compiler::raConfigRegisterFP()
{
    DWORD val = JitConfig.JitRegisterFP();

    return (enumConfigRegisterFP)(val & 0x3);
}
#endif // FEATURE_FP_REGALLOC

regMaskTP Compiler::raConfigRestrictMaskFP()
{
    regMaskTP result = RBM_NONE;

#if FEATURE_FP_REGALLOC
    switch (raConfigRegisterFP())
    {
        case CONFIG_REGISTER_FP_NONE:
            result = RBM_NONE;
            break;
        case CONFIG_REGISTER_FP_CALLEE_TRASH:
            result = RBM_FLT_CALLEE_TRASH;
            break;
        case CONFIG_REGISTER_FP_CALLEE_SAVED:
            result = RBM_FLT_CALLEE_SAVED;
            break;
        case CONFIG_REGISTER_FP_FULL:
            result = RBM_ALLFLOAT;
            break;
    }
#endif

    return result;
}

#if DOUBLE_ALIGN
DWORD Compiler::getCanDoubleAlign()
{
#ifdef DEBUG
    if (compStressCompile(STRESS_DBL_ALN, 20))
        return MUST_DOUBLE_ALIGN;

    return JitConfig.JitDoubleAlign();
#else
    return DEFAULT_DOUBLE_ALIGN;
#endif
}

//------------------------------------------------------------------------
// shouldDoubleAlign: Determine whether to double-align the frame
//
// Arguments:
//    refCntStk       - sum of     ref counts for all stack based variables
//    refCntEBP       - sum of     ref counts for EBP enregistered variables
//    refCntWtdEBP    - sum of wtd ref counts for EBP enregistered variables
//    refCntStkParam  - sum of     ref counts for all stack based parameters
//    refCntWtdStkDbl - sum of wtd ref counts for stack based doubles (including structs
//                      with double fields).
//
// Return Value:
//    Returns true if this method estimates that a double-aligned frame would be beneficial
//
// Notes:
//    The impact of a double-aligned frame is computed as follows:
//    - We save a byte of code for each parameter reference (they are frame-pointer relative)
//    - We pay a byte of code for each non-parameter stack reference.
//    - We save the misalignment penalty and possible cache-line crossing penalty.
//      This is estimated as 0 for SMALL_CODE, 16 for FAST_CODE and 4 otherwise.
//    - We pay 7 extra bytes for:
//        MOV EBP,ESP,
//        LEA ESP,[EBP-offset]
//        AND ESP,-8 to double align ESP
//    - We pay one extra memory reference for each variable that could have been enregistered in EBP (refCntWtdEBP).
//
//    If the misalignment penalty is estimated to be less than the bytes used, we don't double align.
//    Otherwise, we compare the weighted ref count of ebp-enregistered variables aginst double the
//    ref count for double-aligned values.
//
bool Compiler::shouldDoubleAlign(
    unsigned refCntStk, unsigned refCntEBP, unsigned refCntWtdEBP, unsigned refCntStkParam, unsigned refCntWtdStkDbl)
{
    bool           doDoubleAlign        = false;
    const unsigned DBL_ALIGN_SETUP_SIZE = 7;

    unsigned bytesUsed         = refCntStk + refCntEBP - refCntStkParam + DBL_ALIGN_SETUP_SIZE;
    unsigned misaligned_weight = 4;

    if (compCodeOpt() == Compiler::SMALL_CODE)
        misaligned_weight = 0;

    if (compCodeOpt() == Compiler::FAST_CODE)
        misaligned_weight *= 4;

    JITDUMP("\nDouble alignment:\n");
    JITDUMP("  Bytes that could be saved by not using EBP frame: %i\n", bytesUsed);
    JITDUMP("  Sum of weighted ref counts for EBP enregistered variables: %i\n", refCntWtdEBP);
    JITDUMP("  Sum of weighted ref counts for weighted stack based doubles: %i\n", refCntWtdStkDbl);

    if (bytesUsed > ((refCntWtdStkDbl * misaligned_weight) / BB_UNITY_WEIGHT))
    {
        JITDUMP("    Predicting not to double-align ESP to save %d bytes of code.\n", bytesUsed);
    }
    else if (refCntWtdEBP > refCntWtdStkDbl * 2)
    {
        // TODO-CQ: On P4 2 Proc XEON's, SciMark.FFT degrades if SciMark.FFT.transform_internal is
        // not double aligned.
        // Here are the numbers that make this not double-aligned.
        //     refCntWtdStkDbl = 0x164
        //     refCntWtdEBP    = 0x1a4
        // We think we do need to change the heuristic to be in favor of double-align.

        JITDUMP("    Predicting not to double-align ESP to allow EBP to be used to enregister variables.\n");
    }
    else
    {
        // OK we passed all of the benefit tests, so we'll predict a double aligned frame.
        JITDUMP("    Predicting to create a double-aligned frame\n");
        doDoubleAlign = true;
    }
    return doDoubleAlign;
}
#endif // DOUBLE_ALIGN

#ifdef LEGACY_BACKEND // We don't use any of the old register allocator functions when LSRA is used instead.

void Compiler::raInit()
{
#if FEATURE_STACK_FP_X87
    /* We have not assigned any FP variables to registers yet */

    VarSetOps::AssignNoCopy(this, optAllFPregVars, VarSetOps::UninitVal());
#endif
    codeGen->intRegState.rsIsFloat   = false;
    codeGen->floatRegState.rsIsFloat = true;

    rpReverseEBPenreg = false;
    rpAsgVarNum       = -1;
    rpPassesMax       = 6;
    rpPassesPessimize = rpPassesMax - 3;
    if (opts.compDbgCode)
    {
        rpPassesMax++;
    }
    rpStkPredict            = (unsigned)-1;
    rpFrameType             = FT_NOT_SET;
    rpLostEnreg             = false;
    rpMustCreateEBPCalled   = false;
    rpRegAllocDone          = false;
    rpMaskPInvokeEpilogIntf = RBM_NONE;

    rpPredictMap[PREDICT_NONE] = RBM_NONE;
    rpPredictMap[PREDICT_ADDR] = RBM_NONE;

#if FEATURE_FP_REGALLOC
    rpPredictMap[PREDICT_REG]         = RBM_ALLINT | RBM_ALLFLOAT;
    rpPredictMap[PREDICT_SCRATCH_REG] = RBM_ALLINT | RBM_ALLFLOAT;
#else
    rpPredictMap[PREDICT_REG]         = RBM_ALLINT;
    rpPredictMap[PREDICT_SCRATCH_REG] = RBM_ALLINT;
#endif

#define REGDEF(name, rnum, mask, sname) rpPredictMap[PREDICT_REG_##name] = RBM_##name;
#include "register.h"

#if defined(_TARGET_ARM_)

    rpPredictMap[PREDICT_PAIR_R0R1] = RBM_R0 | RBM_R1;
    rpPredictMap[PREDICT_PAIR_R2R3] = RBM_R2 | RBM_R3;
    rpPredictMap[PREDICT_REG_SP]    = RBM_ILLEGAL;

#elif defined(_TARGET_AMD64_)

    rpPredictMap[PREDICT_NOT_REG_EAX] = RBM_ALLINT & ~RBM_EAX;
    rpPredictMap[PREDICT_NOT_REG_ECX] = RBM_ALLINT & ~RBM_ECX;
    rpPredictMap[PREDICT_REG_ESP]     = RBM_ILLEGAL;

#elif defined(_TARGET_X86_)

    rpPredictMap[PREDICT_NOT_REG_EAX] = RBM_ALLINT & ~RBM_EAX;
    rpPredictMap[PREDICT_NOT_REG_ECX] = RBM_ALLINT & ~RBM_ECX;
    rpPredictMap[PREDICT_REG_ESP]     = RBM_ILLEGAL;
    rpPredictMap[PREDICT_PAIR_EAXEDX] = RBM_EAX | RBM_EDX;
    rpPredictMap[PREDICT_PAIR_ECXEBX] = RBM_ECX | RBM_EBX;

#endif

    rpBestRecordedPrediction = NULL;
}

/*****************************************************************************
 *
 *  The following table(s) determines the order in which registers are considered
 *  for variables to live in
 */

const regNumber* Compiler::raGetRegVarOrder(var_types regType, unsigned* wbVarOrderSize)
{
#if FEATURE_FP_REGALLOC
    if (varTypeIsFloating(regType))
    {
        static const regNumber raRegVarOrderFlt[]   = {REG_VAR_ORDER_FLT};
        const unsigned         raRegVarOrderFltSize = sizeof(raRegVarOrderFlt) / sizeof(raRegVarOrderFlt[0]);

        if (wbVarOrderSize != NULL)
            *wbVarOrderSize = raRegVarOrderFltSize;

        return &raRegVarOrderFlt[0];
    }
    else
#endif
    {
        static const regNumber raRegVarOrder[]   = {REG_VAR_ORDER};
        const unsigned         raRegVarOrderSize = sizeof(raRegVarOrder) / sizeof(raRegVarOrder[0]);

        if (wbVarOrderSize != NULL)
            *wbVarOrderSize = raRegVarOrderSize;

        return &raRegVarOrder[0];
    }
}

#ifdef DEBUG

/*****************************************************************************
 *
 *  Dump out the variable interference graph
 *
 */

void Compiler::raDumpVarIntf()
{
    unsigned   lclNum;
    LclVarDsc* varDsc;

    printf("Var. interference graph for %s\n", info.compFullName);

    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        /* Ignore the variable if it's not tracked */

        if (!varDsc->lvTracked)
            continue;

        /* Get hold of the index and the interference mask for the variable */
        unsigned varIndex = varDsc->lvVarIndex;

        printf("  V%02u,T%02u and ", lclNum, varIndex);

        unsigned refIndex;

        for (refIndex = 0; refIndex < lvaTrackedCount; refIndex++)
        {
            if (VarSetOps::IsMember(this, lvaVarIntf[varIndex], refIndex))
                printf("T%02u ", refIndex);
            else
                printf("    ");
        }

        printf("\n");
    }

    printf("\n");
}

/*****************************************************************************
 *
 *  Dump out the register interference graph
 *
 */
void Compiler::raDumpRegIntf()
{
    printf("Reg. interference graph for %s\n", info.compFullName);

    unsigned   lclNum;
    LclVarDsc* varDsc;

    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        unsigned varNum;

        /* Ignore the variable if it's not tracked */

        if (!varDsc->lvTracked)
            continue;

        /* Get hold of the index and the interference mask for the variable */

        varNum = varDsc->lvVarIndex;

        printf("  V%02u,T%02u and ", lclNum, varNum);

        if (varDsc->IsFloatRegType())
        {
#if !FEATURE_STACK_FP_X87
            for (regNumber regNum = REG_FP_FIRST; regNum <= REG_FP_LAST; regNum = REG_NEXT(regNum))
            {
                if (VarSetOps::IsMember(this, raLclRegIntf[regNum], varNum))
                    printf("%3s ", getRegName(regNum, true));
                else
                    printf("    ");
            }
#endif
        }
        else
        {
            for (regNumber regNum = REG_INT_FIRST; regNum <= REG_INT_LAST; regNum = REG_NEXT(regNum))
            {
                if (VarSetOps::IsMember(this, raLclRegIntf[regNum], varNum))
                    printf("%3s ", getRegName(regNum));
                else
                    printf("    ");
            }
        }

        printf("\n");
    }

    printf("\n");
}
#endif // DEBUG

/*****************************************************************************
 *
 * We'll adjust the ref counts based on interference
 *
 */

void Compiler::raAdjustVarIntf()
{
    // This method was not correct and has been disabled.
    return;
}

/*****************************************************************************/
/*****************************************************************************/
/* Determine register mask for a call/return from type.
 */

inline regMaskTP Compiler::genReturnRegForTree(GenTreePtr tree)
{
    var_types type = tree->TypeGet();

    if (type == TYP_STRUCT && IsHfa(tree))
    {
        int retSlots = GetHfaCount(tree);
        return ((1 << retSlots) - 1) << REG_FLOATRET;
    }

    const static regMaskTP returnMap[TYP_COUNT] = {
        RBM_ILLEGAL,   // TYP_UNDEF,
        RBM_NONE,      // TYP_VOID,
        RBM_INTRET,    // TYP_BOOL,
        RBM_INTRET,    // TYP_CHAR,
        RBM_INTRET,    // TYP_BYTE,
        RBM_INTRET,    // TYP_UBYTE,
        RBM_INTRET,    // TYP_SHORT,
        RBM_INTRET,    // TYP_USHORT,
        RBM_INTRET,    // TYP_INT,
        RBM_INTRET,    // TYP_UINT,
        RBM_LNGRET,    // TYP_LONG,
        RBM_LNGRET,    // TYP_ULONG,
        RBM_FLOATRET,  // TYP_FLOAT,
        RBM_DOUBLERET, // TYP_DOUBLE,
        RBM_INTRET,    // TYP_REF,
        RBM_INTRET,    // TYP_BYREF,
        RBM_INTRET,    // TYP_ARRAY,
        RBM_ILLEGAL,   // TYP_STRUCT,
        RBM_ILLEGAL,   // TYP_BLK,
        RBM_ILLEGAL,   // TYP_LCLBLK,
        RBM_ILLEGAL,   // TYP_PTR,
        RBM_ILLEGAL,   // TYP_FNC,
        RBM_ILLEGAL,   // TYP_UNKNOWN,
    };

    assert((unsigned)type < sizeof(returnMap) / sizeof(returnMap[0]));
    assert(returnMap[TYP_LONG] == RBM_LNGRET);
    assert(returnMap[TYP_DOUBLE] == RBM_DOUBLERET);
    assert(returnMap[TYP_REF] == RBM_INTRET);
    assert(returnMap[TYP_STRUCT] == RBM_ILLEGAL);

    regMaskTP result = returnMap[type];
    assert(result != RBM_ILLEGAL);
    return result;
}

/*****************************************************************************/

/****************************************************************************/

#ifdef DEBUG

static void dispLifeSet(Compiler* comp, VARSET_VALARG_TP mask, VARSET_VALARG_TP life)
{
    unsigned   lclNum;
    LclVarDsc* varDsc;

    for (lclNum = 0, varDsc = comp->lvaTable; lclNum < comp->lvaCount; lclNum++, varDsc++)
    {
        if (!varDsc->lvTracked)
            continue;

        if (!VarSetOps::IsMember(comp, mask, varDsc->lvVarIndex))
            continue;

        if (VarSetOps::IsMember(comp, life, varDsc->lvVarIndex))
            printf("V%02u ", lclNum);
    }
}

#endif

/*****************************************************************************/
#ifdef DEBUG
/*****************************************************************************
 *
 *  Debugging helpers - display variables liveness info.
 */

void dispFPvarsInBBlist(BasicBlock* beg, BasicBlock* end, VARSET_TP mask, Compiler* comp)
{
    do
    {
        printf("BB%02u: ", beg->bbNum);

        printf(" in  = [ ");
        dispLifeSet(comp, mask, beg->bbLiveIn);
        printf("] ,");

        printf(" out = [ ");
        dispLifeSet(comp, mask, beg->bbLiveOut);
        printf("]");

        if (beg->bbFlags & BBF_VISITED)
            printf(" inner=%u", beg->bbFPinVars);

        printf("\n");

        beg = beg->bbNext;
        if (!beg)
            return;
    } while (beg != end);
}

#if FEATURE_STACK_FP_X87
void Compiler::raDispFPlifeInfo()
{
    BasicBlock* block;

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        GenTreePtr stmt;

        printf("BB%02u: in  = [ ", block->bbNum);
        dispLifeSet(this, optAllFloatVars, block->bbLiveIn);
        printf("]\n\n");

        VARSET_TP life(VarSetOps::MakeCopy(this, block->bbLiveIn));
        for (stmt = block->bbTreeList; stmt; stmt = stmt->gtNext)
        {
            GenTreePtr tree;

            noway_assert(stmt->gtOper == GT_STMT);

            for (tree = stmt->gtStmt.gtStmtList; tree; tree = tree->gtNext)
            {
                VarSetOps::AssignNoCopy(this, life, fgUpdateLiveSet(life, tree));

                dispLifeSet(this, optAllFloatVars, life);
                printf("   ");
                gtDispTree(tree, 0, NULL, true);
            }

            printf("\n");
        }

        printf("BB%02u: out = [ ", block->bbNum);
        dispLifeSet(this, optAllFloatVars, block->bbLiveOut);
        printf("]\n\n");
    }
}
#endif // FEATURE_STACK_FP_X87
/*****************************************************************************/
#endif // DEBUG
/*****************************************************************************/

/*****************************************************************************/

void Compiler::raSetRegVarOrder(
    var_types regType, regNumber* customVarOrder, unsigned* customVarOrderSize, regMaskTP prefReg, regMaskTP avoidReg)
{
    unsigned         normalVarOrderSize;
    const regNumber* normalVarOrder = raGetRegVarOrder(regType, &normalVarOrderSize);
    unsigned         index;
    unsigned         listIndex = 0;
    regMaskTP        usedReg   = avoidReg;

    noway_assert(*customVarOrderSize >= normalVarOrderSize);

    if (prefReg)
    {
        /* First place the preferred registers at the start of customVarOrder */

        regMaskTP regBit;
        regNumber regNum;

        for (index = 0; index < normalVarOrderSize; index++)
        {
            regNum = normalVarOrder[index];
            regBit = genRegMask(regNum);

            if (usedReg & regBit)
                continue;

            if (prefReg & regBit)
            {
                usedReg |= regBit;
                noway_assert(listIndex < normalVarOrderSize);
                customVarOrder[listIndex++] = regNum;
                prefReg -= regBit;
                if (prefReg == 0)
                    break;
            }
        }

#if CPU_HAS_BYTE_REGS
        /* Then if byteable registers are preferred place them */

        if (prefReg & RBM_BYTE_REG_FLAG)
        {
            for (index = 0; index < normalVarOrderSize; index++)
            {
                regNum = normalVarOrder[index];
                regBit = genRegMask(regNum);

                if (usedReg & regBit)
                    continue;

                if (RBM_BYTE_REGS & regBit)
                {
                    usedReg |= regBit;
                    noway_assert(listIndex < normalVarOrderSize);
                    customVarOrder[listIndex++] = regNum;
                }
            }
        }

#endif // CPU_HAS_BYTE_REGS
    }

    /* Now place all the non-preferred registers */

    for (index = 0; index < normalVarOrderSize; index++)
    {
        regNumber regNum = normalVarOrder[index];
        regMaskTP regBit = genRegMask(regNum);

        if (usedReg & regBit)
            continue;

        usedReg |= regBit;
        noway_assert(listIndex < normalVarOrderSize);
        customVarOrder[listIndex++] = regNum;
    }

    if (avoidReg)
    {
        /* Now place the "avoid" registers */

        for (index = 0; index < normalVarOrderSize; index++)
        {
            regNumber regNum = normalVarOrder[index];
            regMaskTP regBit = genRegMask(regNum);

            if (avoidReg & regBit)
            {
                noway_assert(listIndex < normalVarOrderSize);
                customVarOrder[listIndex++] = regNum;
                avoidReg -= regBit;
                if (avoidReg == 0)
                    break;
            }
        }
    }

    *customVarOrderSize = listIndex;
    noway_assert(listIndex == normalVarOrderSize);
}

/*****************************************************************************
 *
 *  Setup the raAvoidArgRegMask and rsCalleeRegArgMaskLiveIn
 */

void Compiler::raSetupArgMasks(RegState* regState)
{
    /* Determine the registers holding incoming register arguments */
    /*  and setup raAvoidArgRegMask to the set of registers that we  */
    /*  may want to avoid when enregistering the locals.            */

    regState->rsCalleeRegArgMaskLiveIn = RBM_NONE;
    raAvoidArgRegMask                  = RBM_NONE;

    LclVarDsc* argsEnd = lvaTable + info.compArgsCount;

    for (LclVarDsc* argDsc = lvaTable; argDsc < argsEnd; argDsc++)
    {
        noway_assert(argDsc->lvIsParam);

        // Is it a register argument ?
        if (!argDsc->lvIsRegArg)
            continue;

        // only process args that apply to the current register file
        if ((argDsc->IsFloatRegType() && !info.compIsVarArgs && !opts.compUseSoftFP) != regState->rsIsFloat)
        {
            continue;
        }

        // Is it dead on entry ??
        // In certain cases such as when compJmpOpUsed is true,
        // or when we have a generic type context arg that we must report
        // then the arguments have to be kept alive throughout the prolog.
        // So we have to consider it as live on entry.
        //
        bool keepArgAlive = compJmpOpUsed;
        if ((unsigned(info.compTypeCtxtArg) != BAD_VAR_NUM) && lvaReportParamTypeArg() &&
            ((lvaTable + info.compTypeCtxtArg) == argDsc))
        {
            keepArgAlive = true;
        }

        if (!keepArgAlive && argDsc->lvTracked && !VarSetOps::IsMember(this, fgFirstBB->bbLiveIn, argDsc->lvVarIndex))
        {
            continue;
        }

        // The code to set the regState for each arg is outlined for shared use
        // by linear scan
        regNumber inArgReg = raUpdateRegStateForArg(regState, argDsc);

        // Do we need to try to avoid this incoming arg registers?

        // If it's not tracked, don't do the stuff below.
        if (!argDsc->lvTracked)
            continue;

        // If the incoming arg is used after a call it is live accross
        //  a call and will have to be allocated to a caller saved
        //  register anyway (a very common case).
        //
        // In this case it is pointless to ask that the higher ref count
        //  locals to avoid using the incoming arg register

        unsigned argVarIndex = argDsc->lvVarIndex;

        /* Does the incoming register and the arg variable interfere? */

        if (!VarSetOps::IsMember(this, raLclRegIntf[inArgReg], argVarIndex))
        {
            // No they do not interfere,
            //  so we add inArgReg to raAvoidArgRegMask

            raAvoidArgRegMask |= genRegMask(inArgReg);
        }
#ifdef _TARGET_ARM_
        if (argDsc->lvType == TYP_DOUBLE)
        {
            // Avoid the double register argument pair for register allocation.
            if (!VarSetOps::IsMember(this, raLclRegIntf[inArgReg + 1], argVarIndex))
            {
                raAvoidArgRegMask |= genRegMask(static_cast<regNumber>(inArgReg + 1));
            }
        }
#endif
    }
}

#endif // LEGACY_BACKEND

// The code to set the regState for each arg is outlined for shared use
// by linear scan. (It is not shared for System V AMD64 platform.)
regNumber Compiler::raUpdateRegStateForArg(RegState* regState, LclVarDsc* argDsc)
{
    regNumber inArgReg  = argDsc->lvArgReg;
    regMaskTP inArgMask = genRegMask(inArgReg);

    if (regState->rsIsFloat)
    {
        noway_assert(inArgMask & RBM_FLTARG_REGS);
    }
    else //  regState is for the integer registers
    {
        // This might be the fixed return buffer register argument (on ARM64)
        // We check and allow inArgReg to be theFixedRetBuffReg
        if (hasFixedRetBuffReg() && (inArgReg == theFixedRetBuffReg()))
        {
            // We should have a TYP_BYREF or TYP_I_IMPL arg and not a TYP_STRUCT arg
            noway_assert(argDsc->lvType == TYP_BYREF || argDsc->lvType == TYP_I_IMPL);
            // We should have recorded the variable number for the return buffer arg
            noway_assert(info.compRetBuffArg != BAD_VAR_NUM);
        }
        else // we have a regular arg
        {
            noway_assert(inArgMask & RBM_ARG_REGS);
        }
    }

    regState->rsCalleeRegArgMaskLiveIn |= inArgMask;

#ifdef _TARGET_ARM_
    if (argDsc->lvType == TYP_DOUBLE)
    {
        if (info.compIsVarArgs || opts.compUseSoftFP)
        {
            assert((inArgReg == REG_R0) || (inArgReg == REG_R2));
            assert(!regState->rsIsFloat);
        }
        else
        {
            assert(regState->rsIsFloat);
            assert(emitter::isDoubleReg(inArgReg));
        }
        regState->rsCalleeRegArgMaskLiveIn |= genRegMask((regNumber)(inArgReg + 1));
    }
    else if (argDsc->lvType == TYP_LONG)
    {
        assert((inArgReg == REG_R0) || (inArgReg == REG_R2));
        assert(!regState->rsIsFloat);
        regState->rsCalleeRegArgMaskLiveIn |= genRegMask((regNumber)(inArgReg + 1));
    }
#endif // _TARGET_ARM_

#if FEATURE_MULTIREG_ARGS
    if (argDsc->lvType == TYP_STRUCT)
    {
        if (argDsc->lvIsHfaRegArg())
        {
            assert(regState->rsIsFloat);
            unsigned cSlots = GetHfaCount(argDsc->lvVerTypeInfo.GetClassHandleForValueClass());
            for (unsigned i = 1; i < cSlots; i++)
            {
                assert(inArgReg + i <= LAST_FP_ARGREG);
                regState->rsCalleeRegArgMaskLiveIn |= genRegMask(static_cast<regNumber>(inArgReg + i));
            }
        }
        else
        {
            unsigned cSlots = argDsc->lvSize() / TARGET_POINTER_SIZE;
            for (unsigned i = 1; i < cSlots; i++)
            {
                regNumber nextArgReg = (regNumber)(inArgReg + i);
                if (nextArgReg > REG_ARG_LAST)
                {
                    break;
                }
                assert(regState->rsIsFloat == false);
                regState->rsCalleeRegArgMaskLiveIn |= genRegMask(nextArgReg);
            }
        }
    }
#endif // FEATURE_MULTIREG_ARGS

    return inArgReg;
}

#ifdef LEGACY_BACKEND // We don't use any of the old register allocator functions when LSRA is used instead.

/*****************************************************************************
 *
 *  Assign variables to live in registers, etc.
 */

void Compiler::raAssignVars()
{
#ifdef DEBUG
    if (verbose)
        printf("*************** In raAssignVars()\n");
#endif
    /* We need to keep track of which registers we ever touch */

    codeGen->regSet.rsClearRegsModified();

#if FEATURE_STACK_FP_X87
    // FP register allocation
    raEnregisterVarsStackFP();
    raGenerateFPRefCounts();
#endif

    /* Predict registers used by code generation */
    rpPredictRegUse(); // New reg predictor/allocator

    // Change all unused promoted non-argument struct locals to a non-GC type (in this case TYP_INT)
    // so that the gc tracking logic and lvMustInit logic will ignore them.

    unsigned   lclNum;
    LclVarDsc* varDsc;

    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        if (varDsc->lvType != TYP_STRUCT)
            continue;

        if (!varDsc->lvPromoted)
            continue;

        if (varDsc->lvIsParam)
            continue;

        if (varDsc->lvRefCnt > 0)
            continue;

#ifdef DEBUG
        if (verbose)
        {
            printf("Mark unused struct local V%02u\n", lclNum);
        }

        lvaPromotionType promotionType = lvaGetPromotionType(varDsc);

        if (promotionType == PROMOTION_TYPE_DEPENDENT)
        {
            // This should only happen when all its field locals are unused as well.

            for (unsigned varNum = varDsc->lvFieldLclStart; varNum < varDsc->lvFieldLclStart + varDsc->lvFieldCnt;
                 varNum++)
            {
                noway_assert(lvaTable[varNum].lvRefCnt == 0);
            }
        }
        else
        {
            noway_assert(promotionType == PROMOTION_TYPE_INDEPENDENT);
        }

        varDsc->lvUnusedStruct = 1;
#endif

        // Change such struct locals to ints

        varDsc->lvType = TYP_INT; // Bash to a non-gc type.
        noway_assert(!varDsc->lvTracked);
        noway_assert(!varDsc->lvRegister);
        varDsc->lvOnFrame  = false; // Force it not to be onstack.
        varDsc->lvMustInit = false; // Force not to init it.
        varDsc->lvStkOffs  = 0;     // Set it to anything other than BAD_STK_OFFS to make genSetScopeInfo() happy
    }
}

/*****************************************************************************/
/*****************************************************************************/

/*****************************************************************************
 *
 *   Given a regNumber return the correct predictReg enum value
 */

inline static rpPredictReg rpGetPredictForReg(regNumber reg)
{
    return (rpPredictReg)(((int)reg) + ((int)PREDICT_REG_FIRST));
}

/*****************************************************************************
 *
 *   Given a varIndex return the correct predictReg enum value
 */

inline static rpPredictReg rpGetPredictForVarIndex(unsigned varIndex)
{
    return (rpPredictReg)(varIndex + ((int)PREDICT_REG_VAR_T00));
}

/*****************************************************************************
 *
 *   Given a rpPredictReg return the correct varNumber value
 */

inline static unsigned rpGetVarIndexForPredict(rpPredictReg predict)
{
    return (unsigned)predict - (unsigned)PREDICT_REG_VAR_T00;
}

/*****************************************************************************
 *
 *   Given a rpPredictReg return true if it specifies a Txx register
 */

inline static bool rpHasVarIndexForPredict(rpPredictReg predict)
{
    if ((predict >= PREDICT_REG_VAR_T00) && (predict <= PREDICT_REG_VAR_MAX))
        return true;
    else
        return false;
}

/*****************************************************************************
 *
 *   Given a regmask return the correct predictReg enum value
 */

static rpPredictReg rpGetPredictForMask(regMaskTP regmask)
{
    rpPredictReg result = PREDICT_NONE;
    if (regmask != 0) /* Check if regmask has zero bits set */
    {
        if (((regmask - 1) & regmask) == 0) /* Check if regmask has one bit set */
        {
            DWORD reg = 0;
            assert(FitsIn<DWORD>(regmask));
            BitScanForward(&reg, (DWORD)regmask);
            return rpGetPredictForReg((regNumber)reg);
        }

#if defined(_TARGET_ARM_)
        /* It has multiple bits set */
        else if (regmask == (RBM_R0 | RBM_R1))
        {
            result = PREDICT_PAIR_R0R1;
        }
        else if (regmask == (RBM_R2 | RBM_R3))
        {
            result = PREDICT_PAIR_R2R3;
        }
#elif defined(_TARGET_X86_)
        /* It has multiple bits set */
        else if (regmask == (RBM_EAX | RBM_EDX))
        {
            result = PREDICT_PAIR_EAXEDX;
        }
        else if (regmask == (RBM_ECX | RBM_EBX))
        {
            result = PREDICT_PAIR_ECXEBX;
        }
#endif
        else /* It doesn't match anything */
        {
            result = PREDICT_NONE;
            assert(!"unreachable");
            NO_WAY("bad regpair");
        }
    }
    return result;
}

/*****************************************************************************
 *
 *  Record a variable to register(s) interference
 */

bool Compiler::rpRecordRegIntf(regMaskTP regMask, VARSET_VALARG_TP life DEBUGARG(const char* msg))

{
    bool addedIntf = false;

    if (regMask != 0)
    {
        for (regNumber regNum = REG_FIRST; regNum < REG_COUNT; regNum = REG_NEXT(regNum))
        {
            regMaskTP regBit = genRegMask(regNum);

            if (regMask & regBit)
            {
                VARSET_TP newIntf(VarSetOps::Diff(this, life, raLclRegIntf[regNum]));
                if (!VarSetOps::IsEmpty(this, newIntf))
                {
#ifdef DEBUG
                    if (verbose)
                    {
                        VARSET_ITER_INIT(this, newIntfIter, newIntf, varNum);
                        while (newIntfIter.NextElem(&varNum))
                        {
                            unsigned   lclNum = lvaTrackedToVarNum[varNum];
                            LclVarDsc* varDsc = &lvaTable[varNum];
#if FEATURE_FP_REGALLOC
                            // Only print the useful interferences
                            // i.e. floating point LclVar interference with floating point registers
                            //         or integer LclVar interference with general purpose registers
                            if (varTypeIsFloating(varDsc->TypeGet()) == genIsValidFloatReg(regNum))
#endif
                            {
                                printf("Record interference between V%02u,T%02u and %s -- %s\n", lclNum, varNum,
                                       getRegName(regNum), msg);
                            }
                        }
                    }
#endif
                    addedIntf = true;
                    VarSetOps::UnionD(this, raLclRegIntf[regNum], newIntf);
                }

                regMask -= regBit;
                if (regMask == 0)
                    break;
            }
        }
    }
    return addedIntf;
}

/*****************************************************************************
 *
 *  Record a new variable to variable(s) interference
 */

bool Compiler::rpRecordVarIntf(unsigned varNum, VARSET_VALARG_TP intfVar DEBUGARG(const char* msg))
{
    noway_assert((varNum >= 0) && (varNum < lvaTrackedCount));
    noway_assert(!VarSetOps::IsEmpty(this, intfVar));

    VARSET_TP oneVar(VarSetOps::MakeEmpty(this));
    VarSetOps::AddElemD(this, oneVar, varNum);

    bool newIntf = fgMarkIntf(intfVar, oneVar);

    if (newIntf)
        rpAddedVarIntf = true;

#ifdef DEBUG
    if (verbose && newIntf)
    {
        for (unsigned oneNum = 0; oneNum < lvaTrackedCount; oneNum++)
        {
            if (VarSetOps::IsMember(this, intfVar, oneNum))
            {
                unsigned lclNum = lvaTrackedToVarNum[varNum];
                unsigned lclOne = lvaTrackedToVarNum[oneNum];
                printf("Record interference between V%02u,T%02u and V%02u,T%02u -- %s\n", lclNum, varNum, lclOne,
                       oneNum, msg);
            }
        }
    }
#endif

    return newIntf;
}

/*****************************************************************************
 *
 *   Determine preferred register mask for a given predictReg value
 */

inline regMaskTP Compiler::rpPredictRegMask(rpPredictReg predictReg, var_types type)
{
    if (rpHasVarIndexForPredict(predictReg))
        predictReg = PREDICT_REG;

    noway_assert((unsigned)predictReg < sizeof(rpPredictMap) / sizeof(rpPredictMap[0]));
    noway_assert(rpPredictMap[predictReg] != RBM_ILLEGAL);

    regMaskTP regAvailForType = rpPredictMap[predictReg];
    if (varTypeIsFloating(type))
    {
        regAvailForType &= RBM_ALLFLOAT;
    }
    else
    {
        regAvailForType &= RBM_ALLINT;
    }
#ifdef _TARGET_ARM_
    if (type == TYP_DOUBLE)
    {
        if ((predictReg >= PREDICT_REG_F0) && (predictReg <= PREDICT_REG_F31))
        {
            // Fix 388433 ARM JitStress WP7
            if ((regAvailForType & RBM_DBL_REGS) != 0)
            {
                regAvailForType |= (regAvailForType << 1);
            }
            else
            {
                regAvailForType = RBM_NONE;
            }
        }
    }
#endif
    return regAvailForType;
}

/*****************************************************************************
 *
 *  Predict register choice for a type.
 *
 *  Adds the predicted registers to rsModifiedRegsMask.
 */
regMaskTP Compiler::rpPredictRegPick(var_types type, rpPredictReg predictReg, regMaskTP lockedRegs)
{
    regMaskTP preferReg = rpPredictRegMask(predictReg, type);
    regNumber regNum;
    regMaskTP regBits;

    // Add any reserved register to the lockedRegs
    lockedRegs |= codeGen->regSet.rsMaskResvd;

    /* Clear out the lockedRegs from preferReg */
    preferReg &= ~lockedRegs;

    if (rpAsgVarNum != -1)
    {
        noway_assert((rpAsgVarNum >= 0) && (rpAsgVarNum < (int)lclMAX_TRACKED));

        /* Don't pick the register used by rpAsgVarNum either */
        LclVarDsc* tgtVar = lvaTable + lvaTrackedToVarNum[rpAsgVarNum];
        noway_assert(tgtVar->lvRegNum != REG_STK);

        preferReg &= ~genRegMask(tgtVar->lvRegNum);
    }

    switch (type)
    {
        case TYP_BOOL:
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_SHORT:
        case TYP_CHAR:
        case TYP_INT:
        case TYP_UINT:
        case TYP_REF:
        case TYP_BYREF:
#ifdef _TARGET_AMD64_
        case TYP_LONG:
#endif // _TARGET_AMD64_

            // expand preferReg to all non-locked registers if no bits set
            preferReg = codeGen->regSet.rsUseIfZero(preferReg & RBM_ALLINT, RBM_ALLINT & ~lockedRegs);

            if (preferReg == 0) // no bits set?
            {
                // Add one predefined spill choice register if no bits set.
                // (The jit will introduce one spill temp)
                preferReg |= RBM_SPILL_CHOICE;
                rpPredictSpillCnt++;

#ifdef DEBUG
                if (verbose)
                    printf("Predict one spill temp\n");
#endif
            }

            if (preferReg != 0)
            {
                /* Iterate the registers in the order specified by rpRegTmpOrder */

                for (unsigned index = 0; index < REG_TMP_ORDER_COUNT; index++)
                {
                    regNum  = rpRegTmpOrder[index];
                    regBits = genRegMask(regNum);

                    if ((preferReg & regBits) == regBits)
                    {
                        goto RET;
                    }
                }
            }
            /* Otherwise we have allocated all registers, so do nothing */
            break;

#ifndef _TARGET_AMD64_
        case TYP_LONG:

            if ((preferReg == 0) ||                   // no bits set?
                ((preferReg & (preferReg - 1)) == 0)) // or only one bit set?
            {
                // expand preferReg to all non-locked registers
                preferReg = RBM_ALLINT & ~lockedRegs;
            }

            if (preferReg == 0) // no bits set?
            {
                // Add EAX:EDX to the registers
                // (The jit will introduce two spill temps)
                preferReg = RBM_PAIR_TMP;
                rpPredictSpillCnt += 2;
#ifdef DEBUG
                if (verbose)
                    printf("Predict two spill temps\n");
#endif
            }
            else if ((preferReg & (preferReg - 1)) == 0) // only one bit set?
            {
                if ((preferReg & RBM_PAIR_TMP_LO) == 0)
                {
                    // Add EAX to the registers
                    // (The jit will introduce one spill temp)
                    preferReg |= RBM_PAIR_TMP_LO;
                }
                else
                {
                    // Add EDX to the registers
                    // (The jit will introduce one spill temp)
                    preferReg |= RBM_PAIR_TMP_HI;
                }
                rpPredictSpillCnt++;
#ifdef DEBUG
                if (verbose)
                    printf("Predict one spill temp\n");
#endif
            }

            regPairNo regPair;
            regPair = codeGen->regSet.rsFindRegPairNo(preferReg);
            if (regPair != REG_PAIR_NONE)
            {
                regBits = genRegPairMask(regPair);
                goto RET;
            }

            /* Otherwise we have allocated all registers, so do nothing */
            break;
#endif // _TARGET_AMD64_

#ifdef _TARGET_ARM_
        case TYP_STRUCT:
#endif

        case TYP_FLOAT:
        case TYP_DOUBLE:

#if FEATURE_FP_REGALLOC
            regMaskTP restrictMask;
            restrictMask = (raConfigRestrictMaskFP() | RBM_FLT_CALLEE_TRASH);
            assert((restrictMask & RBM_SPILL_CHOICE_FLT) == RBM_SPILL_CHOICE_FLT);

            // expand preferReg to all available non-locked registers if no bits set
            preferReg = codeGen->regSet.rsUseIfZero(preferReg & restrictMask, restrictMask & ~lockedRegs);
            regMaskTP preferDouble;
            preferDouble = preferReg & (preferReg >> 1);

            if ((preferReg == 0) // no bits set?
#ifdef _TARGET_ARM_
                || ((type == TYP_DOUBLE) &&
                    ((preferReg & (preferReg >> 1)) == 0)) // or two consecutive bits set for TYP_DOUBLE
#endif
                )
            {
                // Add one predefined spill choice register if no bits set.
                // (The jit will introduce one spill temp)
                preferReg |= RBM_SPILL_CHOICE_FLT;
                rpPredictSpillCnt++;

#ifdef DEBUG
                if (verbose)
                    printf("Predict one spill temp (float)\n");
#endif
            }

            assert(preferReg != 0);

            /* Iterate the registers in the order specified by raRegFltTmpOrder */

            for (unsigned index = 0; index < REG_FLT_TMP_ORDER_COUNT; index++)
            {
                regNum  = raRegFltTmpOrder[index];
                regBits = genRegMask(regNum);

                if (varTypeIsFloating(type))
                {
#ifdef _TARGET_ARM_
                    if (type == TYP_DOUBLE)
                    {
                        if ((regBits & RBM_DBL_REGS) == 0)
                        {
                            continue; // We must restrict the set to the double registers
                        }
                        else
                        {
                            // TYP_DOUBLE use two consecutive registers
                            regBits |= genRegMask(REG_NEXT(regNum));
                        }
                    }
#endif
                    // See if COMPlus_JitRegisterFP is restricting this FP register
                    //
                    if ((restrictMask & regBits) != regBits)
                        continue;
                }

                if ((preferReg & regBits) == regBits)
                {
                    goto RET;
                }
            }
            /* Otherwise we have allocated all registers, so do nothing */
            break;

#else // !FEATURE_FP_REGALLOC

            return RBM_NONE;

#endif

        default:
            noway_assert(!"unexpected type in reg use prediction");
    }

    /* Abnormal return */
    noway_assert(!"Ran out of registers in rpPredictRegPick");
    return RBM_NONE;

RET:
    /*
     *  If during the first prediction we need to allocate
     *  one of the registers that we used for coloring locals
     *  then flag this by setting rpPredictAssignAgain.
     *  We will have to go back and repredict the registers
     */
    if ((rpPasses == 0) && ((rpPredictAssignMask & regBits) == regBits))
        rpPredictAssignAgain = true;

    // Add a register interference to each of the last use variables
    if (!VarSetOps::IsEmpty(this, rpLastUseVars) || !VarSetOps::IsEmpty(this, rpUseInPlace))
    {
        VARSET_TP lastUse(VarSetOps::MakeEmpty(this));
        VarSetOps::Assign(this, lastUse, rpLastUseVars);
        VARSET_TP inPlaceUse(VarSetOps::MakeEmpty(this));
        VarSetOps::Assign(this, inPlaceUse, rpUseInPlace);
        // While we still have any lastUse or inPlaceUse bits
        VARSET_TP useUnion(VarSetOps::Union(this, lastUse, inPlaceUse));

        VARSET_TP varAsSet(VarSetOps::MakeEmpty(this));
        VARSET_ITER_INIT(this, iter, useUnion, varNum);
        while (iter.NextElem(&varNum))
        {
            // We'll need this for one of the calls...
            VarSetOps::OldStyleClearD(this, varAsSet);
            VarSetOps::AddElemD(this, varAsSet, varNum);

            // If this varBit and lastUse?
            if (VarSetOps::IsMember(this, lastUse, varNum))
            {
                // Record a register to variable interference
                rpRecordRegIntf(regBits, varAsSet DEBUGARG("last use RegPick"));
            }

            // If this varBit and inPlaceUse?
            if (VarSetOps::IsMember(this, inPlaceUse, varNum))
            {
                // Record a register to variable interference
                rpRecordRegIntf(regBits, varAsSet DEBUGARG("used in place RegPick"));
            }
        }
    }
    codeGen->regSet.rsSetRegsModified(regBits);

    return regBits;
}

/*****************************************************************************
 *
 *  Predict integer register use for generating an address mode for a tree,
 *  by setting tree->gtUsedRegs to all registers used by this tree and its
 *  children.
 *    tree       - is the child of a GT_IND node
 *    type       - the type of the GT_IND node (floating point/integer)
 *    lockedRegs - are the registers which are currently held by
 *                 a previously evaluated node.
 *    rsvdRegs   - registers which should not be allocated because they will
 *                 be needed to evaluate a node in the future
 *               - Also if rsvdRegs has the RBM_LASTUSE bit set then
 *                 the rpLastUseVars set should be saved and restored
 *                 so that we don't add any new variables to rpLastUseVars
 *    lenCSE     - is non-NULL only when we have a lenCSE expression
 *
 *  Return the scratch registers to be held by this tree. (one or two registers
 *  to form an address expression)
 */

regMaskTP Compiler::rpPredictAddressMode(
    GenTreePtr tree, var_types type, regMaskTP lockedRegs, regMaskTP rsvdRegs, GenTreePtr lenCSE)
{
    GenTreePtr op1;
    GenTreePtr op2;
    GenTreePtr opTemp;
    genTreeOps oper = tree->OperGet();
    regMaskTP  op1Mask;
    regMaskTP  op2Mask;
    regMaskTP  regMask;
    ssize_t    sh;
    ssize_t    cns = 0;
    bool       rev;
    bool       hasTwoAddConst     = false;
    bool       restoreLastUseVars = false;
    VARSET_TP  oldLastUseVars(VarSetOps::MakeEmpty(this));

    /* do we need to save and restore the rpLastUseVars set ? */
    if ((rsvdRegs & RBM_LASTUSE) && (lenCSE == NULL))
    {
        restoreLastUseVars = true;
        VarSetOps::Assign(this, oldLastUseVars, rpLastUseVars);
    }
    rsvdRegs &= ~RBM_LASTUSE;

    /* if not an add, then just force it to a register */

    if (oper != GT_ADD)
    {
        if (oper == GT_ARR_ELEM)
        {
            regMask = rpPredictTreeRegUse(tree, PREDICT_NONE, lockedRegs, rsvdRegs);
            goto DONE;
        }
        else
        {
            goto NO_ADDR_EXPR;
        }
    }

    op1 = tree->gtOp.gtOp1;
    op2 = tree->gtOp.gtOp2;
    rev = ((tree->gtFlags & GTF_REVERSE_OPS) != 0);

    /* look for (x + y) + icon address mode */

    if (op2->OperGet() == GT_CNS_INT)
    {
        cns = op2->gtIntCon.gtIconVal;

        /* if not an add, then just force op1 into a register */
        if (op1->OperGet() != GT_ADD)
            goto ONE_ADDR_EXPR;

        hasTwoAddConst = true;

        /* Record the 'rev' flag, reverse evaluation order */
        rev = ((op1->gtFlags & GTF_REVERSE_OPS) != 0);

        op2 = op1->gtOp.gtOp2;
        op1 = op1->gtOp.gtOp1; // Overwrite op1 last!!
    }

    /* Check for CNS_INT or LSH of CNS_INT in op2 slot */

    sh = 0;
    if (op2->OperGet() == GT_LSH)
    {
        if (op2->gtOp.gtOp2->OperGet() == GT_CNS_INT)
        {
            sh     = op2->gtOp.gtOp2->gtIntCon.gtIconVal;
            opTemp = op2->gtOp.gtOp1;
        }
        else
        {
            opTemp = NULL;
        }
    }
    else
    {
        opTemp = op2;
    }

    if (opTemp != NULL)
    {
        if (opTemp->OperGet() == GT_NOP)
        {
            opTemp = opTemp->gtOp.gtOp1;
        }

        // Is this a const operand?
        if (opTemp->OperGet() == GT_CNS_INT)
        {
            // Compute the new cns value that Codegen will end up using
            cns += (opTemp->gtIntCon.gtIconVal << sh);

            goto ONE_ADDR_EXPR;
        }
    }

    /* Check for LSH in op1 slot */

    if (op1->OperGet() != GT_LSH)
        goto TWO_ADDR_EXPR;

    opTemp = op1->gtOp.gtOp2;

    if (opTemp->OperGet() != GT_CNS_INT)
        goto TWO_ADDR_EXPR;

    sh = opTemp->gtIntCon.gtIconVal;

    /* Check for LSH of 0, special case */
    if (sh == 0)
        goto TWO_ADDR_EXPR;

#if defined(_TARGET_XARCH_)

    /* Check for LSH of 1 2 or 3 */
    if (sh > 3)
        goto TWO_ADDR_EXPR;

#elif defined(_TARGET_ARM_)

    /* Check for LSH of 1 to 30 */
    if (sh > 30)
        goto TWO_ADDR_EXPR;

#else

    goto TWO_ADDR_EXPR;

#endif

    /* Matched a leftShift by 'sh' subtree, move op1 down */
    op1 = op1->gtOp.gtOp1;

TWO_ADDR_EXPR:

    /* Now we have to evaluate op1 and op2 into registers */

    /* Evaluate op1 and op2 in the correct order */
    if (rev)
    {
        op2Mask = rpPredictTreeRegUse(op2, PREDICT_REG, lockedRegs, rsvdRegs | op1->gtRsvdRegs);
        op1Mask = rpPredictTreeRegUse(op1, PREDICT_REG, lockedRegs | op2Mask, rsvdRegs);
    }
    else
    {
        op1Mask = rpPredictTreeRegUse(op1, PREDICT_REG, lockedRegs, rsvdRegs | op2->gtRsvdRegs);
        op2Mask = rpPredictTreeRegUse(op2, PREDICT_REG, lockedRegs | op1Mask, rsvdRegs);
    }

    /*  If op1 and op2 must be spilled and reloaded then
     *  op1 and op2 might be reloaded into the same register
     *  This can only happen when all the registers are lockedRegs
     */
    if ((op1Mask == op2Mask) && (op1Mask != 0))
    {
        /* We'll need to grab a different register for op2 */
        op2Mask = rpPredictRegPick(TYP_INT, PREDICT_REG, op1Mask);
    }

#ifdef _TARGET_ARM_
    // On the ARM we need a scratch register to evaluate the shifted operand for trees that have this form
    //      [op2 + op1<<sh + cns]
    // when op1 is an enregistered variable, thus the op1Mask is RBM_NONE
    //
    if (hasTwoAddConst && (sh != 0) && (op1Mask == RBM_NONE))
    {
        op1Mask |= rpPredictRegPick(TYP_INT, PREDICT_REG, (lockedRegs | op1Mask | op2Mask));
    }

    //
    // On the ARM we will need at least one scratch register for trees that have this form:
    //     [op1 + op2 + cns] or  [op1 + op2<<sh + cns]
    // or for a float/double or long when we have both op1 and op2
    // or when we have an 'cns' that is too large for the ld/st instruction
    //
    if (hasTwoAddConst || varTypeIsFloating(type) || (type == TYP_LONG) || !codeGen->validDispForLdSt(cns, type))
    {
        op2Mask |= rpPredictRegPick(TYP_INT, PREDICT_REG, (lockedRegs | op1Mask | op2Mask));
    }

    //
    // If we create a CSE that immediately dies then we may need to add an additional register interference
    // so we don't color the CSE into R3
    //
    if (!rev && (op1Mask != RBM_NONE) && (op2->OperGet() == GT_COMMA))
    {
        opTemp = op2->gtOp.gtOp2;
        if (opTemp->OperGet() == GT_LCL_VAR)
        {
            unsigned   varNum = opTemp->gtLclVar.gtLclNum;
            LclVarDsc* varDsc = &lvaTable[varNum];

            if (varDsc->lvTracked && !VarSetOps::IsMember(this, compCurLife, varDsc->lvVarIndex))
            {
                rpRecordRegIntf(RBM_TMP_0,
                                VarSetOps::MakeSingleton(this, varDsc->lvVarIndex) DEBUGARG("dead CSE (gt_ind)"));
            }
        }
    }
#endif

    regMask          = (op1Mask | op2Mask);
    tree->gtUsedRegs = (regMaskSmall)regMask;
    goto DONE;

ONE_ADDR_EXPR:

    /* now we have to evaluate op1 into a register */

    op1Mask = rpPredictTreeRegUse(op1, PREDICT_REG, lockedRegs, rsvdRegs);
    op2Mask = RBM_NONE;

#ifdef _TARGET_ARM_
    //
    // On the ARM we will need another scratch register when we have an 'cns' that is too large for the ld/st
    // instruction
    //
    if (!codeGen->validDispForLdSt(cns, type))
    {
        op2Mask |= rpPredictRegPick(TYP_INT, PREDICT_REG, (lockedRegs | op1Mask | op2Mask));
    }
#endif

    regMask          = (op1Mask | op2Mask);
    tree->gtUsedRegs = (regMaskSmall)regMask;
    goto DONE;

NO_ADDR_EXPR:

#if !CPU_LOAD_STORE_ARCH
    if (oper == GT_CNS_INT)
    {
        /* Indirect of a constant does not require a register */
        regMask = RBM_NONE;
    }
    else
#endif
    {
        /* now we have to evaluate tree into a register */
        regMask = rpPredictTreeRegUse(tree, PREDICT_REG, lockedRegs, rsvdRegs);
    }

DONE:
    regMaskTP regUse = tree->gtUsedRegs;

    if (!VarSetOps::IsEmpty(this, compCurLife))
    {
        // Add interference between the current set of life variables and
        //  the set of temporary registers need to evaluate the sub tree
        if (regUse)
        {
            rpRecordRegIntf(regUse, compCurLife DEBUGARG("tmp use (gt_ind)"));
        }
    }

    /* Do we need to resore the oldLastUseVars value */
    if (restoreLastUseVars)
    {
        /*
         *  If we used a GT_ASG targeted register then we need to add
         *  a variable interference between any new last use variables
         *  and the GT_ASG targeted register
         */
        if (!VarSetOps::Equal(this, rpLastUseVars, oldLastUseVars) && rpAsgVarNum != -1)
        {
            rpRecordVarIntf(rpAsgVarNum,
                            VarSetOps::Diff(this, rpLastUseVars, oldLastUseVars) DEBUGARG("asgn conflict (gt_ind)"));
        }
        VarSetOps::Assign(this, rpLastUseVars, oldLastUseVars);
    }

    return regMask;
}

/*****************************************************************************
 *
 *
 */

void Compiler::rpPredictRefAssign(unsigned lclNum)
{
    LclVarDsc* varDsc = lvaTable + lclNum;

    varDsc->lvRefAssign = 1;

#if NOGC_WRITE_BARRIERS
#ifdef DEBUG
    if (verbose)
    {
        if (!VarSetOps::IsMember(this, raLclRegIntf[REG_EDX], varDsc->lvVarIndex))
            printf("Record interference between V%02u,T%02u and REG WRITE BARRIER -- ref assign\n", lclNum,
                   varDsc->lvVarIndex);
    }
#endif

    /* Make sure that write barrier pointer variables never land in EDX */
    VarSetOps::AddElemD(this, raLclRegIntf[REG_EDX], varDsc->lvVarIndex);
#endif // NOGC_WRITE_BARRIERS
}

/*****************************************************************************
 *
 * Predict the internal temp physical register usage for a block assignment tree,
 * by setting tree->gtUsedRegs.
 * Records the internal temp physical register usage for this tree.
 * Returns a mask of interfering registers for this tree.
 *
 * Each of the switch labels in this function updates regMask and assigns tree->gtUsedRegs
 * to the set of scratch registers needed when evaluating the tree.
 * Generally tree->gtUsedRegs and the return value retMask are the same, except when the
 * parameter "lockedRegs" conflicts with the computed tree->gtUsedRegs, in which case we
 * predict additional internal temp physical registers to spill into.
 *
 *    tree       - is the child of a GT_IND node
 *    predictReg - what type of register does the tree need
 *    lockedRegs - are the registers which are currently held by a previously evaluated node.
 *                 Don't modify lockedRegs as it is used at the end to compute a spill mask.
 *    rsvdRegs   - registers which should not be allocated because they will
 *                 be needed to evaluate a node in the future
 *               - Also, if rsvdRegs has the RBM_LASTUSE bit set then
 *                 the rpLastUseVars set should be saved and restored
 *                 so that we don't add any new variables to rpLastUseVars.
 */
regMaskTP Compiler::rpPredictBlkAsgRegUse(GenTreePtr   tree,
                                          rpPredictReg predictReg,
                                          regMaskTP    lockedRegs,
                                          regMaskTP    rsvdRegs)
{
    regMaskTP regMask         = RBM_NONE;
    regMaskTP interferingRegs = RBM_NONE;

    bool        hasGCpointer  = false;
    bool        dstIsOnStack  = false;
    bool        useMemHelper  = false;
    bool        useBarriers   = false;
    GenTreeBlk* dst           = tree->gtGetOp1()->AsBlk();
    GenTreePtr  dstAddr       = dst->Addr();
    GenTreePtr  srcAddrOrFill = tree->gtGetOp2IfPresent();

    size_t blkSize = dst->gtBlkSize;

    hasGCpointer = (dst->HasGCPtr());

    bool isCopyBlk = tree->OperIsCopyBlkOp();
    bool isCopyObj = isCopyBlk && hasGCpointer;
    bool isInitBlk = tree->OperIsInitBlkOp();

    if (isCopyBlk)
    {
        assert(srcAddrOrFill->OperIsIndir());
        srcAddrOrFill = srcAddrOrFill->AsIndir()->Addr();
    }
    else
    {
        // For initBlk, we don't need to worry about the GC pointers.
        hasGCpointer = false;
    }

    if (blkSize != 0)
    {
        if (isCopyObj)
        {
            dstIsOnStack = (dstAddr->gtOper == GT_ADDR && (dstAddr->gtFlags & GTF_ADDR_ONSTACK));
        }

        if (isInitBlk)
        {
            if (srcAddrOrFill->OperGet() != GT_CNS_INT)
            {
                useMemHelper = true;
            }
        }
    }
    else
    {
        useMemHelper = true;
    }

    if (hasGCpointer && !dstIsOnStack)
    {
        useBarriers = true;
    }

#ifdef _TARGET_ARM_
    //
    // On ARM For COPYBLK & INITBLK we have special treatment for constant lengths.
    //
    if (!useMemHelper && !useBarriers)
    {
        bool     useLoop        = false;
        unsigned fullStoreCount = blkSize / TARGET_POINTER_SIZE;

        // A mask to use to force the predictor to choose low registers (to reduce code size)
        regMaskTP avoidReg = (RBM_R12 | RBM_LR);

        // Allow the src and dst to be used in place, unless we use a loop, in which
        // case we will need scratch registers as we will be writing to them.
        rpPredictReg srcAndDstPredict = PREDICT_REG;

        // Will we be using a loop to implement this INITBLK/COPYBLK?
        if ((isCopyBlk && (fullStoreCount >= 8)) || (isInitBlk && (fullStoreCount >= 16)))
        {
            useLoop          = true;
            avoidReg         = RBM_NONE;
            srcAndDstPredict = PREDICT_SCRATCH_REG;
        }

        if (tree->gtFlags & GTF_REVERSE_OPS)
        {
            regMask |= rpPredictTreeRegUse(srcAddrOrFill, srcAndDstPredict, lockedRegs,
                                           dstAddr->gtRsvdRegs | avoidReg | RBM_LASTUSE);
            regMask |= rpPredictTreeRegUse(dstAddr, srcAndDstPredict, lockedRegs | regMask, avoidReg);
        }
        else
        {
            regMask |= rpPredictTreeRegUse(dstAddr, srcAndDstPredict, lockedRegs,
                                           srcAddrOrFill->gtRsvdRegs | avoidReg | RBM_LASTUSE);
            regMask |= rpPredictTreeRegUse(srcAddrOrFill, srcAndDstPredict, lockedRegs | regMask, avoidReg);
        }

        // We need at least one scratch register for a copyBlk
        if (isCopyBlk)
        {
            // Pick a low register to reduce the code size
            regMask |= rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | regMask | avoidReg);
        }

        if (useLoop)
        {
            if (isCopyBlk)
            {
                // We need a second temp register for a copyBlk (our code gen is load two/store two)
                // Pick another low register to reduce the code size
                regMask |= rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | regMask | avoidReg);
            }

            // We need a loop index register
            regMask |= rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | regMask);
        }

        tree->gtUsedRegs = dstAddr->gtUsedRegs | srcAddrOrFill->gtUsedRegs | (regMaskSmall)regMask;

        return interferingRegs;
    }
#endif
    // What order should the Dest, Val/Src, and Size be calculated
    GenTreePtr opsPtr[3];
    regMaskTP  regsPtr[3];

#if defined(_TARGET_XARCH_)
    fgOrderBlockOps(tree, RBM_EDI, (isInitBlk) ? RBM_EAX : RBM_ESI, RBM_ECX, opsPtr, regsPtr);

    // We're going to use these, might as well make them available now

    codeGen->regSet.rsSetRegsModified(RBM_EDI | RBM_ECX);
    if (isCopyBlk)
        codeGen->regSet.rsSetRegsModified(RBM_ESI);

#elif defined(_TARGET_ARM_)

    if (useMemHelper)
    {
        // For all other cases that involve non-constants, we just call memcpy/memset
        // JIT helpers
        fgOrderBlockOps(tree, RBM_ARG_0, RBM_ARG_1, RBM_ARG_2, opsPtr, regsPtr);
        interferingRegs |= RBM_CALLEE_TRASH;
#ifdef DEBUG
        if (verbose)
            printf("Adding interference with RBM_CALLEE_TRASH for memcpy/memset\n");
#endif
    }
    else // useBarriers
    {
        assert(useBarriers);
        assert(isCopyBlk);

        fgOrderBlockOps(tree, RBM_ARG_0, RBM_ARG_1, REG_TMP_1, opsPtr, regsPtr);

        // For this case Codegen will call the CORINFO_HELP_ASSIGN_BYREF helper
        interferingRegs |= RBM_CALLEE_TRASH_NOGC;
#ifdef DEBUG
        if (verbose)
            printf("Adding interference with RBM_CALLEE_TRASH_NOGC for Byref WriteBarrier\n");
#endif
    }
#else // !_TARGET_X86_ && !_TARGET_ARM_
#error "Non-ARM or x86 _TARGET_ in RegPredict for INITBLK/COPYBLK"
#endif // !_TARGET_X86_ && !_TARGET_ARM_
    regMaskTP opsPtr2RsvdRegs = opsPtr[2] == nullptr ? RBM_NONE : opsPtr[2]->gtRsvdRegs;
    regMask |= rpPredictTreeRegUse(opsPtr[0], rpGetPredictForMask(regsPtr[0]), lockedRegs,
                                   opsPtr[1]->gtRsvdRegs | opsPtr2RsvdRegs | RBM_LASTUSE);
    regMask |= regsPtr[0];
    opsPtr[0]->gtUsedRegs |= regsPtr[0];
    rpRecordRegIntf(regsPtr[0], compCurLife DEBUGARG("movsd dest"));

    regMask |= rpPredictTreeRegUse(opsPtr[1], rpGetPredictForMask(regsPtr[1]), lockedRegs | regMask,
                                   opsPtr2RsvdRegs | RBM_LASTUSE);
    regMask |= regsPtr[1];
    opsPtr[1]->gtUsedRegs |= regsPtr[1];
    rpRecordRegIntf(regsPtr[1], compCurLife DEBUGARG("movsd src"));

    regMaskSmall opsPtr2UsedRegs = (regMaskSmall)regsPtr[2];
    if (opsPtr[2] == nullptr)
    {
        // If we have no "size" node, we will predict that regsPtr[2] will be used for the size.
        // Note that it is quite possible that no register is required, but this preserves
        // former behavior.
        regMask |= rpPredictRegPick(TYP_INT, rpGetPredictForMask(regsPtr[2]), lockedRegs | regMask);
        rpRecordRegIntf(regsPtr[2], compCurLife DEBUGARG("tmp use"));
    }
    else
    {
        regMask |= rpPredictTreeRegUse(opsPtr[2], rpGetPredictForMask(regsPtr[2]), lockedRegs | regMask, RBM_NONE);
        opsPtr[2]->gtUsedRegs |= opsPtr2UsedRegs;
    }
    regMask |= opsPtr2UsedRegs;

    tree->gtUsedRegs = opsPtr[0]->gtUsedRegs | opsPtr[1]->gtUsedRegs | opsPtr2UsedRegs | (regMaskSmall)regMask;
    return interferingRegs;
}

/*****************************************************************************
 *
 * Predict the internal temp physical register usage for a tree by setting tree->gtUsedRegs.
 * Returns a regMask with the internal temp physical register usage for this tree.
 *
 * Each of the switch labels in this function updates regMask and assigns tree->gtUsedRegs
 * to the set of scratch registers needed when evaluating the tree.
 * Generally tree->gtUsedRegs and the return value retMask are the same, except when the
 * parameter "lockedRegs" conflicts with the computed tree->gtUsedRegs, in which case we
 * predict additional internal temp physical registers to spill into.
 *
 *    tree       - is the child of a GT_IND node
 *    predictReg - what type of register does the tree need
 *    lockedRegs - are the registers which are currently held by a previously evaluated node.
 *                 Don't modify lockedRegs as it is used at the end to compute a spill mask.
 *    rsvdRegs   - registers which should not be allocated because they will
 *                 be needed to evaluate a node in the future
 *               - Also, if rsvdRegs has the RBM_LASTUSE bit set then
 *                 the rpLastUseVars set should be saved and restored
 *                 so that we don't add any new variables to rpLastUseVars.
 */

#pragma warning(disable : 4701)

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
regMaskTP Compiler::rpPredictTreeRegUse(GenTreePtr   tree,
                                        rpPredictReg predictReg,
                                        regMaskTP    lockedRegs,
                                        regMaskTP    rsvdRegs)
{
    regMaskTP    regMask = DUMMY_INIT(RBM_ILLEGAL);
    regMaskTP    op2Mask;
    regMaskTP    tmpMask;
    rpPredictReg op1PredictReg;
    rpPredictReg op2PredictReg;
    LclVarDsc*   varDsc = NULL;
    VARSET_TP    oldLastUseVars(VarSetOps::UninitVal());

    VARSET_TP varBits(VarSetOps::UninitVal());
    VARSET_TP lastUseVarBits(VarSetOps::MakeEmpty(this));

    bool      restoreLastUseVars = false;
    regMaskTP interferingRegs    = RBM_NONE;

#ifdef DEBUG
    // if (verbose) printf("rpPredictTreeRegUse() [%08x]\n", tree);
    noway_assert(tree);
    noway_assert(((RBM_ILLEGAL & RBM_ALLINT) == 0));
    noway_assert(RBM_ILLEGAL);
    noway_assert((lockedRegs & RBM_ILLEGAL) == 0);
    /* impossible values, to make sure that we set them */
    tree->gtUsedRegs = RBM_ILLEGAL;
#endif

    /* Figure out what kind of a node we have */

    genTreeOps oper = tree->OperGet();
    var_types  type = tree->TypeGet();
    unsigned   kind = tree->OperKind();

    // In the comma case, we care about whether this is "effectively" ADDR(IND(...))
    genTreeOps effectiveOper = tree->gtEffectiveVal()->OperGet();
    if ((predictReg == PREDICT_ADDR) && (effectiveOper != GT_IND))
        predictReg = PREDICT_NONE;
    else if (rpHasVarIndexForPredict(predictReg))
    {
        // The only place where predictReg is set to a var is in the PURE
        // assignment case where varIndex is the var being assigned to.
        // We need to check whether the variable is used between here and
        // its redefinition.
        unsigned varIndex = rpGetVarIndexForPredict(predictReg);
        unsigned lclNum   = lvaTrackedToVarNum[varIndex];
        bool     found    = false;
        for (GenTreePtr nextTree = tree->gtNext; nextTree != NULL && !found; nextTree = nextTree->gtNext)
        {
            if (nextTree->gtOper == GT_LCL_VAR && nextTree->gtLclVarCommon.gtLclNum == lclNum)
            {
                // Is this the pure assignment?
                if ((nextTree->gtFlags & GTF_VAR_DEF) == 0)
                {
                    predictReg = PREDICT_SCRATCH_REG;
                }
                found = true;
                break;
            }
        }
        assert(found);
    }

    if (rsvdRegs & RBM_LASTUSE)
    {
        restoreLastUseVars = true;
        VarSetOps::Assign(this, oldLastUseVars, rpLastUseVars);
        rsvdRegs &= ~RBM_LASTUSE;
    }

    /* Is this a constant or leaf node? */

    if (kind & (GTK_CONST | GTK_LEAF))
    {
        bool      lastUse   = false;
        regMaskTP enregMask = RBM_NONE;

        switch (oper)
        {
#ifdef _TARGET_ARM_
            case GT_CNS_DBL:
                // Codegen for floating point constants on the ARM is currently
                // movw/movt    rT1, <lo32 bits>
                // movw/movt    rT2, <hi32 bits>
                //  vmov.i2d    dT0, rT1,rT2
                //
                // For TYP_FLOAT one integer register is required
                //
                // These integer register(s) immediately die
                tmpMask = rpPredictRegPick(TYP_INT, PREDICT_REG, lockedRegs | rsvdRegs);
                if (type == TYP_DOUBLE)
                {
                    // For TYP_DOUBLE a second integer register is required
                    //
                    tmpMask |= rpPredictRegPick(TYP_INT, PREDICT_REG, lockedRegs | rsvdRegs | tmpMask);
                }

                // We also need a floating point register that we keep
                //
                if (predictReg == PREDICT_NONE)
                    predictReg = PREDICT_SCRATCH_REG;

                regMask          = rpPredictRegPick(type, predictReg, lockedRegs | rsvdRegs);
                tree->gtUsedRegs = regMask | tmpMask;
                goto RETURN_CHECK;
#endif

            case GT_CNS_INT:
            case GT_CNS_LNG:

                if (rpHasVarIndexForPredict(predictReg))
                {
                    unsigned tgtIndex = rpGetVarIndexForPredict(predictReg);
                    rpAsgVarNum       = tgtIndex;

                    // We don't need any register as we plan on writing to the rpAsgVarNum register
                    predictReg = PREDICT_NONE;

                    LclVarDsc* tgtVar   = lvaTable + lvaTrackedToVarNum[tgtIndex];
                    tgtVar->lvDependReg = true;

                    if (type == TYP_LONG)
                    {
                        assert(oper == GT_CNS_LNG);

                        if (tgtVar->lvOtherReg == REG_STK)
                        {
                            // Well we do need one register for a partially enregistered
                            type       = TYP_INT;
                            predictReg = PREDICT_SCRATCH_REG;
                        }
                    }
                }
                else
                {
#if !CPU_LOAD_STORE_ARCH
                    /* If the constant is a handle then it will need to have a relocation
                       applied to it.  It will need to be loaded into a register.
                       But never throw away an existing hint.
                       */
                    if (opts.compReloc && tree->IsCnsIntOrI() && tree->IsIconHandle())
#endif
                    {
                        if (predictReg == PREDICT_NONE)
                            predictReg = PREDICT_SCRATCH_REG;
                    }
                }
                break;

            case GT_NO_OP:
                break;

            case GT_CLS_VAR:
                if ((predictReg == PREDICT_NONE) && (genActualType(type) == TYP_INT) &&
                    (genTypeSize(type) < sizeof(int)))
                {
                    predictReg = PREDICT_SCRATCH_REG;
                }
#ifdef _TARGET_ARM_
                // Unaligned loads/stores for floating point values must first be loaded into integer register(s)
                //
                if ((tree->gtFlags & GTF_IND_UNALIGNED) && varTypeIsFloating(type))
                {
                    // These integer register(s) immediately die
                    tmpMask = rpPredictRegPick(TYP_INT, PREDICT_REG, lockedRegs | rsvdRegs);
                    // Two integer registers are required for a TYP_DOUBLE
                    if (type == TYP_DOUBLE)
                        tmpMask |= rpPredictRegPick(TYP_INT, PREDICT_REG, lockedRegs | rsvdRegs | tmpMask);
                }
                // We need a temp register in some cases of loads/stores to a class var
                if (predictReg == PREDICT_NONE)
                {
                    predictReg = PREDICT_SCRATCH_REG;
                }
#endif
                if (rpHasVarIndexForPredict(predictReg))
                {
                    unsigned tgtIndex = rpGetVarIndexForPredict(predictReg);
                    rpAsgVarNum       = tgtIndex;

                    // We don't need any register as we plan on writing to the rpAsgVarNum register
                    predictReg = PREDICT_NONE;

                    LclVarDsc* tgtVar   = lvaTable + lvaTrackedToVarNum[tgtIndex];
                    tgtVar->lvDependReg = true;

                    if (type == TYP_LONG)
                    {
                        if (tgtVar->lvOtherReg == REG_STK)
                        {
                            // Well we do need one register for a partially enregistered
                            type       = TYP_INT;
                            predictReg = PREDICT_SCRATCH_REG;
                        }
                    }
                }
                break;

            case GT_LCL_FLD:
#ifdef _TARGET_ARM_
                // Check for a misalignment on a Floating Point field
                //
                if (varTypeIsFloating(type))
                {
                    if ((tree->gtLclFld.gtLclOffs % emitTypeSize(tree->TypeGet())) != 0)
                    {
                        // These integer register(s) immediately die
                        tmpMask = rpPredictRegPick(TYP_INT, PREDICT_REG, lockedRegs | rsvdRegs);
                        // Two integer registers are required for a TYP_DOUBLE
                        if (type == TYP_DOUBLE)
                            tmpMask |= rpPredictRegPick(TYP_INT, PREDICT_REG, lockedRegs | rsvdRegs | tmpMask);
                    }
                }
#endif
                __fallthrough;

            case GT_LCL_VAR:
            case GT_REG_VAR:

                varDsc = lvaTable + tree->gtLclVarCommon.gtLclNum;

                VarSetOps::Assign(this, varBits, fgGetVarBits(tree));
                compUpdateLifeVar</*ForCodeGen*/ false>(tree, &lastUseVarBits);
                lastUse = !VarSetOps::IsEmpty(this, lastUseVarBits);

#if FEATURE_STACK_FP_X87
                // If it's a floating point var, there's nothing to do
                if (varTypeIsFloating(type))
                {
                    tree->gtUsedRegs = RBM_NONE;
                    regMask          = RBM_NONE;
                    goto RETURN_CHECK;
                }
#endif

                // If the variable is already a register variable, no need to go further.
                if (oper == GT_REG_VAR)
                    break;

                /* Apply the type of predictReg to the LCL_VAR */

                if (predictReg == PREDICT_REG)
                {
                PREDICT_REG_COMMON:
                    if (varDsc->lvRegNum == REG_STK)
                        break;

                    goto GRAB_COUNT;
                }
                else if (predictReg == PREDICT_SCRATCH_REG)
                {
                    noway_assert(predictReg == PREDICT_SCRATCH_REG);

                    /* Is this the last use of a local var?   */
                    if (lastUse)
                    {
                        if (VarSetOps::IsEmptyIntersection(this, rpUseInPlace, lastUseVarBits))
                            goto PREDICT_REG_COMMON;
                    }
                }
                else if (rpHasVarIndexForPredict(predictReg))
                {
                    /* Get the tracked local variable that has an lvVarIndex of tgtIndex1 */
                    {
                        unsigned   tgtIndex1 = rpGetVarIndexForPredict(predictReg);
                        LclVarDsc* tgtVar    = lvaTable + lvaTrackedToVarNum[tgtIndex1];
                        VarSetOps::MakeSingleton(this, tgtIndex1);

                        noway_assert(tgtVar->lvVarIndex == tgtIndex1);
                        noway_assert(tgtVar->lvRegNum != REG_STK); /* Must have been enregistered */
#ifndef _TARGET_AMD64_
                        // On amd64 we have the occasional spec-allowed implicit conversion from TYP_I_IMPL to TYP_INT
                        // so this assert is meaningless
                        noway_assert((type != TYP_LONG) || (tgtVar->TypeGet() == TYP_LONG));
#endif // !_TARGET_AMD64_

                        if (varDsc->lvTracked)
                        {
                            unsigned srcIndex;
                            srcIndex = varDsc->lvVarIndex;

                            // If this register has it's last use here then we will prefer
                            // to color to the same register as tgtVar.
                            if (lastUse)
                            {
                                /*
                                 *  Add an entry in the lvaVarPref graph to indicate
                                 *  that it would be worthwhile to color these two variables
                                 *  into the same physical register.
                                 *  This will help us avoid having an extra copy instruction
                                 */
                                VarSetOps::AddElemD(this, lvaVarPref[srcIndex], tgtIndex1);
                                VarSetOps::AddElemD(this, lvaVarPref[tgtIndex1], srcIndex);
                            }

                            // Add a variable interference from srcIndex to each of the last use variables
                            if (!VarSetOps::IsEmpty(this, rpLastUseVars))
                            {
                                rpRecordVarIntf(srcIndex, rpLastUseVars DEBUGARG("src reg conflict"));
                            }
                        }
                        rpAsgVarNum = tgtIndex1;

                        /* We will rely on the target enregistered variable from the GT_ASG */
                        varDsc = tgtVar;
                    }
                GRAB_COUNT:
                    unsigned grabCount;
                    grabCount = 0;

                    if (genIsValidFloatReg(varDsc->lvRegNum))
                    {
                        enregMask = genRegMaskFloat(varDsc->lvRegNum, varDsc->TypeGet());
                    }
                    else
                    {
                        enregMask = genRegMask(varDsc->lvRegNum);
                    }

#ifdef _TARGET_ARM_
                    if ((type == TYP_DOUBLE) && (varDsc->TypeGet() == TYP_FLOAT))
                    {
                        // We need to compute the intermediate value using a TYP_DOUBLE
                        // but we storing the result in a TYP_SINGLE enregistered variable
                        //
                        grabCount++;
                    }
                    else
#endif
                    {
                        /* We can't trust a prediction of rsvdRegs or lockedRegs sets */
                        if (enregMask & (rsvdRegs | lockedRegs))
                        {
                            grabCount++;
                        }
#ifndef _TARGET_64BIT_
                        if (type == TYP_LONG)
                        {
                            if (varDsc->lvOtherReg != REG_STK)
                            {
                                tmpMask = genRegMask(varDsc->lvOtherReg);
                                enregMask |= tmpMask;

                                /* We can't trust a prediction of rsvdRegs or lockedRegs sets */
                                if (tmpMask & (rsvdRegs | lockedRegs))
                                    grabCount++;
                            }
                            else // lvOtherReg == REG_STK
                            {
                                grabCount++;
                            }
                        }
#endif // _TARGET_64BIT_
                    }

                    varDsc->lvDependReg = true;

                    if (grabCount == 0)
                    {
                        /* Does not need a register */
                        predictReg = PREDICT_NONE;
                        // noway_assert(!VarSetOps::IsEmpty(this, varBits));
                        VarSetOps::UnionD(this, rpUseInPlace, varBits);
                    }
                    else // (grabCount > 0)
                    {
#ifndef _TARGET_64BIT_
                        /* For TYP_LONG and we only need one register then change the type to TYP_INT */
                        if ((type == TYP_LONG) && (grabCount == 1))
                        {
                            /* We will need to pick one register */
                            type = TYP_INT;
                            // noway_assert(!VarSetOps::IsEmpty(this, varBits));
                            VarSetOps::UnionD(this, rpUseInPlace, varBits);
                        }
                        noway_assert((type == TYP_DOUBLE) ||
                                     (grabCount == (genTypeSize(genActualType(type)) / REGSIZE_BYTES)));
#else  // !_TARGET_64BIT_
                        noway_assert(grabCount == 1);
#endif // !_TARGET_64BIT_
                    }
                }
                else if (type == TYP_STRUCT)
                {
#ifdef _TARGET_ARM_
                    // TODO-ARM-Bug?: Passing structs in registers on ARM hits an assert here when
                    //        predictReg is PREDICT_REG_R0 to PREDICT_REG_R3
                    //        As a workaround we just bash it to PREDICT_NONE here
                    //
                    if (predictReg != PREDICT_NONE)
                        predictReg = PREDICT_NONE;
#endif
                    // Currently predictReg is saying that we will not need any scratch registers
                    noway_assert(predictReg == PREDICT_NONE);

                    /* We may need to sign or zero extend a small type when pushing a struct */
                    if (varDsc->lvPromoted && !varDsc->lvAddrExposed)
                    {
                        for (unsigned varNum = varDsc->lvFieldLclStart;
                             varNum < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; varNum++)
                        {
                            LclVarDsc* fldVar = lvaTable + varNum;

                            if (fldVar->lvStackAligned())
                            {
                                // When we are stack aligned Codegen will just use
                                // a push instruction and thus doesn't need any register
                                // since we can push both a register or a stack frame location
                                continue;
                            }

                            if (varTypeIsByte(fldVar->TypeGet()))
                            {
                                // We will need to reserve one byteable register,
                                //
                                type       = TYP_BYTE;
                                predictReg = PREDICT_SCRATCH_REG;
#if CPU_HAS_BYTE_REGS
                                // It is best to enregister this fldVar in a byteable register
                                //
                                fldVar->addPrefReg(RBM_BYTE_REG_FLAG, this);
#endif
                            }
                            else if (varTypeIsShort(fldVar->TypeGet()))
                            {
                                bool isEnregistered = fldVar->lvTracked && (fldVar->lvRegNum != REG_STK);
                                // If fldVar is not enregistered then we will need a scratch register
                                //
                                if (!isEnregistered)
                                {
                                    // We will need either an int register or a byte register
                                    // If we are not requesting a byte register we will request an int register
                                    //
                                    if (type != TYP_BYTE)
                                        type   = TYP_INT;
                                    predictReg = PREDICT_SCRATCH_REG;
                                }
                            }
                        }
                    }
                }
                else
                {
                    regMaskTP preferReg = rpPredictRegMask(predictReg, type);
                    if (preferReg != 0)
                    {
                        if ((genTypeStSz(type) == 1) || (genCountBits(preferReg) <= genTypeStSz(type)))
                        {
                            varDsc->addPrefReg(preferReg, this);
                        }
                    }
                }
                break; /* end of case GT_LCL_VAR */

            case GT_JMP:
                tree->gtUsedRegs = RBM_NONE;
                regMask          = RBM_NONE;

#if defined(_TARGET_ARM_) && defined(PROFILING_SUPPORTED)
                // Mark the registers required to emit a tailcall profiler callback
                if (compIsProfilerHookNeeded())
                {
                    tree->gtUsedRegs |= RBM_PROFILER_JMP_USED;
                }
#endif
                goto RETURN_CHECK;

            default:
                break;
        } /* end of switch (oper) */

        /* If we don't need to evaluate to register, regmask is the empty set */
        /* Otherwise we grab a temp for the local variable                    */

        if (predictReg == PREDICT_NONE)
            regMask = RBM_NONE;
        else
        {
            regMask = rpPredictRegPick(type, predictReg, lockedRegs | rsvdRegs | enregMask);

            if ((oper == GT_LCL_VAR) && (tree->TypeGet() == TYP_STRUCT))
            {
                /* We need to sign or zero extend a small type when pushing a struct */
                noway_assert((type == TYP_INT) || (type == TYP_BYTE));

                varDsc = lvaTable + tree->gtLclVarCommon.gtLclNum;
                noway_assert(varDsc->lvPromoted && !varDsc->lvAddrExposed);

                for (unsigned varNum = varDsc->lvFieldLclStart; varNum < varDsc->lvFieldLclStart + varDsc->lvFieldCnt;
                     varNum++)
                {
                    LclVarDsc* fldVar = lvaTable + varNum;
                    if (fldVar->lvTracked)
                    {
                        VARSET_TP fldBit(VarSetOps::MakeSingleton(this, fldVar->lvVarIndex));
                        rpRecordRegIntf(regMask, fldBit DEBUGARG(
                                                     "need scratch register when pushing a small field of a struct"));
                    }
                }
            }
        }

        /* Update the set of lastUse variables that we encountered so far */
        if (lastUse)
        {
            VarSetOps::UnionD(this, rpLastUseVars, lastUseVarBits);
            VARSET_TP varAsSet(VarSetOps::MakeCopy(this, lastUseVarBits));

            /*
             *  Add interference from any previously locked temps into this last use variable.
             */
            if (lockedRegs)
            {
                rpRecordRegIntf(lockedRegs, varAsSet DEBUGARG("last use Predict lockedRegs"));
            }
            /*
             *  Add interference from any reserved temps into this last use variable.
             */
            if (rsvdRegs)
            {
                rpRecordRegIntf(rsvdRegs, varAsSet DEBUGARG("last use Predict rsvdRegs"));
            }
            /*
             *  For partially enregistered longs add an interference with the
             *  register return by rpPredictRegPick
             */
            if ((type == TYP_INT) && (tree->TypeGet() == TYP_LONG))
            {
                rpRecordRegIntf(regMask, varAsSet DEBUGARG("last use with partial enreg"));
            }
        }

        tree->gtUsedRegs = (regMaskSmall)regMask;
        goto RETURN_CHECK;
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_SMPOP)
    {
        GenTreePtr op1 = tree->gtOp.gtOp1;
        GenTreePtr op2 = tree->gtGetOp2IfPresent();

        GenTreePtr opsPtr[3];
        regMaskTP  regsPtr[3];

        VARSET_TP startAsgUseInPlaceVars(VarSetOps::UninitVal());

        switch (oper)
        {
            case GT_ASG:

                /* Is the value being assigned into a LCL_VAR? */
                if (op1->gtOper == GT_LCL_VAR)
                {
                    varDsc = lvaTable + op1->gtLclVarCommon.gtLclNum;

                    /* Are we assigning a LCL_VAR the result of a call? */
                    if (op2->gtOper == GT_CALL)
                    {
                        /* Set a preferred register for the LCL_VAR */
                        if (isRegPairType(varDsc->TypeGet()))
                            varDsc->addPrefReg(RBM_LNGRET, this);
                        else if (!varTypeIsFloating(varDsc->TypeGet()))
                            varDsc->addPrefReg(RBM_INTRET, this);
#ifdef _TARGET_AMD64_
                        else
                            varDsc->addPrefReg(RBM_FLOATRET, this);
#endif
                        /*
                         *  When assigning the result of a call we don't
                         *  bother trying to target the right side of the
                         *  assignment, since we have a fixed calling convention.
                         */
                    }
                    else if (varDsc->lvTracked)
                    {
                        // We interfere with uses in place
                        if (!VarSetOps::IsEmpty(this, rpUseInPlace))
                        {
                            rpRecordVarIntf(varDsc->lvVarIndex, rpUseInPlace DEBUGARG("Assign UseInPlace conflict"));
                        }

                        // Did we predict that this local will be fully enregistered?
                        // and the assignment type is the same as the expression type?
                        // and it is dead on the right side of the assignment?
                        // and we current have no other rpAsgVarNum active?
                        //
                        if ((varDsc->lvRegNum != REG_STK) && ((type != TYP_LONG) || (varDsc->lvOtherReg != REG_STK)) &&
                            (type == op2->TypeGet()) && (op1->gtFlags & GTF_VAR_DEF) && (rpAsgVarNum == -1))
                        {
                            //
                            //  Yes, we should try to target the right side (op2) of this
                            //  assignment into the (enregistered) tracked variable.
                            //

                            op1PredictReg = PREDICT_NONE; /* really PREDICT_REG, but we've already done the check */
                            op2PredictReg = rpGetPredictForVarIndex(varDsc->lvVarIndex);

                            // Remember that this is a new use in place

                            // We've added "new UseInPlace"; remove from the global set.
                            VarSetOps::RemoveElemD(this, rpUseInPlace, varDsc->lvVarIndex);

                            //  Note that later when we walk down to the leaf node for op2
                            //  if we decide to actually use the register for the 'varDsc'
                            //  to enregister the operand, the we will set rpAsgVarNum to
                            //  varDsc->lvVarIndex, by extracting this value using
                            //  rpGetVarIndexForPredict()
                            //
                            //  Also we reset rpAsgVarNum back to -1 after we have finished
                            //  predicting the current GT_ASG node
                            //
                            goto ASG_COMMON;
                        }
                    }
                }
                else if (tree->OperIsBlkOp())
                {
                    interferingRegs |= rpPredictBlkAsgRegUse(tree, predictReg, lockedRegs, rsvdRegs);
                    regMask = 0;
                    goto RETURN_CHECK;
                }
                __fallthrough;

            case GT_CHS:

            case GT_ASG_OR:
            case GT_ASG_XOR:
            case GT_ASG_AND:
            case GT_ASG_SUB:
            case GT_ASG_ADD:
            case GT_ASG_MUL:
            case GT_ASG_DIV:
            case GT_ASG_UDIV:

                /* We can't use "reg <op>= addr" for TYP_LONG or if op2 is a short type */
                if ((type != TYP_LONG) && !varTypeIsSmall(op2->gtType))
                {
                    /* Is the value being assigned into an enregistered LCL_VAR? */
                    /* For debug code we only allow a simple op2 to be assigned */
                    if ((op1->gtOper == GT_LCL_VAR) && (!opts.compDbgCode || rpCanAsgOperWithoutReg(op2, false)))
                    {
                        varDsc = lvaTable + op1->gtLclVarCommon.gtLclNum;
                        /* Did we predict that this local will be enregistered? */
                        if (varDsc->lvRegNum != REG_STK)
                        {
                            /* Yes, we can use "reg <op>= addr" */

                            op1PredictReg = PREDICT_NONE; /* really PREDICT_REG, but we've already done the check */
                            op2PredictReg = PREDICT_NONE;

                            goto ASG_COMMON;
                        }
                    }
                }

#if CPU_LOAD_STORE_ARCH
                if (oper != GT_ASG)
                {
                    op1PredictReg = PREDICT_REG;
                    op2PredictReg = PREDICT_REG;
                }
                else
#endif
                {
                    /*
                     *  Otherwise, initialize the normal forcing of operands:
                     *   "addr <op>= reg"
                     */
                    op1PredictReg = PREDICT_ADDR;
                    op2PredictReg = PREDICT_REG;
                }

            ASG_COMMON:

#if !CPU_LOAD_STORE_ARCH
                if (op2PredictReg != PREDICT_NONE)
                {
                    /* Is the value being assigned a simple one? */
                    if (rpCanAsgOperWithoutReg(op2, false))
                        op2PredictReg = PREDICT_NONE;
                }
#endif

                bool simpleAssignment;
                simpleAssignment = false;

                if ((oper == GT_ASG) && (op1->gtOper == GT_LCL_VAR))
                {
                    // Add a variable interference from the assign target
                    // to each of the last use variables
                    if (!VarSetOps::IsEmpty(this, rpLastUseVars))
                    {
                        varDsc = lvaTable + op1->gtLclVarCommon.gtLclNum;

                        if (varDsc->lvTracked)
                        {
                            unsigned varIndex = varDsc->lvVarIndex;

                            rpRecordVarIntf(varIndex, rpLastUseVars DEBUGARG("Assign conflict"));
                        }
                    }

                    /*  Record whether this tree is a simple assignment to a local */

                    simpleAssignment = ((type != TYP_LONG) || !opts.compDbgCode);
                }

                bool requireByteReg;
                requireByteReg = false;

#if CPU_HAS_BYTE_REGS
                /* Byte-assignments need the byte registers, unless op1 is an enregistered local */

                if (varTypeIsByte(type) &&
                    ((op1->gtOper != GT_LCL_VAR) || (lvaTable[op1->gtLclVarCommon.gtLclNum].lvRegNum == REG_STK)))

                {
                    // Byte-assignments typically need a byte register
                    requireByteReg = true;

                    if (op1->gtOper == GT_LCL_VAR)
                    {
                        varDsc = lvaTable + op1->gtLclVar.gtLclNum;

                        // Did we predict that this local will be enregistered?
                        if (varDsc->lvTracked && (varDsc->lvRegNum != REG_STK) && (oper != GT_CHS))
                        {
                            // We don't require a byte register when op1 is an enregistered local */
                            requireByteReg = false;
                        }

                        // Is op1 part of an Assign-Op or is the RHS a simple memory indirection?
                        if ((oper != GT_ASG) || (op2->gtOper == GT_IND) || (op2->gtOper == GT_CLS_VAR))
                        {
                            // We should try to put op1 in an byte register
                            varDsc->addPrefReg(RBM_BYTE_REG_FLAG, this);
                        }
                    }
                }
#endif

                VarSetOps::Assign(this, startAsgUseInPlaceVars, rpUseInPlace);

                bool isWriteBarrierAsgNode;
                isWriteBarrierAsgNode = codeGen->gcInfo.gcIsWriteBarrierAsgNode(tree);
#ifdef DEBUG
                GCInfo::WriteBarrierForm wbf;
                if (isWriteBarrierAsgNode)
                    wbf = codeGen->gcInfo.gcIsWriteBarrierCandidate(tree->gtOp.gtOp1, tree->gtOp.gtOp2);
                else
                    wbf = GCInfo::WBF_NoBarrier;
#endif // DEBUG

                regMaskTP wbaLockedRegs;
                wbaLockedRegs = lockedRegs;
                if (isWriteBarrierAsgNode)
                {
#if defined(_TARGET_X86_) && NOGC_WRITE_BARRIERS
#ifdef DEBUG
                    if (wbf != GCInfo::WBF_NoBarrier_CheckNotHeapInDebug)
                    {
#endif // DEBUG
                        wbaLockedRegs |= RBM_WRITE_BARRIER;
                        op1->gtRsvdRegs |= RBM_WRITE_BARRIER; // This will steer op2 away from REG_WRITE_BARRIER
                        assert(REG_WRITE_BARRIER == REG_EDX);
                        op1PredictReg = PREDICT_REG_EDX;
#ifdef DEBUG
                    }
                    else
#endif // DEBUG
#endif // defined(_TARGET_X86_) && NOGC_WRITE_BARRIERS

#if defined(DEBUG) || !(defined(_TARGET_X86_) && NOGC_WRITE_BARRIERS)
                    {
#ifdef _TARGET_X86_
                        op1PredictReg = PREDICT_REG_ECX;
                        op2PredictReg = PREDICT_REG_EDX;
#elif defined(_TARGET_ARM_)
                        op1PredictReg = PREDICT_REG_R0;
                        op2PredictReg = PREDICT_REG_R1;

                        // This is my best guess as to what the previous code meant by checking "gtRngChkLen() == NULL".
                        if ((op1->OperGet() == GT_IND) && (op1->gtOp.gtOp1->OperGet() != GT_ARR_BOUNDS_CHECK))
                        {
                            op1 = op1->gtOp.gtOp1;
                        }
#else // !_TARGET_X86_ && !_TARGET_ARM_
#error "Non-ARM or x86 _TARGET_ in RegPredict for WriteBarrierAsg"
#endif
                    }
#endif
                }

                /*  Are we supposed to evaluate RHS first? */

                if (tree->gtFlags & GTF_REVERSE_OPS)
                {
                    op2Mask = rpPredictTreeRegUse(op2, op2PredictReg, lockedRegs, rsvdRegs | op1->gtRsvdRegs);

#if CPU_HAS_BYTE_REGS
                    // Should we insure that op2 gets evaluated into a byte register?
                    if (requireByteReg && ((op2Mask & RBM_BYTE_REGS) == 0))
                    {
                        // We need to grab a byte-able register, (i.e. EAX, EDX, ECX, EBX)
                        // and we can't select one that is already reserved (i.e. lockedRegs)
                        //
                        op2Mask |= rpPredictRegPick(type, PREDICT_SCRATCH_REG, (lockedRegs | RBM_NON_BYTE_REGS));
                        op2->gtUsedRegs |= op2Mask;

                        // No longer a simple assignment because we're using extra registers and might
                        // have interference between op1 and op2.  See DevDiv #136681
                        simpleAssignment = false;
                    }
#endif
                    /*
                     *  For a simple assignment we don't want the op2Mask to be
                     *  marked as interferring with the LCL_VAR, since it is likely
                     *  that we will want to enregister the LCL_VAR in exactly
                     *  the register that is used to compute op2
                     */
                    tmpMask = lockedRegs;

                    if (!simpleAssignment)
                        tmpMask |= op2Mask;

                    regMask = rpPredictTreeRegUse(op1, op1PredictReg, tmpMask, RBM_NONE);

                    // Did we relax the register prediction for op1 and op2 above ?
                    // - because we are depending upon op1 being enregistered
                    //
                    if ((op1PredictReg == PREDICT_NONE) &&
                        ((op2PredictReg == PREDICT_NONE) || rpHasVarIndexForPredict(op2PredictReg)))
                    {
                        /* We must be assigning into an enregistered LCL_VAR */
                        noway_assert(op1->gtOper == GT_LCL_VAR);
                        varDsc = lvaTable + op1->gtLclVar.gtLclNum;
                        noway_assert(varDsc->lvRegNum != REG_STK);

                        /* We need to set lvDependReg, in case we lose the enregistration of op1 */
                        varDsc->lvDependReg = true;
                    }
                }
                else
                {
                    // For the case of simpleAssignments op2 should always be evaluated first
                    noway_assert(!simpleAssignment);

                    regMask = rpPredictTreeRegUse(op1, op1PredictReg, lockedRegs, rsvdRegs | op2->gtRsvdRegs);
                    if (isWriteBarrierAsgNode)
                    {
                        wbaLockedRegs |= op1->gtUsedRegs;
                    }
                    op2Mask = rpPredictTreeRegUse(op2, op2PredictReg, wbaLockedRegs | regMask, RBM_NONE);

#if CPU_HAS_BYTE_REGS
                    // Should we insure that op2 gets evaluated into a byte register?
                    if (requireByteReg && ((op2Mask & RBM_BYTE_REGS) == 0))
                    {
                        // We need to grab a byte-able register, (i.e. EAX, EDX, ECX, EBX)
                        // and we can't select one that is already reserved (i.e. lockedRegs or regMask)
                        //
                        op2Mask |=
                            rpPredictRegPick(type, PREDICT_SCRATCH_REG, (lockedRegs | regMask | RBM_NON_BYTE_REGS));
                        op2->gtUsedRegs |= op2Mask;
                    }
#endif
                }

                if (rpHasVarIndexForPredict(op2PredictReg))
                {
                    rpAsgVarNum = -1;
                }

                if (isWriteBarrierAsgNode)
                {
#if NOGC_WRITE_BARRIERS
#ifdef DEBUG
                    if (wbf != GCInfo::WBF_NoBarrier_CheckNotHeapInDebug)
                    {
#endif // DEBUG

                        /* Steer computation away from REG_WRITE_BARRIER as the pointer is
                           passed to the write-barrier call in REG_WRITE_BARRIER */

                        regMask = op2Mask;

                        if (op1->gtOper == GT_IND)
                        {
                            GenTreePtr rv1, rv2;
                            unsigned   mul, cns;
                            bool       rev;

                            /* Special handling of indirect assigns for write barrier */

                            bool yes = codeGen->genCreateAddrMode(op1->gtOp.gtOp1, -1, true, RBM_NONE, &rev, &rv1, &rv2,
                                                                  &mul, &cns);

                            /* Check address mode for enregisterable locals */

                            if (yes)
                            {
                                if (rv1 != NULL && rv1->gtOper == GT_LCL_VAR)
                                {
                                    rpPredictRefAssign(rv1->gtLclVarCommon.gtLclNum);
                                }
                                if (rv2 != NULL && rv2->gtOper == GT_LCL_VAR)
                                {
                                    rpPredictRefAssign(rv2->gtLclVarCommon.gtLclNum);
                                }
                            }
                        }

                        if (op2->gtOper == GT_LCL_VAR)
                        {
                            rpPredictRefAssign(op2->gtLclVarCommon.gtLclNum);
                        }

                        // Add a register interference for REG_WRITE_BARRIER to each of the last use variables
                        if (!VarSetOps::IsEmpty(this, rpLastUseVars))
                        {
                            rpRecordRegIntf(RBM_WRITE_BARRIER,
                                            rpLastUseVars DEBUGARG("WriteBarrier and rpLastUseVars conflict"));
                        }
                        tree->gtUsedRegs |= RBM_WRITE_BARRIER;
#ifdef DEBUG
                    }
                    else
#endif // DEBUG
#endif // NOGC_WRITE_BARRIERS

#if defined(DEBUG) || !NOGC_WRITE_BARRIERS
                    {
#ifdef _TARGET_ARM_
#ifdef DEBUG
                        if (verbose)
                            printf("Adding interference with RBM_CALLEE_TRASH_NOGC for NoGC WriteBarrierAsg\n");
#endif
                        //
                        // For the ARM target we have an optimized JIT Helper
                        // that only trashes a subset of the callee saved registers
                        //

                        // NOTE: Adding it to the gtUsedRegs will cause the interference to
                        // be added appropriately

                        // the RBM_CALLEE_TRASH_NOGC set is killed.  We will record this in interferingRegs
                        // instead of gtUsedRegs, because the latter will be modified later, but we need
                        // to remember to add the interference.

                        interferingRegs |= RBM_CALLEE_TRASH_NOGC;

                        op1->gtUsedRegs |= RBM_R0;
                        op2->gtUsedRegs |= RBM_R1;
#else // _TARGET_ARM_

#ifdef DEBUG
                        if (verbose)
                            printf("Adding interference with RBM_CALLEE_TRASH for NoGC WriteBarrierAsg\n");
#endif
                        // We have to call a normal JIT helper to perform the Write Barrier Assignment
                        // It will trash the callee saved registers

                        tree->gtUsedRegs |= RBM_CALLEE_TRASH;
#endif // _TARGET_ARM_
                    }
#endif // defined(DEBUG) || !NOGC_WRITE_BARRIERS
                }

                if (simpleAssignment)
                {
                    /*
                     *  Consider a simple assignment to a local:
                     *
                     *   lcl = expr;
                     *
                     *  Since the "=" node is visited after the variable
                     *  is marked live (assuming it's live after the
                     *  assignment), we don't want to use the register
                     *  use mask of the "=" node but rather that of the
                     *  variable itself.
                     */
                    tree->gtUsedRegs = op1->gtUsedRegs;
                }
                else
                {
                    tree->gtUsedRegs = op1->gtUsedRegs | op2->gtUsedRegs;
                }
                VarSetOps::Assign(this, rpUseInPlace, startAsgUseInPlaceVars);
                goto RETURN_CHECK;

            case GT_ASG_LSH:
            case GT_ASG_RSH:
            case GT_ASG_RSZ:
                /* assigning shift operators */

                noway_assert(type != TYP_LONG);

#if CPU_LOAD_STORE_ARCH
                predictReg = PREDICT_ADDR;
#else
                predictReg = PREDICT_NONE;
#endif

                /* shift count is handled same as ordinary shift */
                goto HANDLE_SHIFT_COUNT;

            case GT_ADDR:
                regMask = rpPredictTreeRegUse(op1, PREDICT_ADDR, lockedRegs, RBM_LASTUSE);

                if ((regMask == RBM_NONE) && (predictReg >= PREDICT_REG))
                {
                    // We need a scratch register for the LEA instruction
                    regMask = rpPredictRegPick(TYP_INT, predictReg, lockedRegs | rsvdRegs);
                }

                tree->gtUsedRegs = op1->gtUsedRegs | (regMaskSmall)regMask;
                goto RETURN_CHECK;

            case GT_CAST:

                /* Cannot cast to VOID */
                noway_assert(type != TYP_VOID);

                /* cast to long is special */
                if (type == TYP_LONG && op1->gtType <= TYP_INT)
                {
                    noway_assert(tree->gtCast.gtCastType == TYP_LONG || tree->gtCast.gtCastType == TYP_ULONG);
#if CPU_LONG_USES_REGPAIR
                    rpPredictReg predictRegHi = PREDICT_SCRATCH_REG;

                    if (rpHasVarIndexForPredict(predictReg))
                    {
                        unsigned tgtIndex = rpGetVarIndexForPredict(predictReg);
                        rpAsgVarNum       = tgtIndex;

                        // We don't need any register as we plan on writing to the rpAsgVarNum register
                        predictReg = PREDICT_NONE;

                        LclVarDsc* tgtVar   = lvaTable + lvaTrackedToVarNum[tgtIndex];
                        tgtVar->lvDependReg = true;

                        if (tgtVar->lvOtherReg != REG_STK)
                        {
                            predictRegHi = PREDICT_NONE;
                        }
                    }
                    else
#endif
                        if (predictReg == PREDICT_NONE)
                    {
                        predictReg = PREDICT_SCRATCH_REG;
                    }
#ifdef _TARGET_ARM_
                    // If we are widening an int into a long using a targeted register pair we
                    // should retarget so that the low part get loaded into the appropriate register
                    else if (predictReg == PREDICT_PAIR_R0R1)
                    {
                        predictReg   = PREDICT_REG_R0;
                        predictRegHi = PREDICT_REG_R1;
                    }
                    else if (predictReg == PREDICT_PAIR_R2R3)
                    {
                        predictReg   = PREDICT_REG_R2;
                        predictRegHi = PREDICT_REG_R3;
                    }
#endif
#ifdef _TARGET_X86_
                    // If we are widening an int into a long using a targeted register pair we
                    // should retarget so that the low part get loaded into the appropriate register
                    else if (predictReg == PREDICT_PAIR_EAXEDX)
                    {
                        predictReg   = PREDICT_REG_EAX;
                        predictRegHi = PREDICT_REG_EDX;
                    }
                    else if (predictReg == PREDICT_PAIR_ECXEBX)
                    {
                        predictReg   = PREDICT_REG_ECX;
                        predictRegHi = PREDICT_REG_EBX;
                    }
#endif

                    regMask = rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs);

#if CPU_LONG_USES_REGPAIR
                    if (predictRegHi != PREDICT_NONE)
                    {
                        // Now get one more reg for the upper part
                        regMask |= rpPredictRegPick(TYP_INT, predictRegHi, lockedRegs | rsvdRegs | regMask);
                    }
#endif
                    tree->gtUsedRegs = op1->gtUsedRegs | (regMaskSmall)regMask;
                    goto RETURN_CHECK;
                }

                /* cast from long is special - it frees a register */
                if (type <= TYP_INT // nice.  this presumably is intended to mean "signed int and shorter types"
                    && op1->gtType == TYP_LONG)
                {
                    if ((predictReg == PREDICT_NONE) || rpHasVarIndexForPredict(predictReg))
                        predictReg = PREDICT_REG;

                    regMask = rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs);

                    // If we have 2 or more regs, free one of them
                    if (!genMaxOneBit(regMask))
                    {
                        /* Clear the 2nd lowest bit in regMask */
                        /* First set tmpMask to the lowest bit in regMask */
                        tmpMask = genFindLowestBit(regMask);
                        /* Next find the second lowest bit in regMask */
                        tmpMask = genFindLowestBit(regMask & ~tmpMask);
                        /* Clear this bit from regmask */
                        regMask &= ~tmpMask;
                    }
                    tree->gtUsedRegs = op1->gtUsedRegs;
                    goto RETURN_CHECK;
                }

#if CPU_HAS_BYTE_REGS
                /* cast from signed-byte is special - it uses byteable registers */
                if (type == TYP_INT)
                {
                    var_types smallType;

                    if (genTypeSize(tree->gtCast.CastOp()->TypeGet()) < genTypeSize(tree->gtCast.gtCastType))
                        smallType = tree->gtCast.CastOp()->TypeGet();
                    else
                        smallType = tree->gtCast.gtCastType;

                    if (smallType == TYP_BYTE)
                    {
                        regMask = rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs);

                        if ((regMask & RBM_BYTE_REGS) == 0)
                            regMask = rpPredictRegPick(type, PREDICT_SCRATCH_REG, RBM_NON_BYTE_REGS);

                        tree->gtUsedRegs = (regMaskSmall)regMask;
                        goto RETURN_CHECK;
                    }
                }
#endif

#if FEATURE_STACK_FP_X87
                /* cast to float/double is special */
                if (varTypeIsFloating(type))
                {
                    switch (op1->TypeGet())
                    {
                        /* uses fild, so don't need to be loaded to reg */
                        case TYP_INT:
                        case TYP_LONG:
                            rpPredictTreeRegUse(op1, PREDICT_NONE, lockedRegs, rsvdRegs);
                            tree->gtUsedRegs = op1->gtUsedRegs;
                            regMask          = 0;
                            goto RETURN_CHECK;
                        default:
                            break;
                    }
                }

                /* Casting from integral type to floating type is special */
                if (!varTypeIsFloating(type) && varTypeIsFloating(op1->TypeGet()))
                {
                    if (opts.compCanUseSSE2)
                    {
                        // predict for SSE2 based casting
                        if (predictReg <= PREDICT_REG)
                            predictReg = PREDICT_SCRATCH_REG;
                        regMask        = rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs);

                        // Get one more int reg to hold cast result
                        regMask |= rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | rsvdRegs | regMask);
                        tree->gtUsedRegs = op1->gtUsedRegs | (regMaskSmall)regMask;
                        goto RETURN_CHECK;
                    }
                }
#endif

#if FEATURE_FP_REGALLOC
                // Are we casting between int to float or float to int
                // Fix 388428 ARM JitStress WP7
                if (varTypeIsFloating(type) != varTypeIsFloating(op1->TypeGet()))
                {
                    // op1 needs to go into a register
                    regMask = rpPredictTreeRegUse(op1, PREDICT_REG, lockedRegs, rsvdRegs);

#ifdef _TARGET_ARM_
                    if (varTypeIsFloating(op1->TypeGet()))
                    {
                        // We also need a fp scratch register for the convert operation
                        regMask |= rpPredictRegPick((genTypeStSz(type) == 1) ? TYP_FLOAT : TYP_DOUBLE,
                                                    PREDICT_SCRATCH_REG, regMask | lockedRegs | rsvdRegs);
                    }
#endif
                    // We also need a register to hold the result
                    regMask |= rpPredictRegPick(type, PREDICT_SCRATCH_REG, regMask | lockedRegs | rsvdRegs);
                    tree->gtUsedRegs = op1->gtUsedRegs | (regMaskSmall)regMask;
                    goto RETURN_CHECK;
                }
#endif

                /* otherwise must load op1 into a register */
                goto GENERIC_UNARY;

            case GT_INTRINSIC:

#ifdef _TARGET_XARCH_
                if (tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Round && tree->TypeGet() == TYP_INT)
                {
                    // This is a special case to handle the following
                    // optimization: conv.i4(round.d(d)) -> round.i(d)
                    // if flowgraph 3186

                    if (predictReg <= PREDICT_REG)
                        predictReg = PREDICT_SCRATCH_REG;

                    rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs);

                    regMask = rpPredictRegPick(TYP_INT, predictReg, lockedRegs | rsvdRegs);

                    tree->gtUsedRegs = op1->gtUsedRegs | (regMaskSmall)regMask;
                    goto RETURN_CHECK;
                }
#endif
                __fallthrough;

            case GT_NEG:
#ifdef _TARGET_ARM_
                if (tree->TypeGet() == TYP_LONG)
                {
                    // On ARM this consumes an extra register for the '0' value
                    if (predictReg <= PREDICT_REG)
                        predictReg = PREDICT_SCRATCH_REG;

                    regMaskTP op1Mask = rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs);

                    regMask = rpPredictRegPick(TYP_INT, predictReg, lockedRegs | op1Mask | rsvdRegs);

                    tree->gtUsedRegs = op1->gtUsedRegs | (regMaskSmall)regMask;
                    goto RETURN_CHECK;
                }
#endif // _TARGET_ARM_

                __fallthrough;

            case GT_NOT:
            // these unary operators will write new values
            // and thus will need a scratch register
            GENERIC_UNARY:
                /* generic unary operators */

                if (predictReg <= PREDICT_REG)
                    predictReg = PREDICT_SCRATCH_REG;

                __fallthrough;

            case GT_NOP:
                // these unary operators do not write new values
                // and thus won't need a scratch register
                CLANG_FORMAT_COMMENT_ANCHOR;

#if OPT_BOOL_OPS
                if (!op1)
                {
                    tree->gtUsedRegs = 0;
                    regMask          = 0;
                    goto RETURN_CHECK;
                }
#endif
                regMask          = rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs);
                tree->gtUsedRegs = op1->gtUsedRegs;
                goto RETURN_CHECK;

            case GT_IND:
            case GT_NULLCHECK: // At this point, nullcheck is just like an IND...
            {
                bool      intoReg = true;
                VARSET_TP startIndUseInPlaceVars(VarSetOps::MakeCopy(this, rpUseInPlace));

                if (fgIsIndirOfAddrOfLocal(tree) != NULL)
                {
                    compUpdateLifeVar</*ForCodeGen*/ false>(tree);
                }

                if (predictReg == PREDICT_ADDR)
                {
                    intoReg = false;
                }
                else if (predictReg == PREDICT_NONE)
                {
                    if (type != TYP_LONG)
                    {
                        intoReg = false;
                    }
                    else
                    {
                        predictReg = PREDICT_REG;
                    }
                }

                /* forcing to register? */
                if (intoReg && (type != TYP_LONG))
                {
                    rsvdRegs |= RBM_LASTUSE;
                }

                GenTreePtr lenCSE;
                lenCSE = NULL;

                /* check for address mode */
                regMask = rpPredictAddressMode(op1, type, lockedRegs, rsvdRegs, lenCSE);
                tmpMask = RBM_NONE;

#if CPU_LOAD_STORE_ARCH
                // We may need a scratch register for loading a long
                if (type == TYP_LONG)
                {
                    /* This scratch register immediately dies */
                    tmpMask = rpPredictRegPick(TYP_BYREF, PREDICT_REG, op1->gtUsedRegs | lockedRegs | rsvdRegs);
                }
#endif // CPU_LOAD_STORE_ARCH

#ifdef _TARGET_ARM_
                // Unaligned loads/stores for floating point values must first be loaded into integer register(s)
                //
                if ((tree->gtFlags & GTF_IND_UNALIGNED) && varTypeIsFloating(type))
                {
                    /* These integer register(s) immediately die */
                    tmpMask = rpPredictRegPick(TYP_INT, PREDICT_REG, op1->gtUsedRegs | lockedRegs | rsvdRegs);
                    // Two integer registers are required for a TYP_DOUBLE
                    if (type == TYP_DOUBLE)
                        tmpMask |=
                            rpPredictRegPick(TYP_INT, PREDICT_REG, op1->gtUsedRegs | lockedRegs | rsvdRegs | tmpMask);
                }
#endif

                /* forcing to register? */
                if (intoReg)
                {
                    regMaskTP lockedMask = lockedRegs | rsvdRegs;
                    tmpMask |= regMask;

                    // We will compute a new regMask that holds the register(s)
                    // that we will load the indirection into.
                    //
                    CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef _TARGET_64BIT_
                    if (type == TYP_LONG)
                    {
                        // We need to use multiple load instructions here:
                        // For the first register we can not choose
                        // any registers that are being used in place or
                        // any register in the current regMask
                        //
                        regMask = rpPredictRegPick(TYP_INT, predictReg, regMask | lockedMask);

                        // For the second register we can choose a register that was
                        // used in place or any register in the old now overwritten regMask
                        // but not the same register that we picked above in 'regMask'
                        //
                        VarSetOps::Assign(this, rpUseInPlace, startIndUseInPlaceVars);
                        regMask |= rpPredictRegPick(TYP_INT, predictReg, regMask | lockedMask);
                    }
                    else
#endif
                    {
                        // We will use one load instruction here:
                        // The load target register can be a register that was used in place
                        // or one of the register from the orginal regMask.
                        //
                        VarSetOps::Assign(this, rpUseInPlace, startIndUseInPlaceVars);
                        regMask = rpPredictRegPick(type, predictReg, lockedMask);
                    }
                }
                else if (predictReg != PREDICT_ADDR)
                {
                    /* Unless the caller specified PREDICT_ADDR   */
                    /* we don't return the temp registers used    */
                    /* to form the address                        */
                    regMask = RBM_NONE;
                }
            }

                tree->gtUsedRegs = (regMaskSmall)(regMask | tmpMask);

                goto RETURN_CHECK;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:

#ifdef _TARGET_X86_
                /* Floating point comparison uses EAX for flags */
                if (varTypeIsFloating(op1->TypeGet()))
                {
                    regMask = RBM_EAX;
                }
                else
#endif
                    if (!(tree->gtFlags & GTF_RELOP_JMP_USED))
                {
                    // Some comparisons are converted to ?:
                    noway_assert(!fgMorphRelopToQmark(op1));

                    if (predictReg <= PREDICT_REG)
                        predictReg = PREDICT_SCRATCH_REG;

                    // The set instructions need a byte register
                    regMask = rpPredictRegPick(TYP_BYTE, predictReg, lockedRegs | rsvdRegs);
                }
                else
                {
                    regMask = RBM_NONE;
#ifdef _TARGET_XARCH_
                    tmpMask = RBM_NONE;
                    // Optimize the compare with a constant cases for xarch
                    if (op1->gtOper == GT_CNS_INT)
                    {
                        if (op2->gtOper == GT_CNS_INT)
                            tmpMask =
                                rpPredictTreeRegUse(op1, PREDICT_SCRATCH_REG, lockedRegs, rsvdRegs | op2->gtRsvdRegs);
                        rpPredictTreeRegUse(op2, PREDICT_NONE, lockedRegs | tmpMask, RBM_LASTUSE);
                        tree->gtUsedRegs = op2->gtUsedRegs;
                        goto RETURN_CHECK;
                    }
                    else if (op2->gtOper == GT_CNS_INT)
                    {
                        rpPredictTreeRegUse(op1, PREDICT_NONE, lockedRegs, rsvdRegs);
                        tree->gtUsedRegs = op1->gtUsedRegs;
                        goto RETURN_CHECK;
                    }
                    else if (op2->gtOper == GT_CNS_LNG)
                    {
                        regMaskTP op1Mask = rpPredictTreeRegUse(op1, PREDICT_ADDR, lockedRegs, rsvdRegs);
#ifdef _TARGET_X86_
                        // We also need one extra register to read values from
                        tmpMask = rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | op1Mask | rsvdRegs);
#endif // _TARGET_X86_
                        tree->gtUsedRegs = (regMaskSmall)tmpMask | op1->gtUsedRegs;
                        goto RETURN_CHECK;
                    }
#endif // _TARGET_XARCH_
                }

                unsigned op1TypeSize;
                unsigned op2TypeSize;

                op1TypeSize = genTypeSize(op1->TypeGet());
                op2TypeSize = genTypeSize(op2->TypeGet());

                op1PredictReg = PREDICT_REG;
                op2PredictReg = PREDICT_REG;

                if (tree->gtFlags & GTF_REVERSE_OPS)
                {
#ifdef _TARGET_XARCH_
                    if (op1TypeSize == sizeof(int))
                        op1PredictReg = PREDICT_NONE;
#endif

                    tmpMask = rpPredictTreeRegUse(op2, op2PredictReg, lockedRegs, rsvdRegs | op1->gtRsvdRegs);
                    rpPredictTreeRegUse(op1, op1PredictReg, lockedRegs | tmpMask, RBM_LASTUSE);
                }
                else
                {
#ifdef _TARGET_XARCH_
                    // For full DWORD compares we can have
                    //
                    //      op1 is an address mode and op2 is a register
                    // or
                    //      op1 is a register and op2 is an address mode
                    //
                    if ((op2TypeSize == sizeof(int)) && (op1TypeSize == op2TypeSize))
                    {
                        if (op2->gtOper == GT_LCL_VAR)
                        {
                            unsigned lclNum = op2->gtLclVar.gtLclNum;
                            varDsc          = lvaTable + lclNum;
                            /* Did we predict that this local will be enregistered? */
                            if (varDsc->lvTracked && (varDsc->lvRegNum != REG_STK))
                            {
                                op1PredictReg = PREDICT_ADDR;
                            }
                        }
                    }
                    // Codegen will generate cmp reg,[mem] for 4 or 8-byte types, but not for 1 or 2 byte types
                    if ((op1PredictReg != PREDICT_ADDR) && (op2TypeSize >= sizeof(int)))
                        op2PredictReg = PREDICT_ADDR;
#endif // _TARGET_XARCH_

                    tmpMask = rpPredictTreeRegUse(op1, op1PredictReg, lockedRegs, rsvdRegs | op2->gtRsvdRegs);
#ifdef _TARGET_ARM_
                    if ((op2->gtOper != GT_CNS_INT) || !codeGen->validImmForAlu(op2->gtIntCon.gtIconVal))
#endif
                    {
                        rpPredictTreeRegUse(op2, op2PredictReg, lockedRegs | tmpMask, RBM_LASTUSE);
                    }
                }

#ifdef _TARGET_XARCH_
                // In some cases in genCondSetFlags(), we need to use a temporary register (via rsPickReg())
                // to generate a sign/zero extension before doing a compare. Save a register for this purpose
                // if one of the registers is small and the types aren't equal.

                if (regMask == RBM_NONE)
                {
                    rpPredictReg op1xPredictReg, op2xPredictReg;
                    GenTreePtr   op1x, op2x;
                    if (tree->gtFlags & GTF_REVERSE_OPS) // TODO: do we really need to handle this case?
                    {
                        op1xPredictReg = op2PredictReg;
                        op2xPredictReg = op1PredictReg;
                        op1x           = op2;
                        op2x           = op1;
                    }
                    else
                    {
                        op1xPredictReg = op1PredictReg;
                        op2xPredictReg = op2PredictReg;
                        op1x           = op1;
                        op2x           = op2;
                    }
                    if ((op1xPredictReg < PREDICT_REG) &&  // op1 doesn't get a register (probably an indir)
                        (op2xPredictReg >= PREDICT_REG) && // op2 gets a register
                        varTypeIsSmall(op1x->TypeGet()))   // op1 is smaller than an int
                    {
                        bool needTmp = false;

                        // If op1x is a byte, and op2x is not a byteable register, we'll need a temp.
                        // We could predict a byteable register for op2x, but what if we don't get it?
                        // So, be conservative and always ask for a temp. There are a couple small CQ losses as a
                        // result.
                        if (varTypeIsByte(op1x->TypeGet()))
                        {
                            needTmp = true;
                        }
                        else
                        {
                            if (op2x->gtOper == GT_LCL_VAR) // this will be a GT_REG_VAR during code generation
                            {
                                if (genActualType(op1x->TypeGet()) != lvaGetActualType(op2x->gtLclVar.gtLclNum))
                                    needTmp = true;
                            }
                            else
                            {
                                if (op1x->TypeGet() != op2x->TypeGet())
                                    needTmp = true;
                            }
                        }
                        if (needTmp)
                        {
                            regMask = rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | rsvdRegs);
                        }
                    }
                }
#endif // _TARGET_XARCH_

                tree->gtUsedRegs = (regMaskSmall)regMask | op1->gtUsedRegs | op2->gtUsedRegs;
                goto RETURN_CHECK;

            case GT_MUL:

#ifndef _TARGET_AMD64_
                if (type == TYP_LONG)
                {
                    assert(tree->gtIsValid64RsltMul());

                    /* Strip out the cast nodes */

                    noway_assert(op1->gtOper == GT_CAST && op2->gtOper == GT_CAST);
                    op1 = op1->gtCast.CastOp();
                    op2 = op2->gtCast.CastOp();
#else
                if (false)
                {
#endif // !_TARGET_AMD64_
                USE_MULT_EAX:

#if defined(_TARGET_X86_)
                    // This will done by a 64-bit imul "imul eax, reg"
                    //   (i.e. EDX:EAX = EAX * reg)

                    /* Are we supposed to evaluate op2 first? */
                    if (tree->gtFlags & GTF_REVERSE_OPS)
                    {
                        rpPredictTreeRegUse(op2, PREDICT_PAIR_TMP_LO, lockedRegs, rsvdRegs | op1->gtRsvdRegs);
                        rpPredictTreeRegUse(op1, PREDICT_REG, lockedRegs | RBM_PAIR_TMP_LO, RBM_LASTUSE);
                    }
                    else
                    {
                        rpPredictTreeRegUse(op1, PREDICT_PAIR_TMP_LO, lockedRegs, rsvdRegs | op2->gtRsvdRegs);
                        rpPredictTreeRegUse(op2, PREDICT_REG, lockedRegs | RBM_PAIR_TMP_LO, RBM_LASTUSE);
                    }

                    /* set gtUsedRegs to EAX, EDX and the registers needed by op1 and op2 */

                    tree->gtUsedRegs = RBM_PAIR_TMP | op1->gtUsedRegs | op2->gtUsedRegs;

                    /* set regMask to the set of held registers */

                    regMask = RBM_PAIR_TMP_LO;

                    if (type == TYP_LONG)
                        regMask |= RBM_PAIR_TMP_HI;

#elif defined(_TARGET_ARM_)
                    // This will done by a 4 operand multiply

                    // Are we supposed to evaluate op2 first?
                    if (tree->gtFlags & GTF_REVERSE_OPS)
                    {
                        rpPredictTreeRegUse(op2, PREDICT_REG, lockedRegs, rsvdRegs | op1->gtRsvdRegs);
                        rpPredictTreeRegUse(op1, PREDICT_REG, lockedRegs, RBM_LASTUSE);
                    }
                    else
                    {
                        rpPredictTreeRegUse(op1, PREDICT_REG, lockedRegs, rsvdRegs | op2->gtRsvdRegs);
                        rpPredictTreeRegUse(op2, PREDICT_REG, lockedRegs, RBM_LASTUSE);
                    }

                    // set regMask to the set of held registers,
                    //  the two scratch register we need to compute the mul result

                    regMask = rpPredictRegPick(TYP_LONG, PREDICT_SCRATCH_REG, lockedRegs | rsvdRegs);

                    // set gtUsedRegs toregMask and the registers needed by op1 and op2

                    tree->gtUsedRegs = regMask | op1->gtUsedRegs | op2->gtUsedRegs;

#else // !_TARGET_X86_ && !_TARGET_ARM_
#error "Non-ARM or x86 _TARGET_ in RegPredict for 64-bit imul"
#endif

                    goto RETURN_CHECK;
                }
                else
                {
                    /* We use imulEAX for most unsigned multiply operations */
                    if (tree->gtOverflow())
                    {
                        if ((tree->gtFlags & GTF_UNSIGNED) || varTypeIsSmall(tree->TypeGet()))
                        {
                            goto USE_MULT_EAX;
                        }
                    }
                }

                __fallthrough;

            case GT_OR:
            case GT_XOR:
            case GT_AND:

            case GT_SUB:
            case GT_ADD:
                tree->gtUsedRegs = 0;

                if (predictReg <= PREDICT_REG)
                    predictReg = PREDICT_SCRATCH_REG;

            GENERIC_BINARY:

                noway_assert(op2);
                if (tree->gtFlags & GTF_REVERSE_OPS)
                {
                    op1PredictReg = PREDICT_REG;
#if !CPU_LOAD_STORE_ARCH
                    if (genTypeSize(op1->gtType) >= sizeof(int))
                        op1PredictReg = PREDICT_NONE;
#endif
                    regMask = rpPredictTreeRegUse(op2, predictReg, lockedRegs, rsvdRegs | op1->gtRsvdRegs);
                    rpPredictTreeRegUse(op1, op1PredictReg, lockedRegs | regMask, RBM_LASTUSE);
                }
                else
                {
                    op2PredictReg = PREDICT_REG;
#if !CPU_LOAD_STORE_ARCH
                    if (genTypeSize(op2->gtType) >= sizeof(int))
                        op2PredictReg = PREDICT_NONE;
#endif
                    regMask = rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs | op2->gtRsvdRegs);
#ifdef _TARGET_ARM_
                    // For most ALU operations we can generate a single instruction that encodes
                    // a small immediate integer constant value.  (except for multiply)
                    //
                    if ((op2->gtOper == GT_CNS_INT) && (oper != GT_MUL))
                    {
                        ssize_t ival = op2->gtIntCon.gtIconVal;
                        if (codeGen->validImmForAlu(ival))
                        {
                            op2PredictReg = PREDICT_NONE;
                        }
                        else if (codeGen->validImmForAdd(ival, INS_FLAGS_DONT_CARE) &&
                                 ((oper == GT_ADD) || (oper == GT_SUB)))
                        {
                            op2PredictReg = PREDICT_NONE;
                        }
                    }
                    if (op2PredictReg == PREDICT_NONE)
                    {
                        op2->gtUsedRegs = RBM_NONE;
                    }
                    else
#endif
                    {
                        rpPredictTreeRegUse(op2, op2PredictReg, lockedRegs | regMask, RBM_LASTUSE);
                    }
                }
                tree->gtUsedRegs = (regMaskSmall)regMask | op1->gtUsedRegs | op2->gtUsedRegs;

#if CPU_HAS_BYTE_REGS
                /* We have special register requirements for byte operations */

                if (varTypeIsByte(tree->TypeGet()))
                {
                    /* For 8 bit arithmetic, one operands has to be in a
                       byte-addressable register, and the other has to be
                       in a byte-addrble reg or in memory. Assume its in a reg */

                    regMaskTP regByteMask = 0;
                    regMaskTP op1ByteMask = op1->gtUsedRegs;

                    if (!(op1->gtUsedRegs & RBM_BYTE_REGS))
                    {
                        // Pick a Byte register to use for op1
                        regByteMask = rpPredictRegPick(TYP_BYTE, PREDICT_REG, lockedRegs | rsvdRegs);
                        op1ByteMask = regByteMask;
                    }

                    if (!(op2->gtUsedRegs & RBM_BYTE_REGS))
                    {
                        // Pick a Byte register to use for op2, avoiding the one used by op1
                        regByteMask |= rpPredictRegPick(TYP_BYTE, PREDICT_REG, lockedRegs | rsvdRegs | op1ByteMask);
                    }

                    if (regByteMask)
                    {
                        tree->gtUsedRegs |= regByteMask;
                        regMask = regByteMask;
                    }
                }
#endif
                goto RETURN_CHECK;

            case GT_DIV:
            case GT_MOD:

            case GT_UDIV:
            case GT_UMOD:

                /* non-integer division handled in generic way */
                if (!varTypeIsIntegral(type))
                {
                    tree->gtUsedRegs = 0;
                    if (predictReg <= PREDICT_REG)
                        predictReg = PREDICT_SCRATCH_REG;
                    goto GENERIC_BINARY;
                }

#ifndef _TARGET_64BIT_

                if (type == TYP_LONG && (oper == GT_MOD || oper == GT_UMOD))
                {
                    /* Special case:  a mod with an int op2 is done inline using idiv or div
                       to avoid a costly call to the helper */

                    noway_assert((op2->gtOper == GT_CNS_LNG) &&
                                 (op2->gtLngCon.gtLconVal == int(op2->gtLngCon.gtLconVal)));

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
                    if (tree->gtFlags & GTF_REVERSE_OPS)
                    {
                        tmpMask = rpPredictTreeRegUse(op2, PREDICT_REG, lockedRegs | RBM_PAIR_TMP,
                                                      rsvdRegs | op1->gtRsvdRegs);
                        tmpMask |= rpPredictTreeRegUse(op1, PREDICT_PAIR_TMP, lockedRegs | tmpMask, RBM_LASTUSE);
                    }
                    else
                    {
                        tmpMask = rpPredictTreeRegUse(op1, PREDICT_PAIR_TMP, lockedRegs, rsvdRegs | op2->gtRsvdRegs);
                        tmpMask |=
                            rpPredictTreeRegUse(op2, PREDICT_REG, lockedRegs | tmpMask | RBM_PAIR_TMP, RBM_LASTUSE);
                    }
                    regMask = RBM_PAIR_TMP;
#else // !_TARGET_X86_ && !_TARGET_ARM_
#error "Non-ARM or x86 _TARGET_ in RegPredict for 64-bit MOD"
#endif // !_TARGET_X86_ && !_TARGET_ARM_

                    tree->gtUsedRegs =
                        (regMaskSmall)(regMask | op1->gtUsedRegs | op2->gtUsedRegs |
                                       rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, regMask | tmpMask));

                    goto RETURN_CHECK;
                }
#endif // _TARGET_64BIT_

                /* no divide immediate, so force integer constant which is not
                 * a power of two to register
                 */

                if (op2->OperKind() & GTK_CONST)
                {
                    ssize_t ival = op2->gtIntConCommon.IconValue();

                    /* Is the divisor a power of 2 ? */

                    if (ival > 0 && genMaxOneBit(size_t(ival)))
                    {
                        goto GENERIC_UNARY;
                    }
                    else
                        op2PredictReg = PREDICT_SCRATCH_REG;
                }
                else
                {
                    /* Non integer constant also must be enregistered */
                    op2PredictReg = PREDICT_REG;
                }

                regMaskTP trashedMask;
                trashedMask = DUMMY_INIT(RBM_ILLEGAL);
                regMaskTP op1ExcludeMask;
                op1ExcludeMask = DUMMY_INIT(RBM_ILLEGAL);
                regMaskTP op2ExcludeMask;
                op2ExcludeMask = DUMMY_INIT(RBM_ILLEGAL);

#ifdef _TARGET_XARCH_
                /*  Consider the case "a / b" - we'll need to trash EDX (via "CDQ") before
                 *  we can safely allow the "b" value to die. Unfortunately, if we simply
                 *  mark the node "b" as using EDX, this will not work if "b" is a register
                 *  variable that dies with this particular reference. Thus, if we want to
                 *  avoid this situation (where we would have to spill the variable from
                 *  EDX to someplace else), we need to explicitly mark the interference
                 *  of the variable at this point.
                 */

                if (op2->gtOper == GT_LCL_VAR)
                {
                    unsigned lclNum = op2->gtLclVarCommon.gtLclNum;
                    varDsc          = lvaTable + lclNum;
                    if (varDsc->lvTracked)
                    {
#ifdef DEBUG
                        if (verbose)
                        {
                            if (!VarSetOps::IsMember(this, raLclRegIntf[REG_EAX], varDsc->lvVarIndex))
                                printf("Record interference between V%02u,T%02u and EAX -- int divide\n", lclNum,
                                       varDsc->lvVarIndex);
                            if (!VarSetOps::IsMember(this, raLclRegIntf[REG_EDX], varDsc->lvVarIndex))
                                printf("Record interference between V%02u,T%02u and EDX -- int divide\n", lclNum,
                                       varDsc->lvVarIndex);
                        }
#endif
                        VarSetOps::AddElemD(this, raLclRegIntf[REG_EAX], varDsc->lvVarIndex);
                        VarSetOps::AddElemD(this, raLclRegIntf[REG_EDX], varDsc->lvVarIndex);
                    }
                }

                /* set the held register based on opcode */
                if (oper == GT_DIV || oper == GT_UDIV)
                    regMask = RBM_EAX;
                else
                    regMask    = RBM_EDX;
                trashedMask    = (RBM_EAX | RBM_EDX);
                op1ExcludeMask = 0;
                op2ExcludeMask = (RBM_EAX | RBM_EDX);

#endif // _TARGET_XARCH_

#ifdef _TARGET_ARM_
                trashedMask    = RBM_NONE;
                op1ExcludeMask = RBM_NONE;
                op2ExcludeMask = RBM_NONE;
#endif

                /* set the lvPref reg if possible */
                GenTreePtr dest;
                /*
                 *  Walking the gtNext link twice from here should get us back
                 *  to our parent node, if this is an simple assignment tree.
                 */
                dest = tree->gtNext;
                if (dest && (dest->gtOper == GT_LCL_VAR) && dest->gtNext && (dest->gtNext->OperKind() & GTK_ASGOP) &&
                    dest->gtNext->gtOp.gtOp2 == tree)
                {
                    varDsc = lvaTable + dest->gtLclVarCommon.gtLclNum;
                    varDsc->addPrefReg(regMask, this);
                }
#ifdef _TARGET_XARCH_
                op1PredictReg = PREDICT_REG_EDX; /* Normally target op1 into EDX */
#else
                op1PredictReg        = PREDICT_SCRATCH_REG;
#endif

                /* are we supposed to evaluate op2 first? */
                if (tree->gtFlags & GTF_REVERSE_OPS)
                {
                    tmpMask = rpPredictTreeRegUse(op2, op2PredictReg, lockedRegs | op2ExcludeMask,
                                                  rsvdRegs | op1->gtRsvdRegs);
                    rpPredictTreeRegUse(op1, op1PredictReg, lockedRegs | tmpMask | op1ExcludeMask, RBM_LASTUSE);
                }
                else
                {
                    tmpMask = rpPredictTreeRegUse(op1, op1PredictReg, lockedRegs | op1ExcludeMask,
                                                  rsvdRegs | op2->gtRsvdRegs);
                    rpPredictTreeRegUse(op2, op2PredictReg, tmpMask | lockedRegs | op2ExcludeMask, RBM_LASTUSE);
                }
#ifdef _TARGET_ARM_
                regMask = tmpMask;
#endif
                /* grab EAX, EDX for this tree node */
                tree->gtUsedRegs = (regMaskSmall)trashedMask | op1->gtUsedRegs | op2->gtUsedRegs;

                goto RETURN_CHECK;

            case GT_LSH:
            case GT_RSH:
            case GT_RSZ:

                if (predictReg <= PREDICT_REG)
                    predictReg = PREDICT_SCRATCH_REG;

#ifndef _TARGET_64BIT_
                if (type == TYP_LONG)
                {
                    if (op2->IsCnsIntOrI())
                    {
                        regMask = rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs);
                        // no register used by op2
                        op2->gtUsedRegs  = 0;
                        tree->gtUsedRegs = op1->gtUsedRegs;
                    }
                    else
                    {
                        // since RBM_LNGARG_0 and RBM_SHIFT_LNG are hardwired we can't have them in the locked registers
                        tmpMask = lockedRegs;
                        tmpMask &= ~RBM_LNGARG_0;
                        tmpMask &= ~RBM_SHIFT_LNG;

                        // op2 goes to RBM_SHIFT, op1 to the RBM_LNGARG_0 pair
                        if (tree->gtFlags & GTF_REVERSE_OPS)
                        {
                            rpPredictTreeRegUse(op2, PREDICT_REG_SHIFT_LNG, tmpMask, RBM_NONE);
                            tmpMask |= RBM_SHIFT_LNG;
                            // Ensure that the RBM_SHIFT_LNG register interfere with op2's compCurLife
                            // Fix 383843 X86/ARM ILGEN
                            rpRecordRegIntf(RBM_SHIFT_LNG, compCurLife DEBUGARG("SHIFT_LNG arg setup"));
                            rpPredictTreeRegUse(op1, PREDICT_PAIR_LNGARG_0, tmpMask, RBM_LASTUSE);
                        }
                        else
                        {
                            rpPredictTreeRegUse(op1, PREDICT_PAIR_LNGARG_0, tmpMask, RBM_NONE);
                            tmpMask |= RBM_LNGARG_0;
                            // Ensure that the RBM_LNGARG_0 registers interfere with op1's compCurLife
                            // Fix 383839 ARM ILGEN
                            rpRecordRegIntf(RBM_LNGARG_0, compCurLife DEBUGARG("LNGARG_0 arg setup"));
                            rpPredictTreeRegUse(op2, PREDICT_REG_SHIFT_LNG, tmpMask, RBM_LASTUSE);
                        }
                        regMask = RBM_LNGRET; // function return registers
                        op1->gtUsedRegs |= RBM_LNGARG_0;
                        op2->gtUsedRegs |= RBM_SHIFT_LNG;

                        tree->gtUsedRegs = op1->gtUsedRegs | op2->gtUsedRegs;

                        // We are using a helper function to do shift:
                        //
                        tree->gtUsedRegs |= RBM_CALLEE_TRASH;
                    }
                }
                else
#endif // _TARGET_64BIT_
                {
#ifdef _TARGET_XARCH_
                    if (!op2->IsCnsIntOrI())
                        predictReg = PREDICT_NOT_REG_ECX;
#endif

                HANDLE_SHIFT_COUNT:
                    // Note that this code is also used by assigning shift operators (i.e. GT_ASG_LSH)

                    regMaskTP tmpRsvdRegs;

                    if ((tree->gtFlags & GTF_REVERSE_OPS) == 0)
                    {
                        regMask     = rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs | op2->gtRsvdRegs);
                        rsvdRegs    = RBM_LASTUSE;
                        tmpRsvdRegs = RBM_NONE;
                    }
                    else
                    {
                        regMask = RBM_NONE;
                        // Special case op1 of a constant
                        if (op1->IsCnsIntOrI())
                            tmpRsvdRegs = RBM_LASTUSE; // Allow a last use to occur in op2; See
                                                       // System.Xml.Schema.BitSet:Get(int):bool
                        else
                            tmpRsvdRegs = op1->gtRsvdRegs;
                    }

                    op2Mask = RBM_NONE;
                    if (!op2->IsCnsIntOrI())
                    {
                        if ((REG_SHIFT != REG_NA) && ((RBM_SHIFT & tmpRsvdRegs) == 0))
                        {
                            op2PredictReg = PREDICT_REG_SHIFT;
                        }
                        else
                        {
                            op2PredictReg = PREDICT_REG;
                        }

                        /* evaluate shift count into a register, likely the PREDICT_REG_SHIFT register */
                        op2Mask = rpPredictTreeRegUse(op2, op2PredictReg, lockedRegs | regMask, tmpRsvdRegs);

                        // If our target arch has a REG_SHIFT register then
                        //     we set the PrefReg when we have a LclVar for op2
                        //     we add an interference with REG_SHIFT for any other LclVars alive at op2
                        if (REG_SHIFT != REG_NA)
                        {
                            VARSET_TP liveSet(VarSetOps::MakeCopy(this, compCurLife));

                            while (op2->gtOper == GT_COMMA)
                            {
                                op2 = op2->gtOp.gtOp2;
                            }

                            if (op2->gtOper == GT_LCL_VAR)
                            {
                                varDsc = lvaTable + op2->gtLclVarCommon.gtLclNum;
                                varDsc->setPrefReg(REG_SHIFT, this);
                                if (varDsc->lvTracked)
                                {
                                    VarSetOps::RemoveElemD(this, liveSet, varDsc->lvVarIndex);
                                }
                            }

                            // Ensure that we have a register interference with the LclVar in tree's LiveSet,
                            // excluding the LclVar that was used for the shift amount as it is read-only
                            // and can be kept alive through the shift operation
                            //
                            rpRecordRegIntf(RBM_SHIFT, liveSet DEBUGARG("Variable Shift Register"));
                            // In case op2Mask doesn't contain the required shift register,
                            // we will or it in now.
                            op2Mask |= RBM_SHIFT;
                        }
                    }

                    if (tree->gtFlags & GTF_REVERSE_OPS)
                    {
                        assert(regMask == RBM_NONE);
                        regMask = rpPredictTreeRegUse(op1, predictReg, lockedRegs | op2Mask, rsvdRegs | RBM_LASTUSE);
                    }

#if CPU_HAS_BYTE_REGS
                    if (varTypeIsByte(type))
                    {
                        // Fix 383789 X86 ILGEN
                        // Fix 383813 X86 ILGEN
                        // Fix 383828 X86 ILGEN
                        if (op1->gtOper == GT_LCL_VAR)
                        {
                            varDsc = lvaTable + op1->gtLclVar.gtLclNum;
                            if (varDsc->lvTracked)
                            {
                                VARSET_TP op1VarBit(VarSetOps::MakeSingleton(this, varDsc->lvVarIndex));

                                // Ensure that we don't assign a Non-Byteable register for op1's LCL_VAR
                                rpRecordRegIntf(RBM_NON_BYTE_REGS, op1VarBit DEBUGARG("Non Byte Register"));
                            }
                        }
                        if ((regMask & RBM_BYTE_REGS) == 0)
                        {
                            // We need to grab a byte-able register, (i.e. EAX, EDX, ECX, EBX)
                            // and we can't select one that is already reserved (i.e. lockedRegs or regMask)
                            //
                            regMask |=
                                rpPredictRegPick(type, PREDICT_SCRATCH_REG, (lockedRegs | regMask | RBM_NON_BYTE_REGS));
                        }
                    }
#endif
                    tree->gtUsedRegs = (regMaskSmall)(regMask | op2Mask);
                }

                goto RETURN_CHECK;

            case GT_COMMA:
                if (tree->gtFlags & GTF_REVERSE_OPS)
                {
                    if (predictReg == PREDICT_NONE)
                    {
                        predictReg = PREDICT_REG;
                    }
                    else if (rpHasVarIndexForPredict(predictReg))
                    {
                        /* Don't propagate the use of tgt reg use in a GT_COMMA */
                        predictReg = PREDICT_SCRATCH_REG;
                    }

                    regMask = rpPredictTreeRegUse(op2, predictReg, lockedRegs, rsvdRegs);
                    rpPredictTreeRegUse(op1, PREDICT_NONE, lockedRegs | regMask, RBM_LASTUSE);
                }
                else
                {
                    rpPredictTreeRegUse(op1, PREDICT_NONE, lockedRegs, RBM_LASTUSE);

                    /* CodeGen will enregister the op2 side of a GT_COMMA */
                    if (predictReg == PREDICT_NONE)
                    {
                        predictReg = PREDICT_REG;
                    }
                    else if (rpHasVarIndexForPredict(predictReg))
                    {
                        /* Don't propagate the use of tgt reg use in a GT_COMMA */
                        predictReg = PREDICT_SCRATCH_REG;
                    }

                    regMask = rpPredictTreeRegUse(op2, predictReg, lockedRegs, rsvdRegs);
                }
                // tree should only accumulate the used registers from the op2 side of the GT_COMMA
                //
                tree->gtUsedRegs = op2->gtUsedRegs;
                if ((op2->gtOper == GT_LCL_VAR) && (rsvdRegs != 0))
                {
                    LclVarDsc* op2VarDsc = lvaTable + op2->gtLclVarCommon.gtLclNum;

                    if (op2VarDsc->lvTracked)
                    {
                        VARSET_TP op2VarBit(VarSetOps::MakeSingleton(this, op2VarDsc->lvVarIndex));
                        rpRecordRegIntf(rsvdRegs, op2VarBit DEBUGARG("comma use"));
                    }
                }
                goto RETURN_CHECK;

            case GT_QMARK:
            {
                noway_assert(op1 != NULL && op2 != NULL);

                /*
                 *  If the gtUsedRegs conflicts with lockedRegs
                 *  then we going to have to spill some registers
                 *  into the non-trashed register set to keep it alive
                 */
                unsigned spillCnt;
                spillCnt = 0;
                regMaskTP spillRegs;
                spillRegs = lockedRegs & tree->gtUsedRegs;

                while (spillRegs)
                {
                    /* Find the next register that needs to be spilled */
                    tmpMask = genFindLowestBit(spillRegs);

#ifdef DEBUG
                    if (verbose)
                    {
                        printf("Predict spill  of   %s before: ", getRegName(genRegNumFromMask(tmpMask)));
                        gtDispTree(tree, 0, NULL, true);
                    }
#endif
                    /* In Codegen it will typically introduce a spill temp here */
                    /* rather than relocating the register to a non trashed reg */
                    rpPredictSpillCnt++;
                    spillCnt++;

                    /* Remove it from the spillRegs and lockedRegs*/
                    spillRegs &= ~tmpMask;
                    lockedRegs &= ~tmpMask;
                }
                {
                    VARSET_TP startQmarkCondUseInPlaceVars(VarSetOps::MakeCopy(this, rpUseInPlace));

                    /* Evaluate the <cond> subtree */
                    rpPredictTreeRegUse(op1, PREDICT_NONE, lockedRegs, RBM_LASTUSE);
                    VarSetOps::Assign(this, rpUseInPlace, startQmarkCondUseInPlaceVars);
                    tree->gtUsedRegs = op1->gtUsedRegs;

                    noway_assert(op2->gtOper == GT_COLON);
                    if (rpHasVarIndexForPredict(predictReg) && ((op2->gtFlags & (GTF_ASG | GTF_CALL)) != 0))
                    {
                        // Don't try to target the register specified in predictReg when we have complex subtrees
                        //
                        predictReg = PREDICT_SCRATCH_REG;
                    }
                    GenTreePtr elseTree = op2->AsColon()->ElseNode();
                    GenTreePtr thenTree = op2->AsColon()->ThenNode();

                    noway_assert(thenTree != NULL && elseTree != NULL);

                    // Update compCurLife to only those vars live on the <then> subtree

                    VarSetOps::Assign(this, compCurLife, tree->gtQmark.gtThenLiveSet);

                    if (type == TYP_VOID)
                    {
                        /* Evaluate the <then> subtree */
                        rpPredictTreeRegUse(thenTree, PREDICT_NONE, lockedRegs, RBM_LASTUSE);
                        regMask    = RBM_NONE;
                        predictReg = PREDICT_NONE;
                    }
                    else
                    {
                        // A mask to use to force the predictor to choose low registers (to reduce code size)
                        regMaskTP avoidRegs = RBM_NONE;
#ifdef _TARGET_ARM_
                        avoidRegs = (RBM_R12 | RBM_LR);
#endif
                        if (predictReg <= PREDICT_REG)
                            predictReg = PREDICT_SCRATCH_REG;

                        /* Evaluate the <then> subtree */
                        regMask =
                            rpPredictTreeRegUse(thenTree, predictReg, lockedRegs, rsvdRegs | avoidRegs | RBM_LASTUSE);

                        if (regMask)
                        {
                            rpPredictReg op1PredictReg = rpGetPredictForMask(regMask);
                            if (op1PredictReg != PREDICT_NONE)
                                predictReg = op1PredictReg;
                        }
                    }

                    VarSetOps::Assign(this, rpUseInPlace, startQmarkCondUseInPlaceVars);

                    /* Evaluate the <else> subtree */
                    // First record the post-then liveness, and reset the current liveness to the else
                    // branch liveness.
                    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
                    VARSET_TP postThenLive(VarSetOps::MakeCopy(this, compCurLife));
#endif

                    VarSetOps::Assign(this, compCurLife, tree->gtQmark.gtElseLiveSet);

                    rpPredictTreeRegUse(elseTree, predictReg, lockedRegs, rsvdRegs | RBM_LASTUSE);
                    tree->gtUsedRegs |= thenTree->gtUsedRegs | elseTree->gtUsedRegs;

                    // The then and the else are "virtual basic blocks" that form a control-flow diamond.
                    // They each have only one successor, which they share.  Their live-out sets must equal the
                    // live-in set of this virtual successor block, and thus must be the same.  We can assert
                    // that equality here.
                    assert(VarSetOps::Equal(this, compCurLife, postThenLive));

                    if (spillCnt > 0)
                    {
                        regMaskTP reloadMask = RBM_NONE;

                        while (spillCnt)
                        {
                            regMaskTP reloadReg;

                            /* Get an extra register to hold it */
                            reloadReg = rpPredictRegPick(TYP_INT, PREDICT_REG, lockedRegs | regMask | reloadMask);
#ifdef DEBUG
                            if (verbose)
                            {
                                printf("Predict reload into %s after : ", getRegName(genRegNumFromMask(reloadReg)));
                                gtDispTree(tree, 0, NULL, true);
                            }
#endif
                            reloadMask |= reloadReg;

                            spillCnt--;
                        }

                        /* update the gtUsedRegs mask */
                        tree->gtUsedRegs |= reloadMask;
                    }
                }

                goto RETURN_CHECK;
            }
            case GT_RETURN:
                tree->gtUsedRegs = RBM_NONE;
                regMask          = RBM_NONE;

                /* Is there a return value? */
                if (op1 != NULL)
                {
#if FEATURE_FP_REGALLOC
                    if (varTypeIsFloating(type))
                    {
                        predictReg = PREDICT_FLTRET;
                        if (type == TYP_FLOAT)
                            regMask = RBM_FLOATRET;
                        else
                            regMask = RBM_DOUBLERET;
                    }
                    else
#endif
                        if (isRegPairType(type))
                    {
                        predictReg = PREDICT_LNGRET;
                        regMask    = RBM_LNGRET;
                    }
                    else
                    {
                        predictReg = PREDICT_INTRET;
                        regMask    = RBM_INTRET;
                    }
                    if (info.compCallUnmanaged)
                    {
                        lockedRegs |= (RBM_PINVOKE_TCB | RBM_PINVOKE_FRAME);
                    }
                    rpPredictTreeRegUse(op1, predictReg, lockedRegs, RBM_LASTUSE);
                    tree->gtUsedRegs = op1->gtUsedRegs | (regMaskSmall)regMask;
                }

#if defined(_TARGET_ARM_) && defined(PROFILING_SUPPORTED)
                // When on Arm under profiler, to emit Leave callback we would need RBM_PROFILER_RETURN_USED.
                // We could optimize on registers based on int/long or no return value.  But to
                // keep it simple we will mark entire RBM_PROFILER_RETURN_USED as used regs here.
                if (compIsProfilerHookNeeded())
                {
                    tree->gtUsedRegs |= RBM_PROFILER_RET_USED;
                }

#endif
                goto RETURN_CHECK;

            case GT_RETFILT:
                if (op1 != NULL)
                {
                    rpPredictTreeRegUse(op1, PREDICT_NONE, lockedRegs, RBM_LASTUSE);
                    regMask          = genReturnRegForTree(tree);
                    tree->gtUsedRegs = op1->gtUsedRegs | (regMaskSmall)regMask;
                    goto RETURN_CHECK;
                }
                tree->gtUsedRegs = 0;
                regMask          = 0;

                goto RETURN_CHECK;

            case GT_JTRUE:
                /* This must be a test of a relational operator */

                noway_assert(op1->OperIsCompare());

                /* Only condition code set by this operation */

                rpPredictTreeRegUse(op1, PREDICT_NONE, lockedRegs, RBM_NONE);

                tree->gtUsedRegs = op1->gtUsedRegs;
                regMask          = 0;

                goto RETURN_CHECK;

            case GT_SWITCH:
                noway_assert(type <= TYP_INT);
                noway_assert(compCurBB->bbJumpKind == BBJ_SWITCH);
#ifdef _TARGET_ARM_
                {
                    regMask          = rpPredictTreeRegUse(op1, PREDICT_REG, lockedRegs, RBM_NONE);
                    unsigned jumpCnt = compCurBB->bbJumpSwt->bbsCount;
                    if (jumpCnt > 2)
                    {
                        // Table based switch requires an extra register for the table base
                        regMask |= rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | regMask);
                    }
                    tree->gtUsedRegs = op1->gtUsedRegs | regMask;
                }
#else  // !_TARGET_ARM_
                rpPredictTreeRegUse(op1, PREDICT_REG, lockedRegs, RBM_NONE);
                tree->gtUsedRegs = op1->gtUsedRegs;
#endif // _TARGET_ARM_
                regMask = 0;
                goto RETURN_CHECK;

            case GT_CKFINITE:
                if (predictReg <= PREDICT_REG)
                    predictReg = PREDICT_SCRATCH_REG;

                rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs);
                // Need a reg to load exponent into
                regMask          = rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | rsvdRegs);
                tree->gtUsedRegs = (regMaskSmall)regMask | op1->gtUsedRegs;
                goto RETURN_CHECK;

            case GT_LCLHEAP:
                regMask = rpPredictTreeRegUse(op1, PREDICT_SCRATCH_REG, lockedRegs, rsvdRegs);
                op2Mask = 0;

#ifdef _TARGET_ARM_
                if (info.compInitMem)
                {
                    // We zero out two registers in the ARM codegen path
                    op2Mask |=
                        rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | rsvdRegs | regMask | op2Mask);
                }
#endif

                op1->gtUsedRegs |= (regMaskSmall)regMask;
                tree->gtUsedRegs = op1->gtUsedRegs | (regMaskSmall)op2Mask;

                // The result will be put in the reg we picked for the size
                // regMask = <already set as we want it to be>

                goto RETURN_CHECK;

            case GT_OBJ:
            {
#ifdef _TARGET_ARM_
                if (predictReg <= PREDICT_REG)
                    predictReg = PREDICT_SCRATCH_REG;

                regMaskTP avoidRegs = (RBM_R12 | RBM_LR); // A mask to use to force the predictor to choose low
                                                          // registers (to reduce code size)
                regMask = RBM_NONE;
                tmpMask = rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs | avoidRegs);
#endif

                if (fgIsIndirOfAddrOfLocal(tree) != NULL)
                {
                    compUpdateLifeVar</*ForCodeGen*/ false>(tree);
                }

#ifdef _TARGET_ARM_
                unsigned  objSize   = info.compCompHnd->getClassSize(tree->gtObj.gtClass);
                regMaskTP preferReg = rpPredictRegMask(predictReg, TYP_I_IMPL);
                // If it has one bit set, and that's an arg reg...
                if (preferReg != RBM_NONE && genMaxOneBit(preferReg) && ((preferReg & RBM_ARG_REGS) != 0))
                {
                    // We are passing the 'obj' in the argument registers
                    //
                    regNumber rn = genRegNumFromMask(preferReg);

                    //  Add the registers used to pass the 'obj' to regMask.
                    for (unsigned i = 0; i < objSize / 4; i++)
                    {
                        if (rn == MAX_REG_ARG)
                            break;
                        // Otherwise...
                        regMask |= genRegMask(rn);
                        rn = genRegArgNext(rn);
                    }
                }
                else
                {
                    // We are passing the 'obj' in the outgoing arg space
                    // We will need one register to load into unless the 'obj' size is 4 or less.
                    //
                    if (objSize > 4)
                    {
                        regMask = rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | tmpMask | avoidRegs);
                    }
                }
                tree->gtUsedRegs = (regMaskSmall)(regMask | tmpMask);
                goto RETURN_CHECK;
#else  // !_TARGET_ARM
                goto GENERIC_UNARY;
#endif // _TARGET_ARM_
            }

            case GT_MKREFANY:
            {
#ifdef _TARGET_ARM_
                regMaskTP preferReg = rpPredictRegMask(predictReg, TYP_I_IMPL);
                regMask             = RBM_NONE;
                if ((((preferReg - 1) & preferReg) == 0) && ((preferReg & RBM_ARG_REGS) != 0))
                {
                    // A MKREFANY takes up two registers.
                    regNumber rn = genRegNumFromMask(preferReg);
                    regMask      = RBM_NONE;
                    if (rn < MAX_REG_ARG)
                    {
                        regMask |= genRegMask(rn);
                        rn = genRegArgNext(rn);
                        if (rn < MAX_REG_ARG)
                            regMask |= genRegMask(rn);
                    }
                }
                if (regMask != RBM_NONE)
                {
                    // Condensation of GENERIC_BINARY path.
                    assert((tree->gtFlags & GTF_REVERSE_OPS) == 0);
                    op2PredictReg        = PREDICT_REG;
                    regMaskTP regMaskOp1 = rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs | op2->gtRsvdRegs);
                    rpPredictTreeRegUse(op2, op2PredictReg, lockedRegs | regMaskOp1, RBM_LASTUSE);
                    regMask |= op1->gtUsedRegs | op2->gtUsedRegs;
                    tree->gtUsedRegs = (regMaskSmall)regMask;
                    goto RETURN_CHECK;
                }
                tree->gtUsedRegs = op1->gtUsedRegs;
#endif // _TARGET_ARM_
                goto GENERIC_BINARY;
            }

            case GT_BOX:
                goto GENERIC_UNARY;

            case GT_LOCKADD:
                goto GENERIC_BINARY;

            case GT_XADD:
            case GT_XCHG:
                // Ensure we can write to op2.  op2 will hold the output.
                if (predictReg < PREDICT_SCRATCH_REG)
                    predictReg = PREDICT_SCRATCH_REG;

                if (tree->gtFlags & GTF_REVERSE_OPS)
                {
                    op2Mask = rpPredictTreeRegUse(op2, predictReg, lockedRegs, rsvdRegs);
                    regMask = rpPredictTreeRegUse(op1, PREDICT_REG, lockedRegs, rsvdRegs | op2Mask);
                }
                else
                {
                    regMask = rpPredictTreeRegUse(op1, PREDICT_REG, lockedRegs, rsvdRegs);
                    op2Mask = rpPredictTreeRegUse(op2, PREDICT_SCRATCH_REG, lockedRegs, rsvdRegs | regMask);
                }
                tree->gtUsedRegs = (regMaskSmall)(regMask | op2Mask);
                goto RETURN_CHECK;

            case GT_ARR_LENGTH:
                goto GENERIC_UNARY;

            case GT_INIT_VAL:
                // This unary operator simply passes through the value from its child (much like GT_NOP)
                // and thus won't need a scratch register.
                regMask          = rpPredictTreeRegUse(op1, predictReg, lockedRegs, rsvdRegs);
                tree->gtUsedRegs = op1->gtUsedRegs;
                goto RETURN_CHECK;

            default:
#ifdef DEBUG
                gtDispTree(tree);
#endif
                noway_assert(!"unexpected simple operator in reg use prediction");
                break;
        }
    }

    /* See what kind of a special operator we have here */

    switch (oper)
    {
        GenTreePtr      args;
        GenTreeArgList* list;
        regMaskTP       keepMask;
        unsigned        regArgsNum;
        int             regIndex;
        regMaskTP       regArgMask;
        regMaskTP       curArgMask;

        case GT_CALL:

        {

            /* initialize so we can just or in various bits */
            tree->gtUsedRegs = RBM_NONE;

#if GTF_CALL_REG_SAVE
            /*
             *  Unless the GTF_CALL_REG_SAVE flag is set,
             *  we can't preserve the RBM_CALLEE_TRASH registers.
             *  (likewise we can't preserve the return registers)
             *  So we remove them from the lockedRegs set and
             *  record any of them in the keepMask
             */

            if (tree->gtFlags & GTF_CALL_REG_SAVE)
            {
                regMaskTP trashMask = genReturnRegForTree(tree);

                keepMask = lockedRegs & trashMask;
                lockedRegs &= ~trashMask;
            }
            else
#endif
            {
                keepMask = lockedRegs & RBM_CALLEE_TRASH;
                lockedRegs &= ~RBM_CALLEE_TRASH;
            }

            regArgsNum = 0;
            regIndex   = 0;

            /* Is there an object pointer? */
            if (tree->gtCall.gtCallObjp)
            {
                /* Evaluate the instance pointer first */

                args = tree->gtCall.gtCallObjp;

                /* the objPtr always goes to an integer register (through temp or directly) */
                noway_assert(regArgsNum == 0);
                regArgsNum++;

                /* Must be passed in a register */

                noway_assert(args->gtFlags & GTF_LATE_ARG);

                /* Must be either a deferred reg arg node or a GT_ASG node */

                noway_assert(args->IsArgPlaceHolderNode() || args->IsNothingNode() || (args->gtOper == GT_ASG) ||
                             args->OperIsCopyBlkOp() || (args->gtOper == GT_COMMA));

                if (!args->IsArgPlaceHolderNode())
                {
                    rpPredictTreeRegUse(args, PREDICT_NONE, lockedRegs, RBM_LASTUSE);
                }
            }
            VARSET_TP startArgUseInPlaceVars(VarSetOps::UninitVal());
            VarSetOps::Assign(this, startArgUseInPlaceVars, rpUseInPlace);

            /* process argument list */
            for (list = tree->gtCall.gtCallArgs; list; list = list->Rest())
            {
                args = list->Current();

                if (args->gtFlags & GTF_LATE_ARG)
                {
                    /* Must be either a Placeholder/NOP node or a GT_ASG node */

                    noway_assert(args->IsArgPlaceHolderNode() || args->IsNothingNode() || (args->gtOper == GT_ASG) ||
                                 args->OperIsCopyBlkOp() || (args->gtOper == GT_COMMA));

                    if (!args->IsArgPlaceHolderNode())
                    {
                        rpPredictTreeRegUse(args, PREDICT_NONE, lockedRegs, RBM_LASTUSE);
                    }

                    regArgsNum++;
                }
                else
                {
#ifdef FEATURE_FIXED_OUT_ARGS
                    // We'll store this argument into the outgoing argument area
                    // It needs to be in a register to be stored.
                    //
                    predictReg = PREDICT_REG;

#else // !FEATURE_FIXED_OUT_ARGS
                    // We'll generate a push for this argument
                    //
                    predictReg = PREDICT_NONE;
                    if (varTypeIsSmall(args->TypeGet()))
                    {
                        /* We may need to sign or zero extend a small type using a register */
                        predictReg = PREDICT_SCRATCH_REG;
                    }
#endif

                    rpPredictTreeRegUse(args, predictReg, lockedRegs, RBM_LASTUSE);
                }
                VarSetOps::Assign(this, rpUseInPlace, startArgUseInPlaceVars);
                tree->gtUsedRegs |= args->gtUsedRegs;
            }

            /* Is there a late argument list */

            regIndex   = 0;
            regArgMask = RBM_NONE; // Set of argument registers that have already been setup.
            args       = NULL;

            /* process the late argument list */
            for (list = tree->gtCall.gtCallLateArgs; list; regIndex++)
            {
                // If the current argument being copied is a promoted struct local, set this pointer to its description.
                LclVarDsc* promotedStructLocal = NULL;

                curArgMask = RBM_NONE; // Set of argument registers that are going to be setup by this arg
                tmpMask    = RBM_NONE; // Set of additional temp registers that are need only to setup the current arg

                assert(list->OperIsList());

                args = list->Current();
                list = list->Rest();

                assert(!args->IsArgPlaceHolderNode()); // No place holders nodes are in gtCallLateArgs;

                fgArgTabEntryPtr curArgTabEntry = gtArgEntryByNode(tree->AsCall(), args);
                assert(curArgTabEntry);

                regNumber regNum = curArgTabEntry->regNum; // first register use to pass this argument
                unsigned  numSlots =
                    curArgTabEntry->numSlots; // number of outgoing arg stack slots used by this argument

                rpPredictReg argPredictReg;
                regMaskTP    avoidReg = RBM_NONE;

                if (regNum != REG_STK)
                {
                    argPredictReg = rpGetPredictForReg(regNum);
                    curArgMask |= genRegMask(regNum);
                }
                else
                {
                    assert(numSlots > 0);
                    argPredictReg = PREDICT_NONE;
#ifdef _TARGET_ARM_
                    // Force the predictor to choose a low register when regNum is REG_STK to reduce code bloat
                    avoidReg = (RBM_R12 | RBM_LR);
#endif
                }

#ifdef _TARGET_ARM_
                // For TYP_LONG or TYP_DOUBLE register arguments we need to add the second argument register
                //
                if ((regNum != REG_STK) && ((args->TypeGet() == TYP_LONG) || (args->TypeGet() == TYP_DOUBLE)))
                {
                    // 64-bit longs and doubles require 2 consecutive argument registers
                    curArgMask |= genRegMask(REG_NEXT(regNum));
                }
                else if (args->TypeGet() == TYP_STRUCT)
                {
                    GenTreePtr argx       = args;
                    GenTreePtr lclVarTree = NULL;

                    /* The GT_OBJ may be be a child of a GT_COMMA */
                    while (argx->gtOper == GT_COMMA)
                    {
                        argx = argx->gtOp.gtOp2;
                    }
                    unsigned originalSize = 0;

                    if (argx->gtOper == GT_OBJ)
                    {
                        originalSize = info.compCompHnd->getClassSize(argx->gtObj.gtClass);

                        // Is it the address of a promoted struct local?
                        if (argx->gtObj.gtOp1->gtOper == GT_ADDR && argx->gtObj.gtOp1->gtOp.gtOp1->gtOper == GT_LCL_VAR)
                        {
                            lclVarTree        = argx->gtObj.gtOp1->gtOp.gtOp1;
                            LclVarDsc* varDsc = &lvaTable[lclVarTree->gtLclVarCommon.gtLclNum];
                            if (varDsc->lvPromoted)
                                promotedStructLocal = varDsc;
                        }
                    }
                    else if (argx->gtOper == GT_LCL_VAR)
                    {
                        varDsc       = lvaTable + argx->gtLclVarCommon.gtLclNum;
                        originalSize = varDsc->lvSize();

                        // Is it a promoted struct local?
                        if (varDsc->lvPromoted)
                            promotedStructLocal = varDsc;
                    }
                    else if (argx->gtOper == GT_MKREFANY)
                    {
                        originalSize = 2 * TARGET_POINTER_SIZE;
                    }
                    else
                    {
                        noway_assert(!"Can't predict unsupported TYP_STRUCT arg kind");
                    }

                    // We only pass arguments differently if it a struct local "independently" promoted, which
                    // allows the field locals can be independently enregistered.
                    if (promotedStructLocal != NULL)
                    {
                        if (lvaGetPromotionType(promotedStructLocal) != PROMOTION_TYPE_INDEPENDENT)
                            promotedStructLocal = NULL;
                    }

                    unsigned slots = ((unsigned)(roundUp(originalSize, TARGET_POINTER_SIZE))) / REGSIZE_BYTES;

                    // Are we passing a TYP_STRUCT in multiple integer registers?
                    // if so set up curArgMask to reflect this
                    // Also slots is updated to reflect the number of outgoing arg slots that we will write
                    if (regNum != REG_STK)
                    {
                        regNumber regLast = (curArgTabEntry->isHfaRegArg) ? LAST_FP_ARGREG : REG_ARG_LAST;
                        assert(genIsValidReg(regNum));
                        regNumber nextReg = REG_NEXT(regNum);
                        slots--;
                        while (slots > 0 && nextReg <= regLast)
                        {
                            curArgMask |= genRegMask(nextReg);
                            nextReg = REG_NEXT(nextReg);
                            slots--;
                        }
                    }

                    if ((promotedStructLocal != NULL) && (curArgMask != RBM_NONE))
                    {
                        // All or a portion of this struct will be placed in the argument registers indicated by
                        // "curArgMask". We build in knowledge of the order in which the code is generated here, so
                        // that the second arg to be evaluated interferes with the reg for the first, the third with
                        // the regs for the first and second, etc. But since we always place the stack slots before
                        // placing the register slots we do not add inteferences for any part of the struct that gets
                        // passed on the stack.

                        argPredictReg =
                            PREDICT_NONE; // We will target the indivual fields into registers but not the whole struct
                        regMaskTP prevArgMask = RBM_NONE;
                        for (unsigned i = 0; i < promotedStructLocal->lvFieldCnt; i++)
                        {
                            LclVarDsc* fieldVarDsc = &lvaTable[promotedStructLocal->lvFieldLclStart + i];
                            if (fieldVarDsc->lvTracked)
                            {
                                assert(lclVarTree != NULL);
                                if (prevArgMask != RBM_NONE)
                                {
                                    rpRecordRegIntf(prevArgMask, VarSetOps::MakeSingleton(this, fieldVarDsc->lvVarIndex)
                                                                     DEBUGARG("fieldVar/argReg"));
                                }
                            }
                            // Now see many registers this uses up.
                            unsigned firstRegOffset = fieldVarDsc->lvFldOffset / TARGET_POINTER_SIZE;
                            unsigned nextAfterLastRegOffset =
                                (fieldVarDsc->lvFldOffset + fieldVarDsc->lvExactSize + TARGET_POINTER_SIZE - 1) /
                                TARGET_POINTER_SIZE;
                            unsigned nextAfterLastArgRegOffset =
                                min(nextAfterLastRegOffset,
                                    genIsValidIntReg(regNum) ? REG_NEXT(REG_ARG_LAST) : REG_NEXT(LAST_FP_ARGREG));

                            for (unsigned regOffset = firstRegOffset; regOffset < nextAfterLastArgRegOffset;
                                 regOffset++)
                            {
                                prevArgMask |= genRegMask(regNumber(regNum + regOffset));
                            }

                            if (nextAfterLastRegOffset > nextAfterLastArgRegOffset)
                            {
                                break;
                            }

                            if ((fieldVarDsc->lvFldOffset % TARGET_POINTER_SIZE) == 0)
                            {
                                // Add the argument register used here as a preferred register for this fieldVarDsc
                                //
                                regNumber firstRegUsed = regNumber(regNum + firstRegOffset);
                                fieldVarDsc->setPrefReg(firstRegUsed, this);
                            }
                        }
                        compUpdateLifeVar</*ForCodeGen*/ false>(argx);
                    }

                    // If slots is greater than zero then part or all of this TYP_STRUCT
                    // argument is passed in the outgoing argument area. (except HFA arg)
                    //
                    if ((slots > 0) && !curArgTabEntry->isHfaRegArg)
                    {
                        // We will need a register to address the TYP_STRUCT
                        // Note that we can use an argument register in curArgMask as in
                        // codegen we pass the stack portion of the argument before we
                        // setup the register part.
                        //

                        // Force the predictor to choose a LOW_REG here to reduce code bloat
                        avoidReg = (RBM_R12 | RBM_LR);

                        assert(tmpMask == RBM_NONE);
                        tmpMask = rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | regArgMask | avoidReg);

                        // If slots > 1 then we will need a second register to perform the load/store into the outgoing
                        // arg area
                        if (slots > 1)
                        {
                            tmpMask |= rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG,
                                                        lockedRegs | regArgMask | tmpMask | avoidReg);
                        }
                    }
                } // (args->TypeGet() == TYP_STRUCT)
#endif            // _TARGET_ARM_

                // If we have a promotedStructLocal we don't need to call rpPredictTreeRegUse(args, ...
                // as we have already calculated the correct tmpMask and curArgMask values and
                // by calling rpPredictTreeRegUse we would just add unnecessary register inteferences.
                //
                if (promotedStructLocal == NULL)
                {
                    /* Target the appropriate argument register */
                    tmpMask |= rpPredictTreeRegUse(args, argPredictReg, lockedRegs | regArgMask, RBM_LASTUSE);
                }

                // We mark OBJ(ADDR(LOCAL)) with GTF_VAR_DEATH since the local is required to live
                // for the duration of the OBJ.
                if (args->OperGet() == GT_OBJ && (args->gtFlags & GTF_VAR_DEATH))
                {
                    GenTreePtr lclVarTree = fgIsIndirOfAddrOfLocal(args);
                    assert(lclVarTree != NULL); // Or else would not be marked with GTF_VAR_DEATH.
                    compUpdateLifeVar</*ForCodeGen*/ false>(lclVarTree);
                }

                regArgMask |= curArgMask;
                args->gtUsedRegs |= (tmpMask | regArgMask);
                tree->gtUsedRegs |= args->gtUsedRegs;
                tree->gtCall.gtCallLateArgs->gtUsedRegs |= args->gtUsedRegs;

                if (args->gtUsedRegs != RBM_NONE)
                {
                    // Add register interference with the set of registers used or in use when we evaluated
                    // the current arg, with whatever is alive after the current arg
                    //
                    rpRecordRegIntf(args->gtUsedRegs, compCurLife DEBUGARG("register arg setup"));
                }
                VarSetOps::Assign(this, rpUseInPlace, startArgUseInPlaceVars);
            }
            assert(list == NULL);

            regMaskTP callAddrMask;
            callAddrMask = RBM_NONE;
#if CPU_LOAD_STORE_ARCH
            predictReg = PREDICT_SCRATCH_REG;
#else
            predictReg       = PREDICT_NONE;
#endif

            switch (tree->gtFlags & GTF_CALL_VIRT_KIND_MASK)
            {
                case GTF_CALL_VIRT_STUB:

                    // We only want to record an interference between the virtual stub
                    // param reg and anything that's live AFTER the call, but we've not
                    // yet processed the indirect target.  So add virtualStubParamInfo.regMask
                    // to interferingRegs.
                    interferingRegs |= virtualStubParamInfo->GetRegMask();
#ifdef DEBUG
                    if (verbose)
                        printf("Adding interference with Virtual Stub Param\n");
#endif
                    codeGen->regSet.rsSetRegsModified(virtualStubParamInfo->GetRegMask());

                    if (tree->gtCall.gtCallType == CT_INDIRECT)
                    {
                        predictReg = virtualStubParamInfo->GetPredict();
                    }
                    break;

                case GTF_CALL_VIRT_VTABLE:
                    predictReg = PREDICT_SCRATCH_REG;
                    break;

                case GTF_CALL_NONVIRT:
                    predictReg = PREDICT_SCRATCH_REG;
                    break;
            }

            if (tree->gtCall.gtCallType == CT_INDIRECT)
            {
#if defined(_TARGET_ARM_) || defined(_TARGET_AMD64_)
                if (tree->gtCall.gtCallCookie)
                {
                    codeGen->regSet.rsSetRegsModified(RBM_PINVOKE_COOKIE_PARAM | RBM_PINVOKE_TARGET_PARAM);

                    callAddrMask |= rpPredictTreeRegUse(tree->gtCall.gtCallCookie, PREDICT_REG_PINVOKE_COOKIE_PARAM,
                                                        lockedRegs | regArgMask, RBM_LASTUSE);

                    // Just in case we predict some other registers, force interference with our two special
                    // parameters: PINVOKE_COOKIE_PARAM & PINVOKE_TARGET_PARAM
                    callAddrMask |= (RBM_PINVOKE_COOKIE_PARAM | RBM_PINVOKE_TARGET_PARAM);

                    predictReg = PREDICT_REG_PINVOKE_TARGET_PARAM;
                }
#endif
                callAddrMask |=
                    rpPredictTreeRegUse(tree->gtCall.gtCallAddr, predictReg, lockedRegs | regArgMask, RBM_LASTUSE);
            }
            else if (predictReg != PREDICT_NONE)
            {
                callAddrMask |= rpPredictRegPick(TYP_I_IMPL, predictReg, lockedRegs | regArgMask);
            }

            if (tree->gtFlags & GTF_CALL_UNMANAGED)
            {
                // Need a register for tcbReg
                callAddrMask |=
                    rpPredictRegPick(TYP_I_IMPL, PREDICT_SCRATCH_REG, lockedRegs | regArgMask | callAddrMask);
#if CPU_LOAD_STORE_ARCH
                // Need an extra register for tmpReg
                callAddrMask |=
                    rpPredictRegPick(TYP_I_IMPL, PREDICT_SCRATCH_REG, lockedRegs | regArgMask | callAddrMask);
#endif
            }

            tree->gtUsedRegs |= callAddrMask;

            /* After the call restore the orginal value of lockedRegs */
            lockedRegs |= keepMask;

            /* set the return register */
            regMask = genReturnRegForTree(tree);

            if (regMask & rsvdRegs)
            {
                // We will need to relocate the return register value
                regMaskTP intRegMask = (regMask & RBM_ALLINT);
#if FEATURE_FP_REGALLOC
                regMaskTP floatRegMask = (regMask & RBM_ALLFLOAT);
#endif
                regMask = RBM_NONE;

                if (intRegMask)
                {
                    if (intRegMask == RBM_INTRET)
                    {
                        regMask |= rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, rsvdRegs | regMask);
                    }
                    else if (intRegMask == RBM_LNGRET)
                    {
                        regMask |= rpPredictRegPick(TYP_LONG, PREDICT_SCRATCH_REG, rsvdRegs | regMask);
                    }
                    else
                    {
                        noway_assert(!"unexpected return regMask");
                    }
                }

#if FEATURE_FP_REGALLOC
                if (floatRegMask)
                {
                    if (floatRegMask == RBM_FLOATRET)
                    {
                        regMask |= rpPredictRegPick(TYP_FLOAT, PREDICT_SCRATCH_REG, rsvdRegs | regMask);
                    }
                    else if (floatRegMask == RBM_DOUBLERET)
                    {
                        regMask |= rpPredictRegPick(TYP_DOUBLE, PREDICT_SCRATCH_REG, rsvdRegs | regMask);
                    }
                    else // HFA return case
                    {
                        for (unsigned f = 0; f < genCountBits(floatRegMask); f++)
                        {
                            regMask |= rpPredictRegPick(TYP_FLOAT, PREDICT_SCRATCH_REG, rsvdRegs | regMask);
                        }
                    }
                }
#endif
            }

            /* the return registers (if any) are killed */
            tree->gtUsedRegs |= regMask;

#if GTF_CALL_REG_SAVE
            if (!(tree->gtFlags & GTF_CALL_REG_SAVE))
#endif
            {
                /* the RBM_CALLEE_TRASH set are killed (i.e. EAX,ECX,EDX) */
                tree->gtUsedRegs |= RBM_CALLEE_TRASH;
            }
        }

#if defined(_TARGET_ARM_) && defined(PROFILING_SUPPORTED)
            // Mark required registers for emitting tailcall profiler callback as used
            if (compIsProfilerHookNeeded() && tree->gtCall.IsTailCall() && (tree->gtCall.gtCallType == CT_USER_FUNC))
            {
                tree->gtUsedRegs |= RBM_PROFILER_TAIL_USED;
            }
#endif
            break;

        case GT_ARR_ELEM:

            // Figure out which registers can't be touched
            unsigned dim;
            for (dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
                rsvdRegs |= tree->gtArrElem.gtArrInds[dim]->gtRsvdRegs;

            regMask = rpPredictTreeRegUse(tree->gtArrElem.gtArrObj, PREDICT_REG, lockedRegs, rsvdRegs);

            regMaskTP dimsMask;
            dimsMask = 0;

#if CPU_LOAD_STORE_ARCH
            // We need a register to load the bounds of the MD array
            regMask |= rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | regMask);
#endif

            for (dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
            {
                /* We need scratch registers to compute index-lower_bound.
                   Also, gtArrInds[0]'s register will be used as the second
                   addressability register (besides gtArrObj's) */

                regMaskTP dimMask = rpPredictTreeRegUse(tree->gtArrElem.gtArrInds[dim], PREDICT_SCRATCH_REG,
                                                        lockedRegs | regMask | dimsMask, rsvdRegs);
                if (dim == 0)
                    regMask |= dimMask;

                dimsMask |= dimMask;
            }
#ifdef _TARGET_XARCH_
            // INS_imul doesnt have an immediate constant.
            if (!jitIsScaleIndexMul(tree->gtArrElem.gtArrElemSize))
                regMask |= rpPredictRegPick(TYP_INT, PREDICT_SCRATCH_REG, lockedRegs | regMask | dimsMask);
#endif
            tree->gtUsedRegs = (regMaskSmall)(regMask | dimsMask);
            break;

        case GT_CMPXCHG:
        {
#ifdef _TARGET_XARCH_
            rsvdRegs |= RBM_EAX;
#endif
            if (tree->gtCmpXchg.gtOpLocation->OperGet() == GT_LCL_VAR)
            {
                regMask = rpPredictTreeRegUse(tree->gtCmpXchg.gtOpLocation, PREDICT_REG, lockedRegs, rsvdRegs);
            }
            else
            {
                regMask = rpPredictTreeRegUse(tree->gtCmpXchg.gtOpLocation, PREDICT_ADDR, lockedRegs, rsvdRegs);
            }
            op2Mask = rpPredictTreeRegUse(tree->gtCmpXchg.gtOpValue, PREDICT_REG, lockedRegs, rsvdRegs | regMask);

#ifdef _TARGET_XARCH_
            rsvdRegs &= ~RBM_EAX;
            tmpMask = rpPredictTreeRegUse(tree->gtCmpXchg.gtOpComparand, PREDICT_REG_EAX, lockedRegs,
                                          rsvdRegs | regMask | op2Mask);
            tree->gtUsedRegs = (regMaskSmall)(RBM_EAX | regMask | op2Mask | tmpMask);
            predictReg       = PREDICT_REG_EAX; // When this is done the result is always in EAX.
#else
            tmpMask          = 0;
            tree->gtUsedRegs = (regMaskSmall)(regMask | op2Mask | tmpMask);
#endif
        }
        break;

        case GT_ARR_BOUNDS_CHECK:
        {
            regMaskTP opArrLenRsvd = rsvdRegs | tree->gtBoundsChk.gtIndex->gtRsvdRegs;
            regMask = rpPredictTreeRegUse(tree->gtBoundsChk.gtArrLen, PREDICT_REG, lockedRegs, opArrLenRsvd);
            rpPredictTreeRegUse(tree->gtBoundsChk.gtIndex, PREDICT_REG, lockedRegs | regMask, RBM_LASTUSE);

            tree->gtUsedRegs =
                (regMaskSmall)regMask | tree->gtBoundsChk.gtArrLen->gtUsedRegs | tree->gtBoundsChk.gtIndex->gtUsedRegs;
        }
        break;

        default:
            NO_WAY("unexpected special operator in reg use prediction");
            break;
    }

RETURN_CHECK:

#ifdef DEBUG
    /* make sure we set them to something reasonable */
    if (tree->gtUsedRegs & RBM_ILLEGAL)
        noway_assert(!"used regs not set properly in reg use prediction");

    if (regMask & RBM_ILLEGAL)
        noway_assert(!"return value not set propery in reg use prediction");

#endif

    /*
     *  If the gtUsedRegs conflicts with lockedRegs
     *  then we going to have to spill some registers
     *  into the non-trashed register set to keep it alive
     */
    regMaskTP spillMask;
    spillMask = tree->gtUsedRegs & lockedRegs;

    if (spillMask)
    {
        while (spillMask)
        {
            /* Find the next register that needs to be spilled */
            tmpMask = genFindLowestBit(spillMask);

#ifdef DEBUG
            if (verbose)
            {
                printf("Predict spill  of   %s before: ", getRegName(genRegNumFromMask(tmpMask)));
                gtDispTree(tree, 0, NULL, true);
                if ((tmpMask & regMask) == 0)
                {
                    printf("Predict reload of   %s after : ", getRegName(genRegNumFromMask(tmpMask)));
                    gtDispTree(tree, 0, NULL, true);
                }
            }
#endif
            /* In Codegen it will typically introduce a spill temp here */
            /* rather than relocating the register to a non trashed reg */
            rpPredictSpillCnt++;

            /* Remove it from the spillMask */
            spillMask &= ~tmpMask;
        }
    }

    /*
     *  If the return registers in regMask conflicts with the lockedRegs
     *  then we allocate extra registers for the reload of the conflicting
     *  registers.
     *
     *  Set spillMask to the set of locked registers that have to be reloaded here.
     *  reloadMask is set to the extra registers that are used to reload
     *  the spilled lockedRegs.
     */

    noway_assert(regMask != DUMMY_INIT(RBM_ILLEGAL));
    spillMask = lockedRegs & regMask;

    if (spillMask)
    {
        /* Remove the spillMask from regMask */
        regMask &= ~spillMask;

        regMaskTP reloadMask = RBM_NONE;
        while (spillMask)
        {
            /* Get an extra register to hold it */
            regMaskTP reloadReg = rpPredictRegPick(TYP_INT, PREDICT_REG, lockedRegs | regMask | reloadMask);
#ifdef DEBUG
            if (verbose)
            {
                printf("Predict reload into %s after : ", getRegName(genRegNumFromMask(reloadReg)));
                gtDispTree(tree, 0, NULL, true);
            }
#endif
            reloadMask |= reloadReg;

            /* Remove it from the spillMask */
            spillMask &= ~genFindLowestBit(spillMask);
        }

        /* Update regMask to use the reloadMask */
        regMask |= reloadMask;

        /* update the gtUsedRegs mask */
        tree->gtUsedRegs |= (regMaskSmall)regMask;
    }

    regMaskTP regUse = tree->gtUsedRegs;
    regUse |= interferingRegs;

    if (!VarSetOps::IsEmpty(this, compCurLife))
    {
        // Add interference between the current set of live variables and
        //  the set of temporary registers need to evaluate the sub tree
        if (regUse)
        {
            rpRecordRegIntf(regUse, compCurLife DEBUGARG("tmp use"));
        }
    }

    if (rpAsgVarNum != -1)
    {
        // Add interference between the registers used (if any)
        // and the assignment target variable
        if (regUse)
        {
            rpRecordRegIntf(regUse, VarSetOps::MakeSingleton(this, rpAsgVarNum) DEBUGARG("tgt var tmp use"));
        }

        // Add a variable interference from rpAsgVarNum (i.e. the enregistered left hand
        // side of the assignment passed here using PREDICT_REG_VAR_Txx)
        // to the set of currently live variables. This new interference will prevent us
        // from using the register value used here for enregistering different live variable
        //
        if (!VarSetOps::IsEmpty(this, compCurLife))
        {
            rpRecordVarIntf(rpAsgVarNum, compCurLife DEBUGARG("asg tgt live conflict"));
        }
    }

    /* Do we need to resore the oldLastUseVars value */
    if (restoreLastUseVars)
    {
        /*  If we used a GT_ASG targeted register then we need to add
         *  a variable interference between any new last use variables
         *  and the GT_ASG targeted register
         */
        if (!VarSetOps::Equal(this, rpLastUseVars, oldLastUseVars) && rpAsgVarNum != -1)
        {
            rpRecordVarIntf(rpAsgVarNum, VarSetOps::Diff(this, rpLastUseVars, oldLastUseVars)
                                             DEBUGARG("asgn tgt last use conflict"));
        }
        VarSetOps::Assign(this, rpLastUseVars, oldLastUseVars);
    }

    return regMask;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#endif // LEGACY_BACKEND

/****************************************************************************/
/* Returns true when we must create an EBP frame
   This is used to force most managed methods to have EBP based frames
   which allows the ETW kernel stackwalker to walk the stacks of managed code
   this allows the kernel to perform light weight profiling
 */
bool Compiler::rpMustCreateEBPFrame(INDEBUG(const char** wbReason))
{
    bool result = false;
#ifdef DEBUG
    const char* reason = nullptr;
#endif

#if ETW_EBP_FRAMED
    if (!result && (opts.MinOpts() || opts.compDbgCode))
    {
        INDEBUG(reason = "Debug Code");
        result = true;
    }
    if (!result && (info.compMethodInfo->ILCodeSize > DEFAULT_MAX_INLINE_SIZE))
    {
        INDEBUG(reason = "IL Code Size");
        result = true;
    }
    if (!result && (fgBBcount > 3))
    {
        INDEBUG(reason = "BasicBlock Count");
        result = true;
    }
    if (!result && fgHasLoops)
    {
        INDEBUG(reason = "Method has Loops");
        result = true;
    }
    if (!result && (optCallCount >= 2))
    {
        INDEBUG(reason = "Call Count");
        result = true;
    }
    if (!result && (optIndirectCallCount >= 1))
    {
        INDEBUG(reason = "Indirect Call");
        result = true;
    }
#endif // ETW_EBP_FRAMED

    // VM wants to identify the containing frame of an InlinedCallFrame always
    // via the frame register never the stack register so we need a frame.
    if (!result && (optNativeCallCount != 0))
    {
        INDEBUG(reason = "Uses PInvoke");
        result = true;
    }

#ifdef _TARGET_ARM64_
    // TODO-ARM64-NYI: This is temporary: force a frame pointer-based frame until genFnProlog can handle non-frame
    // pointer frames.
    if (!result)
    {
        INDEBUG(reason = "Temporary ARM64 force frame pointer");
        result = true;
    }
#endif // _TARGET_ARM64_

#ifdef DEBUG
    if ((result == true) && (wbReason != nullptr))
    {
        *wbReason = reason;
    }
#endif

    return result;
}

#ifdef LEGACY_BACKEND // We don't use any of the old register allocator functions when LSRA is used instead.

/*****************************************************************************
 *
 *  Predict which variables will be assigned to registers
 *  This is x86 specific and only predicts the integer registers and
 *  must be conservative, any register that is predicted to be enregister
 *  must end up being enregistered.
 *
 *  The rpPredictTreeRegUse takes advantage of the LCL_VARS that are
 *  predicted to be enregistered to minimize calls to rpPredictRegPick.
 *
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
regMaskTP Compiler::rpPredictAssignRegVars(regMaskTP regAvail)
{
    unsigned regInx;

    if (rpPasses <= rpPassesPessimize)
    {
        // Assume that we won't have to reverse EBP enregistration
        rpReverseEBPenreg = false;

        // Set the default rpFrameType based upon codeGen->isFramePointerRequired()
        if (codeGen->isFramePointerRequired() || codeGen->isFrameRequired())
            rpFrameType = FT_EBP_FRAME;
        else
            rpFrameType = FT_ESP_FRAME;
    }

#if !ETW_EBP_FRAMED
    // If we are using FPBASE as the frame register, we cannot also use it for
    // a local var
    if (rpFrameType == FT_EBP_FRAME)
    {
        regAvail &= ~RBM_FPBASE;
    }
#endif // !ETW_EBP_FRAMED

    rpStkPredict        = 0;
    rpPredictAssignMask = regAvail;

    raSetupArgMasks(&codeGen->intRegState);
#if !FEATURE_STACK_FP_X87
    raSetupArgMasks(&codeGen->floatRegState);
#endif

    // If there is a secret stub param, it is also live in
    if (info.compPublishStubParam)
    {
        codeGen->intRegState.rsCalleeRegArgMaskLiveIn |= RBM_SECRET_STUB_PARAM;
    }

    if (regAvail == RBM_NONE)
    {
        unsigned   lclNum;
        LclVarDsc* varDsc;

        for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
        {
#if FEATURE_STACK_FP_X87
            if (!varDsc->IsFloatRegType())
#endif
            {
                varDsc->lvRegNum = REG_STK;
                if (isRegPairType(varDsc->lvType))
                    varDsc->lvOtherReg = REG_STK;
            }
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nCompiler::rpPredictAssignRegVars pass #%d", rpPasses);
        printf("\n        Available registers = ");
        dspRegMask(regAvail);
        printf("\n");
    }
#endif

    if (regAvail == RBM_NONE)
    {
        return RBM_NONE;
    }

    /* We cannot change the lvVarIndexes at this point, so we  */
    /* can only re-order the existing set of tracked variables */
    /* Which will change the order in which we select the      */
    /* locals for enregistering.                               */

    assert(lvaTrackedFixed); // We should have already set this to prevent us from adding any new tracked variables.

    // Should not be set unless optimizing
    noway_assert((lvaSortAgain == false) || (opts.MinOpts() == false));

    if (lvaSortAgain)
        lvaSortOnly();

#ifdef DEBUG
    fgDebugCheckBBlist();
#endif

    /* Initialize the weighted count of variables that could have */
    /* been enregistered but weren't */
    unsigned refCntStk    = 0; // sum of     ref counts for all stack based variables
    unsigned refCntEBP    = 0; // sum of     ref counts for EBP enregistered variables
    unsigned refCntWtdEBP = 0; // sum of wtd ref counts for EBP enregistered variables
#if DOUBLE_ALIGN
    unsigned refCntStkParam;  // sum of     ref counts for all stack based parameters
    unsigned refCntWtdStkDbl; // sum of wtd ref counts for stack based doubles

#if FEATURE_STACK_FP_X87
    refCntStkParam  = raCntStkParamDblStackFP;
    refCntWtdStkDbl = raCntWtdStkDblStackFP;
    refCntStk       = raCntStkStackFP;
#else
    refCntStkParam  = 0;
    refCntWtdStkDbl = 0;
    refCntStk       = 0;
#endif // FEATURE_STACK_FP_X87

#endif // DOUBLE_ALIGN

    /* Set of registers used to enregister variables in the predition */
    regMaskTP regUsed = RBM_NONE;

    /*-------------------------------------------------------------------------
     *
     *  Predict/Assign the enregistered locals in ref-count order
     *
     */

    VARSET_TP unprocessedVars(VarSetOps::MakeFull(this));

    unsigned FPRegVarLiveInCnt;
    FPRegVarLiveInCnt = 0; // How many enregistered doubles are live on entry to the method

    LclVarDsc* varDsc;
    for (unsigned sortNum = 0; sortNum < lvaCount; sortNum++)
    {
        bool notWorthy = false;

        unsigned  varIndex;
        bool      isDouble;
        regMaskTP regAvailForType;
        var_types regType;
        regMaskTP avoidReg;
        unsigned  customVarOrderSize;
        regNumber customVarOrder[MAX_VAR_ORDER_SIZE];
        bool      firstHalf;
        regNumber saveOtherReg;

        varDsc = lvaRefSorted[sortNum];

#if FEATURE_STACK_FP_X87
        if (varTypeIsFloating(varDsc->TypeGet()))
        {
#ifdef DEBUG
            if (lvaIsFieldOfDependentlyPromotedStruct(varDsc))
            {
                // Field local of a PROMOTION_TYPE_DEPENDENT struct should not
                // be en-registered.
                noway_assert(!varDsc->lvRegister);
            }
#endif
            continue;
        }
#endif

        /* Check the set of invariant things that would prevent enregistration */

        /* Ignore the variable if it's not tracked */
        if (!varDsc->lvTracked)
            goto CANT_REG;

        /* Get hold of the index and the interference mask for the variable */
        varIndex = varDsc->lvVarIndex;

        // Remove 'varIndex' from unprocessedVars
        VarSetOps::RemoveElemD(this, unprocessedVars, varIndex);

        // Skip the variable if it's marked as DoNotEnregister.

        if (varDsc->lvDoNotEnregister)
            goto CANT_REG;

        /* TODO: For now if we have JMP all register args go to stack
         * TODO: Later consider extending the life of the argument or make a copy of it */

        if (compJmpOpUsed && varDsc->lvIsRegArg)
            goto CANT_REG;

        /* Skip the variable if the ref count is zero */

        if (varDsc->lvRefCnt == 0)
            goto CANT_REG;

        /* Ignore field of PROMOTION_TYPE_DEPENDENT type of promoted struct */

        if (lvaIsFieldOfDependentlyPromotedStruct(varDsc))
        {
            goto CANT_REG;
        }

        /* Is the unweighted ref count too low to be interesting? */

        if (!varDsc->lvIsStructField && // We do encourage enregistering field locals.
            (varDsc->lvRefCnt <= 1))
        {
            /* Sometimes it's useful to enregister a variable with only one use */
            /*   arguments referenced in loops are one example */

            if (varDsc->lvIsParam && varDsc->lvRefCntWtd > BB_UNITY_WEIGHT)
                goto OK_TO_ENREGISTER;

            /* If the variable has a preferred register set it may be useful to put it there */
            if (varDsc->lvPrefReg && varDsc->lvIsRegArg)
                goto OK_TO_ENREGISTER;

            /* Keep going; the table is sorted by "weighted" ref count */
            goto CANT_REG;
        }

    OK_TO_ENREGISTER:

        if (varTypeIsFloating(varDsc->TypeGet()))
        {
            regType         = varDsc->TypeGet();
            regAvailForType = regAvail & RBM_ALLFLOAT;
        }
        else
        {
            regType         = TYP_INT;
            regAvailForType = regAvail & RBM_ALLINT;
        }

#ifdef _TARGET_ARM_
        isDouble = (varDsc->TypeGet() == TYP_DOUBLE);

        if (isDouble)
        {
            regAvailForType &= RBM_DBL_REGS; // We must restrict the set to the double registers
        }
#endif

        /* If we don't have any registers available then skip the enregistration attempt */
        if (regAvailForType == RBM_NONE)
            goto NO_REG;

        // On the pessimize passes don't even try to enregister LONGS
        if (isRegPairType(varDsc->lvType))
        {
            if (rpPasses > rpPassesPessimize)
                goto NO_REG;
            else if (rpLostEnreg && (rpPasses == rpPassesPessimize))
                goto NO_REG;
        }

        // Set of registers to avoid when performing register allocation
        avoidReg = RBM_NONE;

        if (!varDsc->lvIsRegArg)
        {
            /* For local variables,
             *  avoid the incoming arguments,
             *  but only if you conflict with them */

            if (raAvoidArgRegMask != 0)
            {
                LclVarDsc* argDsc;
                LclVarDsc* argsEnd = lvaTable + info.compArgsCount;

                for (argDsc = lvaTable; argDsc < argsEnd; argDsc++)
                {
                    if (!argDsc->lvIsRegArg)
                        continue;

                    bool      isFloat  = argDsc->IsFloatRegType();
                    regNumber inArgReg = argDsc->lvArgReg;
                    regMaskTP inArgBit = genRegMask(inArgReg);

                    // Is this inArgReg in the raAvoidArgRegMask set?

                    if (!(raAvoidArgRegMask & inArgBit))
                        continue;

                    noway_assert(argDsc->lvIsParam);
                    noway_assert(inArgBit & (isFloat ? RBM_FLTARG_REGS : RBM_ARG_REGS));

                    unsigned locVarIndex = varDsc->lvVarIndex;
                    unsigned argVarIndex = argDsc->lvVarIndex;

                    /* Does this variable interfere with the arg variable ? */
                    if (VarSetOps::IsMember(this, lvaVarIntf[locVarIndex], argVarIndex))
                    {
                        noway_assert(VarSetOps::IsMember(this, lvaVarIntf[argVarIndex], locVarIndex));
                        /* Yes, so try to avoid the incoming arg reg */
                        avoidReg |= inArgBit;
                    }
                    else
                    {
                        noway_assert(!VarSetOps::IsMember(this, lvaVarIntf[argVarIndex], locVarIndex));
                    }
                }
            }
        }

        // Now we will try to predict which register the variable
        // could  be enregistered in

        customVarOrderSize = MAX_VAR_ORDER_SIZE;

        raSetRegVarOrder(regType, customVarOrder, &customVarOrderSize, varDsc->lvPrefReg, avoidReg);

        firstHalf    = false;
        saveOtherReg = DUMMY_INIT(REG_NA);

        for (regInx = 0; regInx < customVarOrderSize; regInx++)
        {
            regNumber regNum  = customVarOrder[regInx];
            regMaskTP regBits = genRegMask(regNum);

            /* Skip this register if it isn't available */
            if ((regAvailForType & regBits) == 0)
                continue;

            /* Skip this register if it interferes with the variable */

            if (VarSetOps::IsMember(this, raLclRegIntf[regNum], varIndex))
                continue;

            if (varTypeIsFloating(regType))
            {
#ifdef _TARGET_ARM_
                if (isDouble)
                {
                    regNumber regNext = REG_NEXT(regNum);
                    regBits |= genRegMask(regNext);

                    /* Skip if regNext interferes with the variable */
                    if (VarSetOps::IsMember(this, raLclRegIntf[regNext], varIndex))
                        continue;
                }
#endif
            }

            bool firstUseOfReg     = ((regBits & (regUsed | codeGen->regSet.rsGetModifiedRegsMask())) == 0);
            bool lessThanTwoRefWtd = (varDsc->lvRefCntWtd < (2 * BB_UNITY_WEIGHT));
            bool calleeSavedReg    = ((regBits & RBM_CALLEE_SAVED) != 0);

            /* Skip this register if the weighted ref count is less than two
               and we are considering a unused callee saved register */

            if (lessThanTwoRefWtd && // less than two references (weighted)
                firstUseOfReg &&     // first use of this register
                calleeSavedReg)      // callee saved register
            {
                unsigned int totalRefCntWtd = varDsc->lvRefCntWtd;

                // psc is abbeviation for possibleSameColor
                VARSET_TP pscVarSet(VarSetOps::Diff(this, unprocessedVars, lvaVarIntf[varIndex]));

                VARSET_ITER_INIT(this, pscIndexIter, pscVarSet, pscIndex);
                while (pscIndexIter.NextElem(&pscIndex))
                {
                    LclVarDsc* pscVar = lvaTable + lvaTrackedToVarNum[pscIndex];
                    totalRefCntWtd += pscVar->lvRefCntWtd;
                    if (totalRefCntWtd > (2 * BB_UNITY_WEIGHT))
                        break;
                }

                if (totalRefCntWtd <= (2 * BB_UNITY_WEIGHT))
                {
                    notWorthy = true;
                    continue; // not worth spilling a callee saved register
                }
                // otherwise we will spill this callee saved registers,
                // because its uses when combined with the uses of
                // other yet to be processed candidates exceed our threshold.
                // totalRefCntWtd = totalRefCntWtd;
            }

            /* Looks good - mark the variable as living in the register */

            if (isRegPairType(varDsc->lvType))
            {
                if (firstHalf == false)
                {
                    /* Enregister the first half of the long */
                    varDsc->lvRegNum   = regNum;
                    saveOtherReg       = varDsc->lvOtherReg;
                    varDsc->lvOtherReg = REG_STK;
                    firstHalf          = true;
                }
                else
                {
                    /* Ensure 'well-formed' register pairs */
                    /* (those returned by gen[Pick|Grab]RegPair) */

                    if (regNum < varDsc->lvRegNum)
                    {
                        varDsc->lvOtherReg = varDsc->lvRegNum;
                        varDsc->lvRegNum   = regNum;
                    }
                    else
                    {
                        varDsc->lvOtherReg = regNum;
                    }
                    firstHalf = false;
                }
            }
            else
            {
                varDsc->lvRegNum = regNum;
#ifdef _TARGET_ARM_
                if (isDouble)
                {
                    varDsc->lvOtherReg = REG_NEXT(regNum);
                }
#endif
            }

            if (regNum == REG_FPBASE)
            {
                refCntEBP += varDsc->lvRefCnt;
                refCntWtdEBP += varDsc->lvRefCntWtd;
#if DOUBLE_ALIGN
                if (varDsc->lvIsParam)
                {
                    refCntStkParam += varDsc->lvRefCnt;
                }
#endif
            }

            /* Record this register in the regUsed set */
            regUsed |= regBits;

            /* The register is now ineligible for all interfering variables */

            VarSetOps::UnionD(this, raLclRegIntf[regNum], lvaVarIntf[varIndex]);

#ifdef _TARGET_ARM_
            if (isDouble)
            {
                regNumber secondHalf = REG_NEXT(regNum);
                VARSET_ITER_INIT(this, iter, lvaVarIntf[varIndex], intfIndex);
                while (iter.NextElem(&intfIndex))
                {
                    VarSetOps::AddElemD(this, raLclRegIntf[secondHalf], intfIndex);
                }
            }
#endif

            /* If a register argument, remove its incoming register
             * from the "avoid" list */

            if (varDsc->lvIsRegArg)
            {
                raAvoidArgRegMask &= ~genRegMask(varDsc->lvArgReg);
#ifdef _TARGET_ARM_
                if (isDouble)
                {
                    raAvoidArgRegMask &= ~genRegMask(REG_NEXT(varDsc->lvArgReg));
                }
#endif
            }

            /* A variable of TYP_LONG can take two registers */
            if (firstHalf)
                continue;

            // Since we have successfully enregistered this variable it is
            // now time to move on and consider the next variable
            goto ENREG_VAR;
        }

        if (firstHalf)
        {
            noway_assert(isRegPairType(varDsc->lvType));

            /* This TYP_LONG is partially enregistered */

            noway_assert(saveOtherReg != DUMMY_INIT(REG_NA));

            if (varDsc->lvDependReg && (saveOtherReg != REG_STK))
            {
                rpLostEnreg = true;
            }

            raAddToStkPredict(varDsc->lvRefCntWtd);
            goto ENREG_VAR;
        }

    NO_REG:;
        if (varDsc->lvDependReg)
        {
            rpLostEnreg = true;
        }

        if (!notWorthy)
        {
            /* Weighted count of variables that could have been enregistered but weren't */
            raAddToStkPredict(varDsc->lvRefCntWtd);

            if (isRegPairType(varDsc->lvType) && (varDsc->lvOtherReg == REG_STK))
                raAddToStkPredict(varDsc->lvRefCntWtd);
        }

    CANT_REG:;
        varDsc->lvRegister = false;

        varDsc->lvRegNum = REG_STK;
        if (isRegPairType(varDsc->lvType))
            varDsc->lvOtherReg = REG_STK;

        /* unweighted count of variables that were not enregistered */

        refCntStk += varDsc->lvRefCnt;

#if DOUBLE_ALIGN
        if (varDsc->lvIsParam)
        {
            refCntStkParam += varDsc->lvRefCnt;
        }
        else
        {
            /* Is it a stack based double? */
            /* Note that double params are excluded since they can not be double aligned */
            if (varDsc->lvType == TYP_DOUBLE)
            {
                refCntWtdStkDbl += varDsc->lvRefCntWtd;
            }
        }
#endif
#ifdef DEBUG
        if (verbose)
        {
            printf("; ");
            gtDispLclVar((unsigned)(varDsc - lvaTable));
            if (varDsc->lvTracked)
                printf("T%02u", varDsc->lvVarIndex);
            else
                printf("   ");
            printf(" (refcnt=%2u,refwtd=%s) not enregistered", varDsc->lvRefCnt, refCntWtd2str(varDsc->lvRefCntWtd));
            if (varDsc->lvDoNotEnregister)
                printf(", do-not-enregister");
            printf("\n");
        }
#endif
        continue;

    ENREG_VAR:;

        varDsc->lvRegister = true;

        // Record the fact that we enregistered a stack arg when tail call is used.
        if (compJmpOpUsed && !varDsc->lvIsRegArg)
        {
            rpMaskPInvokeEpilogIntf |= genRegMask(varDsc->lvRegNum);
            if (isRegPairType(varDsc->lvType))
            {
                rpMaskPInvokeEpilogIntf |= genRegMask(varDsc->lvOtherReg);
            }
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("; ");
            gtDispLclVar((unsigned)(varDsc - lvaTable));
            printf("T%02u (refcnt=%2u,refwtd=%s) predicted to be assigned to ", varIndex, varDsc->lvRefCnt,
                   refCntWtd2str(varDsc->lvRefCntWtd));
            varDsc->PrintVarReg();
#ifdef _TARGET_ARM_
            if (isDouble)
            {
                printf(":%s", getRegName(varDsc->lvOtherReg));
            }
#endif
            printf("\n");
        }
#endif
    }

#if ETW_EBP_FRAMED
    noway_assert(refCntEBP == 0);
#endif

#ifdef DEBUG
    if (verbose)
    {
        if (refCntStk > 0)
            printf("; refCntStk       = %u\n", refCntStk);
        if (refCntEBP > 0)
            printf("; refCntEBP       = %u\n", refCntEBP);
        if (refCntWtdEBP > 0)
            printf("; refCntWtdEBP    = %u\n", refCntWtdEBP);
#if DOUBLE_ALIGN
        if (refCntStkParam > 0)
            printf("; refCntStkParam  = %u\n", refCntStkParam);
        if (refCntWtdStkDbl > 0)
            printf("; refCntWtdStkDbl = %u\n", refCntWtdStkDbl);
#endif
    }
#endif

    /* Determine how the EBP register should be used */
    CLANG_FORMAT_COMMENT_ANCHOR;

#if DOUBLE_ALIGN

    if (!codeGen->isFramePointerRequired())
    {
        noway_assert(getCanDoubleAlign() < COUNT_DOUBLE_ALIGN);

        /*
            First let us decide if we should use EBP to create a
            double-aligned frame, instead of enregistering variables
        */

        if (getCanDoubleAlign() == MUST_DOUBLE_ALIGN)
        {
            rpFrameType = FT_DOUBLE_ALIGN_FRAME;
            goto REVERSE_EBP_ENREG;
        }

        if (getCanDoubleAlign() == CAN_DOUBLE_ALIGN && (refCntWtdStkDbl > 0))
        {
            if (shouldDoubleAlign(refCntStk, refCntEBP, refCntWtdEBP, refCntStkParam, refCntWtdStkDbl))
            {
                rpFrameType = FT_DOUBLE_ALIGN_FRAME;
                goto REVERSE_EBP_ENREG;
            }
        }
    }

#endif // DOUBLE_ALIGN

    if (!codeGen->isFramePointerRequired() && !codeGen->isFrameRequired())
    {
#ifdef _TARGET_XARCH_
// clang-format off
        /*  If we are using EBP to enregister variables then
            will we actually save bytes by setting up an EBP frame?

            Each stack reference is an extra byte of code if we use
            an ESP frame.

            Here we measure the savings that we get by using EBP to
            enregister variables vs. the cost in code size that we
            pay when using an ESP based frame.

            We pay one byte of code for each refCntStk
            but we save one byte (or more) for each refCntEBP.

            Our savings are the elimination of a stack memory read/write.
            We use the loop weighted value of
               refCntWtdEBP * mem_access_weight (0, 3, 6)
            to represent this savings.
         */

        // We also pay 5 extra bytes for the MOV EBP,ESP and LEA ESP,[EBP-0x10]
        // to set up an EBP frame in the prolog and epilog
        #define EBP_FRAME_SETUP_SIZE  5
        // clang-format on

        if (refCntStk > (refCntEBP + EBP_FRAME_SETUP_SIZE))
        {
            unsigned bytesSaved        = refCntStk - (refCntEBP + EBP_FRAME_SETUP_SIZE);
            unsigned mem_access_weight = 3;

            if (compCodeOpt() == SMALL_CODE)
                mem_access_weight = 0;
            else if (compCodeOpt() == FAST_CODE)
                mem_access_weight *= 2;

            if (bytesSaved > ((refCntWtdEBP * mem_access_weight) / BB_UNITY_WEIGHT))
            {
                /* It's not be a good idea to use EBP in our predictions */
                CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef DEBUG
                if (verbose && (refCntEBP > 0))
                    printf("; Predicting that it's not worth using EBP to enregister variables\n");
#endif
                rpFrameType = FT_EBP_FRAME;
                goto REVERSE_EBP_ENREG;
            }
        }
#endif // _TARGET_XARCH_

        if ((rpFrameType == FT_NOT_SET) || (rpFrameType == FT_ESP_FRAME))
        {
#ifdef DEBUG
            const char* reason;
#endif
            if (rpMustCreateEBPCalled == false)
            {
                rpMustCreateEBPCalled = true;
                if (rpMustCreateEBPFrame(INDEBUG(&reason)))
                {
#ifdef DEBUG
                    if (verbose)
                        printf("; Decided to create an EBP based frame for ETW stackwalking (%s)\n", reason);
#endif
                    codeGen->setFrameRequired(true);

                    rpFrameType = FT_EBP_FRAME;
                    goto REVERSE_EBP_ENREG;
                }
            }
        }
    }

    goto EXIT;

REVERSE_EBP_ENREG:

    noway_assert(rpFrameType != FT_ESP_FRAME);

    rpReverseEBPenreg = true;

#if !ETW_EBP_FRAMED
    if (refCntEBP > 0)
    {
        noway_assert(regUsed & RBM_FPBASE);

        regUsed &= ~RBM_FPBASE;

        /* variables that were enregistered in EBP become stack based variables */
        raAddToStkPredict(refCntWtdEBP);

        unsigned lclNum;

        /* We're going to have to undo some predicted enregistered variables */
        for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
        {
            /* Is this a register variable? */
            if (varDsc->lvRegNum != REG_STK)
            {
                if (isRegPairType(varDsc->lvType))
                {
                    /* Only one can be EBP */
                    if (varDsc->lvRegNum == REG_FPBASE || varDsc->lvOtherReg == REG_FPBASE)
                    {
                        if (varDsc->lvRegNum == REG_FPBASE)
                            varDsc->lvRegNum = varDsc->lvOtherReg;

                        varDsc->lvOtherReg = REG_STK;

                        if (varDsc->lvRegNum == REG_STK)
                            varDsc->lvRegister = false;

                        if (varDsc->lvDependReg)
                            rpLostEnreg = true;
#ifdef DEBUG
                        if (verbose)
                            goto DUMP_MSG;
#endif
                    }
                }
                else
                {
                    if ((varDsc->lvRegNum == REG_FPBASE) && (!varDsc->IsFloatRegType()))
                    {
                        varDsc->lvRegNum = REG_STK;

                        varDsc->lvRegister = false;

                        if (varDsc->lvDependReg)
                            rpLostEnreg = true;
#ifdef DEBUG
                        if (verbose)
                        {
                        DUMP_MSG:
                            printf("; reversing enregisteration of V%02u,T%02u (refcnt=%2u,refwtd=%4u%s)\n", lclNum,
                                   varDsc->lvVarIndex, varDsc->lvRefCnt, varDsc->lvRefCntWtd / 2,
                                   (varDsc->lvRefCntWtd & 1) ? ".5" : "");
                        }
#endif
                    }
                }
            }
        }
    }
#endif // ETW_EBP_FRAMED

EXIT:;

    unsigned lclNum;
    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        /* Clear the lvDependReg flag for next iteration of the predictor */
        varDsc->lvDependReg = false;

        // If we set rpLostEnreg and this is the first pessimize pass
        // then reverse the enreg of all TYP_LONG
        if (rpLostEnreg && isRegPairType(varDsc->lvType) && (rpPasses == rpPassesPessimize))
        {
            varDsc->lvRegNum   = REG_STK;
            varDsc->lvOtherReg = REG_STK;
        }
    }

#ifdef DEBUG
    if (verbose && raNewBlocks)
    {
        printf("\nAdded FP register killing blocks:\n");
        fgDispBasicBlocks();
        printf("\n");
    }
#endif
    noway_assert(rpFrameType != FT_NOT_SET);

    /* return the set of registers used to enregister variables */
    return regUsed;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************
 *
 *  Predict register use for every tree in the function. Note that we do this
 *  at different times (not to mention in a totally different way) for x86 vs
 *  RISC targets.
 */
void Compiler::rpPredictRegUse()
{
#ifdef DEBUG
    if (verbose)
        raDumpVarIntf();
#endif

    // We might want to adjust the ref counts based on interference
    raAdjustVarIntf();

    regMaskTP allAcceptableRegs = RBM_ALLINT;

#if FEATURE_FP_REGALLOC
    allAcceptableRegs |= raConfigRestrictMaskFP();
#endif

    allAcceptableRegs &= ~codeGen->regSet.rsMaskResvd; // Remove any register reserved for special purposes

    /* For debuggable code, genJumpToThrowHlpBlk() generates an inline call
       to acdHelper(). This is done implicitly, without creating a GT_CALL
       node. Hence, this interference is be handled implicitly by
       restricting the registers used for enregistering variables */

    if (opts.compDbgCode)
    {
        allAcceptableRegs &= RBM_CALLEE_SAVED;
    }

    /* Compute the initial regmask to use for the first pass */
    regMaskTP regAvail = RBM_CALLEE_SAVED & allAcceptableRegs;
    regMaskTP regUsed;

#if CPU_USES_BLOCK_MOVE
    /* If we might need to generate a rep mov instruction */
    /* remove ESI and EDI */
    if (compBlkOpUsed)
        regAvail &= ~(RBM_ESI | RBM_EDI);
#endif

#ifdef _TARGET_X86_
    /* If we using longs then we remove ESI to allow */
    /* ESI:EBX to be saved accross a call */
    if (compLongUsed)
        regAvail &= ~(RBM_ESI);
#endif

#ifdef _TARGET_ARM_
    // For the first register allocation pass we don't want to color using r4
    // as we want to allow it to be used to color the internal temps instead
    // when r0,r1,r2,r3 are all in use.
    //
    regAvail &= ~(RBM_R4);
#endif

#if ETW_EBP_FRAMED
    // We never have EBP available when ETW_EBP_FRAME is defined
    regAvail &= ~RBM_FPBASE;
#else
    /* If a frame pointer is required then we remove EBP */
    if (codeGen->isFramePointerRequired() || codeGen->isFrameRequired())
        regAvail &= ~RBM_FPBASE;
#endif

#ifdef DEBUG
    BOOL fJitNoRegLoc = JitConfig.JitNoRegLoc();
    if (fJitNoRegLoc)
        regAvail = RBM_NONE;
#endif

    if ((opts.compFlags & CLFLG_REGVAR) == 0)
        regAvail = RBM_NONE;

#if FEATURE_STACK_FP_X87
    VarSetOps::AssignNoCopy(this, optAllNonFPvars, VarSetOps::MakeEmpty(this));
    VarSetOps::AssignNoCopy(this, optAllFloatVars, VarSetOps::MakeEmpty(this));

    // Calculate the set of all tracked FP/non-FP variables
    //  into optAllFloatVars and optAllNonFPvars

    unsigned   lclNum;
    LclVarDsc* varDsc;

    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        /* Ignore the variable if it's not tracked */

        if (!varDsc->lvTracked)
            continue;

        /* Get hold of the index and the interference mask for the variable */

        unsigned varNum = varDsc->lvVarIndex;

        /* add to the set of all tracked FP/non-FP variables */

        if (varDsc->IsFloatRegType())
            VarSetOps::AddElemD(this, optAllFloatVars, varNum);
        else
            VarSetOps::AddElemD(this, optAllNonFPvars, varNum);
    }
#endif

    for (unsigned i = 0; i < REG_COUNT; i++)
    {
        VarSetOps::AssignNoCopy(this, raLclRegIntf[i], VarSetOps::MakeEmpty(this));
    }
    for (unsigned i = 0; i < lvaTrackedCount; i++)
    {
        VarSetOps::AssignNoCopy(this, lvaVarPref[i], VarSetOps::MakeEmpty(this));
    }

    raNewBlocks          = false;
    rpPredictAssignAgain = false;
    rpPasses             = 0;

    bool      mustPredict   = true;
    unsigned  stmtNum       = 0;
    unsigned  oldStkPredict = DUMMY_INIT(~0);
    VARSET_TP oldLclRegIntf[REG_COUNT];

    for (unsigned i = 0; i < REG_COUNT; i++)
    {
        VarSetOps::AssignNoCopy(this, oldLclRegIntf[i], VarSetOps::MakeEmpty(this));
    }

    while (true)
    {
        /* Assign registers to variables using the variable/register interference
           graph (raLclRegIntf[]) calculated in the previous pass */
        regUsed = rpPredictAssignRegVars(regAvail);

        mustPredict |= rpLostEnreg;

#ifdef _TARGET_ARM_

        // See if we previously reserved REG_R10 and try to make it available if we have a small frame now
        //
        if ((rpPasses == 0) && (codeGen->regSet.rsMaskResvd & RBM_OPT_RSVD))
        {
            if (compRsvdRegCheck(REGALLOC_FRAME_LAYOUT))
            {
                // We must keep reserving R10 in this case
                codeGen->regSet.rsMaskResvd |= RBM_OPT_RSVD;
            }
            else
            {
                // We can release our reservation on R10 and use it to color registers
                //
                codeGen->regSet.rsMaskResvd &= ~RBM_OPT_RSVD;
                allAcceptableRegs |= RBM_OPT_RSVD;
            }
        }
#endif

        /* Is our new prediction good enough?? */
        if (!mustPredict)
        {
            /* For small methods (less than 12 stmts), we add a    */
            /*   extra pass if we are predicting the use of some   */
            /*   of the caller saved registers.                    */
            /* This fixes RAID perf bug 43440 VB Ackerman function */

            if ((rpPasses == 1) && (stmtNum <= 12) && (regUsed & RBM_CALLEE_SAVED))
            {
                goto EXTRA_PASS;
            }

            /* If every variable was fully enregistered then we're done */
            if (rpStkPredict == 0)
                goto ALL_DONE;

            // This was a successful prediction.  Record it, in case it turns out to be the best one.
            rpRecordPrediction();

            if (rpPasses > 1)
            {
                noway_assert(oldStkPredict != (unsigned)DUMMY_INIT(~0));

                // Be careful about overflow
                unsigned highStkPredict = (rpStkPredict * 2 < rpStkPredict) ? ULONG_MAX : rpStkPredict * 2;
                if (oldStkPredict < highStkPredict)
                    goto ALL_DONE;

                if (rpStkPredict < rpPasses * 8)
                    goto ALL_DONE;

                if (rpPasses >= (rpPassesMax - 1))
                    goto ALL_DONE;
            }

        EXTRA_PASS:
            /* We will do another pass */;
        }

#ifdef DEBUG
        if (JitConfig.JitAssertOnMaxRAPasses())
        {
            noway_assert(rpPasses < rpPassesMax &&
                         "This may not a bug, but dev team should look and see what is happening");
        }
#endif

        // The "64" here had been "VARSET_SZ".  It is unclear why this number is connected with
        // the (max) size of a VARSET.  We've eliminated this constant, so I left this as a constant.  We hope
        // that we're phasing out this code, anyway, and this leaves the behavior the way that it was.
        if (rpPasses > (rpPassesMax - rpPassesPessimize) + 64)
        {
            NO_WAY("we seem to be stuck in an infinite loop. breaking out");
        }

#ifdef DEBUG
        if (verbose)
        {
            if (rpPasses > 0)
            {
                if (rpLostEnreg)
                    printf("\n; Another pass due to rpLostEnreg");
                if (rpAddedVarIntf)
                    printf("\n; Another pass due to rpAddedVarIntf");
                if ((rpPasses == 1) && rpPredictAssignAgain)
                    printf("\n; Another pass due to rpPredictAssignAgain");
            }
            printf("\n; Register predicting pass# %d\n", rpPasses + 1);
        }
#endif

        /*  Zero the variable/register interference graph */
        for (unsigned i = 0; i < REG_COUNT; i++)
        {
            VarSetOps::OldStyleClearD(this, raLclRegIntf[i]);
        }

        // if there are PInvoke calls and compLvFrameListRoot is enregistered,
        // it must not be in a register trashed by the callee
        if (info.compCallUnmanaged != 0)
        {
            assert(!opts.ShouldUsePInvokeHelpers());
            noway_assert(info.compLvFrameListRoot < lvaCount);

            LclVarDsc* pinvokeVarDsc = &lvaTable[info.compLvFrameListRoot];

            if (pinvokeVarDsc->lvTracked)
            {
                rpRecordRegIntf(RBM_CALLEE_TRASH, VarSetOps::MakeSingleton(this, pinvokeVarDsc->lvVarIndex)
                                                      DEBUGARG("compLvFrameListRoot"));

                // We would prefer to have this be enregister in the PINVOKE_TCB register
                pinvokeVarDsc->addPrefReg(RBM_PINVOKE_TCB, this);
            }

            // If we're using a single return block, the p/invoke epilog code trashes ESI and EDI (in the
            // worst case).  Make sure that the return value compiler temp that we create for the single
            // return block knows about this interference.
            if (genReturnLocal != BAD_VAR_NUM)
            {
                noway_assert(genReturnBB);
                LclVarDsc* localTmp = &lvaTable[genReturnLocal];
                if (localTmp->lvTracked)
                {
                    rpRecordRegIntf(RBM_PINVOKE_TCB | RBM_PINVOKE_FRAME,
                                    VarSetOps::MakeSingleton(this, localTmp->lvVarIndex) DEBUGARG("genReturnLocal"));
                }
            }
        }

#ifdef _TARGET_ARM_
        if (compFloatingPointUsed)
        {
            bool hasMustInitFloat = false;

            // if we have any must-init floating point LclVars then we will add register interferences
            // for the arguments with RBM_SCRATCH
            // this is so that if we need to reset the initReg to REG_SCRATCH in Compiler::genFnProlog()
            // we won't home the arguments into REG_SCRATCH

            unsigned   lclNum;
            LclVarDsc* varDsc;

            for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
            {
                if (varDsc->lvMustInit && varTypeIsFloating(varDsc->TypeGet()))
                {
                    hasMustInitFloat = true;
                    break;
                }
            }

            if (hasMustInitFloat)
            {
                for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
                {
                    // If is an incoming argument, that is tracked and not floating-point
                    if (varDsc->lvIsParam && varDsc->lvTracked && !varTypeIsFloating(varDsc->TypeGet()))
                    {
                        rpRecordRegIntf(RBM_SCRATCH, VarSetOps::MakeSingleton(this, varDsc->lvVarIndex)
                                                         DEBUGARG("arg home with must-init fp"));
                    }
                }
            }
        }
#endif

        stmtNum        = 0;
        rpAddedVarIntf = false;
        rpLostEnreg    = false;

        /* Walk the basic blocks and predict reg use for each tree */

        for (BasicBlock* block = fgFirstBB; block != NULL; block = block->bbNext)
        {
            GenTreePtr stmt;
            compCurBB       = block;
            compCurLifeTree = NULL;
            VarSetOps::Assign(this, compCurLife, block->bbLiveIn);

            compCurBB = block;

            for (stmt = block->FirstNonPhiDef(); stmt != NULL; stmt = stmt->gtNext)
            {
                noway_assert(stmt->gtOper == GT_STMT);

                rpPredictSpillCnt = 0;
                VarSetOps::AssignNoCopy(this, rpLastUseVars, VarSetOps::MakeEmpty(this));
                VarSetOps::AssignNoCopy(this, rpUseInPlace, VarSetOps::MakeEmpty(this));

                GenTreePtr tree = stmt->gtStmt.gtStmtExpr;
                stmtNum++;
#ifdef DEBUG
                if (verbose && 1)
                {
                    printf("\nRegister predicting BB%02u, stmt %d\n", block->bbNum, stmtNum);
                    gtDispTree(tree);
                    printf("\n");
                }
#endif
                rpPredictTreeRegUse(tree, PREDICT_NONE, RBM_NONE, RBM_NONE);

                noway_assert(rpAsgVarNum == -1);

                if (rpPredictSpillCnt > tmpIntSpillMax)
                    tmpIntSpillMax = rpPredictSpillCnt;
            }
        }
        rpPasses++;

        /* Decide whether we need to set mustPredict */
        mustPredict = false;

        if (rpAddedVarIntf)
        {
            mustPredict = true;
#ifdef DEBUG
            if (verbose)
                raDumpVarIntf();
#endif
        }

        if (rpPasses == 1)
        {
            if ((opts.compFlags & CLFLG_REGVAR) == 0)
                goto ALL_DONE;

            if (rpPredictAssignAgain)
                mustPredict = true;
#ifdef DEBUG
            if (fJitNoRegLoc)
                goto ALL_DONE;
#endif
        }

        /* Calculate the new value to use for regAvail */

        regAvail = allAcceptableRegs;

        /* If a frame pointer is required then we remove EBP */
        if (codeGen->isFramePointerRequired() || codeGen->isFrameRequired())
            regAvail &= ~RBM_FPBASE;

#if ETW_EBP_FRAMED
        // We never have EBP available when ETW_EBP_FRAME is defined
        regAvail &= ~RBM_FPBASE;
#endif

        // If we have done n-passes then we must continue to pessimize the
        // interference graph by or-ing the interferences from the previous pass

        if (rpPasses > rpPassesPessimize)
        {
            for (unsigned regInx = 0; regInx < REG_COUNT; regInx++)
                VarSetOps::UnionD(this, raLclRegIntf[regInx], oldLclRegIntf[regInx]);

            /* If we reverse an EBP enregistration then keep it that way */
            if (rpReverseEBPenreg)
                regAvail &= ~RBM_FPBASE;
        }

#ifdef DEBUG
        if (verbose)
            raDumpRegIntf();
#endif

        /*  Save the old variable/register interference graph */
        for (unsigned i = 0; i < REG_COUNT; i++)
        {
            VarSetOps::Assign(this, oldLclRegIntf[i], raLclRegIntf[i]);
        }
        oldStkPredict = rpStkPredict;
    } // end of while (true)

ALL_DONE:;

    // If we recorded a better feasible allocation than we ended up with, go back to using it.
    rpUseRecordedPredictionIfBetter();

#if DOUBLE_ALIGN
    codeGen->setDoubleAlign(false);
#endif

    switch (rpFrameType)
    {
        default:
            noway_assert(!"rpFrameType not set correctly!");
            break;
        case FT_ESP_FRAME:
            noway_assert(!codeGen->isFramePointerRequired());
            noway_assert(!codeGen->isFrameRequired());
            codeGen->setFramePointerUsed(false);
            break;
        case FT_EBP_FRAME:
            noway_assert((regUsed & RBM_FPBASE) == 0);
            codeGen->setFramePointerUsed(true);
            break;
#if DOUBLE_ALIGN
        case FT_DOUBLE_ALIGN_FRAME:
            noway_assert((regUsed & RBM_FPBASE) == 0);
            noway_assert(!codeGen->isFramePointerRequired());
            codeGen->setFramePointerUsed(false);
            codeGen->setDoubleAlign(true);
            break;
#endif
    }

    /* Record the set of registers that we need */
    codeGen->regSet.rsClearRegsModified();
    if (regUsed != RBM_NONE)
    {
        codeGen->regSet.rsSetRegsModified(regUsed);
    }

    /* We need genFullPtrRegMap if :
     * The method is fully interruptible, or
     * We are generating an EBP-less frame (for stack-pointer deltas)
     */

    genFullPtrRegMap = (genInterruptible || !codeGen->isFramePointerUsed());

    raMarkStkVars();
#ifdef DEBUG
    if (verbose)
    {
        printf("# rpPasses was %u for %s\n", rpPasses, info.compFullName);
        printf("  rpStkPredict was %u\n", rpStkPredict);
    }
#endif
    rpRegAllocDone = true;
}

#endif // LEGACY_BACKEND

/*****************************************************************************
 *
 *  Mark all variables as to whether they live on the stack frame
 *  (part or whole), and if so what the base is (FP or SP).
 */

void Compiler::raMarkStkVars()
{
    unsigned   lclNum;
    LclVarDsc* varDsc;

    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        // For RyuJIT, lvOnFrame is set by LSRA, except in the case of zero-ref, which is set below.
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef LEGACY_BACKEND
        varDsc->lvOnFrame = false;
#endif // LEGACY_BACKEND

        if (lvaIsFieldOfDependentlyPromotedStruct(varDsc))
        {
            noway_assert(!varDsc->lvRegister);
            goto ON_STK;
        }

        /* Fully enregistered variables don't need any frame space */

        if (varDsc->lvRegister)
        {
            if (!isRegPairType(varDsc->TypeGet()))
            {
                goto NOT_STK;
            }

            /* For "large" variables make sure both halves are enregistered */

            if (varDsc->lvRegNum != REG_STK && varDsc->lvOtherReg != REG_STK)
            {
                goto NOT_STK;
            }
        }
        /* Unused variables typically don't get any frame space */
        else if (varDsc->lvRefCnt == 0)
        {
            bool needSlot = false;

            bool stkFixedArgInVarArgs =
                info.compIsVarArgs && varDsc->lvIsParam && !varDsc->lvIsRegArg && lclNum != lvaVarargsHandleArg;

            // If its address has been exposed, ignore lvRefCnt. However, exclude
            // fixed arguments in varargs method as lvOnFrame shouldn't be set
            // for them as we don't want to explicitly report them to GC.

            if (!stkFixedArgInVarArgs)
            {
                needSlot |= varDsc->lvAddrExposed;
            }

#if FEATURE_FIXED_OUT_ARGS

            /* Is this the dummy variable representing GT_LCLBLK ? */
            needSlot |= (lclNum == lvaOutgoingArgSpaceVar);

#endif // FEATURE_FIXED_OUT_ARGS

#ifdef DEBUG
            /* For debugging, note that we have to reserve space even for
               unused variables if they are ever in scope. However, this is not
               an issue as fgExtendDbgLifetimes() adds an initialization and
               variables in scope will not have a zero ref-cnt.
             */
            if (opts.compDbgCode && !varDsc->lvIsParam && varDsc->lvTracked)
            {
                for (unsigned scopeNum = 0; scopeNum < info.compVarScopesCount; scopeNum++)
                {
                    noway_assert(info.compVarScopes[scopeNum].vsdVarNum != lclNum);
                }
            }
#endif
            /*
              For Debug Code, we have to reserve space even if the variable is never
              in scope. We will also need to initialize it if it is a GC var.
              So we set lvMustInit and artifically bump up the ref-cnt.
             */

            if (opts.compDbgCode && !stkFixedArgInVarArgs && lclNum < info.compLocalsCount)
            {
                needSlot |= true;

                if (lvaTypeIsGC(lclNum))
                {
                    varDsc->lvRefCnt = 1;
                }

                if (!varDsc->lvIsParam)
                {
                    varDsc->lvMustInit = true;
                }
            }

#ifndef LEGACY_BACKEND
            varDsc->lvOnFrame = needSlot;
#endif // !LEGACY_BACKEND
            if (!needSlot)
            {
                /* Clear the lvMustInit flag in case it is set */
                varDsc->lvMustInit = false;

                goto NOT_STK;
            }
        }

#ifndef LEGACY_BACKEND
        if (!varDsc->lvOnFrame)
        {
            goto NOT_STK;
        }
#endif // !LEGACY_BACKEND

    ON_STK:
        /* The variable (or part of it) lives on the stack frame */

        noway_assert((varDsc->lvType != TYP_UNDEF) && (varDsc->lvType != TYP_VOID) && (varDsc->lvType != TYP_UNKNOWN));
#if FEATURE_FIXED_OUT_ARGS
        noway_assert((lclNum == lvaOutgoingArgSpaceVar) || lvaLclSize(lclNum) != 0);
#else  // FEATURE_FIXED_OUT_ARGS
        noway_assert(lvaLclSize(lclNum) != 0);
#endif // FEATURE_FIXED_OUT_ARGS

        varDsc->lvOnFrame = true; // Our prediction is that the final home for this local variable will be in the
                                  // stack frame

    NOT_STK:;
        varDsc->lvFramePointerBased = codeGen->isFramePointerUsed();

#if DOUBLE_ALIGN

        if (codeGen->doDoubleAlign())
        {
            noway_assert(codeGen->isFramePointerUsed() == false);

            /* All arguments are off of EBP with double-aligned frames */

            if (varDsc->lvIsParam && !varDsc->lvIsRegArg)
            {
                varDsc->lvFramePointerBased = true;
            }
        }

#endif

        /* Some basic checks */

        // It must be in a register, on frame, or have zero references.

        noway_assert(varDsc->lvIsInReg() || varDsc->lvOnFrame || varDsc->lvRefCnt == 0);

#ifndef LEGACY_BACKEND
        // We can't have both lvRegister and lvOnFrame for RyuJIT
        noway_assert(!varDsc->lvRegister || !varDsc->lvOnFrame);
#else  // LEGACY_BACKEND

        /* If both lvRegister and lvOnFrame are set, it must be partially enregistered */
        noway_assert(!varDsc->lvRegister || !varDsc->lvOnFrame ||
                     (varDsc->lvType == TYP_LONG && varDsc->lvOtherReg == REG_STK));
#endif // LEGACY_BACKEND

#ifdef DEBUG

        // For varargs functions, there should be no direct references to
        // parameter variables except for 'this' (because these were morphed
        // in the importer) and the 'arglist' parameter (which is not a GC
        // pointer). and the return buffer argument (if we are returning a
        // struct).
        // This is important because we don't want to try to report them
        // to the GC, as the frame offsets in these local varables would
        // not be correct.

        if (varDsc->lvIsParam && raIsVarargsStackArg(lclNum))
        {
            if (!varDsc->lvPromoted && !varDsc->lvIsStructField)
            {
                noway_assert(varDsc->lvRefCnt == 0 && !varDsc->lvRegister && !varDsc->lvOnFrame);
            }
        }
#endif
    }
}

#ifdef LEGACY_BACKEND
void Compiler::rpRecordPrediction()
{
    if (rpBestRecordedPrediction == NULL || rpStkPredict < rpBestRecordedStkPredict)
    {
        if (rpBestRecordedPrediction == NULL)
        {
            rpBestRecordedPrediction =
                reinterpret_cast<VarRegPrediction*>(compGetMemArrayA(lvaCount, sizeof(VarRegPrediction)));
        }
        for (unsigned k = 0; k < lvaCount; k++)
        {
            rpBestRecordedPrediction[k].m_isEnregistered = lvaTable[k].lvRegister;
            rpBestRecordedPrediction[k].m_regNum         = (regNumberSmall)lvaTable[k].GetRegNum();
            rpBestRecordedPrediction[k].m_otherReg       = (regNumberSmall)lvaTable[k].GetOtherReg();
        }
        rpBestRecordedStkPredict = rpStkPredict;
        JITDUMP("Recorded a feasible reg prediction with weighted stack use count %d.\n", rpBestRecordedStkPredict);
    }
}

void Compiler::rpUseRecordedPredictionIfBetter()
{
    JITDUMP("rpStkPredict is %d; previous feasible reg prediction is %d.\n", rpStkPredict,
            rpBestRecordedPrediction != NULL ? rpBestRecordedStkPredict : 0);
    if (rpBestRecordedPrediction != NULL && rpStkPredict > rpBestRecordedStkPredict)
    {
        JITDUMP("Reverting to a previously-recorded feasible reg prediction with weighted stack use count %d.\n",
                rpBestRecordedStkPredict);

        for (unsigned k = 0; k < lvaCount; k++)
        {
            lvaTable[k].lvRegister = rpBestRecordedPrediction[k].m_isEnregistered;
            lvaTable[k].SetRegNum(static_cast<regNumber>(rpBestRecordedPrediction[k].m_regNum));
            lvaTable[k].SetOtherReg(static_cast<regNumber>(rpBestRecordedPrediction[k].m_otherReg));
        }
    }
}
#endif // LEGACY_BACKEND
