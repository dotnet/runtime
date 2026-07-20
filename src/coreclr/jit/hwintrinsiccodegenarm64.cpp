// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef FEATURE_HW_INTRINSICS

#include "codegen.h"

// HWIntrinsicImmOpHelper: constructs the helper class instance.
//       This also determines what type of "switch" table is being used (if an immediate operand is not constant) and do
//       some preparation work:
//
//       a) If an immediate operand can be either 0 or 1, this creates <nonZeroLabel>.
//
//       b) If an immediate operand can take any value in [0, upperBound), this extract a internal register from an
//       intrinsic node. The register will be later used to store computed branch target address.
//
// Arguments:
//    codeGen   -- an instance of CodeGen class.
//    immOp     -- an immediate operand of the intrinsic.
//    intrin    -- a hardware intrinsic tree node.
//    numInstrs -- number of instructions that will be in each switch entry. Default 1.
//
// Note: This class is designed to be used in the following way
//       HWIntrinsicImmOpHelper helper(this, immOp, intrin);
//
//       for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
//       {
//         -- emit an instruction for a given value of helper.ImmValue()
//       }
//
//       This allows to combine logic for cases when immOp->isContainedIntOrIImmed() is either true or false in a form
//       of a for-loop.
//
CodeGen::HWIntrinsicImmOpHelper::HWIntrinsicImmOpHelper(CodeGen*            codeGen,
                                                        GenTree*            immOp,
                                                        GenTreeHWIntrinsic* intrin,
                                                        int                 numInstrs)
    : codeGen(codeGen)
    , endLabel(nullptr)
    , nonZeroLabel(nullptr)
    , branchTargetReg(REG_NA)
    , numInstrs(numInstrs)
{
    assert(codeGen != nullptr);
    assert(varTypeIsIntegral(immOp));

    if (immOp->isContainedIntOrIImmed())
    {
        nonConstImmReg = REG_NA;

        immValue      = (int)immOp->AsIntCon()->IconValue();
        immLowerBound = immValue;
        immUpperBound = immValue;
    }
    else
    {
        const HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(intrin->GetHWIntrinsicId());

        if (category == HW_Category_SIMDByIndexedElement)
        {
            const HWIntrinsic intrinInfo(intrin);
            var_types         indexedElementOpType;

            if (intrinInfo.numOperands == 2)
            {
                indexedElementOpType = intrinInfo.op1->TypeGet();
            }
            else if (intrinInfo.numOperands == 3)
            {
                indexedElementOpType = intrinInfo.op2->TypeGet();
            }
            else
            {
                assert(intrinInfo.numOperands == 4);
                indexedElementOpType = intrinInfo.op3->TypeGet();
            }

            assert(varTypeIsSIMD(indexedElementOpType));

            const unsigned int indexedElementSimdSize = genTypeSize(indexedElementOpType);
            HWIntrinsicInfo::lookupImmBounds(intrin->GetHWIntrinsicId(), indexedElementSimdSize,
                                             intrin->GetSimdBaseType(), 1, &immLowerBound, &immUpperBound);
        }
        else
        {
            HWIntrinsicInfo::lookupImmBounds(intrin->GetHWIntrinsicId(), intrin->GetSimdSize(),
                                             intrin->GetSimdBaseType(), 1, &immLowerBound, &immUpperBound);
        }

        nonConstImmReg = immOp->GetRegNum();
        immValue       = immLowerBound;

        if (TestImmOpZeroOrOne())
        {
            nonZeroLabel = codeGen->genCreateTempLabel();
        }
        else
        {
            // At the moment, this helper supports only intrinsics that correspond to one machine instruction.
            // If we ever encounter an intrinsic that is either lowered into multiple instructions or
            // the number of instructions that correspond to each case is unknown apriori - we can extend support to
            // these by
            // using the same approach as in hwintrinsicxarch.cpp - adding an additional indirection level in form of a
            // branch table.
            branchTargetReg = codeGen->internalRegisters.GetSingle(intrin);
        }

        endLabel = codeGen->genCreateTempLabel();
    }
}

// HWIntrinsicImmOpHelper: Variant constructor of the helper class instance.
//       This is used when the immediate does not exist in a GenTree. For example, the immediate has been created
//       during codegen from other immediate values.
//
// Arguments:
//    codeGen       -- an instance of CodeGen class.
//    immReg        -- the register containing the immediate.
//    immLowerBound -- the lower bound of the register.
//    immUpperBound -- the lower bound of the register.
//    intrin        -- a hardware intrinsic tree node.
//
// Note: This instance is designed to be used via the same for loop as the standard constructor.
//
CodeGen::HWIntrinsicImmOpHelper::HWIntrinsicImmOpHelper(
    CodeGen* codeGen, regNumber immReg, int immLowerBound, int immUpperBound, GenTreeHWIntrinsic* intrin, int numInstrs)
    : codeGen(codeGen)
    , endLabel(nullptr)
    , nonZeroLabel(nullptr)
    , immValue(immLowerBound)
    , immLowerBound(immLowerBound)
    , immUpperBound(immUpperBound)
    , nonConstImmReg(immReg)
    , branchTargetReg(REG_NA)
    , numInstrs(numInstrs)
{
    assert(codeGen != nullptr);

    if (TestImmOpZeroOrOne())
    {
        nonZeroLabel = codeGen->genCreateTempLabel();
    }
    else
    {
        // At the moment, this helper supports only intrinsics that correspond to one machine instruction.
        // If we ever encounter an intrinsic that is either lowered into multiple instructions or
        // the number of instructions that correspond to each case is unknown apriori - we can extend support to
        // these by
        // using the same approach as in hwintrinsicxarch.cpp - adding an additional indirection level in form of a
        // branch table.
        branchTargetReg = codeGen->internalRegisters.GetSingle(intrin);
    }

    endLabel = codeGen->genCreateTempLabel();
}

//------------------------------------------------------------------------
// EmitBegin: emits the beginning of a "switch" table, no-op if an immediate operand is constant.
//
// Note: The function is called at the beginning of code generation and emits
//    a) If an immediate operand can be either 0 or 1
//
//       cbnz <nonZeroLabel>, nonConstImmReg
//
//    b) If an immediate operand can take any value in [0, upperBound) range
//
//       adr branchTargetReg, <beginLabel>
//       add branchTargetReg, branchTargetReg, nonConstImmReg, lsl #3
//       br  branchTargetReg
//
//       When an immediate operand is non constant this also defines <beginLabel> right after the emitted code.
//
void CodeGen::HWIntrinsicImmOpHelper::EmitBegin()
{
    if (NonConstImmOp())
    {
        BasicBlock* beginLabel = codeGen->genCreateTempLabel();

        if (TestImmOpZeroOrOne())
        {
            GetEmitter()->emitIns_J_R(INS_cbnz, EA_4BYTE, nonZeroLabel, nonConstImmReg);
        }
        else
        {
            assert(numInstrs == 1 || numInstrs == 2);

            // Here we assume that each case consists of numInstrs arm64 instructions followed by "b endLabel".
            // Since an arm64 instruction is 4 bytes, we branch to AddressOf(beginLabel) + (nonConstImmReg << 3).
            GetEmitter()->emitIns_R_L(INS_adr, EA_8BYTE, beginLabel, branchTargetReg);
            GetEmitter()->emitIns_R_R_R_I(INS_add, EA_8BYTE, branchTargetReg, branchTargetReg, nonConstImmReg, 3,
                                          INS_OPTS_LSL);

            // For two instructions, add the extra one.
            if (numInstrs == 2)
            {
                GetEmitter()->emitIns_R_R_R_I(INS_add, EA_8BYTE, branchTargetReg, branchTargetReg, nonConstImmReg, 2,
                                              INS_OPTS_LSL);
            }

            // If the lower bound is non zero we need to adjust the branch target value by subtracting
            // the lower bound
            if (immLowerBound != 0)
            {
                ssize_t lowerReduce = ((ssize_t)immLowerBound << 3);
                if (numInstrs == 2)
                {
                    lowerReduce += ((ssize_t)immLowerBound << 2);
                }

                GetEmitter()->emitIns_R_R_I(INS_sub, EA_8BYTE, branchTargetReg, branchTargetReg, lowerReduce);
            }

            GetEmitter()->emitIns_R(INS_br, EA_8BYTE, branchTargetReg);
        }

        codeGen->genDefineInlineTempLabel(beginLabel);
    }
}

//------------------------------------------------------------------------
// EmitCaseEnd: emits the end of a "case", no-op if an immediate operand is constant.
//
// Note: The function is called at the end of each "case" (i.e. after an instruction has been emitted for a given
// immediate value ImmValue())
//       and emits
//
//       b <endLabel>
//
//       After the last "case" this defines <endLabel>.
//
//       If an immediate operand is either 0 or 1 it also defines <nonZeroLabel> after the first "case".
//
void CodeGen::HWIntrinsicImmOpHelper::EmitCaseEnd()
{
    assert(!Done());

    if (NonConstImmOp())
    {
        const bool isLastCase = (immValue == immUpperBound);

        if (isLastCase)
        {
            codeGen->genDefineInlineTempLabel(endLabel);
        }
        else
        {
            GetEmitter()->emitIns_J(INS_b, endLabel);

            if (TestImmOpZeroOrOne())
            {
                codeGen->genDefineInlineTempLabel(nonZeroLabel);
            }
            else
            {
                BasicBlock* tempLabel = codeGen->genCreateTempLabel();
                codeGen->genDefineInlineTempLabel(tempLabel);
            }
        }
    }

    immValue++;
}

//------------------------------------------------------------------------
// Emit helper for SVE+SVE2 WHILE* intrinsics - these set the width
// (emitSize) based on the scalar operand
//
static void genEmitCreateWhileMask(emitter*            emit,
                                   GenTreeHWIntrinsic* node,
                                   instruction         ins,
                                   instruction         unsignedIns,
                                   regNumber           targetReg,
                                   regNumber           op1Reg,
                                   regNumber           op2Reg,
                                   insOpts             opt)
{
    var_types auxType  = node->GetAuxiliaryType();
    emitAttr  emitSize = emitActualTypeSize(auxType);
    if (varTypeIsUnsigned(auxType))
    {
        ins = unsignedIns;
    }
    emit->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
}

