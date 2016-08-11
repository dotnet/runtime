// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef LEGACY_BACKEND // This file is NOT used for the RyuJIT backend that uses the linear scan register allocator.

#include "compiler.h"
#include "emit.h"
#include "codegen.h"

#ifndef _TARGET_ARM_
#error "Non-ARM target for registerfp.cpp"
#endif // !_TARGET_ARM_

// get the next argument register which is aligned to 'alignment' # of bytes
regNumber alignFloatArgReg(regNumber argReg, int alignment)
{
    assert(isValidFloatArgReg(argReg));

    int regsize_alignment = alignment /= REGSIZE_BYTES;
    if (genMapFloatRegNumToRegArgNum(argReg) % regsize_alignment)
        argReg = genRegArgNext(argReg);

    // technically the above should be a 'while' so make sure
    // we never should have incremented more than once
    assert(!(genMapFloatRegNumToRegArgNum(argReg) % regsize_alignment));

    return argReg;
}

// Instruction list
// N=normal, R=reverse, P=pop

void CodeGen::genFloatConst(GenTree* tree, RegSet::RegisterPreference* pref)
{
    assert(tree->gtOper == GT_CNS_DBL);
    var_types type       = tree->gtType;
    double    constValue = tree->gtDblCon.gtDconVal;
    size_t*   cv         = (size_t*)&constValue;

    regNumber dst = regSet.PickRegFloat(type, pref);

    if (type == TYP_FLOAT)
    {
        regNumber reg = regSet.rsPickReg();

        float f = forceCastToFloat(constValue);
        genSetRegToIcon(reg, *((int*)(&f)));
        getEmitter()->emitIns_R_R(INS_vmov_i2f, EA_4BYTE, dst, reg);
    }
    else
    {
        assert(type == TYP_DOUBLE);
        regNumber reg1 = regSet.rsPickReg();
        regNumber reg2 = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(reg1));

        genSetRegToIcon(reg1, cv[0]);
        regSet.rsLockReg(genRegMask(reg1));
        genSetRegToIcon(reg2, cv[1]);
        regSet.rsUnlockReg(genRegMask(reg1));

        getEmitter()->emitIns_R_R_R(INS_vmov_i2d, EA_8BYTE, dst, reg1, reg2);
    }
    genMarkTreeInReg(tree, dst);

    return;
}

void CodeGen::genFloatMath(GenTree* tree, RegSet::RegisterPreference* pref)
{
    assert(tree->OperGet() == GT_INTRINSIC);

    GenTreePtr op1 = tree->gtOp.gtOp1;

    // get tree into a register
    genCodeForTreeFloat(op1, pref);

    instruction ins;

    switch (tree->gtIntrinsic.gtIntrinsicId)
    {
        case CORINFO_INTRINSIC_Sin:
            ins = INS_invalid;
            break;
        case CORINFO_INTRINSIC_Cos:
            ins = INS_invalid;
            break;
        case CORINFO_INTRINSIC_Sqrt:
            ins = INS_vsqrt;
            break;
        case CORINFO_INTRINSIC_Abs:
            ins = INS_vabs;
            break;
        case CORINFO_INTRINSIC_Round:
        {
            regNumber reg = regSet.PickRegFloat(tree->TypeGet(), pref);
            genMarkTreeInReg(tree, reg);
            // convert it to a long and back
            inst_RV_RV(ins_FloatConv(TYP_LONG, tree->TypeGet()), reg, op1->gtRegNum, tree->TypeGet());
            inst_RV_RV(ins_FloatConv(tree->TypeGet(), TYP_LONG), reg, reg);
            genCodeForTreeFloat_DONE(tree, op1->gtRegNum);
            return;
        }
        break;
        default:
            unreached();
    }

    if (ins != INS_invalid)
    {
        regNumber reg = regSet.PickRegFloat(tree->TypeGet(), pref);
        genMarkTreeInReg(tree, reg);
        inst_RV_RV(ins, reg, op1->gtRegNum, tree->TypeGet());
        // mark register that holds tree
        genCodeForTreeFloat_DONE(tree, reg);
    }
    else
    {
        unreached();
        // If unreached is removed, mark register that holds tree
        // genCodeForTreeFloat_DONE(tree, op1->gtRegNum);
    }

    return;
}

