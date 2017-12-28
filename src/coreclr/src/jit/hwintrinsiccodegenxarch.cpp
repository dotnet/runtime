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

    if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        TempDsc* tmpDsc = nullptr;
        unsigned varNum = BAD_VAR_NUM;
        unsigned offset = (unsigned)-1;

        if (op2->isUsedFromSpillTemp())
        {
            assert(op2->IsRegOptional());

            tmpDsc = getSpillTempDsc(op2);
            varNum = tmpDsc->tdTempNum();
            offset = 0;

            compiler->tmpRlsTemp(tmpDsc);
        }
        else if (op2->isIndir())
        {
            GenTreeIndir* memIndir = op2->AsIndir();
            GenTree*      memBase  = memIndir->gtOp1;

            switch (memBase->OperGet())
            {
                case GT_LCL_VAR_ADDR:
                {
                    varNum = memBase->AsLclVarCommon()->GetLclNum();
                    offset = 0;

                    // Ensure that all the GenTreeIndir values are set to their defaults.
                    assert(memBase->gtRegNum == REG_NA);
                    assert(!memIndir->HasIndex());
                    assert(memIndir->Scale() == 1);
                    assert(memIndir->Offset() == 0);

                    break;
                }

                case GT_CLS_VAR_ADDR:
                {
                    emit->emitIns_SIMD_R_R_C(ins, targetReg, op1Reg, memBase->gtClsVar.gtClsVarHnd, 0, targetType);
                    return;
                }

                default:
                {
                    emit->emitIns_SIMD_R_R_A(ins, targetReg, op1Reg, memIndir, targetType);
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
                    unreached();
                    break;
            }
        }

        // Ensure we got a good varNum and offset.
        // We also need to check for `tmpDsc != nullptr` since spill temp numbers
        // are negative and start with -1, which also happens to be BAD_VAR_NUM.
        assert((varNum != BAD_VAR_NUM) || (tmpDsc != nullptr));
        assert(offset != (unsigned)-1);

        emit->emitIns_SIMD_R_R_S(ins, targetReg, op1Reg, varNum, offset, targetType);
    }
    else
    {
        emit->emitIns_SIMD_R_R_R(ins, targetReg, op1Reg, op2->gtRegNum, targetType);
    }
}