//------------------------------------------------------------------------
// genEmbeddedMaskedHWIntrinsic: Generates the code for an embedded masked hardware intrinsic.
//
// Arguments:
//    cndSelNode -- the conditional select HWIntrinsic node.
//    targetReg  -- the target register of the HWIntrinsic node.
//
void CodeGen::genEmbeddedMaskedHWIntrinsic(GenTreeHWIntrinsic* cndSelNode, regNumber targetReg)
{
    const HWIntrinsic intrinCndSel(cndSelNode);
    assert(intrinCndSel.id == NI_Sve_ConditionalSelect);

    GenTree* maskOp    = intrinCndSel.op1;
    GenTree* embMaskOp = intrinCndSel.op2;
    GenTree* falseOp   = intrinCndSel.op3;

    assert(embMaskOp->OperIsHWIntrinsic());
    assert(embMaskOp->isContained());
    assert(embMaskOp->IsEmbMaskOp());

    const HWIntrinsic intrinEmbMask(embMaskOp->AsHWIntrinsic());
    instruction       insEmbMask = HWIntrinsicInfo::lookupIns(intrinEmbMask.id, intrinEmbMask.baseType, m_compiler);

    const bool isRMW             = embMaskOp->isRMWHWIntrinsic(m_compiler);
    bool       isOptionalEmbMask = HWIntrinsicInfo::IsOptionalEmbeddedMaskedOperation(intrinEmbMask.id);

    regNumber maskReg       = maskOp->GetRegNum();
    regNumber embMaskOp1Reg = REG_NA;
    regNumber embMaskOp2Reg = REG_NA;
    regNumber embMaskOp3Reg = REG_NA;
    regNumber embMaskOp4Reg = REG_NA;
    regNumber falseReg      = falseOp->GetRegNum();
    regNumber tempReg       = REG_NA;

    switch (intrinEmbMask.numOperands)
    {
        case 4:
            assert(intrinEmbMask.op4 != nullptr);
            embMaskOp4Reg = intrinEmbMask.op4->GetRegNum();
            FALLTHROUGH;

        case 3:
            assert(intrinEmbMask.op3 != nullptr);
            embMaskOp3Reg = intrinEmbMask.op3->GetRegNum();
            FALLTHROUGH;

        case 2:
            assert(intrinEmbMask.op2 != nullptr);
            embMaskOp2Reg = intrinEmbMask.op2->GetRegNum();
            FALLTHROUGH;

        case 1:
            assert(intrinEmbMask.op1 != nullptr);
            embMaskOp1Reg = intrinEmbMask.op1->GetRegNum();
            break;

        default:
            unreached();
    }

    if (intrinEmbMask.id == NI_Sve_MultiplyAddRotateComplex)
    {
        assert(intrinEmbMask.numOperands == 4);
        tempReg = internalRegisters.GetSingle(cndSelNode, RBM_ALLFLOAT);
    }

    emitAttr        emitSize = EA_SCALABLE;
    insOpts         opt      = emitter::optGetSveInsOpt(emitTypeSize(intrinCndSel.baseType));
    insOpts         embOpt   = opt;
    insScalableOpts sopt     = INS_SCALABLE_OPTS_NONE;

#ifdef DEBUG
    if (isRMW)
    {
        checkRMWRegisters(intrinEmbMask, targetReg);
    }
#endif

    // Setup instruction options and handle special cases.
    if (intrinEmbMask.numOperands == 1)
    {
        assert(!isRMW);

        if (HWIntrinsicInfo::IsReduceOperation(intrinEmbMask.id))
        {
            // For reduce operations, targetReg will always be overwritten by the scalar result.
            // So falseReg can be ignored and just perform the operation.
            GetEmitter()->emitInsSve_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg, embOpt);
            return;
        }

        switch (intrinEmbMask.id)
        {
            case NI_Sve2_ConvertToDoubleOdd:
                // This instruction does not support movprfx, so use conditional select instead.
                embOpt = emitTypeSize(intrinEmbMask.baseType) == EA_4BYTE ? INS_OPTS_S_TO_D : INS_OPTS_SCALABLE_D;
                if (!maskOp->IsTrueMask(intrinCndSel.baseType) && (targetReg != falseReg))
                {
                    // Move falseReg to the inactive lanes of targetReg
                    // if mask is not all-true and falseReg is not the same as targetReg.
                    assert(!falseOp->isContained());
                    GetEmitter()->emitIns_R_R_R_R(INS_sve_sel, emitSize, targetReg, maskReg, targetReg, falseReg, opt);
                }
                GetEmitter()->emitInsSve_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg, embOpt, sopt);
                return;

            case NI_Sve_ConvertToInt32:
            case NI_Sve_ConvertToUInt32:
            case NI_Sve_ConvertToSingle:
            case NI_Sve2_ConvertToSingleEvenRoundToOdd:
                embOpt = emitTypeSize(intrinEmbMask.baseType) == EA_8BYTE ? INS_OPTS_D_TO_S : INS_OPTS_SCALABLE_S;
                break;

            case NI_Sve_ConvertToInt64:
            case NI_Sve_ConvertToUInt64:
            case NI_Sve_ConvertToDouble:
                embOpt = emitTypeSize(intrinEmbMask.baseType) == EA_4BYTE ? INS_OPTS_S_TO_D : INS_OPTS_SCALABLE_D;
                break;

            default:
                break;
        }

        if (targetReg == falseReg)
        {
            // targetReg == falseReg: Just perform the masked operation.
            GetEmitter()->emitIns_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg, embOpt);
        }
        else
        {
            // targetReg != falseReg: Move falseReg into targetReg.
            if (falseOp->isContained())
            {
                assert(falseOp->IsVectorZero());
                if (maskOp->IsTrueMask(intrinCndSel.baseType))
                {
                    // If maskOp is all-true, no need to move falseReg to targetReg
                    // because the predicated instruction will eventually set it.
                    GetEmitter()->emitIns_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg, embOpt);
                }
                else
                {
                    // If falseValue is zero, just zero out those lanes of targetReg using zeroing movprfx.
                    GetEmitter()->emitInsSve_R_R_R_R(insEmbMask, emitSize, targetReg, maskReg, targetReg, embMaskOp1Reg,
                                                     embOpt, sopt, INS_SVE_MOV_OPTS_ZEROING);
                }
            }
            else if (emitter::isVectorRegister(embMaskOp1Reg) && (targetReg == embMaskOp1Reg))
            {
                // We cannot use use `movprfx` here to move falseReg to targetReg because that will
                // overwrite the value of embMaskOp1Reg which is present in targetReg.
                GetEmitter()->emitIns_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg, embOpt);
                GetEmitter()->emitIns_R_R_R_R(INS_sve_sel, emitSize, targetReg, maskReg, targetReg, falseReg, opt);
            }
            else
            {
                // targetReg != embMaskOp1Reg != falseReg: Move falseReg unpredicated into targetReg.
                GetEmitter()->emitInsSve_R_R_R_R(insEmbMask, emitSize, targetReg, maskReg, falseReg, embMaskOp1Reg,
                                                 embOpt, sopt, INS_SVE_MOV_OPTS_UNPRED);
            }
        }
        return;
    }
    else if (intrinEmbMask.numOperands == 2)
    {
        switch (intrinEmbMask.id)
        {
            case NI_Sve_CreateBreakPropagateMask:
            {
                embOpt = INS_OPTS_SCALABLE_B;
                // This instruction is zeroing predicated, just use unpredicated mov.
                assert(falseOp->IsVectorZero());
                GetEmitter()->emitInsSve_R_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg, embMaskOp2Reg,
                                                 embOpt, sopt);
                return;
            }

            case NI_Sve_AddSequentialAcross:
            {
                // Predicate functionality is currently not exposed for this API,
                // but the FADDA instruction only has a predicated variant.
                // Thus, we expect the JIT to wrap this with CndSel.
                assert(falseOp->IsVectorZero());
                GetEmitter()->emitInsSve_R_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg, embMaskOp2Reg,
                                                 embOpt, sopt);
                return;
            }

            case NI_Sve2_ConvertToSingleOdd:
            case NI_Sve2_ConvertToSingleOddRoundToOdd:
            {
                // These instructions do not support movprfx.
                embOpt = INS_OPTS_D_TO_S;
                if (falseOp->IsVectorZero() && !maskOp->IsTrueMask(intrinCndSel.baseType) && (targetReg == falseReg))
                {
                    GetEmitter()->emitIns_R_R_R(INS_sve_mov, emitSize, targetReg, maskReg, embMaskOp1Reg, opt,
                                                INS_SCALABLE_OPTS_PREDICATE_MERGE);
                    GetEmitter()->emitInsSve_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp2Reg, embOpt,
                                                   sopt);
                    return;
                }
                FALLTHROUGH;
            }

            case NI_Sve2_AddPairwise:
            case NI_Sve2_MaxNumberPairwise:
            case NI_Sve2_MaxPairwise:
            case NI_Sve2_MinNumberPairwise:
            case NI_Sve2_MinPairwise:
            {
                // These instructions have unpredictable behaviour when using predicated movprfx.
                // Move embMaskOp1Reg to targetReg unpredicated.
                GetEmitter()->emitInsSve_R_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg, embMaskOp2Reg,
                                                 embOpt, sopt);
                if (!maskOp->IsTrueMask(intrinCndSel.baseType) && (targetReg != falseReg))
                {
                    // Use conditional select to move falseReg to the inactive lanes of targetReg if necessary.
                    assert(!falseOp->isContained());
                    GetEmitter()->emitInsSve_R_R_R_R(INS_sve_sel, emitSize, targetReg, maskReg, targetReg, falseReg,
                                                     opt);
                }
                return;
            }

            case NI_Sve2_AddSaturate:
            {
                var_types baseType = embMaskOp->AsHWIntrinsic()->GetSimdBaseType();
                var_types auxType  = embMaskOp->AsHWIntrinsic()->GetAuxiliaryType();
                if (baseType != auxType)
                {
                    insEmbMask = (varTypeIsUnsigned(baseType)) ? INS_sve_usqadd : INS_sve_suqadd;
                    // SUQADD and USQADD must be predicated.
                    isOptionalEmbMask = false;
                }
                else
                {
                    // SQADD and UQADD can be unpredicated.
                    isOptionalEmbMask = true;
                }
                break;
            }

            case NI_Sve_ShiftLeftLogical:
            case NI_Sve_ShiftRightArithmetic:
            case NI_Sve_ShiftRightLogical:
            {
                const emitAttr op2Size = emitTypeSize(embMaskOp->AsHWIntrinsic()->GetAuxiliaryType());
                if (op2Size != emitTypeSize(intrinEmbMask.baseType))
                {
                    assert(emitter::optGetSveInsOpt(op2Size) == INS_OPTS_SCALABLE_D);
                    sopt = INS_SCALABLE_OPTS_WIDE;
                }
                break;
            }

            default:
                break;
        }

        if (!isRMW)
        {
            // Perform the actual "predicated" operation so that `embMaskOp1Reg` is the first operand..
            switch (intrinEmbMask.id)
            {
                case NI_Sve_And_Predicates:
                case NI_Sve_BitwiseClear_Predicates:
                case NI_Sve_Or_Predicates:
                case NI_Sve_Xor_Predicates:
                    embOpt = INS_OPTS_SCALABLE_B;
                    break;

                default:
                    break;
            }

            GetEmitter()->emitIns_R_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg, embMaskOp2Reg,
                                          embOpt);
            return;
        }
        else if (isOptionalEmbMask)
        {
            if (maskOp->IsTrueMask(intrinEmbMask.baseType) ||
                (!falseOp->IsVectorZero() && (targetReg != falseReg) && (falseReg != embMaskOp1Reg)))
            {
                // If the embedded instruction supports optional mask operation, and when movprfx is not needed,
                // use the "unpredicated" version of the instruction.
                if (HWIntrinsicInfo::HasImmediateOperand(intrinEmbMask.id))
                {
                    HWIntrinsicImmOpHelper helper(this, intrinEmbMask.op2, embMaskOp->AsHWIntrinsic());
                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        GetEmitter()->emitInsSve_R_R_I(insEmbMask, emitSize, targetReg, embMaskOp1Reg,
                                                       helper.ImmValue(), embOpt, sopt);
                    }
                }
                else
                {
                    GetEmitter()->emitIns_R_R_R(insEmbMask, emitSize, targetReg, embMaskOp1Reg, embMaskOp2Reg, embOpt,
                                                sopt);
                }

                if (!maskOp->IsTrueMask(intrinCndSel.baseType))
                {
                    // Use "sel" to select the active lanes if mask is not all-true.
                    GetEmitter()->emitIns_R_R_R_R(INS_sve_sel, emitSize, targetReg, maskReg, targetReg, falseReg, opt);
                }
                return;
            }
        }
    }
    else if (HWIntrinsicInfo::IsFmaIntrinsic(intrinEmbMask.id) && (intrinEmbMask.numOperands == 3))
    {
        // For FMA, the operation we are trying to perform is:
        //      result = op1 + (op2 * op3)
        //
        // There are two instructions that can be used depending on which operand's register,
        // optionally, will store the final result.
        //
        // 1. If the result is stored in the operand that was used as an "addend" in the operation,
        // then we use `FMLA` format:
        //      reg1 = reg1 + (reg2 * reg3)
        //
        // 2. If the result is stored in the operand that was used as a "multiplicand" in the operation,
        // then we use `FMAD` format:
        //      reg1 = (reg1 * reg2) + reg3
        //
        // Check if the result's register is same as that of one of the operand's register and
        // accordingly pick the appropriate format. Suppose `targetReg` holds the result, then we have
        // following cases:
        //
        // Case# 1: Result is stored in the operand that held the "addend"
        //      targetReg == reg1
        //
        // We generate the FMLA instruction format and no further changes are needed.
        //
        // Case# 2: Result is stored in the operand `op2` that held the "multiplicand"
        //      targetReg == reg2
        //
        // So we basically have an operation:
        //      reg2 = reg1 + (reg2 * reg3)
        //
        // Since, the result will be stored in the "multiplicand", we pick format `FMAD`.
        // Then, we rearrange the operands to ensure that the operation is done correctly.
        //      reg2 = reg1 + (reg2 * reg3)  // to start with
        //      reg2 = reg3 + (reg2 * reg1)  // swap reg1 <--> reg3
        //      reg1 = reg3 + (reg1 * reg2)  // swap reg1 <--> reg2
        //      reg1 = (reg1 * reg2) + reg3  // rearrange to get FMAD format
        //
        // Case# 3: Result is stored in the operand `op3` that held the "multiplier"
        //      targetReg == reg3
        //
        // So we basically have an operation:
        //      reg3 = reg1 + (reg2 * reg3)
        // Since, the result will be stored in the "multiplier", we again pick format `FMAD`.
        // Then, we rearrange the operands to ensure that the operation is done correctly.
        //      reg3 = reg1 + (reg2 * reg3)  // to start with
        //      reg1 = reg3 + (reg2 * reg1)  // swap reg1 <--> reg3
        //      reg1 = (reg1 * reg2) + reg3  // rearrange to get FMAD format
        bool useAddend = true;
        if (targetReg == embMaskOp2Reg)
        {
            // Case# 2
            useAddend = false;
            std::swap(embMaskOp1Reg, embMaskOp3Reg);
            std::swap(embMaskOp1Reg, embMaskOp2Reg);
        }
        else if (targetReg == embMaskOp3Reg)
        {
            // Case# 3
            useAddend = false;
            std::swap(embMaskOp1Reg, embMaskOp3Reg);
        }
        else
        {
            // Case# 1
        }
        switch (intrinEmbMask.id)
        {
            case NI_Sve_FusedMultiplyAdd:
                insEmbMask = useAddend ? INS_sve_fmla : INS_sve_fmad;
                break;
            case NI_Sve_FusedMultiplyAddNegated:
                insEmbMask = useAddend ? INS_sve_fnmla : INS_sve_fnmad;
                break;
            case NI_Sve_FusedMultiplySubtract:
                insEmbMask = useAddend ? INS_sve_fmls : INS_sve_fmsb;
                break;
            case NI_Sve_FusedMultiplySubtractNegated:
                insEmbMask = useAddend ? INS_sve_fnmls : INS_sve_fnmsb;
                break;
            case NI_Sve_MultiplyAdd:
                insEmbMask = useAddend ? INS_sve_mla : INS_sve_mad;
                break;
            case NI_Sve_MultiplySubtract:
                insEmbMask = useAddend ? INS_sve_mls : INS_sve_msb;
                break;
            default:
                unreached();
        }
    }

    // Determine the move option, based on the register usage.
    insSveMovOpts mopt = INS_SVE_MOV_OPTS_UNPRED;
    if (falseOp->IsVectorZero())
    {
        // If `falseReg` is zero, then move the first operand of `intrinEmbMask` in the
        // destination using /Z.
        mopt = INS_SVE_MOV_OPTS_ZEROING;
    }
    else if (targetReg != falseReg)
    {
        // If `targetReg` and `falseReg` are not same, then we need to move it to `targetReg` first
        // so the `insEmbMask` operation can be merged on top of it.
        if (falseReg != embMaskOp1Reg)
        {
            // targetReg != embMaskOp1Reg != falseReg: Use conditional select.
            // Move embMaskOp1Reg to active lanes and falseReg to inactive lanes of targetReg.
            assert(HWIntrinsicInfo::IsEmbeddedMaskedOperation(intrinEmbMask.id));
            assert(!HWIntrinsicInfo::IsZeroingMaskedOperation(intrinEmbMask.id));
            GetEmitter()->emitIns_R_R_R_R(INS_sve_sel, emitSize, targetReg, maskReg, embMaskOp1Reg, falseReg, opt);
            // embMaskOp1Reg becomes targetReg, then use unpredicated movprfx.
            embMaskOp1Reg = targetReg;
            mopt          = INS_SVE_MOV_OPTS_UNPRED;
        }
        else
        {
            // targetReg != falseReg == embMaskOp1Reg: Use unpredicated movprfx.
            mopt = INS_SVE_MOV_OPTS_UNPRED;
        }
    }
    else if (falseReg != embMaskOp1Reg)
    {
        // targetReg == falseReg != embMaskOp1Reg: Use merging movprfx.
        mopt = INS_SVE_MOV_OPTS_MERGING;
    }

    if (maskOp->IsTrueMask(intrinCndSel.baseType))
    {
        // Prefer using unpredicated movprfx when possible.
        mopt = INS_SVE_MOV_OPTS_UNPRED;
    }

    // Emit the embedded masked intrinsics
    if (HWIntrinsicInfo::HasImmediateOperand(intrinEmbMask.id))
    {
        // The immediate operand is the last operand.
        GenTree* immOp = embMaskOp->AsHWIntrinsic()->Op(intrinEmbMask.numOperands);
        assert(immOp->isContained() == (immOp->GetRegNum() == REG_NA));

        if ((intrinEmbMask.id == NI_Sve_MultiplyAddRotateComplex) && (targetReg != embMaskOp1Reg))
        {
            if (targetReg == embMaskOp2Reg)
            {
                GetEmitter()->emitInsSve_Mov(INS_sve_mov, EA_SCALABLE, tempReg, embMaskOp2Reg, /* canSkip */ true, opt);
                embMaskOp2Reg = tempReg;
                if (embMaskOp3Reg == targetReg)
                {
                    embMaskOp3Reg = tempReg;
                }
            }
            else if (targetReg == embMaskOp3Reg)
            {
                GetEmitter()->emitInsSve_Mov(INS_sve_mov, EA_SCALABLE, tempReg, embMaskOp3Reg, /* canSkip */ true, opt);
                embMaskOp3Reg = tempReg;
            }
        }

        int                    numInstrs = ((mopt != INS_SVE_MOV_OPTS_UNPRED) || (targetReg != embMaskOp1Reg)) ? 2 : 1;
        HWIntrinsicImmOpHelper helper(this, immOp, embMaskOp->AsHWIntrinsic(), numInstrs);
        for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
        {
            ssize_t imm = helper.ImmValue();
            switch (intrinEmbMask.numOperands)
            {
                case 2:
                    GetEmitter()->emitInsSve_R_R_R_I(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg, imm,
                                                     embOpt, sopt, mopt);
                    break;
                case 3:
                    GetEmitter()->emitInsSve_R_R_R_R_I(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg,
                                                       embMaskOp2Reg, imm, embOpt, sopt, mopt);
                    break;
                case 4:
                    GetEmitter()->emitInsSve_R_R_R_R_R_I(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg,
                                                         embMaskOp2Reg, embMaskOp3Reg, imm, embOpt, sopt, mopt);
                    break;
                default:
                    unreached();
            }
        }
    }
    else
    {
        switch (intrinEmbMask.numOperands)
        {
            case 2:
                GetEmitter()->emitInsSve_R_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg, embMaskOp2Reg,
                                                 embOpt, sopt, mopt);
                break;
            case 3:
                GetEmitter()->emitInsSve_R_R_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg,
                                                   embMaskOp2Reg, embMaskOp3Reg, embOpt, sopt, mopt);
                break;
            default:
                unreached();
        }
    }
}