void CodeGen::genFloatSimple(GenTree* tree, RegSet::RegisterPreference* pref)
{
    assert(tree->OperKind() & GTK_SMPOP);
    var_types type = tree->TypeGet();

    RegSet::RegisterPreference defaultPref(RBM_ALLFLOAT, RBM_NONE);
    if (pref == NULL)
    {
        pref = &defaultPref;
    }

    switch (tree->OperGet())
    {
        // Assignment
        case GT_ASG:
        {
            genFloatAssign(tree);
            break;
        }

        // Arithmetic binops
        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
        case GT_DIV:
        {
            genFloatArith(tree, pref);
            break;
        }

        case GT_NEG:
        {
            GenTreePtr op1 = tree->gtOp.gtOp1;

            // get the tree into a register
            genCodeForTreeFloat(op1, pref);

            // change the sign
            regNumber reg = regSet.PickRegFloat(type, pref);
            genMarkTreeInReg(tree, reg);
            inst_RV_RV(ins_MathOp(tree->OperGet(), type), reg, op1->gtRegNum, type);

            // mark register that holds tree
            genCodeForTreeFloat_DONE(tree, reg);
            return;
        }

        case GT_IND:
        {
            regMaskTP addrReg;

            // Make sure the address value is 'addressable' */
            addrReg = genMakeAddressable(tree, 0, RegSet::FREE_REG);

            // Load the value onto the FP stack
            regNumber reg = regSet.PickRegFloat(type, pref);
            genLoadFloat(tree, reg);

            genDoneAddressable(tree, addrReg, RegSet::FREE_REG);

            genCodeForTreeFloat_DONE(tree, reg);

            break;
        }
        case GT_CAST:
        {
            genCodeForTreeCastFloat(tree, pref);
            break;
        }

        // Asg-Arithmetic ops
        case GT_ASG_ADD:
        case GT_ASG_SUB:
        case GT_ASG_MUL:
        case GT_ASG_DIV:
        {
            genFloatAsgArith(tree);
            break;
        }
        case GT_INTRINSIC:
            genFloatMath(tree, pref);
            break;

        case GT_RETURN:
        {
            GenTreePtr op1 = tree->gtOp.gtOp1;
            assert(op1);

            pref->best = (type == TYP_DOUBLE) ? RBM_DOUBLERET : RBM_FLOATRET;

            // Compute the result
            genCodeForTreeFloat(op1, pref);

            inst_RV_TT(ins_FloatConv(tree->TypeGet(), op1->TypeGet()), REG_FLOATRET, op1);
            if (compiler->info.compIsVarArgs || compiler->opts.compUseSoftFP)
            {
                if (tree->TypeGet() == TYP_FLOAT)
                {
                    inst_RV_RV(INS_vmov_f2i, REG_INTRET, REG_FLOATRET, TYP_FLOAT, EA_4BYTE);
                }
                else
                {
                    assert(tree->TypeGet() == TYP_DOUBLE);
                    inst_RV_RV_RV(INS_vmov_d2i, REG_INTRET, REG_NEXT(REG_INTRET), REG_FLOATRET, EA_8BYTE);
                }
            }
            break;
        }
        case GT_ARGPLACE:
            break;

        case GT_COMMA:
        {
            GenTreePtr op1 = tree->gtOp.gtOp1;
            GenTreePtr op2 = tree->gtGetOp2();

            if (tree->gtFlags & GTF_REVERSE_OPS)
            {
                genCodeForTreeFloat(op2, pref);

                regSet.SetUsedRegFloat(op2, true);
                genEvalSideEffects(op1);
                regSet.SetUsedRegFloat(op2, false);
            }
            else
            {
                genEvalSideEffects(op1);
                genCodeForTreeFloat(op2, pref);
            }

            genCodeForTreeFloat_DONE(tree, op2->gtRegNum);
            break;
        }

        case GT_CKFINITE:
            genFloatCheckFinite(tree, pref);
            break;

        default:
            NYI("Unhandled register FP codegen");
    }
}

// generate code for ckfinite tree/instruction
void CodeGen::genFloatCheckFinite(GenTree* tree, RegSet::RegisterPreference* pref)
{
    TempDsc* temp;
    int      offs;

    GenTreePtr op1 = tree->gtOp.gtOp1;

    // Offset of the DWord containing the exponent
    offs = (op1->gtType == TYP_FLOAT) ? 0 : sizeof(int);

    // get tree into a register
    genCodeForTreeFloat(op1, pref);

    regNumber reg = regSet.rsPickReg();

    int expMask;
    if (op1->gtType == TYP_FLOAT)
    {
        getEmitter()->emitIns_R_R(INS_vmov_f2i, EA_4BYTE, reg, op1->gtRegNum);
        expMask = 0x7F800000;
    }
    else // double
    {
        assert(op1->gtType == TYP_DOUBLE);
        getEmitter()->emitIns_R_R(INS_vmov_f2i, EA_4BYTE, reg,
                                  REG_NEXT(op1->gtRegNum)); // the high 32 bits of the double register
        expMask = 0x7FF00000;
    }
    regTracker.rsTrackRegTrash(reg);

    // Check if the exponent is all ones
    inst_RV_IV(INS_and, reg, expMask, EA_4BYTE);
    inst_RV_IV(INS_cmp, reg, expMask, EA_4BYTE);

    // If exponent was all 1's, we need to throw ArithExcep
    emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
    genJumpToThrowHlpBlk(jmpEqual, SCK_ARITH_EXCPN);

    genCodeForTreeFloat_DONE(tree, op1->gtRegNum);
}

