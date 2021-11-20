// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#pragma warning(disable : 4310) // cast truncates constant value - happens for (int8_t)SHUFFLE_ZXXX
#endif

#ifdef TARGET_XARCH
#ifdef FEATURE_SIMD

#include "emit.h"
#include "codegen.h"
#include "sideeffects.h"
#include "lower.h"
#include "gcinfo.h"
#include "gcinfoencoder.h"

// Instruction immediates

// Insertps:
// - bits 6 and 7 of the immediate indicate which source item to select (0..3)
// - bits 4 and 5 of the immediate indicate which target item to insert into (0..3)
// - bits 0 to 3 of the immediate indicate which target item to zero
#define INSERTPS_SOURCE_SELECT(i) ((i) << 6)
#define INSERTPS_TARGET_SELECT(i) ((i) << 4)
#define INSERTPS_ZERO(i) (1 << (i))

// ROUNDPS/PD:
// - Bit 0 through 1 - Rounding mode
//   * 0b00 - Round to nearest (even)
//   * 0b01 - Round toward Neg. Infinity
//   * 0b10 - Round toward Pos. Infinity
//   * 0b11 - Round toward zero (Truncate)
// - Bit 2 - Source of rounding control, 0b0 for immediate.
// - Bit 3 - Precision exception, 0b1 to ignore. (We don't raise FP exceptions)
#define ROUNDPS_TO_NEAREST_IMM 0b1000
#define ROUNDPS_TOWARD_NEGATIVE_INFINITY_IMM 0b1001
#define ROUNDPS_TOWARD_POSITIVE_INFINITY_IMM 0b1010
#define ROUNDPS_TOWARD_ZERO_IMM 0b1011

// getOpForSIMDIntrinsic: return the opcode for the given SIMD Intrinsic
//
// Arguments:
//   intrinsicId    -   SIMD intrinsic Id
//   baseType       -   Base type of the SIMD vector
//   ival           -   Out param. Any immediate byte operand that needs to be passed to SSE2 opcode
//
//
// Return Value:
//   Instruction (op) to be used, and ival is set if instruction requires an immediate operand.
//
instruction CodeGen::getOpForSIMDIntrinsic(SIMDIntrinsicID intrinsicId, var_types baseType, unsigned* ival /*=nullptr*/)
{
    // Minimal required instruction set is SSE2.
    assert(compiler->getSIMDSupportLevel() >= SIMD_SSE2_Supported);

    instruction result = INS_invalid;
    switch (intrinsicId)
    {
        case SIMDIntrinsicInit:
            if (compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported)
            {
                // AVX supports broadcast instructions to populate YMM reg with a single float/double value from memory.
                // AVX2 supports broadcast instructions to populate YMM reg with a single value from memory or mm reg.
                switch (baseType)
                {
                    case TYP_FLOAT:
                        result = INS_vbroadcastss;
                        break;
                    case TYP_DOUBLE:
                        result = INS_vbroadcastsd;
                        break;
                    case TYP_ULONG:
                    case TYP_LONG:
                        // NOTE: for x86, this instruction is valid if the src is xmm2/m64, but NOT if it is supposed
                        // to be TYP_LONG reg.
                        result = INS_vpbroadcastq;
                        break;
                    case TYP_UINT:
                    case TYP_INT:
                        result = INS_vpbroadcastd;
                        break;
                    case TYP_USHORT:
                    case TYP_SHORT:
                        result = INS_vpbroadcastw;
                        break;
                    case TYP_UBYTE:
                    case TYP_BYTE:
                        result = INS_vpbroadcastb;
                        break;
                    default:
                        unreached();
                }
                break;
            }

            // For SSE, SIMDIntrinsicInit uses the same instruction as the SIMDIntrinsicShuffleSSE2 intrinsic.
            FALLTHROUGH;

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
                // We don't have a separate SSE2 instruction and will
                // use the instruction meant for doubles since it is
                // of the same size as a long.
                result = INS_shufpd;
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
            else if (baseType == TYP_USHORT || baseType == TYP_SHORT)
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
            else if (baseType == TYP_USHORT || baseType == TYP_SHORT)
            {
                result = INS_pcmpeqw;
            }
            else if (baseType == TYP_UBYTE || baseType == TYP_BYTE)
            {
                result = INS_pcmpeqb;
            }
            else if ((baseType == TYP_ULONG || baseType == TYP_LONG) &&
                     (compiler->getSIMDSupportLevel() >= SIMD_SSE4_Supported))
            {
                result = INS_pcmpeqq;
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

        case SIMDIntrinsicCast:
            result = INS_movaps;
            break;

        case SIMDIntrinsicConvertToSingle:
            result = INS_cvtdq2ps;
            break;

        case SIMDIntrinsicConvertToDouble:
            assert(baseType == TYP_LONG);
            result = INS_cvtsi2sd;
            break;

        case SIMDIntrinsicConvertToInt32:
            assert(baseType == TYP_FLOAT);
            result = INS_cvttps2dq;
            break;

        case SIMDIntrinsicConvertToInt64:
            assert(baseType == TYP_DOUBLE);
            result = INS_cvttsd2si;
            break;

        case SIMDIntrinsicShiftLeftInternal:
            switch (baseType)
            {
                case TYP_SIMD16:
                    // For SSE2, entire vector is shifted, for AVX2, 16-byte chunks are shifted.
                    result = INS_pslldq;
                    break;
                case TYP_UINT:
                case TYP_INT:
                    result = INS_pslld;
                    break;
                case TYP_SHORT:
                case TYP_USHORT:
                    result = INS_psllw;
                    break;
                default:
                    assert(!"Invalid baseType for SIMDIntrinsicShiftLeftInternal");
                    result = INS_invalid;
                    break;
            }
            break;

        case SIMDIntrinsicShiftRightInternal:
            switch (baseType)
            {
                case TYP_SIMD16:
                    // For SSE2, entire vector is shifted, for AVX2, 16-byte chunks are shifted.
                    result = INS_psrldq;
                    break;
                case TYP_UINT:
                case TYP_INT:
                    result = INS_psrld;
                    break;
                case TYP_SHORT:
                case TYP_USHORT:
                    result = INS_psrlw;
                    break;
                default:
                    assert(!"Invalid baseType for SIMDIntrinsicShiftRightInternal");
                    result = INS_invalid;
                    break;
            }
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
//    targetType       the target type
//    baseType         the base type of value to be moved
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
void CodeGen::genSIMDScalarMove(
    var_types targetType, var_types baseType, regNumber targetReg, regNumber srcReg, SIMDScalarMoveType moveType)
{
    assert(varTypeIsFloating(baseType));
    switch (moveType)
    {
        case SMT_PreserveUpper:
            GetEmitter()->emitIns_SIMD_R_R_R(ins_Store(baseType), emitTypeSize(baseType), targetReg, targetReg, srcReg);
            break;

        case SMT_ZeroInitUpper:
            if (compiler->canUseVexEncoding())
            {
                // insertps is a 128-bit only instruction, and clears the upper 128 bits, which is what we want.
                // The insertpsImm selects which fields are copied and zero'd of the lower 128 bits, so we choose
                // to zero all but the lower bits.
                unsigned int insertpsImm =
                    (INSERTPS_TARGET_SELECT(0) | INSERTPS_ZERO(1) | INSERTPS_ZERO(2) | INSERTPS_ZERO(3));
                assert((insertpsImm >= 0) && (insertpsImm <= 255));
                inst_RV_RV_IV(INS_insertps, EA_16BYTE, targetReg, srcReg, (int8_t)insertpsImm);
            }
            else
            {
                if (srcReg == targetReg)
                {
                    // There is no guarantee that upper bits of op1Reg are zero.
                    // We achieve this by using left logical shift 12-bytes and right logical shift 12 bytes.
                    instruction ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, TYP_SIMD16);
                    GetEmitter()->emitIns_R_I(ins, EA_16BYTE, srcReg, 12);
                    ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, TYP_SIMD16);
                    GetEmitter()->emitIns_R_I(ins, EA_16BYTE, srcReg, 12);
                }
                else
                {
                    genSIMDZero(targetType, TYP_FLOAT, targetReg);
                    inst_Mov(baseType, targetReg, srcReg, /* canSkip */ false);
                }
            }
            break;

        case SMT_ZeroInitUpper_SrcHasUpperZeros:
            inst_Mov(baseType, targetReg, srcReg, /* canSkip */ true);
            break;

        default:
            unreached();
    }
}

void CodeGen::genSIMDZero(var_types targetType, var_types baseType, regNumber targetReg)
{
    // We just use `INS_xorps` since `genSIMDZero` is used for both `System.Numerics.Vectors` and
    // HardwareIntrinsics. Modern CPUs handle this specially in the renamer and it never hits the
    // execution pipeline, additionally `INS_xorps` is always available (when using either the
    // legacy or VEX encoding).
    inst_RV_RV(INS_xorps, targetReg, targetReg, targetType, emitActualTypeSize(targetType));
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
    assert(simdNode->GetSIMDIntrinsicId() == SIMDIntrinsicInit);

    GenTree*  op1       = simdNode->Op(1);
    var_types baseType  = simdNode->GetSimdBaseType();
    regNumber targetReg = simdNode->GetRegNum();
    assert(targetReg != REG_NA);
    var_types targetType = simdNode->TypeGet();
    SIMDLevel level      = compiler->getSIMDSupportLevel();
    unsigned  size       = simdNode->GetSimdSize();

    // Should never see small int base type vectors except for zero initialization.
    noway_assert(!varTypeIsSmallInt(baseType) || op1->IsIntegralConst(0));

    instruction ins = INS_invalid;

#if !defined(TARGET_64BIT)
    if (op1->OperGet() == GT_LONG)
    {
        assert(varTypeIsLong(baseType));

        GenTree* op1lo = op1->gtGetOp1();
        GenTree* op1hi = op1->gtGetOp2();

        if (op1lo->IsIntegralConst(0) && op1hi->IsIntegralConst(0))
        {
            genSIMDZero(targetType, baseType, targetReg);
        }
        else if (op1lo->IsIntegralConst(-1) && op1hi->IsIntegralConst(-1))
        {
            // Initialize elements of vector with all 1's: generate pcmpeqd reg, reg.
            ins = getOpForSIMDIntrinsic(SIMDIntrinsicEqual, TYP_INT);
            inst_RV_RV(ins, targetReg, targetReg, targetType, emitActualTypeSize(targetType));
        }
        else
        {
            // Generate:
            //     mov_i2xmm targetReg, op1lo
            //     mov_i2xmm xmmtmp, op1hi
            //     shl xmmtmp, 4 bytes
            //     por targetReg, xmmtmp
            // Now, targetReg has the long in the low 64 bits. For SSE2, move it to the high 64 bits using:
            //     shufpd targetReg, targetReg, 0 // move the long to all the lanes
            // For AVX2, move it to all 4 of the 64-bit lanes using:
            //     vpbroadcastq targetReg, targetReg

            regNumber op1loReg = genConsumeReg(op1lo);
            inst_Mov(TYP_FLOAT, targetReg, op1loReg, /* canSkip */ false, emitActualTypeSize(TYP_INT));

            regNumber tmpReg = simdNode->GetSingleTempReg();

            regNumber op1hiReg = genConsumeReg(op1hi);
            inst_Mov(TYP_FLOAT, tmpReg, op1hiReg, /* canSkip */ false, emitActualTypeSize(TYP_INT));

            ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, TYP_SIMD16);
            GetEmitter()->emitIns_R_I(ins, EA_16BYTE, tmpReg, 4); // shift left by 4 bytes

            ins = getOpForSIMDIntrinsic(SIMDIntrinsicBitwiseOr, baseType);
            inst_RV_RV(ins, targetReg, tmpReg, targetType, emitActualTypeSize(targetType));

            if (compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported)
            {
                inst_RV_RV(INS_vpbroadcastq, targetReg, targetReg, TYP_SIMD32, emitTypeSize(TYP_SIMD32));
            }
            else
            {
                ins = getOpForSIMDIntrinsic(SIMDIntrinsicShuffleSSE2, baseType);
                GetEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(targetType), targetReg, targetReg, 0);
            }
        }
    }
    else
