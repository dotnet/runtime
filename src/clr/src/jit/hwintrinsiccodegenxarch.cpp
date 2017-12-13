// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX               Intel hardware intrinsic Code Generator                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if FEATURE_HW_INTRINSICS

#include "emit.h"
#include "codegen.h"
#include "sideeffects.h"
#include "lower.h"
#include "gcinfo.h"
#include "gcinfoencoder.h"

void CodeGen::genHWIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    InstructionSet isa         = compiler->isaOfHWIntrinsic(intrinsicID);
    switch (isa)
    {
        case InstructionSet_SSE:
            genSSEIntrinsic(node);
            break;
        case InstructionSet_SSE2:
            genSSE2Intrinsic(node);
            break;
        case InstructionSet_SSE3:
            genSSE3Intrinsic(node);
            break;
        case InstructionSet_SSSE3:
            genSSSE3Intrinsic(node);
            break;
        case InstructionSet_SSE41:
            genSSE41Intrinsic(node);
            break;
        case InstructionSet_SSE42:
            genSSE42Intrinsic(node);
            break;
        case InstructionSet_AVX:
            genAVXIntrinsic(node);
            break;
        case InstructionSet_AVX2:
            genAVX2Intrinsic(node);
            break;
        case InstructionSet_AES:
            genAESIntrinsic(node);
            break;
        case InstructionSet_BMI1:
            genBMI1Intrinsic(node);
            break;
        case InstructionSet_BMI2:
            genBMI2Intrinsic(node);
            break;
        case InstructionSet_FMA:
            genFMAIntrinsic(node);
            break;
        case InstructionSet_LZCNT:
            genLZCNTIntrinsic(node);
            break;
        case InstructionSet_PCLMULQDQ:
            genPCLMULQDQIntrinsic(node);
            break;
        case InstructionSet_POPCNT:
            genPOPCNTIntrinsic(node);
            break;
        default:
            unreached();
            break;
    }
}

void CodeGen::genSSEIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    regNumber      targetReg   = node->gtRegNum;
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->gtSIMDBaseType;

    regNumber op1Reg = op1->gtRegNum;
    regNumber op2Reg = REG_NA;
    emitter*  emit   = getEmitter();

    genConsumeOperands(node);

    switch (intrinsicID)
    {
        case NI_SSE_Add:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_addps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;
        default:
            unreached();
            break;
    }
    genProduceReg(node);
}

void CodeGen::genSSE2Intrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    regNumber      targetReg   = node->gtRegNum;
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->gtSIMDBaseType;

    regNumber op1Reg = op1->gtRegNum;
    regNumber op2Reg = REG_NA;
    emitter*  emit   = getEmitter();

    genConsumeOperands(node);

    switch (intrinsicID)
    {
        case NI_SSE2_Add:
        {
            op2Reg = op2->gtRegNum;

            instruction ins;
            switch (baseType)
            {
                case TYP_DOUBLE:
                    ins = INS_addpd;
                    break;
                case TYP_INT:
                case TYP_UINT:
                    ins = INS_paddd;
                    break;
                case TYP_LONG:
                case TYP_ULONG:
                    ins = INS_paddq;
                    break;
                case TYP_BYTE:
                case TYP_UBYTE:
                    ins = INS_paddb;
                    break;
                case TYP_SHORT:
                case TYP_USHORT:
                    ins = INS_paddw;
                    break;
                default:
                    unreached();
                    break;
            }

            emit->emitIns_SIMD_R_R_R(ins, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;
        }
        default:
            unreached();
            break;
    }
    genProduceReg(node);
}

void CodeGen::genSSE3Intrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement SSE3 intrinsic code generation");
}

void CodeGen::genSSSE3Intrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement SSSE3 intrinsic code generation");
}

void CodeGen::genSSE41Intrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement SSE41 intrinsic code generation");
}

