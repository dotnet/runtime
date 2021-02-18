// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    NamedIntrinsic         intrinsicId = node->gtHWIntrinsicId;
    CORINFO_InstructionSet isa         = HWIntrinsicInfo::lookupIsa(intrinsicId);
    HWIntrinsicCategory    category    = HWIntrinsicInfo::lookupCategory(intrinsicId);
    int                    numArgs     = HWIntrinsicInfo::lookupNumArgs(node);

    int ival = HWIntrinsicInfo::lookupIval(intrinsicId, compiler->compOpportunisticallyDependsOn(InstructionSet_AVX));

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
        emitAttr simdSize = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->gtSIMDSize));
        assert(simdSize != 0);

        switch (numArgs)
        {
            case 1:
            {
                if (node->OperIsMemoryLoad())
                {
                    genConsumeAddress(op1);
                    // Until we improve the handling of addressing modes in the emitter, we'll create a
                    // temporary GT_IND to generate code with.
                    GenTreeIndir load = indirForm(node->TypeGet(), op1);
                    emit->emitInsLoadInd(ins, simdSize, node->GetRegNum(), &load);
                }
                else
                {
                    genConsumeRegs(op1);
                    op1Reg = op1->GetRegNum();

                    if ((ival != -1) && varTypeIsFloating(baseType))
                    {
                        assert((ival >= 0) && (ival <= 127));
                        if ((category == HW_Category_SIMDScalar) && HWIntrinsicInfo::CopiesUpperBits(intrinsicId))
                        {
                            assert(!op1->isContained());
                            emit->emitIns_SIMD_R_R_R_I(ins, simdSize, targetReg, op1Reg, op1Reg,
                                                       static_cast<int8_t>(ival));
                        }
                        else
                        {
                            genHWIntrinsic_R_RM_I(node, ins, static_cast<int8_t>(ival));
                        }
                    }
                    else if ((category == HW_Category_SIMDScalar) && HWIntrinsicInfo::CopiesUpperBits(intrinsicId))
                    {
                        emit->emitIns_SIMD_R_R_R(ins, simdSize, targetReg, op1Reg, op1Reg);
                    }
                    else
                    {
                        genHWIntrinsic_R_RM(node, ins, simdSize, targetReg, op1);
                    }
                }
                break;
            }

            case 2:
            {
                if (category == HW_Category_MemoryStore)
                {
                    genConsumeAddress(op1);

                    if (((intrinsicId == NI_SSE_Store) || (intrinsicId == NI_SSE2_Store)) && op2->isContained())
                    {
                        GenTreeHWIntrinsic* extract = op2->AsHWIntrinsic();

                        assert((extract->gtHWIntrinsicId == NI_AVX_ExtractVector128) ||
                               (extract->gtHWIntrinsicId == NI_AVX2_ExtractVector128));

                        regNumber regData = genConsumeReg(extract->gtGetOp1());

                        ins  = HWIntrinsicInfo::lookupIns(extract->gtHWIntrinsicId, extract->gtSIMDBaseType);
                        ival = static_cast<int>(extract->gtGetOp2()->AsIntCon()->IconValue());

                        GenTreeIndir indir = indirForm(TYP_SIMD16, op1);
                        emit->emitIns_A_R_I(ins, EA_32BYTE, &indir, regData, ival);
                    }
                    else
                    {
                        genConsumeReg(op2);
                        // Until we improve the handling of addressing modes in the emitter, we'll create a
                        // temporary GT_STORE_IND to generate code with.
                        GenTreeStoreInd store = storeIndirForm(node->TypeGet(), op1, op2);
                        emit->emitInsStoreInd(ins, simdSize, &store);
                    }
                    break;
                }
                genConsumeRegs(op1);
                genConsumeRegs(op2);

                op1Reg = op1->GetRegNum();
                op2Reg = op2->GetRegNum();

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

                if ((ival != -1) && varTypeIsFloating(baseType))
                {
                    assert((ival >= 0) && (ival <= 127));
                    genHWIntrinsic_R_R_RM_I(node, ins, static_cast<int8_t>(ival));
                }
                else if (category == HW_Category_MemoryLoad)
                {
                    // Get the address and the 'other' register.
                    GenTree*  addr;
                    regNumber otherReg;
                    if (intrinsicId == NI_AVX_MaskLoad || intrinsicId == NI_AVX2_MaskLoad)
                    {
                        addr     = op1;
                        otherReg = op2Reg;
                    }
                    else
                    {
                        addr     = op2;
                        otherReg = op1Reg;
                    }
                    // Until we improve the handling of addressing modes in the emitter, we'll create a
                    // temporary GT_IND to generate code with.
                    GenTreeIndir load = indirForm(node->TypeGet(), addr);
                    genHWIntrinsic_R_R_RM(node, ins, simdSize, targetReg, otherReg, &load);
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
                else if (node->TypeGet() == TYP_VOID)
                {
                    genHWIntrinsic_R_RM(node, ins, simdSize, op1Reg, op2);
                }
                else
                {
                    genHWIntrinsic_R_R_RM(node, ins, simdSize);
                }
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
                    // The Mask instructions do not currently support containment of the address.
                    assert(!op2->isContained());
                    if (intrinsicId == NI_AVX_MaskStore || intrinsicId == NI_AVX2_MaskStore)
                    {
                        emit->emitIns_AR_R_R(ins, simdSize, op2Reg, op3Reg, op1Reg, 0);
                    }
                    else
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
        case InstructionSet_Vector128:
        case InstructionSet_Vector256:
            genBaseIntrinsic(node);
            break;
        case InstructionSet_X86Base:
        case InstructionSet_X86Base_X64:
            genX86BaseIntrinsic(node);
            break;
        case InstructionSet_SSE:
        case InstructionSet_SSE_X64:
            genSSEIntrinsic(node);
            break;
        case InstructionSet_SSE2:
        case InstructionSet_SSE2_X64:
            genSSE2Intrinsic(node);
            break;
        case InstructionSet_SSE41:
        case InstructionSet_SSE41_X64:
            genSSE41Intrinsic(node);
            break;
        case InstructionSet_SSE42:
        case InstructionSet_SSE42_X64:
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
        case InstructionSet_BMI1_X64:
        case InstructionSet_BMI2:
        case InstructionSet_BMI2_X64:
            genBMI1OrBMI2Intrinsic(node);
            break;
        case InstructionSet_FMA:
            genFMAIntrinsic(node);
            break;
        case InstructionSet_LZCNT:
        case InstructionSet_LZCNT_X64:
            genLZCNTIntrinsic(node);
            break;
        case InstructionSet_PCLMULQDQ:
            genPCLMULQDQIntrinsic(node);
            break;
        case InstructionSet_POPCNT:
        case InstructionSet_POPCNT_X64:
            genPOPCNTIntrinsic(node);
            break;
        default:
            unreached();
            break;
    }
}

//------------------------------------------------------------------------
// genHWIntrinsic_R_RM: Generates code for a hardware intrinsic node that takes a
//                      register operand and a register/memory operand.
//
// Arguments:
//    node - The hardware intrinsic node
//    ins  - The instruction being generated
//    attr - The emit attribute for the instruciton being generated
//    reg  - The register
//    rmOp - The register/memory operand node
//
void CodeGen::genHWIntrinsic_R_RM(
    GenTreeHWIntrinsic* node, instruction ins, emitAttr attr, regNumber reg, GenTree* rmOp)
{
    emitter* emit = GetEmitter();

    assert(reg != REG_NA);

    if (rmOp->isContained() || rmOp->isUsedFromSpillTemp())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->gtHWIntrinsicId));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, rmOp);

        TempDsc* tmpDsc = nullptr;
        unsigned varNum = BAD_VAR_NUM;
        unsigned offset = (unsigned)-1;

        if (rmOp->isUsedFromSpillTemp())
        {
            assert(rmOp->IsRegOptional());

            tmpDsc = getSpillTempDsc(rmOp);
            varNum = tmpDsc->tdTempNum();
            offset = 0;

            regSet.tmpRlsTemp(tmpDsc);
        }
        else if (rmOp->isIndir() || rmOp->OperIsHWIntrinsic())
        {
            GenTree*      addr;
            GenTreeIndir* memIndir = nullptr;

            if (rmOp->isIndir())
            {
                memIndir = rmOp->AsIndir();
                addr     = memIndir->Addr();
            }
            else
            {
                assert(rmOp->AsHWIntrinsic()->OperIsMemoryLoad());
                assert(HWIntrinsicInfo::lookupNumArgs(rmOp->AsHWIntrinsic()) == 1);
                addr = rmOp->gtGetOp1();
            }

            switch (addr->OperGet())
            {
                case GT_LCL_VAR_ADDR:
                case GT_LCL_FLD_ADDR:
                {
                    assert(addr->isContained());
                    varNum = addr->AsLclVarCommon()->GetLclNum();
                    offset = addr->AsLclVarCommon()->GetLclOffs();
                    break;
                }

                case GT_CLS_VAR_ADDR:
                {
                    emit->emitIns_R_C(ins, attr, reg, addr->AsClsVar()->gtClsVarHnd, 0);
                    return;
                }

                default:
                {
                    GenTreeIndir load = indirForm(rmOp->TypeGet(), addr);

                    if (memIndir == nullptr)
                    {
                        // This is the HW intrinsic load case.
                        // Until we improve the handling of addressing modes in the emitter, we'll create a
                        // temporary GT_IND to generate code with.
                        memIndir = &load;
                    }
                    emit->emitIns_R_A(ins, attr, reg, memIndir);
                    return;
                }
            }
        }
        else
        {
            switch (rmOp->OperGet())
            {
                case GT_LCL_FLD:
                    varNum = rmOp->AsLclFld()->GetLclNum();
                    offset = rmOp->AsLclFld()->GetLclOffs();
                    break;

                case GT_LCL_VAR:
                {
                    assert(rmOp->IsRegOptional() || !compiler->lvaGetDesc(rmOp->AsLclVar())->lvIsRegCandidate());
                    varNum = rmOp->AsLclVar()->GetLclNum();
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

        emit->emitIns_R_S(ins, attr, reg, varNum, offset);
    }
    else
    {
        emit->emitIns_R_R(ins, attr, reg, rmOp->GetRegNum());
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
    regNumber targetReg  = node->GetRegNum();
    GenTree*  op1        = node->gtGetOp1();
    emitAttr  simdSize   = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->gtSIMDSize));
    emitter*  emit       = GetEmitter();

    // TODO-XArch-CQ: Commutative operations can have op1 be contained
    // TODO-XArch-CQ: Non-VEX encoded instructions can have both ops contained

    assert(targetReg != REG_NA);
    assert(!node->OperIsCommutative()); // One operand intrinsics cannot be commutative

    if (op1->isContained() || op1->isUsedFromSpillTemp())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->gtHWIntrinsicId));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, op1);
    }
    inst_RV_TT_IV(ins, simdSize, targetReg, op1, ival);
}

