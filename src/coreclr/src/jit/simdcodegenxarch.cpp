// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        Amd64 SIMD Code Generator                          XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator.

#ifdef _TARGET_AMD64_
#include "emit.h"
#include "codegen.h"
#include "lower.h"
#include "gcinfo.h"
#include "gcinfoencoder.h"

#ifdef FEATURE_SIMD

// Instruction immediates

// Insertps:
// - bits 6 and 7 of the immediate indicate which source item to select (0..3)
// - bits 4 and 5 of the immediate indicate which target item to insert into (0..3)
// - bits 0 to 3 of the immediate indicate which target item to zero
#define INSERTPS_SOURCE_SELECT(i) (i << 6)
#define INSERTPS_TARGET_SELECT(i) (i << 4)
#define INSERTPS_ZERO(i) (1 << i)

// getOpForSIMDIntrinsic: return the opcode for the given SIMD Intrinsic
//
// Arguments:
//   intrinsicId    -   SIMD intrinsic Id
//   baseType       -   Base type of the SIMD vector
//   immed          -   Out param. Any immediate byte operand that needs to be passed to SSE2 opcode
//
//
// Return Value:
//   Instruction (op) to be used, and immed is set if instruction requires an immediate operand.
//
instruction CodeGen::getOpForSIMDIntrinsic(SIMDIntrinsicID intrinsicId, var_types baseType, unsigned* ival /*=nullptr*/)
{
    // Minimal required instruction set is SSE2.
    assert(compiler->canUseSSE2());

    instruction result = INS_invalid;
    switch (intrinsicId)
    {
        case SIMDIntrinsicInit:
            if (compiler->canUseAVX())
            {
                // AVX supports broadcast instructions to populate YMM reg with a single float/double value from memory.
                // AVX2 supports broadcast instructions to populate YMM reg with a single value from memory or mm reg.
                // If we decide to use AVX2 only, we can remove this assert.
                if ((compiler->opts.eeFlags & CORJIT_FLG_USE_AVX2) == 0)
                {
                    assert(baseType == TYP_FLOAT || baseType == TYP_DOUBLE);
                }
                switch (baseType)
                {
                    case TYP_FLOAT:
                        result = INS_vbroadcastss;
                        break;
                    case TYP_DOUBLE:
                        result = INS_vbroadcastsd;
                        break;
                    case TYP_ULONG:
                        __fallthrough;
                    case TYP_LONG:
                        result = INS_vpbroadcastq;
                        break;
                    case TYP_UINT:
                        __fallthrough;
                    case TYP_INT:
                        result = INS_vpbroadcastd;
                        break;
                    case TYP_CHAR:
                        __fallthrough;
                    case TYP_SHORT:
                        result = INS_vpbroadcastw;
                        break;
                    case TYP_UBYTE:
                        __fallthrough;
                    case TYP_BYTE:
                        result = INS_vpbroadcastb;
                        break;
                    default:
                        unreached();
                }
                break;
            }
            // For SSE, SIMDIntrinsicInit uses the same instruction as the SIMDIntrinsicShuffleSSE2 intrinsic.
            __fallthrough;
        case SIMDIntrinsicShuffleSSE2:
            if (baseType == TYP_FLOAT)
            {
                result = INS_shufps;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_shufpd;
            }
            else if (baseType == TYP_INT || baseType == TYP_UINT)
            {
                result = INS_pshufd;
            }
            else if (baseType == TYP_LONG || baseType == TYP_ULONG)
            {
                // We don't have a seperate SSE2 instruction and will
                // use the instruction meant for doubles since it is
                // of the same size as a long.
                result = INS_shufpd;
            }
            break;

        case SIMDIntrinsicSqrt:
            if (baseType == TYP_FLOAT)
            {
                result = INS_sqrtps;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_sqrtpd;
            }
            else
            {
                unreached();
            }
            break;

        case SIMDIntrinsicAdd:
            if (baseType == TYP_FLOAT)
            {
                result = INS_addps;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_addpd;
            }
            else if (baseType == TYP_INT || baseType == TYP_UINT)
            {
                result = INS_paddd;
            }
            else if (baseType == TYP_CHAR || baseType == TYP_SHORT)
            {
                result = INS_paddw;
            }
            else if (baseType == TYP_UBYTE || baseType == TYP_BYTE)
            {
                result = INS_paddb;
            }
            else if (baseType == TYP_LONG || baseType == TYP_ULONG)
            {
                result = INS_paddq;
            }
            break;

        case SIMDIntrinsicSub:
            if (baseType == TYP_FLOAT)
            {
                result = INS_subps;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_subpd;
            }
            else if (baseType == TYP_INT || baseType == TYP_UINT)
            {
                result = INS_psubd;
            }
            else if (baseType == TYP_CHAR || baseType == TYP_SHORT)
            {
                result = INS_psubw;
            }
            else if (baseType == TYP_UBYTE || baseType == TYP_BYTE)
            {
                result = INS_psubb;
            }
            else if (baseType == TYP_LONG || baseType == TYP_ULONG)
            {
                result = INS_psubq;
            }
            break;

        case SIMDIntrinsicMul:
            if (baseType == TYP_FLOAT)
            {
                result = INS_mulps;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_mulpd;
            }
            else if (baseType == TYP_SHORT)
            {
                result = INS_pmullw;
            }
            else if (compiler->canUseAVX())
            {
                if (baseType == TYP_INT)
                {
                    result = INS_pmulld;
                }
            }
            break;

        case SIMDIntrinsicDiv:
            if (baseType == TYP_FLOAT)
            {
                result = INS_divps;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_divpd;
            }
            else
            {
                unreached();
            }
            break;

        case SIMDIntrinsicMin:
            if (baseType == TYP_FLOAT)
            {
                result = INS_minps;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_minpd;
            }
            else if (baseType == TYP_UBYTE)
            {
                result = INS_pminub;
            }
            else if (baseType == TYP_SHORT)
            {
                result = INS_pminsw;
            }
            else
            {
                unreached();
            }
            break;

        case SIMDIntrinsicMax:
            if (baseType == TYP_FLOAT)
            {
                result = INS_maxps;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_maxpd;
            }
            else if (baseType == TYP_UBYTE)
            {
                result = INS_pmaxub;
            }
            else if (baseType == TYP_SHORT)
            {
                result = INS_pmaxsw;
            }
            else
            {
                unreached();
            }
            break;

        case SIMDIntrinsicEqual:
            if (baseType == TYP_FLOAT)
            {
                result = INS_cmpps;
                assert(ival != nullptr);
                *ival = 0;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_cmppd;
                assert(ival != nullptr);
                *ival = 0;
            }
            else if (baseType == TYP_INT || baseType == TYP_UINT)
            {
                result = INS_pcmpeqd;
            }
            else if (baseType == TYP_CHAR || baseType == TYP_SHORT)
            {
                result = INS_pcmpeqw;
            }
            else if (baseType == TYP_UBYTE || baseType == TYP_BYTE)
            {
                result = INS_pcmpeqb;
            }
            else if (compiler->canUseAVX() && (baseType == TYP_ULONG || baseType == TYP_LONG))
            {
                result = INS_pcmpeqq;
            }
            break;

        case SIMDIntrinsicLessThan:
            // Packed integers use > with swapped operands
            assert(baseType != TYP_INT);

            if (baseType == TYP_FLOAT)
            {
                result = INS_cmpps;
                assert(ival != nullptr);
                *ival = 1;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_cmppd;
                assert(ival != nullptr);
                *ival = 1;
            }
            break;

        case SIMDIntrinsicLessThanOrEqual:
            // Packed integers use (a==b) || ( b > a) in place of a <= b.
            assert(baseType != TYP_INT);

            if (baseType == TYP_FLOAT)
            {
                result = INS_cmpps;
                assert(ival != nullptr);
                *ival = 2;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_cmppd;
                assert(ival != nullptr);
                *ival = 2;
            }
            break;

        case SIMDIntrinsicGreaterThan:
            // Packed float/double use < with swapped operands
            assert(!varTypeIsFloating(baseType));

            // SSE2 supports only signed >
            if (baseType == TYP_INT)
            {
                result = INS_pcmpgtd;
            }
            else if (baseType == TYP_SHORT)
            {
                result = INS_pcmpgtw;
            }
            else if (baseType == TYP_BYTE)
            {
                result = INS_pcmpgtb;
            }
            else if (compiler->canUseAVX() && (baseType == TYP_LONG))
            {
                result = INS_pcmpgtq;
            }
            break;

        case SIMDIntrinsicBitwiseAnd:
            if (baseType == TYP_FLOAT)
            {
                result = INS_andps;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_andpd;
            }
            else if (varTypeIsIntegral(baseType))
            {
                result = INS_pand;
            }
            break;

        case SIMDIntrinsicBitwiseAndNot:
            if (baseType == TYP_FLOAT)
            {
                result = INS_andnps;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_andnpd;
            }
            else if (baseType == TYP_INT)
            {
                result = INS_pandn;
            }
            else if (varTypeIsIntegral(baseType))
            {
                result = INS_pandn;
            }
            break;

        case SIMDIntrinsicBitwiseOr:
            if (baseType == TYP_FLOAT)
            {
                result = INS_orps;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_orpd;
            }
            else if (varTypeIsIntegral(baseType))
            {
                result = INS_por;
            }
            break;

        case SIMDIntrinsicBitwiseXor:
            if (baseType == TYP_FLOAT)
            {
                result = INS_xorps;
            }
            else if (baseType == TYP_DOUBLE)
            {
                result = INS_xorpd;
            }
            else if (varTypeIsIntegral(baseType))
            {
                result = INS_pxor;
            }
            break;

        case SIMDIntrinsicCast:
            result = INS_movaps;
            break;

        case SIMDIntrinsicShiftLeftInternal:
            // base type doesn't matter since the entire vector is shifted left
            result = INS_pslldq;
            break;

        case SIMDIntrinsicShiftRightInternal:
            // base type doesn't matter since the entire vector is shifted right
            result = INS_psrldq;
            break;

        case SIMDIntrinsicUpperSave:
            result = INS_vextractf128;
            break;

        case SIMDIntrinsicUpperRestore:
            result = INS_insertps;
            break;

        default:
            assert(!"Unsupported SIMD intrinsic");
            unreached();
    }

    noway_assert(result != INS_invalid);
    return result;
}

