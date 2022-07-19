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
//    lowering       - The lowering phase from the compiler
//    containingNode - The HWIntrinsic node that has the contained node
//    containedNode  - The node that is contained
//
static void assertIsContainableHWIntrinsicOp(Lowering*           lowering,
                                             GenTreeHWIntrinsic* containingNode,
                                             GenTree*            containedNode)
{
#if DEBUG
    // The Lowering::IsContainableHWIntrinsicOp call is not quite right, since it follows pre-register allocation
    // logic. However, this check is still important due to the various containment rules that SIMD intrinsics follow.
    //
    // We use isContainable to track the special HWIntrinsic node containment rules (for things like LoadAligned and
    // LoadUnaligned) and we use the supportsRegOptional check to support general-purpose loads (both from stack
    // spillage and for isUsedFromMemory contained nodes, in the case where the register allocator decided to not
    // allocate a register in the first place).

    GenTree* node = containedNode;

    // Now that we are doing full memory containment safety checks, we can't properly check nodes that are not
    // linked into an evaluation tree, like the special nodes we create in genHWIntrinsic.
    // So, just say those are ok.
    //
    if (node->gtNext == nullptr)
    {
        return;
    }

    bool supportsRegOptional = false;
    bool isContainable       = lowering->TryGetContainableHWIntrinsicOp(containingNode, &node, &supportsRegOptional);

    assert(isContainable || supportsRegOptional);
    assert(node == containedNode);
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
    NamedIntrinsic         intrinsicId = node->GetHWIntrinsicId();
    CORINFO_InstructionSet isa         = HWIntrinsicInfo::lookupIsa(intrinsicId);
    HWIntrinsicCategory    category    = HWIntrinsicInfo::lookupCategory(intrinsicId);
    size_t                 numArgs     = node->GetOperandCount();

    // We need to validate that other phases of the compiler haven't introduced unsupported intrinsics
    assert(compiler->compIsaSupportedDebugOnly(isa));

    int ival = HWIntrinsicInfo::lookupIval(intrinsicId, compiler->compOpportunisticallyDependsOn(InstructionSet_AVX));

    assert(HWIntrinsicInfo::RequiresCodegen(intrinsicId));

    if (genIsTableDrivenHWIntrinsic(intrinsicId, category))
    {
        regNumber targetReg = node->GetRegNum();
        var_types baseType  = node->GetSimdBaseType();

        GenTree* op1 = nullptr;
        GenTree* op2 = nullptr;
        GenTree* op3 = nullptr;

        regNumber op1Reg = REG_NA;
        regNumber op2Reg = REG_NA;
        emitter*  emit   = GetEmitter();

        assert(numArgs >= 0);
        instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
        assert(ins != INS_invalid);
        emitAttr simdSize = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->GetSimdSize()));

        assert(simdSize != 0);

        switch (numArgs)
        {
            case 1:
            {
                op1 = node->Op(1);

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
                            genHWIntrinsic_R_RM_I(node, ins, simdSize, static_cast<int8_t>(ival));
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
                op1 = node->Op(1);
                op2 = node->Op(2);

                if (category == HW_Category_MemoryStore)
                {
                    genConsumeAddress(op1);

                    if (((intrinsicId == NI_SSE_Store) || (intrinsicId == NI_SSE2_Store)) && op2->isContained())
                    {
                        GenTreeHWIntrinsic* extract = op2->AsHWIntrinsic();

                        assert((extract->GetHWIntrinsicId() == NI_AVX_ExtractVector128) ||
                               (extract->GetHWIntrinsicId() == NI_AVX2_ExtractVector128));

                        regNumber regData = genConsumeReg(extract->Op(1));

                        ins  = HWIntrinsicInfo::lookupIns(extract->GetHWIntrinsicId(), extract->GetSimdBaseType());
                        ival = static_cast<int>(extract->Op(2)->AsIntCon()->IconValue());

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
                    genHWIntrinsic_R_R_RM_I(node, ins, simdSize, static_cast<int8_t>(ival));
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
                    auto emitSwCase = [&](int8_t i) { genHWIntrinsic_R_RM_I(node, ins, simdSize, i); };

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
                op1 = node->Op(1);
                op2 = node->Op(2);
                op3 = node->Op(3);

                genConsumeRegs(op1);
                op1Reg = op1->GetRegNum();

                genConsumeRegs(op2);
                op2Reg = op2->GetRegNum();

                genConsumeRegs(op3);
                regNumber op3Reg = op3->GetRegNum();

                if (HWIntrinsicInfo::isImmOp(intrinsicId, op3))
                {
                    assert(ival == -1);

                    auto emitSwCase = [&](int8_t i) { genHWIntrinsic_R_R_RM_I(node, ins, simdSize, i); };

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
                        emit->emitIns_Mov(INS_mov, EA_PTRSIZE, REG_EDI, op3Reg, /* canSkip */ true);

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
                            genHWIntrinsic_R_R_RM_R(node, ins, simdSize);
                            break;
                        }
                        case NI_AVXVNNI_MultiplyWideningAndAdd:
                        case NI_AVXVNNI_MultiplyWideningAndAddSaturate:
                        {
                            assert(targetReg != REG_NA);
                            assert(op1Reg != REG_NA);
                            assert(op2Reg != REG_NA);

                            genHWIntrinsic_R_R_R_RM(ins, simdSize, targetReg, op1Reg, op2Reg, op3);
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
        case InstructionSet_X86Serialize:
        case InstructionSet_X86Serialize_X64:
            genX86SerializeIntrinsic(node);
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
    emitter*    emit     = GetEmitter();
    OperandDesc rmOpDesc = genOperandDesc(rmOp);

    if (rmOpDesc.IsContained())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->GetHWIntrinsicId()));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, rmOp);
    }

    switch (rmOpDesc.GetKind())
    {
        case OperandKind::ClsVar:
            emit->emitIns_R_C(ins, attr, reg, rmOpDesc.GetFieldHnd(), 0);
            break;

        case OperandKind::Local:
            emit->emitIns_R_S(ins, attr, reg, rmOpDesc.GetVarNum(), rmOpDesc.GetLclOffset());
            break;

        case OperandKind::Indir:
        {
            // Until we improve the handling of addressing modes in the emitter, we'll create a
            // temporary GT_IND to generate code with.
            GenTreeIndir  indirForm;
            GenTreeIndir* indir = rmOpDesc.GetIndirForm(&indirForm);
            emit->emitIns_R_A(ins, attr, reg, indir);
        }
        break;

        case OperandKind::Reg:
            if (emit->IsMovInstruction(ins))
            {
                emit->emitIns_Mov(ins, attr, reg, rmOp->GetRegNum(), /* canSkip */ false);
            }
            else
            {
                emit->emitIns_R_R(ins, attr, reg, rmOp->GetRegNum());
            }
            break;

        default:
            unreached();
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
void CodeGen::genHWIntrinsic_R_RM_I(GenTreeHWIntrinsic* node, instruction ins, emitAttr simdSize, int8_t ival)
{
    regNumber targetReg = node->GetRegNum();
    GenTree*  op1       = node->Op(1);

    // TODO-XArch-CQ: Commutative operations can have op1 be contained
    // TODO-XArch-CQ: Non-VEX encoded instructions can have both ops contained

    assert(targetReg != REG_NA);
    assert(!node->OperIsCommutative()); // One operand intrinsics cannot be commutative

    if (op1->isContained() || op1->isUsedFromSpillTemp())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->GetHWIntrinsicId()));
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
    GenTree*  op1       = node->Op(1);
    GenTree*  op2       = node->Op(2);
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
        assert(HWIntrinsicInfo::SupportsContainment(node->GetHWIntrinsicId()));
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
void CodeGen::genHWIntrinsic_R_R_RM_I(GenTreeHWIntrinsic* node, instruction ins, emitAttr simdSize, int8_t ival)
{
    regNumber targetReg = node->GetRegNum();
    GenTree*  op1       = node->Op(1);
    GenTree*  op2       = node->Op(2);
    emitter*  emit      = GetEmitter();

    // TODO-XArch-CQ: Commutative operations can have op1 be contained
    // TODO-XArch-CQ: Non-VEX encoded instructions can have both ops contained

    regNumber op1Reg = op1->GetRegNum();

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);

    OperandDesc op2Desc = genOperandDesc(op2);

    if (op2Desc.IsContained())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->GetHWIntrinsicId()));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, op2);
    }

    switch (op2Desc.GetKind())
    {
        case OperandKind::ClsVar:
            emit->emitIns_SIMD_R_R_C_I(ins, simdSize, targetReg, op1Reg, op2Desc.GetFieldHnd(), 0, ival);
            break;

        case OperandKind::Local:
            emit->emitIns_SIMD_R_R_S_I(ins, simdSize, targetReg, op1Reg, op2Desc.GetVarNum(), op2Desc.GetLclOffset(),
                                       ival);
            break;

        case OperandKind::Indir:
        {
            // Until we improve the handling of addressing modes in the emitter, we'll create a
            // temporary GT_IND to generate code with.
            GenTreeIndir  indirForm;
            GenTreeIndir* indir = op2Desc.GetIndirForm(&indirForm);
            emit->emitIns_SIMD_R_R_A_I(ins, simdSize, targetReg, op1Reg, indir, ival);
        }
        break;

        case OperandKind::Reg:
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
        break;

        default:
            unreached();
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
void CodeGen::genHWIntrinsic_R_R_RM_R(GenTreeHWIntrinsic* node, instruction ins, emitAttr simdSize)
{
    regNumber targetReg = node->GetRegNum();
    GenTree*  op1       = node->Op(1);
    GenTree*  op2       = node->Op(2);
    GenTree*  op3       = node->Op(3);
    emitter*  emit      = GetEmitter();

    regNumber op1Reg = op1->GetRegNum();
    regNumber op3Reg = op3->GetRegNum();

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);
    assert(op3Reg != REG_NA);

    OperandDesc op2Desc = genOperandDesc(op2);

    if (op2Desc.IsContained())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->GetHWIntrinsicId()));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, op2);
    }

    switch (op2Desc.GetKind())
    {
        case OperandKind::ClsVar:
            emit->emitIns_SIMD_R_R_C_R(ins, simdSize, targetReg, op1Reg, op3Reg, op2Desc.GetFieldHnd(), 0);
            break;

        case OperandKind::Local:
            emit->emitIns_SIMD_R_R_S_R(ins, simdSize, targetReg, op1Reg, op3Reg, op2Desc.GetVarNum(),
                                       op2Desc.GetLclOffset());
            break;

        case OperandKind::Indir:
        {
            // Until we improve the handling of addressing modes in the emitter, we'll create a
            // temporary GT_IND to generate code with.
            GenTreeIndir  indirForm;
            GenTreeIndir* indir = op2Desc.GetIndirForm(&indirForm);
            emit->emitIns_SIMD_R_R_A_R(ins, simdSize, targetReg, op1Reg, op3Reg, indir);
        }
        break;

        case OperandKind::Reg:
            emit->emitIns_SIMD_R_R_R_R(ins, simdSize, targetReg, op1Reg, op2->GetRegNum(), op3Reg);
            break;

        default:
            unreached();
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

    emitter*    emit    = GetEmitter();
    OperandDesc op3Desc = genOperandDesc(op3);

    switch (op3Desc.GetKind())
    {
        case OperandKind::ClsVar:
            emit->emitIns_SIMD_R_R_R_C(ins, attr, targetReg, op1Reg, op2Reg, op3Desc.GetFieldHnd(), 0);
            break;

        case OperandKind::Local:
            emit->emitIns_SIMD_R_R_R_S(ins, attr, targetReg, op1Reg, op2Reg, op3Desc.GetVarNum(),
                                       op3Desc.GetLclOffset());
            break;

        case OperandKind::Indir:
        {
            // Until we improve the handling of addressing modes in the emitter, we'll create a
            // temporary GT_IND to generate code with.
            GenTreeIndir  indirForm;
            GenTreeIndir* indir = op3Desc.GetIndirForm(&indirForm);
            emit->emitIns_SIMD_R_R_R_A(ins, attr, targetReg, op1Reg, op2Reg, indir);
        }
        break;

        case OperandKind::Reg:
            emit->emitIns_SIMD_R_R_R_R(ins, attr, targetReg, op1Reg, op2Reg, op3->GetRegNum());
            break;

        default:
            unreached();
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
//    (GT_BOUNDS_CHECK) for imm8 argument, so this function does not need to do range-check.
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
    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();
    regNumber      targetReg   = node->GetRegNum();
    var_types      baseType    = node->GetSimdBaseType();

    assert(compiler->compIsaSupportedDebugOnly(InstructionSet_SSE));
    assert((baseType >= TYP_BYTE) && (baseType <= TYP_DOUBLE));

    GenTree* op1 = (node->GetOperandCount() >= 1) ? node->Op(1) : nullptr;
    GenTree* op2 = (node->GetOperandCount() >= 2) ? node->Op(2) : nullptr;

    genConsumeMultiOpOperands(node);
    regNumber op1Reg = (op1 == nullptr) ? REG_NA : op1->GetRegNum();

    emitter*    emit     = GetEmitter();
    var_types   simdType = Compiler::getSIMDTypeForSize(node->GetSimdSize());
    emitAttr    attr     = emitActualTypeSize(simdType);
    instruction ins      = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);

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
                else
                {
                    // Just use movaps for reg->reg moves as it has zero-latency on modern CPUs
                    emit->emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
                }
            }
            break;
        }

        case NI_Vector128_GetElement:
        case NI_Vector256_GetElement:
        {
            if (simdType == TYP_SIMD12)
            {
                // op1 of TYP_SIMD12 should be considered as TYP_SIMD16
                simdType = TYP_SIMD16;
            }

            // Optimize the case of op1 is in memory and trying to access i'th element.
            if (!op1->isUsedFromReg())
            {
                assert(op1->isContained());

                regNumber baseReg;
                regNumber indexReg;
                int       offset = 0;

                if (op1->OperIsLocal())
                {
                    // There are three parts to the total offset here:
                    // {offset of local} + {offset of vector field (lclFld only)} + {offset of element within vector}.
                    bool     isEBPbased;
                    unsigned varNum = op1->AsLclVarCommon()->GetLclNum();
                    offset += compiler->lvaFrameAddress(varNum, &isEBPbased);

#if !FEATURE_FIXED_OUT_ARGS
                    if (!isEBPbased)
                    {
                        // Adjust the offset by the amount currently pushed on the CPU stack
                        offset += genStackLevel;
                    }
#else
                    assert(genStackLevel == 0);
#endif // !FEATURE_FIXED_OUT_ARGS

                    if (op1->OperIs(GT_LCL_FLD))
                    {
                        offset += op1->AsLclFld()->GetLclOffs();
                    }
                    baseReg = (isEBPbased) ? REG_EBP : REG_ESP;
                }
                else
                {
                    // Require GT_IND addr to be not contained.
                    assert(op1->OperIs(GT_IND));

                    GenTree* addr = op1->AsIndir()->Addr();
                    assert(!addr->isContained());
                    baseReg = addr->GetRegNum();
                }

                if (op2->OperIsConst())
                {
                    assert(op2->isContained());
                    indexReg = REG_NA;
                    offset += (int)op2->AsIntCon()->IconValue() * genTypeSize(baseType);
                }
                else
                {
                    indexReg = op2->GetRegNum();
                    assert(genIsValidIntReg(indexReg));
                }

                // Now, load the desired element.
                GetEmitter()->emitIns_R_ARX(ins_Move_Extend(baseType, false), // Load
                                            emitTypeSize(baseType),           // Of the vector baseType
                                            targetReg,                        // To targetReg
                                            baseReg,                          // Base Reg
                                            indexReg,                         // Indexed
                                            genTypeSize(baseType),            // by the size of the baseType
                                            offset);
            }
            else if (op2->OperIsConst())
            {
                assert(intrinsicId == NI_Vector128_GetElement);
                assert(varTypeIsFloating(baseType));
                assert(op1Reg != REG_NA);

                ssize_t ival = op2->AsIntCon()->IconValue();

                if (baseType == TYP_FLOAT)
                {
                    if (ival == 1)
                    {
                        if (compiler->compOpportunisticallyDependsOn(InstructionSet_SSE3))
                        {
                            emit->emitIns_R_R(INS_movshdup, attr, targetReg, op1Reg);
                        }
                        else
                        {
                            emit->emitIns_SIMD_R_R_R_I(INS_shufps, attr, targetReg, op1Reg, op1Reg,
                                                       static_cast<int8_t>(0x55));
                        }
                    }
                    else if (ival == 2)
                    {
                        emit->emitIns_SIMD_R_R_R(INS_unpckhps, attr, targetReg, op1Reg, op1Reg);
                    }
                    else
                    {
                        assert(ival == 3);
                        emit->emitIns_SIMD_R_R_R_I(INS_shufps, attr, targetReg, op1Reg, op1Reg,
                                                   static_cast<int8_t>(0xFF));
                    }
                }
                else
                {
                    assert(baseType == TYP_DOUBLE);
                    assert(ival == 1);
                    emit->emitIns_SIMD_R_R_R(INS_unpckhpd, attr, targetReg, op1Reg, op1Reg);
                }
            }
            else
            {
                // We don't have an instruction to implement this intrinsic if the index is not a constant.
                // So we will use the SIMD temp location to store the vector, and the load the desired element.
                // The range check will already have been performed, so at this point we know we have an index
                // within the bounds of the vector.

                unsigned simdInitTempVarNum = compiler->lvaSIMDInitTempVarNum;
                noway_assert(simdInitTempVarNum != BAD_VAR_NUM);

                bool     isEBPbased;
                unsigned offs = compiler->lvaFrameAddress(simdInitTempVarNum, &isEBPbased);

#if !FEATURE_FIXED_OUT_ARGS
                if (!isEBPbased)
                {
                    // Adjust the offset by the amount currently pushed on the CPU stack
                    offs += genStackLevel;
                }
#else
                assert(genStackLevel == 0);
#endif // !FEATURE_FIXED_OUT_ARGS

                regNumber indexReg = op2->GetRegNum();

                // Store the vector to the temp location.
                GetEmitter()->emitIns_S_R(ins_Store(simdType, compiler->isSIMDTypeLocalAligned(simdInitTempVarNum)),
                                          emitTypeSize(simdType), op1Reg, simdInitTempVarNum, 0);

                // Now, load the desired element.
                GetEmitter()->emitIns_R_ARX(ins_Move_Extend(baseType, false), // Load
                                            emitTypeSize(baseType),           // Of the vector baseType
                                            targetReg,                        // To targetReg
                                            (isEBPbased) ? REG_EBP : REG_ESP, // Stack-based
                                            indexReg,                         // Indexed
                                            genTypeSize(baseType),            // by the size of the baseType
                                            offs);
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
            else
            {
                // Just use movaps for reg->reg moves as it has zero-latency on modern CPUs
                emit->emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
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
                emit->emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ false);
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
            else
            {
                // Just use movaps for reg->reg moves as it has zero-latency on modern CPUs
                emit->emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
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
// genX86BaseIntrinsic: Generates the code for an X86 base hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genX86BaseIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();

    genConsumeMultiOpOperands(node);

    switch (intrinsicId)
    {
        case NI_X86Base_BitScanForward:
        case NI_X86Base_BitScanReverse:
        case NI_X86Base_X64_BitScanForward:
        case NI_X86Base_X64_BitScanReverse:
        {
            GenTree*    op1        = node->Op(1);
            regNumber   targetReg  = node->GetRegNum();
            var_types   targetType = node->TypeGet();
            instruction ins        = HWIntrinsicInfo::lookupIns(intrinsicId, targetType);

            genHWIntrinsic_R_RM(node, ins, emitTypeSize(targetType), targetReg, op1);
            break;
        }

        case NI_X86Base_Pause:
        {
            assert(node->GetSimdBaseType() == TYP_UNKNOWN);
            GetEmitter()->emitIns(INS_pause);
            break;
        }

        default:
            unreached();
            break;
    }

    genProduceReg(node);
}

//------------------------------------------------------------------------
// genSSEIntrinsic: Generates the code for an SSE hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genSSEIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();
    regNumber      targetReg   = node->GetRegNum();
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->GetSimdBaseType();
    emitter*       emit        = GetEmitter();

    genConsumeMultiOpOperands(node);

    switch (intrinsicId)
    {
        case NI_SSE_X64_ConvertToInt64:
        case NI_SSE_X64_ConvertToInt64WithTruncation:
        {
            assert(targetType == TYP_LONG);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            genHWIntrinsic_R_RM(node, ins, EA_8BYTE, targetReg, node->Op(1));
            break;
        }

        case NI_SSE_X64_ConvertScalarToVector128Single:
        {
            assert(baseType == TYP_LONG);
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

            // These do not support containment.
            assert(!node->Op(1)->isContained());
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, node->GetSimdBaseType());
            emit->emitIns_AR(ins, emitTypeSize(baseType), node->Op(1)->GetRegNum(), 0);
            break;
        }

        case NI_SSE_StoreFence:
        {
            assert(baseType == TYP_UNKNOWN);
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
    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();
    regNumber      targetReg   = node->GetRegNum();
    var_types      targetType  = node->TypeGet();
    var_types      baseType    = node->GetSimdBaseType();
    emitter*       emit        = GetEmitter();

    genConsumeMultiOpOperands(node);

    switch (intrinsicId)
    {
        case NI_SSE2_X64_ConvertScalarToVector128Double:
        {
            assert(baseType == TYP_LONG);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            genHWIntrinsic_R_R_RM(node, ins, EA_8BYTE);
            break;
        }

        case NI_SSE2_X64_ConvertScalarToVector128Int64:
        case NI_SSE2_X64_ConvertScalarToVector128UInt64:
        {
            assert(baseType == TYP_LONG || baseType == TYP_ULONG);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            genHWIntrinsic_R_RM(node, ins, emitTypeSize(baseType), targetReg, node->Op(1));
            break;
        }

        case NI_SSE2_ConvertToInt32:
        case NI_SSE2_ConvertToInt32WithTruncation:
        case NI_SSE2_ConvertToUInt32:
        case NI_SSE2_X64_ConvertToInt64:
        case NI_SSE2_X64_ConvertToInt64WithTruncation:
        case NI_SSE2_X64_ConvertToUInt64:
        {
            emitAttr attr;
            if (varTypeIsIntegral(baseType))
            {
                assert(baseType == TYP_INT || baseType == TYP_UINT || baseType == TYP_LONG || baseType == TYP_ULONG);
                attr = emitActualTypeSize(baseType);
            }
            else
            {
                assert(baseType == TYP_DOUBLE || baseType == TYP_FLOAT);
                attr = emitTypeSize(targetType);
            }

            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            genHWIntrinsic_R_RM(node, ins, attr, targetReg, node->Op(1));
            break;
        }

        case NI_SSE2_LoadFence:
        {
            assert(baseType == TYP_UNKNOWN);
            emit->emitIns(INS_lfence);
            break;
        }

        case NI_SSE2_MemoryFence:
        {
            assert(baseType == TYP_UNKNOWN);
            emit->emitIns(INS_mfence);
            break;
        }

        case NI_SSE2_StoreNonTemporal:
        case NI_SSE2_X64_StoreNonTemporal:
        {
            assert(baseType == TYP_INT || baseType == TYP_UINT || baseType == TYP_LONG || baseType == TYP_ULONG);
            instruction     ins   = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            GenTreeStoreInd store = storeIndirForm(node->TypeGet(), node->Op(1), node->Op(2));
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
    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();
    GenTree*       op1         = node->Op(1);
    regNumber      targetReg   = node->GetRegNum();
    var_types      baseType    = node->GetSimdBaseType();

    emitter* emit = GetEmitter();

    genConsumeMultiOpOperands(node);

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
            assert(!varTypeIsFloating(baseType));

            GenTree*    op2  = node->Op(2);
            instruction ins  = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            emitAttr    attr = emitActualTypeSize(node->TypeGet());

            auto emitSwCase = [&](int8_t i) { inst_RV_TT_IV(ins, attr, targetReg, op1, i); };

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
    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();
    regNumber      targetReg   = node->GetRegNum();
    GenTree*       op1         = node->Op(1);
    GenTree*       op2         = node->Op(2);
    var_types      baseType    = node->GetSimdBaseType();
    var_types      targetType  = node->TypeGet();
    emitter*       emit        = GetEmitter();

    genConsumeMultiOpOperands(node);
    regNumber op1Reg = op1->GetRegNum();

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);
    assert(!node->OperIsCommutative());

    switch (intrinsicId)
    {
        case NI_SSE42_Crc32:
        case NI_SSE42_X64_Crc32:
        {
            assert((op2->GetRegNum() != targetReg) || (op1Reg == targetReg));
            emit->emitIns_Mov(INS_mov, emitTypeSize(targetType), targetReg, op1Reg, /* canSkip */ true);

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
    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();
    var_types      baseType    = node->GetSimdBaseType();
    emitAttr       attr        = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->GetSimdSize()));
    var_types      targetType  = node->TypeGet();
    instruction    ins         = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
    size_t         numArgs     = node->GetOperandCount();
    GenTree*       op1         = node->Op(1);
    regNumber      op1Reg      = REG_NA;
    regNumber      targetReg   = node->GetRegNum();
    emitter*       emit        = GetEmitter();

    genConsumeMultiOpOperands(node);

    switch (intrinsicId)
    {
        case NI_AVX2_ConvertToInt32:
        case NI_AVX2_ConvertToUInt32:
        {
            op1Reg = op1->GetRegNum();
            assert((baseType == TYP_INT) || (baseType == TYP_UINT));
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            emit->emitIns_Mov(ins, emitActualTypeSize(baseType), targetReg, op1Reg, /* canSkip */ false);
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
            GenTree* op2     = node->Op(2);
            GenTree* op3     = node->Op(3);
            GenTree* lastOp  = nullptr;
            GenTree* indexOp = nullptr;

            op1Reg                 = op1->GetRegNum();
            regNumber op2Reg       = op2->GetRegNum();
            regNumber addrBaseReg  = REG_NA;
            regNumber addrIndexReg = REG_NA;
            regNumber maskReg      = node->ExtractTempReg(RBM_ALLFLOAT);

            if (numArgs == 5)
            {
                assert(intrinsicId == NI_AVX2_GatherMaskVector128 || intrinsicId == NI_AVX2_GatherMaskVector256);

                GenTree* op4 = node->Op(4);
                lastOp       = node->Op(5);

                regNumber op3Reg = op3->GetRegNum();
                regNumber op4Reg = op4->GetRegNum();

                addrBaseReg  = op2Reg;
                addrIndexReg = op3Reg;
                indexOp      = op3;

                // copy op4Reg into the tmp mask register,
                // the mask register will be cleared by gather instructions
                emit->emitIns_Mov(INS_movaps, attr, maskReg, op4Reg, /* canSkip */ false);

                // copy source vector to the target register for masking merge
                emit->emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
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
    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();
    regNumber      targetReg   = node->GetRegNum();
    var_types      targetType  = node->TypeGet();
    instruction    ins         = HWIntrinsicInfo::lookupIns(intrinsicId, targetType);
    emitter*       emit        = GetEmitter();

    assert(targetReg != REG_NA);

    genConsumeMultiOpOperands(node);

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
            assert((targetType == TYP_INT) || (targetType == TYP_LONG));
            genHWIntrinsic_R_RM(node, ins, emitTypeSize(node->TypeGet()), targetReg, node->Op(1));
            break;
        }

        case NI_BMI1_TrailingZeroCount:
        case NI_BMI1_X64_TrailingZeroCount:
        {
            assert((targetType == TYP_INT) || (targetType == TYP_LONG));
            genXCNTIntrinsic(node, ins);
            break;
        }

        case NI_BMI2_MultiplyNoFlags:
        case NI_BMI2_X64_MultiplyNoFlags:
        {
            size_t numArgs = node->GetOperandCount();
            assert(numArgs == 2 || numArgs == 3);

            GenTree* op1 = node->Op(1);
            GenTree* op2 = node->Op(2);

            regNumber op1Reg = op1->GetRegNum();
            regNumber op2Reg = op2->GetRegNum();
            regNumber op3Reg = REG_NA;
            regNumber lowReg = REG_NA;

            if (numArgs == 2)
            {
                lowReg = targetReg;
            }
            else
            {
                op3Reg = node->Op(3)->GetRegNum();

                assert(!node->Op(3)->isContained());
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
            assert((op2Reg != REG_EDX) || (op1Reg == REG_EDX));
            emit->emitIns_Mov(INS_mov, attr, REG_EDX, op1Reg, /* canSkip */ true);

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
    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();
    var_types      baseType    = node->GetSimdBaseType();
    emitAttr       attr        = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->GetSimdSize()));
    instruction    _213form    = HWIntrinsicInfo::lookupIns(intrinsicId, baseType); // 213 form
    instruction    _132form    = (instruction)(_213form - 1);
    instruction    _231form    = (instruction)(_213form + 1);
    GenTree*       op1         = node->Op(1);
    GenTree*       op2         = node->Op(2);
    GenTree*       op3         = node->Op(3);

    regNumber targetReg = node->GetRegNum();

    genConsumeMultiOpOperands(node);

    regNumber op1NodeReg = op1->GetRegNum();
    regNumber op2NodeReg = op2->GetRegNum();
    regNumber op3NodeReg = op3->GetRegNum();

    GenTree* emitOp1 = op1;
    GenTree* emitOp2 = op2;
    GenTree* emitOp3 = op3;

    const bool copiesUpperBits = HWIntrinsicInfo::CopiesUpperBits(intrinsicId);

    // Intrinsics with CopyUpperBits semantics cannot have op1 be contained
    assert(!copiesUpperBits || !op1->isContained());

    // We need to keep this in sync with lsraxarch.cpp
    // Ideally we'd actually swap the operands in lsra and simplify codegen
    // but its a bit more complicated to do so for many operands as well
    // as being complicated to tell codegen how to pick the right instruction

    instruction ins = INS_invalid;

    if (op1->isContained() || op1->isUsedFromSpillTemp())
    {
        // targetReg == op3NodeReg or targetReg == ?
        // op3 = ([op1] * op2) + op3
        // 231 form: XMM1 = (XMM2 * [XMM3]) + XMM1
        ins = _231form;
        std::swap(emitOp1, emitOp3);

        if (targetReg == op2NodeReg)
        {
            // op2 = ([op1] * op2) + op3
            // 132 form: XMM1 = (XMM1 * [XMM3]) + XMM2
            ins = _132form;
            std::swap(emitOp1, emitOp2);
        }
    }
    else if (op3->isContained() || op3->isUsedFromSpillTemp())
    {
        // targetReg could be op1NodeReg, op2NodeReg, or not equal to any op
        // op1 = (op1 * op2) + [op3] or op2 = (op1 * op2) + [op3]
        // ? = (op1 * op2) + [op3] or ? = (op1 * op2) + op3
        // 213 form: XMM1 = (XMM2 * XMM1) + [XMM3]
        ins = _213form;

        if (!copiesUpperBits && (targetReg == op2NodeReg))
        {
            // op2 = (op1 * op2) + [op3]
            // 213 form: XMM1 = (XMM2 * XMM1) + [XMM3]
            std::swap(emitOp1, emitOp2);
        }
    }
    else if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        // targetReg == op1NodeReg or targetReg == ?
        // op1 = (op1 * [op2]) + op3
        // 132 form: XMM1 = (XMM1 * [XMM3]) + XMM2
        ins = _132form;
        std::swap(emitOp2, emitOp3);

        if (!copiesUpperBits && (targetReg == op3NodeReg))
        {
            // op3 = (op1 * [op2]) + op3
            // 231 form: XMM1 = (XMM2 * [XMM3]) + XMM1
            ins = _231form;
            std::swap(emitOp1, emitOp2);
        }
    }
    else
    {
        // When we don't have a contained operand we still want to
        // preference based on the target register if possible.

        if (targetReg == op2NodeReg)
        {
            ins = _213form;
            std::swap(emitOp1, emitOp2);
        }
        else if (targetReg == op3NodeReg)
        {
            ins = _231form;
            std::swap(emitOp1, emitOp3);
        }
        else
        {
            ins = _213form;
        }
    }

    assert(ins != INS_invalid);
    genHWIntrinsic_R_R_R_RM(ins, attr, targetReg, emitOp1->GetRegNum(), emitOp2->GetRegNum(), emitOp3);
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
    assert((node->GetHWIntrinsicId() == NI_LZCNT_LeadingZeroCount) ||
           (node->GetHWIntrinsicId() == NI_LZCNT_X64_LeadingZeroCount));

    genConsumeMultiOpOperands(node);
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
    assert(node->GetHWIntrinsicId() == NI_POPCNT_PopCount || node->GetHWIntrinsicId() == NI_POPCNT_X64_PopCount);

    genConsumeMultiOpOperands(node);
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

    GenTree*  op1        = node->Op(1);
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

//------------------------------------------------------------------------
// genX86SerializeIntrinsic: Generates the code for an X86 serialize hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genX86SerializeIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();

    genConsumeMultiOpOperands(node);

    switch (intrinsicId)
    {
        case NI_X86Serialize_Serialize:
        {
            assert(node->GetSimdBaseType() == TYP_UNKNOWN);
            GetEmitter()->emitIns(INS_serialize);
            break;
        }

        default:
            unreached();
            break;
    }

    genProduceReg(node);
}

#endif // FEATURE_HW_INTRINSICS