void CodeGen::genFloatAssign(GenTree* tree)
{
    var_types  type = tree->TypeGet();
    GenTreePtr op1  = tree->gtGetOp1();
    GenTreePtr op2  = tree->gtGetOp2();

    regMaskTP needRegOp1 = RBM_ALLINT;
    regMaskTP addrReg    = RBM_NONE;
    bool      volat      = false; // Is this a volatile store
    bool      unaligned  = false; // Is this an unaligned store
    regNumber op2reg     = REG_NA;

#ifdef DEBUGGING_SUPPORT
    unsigned lclVarNum = compiler->lvaCount;
    unsigned lclILoffs = DUMMY_INIT(0);
#endif

    noway_assert(tree->OperGet() == GT_ASG);

    // Is the target a floating-point local variable?
    //  possibly even an enregistered floating-point local variable?
    //
    switch (op1->gtOper)
    {
        unsigned   varNum;
        LclVarDsc* varDsc;

        case GT_LCL_FLD:
            // Check for a misalignment on a Floating Point field
            //
            if (varTypeIsFloating(op1->TypeGet()))
            {
                if ((op1->gtLclFld.gtLclOffs % emitTypeSize(op1->TypeGet())) != 0)
                {
                    unaligned = true;
                }
            }
            break;

        case GT_LCL_VAR:
            varNum = op1->gtLclVarCommon.gtLclNum;
            noway_assert(varNum < compiler->lvaCount);
            varDsc = compiler->lvaTable + varNum;

#ifdef DEBUGGING_SUPPORT
            // For non-debuggable code, every definition of a lcl-var has
            // to be checked to see if we need to open a new scope for it.
            // Remember the local var info to call siCheckVarScope
            // AFTER code generation of the assignment.
            //
            if (compiler->opts.compScopeInfo && !compiler->opts.compDbgCode && (compiler->info.compVarScopesCount > 0))
            {
                lclVarNum = varNum;
                lclILoffs = op1->gtLclVar.gtLclILoffs;
            }
#endif

            // Dead Store assert (with min opts we may have dead stores)
            //
            noway_assert(!varDsc->lvTracked || compiler->opts.MinOpts() || !(op1->gtFlags & GTF_VAR_DEATH));

            // Does this variable live in a register?
            //
            if (genMarkLclVar(op1))
            {
                noway_assert(!compiler->opts.compDbgCode); // We don't enregister any floats with debug codegen

                // Get hold of the target register
                //
                regNumber op1Reg = op1->gtRegVar.gtRegNum;

                // the variable being assigned should be dead in op2
                assert(!varDsc->lvTracked ||
                       !VarSetOps::IsMember(compiler, genUpdateLiveSetForward(op2), varDsc->lvVarIndex));

                // Setup register preferencing, so that we try to target the op1 enregistered variable
                //
                regMaskTP bestMask = genRegMask(op1Reg);
                if (type == TYP_DOUBLE)
                {
                    assert((bestMask & RBM_DBL_REGS) != 0);
                    bestMask |= genRegMask(REG_NEXT(op1Reg));
                }
                RegSet::RegisterPreference pref(RBM_ALLFLOAT, bestMask);

                // Evaluate op2 into a floating point register
                //
                genCodeForTreeFloat(op2, &pref);

                noway_assert(op2->gtFlags & GTF_REG_VAL);

                // Make sure the value ends up in the right place ...
                // For example if op2 is a call that returns a result
                // in REG_F0, we will need to do a move instruction here
                //
                if ((op2->gtRegNum != op1Reg) || (op2->TypeGet() != type))
                {
                    regMaskTP spillRegs = regSet.rsMaskUsed & genRegMaskFloat(op1Reg, op1->TypeGet());
                    if (spillRegs != 0)
                        regSet.rsSpillRegs(spillRegs);

                    assert(type == op1->TypeGet());

                    inst_RV_RV(ins_FloatConv(type, op2->TypeGet()), op1Reg, op2->gtRegNum, type);
                }
                genUpdateLife(op1);
                goto DONE_ASG;
            }
            break;

        case GT_CLS_VAR:
        case GT_IND:
            // Check for a volatile/unaligned store
            //
            assert((op1->OperGet() == GT_CLS_VAR) ||
                   (op1->OperGet() == GT_IND)); // Required for GTF_IND_VOLATILE flag to be valid
            if (op1->gtFlags & GTF_IND_VOLATILE)
                volat = true;
            if (op1->gtFlags & GTF_IND_UNALIGNED)
                unaligned = true;
            break;

        default:
            break;
    }

    // Is the value being assigned an enregistered floating-point local variable?
    //
    switch (op2->gtOper)
    {
        case GT_LCL_VAR:

            if (!genMarkLclVar(op2))
                break;

            __fallthrough;

        case GT_REG_VAR:

            // We must honor the order evalauation in case op1 reassigns our op2 register
            //
            if (tree->gtFlags & GTF_REVERSE_OPS)
                break;

            // Is there an implicit conversion that we have to insert?
            // Handle this case with the normal cases below.
            //
            if (type != op2->TypeGet())
                break;

            // Make the target addressable
            //
            addrReg = genMakeAddressable(op1, needRegOp1, RegSet::KEEP_REG, true);

            noway_assert(op2->gtFlags & GTF_REG_VAL);
            noway_assert(op2->IsRegVar());

            op2reg = op2->gtRegVar.gtRegNum;
            genUpdateLife(op2);

            goto CHK_VOLAT_UNALIGN;
        default:
            break;
    }

    // Is the op2 (RHS) more complex than op1 (LHS)?
    //
    if (tree->gtFlags & GTF_REVERSE_OPS)
    {
        regMaskTP                  bestRegs = regSet.rsNarrowHint(RBM_ALLFLOAT, ~op1->gtRsvdRegs);
        RegSet::RegisterPreference pref(RBM_ALLFLOAT, bestRegs);

        // Generate op2 (RHS) into a floating point register
        //
        genCodeForTreeFloat(op2, &pref);
        regSet.SetUsedRegFloat(op2, true);

        // Make the target addressable
        //
        addrReg = genMakeAddressable(op1, needRegOp1, RegSet::KEEP_REG, true);

        genRecoverReg(op2, RBM_ALLFLOAT, RegSet::KEEP_REG);
        noway_assert(op2->gtFlags & GTF_REG_VAL);
        regSet.SetUsedRegFloat(op2, false);
    }
    else
    {
        needRegOp1 = regSet.rsNarrowHint(needRegOp1, ~op2->gtRsvdRegs);

        // Make the target addressable
        //
        addrReg = genMakeAddressable(op1, needRegOp1, RegSet::KEEP_REG, true);

        // Generate the RHS into any floating point register
        genCodeForTreeFloat(op2);
    }
    noway_assert(op2->gtFlags & GTF_REG_VAL);

    op2reg = op2->gtRegNum;

    // Is there an implicit conversion that we have to insert?
    //
    if (type != op2->TypeGet())
    {
        regMaskTP bestMask = genRegMask(op2reg);
        if (type == TYP_DOUBLE)
        {
            if (bestMask & RBM_DBL_REGS)
            {
                bestMask |= genRegMask(REG_NEXT(op2reg));
            }
            else
            {
                bestMask |= genRegMask(REG_PREV(op2reg));
            }
        }
        RegSet::RegisterPreference op2Pref(RBM_ALLFLOAT, bestMask);
        op2reg = regSet.PickRegFloat(type, &op2Pref);

        inst_RV_RV(ins_FloatConv(type, op2->TypeGet()), op2reg, op2->gtRegNum, type);
    }

    // Make sure the LHS is still addressable
    //
    addrReg = genKeepAddressable(op1, addrReg);

CHK_VOLAT_UNALIGN:

    regSet.rsLockUsedReg(addrReg); // Must prevent unaligned regSet.rsGrabReg from choosing an addrReg

    if (volat)
    {
        // Emit a memory barrier instruction before the store
        instGen_MemoryBarrier();
    }
    if (unaligned)
    {
        var_types storeType = op1->TypeGet();
        assert(storeType == TYP_DOUBLE || storeType == TYP_FLOAT);

        // Unaligned Floating-Point Stores must be done using the integer register(s)
        regNumber intRegLo    = regSet.rsGrabReg(RBM_ALLINT);
        regNumber intRegHi    = REG_NA;
        regMaskTP tmpLockMask = genRegMask(intRegLo);

        if (storeType == TYP_DOUBLE)
        {
            intRegHi = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(intRegLo));
            tmpLockMask |= genRegMask(intRegHi);
        }

        // move the FP register over to the integer register(s)
        //
        if (storeType == TYP_DOUBLE)
        {
            getEmitter()->emitIns_R_R_R(INS_vmov_d2i, EA_8BYTE, intRegLo, intRegHi, op2reg);
            regTracker.rsTrackRegTrash(intRegHi);
        }
        else
        {
            getEmitter()->emitIns_R_R(INS_vmov_f2i, EA_4BYTE, intRegLo, op2reg);
        }
        regTracker.rsTrackRegTrash(intRegLo);

        regSet.rsLockReg(tmpLockMask); // Temporarily lock the intRegs
        op1->gtType = TYP_INT;         // Temporarily change the type to TYP_INT

        inst_TT_RV(ins_Store(TYP_INT), op1, intRegLo);

        if (storeType == TYP_DOUBLE)
        {
            inst_TT_RV(ins_Store(TYP_INT), op1, intRegHi, 4);
        }

        op1->gtType = storeType;         // Change the type back to the floating point type
        regSet.rsUnlockReg(tmpLockMask); // Unlock the intRegs
    }
    else
    {
        // Move the value into the target
        //
        inst_TT_RV(ins_Store(op1->TypeGet()), op1, op2reg);
    }

    // Free up anything that was tied up by the LHS
    //
    regSet.rsUnlockUsedReg(addrReg);
    genDoneAddressable(op1, addrReg, RegSet::KEEP_REG);

