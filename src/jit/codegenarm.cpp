// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        ARM Code Generator                                 XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_ARM_
#include "codegen.h"
#include "lower.h"
#include "gcinfo.h"
#include "emit.h"

//------------------------------------------------------------------------
// genSetRegToIcon: Generate code that will set the given register to the integer constant.
//
void CodeGen::genSetRegToIcon(regNumber reg, ssize_t val, var_types type, insFlags flags)
{
    // Reg cannot be a FP reg
    assert(!genIsValidFloatReg(reg));

    // The only TYP_REF constant that can come this path is a managed 'null' since it is not
    // relocatable.  Other ref type constants (e.g. string objects) go through a different
    // code path.
    noway_assert(type != TYP_REF || val == 0);

    instGen_Set_Reg_To_Imm(emitActualTypeSize(type), reg, val, flags);
}

//------------------------------------------------------------------------
// genCallFinally: Generate a call to the finally block.
//
BasicBlock* CodeGen::genCallFinally(BasicBlock* block)
{
    BasicBlock* bbFinallyRet = nullptr;

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
    // The funclet prolog/epilog will do "push {lr}" / "pop {pc}" to do the return.
    getEmitter()->emitIns_R_L(INS_movw, EA_4BYTE_DSP_RELOC, bbFinallyRet, REG_LR);
    getEmitter()->emitIns_R_L(INS_movt, EA_4BYTE_DSP_RELOC, bbFinallyRet, REG_LR);

    // Jump to the finally BB
    inst_JMP(EJ_jmp, block->bbJumpDest);

    // The BBJ_ALWAYS is used because the BBJ_CALLFINALLY can't point to the
    // jump target using bbJumpDest - that is already used to point
    // to the finally block. So just skip past the BBJ_ALWAYS unless the
    // block is RETLESS.
    assert(!(block->bbFlags & BBF_RETLESS_CALL));
    assert(block->isBBCallAlwaysPair());
    return block->bbNext;
}

//------------------------------------------------------------------------
// genEHCatchRet:
void CodeGen::genEHCatchRet(BasicBlock* block)
{
    getEmitter()->emitIns_R_L(INS_movw, EA_4BYTE_DSP_RELOC, block->bbJumpDest, REG_INTRET);
    getEmitter()->emitIns_R_L(INS_movt, EA_4BYTE_DSP_RELOC, block->bbJumpDest, REG_INTRET);
}

//---------------------------------------------------------------------
// genIntrinsic - generate code for a given intrinsic
//
// Arguments
//    treeNode - the GT_INTRINSIC node
//
// Return value:
//    None
//
void CodeGen::genIntrinsic(GenTreePtr treeNode)
{
    // Both operand and its result must be of the same floating point type.
    GenTreePtr srcNode = treeNode->gtOp.gtOp1;
    assert(varTypeIsFloating(srcNode));
    assert(srcNode->TypeGet() == treeNode->TypeGet());

    // Right now only Abs/Round/Sqrt are treated as math intrinsics.
    //
    switch (treeNode->gtIntrinsic.gtIntrinsicId)
    {
        case CORINFO_INTRINSIC_Abs:
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_vabs, emitTypeSize(treeNode), treeNode, srcNode);
            break;

        case CORINFO_INTRINSIC_Round:
            NYI_ARM("genIntrinsic for round - not implemented yet");
            break;

        case CORINFO_INTRINSIC_Sqrt:
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_vsqrt, emitTypeSize(treeNode), treeNode, srcNode);
            break;

        default:
            assert(!"genIntrinsic: Unsupported intrinsic");
            unreached();
    }

    genProduceReg(treeNode);
}

//---------------------------------------------------------------------
// genPutArgStk - generate code for a GT_PUTARG_STK node
//
// Arguments
//    treeNode - the GT_PUTARG_STK node
//
// Return value:
//    None
//
void CodeGen::genPutArgStk(GenTreePutArgStk* treeNode)
{
    assert(treeNode->OperGet() == GT_PUTARG_STK);
    var_types  targetType = treeNode->TypeGet();
    GenTreePtr source     = treeNode->gtOp1;
    emitter*   emit       = getEmitter();

    // This is the varNum for our store operations,
    // typically this is the varNum for the Outgoing arg space
    // When we are generating a tail call it will be the varNum for arg0
    unsigned varNumOut;
    unsigned argOffsetMax; // Records the maximum size of this area for assert checks

    // Get argument offset to use with 'varNumOut'
    // Here we cross check that argument offset hasn't changed from lowering to codegen since
    // we are storing arg slot number in GT_PUTARG_STK node in lowering phase.
    unsigned argOffsetOut = treeNode->gtSlotNum * TARGET_POINTER_SIZE;

#ifdef DEBUG
    fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(treeNode->gtCall, treeNode);
    assert(curArgTabEntry);
    assert(argOffsetOut == (curArgTabEntry->slotNum * TARGET_POINTER_SIZE));
#endif // DEBUG

    varNumOut    = compiler->lvaOutgoingArgSpaceVar;
    argOffsetMax = compiler->lvaOutgoingArgSpaceSize;

    bool isStruct = (targetType == TYP_STRUCT) || (source->OperGet() == GT_FIELD_LIST);

    if (!isStruct) // a normal non-Struct argument
    {
        instruction storeIns  = ins_Store(targetType);
        emitAttr    storeAttr = emitTypeSize(targetType);

        // If it is contained then source must be the integer constant zero
        if (source->isContained())
        {
            assert(source->OperGet() == GT_CNS_INT);
            assert(source->AsIntConCommon()->IconValue() == 0);
            NYI("genPutArgStk: contained zero source");
        }
        else
        {
            genConsumeReg(source);
            emit->emitIns_S_R(storeIns, storeAttr, source->gtRegNum, varNumOut, argOffsetOut);
        }
        argOffsetOut += EA_SIZE_IN_BYTES(storeAttr);
        assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
    }
    else // We have some kind of a struct argument
    {
        assert(source->isContained()); // We expect that this node was marked as contained in LowerArm

        if (source->OperGet() == GT_FIELD_LIST)
        {
            // Deal with the multi register passed struct args.
            GenTreeFieldList* fieldListPtr = source->AsFieldList();

            // Evaluate each of the GT_FIELD_LIST items into their register
            // and store their register into the outgoing argument area
            for (; fieldListPtr != nullptr; fieldListPtr = fieldListPtr->Rest())
            {
                GenTreePtr nextArgNode = fieldListPtr->gtOp.gtOp1;
                genConsumeReg(nextArgNode);

                regNumber reg  = nextArgNode->gtRegNum;
                var_types type = nextArgNode->TypeGet();
                emitAttr  attr = emitTypeSize(type);

                // Emit store instructions to store the registers produced by the GT_FIELD_LIST into the outgoing
                // argument area
                emit->emitIns_S_R(ins_Store(type), attr, reg, varNumOut, argOffsetOut);
                argOffsetOut += EA_SIZE_IN_BYTES(attr);
                assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
            }
        }
        else // We must have a GT_OBJ or a GT_LCL_VAR
        {
            NYI("genPutArgStk: GT_OBJ or GT_LCL_VAR source of struct type");
        }
    }
}

//------------------------------------------------------------------------
// instGen_Set_Reg_To_Imm: Move an immediate value into an integer register.
//
void CodeGen::instGen_Set_Reg_To_Imm(emitAttr size, regNumber reg, ssize_t imm, insFlags flags)
{
    // reg cannot be a FP register
    assert(!genIsValidFloatReg(reg));

    if (!compiler->opts.compReloc)
    {
        size = EA_SIZE(size); // Strip any Reloc flags from size if we aren't doing relocs
    }

    if (EA_IS_RELOC(size))
    {
        getEmitter()->emitIns_R_I(INS_movw, size, reg, imm);
        getEmitter()->emitIns_R_I(INS_movt, size, reg, imm);
    }
    else if (imm == 0)
    {
        instGen_Set_Reg_To_Zero(size, reg, flags);
    }
    else
    {
        if (arm_Valid_Imm_For_Mov(imm))
        {
            getEmitter()->emitIns_R_I(INS_mov, size, reg, imm, flags);
        }
        else // We have to use a movw/movt pair of instructions
        {
            ssize_t imm_lo16 = (imm & 0xffff);
            ssize_t imm_hi16 = (imm >> 16) & 0xffff;

            assert(arm_Valid_Imm_For_Mov(imm_lo16));
            assert(imm_hi16 != 0);

            getEmitter()->emitIns_R_I(INS_movw, size, reg, imm_lo16);

            // If we've got a low register, the high word is all bits set,
            // and the high bit of the low word is set, we can sign extend
            // halfword and save two bytes of encoding. This can happen for
            // small magnitude negative numbers 'n' for -32768 <= n <= -1.

            if (getEmitter()->isLowRegister(reg) && (imm_hi16 == 0xffff) && ((imm_lo16 & 0x8000) == 0x8000))
            {
                getEmitter()->emitIns_R_R(INS_sxth, EA_2BYTE, reg, reg);
            }
            else
            {
                getEmitter()->emitIns_R_I(INS_movt, size, reg, imm_hi16);
            }

            if (flags == INS_FLAGS_SET)
                getEmitter()->emitIns_R_R(INS_mov, size, reg, reg, INS_FLAGS_SET);
        }
    }

    regTracker.rsTrackRegIntCns(reg, imm);
}

