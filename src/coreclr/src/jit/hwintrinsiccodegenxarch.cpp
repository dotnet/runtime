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
// assertIsContainableHWIntrinsicOp: Asserts that op is containable by node
//
// Arguments:
//    lowering - The lowering phase from the compiler
//    node     - The HWIntrinsic node that has the contained node
//    op       - The op that is contained
//
static void assertIsContainableHWIntrinsicOp(Lowering* lowering, GenTreeHWIntrinsic* node, GenTree* op)
{
#if DEBUG
    // The Lowering::IsContainableHWIntrinsicOp call is not quite right, since it follows pre-register allocation
    // logic. However, this check is still important due to the various containment rules that SIMD intrinsics follow.
    //
    // We use isContainable to track the special HWIntrinsic node containment rules (for things like LoadAligned and
    // LoadUnaligned) and we use the supportsRegOptional check to support general-purpose loads (both from stack
    // spillage
    // and for isUsedFromMemory contained nodes, in the case where the register allocator decided to not allocate a
    // register
    // in the first place).

    bool supportsRegOptional = false;
    bool isContainable       = lowering->IsContainableHWIntrinsicOp(node, op, &supportsRegOptional);
    assert(isContainable || supportsRegOptional);
#endif // DEBUG
}

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
    // TODO - make more categories to the table-driven framework
    // HW_Category_Helper and HW_Flag_MultiIns/HW_Flag_SpecialCodeGen usually need manual codegen
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
    InstructionSet      isa         = HWIntrinsicInfo::lookupIsa(intrinsicId);
    HWIntrinsicCategory category    = HWIntrinsicInfo::lookupCategory(intrinsicId);
    int                 ival        = HWIntrinsicInfo::lookupIval(intrinsicId);
    int                 numArgs     = HWIntrinsicInfo::lookupNumArgs(node);

    assert(HWIntrinsicInfo::RequiresCodegen(intrinsicId));

    if (genIsTableDrivenHWIntrinsic(intrinsicId, category))
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
        instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
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
                else if ((category == HW_Category_SIMDScalar) && HWIntrinsicInfo::CopiesUpperBits(intrinsicId))
                {
                    emit->emitIns_SIMD_R_R_R(ins, simdSize, targetReg, op1Reg, op1Reg);
                }
                else if ((ival != -1) && varTypeIsFloating(baseType))
                {
                    assert((ival >= 0) && (ival <= 127));
                    genHWIntrinsic_R_RM_I(node, ins, (int8_t)ival);
                }
                else
                {
                    genHWIntrinsic_R_RM(node, ins, simdSize);
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
                    assert((ival >= 0) && (ival <= 127));
                    genHWIntrinsic_R_R_RM_I(node, ins, ival);
                }
                else if (category == HW_Category_MemoryLoad)
                {
                    if (intrinsicId == NI_AVX_MaskLoad)
                    {
                        emit->emitIns_SIMD_R_R_AR(ins, simdSize, targetReg, op2Reg, op1Reg);
                    }
                    else
                    {
                        emit->emitIns_SIMD_R_R_AR(ins, simdSize, targetReg, op1Reg, op2Reg);
                    }
                }
                else if (HWIntrinsicInfo::isImmOp(intrinsicId, op2))
                {
                    assert(ival == -1);

                    if (intrinsicId == NI_SSE2_Extract)
                    {
                        // extract instructions return to GP-registers, so it needs int size as the emitsize
                        simdSize = emitTypeSize(TYP_INT);
                    }

                    auto emitSwCase = [&](int8_t i) { genHWIntrinsic_R_RM_I(node, ins, i); };

                    if (op2->IsCnsIntOrI())
                    {
                        ssize_t ival = op2->AsIntCon()->IconValue();
                        assert((ival >= 0) && (ival <= 255));
                        emitSwCase((int8_t)ival);
                    }
                    else
                    {
                        // We emit a fallback case for the scenario when the imm-op is not a constant. This should
                        // normally happen when the intrinsic is called indirectly, such as via Reflection. However, it
                        // can also occur if the consumer calls it directly and just doesn't pass a constant value.
                        regNumber baseReg = node->ExtractTempReg();
                        regNumber offsReg = node->GetSingleTempReg();
                        genHWIntrinsicJumpTableFallback(intrinsicId, op2Reg, baseReg, offsReg, emitSwCase);
                    }
                }
                else
                {
                    genHWIntrinsic_R_R_RM(node, ins, EA_ATTR(node->gtSIMDSize));
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

                if (HWIntrinsicInfo::isImmOp(intrinsicId, op3))
                {
                    assert(ival == -1);

                    auto emitSwCase = [&](int8_t i) { genHWIntrinsic_R_R_RM_I(node, ins, i); };

                    if (op3->IsCnsIntOrI())
                    {
                        ssize_t ival = op3->AsIntCon()->IconValue();
                        assert((ival >= 0) && (ival <= 255));
                        emitSwCase((int8_t)ival);
                    }
                    else
                    {
                        // We emit a fallback case for the scenario when the imm-op is not a constant. This should
                        // normally happen when the intrinsic is called indirectly, such as via Reflection. However, it
                        // can also occur if the consumer calls it directly and just doesn't pass a constant value.
                        regNumber baseReg = node->ExtractTempReg();
                        regNumber offsReg = node->GetSingleTempReg();
                        genHWIntrinsicJumpTableFallback(intrinsicId, op3Reg, baseReg, offsReg, emitSwCase);
                    }
                }
                else if (category == HW_Category_MemoryStore)
                {
                    assert(intrinsicId == NI_SSE2_MaskMove);
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
                    switch (intrinsicId)
                    {
                        case NI_SSE41_BlendVariable:
                        case NI_AVX_BlendVariable:
                        case NI_AVX2_BlendVariable:
                        {
                            genHWIntrinsic_R_R_RM_R(node, ins);
                            break;
                        }

                        default:
                        {
                            unreached();
                            break;
                        };
                    }
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
// genHWIntrinsic_R_RM: Generates the code for a hardware intrinsic node that takes a
//                      register/memory operand and that returns a value in register
//
// Arguments:
//    node - The hardware intrinsic node
//    ins  - The instruction being generated
//    attr - The emit attribute for the instruciton being generated
//
void CodeGen::genHWIntrinsic_R_RM(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr)
{
    var_types targetType = node->TypeGet();
    regNumber targetReg  = node->gtRegNum;
    GenTree*  op1        = node->gtGetOp1();
    GenTree*  op2        = node->gtGetOp2();
    emitter*  emit       = getEmitter();

    if (op2 != nullptr)
    {
        // The Compare*OrderedScalar and Compare*UnorderedScalar intrinsics come down this
        // code path. They are all MultiIns, as the return value comes from the flags and
        // we have two operands instead.

        assert(HWIntrinsicInfo::GeneratesMultipleIns(node->gtHWIntrinsicId));
        assert(targetReg != REG_NA);

        targetReg = op1->gtRegNum;
        op1       = op2;
        op2       = nullptr;
    }
    else
    {
        assert(!node->OperIsCommutative());
    }

    assert(targetReg != REG_NA);
    assert(op2 == nullptr);

    if (op1->isContained() || op1->isUsedFromSpillTemp())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->gtHWIntrinsicId));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, op1);

        TempDsc* tmpDsc = nullptr;
        unsigned varNum = BAD_VAR_NUM;
        unsigned offset = (unsigned)-1;

        if (op1->isUsedFromSpillTemp())
        {
            assert(op1->IsRegOptional());

            tmpDsc = getSpillTempDsc(op1);
            varNum = tmpDsc->tdTempNum();
            offset = 0;

            regSet.tmpRlsTemp(tmpDsc);
        }
        else if (op1->OperIsHWIntrinsic())
        {
            emit->emitIns_R_AR(ins, attr, targetReg, op1->gtGetOp1()->gtRegNum, 0);
            return;
        }
        else if (op1->isIndir())
        {
            GenTreeIndir* memIndir = op1->AsIndir();
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
                    emit->emitIns_R_C(ins, attr, targetReg, memBase->gtClsVar.gtClsVarHnd, 0);
                    return;
                }

                default:
                {
                    emit->emitIns_R_A(ins, attr, targetReg, memIndir);
                    return;
                }
            }
        }
        else
        {
            switch (op1->OperGet())
            {
                case GT_LCL_FLD:
                {
                    GenTreeLclFld* lclField = op1->AsLclFld();

                    varNum = lclField->GetLclNum();
                    offset = lclField->gtLclFld.gtLclOffs;
                    break;
                }

                case GT_LCL_VAR:
                {
                    assert(op1->IsRegOptional() || !compiler->lvaTable[op1->gtLclVar.gtLclNum].lvIsRegCandidate());
                    varNum = op1->AsLclVar()->GetLclNum();
                    offset = 0;
                    break;
                }

                default:
                {
                    unreached();
                    break;
                }
            }
        }

        // Ensure we got a good varNum and offset.
        // We also need to check for `tmpDsc != nullptr` since spill temp numbers
        // are negative and start with -1, which also happens to be BAD_VAR_NUM.
        assert((varNum != BAD_VAR_NUM) || (tmpDsc != nullptr));
        assert(offset != (unsigned)-1);

        emit->emitIns_R_S(ins, attr, targetReg, varNum, offset);
    }
    else
    {
        regNumber op1Reg = op1->gtRegNum;
        emit->emitIns_R_R(ins, attr, targetReg, op1Reg);
    }
}