DONE_ASG:

    genUpdateLife(tree);

#ifdef DEBUGGING_SUPPORT
    /* For non-debuggable code, every definition of a lcl-var has
     * to be checked to see if we need to open a new scope for it.
     */
    if (lclVarNum < compiler->lvaCount)
        siCheckVarScope(lclVarNum, lclILoffs);
#endif
}

void CodeGen::genCodeForTreeFloat(GenTreePtr tree, RegSet::RegisterPreference* pref)
{
    genTreeOps oper;
    unsigned   kind;

    assert(tree);
    assert(tree->gtOper != GT_STMT);

    // What kind of node do we have?
    oper = tree->OperGet();
    kind = tree->OperKind();

    if (kind & GTK_CONST)
    {
        genFloatConst(tree, pref);
    }
    else if (kind & GTK_LEAF)
    {
        genFloatLeaf(tree, pref);
    }
    else if (kind & GTK_SMPOP)
    {
        genFloatSimple(tree, pref);
    }
    else
    {
        assert(oper == GT_CALL);
        genCodeForCall(tree, true);
    }
}

void CodeGen::genFloatLeaf(GenTree* tree, RegSet::RegisterPreference* pref)
{
    regNumber reg = REG_NA;

    switch (tree->OperGet())
    {
        case GT_LCL_VAR:
            // Does the variable live in a register?
            //
            if (!genMarkLclVar(tree))
                goto MEM_LEAF;
            __fallthrough;

        case GT_REG_VAR:
            noway_assert(tree->gtFlags & GTF_REG_VAL);
            reg = tree->gtRegVar.gtRegNum;
            break;

        case GT_LCL_FLD:
            // We only use GT_LCL_FLD for lvAddrTaken vars, so we don't have
            // to worry about it being enregistered.
            noway_assert(compiler->lvaTable[tree->gtLclFld.gtLclNum].lvRegister == 0);
            __fallthrough;

        case GT_CLS_VAR:

        MEM_LEAF:
            reg = regSet.PickRegFloat(tree->TypeGet(), pref);
            genLoadFloat(tree, reg);
            break;

        default:
            DISPTREE(tree);
            assert(!"unexpected leaf");
    }

    genCodeForTreeFloat_DONE(tree, reg);
    return;
}

void CodeGen::genLoadFloat(GenTreePtr tree, regNumber reg)
{
    if (tree->IsRegVar())
    {
        // if it has been spilled, unspill it.%
        LclVarDsc* varDsc = &compiler->lvaTable[tree->gtLclVarCommon.gtLclNum];
        if (varDsc->lvSpilled)
        {
            UnspillFloat(varDsc);
        }

        inst_RV_RV(ins_FloatCopy(tree->TypeGet()), reg, tree->gtRegNum, tree->TypeGet());
    }
    else
    {
        bool unalignedLoad = false;
        switch (tree->OperGet())
        {
            case GT_IND:
            case GT_CLS_VAR:
                if (tree->gtFlags & GTF_IND_UNALIGNED)
                    unalignedLoad = true;
                break;
            case GT_LCL_FLD:
                // Check for a misalignment on a Floating Point field
                //
                if (varTypeIsFloating(tree->TypeGet()))
                {
                    if ((tree->gtLclFld.gtLclOffs % emitTypeSize(tree->TypeGet())) != 0)
                    {
                        unalignedLoad = true;
                    }
                }
                break;
            default:
                break;
        }

        if (unalignedLoad)
        {
            // Make the target addressable
            //
            regMaskTP addrReg = genMakeAddressable(tree, 0, RegSet::KEEP_REG, true);
            regSet.rsLockUsedReg(addrReg); // Must prevent regSet.rsGrabReg from choosing an addrReg

            var_types loadType = tree->TypeGet();
            assert(loadType == TYP_DOUBLE || loadType == TYP_FLOAT);

            // Unaligned Floating-Point Loads must be loaded into integer register(s)
            // and then moved over to the Floating-Point register
            regNumber intRegLo    = regSet.rsGrabReg(RBM_ALLINT);
            regNumber intRegHi    = REG_NA;
            regMaskTP tmpLockMask = genRegMask(intRegLo);

            if (loadType == TYP_DOUBLE)
            {
                intRegHi = regSet.rsGrabReg(RBM_ALLINT & ~genRegMask(intRegLo));
                tmpLockMask |= genRegMask(intRegHi);
            }

            regSet.rsLockReg(tmpLockMask); // Temporarily lock the intRegs
            tree->gtType = TYP_INT;        // Temporarily change the type to TYP_INT

            inst_RV_TT(ins_Load(TYP_INT), intRegLo, tree);
            regTracker.rsTrackRegTrash(intRegLo);

            if (loadType == TYP_DOUBLE)
            {
                inst_RV_TT(ins_Load(TYP_INT), intRegHi, tree, 4);
                regTracker.rsTrackRegTrash(intRegHi);
            }

            tree->gtType = loadType;         // Change the type back to the floating point type
            regSet.rsUnlockReg(tmpLockMask); // Unlock the intRegs

            // move the integer register(s) over to the FP register
            //
            if (loadType == TYP_DOUBLE)
                getEmitter()->emitIns_R_R_R(INS_vmov_i2d, EA_8BYTE, reg, intRegLo, intRegHi);
            else
                getEmitter()->emitIns_R_R(INS_vmov_i2f, EA_4BYTE, reg, intRegLo);

            // Free up anything that was tied up by genMakeAddressable
            //
            regSet.rsUnlockUsedReg(addrReg);
            genDoneAddressable(tree, addrReg, RegSet::KEEP_REG);
        }
        else
        {
            inst_RV_TT(ins_FloatLoad(tree->TypeGet()), reg, tree);
        }
        if (((tree->OperGet() == GT_CLS_VAR) || (tree->OperGet() == GT_IND)) && (tree->gtFlags & GTF_IND_VOLATILE))
        {
            // Emit a memory barrier instruction after the load
            instGen_MemoryBarrier();
        }
    }
}