//------------------------------------------------------------------------
// genSetRegToConst: Generate code to set a register 'targetReg' of type 'targetType'
//    to the constant specified by the constant (GT_CNS_INT or GT_CNS_DBL) in 'tree'.
//
// Notes:
//    This does not call genProduceReg() on the target register.
//
void CodeGen::genSetRegToConst(regNumber targetReg, var_types targetType, GenTreePtr tree)
{
    switch (tree->gtOper)
    {
        case GT_CNS_INT:
        {
            // relocatable values tend to come down as a CNS_INT of native int type
            // so the line between these two opcodes is kind of blurry
            GenTreeIntConCommon* con    = tree->AsIntConCommon();
            ssize_t              cnsVal = con->IconValue();

            bool needReloc = compiler->opts.compReloc && tree->IsIconHandle();
            if (needReloc)
            {
                instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, targetReg, cnsVal);
                regTracker.rsTrackRegTrash(targetReg);
            }
            else
            {
                genSetRegToIcon(targetReg, cnsVal, targetType);
            }
        }
        break;

        case GT_CNS_DBL:
        {
            GenTreeDblCon* dblConst   = tree->AsDblCon();
            double         constValue = dblConst->gtDblCon.gtDconVal;
            // TODO-ARM-CQ: Do we have a faster/smaller way to generate 0.0 in thumb2 ISA ?
            if (targetType == TYP_FLOAT)
            {
                // Get a temp integer register
                regMaskTP tmpRegMask = tree->gtRsvdRegs;
                regNumber tmpReg     = genRegNumFromMask(tmpRegMask);
                assert(tmpReg != REG_NA);

                float f = forceCastToFloat(constValue);
                genSetRegToIcon(tmpReg, *((int*)(&f)));
                getEmitter()->emitIns_R_R(INS_vmov_i2f, EA_4BYTE, targetReg, tmpReg);
            }
            else
            {
                assert(targetType == TYP_DOUBLE);

                unsigned* cv = (unsigned*)&constValue;

                // Get two temp integer registers
                regMaskTP tmpRegsMask = tree->gtRsvdRegs;
                regMaskTP tmpRegMask  = genFindHighestBit(tmpRegsMask); // set tmpRegMsk to a one-bit mask
                regNumber tmpReg1     = genRegNumFromMask(tmpRegMask);
                assert(tmpReg1 != REG_NA);

                tmpRegsMask &= ~genRegMask(tmpReg1);                // remove the bit for 'tmpReg1'
                tmpRegMask        = genFindHighestBit(tmpRegsMask); // set tmpRegMsk to a one-bit mask
                regNumber tmpReg2 = genRegNumFromMask(tmpRegMask);
                assert(tmpReg2 != REG_NA);

                genSetRegToIcon(tmpReg1, cv[0]);
                genSetRegToIcon(tmpReg2, cv[1]);

                getEmitter()->emitIns_R_R_R(INS_vmov_i2d, EA_8BYTE, targetReg, tmpReg1, tmpReg2);
            }
        }
        break;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// genCodeForBinary: Generate code for many binary arithmetic operators
// This method is expected to have called genConsumeOperands() before calling it.
//
// Arguments:
//    treeNode - The binary operation for which we are generating code.
//
// Return Value:
//    None.
//
// Notes:
//    Mul and div are not handled here.
//    See the assert below for the operators that are handled.

void CodeGen::genCodeForBinary(GenTree* treeNode)
{
    const genTreeOps oper       = treeNode->OperGet();
    regNumber        targetReg  = treeNode->gtRegNum;
    var_types        targetType = treeNode->TypeGet();
    emitter*         emit       = getEmitter();

    assert(oper == GT_ADD || oper == GT_SUB || oper == GT_ADD_LO || oper == GT_ADD_HI || oper == GT_SUB_LO ||
           oper == GT_SUB_HI || oper == GT_OR || oper == GT_XOR || oper == GT_AND);

    if ((oper == GT_ADD || oper == GT_SUB || oper == GT_ADD_HI || oper == GT_SUB_HI) && treeNode->gtOverflow())
    {
        // This is also checked in the importer.
        NYI("Overflow not yet implemented");
    }

    GenTreePtr op1 = treeNode->gtGetOp1();
    GenTreePtr op2 = treeNode->gtGetOp2();

    instruction ins = genGetInsForOper(oper, targetType);

    // The arithmetic node must be sitting in a register (since it's not contained)
    noway_assert(targetReg != REG_NA);

    if ((oper == GT_ADD_LO || oper == GT_SUB_LO))
    {
        // During decomposition, all operands become reg
        assert(!op1->isContained() && !op2->isContained());
        emit->emitIns_R_R_R(ins, emitTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum, op2->gtRegNum,
                            INS_FLAGS_SET);
    }
    else
    {
        regNumber r = emit->emitInsTernary(ins, emitTypeSize(treeNode), treeNode, op1, op2);
        assert(r == targetReg);
    }

    genProduceReg(treeNode);
}

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
void CodeGen::genReturn(GenTreePtr treeNode)
{
    assert(treeNode->OperGet() == GT_RETURN || treeNode->OperGet() == GT_RETFILT);
    GenTreePtr op1        = treeNode->gtGetOp1();
    var_types  targetType = treeNode->TypeGet();

#ifdef DEBUG
    if (targetType == TYP_VOID)
    {
        assert(op1 == nullptr);
    }
#endif

    if (treeNode->TypeGet() == TYP_LONG)
    {
        assert(op1 != nullptr);
        noway_assert(op1->OperGet() == GT_LONG);
        GenTree* loRetVal = op1->gtGetOp1();
        GenTree* hiRetVal = op1->gtGetOp2();
        noway_assert((loRetVal->gtRegNum != REG_NA) && (hiRetVal->gtRegNum != REG_NA));

        genConsumeReg(loRetVal);
        genConsumeReg(hiRetVal);
        if (loRetVal->gtRegNum != REG_LNGRET_LO)
        {
            inst_RV_RV(ins_Copy(targetType), REG_LNGRET_LO, loRetVal->gtRegNum, TYP_INT);
        }
        if (hiRetVal->gtRegNum != REG_LNGRET_HI)
        {
            inst_RV_RV(ins_Copy(targetType), REG_LNGRET_HI, hiRetVal->gtRegNum, TYP_INT);
        }
    }
    else
    {
        if (varTypeIsStruct(treeNode))
        {
            NYI_ARM("struct return");
        }
        else if (targetType != TYP_VOID)
        {
            assert(op1 != nullptr);
            noway_assert(op1->gtRegNum != REG_NA);

            // !! NOTE !! genConsumeReg will clear op1 as GC ref after it has
            // consumed a reg for the operand. This is because the variable
            // is dead after return. But we are issuing more instructions
            // like "profiler leave callback" after this consumption. So
            // if you are issuing more instructions after this point,
            // remember to keep the variable live up until the new method
            // exit point where it is actually dead.
            genConsumeReg(op1);

            regNumber retReg = varTypeIsFloating(treeNode) ? REG_FLOATRET : REG_INTRET;
            if (op1->gtRegNum != retReg)
            {
                inst_RV_RV(ins_Move_Extend(targetType, true), retReg, op1->gtRegNum, targetType);
            }
        }
    }
}

//------------------------------------------------------------------------
// genCodeForTreeNode Generate code for a single node in the tree.
//
// Preconditions:
//    All operands have been evaluated.
//
void CodeGen::genCodeForTreeNode(GenTreePtr treeNode)
{
    regNumber targetReg  = treeNode->gtRegNum;
    var_types targetType = treeNode->TypeGet();
    emitter*  emit       = getEmitter();

#ifdef DEBUG
    lastConsumedNode = nullptr;
    if (compiler->verbose)
    {
        unsigned seqNum = treeNode->gtSeqNum; // Useful for setting a conditional break in Visual Studio
        compiler->gtDispLIRNode(treeNode, "Generating: ");
    }
#endif

    // contained nodes are part of their parents for codegen purposes
    // ex : immediates, most LEAs
    if (treeNode->isContained())
    {
        return;
    }

    switch (treeNode->gtOper)
    {
        case GT_LCLHEAP:
            genLclHeap(treeNode);
            break;

        case GT_CNS_INT:
        case GT_CNS_DBL:
            genSetRegToConst(targetReg, targetType, treeNode);
            genProduceReg(treeNode);
            break;

        case GT_NOT:
            assert(!varTypeIsFloating(targetType));

            __fallthrough;

        case GT_NEG:
        {
            instruction ins = genGetInsForOper(treeNode->OperGet(), targetType);

            // The arithmetic node must be sitting in a register (since it's not contained)
            assert(!treeNode->isContained());
            // The dst can only be a register.
            assert(targetReg != REG_NA);

            GenTreePtr operand = treeNode->gtGetOp1();
            assert(!operand->isContained());
            // The src must be a register.
            regNumber operandReg = genConsumeReg(operand);

            if (ins == INS_vneg)
            {
                getEmitter()->emitIns_R_R(ins, emitTypeSize(treeNode), targetReg, operandReg);
            }
            else
            {
                getEmitter()->emitIns_R_R_I(ins, emitTypeSize(treeNode), targetReg, operandReg, 0);
            }
        }
            genProduceReg(treeNode);
            break;

        case GT_OR:
        case GT_XOR:
        case GT_AND:
            assert(varTypeIsIntegralOrI(treeNode));
            __fallthrough;

        case GT_ADD_LO:
        case GT_ADD_HI:
        case GT_SUB_LO:
        case GT_SUB_HI:
        case GT_ADD:
        case GT_SUB:
            genConsumeOperands(treeNode->AsOp());
            genCodeForBinary(treeNode);
            break;

        case GT_MUL:
        {
            genConsumeOperands(treeNode->AsOp());

            const genTreeOps oper = treeNode->OperGet();
            if (treeNode->gtOverflow())
            {
                // This is also checked in the importer.
                NYI("Overflow not yet implemented");
            }

            GenTreePtr  op1 = treeNode->gtGetOp1();
            GenTreePtr  op2 = treeNode->gtGetOp2();
            instruction ins = genGetInsForOper(treeNode->OperGet(), targetType);

            // The arithmetic node must be sitting in a register (since it's not contained)
            noway_assert(targetReg != REG_NA);

            regNumber r = emit->emitInsTernary(ins, emitTypeSize(treeNode), treeNode, op1, op2);
            assert(r == targetReg);
        }
            genProduceReg(treeNode);
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROR:
            genCodeForShift(treeNode);
            break;

        case GT_LSH_HI:
        case GT_RSH_LO:
            genCodeForShiftLong(treeNode);
            break;

        case GT_CAST:
            // Cast is never contained (?)
            noway_assert(targetReg != REG_NA);

            if (varTypeIsFloating(targetType) && varTypeIsFloating(treeNode->gtOp.gtOp1))
            {
                // Casts float/double <--> double/float
                genFloatToFloatCast(treeNode);
            }
            else if (varTypeIsFloating(treeNode->gtOp.gtOp1))
            {
                // Casts float/double --> int32/int64
                genFloatToIntCast(treeNode);
            }
            else if (varTypeIsFloating(targetType))
            {
                // Casts int32/uint32/int64/uint64 --> float/double
                genIntToFloatCast(treeNode);
            }
            else
            {
                // Casts int <--> int
                genIntToIntCast(treeNode);
            }
            // The per-case functions call genProduceReg()
            break;

        case GT_LCL_VAR:
        {
            GenTreeLclVarCommon* lcl = treeNode->AsLclVarCommon();
            // lcl_vars are not defs
            assert((treeNode->gtFlags & GTF_VAR_DEF) == 0);

            bool isRegCandidate = compiler->lvaTable[lcl->gtLclNum].lvIsRegCandidate();

            if (isRegCandidate && !(treeNode->gtFlags & GTF_VAR_DEATH))
            {
                assert((treeNode->InReg()) || (treeNode->gtFlags & GTF_SPILLED));
            }

            // If this is a register candidate that has been spilled, genConsumeReg() will
            // reload it at the point of use.  Otherwise, if it's not in a register, we load it here.

            if (!treeNode->InReg() && !(treeNode->gtFlags & GTF_SPILLED))
            {
                assert(!isRegCandidate);
                emit->emitIns_R_S(ins_Load(treeNode->TypeGet()), emitTypeSize(treeNode), treeNode->gtRegNum,
                                  lcl->gtLclNum, 0);
                genProduceReg(treeNode);
            }
        }
        break;

        case GT_LCL_FLD_ADDR:
        case GT_LCL_VAR_ADDR:
        {
            // Address of a local var.  This by itself should never be allocated a register.
            // If it is worth storing the address in a register then it should be cse'ed into
            // a temp and that would be allocated a register.
            noway_assert(targetType == TYP_BYREF);
            noway_assert(!treeNode->InReg());

            inst_RV_TT(INS_lea, targetReg, treeNode, 0, EA_BYREF);
        }
            genProduceReg(treeNode);
            break;

        case GT_LCL_FLD:
        {
            NYI_IF(targetType == TYP_STRUCT, "GT_LCL_FLD: struct load local field not supported");
            NYI_IF(treeNode->gtRegNum == REG_NA, "GT_LCL_FLD: load local field not into a register is not supported");

            emitAttr size   = emitTypeSize(targetType);
            unsigned offs   = treeNode->gtLclFld.gtLclOffs;
            unsigned varNum = treeNode->gtLclVarCommon.gtLclNum;
            assert(varNum < compiler->lvaCount);

            emit->emitIns_R_S(ins_Move_Extend(targetType, treeNode->InReg()), size, targetReg, varNum, offs);
        }
            genProduceReg(treeNode);
            break;

        case GT_STORE_LCL_FLD:
        {
            noway_assert(targetType != TYP_STRUCT);

            // record the offset
            unsigned offset = treeNode->gtLclFld.gtLclOffs;

            // We must have a stack store with GT_STORE_LCL_FLD
            noway_assert(!treeNode->InReg());
            noway_assert(targetReg == REG_NA);

            GenTreeLclVarCommon* varNode = treeNode->AsLclVarCommon();
            unsigned             varNum  = varNode->gtLclNum;
            assert(varNum < compiler->lvaCount);
            LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);

            // Ensure that lclVar nodes are typed correctly.
            assert(!varDsc->lvNormalizeOnStore() || targetType == genActualType(varDsc->TypeGet()));

            GenTreePtr  data = treeNode->gtOp.gtOp1->gtEffectiveVal();
            instruction ins  = ins_Store(targetType);
            emitAttr    attr = emitTypeSize(targetType);
            if (data->isContainedIntOrIImmed())
            {
                assert(data->IsIntegralConst(0));
                NYI_ARM("st.lclFld contained operand");
            }
            else
            {
                assert(!data->isContained());
                genConsumeReg(data);
                emit->emitIns_S_R(ins, attr, data->gtRegNum, varNum, offset);
            }

            genUpdateLife(varNode);
            varDsc->lvRegNum = REG_STK;
        }
        break;

        case GT_STORE_LCL_VAR:
        {
            GenTreeLclVarCommon* varNode = treeNode->AsLclVarCommon();

            unsigned varNum = varNode->gtLclNum;
            assert(varNum < compiler->lvaCount);
            LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);
            unsigned   offset = 0;

            // Ensure that lclVar nodes are typed correctly.
            assert(!varDsc->lvNormalizeOnStore() || targetType == genActualType(varDsc->TypeGet()));

            GenTreePtr data = treeNode->gtOp.gtOp1->gtEffectiveVal();

            // var = call, where call returns a multi-reg return value
            // case is handled separately.
            if (data->gtSkipReloadOrCopy()->IsMultiRegCall())
            {
                genMultiRegCallStoreToLocal(treeNode);
                break;
            }
            else
            {
                if (treeNode->TypeGet() == TYP_LONG)
                {
                    genStoreLongLclVar(treeNode);
                    break;
                }

                genConsumeRegs(data);

                regNumber dataReg = REG_NA;
                if (data->isContainedIntOrIImmed())
                {
                    assert(data->IsIntegralConst(0));
                    NYI_ARM("st.lclVar contained operand");
                }
                else
                {
                    assert(!data->isContained());
                    dataReg = data->gtRegNum;
                }
                assert(dataReg != REG_NA);

                if (targetReg == REG_NA) // store into stack based LclVar
                {
                    inst_set_SV_var(varNode);

                    instruction ins  = ins_Store(targetType);
                    emitAttr    attr = emitTypeSize(targetType);

                    emit->emitIns_S_R(ins, attr, dataReg, varNum, offset);

                    genUpdateLife(varNode);

                    varDsc->lvRegNum = REG_STK;
                }
                else // store into register (i.e move into register)
                {
                    if (dataReg != targetReg)
                    {
                        // Assign into targetReg when dataReg (from op1) is not the same register
                        inst_RV_RV(ins_Copy(targetType), targetReg, dataReg, targetType);
                    }
                    genProduceReg(treeNode);
                }
            }
        }
        break;

        case GT_RETFILT:
            // A void GT_RETFILT is the end of a finally. For non-void filter returns we need to load the result in
            // the return register, if it's not already there. The processing is the same as GT_RETURN.
            if (targetType != TYP_VOID)
            {
                // For filters, the IL spec says the result is type int32. Further, the only specified legal values
                // are 0 or 1, with the use of other values "undefined".
                assert(targetType == TYP_INT);
            }

            __fallthrough;

        case GT_RETURN:
            genReturn(treeNode);
            break;

        case GT_LEA:
        {
            // if we are here, it is the case where there is an LEA that cannot
            // be folded into a parent instruction
            GenTreeAddrMode* lea = treeNode->AsAddrMode();
            genLeaInstruction(lea);
        }
        // genLeaInstruction calls genProduceReg()
        break;

        case GT_IND:
            genConsumeAddress(treeNode->AsIndir()->Addr());
            emit->emitInsLoadStoreOp(ins_Load(targetType), emitTypeSize(treeNode), targetReg, treeNode->AsIndir());
            genProduceReg(treeNode);
            break;

        case GT_MOD:
        case GT_UDIV:
        case GT_UMOD:
            // We shouldn't be seeing GT_MOD on float/double args as it should get morphed into a
            // helper call by front-end.  Similarly we shouldn't be seeing GT_UDIV and GT_UMOD
            // on float/double args.
            noway_assert(!varTypeIsFloating(treeNode));
            __fallthrough;

        case GT_DIV:
        {
            genConsumeOperands(treeNode->AsOp());

            noway_assert(targetReg != REG_NA);

            GenTreePtr  dst    = treeNode;
            GenTreePtr  src1   = treeNode->gtGetOp1();
            GenTreePtr  src2   = treeNode->gtGetOp2();
            instruction ins    = genGetInsForOper(treeNode->OperGet(), targetType);
            emitAttr    attr   = emitTypeSize(treeNode);
            regNumber   result = REG_NA;

            // dst can only be a reg
            assert(!dst->isContained());

            // src can be only reg
            assert(!src1->isContained() || !src2->isContained());

            if (varTypeIsFloating(targetType))
            {
                // Floating point divide never raises an exception

                emit->emitIns_R_R_R(ins, attr, dst->gtRegNum, src1->gtRegNum, src2->gtRegNum);
            }
            else // an signed integer divide operation
            {
                // TODO-ARM-Bug: handle zero division exception.

                emit->emitIns_R_R_R(ins, attr, dst->gtRegNum, src1->gtRegNum, src2->gtRegNum);
            }

            genProduceReg(treeNode);
        }
        break;

        case GT_INTRINSIC:
        {
            genIntrinsic(treeNode);
        }
        break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        {
            // TODO-ARM-CQ: Check if we can use the currently set flags.
            // TODO-ARM-CQ: Check for the case where we can simply transfer the carry bit to a register
            //         (signed < or >= where targetReg != REG_NA)

            GenTreeOp* tree = treeNode->AsOp();
            GenTreePtr op1  = tree->gtOp1->gtEffectiveVal();
            GenTreePtr op2  = tree->gtOp2->gtEffectiveVal();

            genConsumeIfReg(op1);
            genConsumeIfReg(op2);

            instruction ins = INS_cmp;
            emitAttr    cmpAttr;
            if (varTypeIsFloating(op1))
            {
                assert(op1->TypeGet() == op2->TypeGet());
                ins     = INS_vcmp;
                cmpAttr = emitTypeSize(op1->TypeGet());
                emit->emitInsBinary(ins, cmpAttr, op1, op2);
                // vmrs with register 0xf has special meaning of transferring flags
                emit->emitIns_R(INS_vmrs, EA_4BYTE, REG_R15);
            }
            else if (varTypeIsLong(op1))
            {
#ifdef DEBUG
                // The result of an unlowered long compare on a 32-bit target must either be
                // a) materialized into a register, or
                // b) unused.
                //
                // A long compare that has a result that is used but not materialized into a register should
                // have been handled by Lowering::LowerCompare.

                LIR::Use use;
                assert((treeNode->gtRegNum != REG_NA) || !LIR::AsRange(compiler->compCurBB).TryGetUse(treeNode, &use));
#endif
                genCompareLong(treeNode);
                break;
            }
            else
            {
                var_types op1Type = op1->TypeGet();
                var_types op2Type = op2->TypeGet();
                assert(!varTypeIsFloating(op2Type));
                ins = INS_cmp;
                if (op1Type == op2Type)
                {
                    cmpAttr = emitTypeSize(op1Type);
                }
                else
                {
                    var_types cmpType    = TYP_INT;
                    bool      op1Is64Bit = (varTypeIsLong(op1Type) || op1Type == TYP_REF);
                    bool      op2Is64Bit = (varTypeIsLong(op2Type) || op2Type == TYP_REF);
                    NYI_IF(op1Is64Bit || op2Is64Bit, "Long compare");
                    assert(!op1->isUsedFromMemory() || op1Type == op2Type);
                    assert(!op2->isUsedFromMemory() || op1Type == op2Type);
                    cmpAttr = emitTypeSize(cmpType);
                }
                emit->emitInsBinary(ins, cmpAttr, op1, op2);
            }

            // Are we evaluating this into a register?
            if (targetReg != REG_NA)
            {
                genSetRegToCond(targetReg, tree);
                genProduceReg(tree);
            }
        }
        break;

        case GT_JTRUE:
        {
            GenTree* cmp = treeNode->gtOp.gtOp1->gtEffectiveVal();
            assert(cmp->OperIsCompare());
            assert(compiler->compCurBB->bbJumpKind == BBJ_COND);

            // Get the "kind" and type of the comparison.  Note that whether it is an unsigned cmp
            // is governed by a flag NOT by the inherent type of the node
            // TODO-ARM-CQ: Check if we can use the currently set flags.
            CompareKind compareKind = ((cmp->gtFlags & GTF_UNSIGNED) != 0) ? CK_UNSIGNED : CK_SIGNED;

            emitJumpKind jmpKind   = genJumpKindForOper(cmp->gtOper, compareKind);
            BasicBlock*  jmpTarget = compiler->compCurBB->bbJumpDest;

            inst_JMP(jmpKind, jmpTarget);
        }
        break;

        case GT_JCC:
        {
            GenTreeJumpCC* jcc = treeNode->AsJumpCC();

            assert(compiler->compCurBB->bbJumpKind == BBJ_COND);

            CompareKind  compareKind = ((jcc->gtFlags & GTF_UNSIGNED) != 0) ? CK_UNSIGNED : CK_SIGNED;
            emitJumpKind jumpKind    = genJumpKindForOper(jcc->gtCondition, compareKind);

            inst_JMP(jumpKind, compiler->compCurBB->bbJumpDest);
        }
        break;

        case GT_RETURNTRAP:
        {
            // this is nothing but a conditional call to CORINFO_HELP_STOP_FOR_GC
            // based on the contents of 'data'

            GenTree* data = treeNode->gtOp.gtOp1->gtEffectiveVal();
            genConsumeIfReg(data);
            GenTreeIntCon cns = intForm(TYP_INT, 0);
            emit->emitInsBinary(INS_cmp, emitTypeSize(TYP_INT), data, &cns);

            BasicBlock* skipLabel = genCreateTempLabel();

            emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
            inst_JMP(jmpEqual, skipLabel);
            // emit the call to the EE-helper that stops for GC (or other reasons)

            genEmitHelperCall(CORINFO_HELP_STOP_FOR_GC, 0, EA_UNKNOWN);
            genDefineTempLabel(skipLabel);
        }
        break;

        case GT_STOREIND:
        {
            GenTreeStoreInd* storeInd   = treeNode->AsStoreInd();
            GenTree*         data       = storeInd->Data();
            GenTree*         addr       = storeInd->Addr();
            var_types        targetType = storeInd->TypeGet();

            assert(!varTypeIsFloating(targetType) || (targetType == data->TypeGet()));

            GCInfo::WriteBarrierForm writeBarrierForm = gcInfo.gcIsWriteBarrierCandidate(treeNode, data);
            if (writeBarrierForm != GCInfo::WBF_NoBarrier)
            {
                // data and addr must be in registers.
                // Consume both registers so that any copies of interfering
                // registers are taken care of.
                genConsumeOperands(storeInd->AsOp());

#if NOGC_WRITE_BARRIERS
                NYI_ARM("NOGC_WRITE_BARRIERS");
#else
                // At this point, we should not have any interference.
                // That is, 'data' must not be in REG_ARG_0,
                //  as that is where 'addr' must go.
                noway_assert(data->gtRegNum != REG_ARG_0);

                // addr goes in REG_ARG_0
                if (addr->gtRegNum != REG_ARG_0)
                {
                    inst_RV_RV(INS_mov, REG_ARG_0, addr->gtRegNum, addr->TypeGet());
                }

                // data goes in REG_ARG_1
                if (data->gtRegNum != REG_ARG_1)
                {
                    inst_RV_RV(INS_mov, REG_ARG_1, data->gtRegNum, data->TypeGet());
                }
#endif // NOGC_WRITE_BARRIERS

                genGCWriteBarrier(storeInd, writeBarrierForm);
            }
            else // A normal store, not a WriteBarrier store
            {
                bool reverseOps  = ((storeInd->gtFlags & GTF_REVERSE_OPS) != 0);
                bool dataIsUnary = false;

                // We must consume the operands in the proper execution order,
                // so that liveness is updated appropriately.
                if (!reverseOps)
                {
                    genConsumeAddress(addr);
                }

                if (!data->isContained())
                {
                    genConsumeRegs(data);
                }

                if (reverseOps)
                {
                    genConsumeAddress(addr);
                }

                emit->emitInsLoadStoreOp(ins_Store(targetType), emitTypeSize(storeInd), data->gtRegNum,
                                         treeNode->AsIndir());
            }
        }
        break;

        case GT_COPY:
            // This is handled at the time we call genConsumeReg() on the GT_COPY
            break;

        case GT_LIST:
        case GT_FIELD_LIST:
        case GT_ARGPLACE:
            // Nothing to do
            break;

        case GT_PUTARG_STK:
            genPutArgStk(treeNode->AsPutArgStk());
            break;

        case GT_PUTARG_REG:
        {
            NYI_IF(targetType == TYP_STRUCT, "GT_PUTARG_REG: struct support not implemented");

            // commas show up here commonly, as part of a nullchk operation
            GenTree* op1 = treeNode->gtOp.gtOp1->gtEffectiveVal();
            // If child node is not already in the register we need, move it
            genConsumeReg(op1);
            if (treeNode->gtRegNum != op1->gtRegNum)
            {
                inst_RV_RV(ins_Move_Extend(targetType, true), treeNode->gtRegNum, op1->gtRegNum, targetType);
            }
        }
            genProduceReg(treeNode);
            break;

        case GT_CALL:
            genCallInstruction(treeNode->AsCall());
            break;

        case GT_LOCKADD:
        case GT_XCHG:
        case GT_XADD:
            genLockedInstructions(treeNode->AsOp());
            break;

        case GT_MEMORYBARRIER:
            instGen_MemoryBarrier();
            break;

        case GT_CMPXCHG:
        {
            NYI("GT_CMPXCHG");
        }
            genProduceReg(treeNode);
            break;

        case GT_RELOAD:
            // do nothing - reload is just a marker.
            // The parent node will call genConsumeReg on this which will trigger the unspill of this node's child
            // into the register specified in this node.
            break;

        case GT_NOP:
            break;

        case GT_NO_OP:
            if (treeNode->gtFlags & GTF_NO_OP_NO)
            {
                noway_assert(!"GTF_NO_OP_NO should not be set");
            }
            else
            {
                instGen(INS_nop);
            }
            break;

        case GT_ARR_BOUNDS_CHECK:
            genRangeCheck(treeNode);
            break;

        case GT_PHYSREG:
            if (treeNode->gtRegNum != treeNode->AsPhysReg()->gtSrcReg)
            {
                inst_RV_RV(INS_mov, treeNode->gtRegNum, treeNode->AsPhysReg()->gtSrcReg, targetType);

                genTransferRegGCState(treeNode->gtRegNum, treeNode->AsPhysReg()->gtSrcReg);
            }
            break;

        case GT_PHYSREGDST:
            break;

        case GT_NULLCHECK:
        {
            assert(!treeNode->gtOp.gtOp1->isContained());
            regNumber reg = genConsumeReg(treeNode->gtOp.gtOp1);
            emit->emitIns_AR_R(INS_cmp, EA_4BYTE, reg, reg, 0);
        }
        break;

        case GT_CATCH_ARG:

            noway_assert(handlerGetsXcptnObj(compiler->compCurBB->bbCatchTyp));

            /* Catch arguments get passed in a register. genCodeForBBlist()
               would have marked it as holding a GC object, but not used. */

            noway_assert(gcInfo.gcRegGCrefSetCur & RBM_EXCEPTION_OBJECT);
            genConsumeReg(treeNode);
            break;

        case GT_PINVOKE_PROLOG:
            noway_assert(((gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur) & ~fullIntArgRegMask()) == 0);

            // the runtime side requires the codegen here to be consistent
            emit->emitDisableRandomNops();
            break;

        case GT_LABEL:
            genPendingCallLabel       = genCreateTempLabel();
            treeNode->gtLabel.gtLabBB = genPendingCallLabel;
            emit->emitIns_J_R(INS_adr, EA_PTRSIZE, genPendingCallLabel, treeNode->gtRegNum);
            break;

        case GT_CLS_VAR_ADDR:
            emit->emitIns_R_C(INS_lea, EA_PTRSIZE, targetReg, treeNode->gtClsVar.gtClsVarHnd, 0);
            genProduceReg(treeNode);
            break;

        case GT_STORE_DYN_BLK:
        case GT_STORE_BLK:
            genCodeForStoreBlk(treeNode->AsBlk());
            break;

        case GT_JMPTABLE:
            genJumpTable(treeNode);
            break;

        case GT_SWITCH_TABLE:
            genTableBasedSwitch(treeNode);
            break;

        case GT_ARR_INDEX:
            genCodeForArrIndex(treeNode->AsArrIndex());
            break;

        case GT_ARR_OFFSET:
            genCodeForArrOffset(treeNode->AsArrOffs());
            break;

        case GT_IL_OFFSET:
            // Do nothing; these nodes are simply markers for debug info.
            break;

        default:
        {
#ifdef DEBUG
            char message[256];
            _snprintf_s(message, _countof(message), _TRUNCATE, "NYI: Unimplemented node type %s",
                        GenTree::NodeName(treeNode->OperGet()));
            NYIRAW(message);
#else
            NYI("unimplemented node");
#endif
        }
        break;
    }
}

