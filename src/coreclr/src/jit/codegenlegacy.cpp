// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           CodeGenerator                                   XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif
#include "codegen.h"

#ifdef LEGACY_BACKEND // This file is NOT used for the '!LEGACY_BACKEND' that uses the linear scan register allocator

#ifdef _TARGET_AMD64_
#error AMD64 must be !LEGACY_BACKEND
#endif

#ifdef _TARGET_ARM64_
#error ARM64 must be !LEGACY_BACKEND
#endif

#include "gcinfo.h"
#include "emit.h"

#ifndef JIT32_GCENCODER
#include "gcinfoencoder.h"
#endif

/*****************************************************************************
 *
 *  Determine what variables die between beforeSet and afterSet, and
 *  update the liveness globals accordingly:
 *  compiler->compCurLife, gcInfo.gcVarPtrSetCur, regSet.rsMaskVars, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur
 */

void CodeGen::genDyingVars(VARSET_VALARG_TP beforeSet, VARSET_VALARG_TP afterSet)
{
    unsigned   varNum;
    LclVarDsc* varDsc;
    regMaskTP  regBit;
    VARSET_TP  deadSet(VarSetOps::Diff(compiler, beforeSet, afterSet));

    if (VarSetOps::IsEmpty(compiler, deadSet))
        return;

    /* iterate through the dead variables */

    VARSET_ITER_INIT(compiler, iter, deadSet, varIndex);
    while (iter.NextElem(&varIndex))
    {
        varNum = compiler->lvaTrackedToVarNum[varIndex];
        varDsc = compiler->lvaTable + varNum;

        /* Remove this variable from the 'deadSet' bit set */

        noway_assert(VarSetOps::IsMember(compiler, compiler->compCurLife, varIndex));

        VarSetOps::RemoveElemD(compiler, compiler->compCurLife, varIndex);

        noway_assert(!VarSetOps::IsMember(compiler, gcInfo.gcTrkStkPtrLcls, varIndex) ||
                     VarSetOps::IsMember(compiler, gcInfo.gcVarPtrSetCur, varIndex));

        VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varIndex);

        /* We are done if the variable is not enregistered */

        if (!varDsc->lvRegister)
        {
#ifdef DEBUG
            if (compiler->verbose)
            {
                printf("\t\t\t\t\t\t\tV%02u,T%02u is a dyingVar\n", varNum, varDsc->lvVarIndex);
            }
#endif
            continue;
        }

#if !FEATURE_FP_REGALLOC
        // We don't do FP-enreg of vars whose liveness changes in GTF_COLON_COND
        if (!varDsc->IsFloatRegType())
#endif
        {
            /* Get hold of the appropriate register bit(s) */

            if (varTypeIsFloating(varDsc->TypeGet()))
            {
                regBit = genRegMaskFloat(varDsc->lvRegNum, varDsc->TypeGet());
            }
            else
            {
                regBit = genRegMask(varDsc->lvRegNum);
                if (isRegPairType(varDsc->lvType) && varDsc->lvOtherReg != REG_STK)
                    regBit |= genRegMask(varDsc->lvOtherReg);
            }

#ifdef DEBUG
            if (compiler->verbose)
            {
                printf("\t\t\t\t\t\t\tV%02u,T%02u in reg %s is a dyingVar\n", varNum, varDsc->lvVarIndex,
                       compiler->compRegVarName(varDsc->lvRegNum));
            }
#endif
            noway_assert((regSet.rsMaskVars & regBit) != 0);

            regSet.RemoveMaskVars(regBit);

            // Remove GC tracking if any for this register

            if ((regBit & regSet.rsMaskUsed) == 0) // The register may be multi-used
                gcInfo.gcMarkRegSetNpt(regBit);
        }
    }
}

/*****************************************************************************
 *
 *  Change the given enregistered local variable node to a register variable node
 */

void CodeGenInterface::genBashLclVar(GenTreePtr tree, unsigned varNum, LclVarDsc* varDsc)
{
    noway_assert(tree->gtOper == GT_LCL_VAR);
    noway_assert(varDsc->lvRegister);

    if (isRegPairType(varDsc->lvType))
    {
        /* Check for the case of a variable that was narrowed to an int */

        if (isRegPairType(tree->gtType))
        {
            genMarkTreeInRegPair(tree, gen2regs2pair(varDsc->lvRegNum, varDsc->lvOtherReg));
            return;
        }

        noway_assert(tree->gtFlags & GTF_VAR_CAST);
        noway_assert(tree->gtType == TYP_INT);
    }
    else
    {
        noway_assert(!isRegPairType(tree->gtType));
    }

    /* It's a register variable -- modify the node */

    unsigned livenessFlags = (tree->gtFlags & GTF_LIVENESS_MASK);

    ValueNumPair vnp = tree->gtVNPair; // Save the ValueNumPair
    tree->SetOper(GT_REG_VAR);
    tree->gtVNPair = vnp; // Preserve the ValueNumPair, as SetOper will clear it.

    tree->gtFlags |= livenessFlags;
    tree->SetInReg();
    tree->gtRegNum          = varDsc->lvRegNum;
    tree->gtRegVar.gtRegNum = varDsc->lvRegNum;
    tree->gtRegVar.SetLclNum(varNum);
}

// inline
void CodeGen::saveLiveness(genLivenessSet* ls)
{
    VarSetOps::Assign(compiler, ls->liveSet, compiler->compCurLife);
    VarSetOps::Assign(compiler, ls->varPtrSet, gcInfo.gcVarPtrSetCur);
    ls->maskVars  = (regMaskSmall)regSet.rsMaskVars;
    ls->gcRefRegs = (regMaskSmall)gcInfo.gcRegGCrefSetCur;
    ls->byRefRegs = (regMaskSmall)gcInfo.gcRegByrefSetCur;
}

// inline
void CodeGen::restoreLiveness(genLivenessSet* ls)
{
    VarSetOps::Assign(compiler, compiler->compCurLife, ls->liveSet);
    VarSetOps::Assign(compiler, gcInfo.gcVarPtrSetCur, ls->varPtrSet);
    regSet.rsMaskVars       = ls->maskVars;
    gcInfo.gcRegGCrefSetCur = ls->gcRefRegs;
    gcInfo.gcRegByrefSetCur = ls->byRefRegs;
}

// inline
void CodeGen::checkLiveness(genLivenessSet* ls)
{
    assert(VarSetOps::Equal(compiler, compiler->compCurLife, ls->liveSet));
    assert(VarSetOps::Equal(compiler, gcInfo.gcVarPtrSetCur, ls->varPtrSet));
    assert(regSet.rsMaskVars == ls->maskVars);
    assert(gcInfo.gcRegGCrefSetCur == ls->gcRefRegs);
    assert(gcInfo.gcRegByrefSetCur == ls->byRefRegs);
}

// inline
bool CodeGenInterface::genMarkLclVar(GenTreePtr tree)
{
    unsigned   varNum;
    LclVarDsc* varDsc;

    assert(tree->gtOper == GT_LCL_VAR);

    /* Does the variable live in a register? */

    varNum = tree->gtLclVarCommon.gtLclNum;
    assert(varNum < compiler->lvaCount);
    varDsc = compiler->lvaTable + varNum;

    if (varDsc->lvRegister)
    {
        genBashLclVar(tree, varNum, varDsc);
        return true;
    }
    else
    {
        return false;
    }
}

// inline
GenTreePtr CodeGen::genGetAddrModeBase(GenTreePtr tree)
{
    bool       rev;
    unsigned   mul;
    unsigned   cns;
    GenTreePtr adr;
    GenTreePtr idx;

    if (genCreateAddrMode(tree,     // address
                          0,        // mode
                          false,    // fold
                          RBM_NONE, // reg mask
                          &rev,     // reverse ops
                          &adr,     // base addr
                          &idx,     // index val
#if SCALED_ADDR_MODES
                          &mul, // scaling
#endif
                          &cns,  // displacement
                          true)) // don't generate code
        return adr;
    else
        return NULL;
}

#if FEATURE_STACK_FP_X87
// inline
void CodeGenInterface::genResetFPstkLevel(unsigned newValue /* = 0 */)
{
    genFPstkLevel = newValue;
}

// inline
unsigned CodeGenInterface::genGetFPstkLevel()
{
    return genFPstkLevel;
}

// inline
void CodeGenInterface::genIncrementFPstkLevel(unsigned inc /* = 1 */)
{
    noway_assert((inc == 0) || genFPstkLevel + inc > genFPstkLevel);
    genFPstkLevel += inc;
}

// inline
void CodeGenInterface::genDecrementFPstkLevel(unsigned dec /* = 1 */)
{
    noway_assert((dec == 0) || genFPstkLevel - dec < genFPstkLevel);
    genFPstkLevel -= dec;
}

#endif // FEATURE_STACK_FP_X87

/*****************************************************************************
 *
 *  Generate code that will set the given register to the integer constant.
 */

void CodeGen::genSetRegToIcon(regNumber reg, ssize_t val, var_types type, insFlags flags)
{
    noway_assert(type != TYP_REF || val == NULL);

    /* Does the reg already hold this constant? */

    if (!regTracker.rsIconIsInReg(val, reg))
    {
        if (val == 0)
        {
            instGen_Set_Reg_To_Zero(emitActualTypeSize(type), reg, flags);
        }
#ifdef _TARGET_ARM_
        // If we can set a register to a constant with a small encoding, then do that.
        else if (arm_Valid_Imm_For_Small_Mov(reg, val, flags))
        {
            instGen_Set_Reg_To_Imm(emitActualTypeSize(type), reg, val, flags);
        }
#endif
        else
        {
            /* See if a register holds the value or a close value? */
            bool      constantLoaded = false;
            ssize_t   delta;
            regNumber srcReg = regTracker.rsIconIsInReg(val, &delta);

            if (srcReg != REG_NA)
            {
                if (delta == 0)
                {
                    inst_RV_RV(INS_mov, reg, srcReg, type, emitActualTypeSize(type), flags);
                    constantLoaded = true;
                }
                else
                {
#if defined(_TARGET_XARCH_)
                    /* delta should fit inside a byte */
                    if (delta == (signed char)delta)
                    {
                        /* use an lea instruction to set reg */
                        getEmitter()->emitIns_R_AR(INS_lea, emitTypeSize(type), reg, srcReg, (int)delta);
                        constantLoaded = true;
                    }
#elif defined(_TARGET_ARM_)
                    /* We found a register 'regS' that has the value we need, modulo a small delta.
                       That is, the value we need is 'regS + delta'.
                       We one to generate one of the following instructions, listed in order of preference:

                            adds  regD, delta        ; 2 bytes. if regD == regS, regD is a low register, and
                       0<=delta<=255
                            subs  regD, delta        ; 2 bytes. if regD == regS, regD is a low register, and
                       -255<=delta<=0
                            adds  regD, regS, delta  ; 2 bytes. if regD and regS are low registers and 0<=delta<=7
                            subs  regD, regS, delta  ; 2 bytes. if regD and regS are low registers and -7<=delta<=0
                            mov   regD, icon         ; 4 bytes. icon is a wacky Thumb 12-bit immediate.
                            movw  regD, icon         ; 4 bytes. 0<=icon<=65535
                            add.w regD, regS, delta  ; 4 bytes. delta is a wacky Thumb 12-bit immediate.
                            sub.w regD, regS, delta  ; 4 bytes. delta is a wacky Thumb 12-bit immediate.
                            addw  regD, regS, delta  ; 4 bytes. 0<=delta<=4095
                            subw  regD, regS, delta  ; 4 bytes. -4095<=delta<=0

                       If it wasn't for the desire to generate the "mov reg,icon" forms if possible (and no bigger
                       than necessary), this would be a lot simpler. Note that we might set the overflow flag: we
                       can have regS containing the largest signed int 0x7fffffff and need the smallest signed int
                       0x80000000. In this case, delta will be 1.
                    */

                    bool      useAdd     = false;
                    regMaskTP regMask    = genRegMask(reg);
                    regMaskTP srcRegMask = genRegMask(srcReg);

                    if ((flags != INS_FLAGS_NOT_SET) && (reg == srcReg) && (regMask & RBM_LOW_REGS) &&
                        (unsigned_abs(delta) <= 255))
                    {
                        useAdd = true;
                    }
                    else if ((flags != INS_FLAGS_NOT_SET) && (regMask & RBM_LOW_REGS) && (srcRegMask & RBM_LOW_REGS) &&
                             (unsigned_abs(delta) <= 7))
                    {
                        useAdd = true;
                    }
                    else if (arm_Valid_Imm_For_Mov(val))
                    {
                        // fall through to general "!constantLoaded" case below
                    }
                    else if (arm_Valid_Imm_For_Add(delta, flags))
                    {
                        useAdd = true;
                    }

                    if (useAdd)
                    {
                        getEmitter()->emitIns_R_R_I(INS_add, EA_4BYTE, reg, srcReg, delta, flags);
                        constantLoaded = true;
                    }
#else
                    assert(!"Codegen missing");
#endif
                }
            }

            if (!constantLoaded) // Have we loaded it yet?
            {
#ifdef _TARGET_X86_
                if (val == -1)
                {
                    /* or reg,-1 takes 3 bytes */
                    inst_RV_IV(INS_OR, reg, val, emitActualTypeSize(type));
                }
                else
                    /* For SMALL_CODE it is smaller to push a small immediate and
                       then pop it into the dest register */
                    if ((compiler->compCodeOpt() == Compiler::SMALL_CODE) && val == (signed char)val)
                {
                    /* "mov" has no s(sign)-bit and so always takes 6 bytes,
                       whereas push+pop takes 2+1 bytes */

                    inst_IV(INS_push, val);
                    genSinglePush();

                    inst_RV(INS_pop, reg, type);
                    genSinglePop();
                }
                else
#endif // _TARGET_X86_
                {
                    instGen_Set_Reg_To_Imm(emitActualTypeSize(type), reg, val, flags);
                }
            }
        }
    }
    regTracker.rsTrackRegIntCns(reg, val);
    gcInfo.gcMarkRegPtrVal(reg, type);
}

/*****************************************************************************
 *
 *  Find an existing register set to the given integer constant, or
 *  pick a register and generate code that will set it to the integer constant.
 *
 *  If no existing register is set to the constant, it will use regSet.rsPickReg(regBest)
 *  to pick some register to set.  NOTE that this means the returned regNumber
 *  might *not* be in regBest.  It also implies that you should lock any registers
 *  you don't want spilled (not just mark as used).
 *
 */

regNumber CodeGen::genGetRegSetToIcon(ssize_t val, regMaskTP regBest /* = 0 */, var_types type /* = TYP_INT */)
{
    regNumber regCns;
#if REDUNDANT_LOAD

    // Is there already a register with zero that we can use?
    regCns = regTracker.rsIconIsInReg(val);

    if (regCns == REG_NA)
#endif
    {
        // If not, grab a register to hold the constant, preferring
        // any register besides RBM_TMP_0 so it can hopefully be re-used
        regCns = regSet.rsPickReg(regBest, regBest & ~RBM_TMP_0);

        // Now set the constant
        genSetRegToIcon(regCns, val, type);
    }

    // NOTE: there is guarantee that regCns is in regBest's mask
    return regCns;
}

/*****************************************************************************/
/*****************************************************************************
 *
 *  Add the given constant to the specified register.
 *  'tree' is the resulting tree
 */

void CodeGen::genIncRegBy(regNumber reg, ssize_t ival, GenTreePtr tree, var_types dstType, bool ovfl)
{
    bool setFlags = (tree != NULL) && tree->gtSetFlags();

#ifdef _TARGET_XARCH_
    /* First check to see if we can generate inc or dec instruction(s) */
    /* But avoid inc/dec on P4 in general for fast code or inside loops for blended code */
    if (!ovfl && !compiler->optAvoidIncDec(compiler->compCurBB->getBBWeight(compiler)))
    {
        emitAttr size = emitTypeSize(dstType);

        switch (ival)
        {
            case 2:
                inst_RV(INS_inc, reg, dstType, size);
                __fallthrough;
            case 1:
                inst_RV(INS_inc, reg, dstType, size);

                goto UPDATE_LIVENESS;

            case -2:
                inst_RV(INS_dec, reg, dstType, size);
                __fallthrough;
            case -1:
                inst_RV(INS_dec, reg, dstType, size);

                goto UPDATE_LIVENESS;
        }
    }
#endif
    {
        insFlags flags = setFlags ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
        inst_RV_IV(INS_add, reg, ival, emitActualTypeSize(dstType), flags);
    }

#ifdef _TARGET_XARCH_
UPDATE_LIVENESS:
#endif

    if (setFlags)
        genFlagsEqualToReg(tree, reg);

    regTracker.rsTrackRegTrash(reg);

    gcInfo.gcMarkRegSetNpt(genRegMask(reg));

    if (tree != NULL)
    {
        if (!tree->OperIsAssignment())
        {
            genMarkTreeInReg(tree, reg);
            if (varTypeIsGC(tree->TypeGet()))
                gcInfo.gcMarkRegSetByref(genRegMask(reg));
        }
    }
}

/*****************************************************************************
 *
 *  Subtract the given constant from the specified register.
 *  Should only be used for unsigned sub with overflow. Else
 *  genIncRegBy() can be used using -ival. We shouldn't use genIncRegBy()
 *  for these cases as the flags are set differently, and the following
 *  check for overflow won't work correctly.
 *  'tree' is the resulting tree.
 */

void CodeGen::genDecRegBy(regNumber reg, ssize_t ival, GenTreePtr tree)
{
    noway_assert((tree->gtFlags & GTF_OVERFLOW) &&
                 ((tree->gtFlags & GTF_UNSIGNED) || ival == ((tree->gtType == TYP_INT) ? INT32_MIN : SSIZE_T_MIN)));
    noway_assert(tree->gtType == TYP_INT || tree->gtType == TYP_I_IMPL);

    regTracker.rsTrackRegTrash(reg);

    noway_assert(!varTypeIsGC(tree->TypeGet()));
    gcInfo.gcMarkRegSetNpt(genRegMask(reg));

    insFlags flags = tree->gtSetFlags() ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
    inst_RV_IV(INS_sub, reg, ival, emitActualTypeSize(tree->TypeGet()), flags);

    if (tree->gtSetFlags())
        genFlagsEqualToReg(tree, reg);

    if (tree)
    {
        genMarkTreeInReg(tree, reg);
    }
}

/*****************************************************************************
 *
 *  Multiply the specified register by the given value.
 *  'tree' is the resulting tree
 */

void CodeGen::genMulRegBy(regNumber reg, ssize_t ival, GenTreePtr tree, var_types dstType, bool ovfl)
{
    noway_assert(genActualType(dstType) == TYP_INT || genActualType(dstType) == TYP_I_IMPL);

    regTracker.rsTrackRegTrash(reg);

    if (tree)
    {
        genMarkTreeInReg(tree, reg);
    }

    bool     use_shift = false;
    unsigned shift_by  = 0;

    if ((dstType >= TYP_INT) && !ovfl && (ival > 0) && ((ival & (ival - 1)) == 0))
    {
        use_shift = true;
        BitScanForwardPtr((ULONG*)&shift_by, (ULONG)ival);
    }

    if (use_shift)
    {
        if (shift_by != 0)
        {
            insFlags flags = tree->gtSetFlags() ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
            inst_RV_SH(INS_SHIFT_LEFT_LOGICAL, emitTypeSize(dstType), reg, shift_by, flags);
            if (tree->gtSetFlags())
                genFlagsEqualToReg(tree, reg);
        }
    }
    else
    {
        instruction ins;
#ifdef _TARGET_XARCH_
        ins = getEmitter()->inst3opImulForReg(reg);
#else
        ins = INS_mul;
#endif

        inst_RV_IV(ins, reg, ival, emitActualTypeSize(dstType));
    }
}

/*****************************************************************************/
/*****************************************************************************/
/*****************************************************************************
 *
 *  Compute the value 'tree' into a register that's in 'needReg'
 *  (or any free register if 'needReg' is RBM_NONE).
 *
 *  Note that 'needReg' is just a recommendation unless mustReg==RegSet::EXACT_REG.
 *  If keepReg==RegSet::KEEP_REG, we mark the register as being used.
 *
 *  If you require that the register returned is trashable, pass true for 'freeOnly'.
 */

void CodeGen::genComputeReg(
    GenTreePtr tree, regMaskTP needReg, RegSet::ExactReg mustReg, RegSet::KeepReg keepReg, bool freeOnly)
{
    noway_assert(tree->gtType != TYP_VOID);

    regNumber reg;
    regNumber rg2;

#if FEATURE_STACK_FP_X87
    noway_assert(genActualType(tree->gtType) == TYP_INT || genActualType(tree->gtType) == TYP_I_IMPL ||
                 genActualType(tree->gtType) == TYP_REF || tree->gtType == TYP_BYREF);
#elif defined(_TARGET_ARM_)
    noway_assert(genActualType(tree->gtType) == TYP_INT || genActualType(tree->gtType) == TYP_I_IMPL ||
                 genActualType(tree->gtType) == TYP_REF || tree->gtType == TYP_BYREF ||
                 genActualType(tree->gtType) == TYP_FLOAT || genActualType(tree->gtType) == TYP_DOUBLE ||
                 genActualType(tree->gtType) == TYP_STRUCT);
#else
    noway_assert(genActualType(tree->gtType) == TYP_INT || genActualType(tree->gtType) == TYP_I_IMPL ||
                 genActualType(tree->gtType) == TYP_REF || tree->gtType == TYP_BYREF ||
                 genActualType(tree->gtType) == TYP_FLOAT || genActualType(tree->gtType) == TYP_DOUBLE);
#endif

    /* Generate the value, hopefully into the right register */

    genCodeForTree(tree, needReg);
    noway_assert(tree->InReg());

    // There is a workaround in genCodeForTreeLng() that changes the type of the
    // tree of a GT_MUL with 64 bit result to TYP_INT from TYP_LONG, then calls
    // genComputeReg(). genCodeForTree(), above, will put the result in gtRegPair for ARM,
    // or leave it in EAX/EDX for x86, but only set EAX as gtRegNum. There's no point
    // running the rest of this code, because anything looking at gtRegNum on ARM or
    // attempting to move from EAX/EDX will be wrong.
    if ((tree->OperGet() == GT_MUL) && (tree->gtFlags & GTF_MUL_64RSLT))
        goto REG_OK;

    reg = tree->gtRegNum;

    /* Did the value end up in an acceptable register? */

    if ((mustReg == RegSet::EXACT_REG) && needReg && !(genRegMask(reg) & needReg))
    {
        /* Not good enough to satisfy the caller's orders */

        if (varTypeIsFloating(tree))
        {
            RegSet::RegisterPreference pref(needReg, RBM_NONE);
            rg2 = regSet.PickRegFloat(tree->TypeGet(), &pref);
        }
        else
        {
            rg2 = regSet.rsGrabReg(needReg);
        }
    }
    else
    {
        /* Do we have to end up with a free register? */

        if (!freeOnly)
            goto REG_OK;

        /* Did we luck out and the value got computed into an unused reg? */

        if (genRegMask(reg) & regSet.rsRegMaskFree())
            goto REG_OK;

        /* Register already in use, so spill previous value */

        if ((mustReg == RegSet::EXACT_REG) && needReg && (genRegMask(reg) & needReg))
        {
            rg2 = regSet.rsGrabReg(needReg);
            if (rg2 == reg)
            {
                gcInfo.gcMarkRegPtrVal(reg, tree->TypeGet());
                tree->gtRegNum = reg;
                goto REG_OK;
            }
        }
        else
        {
            /* OK, let's find a trashable home for the value */

            regMaskTP rv1RegUsed;

            regSet.rsLockReg(genRegMask(reg), &rv1RegUsed);
            rg2 = regSet.rsPickReg(needReg);
            regSet.rsUnlockReg(genRegMask(reg), rv1RegUsed);
        }
    }

    noway_assert(reg != rg2);

    /* Update the value in the target register */

    regTracker.rsTrackRegCopy(rg2, reg);

    inst_RV_RV(ins_Copy(tree->TypeGet()), rg2, reg, tree->TypeGet());

    /* The value has been transferred to 'reg' */

    if ((genRegMask(reg) & regSet.rsMaskUsed) == 0)
        gcInfo.gcMarkRegSetNpt(genRegMask(reg));

    gcInfo.gcMarkRegPtrVal(rg2, tree->TypeGet());

    /* The value is now in an appropriate register */

    tree->gtRegNum = rg2;

REG_OK:

    /* Does the caller want us to mark the register as used? */

    if (keepReg == RegSet::KEEP_REG)
    {
        /* In case we're computing a value into a register variable */

        genUpdateLife(tree);

        /* Mark the register as 'used' */

        regSet.rsMarkRegUsed(tree);
    }
}

/*****************************************************************************
 *
 *  Same as genComputeReg(), the only difference being that the result is
 *  guaranteed to end up in a trashable register.
 */

// inline
void CodeGen::genCompIntoFreeReg(GenTreePtr tree, regMaskTP needReg, RegSet::KeepReg keepReg)
{
    genComputeReg(tree, needReg, RegSet::ANY_REG, keepReg, true);
}

/*****************************************************************************
 *
 *  The value 'tree' was earlier computed into a register; free up that
 *  register (but also make sure the value is presently in a register).
 */

void CodeGen::genReleaseReg(GenTreePtr tree)
{
    if (tree->gtFlags & GTF_SPILLED)
    {
        /* The register has been spilled -- reload it */

        regSet.rsUnspillReg(tree, 0, RegSet::FREE_REG);
        return;
    }

    regSet.rsMarkRegFree(genRegMask(tree->gtRegNum));
}

/*****************************************************************************
 *
 *  The value 'tree' was earlier computed into a register. Check whether that
 *  register has been spilled (and reload it if so), and if 'keepReg' is RegSet::FREE_REG,
 *  free the register. The caller shouldn't need to be setting GCness of the register
 *  where tree will be recovered to, so we disallow keepReg==RegSet::FREE_REG for GC type trees.
 */

void CodeGen::genRecoverReg(GenTreePtr tree, regMaskTP needReg, RegSet::KeepReg keepReg)
{
    if (tree->gtFlags & GTF_SPILLED)
    {
        /* The register has been spilled -- reload it */

        regSet.rsUnspillReg(tree, needReg, keepReg);
        return;
    }
    else if (needReg && (needReg & genRegMask(tree->gtRegNum)) == 0)
    {
        /* We need the tree in another register. So move it there */

        noway_assert(tree->InReg());
        regNumber oldReg = tree->gtRegNum;

        /* Pick an acceptable register */

        regNumber reg = regSet.rsGrabReg(needReg);

        /* Copy the value */

        inst_RV_RV(INS_mov, reg, oldReg, tree->TypeGet());
        tree->gtRegNum = reg;

        gcInfo.gcMarkRegPtrVal(tree);
        regSet.rsMarkRegUsed(tree);
        regSet.rsMarkRegFree(oldReg, tree);

        regTracker.rsTrackRegCopy(reg, oldReg);
    }

    /* Free the register if the caller desired so */

    if (keepReg == RegSet::FREE_REG)
    {
        regSet.rsMarkRegFree(genRegMask(tree->gtRegNum));
        // Can't use RegSet::FREE_REG on a GC type
        noway_assert(!varTypeIsGC(tree->gtType));
    }
    else
    {
        noway_assert(regSet.rsMaskUsed & genRegMask(tree->gtRegNum));
    }
}

/*****************************************************************************
 *
 * Move one half of a register pair to its new regPair(half).
 */

// inline
void CodeGen::genMoveRegPairHalf(GenTreePtr tree, regNumber dst, regNumber src, int off)
{
    if (src == REG_STK)
    {
        // handle long to unsigned long overflow casts
        while (tree->gtOper == GT_CAST)
        {
            noway_assert(tree->gtType == TYP_LONG);
            tree = tree->gtCast.CastOp();
        }
        noway_assert(tree->gtEffectiveVal()->gtOper == GT_LCL_VAR);
        noway_assert(tree->gtType == TYP_LONG);
        inst_RV_TT(ins_Load(TYP_INT), dst, tree, off);
        regTracker.rsTrackRegTrash(dst);
    }
    else
    {
        regTracker.rsTrackRegCopy(dst, src);
        inst_RV_RV(INS_mov, dst, src, TYP_INT);
    }
}

/*****************************************************************************
 *
 *  The given long value is in a register pair, but it's not an acceptable
 *  one. We have to move the value into a register pair in 'needReg' (if
 *  non-zero) or the pair 'newPair' (when 'newPair != REG_PAIR_NONE').
 *
 *  Important note: if 'needReg' is non-zero, we assume the current pair
 *  has not been marked as free. If, OTOH, 'newPair' is specified, we
 *  assume that the current register pair is marked as used and free it.
 */

void CodeGen::genMoveRegPair(GenTreePtr tree, regMaskTP needReg, regPairNo newPair)
{
    regPairNo oldPair;

    regNumber oldLo;
    regNumber oldHi;
    regNumber newLo;
    regNumber newHi;

    /* Either a target set or a specific pair may be requested */

    noway_assert((needReg != 0) != (newPair != REG_PAIR_NONE));

    /* Get hold of the current pair */

    oldPair = tree->gtRegPair;
    noway_assert(oldPair != newPair);

    /* Are we supposed to move to a specific pair? */

    if (newPair != REG_PAIR_NONE)
    {
        regMaskTP oldMask = genRegPairMask(oldPair);
        regMaskTP loMask  = genRegMask(genRegPairLo(newPair));
        regMaskTP hiMask  = genRegMask(genRegPairHi(newPair));
        regMaskTP overlap = oldMask & (loMask | hiMask);

        /* First lock any registers that are in both pairs */

        noway_assert((regSet.rsMaskUsed & overlap) == overlap);
        noway_assert((regSet.rsMaskLock & overlap) == 0);
        regSet.rsMaskLock |= overlap;

        /* Make sure any additional registers we need are free */

        if ((loMask & regSet.rsMaskUsed) != 0 && (loMask & oldMask) == 0)
        {
            regSet.rsGrabReg(loMask);
        }

        if ((hiMask & regSet.rsMaskUsed) != 0 && (hiMask & oldMask) == 0)
        {
            regSet.rsGrabReg(hiMask);
        }

        /* Unlock those registers we have temporarily locked */

        noway_assert((regSet.rsMaskUsed & overlap) == overlap);
        noway_assert((regSet.rsMaskLock & overlap) == overlap);
        regSet.rsMaskLock -= overlap;

        /* We can now free the old pair */

        regSet.rsMarkRegFree(oldMask);
    }
    else
    {
        /* Pick the new pair based on the caller's stated preference */

        newPair = regSet.rsGrabRegPair(needReg);
    }

    // If grabbed pair is the same as old one we're done
    if (newPair == oldPair)
    {
        noway_assert((oldLo = genRegPairLo(oldPair), oldHi = genRegPairHi(oldPair), newLo = genRegPairLo(newPair),
                      newHi = genRegPairHi(newPair), newLo != REG_STK && newHi != REG_STK));
        return;
    }

    /* Move the values from the old pair into the new one */

    oldLo = genRegPairLo(oldPair);
    oldHi = genRegPairHi(oldPair);
    newLo = genRegPairLo(newPair);
    newHi = genRegPairHi(newPair);

    noway_assert(newLo != REG_STK && newHi != REG_STK);

    /* Careful - the register pairs might overlap */

    if (newLo == oldLo)
    {
        /* The low registers are identical, just move the upper half */

        noway_assert(newHi != oldHi);
        genMoveRegPairHalf(tree, newHi, oldHi, sizeof(int));
    }
    else
    {
        /* The low registers are different, are the upper ones the same? */

        if (newHi == oldHi)
        {
            /* Just move the lower half, then */
            genMoveRegPairHalf(tree, newLo, oldLo, 0);
        }
        else
        {
            /* Both sets are different - is there an overlap? */

            if (newLo == oldHi)
            {
                /* Are high and low simply swapped ? */

                if (newHi == oldLo)
                {
#ifdef _TARGET_ARM_
                    /* Let's use XOR swap to reduce register pressure. */
                    inst_RV_RV(INS_eor, oldLo, oldHi);
                    inst_RV_RV(INS_eor, oldHi, oldLo);
                    inst_RV_RV(INS_eor, oldLo, oldHi);
#else
                    inst_RV_RV(INS_xchg, oldHi, oldLo);
#endif
                    regTracker.rsTrackRegSwap(oldHi, oldLo);
                }
                else
                {
                    /* New lower == old higher, so move higher half first */

                    noway_assert(newHi != oldLo);
                    genMoveRegPairHalf(tree, newHi, oldHi, sizeof(int));
                    genMoveRegPairHalf(tree, newLo, oldLo, 0);
                }
            }
            else
            {
                /* Move lower half first */
                genMoveRegPairHalf(tree, newLo, oldLo, 0);
                genMoveRegPairHalf(tree, newHi, oldHi, sizeof(int));
            }
        }
    }

    /* Record the fact that we're switching to another pair */

    tree->gtRegPair = newPair;
}

/*****************************************************************************
 *
 *  Compute the value 'tree' into the register pair specified by 'needRegPair'
 *  if 'needRegPair' is REG_PAIR_NONE then use any free register pair, avoid
 *  those in avoidReg.
 *  If 'keepReg' is set to RegSet::KEEP_REG then we mark both registers that the
 *  value ends up in as being used.
 */

void CodeGen::genComputeRegPair(
    GenTreePtr tree, regPairNo needRegPair, regMaskTP avoidReg, RegSet::KeepReg keepReg, bool freeOnly)
{
    regMaskTP regMask;
    regPairNo regPair;
    regMaskTP tmpMask;
    regMaskTP tmpUsedMask;
    regNumber rLo;
    regNumber rHi;

    noway_assert(isRegPairType(tree->gtType));

    if (needRegPair == REG_PAIR_NONE)
    {
        if (freeOnly)
        {
            regMask = regSet.rsRegMaskFree() & ~avoidReg;
            if (genMaxOneBit(regMask))
                regMask = regSet.rsRegMaskFree();
        }
        else
        {
            regMask = RBM_ALLINT & ~avoidReg;
        }

        if (genMaxOneBit(regMask))
            regMask = regSet.rsRegMaskCanGrab();
    }
    else
    {
        regMask = genRegPairMask(needRegPair);
    }

    /* Generate the value, hopefully into the right register pair */

    genCodeForTreeLng(tree, regMask, avoidReg);

    noway_assert(tree->InReg());

    regPair = tree->gtRegPair;
    tmpMask = genRegPairMask(regPair);

    rLo = genRegPairLo(regPair);
    rHi = genRegPairHi(regPair);

    /* At least one half is in a real register */

    noway_assert(rLo != REG_STK || rHi != REG_STK);

    /* Did the value end up in an acceptable register pair? */

    if (needRegPair != REG_PAIR_NONE)
    {
        if (needRegPair != regPair)
        {
            /* This is a workaround. If we specify a regPair for genMoveRegPair */
            /* it expects the source pair being marked as used */
            regSet.rsMarkRegPairUsed(tree);
            genMoveRegPair(tree, 0, needRegPair);
        }
    }
    else if (freeOnly)
    {
        /* Do we have to end up with a free register pair?
           Something might have gotten freed up above */
        bool mustMoveReg = false;

        regMask = regSet.rsRegMaskFree() & ~avoidReg;

        if (genMaxOneBit(regMask))
            regMask = regSet.rsRegMaskFree();

        if ((tmpMask & regMask) != tmpMask || rLo == REG_STK || rHi == REG_STK)
        {
            /* Note that we must call genMoveRegPair if one of our registers
               comes from the used mask, so that it will be properly spilled. */

            mustMoveReg = true;
        }

        if (genMaxOneBit(regMask))
            regMask |= regSet.rsRegMaskCanGrab() & ~avoidReg;

        if (genMaxOneBit(regMask))
            regMask |= regSet.rsRegMaskCanGrab();

        /* Did the value end up in a free register pair? */

        if (mustMoveReg)
        {
            /* We'll have to move the value to a free (trashable) pair */
            genMoveRegPair(tree, regMask, REG_PAIR_NONE);
        }
    }
    else
    {
        noway_assert(needRegPair == REG_PAIR_NONE);
        noway_assert(!freeOnly);

        /* it is possible to have tmpMask also in the regSet.rsMaskUsed */
        tmpUsedMask = tmpMask & regSet.rsMaskUsed;
        tmpMask &= ~regSet.rsMaskUsed;

        /* Make sure that the value is in "real" registers*/
        if (rLo == REG_STK)
        {
            /* Get one of the desired registers, but exclude rHi */

            regSet.rsLockReg(tmpMask);
            regSet.rsLockUsedReg(tmpUsedMask);

            regNumber reg = regSet.rsPickReg(regMask);

            regSet.rsUnlockUsedReg(tmpUsedMask);
            regSet.rsUnlockReg(tmpMask);

            inst_RV_TT(ins_Load(TYP_INT), reg, tree, 0);

            tree->gtRegPair = gen2regs2pair(reg, rHi);

            regTracker.rsTrackRegTrash(reg);
            gcInfo.gcMarkRegSetNpt(genRegMask(reg));
        }
        else if (rHi == REG_STK)
        {
            /* Get one of the desired registers, but exclude rLo */

            regSet.rsLockReg(tmpMask);
            regSet.rsLockUsedReg(tmpUsedMask);

            regNumber reg = regSet.rsPickReg(regMask);

            regSet.rsUnlockUsedReg(tmpUsedMask);
            regSet.rsUnlockReg(tmpMask);

            inst_RV_TT(ins_Load(TYP_INT), reg, tree, 4);

            tree->gtRegPair = gen2regs2pair(rLo, reg);

            regTracker.rsTrackRegTrash(reg);
            gcInfo.gcMarkRegSetNpt(genRegMask(reg));
        }
    }

    /* Does the caller want us to mark the register as used? */

    if (keepReg == RegSet::KEEP_REG)
    {
        /* In case we're computing a value into a register variable */

        genUpdateLife(tree);

        /* Mark the register as 'used' */

        regSet.rsMarkRegPairUsed(tree);
    }
}

/*****************************************************************************
 *
 *  Same as genComputeRegPair(), the only difference being that the result
 *  is guaranteed to end up in a trashable register pair.
 */

// inline
void CodeGen::genCompIntoFreeRegPair(GenTreePtr tree, regMaskTP avoidReg, RegSet::KeepReg keepReg)
{
    genComputeRegPair(tree, REG_PAIR_NONE, avoidReg, keepReg, true);
}

/*****************************************************************************
 *
 *  The value 'tree' was earlier computed into a register pair; free up that
 *  register pair (but also make sure the value is presently in a register
 *  pair).
 */

void CodeGen::genReleaseRegPair(GenTreePtr tree)
{
    if (tree->gtFlags & GTF_SPILLED)
    {
        /* The register has been spilled -- reload it */

        regSet.rsUnspillRegPair(tree, 0, RegSet::FREE_REG);
        return;
    }

    regSet.rsMarkRegFree(genRegPairMask(tree->gtRegPair));
}

/*****************************************************************************
 *
 *  The value 'tree' was earlier computed into a register pair. Check whether
 *  either register of that pair has been spilled (and reload it if so), and
 *  if 'keepReg' is 0, free the register pair.
 */

void CodeGen::genRecoverRegPair(GenTreePtr tree, regPairNo regPair, RegSet::KeepReg keepReg)
{
    if (tree->gtFlags & GTF_SPILLED)
    {
        regMaskTP regMask;

        if (regPair == REG_PAIR_NONE)
            regMask = RBM_NONE;
        else
            regMask = genRegPairMask(regPair);

        /* The register pair has been spilled -- reload it */

        regSet.rsUnspillRegPair(tree, regMask, RegSet::KEEP_REG);
    }

    /* Does the caller insist on the value being in a specific place? */

    if (regPair != REG_PAIR_NONE && regPair != tree->gtRegPair)
    {
        /* No good -- we'll have to move the value to a new place */

        genMoveRegPair(tree, 0, regPair);

        /* Mark the pair as used if appropriate */

        if (keepReg == RegSet::KEEP_REG)
            regSet.rsMarkRegPairUsed(tree);

        return;
    }

    /* Free the register pair if the caller desired so */

    if (keepReg == RegSet::FREE_REG)
        regSet.rsMarkRegFree(genRegPairMask(tree->gtRegPair));
}

/*****************************************************************************
 *
 *  Compute the given long value into the specified register pair; don't mark
 *  the register pair as used.
 */

// inline
void CodeGen::genEvalIntoFreeRegPair(GenTreePtr tree, regPairNo regPair, regMaskTP avoidReg)
{
    genComputeRegPair(tree, regPair, avoidReg, RegSet::KEEP_REG);
    genRecoverRegPair(tree, regPair, RegSet::FREE_REG);
}

/*****************************************************************************
 *  This helper makes sure that the regpair target of an assignment is
 *  available for use.  This needs to be called in genCodeForTreeLng just before
 *  a long assignment, but must not be called until everything has been
 *  evaluated, or else we might try to spill enregistered variables.
 *
 */

// inline
void CodeGen::genMakeRegPairAvailable(regPairNo regPair)
{
    /* Make sure the target of the store is available */

    regNumber regLo = genRegPairLo(regPair);
    regNumber regHi = genRegPairHi(regPair);

    if ((regHi != REG_STK) && (regSet.rsMaskUsed & genRegMask(regHi)))
        regSet.rsSpillReg(regHi);

    if ((regLo != REG_STK) && (regSet.rsMaskUsed & genRegMask(regLo)))
        regSet.rsSpillReg(regLo);
}

/*****************************************************************************/
/*****************************************************************************
 *
 *  Return true if the given tree 'addr' can be computed via an addressing mode,
 *  such as "[ebx+esi*4+20]". If the expression isn't an address mode already
 *  try to make it so (but we don't try 'too hard' to accomplish this).
 *
 *  If we end up needing a register (or two registers) to hold some part(s) of the
 *  address, we return the use register mask via '*useMaskPtr'.
 *
 *  If keepReg==RegSet::KEEP_REG, the registers (viz. *useMaskPtr) will be marked as
 *  in use. The caller would then be responsible for calling
 *  regSet.rsMarkRegFree(*useMaskPtr).
 *
 *  If keepReg==RegSet::FREE_REG, then the caller needs update the GC-tracking by
 *  calling genDoneAddressable(addr, *useMaskPtr, RegSet::FREE_REG);
 */

bool CodeGen::genMakeIndAddrMode(GenTreePtr      addr,
                                 GenTreePtr      oper,
                                 bool            forLea,
                                 regMaskTP       regMask,
                                 RegSet::KeepReg keepReg,
                                 regMaskTP*      useMaskPtr,
                                 bool            deferOK)
{
    if (addr->gtOper == GT_ARR_ELEM)
    {
        regMaskTP regs = genMakeAddrArrElem(addr, oper, RBM_ALLINT, keepReg);
        *useMaskPtr    = regs;
        return true;
    }

    bool       rev;
    GenTreePtr rv1;
    GenTreePtr rv2;
    bool       operIsArrIndex; // is oper an array index
    GenTreePtr scaledIndex;    // If scaled addressing mode can't be used

    regMaskTP anyMask = RBM_ALLINT;

    unsigned cns;
    unsigned mul;

    GenTreePtr tmp;
    int        ixv = INT_MAX; // unset value

    GenTreePtr scaledIndexVal;

    regMaskTP newLiveMask;
    regMaskTP rv1Mask;
    regMaskTP rv2Mask;

    /* Deferred address mode forming NYI for x86 */

    noway_assert(deferOK == false);

    noway_assert(oper == NULL ||
                 ((oper->OperIsIndir() || oper->OperIsAtomicOp()) &&
                  ((oper->gtOper == GT_CMPXCHG && oper->gtCmpXchg.gtOpLocation == addr) || oper->gtOp.gtOp1 == addr)));
    operIsArrIndex = (oper != nullptr && oper->OperGet() == GT_IND && (oper->gtFlags & GTF_IND_ARR_INDEX) != 0);

    if (addr->gtOper == GT_LEA)
    {
        rev                  = (addr->gtFlags & GTF_REVERSE_OPS) != 0;
        GenTreeAddrMode* lea = addr->AsAddrMode();
        rv1                  = lea->Base();
        rv2                  = lea->Index();
        mul                  = lea->gtScale;
        cns                  = lea->gtOffset;

        if (rv1 != NULL && rv2 == NULL && cns == 0 && rv1->InReg())
        {
            scaledIndex = NULL;
            goto YES;
        }
    }
    else
    {
        // NOTE: FOR NOW THIS ISN'T APPROPRIATELY INDENTED - THIS IS TO MAKE IT
        // EASIER TO MERGE

        /* Is the complete address already sitting in a register? */

        if ((addr->InReg()) || (addr->gtOper == GT_LCL_VAR && genMarkLclVar(addr)))
        {
            genUpdateLife(addr);

            rv1 = addr;
            rv2 = scaledIndex = 0;
            cns               = 0;

            goto YES;
        }

        /* Is it an absolute address */

        if (addr->IsCnsIntOrI())
        {
            rv1 = rv2 = scaledIndex = 0;
            // along this code path cns is never used, so place a BOGUS value in it as proof
            // cns = addr->gtIntCon.gtIconVal;
            cns = UINT_MAX;

            goto YES;
        }

        /* Is there a chance of forming an address mode? */

        if (!genCreateAddrMode(addr, forLea ? 1 : 0, false, regMask, &rev, &rv1, &rv2, &mul, &cns))
        {
            /* This better not be an array index */
            noway_assert(!operIsArrIndex);

            return false;
        }
        // THIS IS THE END OF THE INAPPROPRIATELY INDENTED SECTION
    }

    /*  For scaled array access, RV2 may not be pointing to the index of the
        array if the CPU does not support the needed scaling factor.  We will
        make it point to the actual index, and scaledIndex will point to
        the scaled value */

    scaledIndex    = NULL;
    scaledIndexVal = NULL;

    if (operIsArrIndex && rv2 != NULL && (rv2->gtOper == GT_MUL || rv2->gtOper == GT_LSH) &&
        rv2->gtOp.gtOp2->IsIntCnsFitsInI32())
    {
        scaledIndex = rv2;
        compiler->optGetArrayRefScaleAndIndex(scaledIndex, &scaledIndexVal DEBUGARG(true));

        noway_assert(scaledIndex->gtOp.gtOp2->IsIntCnsFitsInI32());
    }

    /* Has the address already been computed? */

    if (addr->InReg())
    {
        if (forLea)
            return true;

        rv1         = addr;
        rv2         = NULL;
        scaledIndex = NULL;
        genUpdateLife(addr);
        goto YES;
    }

    /*
        Here we have the following operands:

            rv1     .....       base address
            rv2     .....       offset value        (or NULL)
            mul     .....       multiplier for rv2  (or 0)
            cns     .....       additional constant (or 0)

        The first operand must be present (and be an address) unless we're
        computing an expression via 'LEA'. The scaled operand is optional,
        but must not be a pointer if present.
     */

    noway_assert(rv2 == NULL || !varTypeIsGC(rv2->TypeGet()));

    /*-------------------------------------------------------------------------
     *
     * Make sure both rv1 and rv2 (if present) are in registers
     *
     */

    // Trivial case : Is either rv1 or rv2 a NULL ?

    if (!rv2)
    {
        /* A single operand, make sure it's in a register */

        if (cns != 0)
        {
            // In the case where "rv1" is already in a register, there's no reason to get into a
            // register in "regMask" yet, if there's a non-zero constant that we're going to add;
            // if there is, we can do an LEA.
            genCodeForTree(rv1, RBM_NONE);
        }
        else
        {
            genCodeForTree(rv1, regMask);
        }
        goto DONE_REGS;
    }
    else if (!rv1)
    {
        /* A single (scaled) operand, make sure it's in a register */

        genCodeForTree(rv2, 0);
        goto DONE_REGS;
    }

    /* At this point, both rv1 and rv2 are non-NULL and we have to make sure
       they are in registers */

    noway_assert(rv1 && rv2);

    /*  If we have to check a constant array index, compare it against
        the array dimension (see below) but then fold the index with a
        scaling factor (if any) and additional offset (if any).
     */

    if (rv2->gtOper == GT_CNS_INT || (scaledIndex != NULL && scaledIndexVal->gtOper == GT_CNS_INT))
    {
        if (scaledIndex != NULL)
        {
            assert(rv2 == scaledIndex && scaledIndexVal != NULL);
            rv2 = scaledIndexVal;
        }
        /* We must have a range-checked index operation */

        noway_assert(operIsArrIndex);

        /* Get hold of the index value and see if it's a constant */

        if (rv2->IsIntCnsFitsInI32())
        {
            ixv = (int)rv2->gtIntCon.gtIconVal;
            // Maybe I should just set "fold" true in the call to genMakeAddressable above.
            if (scaledIndex != NULL)
            {
                int scale = 1 << ((int)scaledIndex->gtOp.gtOp2->gtIntCon.gtIconVal); // If this truncates, that's OK --
                                                                                     // multiple of 2^6.
                if (mul == 0)
                {
                    mul = scale;
                }
                else
                {
                    mul *= scale;
                }
            }
            rv2 = scaledIndex = NULL;

            /* Add the scaled index into the added value */

            if (mul)
                cns += ixv * mul;
            else
                cns += ixv;

            /* Make sure 'rv1' is in a register */

            genCodeForTree(rv1, regMask);

            goto DONE_REGS;
        }
    }

    if (rv1->InReg())
    {
        /* op1 already in register - how about op2? */

        if (rv2->InReg())
        {
            /* Great - both operands are in registers already. Just update
               the liveness and we are done. */

            if (rev)
            {
                genUpdateLife(rv2);
                genUpdateLife(rv1);
            }
            else
            {
                genUpdateLife(rv1);
                genUpdateLife(rv2);
            }

            goto DONE_REGS;
        }

        /* rv1 is in a register, but rv2 isn't */

        if (!rev)
        {
            /* rv1 is already materialized in a register. Just update liveness
               to rv1 and generate code for rv2 */

            genUpdateLife(rv1);
            regSet.rsMarkRegUsed(rv1, oper);
        }

        goto GEN_RV2;
    }
    else if (rv2->InReg())
    {
        /* rv2 is in a register, but rv1 isn't */

        noway_assert(rv2->gtOper == GT_REG_VAR);

        if (rev)
        {
            /* rv2 is already materialized in a register. Update liveness
               to after rv2 and then hang on to rv2 */

            genUpdateLife(rv2);
            regSet.rsMarkRegUsed(rv2, oper);
        }

        /* Generate the for the first operand */

        genCodeForTree(rv1, regMask);

        if (rev)
        {
            // Free up rv2 in the right fashion (it might be re-marked if keepReg)
            regSet.rsMarkRegUsed(rv1, oper);
            regSet.rsLockUsedReg(genRegMask(rv1->gtRegNum));
            genReleaseReg(rv2);
            regSet.rsUnlockUsedReg(genRegMask(rv1->gtRegNum));
            genReleaseReg(rv1);
        }
        else
        {
            /* We have evaluated rv1, and now we just need to update liveness
               to rv2 which was already in a register */

            genUpdateLife(rv2);
        }

        goto DONE_REGS;
    }

    if (forLea && !cns)
        return false;

    /* Make sure we preserve the correct operand order */

    if (rev)
    {
        /* Generate the second operand first */

        // Determine what registers go live between rv2 and rv1
        newLiveMask = genNewLiveRegMask(rv2, rv1);

        rv2Mask = regMask & ~newLiveMask;
        rv2Mask &= ~rv1->gtRsvdRegs;

        if (rv2Mask == RBM_NONE)
        {
            // The regMask hint cannot be honored
            // We probably have a call that trashes the register(s) in regMask
            // so ignore the regMask hint, but try to avoid using
            // the registers in newLiveMask and the rv1->gtRsvdRegs
            //
            rv2Mask = RBM_ALLINT & ~newLiveMask;
            rv2Mask = regSet.rsMustExclude(rv2Mask, rv1->gtRsvdRegs);
        }

        genCodeForTree(rv2, rv2Mask);
        regMask &= ~genRegMask(rv2->gtRegNum);

        regSet.rsMarkRegUsed(rv2, oper);

        /* Generate the first operand second */

        genCodeForTree(rv1, regMask);
        regSet.rsMarkRegUsed(rv1, oper);

        /* Free up both operands in the right order (they might be
           re-marked as used below)
        */
        regSet.rsLockUsedReg(genRegMask(rv1->gtRegNum));
        genReleaseReg(rv2);
        regSet.rsUnlockUsedReg(genRegMask(rv1->gtRegNum));
        genReleaseReg(rv1);
    }
    else
    {
        /* Get the first operand into a register */

        // Determine what registers go live between rv1 and rv2
        newLiveMask = genNewLiveRegMask(rv1, rv2);

        rv1Mask = regMask & ~newLiveMask;
        rv1Mask &= ~rv2->gtRsvdRegs;

        if (rv1Mask == RBM_NONE)
        {
            // The regMask hint cannot be honored
            // We probably have a call that trashes the register(s) in regMask
            // so ignore the regMask hint, but try to avoid using
            // the registers in liveMask and the rv2->gtRsvdRegs
            //
            rv1Mask = RBM_ALLINT & ~newLiveMask;
            rv1Mask = regSet.rsMustExclude(rv1Mask, rv2->gtRsvdRegs);
        }

        genCodeForTree(rv1, rv1Mask);
        regSet.rsMarkRegUsed(rv1, oper);

    GEN_RV2:

        /* Here, we need to get rv2 in a register. We have either already
           materialized rv1 into a register, or it was already in a one */

        noway_assert(rv1->InReg());
        noway_assert(rev || regSet.rsIsTreeInReg(rv1->gtRegNum, rv1));

        /* Generate the second operand as well */

        regMask &= ~genRegMask(rv1->gtRegNum);
        genCodeForTree(rv2, regMask);

        if (rev)
        {
            /* rev==true means the evaluation order is rv2,rv1. We just
               evaluated rv2, and rv1 was already in a register. Just
               update liveness to rv1 and we are done. */

            genUpdateLife(rv1);
        }
        else
        {
            /* We have evaluated rv1 and rv2. Free up both operands in
               the right order (they might be re-marked as used below) */

            /* Even though we have not explicitly marked rv2 as used,
               rv2->gtRegNum may be used if rv2 is a multi-use or
               an enregistered variable. */
            regMaskTP rv2Used;
            regSet.rsLockReg(genRegMask(rv2->gtRegNum), &rv2Used);

            /* Check for special case both rv1 and rv2 are the same register */
            if (rv2Used != genRegMask(rv1->gtRegNum))
            {
                genReleaseReg(rv1);
                regSet.rsUnlockReg(genRegMask(rv2->gtRegNum), rv2Used);
            }
            else
            {
                regSet.rsUnlockReg(genRegMask(rv2->gtRegNum), rv2Used);
                genReleaseReg(rv1);
            }
        }
    }

/*-------------------------------------------------------------------------
 *
 * At this point, both rv1 and rv2 (if present) are in registers
 *
 */

DONE_REGS:

    /* We must verify that 'rv1' and 'rv2' are both sitting in registers */

    if (rv1 && !(rv1->InReg()))
        return false;
    if (rv2 && !(rv2->InReg()))
        return false;

YES:

    // *(intVar1+intVar1) causes problems as we
    // call regSet.rsMarkRegUsed(op1) and regSet.rsMarkRegUsed(op2). So the calling function
    // needs to know that it has to call rsFreeReg(reg1) twice. We can't do
    // that currently as we return a single mask in useMaskPtr.

    if ((keepReg == RegSet::KEEP_REG) && oper && rv1 && rv2 && rv1->InReg() && rv2->InReg())
    {
        if (rv1->gtRegNum == rv2->gtRegNum)
        {
            noway_assert(!operIsArrIndex);
            return false;
        }
    }

    /* Check either register operand to see if it needs to be saved */

    if (rv1)
    {
        noway_assert(rv1->InReg());

        if (keepReg == RegSet::KEEP_REG)
        {
            regSet.rsMarkRegUsed(rv1, oper);
        }
        else
        {
            /* If the register holds an address, mark it */

            gcInfo.gcMarkRegPtrVal(rv1->gtRegNum, rv1->TypeGet());
        }
    }

    if (rv2)
    {
        noway_assert(rv2->InReg());

        if (keepReg == RegSet::KEEP_REG)
            regSet.rsMarkRegUsed(rv2, oper);
    }

    if (deferOK)
    {
        noway_assert(!scaledIndex);
        return true;
    }

    /* Compute the set of registers the address depends on */

    regMaskTP useMask = RBM_NONE;

    if (rv1)
    {
        if (rv1->gtFlags & GTF_SPILLED)
            regSet.rsUnspillReg(rv1, 0, RegSet::KEEP_REG);

        noway_assert(rv1->InReg());
        useMask |= genRegMask(rv1->gtRegNum);
    }

    if (rv2)
    {
        if (rv2->gtFlags & GTF_SPILLED)
        {
            if (rv1)
            {
                regMaskTP lregMask = genRegMask(rv1->gtRegNum);
                regMaskTP used;

                regSet.rsLockReg(lregMask, &used);
                regSet.rsUnspillReg(rv2, 0, RegSet::KEEP_REG);
                regSet.rsUnlockReg(lregMask, used);
            }
            else
                regSet.rsUnspillReg(rv2, 0, RegSet::KEEP_REG);
        }
        noway_assert(rv2->InReg());
        useMask |= genRegMask(rv2->gtRegNum);
    }

    /* Tell the caller which registers we need to hang on to */

    *useMaskPtr = useMask;

    return true;
}

/*****************************************************************************
 *
 *  'oper' is an array bounds check (a GT_ARR_BOUNDS_CHECK node).
 */

void CodeGen::genRangeCheck(GenTreePtr oper)
{
    noway_assert(oper->OperGet() == GT_ARR_BOUNDS_CHECK);
    GenTreeBoundsChk* bndsChk = oper->AsBoundsChk();

    GenTreePtr arrLen    = bndsChk->gtArrLen;
    GenTreePtr arrRef    = NULL;
    int        lenOffset = 0;

    /* Is the array index a constant value? */
    GenTreePtr index = bndsChk->gtIndex;
    if (!index->IsCnsIntOrI())
    {
        // No, it's not a constant.
        genCodeForTree(index, RBM_ALLINT);
        regSet.rsMarkRegUsed(index);
    }

    // If "arrLen" is a ARR_LENGTH operation, get the array whose length that takes in a register.
    // Otherwise, if the length is not a constant, get it (the length, not the arr reference) in
    // a register.

    if (arrLen->OperGet() == GT_ARR_LENGTH)
    {
        GenTreeArrLen* arrLenExact = arrLen->AsArrLen();
        lenOffset                  = arrLenExact->ArrLenOffset();

#if !CPU_LOAD_STORE_ARCH && !defined(_TARGET_64BIT_)
        // We always load the length into a register on ARM and x64.

        // 64-bit has to act like LOAD_STORE_ARCH because the array only holds 32-bit
        // lengths, but the index expression *can* be native int (64-bits)
        arrRef = arrLenExact->ArrRef();
        genCodeForTree(arrRef, RBM_ALLINT);
        noway_assert(arrRef->InReg());
        regSet.rsMarkRegUsed(arrRef);
        noway_assert(regSet.rsMaskUsed & genRegMask(arrRef->gtRegNum));
#endif
    }
#if !CPU_LOAD_STORE_ARCH && !defined(_TARGET_64BIT_)
    // This is another form in which we have an array reference and a constant length.  Don't use
    // on LOAD_STORE or 64BIT.
    else if (arrLen->OperGet() == GT_IND && arrLen->gtOp.gtOp1->IsAddWithI32Const(&arrRef, &lenOffset))
    {
        genCodeForTree(arrRef, RBM_ALLINT);
        noway_assert(arrRef->InReg());
        regSet.rsMarkRegUsed(arrRef);
        noway_assert(regSet.rsMaskUsed & genRegMask(arrRef->gtRegNum));
    }
#endif

    // If we didn't find one of the special forms above, generate code to evaluate the array length to a register.
    if (arrRef == NULL)
    {
        // (Unless it's a constant.)
        if (!arrLen->IsCnsIntOrI())
        {
            genCodeForTree(arrLen, RBM_ALLINT);
            regSet.rsMarkRegUsed(arrLen);

            noway_assert(arrLen->InReg());
            noway_assert(regSet.rsMaskUsed & genRegMask(arrLen->gtRegNum));
        }
    }

    if (!index->IsCnsIntOrI())
    {
        // If we need "arrRef" or "arrLen", and evaluating "index" displaced whichever of them we're using
        // from its register, get it back in a register.
        regMaskTP indRegMask = RBM_ALLINT;
        regMaskTP arrRegMask = RBM_ALLINT;
        if (!(index->gtFlags & GTF_SPILLED))
            arrRegMask = ~genRegMask(index->gtRegNum);
        if (arrRef != NULL)
        {
            genRecoverReg(arrRef, arrRegMask, RegSet::KEEP_REG);
            indRegMask &= ~genRegMask(arrRef->gtRegNum);
        }
        else if (!arrLen->IsCnsIntOrI())
        {
            genRecoverReg(arrLen, arrRegMask, RegSet::KEEP_REG);
            indRegMask &= ~genRegMask(arrLen->gtRegNum);
        }
        if (index->gtFlags & GTF_SPILLED)
            regSet.rsUnspillReg(index, indRegMask, RegSet::KEEP_REG);

        /* Make sure we have the values we expect */
        noway_assert(index->InReg());
        noway_assert(regSet.rsMaskUsed & genRegMask(index->gtRegNum));

        noway_assert(index->TypeGet() == TYP_I_IMPL ||
                     (varTypeIsIntegral(index->TypeGet()) && !varTypeIsLong(index->TypeGet())));
        var_types indxType = index->TypeGet();
        if (indxType != TYP_I_IMPL)
            indxType = TYP_INT;

        if (arrRef != NULL)
        { // _TARGET_X86_ or X64 when we have a TYP_INT (32-bit) index expression in the index register

            /* Generate "cmp index, [arrRef+LenOffs]" */
            inst_RV_AT(INS_cmp, emitTypeSize(indxType), indxType, index->gtRegNum, arrRef, lenOffset);
        }
        else if (arrLen->IsCnsIntOrI())
        {
            ssize_t len = arrLen->AsIntConCommon()->IconValue();
            inst_RV_IV(INS_cmp, index->gtRegNum, len, EA_4BYTE);
        }
        else
        {
            inst_RV_RV(INS_cmp, index->gtRegNum, arrLen->gtRegNum, indxType, emitTypeSize(indxType));
        }

        /* Generate "jae <fail_label>" */

        noway_assert(oper->gtOper == GT_ARR_BOUNDS_CHECK);
        emitJumpKind jmpGEU = genJumpKindForOper(GT_GE, CK_UNSIGNED);
        genJumpToThrowHlpBlk(jmpGEU, SCK_RNGCHK_FAIL, bndsChk->gtIndRngFailBB);
    }
    else
    {
        /* Generate "cmp [rv1+LenOffs], cns" */

        bool indIsInt = true;
#ifdef _TARGET_64BIT_
        int     ixv     = 0;
        ssize_t ixvFull = index->AsIntConCommon()->IconValue();
        if (ixvFull > INT32_MAX)
        {
            indIsInt = false;
        }
        else
        {
            ixv = (int)ixvFull;
        }
#else
        ssize_t ixvFull = index->AsIntConCommon()->IconValue();
        int     ixv     = (int)ixvFull;
#endif
        if (arrRef != NULL && indIsInt)
        { // _TARGET_X86_ or X64 when we have a TYP_INT (32-bit) index expression in the index register
            /* Generate "cmp [arrRef+LenOffs], ixv" */
            inst_AT_IV(INS_cmp, EA_4BYTE, arrRef, ixv, lenOffset);
            // Generate "jbe <fail_label>"
            emitJumpKind jmpLEU = genJumpKindForOper(GT_LE, CK_UNSIGNED);
            genJumpToThrowHlpBlk(jmpLEU, SCK_RNGCHK_FAIL, bndsChk->gtIndRngFailBB);
        }
        else if (arrLen->IsCnsIntOrI())
        {
            ssize_t lenv = arrLen->AsIntConCommon()->IconValue();
            // Both are constants; decide at compile time.
            if (!(0 <= ixvFull && ixvFull < lenv))
            {
                genJumpToThrowHlpBlk(EJ_jmp, SCK_RNGCHK_FAIL, bndsChk->gtIndRngFailBB);
            }
        }
        else if (!indIsInt)
        {
            genJumpToThrowHlpBlk(EJ_jmp, SCK_RNGCHK_FAIL, bndsChk->gtIndRngFailBB);
        }
        else
        {
            /* Generate "cmp arrLen, ixv" */
            inst_RV_IV(INS_cmp, arrLen->gtRegNum, ixv, EA_4BYTE);
            // Generate "jbe <fail_label>"
            emitJumpKind jmpLEU = genJumpKindForOper(GT_LE, CK_UNSIGNED);
            genJumpToThrowHlpBlk(jmpLEU, SCK_RNGCHK_FAIL, bndsChk->gtIndRngFailBB);
        }
    }

    // Free the registers that were used.
    if (!index->IsCnsIntOrI())
    {
        regSet.rsMarkRegFree(index->gtRegNum, index);
    }

    if (arrRef != NULL)
    {
        regSet.rsMarkRegFree(arrRef->gtRegNum, arrRef);
    }
    else if (!arrLen->IsCnsIntOrI())
    {
        regSet.rsMarkRegFree(arrLen->gtRegNum, arrLen);
    }
}

/*****************************************************************************
 *
 * If compiling without REDUNDANT_LOAD, same as genMakeAddressable().
 * Otherwise, check if rvalue is in register. If so, mark it. Then
 * call genMakeAddressable(). Needed because genMakeAddressable is used
 * for both lvalue and rvalue, and we only can do this for rvalue.
 */

// inline
regMaskTP CodeGen::genMakeRvalueAddressable(
    GenTreePtr tree, regMaskTP needReg, RegSet::KeepReg keepReg, bool forLoadStore, bool smallOK)
{
    regNumber reg;

#if REDUNDANT_LOAD

    if (tree->gtOper == GT_LCL_VAR)
    {
        reg = findStkLclInReg(tree->gtLclVarCommon.gtLclNum);

        if (reg != REG_NA && (needReg == 0 || (genRegMask(reg) & needReg) != 0))
        {
            noway_assert(!isRegPairType(tree->gtType));

            genMarkTreeInReg(tree, reg);
        }
    }

#endif

    return genMakeAddressable2(tree, needReg, keepReg, forLoadStore, smallOK);
}

/*****************************************************************************/

bool CodeGen::genIsLocalLastUse(GenTreePtr tree)
{
    const LclVarDsc* varDsc = &compiler->lvaTable[tree->gtLclVarCommon.gtLclNum];

    noway_assert(tree->OperGet() == GT_LCL_VAR);
    noway_assert(varDsc->lvTracked);

    return ((tree->gtFlags & GTF_VAR_DEATH) != 0);
}

/*****************************************************************************
 *
 *  This is genMakeAddressable(GT_ARR_ELEM).
 *  Makes the array-element addressible and returns the addressibility registers.
 *  It also marks them as used if keepReg==RegSet::KEEP_REG.
 *  tree is the dependant tree.
 *
 *  Note that an array-element needs 2 registers to be addressibile, the
 *  array-object and the offset. This function marks gtArrObj and gtArrInds[0]
 *  with the 2 registers so that other functions (like instGetAddrMode()) know
 *  where to look for the offset to use.
 */

regMaskTP CodeGen::genMakeAddrArrElem(GenTreePtr arrElem, GenTreePtr tree, regMaskTP needReg, RegSet::KeepReg keepReg)
{
    noway_assert(arrElem->gtOper == GT_ARR_ELEM);
    noway_assert(!tree || tree->gtOper == GT_IND || tree == arrElem);

    /* Evaluate all the operands. We don't evaluate them into registers yet
       as GT_ARR_ELEM does not reorder the evaluation of the operands, and
       hence may use a sub-optimal ordering. We try to improve this
       situation somewhat by accessing the operands in stages
       (genMakeAddressable2 + genComputeAddressable and
       genCompIntoFreeReg + genRecoverReg).

       Note: we compute operands into free regs to avoid multiple uses of
       the same register. Multi-use would cause problems when we free
       registers in FIFO order instead of the assumed LIFO order that
       applies to all type of tree nodes except for GT_ARR_ELEM.
     */

    GenTreePtr arrObj   = arrElem->gtArrElem.gtArrObj;
    unsigned   rank     = arrElem->gtArrElem.gtArrRank;
    var_types  elemType = arrElem->gtArrElem.gtArrElemType;
    regMaskTP  addrReg  = RBM_NONE;
    regMaskTP  regNeed  = RBM_ALLINT;

#if FEATURE_WRITE_BARRIER && !NOGC_WRITE_BARRIERS
    // In CodeGen::WriteBarrier we set up ARG_1 followed by ARG_0
    // since the arrObj participates in the lea/add instruction
    // that computes ARG_0 we should avoid putting it in ARG_1
    //
    if (varTypeIsGC(elemType))
    {
        regNeed &= ~RBM_ARG_1;
    }
#endif

    // Strip off any comma expression.
    arrObj = genCodeForCommaTree(arrObj);

    // Having generated the code for the comma, we don't care about it anymore.
    arrElem->gtArrElem.gtArrObj = arrObj;

    // If the array ref is a stack var that's dying here we have to move it
    // into a register (regalloc already counts of this), as if it's a GC pointer
    // it can be collected from here on. This is not an issue for locals that are
    // in a register, as they get marked as used an will be tracked.
    // The bug that caused this is #100776. (untracked vars?)
    if (arrObj->OperGet() == GT_LCL_VAR && compiler->optIsTrackedLocal(arrObj) && genIsLocalLastUse(arrObj) &&
        !genMarkLclVar(arrObj))
    {
        genCodeForTree(arrObj, regNeed);
        regSet.rsMarkRegUsed(arrObj, 0);
        addrReg = genRegMask(arrObj->gtRegNum);
    }
    else
    {
        addrReg = genMakeAddressable2(arrObj, regNeed, RegSet::KEEP_REG,
                                      true,  // forLoadStore
                                      false, // smallOK
                                      false, // deferOK
                                      true); // evalSideEffs
    }

    unsigned dim;
    for (dim = 0; dim < rank; dim++)
        genCompIntoFreeReg(arrElem->gtArrElem.gtArrInds[dim], RBM_NONE, RegSet::KEEP_REG);

    /* Ensure that the array-object is in a register */

    addrReg = genKeepAddressable(arrObj, addrReg);
    genComputeAddressable(arrObj, addrReg, RegSet::KEEP_REG, regNeed, RegSet::KEEP_REG);

    regNumber arrReg     = arrObj->gtRegNum;
    regMaskTP arrRegMask = genRegMask(arrReg);
    regMaskTP indRegMask = RBM_ALLINT & ~arrRegMask;
    regSet.rsLockUsedReg(arrRegMask);

    /* Now process all the indices, do the range check, and compute
       the offset of the element */

    regNumber accReg = DUMMY_INIT(REG_CORRUPT); // accumulates the offset calculation

    for (dim = 0; dim < rank; dim++)
    {
        GenTreePtr index = arrElem->gtArrElem.gtArrInds[dim];

        /* Get the index into a free register (other than the register holding the array) */

        genRecoverReg(index, indRegMask, RegSet::KEEP_REG);

#if CPU_LOAD_STORE_ARCH
        /* Subtract the lower bound, and do the range check */

        regNumber valueReg = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(arrReg) & ~genRegMask(index->gtRegNum));
        getEmitter()->emitIns_R_AR(INS_ldr, EA_4BYTE, valueReg, arrReg,
                                   compiler->eeGetArrayDataOffset(elemType) + sizeof(int) * (dim + rank));
        regTracker.rsTrackRegTrash(valueReg);
        getEmitter()->emitIns_R_R(INS_sub, EA_4BYTE, index->gtRegNum, valueReg);
        regTracker.rsTrackRegTrash(index->gtRegNum);

        getEmitter()->emitIns_R_AR(INS_ldr, EA_4BYTE, valueReg, arrReg,
                                   compiler->eeGetArrayDataOffset(elemType) + sizeof(int) * dim);
        getEmitter()->emitIns_R_R(INS_cmp, EA_4BYTE, index->gtRegNum, valueReg);
#else
        /* Subtract the lower bound, and do the range check */
        getEmitter()->emitIns_R_AR(INS_sub, EA_4BYTE, index->gtRegNum, arrReg,
                                   compiler->eeGetArrayDataOffset(elemType) + sizeof(int) * (dim + rank));
        regTracker.rsTrackRegTrash(index->gtRegNum);

        getEmitter()->emitIns_R_AR(INS_cmp, EA_4BYTE, index->gtRegNum, arrReg,
                                   compiler->eeGetArrayDataOffset(elemType) + sizeof(int) * dim);
#endif
        emitJumpKind jmpGEU = genJumpKindForOper(GT_GE, CK_UNSIGNED);
        genJumpToThrowHlpBlk(jmpGEU, SCK_RNGCHK_FAIL);

        if (dim == 0)
        {
            /* Hang on to the register of the first index */

            noway_assert(accReg == DUMMY_INIT(REG_CORRUPT));
            accReg = index->gtRegNum;
            noway_assert(accReg != arrReg);
            regSet.rsLockUsedReg(genRegMask(accReg));
        }
        else
        {
            /* Evaluate accReg = accReg*dim_size + index */

            noway_assert(accReg != DUMMY_INIT(REG_CORRUPT));
#if CPU_LOAD_STORE_ARCH
            getEmitter()->emitIns_R_AR(INS_ldr, EA_4BYTE, valueReg, arrReg,
                                       compiler->eeGetArrayDataOffset(elemType) + sizeof(int) * dim);
            regTracker.rsTrackRegTrash(valueReg);
            getEmitter()->emitIns_R_R(INS_MUL, EA_4BYTE, accReg, valueReg);
#else
            getEmitter()->emitIns_R_AR(INS_MUL, EA_4BYTE, accReg, arrReg,
                                       compiler->eeGetArrayDataOffset(elemType) + sizeof(int) * dim);
#endif

            inst_RV_RV(INS_add, accReg, index->gtRegNum);
            regSet.rsMarkRegFree(index->gtRegNum, index);
            regTracker.rsTrackRegTrash(accReg);
        }
    }

    if (!jitIsScaleIndexMul(arrElem->gtArrElem.gtArrElemSize))
    {
        regNumber sizeReg = genGetRegSetToIcon(arrElem->gtArrElem.gtArrElemSize);

        getEmitter()->emitIns_R_R(INS_MUL, EA_4BYTE, accReg, sizeReg);
        regTracker.rsTrackRegTrash(accReg);
    }

    regSet.rsUnlockUsedReg(genRegMask(arrReg));
    regSet.rsUnlockUsedReg(genRegMask(accReg));

    regSet.rsMarkRegFree(genRegMask(arrReg));
    regSet.rsMarkRegFree(genRegMask(accReg));

    if (keepReg == RegSet::KEEP_REG)
    {
        /* We mark the addressability registers on arrObj and gtArrInds[0].
           instGetAddrMode() knows to work with this. */

        regSet.rsMarkRegUsed(arrObj, tree);
        regSet.rsMarkRegUsed(arrElem->gtArrElem.gtArrInds[0], tree);
    }

    return genRegMask(arrReg) | genRegMask(accReg);
}

/*****************************************************************************
 *
 *  Make sure the given tree is addressable.  'needReg' is a mask that indicates
 *  the set of registers we would prefer the destination tree to be computed
 *  into (RBM_NONE means no preference).
 *
 *  'tree' can subsequently be used with the inst_XX_TT() family of functions.
 *
 *  If 'keepReg' is RegSet::KEEP_REG, we mark any registers the addressability depends
 *  on as used, and return the mask for that register set (if no registers
 *  are marked as used, RBM_NONE is returned).
 *
 *  If 'smallOK' is not true and the datatype being address is a byte or short,
 *  then the tree is forced into a register.  This is useful when the machine
 *  instruction being emitted does not have a byte or short version.
 *
 *  The "deferOK" parameter indicates the mode of operation - when it's false,
 *  upon returning an actual address mode must have been formed (i.e. it must
 *  be possible to immediately call one of the inst_TT methods to operate on
 *  the value). When "deferOK" is true, we do whatever it takes to be ready
 *  to form the address mode later - for example, if an index address mode on
 *  a particular CPU requires the use of a specific register, we usually don't
 *  want to immediately grab that register for an address mode that will only
 *  be needed later. The convention is to call genMakeAddressable() with
 *  "deferOK" equal to true, do whatever work is needed to prepare the other
 *  operand, call genMakeAddressable() with "deferOK" equal to false, and
 *  finally call one of the inst_TT methods right after that.
 *
 *  If we do any other codegen after genMakeAddressable(tree) which can
 *  potentially spill the addressability registers, genKeepAddressable()
 *  needs to be called before accessing the tree again.
 *
 *  genDoneAddressable() needs to be called when we are done with the tree
 *  to free the addressability registers.
 */

regMaskTP CodeGen::genMakeAddressable(
    GenTreePtr tree, regMaskTP needReg, RegSet::KeepReg keepReg, bool smallOK, bool deferOK)
{
    GenTreePtr addr = NULL;
    regMaskTP  regMask;

    /* Is the value simply sitting in a register? */

    if (tree->InReg())
    {
        genUpdateLife(tree);

        goto GOT_VAL;
    }

    // TODO: If the value is for example a cast of float -> int, compute
    // TODO: the converted value into a stack temp, and leave it there,
    // TODO: since stack temps are always addressable. This would require
    // TODO: recording the fact that a particular tree is in a stack temp.

    /* byte/char/short operand -- is this acceptable to the caller? */

    if (varTypeIsSmall(tree->TypeGet()) && !smallOK)
        goto EVAL_TREE;

    // Evaluate non-last elements of comma expressions, to get to the last.
    tree = genCodeForCommaTree(tree);

    switch (tree->gtOper)
    {
        case GT_LCL_FLD:

            // We only use GT_LCL_FLD for lvDoNotEnregister vars, so we don't have
            // to worry about it being enregistered.
            noway_assert(compiler->lvaTable[tree->gtLclFld.gtLclNum].lvRegister == 0);

            genUpdateLife(tree);
            return 0;

        case GT_LCL_VAR:

            if (!genMarkLclVar(tree))
            {
                genUpdateLife(tree);
                return 0;
            }

            __fallthrough; // it turns out the variable lives in a register

        case GT_REG_VAR:

            genUpdateLife(tree);

            goto GOT_VAL;

        case GT_CLS_VAR:

            return 0;

        case GT_CNS_INT:
#ifdef _TARGET_64BIT_
            // Non-relocs will be sign extended, so we don't have to enregister
            // constants that are equivalent to a sign-extended int.
            // Relocs can be left alone if they are RIP-relative.
            if ((genTypeSize(tree->TypeGet()) > 4) &&
                (!tree->IsIntCnsFitsInI32() ||
                 (tree->IsIconHandle() &&
                  (IMAGE_REL_BASED_REL32 != compiler->eeGetRelocTypeHint((void*)tree->gtIntCon.gtIconVal)))))
            {
                break;
            }
#endif // _TARGET_64BIT_
            __fallthrough;

        case GT_CNS_LNG:
        case GT_CNS_DBL:
            // For MinOpts, we don't do constant folding, so we have
            // constants showing up in places we don't like.
            // force them into a register now to prevent that.
            if (compiler->opts.OptEnabled(CLFLG_CONSTANTFOLD))
                return 0;
            break;

        case GT_IND:
        case GT_NULLCHECK:

            /* Try to make the address directly addressable */

            if (genMakeIndAddrMode(tree->gtOp.gtOp1, tree, false, /* not for LEA */
                                   needReg, keepReg, &regMask, deferOK))
            {
                genUpdateLife(tree);
                return regMask;
            }

            /* No good, we'll have to load the address into a register */

            addr = tree;
            tree = tree->gtOp.gtOp1;
            break;

        default:
            break;
    }

EVAL_TREE:

    /* Here we need to compute the value 'tree' into a register */

    genCodeForTree(tree, needReg);

GOT_VAL:

    noway_assert(tree->InReg());

    if (isRegPairType(tree->gtType))
    {
        /* Are we supposed to hang on to the register? */

        if (keepReg == RegSet::KEEP_REG)
            regSet.rsMarkRegPairUsed(tree);

        regMask = genRegPairMask(tree->gtRegPair);
    }
    else
    {
        /* Are we supposed to hang on to the register? */

        if (keepReg == RegSet::KEEP_REG)
            regSet.rsMarkRegUsed(tree, addr);

        regMask = genRegMask(tree->gtRegNum);
    }

    return regMask;
}

/*****************************************************************************
 *  Compute a tree (which was previously made addressable using
 *  genMakeAddressable()) into a register.
 *  needReg - mask of preferred registers.
 *  keepReg - should the computed register be marked as used by the tree
 *  freeOnly - target register needs to be a scratch register
 */

void CodeGen::genComputeAddressable(GenTreePtr      tree,
                                    regMaskTP       addrReg,
                                    RegSet::KeepReg keptReg,
                                    regMaskTP       needReg,
                                    RegSet::KeepReg keepReg,
                                    bool            freeOnly)
{
    noway_assert(genStillAddressable(tree));
    noway_assert(varTypeIsIntegralOrI(tree->TypeGet()));

    genDoneAddressable(tree, addrReg, keptReg);

    regNumber reg;

    if (tree->InReg())
    {
        reg = tree->gtRegNum;

        if (freeOnly && !(genRegMask(reg) & regSet.rsRegMaskFree()))
            goto MOVE_REG;
    }
    else
    {
        if (tree->OperIsConst())
        {
            /* Need to handle consts separately as we don't want to emit
              "mov reg, 0" (emitter doesn't like that). Also, genSetRegToIcon()
              handles consts better for SMALL_CODE */

            noway_assert(tree->IsCnsIntOrI());
            reg = genGetRegSetToIcon(tree->gtIntCon.gtIconVal, needReg, tree->gtType);
        }
        else
        {
        MOVE_REG:
            reg = regSet.rsPickReg(needReg);

            inst_RV_TT(INS_mov, reg, tree);
            regTracker.rsTrackRegTrash(reg);
        }
    }

    genMarkTreeInReg(tree, reg);

    if (keepReg == RegSet::KEEP_REG)
        regSet.rsMarkRegUsed(tree);
    else
        gcInfo.gcMarkRegPtrVal(tree);
}

/*****************************************************************************
 *  Should be similar to genMakeAddressable() but gives more control.
 */

regMaskTP CodeGen::genMakeAddressable2(GenTreePtr      tree,
                                       regMaskTP       needReg,
                                       RegSet::KeepReg keepReg,
                                       bool            forLoadStore,
                                       bool            smallOK,
                                       bool            deferOK,
                                       bool            evalSideEffs)

{
    bool evalToReg = false;

    if (evalSideEffs && (tree->gtOper == GT_IND) && (tree->gtFlags & GTF_EXCEPT))
        evalToReg = true;

#if CPU_LOAD_STORE_ARCH
    if (!forLoadStore)
        evalToReg = true;
#endif

    if (evalToReg)
    {
        genCodeForTree(tree, needReg);

        noway_assert(tree->InReg());

        if (isRegPairType(tree->gtType))
        {
            /* Are we supposed to hang on to the register? */

            if (keepReg == RegSet::KEEP_REG)
                regSet.rsMarkRegPairUsed(tree);

            return genRegPairMask(tree->gtRegPair);
        }
        else
        {
            /* Are we supposed to hang on to the register? */

            if (keepReg == RegSet::KEEP_REG)
                regSet.rsMarkRegUsed(tree);

            return genRegMask(tree->gtRegNum);
        }
    }
    else
    {
        return genMakeAddressable(tree, needReg, keepReg, smallOK, deferOK);
    }
}

/*****************************************************************************
 *
 *  The given tree was previously passed to genMakeAddressable(); return
 *  'true' if the operand is still addressable.
 */

// inline
bool CodeGen::genStillAddressable(GenTreePtr tree)
{
    /* Has the value (or one or more of its sub-operands) been spilled? */

    if (tree->gtFlags & (GTF_SPILLED | GTF_SPILLED_OPER))
        return false;

    return true;
}

/*****************************************************************************
 *
 *  Recursive helper to restore complex address modes. The 'lockPhase'
 *  argument indicates whether we're in the 'lock' or 'reload' phase.
 */

regMaskTP CodeGen::genRestoreAddrMode(GenTreePtr addr, GenTreePtr tree, bool lockPhase)
{
    regMaskTP regMask = RBM_NONE;

    /* Have we found a spilled value? */

    if (tree->gtFlags & GTF_SPILLED)
    {
        /* Do nothing if we're locking, otherwise reload and lock */

        if (!lockPhase)
        {
            /* Unspill the register */

            regSet.rsUnspillReg(tree, 0, RegSet::FREE_REG);

            /* The value should now be sitting in a register */

            noway_assert(tree->InReg());
            regMask = genRegMask(tree->gtRegNum);

            /* Mark the register as used for the address */

            regSet.rsMarkRegUsed(tree, addr);

            /* Lock the register until we're done with the entire address */

            regSet.rsMaskLock |= regMask;
        }

        return regMask;
    }

    /* Is this sub-tree sitting in a register? */

    if (tree->InReg())
    {
        regMask = genRegMask(tree->gtRegNum);

        /* Lock the register if we're in the locking phase */

        if (lockPhase)
            regSet.rsMaskLock |= regMask;
    }
    else
    {
        /* Process any sub-operands of this node */

        unsigned kind = tree->OperKind();

        if (kind & GTK_SMPOP)
        {
            /* Unary/binary operator */

            if (tree->gtOp.gtOp1)
                regMask |= genRestoreAddrMode(addr, tree->gtOp.gtOp1, lockPhase);
            if (tree->gtGetOp2IfPresent())
                regMask |= genRestoreAddrMode(addr, tree->gtOp.gtOp2, lockPhase);
        }
        else if (tree->gtOper == GT_ARR_ELEM)
        {
            /* gtArrObj is the array-object and gtArrInds[0] is marked with the register
               which holds the offset-calculation */

            regMask |= genRestoreAddrMode(addr, tree->gtArrElem.gtArrObj, lockPhase);
            regMask |= genRestoreAddrMode(addr, tree->gtArrElem.gtArrInds[0], lockPhase);
        }
        else if (tree->gtOper == GT_CMPXCHG)
        {
            regMask |= genRestoreAddrMode(addr, tree->gtCmpXchg.gtOpLocation, lockPhase);
        }
        else
        {
            /* Must be a leaf/constant node */

            noway_assert(kind & (GTK_LEAF | GTK_CONST));
        }
    }

    return regMask;
}

/*****************************************************************************
 *
 *  The given tree was previously passed to genMakeAddressable, but since then
 *  some of its registers are known to have been spilled; do whatever it takes
 *  to make the operand addressable again (typically by reloading any spilled
 *  registers).
 */

regMaskTP CodeGen::genRestAddressable(GenTreePtr tree, regMaskTP addrReg, regMaskTP lockMask)
{
    noway_assert((regSet.rsMaskLock & lockMask) == lockMask);

    /* Is this a 'simple' register spill? */

    if (tree->gtFlags & GTF_SPILLED)
    {
        /* The mask must match the original register/regpair */

        if (isRegPairType(tree->gtType))
        {
            noway_assert(addrReg == genRegPairMask(tree->gtRegPair));

            regSet.rsUnspillRegPair(tree, /* restore it anywhere */ RBM_NONE, RegSet::KEEP_REG);

            addrReg = genRegPairMask(tree->gtRegPair);
        }
        else
        {
            noway_assert(addrReg == genRegMask(tree->gtRegNum));

            regSet.rsUnspillReg(tree, /* restore it anywhere */ RBM_NONE, RegSet::KEEP_REG);

            addrReg = genRegMask(tree->gtRegNum);
        }

        noway_assert((regSet.rsMaskLock & lockMask) == lockMask);
        regSet.rsMaskLock -= lockMask;

        return addrReg;
    }

    /* We have a complex address mode with some of its sub-operands spilled */

    noway_assert((tree->InReg()) == 0);
    noway_assert((tree->gtFlags & GTF_SPILLED_OPER) != 0);

    /*
        We'll proceed in several phases:

         1. Lock any registers that are part of the address mode and
            have not been spilled. This prevents these registers from
            getting spilled in step 2.

         2. Reload any registers that have been spilled; lock each
            one right after it is reloaded.

         3. Unlock all the registers.
     */

    addrReg = genRestoreAddrMode(tree, tree, true);
    addrReg |= genRestoreAddrMode(tree, tree, false);

    /* Unlock all registers that the address mode uses */

    lockMask |= addrReg;

    noway_assert((regSet.rsMaskLock & lockMask) == lockMask);
    regSet.rsMaskLock -= lockMask;

    return addrReg;
}

/*****************************************************************************
 *
 *  The given tree was previously passed to genMakeAddressable, but since then
 *  some of its registers might have been spilled ('addrReg' is the set of
 *  registers used by the address). This function makes sure the operand is
 *  still addressable (while avoiding any of the registers in 'avoidMask'),
 *  and returns the (possibly modified) set of registers that are used by
 *  the address (these will be marked as used on exit).
 */

regMaskTP CodeGen::genKeepAddressable(GenTreePtr tree, regMaskTP addrReg, regMaskTP avoidMask)
{
    /* Is the operand still addressable? */

    tree = tree->gtEffectiveVal(/*commaOnly*/ true); // Strip off commas for this purpose.

    if (!genStillAddressable(tree))
    {
        if (avoidMask)
        {
            // Temporarily lock 'avoidMask' while we restore addressability
            // genRestAddressable will unlock the 'avoidMask' for us
            // avoidMask must already be marked as a used reg in regSet.rsMaskUsed
            // In regSet.rsRegMaskFree() we require that all locked register be marked as used
            //
            regSet.rsLockUsedReg(avoidMask);
        }

        addrReg = genRestAddressable(tree, addrReg, avoidMask);

        noway_assert((regSet.rsMaskLock & avoidMask) == 0);
    }

    return addrReg;
}

/*****************************************************************************
 *
 *  After we're finished with the given operand (which was previously marked
 *  by calling genMakeAddressable), this function must be called to free any
 *  registers that may have been used by the address.
 *  keptReg indicates if the addressability registers were marked as used
 *  by genMakeAddressable().
 */

void CodeGen::genDoneAddressable(GenTreePtr tree, regMaskTP addrReg, RegSet::KeepReg keptReg)
{
    if (keptReg == RegSet::FREE_REG)
    {
        // We exclude regSet.rsMaskUsed since the registers may be multi-used.
        // ie. There may be a pending use in a higher-up tree.

        addrReg &= ~regSet.rsMaskUsed;

        /* addrReg was not marked as used. So just reset its GC info */
        if (addrReg)
        {
            gcInfo.gcMarkRegSetNpt(addrReg);
        }
    }
    else
    {
        /* addrReg was marked as used. So we need to free it up (which
           will also reset its GC info) */

        regSet.rsMarkRegFree(addrReg);
    }
}

/*****************************************************************************/
/*****************************************************************************
 *
 *  Make sure the given floating point value is addressable, and return a tree
 *  that will yield the value as an addressing mode (this tree may differ from
 *  the one passed in, BTW). If the only way to make the value addressable is
 *  to evaluate into the FP stack, we do this and return zero.
 */

GenTreePtr CodeGen::genMakeAddrOrFPstk(GenTreePtr tree, regMaskTP* regMaskPtr, bool roundResult)
{
    *regMaskPtr = 0;

    switch (tree->gtOper)
    {
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_CLS_VAR:
            return tree;

        case GT_CNS_DBL:
            if (tree->gtType == TYP_FLOAT)
            {
                float f = forceCastToFloat(tree->gtDblCon.gtDconVal);
                return genMakeConst(&f, TYP_FLOAT, tree, false);
            }
            return genMakeConst(&tree->gtDblCon.gtDconVal, tree->gtType, tree, true);

        case GT_IND:
        case GT_NULLCHECK:

            /* Try to make the address directly addressable */

            if (genMakeIndAddrMode(tree->gtOp.gtOp1, tree, false, /* not for LEA */
                                   0, RegSet::FREE_REG, regMaskPtr, false))
            {
                genUpdateLife(tree);
                return tree;
            }

            break;

        default:
            break;
    }
#if FEATURE_STACK_FP_X87
    /* We have no choice but to compute the value 'tree' onto the FP stack */

    genCodeForTreeFlt(tree);
#endif
    return 0;
}

/*****************************************************************************/
/*****************************************************************************
 *
 *  Display a string literal value (debug only).
 */

#ifdef DEBUG
#endif

/*****************************************************************************
 *
 *   Generate code to check that the GS cookie wasn't thrashed by a buffer
 *   overrun.  If pushReg is true, preserve all registers around code sequence.
 *   Otherwise, ECX maybe modified.
 *
 *   TODO-ARM-Bug?: pushReg is not implemented (is it needed for ARM?)
 */
void CodeGen::genEmitGSCookieCheck(bool pushReg)
{
    // Make sure that EAX didn't die in the return expression
    if (!pushReg && (compiler->info.compRetType == TYP_REF))
        gcInfo.gcRegGCrefSetCur |= RBM_INTRET;

    // Add cookie check code for unsafe buffers
    BasicBlock* gsCheckBlk;
    regMaskTP   byrefPushedRegs = RBM_NONE;
    regMaskTP   norefPushedRegs = RBM_NONE;
    regMaskTP   pushedRegs      = RBM_NONE;

    noway_assert(compiler->gsGlobalSecurityCookieAddr || compiler->gsGlobalSecurityCookieVal);

    if (compiler->gsGlobalSecurityCookieAddr == NULL)
    {
        // JIT case
        CLANG_FORMAT_COMMENT_ANCHOR;

#if CPU_LOAD_STORE_ARCH

        regNumber reg = regSet.rsGrabReg(RBM_ALLINT);
        getEmitter()->emitIns_R_S(ins_Load(TYP_INT), EA_4BYTE, reg, compiler->lvaGSSecurityCookie, 0);
        regTracker.rsTrackRegTrash(reg);

        if (arm_Valid_Imm_For_Alu(compiler->gsGlobalSecurityCookieVal) ||
            arm_Valid_Imm_For_Alu(~compiler->gsGlobalSecurityCookieVal))
        {
            getEmitter()->emitIns_R_I(INS_cmp, EA_4BYTE, reg, compiler->gsGlobalSecurityCookieVal);
        }
        else
        {
            // Load CookieVal into a register
            regNumber immReg = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(reg));
            instGen_Set_Reg_To_Imm(EA_4BYTE, immReg, compiler->gsGlobalSecurityCookieVal);
            getEmitter()->emitIns_R_R(INS_cmp, EA_4BYTE, reg, immReg);
        }
#else
        getEmitter()->emitIns_S_I(INS_cmp, EA_PTRSIZE, compiler->lvaGSSecurityCookie, 0,
                                  (int)compiler->gsGlobalSecurityCookieVal);
#endif
    }
    else
    {
        regNumber regGSCheck;
        regMaskTP regMaskGSCheck;
#if CPU_LOAD_STORE_ARCH
        regGSCheck     = regSet.rsGrabReg(RBM_ALLINT);
        regMaskGSCheck = genRegMask(regGSCheck);
#else
        // Don't pick the 'this' register
        if (compiler->lvaKeepAliveAndReportThis() && compiler->lvaTable[compiler->info.compThisArg].lvRegister &&
            (compiler->lvaTable[compiler->info.compThisArg].lvRegNum == REG_ECX))
        {
            regGSCheck     = REG_EDX;
            regMaskGSCheck = RBM_EDX;
        }
        else
        {
            regGSCheck     = REG_ECX;
            regMaskGSCheck = RBM_ECX;
        }

        // NGen case
        if (pushReg && (regMaskGSCheck & (regSet.rsMaskUsed | regSet.rsMaskVars | regSet.rsMaskLock)))
        {
            pushedRegs = genPushRegs(regMaskGSCheck, &byrefPushedRegs, &norefPushedRegs);
        }
        else
        {
            noway_assert((regMaskGSCheck & (regSet.rsMaskUsed | regSet.rsMaskVars | regSet.rsMaskLock)) == 0);
        }
#endif
#if defined(_TARGET_ARM_)
        instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, regGSCheck, (ssize_t)compiler->gsGlobalSecurityCookieAddr);
        getEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, regGSCheck, regGSCheck, 0);
#else
        getEmitter()->emitIns_R_C(ins_Load(TYP_I_IMPL), EA_PTR_DSP_RELOC, regGSCheck, FLD_GLOBAL_DS,
                                  (ssize_t)compiler->gsGlobalSecurityCookieAddr);
#endif // !_TARGET_ARM_
        regTracker.rsTrashRegSet(regMaskGSCheck);
#ifdef _TARGET_ARM_
        regNumber regTmp = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(regGSCheck));
        getEmitter()->emitIns_R_S(INS_ldr, EA_PTRSIZE, regTmp, compiler->lvaGSSecurityCookie, 0);
        regTracker.rsTrackRegTrash(regTmp);
        getEmitter()->emitIns_R_R(INS_cmp, EA_PTRSIZE, regTmp, regGSCheck);
#else
        getEmitter()->emitIns_S_R(INS_cmp, EA_PTRSIZE, regGSCheck, compiler->lvaGSSecurityCookie, 0);
#endif
    }

    gsCheckBlk            = genCreateTempLabel();
    emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
    inst_JMP(jmpEqual, gsCheckBlk);
    genEmitHelperCall(CORINFO_HELP_FAIL_FAST, 0, EA_UNKNOWN);
    genDefineTempLabel(gsCheckBlk);

    genPopRegs(pushedRegs, byrefPushedRegs, norefPushedRegs);
}

/*****************************************************************************
 *
 *  Generate any side effects within the given expression tree.
 */

void CodeGen::genEvalSideEffects(GenTreePtr tree)
{
    genTreeOps oper;
    unsigned   kind;

AGAIN:

    /* Does this sub-tree contain any side-effects? */
    if (tree->gtFlags & GTF_SIDE_EFFECT)
    {
#if FEATURE_STACK_FP_X87
        /* Remember the current FP stack level */
        int iTemps = genNumberTemps();
#endif
        if (tree->OperIsIndir())
        {
            regMaskTP addrReg = genMakeAddressable(tree, RBM_ALLINT, RegSet::KEEP_REG, true, false);

            if (tree->InReg())
            {
                gcInfo.gcMarkRegPtrVal(tree);
                genDoneAddressable(tree, addrReg, RegSet::KEEP_REG);
            }
            // GTF_IND_RNGCHK trees have already de-referenced the pointer, and so
            // do not need an additional null-check
            /* Do this only if the GTF_EXCEPT or GTF_IND_VOLATILE flag is set on the indir */
            else if ((tree->gtFlags & GTF_IND_ARR_INDEX) == 0 && ((tree->gtFlags & GTF_EXCEPT) | GTF_IND_VOLATILE))
            {
                /* Compare against any register to do null-check */
                CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_XARCH_)
                inst_TT_RV(INS_cmp, tree, REG_TMP_0, 0, EA_1BYTE);
                genDoneAddressable(tree, addrReg, RegSet::KEEP_REG);
#elif CPU_LOAD_STORE_ARCH
                if (varTypeIsFloating(tree->TypeGet()))
                {
                    genComputeAddressableFloat(tree, addrReg, RBM_NONE, RegSet::KEEP_REG, RBM_ALLFLOAT,
                                               RegSet::FREE_REG);
                }
                else
                {
                    genComputeAddressable(tree, addrReg, RegSet::KEEP_REG, RBM_NONE, RegSet::FREE_REG);
                }
#ifdef _TARGET_ARM_
                if (tree->gtFlags & GTF_IND_VOLATILE)
                {
                    // Emit a memory barrier instruction after the load
                    instGen_MemoryBarrier();
                }
#endif
#else
                NYI("TARGET");
#endif
            }
            else
            {
                genDoneAddressable(tree, addrReg, RegSet::KEEP_REG);
            }
        }
        else
        {
            /* Generate the expression and throw it away */
            genCodeForTree(tree, RBM_ALL(tree->TypeGet()));
            if (tree->InReg())
            {
                gcInfo.gcMarkRegPtrVal(tree);
            }
        }
#if FEATURE_STACK_FP_X87
        /* If the tree computed a value on the FP stack, pop the stack */
        if (genNumberTemps() > iTemps)
        {
            noway_assert(genNumberTemps() == iTemps + 1);
            genDiscardStackFP(tree);
        }
#endif
        return;
    }

    noway_assert(tree->gtOper != GT_ASG);

    /* Walk the tree, just to mark any dead values appropriately */

    oper = tree->OperGet();
    kind = tree->OperKind();

    /* Is this a constant or leaf node? */

    if (kind & (GTK_CONST | GTK_LEAF))
    {
#if FEATURE_STACK_FP_X87
        if (tree->IsRegVar() && isFloatRegType(tree->gtType) && tree->IsRegVarDeath())
        {
            genRegVarDeathStackFP(tree);
            FlatFPX87_Unload(&compCurFPState, tree->gtRegNum);
        }
#endif
        genUpdateLife(tree);
        gcInfo.gcMarkRegPtrVal(tree);
        return;
    }

    /* Must be a 'simple' unary/binary operator */

    noway_assert(kind & GTK_SMPOP);

    if (tree->gtGetOp2IfPresent())
    {
        genEvalSideEffects(tree->gtOp.gtOp1);

        tree = tree->gtOp.gtOp2;
        goto AGAIN;
    }
    else
    {
        tree = tree->gtOp.gtOp1;
        if (tree)
            goto AGAIN;
    }
}

/*****************************************************************************
 *
 *  A persistent pointer value is being overwritten, record it for the GC.
 *
 *  tgt        : the destination being written to
 *  assignVal  : the value being assigned (the source). It must currently be in a register.
 *  tgtAddrReg : the set of registers being used by "tgt"
 *
 *  Returns    : the mask of the scratch register that was used.
 *               RBM_NONE if a write-barrier is not needed.
 */

regMaskTP CodeGen::WriteBarrier(GenTreePtr tgt, GenTreePtr assignVal, regMaskTP tgtAddrReg)
{
    noway_assert(assignVal->InReg());

    GCInfo::WriteBarrierForm wbf = gcInfo.gcIsWriteBarrierCandidate(tgt, assignVal);
    if (wbf == GCInfo::WBF_NoBarrier)
        return RBM_NONE;

    regMaskTP resultRegMask = RBM_NONE;

#if FEATURE_WRITE_BARRIER

    regNumber reg = assignVal->gtRegNum;

#if defined(_TARGET_X86_) && NOGC_WRITE_BARRIERS
#ifdef DEBUG
    if (wbf != GCInfo::WBF_NoBarrier_CheckNotHeapInDebug) // This one is always a call to a C++ method.
    {
#endif
        const static int regToHelper[2][8] = {
            // If the target is known to be in managed memory
            {
                CORINFO_HELP_ASSIGN_REF_EAX, CORINFO_HELP_ASSIGN_REF_ECX, -1, CORINFO_HELP_ASSIGN_REF_EBX, -1,
                CORINFO_HELP_ASSIGN_REF_EBP, CORINFO_HELP_ASSIGN_REF_ESI, CORINFO_HELP_ASSIGN_REF_EDI,
            },

            // Don't know if the target is in managed memory
            {
                CORINFO_HELP_CHECKED_ASSIGN_REF_EAX, CORINFO_HELP_CHECKED_ASSIGN_REF_ECX, -1,
                CORINFO_HELP_CHECKED_ASSIGN_REF_EBX, -1, CORINFO_HELP_CHECKED_ASSIGN_REF_EBP,
                CORINFO_HELP_CHECKED_ASSIGN_REF_ESI, CORINFO_HELP_CHECKED_ASSIGN_REF_EDI,
            },
        };

        noway_assert(regToHelper[0][REG_EAX] == CORINFO_HELP_ASSIGN_REF_EAX);
        noway_assert(regToHelper[0][REG_ECX] == CORINFO_HELP_ASSIGN_REF_ECX);
        noway_assert(regToHelper[0][REG_EBX] == CORINFO_HELP_ASSIGN_REF_EBX);
        noway_assert(regToHelper[0][REG_ESP] == -1);
        noway_assert(regToHelper[0][REG_EBP] == CORINFO_HELP_ASSIGN_REF_EBP);
        noway_assert(regToHelper[0][REG_ESI] == CORINFO_HELP_ASSIGN_REF_ESI);
        noway_assert(regToHelper[0][REG_EDI] == CORINFO_HELP_ASSIGN_REF_EDI);

        noway_assert(regToHelper[1][REG_EAX] == CORINFO_HELP_CHECKED_ASSIGN_REF_EAX);
        noway_assert(regToHelper[1][REG_ECX] == CORINFO_HELP_CHECKED_ASSIGN_REF_ECX);
        noway_assert(regToHelper[1][REG_EBX] == CORINFO_HELP_CHECKED_ASSIGN_REF_EBX);
        noway_assert(regToHelper[1][REG_ESP] == -1);
        noway_assert(regToHelper[1][REG_EBP] == CORINFO_HELP_CHECKED_ASSIGN_REF_EBP);
        noway_assert(regToHelper[1][REG_ESI] == CORINFO_HELP_CHECKED_ASSIGN_REF_ESI);
        noway_assert(regToHelper[1][REG_EDI] == CORINFO_HELP_CHECKED_ASSIGN_REF_EDI);

        noway_assert((reg != REG_ESP) && (reg != REG_WRITE_BARRIER));

        /*
            Generate the following code:

                    lea     edx, tgt
                    call    write_barrier_helper_reg

            First grab the RBM_WRITE_BARRIER register for the target address.
         */

        regNumber rg1;
        bool      trashOp1;

        if ((tgtAddrReg & RBM_WRITE_BARRIER) == 0)
        {
            rg1 = regSet.rsGrabReg(RBM_WRITE_BARRIER);

            regSet.rsMaskUsed |= RBM_WRITE_BARRIER;
            regSet.rsMaskLock |= RBM_WRITE_BARRIER;

            trashOp1 = false;
        }
        else
        {
            rg1 = REG_WRITE_BARRIER;

            trashOp1 = true;
        }

        noway_assert(rg1 == REG_WRITE_BARRIER);

        /* Generate "lea EDX, [addr-mode]" */

        noway_assert(tgt->gtType == TYP_REF);
        tgt->gtType = TYP_BYREF;
        inst_RV_TT(INS_lea, rg1, tgt, 0, EA_BYREF);

        /* Free up anything that was tied up by the LHS */
        genDoneAddressable(tgt, tgtAddrReg, RegSet::KEEP_REG);

        // In case "tgt" was a comma:
        tgt = tgt->gtEffectiveVal();

        regTracker.rsTrackRegTrash(rg1);
        gcInfo.gcMarkRegSetNpt(genRegMask(rg1));
        gcInfo.gcMarkRegPtrVal(rg1, TYP_BYREF);

        /* Call the proper vm helper */

        // enforced by gcIsWriteBarrierCandidate
        noway_assert(tgt->gtOper == GT_IND || tgt->gtOper == GT_CLS_VAR);

        unsigned tgtAnywhere = 0;
        if ((tgt->gtOper == GT_IND) &&
            ((tgt->gtFlags & GTF_IND_TGTANYWHERE) || (tgt->gtOp.gtOp1->TypeGet() == TYP_I_IMPL)))
        {
            tgtAnywhere = 1;
        }

        int helper    = regToHelper[tgtAnywhere][reg];
        resultRegMask = genRegMask(reg);

        gcInfo.gcMarkRegSetNpt(RBM_WRITE_BARRIER); // byref EDX is killed in the call

        genEmitHelperCall(helper,
                          0,           // argSize
                          EA_PTRSIZE); // retSize

        if (!trashOp1)
        {
            regSet.rsMaskUsed &= ~RBM_WRITE_BARRIER;
            regSet.rsMaskLock &= ~RBM_WRITE_BARRIER;
        }

        return resultRegMask;

#ifdef DEBUG
    }
    else
#endif
#endif // defined(_TARGET_X86_) && NOGC_WRITE_BARRIERS

#if defined(DEBUG) || !(defined(_TARGET_X86_) && NOGC_WRITE_BARRIERS)
    {
        /*
            Generate the following code (or its equivalent on the given target):

                    mov     arg1, srcReg
                    lea     arg0, tgt
                    call    write_barrier_helper

            First, setup REG_ARG_1 with the GC ref that we are storing via the Write Barrier
         */

        if (reg != REG_ARG_1)
        {
            // We may need to spill whatever is in the ARG_1 register
            //
            if ((regSet.rsMaskUsed & RBM_ARG_1) != 0)
            {
                regSet.rsSpillReg(REG_ARG_1);
            }

            inst_RV_RV(INS_mov, REG_ARG_1, reg, TYP_REF);
        }
        resultRegMask = RBM_ARG_1;

        regTracker.rsTrackRegTrash(REG_ARG_1);
        gcInfo.gcMarkRegSetNpt(REG_ARG_1);
        gcInfo.gcMarkRegSetGCref(RBM_ARG_1); // gcref in ARG_1

        bool free_arg1 = false;
        if ((regSet.rsMaskUsed & RBM_ARG_1) == 0)
        {
            regSet.rsMaskUsed |= RBM_ARG_1;
            free_arg1 = true;
        }

        // Then we setup REG_ARG_0 with the target address to store into via the Write Barrier

        /* Generate "lea R0, [addr-mode]" */

        noway_assert(tgt->gtType == TYP_REF);
        tgt->gtType = TYP_BYREF;

        tgtAddrReg = genKeepAddressable(tgt, tgtAddrReg);

        // We may need to spill whatever is in the ARG_0 register
        //
        if (((tgtAddrReg & RBM_ARG_0) == 0) &&        // tgtAddrReg does not contain REG_ARG_0
            ((regSet.rsMaskUsed & RBM_ARG_0) != 0) && // and regSet.rsMaskUsed contains REG_ARG_0
            (reg != REG_ARG_0)) // unless REG_ARG_0 contains the REF value being written, which we're finished with.
        {
            regSet.rsSpillReg(REG_ARG_0);
        }

        inst_RV_TT(INS_lea, REG_ARG_0, tgt, 0, EA_BYREF);

        /* Free up anything that was tied up by the LHS */
        genDoneAddressable(tgt, tgtAddrReg, RegSet::KEEP_REG);

        regTracker.rsTrackRegTrash(REG_ARG_0);
        gcInfo.gcMarkRegSetNpt(REG_ARG_0);
        gcInfo.gcMarkRegSetByref(RBM_ARG_0); // byref in ARG_0

#ifdef _TARGET_ARM_
#if NOGC_WRITE_BARRIERS
        // Finally, we may be required to spill whatever is in the further argument registers
        // trashed by the call. The write barrier trashes some further registers --
        // either the standard volatile var set, or, if we're using assembly barriers, a more specialized set.

        regMaskTP volatileRegsTrashed = RBM_CALLEE_TRASH_NOGC;
#else
        regMaskTP volatileRegsTrashed = RBM_CALLEE_TRASH;
#endif
        // Spill any other registers trashed by the write barrier call and currently in use.
        regMaskTP mustSpill = (volatileRegsTrashed & regSet.rsMaskUsed & ~(RBM_ARG_0 | RBM_ARG_1));
        if (mustSpill)
            regSet.rsSpillRegs(mustSpill);
#endif // _TARGET_ARM_

        bool free_arg0 = false;
        if ((regSet.rsMaskUsed & RBM_ARG_0) == 0)
        {
            regSet.rsMaskUsed |= RBM_ARG_0;
            free_arg0 = true;
        }

        // genEmitHelperCall might need to grab a register
        // so don't let it spill one of the arguments
        //
        regMaskTP reallyUsedRegs = RBM_NONE;
        regSet.rsLockReg(RBM_ARG_0 | RBM_ARG_1, &reallyUsedRegs);

        genGCWriteBarrier(tgt, wbf);

        regSet.rsUnlockReg(RBM_ARG_0 | RBM_ARG_1, reallyUsedRegs);
        gcInfo.gcMarkRegSetNpt(RBM_ARG_0 | RBM_ARG_1); // byref ARG_0 and reg ARG_1 are killed by the call

        if (free_arg0)
        {
            regSet.rsMaskUsed &= ~RBM_ARG_0;
        }
        if (free_arg1)
        {
            regSet.rsMaskUsed &= ~RBM_ARG_1;
        }

        return resultRegMask;
    }
#endif // defined(DEBUG) || !(defined(_TARGET_X86_) && NOGC_WRITE_BARRIERS)

#else // !FEATURE_WRITE_BARRIER

    NYI("FEATURE_WRITE_BARRIER unimplemented");
    return resultRegMask;

#endif // !FEATURE_WRITE_BARRIER
}

#ifdef _TARGET_X86_
/*****************************************************************************
 *
 *  Generate the appropriate conditional jump(s) right after the low 32 bits
 *  of two long values have been compared.
 */

void CodeGen::genJccLongHi(genTreeOps cmp, BasicBlock* jumpTrue, BasicBlock* jumpFalse, bool isUnsigned)
{
    if (cmp != GT_NE)
    {
        jumpFalse->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;
    }

    switch (cmp)
    {
        case GT_EQ:
            inst_JMP(EJ_jne, jumpFalse);
            break;

        case GT_NE:
            inst_JMP(EJ_jne, jumpTrue);
            break;

        case GT_LT:
        case GT_LE:
            if (isUnsigned)
            {
                inst_JMP(EJ_ja, jumpFalse);
                inst_JMP(EJ_jb, jumpTrue);
            }
            else
            {
                inst_JMP(EJ_jg, jumpFalse);
                inst_JMP(EJ_jl, jumpTrue);
            }
            break;

        case GT_GE:
        case GT_GT:
            if (isUnsigned)
            {
                inst_JMP(EJ_jb, jumpFalse);
                inst_JMP(EJ_ja, jumpTrue);
            }
            else
            {
                inst_JMP(EJ_jl, jumpFalse);
                inst_JMP(EJ_jg, jumpTrue);
            }
            break;

        default:
            noway_assert(!"expected a comparison operator");
    }
}

/*****************************************************************************
 *
 *  Generate the appropriate conditional jump(s) right after the high 32 bits
 *  of two long values have been compared.
 */

void CodeGen::genJccLongLo(genTreeOps cmp, BasicBlock* jumpTrue, BasicBlock* jumpFalse)
{
    switch (cmp)
    {
        case GT_EQ:
            inst_JMP(EJ_je, jumpTrue);
            break;

        case GT_NE:
            inst_JMP(EJ_jne, jumpTrue);
            break;

        case GT_LT:
            inst_JMP(EJ_jb, jumpTrue);
            break;

        case GT_LE:
            inst_JMP(EJ_jbe, jumpTrue);
            break;

        case GT_GE:
            inst_JMP(EJ_jae, jumpTrue);
            break;

        case GT_GT:
            inst_JMP(EJ_ja, jumpTrue);
            break;

        default:
            noway_assert(!"expected comparison");
    }
}
#elif defined(_TARGET_ARM_)
/*****************************************************************************
*
*  Generate the appropriate conditional jump(s) right after the low 32 bits
*  of two long values have been compared.
*/

void CodeGen::genJccLongHi(genTreeOps cmp, BasicBlock* jumpTrue, BasicBlock* jumpFalse, bool isUnsigned)
{
    if (cmp != GT_NE)
    {
        jumpFalse->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL;
    }

    switch (cmp)
    {
        case GT_EQ:
            inst_JMP(EJ_ne, jumpFalse);
            break;

        case GT_NE:
            inst_JMP(EJ_ne, jumpTrue);
            break;

        case GT_LT:
        case GT_LE:
            if (isUnsigned)
            {
                inst_JMP(EJ_hi, jumpFalse);
                inst_JMP(EJ_lo, jumpTrue);
            }
            else
            {
                inst_JMP(EJ_gt, jumpFalse);
                inst_JMP(EJ_lt, jumpTrue);
            }
            break;

        case GT_GE:
        case GT_GT:
            if (isUnsigned)
            {
                inst_JMP(EJ_lo, jumpFalse);
                inst_JMP(EJ_hi, jumpTrue);
            }
            else
            {
                inst_JMP(EJ_lt, jumpFalse);
                inst_JMP(EJ_gt, jumpTrue);
            }
            break;

        default:
            noway_assert(!"expected a comparison operator");
    }
}

/*****************************************************************************
*
*  Generate the appropriate conditional jump(s) right after the high 32 bits
*  of two long values have been compared.
*/

void CodeGen::genJccLongLo(genTreeOps cmp, BasicBlock* jumpTrue, BasicBlock* jumpFalse)
{
    switch (cmp)
    {
        case GT_EQ:
            inst_JMP(EJ_eq, jumpTrue);
            break;

        case GT_NE:
            inst_JMP(EJ_ne, jumpTrue);
            break;

        case GT_LT:
            inst_JMP(EJ_lo, jumpTrue);
            break;

        case GT_LE:
            inst_JMP(EJ_ls, jumpTrue);
            break;

        case GT_GE:
            inst_JMP(EJ_hs, jumpTrue);
            break;

        case GT_GT:
            inst_JMP(EJ_hi, jumpTrue);
            break;

        default:
            noway_assert(!"expected comparison");
    }
}
#endif
/*****************************************************************************
 *
 *  Called by genCondJump() for TYP_LONG.
 */

void CodeGen::genCondJumpLng(GenTreePtr cond, BasicBlock* jumpTrue, BasicBlock* jumpFalse, bool bFPTransition)
{
    noway_assert(jumpTrue && jumpFalse);
    noway_assert((cond->gtFlags & GTF_REVERSE_OPS) == false); // Done in genCondJump()
    noway_assert(cond->gtOp.gtOp1->gtType == TYP_LONG);

    GenTreePtr op1 = cond->gtOp.gtOp1;
    GenTreePtr op2 = cond->gtOp.gtOp2;
    genTreeOps cmp = cond->OperGet();

    regMaskTP addrReg;

    /* Are we comparing against a constant? */

    if (op2->gtOper == GT_CNS_LNG)
    {
        __int64   lval = op2->gtLngCon.gtLconVal;
        regNumber rTmp;

        // We're "done" evaluating op2; let's strip any commas off op1 before we
        // evaluate it.
        op1 = genCodeForCommaTree(op1);

        /* We can generate better code for some special cases */
        instruction ins              = INS_invalid;
        bool        useIncToSetFlags = false;
        bool        specialCaseCmp   = false;

        if (cmp == GT_EQ)
        {
            if (lval == 0)
            {
                /* op1 == 0  */
                ins              = INS_OR;
                useIncToSetFlags = false;
                specialCaseCmp   = true;
            }
            else if (lval == -1)
            {
                /* op1 == -1 */
                ins              = INS_AND;
                useIncToSetFlags = true;
                specialCaseCmp   = true;
            }
        }
        else if (cmp == GT_NE)
        {
            if (lval == 0)
            {
                /* op1 != 0  */
                ins              = INS_OR;
                useIncToSetFlags = false;
                specialCaseCmp   = true;
            }
            else if (lval == -1)
            {
                /* op1 != -1 */
                ins              = INS_AND;
                useIncToSetFlags = true;
                specialCaseCmp   = true;
            }
        }

        if (specialCaseCmp)
        {
            /* Make the comparand addressable */

            addrReg = genMakeRvalueAddressable(op1, 0, RegSet::KEEP_REG, false, true);

            regMaskTP tmpMask = regSet.rsRegMaskCanGrab();
            insFlags  flags   = useIncToSetFlags ? INS_FLAGS_DONT_CARE : INS_FLAGS_SET;

            if (op1->InReg())
            {
                regPairNo regPair = op1->gtRegPair;
                regNumber rLo     = genRegPairLo(regPair);
                regNumber rHi     = genRegPairHi(regPair);
                if (tmpMask & genRegMask(rLo))
                {
                    rTmp = rLo;
                }
                else if (tmpMask & genRegMask(rHi))
                {
                    rTmp = rHi;
                    rHi  = rLo;
                }
                else
                {
                    rTmp = regSet.rsGrabReg(tmpMask);
                    inst_RV_RV(INS_mov, rTmp, rLo, TYP_INT);
                }

                /* The register is now trashed */
                regTracker.rsTrackRegTrash(rTmp);

                if (rHi != REG_STK)
                {
                    /* Set the flags using INS_AND | INS_OR */
                    inst_RV_RV(ins, rTmp, rHi, TYP_INT, EA_4BYTE, flags);
                }
                else
                {
                    /* Set the flags using INS_AND | INS_OR */
                    inst_RV_TT(ins, rTmp, op1, 4, EA_4BYTE, flags);
                }
            }
            else // op1 is not in a register.
            {
                rTmp = regSet.rsGrabReg(tmpMask);

                /* Load the low 32-bits of op1 */
                inst_RV_TT(ins_Load(TYP_INT), rTmp, op1, 0);

                /* The register is now trashed */
                regTracker.rsTrackRegTrash(rTmp);

                /* Set the flags using INS_AND | INS_OR */
                inst_RV_TT(ins, rTmp, op1, 4, EA_4BYTE, flags);
            }

            /* Free up the addrReg(s) if any */
            genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);

            /* compares against -1, also requires an an inc instruction */
            if (useIncToSetFlags)
            {
                /* Make sure the inc will set the flags */
                assert(cond->gtSetFlags());
                genIncRegBy(rTmp, 1, cond, TYP_INT);
            }

#if FEATURE_STACK_FP_X87
            // We may need a transition block
            if (bFPTransition)
            {
                jumpTrue = genTransitionBlockStackFP(&compCurFPState, compiler->compCurBB, jumpTrue);
            }
#endif
            emitJumpKind jmpKind = genJumpKindForOper(cmp, CK_SIGNED);
            inst_JMP(jmpKind, jumpTrue);
        }
        else // specialCaseCmp == false
        {
            /* Make the comparand addressable */
            addrReg = genMakeRvalueAddressable(op1, 0, RegSet::FREE_REG, false, true);

            /* Compare the high part first */

            int ival = (int)(lval >> 32);

            /* Comparing a register against 0 is easier */

            if (!ival && (op1->InReg()) && (rTmp = genRegPairHi(op1->gtRegPair)) != REG_STK)
            {
                /* Generate 'test rTmp, rTmp' */
                instGen_Compare_Reg_To_Zero(emitTypeSize(op1->TypeGet()), rTmp); // set flags
            }
            else
            {
                if (!(op1->InReg()) && (op1->gtOper == GT_CNS_LNG))
                {
                    /* Special case: comparison of two constants */
                    // Needed as gtFoldExpr() doesn't fold longs

                    noway_assert(addrReg == 0);
                    int op1_hiword = (int)(op1->gtLngCon.gtLconVal >> 32);

                    /* Get the constant operand into a register */
                    rTmp = genGetRegSetToIcon(op1_hiword);

                    /* Generate 'cmp rTmp, ival' */

                    inst_RV_IV(INS_cmp, rTmp, ival, EA_4BYTE);
                }
                else
                {
                    /* Generate 'cmp op1, ival' */

                    inst_TT_IV(INS_cmp, op1, ival, 4);
                }
            }

#if FEATURE_STACK_FP_X87
            // We may need a transition block
            if (bFPTransition)
            {
                jumpTrue = genTransitionBlockStackFP(&compCurFPState, compiler->compCurBB, jumpTrue);
            }
#endif
            /* Generate the appropriate jumps */

            if (cond->gtFlags & GTF_UNSIGNED)
                genJccLongHi(cmp, jumpTrue, jumpFalse, true);
            else
                genJccLongHi(cmp, jumpTrue, jumpFalse);

            /* Compare the low part second */

            ival = (int)lval;

            /* Comparing a register against 0 is easier */

            if (!ival && (op1->InReg()) && (rTmp = genRegPairLo(op1->gtRegPair)) != REG_STK)
            {
                /* Generate 'test rTmp, rTmp' */
                instGen_Compare_Reg_To_Zero(emitTypeSize(op1->TypeGet()), rTmp); // set flags
            }
            else
            {
                if (!(op1->InReg()) && (op1->gtOper == GT_CNS_LNG))
                {
                    /* Special case: comparison of two constants */
                    // Needed as gtFoldExpr() doesn't fold longs

                    noway_assert(addrReg == 0);
                    int op1_loword = (int)op1->gtLngCon.gtLconVal;

                    /* get the constant operand into a register */
                    rTmp = genGetRegSetToIcon(op1_loword);

                    /* Generate 'cmp rTmp, ival' */

                    inst_RV_IV(INS_cmp, rTmp, ival, EA_4BYTE);
                }
                else
                {
                    /* Generate 'cmp op1, ival' */

                    inst_TT_IV(INS_cmp, op1, ival, 0);
                }
            }

            /* Generate the appropriate jumps */
            genJccLongLo(cmp, jumpTrue, jumpFalse);

            genDoneAddressable(op1, addrReg, RegSet::FREE_REG);
        }
    }
    else // (op2->gtOper != GT_CNS_LNG)
    {

        /* The operands would be reversed by physically swapping them */

        noway_assert((cond->gtFlags & GTF_REVERSE_OPS) == 0);

        /* Generate the first operand into a register pair */

        genComputeRegPair(op1, REG_PAIR_NONE, op2->gtRsvdRegs, RegSet::KEEP_REG, false);
        noway_assert(op1->InReg());

#if CPU_LOAD_STORE_ARCH
        /* Generate the second operand into a register pair */
        // Fix 388442 ARM JitStress WP7
        genComputeRegPair(op2, REG_PAIR_NONE, genRegPairMask(op1->gtRegPair), RegSet::KEEP_REG, false);
        noway_assert(op2->InReg());
        regSet.rsLockUsedReg(genRegPairMask(op2->gtRegPair));
#else
        /* Make the second operand addressable */

        addrReg = genMakeRvalueAddressable(op2, RBM_ALLINT & ~genRegPairMask(op1->gtRegPair), RegSet::KEEP_REG, false);
#endif
        /* Make sure the first operand hasn't been spilled */

        genRecoverRegPair(op1, REG_PAIR_NONE, RegSet::KEEP_REG);
        noway_assert(op1->InReg());

        regPairNo regPair = op1->gtRegPair;

#if !CPU_LOAD_STORE_ARCH
        /* Make sure 'op2' is still addressable while avoiding 'op1' (regPair) */

        addrReg = genKeepAddressable(op2, addrReg, genRegPairMask(regPair));
#endif

#if FEATURE_STACK_FP_X87
        // We may need a transition block
        if (bFPTransition)
        {
            jumpTrue = genTransitionBlockStackFP(&compCurFPState, compiler->compCurBB, jumpTrue);
        }
#endif

        /* Perform the comparison - high parts */

        inst_RV_TT(INS_cmp, genRegPairHi(regPair), op2, 4);

        if (cond->gtFlags & GTF_UNSIGNED)
            genJccLongHi(cmp, jumpTrue, jumpFalse, true);
        else
            genJccLongHi(cmp, jumpTrue, jumpFalse);

        /* Compare the low parts */

        inst_RV_TT(INS_cmp, genRegPairLo(regPair), op2, 0);
        genJccLongLo(cmp, jumpTrue, jumpFalse);

        /* Free up anything that was tied up by either operand */
        CLANG_FORMAT_COMMENT_ANCHOR;

#if CPU_LOAD_STORE_ARCH

        // Fix 388442 ARM JitStress WP7
        regSet.rsUnlockUsedReg(genRegPairMask(op2->gtRegPair));
        genReleaseRegPair(op2);
#else
        genDoneAddressable(op2, addrReg, RegSet::KEEP_REG);
#endif
        genReleaseRegPair(op1);
    }
}

/*****************************************************************************
 *  gen_fcomp_FN, gen_fcomp_FS_TT, gen_fcompp_FS
 *  Called by genCondJumpFlt() to generate the fcomp instruction appropriate
 *  to the architecture we're running on.
 *
 *  P5:
 *  gen_fcomp_FN:     fcomp ST(0), stk
 *  gen_fcomp_FS_TT:  fcomp ST(0), addr
 *  gen_fcompp_FS:    fcompp
 *    These are followed by fnstsw, sahf to get the flags in EFLAGS.
 *
 *  P6:
 *  gen_fcomp_FN:     fcomip ST(0), stk
 *  gen_fcomp_FS_TT:  fld addr, fcomip ST(0), ST(1), fstp ST(0)
 *      (and reverse the branch condition since addr comes first)
 *  gen_fcompp_FS:    fcomip, fstp
 *    These instructions will correctly set the EFLAGS register.
 *
 *  Return value:  These functions return true if the instruction has
 *    already placed its result in the EFLAGS register.
 */

bool CodeGen::genUse_fcomip()
{
    return compiler->opts.compUseFCOMI;
}

/*****************************************************************************
 *
 *  Sets the flag for the TYP_INT/TYP_REF comparison.
 *  We try to use the flags if they have already been set by a prior
 *  instruction.
 *  eg. i++; if(i<0) {}  Here, the "i++;" will have set the sign flag. We don't
 *                       need to compare again with zero. Just use a "INS_js"
 *
 *  Returns the flags the following jump/set instruction should use.
 */

emitJumpKind CodeGen::genCondSetFlags(GenTreePtr cond)
{
    noway_assert(cond->OperIsCompare());
    noway_assert(varTypeIsI(genActualType(cond->gtOp.gtOp1->gtType)));

    GenTreePtr op1 = cond->gtOp.gtOp1;
    GenTreePtr op2 = cond->gtOp.gtOp2;
    genTreeOps cmp = cond->OperGet();

    if (cond->gtFlags & GTF_REVERSE_OPS)
    {
        /* Don't forget to modify the condition as well */

        cond->gtOp.gtOp1 = op2;
        cond->gtOp.gtOp2 = op1;
        cond->SetOper(GenTree::SwapRelop(cmp));
        cond->gtFlags &= ~GTF_REVERSE_OPS;

        /* Get hold of the new values */

        cmp = cond->OperGet();
        op1 = cond->gtOp.gtOp1;
        op2 = cond->gtOp.gtOp2;
    }

    // Note that op1's type may get bashed. So save it early

    var_types op1Type     = op1->TypeGet();
    bool      unsignedCmp = (cond->gtFlags & GTF_UNSIGNED) != 0;
    emitAttr  size        = EA_UNKNOWN;

    regMaskTP    regNeed;
    regMaskTP    addrReg1 = RBM_NONE;
    regMaskTP    addrReg2 = RBM_NONE;
    emitJumpKind jumpKind = EJ_COUNT; // Initialize with an invalid value

    bool byteCmp;
    bool shortCmp;

    regMaskTP newLiveMask;
    regNumber op1Reg;

    /* Are we comparing against a constant? */

    if (op2->IsCnsIntOrI())
    {
        ssize_t ival = op2->gtIntConCommon.IconValue();

        /* unsigned less than comparisons with 1 ('< 1' )
           should be transformed into '== 0' to potentially
           suppress a tst instruction.
        */
        if ((ival == 1) && (cmp == GT_LT) && unsignedCmp)
        {
            op2->gtIntCon.gtIconVal = ival = 0;
            cond->gtOper = cmp = GT_EQ;
        }

        /* Comparisons against 0 can be easier */

        if (ival == 0)
        {
            // if we can safely change the comparison to unsigned we do so
            if (!unsignedCmp && varTypeIsSmall(op1->TypeGet()) && varTypeIsUnsigned(op1->TypeGet()))
            {
                unsignedCmp = true;
            }

            /* unsigned comparisons with 0 should be transformed into
               '==0' or '!= 0' to potentially suppress a tst instruction. */

            if (unsignedCmp)
            {
                if (cmp == GT_GT)
                    cond->gtOper = cmp = GT_NE;
                else if (cmp == GT_LE)
                    cond->gtOper = cmp = GT_EQ;
            }

            /* Is this a simple zero/non-zero test? */

            if (cmp == GT_EQ || cmp == GT_NE)
            {
                /* Is the operand an "AND" operation? */

                if (op1->gtOper == GT_AND)
                {
                    GenTreePtr an1 = op1->gtOp.gtOp1;
                    GenTreePtr an2 = op1->gtOp.gtOp2;

                    /* Check for the case "expr & icon" */

                    if (an2->IsIntCnsFitsInI32())
                    {
                        int iVal = (int)an2->gtIntCon.gtIconVal;

                        /* make sure that constant is not out of an1's range */

                        switch (an1->gtType)
                        {
                            case TYP_BOOL:
                            case TYP_BYTE:
                                if (iVal & 0xffffff00)
                                    goto NO_TEST_FOR_AND;
                                break;
                            case TYP_CHAR:
                            case TYP_SHORT:
                                if (iVal & 0xffff0000)
                                    goto NO_TEST_FOR_AND;
                                break;
                            default:
                                break;
                        }

                        if (an1->IsCnsIntOrI())
                        {
                            // Special case - Both operands of AND are consts
                            genComputeReg(an1, 0, RegSet::EXACT_REG, RegSet::KEEP_REG);
                            addrReg1 = genRegMask(an1->gtRegNum);
                        }
                        else
                        {
                            addrReg1 = genMakeAddressable(an1, RBM_NONE, RegSet::KEEP_REG, true);
                        }
#if CPU_LOAD_STORE_ARCH
                        if ((an1->InReg()) == 0)
                        {
                            genComputeAddressable(an1, addrReg1, RegSet::KEEP_REG, RBM_NONE, RegSet::KEEP_REG);
                            if (arm_Valid_Imm_For_Alu(iVal))
                            {
                                inst_RV_IV(INS_TEST, an1->gtRegNum, iVal, emitActualTypeSize(an1->gtType));
                            }
                            else
                            {
                                regNumber regTmp = regSet.rsPickFreeReg();
                                instGen_Set_Reg_To_Imm(EmitSize(an2), regTmp, iVal);
                                inst_RV_RV(INS_TEST, an1->gtRegNum, regTmp);
                            }
                            genReleaseReg(an1);
                            addrReg1 = RBM_NONE;
                        }
                        else
#endif
                        {
#ifdef _TARGET_XARCH_
                            // Check to see if we can use a smaller immediate.
                            if ((an1->InReg()) && ((iVal & 0x0000FFFF) == iVal))
                            {
                                var_types testType =
                                    (var_types)(((iVal & 0x000000FF) == iVal) ? TYP_UBYTE : TYP_USHORT);
#if CPU_HAS_BYTE_REGS
                                // if we don't have byte-able register, switch to the 2-byte form
                                if ((testType == TYP_UBYTE) && !(genRegMask(an1->gtRegNum) & RBM_BYTE_REGS))
                                {
                                    testType = TYP_USHORT;
                                }
#endif // CPU_HAS_BYTE_REGS

                                inst_TT_IV(INS_TEST, an1, iVal, testType);
                            }
                            else
#endif // _TARGET_XARCH_
                            {
                                inst_TT_IV(INS_TEST, an1, iVal);
                            }
                        }

                        goto DONE;

                    NO_TEST_FOR_AND:;
                    }

                    // TODO: Check for other cases that can generate 'test',
                    // TODO: also check for a 64-bit integer zero test which
                    // TODO: could generate 'or lo, hi' followed by jz/jnz.
                }
            }

            // See what Jcc instruction we would use if we can take advantage of
            // the knowledge of EFLAGs.

            if (unsignedCmp)
            {
                /*
                    Unsigned comparison to 0. Using this table:

                    ----------------------------------------------------
                    | Comparison | Flags Checked    | Instruction Used |
                    ----------------------------------------------------
                    |    == 0    | ZF = 1           |       je         |
                    ----------------------------------------------------
                    |    != 0    | ZF = 0           |       jne        |
                    ----------------------------------------------------
                    |     < 0    | always FALSE     |       N/A        |
                    ----------------------------------------------------
                    |    <= 0    | ZF = 1           |       je         |
                    ----------------------------------------------------
                    |    >= 0    | always TRUE      |       N/A        |
                    ----------------------------------------------------
                    |     > 0    | ZF = 0           |       jne        |
                    ----------------------------------------------------
                */
                switch (cmp)
                {
#ifdef _TARGET_ARM_
                    case GT_EQ:
                        jumpKind = EJ_eq;
                        break;
                    case GT_NE:
                        jumpKind = EJ_ne;
                        break;
                    case GT_LT:
                        jumpKind = EJ_NONE;
                        break;
                    case GT_LE:
                        jumpKind = EJ_eq;
                        break;
                    case GT_GE:
                        jumpKind = EJ_NONE;
                        break;
                    case GT_GT:
                        jumpKind = EJ_ne;
                        break;
#elif defined(_TARGET_X86_)
                    case GT_EQ:
                        jumpKind = EJ_je;
                        break;
                    case GT_NE:
                        jumpKind = EJ_jne;
                        break;
                    case GT_LT:
                        jumpKind = EJ_NONE;
                        break;
                    case GT_LE:
                        jumpKind = EJ_je;
                        break;
                    case GT_GE:
                        jumpKind = EJ_NONE;
                        break;
                    case GT_GT:
                        jumpKind = EJ_jne;
                        break;
#endif // TARGET
                    default:
                        noway_assert(!"Unexpected comparison OpCode");
                        break;
                }
            }
            else
            {
                /*
                    Signed comparison to 0. Using this table:

                    -----------------------------------------------------
                    | Comparison | Flags Checked     | Instruction Used |
                    -----------------------------------------------------
                    |    == 0    | ZF = 1            |       je         |
                    -----------------------------------------------------
                    |    != 0    | ZF = 0            |       jne        |
                    -----------------------------------------------------
                    |     < 0    | SF = 1            |       js         |
                    -----------------------------------------------------
                    |    <= 0    |      N/A          |       N/A        |
                    -----------------------------------------------------
                    |    >= 0    | SF = 0            |       jns        |
                    -----------------------------------------------------
                    |     > 0    |      N/A          |       N/A        |
                    -----------------------------------------------------
                */

                switch (cmp)
                {
#ifdef _TARGET_ARM_
                    case GT_EQ:
                        jumpKind = EJ_eq;
                        break;
                    case GT_NE:
                        jumpKind = EJ_ne;
                        break;
                    case GT_LT:
                        jumpKind = EJ_mi;
                        break;
                    case GT_LE:
                        jumpKind = EJ_NONE;
                        break;
                    case GT_GE:
                        jumpKind = EJ_pl;
                        break;
                    case GT_GT:
                        jumpKind = EJ_NONE;
                        break;
#elif defined(_TARGET_X86_)
                    case GT_EQ:
                        jumpKind = EJ_je;
                        break;
                    case GT_NE:
                        jumpKind = EJ_jne;
                        break;
                    case GT_LT:
                        jumpKind = EJ_js;
                        break;
                    case GT_LE:
                        jumpKind = EJ_NONE;
                        break;
                    case GT_GE:
                        jumpKind = EJ_jns;
                        break;
                    case GT_GT:
                        jumpKind = EJ_NONE;
                        break;
#endif // TARGET
                    default:
                        noway_assert(!"Unexpected comparison OpCode");
                        break;
                }
                assert(jumpKind == genJumpKindForOper(cmp, CK_LOGICAL));
            }
            assert(jumpKind != EJ_COUNT); // Ensure that it was assigned a valid value above

            /* Is the value a simple local variable? */

            if (op1->gtOper == GT_LCL_VAR)
            {
                /* Is the flags register set to the value? */

                if (genFlagsAreVar(op1->gtLclVarCommon.gtLclNum))
                {
                    if (jumpKind != EJ_NONE)
                    {
                        addrReg1 = RBM_NONE;
                        genUpdateLife(op1);
                        goto DONE_FLAGS;
                    }
                }
            }

            /* Make the comparand addressable */
            addrReg1 = genMakeRvalueAddressable(op1, RBM_NONE, RegSet::KEEP_REG, false, true);

            /* Are the condition flags set based on the value? */

            unsigned flags = (op1->gtFlags & GTF_ZSF_SET);

            if (op1->InReg())
            {
                if (genFlagsAreReg(op1->gtRegNum))
                {
                    flags |= GTF_ZSF_SET;
                }
            }

            if (flags)
            {
                if (jumpKind != EJ_NONE)
                {
                    goto DONE_FLAGS;
                }
            }

            /* Is the value in a register? */

            if (op1->InReg())
            {
                regNumber reg = op1->gtRegNum;

                /* With a 'test' we can do any signed test or any test for equality */

                if (!(cond->gtFlags & GTF_UNSIGNED) || cmp == GT_EQ || cmp == GT_NE)
                {
                    emitAttr compareSize = emitTypeSize(op1->TypeGet());

                    // If we have an GT_REG_VAR then the register will be properly sign/zero extended
                    // But only up to 4 bytes
                    if ((op1->gtOper == GT_REG_VAR) && (compareSize < EA_4BYTE))
                    {
                        compareSize = EA_4BYTE;
                    }

#if CPU_HAS_BYTE_REGS
                    // Make sure if we require a byte compare that we have a byte-able register
                    if ((compareSize != EA_1BYTE) || ((genRegMask(op1->gtRegNum) & RBM_BYTE_REGS) != 0))
#endif // CPU_HAS_BYTE_REGS
                    {
                        /* Generate 'test reg, reg' */
                        instGen_Compare_Reg_To_Zero(compareSize, reg);
                        goto DONE;
                    }
                }
            }
        }

        else // if (ival != 0)
        {
            bool smallOk = true;

            /* make sure that constant is not out of op1's range
               if it is, we need to perform an int with int comparison
               and therefore, we set smallOk to false, so op1 gets loaded
               into a register
            */

            /* If op1 is TYP_SHORT, and is followed by an unsigned
             * comparison, we can use smallOk. But we don't know which
             * flags will be needed. This probably doesn't happen often.
            */
            var_types gtType = op1->TypeGet();

            switch (gtType)
            {
                case TYP_BYTE:
                    if (ival != (signed char)ival)
                        smallOk = false;
                    break;
                case TYP_BOOL:
                case TYP_UBYTE:
                    if (ival != (unsigned char)ival)
                        smallOk = false;
                    break;

                case TYP_SHORT:
                    if (ival != (signed short)ival)
                        smallOk = false;
                    break;
                case TYP_CHAR:
                    if (ival != (unsigned short)ival)
                        smallOk = false;
                    break;

#ifdef _TARGET_64BIT_
                case TYP_INT:
                    if (!FitsIn<INT32>(ival))
                        smallOk = false;
                    break;
                case TYP_UINT:
                    if (!FitsIn<UINT32>(ival))
                        smallOk = false;
                    break;
#endif // _TARGET_64BIT_

                default:
                    break;
            }

            if (smallOk &&                 // constant is in op1's range
                !unsignedCmp &&            // signed comparison
                varTypeIsSmall(gtType) &&  // smalltype var
                varTypeIsUnsigned(gtType)) // unsigned type
            {
                unsignedCmp = true;
            }

            /* Make the comparand addressable */
            addrReg1 = genMakeRvalueAddressable(op1, RBM_NONE, RegSet::KEEP_REG, false, smallOk);
        }

        /* Special case: comparison of two constants */

        // Needed if Importer doesn't call gtFoldExpr()

        if (!(op1->InReg()) && (op1->IsCnsIntOrI()))
        {
            // noway_assert(compiler->opts.MinOpts() || compiler->opts.compDbgCode);

            /* Workaround: get the constant operand into a register */
            genComputeReg(op1, RBM_NONE, RegSet::ANY_REG, RegSet::KEEP_REG);

            noway_assert(addrReg1 == RBM_NONE);
            noway_assert(op1->InReg());

            addrReg1 = genRegMask(op1->gtRegNum);
        }

        /* Compare the operand against the constant */

        if (op2->IsIconHandle())
        {
            inst_TT_IV(INS_cmp, op1, ival, 0, EA_HANDLE_CNS_RELOC);
        }
        else
        {
            inst_TT_IV(INS_cmp, op1, ival);
        }
        goto DONE;
    }

    //---------------------------------------------------------------------
    //
    // We reach here if op2 was not a GT_CNS_INT
    //

    byteCmp  = false;
    shortCmp = false;

    if (op1Type == op2->gtType)
    {
        shortCmp = varTypeIsShort(op1Type);
        byteCmp  = varTypeIsByte(op1Type);
    }

    noway_assert(op1->gtOper != GT_CNS_INT);

    if (op2->gtOper == GT_LCL_VAR)
        genMarkLclVar(op2);

    assert(((addrReg1 | addrReg2) & regSet.rsMaskUsed) == (addrReg1 | addrReg2));
    assert(((addrReg1 & addrReg2) & regSet.rsMaskMult) == (addrReg1 & addrReg2));

    /* Are we comparing against a register? */

    if (op2->InReg())
    {
        /* Make the comparands addressable and mark as used */

        assert(addrReg1 == RBM_NONE);
        addrReg1 = genMakeAddressable2(op1, RBM_NONE, RegSet::KEEP_REG, false, true);

        /* Is the size of the comparison byte/char/short ? */

        if (varTypeIsSmall(op1->TypeGet()))
        {
            /* Is op2 sitting in an appropriate register? */

            if (varTypeIsByte(op1->TypeGet()) && !isByteReg(op2->gtRegNum))
                goto NO_SMALL_CMP;

            /* Is op2 of the right type for a small comparison */

            if (op2->gtOper == GT_REG_VAR)
            {
                if (op1->gtType != compiler->lvaGetRealType(op2->gtRegVar.gtLclNum))
                    goto NO_SMALL_CMP;
            }
            else
            {
                if (op1->gtType != op2->gtType)
                    goto NO_SMALL_CMP;
            }

            if (varTypeIsUnsigned(op1->TypeGet()))
                unsignedCmp = true;
        }

        assert(addrReg2 == RBM_NONE);

        genComputeReg(op2, RBM_NONE, RegSet::ANY_REG, RegSet::KEEP_REG);
        addrReg2 = genRegMask(op2->gtRegNum);
        addrReg1 = genKeepAddressable(op1, addrReg1, addrReg2);
        assert(((addrReg1 | addrReg2) & regSet.rsMaskUsed) == (addrReg1 | addrReg2));
        assert(((addrReg1 & addrReg2) & regSet.rsMaskMult) == (addrReg1 & addrReg2));

        /* Compare against the register */

        inst_TT_RV(INS_cmp, op1, op2->gtRegNum);

        goto DONE;

    NO_SMALL_CMP:

        // op1 has been made addressable and is marked as in use
        // op2 is un-generated
        assert(addrReg2 == 0);

        if ((op1->InReg()) == 0)
        {
            regNumber reg1 = regSet.rsPickReg();

            noway_assert(varTypeIsSmall(op1->TypeGet()));
            instruction ins = ins_Move_Extend(op1->TypeGet(), (op1->InReg()) != 0);

            // regSet.rsPickReg can cause one of the trees within this address mode to get spilled
            // so we need to make sure it is still valid.  Note that at this point, reg1 is
            // *not* marked as in use, and it is possible for it to be used in the address
            // mode expression, but that is OK, because we are done with expression after
            // this.  We only need reg1.
            addrReg1 = genKeepAddressable(op1, addrReg1);
            inst_RV_TT(ins, reg1, op1);
            regTracker.rsTrackRegTrash(reg1);

            genDoneAddressable(op1, addrReg1, RegSet::KEEP_REG);
            addrReg1 = 0;

            genMarkTreeInReg(op1, reg1);

            regSet.rsMarkRegUsed(op1);
            addrReg1 = genRegMask(op1->gtRegNum);
        }

        assert(((addrReg1 | addrReg2) & regSet.rsMaskUsed) == (addrReg1 | addrReg2));
        assert(((addrReg1 & addrReg2) & regSet.rsMaskMult) == (addrReg1 & addrReg2));

        goto DONE_OP1;
    }

    // We come here if op2 is not enregistered or not in a "good" register.

    assert(addrReg1 == 0);

    // Determine what registers go live between op1 and op2
    newLiveMask = genNewLiveRegMask(op1, op2);

    // Setup regNeed with the set of register that we suggest for op1 to be in
    //
    regNeed = RBM_ALLINT;

    // avoid selecting registers that get newly born in op2
    regNeed = regSet.rsNarrowHint(regNeed, ~newLiveMask);

    // avoid selecting op2 reserved regs
    regNeed = regSet.rsNarrowHint(regNeed, ~op2->gtRsvdRegs);

#if CPU_HAS_BYTE_REGS
    // if necessary setup regNeed to select just the byte-able registers
    if (byteCmp)
        regNeed = regSet.rsNarrowHint(RBM_BYTE_REGS, regNeed);
#endif // CPU_HAS_BYTE_REGS

    // Compute the first comparand into some register, regNeed here is simply a hint because RegSet::ANY_REG is used.
    //
    genComputeReg(op1, regNeed, RegSet::ANY_REG, RegSet::FREE_REG);
    noway_assert(op1->InReg());

    op1Reg = op1->gtRegNum;

    // Setup regNeed with the set of register that we require for op1 to be in
    //
    regNeed = RBM_ALLINT;

#if CPU_HAS_BYTE_REGS
    // if necessary setup regNeed to select just the byte-able registers
    if (byteCmp)
        regNeed &= RBM_BYTE_REGS;
#endif // CPU_HAS_BYTE_REGS

    // avoid selecting registers that get newly born in op2, as using them will force a spill temp to be used.
    regNeed = regSet.rsMustExclude(regNeed, newLiveMask);

    // avoid selecting op2 reserved regs, as using them will force a spill temp to be used.
    regNeed = regSet.rsMustExclude(regNeed, op2->gtRsvdRegs);

    // Did we end up in an acceptable register?
    // and do we have an acceptable free register available to grab?
    //
    if (((genRegMask(op1Reg) & regNeed) == 0) && ((regSet.rsRegMaskFree() & regNeed) != 0))
    {
        // Grab an acceptable register
        regNumber newReg = regSet.rsGrabReg(regNeed);

        noway_assert(op1Reg != newReg);

        /* Update the value in the target register */

        regTracker.rsTrackRegCopy(newReg, op1Reg);

        inst_RV_RV(ins_Copy(op1->TypeGet()), newReg, op1Reg, op1->TypeGet());

        /* The value has been transferred to 'reg' */

        if ((genRegMask(op1Reg) & regSet.rsMaskUsed) == 0)
            gcInfo.gcMarkRegSetNpt(genRegMask(op1Reg));

        gcInfo.gcMarkRegPtrVal(newReg, op1->TypeGet());

        /* The value is now in an appropriate register */

        op1->gtRegNum = newReg;
    }
    noway_assert(op1->InReg());
    op1Reg = op1->gtRegNum;

    genUpdateLife(op1);

    /* Mark the register as 'used' */
    regSet.rsMarkRegUsed(op1);

    addrReg1 = genRegMask(op1Reg);

    assert(((addrReg1 | addrReg2) & regSet.rsMaskUsed) == (addrReg1 | addrReg2));
    assert(((addrReg1 & addrReg2) & regSet.rsMaskMult) == (addrReg1 & addrReg2));

DONE_OP1:

    assert(((addrReg1 | addrReg2) & regSet.rsMaskUsed) == (addrReg1 | addrReg2));
    assert(((addrReg1 & addrReg2) & regSet.rsMaskMult) == (addrReg1 & addrReg2));
    noway_assert(op1->InReg());

    // Setup regNeed with either RBM_ALLINT or the RBM_BYTE_REGS subset
    // when byteCmp is true we will perform a byte sized cmp instruction
    // and that instruction requires that any registers used are byte-able ones.
    //
    regNeed = RBM_ALLINT;

#if CPU_HAS_BYTE_REGS
    // if necessary setup regNeed to select just the byte-able registers
    if (byteCmp)
        regNeed &= RBM_BYTE_REGS;
#endif // CPU_HAS_BYTE_REGS

    /* Make the comparand addressable */
    assert(addrReg2 == 0);
    addrReg2 = genMakeRvalueAddressable(op2, regNeed, RegSet::KEEP_REG, false, (byteCmp | shortCmp));

    /*  Make sure the first operand is still in a register; if
        it's been spilled, we have to make sure it's reloaded
        into a byte-addressable register if needed.
        Pass keepReg=RegSet::KEEP_REG. Otherwise get pointer lifetimes wrong.
     */

    assert(addrReg1 != 0);
    genRecoverReg(op1, regNeed, RegSet::KEEP_REG);

    noway_assert(op1->InReg());
    noway_assert(!byteCmp || isByteReg(op1->gtRegNum));

    addrReg1 = genRegMask(op1->gtRegNum);
    regSet.rsLockUsedReg(addrReg1);

    /* Make sure that op2 is addressable. If we are going to do a
       byte-comparison, we need it to be in a byte register. */

    if (byteCmp && (op2->InReg()))
    {
        genRecoverReg(op2, regNeed, RegSet::KEEP_REG);
        addrReg2 = genRegMask(op2->gtRegNum);
    }
    else
    {
        addrReg2 = genKeepAddressable(op2, addrReg2);
    }

    regSet.rsUnlockUsedReg(addrReg1);

    assert(((addrReg1 | addrReg2) & regSet.rsMaskUsed) == (addrReg1 | addrReg2));
    assert(((addrReg1 & addrReg2) & regSet.rsMaskMult) == (addrReg1 & addrReg2));

    if (byteCmp || shortCmp)
    {
        size = emitTypeSize(op2->TypeGet());
        if (varTypeIsUnsigned(op1Type))
            unsignedCmp = true;
    }
    else
    {
        size = emitActualTypeSize(op2->TypeGet());
    }

    /* Perform the comparison */
    inst_RV_TT(INS_cmp, op1->gtRegNum, op2, 0, size);

DONE:

    jumpKind = genJumpKindForOper(cmp, unsignedCmp ? CK_UNSIGNED : CK_SIGNED);

DONE_FLAGS: // We have determined what jumpKind to use

    genUpdateLife(cond);

    /* The condition value is dead at the jump that follows */

    assert(((addrReg1 | addrReg2) & regSet.rsMaskUsed) == (addrReg1 | addrReg2));
    assert(((addrReg1 & addrReg2) & regSet.rsMaskMult) == (addrReg1 & addrReg2));
    genDoneAddressable(op1, addrReg1, RegSet::KEEP_REG);
    genDoneAddressable(op2, addrReg2, RegSet::KEEP_REG);

    noway_assert(jumpKind != EJ_COUNT); // Ensure that it was assigned a valid value

    return jumpKind;
}

/*****************************************************************************/
/*****************************************************************************/
/*****************************************************************************
 *
 *  Generate code to jump to the jump target of the current basic block if
 *  the given relational operator yields 'true'.
 */

void CodeGen::genCondJump(GenTreePtr cond, BasicBlock* destTrue, BasicBlock* destFalse, bool bStackFPFixup)
{
    BasicBlock* jumpTrue;
    BasicBlock* jumpFalse;

    GenTreePtr op1 = cond->gtOp.gtOp1;
    GenTreePtr op2 = cond->gtOp.gtOp2;
    genTreeOps cmp = cond->OperGet();

    if (destTrue)
    {
        jumpTrue  = destTrue;
        jumpFalse = destFalse;
    }
    else
    {
        noway_assert(compiler->compCurBB->bbJumpKind == BBJ_COND);

        jumpTrue  = compiler->compCurBB->bbJumpDest;
        jumpFalse = compiler->compCurBB->bbNext;
    }

    noway_assert(cond->OperIsCompare());

    /* Make sure the more expensive operand is 'op1' */
    noway_assert((cond->gtFlags & GTF_REVERSE_OPS) == 0);

    if (cond->gtFlags & GTF_REVERSE_OPS) // TODO: note that this is now dead code, since the above is a noway_assert()
    {
        /* Don't forget to modify the condition as well */

        cond->gtOp.gtOp1 = op2;
        cond->gtOp.gtOp2 = op1;
        cond->SetOper(GenTree::SwapRelop(cmp));
        cond->gtFlags &= ~GTF_REVERSE_OPS;

        /* Get hold of the new values */

        cmp = cond->OperGet();
        op1 = cond->gtOp.gtOp1;
        op2 = cond->gtOp.gtOp2;
    }

    /* What is the type of the operand? */

    switch (genActualType(op1->gtType))
    {
        case TYP_INT:
        case TYP_REF:
        case TYP_BYREF:
            emitJumpKind jumpKind;

            // Check if we can use the currently set flags. Else set them

            jumpKind = genCondSetFlags(cond);

#if FEATURE_STACK_FP_X87
            if (bStackFPFixup)
            {
                genCondJmpInsStackFP(jumpKind, jumpTrue, jumpFalse);
            }
            else
#endif
            {
                /* Generate the conditional jump */
                inst_JMP(jumpKind, jumpTrue);
            }

            return;

        case TYP_LONG:
#if FEATURE_STACK_FP_X87
            if (bStackFPFixup)
            {
                genCondJumpLngStackFP(cond, jumpTrue, jumpFalse);
            }
            else
#endif
            {
                genCondJumpLng(cond, jumpTrue, jumpFalse);
            }
            return;

        case TYP_FLOAT:
        case TYP_DOUBLE:
#if FEATURE_STACK_FP_X87
            genCondJumpFltStackFP(cond, jumpTrue, jumpFalse, bStackFPFixup);
#else
            genCondJumpFloat(cond, jumpTrue, jumpFalse);
#endif
            return;

        default:
#ifdef DEBUG
            compiler->gtDispTree(cond);
#endif
            unreached(); // unexpected/unsupported 'jtrue' operands type
    }
}

/*****************************************************************************
 *  Spill registers to check callers can handle it.
 */

#ifdef DEBUG

void CodeGen::genStressRegs(GenTreePtr tree)
{
    if (regSet.rsStressRegs() < 2)
        return;

    /* Spill as many registers as possible. Callers should be prepared
       to handle this case.
       But don't spill trees with no size (TYP_STRUCT comes to mind) */

    {
        regMaskTP spillRegs = regSet.rsRegMaskCanGrab() & regSet.rsMaskUsed;
        regNumber regNum;
        regMaskTP regBit;

        for (regNum = REG_FIRST, regBit = 1; regNum < REG_COUNT; regNum = REG_NEXT(regNum), regBit <<= 1)
        {
            if ((spillRegs & regBit) && (regSet.rsUsedTree[regNum] != NULL) &&
                (genTypeSize(regSet.rsUsedTree[regNum]->TypeGet()) > 0))
            {
                regSet.rsSpillReg(regNum);

                spillRegs &= regSet.rsMaskUsed;

                if (!spillRegs)
                    break;
            }
        }
    }

    regMaskTP trashRegs = regSet.rsRegMaskFree();

    if (trashRegs == RBM_NONE)
        return;

    /* It is sometimes reasonable to expect that calling genCodeForTree()
       on certain trees won't spill anything */

    if ((compiler->compCurStmt == compiler->compCurBB->bbTreeList) && (compiler->compCurBB->bbCatchTyp) &&
        handlerGetsXcptnObj(compiler->compCurBB->bbCatchTyp))
    {
        trashRegs &= ~(RBM_EXCEPTION_OBJECT);
    }

    // If genCodeForTree() effectively gets called a second time on the same tree

    if (tree->InReg())
    {
        noway_assert(varTypeIsIntegralOrI(tree->TypeGet()));
        trashRegs &= ~genRegMask(tree->gtRegNum);
    }

    if (tree->gtType == TYP_INT && tree->OperIsSimple())
    {
        GenTreePtr op1 = tree->gtOp.gtOp1;
        GenTreePtr op2 = tree->gtOp.gtOp2;
        if (op1 && (op1->InReg()))
            trashRegs &= ~genRegMask(op1->gtRegNum);
        if (op2 && (op2->InReg()))
            trashRegs &= ~genRegMask(op2->gtRegNum);
    }

    if (compiler->compCurBB == compiler->genReturnBB)
    {
        if (compiler->info.compCallUnmanaged)
        {
            LclVarDsc* varDsc = &compiler->lvaTable[compiler->info.compLvFrameListRoot];
            if (varDsc->lvRegister)
                trashRegs &= ~genRegMask(varDsc->lvRegNum);
        }
    }

    /* Now trash the registers. We use regSet.rsModifiedRegsMask, else we will have
       to save/restore the register. We try to be as unintrusive
       as possible */

    noway_assert((REG_INT_LAST - REG_INT_FIRST) == 7);
    // This is obviously false for ARM, but this function is never called.
    for (regNumber reg = REG_INT_FIRST; reg <= REG_INT_LAST; reg = REG_NEXT(reg))
    {
        regMaskTP regMask = genRegMask(reg);

        if (regSet.rsRegsModified(regMask & trashRegs))
            genSetRegToIcon(reg, 0);
    }
}

#endif // DEBUG

/*****************************************************************************
 *
 *  Generate code for a GTK_CONST tree
 */

void CodeGen::genCodeForTreeConst(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    noway_assert(tree->IsCnsIntOrI());

    ssize_t   ival    = tree->gtIntConCommon.IconValue();
    regMaskTP needReg = destReg;
    regNumber reg;
    bool      needReloc = compiler->opts.compReloc && tree->IsIconHandle();

#if REDUNDANT_LOAD

    /* If we are targeting destReg and ival is zero           */
    /* we would rather xor needReg than copy another register */

    if (!needReloc)
    {
        bool reuseConstantInReg = false;

        if (destReg == RBM_NONE)
            reuseConstantInReg = true;

#ifdef _TARGET_ARM_
        // If we can set a register to a constant with a small encoding, then do that.
        // Assume we'll get a low register if needReg has low registers as options.
        if (!reuseConstantInReg &&
            !arm_Valid_Imm_For_Small_Mov((needReg & RBM_LOW_REGS) ? REG_R0 : REG_R8, ival, INS_FLAGS_DONT_CARE))
        {
            reuseConstantInReg = true;
        }
#else
        if (!reuseConstantInReg && ival != 0)
            reuseConstantInReg = true;
#endif

        if (reuseConstantInReg)
        {
            /* Is the constant already in register? If so, use this register */

            reg = regTracker.rsIconIsInReg(ival);
            if (reg != REG_NA)
                goto REG_LOADED;
        }
    }

#endif // REDUNDANT_LOAD

    reg = regSet.rsPickReg(needReg, bestReg);

    /* If the constant is a handle, we need a reloc to be applied to it */

    if (needReloc)
    {
        instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, reg, ival);
        regTracker.rsTrackRegTrash(reg);
    }
    else
    {
        genSetRegToIcon(reg, ival, tree->TypeGet());
    }

REG_LOADED:

#ifdef DEBUG
    /* Special case: GT_CNS_INT - Restore the current live set if it was changed */

    if (!genTempLiveChg)
    {
        VarSetOps::Assign(compiler, compiler->compCurLife, genTempOldLife);
        genTempLiveChg = true;
    }
#endif

    gcInfo.gcMarkRegPtrVal(reg, tree->TypeGet()); // In case the handle is a GC object (for eg, frozen strings)
    genCodeForTree_DONE(tree, reg);
}

/*****************************************************************************
 *
 *  Generate code for a GTK_LEAF tree
 */

void CodeGen::genCodeForTreeLeaf(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    genTreeOps oper    = tree->OperGet();
    regNumber  reg     = DUMMY_INIT(REG_CORRUPT);
    regMaskTP  regs    = regSet.rsMaskUsed;
    regMaskTP  needReg = destReg;
    size_t     size;

    noway_assert(tree->OperKind() & GTK_LEAF);

    switch (oper)
    {
        case GT_REG_VAR:
            NO_WAY("GT_REG_VAR should have been caught above");
            break;

        case GT_LCL_VAR:

            /* Does the variable live in a register? */

            if (genMarkLclVar(tree))
            {
                genCodeForTree_REG_VAR1(tree);
                return;
            }

#if REDUNDANT_LOAD

            /* Is the local variable already in register? */

            reg = findStkLclInReg(tree->gtLclVarCommon.gtLclNum);

            if (reg != REG_NA)
            {
                /* Use the register the variable happens to be in */
                regMaskTP regMask = genRegMask(reg);

                // If the register that it was in isn't one of the needRegs
                // then try to move it into a needReg register

                if (((regMask & needReg) == 0) && (regSet.rsRegMaskCanGrab() & needReg))
                {
                    regNumber rg2 = reg;
                    reg           = regSet.rsPickReg(needReg, bestReg);
                    if (reg != rg2)
                    {
                        regMask = genRegMask(reg);
                        inst_RV_RV(INS_mov, reg, rg2, tree->TypeGet());
                    }
                }

                gcInfo.gcMarkRegPtrVal(reg, tree->TypeGet());
                regTracker.rsTrackRegLclVar(reg, tree->gtLclVarCommon.gtLclNum);
                break;
            }

#endif
            goto MEM_LEAF;

        case GT_LCL_FLD:

            // We only use GT_LCL_FLD for lvDoNotEnregister vars, so we don't have
            // to worry about it being enregistered.
            noway_assert(compiler->lvaTable[tree->gtLclFld.gtLclNum].lvRegister == 0);
            goto MEM_LEAF;

        case GT_CLS_VAR:

        MEM_LEAF:

            /* Pick a register for the value */

            reg = regSet.rsPickReg(needReg, bestReg);

            /* Load the variable into the register */

            size = genTypeSize(tree->gtType);

            if (size < EA_4BYTE)
            {
                instruction ins = ins_Move_Extend(tree->TypeGet(), tree->InReg());
                inst_RV_TT(ins, reg, tree, 0);

                /* We've now "promoted" the tree-node to TYP_INT */

                tree->gtType = TYP_INT;
            }
            else
            {
                inst_RV_TT(INS_mov, reg, tree, 0);
            }

            regTracker.rsTrackRegTrash(reg);

            gcInfo.gcMarkRegPtrVal(reg, tree->TypeGet());

            switch (oper)
            {
                case GT_CLS_VAR:
                    regTracker.rsTrackRegClsVar(reg, tree);
                    break;
                case GT_LCL_VAR:
                    regTracker.rsTrackRegLclVar(reg, tree->gtLclVarCommon.gtLclNum);
                    break;
                case GT_LCL_FLD:
                    break;
                default:
                    noway_assert(!"Unexpected oper");
            }

#ifdef _TARGET_ARM_
            if (tree->gtFlags & GTF_IND_VOLATILE)
            {
                // Emit a memory barrier instruction after the load
                instGen_MemoryBarrier();
            }
#endif

            break;

        case GT_NO_OP:
            instGen(INS_nop);
            reg = REG_STK;
            break;

#if !FEATURE_EH_FUNCLETS
        case GT_END_LFIN:

            /* Have to clear the shadowSP of the nesting level which
               encloses the finally */

            unsigned finallyNesting;
            finallyNesting = (unsigned)tree->gtVal.gtVal1;
            noway_assert(tree->gtVal.gtVal1 <
                         compiler->compHndBBtabCount); // assert we didn't truncate with the cast above.
            noway_assert(finallyNesting < compiler->compHndBBtabCount);

            // The last slot is reserved for ICodeManager::FixContext(ppEndRegion)
            unsigned filterEndOffsetSlotOffs;
            PREFIX_ASSUME(compiler->lvaLclSize(compiler->lvaShadowSPslotsVar) >
                          sizeof(void*)); // below doesn't underflow.
            filterEndOffsetSlotOffs = (unsigned)(compiler->lvaLclSize(compiler->lvaShadowSPslotsVar) - (sizeof(void*)));

            unsigned curNestingSlotOffs;
            curNestingSlotOffs = filterEndOffsetSlotOffs - ((finallyNesting + 1) * sizeof(void*));
            instGen_Store_Imm_Into_Lcl(TYP_I_IMPL, EA_PTRSIZE, 0, compiler->lvaShadowSPslotsVar, curNestingSlotOffs);
            reg = REG_STK;
            break;
#endif // !FEATURE_EH_FUNCLETS

        case GT_CATCH_ARG:

            noway_assert(compiler->compCurBB->bbCatchTyp && handlerGetsXcptnObj(compiler->compCurBB->bbCatchTyp));

            /* Catch arguments get passed in a register. genCodeForBBlist()
               would have marked it as holding a GC object, but not used. */

            noway_assert(gcInfo.gcRegGCrefSetCur & RBM_EXCEPTION_OBJECT);
            reg = REG_EXCEPTION_OBJECT;
            break;

        case GT_JMP:
            genCodeForTreeLeaf_GT_JMP(tree);
            return;

        case GT_MEMORYBARRIER:
            // Emit the memory barrier instruction
            instGen_MemoryBarrier();
            reg = REG_STK;
            break;

        default:
#ifdef DEBUG
            compiler->gtDispTree(tree);
#endif
            noway_assert(!"unexpected leaf");
    }

    noway_assert(reg != DUMMY_INIT(REG_CORRUPT));
    genCodeForTree_DONE(tree, reg);
}

GenTreePtr CodeGen::genCodeForCommaTree(GenTreePtr tree)
{
    while (tree->OperGet() == GT_COMMA)
    {
        GenTreePtr op1 = tree->gtOp.gtOp1;
        genCodeForTree(op1, RBM_NONE);
        gcInfo.gcMarkRegPtrVal(op1);

        tree = tree->gtOp.gtOp2;
    }
    return tree;
}

/*****************************************************************************
 *
 *  Generate code for the a leaf node of type GT_JMP
 */

void CodeGen::genCodeForTreeLeaf_GT_JMP(GenTreePtr tree)
{
    noway_assert(compiler->compCurBB->bbFlags & BBF_HAS_JMP);

#ifdef PROFILING_SUPPORTED
    if (compiler->compIsProfilerHookNeeded())
    {
        /* fire the event at the call site */
        unsigned saveStackLvl2 = genStackLevel;

        compiler->info.compProfilerCallback = true;

#ifdef _TARGET_X86_
        //
        // Push the profilerHandle
        //
        regMaskTP byrefPushedRegs;
        regMaskTP norefPushedRegs;
        regMaskTP pushedArgRegs =
            genPushRegs(RBM_ARG_REGS & (regSet.rsMaskUsed | regSet.rsMaskVars | regSet.rsMaskLock), &byrefPushedRegs,
                        &norefPushedRegs);

        if (compiler->compProfilerMethHndIndirected)
        {
            getEmitter()->emitIns_AR_R(INS_push, EA_PTR_DSP_RELOC, REG_NA, REG_NA,
                                       (ssize_t)compiler->compProfilerMethHnd);
        }
        else
        {
            inst_IV(INS_push, (size_t)compiler->compProfilerMethHnd);
        }
        genSinglePush();

        genEmitHelperCall(CORINFO_HELP_PROF_FCN_TAILCALL,
                          sizeof(int) * 1, // argSize
                          EA_UNKNOWN);     // retSize

        //
        // Adjust the number of stack slots used by this managed method if necessary.
        //
        if (compiler->fgPtrArgCntMax < 1)
        {
            JITDUMP("Upping fgPtrArgCntMax from %d to 1\n", compiler->fgPtrArgCntMax);
            compiler->fgPtrArgCntMax = 1;
        }

        genPopRegs(pushedArgRegs, byrefPushedRegs, norefPushedRegs);
#elif _TARGET_ARM_
        // For GT_JMP nodes we have added r0 as a used register, when under arm profiler, to evaluate GT_JMP node.
        // To emit tailcall callback we need r0 to pass profiler handle. Any free register could be used as call target.
        regNumber argReg = regSet.rsGrabReg(RBM_PROFILER_JMP_USED);
        noway_assert(argReg == REG_PROFILER_JMP_ARG);
        regSet.rsLockReg(RBM_PROFILER_JMP_USED);

        if (compiler->compProfilerMethHndIndirected)
        {
            getEmitter()->emitIns_R_AI(INS_ldr, EA_PTR_DSP_RELOC, argReg, (ssize_t)compiler->compProfilerMethHnd);
            regTracker.rsTrackRegTrash(argReg);
        }
        else
        {
            instGen_Set_Reg_To_Imm(EA_4BYTE, argReg, (ssize_t)compiler->compProfilerMethHnd);
        }

        genEmitHelperCall(CORINFO_HELP_PROF_FCN_TAILCALL,
                          0,           // argSize
                          EA_UNKNOWN); // retSize

        regSet.rsUnlockReg(RBM_PROFILER_JMP_USED);
#else
        NYI("Pushing the profilerHandle & caller's sp for the profiler callout and locking 'arguments'");
#endif //_TARGET_X86_

        /* Restore the stack level */
        SetStackLevel(saveStackLvl2);
    }
#endif // PROFILING_SUPPORTED

    /* This code is cloned from the regular processing of GT_RETURN values.  We have to remember to
     * call genPInvokeMethodEpilog anywhere that we have a method return.  We should really
     * generate trees for the PInvoke prolog and epilog so we can remove these special cases.
     */

    if (compiler->info.compCallUnmanaged)
    {
        genPInvokeMethodEpilog();
    }

    // Make sure register arguments are in their initial registers
    // and stack arguments are put back as well.
    //
    // This does not deal with circular dependencies of register
    // arguments, which is safe because RegAlloc prevents that by
    // not enregistering any RegArgs when a JMP opcode is used.

    if (compiler->info.compArgsCount == 0)
    {
        return;
    }

    unsigned   varNum;
    LclVarDsc* varDsc;

    // First move any enregistered stack arguments back to the stack
    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->info.compArgsCount; varNum++, varDsc++)
    {
        noway_assert(varDsc->lvIsParam);
        if (varDsc->lvIsRegArg || !varDsc->lvRegister)
            continue;

        /* Argument was passed on the stack, but ended up in a register
         * Store it back to the stack */
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef _TARGET_64BIT_
        if (varDsc->TypeGet() == TYP_LONG)
        {
            /* long - at least the low half must be enregistered */

            getEmitter()->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, varDsc->lvRegNum, varNum, 0);

            /* Is the upper half also enregistered? */

            if (varDsc->lvOtherReg != REG_STK)
            {
                getEmitter()->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, varDsc->lvOtherReg, varNum, sizeof(int));
            }
        }
        else
#endif // _TARGET_64BIT_
        {
            getEmitter()->emitIns_S_R(ins_Store(varDsc->TypeGet()), emitTypeSize(varDsc->TypeGet()), varDsc->lvRegNum,
                                      varNum, 0);
        }
    }

#ifdef _TARGET_ARM_
    regMaskTP fixedArgsMask = RBM_NONE;
#endif

    // Next move any un-enregistered register arguments back to their register
    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->info.compArgsCount; varNum++, varDsc++)
    {
        /* Is this variable a register arg? */

        if (!varDsc->lvIsRegArg)
            continue;

        /* Register argument */

        noway_assert(isRegParamType(genActualType(varDsc->TypeGet())));
        noway_assert(!varDsc->lvRegister);

        /* Reload it from the stack */
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef _TARGET_64BIT_
        if (varDsc->TypeGet() == TYP_LONG)
        {
            /* long - at least the low half must be enregistered */

            getEmitter()->emitIns_R_S(ins_Load(TYP_INT), EA_4BYTE, varDsc->lvArgReg, varNum, 0);
            regTracker.rsTrackRegTrash(varDsc->lvArgReg);

            /* Also assume the upper half also enregistered */

            getEmitter()->emitIns_R_S(ins_Load(TYP_INT), EA_4BYTE, genRegArgNext(varDsc->lvArgReg), varNum,
                                      sizeof(int));
            regTracker.rsTrackRegTrash(genRegArgNext(varDsc->lvArgReg));

#ifdef _TARGET_ARM_
            fixedArgsMask |= genRegMask(varDsc->lvArgReg);
            fixedArgsMask |= genRegMask(genRegArgNext(varDsc->lvArgReg));
#endif
        }
        else
#endif // _TARGET_64BIT_
#ifdef _TARGET_ARM_
            if (varDsc->lvIsHfaRegArg())
        {
            const var_types   elemType = varDsc->GetHfaType();
            const instruction loadOp   = ins_Load(elemType);
            const emitAttr    size     = emitTypeSize(elemType);
            regNumber         argReg   = varDsc->lvArgReg;
            const unsigned    maxSize  = min(varDsc->lvSize(), (LAST_FP_ARGREG + 1 - argReg) * REGSIZE_BYTES);

            for (unsigned ofs = 0; ofs < maxSize; ofs += (unsigned)size)
            {
                getEmitter()->emitIns_R_S(loadOp, size, argReg, varNum, ofs);
                assert(genIsValidFloatReg(argReg)); // we don't use register tracking for FP
                argReg = regNextOfType(argReg, elemType);
            }
        }
        else if (varDsc->TypeGet() == TYP_STRUCT)
        {
            const var_types   elemType = TYP_INT; // we pad everything out to at least 4 bytes
            const instruction loadOp   = ins_Load(elemType);
            const emitAttr    size     = emitTypeSize(elemType);
            regNumber         argReg   = varDsc->lvArgReg;
            const unsigned    maxSize  = min(varDsc->lvSize(), (REG_ARG_LAST + 1 - argReg) * REGSIZE_BYTES);

            for (unsigned ofs = 0; ofs < maxSize; ofs += (unsigned)size)
            {
                getEmitter()->emitIns_R_S(loadOp, size, argReg, varNum, ofs);
                regTracker.rsTrackRegTrash(argReg);

                fixedArgsMask |= genRegMask(argReg);

                argReg = genRegArgNext(argReg);
            }
        }
        else
#endif //_TARGET_ARM_
        {
            var_types loadType = varDsc->TypeGet();
            regNumber argReg   = varDsc->lvArgReg; // incoming arg register
            bool      twoParts = false;

            if (compiler->info.compIsVarArgs && isFloatRegType(loadType))
            {
#ifndef _TARGET_64BIT_
                if (loadType == TYP_DOUBLE)
                    twoParts = true;
#endif
                loadType = TYP_I_IMPL;
                assert(isValidIntArgReg(argReg));
            }

            getEmitter()->emitIns_R_S(ins_Load(loadType), emitTypeSize(loadType), argReg, varNum, 0);
            regTracker.rsTrackRegTrash(argReg);

#ifdef _TARGET_ARM_
            fixedArgsMask |= genRegMask(argReg);
#endif
            if (twoParts)
            {
                argReg = genRegArgNext(argReg);
                assert(isValidIntArgReg(argReg));

                getEmitter()->emitIns_R_S(ins_Load(loadType), emitTypeSize(loadType), argReg, varNum, REGSIZE_BYTES);
                regTracker.rsTrackRegTrash(argReg);

#ifdef _TARGET_ARM_
                fixedArgsMask |= genRegMask(argReg);
#endif
            }
        }
    }

#ifdef _TARGET_ARM_
    // Check if we have any non-fixed args possibly in the arg registers.
    if (compiler->info.compIsVarArgs && (fixedArgsMask & RBM_ARG_REGS) != RBM_ARG_REGS)
    {
        noway_assert(compiler->lvaTable[compiler->lvaVarargsHandleArg].lvOnFrame);

        regNumber regDeclArgs = REG_ARG_FIRST;

        // Skip the 'this' pointer.
        if (!compiler->info.compIsStatic)
        {
            regDeclArgs = REG_NEXT(regDeclArgs);
        }

        // Skip the 'generic context.'
        if (compiler->info.compMethodInfo->args.callConv & CORINFO_CALLCONV_PARAMTYPE)
        {
            regDeclArgs = REG_NEXT(regDeclArgs);
        }

        // Skip any 'return buffer arg.'
        if (compiler->info.compRetBuffArg != BAD_VAR_NUM)
        {
            regDeclArgs = REG_NEXT(regDeclArgs);
        }

        // Skip the 'vararg cookie.'
        regDeclArgs = REG_NEXT(regDeclArgs);

        // Also add offset for the vararg cookie.
        int offset = REGSIZE_BYTES;

        // Load all the variable arguments in registers back to their registers.
        for (regNumber reg = regDeclArgs; reg <= REG_ARG_LAST; reg = REG_NEXT(reg))
        {
            if (!(fixedArgsMask & genRegMask(reg)))
            {
                getEmitter()->emitIns_R_S(ins_Load(TYP_INT), EA_4BYTE, reg, compiler->lvaVarargsHandleArg, offset);
                regTracker.rsTrackRegTrash(reg);
            }
            offset += REGSIZE_BYTES;
        }
    }
#endif // _TARGET_ARM_
}

/*****************************************************************************
 *
 *  Check if a variable is assigned to in a tree.  The variable number is
 *  passed in pCallBackData.  If the variable is assigned to, return
 *  Compiler::WALK_ABORT.  Otherwise return Compiler::WALK_CONTINUE.
 */
Compiler::fgWalkResult CodeGen::fgIsVarAssignedTo(GenTreePtr* pTree, Compiler::fgWalkData* data)
{
    GenTreePtr tree = *pTree;
    if ((tree->OperIsAssignment()) && (tree->gtOp.gtOp1->OperGet() == GT_LCL_VAR) &&
        (tree->gtOp.gtOp1->gtLclVarCommon.gtLclNum == (unsigned)(size_t)data->pCallbackData))
    {
        return Compiler::WALK_ABORT;
    }

    return Compiler::WALK_CONTINUE;
}

regNumber CodeGen::genIsEnregisteredIntVariable(GenTreePtr tree)
{
    unsigned   varNum;
    LclVarDsc* varDsc;

    if (tree->gtOper == GT_LCL_VAR)
    {
        /* Does the variable live in a register? */

        varNum = tree->gtLclVarCommon.gtLclNum;
        noway_assert(varNum < compiler->lvaCount);
        varDsc = compiler->lvaTable + varNum;

        if (!varDsc->IsFloatRegType() && varDsc->lvRegister)
        {
            return varDsc->lvRegNum;
        }
    }

    return REG_NA;
}

// inline
void CodeGen::unspillLiveness(genLivenessSet* ls)
{
    // Only try to unspill the registers that are missing from the currentLiveRegs
    //
    regMaskTP cannotSpillMask = ls->maskVars | ls->gcRefRegs | ls->byRefRegs;
    regMaskTP currentLiveRegs = regSet.rsMaskVars | gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur;
    cannotSpillMask &= ~currentLiveRegs;

    // Typically this will always be true and we will return
    //
    if (cannotSpillMask == 0)
        return;

    for (regNumber reg = REG_INT_FIRST; reg <= REG_INT_LAST; reg = REG_NEXT(reg))
    {
        // Is this a register that we cannot leave in the spilled state?
        //
        if ((cannotSpillMask & genRegMask(reg)) == 0)
            continue;

        RegSet::SpillDsc* spill = regSet.rsSpillDesc[reg];

        // Was it spilled, if not then skip it.
        //
        if (!spill)
            continue;

        noway_assert(spill->spillTree->gtFlags & GTF_SPILLED);

        regSet.rsUnspillReg(spill->spillTree, genRegMask(reg), RegSet::KEEP_REG);
    }
}

/*****************************************************************************
 *
 *  Generate code for a qmark colon
 */

void CodeGen::genCodeForQmark(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    GenTreePtr op1 = tree->gtOp.gtOp1;
    GenTreePtr op2 = tree->gtOp.gtOp2;
    regNumber  reg;
    regMaskTP  regs    = regSet.rsMaskUsed;
    regMaskTP  needReg = destReg;

    noway_assert(compiler->compQmarkUsed);
    noway_assert(tree->gtOper == GT_QMARK);
    noway_assert(op1->OperIsCompare());
    noway_assert(op2->gtOper == GT_COLON);

    GenTreePtr thenNode = op2->AsColon()->ThenNode();
    GenTreePtr elseNode = op2->AsColon()->ElseNode();

    /* If elseNode is a Nop node you must reverse the
       thenNode and elseNode prior to reaching here!
       (If both 'else' and 'then' are Nops, whole qmark will have been optimized away.) */

    noway_assert(!elseNode->IsNothingNode());

    /* Try to implement the qmark colon using a CMOV.  If we can't for
       whatever reason, this will return false and we will implement
       it using regular branching constructs. */

    if (genCodeForQmarkWithCMOV(tree, destReg, bestReg))
        return;

    /*
        This is a ?: operator; generate code like this:

            condition_compare
            jmp_if_true lab_true

        lab_false:
            op1 (false = 'else' part)
            jmp lab_done

        lab_true:
            op2 (true = 'then' part)

        lab_done:


        NOTE: If no 'then' part we do not generate the 'jmp lab_done'
            or the 'lab_done' label
    */

    BasicBlock* lab_true;
    BasicBlock* lab_false;
    BasicBlock* lab_done;

    genLivenessSet entryLiveness;
    genLivenessSet exitLiveness;

    lab_true  = genCreateTempLabel();
    lab_false = genCreateTempLabel();

#if FEATURE_STACK_FP_X87
    /* Spill any register that hold partial values so that the exit liveness
       from sides is the same */
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    regMaskTP spillMask = regSet.rsMaskUsedFloat | regSet.rsMaskLockedFloat | regSet.rsMaskRegVarFloat;

    // spillMask should be the whole FP stack
    noway_assert(compCurFPState.m_uStackSize == genCountBits(spillMask));
#endif

    SpillTempsStackFP(regSet.rsMaskUsedFloat);
    noway_assert(regSet.rsMaskUsedFloat == 0);
#endif

    /* Before we generate code for qmark, we spill all the currently used registers
       that conflict with the registers used in the qmark tree. This is to avoid
       introducing spills that only occur on either the 'then' or 'else' side of
       the tree, but not both identically. We need to be careful with enregistered
       variables that are used; see below.
    */

    if (regSet.rsMaskUsed)
    {
        /* If regSet.rsMaskUsed overlaps with regSet.rsMaskVars (multi-use of the enregistered
           variable), then it may not get spilled. However, the variable may
           then go dead within thenNode/elseNode, at which point regSet.rsMaskUsed
           may get spilled from one side and not the other. So unmark regSet.rsMaskVars
           before spilling regSet.rsMaskUsed */

        regMaskTP rsAdditionalCandidates = regSet.rsMaskUsed & regSet.rsMaskVars;
        regMaskTP rsAdditional           = RBM_NONE;

        // For each multi-use of an enregistered variable, we need to determine if
        // it can get spilled inside the qmark colon.  This can only happen if
        // its life ends somewhere in the qmark colon.  We have the following
        // cases:
        // 1) Variable is dead at the end of the colon -- needs to be spilled
        // 2) Variable is alive at the end of the colon -- needs to be spilled
        //    iff it is assigned to in the colon.  In order to determine that, we
        //    examine the GTF_ASG flag to see if any assignments were made in the
        //    colon.  If there are any, we need to do a tree walk to see if this
        //    variable is the target of an assignment.  This treewalk should not
        //    happen frequently.
        if (rsAdditionalCandidates)
        {
#ifdef DEBUG
            if (compiler->verbose)
            {
                Compiler::printTreeID(tree);
                printf(": Qmark-Colon additional spilling candidates are ");
                dspRegMask(rsAdditionalCandidates);
                printf("\n");
            }
#endif

            // If any candidates are not alive at the GT_QMARK node, then they
            // need to be spilled

            const VARSET_TP& rsLiveNow(compiler->compCurLife);
            VARSET_TP rsLiveAfter(compiler->fgUpdateLiveSet(compiler->compCurLife, compiler->compCurLifeTree, tree));

            VARSET_TP regVarLiveNow(VarSetOps::Intersection(compiler, compiler->raRegVarsMask, rsLiveNow));

            VARSET_ITER_INIT(compiler, iter, regVarLiveNow, varIndex);
            while (iter.NextElem(&varIndex))
            {
                // Find the variable in compiler->lvaTable
                unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
                LclVarDsc* varDsc = compiler->lvaTable + varNum;

#if !FEATURE_FP_REGALLOC
                if (varDsc->IsFloatRegType())
                    continue;
#endif

                noway_assert(varDsc->lvRegister);

                regMaskTP regBit;

                if (varTypeIsFloating(varDsc->TypeGet()))
                {
                    regBit = genRegMaskFloat(varDsc->lvRegNum, varDsc->TypeGet());
                }
                else
                {
                    regBit = genRegMask(varDsc->lvRegNum);

                    // For longs we may need to spill both regs
                    if (isRegPairType(varDsc->lvType) && varDsc->lvOtherReg != REG_STK)
                        regBit |= genRegMask(varDsc->lvOtherReg);
                }

                // Is it one of our reg-use vars?  If not, we don't need to spill it.
                regBit &= rsAdditionalCandidates;
                if (!regBit)
                    continue;

                // Is the variable live at the end of the colon?
                if (VarSetOps::IsMember(compiler, rsLiveAfter, varIndex))
                {
                    // Variable is alive at the end of the colon.  Was it assigned
                    // to inside the colon?

                    if (!(op2->gtFlags & GTF_ASG))
                        continue;

                    if (compiler->fgWalkTreePre(&op2, CodeGen::fgIsVarAssignedTo, (void*)(size_t)varNum) ==
                        Compiler::WALK_ABORT)
                    {
                        // Variable was assigned to, so we need to spill it.

                        rsAdditional |= regBit;
#ifdef DEBUG
                        if (compiler->verbose)
                        {
                            Compiler::printTreeID(tree);
                            printf(": Qmark-Colon candidate ");
                            dspRegMask(regBit);
                            printf("\n");
                            printf("    is assigned to inside colon and will be spilled\n");
                        }
#endif
                    }
                }
                else
                {
                    // Variable is not alive at the end of the colon.  We need to spill it.

                    rsAdditional |= regBit;
#ifdef DEBUG
                    if (compiler->verbose)
                    {
                        Compiler::printTreeID(tree);
                        printf(": Qmark-Colon candidate ");
                        dspRegMask(regBit);
                        printf("\n");
                        printf("    is alive at end of colon and will be spilled\n");
                    }
#endif
                }
            }

#ifdef DEBUG
            if (compiler->verbose)
            {
                Compiler::printTreeID(tree);
                printf(": Qmark-Colon approved additional spilling candidates are ");
                dspRegMask(rsAdditional);
                printf("\n");
            }
#endif
        }

        noway_assert((rsAdditionalCandidates | rsAdditional) == rsAdditionalCandidates);

        // We only need to spill registers that are modified by the qmark tree, as specified in tree->gtUsedRegs.
        // If we ever need to use and spill a register while generating code that is not in tree->gtUsedRegs,
        // we will have unbalanced spills and generate bad code.
        regMaskTP rsSpill =
            ((regSet.rsMaskUsed & ~(regSet.rsMaskVars | regSet.rsMaskResvd)) | rsAdditional) & tree->gtUsedRegs;

#ifdef DEBUG
        // Under register stress, regSet.rsPickReg() ignores the recommended registers and always picks
        // 'bad' registers, causing spills. So, just force all used registers to get spilled
        // in the stress case, to avoid the problem we're trying to resolve here. Thus, any spills
        // that occur within the qmark condition, 'then' case, or 'else' case, will have to be
        // unspilled while generating that same tree.

        if (regSet.rsStressRegs() >= 1)
        {
            rsSpill |= regSet.rsMaskUsed & ~(regSet.rsMaskVars | regSet.rsMaskLock | regSet.rsMaskResvd);
        }
#endif // DEBUG

        if (rsSpill)
        {
            // Remember which registers hold pointers. We will spill
            // them, but the code that follows will fetch reg vars from
            // the registers, so we need that gc compiler->info.
            regMaskTP gcRegSavedByref = gcInfo.gcRegByrefSetCur & rsAdditional;
            regMaskTP gcRegSavedGCRef = gcInfo.gcRegGCrefSetCur & rsAdditional;

            // regSet.rsSpillRegs() will assert if we try to spill any enregistered variables.
            // So, pretend there aren't any, and spill them anyway. This will only occur
            // if rsAdditional is non-empty.
            regMaskTP rsTemp = regSet.rsMaskVars;
            regSet.ClearMaskVars();

            regSet.rsSpillRegs(rsSpill);

            // Restore gc tracking masks.
            gcInfo.gcRegByrefSetCur |= gcRegSavedByref;
            gcInfo.gcRegGCrefSetCur |= gcRegSavedGCRef;

            // Set regSet.rsMaskVars back to normal
            regSet.rsMaskVars = rsTemp;
        }
    }

    // Generate the conditional jump but without doing any StackFP fixups.
    genCondJump(op1, lab_true, lab_false, false);

    /* Save the current liveness, register status, and GC pointers */
    /* This is the liveness information upon entry                 */
    /* to both the then and else parts of the qmark                */

    saveLiveness(&entryLiveness);

    /* Clear the liveness of any local variables that are dead upon   */
    /* entry to the else part.                                        */

    /* Subtract the liveSet upon entry of the then part (op1->gtNext) */
    /* from the "colon or op2" liveSet                                */
    genDyingVars(compiler->compCurLife, tree->gtQmark.gtElseLiveSet);

    /* genCondJump() closes the current emitter block */

    genDefineTempLabel(lab_false);

#if FEATURE_STACK_FP_X87
    // Store fpstate

    QmarkStateStackFP tempFPState;
    bool              bHasFPUState = !compCurFPState.IsEmpty();
    genQMarkBeforeElseStackFP(&tempFPState, tree->gtQmark.gtElseLiveSet, op1->gtNext);
#endif

    /* Does the operator yield a value? */

    if (tree->gtType == TYP_VOID)
    {
        /* Generate the code for the else part of the qmark */

        genCodeForTree(elseNode, needReg, bestReg);

        /* The type is VOID, so we shouldn't have computed a value */

        noway_assert(!(elseNode->InReg()));

        /* Save the current liveness, register status, and GC pointers               */
        /* This is the liveness information upon exit of the then part of the qmark  */

        saveLiveness(&exitLiveness);

        /* Is there a 'then' part? */

        if (thenNode->IsNothingNode())
        {
#if FEATURE_STACK_FP_X87
            if (bHasFPUState)
            {
                // We had FP state on entry just after the condition, so potentially, the else
                // node may have to do transition work.
                lab_done = genCreateTempLabel();

                /* Generate jmp lab_done */

                inst_JMP(EJ_jmp, lab_done);

                /* No 'then' - just generate the 'lab_true' label */

                genDefineTempLabel(lab_true);

                // We need to do this after defining the lab_false label
                genQMarkAfterElseBlockStackFP(&tempFPState, compiler->compCurLife, op2->gtNext);
                genQMarkAfterThenBlockStackFP(&tempFPState);
                genDefineTempLabel(lab_done);
            }
            else
#endif // FEATURE_STACK_FP_X87
            {
                /* No 'then' - just generate the 'lab_true' label */
                genDefineTempLabel(lab_true);
            }
        }
        else
        {
            lab_done = genCreateTempLabel();

            /* Generate jmp lab_done */

            inst_JMP(EJ_jmp, lab_done);

            /* Restore the liveness that we had upon entry of the then part of the qmark */

            restoreLiveness(&entryLiveness);

            /* Clear the liveness of any local variables that are dead upon    */
            /* entry to the then part.                                         */
            genDyingVars(compiler->compCurLife, tree->gtQmark.gtThenLiveSet);

            /* Generate lab_true: */

            genDefineTempLabel(lab_true);
#if FEATURE_STACK_FP_X87
            // We need to do this after defining the lab_false label
            genQMarkAfterElseBlockStackFP(&tempFPState, compiler->compCurLife, op2->gtNext);
#endif
            /* Enter the then part - trash all registers */

            regTracker.rsTrackRegClr();

            /* Generate the code for the then part of the qmark */

            genCodeForTree(thenNode, needReg, bestReg);

            /* The type is VOID, so we shouldn't have computed a value */

            noway_assert(!(thenNode->InReg()));

            unspillLiveness(&exitLiveness);

            /* Verify that the exit liveness information is the same for the two parts of the qmark */

            checkLiveness(&exitLiveness);
#if FEATURE_STACK_FP_X87
            genQMarkAfterThenBlockStackFP(&tempFPState);
#endif
            /* Define the "result" label */

            genDefineTempLabel(lab_done);
        }

        /* Join of the two branches - trash all registers */

        regTracker.rsTrackRegClr();

        /* We're just about done */

        genUpdateLife(tree);
    }
    else
    {
        /* Generate code for a qmark that generates a value */

        /* Generate the code for the else part of the qmark */

        noway_assert(elseNode->IsNothingNode() == false);

        /* Compute the elseNode into any free register */
        genComputeReg(elseNode, needReg, RegSet::ANY_REG, RegSet::FREE_REG, true);
        noway_assert(elseNode->InReg());
        noway_assert(elseNode->gtRegNum != REG_NA);

        /* Record the chosen register */
        reg  = elseNode->gtRegNum;
        regs = genRegMask(reg);

        /* Save the current liveness, register status, and GC pointers               */
        /* This is the liveness information upon exit of the else part of the qmark  */

        saveLiveness(&exitLiveness);

        /* Generate jmp lab_done */
        lab_done = genCreateTempLabel();

#ifdef DEBUG
        // We will use this to assert we don't emit instructions if we decide not to
        // do the jmp
        unsigned emittedInstructions = getEmitter()->emitInsCount;
        bool     bSkippedJump        = false;
#endif
        // We would like to know here if the else node is really going to generate
        // code, as if it isn't, we're generating here a jump to the next instruction.
        // What you would really like is to be able to go back and remove the jump, but
        // we have no way of doing that right now.

        if (
#if FEATURE_STACK_FP_X87
            !bHasFPUState && // If there is no FPU state, we won't need an x87 transition
#endif
            genIsEnregisteredIntVariable(thenNode) == reg)
        {
#ifdef DEBUG
            // For the moment, fix this easy case (enregistered else node), which
            // is the one that happens all the time.

            bSkippedJump = true;
#endif
        }
        else
        {
            inst_JMP(EJ_jmp, lab_done);
        }

        /* Restore the liveness that we had upon entry of the else part of the qmark */

        restoreLiveness(&entryLiveness);

        /* Clear the liveness of any local variables that are dead upon    */
        /* entry to the then part.                                         */
        genDyingVars(compiler->compCurLife, tree->gtQmark.gtThenLiveSet);

        /* Generate lab_true: */
        genDefineTempLabel(lab_true);
#if FEATURE_STACK_FP_X87
        // Store FP state

        // We need to do this after defining the lab_true label
        genQMarkAfterElseBlockStackFP(&tempFPState, compiler->compCurLife, op2->gtNext);
#endif
        /* Enter the then part - trash all registers */

        regTracker.rsTrackRegClr();

        /* Generate the code for the then part of the qmark */

        noway_assert(thenNode->IsNothingNode() == false);

        /* This must place a value into the chosen register */
        genComputeReg(thenNode, regs, RegSet::EXACT_REG, RegSet::FREE_REG, true);

        noway_assert(thenNode->InReg());
        noway_assert(thenNode->gtRegNum == reg);

        unspillLiveness(&exitLiveness);

        /* Verify that the exit liveness information is the same for the two parts of the qmark */
        checkLiveness(&exitLiveness);
#if FEATURE_STACK_FP_X87
        genQMarkAfterThenBlockStackFP(&tempFPState);
#endif

#ifdef DEBUG
        noway_assert(bSkippedJump == false || getEmitter()->emitInsCount == emittedInstructions);
#endif

        /* Define the "result" label */
        genDefineTempLabel(lab_done);

        /* Join of the two branches - trash all registers */

        regTracker.rsTrackRegClr();

        /* Check whether this subtree has freed up any variables */

        genUpdateLife(tree);

        genMarkTreeInReg(tree, reg);
    }
}

/*****************************************************************************
 *
 *  Generate code for a qmark colon using the CMOV instruction.  It's OK
 *  to return false when we can't easily implement it using a cmov (leading
 *  genCodeForQmark to implement it using branches).
 */

bool CodeGen::genCodeForQmarkWithCMOV(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
#ifdef _TARGET_XARCH_
    GenTreePtr cond  = tree->gtOp.gtOp1;
    GenTreePtr colon = tree->gtOp.gtOp2;
    // Warning: this naming of the local vars is backwards!
    GenTreePtr thenNode = colon->gtOp.gtOp1;
    GenTreePtr elseNode = colon->gtOp.gtOp2;
    GenTreePtr alwaysNode, predicateNode;
    regNumber  reg;
    regMaskTP  needReg = destReg;

    noway_assert(tree->gtOper == GT_QMARK);
    noway_assert(cond->OperIsCompare());
    noway_assert(colon->gtOper == GT_COLON);

#ifdef DEBUG
    if (JitConfig.JitNoCMOV())
    {
        return false;
    }
#endif

    /* Can only implement CMOV on processors that support it */

    if (!compiler->opts.compUseCMOV)
    {
        return false;
    }

    /* thenNode better be a local or a constant */

    if ((thenNode->OperGet() != GT_CNS_INT) && (thenNode->OperGet() != GT_LCL_VAR))
    {
        return false;
    }

    /* elseNode better be a local or a constant or nothing */

    if ((elseNode->OperGet() != GT_CNS_INT) && (elseNode->OperGet() != GT_LCL_VAR))
    {
        return false;
    }

    /* can't handle two constants here */

    if ((thenNode->OperGet() == GT_CNS_INT) && (elseNode->OperGet() == GT_CNS_INT))
    {
        return false;
    }

    /* let's not handle comparisons of non-integer types */

    if (!varTypeIsI(cond->gtOp.gtOp1->gtType))
    {
        return false;
    }

    /* Choose nodes for predicateNode and alwaysNode.  Swap cond if necessary.
       The biggest constraint is that cmov doesn't take an integer argument.
    */

    bool reverseCond = false;
    if (elseNode->OperGet() == GT_CNS_INT)
    {
        // else node is a constant

        alwaysNode    = elseNode;
        predicateNode = thenNode;
        reverseCond   = true;
    }
    else
    {
        alwaysNode    = thenNode;
        predicateNode = elseNode;
    }

    // If the live set in alwaysNode is not the same as in tree, then
    // the variable in predicate node dies here.  This is a dangerous
    // case that we don't handle (genComputeReg could overwrite
    // the value of the variable in the predicate node).

    // This assert is just paranoid (we've already asserted it above)
    assert(predicateNode->OperGet() == GT_LCL_VAR);
    if ((predicateNode->gtFlags & GTF_VAR_DEATH) != 0)
    {
        return false;
    }

    // Pass this point we are comitting to use CMOV.

    if (reverseCond)
    {
        compiler->gtReverseCond(cond);
    }

    emitJumpKind jumpKind = genCondSetFlags(cond);

    // Compute the always node into any free register.  If it's a constant,
    // we need to generate the mov instruction here (otherwise genComputeReg might
    // modify the flags, as in xor reg,reg).

    if (alwaysNode->OperGet() == GT_CNS_INT)
    {
        reg = regSet.rsPickReg(needReg, bestReg);
        inst_RV_IV(INS_mov, reg, alwaysNode->gtIntCon.gtIconVal, emitActualTypeSize(alwaysNode->TypeGet()));
        gcInfo.gcMarkRegPtrVal(reg, alwaysNode->TypeGet());
        regTracker.rsTrackRegTrash(reg);
    }
    else
    {
        genComputeReg(alwaysNode, needReg, RegSet::ANY_REG, RegSet::FREE_REG, true);
        noway_assert(alwaysNode->InReg());
        noway_assert(alwaysNode->gtRegNum != REG_NA);

        // Record the chosen register

        reg = alwaysNode->gtRegNum;
    }

    regNumber regPredicate = REG_NA;

    // Is predicateNode an enregistered variable?

    if (genMarkLclVar(predicateNode))
    {
        // Variable lives in a register

        regPredicate = predicateNode->gtRegNum;
    }
#if REDUNDANT_LOAD
    else
    {
        // Checks if the variable happens to be in any of the registers

        regPredicate = findStkLclInReg(predicateNode->gtLclVarCommon.gtLclNum);
    }
#endif

    const static instruction EJtoCMOV[] = {INS_nop,    INS_nop,    INS_cmovo,  INS_cmovno, INS_cmovb,  INS_cmovae,
                                           INS_cmove,  INS_cmovne, INS_cmovbe, INS_cmova,  INS_cmovs,  INS_cmovns,
                                           INS_cmovpe, INS_cmovpo, INS_cmovl,  INS_cmovge, INS_cmovle, INS_cmovg};

    noway_assert((unsigned)jumpKind < (sizeof(EJtoCMOV) / sizeof(EJtoCMOV[0])));
    instruction cmov_ins = EJtoCMOV[jumpKind];

    noway_assert(insIsCMOV(cmov_ins));

    if (regPredicate != REG_NA)
    {
        // regPredicate is in a register

        inst_RV_RV(cmov_ins, reg, regPredicate, predicateNode->TypeGet());
    }
    else
    {
        // regPredicate is in memory

        inst_RV_TT(cmov_ins, reg, predicateNode, NULL);
    }
    gcInfo.gcMarkRegPtrVal(reg, predicateNode->TypeGet());
    regTracker.rsTrackRegTrash(reg);

    genUpdateLife(alwaysNode);
    genUpdateLife(predicateNode);
    genCodeForTree_DONE_LIFE(tree, reg);
    return true;
#else
    return false;
#endif
}

#ifdef _TARGET_XARCH_
void CodeGen::genCodeForMultEAX(GenTreePtr tree)
{
    GenTreePtr op1  = tree->gtOp.gtOp1;
    GenTreePtr op2  = tree->gtGetOp2();
    bool       ovfl = tree->gtOverflow();
    regNumber  reg  = DUMMY_INIT(REG_CORRUPT);
    regMaskTP  addrReg;

    noway_assert(tree->OperGet() == GT_MUL);

    /* We'll evaluate 'op1' first */

    regMaskTP op1Mask = regSet.rsMustExclude(RBM_EAX, op2->gtRsvdRegs);

    /* Generate the op1 into op1Mask and hold on to it. freeOnly=true */

    genComputeReg(op1, op1Mask, RegSet::ANY_REG, RegSet::KEEP_REG, true);
    noway_assert(op1->InReg());

    // If op2 is a constant we need to load  the constant into a register
    if (op2->OperKind() & GTK_CONST)
    {
        genCodeForTree(op2, RBM_EDX); // since EDX is going to be spilled anyway
        noway_assert(op2->InReg());
        regSet.rsMarkRegUsed(op2);
        addrReg = genRegMask(op2->gtRegNum);
    }
    else
    {
        /* Make the second operand addressable */
        // Try to avoid EAX.
        addrReg = genMakeRvalueAddressable(op2, RBM_ALLINT & ~RBM_EAX, RegSet::KEEP_REG, false);
    }

    /* Make sure the first operand is still in a register */
    // op1 *must* go into EAX.
    genRecoverReg(op1, RBM_EAX, RegSet::KEEP_REG);
    noway_assert(op1->InReg());

    reg = op1->gtRegNum;

    // For 8 bit operations, we need to pick byte addressable registers

    if (ovfl && varTypeIsByte(tree->TypeGet()) && !(genRegMask(reg) & RBM_BYTE_REGS))
    {
        regNumber byteReg = regSet.rsGrabReg(RBM_BYTE_REGS);

        inst_RV_RV(INS_mov, byteReg, reg);

        regTracker.rsTrackRegTrash(byteReg);
        regSet.rsMarkRegFree(genRegMask(reg));

        reg           = byteReg;
        op1->gtRegNum = reg;
        regSet.rsMarkRegUsed(op1);
    }

    /* Make sure the operand is still addressable */
    addrReg = genKeepAddressable(op2, addrReg, genRegMask(reg));

    /* Free up the operand, if it's a regvar */

    genUpdateLife(op2);

    /* The register is about to be trashed */

    regTracker.rsTrackRegTrash(reg);

    // For overflow instructions, tree->TypeGet() is the accurate type,
    // and gives us the size for the operands.

    emitAttr opSize = emitTypeSize(tree->TypeGet());

    /* Compute the new value */

    noway_assert(op1->gtRegNum == REG_EAX);

    // Make sure Edx is free (unless used by op2 itself)
    bool op2Released = false;

    if ((addrReg & RBM_EDX) == 0)
    {
        // op2 does not use Edx, so make sure noone else does either
        regSet.rsGrabReg(RBM_EDX);
    }
    else if (regSet.rsMaskMult & RBM_EDX)
    {
        /* Edx is used by op2 and some other trees.
           Spill the other trees besides op2. */

        regSet.rsGrabReg(RBM_EDX);
        op2Released = true;

        /* keepReg==RegSet::FREE_REG so that the other multi-used trees
           don't get marked as unspilled as well. */
        regSet.rsUnspillReg(op2, RBM_EDX, RegSet::FREE_REG);
    }

    instruction ins;

    if (tree->gtFlags & GTF_UNSIGNED)
        ins = INS_mulEAX;
    else
        ins = INS_imulEAX;

    inst_TT(ins, op2, 0, 0, opSize);

    /* Both EAX and EDX are now trashed */

    regTracker.rsTrackRegTrash(REG_EAX);
    regTracker.rsTrackRegTrash(REG_EDX);

    /* Free up anything that was tied up by the operand */

    if (!op2Released)
        genDoneAddressable(op2, addrReg, RegSet::KEEP_REG);

    /* The result will be where the first operand is sitting */

    /* We must use RegSet::KEEP_REG since op1 can have a GC pointer here */
    genRecoverReg(op1, 0, RegSet::KEEP_REG);

    reg = op1->gtRegNum;
    noway_assert(reg == REG_EAX);

    genReleaseReg(op1);

    /* Do we need an overflow check */

    if (ovfl)
        genCheckOverflow(tree);

    genCodeForTree_DONE(tree, reg);
}
#endif // _TARGET_XARCH_

#ifdef _TARGET_ARM_
void CodeGen::genCodeForMult64(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    GenTreePtr op1 = tree->gtOp.gtOp1;
    GenTreePtr op2 = tree->gtGetOp2();

    noway_assert(tree->OperGet() == GT_MUL);

    /* Generate the first operand into some register */

    genComputeReg(op1, RBM_ALLINT, RegSet::ANY_REG, RegSet::KEEP_REG);
    noway_assert(op1->InReg());

    /* Generate the second operand into some register */

    genComputeReg(op2, RBM_ALLINT, RegSet::ANY_REG, RegSet::KEEP_REG);
    noway_assert(op2->InReg());

    /* Make sure the first operand is still in a register */
    genRecoverReg(op1, 0, RegSet::KEEP_REG);
    noway_assert(op1->InReg());

    /* Free up the operands */
    genUpdateLife(tree);

    genReleaseReg(op1);
    genReleaseReg(op2);

    regNumber regLo = regSet.rsPickReg(destReg, bestReg);
    regNumber regHi;

    regSet.rsLockReg(genRegMask(regLo));
    regHi = regSet.rsPickReg(destReg & ~genRegMask(regLo));
    regSet.rsUnlockReg(genRegMask(regLo));

    instruction ins;
    if (tree->gtFlags & GTF_UNSIGNED)
        ins = INS_umull;
    else
        ins = INS_smull;

    getEmitter()->emitIns_R_R_R_R(ins, EA_4BYTE, regLo, regHi, op1->gtRegNum, op2->gtRegNum);
    regTracker.rsTrackRegTrash(regHi);
    regTracker.rsTrackRegTrash(regLo);

    /* Do we need an overflow check */

    if (tree->gtOverflow())
    {
        // Keep regLo [and regHi] locked while generating code for the gtOverflow() case
        //
        regSet.rsLockReg(genRegMask(regLo));

        if (tree->gtFlags & GTF_MUL_64RSLT)
            regSet.rsLockReg(genRegMask(regHi));

        regNumber regTmpHi = regHi;
        if ((tree->gtFlags & GTF_UNSIGNED) == 0)
        {
            getEmitter()->emitIns_R_I(INS_cmp, EA_4BYTE, regLo, 0x80000000);
            regTmpHi = regSet.rsPickReg(RBM_ALLINT);
            getEmitter()->emitIns_R_R_I(INS_adc, EA_4BYTE, regTmpHi, regHi, 0);
            regTracker.rsTrackRegTrash(regTmpHi);
        }
        getEmitter()->emitIns_R_I(INS_cmp, EA_4BYTE, regTmpHi, 0);

        // Jump to the block which will throw the expection
        emitJumpKind jmpNotEqual = genJumpKindForOper(GT_NE, CK_SIGNED);
        genJumpToThrowHlpBlk(jmpNotEqual, SCK_OVERFLOW);

        // Unlock regLo [and regHi] after generating code for the gtOverflow() case
        //
        regSet.rsUnlockReg(genRegMask(regLo));

        if (tree->gtFlags & GTF_MUL_64RSLT)
            regSet.rsUnlockReg(genRegMask(regHi));
    }

    genUpdateLife(tree);

    if (tree->gtFlags & GTF_MUL_64RSLT)
        genMarkTreeInRegPair(tree, gen2regs2pair(regLo, regHi));
    else
        genMarkTreeInReg(tree, regLo);
}
#endif // _TARGET_ARM_

/*****************************************************************************
 *
 *  Generate code for a simple binary arithmetic or logical operator.
 *  Handles GT_AND, GT_OR, GT_XOR, GT_ADD, GT_SUB, GT_MUL.
 */

void CodeGen::genCodeForTreeSmpBinArithLogOp(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    instruction     ins;
    genTreeOps      oper     = tree->OperGet();
    const var_types treeType = tree->TypeGet();
    GenTreePtr      op1      = tree->gtOp.gtOp1;
    GenTreePtr      op2      = tree->gtGetOp2();
    insFlags        flags    = tree->gtSetFlags() ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
    regNumber       reg      = DUMMY_INIT(REG_CORRUPT);
    regMaskTP       needReg  = destReg;

    /* Figure out what instruction to generate */

    bool isArith;
    switch (oper)
    {
        case GT_AND:
            ins     = INS_AND;
            isArith = false;
            break;
        case GT_OR:
            ins     = INS_OR;
            isArith = false;
            break;
        case GT_XOR:
            ins     = INS_XOR;
            isArith = false;
            break;
        case GT_ADD:
            ins     = INS_add;
            isArith = true;
            break;
        case GT_SUB:
            ins     = INS_sub;
            isArith = true;
            break;
        case GT_MUL:
            ins     = INS_MUL;
            isArith = true;
            break;
        default:
            unreached();
    }

#ifdef _TARGET_XARCH_
    /* Special case: try to use the 3 operand form "imul reg, op1, icon" */

    if ((oper == GT_MUL) &&
        op2->IsIntCnsFitsInI32() &&              // op2 is a constant that fits in a sign-extended 32-bit immediate
        !op1->IsCnsIntOrI() &&                   // op1 is not a constant
        (tree->gtFlags & GTF_MUL_64RSLT) == 0 && // tree not marked with MUL_64RSLT
        !varTypeIsByte(treeType) &&              // No encoding for say "imul al,al,imm"
        !tree->gtOverflow())                     // 3 operand imul doesn't set flags
    {
        /* Make the first operand addressable */

        regMaskTP addrReg = genMakeRvalueAddressable(op1, needReg & ~op2->gtRsvdRegs, RegSet::FREE_REG, false);

        /* Grab a register for the target */

        reg = regSet.rsPickReg(needReg, bestReg);

#if LEA_AVAILABLE
        /* Compute the value into the target: reg=op1*op2_icon */
        if (op2->gtIntCon.gtIconVal == 3 || op2->gtIntCon.gtIconVal == 5 || op2->gtIntCon.gtIconVal == 9)
        {
            regNumber regSrc;
            if (op1->InReg())
            {
                regSrc = op1->gtRegNum;
            }
            else
            {
                inst_RV_TT(INS_mov, reg, op1, 0, emitActualTypeSize(op1->TypeGet()));
                regSrc = reg;
            }
            getEmitter()->emitIns_R_ARX(INS_lea, emitActualTypeSize(treeType), reg, regSrc, regSrc,
                                        (op2->gtIntCon.gtIconVal & -2), 0);
        }
        else
#endif // LEA_AVAILABLE
        {
            /* Compute the value into the target: reg=op1*op2_icon */
            inst_RV_TT_IV(INS_MUL, reg, op1, (int)op2->gtIntCon.gtIconVal);
        }

        /* The register has been trashed now */

        regTracker.rsTrackRegTrash(reg);

        /* The address is no longer live */

        genDoneAddressable(op1, addrReg, RegSet::FREE_REG);

        genCodeForTree_DONE(tree, reg);
        return;
    }
#endif // _TARGET_XARCH_

    bool ovfl = false;

    if (isArith)
    {
        // We only reach here for GT_ADD, GT_SUB and GT_MUL.
        assert((oper == GT_ADD) || (oper == GT_SUB) || (oper == GT_MUL));

        ovfl = tree->gtOverflow();

        /* We record the accurate (small) types in trees only we need to
         * check for overflow. Otherwise we record genActualType()
         */

        noway_assert(ovfl || (treeType == genActualType(treeType)));

#if LEA_AVAILABLE

        /* Can we use an 'lea' to compute the result?
           Can't use 'lea' for overflow as it doesn't set flags
           Can't use 'lea' unless we have at least two free registers */
        {
            bool bEnoughRegs = genRegCountForLiveIntEnregVars(tree) + // Live intreg variables
                                   genCountBits(regSet.rsMaskLock) +  // Locked registers
                                   2                                  // We will need two regisers
                               <= genCountBits(RBM_ALLINT & ~(doubleAlignOrFramePointerUsed() ? RBM_FPBASE : 0));

            regMaskTP regs = RBM_NONE; // OUT argument
            if (!ovfl && bEnoughRegs && genMakeIndAddrMode(tree, NULL, true, needReg, RegSet::FREE_REG, &regs, false))
            {
                emitAttr size;

                /* Is the value now computed in some register? */

                if (tree->InReg())
                {
                    genCodeForTree_REG_VAR1(tree);
                    return;
                }

                /* If we can reuse op1/2's register directly, and 'tree' is
                   a simple expression (ie. not in scaled index form),
                   might as well just use "add" instead of "lea" */

                // However, if we're in a context where we want to evaluate "tree" into a specific
                // register different from the reg we'd use in this optimization, then it doesn't
                // make sense to do the "add", since we'd also have to do a "mov."
                if (op1->InReg())
                {
                    reg = op1->gtRegNum;

                    if ((genRegMask(reg) & regSet.rsRegMaskFree()) && (genRegMask(reg) & needReg))
                    {
                        if (op2->InReg())
                        {
                            /* Simply add op2 to the register */

                            inst_RV_TT(INS_add, reg, op2, 0, emitTypeSize(treeType), flags);

                            if (tree->gtSetFlags())
                                genFlagsEqualToReg(tree, reg);

                            goto DONE_LEA_ADD;
                        }
                        else if (op2->OperGet() == GT_CNS_INT)
                        {
                            /* Simply add op2 to the register */

                            genIncRegBy(reg, op2->gtIntCon.gtIconVal, tree, treeType);

                            goto DONE_LEA_ADD;
                        }
                    }
                }

                if (op2->InReg())
                {
                    reg = op2->gtRegNum;

                    if ((genRegMask(reg) & regSet.rsRegMaskFree()) && (genRegMask(reg) & needReg))
                    {
                        if (op1->InReg())
                        {
                            /* Simply add op1 to the register */

                            inst_RV_TT(INS_add, reg, op1, 0, emitTypeSize(treeType), flags);

                            if (tree->gtSetFlags())
                                genFlagsEqualToReg(tree, reg);

                            goto DONE_LEA_ADD;
                        }
                    }
                }

                // The expression either requires a scaled-index form, or the
                // op1 or op2's register can't be targeted, this can be
                // caused when op1 or op2 are enregistered variables.

                reg  = regSet.rsPickReg(needReg, bestReg);
                size = emitActualTypeSize(treeType);

                /* Generate "lea reg, [addr-mode]" */

                inst_RV_AT(INS_lea, size, treeType, reg, tree, 0, flags);

#ifndef _TARGET_XARCH_
                // Don't call genFlagsEqualToReg on x86/x64
                //  as it does not set the flags
                if (tree->gtSetFlags())
                    genFlagsEqualToReg(tree, reg);
#endif

            DONE_LEA_ADD:
                /* The register has been trashed now */
                regTracker.rsTrackRegTrash(reg);

                genDoneAddressable(tree, regs, RegSet::FREE_REG);

                /* The following could be an 'inner' pointer!!! */

                noway_assert(treeType == TYP_BYREF || !varTypeIsGC(treeType));

                if (treeType == TYP_BYREF)
                {
                    genUpdateLife(tree);

                    gcInfo.gcMarkRegSetNpt(genRegMask(reg)); // in case "reg" was a TYP_GCREF before
                    gcInfo.gcMarkRegPtrVal(reg, TYP_BYREF);
                }

                genCodeForTree_DONE(tree, reg);
                return;
            }
        }

#endif // LEA_AVAILABLE

        noway_assert((varTypeIsGC(treeType) == false) || (treeType == TYP_BYREF && (ins == INS_add || ins == INS_sub)));
    }

    /* The following makes an assumption about gtSetEvalOrder(this) */

    noway_assert((tree->gtFlags & GTF_REVERSE_OPS) == 0);

    /* Compute a useful register mask */
    needReg = regSet.rsMustExclude(needReg, op2->gtRsvdRegs);
    needReg = regSet.rsNarrowHint(needReg, regSet.rsRegMaskFree());

    // Determine what registers go live between op1 and op2
    // Don't bother checking if op1 is already in a register.
    // This is not just for efficiency; if it's already in a
    // register then it may already be considered "evaluated"
    // for the purposes of liveness, in which genNewLiveRegMask
    // will assert
    if (!op1->InReg())
    {
        regMaskTP newLiveMask = genNewLiveRegMask(op1, op2);
        if (newLiveMask)
        {
            needReg = regSet.rsNarrowHint(needReg, ~newLiveMask);
        }
    }

#if CPU_HAS_BYTE_REGS
    /* 8-bit operations can only be done in the byte-regs */
    if (varTypeIsByte(treeType))
        needReg = regSet.rsNarrowHint(RBM_BYTE_REGS, needReg);
#endif // CPU_HAS_BYTE_REGS

    // Try selecting one of the 'bestRegs'
    needReg = regSet.rsNarrowHint(needReg, bestReg);

    /* Special case: small_val & small_mask */

    if (varTypeIsSmall(op1->TypeGet()) && op2->IsCnsIntOrI() && oper == GT_AND)
    {
        size_t    and_val = op2->gtIntCon.gtIconVal;
        size_t    andMask;
        var_types typ = op1->TypeGet();

        switch (typ)
        {
            case TYP_BOOL:
            case TYP_BYTE:
            case TYP_UBYTE:
                andMask = 0x000000FF;
                break;
            case TYP_SHORT:
            case TYP_CHAR:
                andMask = 0x0000FFFF;
                break;
            default:
                noway_assert(!"unexpected type");
                return;
        }

        // Is the 'and_val' completely contained within the bits found in 'andMask'
        if ((and_val & ~andMask) == 0)
        {
            // We must use unsigned instructions when loading op1
            if (varTypeIsByte(typ))
            {
                op1->gtType = TYP_UBYTE;
            }
            else // varTypeIsShort(typ)
            {
                assert(varTypeIsShort(typ));
                op1->gtType = TYP_CHAR;
            }

            /* Generate the first operand into a scratch register */

            op1 = genCodeForCommaTree(op1);
            genComputeReg(op1, needReg, RegSet::ANY_REG, RegSet::FREE_REG, true);

            noway_assert(op1->InReg());

            regNumber op1Reg = op1->gtRegNum;

            // Did we end up in an acceptable register?
            // and do we have an acceptable free register available to grab?
            //
            if (((genRegMask(op1Reg) & needReg) == 0) && ((regSet.rsRegMaskFree() & needReg) != 0))
            {
                // See if we can pick a register from bestReg
                bestReg &= needReg;

                // Grab an acceptable register
                regNumber newReg;
                if ((bestReg & regSet.rsRegMaskFree()) != 0)
                    newReg = regSet.rsGrabReg(bestReg);
                else
                    newReg = regSet.rsGrabReg(needReg);

                noway_assert(op1Reg != newReg);

                /* Update the value in the target register */

                regTracker.rsTrackRegCopy(newReg, op1Reg);

                inst_RV_RV(ins_Copy(op1->TypeGet()), newReg, op1Reg, op1->TypeGet());

                /* The value has been transferred to 'reg' */

                if ((genRegMask(op1Reg) & regSet.rsMaskUsed) == 0)
                    gcInfo.gcMarkRegSetNpt(genRegMask(op1Reg));

                gcInfo.gcMarkRegPtrVal(newReg, op1->TypeGet());

                /* The value is now in an appropriate register */

                op1->gtRegNum = newReg;
            }
            noway_assert(op1->InReg());
            genUpdateLife(op1);

            /* Mark the register as 'used' */
            regSet.rsMarkRegUsed(op1);
            reg = op1->gtRegNum;

            if (and_val != andMask) // Does the "and" mask only cover some of the bits?
            {
                /* "and" the value */

                inst_RV_IV(INS_AND, reg, and_val, EA_4BYTE, flags);
            }

#ifdef DEBUG
            /* Update the live set of register variables */
            if (compiler->opts.varNames)
                genUpdateLife(tree);
#endif

            /* Now we can update the register pointer information */

            genReleaseReg(op1);
            gcInfo.gcMarkRegPtrVal(reg, treeType);

            genCodeForTree_DONE_LIFE(tree, reg);
            return;
        }
    }

#ifdef _TARGET_XARCH_

    // Do we have to use the special "imul" instruction
    // which has eax as the implicit operand ?
    //
    bool multEAX = false;

    if (oper == GT_MUL)
    {
        if (tree->gtFlags & GTF_MUL_64RSLT)
        {
            /* Only multiplying with EAX will leave the 64-bit
             * result in EDX:EAX */

            multEAX = true;
        }
        else if (ovfl)
        {
            if (tree->gtFlags & GTF_UNSIGNED)
            {
                /* "mul reg/mem" always has EAX as default operand */

                multEAX = true;
            }
            else if (varTypeIsSmall(treeType))
            {
                /* Only the "imul with EAX" encoding has the 'w' bit
                 * to specify the size of the operands */

                multEAX = true;
            }
        }
    }

    if (multEAX)
    {
        noway_assert(oper == GT_MUL);

        return genCodeForMultEAX(tree);
    }
#endif // _TARGET_XARCH_

#ifdef _TARGET_ARM_

    // Do we have to use the special 32x32 => 64 bit multiply
    //
    bool mult64 = false;

    if (oper == GT_MUL)
    {
        if (tree->gtFlags & GTF_MUL_64RSLT)
        {
            mult64 = true;
        }
        else if (ovfl)
        {
            // We always must use the 32x32 => 64 bit multiply
            // to detect overflow
            mult64 = true;
        }
    }

    if (mult64)
    {
        noway_assert(oper == GT_MUL);

        return genCodeForMult64(tree, destReg, bestReg);
    }
#endif // _TARGET_ARM_

    /* Generate the first operand into a scratch register */

    op1 = genCodeForCommaTree(op1);
    genComputeReg(op1, needReg, RegSet::ANY_REG, RegSet::FREE_REG, true);

    noway_assert(op1->InReg());

    regNumber op1Reg = op1->gtRegNum;

    // Setup needReg with the set of register that we require for op1 to be in
    //
    needReg = RBM_ALLINT;

    /* Compute a useful register mask */
    needReg = regSet.rsMustExclude(needReg, op2->gtRsvdRegs);
    needReg = regSet.rsNarrowHint(needReg, regSet.rsRegMaskFree());

#if CPU_HAS_BYTE_REGS
    /* 8-bit operations can only be done in the byte-regs */
    if (varTypeIsByte(treeType))
        needReg = regSet.rsNarrowHint(RBM_BYTE_REGS, needReg);
#endif // CPU_HAS_BYTE_REGS

    // Did we end up in an acceptable register?
    // and do we have an acceptable free register available to grab?
    //
    if (((genRegMask(op1Reg) & needReg) == 0) && ((regSet.rsRegMaskFree() & needReg) != 0))
    {
        // See if we can pick a register from bestReg
        bestReg &= needReg;

        // Grab an acceptable register
        regNumber newReg;
        if ((bestReg & regSet.rsRegMaskFree()) != 0)
            newReg = regSet.rsGrabReg(bestReg);
        else
            newReg = regSet.rsGrabReg(needReg);

        noway_assert(op1Reg != newReg);

        /* Update the value in the target register */

        regTracker.rsTrackRegCopy(newReg, op1Reg);

        inst_RV_RV(ins_Copy(op1->TypeGet()), newReg, op1Reg, op1->TypeGet());

        /* The value has been transferred to 'reg' */

        if ((genRegMask(op1Reg) & regSet.rsMaskUsed) == 0)
            gcInfo.gcMarkRegSetNpt(genRegMask(op1Reg));

        gcInfo.gcMarkRegPtrVal(newReg, op1->TypeGet());

        /* The value is now in an appropriate register */

        op1->gtRegNum = newReg;
    }
    noway_assert(op1->InReg());
    op1Reg = op1->gtRegNum;

    genUpdateLife(op1);

    /* Mark the register as 'used' */
    regSet.rsMarkRegUsed(op1);

    bool isSmallConst = false;

#ifdef _TARGET_ARM_
    if ((op2->gtOper == GT_CNS_INT) && arm_Valid_Imm_For_Instr(ins, op2->gtIntCon.gtIconVal, INS_FLAGS_DONT_CARE))
    {
        isSmallConst = true;
    }
#endif
    /* Make the second operand addressable */

    regMaskTP addrReg = genMakeRvalueAddressable(op2, RBM_ALLINT, RegSet::KEEP_REG, isSmallConst);

#if CPU_LOAD_STORE_ARCH
    genRecoverReg(op1, RBM_ALLINT, RegSet::KEEP_REG);
#else  // !CPU_LOAD_STORE_ARCH
    /* Is op1 spilled and op2 in a register? */

    if ((op1->gtFlags & GTF_SPILLED) && (op2->InReg()) && (ins != INS_sub))
    {
        noway_assert(ins == INS_add || ins == INS_MUL || ins == INS_AND || ins == INS_OR || ins == INS_XOR);

        // genMakeRvalueAddressable(GT_LCL_VAR) shouldn't spill anything
        noway_assert(op2->gtOper != GT_LCL_VAR ||
                     varTypeIsSmall(compiler->lvaTable[op2->gtLclVarCommon.gtLclNum].TypeGet()));

        reg               = op2->gtRegNum;
        regMaskTP regMask = genRegMask(reg);

        /* Is the register holding op2 available? */

        if (regMask & regSet.rsMaskVars)
        {
        }
        else
        {
            /* Get the temp we spilled into. */

            TempDsc* temp = regSet.rsUnspillInPlace(op1, op1->gtRegNum);

            /* For 8bit operations, we need to make sure that op2 is
               in a byte-addressable registers */

            if (varTypeIsByte(treeType) && !(regMask & RBM_BYTE_REGS))
            {
                regNumber byteReg = regSet.rsGrabReg(RBM_BYTE_REGS);

                inst_RV_RV(INS_mov, byteReg, reg);
                regTracker.rsTrackRegTrash(byteReg);

                /* op2 couldn't have spilled as it was not sitting in
                   RBM_BYTE_REGS, and regSet.rsGrabReg() will only spill its args */
                noway_assert(op2->InReg());

                regSet.rsUnlockReg(regMask);
                regSet.rsMarkRegFree(regMask);

                reg           = byteReg;
                regMask       = genRegMask(reg);
                op2->gtRegNum = reg;
                regSet.rsMarkRegUsed(op2);
            }

            inst_RV_ST(ins, reg, temp, 0, treeType);

            regTracker.rsTrackRegTrash(reg);

            /* Free the temp */

            compiler->tmpRlsTemp(temp);

            /* 'add'/'sub' set all CC flags, others only ZF */

            /* If we need to check overflow, for small types, the
             * flags can't be used as we perform the arithmetic
             * operation (on small registers) and then sign extend it
             *
             * NOTE : If we ever don't need to sign-extend the result,
             * we can use the flags
             */

            if (tree->gtSetFlags())
            {
                genFlagsEqualToReg(tree, reg);
            }

            /* The result is where the second operand is sitting. Mark result reg as free */
            regSet.rsMarkRegFree(genRegMask(reg));

            gcInfo.gcMarkRegPtrVal(reg, treeType);

            goto CHK_OVF;
        }
    }
#endif // !CPU_LOAD_STORE_ARCH

    /* Make sure the first operand is still in a register */
    regSet.rsLockUsedReg(addrReg);
    genRecoverReg(op1, 0, RegSet::KEEP_REG);
    noway_assert(op1->InReg());
    regSet.rsUnlockUsedReg(addrReg);

    reg = op1->gtRegNum;

    // For 8 bit operations, we need to pick byte addressable registers

    if (varTypeIsByte(treeType) && !(genRegMask(reg) & RBM_BYTE_REGS))
    {
        regNumber byteReg = regSet.rsGrabReg(RBM_BYTE_REGS);

        inst_RV_RV(INS_mov, byteReg, reg);

        regTracker.rsTrackRegTrash(byteReg);
        regSet.rsMarkRegFree(genRegMask(reg));

        reg           = byteReg;
        op1->gtRegNum = reg;
        regSet.rsMarkRegUsed(op1);
    }

    /* Make sure the operand is still addressable */
    addrReg = genKeepAddressable(op2, addrReg, genRegMask(reg));

    /* Free up the operand, if it's a regvar */

    genUpdateLife(op2);

    /* The register is about to be trashed */

    regTracker.rsTrackRegTrash(reg);

    {
        bool op2Released = false;

        // For overflow instructions, tree->gtType is the accurate type,
        // and gives us the size for the operands.

        emitAttr opSize = emitTypeSize(treeType);

        /* Compute the new value */

        if (isArith && !op2->InReg() && (op2->OperKind() & GTK_CONST)
#if !CPU_HAS_FP_SUPPORT
            && (treeType == TYP_INT || treeType == TYP_I_IMPL)
#endif
                )
        {
            ssize_t ival = op2->gtIntCon.gtIconVal;

            if (oper == GT_ADD)
            {
                genIncRegBy(reg, ival, tree, treeType, ovfl);
            }
            else if (oper == GT_SUB)
            {
                if (ovfl && ((tree->gtFlags & GTF_UNSIGNED) ||
                             (ival == ((treeType == TYP_INT) ? INT32_MIN : SSIZE_T_MIN))) // -0x80000000 == 0x80000000.
                    // Therefore we can't use -ival.
                    )
                {
                    /* For unsigned overflow, we have to use INS_sub to set
                    the flags correctly */

                    genDecRegBy(reg, ival, tree);
                }
                else
                {
                    /* Else, we simply add the negative of the value */

                    genIncRegBy(reg, -ival, tree, treeType, ovfl);
                }
            }
            else if (oper == GT_MUL)
            {
                genMulRegBy(reg, ival, tree, treeType, ovfl);
            }
        }
        else
        {
            // op2 could be a GT_COMMA (i.e. an assignment for a CSE def)
            op2 = op2->gtEffectiveVal();
            if (varTypeIsByte(treeType) && op2->InReg())
            {
                noway_assert(genRegMask(reg) & RBM_BYTE_REGS);

                regNumber op2reg     = op2->gtRegNum;
                regMaskTP op2regMask = genRegMask(op2reg);

                if (!(op2regMask & RBM_BYTE_REGS))
                {
                    regNumber byteReg = regSet.rsGrabReg(RBM_BYTE_REGS);

                    inst_RV_RV(INS_mov, byteReg, op2reg);
                    regTracker.rsTrackRegTrash(byteReg);

                    genDoneAddressable(op2, addrReg, RegSet::KEEP_REG);
                    op2Released = true;

                    op2->gtRegNum = byteReg;
                }
            }

            inst_RV_TT(ins, reg, op2, 0, opSize, flags);
        }

        /* Free up anything that was tied up by the operand */

        if (!op2Released)
        {
            genDoneAddressable(op2, addrReg, RegSet::KEEP_REG);
        }
    }
    /* The result will be where the first operand is sitting */

    /* We must use RegSet::KEEP_REG since op1 can have a GC pointer here */
    genRecoverReg(op1, 0, RegSet::KEEP_REG);

    reg = op1->gtRegNum;

    /* 'add'/'sub' set all CC flags, others only ZF+SF */

    if (tree->gtSetFlags())
        genFlagsEqualToReg(tree, reg);

    genReleaseReg(op1);

#if !CPU_LOAD_STORE_ARCH
CHK_OVF:
#endif // !CPU_LOAD_STORE_ARCH

    /* Do we need an overflow check */

    if (ovfl)
        genCheckOverflow(tree);

    genCodeForTree_DONE(tree, reg);
}

/*****************************************************************************
 *
 *  Generate code for a simple binary arithmetic or logical assignment operator: x <op>= y.
 *  Handles GT_ASG_AND, GT_ASG_OR, GT_ASG_XOR, GT_ASG_ADD, GT_ASG_SUB.
 */

void CodeGen::genCodeForTreeSmpBinArithLogAsgOp(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    instruction      ins;
    const genTreeOps oper     = tree->OperGet();
    const var_types  treeType = tree->TypeGet();
    GenTreePtr       op1      = tree->gtOp.gtOp1;
    GenTreePtr       op2      = tree->gtGetOp2();
    insFlags         flags    = tree->gtSetFlags() ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
    regNumber        reg      = DUMMY_INIT(REG_CORRUPT);
    regMaskTP        needReg  = destReg;
    regMaskTP        addrReg;

    /* Figure out what instruction to generate */

    bool isArith;
    switch (oper)
    {
        case GT_ASG_AND:
            ins     = INS_AND;
            isArith = false;
            break;
        case GT_ASG_OR:
            ins     = INS_OR;
            isArith = false;
            break;
        case GT_ASG_XOR:
            ins     = INS_XOR;
            isArith = false;
            break;
        case GT_ASG_ADD:
            ins     = INS_add;
            isArith = true;
            break;
        case GT_ASG_SUB:
            ins     = INS_sub;
            isArith = true;
            break;
        default:
            unreached();
    }

    bool ovfl = false;

    if (isArith)
    {
        // We only reach here for GT_ASG_SUB, GT_ASG_ADD.

        ovfl = tree->gtOverflow();

        // We can't use += with overflow if the value cannot be changed
        // in case of an overflow-exception which the "+" might cause
        noway_assert(!ovfl ||
                     ((op1->gtOper == GT_LCL_VAR || op1->gtOper == GT_LCL_FLD) && !compiler->compCurBB->hasTryIndex()));

        /* Do not allow overflow instructions with refs/byrefs */

        noway_assert(!ovfl || !varTypeIsGC(treeType));

        // We disallow overflow and byte-ops here as it is too much trouble
        noway_assert(!ovfl || !varTypeIsByte(treeType));

        /* Is the second operand a constant? */

        if (op2->IsIntCnsFitsInI32())
        {
            int ival = (int)op2->gtIntCon.gtIconVal;

            /* What is the target of the assignment? */

            switch (op1->gtOper)
            {
                case GT_REG_VAR:

                REG_VAR4:

                    reg = op1->gtRegVar.gtRegNum;

                    /* No registers are needed for addressing */

                    addrReg = RBM_NONE;
#if !CPU_LOAD_STORE_ARCH
                INCDEC_REG:
#endif
                    /* We're adding a constant to a register */

                    if (oper == GT_ASG_ADD)
                        genIncRegBy(reg, ival, tree, treeType, ovfl);
                    else if (ovfl && ((tree->gtFlags & GTF_UNSIGNED) ||
                                      ival == ((treeType == TYP_INT) ? INT32_MIN : SSIZE_T_MIN)) // -0x80000000 ==
                                                                                                 // 0x80000000.
                                                                                                 // Therefore we can't
                                                                                                 // use -ival.
                             )
                        /* For unsigned overflow, we have to use INS_sub to set
                            the flags correctly */
                        genDecRegBy(reg, ival, tree);
                    else
                        genIncRegBy(reg, -ival, tree, treeType, ovfl);

                    break;

                case GT_LCL_VAR:

                    /* Does the variable live in a register? */

                    if (genMarkLclVar(op1))
                        goto REG_VAR4;

                    __fallthrough;

                default:

                    /* Make the target addressable for load/store */
                    addrReg = genMakeAddressable2(op1, needReg, RegSet::KEEP_REG, true, true);

#if !CPU_LOAD_STORE_ARCH
                    // For CPU_LOAD_STORE_ARCH, we always load from memory then store to memory

                    /* For small types with overflow check, we need to
                        sign/zero extend the result, so we need it in a reg */

                    if (ovfl && genTypeSize(treeType) < sizeof(int))
#endif // !CPU_LOAD_STORE_ARCH
                    {
                        // Load op1 into a reg

                        reg = regSet.rsGrabReg(RBM_ALLINT & ~addrReg);

                        inst_RV_TT(INS_mov, reg, op1);

                        // Issue the add/sub and the overflow check

                        inst_RV_IV(ins, reg, ival, emitActualTypeSize(treeType), flags);
                        regTracker.rsTrackRegTrash(reg);

                        if (ovfl)
                        {
                            genCheckOverflow(tree);
                        }

                        /* Store the (sign/zero extended) result back to
                            the stack location of the variable */

                        inst_TT_RV(ins_Store(op1->TypeGet()), op1, reg);

                        break;
                    }
#if !CPU_LOAD_STORE_ARCH
                    else
                    {
                        /* Add/subtract the new value into/from the target */

                        if (op1->InReg())
                        {
                            reg = op1->gtRegNum;
                            goto INCDEC_REG;
                        }

                        /* Special case: inc/dec (up to P3, or for small code, or blended code outside loops) */
                        if (!ovfl && (ival == 1 || ival == -1) &&
                            !compiler->optAvoidIncDec(compiler->compCurBB->getBBWeight(compiler)))
                        {
                            noway_assert(oper == GT_ASG_SUB || oper == GT_ASG_ADD);
                            if (oper == GT_ASG_SUB)
                                ival = -ival;

                            ins = (ival > 0) ? INS_inc : INS_dec;
                            inst_TT(ins, op1);
                        }
                        else
                        {
                            inst_TT_IV(ins, op1, ival);
                        }

                        if ((op1->gtOper == GT_LCL_VAR) && (!ovfl || treeType == TYP_INT))
                        {
                            if (tree->gtSetFlags())
                                genFlagsEqualToVar(tree, op1->gtLclVarCommon.gtLclNum);
                        }

                        break;
                    }
#endif        // !CPU_LOAD_STORE_ARCH
            } // end switch (op1->gtOper)

            genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);

            genCodeForTreeSmpOpAsg_DONE_ASSG(tree, addrReg, reg, ovfl);
            return;
        } // end if (op2->IsIntCnsFitsInI32())
    }     // end if (isArith)

    noway_assert(!varTypeIsGC(treeType) || ins == INS_sub || ins == INS_add);

    /* Is the target a register or local variable? */

    switch (op1->gtOper)
    {
        case GT_LCL_VAR:

            /* Does the target variable live in a register? */

            if (!genMarkLclVar(op1))
                break;

            __fallthrough;

        case GT_REG_VAR:

            /* Get hold of the target register */

            reg = op1->gtRegVar.gtRegNum;

            /* Make sure the target of the store is available */

            if (regSet.rsMaskUsed & genRegMask(reg))
            {
                regSet.rsSpillReg(reg);
            }

            /* Make the RHS addressable */

            addrReg = genMakeRvalueAddressable(op2, 0, RegSet::KEEP_REG, false);

            /* Compute the new value into the target register */
            CLANG_FORMAT_COMMENT_ANCHOR;

#if CPU_HAS_BYTE_REGS

            // Fix 383833 X86 ILGEN
            regNumber reg2;
            if (op2->InReg())
            {
                reg2 = op2->gtRegNum;
            }
            else
            {
                reg2 = REG_STK;
            }

            // We can only generate a byte ADD,SUB,OR,AND operation when reg and reg2 are both BYTE registers
            // when op2 is in memory then reg2==REG_STK and we will need to force op2 into a register
            //
            if (varTypeIsByte(treeType) &&
                (((genRegMask(reg) & RBM_BYTE_REGS) == 0) || ((genRegMask(reg2) & RBM_BYTE_REGS) == 0)))
            {
                // We will force op2 into a register (via sign/zero extending load)
                // for the cases where op2 is in memory and thus could have
                // an unmapped page just beyond its location
                //
                if ((op2->OperIsIndir() || (op2->gtOper == GT_CLS_VAR)) && varTypeIsSmall(op2->TypeGet()))
                {
                    genCodeForTree(op2, 0);
                    assert(op2->InReg());
                }

                inst_RV_TT(ins, reg, op2, 0, EA_4BYTE, flags);

                bool canOmit = false;

                if (varTypeIsUnsigned(treeType))
                {
                    // When op2 is a byte sized constant we can omit the zero extend instruction
                    if ((op2->gtOper == GT_CNS_INT) && ((op2->gtIntCon.gtIconVal & 0xFF) == op2->gtIntCon.gtIconVal))
                    {
                        canOmit = true;
                    }
                }
                else // treeType is signed
                {
                    // When op2 is a positive 7-bit or smaller constant
                    // we can omit the sign extension sequence.
                    if ((op2->gtOper == GT_CNS_INT) && ((op2->gtIntCon.gtIconVal & 0x7F) == op2->gtIntCon.gtIconVal))
                    {
                        canOmit = true;
                    }
                }

                if (!canOmit)
                {
                    // If reg is a byte reg then we can use a movzx/movsx instruction
                    //
                    if ((genRegMask(reg) & RBM_BYTE_REGS) != 0)
                    {
                        instruction extendIns = ins_Move_Extend(treeType, true);
                        inst_RV_RV(extendIns, reg, reg, treeType, emitTypeSize(treeType));
                    }
                    else // we can't encode a movzx/movsx instruction
                    {
                        if (varTypeIsUnsigned(treeType))
                        {
                            // otherwise, we must zero the upper 24 bits of 'reg'
                            inst_RV_IV(INS_AND, reg, 0xFF, EA_4BYTE);
                        }
                        else // treeType is signed
                        {
                            // otherwise, we must sign extend the result in the non-byteable register 'reg'
                            // We will shift the register left 24 bits, thus putting the sign-bit into the high bit
                            // then we do an arithmetic shift back 24 bits which propagate the sign bit correctly.
                            //
                            inst_RV_SH(INS_SHIFT_LEFT_LOGICAL, EA_4BYTE, reg, 24);
                            inst_RV_SH(INS_SHIFT_RIGHT_ARITHM, EA_4BYTE, reg, 24);
                        }
                    }
                }
            }
            else
#endif // CPU_HAS_BYTE_REGS
            {
                inst_RV_TT(ins, reg, op2, 0, emitTypeSize(treeType), flags);
            }

            /* The zero flag is now equal to the register value */

            if (tree->gtSetFlags())
                genFlagsEqualToReg(tree, reg);

            /* Remember that we trashed the target */

            regTracker.rsTrackRegTrash(reg);

            /* Free up anything that was tied up by the RHS */

            genDoneAddressable(op2, addrReg, RegSet::KEEP_REG);

            genCodeForTreeSmpOpAsg_DONE_ASSG(tree, addrReg, reg, ovfl);
            return;

        default:
            break;
    } // end switch (op1->gtOper)

#if !CPU_LOAD_STORE_ARCH
    /* Special case: "x ^= -1" is actually "not(x)" */

    if (oper == GT_ASG_XOR)
    {
        if (op2->gtOper == GT_CNS_INT && op2->gtIntCon.gtIconVal == -1)
        {
            addrReg = genMakeAddressable(op1, RBM_ALLINT, RegSet::KEEP_REG, true);
            inst_TT(INS_NOT, op1);
            genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);

            genCodeForTreeSmpOpAsg_DONE_ASSG(tree, addrReg, tree->gtRegNum, ovfl);
            return;
        }
    }
#endif // !CPU_LOAD_STORE_ARCH

    /* Setup target mask for op2 (byte-regs for small operands) */

    unsigned needMask;
    needMask = (varTypeIsByte(treeType)) ? RBM_BYTE_REGS : RBM_ALLINT;

    /* Is the second operand a constant? */

    if (op2->IsIntCnsFitsInI32())
    {
        int ival = (int)op2->gtIntCon.gtIconVal;

        /* Make the target addressable */
        addrReg = genMakeAddressable(op1, needReg, RegSet::FREE_REG, true);

        inst_TT_IV(ins, op1, ival, 0, emitTypeSize(treeType), flags);

        genDoneAddressable(op1, addrReg, RegSet::FREE_REG);

        genCodeForTreeSmpOpAsg_DONE_ASSG(tree, addrReg, tree->gtRegNum, ovfl);
        return;
    }

    /* Is the value or the address to be computed first? */

    if (tree->gtFlags & GTF_REVERSE_OPS)
    {
        /* Compute the new value into a register */

        genComputeReg(op2, needMask, RegSet::EXACT_REG, RegSet::KEEP_REG);

        /* Make the target addressable for load/store */
        addrReg = genMakeAddressable2(op1, 0, RegSet::KEEP_REG, true, true);
        regSet.rsLockUsedReg(addrReg);

#if !CPU_LOAD_STORE_ARCH
        // For CPU_LOAD_STORE_ARCH, we always load from memory then store to memory
        /* For small types with overflow check, we need to
            sign/zero extend the result, so we need it in a reg */

        if (ovfl && genTypeSize(treeType) < sizeof(int))
#endif // !CPU_LOAD_STORE_ARCH
        {
            reg = regSet.rsPickReg();
            regSet.rsLockReg(genRegMask(reg));

            noway_assert(genIsValidReg(reg));

            /* Generate "ldr reg, [var]" */

            inst_RV_TT(ins_Load(op1->TypeGet()), reg, op1);

            if (op1->gtOper == GT_LCL_VAR)
                regTracker.rsTrackRegLclVar(reg, op1->gtLclVar.gtLclNum);
            else
                regTracker.rsTrackRegTrash(reg);

            /* Make sure the new value is in a register */

            genRecoverReg(op2, 0, RegSet::KEEP_REG);

            /* Compute the new value */

            inst_RV_RV(ins, reg, op2->gtRegNum, treeType, emitTypeSize(treeType), flags);

            if (ovfl)
                genCheckOverflow(tree);

            /* Move the new value back to the variable */
            /* Generate "str reg, [var]" */

            inst_TT_RV(ins_Store(op1->TypeGet()), op1, reg);
            regSet.rsUnlockReg(genRegMask(reg));

            if (op1->gtOper == GT_LCL_VAR)
                regTracker.rsTrackRegLclVar(reg, op1->gtLclVarCommon.gtLclNum);
        }
#if !CPU_LOAD_STORE_ARCH
        else
        {
            /* Make sure the new value is in a register */

            genRecoverReg(op2, 0, RegSet::KEEP_REG);

            /* Add the new value into the target */

            inst_TT_RV(ins, op1, op2->gtRegNum);
        }
#endif // !CPU_LOAD_STORE_ARCH
        /* Free up anything that was tied up either side */
        regSet.rsUnlockUsedReg(addrReg);
        genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);
        genReleaseReg(op2);
    }
    else
    {
        /* Make the target addressable */

        addrReg = genMakeAddressable2(op1, RBM_ALLINT & ~op2->gtRsvdRegs, RegSet::KEEP_REG, true, true);

        /* Compute the new value into a register */

        genComputeReg(op2, needMask, RegSet::EXACT_REG, RegSet::KEEP_REG);
        regSet.rsLockUsedReg(genRegMask(op2->gtRegNum));

        /* Make sure the target is still addressable */

        addrReg = genKeepAddressable(op1, addrReg);
        regSet.rsLockUsedReg(addrReg);

#if !CPU_LOAD_STORE_ARCH
        // For CPU_LOAD_STORE_ARCH, we always load from memory then store to memory

        /* For small types with overflow check, we need to
            sign/zero extend the result, so we need it in a reg */

        if (ovfl && genTypeSize(treeType) < sizeof(int))
#endif // !CPU_LOAD_STORE_ARCH
        {
            reg = regSet.rsPickReg();

            inst_RV_TT(INS_mov, reg, op1);

            inst_RV_RV(ins, reg, op2->gtRegNum, treeType, emitTypeSize(treeType), flags);
            regTracker.rsTrackRegTrash(reg);

            if (ovfl)
                genCheckOverflow(tree);

            inst_TT_RV(ins_Store(op1->TypeGet()), op1, reg);

            if (op1->gtOper == GT_LCL_VAR)
                regTracker.rsTrackRegLclVar(reg, op1->gtLclVar.gtLclNum);
        }
#if !CPU_LOAD_STORE_ARCH
        else
        {
            /* Add the new value into the target */

            inst_TT_RV(ins, op1, op2->gtRegNum);
        }
#endif

        /* Free up anything that was tied up either side */
        regSet.rsUnlockUsedReg(addrReg);
        genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);

        regSet.rsUnlockUsedReg(genRegMask(op2->gtRegNum));
        genReleaseReg(op2);
    }

    genCodeForTreeSmpOpAsg_DONE_ASSG(tree, addrReg, reg, ovfl);
}

/*****************************************************************************
 *
 *  Generate code for GT_UMOD.
 */

void CodeGen::genCodeForUnsignedMod(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    assert(tree->OperGet() == GT_UMOD);

    GenTreePtr      op1      = tree->gtOp.gtOp1;
    GenTreePtr      op2      = tree->gtOp.gtOp2;
    const var_types treeType = tree->TypeGet();
    regMaskTP       needReg  = destReg;
    regNumber       reg;

    /* Is this a division by an integer constant? */

    noway_assert(op2);
    if (compiler->fgIsUnsignedModOptimizable(op2))
    {
        /* Generate the operand into some register */

        genCompIntoFreeReg(op1, needReg, RegSet::FREE_REG);
        noway_assert(op1->InReg());

        reg = op1->gtRegNum;

        /* Generate the appropriate sequence */
        size_t ival = op2->gtIntCon.gtIconVal - 1;
        inst_RV_IV(INS_AND, reg, ival, emitActualTypeSize(treeType));

        /* The register is now trashed */

        regTracker.rsTrackRegTrash(reg);

        genCodeForTree_DONE(tree, reg);
        return;
    }

    genCodeForGeneralDivide(tree, destReg, bestReg);
}

/*****************************************************************************
 *
 *  Generate code for GT_MOD.
 */

void CodeGen::genCodeForSignedMod(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    assert(tree->OperGet() == GT_MOD);

    GenTreePtr      op1      = tree->gtOp.gtOp1;
    GenTreePtr      op2      = tree->gtOp.gtOp2;
    const var_types treeType = tree->TypeGet();
    regMaskTP       needReg  = destReg;
    regNumber       reg;

    /* Is this a division by an integer constant? */

    noway_assert(op2);
    if (compiler->fgIsSignedModOptimizable(op2))
    {
        ssize_t     ival = op2->gtIntCon.gtIconVal;
        BasicBlock* skip = genCreateTempLabel();

        /* Generate the operand into some register */

        genCompIntoFreeReg(op1, needReg, RegSet::FREE_REG);
        noway_assert(op1->InReg());

        reg = op1->gtRegNum;

        /* Generate the appropriate sequence */

        inst_RV_IV(INS_AND, reg, (int)(ival - 1) | 0x80000000, EA_4BYTE, INS_FLAGS_SET);

        /* The register is now trashed */

        regTracker.rsTrackRegTrash(reg);

        /* Check and branch for a postive value */
        emitJumpKind jmpGEL = genJumpKindForOper(GT_GE, CK_LOGICAL);
        inst_JMP(jmpGEL, skip);

        /* Generate the rest of the sequence and we're done */

        genIncRegBy(reg, -1, NULL, treeType);
        ival = -ival;
        if ((treeType == TYP_LONG) && ((int)ival != ival))
        {
            regNumber immReg = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(reg));
            instGen_Set_Reg_To_Imm(EA_8BYTE, immReg, ival);
            inst_RV_RV(INS_OR, reg, immReg, TYP_LONG);
        }
        else
        {
            inst_RV_IV(INS_OR, reg, (int)ival, emitActualTypeSize(treeType));
        }
        genIncRegBy(reg, 1, NULL, treeType);

        /* Define the 'skip' label and we're done */

        genDefineTempLabel(skip);

        genCodeForTree_DONE(tree, reg);
        return;
    }

    genCodeForGeneralDivide(tree, destReg, bestReg);
}

/*****************************************************************************
 *
 *  Generate code for GT_UDIV.
 */

void CodeGen::genCodeForUnsignedDiv(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    assert(tree->OperGet() == GT_UDIV);

    GenTreePtr      op1      = tree->gtOp.gtOp1;
    GenTreePtr      op2      = tree->gtOp.gtOp2;
    const var_types treeType = tree->TypeGet();
    regMaskTP       needReg  = destReg;
    regNumber       reg;

    /* Is this a division by an integer constant? */

    noway_assert(op2);
    if (compiler->fgIsUnsignedDivOptimizable(op2))
    {
        size_t ival = op2->gtIntCon.gtIconVal;

        /* Division by 1 must be handled elsewhere */

        noway_assert(ival != 1 || compiler->opts.MinOpts());

        /* Generate the operand into some register */

        genCompIntoFreeReg(op1, needReg, RegSet::FREE_REG);
        noway_assert(op1->InReg());

        reg = op1->gtRegNum;

        /* Generate "shr reg, log2(value)" */

        inst_RV_SH(INS_SHIFT_RIGHT_LOGICAL, emitTypeSize(treeType), reg, genLog2(ival));

        /* The register is now trashed */

        regTracker.rsTrackRegTrash(reg);

        genCodeForTree_DONE(tree, reg);
        return;
    }

    genCodeForGeneralDivide(tree, destReg, bestReg);
}

/*****************************************************************************
 *
 *  Generate code for GT_DIV.
 */

void CodeGen::genCodeForSignedDiv(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    assert(tree->OperGet() == GT_DIV);

    GenTreePtr      op1      = tree->gtOp.gtOp1;
    GenTreePtr      op2      = tree->gtOp.gtOp2;
    const var_types treeType = tree->TypeGet();
    regMaskTP       needReg  = destReg;
    regNumber       reg;

    /* Is this a division by an integer constant? */

    noway_assert(op2);
    if (compiler->fgIsSignedDivOptimizable(op2))
    {
        ssize_t ival_s = op2->gtIntConCommon.IconValue();
        assert(ival_s > 0); // Postcondition of compiler->fgIsSignedDivOptimizable...
        size_t ival = static_cast<size_t>(ival_s);

        /* Division by 1 must be handled elsewhere */

        noway_assert(ival != 1);

        BasicBlock* onNegDivisee = genCreateTempLabel();

        /* Generate the operand into some register */

        genCompIntoFreeReg(op1, needReg, RegSet::FREE_REG);
        noway_assert(op1->InReg());

        reg = op1->gtRegNum;

        if (ival == 2)
        {
            /* Generate "sar reg, log2(value)" */

            inst_RV_SH(INS_SHIFT_RIGHT_ARITHM, emitTypeSize(treeType), reg, genLog2(ival), INS_FLAGS_SET);

            // Check and branch for a postive value, skipping the INS_ADDC instruction
            emitJumpKind jmpGEL = genJumpKindForOper(GT_GE, CK_LOGICAL);
            inst_JMP(jmpGEL, onNegDivisee);

            // Add the carry flag to 'reg'
            inst_RV_IV(INS_ADDC, reg, 0, emitActualTypeSize(treeType));

            /* Define the 'onNegDivisee' label and we're done */

            genDefineTempLabel(onNegDivisee);

            /* The register is now trashed */

            regTracker.rsTrackRegTrash(reg);

            /* The result is the same as the operand */

            reg = op1->gtRegNum;
        }
        else
        {
            /* Generate the following sequence */
            /*
            test    reg, reg
            jns     onNegDivisee
            add     reg, ival-1
            onNegDivisee:
            sar     reg, log2(ival)
            */

            instGen_Compare_Reg_To_Zero(emitTypeSize(treeType), reg);

            // Check and branch for a postive value, skipping the INS_add instruction
            emitJumpKind jmpGEL = genJumpKindForOper(GT_GE, CK_LOGICAL);
            inst_JMP(jmpGEL, onNegDivisee);

            inst_RV_IV(INS_add, reg, (int)ival - 1, emitActualTypeSize(treeType));

            /* Define the 'onNegDivisee' label and we're done */

            genDefineTempLabel(onNegDivisee);

            /* Generate "sar reg, log2(value)" */

            inst_RV_SH(INS_SHIFT_RIGHT_ARITHM, emitTypeSize(treeType), reg, genLog2(ival));

            /* The register is now trashed */

            regTracker.rsTrackRegTrash(reg);

            /* The result is the same as the operand */

            reg = op1->gtRegNum;
        }

        genCodeForTree_DONE(tree, reg);
        return;
    }

    genCodeForGeneralDivide(tree, destReg, bestReg);
}

/*****************************************************************************
 *
 *  Generate code for a general divide. Handles the general case for GT_UMOD, GT_MOD, GT_UDIV, GT_DIV
 *  (if op2 is not a power of 2 constant).
 */

void CodeGen::genCodeForGeneralDivide(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    assert(tree->OperGet() == GT_UMOD || tree->OperGet() == GT_MOD || tree->OperGet() == GT_UDIV ||
           tree->OperGet() == GT_DIV);

    GenTreePtr      op1      = tree->gtOp.gtOp1;
    GenTreePtr      op2      = tree->gtOp.gtOp2;
    const var_types treeType = tree->TypeGet();
    regMaskTP       needReg  = destReg;
    regNumber       reg;
    instruction     ins;
    bool            gotOp1;
    regMaskTP       addrReg;

#if USE_HELPERS_FOR_INT_DIV
    noway_assert(!"Unreachable: fgMorph should have transformed this into a JitHelper");
#endif

#if defined(_TARGET_XARCH_)

    /* Which operand are we supposed to evaluate first? */

    if (tree->gtFlags & GTF_REVERSE_OPS)
    {
        /* We'll evaluate 'op2' first */

        gotOp1 = false;
        destReg &= ~op1->gtRsvdRegs;

        /* Also if op1 is an enregistered LCL_VAR then exclude its register as well */
        if (op1->gtOper == GT_LCL_VAR)
        {
            unsigned varNum = op1->gtLclVarCommon.gtLclNum;
            noway_assert(varNum < compiler->lvaCount);
            LclVarDsc* varDsc = compiler->lvaTable + varNum;
            if (varDsc->lvRegister)
            {
                destReg &= ~genRegMask(varDsc->lvRegNum);
            }
        }
    }
    else
    {
        /* We'll evaluate 'op1' first */

        gotOp1 = true;

        regMaskTP op1Mask;
        if (RBM_EAX & op2->gtRsvdRegs)
            op1Mask = RBM_ALLINT & ~op2->gtRsvdRegs;
        else
            op1Mask = RBM_EAX; // EAX would be ideal

        /* Generate the dividend into EAX and hold on to it. freeOnly=true */

        genComputeReg(op1, op1Mask, RegSet::ANY_REG, RegSet::KEEP_REG, true);
    }

    /* We want to avoid using EAX or EDX for the second operand */

    destReg = regSet.rsMustExclude(destReg, RBM_EAX | RBM_EDX);

    /* Make the second operand addressable */
    op2 = genCodeForCommaTree(op2);

    /* Special case: if op2 is a local var we are done */

    if (op2->gtOper == GT_LCL_VAR || op2->gtOper == GT_LCL_FLD)
    {
        if (!op2->InReg())
            addrReg = genMakeRvalueAddressable(op2, destReg, RegSet::KEEP_REG, false);
        else
            addrReg = 0;
    }
    else
    {
        genComputeReg(op2, destReg, RegSet::ANY_REG, RegSet::KEEP_REG);

        noway_assert(op2->InReg());
        addrReg = genRegMask(op2->gtRegNum);
    }

    /* Make sure we have the dividend in EAX */

    if (gotOp1)
    {
        /* We've previously computed op1 into EAX */

        genRecoverReg(op1, RBM_EAX, RegSet::KEEP_REG);
    }
    else
    {
        /* Compute op1 into EAX and hold on to it */

        genComputeReg(op1, RBM_EAX, RegSet::EXACT_REG, RegSet::KEEP_REG, true);
    }

    noway_assert(op1->InReg());
    noway_assert(op1->gtRegNum == REG_EAX);

    /* We can now safely (we think) grab EDX */

    regSet.rsGrabReg(RBM_EDX);
    regSet.rsLockReg(RBM_EDX);

    /* Convert the integer in EAX into a un/signed long in EDX:EAX */

    const genTreeOps oper = tree->OperGet();

    if (oper == GT_UMOD || oper == GT_UDIV)
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, REG_EDX);
    else
        instGen(INS_cdq);

    /* Make sure the divisor is still addressable */

    addrReg = genKeepAddressable(op2, addrReg, RBM_EAX);

    /* Perform the division */

    if (oper == GT_UMOD || oper == GT_UDIV)
        inst_TT(INS_UNSIGNED_DIVIDE, op2);
    else
        inst_TT(INS_SIGNED_DIVIDE, op2);

    /* Free up anything tied up by the divisor's address */

    genDoneAddressable(op2, addrReg, RegSet::KEEP_REG);

    /* Unlock and free EDX */

    regSet.rsUnlockReg(RBM_EDX);

    /* Free up op1 (which is in EAX) as well */

    genReleaseReg(op1);

    /* Both EAX and EDX are now trashed */

    regTracker.rsTrackRegTrash(REG_EAX);
    regTracker.rsTrackRegTrash(REG_EDX);

    /* Figure out which register the result is in */

    reg = (oper == GT_DIV || oper == GT_UDIV) ? REG_EAX : REG_EDX;

    /* Don't forget to mark the first operand as using EAX and EDX */

    op1->gtRegNum = reg;

    genCodeForTree_DONE(tree, reg);

#elif defined(_TARGET_ARM_)

    /* Which operand are we supposed to evaluate first? */

    if (tree->gtFlags & GTF_REVERSE_OPS)
    {
        /* We'll evaluate 'op2' first */

        gotOp1 = false;
        destReg &= ~op1->gtRsvdRegs;

        /* Also if op1 is an enregistered LCL_VAR then exclude its register as well */
        if (op1->gtOper == GT_LCL_VAR)
        {
            unsigned varNum = op1->gtLclVarCommon.gtLclNum;
            noway_assert(varNum < compiler->lvaCount);
            LclVarDsc* varDsc = compiler->lvaTable + varNum;
            if (varDsc->lvRegister)
            {
                destReg &= ~genRegMask(varDsc->lvRegNum);
            }
        }
    }
    else
    {
        /* We'll evaluate 'op1' first */

        gotOp1            = true;
        regMaskTP op1Mask = RBM_ALLINT & ~op2->gtRsvdRegs;

        /* Generate the dividend into a register and hold on to it. */

        genComputeReg(op1, op1Mask, RegSet::ANY_REG, RegSet::KEEP_REG, true);
    }

    /* Evaluate the second operand into a register and hold onto it. */

    genComputeReg(op2, destReg, RegSet::ANY_REG, RegSet::KEEP_REG);

    noway_assert(op2->InReg());
    addrReg = genRegMask(op2->gtRegNum);

    if (gotOp1)
    {
        // Recover op1 if spilled
        genRecoverReg(op1, RBM_NONE, RegSet::KEEP_REG);
    }
    else
    {
        /* Compute op1 into any register and hold on to it */
        genComputeReg(op1, RBM_ALLINT, RegSet::ANY_REG, RegSet::KEEP_REG, true);
    }
    noway_assert(op1->InReg());

    reg = regSet.rsPickReg(needReg, bestReg);

    // Perform the divison

    const genTreeOps oper = tree->OperGet();

    if (oper == GT_UMOD || oper == GT_UDIV)
        ins = INS_udiv;
    else
        ins = INS_sdiv;

    getEmitter()->emitIns_R_R_R(ins, EA_4BYTE, reg, op1->gtRegNum, op2->gtRegNum);

    if (oper == GT_UMOD || oper == GT_MOD)
    {
        getEmitter()->emitIns_R_R_R(INS_mul, EA_4BYTE, reg, op2->gtRegNum, reg);
        getEmitter()->emitIns_R_R_R(INS_sub, EA_4BYTE, reg, op1->gtRegNum, reg);
    }
    /* Free up op1 and op2 */
    genReleaseReg(op1);
    genReleaseReg(op2);

    genCodeForTree_DONE(tree, reg);

#else
#error "Unknown _TARGET_"
#endif
}

/*****************************************************************************
 *
 *  Generate code for an assignment shift (x <op>= ). Handles GT_ASG_LSH, GT_ASG_RSH, GT_ASG_RSZ.
 */

void CodeGen::genCodeForAsgShift(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    assert(tree->OperGet() == GT_ASG_LSH || tree->OperGet() == GT_ASG_RSH || tree->OperGet() == GT_ASG_RSZ);

    const genTreeOps oper     = tree->OperGet();
    GenTreePtr       op1      = tree->gtOp.gtOp1;
    GenTreePtr       op2      = tree->gtOp.gtOp2;
    const var_types  treeType = tree->TypeGet();
    insFlags         flags    = tree->gtSetFlags() ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
    regMaskTP        needReg  = destReg;
    regNumber        reg;
    instruction      ins;
    regMaskTP        addrReg;

    switch (oper)
    {
        case GT_ASG_LSH:
            ins = INS_SHIFT_LEFT_LOGICAL;
            break;
        case GT_ASG_RSH:
            ins = INS_SHIFT_RIGHT_ARITHM;
            break;
        case GT_ASG_RSZ:
            ins = INS_SHIFT_RIGHT_LOGICAL;
            break;
        default:
            unreached();
    }

    noway_assert(!varTypeIsGC(treeType));
    noway_assert(op2);

    /* Shifts by a constant amount are easier */

    if (op2->IsCnsIntOrI())
    {
        /* Make the target addressable */

        addrReg = genMakeAddressable(op1, needReg, RegSet::KEEP_REG, true);

        /* Are we shifting a register left by 1 bit? */

        if ((oper == GT_ASG_LSH) && (op2->gtIntCon.gtIconVal == 1) && op1->InReg())
        {
            /* The target lives in a register */

            reg = op1->gtRegNum;

            /* "add reg, reg" is cheaper than "shl reg, 1" */

            inst_RV_RV(INS_add, reg, reg, treeType, emitActualTypeSize(treeType), flags);
        }
        else
        {
#if CPU_LOAD_STORE_ARCH
            if (!op1->InReg())
            {
                regSet.rsLockUsedReg(addrReg);

                // Load op1 into a reg

                reg = regSet.rsPickReg(RBM_ALLINT);

                inst_RV_TT(INS_mov, reg, op1);

                // Issue the shift

                inst_RV_IV(ins, reg, (int)op2->gtIntCon.gtIconVal, emitActualTypeSize(treeType), flags);
                regTracker.rsTrackRegTrash(reg);

                /* Store the (sign/zero extended) result back to the stack location of the variable */

                inst_TT_RV(ins_Store(op1->TypeGet()), op1, reg);

                regSet.rsUnlockUsedReg(addrReg);
            }
            else
#endif // CPU_LOAD_STORE_ARCH
            {
                /* Shift by the constant value */

                inst_TT_SH(ins, op1, (int)op2->gtIntCon.gtIconVal);
            }
        }

        /* If the target is a register, it has a new value */

        if (op1->InReg())
            regTracker.rsTrackRegTrash(op1->gtRegNum);

        genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);

        /* The zero flag is now equal to the target value */
        /* X86: But only if the shift count is != 0 */

        if (op2->gtIntCon.gtIconVal != 0)
        {
            if (tree->gtSetFlags())
            {
                if (op1->gtOper == GT_LCL_VAR)
                {
                    genFlagsEqualToVar(tree, op1->gtLclVarCommon.gtLclNum);
                }
                else if (op1->gtOper == GT_REG_VAR)
                {
                    genFlagsEqualToReg(tree, op1->gtRegNum);
                }
            }
        }
        else
        {
            // It is possible for the shift count to equal 0 with valid
            // IL, and not be optimized away, in the case where the node
            // is of a small type.  The sequence of instructions looks like
            // ldsfld, shr, stsfld and executed on a char field.  This will
            // never happen with code produced by our compilers, because the
            // compilers will insert a conv.u2 before the stsfld (which will
            // lead us down a different codepath in the JIT and optimize away
            // the shift by zero).  This case is not worth optimizing and we
            // will just make sure to generate correct code for it.

            genFlagsEqualToNone();
        }
    }
    else
    {
        regMaskTP op2Regs = RBM_NONE;
        if (REG_SHIFT != REG_NA)
            op2Regs = RBM_SHIFT;

        regMaskTP tempRegs;

        if (tree->gtFlags & GTF_REVERSE_OPS)
        {
            tempRegs = regSet.rsMustExclude(op2Regs, op1->gtRsvdRegs);
            genCodeForTree(op2, tempRegs);
            regSet.rsMarkRegUsed(op2);

            tempRegs = regSet.rsMustExclude(RBM_ALLINT, genRegMask(op2->gtRegNum));
            addrReg  = genMakeAddressable(op1, tempRegs, RegSet::KEEP_REG, true);

            genRecoverReg(op2, op2Regs, RegSet::KEEP_REG);
        }
        else
        {
            /* Make the target addressable avoiding op2->RsvdRegs [and RBM_SHIFT] */
            regMaskTP excludeMask = op2->gtRsvdRegs;
            if (REG_SHIFT != REG_NA)
                excludeMask |= RBM_SHIFT;

            tempRegs = regSet.rsMustExclude(RBM_ALLINT, excludeMask);
            addrReg  = genMakeAddressable(op1, tempRegs, RegSet::KEEP_REG, true);

            /* Load the shift count into the necessary register */
            genComputeReg(op2, op2Regs, RegSet::EXACT_REG, RegSet::KEEP_REG);
        }

        /* Make sure the address registers are still here */
        addrReg = genKeepAddressable(op1, addrReg, op2Regs);

#ifdef _TARGET_XARCH_
        /* Perform the shift */
        inst_TT_CL(ins, op1);
#else
        /* Perform the shift */
        noway_assert(op2->InReg());
        op2Regs = genRegMask(op2->gtRegNum);

        regSet.rsLockUsedReg(addrReg | op2Regs);
        inst_TT_RV(ins, op1, op2->gtRegNum, 0, emitTypeSize(treeType), flags);
        regSet.rsUnlockUsedReg(addrReg | op2Regs);
#endif
        /* Free the address registers */
        genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);

        /* If the value is in a register, it's now trash */

        if (op1->InReg())
            regTracker.rsTrackRegTrash(op1->gtRegNum);

        /* Release the op2 [RBM_SHIFT] operand */

        genReleaseReg(op2);
    }

    genCodeForTreeSmpOpAsg_DONE_ASSG(tree, addrReg, /* unused for ovfl=false */ REG_NA, /* ovfl */ false);
}

/*****************************************************************************
 *
 *  Generate code for a shift. Handles GT_LSH, GT_RSH, GT_RSZ.
 */

void CodeGen::genCodeForShift(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    assert(tree->OperIsShift());

    const genTreeOps oper     = tree->OperGet();
    GenTreePtr       op1      = tree->gtOp.gtOp1;
    GenTreePtr       op2      = tree->gtOp.gtOp2;
    const var_types  treeType = tree->TypeGet();
    insFlags         flags    = tree->gtSetFlags() ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
    regMaskTP        needReg  = destReg;
    regNumber        reg;
    instruction      ins;

    switch (oper)
    {
        case GT_LSH:
            ins = INS_SHIFT_LEFT_LOGICAL;
            break;
        case GT_RSH:
            ins = INS_SHIFT_RIGHT_ARITHM;
            break;
        case GT_RSZ:
            ins = INS_SHIFT_RIGHT_LOGICAL;
            break;
        default:
            unreached();
    }

    /* Is the shift count constant? */
    noway_assert(op2);
    if (op2->IsIntCnsFitsInI32())
    {
        // TODO: Check to see if we could generate a LEA instead!

        /* Compute the left operand into any free register */

        genCompIntoFreeReg(op1, needReg, RegSet::KEEP_REG);

        noway_assert(op1->InReg());
        reg = op1->gtRegNum;

        /* Are we shifting left by 1 bit? (or 2 bits for fast code) */

        // On ARM, until proven otherwise by performance numbers, just do the shift.
        // It's no bigger than add (16 bits for low registers, 32 bits for high registers).
        // It's smaller than two "add reg, reg".

        CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef _TARGET_ARM_
        if (oper == GT_LSH)
        {
            emitAttr size = emitActualTypeSize(treeType);
            if (op2->gtIntConCommon.IconValue() == 1)
            {
                /* "add reg, reg" is smaller and faster than "shl reg, 1" */
                inst_RV_RV(INS_add, reg, reg, treeType, size, flags);
            }
            else if ((op2->gtIntConCommon.IconValue() == 2) && (compiler->compCodeOpt() == Compiler::FAST_CODE))
            {
                /* two "add reg, reg" instructions are faster than "shl reg, 2" */
                inst_RV_RV(INS_add, reg, reg, treeType);
                inst_RV_RV(INS_add, reg, reg, treeType, size, flags);
            }
            else
                goto DO_SHIFT_BY_CNS;
        }
        else
#endif // _TARGET_ARM_
        {
#ifndef _TARGET_ARM_
        DO_SHIFT_BY_CNS:
#endif // _TARGET_ARM_
            // If we are shifting 'reg' by zero bits and do not need the flags to be set
            // then we can just skip emitting the instruction as 'reg' is already correct.
            //
            if ((op2->gtIntConCommon.IconValue() != 0) || tree->gtSetFlags())
            {
                /* Generate the appropriate shift instruction */
                inst_RV_SH(ins, emitTypeSize(treeType), reg, (int)op2->gtIntConCommon.IconValue(), flags);
            }
        }
    }
    else
    {
        /* Calculate a useful register mask for computing op1 */
        needReg = regSet.rsNarrowHint(regSet.rsRegMaskFree(), needReg);
        regMaskTP op2RegMask;
#ifdef _TARGET_XARCH_
        op2RegMask = RBM_ECX;
#else
        op2RegMask = RBM_NONE;
#endif
        needReg = regSet.rsMustExclude(needReg, op2RegMask);

        regMaskTP tempRegs;

        /* Which operand are we supposed to evaluate first? */
        if (tree->gtFlags & GTF_REVERSE_OPS)
        {
            /* Load the shift count [into ECX on XARCH] */
            tempRegs = regSet.rsMustExclude(op2RegMask, op1->gtRsvdRegs);
            genComputeReg(op2, tempRegs, RegSet::EXACT_REG, RegSet::KEEP_REG, false);

            /* We must not target the register that is holding op2 */
            needReg = regSet.rsMustExclude(needReg, genRegMask(op2->gtRegNum));

            /* Now evaluate 'op1' into a free register */
            genComputeReg(op1, needReg, RegSet::ANY_REG, RegSet::KEEP_REG, true);

            /* Recover op2 into ECX */
            genRecoverReg(op2, op2RegMask, RegSet::KEEP_REG);
        }
        else
        {
            /* Compute op1 into a register, trying to avoid op2->rsvdRegs and ECX */
            tempRegs = regSet.rsMustExclude(needReg, op2->gtRsvdRegs);
            genComputeReg(op1, tempRegs, RegSet::ANY_REG, RegSet::KEEP_REG, true);

            /* Load the shift count [into ECX on XARCH] */
            genComputeReg(op2, op2RegMask, RegSet::EXACT_REG, RegSet::KEEP_REG, false);
        }

        noway_assert(op2->InReg());
#ifdef _TARGET_XARCH_
        noway_assert(genRegMask(op2->gtRegNum) == op2RegMask);
#endif
        // Check for the case of op1 being spilled during the evaluation of op2
        if (op1->gtFlags & GTF_SPILLED)
        {
            // The register has been spilled -- reload it to any register except ECX
            regSet.rsLockUsedReg(op2RegMask);
            regSet.rsUnspillReg(op1, 0, RegSet::KEEP_REG);
            regSet.rsUnlockUsedReg(op2RegMask);
        }

        noway_assert(op1->InReg());
        reg = op1->gtRegNum;

#ifdef _TARGET_ARM_
        /* Perform the shift */
        getEmitter()->emitIns_R_R(ins, EA_4BYTE, reg, op2->gtRegNum, flags);
#else
        /* Perform the shift */
        inst_RV_CL(ins, reg);
#endif
        genReleaseReg(op2);
    }

    noway_assert(op1->InReg());
    noway_assert(reg == op1->gtRegNum);

    /* The register is now trashed */
    genReleaseReg(op1);
    regTracker.rsTrackRegTrash(reg);

    genCodeForTree_DONE(tree, reg);
}

/*****************************************************************************
 *
 *  Generate code for a top-level relational operator (not one that is part of a GT_JTRUE tree).
 *  Handles GT_EQ, GT_NE, GT_LT, GT_LE, GT_GE, GT_GT.
 */

void CodeGen::genCodeForRelop(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    assert(tree->OperGet() == GT_EQ || tree->OperGet() == GT_NE || tree->OperGet() == GT_LT ||
           tree->OperGet() == GT_LE || tree->OperGet() == GT_GE || tree->OperGet() == GT_GT);

    const genTreeOps oper     = tree->OperGet();
    GenTreePtr       op1      = tree->gtOp.gtOp1;
    const var_types  treeType = tree->TypeGet();
    regMaskTP        needReg  = destReg;
    regNumber        reg;

    // Longs and float comparisons are converted to "?:"
    noway_assert(!compiler->fgMorphRelopToQmark(op1));

    // Check if we can use the currently set flags. Else set them

    emitJumpKind jumpKind = genCondSetFlags(tree);

    // Grab a register to materialize the bool value into

    bestReg = regSet.rsRegMaskCanGrab() & RBM_BYTE_REGS;

    // Check that the predictor did the right job
    noway_assert(bestReg);

    // If needReg is in bestReg then use it
    if (needReg & bestReg)
        reg = regSet.rsGrabReg(needReg & bestReg);
    else
        reg = regSet.rsGrabReg(bestReg);

#if defined(_TARGET_ARM_)

    // Generate:
    //      jump-if-true L_true
    //      mov reg, 0
    //      jmp L_end
    //    L_true:
    //      mov reg, 1
    //    L_end:

    BasicBlock* L_true;
    BasicBlock* L_end;

    L_true = genCreateTempLabel();
    L_end  = genCreateTempLabel();

    inst_JMP(jumpKind, L_true);
    getEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, reg, 0); // Executes when the cond is false
    inst_JMP(EJ_jmp, L_end);
    genDefineTempLabel(L_true);
    getEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, reg, 1); // Executes when the cond is true
    genDefineTempLabel(L_end);

    regTracker.rsTrackRegTrash(reg);

#elif defined(_TARGET_XARCH_)
    regMaskTP regs = genRegMask(reg);
    noway_assert(regs & RBM_BYTE_REGS);

    // Set (lower byte of) reg according to the flags

    /* Look for the special case where just want to transfer the carry bit */

    if (jumpKind == EJ_jb)
    {
        inst_RV_RV(INS_SUBC, reg, reg);
        inst_RV(INS_NEG, reg, TYP_INT);
        regTracker.rsTrackRegTrash(reg);
    }
    else if (jumpKind == EJ_jae)
    {
        inst_RV_RV(INS_SUBC, reg, reg);
        genIncRegBy(reg, 1, tree, TYP_INT);
        regTracker.rsTrackRegTrash(reg);
    }
    else
    {
        inst_SET(jumpKind, reg);

        regTracker.rsTrackRegTrash(reg);

        if (treeType == TYP_INT)
        {
            // Set the higher bytes to 0
            inst_RV_RV(ins_Move_Extend(TYP_UBYTE, true), reg, reg, TYP_UBYTE, emitTypeSize(TYP_UBYTE));
        }
        else
        {
            noway_assert(treeType == TYP_BYTE);
        }
    }
#else
    NYI("TARGET");
#endif // _TARGET_XXX

    genCodeForTree_DONE(tree, reg);
}

//------------------------------------------------------------------------
// genCodeForCopyObj: Generate code for a CopyObj node
//
// Arguments:
//    tree    - The CopyObj node we are going to generate code for.
//    destReg - The register mask for register(s), if any, that will be defined.
//
// Return Value:
//    None

void CodeGen::genCodeForCopyObj(GenTreePtr tree, regMaskTP destReg)
{
    // If the value class doesn't have any fields that are GC refs or
    // the target isn't on the GC-heap, we can merge it with CPBLK.
    // GC fields cannot be copied directly, instead we will
    // need to use a jit-helper for that.
    assert(tree->gtOper == GT_ASG);
    assert(tree->gtOp.gtOp1->gtOper == GT_OBJ);

    GenTreeObj* cpObjOp = tree->gtOp.gtOp1->AsObj();
    assert(cpObjOp->HasGCPtr());

#ifdef _TARGET_ARM_
    if (cpObjOp->IsVolatile())
    {
        // Emit a memory barrier instruction before the CopyBlk
        instGen_MemoryBarrier();
    }
#endif
    assert(tree->gtOp.gtOp2->OperIsIndir());
    GenTreePtr srcObj = tree->gtOp.gtOp2->AsIndir()->Addr();
    GenTreePtr dstObj = cpObjOp->Addr();

    noway_assert(dstObj->gtType == TYP_BYREF || dstObj->gtType == TYP_I_IMPL);

#ifdef DEBUG
    CORINFO_CLASS_HANDLE clsHnd       = (CORINFO_CLASS_HANDLE)cpObjOp->gtClass;
    size_t               debugBlkSize = roundUp(compiler->info.compCompHnd->getClassSize(clsHnd), TARGET_POINTER_SIZE);

    // Since we round up, we are not handling the case where we have a non-pointer sized struct with GC pointers.
    // The EE currently does not allow this.  Let's assert it just to be safe.
    noway_assert(compiler->info.compCompHnd->getClassSize(clsHnd) == debugBlkSize);
#endif

    size_t   blkSize    = cpObjOp->gtSlots * TARGET_POINTER_SIZE;
    unsigned slots      = cpObjOp->gtSlots;
    BYTE*    gcPtrs     = cpObjOp->gtGcPtrs;
    unsigned gcPtrCount = cpObjOp->gtGcPtrCount;
    assert(blkSize == cpObjOp->gtBlkSize);

    GenTreePtr treeFirst, treeSecond;
    regNumber  regFirst, regSecond;

    // Check what order the object-ptrs have to be evaluated in ?

    if (tree->gtFlags & GTF_REVERSE_OPS)
    {
        treeFirst  = srcObj;
        treeSecond = dstObj;
#if CPU_USES_BLOCK_MOVE
        regFirst  = REG_ESI;
        regSecond = REG_EDI;
#else
        regFirst  = REG_ARG_1;
        regSecond = REG_ARG_0;
#endif
    }
    else
    {
        treeFirst  = dstObj;
        treeSecond = srcObj;
#if CPU_USES_BLOCK_MOVE
        regFirst  = REG_EDI;
        regSecond = REG_ESI;
#else
        regFirst  = REG_ARG_0;
        regSecond = REG_ARG_1;
#endif
    }

    bool     dstIsOnStack = (dstObj->gtOper == GT_ADDR && (dstObj->gtFlags & GTF_ADDR_ONSTACK));
    bool     srcIsOnStack = (srcObj->gtOper == GT_ADDR && (srcObj->gtFlags & GTF_ADDR_ONSTACK));
    emitAttr srcType      = (varTypeIsGC(srcObj) && !srcIsOnStack) ? EA_BYREF : EA_PTRSIZE;
    emitAttr dstType      = (varTypeIsGC(dstObj) && !dstIsOnStack) ? EA_BYREF : EA_PTRSIZE;

#if CPU_USES_BLOCK_MOVE
    // Materialize the trees in the order desired

    genComputeReg(treeFirst, genRegMask(regFirst), RegSet::EXACT_REG, RegSet::KEEP_REG, true);
    genComputeReg(treeSecond, genRegMask(regSecond), RegSet::EXACT_REG, RegSet::KEEP_REG, true);
    genRecoverReg(treeFirst, genRegMask(regFirst), RegSet::KEEP_REG);

    // Grab ECX because it will be trashed by the helper
    //
    regSet.rsGrabReg(RBM_ECX);

    while (blkSize >= TARGET_POINTER_SIZE)
    {
        if (*gcPtrs++ == TYPE_GC_NONE || dstIsOnStack)
        {
            // Note that we can use movsd even if it is a GC pointer being transfered
            // because the value is not cached anywhere.  If we did this in two moves,
            // we would have to make certain we passed the appropriate GC info on to
            // the emitter.
            instGen(INS_movsp);
        }
        else
        {
            // This helper will act like a MOVSD
            //    -- inputs EDI and ESI are byrefs
            //    -- including incrementing of ESI and EDI by 4
            //    -- helper will trash ECX
            //
            regMaskTP argRegs = genRegMask(regFirst) | genRegMask(regSecond);
            regSet.rsLockUsedReg(argRegs);
            genEmitHelperCall(CORINFO_HELP_ASSIGN_BYREF,
                              0,           // argSize
                              EA_PTRSIZE); // retSize
            regSet.rsUnlockUsedReg(argRegs);
        }

        blkSize -= TARGET_POINTER_SIZE;
    }

    // "movsd/movsq" as well as CPX_BYREF_ASG modify all three registers

    regTracker.rsTrackRegTrash(REG_EDI);
    regTracker.rsTrackRegTrash(REG_ESI);
    regTracker.rsTrackRegTrash(REG_ECX);

    gcInfo.gcMarkRegSetNpt(RBM_ESI | RBM_EDI);

    /* The emitter won't record CORINFO_HELP_ASSIGN_BYREF in the GC tables as
        it is a emitNoGChelper. However, we have to let the emitter know that
        the GC liveness has changed. We do this by creating a new label.
        */

    noway_assert(emitter::emitNoGChelper(CORINFO_HELP_ASSIGN_BYREF));

    genDefineTempLabel(&dummyBB);

#else //  !CPU_USES_BLOCK_MOVE

#ifndef _TARGET_ARM_
// Currently only the ARM implementation is provided
#error "COPYBLK for non-ARM && non-CPU_USES_BLOCK_MOVE"
#endif

    // Materialize the trees in the order desired
    bool      helperUsed;
    regNumber regDst;
    regNumber regSrc;
    regNumber regTemp;

    if ((gcPtrCount > 0) && !dstIsOnStack)
    {
        genComputeReg(treeFirst, genRegMask(regFirst), RegSet::EXACT_REG, RegSet::KEEP_REG, true);
        genComputeReg(treeSecond, genRegMask(regSecond), RegSet::EXACT_REG, RegSet::KEEP_REG, true);
        genRecoverReg(treeFirst, genRegMask(regFirst), RegSet::KEEP_REG);

        /* The helper is a Asm-routine that will trash R2,R3 and LR */
        {
            /* Spill any callee-saved registers which are being used */
            regMaskTP spillRegs = RBM_CALLEE_TRASH_NOGC & regSet.rsMaskUsed;

            if (spillRegs)
            {
                regSet.rsSpillRegs(spillRegs);
            }
        }

        // Grab R2 (aka REG_TMP_1) because it will be trashed by the helper
        // We will also use it as the temp register for our load/store sequences
        //
        assert(REG_R2 == REG_TMP_1);
        regTemp    = regSet.rsGrabReg(RBM_R2);
        helperUsed = true;
    }
    else
    {
        genCompIntoFreeReg(treeFirst, (RBM_ALLINT & ~treeSecond->gtRsvdRegs), RegSet::KEEP_REG);
        genCompIntoFreeReg(treeSecond, RBM_ALLINT, RegSet::KEEP_REG);
        genRecoverReg(treeFirst, RBM_ALLINT, RegSet::KEEP_REG);

        // Grab any temp register to use for our load/store sequences
        //
        regTemp    = regSet.rsGrabReg(RBM_ALLINT);
        helperUsed = false;
    }
    assert(dstObj->InReg());
    assert(srcObj->InReg());

    regDst = dstObj->gtRegNum;
    regSrc = srcObj->gtRegNum;

    assert(regDst != regTemp);
    assert(regSrc != regTemp);

    instruction loadIns  = ins_Load(TYP_I_IMPL);  // INS_ldr
    instruction storeIns = ins_Store(TYP_I_IMPL); // INS_str

    size_t offset = 0;
    while (blkSize >= TARGET_POINTER_SIZE)
    {
        CorInfoGCType gcType;
        CorInfoGCType gcTypeNext = TYPE_GC_NONE;
        var_types     type       = TYP_I_IMPL;

#if FEATURE_WRITE_BARRIER
        gcType                   = (CorInfoGCType)(*gcPtrs++);
        if (blkSize > TARGET_POINTER_SIZE)
            gcTypeNext = (CorInfoGCType)(*gcPtrs);

        if (gcType == TYPE_GC_REF)
            type = TYP_REF;
        else if (gcType == TYPE_GC_BYREF)
            type = TYP_BYREF;

        if (helperUsed)
        {
            assert(regDst == REG_ARG_0);
            assert(regSrc == REG_ARG_1);
            assert(regTemp == REG_R2);
        }
#else
        gcType = TYPE_GC_NONE;
#endif // FEATURE_WRITE_BARRIER

        blkSize -= TARGET_POINTER_SIZE;

        emitAttr opSize = emitTypeSize(type);

        if (!helperUsed || (gcType == TYPE_GC_NONE))
        {
            getEmitter()->emitIns_R_R_I(loadIns, opSize, regTemp, regSrc, offset);
            getEmitter()->emitIns_R_R_I(storeIns, opSize, regTemp, regDst, offset);
            offset += TARGET_POINTER_SIZE;

            if ((helperUsed && (gcTypeNext != TYPE_GC_NONE)) || ((offset >= 128) && (blkSize > 0)))
            {
                getEmitter()->emitIns_R_I(INS_add, srcType, regSrc, offset);
                getEmitter()->emitIns_R_I(INS_add, dstType, regDst, offset);
                offset = 0;
            }
        }
        else
        {
            assert(offset == 0);

            // The helper will act like this:
            //    -- inputs R0 and R1 are byrefs
            //    -- helper will perform copy from *R1 into *R0
            //    -- helper will perform post increment of R0 and R1 by 4
            //    -- helper will trash R2
            //    -- helper will trash R3
            //    -- calling the helper implicitly trashes LR
            //
            assert(helperUsed);
            regMaskTP argRegs = genRegMask(regFirst) | genRegMask(regSecond);
            regSet.rsLockUsedReg(argRegs);
            genEmitHelperCall(CORINFO_HELP_ASSIGN_BYREF,
                              0,           // argSize
                              EA_PTRSIZE); // retSize

            regSet.rsUnlockUsedReg(argRegs);
            regTracker.rsTrackRegMaskTrash(RBM_CALLEE_TRASH_NOGC);
        }
    }

    regTracker.rsTrackRegTrash(regDst);
    regTracker.rsTrackRegTrash(regSrc);
    regTracker.rsTrackRegTrash(regTemp);

    gcInfo.gcMarkRegSetNpt(genRegMask(regDst) | genRegMask(regSrc));

    /* The emitter won't record CORINFO_HELP_ASSIGN_BYREF in the GC tables as
        it is a emitNoGChelper. However, we have to let the emitter know that
        the GC liveness has changed. We do this by creating a new label.
        */

    noway_assert(emitter::emitNoGChelper(CORINFO_HELP_ASSIGN_BYREF));

    genDefineTempLabel(&dummyBB);

#endif //  !CPU_USES_BLOCK_MOVE

    assert(blkSize == 0);

    genReleaseReg(dstObj);
    genReleaseReg(srcObj);

    genCodeForTree_DONE(tree, REG_NA);

#ifdef _TARGET_ARM_
    if (cpObjOp->IsVolatile())
    {
        // Emit a memory barrier instruction after the CopyBlk
        instGen_MemoryBarrier();
    }
#endif
}

//------------------------------------------------------------------------
// genCodeForBlkOp: Generate code for a block copy or init operation
//
// Arguments:
//    tree    - The block assignment
//    destReg - The expected destination register
//
void CodeGen::genCodeForBlkOp(GenTreePtr tree, regMaskTP destReg)
{
    genTreeOps oper    = tree->OperGet();
    GenTreePtr dest    = tree->gtOp.gtOp1;
    GenTreePtr src     = tree->gtGetOp2();
    regMaskTP  needReg = destReg;
    regMaskTP  regs    = regSet.rsMaskUsed;
    GenTreePtr opsPtr[3];
    regMaskTP  regsPtr[3];
    GenTreePtr destPtr;
    GenTreePtr srcPtrOrVal;

    noway_assert(tree->OperIsBlkOp());

    bool       isCopyBlk    = false;
    bool       isInitBlk    = false;
    bool       hasGCpointer = false;
    unsigned   blockSize    = dest->AsBlk()->gtBlkSize;
    GenTreePtr sizeNode     = nullptr;
    bool       sizeIsConst  = true;
    if (dest->gtOper == GT_DYN_BLK)
    {
        sizeNode    = dest->AsDynBlk()->gtDynamicSize;
        sizeIsConst = false;
    }

    if (tree->OperIsCopyBlkOp())
    {
        isCopyBlk = true;
        if (dest->gtOper == GT_OBJ)
        {
            if (dest->AsObj()->gtGcPtrCount != 0)
            {
                genCodeForCopyObj(tree, destReg);
                return;
            }
        }
    }
    else
    {
        isInitBlk = true;
    }

    // Ensure that we have an address in the CopyBlk case.
    if (isCopyBlk)
    {
        // TODO-1stClassStructs: Allow a lclVar here.
        assert(src->OperIsIndir());
        srcPtrOrVal = src->AsIndir()->Addr();
    }
    else
    {
        srcPtrOrVal = src;
    }

#ifdef _TARGET_ARM_
    if (dest->AsBlk()->IsVolatile())
    {
        // Emit a memory barrier instruction before the InitBlk/CopyBlk
        instGen_MemoryBarrier();
    }
#endif
    {
        destPtr = dest->AsBlk()->Addr();
        noway_assert(destPtr->TypeGet() == TYP_BYREF || varTypeIsIntegral(destPtr->TypeGet()));
        noway_assert(
            (isCopyBlk && (srcPtrOrVal->TypeGet() == TYP_BYREF || varTypeIsIntegral(srcPtrOrVal->TypeGet()))) ||
            (isInitBlk && varTypeIsIntegral(srcPtrOrVal->TypeGet())));

        noway_assert(destPtr && srcPtrOrVal);

#if CPU_USES_BLOCK_MOVE
        regs = isInitBlk ? RBM_EAX : RBM_ESI; // What is the needReg for Val/Src

        /* Some special code for block moves/inits for constant sizes */

        //
        // Is this a fixed size COPYBLK?
        //      or a fixed size INITBLK with a constant init value?
        //
        if ((sizeIsConst) && (isCopyBlk || (srcPtrOrVal->IsCnsIntOrI())))
        {
            size_t      length  = blockSize;
            size_t      initVal = 0;
            instruction ins_P, ins_PR, ins_B;

            if (isInitBlk)
            {
                ins_P  = INS_stosp;
                ins_PR = INS_r_stosp;
                ins_B  = INS_stosb;

                /* Properly extend the init constant from a U1 to a U4 */
                initVal = 0xFF & ((unsigned)srcPtrOrVal->gtIntCon.gtIconVal);

                /* If it is a non-zero value we have to replicate      */
                /* the byte value four times to form the DWORD         */
                /* Then we change this new value into the tree-node      */

                if (initVal)
                {
                    initVal = initVal | (initVal << 8) | (initVal << 16) | (initVal << 24);
#ifdef _TARGET_64BIT_
                    if (length > 4)
                    {
                        initVal             = initVal | (initVal << 32);
                        srcPtrOrVal->gtType = TYP_LONG;
                    }
                    else
                    {
                        srcPtrOrVal->gtType = TYP_INT;
                    }
#endif // _TARGET_64BIT_
                }
                srcPtrOrVal->gtIntCon.gtIconVal = initVal;
            }
            else
            {
                ins_P  = INS_movsp;
                ins_PR = INS_r_movsp;
                ins_B  = INS_movsb;
            }

            // Determine if we will be using SSE2
            unsigned movqLenMin = 8;
            unsigned movqLenMax = 24;

            bool bWillUseSSE2      = false;
            bool bWillUseOnlySSE2  = false;
            bool bNeedEvaluateCnst = true; // If we only use SSE2, we will just load the constant there.

#ifdef _TARGET_64BIT_

// Until we get SSE2 instructions that move 16 bytes at a time instead of just 8
// there is no point in wasting space on the bigger instructions

#else // !_TARGET_64BIT_

            if (compiler->opts.compCanUseSSE2)
            {
                unsigned curBBweight = compiler->compCurBB->getBBWeight(compiler);

                /* Adjust for BB weight */
                if (curBBweight == BB_ZERO_WEIGHT)
                {
                    // Don't bother with this optimization in
                    // rarely run blocks
                    movqLenMax = movqLenMin = 0;
                }
                else if (curBBweight < BB_UNITY_WEIGHT)
                {
                    // Be less aggressive when we are inside a conditional
                    movqLenMax = 16;
                }
                else if (curBBweight >= (BB_LOOP_WEIGHT * BB_UNITY_WEIGHT) / 2)
                {
                    // Be more aggressive when we are inside a loop
                    movqLenMax = 48;
                }

                if ((compiler->compCodeOpt() == Compiler::FAST_CODE) || isInitBlk)
                {
                    // Be more aggressive when optimizing for speed
                    // InitBlk uses fewer instructions
                    movqLenMax += 16;
                }

                if (compiler->compCodeOpt() != Compiler::SMALL_CODE && length >= movqLenMin && length <= movqLenMax)
                {
                    bWillUseSSE2 = true;

                    if ((length % 8) == 0)
                    {
                        bWillUseOnlySSE2 = true;
                        if (isInitBlk && (initVal == 0))
                        {
                            bNeedEvaluateCnst = false;
                            noway_assert((srcPtrOrVal->OperGet() == GT_CNS_INT));
                        }
                    }
                }
            }

#endif // !_TARGET_64BIT_

            const bool bWillTrashRegSrc = (isCopyBlk && !bWillUseOnlySSE2);
            /* Evaluate dest and src/val */

            if (tree->gtFlags & GTF_REVERSE_OPS)
            {
                if (bNeedEvaluateCnst)
                {
                    genComputeReg(srcPtrOrVal, regs, RegSet::EXACT_REG, RegSet::KEEP_REG, bWillTrashRegSrc);
                }
                genComputeReg(destPtr, RBM_EDI, RegSet::EXACT_REG, RegSet::KEEP_REG, !bWillUseOnlySSE2);
                if (bNeedEvaluateCnst)
                {
                    genRecoverReg(srcPtrOrVal, regs, RegSet::KEEP_REG);
                }
            }
            else
            {
                genComputeReg(destPtr, RBM_EDI, RegSet::EXACT_REG, RegSet::KEEP_REG, !bWillUseOnlySSE2);
                if (bNeedEvaluateCnst)
                {
                    genComputeReg(srcPtrOrVal, regs, RegSet::EXACT_REG, RegSet::KEEP_REG, bWillTrashRegSrc);
                }
                genRecoverReg(destPtr, RBM_EDI, RegSet::KEEP_REG);
            }

            bool bTrashedESI = false;
            bool bTrashedEDI = false;

            if (bWillUseSSE2)
            {
                int       blkDisp = 0;
                regNumber xmmReg  = REG_XMM0;

                if (isInitBlk)
                {
                    if (initVal)
                    {
                        getEmitter()->emitIns_R_R(INS_mov_i2xmm, EA_4BYTE, xmmReg, REG_EAX);
                        getEmitter()->emitIns_R_R(INS_punpckldq, EA_4BYTE, xmmReg, xmmReg);
                    }
                    else
                    {
                        getEmitter()->emitIns_R_R(INS_xorps, EA_8BYTE, xmmReg, xmmReg);
                    }
                }

                JITLOG_THIS(compiler, (LL_INFO100, "Using XMM instructions for %3d byte %s while compiling %s\n",
                                       length, isInitBlk ? "initblk" : "copyblk", compiler->info.compFullName));

                while (length > 7)
                {
                    if (isInitBlk)
                    {
                        getEmitter()->emitIns_AR_R(INS_movq, EA_8BYTE, xmmReg, REG_EDI, blkDisp);
                    }
                    else
                    {
                        getEmitter()->emitIns_R_AR(INS_movq, EA_8BYTE, xmmReg, REG_ESI, blkDisp);
                        getEmitter()->emitIns_AR_R(INS_movq, EA_8BYTE, xmmReg, REG_EDI, blkDisp);
                    }
                    blkDisp += 8;
                    length -= 8;
                }

                if (length > 0)
                {
                    noway_assert(bNeedEvaluateCnst);
                    noway_assert(!bWillUseOnlySSE2);

                    if (isCopyBlk)
                    {
                        inst_RV_IV(INS_add, REG_ESI, blkDisp, emitActualTypeSize(srcPtrOrVal->TypeGet()));
                        bTrashedESI = true;
                    }

                    inst_RV_IV(INS_add, REG_EDI, blkDisp, emitActualTypeSize(destPtr->TypeGet()));
                    bTrashedEDI = true;

                    if (length >= REGSIZE_BYTES)
                    {
                        instGen(ins_P);
                        length -= REGSIZE_BYTES;
                    }
                }
            }
            else if (compiler->compCodeOpt() == Compiler::SMALL_CODE)
            {
                /* For small code, we can only use ins_DR to generate fast
                    and small code. We also can't use "rep movsb" because
                    we may not atomically reading and writing the DWORD */

                noway_assert(bNeedEvaluateCnst);

                goto USE_DR;
            }
            else if (length <= 4 * REGSIZE_BYTES)
            {
                noway_assert(bNeedEvaluateCnst);

                while (length >= REGSIZE_BYTES)
                {
                    instGen(ins_P);
                    length -= REGSIZE_BYTES;
                }

                bTrashedEDI = true;
                if (isCopyBlk)
                    bTrashedESI = true;
            }
            else
            {
            USE_DR:
                noway_assert(bNeedEvaluateCnst);

                /* set ECX to length/REGSIZE_BYTES (in pointer-sized words) */
                genSetRegToIcon(REG_ECX, length / REGSIZE_BYTES, TYP_I_IMPL);

                length &= (REGSIZE_BYTES - 1);

                instGen(ins_PR);

                regTracker.rsTrackRegTrash(REG_ECX);

                bTrashedEDI = true;
                if (isCopyBlk)
                    bTrashedESI = true;
            }

            /* Now take care of the remainder */
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_64BIT_
            if (length > 4)
            {
                noway_assert(bNeedEvaluateCnst);
                noway_assert(length < 8);

                instGen((isInitBlk) ? INS_stosd : INS_movsd);
                length -= 4;

                bTrashedEDI = true;
                if (isCopyBlk)
                    bTrashedESI = true;
            }

#endif // _TARGET_64BIT_

            if (length)
            {
                noway_assert(bNeedEvaluateCnst);

                while (length--)
                {
                    instGen(ins_B);
                }

                bTrashedEDI = true;
                if (isCopyBlk)
                    bTrashedESI = true;
            }

            noway_assert(bTrashedEDI == !bWillUseOnlySSE2);
            if (bTrashedEDI)
                regTracker.rsTrackRegTrash(REG_EDI);
            if (bTrashedESI)
                regTracker.rsTrackRegTrash(REG_ESI);
            // else No need to trash EAX as it wasnt destroyed by the "rep stos"

            genReleaseReg(destPtr);
            if (bNeedEvaluateCnst)
                genReleaseReg(srcPtrOrVal);
        }
        else
        {
            //
            // This a variable-sized COPYBLK/INITBLK,
            //   or a fixed size INITBLK with a variable init value,
            //

            // What order should the Dest, Val/Src, and Size be calculated

            compiler->fgOrderBlockOps(tree, RBM_EDI, regs, RBM_ECX, opsPtr, regsPtr); // OUT arguments

            noway_assert((isInitBlk && (regs == RBM_EAX)) || (isCopyBlk && (regs == RBM_ESI)));
            genComputeReg(opsPtr[0], regsPtr[0], RegSet::EXACT_REG, RegSet::KEEP_REG, (regsPtr[0] != RBM_EAX));
            genComputeReg(opsPtr[1], regsPtr[1], RegSet::EXACT_REG, RegSet::KEEP_REG, (regsPtr[1] != RBM_EAX));
            if (opsPtr[2] != nullptr)
            {
                genComputeReg(opsPtr[2], regsPtr[2], RegSet::EXACT_REG, RegSet::KEEP_REG, (regsPtr[2] != RBM_EAX));
            }
            genRecoverReg(opsPtr[0], regsPtr[0], RegSet::KEEP_REG);
            genRecoverReg(opsPtr[1], regsPtr[1], RegSet::KEEP_REG);

            noway_assert((destPtr->InReg()) && // Dest
                         (destPtr->gtRegNum == REG_EDI));

            noway_assert((srcPtrOrVal->InReg()) && // Val/Src
                         (genRegMask(srcPtrOrVal->gtRegNum) == regs));

            if (sizeIsConst)
            {
                inst_RV_IV(INS_mov, REG_ECX, blockSize, EA_PTRSIZE);
            }
            else
            {
                noway_assert((sizeNode->InReg()) && // Size
                             (sizeNode->gtRegNum == REG_ECX));
            }

            if (isInitBlk)
                instGen(INS_r_stosb);
            else
                instGen(INS_r_movsb);

            regTracker.rsTrackRegTrash(REG_EDI);
            regTracker.rsTrackRegTrash(REG_ECX);

            if (isCopyBlk)
                regTracker.rsTrackRegTrash(REG_ESI);
            // else No need to trash EAX as it wasnt destroyed by the "rep stos"

            genReleaseReg(opsPtr[0]);
            genReleaseReg(opsPtr[1]);
            if (opsPtr[2] != nullptr)
            {
                genReleaseReg(opsPtr[2]);
            }
        }

#else // !CPU_USES_BLOCK_MOVE

#ifndef _TARGET_ARM_
// Currently only the ARM implementation is provided
#error "COPYBLK/INITBLK non-ARM && non-CPU_USES_BLOCK_MOVE"
#endif
        //
        // Is this a fixed size COPYBLK?
        //      or a fixed size INITBLK with a constant init value?
        //
        if (sizeIsConst && (isCopyBlk || (srcPtrOrVal->OperGet() == GT_CNS_INT)))
        {
            GenTreePtr dstOp          = destPtr;
            GenTreePtr srcOp          = srcPtrOrVal;
            unsigned   length         = blockSize;
            unsigned   fullStoreCount = length / TARGET_POINTER_SIZE;
            unsigned   initVal        = 0;
            bool       useLoop        = false;

            if (isInitBlk)
            {
                /* Properly extend the init constant from a U1 to a U4 */
                initVal = 0xFF & ((unsigned)srcOp->gtIntCon.gtIconVal);

                /* If it is a non-zero value we have to replicate      */
                /* the byte value four times to form the DWORD         */
                /* Then we store this new value into the tree-node      */

                if (initVal != 0)
                {
                    initVal                         = initVal | (initVal << 8) | (initVal << 16) | (initVal << 24);
                    srcPtrOrVal->gtIntCon.gtIconVal = initVal;
                }
            }

            // Will we be using a loop to implement this INITBLK/COPYBLK?
            if ((isCopyBlk && (fullStoreCount >= 8)) || (isInitBlk && (fullStoreCount >= 16)))
            {
                useLoop = true;
            }

            regMaskTP usedRegs;
            regNumber regDst;
            regNumber regSrc;
            regNumber regTemp;

            /* Evaluate dest and src/val */

            if (tree->gtFlags & GTF_REVERSE_OPS)
            {
                genComputeReg(srcOp, (needReg & ~dstOp->gtRsvdRegs), RegSet::ANY_REG, RegSet::KEEP_REG, useLoop);
                assert(srcOp->InReg());

                genComputeReg(dstOp, needReg, RegSet::ANY_REG, RegSet::KEEP_REG, useLoop);
                assert(dstOp->InReg());
                regDst = dstOp->gtRegNum;

                genRecoverReg(srcOp, needReg, RegSet::KEEP_REG);
                regSrc = srcOp->gtRegNum;
            }
            else
            {
                genComputeReg(dstOp, (needReg & ~srcOp->gtRsvdRegs), RegSet::ANY_REG, RegSet::KEEP_REG, useLoop);
                assert(dstOp->InReg());

                genComputeReg(srcOp, needReg, RegSet::ANY_REG, RegSet::KEEP_REG, useLoop);
                assert(srcOp->InReg());
                regSrc = srcOp->gtRegNum;

                genRecoverReg(dstOp, needReg, RegSet::KEEP_REG);
                regDst = dstOp->gtRegNum;
            }
            assert(dstOp->InReg());
            assert(srcOp->InReg());

            regDst                = dstOp->gtRegNum;
            regSrc                = srcOp->gtRegNum;
            usedRegs              = (genRegMask(regSrc) | genRegMask(regDst));
            bool     dstIsOnStack = (dstOp->gtOper == GT_ADDR && (dstOp->gtFlags & GTF_ADDR_ONSTACK));
            emitAttr dstType      = (varTypeIsGC(dstOp) && !dstIsOnStack) ? EA_BYREF : EA_PTRSIZE;
            emitAttr srcType;

            if (isCopyBlk)
            {
                // Prefer a low register,but avoid one of the ones we've already grabbed
                regTemp = regSet.rsGrabReg(regSet.rsNarrowHint(regSet.rsRegMaskCanGrab() & ~usedRegs, RBM_LOW_REGS));
                usedRegs |= genRegMask(regTemp);
                bool srcIsOnStack = (srcOp->gtOper == GT_ADDR && (srcOp->gtFlags & GTF_ADDR_ONSTACK));
                srcType           = (varTypeIsGC(srcOp) && !srcIsOnStack) ? EA_BYREF : EA_PTRSIZE;
            }
            else
            {
                regTemp = REG_STK;
                srcType = EA_PTRSIZE;
            }

            instruction loadIns  = ins_Load(TYP_I_IMPL);  // INS_ldr
            instruction storeIns = ins_Store(TYP_I_IMPL); // INS_str

            int finalOffset;

            // Can we emit a small number of ldr/str instructions to implement this INITBLK/COPYBLK?
            if (!useLoop)
            {
                for (unsigned i = 0; i < fullStoreCount; i++)
                {
                    if (isCopyBlk)
                    {
                        getEmitter()->emitIns_R_R_I(loadIns, EA_4BYTE, regTemp, regSrc, i * TARGET_POINTER_SIZE);
                        getEmitter()->emitIns_R_R_I(storeIns, EA_4BYTE, regTemp, regDst, i * TARGET_POINTER_SIZE);
                        gcInfo.gcMarkRegSetNpt(genRegMask(regTemp));
                        regTracker.rsTrackRegTrash(regTemp);
                    }
                    else
                    {
                        getEmitter()->emitIns_R_R_I(storeIns, EA_4BYTE, regSrc, regDst, i * TARGET_POINTER_SIZE);
                    }
                }

                finalOffset = fullStoreCount * TARGET_POINTER_SIZE;
                length -= finalOffset;
            }
            else // We will use a loop to implement this INITBLK/COPYBLK
            {
                unsigned pairStoreLoopCount = fullStoreCount / 2;

                // We need a second temp register for CopyBlk
                regNumber regTemp2 = REG_STK;
                if (isCopyBlk)
                {
                    // Prefer a low register, but avoid one of the ones we've already grabbed
                    regTemp2 =
                        regSet.rsGrabReg(regSet.rsNarrowHint(regSet.rsRegMaskCanGrab() & ~usedRegs, RBM_LOW_REGS));
                    usedRegs |= genRegMask(regTemp2);
                }

                // Pick and initialize the loop counter register
                regNumber regLoopIndex;
                regLoopIndex =
                    regSet.rsGrabReg(regSet.rsNarrowHint(regSet.rsRegMaskCanGrab() & ~usedRegs, RBM_LOW_REGS));
                genSetRegToIcon(regLoopIndex, pairStoreLoopCount, TYP_INT);

                // Create and define the Basic Block for the loop top
                BasicBlock* loopTopBlock = genCreateTempLabel();
                genDefineTempLabel(loopTopBlock);

                // The loop body
                if (isCopyBlk)
                {
                    getEmitter()->emitIns_R_R_I(loadIns, EA_4BYTE, regTemp, regSrc, 0);
                    getEmitter()->emitIns_R_R_I(loadIns, EA_4BYTE, regTemp2, regSrc, TARGET_POINTER_SIZE);
                    getEmitter()->emitIns_R_R_I(storeIns, EA_4BYTE, regTemp, regDst, 0);
                    getEmitter()->emitIns_R_R_I(storeIns, EA_4BYTE, regTemp2, regDst, TARGET_POINTER_SIZE);
                    getEmitter()->emitIns_R_I(INS_add, srcType, regSrc, 2 * TARGET_POINTER_SIZE);
                    gcInfo.gcMarkRegSetNpt(genRegMask(regTemp));
                    gcInfo.gcMarkRegSetNpt(genRegMask(regTemp2));
                    regTracker.rsTrackRegTrash(regSrc);
                    regTracker.rsTrackRegTrash(regTemp);
                    regTracker.rsTrackRegTrash(regTemp2);
                }
                else // isInitBlk
                {
                    getEmitter()->emitIns_R_R_I(storeIns, EA_4BYTE, regSrc, regDst, 0);
                    getEmitter()->emitIns_R_R_I(storeIns, EA_4BYTE, regSrc, regDst, TARGET_POINTER_SIZE);
                }

                getEmitter()->emitIns_R_I(INS_add, dstType, regDst, 2 * TARGET_POINTER_SIZE);
                regTracker.rsTrackRegTrash(regDst);
                getEmitter()->emitIns_R_I(INS_sub, EA_4BYTE, regLoopIndex, 1, INS_FLAGS_SET);
                emitJumpKind jmpGTS = genJumpKindForOper(GT_GT, CK_SIGNED);
                inst_JMP(jmpGTS, loopTopBlock);

                regTracker.rsTrackRegIntCns(regLoopIndex, 0);

                length -= (pairStoreLoopCount * (2 * TARGET_POINTER_SIZE));

                if (length & TARGET_POINTER_SIZE)
                {
                    if (isCopyBlk)
                    {
                        getEmitter()->emitIns_R_R_I(loadIns, EA_4BYTE, regTemp, regSrc, 0);
                        getEmitter()->emitIns_R_R_I(storeIns, EA_4BYTE, regTemp, regDst, 0);
                    }
                    else
                    {
                        getEmitter()->emitIns_R_R_I(storeIns, EA_4BYTE, regSrc, regDst, 0);
                    }
                    finalOffset = TARGET_POINTER_SIZE;
                    length -= TARGET_POINTER_SIZE;
                }
                else
                {
                    finalOffset = 0;
                }
            }

            if (length & sizeof(short))
            {
                loadIns  = ins_Load(TYP_USHORT);  // INS_ldrh
                storeIns = ins_Store(TYP_USHORT); // INS_strh

                if (isCopyBlk)
                {
                    getEmitter()->emitIns_R_R_I(loadIns, EA_2BYTE, regTemp, regSrc, finalOffset);
                    getEmitter()->emitIns_R_R_I(storeIns, EA_2BYTE, regTemp, regDst, finalOffset);
                    gcInfo.gcMarkRegSetNpt(genRegMask(regTemp));
                    regTracker.rsTrackRegTrash(regTemp);
                }
                else
                {
                    getEmitter()->emitIns_R_R_I(storeIns, EA_2BYTE, regSrc, regDst, finalOffset);
                }
                length -= sizeof(short);
                finalOffset += sizeof(short);
            }

            if (length & sizeof(char))
            {
                loadIns  = ins_Load(TYP_UBYTE);  // INS_ldrb
                storeIns = ins_Store(TYP_UBYTE); // INS_strb

                if (isCopyBlk)
                {
                    getEmitter()->emitIns_R_R_I(loadIns, EA_1BYTE, regTemp, regSrc, finalOffset);
                    getEmitter()->emitIns_R_R_I(storeIns, EA_1BYTE, regTemp, regDst, finalOffset);
                    gcInfo.gcMarkRegSetNpt(genRegMask(regTemp));
                    regTracker.rsTrackRegTrash(regTemp);
                }
                else
                {
                    getEmitter()->emitIns_R_R_I(storeIns, EA_1BYTE, regSrc, regDst, finalOffset);
                }
                length -= sizeof(char);
            }
            assert(length == 0);

            genReleaseReg(dstOp);
            genReleaseReg(srcOp);
        }
        else
        {
            //
            // This a variable-sized COPYBLK/INITBLK,
            //   or a fixed size INITBLK with a variable init value,
            //

            // What order should the Dest, Val/Src, and Size be calculated

            compiler->fgOrderBlockOps(tree, RBM_ARG_0, RBM_ARG_1, RBM_ARG_2, opsPtr, regsPtr); // OUT arguments

            genComputeReg(opsPtr[0], regsPtr[0], RegSet::EXACT_REG, RegSet::KEEP_REG);
            genComputeReg(opsPtr[1], regsPtr[1], RegSet::EXACT_REG, RegSet::KEEP_REG);
            if (opsPtr[2] != nullptr)
            {
                genComputeReg(opsPtr[2], regsPtr[2], RegSet::EXACT_REG, RegSet::KEEP_REG);
            }
            genRecoverReg(opsPtr[0], regsPtr[0], RegSet::KEEP_REG);
            genRecoverReg(opsPtr[1], regsPtr[1], RegSet::KEEP_REG);

            noway_assert((destPtr->InReg()) && // Dest
                         (destPtr->gtRegNum == REG_ARG_0));

            noway_assert((srcPtrOrVal->InReg()) && // Val/Src
                         (srcPtrOrVal->gtRegNum == REG_ARG_1));

            if (sizeIsConst)
            {
                inst_RV_IV(INS_mov, REG_ARG_2, blockSize, EA_PTRSIZE);
            }
            else
            {
                noway_assert((sizeNode->InReg()) && // Size
                             (sizeNode->gtRegNum == REG_ARG_2));
            }

            regSet.rsLockUsedReg(RBM_ARG_0 | RBM_ARG_1 | RBM_ARG_2);

            genEmitHelperCall(isCopyBlk ? CORINFO_HELP_MEMCPY
                                        /* GT_INITBLK */
                                        : CORINFO_HELP_MEMSET,
                              0, EA_UNKNOWN);

            regTracker.rsTrackRegMaskTrash(RBM_CALLEE_TRASH);

            regSet.rsUnlockUsedReg(RBM_ARG_0 | RBM_ARG_1 | RBM_ARG_2);
            genReleaseReg(opsPtr[0]);
            genReleaseReg(opsPtr[1]);
            if (opsPtr[2] != nullptr)
            {
                genReleaseReg(opsPtr[2]);
            }
        }

        if (isCopyBlk && dest->AsBlk()->IsVolatile())
        {
            // Emit a memory barrier instruction after the CopyBlk
            instGen_MemoryBarrier();
        }
#endif // !CPU_USES_BLOCK_MOVE
    }
}
BasicBlock dummyBB;

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
void CodeGen::genCodeForTreeSmpOp(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    const genTreeOps oper     = tree->OperGet();
    const var_types  treeType = tree->TypeGet();
    GenTreePtr       op1      = tree->gtOp.gtOp1;
    GenTreePtr       op2      = tree->gtGetOp2IfPresent();
    regNumber        reg      = DUMMY_INIT(REG_CORRUPT);
    regMaskTP        regs     = regSet.rsMaskUsed;
    regMaskTP        needReg  = destReg;
    insFlags         flags    = tree->gtSetFlags() ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
    emitAttr         size;
    instruction      ins;
    regMaskTP        addrReg;
    GenTreePtr       opsPtr[3];
    regMaskTP        regsPtr[3];

#ifdef DEBUG
    addrReg = 0xDEADCAFE;
#endif

    noway_assert(tree->OperKind() & GTK_SMPOP);

    switch (oper)
    {
        case GT_ASG:
            if (tree->OperIsBlkOp() && op1->gtOper != GT_LCL_VAR)
            {
                genCodeForBlkOp(tree, destReg);
            }
            else
            {
                genCodeForTreeSmpOpAsg(tree);
            }
            return;

        case GT_ASG_LSH:
        case GT_ASG_RSH:
        case GT_ASG_RSZ:
            genCodeForAsgShift(tree, destReg, bestReg);
            return;

        case GT_ASG_AND:
        case GT_ASG_OR:
        case GT_ASG_XOR:
        case GT_ASG_ADD:
        case GT_ASG_SUB:
            genCodeForTreeSmpBinArithLogAsgOp(tree, destReg, bestReg);
            return;

        case GT_CHS:
            addrReg = genMakeAddressable(op1, 0, RegSet::KEEP_REG, true);
#ifdef _TARGET_XARCH_
            // Note that the specialCase here occurs when the treeType specifies a byte sized operation
            // and we decided to enregister the op1 LclVar in a non-byteable register (ESI or EDI)
            //
            bool specialCase;
            specialCase = false;
            if (op1->gtOper == GT_REG_VAR)
            {
                /* Get hold of the target register */

                reg = op1->gtRegVar.gtRegNum;
                if (varTypeIsByte(treeType) && !(genRegMask(reg) & RBM_BYTE_REGS))
                {
                    regNumber byteReg = regSet.rsGrabReg(RBM_BYTE_REGS);

                    inst_RV_RV(INS_mov, byteReg, reg);
                    regTracker.rsTrackRegTrash(byteReg);

                    inst_RV(INS_NEG, byteReg, treeType, emitTypeSize(treeType));
                    var_types   op1Type     = op1->TypeGet();
                    instruction wideningIns = ins_Move_Extend(op1Type, true);
                    inst_RV_RV(wideningIns, reg, byteReg, op1Type, emitTypeSize(op1Type));
                    regTracker.rsTrackRegTrash(reg);
                    specialCase = true;
                }
            }

            if (!specialCase)
            {
                inst_TT(INS_NEG, op1, 0, 0, emitTypeSize(treeType));
            }
#else // not  _TARGET_XARCH_
            if (op1->InReg())
            {
                inst_TT_IV(INS_NEG, op1, 0, 0, emitTypeSize(treeType), flags);
            }
            else
            {
                // Fix 388382 ARM JitStress WP7
                var_types op1Type = op1->TypeGet();
                regNumber reg     = regSet.rsPickFreeReg();
                inst_RV_TT(ins_Load(op1Type), reg, op1, 0, emitTypeSize(op1Type));
                regTracker.rsTrackRegTrash(reg);
                inst_RV_IV(INS_NEG, reg, 0, emitTypeSize(treeType), flags);
                inst_TT_RV(ins_Store(op1Type), op1, reg, 0, emitTypeSize(op1Type));
            }
#endif
            if (op1->InReg())
                regTracker.rsTrackRegTrash(op1->gtRegNum);
            genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);

            genCodeForTreeSmpOpAsg_DONE_ASSG(tree, addrReg, tree->gtRegNum, /* ovfl */ false);
            return;

        case GT_AND:
        case GT_OR:
        case GT_XOR:
        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
            genCodeForTreeSmpBinArithLogOp(tree, destReg, bestReg);
            return;

        case GT_UMOD:
            genCodeForUnsignedMod(tree, destReg, bestReg);
            return;

        case GT_MOD:
            genCodeForSignedMod(tree, destReg, bestReg);
            return;

        case GT_UDIV:
            genCodeForUnsignedDiv(tree, destReg, bestReg);
            return;

        case GT_DIV:
            genCodeForSignedDiv(tree, destReg, bestReg);
            return;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
            genCodeForShift(tree, destReg, bestReg);
            return;

        case GT_NEG:
        case GT_NOT:

            /* Generate the operand into some register */

            genCompIntoFreeReg(op1, needReg, RegSet::FREE_REG);
            noway_assert(op1->InReg());

            reg = op1->gtRegNum;

            /* Negate/reverse the value in the register */

            inst_RV((oper == GT_NEG) ? INS_NEG : INS_NOT, reg, treeType);

            /* The register is now trashed */

            regTracker.rsTrackRegTrash(reg);

            genCodeForTree_DONE(tree, reg);
            return;

        case GT_IND:
        case GT_NULLCHECK: // At this point, explicit null checks are just like inds...

            /* Make sure the operand is addressable */

            addrReg = genMakeAddressable(tree, RBM_ALLINT, RegSet::KEEP_REG, true);

            genDoneAddressable(tree, addrReg, RegSet::KEEP_REG);

            /* Figure out the size of the value being loaded */

            size = EA_ATTR(genTypeSize(tree->gtType));

            /* Pick a register for the value */

            if (needReg == RBM_ALLINT && bestReg == 0)
            {
                /* Absent a better suggestion, pick a useless register */

                bestReg = regSet.rsExcludeHint(regSet.rsRegMaskFree(), ~regTracker.rsUselessRegs());
            }

            reg = regSet.rsPickReg(needReg, bestReg);

            if (op1->IsCnsIntOrI() && op1->IsIconHandle(GTF_ICON_TLS_HDL))
            {
                noway_assert(size == EA_PTRSIZE);
                getEmitter()->emitIns_R_C(ins_Load(TYP_I_IMPL), EA_PTRSIZE, reg, FLD_GLOBAL_FS,
                                          (int)op1->gtIntCon.gtIconVal);
            }
            else
            {
                /* Generate "mov reg, [addr]" or "movsx/movzx reg, [addr]" */

                inst_mov_RV_ST(reg, tree);
            }

#ifdef _TARGET_ARM_
            if (tree->gtFlags & GTF_IND_VOLATILE)
            {
                // Emit a memory barrier instruction after the load
                instGen_MemoryBarrier();
            }
#endif

            /* Note the new contents of the register we used */

            regTracker.rsTrackRegTrash(reg);

#ifdef DEBUG
            /* Update the live set of register variables */
            if (compiler->opts.varNames)
                genUpdateLife(tree);
#endif

            /* Now we can update the register pointer information */

            // genDoneAddressable(tree, addrReg, RegSet::KEEP_REG);
            gcInfo.gcMarkRegPtrVal(reg, treeType);

            genCodeForTree_DONE_LIFE(tree, reg);
            return;

        case GT_CAST:

            genCodeForNumericCast(tree, destReg, bestReg);
            return;

        case GT_JTRUE:

            /* Is this a test of a relational operator? */

            if (op1->OperIsCompare())
            {
                /* Generate the conditional jump */

                genCondJump(op1);

                genUpdateLife(tree);
                return;
            }

#ifdef DEBUG
            compiler->gtDispTree(tree);
#endif
            NO_WAY("ISSUE: can we ever have a jumpCC without a compare node?");
            break;

        case GT_SWITCH:
            genCodeForSwitch(tree);
            return;

        case GT_RETFILT:
            noway_assert(tree->gtType == TYP_VOID || op1 != 0);
            if (op1 == 0) // endfinally
            {
                reg = REG_NA;

#ifdef _TARGET_XARCH_
                /* Return using a pop-jmp sequence. As the "try" block calls
                   the finally with a jmp, this leaves the x86 call-ret stack
                   balanced in the normal flow of path. */

                noway_assert(isFramePointerRequired());
                inst_RV(INS_pop_hide, REG_EAX, TYP_I_IMPL);
                inst_RV(INS_i_jmp, REG_EAX, TYP_I_IMPL);
#elif defined(_TARGET_ARM_)
// Nothing needed for ARM
#else
                NYI("TARGET");
#endif
            }
            else // endfilter
            {
                genComputeReg(op1, RBM_INTRET, RegSet::EXACT_REG, RegSet::FREE_REG);
                noway_assert(op1->InReg());
                noway_assert(op1->gtRegNum == REG_INTRET);
                /* The return value has now been computed */
                reg = op1->gtRegNum;

                /* Return */
                instGen_Return(0);
            }

            genCodeForTree_DONE(tree, reg);
            return;

        case GT_RETURN:

            // TODO: this should be done AFTER we called exit mon so that
            //       we are sure that we don't have to keep 'this' alive

            if (compiler->info.compCallUnmanaged && (compiler->compCurBB == compiler->genReturnBB))
            {
                /* either it's an "empty" statement or the return statement
                   of a synchronized method
                 */

                genPInvokeMethodEpilog();
            }

            /* Is there a return value and/or an exit statement? */

            if (op1)
            {
                if (op1->gtType == TYP_VOID)
                {
                    // We're returning nothing, just generate the block (shared epilog calls).
                    genCodeForTree(op1, 0);
                }
#ifdef _TARGET_ARM_
                else if (op1->gtType == TYP_STRUCT)
                {
                    if (op1->gtOper == GT_CALL)
                    {
                        // We have a return call() because we failed to tail call.
                        // In any case, just generate the call and be done.
                        assert(compiler->IsHfa(op1));
                        genCodeForCall(op1->AsCall(), true);
                        genMarkTreeInReg(op1, REG_FLOATRET);
                    }
                    else
                    {
                        assert(op1->gtOper == GT_LCL_VAR);
                        assert(compiler->IsHfa(compiler->lvaGetStruct(op1->gtLclVarCommon.gtLclNum)));
                        genLoadIntoFltRetRegs(op1);
                    }
                }
                else if (op1->TypeGet() == TYP_FLOAT)
                {
                    // This can only occur when we are returning a non-HFA struct
                    // that is composed of a single float field and we performed
                    // struct promotion and enregistered the float field.
                    //
                    genComputeReg(op1, 0, RegSet::ANY_REG, RegSet::FREE_REG);
                    getEmitter()->emitIns_R_R(INS_vmov_f2i, EA_4BYTE, REG_INTRET, op1->gtRegNum);
                }
#endif // _TARGET_ARM_
                else
                {
                    // we can now go through this code for compiler->genReturnBB.  I've regularized all the code.

                    // noway_assert(compiler->compCurBB != compiler->genReturnBB);

                    noway_assert(op1->gtType != TYP_VOID);

                    /* Generate the return value into the return register */

                    genComputeReg(op1, RBM_INTRET, RegSet::EXACT_REG, RegSet::FREE_REG);

                    /* The result must now be in the return register */

                    noway_assert(op1->InReg());
                    noway_assert(op1->gtRegNum == REG_INTRET);
                }

                /* The return value has now been computed */

                reg = op1->gtRegNum;

                genCodeForTree_DONE(tree, reg);
            }

#ifdef PROFILING_SUPPORTED
            // The profiling hook does not trash registers, so it's safe to call after we emit the code for
            // the GT_RETURN tree.

            if (compiler->compCurBB == compiler->genReturnBB)
            {
                genProfilingLeaveCallback();
            }
#endif
#ifdef DEBUG
            if (compiler->opts.compStackCheckOnRet)
            {
                noway_assert(compiler->lvaReturnEspCheck != 0xCCCCCCCC &&
                             compiler->lvaTable[compiler->lvaReturnEspCheck].lvDoNotEnregister &&
                             compiler->lvaTable[compiler->lvaReturnEspCheck].lvOnFrame);
                getEmitter()->emitIns_S_R(INS_cmp, EA_PTRSIZE, REG_SPBASE, compiler->lvaReturnEspCheck, 0);

                BasicBlock*  esp_check = genCreateTempLabel();
                emitJumpKind jmpEqual  = genJumpKindForOper(GT_EQ, CK_SIGNED);
                inst_JMP(jmpEqual, esp_check);
                getEmitter()->emitIns(INS_BREAKPOINT);
                genDefineTempLabel(esp_check);
            }
#endif
            return;

        case GT_COMMA:

            if (tree->gtFlags & GTF_REVERSE_OPS)
            {
                if (tree->gtType == TYP_VOID)
                {
                    genEvalSideEffects(op2);
                    genUpdateLife(op2);
                    genEvalSideEffects(op1);
                    genUpdateLife(tree);
                    return;
                }

                // Generate op2
                genCodeForTree(op2, needReg);
                genUpdateLife(op2);

                noway_assert(op2->InReg());

                regSet.rsMarkRegUsed(op2);

                // Do side effects of op1
                genEvalSideEffects(op1);

                // Recover op2 if spilled
                genRecoverReg(op2, RBM_NONE, RegSet::KEEP_REG);

                regSet.rsMarkRegFree(genRegMask(op2->gtRegNum));

                // set gc info if we need so
                gcInfo.gcMarkRegPtrVal(op2->gtRegNum, treeType);

                genUpdateLife(tree);
                genCodeForTree_DONE(tree, op2->gtRegNum);

                return;
            }
            else
            {
                noway_assert((tree->gtFlags & GTF_REVERSE_OPS) == 0);

                /* Generate side effects of the first operand */

                genEvalSideEffects(op1);
                genUpdateLife(op1);

                /* Is the value of the second operand used? */

                if (tree->gtType == TYP_VOID)
                {
                    /* The right operand produces no result. The morpher is
                       responsible for resetting the type of GT_COMMA nodes
                       to TYP_VOID if op2 isn't meant to yield a result. */

                    genEvalSideEffects(op2);
                    genUpdateLife(tree);
                    return;
                }

                /* Generate the second operand, i.e. the 'real' value */

                genCodeForTree(op2, needReg);
                noway_assert(op2->InReg());

                /* The result of 'op2' is also the final result */

                reg = op2->gtRegNum;

                /* Remember whether we set the flags */

                tree->gtFlags |= (op2->gtFlags & GTF_ZSF_SET);

                genCodeForTree_DONE(tree, reg);
                return;
            }

        case GT_BOX:
            genCodeForTree(op1, needReg);
            noway_assert(op1->InReg());

            /* The result of 'op1' is also the final result */

            reg = op1->gtRegNum;

            /* Remember whether we set the flags */

            tree->gtFlags |= (op1->gtFlags & GTF_ZSF_SET);

            genCodeForTree_DONE(tree, reg);
            return;

        case GT_QMARK:

            genCodeForQmark(tree, destReg, bestReg);
            return;

        case GT_NOP:

#if OPT_BOOL_OPS
            if (op1 == NULL)
                return;
#endif
            __fallthrough;

        case GT_INIT_VAL:

            /* Generate the operand into some register */

            genCodeForTree(op1, needReg);

            /* The result is the same as the operand */

            reg = op1->gtRegNum;

            genCodeForTree_DONE(tree, reg);
            return;

        case GT_INTRINSIC:

            switch (tree->gtIntrinsic.gtIntrinsicId)
            {
                case CORINFO_INTRINSIC_Round:
                {
                    noway_assert(tree->gtType == TYP_INT);

#if FEATURE_STACK_FP_X87
                    genCodeForTreeFlt(op1);

                    /* Store the FP value into the temp */
                    TempDsc* temp = compiler->tmpGetTemp(TYP_INT);

                    FlatFPX87_MoveToTOS(&compCurFPState, op1->gtRegNum);
                    FlatFPX87_Kill(&compCurFPState, op1->gtRegNum);
                    inst_FS_ST(INS_fistp, EA_4BYTE, temp, 0);

                    reg = regSet.rsPickReg(needReg, bestReg);
                    regTracker.rsTrackRegTrash(reg);

                    inst_RV_ST(INS_mov, reg, temp, 0, TYP_INT);

                    compiler->tmpRlsTemp(temp);
#else
                    genCodeForTreeFloat(tree, needReg, bestReg);
                    return;
#endif
                }
                break;

                default:
                    noway_assert(!"unexpected math intrinsic");
            }

            genCodeForTree_DONE(tree, reg);
            return;

        case GT_LCLHEAP:

            reg = genLclHeap(op1);
            genCodeForTree_DONE(tree, reg);
            return;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
            genCodeForRelop(tree, destReg, bestReg);
            return;

        case GT_ADDR:

            genCodeForTreeSmpOp_GT_ADDR(tree, destReg, bestReg);
            return;

#ifdef _TARGET_XARCH_
        case GT_LOCKADD:

            // This is for a locked add operation.  We know that the resulting value doesn't "go" anywhere.
            // For reference, op1 is the location.  op2 is the addend or the value.
            if (op2->OperIsConst())
            {
                noway_assert(op2->TypeGet() == TYP_INT);
                ssize_t cns = op2->gtIntCon.gtIconVal;

                genComputeReg(op1, RBM_NONE, RegSet::ANY_REG, RegSet::KEEP_REG);
                switch (cns)
                {
                    case 1:
                        instGen(INS_lock);
                        instEmit_RM(INS_inc, op1, op1, 0);
                        break;
                    case -1:
                        instGen(INS_lock);
                        instEmit_RM(INS_dec, op1, op1, 0);
                        break;
                    default:
                        assert((int)cns == cns); // By test above for AMD64.
                        instGen(INS_lock);
                        inst_AT_IV(INS_add, EA_4BYTE, op1, (int)cns, 0);
                        break;
                }
                genReleaseReg(op1);
            }
            else
            {
                // non constant addend means it needs to go into a register.
                ins = INS_add;
                goto LockBinOpCommon;
            }

            genFlagsEqualToNone(); // We didn't compute a result into a register.
            genUpdateLife(tree);   // We didn't compute an operand into anything.
            return;

        case GT_XADD:
            ins = INS_xadd;
            goto LockBinOpCommon;
        case GT_XCHG:
            ins = INS_xchg;
            goto LockBinOpCommon;
        LockBinOpCommon:
        {
            // Compute the second operand into a register.  xadd and xchg are r/m32, r32.  So even if op2
            // is a constant, it needs to be in a register.  This should be the output register if
            // possible.
            //
            // For reference, gtOp1 is the location.  gtOp2 is the addend or the value.

            GenTreePtr location = op1;
            GenTreePtr value    = op2;

            // Again, a friendly reminder.  IL calling convention is left to right.
            if (tree->gtFlags & GTF_REVERSE_OPS)
            {
                // The atomic operations destroy this argument, so force it into a scratch register
                reg = regSet.rsPickFreeReg();
                genComputeReg(value, genRegMask(reg), RegSet::EXACT_REG, RegSet::KEEP_REG);

                // Must evaluate location into a register
                genCodeForTree(location, needReg, RBM_NONE);
                assert(location->InReg());
                regSet.rsMarkRegUsed(location);
                regSet.rsLockUsedReg(genRegMask(location->gtRegNum));
                genRecoverReg(value, RBM_NONE, RegSet::KEEP_REG);
                regSet.rsUnlockUsedReg(genRegMask(location->gtRegNum));

                if (ins != INS_xchg)
                {
                    // xchg implies the lock prefix, but xadd and add require it.
                    instGen(INS_lock);
                }
                instEmit_RM_RV(ins, EA_4BYTE, location, reg, 0);
                genReleaseReg(value);
                regTracker.rsTrackRegTrash(reg);
                genReleaseReg(location);
            }
            else
            {
                regMaskTP addrReg;
                if (genMakeIndAddrMode(location, tree, false, /* not for LEA */
                                       needReg, RegSet::KEEP_REG, &addrReg))
                {
                    genUpdateLife(location);

                    reg = regSet.rsPickFreeReg();
                    genComputeReg(value, genRegMask(reg), RegSet::EXACT_REG, RegSet::KEEP_REG);
                    addrReg = genKeepAddressable(location, addrReg, genRegMask(reg));

                    if (ins != INS_xchg)
                    {
                        // xchg implies the lock prefix, but xadd and add require it.
                        instGen(INS_lock);
                    }

                    // instEmit_RM_RV(ins, EA_4BYTE, location, reg, 0);
                    // inst_TT_RV(ins, location, reg);
                    sched_AM(ins, EA_4BYTE, reg, false, location, 0);

                    genReleaseReg(value);
                    regTracker.rsTrackRegTrash(reg);
                    genDoneAddressable(location, addrReg, RegSet::KEEP_REG);
                }
                else
                {
                    // Must evalute location into a register.
                    genCodeForTree(location, needReg, RBM_NONE);
                    assert(location->InReg());
                    regSet.rsMarkRegUsed(location);

                    // xadd destroys this argument, so force it into a scratch register
                    reg = regSet.rsPickFreeReg();
                    genComputeReg(value, genRegMask(reg), RegSet::EXACT_REG, RegSet::KEEP_REG);
                    regSet.rsLockUsedReg(genRegMask(value->gtRegNum));
                    genRecoverReg(location, RBM_NONE, RegSet::KEEP_REG);
                    regSet.rsUnlockUsedReg(genRegMask(value->gtRegNum));

                    if (ins != INS_xchg)
                    {
                        // xchg implies the lock prefix, but xadd and add require it.
                        instGen(INS_lock);
                    }

                    instEmit_RM_RV(ins, EA_4BYTE, location, reg, 0);

                    genReleaseReg(value);
                    regTracker.rsTrackRegTrash(reg);
                    genReleaseReg(location);
                }
            }

            // The flags are equal to the target of the tree (i.e. the result of the add), not to the
            // result in the register.  If tree is actually GT_IND->GT_ADDR->GT_LCL_VAR, we could use
            // that information to set the flags.  Doesn't seem like there is a good reason for that.
            // Therefore, trash the flags.
            genFlagsEqualToNone();

            if (ins == INS_add)
            {
                // If the operator was add, then we were called from the GT_LOCKADD
                // case.  In that case we don't use the result, so we don't need to
                // update anything.
                genUpdateLife(tree);
            }
            else
            {
                genCodeForTree_DONE(tree, reg);
            }
        }
            return;

#else // !_TARGET_XARCH_

        case GT_LOCKADD:
        case GT_XADD:
        case GT_XCHG:

            NYI_ARM("LOCK instructions");
#endif

        case GT_ARR_LENGTH:
        {
            // Make the corresponding ind(a + c) node, and do codegen for that.
            GenTreePtr addr = compiler->gtNewOperNode(GT_ADD, TYP_BYREF, tree->gtArrLen.ArrRef(),
                                                      compiler->gtNewIconNode(tree->AsArrLen()->ArrLenOffset()));
            tree->SetOper(GT_IND);
            tree->gtFlags |= GTF_IND_ARR_LEN; // Record that this node represents an array length expression.
            assert(tree->TypeGet() == TYP_INT);
            tree->gtOp.gtOp1 = addr;
            genCodeForTree(tree, destReg, bestReg);
            return;
        }

        case GT_OBJ:
            // All GT_OBJ nodes must have been morphed prior to this.
            noway_assert(!"Should not see a GT_OBJ node during CodeGen.");

        default:
#ifdef DEBUG
            compiler->gtDispTree(tree);
#endif
            noway_assert(!"unexpected unary/binary operator");
    } // end switch (oper)

    unreached();
}
#ifdef _PREFAST_
#pragma warning(pop) // End suppress PREFast warning about overly large function
#endif

regNumber CodeGen::genIntegerCast(GenTree* tree, regMaskTP needReg, regMaskTP bestReg)
{
    instruction ins;
    emitAttr    size;
    bool        unsv;
    bool        andv = false;
    regNumber   reg;
    GenTreePtr  op1     = tree->gtOp.gtOp1->gtEffectiveVal();
    var_types   dstType = tree->CastToType();
    var_types   srcType = op1->TypeGet();

    if (genTypeSize(srcType) < genTypeSize(dstType))
    {
        // Widening cast

        /* we need the source size */

        size = EA_ATTR(genTypeSize(srcType));

        noway_assert(size < EA_PTRSIZE);

        unsv = varTypeIsUnsigned(srcType);
        ins  = ins_Move_Extend(srcType, op1->InReg());

        /*
            Special case: for a cast of byte to char we first
            have to expand the byte (w/ sign extension), then
            mask off the high bits.
            Use 'movsx' followed by 'and'
        */
        if (!unsv && varTypeIsUnsigned(dstType) && genTypeSize(dstType) < EA_4BYTE)
        {
            noway_assert(genTypeSize(dstType) == EA_2BYTE && size == EA_1BYTE);
            andv = true;
        }
    }
    else
    {
        // Narrowing cast, or sign-changing cast

        noway_assert(genTypeSize(srcType) >= genTypeSize(dstType));

        size = EA_ATTR(genTypeSize(dstType));

        unsv = varTypeIsUnsigned(dstType);
        ins  = ins_Move_Extend(dstType, op1->InReg());
    }

    noway_assert(size < EA_PTRSIZE);

    // Set bestReg to the same register a op1 if op1 is a regVar and is available
    if (op1->InReg())
    {
        regMaskTP op1RegMask = genRegMask(op1->gtRegNum);
        if ((((op1RegMask & bestReg) != 0) || (bestReg == 0)) && ((op1RegMask & regSet.rsRegMaskFree()) != 0))
        {
            bestReg = op1RegMask;
        }
    }

    /* Is the value sitting in a non-byte-addressable register? */

    if (op1->InReg() && (size == EA_1BYTE) && !isByteReg(op1->gtRegNum))
    {
        if (unsv)
        {
            // for unsigned values we can AND, so it needs not be a byte register

            reg = regSet.rsPickReg(needReg, bestReg);

            ins = INS_AND;
        }
        else
        {
            /* Move the value into a byte register */

            reg = regSet.rsGrabReg(RBM_BYTE_REGS);
        }

        if (reg != op1->gtRegNum)
        {
            /* Move the value into that register */

            regTracker.rsTrackRegCopy(reg, op1->gtRegNum);
            inst_RV_RV(INS_mov, reg, op1->gtRegNum, srcType);

            /* The value has a new home now */

            op1->gtRegNum = reg;
        }
    }
    else
    {
        /* Pick a register for the value (general case) */

        reg = regSet.rsPickReg(needReg, bestReg);

        // if we (might) need to set the flags and the value is in the same register
        // and we have an unsigned value then use AND instead of MOVZX
        if (tree->gtSetFlags() && unsv && op1->InReg() && (op1->gtRegNum == reg))
        {
#ifdef _TARGET_X86_
            noway_assert(ins == INS_movzx);
#endif
            ins = INS_AND;
        }
    }

    if (ins == INS_AND)
    {
        noway_assert(andv == false && unsv);

        /* Generate "and reg, MASK */

        insFlags flags = tree->gtSetFlags() ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
        inst_RV_IV(INS_AND, reg, (size == EA_1BYTE) ? 0xFF : 0xFFFF, EA_4BYTE, flags);

        if (tree->gtSetFlags())
            genFlagsEqualToReg(tree, reg);
    }
    else
    {
#ifdef _TARGET_XARCH_
        noway_assert(ins == INS_movsx || ins == INS_movzx);
#endif

        /* Generate "movsx/movzx reg, [addr]" */

        inst_RV_ST(ins, size, reg, op1);

        /* Mask off high bits for cast from byte to char */

        if (andv)
        {
#ifdef _TARGET_XARCH_
            noway_assert(genTypeSize(dstType) == 2 && ins == INS_movsx);
#endif
            insFlags flags = tree->gtSetFlags() ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
            inst_RV_IV(INS_AND, reg, 0xFFFF, EA_4BYTE, flags);

            if (tree->gtSetFlags())
                genFlagsEqualToReg(tree, reg);
        }
    }

    regTracker.rsTrackRegTrash(reg);
    return reg;
}

void CodeGen::genCodeForNumericCast(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    GenTreePtr op1      = tree->gtOp.gtOp1;
    var_types  dstType  = tree->CastToType();
    var_types  baseType = TYP_INT;
    regNumber  reg      = DUMMY_INIT(REG_CORRUPT);
    regMaskTP  needReg  = destReg;
    regMaskTP  addrReg;
    emitAttr   size;
    BOOL       unsv;

    /*
      * Constant casts should have been folded earlier
      * If not finite don't bother
      * We don't do this optimization for debug code/no optimization
      */

    noway_assert((op1->gtOper != GT_CNS_INT && op1->gtOper != GT_CNS_LNG && op1->gtOper != GT_CNS_DBL) ||
                 tree->gtOverflow() || (op1->gtOper == GT_CNS_DBL && !_finite(op1->gtDblCon.gtDconVal)) ||
                 !compiler->opts.OptEnabled(CLFLG_CONSTANTFOLD));

    noway_assert(dstType != TYP_VOID);

    /* What type are we casting from? */

    switch (op1->TypeGet())
    {
        case TYP_LONG:

            /* Special case: the long is generated via the mod of long
               with an int.  This is really an int and need not be
               converted to a reg pair. NOTE: the flag only indicates
               that this is a case to TYP_INT, it hasn't actually
               verified the second operand of the MOD! */

            if (((op1->gtOper == GT_MOD) || (op1->gtOper == GT_UMOD)) && (op1->gtFlags & GTF_MOD_INT_RESULT))
            {

                /* Verify that the op2 of the mod node is
                   1) An integer tree, or
                   2) A long constant that is small enough to fit in an integer
                */

                GenTreePtr modop2 = op1->gtOp.gtOp2;
                if ((genActualType(modop2->gtType) == TYP_INT) ||
                    ((modop2->gtOper == GT_CNS_LNG) && (modop2->gtLngCon.gtLconVal == (int)modop2->gtLngCon.gtLconVal)))
                {
                    genCodeForTree(op1, destReg, bestReg);

#ifdef _TARGET_64BIT_
                    reg = op1->gtRegNum;
#else  // _TARGET_64BIT_
                    reg = genRegPairLo(op1->gtRegPair);
#endif //_TARGET_64BIT_

                    genCodeForTree_DONE(tree, reg);
                    return;
                }
            }

            /* Make the operand addressable.  When gtOverflow() is true,
               hold on to the addrReg as we will need it to access the higher dword */

            op1 = genCodeForCommaTree(op1); // Strip off any commas (necessary, since we seem to generate code for op1
                                            // twice!)
                                            // See, e.g., the TYP_INT case below...

            addrReg = genMakeAddressable2(op1, 0, tree->gtOverflow() ? RegSet::KEEP_REG : RegSet::FREE_REG, false);

            /* Load the lower half of the value into some register */

            if (op1->InReg())
            {
                /* Can we simply use the low part of the value? */
                reg = genRegPairLo(op1->gtRegPair);

                if (tree->gtOverflow())
                    goto REG_OK;

                regMaskTP loMask;
                loMask = genRegMask(reg);
                if (loMask & regSet.rsRegMaskFree())
                    bestReg = loMask;
            }

            // for cast overflow we need to preserve addrReg for testing the hiDword
            // so we lock it to prevent regSet.rsPickReg from picking it.
            if (tree->gtOverflow())
                regSet.rsLockUsedReg(addrReg);

            reg = regSet.rsPickReg(needReg, bestReg);

            if (tree->gtOverflow())
                regSet.rsUnlockUsedReg(addrReg);

            noway_assert(genStillAddressable(op1));

        REG_OK:
            if (!op1->InReg() || (reg != genRegPairLo(op1->gtRegPair)))
            {
                /* Generate "mov reg, [addr-mode]" */
                inst_RV_TT(ins_Load(TYP_INT), reg, op1);
            }

            /* conv.ovf.i8i4, or conv.ovf.u8u4 */

            if (tree->gtOverflow())
            {
                regNumber hiReg = (op1->InReg()) ? genRegPairHi(op1->gtRegPair) : REG_NA;

                emitJumpKind jmpNotEqual = genJumpKindForOper(GT_NE, CK_SIGNED);
                emitJumpKind jmpLTS      = genJumpKindForOper(GT_LT, CK_SIGNED);

                switch (dstType)
                {
                    case TYP_INT:
                        // conv.ovf.i8.i4
                        /*  Generate the following sequence

                                test loDWord, loDWord   // set flags
                                jl neg
                           pos: test hiDWord, hiDWord   // set flags
                                jne ovf
                                jmp done
                           neg: cmp hiDWord, 0xFFFFFFFF
                                jne ovf
                          done:

                        */

                        instGen_Compare_Reg_To_Zero(EA_4BYTE, reg);
                        if (tree->gtFlags & GTF_UNSIGNED) // conv.ovf.u8.i4       (i4 > 0 and upper bits 0)
                        {
                            genJumpToThrowHlpBlk(jmpLTS, SCK_OVERFLOW);
                            goto UPPER_BITS_ZERO;
                        }

#if CPU_LOAD_STORE_ARCH
                        // This is tricky.
                        // We will generate code like
                        // if (...)
                        // {
                        // ...
                        // }
                        // else
                        // {
                        // ...
                        // }
                        // We load the tree op1 into regs when we generate code for if clause.
                        // When we generate else clause, we see the tree is already loaded into reg, and start use it
                        // directly.
                        // Well, when the code is run, we may execute else clause without going through if clause.
                        //
                        genCodeForTree(op1, 0);
#endif

                        BasicBlock* neg;
                        BasicBlock* done;

                        neg  = genCreateTempLabel();
                        done = genCreateTempLabel();

                        // Is the loDWord positive or negative
                        inst_JMP(jmpLTS, neg);

                        // If loDWord is positive, hiDWord should be 0 (sign extended loDWord)

                        if (hiReg < REG_STK)
                        {
                            instGen_Compare_Reg_To_Zero(EA_4BYTE, hiReg);
                        }
                        else
                        {
                            inst_TT_IV(INS_cmp, op1, 0x00000000, 4);
                        }

                        genJumpToThrowHlpBlk(jmpNotEqual, SCK_OVERFLOW);
                        inst_JMP(EJ_jmp, done);

                        // If loDWord is negative, hiDWord should be -1 (sign extended loDWord)

                        genDefineTempLabel(neg);

                        if (hiReg < REG_STK)
                        {
                            inst_RV_IV(INS_cmp, hiReg, 0xFFFFFFFFL, EA_4BYTE);
                        }
                        else
                        {
                            inst_TT_IV(INS_cmp, op1, 0xFFFFFFFFL, 4);
                        }
                        genJumpToThrowHlpBlk(jmpNotEqual, SCK_OVERFLOW);

                        // Done

                        genDefineTempLabel(done);

                        break;

                    case TYP_UINT: // conv.ovf.u8u4
                    UPPER_BITS_ZERO:
                        // Just check that the upper DWord is 0

                        if (hiReg < REG_STK)
                        {
                            instGen_Compare_Reg_To_Zero(EA_4BYTE, hiReg); // set flags
                        }
                        else
                        {
                            inst_TT_IV(INS_cmp, op1, 0, 4);
                        }

                        genJumpToThrowHlpBlk(jmpNotEqual, SCK_OVERFLOW);
                        break;

                    default:
                        noway_assert(!"Unexpected dstType");
                        break;
                }

                genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);
            }

            regTracker.rsTrackRegTrash(reg);
            genDoneAddressable(op1, addrReg, RegSet::FREE_REG);

            genCodeForTree_DONE(tree, reg);
            return;

        case TYP_BOOL:
        case TYP_BYTE:
        case TYP_SHORT:
        case TYP_CHAR:
        case TYP_UBYTE:
            break;

        case TYP_UINT:
        case TYP_INT:
            break;

#if FEATURE_STACK_FP_X87
        case TYP_FLOAT:
            NO_WAY("OPCAST from TYP_FLOAT should have been converted into a helper call");
            break;

        case TYP_DOUBLE:
            if (compiler->opts.compCanUseSSE2)
            {
                // do the SSE2 based cast inline
                // getting the fp operand

                regMaskTP addrRegInt = 0;
                regMaskTP addrRegFlt = 0;

                // make the operand addressable
                // We don't want to collapse constant doubles into floats, as the SSE2 instruction
                // operates on doubles. Note that these (casts from constant doubles) usually get
                // folded, but we don't do it for some cases (infinitys, etc). So essentially this
                // shouldn't affect performance or size at all. We're fixing this for #336067
                op1 = genMakeAddressableStackFP(op1, &addrRegInt, &addrRegFlt, false);
                if (!addrRegFlt && !op1->IsRegVar())
                {
                    // we have the address

                    inst_RV_TT(INS_movsdsse2, REG_XMM0, op1, 0, EA_8BYTE);
                    genDoneAddressableStackFP(op1, addrRegInt, addrRegFlt, RegSet::KEEP_REG);
                    genUpdateLife(op1);

                    reg = regSet.rsPickReg(needReg);
                    getEmitter()->emitIns_R_R(INS_cvttsd2si, EA_8BYTE, reg, REG_XMM0);

                    regTracker.rsTrackRegTrash(reg);
                    genCodeForTree_DONE(tree, reg);
                }
                else
                {
                    // we will need to use a temp to get it into the xmm reg
                    var_types typeTemp = op1->TypeGet();
                    TempDsc*  temp     = compiler->tmpGetTemp(typeTemp);

                    size = EA_ATTR(genTypeSize(typeTemp));

                    if (addrRegFlt)
                    {
                        // On the fp stack; Take reg to top of stack

                        FlatFPX87_MoveToTOS(&compCurFPState, op1->gtRegNum);
                    }
                    else
                    {
                        // op1->IsRegVar()
                        // pick a register
                        reg = regSet.PickRegFloat();
                        if (!op1->IsRegVarDeath())
                        {
                            // Load it on the fp stack
                            genLoadStackFP(op1, reg);
                        }
                        else
                        {
                            // if it's dying, genLoadStackFP just renames it and then we move reg to TOS
                            genLoadStackFP(op1, reg);
                            FlatFPX87_MoveToTOS(&compCurFPState, reg);
                        }
                    }

                    // pop it off the fp stack
                    compCurFPState.Pop();

                    getEmitter()->emitIns_S(INS_fstp, size, temp->tdTempNum(), 0);
                    // pick a reg
                    reg = regSet.rsPickReg(needReg);

                    inst_RV_ST(INS_movsdsse2, REG_XMM0, temp, 0, TYP_DOUBLE, EA_8BYTE);
                    getEmitter()->emitIns_R_R(INS_cvttsd2si, EA_8BYTE, reg, REG_XMM0);

                    // done..release the temp
                    compiler->tmpRlsTemp(temp);

                    // the reg is now trashed
                    regTracker.rsTrackRegTrash(reg);
                    genDoneAddressableStackFP(op1, addrRegInt, addrRegFlt, RegSet::KEEP_REG);
                    genUpdateLife(op1);
                    genCodeForTree_DONE(tree, reg);
                }
            }
#else
        case TYP_FLOAT:
        case TYP_DOUBLE:
            genCodeForTreeFloat(tree, needReg, bestReg);
#endif // FEATURE_STACK_FP_X87
            return;

        default:
            noway_assert(!"unexpected cast type");
    }

    if (tree->gtOverflow())
    {
        /* Compute op1 into a register, and free the register */

        genComputeReg(op1, destReg, RegSet::ANY_REG, RegSet::FREE_REG);
        reg = op1->gtRegNum;

        /* Do we need to compare the value, or just check masks */

        ssize_t typeMin = DUMMY_INIT(~0), typeMax = DUMMY_INIT(0);
        ssize_t typeMask;

        switch (dstType)
        {
            case TYP_BYTE:
                typeMask = ssize_t((int)0xFFFFFF80);
                typeMin  = SCHAR_MIN;
                typeMax  = SCHAR_MAX;
                unsv     = (tree->gtFlags & GTF_UNSIGNED);
                break;
            case TYP_SHORT:
                typeMask = ssize_t((int)0xFFFF8000);
                typeMin  = SHRT_MIN;
                typeMax  = SHRT_MAX;
                unsv     = (tree->gtFlags & GTF_UNSIGNED);
                break;
            case TYP_INT:
                typeMask = ssize_t((int)0x80000000L);
#ifdef _TARGET_64BIT_
                unsv    = (tree->gtFlags & GTF_UNSIGNED);
                typeMin = INT_MIN;
                typeMax = INT_MAX;
#else // _TARGET_64BIT_
                noway_assert((tree->gtFlags & GTF_UNSIGNED) != 0);
                unsv     = true;
#endif // _TARGET_64BIT_
                break;
            case TYP_UBYTE:
                unsv     = true;
                typeMask = ssize_t((int)0xFFFFFF00L);
                break;
            case TYP_CHAR:
                unsv     = true;
                typeMask = ssize_t((int)0xFFFF0000L);
                break;
            case TYP_UINT:
                unsv = true;
#ifdef _TARGET_64BIT_
                typeMask = 0xFFFFFFFF00000000LL;
#else  // _TARGET_64BIT_
                typeMask = 0x80000000L;
                noway_assert((tree->gtFlags & GTF_UNSIGNED) == 0);
#endif // _TARGET_64BIT_
                break;
            default:
                NO_WAY("Unknown type");
                return;
        }

        // If we just have to check a mask.
        // This must be conv.ovf.u4u1, conv.ovf.u4u2, conv.ovf.u4i4,
        // or conv.i4u4

        if (unsv)
        {
            inst_RV_IV(INS_TEST, reg, typeMask, emitActualTypeSize(baseType));
            emitJumpKind jmpNotEqual = genJumpKindForOper(GT_NE, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpNotEqual, SCK_OVERFLOW);
        }
        else
        {
            // Check the value is in range.
            // This must be conv.ovf.i4i1, etc.

            // Compare with the MAX

            noway_assert(typeMin != DUMMY_INIT(~0) && typeMax != DUMMY_INIT(0));

            inst_RV_IV(INS_cmp, reg, typeMax, emitActualTypeSize(baseType));
            emitJumpKind jmpGTS = genJumpKindForOper(GT_GT, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpGTS, SCK_OVERFLOW);

            // Compare with the MIN

            inst_RV_IV(INS_cmp, reg, typeMin, emitActualTypeSize(baseType));
            emitJumpKind jmpLTS = genJumpKindForOper(GT_LT, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpLTS, SCK_OVERFLOW);
        }

        genCodeForTree_DONE(tree, reg);
        return;
    }

    /* Make the operand addressable */

    addrReg = genMakeAddressable(op1, needReg, RegSet::FREE_REG, true);

    reg = genIntegerCast(tree, needReg, bestReg);

    genDoneAddressable(op1, addrReg, RegSet::FREE_REG);

    genCodeForTree_DONE(tree, reg);
}

/*****************************************************************************
 *
 *  Generate code for a leaf node of type GT_ADDR
 */

void CodeGen::genCodeForTreeSmpOp_GT_ADDR(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    genTreeOps      oper     = tree->OperGet();
    const var_types treeType = tree->TypeGet();
    GenTreePtr      op1;
    regNumber       reg;
    regMaskTP       needReg = destReg;
    regMaskTP       addrReg;

#ifdef DEBUG
    reg     = (regNumber)0xFEEFFAAF; // to detect uninitialized use
    addrReg = 0xDEADCAFE;
#endif

    // We should get here for ldloca, ldarga, ldslfda, ldelema,
    // or ldflda.
    if (oper == GT_ARR_ELEM)
    {
        op1 = tree;
    }
    else
    {
        op1 = tree->gtOp.gtOp1;
    }

    // (tree=op1, needReg=0, keepReg=RegSet::FREE_REG, smallOK=true)
    if (oper == GT_ARR_ELEM)
    {
        // To get the address of the array element,
        // we first call genMakeAddrArrElem to make the element addressable.
        //     (That is, for example, we first emit code to calculate EBX, and EAX.)
        // And then use lea to obtain the address.
        //     (That is, for example, we then emit
        //         lea EBX, bword ptr [EBX+4*EAX+36]
        //      to obtain the address of the array element.)
        addrReg = genMakeAddrArrElem(op1, tree, RBM_NONE, RegSet::FREE_REG);
    }
    else
    {
        addrReg = genMakeAddressable(op1, 0, RegSet::FREE_REG, true);
    }

    noway_assert(treeType == TYP_BYREF || treeType == TYP_I_IMPL);

    // We want to reuse one of the scratch registers that were used
    // in forming the address mode as the target register for the lea.
    // If bestReg is unset or if it is set to one of the registers used to
    // form the address (i.e. addrReg), we calculate the scratch register
    // to use as the target register for the LEA

    bestReg = regSet.rsUseIfZero(bestReg, addrReg);
    bestReg = regSet.rsNarrowHint(bestReg, addrReg);

    /* Even if addrReg is regSet.rsRegMaskCanGrab(), regSet.rsPickReg() won't spill
       it since keepReg==false.
       If addrReg can't be grabbed, regSet.rsPickReg() won't touch it anyway.
       So this is guaranteed not to spill addrReg */

    reg = regSet.rsPickReg(needReg, bestReg);

    // Slight workaround, force the inst routine to think that
    // value being loaded is an int (since that is what what
    // LEA will return)  otherwise it would try to allocate
    // two registers for a long etc.
    noway_assert(treeType == TYP_I_IMPL || treeType == TYP_BYREF);
    op1->gtType = treeType;

    inst_RV_TT(INS_lea, reg, op1, 0, (treeType == TYP_BYREF) ? EA_BYREF : EA_PTRSIZE);

    // The Lea instruction above better not have tried to put the
    // 'value' pointed to by 'op1' in a register, LEA will not work.
    noway_assert(!(op1->InReg()));

    genDoneAddressable(op1, addrReg, RegSet::FREE_REG);
    // gcInfo.gcMarkRegSetNpt(genRegMask(reg));
    noway_assert((gcInfo.gcRegGCrefSetCur & genRegMask(reg)) == 0);

    regTracker.rsTrackRegTrash(reg); // reg does have foldable value in it
    gcInfo.gcMarkRegPtrVal(reg, treeType);

    genCodeForTree_DONE(tree, reg);
}

#ifdef _TARGET_ARM_

/*****************************************************************************
 *
 * Move (load/store) between float ret regs and struct promoted variable.
 *
 * varDsc - The struct variable to be loaded from or stored into.
 * isLoadIntoFlt - Perform a load operation if "true" or store if "false."
 *
 */
void CodeGen::genLdStFltRetRegsPromotedVar(LclVarDsc* varDsc, bool isLoadIntoFlt)
{
    regNumber curReg = REG_FLOATRET;

    unsigned lclLast = varDsc->lvFieldLclStart + varDsc->lvFieldCnt - 1;
    for (unsigned lclNum = varDsc->lvFieldLclStart; lclNum <= lclLast; ++lclNum)
    {
        LclVarDsc* varDscFld = &compiler->lvaTable[lclNum];

        // Is the struct field promoted and sitting in a register?
        if (varDscFld->lvRegister)
        {
            // Move from the struct field into curReg if load
            // else move into struct field from curReg if store
            regNumber srcReg = (isLoadIntoFlt) ? varDscFld->lvRegNum : curReg;
            regNumber dstReg = (isLoadIntoFlt) ? curReg : varDscFld->lvRegNum;
            if (srcReg != dstReg)
            {
                inst_RV_RV(ins_Copy(varDscFld->TypeGet()), dstReg, srcReg, varDscFld->TypeGet());
                regTracker.rsTrackRegCopy(dstReg, srcReg);
            }
        }
        else
        {
            // This field is in memory, do a move between the field and float registers.
            emitAttr size = (varDscFld->TypeGet() == TYP_DOUBLE) ? EA_8BYTE : EA_4BYTE;
            if (isLoadIntoFlt)
            {
                getEmitter()->emitIns_R_S(ins_Load(varDscFld->TypeGet()), size, curReg, lclNum, 0);
                regTracker.rsTrackRegTrash(curReg);
            }
            else
            {
                getEmitter()->emitIns_S_R(ins_Store(varDscFld->TypeGet()), size, curReg, lclNum, 0);
            }
        }

        // Advance the current reg.
        curReg = (varDscFld->TypeGet() == TYP_DOUBLE) ? REG_NEXT(REG_NEXT(curReg)) : REG_NEXT(curReg);
    }
}

void CodeGen::genLoadIntoFltRetRegs(GenTreePtr tree)
{
    assert(tree->TypeGet() == TYP_STRUCT);
    assert(tree->gtOper == GT_LCL_VAR);
    LclVarDsc* varDsc = compiler->lvaTable + tree->gtLclVarCommon.gtLclNum;
    int        slots  = varDsc->lvSize() / REGSIZE_BYTES;
    if (varDsc->lvPromoted)
    {
        genLdStFltRetRegsPromotedVar(varDsc, true);
    }
    else
    {
        if (slots <= 2)
        {
            // Use the load float/double instruction.
            inst_RV_TT(ins_Load((slots == 1) ? TYP_FLOAT : TYP_DOUBLE), REG_FLOATRET, tree, 0,
                       (slots == 1) ? EA_4BYTE : EA_8BYTE);
        }
        else
        {
            // Use the load store multiple instruction.
            regNumber reg = regSet.rsPickReg(RBM_ALLINT);
            inst_RV_TT(INS_lea, reg, tree, 0, EA_PTRSIZE);
            regTracker.rsTrackRegTrash(reg);
            getEmitter()->emitIns_R_R_I(INS_vldm, EA_4BYTE, REG_FLOATRET, reg, slots * REGSIZE_BYTES);
        }
    }
    genMarkTreeInReg(tree, REG_FLOATRET);
}

void CodeGen::genStoreFromFltRetRegs(GenTreePtr tree)
{
    assert(tree->TypeGet() == TYP_STRUCT);
    assert(tree->OperGet() == GT_ASG);

    // LHS should be lcl var or fld.
    GenTreePtr op1 = tree->gtOp.gtOp1;

    // TODO: We had a bug where op1 was a GT_IND, the result of morphing a GT_BOX, and not properly
    // handling multiple levels of inlined functions that return HFA on the right-hand-side.
    // So, make the op1 check a noway_assert (that exists in non-debug builds) so we'll fall
    // back to MinOpts with no inlining, if we don't have what we expect. We don't want to
    // do the full IsHfa() check in non-debug, since that involves VM calls, so leave that
    // as a regular assert().
    noway_assert((op1->gtOper == GT_LCL_VAR) || (op1->gtOper == GT_LCL_FLD));
    unsigned varNum = op1->gtLclVarCommon.gtLclNum;
    assert(compiler->IsHfa(compiler->lvaGetStruct(varNum)));

    // The RHS should be a call.
    GenTreePtr op2 = tree->gtOp.gtOp2;
    assert(op2->gtOper == GT_CALL);

    // Generate code for call and copy the return registers into the local.
    regMaskTP retMask = genCodeForCall(op2->AsCall(), true);

    // Ret mask should be contiguously set from s0, up to s3 or starting from d0 upto d3.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    regMaskTP mask = ((retMask >> REG_FLOATRET) + 1);
    assert((mask & (mask - 1)) == 0);
    assert(mask <= (1 << MAX_HFA_RET_SLOTS));
    assert((retMask & (((regMaskTP)RBM_FLOATRET) - 1)) == 0);
#endif

    int slots = genCountBits(retMask & RBM_ALLFLOAT);

    LclVarDsc* varDsc = &compiler->lvaTable[varNum];

    if (varDsc->lvPromoted)
    {
        genLdStFltRetRegsPromotedVar(varDsc, false);
    }
    else
    {
        if (slots <= 2)
        {
            inst_TT_RV(ins_Store((slots == 1) ? TYP_FLOAT : TYP_DOUBLE), op1, REG_FLOATRET, 0,
                       (slots == 1) ? EA_4BYTE : EA_8BYTE);
        }
        else
        {
            regNumber reg = regSet.rsPickReg(RBM_ALLINT);
            inst_RV_TT(INS_lea, reg, op1, 0, EA_PTRSIZE);
            regTracker.rsTrackRegTrash(reg);
            getEmitter()->emitIns_R_R_I(INS_vstm, EA_4BYTE, REG_FLOATRET, reg, slots * REGSIZE_BYTES);
        }
    }
}

#endif // _TARGET_ARM_

/*****************************************************************************
 *
 *  Generate code for a GT_ASG tree
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
void CodeGen::genCodeForTreeSmpOpAsg(GenTreePtr tree)
{
    noway_assert(tree->gtOper == GT_ASG);

    GenTreePtr  op1     = tree->gtOp.gtOp1;
    GenTreePtr  op2     = tree->gtOp.gtOp2;
    regMaskTP   needReg = RBM_ALLINT;
    regMaskTP   bestReg = RBM_CORRUPT;
    regMaskTP   addrReg = DUMMY_INIT(RBM_CORRUPT);
    bool        ovfl    = false; // Do we need an overflow check
    bool        volat   = false; // Is this a volatile store
    regMaskTP   regGC;
    instruction ins;
    unsigned    lclVarNum = compiler->lvaCount;
    unsigned    lclILoffs = DUMMY_INIT(0);

#ifdef _TARGET_ARM_
    if (tree->gtType == TYP_STRUCT)
    {
        // We use copy block to assign structs, however to receive HFAs in registers
        // from a CALL, we use assignment, var = (hfa) call();
        assert(compiler->IsHfa(tree));
        genStoreFromFltRetRegs(tree);
        return;
    }
#endif

#ifdef DEBUG
    if (varTypeIsFloating(op1) != varTypeIsFloating(op2))
    {
        if (varTypeIsFloating(op1))
            assert(!"Bad IL: Illegal assignment of integer into float!");
        else
            assert(!"Bad IL: Illegal assignment of float into integer!");
    }
#endif

    if ((tree->gtFlags & GTF_REVERSE_OPS) == 0)
    {
        op1 = genCodeForCommaTree(op1); // Strip away any comma expressions.
    }

    /* Is the target a register or local variable? */
    switch (op1->gtOper)
    {
        unsigned   varNum;
        LclVarDsc* varDsc;

        case GT_LCL_VAR:
            varNum = op1->gtLclVarCommon.gtLclNum;
            noway_assert(varNum < compiler->lvaCount);
            varDsc = compiler->lvaTable + varNum;

            /* For non-debuggable code, every definition of a lcl-var has
             * to be checked to see if we need to open a new scope for it.
             * Remember the local var info to call siCheckVarScope
             * AFTER code generation of the assignment.
             */
            if (compiler->opts.compScopeInfo && !compiler->opts.compDbgCode && (compiler->info.compVarScopesCount > 0))
            {
                lclVarNum = varNum;
                lclILoffs = op1->gtLclVar.gtLclILoffs;
            }

            /* Check against dead store ? (with min opts we may have dead stores) */

            noway_assert(!varDsc->lvTracked || compiler->opts.MinOpts() || !(op1->gtFlags & GTF_VAR_DEATH));

            /* Does this variable live in a register? */

            if (genMarkLclVar(op1))
                goto REG_VAR2;

            break;

        REG_VAR2:

            /* Get hold of the target register */

            regNumber op1Reg;

            op1Reg = op1->gtRegVar.gtRegNum;

#ifdef DEBUG
            /* Compute the RHS (hopefully) into the variable's register.
               For debuggable code, op1Reg may already be part of regSet.rsMaskVars,
               as variables are kept alive everywhere. So we have to be
               careful if we want to compute the value directly into
               the variable's register. */

            bool needToUpdateRegSetCheckLevel;
            needToUpdateRegSetCheckLevel = false;
#endif

            // We should only be accessing lvVarIndex if varDsc is tracked.
            assert(varDsc->lvTracked);

            if (VarSetOps::IsMember(compiler, genUpdateLiveSetForward(op2), varDsc->lvVarIndex))
            {
                noway_assert(compiler->opts.compDbgCode);

                /* The predictor might expect us to generate op2 directly
                   into the var's register. However, since the variable is
                   already alive, first kill it and its register. */

                if (rpCanAsgOperWithoutReg(op2, true))
                {
                    genUpdateLife(VarSetOps::RemoveElem(compiler, compiler->compCurLife, varDsc->lvVarIndex));
                    needReg = regSet.rsNarrowHint(needReg, genRegMask(op1Reg));
#ifdef DEBUG
                    needToUpdateRegSetCheckLevel = true;
#endif
                }
            }
            else
            {
                needReg = regSet.rsNarrowHint(needReg, genRegMask(op1Reg));
            }

#ifdef DEBUG

            /* Special cases: op2 is a GT_CNS_INT */

            if (op2->gtOper == GT_CNS_INT && !(op1->gtFlags & GTF_VAR_DEATH))
            {
                /* Save the old life status */

                VarSetOps::Assign(compiler, genTempOldLife, compiler->compCurLife);
                VarSetOps::AddElemD(compiler, compiler->compCurLife, varDsc->lvVarIndex);

                /* Set a flag to avoid printing the message
                   and remember that life was changed. */

                genTempLiveChg = false;
            }
#endif

#ifdef DEBUG
            if (needToUpdateRegSetCheckLevel)
                compiler->compRegSetCheckLevel++;
#endif
            genCodeForTree(op2, needReg, genRegMask(op1Reg));
#ifdef DEBUG
            if (needToUpdateRegSetCheckLevel)
                compiler->compRegSetCheckLevel--;
            noway_assert(compiler->compRegSetCheckLevel >= 0);
#endif
            noway_assert(op2->InReg());

            /* Make sure the value ends up in the right place ... */

            if (op2->gtRegNum != op1Reg)
            {
                /* Make sure the target of the store is available */

                if (regSet.rsMaskUsed & genRegMask(op1Reg))
                    regSet.rsSpillReg(op1Reg);

#ifdef _TARGET_ARM_
                if (op1->TypeGet() == TYP_FLOAT)
                {
                    // This can only occur when we are returning a non-HFA struct
                    // that is composed of a single float field.
                    //
                    inst_RV_RV(INS_vmov_i2f, op1Reg, op2->gtRegNum, op1->TypeGet());
                }
                else
#endif // _TARGET_ARM_
                {
                    inst_RV_RV(INS_mov, op1Reg, op2->gtRegNum, op1->TypeGet());
                }

                /* The value has been transferred to 'op1Reg' */

                regTracker.rsTrackRegCopy(op1Reg, op2->gtRegNum);

                if ((genRegMask(op2->gtRegNum) & regSet.rsMaskUsed) == 0)
                    gcInfo.gcMarkRegSetNpt(genRegMask(op2->gtRegNum));

                gcInfo.gcMarkRegPtrVal(op1Reg, tree->TypeGet());
            }
            else
            {
                // First we need to remove it from the original reg set mask (or else trigger an
                // assert when we add it to the other reg set mask).
                gcInfo.gcMarkRegSetNpt(genRegMask(op1Reg));
                gcInfo.gcMarkRegPtrVal(op1Reg, tree->TypeGet());

                // The emitter has logic that tracks the GCness of registers and asserts if you
                // try to do bad things to a GC pointer (like lose its GCness).

                // An explict cast of a GC pointer to an int (which is legal if the
                // pointer is pinned) is encoded as an assignment of a GC source
                // to a integer variable.  Unfortunately if the source was the last
                // use, and the source register gets reused by the destination, no
                // code gets emitted (That is where we are at right now).  The emitter
                // thinks the register is a GC pointer (it did not see the cast).
                // This causes asserts, as well as bad GC info since we will continue
                // to report the register as a GC pointer even if we do arithmetic
                // with it. So force the emitter to see the change in the type
                // of variable by placing a label.
                // We only have to do this check at this point because in the
                // CAST morphing, we create a temp and assignment whenever we
                // have a cast that loses its GCness.

                if (varTypeGCtype(op2->TypeGet()) != varTypeGCtype(op1->TypeGet()))
                {
                    void* label = getEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                             gcInfo.gcRegByrefSetCur);
                }
            }

            addrReg = 0;

            genCodeForTreeSmpOpAsg_DONE_ASSG(tree, addrReg, op1Reg, ovfl);
            goto LExit;

        case GT_LCL_FLD:

            // We only use GT_LCL_FLD for lvDoNotEnregister vars, so we don't have
            // to worry about it being enregistered.
            noway_assert(compiler->lvaTable[op1->gtLclFld.gtLclNum].lvRegister == 0);
            break;

        case GT_CLS_VAR:

            __fallthrough;

        case GT_IND:
        case GT_NULLCHECK:

            assert((op1->OperGet() == GT_CLS_VAR) || (op1->OperGet() == GT_IND));

            if (op1->gtFlags & GTF_IND_VOLATILE)
            {
                volat = true;
            }

            break;

        default:
            break;
    }

    /* Is the value being assigned a simple one? */

    noway_assert(op2);
    switch (op2->gtOper)
    {
        case GT_LCL_VAR:

            if (!genMarkLclVar(op2))
                goto SMALL_ASG;

            __fallthrough;

        case GT_REG_VAR:

            /* Is the target a byte/short/char value? */

            if (varTypeIsSmall(op1->TypeGet()))
                goto SMALL_ASG;

            if (tree->gtFlags & GTF_REVERSE_OPS)
                goto SMALL_ASG;

            /* Make the target addressable */

            op1 = genCodeForCommaTree(op1); // Strip away comma expressions.

            addrReg = genMakeAddressable(op1, needReg, RegSet::KEEP_REG, true);

            /* Does the write barrier helper do the assignment? */

            regGC = WriteBarrier(op1, op2, addrReg);

            // Was assignment done by the WriteBarrier
            if (regGC == RBM_NONE)
            {
#ifdef _TARGET_ARM_
                if (volat)
                {
                    // Emit a memory barrier instruction before the store
                    instGen_MemoryBarrier();
                }
#endif

                /* Move the value into the target */

                inst_TT_RV(ins_Store(op1->TypeGet()), op1, op2->gtRegVar.gtRegNum);

                // This is done in WriteBarrier when (regGC != RBM_NONE)

                /* Free up anything that was tied up by the LHS */
                genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);
            }

            /* Free up the RHS */
            genUpdateLife(op2);

            /* Remember that we've also touched the op2 register */

            addrReg |= genRegMask(op2->gtRegVar.gtRegNum);
            break;

        case GT_CNS_INT:

            ssize_t ival;
            ival = op2->gtIntCon.gtIconVal;
            emitAttr size;
            size = emitTypeSize(tree->TypeGet());

            ins = ins_Store(op1->TypeGet());

            // If we are storing a constant into a local variable
            // we extend the size of the store here
            // this normally takes place in CodeGen::inst_TT_IV on x86.
            //
            if ((op1->gtOper == GT_LCL_VAR) && (size < EA_4BYTE))
            {
                unsigned   varNum = op1->gtLclVarCommon.gtLclNum;
                LclVarDsc* varDsc = compiler->lvaTable + varNum;

                // Fix the immediate by sign extending if needed
                if (!varTypeIsUnsigned(varDsc->TypeGet()))
                {
                    if (size == EA_1BYTE)
                    {
                        if ((ival & 0x7f) != ival)
                            ival = ival | 0xffffff00;
                    }
                    else
                    {
                        assert(size == EA_2BYTE);
                        if ((ival & 0x7fff) != ival)
                            ival = ival | 0xffff0000;
                    }
                }

                // A local stack slot is at least 4 bytes in size, regardless of
                // what the local var is typed as, so auto-promote it here
                // unless it is a field of a promoted struct
                if (!varDsc->lvIsStructField)
                {
                    size = EA_SET_SIZE(size, EA_4BYTE);
                    ins  = ins_Store(TYP_INT);
                }
            }

            /* Make the target addressable */

            addrReg = genMakeAddressable(op1, needReg, RegSet::KEEP_REG, true);

#ifdef _TARGET_ARM_
            if (volat)
            {
                // Emit a memory barrier instruction before the store
                instGen_MemoryBarrier();
            }
#endif

            /* Move the value into the target */

            noway_assert(op1->gtOper != GT_REG_VAR);
            if (compiler->opts.compReloc && op2->IsIconHandle())
            {
                /* The constant is actually a handle that may need relocation
                   applied to it.  genComputeReg will do the right thing (see
                   code in genCodeForTreeConst), so we'll just call it to load
                   the constant into a register. */

                genComputeReg(op2, needReg & ~addrReg, RegSet::ANY_REG, RegSet::KEEP_REG);
                addrReg = genKeepAddressable(op1, addrReg, genRegMask(op2->gtRegNum));
                noway_assert(op2->InReg());
                inst_TT_RV(ins, op1, op2->gtRegNum);
                genReleaseReg(op2);
            }
            else
            {
                regSet.rsLockUsedReg(addrReg);

#if REDUNDANT_LOAD
                bool      copyIconFromReg = true;
                regNumber iconReg         = REG_NA;

#ifdef _TARGET_ARM_
                // Only if the constant can't be encoded in a small instruction,
                // look for another register to copy the value from. (Assumes
                // target is a small register.)
                if ((op1->InReg()) && !isRegPairType(tree->gtType) &&
                    arm_Valid_Imm_For_Small_Mov(op1->gtRegNum, ival, INS_FLAGS_DONT_CARE))
                {
                    copyIconFromReg = false;
                }
#endif // _TARGET_ARM_

                if (copyIconFromReg)
                {
                    iconReg = regTracker.rsIconIsInReg(ival);
                    if (iconReg == REG_NA)
                        copyIconFromReg = false;
                }

                if (copyIconFromReg && (isByteReg(iconReg) || (genTypeSize(tree->TypeGet()) == EA_PTRSIZE) ||
                                        (genTypeSize(tree->TypeGet()) == EA_4BYTE)))
                {
                    /* Move the value into the target */

                    inst_TT_RV(ins, op1, iconReg, 0, size);
                }
                else
#endif // REDUNDANT_LOAD
                {
                    inst_TT_IV(ins, op1, ival, 0, size);
                }

                regSet.rsUnlockUsedReg(addrReg);
            }

            /* Free up anything that was tied up by the LHS */

            genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);
            break;

        default:

        SMALL_ASG:

            bool             isWriteBarrier = false;
            regMaskTP        needRegOp1     = RBM_ALLINT;
            RegSet::ExactReg mustReg        = RegSet::ANY_REG; // set to RegSet::EXACT_REG for op1 and NOGC helpers

            /*  Is the LHS more complex than the RHS? */

            if (tree->gtFlags & GTF_REVERSE_OPS)
            {
                /* Is the target a byte/short/char value? */

                if (varTypeIsSmall(op1->TypeGet()))
                {
                    noway_assert(op1->gtOper != GT_LCL_VAR || (op1->gtFlags & GTF_VAR_CAST) ||
                                 // TODO: Why does this have to be true?
                                 compiler->lvaTable[op1->gtLclVarCommon.gtLclNum].lvIsStructField ||
                                 compiler->lvaTable[op1->gtLclVarCommon.gtLclNum].lvNormalizeOnLoad());

                    if (op2->gtOper == GT_CAST && !op2->gtOverflow())
                    {
                        /* Special case: cast to small type */

                        if (op2->CastToType() >= op1->gtType)
                        {
                            /* Make sure the cast operand is not > int */

                            if (op2->CastFromType() <= TYP_INT)
                            {
                                /* Cast via a non-smaller type */

                                op2 = op2->gtCast.CastOp();
                            }
                        }
                    }

                    if (op2->gtOper == GT_AND && op2->gtOp.gtOp2->gtOper == GT_CNS_INT)
                    {
                        unsigned mask;
                        switch (op1->gtType)
                        {
                            case TYP_BYTE:
                                mask = 0x000000FF;
                                break;
                            case TYP_SHORT:
                                mask = 0x0000FFFF;
                                break;
                            case TYP_CHAR:
                                mask = 0x0000FFFF;
                                break;
                            default:
                                goto SIMPLE_SMALL;
                        }

                        if (unsigned(op2->gtOp.gtOp2->gtIntCon.gtIconVal) == mask)
                        {
                            /* Redundant AND */

                            op2 = op2->gtOp.gtOp1;
                        }
                    }

                /* Must get the new value into a byte register */

                SIMPLE_SMALL:
                    if (varTypeIsByte(op1->TypeGet()))
                        genComputeReg(op2, RBM_BYTE_REGS, RegSet::EXACT_REG, RegSet::KEEP_REG);
                    else
                        goto NOT_SMALL;
                }
                else
                {
                NOT_SMALL:
                    /* Generate the RHS into a register */

                    isWriteBarrier = gcInfo.gcIsWriteBarrierAsgNode(tree);
                    if (isWriteBarrier)
                    {
#if NOGC_WRITE_BARRIERS
                        // Exclude the REG_WRITE_BARRIER from op2's needReg mask
                        needReg = Target::exclude_WriteBarrierReg(needReg);
                        mustReg = RegSet::EXACT_REG;
#else  // !NOGC_WRITE_BARRIERS
                        // This code should be generic across architectures.

                        // For the standard JIT Helper calls
                        // op1 goes into REG_ARG_0 and
                        // op2 goes into REG_ARG_1
                        //
                        needRegOp1 = RBM_ARG_0;
                        needReg    = RBM_ARG_1;
#endif // !NOGC_WRITE_BARRIERS
                    }
                    genComputeReg(op2, needReg, mustReg, RegSet::KEEP_REG);
                }

                noway_assert(op2->InReg());

                /* Make the target addressable */

                op1     = genCodeForCommaTree(op1); // Strip off any comma expressions.
                addrReg = genMakeAddressable(op1, needRegOp1, RegSet::KEEP_REG, true);

                /*  Make sure the RHS register hasn't been spilled;
                    keep the register marked as "used", otherwise
                    we might get the pointer lifetimes wrong.
                */

                if (varTypeIsByte(op1->TypeGet()))
                    needReg = regSet.rsNarrowHint(RBM_BYTE_REGS, needReg);

                genRecoverReg(op2, needReg, RegSet::KEEP_REG);
                noway_assert(op2->InReg());

                /* Lock the RHS temporarily (lock only already used) */

                regSet.rsLockUsedReg(genRegMask(op2->gtRegNum));

                /* Make sure the LHS is still addressable */

                addrReg = genKeepAddressable(op1, addrReg);

                /* We can unlock (only already used ) the RHS register */

                regSet.rsUnlockUsedReg(genRegMask(op2->gtRegNum));

                /* Does the write barrier helper do the assignment? */

                regGC = WriteBarrier(op1, op2, addrReg);

                if (regGC != 0)
                {
                    // Yes, assignment done by the WriteBarrier
                    noway_assert(isWriteBarrier);
                }
                else
                {
#ifdef _TARGET_ARM_
                    if (volat)
                    {
                        // Emit a memory barrier instruction before the store
                        instGen_MemoryBarrier();
                    }
#endif

                    /* Move the value into the target */

                    inst_TT_RV(ins_Store(op1->TypeGet()), op1, op2->gtRegNum);
                }

#ifdef DEBUG
                /* Update the current liveness info */
                if (compiler->opts.varNames)
                    genUpdateLife(tree);
#endif

                // If op2 register is still in use, free it.  (Might not be in use, if
                // a full-call write barrier was done, and the register was a caller-saved
                // register.)
                regMaskTP op2RM = genRegMask(op2->gtRegNum);
                if (op2RM & regSet.rsMaskUsed)
                    regSet.rsMarkRegFree(genRegMask(op2->gtRegNum));

                // This is done in WriteBarrier when (regGC != 0)
                if (regGC == 0)
                {
                    /* Free up anything that was tied up by the LHS */
                    genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);
                }
            }
            else
            {
                /* Make the target addressable */

                isWriteBarrier = gcInfo.gcIsWriteBarrierAsgNode(tree);

                if (isWriteBarrier)
                {
#if NOGC_WRITE_BARRIERS
                    /* Try to avoid RBM_TMP_0 */
                    needRegOp1 = regSet.rsNarrowHint(needRegOp1, ~RBM_TMP_0);
                    mustReg    = RegSet::EXACT_REG; // For op2
#else                                               // !NOGC_WRITE_BARRIERS
                    // This code should be generic across architectures.

                    // For the standard JIT Helper calls
                    // op1 goes into REG_ARG_0 and
                    // op2 goes into REG_ARG_1
                    //
                    needRegOp1 = RBM_ARG_0;
                    needReg    = RBM_ARG_1;
                    mustReg    = RegSet::EXACT_REG; // For op2
#endif                                              // !NOGC_WRITE_BARRIERS
                }

                needRegOp1 = regSet.rsNarrowHint(needRegOp1, ~op2->gtRsvdRegs);

                op1 = genCodeForCommaTree(op1); // Strip away any comma expression.

                addrReg = genMakeAddressable(op1, needRegOp1, RegSet::KEEP_REG, true);

#if CPU_HAS_BYTE_REGS
                /* Is the target a byte value? */
                if (varTypeIsByte(op1->TypeGet()))
                {
                    /* Must get the new value into a byte register */
                    needReg = regSet.rsNarrowHint(RBM_BYTE_REGS, needReg);
                    mustReg = RegSet::EXACT_REG;

                    if (op2->gtType >= op1->gtType)
                        op2->gtFlags |= GTF_SMALL_OK;
                }
#endif

#if NOGC_WRITE_BARRIERS
                /* For WriteBarrier we can't use REG_WRITE_BARRIER */
                if (isWriteBarrier)
                    needReg = Target::exclude_WriteBarrierReg(needReg);

                /* Also avoid using the previously computed addrReg(s) */
                bestReg = regSet.rsNarrowHint(needReg, ~addrReg);

                /* If we have a reg available to grab then use bestReg */
                if (bestReg & regSet.rsRegMaskCanGrab())
                    needReg = bestReg;

                mustReg = RegSet::EXACT_REG;
#endif

                /* Generate the RHS into a register */
                genComputeReg(op2, needReg, mustReg, RegSet::KEEP_REG);
                noway_assert(op2->InReg());

                /* Make sure the target is still addressable */
                addrReg = genKeepAddressable(op1, addrReg, genRegMask(op2->gtRegNum));
                noway_assert(op2->InReg());

                /* Does the write barrier helper do the assignment? */

                regGC = WriteBarrier(op1, op2, addrReg);

                if (regGC != 0)
                {
                    // Yes, assignment done by the WriteBarrier
                    noway_assert(isWriteBarrier);
                }
                else
                {
                    assert(!isWriteBarrier);

#ifdef _TARGET_ARM_
                    if (volat)
                    {
                        // Emit a memory barrier instruction before the store
                        instGen_MemoryBarrier();
                    }
#endif

                    /* Move the value into the target */

                    inst_TT_RV(ins_Store(op1->TypeGet()), op1, op2->gtRegNum);
                }

                /* The new value is no longer needed */

                genReleaseReg(op2);

#ifdef DEBUG
                /* Update the current liveness info */
                if (compiler->opts.varNames)
                    genUpdateLife(tree);
#endif

                // This is done in WriteBarrier when (regGC != 0)
                if (regGC == 0)
                {
                    /* Free up anything that was tied up by the LHS */
                    genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);
                }
            }

            addrReg = RBM_NONE;
            break;
    }

    noway_assert(addrReg != DUMMY_INIT(RBM_CORRUPT));
    genCodeForTreeSmpOpAsg_DONE_ASSG(tree, addrReg, REG_NA, ovfl);

LExit:
    /* For non-debuggable code, every definition of a lcl-var has
     * to be checked to see if we need to open a new scope for it.
     */
    if (lclVarNum < compiler->lvaCount)
        siCheckVarScope(lclVarNum, lclILoffs);
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************
 *
 *  Generate code to complete the assignment operation
 */

void CodeGen::genCodeForTreeSmpOpAsg_DONE_ASSG(GenTreePtr tree, regMaskTP addrReg, regNumber reg, bool ovfl)
{
    const var_types treeType = tree->TypeGet();
    GenTreePtr      op1      = tree->gtOp.gtOp1;
    GenTreePtr      op2      = tree->gtOp.gtOp2;
    noway_assert(op2);

    if (op1->gtOper == GT_LCL_VAR || op1->gtOper == GT_REG_VAR)
        genUpdateLife(op1);
    genUpdateLife(tree);

#if REDUNDANT_LOAD

    if (op1->gtOper == GT_LCL_VAR)
        regTracker.rsTrashLcl(op1->gtLclVarCommon.gtLclNum);

    /* Have we just assigned a value that is in a register? */

    if (op2->InReg() && tree->gtOper == GT_ASG)
    {
        regTracker.rsTrackRegAssign(op1, op2);
    }

#endif

    noway_assert(addrReg != 0xDEADCAFE);

    gcInfo.gcMarkRegSetNpt(addrReg);

    if (ovfl)
    {
        noway_assert(tree->gtOper == GT_ASG_ADD || tree->gtOper == GT_ASG_SUB);

        /* If it is not in a register and it is a small type, then
           we must have loaded it up from memory, done the increment,
           checked for overflow, and then stored it back to memory */

        bool ovfCheckDone = (genTypeSize(op1->TypeGet()) < sizeof(int)) && !(op1->InReg());

        if (!ovfCheckDone)
        {
            // For small sizes, reg should be set as we sign/zero extend it.

            noway_assert(genIsValidReg(reg) || genTypeSize(treeType) == sizeof(int));

            /* Currently we don't morph x=x+y into x+=y in try blocks
             * if we need overflow check, as x+y may throw an exception.
             * We can do it if x is not live on entry to the catch block.
             */
            noway_assert(!compiler->compCurBB->hasTryIndex());

            genCheckOverflow(tree);
        }
    }
}

/*****************************************************************************
 *
 *  Generate code for a special op tree
 */

void CodeGen::genCodeForTreeSpecialOp(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
    genTreeOps oper = tree->OperGet();
    regNumber  reg  = DUMMY_INIT(REG_CORRUPT);
    regMaskTP  regs = regSet.rsMaskUsed;

    noway_assert((tree->OperKind() & (GTK_CONST | GTK_LEAF | GTK_SMPOP)) == 0);

    switch (oper)
    {
        case GT_CALL:
            regs = genCodeForCall(tree->AsCall(), true);

            /* If the result is in a register, make sure it ends up in the right place */

            if (regs != RBM_NONE)
            {
                genMarkTreeInReg(tree, genRegNumFromMask(regs));
            }

            genUpdateLife(tree);
            return;

        case GT_FIELD:
            NO_WAY("should not see this operator in this phase");
            break;

        case GT_ARR_BOUNDS_CHECK:
        {
#ifdef FEATURE_ENABLE_NO_RANGE_CHECKS
            // MUST NEVER CHECK-IN WITH THIS ENABLED.
            // This is just for convenience in doing performance investigations and requires x86ret builds
            if (!JitConfig.JitNoRngChk())
#endif
                genRangeCheck(tree);
        }
            return;

        case GT_ARR_ELEM:
            genCodeForTreeSmpOp_GT_ADDR(tree, destReg, bestReg);
            return;

        case GT_CMPXCHG:
        {
#if defined(_TARGET_XARCH_)
            // cmpxchg does not have an [r/m32], imm32 encoding, so we need a register for the value operand

            // Since this is a "call", evaluate the operands from right to left.  Don't worry about spilling
            // right now, just get the trees evaluated.

            // As a friendly reminder.  IL args are evaluated left to right.

            GenTreePtr location  = tree->gtCmpXchg.gtOpLocation;  // arg1
            GenTreePtr value     = tree->gtCmpXchg.gtOpValue;     // arg2
            GenTreePtr comparand = tree->gtCmpXchg.gtOpComparand; // arg3
            regMaskTP  addrReg;

            bool isAddr = genMakeIndAddrMode(location, tree, false, /* not for LEA */
                                             RBM_ALLINT, RegSet::KEEP_REG, &addrReg);

            if (!isAddr)
            {
                genCodeForTree(location, RBM_NONE, RBM_NONE);
                assert(location->InReg());
                addrReg = genRegMask(location->gtRegNum);
                regSet.rsMarkRegUsed(location);
            }

            // We must have a reg for the Value, but it doesn't really matter which register.

            // Try to avoid EAX and the address regsiter if possible.
            genComputeReg(value, regSet.rsNarrowHint(RBM_ALLINT, RBM_EAX | addrReg), RegSet::ANY_REG, RegSet::KEEP_REG);

#ifdef DEBUG
            // cmpxchg uses EAX as an implicit operand to hold the comparand
            // We're going to destroy EAX in this operation, so we better not be keeping
            // anything important in it.
            if (RBM_EAX & regSet.rsMaskVars)
            {
                // We have a variable enregistered in EAX.  Make sure it goes dead in this tree.
                for (unsigned varNum = 0; varNum < compiler->lvaCount; ++varNum)
                {
                    const LclVarDsc& varDesc = compiler->lvaTable[varNum];
                    if (!varDesc.lvIsRegCandidate())
                        continue;
                    if (!varDesc.lvRegister)
                        continue;
                    if (isFloatRegType(varDesc.lvType))
                        continue;
                    if (varDesc.lvRegNum != REG_EAX)
                        continue;
                    // We may need to check lvOtherReg.

                    // If the variable isn't going dead during this tree, we've just trashed a local with
                    // cmpxchg.
                    noway_assert(genContainsVarDeath(value->gtNext, comparand->gtNext, varNum));

                    break;
                }
            }
#endif
            genComputeReg(comparand, RBM_EAX, RegSet::EXACT_REG, RegSet::KEEP_REG);

            // By this point we've evaluated everything.  However the odds are that we've spilled something by
            // now.  Let's recover all the registers and force them to stay.

            // Well, we just computed comparand, so it's still in EAX.
            noway_assert(comparand->gtRegNum == REG_EAX);
            regSet.rsLockUsedReg(RBM_EAX);

            // Stick it anywhere other than EAX.
            genRecoverReg(value, ~RBM_EAX, RegSet::KEEP_REG);
            reg = value->gtRegNum;
            noway_assert(reg != REG_EAX);
            regSet.rsLockUsedReg(genRegMask(reg));

            if (isAddr)
            {
                addrReg = genKeepAddressable(/*location*/ tree, addrReg, 0 /*avoidMask*/);
            }
            else
            {
                genRecoverReg(location, ~(RBM_EAX | genRegMask(reg)), RegSet::KEEP_REG);
            }

            regSet.rsUnlockUsedReg(genRegMask(reg));
            regSet.rsUnlockUsedReg(RBM_EAX);

            instGen(INS_lock);
            if (isAddr)
            {
                sched_AM(INS_cmpxchg, EA_4BYTE, reg, false, location, 0);
                genDoneAddressable(location, addrReg, RegSet::KEEP_REG);
            }
            else
            {
                instEmit_RM_RV(INS_cmpxchg, EA_4BYTE, location, reg, 0);
                genReleaseReg(location);
            }

            genReleaseReg(value);
            genReleaseReg(comparand);

            // EAX and the value register are both trashed at this point.
            regTracker.rsTrackRegTrash(REG_EAX);
            regTracker.rsTrackRegTrash(reg);

            reg = REG_EAX;

            genFlagsEqualToNone();
            break;
#else // not defined(_TARGET_XARCH_)
            NYI("GT_CMPXCHG codegen");
            break;
#endif
        }

        default:
#ifdef DEBUG
            compiler->gtDispTree(tree);
#endif
            noway_assert(!"unexpected operator");
            NO_WAY("unexpected operator");
    }

    noway_assert(reg != DUMMY_INIT(REG_CORRUPT));
    genCodeForTree_DONE(tree, reg);
}

/*****************************************************************************
 *
 *  Generate code for the given tree. tree->gtRegNum will be set to the
 *  register where the tree lives.
 *
 *  If 'destReg' is non-zero, we'll do our best to compute the value into a
 *  register that is in that register set.
 *  Use genComputeReg() if you need the tree in a specific register.
 *  Use genCompIntoFreeReg() if the register needs to be written to. Otherwise,
 *  the register can only be used for read, but not for write.
 *  Use genMakeAddressable() if you only need the tree to be accessible
 *  using a complex addressing mode, and do not necessarily need the tree
 *  materialized in a register.
 *
 *  The GCness of the register will be properly set in gcInfo.gcRegGCrefSetCur/gcInfo.gcRegByrefSetCur.
 *
 *  The register will not be marked as used. Use regSet.rsMarkRegUsed() if the
 *  register will not be consumed right away and could possibly be spilled.
 */

void CodeGen::genCodeForTree(GenTreePtr tree, regMaskTP destReg, regMaskTP bestReg)
{
#if 0
    if  (compiler->verbose)
    {
        printf("Generating code for tree ");
        Compiler::printTreeID(tree);
        printf(" destReg = 0x%x bestReg = 0x%x\n", destReg, bestReg);
    }
    genStressRegs(tree);
#endif

    noway_assert(tree);
    noway_assert(tree->gtOper != GT_STMT);
    assert(tree->IsNodeProperlySized());

    // When assigning to a enregistered local variable we receive
    // a hint that we should target the register that is used to
    // hold the enregistered local variable.
    // When receiving this hint both destReg and bestReg masks are set
    // to the register that is used by the enregistered local variable.
    //
    // However it is possible to us to have a different local variable
    // targeting the same register to become alive (and later die)
    // as we descend the expression tree.
    //
    // To handle such cases we will remove any registers that are alive from the
    // both the destReg and bestReg masks.
    //
    regMaskTP liveMask = genLiveMask(tree);

    // This removes any registers used to hold enregistered locals
    // from the destReg and bestReg masks.
    // After this either mask could become 0
    //
    destReg &= ~liveMask;
    bestReg &= ~liveMask;

    /* 'destReg' of 0 really means 'any' */

    destReg = regSet.rsUseIfZero(destReg, RBM_ALL(tree->TypeGet()));

    if (destReg != RBM_ALL(tree->TypeGet()))
        bestReg = regSet.rsUseIfZero(bestReg, destReg);

    // Long, float, and double have their own codegen functions
    switch (tree->TypeGet())
    {

        case TYP_LONG:
#if !CPU_HAS_FP_SUPPORT
        case TYP_DOUBLE:
#endif
            genCodeForTreeLng(tree, destReg, /*avoidReg*/ RBM_NONE);
            return;

#if CPU_HAS_FP_SUPPORT
        case TYP_FLOAT:
        case TYP_DOUBLE:

            // For comma nodes, we'll get back here for the last node in the comma list.
            if (tree->gtOper != GT_COMMA)
            {
                genCodeForTreeFlt(tree, RBM_ALLFLOAT, RBM_ALLFLOAT & (destReg | bestReg));
                return;
            }
            break;
#endif

#ifdef DEBUG
        case TYP_UINT:
        case TYP_ULONG:
            noway_assert(!"These types are only used as markers in GT_CAST nodes");
            break;
#endif

        default:
            break;
    }

    /* Is the value already in a register? */

    if (tree->InReg())
    {
        genCodeForTree_REG_VAR1(tree);
        return;
    }

    /* We better not have a spilled value here */

    noway_assert((tree->gtFlags & GTF_SPILLED) == 0);

    /* Figure out what kind of a node we have */

    unsigned kind = tree->OperKind();

    if (kind & GTK_CONST)
    {
        /* Handle constant nodes */

        genCodeForTreeConst(tree, destReg, bestReg);
    }
    else if (kind & GTK_LEAF)
    {
        /* Handle leaf nodes */

        genCodeForTreeLeaf(tree, destReg, bestReg);
    }
    else if (kind & GTK_SMPOP)
    {
        /* Handle 'simple' unary/binary operators */

        genCodeForTreeSmpOp(tree, destReg, bestReg);
    }
    else
    {
        /* Handle special operators */

        genCodeForTreeSpecialOp(tree, destReg, bestReg);
    }
}

/*****************************************************************************
 *
 *  Generate code for all the basic blocks in the function.
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
void CodeGen::genCodeForBBlist()
{
    unsigned   varNum;
    LclVarDsc* varDsc;

    unsigned savedStkLvl;

#ifdef DEBUG
    genInterruptibleUsed = true;
    unsigned stmtNum     = 0;
    unsigned totalCostEx = 0;
    unsigned totalCostSz = 0;

    // You have to be careful if you create basic blocks from now on
    compiler->fgSafeBasicBlockCreation = false;

    // This stress mode is not comptible with fully interruptible GC
    if (genInterruptible && compiler->opts.compStackCheckOnCall)
    {
        compiler->opts.compStackCheckOnCall = false;
    }

    // This stress mode is not comptible with fully interruptible GC
    if (genInterruptible && compiler->opts.compStackCheckOnRet)
    {
        compiler->opts.compStackCheckOnRet = false;
    }
#endif

    // Prepare the blocks for exception handling codegen: mark the blocks that needs labels.
    genPrepForEHCodegen();

    assert(!compiler->fgFirstBBScratch ||
           compiler->fgFirstBB == compiler->fgFirstBBScratch); // compiler->fgFirstBBScratch has to be first.

    /* Initialize the spill tracking logic */

    regSet.rsSpillBeg();

    /* Initialize the line# tracking logic */

    if (compiler->opts.compScopeInfo)
    {
        siInit();
    }

#ifdef _TARGET_X86_
    if (compiler->compTailCallUsed)
    {
        noway_assert(isFramePointerUsed());
        regSet.rsSetRegsModified(RBM_INT_CALLEE_SAVED & ~RBM_FPBASE);
    }
#endif

    if (compiler->opts.compDbgEnC)
    {
        noway_assert(isFramePointerUsed());
        regSet.rsSetRegsModified(RBM_INT_CALLEE_SAVED & ~RBM_FPBASE);
    }

    /* If we have any pinvoke calls, we might potentially trash everything */

    if (compiler->info.compCallUnmanaged)
    {
        noway_assert(isFramePointerUsed()); // Setup of Pinvoke frame currently requires an EBP style frame
        regSet.rsSetRegsModified(RBM_INT_CALLEE_SAVED & ~RBM_FPBASE);
    }

    /* Initialize the pointer tracking code */

    gcInfo.gcRegPtrSetInit();
    gcInfo.gcVarPtrSetInit();

    /* If any arguments live in registers, mark those regs as such */

    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        /* Is this variable a parameter assigned to a register? */

        if (!varDsc->lvIsParam || !varDsc->lvRegister)
            continue;

        /* Is the argument live on entry to the method? */

        if (!VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
            continue;

#if CPU_HAS_FP_SUPPORT
        /* Is this a floating-point argument? */

        if (varDsc->IsFloatRegType())
            continue;

        noway_assert(!varTypeIsFloating(varDsc->TypeGet()));
#endif

        /* Mark the register as holding the variable */

        if (isRegPairType(varDsc->lvType))
        {
            regTracker.rsTrackRegLclVarLng(varDsc->lvRegNum, varNum, true);

            if (varDsc->lvOtherReg != REG_STK)
                regTracker.rsTrackRegLclVarLng(varDsc->lvOtherReg, varNum, false);
        }
        else
        {
            regTracker.rsTrackRegLclVar(varDsc->lvRegNum, varNum);
        }
    }

    unsigned finallyNesting = 0;

    // Make sure a set is allocated for compiler->compCurLife (in the long case), so we can set it to empty without
    // allocation at the start of each basic block.
    VarSetOps::AssignNoCopy(compiler, compiler->compCurLife, VarSetOps::MakeEmpty(compiler));

    /*-------------------------------------------------------------------------
     *
     *  Walk the basic blocks and generate code for each one
     *
     */

    BasicBlock* block;
    BasicBlock* lblk; /* previous block */

    for (lblk = NULL, block = compiler->fgFirstBB; block != NULL; lblk = block, block = block->bbNext)
    {
#ifdef DEBUG
        if (compiler->verbose)
        {
            printf("\n=============== Generating ");
            block->dspBlockHeader(compiler, true, true);
            compiler->fgDispBBLiveness(block);
        }
#endif // DEBUG

        VARSET_TP liveSet(VarSetOps::UninitVal());

        regMaskTP gcrefRegs = 0;
        regMaskTP byrefRegs = 0;

        /* Does any other block jump to this point ? */

        if (block->bbFlags & BBF_JMP_TARGET)
        {
            /* Someone may jump here, so trash all regs */

            regTracker.rsTrackRegClr();

            genFlagsEqualToNone();
        }
        else
        {
            /* No jump, but pointers always need to get trashed for proper GC tracking */

            regTracker.rsTrackRegClrPtr();
        }

        /* No registers are used or locked on entry to a basic block */

        regSet.rsMaskUsed = RBM_NONE;
        regSet.rsMaskMult = RBM_NONE;
        regSet.rsMaskLock = RBM_NONE;

        // If we need to reserve registers such that they are not used
        // by CodeGen in this BasicBlock we do so here.
        // On the ARM when we have large frame offsets for locals we
        // will have RBM_R10 in the regSet.rsMaskResvd set,
        // additionally if a LocAlloc or alloca is used RBM_R9 is in
        // the regSet.rsMaskResvd set and we lock these registers here.
        //
        if (regSet.rsMaskResvd != RBM_NONE)
        {
            regSet.rsLockReg(regSet.rsMaskResvd);
            regSet.rsSetRegsModified(regSet.rsMaskResvd);
        }

        /* Figure out which registers hold variables on entry to this block */

        regMaskTP specialUseMask = regSet.rsMaskResvd;

        specialUseMask |= doubleAlignOrFramePointerUsed() ? RBM_SPBASE | RBM_FPBASE : RBM_SPBASE;
        regSet.ClearMaskVars();
        VarSetOps::ClearD(compiler, compiler->compCurLife);
        VarSetOps::Assign(compiler, liveSet, block->bbLiveIn);

#if FEATURE_STACK_FP_X87
        VarSetOps::AssignNoCopy(compiler, genFPregVars,
                                VarSetOps::Intersection(compiler, liveSet, compiler->optAllFPregVars));
        genFPregCnt     = VarSetOps::Count(compiler, genFPregVars);
        genFPdeadRegCnt = 0;
#endif
        gcInfo.gcResetForBB();

        genUpdateLife(liveSet); // This updates regSet.rsMaskVars with bits from any enregistered LclVars
#if FEATURE_STACK_FP_X87
        VarSetOps::IntersectionD(compiler, liveSet, compiler->optAllNonFPvars);
#endif

        // We should never enregister variables in any of the specialUseMask registers
        noway_assert((specialUseMask & regSet.rsMaskVars) == 0);

        VARSET_ITER_INIT(compiler, iter, liveSet, varIndex);
        while (iter.NextElem(&varIndex))
        {
            varNum = compiler->lvaTrackedToVarNum[varIndex];
            varDsc = compiler->lvaTable + varNum;
            assert(varDsc->lvTracked);
            /* Ignore the variable if it's not not in a reg */

            if (!varDsc->lvRegister)
                continue;
            if (isFloatRegType(varDsc->lvType))
                continue;

            /* Get hold of the index and the bitmask for the variable */
            regNumber regNum  = varDsc->lvRegNum;
            regMaskTP regMask = genRegMask(regNum);

            regSet.AddMaskVars(regMask);

            if (varDsc->lvType == TYP_REF)
                gcrefRegs |= regMask;
            else if (varDsc->lvType == TYP_BYREF)
                byrefRegs |= regMask;

            /* Mark the register holding the variable as such */

            if (varTypeIsMultiReg(varDsc))
            {
                regTracker.rsTrackRegLclVarLng(regNum, varNum, true);
                if (varDsc->lvOtherReg != REG_STK)
                {
                    regTracker.rsTrackRegLclVarLng(varDsc->lvOtherReg, varNum, false);
                    regMask |= genRegMask(varDsc->lvOtherReg);
                }
            }
            else
            {
                regTracker.rsTrackRegLclVar(regNum, varNum);
            }
        }

        gcInfo.gcPtrArgCnt = 0;

#if FEATURE_STACK_FP_X87

        regSet.rsMaskUsedFloat = regSet.rsMaskRegVarFloat = regSet.rsMaskLockedFloat = RBM_NONE;

        memset(regSet.genUsedRegsFloat, 0, sizeof(regSet.genUsedRegsFloat));
        memset(regSet.genRegVarsFloat, 0, sizeof(regSet.genRegVarsFloat));

        // Setup fp state on block entry
        genSetupStateStackFP(block);

#ifdef DEBUG
        if (compiler->verbose)
        {
            JitDumpFPState();
        }
#endif // DEBUG
#endif // FEATURE_STACK_FP_X87

        /* Make sure we keep track of what pointers are live */

        noway_assert((gcrefRegs & byrefRegs) == 0); // Something can't be both a gcref and a byref
        gcInfo.gcRegGCrefSetCur = gcrefRegs;
        gcInfo.gcRegByrefSetCur = byrefRegs;

        /* Blocks with handlerGetsXcptnObj()==true use GT_CATCH_ARG to
           represent the exception object (TYP_REF).
           We mark REG_EXCEPTION_OBJECT as holding a GC object on entry
           to the block,  it will be the first thing evaluated
           (thanks to GTF_ORDER_SIDEEFF).
         */

        if (handlerGetsXcptnObj(block->bbCatchTyp))
        {
            GenTreePtr firstStmt = block->FirstNonPhiDef();
            if (firstStmt != NULL)
            {
                GenTreePtr firstTree = firstStmt->gtStmt.gtStmtExpr;
                if (compiler->gtHasCatchArg(firstTree))
                {
                    gcInfo.gcRegGCrefSetCur |= RBM_EXCEPTION_OBJECT;
                }
            }
        }

        /* Start a new code output block */
        CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_EH_FUNCLETS
#if defined(_TARGET_ARM_)
        genInsertNopForUnwinder(block);
#endif // defined(_TARGET_ARM_)

        genUpdateCurrentFunclet(block);
#endif // FEATURE_EH_FUNCLETS

#ifdef _TARGET_XARCH_
        if (genAlignLoops && block->bbFlags & BBF_LOOP_HEAD)
        {
            getEmitter()->emitLoopAlign();
        }
#endif

#ifdef DEBUG
        if (compiler->opts.dspCode)
            printf("\n      L_M%03u_BB%02u:\n", Compiler::s_compMethodsCount, block->bbNum);
#endif

        block->bbEmitCookie = NULL;

        if (block->bbFlags & (BBF_JMP_TARGET | BBF_HAS_LABEL))
        {
            /* Mark a label and update the current set of live GC refs */

            block->bbEmitCookie =
                getEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur,
#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
                                           /*isFinally*/ block->bbFlags & BBF_FINALLY_TARGET
#else
                                           FALSE
#endif
                                           );
        }

        if (block == compiler->fgFirstColdBlock)
        {
#ifdef DEBUG
            if (compiler->verbose)
            {
                printf("\nThis is the start of the cold region of the method\n");
            }
#endif
            // We should never have a block that falls through into the Cold section
            noway_assert(!lblk->bbFallsThrough());

            // We require the block that starts the Cold section to have a label
            noway_assert(block->bbEmitCookie);
            getEmitter()->emitSetFirstColdIGCookie(block->bbEmitCookie);
        }

        /* Both stacks are always empty on entry to a basic block */

        SetStackLevel(0);
#if FEATURE_STACK_FP_X87
        genResetFPstkLevel();
#endif // FEATURE_STACK_FP_X87

        genAdjustStackLevel(block);

        savedStkLvl = genStackLevel;

        /* Tell everyone which basic block we're working on */

        compiler->compCurBB = block;

        siBeginBlock(block);

        // BBF_INTERNAL blocks don't correspond to any single IL instruction.
        if (compiler->opts.compDbgInfo && (block->bbFlags & BBF_INTERNAL) && block != compiler->fgFirstBB)
            genIPmappingAdd((IL_OFFSETX)ICorDebugInfo::NO_MAPPING, true);

        bool firstMapping = true;

        /*---------------------------------------------------------------------
         *
         *  Generate code for each statement-tree in the block
         *
         */
        CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_EH_FUNCLETS
        if (block->bbFlags & BBF_FUNCLET_BEG)
        {
            genReserveFuncletProlog(block);
        }
#endif // FEATURE_EH_FUNCLETS

        for (GenTreePtr stmt = block->FirstNonPhiDef(); stmt; stmt = stmt->gtNext)
        {
            noway_assert(stmt->gtOper == GT_STMT);

            /* Do we have a new IL-offset ? */

            if (stmt->gtStmt.gtStmtILoffsx != BAD_IL_OFFSET)
            {
                /* Create and append a new IP-mapping entry */
                genIPmappingAdd(stmt->gtStmt.gtStmt.gtStmtILoffsx, firstMapping);
                firstMapping = false;
            }

#ifdef DEBUG
            if (stmt->gtStmt.gtStmtLastILoffs != BAD_IL_OFFSET)
            {
                noway_assert(stmt->gtStmt.gtStmtLastILoffs <= compiler->info.compILCodeSize);
                if (compiler->opts.dspCode && compiler->opts.dspInstrs)
                {
                    while (genCurDispOffset <= stmt->gtStmt.gtStmtLastILoffs)
                    {
                        genCurDispOffset += dumpSingleInstr(compiler->info.compCode, genCurDispOffset, ">    ");
                    }
                }
            }
#endif // DEBUG

            /* Get hold of the statement tree */
            GenTreePtr tree = stmt->gtStmt.gtStmtExpr;

#ifdef DEBUG
            stmtNum++;
            if (compiler->verbose)
            {
                printf("\nGenerating BB%02u, stmt %u\t\t", block->bbNum, stmtNum);
                printf("Holding variables: ");
                dspRegMask(regSet.rsMaskVars);
                printf("\n\n");
                compiler->gtDispTree(compiler->opts.compDbgInfo ? stmt : tree);
                printf("\n");
#if FEATURE_STACK_FP_X87
                JitDumpFPState();
#endif

                printf("Execution Order:\n");
                for (GenTreePtr treeNode = stmt->gtStmt.gtStmtList; treeNode != NULL; treeNode = treeNode->gtNext)
                {
                    compiler->gtDispTree(treeNode, 0, NULL, true);
                }
                printf("\n");
            }
            totalCostEx += (stmt->gtCostEx * block->getBBWeight(compiler));
            totalCostSz += stmt->gtCostSz;
#endif // DEBUG

            compiler->compCurStmt = stmt;

            compiler->compCurLifeTree = NULL;
            switch (tree->gtOper)
            {
                case GT_CALL:
                    // Managed Retval under managed debugger - we need to make sure that the returned ref-type is
                    // reported as alive even though not used within the caller for managed debugger sake.  So
                    // consider the return value of the method as used if generating debuggable code.
                    genCodeForCall(tree->AsCall(), compiler->opts.MinOpts() || compiler->opts.compDbgCode);
                    genUpdateLife(tree);
                    gcInfo.gcMarkRegSetNpt(RBM_INTRET);
                    break;

                case GT_IND:
                case GT_NULLCHECK:

                    // Just do the side effects
                    genEvalSideEffects(tree);
                    break;

                default:
                    /* Generate code for the tree */

                    genCodeForTree(tree, 0);
                    break;
            }

            regSet.rsSpillChk();

            /* The value of the tree isn't used, unless it's a return stmt */

            if (tree->gtOper != GT_RETURN)
                gcInfo.gcMarkRegPtrVal(tree);

#if FEATURE_STACK_FP_X87
            genEndOfStatement();
#endif

#ifdef DEBUG
            /* Make sure we didn't bungle pointer register tracking */

            regMaskTP ptrRegs       = (gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur);
            regMaskTP nonVarPtrRegs = ptrRegs & ~regSet.rsMaskVars;

            // If return is a GC-type, clear it.  Note that if a common
            // epilog is generated (compiler->genReturnBB) it has a void return
            // even though we might return a ref.  We can't use the compRetType
            // as the determiner because something we are tracking as a byref
            // might be used as a return value of a int function (which is legal)
            if (tree->gtOper == GT_RETURN && (varTypeIsGC(compiler->info.compRetType) ||
                                              (tree->gtOp.gtOp1 != 0 && varTypeIsGC(tree->gtOp.gtOp1->TypeGet()))))
            {
                nonVarPtrRegs &= ~RBM_INTRET;
            }

            // When profiling, the first statement in a catch block will be the
            // harmless "inc" instruction (does not interfere with the exception
            // object).

            if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR) && (stmt == block->bbTreeList) &&
                (block->bbCatchTyp && handlerGetsXcptnObj(block->bbCatchTyp)))
            {
                nonVarPtrRegs &= ~RBM_EXCEPTION_OBJECT;
            }

            if (nonVarPtrRegs)
            {
                printf("Regset after tree=");
                Compiler::printTreeID(tree);
                printf(" BB%02u gcr=", block->bbNum);
                printRegMaskInt(gcInfo.gcRegGCrefSetCur & ~regSet.rsMaskVars);
                compiler->getEmitter()->emitDispRegSet(gcInfo.gcRegGCrefSetCur & ~regSet.rsMaskVars);
                printf(", byr=");
                printRegMaskInt(gcInfo.gcRegByrefSetCur & ~regSet.rsMaskVars);
                compiler->getEmitter()->emitDispRegSet(gcInfo.gcRegByrefSetCur & ~regSet.rsMaskVars);
                printf(", regVars=");
                printRegMaskInt(regSet.rsMaskVars);
                compiler->getEmitter()->emitDispRegSet(regSet.rsMaskVars);
                printf("\n");
            }

            noway_assert(nonVarPtrRegs == 0);
#endif // DEBUG

            noway_assert(stmt->gtOper == GT_STMT);

            genEnsureCodeEmitted(stmt->gtStmt.gtStmtILoffsx);

        } //-------- END-FOR each statement-tree of the current block ---------

        if (compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0))
        {
            siEndBlock(block);

            /* Is this the last block, and are there any open scopes left ? */

            bool isLastBlockProcessed = (block->bbNext == NULL);
            if (block->isBBCallAlwaysPair())
            {
                isLastBlockProcessed = (block->bbNext->bbNext == NULL);
            }

            if (isLastBlockProcessed && siOpenScopeList.scNext)
            {
                /* This assert no longer holds, because we may insert a throw
                   block to demarcate the end of a try or finally region when they
                   are at the end of the method.  It would be nice if we could fix
                   our code so that this throw block will no longer be necessary. */

                // noway_assert(block->bbCodeOffsEnd != compiler->info.compILCodeSize);

                siCloseAllOpenScopes();
            }
        }

        SubtractStackLevel(savedStkLvl);

        gcInfo.gcMarkRegSetNpt(gcrefRegs | byrefRegs);

        if (!VarSetOps::Equal(compiler, compiler->compCurLife, block->bbLiveOut))
            compiler->genChangeLife(block->bbLiveOut DEBUGARG(NULL));

        /* Both stacks should always be empty on exit from a basic block */

        noway_assert(genStackLevel == 0);
#if FEATURE_STACK_FP_X87
        noway_assert(genGetFPstkLevel() == 0);

        // Do the FPState matching that may have to be done
        genCodeForEndBlockTransitionStackFP(block);
#endif

        noway_assert(genFullPtrRegMap == false || gcInfo.gcPtrArgCnt == 0);

        /* Do we need to generate a jump or return? */

        switch (block->bbJumpKind)
        {
            case BBJ_ALWAYS:
                inst_JMP(EJ_jmp, block->bbJumpDest);
                break;

            case BBJ_RETURN:
                genExitCode(block);
                break;

            case BBJ_THROW:
                // If we have a throw at the end of a function or funclet, we need to emit another instruction
                // afterwards to help the OS unwinder determine the correct context during unwind.
                // We insert an unexecuted breakpoint instruction in several situations
                // following a throw instruction:
                // 1. If the throw is the last instruction of the function or funclet. This helps
                //    the OS unwinder determine the correct context during an unwind from the
                //    thrown exception.
                // 2. If this is this is the last block of the hot section.
                // 3. If the subsequent block is a special throw block.
                if ((block->bbNext == NULL)
#if FEATURE_EH_FUNCLETS
                    || (block->bbNext->bbFlags & BBF_FUNCLET_BEG)
#endif // FEATURE_EH_FUNCLETS
                    || (!isFramePointerUsed() && compiler->fgIsThrowHlpBlk(block->bbNext)) ||
                    block->bbNext == compiler->fgFirstColdBlock)
                {
                    instGen(INS_BREAKPOINT); // This should never get executed
                }

                break;

            case BBJ_CALLFINALLY:

#if defined(_TARGET_X86_)

                /* If we are about to invoke a finally locally from a try block,
                   we have to set the hidden slot corresponding to the finally's
                   nesting level. When invoked in response to an exception, the
                   EE usually does it.

                   We must have : BBJ_CALLFINALLY followed by a BBJ_ALWAYS.

                   This code depends on this order not being messed up.
                   We will emit :
                        mov [ebp-(n+1)],0
                        mov [ebp-  n  ],0xFC
                        push &step
                        jmp  finallyBlock

                  step: mov [ebp-  n  ],0
                        jmp leaveTarget
                  leaveTarget:
                 */

                noway_assert(isFramePointerUsed());

                // Get the nesting level which contains the finally
                compiler->fgGetNestingLevel(block, &finallyNesting);

                // The last slot is reserved for ICodeManager::FixContext(ppEndRegion)
                unsigned filterEndOffsetSlotOffs;
                filterEndOffsetSlotOffs =
                    (unsigned)(compiler->lvaLclSize(compiler->lvaShadowSPslotsVar) - (sizeof(void*)));

                unsigned curNestingSlotOffs;
                curNestingSlotOffs = (unsigned)(filterEndOffsetSlotOffs - ((finallyNesting + 1) * sizeof(void*)));

                // Zero out the slot for the next nesting level
                instGen_Store_Imm_Into_Lcl(TYP_I_IMPL, EA_PTRSIZE, 0, compiler->lvaShadowSPslotsVar,
                                           curNestingSlotOffs - sizeof(void*));

                instGen_Store_Imm_Into_Lcl(TYP_I_IMPL, EA_PTRSIZE, LCL_FINALLY_MARK, compiler->lvaShadowSPslotsVar,
                                           curNestingSlotOffs);

                // Now push the address of where the finally funclet should
                // return to directly.
                if (!(block->bbFlags & BBF_RETLESS_CALL))
                {
                    assert(block->isBBCallAlwaysPair());
                    getEmitter()->emitIns_J(INS_push_hide, block->bbNext->bbJumpDest);
                }
                else
                {
                    // EE expects a DWORD, so we give him 0
                    inst_IV(INS_push_hide, 0);
                }

                // Jump to the finally BB
                inst_JMP(EJ_jmp, block->bbJumpDest);

#elif defined(_TARGET_ARM_)

                // Now set REG_LR to the address of where the finally funclet should
                // return to directly.

                BasicBlock* bbFinallyRet;
                bbFinallyRet = NULL;

                // We don't have retless calls, since we use the BBJ_ALWAYS to point at a NOP pad where
                // we would have otherwise created retless calls.
                assert(block->isBBCallAlwaysPair());

                assert(block->bbNext != NULL);
                assert(block->bbNext->bbJumpKind == BBJ_ALWAYS);
                assert(block->bbNext->bbJumpDest != NULL);
                assert(block->bbNext->bbJumpDest->bbFlags & BBF_FINALLY_TARGET);

                bbFinallyRet = block->bbNext->bbJumpDest;
                bbFinallyRet->bbFlags |= BBF_JMP_TARGET;

                // Load the address where the finally funclet should return into LR.
                // The funclet prolog/epilog will do "push {lr}" / "pop {pc}" to do
                // the return.
                genMov32RelocatableDisplacement(bbFinallyRet, REG_LR);
                regTracker.rsTrackRegTrash(REG_LR);

                // Jump to the finally BB
                inst_JMP(EJ_jmp, block->bbJumpDest);
#else
                NYI("TARGET");
#endif

                // The BBJ_ALWAYS is used because the BBJ_CALLFINALLY can't point to the
                // jump target using bbJumpDest - that is already used to point
                // to the finally block. So just skip past the BBJ_ALWAYS unless the
                // block is RETLESS.
                if (!(block->bbFlags & BBF_RETLESS_CALL))
                {
                    assert(block->isBBCallAlwaysPair());

                    lblk  = block;
                    block = block->bbNext;
                }
                break;

#ifdef _TARGET_ARM_

            case BBJ_EHCATCHRET:
                // set r0 to the address the VM should return to after the catch
                genMov32RelocatableDisplacement(block->bbJumpDest, REG_R0);
                regTracker.rsTrackRegTrash(REG_R0);

                __fallthrough;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
                genReserveFuncletEpilog(block);
                break;

#else // _TARGET_ARM_

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
            case BBJ_EHCATCHRET:
                break;

#endif // _TARGET_ARM_

            case BBJ_NONE:
            case BBJ_COND:
            case BBJ_SWITCH:
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }

#ifdef DEBUG
        compiler->compCurBB = 0;
#endif

    } //------------------ END-FOR each block of the method -------------------

    /* Nothing is live at this point */
    genUpdateLife(VarSetOps::MakeEmpty(compiler));

    /* Finalize the spill  tracking logic */

    regSet.rsSpillEnd();

    /* Finalize the temp   tracking logic */

    compiler->tmpEnd();

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("\n# ");
        printf("totalCostEx = %6d, totalCostSz = %5d ", totalCostEx, totalCostSz);
        printf("%s\n", compiler->info.compFullName);
    }
#endif
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************
 *
 *  Generate code for a long operation.
 *  needReg is a recommendation of which registers to use for the tree.
 *  For partially enregistered longs, the tree will be marked as in a register
 *    without loading the stack part into a register. Note that only leaf
 *    nodes (or if gtEffectiveVal() == leaf node) may be marked as partially
 *    enregistered so that we can know the memory location of the other half.
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
void CodeGen::genCodeForTreeLng(GenTreePtr tree, regMaskTP needReg, regMaskTP avoidReg)
{
    genTreeOps oper;
    unsigned   kind;

    regPairNo regPair = DUMMY_INIT(REG_PAIR_CORRUPT);
    regMaskTP addrReg;
    regNumber regLo;
    regNumber regHi;

    noway_assert(tree);
    noway_assert(tree->gtOper != GT_STMT);
    noway_assert(genActualType(tree->gtType) == TYP_LONG);

    /* Figure out what kind of a node we have */

    oper = tree->OperGet();
    kind = tree->OperKind();

    if (tree->InReg())
    {
    REG_VAR_LONG:
        regPair = tree->gtRegPair;

        gcInfo.gcMarkRegSetNpt(genRegPairMask(regPair));

        goto DONE;
    }

    /* Is this a constant node? */

    if (kind & GTK_CONST)
    {
        __int64 lval;

        /* Pick a register pair for the value */

        regPair = regSet.rsPickRegPair(needReg);

        /* Load the value into the registers */
        CLANG_FORMAT_COMMENT_ANCHOR;

#if !CPU_HAS_FP_SUPPORT
        if (oper == GT_CNS_DBL)
        {
            noway_assert(sizeof(__int64) == sizeof(double));

            noway_assert(sizeof(tree->gtLngCon.gtLconVal) == sizeof(tree->gtDblCon.gtDconVal));

            lval = *(__int64*)(&tree->gtDblCon.gtDconVal);
        }
        else
#endif
        {
            noway_assert(oper == GT_CNS_LNG);

            lval = tree->gtLngCon.gtLconVal;
        }

        genSetRegToIcon(genRegPairLo(regPair), int(lval));
        genSetRegToIcon(genRegPairHi(regPair), int(lval >> 32));
        goto DONE;
    }

    /* Is this a leaf node? */

    if (kind & GTK_LEAF)
    {
        switch (oper)
        {
            case GT_LCL_VAR:

#if REDUNDANT_LOAD

                /*  This case has to consider the case in which an int64 LCL_VAR
                 *  may both be enregistered and also have a cached copy of itself
                 *  in a different set of registers.
                 *  We want to return the registers that have the most in common
                 *  with the needReg mask
                 */

                /*  Does the var have a copy of itself in the cached registers?
                 *  And are these cached registers both free?
                 *  If so use these registers if they match any needReg.
                 */

                regPair = regTracker.rsLclIsInRegPair(tree->gtLclVarCommon.gtLclNum);

                if ((regPair != REG_PAIR_NONE) && ((regSet.rsRegMaskFree() & needReg) == needReg) &&
                    ((genRegPairMask(regPair) & needReg) != RBM_NONE))
                {
                    goto DONE;
                }

                /*  Does the variable live in a register?
                 *  If so use these registers.
                 */
                if (genMarkLclVar(tree))
                    goto REG_VAR_LONG;

                /*  If tree is not an enregistered variable then
                 *  be sure to use any cached register that contain
                 *  a copy of this local variable
                 */
                if (regPair != REG_PAIR_NONE)
                {
                    goto DONE;
                }
#endif
                goto MEM_LEAF;

            case GT_LCL_FLD:

                // We only use GT_LCL_FLD for lvDoNotEnregister vars, so we don't have
                // to worry about it being enregistered.
                noway_assert(compiler->lvaTable[tree->gtLclFld.gtLclNum].lvRegister == 0);
                goto MEM_LEAF;

            case GT_CLS_VAR:
            MEM_LEAF:

                /* Pick a register pair for the value */

                regPair = regSet.rsPickRegPair(needReg);

                /* Load the value into the registers */

                instruction loadIns;

                loadIns = ins_Load(TYP_INT); // INS_ldr
                regLo   = genRegPairLo(regPair);
                regHi   = genRegPairHi(regPair);

#if CPU_LOAD_STORE_ARCH
                {
                    regNumber regAddr = regSet.rsGrabReg(RBM_ALLINT);
                    inst_RV_TT(INS_lea, regAddr, tree, 0);
                    regTracker.rsTrackRegTrash(regAddr);

                    if (regLo != regAddr)
                    {
                        // assert(regLo != regAddr);  // forced by if statement
                        getEmitter()->emitIns_R_R_I(loadIns, EA_4BYTE, regLo, regAddr, 0);
                        getEmitter()->emitIns_R_R_I(loadIns, EA_4BYTE, regHi, regAddr, 4);
                    }
                    else
                    {
                        // assert(regHi != regAddr);  // implied by regpair property and the if statement
                        getEmitter()->emitIns_R_R_I(loadIns, EA_4BYTE, regHi, regAddr, 4);
                        getEmitter()->emitIns_R_R_I(loadIns, EA_4BYTE, regLo, regAddr, 0);
                    }
                }
#else
                inst_RV_TT(loadIns, regLo, tree, 0);
                inst_RV_TT(loadIns, regHi, tree, 4);
#endif

#ifdef _TARGET_ARM_
                if ((oper == GT_CLS_VAR) && (tree->gtFlags & GTF_IND_VOLATILE))
                {
                    // Emit a memory barrier instruction after the load
                    instGen_MemoryBarrier();
                }
#endif

                regTracker.rsTrackRegTrash(regLo);
                regTracker.rsTrackRegTrash(regHi);

                goto DONE;

            default:
#ifdef DEBUG
                compiler->gtDispTree(tree);
#endif
                noway_assert(!"unexpected leaf");
        }
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_SMPOP)
    {
        instruction insLo;
        instruction insHi;
        bool        doLo;
        bool        doHi;
        bool        setCarry = false;
        int         helper;

        GenTreePtr op1 = tree->gtOp.gtOp1;
        GenTreePtr op2 = tree->gtGetOp2IfPresent();

        switch (oper)
        {
            case GT_ASG:
            {
                unsigned lclVarNum    = compiler->lvaCount;
                unsigned lclVarILoffs = DUMMY_INIT(0);

                /* Is the target a local ? */

                if (op1->gtOper == GT_LCL_VAR)
                {
                    unsigned   varNum = op1->gtLclVarCommon.gtLclNum;
                    LclVarDsc* varDsc;

                    noway_assert(varNum < compiler->lvaCount);
                    varDsc = compiler->lvaTable + varNum;

                    // No dead stores, (with min opts we may have dead stores)
                    noway_assert(!varDsc->lvTracked || compiler->opts.MinOpts() || !(op1->gtFlags & GTF_VAR_DEATH));

                    /* For non-debuggable code, every definition of a lcl-var has
                     * to be checked to see if we need to open a new scope for it.
                     * Remember the local var info to call siCheckVarScope
                     * AFTER codegen of the assignment.
                     */
                    if (compiler->opts.compScopeInfo && !compiler->opts.compDbgCode &&
                        (compiler->info.compVarScopesCount > 0))
                    {
                        lclVarNum    = varNum;
                        lclVarILoffs = op1->gtLclVar.gtLclILoffs;
                    }

                    /* Has the variable been assigned to a register (pair) ? */

                    if (genMarkLclVar(op1))
                    {
                        noway_assert(op1->InReg());
                        regPair = op1->gtRegPair;
                        regLo   = genRegPairLo(regPair);
                        regHi   = genRegPairHi(regPair);
                        noway_assert(regLo != regHi);

                        /* Is the value being assigned a constant? */

                        if (op2->gtOper == GT_CNS_LNG)
                        {
                            /* Move the value into the target */

                            genMakeRegPairAvailable(regPair);

                            instruction ins;
                            if (regLo == REG_STK)
                            {
                                ins = ins_Store(TYP_INT);
                            }
                            else
                            {
                                // Always do the stack first (in case it grabs a register it can't
                                // clobber regLo this way)
                                if (regHi == REG_STK)
                                {
                                    inst_TT_IV(ins_Store(TYP_INT), op1, (int)(op2->gtLngCon.gtLconVal >> 32), 4);
                                }
                                ins = INS_mov;
                            }
                            inst_TT_IV(ins, op1, (int)(op2->gtLngCon.gtLconVal), 0);

                            // The REG_STK case has already been handled
                            if (regHi != REG_STK)
                            {
                                ins = INS_mov;
                                inst_TT_IV(ins, op1, (int)(op2->gtLngCon.gtLconVal >> 32), 4);
                            }

                            goto DONE_ASSG_REGS;
                        }

                        /* Compute the RHS into desired register pair */

                        if (regHi != REG_STK)
                        {
                            genComputeRegPair(op2, regPair, avoidReg, RegSet::KEEP_REG);
                            noway_assert(op2->InReg());
                            noway_assert(op2->gtRegPair == regPair);
                        }
                        else
                        {
                            regPairNo curPair;
                            regNumber curLo;
                            regNumber curHi;

                            genComputeRegPair(op2, REG_PAIR_NONE, avoidReg, RegSet::KEEP_REG);

                            noway_assert(op2->InReg());

                            curPair = op2->gtRegPair;
                            curLo   = genRegPairLo(curPair);
                            curHi   = genRegPairHi(curPair);

                            /* move high first, target is on stack */
                            inst_TT_RV(ins_Store(TYP_INT), op1, curHi, 4);

                            if (regLo != curLo)
                            {
                                if ((regSet.rsMaskUsed & genRegMask(regLo)) && (regLo != curHi))
                                    regSet.rsSpillReg(regLo);
                                inst_RV_RV(INS_mov, regLo, curLo, TYP_LONG);
                                regTracker.rsTrackRegCopy(regLo, curLo);
                            }
                        }

                        genReleaseRegPair(op2);
                        goto DONE_ASSG_REGS;
                    }
                }

                /* Is the value being assigned a constant? */

                if (op2->gtOper == GT_CNS_LNG)
                {
                    /* Make the target addressable */

                    addrReg = genMakeAddressable(op1, needReg, RegSet::KEEP_REG);

                    /* Move the value into the target */

                    inst_TT_IV(ins_Store(TYP_INT), op1, (int)(op2->gtLngCon.gtLconVal), 0);
                    inst_TT_IV(ins_Store(TYP_INT), op1, (int)(op2->gtLngCon.gtLconVal >> 32), 4);

                    genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);

                    goto LAsgExit;
                }

#if 0
                /* Catch a case where can avoid generating op reg, mem. Better pairing
                 * from
                 *     mov regHi, mem
                 *     op  regHi, reg
                 *
                 * To avoid problems with order of evaluation, only do this if op2 is
                 * a non-enregistered local variable
                 */

                if (GenTree::OperIsCommutative(oper) &&
                    op1->gtOper == GT_LCL_VAR &&
                    op2->gtOper == GT_LCL_VAR)
                {
                    regPair = regTracker.rsLclIsInRegPair(op2->gtLclVarCommon.gtLclNum);

                    /* Is op2 a non-enregistered local variable? */
                    if (regPair == REG_PAIR_NONE)
                    {
                        regPair = regTracker.rsLclIsInRegPair(op1->gtLclVarCommon.gtLclNum);

                        /* Is op1 an enregistered local variable? */
                        if (regPair != REG_PAIR_NONE)
                        {
                            /* Swap the operands */
                            GenTreePtr op = op1;
                            op1 = op2;
                            op2 = op;
                        }
                    }
                }
#endif

                /* Eliminate worthless assignment "lcl = lcl" */

                if (op2->gtOper == GT_LCL_VAR && op1->gtOper == GT_LCL_VAR &&
                    op2->gtLclVarCommon.gtLclNum == op1->gtLclVarCommon.gtLclNum)
                {
                    genUpdateLife(op2);
                    goto LAsgExit;
                }

                if (op2->gtOper == GT_CAST && TYP_ULONG == op2->CastToType() && op2->CastFromType() <= TYP_INT &&
                    // op1,op2 need to be materialized in the correct order.
                    (tree->gtFlags & GTF_REVERSE_OPS))
                {
                    /* Generate the small RHS into a register pair */

                    GenTreePtr smallOpr = op2->gtOp.gtOp1;

                    genComputeReg(smallOpr, 0, RegSet::ANY_REG, RegSet::KEEP_REG);

                    /* Make the target addressable */

                    addrReg = genMakeAddressable(op1, 0, RegSet::KEEP_REG, true);

                    /* Make sure everything is still addressable */

                    genRecoverReg(smallOpr, 0, RegSet::KEEP_REG);
                    noway_assert(smallOpr->InReg());
                    regHi   = smallOpr->gtRegNum;
                    addrReg = genKeepAddressable(op1, addrReg, genRegMask(regHi));

                    // conv.ovf.u8 could overflow if the original number was negative
                    if (op2->gtOverflow())
                    {
                        noway_assert((op2->gtFlags & GTF_UNSIGNED) ==
                                     0);                              // conv.ovf.u8.un should be bashed to conv.u8.un
                        instGen_Compare_Reg_To_Zero(EA_4BYTE, regHi); // set flags
                        emitJumpKind jmpLTS = genJumpKindForOper(GT_LT, CK_SIGNED);
                        genJumpToThrowHlpBlk(jmpLTS, SCK_OVERFLOW);
                    }

                    /* Move the value into the target */

                    inst_TT_RV(ins_Store(TYP_INT), op1, regHi, 0);
                    inst_TT_IV(ins_Store(TYP_INT), op1, 0, 4); // Store 0 in hi-word

                    /* Free up anything that was tied up by either side */

                    genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);
                    genReleaseReg(smallOpr);

#if REDUNDANT_LOAD
                    if (op1->gtOper == GT_LCL_VAR)
                    {
                        /* clear this local from reg table */
                        regTracker.rsTrashLclLong(op1->gtLclVarCommon.gtLclNum);

                        /* mark RHS registers as containing the local var */
                        regTracker.rsTrackRegLclVarLng(regHi, op1->gtLclVarCommon.gtLclNum, true);
                    }
#endif
                    goto LAsgExit;
                }

                /* Is the LHS more complex than the RHS? */

                if (tree->gtFlags & GTF_REVERSE_OPS)
                {
                    /* Generate the RHS into a register pair */

                    genComputeRegPair(op2, REG_PAIR_NONE, avoidReg | op1->gtUsedRegs, RegSet::KEEP_REG);
                    noway_assert(op2->InReg());

                    /* Make the target addressable */
                    op1     = genCodeForCommaTree(op1);
                    addrReg = genMakeAddressable(op1, 0, RegSet::KEEP_REG);

                    /* Make sure the RHS register hasn't been spilled */

                    genRecoverRegPair(op2, REG_PAIR_NONE, RegSet::KEEP_REG);
                }
                else
                {
                    /* Make the target addressable */

                    op1     = genCodeForCommaTree(op1);
                    addrReg = genMakeAddressable(op1, RBM_ALLINT & ~op2->gtRsvdRegs, RegSet::KEEP_REG, true);

                    /* Generate the RHS into a register pair */

                    genComputeRegPair(op2, REG_PAIR_NONE, avoidReg, RegSet::KEEP_REG, false);
                }

                /* Lock 'op2' and make sure 'op1' is still addressable */

                noway_assert(op2->InReg());
                regPair = op2->gtRegPair;

                addrReg = genKeepAddressable(op1, addrReg, genRegPairMask(regPair));

                /* Move the value into the target */

                inst_TT_RV(ins_Store(TYP_INT), op1, genRegPairLo(regPair), 0);
                inst_TT_RV(ins_Store(TYP_INT), op1, genRegPairHi(regPair), 4);

                /* Free up anything that was tied up by either side */

                genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);
                genReleaseRegPair(op2);

            DONE_ASSG_REGS:

#if REDUNDANT_LOAD

                if (op1->gtOper == GT_LCL_VAR)
                {
                    /* Clear this local from reg table */

                    regTracker.rsTrashLclLong(op1->gtLclVarCommon.gtLclNum);

                    if ((op2->InReg()) &&
                        /* constant has precedence over local */
                        //                    rsRegValues[op2->gtRegNum].rvdKind != RV_INT_CNS &&
                        tree->gtOper == GT_ASG)
                    {
                        regNumber regNo;

                        /* mark RHS registers as containing the local var */

                        regNo = genRegPairLo(op2->gtRegPair);
                        if (regNo != REG_STK)
                            regTracker.rsTrackRegLclVarLng(regNo, op1->gtLclVarCommon.gtLclNum, true);

                        regNo = genRegPairHi(op2->gtRegPair);
                        if (regNo != REG_STK)
                        {
                            /* For partially enregistered longs, we might have
                               stomped on op2's hiReg */
                            if (!(op1->InReg()) || regNo != genRegPairLo(op1->gtRegPair))
                            {
                                regTracker.rsTrackRegLclVarLng(regNo, op1->gtLclVarCommon.gtLclNum, false);
                            }
                        }
                    }
                }
#endif

            LAsgExit:

                genUpdateLife(op1);
                genUpdateLife(tree);

                /* For non-debuggable code, every definition of a lcl-var has
                 * to be checked to see if we need to open a new scope for it.
                 */
                if (lclVarNum < compiler->lvaCount)
                    siCheckVarScope(lclVarNum, lclVarILoffs);
            }
                return;

            case GT_SUB:
                insLo    = INS_sub;
                insHi    = INS_SUBC;
                setCarry = true;
                goto BINOP_OVF;
            case GT_ADD:
                insLo    = INS_add;
                insHi    = INS_ADDC;
                setCarry = true;
                goto BINOP_OVF;

                bool ovfl;

            BINOP_OVF:
                ovfl = tree->gtOverflow();
                goto _BINOP;

            case GT_AND:
                insLo = insHi = INS_AND;
                goto BINOP;
            case GT_OR:
                insLo = insHi = INS_OR;
                goto BINOP;
            case GT_XOR:
                insLo = insHi = INS_XOR;
                goto BINOP;

            BINOP:
                ovfl = false;
                goto _BINOP;

            _BINOP:

                /* The following makes an assumption about gtSetEvalOrder(this) */

                noway_assert((tree->gtFlags & GTF_REVERSE_OPS) == 0);

                /* Special case: check for "(long(intval) << 32) | longval" */

                if (oper == GT_OR && op1->gtOper == GT_LSH)
                {
                    GenTreePtr lshLHS = op1->gtOp.gtOp1;
                    GenTreePtr lshRHS = op1->gtOp.gtOp2;

                    if (lshLHS->gtOper == GT_CAST && lshRHS->gtOper == GT_CNS_INT && lshRHS->gtIntCon.gtIconVal == 32 &&
                        genTypeSize(TYP_INT) == genTypeSize(lshLHS->CastFromType()))
                    {

                        /* Throw away the cast of the shift operand. */

                        op1 = lshLHS->gtCast.CastOp();

                        /* Special case: check op2 for "ulong(intval)" */
                        if ((op2->gtOper == GT_CAST) && (op2->CastToType() == TYP_ULONG) &&
                            genTypeSize(TYP_INT) == genTypeSize(op2->CastFromType()))
                        {
                            /* Throw away the cast of the second operand. */

                            op2 = op2->gtCast.CastOp();
                            goto SIMPLE_OR_LONG;
                        }
                        /* Special case: check op2 for "long(intval) & 0xFFFFFFFF" */
                        else if (op2->gtOper == GT_AND)
                        {
                            GenTreePtr andLHS;
                            andLHS = op2->gtOp.gtOp1;
                            GenTreePtr andRHS;
                            andRHS = op2->gtOp.gtOp2;

                            if (andLHS->gtOper == GT_CAST && andRHS->gtOper == GT_CNS_LNG &&
                                andRHS->gtLngCon.gtLconVal == 0x00000000FFFFFFFF &&
                                genTypeSize(TYP_INT) == genTypeSize(andLHS->CastFromType()))
                            {
                                /* Throw away the cast of the second operand. */

                                op2 = andLHS->gtCast.CastOp();

                            SIMPLE_OR_LONG:
                                // Load the high DWORD, ie. op1

                                genCodeForTree(op1, needReg & ~op2->gtRsvdRegs);

                                noway_assert(op1->InReg());
                                regHi = op1->gtRegNum;
                                regSet.rsMarkRegUsed(op1);

                                // Load the low DWORD, ie. op2

                                genCodeForTree(op2, needReg & ~genRegMask(regHi));

                                noway_assert(op2->InReg());
                                regLo = op2->gtRegNum;

                                /* Make sure regHi is still around. Also, force
                                   regLo to be excluded in case regLo==regHi */

                                genRecoverReg(op1, ~genRegMask(regLo), RegSet::FREE_REG);
                                regHi = op1->gtRegNum;

                                regPair = gen2regs2pair(regLo, regHi);
                                goto DONE;
                            }
                        }

                        /*  Generate the following sequence:
                               Prepare op1 (discarding shift)
                               Compute op2 into some regpair
                               OR regpairhi, op1
                         */

                        /* First, make op1 addressable */

                        /* tempReg must avoid both needReg, op2->RsvdRegs and regSet.rsMaskResvd.

                           It appears incorrect to exclude needReg as we are not ensuring that the reg pair into
                           which the long value is computed is from needReg.  But at this point the safest fix is
                           to exclude regSet.rsMaskResvd.

                           Note that needReg could be the set of free registers (excluding reserved ones).  If we don't
                           exclude regSet.rsMaskResvd, the expression below will have the effect of trying to choose a
                           reg from
                           reserved set which is bound to fail.  To prevent that we avoid regSet.rsMaskResvd.
                         */
                        regMaskTP tempReg = RBM_ALLINT & ~needReg & ~op2->gtRsvdRegs & ~avoidReg & ~regSet.rsMaskResvd;

                        addrReg = genMakeAddressable(op1, tempReg, RegSet::KEEP_REG);

                        genCompIntoFreeRegPair(op2, avoidReg, RegSet::KEEP_REG);

                        noway_assert(op2->InReg());
                        regPair = op2->gtRegPair;
                        regHi   = genRegPairHi(regPair);

                        /* The operand might have interfered with the address */

                        addrReg = genKeepAddressable(op1, addrReg, genRegPairMask(regPair));

                        /* Now compute the result */

                        inst_RV_TT(insHi, regHi, op1, 0);

                        regTracker.rsTrackRegTrash(regHi);

                        /* Free up anything that was tied up by the LHS */

                        genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);

                        /* The result is where the second operand is sitting */

                        genRecoverRegPair(op2, REG_PAIR_NONE, RegSet::FREE_REG);

                        regPair = op2->gtRegPair;
                        goto DONE;
                    }
                }

                /* Special case: check for "longval | (long(intval) << 32)" */

                if (oper == GT_OR && op2->gtOper == GT_LSH)
                {
                    GenTreePtr lshLHS = op2->gtOp.gtOp1;
                    GenTreePtr lshRHS = op2->gtOp.gtOp2;

                    if (lshLHS->gtOper == GT_CAST && lshRHS->gtOper == GT_CNS_INT && lshRHS->gtIntCon.gtIconVal == 32 &&
                        genTypeSize(TYP_INT) == genTypeSize(lshLHS->CastFromType()))

                    {
                        /* We throw away the cast of the shift operand. */

                        op2 = lshLHS->gtCast.CastOp();

                        /* Special case: check op1 for "long(intval) & 0xFFFFFFFF" */

                        if (op1->gtOper == GT_AND)
                        {
                            GenTreePtr andLHS = op1->gtOp.gtOp1;
                            GenTreePtr andRHS = op1->gtOp.gtOp2;

                            if (andLHS->gtOper == GT_CAST && andRHS->gtOper == GT_CNS_LNG &&
                                andRHS->gtLngCon.gtLconVal == 0x00000000FFFFFFFF &&
                                genTypeSize(TYP_INT) == genTypeSize(andLHS->CastFromType()))
                            {
                                /* Throw away the cast of the first operand. */

                                op1 = andLHS->gtCast.CastOp();

                                // Load the low DWORD, ie. op1

                                genCodeForTree(op1, needReg & ~op2->gtRsvdRegs);

                                noway_assert(op1->InReg());
                                regLo = op1->gtRegNum;
                                regSet.rsMarkRegUsed(op1);

                                // Load the high DWORD, ie. op2

                                genCodeForTree(op2, needReg & ~genRegMask(regLo));

                                noway_assert(op2->InReg());
                                regHi = op2->gtRegNum;

                                /* Make sure regLo is still around. Also, force
                                   regHi to be excluded in case regLo==regHi */

                                genRecoverReg(op1, ~genRegMask(regHi), RegSet::FREE_REG);
                                regLo = op1->gtRegNum;

                                regPair = gen2regs2pair(regLo, regHi);
                                goto DONE;
                            }
                        }

                        /*  Generate the following sequence:
                              Compute op1 into some regpair
                              Make op2 (ignoring shift) addressable
                              OR regPairHi, op2
                         */

                        // First, generate the first operand into some register

                        genCompIntoFreeRegPair(op1, avoidReg | op2->gtRsvdRegs, RegSet::KEEP_REG);
                        noway_assert(op1->InReg());

                        /* Make the second operand addressable */

                        addrReg = genMakeAddressable(op2, needReg, RegSet::KEEP_REG);

                        /* Make sure the result is in a free register pair */

                        genRecoverRegPair(op1, REG_PAIR_NONE, RegSet::KEEP_REG);
                        regPair = op1->gtRegPair;
                        regHi   = genRegPairHi(regPair);

                        /* The operand might have interfered with the address */

                        addrReg = genKeepAddressable(op2, addrReg, genRegPairMask(regPair));

                        /* Compute the new value */

                        inst_RV_TT(insHi, regHi, op2, 0);

                        /* The value in the high register has been trashed */

                        regTracker.rsTrackRegTrash(regHi);

                        goto DONE_OR;
                    }
                }

                /* Generate the first operand into registers */

                if ((genCountBits(needReg) == 2) && ((regSet.rsRegMaskFree() & needReg) == needReg) &&
                    ((op2->gtRsvdRegs & needReg) == RBM_NONE) && (!(tree->gtFlags & GTF_ASG)))
                {
                    regPair = regSet.rsPickRegPair(needReg);
                    genComputeRegPair(op1, regPair, avoidReg | op2->gtRsvdRegs, RegSet::KEEP_REG);
                }
                else
                {
                    genCompIntoFreeRegPair(op1, avoidReg | op2->gtRsvdRegs, RegSet::KEEP_REG);
                }
                noway_assert(op1->InReg());
                regMaskTP op1Mask;
                regPair = op1->gtRegPair;
                op1Mask = genRegPairMask(regPair);

                /* Make the second operand addressable */
                regMaskTP needReg2;
                needReg2 = regSet.rsNarrowHint(needReg, ~op1Mask);
                addrReg  = genMakeAddressable(op2, needReg2, RegSet::KEEP_REG);

                // TODO: If 'op1' got spilled and 'op2' happens to be
                // TODO: in a register, and we have add/mul/and/or/xor,
                // TODO: reverse the operands since we can perform the
                // TODO: operation directly with the spill temp, e.g.
                // TODO: 'add regHi, [temp]'.

                /* Make sure the result is in a free register pair */

                genRecoverRegPair(op1, REG_PAIR_NONE, RegSet::KEEP_REG);
                regPair = op1->gtRegPair;
                op1Mask = genRegPairMask(regPair);

                regLo = genRegPairLo(regPair);
                regHi = genRegPairHi(regPair);

                /* Make sure that we don't spill regLo/regHi below */
                regSet.rsLockUsedReg(op1Mask);

                /* The operand might have interfered with the address */

                addrReg = genKeepAddressable(op2, addrReg);

                /* The value in the register pair is about to be trashed */

                regTracker.rsTrackRegTrash(regLo);
                regTracker.rsTrackRegTrash(regHi);

                /* Compute the new value */

                doLo = true;
                doHi = true;

                if (op2->gtOper == GT_CNS_LNG)
                {
                    __int64 icon = op2->gtLngCon.gtLconVal;

                    /* Check for "(op1 AND -1)" and "(op1 [X]OR 0)" */

                    switch (oper)
                    {
                        case GT_AND:
                            if ((int)(icon) == -1)
                                doLo = false;
                            if ((int)(icon >> 32) == -1)
                                doHi = false;

                            if (!(icon & I64(0x00000000FFFFFFFF)))
                            {
                                genSetRegToIcon(regLo, 0);
                                doLo = false;
                            }

                            if (!(icon & I64(0xFFFFFFFF00000000)))
                            {
                                /* Just to always set low first*/

                                if (doLo)
                                {
                                    inst_RV_TT(insLo, regLo, op2, 0);
                                    doLo = false;
                                }
                                genSetRegToIcon(regHi, 0);
                                doHi = false;
                            }

                            break;

                        case GT_OR:
                        case GT_XOR:
                            if (!(icon & I64(0x00000000FFFFFFFF)))
                                doLo = false;
                            if (!(icon & I64(0xFFFFFFFF00000000)))
                                doHi = false;
                            break;
                        default:
                            break;
                    }
                }

                // Fix 383813 X86/ARM ILGEN
                // Fix 383793 ARM ILGEN
                // Fix 383911 ARM ILGEN
                regMaskTP newMask;
                newMask = addrReg & ~op1Mask;
                regSet.rsLockUsedReg(newMask);

                if (doLo)
                {
                    insFlags flagsLo = setCarry ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
                    inst_RV_TT(insLo, regLo, op2, 0, EA_4BYTE, flagsLo);
                }
                if (doHi)
                {
                    insFlags flagsHi = ovfl ? INS_FLAGS_SET : INS_FLAGS_DONT_CARE;
                    inst_RV_TT(insHi, regHi, op2, 4, EA_4BYTE, flagsHi);
                }

                regSet.rsUnlockUsedReg(newMask);
                regSet.rsUnlockUsedReg(op1Mask);

            DONE_OR:

                /* Free up anything that was tied up by the LHS */

                genDoneAddressable(op2, addrReg, RegSet::KEEP_REG);

                /* The result is where the first operand is sitting */

                genRecoverRegPair(op1, REG_PAIR_NONE, RegSet::FREE_REG);

                regPair = op1->gtRegPair;

                if (ovfl)
                    genCheckOverflow(tree);

                goto DONE;

            case GT_UMOD:

                regPair = genCodeForLongModInt(tree, needReg);
                goto DONE;

            case GT_MUL:

                /* Special case: both operands promoted from int */

                assert(tree->gtIsValid64RsltMul());

                /* Change to an integer multiply temporarily */

                tree->gtType = TYP_INT;

                noway_assert(op1->gtOper == GT_CAST && op2->gtOper == GT_CAST);
                tree->gtOp.gtOp1 = op1->gtCast.CastOp();
                tree->gtOp.gtOp2 = op2->gtCast.CastOp();

                assert(tree->gtFlags & GTF_MUL_64RSLT);

#if defined(_TARGET_X86_)
                // imul on x86 requires EDX:EAX
                genComputeReg(tree, (RBM_EAX | RBM_EDX), RegSet::EXACT_REG, RegSet::FREE_REG);
                noway_assert(tree->InReg());
                noway_assert(tree->gtRegNum == REG_EAX); // Also REG_EDX is setup with hi 32-bits
#elif defined(_TARGET_ARM_)
                genComputeReg(tree, needReg, RegSet::ANY_REG, RegSet::FREE_REG);
                noway_assert(tree->InReg());
#else
                assert(!"Unsupported target for 64-bit multiply codegen");
#endif

                /* Restore gtType, op1 and op2 from the change above */

                tree->gtType     = TYP_LONG;
                tree->gtOp.gtOp1 = op1;
                tree->gtOp.gtOp2 = op2;

#if defined(_TARGET_X86_)
                /* The result is now in EDX:EAX */
                regPair = REG_PAIR_EAXEDX;
#elif defined(_TARGET_ARM_)
                regPair = tree->gtRegPair;
#endif
                goto DONE;

            case GT_LSH:
                helper = CORINFO_HELP_LLSH;
                goto SHIFT;
            case GT_RSH:
                helper = CORINFO_HELP_LRSH;
                goto SHIFT;
            case GT_RSZ:
                helper = CORINFO_HELP_LRSZ;
                goto SHIFT;

            SHIFT:

                noway_assert(op1->gtType == TYP_LONG);
                noway_assert(genActualType(op2->gtType) == TYP_INT);

                /* Is the second operand a constant? */

                if (op2->gtOper == GT_CNS_INT)
                {
                    unsigned int count = op2->gtIntCon.gtIconVal;

                    /* Compute the left operand into a free register pair */

                    genCompIntoFreeRegPair(op1, avoidReg | op2->gtRsvdRegs, RegSet::FREE_REG);
                    noway_assert(op1->InReg());

                    regPair = op1->gtRegPair;
                    regLo   = genRegPairLo(regPair);
                    regHi   = genRegPairHi(regPair);

                    /* Assume the value in the register pair is trashed. In some cases, though,
                       a register might be set to zero, and we can use that information to improve
                       some code generation.
                    */

                    regTracker.rsTrackRegTrash(regLo);
                    regTracker.rsTrackRegTrash(regHi);

                    /* Generate the appropriate shift instructions */

                    switch (oper)
                    {
                        case GT_LSH:
                            if (count == 0)
                            {
                                // regHi, regLo are correct
                            }
                            else if (count < 32)
                            {
#if defined(_TARGET_XARCH_)
                                inst_RV_RV_IV(INS_shld, EA_4BYTE, regHi, regLo, count);
#elif defined(_TARGET_ARM_)
                                inst_RV_SH(INS_SHIFT_LEFT_LOGICAL, EA_4BYTE, regHi, count);
                                getEmitter()->emitIns_R_R_R_I(INS_OR, EA_4BYTE, regHi, regHi, regLo, 32 - count,
                                                              INS_FLAGS_DONT_CARE, INS_OPTS_LSR);
#else  // _TARGET_*
                                NYI("INS_shld");
#endif // _TARGET_*
                                inst_RV_SH(INS_SHIFT_LEFT_LOGICAL, EA_4BYTE, regLo, count);
                            }
                            else // count >= 32
                            {
                                assert(count >= 32);
                                if (count < 64)
                                {
#if defined(_TARGET_ARM_)
                                    if (count == 32)
                                    {
                                        // mov low dword into high dword (i.e. shift left by 32-bits)
                                        inst_RV_RV(INS_mov, regHi, regLo);
                                    }
                                    else
                                    {
                                        assert(count > 32 && count < 64);
                                        getEmitter()->emitIns_R_R_I(INS_SHIFT_LEFT_LOGICAL, EA_4BYTE, regHi, regLo,
                                                                    count - 32);
                                    }
#else  // _TARGET_*
                                    // mov low dword into high dword (i.e. shift left by 32-bits)
                                    inst_RV_RV(INS_mov, regHi, regLo);
                                    if (count > 32)
                                    {
                                        // Shift high dword left by count - 32
                                        inst_RV_SH(INS_SHIFT_LEFT_LOGICAL, EA_4BYTE, regHi, count - 32);
                                    }
#endif // _TARGET_*
                                }
                                else // count >= 64
                                {
                                    assert(count >= 64);
                                    genSetRegToIcon(regHi, 0);
                                }
                                genSetRegToIcon(regLo, 0);
                            }
                            break;

                        case GT_RSH:
                            if (count == 0)
                            {
                                // regHi, regLo are correct
                            }
                            else if (count < 32)
                            {
#if defined(_TARGET_XARCH_)
                                inst_RV_RV_IV(INS_shrd, EA_4BYTE, regLo, regHi, count);
#elif defined(_TARGET_ARM_)
                                inst_RV_SH(INS_SHIFT_RIGHT_LOGICAL, EA_4BYTE, regLo, count);
                                getEmitter()->emitIns_R_R_R_I(INS_OR, EA_4BYTE, regLo, regLo, regHi, 32 - count,
                                                              INS_FLAGS_DONT_CARE, INS_OPTS_LSL);
#else  // _TARGET_*
                                NYI("INS_shrd");
#endif // _TARGET_*
                                inst_RV_SH(INS_SHIFT_RIGHT_ARITHM, EA_4BYTE, regHi, count);
                            }
                            else // count >= 32
                            {
                                assert(count >= 32);
                                if (count < 64)
                                {
#if defined(_TARGET_ARM_)
                                    if (count == 32)
                                    {
                                        // mov high dword into low dword (i.e. shift right by 32-bits)
                                        inst_RV_RV(INS_mov, regLo, regHi);
                                    }
                                    else
                                    {
                                        assert(count > 32 && count < 64);
                                        getEmitter()->emitIns_R_R_I(INS_SHIFT_RIGHT_ARITHM, EA_4BYTE, regLo, regHi,
                                                                    count - 32);
                                    }
#else  // _TARGET_*
                                    // mov high dword into low dword (i.e. shift right by 32-bits)
                                    inst_RV_RV(INS_mov, regLo, regHi);
                                    if (count > 32)
                                    {
                                        // Shift low dword right by count - 32
                                        inst_RV_SH(INS_SHIFT_RIGHT_ARITHM, EA_4BYTE, regLo, count - 32);
                                    }
#endif // _TARGET_*
                                }

                                // Propagate sign bit in high dword
                                inst_RV_SH(INS_SHIFT_RIGHT_ARITHM, EA_4BYTE, regHi, 31);

                                if (count >= 64)
                                {
                                    // Propagate the sign from the high dword
                                    inst_RV_RV(INS_mov, regLo, regHi, TYP_INT);
                                }
                            }
                            break;

                        case GT_RSZ:
                            if (count == 0)
                            {
                                // regHi, regLo are correct
                            }
                            else if (count < 32)
                            {
#if defined(_TARGET_XARCH_)
                                inst_RV_RV_IV(INS_shrd, EA_4BYTE, regLo, regHi, count);
#elif defined(_TARGET_ARM_)
                                inst_RV_SH(INS_SHIFT_RIGHT_LOGICAL, EA_4BYTE, regLo, count);
                                getEmitter()->emitIns_R_R_R_I(INS_OR, EA_4BYTE, regLo, regLo, regHi, 32 - count,
                                                              INS_FLAGS_DONT_CARE, INS_OPTS_LSL);
#else  // _TARGET_*
                                NYI("INS_shrd");
#endif // _TARGET_*
                                inst_RV_SH(INS_SHIFT_RIGHT_LOGICAL, EA_4BYTE, regHi, count);
                            }
                            else // count >= 32
                            {
                                assert(count >= 32);
                                if (count < 64)
                                {
#if defined(_TARGET_ARM_)
                                    if (count == 32)
                                    {
                                        // mov high dword into low dword (i.e. shift right by 32-bits)
                                        inst_RV_RV(INS_mov, regLo, regHi);
                                    }
                                    else
                                    {
                                        assert(count > 32 && count < 64);
                                        getEmitter()->emitIns_R_R_I(INS_SHIFT_RIGHT_LOGICAL, EA_4BYTE, regLo, regHi,
                                                                    count - 32);
                                    }
#else  // _TARGET_*
                                    // mov high dword into low dword (i.e. shift right by 32-bits)
                                    inst_RV_RV(INS_mov, regLo, regHi);
                                    if (count > 32)
                                    {
                                        // Shift low dword right by count - 32
                                        inst_RV_SH(INS_SHIFT_RIGHT_LOGICAL, EA_4BYTE, regLo, count - 32);
                                    }
#endif // _TARGET_*
                                }
                                else // count >= 64
                                {
                                    assert(count >= 64);
                                    genSetRegToIcon(regLo, 0);
                                }
                                genSetRegToIcon(regHi, 0);
                            }
                            break;

                        default:
                            noway_assert(!"Illegal oper for long shift");
                            break;
                    }

                    goto DONE_SHF;
                }

                /* Which operand are we supposed to compute first? */

                assert((RBM_SHIFT_LNG & RBM_LNGARG_0) == 0);

                if (tree->gtFlags & GTF_REVERSE_OPS)
                {
                    /* The second operand can't be a constant */

                    noway_assert(op2->gtOper != GT_CNS_INT);

                    /* Load the shift count, hopefully into RBM_SHIFT */
                    RegSet::ExactReg exactReg;
                    if ((RBM_SHIFT_LNG & op1->gtRsvdRegs) == 0)
                        exactReg = RegSet::EXACT_REG;
                    else
                        exactReg = RegSet::ANY_REG;
                    genComputeReg(op2, RBM_SHIFT_LNG, exactReg, RegSet::KEEP_REG);

                    /* Compute the left operand into REG_LNGARG_0 */

                    genComputeRegPair(op1, REG_LNGARG_0, avoidReg, RegSet::KEEP_REG, false);
                    noway_assert(op1->InReg());

                    /* Lock op1 so that it doesn't get trashed */

                    regSet.rsLockUsedReg(RBM_LNGARG_0);

                    /* Make sure the shift count wasn't displaced */

                    genRecoverReg(op2, RBM_SHIFT_LNG, RegSet::KEEP_REG);

                    /* Lock op2 */

                    regSet.rsLockUsedReg(RBM_SHIFT_LNG);
                }
                else
                {
                    /* Compute the left operand into REG_LNGARG_0 */

                    genComputeRegPair(op1, REG_LNGARG_0, avoidReg, RegSet::KEEP_REG, false);
                    noway_assert(op1->InReg());

                    /* Compute the shift count into RBM_SHIFT */

                    genComputeReg(op2, RBM_SHIFT_LNG, RegSet::EXACT_REG, RegSet::KEEP_REG);

                    /* Lock op2 */

                    regSet.rsLockUsedReg(RBM_SHIFT_LNG);

                    /* Make sure the value hasn't been displaced */

                    genRecoverRegPair(op1, REG_LNGARG_0, RegSet::KEEP_REG);

                    /* Lock op1 so that it doesn't get trashed */

                    regSet.rsLockUsedReg(RBM_LNGARG_0);
                }

#ifndef _TARGET_X86_
                /* The generic helper is a C-routine and so it follows the full ABI */
                {
                    /* Spill any callee-saved registers which are being used */
                    regMaskTP spillRegs = RBM_CALLEE_TRASH & regSet.rsMaskUsed;

                    /* But do not spill our argument registers. */
                    spillRegs &= ~(RBM_LNGARG_0 | RBM_SHIFT_LNG);

                    if (spillRegs)
                    {
                        regSet.rsSpillRegs(spillRegs);
                    }
                }
#endif // !_TARGET_X86_

                /* Perform the shift by calling a helper function */

                noway_assert(op1->gtRegPair == REG_LNGARG_0);
                noway_assert(op2->gtRegNum == REG_SHIFT_LNG);
                noway_assert((regSet.rsMaskLock & (RBM_LNGARG_0 | RBM_SHIFT_LNG)) == (RBM_LNGARG_0 | RBM_SHIFT_LNG));

                genEmitHelperCall(helper,
                                  0,         // argSize
                                  EA_8BYTE); // retSize

#ifdef _TARGET_X86_
                /* The value in the register pair is trashed */

                regTracker.rsTrackRegTrash(genRegPairLo(REG_LNGARG_0));
                regTracker.rsTrackRegTrash(genRegPairHi(REG_LNGARG_0));
#else  // _TARGET_X86_
                /* The generic helper is a C-routine and so it follows the full ABI */
                regTracker.rsTrackRegMaskTrash(RBM_CALLEE_TRASH);
#endif // _TARGET_X86_

                /* Release both operands */

                regSet.rsUnlockUsedReg(RBM_LNGARG_0 | RBM_SHIFT_LNG);
                genReleaseRegPair(op1);
                genReleaseReg(op2);

            DONE_SHF:

                noway_assert(op1->InReg());
                regPair = op1->gtRegPair;
                goto DONE;

            case GT_NEG:
            case GT_NOT:

                /* Generate the operand into some register pair */

                genCompIntoFreeRegPair(op1, avoidReg, RegSet::FREE_REG);
                noway_assert(op1->InReg());

                regPair = op1->gtRegPair;

                /* Figure out which registers the value is in */

                regLo = genRegPairLo(regPair);
                regHi = genRegPairHi(regPair);

                /* The value in the register pair is about to be trashed */

                regTracker.rsTrackRegTrash(regLo);
                regTracker.rsTrackRegTrash(regHi);

                /* Unary "neg": negate the value  in the register pair */
                if (oper == GT_NEG)
                {
#ifdef _TARGET_ARM_

                    // ARM doesn't have an opcode that sets the carry bit like
                    // x86, so we can't use neg/addc/neg.  Instead we use subtract
                    // with carry.  Too bad this uses an extra register.

                    // Lock regLo and regHi so we don't pick them, and then pick
                    // a third register to be our 0.
                    regMaskTP regPairMask = genRegMask(regLo) | genRegMask(regHi);
                    regSet.rsLockReg(regPairMask);
                    regMaskTP regBest = RBM_ALLINT & ~avoidReg;
                    regNumber regZero = genGetRegSetToIcon(0, regBest);
                    regSet.rsUnlockReg(regPairMask);

                    inst_RV_IV(INS_rsb, regLo, 0, EA_4BYTE, INS_FLAGS_SET);
                    getEmitter()->emitIns_R_R_R_I(INS_sbc, EA_4BYTE, regHi, regZero, regHi, 0);

#elif defined(_TARGET_XARCH_)

                    inst_RV(INS_NEG, regLo, TYP_LONG);
                    inst_RV_IV(INS_ADDC, regHi, 0, emitActualTypeSize(TYP_LONG));
                    inst_RV(INS_NEG, regHi, TYP_LONG);
#else
                    NYI("GT_NEG on TYP_LONG");
#endif
                }
                else
                {
                    /* Unary "not": flip all the bits in the register pair */

                    inst_RV(INS_NOT, regLo, TYP_LONG);
                    inst_RV(INS_NOT, regHi, TYP_LONG);
                }

                goto DONE;

            case GT_IND:
            case GT_NULLCHECK:
            {
                regMaskTP tmpMask;
                int       hiFirst;

                regMaskTP availMask = RBM_ALLINT & ~needReg;

                /* Make sure the operand is addressable */

                addrReg = genMakeAddressable(tree, availMask, RegSet::FREE_REG);

                GenTreePtr addr = oper == GT_IND ? op1 : tree;

                /* Pick a register for the value */

                regPair = regSet.rsPickRegPair(needReg);
                tmpMask = genRegPairMask(regPair);

                /* Is there any overlap between the register pair and the address? */

                hiFirst = FALSE;

                if (tmpMask & addrReg)
                {
                    /* Does one or both of the target registers overlap? */

                    if ((tmpMask & addrReg) != tmpMask)
                    {
                        /* Only one register overlaps */

                        noway_assert(genMaxOneBit(tmpMask & addrReg) == TRUE);

                        /* If the low register overlaps, load the upper half first */

                        if (addrReg & genRegMask(genRegPairLo(regPair)))
                            hiFirst = TRUE;
                    }
                    else
                    {
                        regMaskTP regFree;

                        /* The register completely overlaps with the address */

                        noway_assert(genMaxOneBit(tmpMask & addrReg) == FALSE);

                        /* Can we pick another pair easily? */

                        regFree = regSet.rsRegMaskFree() & ~addrReg;
                        if (needReg)
                            regFree &= needReg;

                        /* More than one free register available? */

                        if (regFree && !genMaxOneBit(regFree))
                        {
                            regPair = regSet.rsPickRegPair(regFree);
                            tmpMask = genRegPairMask(regPair);
                        }
                        else
                        {
                            // printf("Overlap: needReg = %08X\n", needReg);

                            // Reg-prediction won't allow this
                            noway_assert((regSet.rsMaskVars & addrReg) == 0);

                            // Grab one fresh reg, and use any one of addrReg

                            if (regFree) // Try to follow 'needReg'
                                regLo = regSet.rsGrabReg(regFree);
                            else // Pick any reg besides addrReg
                                regLo = regSet.rsGrabReg(RBM_ALLINT & ~addrReg);

                            unsigned  regBit = 0x1;
                            regNumber regNo;

                            for (regNo = REG_INT_FIRST; regNo <= REG_INT_LAST; regNo = REG_NEXT(regNo), regBit <<= 1)
                            {
                                // Found one of addrReg. Use it.
                                if (regBit & addrReg)
                                    break;
                            }
                            noway_assert(genIsValidReg(regNo)); // Should have found regNo

                            regPair = gen2regs2pair(regLo, regNo);
                            tmpMask = genRegPairMask(regPair);
                        }
                    }
                }

                /* Make sure the value is still addressable */

                noway_assert(genStillAddressable(tree));

                /* Figure out which registers the value is in */

                regLo = genRegPairLo(regPair);
                regHi = genRegPairHi(regPair);

                /* The value in the register pair is about to be trashed */

                regTracker.rsTrackRegTrash(regLo);
                regTracker.rsTrackRegTrash(regHi);

                /* Load the target registers from where the value is */

                if (hiFirst)
                {
                    inst_RV_AT(ins_Load(TYP_INT), EA_4BYTE, TYP_INT, regHi, addr, 4);
                    regSet.rsLockReg(genRegMask(regHi));
                    inst_RV_AT(ins_Load(TYP_INT), EA_4BYTE, TYP_INT, regLo, addr, 0);
                    regSet.rsUnlockReg(genRegMask(regHi));
                }
                else
                {
                    inst_RV_AT(ins_Load(TYP_INT), EA_4BYTE, TYP_INT, regLo, addr, 0);
                    regSet.rsLockReg(genRegMask(regLo));
                    inst_RV_AT(ins_Load(TYP_INT), EA_4BYTE, TYP_INT, regHi, addr, 4);
                    regSet.rsUnlockReg(genRegMask(regLo));
                }

#ifdef _TARGET_ARM_
                if (tree->gtFlags & GTF_IND_VOLATILE)
                {
                    // Emit a memory barrier instruction after the load
                    instGen_MemoryBarrier();
                }
#endif

                genUpdateLife(tree);
                genDoneAddressable(tree, addrReg, RegSet::FREE_REG);
            }
                goto DONE;

            case GT_CAST:

                /* What are we casting from? */

                switch (op1->gtType)
                {
                    case TYP_BOOL:
                    case TYP_BYTE:
                    case TYP_CHAR:
                    case TYP_SHORT:
                    case TYP_INT:
                    case TYP_UBYTE:
                    case TYP_BYREF:
                    {
                        regMaskTP hiRegMask;
                        regMaskTP loRegMask;

                        // For an unsigned cast we don't need to sign-extend the 32 bit value
                        if (tree->gtFlags & GTF_UNSIGNED)
                        {
                            // Does needReg have exactly two bits on and thus
                            // specifies the exact register pair that we want to use
                            if (!genMaxOneBit(needReg))
                            {
                                regPair = regSet.rsFindRegPairNo(needReg);
                                if (needReg != genRegPairMask(regPair))
                                    goto ANY_FREE_REG_UNSIGNED;
                                loRegMask = genRegMask(genRegPairLo(regPair));
                                if ((loRegMask & regSet.rsRegMaskCanGrab()) == 0)
                                    goto ANY_FREE_REG_UNSIGNED;
                                hiRegMask = genRegMask(genRegPairHi(regPair));
                            }
                            else
                            {
                            ANY_FREE_REG_UNSIGNED:
                                loRegMask = needReg;
                                hiRegMask = needReg;
                            }

                            genComputeReg(op1, loRegMask, RegSet::ANY_REG, RegSet::KEEP_REG);
                            noway_assert(op1->InReg());

                            regLo     = op1->gtRegNum;
                            loRegMask = genRegMask(regLo);
                            regSet.rsLockUsedReg(loRegMask);
                            regHi = regSet.rsPickReg(hiRegMask);
                            regSet.rsUnlockUsedReg(loRegMask);

                            regPair = gen2regs2pair(regLo, regHi);

                            // Move 0 to the higher word of the ULong
                            genSetRegToIcon(regHi, 0, TYP_INT);

                            /* We can now free up the operand */
                            genReleaseReg(op1);

                            goto DONE;
                        }
#ifdef _TARGET_XARCH_
                        /* Cast of 'int' to 'long' --> Use cdq if EAX,EDX are available
                           and we need the result to be in those registers.
                           cdq is smaller so we use it for SMALL_CODE
                        */

                        if ((needReg & (RBM_EAX | RBM_EDX)) == (RBM_EAX | RBM_EDX) &&
                            (regSet.rsRegMaskFree() & RBM_EDX))
                        {
                            genCodeForTree(op1, RBM_EAX);
                            regSet.rsMarkRegUsed(op1);

                            /* If we have to spill EDX, might as well use the faster
                               sar as the spill will increase code size anyway */

                            if (op1->gtRegNum != REG_EAX || !(regSet.rsRegMaskFree() & RBM_EDX))
                            {
                                hiRegMask = regSet.rsRegMaskFree();
                                goto USE_SAR_FOR_CAST;
                            }

                            regSet.rsGrabReg(RBM_EDX);
                            regTracker.rsTrackRegTrash(REG_EDX);

                            /* Convert the int in EAX into a long in EDX:EAX */

                            instGen(INS_cdq);

                            /* The result is in EDX:EAX */

                            regPair = REG_PAIR_EAXEDX;
                        }
                        else
#endif
                        {
                            /* use the sar instruction to sign-extend a 32-bit integer */

                            // Does needReg have exactly two bits on and thus
                            // specifies the exact register pair that we want to use
                            if (!genMaxOneBit(needReg))
                            {
                                regPair = regSet.rsFindRegPairNo(needReg);
                                if ((regPair == REG_PAIR_NONE) || (needReg != genRegPairMask(regPair)))
                                    goto ANY_FREE_REG_SIGNED;
                                loRegMask = genRegMask(genRegPairLo(regPair));
                                if ((loRegMask & regSet.rsRegMaskCanGrab()) == 0)
                                    goto ANY_FREE_REG_SIGNED;
                                hiRegMask = genRegMask(genRegPairHi(regPair));
                            }
                            else
                            {
                            ANY_FREE_REG_SIGNED:
                                loRegMask = needReg;
                                hiRegMask = RBM_NONE;
                            }

                            genComputeReg(op1, loRegMask, RegSet::ANY_REG, RegSet::KEEP_REG);
#ifdef _TARGET_XARCH_
                        USE_SAR_FOR_CAST:
#endif
                            noway_assert(op1->InReg());

                            regLo     = op1->gtRegNum;
                            loRegMask = genRegMask(regLo);
                            regSet.rsLockUsedReg(loRegMask);
                            regHi = regSet.rsPickReg(hiRegMask);
                            regSet.rsUnlockUsedReg(loRegMask);

                            regPair = gen2regs2pair(regLo, regHi);

#ifdef _TARGET_ARM_
                            /* Copy the lo32 bits from regLo to regHi and sign-extend it */
                            // Use one instruction instead of two
                            getEmitter()->emitIns_R_R_I(INS_SHIFT_RIGHT_ARITHM, EA_4BYTE, regHi, regLo, 31);
#else
                            /* Copy the lo32 bits from regLo to regHi and sign-extend it */
                            inst_RV_RV(INS_mov, regHi, regLo, TYP_INT);
                            inst_RV_SH(INS_SHIFT_RIGHT_ARITHM, EA_4BYTE, regHi, 31);
#endif

                            /* The value in the upper register is trashed */

                            regTracker.rsTrackRegTrash(regHi);
                        }

                        /* We can now free up the operand */
                        genReleaseReg(op1);

                        // conv.ovf.u8 could overflow if the original number was negative
                        if (tree->gtOverflow() && TYP_ULONG == tree->CastToType())
                        {
                            regNumber hiReg = genRegPairHi(regPair);
                            instGen_Compare_Reg_To_Zero(EA_4BYTE, hiReg); // set flags
                            emitJumpKind jmpLTS = genJumpKindForOper(GT_LT, CK_SIGNED);
                            genJumpToThrowHlpBlk(jmpLTS, SCK_OVERFLOW);
                        }
                    }
                        goto DONE;

                    case TYP_FLOAT:
                    case TYP_DOUBLE:

#if 0
                /* Load the FP value onto the coprocessor stack */

                genCodeForTreeFlt(op1);

                /* Allocate a temp for the long value */

                temp = compiler->tmpGetTemp(TYP_LONG);

                /* Store the FP value into the temp */

                inst_FS_ST(INS_fistpl, sizeof(int), temp, 0);
                genFPstkLevel--;

                /* Pick a register pair for the value */

                regPair  = regSet.rsPickRegPair(needReg);

                /* Figure out which registers the value is in */

                regLo = genRegPairLo(regPair);
                regHi = genRegPairHi(regPair);

                /* The value in the register pair is about to be trashed */

                regTracker.rsTrackRegTrash(regLo);
                regTracker.rsTrackRegTrash(regHi);

                /* Load the converted value into the registers */

                inst_RV_ST(INS_mov, EA_4BYTE, regLo, temp, 0);
                inst_RV_ST(INS_mov, EA_4BYTE, regHi, temp, 4);

                /* We no longer need the temp */

                compiler->tmpRlsTemp(temp);
                goto DONE;
#else
                        NO_WAY("Cast from TYP_FLOAT or TYP_DOUBLE supposed to be done via a helper call");
                        break;
#endif
                    case TYP_LONG:
                    case TYP_ULONG:
                    {
                        noway_assert(tree->gtOverflow()); // conv.ovf.u8 or conv.ovf.i8

                        genComputeRegPair(op1, REG_PAIR_NONE, RBM_ALLINT & ~needReg, RegSet::FREE_REG);
                        regPair = op1->gtRegPair;

                        // Do we need to set the sign-flag, or can we checked if it is set?
                        // and not do this "test" if so.

                        if (op1->InReg())
                        {
                            regNumber hiReg = genRegPairHi(op1->gtRegPair);
                            noway_assert(hiReg != REG_STK);
                            instGen_Compare_Reg_To_Zero(EA_4BYTE, hiReg); // set flags
                        }
                        else
                        {
                            inst_TT_IV(INS_cmp, op1, 0, sizeof(int));
                        }

                        emitJumpKind jmpLTS = genJumpKindForOper(GT_LT, CK_SIGNED);
                        genJumpToThrowHlpBlk(jmpLTS, SCK_OVERFLOW);
                    }
                        goto DONE;

                    default:
#ifdef DEBUG
                        compiler->gtDispTree(tree);
#endif
                        NO_WAY("unexpected cast to long");
                }
                break;

            case GT_RETURN:

                /* TODO:
                 * This code is cloned from the regular processing of GT_RETURN values.  We have to remember to
                 * call genPInvokeMethodEpilog anywhere that we have a GT_RETURN statement.  We should really
                 * generate trees for the PInvoke prolog and epilog so we can remove these special cases.
                 */

                // TODO: this should be done AFTER we called exit mon so that
                //       we are sure that we don't have to keep 'this' alive

                if (compiler->info.compCallUnmanaged && (compiler->compCurBB == compiler->genReturnBB))
                {
                    /* either it's an "empty" statement or the return statement
                       of a synchronized method
                     */

                    genPInvokeMethodEpilog();
                }

#if CPU_LONG_USES_REGPAIR
                /* There must be a long return value */

                noway_assert(op1);

                /* Evaluate the return value into EDX:EAX */

                genEvalIntoFreeRegPair(op1, REG_LNGRET, avoidReg);

                noway_assert(op1->InReg());
                noway_assert(op1->gtRegPair == REG_LNGRET);

#else
                NYI("64-bit return");
#endif

#ifdef PROFILING_SUPPORTED
                // The profiling hook does not trash registers, so it's safe to call after we emit the code for
                // the GT_RETURN tree.

                if (compiler->compCurBB == compiler->genReturnBB)
                {
                    genProfilingLeaveCallback();
                }
#endif
                return;

            case GT_QMARK:
                noway_assert(!"inliner-generated ?: for longs NYI");
                NO_WAY("inliner-generated ?: for longs NYI");
                break;

            case GT_COMMA:

                if (tree->gtFlags & GTF_REVERSE_OPS)
                {
                    // Generate op2
                    genCodeForTreeLng(op2, needReg, avoidReg);
                    genUpdateLife(op2);

                    noway_assert(op2->InReg());

                    regSet.rsMarkRegPairUsed(op2);

                    // Do side effects of op1
                    genEvalSideEffects(op1);

                    // Recover op2 if spilled
                    genRecoverRegPair(op2, REG_PAIR_NONE, RegSet::KEEP_REG);

                    genReleaseRegPair(op2);

                    genUpdateLife(tree);

                    regPair = op2->gtRegPair;
                }
                else
                {
                    noway_assert((tree->gtFlags & GTF_REVERSE_OPS) == 0);

                    /* Generate side effects of the first operand */

                    genEvalSideEffects(op1);
                    genUpdateLife(op1);

                    /* Is the value of the second operand used? */

                    if (tree->gtType == TYP_VOID)
                    {
                        /* The right operand produces no result */

                        genEvalSideEffects(op2);
                        genUpdateLife(tree);
                        return;
                    }

                    /* Generate the second operand, i.e. the 'real' value */

                    genCodeForTreeLng(op2, needReg, avoidReg);

                    /* The result of 'op2' is also the final result */

                    regPair = op2->gtRegPair;
                }

                goto DONE;

            case GT_BOX:
            {
                /* Generate the  operand, i.e. the 'real' value */

                genCodeForTreeLng(op1, needReg, avoidReg);

                /* The result of 'op1' is also the final result */

                regPair = op1->gtRegPair;
            }

                goto DONE;

            case GT_NOP:
                if (op1 == NULL)
                    return;

                genCodeForTreeLng(op1, needReg, avoidReg);
                regPair = op1->gtRegPair;
                goto DONE;

            default:
                break;
        }

#ifdef DEBUG
        compiler->gtDispTree(tree);
#endif
        noway_assert(!"unexpected 64-bit operator");
    }

    /* See what kind of a special operator we have here */

    switch (oper)
    {
        regMaskTP retMask;
        case GT_CALL:
            retMask = genCodeForCall(tree->AsCall(), true);
            if (retMask == RBM_NONE)
                regPair = REG_PAIR_NONE;
            else
                regPair = regSet.rsFindRegPairNo(retMask);
            break;

        default:
#ifdef DEBUG
            compiler->gtDispTree(tree);
#endif
            NO_WAY("unexpected long operator");
    }

DONE:

    genUpdateLife(tree);

    /* Here we've computed the value of 'tree' into 'regPair' */

    noway_assert(regPair != DUMMY_INIT(REG_PAIR_CORRUPT));

    genMarkTreeInRegPair(tree, regPair);
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************
 *
 *  Generate code for a mod of a long by an int.
 */

regPairNo CodeGen::genCodeForLongModInt(GenTreePtr tree, regMaskTP needReg)
{
#ifdef _TARGET_X86_

    regPairNo regPair;
    regMaskTP addrReg;

    genTreeOps oper = tree->OperGet();
    GenTreePtr op1  = tree->gtOp.gtOp1;
    GenTreePtr op2  = tree->gtOp.gtOp2;

    /* Codegen only for Unsigned MOD */
    noway_assert(oper == GT_UMOD);

    /* op2 must be a long constant in the range 2 to 0x3fffffff */

    noway_assert((op2->gtOper == GT_CNS_LNG) && (op2->gtLngCon.gtLconVal >= 2) &&
                 (op2->gtLngCon.gtLconVal <= 0x3fffffff));
    int val = (int)op2->gtLngCon.gtLconVal;

    op2->ChangeOperConst(GT_CNS_INT); // it's effectively an integer constant

    op2->gtType             = TYP_INT;
    op2->gtIntCon.gtIconVal = val;

    /* Which operand are we supposed to compute first? */

    if (tree->gtFlags & GTF_REVERSE_OPS)
    {
        /* Compute the second operand into a scratch register, other
           than EAX or EDX */

        needReg = regSet.rsMustExclude(needReg, RBM_PAIR_TMP);

        /* Special case: if op2 is a local var we are done */

        if (op2->gtOper == GT_LCL_VAR || op2->gtOper == GT_LCL_FLD || op2->gtOper == GT_CLS_VAR)
        {
            addrReg = genMakeRvalueAddressable(op2, needReg, RegSet::KEEP_REG, false);
        }
        else
        {
            genComputeReg(op2, needReg, RegSet::ANY_REG, RegSet::KEEP_REG);

            noway_assert(op2->InReg());
            addrReg = genRegMask(op2->gtRegNum);
        }

        /* Compute the first operand into EAX:EDX */

        genComputeRegPair(op1, REG_PAIR_TMP, RBM_NONE, RegSet::KEEP_REG, true);
        noway_assert(op1->InReg());
        noway_assert(op1->gtRegPair == REG_PAIR_TMP);

        /* And recover the second argument while locking the first one */

        addrReg = genKeepAddressable(op2, addrReg, RBM_PAIR_TMP);
    }
    else
    {
        /* Compute the first operand into EAX:EDX */

        genComputeRegPair(op1, REG_PAIR_EAXEDX, RBM_NONE, RegSet::KEEP_REG, true);
        noway_assert(op1->InReg());
        noway_assert(op1->gtRegPair == REG_PAIR_TMP);

        /* Compute the second operand into a scratch register, other
           than EAX or EDX */

        needReg = regSet.rsMustExclude(needReg, RBM_PAIR_TMP);

        /* Special case: if op2 is a local var we are done */

        if (op2->gtOper == GT_LCL_VAR || op2->gtOper == GT_LCL_FLD || op2->gtOper == GT_CLS_VAR)
        {
            addrReg = genMakeRvalueAddressable(op2, needReg, RegSet::KEEP_REG, false);
        }
        else
        {
            genComputeReg(op2, needReg, RegSet::ANY_REG, RegSet::KEEP_REG);

            noway_assert(op2->InReg());
            addrReg = genRegMask(op2->gtRegNum);
        }

        /* Recover the first argument */

        genRecoverRegPair(op1, REG_PAIR_EAXEDX, RegSet::KEEP_REG);

        /* And recover the second argument while locking the first one */

        addrReg = genKeepAddressable(op2, addrReg, RBM_PAIR_TMP);
    }

    /* At this point, EAX:EDX contains the 64bit dividend and op2->gtRegNum
       contains the 32bit divisor.  We want to generate the following code:

       ==========================
       Unsigned (GT_UMOD)

       cmp edx, op2->gtRegNum
       jb  lab_no_overflow

       mov temp, eax
       mov eax, edx
       xor edx, edx
       div op2->g2RegNum
       mov eax, temp

       lab_no_overflow:
       idiv
       ==========================
       This works because (a * 2^32 + b) % c = ((a % c) * 2^32 + b) % c
    */

    BasicBlock* lab_no_overflow = genCreateTempLabel();

    // grab a temporary register other than eax, edx, and op2->gtRegNum

    regNumber tempReg = regSet.rsGrabReg(RBM_ALLINT & ~(RBM_PAIR_TMP | genRegMask(op2->gtRegNum)));

    // EAX and tempReg will be trashed by the mov instructions.  Doing
    // this early won't hurt, and might prevent confusion in genSetRegToIcon.

    regTracker.rsTrackRegTrash(REG_PAIR_TMP_LO);
    regTracker.rsTrackRegTrash(tempReg);

    inst_RV_RV(INS_cmp, REG_PAIR_TMP_HI, op2->gtRegNum);
    inst_JMP(EJ_jb, lab_no_overflow);

    inst_RV_RV(INS_mov, tempReg, REG_PAIR_TMP_LO, TYP_INT);
    inst_RV_RV(INS_mov, REG_PAIR_TMP_LO, REG_PAIR_TMP_HI, TYP_INT);
    genSetRegToIcon(REG_PAIR_TMP_HI, 0, TYP_INT);
    inst_TT(INS_UNSIGNED_DIVIDE, op2);
    inst_RV_RV(INS_mov, REG_PAIR_TMP_LO, tempReg, TYP_INT);

    // Jump point for no overflow divide

    genDefineTempLabel(lab_no_overflow);

    // Issue the divide instruction

    inst_TT(INS_UNSIGNED_DIVIDE, op2);

    /* EAX, EDX, tempReg and op2->gtRegNum are now trashed */

    regTracker.rsTrackRegTrash(REG_PAIR_TMP_LO);
    regTracker.rsTrackRegTrash(REG_PAIR_TMP_HI);
    regTracker.rsTrackRegTrash(tempReg);
    regTracker.rsTrackRegTrash(op2->gtRegNum);

    if (tree->gtFlags & GTF_MOD_INT_RESULT)
    {
        /* We don't need to normalize the result, because the caller wants
           an int (in edx) */

        regPair = REG_PAIR_TMP_REVERSE;
    }
    else
    {
        /* The result is now in EDX, we now have to normalize it, i.e. we have
           to issue:
           mov eax, edx; xor edx, edx (for UMOD)
        */

        inst_RV_RV(INS_mov, REG_PAIR_TMP_LO, REG_PAIR_TMP_HI, TYP_INT);

        genSetRegToIcon(REG_PAIR_TMP_HI, 0, TYP_INT);

        regPair = REG_PAIR_TMP;
    }

    genReleaseRegPair(op1);
    genDoneAddressable(op2, addrReg, RegSet::KEEP_REG);

    return regPair;

#else // !_TARGET_X86_

    NYI("codegen for LongModInt");

    return REG_PAIR_NONE;

#endif // !_TARGET_X86_
}

// Given a tree, return the number of registers that are currently
// used to hold integer enregistered local variables.
// Note that, an enregistered TYP_LONG can take 1 or 2 registers.
unsigned CodeGen::genRegCountForLiveIntEnregVars(GenTreePtr tree)
{
    unsigned regCount = 0;

    VARSET_ITER_INIT(compiler, iter, compiler->compCurLife, varNum);
    while (iter.NextElem(&varNum))
    {
        unsigned   lclNum = compiler->lvaTrackedToVarNum[varNum];
        LclVarDsc* varDsc = &compiler->lvaTable[lclNum];

        if (varDsc->lvRegister && !varTypeIsFloating(varDsc->TypeGet()))
        {
            ++regCount;

            if (varTypeIsLong(varDsc->TypeGet()))
            {
                // For enregistered LONG/ULONG, the lower half should always be in a register.
                noway_assert(varDsc->lvRegNum != REG_STK);

                // If the LONG/ULONG is NOT paritally enregistered, then the higher half should be in a register as
                // well.
                if (varDsc->lvOtherReg != REG_STK)
                {
                    ++regCount;
                }
            }
        }
    }

    return regCount;
}

/*****************************************************************************/
/*****************************************************************************/
#if CPU_HAS_FP_SUPPORT
/*****************************************************************************
 *
 *  Generate code for a floating-point operation.
 */

void CodeGen::genCodeForTreeFlt(GenTreePtr tree,
                                regMaskTP  needReg, /* = RBM_ALLFLOAT */
                                regMaskTP  bestReg) /* = RBM_NONE */
{
    genCodeForTreeFloat(tree, needReg, bestReg);

    if (tree->OperGet() == GT_RETURN)
    {
        // Make sure to get ALL THE EPILOG CODE

        // TODO: this should be done AFTER we called exit mon so that
        //       we are sure that we don't have to keep 'this' alive

        if (compiler->info.compCallUnmanaged && (compiler->compCurBB == compiler->genReturnBB))
        {
            /* either it's an "empty" statement or the return statement
               of a synchronized method
             */

            genPInvokeMethodEpilog();
        }

#ifdef PROFILING_SUPPORTED
        // The profiling hook does not trash registers, so it's safe to call after we emit the code for
        // the GT_RETURN tree.

        if (compiler->compCurBB == compiler->genReturnBB)
        {
            genProfilingLeaveCallback();
        }
#endif
    }
}

/*****************************************************************************/
#endif // CPU_HAS_FP_SUPPORT

/*****************************************************************************
 *
 *  Generate a table switch - the switch value (0-based) is in register 'reg'.
 */

void CodeGen::genTableSwitch(regNumber reg, unsigned jumpCnt, BasicBlock** jumpTab)
{
    unsigned jmpTabBase;

    if (jumpCnt == 1)
    {
        // In debug code, we don't optimize away the trivial switch statements.  So we can get here with a
        // BBJ_SWITCH with only a default case.  Therefore, don't generate the switch table.
        noway_assert(compiler->opts.MinOpts() || compiler->opts.compDbgCode);
        inst_JMP(EJ_jmp, jumpTab[0]);
        return;
    }

    noway_assert(jumpCnt >= 2);

    /* Is the number of cases right for a test and jump switch? */

    const bool fFirstCaseFollows = (compiler->compCurBB->bbNext == jumpTab[0]);
    const bool fDefaultFollows   = (compiler->compCurBB->bbNext == jumpTab[jumpCnt - 1]);
    const bool fHaveScratchReg   = ((regSet.rsRegMaskFree() & genRegMask(reg)) != 0);

    unsigned minSwitchTabJumpCnt = 2; // table is better than just 2 cmp/jcc

    // This means really just a single cmp/jcc (aka a simple if/else)
    if (fFirstCaseFollows || fDefaultFollows)
        minSwitchTabJumpCnt++;

#ifdef _TARGET_ARM_
    // On the ARM for small switch tables we will
    // generate a sequence of compare and branch instructions
    // because the code to load the base of the switch
    // table is huge and hideous due to the relocation... :(
    //
    minSwitchTabJumpCnt++;
    if (fHaveScratchReg)
        minSwitchTabJumpCnt++;

#endif // _TARGET_ARM_

    if (jumpCnt < minSwitchTabJumpCnt)
    {
        /* Does the first case label follow? */
        emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);

        if (fFirstCaseFollows)
        {
            /* Check for the default case */
            inst_RV_IV(INS_cmp, reg, jumpCnt - 1, EA_4BYTE);
            emitJumpKind jmpGEU = genJumpKindForOper(GT_GE, CK_UNSIGNED);
            inst_JMP(jmpGEU, jumpTab[jumpCnt - 1]);

            /* No need to jump to the first case */

            jumpCnt -= 2;
            jumpTab += 1;

            /* Generate a series of "dec reg; jmp label" */

            // Make sure that we can trash the register so
            // that we can generate a series of compares and jumps
            //
            if ((jumpCnt > 0) && !fHaveScratchReg)
            {
                regNumber tmpReg = regSet.rsGrabReg(RBM_ALLINT);
                inst_RV_RV(INS_mov, tmpReg, reg);
                regTracker.rsTrackRegTrash(tmpReg);
                reg = tmpReg;
            }

            while (jumpCnt > 0)
            {
                inst_RV_IV(INS_sub, reg, 1, EA_4BYTE, INS_FLAGS_SET);
                inst_JMP(jmpEqual, *jumpTab++);
                jumpCnt--;
            }
        }
        else
        {
            /* Check for case0 first */
            instGen_Compare_Reg_To_Zero(EA_4BYTE, reg); // set flags
            inst_JMP(jmpEqual, *jumpTab);

            /* No need to jump to the first case or the default */

            jumpCnt -= 2;
            jumpTab += 1;

            /* Generate a series of "dec reg; jmp label" */

            // Make sure that we can trash the register so
            // that we can generate a series of compares and jumps
            //
            if ((jumpCnt > 0) && !fHaveScratchReg)
            {
                regNumber tmpReg = regSet.rsGrabReg(RBM_ALLINT);
                inst_RV_RV(INS_mov, tmpReg, reg);
                regTracker.rsTrackRegTrash(tmpReg);
                reg = tmpReg;
            }

            while (jumpCnt > 0)
            {
                inst_RV_IV(INS_sub, reg, 1, EA_4BYTE, INS_FLAGS_SET);
                inst_JMP(jmpEqual, *jumpTab++);
                jumpCnt--;
            }

            if (!fDefaultFollows)
            {
                inst_JMP(EJ_jmp, *jumpTab);
            }
        }

        if ((fFirstCaseFollows || fDefaultFollows) &&
            compiler->fgInDifferentRegions(compiler->compCurBB, compiler->compCurBB->bbNext))
        {
            inst_JMP(EJ_jmp, compiler->compCurBB->bbNext);
        }

        return;
    }

    /* First take care of the default case */

    inst_RV_IV(INS_cmp, reg, jumpCnt - 1, EA_4BYTE);
    emitJumpKind jmpGEU = genJumpKindForOper(GT_GE, CK_UNSIGNED);
    inst_JMP(jmpGEU, jumpTab[jumpCnt - 1]);

    /* Generate the jump table contents */

    jmpTabBase = getEmitter()->emitBBTableDataGenBeg(jumpCnt - 1, false);

#ifdef DEBUG
    if (compiler->opts.dspCode)
        printf("\n      J_M%03u_DS%02u LABEL   DWORD\n", Compiler::s_compMethodsCount, jmpTabBase);
#endif

    for (unsigned index = 0; index < jumpCnt - 1; index++)
    {
        BasicBlock* target = jumpTab[index];

        noway_assert(target->bbFlags & BBF_JMP_TARGET);

#ifdef DEBUG
        if (compiler->opts.dspCode)
            printf("            DD      L_M%03u_BB%02u\n", Compiler::s_compMethodsCount, target->bbNum);
#endif

        getEmitter()->emitDataGenData(index, target);
    }

    getEmitter()->emitDataGenEnd();

#ifdef _TARGET_ARM_
    // We need to load the address of the table into a register.
    // The data section might get placed a long distance away, so we
    // can't safely do a PC-relative ADR. :(
    // Pick any register except the index register.
    //
    regNumber regTabBase = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(reg));
    genMov32RelocatableDataLabel(jmpTabBase, regTabBase);
    regTracker.rsTrackRegTrash(regTabBase);

    // LDR PC, [regTableBase + reg * 4] (encoded as LDR PC, [regTableBase, reg, LSL 2]
    getEmitter()->emitIns_R_ARX(INS_ldr, EA_PTRSIZE, REG_PC, regTabBase, reg, TARGET_POINTER_SIZE, 0);

#else // !_TARGET_ARM_

    getEmitter()->emitIns_IJ(EA_4BYTE_DSP_RELOC, reg, jmpTabBase);

#endif
}

/*****************************************************************************
 *
 *  Generate code for a switch statement.
 */

void CodeGen::genCodeForSwitch(GenTreePtr tree)
{
    unsigned     jumpCnt;
    BasicBlock** jumpTab;

    GenTreePtr oper;
    regNumber  reg;

    noway_assert(tree->gtOper == GT_SWITCH);
    oper = tree->gtOp.gtOp1;
    noway_assert(genActualTypeIsIntOrI(oper->gtType));

    /* Get hold of the jump table */

    noway_assert(compiler->compCurBB->bbJumpKind == BBJ_SWITCH);

    jumpCnt = compiler->compCurBB->bbJumpSwt->bbsCount;
    jumpTab = compiler->compCurBB->bbJumpSwt->bbsDstTab;

    /* Compute the switch value into some register */

    genCodeForTree(oper, 0);

    /* Get hold of the register the value is in */

    noway_assert(oper->InReg());
    reg = oper->gtRegNum;

#if FEATURE_STACK_FP_X87
    if (!compCurFPState.IsEmpty())
    {
        return genTableSwitchStackFP(reg, jumpCnt, jumpTab);
    }
    else
#endif // FEATURE_STACK_FP_X87
    {
        return genTableSwitch(reg, jumpCnt, jumpTab);
    }
}

/*****************************************************************************/
/*****************************************************************************
 *  Emit a call to a helper function.
 */

// inline
void CodeGen::genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize)
{
    // Can we call the helper function directly

    void *addr = NULL, **pAddr = NULL;

#if defined(_TARGET_ARM_) && defined(DEBUG) && defined(PROFILING_SUPPORTED)
    // Don't ask VM if it hasn't requested ELT hooks
    if (!compiler->compProfilerHookNeeded && compiler->opts.compJitELTHookEnabled &&
        (helper == CORINFO_HELP_PROF_FCN_ENTER || helper == CORINFO_HELP_PROF_FCN_LEAVE ||
         helper == CORINFO_HELP_PROF_FCN_TAILCALL))
    {
        addr = compiler->compProfilerMethHnd;
    }
    else
#endif
    {
        addr = compiler->compGetHelperFtn((CorInfoHelpFunc)helper, (void**)&pAddr);
    }

#ifdef _TARGET_ARM_
    if (!addr || !arm_Valid_Imm_For_BL((ssize_t)addr))
    {
        // Load the address into a register and call  through a register
        regNumber indCallReg =
            regSet.rsGrabReg(RBM_ALLINT); // Grab an available register to use for the CALL indirection
        if (addr)
        {
            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indCallReg, (ssize_t)addr);
        }
        else
        {
            getEmitter()->emitIns_R_AI(INS_ldr, EA_PTR_DSP_RELOC, indCallReg, (ssize_t)pAddr);
            regTracker.rsTrackRegTrash(indCallReg);
        }

        getEmitter()->emitIns_Call(emitter::EC_INDIR_R, compiler->eeFindHelper(helper),
                                   INDEBUG_LDISASM_COMMA(nullptr) NULL, // addr
                                   argSize, retSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                   gcInfo.gcRegByrefSetCur,
                                   BAD_IL_OFFSET, // ilOffset
                                   indCallReg,    // ireg
                                   REG_NA, 0, 0,  // xreg, xmul, disp
                                   false,         // isJump
                                   emitter::emitNoGChelper(helper),
                                   (CorInfoHelpFunc)helper == CORINFO_HELP_PROF_FCN_LEAVE);
    }
    else
    {
        getEmitter()->emitIns_Call(emitter::EC_FUNC_TOKEN, compiler->eeFindHelper(helper),
                                   INDEBUG_LDISASM_COMMA(nullptr) addr, argSize, retSize, gcInfo.gcVarPtrSetCur,
                                   gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur, BAD_IL_OFFSET, REG_NA, REG_NA, 0,
                                   0,     /* ilOffset, ireg, xreg, xmul, disp */
                                   false, /* isJump */
                                   emitter::emitNoGChelper(helper),
                                   (CorInfoHelpFunc)helper == CORINFO_HELP_PROF_FCN_LEAVE);
    }
#else

    {
        emitter::EmitCallType callType = emitter::EC_FUNC_TOKEN;

        if (!addr)
        {
            callType = emitter::EC_FUNC_TOKEN_INDIR;
            addr     = pAddr;
        }

        getEmitter()->emitIns_Call(callType, compiler->eeFindHelper(helper), INDEBUG_LDISASM_COMMA(nullptr) addr,
                                   argSize, retSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                   gcInfo.gcRegByrefSetCur, BAD_IL_OFFSET, REG_NA, REG_NA, 0,
                                   0,     /* ilOffset, ireg, xreg, xmul, disp */
                                   false, /* isJump */
                                   emitter::emitNoGChelper(helper));
    }
#endif

    regTracker.rsTrashRegSet(RBM_CALLEE_TRASH);
    regTracker.rsTrashRegsForGCInterruptability();
}

/*****************************************************************************
 *
 *  Push the given argument list, right to left; returns the total amount of
 *  stuff pushed.
 */

#if !FEATURE_FIXED_OUT_ARGS
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
size_t CodeGen::genPushArgList(GenTreeCall* call)
{
    GenTreeArgList* regArgs = call->gtCallLateArgs;
    size_t          size    = 0;
    regMaskTP       addrReg;

    GenTreeArgList* args;
    // Create a local, artificial GenTreeArgList that includes the gtCallObjp, if that exists, as first argument,
    // so we can iterate over this argument list more uniformly.
    // Need to provide a temporary non-null first argument here: if we use this, we'll replace it.
    GenTreeArgList firstForObjp(/*temp dummy arg*/ call, call->gtCallArgs);
    if (call->gtCallObjp == NULL)
    {
        args = call->gtCallArgs;
    }
    else
    {
        firstForObjp.Current() = call->gtCallObjp;
        args                   = &firstForObjp;
    }

    GenTreePtr curr;
    var_types  type;
    size_t     opsz;

    for (; args; args = args->Rest())
    {
        addrReg = DUMMY_INIT(RBM_CORRUPT); // to detect uninitialized use

        /* Get hold of the next argument value */
        curr = args->Current();

        if (curr->IsArgPlaceHolderNode())
        {
            assert(curr->gtFlags & GTF_LATE_ARG);

            addrReg = 0;
            continue;
        }

        // If we have a comma expression, eval the non-last, then deal with the last.
        if (!(curr->gtFlags & GTF_LATE_ARG))
            curr = genCodeForCommaTree(curr);

        /* See what type of a value we're passing */
        type = curr->TypeGet();

        opsz = genTypeSize(genActualType(type));

        switch (type)
        {
            case TYP_BOOL:
            case TYP_BYTE:
            case TYP_SHORT:
            case TYP_CHAR:
            case TYP_UBYTE:

                /* Don't want to push a small value, make it a full word */

                genCodeForTree(curr, 0);

                __fallthrough; // now the value should be in a register ...

            case TYP_INT:
            case TYP_REF:
            case TYP_BYREF:
#if !CPU_HAS_FP_SUPPORT
            case TYP_FLOAT:
#endif

                if (curr->gtFlags & GTF_LATE_ARG)
                {
                    assert(curr->gtOper == GT_ASG);
                    /* one more argument will be passed in a register */
                    noway_assert(intRegState.rsCurRegArgNum < MAX_REG_ARG);

                    /* arg is passed in the register, nothing on the stack */

                    opsz = 0;
                }

                /* Is this value a handle? */

                if (curr->gtOper == GT_CNS_INT && curr->IsIconHandle())
                {
                    /* Emit a fixup for the push instruction */

                    inst_IV_handle(INS_push, curr->gtIntCon.gtIconVal);
                    genSinglePush();

                    addrReg = 0;
                    break;
                }

                /* Is the value a constant? */

                if (curr->gtOper == GT_CNS_INT)
                {

#if REDUNDANT_LOAD
                    regNumber reg = regTracker.rsIconIsInReg(curr->gtIntCon.gtIconVal);

                    if (reg != REG_NA)
                    {
                        inst_RV(INS_push, reg, TYP_INT);
                    }
                    else
#endif
                    {
                        inst_IV(INS_push, curr->gtIntCon.gtIconVal);
                    }

                    /* If the type is TYP_REF, then this must be a "null". So we can
                       treat it as a TYP_INT as we don't need to report it as a GC ptr */

                    noway_assert(curr->TypeGet() == TYP_INT ||
                                 (varTypeIsGC(curr->TypeGet()) && curr->gtIntCon.gtIconVal == 0));

                    genSinglePush();

                    addrReg = 0;
                    break;
                }

                if (curr->gtFlags & GTF_LATE_ARG)
                {
                    /* This must be a register arg temp assignment */

                    noway_assert(curr->gtOper == GT_ASG);

                    /* Evaluate it to the temp */

                    genCodeForTree(curr, 0);

                    /* Increment the current argument register counter */

                    intRegState.rsCurRegArgNum++;

                    addrReg = 0;
                }
                else
                {
                    /* This is a 32-bit integer non-register argument */

                    addrReg = genMakeRvalueAddressable(curr, 0, RegSet::KEEP_REG, false);
                    inst_TT(INS_push, curr);
                    genSinglePush();
                    genDoneAddressable(curr, addrReg, RegSet::KEEP_REG);
                }
                break;

            case TYP_LONG:
#if !CPU_HAS_FP_SUPPORT
            case TYP_DOUBLE:
#endif

                /* Is the value a constant? */

                if (curr->gtOper == GT_CNS_LNG)
                {
                    inst_IV(INS_push, (int)(curr->gtLngCon.gtLconVal >> 32));
                    genSinglePush();
                    inst_IV(INS_push, (int)(curr->gtLngCon.gtLconVal));
                    genSinglePush();

                    addrReg = 0;
                }
                else
                {
                    addrReg = genMakeAddressable(curr, 0, RegSet::FREE_REG);

                    inst_TT(INS_push, curr, sizeof(int));
                    genSinglePush();
                    inst_TT(INS_push, curr);
                    genSinglePush();
                }
                break;

#if CPU_HAS_FP_SUPPORT
            case TYP_FLOAT:
            case TYP_DOUBLE:
#endif
#if FEATURE_STACK_FP_X87
                addrReg = genPushArgumentStackFP(curr);
#else
                NYI("FP codegen");
                addrReg = 0;
#endif
                break;

            case TYP_VOID:

                /* Is this a nothing node, deferred register argument? */

                if (curr->gtFlags & GTF_LATE_ARG)
                {
                    GenTree* arg = curr;
                    if (arg->gtOper == GT_COMMA)
                    {
                        while (arg->gtOper == GT_COMMA)
                        {
                            GenTreePtr op1 = arg->gtOp.gtOp1;
                            genEvalSideEffects(op1);
                            genUpdateLife(op1);
                            arg = arg->gtOp.gtOp2;
                        }
                        if (!arg->IsNothingNode())
                        {
                            genEvalSideEffects(arg);
                            genUpdateLife(arg);
                        }
                    }

                    /* increment the register count and continue with the next argument */

                    intRegState.rsCurRegArgNum++;

                    noway_assert(opsz == 0);

                    addrReg = 0;
                    break;
                }

                __fallthrough;

            case TYP_STRUCT:
            {
                GenTree* arg = curr;
                while (arg->gtOper == GT_COMMA)
                {
                    GenTreePtr op1 = arg->gtOp.gtOp1;
                    genEvalSideEffects(op1);
                    genUpdateLife(op1);
                    arg = arg->gtOp.gtOp2;
                }

                noway_assert(arg->gtOper == GT_OBJ || arg->gtOper == GT_MKREFANY || arg->gtOper == GT_IND);
                noway_assert((arg->gtFlags & GTF_REVERSE_OPS) == 0);
                noway_assert(addrReg == DUMMY_INIT(RBM_CORRUPT));

                if (arg->gtOper == GT_MKREFANY)
                {
                    GenTreePtr op1 = arg->gtOp.gtOp1;
                    GenTreePtr op2 = arg->gtOp.gtOp2;

                    addrReg = genMakeAddressable(op1, RBM_NONE, RegSet::KEEP_REG);

                    /* Is this value a handle? */
                    if (op2->gtOper == GT_CNS_INT && op2->IsIconHandle())
                    {
                        /* Emit a fixup for the push instruction */

                        inst_IV_handle(INS_push, op2->gtIntCon.gtIconVal);
                        genSinglePush();
                    }
                    else
                    {
                        regMaskTP addrReg2 = genMakeRvalueAddressable(op2, 0, RegSet::KEEP_REG, false);
                        inst_TT(INS_push, op2);
                        genSinglePush();
                        genDoneAddressable(op2, addrReg2, RegSet::KEEP_REG);
                    }
                    addrReg = genKeepAddressable(op1, addrReg);
                    inst_TT(INS_push, op1);
                    genSinglePush();
                    genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);

                    opsz = 2 * TARGET_POINTER_SIZE;
                }
                else
                {
                    noway_assert(arg->gtOper == GT_OBJ);

                    if (arg->gtObj.gtOp1->gtOper == GT_ADDR && arg->gtObj.gtOp1->gtOp.gtOp1->gtOper == GT_LCL_VAR)
                    {
                        GenTreePtr structLocalTree = arg->gtObj.gtOp1->gtOp.gtOp1;
                        unsigned   structLclNum    = structLocalTree->gtLclVarCommon.gtLclNum;
                        LclVarDsc* varDsc          = &compiler->lvaTable[structLclNum];

                        // As much as we would like this to be a noway_assert, we can't because
                        // there are some weird casts out there, and backwards compatiblity
                        // dictates we do *NOT* start rejecting them now. lvaGetPromotion and
                        // lvPromoted in general currently do not require the local to be
                        // TYP_STRUCT, so this assert is really more about how we wish the world
                        // was then some JIT invariant.
                        assert((structLocalTree->TypeGet() == TYP_STRUCT) || compiler->compUnsafeCastUsed);

                        Compiler::lvaPromotionType promotionType = compiler->lvaGetPromotionType(varDsc);

                        if (varDsc->lvPromoted &&
                            promotionType ==
                                Compiler::PROMOTION_TYPE_INDEPENDENT) // Otherwise it is guaranteed to live on stack.
                        {
                            assert(!varDsc->lvAddrExposed); // Compiler::PROMOTION_TYPE_INDEPENDENT ==> not exposed.

                            addrReg = 0;

                            // Get the number of BYTES to copy to the stack
                            opsz = roundUp(compiler->info.compCompHnd->getClassSize(arg->gtObj.gtClass), sizeof(void*));
                            size_t bytesToBeCopied = opsz;

                            // postponedFields is true if we have any postponed fields
                            //   Any field that does not start on a 4-byte boundary is a postponed field
                            //   Such a field is required to be a short or a byte
                            //
                            // postponedRegKind records the kind of scratch register we will
                            //   need to process the postponed fields
                            //   RBM_NONE means that we don't need a register
                            //
                            // expectedAlignedOffset records the aligned offset that
                            //   has to exist for a push to cover the postponed fields.
                            //   Since all promoted structs have the tightly packed property
                            //   we are guaranteed that we will have such a push
                            //
                            bool      postponedFields       = false;
                            regMaskTP postponedRegKind      = RBM_NONE;
                            size_t    expectedAlignedOffset = UINT_MAX;

                            VARSET_TP* deadVarBits = NULL;
                            compiler->GetPromotedStructDeathVars()->Lookup(structLocalTree, &deadVarBits);

                            // Reverse loop, starts pushing from the end of the struct (i.e. the highest field offset)
                            //
                            for (int varNum = varDsc->lvFieldLclStart + varDsc->lvFieldCnt - 1;
                                 varNum >= (int)varDsc->lvFieldLclStart; varNum--)
                            {
                                LclVarDsc* fieldVarDsc = compiler->lvaTable + varNum;
#ifdef DEBUG
                                if (fieldVarDsc->lvExactSize == 2 * sizeof(unsigned))
                                {
                                    noway_assert(fieldVarDsc->lvFldOffset % (2 * sizeof(unsigned)) == 0);
                                    noway_assert(fieldVarDsc->lvFldOffset + (2 * sizeof(unsigned)) == bytesToBeCopied);
                                }
#endif
                                // Whenever we see a stack-aligned fieldVarDsc then we use 4-byte push instruction(s)
                                // For packed structs we will go back and store the unaligned bytes and shorts
                                // in the next loop
                                //
                                if (fieldVarDsc->lvStackAligned())
                                {
                                    if (fieldVarDsc->lvExactSize != 2 * sizeof(unsigned) &&
                                        fieldVarDsc->lvFldOffset + sizeof(void*) != bytesToBeCopied)
                                    {
                                        // Might need 4-bytes paddings for fields other than LONG and DOUBLE.
                                        // Just push some junk (i.e EAX) on the stack.
                                        inst_RV(INS_push, REG_EAX, TYP_INT);
                                        genSinglePush();

                                        bytesToBeCopied -= sizeof(void*);
                                    }

                                    // If we have an expectedAlignedOffset make sure that this push instruction
                                    // is what we expect to cover the postponedFields
                                    //
                                    if (expectedAlignedOffset != UINT_MAX)
                                    {
                                        // This push must be for a small field
                                        noway_assert(fieldVarDsc->lvExactSize < 4);
                                        // The fldOffset for this push should be equal to the expectedAlignedOffset
                                        noway_assert(fieldVarDsc->lvFldOffset == expectedAlignedOffset);
                                        expectedAlignedOffset = UINT_MAX;
                                    }

                                    // Push the "upper half" of LONG var first

                                    if (isRegPairType(fieldVarDsc->lvType))
                                    {
                                        if (fieldVarDsc->lvOtherReg != REG_STK)
                                        {
                                            inst_RV(INS_push, fieldVarDsc->lvOtherReg, TYP_INT);
                                            genSinglePush();

                                            // Prepare the set of vars to be cleared from gcref/gcbyref set
                                            // in case they become dead after genUpdateLife.
                                            // genDoneAddressable() will remove dead gc vars by calling
                                            // gcInfo.gcMarkRegSetNpt.
                                            // Although it is not addrReg, we just borrow the name here.
                                            addrReg |= genRegMask(fieldVarDsc->lvOtherReg);
                                        }
                                        else
                                        {
                                            getEmitter()->emitIns_S(INS_push, EA_4BYTE, varNum, sizeof(void*));
                                            genSinglePush();
                                        }

                                        bytesToBeCopied -= sizeof(void*);
                                    }

                                    // Push the "upper half" of DOUBLE var if it is not enregistered.

                                    if (fieldVarDsc->lvType == TYP_DOUBLE)
                                    {
                                        if (!fieldVarDsc->lvRegister)
                                        {
                                            getEmitter()->emitIns_S(INS_push, EA_4BYTE, varNum, sizeof(void*));
                                            genSinglePush();
                                        }

                                        bytesToBeCopied -= sizeof(void*);
                                    }

                                    //
                                    // Push the field local.
                                    //

                                    if (fieldVarDsc->lvRegister)
                                    {
                                        if (!varTypeIsFloating(genActualType(fieldVarDsc->TypeGet())))
                                        {
                                            inst_RV(INS_push, fieldVarDsc->lvRegNum,
                                                    genActualType(fieldVarDsc->TypeGet()));
                                            genSinglePush();

                                            // Prepare the set of vars to be cleared from gcref/gcbyref set
                                            // in case they become dead after genUpdateLife.
                                            // genDoneAddressable() will remove dead gc vars by calling
                                            // gcInfo.gcMarkRegSetNpt.
                                            // Although it is not addrReg, we just borrow the name here.
                                            addrReg |= genRegMask(fieldVarDsc->lvRegNum);
                                        }
                                        else
                                        {
                                            // Must be TYP_FLOAT or TYP_DOUBLE
                                            noway_assert(fieldVarDsc->lvRegNum != REG_FPNONE);

                                            noway_assert(fieldVarDsc->lvExactSize == sizeof(unsigned) ||
                                                         fieldVarDsc->lvExactSize == 2 * sizeof(unsigned));

                                            inst_RV_IV(INS_sub, REG_SPBASE, fieldVarDsc->lvExactSize, EA_PTRSIZE);

                                            genSinglePush();
                                            if (fieldVarDsc->lvExactSize == 2 * sizeof(unsigned))
                                            {
                                                genSinglePush();
                                            }

#if FEATURE_STACK_FP_X87
                                            GenTree* fieldTree = new (compiler, GT_REG_VAR)
                                                GenTreeLclVar(fieldVarDsc->lvType, varNum, BAD_IL_OFFSET);
                                            fieldTree->gtOper            = GT_REG_VAR;
                                            fieldTree->gtRegNum          = fieldVarDsc->lvRegNum;
                                            fieldTree->gtRegVar.gtRegNum = fieldVarDsc->lvRegNum;
                                            if ((arg->gtFlags & GTF_VAR_DEATH) != 0)
                                            {
                                                if (fieldVarDsc->lvTracked &&
                                                    (deadVarBits == NULL ||
                                                     VarSetOps::IsMember(compiler, *deadVarBits,
                                                                         fieldVarDsc->lvVarIndex)))
                                                {
                                                    fieldTree->gtFlags |= GTF_VAR_DEATH;
                                                }
                                            }
                                            genCodeForTreeStackFP_Leaf(fieldTree);

                                            // Take reg to top of stack

                                            FlatFPX87_MoveToTOS(&compCurFPState, fieldTree->gtRegNum);

                                            // Pop it off to stack
                                            compCurFPState.Pop();

                                            getEmitter()->emitIns_AR_R(INS_fstp, EA_ATTR(fieldVarDsc->lvExactSize),
                                                                       REG_NA, REG_SPBASE, 0);
#else
                                            NYI_FLAT_FP_X87("FP codegen");
#endif
                                        }
                                    }
                                    else
                                    {
                                        getEmitter()->emitIns_S(INS_push,
                                                                (fieldVarDsc->TypeGet() == TYP_REF) ? EA_GCREF
                                                                                                    : EA_4BYTE,
                                                                varNum, 0);
                                        genSinglePush();
                                    }

                                    bytesToBeCopied -= sizeof(void*);
                                }
                                else // not stack aligned
                                {
                                    noway_assert(fieldVarDsc->lvExactSize < 4);

                                    // We will need to use a store byte or store word
                                    // to set this unaligned location
                                    postponedFields = true;

                                    if (expectedAlignedOffset != UINT_MAX)
                                    {
                                        // This should never change until it is set back to UINT_MAX by an aligned
                                        // offset
                                        noway_assert(expectedAlignedOffset ==
                                                     roundUp(fieldVarDsc->lvFldOffset, sizeof(void*)) - sizeof(void*));
                                    }

                                    expectedAlignedOffset =
                                        roundUp(fieldVarDsc->lvFldOffset, sizeof(void*)) - sizeof(void*);

                                    noway_assert(expectedAlignedOffset < bytesToBeCopied);

                                    if (fieldVarDsc->lvRegister)
                                    {
                                        // Do we need to use a byte-able register?
                                        if (fieldVarDsc->lvExactSize == 1)
                                        {
                                            // Did we enregister fieldVarDsc2 in a non byte-able register?
                                            if ((genRegMask(fieldVarDsc->lvRegNum) & RBM_BYTE_REGS) == 0)
                                            {
                                                // then we will need to grab a byte-able register
                                                postponedRegKind = RBM_BYTE_REGS;
                                            }
                                        }
                                    }
                                    else // not enregistered
                                    {
                                        if (fieldVarDsc->lvExactSize == 1)
                                        {
                                            // We will need to grab a byte-able register
                                            postponedRegKind = RBM_BYTE_REGS;
                                        }
                                        else
                                        {
                                            // We will need to grab any scratch register
                                            if (postponedRegKind != RBM_BYTE_REGS)
                                                postponedRegKind = RBM_ALLINT;
                                        }
                                    }
                                }
                            }

                            // Now we've pushed all of the aligned fields.
                            //
                            // We should have pushed bytes equal to the entire struct
                            noway_assert(bytesToBeCopied == 0);

                            // We should have seen a push that covers every postponed field
                            noway_assert(expectedAlignedOffset == UINT_MAX);

                            // Did we have any postponed fields?
                            if (postponedFields)
                            {
                                regNumber regNum = REG_STK; // means no register

                                // If we needed a scratch register then grab it here

                                if (postponedRegKind != RBM_NONE)
                                    regNum = regSet.rsGrabReg(postponedRegKind);

                                // Forward loop, starts from the lowest field offset
                                //
                                for (unsigned varNum = varDsc->lvFieldLclStart;
                                     varNum < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; varNum++)
                                {
                                    LclVarDsc* fieldVarDsc = compiler->lvaTable + varNum;

                                    // All stack aligned fields have already been pushed
                                    if (fieldVarDsc->lvStackAligned())
                                        continue;

                                    // We have a postponed field

                                    // It must be a byte or a short
                                    noway_assert(fieldVarDsc->lvExactSize < 4);

                                    // Is the field enregistered?
                                    if (fieldVarDsc->lvRegister)
                                    {
                                        // Frequently we can just use that register
                                        regNumber tmpRegNum = fieldVarDsc->lvRegNum;

                                        // Do we need to use a byte-able register?
                                        if (fieldVarDsc->lvExactSize == 1)
                                        {
                                            // Did we enregister the field in a non byte-able register?
                                            if ((genRegMask(tmpRegNum) & RBM_BYTE_REGS) == 0)
                                            {
                                                // then we will need to use the byte-able register 'regNum'
                                                noway_assert((genRegMask(regNum) & RBM_BYTE_REGS) != 0);

                                                // Copy the register that contains fieldVarDsc into 'regNum'
                                                getEmitter()->emitIns_R_R(INS_mov, EA_4BYTE, regNum,
                                                                          fieldVarDsc->lvRegNum);
                                                regTracker.rsTrackRegLclVar(regNum, varNum);

                                                // tmpRegNum is the register that we will extract the byte value from
                                                tmpRegNum = regNum;
                                            }
                                            noway_assert((genRegMask(tmpRegNum) & RBM_BYTE_REGS) != 0);
                                        }

                                        getEmitter()->emitIns_AR_R(ins_Store(fieldVarDsc->TypeGet()),
                                                                   (emitAttr)fieldVarDsc->lvExactSize, tmpRegNum,
                                                                   REG_SPBASE, fieldVarDsc->lvFldOffset);
                                    }
                                    else // not enregistered
                                    {
                                        // We will copy the non-enregister fieldVar into our scratch register 'regNum'

                                        noway_assert(regNum != REG_STK);
                                        getEmitter()->emitIns_R_S(ins_Load(fieldVarDsc->TypeGet()),
                                                                  (emitAttr)fieldVarDsc->lvExactSize, regNum, varNum,
                                                                  0);

                                        regTracker.rsTrackRegLclVar(regNum, varNum);

                                        // Store the value (byte or short) into the stack

                                        getEmitter()->emitIns_AR_R(ins_Store(fieldVarDsc->TypeGet()),
                                                                   (emitAttr)fieldVarDsc->lvExactSize, regNum,
                                                                   REG_SPBASE, fieldVarDsc->lvFldOffset);
                                    }
                                }
                            }
                            genUpdateLife(structLocalTree);

                            break;
                        }
                    }

                    genCodeForTree(arg->gtObj.gtOp1, 0);
                    noway_assert(arg->gtObj.gtOp1->InReg());
                    regNumber reg = arg->gtObj.gtOp1->gtRegNum;
                    // Get the number of DWORDS to copy to the stack
                    opsz = roundUp(compiler->info.compCompHnd->getClassSize(arg->gtObj.gtClass), sizeof(void*));
                    unsigned slots = (unsigned)(opsz / sizeof(void*));

                    BYTE* gcLayout = new (compiler, CMK_Codegen) BYTE[slots];

                    compiler->info.compCompHnd->getClassGClayout(arg->gtObj.gtClass, gcLayout);

                    BOOL bNoneGC = TRUE;
                    for (int i = slots - 1; i >= 0; --i)
                    {
                        if (gcLayout[i] != TYPE_GC_NONE)
                        {
                            bNoneGC = FALSE;
                            break;
                        }
                    }

                    /* passing large structures using movq instead of pushes does not increase codesize very much */
                    unsigned movqLenMin  = 8;
                    unsigned movqLenMax  = 64;
                    unsigned curBBweight = compiler->compCurBB->getBBWeight(compiler);

                    if ((compiler->compCodeOpt() == Compiler::SMALL_CODE) || (curBBweight == BB_ZERO_WEIGHT))
                    {
                        // Don't bother with this optimization in
                        // rarely run blocks or when optimizing for size
                        movqLenMax = movqLenMin = 0;
                    }
                    else if (compiler->compCodeOpt() == Compiler::FAST_CODE)
                    {
                        // Be more aggressive when optimizing for speed
                        movqLenMax *= 2;
                    }

                    /* Adjust for BB weight */
                    if (curBBweight >= (BB_LOOP_WEIGHT * BB_UNITY_WEIGHT) / 2)
                    {
                        // Be more aggressive when we are inside a loop
                        movqLenMax *= 2;
                    }

                    if (compiler->opts.compCanUseSSE2 && bNoneGC && (opsz >= movqLenMin) && (opsz <= movqLenMax))
                    {
                        JITLOG_THIS(compiler, (LL_INFO10000,
                                               "Using XMM instructions to pass %3d byte valuetype while compiling %s\n",
                                               opsz, compiler->info.compFullName));

                        int       stkDisp = (int)(unsigned)opsz;
                        int       curDisp = 0;
                        regNumber xmmReg  = REG_XMM0;

                        if (opsz & 0x4)
                        {
                            stkDisp -= sizeof(void*);
                            getEmitter()->emitIns_AR_R(INS_push, EA_4BYTE, REG_NA, reg, stkDisp);
                            genSinglePush();
                        }

                        inst_RV_IV(INS_sub, REG_SPBASE, stkDisp, EA_PTRSIZE);
                        AddStackLevel(stkDisp);

                        while (curDisp < stkDisp)
                        {
                            getEmitter()->emitIns_R_AR(INS_movq, EA_8BYTE, xmmReg, reg, curDisp);
                            getEmitter()->emitIns_AR_R(INS_movq, EA_8BYTE, xmmReg, REG_SPBASE, curDisp);
                            curDisp += 2 * sizeof(void*);
                        }
                        noway_assert(curDisp == stkDisp);
                    }
                    else
                    {
                        for (int i = slots - 1; i >= 0; --i)
                        {
                            emitAttr fieldSize;
                            if (gcLayout[i] == TYPE_GC_NONE)
                                fieldSize = EA_4BYTE;
                            else if (gcLayout[i] == TYPE_GC_REF)
                                fieldSize = EA_GCREF;
                            else
                            {
                                noway_assert(gcLayout[i] == TYPE_GC_BYREF);
                                fieldSize = EA_BYREF;
                            }
                            getEmitter()->emitIns_AR_R(INS_push, fieldSize, REG_NA, reg, i * sizeof(void*));
                            genSinglePush();
                        }
                    }
                    gcInfo.gcMarkRegSetNpt(genRegMask(reg)); // Kill the pointer in op1
                }

                addrReg = 0;
                break;
            }

            default:
                noway_assert(!"unhandled/unexpected arg type");
                NO_WAY("unhandled/unexpected arg type");
        }

        /* Update the current set of live variables */

        genUpdateLife(curr);

        /* Update the current set of register pointers */

        noway_assert(addrReg != DUMMY_INIT(RBM_CORRUPT));
        genDoneAddressable(curr, addrReg, RegSet::FREE_REG);

        /* Remember how much stuff we've pushed on the stack */

        size += opsz;

        /* Update the current argument stack offset */

        /* Continue with the next argument, if any more are present */

    } // while args

    /* Move the deferred arguments to registers */

    for (args = regArgs; args; args = args->Rest())
    {
        curr = args->Current();

        assert(!curr->IsArgPlaceHolderNode()); // No place holders nodes are in the late args

        fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(call, curr);
        assert(curArgTabEntry);
        regNumber regNum = curArgTabEntry->regNum;

        noway_assert(isRegParamType(curr->TypeGet()));
        noway_assert(curr->gtType != TYP_VOID);

        /* Evaluate the argument to a register [pair] */

        if (genTypeSize(genActualType(curr->TypeGet())) == sizeof(int))
        {
            /* Check if this is the guess area for the resolve interface call
             * Pass a size of EA_OFFSET*/
            if (curr->gtOper == GT_CLS_VAR && compiler->eeGetJitDataOffs(curr->gtClsVar.gtClsVarHnd) >= 0)
            {
                getEmitter()->emitIns_R_C(ins_Load(TYP_INT), EA_OFFSET, regNum, curr->gtClsVar.gtClsVarHnd, 0);
                regTracker.rsTrackRegTrash(regNum);

                /* The value is now in the appropriate register */

                genMarkTreeInReg(curr, regNum);
            }
            else
            {
                genComputeReg(curr, genRegMask(regNum), RegSet::EXACT_REG, RegSet::FREE_REG, false);
            }

            noway_assert(curr->gtRegNum == regNum);

            /* If the register is already marked as used, it will become
               multi-used. However, since it is a callee-trashed register,
               we will have to spill it before the call anyway. So do it now */

            if (regSet.rsMaskUsed & genRegMask(regNum))
            {
                noway_assert(genRegMask(regNum) & RBM_CALLEE_TRASH);
                regSet.rsSpillReg(regNum);
            }

            /* Mark the register as 'used' */

            regSet.rsMarkRegUsed(curr);
        }
        else
        {
            noway_assert(!"UNDONE: Passing a TYP_STRUCT in register arguments");
        }
    }

    /* If any of the previously loaded arguments were spilled - reload them */

    for (args = regArgs; args; args = args->Rest())
    {
        curr = args->Current();
        assert(curr);

        if (curr->gtFlags & GTF_SPILLED)
        {
            if (isRegPairType(curr->gtType))
            {
                regSet.rsUnspillRegPair(curr, genRegPairMask(curr->gtRegPair), RegSet::KEEP_REG);
            }
            else
            {
                regSet.rsUnspillReg(curr, genRegMask(curr->gtRegNum), RegSet::KEEP_REG);
            }
        }
    }

    /* Return the total size pushed */

    return size;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#else // FEATURE_FIXED_OUT_ARGS

//
// ARM and AMD64 uses this method to pass the stack based args
//
// returns size pushed (always zero)
size_t CodeGen::genPushArgList(GenTreeCall* call)
{
    GenTreeArgList* lateArgs = call->gtCallLateArgs;
    GenTreePtr      curr;
    var_types       type;
    int             argSize;

    GenTreeArgList* args;
    // Create a local, artificial GenTreeArgList that includes the gtCallObjp, if that exists, as first argument,
    // so we can iterate over this argument list more uniformly.
    // Need to provide a temporary non-null first argument here: if we use this, we'll replace it.
    GenTreeArgList objpArgList(/*temp dummy arg*/ call, call->gtCallArgs);
    if (call->gtCallObjp == NULL)
    {
        args = call->gtCallArgs;
    }
    else
    {
        objpArgList.Current() = call->gtCallObjp;
        args                  = &objpArgList;
    }

    for (; args; args = args->Rest())
    {
        /* Get hold of the next argument value */
        curr = args->Current();

        fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(call, curr);
        assert(curArgTabEntry);
        regNumber regNum    = curArgTabEntry->regNum;
        int       argOffset = curArgTabEntry->slotNum * TARGET_POINTER_SIZE;

        /* See what type of a value we're passing */
        type = curr->TypeGet();

        if ((type == TYP_STRUCT) && (curr->gtOper == GT_ASG))
        {
            type = TYP_VOID;
        }

        // This holds the set of registers corresponding to enregistered promoted struct field variables
        // that go dead after this use of the variable in the argument list.
        regMaskTP deadFieldVarRegs = RBM_NONE;

        argSize = TARGET_POINTER_SIZE; // The default size for an arg is one pointer-sized item

        if (curr->IsArgPlaceHolderNode())
        {
            assert(curr->gtFlags & GTF_LATE_ARG);
            goto DEFERRED;
        }

        if (varTypeIsSmall(type))
        {
            // Normalize 'type', it represents the item that we will be storing in the Outgoing Args
            type = TYP_I_IMPL;
        }

        switch (type)
        {

            case TYP_DOUBLE:
            case TYP_LONG:

#if defined(_TARGET_ARM_)

                argSize = (TARGET_POINTER_SIZE * 2);

                /* Is the value a constant? */

                if (curr->gtOper == GT_CNS_LNG)
                {
                    assert((curr->gtFlags & GTF_LATE_ARG) == 0);

                    int hiVal = (int)(curr->gtLngCon.gtLconVal >> 32);
                    int loVal = (int)(curr->gtLngCon.gtLconVal & 0xffffffff);

                    instGen_Store_Imm_Into_Lcl(TYP_INT, EA_4BYTE, loVal, compiler->lvaOutgoingArgSpaceVar, argOffset);

                    instGen_Store_Imm_Into_Lcl(TYP_INT, EA_4BYTE, hiVal, compiler->lvaOutgoingArgSpaceVar,
                                               argOffset + 4);

                    break;
                }
                else
                {
                    genCodeForTree(curr, 0);

                    if (curr->gtFlags & GTF_LATE_ARG)
                    {
                        // The arg was assigned into a temp and
                        // will be moved to the correct register or slot later

                        argSize = 0; // nothing is passed on the stack
                    }
                    else
                    {
                        // The arg is passed in the outgoing argument area of the stack frame
                        //
                        assert(curr->gtOper != GT_ASG); // GTF_LATE_ARG should be set if this is the case
                        assert(curr->InReg());          // should be enregistered after genCodeForTree(curr, 0)

                        if (type == TYP_LONG)
                        {
                            regNumber regLo = genRegPairLo(curr->gtRegPair);
                            regNumber regHi = genRegPairHi(curr->gtRegPair);

                            assert(regLo != REG_STK);
                            inst_SA_RV(ins_Store(TYP_INT), argOffset, regLo, TYP_INT);
                            if (regHi == REG_STK)
                            {
                                regHi = regSet.rsPickFreeReg();
                                inst_RV_TT(ins_Load(TYP_INT), regHi, curr, 4);
                                regTracker.rsTrackRegTrash(regHi);
                            }
                            inst_SA_RV(ins_Store(TYP_INT), argOffset + 4, regHi, TYP_INT);
                        }
                        else // (type == TYP_DOUBLE)
                        {
                            inst_SA_RV(ins_Store(type), argOffset, curr->gtRegNum, type);
                        }
                    }
                }
                break;

#elif defined(_TARGET_64BIT_)
                __fallthrough;
#else
#error "Unknown target for passing TYP_LONG argument using FIXED_ARGS"
#endif

            case TYP_REF:
            case TYP_BYREF:

            case TYP_FLOAT:
            case TYP_INT:
                /* Is the value a constant? */

                if (curr->gtOper == GT_CNS_INT)
                {
                    assert(!(curr->gtFlags & GTF_LATE_ARG));

#if REDUNDANT_LOAD
                    regNumber reg = regTracker.rsIconIsInReg(curr->gtIntCon.gtIconVal);

                    if (reg != REG_NA)
                    {
                        inst_SA_RV(ins_Store(type), argOffset, reg, type);
                    }
                    else
#endif
                    {
                        bool     needReloc = compiler->opts.compReloc && curr->IsIconHandle();
                        emitAttr attr      = needReloc ? EA_HANDLE_CNS_RELOC : emitTypeSize(type);
                        instGen_Store_Imm_Into_Lcl(type, attr, curr->gtIntCon.gtIconVal,
                                                   compiler->lvaOutgoingArgSpaceVar, argOffset);
                    }
                    break;
                }

                /* This is passed as a pointer-sized integer argument */

                genCodeForTree(curr, 0);

                // The arg has been evaluated now, but will be put in a register or pushed on the stack later.
                if (curr->gtFlags & GTF_LATE_ARG)
                {
#ifdef _TARGET_ARM_
                    argSize = 0; // nothing is passed on the stack
#endif
                }
                else
                {
                    // The arg is passed in the outgoing argument area of the stack frame

                    assert(curr->gtOper != GT_ASG); // GTF_LATE_ARG should be set if this is the case
                    assert(curr->InReg());          // should be enregistered after genCodeForTree(curr, 0)
                    inst_SA_RV(ins_Store(type), argOffset, curr->gtRegNum, type);

                    if ((genRegMask(curr->gtRegNum) & regSet.rsMaskUsed) == 0)
                        gcInfo.gcMarkRegSetNpt(genRegMask(curr->gtRegNum));
                }
                break;

            case TYP_VOID:
                /* Is this a nothing node, deferred register argument? */

                if (curr->gtFlags & GTF_LATE_ARG)
                {
                /* Handle side-effects */
                DEFERRED:
                    if (curr->OperIsCopyBlkOp() || curr->OperGet() == GT_COMMA)
                    {
#ifdef _TARGET_ARM_
                        {
                            GenTreePtr curArgNode    = curArgTabEntry->node;
                            var_types  curRegArgType = curArgNode->gtType;
                            assert(curRegArgType != TYP_UNDEF);

                            if (curRegArgType == TYP_STRUCT)
                            {
                                // If the RHS of the COPYBLK is a promoted struct local, then the use of that
                                // is an implicit use of all its field vars.  If these are last uses, remember that,
                                // so we can later update the GC compiler->info.
                                if (curr->OperIsCopyBlkOp())
                                    deadFieldVarRegs |= genFindDeadFieldRegs(curr);
                            }
                        }
#endif // _TARGET_ARM_

                        genCodeForTree(curr, 0);
                    }
                    else
                    {
                        assert(curr->IsArgPlaceHolderNode() || curr->IsNothingNode());
                    }

#if defined(_TARGET_ARM_)
                    argSize = curArgTabEntry->numSlots * TARGET_POINTER_SIZE;
#endif
                }
                else
                {
                    for (GenTree* arg = curr; arg->gtOper == GT_COMMA; arg = arg->gtOp.gtOp2)
                    {
                        GenTreePtr op1 = arg->gtOp.gtOp1;

                        genEvalSideEffects(op1);
                        genUpdateLife(op1);
                    }
                }
                break;

#ifdef _TARGET_ARM_

            case TYP_STRUCT:
            {
                GenTree* arg = curr;
                while (arg->gtOper == GT_COMMA)
                {
                    GenTreePtr op1 = arg->gtOp.gtOp1;
                    genEvalSideEffects(op1);
                    genUpdateLife(op1);
                    arg = arg->gtOp.gtOp2;
                }
                noway_assert((arg->OperGet() == GT_OBJ) || (arg->OperGet() == GT_MKREFANY));

                CORINFO_CLASS_HANDLE clsHnd;
                unsigned             argAlign;
                unsigned             slots;
                BYTE*                gcLayout = NULL;

                // If the struct being passed is a OBJ of a local struct variable that is promoted (in the
                // INDEPENDENT fashion, which doesn't require writes to be written through to the variable's
                // home stack loc) "promotedStructLocalVarDesc" will be set to point to the local variable
                // table entry for the promoted struct local.  As we fill slots with the contents of a
                // promoted struct, "bytesOfNextSlotOfCurPromotedStruct" will be the number of filled bytes
                // that indicate another filled slot, and "nextPromotedStructFieldVar" will be the local
                // variable number of the next field variable to be copied.
                LclVarDsc* promotedStructLocalVarDesc           = NULL;
                GenTreePtr structLocalTree                      = NULL;
                unsigned   bytesOfNextSlotOfCurPromotedStruct   = TARGET_POINTER_SIZE; // Size of slot.
                unsigned   nextPromotedStructFieldVar           = BAD_VAR_NUM;
                unsigned   promotedStructOffsetOfFirstStackSlot = 0;
                unsigned   argOffsetOfFirstStackSlot            = UINT32_MAX; // Indicates uninitialized.

                if (arg->OperGet() == GT_OBJ)
                {
                    clsHnd                = arg->gtObj.gtClass;
                    unsigned originalSize = compiler->info.compCompHnd->getClassSize(clsHnd);
                    argAlign =
                        roundUp(compiler->info.compCompHnd->getClassAlignmentRequirement(clsHnd), TARGET_POINTER_SIZE);
                    argSize = (unsigned)(roundUp(originalSize, TARGET_POINTER_SIZE));

                    slots = (unsigned)(argSize / TARGET_POINTER_SIZE);

                    gcLayout = new (compiler, CMK_Codegen) BYTE[slots];

                    compiler->info.compCompHnd->getClassGClayout(clsHnd, gcLayout);

                    // Are we loading a promoted struct local var?
                    if (arg->gtObj.gtOp1->gtOper == GT_ADDR && arg->gtObj.gtOp1->gtOp.gtOp1->gtOper == GT_LCL_VAR)
                    {
                        structLocalTree         = arg->gtObj.gtOp1->gtOp.gtOp1;
                        unsigned   structLclNum = structLocalTree->gtLclVarCommon.gtLclNum;
                        LclVarDsc* varDsc       = &compiler->lvaTable[structLclNum];

                        // As much as we would like this to be a noway_assert, we can't because
                        // there are some weird casts out there, and backwards compatiblity
                        // dictates we do *NOT* start rejecting them now. lvaGetPromotion and
                        // lvPromoted in general currently do not require the local to be
                        // TYP_STRUCT, so this assert is really more about how we wish the world
                        // was then some JIT invariant.
                        assert((structLocalTree->TypeGet() == TYP_STRUCT) || compiler->compUnsafeCastUsed);

                        Compiler::lvaPromotionType promotionType = compiler->lvaGetPromotionType(varDsc);

                        if (varDsc->lvPromoted &&
                            promotionType == Compiler::PROMOTION_TYPE_INDEPENDENT) // Otherwise it is guaranteed to live
                                                                                   // on stack.
                        {
                            assert(!varDsc->lvAddrExposed); // Compiler::PROMOTION_TYPE_INDEPENDENT ==> not exposed.
                            promotedStructLocalVarDesc = varDsc;
                            nextPromotedStructFieldVar = promotedStructLocalVarDesc->lvFieldLclStart;
                        }
                    }
                }
                else
                {
                    noway_assert(arg->OperGet() == GT_MKREFANY);

                    clsHnd   = NULL;
                    argAlign = TARGET_POINTER_SIZE;
                    argSize  = 2 * TARGET_POINTER_SIZE;
                    slots    = 2;
                }

                // Any TYP_STRUCT argument that is passed in registers must be moved over to the LateArg list
                noway_assert(regNum == REG_STK);

                // This code passes a TYP_STRUCT by value using the outgoing arg space var
                //
                if (arg->OperGet() == GT_OBJ)
                {
                    regNumber regSrc = REG_STK;
                    regNumber regTmp = REG_STK; // This will get set below if the obj is not of a promoted struct local.
                    int       cStackSlots = 0;

                    if (promotedStructLocalVarDesc == NULL)
                    {
                        genComputeReg(arg->gtObj.gtOp1, 0, RegSet::ANY_REG, RegSet::KEEP_REG);
                        noway_assert(arg->gtObj.gtOp1->InReg());
                        regSrc = arg->gtObj.gtOp1->gtRegNum;
                    }

                    // The number of bytes to add "argOffset" to get the arg offset of the current slot.
                    int extraArgOffset = 0;

                    for (unsigned i = 0; i < slots; i++)
                    {
                        emitAttr fieldSize;
                        if (gcLayout[i] == TYPE_GC_NONE)
                            fieldSize = EA_PTRSIZE;
                        else if (gcLayout[i] == TYPE_GC_REF)
                            fieldSize = EA_GCREF;
                        else
                        {
                            noway_assert(gcLayout[i] == TYPE_GC_BYREF);
                            fieldSize = EA_BYREF;
                        }

                        // Pass the argument using the lvaOutgoingArgSpaceVar

                        if (promotedStructLocalVarDesc != NULL)
                        {
                            if (argOffsetOfFirstStackSlot == UINT32_MAX)
                                argOffsetOfFirstStackSlot = argOffset;

                            regNumber maxRegArg       = regNumber(MAX_REG_ARG);
                            bool      filledExtraSlot = genFillSlotFromPromotedStruct(
                                arg, curArgTabEntry, promotedStructLocalVarDesc, fieldSize, &nextPromotedStructFieldVar,
                                &bytesOfNextSlotOfCurPromotedStruct,
                                /*pCurRegNum*/ &maxRegArg,
                                /*argOffset*/ argOffset + extraArgOffset,
                                /*fieldOffsetOfFirstStackSlot*/ promotedStructOffsetOfFirstStackSlot,
                                argOffsetOfFirstStackSlot, &deadFieldVarRegs, &regTmp);
                            extraArgOffset += TARGET_POINTER_SIZE;
                            // If we filled an extra slot with an 8-byte value, skip a slot.
                            if (filledExtraSlot)
                            {
                                i++;
                                cStackSlots++;
                                extraArgOffset += TARGET_POINTER_SIZE;
                            }
                        }
                        else
                        {
                            if (regTmp == REG_STK)
                            {
                                regTmp = regSet.rsPickFreeReg();
                            }

                            getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), fieldSize, regTmp, regSrc,
                                                       i * TARGET_POINTER_SIZE);

                            getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), fieldSize, regTmp,
                                                      compiler->lvaOutgoingArgSpaceVar,
                                                      argOffset + cStackSlots * TARGET_POINTER_SIZE);
                            regTracker.rsTrackRegTrash(regTmp);
                        }
                        cStackSlots++;
                    }

                    if (promotedStructLocalVarDesc == NULL)
                    {
                        regSet.rsMarkRegFree(genRegMask(regSrc));
                    }
                    if (structLocalTree != NULL)
                        genUpdateLife(structLocalTree);
                }
                else
                {
                    assert(arg->OperGet() == GT_MKREFANY);
                    PushMkRefAnyArg(arg, curArgTabEntry, RBM_ALLINT);
                    argSize = (curArgTabEntry->numSlots * TARGET_POINTER_SIZE);
                }
            }
            break;
#endif // _TARGET_ARM_

            default:
                assert(!"unhandled/unexpected arg type");
                NO_WAY("unhandled/unexpected arg type");
        }

        /* Update the current set of live variables */

        genUpdateLife(curr);

        // Now, if some copied field locals were enregistered, and they're now dead, update the set of
        // register holding gc pointers.
        if (deadFieldVarRegs != 0)
            gcInfo.gcMarkRegSetNpt(deadFieldVarRegs);

        /* Update the current argument stack offset */

        argOffset += argSize;

        /* Continue with the next argument, if any more are present */
    } // while (args)

    if (lateArgs)
    {
        SetupLateArgs(call);
    }

    /* Return the total size pushed */

    return 0;
}

#ifdef _TARGET_ARM_
bool CodeGen::genFillSlotFromPromotedStruct(GenTreePtr       arg,
                                            fgArgTabEntryPtr curArgTabEntry,
                                            LclVarDsc*       promotedStructLocalVarDesc,
                                            emitAttr         fieldSize,
                                            unsigned*        pNextPromotedStructFieldVar,
                                            unsigned*        pBytesOfNextSlotOfCurPromotedStruct,
                                            regNumber*       pCurRegNum,
                                            int              argOffset,
                                            int              fieldOffsetOfFirstStackSlot,
                                            int              argOffsetOfFirstStackSlot,
                                            regMaskTP*       deadFieldVarRegs,
                                            regNumber*       pRegTmp)
{
    unsigned nextPromotedStructFieldVar = *pNextPromotedStructFieldVar;
    unsigned limitPromotedStructFieldVar =
        promotedStructLocalVarDesc->lvFieldLclStart + promotedStructLocalVarDesc->lvFieldCnt;
    unsigned bytesOfNextSlotOfCurPromotedStruct = *pBytesOfNextSlotOfCurPromotedStruct;

    regNumber curRegNum       = *pCurRegNum;
    regNumber regTmp          = *pRegTmp;
    bool      filledExtraSlot = false;

    if (nextPromotedStructFieldVar == limitPromotedStructFieldVar)
    {
        // We've already finished; just return.
        // We can reach this because the calling loop computes a # of slots based on the size of the struct.
        // If the struct has padding at the end because of alignment (say, long/int), then we'll get a call for
        // the fourth slot, even though we've copied all the fields.
        return false;
    }

    LclVarDsc* fieldVarDsc = &compiler->lvaTable[nextPromotedStructFieldVar];

    // Does this field fill an entire slot, and does it go at the start of the slot?
    // If so, things are easier...

    bool oneFieldFillsSlotFromStart =
        (fieldVarDsc->lvFldOffset < bytesOfNextSlotOfCurPromotedStruct) // The field should start in the current slot...
        && ((fieldVarDsc->lvFldOffset % 4) == 0)                        // at the start of the slot, and...
        && (nextPromotedStructFieldVar + 1 ==
                limitPromotedStructFieldVar // next field, if there is one, goes in the next slot.
            || compiler->lvaTable[nextPromotedStructFieldVar + 1].lvFldOffset >= bytesOfNextSlotOfCurPromotedStruct);

    // Compute the proper size.
    if (fieldSize == EA_4BYTE) // Not a GC ref or byref.
    {
        switch (fieldVarDsc->lvExactSize)
        {
            case 1:
                fieldSize = EA_1BYTE;
                break;
            case 2:
                fieldSize = EA_2BYTE;
                break;
            case 8:
                // An 8-byte field will be at an 8-byte-aligned offset unless explicit layout has been used,
                // in which case we should not have promoted the struct variable.
                noway_assert((fieldVarDsc->lvFldOffset % 8) == 0);

                // If the current reg number is not aligned, align it, and return to the calling loop, which will
                // consider that a filled slot and move on to the next argument register.
                if (curRegNum != MAX_REG_ARG && ((curRegNum % 2) != 0))
                {
                    // We must update the slot target, however!
                    bytesOfNextSlotOfCurPromotedStruct += 4;
                    *pBytesOfNextSlotOfCurPromotedStruct = bytesOfNextSlotOfCurPromotedStruct;
                    return false;
                }
                // Dest is an aligned pair of arg regs, if the struct type demands it.
                noway_assert((curRegNum % 2) == 0);
                // We leave the fieldSize as EA_4BYTE; but we must do 2 reg moves.
                break;
            default:
                assert(fieldVarDsc->lvExactSize == 4);
                break;
        }
    }
    else
    {
        // If the gc layout said it's a GC ref or byref, then the field size must be 4.
        noway_assert(fieldVarDsc->lvExactSize == 4);
    }

    // We may need the type of the field to influence instruction selection.
    // If we have a TYP_LONG we can use TYP_I_IMPL and we do two loads/stores
    // If the fieldVarDsc is enregistered float we must use the field's exact type
    // however if it is in memory we can use an integer type TYP_I_IMPL
    //
    var_types fieldTypeForInstr = var_types(fieldVarDsc->lvType);
    if ((fieldVarDsc->lvType == TYP_LONG) || (!fieldVarDsc->lvRegister && varTypeIsFloating(fieldTypeForInstr)))
    {
        fieldTypeForInstr = TYP_I_IMPL;
    }

    // If we have a HFA, then it is a much simpler deal -- HFAs are completely enregistered.
    if (curArgTabEntry->isHfaRegArg)
    {
        assert(oneFieldFillsSlotFromStart);

        // Is the field variable promoted?
        if (fieldVarDsc->lvRegister)
        {
            // Move the field var living in register to dst, if they are different registers.
            regNumber srcReg = fieldVarDsc->lvRegNum;
            regNumber dstReg = curRegNum;
            if (srcReg != dstReg)
            {
                inst_RV_RV(ins_Copy(fieldVarDsc->TypeGet()), dstReg, srcReg, fieldVarDsc->TypeGet());
                assert(genIsValidFloatReg(dstReg)); // we don't use register tracking for FP
            }
        }
        else
        {
            // Move the field var living in stack to dst.
            getEmitter()->emitIns_R_S(ins_Load(fieldVarDsc->TypeGet()),
                                      fieldVarDsc->TypeGet() == TYP_DOUBLE ? EA_8BYTE : EA_4BYTE, curRegNum,
                                      nextPromotedStructFieldVar, 0);
            assert(genIsValidFloatReg(curRegNum)); // we don't use register tracking for FP
        }

        // Mark the arg as used and using reg val.
        genMarkTreeInReg(arg, curRegNum);
        regSet.SetUsedRegFloat(arg, true);

        // Advance for double.
        if (fieldVarDsc->TypeGet() == TYP_DOUBLE)
        {
            bytesOfNextSlotOfCurPromotedStruct += 4;
            curRegNum     = REG_NEXT(curRegNum);
            arg->gtRegNum = curRegNum;
            regSet.SetUsedRegFloat(arg, true);
            filledExtraSlot = true;
        }
        arg->gtRegNum = curArgTabEntry->regNum;

        // Advance.
        bytesOfNextSlotOfCurPromotedStruct += 4;
        nextPromotedStructFieldVar++;
    }
    else
    {
        if (oneFieldFillsSlotFromStart)
        {
            // If we write to the stack, offset in outgoing args at which we'll write.
            int fieldArgOffset = argOffsetOfFirstStackSlot + fieldVarDsc->lvFldOffset - fieldOffsetOfFirstStackSlot;
            assert(fieldArgOffset >= 0);

            // Is the source a register or memory?
            if (fieldVarDsc->lvRegister)
            {
                if (fieldTypeForInstr == TYP_DOUBLE)
                {
                    fieldSize = EA_8BYTE;
                }

                // Are we writing to a register or to the stack?
                if (curRegNum != MAX_REG_ARG)
                {
                    // Source is register and Dest is register.

                    instruction insCopy = INS_mov;

                    if (varTypeIsFloating(fieldTypeForInstr))
                    {
                        if (fieldTypeForInstr == TYP_FLOAT)
                        {
                            insCopy = INS_vmov_f2i;
                        }
                        else
                        {
                            assert(fieldTypeForInstr == TYP_DOUBLE);
                            insCopy = INS_vmov_d2i;
                        }
                    }

                    // If the value being copied is a TYP_LONG (8 bytes), it may be in two registers.  Record the second
                    // register (which may become a tmp register, if its held in the argument register that the first
                    // register to be copied will overwrite).
                    regNumber otherRegNum = REG_STK;
                    if (fieldVarDsc->lvType == TYP_LONG)
                    {
                        otherRegNum = fieldVarDsc->lvOtherReg;
                        // Are we about to overwrite?
                        if (otherRegNum == curRegNum)
                        {
                            if (regTmp == REG_STK)
                            {
                                regTmp = regSet.rsPickFreeReg();
                            }
                            // Copy the second register to the temp reg.
                            getEmitter()->emitIns_R_R(INS_mov, fieldSize, regTmp, otherRegNum);
                            regTracker.rsTrackRegCopy(regTmp, otherRegNum);
                            otherRegNum = regTmp;
                        }
                    }

                    if (fieldVarDsc->lvType == TYP_DOUBLE)
                    {
                        assert(curRegNum <= REG_R2);
                        getEmitter()->emitIns_R_R_R(insCopy, fieldSize, curRegNum, genRegArgNext(curRegNum),
                                                    fieldVarDsc->lvRegNum);
                        regTracker.rsTrackRegTrash(curRegNum);
                        regTracker.rsTrackRegTrash(genRegArgNext(curRegNum));
                    }
                    else
                    {
                        // Now do the first register.
                        // It might be the case that it's already in the desired register; if so do nothing.
                        if (curRegNum != fieldVarDsc->lvRegNum)
                        {
                            getEmitter()->emitIns_R_R(insCopy, fieldSize, curRegNum, fieldVarDsc->lvRegNum);
                            regTracker.rsTrackRegCopy(curRegNum, fieldVarDsc->lvRegNum);
                        }
                    }

                    // In either case, mark the arg register as used.
                    regSet.rsMarkArgRegUsedByPromotedFieldArg(arg, curRegNum, EA_IS_GCREF(fieldSize));

                    // Is there a second half of the value?
                    if (fieldVarDsc->lvExactSize == 8)
                    {
                        curRegNum = genRegArgNext(curRegNum);
                        // The second dest reg must also be an argument register.
                        noway_assert(curRegNum < MAX_REG_ARG);

                        // Now, if it's an 8-byte TYP_LONG, we have to do the second 4 bytes.
                        if (fieldVarDsc->lvType == TYP_LONG)
                        {
                            // Copy the second register into the next argument register

                            // If it's a register variable for a TYP_LONG value, then otherReg now should
                            //  hold the second register or it might say that it's in the stack.
                            if (otherRegNum == REG_STK)
                            {
                                // Apparently when we partially enregister, we allocate stack space for the full
                                // 8 bytes, and enregister the low half.  Thus the final TARGET_POINTER_SIZE offset
                                // parameter, to get the high half.
                                getEmitter()->emitIns_R_S(ins_Load(fieldTypeForInstr), fieldSize, curRegNum,
                                                          nextPromotedStructFieldVar, TARGET_POINTER_SIZE);
                                regTracker.rsTrackRegTrash(curRegNum);
                            }
                            else
                            {
                                // The other half is in a register.
                                // Again, it might be the case that it's already in the desired register; if so do
                                // nothing.
                                if (curRegNum != otherRegNum)
                                {
                                    getEmitter()->emitIns_R_R(INS_mov, fieldSize, curRegNum, otherRegNum);
                                    regTracker.rsTrackRegCopy(curRegNum, otherRegNum);
                                }
                            }
                        }

                        // Also mark the 2nd arg register as used.
                        regSet.rsMarkArgRegUsedByPromotedFieldArg(arg, curRegNum, false);
                        // Record the fact that we filled in an extra register slot
                        filledExtraSlot = true;
                    }
                }
                else
                {
                    // Source is register and Dest is memory (OutgoingArgSpace).

                    // Now write the srcReg into the right location in the outgoing argument list.
                    getEmitter()->emitIns_S_R(ins_Store(fieldTypeForInstr), fieldSize, fieldVarDsc->lvRegNum,
                                              compiler->lvaOutgoingArgSpaceVar, fieldArgOffset);

                    if (fieldVarDsc->lvExactSize == 8)
                    {
                        // Now, if it's an 8-byte TYP_LONG, we have to do the second 4 bytes.
                        if (fieldVarDsc->lvType == TYP_LONG)
                        {
                            if (fieldVarDsc->lvOtherReg == REG_STK)
                            {
                                // Source is stack.
                                if (regTmp == REG_STK)
                                {
                                    regTmp = regSet.rsPickFreeReg();
                                }
                                // Apparently if we partially enregister, we allocate stack space for the full
                                // 8 bytes, and enregister the low half.  Thus the final TARGET_POINTER_SIZE offset
                                // parameter, to get the high half.
                                getEmitter()->emitIns_R_S(ins_Load(fieldTypeForInstr), fieldSize, regTmp,
                                                          nextPromotedStructFieldVar, TARGET_POINTER_SIZE);
                                regTracker.rsTrackRegTrash(regTmp);
                                getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), fieldSize, regTmp,
                                                          compiler->lvaOutgoingArgSpaceVar,
                                                          fieldArgOffset + TARGET_POINTER_SIZE);
                            }
                            else
                            {
                                getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), fieldSize, fieldVarDsc->lvOtherReg,
                                                          compiler->lvaOutgoingArgSpaceVar,
                                                          fieldArgOffset + TARGET_POINTER_SIZE);
                            }
                        }
                        // Record the fact that we filled in an extra register slot
                        filledExtraSlot = true;
                    }
                }
                assert(fieldVarDsc->lvTracked); // Must be tracked, since it's enregistered...
                // If the fieldVar becomes dead, then declare the register not to contain a pointer value.
                if (arg->gtFlags & GTF_VAR_DEATH)
                {
                    *deadFieldVarRegs |= genRegMask(fieldVarDsc->lvRegNum);
                    // We don't bother with the second reg of a register pair, since if it has one,
                    // it obviously doesn't hold a pointer.
                }
            }
            else
            {
                // Source is in memory.

                if (curRegNum != MAX_REG_ARG)
                {
                    // Dest is reg.
                    getEmitter()->emitIns_R_S(ins_Load(fieldTypeForInstr), fieldSize, curRegNum,
                                              nextPromotedStructFieldVar, 0);
                    regTracker.rsTrackRegTrash(curRegNum);

                    regSet.rsMarkArgRegUsedByPromotedFieldArg(arg, curRegNum, EA_IS_GCREF(fieldSize));

                    if (fieldVarDsc->lvExactSize == 8)
                    {
                        noway_assert(fieldSize == EA_4BYTE);
                        curRegNum = genRegArgNext(curRegNum);
                        noway_assert(curRegNum < MAX_REG_ARG); // Because of 8-byte alignment.
                        getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), fieldSize, curRegNum,
                                                  nextPromotedStructFieldVar, TARGET_POINTER_SIZE);
                        regTracker.rsTrackRegTrash(curRegNum);
                        regSet.rsMarkArgRegUsedByPromotedFieldArg(arg, curRegNum, EA_IS_GCREF(fieldSize));
                        // Record the fact that we filled in an extra stack slot
                        filledExtraSlot = true;
                    }
                }
                else
                {
                    // Dest is stack.
                    if (regTmp == REG_STK)
                    {
                        regTmp = regSet.rsPickFreeReg();
                    }
                    getEmitter()->emitIns_R_S(ins_Load(fieldTypeForInstr), fieldSize, regTmp,
                                              nextPromotedStructFieldVar, 0);

                    // Now write regTmp into the right location in the outgoing argument list.
                    getEmitter()->emitIns_S_R(ins_Store(fieldTypeForInstr), fieldSize, regTmp,
                                              compiler->lvaOutgoingArgSpaceVar, fieldArgOffset);
                    // We overwrote "regTmp", so erase any previous value we recorded that it contained.
                    regTracker.rsTrackRegTrash(regTmp);

                    if (fieldVarDsc->lvExactSize == 8)
                    {
                        getEmitter()->emitIns_R_S(ins_Load(fieldTypeForInstr), fieldSize, regTmp,
                                                  nextPromotedStructFieldVar, TARGET_POINTER_SIZE);

                        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), fieldSize, regTmp,
                                                  compiler->lvaOutgoingArgSpaceVar,
                                                  fieldArgOffset + TARGET_POINTER_SIZE);
                        // Record the fact that we filled in an extra stack slot
                        filledExtraSlot = true;
                    }
                }
            }

            // Bump up the following if we filled in an extra slot
            if (filledExtraSlot)
                bytesOfNextSlotOfCurPromotedStruct += 4;

            // Go to the next field.
            nextPromotedStructFieldVar++;
            if (nextPromotedStructFieldVar == limitPromotedStructFieldVar)
            {
                fieldVarDsc = NULL;
            }
            else
            {
                // The next field should have the same parent variable, and we should have put the field vars in order
                // sorted by offset.
                assert(fieldVarDsc->lvIsStructField && compiler->lvaTable[nextPromotedStructFieldVar].lvIsStructField &&
                       fieldVarDsc->lvParentLcl == compiler->lvaTable[nextPromotedStructFieldVar].lvParentLcl &&
                       fieldVarDsc->lvFldOffset < compiler->lvaTable[nextPromotedStructFieldVar].lvFldOffset);
                fieldVarDsc = &compiler->lvaTable[nextPromotedStructFieldVar];
            }
            bytesOfNextSlotOfCurPromotedStruct += 4;
        }
        else // oneFieldFillsSlotFromStart == false
        {
            // The current slot should contain more than one field.
            // We'll construct a word in memory for the slot, then load it into a register.
            // (Note that it *may* be possible for the fldOffset to be greater than the largest offset in the current
            // slot, in which case we'll just skip this loop altogether.)
            while (fieldVarDsc != NULL && fieldVarDsc->lvFldOffset < bytesOfNextSlotOfCurPromotedStruct)
            {
                // If it doesn't fill a slot, it can't overflow the slot (again, because we only promote structs
                // whose fields have their natural alignment, and alignment == size on ARM).
                noway_assert(fieldVarDsc->lvFldOffset + fieldVarDsc->lvExactSize <= bytesOfNextSlotOfCurPromotedStruct);

                // If the argument goes to the stack, the offset in the outgoing arg area for the argument.
                int fieldArgOffset = argOffsetOfFirstStackSlot + fieldVarDsc->lvFldOffset - fieldOffsetOfFirstStackSlot;
                noway_assert(argOffset == INT32_MAX ||
                             (argOffset <= fieldArgOffset && fieldArgOffset < argOffset + TARGET_POINTER_SIZE));

                if (fieldVarDsc->lvRegister)
                {
                    if (curRegNum != MAX_REG_ARG)
                    {
                        noway_assert(compiler->lvaPromotedStructAssemblyScratchVar != BAD_VAR_NUM);

                        getEmitter()->emitIns_S_R(ins_Store(fieldTypeForInstr), fieldSize, fieldVarDsc->lvRegNum,
                                                  compiler->lvaPromotedStructAssemblyScratchVar,
                                                  fieldVarDsc->lvFldOffset % 4);
                    }
                    else
                    {
                        // Dest is stack; write directly.
                        getEmitter()->emitIns_S_R(ins_Store(fieldTypeForInstr), fieldSize, fieldVarDsc->lvRegNum,
                                                  compiler->lvaOutgoingArgSpaceVar, fieldArgOffset);
                    }
                }
                else
                {
                    // Source is in memory.

                    // Make sure we have a temporary register to use...
                    if (regTmp == REG_STK)
                    {
                        regTmp = regSet.rsPickFreeReg();
                    }
                    getEmitter()->emitIns_R_S(ins_Load(fieldTypeForInstr), fieldSize, regTmp,
                                              nextPromotedStructFieldVar, 0);
                    regTracker.rsTrackRegTrash(regTmp);

                    if (curRegNum != MAX_REG_ARG)
                    {
                        noway_assert(compiler->lvaPromotedStructAssemblyScratchVar != BAD_VAR_NUM);

                        getEmitter()->emitIns_S_R(ins_Store(fieldTypeForInstr), fieldSize, regTmp,
                                                  compiler->lvaPromotedStructAssemblyScratchVar,
                                                  fieldVarDsc->lvFldOffset % 4);
                    }
                    else
                    {
                        getEmitter()->emitIns_S_R(ins_Store(fieldTypeForInstr), fieldSize, regTmp,
                                                  compiler->lvaOutgoingArgSpaceVar, fieldArgOffset);
                    }
                }
                // Go to the next field.
                nextPromotedStructFieldVar++;
                if (nextPromotedStructFieldVar == limitPromotedStructFieldVar)
                {
                    fieldVarDsc = NULL;
                }
                else
                {
                    // The next field should have the same parent variable, and we should have put the field vars in
                    // order sorted by offset.
                    noway_assert(fieldVarDsc->lvIsStructField &&
                                 compiler->lvaTable[nextPromotedStructFieldVar].lvIsStructField &&
                                 fieldVarDsc->lvParentLcl ==
                                     compiler->lvaTable[nextPromotedStructFieldVar].lvParentLcl &&
                                 fieldVarDsc->lvFldOffset < compiler->lvaTable[nextPromotedStructFieldVar].lvFldOffset);
                    fieldVarDsc = &compiler->lvaTable[nextPromotedStructFieldVar];
                }
            }
            // Now, if we were accumulating into the first scratch word of the outgoing argument space in order to
            // write to an argument register, do so.
            if (curRegNum != MAX_REG_ARG)
            {
                noway_assert(compiler->lvaPromotedStructAssemblyScratchVar != BAD_VAR_NUM);

                getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_4BYTE, curRegNum,
                                          compiler->lvaPromotedStructAssemblyScratchVar, 0);
                regTracker.rsTrackRegTrash(curRegNum);
                regSet.rsMarkArgRegUsedByPromotedFieldArg(arg, curRegNum, EA_IS_GCREF(fieldSize));
            }
            // We've finished a slot; set the goal of the next slot.
            bytesOfNextSlotOfCurPromotedStruct += 4;
        }
    }

    // Write back the updates.
    *pNextPromotedStructFieldVar         = nextPromotedStructFieldVar;
    *pBytesOfNextSlotOfCurPromotedStruct = bytesOfNextSlotOfCurPromotedStruct;
    *pCurRegNum                          = curRegNum;
    *pRegTmp                             = regTmp;

    return filledExtraSlot;
}
#endif // _TARGET_ARM_

regMaskTP CodeGen::genFindDeadFieldRegs(GenTreePtr cpBlk)
{
    noway_assert(cpBlk->OperIsCopyBlkOp()); // Precondition.
    GenTreePtr rhs = cpBlk->gtOp.gtOp1;
    regMaskTP  res = 0;
    if (rhs->OperIsIndir())
    {
        GenTree* addr = rhs->AsIndir()->Addr();
        if (addr->gtOper == GT_ADDR)
        {
            rhs = addr->gtOp.gtOp1;
        }
    }
    if (rhs->OperGet() == GT_LCL_VAR)
    {
        LclVarDsc* rhsDsc = &compiler->lvaTable[rhs->gtLclVarCommon.gtLclNum];
        if (rhsDsc->lvPromoted)
        {
            // It is promoted; iterate over its field vars.
            unsigned fieldVarNum = rhsDsc->lvFieldLclStart;
            for (unsigned i = 0; i < rhsDsc->lvFieldCnt; i++, fieldVarNum++)
            {
                LclVarDsc* fieldVarDsc = &compiler->lvaTable[fieldVarNum];
                // Did the variable go dead, and is it enregistered?
                if (fieldVarDsc->lvRegister && (rhs->gtFlags & GTF_VAR_DEATH))
                {
                    // Add the register number to the set of registers holding field vars that are going dead.
                    res |= genRegMask(fieldVarDsc->lvRegNum);
                }
            }
        }
    }
    return res;
}

void CodeGen::SetupLateArgs(GenTreeCall* call)
{
    GenTreeArgList* lateArgs;
    GenTreePtr      curr;

    /* Generate the code to move the late arguments into registers */

    for (lateArgs = call->gtCallLateArgs; lateArgs; lateArgs = lateArgs->Rest())
    {
        curr = lateArgs->Current();
        assert(curr);

        fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(call, curr);
        assert(curArgTabEntry);
        regNumber regNum    = curArgTabEntry->regNum;
        unsigned  argOffset = curArgTabEntry->slotNum * TARGET_POINTER_SIZE;

        assert(isRegParamType(curr->TypeGet()));
        assert(curr->gtType != TYP_VOID);

        /* If the register is already marked as used, it will become
           multi-used. However, since it is a callee-trashed register,
           we will have to spill it before the call anyway. So do it now */

        {
            // Remember which registers hold pointers. We will spill
            // them, but the code that follows will fetch reg vars from
            // the registers, so we need that gc compiler->info.
            // Also regSet.rsSpillReg doesn't like to spill enregistered
            // variables, but if this is their last use that is *exactly*
            // what we need to do, so we have to temporarily pretend
            // they are no longer live.
            // You might ask why are they in regSet.rsMaskUsed and regSet.rsMaskVars
            // when their last use is about to occur?
            // It is because this is the second operand to be evaluated
            // of some parent binary op, and the first operand is
            // live across this tree, and thought it could re-use the
            // variables register (like a GT_REG_VAR). This probably
            // is caused by RegAlloc assuming the first operand would
            // evaluate into another register.
            regMaskTP rsTemp          = regSet.rsMaskVars & regSet.rsMaskUsed & RBM_CALLEE_TRASH;
            regMaskTP gcRegSavedByref = gcInfo.gcRegByrefSetCur & rsTemp;
            regMaskTP gcRegSavedGCRef = gcInfo.gcRegGCrefSetCur & rsTemp;
            regSet.RemoveMaskVars(rsTemp);

            regNumber regNum2 = regNum;
            for (unsigned i = 0; i < curArgTabEntry->numRegs; i++)
            {
                if (regSet.rsMaskUsed & genRegMask(regNum2))
                {
                    assert(genRegMask(regNum2) & RBM_CALLEE_TRASH);
                    regSet.rsSpillReg(regNum2);
                }
                regNum2 = genRegArgNext(regNum2);
                assert(i + 1 == curArgTabEntry->numRegs || regNum2 != MAX_REG_ARG);
            }

            // Restore gc tracking masks.
            gcInfo.gcRegByrefSetCur |= gcRegSavedByref;
            gcInfo.gcRegGCrefSetCur |= gcRegSavedGCRef;

            // Set maskvars back to normal
            regSet.AddMaskVars(rsTemp);
        }

        /* Evaluate the argument to a register */

        /* Check if this is the guess area for the resolve interface call
         * Pass a size of EA_OFFSET*/
        if (curr->gtOper == GT_CLS_VAR && compiler->eeGetJitDataOffs(curr->gtClsVar.gtClsVarHnd) >= 0)
        {
            getEmitter()->emitIns_R_C(ins_Load(TYP_INT), EA_OFFSET, regNum, curr->gtClsVar.gtClsVarHnd, 0);
            regTracker.rsTrackRegTrash(regNum);

            /* The value is now in the appropriate register */

            genMarkTreeInReg(curr, regNum);

            regSet.rsMarkRegUsed(curr);
        }
#ifdef _TARGET_ARM_
        else if (curr->gtType == TYP_STRUCT)
        {
            GenTree* arg = curr;
            while (arg->gtOper == GT_COMMA)
            {
                GenTreePtr op1 = arg->gtOp.gtOp1;
                genEvalSideEffects(op1);
                genUpdateLife(op1);
                arg = arg->gtOp.gtOp2;
            }
            noway_assert((arg->OperGet() == GT_OBJ) || (arg->OperGet() == GT_LCL_VAR) ||
                         (arg->OperGet() == GT_MKREFANY));

            // This code passes a TYP_STRUCT by value using
            // the argument registers first and
            // then the lvaOutgoingArgSpaceVar area.
            //

            // We prefer to choose low registers here to reduce code bloat
            regMaskTP regNeedMask    = RBM_LOW_REGS;
            unsigned  firstStackSlot = 0;
            unsigned  argAlign       = TARGET_POINTER_SIZE;
            size_t    originalSize   = InferStructOpSizeAlign(arg, &argAlign);

            unsigned slots = (unsigned)(roundUp(originalSize, TARGET_POINTER_SIZE) / TARGET_POINTER_SIZE);
            assert(slots > 0);

            if (regNum == REG_STK)
            {
                firstStackSlot = 0;
            }
            else
            {
                if (argAlign == (TARGET_POINTER_SIZE * 2))
                {
                    assert((regNum & 1) == 0);
                }

                // firstStackSlot is an index of the first slot of the struct
                // that is on the stack, in the range [0,slots]. If it is 'slots',
                // then the entire struct is in registers. It is also equal to
                // the number of slots of the struct that are passed in registers.

                if (curArgTabEntry->isHfaRegArg)
                {
                    // HFA arguments that have been decided to go into registers fit the reg space.
                    assert(regNum >= FIRST_FP_ARGREG && "HFA must go in FP register");
                    assert(regNum + slots - 1 <= LAST_FP_ARGREG &&
                           "HFA argument doesn't fit entirely in FP argument registers");
                    firstStackSlot = slots;
                }
                else if (regNum + slots > MAX_REG_ARG)
                {
                    firstStackSlot = MAX_REG_ARG - regNum;
                    assert(firstStackSlot > 0);
                }
                else
                {
                    firstStackSlot = slots;
                }

                if (curArgTabEntry->isHfaRegArg)
                {
                    // Mask out the registers used by an HFA arg from the ones used to compute tree into.
                    for (unsigned i = regNum; i < regNum + slots; i++)
                    {
                        regNeedMask &= ~genRegMask(regNumber(i));
                    }
                }
            }

            // This holds the set of registers corresponding to enregistered promoted struct field variables
            // that go dead after this use of the variable in the argument list.
            regMaskTP deadFieldVarRegs = RBM_NONE;

            // If the struct being passed is an OBJ of a local struct variable that is promoted (in the
            // INDEPENDENT fashion, which doesn't require writes to be written through to the variables
            // home stack loc) "promotedStructLocalVarDesc" will be set to point to the local variable
            // table entry for the promoted struct local.  As we fill slots with the contents of a
            // promoted struct, "bytesOfNextSlotOfCurPromotedStruct" will be the number of filled bytes
            // that indicate another filled slot (if we have a 12-byte struct, it has 3 four byte slots; when we're
            // working on the second slot, "bytesOfNextSlotOfCurPromotedStruct" will be 8, the point at which we're
            // done), and "nextPromotedStructFieldVar" will be the local variable number of the next field variable
            // to be copied.
            LclVarDsc* promotedStructLocalVarDesc         = NULL;
            unsigned   bytesOfNextSlotOfCurPromotedStruct = 0; // Size of slot.
            unsigned   nextPromotedStructFieldVar         = BAD_VAR_NUM;
            GenTreePtr structLocalTree                    = NULL;

            BYTE*     gcLayout = NULL;
            regNumber regSrc   = REG_NA;
            if (arg->gtOper == GT_OBJ)
            {
                // Are we loading a promoted struct local var?
                if (arg->gtObj.gtOp1->gtOper == GT_ADDR && arg->gtObj.gtOp1->gtOp.gtOp1->gtOper == GT_LCL_VAR)
                {
                    structLocalTree         = arg->gtObj.gtOp1->gtOp.gtOp1;
                    unsigned   structLclNum = structLocalTree->gtLclVarCommon.gtLclNum;
                    LclVarDsc* varDsc       = &compiler->lvaTable[structLclNum];

                    Compiler::lvaPromotionType promotionType = compiler->lvaGetPromotionType(varDsc);

                    if (varDsc->lvPromoted && promotionType == Compiler::PROMOTION_TYPE_INDEPENDENT) // Otherwise it is
                                                                                                     // guaranteed to
                                                                                                     // live on stack.
                    {
                        // Fix 388395 ARM JitStress WP7
                        noway_assert(structLocalTree->TypeGet() == TYP_STRUCT);

                        assert(!varDsc->lvAddrExposed); // Compiler::PROMOTION_TYPE_INDEPENDENT ==> not exposed.
                        promotedStructLocalVarDesc = varDsc;
                        nextPromotedStructFieldVar = promotedStructLocalVarDesc->lvFieldLclStart;
                    }
                }

                if (promotedStructLocalVarDesc == NULL)
                {
                    // If it's not a promoted struct variable, set "regSrc" to the address
                    // of the struct local.
                    genComputeReg(arg->gtObj.gtOp1, regNeedMask, RegSet::EXACT_REG, RegSet::KEEP_REG);
                    noway_assert(arg->gtObj.gtOp1->InReg());
                    regSrc = arg->gtObj.gtOp1->gtRegNum;
                    // Remove this register from the set of registers that we pick from, unless slots equals 1
                    if (slots > 1)
                        regNeedMask &= ~genRegMask(regSrc);
                }

                gcLayout = new (compiler, CMK_Codegen) BYTE[slots];
                compiler->info.compCompHnd->getClassGClayout(arg->gtObj.gtClass, gcLayout);
            }
            else if (arg->gtOper == GT_LCL_VAR)
            {
                // Move the address of the LCL_VAR in arg into reg

                unsigned varNum = arg->gtLclVarCommon.gtLclNum;

                // Are we loading a promoted struct local var?
                structLocalTree         = arg;
                unsigned   structLclNum = structLocalTree->gtLclVarCommon.gtLclNum;
                LclVarDsc* varDsc       = &compiler->lvaTable[structLclNum];

                noway_assert(structLocalTree->TypeGet() == TYP_STRUCT);

                Compiler::lvaPromotionType promotionType = compiler->lvaGetPromotionType(varDsc);

                if (varDsc->lvPromoted && promotionType == Compiler::PROMOTION_TYPE_INDEPENDENT) // Otherwise it is
                                                                                                 // guaranteed to live
                                                                                                 // on stack.
                {
                    assert(!varDsc->lvAddrExposed); // Compiler::PROMOTION_TYPE_INDEPENDENT ==> not exposed.
                    promotedStructLocalVarDesc = varDsc;
                    nextPromotedStructFieldVar = promotedStructLocalVarDesc->lvFieldLclStart;
                }

                if (promotedStructLocalVarDesc == NULL)
                {
                    regSrc = regSet.rsPickFreeReg(regNeedMask);
                    // Remove this register from the set of registers that we pick from, unless slots equals 1
                    if (slots > 1)
                        regNeedMask &= ~genRegMask(regSrc);

                    getEmitter()->emitIns_R_S(INS_lea, EA_PTRSIZE, regSrc, varNum, 0);
                    regTracker.rsTrackRegTrash(regSrc);
                    gcLayout = compiler->lvaGetGcLayout(varNum);
                }
            }
            else if (arg->gtOper == GT_MKREFANY)
            {
                assert(slots == 2);
                assert((firstStackSlot == 1) || (firstStackSlot == 2));
                assert(argOffset == 0); // ???
                PushMkRefAnyArg(arg, curArgTabEntry, regNeedMask);

                // Adjust argOffset if part of this guy was pushed onto the stack
                if (firstStackSlot < slots)
                {
                    argOffset += TARGET_POINTER_SIZE;
                }

                // Skip the copy loop below because we have already placed the argument in the right place
                slots    = 0;
                gcLayout = NULL;
            }
            else
            {
                assert(!"Unsupported TYP_STRUCT arg kind");
                gcLayout = new (compiler, CMK_Codegen) BYTE[slots];
            }

            if (promotedStructLocalVarDesc != NULL)
            {
                // We must do do the stack parts first, since those might need values
                // from argument registers that will be overwritten in the portion of the
                // loop that writes into the argument registers.
                bytesOfNextSlotOfCurPromotedStruct = (firstStackSlot + 1) * TARGET_POINTER_SIZE;
                // Now find the var number of the first that starts in the first stack slot.
                unsigned fieldVarLim =
                    promotedStructLocalVarDesc->lvFieldLclStart + promotedStructLocalVarDesc->lvFieldCnt;
                while (compiler->lvaTable[nextPromotedStructFieldVar].lvFldOffset <
                           (firstStackSlot * TARGET_POINTER_SIZE) &&
                       nextPromotedStructFieldVar < fieldVarLim)
                {
                    nextPromotedStructFieldVar++;
                }
                // If we reach the limit, meaning there is no field that goes even partly in the stack, only if the
                // first stack slot is after the last slot.
                assert(nextPromotedStructFieldVar < fieldVarLim || firstStackSlot >= slots);
            }

            if (slots > 0) // the mkref case may have set "slots" to zero.
            {
                // First pass the stack portion of the struct (if any)
                //
                int argOffsetOfFirstStackSlot = argOffset;
                for (unsigned i = firstStackSlot; i < slots; i++)
                {
                    emitAttr fieldSize;
                    if (gcLayout[i] == TYPE_GC_NONE)
                        fieldSize = EA_PTRSIZE;
                    else if (gcLayout[i] == TYPE_GC_REF)
                        fieldSize = EA_GCREF;
                    else
                    {
                        noway_assert(gcLayout[i] == TYPE_GC_BYREF);
                        fieldSize = EA_BYREF;
                    }

                    regNumber maxRegArg = regNumber(MAX_REG_ARG);
                    if (promotedStructLocalVarDesc != NULL)
                    {
                        regNumber regTmp = REG_STK;

                        bool filledExtraSlot =
                            genFillSlotFromPromotedStruct(arg, curArgTabEntry, promotedStructLocalVarDesc, fieldSize,
                                                          &nextPromotedStructFieldVar,
                                                          &bytesOfNextSlotOfCurPromotedStruct,
                                                          /*pCurRegNum*/ &maxRegArg, argOffset,
                                                          /*fieldOffsetOfFirstStackSlot*/ firstStackSlot *
                                                              TARGET_POINTER_SIZE,
                                                          argOffsetOfFirstStackSlot, &deadFieldVarRegs, &regTmp);
                        if (filledExtraSlot)
                        {
                            i++;
                            argOffset += TARGET_POINTER_SIZE;
                        }
                    }
                    else // (promotedStructLocalVarDesc == NULL)
                    {
                        // when slots > 1, we perform multiple load/stores thus regTmp cannot be equal to regSrc
                        // and although regSrc has been excluded from regNeedMask, regNeedMask is only a *hint*
                        // to regSet.rsPickFreeReg, so we need to be a little more forceful.
                        // Otherwise, just re-use the same register.
                        //
                        regNumber regTmp = regSrc;
                        if (slots != 1)
                        {
                            regMaskTP regSrcUsed;
                            regSet.rsLockReg(genRegMask(regSrc), &regSrcUsed);

                            regTmp = regSet.rsPickFreeReg(regNeedMask);

                            noway_assert(regTmp != regSrc);

                            regSet.rsUnlockReg(genRegMask(regSrc), regSrcUsed);
                        }

                        getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), fieldSize, regTmp, regSrc,
                                                   i * TARGET_POINTER_SIZE);

                        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), fieldSize, regTmp,
                                                  compiler->lvaOutgoingArgSpaceVar, argOffset);
                        regTracker.rsTrackRegTrash(regTmp);
                    }
                    argOffset += TARGET_POINTER_SIZE;
                }

                // Now pass the register portion of the struct
                //

                bytesOfNextSlotOfCurPromotedStruct = TARGET_POINTER_SIZE;
                if (promotedStructLocalVarDesc != NULL)
                    nextPromotedStructFieldVar = promotedStructLocalVarDesc->lvFieldLclStart;

                // Create a nested loop here so that the first time thru the loop
                // we setup all of the regArg registers except for possibly
                // the one that would overwrite regSrc.  Then in the final loop
                // (if necessary) we just setup regArg/regSrc with the overwrite
                //
                bool overwriteRegSrc     = false;
                bool needOverwriteRegSrc = false;
                do
                {
                    if (needOverwriteRegSrc)
                        overwriteRegSrc = true;

                    for (unsigned i = 0; i < firstStackSlot; i++)
                    {
                        regNumber regArg = (regNumber)(regNum + i);

                        if (overwriteRegSrc == false)
                        {
                            if (regArg == regSrc)
                            {
                                needOverwriteRegSrc = true;
                                continue;
                            }
                        }
                        else
                        {
                            if (regArg != regSrc)
                                continue;
                        }

                        emitAttr fieldSize;
                        if (gcLayout[i] == TYPE_GC_NONE)
                            fieldSize = EA_PTRSIZE;
                        else if (gcLayout[i] == TYPE_GC_REF)
                            fieldSize = EA_GCREF;
                        else
                        {
                            noway_assert(gcLayout[i] == TYPE_GC_BYREF);
                            fieldSize = EA_BYREF;
                        }

                        regNumber regTmp = REG_STK;
                        if (promotedStructLocalVarDesc != NULL)
                        {
                            bool filledExtraSlot =
                                genFillSlotFromPromotedStruct(arg, curArgTabEntry, promotedStructLocalVarDesc,
                                                              fieldSize, &nextPromotedStructFieldVar,
                                                              &bytesOfNextSlotOfCurPromotedStruct,
                                                              /*pCurRegNum*/ &regArg,
                                                              /*argOffset*/ INT32_MAX,
                                                              /*fieldOffsetOfFirstStackSlot*/ INT32_MAX,
                                                              /*argOffsetOfFirstStackSlot*/ INT32_MAX,
                                                              &deadFieldVarRegs, &regTmp);
                            if (filledExtraSlot)
                                i++;
                        }
                        else
                        {
                            getEmitter()->emitIns_R_AR(ins_Load(curArgTabEntry->isHfaRegArg ? TYP_FLOAT : TYP_I_IMPL),
                                                       fieldSize, regArg, regSrc, i * TARGET_POINTER_SIZE);
                        }
                        regTracker.rsTrackRegTrash(regArg);
                    }
                } while (needOverwriteRegSrc != overwriteRegSrc);
            }

            if ((arg->gtOper == GT_OBJ) && (promotedStructLocalVarDesc == NULL))
            {
                regSet.rsMarkRegFree(genRegMask(regSrc));
            }

            if (regNum != REG_STK && promotedStructLocalVarDesc == NULL) // If promoted, we already declared the regs
                                                                         // used.
            {
                arg->SetInReg();
                for (unsigned i = 1; i < firstStackSlot; i++)
                {
                    arg->gtRegNum = (regNumber)(regNum + i);
                    curArgTabEntry->isHfaRegArg ? regSet.SetUsedRegFloat(arg, true) : regSet.rsMarkRegUsed(arg);
                }
                arg->gtRegNum = regNum;
                curArgTabEntry->isHfaRegArg ? regSet.SetUsedRegFloat(arg, true) : regSet.rsMarkRegUsed(arg);
            }

            // If we're doing struct promotion, the liveness of the promoted field vars may change after this use,
            // so update liveness.
            genUpdateLife(arg);

            // Now, if some copied field locals were enregistered, and they're now dead, update the set of
            // register holding gc pointers.
            if (deadFieldVarRegs != RBM_NONE)
                gcInfo.gcMarkRegSetNpt(deadFieldVarRegs);
        }
        else if (curr->gtType == TYP_LONG || curr->gtType == TYP_ULONG)
        {
            if (curArgTabEntry->regNum == REG_STK)
            {
                // The arg is passed in the outgoing argument area of the stack frame
                genCompIntoFreeRegPair(curr, RBM_NONE, RegSet::FREE_REG);
                assert(curr->InReg()); // should be enregistered after genCompIntoFreeRegPair(curr, 0)

                inst_SA_RV(ins_Store(TYP_INT), argOffset + 0, genRegPairLo(curr->gtRegPair), TYP_INT);
                inst_SA_RV(ins_Store(TYP_INT), argOffset + 4, genRegPairHi(curr->gtRegPair), TYP_INT);
            }
            else
            {
                assert(regNum < REG_ARG_LAST);
                regPairNo regPair = gen2regs2pair(regNum, REG_NEXT(regNum));
                genComputeRegPair(curr, regPair, RBM_NONE, RegSet::FREE_REG, false);
                assert(curr->gtRegPair == regPair);
                regSet.rsMarkRegPairUsed(curr);
            }
        }
#endif // _TARGET_ARM_
        else if (curArgTabEntry->regNum == REG_STK)
        {
            // The arg is passed in the outgoing argument area of the stack frame
            //
            genCodeForTree(curr, 0);
            assert(curr->InReg()); // should be enregistered after genCodeForTree(curr, 0)

            inst_SA_RV(ins_Store(curr->gtType), argOffset, curr->gtRegNum, curr->gtType);

            if ((genRegMask(curr->gtRegNum) & regSet.rsMaskUsed) == 0)
                gcInfo.gcMarkRegSetNpt(genRegMask(curr->gtRegNum));
        }
        else
        {
            if (!varTypeIsFloating(curr->gtType))
            {
                genComputeReg(curr, genRegMask(regNum), RegSet::EXACT_REG, RegSet::FREE_REG, false);
                assert(curr->gtRegNum == regNum);
                regSet.rsMarkRegUsed(curr);
            }
            else // varTypeIsFloating(curr->gtType)
            {
                if (genIsValidFloatReg(regNum))
                {
                    genComputeReg(curr, genRegMaskFloat(regNum, curr->gtType), RegSet::EXACT_REG, RegSet::FREE_REG,
                                  false);
                    assert(curr->gtRegNum == regNum);
                    regSet.rsMarkRegUsed(curr);
                }
                else
                {
                    genCodeForTree(curr, 0);
                    // If we are loading a floating point type into integer registers
                    // then it must be for varargs.
                    // genCodeForTree will load it into a floating point register,
                    // now copy it into the correct integer register(s)
                    if (curr->TypeGet() == TYP_FLOAT)
                    {
                        assert(genRegMask(regNum) & RBM_CALLEE_TRASH);
                        regSet.rsSpillRegIfUsed(regNum);
#ifdef _TARGET_ARM_
                        getEmitter()->emitIns_R_R(INS_vmov_f2i, EA_4BYTE, regNum, curr->gtRegNum);
#else
#error "Unsupported target"
#endif
                        regTracker.rsTrackRegTrash(regNum);

                        curr->gtType   = TYP_INT; // Change this to TYP_INT in case we need to spill this register
                        curr->gtRegNum = regNum;
                        regSet.rsMarkRegUsed(curr);
                    }
                    else
                    {
                        assert(curr->TypeGet() == TYP_DOUBLE);
                        regNumber intRegNumLo = regNum;
                        curr->gtType = TYP_LONG; // Change this to TYP_LONG in case we spill this
#ifdef _TARGET_ARM_
                        regNumber intRegNumHi = regNumber(intRegNumLo + 1);
                        assert(genRegMask(intRegNumHi) & RBM_CALLEE_TRASH);
                        assert(genRegMask(intRegNumLo) & RBM_CALLEE_TRASH);
                        regSet.rsSpillRegIfUsed(intRegNumHi);
                        regSet.rsSpillRegIfUsed(intRegNumLo);

                        getEmitter()->emitIns_R_R_R(INS_vmov_d2i, EA_8BYTE, intRegNumLo, intRegNumHi, curr->gtRegNum);
                        regTracker.rsTrackRegTrash(intRegNumLo);
                        regTracker.rsTrackRegTrash(intRegNumHi);
                        curr->gtRegPair = gen2regs2pair(intRegNumLo, intRegNumHi);
                        regSet.rsMarkRegPairUsed(curr);
#else
#error "Unsupported target"
#endif
                    }
                }
            }
        }
    }

    /* If any of the previously loaded arguments were spilled - reload them */

    for (lateArgs = call->gtCallLateArgs; lateArgs; lateArgs = lateArgs->Rest())
    {
        curr = lateArgs->Current();
        assert(curr);

        if (curr->gtFlags & GTF_SPILLED)
        {
            if (isRegPairType(curr->gtType))
            {
                regSet.rsUnspillRegPair(curr, genRegPairMask(curr->gtRegPair), RegSet::KEEP_REG);
            }
            else
            {
                regSet.rsUnspillReg(curr, genRegMask(curr->gtRegNum), RegSet::KEEP_REG);
            }
        }
    }
}

#ifdef _TARGET_ARM_

// 'Push' a single GT_MKREFANY argument onto a call's argument list
// The argument is passed as described by the fgArgTabEntry
// If any part of the struct is to be passed in a register the
// regNum value will be equal to the the registers used to pass the
// the first part of the struct.
// If any part is to go onto the stack, we first generate the
// value into a register specified by 'regNeedMask' and
// then store it to the out going argument area.
// When this method returns, both parts of the TypeReference have
// been pushed onto the stack, but *no* registers have been marked
// as 'in-use', that is the responsibility of the caller.
//
void CodeGen::PushMkRefAnyArg(GenTreePtr mkRefAnyTree, fgArgTabEntryPtr curArgTabEntry, regMaskTP regNeedMask)
{
    regNumber regNum = curArgTabEntry->regNum;
    regNumber regNum2;
    assert(mkRefAnyTree->gtOper == GT_MKREFANY);
    regMaskTP arg1RegMask = 0;
    int       argOffset   = curArgTabEntry->slotNum * TARGET_POINTER_SIZE;

    // Construct the TypedReference directly into the argument list of the call by
    // 'pushing' the first field of the typed reference: the pointer.
    // Do this by directly generating it into the argument register or outgoing arg area of the stack.
    // Mark it as used so we don't trash it while generating the second field.
    //
    if (regNum == REG_STK)
    {
        genComputeReg(mkRefAnyTree->gtOp.gtOp1, regNeedMask, RegSet::EXACT_REG, RegSet::FREE_REG);
        noway_assert(mkRefAnyTree->gtOp.gtOp1->InReg());
        regNumber tmpReg1 = mkRefAnyTree->gtOp.gtOp1->gtRegNum;
        inst_SA_RV(ins_Store(TYP_I_IMPL), argOffset, tmpReg1, TYP_I_IMPL);
        regTracker.rsTrackRegTrash(tmpReg1);
        argOffset += TARGET_POINTER_SIZE;
        regNum2 = REG_STK;
    }
    else
    {
        assert(regNum <= REG_ARG_LAST);
        arg1RegMask = genRegMask(regNum);
        genComputeReg(mkRefAnyTree->gtOp.gtOp1, arg1RegMask, RegSet::EXACT_REG, RegSet::KEEP_REG);
        regNum2 = (regNum == REG_ARG_LAST) ? REG_STK : genRegArgNext(regNum);
    }

    // Now 'push' the second field of the typed reference: the method table.
    if (regNum2 == REG_STK)
    {
        genComputeReg(mkRefAnyTree->gtOp.gtOp2, regNeedMask, RegSet::EXACT_REG, RegSet::FREE_REG);
        noway_assert(mkRefAnyTree->gtOp.gtOp2->InReg());
        regNumber tmpReg2 = mkRefAnyTree->gtOp.gtOp2->gtRegNum;
        inst_SA_RV(ins_Store(TYP_I_IMPL), argOffset, tmpReg2, TYP_I_IMPL);
        regTracker.rsTrackRegTrash(tmpReg2);
    }
    else
    {
        assert(regNum2 <= REG_ARG_LAST);
        // We don't have to mark this register as being in use here because it will
        // be done by the caller, and we don't want to double-count it.
        genComputeReg(mkRefAnyTree->gtOp.gtOp2, genRegMask(regNum2), RegSet::EXACT_REG, RegSet::FREE_REG);
    }

    // Now that we are done generating the second part of the TypeReference, we can mark
    // the first register as free.
    // The caller in the shared path we will re-mark all registers used by this argument
    // as being used, so we don't want to double-count this one.
    if (arg1RegMask != 0)
    {
        GenTreePtr op1 = mkRefAnyTree->gtOp.gtOp1;
        if (op1->gtFlags & GTF_SPILLED)
        {
            /* The register that we loaded arg1 into has been spilled -- reload it back into the correct arg register */

            regSet.rsUnspillReg(op1, arg1RegMask, RegSet::FREE_REG);
        }
        else
        {
            regSet.rsMarkRegFree(arg1RegMask);
        }
    }
}
#endif // _TARGET_ARM_

#endif // FEATURE_FIXED_OUT_ARGS

regMaskTP CodeGen::genLoadIndirectCallTarget(GenTreeCall* call)
{
    assert((gtCallTypes)call->gtCallType == CT_INDIRECT);

    regMaskTP fptrRegs;

    /* Loading the indirect call target might cause one or more of the previously
       loaded argument registers to be spilled. So, we save information about all
       the argument registers, and unspill any of them that get spilled, after
       the call target is loaded.
    */
    struct
    {
        GenTreePtr node;
        union {
            regNumber regNum;
            regPairNo regPair;
        };
    } regArgTab[MAX_REG_ARG];

    /* Record the previously loaded arguments, if any */

    unsigned  regIndex;
    regMaskTP prefRegs = regSet.rsRegMaskFree();
    regMaskTP argRegs  = RBM_NONE;
    for (regIndex = 0; regIndex < MAX_REG_ARG; regIndex++)
    {
        regMaskTP  mask;
        regNumber  regNum        = genMapRegArgNumToRegNum(regIndex, TYP_INT);
        GenTreePtr argTree       = regSet.rsUsedTree[regNum];
        regArgTab[regIndex].node = argTree;
        if ((argTree != NULL) && (argTree->gtType != TYP_STRUCT)) // We won't spill the struct
        {
            assert(argTree->InReg());
            if (isRegPairType(argTree->gtType))
            {
                regPairNo regPair = argTree->gtRegPair;
                assert(regNum == genRegPairHi(regPair) || regNum == genRegPairLo(regPair));
                regArgTab[regIndex].regPair = regPair;
                mask                        = genRegPairMask(regPair);
            }
            else
            {
                assert(regNum == argTree->gtRegNum);
                regArgTab[regIndex].regNum = regNum;
                mask                       = genRegMask(regNum);
            }
            assert(!(prefRegs & mask));
            argRegs |= mask;
        }
    }

    /* Record the register(s) used for the indirect call func ptr */
    fptrRegs = genMakeRvalueAddressable(call->gtCallAddr, prefRegs, RegSet::KEEP_REG, false);

    /* If any of the previously loaded arguments were spilled, reload them */

    for (regIndex = 0; regIndex < MAX_REG_ARG; regIndex++)
    {
        GenTreePtr argTree = regArgTab[regIndex].node;
        if ((argTree != NULL) && (argTree->gtFlags & GTF_SPILLED))
        {
            assert(argTree->gtType != TYP_STRUCT); // We currently don't support spilling structs in argument registers
            if (isRegPairType(argTree->gtType))
            {
                regSet.rsUnspillRegPair(argTree, genRegPairMask(regArgTab[regIndex].regPair), RegSet::KEEP_REG);
            }
            else
            {
                regSet.rsUnspillReg(argTree, genRegMask(regArgTab[regIndex].regNum), RegSet::KEEP_REG);
            }
        }
    }

    /* Make sure the target is still addressable while avoiding the argument registers */

    fptrRegs = genKeepAddressable(call->gtCallAddr, fptrRegs, argRegs);

    return fptrRegs;
}

/*****************************************************************************
 *
 *  Generate code for a call. If the call returns a value in register(s), the
 *  register mask that describes where the result will be found is returned;
 *  otherwise, RBM_NONE is returned.
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
regMaskTP CodeGen::genCodeForCall(GenTreeCall* call, bool valUsed)
{
    emitAttr              retSize;
    size_t                argSize;
    size_t                args;
    regMaskTP             retVal;
    emitter::EmitCallType emitCallType;

    unsigned saveStackLvl;

    BasicBlock* returnLabel   = DUMMY_INIT(NULL);
    LclVarDsc*  frameListRoot = NULL;

    unsigned savCurIntArgReg;
    unsigned savCurFloatArgReg;

    unsigned areg;

    regMaskTP fptrRegs = RBM_NONE;
    regMaskTP vptrMask = RBM_NONE;

#ifdef DEBUG
    unsigned stackLvl = getEmitter()->emitCurStackLvl;

    if (compiler->verbose)
    {
        printf("\t\t\t\t\t\t\tBeg call ");
        Compiler::printTreeID(call);
        printf(" stack %02u [E=%02u]\n", genStackLevel, stackLvl);
    }
#endif

#ifdef _TARGET_ARM_
    if (compiler->opts.ShouldUsePInvokeHelpers() && (call->gtFlags & GTF_CALL_UNMANAGED) &&
        ((call->gtFlags & GTF_CALL_VIRT_KIND_MASK) == GTF_CALL_NONVIRT))
    {
        (void)genPInvokeCallProlog(nullptr, 0, (CORINFO_METHOD_HANDLE) nullptr, nullptr);
    }
#endif

    gtCallTypes callType = (gtCallTypes)call->gtCallType;
    IL_OFFSETX  ilOffset = BAD_IL_OFFSET;

    CORINFO_SIG_INFO* sigInfo = nullptr;

    if (compiler->opts.compDbgInfo && compiler->genCallSite2ILOffsetMap != NULL)
    {
        (void)compiler->genCallSite2ILOffsetMap->Lookup(call, &ilOffset);
    }

    /* Make some sanity checks on the call node */

    // "this" only makes sense for user functions
    noway_assert(call->gtCallObjp == 0 || callType == CT_USER_FUNC || callType == CT_INDIRECT);
    // tailcalls won't be done for helpers, caller-pop args, and check that
    // the global flag is set
    noway_assert(!call->IsTailCall() ||
                 (callType != CT_HELPER && !(call->gtFlags & GTF_CALL_POP_ARGS) && compiler->compTailCallUsed));

#ifdef DEBUG
    // Pass the call signature information down into the emitter so the emitter can associate
    // native call sites with the signatures they were generated from.
    if (callType != CT_HELPER)
    {
        sigInfo = call->callSig;
    }
#endif // DEBUG

    unsigned pseudoStackLvl = 0;

    if (!isFramePointerUsed() && (genStackLevel != 0) && compiler->fgIsThrowHlpBlk(compiler->compCurBB))
    {
        noway_assert(compiler->compCurBB->bbTreeList->gtStmt.gtStmtExpr == call);

        pseudoStackLvl = genStackLevel;

        noway_assert(!"Blocks with non-empty stack on entry are NYI in the emitter "
                      "so fgAddCodeRef() should have set isFramePointerRequired()");
    }

    /* Mark the current stack level and list of pointer arguments */

    saveStackLvl = genStackLevel;

    /*-------------------------------------------------------------------------
     *  Set up the registers and arguments
     */

    /* We'll keep track of how much we've pushed on the stack */

    argSize = 0;

    /* We need to get a label for the return address with the proper stack depth. */
    /* For the callee pops case (the default) that is before the args are pushed. */

    if ((call->gtFlags & GTF_CALL_UNMANAGED) && !(call->gtFlags & GTF_CALL_POP_ARGS))
    {
        returnLabel = genCreateTempLabel();
    }

    /*
        Make sure to save the current argument register status
        in case we have nested calls.
     */

    noway_assert(intRegState.rsCurRegArgNum <= MAX_REG_ARG);
    savCurIntArgReg              = intRegState.rsCurRegArgNum;
    savCurFloatArgReg            = floatRegState.rsCurRegArgNum;
    intRegState.rsCurRegArgNum   = 0;
    floatRegState.rsCurRegArgNum = 0;

    /* Pass the arguments */

    if ((call->gtCallObjp != NULL) || (call->gtCallArgs != NULL))
    {
        argSize += genPushArgList(call);
    }

    /* We need to get a label for the return address with the proper stack depth. */
    /* For the caller pops case (cdecl) that is after the args are pushed. */

    if (call->gtFlags & GTF_CALL_UNMANAGED)
    {
        if (call->gtFlags & GTF_CALL_POP_ARGS)
            returnLabel = genCreateTempLabel();

        /* Make sure that we now have a label */
        noway_assert(returnLabel != DUMMY_INIT(NULL));
    }

    if (callType == CT_INDIRECT)
    {
        fptrRegs = genLoadIndirectCallTarget(call);
    }

    /* Make sure any callee-trashed registers are saved */

    regMaskTP calleeTrashedRegs = RBM_NONE;

#if GTF_CALL_REG_SAVE
    if (call->gtFlags & GTF_CALL_REG_SAVE)
    {
        /* The return value reg(s) will definitely be trashed */

        switch (call->gtType)
        {
            case TYP_INT:
            case TYP_REF:
            case TYP_BYREF:
#if !CPU_HAS_FP_SUPPORT
            case TYP_FLOAT:
#endif
                calleeTrashedRegs = RBM_INTRET;
                break;

            case TYP_LONG:
#if !CPU_HAS_FP_SUPPORT
            case TYP_DOUBLE:
#endif
                calleeTrashedRegs = RBM_LNGRET;
                break;

            case TYP_VOID:
#if CPU_HAS_FP_SUPPORT
            case TYP_FLOAT:
            case TYP_DOUBLE:
#endif
                calleeTrashedRegs = 0;
                break;

            default:
                noway_assert(!"unhandled/unexpected type");
        }
    }
    else
#endif
    {
        calleeTrashedRegs = RBM_CALLEE_TRASH;
    }

    /* Spill any callee-saved registers which are being used */

    regMaskTP spillRegs = calleeTrashedRegs & regSet.rsMaskUsed;

    /* We need to save all GC registers to the InlinedCallFrame.
       Instead, just spill them to temps. */

    if (call->gtFlags & GTF_CALL_UNMANAGED)
        spillRegs |= (gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur) & regSet.rsMaskUsed;

    // Ignore fptrRegs as it is needed only to perform the indirect call

    spillRegs &= ~fptrRegs;

    /* Do not spill the argument registers.
       Multi-use of RBM_ARG_REGS should be prevented by genPushArgList() */

    noway_assert((regSet.rsMaskMult & call->gtCallRegUsedMask) == 0);
    spillRegs &= ~call->gtCallRegUsedMask;

    if (spillRegs)
    {
        regSet.rsSpillRegs(spillRegs);
    }

#if FEATURE_STACK_FP_X87
    // Spill fp stack
    SpillForCallStackFP();

    if (call->gtType == TYP_FLOAT || call->gtType == TYP_DOUBLE)
    {
        // Pick up a reg
        regNumber regReturn = regSet.PickRegFloat();

        // Assign reg to tree
        genMarkTreeInReg(call, regReturn);

        // Mark as used
        regSet.SetUsedRegFloat(call, true);

        // Update fp state
        compCurFPState.Push(regReturn);
    }
#else
    SpillForCallRegisterFP(call->gtCallRegUsedMask);
#endif

    /* If the method returns a GC ref, set size to EA_GCREF or EA_BYREF */

    retSize = EA_PTRSIZE;

    if (valUsed)
    {
        if (call->gtType == TYP_REF || call->gtType == TYP_ARRAY)
        {
            retSize = EA_GCREF;
        }
        else if (call->gtType == TYP_BYREF)
        {
            retSize = EA_BYREF;
        }
    }

    /*-------------------------------------------------------------------------
     * For caller-pop calls, the GC info will report the arguments as pending
       arguments as the caller explicitly pops them. Also should be
       reported as non-GC arguments as they effectively go dead at the
       call site (callee owns them)
     */

    args = (call->gtFlags & GTF_CALL_POP_ARGS) ? -int(argSize) : argSize;

#ifdef PROFILING_SUPPORTED

    /*-------------------------------------------------------------------------
     *  Generate the profiling hooks for the call
     */

    /* Treat special cases first */

    /* fire the event at the call site */
    /* alas, right now I can only handle calls via a method handle */
    if (compiler->compIsProfilerHookNeeded() && (callType == CT_USER_FUNC) && call->IsTailCall())
    {
        unsigned saveStackLvl2 = genStackLevel;

        //
        // Push the profilerHandle
        //
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_X86_
        regMaskTP byrefPushedRegs;
        regMaskTP norefPushedRegs;
        regMaskTP pushedArgRegs = genPushRegs(call->gtCallRegUsedMask, &byrefPushedRegs, &norefPushedRegs);

        if (compiler->compProfilerMethHndIndirected)
        {
            getEmitter()->emitIns_AR_R(INS_push, EA_PTR_DSP_RELOC, REG_NA, REG_NA,
                                       (ssize_t)compiler->compProfilerMethHnd);
        }
        else
        {
            inst_IV(INS_push, (size_t)compiler->compProfilerMethHnd);
        }
        genSinglePush();

        genEmitHelperCall(CORINFO_HELP_PROF_FCN_TAILCALL,
                          sizeof(int) * 1, // argSize
                          EA_UNKNOWN);     // retSize

        //
        // Adjust the number of stack slots used by this managed method if necessary.
        //
        if (compiler->fgPtrArgCntMax < 1)
        {
            JITDUMP("Upping fgPtrArgCntMax from %d to 1\n", compiler->fgPtrArgCntMax);
            compiler->fgPtrArgCntMax = 1;
        }

        genPopRegs(pushedArgRegs, byrefPushedRegs, norefPushedRegs);
#elif _TARGET_ARM_
        // We need r0 (to pass profiler handle) and another register (call target) to emit a tailcall callback.
        // To make r0 available, we add REG_PROFILER_TAIL_SCRATCH as an additional interference for tail prefixed calls.
        // Here we grab a register to temporarily store r0 and revert it back after we have emitted callback.
        //
        // By the time we reach this point argument registers are setup (by genPushArgList()), therefore we don't want
        // to disturb them and hence argument registers are locked here.
        regMaskTP usedMask = RBM_NONE;
        regSet.rsLockReg(RBM_ARG_REGS, &usedMask);

        regNumber scratchReg = regSet.rsGrabReg(RBM_CALLEE_SAVED);
        regSet.rsLockReg(genRegMask(scratchReg));

        emitAttr attr = EA_UNKNOWN;
        if (RBM_R0 & gcInfo.gcRegGCrefSetCur)
        {
            attr = EA_GCREF;
            gcInfo.gcMarkRegSetGCref(scratchReg);
        }
        else if (RBM_R0 & gcInfo.gcRegByrefSetCur)
        {
            attr = EA_BYREF;
            gcInfo.gcMarkRegSetByref(scratchReg);
        }
        else
        {
            attr = EA_4BYTE;
        }

        getEmitter()->emitIns_R_R(INS_mov, attr, scratchReg, REG_R0);
        regTracker.rsTrackRegTrash(scratchReg);

        if (compiler->compProfilerMethHndIndirected)
        {
            getEmitter()->emitIns_R_AI(INS_ldr, EA_PTR_DSP_RELOC, REG_R0, (ssize_t)compiler->compProfilerMethHnd);
            regTracker.rsTrackRegTrash(REG_R0);
        }
        else
        {
            instGen_Set_Reg_To_Imm(EA_4BYTE, REG_R0, (ssize_t)compiler->compProfilerMethHnd);
        }

        genEmitHelperCall(CORINFO_HELP_PROF_FCN_TAILCALL,
                          0,           // argSize
                          EA_UNKNOWN); // retSize

        // Restore back to the state that existed before profiler callback
        gcInfo.gcMarkRegSetNpt(scratchReg);
        getEmitter()->emitIns_R_R(INS_mov, attr, REG_R0, scratchReg);
        regTracker.rsTrackRegTrash(REG_R0);
        regSet.rsUnlockReg(genRegMask(scratchReg));
        regSet.rsUnlockReg(RBM_ARG_REGS, usedMask);
#else
        NYI("Pushing the profilerHandle & caller's sp for the profiler callout and locking any registers");
#endif //_TARGET_X86_

        /* Restore the stack level */
        SetStackLevel(saveStackLvl2);
    }

#endif // PROFILING_SUPPORTED

#ifdef DEBUG
    /*-------------------------------------------------------------------------
     *  Generate an ESP check for the call
     */

    if (compiler->opts.compStackCheckOnCall
#if defined(USE_TRANSITION_THUNKS) || defined(USE_DYNAMIC_STACK_ALIGN)
        // check the stacks as frequently as possible
        && !call->IsHelperCall()
#else
        && call->gtCallType == CT_USER_FUNC
#endif
            )
    {
        noway_assert(compiler->lvaCallEspCheck != 0xCCCCCCCC &&
                     compiler->lvaTable[compiler->lvaCallEspCheck].lvDoNotEnregister &&
                     compiler->lvaTable[compiler->lvaCallEspCheck].lvOnFrame);
        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaCallEspCheck, 0);
    }
#endif

    /*-------------------------------------------------------------------------
     *  Generate the call
     */

    bool            fPossibleSyncHelperCall = false;
    CorInfoHelpFunc helperNum               = CORINFO_HELP_UNDEF; /* only initialized to avoid compiler C4701 warning */

    bool fTailCallTargetIsVSD = false;

    bool fTailCall = (call->gtCallMoreFlags & GTF_CALL_M_TAILCALL) != 0;

    /* Check for Delegate.Invoke. If so, we inline it. We get the
       target-object and target-function from the delegate-object, and do
       an indirect call.
     */

    if ((call->gtCallMoreFlags & GTF_CALL_M_DELEGATE_INV) && !fTailCall)
    {
        noway_assert(call->gtCallType == CT_USER_FUNC);

        assert((compiler->info.compCompHnd->getMethodAttribs(call->gtCallMethHnd) &
                (CORINFO_FLG_DELEGATE_INVOKE | CORINFO_FLG_FINAL)) ==
               (CORINFO_FLG_DELEGATE_INVOKE | CORINFO_FLG_FINAL));

        /* Find the offsets of the 'this' pointer and new target */

        CORINFO_EE_INFO* pInfo;
        unsigned         instOffs;     // offset of new 'this' pointer
        unsigned         firstTgtOffs; // offset of first target to invoke
        const regNumber  regThis = genGetThisArgReg(call);

        pInfo        = compiler->eeGetEEInfo();
        instOffs     = pInfo->offsetOfDelegateInstance;
        firstTgtOffs = pInfo->offsetOfDelegateFirstTarget;

#ifdef _TARGET_ARM_
        if ((call->gtCallMoreFlags & GTF_CALL_M_SECURE_DELEGATE_INV))
        {
            getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, compiler->virtualStubParamInfo->GetReg(), regThis,
                                        pInfo->offsetOfSecureDelegateIndirectCell);
            regTracker.rsTrackRegTrash(compiler->virtualStubParamInfo->GetReg());
        }
#endif // _TARGET_ARM_

        // Grab an available register to use for the CALL indirection
        regNumber indCallReg = regSet.rsGrabReg(RBM_ALLINT);

        //  Save the invoke-target-function in indCallReg
        //  'mov indCallReg, dword ptr [regThis + firstTgtOffs]'
        getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, indCallReg, regThis, firstTgtOffs);
        regTracker.rsTrackRegTrash(indCallReg);

        /* Set new 'this' in REG_CALL_THIS - 'mov REG_CALL_THIS, dword ptr [regThis + instOffs]' */

        getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_GCREF, regThis, regThis, instOffs);
        regTracker.rsTrackRegTrash(regThis);
        noway_assert(instOffs < 127);

        /* Call through indCallReg */

        getEmitter()->emitIns_Call(emitter::EC_INDIR_R,
                                   NULL,                                // methHnd
                                   INDEBUG_LDISASM_COMMA(sigInfo) NULL, // addr
                                   args, retSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                   gcInfo.gcRegByrefSetCur, ilOffset, indCallReg);
    }
    else

        /*-------------------------------------------------------------------------
         *  Virtual and interface calls
         */

        switch (call->gtFlags & GTF_CALL_VIRT_KIND_MASK)
        {
            case GTF_CALL_VIRT_STUB:
            {
                regSet.rsSetRegsModified(compiler->virtualStubParamInfo->GetRegMask());

                // An x86 JIT which uses full stub dispatch must generate only
                // the following stub dispatch calls:
                //
                // (1) isCallRelativeIndirect:
                //        call dword ptr [rel32]  ;  FF 15 ---rel32----
                // (2) isCallRelative:
                //        call abc                ;     E8 ---rel32----
                // (3) isCallRegisterIndirect:
                //     3-byte nop                 ;
                //     call dword ptr [eax]       ;     FF 10
                //
                // THIS IS VERY TIGHTLY TIED TO THE PREDICATES IN
                // vm\i386\cGenCpu.h, esp. isCallRegisterIndirect.

                //
                // Please do not insert any Random NOPs while constructing this VSD call
                //
                getEmitter()->emitDisableRandomNops();

                if (!fTailCall)
                {
                    // This is code to set up an indirect call to a stub address computed
                    // via dictionary lookup.  However the dispatch stub receivers aren't set up
                    // to accept such calls at the moment.
                    if (callType == CT_INDIRECT)
                    {
                        regNumber indReg;

                        // -------------------------------------------------------------------------
                        // The importer decided we needed a stub call via a computed
                        // stub dispatch address, i.e. an address which came from a dictionary lookup.
                        //   - The dictionary lookup produces an indirected address, suitable for call
                        //     via "call [virtualStubParamInfo.reg]"
                        //
                        // This combination will only be generated for shared generic code and when
                        // stub dispatch is active.

                        // No need to null check the this pointer - the dispatch code will deal with this.

                        noway_assert(genStillAddressable(call->gtCallAddr));

                        // Now put the address in virtualStubParamInfo.reg.
                        // This is typically a nop when the register used for
                        // the gtCallAddr is virtualStubParamInfo.reg
                        //
                        inst_RV_TT(INS_mov, compiler->virtualStubParamInfo->GetReg(), call->gtCallAddr);
                        regTracker.rsTrackRegTrash(compiler->virtualStubParamInfo->GetReg());

#if defined(_TARGET_X86_)
                        // Emit enough bytes of nops so that this sequence can be distinguished
                        // from other virtual stub dispatch calls.
                        //
                        // NOTE: THIS IS VERY TIGHTLY TIED TO THE PREDICATES IN
                        //        vm\i386\cGenCpu.h, esp. isCallRegisterIndirect.
                        //
                        getEmitter()->emitIns_Nop(3);

                        // Make the virtual stub call:
                        //     call   [virtualStubParamInfo.reg]
                        //
                        emitCallType = emitter::EC_INDIR_ARD;

                        indReg = compiler->virtualStubParamInfo->GetReg();
                        genDoneAddressable(call->gtCallAddr, fptrRegs, RegSet::KEEP_REG);

#elif CPU_LOAD_STORE_ARCH // ARM doesn't allow us to use an indirection for the call

                        genDoneAddressable(call->gtCallAddr, fptrRegs, RegSet::KEEP_REG);

                        // Make the virtual stub call:
                        //     ldr   indReg, [virtualStubParamInfo.reg]
                        //     call  indReg
                        //
                        emitCallType = emitter::EC_INDIR_R;

                        // Now dereference [virtualStubParamInfo.reg] and put it in a new temp register 'indReg'
                        //
                        indReg = regSet.rsGrabReg(RBM_ALLINT & ~compiler->virtualStubParamInfo->GetRegMask());
                        assert(call->gtCallAddr->InReg());
                        getEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, indReg,
                                                    compiler->virtualStubParamInfo->GetReg(), 0);
                        regTracker.rsTrackRegTrash(indReg);

#else
#error "Unknown target for VSD call"
#endif

                        getEmitter()->emitIns_Call(emitCallType,
                                                   NULL,                                // methHnd
                                                   INDEBUG_LDISASM_COMMA(sigInfo) NULL, // addr
                                                   args, retSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                   gcInfo.gcRegByrefSetCur, ilOffset, indReg);
                    }
                    else
                    {
                        // -------------------------------------------------------------------------
                        // Check for a direct stub call.
                        //

                        // Get stub addr. This will return NULL if virtual call stubs are not active
                        void* stubAddr = NULL;

                        stubAddr = (void*)call->gtStubCallStubAddr;

                        noway_assert(stubAddr != NULL);

                        // -------------------------------------------------------------------------
                        // Direct stub calls, though the stubAddr itself may still need to be
                        // accesed via an indirection.
                        //

                        // No need to null check - the dispatch code will deal with null this.

                        emitter::EmitCallType callTypeStubAddr = emitter::EC_FUNC_ADDR;
                        void*                 addr             = stubAddr;
                        int                   disp             = 0;
                        regNumber             callReg          = REG_NA;

                        if (call->gtCallMoreFlags & GTF_CALL_M_VIRTSTUB_REL_INDIRECT)
                        {
#if CPU_LOAD_STORE_ARCH
                            callReg = regSet.rsGrabReg(compiler->virtualStubParamInfo->GetRegMask());
                            noway_assert(callReg == compiler->virtualStubParamInfo->GetReg());

                            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, compiler->virtualStubParamInfo->GetReg(),
                                                   (ssize_t)stubAddr);
                            // The stub will write-back to this register, so don't track it
                            regTracker.rsTrackRegTrash(compiler->virtualStubParamInfo->GetReg());
                            getEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, REG_JUMP_THUNK_PARAM,
                                                        compiler->virtualStubParamInfo->GetReg(), 0);
                            regTracker.rsTrackRegTrash(REG_JUMP_THUNK_PARAM);
                            callTypeStubAddr = emitter::EC_INDIR_R;
                            getEmitter()->emitIns_Call(emitter::EC_INDIR_R,
                                                       NULL,                                // methHnd
                                                       INDEBUG_LDISASM_COMMA(sigInfo) NULL, // addr
                                                       args, retSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                       gcInfo.gcRegByrefSetCur, ilOffset, REG_JUMP_THUNK_PARAM);

#else
                            // emit an indirect call
                            callTypeStubAddr = emitter::EC_INDIR_C;
                            addr             = 0;
                            disp             = (ssize_t)stubAddr;
#endif
                        }
#if CPU_LOAD_STORE_ARCH
                        if (callTypeStubAddr != emitter::EC_INDIR_R)
#endif
                        {
                            getEmitter()->emitIns_Call(callTypeStubAddr, call->gtCallMethHnd,
                                                       INDEBUG_LDISASM_COMMA(sigInfo) addr, args, retSize,
                                                       gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                       gcInfo.gcRegByrefSetCur, ilOffset, callReg, REG_NA, 0, disp);
                        }
                    }
                }
                else // tailCall is true
                {

// Non-X86 tail calls materialize the null-check in fgMorphTailCall, when it
// moves the this pointer out of it's usual place and into the argument list.
#ifdef _TARGET_X86_

                    // Generate "cmp ECX, [ECX]" to trap null pointers
                    const regNumber regThis = genGetThisArgReg(call);
                    getEmitter()->emitIns_AR_R(INS_cmp, EA_4BYTE, regThis, regThis, 0);

#endif // _TARGET_X86_

                    if (callType == CT_INDIRECT)
                    {
                        noway_assert(genStillAddressable(call->gtCallAddr));

                        // Now put the address in EAX.
                        inst_RV_TT(INS_mov, REG_TAILCALL_ADDR, call->gtCallAddr);
                        regTracker.rsTrackRegTrash(REG_TAILCALL_ADDR);

                        genDoneAddressable(call->gtCallAddr, fptrRegs, RegSet::KEEP_REG);
                    }
                    else
                    {
                        // importer/EE should guarantee the indirection
                        noway_assert(call->gtCallMoreFlags & GTF_CALL_M_VIRTSTUB_REL_INDIRECT);

                        instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, REG_TAILCALL_ADDR,
                                               ssize_t(call->gtStubCallStubAddr));
                    }

                    fTailCallTargetIsVSD = true;
                }

                //
                // OK to start inserting random NOPs again
                //
                getEmitter()->emitEnableRandomNops();
            }
            break;

            case GTF_CALL_VIRT_VTABLE:
                // stub dispatching is off or this is not a virtual call (could be a tailcall)
                {
                    regNumber vptrReg;
                    regNumber vptrReg1;
                    regMaskTP vptrMask1;
                    unsigned  vtabOffsOfIndirection;
                    unsigned  vtabOffsAfterIndirection;
                    unsigned  isRelative;

                    noway_assert(callType == CT_USER_FUNC);

                    /* Get hold of the vtable offset (note: this might be expensive) */

                    compiler->info.compCompHnd->getMethodVTableOffset(call->gtCallMethHnd, &vtabOffsOfIndirection,
                                                                      &vtabOffsAfterIndirection, &isRelative);

                    vptrReg =
                        regSet.rsGrabReg(RBM_ALLINT); // Grab an available register to use for the CALL indirection
                    vptrMask = genRegMask(vptrReg);

                    if (isRelative)
                    {
                        vptrReg1  = regSet.rsGrabReg(RBM_ALLINT & ~vptrMask);
                        vptrMask1 = genRegMask(vptrReg1);
                    }

                    /* The register no longer holds a live pointer value */
                    gcInfo.gcMarkRegSetNpt(vptrMask);

                    if (isRelative)
                    {
                        gcInfo.gcMarkRegSetNpt(vptrMask1);
                    }

                    // MOV vptrReg, [REG_CALL_THIS + offs]
                    getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, vptrReg, genGetThisArgReg(call),
                                               VPTR_OFFS);
                    regTracker.rsTrackRegTrash(vptrReg);

                    if (isRelative)
                    {
                        regTracker.rsTrackRegTrash(vptrReg1);
                    }

                    noway_assert(vptrMask & ~call->gtCallRegUsedMask);

                    /* The register no longer holds a live pointer value */
                    gcInfo.gcMarkRegSetNpt(vptrMask);

                    /* Get the appropriate vtable chunk */

                    if (vtabOffsOfIndirection != CORINFO_VIRTUALCALL_NO_CHUNK)
                    {
                        if (isRelative)
                        {
#if defined(_TARGET_ARM_)
                            unsigned offset = vtabOffsOfIndirection + vtabOffsAfterIndirection;

                            // ADD vptrReg1, REG_CALL_IND_SCRATCH, vtabOffsOfIndirection + vtabOffsAfterIndirection
                            getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, vptrReg1, vptrReg, offset);
#else
                            _ASSERTE(false);
#endif
                        }

                        // MOV vptrReg, [REG_CALL_IND_SCRATCH + vtabOffsOfIndirection]
                        getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, vptrReg, vptrReg,
                                                   vtabOffsOfIndirection);
                    }
                    else
                    {
                        _ASSERTE(!isRelative);
                    }

                    /* Call through the appropriate vtable slot */

                    if (fTailCall)
                    {
                        if (isRelative)
                        {
#if defined(_TARGET_ARM_)
                            /* Load the function address: "[vptrReg1 + vptrReg] -> reg_intret" */
                            getEmitter()->emitIns_R_ARR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_TAILCALL_ADDR, vptrReg1,
                                                        vptrReg, 0);
#else
                            _ASSERTE(false);
#endif
                        }
                        else
                        {
                            /* Load the function address: "[vptrReg+vtabOffs] -> reg_intret" */
                            getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_TAILCALL_ADDR, vptrReg,
                                                       vtabOffsAfterIndirection);
                        }
                    }
                    else
                    {
#if CPU_LOAD_STORE_ARCH
                        if (isRelative)
                        {
                            getEmitter()->emitIns_R_ARR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, vptrReg, vptrReg1, vptrReg,
                                                        0);
                        }
                        else
                        {
                            getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, vptrReg, vptrReg,
                                                       vtabOffsAfterIndirection);
                        }

                        getEmitter()->emitIns_Call(emitter::EC_INDIR_R, call->gtCallMethHnd,
                                                   INDEBUG_LDISASM_COMMA(sigInfo) NULL, // addr
                                                   args, retSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                   gcInfo.gcRegByrefSetCur, ilOffset,
                                                   vptrReg); // ireg
#else
                        _ASSERTE(!isRelative);
                        getEmitter()->emitIns_Call(emitter::EC_FUNC_VIRTUAL, call->gtCallMethHnd,
                                                   INDEBUG_LDISASM_COMMA(sigInfo) NULL, // addr
                                                   args, retSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                   gcInfo.gcRegByrefSetCur, ilOffset,
                                                   vptrReg,                   // ireg
                                                   REG_NA,                    // xreg
                                                   0,                         // xmul
                                                   vtabOffsAfterIndirection); // disp
#endif // CPU_LOAD_STORE_ARCH
                    }
                }
                break;

            case GTF_CALL_NONVIRT:
            {
                //------------------------ Non-virtual/Indirect calls -------------------------
                // Lots of cases follow
                //    - Direct P/Invoke calls
                //    - Indirect calls to P/Invoke functions via the P/Invoke stub
                //    - Direct Helper calls
                //    - Indirect Helper calls
                //    - Direct calls to known addresses
                //    - Direct calls where address is accessed by one or two indirections
                //    - Indirect calls to computed addresses
                //    - Tailcall versions of all of the above

                CORINFO_METHOD_HANDLE methHnd = call->gtCallMethHnd;

                //------------------------------------------------------
                // Non-virtual/Indirect calls: Insert a null check on the "this" pointer if needed
                //
                // For (final and private) functions which were called with
                //  invokevirtual, but which we call directly, we need to
                //  dereference the object pointer to make sure it's not NULL.
                //

                if (call->gtFlags & GTF_CALL_NULLCHECK)
                {
                    /* Generate "cmp ECX, [ECX]" to trap null pointers */
                    const regNumber regThis = genGetThisArgReg(call);
#if CPU_LOAD_STORE_ARCH
                    regNumber indReg =
                        regSet.rsGrabReg(RBM_ALLINT); // Grab an available register to use for the indirection
                    getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, indReg, regThis, 0);
                    regTracker.rsTrackRegTrash(indReg);
#else
                    getEmitter()->emitIns_AR_R(INS_cmp, EA_4BYTE, regThis, regThis, 0);
#endif
                }

                if (call->gtFlags & GTF_CALL_UNMANAGED)
                {
                    //------------------------------------------------------
                    // Non-virtual/Indirect calls: PInvoke calls.

                    noway_assert(compiler->info.compCallUnmanaged != 0);

                    /* args shouldn't be greater than 64K */

                    noway_assert((argSize & 0xffff0000) == 0);

                    /* Remember the varDsc for the callsite-epilog */

                    frameListRoot = &compiler->lvaTable[compiler->info.compLvFrameListRoot];

                    // exact codegen is required
                    getEmitter()->emitDisableRandomNops();

                    int nArgSize = 0;

                    regNumber indCallReg = REG_NA;

                    if (callType == CT_INDIRECT)
                    {
                        noway_assert(genStillAddressable(call->gtCallAddr));

                        if (call->gtCallAddr->InReg())
                            indCallReg = call->gtCallAddr->gtRegNum;

                        nArgSize = (call->gtFlags & GTF_CALL_POP_ARGS) ? 0 : (int)argSize;
                        methHnd  = 0;
                    }
                    else
                    {
                        noway_assert(callType == CT_USER_FUNC);
                    }

                    regNumber tcbReg = REG_NA;

                    if (!compiler->opts.ShouldUsePInvokeHelpers())
                    {
                        tcbReg = genPInvokeCallProlog(frameListRoot, nArgSize, methHnd, returnLabel);
                    }

                    void* addr = NULL;

                    if (callType == CT_INDIRECT)
                    {
                        /* Double check that the callee didn't use/trash the
                           registers holding the call target.
                        */
                        noway_assert(tcbReg != indCallReg);

                        if (indCallReg == REG_NA)
                        {
                            indCallReg = regSet.rsGrabReg(RBM_ALLINT); // Grab an available register to use for the CALL
                                                                       // indirection

                            /* Please note that this even works with tcbReg == REG_EAX.
                            tcbReg contains an interesting value only if frameListRoot is
                            an enregistered local that stays alive across the call
                            (certainly not EAX). If frameListRoot has been moved into
                            EAX, we can trash it since it won't survive across the call
                            anyways.
                            */

                            inst_RV_TT(INS_mov, indCallReg, call->gtCallAddr);
                            regTracker.rsTrackRegTrash(indCallReg);
                        }

                        emitCallType = emitter::EC_INDIR_R;
                    }
                    else
                    {
                        noway_assert(callType == CT_USER_FUNC);

                        void* pAddr;
                        addr = compiler->info.compCompHnd->getAddressOfPInvokeFixup(methHnd, (void**)&pAddr);
                        if (addr != NULL)
                        {
#if CPU_LOAD_STORE_ARCH
                            // Load the address into a register, indirect it and call  through a register
                            indCallReg = regSet.rsGrabReg(RBM_ALLINT); // Grab an available register to use for the CALL
                                                                       // indirection
                            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indCallReg, (ssize_t)addr);
                            getEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, indCallReg, indCallReg, 0);
                            regTracker.rsTrackRegTrash(indCallReg);
                            // Now make the call "call indCallReg"

                            getEmitter()->emitIns_Call(emitter::EC_INDIR_R,
                                                       methHnd,                       // methHnd
                                                       INDEBUG_LDISASM_COMMA(sigInfo) // sigInfo
                                                       NULL,                          // addr
                                                       args,
                                                       retSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                       gcInfo.gcRegByrefSetCur, ilOffset, indCallReg);

                            emitCallType = emitter::EC_INDIR_R;
                            break;
#else
                            emitCallType = emitter::EC_FUNC_TOKEN_INDIR;
                            indCallReg   = REG_NA;
#endif
                        }
                        else
                        {
                            // Double-indirection. Load the address into a register
                            // and call indirectly through a register
                            indCallReg = regSet.rsGrabReg(RBM_ALLINT); // Grab an available register to use for the CALL
                                                                       // indirection

#if CPU_LOAD_STORE_ARCH
                            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indCallReg, (ssize_t)pAddr);
                            getEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, indCallReg, indCallReg, 0);
                            getEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, indCallReg, indCallReg, 0);
                            regTracker.rsTrackRegTrash(indCallReg);

                            emitCallType = emitter::EC_INDIR_R;

#else
                            getEmitter()->emitIns_R_AI(INS_mov, EA_PTR_DSP_RELOC, indCallReg, (ssize_t)pAddr);
                            regTracker.rsTrackRegTrash(indCallReg);
                            emitCallType = emitter::EC_INDIR_ARD;

#endif // CPU_LOAD_STORE_ARCH
                        }
                    }

                    getEmitter()->emitIns_Call(emitCallType, compiler->eeMarkNativeTarget(methHnd),
                                               INDEBUG_LDISASM_COMMA(sigInfo) addr, args, retSize,
                                               gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur,
                                               ilOffset, indCallReg);

                    if (callType == CT_INDIRECT)
                        genDoneAddressable(call->gtCallAddr, fptrRegs, RegSet::KEEP_REG);

                    getEmitter()->emitEnableRandomNops();

                    // Done with PInvoke calls
                    break;
                }

                if (callType == CT_INDIRECT)
                {
                    noway_assert(genStillAddressable(call->gtCallAddr));

                    if (call->gtCallCookie)
                    {
                        //------------------------------------------------------
                        // Non-virtual indirect calls via the P/Invoke stub

                        GenTreePtr cookie = call->gtCallCookie;
                        GenTreePtr target = call->gtCallAddr;

                        noway_assert((call->gtFlags & GTF_CALL_POP_ARGS) == 0);

                        noway_assert(cookie->gtOper == GT_CNS_INT ||
                                     cookie->gtOper == GT_IND && cookie->gtOp.gtOp1->gtOper == GT_CNS_INT);

                        noway_assert(args == argSize);

#if defined(_TARGET_X86_)
                        /* load eax with the real target */

                        inst_RV_TT(INS_mov, REG_EAX, target);
                        regTracker.rsTrackRegTrash(REG_EAX);

                        if (cookie->gtOper == GT_CNS_INT)
                            inst_IV_handle(INS_push, cookie->gtIntCon.gtIconVal);
                        else
                            inst_TT(INS_push, cookie);

                        /* Keep track of ESP for EBP-less frames */
                        genSinglePush();

                        argSize += sizeof(void*);

#elif defined(_TARGET_ARM_)

                        // Ensure that we spill these registers (if caller saved) in the prolog
                        regSet.rsSetRegsModified(RBM_PINVOKE_COOKIE_PARAM | RBM_PINVOKE_TARGET_PARAM);

                        // ARM: load r12 with the real target
                        // X64: load r10 with the real target
                        inst_RV_TT(INS_mov, REG_PINVOKE_TARGET_PARAM, target);
                        regTracker.rsTrackRegTrash(REG_PINVOKE_TARGET_PARAM);

                        // ARM: load r4  with the pinvoke VASigCookie
                        // X64: load r11 with the pinvoke VASigCookie
                        if (cookie->gtOper == GT_CNS_INT)
                            inst_RV_IV(INS_mov, REG_PINVOKE_COOKIE_PARAM, cookie->gtIntCon.gtIconVal,
                                       EA_HANDLE_CNS_RELOC);
                        else
                            inst_RV_TT(INS_mov, REG_PINVOKE_COOKIE_PARAM, cookie);
                        regTracker.rsTrackRegTrash(REG_PINVOKE_COOKIE_PARAM);

                        noway_assert(args == argSize);

                        // Ensure that we don't trash any of these registers if we have to load
                        // the helper call target into a register to invoke it.
                        regMaskTP regsUsed;
                        regSet.rsLockReg(call->gtCallRegUsedMask | RBM_PINVOKE_TARGET_PARAM | RBM_PINVOKE_COOKIE_PARAM,
                                         &regsUsed);
#else
                        NYI("Non-virtual indirect calls via the P/Invoke stub");
#endif

                        args = argSize;
                        noway_assert((size_t)(int)args == args);

                        genEmitHelperCall(CORINFO_HELP_PINVOKE_CALLI, (int)args, retSize);

#if defined(_TARGET_ARM_)
                        regSet.rsUnlockReg(call->gtCallRegUsedMask | RBM_PINVOKE_TARGET_PARAM |
                                               RBM_PINVOKE_COOKIE_PARAM,
                                           regsUsed);
#endif

#ifdef _TARGET_ARM_
                        // genEmitHelperCall doesn't record all registers a helper call would trash.
                        regTracker.rsTrackRegTrash(REG_PINVOKE_COOKIE_PARAM);
#endif
                    }
                    else
                    {
                        //------------------------------------------------------
                        // Non-virtual indirect calls

                        if (fTailCall)
                        {
                            inst_RV_TT(INS_mov, REG_TAILCALL_ADDR, call->gtCallAddr);
                            regTracker.rsTrackRegTrash(REG_TAILCALL_ADDR);
                        }
                        else
                            instEmit_indCall(call, args, retSize);
                    }

                    genDoneAddressable(call->gtCallAddr, fptrRegs, RegSet::KEEP_REG);

                    // Done with indirect calls
                    break;
                }

                //------------------------------------------------------
                // Non-virtual direct/indirect calls: Work out if the address of the
                // call is known at JIT time (if not it is either an indirect call
                // or the address must be accessed via an single/double indirection)

                noway_assert(callType == CT_USER_FUNC || callType == CT_HELPER);

                void*          addr;
                InfoAccessType accessType;

                helperNum = compiler->eeGetHelperNum(methHnd);

                if (callType == CT_HELPER)
                {
                    noway_assert(helperNum != CORINFO_HELP_UNDEF);

#ifdef FEATURE_READYTORUN_COMPILER
                    if (call->gtEntryPoint.addr != NULL)
                    {
                        accessType = call->gtEntryPoint.accessType;
                        addr       = call->gtEntryPoint.addr;
                    }
                    else
#endif // FEATURE_READYTORUN_COMPILER
                    {
                        void* pAddr;

                        accessType = IAT_VALUE;
                        addr       = compiler->compGetHelperFtn(helperNum, (void**)&pAddr);

                        if (!addr)
                        {
                            accessType = IAT_PVALUE;
                            addr       = pAddr;
                        }
                    }
                }
                else
                {
                    noway_assert(helperNum == CORINFO_HELP_UNDEF);

                    CORINFO_ACCESS_FLAGS aflags = CORINFO_ACCESS_ANY;

                    if (call->gtCallMoreFlags & GTF_CALL_M_NONVIRT_SAME_THIS)
                        aflags = (CORINFO_ACCESS_FLAGS)(aflags | CORINFO_ACCESS_THIS);

                    if ((call->gtFlags & GTF_CALL_NULLCHECK) == 0)
                        aflags = (CORINFO_ACCESS_FLAGS)(aflags | CORINFO_ACCESS_NONNULL);

#ifdef FEATURE_READYTORUN_COMPILER
                    if (call->gtEntryPoint.addr != NULL)
                    {
                        accessType = call->gtEntryPoint.accessType;
                        addr       = call->gtEntryPoint.addr;
                    }
                    else
#endif // FEATURE_READYTORUN_COMPILER
                    {
                        CORINFO_CONST_LOOKUP addrInfo;
                        compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &addrInfo, aflags);

                        accessType = addrInfo.accessType;
                        addr       = addrInfo.addr;
                    }
                }

                if (fTailCall)
                {
                    noway_assert(callType == CT_USER_FUNC);

                    switch (accessType)
                    {
                        case IAT_VALUE:
                            //------------------------------------------------------
                            // Non-virtual direct calls to known addressess
                            //
                            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, REG_TAILCALL_ADDR, (ssize_t)addr);
                            break;

                        case IAT_PVALUE:
                            //------------------------------------------------------
                            // Non-virtual direct calls to addresses accessed by
                            // a single indirection.
                            //
                            // For tailcalls we place the target address in REG_TAILCALL_ADDR
                            CLANG_FORMAT_COMMENT_ANCHOR;

#if CPU_LOAD_STORE_ARCH
                            {
                                regNumber indReg = REG_TAILCALL_ADDR;
                                instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indReg, (ssize_t)addr);
                                getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, indReg, indReg, 0);
                                regTracker.rsTrackRegTrash(indReg);
                            }
#else
                            getEmitter()->emitIns_R_AI(INS_mov, EA_PTR_DSP_RELOC, REG_TAILCALL_ADDR, (ssize_t)addr);
                            regTracker.rsTrackRegTrash(REG_TAILCALL_ADDR);
#endif
                            break;

                        case IAT_PPVALUE:
                            //------------------------------------------------------
                            // Non-virtual direct calls to addresses accessed by
                            // a double indirection.
                            //
                            // For tailcalls we place the target address in REG_TAILCALL_ADDR
                            CLANG_FORMAT_COMMENT_ANCHOR;

#if CPU_LOAD_STORE_ARCH
                            {
                                regNumber indReg = REG_TAILCALL_ADDR;
                                instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indReg, (ssize_t)addr);
                                getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, indReg, indReg, 0);
                                getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, indReg, indReg, 0);
                                regTracker.rsTrackRegTrash(indReg);
                            }
#else
                            getEmitter()->emitIns_R_AI(INS_mov, EA_PTR_DSP_RELOC, REG_TAILCALL_ADDR, (ssize_t)addr);
                            getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_TAILCALL_ADDR,
                                                       REG_TAILCALL_ADDR, 0);
                            regTracker.rsTrackRegTrash(REG_TAILCALL_ADDR);
#endif
                            break;

                        default:
                            noway_assert(!"Bad accessType");
                            break;
                    }
                }
                else
                {
                    switch (accessType)
                    {
                        regNumber indCallReg;

                        case IAT_VALUE:
                        {
                            //------------------------------------------------------
                            // Non-virtual direct calls to known addressess
                            //
                            // The vast majority of calls end up here....  Wouldn't
                            // it be nice if they all did!
                            CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef _TARGET_ARM_
                            // We may use direct call for some of recursive calls
                            // as we can safely estimate the distance from the call site to the top of the method
                            const int codeOffset = MAX_PROLOG_SIZE_BYTES +           // prolog size
                                                   getEmitter()->emitCurCodeOffset + // offset of the current IG
                                                   getEmitter()->emitCurIGsize +     // size of the current IG
                                                   4;                                // size of the jump instruction
                                                                                     // that we are now emitting
                            if (compiler->gtIsRecursiveCall(call) && codeOffset <= -CALL_DIST_MAX_NEG)
                            {
                                getEmitter()->emitIns_Call(emitter::EC_FUNC_TOKEN, methHnd,
                                                           INDEBUG_LDISASM_COMMA(sigInfo) NULL, // addr
                                                           args, retSize, gcInfo.gcVarPtrSetCur,
                                                           gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur, ilOffset,
                                                           REG_NA, REG_NA, 0, 0, // ireg, xreg, xmul, disp
                                                           false,                // isJump
                                                           emitter::emitNoGChelper(helperNum));
                            }
                            else if (!arm_Valid_Imm_For_BL((ssize_t)addr))
                            {
                                // Load the address into a register and call  through a register
                                indCallReg = regSet.rsGrabReg(RBM_ALLINT); // Grab an available register to use for the
                                                                           // CALL indirection
                                instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indCallReg, (ssize_t)addr);

                                getEmitter()->emitIns_Call(emitter::EC_INDIR_R, methHnd,
                                                           INDEBUG_LDISASM_COMMA(sigInfo) NULL, // addr
                                                           args, retSize, gcInfo.gcVarPtrSetCur,
                                                           gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur, ilOffset,
                                                           indCallReg,   // ireg
                                                           REG_NA, 0, 0, // xreg, xmul, disp
                                                           false,        // isJump
                                                           emitter::emitNoGChelper(helperNum));
                            }
                            else
#endif
                            {
                                getEmitter()->emitIns_Call(emitter::EC_FUNC_TOKEN, methHnd,
                                                           INDEBUG_LDISASM_COMMA(sigInfo) addr, args, retSize,
                                                           gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                           gcInfo.gcRegByrefSetCur, ilOffset, REG_NA, REG_NA, 0,
                                                           0,     /* ireg, xreg, xmul, disp */
                                                           false, /* isJump */
                                                           emitter::emitNoGChelper(helperNum));
                            }
                        }
                        break;

                        case IAT_PVALUE:
                            //------------------------------------------------------
                            // Non-virtual direct calls to addresses accessed by
                            // a single indirection.
                            //

                            // Load the address into a register, load indirect and call  through a register
                            CLANG_FORMAT_COMMENT_ANCHOR;
#if CPU_LOAD_STORE_ARCH
                            indCallReg = regSet.rsGrabReg(RBM_ALLINT); // Grab an available register to use for the CALL
                                                                       // indirection

                            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indCallReg, (ssize_t)addr);
                            getEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, indCallReg, indCallReg, 0);
                            regTracker.rsTrackRegTrash(indCallReg);

                            emitCallType = emitter::EC_INDIR_R;
                            addr         = NULL;

#else
                            emitCallType = emitter::EC_FUNC_TOKEN_INDIR;
                            indCallReg   = REG_NA;

#endif // CPU_LOAD_STORE_ARCH

                            getEmitter()->emitIns_Call(emitCallType, methHnd, INDEBUG_LDISASM_COMMA(sigInfo) addr, args,
                                                       retSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                       gcInfo.gcRegByrefSetCur, ilOffset,
                                                       indCallReg,   // ireg
                                                       REG_NA, 0, 0, // xreg, xmul, disp
                                                       false,        /* isJump */
                                                       emitter::emitNoGChelper(helperNum));
                            break;

                        case IAT_PPVALUE:
                        {
                            //------------------------------------------------------
                            // Non-virtual direct calls to addresses accessed by
                            // a double indirection.
                            //
                            // Double-indirection. Load the address into a register
                            // and call indirectly through the register

                            noway_assert(helperNum == CORINFO_HELP_UNDEF);

                            // Grab an available register to use for the CALL indirection
                            indCallReg = regSet.rsGrabReg(RBM_ALLINT);

#if CPU_LOAD_STORE_ARCH
                            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indCallReg, (ssize_t)addr);
                            getEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, indCallReg, indCallReg, 0);
                            getEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, indCallReg, indCallReg, 0);
                            regTracker.rsTrackRegTrash(indCallReg);

                            emitCallType = emitter::EC_INDIR_R;

#else

                            getEmitter()->emitIns_R_AI(INS_mov, EA_PTR_DSP_RELOC, indCallReg, (ssize_t)addr);
                            regTracker.rsTrackRegTrash(indCallReg);

                            emitCallType = emitter::EC_INDIR_ARD;

#endif // CPU_LOAD_STORE_ARCH

                            getEmitter()->emitIns_Call(emitCallType, methHnd,
                                                       INDEBUG_LDISASM_COMMA(sigInfo) NULL, // addr
                                                       args, retSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                       gcInfo.gcRegByrefSetCur, ilOffset,
                                                       indCallReg,   // ireg
                                                       REG_NA, 0, 0, // xreg, xmul, disp
                                                       false,        // isJump
                                                       emitter::emitNoGChelper(helperNum));
                        }
                        break;

                        default:
                            noway_assert(!"Bad accessType");
                            break;
                    }

                    // tracking of region protected by the monitor in synchronized methods
                    if ((helperNum != CORINFO_HELP_UNDEF) && (compiler->info.compFlags & CORINFO_FLG_SYNCH))
                    {
                        fPossibleSyncHelperCall = true;
                    }
                }
            }
            break;

            default:
                noway_assert(!"strange call type");
                break;
        }

    /*-------------------------------------------------------------------------
     *  For tailcalls, REG_INTRET contains the address of the target function,
     *  enregistered args are in the correct registers, and the stack arguments
     *  have been pushed on the stack. Now call the stub-sliding helper
     */

    if (fTailCall)
    {

        if (compiler->info.compCallUnmanaged)
            genPInvokeMethodEpilog();

#ifdef _TARGET_X86_
        noway_assert(0 <= (ssize_t)args); // caller-pop args not supported for tailcall

        // Push the count of the incoming stack arguments

        unsigned nOldStkArgs =
            (unsigned)((compiler->compArgSize - (intRegState.rsCalleeRegArgCount * sizeof(void*))) / sizeof(void*));
        getEmitter()->emitIns_I(INS_push, EA_4BYTE, nOldStkArgs);
        genSinglePush(); // Keep track of ESP for EBP-less frames
        args += sizeof(void*);

        // Push the count of the outgoing stack arguments

        getEmitter()->emitIns_I(INS_push, EA_4BYTE, argSize / sizeof(void*));
        genSinglePush(); // Keep track of ESP for EBP-less frames
        args += sizeof(void*);

        // Push info about the callee-saved registers to be restored
        // For now, we always spill all registers if compiler->compTailCallUsed

        DWORD calleeSavedRegInfo = 1 |                                 // always restore EDI,ESI,EBX
                                   (fTailCallTargetIsVSD ? 0x2 : 0x0); // Stub dispatch flag
        getEmitter()->emitIns_I(INS_push, EA_4BYTE, calleeSavedRegInfo);
        genSinglePush(); // Keep track of ESP for EBP-less frames
        args += sizeof(void*);

        // Push the address of the target function

        getEmitter()->emitIns_R(INS_push, EA_4BYTE, REG_TAILCALL_ADDR);
        genSinglePush(); // Keep track of ESP for EBP-less frames
        args += sizeof(void*);

#else // _TARGET_X86_

        args    = 0;
        retSize = EA_UNKNOWN;

#endif // _TARGET_X86_

        if (compiler->getNeedsGSSecurityCookie())
        {
            genEmitGSCookieCheck(true);
        }

        // TailCall helper does not poll for GC. An explicit GC poll
        // Should have been placed in when we morphed this into a tail call.
        noway_assert(compiler->compCurBB->bbFlags & BBF_GC_SAFE_POINT);

        // Now call the helper

        genEmitHelperCall(CORINFO_HELP_TAILCALL, (int)args, retSize);
    }

    /*-------------------------------------------------------------------------
     *  Done with call.
     *  Trash registers, pop arguments if needed, etc
     */

    /* Mark the argument registers as free */

    noway_assert(intRegState.rsCurRegArgNum <= MAX_REG_ARG);

    for (areg = 0; areg < MAX_REG_ARG; areg++)
    {
        regMaskTP curArgMask = genMapArgNumToRegMask(areg, TYP_INT);

        // Is this one of the used argument registers?
        if ((curArgMask & call->gtCallRegUsedMask) == 0)
            continue;

#ifdef _TARGET_ARM_
        if (regSet.rsUsedTree[areg] == NULL)
        {
            noway_assert(areg % 2 == 1 &&
                         (((areg + 1) >= MAX_REG_ARG) || (regSet.rsUsedTree[areg + 1]->TypeGet() == TYP_STRUCT) ||
                          (genTypeStSz(regSet.rsUsedTree[areg + 1]->TypeGet()) == 2)));
            continue;
        }
#endif

        regSet.rsMarkRegFree(curArgMask);

        // We keep regSet.rsMaskVars current during codegen, so we have to remove any
        // that have been copied into arg regs.

        regSet.RemoveMaskVars(curArgMask);
        gcInfo.gcRegGCrefSetCur &= ~(curArgMask);
        gcInfo.gcRegByrefSetCur &= ~(curArgMask);
    }

#if !FEATURE_STACK_FP_X87
    //-------------------------------------------------------------------------
    // free up the FP args

    for (areg = 0; areg < MAX_FLOAT_REG_ARG; areg++)
    {
        regNumber argRegNum  = genMapRegArgNumToRegNum(areg, TYP_FLOAT);
        regMaskTP curArgMask = genMapArgNumToRegMask(areg, TYP_FLOAT);

        // Is this one of the used argument registers?
        if ((curArgMask & call->gtCallRegUsedMask) == 0)
            continue;

        regSet.rsMaskUsed &= ~curArgMask;
        regSet.rsUsedTree[argRegNum] = NULL;
    }
#endif // !FEATURE_STACK_FP_X87

    /* restore the old argument register status */

    intRegState.rsCurRegArgNum   = savCurIntArgReg;
    floatRegState.rsCurRegArgNum = savCurFloatArgReg;

    noway_assert(intRegState.rsCurRegArgNum <= MAX_REG_ARG);

    /* Mark all trashed registers as such */

    if (calleeTrashedRegs)
        regTracker.rsTrashRegSet(calleeTrashedRegs);

    regTracker.rsTrashRegsForGCInterruptability();

#ifdef DEBUG

    if (!(call->gtFlags & GTF_CALL_POP_ARGS))
    {
        if (compiler->verbose)
        {
            printf("\t\t\t\t\t\t\tEnd call ");
            Compiler::printTreeID(call);
            printf(" stack %02u [E=%02u] argSize=%u\n", saveStackLvl, getEmitter()->emitCurStackLvl, argSize);
        }
        noway_assert(stackLvl == getEmitter()->emitCurStackLvl);
    }

#endif

#if FEATURE_STACK_FP_X87
    /* All float temps must be spilled around function calls */
    if (call->gtType == TYP_FLOAT || call->gtType == TYP_DOUBLE)
    {
        noway_assert(compCurFPState.m_uStackSize == 1);
    }
    else
    {
        noway_assert(compCurFPState.m_uStackSize == 0);
    }
#else
    if (call->gtType == TYP_FLOAT || call->gtType == TYP_DOUBLE)
    {
#ifdef _TARGET_ARM_
        if (call->IsVarargs() || compiler->opts.compUseSoftFP)
        {
            // Result return for vararg methods is in r0, r1, but our callers would
            // expect the return in s0, s1 because of floating type. Do the move now.
            if (call->gtType == TYP_FLOAT)
            {
                inst_RV_RV(INS_vmov_i2f, REG_FLOATRET, REG_INTRET, TYP_FLOAT, EA_4BYTE);
            }
            else
            {
                inst_RV_RV_RV(INS_vmov_i2d, REG_FLOATRET, REG_INTRET, REG_NEXT(REG_INTRET), EA_8BYTE);
            }
        }
#endif
        genMarkTreeInReg(call, REG_FLOATRET);
    }
#endif

    /* The function will pop all arguments before returning */

    SetStackLevel(saveStackLvl);

    /* No trashed registers may possibly hold a pointer at this point */
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG

    regMaskTP ptrRegs = (gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur) & (calleeTrashedRegs & RBM_ALLINT) &
                        ~regSet.rsMaskVars & ~vptrMask;
    if (ptrRegs)
    {
        // A reg may be dead already.  The assertion is too strong.
        LclVarDsc* varDsc;
        unsigned   varNum;

        // use compiler->compCurLife
        for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount && ptrRegs != 0; varNum++, varDsc++)
        {
            /* Ignore the variable if it's not tracked, not in a register, or a floating-point type */

            if (!varDsc->lvTracked)
                continue;
            if (!varDsc->lvRegister)
                continue;
            if (varDsc->IsFloatRegType())
                continue;

            /* Get hold of the index and the bitmask for the variable */

            unsigned varIndex = varDsc->lvVarIndex;

            /* Is this variable live currently? */

            if (!VarSetOps::IsMember(compiler, compiler->compCurLife, varIndex))
            {
                regNumber regNum  = varDsc->lvRegNum;
                regMaskTP regMask = genRegMask(regNum);

                if (varDsc->lvType == TYP_REF || varDsc->lvType == TYP_BYREF)
                    ptrRegs &= ~regMask;
            }
        }
        if (ptrRegs)
        {
            printf("Bad call handling for ");
            Compiler::printTreeID(call);
            printf("\n");
            noway_assert(!"A callee trashed reg is holding a GC pointer");
        }
    }
#endif

#if defined(_TARGET_X86_)
    //-------------------------------------------------------------------------
    // Create a label for tracking of region protected by the monitor in synchronized methods.
    // This needs to be here, rather than above where fPossibleSyncHelperCall is set,
    // so the GC state vars have been updated before creating the label.

    if (fPossibleSyncHelperCall)
    {
        switch (helperNum)
        {
            case CORINFO_HELP_MON_ENTER:
            case CORINFO_HELP_MON_ENTER_STATIC:
                noway_assert(compiler->syncStartEmitCookie == NULL);
                compiler->syncStartEmitCookie =
                    getEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur);
                noway_assert(compiler->syncStartEmitCookie != NULL);
                break;
            case CORINFO_HELP_MON_EXIT:
            case CORINFO_HELP_MON_EXIT_STATIC:
                noway_assert(compiler->syncEndEmitCookie == NULL);
                compiler->syncEndEmitCookie =
                    getEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur);
                noway_assert(compiler->syncEndEmitCookie != NULL);
                break;
            default:
                break;
        }
    }
#endif // _TARGET_X86_

    if (call->gtFlags & GTF_CALL_UNMANAGED)
    {
        genDefineTempLabel(returnLabel);

#ifdef _TARGET_X86_
        if (getInlinePInvokeCheckEnabled())
        {
            noway_assert(compiler->lvaInlinedPInvokeFrameVar != BAD_VAR_NUM);
            BasicBlock* esp_check;

            CORINFO_EE_INFO* pInfo = compiler->eeGetEEInfo();
            /* mov   ecx, dword ptr [frame.callSiteTracker] */

            getEmitter()->emitIns_R_S(INS_mov, EA_4BYTE, REG_ARG_0, compiler->lvaInlinedPInvokeFrameVar,
                                      pInfo->inlinedCallFrameInfo.offsetOfCallSiteSP);
            regTracker.rsTrackRegTrash(REG_ARG_0);

            /* Generate the conditional jump */

            if (!(call->gtFlags & GTF_CALL_POP_ARGS))
            {
                if (argSize)
                {
                    getEmitter()->emitIns_R_I(INS_add, EA_PTRSIZE, REG_ARG_0, argSize);
                }
            }
            /* cmp   ecx, esp */

            getEmitter()->emitIns_R_R(INS_cmp, EA_PTRSIZE, REG_ARG_0, REG_SPBASE);

            esp_check = genCreateTempLabel();

            emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
            inst_JMP(jmpEqual, esp_check);

            getEmitter()->emitIns(INS_BREAKPOINT);

            /* genCondJump() closes the current emitter block */

            genDefineTempLabel(esp_check);
        }
#endif
    }

    /* Are we supposed to pop the arguments? */
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_X86_)
    if (call->gtFlags & GTF_CALL_UNMANAGED)
    {
        if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PINVOKE_RESTORE_ESP) ||
            compiler->compStressCompile(Compiler::STRESS_PINVOKE_RESTORE_ESP, 50))
        {
            // P/Invoke signature mismatch resilience - restore ESP to pre-call value. We would ideally
            // take care of the cdecl argument popping here as well but the stack depth tracking logic
            // makes this very hard, i.e. it needs to "see" the actual pop.

            CORINFO_EE_INFO* pInfo = compiler->eeGetEEInfo();

            if (argSize == 0 || (call->gtFlags & GTF_CALL_POP_ARGS))
            {
                /* mov   esp, dword ptr [frame.callSiteTracker] */
                getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE,
                                          compiler->lvaInlinedPInvokeFrameVar,
                                          pInfo->inlinedCallFrameInfo.offsetOfCallSiteSP);
            }
            else
            {
                /* mov   ecx, dword ptr [frame.callSiteTracker] */
                getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_ARG_0,
                                          compiler->lvaInlinedPInvokeFrameVar,
                                          pInfo->inlinedCallFrameInfo.offsetOfCallSiteSP);
                regTracker.rsTrackRegTrash(REG_ARG_0);

                /* lea   esp, [ecx + argSize] */
                getEmitter()->emitIns_R_AR(INS_lea, EA_PTRSIZE, REG_SPBASE, REG_ARG_0, (int)argSize);
            }
        }
    }
#endif // _TARGET_X86_

    if (call->gtFlags & GTF_CALL_POP_ARGS)
    {
        noway_assert(args == (size_t) - (int)argSize);

        if (argSize)
        {
            genAdjustSP(argSize);
        }
    }

    if (pseudoStackLvl)
    {
        noway_assert(call->gtType == TYP_VOID);

        /* Generate NOP */

        instGen(INS_nop);
    }

    /* What does the function return? */

    retVal = RBM_NONE;

    switch (call->gtType)
    {
        case TYP_REF:
        case TYP_ARRAY:
        case TYP_BYREF:
            gcInfo.gcMarkRegPtrVal(REG_INTRET, call->TypeGet());

            __fallthrough;

        case TYP_INT:
#if !CPU_HAS_FP_SUPPORT
        case TYP_FLOAT:
#endif
            retVal = RBM_INTRET;
            break;

#ifdef _TARGET_ARM_
        case TYP_STRUCT:
        {
            assert(call->gtRetClsHnd != NULL);
            assert(compiler->IsHfa(call->gtRetClsHnd));
            int retSlots = compiler->GetHfaCount(call->gtRetClsHnd);
            assert(retSlots > 0 && retSlots <= MAX_HFA_RET_SLOTS);
            assert(MAX_HFA_RET_SLOTS < sizeof(int) * 8);
            retVal = ((1 << retSlots) - 1) << REG_FLOATRET;
        }
        break;
#endif

        case TYP_LONG:
#if !CPU_HAS_FP_SUPPORT
        case TYP_DOUBLE:
#endif
            retVal = RBM_LNGRET;
            break;

#if CPU_HAS_FP_SUPPORT
        case TYP_FLOAT:
        case TYP_DOUBLE:

            break;
#endif

        case TYP_VOID:
            break;

        default:
            noway_assert(!"unexpected/unhandled fn return type");
    }

    // We now have to generate the "call epilog" (if it was a call to unmanaged code).
    /* if it is a call to unmanaged code, frameListRoot must be set */

    noway_assert((call->gtFlags & GTF_CALL_UNMANAGED) == 0 || frameListRoot);

    if (frameListRoot)
        genPInvokeCallEpilog(frameListRoot, retVal);

    if (frameListRoot && (call->gtCallMoreFlags & GTF_CALL_M_FRAME_VAR_DEATH))
    {
        if (frameListRoot->lvRegister)
        {
            bool isBorn  = false;
            bool isDying = true;
            genUpdateRegLife(frameListRoot, isBorn, isDying DEBUGARG(call));
        }
    }

#ifdef DEBUG
    if (compiler->opts.compStackCheckOnCall
#if defined(USE_TRANSITION_THUNKS) || defined(USE_DYNAMIC_STACK_ALIGN)
        // check the stack as frequently as possible
        && !call->IsHelperCall()
#else
        && call->gtCallType == CT_USER_FUNC
#endif
            )
    {
        noway_assert(compiler->lvaCallEspCheck != 0xCCCCCCCC &&
                     compiler->lvaTable[compiler->lvaCallEspCheck].lvDoNotEnregister &&
                     compiler->lvaTable[compiler->lvaCallEspCheck].lvOnFrame);
        if (argSize > 0)
        {
            getEmitter()->emitIns_R_R(INS_mov, EA_4BYTE, REG_ARG_0, REG_SPBASE);
            getEmitter()->emitIns_R_I(INS_sub, EA_4BYTE, REG_ARG_0, argSize);
            getEmitter()->emitIns_S_R(INS_cmp, EA_4BYTE, REG_ARG_0, compiler->lvaCallEspCheck, 0);
            regTracker.rsTrackRegTrash(REG_ARG_0);
        }
        else
            getEmitter()->emitIns_S_R(INS_cmp, EA_4BYTE, REG_SPBASE, compiler->lvaCallEspCheck, 0);

        BasicBlock*  esp_check = genCreateTempLabel();
        emitJumpKind jmpEqual  = genJumpKindForOper(GT_EQ, CK_SIGNED);
        inst_JMP(jmpEqual, esp_check);
        getEmitter()->emitIns(INS_BREAKPOINT);
        genDefineTempLabel(esp_check);
    }
#endif // DEBUG

#if FEATURE_STACK_FP_X87
    UnspillRegVarsStackFp();
#endif // FEATURE_STACK_FP_X87

    if (call->gtType == TYP_FLOAT || call->gtType == TYP_DOUBLE)
    {
        // Restore return node if necessary
        if (call->gtFlags & GTF_SPILLED)
        {
            UnspillFloat(call);
        }

#if FEATURE_STACK_FP_X87
        // Mark as free
        regSet.SetUsedRegFloat(call, false);
#endif
    }

#if FEATURE_STACK_FP_X87
#ifdef DEBUG
    if (compiler->verbose)
    {
        JitDumpFPState();
    }
#endif
#endif

    return retVal;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*****************************************************************************
 *
 *  Create and record GC Info for the function.
 */
#ifdef JIT32_GCENCODER
void*
#else
void
#endif
CodeGen::genCreateAndStoreGCInfo(unsigned codeSize, unsigned prologSize, unsigned epilogSize DEBUGARG(void* codePtr))
{
#ifdef JIT32_GCENCODER
    return genCreateAndStoreGCInfoJIT32(codeSize, prologSize, epilogSize DEBUGARG(codePtr));
#else
    genCreateAndStoreGCInfoX64(codeSize, prologSize DEBUGARG(codePtr));
#endif
}

#ifdef JIT32_GCENCODER
void* CodeGen::genCreateAndStoreGCInfoJIT32(unsigned codeSize,
                                            unsigned prologSize,
                                            unsigned epilogSize DEBUGARG(void* codePtr))
{
    BYTE    headerBuf[64];
    InfoHdr header;

    int s_cached;
#ifdef DEBUG
    size_t headerSize =
#endif
        compiler->compInfoBlkSize =
            gcInfo.gcInfoBlockHdrSave(headerBuf, 0, codeSize, prologSize, epilogSize, &header, &s_cached);

    size_t argTabOffset = 0;
    size_t ptrMapSize   = gcInfo.gcPtrTableSize(header, codeSize, &argTabOffset);

#if DISPLAY_SIZES

    if (genInterruptible)
    {
        gcHeaderISize += compiler->compInfoBlkSize;
        gcPtrMapISize += ptrMapSize;
    }
    else
    {
        gcHeaderNSize += compiler->compInfoBlkSize;
        gcPtrMapNSize += ptrMapSize;
    }

#endif // DISPLAY_SIZES

    compiler->compInfoBlkSize += ptrMapSize;

    /* Allocate the info block for the method */

    compiler->compInfoBlkAddr = (BYTE*)compiler->info.compCompHnd->allocGCInfo(compiler->compInfoBlkSize);

#if 0 // VERBOSE_SIZES
    // TODO-Review: 'dataSize', below, is not defined

//  if  (compiler->compInfoBlkSize > codeSize && compiler->compInfoBlkSize > 100)
    {
        printf("[%7u VM, %7u+%7u/%7u x86 %03u/%03u%%] %s.%s\n",
               compiler->info.compILCodeSize,
               compiler->compInfoBlkSize,
               codeSize + dataSize,
               codeSize + dataSize - prologSize - epilogSize,
               100 * (codeSize + dataSize) / compiler->info.compILCodeSize,
               100 * (codeSize + dataSize + compiler->compInfoBlkSize) / compiler->info.compILCodeSize,
               compiler->info.compClassName,
               compiler->info.compMethodName);
    }

#endif

    /* Fill in the info block and return it to the caller */

    void* infoPtr = compiler->compInfoBlkAddr;

    /* Create the method info block: header followed by GC tracking tables */

    compiler->compInfoBlkAddr +=
        gcInfo.gcInfoBlockHdrSave(compiler->compInfoBlkAddr, -1, codeSize, prologSize, epilogSize, &header, &s_cached);

    assert(compiler->compInfoBlkAddr == (BYTE*)infoPtr + headerSize);
    compiler->compInfoBlkAddr = gcInfo.gcPtrTableSave(compiler->compInfoBlkAddr, header, codeSize, &argTabOffset);
    assert(compiler->compInfoBlkAddr == (BYTE*)infoPtr + headerSize + ptrMapSize);

#ifdef DEBUG

    if (0)
    {
        BYTE*    temp = (BYTE*)infoPtr;
        unsigned size = compiler->compInfoBlkAddr - temp;
        BYTE*    ptab = temp + headerSize;

        noway_assert(size == headerSize + ptrMapSize);

        printf("Method info block - header [%u bytes]:", headerSize);

        for (unsigned i = 0; i < size; i++)
        {
            if (temp == ptab)
            {
                printf("\nMethod info block - ptrtab [%u bytes]:", ptrMapSize);
                printf("\n    %04X: %*c", i & ~0xF, 3 * (i & 0xF), ' ');
            }
            else
            {
                if (!(i % 16))
                    printf("\n    %04X: ", i);
            }

            printf("%02X ", *temp++);
        }

        printf("\n");
    }

#endif // DEBUG

#if DUMP_GC_TABLES

    if (compiler->opts.dspGCtbls)
    {
        const BYTE* base = (BYTE*)infoPtr;
        unsigned    size;
        unsigned    methodSize;
        InfoHdr     dumpHeader;

        printf("GC Info for method %s\n", compiler->info.compFullName);
        printf("GC info size = %3u\n", compiler->compInfoBlkSize);

        size = gcInfo.gcInfoBlockHdrDump(base, &dumpHeader, &methodSize);
        // printf("size of header encoding is %3u\n", size);
        printf("\n");

        if (compiler->opts.dspGCtbls)
        {
            base += size;
            size = gcInfo.gcDumpPtrTable(base, dumpHeader, methodSize);
            // printf("size of pointer table is %3u\n", size);
            printf("\n");
            noway_assert(compiler->compInfoBlkAddr == (base + size));
        }
    }

#ifdef DEBUG
    if (jitOpts.testMask & 128)
    {
        for (unsigned offs = 0; offs < codeSize; offs++)
        {
            gcInfo.gcFindPtrsInFrame(infoPtr, codePtr, offs);
        }
    }
#endif // DEBUG
#endif // DUMP_GC_TABLES

    /* Make sure we ended up generating the expected number of bytes */

    noway_assert(compiler->compInfoBlkAddr == (BYTE*)infoPtr + compiler->compInfoBlkSize);

    return infoPtr;
}

#else // JIT32_GCENCODER

void CodeGen::genCreateAndStoreGCInfoX64(unsigned codeSize, unsigned prologSize DEBUGARG(void* codePtr))
{
    IAllocator*    allowZeroAlloc = new (compiler, CMK_GC) AllowZeroAllocator(compiler->getAllocatorGC());
    GcInfoEncoder* gcInfoEncoder  = new (compiler, CMK_GC)
        GcInfoEncoder(compiler->info.compCompHnd, compiler->info.compMethodInfo, allowZeroAlloc, NOMEM);
    assert(gcInfoEncoder);

    // Follow the code pattern of the x86 gc info encoder (genCreateAndStoreGCInfoJIT32).
    gcInfo.gcInfoBlockHdrSave(gcInfoEncoder, codeSize, prologSize);

    // We keep the call count for the second call to gcMakeRegPtrTable() below.
    unsigned callCnt = 0;
    // First we figure out the encoder ID's for the stack slots and registers.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_ASSIGN_SLOTS, &callCnt);
    // Now we've requested all the slots we'll need; "finalize" these (make more compact data structures for them).
    gcInfoEncoder->FinalizeSlotIds();
    // Now we can actually use those slot ID's to declare live ranges.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_DO_WORK, &callCnt);

    gcInfoEncoder->Build();

    // GC Encoder automatically puts the GC info in the right spot using ICorJitInfo::allocGCInfo(size_t)
    // let's save the values anyway for debugging purposes
    compiler->compInfoBlkAddr = gcInfoEncoder->Emit();
    compiler->compInfoBlkSize = 0; // not exposed by the GCEncoder interface
}
#endif

/*****************************************************************************
 *  For CEE_LOCALLOC
 */

regNumber CodeGen::genLclHeap(GenTreePtr size)
{
    noway_assert((genActualType(size->gtType) == TYP_INT) || (genActualType(size->gtType) == TYP_I_IMPL));

    // regCnt is a register used to hold both
    //              the amount to stack alloc (either in bytes or pointer sized words)
    //          and the final stack alloc address to return as the result
    //
    regNumber regCnt = DUMMY_INIT(REG_CORRUPT);
    var_types type   = genActualType(size->gtType);
    emitAttr  easz   = emitTypeSize(type);

#ifdef DEBUG
    // Verify ESP
    if (compiler->opts.compStackCheckOnRet)
    {
        noway_assert(compiler->lvaReturnEspCheck != 0xCCCCCCCC &&
                     compiler->lvaTable[compiler->lvaReturnEspCheck].lvDoNotEnregister &&
                     compiler->lvaTable[compiler->lvaReturnEspCheck].lvOnFrame);
        getEmitter()->emitIns_S_R(INS_cmp, EA_PTRSIZE, REG_SPBASE, compiler->lvaReturnEspCheck, 0);

        BasicBlock*  esp_check = genCreateTempLabel();
        emitJumpKind jmpEqual  = genJumpKindForOper(GT_EQ, CK_SIGNED);
        inst_JMP(jmpEqual, esp_check);
        getEmitter()->emitIns(INS_BREAKPOINT);
        genDefineTempLabel(esp_check);
    }
#endif

    noway_assert(isFramePointerUsed());
    noway_assert(genStackLevel == 0); // Can't have anything on the stack

    BasicBlock* endLabel = NULL;
#if FEATURE_FIXED_OUT_ARGS
    bool stackAdjusted = false;
#endif

    if (size->IsCnsIntOrI())
    {
#if FEATURE_FIXED_OUT_ARGS
        // If we have an outgoing arg area then we must adjust the SP
        // essentially popping off the outgoing arg area,
        // We will restore it right before we return from this method
        //
        if (compiler->lvaOutgoingArgSpaceSize > 0)
        {
            assert((compiler->lvaOutgoingArgSpaceSize % STACK_ALIGN) ==
                   0); // This must be true for the stack to remain aligned
            inst_RV_IV(INS_add, REG_SPBASE, compiler->lvaOutgoingArgSpaceSize, EA_PTRSIZE);
            stackAdjusted = true;
        }
#endif
        size_t amount = size->gtIntCon.gtIconVal;

        // Convert amount to be properly STACK_ALIGN and count of DWORD_PTRs
        amount += (STACK_ALIGN - 1);
        amount &= ~(STACK_ALIGN - 1);
        amount >>= STACK_ALIGN_SHIFT;      // amount is number of pointer-sized words to locAlloc
        size->gtIntCon.gtIconVal = amount; // update the GT_CNS value in the node

        /* If amount is zero then return null in RegCnt */
        if (amount == 0)
        {
            regCnt = regSet.rsGrabReg(RBM_ALLINT);
            instGen_Set_Reg_To_Zero(EA_PTRSIZE, regCnt);
            goto DONE;
        }

        /* For small allocations we will generate up to six push 0 inline */
        if (amount <= 6)
        {
            regCnt = regSet.rsGrabReg(RBM_ALLINT);
#if CPU_LOAD_STORE_ARCH
            regNumber regZero = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(regCnt));
            // Set 'regZero' to zero
            instGen_Set_Reg_To_Zero(EA_PTRSIZE, regZero);
#endif

            while (amount != 0)
            {
#if CPU_LOAD_STORE_ARCH
                inst_IV(INS_push, (unsigned)genRegMask(regZero));
#else
                inst_IV(INS_push_hide, 0); // push_hide means don't track the stack
#endif
                amount--;
            }

            regTracker.rsTrackRegTrash(regCnt);
            // --- move regCnt, ESP
            inst_RV_RV(INS_mov, regCnt, REG_SPBASE, TYP_I_IMPL);
            goto DONE;
        }
        else
        {
            if (!compiler->info.compInitMem)
            {
                // Re-bias amount to be number of bytes to adjust the SP
                amount <<= STACK_ALIGN_SHIFT;
                size->gtIntCon.gtIconVal = amount;      // update the GT_CNS value in the node
                if (amount < compiler->eeGetPageSize()) // must be < not <=
                {
                    // Since the size is a page or less, simply adjust ESP

                    // ESP might already be in the guard page, must touch it BEFORE
                    // the alloc, not after.
                    regCnt = regSet.rsGrabReg(RBM_ALLINT);
                    inst_RV_RV(INS_mov, regCnt, REG_SPBASE, TYP_I_IMPL);
#if CPU_LOAD_STORE_ARCH
                    regNumber regTmp = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(regCnt));
                    getEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, regTmp, REG_SPBASE, 0);
                    regTracker.rsTrackRegTrash(regTmp);
#else
                    getEmitter()->emitIns_AR_R(INS_TEST, EA_4BYTE, REG_SPBASE, REG_SPBASE, 0);
#endif
                    inst_RV_IV(INS_sub, regCnt, amount, EA_PTRSIZE);
                    inst_RV_RV(INS_mov, REG_SPBASE, regCnt, TYP_I_IMPL);
                    regTracker.rsTrackRegTrash(regCnt);
                    goto DONE;
                }
            }
        }
    }

    // Compute the size of the block to allocate
    genCompIntoFreeReg(size, 0, RegSet::KEEP_REG);
    noway_assert(size->InReg());
    regCnt = size->gtRegNum;

#if FEATURE_FIXED_OUT_ARGS
    // If we have an outgoing arg area then we must adjust the SP
    // essentially popping off the outgoing arg area,
    // We will restore it right before we return from this method
    //
    if ((compiler->lvaOutgoingArgSpaceSize > 0) && !stackAdjusted)
    {
        assert((compiler->lvaOutgoingArgSpaceSize % STACK_ALIGN) ==
               0); // This must be true for the stack to remain aligned
        inst_RV_IV(INS_add, REG_SPBASE, compiler->lvaOutgoingArgSpaceSize, EA_PTRSIZE);
        stackAdjusted = true;
    }
#endif

    //  Perform alignment if we don't have a GT_CNS size
    //
    if (!size->IsCnsIntOrI())
    {
        endLabel = genCreateTempLabel();

        // If 0 we bail out
        instGen_Compare_Reg_To_Zero(easz, regCnt); // set flags
        emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
        inst_JMP(jmpEqual, endLabel);

        // Align to STACK_ALIGN
        inst_RV_IV(INS_add, regCnt, (STACK_ALIGN - 1), emitActualTypeSize(type));

        if (compiler->info.compInitMem)
        {
#if ((STACK_ALIGN >> STACK_ALIGN_SHIFT) > 1)
            // regCnt will be the number of pointer-sized words to locAlloc
            // If the shift right won't do the 'and' do it here
            inst_RV_IV(INS_AND, regCnt, ~(STACK_ALIGN - 1), emitActualTypeSize(type));
#endif
            // --- shr regCnt, 2 ---
            inst_RV_SH(INS_SHIFT_RIGHT_LOGICAL, EA_PTRSIZE, regCnt, STACK_ALIGN_SHIFT);
        }
        else
        {
            // regCnt will be the total number of bytes to locAlloc

            inst_RV_IV(INS_AND, regCnt, ~(STACK_ALIGN - 1), emitActualTypeSize(type));
        }
    }

    BasicBlock* loop;
    loop = genCreateTempLabel();

    if (compiler->info.compInitMem)
    {
        // At this point 'regCnt' is set to the number of pointer-sized words to locAlloc

        /* Since we have to zero out the allocated memory AND ensure that
           ESP is always valid by tickling the pages, we will just push 0's
           on the stack */
        CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_ARM_)
        regNumber regZero1 = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(regCnt));
        regNumber regZero2 = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(regCnt) & ~genRegMask(regZero1));
        // Set 'regZero1' and 'regZero2' to zero
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, regZero1);
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, regZero2);
#endif

        // Loop:
        genDefineTempLabel(loop);

#if defined(_TARGET_X86_)

        inst_IV(INS_push_hide, 0); // --- push 0
        // Are we done?
        inst_RV(INS_dec, regCnt, type);

#elif defined(_TARGET_ARM_)

        inst_IV(INS_push, (unsigned)(genRegMask(regZero1) | genRegMask(regZero2)));
        // Are we done?
        inst_RV_IV(INS_sub, regCnt, 2, emitActualTypeSize(type), INS_FLAGS_SET);

#else
        assert(!"Codegen missing");
#endif // TARGETS

        emitJumpKind jmpNotEqual = genJumpKindForOper(GT_NE, CK_SIGNED);
        inst_JMP(jmpNotEqual, loop);

        // Move the final value of ESP into regCnt
        inst_RV_RV(INS_mov, regCnt, REG_SPBASE);
        regTracker.rsTrackRegTrash(regCnt);
    }
    else
    {
        // At this point 'regCnt' is set to the total number of bytes to locAlloc

        /* We don't need to zero out the allocated memory. However, we do have
           to tickle the pages to ensure that ESP is always valid and is
           in sync with the "stack guard page".  Note that in the worst
           case ESP is on the last byte of the guard page.  Thus you must
           touch ESP+0 first not ESP+x01000.

           Another subtlety is that you don't want ESP to be exactly on the
           boundary of the guard page because PUSH is predecrement, thus
           call setup would not touch the guard page but just beyond it */

        /* Note that we go through a few hoops so that ESP never points to
           illegal pages at any time during the ticking process

                  neg   REG
                  add   REG, ESP         // reg now holds ultimate ESP
                  jb    loop             // result is smaller than orignial ESP (no wrap around)
                  xor   REG, REG,        // Overflow, pick lowest possible number
             loop:
                  test  ESP, [ESP+0]     // X86 - tickle the page
                  ldr   REGH,[ESP+0]     // ARM - tickle the page
                  mov   REGH, ESP
                  sub   REGH, GetOsPageSize()
                  mov   ESP, REGH
                  cmp   ESP, REG
                  jae   loop

                  mov   ESP, REG
             end:
          */
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_ARM_

        inst_RV_RV_RV(INS_sub, regCnt, REG_SPBASE, regCnt, EA_4BYTE, INS_FLAGS_SET);
        inst_JMP(EJ_hs, loop);
#else
        inst_RV(INS_NEG, regCnt, TYP_I_IMPL);
        inst_RV_RV(INS_add, regCnt, REG_SPBASE, TYP_I_IMPL);
        inst_JMP(EJ_jb, loop);
#endif
        regTracker.rsTrackRegTrash(regCnt);

        instGen_Set_Reg_To_Zero(EA_PTRSIZE, regCnt);

        genDefineTempLabel(loop);

        // This is a workaround to avoid the emitter trying to track the
        // decrement of the ESP - we do the subtraction in another reg
        // instead of adjusting ESP directly.

        regNumber regTemp = regSet.rsPickReg();

        // Tickle the decremented value, and move back to ESP,
        // note that it has to be done BEFORE the update of ESP since
        // ESP might already be on the guard page.  It is OK to leave
        // the final value of ESP on the guard page
        CLANG_FORMAT_COMMENT_ANCHOR;

#if CPU_LOAD_STORE_ARCH
        getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, regTemp, REG_SPBASE, 0);
#else
        getEmitter()->emitIns_AR_R(INS_TEST, EA_4BYTE, REG_SPBASE, REG_SPBASE, 0);
#endif

        inst_RV_RV(INS_mov, regTemp, REG_SPBASE, TYP_I_IMPL);
        regTracker.rsTrackRegTrash(regTemp);

        inst_RV_IV(INS_sub, regTemp, compiler->eeGetPageSize(), EA_PTRSIZE);
        inst_RV_RV(INS_mov, REG_SPBASE, regTemp, TYP_I_IMPL);

        genRecoverReg(size, RBM_ALLINT,
                      RegSet::KEEP_REG); // not purely the 'size' tree anymore; though it is derived from 'size'
        noway_assert(size->InReg());
        regCnt = size->gtRegNum;
        inst_RV_RV(INS_cmp, REG_SPBASE, regCnt, TYP_I_IMPL);
        emitJumpKind jmpGEU = genJumpKindForOper(GT_GE, CK_UNSIGNED);
        inst_JMP(jmpGEU, loop);

        // Move the final value to ESP
        inst_RV_RV(INS_mov, REG_SPBASE, regCnt);
    }
    regSet.rsMarkRegFree(genRegMask(regCnt));

DONE:

    noway_assert(regCnt != DUMMY_INIT(REG_CORRUPT));

    if (endLabel != NULL)
        genDefineTempLabel(endLabel);

#if FEATURE_FIXED_OUT_ARGS
    // If we have an outgoing arg area then we must readjust the SP
    //
    if (stackAdjusted)
    {
        assert(compiler->lvaOutgoingArgSpaceSize > 0);
        assert((compiler->lvaOutgoingArgSpaceSize % STACK_ALIGN) ==
               0); // This must be true for the stack to remain aligned
        inst_RV_IV(INS_sub, REG_SPBASE, compiler->lvaOutgoingArgSpaceSize, EA_PTRSIZE);
    }
#endif

    /* Write the lvaLocAllocSPvar stack frame slot */
    if (compiler->lvaLocAllocSPvar != BAD_VAR_NUM)
    {
        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaLocAllocSPvar, 0);
    }

#if STACK_PROBES
    // Don't think it is worth it the codegen complexity to embed this
    // when it's possible in each of the customized allocas.
    if (compiler->opts.compNeedStackProbes)
    {
        genGenerateStackProbe();
    }
#endif

#ifdef DEBUG
    // Update new ESP
    if (compiler->opts.compStackCheckOnRet)
    {
        noway_assert(compiler->lvaReturnEspCheck != 0xCCCCCCCC &&
                     compiler->lvaTable[compiler->lvaReturnEspCheck].lvDoNotEnregister &&
                     compiler->lvaTable[compiler->lvaReturnEspCheck].lvOnFrame);
        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaReturnEspCheck, 0);
    }
#endif

    return regCnt;
}

/*****************************************************************************
 *
 *  Return non-zero if the given register is free after the given tree is
 *  evaluated (i.e. the register is either not used at all, or it holds a
 *  register variable which is not live after the given node).
 *  This is only called by genCreateAddrMode, when tree is a GT_ADD, with one
 *  constant operand, and one that's in a register.  Thus, the only thing we
 *  need to determine is whether the register holding op1 is dead.
 */
bool CodeGen::genRegTrashable(regNumber reg, GenTreePtr tree)
{
    regMaskTP vars;
    regMaskTP mask = genRegMask(reg);

    if (regSet.rsMaskUsed & mask)
        return false;

    assert(tree->gtOper == GT_ADD);
    GenTreePtr regValTree = tree->gtOp.gtOp1;
    if (!tree->gtOp.gtOp2->IsCnsIntOrI())
    {
        regValTree = tree->gtOp.gtOp2;
        assert(tree->gtOp.gtOp1->IsCnsIntOrI());
    }
    assert(regValTree->InReg());

    /* At this point, the only way that the register will remain live
     * is if it is itself a register variable that isn't dying.
     */
    assert(regValTree->gtRegNum == reg);
    if (regValTree->IsRegVar() && !regValTree->IsRegVarDeath())
        return false;
    else
        return true;
}

/*****************************************************************************/
//
// This method calculates the USE and DEF values for a statement.
// It also calls fgSetRngChkTarget for the statement.
//
// We refactor out this code from fgPerBlockLocalVarLiveness
// and add QMARK logics to it.
//
// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
//
// The usage of this method is very limited.
// We should only call it for the first node in the statement or
// for the node after the GTF_RELOP_QMARK node.
//
// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE

/*
       Since a GT_QMARK tree can take two paths (i.e. the thenTree Path or the elseTree path),
       when we calculate its fgCurDefSet and fgCurUseSet, we need to combine the results
       from both trees.

       Note that the GT_QMARK trees are threaded as shown below with nodes 1 to 11
       linked by gtNext.

       The algorithm we use is:
       (1) We walk these nodes according the the evaluation order (i.e. from node 1 to node 11).
       (2) When we see the GTF_RELOP_QMARK node, we know we are about to split the path.
           We cache copies of current fgCurDefSet and fgCurUseSet.
           (The fact that it is recursively calling itself is for nested QMARK case,
            where we need to remember multiple copies of fgCurDefSet and fgCurUseSet.)
       (3) We walk the thenTree.
       (4) When we see GT_COLON node, we know that we just finished the thenTree.
           We then make a copy of the current fgCurDefSet and fgCurUseSet,
           restore them to the ones before the thenTree, and then continue walking
           the elseTree.
       (5) When we see the GT_QMARK node, we know we just finished the elseTree.
           So we combine the results from the thenTree and elseTree and then return.


                                 +--------------------+
                                 |      GT_QMARK    11|
                                 +----------+---------+
                                            |
                                            *
                                           / \
                                         /     \
                                       /         \
                  +---------------------+       +--------------------+
                  |      GT_<cond>    3 |       |     GT_COLON     7 |
                  |  w/ GTF_RELOP_QMARK |       |  w/ GTF_COLON_COND |
                  +----------+----------+       +---------+----------+
                             |                            |
                             *                            *
                            / \                          / \
                          /     \                      /     \
                        /         \                  /         \
                       2           1          thenTree 6       elseTree 10
                                  x               |                |
                                 /                *                *
     +----------------+        /                 / \              / \
     |prevExpr->gtNext+------/                 /     \          /     \
     +----------------+                      /         \      /         \
                                            5           4    9           8


*/

GenTreePtr Compiler::fgLegacyPerStatementLocalVarLiveness(GenTreePtr startNode, // The node to start walking with.
                                                          GenTreePtr relopNode) // The node before the startNode.
                                                                                // (It should either be NULL or
                                                                                // a GTF_RELOP_QMARK node.)
{
    GenTreePtr tree;

    VARSET_TP defSet_BeforeSplit(VarSetOps::MakeCopy(this, fgCurDefSet)); // Store the current fgCurDefSet and
                                                                          // fgCurUseSet so
    VARSET_TP useSet_BeforeSplit(VarSetOps::MakeCopy(this, fgCurUseSet)); // we can restore then before entering the
                                                                          // elseTree.

    MemoryKindSet memoryUse_BeforeSplit   = fgCurMemoryUse;
    MemoryKindSet memoryDef_BeforeSplit   = fgCurMemoryDef;
    MemoryKindSet memoryHavoc_BeforeSplit = fgCurMemoryHavoc;

    VARSET_TP defSet_AfterThenTree(VarSetOps::MakeEmpty(this)); // These two variables will store
                                                                // the USE and DEF sets after
    VARSET_TP useSet_AfterThenTree(VarSetOps::MakeEmpty(this)); // evaluating the thenTree.

    MemoryKindSet memoryUse_AfterThenTree   = fgCurMemoryUse;
    MemoryKindSet memoryDef_AfterThenTree   = fgCurMemoryDef;
    MemoryKindSet memoryHavoc_AfterThenTree = fgCurMemoryHavoc;

    // relopNode is either NULL or a GTF_RELOP_QMARK node.
    assert(!relopNode || (relopNode->OperKind() & GTK_RELOP) && (relopNode->gtFlags & GTF_RELOP_QMARK));

    // If relopNode is NULL, then the startNode must be the 1st node of the statement.
    // If relopNode is non-NULL, then the startNode must be the node right after the GTF_RELOP_QMARK node.
    assert((!relopNode && startNode == compCurStmt->gtStmt.gtStmtList) ||
           (relopNode && startNode == relopNode->gtNext));

    for (tree = startNode; tree; tree = tree->gtNext)
    {
        switch (tree->gtOper)
        {

            case GT_QMARK:

                // This must be a GT_QMARK node whose GTF_RELOP_QMARK node is recursively calling us.
                noway_assert(relopNode && tree->gtOp.gtOp1 == relopNode);

                // By the time we see a GT_QMARK, we must have finished processing the elseTree.
                // So it's the time to combine the results
                // from the the thenTree and the elseTree, and then return.

                VarSetOps::IntersectionD(this, fgCurDefSet, defSet_AfterThenTree);
                VarSetOps::UnionD(this, fgCurUseSet, useSet_AfterThenTree);

                fgCurMemoryDef   = fgCurMemoryDef & memoryDef_AfterThenTree;
                fgCurMemoryHavoc = fgCurMemoryHavoc & memoryHavoc_AfterThenTree;
                fgCurMemoryUse   = fgCurMemoryUse | memoryUse_AfterThenTree;

                // Return the GT_QMARK node itself so the caller can continue from there.
                // NOTE: the caller will get to the next node by doing the "tree = tree->gtNext"
                // in the "for" statement.
                goto _return;

            case GT_COLON:
                // By the time we see GT_COLON, we must have just walked the thenTree.
                // So we need to do two things here.
                // (1) Save the current fgCurDefSet and fgCurUseSet so that later we can combine them
                //     with the result from the elseTree.
                // (2) Restore fgCurDefSet and fgCurUseSet to the points before the thenTree is walked.
                //     and then continue walking the elseTree.
                VarSetOps::Assign(this, defSet_AfterThenTree, fgCurDefSet);
                VarSetOps::Assign(this, useSet_AfterThenTree, fgCurUseSet);

                memoryDef_AfterThenTree   = fgCurMemoryDef;
                memoryHavoc_AfterThenTree = fgCurMemoryHavoc;
                memoryUse_AfterThenTree   = fgCurMemoryUse;

                VarSetOps::Assign(this, fgCurDefSet, defSet_BeforeSplit);
                VarSetOps::Assign(this, fgCurUseSet, useSet_BeforeSplit);

                fgCurMemoryDef   = memoryDef_BeforeSplit;
                fgCurMemoryHavoc = memoryHavoc_BeforeSplit;
                fgCurMemoryUse   = memoryUse_BeforeSplit;

                break;

            case GT_LCL_VAR:
            case GT_LCL_FLD:
            case GT_LCL_VAR_ADDR:
            case GT_LCL_FLD_ADDR:
            case GT_STORE_LCL_VAR:
            case GT_STORE_LCL_FLD:
                fgMarkUseDef(tree->AsLclVarCommon());
                break;

            case GT_CLS_VAR:
                // For Volatile indirection, first mutate GcHeap/ByrefExposed
                // see comments in ValueNum.cpp (under case GT_CLS_VAR)
                // This models Volatile reads as def-then-use of the heap.
                // and allows for a CSE of a subsequent non-volatile read
                if ((tree->gtFlags & GTF_FLD_VOLATILE) != 0)
                {
                    // For any Volatile indirection, we must handle it as a
                    // definition of GcHeap/ByrefExposed
                    fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
                }
                // If the GT_CLS_VAR is the lhs of an assignment, we'll handle it as a heap def, when we get to
                // assignment.
                // Otherwise, we treat it as a use here.
                if ((tree->gtFlags & GTF_CLS_VAR_ASG_LHS) == 0)
                {
                    fgCurMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
                }
                break;

            case GT_IND:
                // For Volatile indirection, first mutate GcHeap/ByrefExposed
                // see comments in ValueNum.cpp (under case GT_CLS_VAR)
                // This models Volatile reads as def-then-use of the heap.
                // and allows for a CSE of a subsequent non-volatile read
                if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
                {
                    // For any Volatile indirection, we must handle it as a
                    // definition of GcHeap/ByrefExposed
                    fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
                }

                // If the GT_IND is the lhs of an assignment, we'll handle it
                // as a heap/byref def, when we get to assignment.
                // Otherwise, we treat it as a use here.
                if ((tree->gtFlags & GTF_IND_ASG_LHS) == 0)
                {
                    GenTreeLclVarCommon* dummyLclVarTree = NULL;
                    bool                 dummyIsEntire   = false;
                    GenTreePtr           addrArg         = tree->gtOp.gtOp1->gtEffectiveVal(/*commaOnly*/ true);
                    if (!addrArg->DefinesLocalAddr(this, /*width doesn't matter*/ 0, &dummyLclVarTree, &dummyIsEntire))
                    {
                        fgCurMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
                    }
                    else
                    {
                        // Defines a local addr
                        assert(dummyLclVarTree != nullptr);
                        fgMarkUseDef(dummyLclVarTree->AsLclVarCommon());
                    }
                }
                break;

            // These should have been morphed away to become GT_INDs:
            case GT_FIELD:
            case GT_INDEX:
                unreached();
                break;

            // We'll assume these are use-then-defs of GcHeap/ByrefExposed.
            case GT_LOCKADD:
            case GT_XADD:
            case GT_XCHG:
            case GT_CMPXCHG:
                fgCurMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
                fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
                fgCurMemoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                break;

            case GT_MEMORYBARRIER:
                // Simliar to any Volatile indirection, we must handle this as a definition of GcHeap/ByrefExposed
                fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
                break;

            // For now, all calls read/write GcHeap/ByrefExposed, writes in their entirety.  Might tighten this case
            // later.
            case GT_CALL:
            {
                GenTreeCall* call    = tree->AsCall();
                bool         modHeap = true;
                if (call->gtCallType == CT_HELPER)
                {
                    CorInfoHelpFunc helpFunc = eeGetHelperNum(call->gtCallMethHnd);

                    if (!s_helperCallProperties.MutatesHeap(helpFunc) && !s_helperCallProperties.MayRunCctor(helpFunc))
                    {
                        modHeap = false;
                    }
                }
                if (modHeap)
                {
                    fgCurMemoryUse |= memoryKindSet(GcHeap, ByrefExposed);
                    fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
                    fgCurMemoryHavoc |= memoryKindSet(GcHeap, ByrefExposed);
                }
            }

                // If this is a p/invoke unmanaged call or if this is a tail-call
                // and we have an unmanaged p/invoke call in the method,
                // then we're going to run the p/invoke epilog.
                // So we mark the FrameRoot as used by this instruction.
                // This ensures that the block->bbVarUse will contain
                // the FrameRoot local var if is it a tracked variable.

                if (tree->gtCall.IsUnmanaged() || (tree->gtCall.IsTailCall() && info.compCallUnmanaged))
                {
                    /* Get the TCB local and mark it as used */

                    noway_assert(info.compLvFrameListRoot < lvaCount);

                    LclVarDsc* varDsc = &lvaTable[info.compLvFrameListRoot];

                    if (varDsc->lvTracked)
                    {
                        if (!VarSetOps::IsMember(this, fgCurDefSet, varDsc->lvVarIndex))
                        {
                            VarSetOps::AddElemD(this, fgCurUseSet, varDsc->lvVarIndex);
                        }
                    }
                }

                break;

            default:

                // Determine what memory kinds it defines.
                if (tree->OperIsAssignment() || tree->OperIsBlkOp())
                {
                    GenTreeLclVarCommon* dummyLclVarTree = NULL;
                    if (tree->DefinesLocal(this, &dummyLclVarTree))
                    {
                        if (lvaVarAddrExposed(dummyLclVarTree->gtLclNum))
                        {
                            fgCurMemoryDef |= memoryKindSet(ByrefExposed);

                            // We've found a store that modifies ByrefExposed
                            // memory but not GcHeap memory, so track their
                            // states separately.
                            byrefStatesMatchGcHeapStates = false;
                        }
                    }
                    else
                    {
                        // If it doesn't define a local, then it might update GcHeap/ByrefExposed.
                        fgCurMemoryDef |= memoryKindSet(GcHeap, ByrefExposed);
                    }
                }

                // Are we seeing a GT_<cond> for a GT_QMARK node?
                if ((tree->OperKind() & GTK_RELOP) && (tree->gtFlags & GTF_RELOP_QMARK))
                {
                    // We are about to enter the parallel paths (i.e. the thenTree and the elseTree).
                    // Recursively call fgLegacyPerStatementLocalVarLiveness.
                    // At the very beginning of fgLegacyPerStatementLocalVarLiveness, we will cache the values of the
                    // current
                    // fgCurDefSet and fgCurUseSet into local variables defSet_BeforeSplit and useSet_BeforeSplit.
                    // The cached values will be used to restore fgCurDefSet and fgCurUseSet once we see the GT_COLON
                    // node.
                    tree = fgLegacyPerStatementLocalVarLiveness(tree->gtNext, tree);

                    // We must have been returned here after seeing a GT_QMARK node.
                    noway_assert(tree->gtOper == GT_QMARK);
                }

                break;
        }
    }

_return:
    return tree;
}

/*****************************************************************************/

/*****************************************************************************
 * Initialize the TCB local and the NDirect stub, afterwards "push"
 * the hoisted NDirect stub.
 *
 * 'initRegs' is the set of registers which will be zeroed out by the prolog
 *             typically initRegs is zero
 *
 * The layout of the NDirect Inlined Call Frame is as follows:
 * (see VM/frames.h and VM/JITInterface.cpp for more information)
 *
 *   offset     field name                        when set
 *  --------------------------------------------------------------
 *    +00h      vptr for class InlinedCallFrame   method prolog
 *    +04h      m_Next                            method prolog
 *    +08h      m_Datum                           call site
 *    +0ch      m_pCallSiteTracker (callsite ESP) call site and zeroed in method prolog
 *    +10h      m_pCallerReturnAddress            call site
 *    +14h      m_pCalleeSavedRegisters           not set by JIT
 *    +18h      JIT retval spill area (int)       before call_gc
 *    +1ch      JIT retval spill area (long)      before call_gc
 *    +20h      Saved value of EBP                method prolog
 */

regMaskTP CodeGen::genPInvokeMethodProlog(regMaskTP initRegs)
{
    assert(compiler->compGeneratingProlog);
    noway_assert(!compiler->opts.ShouldUsePInvokeHelpers());
    noway_assert(compiler->info.compCallUnmanaged);

    CORINFO_EE_INFO* pInfo = compiler->eeGetEEInfo();
    noway_assert(compiler->lvaInlinedPInvokeFrameVar != BAD_VAR_NUM);

    /* let's find out if compLvFrameListRoot is enregistered */

    LclVarDsc* varDsc = &compiler->lvaTable[compiler->info.compLvFrameListRoot];

    noway_assert(!varDsc->lvIsParam);
    noway_assert(varDsc->lvType == TYP_I_IMPL);

    DWORD threadTlsIndex, *pThreadTlsIndex;

    threadTlsIndex = compiler->info.compCompHnd->getThreadTLSIndex((void**)&pThreadTlsIndex);
#if defined(_TARGET_X86_)
    if (threadTlsIndex == (DWORD)-1 || pInfo->osType != CORINFO_WINNT)
#else
    if (true)
#endif
    {
        // Instead of calling GetThread(), and getting GS cookie and
        // InlinedCallFrame vptr through indirections, we'll call only one helper.
        // The helper takes frame address in REG_PINVOKE_FRAME, returns TCB in REG_PINVOKE_TCB
        // and uses REG_PINVOKE_SCRATCH as scratch register.
        getEmitter()->emitIns_R_S(INS_lea, EA_PTRSIZE, REG_PINVOKE_FRAME, compiler->lvaInlinedPInvokeFrameVar,
                                  pInfo->inlinedCallFrameInfo.offsetOfFrameVptr);
        regTracker.rsTrackRegTrash(REG_PINVOKE_FRAME);

        // We're about to trask REG_PINVOKE_TCB, it better not be in use!
        assert((regSet.rsMaskUsed & RBM_PINVOKE_TCB) == 0);

        // Don't use the argument registers (including the special argument in
        // REG_PINVOKE_FRAME) for computing the target address.
        regSet.rsLockReg(RBM_ARG_REGS | RBM_PINVOKE_FRAME);

        genEmitHelperCall(CORINFO_HELP_INIT_PINVOKE_FRAME, 0, EA_UNKNOWN);

        regSet.rsUnlockReg(RBM_ARG_REGS | RBM_PINVOKE_FRAME);

        if (varDsc->lvRegister)
        {
            regNumber regTgt = varDsc->lvRegNum;

            // we are about to initialize it. So turn the bit off in initRegs to prevent
            // the prolog reinitializing it.
            initRegs &= ~genRegMask(regTgt);

            if (regTgt != REG_PINVOKE_TCB)
            {
                // move TCB to the its register if necessary
                getEmitter()->emitIns_R_R(INS_mov, EA_PTRSIZE, regTgt, REG_PINVOKE_TCB);
                regTracker.rsTrackRegTrash(regTgt);
            }
        }
        else
        {
            // move TCB to its stack location
            getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_PINVOKE_TCB,
                                      compiler->info.compLvFrameListRoot, 0);
        }

        // We are done, the rest of this function deals with the inlined case.
        return initRegs;
    }

    regNumber regTCB;

    if (varDsc->lvRegister)
    {
        regTCB = varDsc->lvRegNum;

        // we are about to initialize it. So turn the bit off in initRegs to prevent
        // the prolog reinitializing it.
        initRegs &= ~genRegMask(regTCB);
    }
    else // varDsc is allocated on the Stack
    {
        regTCB = REG_PINVOKE_TCB;
    }

#if !defined(_TARGET_ARM_)
#define WIN_NT_TLS_OFFSET (0xE10)
#define WIN_NT5_TLS_HIGHOFFSET (0xf94)

    /* get TCB,  mov reg, FS:[compiler->info.compEEInfo.threadTlsIndex] */

    // TODO-ARM-CQ: should we inline TlsGetValue here?

    if (threadTlsIndex < 64)
    {
        //  mov  reg, FS:[0xE10+threadTlsIndex*4]
        getEmitter()->emitIns_R_C(ins_Load(TYP_I_IMPL), EA_PTRSIZE, regTCB, FLD_GLOBAL_FS,
                                  WIN_NT_TLS_OFFSET + threadTlsIndex * sizeof(int));
        regTracker.rsTrackRegTrash(regTCB);
    }
    else
    {
        DWORD basePtr = WIN_NT5_TLS_HIGHOFFSET;
        threadTlsIndex -= 64;

        // mov reg, FS:[0x2c] or mov reg, fs:[0xf94]
        // mov reg, [reg+threadTlsIndex*4]

        getEmitter()->emitIns_R_C(ins_Load(TYP_I_IMPL), EA_PTRSIZE, regTCB, FLD_GLOBAL_FS, basePtr);
        getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, regTCB, regTCB, threadTlsIndex * sizeof(int));
        regTracker.rsTrackRegTrash(regTCB);
    }
#endif

    /* save TCB in local var if not enregistered */

    if (!varDsc->lvRegister)
    {
        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, regTCB, compiler->info.compLvFrameListRoot, 0);
    }

    /* set frame's vptr */

    const void *inlinedCallFrameVptr, **pInlinedCallFrameVptr;
    inlinedCallFrameVptr = compiler->info.compCompHnd->getInlinedCallFrameVptr((void**)&pInlinedCallFrameVptr);
    noway_assert(inlinedCallFrameVptr != NULL); // if we have the TLS index, vptr must also be known

    instGen_Store_Imm_Into_Lcl(TYP_I_IMPL, EA_HANDLE_CNS_RELOC, (ssize_t)inlinedCallFrameVptr,
                               compiler->lvaInlinedPInvokeFrameVar, pInfo->inlinedCallFrameInfo.offsetOfFrameVptr,
                               REG_PINVOKE_SCRATCH);

    // Set the GSCookie
    GSCookie gsCookie, *pGSCookie;
    compiler->info.compCompHnd->getGSCookie(&gsCookie, &pGSCookie);
    noway_assert(gsCookie != 0); // if we have the TLS index, GS cookie must also be known

    instGen_Store_Imm_Into_Lcl(TYP_I_IMPL, EA_PTRSIZE, (ssize_t)gsCookie, compiler->lvaInlinedPInvokeFrameVar,
                               pInfo->inlinedCallFrameInfo.offsetOfGSCookie, REG_PINVOKE_SCRATCH);

    /* Get current frame root (mov reg2, [reg+offsetOfThreadFrame]) and
       set next field in frame */

    getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_PINVOKE_SCRATCH, regTCB,
                               pInfo->offsetOfThreadFrame);
    regTracker.rsTrackRegTrash(REG_PINVOKE_SCRATCH);

    getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_PINVOKE_SCRATCH,
                              compiler->lvaInlinedPInvokeFrameVar, pInfo->inlinedCallFrameInfo.offsetOfFrameLink);

    noway_assert(isFramePointerUsed()); // Setup of Pinvoke frame currently requires an EBP style frame

    /* set EBP value in frame */
    getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, genFramePointerReg(),
                              compiler->lvaInlinedPInvokeFrameVar, pInfo->inlinedCallFrameInfo.offsetOfCalleeSavedFP);

    /* reset track field in frame */
    instGen_Store_Imm_Into_Lcl(TYP_I_IMPL, EA_PTRSIZE, 0, compiler->lvaInlinedPInvokeFrameVar,
                               pInfo->inlinedCallFrameInfo.offsetOfReturnAddress, REG_PINVOKE_SCRATCH);

    /* get address of our frame */

    getEmitter()->emitIns_R_S(INS_lea, EA_PTRSIZE, REG_PINVOKE_SCRATCH, compiler->lvaInlinedPInvokeFrameVar,
                              pInfo->inlinedCallFrameInfo.offsetOfFrameVptr);
    regTracker.rsTrackRegTrash(REG_PINVOKE_SCRATCH);

    /* now "push" our N/direct frame */

    getEmitter()->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_PINVOKE_SCRATCH, regTCB,
                               pInfo->offsetOfThreadFrame);

    return initRegs;
}

/*****************************************************************************
 *  Unchain the InlinedCallFrame.
 *  Technically, this is not part of the epilog; it is called when we are generating code for a GT_RETURN node
 *  or tail call.
 */
void CodeGen::genPInvokeMethodEpilog()
{
    if (compiler->opts.ShouldUsePInvokeHelpers())
        return;

    noway_assert(compiler->info.compCallUnmanaged);
    noway_assert(!compiler->opts.ShouldUsePInvokeHelpers());
    noway_assert(compiler->compCurBB == compiler->genReturnBB ||
                 (compiler->compTailCallUsed && (compiler->compCurBB->bbJumpKind == BBJ_THROW)) ||
                 (compiler->compJmpOpUsed && (compiler->compCurBB->bbFlags & BBF_HAS_JMP)));

    CORINFO_EE_INFO* pInfo = compiler->eeGetEEInfo();
    noway_assert(compiler->lvaInlinedPInvokeFrameVar != BAD_VAR_NUM);

    getEmitter()->emitDisableRandomNops();
    // debug check to make sure that we're not using ESI and/or EDI across this call, except for
    // compLvFrameListRoot.
    unsigned regTrashCheck = 0;

    /* XXX Tue 5/29/2007
     * We explicitly add interference for these in CodeGen::rgPredictRegUse.  If you change the code
     * sequence or registers used, make sure to update the interference for compiler->genReturnLocal.
     */
    LclVarDsc* varDsc = &compiler->lvaTable[compiler->info.compLvFrameListRoot];
    regNumber  reg;
    regNumber  reg2 = REG_PINVOKE_FRAME;

    //
    // Two cases for epilog invocation:
    //
    // 1. Return
    //    We can trash the ESI/EDI registers.
    //
    // 2. Tail call
    //    When tail called, we'd like to preserve enregistered args,
    //    in ESI/EDI so we can pass it to the callee.
    //
    // For ARM, don't modify SP for storing and restoring the TCB/frame registers.
    // Instead use the reserved local variable slot.
    //
    if (compiler->compCurBB->bbFlags & BBF_HAS_JMP)
    {
        if (compiler->rpMaskPInvokeEpilogIntf & RBM_PINVOKE_TCB)
        {
#if FEATURE_FIXED_OUT_ARGS
            // Save the register in the reserved local var slot.
            getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_PINVOKE_TCB,
                                      compiler->lvaPInvokeFrameRegSaveVar, 0);
#else
            inst_RV(INS_push, REG_PINVOKE_TCB, TYP_I_IMPL);
#endif
        }
        if (compiler->rpMaskPInvokeEpilogIntf & RBM_PINVOKE_FRAME)
        {
#if FEATURE_FIXED_OUT_ARGS
            // Save the register in the reserved local var slot.
            getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_PINVOKE_FRAME,
                                      compiler->lvaPInvokeFrameRegSaveVar, REGSIZE_BYTES);
#else
            inst_RV(INS_push, REG_PINVOKE_FRAME, TYP_I_IMPL);
#endif
        }
    }

    if (varDsc->lvRegister)
    {
        reg = varDsc->lvRegNum;
        if (reg == reg2)
            reg2 = REG_PINVOKE_TCB;

        regTrashCheck |= genRegMask(reg2);
    }
    else
    {
        /* mov esi, [tcb address]    */

        getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_PINVOKE_TCB, compiler->info.compLvFrameListRoot,
                                  0);
        regTracker.rsTrackRegTrash(REG_PINVOKE_TCB);
        reg = REG_PINVOKE_TCB;

        regTrashCheck = RBM_PINVOKE_TCB | RBM_PINVOKE_FRAME;
    }

    /* mov edi, [ebp-frame.next] */

    getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, reg2, compiler->lvaInlinedPInvokeFrameVar,
                              pInfo->inlinedCallFrameInfo.offsetOfFrameLink);
    regTracker.rsTrackRegTrash(reg2);

    /* mov [esi+offsetOfThreadFrame], edi */

    getEmitter()->emitIns_AR_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, reg2, reg, pInfo->offsetOfThreadFrame);

    noway_assert(!(regSet.rsMaskUsed & regTrashCheck));

    if (compiler->genReturnLocal != BAD_VAR_NUM && compiler->lvaTable[compiler->genReturnLocal].lvTracked &&
        compiler->lvaTable[compiler->genReturnLocal].lvRegister)
    {
        // really make sure we're not clobbering compiler->genReturnLocal.
        noway_assert(
            !(genRegMask(compiler->lvaTable[compiler->genReturnLocal].lvRegNum) &
              ((varDsc->lvRegister ? genRegMask(varDsc->lvRegNum) : 0) | RBM_PINVOKE_TCB | RBM_PINVOKE_FRAME)));
    }

    (void)regTrashCheck;

    // Restore the registers ESI and EDI.
    if (compiler->compCurBB->bbFlags & BBF_HAS_JMP)
    {
        if (compiler->rpMaskPInvokeEpilogIntf & RBM_PINVOKE_FRAME)
        {
#if FEATURE_FIXED_OUT_ARGS
            // Restore the register from the reserved local var slot.
            getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_PINVOKE_FRAME,
                                      compiler->lvaPInvokeFrameRegSaveVar, REGSIZE_BYTES);
#else
            inst_RV(INS_pop, REG_PINVOKE_FRAME, TYP_I_IMPL);
#endif
            regTracker.rsTrackRegTrash(REG_PINVOKE_FRAME);
        }
        if (compiler->rpMaskPInvokeEpilogIntf & RBM_PINVOKE_TCB)
        {
#if FEATURE_FIXED_OUT_ARGS
            // Restore the register from the reserved local var slot.
            getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_PINVOKE_TCB,
                                      compiler->lvaPInvokeFrameRegSaveVar, 0);
#else
            inst_RV(INS_pop, REG_PINVOKE_TCB, TYP_I_IMPL);
#endif
            regTracker.rsTrackRegTrash(REG_PINVOKE_TCB);
        }
    }
    getEmitter()->emitEnableRandomNops();
}

/*****************************************************************************
    This function emits the call-site prolog for direct calls to unmanaged code.
    It does all the necessary setup of the InlinedCallFrame.
    frameListRoot specifies the local containing the thread control block.
    argSize or methodToken is the value to be copied into the m_datum
            field of the frame (methodToken may be indirected & have a reloc)
    The function returns  the register now containing the thread control block,
    (it could be either enregistered or loaded into one of the scratch registers)
*/

regNumber CodeGen::genPInvokeCallProlog(LclVarDsc*            frameListRoot,
                                        int                   argSize,
                                        CORINFO_METHOD_HANDLE methodToken,
                                        BasicBlock*           returnLabel)
{
    // Some stack locals might be 'cached' in registers, we need to trash them
    // from the regTracker *and* also ensure the gc tracker does not consider
    // them live (see the next assert).  However, they might be live reg vars
    // that are non-pointers CSE'd from pointers.
    // That means the register will be live in rsMaskVars, so we can't just
    // call gcMarkSetNpt().
    {
        regMaskTP deadRegs = regTracker.rsTrashRegsForGCInterruptability() & ~RBM_ARG_REGS;
        gcInfo.gcRegGCrefSetCur &= ~deadRegs;
        gcInfo.gcRegByrefSetCur &= ~deadRegs;

#ifdef DEBUG
        deadRegs &= regSet.rsMaskVars;
        if (deadRegs)
        {
            for (LclVarDsc* varDsc = compiler->lvaTable;
                 ((varDsc < (compiler->lvaTable + compiler->lvaCount)) && deadRegs); varDsc++)
            {
                if (!varDsc->lvTracked || !varDsc->lvRegister)
                    continue;

                if (!VarSetOps::IsMember(compiler, compiler->compCurLife, varDsc->lvVarIndex))
                    continue;

                regMaskTP varRegMask = genRegMask(varDsc->lvRegNum);
                if (isRegPairType(varDsc->lvType) && varDsc->lvOtherReg != REG_STK)
                    varRegMask |= genRegMask(varDsc->lvOtherReg);

                if (varRegMask & deadRegs)
                {
                    // We found the enregistered var that should not be live if it
                    // was a GC pointer.
                    noway_assert(!varTypeIsGC(varDsc));
                    deadRegs &= ~varRegMask;
                }
            }
        }
#endif // DEBUG
    }

    /* Since we are using the InlinedCallFrame, we should have spilled all
       GC pointers to it - even from callee-saved registers */

    noway_assert(((gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur) & ~RBM_ARG_REGS) == 0);

    /* must specify only one of these parameters */
    noway_assert((argSize == 0) || (methodToken == NULL));

    /* We are about to call unmanaged code directly.
       Before we can do that we have to emit the following sequence:

       mov  dword ptr [frame.callTarget], MethodToken
       mov  dword ptr [frame.callSiteTracker], esp
       mov  reg, dword ptr [tcb_address]
       mov  byte  ptr [tcb+offsetOfGcState], 0

     */

    CORINFO_EE_INFO* pInfo = compiler->eeGetEEInfo();

    noway_assert(compiler->lvaInlinedPInvokeFrameVar != BAD_VAR_NUM);

#ifdef _TARGET_ARM_
    if (compiler->opts.ShouldUsePInvokeHelpers())
    {
        regNumber baseReg;
        int       adr = compiler->lvaFrameAddress(compiler->lvaInlinedPInvokeFrameVar, true, &baseReg, 0);

        getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_ARG_0, baseReg, adr);
        genEmitHelperCall(CORINFO_HELP_JIT_PINVOKE_BEGIN,
                          0,           // argSize
                          EA_UNKNOWN); // retSize
        regTracker.rsTrackRegTrash(REG_ARG_0);
        return REG_ARG_0;
    }
#endif

    /* mov   dword ptr [frame.callSiteTarget], value */

    if (methodToken == NULL)
    {
        /* mov   dword ptr [frame.callSiteTarget], argSize */
        instGen_Store_Imm_Into_Lcl(TYP_INT, EA_4BYTE, argSize, compiler->lvaInlinedPInvokeFrameVar,
                                   pInfo->inlinedCallFrameInfo.offsetOfCallTarget);
    }
    else
    {
        void *embedMethHnd, *pEmbedMethHnd;

        embedMethHnd = (void*)compiler->info.compCompHnd->embedMethodHandle(methodToken, &pEmbedMethHnd);

        noway_assert((!embedMethHnd) != (!pEmbedMethHnd));

        if (embedMethHnd != NULL)
        {
            /* mov   dword ptr [frame.callSiteTarget], "MethodDesc" */

            instGen_Store_Imm_Into_Lcl(TYP_I_IMPL, EA_HANDLE_CNS_RELOC, (ssize_t)embedMethHnd,
                                       compiler->lvaInlinedPInvokeFrameVar,
                                       pInfo->inlinedCallFrameInfo.offsetOfCallTarget);
        }
        else
        {
            /* mov   reg, dword ptr [MethodDescIndir]
               mov   dword ptr [frame.callSiteTarget], reg */

            regNumber reg = regSet.rsPickFreeReg();

#if CPU_LOAD_STORE_ARCH
            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, reg, (ssize_t)pEmbedMethHnd);
            getEmitter()->emitIns_R_AR(ins_Load(TYP_I_IMPL), EA_PTRSIZE, reg, reg, 0);
#else  // !CPU_LOAD_STORE_ARCH
            getEmitter()->emitIns_R_AI(ins_Load(TYP_I_IMPL), EA_PTR_DSP_RELOC, reg, (ssize_t)pEmbedMethHnd);
#endif // !CPU_LOAD_STORE_ARCH
            regTracker.rsTrackRegTrash(reg);
            getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, reg, compiler->lvaInlinedPInvokeFrameVar,
                                      pInfo->inlinedCallFrameInfo.offsetOfCallTarget);
        }
    }

    regNumber tcbReg = REG_NA;

    if (frameListRoot->lvRegister)
    {
        tcbReg = frameListRoot->lvRegNum;
    }
    else
    {
        tcbReg = regSet.rsGrabReg(RBM_ALLINT);

        /* mov reg, dword ptr [tcb address]    */

        getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, tcbReg,
                                  (unsigned)(frameListRoot - compiler->lvaTable), 0);
        regTracker.rsTrackRegTrash(tcbReg);
    }

#ifdef _TARGET_X86_
    /* mov   dword ptr [frame.callSiteTracker], esp */

    getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_SPBASE, compiler->lvaInlinedPInvokeFrameVar,
                              pInfo->inlinedCallFrameInfo.offsetOfCallSiteSP);
#endif // _TARGET_X86_

#if CPU_LOAD_STORE_ARCH
    regNumber tmpReg = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(tcbReg));
    getEmitter()->emitIns_R_L(INS_adr, EA_PTRSIZE, returnLabel, tmpReg);
    regTracker.rsTrackRegTrash(tmpReg);
    getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, tmpReg, compiler->lvaInlinedPInvokeFrameVar,
                              pInfo->inlinedCallFrameInfo.offsetOfReturnAddress);
#else  // !CPU_LOAD_STORE_ARCH
    /* mov   dword ptr [frame.callSiteReturnAddress], label */

    getEmitter()->emitIns_J_S(ins_Store(TYP_I_IMPL), EA_PTRSIZE, returnLabel, compiler->lvaInlinedPInvokeFrameVar,
                              pInfo->inlinedCallFrameInfo.offsetOfReturnAddress);
#endif // !CPU_LOAD_STORE_ARCH

#if CPU_LOAD_STORE_ARCH
    instGen_Set_Reg_To_Zero(EA_1BYTE, tmpReg);

    noway_assert(tmpReg != tcbReg);

    getEmitter()->emitIns_AR_R(ins_Store(TYP_BYTE), EA_1BYTE, tmpReg, tcbReg, pInfo->offsetOfGCState);
#else  // !CPU_LOAD_STORE_ARCH
    /* mov   byte  ptr [tcbReg+offsetOfGcState], 0 */

    getEmitter()->emitIns_I_AR(ins_Store(TYP_BYTE), EA_1BYTE, 0, tcbReg, pInfo->offsetOfGCState);
#endif // !CPU_LOAD_STORE_ARCH

    return tcbReg;
}

/*****************************************************************************
 *
   First we have to mark in the hoisted NDirect stub that we are back
   in managed code. Then we have to check (a global flag) whether GC is
   pending or not. If so, we just call into a jit-helper.
   Right now we have this call always inlined, i.e. we always skip around
   the jit-helper call.
   Note:
   The tcb address is a regular local (initialized in the prolog), so it is either
   enregistered or in the frame:

        tcb_reg = [tcb_address is enregistered] OR [mov ecx, tcb_address]
        mov  byte ptr[tcb_reg+offsetOfGcState], 1
        cmp  'global GC pending flag', 0
        je   @f
        [mov  ECX, tcb_reg]  OR [ecx was setup above]     ; we pass the tcb value to callGC
        [mov  [EBP+spill_area+0], eax]                    ; spill the int  return value if any
        [mov  [EBP+spill_area+4], edx]                    ; spill the long return value if any
        call @callGC
        [mov  eax, [EBP+spill_area+0] ]                   ; reload the int  return value if any
        [mov  edx, [EBP+spill_area+4] ]                   ; reload the long return value if any
    @f:
 */

void CodeGen::genPInvokeCallEpilog(LclVarDsc* frameListRoot, regMaskTP retVal)
{
#ifdef _TARGET_ARM_
    if (compiler->opts.ShouldUsePInvokeHelpers())
    {
        noway_assert(compiler->lvaInlinedPInvokeFrameVar != BAD_VAR_NUM);

        regNumber baseReg;
        int       adr = compiler->lvaFrameAddress(compiler->lvaInlinedPInvokeFrameVar, true, &baseReg, 0);

        getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_ARG_0, baseReg, adr);
        genEmitHelperCall(CORINFO_HELP_JIT_PINVOKE_END,
                          0,           // argSize
                          EA_UNKNOWN); // retSize
        regTracker.rsTrackRegTrash(REG_ARG_0);
        return;
    }
#endif

    BasicBlock*      clab_nostop;
    CORINFO_EE_INFO* pInfo = compiler->eeGetEEInfo();
    regNumber        reg2;
    regNumber        reg3;

#ifdef _TARGET_ARM_
    reg3 = REG_R3;
#else
    reg3     = REG_EDX;
#endif

    getEmitter()->emitDisableRandomNops();

    if (frameListRoot->lvRegister)
    {
        /* make sure that register is live across the call */

        reg2 = frameListRoot->lvRegNum;
        noway_assert(genRegMask(reg2) & RBM_INT_CALLEE_SAVED);
    }
    else
    {
        /* mov   reg2, dword ptr [tcb address]    */
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_ARM_
        reg2 = REG_R2;
#else
        reg2 = REG_ECX;
#endif

        getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, reg2,
                                  (unsigned)(frameListRoot - compiler->lvaTable), 0);
        regTracker.rsTrackRegTrash(reg2);
    }

#ifdef _TARGET_ARM_
    /* mov   r3, 1 */
    /* strb  [r2+offsetOfGcState], r3 */
    instGen_Set_Reg_To_Imm(EA_PTRSIZE, reg3, 1);
    getEmitter()->emitIns_AR_R(ins_Store(TYP_BYTE), EA_1BYTE, reg3, reg2, pInfo->offsetOfGCState);
#else
    /* mov   byte ptr [tcb+offsetOfGcState], 1 */
    getEmitter()->emitIns_I_AR(ins_Store(TYP_BYTE), EA_1BYTE, 1, reg2, pInfo->offsetOfGCState);
#endif

    /* test global flag (we return to managed code) */

    LONG *addrOfCaptureThreadGlobal, **pAddrOfCaptureThreadGlobal;

    addrOfCaptureThreadGlobal =
        compiler->info.compCompHnd->getAddrOfCaptureThreadGlobal((void**)&pAddrOfCaptureThreadGlobal);
    noway_assert((!addrOfCaptureThreadGlobal) != (!pAddrOfCaptureThreadGlobal));

    // Can we directly use addrOfCaptureThreadGlobal?

    if (addrOfCaptureThreadGlobal)
    {
#ifdef _TARGET_ARM_
        instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, reg3, (ssize_t)addrOfCaptureThreadGlobal);
        getEmitter()->emitIns_R_R_I(ins_Load(TYP_INT), EA_4BYTE, reg3, reg3, 0);
        regTracker.rsTrackRegTrash(reg3);
        getEmitter()->emitIns_R_I(INS_cmp, EA_4BYTE, reg3, 0);
#else
        getEmitter()->emitIns_C_I(INS_cmp, EA_PTR_DSP_RELOC, FLD_GLOBAL_DS, (ssize_t)addrOfCaptureThreadGlobal, 0);
#endif
    }
    else
    {
#ifdef _TARGET_ARM_
        instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, reg3, (ssize_t)pAddrOfCaptureThreadGlobal);
        getEmitter()->emitIns_R_R_I(ins_Load(TYP_INT), EA_4BYTE, reg3, reg3, 0);
        regTracker.rsTrackRegTrash(reg3);
        getEmitter()->emitIns_R_R_I(ins_Load(TYP_INT), EA_4BYTE, reg3, reg3, 0);
        getEmitter()->emitIns_R_I(INS_cmp, EA_4BYTE, reg3, 0);
#else // !_TARGET_ARM_

        getEmitter()->emitIns_R_AI(ins_Load(TYP_I_IMPL), EA_PTR_DSP_RELOC, REG_ECX,
                                   (ssize_t)pAddrOfCaptureThreadGlobal);
        regTracker.rsTrackRegTrash(REG_ECX);

        getEmitter()->emitIns_I_AR(INS_cmp, EA_4BYTE, 0, REG_ECX, 0);

#endif // !_TARGET_ARM_
    }

    /* */
    clab_nostop = genCreateTempLabel();

    /* Generate the conditional jump */
    emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
    inst_JMP(jmpEqual, clab_nostop);

#ifdef _TARGET_ARM_
// The helper preserves the return value on ARM
#else
    /* save return value (if necessary) */
    if (retVal != RBM_NONE)
    {
        if (retVal == RBM_INTRET || retVal == RBM_LNGRET)
        {
            /* push eax */

            inst_RV(INS_push, REG_INTRET, TYP_INT);

            if (retVal == RBM_LNGRET)
            {
                /* push edx */

                inst_RV(INS_push, REG_EDX, TYP_INT);
            }
        }
    }
#endif

    /* emit the call to the EE-helper that stops for GC (or other reasons) */

    genEmitHelperCall(CORINFO_HELP_STOP_FOR_GC, 0, /* argSize */
                      EA_UNKNOWN);                 /* retSize */

#ifdef _TARGET_ARM_
// The helper preserves the return value on ARM
#else
    /* restore return value (if necessary) */

    if (retVal != RBM_NONE)
    {
        if (retVal == RBM_INTRET || retVal == RBM_LNGRET)
        {
            if (retVal == RBM_LNGRET)
            {
                /* pop edx */

                inst_RV(INS_pop, REG_EDX, TYP_INT);
                regTracker.rsTrackRegTrash(REG_EDX);
            }

            /* pop eax */

            inst_RV(INS_pop, REG_INTRET, TYP_INT);
            regTracker.rsTrackRegTrash(REG_INTRET);
        }
    }
#endif

    /* genCondJump() closes the current emitter block */

    genDefineTempLabel(clab_nostop);

    // This marks the InlinedCallFrame as "inactive".  In fully interruptible code, this is not atomic with
    // the above code.  So the process is:
    // 1) Return to cooperative mode
    // 2) Check to see if we need to stop for GC
    // 3) Return from the p/invoke (as far as the stack walker is concerned).

    /* mov  dword ptr [frame.callSiteTracker], 0 */

    instGen_Store_Imm_Into_Lcl(TYP_I_IMPL, EA_PTRSIZE, 0, compiler->lvaInlinedPInvokeFrameVar,
                               pInfo->inlinedCallFrameInfo.offsetOfReturnAddress);

    getEmitter()->emitEnableRandomNops();
}

/*****************************************************************************/

/*****************************************************************************
*           TRACKING OF FLAGS
*****************************************************************************/

void CodeGen::genFlagsEqualToNone()
{
    genFlagsEqReg = REG_NA;
    genFlagsEqVar = (unsigned)-1;
    genFlagsEqLoc.Init();
}

/*****************************************************************************
 *
 *  Record the fact that the flags register has a value that reflects the
 *  contents of the given register.
 */

void CodeGen::genFlagsEqualToReg(GenTreePtr tree, regNumber reg)
{
    genFlagsEqLoc.CaptureLocation(getEmitter());
    genFlagsEqReg = reg;

    /* previous setting of flags by a var becomes invalid */

    genFlagsEqVar = 0xFFFFFFFF;

    /* Set appropriate flags on the tree */

    if (tree)
    {
        tree->gtFlags |= GTF_ZSF_SET;
        assert(tree->gtSetFlags());
    }
}

/*****************************************************************************
 *
 *  Record the fact that the flags register has a value that reflects the
 *  contents of the given local variable.
 */

void CodeGen::genFlagsEqualToVar(GenTreePtr tree, unsigned var)
{
    genFlagsEqLoc.CaptureLocation(getEmitter());
    genFlagsEqVar = var;

    /* previous setting of flags by a register becomes invalid */

    genFlagsEqReg = REG_NA;

    /* Set appropriate flags on the tree */

    if (tree)
    {
        tree->gtFlags |= GTF_ZSF_SET;
        assert(tree->gtSetFlags());
    }
}

/*****************************************************************************
 *
 *  Return an indication of whether the flags register is set to the current
 *  value of the given register/variable. The return value is as follows:
 *
 *      false  ..  nothing
 *      true   ..  the zero flag (ZF) and sign flag (SF) is set
 */

bool CodeGen::genFlagsAreReg(regNumber reg)
{
    if ((genFlagsEqReg == reg) && genFlagsEqLoc.IsCurrentLocation(getEmitter()))
    {
        return true;
    }

    return false;
}

bool CodeGen::genFlagsAreVar(unsigned var)
{
    if ((genFlagsEqVar == var) && genFlagsEqLoc.IsCurrentLocation(getEmitter()))
    {
        return true;
    }

    return false;
}

/*****************************************************************************
 * This utility function returns true iff the execution path from "from"
 * (inclusive) to "to" (exclusive) contains a death of the given var
 */
bool CodeGen::genContainsVarDeath(GenTreePtr from, GenTreePtr to, unsigned varNum)
{
    GenTreePtr tree;
    for (tree = from; tree != NULL && tree != to; tree = tree->gtNext)
    {
        if (tree->IsLocal() && (tree->gtFlags & GTF_VAR_DEATH))
        {
            unsigned dyingVarNum = tree->gtLclVarCommon.gtLclNum;
            if (dyingVarNum == varNum)
                return true;
            LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);
            if (varDsc->lvPromoted)
            {
                assert(varDsc->lvType == TYP_STRUCT);
                unsigned firstFieldNum = varDsc->lvFieldLclStart;
                if (varNum >= firstFieldNum && varNum < firstFieldNum + varDsc->lvFieldCnt)
                {
                    return true;
                }
            }
        }
    }
    assert(tree != NULL);
    return false;
}

#endif // LEGACY_BACKEND