// genSIMDScalarMove: Generate code to move a value of type "type" from src mm reg
// to target mm reg, zeroing out the upper bits if and only if specified.
//
// Arguments:
//    type             the type of value to be moved
//    targetReg        the target reg
//    srcReg           the src reg
//    moveType         action to be performed on target upper bits
//
// Return Value:
//    None
//
// Notes:
//    This is currently only supported for floating point types.
//
void CodeGen::genSIMDScalarMove(var_types type, regNumber targetReg, regNumber srcReg, SIMDScalarMoveType moveType)
{
    var_types targetType = compiler->getSIMDVectorType();
    assert(varTypeIsFloating(type));
#ifdef FEATURE_AVX_SUPPORT
    if (compiler->getSIMDInstructionSet() == InstructionSet_AVX)
    {
        switch (moveType)
        {
            case SMT_PreserveUpper:
                if (srcReg != targetReg)
                {
                    instruction ins = ins_Store(type);
                    if (getEmitter()->IsThreeOperandMoveAVXInstruction(ins))
                    {
                        // In general, when we use a three-operands move instruction, we want to merge the src with
                        // itself. This is an exception in that we actually want the "merge" behavior, so we must
                        // specify it with all 3 operands.
                        inst_RV_RV_RV(ins, targetReg, targetReg, srcReg, emitTypeSize(targetType));
                    }
                    else
                    {
                        inst_RV_RV(ins, targetReg, srcReg, targetType, emitTypeSize(targetType));
                    }
                }
                break;

            case SMT_ZeroInitUpper:
            {
                // insertps is a 128-bit only instruction, and clears the upper 128 bits, which is what we want.
                // The insertpsImm selects which fields are copied and zero'd of the lower 128 bits, so we choose
                // to zero all but the lower bits.
                unsigned int insertpsImm =
                    (INSERTPS_TARGET_SELECT(0) | INSERTPS_ZERO(1) | INSERTPS_ZERO(2) | INSERTPS_ZERO(3));
                inst_RV_RV_IV(INS_insertps, EA_16BYTE, targetReg, srcReg, insertpsImm);
                break;
            }

            case SMT_ZeroInitUpper_SrcHasUpperZeros:
                if (srcReg != targetReg)
                {
                    instruction ins = ins_Copy(type);
                    assert(!getEmitter()->IsThreeOperandMoveAVXInstruction(ins));
                    inst_RV_RV(ins, targetReg, srcReg, targetType, emitTypeSize(targetType));
                }
                break;

            default:
                unreached();
        }
    }
    else
#endif // FEATURE_AVX_SUPPORT
    {
        // SSE

        switch (moveType)
        {
            case SMT_PreserveUpper:
                if (srcReg != targetReg)
                {
                    inst_RV_RV(ins_Store(type), targetReg, srcReg, targetType, emitTypeSize(targetType));
                }
                break;

            case SMT_ZeroInitUpper:
                if (srcReg == targetReg)
                {
                    // There is no guarantee that upper bits of op1Reg are zero.
                    // We achieve this by using left logical shift 12-bytes and right logical shift 12 bytes.
                    instruction ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, type);
                    getEmitter()->emitIns_R_I(ins, EA_16BYTE, srcReg, 12);
                    ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, type);
                    getEmitter()->emitIns_R_I(ins, EA_16BYTE, srcReg, 12);
                }
                else
                {
                    genSIMDZero(targetType, TYP_FLOAT, targetReg);
                    inst_RV_RV(ins_Store(type), targetReg, srcReg);
                }
                break;

            case SMT_ZeroInitUpper_SrcHasUpperZeros:
                if (srcReg != targetReg)
                {
                    inst_RV_RV(ins_Copy(type), targetReg, srcReg, targetType, emitTypeSize(targetType));
                }
                break;

            default:
                unreached();
        }
    }
}

void CodeGen::genSIMDZero(var_types targetType, var_types baseType, regNumber targetReg)
{
    // pxor reg, reg
    instruction ins = getOpForSIMDIntrinsic(SIMDIntrinsicBitwiseXor, baseType);
    inst_RV_RV(ins, targetReg, targetReg, targetType, emitActualTypeSize(targetType));
}

//------------------------------------------------------------------------
// genSIMDIntrinsicInit: Generate code for SIMD Intrinsic Initialize.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicInit(GenTreeSIMD* simdNode)
{
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicInit);

    GenTree*  op1       = simdNode->gtGetOp1();
    var_types baseType  = simdNode->gtSIMDBaseType;
    regNumber targetReg = simdNode->gtRegNum;
    assert(targetReg != REG_NA);
    var_types      targetType = simdNode->TypeGet();
    InstructionSet iset       = compiler->getSIMDInstructionSet();
    unsigned       size       = simdNode->gtSIMDSize;

    // Should never see small int base type vectors except for zero initialization.
    noway_assert(!varTypeIsSmallInt(baseType) || op1->IsIntegralConst(0));

    instruction ins = INS_invalid;
    if (op1->isContained())
    {
        if (op1->IsIntegralConst(0) || op1->IsFPZero())
        {
            genSIMDZero(targetType, baseType, targetReg);
        }
        else if (varTypeIsIntegral(baseType) && op1->IsIntegralConst(-1))
        {
            // case of initializing elements of vector with all 1's
            // generate pcmpeqd reg, reg
            ins = getOpForSIMDIntrinsic(SIMDIntrinsicEqual, TYP_INT);
            inst_RV_RV(ins, targetReg, targetReg, targetType, emitActualTypeSize(targetType));
        }
#ifdef FEATURE_AVX_SUPPORT
        else
        {
            assert(iset == InstructionSet_AVX);
            ins = getOpForSIMDIntrinsic(SIMDIntrinsicInit, baseType);
            if (op1->IsCnsFltOrDbl())
            {
                getEmitter()->emitInsBinary(ins, emitTypeSize(targetType), simdNode, op1);
            }
            else if (op1->OperIsLocalAddr())
            {
                unsigned offset = (op1->OperGet() == GT_LCL_FLD_ADDR) ? op1->gtLclFld.gtLclOffs : 0;
                getEmitter()->emitIns_R_S(ins, emitTypeSize(targetType), targetReg, op1->gtLclVarCommon.gtLclNum,
                                          offset);
            }
            else
            {
                unreached();
            }
        }
#endif // FEATURE_AVX_SUPPORT
    }
    else if (iset == InstructionSet_AVX && ((size == 32) || (size == 16)))
    {
        regNumber srcReg = genConsumeReg(op1);
        if (baseType == TYP_INT || baseType == TYP_UINT || baseType == TYP_LONG || baseType == TYP_ULONG)
        {
            ins = ins_CopyIntToFloat(baseType, TYP_FLOAT);
            assert(ins != INS_invalid);
            inst_RV_RV(ins, targetReg, srcReg, baseType, emitTypeSize(baseType));
            srcReg = targetReg;
        }

        ins = getOpForSIMDIntrinsic(simdNode->gtSIMDIntrinsicID, baseType);
        getEmitter()->emitIns_R_R(ins, emitActualTypeSize(targetType), targetReg, srcReg);
    }
    else
    {
        // If we reach here, op1 is not contained and we are using SSE or it is a SubRegisterSIMDType.
        // In either case we are going to use the SSE2 shuffle instruction.

        regNumber op1Reg         = genConsumeReg(op1);
        unsigned  shuffleControl = 0;

        if (compiler->isSubRegisterSIMDType(simdNode))
        {
            assert(baseType == TYP_FLOAT);

            // We cannot assume that upper bits of op1Reg or targetReg be zero.
            // Therefore we need to explicitly zero out upper bits.  This is
            // essential for the shuffle operation performed below.
            //
            // If op1 is a float/double constant, we would have loaded it from
            // data section using movss/sd.  Similarly if op1 is a memory op we
            // would have loaded it using movss/sd.  Movss/sd when loading a xmm reg
            // from memory would zero-out upper bits. In these cases we can
            // avoid explicitly zero'ing out targetReg if targetReg and op1Reg are the same or do it more efficiently
            // if they are not the same.
            SIMDScalarMoveType moveType =
                op1->IsCnsFltOrDbl() || op1->isMemoryOp() ? SMT_ZeroInitUpper_SrcHasUpperZeros : SMT_ZeroInitUpper;

            genSIMDScalarMove(TYP_FLOAT, targetReg, op1Reg, moveType);

            if (size == 8)
            {
                shuffleControl = 0x50;
            }
            else if (size == 12)
            {
                shuffleControl = 0x40;
            }
            else
            {
                noway_assert(!"Unexpected size for SIMD type");
            }
        }
        else // Vector<T>
        {
            if (op1Reg != targetReg)
            {
                if (varTypeIsFloating(baseType))
                {
                    ins = ins_Copy(targetType);
                }
                else if (baseType == TYP_INT || baseType == TYP_UINT || baseType == TYP_LONG || baseType == TYP_ULONG)
                {
                    ins = ins_CopyIntToFloat(baseType, TYP_FLOAT);
                }

                assert(ins != INS_invalid);
                inst_RV_RV(ins, targetReg, op1Reg, baseType, emitTypeSize(baseType));
            }
        }

        ins = getOpForSIMDIntrinsic(SIMDIntrinsicShuffleSSE2, baseType);
        getEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(targetType), targetReg, targetReg, shuffleControl);
    }

    genProduceReg(simdNode);
}