//------------------------------------------------------------------------
// genLockedInstructions: Generate code for the locked operations.
//
// Notes:
//    Handles GT_LOCKADD, GT_XCHG, GT_XADD nodes.
//
void CodeGen::genLockedInstructions(GenTreeOp* treeNode)
{
    NYI("genLockedInstructions");
}

//----------------------------------------------------------------------------------
// genMultiRegCallStoreToLocal: store multi-reg return value of a call node to a local
//
// Arguments:
//    treeNode  -  Gentree of GT_STORE_LCL_VAR
//
// Return Value:
//    None
//
// Assumption:
//    The child of store is a multi-reg call node.
//    genProduceReg() on treeNode is made by caller of this routine.
//
void CodeGen::genMultiRegCallStoreToLocal(GenTreePtr treeNode)
{
    assert(treeNode->OperGet() == GT_STORE_LCL_VAR);

    // Longs are returned in two return registers on Arm32.
    assert(varTypeIsLong(treeNode));

    // Assumption: current Arm32 implementation requires that a multi-reg long
    // var in 'var = call' is flagged as lvIsMultiRegRet to prevent it from
    // being promoted.
    unsigned   lclNum = treeNode->AsLclVarCommon()->gtLclNum;
    LclVarDsc* varDsc = &(compiler->lvaTable[lclNum]);
    noway_assert(varDsc->lvIsMultiRegRet);

    GenTree*     op1       = treeNode->gtGetOp1();
    GenTree*     actualOp1 = op1->gtSkipReloadOrCopy();
    GenTreeCall* call      = actualOp1->AsCall();
    assert(call->HasMultiRegRetVal());

    genConsumeRegs(op1);

    ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
    unsigned        regCount    = retTypeDesc->GetReturnRegCount();
    assert(regCount <= MAX_RET_REG_COUNT);

    // Stack store
    int offset = 0;
    for (unsigned i = 0; i < regCount; ++i)
    {
        var_types type = retTypeDesc->GetReturnRegType(i);
        regNumber reg  = call->GetRegNumByIdx(i);
        if (op1->IsCopyOrReload())
        {
            // GT_COPY/GT_RELOAD will have valid reg for those positions
            // that need to be copied or reloaded.
            regNumber reloadReg = op1->AsCopyOrReload()->GetRegNumByIdx(i);
            if (reloadReg != REG_NA)
            {
                reg = reloadReg;
            }
        }

        assert(reg != REG_NA);
        getEmitter()->emitIns_S_R(ins_Store(type), emitTypeSize(type), reg, lclNum, offset);
        offset += genTypeSize(type);
    }

    varDsc->lvRegNum = REG_STK;
}

