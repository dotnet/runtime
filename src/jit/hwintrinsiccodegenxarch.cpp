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
static bool genIsTableDrivenHWIntrinsic(HWIntrinsicCategory category, HWIntrinsicFlag flags)
{
    // TODO - make more categories to the table-driven framework
    // HW_Category_Helper and HW_Flag_MultiIns/HW_Flag_SpecialCodeGen usually need manual codegen
    const bool tableDrivenCategory =
        category != HW_Category_Special && category != HW_Category_Scalar && category != HW_Category_Helper;
    const bool tableDrivenFlag = (flags & (HW_Flag_MultiIns | HW_Flag_SpecialCodeGen)) == 0;
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
    NamedIntrinsic      intrinsicID = node->gtHWIntrinsicId;
    InstructionSet      isa         = Compiler::isaOfHWIntrinsic(intrinsicID);
    HWIntrinsicCategory category    = Compiler::categoryOfHWIntrinsic(intrinsicID);
    HWIntrinsicFlag     flags       = Compiler::flagsOfHWIntrinsic(intrinsicID);
    int                 ival        = Compiler::ivalOfHWIntrinsic(intrinsicID);
    int                 numArgs     = Compiler::numArgsOfHWIntrinsic(node);

    assert((flags & HW_Flag_NoCodeGen) == 0);

    if (genIsTableDrivenHWIntrinsic(category, flags))
    {
        GenTree*  op1        = node->gtGetOp1();
        GenTree*  op2        = node->gtGetOp2();
        regNumber targetReg  = node->gtRegNum;
        var_types targetType = node->TypeGet();
        var_types baseType   = node->gtSIMDBaseType;

        regNumber op1Reg = REG_NA;
        regNumber op2Reg = REG_NA;
        emitter*  emit   = getEmitter();

        assert(numArgs >= 0);
        instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);
        assert(ins != INS_invalid);
        emitAttr simdSize = EA_ATTR(node->gtSIMDSize);
        assert(simdSize != 0);

        switch (numArgs)
        {
            case 1:
            {
                genConsumeOperands(node);
                op1Reg = op1->gtRegNum;
                if (category == HW_Category_MemoryLoad)
                {
                    emit->emitIns_R_AR(ins, simdSize, targetReg, op1Reg, 0);
                }
                else if (category == HW_Category_SIMDScalar && (flags & HW_Flag_CopyUpperBits) != 0)
                {
                    emit->emitIns_SIMD_R_R_R(ins, simdSize, targetReg, op1Reg, op1Reg);
                }
                else if ((ival != -1) && varTypeIsFloating(baseType))
                {
                    emit->emitIns_R_R_I(ins, simdSize, targetReg, op1Reg, ival);
                }
                else
                {
                    emit->emitIns_R_R(ins, simdSize, targetReg, op1Reg);
                }
                break;
            }

            case 2:
            {
                genConsumeOperands(node);

                op1Reg = op1->gtRegNum;
                op2Reg = op2->gtRegNum;

                if ((op1Reg != targetReg) && (op2Reg == targetReg) && node->isRMWHWIntrinsic(compiler))
                {
                    // We have "reg2 = reg1 op reg2" where "reg1 != reg2" on a RMW intrinsic.
                    //
                    // For non-commutative intrinsics, we should have ensured that op2 was marked
                    // delay free in order to prevent it from getting assigned the same register
                    // as target. However, for commutative intrinsics, we can just swap the operands
                    // in order to have "reg2 = reg2 op reg1" which will end up producing the right code.

                    noway_assert(node->OperIsCommutative());
                    op2Reg = op1Reg;
                    op1Reg = targetReg;
                }

                if (category == HW_Category_MemoryStore)
                {
                    emit->emitIns_AR_R(ins, simdSize, op2Reg, op1Reg, 0);
                }
                else if ((ival != -1) && varTypeIsFloating(baseType))
                {
                    genHWIntrinsic_R_R_RM_I(node, ins);
                }
                else if (category == HW_Category_MemoryLoad)
                {
                    emit->emitIns_SIMD_R_R_AR(ins, simdSize, targetReg, op1Reg, op2Reg);
                }
                else if (Compiler::isImmHWIntrinsic(intrinsicID, op2))
                {
                    if (intrinsicID == NI_SSE2_Extract)
                    {
                        // extract instructions return to GP-registers, so it needs int size as the emitsize
                        simdSize = emitTypeSize(TYP_INT);
                    }
                    auto emitSwCase = [&](unsigned i) {
                        emit->emitIns_SIMD_R_R_I(ins, simdSize, targetReg, op1Reg, (int)i);
                    };

                    if (op2->IsCnsIntOrI())
                    {
                        ssize_t ival = op2->AsIntCon()->IconValue();
                        emitSwCase((unsigned)ival);
                    }
                    else
                    {
                        // We emit a fallback case for the scenario when the imm-op is not a constant. This should
                        // normally happen when the intrinsic is called indirectly, such as via Reflection. However, it
                        // can also occur if the consumer calls it directly and just doesn't pass a constant value.
                        regNumber baseReg = node->ExtractTempReg();
                        regNumber offsReg = node->GetSingleTempReg();
                        genHWIntrinsicJumpTableFallback(intrinsicID, op2Reg, baseReg, offsReg, emitSwCase);
                    }
                }
                else
                {
                    genHWIntrinsic_R_R_RM(node, ins);
                }
                break;
            }

            case 3:
            {
                assert(op1->OperIsList());
                assert(op1->gtGetOp2()->OperIsList());
                assert(op1->gtGetOp2()->gtGetOp2()->OperIsList());

                GenTreeArgList* argList = op1->AsArgList();
                op1                     = argList->Current();
                genConsumeRegs(op1);
                op1Reg = op1->gtRegNum;

                argList = argList->Rest();
                op2     = argList->Current();
                genConsumeRegs(op2);
                op2Reg = op2->gtRegNum;

                argList      = argList->Rest();
                GenTree* op3 = argList->Current();
                genConsumeRegs(op3);
                regNumber op3Reg = op3->gtRegNum;

                if (Compiler::isImmHWIntrinsic(intrinsicID, op3))
                {
                    auto emitSwCase = [&](unsigned i) {
                        emit->emitIns_SIMD_R_R_R_I(ins, simdSize, targetReg, op1Reg, op2Reg, (int)i);
                    };
                    if (op3->IsCnsIntOrI())
                    {
                        ssize_t ival = op3->AsIntCon()->IconValue();
                        emitSwCase((unsigned)ival);
                    }
                    else
                    {
                        // We emit a fallback case for the scenario when the imm-op is not a constant. This should
                        // normally happen when the intrinsic is called indirectly, such as via Reflection. However, it
                        // can also occur if the consumer calls it directly and just doesn't pass a constant value.
                        regNumber baseReg = node->ExtractTempReg();
                        regNumber offsReg = node->GetSingleTempReg();
                        genHWIntrinsicJumpTableFallback(intrinsicID, op3Reg, baseReg, offsReg, emitSwCase);
                    }
                }
                else if (category == HW_Category_MemoryStore)
                {
                    assert(intrinsicID == NI_SSE2_MaskMove);
                    assert(targetReg == REG_NA);

                    // SSE2 MaskMove hardcodes the destination (op3) in DI/EDI/RDI
                    if (op3Reg != REG_EDI)
                    {
                        emit->emitIns_R_R(INS_mov, EA_PTRSIZE, REG_EDI, op3Reg);
                    }
                    emit->emitIns_R_R(ins, simdSize, op1Reg, op2Reg);
                }
                else
                {
                    emit->emitIns_SIMD_R_R_R_R(ins, simdSize, targetReg, op1Reg, op2Reg, op3Reg);
                }
                break;
            }

            default:
                unreached();
                break;
        }
        genProduceReg(node);
        return;
    }

    switch (isa)
    {
        case InstructionSet_SSE:
            genSSEIntrinsic(node);
            break;
        case InstructionSet_SSE2:
            genSSE2Intrinsic(node);
            break;
        case InstructionSet_SSE41:
            genSSE41Intrinsic(node);
            break;
        case InstructionSet_SSE42:
            genSSE42Intrinsic(node);
            break;
        case InstructionSet_AVX:
        case InstructionSet_AVX2:
            genAvxOrAvx2Intrinsic(node);
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

//------------------------------------------------------------------------
// genHWIntrinsic_R_R_RM: Generates the code for a hardware intrinsic node that takes a register operand, a
//                        register/memory operand, and that returns a value in register
//
// Arguments:
//    node - The hardware intrinsic node
//    ins  - The instruction being generated
//
void CodeGen::genHWIntrinsic_R_R_RM(GenTreeHWIntrinsic* node, instruction ins)
{
    var_types targetType = node->TypeGet();
    regNumber targetReg  = node->gtRegNum;
    GenTree*  op1        = node->gtGetOp1();
    GenTree*  op2        = node->gtGetOp2();
    emitAttr  simdSize   = EA_ATTR(node->gtSIMDSize);
    emitter*  emit       = getEmitter();

    // TODO-XArch-CQ: Commutative operations can have op1 be contained
    // TODO-XArch-CQ: Non-VEX encoded instructions can have both ops contained

    regNumber op1Reg = op1->gtRegNum;

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);

    if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        assert((Compiler::flagsOfHWIntrinsic(node->gtHWIntrinsicId) & HW_Flag_NoContainment) == 0);
        assert(compiler->m_pLowering->IsContainableHWIntrinsicOp(node, op2) || op2->IsRegOptional());

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
        else if (op2->OperIsHWIntrinsic())
        {
            emit->emitIns_SIMD_R_R_AR(ins, simdSize, targetReg, op1Reg, op2->gtGetOp1()->gtRegNum);
            return;
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
                    assert(!memIndir->HasIndex());
                    assert(memIndir->Scale() == 1);
                    assert(memIndir->Offset() == 0);

                    break;
                }

                case GT_CLS_VAR_ADDR:
                {
                    emit->emitIns_SIMD_R_R_C(ins, simdSize, targetReg, op1Reg, memBase->gtClsVar.gtClsVarHnd, 0);
                    return;
                }

                default:
                {
                    emit->emitIns_SIMD_R_R_A(ins, simdSize, targetReg, op1Reg, memIndir);
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

        emit->emitIns_SIMD_R_R_S(ins, simdSize, targetReg, op1Reg, varNum, offset);
    }
    else
    {
        regNumber op2Reg = op2->gtRegNum;

        if ((op1Reg != targetReg) && (op2Reg == targetReg) && node->isRMWHWIntrinsic(compiler))
        {
            // We have "reg2 = reg1 op reg2" where "reg1 != reg2" on a RMW intrinsic.
            //
            // For non-commutative intrinsics, we should have ensured that op2 was marked
            // delay free in order to prevent it from getting assigned the same register
            // as target. However, for commutative intrinsics, we can just swap the operands
            // in order to have "reg2 = reg2 op reg1" which will end up producing the right code.

            noway_assert(node->OperIsCommutative());
            op2Reg = op1Reg;
            op1Reg = targetReg;
        }

        emit->emitIns_SIMD_R_R_R(ins, simdSize, targetReg, op1Reg, op2Reg);
    }
}