//-------------------------------------------------------------------------------------------
// genSIMDIntrinsicInitN: Generate code for SIMD Intrinsic Initialize for the form that takes
//                        a number of arguments equal to the length of the Vector.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicInitN(GenTreeSIMD* simdNode)
{
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicInitN);

    // Right now this intrinsic is supported only on TYP_FLOAT vectors
    var_types baseType = simdNode->gtSIMDBaseType;
    noway_assert(baseType == TYP_FLOAT);

    regNumber targetReg = simdNode->gtRegNum;
    assert(targetReg != REG_NA);

    var_types targetType = simdNode->TypeGet();

    // Note that we cannot use targetReg before consumed all source operands. Therefore,
    // Need an internal register to stitch together all the values into a single vector
    // in an XMM reg.
    assert(simdNode->gtRsvdRegs != RBM_NONE);
    assert(genCountBits(simdNode->gtRsvdRegs) == 1);
    regNumber vectorReg = genRegNumFromMask(simdNode->gtRsvdRegs);

    // Zero out vectorReg if we are constructing a vector whose size is not equal to targetType vector size.
    // For example in case of Vector4f we don't need to zero when using SSE2.
    if (compiler->isSubRegisterSIMDType(simdNode))
    {
        genSIMDZero(targetType, baseType, vectorReg);
    }

    unsigned int baseTypeSize = genTypeSize(baseType);
    instruction  insLeftShift = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, baseType);

    // We will first consume the list items in execution (left to right) order,
    // and record the registers.
    regNumber operandRegs[SIMD_INTRINSIC_MAX_PARAM_COUNT];
    unsigned  initCount = 0;
    for (GenTree* list = simdNode->gtGetOp1(); list != nullptr; list = list->gtGetOp2())
    {
        assert(list->OperGet() == GT_LIST);
        GenTree* listItem = list->gtGetOp1();
        assert(listItem->TypeGet() == baseType);
        assert(!listItem->isContained());
        regNumber operandReg   = genConsumeReg(listItem);
        operandRegs[initCount] = operandReg;
        initCount++;
    }

    unsigned int offset = 0;
    for (unsigned i = 0; i < initCount; i++)
    {
        // We will now construct the vector from the list items in reverse order.
        // This allows us to efficiently stitch together a vector as follows:
        // vectorReg = (vectorReg << offset)
        // VectorReg[0] = listItemReg
        // Use genSIMDScalarMove with SMT_PreserveUpper in order to ensure that the upper
        // bits of vectorReg are not modified.

        regNumber operandReg = operandRegs[initCount - i - 1];
        if (offset != 0)
        {
            getEmitter()->emitIns_R_I(insLeftShift, EA_16BYTE, vectorReg, baseTypeSize);
        }
        genSIMDScalarMove(baseType, vectorReg, operandReg, SMT_PreserveUpper);

        offset += baseTypeSize;
    }

    noway_assert(offset == simdNode->gtSIMDSize);

    // Load the initialized value.
    if (targetReg != vectorReg)
    {
        inst_RV_RV(ins_Copy(targetType), targetReg, vectorReg, targetType, emitActualTypeSize(targetType));
    }
    genProduceReg(simdNode);
}

//----------------------------------------------------------------------------------
// genSIMDIntrinsicUnOp: Generate code for SIMD Intrinsic unary operations like sqrt.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicUnOp(GenTreeSIMD* simdNode)
{
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicSqrt || simdNode->gtSIMDIntrinsicID == SIMDIntrinsicCast);

    GenTree*  op1       = simdNode->gtGetOp1();
    var_types baseType  = simdNode->gtSIMDBaseType;
    regNumber targetReg = simdNode->gtRegNum;
    assert(targetReg != REG_NA);
    var_types targetType = simdNode->TypeGet();

    regNumber   op1Reg = genConsumeReg(op1);
    instruction ins    = getOpForSIMDIntrinsic(simdNode->gtSIMDIntrinsicID, baseType);
    if (simdNode->gtSIMDIntrinsicID != SIMDIntrinsicCast || targetReg != op1Reg)
    {
        inst_RV_RV(ins, targetReg, op1Reg, targetType, emitActualTypeSize(targetType));
    }
    genProduceReg(simdNode);
}

//--------------------------------------------------------------------------------
// genSIMDIntrinsicBinOp: Generate code for SIMD Intrinsic binary operations
// add, sub, mul, bit-wise And, AndNot and Or.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicBinOp(GenTreeSIMD* simdNode)
{
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicAdd || simdNode->gtSIMDIntrinsicID == SIMDIntrinsicSub ||
           simdNode->gtSIMDIntrinsicID == SIMDIntrinsicMul || simdNode->gtSIMDIntrinsicID == SIMDIntrinsicDiv ||
           simdNode->gtSIMDIntrinsicID == SIMDIntrinsicBitwiseAnd ||
           simdNode->gtSIMDIntrinsicID == SIMDIntrinsicBitwiseAndNot ||
           simdNode->gtSIMDIntrinsicID == SIMDIntrinsicBitwiseOr ||
           simdNode->gtSIMDIntrinsicID == SIMDIntrinsicBitwiseXor || simdNode->gtSIMDIntrinsicID == SIMDIntrinsicMin ||
           simdNode->gtSIMDIntrinsicID == SIMDIntrinsicMax);

    GenTree*  op1       = simdNode->gtGetOp1();
    GenTree*  op2       = simdNode->gtGetOp2();
    var_types baseType  = simdNode->gtSIMDBaseType;
    regNumber targetReg = simdNode->gtRegNum;
    assert(targetReg != REG_NA);
    var_types      targetType = simdNode->TypeGet();
    InstructionSet iset       = compiler->getSIMDInstructionSet();

    genConsumeOperands(simdNode);
    regNumber op1Reg   = op1->gtRegNum;
    regNumber op2Reg   = op2->gtRegNum;
    regNumber otherReg = op2Reg;

    // Vector<Int>.Mul:
    // SSE2 doesn't have an instruction to perform this operation directly
    // whereas SSE4.1 does (pmulld).  This is special cased and computed
    // as follows.
    if (simdNode->gtSIMDIntrinsicID == SIMDIntrinsicMul && baseType == TYP_INT && iset == InstructionSet_SSE2)
    {
        // We need a temporary register that is NOT the same as the target,
        // and we MAY need another.
        assert(simdNode->gtRsvdRegs != RBM_NONE);
        assert(genCountBits(simdNode->gtRsvdRegs) == 2);

        regMaskTP tmpRegsMask = simdNode->gtRsvdRegs;
        regMaskTP tmpReg1Mask = genFindLowestBit(tmpRegsMask);
        tmpRegsMask &= ~tmpReg1Mask;
        regNumber tmpReg  = genRegNumFromMask(tmpReg1Mask);
        regNumber tmpReg2 = genRegNumFromMask(tmpRegsMask);
        // The register allocator guarantees the following conditions:
        // - the only registers that may be the same among op1Reg, op2Reg, tmpReg
        //   and tmpReg2 are op1Reg and op2Reg.
        // Let's be extra-careful and assert that now.
        assert((op1Reg != tmpReg) && (op1Reg != tmpReg2) && (op2Reg != tmpReg) && (op2Reg != tmpReg2) &&
               (tmpReg != tmpReg2));

        // We will start by setting things up so that:
        //    - We have op1 in op1Reg and targetReg, and they are different registers.
        //    - We have op2 in op2Reg and tmpReg
        //    - Either we will leave the input registers (the original op1Reg and op2Reg) unmodified,
        //      OR they are the targetReg that will be produced.
        //      (Note that in the code we generate below op1Reg and op2Reg are never written.)
        // We will copy things as necessary to ensure that this is the case.
        // Note that we can swap op1 and op2, since multiplication is commutative.
        // We will not modify the values in op1Reg and op2Reg.
        // (Though note that if either op1 or op2 is the same as targetReg, we will make
        // a copy and use that copy as the input register.  In that case we WILL modify
        // the original value in the register, but will wind up with the result in targetReg
        // in the end, as expected.)

        // First, we need a tmpReg that is NOT the same as targetReg.
        // Note that if we have another reg that is the same as targetReg,
        // we can use tmpReg2 for that case, as we will not have hit this case.
        if (tmpReg == targetReg)
        {
            tmpReg = tmpReg2;
        }

        if (op2Reg == targetReg)
        {
            // We will swap the operands.
            // Since the code below only deals with registers, this now becomes the case where
            // op1Reg == targetReg.
            op2Reg = op1Reg;
            op1Reg = targetReg;
        }
        if (op1Reg == targetReg)
        {
            // Copy op1, and make tmpReg2 the new op1Reg.
            // Note that those regs can't be the same, as we asserted above.
            // Also, we know that tmpReg2 hasn't been used, because we couldn't have hit
            // the "tmpReg == targetReg" case.
            inst_RV_RV(INS_movaps, tmpReg2, op1Reg, targetType, emitActualTypeSize(targetType));
            op1Reg = tmpReg2;
            inst_RV_RV(INS_movaps, tmpReg, op2Reg, targetType, emitActualTypeSize(targetType));
            // However, we have one more case to worry about: what if op2Reg is also targetReg
            // (i.e. we have the same operand as op1 and op2)?
            // In that case we will set op2Reg to the same register as op1Reg.
            if (op2Reg == targetReg)
            {
                op2Reg = tmpReg2;
            }
        }
        else
        {
            // Copy op1 to targetReg and op2 to tmpReg.
            inst_RV_RV(INS_movaps, targetReg, op1Reg, targetType, emitActualTypeSize(targetType));
            inst_RV_RV(INS_movaps, tmpReg, op2Reg, targetType, emitActualTypeSize(targetType));
        }
        // Let's assert that things are as we expect.
        //    - We have op1 in op1Reg and targetReg, and they are different registers.
        assert(op1Reg != targetReg);
        //    - We have op2 in op2Reg and tmpReg, and they are different registers.
        assert(op2Reg != tmpReg);
        //    - Either we are going to leave op1's reg unmodified, or it is the targetReg.
        assert((op1->gtRegNum == op1Reg) || (op1->gtRegNum == op2Reg) || (op1->gtRegNum == targetReg));
        //    - Similarly, we are going to leave op2's reg unmodified, or it is the targetReg.
        assert((op2->gtRegNum == op1Reg) || (op2->gtRegNum == op2Reg) || (op2->gtRegNum == targetReg));

        // Now we can generate the code.

        // targetReg = op1 >> 4-bytes (op1 is already in targetReg)
        getEmitter()->emitIns_R_I(INS_psrldq, emitActualTypeSize(targetType), targetReg, 4);

        // tmpReg  = op2 >> 4-bytes (op2 is already in tmpReg)
        getEmitter()->emitIns_R_I(INS_psrldq, emitActualTypeSize(targetType), tmpReg, 4);

        // tmp = unsigned double word multiply of targetReg and tmpReg. Essentially
        // tmpReg[63:0] = op1[1] * op2[1]
        // tmpReg[127:64] = op1[3] * op2[3]
        inst_RV_RV(INS_pmuludq, tmpReg, targetReg, targetType, emitActualTypeSize(targetType));

        // Extract first and third double word results from tmpReg
        // tmpReg = shuffle(0,0,2,0) of tmpReg
        getEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(targetType), tmpReg, tmpReg, 0x08);

        // targetReg[63:0] = op1[0] * op2[0]
        // targetReg[127:64] = op1[2] * op2[2]
        inst_RV_RV(INS_movaps, targetReg, op1Reg, targetType, emitActualTypeSize(targetType));
        inst_RV_RV(INS_pmuludq, targetReg, op2Reg, targetType, emitActualTypeSize(targetType));

        // Extract first and third double word results from targetReg
        // targetReg = shuffle(0,0,2,0) of targetReg
        getEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(targetType), targetReg, targetReg, 0x08);

        // pack the results into a single vector
        inst_RV_RV(INS_punpckldq, targetReg, tmpReg, targetType, emitActualTypeSize(targetType));
    }
    else
    {
        instruction ins = getOpForSIMDIntrinsic(simdNode->gtSIMDIntrinsicID, baseType);

        // Currently AVX doesn't support integer.
        // if the ins is INS_cvtsi2ss or INS_cvtsi2sd, we won't use AVX.
        if (op1Reg != targetReg && compiler->canUseAVX() && !(ins == INS_cvtsi2ss || ins == INS_cvtsi2sd) &&
            getEmitter()->IsThreeOperandAVXInstruction(ins))
        {
            inst_RV_RV_RV(ins, targetReg, op1Reg, op2Reg, emitActualTypeSize(targetType));
        }
        else
        {
            if (op2Reg == targetReg)
            {
                otherReg = op1Reg;
            }
            else if (op1Reg != targetReg)
            {
                inst_RV_RV(ins_Copy(targetType), targetReg, op1Reg, targetType, emitActualTypeSize(targetType));
            }

            inst_RV_RV(ins, targetReg, otherReg, targetType, emitActualTypeSize(targetType));
        }
    }

    // Vector2/3 div: since the top-most elements will be zero, we end up
    // perfoming 0/0 which is a NAN. Therefore, post division we need to set the
    // top-most elements to zero. This is achieved by left logical shift followed
    // by right logical shift of targetReg.
    if (simdNode->gtSIMDIntrinsicID == SIMDIntrinsicDiv && (simdNode->gtSIMDSize < 16))
    {
        // These are 16 byte operations, so we subtract from 16 bytes, not the vector register length.
        unsigned shiftCount = 16 - simdNode->gtSIMDSize;
        assert(shiftCount != 0);
        instruction ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, baseType);
        getEmitter()->emitIns_R_I(ins, EA_16BYTE, targetReg, shiftCount);
        ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, baseType);
        getEmitter()->emitIns_R_I(ins, EA_16BYTE, targetReg, shiftCount);
    }

    genProduceReg(simdNode);
}