//--------------------------------------------------------------------------------------
// genLclHeap: Generate code for localloc
//
// Description:
//      There are 2 ways depending from build version to generate code for localloc:
//          1) For debug build where memory should be initialized we generate loop
//             which invoke push {tmpReg} N times.
//          2) Fore /o build  However, we tickle the pages to ensure that SP is always
//             valid and is in sync with the "stack guard page". Amount of iteration
//             is N/PAGE_SIZE.
//
// Comments:
//      There can be some optimization:
//          1) It's not needed to generate loop for zero size allocation
//          2) For small allocation (less than 4 store) we unroll loop
//          3) For allocation less than PAGE_SIZE and when it's not needed to initialize
//             memory to zero, we can just increment SP.
//
// Notes: Size N should be aligned to STACK_ALIGN before any allocation
//
void CodeGen::genLclHeap(GenTreePtr tree)
{
    assert(tree->OperGet() == GT_LCLHEAP);

    GenTreePtr size = tree->gtOp.gtOp1;
    noway_assert((genActualType(size->gtType) == TYP_INT) || (genActualType(size->gtType) == TYP_I_IMPL));

    // Result of localloc will be returned in regCnt.
    // Also it used as temporary register in code generation
    // for storing allocation size
    regNumber   regCnt          = tree->gtRegNum;
    regMaskTP   tmpRegsMask     = tree->gtRsvdRegs;
    regNumber   pspSymReg       = REG_NA;
    var_types   type            = genActualType(size->gtType);
    emitAttr    easz            = emitTypeSize(type);
    BasicBlock* endLabel        = nullptr;
    BasicBlock* loop            = nullptr;
    unsigned    stackAdjustment = 0;

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

    noway_assert(isFramePointerUsed()); // localloc requires Frame Pointer to be established since SP changes
    noway_assert(genStackLevel == 0);   // Can't have anything on the stack

    // Whether method has PSPSym.
    bool hasPspSym;
#if FEATURE_EH_FUNCLETS
    hasPspSym = (compiler->lvaPSPSym != BAD_VAR_NUM);
#else
    hasPspSym = false;
#endif

    // Check to 0 size allocations
    // size_t amount = 0;
    if (size->IsCnsIntOrI())
    {
        // If size is a constant, then it must be contained.
        assert(size->isContained());

        // If amount is zero then return null in regCnt
        size_t amount = size->gtIntCon.gtIconVal;
        if (amount == 0)
        {
            instGen_Set_Reg_To_Zero(EA_PTRSIZE, regCnt);
            goto BAILOUT;
        }
    }
    else
    {
        // If 0 bail out by returning null in regCnt
        genConsumeRegAndCopy(size, regCnt);
        endLabel = genCreateTempLabel();
        getEmitter()->emitIns_R_R(INS_TEST, easz, regCnt, regCnt);
        emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
        inst_JMP(jmpEqual, endLabel);
    }

    stackAdjustment = 0;
#if FEATURE_EH_FUNCLETS
    // If we have PSPsym, then need to re-locate it after localloc.
    if (hasPspSym)
    {
        stackAdjustment += STACK_ALIGN;

        // Save a copy of PSPSym
        assert(genCountBits(tmpRegsMask) >= 1);
        regMaskTP pspSymRegMask = genFindLowestBit(tmpRegsMask);
        tmpRegsMask &= ~pspSymRegMask;
        pspSymReg = genRegNumFromMask(pspSymRegMask);
        getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, pspSymReg, compiler->lvaPSPSym, 0);
    }
#endif

#if FEATURE_FIXED_OUT_ARGS
    // If we have an outgoing arg area then we must adjust the SP by popping off the
    // outgoing arg area. We will restore it right before we return from this method.
    if (compiler->lvaOutgoingArgSpaceSize > 0)
    {
        assert((compiler->lvaOutgoingArgSpaceSize % STACK_ALIGN) == 0); // This must be true for the stack to remain
                                                                        // aligned
        inst_RV_IV(INS_add, REG_SPBASE, compiler->lvaOutgoingArgSpaceSize, EA_PTRSIZE);
        stackAdjustment += compiler->lvaOutgoingArgSpaceSize;
    }
#endif

    // Put aligned allocation size to regCnt
    if (size->IsCnsIntOrI())
    {
        // 'amount' is the total number of bytes to localloc to properly STACK_ALIGN
        size_t amount = size->gtIntCon.gtIconVal;
        amount        = AlignUp(amount, STACK_ALIGN);

        // For small allocations we will generate up to four stp instructions
        size_t cntStackAlignedWidthItems = (amount >> STACK_ALIGN_SHIFT);
        if (cntStackAlignedWidthItems <= 4)
        {
            instGen_Set_Reg_To_Zero(EA_PTRSIZE, regCnt);

            while (cntStackAlignedWidthItems != 0)
            {
                inst_IV(INS_push, (unsigned)genRegMask(regCnt));
                cntStackAlignedWidthItems -= 1;
            }

            goto ALLOC_DONE;
        }
        else if (!compiler->info.compInitMem && (amount < compiler->eeGetPageSize())) // must be < not <=
        {
            // Since the size is a page or less, simply adjust the SP value
            // The SP might already be in the guard page, must touch it BEFORE
            // the alloc, not after.
            getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, regCnt, REG_SP, 0);
            inst_RV_IV(INS_sub, REG_SP, amount, EA_PTRSIZE);
            goto ALLOC_DONE;
        }

        // regCnt will be the total number of bytes to locAlloc
        genSetRegToIcon(regCnt, amount, ((int)amount == amount) ? TYP_INT : TYP_LONG);
    }
    else
    {
        // Round up the number of bytes to allocate to a STACK_ALIGN boundary.
        inst_RV_IV(INS_add, regCnt, (STACK_ALIGN - 1), emitActualTypeSize(type));
        inst_RV_IV(INS_AND, regCnt, ~(STACK_ALIGN - 1), emitActualTypeSize(type));
    }

    // Allocation
    if (compiler->info.compInitMem)
    {
        // At this point 'regCnt' is set to the total number of bytes to locAlloc.
        // Since we have to zero out the allocated memory AND ensure that RSP is always valid
        // by tickling the pages, we will just push 0's on the stack.

        assert(tmpRegsMask != RBM_NONE);
        assert(genCountBits(tmpRegsMask) >= 1);

        regMaskTP regCntMask = genFindLowestBit(tmpRegsMask);
        tmpRegsMask &= ~regCntMask;
        regNumber regTmp = genRegNumFromMask(regCntMask);
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, regTmp);

        // Loop:
        BasicBlock* loop = genCreateTempLabel();
        genDefineTempLabel(loop);

        noway_assert(STACK_ALIGN == 8);
        inst_IV(INS_push, (unsigned)genRegMask(regTmp));
        inst_IV(INS_push, (unsigned)genRegMask(regTmp));

        // If not done, loop
        // Note that regCnt is the number of bytes to stack allocate.
        assert(genIsValidIntReg(regCnt));
        getEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, regCnt, regCnt, STACK_ALIGN);
        emitJumpKind jmpNotEqual = genJumpKindForOper(GT_NE, CK_SIGNED);
        inst_JMP(jmpNotEqual, loop);
    }
    else
    {
        // At this point 'regCnt' is set to the total number of bytes to locAlloc.
        //
        // We don't need to zero out the allocated memory. However, we do have
        // to tickle the pages to ensure that SP is always valid and is
        // in sync with the "stack guard page".  Note that in the worst
        // case SP is on the last byte of the guard page.  Thus you must
        // touch SP+0 first not SP+0x1000.
        //
        // Another subtlety is that you don't want SP to be exactly on the
        // boundary of the guard page because PUSH is predecrement, thus
        // call setup would not touch the guard page but just beyond it
        //
        // Note that we go through a few hoops so that SP never points to
        // illegal pages at any time during the ticking process
        //
        //       subs  regCnt, SP, regCnt      // regCnt now holds ultimate SP
        //       jb    Loop                    // result is smaller than orignial SP (no wrap around)
        //       mov   regCnt, #0              // Overflow, pick lowest possible value
        //
        //  Loop:
        //       ldr   regTmp, [SP + 0]        // tickle the page - read from the page
        //       sub   regTmp, SP, PAGE_SIZE   // decrement SP by PAGE_SIZE
        //       cmp   regTmp, regCnt
        //       jb    Done
        //       mov   SP, regTmp
        //       j     Loop
        //
        //  Done:
        //       mov   SP, regCnt
        //

        // Setup the regTmp
        assert(tmpRegsMask != RBM_NONE);
        assert(genCountBits(tmpRegsMask) == 1);
        regNumber regTmp = genRegNumFromMask(tmpRegsMask);

        BasicBlock* loop = genCreateTempLabel();
        BasicBlock* done = genCreateTempLabel();

        //       subs  regCnt, SP, regCnt      // regCnt now holds ultimate SP
        getEmitter()->emitIns_R_R_R(INS_sub, EA_PTRSIZE, regCnt, REG_SPBASE, regCnt);

        inst_JMP(EJ_vc, loop); // branch if the V flag is not set

        // Ups... Overflow, set regCnt to lowest possible value
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, regCnt);

        genDefineTempLabel(loop);

        // tickle the page - Read from the updated SP - this triggers a page fault when on the guard page
        getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, regTmp, REG_SPBASE, 0);

        // decrement SP by PAGE_SIZE
        getEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, regTmp, REG_SPBASE, compiler->eeGetPageSize());

        getEmitter()->emitIns_R_R(INS_cmp, EA_PTRSIZE, regTmp, regCnt);
        emitJumpKind jmpLTU = genJumpKindForOper(GT_LT, CK_UNSIGNED);
        inst_JMP(jmpLTU, done);

        // Update SP to be at the next page of stack that we will tickle
        getEmitter()->emitIns_R_R(INS_mov, EA_PTRSIZE, REG_SPBASE, regCnt);

        // Jump to loop and tickle new stack address
        inst_JMP(EJ_jmp, loop);

        // Done with stack tickle loop
        genDefineTempLabel(done);

        // Now just move the final value to SP
        getEmitter()->emitIns_R_R(INS_mov, EA_PTRSIZE, REG_SPBASE, regCnt);
    }

ALLOC_DONE:
    // Re-adjust SP to allocate PSPSym and out-going arg area
    if (stackAdjustment != 0)
    {
        assert((stackAdjustment % STACK_ALIGN) == 0); // This must be true for the stack to remain aligned
        assert(stackAdjustment > 0);
        getEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, (int)stackAdjustment);

#if FEATURE_EH_FUNCLETS
        // Write PSPSym to its new location.
        if (hasPspSym)
        {
            assert(genIsValidIntReg(pspSymReg));
            getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, pspSymReg, compiler->lvaPSPSym, 0);
        }
#endif
        // Return the stackalloc'ed address in result register.
        // regCnt = RSP + stackAdjustment.
        getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, regCnt, REG_SPBASE, (int)stackAdjustment);
    }
    else // stackAdjustment == 0
    {
        // Move the final value of SP to regCnt
        inst_RV_RV(INS_mov, regCnt, REG_SPBASE);
    }

BAILOUT:
    if (endLabel != nullptr)
        genDefineTempLabel(endLabel);

    // Write the lvaLocAllocSPvar stack frame slot
    if (compiler->lvaLocAllocSPvar != BAD_VAR_NUM)
    {
        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, regCnt, compiler->lvaLocAllocSPvar, 0);
    }

#if STACK_PROBES
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
        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, regCnt, compiler->lvaReturnEspCheck, 0);
    }
#endif

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genTableBasedSwitch: generate code for a switch statement based on a table of ip-relative offsets
//
void CodeGen::genTableBasedSwitch(GenTree* treeNode)
{
    genConsumeOperands(treeNode->AsOp());
    regNumber idxReg  = treeNode->gtOp.gtOp1->gtRegNum;
    regNumber baseReg = treeNode->gtOp.gtOp2->gtRegNum;

    getEmitter()->emitIns_R_ARX(INS_ldr, EA_4BYTE, REG_PC, baseReg, idxReg, TARGET_POINTER_SIZE, 0);
}