//------------------------------------------------------------------------
// genHWIntrinsic_R_RM_I: Generates the code for a hardware intrinsic node that takes a register/memory operand,
//                        an immediate operand, and that returns a value in register
//
// Arguments:
//    node - The hardware intrinsic node
//    ins  - The instruction being generated
//    ival - The immediate value
//
void CodeGen::genHWIntrinsic_R_RM_I(GenTreeHWIntrinsic* node, instruction ins, int8_t ival)
{
    var_types targetType = node->TypeGet();
    regNumber targetReg  = node->gtRegNum;
    GenTree*  op1        = node->gtGetOp1();
    emitAttr  simdSize   = EA_ATTR(node->gtSIMDSize);
    emitter*  emit       = getEmitter();

    // TODO-XArch-CQ: Commutative operations can have op1 be contained
    // TODO-XArch-CQ: Non-VEX encoded instructions can have both ops contained

    assert(targetReg != REG_NA);
    assert(!node->OperIsCommutative()); // One operand intrinsics cannot be commutative

    if (op1->isContained() || op1->isUsedFromSpillTemp())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->gtHWIntrinsicId));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, op1);

        TempDsc* tmpDsc = nullptr;
        unsigned varNum = BAD_VAR_NUM;
        unsigned offset = (unsigned)-1;

        if (op1->isUsedFromSpillTemp())
        {
            assert(op1->IsRegOptional());

            tmpDsc = getSpillTempDsc(op1);
            varNum = tmpDsc->tdTempNum();
            offset = 0;

            regSet.tmpRlsTemp(tmpDsc);
        }
        else if (op1->OperIsHWIntrinsic())
        {
            emit->emitIns_R_AR_I(ins, simdSize, targetReg, op1->gtGetOp1()->gtRegNum, 0, ival);
            return;
        }
        else if (op1->isIndir())
        {
            GenTreeIndir* memIndir = op1->AsIndir();
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
                    emit->emitIns_R_C_I(ins, simdSize, targetReg, memBase->gtClsVar.gtClsVarHnd, 0, ival);
                    return;
                }

                default:
                {
                    emit->emitIns_R_A_I(ins, simdSize, targetReg, memIndir, ival);
                    return;
                }
            }
        }
        else
        {
            switch (op1->OperGet())
            {
                case GT_LCL_FLD:
                {
                    GenTreeLclFld* lclField = op1->AsLclFld();

                    varNum = lclField->GetLclNum();
                    offset = lclField->gtLclFld.gtLclOffs;
                    break;
                }

                case GT_LCL_VAR:
                {
                    assert(op1->IsRegOptional() || !compiler->lvaTable[op1->gtLclVar.gtLclNum].lvIsRegCandidate());
                    varNum = op1->AsLclVar()->GetLclNum();
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

        emit->emitIns_R_S_I(ins, simdSize, targetReg, varNum, offset, ival);
    }
    else
    {
        regNumber op1Reg = op1->gtRegNum;
        emit->emitIns_SIMD_R_R_I(ins, simdSize, targetReg, op1Reg, ival);
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
void CodeGen::genHWIntrinsic_R_R_RM(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr)
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
        assert(HWIntrinsicInfo::SupportsContainment(node->gtHWIntrinsicId));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, op2);

        TempDsc* tmpDsc = nullptr;
        unsigned varNum = BAD_VAR_NUM;
        unsigned offset = (unsigned)-1;

        if (op2->isUsedFromSpillTemp())
        {
            assert(op2->IsRegOptional());

            tmpDsc = getSpillTempDsc(op2);
            varNum = tmpDsc->tdTempNum();
            offset = 0;

            regSet.tmpRlsTemp(tmpDsc);
        }
        else if (op2->OperIsHWIntrinsic())
        {
            emit->emitIns_SIMD_R_R_AR(ins, attr, targetReg, op1Reg, op2->gtGetOp1()->gtRegNum);
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
                    emit->emitIns_SIMD_R_R_C(ins, attr, targetReg, op1Reg, memBase->gtClsVar.gtClsVarHnd, 0);
                    return;
                }

                default:
                {
                    emit->emitIns_SIMD_R_R_A(ins, attr, targetReg, op1Reg, memIndir);
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

        emit->emitIns_SIMD_R_R_S(ins, attr, targetReg, op1Reg, varNum, offset);
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

        emit->emitIns_SIMD_R_R_R(ins, attr, targetReg, op1Reg, op2Reg);
    }
}

//------------------------------------------------------------------------
// genHWIntrinsic_R_R_RM_I: Generates the code for a hardware intrinsic node that takes a register operand, a
//                        register/memory operand, an immediate operand, and that returns a value in register
//
// Arguments:
//    node - The hardware intrinsic node
//    ins  - The instruction being generated
//    ival - The immediate value
//
void CodeGen::genHWIntrinsic_R_R_RM_I(GenTreeHWIntrinsic* node, instruction ins, int8_t ival)
{
    var_types targetType = node->TypeGet();
    regNumber targetReg  = node->gtRegNum;
    GenTree*  op1        = node->gtGetOp1();
    GenTree*  op2        = node->gtGetOp2();
    emitAttr  simdSize   = EA_ATTR(node->gtSIMDSize);
    emitter*  emit       = getEmitter();

    // TODO-XArch-CQ: Commutative operations can have op1 be contained
    // TODO-XArch-CQ: Non-VEX encoded instructions can have both ops contained

    if (op1->OperIsList())
    {
        assert(op2 == nullptr);

        GenTreeArgList* argList = op1->AsArgList();

        op1     = argList->Current();
        argList = argList->Rest();

        op2     = argList->Current();
        argList = argList->Rest();

        assert(argList->Current() != nullptr);
        assert(argList->Rest() == nullptr);
    }

    regNumber op1Reg = op1->gtRegNum;

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);

    if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->gtHWIntrinsicId));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, op2);

        TempDsc* tmpDsc = nullptr;
        unsigned varNum = BAD_VAR_NUM;
        unsigned offset = (unsigned)-1;

        if (op2->isUsedFromSpillTemp())
        {
            assert(op2->IsRegOptional());

            tmpDsc = getSpillTempDsc(op2);
            varNum = tmpDsc->tdTempNum();
            offset = 0;

            regSet.tmpRlsTemp(tmpDsc);
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

//------------------------------------------------------------------------
// genHWIntrinsic_R_R_RM_R: Generates the code for a hardware intrinsic node that takes a register operand, a
//                          register/memory operand, another register operand, and that returns a value in register
//
// Arguments:
//    node - The hardware intrinsic node
//    ins  - The instruction being generated
//
void CodeGen::genHWIntrinsic_R_R_RM_R(GenTreeHWIntrinsic* node, instruction ins)
{
    var_types targetType = node->TypeGet();
    regNumber targetReg  = node->gtRegNum;
    GenTree*  op1        = node->gtGetOp1();
    GenTree*  op2        = node->gtGetOp2();
    GenTree*  op3        = nullptr;
    emitAttr  simdSize   = EA_ATTR(node->gtSIMDSize);
    emitter*  emit       = getEmitter();

    assert(op1->OperIsList());
    assert(op2 == nullptr);

    GenTreeArgList* argList = op1->AsArgList();

    op1     = argList->Current();
    argList = argList->Rest();

    op2     = argList->Current();
    argList = argList->Rest();

    op3 = argList->Current();
    assert(argList->Rest() == nullptr);

    regNumber op1Reg = op1->gtRegNum;
    regNumber op3Reg = op3->gtRegNum;

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);
    assert(op3Reg != REG_NA);

    if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->gtHWIntrinsicId));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, op2);

        TempDsc* tmpDsc = nullptr;
        unsigned varNum = BAD_VAR_NUM;
        unsigned offset = (unsigned)-1;

        if (op2->isUsedFromSpillTemp())
        {
            assert(op2->IsRegOptional());

            // TODO-XArch-Cleanup: The getSpillTempDsc...tempRlsTemp code is a fairly common
            //                     pattern. It could probably be extracted to its own method.
            tmpDsc = getSpillTempDsc(op2);
            varNum = tmpDsc->tdTempNum();
            offset = 0;

            regSet.tmpRlsTemp(tmpDsc);
        }
        else if (op2->OperIsHWIntrinsic())
        {
            emit->emitIns_SIMD_R_R_AR_R(ins, simdSize, targetReg, op1Reg, op3Reg, op2->gtGetOp1()->gtRegNum);
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
                    emit->emitIns_SIMD_R_R_C_R(ins, simdSize, targetReg, op1Reg, op3Reg, memBase->gtClsVar.gtClsVarHnd,
                                               0);
                    return;
                }

                default:
                {
                    emit->emitIns_SIMD_R_R_A_R(ins, simdSize, targetReg, op1Reg, op3Reg, memIndir);
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

        emit->emitIns_SIMD_R_R_S_R(ins, simdSize, targetReg, op1Reg, op3Reg, varNum, offset);
    }
    else
    {
        emit->emitIns_SIMD_R_R_R_R(ins, simdSize, targetReg, op1Reg, op2->gtRegNum, op3Reg);
    }
}

