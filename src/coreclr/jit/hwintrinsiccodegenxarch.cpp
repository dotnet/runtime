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
    bool isContainable       = lowering->IsContainableHWIntrinsicOp(containingNode, node, &supportsRegOptional);

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
// AddEmbRoundingMode: Adds the embedded rounding mode to the insOpts
//
// Arguments:
//    instOptions - The existing insOpts
//    mode        - The embedded rounding mode to add to instOptions
//
// Return Value:
//    The modified insOpts
//
static insOpts AddEmbRoundingMode(insOpts instOptions, int8_t mode)
{
    // The full rounding mode is a bitmask in the shape of:
    // * RC: 2-bit rounding control
    // * RS: 1-bit rounding select
    // * P:  1-bit precision mask
    // *     4-bit reserved
    //
    // The embedded rounding form assumes that P is 1, indicating
    // that floating-point exceptions should not be raised and also
    // assumes that RS is 0, indicating that MXCSR.RC is ignored.
    //
    // Given that the user is specifying a rounding mode and that
    // .NET doesn't support raising IEEE 754 floating-point exceptions,
    // we simplify the handling below to only consider the 2-bits of RC.

    assert((instOptions & INS_OPTS_EVEX_b_MASK) == 0);
    unsigned result = static_cast<unsigned>(instOptions);

    switch (mode & 0x03)
    {
        case 0x01:
        {
            result |= INS_OPTS_EVEX_eb_er_rd;
            break;
        }

        case 0x02:
        {
            result |= INS_OPTS_EVEX_er_ru;
            break;
        }

        case 0x03:
        {
            result |= INS_OPTS_EVEX_er_rz;
            break;
        }

        default:
        {
            break;
        }
    }

    return static_cast<insOpts>(result);
}

//------------------------------------------------------------------------
// AddEmbMaskingMode: Adds the embedded masking mode to the insOpts
//
// Arguments:
//    instOptions   - The existing insOpts
//    maskReg       - The register to use for the embedded mask
//    mergeWithZero - true if the mask merges with zero; otherwise, false
//
// Return Value:
//    The modified insOpts
//
static insOpts AddEmbMaskingMode(insOpts instOptions, regNumber maskReg, bool mergeWithZero)
{
    assert((instOptions & INS_OPTS_EVEX_aaa_MASK) == 0);
    assert((instOptions & INS_OPTS_EVEX_z_MASK) == 0);

    unsigned result = static_cast<unsigned>(instOptions);
    unsigned em_k   = (maskReg - KBASE) << 2;
    unsigned em_z   = mergeWithZero ? INS_OPTS_EVEX_em_zero : 0;

    assert(emitter::isMaskReg(maskReg));
    assert((em_k & INS_OPTS_EVEX_aaa_MASK) == em_k);

    result |= em_k;
    result |= em_z;

    return static_cast<insOpts>(result);
}