//--------------------------------------------------------------------------------
// genSIMDIntrinsicRelOp: Generate code for a SIMD Intrinsic relational operater
// <, <=, >, >= and ==
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicRelOp(GenTreeSIMD* simdNode)
{
    GenTree*  op1       = simdNode->gtGetOp1();
    GenTree*  op2       = simdNode->gtGetOp2();
    var_types baseType  = simdNode->gtSIMDBaseType;
    regNumber targetReg = simdNode->gtRegNum;
    assert(targetReg != REG_NA);
    var_types      targetType = simdNode->TypeGet();
    InstructionSet iset       = compiler->getSIMDInstructionSet();

    genConsumeOperands(simdNode);
    regNumber op1Reg   = op1->gtRegNum;
    regNumber op2Reg   = op2->gtRegNum;
    regNumber otherReg = op2Reg;

    switch (simdNode->gtSIMDIntrinsicID)
    {
        case SIMDIntrinsicEqual:
        case SIMDIntrinsicGreaterThan:
        {
            // SSE2: vector<(u)long> relation op should be implemented in terms of TYP_INT comparison operations
            assert(((iset == InstructionSet_AVX) || (baseType != TYP_LONG)) && (baseType != TYP_ULONG));

            // Greater-than: Floating point vectors use "<" with swapped operands
            if (simdNode->gtSIMDIntrinsicID == SIMDIntrinsicGreaterThan)
            {
                assert(!varTypeIsFloating(baseType));
            }

            unsigned    ival = 0;
            instruction ins  = getOpForSIMDIntrinsic(simdNode->gtSIMDIntrinsicID, baseType, &ival);

            // targetReg = op1reg > op2reg
            // Therefore, we can optimize if op1Reg == targetReg
            otherReg = op2Reg;
            if (op1Reg != targetReg)
            {
                if (op2Reg == targetReg)
                {
                    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicEqual);
                    otherReg = op1Reg;
                }
                else
                {
                    inst_RV_RV(ins_Copy(targetType), targetReg, op1Reg, targetType, emitActualTypeSize(targetType));
                }
            }

            if (varTypeIsFloating(baseType))
            {
                getEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(targetType), targetReg, otherReg, ival);
            }
            else
            {
                inst_RV_RV(ins, targetReg, otherReg, targetType, emitActualTypeSize(targetType));
            }
        }
        break;

        case SIMDIntrinsicLessThan:
        case SIMDIntrinsicLessThanOrEqual:
        {
            // Int vectors use ">" and ">=" with swapped operands
            assert(varTypeIsFloating(baseType));

            // Get the instruction opcode for compare operation
            unsigned    ival;
            instruction ins = getOpForSIMDIntrinsic(simdNode->gtSIMDIntrinsicID, baseType, &ival);

            // targetReg = op1reg RelOp op2reg
            // Thefore, we can optimize if op1Reg == targetReg
            if (op1Reg != targetReg)
            {
                inst_RV_RV(ins_Copy(targetType), targetReg, op1Reg, targetType, emitActualTypeSize(targetType));
            }

            getEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(targetType), targetReg, op2Reg, ival);
        }
        break;

        // (In)Equality that produces bool result instead of a bit vector
        case SIMDIntrinsicOpEquality:
        case SIMDIntrinsicOpInEquality:
        {
            assert(genIsValidIntReg(targetReg));

            // We need two additional XMM register as scratch
            assert(simdNode->gtRsvdRegs != RBM_NONE);
            assert(genCountBits(simdNode->gtRsvdRegs) == 2);

            regMaskTP tmpRegsMask = simdNode->gtRsvdRegs;
            regMaskTP tmpReg1Mask = genFindLowestBit(tmpRegsMask);
            tmpRegsMask &= ~tmpReg1Mask;
            regNumber tmpReg1  = genRegNumFromMask(tmpReg1Mask);
            regNumber tmpReg2  = genRegNumFromMask(tmpRegsMask);
            var_types simdType = op1->TypeGet();
            // TODO-1stClassStructs: Temporary to minimize asmDiffs
            if (simdType == TYP_DOUBLE)
            {
                simdType = TYP_SIMD8;
            }

            // Here we should consider TYP_SIMD12 operands as if they were TYP_SIMD16
            // since both the operands will be in XMM registers.
            if (simdType == TYP_SIMD12)
            {
                simdType = TYP_SIMD16;
            }

            // tmpReg1 = (op1Reg == op2Reg)
            // Call this value of tmpReg1 as 'compResult' for further reference below.
            regNumber otherReg = op2Reg;
            if (tmpReg1 != op2Reg)
            {
                if (tmpReg1 != op1Reg)
                {
                    inst_RV_RV(ins_Copy(simdType), tmpReg1, op1Reg, simdType, emitActualTypeSize(simdType));
                }
            }
            else
            {
                otherReg = op1Reg;
            }

            // For all integer types we can use TYP_INT comparison.
            unsigned    ival = 0;
            instruction ins =
                getOpForSIMDIntrinsic(SIMDIntrinsicEqual, varTypeIsFloating(baseType) ? baseType : TYP_INT, &ival);

            if (varTypeIsFloating(baseType))
            {
                getEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(simdType), tmpReg1, otherReg, ival);
            }
            else
            {
                inst_RV_RV(ins, tmpReg1, otherReg, simdType, emitActualTypeSize(simdType));
            }

            // If we have 32 bytes, start by anding the two 16-byte halves to get a 16-byte result.
            if (compiler->canUseAVX() && (simdType == TYP_SIMD32))
            {
                // Reduce tmpReg1 from 256-bits to 128-bits bitwise-Anding the lower and uppper 128-bits
                //
                // Generated code sequence
                // - vextractf128 tmpReg2, tmpReg1, 0x01
                //       tmpReg2[128..255] <- 0
                //       tmpReg2[0..127]   <- tmpReg1[128..255]
                // - vandps tmpReg1, tempReg2
                //       This will zero-out upper portion of tmpReg1 and
                //       lower portion of tmpReg1 is and of upper and lower 128-bit comparison result.
                getEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, tmpReg2, tmpReg1, 0x01);
                inst_RV_RV(INS_andps, tmpReg1, tmpReg2, simdType, emitActualTypeSize(simdType));
            }
            // Next, if we have more than 8 bytes, and the two 8-byte halves to get a 8-byte result.
            if (simdType != TYP_SIMD8)
            {
                // tmpReg2 = Shuffle(tmpReg1, (1,0,3,2))
                // Note: vpshufd is a 128-bit only instruction. Therefore, explicitly pass EA_16BYTE
                getEmitter()->emitIns_R_R_I(INS_pshufd, EA_16BYTE, tmpReg2, tmpReg1, 0x4E);

                // tmpReg1 = BitwiseAnd(tmpReg1, tmpReg2)
                //
                // Note that what we have computed is as follows at this point:
                // tmpReg1[0] = compResult[0] & compResult[2]
                // tmpReg1[1] = compResult[1] & compResult[3]
                inst_RV_RV(INS_andps, tmpReg1, tmpReg2, simdType, emitActualTypeSize(simdType));
            }
            // At this point, we have either reduced the result to 8 bytes: tmpReg1[0] and tmpReg1[1],
            // OR we have a Vector2 (TYP_SIMD8) in tmpReg1, which has only those two fields.

            // tmpReg2 = Shuffle(tmpReg1, (0,0,0,1))
            // tmpReg2[0] = compResult[1] & compResult[3]
            getEmitter()->emitIns_R_R_I(INS_pshufd, EA_16BYTE, tmpReg2, tmpReg1, 0x1);

            // tmpReg1 = BitwiseAnd(tmpReg1, tmpReg2)
            // That is tmpReg1[0] = compResult[0] & compResult[1] & compResult[2] & compResult[3]
            inst_RV_RV(INS_pand, tmpReg1, tmpReg2, simdType, emitActualTypeSize(simdType)); // ??? INS_andps??

            // targetReg = lower 32-bits of tmpReg1 = compResult[0] & compResult[1] & compResult[2] & compResult[3]
            // (Note that for mov_xmm2i, the int register is always in the reg2 position.
            inst_RV_RV(INS_mov_xmm2i, tmpReg1, targetReg, TYP_INT);

            // Since we need to compute a bool result, targetReg needs to be set to 1 on true and zero on false.
            // Equality:
            //   cmp targetReg, 0xFFFFFFFF
            //   sete targetReg
            //   movzx targetReg, targetReg
            //
            // InEquality:
            //   cmp targetReg, 0xFFFFFFFF
            //   setne targetReg
            //   movzx targetReg, targetReg
            //
            getEmitter()->emitIns_R_I(INS_cmp, EA_4BYTE, targetReg, 0xFFFFFFFF);
            inst_RV((simdNode->gtSIMDIntrinsicID == SIMDIntrinsicOpEquality) ? INS_sete : INS_setne, targetReg, TYP_INT,
                    EA_1BYTE);
            assert(simdNode->TypeGet() == TYP_INT);
            // Set the higher bytes to 0
            inst_RV_RV(ins_Move_Extend(TYP_UBYTE, true), targetReg, targetReg, TYP_UBYTE, emitTypeSize(TYP_UBYTE));
        }
        break;

        default:
            noway_assert(!"Unimplemented SIMD relational operation.");
            unreached();
    }

    genProduceReg(simdNode);
}