#ifdef DEBUG
void CodeGen::checkRMWRegisters(const HWIntrinsic intrin, regNumber targetReg)
{
    const bool canRepairTargetOverlap = (intrin.id == NI_Sve_MultiplyAddRotateComplex);

    GenTree* rmwOp;
    if (HWIntrinsicInfo::IsFmaIntrinsic(intrin.id) && (intrin.numOperands == 3))
    {
        // SVE FMA intrinsics can use either the addend or a multiplicand as the destructive operand. Codegen
        // selects the matching instruction form and rearranges the operands based on the allocated target.
        if (targetReg == intrin.op2->GetRegNum())
        {
            rmwOp = intrin.op2;
        }
        else if (targetReg == intrin.op3->GetRegNum())
        {
            rmwOp = intrin.op3;
        }
        else
        {
            rmwOp = intrin.op1;
        }
    }
    else
    {
        switch (intrin.id)
        {
            case NI_Sve2_AddCarryWideningEven:
            case NI_Sve2_AddCarryWideningOdd:
                // RMW operates on op3
                rmwOp = intrin.op3;
                break;
            case NI_Sve_CreateBreakPropagateMask:
            case NI_Sve2_BitwiseSelect:
            case NI_Sve2_BitwiseSelectLeftInverted:
            case NI_Sve2_BitwiseSelectRightInverted:
                // RMW operates on op2
                rmwOp = intrin.op2;
                break;
            default:
                if (HWIntrinsicInfo::IsExplicitMaskedOperation(intrin.id))
                {
                    rmwOp = intrin.op2;
                }
                else
                {
                    rmwOp = intrin.op1;
                }
                break;
        }
    }

    regNumber rmwReg = rmwOp->GetRegNum();
    if (targetReg != rmwReg)
    {
        switch (intrin.numOperands)
        {
            case 5:
                assert((targetReg != intrin.op5->GetRegNum()) || genIsSameLocalVar(rmwOp, intrin.op5));
                FALLTHROUGH;

            case 4:
                assert((targetReg != intrin.op4->GetRegNum()) || genIsSameLocalVar(rmwOp, intrin.op4));
                FALLTHROUGH;

            case 3:
                if (rmwReg != intrin.op3->GetRegNum())
                {
                    assert(canRepairTargetOverlap || (targetReg != intrin.op3->GetRegNum()) ||
                           genIsSameLocalVar(rmwOp, intrin.op3));
                }
                FALLTHROUGH;

            case 2:
                if (rmwReg != intrin.op2->GetRegNum())
                {
                    assert(canRepairTargetOverlap || (targetReg != intrin.op2->GetRegNum()) ||
                           genIsSameLocalVar(rmwOp, intrin.op2));
                }
                if (rmwReg != intrin.op1->GetRegNum())
                {
                    assert((targetReg != intrin.op1->GetRegNum()) || genIsSameLocalVar(rmwOp, intrin.op1));
                }
                break;

            default:
                break;
        }
    }
}
#endif // DEBUG