#endif // !defined(TARGET_64BIT)
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
        else
        {
            assert(level == SIMD_AVX2_Supported);
            ins = getOpForSIMDIntrinsic(SIMDIntrinsicInit, baseType);
            if (op1->IsCnsFltOrDbl())
            {
                GetEmitter()->emitInsBinary(ins, emitTypeSize(targetType), simdNode, op1);
            }
            else if (op1->OperIsLocalAddr())
            {
                const GenTreeLclVarCommon* lclVar = op1->AsLclVarCommon();
                unsigned                   offset = lclVar->GetLclOffs();
                GetEmitter()->emitIns_R_S(ins, emitTypeSize(targetType), targetReg, lclVar->GetLclNum(), offset);
            }
            else
            {
                unreached();
            }
        }
    }
    else if (level == SIMD_AVX2_Supported && ((size == 32) || (size == 16)))
    {
        regNumber srcReg = genConsumeReg(op1);
        if (baseType == TYP_INT || baseType == TYP_UINT || baseType == TYP_LONG || baseType == TYP_ULONG)
        {
            inst_Mov(TYP_FLOAT, targetReg, srcReg, /* canSkip */ false, emitTypeSize(baseType));
            srcReg = targetReg;
        }

        ins = getOpForSIMDIntrinsic(simdNode->GetSIMDIntrinsicId(), baseType);
        GetEmitter()->emitIns_R_R(ins, emitActualTypeSize(targetType), targetReg, srcReg);
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

            genSIMDScalarMove(targetType, TYP_FLOAT, targetReg, op1Reg, moveType);

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
            inst_Mov(TYP_FLOAT, targetReg, op1Reg, /* canSkip */ true, emitTypeSize(baseType));
        }

        ins = getOpForSIMDIntrinsic(SIMDIntrinsicShuffleSSE2, baseType);
        assert((shuffleControl >= 0) && (shuffleControl <= 255));
        GetEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(targetType), targetReg, targetReg, (int8_t)shuffleControl);
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
    assert(simdNode->GetSIMDIntrinsicId() == SIMDIntrinsicInitN);

    // Right now this intrinsic is supported only on TYP_FLOAT vectors
    var_types baseType = simdNode->GetSimdBaseType();
    noway_assert(baseType == TYP_FLOAT);

    regNumber targetReg = simdNode->GetRegNum();
    assert(targetReg != REG_NA);

    var_types targetType = simdNode->TypeGet();

    // Note that we cannot use targetReg before consumed all source operands. Therefore,
    // Need an internal register to stitch together all the values into a single vector
    // in an XMM reg.
    regNumber vectorReg = simdNode->GetSingleTempReg();

    // Zero out vectorReg if we are constructing a vector whose size is not equal to targetType vector size.
    // For example in case of Vector4f we don't need to zero when using SSE2.
    if (compiler->isSubRegisterSIMDType(simdNode))
    {
        genSIMDZero(targetType, baseType, vectorReg);
    }

    unsigned int baseTypeSize = genTypeSize(baseType);
    instruction  insLeftShift = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, TYP_SIMD16);

    // We will first consume the list items in execution (left to right) order,
    // and record the registers.
    regNumber operandRegs[SIMD_INTRINSIC_MAX_PARAM_COUNT];
    size_t    initCount = simdNode->GetOperandCount();
    for (size_t i = 1; i <= initCount; i++)
    {
        GenTree* operand = simdNode->Op(i);
        assert(operand->TypeIs(baseType));
        assert(!operand->isContained());

        operandRegs[i - 1] = genConsumeReg(operand);
    }

    unsigned offset = 0;
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
            assert((baseTypeSize >= 0) && (baseTypeSize <= 255));
            GetEmitter()->emitIns_R_I(insLeftShift, EA_16BYTE, vectorReg, (int8_t)baseTypeSize);
        }
        genSIMDScalarMove(targetType, baseType, vectorReg, operandReg, SMT_PreserveUpper);

        offset += baseTypeSize;
    }

    noway_assert(offset == simdNode->GetSimdSize());

    // Load the initialized value.
    inst_Mov(targetType, targetReg, vectorReg, /* canSkip */ true);
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
    assert(simdNode->GetSIMDIntrinsicId() == SIMDIntrinsicCast);

    GenTree*  op1       = simdNode->Op(1);
    var_types baseType  = simdNode->GetSimdBaseType();
    regNumber targetReg = simdNode->GetRegNum();
    assert(targetReg != REG_NA);
    var_types targetType = simdNode->TypeGet();

    regNumber   op1Reg = genConsumeReg(op1);
    instruction ins    = getOpForSIMDIntrinsic(simdNode->GetSIMDIntrinsicId(), baseType);
    if (simdNode->GetSIMDIntrinsicId() != SIMDIntrinsicCast)
    {
        inst_RV_RV(ins, targetReg, op1Reg, targetType, emitActualTypeSize(targetType));
    }
    else
    {
        inst_Mov(targetType, targetReg, op1Reg, /* canSkip */ true);
    }
    genProduceReg(simdNode);
}

