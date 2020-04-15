// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef FEATURE_HW_INTRINSICS

#include "codegen.h"

CodeGen::HWIntrinsicImmOpHelper::HWIntrinsicImmOpHelper(CodeGen* codeGen, GenTree* immOp, GenTreeHWIntrinsic* intrin)
    : codeGen(codeGen), endLabel(nullptr), nonZeroLabel(nullptr), branchTargetReg(REG_NA)
{
    assert(codeGen != nullptr);

    if (immOp->isContainedIntOrIImmed())
    {
        nonConstImmReg = REG_NA;

        immValue      = (int)immOp->AsIntCon()->IconValue();
        immUpperBound = immValue + 1;
    }
    else
    {
        nonConstImmReg = immOp->GetRegNum();

        immValue = 0;
        immUpperBound =
            HWIntrinsicInfo::lookupImmUpperBound(intrin->gtHWIntrinsicId, intrin->gtSIMDSize, intrin->gtSIMDBaseType);

        if (BranchAtNonZero())
        {
            nonZeroLabel = codeGen->genCreateTempLabel();
        }
        else
        {
            // At the moment, this helper supports only intrinsics that correspond to one machine instruction.
            // If we ever encounter an intrinsic that is either lowered into multiple instructions or
            // the number of instructions that correspond to each case is unknown apriori - we can extend support to
            // these by
            // using the same approach as in hwintrinsicxarch.cpp - adding an additional indirection level in form of a
            // branch table.
            assert(!HWIntrinsicInfo::GeneratesMultipleIns(intrin->gtHWIntrinsicId));
            branchTargetReg = intrin->GetSingleTempReg();
        }

        endLabel = codeGen->genCreateTempLabel();
    }
}

void CodeGen::HWIntrinsicImmOpHelper::EmitAtFirst()
{
    if (NonConstImmOp())
    {
        BasicBlock* beginLabel = codeGen->genCreateTempLabel();

        if (BranchAtNonZero())
        {
            GetEmitter()->emitIns_J_R(INS_cbnz, EA_4BYTE, nonZeroLabel, nonConstImmReg);
        }
        else
        {
            // Here we assume that each case consists of one arm64 instruction followed by "b endLabel".
            // Since an arm64 instruction is 4 bytes, we branch to AddressOf(beginLabel) + (nonConstImmReg << 3).
            GetEmitter()->emitIns_R_L(INS_adr, EA_8BYTE, beginLabel, branchTargetReg);
            GetEmitter()->emitIns_R_R_R_I(INS_add, EA_8BYTE, branchTargetReg, branchTargetReg, nonConstImmReg, 3,
                                          INS_OPTS_LSL);
            GetEmitter()->emitIns_R(INS_br, EA_8BYTE, branchTargetReg);
        }

        codeGen->genDefineInlineTempLabel(beginLabel);
    }
}

void CodeGen::HWIntrinsicImmOpHelper::EmitAfterCase()
{
    assert(!Done());

    if (NonConstImmOp())
    {
        const bool isLastCase = (immValue + 1 == immUpperBound);

        if (isLastCase)
        {
            codeGen->genDefineInlineTempLabel(endLabel);
        }
        else
        {
            GetEmitter()->emitIns_J(INS_b, endLabel);

            if (BranchAtNonZero())
            {
                codeGen->genDefineInlineTempLabel(nonZeroLabel);
            }
            else
            {
                BasicBlock* tempLabel = codeGen->genCreateTempLabel();
                codeGen->genDefineInlineTempLabel(tempLabel);
            }
        }
    }

    immValue++;
}