//------------------------------------------------------------------------
// genHWIntrinsic_R_R_RM: Generates the code for a hardware intrinsic node that takes a register operand, a
//                        register/memory operand, and that returns a value in register
//
// Arguments:
//    node - The hardware intrinsic node
//    ins  - The instruction being generated
//    attr - The emit attribute for the instruciton being generated
//
void CodeGen::genHWIntrinsic_R_R_RM(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr)
{
    regNumber targetReg = node->GetRegNum();
    GenTree*  op1       = node->gtGetOp1();
    GenTree*  op2       = node->gtGetOp2();
    regNumber op1Reg    = op1->GetRegNum();

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);

    genHWIntrinsic_R_R_RM(node, ins, attr, targetReg, op1Reg, op2);
}

//------------------------------------------------------------------------
// genHWIntrinsic_R_R_RM: Generates the code for a hardware intrinsic node that takes a register operand, a
//                        register/memory operand, and that returns a value in register
//
// Arguments:
//    node - The hardware intrinsic node
//    ins  - The instruction being generated
//    attr - The emit attribute for the instruciton being generated
//    targetReg - The register allocated to the result
//    op1Reg    - The register allocated to the first operand
//    op2       - Another operand that maybe in register or memory
//
void CodeGen::genHWIntrinsic_R_R_RM(
    GenTreeHWIntrinsic* node, instruction ins, emitAttr attr, regNumber targetReg, regNumber op1Reg, GenTree* op2)
{
    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);

    if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->gtHWIntrinsicId));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, op2);
    }

    bool isRMW = node->isRMWHWIntrinsic(compiler);
    inst_RV_RV_TT(ins, attr, targetReg, op1Reg, op2, isRMW);
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
    regNumber targetReg  = node->GetRegNum();
    GenTree*  op1        = node->gtGetOp1();
    GenTree*  op2        = node->gtGetOp2();
    emitAttr  simdSize   = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->gtSIMDSize));
    emitter*  emit       = GetEmitter();

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

    regNumber op1Reg = op1->GetRegNum();

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
        else if (op2->isIndir() || op2->OperIsHWIntrinsic())
        {
            GenTree*      addr;
            GenTreeIndir* memIndir = nullptr;

            if (op2->isIndir())
            {
                memIndir = op2->AsIndir();
                addr     = memIndir->Addr();
            }
            else
            {
                assert(op2->AsHWIntrinsic()->OperIsMemoryLoad());
                assert(HWIntrinsicInfo::lookupNumArgs(op2->AsHWIntrinsic()) == 1);
                addr = op2->gtGetOp1();
            }

            switch (addr->OperGet())
            {
                case GT_LCL_VAR_ADDR:
                case GT_LCL_FLD_ADDR:
                {
                    assert(addr->isContained());
                    varNum = addr->AsLclVarCommon()->GetLclNum();
                    offset = addr->AsLclVarCommon()->GetLclOffs();
                    break;
                }

                case GT_CLS_VAR_ADDR:
                {
                    emit->emitIns_SIMD_R_R_C_I(ins, simdSize, targetReg, op1Reg, addr->AsClsVar()->gtClsVarHnd, 0,
                                               ival);
                    return;
                }

                default:
                {
                    GenTreeIndir load = indirForm(op2->TypeGet(), addr);

                    if (memIndir == nullptr)
                    {
                        // This is the HW intrinsic load case.
                        // Until we improve the handling of addressing modes in the emitter, we'll create a
                        // temporary GT_IND to generate code with.
                        memIndir = &load;
                    }
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
                    varNum = op2->AsLclFld()->GetLclNum();
                    offset = op2->AsLclFld()->GetLclOffs();
                    break;

                case GT_LCL_VAR:
                {
                    assert(op2->IsRegOptional() ||
                           !compiler->lvaTable[op2->AsLclVar()->GetLclNum()].lvIsRegCandidate());
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
        regNumber op2Reg = op2->GetRegNum();

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
    regNumber targetReg  = node->GetRegNum();
    GenTree*  op1        = node->gtGetOp1();
    GenTree*  op2        = node->gtGetOp2();
    GenTree*  op3        = nullptr;
    emitAttr  simdSize   = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->gtSIMDSize));
    emitter*  emit       = GetEmitter();

    assert(op1->OperIsList());
    assert(op2 == nullptr);

    GenTreeArgList* argList = op1->AsArgList();

    op1     = argList->Current();
    argList = argList->Rest();

    op2     = argList->Current();
    argList = argList->Rest();

    op3 = argList->Current();
    assert(argList->Rest() == nullptr);

    regNumber op1Reg = op1->GetRegNum();
    regNumber op3Reg = op3->GetRegNum();

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
        else if (op2->isIndir() || op2->OperIsHWIntrinsic())
        {
            GenTree*      addr;
            GenTreeIndir* memIndir = nullptr;

            if (op2->isIndir())
            {
                memIndir = op2->AsIndir();
                addr     = memIndir->Addr();
            }
            else
            {
                assert(op2->AsHWIntrinsic()->OperIsMemoryLoad());
                assert(HWIntrinsicInfo::lookupNumArgs(op2->AsHWIntrinsic()) == 1);
                addr = op2->gtGetOp1();
            }

            switch (addr->OperGet())
            {
                case GT_LCL_VAR_ADDR:
                case GT_LCL_FLD_ADDR:
                {
                    assert(addr->isContained());
                    varNum = addr->AsLclVarCommon()->GetLclNum();
                    offset = addr->AsLclVarCommon()->GetLclOffs();
                    break;
                }

                case GT_CLS_VAR_ADDR:
                {
                    emit->emitIns_SIMD_R_R_C_R(ins, simdSize, targetReg, op1Reg, op3Reg, addr->AsClsVar()->gtClsVarHnd,
                                               0);
                    return;
                }

                default:
                {
                    GenTreeIndir load = indirForm(op2->TypeGet(), addr);

                    if (memIndir == nullptr)
                    {
                        // This is the HW intrinsic load case.
                        // Until we improve the handling of addressing modes in the emitter, we'll create a
                        // temporary GT_IND to generate code with.
                        memIndir = &load;
                    }
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
                    varNum = op2->AsLclFld()->GetLclNum();
                    offset = op2->AsLclFld()->GetLclOffs();
                    break;

                case GT_LCL_VAR:
                {
                    assert(op2->IsRegOptional() ||
                           !compiler->lvaTable[op2->AsLclVar()->GetLclNum()].lvIsRegCandidate());
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
        emit->emitIns_SIMD_R_R_R_R(ins, simdSize, targetReg, op1Reg, op2->GetRegNum(), op3Reg);
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

    emitter* emit = GetEmitter();

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
        else if (op3->isIndir() || op3->OperIsHWIntrinsic())
        {
            GenTree*      addr;
            GenTreeIndir* memIndir = nullptr;
            if (op3->isIndir())
            {
                memIndir = op3->AsIndir();
                addr     = memIndir->Addr();
            }
            else
            {
                assert(op3->AsHWIntrinsic()->OperIsMemoryLoad());
                assert(HWIntrinsicInfo::lookupNumArgs(op3->AsHWIntrinsic()) == 1);
                addr = op3->gtGetOp1();
            }

            switch (addr->OperGet())
            {
                case GT_LCL_VAR_ADDR:
                case GT_LCL_FLD_ADDR:
                {
                    assert(addr->isContained());
                    varNum = addr->AsLclVarCommon()->GetLclNum();
                    offset = addr->AsLclVarCommon()->GetLclOffs();
                    break;
                }

                case GT_CLS_VAR_ADDR:
                {
                    emit->emitIns_SIMD_R_R_R_C(ins, attr, targetReg, op1Reg, op2Reg, addr->AsClsVar()->gtClsVarHnd, 0);
                    return;
                }

                default:
                {
                    GenTreeIndir load = indirForm(op3->TypeGet(), addr);

                    if (memIndir == nullptr)
                    {
                        // This is the HW intrinsic load case.
                        // Until we improve the handling of addressing modes in the emitter, we'll create a
                        // temporary GT_IND to generate code with.
                        memIndir = &load;
                    }
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
                    varNum = op3->AsLclFld()->GetLclNum();
                    offset = op3->AsLclFld()->GetLclOffs();
                    break;

                case GT_LCL_VAR:
                {
                    assert(op3->IsRegOptional() ||
                           !compiler->lvaTable[op3->AsLclVar()->GetLclNum()].lvIsRegCandidate());
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
        emit->emitIns_SIMD_R_R_R_R(ins, attr, targetReg, op1Reg, op2Reg, op3->GetRegNum());
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
//    emitSwCase     - the lambda to generate a switch case
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
    // AVX2 Gather intrinsics use managed non-const fallback since they have discrete imm8 value range
    // that does work with the current compiler generated jump-table fallback
    assert(!HWIntrinsicInfo::isAVX2GatherIntrinsic(intrinsic));
    emitter* emit = GetEmitter();

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
// genBaseIntrinsic: Generates the code for a base hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
// Note:
//    We currently assume that all base intrinsics have zero or one operand.
//
void CodeGen::genBaseIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;
    regNumber      targetReg   = node->GetRegNum();
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->gtSIMDBaseType;

    assert(compiler->compIsaSupportedDebugOnly(InstructionSet_SSE));
    assert((baseType >= TYP_BYTE) && (baseType <= TYP_DOUBLE));

    GenTree* op1 = node->gtGetOp1();

    genConsumeHWIntrinsicOperands(node);
    regNumber op1Reg = (op1 == nullptr) ? REG_NA : op1->GetRegNum();

    assert(node->gtGetOp2() == nullptr);

    emitter*    emit = GetEmitter();
    emitAttr    attr = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->gtSIMDSize));
    instruction ins  = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);

    switch (intrinsicId)
    {
        case NI_Vector128_CreateScalarUnsafe:
        case NI_Vector256_CreateScalarUnsafe:
        {
            if (varTypeIsIntegral(baseType))
            {
                genHWIntrinsic_R_RM(node, ins, emitActualTypeSize(baseType), targetReg, op1);
            }
            else
            {
                assert(varTypeIsFloating(baseType));

                attr = emitTypeSize(baseType);

                if (op1->isContained() || op1->isUsedFromSpillTemp())
                {
                    genHWIntrinsic_R_RM(node, ins, attr, targetReg, op1);
                }
                else if (targetReg != op1Reg)
                {
                    // Just use movaps for reg->reg moves as it has zero-latency on modern CPUs
                    emit->emitIns_R_R(INS_movaps, attr, targetReg, op1Reg);
                }
            }
            break;
        }

        case NI_Vector128_ToScalar:
        case NI_Vector256_ToScalar:
        {
            assert(varTypeIsFloating(baseType));

            attr = emitTypeSize(TYP_SIMD16);

            if (op1->isContained() || op1->isUsedFromSpillTemp())
            {
                genHWIntrinsic_R_RM(node, ins, attr, targetReg, op1);
            }
            else if (targetReg != op1Reg)
            {
                // Just use movaps for reg->reg moves as it has zero-latency on modern CPUs
                emit->emitIns_R_R(INS_movaps, attr, targetReg, op1Reg);
            }
            break;
        }

        case NI_Vector128_ToVector256:
        {
            // ToVector256 has zero-extend semantics in order to ensure it is deterministic
            // We always emit a move to the target register, even when op1Reg == targetReg,
            // in order to ensure that Bits MAXVL-1:128 are zeroed.

            attr = emitTypeSize(TYP_SIMD16);

            if (op1->isContained() || op1->isUsedFromSpillTemp())
            {
                genHWIntrinsic_R_RM(node, ins, attr, targetReg, op1);
            }
            else
            {
                // Just use movaps for reg->reg moves as it has zero-latency on modern CPUs
                emit->emitIns_R_R(INS_movaps, attr, targetReg, op1Reg);
            }
            break;
        }

        case NI_Vector128_ToVector256Unsafe:
        case NI_Vector256_GetLower:
        {
            if (op1->isContained() || op1->isUsedFromSpillTemp())
            {
                genHWIntrinsic_R_RM(node, ins, attr, targetReg, op1);
            }
            else if (targetReg != op1Reg)
            {
                // Just use movaps for reg->reg moves as it has zero-latency on modern CPUs
                emit->emitIns_R_R(INS_movaps, attr, targetReg, op1Reg);
            }
            break;
        }

        case NI_Vector128_get_Zero:
        case NI_Vector256_get_Zero:
        {
            assert(op1 == nullptr);
            emit->emitIns_SIMD_R_R_R(ins, attr, targetReg, targetReg, targetReg);
            break;
        }

        case NI_Vector128_get_AllBitsSet:
            assert(op1 == nullptr);
            if (varTypeIsFloating(baseType) && compiler->compOpportunisticallyDependsOn(InstructionSet_AVX))
            {
                // The following corresponds to vcmptrueps pseudo-op and not available without VEX prefix.
                emit->emitIns_SIMD_R_R_R_I(ins, attr, targetReg, targetReg, targetReg, 15);
            }
            else
            {
                emit->emitIns_SIMD_R_R_R(INS_pcmpeqd, attr, targetReg, targetReg, targetReg);
            }
            break;

        case NI_Vector256_get_AllBitsSet:
            assert(op1 == nullptr);
            if (varTypeIsIntegral(baseType) && compiler->compOpportunisticallyDependsOn(InstructionSet_AVX2))
            {
                emit->emitIns_SIMD_R_R_R(ins, attr, targetReg, targetReg, targetReg);
            }
            else
            {
                assert(compiler->compIsaSupportedDebugOnly(InstructionSet_AVX));
                // The following corresponds to vcmptrueps pseudo-op.
                emit->emitIns_SIMD_R_R_R_I(INS_cmpps, attr, targetReg, targetReg, targetReg, 15);
            }
            break;

        default:
        {
            unreached();
            break;
        }
    }

    genProduceReg(node);
}

//------------------------------------------------------------------------
// genX86BaseIntrinsic: Generates the code for an X86 base hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genX86BaseIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;

    switch (intrinsicId)
    {
        case NI_X86Base_BitScanForward:
        case NI_X86Base_BitScanReverse:
        case NI_X86Base_X64_BitScanForward:
        case NI_X86Base_X64_BitScanReverse:
        {
            GenTree*    op1        = node->gtGetOp1();
            regNumber   targetReg  = node->GetRegNum();
            var_types   targetType = node->TypeGet();
            instruction ins        = HWIntrinsicInfo::lookupIns(intrinsicId, targetType);

            genConsumeOperands(node);
            genHWIntrinsic_R_RM(node, ins, emitTypeSize(targetType), targetReg, op1);
            genProduceReg(node);
            break;
        }

        default:
            unreached();
            break;
    }
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
    regNumber      targetReg   = node->GetRegNum();
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->gtSIMDBaseType;

    regNumber op1Reg = REG_NA;
    regNumber op2Reg = REG_NA;
    regNumber op3Reg = REG_NA;
    regNumber op4Reg = REG_NA;
    emitter*  emit   = GetEmitter();

    genConsumeHWIntrinsicOperands(node);

    switch (intrinsicId)
    {
        case NI_SSE_X64_ConvertToInt64:
        case NI_SSE_X64_ConvertToInt64WithTruncation:
        {
            assert(targetType == TYP_LONG);
            assert(op1 != nullptr);
            assert(op2 == nullptr);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            genHWIntrinsic_R_RM(node, ins, EA_8BYTE, targetReg, op1);
            break;
        }

        case NI_SSE_X64_ConvertScalarToVector128Single:
        {
            assert(baseType == TYP_LONG);
            assert(op1 != nullptr);
            assert(op2 != nullptr);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            genHWIntrinsic_R_R_RM(node, ins, EA_8BYTE);
            break;
        }

        case NI_SSE_Prefetch0:
        case NI_SSE_Prefetch1:
        case NI_SSE_Prefetch2:
        case NI_SSE_PrefetchNonTemporal:
        {
            assert(baseType == TYP_UBYTE);
            assert(op2 == nullptr);

            // These do not support containment.
            assert(!op1->isContained());
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, node->gtSIMDBaseType);
            op1Reg          = op1->GetRegNum();
            emit->emitIns_AR(ins, emitTypeSize(baseType), op1Reg, 0);
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
    regNumber      targetReg   = node->GetRegNum();
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->gtSIMDBaseType;
    regNumber      op1Reg      = REG_NA;
    regNumber      op2Reg      = REG_NA;
    emitter*       emit        = GetEmitter();

    genConsumeHWIntrinsicOperands(node);

    switch (intrinsicId)
    {
        case NI_SSE2_X64_ConvertScalarToVector128Double:
        {
            assert(baseType == TYP_LONG);
            assert(op1 != nullptr);
            assert(op2 != nullptr);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            genHWIntrinsic_R_R_RM(node, ins, EA_8BYTE);
            break;
        }

        case NI_SSE2_X64_ConvertScalarToVector128Int64:
        case NI_SSE2_X64_ConvertScalarToVector128UInt64:
        {
            assert(baseType == TYP_LONG || baseType == TYP_ULONG);
            assert(op1 != nullptr);
            assert(op2 == nullptr);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            genHWIntrinsic_R_RM(node, ins, emitTypeSize(baseType), targetReg, op1);
            break;
        }

        case NI_SSE2_ConvertToInt32:
        case NI_SSE2_ConvertToInt32WithTruncation:
        case NI_SSE2_ConvertToUInt32:
        case NI_SSE2_X64_ConvertToInt64:
        case NI_SSE2_X64_ConvertToInt64WithTruncation:
        case NI_SSE2_X64_ConvertToUInt64:
        {
            assert(op2 == nullptr);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);

            if (varTypeIsIntegral(baseType))
            {
                assert(baseType == TYP_INT || baseType == TYP_UINT || baseType == TYP_LONG || baseType == TYP_ULONG);
                op1Reg = op1->GetRegNum();
                emit->emitIns_R_R(ins, emitActualTypeSize(baseType), targetReg, op1Reg);
            }
            else
            {
                assert(baseType == TYP_DOUBLE || baseType == TYP_FLOAT);
                genHWIntrinsic_R_RM(node, ins, emitTypeSize(targetType), targetReg, op1);
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

        case NI_SSE2_StoreNonTemporal:
        case NI_SSE2_X64_StoreNonTemporal:
        {
            assert(baseType == TYP_INT || baseType == TYP_UINT || baseType == TYP_LONG || baseType == TYP_ULONG);
            assert(op1 != nullptr);
            assert(op2 != nullptr);

            instruction     ins   = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            GenTreeStoreInd store = storeIndirForm(node->TypeGet(), op1, op2);
            emit->emitInsStoreInd(ins, emitTypeSize(baseType), &store);
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
    regNumber      targetReg   = node->GetRegNum();
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->gtSIMDBaseType;

    regNumber op1Reg = REG_NA;
    regNumber op2Reg = REG_NA;
    regNumber op3Reg = REG_NA;
    regNumber op4Reg = REG_NA;
    emitter*  emit   = GetEmitter();

    genConsumeHWIntrinsicOperands(node);

    switch (intrinsicId)
    {
        case NI_SSE41_ConvertToVector128Int16:
        case NI_SSE41_ConvertToVector128Int32:
        case NI_SSE41_ConvertToVector128Int64:
        {
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);

            if (!varTypeIsSIMD(op1->gtType))
            {
                // Until we improve the handling of addressing modes in the emitter, we'll create a
                // temporary GT_IND to generate code with.
                GenTreeIndir load = indirForm(node->TypeGet(), op1);
                emit->emitInsLoadInd(ins, emitTypeSize(TYP_SIMD16), node->GetRegNum(), &load);
            }
            else
            {
                genHWIntrinsic_R_RM(node, ins, EA_16BYTE, targetReg, op1);
            }
            break;
        }

        case NI_SSE41_Extract:
        case NI_SSE41_X64_Extract:
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
                    inst_RV_TT_IV(ins, emitTypeSize(TYP_INT), tmpTargetReg, op1, i);
                    emit->emitIns_R_R(INS_movd, EA_4BYTE, targetReg, tmpTargetReg);
                }
                else
                {
                    inst_RV_TT_IV(ins, emitTypeSize(TYP_INT), targetReg, op1, i);
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
                genHWIntrinsicJumpTableFallback(intrinsicId, op2->GetRegNum(), baseReg, offsReg, emitSwCase);
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
    regNumber      targetReg   = node->GetRegNum();
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    var_types      baseType    = node->gtSIMDBaseType;
    var_types      targetType  = node->TypeGet();
    emitter*       emit        = GetEmitter();

    genConsumeHWIntrinsicOperands(node);
    regNumber op1Reg = op1->GetRegNum();

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);
    assert(op2 != nullptr);
    assert(!node->OperIsCommutative());

    switch (intrinsicId)
    {
        case NI_SSE42_Crc32:
        case NI_SSE42_X64_Crc32:
        {
            if (op1Reg != targetReg)
            {
                assert(op2->GetRegNum() != targetReg);
                emit->emitIns_R_R(INS_mov, emitTypeSize(targetType), targetReg, op1Reg);
            }

            if ((baseType == TYP_UBYTE) || (baseType == TYP_USHORT)) // baseType is the type of the second argument
            {
                assert(targetType == TYP_INT);
                genHWIntrinsic_R_RM(node, INS_crc32, emitTypeSize(baseType), targetReg, op2);
            }
            else
            {
                assert(op1->TypeGet() == op2->TypeGet());
                assert((targetType == TYP_INT) || (targetType == TYP_LONG));
                genHWIntrinsic_R_RM(node, INS_crc32, emitTypeSize(targetType), targetReg, op2);
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
    emitAttr       attr        = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->gtSIMDSize));
    var_types      targetType  = node->TypeGet();
    instruction    ins         = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
    int            numArgs     = HWIntrinsicInfo::lookupNumArgs(node);
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    regNumber      op1Reg      = REG_NA;
    regNumber      op2Reg      = REG_NA;
    regNumber      targetReg   = node->GetRegNum();
    emitter*       emit        = GetEmitter();

    genConsumeHWIntrinsicOperands(node);

    switch (intrinsicId)
    {
        case NI_AVX2_ConvertToInt32:
        case NI_AVX2_ConvertToUInt32:
        {
            op1Reg = op1->GetRegNum();
            assert(numArgs == 1);
            assert((baseType == TYP_INT) || (baseType == TYP_UINT));
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            emit->emitIns_R_R(ins, emitActualTypeSize(baseType), targetReg, op1Reg);
            break;
        }

        case NI_AVX2_ConvertToVector256Int16:
        case NI_AVX2_ConvertToVector256Int32:
        case NI_AVX2_ConvertToVector256Int64:
        {
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);

            if (!varTypeIsSIMD(op1->gtType))
            {
                // Until we improve the handling of addressing modes in the emitter, we'll create a
                // temporary GT_IND to generate code with.
                GenTreeIndir load = indirForm(node->TypeGet(), op1);
                emit->emitInsLoadInd(ins, emitTypeSize(TYP_SIMD32), node->GetRegNum(), &load);
            }
            else
            {
                genHWIntrinsic_R_RM(node, ins, EA_32BYTE, targetReg, op1);
            }
            break;
        }

        case NI_AVX2_GatherVector128:
        case NI_AVX2_GatherVector256:
        case NI_AVX2_GatherMaskVector128:
        case NI_AVX2_GatherMaskVector256:
        {
            GenTreeArgList* list = op1->AsArgList();
            op1                  = list->Current();
            op1Reg               = op1->GetRegNum();

            list   = list->Rest();
            op2    = list->Current();
            op2Reg = op2->GetRegNum();

            list         = list->Rest();
            GenTree* op3 = list->Current();

            list             = list->Rest();
            GenTree* op4     = nullptr;
            GenTree* lastOp  = nullptr;
            GenTree* indexOp = nullptr;

            regNumber op3Reg       = REG_NA;
            regNumber op4Reg       = REG_NA;
            regNumber addrBaseReg  = REG_NA;
            regNumber addrIndexReg = REG_NA;
            regNumber maskReg      = node->ExtractTempReg(RBM_ALLFLOAT);

            if (numArgs == 5)
            {
                assert(intrinsicId == NI_AVX2_GatherMaskVector128 || intrinsicId == NI_AVX2_GatherMaskVector256);
                op4          = list->Current();
                list         = list->Rest();
                lastOp       = list->Current();
                op3Reg       = op3->GetRegNum();
                op4Reg       = op4->GetRegNum();
                addrBaseReg  = op2Reg;
                addrIndexReg = op3Reg;
                indexOp      = op3;

                // copy op4Reg into the tmp mask register,
                // the mask register will be cleared by gather instructions
                emit->emitIns_R_R(INS_movaps, attr, maskReg, op4Reg);

                if (targetReg != op1Reg)
                {
                    // copy source vector to the target register for masking merge
                    emit->emitIns_R_R(INS_movaps, attr, targetReg, op1Reg);
                }
            }
            else
            {
                assert(intrinsicId == NI_AVX2_GatherVector128 || intrinsicId == NI_AVX2_GatherVector256);
                addrBaseReg  = op1Reg;
                addrIndexReg = op2Reg;
                indexOp      = op2;
                lastOp       = op3;

                // generate all-one mask vector
                emit->emitIns_SIMD_R_R_R(INS_pcmpeqd, attr, maskReg, maskReg, maskReg);
            }

            bool isVector128GatherWithVector256Index = (targetType == TYP_SIMD16) && (indexOp->TypeGet() == TYP_SIMD32);

            // hwintrinsiclistxarch.h uses Dword index instructions in default
            if (varTypeIsLong(node->GetAuxiliaryType()))
            {
                switch (ins)
                {
                    case INS_vpgatherdd:
                        ins = INS_vpgatherqd;
                        if (isVector128GatherWithVector256Index)
                        {
                            // YMM index in address mode
                            attr = emitTypeSize(TYP_SIMD32);
                        }
                        break;
                    case INS_vpgatherdq:
                        ins = INS_vpgatherqq;
                        break;
                    case INS_vgatherdps:
                        ins = INS_vgatherqps;
                        if (isVector128GatherWithVector256Index)
                        {
                            // YMM index in address mode
                            attr = emitTypeSize(TYP_SIMD32);
                        }
                        break;
                    case INS_vgatherdpd:
                        ins = INS_vgatherqpd;
                        break;
                    default:
                        unreached();
                }
            }

            assert(lastOp->IsCnsIntOrI());
            ssize_t ival = lastOp->AsIntCon()->IconValue();
            assert((ival >= 0) && (ival <= 255));

            assert(targetReg != maskReg);
            assert(targetReg != addrIndexReg);
            assert(maskReg != addrIndexReg);
            emit->emitIns_R_AR_R(ins, attr, targetReg, maskReg, addrBaseReg, addrIndexReg, (int8_t)ival, 0);

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
// genBMI1OrBMI2Intrinsic: Generates the code for a BMI1 and BMI2 hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genBMI1OrBMI2Intrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;
    regNumber      targetReg   = node->GetRegNum();
    GenTree*       op1         = node->gtGetOp1();
    GenTree*       op2         = node->gtGetOp2();
    var_types      targetType  = node->TypeGet();
    instruction    ins         = HWIntrinsicInfo::lookupIns(intrinsicId, targetType);
    emitter*       emit        = GetEmitter();

    assert(targetReg != REG_NA);
    assert(op1 != nullptr);

    genConsumeHWIntrinsicOperands(node);

    switch (intrinsicId)
    {
        case NI_BMI1_AndNot:
        case NI_BMI1_X64_AndNot:
        case NI_BMI1_BitFieldExtract:
        case NI_BMI1_X64_BitFieldExtract:
        case NI_BMI2_ParallelBitDeposit:
        case NI_BMI2_ParallelBitExtract:
        case NI_BMI2_X64_ParallelBitDeposit:
        case NI_BMI2_X64_ParallelBitExtract:
        case NI_BMI2_ZeroHighBits:
        case NI_BMI2_X64_ZeroHighBits:
        {
            assert(op2 != nullptr);
            assert((targetType == TYP_INT) || (targetType == TYP_LONG));
            genHWIntrinsic_R_R_RM(node, ins, emitTypeSize(node->TypeGet()));
            break;
        }

        case NI_BMI1_ExtractLowestSetBit:
        case NI_BMI1_GetMaskUpToLowestSetBit:
        case NI_BMI1_ResetLowestSetBit:
        case NI_BMI1_X64_ExtractLowestSetBit:
        case NI_BMI1_X64_GetMaskUpToLowestSetBit:
        case NI_BMI1_X64_ResetLowestSetBit:
        {
            assert(op2 == nullptr);
            assert((targetType == TYP_INT) || (targetType == TYP_LONG));
            genHWIntrinsic_R_RM(node, ins, emitTypeSize(node->TypeGet()), targetReg, op1);
            break;
        }

        case NI_BMI1_TrailingZeroCount:
        case NI_BMI1_X64_TrailingZeroCount:
        {
            assert(op2 == nullptr);
            assert((targetType == TYP_INT) || (targetType == TYP_LONG));
            genXCNTIntrinsic(node, ins);
            break;
        }

        case NI_BMI2_MultiplyNoFlags:
        case NI_BMI2_X64_MultiplyNoFlags:
        {
            int numArgs = HWIntrinsicInfo::lookupNumArgs(node);
            assert(numArgs == 2 || numArgs == 3);

            regNumber op1Reg = REG_NA;
            regNumber op2Reg = REG_NA;
            regNumber op3Reg = REG_NA;
            regNumber lowReg = REG_NA;

            if (numArgs == 2)
            {
                op1Reg = op1->GetRegNum();
                op2Reg = op2->GetRegNum();
                lowReg = targetReg;
            }
            else
            {
                GenTreeArgList* argList = op1->AsArgList();
                op1                     = argList->Current();
                op1Reg                  = op1->GetRegNum();
                argList                 = argList->Rest();
                op2                     = argList->Current();
                op2Reg                  = op2->GetRegNum();
                argList                 = argList->Rest();
                GenTree* op3            = argList->Current();
                op3Reg                  = op3->GetRegNum();
                assert(!op3->isContained());
                assert(op3Reg != op1Reg);
                assert(op3Reg != targetReg);
                assert(op3Reg != REG_EDX);
                lowReg = node->GetSingleTempReg();
                assert(op3Reg != lowReg);
                assert(lowReg != targetReg);
            }

            // These do not support containment
            assert(!op2->isContained());
            emitAttr attr = emitTypeSize(targetType);
            // mov the first operand into implicit source operand EDX/RDX
            if (op1Reg != REG_EDX)
            {
                assert(op2Reg != REG_EDX);
                emit->emitIns_R_R(INS_mov, attr, REG_EDX, op1Reg);
            }

            // generate code for MULX
            genHWIntrinsic_R_R_RM(node, ins, attr, targetReg, lowReg, op2);

            // If requires the lower half result, store in the memory pointed to by op3
            if (numArgs == 3)
            {
                emit->emitIns_AR_R(INS_mov, attr, lowReg, op3Reg, 0);
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
// genFMAIntrinsic: Generates the code for an FMA hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genFMAIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;
    var_types      baseType    = node->gtSIMDBaseType;
    emitAttr       attr        = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->gtSIMDSize));
    instruction    ins         = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
    GenTree*       op1         = node->gtGetOp1();
    regNumber      targetReg   = node->GetRegNum();

    assert(HWIntrinsicInfo::lookupNumArgs(node) == 3);

    genConsumeHWIntrinsicOperands(node);
    GenTreeArgList* argList = op1->AsArgList();
    op1                     = argList->Current();

    argList      = argList->Rest();
    GenTree* op2 = argList->Current();

    argList      = argList->Rest();
    GenTree* op3 = argList->Current();

    regNumber op1Reg;
    regNumber op2Reg;

    bool       isCommutative   = false;
    const bool copiesUpperBits = HWIntrinsicInfo::CopiesUpperBits(intrinsicId);

    // Intrinsics with CopyUpperBits semantics cannot have op1 be contained
    assert(!copiesUpperBits || !op1->isContained());

    if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        // 132 form: op1 = (op1 * op3) + [op2]

        ins    = (instruction)(ins - 1);
        op1Reg = op1->GetRegNum();
        op2Reg = op3->GetRegNum();
        op3    = op2;
    }
    else if (op1->isContained() || op1->isUsedFromSpillTemp())
    {
        // 231 form: op3 = (op2 * op3) + [op1]

        ins    = (instruction)(ins + 1);
        op1Reg = op3->GetRegNum();
        op2Reg = op2->GetRegNum();
        op3    = op1;
    }
    else
    {
        // 213 form: op1 = (op2 * op1) + [op3]

        op1Reg = op1->GetRegNum();
        op2Reg = op2->GetRegNum();

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
    assert(node->gtHWIntrinsicId == NI_LZCNT_LeadingZeroCount ||
           node->gtHWIntrinsicId == NI_LZCNT_X64_LeadingZeroCount);

    genConsumeOperands(node);
    genXCNTIntrinsic(node, INS_lzcnt);
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
    assert(node->gtHWIntrinsicId == NI_POPCNT_PopCount || node->gtHWIntrinsicId == NI_POPCNT_X64_PopCount);

    genConsumeOperands(node);
    genXCNTIntrinsic(node, INS_popcnt);
    genProduceReg(node);
}

//------------------------------------------------------------------------
// genXCNTIntrinsic: Generates the code for a lzcnt/tzcnt/popcnt hardware intrinsic node, breaks false dependencies on
// the target register
//
// Arguments:
//    node - The hardware intrinsic node
//    ins  - The instruction being generated
//
void CodeGen::genXCNTIntrinsic(GenTreeHWIntrinsic* node, instruction ins)
{
    // LZCNT/TZCNT/POPCNT have a false dependency on the target register on Intel Sandy Bridge, Haswell, and Skylake
    // (POPCNT only) processors, so insert a `XOR target, target` to break the dependency via XOR triggering register
    // renaming, but only if it's not an actual dependency.

    GenTree*  op1        = node->gtGetOp1();
    regNumber sourceReg1 = REG_NA;
    regNumber sourceReg2 = REG_NA;

    if (!op1->isContained())
    {
        sourceReg1 = op1->GetRegNum();
    }
    else if (op1->isIndir())
    {
        GenTreeIndir* indir   = op1->AsIndir();
        GenTree*      memBase = indir->Base();

        if (memBase != nullptr)
        {
            sourceReg1 = memBase->GetRegNum();
        }

        if (indir->HasIndex())
        {
            sourceReg2 = indir->Index()->GetRegNum();
        }
    }

    regNumber targetReg = node->GetRegNum();
    if ((targetReg != sourceReg1) && (targetReg != sourceReg2))
    {
        GetEmitter()->emitIns_R_R(INS_xor, EA_4BYTE, targetReg, targetReg);
    }
    genHWIntrinsic_R_RM(node, ins, emitTypeSize(node->TypeGet()), targetReg, op1);
}

#endif // FEATURE_HW_INTRINSICS