//------------------------------------------------------------------------
// genHWIntrinsic: Generates the code for a given hardware intrinsic node.
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genHWIntrinsic(GenTreeHWIntrinsic* node)
{
    const HWIntrinsic      intrin(node);
    CORINFO_InstructionSet isa = HWIntrinsicInfo::lookupIsa(intrin.id);

    // We need to validate that other phases of the compiler haven't introduced unsupported intrinsics

    if (isa == InstructionSet_Vector)
    {
        if (node->GetSimdSize() == 8)
        {
            assert(m_compiler->compIsaSupportedDebugOnly(InstructionSet_Vector64));
        }
        else
        {
            assert((node->GetSimdSize() == 12) || (node->GetSimdSize() == 16));
            assert(m_compiler->compIsaSupportedDebugOnly(InstructionSet_Vector128));
        }
    }
    else
    {
        assert(m_compiler->compIsaSupportedDebugOnly(isa));
    }

    regNumber targetReg = node->GetRegNum();

    regNumber op1Reg = REG_NA;
    regNumber op2Reg = REG_NA;
    regNumber op3Reg = REG_NA;
    regNumber op4Reg = REG_NA;
    regNumber op5Reg = REG_NA;

    switch (intrin.numOperands)
    {
        case 5:
            assert(intrin.op5 != nullptr);
            op5Reg = intrin.op5->GetRegNum();
            FALLTHROUGH;

        case 4:
            assert(intrin.op4 != nullptr);
            op4Reg = intrin.op4->GetRegNum();
            FALLTHROUGH;

        case 3:
            assert(intrin.op3 != nullptr);
            op3Reg = intrin.op3->GetRegNum();
            FALLTHROUGH;

        case 2:
            assert(intrin.op2 != nullptr);
            op2Reg = intrin.op2->GetRegNum();
            FALLTHROUGH;

        case 1:
            assert(intrin.op1 != nullptr);
            op1Reg = intrin.op1->GetRegNum();
            break;

        case 0:
            break;

        default:
            unreached();
    }

    emitAttr emitSize;
    insOpts  opt;

    if (HWIntrinsicInfo::SIMDScalar(intrin.id))
    {
        emitSize = emitTypeSize(intrin.baseType);
        opt      = INS_OPTS_NONE;
    }
    else if (intrin.category == HW_Category_Scalar)
    {
        emitSize = emitActualTypeSize(intrin.baseType);
        opt      = INS_OPTS_NONE;
    }
    else if (HWIntrinsicInfo::IsScalable(intrin.id))
    {
        emitSize = EA_SCALABLE;
        opt      = emitter::optGetSveInsOpt(emitTypeSize(intrin.baseType));
    }
    else if (intrin.category == HW_Category_Special)
    {
        assert(intrin.id == NI_ArmBase_Yield);

        emitSize = EA_UNKNOWN;
        opt      = INS_OPTS_NONE;
    }
    else
    {
        emitSize = emitActualTypeSize(Compiler::getSIMDTypeForSize(node->GetSimdSize()));
        opt      = genGetSimdInsOpt(emitSize, intrin.baseType);
    }

    const bool isRMW               = node->isRMWHWIntrinsic(m_compiler);
    const bool hasImmediateOperand = HWIntrinsicInfo::HasImmediateOperand(intrin.id);

    genConsumeMultiOpOperands(node);

#ifdef DEBUG
    // Check that RMW instructions are not reusing source registers as destination register,
    // unless they are referencing the same local variable.
    // If we see an optional embedded masked operation here, it is not embedded (and not RMW).
    if (isRMW && !HWIntrinsicInfo::IsOptionalEmbeddedMaskedOperation(intrin.id))
    {
        checkRMWRegisters(intrin, targetReg);
    }
#endif // DEBUG

    if (intrin.codeGenIsTableDriven())
    {
        const instruction ins = HWIntrinsicInfo::lookupIns(intrin.id, intrin.baseType, m_compiler);
        assert(ins != INS_invalid);

        if (intrin.category == HW_Category_SIMDByIndexedElement)
        {
            if (hasImmediateOperand)
            {
                switch (intrin.numOperands)
                {
                    case 2:
                    {
                        assert(!isRMW);

                        HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                        for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                        {
                            const int elementIndex = helper.ImmValue();

                            GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op1Reg, elementIndex, opt);
                        }
                        break;
                    }

                    case 3:
                    {
                        assert(!isRMW);

                        HWIntrinsicImmOpHelper helper(this, intrin.op3, node);

                        for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                        {
                            const int elementIndex = helper.ImmValue();

                            GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, elementIndex, opt);
                        }
                        break;
                    }

                    case 4:
                    {
                        assert(isRMW);

                        // emitIns_R_R_R_R_I may emit a mov (when not redundant) for RMW instructions;
                        // the ImmOpHelper must account for that mov
                        int                    numInstrs = (targetReg != op1Reg) ? 2 : 1;
                        HWIntrinsicImmOpHelper helper(this, intrin.op4, node, numInstrs);

                        for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                        {
                            const int elementIndex = helper.ImmValue();

                            GetEmitter()->emitIns_R_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg,
                                                            elementIndex, opt);
                        }
                        break;
                    }

                    default:
                        unreached();
                }
            }
            else if (isRMW)
            {
                GetEmitter()->emitIns_R_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, 0, opt);
            }
            else
            {
                GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, 0, opt);
            }
        }
        else if ((intrin.category == HW_Category_ShiftLeftByImmediate) ||
                 (intrin.category == HW_Category_ShiftRightByImmediate))
        {
            assert(hasImmediateOperand);

            GenTree*               shiftOp   = isRMW ? intrin.op3 : intrin.op2;
            int                    numInstrs = (isRMW && (targetReg != op1Reg)) ? 2 : 1;
            HWIntrinsicImmOpHelper helper(this, shiftOp, node, numInstrs);

            for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
            {
                const int shiftAmount = helper.ImmValue();
                assert((shiftAmount != 0) || (intrin.category == HW_Category_ShiftLeftByImmediate));

                if (isRMW)
                {
                    assert(intrin.numOperands == 3);
                    GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, shiftAmount, opt);
                }
                else
                {
                    assert(intrin.numOperands == 2);
                    GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op1Reg, shiftAmount, opt);
                }
            }
        }
        else if (intrin.id == NI_Sve_ConditionalSelect && intrin.op2->IsEmbMaskOp())
        {
            // Handle case where op2 is operation that needs embedded mask
            genEmbeddedMaskedHWIntrinsic(node, targetReg);
        }
        else
        {
            switch (intrin.numOperands)
            {
                case 0:
                    assert(!hasImmediateOperand);
                    GetEmitter()->emitIns_R(ins, emitSize, targetReg, opt);
                    break;

                case 1:
                    if (hasImmediateOperand)
                    {
                        assert(HWIntrinsicInfo::IsScalable(intrin.id));
                        HWIntrinsicImmOpHelper helper(this, intrin.op1, node);
                        for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                        {
                            const insSvePattern pattern = (insSvePattern)helper.ImmValue();
                            GetEmitter()->emitIns_R_PATTERN(ins, emitSize, targetReg, opt, pattern);
                        }
                    }
                    else if (HWIntrinsicInfo::IsEmbeddedMaskedOperation(intrin.id) && intrin.op1->isContained())
                    {
                        // Handle instructions that have a contained conditional select.
                        assert(intrin.op1->OperIsHWIntrinsic());
                        const HWIntrinsic cselIntrin(intrin.op1->AsHWIntrinsic());

                        assert(cselIntrin.id == NI_Sve_ConditionalSelect);
                        regNumber maskReg = cselIntrin.op1->GetRegNum();
                        op1Reg            = cselIntrin.op2->GetRegNum();
                        GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, maskReg, op1Reg, opt);
                    }
                    else
                    {
                        GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg, opt);
                    }
                    break;

                case 2:
                    assert(!hasImmediateOperand);

                    // This handles optimizations for instructions that have
                    // an implicit 'zero' vector of what would be the second operand.
                    if (HWIntrinsicInfo::SupportsContainment(intrin.id) && intrin.op2->isContained() &&
                        intrin.op2->IsVectorZero())
                    {
                        GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg, opt);
                    }
                    else
                    {
                        GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                    }
                    break;

                case 3:

                    if (hasImmediateOperand)
                    {
                        assert(!isRMW);
                        HWIntrinsicImmOpHelper helper(this, intrin.op3, node);
                        for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                        {
                            const int imm = helper.ImmValue();
                            GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, imm, opt);
                        }
                    }
                    else
                    {
                        GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, opt);
                    }
                    break;

                default:
                    unreached();
            }
        }
    }
    else
    {
        instruction ins = INS_invalid;
        switch (intrin.id)
        {
            case NI_AdvSimd_AddWideningLower:
                assert(varTypeIsIntegral(intrin.baseType));
                if (intrin.op1->TypeIs(TYP_SIMD8))
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_uaddl : INS_saddl;
                }
                else
                {
                    assert(intrin.op1->TypeIs(TYP_SIMD16));
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_uaddw : INS_saddw;
                }
                break;

            case NI_AdvSimd_SubtractWideningLower:
                assert(varTypeIsIntegral(intrin.baseType));
                if (intrin.op1->TypeIs(TYP_SIMD8))
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_usubl : INS_ssubl;
                }
                else
                {
                    assert(intrin.op1->TypeIs(TYP_SIMD16));
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_usubw : INS_ssubw;
                }
                break;

            case NI_AdvSimd_AddWideningUpper:
                assert(varTypeIsIntegral(intrin.baseType));
                if (node->GetAuxiliaryType() == intrin.baseType)
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_uaddl2 : INS_saddl2;
                }
                else
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_uaddw2 : INS_saddw2;
                }
                break;

            case NI_AdvSimd_SubtractWideningUpper:
                assert(varTypeIsIntegral(intrin.baseType));
                if (node->GetAuxiliaryType() == intrin.baseType)
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_usubl2 : INS_ssubl2;
                }
                else
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_usubw2 : INS_ssubw2;
                }
                break;

            case NI_ArmBase_Yield:
            {
                ins = INS_yield;
                break;
            }

            case NI_ArmBase_Arm64_MultiplyLongAdd:
                ins = varTypeIsUnsigned(intrin.baseType) ? INS_umaddl : INS_smaddl;
                break;

            case NI_ArmBase_Arm64_MultiplyLongSub:
                ins = varTypeIsUnsigned(intrin.baseType) ? INS_umsubl : INS_smsubl;
                break;

            case NI_Sve_StoreNarrowing:
                ins = HWIntrinsicInfo::lookupIns(intrin.id, node->GetAuxiliaryType(), m_compiler);
                break;

            default:
                ins = HWIntrinsicInfo::lookupIns(intrin.id, intrin.baseType, m_compiler);
                break;
        }

        assert(ins != INS_invalid);

        switch (intrin.id)
        {
            case NI_AdvSimd_BitwiseSelect:
                // Even though BitwiseSelect is an RMW intrinsic per se, we don't want to mark it as such
                // since we can handle all possible allocation decisions for targetReg.
                assert(!isRMW);

                if (targetReg == op1Reg)
                {
                    GetEmitter()->emitIns_R_R_R(INS_bsl, emitSize, targetReg, op2Reg, op3Reg, opt);
                }
                else if (targetReg == op2Reg)
                {
                    GetEmitter()->emitIns_R_R_R(INS_bif, emitSize, targetReg, op3Reg, op1Reg, opt);
                }
                else if (targetReg == op3Reg)
                {
                    GetEmitter()->emitIns_R_R_R(INS_bit, emitSize, targetReg, op2Reg, op1Reg, opt);
                }
                else
                {
                    GetEmitter()->emitIns_Mov(INS_mov, emitSize, targetReg, op1Reg, /* canSkip */ false);
                    GetEmitter()->emitIns_R_R_R(INS_bsl, emitSize, targetReg, op2Reg, op3Reg, opt);
                }
                break;

            case NI_Crc32_ComputeCrc32:
            case NI_Crc32_ComputeCrc32C:
            case NI_Crc32_Arm64_ComputeCrc32:
            case NI_Crc32_Arm64_ComputeCrc32C:
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;

            case NI_AdvSimd_CompareLessThan:
            case NI_AdvSimd_CompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_CompareLessThan:
            case NI_AdvSimd_Arm64_CompareLessThanScalar:
            case NI_AdvSimd_Arm64_CompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_CompareLessThanOrEqualScalar:
                // If the second operand is a contained zero, we can emit the
                // 'less than [or equal to] zero' form directly instead of
                // materializing a zero vector and swapping the operands.
                if (intrin.op2->isContained())
                {
                    assert(intrin.op2->IsVectorZero());

                    instruction zeroIns = INS_invalid;
                    switch (ins)
                    {
                        case INS_cmgt:
                            zeroIns = INS_cmlt;
                            break;
                        case INS_cmge:
                            zeroIns = INS_cmle;
                            break;
                        case INS_fcmgt:
                            zeroIns = INS_fcmlt;
                            break;
                        case INS_fcmge:
                            zeroIns = INS_fcmle;
                            break;
                        default:
                            unreached();
                    }

                    GetEmitter()->emitIns_R_R(zeroIns, emitSize, targetReg, op1Reg, opt);
                }
                else
                {
                    GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op2Reg, op1Reg, opt);
                }
                break;

            case NI_AdvSimd_AbsoluteCompareLessThan:
            case NI_AdvSimd_AbsoluteCompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThan:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThanScalar:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThanOrEqualScalar:
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op2Reg, op1Reg, opt);
                break;

            case NI_AdvSimd_FusedMultiplyAddScalar:
            case NI_AdvSimd_FusedMultiplyAddNegatedScalar:
            case NI_AdvSimd_FusedMultiplySubtractNegatedScalar:
            case NI_AdvSimd_FusedMultiplySubtractScalar:
                assert(opt == INS_OPTS_NONE);
                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op2Reg, op3Reg, op1Reg);
                break;

            case NI_AdvSimd_DuplicateSelectedScalarToVector64:
            case NI_AdvSimd_DuplicateSelectedScalarToVector128:
            case NI_AdvSimd_Arm64_DuplicateSelectedScalarToVector128:
            {
                HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                // Prior to codegen, the emitSize is based on node->GetSimdSize() which
                // tracks the size of the first operand and is used to tell if the index
                // is in range. However, when actually emitting it needs to be the size
                // of the return and the size of the operand is interpreted based on the
                // index value.

                assert(
                    GetEmitter()->isValidVectorIndex(emitSize, GetEmitter()->optGetElemsize(opt), helper.ImmValue()));

                emitSize = emitActualTypeSize(node->gtType);
                opt      = genGetSimdInsOpt(emitSize, intrin.baseType);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();

                    assert(opt != INS_OPTS_NONE);
                    GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op1Reg, elementIndex, opt);
                }

                break;
            }

            case NI_AdvSimd_Extract:
            {
                HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();

                    GetEmitter()->emitIns_R_R_I(ins, emitTypeSize(intrin.baseType), targetReg, op1Reg, elementIndex,
                                                INS_OPTS_NONE);
                }
            }
            break;

            case NI_AdvSimd_ExtractVector64:
            case NI_AdvSimd_ExtractVector128:
            {
                opt = (intrin.id == NI_AdvSimd_ExtractVector64) ? INS_OPTS_8B : INS_OPTS_16B;

                HWIntrinsicImmOpHelper helper(this, intrin.op3, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();
                    const int byteIndex    = genTypeSize(intrin.baseType) * elementIndex;

                    GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, byteIndex, opt);
                }
            }
            break;

            case NI_AdvSimd_Insert:
            {
                assert(isRMW);

                GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);

                // fmov (scalar) zeros the upper bits and is not safe to use
                assert(!intrin.op3->isContainedFltOrDblImmed());

                // The mov above copies op1 into targetReg and the ins below then reads op3. That is
                // only unsafe when targetReg == op3Reg but targetReg != op1Reg, as the mov would then
                // clobber op3 before it is read. LSRA marks op3 delayFree, so a distinct op3 can never
                // share the def register; targetReg == op3Reg is only reachable when op3 aliases op1
                // (e.g. Vector.Create(x, ..., x, ...) after a floating-point CreateScalarUnsafe is
                // elided into a bare scalar), in which case the mov is skipped and op3 is preserved.
                assert((targetReg != op3Reg) || (targetReg == op1Reg));

                HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                if (varTypeIsFloating(intrin.baseType))
                {
                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        const int elementIndex = helper.ImmValue();

                        GetEmitter()->emitIns_R_R_I_I(ins, emitSize, targetReg, op3Reg, elementIndex, 0, opt);
                    }
                }
                else
                {
                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        const int elementIndex = helper.ImmValue();

                        GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op3Reg, elementIndex, opt);
                    }
                }
                break;
            }

            case NI_AdvSimd_InsertScalar:
            {
                assert(isRMW);
                GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);

                HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();

                    GetEmitter()->emitIns_R_R_I_I(ins, emitSize, targetReg, op3Reg, elementIndex, 0, opt);
                }
            }
            break;

            case NI_AdvSimd_Arm64_InsertSelectedScalar:
            {
                assert(isRMW);
                GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);

                const int resultIndex = (int)intrin.op2->AsIntCon()->IconValue();
                const int valueIndex  = (int)intrin.op4->AsIntCon()->IconValue();
                GetEmitter()->emitIns_R_R_I_I(ins, emitSize, targetReg, op3Reg, resultIndex, valueIndex, opt);
            }
            break;

            case NI_AdvSimd_LoadAndInsertScalar:
            {
                assert(isRMW);
                GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);

                HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();

                    GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op3Reg, elementIndex);
                }
            }
            break;

            case NI_AdvSimd_LoadAndInsertScalarVector64x2:
            case NI_AdvSimd_LoadAndInsertScalarVector64x3:
            case NI_AdvSimd_LoadAndInsertScalarVector64x4:
            case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x2:
            case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x3:
            case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x4:
            {
                assert(isRMW);
                unsigned fieldIdx = 0;
                op2Reg            = intrin.op2->GetRegNum();
                op3Reg            = intrin.op3->GetRegNum();
                assert(intrin.op1->OperIsFieldList());

                GenTreeFieldList* fieldList  = intrin.op1->AsFieldList();
                GenTree*          firstField = fieldList->Uses().GetHead()->GetNode();
                op1Reg                       = firstField->GetRegNum();

                regNumber targetFieldReg = REG_NA;
                regNumber op1FieldReg    = REG_NA;

                for (GenTreeFieldList::Use& use : fieldList->Uses())
                {
                    GenTree* fieldNode = use.GetNode();

                    targetFieldReg = node->GetRegByIndex(fieldIdx);
                    op1FieldReg    = fieldNode->GetRegNum();

                    GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(fieldNode), targetFieldReg, op1FieldReg,
                                              /* canSkip */ true);
                    fieldIdx++;
                }

                HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();

                    GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op3Reg, elementIndex);
                }

                break;
            }
            case NI_AdvSimd_Arm64_LoadPairVector128:
            case NI_AdvSimd_Arm64_LoadPairVector128NonTemporal:
            case NI_AdvSimd_Arm64_LoadPairVector64:
            case NI_AdvSimd_Arm64_LoadPairVector64NonTemporal:
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, node->GetRegByIndex(1), op1Reg);
                break;

            case NI_AdvSimd_Arm64_LoadPairScalarVector64:
            case NI_AdvSimd_Arm64_LoadPairScalarVector64NonTemporal:
                GetEmitter()->emitIns_R_R_R(ins, emitTypeSize(intrin.baseType), targetReg, node->GetRegByIndex(1),
                                            op1Reg);
                break;

            case NI_AdvSimd_Arm64_StorePair:
            case NI_AdvSimd_Arm64_StorePairNonTemporal:
                GetEmitter()->emitIns_R_R_R(ins, emitSize, op2Reg, op3Reg, op1Reg);
                break;

            case NI_AdvSimd_Arm64_StorePairScalar:
            case NI_AdvSimd_Arm64_StorePairScalarNonTemporal:
                GetEmitter()->emitIns_R_R_R(ins, emitTypeSize(intrin.baseType), op2Reg, op3Reg, op1Reg);
                break;

            case NI_AdvSimd_StoreSelectedScalar:
            case NI_AdvSimd_Arm64_StoreSelectedScalar:
            {
                unsigned regCount = 0;
                if (intrin.op2->OperIsFieldList())
                {
                    GenTreeFieldList* fieldList  = intrin.op2->AsFieldList();
                    GenTree*          firstField = fieldList->Uses().GetHead()->GetNode();
                    op2Reg                       = firstField->GetRegNum();

                    regNumber argReg = op2Reg;
                    for (GenTreeFieldList::Use& use : fieldList->Uses())
                    {
                        regCount++;
#ifdef DEBUG
                        GenTree* argNode = use.GetNode();
                        assert(argReg == argNode->GetRegNum());
                        argReg = getNextSIMDRegWithWraparound(argReg);
#endif
                    }
                }
                else
                {
                    regCount = 1;
                }

                switch (regCount)
                {
                    case 2:
                        ins = INS_st2;
                        break;

                    case 3:
                        ins = INS_st3;
                        break;

                    case 4:
                        ins = INS_st4;
                        break;

                    default:
                        assert(regCount == 1);
                        ins = INS_st1;
                        break;
                }

                HWIntrinsicImmOpHelper helper(this, intrin.op3, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();

                    GetEmitter()->emitIns_R_R_I(ins, emitSize, op2Reg, op1Reg, elementIndex, opt);
                }
                break;
            }

            case NI_AdvSimd_Store:
            case NI_AdvSimd_Arm64_Store:
            case NI_AdvSimd_StoreVectorAndZip:
            case NI_AdvSimd_Arm64_StoreVectorAndZip:
            {
                unsigned regCount = 0;

                assert(intrin.op2->OperIsFieldList());

                GenTreeFieldList* fieldList  = intrin.op2->AsFieldList();
                GenTree*          firstField = fieldList->Uses().GetHead()->GetNode();
                op2Reg                       = firstField->GetRegNum();

                regNumber argReg = op2Reg;
                for (GenTreeFieldList::Use& use : fieldList->Uses())
                {
                    regCount++;
#ifdef DEBUG
                    GenTree* argNode = use.GetNode();
                    assert(argReg == argNode->GetRegNum());
                    argReg = getNextSIMDRegWithWraparound(argReg);
#endif
                }

                bool isSequentialStore = (intrin.id == NI_AdvSimd_Arm64_Store || intrin.id == NI_AdvSimd_Store);
                switch (regCount)
                {
                    case 2:
                        ins = isSequentialStore ? INS_st1_2regs : INS_st2;
                        break;

                    case 3:
                        ins = isSequentialStore ? INS_st1_3regs : INS_st3;
                        break;

                    case 4:
                        ins = isSequentialStore ? INS_st1_4regs : INS_st4;
                        break;

                    default:
                        unreached();
                }
                GetEmitter()->emitIns_R_R(ins, emitSize, op2Reg, op1Reg, opt);
                break;
            }

            case NI_Vector_CreateScalarUnsafe:
                if (intrin.op1->isContainedFltOrDblImmed())
                {
                    // fmov reg, #imm8
                    const double dataValue = intrin.op1->AsDblCon()->DconValue();
                    GetEmitter()->emitIns_R_F(ins, emitTypeSize(intrin.baseType), targetReg, dataValue, INS_OPTS_NONE);
                }
                else if (varTypeIsFloating(intrin.baseType))
                {
                    // fmov reg1, reg2
                    assert(GetEmitter()->IsMovInstruction(ins));
                    assert(intrin.baseType == intrin.op1->gtType);
                    GetEmitter()->emitIns_Mov(ins, emitTypeSize(intrin.baseType), targetReg, op1Reg,
                                              /* canSkip */ true, INS_OPTS_NONE);
                }
                else
                {
                    if (intrin.op1->isContainedIntOrIImmed())
                    {
                        // movi/movni reg, #imm8
                        const ssize_t dataValue = intrin.op1->AsIntCon()->IconValue();
                        GetEmitter()->emitIns_R_I(INS_movi, emitSize, targetReg, dataValue, opt);
                    }
                    else
                    {
                        // ins reg1[0], reg2
                        GetEmitter()->emitIns_R_R_I(ins, emitTypeSize(intrin.baseType), targetReg, op1Reg, 0,
                                                    INS_OPTS_NONE);
                    }
                }
                break;

            case NI_AdvSimd_AddWideningLower:
            case NI_AdvSimd_AddWideningUpper:
            case NI_AdvSimd_SubtractWideningLower:
            case NI_AdvSimd_SubtractWideningUpper:
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;

            case NI_AdvSimd_Arm64_AddSaturateScalar:
                if (varTypeIsUnsigned(node->GetAuxiliaryType()) != varTypeIsUnsigned(intrin.baseType))
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_usqadd : INS_suqadd;
                }
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;

            case NI_ArmBase_Yield:
            {
                GetEmitter()->emitIns(ins);
                break;
            }

            case NI_AdvSimd_DuplicateToVector64:
            case NI_AdvSimd_DuplicateToVector128:
            case NI_AdvSimd_Arm64_DuplicateToVector64:
            case NI_AdvSimd_Arm64_DuplicateToVector128:
            {
                if (varTypeIsFloating(intrin.baseType))
                {
                    if (intrin.op1->isContainedFltOrDblImmed())
                    {
                        const double dataValue = intrin.op1->AsDblCon()->DconValue();
                        GetEmitter()->emitIns_R_F(INS_fmov, emitSize, targetReg, dataValue, opt);
                    }
                    else if (intrin.id == NI_AdvSimd_Arm64_DuplicateToVector64)
                    {
                        assert(intrin.baseType == TYP_DOUBLE);
                        assert(GetEmitter()->IsMovInstruction(ins));
                        assert(intrin.baseType == intrin.op1->gtType);
                        GetEmitter()->emitIns_Mov(ins, emitSize, targetReg, op1Reg, /* canSkip */ true, opt);
                    }
                    else
                    {
                        GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op1Reg, 0, opt);
                    }
                }
                else if (intrin.op1->isContainedIntOrIImmed())
                {
                    const ssize_t dataValue = intrin.op1->AsIntCon()->IconValue();
                    GetEmitter()->emitIns_R_I(INS_movi, emitSize, targetReg, dataValue, opt);
                }
                else if (GetEmitter()->IsMovInstruction(ins))
                {
                    GetEmitter()->emitIns_Mov(ins, emitSize, targetReg, op1Reg, /* canSkip */ false, opt);
                }
                else
                {
                    GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg, opt);
                }
            }
            break;

            case NI_Sve_Load2xVectorAndUnzip:
            case NI_Sve_Load3xVectorAndUnzip:
            case NI_Sve_Load4xVectorAndUnzip:
            {
#ifdef DEBUG
                // Validates that consecutive registers were used properly.

                assert(node->GetMultiRegCount(m_compiler) == (unsigned int)GetEmitter()->insGetSveReg1ListSize(ins));

                regNumber argReg = targetReg;
                for (unsigned int i = 0; i < node->GetMultiRegCount(m_compiler); i++)
                {
                    assert(argReg == node->GetRegNumByIdx(i));
                    argReg = getNextSIMDRegWithWraparound(argReg);
                }
#endif // DEBUG
                GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, 0, opt);
                break;
            }

            case NI_Sve2_VectorTableLookup:
            {
                assert(intrin.op1->OperIsFieldList());
                GenTreeFieldList* fieldList  = intrin.op1->AsFieldList();
                GenTree*          firstField = fieldList->Uses().GetHead()->GetNode();
                op1Reg                       = firstField->GetRegNum();
#ifdef DEBUG
                unsigned  regCount = 0;
                regNumber argReg   = op1Reg;
                for (GenTreeFieldList::Use& use : fieldList->Uses())
                {
                    regCount++;

                    GenTree* argNode = use.GetNode();
                    assert(argReg == argNode->GetRegNum());
                    argReg = getNextSIMDRegWithWraparound(argReg);
                }
                assert(regCount == 2);
#endif
                GetEmitter()->emitInsSve_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt,
                                               INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
                break;
            }

            case NI_Sve_StoreAndZipx2:
            case NI_Sve_StoreAndZipx3:
            case NI_Sve_StoreAndZipx4:
            {
                assert(intrin.op3->OperIsFieldList());
                GenTreeFieldList* fieldList  = intrin.op3->AsFieldList();
                GenTree*          firstField = fieldList->Uses().GetHead()->GetNode();
                op3Reg                       = firstField->GetRegNum();

#ifdef DEBUG
                unsigned  regCount = 0;
                regNumber argReg   = op3Reg;
                for (GenTreeFieldList::Use& use : fieldList->Uses())
                {
                    regCount++;

                    GenTree* argNode = use.GetNode();
                    assert(argReg == argNode->GetRegNum());
                    argReg = getNextSIMDRegWithWraparound(argReg);
                }

                switch (ins)
                {
                    case INS_sve_st2b:
                    case INS_sve_st2d:
                    case INS_sve_st2h:
                    case INS_sve_st2w:
                    case INS_sve_st2q:
                        assert(regCount == 2);
                        break;

                    case INS_sve_st3b:
                    case INS_sve_st3d:
                    case INS_sve_st3h:
                    case INS_sve_st3w:
                    case INS_sve_st3q:
                        assert(regCount == 3);
                        break;

                    case INS_sve_st4b:
                    case INS_sve_st4d:
                    case INS_sve_st4h:
                    case INS_sve_st4w:
                    case INS_sve_st4q:
                        assert(regCount == 4);
                        break;

                    default:
                        unreached();
                }
#endif
                GetEmitter()->emitIns_R_R_R_I(ins, emitSize, op3Reg, op1Reg, op2Reg, 0, opt);
                break;
            }

            case NI_Sve_StoreAndZip:
            case NI_Sve_StoreNonTemporal:
            {
                GetEmitter()->emitIns_R_R_R_I(ins, emitSize, op3Reg, op1Reg, op2Reg, 0, opt);
                break;
            }

            case NI_Sve_Prefetch16Bit:
            case NI_Sve_Prefetch32Bit:
            case NI_Sve_Prefetch64Bit:
            case NI_Sve_Prefetch8Bit:
            {
                assert(hasImmediateOperand);
                HWIntrinsicImmOpHelper helper(this, intrin.op3, node);
                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const insSvePrfop prfop = (insSvePrfop)helper.ImmValue();
                    GetEmitter()->emitIns_PRFOP_R_R_I(ins, emitSize, prfop, op1Reg, op2Reg, 0);
                }
                break;
            }

            case NI_Vector_ToVector128:
                GetEmitter()->emitIns_Mov(ins, emitSize, targetReg, op1Reg, /* canSkip */ false);
                break;

            case NI_Vector_ToVector128Unsafe:
            case NI_Vector_AsVector128Unsafe:
            case NI_Vector_GetLower:
                GetEmitter()->emitIns_Mov(ins, emitSize, targetReg, op1Reg, /* canSkip */ true);
                break;

            case NI_Vector_GetElement:
            {
                assert(intrin.numOperands == 2);
                assert(!intrin.op1->isContained());

                assert(intrin.op2->OperIsConst());
                assert(intrin.op2->isContained());

                var_types simdType = Compiler::getSIMDTypeForSize(node->GetSimdSize());

                if (simdType == TYP_SIMD12)
                {
                    // op1 of TYP_SIMD12 should be considered as TYP_SIMD16
                    simdType = TYP_SIMD16;
                }

                ssize_t ival = intrin.op2->AsIntCon()->IconValue();

                if (!GetEmitter()->isValidVectorIndex(emitTypeSize(simdType), emitTypeSize(intrin.baseType), ival))
                {
                    // We only need to generate code for the get if the index is valid
                    // If the index is invalid, previously generated for the range check will throw
                    break;
                }

                if ((varTypeIsFloating(intrin.baseType) && (targetReg == op1Reg) && (ival == 0)))
                {
                    // no-op if vector is float/double, targetReg == op1Reg and fetching for 0th index.
                    break;
                }

                GetEmitter()->emitIns_R_R_I(ins, emitTypeSize(intrin.baseType), targetReg, op1Reg, ival, INS_OPTS_NONE);
                break;
            }

            case NI_Vector_GetUpper:
            {
                const int byteIndex = 8;
                GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op1Reg, byteIndex, INS_OPTS_16B);
                break;
            }

            case NI_Vector_AsVector3:
            {
                // AsVector3 can be a no-op when it's already in the right register, otherwise
                // we just need to move the value over. Vector3 operations will themselves mask
                // out the upper element when it's relevant, so it's not worth us spending extra
                // cycles doing so here.

                GetEmitter()->emitIns_Mov(ins, emitSize, targetReg, op1Reg, /* canSkip */ true);
                break;
            }

            case NI_Vector_ToScalar:
            {
                if ((varTypeIsFloating(intrin.baseType) && (targetReg == op1Reg)))
                {
                    // no-op if vector is float/double and targetReg == op1Reg
                    break;
                }

                if (varTypeIsLong(intrin.baseType))
                {
                    // Use fmov for 64-bit integer types instead of umov
                    GetEmitter()->emitIns_Mov(INS_fmov, EA_8BYTE, targetReg, op1Reg, /* canSkip */ false);
                }
                else
                {
                    GetEmitter()->emitIns_R_R_I(ins, emitTypeSize(intrin.baseType), targetReg, op1Reg, /* imm */ 0,
                                                INS_OPTS_NONE);
                }
            }
            break;

            case NI_AdvSimd_ReverseElement16:
                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg,
                                          (emitSize == EA_8BYTE) ? INS_OPTS_4H : INS_OPTS_8H);
                break;

            case NI_AdvSimd_ReverseElement32:
                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg,
                                          (emitSize == EA_8BYTE) ? INS_OPTS_2S : INS_OPTS_4S);
                break;

            case NI_AdvSimd_ReverseElement8:
                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg,
                                          (emitSize == EA_8BYTE) ? INS_OPTS_8B : INS_OPTS_16B);
                break;

            case NI_AdvSimd_VectorTableLookup:
            case NI_AdvSimd_Arm64_VectorTableLookup:
            {
                unsigned regCount = 0;
                if (intrin.op1->OperIsFieldList())
                {
                    GenTreeFieldList* fieldList  = intrin.op1->AsFieldList();
                    GenTree*          firstField = fieldList->Uses().GetHead()->GetNode();
                    op1Reg                       = firstField->GetRegNum();
                    INDEBUG(regNumber argReg = op1Reg);
                    for (GenTreeFieldList::Use& use : fieldList->Uses())
                    {
                        regCount++;
#ifdef DEBUG

                        GenTree* argNode = use.GetNode();
                        assert(argReg == argNode->GetRegNum());
                        argReg = getNextSIMDRegWithWraparound(argReg);
#endif
                    }
                }
                else
                {
                    regCount = 1;
                    op1Reg   = intrin.op1->GetRegNum();
                }

                switch (regCount)
                {
                    case 2:
                        ins = INS_tbl_2regs;
                        break;
                    case 3:
                        ins = INS_tbl_3regs;
                        break;
                    case 4:
                        ins = INS_tbl_4regs;
                        break;
                    default:
                        assert(regCount == 1);
                        assert(ins == INS_tbl);
                        break;
                }

                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;
            }

            case NI_AdvSimd_VectorTableLookupExtension:
            case NI_AdvSimd_Arm64_VectorTableLookupExtension:
            {
                assert(isRMW);
                unsigned regCount = 0;
                op1Reg            = intrin.op1->GetRegNum();
                op3Reg            = intrin.op3->GetRegNum();
                if (intrin.op2->OperIsFieldList())
                {
                    GenTreeFieldList* fieldList  = intrin.op2->AsFieldList();
                    GenTree*          firstField = fieldList->Uses().GetHead()->GetNode();
                    op2Reg                       = firstField->GetRegNum();
                    INDEBUG(regNumber argReg = op2Reg);
                    for (GenTreeFieldList::Use& use : fieldList->Uses())
                    {
                        regCount++;
#ifdef DEBUG

                        GenTree* argNode = use.GetNode();

                        // registers should be consecutive
                        assert(argReg == argNode->GetRegNum());
                        // and they should not interfere with targetReg
                        assert(targetReg != argReg);
                        argReg = getNextSIMDRegWithWraparound(argReg);
#endif
                    }
                }
                else
                {
                    regCount = 1;
                    op2Reg   = intrin.op2->GetRegNum();
                }

                switch (regCount)
                {
                    case 2:
                        ins = INS_tbx_2regs;
                        break;
                    case 3:
                        ins = INS_tbx_3regs;
                        break;
                    case 4:
                        ins = INS_tbx_4regs;
                        break;
                    default:
                        assert(regCount == 1);
                        assert(ins == INS_tbx);
                        break;
                }

                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, opt);
                break;
            }

            case NI_ArmBase_Arm64_MultiplyLongAdd:
            case NI_ArmBase_Arm64_MultiplyLongSub:
                assert(opt == INS_OPTS_NONE);
                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg);
                break;

            case NI_Sha3_BitwiseClearXor:
            case NI_Sha3_Xor:
                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, INS_OPTS_16B);
                break;

            case NI_Sve_ConvertMaskToVector:
                // PMOV would be ideal here, but it is in SVE2.1.
                // Instead, use a predicated move: MOV <Zd>.<T>, <Pg>/Z, #1
                GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op1Reg, 1, opt);
                break;

            case NI_Sve_ConvertVectorToMask:
                // PMOV would be ideal here, but it is in SVE2.1.
                // Instead, use a compare: CMPNE <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0
                GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, 0, opt);
                break;

            case NI_Sve_Count16BitElements:
            case NI_Sve_Count32BitElements:
            case NI_Sve_Count64BitElements:
            case NI_Sve_Count8BitElements:
            {
                // Instruction has an additional immediate to multiply the result by. Use 1.
                assert(hasImmediateOperand);
                HWIntrinsicImmOpHelper helper(this, intrin.op1, node);
                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const insSvePattern pattern = (insSvePattern)helper.ImmValue();
                    GetEmitter()->emitIns_R_PATTERN_I(ins, emitSize, targetReg, pattern, 1, opt);
                }
                break;
            }

            case NI_Sve_ConversionTrueMask:
                // Must use the pattern variant, as the non-pattern varient is SVE2.1.
                GetEmitter()->emitIns_R_PATTERN(ins, emitSize, targetReg, opt, SVE_PATTERN_ALL);
                break;

            case NI_Sve_CreateWhileLessThanMaskByte:
            case NI_Sve_CreateWhileLessThanMaskDouble:
            case NI_Sve_CreateWhileLessThanMaskInt16:
            case NI_Sve_CreateWhileLessThanMaskInt32:
            case NI_Sve_CreateWhileLessThanMaskInt64:
            case NI_Sve_CreateWhileLessThanMaskSByte:
            case NI_Sve_CreateWhileLessThanMaskSingle:
            case NI_Sve_CreateWhileLessThanMaskUInt16:
            case NI_Sve_CreateWhileLessThanMaskUInt32:
            case NI_Sve_CreateWhileLessThanMaskUInt64:
                genEmitCreateWhileMask(GetEmitter(), node, ins, INS_sve_whilelo, targetReg, op1Reg, op2Reg, opt);
                break;

            case NI_Sve_CreateWhileLessThanOrEqualMaskByte:
            case NI_Sve_CreateWhileLessThanOrEqualMaskDouble:
            case NI_Sve_CreateWhileLessThanOrEqualMaskInt16:
            case NI_Sve_CreateWhileLessThanOrEqualMaskInt32:
            case NI_Sve_CreateWhileLessThanOrEqualMaskInt64:
            case NI_Sve_CreateWhileLessThanOrEqualMaskSByte:
            case NI_Sve_CreateWhileLessThanOrEqualMaskSingle:
            case NI_Sve_CreateWhileLessThanOrEqualMaskUInt16:
            case NI_Sve_CreateWhileLessThanOrEqualMaskUInt32:
            case NI_Sve_CreateWhileLessThanOrEqualMaskUInt64:
                genEmitCreateWhileMask(GetEmitter(), node, ins, INS_sve_whilels, targetReg, op1Reg, op2Reg, opt);
                break;

            case NI_Sve2_CreateWhileGreaterThanMaskByte:
            case NI_Sve2_CreateWhileGreaterThanMaskDouble:
            case NI_Sve2_CreateWhileGreaterThanMaskInt16:
            case NI_Sve2_CreateWhileGreaterThanMaskInt32:
            case NI_Sve2_CreateWhileGreaterThanMaskInt64:
            case NI_Sve2_CreateWhileGreaterThanMaskSByte:
            case NI_Sve2_CreateWhileGreaterThanMaskSingle:
            case NI_Sve2_CreateWhileGreaterThanMaskUInt16:
            case NI_Sve2_CreateWhileGreaterThanMaskUInt32:
            case NI_Sve2_CreateWhileGreaterThanMaskUInt64:
                genEmitCreateWhileMask(GetEmitter(), node, ins, INS_sve_whilehi, targetReg, op1Reg, op2Reg, opt);
                break;

            case NI_Sve2_CreateWhileGreaterThanOrEqualMaskByte:
            case NI_Sve2_CreateWhileGreaterThanOrEqualMaskDouble:
            case NI_Sve2_CreateWhileGreaterThanOrEqualMaskInt16:
            case NI_Sve2_CreateWhileGreaterThanOrEqualMaskInt32:
            case NI_Sve2_CreateWhileGreaterThanOrEqualMaskInt64:
            case NI_Sve2_CreateWhileGreaterThanOrEqualMaskSByte:
            case NI_Sve2_CreateWhileGreaterThanOrEqualMaskSingle:
            case NI_Sve2_CreateWhileGreaterThanOrEqualMaskUInt16:
            case NI_Sve2_CreateWhileGreaterThanOrEqualMaskUInt32:
            case NI_Sve2_CreateWhileGreaterThanOrEqualMaskUInt64:
                genEmitCreateWhileMask(GetEmitter(), node, ins, INS_sve_whilehs, targetReg, op1Reg, op2Reg, opt);
                break;

            case NI_Sve2_CreateWhileReadAfterWriteMaskByte:
            case NI_Sve2_CreateWhileReadAfterWriteMaskDouble:
            case NI_Sve2_CreateWhileReadAfterWriteMaskInt16:
            case NI_Sve2_CreateWhileReadAfterWriteMaskInt32:
            case NI_Sve2_CreateWhileReadAfterWriteMaskInt64:
            case NI_Sve2_CreateWhileReadAfterWriteMaskSByte:
            case NI_Sve2_CreateWhileReadAfterWriteMaskSingle:
            case NI_Sve2_CreateWhileReadAfterWriteMaskUInt16:
            case NI_Sve2_CreateWhileReadAfterWriteMaskUInt32:
            case NI_Sve2_CreateWhileReadAfterWriteMaskUInt64:
            case NI_Sve2_CreateWhileWriteAfterReadMaskByte:
            case NI_Sve2_CreateWhileWriteAfterReadMaskDouble:
            case NI_Sve2_CreateWhileWriteAfterReadMaskInt16:
            case NI_Sve2_CreateWhileWriteAfterReadMaskInt32:
            case NI_Sve2_CreateWhileWriteAfterReadMaskInt64:
            case NI_Sve2_CreateWhileWriteAfterReadMaskSByte:
            case NI_Sve2_CreateWhileWriteAfterReadMaskSingle:
            case NI_Sve2_CreateWhileWriteAfterReadMaskUInt16:
            case NI_Sve2_CreateWhileWriteAfterReadMaskUInt32:
            case NI_Sve2_CreateWhileWriteAfterReadMaskUInt64:
                // WHILERW/WHILEWR operands are always pointers (64-bit), so emitSize is always EA_8BYTE.
                // No signed/unsigned instruction variant exists.
                GetEmitter()->emitIns_R_R_R(ins, EA_8BYTE, targetReg, op1Reg, op2Reg, opt);
                break;

            case NI_Sve_GatherPrefetch8Bit:
            case NI_Sve_GatherPrefetch16Bit:
            case NI_Sve_GatherPrefetch32Bit:
            case NI_Sve_GatherPrefetch64Bit:
            {
                assert(hasImmediateOperand);

                if (!varTypeIsSIMD(intrin.op2->gtType))
                {
                    // GatherPrefetch...(Vector<T> mask, T* address, Vector<T2> indices, SvePrefetchType prefetchType)

                    assert(intrin.numOperands == 4);
                    emitAttr        baseSize = emitActualTypeSize(intrin.baseType);
                    insScalableOpts sopt     = INS_SCALABLE_OPTS_NONE;

                    if (baseSize == EA_8BYTE)
                    {
                        // Index is multiplied.
                        sopt = (ins == INS_sve_prfb) ? INS_SCALABLE_OPTS_NONE : INS_SCALABLE_OPTS_LSL_N;
                    }
                    else
                    {
                        // Index is sign or zero extended to 64bits, then multiplied.
                        assert(baseSize == EA_4BYTE);
                        opt = varTypeIsUnsigned(node->GetAuxiliaryType()) ? INS_OPTS_SCALABLE_S_UXTW
                                                                          : INS_OPTS_SCALABLE_S_SXTW;

                        sopt = (ins == INS_sve_prfb) ? INS_SCALABLE_OPTS_NONE : INS_SCALABLE_OPTS_MOD_N;
                    }

                    HWIntrinsicImmOpHelper helper(this, intrin.op4, node);
                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        const insSvePrfop prfop = (insSvePrfop)helper.ImmValue();
                        GetEmitter()->emitIns_PRFOP_R_R_R(ins, emitSize, prfop, op1Reg, op2Reg, op3Reg, opt, sopt);
                    }
                }
                else
                {
                    // GatherPrefetch...(Vector<T> mask, Vector<T2> addresses, SvePrefetchType prefetchType)

                    opt = emitter::optGetSveInsOpt(emitTypeSize(node->GetAuxiliaryType()));

                    assert(intrin.numOperands == 3);
                    HWIntrinsicImmOpHelper helper(this, intrin.op3, node);
                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        const insSvePrfop prfop = (insSvePrfop)helper.ImmValue();
                        GetEmitter()->emitIns_PRFOP_R_R_I(ins, emitSize, prfop, op1Reg, op2Reg, 0, opt);
                    }
                }

                break;
            }

            case NI_Sve_LoadVectorByteNonFaultingZeroExtendToInt16:
            case NI_Sve_LoadVectorByteNonFaultingZeroExtendToInt32:
            case NI_Sve_LoadVectorByteNonFaultingZeroExtendToInt64:
            case NI_Sve_LoadVectorByteNonFaultingZeroExtendToUInt16:
            case NI_Sve_LoadVectorByteNonFaultingZeroExtendToUInt32:
            case NI_Sve_LoadVectorByteNonFaultingZeroExtendToUInt64:
            case NI_Sve_LoadVectorInt16NonFaultingSignExtendToInt32:
            case NI_Sve_LoadVectorInt16NonFaultingSignExtendToInt64:
            case NI_Sve_LoadVectorInt16NonFaultingSignExtendToUInt32:
            case NI_Sve_LoadVectorInt16NonFaultingSignExtendToUInt64:
            case NI_Sve_LoadVectorInt32NonFaultingSignExtendToInt64:
            case NI_Sve_LoadVectorInt32NonFaultingSignExtendToUInt64:
            case NI_Sve_LoadVectorNonFaulting:
            case NI_Sve_LoadVectorSByteNonFaultingSignExtendToInt16:
            case NI_Sve_LoadVectorSByteNonFaultingSignExtendToInt32:
            case NI_Sve_LoadVectorSByteNonFaultingSignExtendToInt64:
            case NI_Sve_LoadVectorSByteNonFaultingSignExtendToUInt16:
            case NI_Sve_LoadVectorSByteNonFaultingSignExtendToUInt32:
            case NI_Sve_LoadVectorSByteNonFaultingSignExtendToUInt64:
            case NI_Sve_LoadVectorUInt16NonFaultingZeroExtendToInt32:
            case NI_Sve_LoadVectorUInt16NonFaultingZeroExtendToInt64:
            case NI_Sve_LoadVectorUInt16NonFaultingZeroExtendToUInt32:
            case NI_Sve_LoadVectorUInt16NonFaultingZeroExtendToUInt64:
            case NI_Sve_LoadVectorUInt32NonFaultingZeroExtendToInt64:
            case NI_Sve_LoadVectorUInt32NonFaultingZeroExtendToUInt64:
            {
                if (intrin.numOperands == 3)
                {
                    // We have extra argument which means there is a "use" of FFR here. Restore it back in FFR register.
                    assert(op3Reg != REG_NA);
                    GetEmitter()->emitIns_R(INS_sve_wrffr, emitSize, op3Reg, opt);
                }

                GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, 0, opt);
                break;
            }

            case NI_Sve_GatherVectorByteZeroExtendFirstFaulting:
            case NI_Sve_GatherVectorFirstFaulting:
            case NI_Sve_GatherVectorInt16SignExtendFirstFaulting:
            case NI_Sve_GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting:
            case NI_Sve_GatherVectorInt32SignExtendFirstFaulting:
            case NI_Sve_GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting:
            case NI_Sve_GatherVectorSByteSignExtendFirstFaulting:
            case NI_Sve_GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting:
            case NI_Sve_GatherVectorUInt16ZeroExtendFirstFaulting:
            case NI_Sve_GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting:
            case NI_Sve_GatherVectorUInt32ZeroExtendFirstFaulting:
            {
                if (node->GetAuxiliaryType() == TYP_UNKNOWN)
                {
                    if (intrin.numOperands == 3)
                    {
                        // We have extra argument which means there is a "use" of FFR here. Restore it back in FFR
                        // register.
                        assert(op3Reg != REG_NA);
                        GetEmitter()->emitIns_R(INS_sve_wrffr, emitSize, op3Reg, opt);
                    }
                }
                else
                {
                    // AuxilaryType is added only for numOperands == 3. If there is an extra argument, we need to
                    // "use" FFR here. Restore it back in FFR register.

                    if (intrin.numOperands == 4)
                    {
                        // We have extra argument which means there is a "use" of FFR here. Restore it back in FFR
                        // register.
                        assert(op4Reg != REG_NA);
                        GetEmitter()->emitIns_R(INS_sve_wrffr, emitSize, op4Reg, opt);
                    }
                }

                FALLTHROUGH;
            }
            case NI_Sve_GatherVector:
            case NI_Sve_GatherVectorByteZeroExtend:
            case NI_Sve_GatherVectorInt16SignExtend:
            case NI_Sve_GatherVectorInt16WithByteOffsetsSignExtend:
            case NI_Sve_GatherVectorInt32SignExtend:
            case NI_Sve_GatherVectorInt32WithByteOffsetsSignExtend:
            case NI_Sve_GatherVectorSByteSignExtend:
            case NI_Sve_GatherVectorUInt16WithByteOffsetsZeroExtend:
            case NI_Sve_GatherVectorUInt16ZeroExtend:
            case NI_Sve_GatherVectorUInt32WithByteOffsetsZeroExtend:
            case NI_Sve_GatherVectorUInt32ZeroExtend:
            case NI_Sve_GatherVectorWithByteOffsetFirstFaulting:
            {
                if (!varTypeIsSIMD(intrin.op2->gtType))
                {
                    // GatherVector...(Vector<T> mask, T* address, Vector<T2> indices)

                    emitAttr baseSize = emitActualTypeSize(intrin.baseType);
                    bool     isLoadingFromOffsets =
                        ((intrin.id == NI_Sve_GatherVectorByteZeroExtend) ||
                         (intrin.id == NI_Sve_GatherVectorByteZeroExtendFirstFaulting) ||
                         (intrin.id == NI_Sve_GatherVectorInt16WithByteOffsetsSignExtend) ||
                         (intrin.id == NI_Sve_GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting) ||
                         (intrin.id == NI_Sve_GatherVectorInt32WithByteOffsetsSignExtend) ||
                         (intrin.id == NI_Sve_GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting) ||
                         (intrin.id == NI_Sve_GatherVectorSByteSignExtend) ||
                         (intrin.id == NI_Sve_GatherVectorSByteSignExtendFirstFaulting) ||
                         (intrin.id == NI_Sve_GatherVectorUInt16WithByteOffsetsZeroExtend) ||
                         (intrin.id == NI_Sve_GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting) ||
                         (intrin.id == NI_Sve_GatherVectorUInt32WithByteOffsetsZeroExtend) ||
                         (intrin.id == NI_Sve_GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting) ||
                         (intrin.id == NI_Sve_GatherVectorWithByteOffsetFirstFaulting));
                    insScalableOpts sopt = INS_SCALABLE_OPTS_NONE;

                    if (baseSize == EA_4BYTE)
                    {
                        // Index is sign or zero extended to 64bits, then multiplied.
                        opt = varTypeIsUnsigned(node->GetAuxiliaryType()) ? INS_OPTS_SCALABLE_S_UXTW
                                                                          : INS_OPTS_SCALABLE_S_SXTW;

                        sopt = isLoadingFromOffsets ? INS_SCALABLE_OPTS_NONE : INS_SCALABLE_OPTS_MOD_N;
                    }
                    else
                    {
                        // Index is multiplied.
                        assert(baseSize == EA_8BYTE);
                        sopt = isLoadingFromOffsets ? INS_SCALABLE_OPTS_NONE : INS_SCALABLE_OPTS_LSL_N;
                    }

                    GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, opt, sopt);
                }
                else
                {
                    // GatherVector...(Vector<T> mask, Vector<T2> addresses)

                    GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, 0, opt);
                }

                break;
            }

            case NI_Sve_GatherVectorWithByteOffsets:
            {
                assert(!varTypeIsSIMD(intrin.op2->gtType));
                assert(intrin.numOperands == 3);
                emitAttr baseSize = emitActualTypeSize(intrin.baseType);

                if (baseSize == EA_4BYTE)
                {
                    // Index is sign or zero extended to 64bits.
                    opt = varTypeIsUnsigned(node->GetAuxiliaryType()) ? INS_OPTS_SCALABLE_S_UXTW
                                                                      : INS_OPTS_SCALABLE_S_SXTW;
                }
                else
                {
                    assert(baseSize == EA_8BYTE);
                }

                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, opt);
                break;
            }

            case NI_Sve2_GatherVectorInt16SignExtendNonTemporal:
            case NI_Sve2_GatherVectorInt32SignExtendNonTemporal:
            case NI_Sve2_GatherVectorNonTemporal:
            case NI_Sve2_GatherVectorUInt16ZeroExtendNonTemporal:
            case NI_Sve2_GatherVectorUInt32ZeroExtendNonTemporal:
            {
                if (!varTypeIsSIMD(intrin.op2->gtType))
                {
                    // GatherVector...(Vector<T> mask, T* address, Vector<T2> indices)

                    assert(intrin.numOperands == 3);

                    ssize_t   shift   = 0;
                    regNumber tempReg = internalRegisters.GetSingle(node, RBM_ALLFLOAT);

                    if ((intrin.id == NI_Sve2_GatherVectorInt16SignExtendNonTemporal) ||
                        (intrin.id == NI_Sve2_GatherVectorUInt16ZeroExtendNonTemporal))
                    {
                        shift = 1;
                    }
                    else if ((intrin.id == NI_Sve2_GatherVectorInt32SignExtendNonTemporal) ||
                             (intrin.id == NI_Sve2_GatherVectorUInt32ZeroExtendNonTemporal))
                    {
                        shift = 2;
                    }
                    else
                    {
                        assert(intrin.id == NI_Sve2_GatherVectorNonTemporal);
                        assert(emitActualTypeSize(intrin.baseType) == EA_8BYTE);
                        shift = 3;
                    }

                    // The SVE2 instructions only support byte offsets. Convert indices to bytes.
                    GetEmitter()->emitIns_R_R_I(INS_sve_lsl, emitSize, tempReg, op3Reg, shift, opt);

                    GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, tempReg, op2Reg, opt);
                }
                else
                {
                    // GatherVector...(Vector<T> mask, Vector<T2> addresses)
                    assert(intrin.numOperands == 2);
                    GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, REG_ZR, opt);
                }
                break;
            }

            case NI_Sve2_GatherVectorByteZeroExtendNonTemporal:
            case NI_Sve2_GatherVectorSByteSignExtendNonTemporal:
                if (!varTypeIsSIMD(intrin.op2->gtType))
                {
                    // GatherVector...(Vector<T> mask, T* address, Vector<T2> offsets)
                    assert(intrin.numOperands == 3);
                    GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op3Reg, op2Reg, opt);
                }
                else
                {
                    // GatherVector...(Vector<T> mask, Vector<T2> addresses)
                    assert(intrin.numOperands == 2);
                    GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, REG_ZR, opt);
                }
                break;

            case NI_Sve2_GatherVectorInt16WithByteOffsetsSignExtendNonTemporal:
            case NI_Sve2_GatherVectorInt32WithByteOffsetsSignExtendNonTemporal:
            case NI_Sve2_GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal:
            case NI_Sve2_GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal:
            case NI_Sve2_GatherVectorWithByteOffsetsNonTemporal:
                // GatherVector...(Vector<T> mask, T* address, Vector<T2> offsets)
                assert(!varTypeIsSIMD(intrin.op2->gtType));
                assert(intrin.numOperands == 3);
                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op3Reg, op2Reg, opt);
                break;

            case NI_Sve_ReverseElement:
                // Use non-predicated version explicitly
                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg, opt);
                break;

            case NI_Sve_Scatter:
            case NI_Sve_Scatter16BitNarrowing:
            case NI_Sve_Scatter32BitNarrowing:
            case NI_Sve_Scatter8BitNarrowing:
            {
                if (!varTypeIsSIMD(intrin.op2->gtType))
                {
                    // Scatter(Vector<T1> mask, T1* address, Vector<T2> indicies, Vector<T> data)
                    assert(intrin.numOperands == 4);
                    emitAttr        baseSize = emitActualTypeSize(intrin.baseType);
                    insScalableOpts sopt;

                    if (baseSize == EA_8BYTE)
                    {
                        // Index is multiplied by 8
                        sopt = (ins == INS_sve_st1b) ? INS_SCALABLE_OPTS_NONE : INS_SCALABLE_OPTS_LSL_N;
                        GetEmitter()->emitIns_R_R_R_R(ins, emitSize, op4Reg, op1Reg, op2Reg, op3Reg, opt, sopt);
                    }
                    else
                    {
                        // Index is sign or zero extended to 64bits, then multiplied by 4
                        assert(baseSize == EA_4BYTE);
                        opt  = varTypeIsUnsigned(node->GetAuxiliaryType()) ? INS_OPTS_SCALABLE_S_UXTW
                                                                           : INS_OPTS_SCALABLE_S_SXTW;
                        sopt = (ins == INS_sve_st1b) ? INS_SCALABLE_OPTS_NONE : INS_SCALABLE_OPTS_MOD_N;

                        GetEmitter()->emitIns_R_R_R_R(ins, emitSize, op4Reg, op1Reg, op2Reg, op3Reg, opt, sopt);
                    }
                }
                else
                {
                    // Scatter(Vector<T> mask, Vector<T> addresses, Vector<T> data)
                    assert(intrin.numOperands == 3);
                    GetEmitter()->emitIns_R_R_R_I(ins, emitSize, op3Reg, op1Reg, op2Reg, 0, opt);
                }
                break;
            }

            case NI_Sve_Scatter16BitWithByteOffsetsNarrowing:
            case NI_Sve_Scatter32BitWithByteOffsetsNarrowing:
            case NI_Sve_Scatter8BitWithByteOffsetsNarrowing:
            case NI_Sve_ScatterWithByteOffsets:
            {
                emitAttr baseSize = emitActualTypeSize(intrin.baseType);

                if (baseSize == EA_4BYTE)
                {
                    opt = varTypeIsUnsigned(node->GetAuxiliaryType()) ? INS_OPTS_SCALABLE_S_UXTW
                                                                      : INS_OPTS_SCALABLE_S_SXTW;
                }

                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, op4Reg, op1Reg, op2Reg, op3Reg, opt);
                break;
            }

            case NI_Sve2_Scatter16BitNarrowingNonTemporal:
            case NI_Sve2_Scatter32BitNarrowingNonTemporal:
            case NI_Sve2_ScatterNonTemporal:
            {
                if (!varTypeIsSIMD(intrin.op2->gtType))
                {
                    // Scatter...(Vector<T> mask, T* address, Vector<T2> indices, Vector<T> data)

                    assert(intrin.numOperands == 4);

                    ssize_t   shift   = 0;
                    regNumber tempReg = internalRegisters.GetSingle(node, RBM_ALLFLOAT);

                    if (intrin.id == NI_Sve2_Scatter16BitNarrowingNonTemporal)
                    {
                        shift = 1;
                    }
                    else if (intrin.id == NI_Sve2_Scatter32BitNarrowingNonTemporal)
                    {
                        shift = 2;
                    }
                    else
                    {
                        assert(intrin.id == NI_Sve2_ScatterNonTemporal);
                        shift = 3;
                    }

                    // The SVE2 instructions only support byte offsets. Convert indices to bytes.
                    GetEmitter()->emitIns_R_R_I(INS_sve_lsl, emitSize, tempReg, op3Reg, shift, opt);

                    GetEmitter()->emitIns_R_R_R_R(ins, emitSize, op4Reg, op1Reg, tempReg, op2Reg, opt);
                }
                else
                {
                    // Scatter...(Vector<T> mask, Vector<T> addresses, Vector<T> data)

                    assert(intrin.numOperands == 3);
                    GetEmitter()->emitIns_R_R_R_R(ins, emitSize, op3Reg, op1Reg, op2Reg, REG_ZR, opt);
                }
                break;
            }

            case NI_Sve2_Scatter8BitNarrowingNonTemporal:
                if (!varTypeIsSIMD(intrin.op2->gtType))
                {
                    // Scatter...(Vector<T> mask, T* address, Vector<T2> indices, Vector<T> data)
                    assert(intrin.numOperands == 4);
                    GetEmitter()->emitIns_R_R_R_R(ins, emitSize, op4Reg, op1Reg, op3Reg, op2Reg, opt);
                }
                else
                {
                    // Scatter...(Vector<T> mask, Vector<T> addresses, Vector<T> data)
                    assert(intrin.numOperands == 3);
                    GetEmitter()->emitIns_R_R_R_R(ins, emitSize, op3Reg, op1Reg, op2Reg, REG_ZR, opt);
                }
                break;

            case NI_Sve2_Scatter16BitWithByteOffsetsNarrowingNonTemporal:
            case NI_Sve2_Scatter32BitWithByteOffsetsNarrowingNonTemporal:
            case NI_Sve2_Scatter8BitWithByteOffsetsNarrowingNonTemporal:
            case NI_Sve2_ScatterWithByteOffsetsNonTemporal:
                // Scatter...(Vector<T> mask, T* address, Vector<T2> offsets, Vector<T> data)
                assert(!varTypeIsSIMD(intrin.op2->gtType));
                assert(intrin.numOperands == 4);

                // op2Reg and op3Reg are swapped
                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, op4Reg, op1Reg, op3Reg, op2Reg, opt);
                break;

            case NI_Sve_StoreNarrowing:
                opt = emitter::optGetSveInsOpt(emitTypeSize(intrin.baseType));
                GetEmitter()->emitIns_R_R_R_I(ins, emitSize, op3Reg, op1Reg, op2Reg, 0, opt);
                break;

            case NI_Sve_TransposeEven:
            case NI_Sve_TransposeOdd:
            case NI_Sve_UnzipEven:
            case NI_Sve_UnzipOdd:
            case NI_Sve_ZipHigh:
            case NI_Sve_ZipLow:
                // Use non-predicated version explicitly
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;

            case NI_Sve_SaturatingDecrementBy16BitElementCountScalar:
            case NI_Sve_SaturatingDecrementBy32BitElementCountScalar:
            case NI_Sve_SaturatingDecrementBy64BitElementCountScalar:
            case NI_Sve_SaturatingIncrementBy16BitElementCountScalar:
            case NI_Sve_SaturatingIncrementBy32BitElementCountScalar:
            case NI_Sve_SaturatingIncrementBy64BitElementCountScalar:
                // Use scalar sizes.
                emitSize = emitActualTypeSize(node->gtType);
                opt      = INS_OPTS_NONE;
                FALLTHROUGH;

            case NI_Sve_SaturatingDecrementBy16BitElementCount:
            case NI_Sve_SaturatingDecrementBy32BitElementCount:
            case NI_Sve_SaturatingDecrementBy64BitElementCount:
            case NI_Sve_SaturatingDecrementBy8BitElementCount:
            case NI_Sve_SaturatingIncrementBy16BitElementCount:
            case NI_Sve_SaturatingIncrementBy32BitElementCount:
            case NI_Sve_SaturatingIncrementBy64BitElementCount:
            case NI_Sve_SaturatingIncrementBy8BitElementCount:
            {
                assert(isRMW);

                if (intrin.op2->IsCnsIntOrI() && intrin.op3->IsCnsIntOrI())
                {
                    // Both immediates are constant, emit the intruction.

                    assert(intrin.op2->isContainedIntOrIImmed() && intrin.op3->isContainedIntOrIImmed());
                    int           scale   = (int)intrin.op2->AsIntCon()->IconValue();
                    insSvePattern pattern = (insSvePattern)intrin.op3->AsIntCon()->IconValue();
                    GetEmitter()->emitIns_R_R_PATTERN_I(ins, emitSize, targetReg, op1Reg, pattern, scale, opt);
                }
                else
                {
                    // Use the helper to generate a table. The table can only use a single lookup value, therefore
                    // the two immediates scale (1 to 16, in op2Reg) and pattern (0 to 31, in op3reg) must be
                    // combined to a single value (0 to 511)

                    assert(!intrin.op2->isContainedIntOrIImmed() && !intrin.op3->isContainedIntOrIImmed());

                    emitAttr scalarSize = emitActualTypeSize(node->GetSimdBaseType());

                    // Combine the two immediates into op2Reg.
                    // Reduce scale to have a lower bound of 0.
                    GetEmitter()->emitIns_R_R_I(INS_sub, scalarSize, op2Reg, op2Reg, 1);
                    // Shift pattern left to be out of range of scale.
                    GetEmitter()->emitIns_R_R_I(INS_lsl, scalarSize, op3Reg, op3Reg, 4);
                    // Combine the two values by ORing.
                    GetEmitter()->emitIns_R_R_R(INS_orr, scalarSize, op2Reg, op2Reg, op3Reg);

                    // Generate the table using the combined immediate.
                    int                    numInstrs = (targetReg != op1Reg) ? 2 : 1;
                    HWIntrinsicImmOpHelper helper(this, op2Reg, 0, 511, node, numInstrs);
                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        // Extract scale and pattern from the immediate
                        const int           value   = helper.ImmValue();
                        const int           scale   = (value & 0xF) + 1;
                        const insSvePattern pattern = (insSvePattern)(value >> 4);
                        GetEmitter()->emitIns_R_R_PATTERN_I(ins, emitSize, targetReg, op1Reg, pattern, scale, opt);
                    }

                    // Restore the original values in op2Reg and op3Reg.
                    GetEmitter()->emitIns_R_R_I(INS_and, scalarSize, op2Reg, op2Reg, 0xF);
                    GetEmitter()->emitIns_R_R_I(INS_lsr, scalarSize, op3Reg, op3Reg, 4);
                    GetEmitter()->emitIns_R_R_I(INS_add, scalarSize, op2Reg, op2Reg, 1);
                }
                break;
            }

            case NI_Sve_SaturatingDecrementByActiveElementCount:
            case NI_Sve_SaturatingIncrementByActiveElementCount:
            {
                // Switch instruction if arg1 is unsigned.
                if (varTypeIsUnsigned(node->GetAuxiliaryType()))
                {
                    ins =
                        (intrin.id == NI_Sve_SaturatingDecrementByActiveElementCount) ? INS_sve_uqdecp : INS_sve_uqincp;
                }

                // If this is the scalar variant, get the correct size.
                if (!varTypeIsSIMD(node->gtType))
                {
                    emitSize = emitActualTypeSize(intrin.op1);
                }

                // RMW semantics
                GetEmitter()->emitInsSve_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;
            }

            case NI_Sve_Compute8BitAddresses:
            case NI_Sve_Compute16BitAddresses:
            case NI_Sve_Compute32BitAddresses:
            case NI_Sve_Compute64BitAddresses:
            {
                GetEmitter()->emitInsSve_R_R_R_I(ins, EA_SCALABLE, targetReg, op1Reg, op2Reg,
                                                 HWIntrinsicInfo::lookupIval(intrin.id), opt, INS_SCALABLE_OPTS_LSL_N);
                break;
            }

            case NI_Sve_TestAnyTrue:
            case NI_Sve_TestFirstTrue:
            case NI_Sve_TestLastTrue:
                assert(targetReg == REG_NA);
                GetEmitter()->emitIns_R_R(ins, EA_SCALABLE, op1Reg, op2Reg, INS_OPTS_SCALABLE_B);
                break;

            case NI_Sve_ExtractVector:
            {
                assert(isRMW);

                int                    numInstrs = (targetReg != op1Reg) ? 2 : 1;
                HWIntrinsicImmOpHelper helper(this, intrin.op3, node, numInstrs);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();
                    const int byteIndex    = genTypeSize(intrin.baseType) * elementIndex;

                    GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, byteIndex,
                                                  INS_OPTS_SCALABLE_B);
                }
                break;
            }

            case NI_Sve_InsertIntoShiftedVector:
            {
                assert(isRMW);
                assert(emitter::isFloatReg(op2Reg) == varTypeIsFloating(intrin.baseType));
                GetEmitter()->emitInsSve_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;
            }

            case NI_Sve_CreateBreakAfterMask:
            case NI_Sve_CreateBreakBeforeMask:
            {
                GetEmitter()->emitInsSve_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, INS_OPTS_SCALABLE_B);
                break;
            }

            case NI_Sve_CreateBreakAfterPropagateMask:
            case NI_Sve_CreateBreakBeforePropagateMask:
            case NI_Sve_ConditionalSelect_Predicates:
            {
                GetEmitter()->emitInsSve_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, INS_OPTS_SCALABLE_B);
                break;
            }

            case NI_Sve_CreateMaskForFirstActiveElement:
            {
                assert(isRMW);
                assert(HWIntrinsicInfo::IsExplicitMaskedOperation(intrin.id));
                GetEmitter()->emitInsSve_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, INS_OPTS_SCALABLE_B);
                break;
            }

            case NI_Sve_LoadVectorFirstFaulting:
            case NI_Sve_LoadVectorInt16SignExtendFirstFaulting:
            case NI_Sve_LoadVectorInt32SignExtendFirstFaulting:
            case NI_Sve_LoadVectorUInt16ZeroExtendFirstFaulting:
            case NI_Sve_LoadVectorUInt32ZeroExtendFirstFaulting:
            {
                if (intrin.numOperands == 3)
                {
                    // We have extra argument which means there is a "use" of FFR here. Restore it back in FFR register.
                    assert(op3Reg != REG_NA);
                    GetEmitter()->emitIns_R(INS_sve_wrffr, emitSize, op3Reg, opt);
                }

                insScalableOpts sopt = (opt == INS_OPTS_SCALABLE_B) ? INS_SCALABLE_OPTS_NONE : INS_SCALABLE_OPTS_LSL_N;
                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, REG_ZR, opt, sopt);
                break;
            }

            case NI_Sve_LoadVectorByteZeroExtendFirstFaulting:
            case NI_Sve_LoadVectorSByteSignExtendFirstFaulting:
            {
                if (intrin.numOperands == 3)
                {
                    // We have extra argument which means there is a "use" of FFR here. Restore it back in FFR register.
                    assert(op3Reg != REG_NA);
                    GetEmitter()->emitIns_R(INS_sve_wrffr, emitSize, op3Reg, opt);
                }

                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, REG_ZR, opt);
                break;
            }

            case NI_Sve_SetFfr:
            {
                assert(targetReg == REG_NA);
                GetEmitter()->emitIns_R(ins, emitSize, op1Reg, opt);
                break;
            }
            case NI_Sve_ConditionalExtractAfterLastActiveElementScalar:
            case NI_Sve_ConditionalExtractLastActiveElementScalar:
            {
                opt = emitter::optGetSveInsOpt(emitTypeSize(node->GetSimdBaseType()));

                if (emitter::isGeneralRegisterOrZR(targetReg))
                {
                    assert(varTypeIsIntegralOrI(intrin.baseType));

                    emitSize = varTypeIsLong(intrin.baseType) ? EA_8BYTE : EA_4BYTE;

                    GetEmitter()->emitInsSve_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, opt,
                                                     INS_SCALABLE_OPTS_NONE);

                    // clasta/clastb scalar variants produce 32-bit results for byte/short base types.
                    // Narrow down to the correct type if required.
                    if (varTypeIsSmall(intrin.baseType))
                    {
                        emitAttr castSize = emitActualTypeSize(node->TypeGet());
                        inst_Mov_Extend(intrin.baseType, /* srcInReg */ true, targetReg, targetReg,
                                        /* canSkip */ false, castSize);
                    }
                    break;
                }

                // FP scalars are processed by the INS_SCALABLE_OPTS_WITH_SIMD_SCALAR variant of the instructions
                FALLTHROUGH;
            }
            case NI_Sve_ConditionalExtractAfterLastActiveElement:
            case NI_Sve_ConditionalExtractLastActiveElement:
            {
                assert(emitter::isFloatReg(targetReg));
                assert(varTypeIsFloating(node->gtType) || varTypeIsSIMD(node->gtType));

                GetEmitter()->emitInsSve_R_R_R_R(ins, EA_SCALABLE, targetReg, op1Reg, op2Reg, op3Reg, opt,
                                                 INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
                break;
            }

            case NI_Sve_ExtractAfterLastActiveElementScalar:
            case NI_Sve_ExtractLastActiveElementScalar:
            {
                opt = emitter::optGetSveInsOpt(emitTypeSize(node->GetSimdBaseType()));

                if (emitter::isGeneralRegisterOrZR(targetReg))
                {
                    assert(varTypeIsIntegralOrI(intrin.baseType));

                    emitSize = varTypeIsLong(intrin.baseType) ? EA_8BYTE : EA_4BYTE;

                    GetEmitter()->emitInsSve_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt,
                                                   INS_SCALABLE_OPTS_NONE);

                    // lasta/lastb scalar variants produce 32-bit results for byte/short base types.
                    // Narrow down to the correct type if required.
                    if (varTypeIsSmall(intrin.baseType))
                    {
                        emitAttr castSize = emitActualTypeSize(node->TypeGet());
                        inst_Mov_Extend(intrin.baseType, /* srcInReg */ true, targetReg, targetReg,
                                        /* canSkip */ false, castSize);
                    }
                    break;
                }

                // FP scalars are processed by the INS_SCALABLE_OPTS_WITH_SIMD_SCALAR variant of the instructions
                FALLTHROUGH;
            }
            case NI_Sve_ExtractAfterLastActiveElement:
            case NI_Sve_ExtractLastActiveElement:
            {
                assert(emitter::isFloatReg(targetReg));
                assert(varTypeIsFloating(node->gtType) || varTypeIsSIMD(node->gtType));

                GetEmitter()->emitInsSve_R_R_R(ins, EA_SCALABLE, targetReg, op1Reg, op2Reg, opt,
                                               INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
                break;
            }

            case NI_Sve_TrigonometricMultiplyAddCoefficient:
            case NI_Sve2_AddRotateComplex:
            case NI_Sve2_AddSaturateRotateComplex:
            {
                assert(isRMW);

                HWIntrinsicImmOpHelper helper(this, intrin.op3, node, (targetReg != op1Reg) ? 2 : 1);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    GetEmitter()->emitInsSve_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, helper.ImmValue(), opt);
                }
                break;
            }

            case NI_Sve_MultiplyAddRotateComplexBySelectedScalar:
            case NI_Sve2_MultiplyAddRotateComplexBySelectedScalar:
            case NI_Sve2_MultiplyAddRoundedDoublingSaturateHighRotateComplexBySelectedScalar:
            {
                assert(isRMW);
                assert(hasImmediateOperand);

                // If both immediates are constant, we don't need a jump table
                if (intrin.op4->IsCnsIntOrI() && intrin.op5->IsCnsIntOrI())
                {
                    assert(intrin.op4->isContainedIntOrIImmed() && intrin.op5->isContainedIntOrIImmed());
                    GetEmitter()->emitInsSve_R_R_R_R_I_I(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg,
                                                         intrin.op4->AsIntCon()->IconValue(),
                                                         intrin.op5->AsIntCon()->IconValue(), opt);
                }
                else
                {
                    // Use the helper to generate a table. The table can only use a single lookup value, therefore
                    // the two immediates index and rotation must be combined to a single value
                    assert(!intrin.op4->isContainedIntOrIImmed() && !intrin.op5->isContainedIntOrIImmed());
                    emitAttr scalarSize = emitActualTypeSize(node->GetSimdBaseType());

                    var_types baseType = node->GetSimdBaseType();

                    const unsigned rotMask      = 0b11;
                    const unsigned indexMask    = (baseType == TYP_SHORT || baseType == TYP_USHORT) ? 0b11 : 0b1;
                    const unsigned numIndexBits = genCountBits(indexMask);

                    GetEmitter()->emitIns_R_R_I(INS_lsl, scalarSize, op5Reg, op5Reg, numIndexBits);
                    GetEmitter()->emitIns_R_R_R(INS_orr, scalarSize, op4Reg, op4Reg, op5Reg);

                    const unsigned         upperBound = (rotMask << numIndexBits) | indexMask;
                    HWIntrinsicImmOpHelper helper(this, op4Reg, 0, upperBound, node, (targetReg != op1Reg) ? 2 : 1);

                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        const int     value    = helper.ImmValue();
                        const ssize_t index    = value & indexMask;
                        const ssize_t rotation = value >> numIndexBits;
                        GetEmitter()->emitInsSve_R_R_R_R_I_I(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, index,
                                                             rotation, opt);
                    }

                    GetEmitter()->emitIns_R_R_I(INS_and, scalarSize, op4Reg, op4Reg, indexMask);
                    GetEmitter()->emitIns_R_R_I(INS_lsr, scalarSize, op5Reg, op5Reg, numIndexBits);
                }

                break;
            }

            case NI_Sve2_AddWideningEven:
            {
                var_types returnType = node->AsHWIntrinsic()->GetSimdBaseType();
                var_types op1Type    = node->AsHWIntrinsic()->GetAuxiliaryType();
                if (returnType != op1Type)
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_sve_uaddlb : INS_sve_saddlb;
                }

                GetEmitter()->emitInsSve_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;
            }

            case NI_Sve2_AddWideningOdd:
            {
                var_types returnType = node->AsHWIntrinsic()->GetSimdBaseType();
                var_types op1Type    = node->AsHWIntrinsic()->GetAuxiliaryType();
                if (returnType != op1Type)
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_sve_uaddlt : INS_sve_saddlt;
                }
                GetEmitter()->emitInsSve_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;
            }

            case NI_Sve2_BitwiseClearXor:
            case NI_Sve2_Xor:
                // Always use the lane size D. It's a bitwise operation so this is fine for all integer vector types.
                GetEmitter()->emitInsSve_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, INS_OPTS_SCALABLE_D);
                break;

            case NI_Sve2_BitwiseSelect:
            case NI_Sve2_BitwiseSelectLeftInverted:
            case NI_Sve2_BitwiseSelectRightInverted:
                // op1: select, op2: left, op3: right
                // Always use the lane size D. It's a bitwise operation so this is fine for all integer vector types.
                GetEmitter()->emitInsSve_R_R_R_R(ins, emitSize, targetReg, op2Reg, op3Reg, op1Reg, INS_OPTS_SCALABLE_D);
                break;

            case NI_Sve2_MultiplyAddRotateComplex:
            case NI_Sve2_MultiplyAddRoundedDoublingSaturateHighRotateComplex:
            case NI_Sve2_DotProductRotateComplex:
            {
                assert(isRMW);
                assert(hasImmediateOperand);

                HWIntrinsicImmOpHelper helper(this, intrin.op4, node, (targetReg != op1Reg) ? 2 : 1);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    GetEmitter()->emitInsSve_R_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg,
                                                       helper.ImmValue(), opt);
                }
                break;
            }

            case NI_Sve2_DotProductRotateComplexBySelectedIndex:
            {
                assert(isRMW);
                assert(hasImmediateOperand);

                // If both immediates are constant, we don't need a jump table
                if (intrin.op4->IsCnsIntOrI() && intrin.op5->IsCnsIntOrI())
                {
                    assert(intrin.op4->isContainedIntOrIImmed() && intrin.op5->isContainedIntOrIImmed());
                    GetEmitter()->emitInsSve_R_R_R_R_I_I(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg,
                                                         intrin.op4->AsIntCon()->IconValue(),
                                                         intrin.op5->AsIntCon()->IconValue(), opt);
                }
                else
                {
                    // Use the helper to generate a table. The table can only use a single lookup value, therefore
                    // the two immediates index and rotation must be combined to a single value
                    assert(!intrin.op4->isContainedIntOrIImmed() && !intrin.op5->isContainedIntOrIImmed());
                    emitAttr scalarSize = emitActualTypeSize(node->GetSimdBaseType());

                    var_types baseType = node->GetSimdBaseType();

                    const unsigned rotMask      = 0b11;
                    const unsigned indexMask    = (baseType == TYP_BYTE) ? 0b11 : 0b1;
                    const unsigned numIndexBits = genCountBits(indexMask);

                    GetEmitter()->emitIns_R_R_I(INS_lsl, scalarSize, op5Reg, op5Reg, numIndexBits);
                    GetEmitter()->emitIns_R_R_R(INS_orr, scalarSize, op4Reg, op4Reg, op5Reg);

                    const unsigned         upperBound = (rotMask << numIndexBits) | indexMask;
                    HWIntrinsicImmOpHelper helper(this, op4Reg, 0, upperBound, node, (targetReg != op1Reg) ? 2 : 1);

                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        const int     value    = helper.ImmValue();
                        const ssize_t index    = value & indexMask;
                        const ssize_t rotation = value >> numIndexBits;
                        GetEmitter()->emitInsSve_R_R_R_R_I_I(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, index,
                                                             rotation, opt);
                    }

                    GetEmitter()->emitIns_R_R_I(INS_and, scalarSize, op4Reg, op4Reg, indexMask);
                    GetEmitter()->emitIns_R_R_I(INS_lsr, scalarSize, op5Reg, op5Reg, numIndexBits);
                }

                break;
            }

            case NI_Sve2_SubtractWideningEven:
            {
                var_types returnType = node->AsHWIntrinsic()->GetSimdBaseType();
                var_types op1Type    = node->AsHWIntrinsic()->GetAuxiliaryType();
                if (returnType != op1Type)
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_sve_usublb : INS_sve_ssublb;
                }
                GetEmitter()->emitInsSve_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;
            }

            case NI_Sve2_SubtractWideningOdd:
            {
                var_types returnType = node->AsHWIntrinsic()->GetSimdBaseType();
                var_types op1Type    = node->AsHWIntrinsic()->GetAuxiliaryType();
                if (returnType != op1Type)
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_sve_usublt : INS_sve_ssublt;
                }
                GetEmitter()->emitInsSve_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;
            }

            case NI_SveSha3_BitwiseRotateLeftBy1AndXor:
            {
                opt = INS_OPTS_SCALABLE_D;
                GetEmitter()->emitInsSve_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;
            }

            default:
                unreached();
        }
    }

    genProduceReg(node);
}

#endif // FEATURE_HW_INTRINSICS