void CodeGen::genCodeForTreeFloat_DONE(GenTreePtr tree, regNumber reg)
{
    return genCodeForTree_DONE(tree, reg);
}

void CodeGen::genFloatAsgArith(GenTreePtr tree)
{
    // Set Flowgraph.cpp, line 13750
    // arm VFP has tons of regs, 3-op instructions, and no addressing modes
    // so asg ops are kind of pointless
    noway_assert(!"Not Reachable for _TARGET_ARM_");
}

regNumber CodeGen::genAssignArithFloat(
    genTreeOps oper, GenTreePtr dst, regNumber dstreg, GenTreePtr src, regNumber srcreg)
{
    regNumber result;

    // dst should be a regvar or memory

    if (dst->IsRegVar())
    {
        regNumber reg = dst->gtRegNum;

        if (src->IsRegVar())
        {
            inst_RV_RV(ins_MathOp(oper, dst->gtType), reg, src->gtRegNum, dst->gtType);
        }
        else
        {
            inst_RV_TT(ins_MathOp(oper, dst->gtType), reg, src, 0, EmitSize(dst));
        }
        result = reg;
    }
    else // dst in memory
    {
        // since this is an asgop the ACTUAL destination is memory
        // but it is also one of the sources and SSE ops do not allow mem dests
        // so we have loaded it into a reg, and that is what dstreg represents
        assert(dstreg != REG_NA);

        if ((src->InReg()))
        {
            inst_RV_RV(ins_MathOp(oper, dst->gtType), dstreg, src->gtRegNum, dst->gtType);
        }
        else
        {
            // mem mem operation
            inst_RV_TT(ins_MathOp(oper, dst->gtType), dstreg, src, 0, EmitSize(dst));
        }

        dst->gtFlags &= ~GTF_REG_VAL; // ???

        inst_TT_RV(ins_FloatStore(dst->gtType), dst, dstreg, 0, EmitSize(dst));

        result = REG_NA;
    }

    return result;
}

void CodeGen::genFloatArith(GenTreePtr tree, RegSet::RegisterPreference* tgtPref)
{
    var_types  type = tree->TypeGet();
    genTreeOps oper = tree->OperGet();
    GenTreePtr op1  = tree->gtGetOp1();
    GenTreePtr op2  = tree->gtGetOp2();

    regNumber  tgtReg;
    unsigned   varNum;
    LclVarDsc* varDsc;
    VARSET_TP  varBit;

    assert(oper == GT_ADD || oper == GT_SUB || oper == GT_MUL || oper == GT_DIV);

    RegSet::RegisterPreference defaultPref(RBM_ALLFLOAT, RBM_NONE);
    if (tgtPref == NULL)
    {
        tgtPref = &defaultPref;
    }

    // Is the op2 (RHS)more complex than op1 (LHS)?
    //
    if (tree->gtFlags & GTF_REVERSE_OPS)
    {
        regMaskTP                  bestRegs = regSet.rsNarrowHint(RBM_ALLFLOAT, ~op1->gtRsvdRegs);
        RegSet::RegisterPreference pref(RBM_ALLFLOAT, bestRegs);

        // Evaluate op2 into a floating point register
        //
        genCodeForTreeFloat(op2, &pref);
        regSet.SetUsedRegFloat(op2, true);

        // Evaluate op1 into any floating point register
        //
        genCodeForTreeFloat(op1);
        regSet.SetUsedRegFloat(op1, true);

        regNumber op1Reg  = op1->gtRegNum;
        regMaskTP op1Mask = genRegMaskFloat(op1Reg, type);

        // Fix 388445 ARM JitStress WP7
        regSet.rsLockUsedReg(op1Mask);
        genRecoverReg(op2, RBM_ALLFLOAT, RegSet::KEEP_REG);
        noway_assert(op2->gtFlags & GTF_REG_VAL);
        regSet.rsUnlockUsedReg(op1Mask);

        regSet.SetUsedRegFloat(op1, false);
        regSet.SetUsedRegFloat(op2, false);
    }
    else
    {
        regMaskTP                  bestRegs = regSet.rsNarrowHint(RBM_ALLFLOAT, ~op2->gtRsvdRegs);
        RegSet::RegisterPreference pref(RBM_ALLFLOAT, bestRegs);

        // Evaluate op1 into a floating point register
        //
        genCodeForTreeFloat(op1, &pref);
        regSet.SetUsedRegFloat(op1, true);

        // Evaluate op2 into any floating point register
        //
        genCodeForTreeFloat(op2);
        regSet.SetUsedRegFloat(op2, true);

        regNumber op2Reg  = op2->gtRegNum;
        regMaskTP op2Mask = genRegMaskFloat(op2Reg, type);

        // Fix 388445 ARM JitStress WP7
        regSet.rsLockUsedReg(op2Mask);
        genRecoverReg(op1, RBM_ALLFLOAT, RegSet::KEEP_REG);
        noway_assert(op1->gtFlags & GTF_REG_VAL);
        regSet.rsUnlockUsedReg(op2Mask);

        regSet.SetUsedRegFloat(op2, false);
        regSet.SetUsedRegFloat(op1, false);
    }

    tgtReg = regSet.PickRegFloat(type, tgtPref, true);

    noway_assert(op1->gtFlags & GTF_REG_VAL);
    noway_assert(op2->gtFlags & GTF_REG_VAL);

    inst_RV_RV_RV(ins_MathOp(oper, type), tgtReg, op1->gtRegNum, op2->gtRegNum, emitActualTypeSize(type));

    genCodeForTreeFloat_DONE(tree, tgtReg);
}