//----------------------------------------------------------------------------------
// genSIMDIntrinsic32BitConvert: Generate code for 32-bit SIMD Convert (int/uint <-> float)
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsic32BitConvert(GenTreeSIMD* simdNode)
{
    SIMDIntrinsicID intrinsicID = simdNode->GetSIMDIntrinsicId();
    assert((intrinsicID == SIMDIntrinsicConvertToSingle) || (intrinsicID == SIMDIntrinsicConvertToInt32));

    GenTree*  op1       = simdNode->Op(1);
    var_types baseType  = simdNode->GetSimdBaseType();
    regNumber targetReg = simdNode->GetRegNum();
    assert(targetReg != REG_NA);
    var_types targetType = simdNode->TypeGet();

    regNumber   op1Reg = genConsumeReg(op1);
    instruction ins    = getOpForSIMDIntrinsic(simdNode->GetSIMDIntrinsicId(), baseType);
    if (intrinsicID == SIMDIntrinsicConvertToSingle && baseType == TYP_UINT)
    {
        regNumber tmpIntReg = simdNode->GetSingleTempReg(RBM_ALLINT);
        regNumber tmpReg    = simdNode->ExtractTempReg(RBM_ALLFLOAT);
        regNumber tmpReg2   = simdNode->GetSingleTempReg(RBM_ALLFLOAT);
        assert(tmpReg != op1Reg && tmpReg2 != op1Reg);

        // We will generate the following:
        //   vmovdqu  tmpReg2, op1Reg           (copy the src and put it into tmpReg2)
        //   vmovdqu  targetReg, op1Reg         (copy the src and put it into targetReg)
        //   vpsrld   targetReg, 16             (get upper 16 bits of src and put it into targetReg)
        //   vpslld   tmpReg2, 16
        //   vpsrld   tmpReg2, 16               (get lower 16 bits of src and put it into tmpReg2)
        //   mov      tmpIntReg, 0x5300000053000000
        //   vmovd    tmpReg, tmpIntReg
        //   vpbroadcastd tmpReg, tmpReg        (build mask for converting upper 16 bits of src)
        //   vorps    targetReg, tmpReg
        //   vsubps   targetReg, tmpReg         (convert upper 16 bits of src and put it into targetReg)
        //   vcvtdq2ps tmpReg2, tmpReg2         (convert lower 16 bits of src and put it into tmpReg2)
        //   vaddps   targetReg, tmpReg2        (add upper 16 bits and lower 16 bits)
        inst_Mov(targetType, tmpReg2, op1Reg, /* canSkip */ false);
        inst_Mov(targetType, targetReg, op1Reg, /* canSkip */ true);

        // prepare upper 16 bits
        GetEmitter()->emitIns_R_I(INS_psrld, emitActualTypeSize(targetType), targetReg, 16);

        // prepare lower 16 bits
        GetEmitter()->emitIns_R_I(INS_pslld, emitActualTypeSize(targetType), tmpReg2, 16);
        GetEmitter()->emitIns_R_I(INS_psrld, emitActualTypeSize(targetType), tmpReg2, 16);

// prepare mask
#ifdef TARGET_AMD64
        GetEmitter()->emitIns_R_I(INS_mov, EA_8BYTE, tmpIntReg, (ssize_t)0X5300000053000000);
        inst_Mov(targetType, tmpReg, tmpIntReg, /* canSkip */ false, emitActualTypeSize(TYP_ULONG));
#else
        if (compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported)
        {
            GetEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, tmpIntReg, (ssize_t)0X53000000);
            inst_Mov(targetType, tmpReg, tmpIntReg, /* canSkip */ false, emitActualTypeSize(TYP_UINT));
        }
        else
        {
            GetEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, tmpIntReg, (ssize_t)0X00005300);
            inst_RV_RV(INS_pxor, tmpReg, tmpReg, targetType, emitActualTypeSize(targetType));
            GetEmitter()->emitIns_R_R_I(INS_pinsrw, emitTypeSize(TYP_INT), tmpReg, tmpIntReg, 1);
            GetEmitter()->emitIns_R_R_I(INS_pinsrw, emitTypeSize(TYP_INT), tmpReg, tmpIntReg, 3);
        }
#endif
        if (compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported)
        {
            inst_RV_RV(INS_vpbroadcastd, tmpReg, tmpReg, targetType, emitActualTypeSize(targetType));
        }
        else
        {
            inst_RV_RV(INS_movlhps, tmpReg, tmpReg, targetType, emitActualTypeSize(targetType));
        }

        // convert upper 16 bits
        inst_RV_RV(INS_orps, targetReg, tmpReg, targetType, emitActualTypeSize(targetType));
        inst_RV_RV(INS_subps, targetReg, tmpReg, targetType, emitActualTypeSize(targetType));

        // convert lower 16 bits
        inst_RV_RV(ins, tmpReg2, tmpReg2, targetType, emitActualTypeSize(targetType));

        // add lower 16 bits and upper 16 bits
        inst_RV_RV(INS_addps, targetReg, tmpReg2, targetType, emitActualTypeSize(targetType));
    }
    else
    {
        inst_RV_RV(ins, targetReg, op1Reg, targetType, emitActualTypeSize(targetType));
    }
    genProduceReg(simdNode);
}