//--------------------------------------------------------------------------------
// genSIMDIntrinsicDotProduct: Generate code for SIMD Intrinsic Dot Product.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicDotProduct(GenTreeSIMD* simdNode)
{
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicDotProduct);

    GenTree*  op1      = simdNode->gtGetOp1();
    GenTree*  op2      = simdNode->gtGetOp2();
    var_types baseType = simdNode->gtSIMDBaseType;
    var_types simdType = op1->TypeGet();
    // TODO-1stClassStructs: Temporary to minimize asmDiffs
    if (simdType == TYP_DOUBLE)
    {
        simdType = TYP_SIMD8;
    }
    var_types simdEvalType = (simdType == TYP_SIMD12) ? TYP_SIMD16 : simdType;
    regNumber targetReg    = simdNode->gtRegNum;
    assert(targetReg != REG_NA);

    // DotProduct is only supported on floating point types.
    var_types targetType = simdNode->TypeGet();
    assert(targetType == baseType);
    assert(varTypeIsFloating(baseType));

    genConsumeOperands(simdNode);
    regNumber op1Reg = op1->gtRegNum;
    regNumber op2Reg = op2->gtRegNum;

    regNumber tmpReg = REG_NA;
    // For SSE, or AVX with 32-byte vectors, we need an additional Xmm register as scratch.
    // However, it must be distinct from targetReg, so we request two from the register allocator.
    // Note that if this is a TYP_SIMD16 or smaller on AVX, then we don't need a tmpReg.
    if ((compiler->getSIMDInstructionSet() == InstructionSet_SSE2) || (simdEvalType == TYP_SIMD32))
    {
        assert(simdNode->gtRsvdRegs != RBM_NONE);
        assert(genCountBits(simdNode->gtRsvdRegs) == 2);

        regMaskTP tmpRegsMask = simdNode->gtRsvdRegs;
        regMaskTP tmpReg1Mask = genFindLowestBit(tmpRegsMask);
        tmpRegsMask &= ~tmpReg1Mask;
        regNumber tmpReg1 = genRegNumFromMask(tmpReg1Mask);
        regNumber tmpReg2 = genRegNumFromMask(tmpRegsMask);

        // Choose any register different from targetReg as tmpReg
        if (tmpReg1 != targetReg)
        {
            tmpReg = tmpReg1;
        }
        else
        {
            assert(targetReg != tmpReg2);
            tmpReg = tmpReg2;
        }
        assert(tmpReg != REG_NA);
        assert(tmpReg != targetReg);
    }

    if (compiler->getSIMDInstructionSet() == InstructionSet_SSE2)
    {
        // We avoid reg move if either op1Reg == targetReg or op2Reg == targetReg
        if (op1Reg == targetReg)
        {
            // Best case
            // nothing to do, we have registers in the right place
        }
        else if (op2Reg == targetReg)
        {
            op2Reg = op1Reg;
        }
        else
        {
            inst_RV_RV(ins_Copy(simdType), targetReg, op1Reg, simdEvalType, emitActualTypeSize(simdType));
        }

        // DotProduct(v1, v2)
        // Here v0 = targetReg, v1 = op1Reg, v2 = op2Reg and tmp = tmpReg
        if (baseType == TYP_FLOAT)
        {
            // v0 = v1 * v2
            // tmp = v0                                       // v0  = (3, 2, 1, 0) - each element is given by its
            //                                                // position
            // tmp = shuffle(tmp, tmp, Shuffle(2,3,0,1))      // tmp = (2, 3, 0, 1)
            // v0 = v0 + tmp                                  // v0  = (3+2, 2+3, 1+0, 0+1)
            // tmp = v0
            // tmp = shuffle(tmp, tmp, Shuffle(0,1,2,3))      // tmp = (0+1, 1+0, 2+3, 3+2)
            // v0 = v0 + tmp                                  // v0  = (0+1+2+3, 0+1+2+3, 0+1+2+3, 0+1+2+3)
            //                                                // Essentially horizontal addtion of all elements.
            //                                                // We could achieve the same using SSEv3 instruction
            //                                                // HADDPS.
            //
            inst_RV_RV(INS_mulps, targetReg, op2Reg);
            inst_RV_RV(INS_movaps, tmpReg, targetReg);
            inst_RV_RV_IV(INS_shufps, EA_16BYTE, tmpReg, tmpReg, 0xb1);
            inst_RV_RV(INS_addps, targetReg, tmpReg);
            inst_RV_RV(INS_movaps, tmpReg, targetReg);
            inst_RV_RV_IV(INS_shufps, EA_16BYTE, tmpReg, tmpReg, 0x1b);
            inst_RV_RV(INS_addps, targetReg, tmpReg);
        }
        else if (baseType == TYP_DOUBLE)
        {
            // v0 = v1 * v2
            // tmp = v0                                       // v0  = (1, 0) - each element is given by its position
            // tmp = shuffle(tmp, tmp, Shuffle(0,1))          // tmp = (0, 1)
            // v0 = v0 + tmp                                  // v0  = (1+0, 0+1)
            inst_RV_RV(INS_mulpd, targetReg, op2Reg);
            inst_RV_RV(INS_movaps, tmpReg, targetReg);
            inst_RV_RV_IV(INS_shufpd, EA_16BYTE, tmpReg, tmpReg, 0x01);
            inst_RV_RV(INS_addpd, targetReg, tmpReg);
        }
        else
        {
            unreached();
        }
    }
    else
    {
        // We avoid reg move if either op1Reg == targetReg or op2Reg == targetReg.
        // Note that this is a duplicate of the code above for SSE, but in the AVX case we can eventually
        // use the 3-op form, so that we can avoid these copies.
        // TODO-CQ: Add inst_RV_RV_RV_IV().
        if (op1Reg == targetReg)
        {
            // Best case
            // nothing to do, we have registers in the right place
        }
        else if (op2Reg == targetReg)
        {
            op2Reg = op1Reg;
        }
        else
        {
            inst_RV_RV(ins_Copy(simdType), targetReg, op1Reg, simdEvalType, emitActualTypeSize(simdType));
        }

        emitAttr emitSize = emitActualTypeSize(simdEvalType);
        if (baseType == TYP_FLOAT)
        {
            // dpps computes the dot product of the upper & lower halves of the 32-byte register.
            // Notice that if this is a TYP_SIMD16 or smaller on AVX, then we don't need a tmpReg.
            inst_RV_RV_IV(INS_dpps, emitSize, targetReg, op2Reg, 0xf1);
            // If this is TYP_SIMD32, we need to combine the lower & upper results.
            if (simdEvalType == TYP_SIMD32)
            {
                getEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, tmpReg, targetReg, 0x01);
                inst_RV_RV(INS_addps, targetReg, tmpReg, targetType, emitTypeSize(targetType));
            }
        }
        else if (baseType == TYP_DOUBLE)
        {
            // On AVX, we have no 16-byte vectors of double.  Note that, if we did, we could use
            // dppd directly.
            assert(simdType == TYP_SIMD32);

            // targetReg = targetReg * op2Reg
            // targetReg = vhaddpd(targetReg, targetReg) ; horizontal sum of lower & upper halves
            // tmpReg    = vextractf128(targetReg, 1)    ; Moves the upper sum into tempReg
            // targetReg = targetReg + tmpReg
            inst_RV_RV(INS_mulpd, targetReg, op2Reg, simdEvalType, emitActualTypeSize(simdType));
            inst_RV_RV(INS_haddpd, targetReg, targetReg, simdEvalType, emitActualTypeSize(simdType));
            getEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, tmpReg, targetReg, 0x01);
            inst_RV_RV(INS_addpd, targetReg, tmpReg, targetType, emitTypeSize(targetType));
        }
        else
        {
            unreached();
        }
    }

    genProduceReg(simdNode);
}