//------------------------------------------------------------------------
// genHWIntrinsic_R_R_RM_I: Generates the code for a hardware intrinsic node that takes a register operand, a
//                        register/memory operand, an immediate operand, and that returns a value in register
//
// Arguments:
//    node - The hardware intrinsic node
//    ins  - The instruction being generated
//
void CodeGen::genHWIntrinsic_R_R_RM_I(GenTreeHWIntrinsic* node, instruction ins)
{
    var_types targetType = node->TypeGet();
    regNumber targetReg  = node->gtRegNum;
    GenTree*  op1        = node->gtGetOp1();
    GenTree*  op2        = node->gtGetOp2();
    emitAttr  simdSize   = EA_ATTR(node->gtSIMDSize);
    int       ival       = Compiler::ivalOfHWIntrinsic(node->gtHWIntrinsicId);
    emitter*  emit       = getEmitter();

    // TODO-XArch-CQ: Commutative operations can have op1 be contained
    // TODO-XArch-CQ: Non-VEX encoded instructions can have both ops contained

    regNumber op1Reg = op1->gtRegNum;

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);

    if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        assert((Compiler::flagsOfHWIntrinsic(node->gtHWIntrinsicId) & HW_Flag_NoContainment) == 0);
        assert(compiler->m_pLowering->IsContainableHWIntrinsicOp(node, op2) || op2->IsRegOptional());

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
        else if (op2->OperIsHWIntrinsic())
        {
            emit->emitIns_SIMD_R_R_AR_I(ins, simdSize, targetReg, op1Reg, op2->gtGetOp1()->gtRegNum, ival);
            return;
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
                    assert(!memIndir->HasIndex());
                    assert(memIndir->Scale() == 1);
                    assert(memIndir->Offset() == 0);

                    break;
                }

                case GT_CLS_VAR_ADDR:
                {
                    emit->emitIns_SIMD_R_R_C_I(ins, simdSize, targetReg, op1Reg, memBase->gtClsVar.gtClsVarHnd, 0,
                                               ival);
                    return;
                }

                default:
                {
                    emit->emitIns_SIMD_R_R_A_I(ins, simdSize, targetReg, op1Reg, memIndir, ival);
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

        emit->emitIns_SIMD_R_R_S_I(ins, simdSize, targetReg, op1Reg, varNum, offset, ival);
    }
    else
    {
        regNumber op2Reg = op2->gtRegNum;

        if ((op1Reg != targetReg) && (op2Reg == targetReg) && node->isRMWHWIntrinsic(compiler))
        {
            // We have "reg2 = reg1 op reg2" where "reg1 != reg2" on a RMW intrinsic.
            //
            // For non-commutative intrinsics, we should have ensured that op2 was marked
            // delay free in order to prevent it from getting assigned the same register
            // as target. However, for commutative intrinsics, we can just swap the operands
            // in order to have "reg2 = reg2 op reg1" which will end up producing the right code.

            noway_assert(node->OperIsCommutative());
            op2Reg = op1Reg;
            op1Reg = targetReg;
        }

        emit->emitIns_SIMD_R_R_R_I(ins, simdSize, targetReg, op1Reg, op2Reg, ival);
    }
}

