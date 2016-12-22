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
// genEmitGSCookieCheck: Generate code to check that the GS cookie wasn't thrashed by a buffer overrun.
//
void CodeGen::genEmitGSCookieCheck(bool pushReg)
{
    NYI("ARM genEmitGSCookieCheck");
}

//------------------------------------------------------------------------
// genCallFinally: Generate a call to the finally block.
//
BasicBlock* CodeGen::genCallFinally(BasicBlock* block, BasicBlock* lblk)
{
    NYI("ARM genCallFinally");
    return block;
}

//------------------------------------------------------------------------
// genEHCatchRet:
void CodeGen::genEHCatchRet(BasicBlock* block)
{
    NYI("ARM genEHCatchRet");
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
            NYI_ARM("genIntrinsic for sqrt - not implementd yet");
            break;

        default:
            assert(!"genIntrinsic: Unsupported intrinsic");
            unreached();
    }

    genProduceReg(treeNode);
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
#endif

    JITDUMP("Generating: ");
    DISPNODE(treeNode);

    // contained nodes are part of their parents for codegen purposes
    // ex : immediates, most LEAs
    if (treeNode->isContained())
    {
        return;
    }

    switch (treeNode->gtOper)
    {
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

            getEmitter()->emitIns_R_R_I(ins, emitTypeSize(treeNode), targetReg, operandReg, 0);
        }
            genProduceReg(treeNode);
            break;

        case GT_OR:
        case GT_XOR:
        case GT_AND:
            assert(varTypeIsIntegralOrI(treeNode));
            __fallthrough;

        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
        {
            const genTreeOps oper = treeNode->OperGet();
            if ((oper == GT_ADD || oper == GT_SUB || oper == GT_MUL) && treeNode->gtOverflow())
            {
                // This is also checked in the importer.
                NYI("Overflow not yet implemented");
            }

            GenTreePtr  op1 = treeNode->gtGetOp1();
            GenTreePtr  op2 = treeNode->gtGetOp2();
            instruction ins = genGetInsForOper(treeNode->OperGet(), targetType);

            // The arithmetic node must be sitting in a register (since it's not contained)
            noway_assert(targetReg != REG_NA);

            regNumber op1reg = op1->gtRegNum;
            regNumber op2reg = op2->gtRegNum;

            GenTreePtr dst;
            GenTreePtr src;

            genConsumeIfReg(op1);
            genConsumeIfReg(op2);

            if (!varTypeIsFloating(targetType))
            {
                // This is the case of reg1 = reg1 op reg2
                // We're ready to emit the instruction without any moves
                if (op1reg == targetReg)
                {
                    dst = op1;
                    src = op2;
                }
                // We have reg1 = reg2 op reg1
                // In order for this operation to be correct
                // we need that op is a commutative operation so
                // we can convert it into reg1 = reg1 op reg2 and emit
                // the same code as above
                else if (op2reg == targetReg)
                {
                    assert(GenTree::OperIsCommutative(treeNode->OperGet()));
                    dst = op2;
                    src = op1;
                }
                // dest, op1 and op2 registers are different:
                // reg3 = reg1 op reg2
                // We can implement this by issuing a mov:
                // reg3 = reg1
                // reg3 = reg3 op reg2
                else
                {
                    inst_RV_RV(ins_Move_Extend(targetType, true), targetReg, op1reg, op1->gtType);
                    regTracker.rsTrackRegCopy(targetReg, op1reg);
                    gcInfo.gcMarkRegPtrVal(targetReg, targetType);
                    dst = treeNode;
                    src = op2;
                }

                regNumber r = emit->emitInsBinary(ins, emitTypeSize(treeNode), dst, src);
                assert(r == targetReg);
            }
            else
            {
                emit->emitIns_R_R_R(ins, emitTypeSize(treeNode), targetReg, op1reg, op2reg);
            }
        }
            genProduceReg(treeNode);
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
            genCodeForShift(treeNode);
            // genCodeForShift() calls genProduceReg()
            break;

        case GT_CAST:
            // Cast is never contained (?)
            noway_assert(targetReg != REG_NA);

            // Overflow conversions from float/double --> int types go through helper calls.
            if (treeNode->gtOverflow() && !varTypeIsFloating(treeNode->gtOp.gtOp1))
                NYI("Unimplmented GT_CAST:int <--> int with overflow");

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
            NYI_IF(targetType == TYP_STRUCT, "GT_STORE_LCL_FLD: struct store local field not supported");
            noway_assert(!treeNode->InReg());

            GenTreePtr op1 = treeNode->gtOp.gtOp1->gtEffectiveVal();
            genConsumeIfReg(op1);
            emit->emitInsBinary(ins_Store(targetType), emitTypeSize(treeNode), treeNode, op1);
        }
        break;

        case GT_STORE_LCL_VAR:
        {
            NYI_IF(targetType == TYP_STRUCT, "struct store local not supported");

            GenTreePtr op1 = treeNode->gtOp.gtOp1->gtEffectiveVal();
            genConsumeIfReg(op1);
            if (treeNode->gtRegNum == REG_NA)
            {
                // stack store
                emit->emitInsMov(ins_Store(targetType), emitTypeSize(treeNode), treeNode);
                compiler->lvaTable[treeNode->AsLclVarCommon()->gtLclNum].lvRegNum = REG_STK;
            }
            else if (op1->isContained())
            {
                // Currently, we assume that the contained source of a GT_STORE_LCL_VAR writing to a register
                // must be a constant. However, in the future we might want to support a contained memory op.
                // This is a bit tricky because we have to decide it's contained before register allocation,
                // and this would be a case where, once that's done, we need to mark that node as always
                // requiring a register - which we always assume now anyway, but once we "optimize" that
                // we'll have to take cases like this into account.
                assert((op1->gtRegNum == REG_NA) && op1->OperIsConst());
                genSetRegToConst(treeNode->gtRegNum, targetType, op1);
            }
            else if (op1->gtRegNum != treeNode->gtRegNum)
            {
                assert(op1->gtRegNum != REG_NA);
                emit->emitInsBinary(ins_Move_Extend(targetType, true), emitTypeSize(treeNode), treeNode, op1);
            }
            if (treeNode->gtRegNum != REG_NA)
                genProduceReg(treeNode);
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
        {
            GenTreePtr op1 = treeNode->gtOp.gtOp1;
            if (targetType == TYP_VOID)
            {
                assert(op1 == nullptr);
                break;
            }
            assert(op1 != nullptr);
            op1 = op1->gtEffectiveVal();

            NYI_IF(op1->gtRegNum == REG_NA, "GT_RETURN: return of a value not in register");
            genConsumeReg(op1);

            regNumber retReg = varTypeIsFloating(op1) ? REG_FLOATRET : REG_INTRET;
            if (op1->gtRegNum != retReg)
            {
                inst_RV_RV(ins_Move_Extend(targetType, true), retReg, op1->gtRegNum, targetType);
            }
        }
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
            emit->emitInsMov(ins_Load(treeNode->TypeGet()), emitTypeSize(treeNode), treeNode);
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

                emit->emitInsMov(ins_Store(data->TypeGet()), emitTypeSize(storeInd), storeInd);
            }
        }
        break;

        case GT_COPY:
        {
            assert(treeNode->gtOp.gtOp1->IsLocal());
            GenTreeLclVarCommon* lcl    = treeNode->gtOp.gtOp1->AsLclVarCommon();
            LclVarDsc*           varDsc = &compiler->lvaTable[lcl->gtLclNum];
            inst_RV_RV(ins_Move_Extend(targetType, true), targetReg, genConsumeReg(treeNode->gtOp.gtOp1), targetType,
                       emitTypeSize(targetType));

            // The old location is dying
            genUpdateRegLife(varDsc, /*isBorn*/ false, /*isDying*/ true DEBUGARG(treeNode->gtOp.gtOp1));

            gcInfo.gcMarkRegSetNpt(genRegMask(treeNode->gtOp.gtOp1->gtRegNum));

            genUpdateVarReg(varDsc, treeNode);

            // The new location is going live
            genUpdateRegLife(varDsc, /*isBorn*/ true, /*isDying*/ false DEBUGARG(treeNode));
        }
            genProduceReg(treeNode);
            break;

        case GT_LIST:
        case GT_FIELD_LIST:
        case GT_ARGPLACE:
            // Nothing to do
            break;

        case GT_PUTARG_STK:
        {
            NYI_IF(targetType == TYP_STRUCT, "GT_PUTARG_STK: struct support not implemented");

            // Get argument offset on stack.
            // Here we cross check that argument offset hasn't changed from lowering to codegen since
            // we are storing arg slot number in GT_PUTARG_STK node in lowering phase.
            int argOffset = treeNode->AsPutArgStk()->gtSlotNum * TARGET_POINTER_SIZE;
#ifdef DEBUG
            fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(treeNode->AsPutArgStk()->gtCall, treeNode);
            assert(curArgTabEntry);
            assert(argOffset == (int)curArgTabEntry->slotNum * TARGET_POINTER_SIZE);
#endif

            GenTreePtr data = treeNode->gtOp.gtOp1->gtEffectiveVal();
            if (data->isContained())
            {
                emit->emitIns_S_I(ins_Store(targetType), emitTypeSize(targetType), compiler->lvaOutgoingArgSpaceVar,
                                  argOffset, (int)data->AsIntConCommon()->IconValue());
            }
            else
            {
                genConsumeReg(data);
                emit->emitIns_S_R(ins_Store(targetType), emitTypeSize(targetType), data->gtRegNum,
                                  compiler->lvaOutgoingArgSpaceVar, argOffset);
            }
        }
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
            genCallInstruction(treeNode);
            break;

        case GT_LOCKADD:
        case GT_XCHG:
        case GT_XADD:
            genLockedInstructions(treeNode->AsOp());
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
            emit->emitIns_R_L(INS_lea, EA_PTRSIZE, genPendingCallLabel, treeNode->gtRegNum);
            break;

        case GT_CLS_VAR_ADDR:
            emit->emitIns_R_C(INS_lea, EA_PTRSIZE, targetReg, treeNode->gtClsVar.gtClsVarHnd, 0);
            genProduceReg(treeNode);
            break;

        case GT_IL_OFFSET:
            // Do nothing; these nodes are simply markers for debug info.
            break;

        default:
        {
#ifdef DEBUG
            char message[256];
            _snprintf_s(message, _countof(message), _TRUNCATE, "NYI: Unimplemented node type %s\n",
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

    GenTreePtr operand = tree->gtGetOp1();
    genConsumeReg(operand);

    GenTreePtr shiftBy = tree->gtGetOp2();
    if (!shiftBy->IsCnsIntOrI())
    {
        genConsumeReg(shiftBy);
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

//------------------------------------------------------------------------
// genRegCopy: Generate a register copy.
//
void CodeGen::genRegCopy(GenTree* treeNode)
{
    NYI("genRegCopy");
}

//------------------------------------------------------------------------
// genCallInstruction: Produce code for a GT_CALL node
//
void CodeGen::genCallInstruction(GenTreePtr node)
{
    GenTreeCall* call = node->AsCall();

    assert(call->gtOper == GT_CALL);

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
        const regNumber regThis = genGetThisArgReg(call);
        const regNumber tmpReg  = genRegNumFromMask(node->gtRsvdRegs);
        getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, tmpReg, regThis, 0);
    }

    // Either gtControlExpr != null or gtCallAddr != null or it is a direct non-virtual call to a user or helper method.
    CORINFO_METHOD_HANDLE methHnd;
    GenTree*              target = call->gtControlExpr;
    if (callType == CT_INDIRECT)
    {
        assert(target == nullptr);
        target  = call->gtCall.gtCallAddr;
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
    ReturnTypeDesc* pRetTypeDesc = call->GetReturnTypeDesc();
    emitAttr        retSize      = EA_PTRSIZE;

    if (call->HasMultiRegRetVal())
    {
        NYI_ARM("has multi reg ret val");
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
            regNumber tmpReg = genRegNumFromMask(node->gtRsvdRegs);
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
            if (varTypeIsFloating(returnType))
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
    if (lea->Base() && lea->Index())
    {
        regNumber baseReg  = genConsumeReg(lea->Base());
        regNumber indexReg = genConsumeReg(lea->Index());
        getEmitter()->emitIns_R_ARX(INS_lea, EA_BYREF, lea->gtRegNum, baseReg, indexReg, lea->gtScale, lea->gtOffset);
    }
    else if (lea->Base())
    {
        getEmitter()->emitIns_R_AR(INS_lea, EA_BYREF, lea->gtRegNum, genConsumeReg(lea->Base()), lea->gtOffset);
    }

    genProduceReg(lea);
}

//------------------------------------------------------------------------
// genSetRegToCond: Generate code to materialize a condition into a register.
//
// Preconditions:
//    The condition codes must already have been appropriately set.
//
void CodeGen::genSetRegToCond(regNumber dstReg, GenTreePtr tree)
{
    NYI("genSetRegToCond");
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
    NYI("Cast");
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
    NYI("Cast");
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
    NYI("Cast");
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
    NYI("Cast");
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

    // First we figure out the encoder ID's for the stack slots and registers.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_ASSIGN_SLOTS);
    // Now we've requested all the slots we'll need; "finalize" these (make more compact data structures for them).
    gcInfoEncoder->FinalizeSlotIds();
    // Now we can actually use those slot ID's to declare live ranges.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_DO_WORK);

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

#endif // _TARGET_ARM_

#endif // !LEGACY_BACKEND