//------------------------------------------------------------------------------------
// genSIMDIntrinsicGetItem: Generate code for SIMD Intrinsic get element at index i.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicGetItem(GenTreeSIMD* simdNode)
{
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicGetItem);

    GenTree*  op1      = simdNode->gtGetOp1();
    GenTree*  op2      = simdNode->gtGetOp2();
    var_types simdType = op1->TypeGet();
    assert(varTypeIsSIMD(simdType));

    // op1 of TYP_SIMD12 should be considered as TYP_SIMD16,
    // since it is in XMM register.
    if (simdType == TYP_SIMD12)
    {
        simdType = TYP_SIMD16;
    }

    var_types baseType  = simdNode->gtSIMDBaseType;
    regNumber targetReg = simdNode->gtRegNum;
    assert(targetReg != REG_NA);
    var_types targetType = simdNode->TypeGet();
    assert(targetType == genActualType(baseType));

    // GetItem has 2 operands:
    // - the source of SIMD type (op1)
    // - the index of the value to be returned.
    genConsumeOperands(simdNode);
    regNumber srcReg = op1->gtRegNum;

    // SSE2 doesn't have an instruction to implement this intrinsic if the index is not a constant.
    // For the non-constant case, we will use the SIMD temp location to store the vector, and
    // the load the desired element.
    // The range check will already have been performed, so at this point we know we have an index
    // within the bounds of the vector.
    if (!op2->IsCnsIntOrI())
    {
        unsigned simdInitTempVarNum = compiler->lvaSIMDInitTempVarNum;
        noway_assert(simdInitTempVarNum != BAD_VAR_NUM);
        bool      isEBPbased;
        unsigned  offs     = compiler->lvaFrameAddress(simdInitTempVarNum, &isEBPbased);
        regNumber indexReg = op2->gtRegNum;

        // Store the vector to the temp location.
        getEmitter()->emitIns_S_R(ins_Store(simdType, compiler->isSIMDTypeLocalAligned(simdInitTempVarNum)),
                                  emitTypeSize(simdType), srcReg, simdInitTempVarNum, 0);

        // Now, load the desired element.
        getEmitter()->emitIns_R_ARX(ins_Move_Extend(baseType, false), // Load
                                    emitTypeSize(baseType),           // Of the vector baseType
                                    targetReg,                        // To targetReg
                                    (isEBPbased) ? REG_EBP : REG_ESP, // Stack-based
                                    indexReg,                         // Indexed
                                    genTypeSize(baseType),            // by the size of the baseType
                                    offs);
        genProduceReg(simdNode);
        return;
    }

    noway_assert(op2->isContained());
    unsigned int index        = (unsigned int)op2->gtIntCon.gtIconVal;
    unsigned int byteShiftCnt = index * genTypeSize(baseType);

    // In general we shouldn't have an index greater than or equal to the length of the vector.
    // However, if we have an out-of-range access, under minOpts it will not be optimized
    // away. The code will throw before we reach this point, but we still need to generate
    // code. In that case, we will simply mask off the upper bits.
    if (byteShiftCnt >= compiler->getSIMDVectorRegisterByteLength())
    {
        byteShiftCnt &= (compiler->getSIMDVectorRegisterByteLength() - 1);
        index = byteShiftCnt / genTypeSize(baseType);
    }

    regNumber tmpReg = REG_NA;
    if (simdNode->gtRsvdRegs != RBM_NONE)
    {
        assert(genCountBits(simdNode->gtRsvdRegs) == 1);
        tmpReg = genRegNumFromMask(simdNode->gtRsvdRegs);
    }
    else
    {
        assert((byteShiftCnt == 0) || varTypeIsFloating(baseType) ||
               (varTypeIsSmallInt(baseType) && (byteShiftCnt < 16)));
    }

    if (byteShiftCnt >= 16)
    {
        assert(compiler->getSIMDInstructionSet() == InstructionSet_AVX);
        byteShiftCnt -= 16;
        regNumber newSrcReg;
        if (varTypeIsFloating(baseType))
        {
            newSrcReg = targetReg;
        }
        else
        {
            // Integer types
            assert(tmpReg != REG_NA);
            newSrcReg = tmpReg;
        }
        getEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, newSrcReg, srcReg, 0x01);

        srcReg = newSrcReg;
    }

    // Generate the following sequence:
    // 1) baseType is floating point
    //   movaps    targetReg, srcReg
    //   psrldq    targetReg, byteShiftCnt  <-- not generated if accessing zero'th element
    //
    // 2) baseType is not floating point
    //   movaps    tmpReg, srcReg           <-- not generated if accessing zero'th element
    //                                          OR if tmpReg == srcReg
    //   psrldq    tmpReg, byteShiftCnt     <-- not generated if accessing zero'th element
    //   mov_xmm2i targetReg, tmpReg
    if (varTypeIsFloating(baseType))
    {
        if (targetReg != srcReg)
        {
            inst_RV_RV(ins_Copy(simdType), targetReg, srcReg, simdType, emitActualTypeSize(simdType));
        }

        if (byteShiftCnt != 0)
        {
            instruction ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, baseType);
            getEmitter()->emitIns_R_I(ins, emitActualTypeSize(simdType), targetReg, byteShiftCnt);
        }
    }
    else
    {
        if (varTypeIsSmallInt(baseType))
        {
            // Note that pextrw extracts 16-bit value by index and zero extends it to 32-bits.
            // In case of vector<short> we also need to sign extend the 16-bit value in targetReg
            // Vector<byte> - index/2 will give the index of the 16-bit value to extract. Shift right
            // by 8-bits if index is odd.  In case of Vector<sbyte> also sign extend targetReg.

            unsigned baseSize = genTypeSize(baseType);
            if (baseSize == 1)
            {
                index /= 2;
            }
            // We actually want index % 8 for the AVX case (for SSE it will never be > 8).
            // Note that this doesn't matter functionally, because the instruction uses just the
            // low 3 bits of index, but it's better to use the right value.
            if (index > 8)
            {
                assert(compiler->getSIMDInstructionSet() == InstructionSet_AVX);
                index -= 8;
            }

            getEmitter()->emitIns_R_R_I(INS_pextrw, emitTypeSize(TYP_INT), targetReg, srcReg, index);

            bool ZeroOrSignExtnReqd = true;
            if (baseSize == 1)
            {
                if ((op2->gtIntCon.gtIconVal % 2) == 1)
                {
                    // Right shift extracted word by 8-bits if index is odd if we are extracting a byte sized element.
                    inst_RV_SH(INS_SHIFT_RIGHT_LOGICAL, EA_4BYTE, targetReg, 8);

                    // Since Pextrw zero extends to 32-bits, we need sign extension in case of TYP_BYTE
                    ZeroOrSignExtnReqd = (baseType == TYP_BYTE);
                }
                // else - we just need to zero/sign extend the byte since pextrw extracted 16-bits
            }
            else
            {
                // Since Pextrw zero extends to 32-bits, we need sign extension in case of TYP_SHORT
                assert(baseSize == 2);
                ZeroOrSignExtnReqd = (baseType == TYP_SHORT);
            }

            if (ZeroOrSignExtnReqd)
            {
                // Zero/sign extend the byte/short to 32-bits
                inst_RV_RV(ins_Move_Extend(baseType, false), targetReg, targetReg, baseType, emitTypeSize(baseType));
            }
        }
        else
        {
            // We need a temp xmm register if the baseType is not floating point and
            // accessing non-zero'th element.
            instruction ins;

            if (byteShiftCnt != 0)
            {
                assert(tmpReg != REG_NA);

                if (tmpReg != srcReg)
                {
                    inst_RV_RV(ins_Copy(simdType), tmpReg, srcReg, simdType, emitActualTypeSize(simdType));
                }

                ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, baseType);
                getEmitter()->emitIns_R_I(ins, emitActualTypeSize(simdType), tmpReg, byteShiftCnt);
            }
            else
            {
                tmpReg = srcReg;
            }

            assert(tmpReg != REG_NA);
            ins = ins_CopyFloatToInt(TYP_FLOAT, baseType);
            // (Note that for mov_xmm2i, the int register is always in the reg2 position.
            inst_RV_RV(ins, tmpReg, targetReg, baseType);
        }
    }

    genProduceReg(simdNode);
}