// genHWIntrinsicJumpTableFallback : generate the jump-table fallback for imm-intrinsics
//                       with non-constant argument
//
// Arguments:
//    intrinsic      - intrinsic ID
//    nonConstImmReg - the register contains non-constant imm8 argument
//    baseReg        - a register for the start of the switch table
//    offsReg        - a register for the offset into the switch table
//    emitSwCase     - the lambda to generate siwtch-case
//
// Return Value:
//    generate the jump-table fallback for imm-intrinsics with non-constant argument.
// Note:
//    This function can be used for all imm-intrinsics (whether full-range or not),
//    The compiler front-end (i.e. importer) is responsible to insert a range-check IR
//    (GT_HW_INTRINSIC_CHK) for imm8 argument, so this function does not need to do range-check.
//
template <typename HWIntrinsicSwitchCaseBody>
void CodeGen::genHWIntrinsicJumpTableFallback(NamedIntrinsic            intrinsic,
                                              regNumber                 nonConstImmReg,
                                              regNumber                 baseReg,
                                              regNumber                 offsReg,
                                              HWIntrinsicSwitchCaseBody emitSwCase)
{
    assert(nonConstImmReg != REG_NA);
    emitter* emit = getEmitter();

    const unsigned maxByte = (unsigned)Compiler::immUpperBoundOfHWIntrinsic(intrinsic) + 1;
    assert(maxByte <= 256);
    BasicBlock* jmpTable[256];

    unsigned jmpTableBase = emit->emitBBTableDataGenBeg(maxByte, true);
    unsigned jmpTableOffs = 0;

    // Emit the jump table
    for (unsigned i = 0; i < maxByte; i++)
    {
        jmpTable[i] = genCreateTempLabel();
        emit->emitDataGenData(i, jmpTable[i]);
    }

    emit->emitDataGenEnd();

    // Compute and jump to the appropriate offset in the switch table
    emit->emitIns_R_C(INS_lea, emitTypeSize(TYP_I_IMPL), offsReg, compiler->eeFindJitDataOffs(jmpTableBase), 0);

    emit->emitIns_R_ARX(INS_mov, EA_4BYTE, offsReg, offsReg, nonConstImmReg, 4, 0);
    emit->emitIns_R_L(INS_lea, EA_PTR_DSP_RELOC, compiler->fgFirstBB, baseReg);
    emit->emitIns_R_R(INS_add, EA_PTRSIZE, offsReg, baseReg);
    emit->emitIns_R(INS_i_jmp, emitTypeSize(TYP_I_IMPL), offsReg);

    // Emit the switch table entries

    BasicBlock* switchTableBeg = genCreateTempLabel();
    BasicBlock* switchTableEnd = genCreateTempLabel();

    genDefineTempLabel(switchTableBeg);

    for (unsigned i = 0; i < maxByte; i++)
    {
        genDefineTempLabel(jmpTable[i]);
        emitSwCase(i);
        emit->emitIns_J(INS_jmp, switchTableEnd);
    }

    genDefineTempLabel(switchTableEnd);
}