//----------------------------------------------------------------------------------
// genSIMDLo64BitConvert: Generate code to convert lower-most 64-bit item (long <--> double)
//
// Arguments:
//    intrinsicID      the SIMD intrinsic ID
//    simdType         the SIMD node type
//    baseType         the base type of value to be converted
//    tmpReg           the tmp reg
//    tmpIntReg        the tmp integer reg
//    targetReg        the target reg
//
// Return Value:
//    None.
//
void CodeGen::genSIMDLo64BitConvert(SIMDIntrinsicID intrinsicID,
                                    var_types       simdType,
                                    var_types       baseType,
                                    regNumber       tmpReg,
                                    regNumber       tmpIntReg,
                                    regNumber       targetReg)
{
    instruction ins = getOpForSIMDIntrinsic(intrinsicID, baseType);
    if (intrinsicID == SIMDIntrinsicConvertToDouble)
    {
        inst_Mov(TYP_LONG, tmpIntReg, tmpReg, /* canSkip */ false);
        inst_RV_RV(ins, targetReg, tmpIntReg, baseType, emitActualTypeSize(baseType));
    }
    else
    {
        inst_RV_RV(ins, tmpIntReg, tmpReg, baseType, emitActualTypeSize(baseType));
        inst_Mov(simdType, targetReg, tmpIntReg, /* canSkip */ false, emitActualTypeSize(TYP_LONG));
    }
}

//----------------------------------------------------------------------------------
// genSIMDIntrinsic64BitConvert: Generate code for 64-bit SIMD Convert (long/ulong <-> double)
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Notes:
//    There are no instructions for converting to/from 64-bit integers, so for these we
//    do the conversion an element at a time.
//
void CodeGen::genSIMDIntrinsic64BitConvert(GenTreeSIMD* simdNode)
{
    SIMDIntrinsicID intrinsicID = simdNode->GetSIMDIntrinsicId();
    assert((intrinsicID == SIMDIntrinsicConvertToDouble) || (intrinsicID == SIMDIntrinsicConvertToInt64));

    GenTree*  op1       = simdNode->Op(1);
    var_types baseType  = simdNode->GetSimdBaseType();
    regNumber targetReg = simdNode->GetRegNum();
    assert(targetReg != REG_NA);
    var_types simdType  = simdNode->TypeGet();
    regNumber op1Reg    = genConsumeReg(op1);
    regNumber tmpIntReg = simdNode->GetSingleTempReg(RBM_ALLINT);
    regNumber tmpReg;
    regNumber tmpReg2;
    regNumber tmpReg3;
    SIMDLevel level = compiler->getSIMDSupportLevel();

#ifdef TARGET_X86
    if (baseType == TYP_LONG)
    {
        tmpReg  = simdNode->ExtractTempReg(RBM_ALLFLOAT);
        tmpReg2 = simdNode->ExtractTempReg(RBM_ALLFLOAT);
        tmpReg3 = simdNode->GetSingleTempReg(RBM_ALLFLOAT);
        assert(tmpReg != op1Reg && tmpReg2 != op1Reg && tmpReg3 != op1Reg);
    }
    else
#endif
        if (level == SIMD_AVX2_Supported || (baseType == TYP_ULONG))
    {
        tmpReg  = simdNode->ExtractTempReg(RBM_ALLFLOAT);
        tmpReg2 = simdNode->GetSingleTempReg(RBM_ALLFLOAT);
        tmpReg3 = REG_NA;
        assert(tmpReg != op1Reg && tmpReg2 != op1Reg);
    }
    else
    {
        tmpReg = simdNode->GetSingleTempReg(RBM_ALLFLOAT);
        assert(tmpReg != op1Reg);
        tmpReg2 = REG_NA;
        tmpReg3 = REG_NA;
    }

    if ((intrinsicID == SIMDIntrinsicConvertToDouble) && (baseType == TYP_ULONG))
    {
        // We will generate the following
        //   vmovdqu  tmpReg2, op1Reg               (copy the src and put it into tmpReg2)
        //   vmovdqu  targetReg, op1Reg             (copy the src and put it into targetReg)
        //   vpsrlq   targetReg, 32                 (get upper 32 bits of src and put it into targetReg)
        //   vpsllq   tmpReg2, 32
        //   vpsrlq   tmpReg2, 32                   (get lower 32 bits of src and put it into tmpReg2)
        //   mov      tmpIntReg, 0x4530000000000000
        //   vmovd    tmpReg, tmpIntReg
        //   vpbroadcastq tmpReg, tmpReg            (build mask for upper 32 bits of src)
        //   vorpd    targetReg, tmpReg
        //   vsubpd   targetReg, tmpReg             (convert upper 32 bits of src and put it into targetReg)
        //   mov      tmpIntReg, 0x4330000000000000
        //   vmovd    tmpReg, tmpIntReg
        //   vpbroadcastq tmpReg, tmpReg            (build mask for lower 32 bits of src)
        //   vorpd    tmpReg2, tmpReg
        //   vsubpd   tmpReg2, tmpReg               (convert lower 32 bits of src and put it into tmpReg2)
        //   vaddpd   targetReg, tmpReg2            (add upper 32 bits and lower 32 bits together)
        inst_Mov(simdType, tmpReg2, op1Reg, /* canSkip */ false);
        inst_Mov(simdType, targetReg, op1Reg, /* canSkip */ true);

        // prepare upper 32 bits
        GetEmitter()->emitIns_R_I(INS_psrlq, emitActualTypeSize(simdType), targetReg, 32);

        // prepare lower 32 bits
        GetEmitter()->emitIns_R_I(INS_psllq, emitActualTypeSize(simdType), tmpReg2, 32);
        GetEmitter()->emitIns_R_I(INS_psrlq, emitActualTypeSize(simdType), tmpReg2, 32);

// prepare mask for converting upper 32 bits
#ifdef TARGET_AMD64
        GetEmitter()->emitIns_R_I(INS_mov, EA_8BYTE, tmpIntReg, (ssize_t)0X4530000000000000);
        inst_Mov(simdType, tmpReg, tmpIntReg, /* canSkip */ false, emitActualTypeSize(TYP_ULONG));
#else
        GetEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, tmpIntReg, (ssize_t)0X45300000);
        inst_Mov(simdType, tmpReg, tmpIntReg, /* canSkip */ false, emitActualTypeSize(TYP_UINT));
        GetEmitter()->emitIns_R_I(INS_pslldq, EA_16BYTE, tmpReg, 4);
#endif
        if (level == SIMD_AVX2_Supported)
        {
            inst_RV_RV(INS_vpbroadcastq, tmpReg, tmpReg, simdType, emitActualTypeSize(simdType));
        }
        else
        {
            inst_RV_RV(INS_movlhps, tmpReg, tmpReg, simdType, emitActualTypeSize(simdType));
        }

        // convert upper 32 bits
        inst_RV_RV(INS_orpd, targetReg, tmpReg, simdType, emitActualTypeSize(simdType));
        inst_RV_RV(INS_subpd, targetReg, tmpReg, simdType, emitActualTypeSize(simdType));

// prepare mask for converting lower 32 bits
#ifdef TARGET_AMD64
        GetEmitter()->emitIns_R_I(INS_mov, EA_8BYTE, tmpIntReg, (ssize_t)0X4330000000000000);
        inst_Mov(simdType, tmpReg, tmpIntReg, /* canSkip */ false, emitActualTypeSize(TYP_ULONG));
#else
        GetEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, tmpIntReg, (ssize_t)0X43300000);
        inst_Mov(simdType, tmpReg, tmpIntReg, /* canSkip */ false, emitActualTypeSize(TYP_UINT));
        GetEmitter()->emitIns_R_I(INS_pslldq, EA_16BYTE, tmpReg, 4);