//------------------------------------------------------------------------------------
// genSIMDIntrinsicSetItem: Generate code for SIMD Intrinsic set element at index i.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
// TODO-CQ: Use SIMDIntrinsicShuffleSSE2 for the SSE2 case.
//
void CodeGen::genSIMDIntrinsicSetItem(GenTreeSIMD* simdNode)
{
    // Determine index based on intrinsic ID
    int index = -1;
    switch (simdNode->gtSIMDIntrinsicID)
    {
        case SIMDIntrinsicSetX:
            index = 0;
            break;
        case SIMDIntrinsicSetY:
            index = 1;
            break;
        case SIMDIntrinsicSetZ:
            index = 2;
            break;
        case SIMDIntrinsicSetW:
            index = 3;
            break;

        default:
            unreached();
    }
    assert(index != -1);

    // op1 is the SIMD vector
    // op2 is the value to be set
    GenTree* op1 = simdNode->gtGetOp1();
    GenTree* op2 = simdNode->gtGetOp2();

    var_types baseType  = simdNode->gtSIMDBaseType;
    regNumber targetReg = simdNode->gtRegNum;
    assert(targetReg != REG_NA);
    var_types targetType = simdNode->TypeGet();
    assert(varTypeIsSIMD(targetType));

    // the following assert must hold.
    // supported only on vector2f/3f/4f right now
    noway_assert(baseType == TYP_FLOAT);
    assert(op2->TypeGet() == baseType);
    assert(simdNode->gtSIMDSize >= ((index + 1) * genTypeSize(baseType)));

    genConsumeOperands(simdNode);
    regNumber op1Reg = op1->gtRegNum;
    regNumber op2Reg = op2->gtRegNum;

    // TODO-CQ: For AVX we don't need to do a copy because it supports 3 operands plus immediate.
    if (targetReg != op1Reg)
    {
        inst_RV_RV(ins_Copy(targetType), targetReg, op1Reg, targetType, emitActualTypeSize(targetType));
    }

    // Right now this intrinsic is supported only for float base type vectors.
    // If in future need to support on other base type vectors, the below
    // logic needs modification.
    noway_assert(baseType == TYP_FLOAT);

    if (compiler->getSIMDInstructionSet() == InstructionSet_SSE2)
    {
        // We need one additional int register as scratch
        assert(simdNode->gtRsvdRegs != RBM_NONE);
        assert(genCountBits(simdNode->gtRsvdRegs) == 1);
        regNumber tmpReg = genRegNumFromMask(simdNode->gtRsvdRegs);
        assert(genIsValidIntReg(tmpReg));

        // Move the value from xmm reg to an int reg
        instruction ins = ins_CopyFloatToInt(TYP_FLOAT, TYP_INT);
        // (Note that for mov_xmm2i, the int register is always in the reg2 position.
        inst_RV_RV(ins, op2Reg, tmpReg, baseType);

        // First insert the lower 16-bits of tmpReg in targetReg at 2*index position
        // since every float has two 16-bit words.
        getEmitter()->emitIns_R_R_I(INS_pinsrw, emitTypeSize(TYP_INT), targetReg, tmpReg, 2 * index);

        // Logical right shift tmpReg by 16-bits and insert in targetReg at 2*index + 1 position
        inst_RV_SH(INS_SHIFT_RIGHT_LOGICAL, EA_4BYTE, tmpReg, 16);
        getEmitter()->emitIns_R_R_I(INS_pinsrw, emitTypeSize(TYP_INT), targetReg, tmpReg, 2 * index + 1);
    }
    else
    {
        unsigned int insertpsImm = (INSERTPS_SOURCE_SELECT(0) | INSERTPS_TARGET_SELECT(index));
        inst_RV_RV_IV(INS_insertps, EA_16BYTE, targetReg, op2Reg, insertpsImm);
    }

    genProduceReg(simdNode);
}

//------------------------------------------------------------------------
// genSIMDIntrinsicShuffleSSE2: Generate code for SIMD Intrinsic shuffle.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicShuffleSSE2(GenTreeSIMD* simdNode)
{
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicShuffleSSE2);
    noway_assert(compiler->getSIMDInstructionSet() == InstructionSet_SSE2);

    GenTree* op1 = simdNode->gtGetOp1();
    GenTree* op2 = simdNode->gtGetOp2();
    assert(op2->isContained());
    assert(op2->IsCnsIntOrI());
    int       shuffleControl = (int)op2->AsIntConCommon()->IconValue();
    var_types baseType       = simdNode->gtSIMDBaseType;
    var_types targetType     = simdNode->TypeGet();
    regNumber targetReg      = simdNode->gtRegNum;
    assert(targetReg != REG_NA);

    regNumber op1Reg = genConsumeReg(op1);
    if (targetReg != op1Reg)
    {
        inst_RV_RV(ins_Copy(targetType), targetReg, op1Reg, targetType, emitActualTypeSize(targetType));
    }

    instruction ins = getOpForSIMDIntrinsic(simdNode->gtSIMDIntrinsicID, baseType);
    getEmitter()->emitIns_R_R_I(ins, emitTypeSize(baseType), targetReg, targetReg, shuffleControl);
    genProduceReg(simdNode);
}

//-----------------------------------------------------------------------------
// genStoreIndTypeSIMD12: store indirect a TYP_SIMD12 (i.e. Vector3) to memory.
// Since Vector3 is not a hardware supported write size, it is performed
// as two writes: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node that is attempting to store indirect
//
//
// Return Value:
//    None.
//
void CodeGen::genStoreIndTypeSIMD12(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_STOREIND);

    GenTree* addr = treeNode->gtOp.gtOp1;
    GenTree* data = treeNode->gtOp.gtOp2;

    // addr and data should not be contained.
    assert(!data->isContained());
    assert(!addr->isContained());

#ifdef DEBUG
    // Should not require a write barrier
    GCInfo::WriteBarrierForm writeBarrierForm = gcInfo.gcIsWriteBarrierCandidate(treeNode, data);
    assert(writeBarrierForm == GCInfo::WBF_NoBarrier);
#endif

    // Need an addtional Xmm register to extract upper 4 bytes from data.
    assert(treeNode->gtRsvdRegs != RBM_NONE);
    assert(genCountBits(treeNode->gtRsvdRegs) == 1);
    regNumber tmpReg = genRegNumFromMask(treeNode->gtRsvdRegs);

    genConsumeOperands(treeNode->AsOp());

    // 8-byte write
    getEmitter()->emitIns_AR_R(ins_Store(TYP_DOUBLE), EA_8BYTE, data->gtRegNum, addr->gtRegNum, 0);

    // Extract upper 4-bytes from data
    getEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(TYP_SIMD16), tmpReg, data->gtRegNum, 0x02);

    // 4-byte write
    getEmitter()->emitIns_AR_R(ins_Store(TYP_FLOAT), EA_4BYTE, tmpReg, addr->gtRegNum, 8);
}

//-----------------------------------------------------------------------------
// genLoadIndTypeSIMD12: load indirect a TYP_SIMD12 (i.e. Vector3) value.
// Since Vector3 is not a hardware supported write size, it is performed
// as two loads: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node of GT_IND
//
//
// Return Value:
//    None.
//
void CodeGen::genLoadIndTypeSIMD12(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_IND);

    regNumber  targetReg = treeNode->gtRegNum;
    GenTreePtr op1       = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());
    regNumber operandReg = genConsumeReg(op1);

    // Need an addtional Xmm register to read upper 4 bytes, which is different from targetReg
    assert(treeNode->gtRsvdRegs != RBM_NONE);
    assert(genCountBits(treeNode->gtRsvdRegs) == 2);

    regNumber tmpReg      = REG_NA;
    regMaskTP tmpRegsMask = treeNode->gtRsvdRegs;
    regMaskTP tmpReg1Mask = genFindLowestBit(tmpRegsMask);
    tmpRegsMask &= ~tmpReg1Mask;
    regNumber tmpReg1 = genRegNumFromMask(tmpReg1Mask);
    regNumber tmpReg2 = genRegNumFromMask(tmpRegsMask);

    // Choose any register different from targetReg as tmpReg
    if (tmpReg1 != targetReg)
    {
        tmpReg = tmpReg1;
    }
    else
    {
        assert(targetReg != tmpReg2);
        tmpReg = tmpReg2;
    }
    assert(tmpReg != REG_NA);
    assert(tmpReg != targetReg);

    // Load upper 4 bytes in tmpReg
    getEmitter()->emitIns_R_AR(ins_Load(TYP_FLOAT), EA_4BYTE, tmpReg, operandReg, 8);

    // Load lower 8 bytes in targetReg
    getEmitter()->emitIns_R_AR(ins_Load(TYP_DOUBLE), EA_8BYTE, targetReg, operandReg, 0);

    // combine upper 4 bytes and lower 8 bytes in targetReg
    getEmitter()->emitIns_R_R_I(INS_shufps, emitActualTypeSize(TYP_SIMD16), targetReg, tmpReg, 0x44);

    genProduceReg(treeNode);
}

//-----------------------------------------------------------------------------
// genStoreLclFldTypeSIMD12: store a TYP_SIMD12 (i.e. Vector3) type field.
// Since Vector3 is not a hardware supported write size, it is performed
// as two stores: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node that is attempting to store TYP_SIMD12 field
//
// Return Value:
//    None.
//
void CodeGen::genStoreLclFldTypeSIMD12(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_STORE_LCL_FLD);

    unsigned offs   = treeNode->gtLclFld.gtLclOffs;
    unsigned varNum = treeNode->gtLclVarCommon.gtLclNum;
    assert(varNum < compiler->lvaCount);

    GenTreePtr op1 = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());
    regNumber operandReg = genConsumeReg(op1);

    // Need an addtional Xmm register to extract upper 4 bytes from data.
    assert(treeNode->gtRsvdRegs != RBM_NONE);
    assert(genCountBits(treeNode->gtRsvdRegs) == 1);
    regNumber tmpReg = genRegNumFromMask(treeNode->gtRsvdRegs);

    // store lower 8 bytes
    getEmitter()->emitIns_S_R(ins_Store(TYP_DOUBLE), EA_8BYTE, operandReg, varNum, offs);

    // Extract upper 4-bytes from operandReg
    getEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(TYP_SIMD16), tmpReg, operandReg, 0x02);

    // Store upper 4 bytes
    getEmitter()->emitIns_S_R(ins_Store(TYP_FLOAT), EA_4BYTE, tmpReg, varNum, offs + 8);
}