//------------------------------------------------------------------------
// genSSEIntrinsic: Generates the code for an SSE hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
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
        case NI_SSE_CompareEqualOrderedScalar:
        case NI_SSE_CompareEqualUnorderedScalar:
        {
            assert(baseType == TYP_FLOAT);
            op2Reg             = op2->gtRegNum;
            regNumber   tmpReg = node->GetSingleTempReg();
            instruction ins    = Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType);

            // Ensure we aren't overwriting targetReg
            assert(tmpReg != targetReg);

            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), op1Reg, op2Reg);
            emit->emitIns_R(INS_setpo, EA_1BYTE, targetReg);
            emit->emitIns_R(INS_sete, EA_1BYTE, tmpReg);
            emit->emitIns_R_R(INS_and, EA_1BYTE, tmpReg, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, tmpReg);
            break;
        }

        case NI_SSE_CompareGreaterThanOrderedScalar:
        case NI_SSE_CompareGreaterThanUnorderedScalar:
        {
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;

            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType);
            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), op1Reg, op2Reg);
            emit->emitIns_R(INS_seta, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE_CompareGreaterThanOrEqualOrderedScalar:
        case NI_SSE_CompareGreaterThanOrEqualUnorderedScalar:
        {
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;

            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType);
            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), op1Reg, op2Reg);
            emit->emitIns_R(INS_setae, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE_CompareLessThanOrderedScalar:
        case NI_SSE_CompareLessThanUnorderedScalar:
        {
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;

            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType);
            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), op2Reg, op1Reg);
            emit->emitIns_R(INS_seta, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE_CompareLessThanOrEqualOrderedScalar:
        case NI_SSE_CompareLessThanOrEqualUnorderedScalar:
        {
            assert(baseType == TYP_FLOAT);
            op2Reg = op2->gtRegNum;

            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType);
            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), op2Reg, op1Reg);
            emit->emitIns_R(INS_setae, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE_CompareNotEqualOrderedScalar:
        case NI_SSE_CompareNotEqualUnorderedScalar:
        {
            assert(baseType == TYP_FLOAT);
            op2Reg             = op2->gtRegNum;
            regNumber   tmpReg = node->GetSingleTempReg();
            instruction ins    = Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType);

            // Ensure we aren't overwriting targetReg
            assert(tmpReg != targetReg);

            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), op1Reg, op2Reg);
            emit->emitIns_R(INS_setpe, EA_1BYTE, targetReg);
            emit->emitIns_R(INS_setne, EA_1BYTE, tmpReg);
            emit->emitIns_R_R(INS_or, EA_1BYTE, tmpReg, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, tmpReg);
            break;
        }

        case NI_SSE_ConvertToSingle:
        case NI_SSE_StaticCast:
        {
            assert(op2 == nullptr);
            if (op1Reg != targetReg)
            {
                instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType);
                emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), targetReg, op1Reg);
            }
            break;
        }

        case NI_SSE_MoveMask:
        {
            assert(baseType == TYP_FLOAT);
            assert(op2 == nullptr);

            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType);
            emit->emitIns_R_R(ins, emitTypeSize(TYP_INT), targetReg, op1Reg);
            break;
        }

        case NI_SSE_Prefetch0:
        case NI_SSE_Prefetch1:
        case NI_SSE_Prefetch2:
        case NI_SSE_PrefetchNonTemporal:
        {
            assert(baseType == TYP_UBYTE);
            assert(op2 == nullptr);

            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType);
            emit->emitIns_AR(ins, emitTypeSize(baseType), op1Reg, 0);
            break;
        }

        case NI_SSE_SetScalarVector128:
        {
            assert(baseType == TYP_FLOAT);
            assert(op2 == nullptr);

            if (op1Reg == targetReg)
            {
                regNumber tmpReg = node->GetSingleTempReg();

                // Ensure we aren't overwriting targetReg
                assert(tmpReg != targetReg);

                emit->emitIns_R_R(INS_movaps, emitTypeSize(TYP_SIMD16), tmpReg, op1Reg);
                op1Reg = tmpReg;
            }

            emit->emitIns_SIMD_R_R_R(INS_xorps, emitTypeSize(TYP_SIMD16), targetReg, targetReg, targetReg);
            emit->emitIns_SIMD_R_R_R(INS_movss, emitTypeSize(TYP_SIMD16), targetReg, targetReg, op1Reg);
            break;
        }

        case NI_SSE_SetZeroVector128:
        {
            assert(baseType == TYP_FLOAT);
            assert(op1 == nullptr);
            assert(op2 == nullptr);
            emit->emitIns_SIMD_R_R_R(INS_xorps, emitTypeSize(TYP_SIMD16), targetReg, targetReg, targetReg);
            break;
        }

        case NI_SSE_StoreFence:
        {
            assert(baseType == TYP_VOID);
            assert(op1 == nullptr);
            assert(op2 == nullptr);
            emit->emitIns(INS_sfence);
            break;
        }

        default:
            unreached();
            break;
    }

    genProduceReg(node);
}

