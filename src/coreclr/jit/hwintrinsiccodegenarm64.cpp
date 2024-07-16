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
CodeGen::HWIntrinsicImmOpHelper::HWIntrinsicImmOpHelper(CodeGen* codeGen, GenTree* immOp, GenTreeHWIntrinsic* intrin)
    : codeGen(codeGen)
    , endLabel(nullptr)
    , nonZeroLabel(nullptr)
    , branchTargetReg(REG_NA)
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
    CodeGen* codeGen, regNumber immReg, int immLowerBound, int immUpperBound, GenTreeHWIntrinsic* intrin)
    : codeGen(codeGen)
    , endLabel(nullptr)
    , nonZeroLabel(nullptr)
    , immValue(immLowerBound)
    , immLowerBound(immLowerBound)
    , immUpperBound(immUpperBound)
    , nonConstImmReg(immReg)
    , branchTargetReg(REG_NA)
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
            // Here we assume that each case consists of one arm64 instruction followed by "b endLabel".
            // Since an arm64 instruction is 4 bytes, we branch to AddressOf(beginLabel) + (nonConstImmReg << 3).
            GetEmitter()->emitIns_R_L(INS_adr, EA_8BYTE, beginLabel, branchTargetReg);
            GetEmitter()->emitIns_R_R_R_I(INS_add, EA_8BYTE, branchTargetReg, branchTargetReg, nonConstImmReg, 3,
                                          INS_OPTS_LSL);

            // If the lower bound is non zero we need to adjust the branch target value by subtracting
            // (immLowerBound << 3).
            if (immLowerBound != 0)
            {
                GetEmitter()->emitIns_R_R_I(INS_sub, EA_8BYTE, branchTargetReg, branchTargetReg,
                                            ((ssize_t)immLowerBound << 3));
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
// genHWIntrinsic: Generates the code for a given hardware intrinsic node.
//
// Arguments:
//    node - The hardware intrinsic node
//
void CodeGen::genHWIntrinsic(GenTreeHWIntrinsic* node)
{
    const HWIntrinsic intrin(node);

    // We need to validate that other phases of the compiler haven't introduced unsupported intrinsics
    assert(compiler->compIsaSupportedDebugOnly(HWIntrinsicInfo::lookupIsa(intrin.id)));

    regNumber targetReg = node->GetRegNum();

    regNumber op1Reg = REG_NA;
    regNumber op2Reg = REG_NA;
    regNumber op3Reg = REG_NA;
    regNumber op4Reg = REG_NA;

    switch (intrin.numOperands)
    {
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

    const bool isRMW               = node->isRMWHWIntrinsic(compiler);
    const bool hasImmediateOperand = HWIntrinsicInfo::HasImmediateOperand(intrin.id);

    genConsumeMultiOpOperands(node);

    if (intrin.codeGenIsTableDriven())
    {
        const instruction ins = HWIntrinsicInfo::lookupIns(intrin.id, intrin.baseType);
        assert(ins != INS_invalid);

        if (intrin.category == HW_Category_SIMDByIndexedElement)
        {
            if (hasImmediateOperand)
            {
                if (isRMW)
                {
                    if (targetReg != op1Reg)
                    {
                        assert(targetReg != op2Reg);
                        assert(targetReg != op3Reg);

                        GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);
                    }

                    HWIntrinsicImmOpHelper helper(this, intrin.op4, node);

                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        const int elementIndex = helper.ImmValue();

                        GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op2Reg, op3Reg, elementIndex, opt);
                    }
                }
                else
                {
                    if (intrin.numOperands == 2)
                    {
                        HWIntrinsicImmOpHelper helper(this, intrin.op2, node);

                        for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                        {
                            const int elementIndex = helper.ImmValue();

                            GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op1Reg, elementIndex, opt);
                        }
                    }
                    else
                    {
                        assert(intrin.numOperands == 3);
                        HWIntrinsicImmOpHelper helper(this, intrin.op3, node);

                        for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                        {
                            const int elementIndex = helper.ImmValue();

                            GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, elementIndex, opt);
                        }
                    }
                }
            }
            else
            {
                if (isRMW)
                {
                    if (targetReg != op1Reg)
                    {
                        assert(targetReg != op2Reg);
                        assert(targetReg != op3Reg);

                        GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);
                    }

                    GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op2Reg, op3Reg, 0, opt);
                }
                else
                {
                    GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, 0, opt);
                }
            }
        }
        else if ((intrin.category == HW_Category_ShiftLeftByImmediate) ||
                 (intrin.category == HW_Category_ShiftRightByImmediate))
        {
            assert(hasImmediateOperand);

            auto emitShift = [&](GenTree* op, regNumber reg) {
                HWIntrinsicImmOpHelper helper(this, op, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int shiftAmount = helper.ImmValue();

                    if (shiftAmount == 0)
                    {
                        // TODO: Use emitIns_Mov instead.
                        //       We do not use it currently because it will still elide the 'mov'
                        //       even if 'canSkip' is false. We cannot elide the 'mov' here.
                        GetEmitter()->emitIns_R_R_R(INS_mov, emitTypeSize(node), targetReg, reg, reg);
                    }
                    else
                    {
                        GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, reg, shiftAmount, opt);
                    }
                }
            };

            if (isRMW)
            {
                GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);
                emitShift(intrin.op3, op2Reg);
            }
            else
            {
                emitShift(intrin.op2, op1Reg);
            }
        }
        else if (HWIntrinsicInfo::HasEnumOperand(intrin.id))
        {
            assert(hasImmediateOperand);

            switch (intrin.numOperands)
            {
                case 1:
                {
                    HWIntrinsicImmOpHelper helper(this, intrin.op1, node);
                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        const insSvePattern pattern = (insSvePattern)helper.ImmValue();
                        GetEmitter()->emitIns_R_PATTERN(ins, emitSize, targetReg, opt, pattern);
                    }
                };
                break;

                default:
                    unreached();
            }
        }
        else if (intrin.numOperands >= 2 && intrin.op2->IsEmbMaskOp())
        {
            // Handle case where op2 is operation that needs embedded mask
            GenTree* op2 = intrin.op2;
            assert(intrin.id == NI_Sve_ConditionalSelect);
            assert(op2->OperIsHWIntrinsic());
            assert(op2->isContained());

            // Get the registers and intrinsics that needs embedded mask
            const HWIntrinsic intrinEmbMask(op2->AsHWIntrinsic());
            instruction       insEmbMask = HWIntrinsicInfo::lookupIns(intrinEmbMask.id, intrinEmbMask.baseType);
            const bool        instrIsRMW = op2->isRMWHWIntrinsic(compiler);

            regNumber maskReg       = op1Reg;
            regNumber embMaskOp1Reg = REG_NA;
            regNumber embMaskOp2Reg = REG_NA;
            regNumber embMaskOp3Reg = REG_NA;
            regNumber embMaskOp4Reg = REG_NA;
            regNumber falseReg      = op3Reg;

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

            // Shared code for setting up embedded mask arg for intrinsics with 3+ operands
            auto emitEmbeddedMaskSetup = [&] {
                if (intrin.op3->IsVectorZero())
                {
                    // If `falseReg` is zero, then move the first operand of `intrinEmbMask` in the
                    // destination using /Z.

                    assert(targetReg != embMaskOp2Reg);
                    assert(intrin.op3->isContained() || !intrin.op1->IsMaskAllBitsSet());
                    GetEmitter()->emitInsSve_R_R_R(INS_sve_movprfx, emitSize, targetReg, maskReg, embMaskOp1Reg, opt);
                }
                else
                {
                    // Below are the considerations we need to handle:
                    //
                    // targetReg == falseReg && targetReg == embMaskOp1Reg
                    //      fmla    Zd, P/m, Zn, Zm
                    //
                    // targetReg == falseReg && targetReg != embMaskOp1Reg
                    //      movprfx target, P/m, embMaskOp1Reg
                    //      fmla    target, P/m, embMaskOp2Reg, embMaskOp3Reg
                    //
                    // targetReg != falseReg && targetReg == embMaskOp1Reg
                    //      sel     target, P/m, embMaskOp1Reg, falseReg
                    //      fmla    target, P/m, embMaskOp2Reg, embMaskOp3Reg
                    //
                    // targetReg != falseReg && targetReg != embMaskOp1Reg
                    //      sel     target, P/m, embMaskOp1Reg, falseReg
                    //      fmla    target, P/m, embMaskOp2Reg, embMaskOp3Reg
                    //
                    // Note that, we just check if the targetReg/falseReg or targetReg/embMaskOp1Reg
                    // coincides or not.

                    if (targetReg != falseReg)
                    {
                        if (falseReg == embMaskOp1Reg)
                        {
                            // If falseReg value and embMaskOp1Reg value are same, then just mov the value
                            // to the target.

                            GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, embMaskOp1Reg,
                                                      /* canSkip */ true);
                        }
                        else
                        {
                            // If falseReg value is not present in targetReg yet, move the inactive lanes
                            // into the targetReg using `sel`. Since this is RMW, the active lanes should
                            // have the value from embMaskOp1Reg

                            GetEmitter()->emitInsSve_R_R_R_R(INS_sve_sel, emitSize, targetReg, maskReg, embMaskOp1Reg,
                                                             falseReg, opt);
                        }
                    }
                    else if (targetReg != embMaskOp1Reg)
                    {
                        // If target already contains the values of `falseReg`, just merge the lanes from
                        // `embMaskOp1Reg`, again because this is RMW semantics.

                        GetEmitter()->emitInsSve_R_R_R(INS_sve_movprfx, emitSize, targetReg, maskReg, embMaskOp1Reg,
                                                       opt, INS_SCALABLE_OPTS_PREDICATE_MERGE);
                    }
                }
            };

            switch (intrinEmbMask.numOperands)
            {
                case 1:
                {
                    assert(!instrIsRMW);

                    // Special handling for ConvertTo* APIs
                    // Just need to change the opt here.
                    insOpts embOpt = opt;
                    switch (intrinEmbMask.id)
                    {
                        case NI_Sve_ConvertToInt32:
                        case NI_Sve_ConvertToUInt32:
                        {
                            embOpt = emitTypeSize(intrinEmbMask.baseType) == EA_8BYTE ? INS_OPTS_D_TO_S
                                                                                      : INS_OPTS_SCALABLE_S;
                            break;
                        }

                        case NI_Sve_ConvertToInt64:
                        case NI_Sve_ConvertToUInt64:
                        {
                            embOpt = emitTypeSize(intrinEmbMask.baseType) == EA_4BYTE ? INS_OPTS_S_TO_D
                                                                                      : INS_OPTS_SCALABLE_D;
                            break;
                        }
                        default:
                            break;
                    }

                    if (targetReg != falseReg)
                    {
                        // If targetReg is not the same as `falseReg` then need to move
                        // the `falseReg` to `targetReg`.

                        if (intrin.op3->isContained())
                        {
                            assert(intrin.op3->IsVectorZero());
                            if (intrin.op1->isContained() || intrin.op1->IsMaskAllBitsSet())
                            {
                                // We already skip importing ConditionalSelect if op1 == trueAll, however
                                // if we still see it here, it is because we wrapped the predicated instruction
                                // inside ConditionalSelect.
                                // As such, no need to move the `falseReg` to `targetReg`
                                // because the predicated instruction will eventually set it.
                            }
                            else
                            {
                                // If falseValue is zero, just zero out those lanes of targetReg using `movprfx`
                                // and /Z
                                GetEmitter()->emitIns_R_R_R(INS_sve_movprfx, emitSize, targetReg, maskReg, targetReg,
                                                            opt);
                            }
                        }
                        else if (emitter::isVectorRegister(embMaskOp1Reg) && (targetReg == embMaskOp1Reg))
                        {
                            // target != falseValue, but we do not want to overwrite target with `embMaskOp1Reg`.
                            // We will first do the predicate operation and then do conditionalSelect inactive
                            // elements from falseValue

                            // We cannot use use `movprfx` here to move falseReg to targetReg because that will
                            // overwrite the value of embMaskOp1Reg which is present in targetReg.
                            GetEmitter()->emitIns_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg,
                                                        embOpt);

                            GetEmitter()->emitIns_R_R_R_R(INS_sve_sel, emitSize, targetReg, maskReg, targetReg,
                                                          falseReg, opt);
                            break;
                        }
                        else
                        {
                            // At this point, target != embMaskOp1Reg != falseReg, so just go ahead
                            // and move the falseReg unpredicated into targetReg.
                            GetEmitter()->emitIns_R_R(INS_sve_movprfx, EA_SCALABLE, targetReg, falseReg);
                        }
                    }

                    GetEmitter()->emitIns_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg, embOpt);
                    break;
                }

                case 2:
                {
                    if (!instrIsRMW)
                    {
                        // Perform the actual "predicated" operation so that `embMaskOp1Reg` is the first operand
                        // and `embMaskOp2Reg` is the second operand.
                        GetEmitter()->emitIns_R_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp1Reg,
                                                      embMaskOp2Reg, opt);
                        break;
                    }

                    insScalableOpts sopt     = INS_SCALABLE_OPTS_NONE;
                    bool            hasShift = false;

                    switch (intrinEmbMask.id)
                    {
                        case NI_Sve_ShiftLeftLogical:
                        case NI_Sve_ShiftRightArithmetic:
                        case NI_Sve_ShiftRightLogical:
                        {
                            const emitAttr op2Size = emitTypeSize(op2->AsHWIntrinsic()->GetAuxiliaryType());
                            if (op2Size != emitTypeSize(intrinEmbMask.baseType))
                            {
                                assert(emitter::optGetSveInsOpt(op2Size) == INS_OPTS_SCALABLE_D);
                                sopt = INS_SCALABLE_OPTS_WIDE;
                            }
                            break;
                        }

                        case NI_Sve_ShiftRightArithmeticForDivide:
                            hasShift = true;
                            break;

                        default:
                            break;
                    }

                    auto emitInsHelper = [&](regNumber reg1, regNumber reg2, regNumber reg3) {
                        if (hasShift)
                        {
                            HWIntrinsicImmOpHelper helper(this, intrinEmbMask.op2, op2->AsHWIntrinsic());
                            for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                            {
                                GetEmitter()->emitInsSve_R_R_I(insEmbMask, emitSize, reg1, reg2, helper.ImmValue(), opt,
                                                               sopt);
                            }
                        }
                        else
                        {
                            GetEmitter()->emitIns_R_R_R(insEmbMask, emitSize, reg1, reg2, reg3, opt, sopt);
                        }
                    };

                    if (intrin.op3->IsVectorZero())
                    {
                        // If `falseReg` is zero, then move the first operand of `intrinEmbMask` in the
                        // destination using /Z.

                        assert(targetReg != embMaskOp2Reg);
                        GetEmitter()->emitIns_R_R_R(INS_sve_movprfx, emitSize, targetReg, maskReg, embMaskOp1Reg, opt);

                        // Finally, perform the actual "predicated" operation so that `targetReg` is the first operand
                        // and `embMaskOp2Reg` is the second operand.
                        emitInsHelper(targetReg, maskReg, embMaskOp2Reg);
                    }
                    else if (targetReg != falseReg)
                    {
                        // If `targetReg` and `falseReg` are not same, then we need to move it to `targetReg` first
                        // so the `insEmbMask` operation can be merged on top of it.

                        if (falseReg != embMaskOp1Reg)
                        {
                            // At the point, targetReg != embMaskOp1Reg != falseReg
                            if (HWIntrinsicInfo::IsOptionalEmbeddedMaskedOperation(intrinEmbMask.id))
                            {
                                // If the embedded instruction supports optional mask operation, use the "unpredicated"
                                // version of the instruction, followed by "sel" to select the active lanes.
                                emitInsHelper(targetReg, embMaskOp1Reg, embMaskOp2Reg);
                            }
                            else
                            {
                                // If the instruction just has "predicated" version, then move the "embMaskOp1Reg"
                                // into targetReg. Next, do the predicated operation on the targetReg and last,
                                // use "sel" to select the active lanes based on mask, and set inactive lanes
                                // to falseReg.

                                assert(targetReg != embMaskOp2Reg);
                                assert(HWIntrinsicInfo::IsEmbeddedMaskedOperation(intrinEmbMask.id));

                                GetEmitter()->emitIns_R_R(INS_sve_movprfx, EA_SCALABLE, targetReg, embMaskOp1Reg);

                                emitInsHelper(targetReg, maskReg, embMaskOp2Reg);
                            }

                            GetEmitter()->emitIns_R_R_R_R(INS_sve_sel, emitSize, targetReg, maskReg, targetReg,
                                                          falseReg, opt);
                            break;
                        }
                        else if (targetReg != embMaskOp1Reg)
                        {
                            // embMaskOp1Reg is same as `falseReg`, but not same as `targetReg`. Move the
                            // `embMaskOp1Reg` i.e. `falseReg` in `targetReg`, using "unpredicated movprfx", so the
                            // subsequent `insEmbMask` operation can be merged on top of it.
                            GetEmitter()->emitIns_R_R(INS_sve_movprfx, EA_SCALABLE, targetReg, falseReg);
                        }

                        // Finally, perform the actual "predicated" operation so that `targetReg` is the first operand
                        // and `embMaskOp2Reg` is the second operand.
                        emitInsHelper(targetReg, maskReg, embMaskOp2Reg);
                    }
                    else
                    {
                        // Just perform the actual "predicated" operation so that `targetReg` is the first operand
                        // and `embMaskOp2Reg` is the second operand.
                        emitInsHelper(targetReg, maskReg, embMaskOp2Reg);
                    }

                    break;
                }

                case 3:
                {
                    assert(instrIsRMW);

                    if (HWIntrinsicInfo::IsFmaIntrinsic(intrinEmbMask.id))
                    {
                        assert(falseReg != embMaskOp3Reg);
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

                    emitEmbeddedMaskSetup();

                    // Finally, perform the desired operation.
                    if (HWIntrinsicInfo::HasImmediateOperand(intrinEmbMask.id))
                    {
                        HWIntrinsicImmOpHelper helper(this, intrinEmbMask.op3, op2->AsHWIntrinsic());
                        for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                        {
                            GetEmitter()->emitInsSve_R_R_R_I(insEmbMask, emitSize, targetReg, maskReg, embMaskOp2Reg,
                                                             helper.ImmValue(), opt);
                        }
                    }
                    else
                    {
                        assert(HWIntrinsicInfo::IsFmaIntrinsic(intrinEmbMask.id));
                        GetEmitter()->emitInsSve_R_R_R_R(insEmbMask, emitSize, targetReg, maskReg, embMaskOp2Reg,
                                                         embMaskOp3Reg, opt);
                    }

                    break;
                }

                case 4:
                {
                    assert(instrIsRMW);
                    assert(intrinEmbMask.op4->isContained() == (embMaskOp4Reg == REG_NA));
                    assert(HWIntrinsicInfo::HasImmediateOperand(intrinEmbMask.id));

                    emitEmbeddedMaskSetup();

                    HWIntrinsicImmOpHelper helper(this, intrinEmbMask.op4, op2->AsHWIntrinsic());
                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        GetEmitter()->emitInsSve_R_R_R_R_I(insEmbMask, emitSize, targetReg, maskReg, embMaskOp2Reg,
                                                           embMaskOp3Reg, helper.ImmValue(), opt);
                    }

                    break;
                }

                default:
                    unreached();
            }
        }
        else
        {
            assert(!hasImmediateOperand);

            switch (intrin.numOperands)
            {
                case 0:
                    GetEmitter()->emitIns_R(ins, emitSize, targetReg, opt);
                    break;
                case 1:
                    GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg, opt);
                    break;

                case 2:
                    // This handles optimizations for instructions that have
                    // an implicit 'zero' vector of what would be the second operand.
                    if (HWIntrinsicInfo::SupportsContainment(intrin.id) && intrin.op2->isContained() &&
                        intrin.op2->IsVectorZero())
                    {
                        GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg, opt);
                    }
                    else if (HWIntrinsicInfo::IsScalable(intrin.id))
                    {
                        assert(!node->IsEmbMaskOp());
                        if (HWIntrinsicInfo::IsExplicitMaskedOperation(intrin.id))
                        {
                            if (isRMW)
                            {
                                if (targetReg != op2Reg)
                                {
                                    assert(targetReg != op1Reg);

                                    GetEmitter()->emitIns_Mov(ins_Move_Extend(intrin.op2->TypeGet(), false),
                                                              emitTypeSize(node), targetReg, op2Reg,
                                                              /* canSkip */ true);
                                }

                                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg, opt);
                            }
                            else
                            {
                                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                            }
                        }
                        else
                        {
                            // This generates an unpredicated version
                            // Implicitly predicated should be taken care above `intrin.op2->IsEmbMaskOp()`
                            GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                        }
                    }
                    else if (isRMW)
                    {
                        if (targetReg != op1Reg)
                        {
                            assert(targetReg != op2Reg);

                            GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg,
                                                      /* canSkip */ true);
                        }
                        GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op2Reg, opt);
                    }
                    else
                    {
                        GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                    }
                    break;

                case 3:
                    if (isRMW)
                    {
                        if (HWIntrinsicInfo::IsExplicitMaskedOperation(intrin.id))
                        {
                            if (targetReg != op2Reg)
                            {
                                assert(targetReg != op1Reg);
                                assert(targetReg != op3Reg);

                                GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op2Reg,
                                                          /* canSkip */ true);
                            }

                            GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op3Reg, opt);
                        }
                        else
                        {
                            if (targetReg != op1Reg)
                            {
                                assert(targetReg != op2Reg);
                                assert(targetReg != op3Reg);

                                GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg,
                                                          /* canSkip */ true);
                            }
                            GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op2Reg, op3Reg, opt);
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
                if (intrin.op1->TypeGet() == TYP_SIMD8)
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_uaddl : INS_saddl;
                }
                else
                {
                    assert(intrin.op1->TypeGet() == TYP_SIMD16);
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_uaddw : INS_saddw;
                }
                break;

            case NI_AdvSimd_SubtractWideningLower:
                assert(varTypeIsIntegral(intrin.baseType));
                if (intrin.op1->TypeGet() == TYP_SIMD8)
                {
                    ins = varTypeIsUnsigned(intrin.baseType) ? INS_usubl : INS_ssubl;
                }
                else
                {
                    assert(intrin.op1->TypeGet() == TYP_SIMD16);
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
                ins = HWIntrinsicInfo::lookupIns(intrin.id, node->GetAuxiliaryType());
                break;

            default:
                ins = HWIntrinsicInfo::lookupIns(intrin.id, intrin.baseType);
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

            case NI_AdvSimd_AbsoluteCompareLessThan:
            case NI_AdvSimd_AbsoluteCompareLessThanOrEqual:
            case NI_AdvSimd_CompareLessThan:
            case NI_AdvSimd_CompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThan:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThanScalar:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_AbsoluteCompareLessThanOrEqualScalar:
            case NI_AdvSimd_Arm64_CompareLessThan:
            case NI_AdvSimd_Arm64_CompareLessThanScalar:
            case NI_AdvSimd_Arm64_CompareLessThanOrEqual:
            case NI_AdvSimd_Arm64_CompareLessThanOrEqualScalar:
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
                assert(isRMW);

                GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);

                if (intrin.op3->isContainedFltOrDblImmed())
                {
                    assert(intrin.op2->isContainedIntOrIImmed());
                    assert(intrin.op2->AsIntCon()->gtIconVal == 0);

                    const double dataValue = intrin.op3->AsDblCon()->DconValue();
                    GetEmitter()->emitIns_R_F(INS_fmov, emitSize, targetReg, dataValue, opt);
                }
                else
                {
                    assert(targetReg != op3Reg);

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
                }
                break;

            case NI_AdvSimd_InsertScalar:
            {
                assert(isRMW);
                if (targetReg != op1Reg)
                {
                    assert(targetReg != op3Reg);

                    GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);
                }

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
                if (targetReg != op1Reg)
                {
                    assert(targetReg != op3Reg);

                    GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);
                }

                const int resultIndex = (int)intrin.op2->AsIntCon()->gtIconVal;
                const int valueIndex  = (int)intrin.op4->AsIntCon()->gtIconVal;
                GetEmitter()->emitIns_R_R_I_I(ins, emitSize, targetReg, op3Reg, resultIndex, valueIndex, opt);
            }
            break;

            case NI_AdvSimd_LoadAndInsertScalar:
            {
                assert(isRMW);
                if (targetReg != op1Reg)
                {
                    assert(targetReg != op3Reg);

                    GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);
                }

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

                    if (targetFieldReg != op1FieldReg)
                    {
                        GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(fieldNode), targetFieldReg, op1FieldReg,
                                                  /* canSkip */ true);
                    }
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

            case NI_Vector64_CreateScalarUnsafe:
            case NI_Vector128_CreateScalarUnsafe:
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
                        const ssize_t dataValue = intrin.op1->AsIntCon()->gtIconVal;
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

                    GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);
                    GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op2Reg, opt);
                }
                else
                {
                    GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                }
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
                    const ssize_t dataValue = intrin.op1->AsIntCon()->gtIconVal;
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

                assert(node->GetMultiRegCount(compiler) == (unsigned int)GetEmitter()->insGetSveReg1ListSize(ins));

                regNumber argReg = targetReg;
                for (unsigned int i = 0; i < node->GetMultiRegCount(compiler); i++)
                {
                    assert(argReg == node->GetRegNumByIdx(i));
                    argReg = getNextSIMDRegWithWraparound(argReg);
                }