regNumber CodeGen::genArithmFloat(
    genTreeOps oper, GenTreePtr dst, regNumber dstreg, GenTreePtr src, regNumber srcreg, bool bReverse)
{
    regNumber result = REG_NA;

    assert(dstreg != REG_NA);

    if (bReverse)
    {
        GenTree*  temp    = src;
        regNumber tempreg = srcreg;
        src               = dst;
        srcreg            = dstreg;
        dst               = temp;
        dstreg            = tempreg;
    }

    if (srcreg == REG_NA)
    {
        if (src->IsRegVar())
        {
            inst_RV_RV(ins_MathOp(oper, dst->gtType), dst->gtRegNum, src->gtRegNum, dst->gtType);
        }
        else
        {
            inst_RV_TT(ins_MathOp(oper, dst->gtType), dst->gtRegNum, src);
        }
    }
    else
    {
        inst_RV_RV(ins_MathOp(oper, dst->gtType), dstreg, srcreg, dst->gtType);
    }

    result = dstreg;

    assert(result != REG_NA);
    return result;
}

void CodeGen::genKeepAddressableFloat(GenTreePtr tree, regMaskTP* regMaskIntPtr, regMaskTP* regMaskFltPtr)
{
    regMaskTP regMaskInt, regMaskFlt;

    regMaskInt = *regMaskIntPtr;
    regMaskFlt = *regMaskFltPtr;

    *regMaskIntPtr = *regMaskFltPtr = 0;

    switch (tree->OperGet())
    {
        case GT_REG_VAR:
            // If register has been spilled, unspill it
            if (tree->gtFlags & GTF_SPILLED)
            {
                UnspillFloat(&compiler->lvaTable[tree->gtLclVarCommon.gtLclNum]);
            }
            break;

        case GT_CNS_DBL:
            if (tree->gtFlags & GTF_SPILLED)
            {
                UnspillFloat(tree);
            }
            *regMaskFltPtr = genRegMaskFloat(tree->gtRegNum, tree->TypeGet());
            break;

        case GT_LCL_FLD:
        case GT_LCL_VAR:
        case GT_CLS_VAR:
            break;

        case GT_IND:
            if (regMaskFlt == RBM_NONE)
            {
                *regMaskIntPtr = genKeepAddressable(tree, regMaskInt, 0);
                *regMaskFltPtr = 0;
                return;
            }
            __fallthrough;

        default:
            *regMaskIntPtr = 0;
            if (tree->gtFlags & GTF_SPILLED)
            {
                UnspillFloat(tree);
            }
            *regMaskFltPtr = genRegMaskFloat(tree->gtRegNum, tree->TypeGet());
            break;
    }
}

void CodeGen::genComputeAddressableFloat(GenTreePtr      tree,
                                         regMaskTP       addrRegInt,
                                         regMaskTP       addrRegFlt,
                                         RegSet::KeepReg keptReg,
                                         regMaskTP       needReg,
                                         RegSet::KeepReg keepReg,
                                         bool            freeOnly /* = false */)
{
    noway_assert(genStillAddressable(tree));
    noway_assert(varTypeIsFloating(tree->TypeGet()));

    genDoneAddressableFloat(tree, addrRegInt, addrRegFlt, keptReg);

    regNumber reg;
    if (tree->gtFlags & GTF_REG_VAL)
    {
        reg = tree->gtRegNum;
        if (freeOnly && !(genRegMaskFloat(reg, tree->TypeGet()) & regSet.RegFreeFloat()))
        {
            goto LOAD_REG;
        }
    }
    else
    {
    LOAD_REG:
        RegSet::RegisterPreference pref(needReg, RBM_NONE);
        reg = regSet.PickRegFloat(tree->TypeGet(), &pref);
        genLoadFloat(tree, reg);
    }

    genMarkTreeInReg(tree, reg);

    if (keepReg == RegSet::KEEP_REG)
    {
        regSet.SetUsedRegFloat(tree, true);
    }
}

void CodeGen::genDoneAddressableFloat(GenTreePtr      tree,
                                      regMaskTP       addrRegInt,
                                      regMaskTP       addrRegFlt,
                                      RegSet::KeepReg keptReg)
{
    assert(!(addrRegInt && addrRegFlt));

    if (addrRegInt)
    {
        return genDoneAddressable(tree, addrRegInt, keptReg);
    }
    else if (addrRegFlt)
    {
        if (keptReg == RegSet::KEEP_REG)
        {
            for (regNumber r = REG_FP_FIRST; r != REG_NA; r = regNextOfType(r, tree->TypeGet()))
            {
                regMaskTP mask = genRegMaskFloat(r, tree->TypeGet());
                // some masks take up more than one bit
                if ((mask & addrRegFlt) == mask)
                {
                    regSet.SetUsedRegFloat(tree, false);
                }
            }
        }
    }
}

GenTreePtr CodeGen::genMakeAddressableFloat(GenTreePtr tree,
                                            regMaskTP* regMaskIntPtr,
                                            regMaskTP* regMaskFltPtr,
                                            bool       bCollapseConstantDoubles)
{
    *regMaskIntPtr = *regMaskFltPtr = 0;

    switch (tree->OperGet())
    {

        case GT_LCL_VAR:
            genMarkLclVar(tree);
            __fallthrough;

        case GT_REG_VAR:
        case GT_LCL_FLD:
        case GT_CLS_VAR:
            return tree;

        case GT_IND:
            // Try to make the address directly addressable

            if (genMakeIndAddrMode(tree->gtOp.gtOp1, tree, false, RBM_ALLFLOAT, RegSet::KEEP_REG, regMaskIntPtr, false))
            {
                genUpdateLife(tree);
                return tree;
            }
            else
            {
                GenTreePtr addr = tree;
                tree            = tree->gtOp.gtOp1;
                genCodeForTree(tree, 0);
                regSet.rsMarkRegUsed(tree, addr);

                *regMaskIntPtr = genRegMask(tree->gtRegNum);
                return addr;
            }

        // fall through

        default:
            genCodeForTreeFloat(tree);
            regSet.SetUsedRegFloat(tree, true);

            // update mask
            *regMaskFltPtr = genRegMaskFloat(tree->gtRegNum, tree->TypeGet());

            return tree;
            break;
    }
}