//------------------------------------------------------------------------
// genJumpTable: emits the table and an instruction to get the address of the first element
//
void CodeGen::genJumpTable(GenTree* treeNode)
{
    noway_assert(compiler->compCurBB->bbJumpKind == BBJ_SWITCH);
    assert(treeNode->OperGet() == GT_JMPTABLE);

    unsigned     jumpCount = compiler->compCurBB->bbJumpSwt->bbsCount;
    BasicBlock** jumpTable = compiler->compCurBB->bbJumpSwt->bbsDstTab;
    unsigned     jmpTabBase;

    jmpTabBase = getEmitter()->emitBBTableDataGenBeg(jumpCount, false);

    JITDUMP("\n      J_M%03u_DS%02u LABEL   DWORD\n", Compiler::s_compMethodsCount, jmpTabBase);

    for (unsigned i = 0; i < jumpCount; i++)
    {
        BasicBlock* target = *jumpTable++;
        noway_assert(target->bbFlags & BBF_JMP_TARGET);

        JITDUMP("            DD      L_M%03u_BB%02u\n", Compiler::s_compMethodsCount, target->bbNum);

        getEmitter()->emitDataGenData(i, target);
    }

    getEmitter()->emitDataGenEnd();

    getEmitter()->emitIns_R_D(INS_movw, EA_HANDLE_CNS_RELOC, jmpTabBase, treeNode->gtRegNum);
    getEmitter()->emitIns_R_D(INS_movt, EA_HANDLE_CNS_RELOC, jmpTabBase, treeNode->gtRegNum);

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genRangeCheck: generate code for GT_ARR_BOUNDS_CHECK node.
//
void CodeGen::genRangeCheck(GenTreePtr oper)
{
    noway_assert(oper->OperGet() == GT_ARR_BOUNDS_CHECK);
    GenTreeBoundsChk* bndsChk = oper->AsBoundsChk();

    GenTreePtr arrIdx    = bndsChk->gtIndex->gtEffectiveVal();
    GenTreePtr arrLen    = bndsChk->gtArrLen->gtEffectiveVal();
    GenTreePtr arrRef    = NULL;
    int        lenOffset = 0;

    genConsumeIfReg(arrIdx);
    genConsumeIfReg(arrLen);

    GenTree *    src1, *src2;
    emitJumpKind jmpKind;

    if (arrIdx->isContainedIntOrIImmed())
    {
        // To encode using a cmp immediate, we place the
        //  constant operand in the second position
        src1    = arrLen;
        src2    = arrIdx;
        jmpKind = genJumpKindForOper(GT_LE, CK_UNSIGNED);
    }
    else
    {
        src1    = arrIdx;
        src2    = arrLen;
        jmpKind = genJumpKindForOper(GT_GE, CK_UNSIGNED);
    }

    getEmitter()->emitInsBinary(INS_cmp, emitAttr(TYP_INT), src1, src2);
    genJumpToThrowHlpBlk(jmpKind, SCK_RNGCHK_FAIL, bndsChk->gtIndRngFailBB);
}

//------------------------------------------------------------------------
// genOffsetOfMDArrayLowerBound: Returns the offset from the Array object to the
//   lower bound for the given dimension.
//
// Arguments:
//    elemType  - the element type of the array
//    rank      - the rank of the array
//    dimension - the dimension for which the lower bound offset will be returned.
//
// Return Value:
//    The offset.
// TODO-Cleanup: move to CodeGenCommon.cpp

// static
unsigned CodeGen::genOffsetOfMDArrayLowerBound(var_types elemType, unsigned rank, unsigned dimension)
{
    // Note that the lower bound and length fields of the Array object are always TYP_INT
    return compiler->eeGetArrayDataOffset(elemType) + genTypeSize(TYP_INT) * (dimension + rank);
}

//------------------------------------------------------------------------
// genOffsetOfMDArrayLength: Returns the offset from the Array object to the
//   size for the given dimension.
//
// Arguments:
//    elemType  - the element type of the array
//    rank      - the rank of the array
//    dimension - the dimension for which the lower bound offset will be returned.
//
// Return Value:
//    The offset.
// TODO-Cleanup: move to CodeGenCommon.cpp

// static
unsigned CodeGen::genOffsetOfMDArrayDimensionSize(var_types elemType, unsigned rank, unsigned dimension)
{
    // Note that the lower bound and length fields of the Array object are always TYP_INT
    return compiler->eeGetArrayDataOffset(elemType) + genTypeSize(TYP_INT) * dimension;
}

//------------------------------------------------------------------------
// genCodeForArrIndex: Generates code to bounds check the index for one dimension of an array reference,
//                     producing the effective index by subtracting the lower bound.
//
// Arguments:
//    arrIndex - the node for which we're generating code
//
// Return Value:
//    None.
//

void CodeGen::genCodeForArrIndex(GenTreeArrIndex* arrIndex)
{
    emitter*   emit      = getEmitter();
    GenTreePtr arrObj    = arrIndex->ArrObj();
    GenTreePtr indexNode = arrIndex->IndexExpr();
    regNumber  arrReg    = genConsumeReg(arrObj);
    regNumber  indexReg  = genConsumeReg(indexNode);
    regNumber  tgtReg    = arrIndex->gtRegNum;
    noway_assert(tgtReg != REG_NA);

    // We will use a temp register to load the lower bound and dimension size values
    //
    regMaskTP tmpRegsMask = arrIndex->gtRsvdRegs; // there will be two bits set
    tmpRegsMask &= ~genRegMask(tgtReg);           // remove the bit for 'tgtReg' from 'tmpRegsMask'

    regMaskTP tmpRegMask = genFindLowestBit(tmpRegsMask); // set tmpRegMsk to a one-bit mask
    regNumber tmpReg     = genRegNumFromMask(tmpRegMask); // set tmpReg from that mask
    noway_assert(tmpReg != REG_NA);

    assert(tgtReg != tmpReg);

    unsigned  dim      = arrIndex->gtCurrDim;
    unsigned  rank     = arrIndex->gtArrRank;
    var_types elemType = arrIndex->gtArrElemType;
    unsigned  offset;

    offset = genOffsetOfMDArrayLowerBound(elemType, rank, dim);
    emit->emitIns_R_R_I(ins_Load(TYP_INT), EA_4BYTE, tmpReg, arrReg, offset); // a 4 BYTE sign extending load
    emit->emitIns_R_R_R(INS_sub, EA_4BYTE, tgtReg, indexReg, tmpReg);

    offset = genOffsetOfMDArrayDimensionSize(elemType, rank, dim);
    emit->emitIns_R_R_I(ins_Load(TYP_INT), EA_4BYTE, tmpReg, arrReg, offset); // a 4 BYTE sign extending load
    emit->emitIns_R_R(INS_cmp, EA_4BYTE, tgtReg, tmpReg);

    emitJumpKind jmpGEU = genJumpKindForOper(GT_GE, CK_UNSIGNED);
    genJumpToThrowHlpBlk(jmpGEU, SCK_RNGCHK_FAIL);

    genProduceReg(arrIndex);
}

//------------------------------------------------------------------------
// genCodeForArrOffset: Generates code to compute the flattened array offset for
//    one dimension of an array reference:
//        result = (prevDimOffset * dimSize) + effectiveIndex
//    where dimSize is obtained from the arrObj operand
//
// Arguments:
//    arrOffset - the node for which we're generating code
//
// Return Value:
//    None.
//
// Notes:
//    dimSize and effectiveIndex are always non-negative, the former by design,
//    and the latter because it has been normalized to be zero-based.

void CodeGen::genCodeForArrOffset(GenTreeArrOffs* arrOffset)
{
    GenTreePtr offsetNode = arrOffset->gtOffset;
    GenTreePtr indexNode  = arrOffset->gtIndex;
    regNumber  tgtReg     = arrOffset->gtRegNum;

    noway_assert(tgtReg != REG_NA);

    if (!offsetNode->IsIntegralConst(0))
    {
        emitter*  emit      = getEmitter();
        regNumber offsetReg = genConsumeReg(offsetNode);
        noway_assert(offsetReg != REG_NA);
        regNumber indexReg = genConsumeReg(indexNode);
        noway_assert(indexReg != REG_NA);
        GenTreePtr arrObj = arrOffset->gtArrObj;
        regNumber  arrReg = genConsumeReg(arrObj);
        noway_assert(arrReg != REG_NA);
        regMaskTP tmpRegMask = arrOffset->gtRsvdRegs;
        regNumber tmpReg     = genRegNumFromMask(tmpRegMask);
        noway_assert(tmpReg != REG_NA);
        unsigned  dim      = arrOffset->gtCurrDim;
        unsigned  rank     = arrOffset->gtArrRank;
        var_types elemType = arrOffset->gtArrElemType;
        unsigned  offset   = genOffsetOfMDArrayDimensionSize(elemType, rank, dim);

        // Load tmpReg with the dimension size
        emit->emitIns_R_R_I(ins_Load(TYP_INT), EA_4BYTE, tmpReg, arrReg, offset); // a 4 BYTE sign extending load

        // Evaluate tgtReg = offsetReg*dim_size + indexReg.
        emit->emitIns_R_R_R(INS_MUL, EA_4BYTE, tgtReg, tmpReg, offsetReg);
        emit->emitIns_R_R_R(INS_add, EA_4BYTE, tgtReg, tgtReg, indexReg);
    }
    else
    {
        regNumber indexReg = genConsumeReg(indexNode);
        if (indexReg != tgtReg)
        {
            inst_RV_RV(INS_mov, tgtReg, indexReg, TYP_INT);
        }
    }
    genProduceReg(arrOffset);
}

//------------------------------------------------------------------------
// indirForm: Make a temporary indir we can feed to pattern matching routines
//    in cases where we don't want to instantiate all the indirs that happen.
//
GenTreeIndir CodeGen::indirForm(var_types type, GenTree* base)
{
    GenTreeIndir i(GT_IND, type, base, nullptr);
    i.gtRegNum = REG_NA;
    // has to be nonnull (because contained nodes can't be the last in block)
    // but don't want it to be a valid pointer
    i.gtNext = (GenTree*)(-1);
    return i;
}

//------------------------------------------------------------------------
// intForm: Make a temporary int we can feed to pattern matching routines
//    in cases where we don't want to instantiate.
//
GenTreeIntCon CodeGen::intForm(var_types type, ssize_t value)
{
    GenTreeIntCon i(type, value);
    i.gtRegNum = REG_NA;
    // has to be nonnull (because contained nodes can't be the last in block)
    // but don't want it to be a valid pointer
    i.gtNext = (GenTree*)(-1);
    return i;
}

//------------------------------------------------------------------------
// genGetInsForOper: Return instruction encoding of the operation tree.
//
instruction CodeGen::genGetInsForOper(genTreeOps oper, var_types type)
{
    instruction ins;

    if (varTypeIsFloating(type))
        return CodeGen::ins_MathOp(oper, type);

    switch (oper)
    {
        case GT_ADD:
            ins = INS_add;
            break;
        case GT_AND:
            ins = INS_AND;
            break;
        case GT_MUL:
            ins = INS_MUL;
            break;
        case GT_DIV:
            ins = INS_sdiv;
            break;
        case GT_LSH:
            ins = INS_SHIFT_LEFT_LOGICAL;
            break;
        case GT_NEG:
            ins = INS_rsb;
            break;
        case GT_NOT:
            ins = INS_NOT;
            break;
        case GT_OR:
            ins = INS_OR;
            break;
        case GT_RSH:
            ins = INS_SHIFT_RIGHT_ARITHM;
            break;
        case GT_RSZ:
            ins = INS_SHIFT_RIGHT_LOGICAL;
            break;
        case GT_SUB:
            ins = INS_sub;
            break;
        case GT_XOR:
            ins = INS_XOR;
            break;
        case GT_ROR:
            ins = INS_ror;
            break;
        case GT_ADD_LO:
            ins = INS_add;
            break;
        case GT_ADD_HI:
            ins = INS_adc;
            break;
        case GT_SUB_LO:
            ins = INS_sub;
            break;
        case GT_SUB_HI:
            ins = INS_sbc;
            break;
        case GT_LSH_HI:
            ins = INS_SHIFT_LEFT_LOGICAL;
            break;
        case GT_RSH_LO:
            ins = INS_SHIFT_RIGHT_LOGICAL;
            break;
        default:
            unreached();
            break;
    }
    return ins;
}

//------------------------------------------------------------------------
// genCodeForShift: Generates the code sequence for a GenTree node that
// represents a bit shift or rotate operation (<<, >>, >>>, rol, ror).
//
// Arguments:
//    tree - the bit shift node (that specifies the type of bit shift to perform).
//
// Assumptions:
//    a) All GenTrees are register allocated.
//
void CodeGen::genCodeForShift(GenTreePtr tree)
{
    var_types   targetType = tree->TypeGet();
    genTreeOps  oper       = tree->OperGet();
    instruction ins        = genGetInsForOper(oper, targetType);
    emitAttr    size       = emitTypeSize(tree);

    assert(tree->gtRegNum != REG_NA);

    genConsumeOperands(tree->AsOp());

    GenTreePtr operand = tree->gtGetOp1();
    GenTreePtr shiftBy = tree->gtGetOp2();
    if (!shiftBy->IsCnsIntOrI())
    {
        getEmitter()->emitIns_R_R_R(ins, size, tree->gtRegNum, operand->gtRegNum, shiftBy->gtRegNum);
    }
    else
    {
        unsigned immWidth   = size * BITS_PER_BYTE;
        ssize_t  shiftByImm = shiftBy->gtIntCon.gtIconVal & (immWidth - 1);

        getEmitter()->emitIns_R_R_I(ins, size, tree->gtRegNum, operand->gtRegNum, shiftByImm);
    }

    genProduceReg(tree);
}

// Generate code for a CpBlk node by the means of the VM memcpy helper call
// Preconditions:
// a) The size argument of the CpBlk is not an integer constant
// b) The size argument is a constant but is larger than CPBLK_MOVS_LIMIT bytes.
void CodeGen::genCodeForCpBlk(GenTreeBlk* cpBlkNode)
{
    // Make sure we got the arguments of the cpblk operation in the right registers
    unsigned   blockSize = cpBlkNode->Size();
    GenTreePtr dstAddr   = cpBlkNode->Addr();
    assert(!dstAddr->isContained());

    genConsumeBlockOp(cpBlkNode, REG_ARG_0, REG_ARG_1, REG_ARG_2);
    genEmitHelperCall(CORINFO_HELP_MEMCPY, 0, EA_UNKNOWN);
}

// Generates CpBlk code by performing a loop unroll
// Preconditions:
//  The size argument of the CpBlk node is a constant and <= 64 bytes.
//  This may seem small but covers >95% of the cases in several framework assemblies.
void CodeGen::genCodeForCpBlkUnroll(GenTreeBlk* cpBlkNode)
{
    NYI_ARM("genCodeForCpBlkUnroll");
}

// Generate code for InitBlk by performing a loop unroll
// Preconditions:
//   a) Both the size and fill byte value are integer constants.
//   b) The size of the struct to initialize is smaller than INITBLK_UNROLL_LIMIT bytes.
void CodeGen::genCodeForInitBlkUnroll(GenTreeBlk* initBlkNode)
{
    NYI_ARM("genCodeForInitBlkUnroll");
}

void CodeGen::genCodeForStoreBlk(GenTreeBlk* blkOp)
{
    if (blkOp->gtBlkOpGcUnsafe)
    {
        getEmitter()->emitDisableGC();
    }
    bool isCopyBlk = blkOp->OperIsCopyBlkOp();

    switch (blkOp->gtBlkOpKind)
    {
        case GenTreeBlk::BlkOpKindHelper:
            if (isCopyBlk)
            {
                genCodeForCpBlk(blkOp);
            }
            else
            {
                genCodeForInitBlk(blkOp);
            }
            break;
        case GenTreeBlk::BlkOpKindUnroll:
            if (isCopyBlk)
            {
                genCodeForCpBlkUnroll(blkOp);
            }
            else
            {
                genCodeForInitBlkUnroll(blkOp);
            }
            break;
        default:
            unreached();
    }
    if (blkOp->gtBlkOpGcUnsafe)
    {
        getEmitter()->emitEnableGC();
    }
}

// Generates code for InitBlk by calling the VM memset helper function.
// Preconditions:
// a) The size argument of the InitBlk is not an integer constant.
// b) The size argument of the InitBlk is >= INITBLK_STOS_LIMIT bytes.
void CodeGen::genCodeForInitBlk(GenTreeBlk* initBlkNode)
{
    // Make sure we got the arguments of the initblk operation in the right registers
    unsigned   size    = initBlkNode->Size();
    GenTreePtr dstAddr = initBlkNode->Addr();
    GenTreePtr initVal = initBlkNode->Data();
    if (initVal->OperIsInitVal())
    {
        initVal = initVal->gtGetOp1();
    }

    assert(!dstAddr->isContained());
    assert(!initVal->isContained());
    if (initBlkNode->gtOper == GT_STORE_DYN_BLK)
    {
        assert(initBlkNode->AsDynBlk()->gtDynamicSize->gtRegNum == REG_ARG_2);
    }
    else
    {
        assert(initBlkNode->gtRsvdRegs == RBM_ARG_2);
    }

    genConsumeBlockOp(initBlkNode, REG_ARG_0, REG_ARG_1, REG_ARG_2);
    genEmitHelperCall(CORINFO_HELP_MEMSET, 0, EA_UNKNOWN);
}

//------------------------------------------------------------------------
// genCodeForShiftLong: Generates the code sequence for a GenTree node that
// represents a three operand bit shift or rotate operation (<<Hi, >>Lo).
//
// Arguments:
//    tree - the bit shift node (that specifies the type of bit shift to perform).
//
// Assumptions:
//    a) All GenTrees are register allocated.
//    b) The shift-by-amount in tree->gtOp.gtOp2 is a contained constant
//
void CodeGen::genCodeForShiftLong(GenTreePtr tree)
{
    // Only the non-RMW case here.
    genTreeOps oper = tree->OperGet();
    assert(oper == GT_LSH_HI || oper == GT_RSH_LO);

    GenTree* operand = tree->gtOp.gtOp1;
    assert(operand->OperGet() == GT_LONG);
    assert(operand->gtOp.gtOp1->isUsedFromReg());
    assert(operand->gtOp.gtOp2->isUsedFromReg());

    GenTree* operandLo = operand->gtGetOp1();
    GenTree* operandHi = operand->gtGetOp2();

    regNumber regLo = operandLo->gtRegNum;
    regNumber regHi = operandHi->gtRegNum;

    genConsumeOperands(tree->AsOp());

    var_types   targetType = tree->TypeGet();
    instruction ins        = genGetInsForOper(oper, targetType);

    GenTreePtr shiftBy = tree->gtGetOp2();

    assert(shiftBy->isContainedIntOrIImmed());

    unsigned int count = shiftBy->AsIntConCommon()->IconValue();

    regNumber regResult = (oper == GT_LSH_HI) ? regHi : regLo;

    if (regResult != tree->gtRegNum)
    {
        inst_RV_RV(INS_mov, tree->gtRegNum, regResult, targetType);
    }

    if (oper == GT_LSH_HI)
    {
        inst_RV_SH(ins, EA_4BYTE, tree->gtRegNum, count);
        getEmitter()->emitIns_R_R_R_I(INS_OR, EA_4BYTE, tree->gtRegNum, tree->gtRegNum, regLo, 32 - count,
                                      INS_FLAGS_DONT_CARE, INS_OPTS_LSR);
    }
    else
    {
        assert(oper == GT_RSH_LO);
        inst_RV_SH(INS_SHIFT_RIGHT_LOGICAL, EA_4BYTE, tree->gtRegNum, count);
        getEmitter()->emitIns_R_R_R_I(INS_OR, EA_4BYTE, tree->gtRegNum, tree->gtRegNum, regHi, 32 - count,
                                      INS_FLAGS_DONT_CARE, INS_OPTS_LSL);
    }

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genRegCopy: Generate a register copy.
//
void CodeGen::genRegCopy(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_COPY);

    var_types targetType = treeNode->TypeGet();
    regNumber targetReg  = treeNode->gtRegNum;
    assert(targetReg != REG_NA);

    GenTree* op1 = treeNode->gtOp.gtOp1;

    // Check whether this node and the node from which we're copying the value have the same
    // register type.
    // This can happen if (currently iff) we have a SIMD vector type that fits in an integer
    // register, in which case it is passed as an argument, or returned from a call,
    // in an integer register and must be copied if it's in an xmm register.

    if (varTypeIsFloating(treeNode) != varTypeIsFloating(op1))
    {
        NYI("genRegCopy floating point");
    }
    else
    {
        inst_RV_RV(ins_Copy(targetType), targetReg, genConsumeReg(op1), targetType);
    }

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
            LclVarDsc* varDsc = &compiler->lvaTable[lcl->gtLclNum];

            // If we didn't just spill it (in genConsumeReg, above), then update the register info
            if (varDsc->lvRegNum != REG_STK)
            {
                // The old location is dying
                genUpdateRegLife(varDsc, /*isBorn*/ false, /*isDying*/ true DEBUGARG(op1));

                gcInfo.gcMarkRegSetNpt(genRegMask(op1->gtRegNum));

                genUpdateVarReg(varDsc, treeNode);

                // The new location is going live
                genUpdateRegLife(varDsc, /*isBorn*/ true, /*isDying*/ false DEBUGARG(treeNode));
            }
        }
    }

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCallInstruction: Produce code for a GT_CALL node
//
void CodeGen::genCallInstruction(GenTreeCall* call)
{
    gtCallTypes callType = (gtCallTypes)call->gtCallType;

    IL_OFFSETX ilOffset = BAD_IL_OFFSET;

    // all virtuals should have been expanded into a control expression
    assert(!call->IsVirtual() || call->gtControlExpr || call->gtCallAddr);

    // Consume all the arg regs
    for (GenTreePtr list = call->gtCallLateArgs; list; list = list->MoveNext())
    {
        assert(list->OperIsList());

        GenTreePtr argNode = list->Current();

        fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(call, argNode->gtSkipReloadOrCopy());
        assert(curArgTabEntry);

        if (curArgTabEntry->regNum == REG_STK)
            continue;

        // Deal with multi register passed struct args.
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            GenTreeArgList* argListPtr   = argNode->AsArgList();
            unsigned        iterationNum = 0;
            regNumber       argReg       = curArgTabEntry->regNum;
            for (; argListPtr != nullptr; argListPtr = argListPtr->Rest(), iterationNum++)
            {
                GenTreePtr putArgRegNode = argListPtr->gtOp.gtOp1;
                assert(putArgRegNode->gtOper == GT_PUTARG_REG);

                genConsumeReg(putArgRegNode);

                if (putArgRegNode->gtRegNum != argReg)
                {
                    inst_RV_RV(ins_Move_Extend(putArgRegNode->TypeGet(), putArgRegNode->InReg()), argReg,
                               putArgRegNode->gtRegNum);
                }

                argReg = genRegArgNext(argReg);
            }
        }
        else
        {
            regNumber argReg = curArgTabEntry->regNum;
            genConsumeReg(argNode);
            if (argNode->gtRegNum != argReg)
            {
                inst_RV_RV(ins_Move_Extend(argNode->TypeGet(), argNode->InReg()), argReg, argNode->gtRegNum);
            }
        }

        // In the case of a varargs call,
        // the ABI dictates that if we have floating point args,
        // we must pass the enregistered arguments in both the
        // integer and floating point registers so, let's do that.
        if (call->IsVarargs() && varTypeIsFloating(argNode))
        {
            NYI_ARM("CodeGen - IsVarargs");
        }
    }

    // Insert a null check on "this" pointer if asked.
    if (call->NeedsNullCheck())
    {
        const regNumber regThis  = genGetThisArgReg(call);
        regMaskTP       tempMask = genFindLowestBit(call->gtRsvdRegs);
        const regNumber tmpReg   = genRegNumFromMask(tempMask);
        if (genCountBits(call->gtRsvdRegs) > 1)
        {
            call->gtRsvdRegs &= ~tempMask;
        }
        getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, tmpReg, regThis, 0);
    }

    // Either gtControlExpr != null or gtCallAddr != null or it is a direct non-virtual call to a user or helper method.
    CORINFO_METHOD_HANDLE methHnd;
    GenTree*              target = call->gtControlExpr;
    if (callType == CT_INDIRECT)
    {
        assert(target == nullptr);
        target  = call->gtCallAddr;
        methHnd = nullptr;
    }
    else
    {
        methHnd = call->gtCallMethHnd;
    }

    CORINFO_SIG_INFO* sigInfo = nullptr;
