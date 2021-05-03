// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef FEATURE_HW_INTRINSICS

#include "codegen.h"

// HWIntrinsicImmOpHelper: constructs the helper class instance.
//       This also determines what type of "switch" table is being used (if an immediate operand is not constant) and do
//       some preparation work:
//
//       a) If an immediate operand can be either 0 or 1, this creates <nonZeroLabel>.
//
//       b) If an immediate operand can take any value in [0, upperBound), this extract a internal register from an
//       intrinsic node. The register will be later used to store computed branch target address.
//
// Arguments:
//    codeGen -- an instance of CodeGen class.
//    immOp   -- an immediate operand of the intrinsic.
//    intrin  -- a hardware intrinsic tree node.
//
// Note: This class is designed to be used in the following way
//       HWIntrinsicImmOpHelper helper(this, immOp, intrin);
//
//       for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
//       {
//         -- emit an instruction for a given value of helper.ImmValue()
//       }
//
//       This allows to combine logic for cases when immOp->isContainedIntOrIImmed() is either true or false in a form
//       of a for-loop.
//
CodeGen::HWIntrinsicImmOpHelper::HWIntrinsicImmOpHelper(CodeGen* codeGen, GenTree* immOp, GenTreeHWIntrinsic* intrin)
    : codeGen(codeGen), endLabel(nullptr), nonZeroLabel(nullptr), branchTargetReg(REG_NA)
{
    assert(codeGen != nullptr);
    assert(varTypeIsIntegral(immOp));

    if (immOp->isContainedIntOrIImmed())
    {
        nonConstImmReg = REG_NA;

        immValue      = (int)immOp->AsIntCon()->IconValue();
        immLowerBound = immValue;
        immUpperBound = immValue;
    }
    else
    {
        const HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(intrin->gtHWIntrinsicId);

        if (category == HW_Category_SIMDByIndexedElement)
        {
            const HWIntrinsic intrinInfo(intrin);
            var_types         indexedElementOpType;

            if (intrinInfo.numOperands == 3)
            {
                indexedElementOpType = intrinInfo.op2->TypeGet();
            }
            else
            {
                assert(intrinInfo.numOperands == 4);
                indexedElementOpType = intrinInfo.op3->TypeGet();
            }

            assert(varTypeIsSIMD(indexedElementOpType));

            const unsigned int indexedElementSimdSize = genTypeSize(indexedElementOpType);
            HWIntrinsicInfo::lookupImmBounds(intrin->gtHWIntrinsicId, indexedElementSimdSize, intrin->GetSimdBaseType(),
                                             &immLowerBound, &immUpperBound);
        }
        else
        {
            HWIntrinsicInfo::lookupImmBounds(intrin->gtHWIntrinsicId, intrin->GetSimdSize(), intrin->GetSimdBaseType(),
                                             &immLowerBound, &immUpperBound);
        }

        nonConstImmReg = immOp->GetRegNum();
        immValue       = immLowerBound;

        if (TestImmOpZeroOrOne())
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

//------------------------------------------------------------------------
// EmitBegin: emits the beginning of a "switch" table, no-op if an immediate operand is constant.
//
// Note: The function is called at the beginning of code generation and emits
//    a) If an immediate operand can be either 0 or 1
//
//       cbnz <nonZeroLabel>, nonConstImmReg
//
//    b) If an immediate operand can take any value in [0, upperBound) range
//
//       adr branchTargetReg, <beginLabel>
//       add branchTargetReg, branchTargetReg, nonConstImmReg, lsl #3
//       br  branchTargetReg
//
//       When an immediate operand is non constant this also defines <beginLabel> right after the emitted code.
//
void CodeGen::HWIntrinsicImmOpHelper::EmitBegin()
{
    if (NonConstImmOp())
    {
        BasicBlock* beginLabel = codeGen->genCreateTempLabel();

        if (TestImmOpZeroOrOne())
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

            // If the lower bound is non zero we need to adjust the branch target value by subtracting
            // (immLowerBound << 3).
            if (immLowerBound != 0)
            {
                GetEmitter()->emitIns_R_R_I(INS_sub, EA_8BYTE, branchTargetReg, branchTargetReg,
                                            ((ssize_t)immLowerBound << 3));
            }

            GetEmitter()->emitIns_R(INS_br, EA_8BYTE, branchTargetReg);
        }

        codeGen->genDefineInlineTempLabel(beginLabel);
    }
}

//------------------------------------------------------------------------
// EmitCaseEnd: emits the end of a "case", no-op if an immediate operand is constant.
//
// Note: The function is called at the end of each "case" (i.e. after an instruction has been emitted for a given
// immediate value ImmValue())
//       and emits
//
//       b <endLabel>
//
//       After the last "case" this defines <endLabel>.
//
//       If an immediate operand is either 0 or 1 it also defines <nonZeroLabel> after the first "case".
//
void CodeGen::HWIntrinsicImmOpHelper::EmitCaseEnd()
{
    assert(!Done());

    if (NonConstImmOp())
    {
        const bool isLastCase = (immValue == immUpperBound);

        if (isLastCase)
        {
            codeGen->genDefineInlineTempLabel(endLabel);
        }
        else
        {
            GetEmitter()->emitIns_J(INS_b, endLabel);

            if (TestImmOpZeroOrOne())
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
    regNumber op4Reg = REG_NA;

    switch (intrin.numOperands)
    {
        case 4:
            assert(intrin.op4 != nullptr);
            op4Reg = intrin.op4->GetRegNum();
            FALLTHROUGH;

        case 3:
            assert(intrin.op3 != nullptr);
            op3Reg = intrin.op3->GetRegNum();
            FALLTHROUGH;

        case 2:
            assert(intrin.op2 != nullptr);
            op2Reg = intrin.op2->GetRegNum();
            FALLTHROUGH;

        case 1:
            assert(intrin.op1 != nullptr);
            op1Reg = intrin.op1->GetRegNum();
            break;

        case 0:
            break;

        default:
            unreached();
    }

    emitAttr emitSize;
    insOpts  opt;

    if (HWIntrinsicInfo::SIMDScalar(intrin.id))
    {
        emitSize = emitTypeSize(intrin.baseType);
        opt      = INS_OPTS_NONE;
    }
    else if (intrin.category == HW_Category_Scalar)
    {
        emitSize = emitActualTypeSize(intrin.baseType);
        opt      = INS_OPTS_NONE;
    }
    else
    {
        emitSize = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->GetSimdSize()));
        opt      = genGetSimdInsOpt(emitSize, intrin.baseType);
    }

    const bool isRMW               = node->isRMWHWIntrinsic(compiler);
    const bool hasImmediateOperand = HWIntrinsicInfo::HasImmediateOperand(intrin.id);

    genConsumeHWIntrinsicOperands(node);

    if (intrin.IsTableDriven())
    {
        const instruction ins = HWIntrinsicInfo::lookupIns(intrin.id, intrin.baseType);
        assert(ins != INS_invalid);

        if (intrin.category == HW_Category_SIMDByIndexedElement)
        {
            if (hasImmediateOperand)
            {
                if (isRMW)
                {
                    assert(targetReg != op2Reg);
                    assert(targetReg != op3Reg);

                    if (targetReg != op1Reg)
                    {
                        GetEmitter()->emitIns_R_R(INS_mov, emitTypeSize(node), targetReg, op1Reg);
                    }

                    HWIntrinsicImmOpHelper helper(this, intrin.op4, node);

                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        const int elementIndex = helper.ImmValue();

                        GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op2Reg, op3Reg, elementIndex, opt);
                    }
                }
                else
                {
                    HWIntrinsicImmOpHelper helper(this, intrin.op3, node);

                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        const int elementIndex = helper.ImmValue();

                        GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, elementIndex, opt);
                    }
                }
            }
            else
            {
                if (isRMW)
                {
                    assert(targetReg != op2Reg);
                    assert(targetReg != op3Reg);

                    if (targetReg != op1Reg)
                    {
                        GetEmitter()->emitIns_R_R(INS_mov, emitTypeSize(node), targetReg, op1Reg);
                    }

                    GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op2Reg, op3Reg, 0, opt);
                }
                else
                {
                    GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, 0, opt);
                }
            }
        }
        else if ((intrin.category == HW_Category_ShiftLeftByImmediate) ||
                 (intrin.category == HW_Category_ShiftRightByImmediate))
        {
            assert(hasImmediateOperand);

            if (isRMW)
            {
                if (targetReg != op1Reg)
                {
                    GetEmitter()->emitIns_R_R(INS_mov, emitTypeSize(node), targetReg, op1Reg);
                }

                HWIntrinsicImmOpHelper helper(this, intrin.op3, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int shiftAmount = helper.ImmValue();

                    GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op2Reg, shiftAmount, opt);
                }
            }
            else
            {
                HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int shiftAmount = helper.ImmValue();

                    GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op1Reg, shiftAmount, opt);
                }
            }
        }
        else
        {
            assert(!hasImmediateOperand);

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
                            GetEmitter()->emitIns_R_R(INS_mov, emitTypeSize(node), targetReg, op1Reg);
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
                        GetEmitter()->emitIns_R_R(INS_mov, emitTypeSize(node), targetReg, op1Reg);
                    }

                    GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op2Reg, op3Reg, opt);
                    break;

                default:
                    unreached();
            }
        }
    }
    else
    {
        instruction ins = INS_invalid;
        switch (intrin.id)
        {
            case NI_AdvSimd_AddWideningLower:
                assert(varTypeIsIntegral(intrin.baseType));
                if (intrin.op1->TypeGet() == TYP_SIMD8)
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_uaddl : INS_saddl;
                }
                else
                {
                    assert(intrin.op1->TypeGet() == TYP_SIMD16);
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_uaddw : INS_saddw;
                }
                break;

            case NI_AdvSimd_SubtractWideningLower:
                assert(varTypeIsIntegral(intrin.baseType));
                if (intrin.op1->TypeGet() == TYP_SIMD8)
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_usubl : INS_ssubl;
                }
                else
                {
                    assert(intrin.op1->TypeGet() == TYP_SIMD16);
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_usubw : INS_ssubw;
                }
                break;

            case NI_AdvSimd_AddWideningUpper:
                assert(varTypeIsIntegral(intrin.baseType));
                if (node->GetAuxiliaryType() == intrin.baseType)
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_uaddl2 : INS_saddl2;
                }
                else
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_uaddw2 : INS_saddw2;
                }
                break;

            case NI_AdvSimd_SubtractWideningUpper:
                assert(varTypeIsIntegral(intrin.baseType));
                if (node->GetAuxiliaryType() == intrin.baseType)
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_usubl2 : INS_ssubl2;
                }
                else
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_usubw2 : INS_ssubw2;
                }
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

            case NI_AdvSimd_AbsoluteCompareLessThan:
            case NI_AdvSimd_AbsoluteCompareLessThanOrEqual:
            case NI_AdvSimd_CompareLessThan:
            case NI_AdvSimd_CompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThan:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThanScalar:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThanOrEqualScalar:
            case NI_AdvSimd_Arm64_CompareLessThan:
            case NI_AdvSimd_Arm64_CompareLessThanScalar:
            case NI_AdvSimd_Arm64_CompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_CompareLessThanOrEqualScalar:
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op2Reg, op1Reg, opt);
                break;

            case NI_AdvSimd_FusedMultiplyAddScalar:
            case NI_AdvSimd_FusedMultiplyAddNegatedScalar:
            case NI_AdvSimd_FusedMultiplySubtractNegatedScalar:
            case NI_AdvSimd_FusedMultiplySubtractScalar:
                assert(opt == INS_OPTS_NONE);
                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op2Reg, op3Reg, op1Reg);
                break;

            case NI_AdvSimd_DuplicateSelectedScalarToVector64:
            case NI_AdvSimd_DuplicateSelectedScalarToVector128:
            case NI_AdvSimd_Arm64_DuplicateSelectedScalarToVector128:
            {
                HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                // Prior to codegen, the emitSize is based on node->GetSimdSize() which
                // tracks the size of the first operand and is used to tell if the index
                // is in range. However, when actually emitting it needs to be the size
                // of the return and the size of the operand is interpreted based on the
                // index value.

                assert(
                    GetEmitter()->isValidVectorIndex(emitSize, GetEmitter()->optGetElemsize(opt), helper.ImmValue()));

                emitSize = emitActualTypeSize(node->gtType);
                opt      = genGetSimdInsOpt(emitSize, intrin.baseType);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();

                    assert(opt != INS_OPTS_NONE);
                    GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op1Reg, elementIndex, opt);
                }

                break;
            }

            case NI_AdvSimd_Extract:
            {
                HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();

                    GetEmitter()->emitIns_R_R_I(ins, emitTypeSize(intrin.baseType), targetReg, op1Reg, elementIndex,
                                                INS_OPTS_NONE);
                }
            }
            break;

            case NI_AdvSimd_ExtractVector64:
            case NI_AdvSimd_ExtractVector128:
            {
                opt = (intrin.id == NI_AdvSimd_ExtractVector64) ? INS_OPTS_8B : INS_OPTS_16B;

                HWIntrinsicImmOpHelper helper(this, intrin.op3, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();
                    const int byteIndex    = genTypeSize(intrin.baseType) * elementIndex;

                    GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, byteIndex, opt);
                }
            }
            break;

            case NI_AdvSimd_Insert:
                assert(isRMW);

                if (targetReg != op1Reg)
                {
                    GetEmitter()->emitIns_R_R(INS_mov, emitTypeSize(node), targetReg, op1Reg);
                }

                if (intrin.op3->isContainedFltOrDblImmed())
                {
                    assert(intrin.op2->isContainedIntOrIImmed());
                    assert(intrin.op2->AsIntCon()->gtIconVal == 0);

                    const double dataValue = intrin.op3->AsDblCon()->gtDconVal;
                    GetEmitter()->emitIns_R_F(INS_fmov, emitSize, targetReg, dataValue, opt);
                }
                else
                {
                    assert(targetReg != op3Reg);

                    HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                    if (varTypeIsFloating(intrin.baseType))
                    {
                        for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                        {
                            const int elementIndex = helper.ImmValue();

                            GetEmitter()->emitIns_R_R_I_I(ins, emitSize, targetReg, op3Reg, elementIndex, 0, opt);
                        }
                    }
                    else
                    {
                        for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                        {
                            const int elementIndex = helper.ImmValue();

                            GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op3Reg, elementIndex, opt);
                        }
                    }
                }
                break;

            case NI_AdvSimd_InsertScalar:
            {
                assert(isRMW);
                assert(targetReg != op3Reg);

                if (targetReg != op1Reg)
                {
                    GetEmitter()->emitIns_R_R(INS_mov, emitTypeSize(node), targetReg, op1Reg);
                }

                HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();

                    GetEmitter()->emitIns_R_R_I_I(ins, emitSize, targetReg, op3Reg, elementIndex, 0, opt);
                }
            }
            break;

            case NI_AdvSimd_Arm64_InsertSelectedScalar:
            {
                assert(isRMW);
                assert(targetReg != op3Reg);

                if (targetReg != op1Reg)
                {
                    GetEmitter()->emitIns_R_R(INS_mov, emitTypeSize(node), targetReg, op1Reg);
                }

                const int resultIndex = (int)intrin.op2->AsIntCon()->gtIconVal;
                const int valueIndex  = (int)intrin.op4->AsIntCon()->gtIconVal;
                GetEmitter()->emitIns_R_R_I_I(ins, emitSize, targetReg, op3Reg, resultIndex, valueIndex, opt);
            }
            break;

            case NI_AdvSimd_LoadAndInsertScalar:
            {
                assert(isRMW);
                assert(targetReg != op3Reg);

                if (targetReg != op1Reg)
                {
                    GetEmitter()->emitIns_R_R(INS_mov, emitTypeSize(node), targetReg, op1Reg);
                }

                HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();

                    GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op3Reg, elementIndex);
                }
            }
            break;

            case NI_AdvSimd_Store:
                GetEmitter()->emitIns_R_R(ins, emitSize, op2Reg, op1Reg, opt);
                break;

            case NI_AdvSimd_StoreSelectedScalar:
            {
                HWIntrinsicImmOpHelper helper(this, intrin.op3, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();

                    GetEmitter()->emitIns_R_R_I(ins, emitSize, op2Reg, op1Reg, elementIndex, opt);
                }
            }
            break;

            case NI_AdvSimd_Arm64_StorePair:
            case NI_AdvSimd_Arm64_StorePairNonTemporal:
                GetEmitter()->emitIns_R_R_R(ins, emitSize, op2Reg, op3Reg, op1Reg);
                break;

            case NI_AdvSimd_Arm64_StorePairScalar:
            case NI_AdvSimd_Arm64_StorePairScalarNonTemporal:
                GetEmitter()->emitIns_R_R_R(ins, emitTypeSize(intrin.baseType), op2Reg, op3Reg, op1Reg);
                break;

            case NI_Vector64_CreateScalarUnsafe:
            case NI_Vector128_CreateScalarUnsafe:
                if (intrin.op1->isContainedFltOrDblImmed())
                {
                    // fmov reg, #imm8
                    const double dataValue = intrin.op1->AsDblCon()->gtDconVal;
                    GetEmitter()->emitIns_R_F(ins, emitTypeSize(intrin.baseType), targetReg, dataValue, INS_OPTS_NONE);
                }
                else if (varTypeIsFloating(intrin.baseType))
                {
                    // fmov reg1, reg2
                    GetEmitter()->emitIns_R_R(ins, emitTypeSize(intrin.baseType), targetReg, op1Reg, INS_OPTS_NONE);
                }
                else
                {
                    if (intrin.op1->isContainedIntOrIImmed())
                    {
                        // movi/movni reg, #imm8
                        const ssize_t dataValue = intrin.op1->AsIntCon()->gtIconVal;
                        GetEmitter()->emitIns_R_I(INS_movi, emitSize, targetReg, dataValue, opt);
                    }
                    else
                    {
                        // ins reg1[0], reg2
                        GetEmitter()->emitIns_R_R_I(ins, emitTypeSize(intrin.baseType), targetReg, op1Reg, 0,
                                                    INS_OPTS_NONE);
                    }
                }
                break;

            case NI_AdvSimd_AddWideningLower:
            case NI_AdvSimd_AddWideningUpper:
            case NI_AdvSimd_SubtractWideningLower:
            case NI_AdvSimd_SubtractWideningUpper:
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;

            case NI_AdvSimd_Arm64_AddSaturateScalar:
                if (varTypeIsUnsigned(node->GetAuxiliaryType()) != varTypeIsUnsigned(intrin.baseType))
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_usqadd : INS_suqadd;

                    if (targetReg != op1Reg)
                    {
                        GetEmitter()->emitIns_R_R(INS_mov, emitTypeSize(node), targetReg, op1Reg);
                    }

                    GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op2Reg, opt);
                }
                else
                {
                    GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                }
                break;

            // mvni doesn't support the range of element types, so hard code the 'opts' value.
            case NI_Vector64_get_Zero:
            case NI_Vector64_get_AllBitsSet:
                GetEmitter()->emitIns_R_I(ins, emitSize, targetReg, 0, INS_OPTS_2S);
                break;

            case NI_Vector128_get_Zero:
            case NI_Vector128_get_AllBitsSet:
                GetEmitter()->emitIns_R_I(ins, emitSize, targetReg, 0, INS_OPTS_4S);
                break;

            case NI_AdvSimd_DuplicateToVector64:
            case NI_AdvSimd_DuplicateToVector128:
            case NI_AdvSimd_Arm64_DuplicateToVector64:
            case NI_AdvSimd_Arm64_DuplicateToVector128:
            {
                if (varTypeIsFloating(intrin.baseType))
                {
                    if (intrin.op1->isContainedFltOrDblImmed())
                    {
                        const double dataValue = intrin.op1->AsDblCon()->gtDconVal;
                        GetEmitter()->emitIns_R_F(INS_fmov, emitSize, targetReg, dataValue, opt);
                    }
                    else if (intrin.id == NI_AdvSimd_Arm64_DuplicateToVector64)
                    {
                        assert(intrin.baseType == TYP_DOUBLE);
                        GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg, opt);
                    }
                    else
                    {
                        GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op1Reg, 0, opt);
                    }
                }
                else if (intrin.op1->isContainedIntOrIImmed())
                {
                    const ssize_t dataValue = intrin.op1->AsIntCon()->gtIconVal;
                    GetEmitter()->emitIns_R_I(INS_movi, emitSize, targetReg, dataValue, opt);
                }
                else
                {
                    GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg, opt);
                }
            }
            break;

            case NI_Vector64_ToVector128:
                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg);
                break;

            case NI_Vector64_ToVector128Unsafe:
            case NI_Vector128_GetLower:
                if (op1Reg != targetReg)
                {
                    GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg);
                }
                break;

            case NI_Vector64_GetElement:
            case NI_Vector128_GetElement:
            case NI_Vector64_ToScalar:
            case NI_Vector128_ToScalar:
            {
                ssize_t indexValue = 0;
                if ((intrin.id == NI_Vector64_GetElement) || (intrin.id == NI_Vector128_GetElement))
                {
                    assert(intrin.op2->IsCnsIntOrI());
                    indexValue = intrin.op2->AsIntCon()->gtIconVal;
                }

                // no-op if vector is float/double, targetReg == op1Reg and fetching for 0th index.
                if ((varTypeIsFloating(intrin.baseType) && (targetReg == op1Reg) && (indexValue == 0)))
                {
                    break;
                }

                GetEmitter()->emitIns_R_R_I(ins, emitTypeSize(intrin.baseType), targetReg, op1Reg, indexValue,
                                            INS_OPTS_NONE);
            }
            break;

            case NI_AdvSimd_ReverseElement16:
                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg,
                                          (emitSize == EA_8BYTE) ? INS_OPTS_4H : INS_OPTS_8H);
                break;

            case NI_AdvSimd_ReverseElement32:
                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg,
                                          (emitSize == EA_8BYTE) ? INS_OPTS_2S : INS_OPTS_4S);
                break;

            case NI_AdvSimd_ReverseElement8:
                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg,
                                          (emitSize == EA_8BYTE) ? INS_OPTS_8B : INS_OPTS_16B);
                break;

            default:
                unreached();
        }
    }

    genProduceReg(node);
}

#endif // FEATURE_HW_INTRINSICS