#endif
        if (level == SIMD_AVX2_Supported)
        {
            inst_RV_RV(INS_vpbroadcastq, tmpReg, tmpReg, simdType, emitActualTypeSize(simdType));
        }
        else
        {
            inst_RV_RV(INS_movlhps, tmpReg, tmpReg, simdType, emitActualTypeSize(simdType));
        }

        // convert lower 32 bits
        inst_RV_RV(INS_orpd, tmpReg2, tmpReg, simdType, emitActualTypeSize(simdType));
        inst_RV_RV(INS_subpd, tmpReg2, tmpReg, simdType, emitActualTypeSize(simdType));

        // add lower 32 bits and upper 32 bits
        inst_RV_RV(INS_addpd, targetReg, tmpReg2, simdType, emitActualTypeSize(simdType));
    }
    else if ((intrinsicID == SIMDIntrinsicConvertToDouble) && (baseType == TYP_LONG))
    {
#ifdef TARGET_AMD64
        instruction rightShiftIns = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, TYP_SIMD16);
        instruction leftShiftIns  = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, TYP_SIMD16);

        if (level == SIMD_AVX2_Supported)
        {
            // Extract the high 16-bits
            GetEmitter()->emitIns_R_R_I(INS_vextracti128, EA_32BYTE, tmpReg, op1Reg, 0x01);

            // Put v[3] (the high-order element) in tmpReg2 and convert it.
            inst_Mov(simdType, tmpReg2, tmpReg, /* canSkip */ false);
            GetEmitter()->emitIns_R_I(rightShiftIns, emitActualTypeSize(simdType), tmpReg2, 8);
            genSIMDLo64BitConvert(intrinsicID, simdType, baseType, tmpReg2, tmpIntReg, tmpReg2);

            // Shift the resulting 64-bits left.
            GetEmitter()->emitIns_R_I(leftShiftIns, emitActualTypeSize(simdType), tmpReg2, 8);

            // Convert v[2], in the lo bits of tmpReg.
            // For the convert to double, the convert preserves the upper bits in tmpReg2.
            // For the integer convert, we have to put it in tmpReg and or it in, since movd clears the upper bits.
            genSIMDLo64BitConvert(intrinsicID, simdType, baseType, tmpReg, tmpIntReg, tmpReg2);
        }

        // Put v[1] in tmpReg.
        inst_Mov(simdType, tmpReg, op1Reg, /* canSkip */ false);
        GetEmitter()->emitIns_R_I(rightShiftIns, emitActualTypeSize(simdType), tmpReg, 8);

        // At this point we have v[1] in the low-order 64-bits of tmpReg. Convert it.
        genSIMDLo64BitConvert(intrinsicID, simdType, baseType, tmpReg, tmpIntReg, tmpReg);

        // Shift the resulting 64-bits left.
        GetEmitter()->emitIns_R_I(leftShiftIns, emitActualTypeSize(simdType), tmpReg, 8);

        // Convert the lo 64-bits into targetReg
        genSIMDLo64BitConvert(intrinsicID, simdType, baseType, op1Reg, tmpIntReg, tmpReg);

        // Merge or copy the results (only at this point are we done with op1Reg).
        inst_Mov(simdType, targetReg, tmpReg, /* canSkip */ true);

        if (level == SIMD_AVX2_Supported)
        {
            GetEmitter()->emitIns_R_R_I(INS_vinsertf128, EA_32BYTE, targetReg, tmpReg2, 0x01);
        }
#else
        // get the sign bit and put it in tmpReg3
        inst_Mov(simdType, tmpReg3, op1Reg, /* canSkip */ false);
        GetEmitter()->emitIns_R_I(INS_psrlq, emitActualTypeSize(simdType), tmpReg3, 63);
        GetEmitter()->emitIns_R_I(INS_psllq, emitActualTypeSize(simdType), tmpReg3, 63);

        // get the absolute value of src and put it into tmpReg2 and targetReg
        inst_Mov(simdType, tmpReg2, op1Reg, /* canSkip */ false);
        GetEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(simdType), tmpReg, op1Reg, (int8_t)SHUFFLE_WWYY);
        GetEmitter()->emitIns_R_I(INS_psrad, emitActualTypeSize(simdType), tmpReg, 32);
        inst_RV_RV(INS_pxor, tmpReg2, tmpReg, baseType, emitActualTypeSize(simdType));
        inst_RV_RV(INS_psubq, tmpReg2, tmpReg, baseType, emitActualTypeSize(simdType));
        inst_Mov(simdType, targetReg, tmpReg2, /* canSkip */ false);

        // prepare upper 32 bits
        GetEmitter()->emitIns_R_I(INS_psrlq, emitActualTypeSize(simdType), targetReg, 32);

        // prepare lower 32 bits
        GetEmitter()->emitIns_R_I(INS_psllq, emitActualTypeSize(simdType), tmpReg2, 32);
        GetEmitter()->emitIns_R_I(INS_psrlq, emitActualTypeSize(simdType), tmpReg2, 32);

        // prepare mask for converting upper 32 bits
        GetEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, tmpIntReg, (ssize_t)0X45300000);
        inst_Mov(simdType, tmpReg, tmpIntReg, /* canSkip */ false, emitActualTypeSize(TYP_UINT));
        GetEmitter()->emitIns_R_I(INS_pslldq, EA_16BYTE, tmpReg, 4);

        if (level == SIMD_AVX2_Supported)
        {
            inst_RV_RV(INS_vpbroadcastq, tmpReg, tmpReg, simdType, emitActualTypeSize(simdType));
        }
        else
        {
            inst_RV_RV(INS_movlhps, tmpReg, tmpReg, simdType, emitActualTypeSize(simdType));
        }

        // convert upper 32 bits
        inst_RV_RV(INS_orpd, targetReg, tmpReg, simdType, emitActualTypeSize(simdType));
        inst_RV_RV(INS_subpd, targetReg, tmpReg, simdType, emitActualTypeSize(simdType));

        // prepare mask for converting lower 32 bits
        GetEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, tmpIntReg, (ssize_t)0X43300000);
        inst_Mov(simdType, tmpReg, tmpIntReg, /* canSkip */ false, emitActualTypeSize(TYP_UINT));
        GetEmitter()->emitIns_R_I(INS_pslldq, EA_16BYTE, tmpReg, 4);

        if (level == SIMD_AVX2_Supported)
        {
            inst_RV_RV(INS_vpbroadcastq, tmpReg, tmpReg, simdType, emitActualTypeSize(simdType));
        }
        else
        {
            inst_RV_RV(INS_movlhps, tmpReg, tmpReg, simdType, emitActualTypeSize(simdType));
        }

        // convert lower 32 bits
        inst_RV_RV(INS_orpd, tmpReg2, tmpReg, simdType, emitActualTypeSize(simdType));
        inst_RV_RV(INS_subpd, tmpReg2, tmpReg, simdType, emitActualTypeSize(simdType));

        // add lower 32 bits and upper 32 bits
        inst_RV_RV(INS_addpd, targetReg, tmpReg2, simdType, emitActualTypeSize(simdType));

        // add sign bit
        inst_RV_RV(INS_por, targetReg, tmpReg3, simdType, emitActualTypeSize(simdType));