//------------------------------------------------------------------------
// genHWIntrinsic_R_R_R_RM: Generates the code for a hardware intrinsic node that takes two register operands,
//                          a register/memory operand, and that returns a value in register
//
// Arguments:
//    ins       - The instruction being generated
//    attr      - The emit attribute
//    targetReg - The target register
//    op1Reg    - The register of the first operand
//    op2Reg    - The register of the second operand
//    op3       - The third operand
//
void CodeGen::genHWIntrinsic_R_R_R_RM(
    instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, regNumber op2Reg, GenTree* op3)
{
    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);
    assert(op2Reg != REG_NA);

    emitter* emit = getEmitter();

    if (op3->isContained() || op3->isUsedFromSpillTemp())
    {
        TempDsc* tmpDsc = nullptr;
        unsigned varNum = BAD_VAR_NUM;
        unsigned offset = (unsigned)-1;

        if (op3->isUsedFromSpillTemp())
        {
            assert(op3->IsRegOptional());

            // TODO-XArch-Cleanup: The getSpillTempDsc...tempRlsTemp code is a fairly common
            //                     pattern. It could probably be extracted to its own method.
            tmpDsc = getSpillTempDsc(op3);
            varNum = tmpDsc->tdTempNum();
            offset = 0;

            regSet.tmpRlsTemp(tmpDsc);
        }
        else if (op3->OperIsHWIntrinsic())
        {
            emit->emitIns_SIMD_R_R_R_AR(ins, attr, targetReg, op1Reg, op2Reg, op3->gtGetOp1()->gtRegNum);
            return;
        }
        else if (op3->isIndir())
        {
            GenTreeIndir* memIndir = op3->AsIndir();
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
                    emit->emitIns_SIMD_R_R_R_C(ins, attr, targetReg, op1Reg, op2Reg, memBase->gtClsVar.gtClsVarHnd, 0);
                    return;
                }

                default:
                {
                    emit->emitIns_SIMD_R_R_R_A(ins, attr, targetReg, op1Reg, op2Reg, memIndir);
                    return;
                }
            }
        }
        else
        {
            switch (op3->OperGet())
            {
                case GT_LCL_FLD:
                {
                    GenTreeLclFld* lclField = op3->AsLclFld();

                    varNum = lclField->GetLclNum();
                    offset = lclField->gtLclFld.gtLclOffs;
                    break;
                }

                case GT_LCL_VAR:
                {
                    assert(op3->IsRegOptional() || !compiler->lvaTable[op3->gtLclVar.gtLclNum].lvIsRegCandidate());
                    varNum = op3->AsLclVar()->GetLclNum();
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

        emit->emitIns_SIMD_R_R_R_S(ins, attr, targetReg, op1Reg, op2Reg, varNum, offset);
    }
    else
    {
        emit->emitIns_SIMD_R_R_R_R(ins, attr, targetReg, op1Reg, op2Reg, op3->gtRegNum);
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

    const unsigned maxByte = (unsigned)HWIntrinsicInfo::lookupImmUpperBound(intrinsic) + 1;
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
        emitSwCase((int8_t)i);
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
    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;
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

    switch (intrinsicId)
    {
        case NI_SSE_CompareEqualOrderedScalar:
        case NI_SSE_CompareEqualUnorderedScalar:
        {
            assert(baseType == TYP_FLOAT);
            regNumber   tmpReg = node->GetSingleTempReg();
            instruction ins    = HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType);

            // Ensure we aren't overwriting targetReg
            assert(tmpReg != targetReg);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(TYP_SIMD16));
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
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(TYP_SIMD16));
            emit->emitIns_R(INS_seta, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE_CompareGreaterThanOrEqualOrderedScalar:
        case NI_SSE_CompareGreaterThanOrEqualUnorderedScalar:
        {
            assert(baseType == TYP_FLOAT);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(TYP_SIMD16));
            emit->emitIns_R(INS_setae, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE_CompareLessThanOrderedScalar:
        case NI_SSE_CompareLessThanUnorderedScalar:
        {
            assert(baseType == TYP_FLOAT);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(TYP_SIMD16));
            emit->emitIns_R(INS_seta, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE_CompareLessThanOrEqualOrderedScalar:
        case NI_SSE_CompareLessThanOrEqualUnorderedScalar:
        {
            assert(baseType == TYP_FLOAT);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(TYP_SIMD16));
            emit->emitIns_R(INS_setae, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE_CompareNotEqualOrderedScalar:
        case NI_SSE_CompareNotEqualUnorderedScalar:
        {
            assert(baseType == TYP_FLOAT);
            regNumber   tmpReg = node->GetSingleTempReg();
            instruction ins    = HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType);

            // Ensure we aren't overwriting targetReg
            assert(tmpReg != targetReg);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(TYP_SIMD16));
            emit->emitIns_R(INS_setpe, EA_1BYTE, targetReg);
            emit->emitIns_R(INS_setne, EA_1BYTE, tmpReg);
            emit->emitIns_R_R(INS_or, EA_1BYTE, tmpReg, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, tmpReg);
            break;
        }

        case NI_SSE_ConvertToSingle:
        {
            assert(op2 == nullptr);
            if (op1Reg != targetReg)
            {
                instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType);
                emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), targetReg, op1Reg);
            }
            break;
        }

        case NI_SSE_MoveMask:
        {
            assert(baseType == TYP_FLOAT);
            assert(op2 == nullptr);

            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType);
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

            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType);
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
    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    regNumber      targetReg   = node->gtRegNum;
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->gtSIMDBaseType;
    regNumber      op1Reg      = REG_NA;
    regNumber      op2Reg      = REG_NA;
    emitter*       emit        = getEmitter();

    if ((op1 != nullptr) && !op1->OperIsList())
    {
        op1Reg = op1->gtRegNum;
        genConsumeOperands(node);
    }

    switch (intrinsicId)
    {
        // All integer overloads are handled by table codegen
        case NI_SSE2_CompareLessThan:
        {
            assert(op1 != nullptr);
            assert(op2 != nullptr);

            assert(baseType == TYP_DOUBLE);

            int ival = HWIntrinsicInfo::lookupIval(intrinsicId);
            assert((ival >= 0) && (ival <= 127));

            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            op2Reg          = op2->gtRegNum;
            emit->emitIns_SIMD_R_R_R_I(ins, emitTypeSize(TYP_SIMD16), targetReg, op1Reg, op2Reg, ival);

            break;
        }

        case NI_SSE2_CompareEqualOrderedScalar:
        case NI_SSE2_CompareEqualUnorderedScalar:
        {
            assert(baseType == TYP_DOUBLE);
            regNumber   tmpReg = node->GetSingleTempReg();
            instruction ins    = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);

            // Ensure we aren't overwriting targetReg
            assert(tmpReg != targetReg);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(TYP_SIMD16));
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
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(TYP_SIMD16));
            emit->emitIns_R(INS_seta, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE2_CompareGreaterThanOrEqualOrderedScalar:
        case NI_SSE2_CompareGreaterThanOrEqualUnorderedScalar:
        {
            assert(baseType == TYP_DOUBLE);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(TYP_SIMD16));
            emit->emitIns_R(INS_setae, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE2_CompareLessThanOrderedScalar:
        case NI_SSE2_CompareLessThanUnorderedScalar:
        {
            assert(baseType == TYP_DOUBLE);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(TYP_SIMD16));
            emit->emitIns_R(INS_seta, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE2_CompareLessThanOrEqualOrderedScalar:
        case NI_SSE2_CompareLessThanOrEqualUnorderedScalar:
        {
            assert(baseType == TYP_DOUBLE);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(TYP_SIMD16));
            emit->emitIns_R(INS_setae, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE2_CompareNotEqualOrderedScalar:
        case NI_SSE2_CompareNotEqualUnorderedScalar:
        {
            assert(baseType == TYP_DOUBLE);
            instruction ins    = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            regNumber   tmpReg = node->GetSingleTempReg();

            // Ensure we aren't overwriting targetReg
            assert(tmpReg != targetReg);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(TYP_SIMD16));
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
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            genHWIntrinsic_R_R_RM(node, ins, EA_ATTR(node->gtSIMDSize));
            break;
        }

        case NI_SSE2_ConvertScalarToVector128Int64:
        case NI_SSE2_ConvertScalarToVector128UInt64:
        {
            assert(baseType == TYP_LONG || baseType == TYP_ULONG);
            assert(op1 != nullptr);
            assert(op2 == nullptr);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            genHWIntrinsic_R_RM(node, ins, emitTypeSize(baseType));
            break;
        }

        case NI_SSE2_ConvertToDouble:
        {
            assert(op2 == nullptr);
            if (op1Reg != targetReg)
            {
                instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
                emit->emitIns_R_R(ins, emitTypeSize(targetType), targetReg, op1Reg);
            }
            break;
        }

        case NI_SSE2_ConvertToInt32:
        case NI_SSE2_ConvertToInt32WithTruncation:
        case NI_SSE2_ConvertToInt64:
        case NI_SSE2_ConvertToUInt32:
        case NI_SSE2_ConvertToUInt64:
        {
            assert(op2 == nullptr);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);

            if (varTypeIsIntegral(baseType))
            {
                assert(baseType == TYP_INT || baseType == TYP_UINT || baseType == TYP_LONG || baseType == TYP_ULONG);
                emit->emitIns_R_R(ins, emitActualTypeSize(baseType), op1Reg, targetReg);
            }
            else
            {
                assert(baseType == TYP_DOUBLE || baseType == TYP_FLOAT);
                genHWIntrinsic_R_RM(node, ins, emitTypeSize(targetType));
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

            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            emit->emitIns_R_R(ins, emitTypeSize(TYP_INT), targetReg, op1Reg);
            break;
        }

        case NI_SSE2_SetScalarVector128:
        {
            assert(baseType == TYP_DOUBLE);
            assert(op2 == nullptr);

            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType);
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
            assert(baseType >= TYP_BYTE && baseType <= TYP_DOUBLE);
            assert(op1 == nullptr);
            assert(op2 == nullptr);

            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            emit->emitIns_SIMD_R_R_R(ins, emitTypeSize(TYP_SIMD16), targetReg, targetReg, targetReg);
            break;
        }

        case NI_SSE2_StoreNonTemporal:
        {
            assert(baseType == TYP_INT || baseType == TYP_UINT || baseType == TYP_LONG || baseType == TYP_ULONG);
            assert(op1 != nullptr);
            assert(op2 != nullptr);

            op2Reg          = op2->gtRegNum;
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
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
    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;
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

    switch (intrinsicId)
    {
        case NI_SSE41_TestAllOnes:
        {
            regNumber tmpReg = node->GetSingleTempReg();
            assert(HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType) == INS_ptest);
            emit->emitIns_SIMD_R_R_R(INS_pcmpeqd, emitTypeSize(TYP_SIMD16), tmpReg, tmpReg, tmpReg);
            emit->emitIns_R_R(INS_ptest, emitTypeSize(TYP_SIMD16), op1Reg, tmpReg);
            emit->emitIns_R(INS_setb, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE41_TestAllZeros:
        case NI_SSE41_TestZ:
        {
            assert(HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType) == INS_ptest);
            genHWIntrinsic_R_RM(node, INS_ptest, emitTypeSize(TYP_SIMD16));
            emit->emitIns_R(INS_sete, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE41_TestC:
        {
            assert(HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType) == INS_ptest);
            genHWIntrinsic_R_RM(node, INS_ptest, emitTypeSize(TYP_SIMD16));
            emit->emitIns_R(INS_setb, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE41_TestMixOnesZeros:
        case NI_SSE41_TestNotZAndNotC:
        {
            assert(HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType) == INS_ptest);
            genHWIntrinsic_R_RM(node, INS_ptest, emitTypeSize(TYP_SIMD16));
            emit->emitIns_R(INS_seta, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_SSE41_Extract:
        {
            regNumber   tmpTargetReg = REG_NA;
            instruction ins          = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            if (baseType == TYP_FLOAT)
            {
                tmpTargetReg = node->ExtractTempReg();
            }

            auto emitSwCase = [&](int8_t i) {
                if (baseType == TYP_FLOAT)
                {
                    // extract instructions return to GP-registers, so it needs int size as the emitsize
                    emit->emitIns_SIMD_R_R_I(ins, emitTypeSize(TYP_INT), tmpTargetReg, op1Reg, i);
                    emit->emitIns_R_R(INS_mov_i2xmm, EA_4BYTE, targetReg, tmpTargetReg);
                }
                else
                {
                    emit->emitIns_SIMD_R_R_I(ins, emitTypeSize(TYP_INT), targetReg, op1Reg, i);
                }
            };

            if (op2->IsCnsIntOrI())
            {
                ssize_t ival = op2->AsIntCon()->IconValue();
                assert((ival >= 0) && (ival <= 255));
                emitSwCase((int8_t)ival);
            }
            else
            {
                // We emit a fallback case for the scenario when the imm-op is not a constant. This should
                // normally happen when the intrinsic is called indirectly, such as via Reflection. However, it
                // can also occur if the consumer calls it directly and just doesn't pass a constant value.
                regNumber baseReg = node->ExtractTempReg();
                regNumber offsReg = node->GetSingleTempReg();
                genHWIntrinsicJumpTableFallback(intrinsicId, op2->gtRegNum, baseReg, offsReg, emitSwCase);
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
    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;
    regNumber      targetReg   = node->gtRegNum;
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    var_types      baseType    = node->gtSIMDBaseType;
    var_types      targetType  = node->TypeGet();
    emitter*       emit        = getEmitter();

    regNumber op1Reg = op1->gtRegNum;
    genConsumeOperands(node);

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);
    assert(op2 != nullptr);
    assert(!node->OperIsCommutative());

    switch (intrinsicId)
    {
        case NI_SSE42_Crc32:
        {
            if (op1Reg != targetReg)
            {
                assert(op2->gtRegNum != targetReg);
                emit->emitIns_R_R(INS_mov, emitTypeSize(targetType), targetReg, op1Reg);
            }

            // This makes the genHWIntrinsic_R_RM code much simpler, as we don't need an
            // overload that explicitly takes the operands.
            node->gtOp1 = op2;
            node->gtOp2 = nullptr;

            if ((baseType == TYP_UBYTE) || (baseType == TYP_USHORT)) // baseType is the type of the second argument
            {
                assert(targetType == TYP_INT);
                genHWIntrinsic_R_RM(node, INS_crc32, emitTypeSize(baseType));
            }
            else
            {
                assert(op1->TypeGet() == op2->TypeGet());
                assert((targetType == TYP_INT) || (targetType == TYP_LONG));
                genHWIntrinsic_R_RM(node, INS_crc32, emitTypeSize(targetType));
            }

            break;
        }

        default:
        {
            unreached();
            break;
        }
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
    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;
    var_types      baseType    = node->gtSIMDBaseType;
    emitAttr       attr        = EA_ATTR(node->gtSIMDSize);
    var_types      targetType  = node->TypeGet();
    instruction    ins         = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
    int            numArgs     = HWIntrinsicInfo::lookupNumArgs(node);
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    regNumber      op1Reg      = REG_NA;
    regNumber      op2Reg      = REG_NA;
    regNumber      targetReg   = node->gtRegNum;
    emitter*       emit        = getEmitter();

    if ((op1 != nullptr) && !op1->OperIsList())
    {
        op1Reg = op1->gtRegNum;
        genConsumeOperands(node);
    }

    switch (intrinsicId)
    {
        case NI_AVX2_ConvertToDouble:
        {
            assert(op2 == nullptr);
            if (op1Reg != targetReg)
            {
                instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
                emit->emitIns_R_R(ins, emitTypeSize(targetType), targetReg, op1Reg);
            }
            break;
        }

        case NI_AVX2_ConvertToInt32:
        case NI_AVX2_ConvertToUInt32:
        {
            assert(op2 == nullptr);
            assert((baseType == TYP_INT) || (baseType == TYP_UINT));
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            emit->emitIns_R_R(ins, emitActualTypeSize(baseType), op1Reg, targetReg);
            break;
        }

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

        case NI_AVX_SetAllVector256:
        {
            assert(op1 != nullptr);
            assert(op2 == nullptr);
            if (varTypeIsIntegral(baseType))
            {
                // If the argument is a integer, it needs to be moved into a XMM register
                regNumber tmpXMM = node->ExtractTempReg();
                emit->emitIns_R_R(INS_mov_i2xmm, emitActualTypeSize(baseType), tmpXMM, op1Reg);
                op1Reg = tmpXMM;
            }

            if (compiler->compSupports(InstructionSet_AVX2))
            {
                // generate broadcast instructions if AVX2 is available
                emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD32), targetReg, op1Reg);
            }
            else
            {
                // duplicate the scalar argument to XMM register
                switch (baseType)
                {
                    case TYP_FLOAT:
                        emit->emitIns_SIMD_R_R_I(INS_vpermilps, emitTypeSize(TYP_SIMD16), op1Reg, op1Reg, 0);
                        break;
                    case TYP_DOUBLE:
                        emit->emitIns_R_R(INS_movddup, emitTypeSize(TYP_SIMD16), op1Reg, op1Reg);
                        break;
                    case TYP_BYTE:
                    case TYP_UBYTE:
                    {
                        regNumber tmpZeroReg = node->GetSingleTempReg();
                        emit->emitIns_R_R(INS_pxor, emitTypeSize(TYP_SIMD16), tmpZeroReg, tmpZeroReg);
                        emit->emitIns_SIMD_R_R_R(INS_pshufb, emitTypeSize(TYP_SIMD16), op1Reg, op1Reg, tmpZeroReg);
                        break;
                    }
                    case TYP_SHORT:
                    case TYP_USHORT:
                        emit->emitIns_SIMD_R_R_I(INS_pshuflw, emitTypeSize(TYP_SIMD16), op1Reg, op1Reg, 0);
                        emit->emitIns_SIMD_R_R_I(INS_pshufd, emitTypeSize(TYP_SIMD16), op1Reg, op1Reg, 80);
                        break;
                    case TYP_INT:
                    case TYP_UINT:
                        emit->emitIns_SIMD_R_R_I(INS_pshufd, emitTypeSize(TYP_SIMD16), op1Reg, op1Reg, 0);
                        break;
                    case TYP_LONG:
                    case TYP_ULONG:
                        emit->emitIns_SIMD_R_R_I(INS_pshufd, emitTypeSize(TYP_SIMD16), op1Reg, op1Reg, 68);
                        break;

                    default:
                        unreached();
                        break;
                }
                // duplicate the XMM register to YMM register
                emit->emitIns_SIMD_R_R_R_I(INS_vinsertf128, emitTypeSize(TYP_SIMD32), targetReg, op1Reg, op1Reg, 1);
            }
            break;
        }

        case NI_AVX_ExtendToVector256:
        {
            // ExtendToVector256 has zero-extend semantics in order to ensure it is deterministic
            // We always emit a move to the target register, even when op1Reg == targetReg, in order
            // to ensure that Bits MAXVL-1:128 are zeroed.

            assert(op2 == nullptr);
            emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD16), targetReg, op1Reg);
            break;
        }

        case NI_AVX_GetLowerHalf:
        {
            assert(op2 == nullptr);
            if (op1Reg != targetReg)
            {
                emit->emitIns_R_R(ins, emitTypeSize(TYP_SIMD32), targetReg, op1Reg);
            }
            break;
        }

        case NI_AVX_TestC:
        {
            genHWIntrinsic_R_RM(node, ins, attr);
            emit->emitIns_R(INS_setb, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_AVX_TestNotZAndNotC:
        {
            genHWIntrinsic_R_RM(node, ins, attr);
            emit->emitIns_R(INS_seta, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
            break;
        }

        case NI_AVX_TestZ:
        {
            genHWIntrinsic_R_RM(node, ins, attr);
            emit->emitIns_R(INS_sete, EA_1BYTE, targetReg);
            emit->emitIns_R_R(INS_movzx, EA_1BYTE, targetReg, targetReg);
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
                assert(intrinsicId == NI_AVX_ExtractVector128 || NI_AVX_ExtractVector128);
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

            auto emitSwCase = [&](int8_t i) {
                if (numArgs == 3)
                {
                    if (intrinsicId == NI_AVX_ExtractVector128 || intrinsicId == NI_AVX2_ExtractVector128)
                    {
                        emit->emitIns_AR_R_I(ins, attr, op1Reg, 0, op2Reg, i);
                    }
                    else if (op2->TypeGet() == TYP_I_IMPL)
                    {
                        emit->emitIns_SIMD_R_R_AR_I(ins, attr, targetReg, op1Reg, op2Reg, i);
                    }
                    else
                    {
                        assert(op2->TypeGet() == TYP_SIMD16);
                        emit->emitIns_SIMD_R_R_R_I(ins, attr, targetReg, op1Reg, op2Reg, i);
                    }
                }
                else
                {
                    assert(numArgs == 2);
                    assert(intrinsicId == NI_AVX_ExtractVector128 || intrinsicId == NI_AVX2_ExtractVector128);
                    emit->emitIns_SIMD_R_R_I(ins, attr, targetReg, op1Reg, i);
                }
            };

            if (lastOp->IsCnsIntOrI())
            {
                ssize_t ival = lastOp->AsIntCon()->IconValue();
                assert((ival >= 0) && (ival <= 255));
                emitSwCase((int8_t)ival);
            }
            else
            {
                // We emit a fallback case for the scenario when the imm-op is not a constant. This should
                // normally happen when the intrinsic is called indirectly, such as via Reflection. However, it
                // can also occur if the consumer calls it directly and just doesn't pass a constant value.
                regNumber baseReg = node->ExtractTempReg();
                regNumber offsReg = node->GetSingleTempReg();
                genHWIntrinsicJumpTableFallback(intrinsicId, op3Reg, baseReg, offsReg, emitSwCase);
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
    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;
    regNumber      targetReg   = node->gtRegNum;
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    var_types      baseType    = node->gtSIMDBaseType;
    var_types      targetType  = node->TypeGet();
    instruction    ins         = HWIntrinsicInfo::lookupIns(intrinsicId, targetType);
    emitter*       emit        = getEmitter();

    assert(targetReg != REG_NA);
    assert(op1 != nullptr);

    if (!op1->OperIsList())
    {
        genConsumeOperands(node);
    }

    switch (intrinsicId)
    {
        case NI_BMI1_AndNot:
        {
            assert(op2 != nullptr);
            assert(op1->TypeGet() == op2->TypeGet());
            assert((targetType == TYP_INT) || (targetType == TYP_LONG));
            genHWIntrinsic_R_R_RM(node, ins, emitTypeSize(node->TypeGet()));
            break;
        }

        case NI_BMI1_ExtractLowestSetBit:
        case NI_BMI1_GetMaskUpToLowestSetBit:
        case NI_BMI1_ResetLowestSetBit:
        case NI_BMI1_TrailingZeroCount:
        {
            assert(op2 == nullptr);
            assert((targetType == TYP_INT) || (targetType == TYP_LONG));
            genHWIntrinsic_R_RM(node, ins, emitTypeSize(node->TypeGet()));
            break;
        }

        default:
        {
            unreached();
            break;
        }
    }

    genProduceReg(node);
}

//------------------------------------------------------------------------
// genBMI2Intrinsic: Generates the code for a BMI2 hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genBMI2Intrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;
    regNumber      targetReg   = node->gtRegNum;
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    var_types      baseType    = node->gtSIMDBaseType;
    var_types      targetType  = node->TypeGet();
    instruction    ins         = HWIntrinsicInfo::lookupIns(intrinsicId, targetType);
    emitter*       emit        = getEmitter();

    assert(targetReg != REG_NA);
    assert(op1 != nullptr);

    if (!op1->OperIsList())
    {
        genConsumeOperands(node);
    }

    switch (intrinsicId)
    {
        case NI_BMI2_ParallelBitDeposit:
        case NI_BMI2_ParallelBitExtract:
        {
            assert(op2 != nullptr);
            assert(op1->TypeGet() == op2->TypeGet());
            assert((targetType == TYP_INT) || (targetType == TYP_LONG));
            genHWIntrinsic_R_R_RM(node, ins, emitTypeSize(node->TypeGet()));
            break;
        }

        default:
        {
            unreached();
            break;
        }
    }

    genProduceReg(node);
}

//------------------------------------------------------------------------
// genFMAIntrinsic: Generates the code for an FMA hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genFMAIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;
    var_types      baseType    = node->gtSIMDBaseType;
    emitAttr       attr        = EA_ATTR(node->gtSIMDSize);
    instruction    ins         = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
    GenTree*       op1         = node->gtGetOp1();
    regNumber      targetReg   = node->gtRegNum;

    assert(HWIntrinsicInfo::lookupNumArgs(node) == 3);
    assert(op1 != nullptr);
    assert(op1->OperIsList());
    assert(op1->gtGetOp2()->OperIsList());
    assert(op1->gtGetOp2()->gtGetOp2()->OperIsList());

    GenTreeArgList* argList = op1->AsArgList();
    op1                     = argList->Current();
    genConsumeRegs(op1);

    argList      = argList->Rest();
    GenTree* op2 = argList->Current();
    genConsumeRegs(op2);

    argList      = argList->Rest();
    GenTree* op3 = argList->Current();
    genConsumeRegs(op3);

    regNumber op1Reg;
    regNumber op2Reg;

    bool       isCommutative   = false;
    const bool copiesUpperBits = HWIntrinsicInfo::CopiesUpperBits(intrinsicId);

    // Intrinsics with CopyUpperBits semantics cannot have op1 be contained
    assert(!copiesUpperBits || !op1->isContained());

    if (op3->isContained() || op3->isUsedFromSpillTemp())
    {
        // 213 form: op1 = (op2 * op1) + [op3]

        op1Reg = op1->gtRegNum;
        op2Reg = op2->gtRegNum;

        isCommutative = !copiesUpperBits;
    }
    else if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        // 132 form: op1 = (op1 * op3) + [op2]

        ins    = (instruction)(ins - 1);
        op1Reg = op1->gtRegNum;
        op2Reg = op3->gtRegNum;
        op3    = op2;
    }
    else if (op1->isContained() || op1->isUsedFromSpillTemp())
    {
        // 231 form: op3 = (op2 * op3) + [op1]

        ins    = (instruction)(ins + 1);
        op1Reg = op3->gtRegNum;
        op2Reg = op2->gtRegNum;
        op3    = op1;
    }
    else
    {
        // 213 form: op1 = (op2 * op1) + op3

        op1Reg = op1->gtRegNum;
        op2Reg = op2->gtRegNum;

        isCommutative = !copiesUpperBits;
    }

    if (isCommutative && (op1Reg != targetReg) && (op2Reg == targetReg))
    {
        assert(node->isRMWHWIntrinsic(compiler));

        // We have "reg2 = (reg1 * reg2) +/- op3" where "reg1 != reg2" on a RMW intrinsic.
        //
        // For non-commutative intrinsics, we should have ensured that op2 was marked
        // delay free in order to prevent it from getting assigned the same register
        // as target. However, for commutative intrinsics, we can just swap the operands
        // in order to have "reg2 = reg2 op reg1" which will end up producing the right code.

        op2Reg = op1Reg;
        op1Reg = targetReg;
    }

    genHWIntrinsic_R_R_R_RM(ins, attr, targetReg, op1Reg, op2Reg, op3);
    genProduceReg(node);
}

//------------------------------------------------------------------------
// genLZCNTIntrinsic: Generates the code for a LZCNT hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genLZCNTIntrinsic(GenTreeHWIntrinsic* node)
{
    assert(node->gtHWIntrinsicId == NI_LZCNT_LeadingZeroCount);

    genConsumeOperands(node);
    genHWIntrinsic_R_RM(node, INS_lzcnt, emitTypeSize(node->TypeGet()));
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
    assert(node->gtHWIntrinsicId == NI_POPCNT_PopCount);

    genConsumeOperands(node);
    genHWIntrinsic_R_RM(node, INS_popcnt, emitTypeSize(node->TypeGet()));
    genProduceReg(node);
}

#endif // FEATURE_HW_INTRINSICS
