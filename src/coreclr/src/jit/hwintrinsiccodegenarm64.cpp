// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef FEATURE_HW_INTRINSICS

#include "emit.h"
#include "codegen.h"
#include "sideeffects.h"
#include "lower.h"
#include "gcinfo.h"
#include "gcinfoencoder.h"

struct HWIntrinsic final
{
    HWIntrinsic(const GenTreeHWIntrinsic* node)
        : op1(nullptr), op2(nullptr), op3(nullptr), numOperands(0), baseType(TYP_UNDEF)
    {
        assert(node != nullptr);

        id       = node->gtHWIntrinsicId;
        category = HWIntrinsicInfo::lookupCategory(id);

        assert(HWIntrinsicInfo::RequiresCodegen(id));

        InitializeOperands(node);
        InitializeBaseType(node);
    }

    bool IsTableDriven() const
    {
        // TODO-Arm64-Cleanup - make more categories to the table-driven framework
        bool isTableDrivenCategory = (category != HW_Category_Special) && (category != HW_Category_Helper);
        bool isTableDrivenFlag = !HWIntrinsicInfo::GeneratesMultipleIns(id) && !HWIntrinsicInfo::HasSpecialCodegen(id);

        return isTableDrivenCategory && isTableDrivenFlag;
    }

    NamedIntrinsic      id;
    HWIntrinsicCategory category;
    GenTree*            op1;
    GenTree*            op2;
    GenTree*            op3;
    int                 numOperands;
    var_types           baseType;

private:
    void InitializeOperands(const GenTreeHWIntrinsic* node)
    {
        op1 = node->gtGetOp1();
        op2 = node->gtGetOp2();

        assert(op1 != nullptr);

        if (op1->OperIsList())
        {
            assert(op2 == nullptr);

            GenTreeArgList* list = op1->AsArgList();
            op1                  = list->Current();
            list                 = list->Rest();
            op2                  = list->Current();
            list                 = list->Rest();
            op3                  = list->Current();

            assert(list->Rest() == nullptr);

            numOperands = 3;
        }
        else if (op2 != nullptr)
        {
            numOperands = 2;
        }
        else
        {
            numOperands = 1;
        }
    }

    void InitializeBaseType(const GenTreeHWIntrinsic* node)
    {
        baseType = node->gtSIMDBaseType;

        if (baseType == TYP_UNKNOWN)
        {
            assert(category == HW_Category_Scalar);

            if (HWIntrinsicInfo::BaseTypeFromFirstArg(id))
            {
                assert(op1 != nullptr);
                baseType = op1->TypeGet();
            }
            else if (HWIntrinsicInfo::BaseTypeFromSecondArg(id))
            {
                assert(op2 != nullptr);
                baseType = op2->TypeGet();
            }
            else
            {
                baseType = node->TypeGet();
            }
        }
    }
};

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
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;

            case 3:
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

            case NI_Aes_Decrypt:
            case NI_Aes_Encrypt:
                if (targetReg != op1Reg)
                {
                    GetEmitter()->emitIns_R_R(INS_mov, emitSize, targetReg, op1Reg);
                }
                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op2Reg, opt);
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

            default:
                unreached();
        }
    }

    genProduceReg(node);
}

#endif // FEATURE_HW_INTRINSICS