//------------------------------------------------------------------------
// genHWIntrinsic: Generates the code for a given hardware intrinsic node.
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genHWIntrinsic(GenTreeHWIntrinsic* node)
{
    const HWIntrinsic intrin(node);

    regNumber targetReg = node->GetRegNum();

    regNumber op1Reg = REG_NA;
    regNumber op2Reg = REG_NA;
    regNumber op3Reg = REG_NA;

    switch (intrin.numOperands)
    {
        case 3:
            assert(intrin.op3 != nullptr);
            op3Reg = intrin.op3->GetRegNum();
            __fallthrough;

        case 2:
            assert(intrin.op2 != nullptr);
            op2Reg = intrin.op2->GetRegNum();
            __fallthrough;

        case 1:
            assert(intrin.op1 != nullptr);
            op1Reg = intrin.op1->GetRegNum();
            break;

        default:
            unreached();
    }

    emitAttr emitSize;
    insOpts  opt = INS_OPTS_NONE;

    if ((intrin.category == HW_Category_SIMDScalar) || (intrin.category == HW_Category_Scalar))
    {
        emitSize = emitActualTypeSize(intrin.baseType);
    }
    else
    {
        emitSize = EA_SIZE(node->gtSIMDSize);
        opt      = genGetSimdInsOpt(emitSize, intrin.baseType);

        if ((opt == INS_OPTS_1D) && (intrin.category == HW_Category_SimpleSIMD))
        {
            opt = INS_OPTS_NONE;
        }
    }

    const bool isRMW = node->isRMWHWIntrinsic(compiler);

    genConsumeHWIntrinsicOperands(node);

    if (intrin.IsTableDriven())
    {
        instruction ins = HWIntrinsicInfo::lookupIns(intrin.id, intrin.baseType);
        assert(ins != INS_invalid);

        switch (intrin.numOperands)
        {
            case 1:
                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg, opt);
                break;

            case 2:
                if (isRMW)
                {
                    assert(targetReg != op2Reg);

                    if (targetReg != op1Reg)
                    {
                        GetEmitter()->emitIns_R_R(INS_mov, emitSize, targetReg, op1Reg);
                    }
                    GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op2Reg, opt);
                }
                else
                {
                    GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                }
                break;

            case 3:
                assert(isRMW);
                assert(targetReg != op2Reg);
                assert(targetReg != op3Reg);

                if (targetReg != op1Reg)
                {
                    GetEmitter()->emitIns_R_R(INS_mov, emitSize, targetReg, op1Reg);
                }
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op2Reg, op3Reg, opt);
                break;

            default:
                unreached();
        }
    }
    else
    {
        instruction ins = INS_invalid;

        switch (intrin.id)
        {
            case NI_Crc32_ComputeCrc32:
                if (intrin.baseType == TYP_INT)
                {
                    ins = INS_crc32w;
                }
                else
                {
                    ins = HWIntrinsicInfo::lookupIns(intrin.id, intrin.baseType);
                }
                break;

            case NI_Crc32_ComputeCrc32C:
                if (intrin.baseType == TYP_INT)
                {
                    ins = INS_crc32cw;
                }
                else
                {
                    ins = HWIntrinsicInfo::lookupIns(intrin.id, intrin.baseType);
                }
                break;

            case NI_Crc32_Arm64_ComputeCrc32:
                assert(intrin.baseType == TYP_LONG);
                ins = INS_crc32x;
                break;

            case NI_Crc32_Arm64_ComputeCrc32C:
                assert(intrin.baseType == TYP_LONG);
                ins = INS_crc32cx;
                break;

            default:
                ins = HWIntrinsicInfo::lookupIns(intrin.id, intrin.baseType);
                break;
        }

        assert(ins != INS_invalid);

        switch (intrin.id)
        {
            case NI_AdvSimd_BitwiseSelect:
                // Even though BitwiseSelect is an RMW intrinsic per se, we don't want to mark it as such
                // since we can handle all possible allocation decisions for targetReg.
                assert(!isRMW);

                if (targetReg == op1Reg)
                {
                    GetEmitter()->emitIns_R_R_R(INS_bsl, emitSize, targetReg, op2Reg, op3Reg, opt);
                }
                else if (targetReg == op2Reg)
                {
                    GetEmitter()->emitIns_R_R_R(INS_bif, emitSize, targetReg, op3Reg, op1Reg, opt);
                }
                else if (targetReg == op3Reg)
                {
                    GetEmitter()->emitIns_R_R_R(INS_bit, emitSize, targetReg, op2Reg, op1Reg, opt);
                }
                else
                {
                    GetEmitter()->emitIns_R_R(INS_mov, emitSize, targetReg, op1Reg);
                    GetEmitter()->emitIns_R_R_R(INS_bsl, emitSize, targetReg, op2Reg, op3Reg, opt);
                }
                break;

            case NI_Crc32_ComputeCrc32:
            case NI_Crc32_ComputeCrc32C:
            case NI_Crc32_Arm64_ComputeCrc32:
            case NI_Crc32_Arm64_ComputeCrc32C:
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;

            case NI_AdvSimd_CompareLessThan:
            case NI_AdvSimd_CompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_CompareLessThan:
            case NI_AdvSimd_Arm64_CompareLessThanScalar:
            case NI_AdvSimd_Arm64_CompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_CompareLessThanOrEqualScalar:
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op2Reg, op1Reg, opt);
                break;

            case NI_AdvSimd_AbsoluteCompareLessThan:
            case NI_AdvSimd_AbsoluteCompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThan:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThanScalar:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThanOrEqualScalar:
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op2Reg, op1Reg, opt);
                break;

            case NI_AdvSimd_FusedMultiplyAddScalar:
            case NI_AdvSimd_FusedMultiplyAddNegatedScalar:
            case NI_AdvSimd_FusedMultiplySubtractNegatedScalar:
            case NI_AdvSimd_FusedMultiplySubtractScalar:
                assert(opt == INS_OPTS_NONE);
                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op2Reg, op3Reg, op1Reg);
                break;

            case NI_AdvSimd_Store:
                GetEmitter()->emitIns_R_R(ins, emitSize, op2Reg, op1Reg, opt);
                break;

            default:
                unreached();
        }
    }

    genProduceReg(node);
}

#endif // FEATURE_HW_INTRINSICS