void CodeGen::genSSEIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    GenTree*       op3         = nullptr;
    GenTree*       op4         = nullptr;
    regNumber      targetReg   = node->gtRegNum;
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->gtSIMDBaseType;
    instruction    ins         = INS_invalid;

    regNumber op1Reg = REG_NA;
    regNumber op2Reg = REG_NA;
    regNumber op3Reg = REG_NA;
    regNumber op4Reg = REG_NA;
    emitter*  emit   = getEmitter();

    if ((op1 != nullptr) && !op1->OperIsList())
    {
        op1Reg = op1->gtRegNum;
        genConsumeOperands(node);
    }

    switch (intrinsicID)
    {
        case NI_SSE_Add:
        {
            assert(node->TypeGet() == TYP_SIMD16);
            assert(node->gtSIMDBaseType == TYP_FLOAT);
            genHWIntrinsic_R_R_RM(node, INS_addps);
            break;
        }

        case NI_SSE_And:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_andps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;

        case NI_SSE_AndNot:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_andnps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;

        case NI_SSE_CompareEqual:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R_I(INS_cmpps, targetReg, op1Reg, op2Reg, 0, TYP_SIMD16);
            break;

        case NI_SSE_CompareGreaterThan:
        case NI_SSE_CompareNotLessThanOrEqual:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R_I(INS_cmpps, targetReg, op1Reg, op2Reg, 6, TYP_SIMD16);
            break;

        case NI_SSE_CompareGreaterThanOrEqual:
        case NI_SSE_CompareNotLessThan:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R_I(INS_cmpps, targetReg, op1Reg, op2Reg, 5, TYP_SIMD16);
            break;

        case NI_SSE_CompareLessThan:
        case NI_SSE_CompareNotGreaterThanOrEqual:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R_I(INS_cmpps, targetReg, op1Reg, op2Reg, 1, TYP_SIMD16);
            break;

        case NI_SSE_CompareLessThanOrEqual:
        case NI_SSE_CompareNotGreaterThan:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R_I(INS_cmpps, targetReg, op1Reg, op2Reg, 2, TYP_SIMD16);
            break;

        case NI_SSE_CompareNotEqual:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R_I(INS_cmpps, targetReg, op1Reg, op2Reg, 4, TYP_SIMD16);
            break;

        case NI_SSE_CompareOrdered:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R_I(INS_cmpps, targetReg, op1Reg, op2Reg, 7, TYP_SIMD16);
            break;

        case NI_SSE_CompareUnordered:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R_I(INS_cmpps, targetReg, op1Reg, op2Reg, 3, TYP_SIMD16);
            break;

        case NI_SSE_Divide:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_divps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;

        case NI_SSE_Max:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_maxps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;

        case NI_SSE_Min:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_minps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;

        case NI_SSE_MoveHighToLow:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_movhlps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;

        case NI_SSE_MoveLowToHigh:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_movlhps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;

        case NI_SSE_Multiply:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_mulps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;

        case NI_SSE_Or:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_orps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;

        case NI_SSE_Reciprocal:
            assert(baseType == TYP_FLOAT);
            assert(op2 == nullptr);
            emit->emitIns_SIMD_R_R(INS_rcpps, targetReg, op1Reg, TYP_SIMD16);
            break;

        case NI_SSE_ReciprocalSqrt:
            assert(baseType == TYP_FLOAT);
            assert(op2 == nullptr);
            emit->emitIns_SIMD_R_R(INS_rsqrtps, targetReg, op1Reg, TYP_SIMD16);
            break;

        case NI_SSE_SetAllVector128:
            assert(baseType == TYP_FLOAT);
            assert(op2 == nullptr);
            emit->emitIns_SIMD_R_R_R_I(INS_shufps, targetReg, op1Reg, op1Reg, 0, TYP_SIMD16);
            break;

        case NI_SSE_SetZeroVector128:
            assert(baseType == TYP_FLOAT);
            assert(op1 == nullptr);
            assert(op2 == nullptr);
            emit->emitIns_SIMD_R_R_R(INS_xorps, targetReg, targetReg, targetReg, TYP_SIMD16);
            break;

        case NI_SSE_Shuffle:
        {
            GenTreeArgList* argList;

            // Shuffle takes 3 operands, so op1 should be an arg list with two
            // additional node in the chain.
            assert(baseType == TYP_FLOAT);
            assert(op1->OperIsList());
            assert(op1->AsArgList()->Rest() != nullptr);
            assert(op1->AsArgList()->Rest()->Rest() != nullptr);
            assert(op1->AsArgList()->Rest()->Rest()->Rest() == nullptr);
            assert(op2 == nullptr);

            argList = op1->AsArgList();
            op1     = argList->Current();
            op1Reg  = op1->gtRegNum;
            genConsumeRegs(op1);

            argList = argList->Rest();
            op2     = argList->Current();
            op2Reg  = op2->gtRegNum;
            genConsumeRegs(op2);

            argList = argList->Rest();
            op3     = argList->Current();
            genConsumeRegs(op3);

            if (op3->IsCnsIntOrI())
            {
                ssize_t ival = op3->AsIntConCommon()->IconValue();
                emit->emitIns_SIMD_R_R_R_I(INS_shufps, targetReg, op1Reg, op2Reg, (int)ival, TYP_SIMD16);
            }
            else
            {
                // We emit a fallback case for the scenario when op3 is not a constant. This should normally
                // happen when the intrinsic is called indirectly, such as via Reflection. However, it can
                // also occur if the consumer calls it directly and just doesn't pass a constant value.

                const unsigned jmpCount = 256;
                BasicBlock*    jmpTable[jmpCount];

                unsigned jmpTableBase = emit->emitBBTableDataGenBeg(jmpCount, true);
                unsigned jmpTableOffs = 0;

                // Emit the jump table

                JITDUMP("\n      J_M%03u_DS%02u LABEL   DWORD\n", Compiler::s_compMethodsCount, jmpTableBase);

                for (unsigned i = 0; i < jmpCount; i++)
                {
                    jmpTable[i] = genCreateTempLabel();
                    JITDUMP("            DD      L_M%03u_BB%02u\n", Compiler::s_compMethodsCount, jmpTable[i]->bbNum);
                    emit->emitDataGenData(i, jmpTable[i]);
                }

                emit->emitDataGenEnd();

                // Compute and jump to the appropriate offset in the switch table

                regNumber baseReg = node->ExtractTempReg();   // the start of the switch table
                regNumber offsReg = node->GetSingleTempReg(); // the offset into the switch table

                emit->emitIns_R_C(INS_lea, emitTypeSize(TYP_I_IMPL), offsReg, compiler->eeFindJitDataOffs(jmpTableBase),
                                  0);

                emit->emitIns_R_ARX(INS_mov, EA_4BYTE, offsReg, offsReg, op3->gtRegNum, 4, 0);
                emit->emitIns_R_L(INS_lea, EA_PTR_DSP_RELOC, compiler->fgFirstBB, baseReg);
                emit->emitIns_R_R(INS_add, EA_PTRSIZE, offsReg, baseReg);
                emit->emitIns_R(INS_i_jmp, emitTypeSize(TYP_I_IMPL), offsReg);

                // Emit the switch table entries

                BasicBlock* switchTableBeg = genCreateTempLabel();
                BasicBlock* switchTableEnd = genCreateTempLabel();

                genDefineTempLabel(switchTableBeg);

                for (unsigned i = 0; i < jmpCount; i++)
                {
                    genDefineTempLabel(jmpTable[i]);
                    emit->emitIns_SIMD_R_R_R_I(INS_shufps, targetReg, op1Reg, op2Reg, i, TYP_SIMD16);
                    emit->emitIns_J(INS_jmp, switchTableEnd);
                }

                genDefineTempLabel(switchTableEnd);
            }
            break;
        }

        case NI_SSE_Sqrt:
            assert(baseType == TYP_FLOAT);
            assert(op2 == nullptr);
            emit->emitIns_SIMD_R_R(INS_sqrtps, targetReg, op1Reg, TYP_SIMD16);
            break;

        case NI_SSE_StaticCast:
            assert(op2 == nullptr);
            if (op1Reg != targetReg)
            {
                emit->emitIns_SIMD_R_R(INS_movaps, targetReg, op1Reg, TYP_SIMD16);
            }
            break;

        case NI_SSE_Subtract:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_subps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;

        case NI_SSE_UnpackHigh:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_unpckhps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;

        case NI_SSE_UnpackLow:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_unpcklps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
            break;

        case NI_SSE_Xor:
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R(INS_xorps, targetReg, op1Reg, op2Reg, TYP_SIMD16);
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
    var_types      baseType    = node->gtSIMDBaseType;
    instruction    ins         = INS_invalid;

    genConsumeOperands(node);

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
    var_types      baseType    = node->gtSIMDBaseType;
    instruction    ins         = INS_invalid;

    genConsumeOperands(node);

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

    genProduceReg(node);
}

void CodeGen::genAVX2Intrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    var_types      baseType    = node->gtSIMDBaseType;
    instruction    ins         = INS_invalid;

    genConsumeOperands(node);

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
