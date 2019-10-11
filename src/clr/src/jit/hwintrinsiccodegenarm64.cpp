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
    const bool tableDrivenCategory = (category != HW_Category_Scalar) && (category != HW_Category_Helper);
    const bool tableDrivenFlag     = true;
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
    InstructionSet      isa         = HWIntrinsicInfo::lookupIsa(intrinsicId);
    HWIntrinsicCategory category    = HWIntrinsicInfo::lookupCategory(intrinsicId);
    int                 ival        = HWIntrinsicInfo::lookupIval(intrinsicId);
    int                 numArgs     = HWIntrinsicInfo::lookupNumArgs(node);

    assert(HWIntrinsicInfo::RequiresCodegen(intrinsicId));

    if (genIsTableDrivenHWIntrinsic(intrinsicId, category))
    {
        GenTree*  op1        = node->gtGetOp1();
        GenTree*  op2        = node->gtGetOp2();
        regNumber targetReg  = node->GetRegNum();
        var_types targetType = node->TypeGet();
        var_types baseType   = node->gtSIMDBaseType;

        regNumber op1Reg = REG_NA;
        regNumber op2Reg = REG_NA;
        emitter*  emit   = GetEmitter();

        assert(numArgs >= 0);
        instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
        assert(ins != INS_invalid);
        emitAttr simdSize = EA_ATTR(node->gtSIMDSize);
        insOpts  opt      = INS_OPTS_NONE;

        if (category == HW_Category_SIMDScalar)
        {
            simdSize = emitActualTypeSize(baseType);
        }
        else
        {
            opt = genGetSimdInsOpt(simdSize, baseType);
        }

        assert(simdSize != 0);

        switch (numArgs)
        {
            case 1:
            {
                genConsumeRegs(op1);
                op1Reg = op1->GetRegNum();
                GetEmitter()->emitIns_R_R(ins, simdSize, targetReg, op1Reg, opt);
                break;
            }

            case 2:
            {
                genConsumeRegs(op1);
                genConsumeRegs(op2);

                op1Reg = op1->GetRegNum();
                op2Reg = op2->GetRegNum();

                GetEmitter()->emitIns_R_R_R(ins, simdSize, targetReg, op1Reg, op2Reg, opt);
                break;
            }

            case 3:
            {
                GenTreeArgList* argList = op1->AsArgList();
                op1                     = argList->Current();
                genConsumeRegs(op1);
                op1Reg = op1->GetRegNum();

                argList = argList->Rest();
                op2     = argList->Current();
                genConsumeRegs(op2);
                op2Reg = op2->GetRegNum();

                argList      = argList->Rest();
                GenTree* op3 = argList->Current();
                genConsumeRegs(op3);
                regNumber op3Reg = op3->GetRegNum();

                if (targetReg != op1Reg)
                {
                    GetEmitter()->emitIns_R_R(INS_mov, simdSize, targetReg, op1Reg);
                }
                GetEmitter()->emitIns_R_R_R(ins, simdSize, targetReg, op2Reg, op3Reg);
                break;
            }

            default:
            {
                unreached();
                break;
            }
        }
        genProduceReg(node);
        return;
    }

    genSpecialIntrinsic(node);
}

void CodeGen::genSpecialIntrinsic(GenTreeHWIntrinsic* node)
{
    unreached();
}

#endif // FEATURE_HW_INTRINSICS