//-----------------------------------------------------------------------------
// genLoadLclFldTypeSIMD12: load a TYP_SIMD12 (i.e. Vector3) type field.
// Since Vector3 is not a hardware supported write size, it is performed
// as two reads: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node that is attempting to load TYP_SIMD12 field
//
// Return Value:
//    None.
//
void CodeGen::genLoadLclFldTypeSIMD12(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_LCL_FLD);

    regNumber targetReg = treeNode->gtRegNum;
    unsigned  offs      = treeNode->gtLclFld.gtLclOffs;
    unsigned  varNum    = treeNode->gtLclVarCommon.gtLclNum;
    assert(varNum < compiler->lvaCount);

    // Need an addtional Xmm register to read upper 4 bytes
    assert(treeNode->gtRsvdRegs != RBM_NONE);
    assert(genCountBits(treeNode->gtRsvdRegs) == 2);

    regNumber tmpReg      = REG_NA;
    regMaskTP tmpRegsMask = treeNode->gtRsvdRegs;
    regMaskTP tmpReg1Mask = genFindLowestBit(tmpRegsMask);
    tmpRegsMask &= ~tmpReg1Mask;
    regNumber tmpReg1 = genRegNumFromMask(tmpReg1Mask);
    regNumber tmpReg2 = genRegNumFromMask(tmpRegsMask);

    // Choose any register different from targetReg as tmpReg
    if (tmpReg1 != targetReg)
    {
        tmpReg = tmpReg1;
    }
    else
    {
        assert(targetReg != tmpReg2);
        tmpReg = tmpReg2;
    }
    assert(tmpReg != REG_NA);
    assert(tmpReg != targetReg);

    // Read upper 4 bytes to tmpReg
    getEmitter()->emitIns_R_S(ins_Move_Extend(TYP_FLOAT, false), EA_4BYTE, tmpReg, varNum, offs + 8);

    // Read lower 8 bytes to targetReg
    getEmitter()->emitIns_R_S(ins_Move_Extend(TYP_DOUBLE, false), EA_8BYTE, targetReg, varNum, offs);

    // combine upper 4 bytes and lower 8 bytes in targetReg
    getEmitter()->emitIns_R_R_I(INS_shufps, emitActualTypeSize(TYP_SIMD16), targetReg, tmpReg, 0x44);

    genProduceReg(treeNode);
}

//-----------------------------------------------------------------------------
// genSIMDIntrinsicUpperSave: save the upper half of a TYP_SIMD32 vector to
//                            the given register, if any, or to memory.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
// Notes:
//    The upper half of all AVX registers is volatile, even the callee-save registers.
//    When a 32-byte SIMD value is live across a call, the register allocator will use this intrinsic
//    to cause the upper half to be saved.  It will first attempt to find another, unused, callee-save
//    register.  If such a register cannot be found, it will save it to an available caller-save register.
//    In that case, this node will be marked GTF_SPILL, which will cause genProduceReg to save the 16 byte
//    value to the stack.  (Note that if there are no caller-save registers available, the entire 32 byte
//    value will be spilled to the stack.)
//
void CodeGen::genSIMDIntrinsicUpperSave(GenTreeSIMD* simdNode)
{
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicUpperSave);

    GenTree* op1 = simdNode->gtGetOp1();
    assert(op1->IsLocal() && op1->TypeGet() == TYP_SIMD32);
    regNumber targetReg = simdNode->gtRegNum;
    regNumber op1Reg    = genConsumeReg(op1);
    assert(op1Reg != REG_NA);
    assert(targetReg != REG_NA);
    getEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, targetReg, op1Reg, 0x01);

    genProduceReg(simdNode);
}

//-----------------------------------------------------------------------------
// genSIMDIntrinsicUpperRestore: Restore the upper half of a TYP_SIMD32 vector to
//                               the given register, if any, or to memory.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
// Notes:
//    For consistency with genSIMDIntrinsicUpperSave, and to ensure that lclVar nodes always
//    have their home register, this node has its targetReg on the lclVar child, and its source
//    on the simdNode.
//    Regarding spill, please see the note above on genSIMDIntrinsicUpperSave.  If we have spilled
//    an upper-half to a caller save register, this node will be marked GTF_SPILLED.  However, unlike
//    most spill scenarios, the saved tree will be different from the restored tree, but the spill
//    restore logic, which is triggered by the call to genConsumeReg, requires us to provide the
//    spilled tree (saveNode) in order to perform the reload.  We can easily find that tree,
//    as it is in the spill descriptor for the register from which it was saved.
//
void CodeGen::genSIMDIntrinsicUpperRestore(GenTreeSIMD* simdNode)
{
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicUpperRestore);

    GenTree* op1 = simdNode->gtGetOp1();
    assert(op1->IsLocal() && op1->TypeGet() == TYP_SIMD32);
    regNumber srcReg    = simdNode->gtRegNum;
    regNumber lclVarReg = genConsumeReg(op1);
    unsigned  varNum    = op1->AsLclVarCommon()->gtLclNum;
    assert(lclVarReg != REG_NA);
    assert(srcReg != REG_NA);
    if (simdNode->gtFlags & GTF_SPILLED)
    {
        GenTree* saveNode = regSet.rsSpillDesc[srcReg]->spillTree;
        noway_assert(saveNode != nullptr && (saveNode->gtRegNum == srcReg));
        genConsumeReg(saveNode);
    }
    getEmitter()->emitIns_R_R_I(INS_vinsertf128, EA_32BYTE, lclVarReg, srcReg, 0x01);
}

//------------------------------------------------------------------------
// genSIMDIntrinsic: Generate code for a SIMD Intrinsic.  This is the main
// routine which in turn calls apropriate genSIMDIntrinsicXXX() routine.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
// Notes:
//    Currently, we only recognize SIMDVector<float> and SIMDVector<int>, and
//    a limited set of methods.
//
void CodeGen::genSIMDIntrinsic(GenTreeSIMD* simdNode)
{
    // NYI for unsupported base types
    if (simdNode->gtSIMDBaseType != TYP_INT && simdNode->gtSIMDBaseType != TYP_LONG &&
        simdNode->gtSIMDBaseType != TYP_FLOAT && simdNode->gtSIMDBaseType != TYP_DOUBLE &&
        simdNode->gtSIMDBaseType != TYP_CHAR && simdNode->gtSIMDBaseType != TYP_UBYTE &&
        simdNode->gtSIMDBaseType != TYP_SHORT && simdNode->gtSIMDBaseType != TYP_BYTE &&
        simdNode->gtSIMDBaseType != TYP_UINT && simdNode->gtSIMDBaseType != TYP_ULONG)
    {
        noway_assert(!"SIMD intrinsic with unsupported base type.");
    }

    switch (simdNode->gtSIMDIntrinsicID)
    {
        case SIMDIntrinsicInit:
            genSIMDIntrinsicInit(simdNode);
            break;

        case SIMDIntrinsicInitN:
            genSIMDIntrinsicInitN(simdNode);
            break;

        case SIMDIntrinsicSqrt:
        case SIMDIntrinsicCast:
            genSIMDIntrinsicUnOp(simdNode);
            break;

        case SIMDIntrinsicAdd:
        case SIMDIntrinsicSub:
        case SIMDIntrinsicMul:
        case SIMDIntrinsicDiv:
        case SIMDIntrinsicBitwiseAnd:
        case SIMDIntrinsicBitwiseAndNot:
        case SIMDIntrinsicBitwiseOr:
        case SIMDIntrinsicBitwiseXor:
        case SIMDIntrinsicMin:
        case SIMDIntrinsicMax:
            genSIMDIntrinsicBinOp(simdNode);
            break;

        case SIMDIntrinsicOpEquality:
        case SIMDIntrinsicOpInEquality:
        case SIMDIntrinsicEqual:
        case SIMDIntrinsicLessThan:
        case SIMDIntrinsicGreaterThan:
        case SIMDIntrinsicLessThanOrEqual:
        case SIMDIntrinsicGreaterThanOrEqual:
            genSIMDIntrinsicRelOp(simdNode);
            break;

        case SIMDIntrinsicDotProduct:
            genSIMDIntrinsicDotProduct(simdNode);
            break;

        case SIMDIntrinsicGetItem:
            genSIMDIntrinsicGetItem(simdNode);
            break;

        case SIMDIntrinsicShuffleSSE2:
            genSIMDIntrinsicShuffleSSE2(simdNode);
            break;

        case SIMDIntrinsicSetX:
        case SIMDIntrinsicSetY:
        case SIMDIntrinsicSetZ:
        case SIMDIntrinsicSetW:
            genSIMDIntrinsicSetItem(simdNode);
            break;

        case SIMDIntrinsicUpperSave:
            genSIMDIntrinsicUpperSave(simdNode);
            break;
        case SIMDIntrinsicUpperRestore:
            genSIMDIntrinsicUpperRestore(simdNode);
            break;

        default:
            noway_assert(!"Unimplemented SIMD intrinsic.");
            unreached();
    }
}

#endif // FEATURE_SIMD
#endif //_TARGET_AMD64_
#endif // !LEGACY_BACKEND
