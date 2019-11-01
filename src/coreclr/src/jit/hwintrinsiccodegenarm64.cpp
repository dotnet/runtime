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

//------------------------------------------------------------------------
// genIsTableDrivenHWIntrinsic:
//
// Arguments:
//    category - category of a HW intrinsic
//
// Return Value:
//    returns true if this category can be table-driven in CodeGen
//
static bool genIsTableDrivenHWIntrinsic(NamedIntrinsic intrinsicId, HWIntrinsicCategory category)
{
    // TODO-Arm64-Cleanup - make more categories to the table-driven framework
    const bool tableDrivenCategory =
        (category != HW_Category_Special) && (category != HW_Category_Scalar) && (category != HW_Category_Helper);
    const bool tableDrivenFlag =
        !HWIntrinsicInfo::GeneratesMultipleIns(intrinsicId) && !HWIntrinsicInfo::HasSpecialCodegen(intrinsicId);
    return tableDrivenCategory && tableDrivenFlag;
}

//------------------------------------------------------------------------
// genHWIntrinsic: Generates the code for a given hardware intrinsic node.
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genHWIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic      intrinsicId = node->gtHWIntrinsicId;
    HWIntrinsicCategory category    = HWIntrinsicInfo::lookupCategory(intrinsicId);

    assert(HWIntrinsicInfo::RequiresCodegen(intrinsicId));

    if (genIsTableDrivenHWIntrinsic(intrinsicId, category))
    {
        InstructionSet isa     = HWIntrinsicInfo::lookupIsa(intrinsicId);
        int            ival    = HWIntrinsicInfo::lookupIval(intrinsicId);
        int            numArgs = HWIntrinsicInfo::lookupNumArgs(node);

        assert(numArgs >= 0);

        GenTree*  op1        = node->gtGetOp1();
        GenTree*  op2        = node->gtGetOp2();
        regNumber targetReg  = node->GetRegNum();
        var_types targetType = node->TypeGet();
        var_types baseType   = node->gtSIMDBaseType;

        instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
        assert(ins != INS_invalid);

        regNumber op1Reg   = REG_NA;
        regNumber op2Reg   = REG_NA;
        emitter*  emit     = GetEmitter();
        emitAttr  emitSize = EA_ATTR(node->gtSIMDSize);
        insOpts   opt      = INS_OPTS_NONE;

        if (category == HW_Category_SIMDScalar)
        {
            emitSize = emitActualTypeSize(baseType);
        }
        else
        {
            opt = genGetSimdInsOpt(emitSize, baseType);
        }

        assert(emitSize != 0);
        genConsumeOperands(node);

        switch (numArgs)
        {
            case 1:
            {
                assert(op1 != nullptr);
                assert(op2 == nullptr);

                op1Reg = op1->GetRegNum();
                emit->emitIns_R_R(ins, emitSize, targetReg, op1Reg, opt);
                break;
            }

            case 2:
            {
                assert(op1 != nullptr);
                assert(op2 != nullptr);

                op1Reg = op1->GetRegNum();
                op2Reg = op2->GetRegNum();

                emit->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;
            }

            case 3:
            {
                assert(op1 != nullptr);
                assert(op2 == nullptr);

                GenTreeArgList* argList = op1->AsArgList();
                op1                     = argList->Current();
                op1Reg                  = op1->GetRegNum();

                argList = argList->Rest();
                op2     = argList->Current();
                op2Reg  = op2->GetRegNum();

                argList          = argList->Rest();
                GenTree*  op3    = argList->Current();
                regNumber op3Reg = op3->GetRegNum();

                if (targetReg != op1Reg)
                {
                    emit->emitIns_R_R(INS_mov, emitSize, targetReg, op1Reg);
                }
                emit->emitIns_R_R_R(ins, emitSize, targetReg, op2Reg, op3Reg, opt);
                break;
            }

            default:
            {
                unreached();
            }
        }
        genProduceReg(node);
    }
    else
    {
        genSpecialIntrinsic(node);
    }
}

void CodeGen::genSpecialIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic      intrinsicId = node->gtHWIntrinsicId;
    HWIntrinsicCategory category    = HWIntrinsicInfo::lookupCategory(intrinsicId);

    assert(HWIntrinsicInfo::RequiresCodegen(intrinsicId));

    InstructionSet isa     = HWIntrinsicInfo::lookupIsa(intrinsicId);
    int            ival    = HWIntrinsicInfo::lookupIval(intrinsicId);
    int            numArgs = HWIntrinsicInfo::lookupNumArgs(node);

    assert(numArgs >= 0);

    GenTree*  op1        = node->gtGetOp1();
    GenTree*  op2        = node->gtGetOp2();
    regNumber targetReg  = node->GetRegNum();
    var_types targetType = node->TypeGet();
    var_types baseType   = (category == HW_Category_Scalar) ? op1->TypeGet() : node->gtSIMDBaseType;

    instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
    assert(ins != INS_invalid);

    regNumber op1Reg   = REG_NA;
    regNumber op2Reg   = REG_NA;
    emitter*  emit     = GetEmitter();
    emitAttr  emitSize = EA_ATTR(node->gtSIMDSize);
    insOpts   opt      = INS_OPTS_NONE;

    if ((category == HW_Category_SIMDScalar) || (category == HW_Category_Scalar))
    {
        emitSize = emitActualTypeSize(baseType);
    }
    else
    {
        opt = genGetSimdInsOpt(emitSize, baseType);
    }

    genConsumeOperands(node);

    switch (intrinsicId)
    {
        case NI_Aes_Decrypt:
        case NI_Aes_Encrypt:
        {
            assert(op1 != nullptr);
            assert(op2 != nullptr);

            op1Reg = op1->GetRegNum();
            op2Reg = op2->GetRegNum();

            if (op1Reg != targetReg)
            {
                emit->emitIns_R_R(INS_mov, emitSize, targetReg, op1Reg);
            }
            emit->emitIns_R_R(ins, emitSize, targetReg, op2Reg, opt);
            break;
        }

        case NI_ArmBase_LeadingZeroCount:
        case NI_ArmBase_Arm64_LeadingSignCount:
        case NI_ArmBase_Arm64_LeadingZeroCount:
        {
            assert(op1 != nullptr);
            assert(op2 == nullptr);

            op1Reg = op1->GetRegNum();
            emit->emitIns_R_R(ins, emitSize, targetReg, op1Reg);
            break;
        }

        default:
        {
            unreached();
        }
    }

    genProduceReg(node);
}

#endif // FEATURE_HW_INTRINSICS
