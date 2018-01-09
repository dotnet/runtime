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

void CodeGen::genHWIntrinsic_R_R_RM(GenTreeHWIntrinsic* node, instruction ins)
{
    var_types targetType = node->TypeGet();
    regNumber targetReg  = node->gtRegNum;
    GenTree*  op1        = node->gtGetOp1();
    GenTree*  op2        = node->gtGetOp2();
    emitter*  emit       = getEmitter();

    // TODO-XArch-CQ: Commutative operations can have op1 be contained
    // TODO-XArch-CQ: Non-VEX encoded instructions can have both ops contained

    regNumber op1Reg = op1->gtRegNum;

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);

    genConsumeOperands(node);

    if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        unsigned varNum = BAD_VAR_NUM;
        unsigned offset = (unsigned)-1;

        if (op2->isIndir())
        {
            assert(op2->isContained());

            GenTreeIndir* mem     = op2->AsIndir();
            GenTree*      memBase = mem->Base();

            switch (memBase->OperGet())
            {
                case GT_LCL_VAR_ADDR:
                {
                    varNum = memBase->AsLclVarCommon()->GetLclNum();
                    offset = 0;
                    break;
                }

                case GT_CLS_VAR_ADDR:
                {
                    emit->emitIns_SIMD_R_R_C(ins, targetReg, op1Reg, memBase->gtClsVar.gtClsVarHnd, 0, targetType);
                    return;
                }

                default:
                {
                    regNumber indxReg = mem->HasIndex() ? mem->Index()->gtRegNum : REG_NA;
                    emit->emitIns_SIMD_R_R_A(ins, targetReg, op1Reg, memBase->gtRegNum, indxReg, mem->Scale(),
                                             mem->Offset(), targetType);
                    return;
                }
            }
        }
        else
        {
            switch (op2->OperGet())
            {
                case GT_LCL_FLD:
                {
                    GenTreeLclFld* lclField = op2->AsLclFld();

                    varNum = lclField->GetLclNum();
                    offset = lclField->gtLclFld.gtLclOffs;
                    break;
                }

                case GT_LCL_VAR:
                {
                    assert(op2->IsRegOptional() || !compiler->lvaTable[op2->gtLclVar.gtLclNum].lvIsRegCandidate());

                    varNum = op2->AsLclVar()->GetLclNum();
                    offset = 0;
                    break;
                }

                default:
                {
                    assert(op2->isUsedFromSpillTemp());
                    assert(op2->IsRegOptional());

                    TempDsc* tmpDsc = getSpillTempDsc(op2);

                    varNum = tmpDsc->tdTempNum();
                    offset = 0;

                    compiler->tmpRlsTemp(tmpDsc);
                    break;
                }
            }
        }

        emit->emitIns_SIMD_R_R_S(ins, targetReg, op1Reg, varNum, offset, targetType);
    }
    else
    {
        emit->emitIns_SIMD_R_R_R(ins, targetReg, op1Reg, op2->gtRegNum, targetType);
    }

    genProduceReg(node);
}

void CodeGen::genSSEIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    instruction    ins         = INS_invalid;

    switch (intrinsicID)
    {
        case NI_SSE_Add:
        {
            assert(node->TypeGet() == TYP_SIMD16);
            assert(node->gtSIMDBaseType == TYP_FLOAT);
            genHWIntrinsic_R_R_RM(node, INS_addps);
            break;
        }

        default:
            unreached();
            break;
    }
}

void CodeGen::genSSE2Intrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    var_types      baseType    = node->gtSIMDBaseType;
    instruction    ins         = INS_invalid;

    switch (intrinsicID)
    {
        case NI_SSE2_Add:
        {
            assert(node->TypeGet() == TYP_SIMD16);

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

            genHWIntrinsic_R_R_RM(node, ins);
            break;
        }

        default:
            unreached();
            break;
    }
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
    var_types      baseType    = node->gtSIMDBaseType;
    instruction    ins         = INS_invalid;

    switch (intrinsicID)
    {
        case NI_AVX_Add:
        {
            assert(node->TypeGet() == TYP_SIMD32);

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

            genHWIntrinsic_R_R_RM(node, ins);
            break;
        }

        default:
            unreached();
            break;
    }
}

void CodeGen::genAVX2Intrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    var_types      baseType    = node->gtSIMDBaseType;
    instruction    ins         = INS_invalid;

    switch (intrinsicID)
    {
        case NI_AVX2_Add:
        {
            assert(node->TypeGet() == TYP_SIMD32);

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

            genHWIntrinsic_R_R_RM(node, ins);
            break;
        }

        default:
            unreached();
            break;
    }
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