void CodeGen::genCodeForTreeCastFloat(GenTree* tree, RegSet::RegisterPreference* pref)
{
    GenTreePtr op1  = tree->gtOp.gtOp1;
    var_types  from = op1->gtType;
    var_types  to   = tree->gtType;

    if (varTypeIsFloating(from))
        genCodeForTreeCastFromFloat(tree, pref);
    else
        genCodeForTreeCastToFloat(tree, pref);
}

void CodeGen::genCodeForTreeCastFromFloat(GenTree* tree, RegSet::RegisterPreference* pref)
{
    GenTreePtr op1          = tree->gtOp.gtOp1;
    var_types  from         = op1->gtType;
    var_types  final        = tree->gtType;
    var_types  intermediate = tree->CastToType();

    regNumber srcReg;
    regNumber dstReg;

    assert(varTypeIsFloating(from));

    // Evaluate op1 into a floating point register
    //
    if (varTypeIsFloating(final))
    {
        genCodeForTreeFloat(op1, pref);
    }
    else
    {
        RegSet::RegisterPreference defaultPref(RBM_ALLFLOAT, RBM_NONE);
        genCodeForTreeFloat(op1, &defaultPref);
    }

    srcReg = op1->gtRegNum;

    if (varTypeIsFloating(final))
    {
        // float  => double  or
        // double => float

        dstReg = regSet.PickRegFloat(final, pref);

        instruction ins = ins_FloatConv(final, from);
        if (!isMoveIns(ins) || (srcReg != dstReg))
        {
            inst_RV_RV(ins, dstReg, srcReg, from);
        }
    }
    else
    {
        // float  => int  or
        // double => int

        dstReg = regSet.rsPickReg(pref->ok, pref->best);

        RegSet::RegisterPreference defaultPref(RBM_ALLFLOAT, genRegMask(srcReg));
        regNumber                  intermediateReg = regSet.PickRegFloat(TYP_FLOAT, &defaultPref);

        if ((intermediate == TYP_UINT) && (final == TYP_INT))
        {
            // Perform the conversion using the FP unit
            inst_RV_RV(ins_FloatConv(TYP_UINT, from), intermediateReg, srcReg, from);

            // Prevent the call to genIntegerCast
            final = TYP_UINT;
        }
        else
        {
            // Perform the conversion using the FP unit
            inst_RV_RV(ins_FloatConv(TYP_INT, from), intermediateReg, srcReg, from);
        }

        // the integer result is now in the FP register, move it to the integer ones
        getEmitter()->emitIns_R_R(INS_vmov_f2i, EA_4BYTE, dstReg, intermediateReg);

        regTracker.rsTrackRegTrash(dstReg);

        // handle things like int <- short <- double
        if (final != intermediate)
        {
            // lie about the register so integer cast logic will finish the job
            op1->gtRegNum = dstReg;
            genIntegerCast(tree, pref->ok, pref->best);
        }
    }

    genUpdateLife(op1);
    genCodeForTree_DONE(tree, dstReg);
}

void CodeGen::genCodeForTreeCastToFloat(GenTreePtr tree, RegSet::RegisterPreference* pref)
{
    regNumber srcReg;
    regNumber dstReg;
    regNumber vmovReg;

    regMaskTP addrReg;

    GenTreePtr op1 = tree->gtOp.gtOp1;
    op1            = genCodeForCommaTree(op1); // Trim off any comma expressions.
    var_types from = op1->gtType;
    var_types to   = tree->gtType;

    switch (from)
    {
        case TYP_BOOL:
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_CHAR:
        case TYP_SHORT:
            // load it into a register
            genCodeForTree(op1, 0);

            __fallthrough;

        case TYP_BYREF:
            from = TYP_INT;

            __fallthrough;

        case TYP_INT:
        {
            if (op1->gtOper == GT_LCL_FLD)
            {
                genComputeReg(op1, 0, RegSet::ANY_REG, RegSet::FREE_REG);
                addrReg = 0;
            }
            else
            {
                addrReg = genMakeAddressable(op1, 0, RegSet::FREE_REG);
            }

            // Grab register for the cast
            dstReg = regSet.PickRegFloat(to, pref);

            // float type that is same size as the int we are coming from
            var_types vmovType = TYP_FLOAT;
            regNumber vmovReg  = regSet.PickRegFloat(vmovType);

            if (tree->gtFlags & GTF_UNSIGNED)
                from = TYP_UINT;

            // Is the value a constant, or now sitting in a register?
            if (op1->InReg() || op1->IsCnsIntOrI())
            {
                if (op1->IsCnsIntOrI())
                {
                    srcReg = genGetRegSetToIcon(op1->AsIntConCommon()->IconValue(), RBM_NONE, op1->TypeGet());
                }
                else
                {
                    srcReg = op1->gtRegNum;
                }

                // move the integer register value over to the FP register
                getEmitter()->emitIns_R_R(INS_vmov_i2f, EA_4BYTE, vmovReg, srcReg);
                // now perform the conversion to the proper floating point representation
                inst_RV_RV(ins_FloatConv(to, from), dstReg, vmovReg, to);
            }
            else
            {
                // Load the value from its address
                inst_RV_TT(ins_FloatLoad(vmovType), vmovReg, op1);
                inst_RV_RV(ins_FloatConv(to, from), dstReg, vmovReg, to);
            }

            if (addrReg)
            {
                genDoneAddressable(op1, addrReg, RegSet::FREE_REG);
            }
            genMarkTreeInReg(tree, dstReg);

            break;
        }
        case TYP_FLOAT:
        case TYP_DOUBLE:
        {
            //  This is a cast from float to double or double to float

            genCodeForTreeFloat(op1, pref);

            // Grab register for the cast
            dstReg = regSet.PickRegFloat(to, pref);

            if ((from != to) || (dstReg != op1->gtRegNum))
            {
                inst_RV_RV(ins_FloatConv(to, from), dstReg, op1->gtRegNum, to);
            }

            // Assign reg to tree
            genMarkTreeInReg(tree, dstReg);

            break;
        }
        default:
        {
            assert(!"unsupported cast");
            break;
        }
    }
}