//------------------------------------------------------------------------
// genHWIntrinsic: Generates the code for a given hardware intrinsic node.
//
// Arguments:
//    node        - The hardware intrinsic node
//
void CodeGen::genHWIntrinsic(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic         intrinsicId = node->GetHWIntrinsicId();
    CORINFO_InstructionSet isa         = HWIntrinsicInfo::lookupIsa(intrinsicId);
    HWIntrinsicCategory    category    = HWIntrinsicInfo::lookupCategory(intrinsicId);
    size_t                 numArgs     = node->GetOperandCount();
    GenTree*               embMaskOp   = nullptr;

    // We need to validate that other phases of the compiler haven't introduced unsupported intrinsics
    assert(compiler->compIsaSupportedDebugOnly(isa));
    assert(HWIntrinsicInfo::RequiresCodegen(intrinsicId));

    bool    isTableDriven = genIsTableDrivenHWIntrinsic(intrinsicId, category);
    insOpts instOptions   = INS_OPTS_NONE;

    if (GetEmitter()->UseEvexEncoding())
    {
        if (numArgs == 3)
        {
            GenTree* op2 = node->Op(2);

            if (op2->IsEmbMaskOp())
            {
                assert(intrinsicId == NI_AVX512F_BlendVariableMask);
                assert(op2->isContained());
                assert(op2->OperIsHWIntrinsic());

                // We currently only support this for table driven intrinsics
                assert(isTableDriven);

                GenTree* op1 = node->Op(1);
                GenTree* op3 = node->Op(3);

                regNumber targetReg = node->GetRegNum();
                regNumber mergeReg  = op1->GetRegNum();
                regNumber maskReg   = op3->GetRegNum();

                // TODO-AVX512-CQ: Ensure we can support embedded operations on RMW intrinsics
                assert(!op2->isRMWHWIntrinsic(compiler));

                bool mergeWithZero = op1->isContained();

                if (mergeWithZero)
                {
                    // We're merging with zero, so we the target register isn't RMW
                    assert(op1->IsVectorZero());
                    mergeWithZero = true;
                }
                else
                {
                    // We're merging with a non-zero value, so the target register is RMW
                    emitAttr attr = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->GetSimdSize()));
                    GetEmitter()->emitIns_Mov(INS_movaps, attr, targetReg, mergeReg, /* canSkip */ true);
                }

                // Update op2 to use the actual target register
                op2->ClearContained();
                op2->SetRegNum(targetReg);

                // Fixup all the already initialized variables
                node        = op2->AsHWIntrinsic();
                intrinsicId = node->GetHWIntrinsicId();
                isa         = HWIntrinsicInfo::lookupIsa(intrinsicId);
                category    = HWIntrinsicInfo::lookupCategory(intrinsicId);
                numArgs     = node->GetOperandCount();

                // Add the embedded masking info to the insOpts
                instOptions = AddEmbMaskingMode(instOptions, maskReg, mergeWithZero);

                // We don't need to genProduceReg(node) since that will be handled by processing op2
                // likewise, processing op2 will ensure its own registers are consumed

                // Make sure we consume the registers that are getting specially handled
                genConsumeReg(op1);
                embMaskOp = op3;
            }
        }

        if (node->OperIsEmbRoundingEnabled())
        {
            GenTree* lastOp = node->Op(numArgs);

            // Now that we've extracted the rounding mode, we'll remove the
            // last operand, adjust the arg count, and continue. This allows
            // us to reuse all the existing logic without having to add new
            // specialized handling everywhere.

            switch (numArgs)
            {
                case 2:
                {
                    numArgs = 1;
                    node->ResetHWIntrinsicId(intrinsicId, compiler, node->Op(1));
                    break;
                }

                case 3:
                {
                    numArgs = 2;
                    node->ResetHWIntrinsicId(intrinsicId, compiler, node->Op(1), node->Op(2));
                    break;
                }

                default:
                {
                    unreached();
                }
            }

            if (lastOp->isContained())
            {
                assert(lastOp->IsCnsIntOrI());

                int8_t mode = static_cast<int8_t>(lastOp->AsIntCon()->IconValue());
                instOptions = AddEmbRoundingMode(instOptions, mode);
            }
            else
            {
                var_types baseType = node->GetSimdBaseType();

                instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
                assert(ins != INS_invalid);

                emitAttr simdSize = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->GetSimdSize()));
                assert(simdSize != 0);

                genConsumeMultiOpOperands(node);
                genConsumeRegs(lastOp);

                if(isTableDriven)
                {
                    switch (numArgs)
                    {
                        case 1:
                        {
                            regNumber targetReg = node->GetRegNum();
                            GenTree* rmOp = node->Op(1);
                            auto emitSwCase = [&](int8_t i) {
                                insOpts newInstOptions = AddEmbRoundingMode(instOptions, i);
                                genHWIntrinsic_R_RM(node, ins, simdSize, targetReg, rmOp, newInstOptions);
                            };
                            regNumber baseReg = node->ExtractTempReg();
                            regNumber offsReg = node->GetSingleTempReg();
                            genHWIntrinsicJumpTableFallback(intrinsicId, lastOp->GetRegNum(), baseReg, offsReg,
                                                            emitSwCase);
                            break;
                        }
                        case 2:
                        {
                            auto emitSwCase = [&](int8_t i) {
                                insOpts newInstOptions = AddEmbRoundingMode(instOptions, i);
                                genHWIntrinsic_R_R_RM(node, ins, simdSize, newInstOptions);
                            };
                            regNumber baseReg = node->ExtractTempReg();
                            regNumber offsReg = node->GetSingleTempReg();
                            genHWIntrinsicJumpTableFallback(intrinsicId, lastOp->GetRegNum(), baseReg, offsReg,
                                                            emitSwCase);
                            break;
                        }

                        default:
                        {
                            unreached();
                        }
                    }
                }
                else
                {
                    // There are a few embedded rounding intrinsics that need to be emitted with special handling.
                    genNonTableDrivenHWIntrinsicsJumpTableFallback(node, lastOp);
                }

                genProduceReg(node);
                return;
            }
        }
    }

    if (isTableDriven)
    {
        regNumber targetReg = node->GetRegNum();
        var_types baseType  = node->GetSimdBaseType();

        GenTree* op1 = nullptr;
        GenTree* op2 = nullptr;
        GenTree* op3 = nullptr;
        GenTree* op4 = nullptr;

        regNumber op1Reg = REG_NA;
        regNumber op2Reg = REG_NA;
        regNumber op3Reg = REG_NA;
        regNumber op4Reg = REG_NA;
        emitter*  emit   = GetEmitter();

        assert(numArgs >= 0);

        instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
        assert(ins != INS_invalid);

        emitAttr simdSize = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->GetSimdSize()));
        assert(simdSize != 0);

        int ival = HWIntrinsicInfo::lookupIval(compiler, intrinsicId, baseType);

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

                    if (ival != -1)
                    {
                        assert((ival >= 0) && (ival <= 127));
                        if (HWIntrinsicInfo::CopiesUpperBits(intrinsicId))
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
                    else if (HWIntrinsicInfo::CopiesUpperBits(intrinsicId))
                    {
                        assert(!op1->isContained());
                        emit->emitIns_SIMD_R_R_R(ins, simdSize, targetReg, op1Reg, op1Reg);
                    }
                    else
                    {
                        genHWIntrinsic_R_RM(node, ins, simdSize, targetReg, op1, instOptions);
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
                    genConsumeReg(op2);

                    // Until we improve the handling of addressing modes in the emitter, we'll create a
                    // temporary GT_STORE_IND to generate code with.

                    GenTreeStoreInd store = storeIndirForm(node->TypeGet(), op1, op2);
                    emit->emitInsStoreInd(ins, simdSize, &store);
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

                if (ival != -1)
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

                    assert(!node->isRMWHWIntrinsic(compiler));
                    inst_RV_RV_TT(ins, simdSize, targetReg, otherReg, &load, false, instOptions);
                }
                else if (HWIntrinsicInfo::isImmOp(intrinsicId, op2))
                {
                    auto emitSwCase = [&](int8_t i) {
                        if (HWIntrinsicInfo::CopiesUpperBits(intrinsicId))
                        {
                            assert(!op1->isContained());
                            emit->emitIns_SIMD_R_R_R_I(ins, simdSize, targetReg, op1Reg, op1Reg, i);
                        }
                        else
                        {
                            genHWIntrinsic_R_RM_I(node, ins, simdSize, i);
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
                        // We emit a fallback case for the scenario when the imm-op is not a constant.
                        // This should
                        // normally happen when the intrinsic is called indirectly, such as via
                        // Reflection. However, it
                        // can also occur if the consumer calls it directly and just doesn't pass a
                        // constant value.
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
                    genHWIntrinsic_R_R_RM(node, ins, simdSize, instOptions);
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
                op3Reg = op3->GetRegNum();

                assert(ival == -1);

                if (HWIntrinsicInfo::isImmOp(intrinsicId, op3))
                {
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
                        case NI_AVX512F_BlendVariableMask:
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

            case 4:
            {
                op1 = node->Op(1);
                op2 = node->Op(2);
                op3 = node->Op(3);
                op4 = node->Op(4);

                genConsumeRegs(op1);
                op1Reg = op1->GetRegNum();

                genConsumeRegs(op2);
                op2Reg = op2->GetRegNum();

                genConsumeRegs(op3);
                op3Reg = op3->GetRegNum();

                genConsumeRegs(op4);
                op4Reg = op4->GetRegNum();

                assert(ival == -1);

                if (HWIntrinsicInfo::isImmOp(intrinsicId, op4))
                {
                    auto emitSwCase = [&](int8_t i) { genHWIntrinsic_R_R_R_RM_I(node, ins, simdSize, i); };

                    if (op4->IsCnsIntOrI())
                    {
                        ssize_t ival = op4->AsIntCon()->IconValue();
                        assert((ival >= 0) && (ival <= 255));
                        emitSwCase(static_cast<int8_t>(ival));
                    }
                    else
                    {
                        // We emit a fallback case for the scenario when the imm-op is not a constant. This should
                        // normally happen when the intrinsic is called indirectly, such as via Reflection. However, it
                        // can also occur if the consumer calls it directly and just doesn't pass a constant value.
                        regNumber baseReg = node->ExtractTempReg();
                        regNumber offsReg = node->GetSingleTempReg();
                        genHWIntrinsicJumpTableFallback(intrinsicId, op4Reg, baseReg, offsReg, emitSwCase);
                    }
                }
                else
                {
                    unreached();
                }
                break;
            }

            default:
                unreached();
                break;
        }

        if (embMaskOp != nullptr)
        {
            // Handle an extra operand we need to consume so that
            // embedded masking can work without making the overall
            // logic significantly more complex.
            genConsumeReg(embMaskOp);
        }

        genProduceReg(node);
        return;
    }

    assert(embMaskOp == nullptr);

    switch (isa)
    {
        case InstructionSet_Vector128:
        case InstructionSet_Vector256:
        case InstructionSet_Vector512:
            genBaseIntrinsic(node);
            break;
        case InstructionSet_X86Base:
        case InstructionSet_X86Base_X64:
            genX86BaseIntrinsic(node);
            break;
        case InstructionSet_SSE:
        case InstructionSet_SSE_X64:
            genSSEIntrinsic(node, instOptions);
            break;
        case InstructionSet_SSE2:
        case InstructionSet_SSE2_X64:
            genSSE2Intrinsic(node, instOptions);
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
        case InstructionSet_AVX512F:
        case InstructionSet_AVX512F_VL:
        case InstructionSet_AVX512F_X64:
        case InstructionSet_AVX512BW:
        case InstructionSet_AVX512BW_VL:
        case InstructionSet_AVX512VBMI:
        case InstructionSet_AVX512VBMI_VL:
            genAvxFamilyIntrinsic(node, instOptions);
            break;
        case InstructionSet_AES:
            genAESIntrinsic(node);
            break;
        case InstructionSet_BMI1:
        case InstructionSet_BMI1_X64:
        case InstructionSet_BMI2:
        case InstructionSet_BMI2_X64:
            genBMI1OrBMI2Intrinsic(node, instOptions);
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
//    attr - The emit attribute for the instruction being generated
//    reg  - The register
//    rmOp - The register/memory operand node
//    instOptions - the existing intOpts
void CodeGen::genHWIntrinsic_R_RM(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr, regNumber reg, GenTree* rmOp, insOpts instOptions)
{
    emitter*    emit     = GetEmitter();
    OperandDesc rmOpDesc = genOperandDesc(rmOp);

    assert(reg != REG_NA);

    if ((instOptions & INS_OPTS_EVEX_b_MASK) != 0)
    {
        // As embedded rounding only appies in R_R_R case, we can skip other checks for different paths.
        assert(rmOpDesc.GetKind() == OperandKind::Reg);
        regNumber op1Reg = rmOp->GetRegNum();
        assert(op1Reg != REG_NA);

        emit->emitIns_R_R(ins, attr, reg, op1Reg, instOptions);
        return;
    }

    genHWIntrinsic_R_RM(node, ins, attr, reg, rmOp);
}

//------------------------------------------------------------------------
// genHWIntrinsic_R_RM: Generates code for a hardware intrinsic node that takes a
//                      register operand and a register/memory operand.
//
// Arguments:
//    node - The hardware intrinsic node
//    ins  - The instruction being generated
//    attr - The emit attribute for the instruction being generated
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
        {
            regNumber rmOpReg = rmOpDesc.GetReg();

            if (emit->IsMovInstruction(ins))
            {
                emit->emitIns_Mov(ins, attr, reg, rmOpReg, /* canSkip */ false);
            }
            else
            {
                if (varTypeIsIntegral(rmOp))
                {
                    bool needsBroadcastFixup   = false;
                    bool needsInstructionFixup = false;

                    switch (node->GetHWIntrinsicId())
                    {
                        case NI_AVX2_BroadcastScalarToVector128:
                        case NI_AVX2_BroadcastScalarToVector256:
                        {
                            if (varTypeIsSmall(node->GetSimdBaseType()))
                            {
                                if (compiler->compOpportunisticallyDependsOn(InstructionSet_AVX512BW_VL))
                                {
                                    needsInstructionFixup = true;
                                }
                                else
                                {
                                    needsBroadcastFixup = true;
                                }
                            }
                            else if (compiler->compOpportunisticallyDependsOn(InstructionSet_AVX512F_VL))
                            {
                                needsInstructionFixup = true;
                            }
                            else
                            {
                                needsBroadcastFixup = true;
                            }
                            break;
                        }

                        case NI_AVX512F_BroadcastScalarToVector512:
                        case NI_AVX512BW_BroadcastScalarToVector512:
                        {
                            needsInstructionFixup = true;
                            break;
                        }

                        default:
                        {
                            break;
                        }
                    }

                    if (needsBroadcastFixup)
                    {
                        // In lowering we had the special case of BroadcastScalarToVector(CreateScalarUnsafe(op1))
                        //
                        // This is one of the only instructions where it supports taking integer types from
                        // a SIMD register or directly as a scalar from memory. Most other instructions, in
                        // comparison, take such values from general-purpose registers instead.
                        //
                        // Because of this, we removed the CreateScalarUnsafe and tried to contain op1 directly
                        // that failed and we either didn't get marked regOptional or we did and didn't get spilled
                        //
                        // As such, we need to emulate the removed CreateScalarUnsafe to ensure that op1 is in a
                        // SIMD register so the broadcast instruction can execute succesfully. We'll just move
                        // the value into the target register and then broadcast it out from that.

                        emitAttr movdAttr = emitActualTypeSize(node->GetSimdBaseType());
                        emit->emitIns_Mov(INS_movd, movdAttr, reg, rmOpReg, /* canSkip */ false);
                        rmOpReg = reg;
                    }
                    else if (needsInstructionFixup)
                    {
                        switch (ins)
                        {
                            case INS_vpbroadcastb:
                            {
                                ins = INS_vpbroadcastb_gpr;
                                break;
                            }

                            case INS_vpbroadcastd:
                            {
                                ins = INS_vpbroadcastd_gpr;
                                break;
                            }

                            case INS_vpbroadcastq:
                            {
                                ins = INS_vpbroadcastq_gpr;
                                break;
                            }

                            case INS_vpbroadcastw:
                            {
                                ins = INS_vpbroadcastw_gpr;
                                break;
                            }

                            default:
                            {
                                unreached();
                            }
                        }
                    }
                }

                emit->emitIns_R_R(ins, attr, reg, rmOpReg);
            }
            break;
        }

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
//    node        - The hardware intrinsic node
//    ins         - The instruction being generated
//    attr        - The emit attribute for the instruction being generated
//    instOptions - The options that modify how the instruction is generated
//
void CodeGen::genHWIntrinsic_R_R_RM(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr, insOpts instOptions)
{
    regNumber targetReg = node->GetRegNum();
    GenTree*  op1       = node->Op(1);
    GenTree*  op2       = node->Op(2);
    regNumber op1Reg    = op1->GetRegNum();

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);

    if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->GetHWIntrinsicId()));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, op2);
    }

    bool isRMW = node->isRMWHWIntrinsic(compiler);
    inst_RV_RV_TT(ins, attr, targetReg, op1Reg, op2, isRMW, instOptions);
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
    regNumber op1Reg    = op1->GetRegNum();

    assert(targetReg != REG_NA);

    if (op2->isContained() || op2->isUsedFromSpillTemp())
    {
        assert(HWIntrinsicInfo::SupportsContainment(node->GetHWIntrinsicId()));
        assertIsContainableHWIntrinsicOp(compiler->m_pLowering, node, op2);
    }

    if (ins == INS_insertps)
    {
        if (op1Reg == REG_NA)
        {
            // insertps is special and can contain op1 when it is zero
            assert(op1->isContained() && op1->IsVectorZero());
            op1Reg = targetReg;
        }

        if (op2->isContained() && op2->IsVectorZero())
        {
            // insertps can also contain op2 when it is zero in which case
            // we just reuse op1Reg since ival specifies the entry to zero

            GetEmitter()->emitIns_SIMD_R_R_R_I(ins, simdSize, targetReg, op1Reg, op1Reg, ival);
            return;
        }
    }

    assert(op1Reg != REG_NA);

    bool isRMW = node->isRMWHWIntrinsic(compiler);
    inst_RV_RV_TT_IV(ins, simdSize, targetReg, op1Reg, op2, ival, isRMW);
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
            emit->emitIns_SIMD_R_R_R_R(ins, simdSize, targetReg, op1Reg, op2Desc.GetReg(), op3Reg);
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
            emit->emitIns_SIMD_R_R_R_R(ins, attr, targetReg, op1Reg, op2Reg, op3Desc.GetReg());
            break;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// genHWIntrinsic_R_R_R_RM_I: Generates the code for a hardware intrinsic node that takes two register operands,