//------------------------------------------------------------------------
// genSSE2Intrinsic: Generates the code for an SSE2 hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genSSE2Intrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    regNumber      targetReg   = node->gtRegNum;
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->gtSIMDBaseType;
    regNumber      op1Reg      = REG_NA;
    regNumber      op2Reg      = REG_NA;
    emitter*       emit        = getEmitter();
    int            ival        = -1;

    if ((op1 != nullptr) && !op1->OperIsList())
    {
        op1Reg = op1->gtRegNum;
        genConsumeOperands(node);
    }

    switch (intrinsicID)
    {
        // All integer overloads are handled by table codegen
        case NI_SSE2_CompareLessThan:
        {
            assert(op1 != nullptr);
            assert(op2 != nullptr);
            assert(baseType == TYP_DOUBLE);
            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);
            op2Reg          = op2->gtRegNum;
            ival            = Compiler::ivalOfHWIntrinsic(intrinsicID);
            assert(ival != -1);
            emit->emitIns_SIMD_R_R_R_I(ins, emitTypeSize(TYP_SIMD16), targetReg, op1Reg, op2Reg, ival);

            break;
        }

        case NI_SSE2_CompareEqualOrderedScalar:
        case NI_SSE2_CompareEqualUnorderedScalar:
        {
            assert(baseType == TYP_DOUBLE);
            op2Reg             = op2->gtRegNum;
            regNumber   tmpReg = node->GetSingleTempReg();
            instruction ins    = Compiler::insOfHWIntrinsic(intrinsicID, baseType);

            // Ensure we aren't overwriting targetReg
            assert(tmpReg != targetReg);

            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), op1Reg, op2Reg);
            emit->emitIns_R(INS_setpo, EA_1BYTE, targetReg);
            emit->emitIns_R(INS_sete, EA_1BYTE, tmpReg);
            emit->emitIns_R_R(INS_and, EA_1BYTE, tmpReg, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, tmpReg);
            break;
        }

        case NI_SSE2_CompareGreaterThanOrderedScalar:
        case NI_SSE2_CompareGreaterThanUnorderedScalar:
        {
            assert(baseType == TYP_DOUBLE);
            op2Reg          = op2->gtRegNum;
            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);

            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), op1Reg, op2Reg);
            emit->emitIns_R(INS_seta, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE2_CompareGreaterThanOrEqualOrderedScalar:
        case NI_SSE2_CompareGreaterThanOrEqualUnorderedScalar:
        {
            assert(baseType == TYP_DOUBLE);
            op2Reg          = op2->gtRegNum;
            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);

            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), op1Reg, op2Reg);
            emit->emitIns_R(INS_setae, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE2_CompareLessThanOrderedScalar:
        case NI_SSE2_CompareLessThanUnorderedScalar:
        {
            assert(baseType == TYP_DOUBLE);
            op2Reg          = op2->gtRegNum;
            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);

            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), op2Reg, op1Reg);
            emit->emitIns_R(INS_seta, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE2_CompareLessThanOrEqualOrderedScalar:
        case NI_SSE2_CompareLessThanOrEqualUnorderedScalar:
        {
            assert(baseType == TYP_DOUBLE);
            op2Reg          = op2->gtRegNum;
            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);

            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), op2Reg, op1Reg);
            emit->emitIns_R(INS_setae, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE2_CompareNotEqualOrderedScalar:
        case NI_SSE2_CompareNotEqualUnorderedScalar:
        {
            assert(baseType == TYP_DOUBLE);
            op2Reg             = op2->gtRegNum;
            instruction ins    = Compiler::insOfHWIntrinsic(intrinsicID, baseType);
            regNumber   tmpReg = node->GetSingleTempReg();

            // Ensure we aren't overwriting targetReg
            assert(tmpReg != targetReg);

            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), op1Reg, op2Reg);
            emit->emitIns_R(INS_setpe, EA_1BYTE, targetReg);
            emit->emitIns_R(INS_setne, EA_1BYTE, tmpReg);
            emit->emitIns_R_R(INS_or, EA_1BYTE, tmpReg, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, tmpReg);
            break;
        }

        case NI_SSE2_ConvertScalarToVector128Double:
        case NI_SSE2_ConvertScalarToVector128Single:
        {
            assert(baseType == TYP_INT || baseType == TYP_LONG || baseType == TYP_FLOAT || baseType == TYP_DOUBLE);
            assert(op1 != nullptr);
            assert(op2 != nullptr);
            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);
            genHWIntrinsic_R_R_RM(node, ins);
            break;
        }

        case NI_SSE2_ConvertScalarToVector128Int64:
        case NI_SSE2_ConvertScalarToVector128UInt64:
        {
            assert(baseType == TYP_LONG || baseType == TYP_ULONG);
            assert(op1 != nullptr);
            assert(op2 == nullptr);
            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);
            emit->emitIns_R_R(ins, emitTypeSize(baseType), targetReg, op1Reg);
            break;
        }

        case NI_SSE2_ConvertToDouble:
        {
            assert(op2 == nullptr);
            if (op1Reg != targetReg)
            {
                instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);
                emit->emitIns_R_R(ins, emitTypeSize(targetType), targetReg, op1Reg);
            }
            break;
        }

        case NI_SSE2_ConvertToInt32:
        case NI_SSE2_ConvertToInt64:
        case NI_SSE2_ConvertToUInt32:
        case NI_SSE2_ConvertToUInt64:
        {
            assert(op2 == nullptr);
            assert(baseType == TYP_DOUBLE || baseType == TYP_FLOAT || baseType == TYP_INT || baseType == TYP_UINT ||
                   baseType == TYP_LONG || baseType == TYP_ULONG);
            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);
            if (baseType == TYP_DOUBLE || baseType == TYP_FLOAT)
            {
                emit->emitIns_R_R(ins, emitTypeSize(targetType), targetReg, op1Reg);
            }
            else
            {
                emit->emitIns_R_R(ins, emitActualTypeSize(baseType), op1Reg, targetReg);
            }
            break;
        }

        case NI_SSE2_LoadFence:
        {
            assert(baseType == TYP_VOID);
            assert(op1 == nullptr);
            assert(op2 == nullptr);
            emit->emitIns(INS_lfence);
            break;
        }

        case NI_SSE2_MemoryFence:
        {
            assert(baseType == TYP_VOID);
            assert(op1 == nullptr);
            assert(op2 == nullptr);
            emit->emitIns(INS_mfence);
            break;
        }

        case NI_SSE2_MoveMask:
        {
            assert(op2 == nullptr);
            assert(baseType == TYP_BYTE || baseType == TYP_UBYTE || baseType == TYP_DOUBLE);

            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);
            emit->emitIns_R_R(ins, emitTypeSize(TYP_INT), targetReg, op1Reg);
            break;
        }

        case NI_SSE2_SetScalarVector128:
        {
            assert(baseType == TYP_DOUBLE);
            assert(op2 == nullptr);

            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType);
            if (op1Reg == targetReg)
            {
                regNumber tmpReg = node->GetSingleTempReg();

                // Ensure we aren't overwriting targetReg
                assert(tmpReg != targetReg);

                emit->emitIns_R_R(INS_movapd, emitTypeSize(TYP_SIMD16), tmpReg, op1Reg);
                op1Reg = tmpReg;
            }

            emit->emitIns_SIMD_R_R_R(INS_xorpd, emitTypeSize(TYP_SIMD16), targetReg, targetReg, targetReg);
            emit->emitIns_SIMD_R_R_R(ins, emitTypeSize(TYP_SIMD16), targetReg, targetReg, op1Reg);
            break;
        }

        case NI_SSE2_SetZeroVector128:
        {
            assert(baseType != TYP_FLOAT);
            assert(baseType >= TYP_BYTE && baseType <= TYP_DOUBLE);
            assert(op1 == nullptr);
            assert(op2 == nullptr);

            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);
            emit->emitIns_SIMD_R_R_R(ins, emitTypeSize(TYP_SIMD16), targetReg, targetReg, targetReg);
            break;
        }

        case NI_SSE2_StoreNonTemporal:
        {
            assert(baseType == TYP_INT || baseType == TYP_UINT || baseType == TYP_LONG || baseType == TYP_ULONG);
            assert(op1 != nullptr);
            assert(op2 != nullptr);

            op2Reg          = op2->gtRegNum;
            instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, baseType);
            emit->emitIns_AR_R(ins, emitTypeSize(baseType), op2Reg, op1Reg, 0);
            break;
        }

        default:
            unreached();
            break;
    }

    genProduceReg(node);
}