void CodeGen::genSSE42Intrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    regNumber      targetReg   = node->gtRegNum;
    assert(targetReg != REG_NA);
    var_types targetType = node->TypeGet();
    var_types baseType   = node->gtSIMDBaseType;

    regNumber op1Reg = op1->gtRegNum;
    regNumber op2Reg = op2->gtRegNum;
    genConsumeOperands(node);

    switch (intrinsicID)
    {
        case NI_SSE42_Crc32:
            if (op1Reg != targetReg)
            {
                inst_RV_RV(INS_mov, targetReg, op1Reg, targetType, emitTypeSize(targetType));
            }

            if (baseType == TYP_UBYTE || baseType == TYP_USHORT) // baseType is the type of the second argument
            {
                assert(targetType == TYP_INT);
                inst_RV_RV(INS_crc32, targetReg, op2Reg, baseType, emitTypeSize(baseType));
            }
            else
            {
                assert(op1->TypeGet() == op2->TypeGet());
                assert(targetType == TYP_INT || targetType == TYP_LONG);
                inst_RV_RV(INS_crc32, targetReg, op2Reg, targetType, emitTypeSize(targetType));
            }

            break;
        default:
            unreached();
            break;
    }
    genProduceReg(node);
}

void CodeGen::genAVXIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    regNumber      targetReg   = node->gtRegNum;
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->gtSIMDBaseType;

    regNumber op1Reg = op1->gtRegNum;
    regNumber op2Reg = REG_NA;

    genConsumeOperands(node);

    emitter* emit = getEmitter();
    switch (intrinsicID)
    {
        case NI_AVX_Add:
        {
            op2Reg = op2->gtRegNum;

            instruction ins;
            switch (baseType)
            {
                case TYP_DOUBLE:
                    ins = INS_addpd;
                    break;
                case TYP_FLOAT:
                    ins = INS_addps;
                    break;
                default:
                    unreached();
                    break;
            }

            emit->emitIns_R_R_R(ins, emitTypeSize(TYP_SIMD32), targetReg, op1Reg, op2Reg);
            break;
        }
        default:
            unreached();
            break;
    }
    genProduceReg(node);
}

void CodeGen::genAVX2Intrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    regNumber      targetReg   = node->gtRegNum;
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->gtSIMDBaseType;

    regNumber op1Reg = op1->gtRegNum;
    regNumber op2Reg = REG_NA;

    genConsumeOperands(node);

    emitter* emit = getEmitter();
    switch (intrinsicID)
    {
        case NI_AVX2_Add:
        {
            op2Reg = op2->gtRegNum;

            instruction ins;
            switch (baseType)
            {
                case TYP_INT:
                case TYP_UINT:
                    ins = INS_paddd;
                    break;
                case TYP_LONG:
                case TYP_ULONG:
                    ins = INS_paddq;
                    break;
                case TYP_BYTE:
                case TYP_UBYTE:
                    ins = INS_paddb;
                    break;
                case TYP_SHORT:
                case TYP_USHORT:
                    ins = INS_paddw;
                    break;
                default:
                    unreached();
                    break;
            }

            emit->emitIns_R_R_R(ins, emitTypeSize(TYP_SIMD32), targetReg, op1Reg, op2Reg);
            break;
        }
        default:
            unreached();
            break;
    }
    genProduceReg(node);
}

void CodeGen::genAESIntrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement AES intrinsic code generation");
}

void CodeGen::genBMI1Intrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement BMI1 intrinsic code generation");
}

void CodeGen::genBMI2Intrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement BMI2 intrinsic code generation");
}

void CodeGen::genFMAIntrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement FMA intrinsic code generation");
}

void CodeGen::genLZCNTIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    GenTree*       op1         = node->gtGetOp1();
    regNumber      targetReg   = node->gtRegNum;
    assert(targetReg != REG_NA);
    var_types targetType = node->TypeGet();
    regNumber op1Reg     = op1->gtRegNum;
    genConsumeOperands(node);

    assert(intrinsicID == NI_LZCNT_LeadingZeroCount);

    inst_RV_RV(INS_lzcnt, targetReg, op1Reg, targetType, emitTypeSize(targetType));

    genProduceReg(node);
}

void CodeGen::genPCLMULQDQIntrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement PCLMULQDQ intrinsic code generation");
}

void CodeGen::genPOPCNTIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    GenTree*       op1         = node->gtGetOp1();
    regNumber      targetReg   = node->gtRegNum;
    assert(targetReg != REG_NA);
    var_types targetType = node->TypeGet();
    regNumber op1Reg     = op1->gtRegNum;
    genConsumeOperands(node);

    assert(intrinsicID == NI_POPCNT_PopCount);

    inst_RV_RV(INS_popcnt, targetReg, op1Reg, targetType, emitTypeSize(targetType));

    genProduceReg(node);
}

#endif // FEATURE_HW_INTRINSICS