#endif
    }
    else
    {
        instruction rightShiftIns = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, TYP_SIMD16);
        instruction leftShiftIns  = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, TYP_SIMD16);

        if (level == SIMD_AVX2_Supported)
        {
            // Extract the high 16-bits
            GetEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, tmpReg, op1Reg, 0x01);

            // Put v[3] (the high-order element) in tmpReg2 and convert it.
            inst_Mov(simdType, tmpReg2, tmpReg, /* canSkip */ false);
            GetEmitter()->emitIns_R_I(rightShiftIns, emitActualTypeSize(simdType), tmpReg2, 8);
            genSIMDLo64BitConvert(intrinsicID, simdType, baseType, tmpReg2, tmpIntReg, tmpReg2);

            // Shift the resulting 64-bits left.
            GetEmitter()->emitIns_R_I(leftShiftIns, emitActualTypeSize(simdType), tmpReg2, 8);

            // Convert v[2], in the lo bits of tmpReg.
            // For the convert to double, the convert preserves the upper bits in tmpReg2.
            // For the integer convert, we have to put it in tmpReg and or it in, since movd clears the upper bits.
            genSIMDLo64BitConvert(intrinsicID, simdType, baseType, tmpReg, tmpIntReg, tmpReg);
            inst_RV_RV(INS_por, tmpReg2, tmpReg, simdType, emitActualTypeSize(simdType));
        }

        // Put v[1] in tmpReg.
        inst_Mov(simdType, tmpReg, op1Reg, /* canSkip */ false);
        GetEmitter()->emitIns_R_I(rightShiftIns, emitActualTypeSize(simdType), tmpReg, 8);

        // At this point we have v[1] in the low-order 64-bits of tmpReg. Convert it.
        genSIMDLo64BitConvert(intrinsicID, simdType, baseType, tmpReg, tmpIntReg, tmpReg);

        // Shift the resulting 64-bits left.
        GetEmitter()->emitIns_R_I(leftShiftIns, emitActualTypeSize(simdType), tmpReg, 8);

        // Convert the lo 64-bits into targetReg
        genSIMDLo64BitConvert(intrinsicID, simdType, baseType, op1Reg, tmpIntReg, targetReg);

        // Merge or copy the results (only at this point are we done with op1Reg).
        assert(tmpReg != targetReg);
        inst_RV_RV(INS_por, targetReg, tmpReg, simdType, emitActualTypeSize(simdType));
        if (level == SIMD_AVX2_Supported)
        {
            GetEmitter()->emitIns_R_R_I(INS_vinserti128, EA_32BYTE, targetReg, tmpReg2, 0x01);
        }
    }
    genProduceReg(simdNode);
}