//------------------------------------------------------------------------
// genSSE41Intrinsic: Generates the code for an SSE4.1 hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genSSE41Intrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    GenTree*       op3         = nullptr;
    GenTree*       op4         = nullptr;
    regNumber      targetReg   = node->gtRegNum;
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->gtSIMDBaseType;

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
        case NI_SSE41_TestAllOnes:
        {
            regNumber tmpReg = node->GetSingleTempReg();
            assert(Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType) == INS_ptest);
            emit->emitIns_SIMD_R_R_R(INS_pcmpeqd, emitTypeSize(TYP_SIMD16), tmpReg, tmpReg, tmpReg);
            emit->emitIns_R_R(INS_xor, EA_4BYTE, targetReg, targetReg);
            emit->emitIns_R_R(INS_ptest, emitTypeSize(TYP_SIMD16), op1Reg, tmpReg);
            emit->emitIns_R(INS_setb, EA_1BYTE, targetReg);
            break;
        }

        case NI_SSE41_TestAllZeros:
        case NI_SSE41_TestZ:
        {
            assert(Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType) == INS_ptest);
            emit->emitIns_R_R(INS_xor, EA_4BYTE, targetReg, targetReg);
            emit->emitIns_R_R(INS_ptest, emitTypeSize(TYP_SIMD16), op1Reg, op2->gtRegNum);
            emit->emitIns_R(INS_sete, EA_1BYTE, targetReg);
            break;
        }

        case NI_SSE41_TestC:
        {
            assert(Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType) == INS_ptest);
            emit->emitIns_R_R(INS_xor, EA_4BYTE, targetReg, targetReg);
            emit->emitIns_R_R(INS_ptest, emitTypeSize(TYP_SIMD16), op1Reg, op2->gtRegNum);
            emit->emitIns_R(INS_setb, EA_1BYTE, targetReg);
            break;
        }

        case NI_SSE41_TestMixOnesZeros:
        case NI_SSE41_TestNotZAndNotC:
        {
            assert(Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType) == INS_ptest);
            emit->emitIns_R_R(INS_xor, EA_4BYTE, targetReg, targetReg);
            emit->emitIns_R_R(INS_ptest, emitTypeSize(TYP_SIMD16), op1Reg, op2->gtRegNum);
            emit->emitIns_R(INS_seta, EA_1BYTE, targetReg);
            break;
        }

        case NI_SSE41_Extract:
        {
            regNumber   tmpTargetReg = REG_NA;
            instruction ins          = Compiler::insOfHWIntrinsic(intrinsicID, baseType);
            if (baseType == TYP_FLOAT)
            {
                tmpTargetReg = node->ExtractTempReg();
            }
            auto emitSwCase = [&](unsigned i) {
                if (baseType == TYP_FLOAT)
                {
                    // extract instructions return to GP-registers, so it needs int size as the emitsize
                    emit->emitIns_SIMD_R_R_I(ins, emitTypeSize(TYP_INT), op1Reg, tmpTargetReg, (int)i);
                    emit->emitIns_R_R(INS_mov_i2xmm, EA_4BYTE, targetReg, tmpTargetReg);
                }
                else
                {
                    emit->emitIns_SIMD_R_R_I(ins, emitTypeSize(TYP_INT), targetReg, op1Reg, (int)i);
                }
            };

            if (op2->IsCnsIntOrI())
            {
                ssize_t ival = op2->AsIntCon()->IconValue();
                emitSwCase((unsigned)ival);
            }
            else
            {
                // We emit a fallback case for the scenario when the imm-op is not a constant. This should
                // normally happen when the intrinsic is called indirectly, such as via Reflection. However, it
                // can also occur if the consumer calls it directly and just doesn't pass a constant value.
                regNumber baseReg = node->ExtractTempReg();
                regNumber offsReg = node->GetSingleTempReg();
                genHWIntrinsicJumpTableFallback(intrinsicID, op2->gtRegNum, baseReg, offsReg, emitSwCase);
            }
            break;
        }

        default:
            unreached();
            break;
    }

    genProduceReg(node);
}

