// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef FEATURE_HW_INTRINSICS

#include "codegen.h"

//------------------------------------------------------------------------
// genHWIntrinsic: Generates the code for a given hardware intrinsic node.
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genHWIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic id = node->GetHWIntrinsicId();

    HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(id);
    assert(category == HW_Category_Scalar);

    // We need to validate that other phases of the compiler haven't introduced unsupported intrinsics
    assert(compiler->compIsaSupportedDebugOnly(HWIntrinsicInfo::lookupIsa(id)));

    size_t opCount = node->GetOperandCount();
    assert(opCount <= 3);

    regNumber destReg = node->GetRegNum();
    regNumber op1Reg  = (opCount >= 1) ? node->Op(1)->GetRegNum() : REG_NA;
    regNumber op2Reg  = (opCount >= 2) ? node->Op(2)->GetRegNum() : REG_NA;
    regNumber op3Reg  = (opCount >= 3) ? node->Op(3)->GetRegNum() : REG_NA;

    var_types type     = genActualType(node);
    emitAttr  emitSize = emitActualTypeSize(type);

    assert(!node->isRMWHWIntrinsic(compiler));
    assert(!HWIntrinsicInfo::HasImmediateOperand(id));

    genConsumeMultiOpOperands(node);

    instruction ins = HWIntrinsicInfo::lookupIns(id, type);
    assert(ins != INS_invalid);

    switch (id)
    {
        case NI_RiscV64Base_FusedMultiplyAddScalar:
        case NI_RiscV64Base_FusedMultiplySubtractScalar:
        case NI_RiscV64Base_FusedNegatedMultiplyAddScalar:
        case NI_RiscV64Base_FusedNegatedMultiplySubtractScalar:
            assert(opCount == 3);
            GetEmitter()->emitIns_R_R_R_R(ins, emitSize, destReg, op1Reg, op2Reg, op3Reg);
            break;

        default:
            unreached();
    }

    genProduceReg(node);
}

#endif // FEATURE_HW_INTRINSICS