//--------------------------------------------------------------------------------
// genSIMDExtractUpperHalf: Generate code to extract the upper half of a SIMD register
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Notes:
//    This is used for the WidenHi intrinsic to extract the upper half.
//    On SSE*, this is 8 bytes, and on AVX2 it is 16 bytes.
//
void CodeGen::genSIMDExtractUpperHalf(GenTreeSIMD* simdNode, regNumber srcReg, regNumber tgtReg)
{
    var_types simdType = simdNode->TypeGet();
    emitAttr  emitSize = emitActualTypeSize(simdType);
    if (compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported)
    {
        instruction extractIns = varTypeIsFloating(simdNode->GetSimdBaseType()) ? INS_vextractf128 : INS_vextracti128;
        GetEmitter()->emitIns_R_R_I(extractIns, EA_32BYTE, tgtReg, srcReg, 0x01);
    }
    else
    {
        instruction shiftIns = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, TYP_SIMD16);
        inst_Mov(simdType, tgtReg, srcReg, /* canSkip */ true);
        GetEmitter()->emitIns_R_I(shiftIns, emitSize, tgtReg, 8);
    }
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
    assert((simdNode->GetSIMDIntrinsicId() == SIMDIntrinsicSub) ||
           (simdNode->GetSIMDIntrinsicId() == SIMDIntrinsicBitwiseAnd) ||
           (simdNode->GetSIMDIntrinsicId() == SIMDIntrinsicBitwiseOr));

    GenTree*  op1       = simdNode->Op(1);
    GenTree*  op2       = simdNode->Op(2);
    var_types baseType  = simdNode->GetSimdBaseType();
    regNumber targetReg = simdNode->GetRegNum();
    assert(targetReg != REG_NA);
    var_types targetType = simdNode->TypeGet();

    genConsumeMultiOpOperands(simdNode);
    regNumber op1Reg   = op1->GetRegNum();
    regNumber op2Reg   = op2->GetRegNum();
    regNumber otherReg = op2Reg;

    instruction ins = getOpForSIMDIntrinsic(simdNode->GetSIMDIntrinsicId(), baseType);

    // Currently AVX doesn't support integer.
    // if the ins is INS_cvtsi2ss or INS_cvtsi2sd, we won't use AVX.
    if (op1Reg != targetReg && compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported &&
        !(ins == INS_cvtsi2ss || ins == INS_cvtsi2sd) && GetEmitter()->IsThreeOperandAVXInstruction(ins))
    {
        inst_RV_RV_RV(ins, targetReg, op1Reg, op2Reg, emitActualTypeSize(targetType));
    }
    else
    {
        if (op2Reg == targetReg)
        {
            otherReg = op1Reg;
        }
        else
        {
            inst_Mov(targetType, targetReg, op1Reg, /* canSkip */ true);
        }

        inst_RV_RV(ins, targetReg, otherReg, targetType, emitActualTypeSize(targetType));
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
    GenTree*  op1        = simdNode->Op(1);
    GenTree*  op2        = simdNode->Op(2);
    var_types baseType   = simdNode->GetSimdBaseType();
    regNumber targetReg  = simdNode->GetRegNum();
    var_types targetType = simdNode->TypeGet();
    SIMDLevel level      = compiler->getSIMDSupportLevel();

    genConsumeMultiOpOperands(simdNode);
    regNumber op1Reg   = op1->GetRegNum();
    regNumber op2Reg   = op2->GetRegNum();
    regNumber otherReg = op2Reg;

    switch (simdNode->GetSIMDIntrinsicId())
    {
        case SIMDIntrinsicEqual:
        {
            assert(targetReg != REG_NA);

#ifdef DEBUG
            // SSE2: vector<(u)long> relational op should be implemented in terms of
            // TYP_INT comparison operations
            if (baseType == TYP_LONG || baseType == TYP_ULONG)
            {
                assert(level >= SIMD_SSE4_Supported);
            }
#endif

            unsigned    ival = 0;
            instruction ins  = getOpForSIMDIntrinsic(simdNode->GetSIMDIntrinsicId(), baseType, &ival);

            // targetReg = op1reg > op2reg
            // Therefore, we can optimize if op1Reg == targetReg
            otherReg = op2Reg;
            if (op1Reg != targetReg)
            {
                if (op2Reg == targetReg)
                {
                    assert(simdNode->GetSIMDIntrinsicId() == SIMDIntrinsicEqual);
                    otherReg = op1Reg;
                }
                else
                {
                    inst_Mov(targetType, targetReg, op1Reg, /* canSkip */ false);
                }
            }

            if (varTypeIsFloating(baseType))
            {
                assert((ival >= 0) && (ival <= 255));
                GetEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(targetType), targetReg, otherReg, (int8_t)ival);
            }
            else
            {
                inst_RV_RV(ins, targetReg, otherReg, targetType, emitActualTypeSize(targetType));
            }
        }
        break;

        default:
            noway_assert(!"Unimplemented SIMD relational operation.");
            unreached();
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
    assert(simdNode->GetSIMDIntrinsicId() == SIMDIntrinsicShuffleSSE2);
    noway_assert(compiler->getSIMDSupportLevel() == SIMD_SSE2_Supported);

    GenTree* op1 = simdNode->Op(1);
    GenTree* op2 = simdNode->Op(2);
    assert(op2->isContained());
    assert(op2->IsCnsIntOrI());
    ssize_t   shuffleControl = op2->AsIntConCommon()->IconValue();
    var_types baseType       = simdNode->GetSimdBaseType();
    var_types targetType     = simdNode->TypeGet();
    regNumber targetReg      = simdNode->GetRegNum();
    assert(targetReg != REG_NA);

    regNumber op1Reg = genConsumeReg(op1);
    inst_Mov(targetType, targetReg, op1Reg, /* canSkip */ true);

    instruction ins = getOpForSIMDIntrinsic(simdNode->GetSIMDIntrinsicId(), baseType);
    assert((shuffleControl >= 0) && (shuffleControl <= 255));
    GetEmitter()->emitIns_R_R_I(ins, emitTypeSize(baseType), targetReg, targetReg, (int8_t)shuffleControl);
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

    GenTree* addr = treeNode->AsOp()->gtOp1;
    GenTree* data = treeNode->AsOp()->gtOp2;

    // addr and data should not be contained.
    assert(!data->isContained());
    assert(!addr->isContained());

#ifdef DEBUG
    // Should not require a write barrier
    GCInfo::WriteBarrierForm writeBarrierForm = gcInfo.gcIsWriteBarrierCandidate(treeNode, data);
    assert(writeBarrierForm == GCInfo::WBF_NoBarrier);
#endif

    // Need an addtional Xmm register to extract upper 4 bytes from data.
    regNumber tmpReg = treeNode->GetSingleTempReg();

    genConsumeOperands(treeNode->AsOp());

    // 8-byte write
    GetEmitter()->emitIns_AR_R(ins_Store(TYP_DOUBLE), EA_8BYTE, data->GetRegNum(), addr->GetRegNum(), 0);

    // Extract upper 4-bytes from data
    GetEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(TYP_SIMD16), tmpReg, data->GetRegNum(), 0x02);

    // 4-byte write
    GetEmitter()->emitIns_AR_R(ins_Store(TYP_FLOAT), EA_4BYTE, tmpReg, addr->GetRegNum(), 8);
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

    regNumber targetReg = treeNode->GetRegNum();
    GenTree*  op1       = treeNode->AsOp()->gtOp1;
    assert(!op1->isContained());
    regNumber operandReg = genConsumeReg(op1);

    // Need an addtional Xmm register to read upper 4 bytes, which is different from targetReg
    regNumber tmpReg = treeNode->GetSingleTempReg();
    assert(tmpReg != targetReg);

    // Load upper 4 bytes in tmpReg
    GetEmitter()->emitIns_R_AR(ins_Load(TYP_FLOAT), EA_4BYTE, tmpReg, operandReg, 8);

    // Load lower 8 bytes in targetReg
    GetEmitter()->emitIns_R_AR(ins_Load(TYP_DOUBLE), EA_8BYTE, targetReg, operandReg, 0);

    // combine upper 4 bytes and lower 8 bytes in targetReg
    GetEmitter()->emitIns_R_R_I(INS_shufps, emitActualTypeSize(TYP_SIMD16), targetReg, tmpReg, (int8_t)SHUFFLE_YXYX);

    genProduceReg(treeNode);
}

//-----------------------------------------------------------------------------
// genStoreLclTypeSIMD12: store a TYP_SIMD12 (i.e. Vector3) type field.
// Since Vector3 is not a hardware supported write size, it is performed
// as two stores: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node that is attempting to store TYP_SIMD12 field
//
// Return Value:
//    None.
//
void CodeGen::genStoreLclTypeSIMD12(GenTree* treeNode)
{
    assert((treeNode->OperGet() == GT_STORE_LCL_FLD) || (treeNode->OperGet() == GT_STORE_LCL_VAR));

    const GenTreeLclVarCommon* lclVar = treeNode->AsLclVarCommon();

    unsigned offs   = lclVar->GetLclOffs();
    unsigned varNum = lclVar->GetLclNum();
    assert(varNum < compiler->lvaCount);

    regNumber tmpReg = treeNode->GetSingleTempReg();
    GenTree*  op1    = lclVar->gtOp1;
    if (op1->isContained())
    {
        // This is only possible for a zero-init.
        assert(op1->IsIntegralConst(0) || op1->IsSIMDZero());
        genSIMDZero(TYP_SIMD16, op1->AsSIMD()->GetSimdBaseType(), tmpReg);

        // store lower 8 bytes
        GetEmitter()->emitIns_S_R(ins_Store(TYP_DOUBLE), EA_8BYTE, tmpReg, varNum, offs);

        // Store upper 4 bytes
        GetEmitter()->emitIns_S_R(ins_Store(TYP_FLOAT), EA_4BYTE, tmpReg, varNum, offs + 8);

        return;
    }

    assert(!op1->isContained());
    regNumber operandReg = genConsumeReg(op1);

    // store lower 8 bytes
    GetEmitter()->emitIns_S_R(ins_Store(TYP_DOUBLE), EA_8BYTE, operandReg, varNum, offs);

    // Extract upper 4-bytes from operandReg
    GetEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(TYP_SIMD16), tmpReg, operandReg, 0x02);

    // Store upper 4 bytes
    GetEmitter()->emitIns_S_R(ins_Store(TYP_FLOAT), EA_4BYTE, tmpReg, varNum, offs + 8);
}