//------------------------------------------------------------------------
// genSSE42Intrinsic: Generates the code for an SSE4.2 hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
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
                assert(op2Reg != targetReg);
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

//------------------------------------------------------------------------
// genAvxOrAvx2Intrinsic: Generates the code for an AVX/AVX2 hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genAvxOrAvx2Intrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicID = node->gtHWIntrinsicId;
    var_types      baseType    = node->gtSIMDBaseType;
    emitAttr       attr        = EA_ATTR(node->gtSIMDSize);
    var_types      targetType  = node->TypeGet();
    instruction    ins         = Compiler::insOfHWIntrinsic(intrinsicID, baseType);
    int            numArgs     = Compiler::numArgsOfHWIntrinsic(node);
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    regNumber      op1Reg      = REG_NA;
    regNumber      op2Reg      = REG_NA;
    regNumber      targetReg   = node->gtRegNum;
    emitter*       emit        = getEmitter();

    if ((op1 != nullptr) && !op1->OperIsList())
    {
        genConsumeOperands(node);
    }

    switch (intrinsicID)
    {
        case NI_AVX_SetZeroVector256:
        {
            assert(op1 == nullptr);
            assert(op2 == nullptr);
            // SetZeroVector256 will generate pxor with integral base-typ, but pxor is a AVX2 instruction, so we
            // generate xorps on AVX machines.
            if (!compiler->compSupports(InstructionSet_AVX2) && varTypeIsIntegral(baseType))
            {
                emit->emitIns_SIMD_R_R_R(INS_xorps, attr, targetReg, targetReg, targetReg);
            }
            else
            {
                emit->emitIns_SIMD_R_R_R(ins, attr, targetReg, targetReg, targetReg);
            }
            break;
        }

        case NI_AVX_ExtendToVector256:
        {
            // ExtendToVector256 has zero-extend semantics in order to ensure it is deterministic
            // We always emit a move to the target register, even when op1Reg == targetReg, in order
            // to ensure that Bits MAXVL-1:128 are zeroed.

            assert(op2 == nullptr);
            regNumber op1Reg = op1->gtRegNum;
            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), targetReg, op1Reg);
            break;
        }

        case NI_AVX_GetLowerHalf:
        case NI_AVX_StaticCast:
        {
            assert(op2 == nullptr);
            regNumber op1Reg = op1->gtRegNum;

            if (op1Reg != targetReg)
            {
                instruction ins = Compiler::insOfHWIntrinsic(intrinsicID, node->gtSIMDBaseType);
                emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD32), targetReg, op1Reg);
            }
            break;
        }

        case NI_AVX_TestC:
        {
            emit->emitIns_R_R(INS_xor, EA_4BYTE, targetReg, targetReg);
            emit->emitIns_R_R(ins, attr, op1->gtRegNum, op2->gtRegNum);
            emit->emitIns_R(INS_setb, EA_1BYTE, targetReg);
            break;
        }

        case NI_AVX_TestNotZAndNotC:
        {
            emit->emitIns_R_R(INS_xor, EA_4BYTE, targetReg, targetReg);
            emit->emitIns_R_R(ins, attr, op1->gtRegNum, op2->gtRegNum);
            emit->emitIns_R(INS_seta, EA_1BYTE, targetReg);
            break;
        }

        case NI_AVX_TestZ:
        {
            emit->emitIns_R_R(INS_xor, EA_4BYTE, targetReg, targetReg);
            emit->emitIns_R_R(ins, attr, op1->gtRegNum, op2->gtRegNum);
            emit->emitIns_R(INS_sete, EA_1BYTE, targetReg);
            break;
        }

        case NI_AVX_ExtractVector128:
        case NI_AVX_InsertVector128:
        case NI_AVX2_ExtractVector128:
        case NI_AVX2_InsertVector128:
        {
            GenTree* lastOp = nullptr;
            if (numArgs == 2)
            {
                assert(intrinsicID == NI_AVX_ExtractVector128 || NI_AVX_ExtractVector128);
                op1Reg = op1->gtRegNum;
                op2Reg = op2->gtRegNum;
                lastOp = op2;
            }
            else
            {
                assert(numArgs == 3);
                assert(op1->OperIsList());
                assert(op1->gtGetOp2()->OperIsList());
                assert(op1->gtGetOp2()->gtGetOp2()->OperIsList());

                GenTreeArgList* argList = op1->AsArgList();
                op1                     = argList->Current();
                genConsumeRegs(op1);
                op1Reg = op1->gtRegNum;

                argList = argList->Rest();
                op2     = argList->Current();
                genConsumeRegs(op2);
                op2Reg = op2->gtRegNum;

                argList = argList->Rest();
                lastOp  = argList->Current();
                genConsumeRegs(lastOp);
            }

            regNumber op3Reg = lastOp->gtRegNum;

            auto emitSwCase = [&](unsigned i) {
                // TODO-XARCH-Bug the emitter cannot work with imm8 >= 128,
                // so clear the 8th bit that is not used by the instructions
                i &= 0x7FU;
                if (numArgs == 3)
                {
                    if (intrinsicID == NI_AVX_ExtractVector128 || intrinsicID == NI_AVX2_ExtractVector128)
                    {
                        emit->emitIns_R_AR_I(ins, attr, op2Reg, op1Reg, 0, (int)i);
                    }
                    else if (op2->TypeGet() == TYP_I_IMPL)
                    {
                        emit->emitIns_SIMD_R_R_AR_I(ins, attr, targetReg, op1Reg, op2Reg, (int)i);
                    }
                    else
                    {
                        assert(op2->TypeGet() == TYP_SIMD16);
                        emit->emitIns_SIMD_R_R_R_I(ins, attr, targetReg, op1Reg, op2Reg, (int)i);
                    }
                }
                else
                {
                    assert(numArgs == 2);
                    assert(intrinsicID == NI_AVX_ExtractVector128 || intrinsicID == NI_AVX2_ExtractVector128);
                    emit->emitIns_SIMD_R_R_I(ins, attr, targetReg, op1Reg, (int)i);
                }
            };

            if (lastOp->IsCnsIntOrI())
            {
                ssize_t ival = lastOp->AsIntCon()->IconValue();
                emitSwCase((unsigned)ival);
            }
            else
            {
                // We emit a fallback case for the scenario when the imm-op is not a constant. This should
                // normally happen when the intrinsic is called indirectly, such as via Reflection. However, it
                // can also occur if the consumer calls it directly and just doesn't pass a constant value.
                regNumber baseReg = node->ExtractTempReg();
                regNumber offsReg = node->GetSingleTempReg();
                genHWIntrinsicJumpTableFallback(intrinsicID, op3Reg, baseReg, offsReg, emitSwCase);
            }
            break;
        }

        default:
            unreached();
            break;
    }

    genProduceReg(node);
}

//------------------------------------------------------------------------
// genAESIntrinsic: Generates the code for an AES hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genAESIntrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement AES intrinsic code generation");
}

//------------------------------------------------------------------------
// genBMI1Intrinsic: Generates the code for a BMI1 hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genBMI1Intrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement BMI1 intrinsic code generation");
}

//------------------------------------------------------------------------
// genBMI2Intrinsic: Generates the code for a BMI2 hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genBMI2Intrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement BMI2 intrinsic code generation");
}

//------------------------------------------------------------------------
// genFMAIntrinsic: Generates the code for an FMA hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genFMAIntrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement FMA intrinsic code generation");
}

//------------------------------------------------------------------------
// genLZCNTIntrinsic: Generates the code for a LZCNT hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
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

//------------------------------------------------------------------------
// genPCLMULQDQIntrinsic: Generates the code for a PCLMULQDQ hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genPCLMULQDQIntrinsic(GenTreeHWIntrinsic* node)
{
    NYI("Implement PCLMULQDQ intrinsic code generation");
}

//------------------------------------------------------------------------
// genPOPCNTIntrinsic: Generates the code for a POPCNT hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
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