void CodeGen::genRoundFloatExpression(GenTreePtr op, var_types type)
{
    // Do nothing with memory resident opcodes - these are the right precision
    if (type == TYP_UNDEF)
        type = op->TypeGet();

    switch (op->gtOper)
    {
        case GT_LCL_VAR:
            genMarkLclVar(op);
            __fallthrough;

        case GT_LCL_FLD:
        case GT_CLS_VAR:
        case GT_CNS_DBL:
        case GT_IND:
            if (type == op->TypeGet())
                return;

        default:
            break;
    }
}

#ifdef DEBUG

regMaskTP CodeGenInterface::genStressLockedMaskFloat()
{
    return 0;
}

#endif // DEBUG

/*********************************************************************
 * Preserve used callee trashed registers across calls.
 *
 */
void CodeGen::SpillForCallRegisterFP(regMaskTP noSpillMask)
{
    regMaskTP regBit = 1;
    for (regNumber regNum = REG_FIRST; regNum < REG_COUNT; regNum = REG_NEXT(regNum), regBit <<= 1)
    {
        if (!(regBit & noSpillMask) && (regBit & RBM_FLT_CALLEE_TRASH) && regSet.rsUsedTree[regNum])
        {
            SpillFloat(regNum, true);
        }
    }
}

/*********************************************************************
 *
 * Spill the used floating point register or the enregistered var.
 * If spilling for a call, then record so, so we can unspill the
 * ones that were spilled for the call.
 *
 */
void CodeGenInterface::SpillFloat(regNumber reg, bool bIsCall /* = false */)
{
    regSet.rsSpillReg(reg);
}

void CodeGen::UnspillFloatMachineDep(RegSet::SpillDsc* spillDsc)
{
    // Do actual unspill
    regNumber reg;
    if (spillDsc->bEnregisteredVariable)
    {
        NYI("unspill enreg var");
        reg = regSet.PickRegFloat();
    }
    else
    {
        UnspillFloatMachineDep(spillDsc, false);
    }
}

void CodeGen::UnspillFloatMachineDep(RegSet::SpillDsc* spillDsc, bool useSameReg)
{
    assert(!spillDsc->bEnregisteredVariable);

    assert(spillDsc->spillTree->gtFlags & GTF_SPILLED);

    spillDsc->spillTree->gtFlags &= ~GTF_SPILLED;

    var_types type = spillDsc->spillTree->TypeGet();
    regNumber reg;
    if (useSameReg)
    {
        // Give register preference as the same register that the tree was originally using.
        reg = spillDsc->spillTree->gtRegNum;

        regMaskTP maskPref = genRegMask(reg);
        if (type == TYP_DOUBLE)
        {
            assert((maskPref & RBM_DBL_REGS) != 0);
            maskPref |= genRegMask(REG_NEXT(reg));
        }

        RegSet::RegisterPreference pref(RBM_ALLFLOAT, maskPref);
        reg = regSet.PickRegFloat(type, &pref);
    }
    else
    {
        reg = regSet.PickRegFloat();
    }

    // load from spilled spot
    compiler->codeGen->reloadFloatReg(type, spillDsc->spillTemp, reg);

    compiler->codeGen->genMarkTreeInReg(spillDsc->spillTree, reg);
    regSet.SetUsedRegFloat(spillDsc->spillTree, true);
}

//
instruction genFloatJumpInstr(genTreeOps cmp, bool isUnordered)
{
    switch (cmp)
    {
        case GT_EQ:
            return INS_beq;
        case GT_NE:
            return INS_bne;
        case GT_LT:
            return isUnordered ? INS_blt : INS_blo;
        case GT_LE:
            return isUnordered ? INS_ble : INS_bls;
        case GT_GE:
            return isUnordered ? INS_bpl : INS_bge;
        case GT_GT:
            return isUnordered ? INS_bhi : INS_bgt;
        default:
            unreached();
    }
}

void CodeGen::genCondJumpFloat(GenTreePtr cond, BasicBlock* jumpTrue, BasicBlock* jumpFalse)
{
    assert(jumpTrue && jumpFalse);
    assert(!(cond->gtFlags & GTF_REVERSE_OPS)); // Done in genCondJump()
    assert(varTypeIsFloating(cond->gtOp.gtOp1->gtType));

    GenTreePtr op1         = cond->gtOp.gtOp1;
    GenTreePtr op2         = cond->gtOp.gtOp2;
    genTreeOps cmp         = cond->OperGet();
    bool       isUnordered = cond->gtFlags & GTF_RELOP_NAN_UN ? true : false;

    regMaskTP                  bestRegs = regSet.rsNarrowHint(RBM_ALLFLOAT, ~op2->gtRsvdRegs);
    RegSet::RegisterPreference pref(RBM_ALLFLOAT, bestRegs);

    // Prepare operands.
    genCodeForTreeFloat(op1, &pref);
    regSet.SetUsedRegFloat(op1, true);

    genCodeForTreeFloat(op2);
    regSet.SetUsedRegFloat(op2, true);

    genRecoverReg(op1, RBM_ALLFLOAT, RegSet::KEEP_REG);
    noway_assert(op1->gtFlags & GTF_REG_VAL);

    // cmp here
    getEmitter()->emitIns_R_R(INS_vcmp, EmitSize(op1), op1->gtRegNum, op2->gtRegNum);

    // vmrs with register 0xf has special meaning of transferring flags
    getEmitter()->emitIns_R(INS_vmrs, EA_4BYTE, REG_R15);

    regSet.SetUsedRegFloat(op2, false);
    regSet.SetUsedRegFloat(op1, false);

    getEmitter()->emitIns_J(genFloatJumpInstr(cmp, isUnordered), jumpTrue);
}

#endif // LEGACY_BACKEND