#endif // DEBUG
                GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op2Reg, 0, opt);
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

            case NI_Sve_PrefetchBytes:
            case NI_Sve_PrefetchInt16:
            case NI_Sve_PrefetchInt32:
            case NI_Sve_PrefetchInt64:
            {
                assert(hasImmediateOperand);
                assert(HWIntrinsicInfo::HasEnumOperand(intrin.id));
                HWIntrinsicImmOpHelper helper(this, intrin.op3, node);
                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const insSvePrfop prfop = (insSvePrfop)helper.ImmValue();
                    GetEmitter()->emitIns_PRFOP_R_R_I(ins, emitSize, prfop, op1Reg, op2Reg, 0);
                }
                break;
            }

            case NI_Vector64_ToVector128:
                GetEmitter()->emitIns_Mov(ins, emitSize, targetReg, op1Reg, /* canSkip */ false);
                break;

            case NI_Vector64_ToVector128Unsafe:
            case NI_Vector128_AsVector128Unsafe:
            case NI_Vector128_GetLower:
                GetEmitter()->emitIns_Mov(ins, emitSize, targetReg, op1Reg, /* canSkip */ true);
                break;

            case NI_Vector64_GetElement:
            case NI_Vector128_GetElement:
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

            case NI_Vector128_GetUpper:
            {
                const int byteIndex = 8;
                GetEmitter()->emitIns_R_R_R_I(ins, emitSize, targetReg, op1Reg, op1Reg, byteIndex, INS_OPTS_16B);
                break;
            }

            case NI_Vector128_AsVector3:
            {
                // AsVector3 can be a no-op when it's already in the right register, otherwise
                // we just need to move the value over. Vector3 operations will themselves mask
                // out the upper element when it's relevant, so it's not worth us spending extra
                // cycles doing so here.

                GetEmitter()->emitIns_Mov(ins, emitSize, targetReg, op1Reg, /* canSkip */ true);
                break;
            }

            case NI_Vector64_ToScalar:
            case NI_Vector128_ToScalar:
            {
                if ((varTypeIsFloating(intrin.baseType) && (targetReg == op1Reg)))
                {
                    // no-op if vector is float/double and targetReg == op1Reg
                    break;
                }

                GetEmitter()->emitIns_R_R_I(ins, emitTypeSize(intrin.baseType), targetReg, op1Reg, /* imm */ 0,
                                            INS_OPTS_NONE);
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

                if (targetReg != op1Reg)
                {
                    assert(targetReg != op3Reg);
                    GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);
                }
                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op2Reg, op3Reg, opt);
                break;
            }

            case NI_ArmBase_Arm64_MultiplyLongAdd:
            case NI_ArmBase_Arm64_MultiplyLongSub:
                assert(opt == INS_OPTS_NONE);
                GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg);
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

            case NI_Sve_CreateTrueMaskAll:
                // Must use the pattern variant, as the non-pattern varient is SVE2.1.
                GetEmitter()->emitIns_R_PATTERN(ins, emitSize, targetReg, opt, SVE_PATTERN_ALL);
                break;

            case NI_Sve_CreateWhileLessThanMask8Bit:
            case NI_Sve_CreateWhileLessThanMask16Bit:
            case NI_Sve_CreateWhileLessThanMask32Bit:
            case NI_Sve_CreateWhileLessThanMask64Bit:
            {
                // Emit size and instruction is based on the scalar operands.
                var_types auxType = node->GetAuxiliaryType();
                emitSize          = emitActualTypeSize(auxType);
                if (varTypeIsUnsigned(auxType))
                {
                    ins = INS_sve_whilelo;
                }

                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;
            }

            case NI_Sve_CreateWhileLessThanOrEqualMask8Bit:
            case NI_Sve_CreateWhileLessThanOrEqualMask16Bit:
            case NI_Sve_CreateWhileLessThanOrEqualMask32Bit:
            case NI_Sve_CreateWhileLessThanOrEqualMask64Bit:
            {
                // Emit size and instruction is based on the scalar operands.
                var_types auxType = node->GetAuxiliaryType();
                emitSize          = emitActualTypeSize(auxType);
                if (varTypeIsUnsigned(auxType))
                {
                    ins = INS_sve_whilels;
                }

                GetEmitter()->emitIns_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, opt);
                break;
            }

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
            {
                if (!varTypeIsSIMD(intrin.op2->gtType))
                {
                    // GatherVector...(Vector<T> mask, T* address, Vector<T2> indices)

                    assert(intrin.numOperands == 3);
                    emitAttr        baseSize = emitActualTypeSize(intrin.baseType);
                    insScalableOpts sopt     = INS_SCALABLE_OPTS_NONE;

                    if (baseSize == EA_8BYTE)
                    {
                        // Index is multiplied.
                        sopt = (ins == INS_sve_ld1b || ins == INS_sve_ld1sb) ? INS_SCALABLE_OPTS_NONE
                                                                             : INS_SCALABLE_OPTS_LSL_N;
                    }
                    else
                    {
                        // Index is sign or zero extended to 64bits, then multiplied.
                        assert(baseSize == EA_4BYTE);
                        opt = varTypeIsUnsigned(node->GetAuxiliaryType()) ? INS_OPTS_SCALABLE_S_UXTW
                                                                          : INS_OPTS_SCALABLE_S_SXTW;

                        sopt = (ins == INS_sve_ld1b || ins == INS_sve_ld1sb) ? INS_SCALABLE_OPTS_NONE
                                                                             : INS_SCALABLE_OPTS_MOD_N;
                    }

                    GetEmitter()->emitIns_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, opt, sopt);
                }
                else
                {
                    // GatherVector...(Vector<T> mask, Vector<T2> addresses)

                    assert(intrin.numOperands == 2);
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

            case NI_Sve_ReverseElement:
                // Use non-predicated version explicitly
                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg, opt);
                break;

            case NI_Sve_Scatter:
            case NI_Sve_Scatter16BitNarrowing:
            case NI_Sve_Scatter16BitWithByteOffsetsNarrowing:
            case NI_Sve_Scatter32BitNarrowing:
            case NI_Sve_Scatter32BitWithByteOffsetsNarrowing:
            case NI_Sve_Scatter8BitNarrowing:
            case NI_Sve_Scatter8BitWithByteOffsetsNarrowing:
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
                if (targetReg != op1Reg)
                {
                    assert(targetReg != op2Reg);
                    assert(targetReg != op3Reg);
                    GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);
                }

                if (intrin.op2->IsCnsIntOrI() && intrin.op3->IsCnsIntOrI())
                {
                    // Both immediates are constant, emit the intruction.

                    assert(intrin.op2->isContainedIntOrIImmed() && intrin.op3->isContainedIntOrIImmed());
                    int           scale   = (int)intrin.op2->AsIntCon()->gtIconVal;
                    insSvePattern pattern = (insSvePattern)intrin.op3->AsIntCon()->gtIconVal;
                    GetEmitter()->emitIns_R_PATTERN_I(ins, emitSize, targetReg, pattern, scale, opt);
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
                    HWIntrinsicImmOpHelper helper(this, op2Reg, 0, 511, node);
                    for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                    {
                        // Extract scale and pattern from the immediate
                        const int           value   = helper.ImmValue();
                        const int           scale   = (value & 0xF) + 1;
                        const insSvePattern pattern = (insSvePattern)(value >> 4);
                        GetEmitter()->emitIns_R_PATTERN_I(ins, emitSize, targetReg, pattern, scale, opt);
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
                // RMW semantics
                if (targetReg != op1Reg)
                {
                    assert(targetReg != op2Reg);
                    GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg, /* canSkip */ true);
                }

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

                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op2Reg, opt);
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

                if (targetReg != op1Reg)
                {
                    assert(targetReg != op2Reg);

                    GetEmitter()->emitIns_R_R(INS_sve_movprfx, EA_SCALABLE, targetReg, op1Reg);
                }

                HWIntrinsicImmOpHelper helper(this, intrin.op3, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    const int elementIndex = helper.ImmValue();
                    const int byteIndex    = genTypeSize(intrin.baseType) * elementIndex;

                    GetEmitter()->emitIns_R_R_I(ins, emitSize, targetReg, op2Reg, byteIndex, INS_OPTS_SCALABLE_B);
                }
                break;
            }

            case NI_Sve_InsertIntoShiftedVector:
            {
                assert(isRMW);
                assert(emitter::isFloatReg(op2Reg) == varTypeIsFloating(intrin.baseType));
                if (targetReg != op1Reg)
                {
                    assert(targetReg != op2Reg);
                    GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op1Reg,
                                              /* canSkip */ true);
                }

                GetEmitter()->emitInsSve_R_R(ins, emitSize, targetReg, op2Reg, opt);
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
            {
                GetEmitter()->emitInsSve_R_R_R_R(ins, emitSize, targetReg, op1Reg, op2Reg, op3Reg, INS_OPTS_SCALABLE_B);
                break;
            }

            case NI_Sve_CreateMaskForFirstActiveElement:
            {
                assert(isRMW);
                assert(HWIntrinsicInfo::IsExplicitMaskedOperation(intrin.id));

                if (targetReg != op2Reg)
                {
                    assert(targetReg != op1Reg);
                    GetEmitter()->emitIns_Mov(INS_sve_mov, emitTypeSize(node), targetReg, op2Reg, /* canSkip */ true);
                }

                GetEmitter()->emitIns_R_R(ins, emitSize, targetReg, op1Reg, INS_OPTS_SCALABLE_B);
                break;
            }

            case NI_Sve_ConditionalExtractAfterLastActiveElementScalar:
            case NI_Sve_ConditionalExtractLastActiveElementScalar:
            {
                opt = emitter::optGetSveInsOpt(emitTypeSize(node->GetSimdBaseType()));

                if (emitter::isGeneralRegisterOrZR(targetReg))
                {
                    assert(varTypeIsIntegralOrI(intrin.baseType));

                    emitSize = emitTypeSize(node);

                    if (targetReg != op2Reg)
                    {
                        assert(targetReg != op1Reg);
                        assert(targetReg != op3Reg);
                        GetEmitter()->emitIns_Mov(INS_mov, emitSize, targetReg, op2Reg,
                                                  /* canSkip */ true);
                    }

                    GetEmitter()->emitInsSve_R_R_R(ins, emitSize, targetReg, op1Reg, op3Reg, opt,
                                                   INS_SCALABLE_OPTS_NONE);
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

                if (targetReg != op2Reg)
                {
                    assert(targetReg != op1Reg);
                    assert(targetReg != op3Reg);
                    GetEmitter()->emitIns_Mov(INS_mov, emitTypeSize(node), targetReg, op2Reg,
                                              /* canSkip */ true);
                }
                GetEmitter()->emitInsSve_R_R_R(ins, EA_SCALABLE, targetReg, op1Reg, op3Reg, opt,
                                               INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
                break;
            }

            case NI_Sve_TrigonometricMultiplyAddCoefficient:
            {
                assert(isRMW);

                if (targetReg != op1Reg)
                {
                    assert(targetReg != op2Reg);

                    GetEmitter()->emitInsSve_R_R(INS_sve_movprfx, EA_SCALABLE, targetReg, op1Reg);
                }

                HWIntrinsicImmOpHelper helper(this, intrin.op3, node);

                for (helper.EmitBegin(); !helper.Done(); helper.EmitCaseEnd())
                {
                    GetEmitter()->emitInsSve_R_R_I(ins, emitSize, targetReg, op2Reg, helper.ImmValue(), opt);
                }
                break;
            }

            default:
                unreached();
        }
    }

    genProduceReg(node);
}

#endif // FEATURE_HW_INTRINSICS