//-----------------------------------------------------------------------------
// genLoadLclTypeSIMD12: load a TYP_SIMD12 (i.e. Vector3) type field.
// Since Vector3 is not a hardware supported read size, it is performed
// as two reads: 4 byte followed by 8 byte.
//
// Arguments:
//    treeNode - tree node that is attempting to load TYP_SIMD12 field
//
// Return Value:
//    None.
//
void CodeGen::genLoadLclTypeSIMD12(GenTree* treeNode)
{
    assert((treeNode->OperGet() == GT_LCL_FLD) || (treeNode->OperGet() == GT_LCL_VAR));

    const GenTreeLclVarCommon* lclVar    = treeNode->AsLclVarCommon();
    regNumber                  targetReg = lclVar->GetRegNum();
    unsigned                   offs      = lclVar->GetLclOffs();
    unsigned                   varNum    = lclVar->GetLclNum();
    assert(varNum < compiler->lvaCount);

    // Need an additional Xmm register that is different from targetReg to read upper 4 bytes.
    regNumber tmpReg = treeNode->GetSingleTempReg();
    assert(tmpReg != targetReg);

    // Read upper 4 bytes to tmpReg
    GetEmitter()->emitIns_R_S(ins_Move_Extend(TYP_FLOAT, false), EA_4BYTE, tmpReg, varNum, offs + 8);

    // Read lower 8 bytes to targetReg
    GetEmitter()->emitIns_R_S(ins_Move_Extend(TYP_DOUBLE, false), EA_8BYTE, targetReg, varNum, offs);

    // combine upper 4 bytes and lower 8 bytes in targetReg
    GetEmitter()->emitIns_R_R_I(INS_shufps, emitActualTypeSize(TYP_SIMD16), targetReg, tmpReg, (int8_t)SHUFFLE_YXYX);

    genProduceReg(treeNode);
}

#ifdef TARGET_X86

//-----------------------------------------------------------------------------
// genStoreSIMD12ToStack: store a TYP_SIMD12 (i.e. Vector3) type field to the stack.
// Since Vector3 is not a hardware supported write size, it is performed
// as two stores: 8 byte followed by 4-byte. The stack is assumed to have
// already been adjusted.
//
// Arguments:
//    operandReg - the xmm register containing the SIMD12 to store.
//    tmpReg - an xmm register that can be used as a temporary for the operation.
//
// Return Value:
//    None.
//
void CodeGen::genStoreSIMD12ToStack(regNumber operandReg, regNumber tmpReg)
{
    assert(genIsValidFloatReg(operandReg));
    assert(genIsValidFloatReg(tmpReg));

    // 8-byte write
    GetEmitter()->emitIns_AR_R(ins_Store(TYP_DOUBLE), EA_8BYTE, operandReg, REG_SPBASE, 0);

    // Extract upper 4-bytes from data
    GetEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(TYP_SIMD16), tmpReg, operandReg, 0x02);

    // 4-byte write
    GetEmitter()->emitIns_AR_R(ins_Store(TYP_FLOAT), EA_4BYTE, tmpReg, REG_SPBASE, 8);
}

//-----------------------------------------------------------------------------
// genPutArgStkSIMD12: store a TYP_SIMD12 (i.e. Vector3) type field.
// Since Vector3 is not a hardware supported write size, it is performed
// as two stores: 8 byte followed by 4-byte. The stack is assumed to have
// already been adjusted.
//
// Arguments:
//    treeNode - tree node that is attempting to store TYP_SIMD12 field
//
// Return Value:
//    None.
//
void CodeGen::genPutArgStkSIMD12(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_PUTARG_STK);

    GenTree* op1 = treeNode->AsOp()->gtOp1;
    assert(!op1->isContained());
    regNumber operandReg = genConsumeReg(op1);

    // Need an addtional Xmm register to extract upper 4 bytes from data.
    regNumber tmpReg = treeNode->GetSingleTempReg();

    genStoreSIMD12ToStack(operandReg, tmpReg);
}

#endif // TARGET_X86

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
//    register.  If such a register cannot be found, it will save the upper half to the upper half
//    of the localVar's home location.
//    (Note that if there are no caller-save registers available, the entire 32 byte
//    value will be spilled to the stack.)
//
void CodeGen::genSIMDIntrinsicUpperSave(GenTreeSIMD* simdNode)
{
    assert(simdNode->GetSIMDIntrinsicId() == SIMDIntrinsicUpperSave);

    GenTree* op1 = simdNode->Op(1);
    assert(op1->IsLocal() && op1->TypeGet() == TYP_SIMD32);
    regNumber targetReg = simdNode->GetRegNum();
    regNumber op1Reg    = genConsumeReg(op1);
    assert(op1Reg != REG_NA);
    if (targetReg != REG_NA)
    {
        GetEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, targetReg, op1Reg, 0x01);
        genProduceReg(simdNode);
    }
    else
    {
        // The localVar must have a stack home.
        unsigned   varNum = op1->AsLclVarCommon()->GetLclNum();
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
        assert(varDsc->lvOnFrame);
        // We want to store this to the upper 16 bytes of this localVar's home.
        int offs = 16;

        GetEmitter()->emitIns_S_R_I(INS_vextractf128, EA_32BYTE, varNum, offs, op1Reg, 0x01);
    }
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
//
void CodeGen::genSIMDIntrinsicUpperRestore(GenTreeSIMD* simdNode)
{
    assert(simdNode->GetSIMDIntrinsicId() == SIMDIntrinsicUpperRestore);

    GenTree* op1 = simdNode->Op(1);
    assert(op1->IsLocal() && op1->TypeGet() == TYP_SIMD32);
    regNumber srcReg    = simdNode->GetRegNum();
    regNumber lclVarReg = genConsumeReg(op1);
    assert(lclVarReg != REG_NA);
    if (srcReg != REG_NA)
    {
        GetEmitter()->emitIns_R_R_R_I(INS_vinsertf128, EA_32BYTE, lclVarReg, lclVarReg, srcReg, 0x01);
    }
    else
    {
        // The localVar must have a stack home.
        unsigned   varNum = op1->AsLclVarCommon()->GetLclNum();
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
        assert(varDsc->lvOnFrame);
        // We will load this from the upper 16 bytes of this localVar's home.
        int offs = 16;
        GetEmitter()->emitIns_R_R_S_I(INS_vinsertf128, EA_32BYTE, lclVarReg, lclVarReg, varNum, offs, 0x01);
    }
}

//------------------------------------------------------------------------
// genSIMDIntrinsic: Generate code for a SIMD Intrinsic.  This is the main
// routine which in turn calls appropriate genSIMDIntrinsicXXX() routine.
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
    if (!varTypeIsArithmetic(simdNode->GetSimdBaseType()))
    {
        noway_assert(!"SIMD intrinsic with unsupported base type.");
    }

    switch (simdNode->GetSIMDIntrinsicId())
    {
        case SIMDIntrinsicInit:
            genSIMDIntrinsicInit(simdNode);
            break;

        case SIMDIntrinsicInitN:
            genSIMDIntrinsicInitN(simdNode);
            break;

        case SIMDIntrinsicCast:
            genSIMDIntrinsicUnOp(simdNode);
            break;

        case SIMDIntrinsicConvertToSingle:
        case SIMDIntrinsicConvertToInt32:
            genSIMDIntrinsic32BitConvert(simdNode);
            break;

        case SIMDIntrinsicConvertToDouble:
        case SIMDIntrinsicConvertToInt64:
            genSIMDIntrinsic64BitConvert(simdNode);
            break;

        case SIMDIntrinsicSub:
        case SIMDIntrinsicBitwiseAnd:
        case SIMDIntrinsicBitwiseOr:
            genSIMDIntrinsicBinOp(simdNode);
            break;

        case SIMDIntrinsicEqual:
            genSIMDIntrinsicRelOp(simdNode);
            break;

        case SIMDIntrinsicShuffleSSE2:
            genSIMDIntrinsicShuffleSSE2(simdNode);
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
#endif // TARGET_XARCH