//                          a register/memory operand, an immediate operand, and that returns a value in register
//
// Arguments:
//    node - The hardware intrinsic node
//    ins  - The instruction being generated
//    ival - The immediate value
//
void CodeGen::genHWIntrinsic_R_R_R_RM_I(GenTreeHWIntrinsic* node, instruction ins, emitAttr attr, int8_t ival)
{
    regNumber targetReg = node->GetRegNum();
    GenTree*  op1       = node->Op(1);
    GenTree*  op2       = node->Op(2);
    GenTree*  op3       = node->Op(3);
    regNumber op1Reg    = op1->GetRegNum();
    regNumber op2Reg    = op2->GetRegNum();

    if (op1->isContained())
    {
        // op1 is never selected by the table so
        // we can contain and ignore any register
        // allocated to it resulting in better
        // non-RMW based codegen.

        assert(!node->isRMWHWIntrinsic(compiler));
        op1Reg = targetReg;

        if (op2->isContained())
        {
// op2 is never selected by the table so
// we can contain and ignore any register
// allocated to it resulting in better
// non-RMW based codegen.

#if defined(DEBUG)
            NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();
            assert((intrinsicId == NI_AVX512F_TernaryLogic) || (intrinsicId == NI_AVX512F_VL_TernaryLogic));

            uint8_t                 control  = static_cast<uint8_t>(ival);
            const TernaryLogicInfo& info     = TernaryLogicInfo::lookup(control);
            TernaryLogicUseFlags    useFlags = info.GetAllUseFlags();

            assert(useFlags == TernaryLogicUseFlags::C);
#endif // DEBUG

            op2Reg = targetReg;
        }
    }

    assert(targetReg != REG_NA);
    assert(op1Reg != REG_NA);
    assert(op2Reg != REG_NA);

    emitter*    emit    = GetEmitter();
    OperandDesc op3Desc = genOperandDesc(op3);

    switch (op3Desc.GetKind())
    {
        case OperandKind::ClsVar:
        {
            emit->emitIns_SIMD_R_R_R_C_I(ins, attr, targetReg, op1Reg, op2Reg, op3Desc.GetFieldHnd(), 0, ival);
            break;
        }

        case OperandKind::Local:
        {
            emit->emitIns_SIMD_R_R_R_S_I(ins, attr, targetReg, op1Reg, op2Reg, op3Desc.GetVarNum(),
                                         op3Desc.GetLclOffset(), ival);
            break;
        }

        case OperandKind::Indir:
        {
            // Until we improve the handling of addressing modes in the emitter, we'll create a
            // temporary GT_IND to generate code with.
            GenTreeIndir  indirForm;
            GenTreeIndir* indir = op3Desc.GetIndirForm(&indirForm);
            emit->emitIns_SIMD_R_R_R_A_I(ins, attr, targetReg, op1Reg, op2Reg, indir, ival);
        }
        break;

        case OperandKind::Reg:
        {
            emit->emitIns_SIMD_R_R_R_R_I(ins, attr, targetReg, op1Reg, op2Reg, op3Desc.GetReg(), ival);
            break;
        }

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

void CodeGen::genNonTableDrivenHWIntrinsicsJumpTableFallback(GenTreeHWIntrinsic* node, GenTree* lastOp)
{
    NamedIntrinsic         intrinsicId = node->GetHWIntrinsicId();
    HWIntrinsicCategory    category    = HWIntrinsicInfo::lookupCategory(intrinsicId);
    
    assert(HWIntrinsicInfo::IsEmbRoundingCompatible(intrinsicId));
    assert(!lastOp->isContained());
    assert(!genIsTableDrivenHWIntrinsic(intrinsicId, category));

    var_types   baseType   = node->GetSimdBaseType();
    emitAttr    attr       = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->GetSimdSize()));
    var_types   targetType = node->TypeGet();
    instruction ins        = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
    regNumber   targetReg  = node->GetRegNum();

    insOpts instOptions   = INS_OPTS_NONE;
    switch (intrinsicId)
    {
        case NI_AVX512F_ConvertToVector256Int32:
        case NI_AVX512F_ConvertToVector256UInt32:
        {
            // This intrinsic has several overloads, only the ones with floating number inputs should reach this part.
            assert(varTypeIsFloating(baseType));
            auto emitSwCase = [&](int8_t i) {
                insOpts newInstOptions = AddEmbRoundingMode(instOptions, i);
                genHWIntrinsic_R_RM(node, ins, attr, targetReg, lastOp, newInstOptions);
                };
            regNumber baseReg = node->ExtractTempReg();
            regNumber offsReg = node->GetSingleTempReg();
            genHWIntrinsicJumpTableFallback(intrinsicId, lastOp->GetRegNum(), baseReg, offsReg,
                                            emitSwCase);
            break;
        }
        
        case NI_AVX512F_ConvertToInt32:
        case NI_AVX512F_ConvertToUInt32:
#if defined(TARGET_AMD64)
        case NI_AVX512F_X64_ConvertToInt64:
        case NI_AVX512F_X64_ConvertToUInt64:
#endif // TARGET_AMD64
        {
            assert(varTypeIsFloating(baseType));
            emitAttr attr = emitTypeSize(targetType);

            auto emitSwCase = [&](int8_t i) {
                insOpts newInstOptions = AddEmbRoundingMode(instOptions, i);
                genHWIntrinsic_R_RM(node, ins, attr, targetReg, lastOp, newInstOptions);
                };
            regNumber baseReg = node->ExtractTempReg();
            regNumber offsReg = node->GetSingleTempReg();
            genHWIntrinsicJumpTableFallback(intrinsicId, lastOp->GetRegNum(), baseReg, offsReg,
                                            emitSwCase);
            break;
        }

        case NI_AVX512F_X64_ConvertScalarToVector128Single:
        case NI_AVX512F_X64_ConvertScalarToVector128Double:
        {
            assert(varTypeIsLong(baseType));
            auto emitSwCase = [&](int8_t i) {
                insOpts newInstOptions = AddEmbRoundingMode(instOptions, i);
                genHWIntrinsic_R_R_RM(node, ins, EA_8BYTE, newInstOptions);
                };
            regNumber baseReg = node->ExtractTempReg();
            regNumber offsReg = node->GetSingleTempReg();
            genHWIntrinsicJumpTableFallback(intrinsicId, lastOp->GetRegNum(), baseReg, offsReg,
                                            emitSwCase);
            break;
        }

        default:
            unreached();
            break;
    }
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
        case NI_Vector512_CreateScalarUnsafe:
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
        case NI_Vector512_GetElement:
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

        case NI_Vector128_AsVector2:
        case NI_Vector128_AsVector3:
        case NI_Vector128_ToScalar:
        case NI_Vector256_ToScalar:
        case NI_Vector512_ToScalar:
        {
            if (op1->isContained() || op1->isUsedFromSpillTemp())
            {
                if (varTypeIsIntegral(baseType))
                {
                    // We just want to emit a standard read from memory
                    ins  = ins_Move_Extend(baseType, false);
                    attr = emitTypeSize(baseType);
                }
                genHWIntrinsic_R_RM(node, ins, attr, targetReg, op1);
            }
            else
            {
                assert(varTypeIsFloating(baseType));

                // Just use movaps for reg->reg moves as it has zero-latency on modern CPUs
                emit->emitIns_Mov(INS_movaps, attr, targetReg, op1Reg, /* canSkip */ true);
            }
            break;
        }

        case NI_Vector128_ToVector256:
        case NI_Vector128_ToVector512:
        case NI_Vector256_ToVector512:
        {
            // ToVector256 has zero-extend semantics in order to ensure it is deterministic
            // We always emit a move to the target register, even when op1Reg == targetReg,
            // in order to ensure that Bits MAXVL-1:128 are zeroed.

            if (intrinsicId == NI_Vector256_ToVector512)
            {
                attr = emitTypeSize(TYP_SIMD32);
            }
            else
            {
                attr = emitTypeSize(TYP_SIMD16);
            }

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
        case NI_Vector256_ToVector512Unsafe:
        case NI_Vector256_GetLower:
        case NI_Vector512_GetLower:
        case NI_Vector512_GetLower128:
        {
            if (op1->isContained() || op1->isUsedFromSpillTemp())
            {
                // We want to always emit the EA_16BYTE version here.
                //
                // For ToVector256Unsafe the upper bits don't matter and for GetLower we
                // only actually need the lower 16-bytes, so we can just be "more efficient"
                if ((intrinsicId == NI_Vector512_GetLower) || (intrinsicId == NI_Vector256_ToVector512Unsafe))
                {
                    attr = emitTypeSize(TYP_SIMD32);
                }
                else
                {
                    attr = emitTypeSize(TYP_SIMD16);
                }
                genHWIntrinsic_R_RM(node, ins, attr, targetReg, op1);
            }
            else
            {
                // We want to always emit the EA_32BYTE version here.
                //
                // For ToVector256Unsafe the upper bits don't matter and this allows same
                // register moves to be elided. For GetLower we're getting a Vector128 and
                // so the upper bits aren't impactful either allowing the same.

                // Just use movaps for reg->reg moves as it has zero-latency on modern CPUs
                if ((intrinsicId == NI_Vector128_ToVector256Unsafe) || (intrinsicId == NI_Vector256_GetLower))
                {
                    attr = emitTypeSize(TYP_SIMD32);
                }
                else
                {
                    attr = emitTypeSize(TYP_SIMD64);
                }
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

        case NI_X86Base_DivRem:
        case NI_X86Base_X64_DivRem:
        {
            assert(node->GetOperandCount() == 3);

            // SIMD base type is from signature and can distinguish signed and unsigned
            var_types   targetType = node->GetSimdBaseType();
            GenTree*    op1        = node->Op(1);
            GenTree*    op2        = node->Op(2);
            GenTree*    op3        = node->Op(3);
            instruction ins        = HWIntrinsicInfo::lookupIns(intrinsicId, targetType);

            regNumber op1Reg = op1->GetRegNum();
            regNumber op2Reg = op2->GetRegNum();
            regNumber op3Reg = op3->GetRegNum();

            emitAttr attr = emitTypeSize(targetType);
            emitter* emit = GetEmitter();

            // op1: EAX, op2: EDX, op3: free
            assert(op1Reg != REG_EDX);
            assert(op2Reg != REG_EAX);
            if (op3->isUsedFromReg())
            {
                assert(op3Reg != REG_EDX);
                assert(op3Reg != REG_EAX);
            }

            emit->emitIns_Mov(INS_mov, attr, REG_EAX, op1Reg, /* canSkip */ true);
            emit->emitIns_Mov(INS_mov, attr, REG_EDX, op2Reg, /* canSkip */ true);

            // emit the DIV/IDIV instruction
            emit->emitInsBinary(ins, attr, node, op3);

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
//    node        - The hardware intrinsic node
//    instOptions - The options used to when generating the instruction.
//
void CodeGen::genSSEIntrinsic(GenTreeHWIntrinsic* node, insOpts instOptions)
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
            genHWIntrinsic_R_R_RM(node, ins, EA_8BYTE, instOptions);
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
//    node        - The hardware intrinsic node
//    instOptions - The options used to when generating the instruction.
//
void CodeGen::genSSE2Intrinsic(GenTreeHWIntrinsic* node, insOpts instOptions)
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
            genHWIntrinsic_R_R_RM(node, ins, EA_8BYTE, instOptions);
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
// genAvxFamilyIntrinsic: Generates the code for an AVX/AVX2/AVX512 hardware intrinsic node
//
// Arguments:
//    node        - The hardware intrinsic node
//    instOptions - The options used to when generating the instruction.
//
void CodeGen::genAvxFamilyIntrinsic(GenTreeHWIntrinsic* node, insOpts instOptions)
{
    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();

    if (HWIntrinsicInfo::IsFmaIntrinsic(intrinsicId))
    {
        genFMAIntrinsic(node);
        return;
    }

    if (HWIntrinsicInfo::IsPermuteVar2x(intrinsicId))
    {
        genPermuteVar2x(node);
        return;
    }

    var_types   baseType   = node->GetSimdBaseType();
    emitAttr    attr       = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->GetSimdSize()));
    var_types   targetType = node->TypeGet();
    instruction ins        = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
    size_t      numArgs    = node->GetOperandCount();
    GenTree*    op1        = node->Op(1);
    regNumber   op1Reg     = REG_NA;
    regNumber   targetReg  = node->GetRegNum();
    emitter*    emit       = GetEmitter();

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
                assert(!emitter::isHighSimdReg(targetReg));
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

        case NI_AVX512F_AddMask:
        {
            uint32_t simdSize = node->GetSimdSize();
            uint32_t count    = simdSize / genTypeSize(baseType);

            if (count <= 8)
            {
                assert((count == 2) || (count == 4) || (count == 8));
                ins = INS_kaddb;
            }
            else if (count == 16)
            {
                ins = INS_kaddw;
            }
            else if (count == 32)
            {
                ins = INS_kaddd;
            }
            else
            {
                assert(count == 64);
                ins = INS_kaddq;
            }

            op1Reg = op1->GetRegNum();

            GenTree*  op2    = node->Op(2);
            regNumber op2Reg = op2->GetRegNum();

            assert(emitter::isMaskReg(targetReg));
            assert(emitter::isMaskReg(op1Reg));
            assert(emitter::isMaskReg(op2Reg));

            // Use EA_32BYTE to ensure the VEX.L bit gets set
            emit->emitIns_R_R_R(ins, EA_32BYTE, targetReg, op1Reg, op2Reg);
            break;
        }

        case NI_AVX512F_AndMask:
        {
            uint32_t simdSize = node->GetSimdSize();
            uint32_t count    = simdSize / genTypeSize(baseType);

            if (count <= 8)
            {
                assert((count == 2) || (count == 4) || (count == 8));
                ins = INS_kandb;
            }
            else if (count == 16)
            {
                ins = INS_kandw;
            }
            else if (count == 32)
            {
                ins = INS_kandd;
            }
            else
            {
                assert(count == 64);
                ins = INS_kandq;
            }

            op1Reg = op1->GetRegNum();

            GenTree*  op2    = node->Op(2);
            regNumber op2Reg = op2->GetRegNum();

            assert(emitter::isMaskReg(targetReg));
            assert(emitter::isMaskReg(op1Reg));
            assert(emitter::isMaskReg(op2Reg));

            // Use EA_32BYTE to ensure the VEX.L bit gets set
            emit->emitIns_R_R_R(ins, EA_32BYTE, targetReg, op1Reg, op2Reg);
            break;
        }

        case NI_AVX512F_AndNotMask:
        {
            uint32_t simdSize = node->GetSimdSize();
            uint32_t count    = simdSize / genTypeSize(baseType);

            if (count <= 8)
            {
                assert((count == 2) || (count == 4) || (count == 8));
                ins = INS_kandnb;
            }
            else if (count == 16)
            {
                ins = INS_kandnw;
            }
            else if (count == 32)
            {
                ins = INS_kandnd;
            }
            else
            {
                assert(count == 64);
                ins = INS_kandnq;
            }

            op1Reg = op1->GetRegNum();

            GenTree*  op2    = node->Op(2);
            regNumber op2Reg = op2->GetRegNum();

            assert(emitter::isMaskReg(targetReg));
            assert(emitter::isMaskReg(op1Reg));
            assert(emitter::isMaskReg(op2Reg));

            // Use EA_32BYTE to ensure the VEX.L bit gets set
            emit->emitIns_R_R_R(ins, EA_32BYTE, targetReg, op1Reg, op2Reg);
            break;
        }

        case NI_AVX512F_MoveMask:
        {
            uint32_t simdSize = node->GetSimdSize();
            uint32_t count    = simdSize / genTypeSize(baseType);

            if (count <= 8)
            {
                assert((count == 2) || (count == 4) || (count == 8));
                ins  = INS_kmovb_gpr;
                attr = EA_4BYTE;
            }
            else if (count == 16)
            {
                ins  = INS_kmovw_gpr;
                attr = EA_4BYTE;
            }
            else if (count == 32)
            {
                ins  = INS_kmovd_gpr;
                attr = EA_4BYTE;
            }
            else
            {
                assert(count == 64);
                ins  = INS_kmovq_gpr;
                attr = EA_8BYTE;
            }

            op1Reg = op1->GetRegNum();
            assert(emitter::isMaskReg(op1Reg));

            emit->emitIns_Mov(ins, attr, targetReg, op1Reg, INS_FLAGS_DONT_CARE);
            break;
        }

        case NI_AVX512F_KORTEST:
        {
            uint32_t simdSize = node->GetSimdSize();
            uint32_t count    = simdSize / genTypeSize(baseType);

            if (count <= 8)
            {
                assert((count == 2) || (count == 4) || (count == 8));
                ins = INS_kortestb;
            }
            else if (count == 16)
            {
                ins = INS_kortestw;
            }
            else if (count == 32)
            {
                ins = INS_kortestd;
            }
            else
            {
                assert(count == 64);
                ins = INS_kortestq;
            }

            op1Reg           = op1->GetRegNum();
            regNumber op2Reg = op1Reg;

            if (node->GetOperandCount() == 2)
            {
                GenTree* op2 = node->Op(2);
                op2Reg       = op2->GetRegNum();
            }

            assert(emitter::isMaskReg(op1Reg));
            assert(emitter::isMaskReg(op2Reg));

            emit->emitIns_R_R(ins, EA_8BYTE, op1Reg, op1Reg);
            break;
        }

        case NI_AVX512F_KTEST:
        {
            uint32_t simdSize = node->GetSimdSize();
            uint32_t count    = simdSize / genTypeSize(baseType);

            if (count <= 8)
            {
                assert((count == 2) || (count == 4) || (count == 8));
                ins = INS_ktestb;
            }
            else if (count == 16)
            {
                ins = INS_ktestw;
            }
            else if (count == 32)
            {
                ins = INS_ktestd;
            }
            else
            {
                assert(count == 64);
                ins = INS_ktestq;
            }

            op1Reg = op1->GetRegNum();

            GenTree*  op2    = node->Op(2);
            regNumber op2Reg = op2->GetRegNum();

            assert(emitter::isMaskReg(op1Reg));
            assert(emitter::isMaskReg(op2Reg));

            emit->emitIns_R_R(ins, EA_8BYTE, op1Reg, op1Reg);
            break;
        }

        case NI_AVX512F_NotMask:
        {
            uint32_t simdSize = node->GetSimdSize();
            uint32_t count    = simdSize / genTypeSize(baseType);

            if (count <= 8)
            {
                assert((count == 2) || (count == 4) || (count == 8));
                ins = INS_knotb;
            }
            else if (count == 16)
            {
                ins = INS_knotw;
            }
            else if (count == 32)
            {
                ins = INS_knotd;
            }
            else
            {
                assert(count == 64);
                ins = INS_knotq;
            }

            op1Reg = op1->GetRegNum();

            assert(emitter::isMaskReg(targetReg));
            assert(emitter::isMaskReg(op1Reg));

            emit->emitIns_R_R(ins, EA_8BYTE, targetReg, op1Reg);
            break;
        }

        case NI_AVX512F_OrMask:
        {
            uint32_t simdSize = node->GetSimdSize();
            uint32_t count    = simdSize / genTypeSize(baseType);

            if (count <= 8)
            {
                assert((count == 2) || (count == 4) || (count == 8));
                ins = INS_korb;
            }
            else if (count == 16)
            {
                ins = INS_korw;
            }
            else if (count == 32)
            {
                ins = INS_kord;
            }
            else
            {
                assert(count == 64);
                ins = INS_korq;
            }

            op1Reg = op1->GetRegNum();

            GenTree*  op2    = node->Op(2);
            regNumber op2Reg = op2->GetRegNum();

            assert(emitter::isMaskReg(targetReg));
            assert(emitter::isMaskReg(op1Reg));
            assert(emitter::isMaskReg(op2Reg));

            // Use EA_32BYTE to ensure the VEX.L bit gets set
            emit->emitIns_R_R_R(ins, EA_32BYTE, targetReg, op1Reg, op2Reg);
            break;
        }

        case NI_AVX512F_ShiftLeftMask:
        {
            uint32_t simdSize = node->GetSimdSize();
            uint32_t count    = simdSize / genTypeSize(baseType);

            if (count <= 8)
            {
                assert((count == 2) || (count == 4) || (count == 8));
                ins = INS_kshiftlb;
            }
            else if (count == 16)
            {
                ins = INS_kshiftlw;
            }
            else if (count == 32)
            {
                ins = INS_kshiftld;
            }
            else
            {
                assert(count == 64);
                ins = INS_kshiftlq;
            }

            op1Reg = op1->GetRegNum();

            GenTree* op2 = node->Op(2);
            assert(op2->IsCnsIntOrI() && op2->isContained());

            assert(emitter::isMaskReg(targetReg));
            assert(emitter::isMaskReg(op1Reg));

            ssize_t ival = op2->AsIntCon()->IconValue();
            assert((ival >= 0) && (ival <= 255));

            emit->emitIns_R_R_I(ins, EA_8BYTE, targetReg, op1Reg, (int8_t)ival);
            break;
        }

        case NI_AVX512F_ShiftRightMask:
        {
            uint32_t simdSize = node->GetSimdSize();
            uint32_t count    = simdSize / genTypeSize(baseType);

            if (count <= 8)
            {
                assert((count == 2) || (count == 4) || (count == 8));
                ins = INS_kshiftrb;
            }
            else if (count == 16)
            {
                ins = INS_kshiftrw;
            }
            else if (count == 32)
            {
                ins = INS_kshiftrd;
            }
            else
            {
                assert(count == 64);
                ins = INS_kshiftrq;
            }

            op1Reg = op1->GetRegNum();

            GenTree* op2 = node->Op(2);
            assert(op2->IsCnsIntOrI() && op2->isContained());

            assert(emitter::isMaskReg(targetReg));
            assert(emitter::isMaskReg(op1Reg));

            ssize_t ival = op2->AsIntCon()->IconValue();
            assert((ival >= 0) && (ival <= 255));

            emit->emitIns_R_R_I(ins, EA_8BYTE, targetReg, op1Reg, (int8_t)ival);
            break;
        }

        case NI_AVX512F_XorMask:
        {
            uint32_t simdSize = node->GetSimdSize();
            uint32_t count    = simdSize / genTypeSize(baseType);

            if (count <= 8)
            {
                assert((count == 2) || (count == 4) || (count == 8));
                ins = INS_kxorb;
            }
            else if (count == 16)
            {
                ins = INS_kxorw;
            }
            else if (count == 32)
            {
                ins = INS_kxord;
            }
            else
            {
                assert(count == 64);
                ins = INS_kxorq;
            }

            op1Reg = op1->GetRegNum();

            GenTree*  op2    = node->Op(2);
            regNumber op2Reg = op2->GetRegNum();

            assert(emitter::isMaskReg(targetReg));
            assert(emitter::isMaskReg(op1Reg));
            assert(emitter::isMaskReg(op2Reg));

            // Use EA_32BYTE to ensure the VEX.L bit gets set
            emit->emitIns_R_R_R(ins, EA_32BYTE, targetReg, op1Reg, op2Reg);
            break;
        }

        case NI_AVX512F_ConvertToInt32:
        case NI_AVX512F_ConvertToUInt32:
        case NI_AVX512F_ConvertToUInt32WithTruncation:
        case NI_AVX512F_X64_ConvertToInt64:
        case NI_AVX512F_X64_ConvertToUInt64:
        case NI_AVX512F_X64_ConvertToUInt64WithTruncation:
        {
            assert(baseType == TYP_DOUBLE || baseType == TYP_FLOAT);
            emitAttr attr = emitTypeSize(targetType);

            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            genHWIntrinsic_R_RM(node, ins, attr, targetReg, op1, instOptions);
            break;
        }

        case NI_AVX512F_ConvertToVector256Int32:
        case NI_AVX512F_ConvertToVector256UInt32:
        case NI_AVX512F_VL_ConvertToVector128UInt32:
        case NI_AVX512F_VL_ConvertToVector128UInt32WithSaturation:
        {
            if (varTypeIsFloating(baseType))
            {
                instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
                genHWIntrinsic_R_RM(node, ins, attr, targetReg, op1, instOptions);
                break;
            }
            FALLTHROUGH;
        }

        case NI_AVX512F_ConvertToVector128Byte:
        case NI_AVX512F_ConvertToVector128ByteWithSaturation:
        case NI_AVX512F_ConvertToVector128Int16:
        case NI_AVX512F_ConvertToVector128Int16WithSaturation:
        case NI_AVX512F_ConvertToVector128SByte:
        case NI_AVX512F_ConvertToVector128SByteWithSaturation:
        case NI_AVX512F_ConvertToVector128UInt16:
        case NI_AVX512F_ConvertToVector128UInt16WithSaturation:
        case NI_AVX512F_ConvertToVector256Int16:
        case NI_AVX512F_ConvertToVector256Int16WithSaturation:
        case NI_AVX512F_ConvertToVector256Int32WithSaturation:
        case NI_AVX512F_ConvertToVector256UInt16:
        case NI_AVX512F_ConvertToVector256UInt16WithSaturation:
        case NI_AVX512F_ConvertToVector256UInt32WithSaturation:
        case NI_AVX512F_VL_ConvertToVector128Byte:
        case NI_AVX512F_VL_ConvertToVector128ByteWithSaturation:
        case NI_AVX512F_VL_ConvertToVector128Int16:
        case NI_AVX512F_VL_ConvertToVector128Int16WithSaturation:
        case NI_AVX512F_VL_ConvertToVector128Int32:
        case NI_AVX512F_VL_ConvertToVector128Int32WithSaturation:
        case NI_AVX512F_VL_ConvertToVector128SByte:
        case NI_AVX512F_VL_ConvertToVector128SByteWithSaturation:
        case NI_AVX512F_VL_ConvertToVector128UInt16:
        case NI_AVX512F_VL_ConvertToVector128UInt16WithSaturation:
        case NI_AVX512BW_ConvertToVector256Byte:
        case NI_AVX512BW_ConvertToVector256ByteWithSaturation:
        case NI_AVX512BW_ConvertToVector256SByte:
        case NI_AVX512BW_ConvertToVector256SByteWithSaturation:
        case NI_AVX512BW_VL_ConvertToVector128Byte:
        case NI_AVX512BW_VL_ConvertToVector128ByteWithSaturation:
        case NI_AVX512BW_VL_ConvertToVector128SByte:
        case NI_AVX512BW_VL_ConvertToVector128SByteWithSaturation:
        {
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);

            // These instructions are RM_R and so we need to ensure the targetReg
            // is passed in as the RM register and op1 is passed as the R register

            op1Reg = op1->GetRegNum();
            emit->emitIns_R_R(ins, attr, op1Reg, targetReg);
            break;
        }

        case NI_AVX512F_X64_ConvertScalarToVector128Double:
        case NI_AVX512F_X64_ConvertScalarToVector128Single:
        {
            assert(baseType == TYP_ULONG || baseType == TYP_LONG);
            instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType);
            genHWIntrinsic_R_R_RM(node, ins, EA_8BYTE, instOptions);
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
//    node        - The hardware intrinsic node
//    instOptions - The options used to when generating the instruction.
//
void CodeGen::genBMI1OrBMI2Intrinsic(GenTreeHWIntrinsic* node, insOpts instOptions)
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
            genHWIntrinsic_R_R_RM(node, ins, emitTypeSize(node->TypeGet()), instOptions);
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
            assert(!node->isRMWHWIntrinsic(compiler));
            inst_RV_RV_TT(ins, attr, targetReg, lowReg, op2, false, INS_OPTS_NONE);

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
    assert(HWIntrinsicInfo::IsFmaIntrinsic(intrinsicId));

    var_types   baseType = node->GetSimdBaseType();
    emitAttr    attr     = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->GetSimdSize()));
    instruction _213form = HWIntrinsicInfo::lookupIns(intrinsicId, baseType); // 213 form
    instruction _132form = (instruction)(_213form - 1);
    instruction _231form = (instruction)(_213form + 1);
    GenTree*    op1      = node->Op(1);
    GenTree*    op2      = node->Op(2);
    GenTree*    op3      = node->Op(3);

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
// genPermuteVar2x: Generates the code for a PermuteVar2x hardware intrinsic node
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genPermuteVar2x(GenTreeHWIntrinsic* node)
{
    NamedIntrinsic intrinsicId = node->GetHWIntrinsicId();
    assert(HWIntrinsicInfo::IsPermuteVar2x(intrinsicId));

    var_types baseType = node->GetSimdBaseType();
    emitAttr  attr     = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->GetSimdSize()));
    GenTree*  op1      = node->Op(1);
    GenTree*  op2      = node->Op(2);
    GenTree*  op3      = node->Op(3);

    regNumber targetReg = node->GetRegNum();

    genConsumeMultiOpOperands(node);

    regNumber op1NodeReg = op1->GetRegNum();
    regNumber op2NodeReg = op2->GetRegNum();
    regNumber op3NodeReg = op3->GetRegNum();

    GenTree* emitOp1 = op1;
    GenTree* emitOp2 = op2;
    GenTree* emitOp3 = op3;

    // We need to keep this in sync with lsraxarch.cpp
    // Ideally we'd actually swap the operands in lsra and simplify codegen
    // but its a bit more complicated to do so for many operands as well
    // as being complicated to tell codegen how to pick the right instruction

    assert(!op1->isContained());
    assert(!op2->isContained());

    instruction ins = HWIntrinsicInfo::lookupIns(intrinsicId, baseType); // vpermt2

    if (targetReg == op2NodeReg)
    {
        std::swap(emitOp1, emitOp2);

        switch (ins)
        {
            case INS_vpermt2b:
            {
                ins = INS_vpermi2b;
                break;
            }

            case INS_vpermt2d:
            {
                ins = INS_vpermi2d;
                break;
            }

            case INS_vpermt2pd:
            {
                ins = INS_vpermi2pd;
                break;
            }

            case INS_vpermt2ps:
            {
                ins = INS_vpermi2ps;
                break;
            }

            case INS_vpermt2q:
            {
                ins = INS_vpermi2q;
                break;
            }

            case INS_vpermt2w:
            {
                ins = INS_vpermi2w;
                break;
            }

            default:
            {
                unreached();
            }
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