#ifdef DEBUG
    // Pass the call signature information down into the emitter so the emitter can associate
    // native call sites with the signatures they were generated from.
    if (callType != CT_HELPER)
    {
        sigInfo = call->callSig;
    }
#endif // DEBUG

    // If fast tail call, then we are done.
    if (call->IsFastTailCall())
    {
        NYI_ARM("fast tail call");
    }

    // For a pinvoke to unmanaged code we emit a label to clear
    // the GC pointer state before the callsite.
    // We can't utilize the typical lazy killing of GC pointers
    // at (or inside) the callsite.
    if (call->IsUnmanaged())
    {
        genDefineTempLabel(genCreateTempLabel());
    }

    // Determine return value size(s).
    ReturnTypeDesc* pRetTypeDesc  = call->GetReturnTypeDesc();
    emitAttr        retSize       = EA_PTRSIZE;
    emitAttr        secondRetSize = EA_UNKNOWN;

    if (call->HasMultiRegRetVal())
    {
        retSize       = emitTypeSize(pRetTypeDesc->GetReturnRegType(0));
        secondRetSize = emitTypeSize(pRetTypeDesc->GetReturnRegType(1));
    }
    else
    {
        assert(!varTypeIsStruct(call));

        if (call->gtType == TYP_REF || call->gtType == TYP_ARRAY)
        {
            retSize = EA_GCREF;
        }
        else if (call->gtType == TYP_BYREF)
        {
            retSize = EA_BYREF;
        }
    }

    // We need to propagate the IL offset information to the call instruction, so we can emit
    // an IL to native mapping record for the call, to support managed return value debugging.
    // We don't want tail call helper calls that were converted from normal calls to get a record,
    // so we skip this hash table lookup logic in that case.
    if (compiler->opts.compDbgInfo && compiler->genCallSite2ILOffsetMap != nullptr && !call->IsTailCall())
    {
        (void)compiler->genCallSite2ILOffsetMap->Lookup(call, &ilOffset);
    }

    if (target != nullptr)
    {
        // For ARM a call target can not be a contained indirection
        assert(!target->isContainedIndir());

        genConsumeReg(target);

        // We have already generated code for gtControlExpr evaluating it into a register.
        // We just need to emit "call reg" in this case.
        //
        assert(genIsValidIntReg(target->gtRegNum));

        genEmitCall(emitter::EC_INDIR_R, methHnd,
                    INDEBUG_LDISASM_COMMA(sigInfo) nullptr, // addr
                    retSize, ilOffset, target->gtRegNum);
    }
    else
    {
        // Generate a direct call to a non-virtual user defined or helper method
        assert(callType == CT_HELPER || callType == CT_USER_FUNC);

        void* addr = nullptr;
        if (callType == CT_HELPER)
        {
            // Direct call to a helper method.
            CorInfoHelpFunc helperNum = compiler->eeGetHelperNum(methHnd);
            noway_assert(helperNum != CORINFO_HELP_UNDEF);

            void* pAddr = nullptr;
            addr        = compiler->compGetHelperFtn(helperNum, (void**)&pAddr);

            if (addr == nullptr)
            {
                addr = pAddr;
            }
        }
        else
        {
            // Direct call to a non-virtual user function.
            CORINFO_ACCESS_FLAGS aflags = CORINFO_ACCESS_ANY;
            if (call->IsSameThis())
            {
                aflags = (CORINFO_ACCESS_FLAGS)(aflags | CORINFO_ACCESS_THIS);
            }

            if ((call->NeedsNullCheck()) == 0)
            {
                aflags = (CORINFO_ACCESS_FLAGS)(aflags | CORINFO_ACCESS_NONNULL);
            }

            CORINFO_CONST_LOOKUP addrInfo;
            compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &addrInfo, aflags);

            addr = addrInfo.addr;
        }

        assert(addr);
        // Non-virtual direct call to known addresses
        if (!arm_Valid_Imm_For_BL((ssize_t)addr))
        {
            regNumber tmpReg = genRegNumFromMask(call->gtRsvdRegs);
            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, tmpReg, (ssize_t)addr);
            genEmitCall(emitter::EC_INDIR_R, methHnd, INDEBUG_LDISASM_COMMA(sigInfo) NULL, retSize, ilOffset, tmpReg);
        }
        else
        {
            genEmitCall(emitter::EC_FUNC_TOKEN, methHnd, INDEBUG_LDISASM_COMMA(sigInfo) addr, retSize, ilOffset);
        }
    }

    // if it was a pinvoke we may have needed to get the address of a label
    if (genPendingCallLabel)
    {
        assert(call->IsUnmanaged());
        genDefineTempLabel(genPendingCallLabel);
        genPendingCallLabel = nullptr;
    }

    // Update GC info:
    // All Callee arg registers are trashed and no longer contain any GC pointers.
    // TODO-ARM-Bug?: As a matter of fact shouldn't we be killing all of callee trashed regs here?
    // For now we will assert that other than arg regs gc ref/byref set doesn't contain any other
    // registers from RBM_CALLEE_TRASH
    assert((gcInfo.gcRegGCrefSetCur & (RBM_CALLEE_TRASH & ~RBM_ARG_REGS)) == 0);
    assert((gcInfo.gcRegByrefSetCur & (RBM_CALLEE_TRASH & ~RBM_ARG_REGS)) == 0);
    gcInfo.gcRegGCrefSetCur &= ~RBM_ARG_REGS;
    gcInfo.gcRegByrefSetCur &= ~RBM_ARG_REGS;

    var_types returnType = call->TypeGet();
    if (returnType != TYP_VOID)
    {
        regNumber returnReg;

        if (call->HasMultiRegRetVal())
        {
            assert(pRetTypeDesc != nullptr);
            unsigned regCount = pRetTypeDesc->GetReturnRegCount();

            // If regs allocated to call node are different from ABI return
            // regs in which the call has returned its result, move the result
            // to regs allocated to call node.
            for (unsigned i = 0; i < regCount; ++i)
            {
                var_types regType      = pRetTypeDesc->GetReturnRegType(i);
                returnReg              = pRetTypeDesc->GetABIReturnReg(i);
                regNumber allocatedReg = call->GetRegNumByIdx(i);
                if (returnReg != allocatedReg)
                {
                    inst_RV_RV(ins_Copy(regType), allocatedReg, returnReg, regType);
                }
            }
        }
        else
        {
            if (call->IsHelperCall(compiler, CORINFO_HELP_INIT_PINVOKE_FRAME))
            {
                // The CORINFO_HELP_INIT_PINVOKE_FRAME helper uses a custom calling convention that returns with
                // TCB in REG_PINVOKE_TCB. fgMorphCall() sets the correct argument registers.
                returnReg = REG_PINVOKE_TCB;
            }
            else if (varTypeIsFloating(returnType))
            {
                returnReg = REG_FLOATRET;
            }
            else
            {
                returnReg = REG_INTRET;
            }

            if (call->gtRegNum != returnReg)
            {
                inst_RV_RV(ins_Copy(returnType), call->gtRegNum, returnReg, returnType);
            }
        }

        genProduceReg(call);
    }

    // If there is nothing next, that means the result is thrown away, so this value is not live.
    // However, for minopts or debuggable code, we keep it live to support managed return value debugging.
    if ((call->gtNext == nullptr) && !compiler->opts.MinOpts() && !compiler->opts.compDbgCode)
    {
        gcInfo.gcMarkRegSetNpt(RBM_INTRET);
    }
}

//------------------------------------------------------------------------
// genLeaInstruction: Produce code for a GT_LEA subnode.
//
void CodeGen::genLeaInstruction(GenTreeAddrMode* lea)
{
    emitAttr size = emitTypeSize(lea);
    genConsumeOperands(lea);

    if (lea->Base() && lea->Index())
    {
        regNumber baseReg  = lea->Base()->gtRegNum;
        regNumber indexReg = lea->Index()->gtRegNum;
        getEmitter()->emitIns_R_ARX(INS_lea, size, lea->gtRegNum, baseReg, indexReg, lea->gtScale, lea->gtOffset);
    }
    else if (lea->Base())
    {
        regNumber baseReg = lea->Base()->gtRegNum;
        getEmitter()->emitIns_R_AR(INS_lea, size, lea->gtRegNum, baseReg, lea->gtOffset);
    }
    else if (lea->Index())
    {
        assert(!"Should we see a baseless address computation during CodeGen for ARM32?");
    }

    genProduceReg(lea);
}

//------------------------------------------------------------------------
// genCompareLong: Generate code for comparing two longs when the result of the compare
// is manifested in a register.
//
// Arguments:
//    treeNode - the compare tree
//
// Return Value:
//    None.
//
// Comments:
// For long compares, we need to compare the high parts of operands first, then the low parts.
// If the high compare is false, we do not need to compare the low parts. For less than and
// greater than, if the high compare is true, we can assume the entire compare is true.
//
void CodeGen::genCompareLong(GenTreePtr treeNode)
{
    assert(treeNode->OperIsCompare());

    GenTreeOp* tree = treeNode->AsOp();
    GenTreePtr op1  = tree->gtOp1;
    GenTreePtr op2  = tree->gtOp2;

    assert(varTypeIsLong(op1->TypeGet()));
    assert(varTypeIsLong(op2->TypeGet()));

    regNumber targetReg = treeNode->gtRegNum;

    genConsumeOperands(tree);

    GenTreePtr loOp1 = op1->gtGetOp1();
    GenTreePtr hiOp1 = op1->gtGetOp2();
    GenTreePtr loOp2 = op2->gtGetOp1();
    GenTreePtr hiOp2 = op2->gtGetOp2();

    // Create compare for the high parts
    instruction ins     = INS_cmp;
    var_types   cmpType = TYP_INT;
    emitAttr    cmpAttr = emitTypeSize(cmpType);

    // Emit the compare instruction
    getEmitter()->emitInsBinary(ins, cmpAttr, hiOp1, hiOp2);

    // If the result is not being materialized in a register, we're done.
    if (targetReg == REG_NA)
    {
        return;
    }

    BasicBlock* labelTrue  = genCreateTempLabel();
    BasicBlock* labelFalse = genCreateTempLabel();
    BasicBlock* labelNext  = genCreateTempLabel();

    genJccLongHi(tree->gtOper, labelTrue, labelFalse, tree->IsUnsigned());
    getEmitter()->emitInsBinary(ins, cmpAttr, loOp1, loOp2);
    genJccLongLo(tree->gtOper, labelTrue, labelFalse);

    genDefineTempLabel(labelFalse);
    getEmitter()->emitIns_R_I(INS_mov, emitActualTypeSize(tree->gtType), tree->gtRegNum, 0);
    getEmitter()->emitIns_J(INS_b, labelNext);

    genDefineTempLabel(labelTrue);
    getEmitter()->emitIns_R_I(INS_mov, emitActualTypeSize(tree->gtType), tree->gtRegNum, 1);

    genDefineTempLabel(labelNext);

    genProduceReg(tree);
}

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

//------------------------------------------------------------------------
// genSetRegToCond: Generate code to materialize a condition into a register.
//
// Arguments:
//   dstReg - The target register to set to 1 or 0
//   tree - The GenTree Relop node that was used to set the Condition codes
//
// Return Value: none
//
// Preconditions:
//    The condition codes must already have been appropriately set.
//
void CodeGen::genSetRegToCond(regNumber dstReg, GenTreePtr tree)
{
    // Emit code like that:
    //   ...
    //   bgt True
    //   movs rD, #0
    //   b Next
    // True:
    //   movs rD, #1
    // Next:
    //   ...

    CompareKind  compareKind = ((tree->gtFlags & GTF_UNSIGNED) != 0) ? CK_UNSIGNED : CK_SIGNED;
    emitJumpKind jmpKind     = genJumpKindForOper(tree->gtOper, compareKind);

    BasicBlock* labelTrue = genCreateTempLabel();
    getEmitter()->emitIns_J(emitter::emitJumpKindToIns(jmpKind), labelTrue);

    getEmitter()->emitIns_R_I(INS_mov, emitActualTypeSize(tree->gtType), dstReg, 0);

    BasicBlock* labelNext = genCreateTempLabel();
    getEmitter()->emitIns_J(INS_b, labelNext);

    genDefineTempLabel(labelTrue);
    getEmitter()->emitIns_R_I(INS_mov, emitActualTypeSize(tree->gtType), dstReg, 1);
    genDefineTempLabel(labelNext);
}

//------------------------------------------------------------------------
// genLongToIntCast: Generate code for long to int casts.
//
// Arguments:
//    cast - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    The cast node and its sources (via GT_LONG) must have been assigned registers.
//    The destination cannot be a floating point type or a small integer type.
//
void CodeGen::genLongToIntCast(GenTree* cast)
{
    assert(cast->OperGet() == GT_CAST);

    GenTree* src = cast->gtGetOp1();
    noway_assert(src->OperGet() == GT_LONG);

    genConsumeRegs(src);

    var_types srcType  = ((cast->gtFlags & GTF_UNSIGNED) != 0) ? TYP_ULONG : TYP_LONG;
    var_types dstType  = cast->CastToType();
    regNumber loSrcReg = src->gtGetOp1()->gtRegNum;
    regNumber hiSrcReg = src->gtGetOp2()->gtRegNum;
    regNumber dstReg   = cast->gtRegNum;

    assert((dstType == TYP_INT) || (dstType == TYP_UINT));
    assert(genIsValidIntReg(loSrcReg));
    assert(genIsValidIntReg(hiSrcReg));
    assert(genIsValidIntReg(dstReg));

    if (cast->gtOverflow())
    {
        //
        // Generate an overflow check for [u]long to [u]int casts:
        //
        // long  -> int  - check if the upper 33 bits are all 0 or all 1
        //
        // ulong -> int  - check if the upper 33 bits are all 0
        //
        // long  -> uint - check if the upper 32 bits are all 0
        // ulong -> uint - check if the upper 32 bits are all 0
        //

        if ((srcType == TYP_LONG) && (dstType == TYP_INT))
        {
            BasicBlock* allOne  = genCreateTempLabel();
            BasicBlock* success = genCreateTempLabel();

            inst_RV_RV(INS_tst, loSrcReg, loSrcReg, TYP_INT, EA_4BYTE);
            emitJumpKind JmpNegative = genJumpKindForOper(GT_LT, CK_LOGICAL);
            inst_JMP(JmpNegative, allOne);
            inst_RV_RV(INS_tst, hiSrcReg, hiSrcReg, TYP_INT, EA_4BYTE);
            emitJumpKind jmpNotEqualL = genJumpKindForOper(GT_NE, CK_LOGICAL);
            genJumpToThrowHlpBlk(jmpNotEqualL, SCK_OVERFLOW);
            inst_JMP(EJ_jmp, success);

            genDefineTempLabel(allOne);
            inst_RV_IV(INS_cmp, hiSrcReg, -1, EA_4BYTE);
            emitJumpKind jmpNotEqualS = genJumpKindForOper(GT_NE, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpNotEqualS, SCK_OVERFLOW);

            genDefineTempLabel(success);
        }
        else
        {
            if ((srcType == TYP_ULONG) && (dstType == TYP_INT))
            {
                inst_RV_RV(INS_tst, loSrcReg, loSrcReg, TYP_INT, EA_4BYTE);
                emitJumpKind JmpNegative = genJumpKindForOper(GT_LT, CK_LOGICAL);
                genJumpToThrowHlpBlk(JmpNegative, SCK_OVERFLOW);
            }

            inst_RV_RV(INS_tst, hiSrcReg, hiSrcReg, TYP_INT, EA_4BYTE);
            emitJumpKind jmpNotEqual = genJumpKindForOper(GT_NE, CK_LOGICAL);
            genJumpToThrowHlpBlk(jmpNotEqual, SCK_OVERFLOW);
        }
    }

    if (dstReg != loSrcReg)
    {
        inst_RV_RV(INS_mov, dstReg, loSrcReg, TYP_INT, EA_4BYTE);
    }

    genProduceReg(cast);
}

//------------------------------------------------------------------------
// genIntToIntCast: Generate code for an integer cast
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    The treeNode must have an assigned register.
//    For a signed convert from byte, the source must be in a byte-addressable register.
//    Neither the source nor target type can be a floating point type.
//
void CodeGen::genIntToIntCast(GenTreePtr treeNode)
{
    assert(treeNode->OperGet() == GT_CAST);

    GenTreePtr castOp = treeNode->gtCast.CastOp();
    emitter*   emit   = getEmitter();

    var_types dstType     = treeNode->CastToType();
    var_types srcType     = genActualType(castOp->TypeGet());
    emitAttr  movSize     = emitActualTypeSize(dstType);
    bool      movRequired = false;

    if (varTypeIsLong(srcType))
    {
        genLongToIntCast(treeNode);
        return;
    }

    regNumber targetReg = treeNode->gtRegNum;
    regNumber sourceReg = castOp->gtRegNum;

    // For Long to Int conversion we will have a reserved integer register to hold the immediate mask
    regNumber tmpReg = (treeNode->gtRsvdRegs == RBM_NONE) ? REG_NA : genRegNumFromMask(treeNode->gtRsvdRegs);

    assert(genIsValidIntReg(targetReg));
    assert(genIsValidIntReg(sourceReg));

    instruction ins = INS_invalid;

    genConsumeReg(castOp);
    Lowering::CastInfo castInfo;

    // Get information about the cast.
    Lowering::getCastDescription(treeNode, &castInfo);

    if (castInfo.requiresOverflowCheck)
    {
        emitAttr cmpSize = EA_ATTR(genTypeSize(srcType));

        if (castInfo.signCheckOnly)
        {
            // We only need to check for a negative value in sourceReg
            emit->emitIns_R_I(INS_cmp, cmpSize, sourceReg, 0);
            emitJumpKind jmpLT = genJumpKindForOper(GT_LT, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpLT, SCK_OVERFLOW);
            noway_assert(genTypeSize(srcType) == 4 || genTypeSize(srcType) == 8);
            // This is only interesting case to ensure zero-upper bits.
            if ((srcType == TYP_INT) && (dstType == TYP_ULONG))
            {
                // cast to TYP_ULONG:
                // We use a mov with size=EA_4BYTE
                // which will zero out the upper bits
                movSize     = EA_4BYTE;
                movRequired = true;
            }
        }
        else if (castInfo.unsignedSource || castInfo.unsignedDest)
        {
            // When we are converting from/to unsigned,
            // we only have to check for any bits set in 'typeMask'

            noway_assert(castInfo.typeMask != 0);
            emit->emitIns_R_I(INS_tst, cmpSize, sourceReg, castInfo.typeMask);
            emitJumpKind jmpNotEqual = genJumpKindForOper(GT_NE, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpNotEqual, SCK_OVERFLOW);
        }
        else
        {
            // For a narrowing signed cast
            //
            // We must check the value is in a signed range.

            // Compare with the MAX

            noway_assert((castInfo.typeMin != 0) && (castInfo.typeMax != 0));

            if (emitter::emitIns_valid_imm_for_cmp(castInfo.typeMax, INS_FLAGS_DONT_CARE))
            {
                emit->emitIns_R_I(INS_cmp, cmpSize, sourceReg, castInfo.typeMax);
            }
            else
            {
                noway_assert(tmpReg != REG_NA);
                instGen_Set_Reg_To_Imm(cmpSize, tmpReg, castInfo.typeMax);
                emit->emitIns_R_R(INS_cmp, cmpSize, sourceReg, tmpReg);
            }

            emitJumpKind jmpGT = genJumpKindForOper(GT_GT, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpGT, SCK_OVERFLOW);

            // Compare with the MIN

            if (emitter::emitIns_valid_imm_for_cmp(castInfo.typeMin, INS_FLAGS_DONT_CARE))
            {
                emit->emitIns_R_I(INS_cmp, cmpSize, sourceReg, castInfo.typeMin);
            }
            else
            {
                noway_assert(tmpReg != REG_NA);
                instGen_Set_Reg_To_Imm(cmpSize, tmpReg, castInfo.typeMin);
                emit->emitIns_R_R(INS_cmp, cmpSize, sourceReg, tmpReg);
            }

            emitJumpKind jmpLT = genJumpKindForOper(GT_LT, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpLT, SCK_OVERFLOW);
        }
        ins = INS_mov;
    }
    else // Non-overflow checking cast.
    {
        if (genTypeSize(srcType) == genTypeSize(dstType))
        {
            ins = INS_mov;
        }
        else
        {
            var_types extendType = TYP_UNKNOWN;

            // If we need to treat a signed type as unsigned
            if ((treeNode->gtFlags & GTF_UNSIGNED) != 0)
            {
                extendType  = genUnsignedType(srcType);
                movSize     = emitTypeSize(extendType);
                movRequired = true;
            }
            else
            {
                if (genTypeSize(srcType) < genTypeSize(dstType))
                {
                    extendType = srcType;
                    movSize    = emitTypeSize(srcType);
                    if (srcType == TYP_UINT)
                    {
                        movRequired = true;
                    }
                }
                else // (genTypeSize(srcType) > genTypeSize(dstType))
                {
                    extendType = dstType;
                    movSize    = emitTypeSize(dstType);
                }
            }

            ins = ins_Move_Extend(extendType, castOp->InReg());
        }
    }

    // We should never be generating a load from memory instruction here!
    assert(!emit->emitInsIsLoad(ins));

    if ((ins != INS_mov) || movRequired || (targetReg != sourceReg))
    {
        emit->emitIns_R_R(ins, movSize, targetReg, sourceReg);
    }

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genFloatToFloatCast: Generate code for a cast between float and double
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    Cast is a non-overflow conversion.
//    The treeNode must have an assigned register.
//    The cast is between float and double.
//
void CodeGen::genFloatToFloatCast(GenTreePtr treeNode)
{
    // float <--> double conversions are always non-overflow ones
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->gtRegNum;
    assert(genIsValidFloatReg(targetReg));

    GenTreePtr op1 = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());               // Cannot be contained
    assert(genIsValidFloatReg(op1->gtRegNum)); // Must be a valid float reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

    genConsumeOperands(treeNode->AsOp());

    // treeNode must be a reg
    assert(!treeNode->isContained());

    if (srcType != dstType)
    {
        instruction insVcvt = (srcType == TYP_FLOAT) ? INS_vcvt_f2d  // convert Float to Double
                                                     : INS_vcvt_d2f; // convert Double to Float

        getEmitter()->emitIns_R_R(insVcvt, emitTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum);
    }
    else if (treeNode->gtRegNum != op1->gtRegNum)
    {
        getEmitter()->emitIns_R_R(INS_vmov, emitTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum);
    }

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genIntToFloatCast: Generate code to cast an int/long to float/double
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    Cast is a non-overflow conversion.
//    The treeNode must have an assigned register.
//    SrcType= int32/uint32/int64/uint64 and DstType=float/double.
//
void CodeGen::genIntToFloatCast(GenTreePtr treeNode)
{
    // int --> float/double conversions are always non-overflow ones
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->gtRegNum;
    assert(genIsValidFloatReg(targetReg));

    GenTreePtr op1 = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());             // Cannot be contained
    assert(genIsValidIntReg(op1->gtRegNum)); // Must be a valid int reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(!varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

    // force the srcType to unsigned if GT_UNSIGNED flag is set
    if (treeNode->gtFlags & GTF_UNSIGNED)
    {
        srcType = genUnsignedType(srcType);
    }

    // We should never see a srcType whose size is neither EA_4BYTE or EA_8BYTE
    // For conversions from small types (byte/sbyte/int16/uint16) to float/double,
    // we expect the front-end or lowering phase to have generated two levels of cast.
    //
    emitAttr srcSize = EA_ATTR(genTypeSize(srcType));
    noway_assert((srcSize == EA_4BYTE) || (srcSize == EA_8BYTE));

    instruction insVcvt = INS_invalid;

    if (dstType == TYP_DOUBLE)
    {
        if (srcSize == EA_4BYTE)
        {
            insVcvt = (varTypeIsUnsigned(srcType)) ? INS_vcvt_u2d : INS_vcvt_i2d;
        }
        else
        {
            assert(srcSize == EA_8BYTE);
            NYI_ARM("Casting int64/uint64 to double in genIntToFloatCast");
        }
    }
    else
    {
        assert(dstType == TYP_FLOAT);
        if (srcSize == EA_4BYTE)
        {
            insVcvt = (varTypeIsUnsigned(srcType)) ? INS_vcvt_u2f : INS_vcvt_i2f;
        }
        else
        {
            assert(srcSize == EA_8BYTE);
            NYI_ARM("Casting int64/uint64 to float in genIntToFloatCast");
        }
    }

    genConsumeOperands(treeNode->AsOp());

    assert(insVcvt != INS_invalid);
    getEmitter()->emitIns_R_R(INS_vmov_i2f, srcSize, treeNode->gtRegNum, op1->gtRegNum);
    getEmitter()->emitIns_R_R(insVcvt, srcSize, treeNode->gtRegNum, treeNode->gtRegNum);

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genFloatToIntCast: Generate code to cast float/double to int/long
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    Cast is a non-overflow conversion.
//    The treeNode must have an assigned register.
//    SrcType=float/double and DstType= int32/uint32/int64/uint64
//
void CodeGen::genFloatToIntCast(GenTreePtr treeNode)
{
    // we don't expect to see overflow detecting float/double --> int type conversions here
    // as they should have been converted into helper calls by front-end.
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->gtRegNum;
    assert(genIsValidIntReg(targetReg)); // Must be a valid int reg.

    GenTreePtr op1 = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());               // Cannot be contained
    assert(genIsValidFloatReg(op1->gtRegNum)); // Must be a valid float reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(varTypeIsFloating(srcType) && !varTypeIsFloating(dstType));

    // We should never see a dstType whose size is neither EA_4BYTE or EA_8BYTE
    // For conversions to small types (byte/sbyte/int16/uint16) from float/double,
    // we expect the front-end or lowering phase to have generated two levels of cast.
    //
    emitAttr dstSize = EA_ATTR(genTypeSize(dstType));
    noway_assert((dstSize == EA_4BYTE) || (dstSize == EA_8BYTE));

    instruction insVcvt = INS_invalid;

    if (srcType == TYP_DOUBLE)
    {
        if (dstSize == EA_4BYTE)
        {
            insVcvt = (varTypeIsUnsigned(dstType)) ? INS_vcvt_d2u : INS_vcvt_d2i;
        }
        else
        {
            assert(dstSize == EA_8BYTE);
            NYI_ARM("Casting double to int64/uint64 in genIntToFloatCast");
        }
    }
    else
    {
        assert(srcType == TYP_FLOAT);
        if (dstSize == EA_4BYTE)
        {
            insVcvt = (varTypeIsUnsigned(dstType)) ? INS_vcvt_f2u : INS_vcvt_f2i;
        }
        else
        {
            assert(dstSize == EA_8BYTE);
            NYI_ARM("Casting float to int64/uint64 in genIntToFloatCast");
        }
    }

    genConsumeOperands(treeNode->AsOp());

    assert(insVcvt != INS_invalid);
    getEmitter()->emitIns_R_R(insVcvt, dstSize, op1->gtRegNum, op1->gtRegNum);
    getEmitter()->emitIns_R_R(INS_vmov_f2i, dstSize, treeNode->gtRegNum, op1->gtRegNum);

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCreateAndStoreGCInfo: Create and record GC Info for the function.
//
void CodeGen::genCreateAndStoreGCInfo(unsigned codeSize,
                                      unsigned prologSize,
                                      unsigned epilogSize DEBUGARG(void* codePtr))
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

//------------------------------------------------------------------------
// genEmitHelperCall: Emit a call to a helper function.
//
void CodeGen::genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize, regNumber callTargetReg /*= REG_NA */)
{
    // Can we call the helper function directly

    void *addr = NULL, **pAddr = NULL;

#if defined(DEBUG) && defined(PROFILING_SUPPORTED)
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

    if (!addr || !arm_Valid_Imm_For_BL((ssize_t)addr))
    {
        if (callTargetReg == REG_NA)
        {
            // If a callTargetReg has not been explicitly provided, we will use REG_DEFAULT_HELPER_CALL_TARGET, but
            // this is only a valid assumption if the helper call is known to kill REG_DEFAULT_HELPER_CALL_TARGET.
            callTargetReg = REG_DEFAULT_HELPER_CALL_TARGET;
        }

        // Load the address into a register and call through a register
        if (addr)
        {
            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, callTargetReg, (ssize_t)addr);
        }
        else
        {
            getEmitter()->emitIns_R_AI(INS_ldr, EA_PTR_DSP_RELOC, callTargetReg, (ssize_t)pAddr);
            regTracker.rsTrackRegTrash(callTargetReg);
        }

        getEmitter()->emitIns_Call(emitter::EC_INDIR_R, compiler->eeFindHelper(helper),
                                   INDEBUG_LDISASM_COMMA(nullptr) NULL, // addr
                                   argSize, retSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                   gcInfo.gcRegByrefSetCur,
                                   BAD_IL_OFFSET, // ilOffset
                                   callTargetReg, // ireg
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

    regTracker.rsTrashRegSet(RBM_CALLEE_TRASH);
    regTracker.rsTrashRegsForGCInterruptability();
}

//------------------------------------------------------------------------
// genStoreLongLclVar: Generate code to store a non-enregistered long lclVar
//
// Arguments:
//    treeNode - A TYP_LONG lclVar node.
//
// Return Value:
//    None.
//
// Assumptions:
//    'treeNode' must be a TYP_LONG lclVar node for a lclVar that has NOT been promoted.
//    Its operand must be a GT_LONG node.
//
void CodeGen::genStoreLongLclVar(GenTree* treeNode)
{
    emitter* emit = getEmitter();

    GenTreeLclVarCommon* lclNode = treeNode->AsLclVarCommon();
    unsigned             lclNum  = lclNode->gtLclNum;
    LclVarDsc*           varDsc  = &(compiler->lvaTable[lclNum]);
    assert(varDsc->TypeGet() == TYP_LONG);
    assert(!varDsc->lvPromoted);
    GenTreePtr op1 = treeNode->gtOp.gtOp1;
    noway_assert(op1->OperGet() == GT_LONG || op1->OperGet() == GT_MUL_LONG);
    genConsumeRegs(op1);

    if (op1->OperGet() == GT_LONG)
    {
        // Definitions of register candidates will have been lowered to 2 int lclVars.
        assert(!treeNode->InReg());

        GenTreePtr loVal = op1->gtGetOp1();
        GenTreePtr hiVal = op1->gtGetOp2();

        // NYI: Contained immediates.
        NYI_IF((loVal->gtRegNum == REG_NA) || (hiVal->gtRegNum == REG_NA),
               "Store of long lclVar with contained immediate");

        emit->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, loVal->gtRegNum, lclNum, 0);
        emit->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, hiVal->gtRegNum, lclNum, genTypeSize(TYP_INT));
    }
    else if (op1->OperGet() == GT_MUL_LONG)
    {
        assert((op1->gtFlags & GTF_MUL_64RSLT) != 0);

        // Stack store
        getEmitter()->emitIns_S_R(ins_Store(TYP_INT), emitTypeSize(TYP_INT), REG_LNGRET_LO, lclNum, 0);
        getEmitter()->emitIns_S_R(ins_Store(TYP_INT), emitTypeSize(TYP_INT), REG_LNGRET_HI, lclNum,
                                  genTypeSize(TYP_INT));
    }
}

#endif // _TARGET_ARM_

#endif // !LEGACY_BACKEND
