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

#ifdef _TARGET_XARCH_
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
    assert(compiler->getSIMDSupportLevel() >= SIMD_SSE2_Supported);

    instruction result = INS_invalid;
    switch (intrinsicId)
    {
        case SIMDIntrinsicInit:
            if (compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported)
            {
                // AVX supports broadcast instructions to populate YMM reg with a single float/double value from memory.
                // AVX2 supports broadcast instructions to populate YMM reg with a single value from memory or mm reg.
                // If we decide to use AVX2 only, we can remove this assert.
                if (!compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_USE_AVX2))
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
                // We don't have a separate SSE2 instruction and will
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
            else if (baseType == TYP_USHORT || baseType == TYP_SHORT)
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
            else if ((baseType == TYP_INT) && (compiler->getSIMDSupportLevel() >= SIMD_SSE4_Supported))
            {
                result = INS_pmulld;
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
            else if (compiler->getSIMDSupportLevel() >= SIMD_SSE4_Supported)
            {
                if (baseType == TYP_BYTE)
                {
                    result = INS_pminsb;
                }
                else if (baseType == TYP_USHORT)
                {
                    result = INS_pminuw;
                }
                else if (baseType == TYP_INT)
                {
                    result = INS_pminsd;
                }
                else if (baseType == TYP_UINT)
                {
                    result = INS_pminud;
                }
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
            else if (compiler->getSIMDSupportLevel() >= SIMD_SSE4_Supported)
            {
                if (baseType == TYP_BYTE)
                {
                    result = INS_pmaxsb;
                }
                else if (baseType == TYP_USHORT)
                {
                    result = INS_pmaxuw;
                }
                else if (baseType == TYP_INT)
                {
                    result = INS_pmaxsd;
                }
                else if (baseType == TYP_UINT)
                {
                    result = INS_pmaxud;
                }
            }
            else
            {
                unreached();
            }
            break;

        case SIMDIntrinsicAbs:
            if (compiler->getSIMDSupportLevel() >= SIMD_SSE4_Supported)
            {
                if (baseType == TYP_INT)
                {
                    result = INS_pabsd;
                }
                else if (baseType == TYP_SHORT)
                {
                    result = INS_pabsw;
                }
                else if (baseType == TYP_BYTE)
                {
                    result = INS_pabsb;
                }
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
            else if ((baseType == TYP_LONG) && (compiler->getSIMDSupportLevel() >= SIMD_SSE4_Supported))
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

        case SIMDIntrinsicNarrow:
            // Note that for the integer types the caller must zero the upper bits of
            // each source element, since the instructions saturate.
            switch (baseType)
            {
                case TYP_INT:
                case TYP_UINT:
                    if (compiler->getSIMDSupportLevel() >= SIMD_SSE4_Supported)
                    {
                        result = INS_packusdw;
                    }
                    else
                    {
                        result = INS_packssdw;
                    }
                    break;
                case TYP_SHORT:
                case TYP_USHORT:
                    result = INS_packuswb;
                    break;
                default:
                    assert(!"Invalid baseType for SIMDIntrinsicNarrow");
                    result = INS_invalid;
                    break;
            }
            break;

        case SIMDIntrinsicWidenLo:
            // Some of these have multiple instruction implementations, with one instruction to widen the lo half,
            // and another to widen the hi half.
            switch (baseType)
            {
                case TYP_FLOAT:
                    result = INS_cvtps2pd;
                    break;
                case TYP_INT:
                case TYP_UINT:
                    result = INS_punpckldq;
                    break;
                case TYP_SHORT:
                case TYP_USHORT:
                    result = INS_punpcklwd;
                    break;
                case TYP_BYTE:
                case TYP_UBYTE:
                    result = INS_punpcklbw;
                    break;
                default:
                    assert(!"Invalid baseType for SIMDIntrinsicWidenLo");
                    result = INS_invalid;
                    break;
            }
            break;

        case SIMDIntrinsicWidenHi:
            switch (baseType)
            {
                case TYP_FLOAT:
                    // For this case, we actually use the same instruction.
                    result = INS_cvtps2pd;
                    break;
                case TYP_INT:
                case TYP_UINT:
                    result = INS_punpckhdq;
                    break;
                case TYP_SHORT:
                case TYP_USHORT:
                    result = INS_punpckhwd;
                    break;
                case TYP_BYTE:
                case TYP_UBYTE:
                    result = INS_punpckhbw;
                    break;
                default:
                    assert(!"Invalid baseType for SIMDIntrinsicWidenHi");
                    result = INS_invalid;
                    break;
            }
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
            if (srcReg != targetReg)
            {
                instruction ins = ins_Store(baseType);
                if (getEmitter()->IsDstSrcSrcAVXInstruction(ins))
                {
                    // In general, when we use a three-operands move instruction, we want to merge the src with
                    // itself. This is an exception in that we actually want the "merge" behavior, so we must
                    // specify it with all 3 operands.
                    inst_RV_RV_RV(ins, targetReg, targetReg, srcReg, emitTypeSize(baseType));
                }
                else
                {
                    inst_RV_RV(ins, targetReg, srcReg, baseType, emitTypeSize(baseType));
                }
            }
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
                    getEmitter()->emitIns_R_I(ins, EA_16BYTE, srcReg, 12);
                    ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, TYP_SIMD16);
                    getEmitter()->emitIns_R_I(ins, EA_16BYTE, srcReg, 12);
                }
                else
                {
                    genSIMDZero(targetType, TYP_FLOAT, targetReg);
                    inst_RV_RV(ins_Store(baseType), targetReg, srcReg);
                }
            }
            break;

        case SMT_ZeroInitUpper_SrcHasUpperZeros:
            if (srcReg != targetReg)
            {
                instruction ins = ins_Copy(baseType);
                assert(!getEmitter()->IsDstSrcSrcAVXInstruction(ins));
                inst_RV_RV(ins, targetReg, srcReg, baseType, emitTypeSize(baseType));
            }
            break;

        default:
            unreached();
    }
}

void CodeGen::genSIMDZero(var_types targetType, var_types baseType, regNumber targetReg)
{
    // We just use `INS_xorps` instead of `getOpForSIMDIntrinsic(SIMDIntrinsicBitwiseXor, baseType)`
    // since `genSIMDZero` is used for both `System.Numerics.Vectors` and HardwareIntrinsics. Modern
    // CPUs handle this specially in the renamer and it never hits the execution pipeline, additionally
    // `INS_xorps` is always available (when using either the legacy or VEX encoding).
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
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicInit);

    GenTree*  op1       = simdNode->gtGetOp1();
    var_types baseType  = simdNode->gtSIMDBaseType;
    regNumber targetReg = simdNode->gtRegNum;
    assert(targetReg != REG_NA);
    var_types targetType = simdNode->TypeGet();
    SIMDLevel level      = compiler->getSIMDSupportLevel();
    unsigned  size       = simdNode->gtSIMDSize;

    // Should never see small int base type vectors except for zero initialization.
    noway_assert(!varTypeIsSmallInt(baseType) || op1->IsIntegralConst(0));

    instruction ins = INS_invalid;

#if !defined(_TARGET_64BIT_)
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

            instruction ins;

            regNumber op1loReg = genConsumeReg(op1lo);
            ins                = ins_CopyIntToFloat(TYP_INT, TYP_FLOAT);
            inst_RV_RV(ins, targetReg, op1loReg, TYP_INT, emitTypeSize(TYP_INT));

            regNumber tmpReg = simdNode->GetSingleTempReg();

            regNumber op1hiReg = genConsumeReg(op1hi);
            ins                = ins_CopyIntToFloat(TYP_INT, TYP_FLOAT);
            inst_RV_RV(ins, tmpReg, op1hiReg, TYP_INT, emitTypeSize(TYP_INT));

            ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, TYP_SIMD16);
            getEmitter()->emitIns_R_I(ins, EA_16BYTE, tmpReg, 4); // shift left by 4 bytes

            ins = getOpForSIMDIntrinsic(SIMDIntrinsicBitwiseOr, baseType);
            inst_RV_RV(ins, targetReg, tmpReg, targetType, emitActualTypeSize(targetType));

            if (compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported)
            {
                inst_RV_RV(INS_vpbroadcastq, targetReg, targetReg, TYP_SIMD32, emitTypeSize(TYP_SIMD32));
            }
            else
            {
                ins = getOpForSIMDIntrinsic(SIMDIntrinsicShuffleSSE2, baseType);
                getEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(targetType), targetReg, targetReg, 0);
            }
        }
    }
    else
#endif // !defined(_TARGET_64BIT_)
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
    }
    else if (level == SIMD_AVX2_Supported && ((size == 32) || (size == 16)))
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
        assert((shuffleControl >= 0) && (shuffleControl <= 255));
        getEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(targetType), targetReg, targetReg, (int8_t)shuffleControl);
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
            assert((baseTypeSize >= 0) && (baseTypeSize <= 255));
            getEmitter()->emitIns_R_I(insLeftShift, EA_16BYTE, vectorReg, (int8_t)baseTypeSize);
        }
        genSIMDScalarMove(targetType, baseType, vectorReg, operandReg, SMT_PreserveUpper);

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
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicSqrt || simdNode->gtSIMDIntrinsicID == SIMDIntrinsicCast ||
           simdNode->gtSIMDIntrinsicID == SIMDIntrinsicAbs);

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
    SIMDIntrinsicID intrinsicID = simdNode->gtSIMDIntrinsicID;
    assert((intrinsicID == SIMDIntrinsicConvertToSingle) || (intrinsicID == SIMDIntrinsicConvertToInt32));

    GenTree*  op1       = simdNode->gtGetOp1();
    var_types baseType  = simdNode->gtSIMDBaseType;
    regNumber targetReg = simdNode->gtRegNum;
    assert(targetReg != REG_NA);
    var_types targetType = simdNode->TypeGet();

    regNumber   op1Reg = genConsumeReg(op1);
    instruction ins    = getOpForSIMDIntrinsic(simdNode->gtSIMDIntrinsicID, baseType);
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
        inst_RV_RV(INS_movdqu, tmpReg2, op1Reg, baseType, emitActualTypeSize(targetType));
        if (targetReg != op1Reg)
        {
            inst_RV_RV(INS_movdqu, targetReg, op1Reg, baseType, emitActualTypeSize(targetType));
        }

        // prepare upper 16 bits
        getEmitter()->emitIns_R_I(INS_psrld, emitActualTypeSize(targetType), targetReg, 16);

        // prepare lower 16 bits
        getEmitter()->emitIns_R_I(INS_pslld, emitActualTypeSize(targetType), tmpReg2, 16);
        getEmitter()->emitIns_R_I(INS_psrld, emitActualTypeSize(targetType), tmpReg2, 16);

// prepare mask
#ifdef _TARGET_AMD64_
        getEmitter()->emitIns_R_I(INS_mov, EA_8BYTE, tmpIntReg, (ssize_t)0X5300000053000000);
        inst_RV_RV(INS_mov_i2xmm, tmpReg, tmpIntReg, TYP_ULONG);
#else
        if (compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported)
        {
            getEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, tmpIntReg, (ssize_t)0X53000000);
            inst_RV_RV(INS_mov_i2xmm, tmpReg, tmpIntReg, TYP_UINT);
        }
        else
        {
            getEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, tmpIntReg, (ssize_t)0X00005300);
            inst_RV_RV(INS_pxor, tmpReg, tmpReg, targetType, emitActualTypeSize(targetType));
            getEmitter()->emitIns_R_R_I(INS_pinsrw, emitTypeSize(TYP_INT), tmpReg, tmpIntReg, 1);
            getEmitter()->emitIns_R_R_I(INS_pinsrw, emitTypeSize(TYP_INT), tmpReg, tmpIntReg, 3);
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
        // Note that for mov_xmm2i, the int register is always in the reg2 position
        inst_RV_RV(INS_mov_xmm2i, tmpReg, tmpIntReg, TYP_LONG);
        inst_RV_RV(ins, targetReg, tmpIntReg, baseType, emitActualTypeSize(baseType));
    }
    else
    {
        inst_RV_RV(ins, tmpIntReg, tmpReg, baseType, emitActualTypeSize(baseType));
        inst_RV_RV(INS_mov_i2xmm, targetReg, tmpIntReg, TYP_LONG);
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
    SIMDIntrinsicID intrinsicID = simdNode->gtSIMDIntrinsicID;
    assert((intrinsicID == SIMDIntrinsicConvertToDouble) || (intrinsicID == SIMDIntrinsicConvertToInt64));

    GenTree*  op1       = simdNode->gtGetOp1();
    var_types baseType  = simdNode->gtSIMDBaseType;
    regNumber targetReg = simdNode->gtRegNum;
    assert(targetReg != REG_NA);
    var_types simdType  = simdNode->TypeGet();
    regNumber op1Reg    = genConsumeReg(op1);
    regNumber tmpIntReg = simdNode->GetSingleTempReg(RBM_ALLINT);
    regNumber tmpReg;
    regNumber tmpReg2;
    regNumber tmpReg3;
    SIMDLevel level = compiler->getSIMDSupportLevel();

#ifdef _TARGET_X86_
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
        inst_RV_RV(INS_movdqu, tmpReg2, op1Reg, baseType, emitActualTypeSize(simdType));
        if (targetReg != op1Reg)
        {
            inst_RV_RV(INS_movdqu, targetReg, op1Reg, baseType, emitActualTypeSize(simdType));
        }

        // prepare upper 32 bits
        getEmitter()->emitIns_R_I(INS_psrlq, emitActualTypeSize(simdType), targetReg, 32);

        // prepare lower 32 bits
        getEmitter()->emitIns_R_I(INS_psllq, emitActualTypeSize(simdType), tmpReg2, 32);
        getEmitter()->emitIns_R_I(INS_psrlq, emitActualTypeSize(simdType), tmpReg2, 32);

// prepare mask for converting upper 32 bits
#ifdef _TARGET_AMD64_
        getEmitter()->emitIns_R_I(INS_mov, EA_8BYTE, tmpIntReg, (ssize_t)0X4530000000000000);
        inst_RV_RV(INS_mov_i2xmm, tmpReg, tmpIntReg, TYP_ULONG);
#else
        getEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, tmpIntReg, (ssize_t)0X45300000);
        inst_RV_RV(INS_mov_i2xmm, tmpReg, tmpIntReg, TYP_UINT);
        getEmitter()->emitIns_R_I(INS_pslldq, EA_16BYTE, tmpReg, 4);
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
#ifdef _TARGET_AMD64_
        getEmitter()->emitIns_R_I(INS_mov, EA_8BYTE, tmpIntReg, (ssize_t)0X4330000000000000);
        inst_RV_RV(INS_mov_i2xmm, tmpReg, tmpIntReg, TYP_ULONG);
#else
        getEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, tmpIntReg, (ssize_t)0X43300000);
        inst_RV_RV(INS_mov_i2xmm, tmpReg, tmpIntReg, TYP_UINT);
        getEmitter()->emitIns_R_I(INS_pslldq, EA_16BYTE, tmpReg, 4);
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
#ifdef _TARGET_AMD64_
        instruction rightShiftIns = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, TYP_SIMD16);
        instruction leftShiftIns  = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, TYP_SIMD16);

        if (level == SIMD_AVX2_Supported)
        {
            // Extract the high 16-bits
            getEmitter()->emitIns_R_R_I(INS_vextracti128, EA_32BYTE, tmpReg, op1Reg, 0x01);

            // Put v[3] (the high-order element) in tmpReg2 and convert it.
            inst_RV_RV(ins_Copy(simdType), tmpReg2, tmpReg, simdType, emitActualTypeSize(simdType));
            getEmitter()->emitIns_R_I(rightShiftIns, emitActualTypeSize(simdType), tmpReg2, 8);
            genSIMDLo64BitConvert(intrinsicID, simdType, baseType, tmpReg2, tmpIntReg, tmpReg2);

            // Shift the resulting 64-bits left.
            getEmitter()->emitIns_R_I(leftShiftIns, emitActualTypeSize(simdType), tmpReg2, 8);

            // Convert v[2], in the lo bits of tmpReg.
            // For the convert to double, the convert preserves the upper bits in tmpReg2.
            // For the integer convert, we have to put it in tmpReg and or it in, since movd clears the upper bits.
            genSIMDLo64BitConvert(intrinsicID, simdType, baseType, tmpReg, tmpIntReg, tmpReg2);
        }

        // Put v[1] in tmpReg.
        inst_RV_RV(ins_Copy(simdType), tmpReg, op1Reg, simdType, emitActualTypeSize(simdType));
        getEmitter()->emitIns_R_I(rightShiftIns, emitActualTypeSize(simdType), tmpReg, 8);

        // At this point we have v[1] in the low-order 64-bits of tmpReg. Convert it.
        genSIMDLo64BitConvert(intrinsicID, simdType, baseType, tmpReg, tmpIntReg, tmpReg);

        // Shift the resulting 64-bits left.
        getEmitter()->emitIns_R_I(leftShiftIns, emitActualTypeSize(simdType), tmpReg, 8);

        // Convert the lo 64-bits into targetReg
        genSIMDLo64BitConvert(intrinsicID, simdType, baseType, op1Reg, tmpIntReg, tmpReg);

        // Merge or copy the results (only at this point are we done with op1Reg).
        if (tmpReg != targetReg)
        {
            inst_RV_RV(INS_movaps, targetReg, tmpReg, simdType, emitActualTypeSize(simdType));
        }

        if (level == SIMD_AVX2_Supported)
        {
            getEmitter()->emitIns_R_R_I(INS_vinsertf128, EA_32BYTE, targetReg, tmpReg2, 0x01);
        }
#else
        // get the sign bit and put it in tmpReg3
        inst_RV_RV(INS_movdqu, tmpReg3, op1Reg, baseType, emitActualTypeSize(simdType));
        getEmitter()->emitIns_R_I(INS_psrlq, emitActualTypeSize(simdType), tmpReg3, 63);
        getEmitter()->emitIns_R_I(INS_psllq, emitActualTypeSize(simdType), tmpReg3, 63);

        // get the absolute value of src and put it into tmpReg2 and targetReg
        inst_RV_RV(INS_movdqu, tmpReg2, op1Reg, baseType, emitActualTypeSize(simdType));
        getEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(simdType), tmpReg, op1Reg, (int8_t)SHUFFLE_WWYY);
        getEmitter()->emitIns_R_I(INS_psrad, emitActualTypeSize(simdType), tmpReg, 32);
        inst_RV_RV(INS_pxor, tmpReg2, tmpReg, baseType, emitActualTypeSize(simdType));
        inst_RV_RV(INS_psubq, tmpReg2, tmpReg, baseType, emitActualTypeSize(simdType));
        inst_RV_RV(INS_movdqu, targetReg, tmpReg2, baseType, emitActualTypeSize(simdType));

        // prepare upper 32 bits
        getEmitter()->emitIns_R_I(INS_psrlq, emitActualTypeSize(simdType), targetReg, 32);

        // prepare lower 32 bits
        getEmitter()->emitIns_R_I(INS_psllq, emitActualTypeSize(simdType), tmpReg2, 32);
        getEmitter()->emitIns_R_I(INS_psrlq, emitActualTypeSize(simdType), tmpReg2, 32);

        // prepare mask for converting upper 32 bits
        getEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, tmpIntReg, (ssize_t)0X45300000);
        inst_RV_RV(INS_mov_i2xmm, tmpReg, tmpIntReg, TYP_UINT);
        getEmitter()->emitIns_R_I(INS_pslldq, EA_16BYTE, tmpReg, 4);

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
        getEmitter()->emitIns_R_I(INS_mov, EA_4BYTE, tmpIntReg, (ssize_t)0X43300000);
        inst_RV_RV(INS_mov_i2xmm, tmpReg, tmpIntReg, TYP_UINT);
        getEmitter()->emitIns_R_I(INS_pslldq, EA_16BYTE, tmpReg, 4);

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
            getEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, tmpReg, op1Reg, 0x01);

            // Put v[3] (the high-order element) in tmpReg2 and convert it.
            inst_RV_RV(ins_Copy(simdType), tmpReg2, tmpReg, simdType, emitActualTypeSize(simdType));
            getEmitter()->emitIns_R_I(rightShiftIns, emitActualTypeSize(simdType), tmpReg2, 8);
            genSIMDLo64BitConvert(intrinsicID, simdType, baseType, tmpReg2, tmpIntReg, tmpReg2);

            // Shift the resulting 64-bits left.
            getEmitter()->emitIns_R_I(leftShiftIns, emitActualTypeSize(simdType), tmpReg2, 8);

            // Convert v[2], in the lo bits of tmpReg.
            // For the convert to double, the convert preserves the upper bits in tmpReg2.
            // For the integer convert, we have to put it in tmpReg and or it in, since movd clears the upper bits.
            genSIMDLo64BitConvert(intrinsicID, simdType, baseType, tmpReg, tmpIntReg, tmpReg);
            inst_RV_RV(INS_por, tmpReg2, tmpReg, simdType, emitActualTypeSize(simdType));
        }

        // Put v[1] in tmpReg.
        inst_RV_RV(ins_Copy(simdType), tmpReg, op1Reg, simdType, emitActualTypeSize(simdType));
        getEmitter()->emitIns_R_I(rightShiftIns, emitActualTypeSize(simdType), tmpReg, 8);

        // At this point we have v[1] in the low-order 64-bits of tmpReg. Convert it.
        genSIMDLo64BitConvert(intrinsicID, simdType, baseType, tmpReg, tmpIntReg, tmpReg);

        // Shift the resulting 64-bits left.
        getEmitter()->emitIns_R_I(leftShiftIns, emitActualTypeSize(simdType), tmpReg, 8);

        // Convert the lo 64-bits into targetReg
        genSIMDLo64BitConvert(intrinsicID, simdType, baseType, op1Reg, tmpIntReg, targetReg);

        // Merge or copy the results (only at this point are we done with op1Reg).
        assert(tmpReg != targetReg);
        inst_RV_RV(INS_por, targetReg, tmpReg, simdType, emitActualTypeSize(simdType));
        if (level == SIMD_AVX2_Supported)
        {
            getEmitter()->emitIns_R_R_I(INS_vinserti128, EA_32BYTE, targetReg, tmpReg2, 0x01);
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
        instruction extractIns = varTypeIsFloating(simdNode->gtSIMDBaseType) ? INS_vextractf128 : INS_vextracti128;
        getEmitter()->emitIns_R_R_I(extractIns, EA_32BYTE, tgtReg, srcReg, 0x01);
    }
    else
    {
        instruction shiftIns = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, TYP_SIMD16);
        if (tgtReg != srcReg)
        {
            inst_RV_RV(ins_Copy(simdType), tgtReg, srcReg, simdType, emitSize);
        }
        getEmitter()->emitIns_R_I(shiftIns, emitSize, tgtReg, 8);
    }
}

//--------------------------------------------------------------------------------
// genSIMDIntrinsicWiden: Generate code for SIMD Intrinsic Widen operations
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Notes:
//    The Widen intrinsics are broken into separate intrinsics for the two results.
//
void CodeGen::genSIMDIntrinsicWiden(GenTreeSIMD* simdNode)
{
    assert((simdNode->gtSIMDIntrinsicID == SIMDIntrinsicWidenLo) ||
           (simdNode->gtSIMDIntrinsicID == SIMDIntrinsicWidenHi));

    GenTree*  op1       = simdNode->gtGetOp1();
    var_types baseType  = simdNode->gtSIMDBaseType;
    regNumber targetReg = simdNode->gtRegNum;
    assert(targetReg != REG_NA);
    var_types simdType = simdNode->TypeGet();
    SIMDLevel level    = compiler->getSIMDSupportLevel();

    genConsumeOperands(simdNode);
    regNumber   op1Reg   = op1->gtRegNum;
    regNumber   srcReg   = op1Reg;
    emitAttr    emitSize = emitActualTypeSize(simdType);
    instruction widenIns = getOpForSIMDIntrinsic(simdNode->gtSIMDIntrinsicID, baseType);

    if (baseType == TYP_FLOAT)
    {
        if (simdNode->gtSIMDIntrinsicID == SIMDIntrinsicWidenHi)
        {
            genSIMDExtractUpperHalf(simdNode, srcReg, targetReg);
            srcReg = targetReg;
        }
        inst_RV_RV(widenIns, targetReg, srcReg, simdType);
    }
    else
    {
        // We will generate the following on AVX:
        // vpermq   targetReg, op1Reg, 0xd4|0xe8
        // vpxor    tmpReg, tmpReg
        // vpcmpgt[b|w|d] tmpReg, targetReg             (if basetype is signed)
        // vpunpck[l|h][bw|wd|dq] targetReg, tmpReg
        regNumber tmpReg = simdNode->GetSingleTempReg(RBM_ALLFLOAT);
        assert(tmpReg != op1Reg);

        if (level == SIMD_AVX2_Supported)
        {
            // permute op1Reg and put it into targetReg
            unsigned ival = 0xd4;
            if (simdNode->gtSIMDIntrinsicID == SIMDIntrinsicWidenHi)
            {
                ival = 0xe8;
            }
            assert((ival >= 0) && (ival <= 255));
            getEmitter()->emitIns_R_R_I(INS_vpermq, emitSize, targetReg, op1Reg, (int8_t)ival);
        }
        else if (targetReg != op1Reg)
        {
            inst_RV_RV(ins_Copy(simdType), targetReg, op1Reg, simdType, emitSize);
        }

        genSIMDZero(simdType, baseType, tmpReg);
        if (!varTypeIsUnsigned(baseType))
        {
            instruction compareIns = getOpForSIMDIntrinsic(SIMDIntrinsicGreaterThan, baseType);
            inst_RV_RV(compareIns, tmpReg, targetReg, simdType, emitSize);
        }
        inst_RV_RV(widenIns, targetReg, tmpReg, simdType);
    }
    genProduceReg(simdNode);
}

//--------------------------------------------------------------------------------
// genSIMDIntrinsicNarrow: Generate code for SIMD Intrinsic Narrow operations
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Notes:
//    This intrinsic takes two arguments. The first operand is narrowed to produce the
//    lower elements of the results, and the second operand produces the high elements.
//
void CodeGen::genSIMDIntrinsicNarrow(GenTreeSIMD* simdNode)
{
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicNarrow);

    GenTree*  op1       = simdNode->gtGetOp1();
    GenTree*  op2       = simdNode->gtGetOp2();
    var_types baseType  = simdNode->gtSIMDBaseType;
    regNumber targetReg = simdNode->gtRegNum;
    assert(targetReg != REG_NA);
    var_types simdType = simdNode->TypeGet();
    emitAttr  emitSize = emitTypeSize(simdType);
    SIMDLevel level    = compiler->getSIMDSupportLevel();

    genConsumeOperands(simdNode);
    regNumber op1Reg = op1->gtRegNum;
    regNumber op2Reg = op2->gtRegNum;
    if (baseType == TYP_DOUBLE)
    {
        regNumber tmpReg = simdNode->GetSingleTempReg(RBM_ALLFLOAT);

        inst_RV_RV(INS_cvtpd2ps, targetReg, op1Reg, simdType);
        inst_RV_RV(INS_cvtpd2ps, tmpReg, op2Reg, simdType);
        // Now insert the high-order result (in tmpReg) into the upper half of targetReg.
        if (level == SIMD_AVX2_Supported)
        {
            getEmitter()->emitIns_R_R_I(INS_vinsertf128, EA_32BYTE, targetReg, tmpReg, 0x01);
        }
        else
        {
            inst_RV_RV_IV(INS_shufps, EA_16BYTE, targetReg, tmpReg, (int8_t)SHUFFLE_YXYX);
        }
    }
    else if (varTypeIsLong(baseType))
    {
        if (level == SIMD_AVX2_Supported)
        {
            // We have 8 long elements, 0-3 in op1Reg, 4-7 in op2Reg.
            // We will generate the following:
            //   vextracti128 tmpReg, op1Reg, 1       (extract elements 2 and 3 into tmpReg)
            //   vextracti128 tmpReg2, op2Reg, 1      (extract elements 6 and 7 into tmpReg2)
            //   vinserti128  tmpReg, tmpReg2, 1       (insert elements 6 and 7 into the high half of tmpReg)
            //   mov          tmpReg2, op1Reg
            //   vinserti128  tmpReg2, op2Reg, 1      (insert elements 4 and 5 into the high half of tmpReg2)
            //   pshufd       tmpReg, tmpReg, XXZX    ( -  - 7L 6L  -  - 3L 2L) in tmpReg
            //   pshufd       tgtReg, tmpReg2, XXZX   ( -  - 5L 4L  -  - 1L 0L) in tgtReg
            //   punpcklqdq   tgtReg, tmpReg
            regNumber tmpReg  = simdNode->ExtractTempReg(RBM_ALLFLOAT);
            regNumber tmpReg2 = simdNode->GetSingleTempReg(RBM_ALLFLOAT);
            getEmitter()->emitIns_R_R_I(INS_vextracti128, EA_32BYTE, tmpReg, op1Reg, 0x01);
            getEmitter()->emitIns_R_R_I(INS_vextracti128, EA_32BYTE, tmpReg2, op2Reg, 0x01);
            getEmitter()->emitIns_R_R_I(INS_vinserti128, EA_32BYTE, tmpReg, tmpReg2, 0x01);
            inst_RV_RV(ins_Copy(simdType), tmpReg2, op1Reg, simdType, emitSize);
            getEmitter()->emitIns_R_R_I(INS_vinserti128, EA_32BYTE, tmpReg2, op2Reg, 0x01);
            getEmitter()->emitIns_R_R_I(INS_pshufd, emitSize, tmpReg, tmpReg, (int8_t)SHUFFLE_XXZX);
            getEmitter()->emitIns_R_R_I(INS_pshufd, emitSize, targetReg, tmpReg2, (int8_t)SHUFFLE_XXZX);
            inst_RV_RV_RV(INS_punpcklqdq, targetReg, targetReg, tmpReg, emitSize);
        }
        else
        {
            // We will generate the following:
            //   pshufd  targetReg, op1Reg, ZXXX (extract the low 32-bits into the upper two 32-bit elements)
            //   psrldq  targetReg, 8            (shift them right to get zeros in the high elements)
            //   pshufd  tmpReg, op2Reg, XXZX    (same as above, but extract into the lower two 32-bit elements)
            //   pslldq  tmpReg, 8               (now shift these left to get zeros in the low elements)
            //   por     targetReg, tmpReg
            regNumber   tmpReg        = simdNode->GetSingleTempReg(RBM_ALLFLOAT);
            instruction shiftLeftIns  = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, TYP_SIMD16);
            instruction shiftRightIns = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, TYP_SIMD16);
            emitAttr    emitSize      = emitTypeSize(simdType);

            getEmitter()->emitIns_R_R_I(INS_pshufd, emitSize, targetReg, op1Reg, (int8_t)SHUFFLE_ZXXX);
            getEmitter()->emitIns_R_I(shiftRightIns, emitSize, targetReg, 8);
            getEmitter()->emitIns_R_R_I(INS_pshufd, emitSize, tmpReg, op2Reg, (int8_t)SHUFFLE_XXZX);
            getEmitter()->emitIns_R_I(shiftLeftIns, emitSize, tmpReg, 8);
            inst_RV_RV(INS_por, targetReg, tmpReg, simdType);
        }
    }
    else
    {
        // We will generate the following:
        //   mov     targetReg, op1Reg
        //   mov     tmpReg, op2Reg
        //   psll?   targetReg, shiftCount
        //   pslr?   targetReg, shiftCount
        //   psll?   tmpReg, shiftCount
        //   pslr?   tmpReg, shiftCount
        //   <pack>  targetReg, tmpReg
        // Where shiftCount is the size of the target baseType (i.e. half the size of the source baseType),
        // and <pack> is the appropriate instruction to pack the result (note that we have to truncate to
        // get CLR type semantics; otherwise it will saturate).
        //
        int         shiftCount    = genTypeSize(baseType) * (BITS_IN_BYTE / 2);
        instruction ins           = getOpForSIMDIntrinsic(simdNode->gtSIMDIntrinsicID, baseType);
        instruction shiftLeftIns  = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, baseType);
        instruction shiftRightIns = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, baseType);

        assert((shiftCount >= 0) && (shiftCount <= 127));

        if (level == SIMD_AVX2_Supported)
        {
            regNumber tmpReg  = simdNode->ExtractTempReg(RBM_ALLFLOAT);
            regNumber tmpReg2 = simdNode->GetSingleTempReg(RBM_ALLFLOAT);

            // The AVX instructions generally operate on "lanes", so we have to permute the
            // inputs so that the destination register has the low 128-bit halves of the two
            // inputs, and 'tmpReg' has the high 128-bit halves of the two inputs.
            getEmitter()->emitIns_R_R_R_I(INS_vperm2i128, emitSize, tmpReg2, op1Reg, op2Reg, 0x20);
            getEmitter()->emitIns_R_R_R_I(INS_vperm2i128, emitSize, tmpReg, op1Reg, op2Reg, 0x31);
            getEmitter()->emitIns_R_I(shiftLeftIns, emitSize, tmpReg2, shiftCount);
            getEmitter()->emitIns_R_I(shiftRightIns, emitSize, tmpReg2, shiftCount);
            getEmitter()->emitIns_R_I(shiftLeftIns, emitSize, tmpReg, shiftCount);
            getEmitter()->emitIns_R_I(shiftRightIns, emitSize, tmpReg, shiftCount);
            inst_RV_RV_RV(ins, targetReg, tmpReg2, tmpReg, emitActualTypeSize(simdType));
        }
        else
        {
            regNumber tmpReg = simdNode->GetSingleTempReg(RBM_ALLFLOAT);

            inst_RV_RV(ins_Copy(simdType), targetReg, op1Reg, simdType, emitSize);
            inst_RV_RV(ins_Copy(simdType), tmpReg, op2Reg, simdType, emitSize);

            instruction tmpShiftRight = shiftRightIns;
            if ((baseType == TYP_INT || baseType == TYP_UINT) && level == SIMD_SSE2_Supported)
            {
                tmpShiftRight = INS_psrad;
            }

            getEmitter()->emitIns_R_I(shiftLeftIns, emitSize, targetReg, shiftCount);
            getEmitter()->emitIns_R_I(tmpShiftRight, emitSize, targetReg, shiftCount);
            getEmitter()->emitIns_R_I(shiftLeftIns, emitSize, tmpReg, shiftCount);
            getEmitter()->emitIns_R_I(tmpShiftRight, emitSize, tmpReg, shiftCount);
            inst_RV_RV(ins, targetReg, tmpReg, simdType);
        }
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
    var_types targetType = simdNode->TypeGet();
    SIMDLevel level      = compiler->getSIMDSupportLevel();

    genConsumeOperands(simdNode);
    regNumber op1Reg   = op1->gtRegNum;
    regNumber op2Reg   = op2->gtRegNum;
    regNumber otherReg = op2Reg;

    // Vector<Int>.Mul:
    // SSE2 doesn't have an instruction to perform this operation directly
    // whereas SSE4.1 does (pmulld).  This is special cased and computed
    // as follows.
    if (simdNode->gtSIMDIntrinsicID == SIMDIntrinsicMul && baseType == TYP_INT && level == SIMD_SSE2_Supported)
    {
        // We need a temporary register that is NOT the same as the target,
        // and we MAY need another.
        regNumber tmpReg  = simdNode->ExtractTempReg();
        regNumber tmpReg2 = simdNode->GetSingleTempReg();

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
        getEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(targetType), tmpReg, tmpReg, (int8_t)SHUFFLE_XXZX);

        // targetReg[63:0] = op1[0] * op2[0]
        // targetReg[127:64] = op1[2] * op2[2]
        inst_RV_RV(INS_movaps, targetReg, op1Reg, targetType, emitActualTypeSize(targetType));
        inst_RV_RV(INS_pmuludq, targetReg, op2Reg, targetType, emitActualTypeSize(targetType));

        // Extract first and third double word results from targetReg
        // targetReg = shuffle(0,0,2,0) of targetReg
        getEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(targetType), targetReg, targetReg,
                                    (int8_t)SHUFFLE_XXZX);

        // pack the results into a single vector
        inst_RV_RV(INS_punpckldq, targetReg, tmpReg, targetType, emitActualTypeSize(targetType));
    }
    else
    {
        instruction ins = getOpForSIMDIntrinsic(simdNode->gtSIMDIntrinsicID, baseType);

        // Currently AVX doesn't support integer.
        // if the ins is INS_cvtsi2ss or INS_cvtsi2sd, we won't use AVX.
        if (op1Reg != targetReg && compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported &&
            !(ins == INS_cvtsi2ss || ins == INS_cvtsi2sd) && getEmitter()->IsThreeOperandAVXInstruction(ins))
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
        assert((shiftCount > 0) && (shiftCount <= 16));
        instruction ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftLeftInternal, TYP_SIMD16);
        getEmitter()->emitIns_R_I(ins, EA_16BYTE, targetReg, shiftCount);
        ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, TYP_SIMD16);
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
    GenTree*  op1        = simdNode->gtGetOp1();
    GenTree*  op2        = simdNode->gtGetOp2();
    var_types baseType   = simdNode->gtSIMDBaseType;
    regNumber targetReg  = simdNode->gtRegNum;
    var_types targetType = simdNode->TypeGet();
    SIMDLevel level      = compiler->getSIMDSupportLevel();

    genConsumeOperands(simdNode);
    regNumber op1Reg   = op1->gtRegNum;
    regNumber op2Reg   = op2->gtRegNum;
    regNumber otherReg = op2Reg;

    switch (simdNode->gtSIMDIntrinsicID)
    {
        case SIMDIntrinsicEqual:
        case SIMDIntrinsicGreaterThan:
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
                assert((ival >= 0) && (ival <= 255));
                getEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(targetType), targetReg, otherReg, (int8_t)ival);
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
            assert(targetReg != REG_NA);

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

            assert((ival >= 0) && (ival <= 255));
            getEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(targetType), targetReg, op2Reg, (int8_t)ival);
        }
        break;

        // (In)Equality that produces bool result instead of a bit vector
        case SIMDIntrinsicOpEquality:
        case SIMDIntrinsicOpInEquality:
        {
            // We're only setting condition flags, if a 0/1 value is desired then Lowering should have inserted a SETCC.
            assert(targetReg == REG_NA);

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

            // On SSE4/AVX, we can generate optimal code for (in)equality against zero using ptest.
            if (op2->isContained())
            {
                assert((compiler->getSIMDSupportLevel() >= SIMD_SSE4_Supported) && op2->IsIntegralConstVector(0));
                inst_RV_RV(INS_ptest, op1->gtRegNum, op1->gtRegNum, simdType, emitActualTypeSize(simdType));
            }
            else
            {
                // We need one additional SIMD register to store the result of the SIMD compare.
                regNumber tmpReg1 = simdNode->GetSingleTempReg(RBM_ALLFLOAT);

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
                    assert((ival >= 0) && (ival <= 255));
                    getEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(simdType), tmpReg1, otherReg, (int8_t)ival);
                }
                else
                {
                    inst_RV_RV(ins, tmpReg1, otherReg, simdType, emitActualTypeSize(simdType));
                }

                regNumber intReg = simdNode->GetSingleTempReg(RBM_ALLINT);
                inst_RV_RV(INS_pmovmskb, intReg, tmpReg1, simdType, emitActualTypeSize(simdType));
                // There's no pmovmskw/pmovmskd/pmovmskq but they're not needed anyway. Vector compare
                // instructions produce "all ones"/"all zeroes" components and pmovmskb extracts a
                // subset of each component's ones/zeroes. In the end we need to know if the result is
                // "all ones" where the number of ones is given by the vector byte size, not by the
                // vector component count. So, for AVX registers we need to compare to 0xFFFFFFFF and
                // for SSE registers we need to compare to 0x0000FFFF.
                // The SIMD12 case is handled specially, because we can't rely on the upper bytes being
                // zero, so we must compare only the lower 3 floats (hence the byte mask of 0xFFF).
                // Note that -1 is used instead of 0xFFFFFFFF, on x64 emit doesn't correctly recognize
                // that 0xFFFFFFFF can be encoded in a single byte and emits the longer 3DFFFFFFFF
                // encoding instead of 83F8FF.
                ssize_t mask;
                if ((simdNode->gtFlags & GTF_SIMD12_OP) != 0)
                {
                    mask = 0x00000FFF;
                    getEmitter()->emitIns_R_I(INS_and, EA_4BYTE, intReg, mask);
                }
                else if (emitActualTypeSize(simdType) == 32)
                {
                    mask = -1;
                }
                else
                {
                    mask = 0x0000FFFF;
                }
                getEmitter()->emitIns_R_I(INS_cmp, EA_4BYTE, intReg, mask);
            }
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

    var_types targetType = simdNode->TypeGet();
    assert(targetType == baseType);

    genConsumeOperands(simdNode);
    regNumber op1Reg  = op1->gtRegNum;
    regNumber op2Reg  = op2->gtRegNum;
    regNumber tmpReg1 = REG_NA;
    regNumber tmpReg2 = REG_NA;

    SIMDLevel level = compiler->getSIMDSupportLevel();

    // Dot product intrinsic is supported only on float/double vectors
    // and 32-byte int vectors on AVX.
    //
    // Float/Double Vectors:
    // For SSE, or AVX with 32-byte vectors, we need one additional Xmm register
    // different from targetReg as scratch. Note that if this is a TYP_SIMD16 or
    // smaller on AVX, then we don't need a tmpReg.
    //
    // 32-byte integer vector on AVX: we need two additional Xmm registers
    // different from targetReg as scratch.
    //
    // 16-byte integer vector on SSE4: we need one additional Xmm register
    // different from targetReg as scratch.
    if (varTypeIsFloating(baseType))
    {
        if ((compiler->getSIMDSupportLevel() == SIMD_SSE2_Supported) || (simdEvalType == TYP_SIMD32))
        {
            tmpReg1 = simdNode->GetSingleTempReg();
            assert(tmpReg1 != targetReg);
        }
        else
        {
            assert(simdNode->AvailableTempRegCount() == 0);
        }
    }
    else
    {
        assert(baseType == TYP_INT);
        assert(level >= SIMD_SSE4_Supported);

        if (level == SIMD_SSE4_Supported)
        {
            tmpReg1 = simdNode->GetSingleTempReg();
        }
        else
        {
            tmpReg1 = simdNode->ExtractTempReg();
            tmpReg2 = simdNode->GetSingleTempReg();
        }
    }

    if (level == SIMD_SSE2_Supported)
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
        // Here v0 = targetReg, v1 = op1Reg, v2 = op2Reg and tmp = tmpReg1
        if ((simdNode->gtFlags & GTF_SIMD12_OP) != 0)
        {
            assert(baseType == TYP_FLOAT);
            // v0 = v1 * v2
            // tmp = v0                                       // v0  = (3, 2, 1, 0) - each element is given by its
            //                                                // position
            // tmp = shuffle(tmp, tmp, SHUFFLE_ZXXY)          // tmp = (2, 0, 0, 1) - don't really care what's in upper
            //                                                // bits
            // v0 = v0 + tmp                                  // v0  = (3+2, 0+2, 1+0, 0+1)
            // tmp = shuffle(tmp, tmp, SHUFFLE_XXWW)          // tmp = (  1,   1,   2,   2)
            // v0 = v0 + tmp                                  // v0  = (1+2+3,  0+1+2, 0+1+2, 0+1+2)
            //
            inst_RV_RV(INS_mulps, targetReg, op2Reg);
            inst_RV_RV(INS_movaps, tmpReg1, targetReg);
            inst_RV_RV_IV(INS_shufps, EA_16BYTE, tmpReg1, tmpReg1, (int8_t)SHUFFLE_ZXXY);
            inst_RV_RV(INS_addps, targetReg, tmpReg1);
            inst_RV_RV_IV(INS_shufps, EA_16BYTE, tmpReg1, tmpReg1, (int8_t)SHUFFLE_XXWW);
            inst_RV_RV(INS_addps, targetReg, tmpReg1);
        }
        else if (baseType == TYP_FLOAT)
        {
            // v0 = v1 * v2
            // tmp = v0                                       // v0  = (3, 2, 1, 0) - each element is given by its
            //                                                // position
            // tmp = shuffle(tmp, tmp, SHUFFLE_ZWXY)          // tmp = (2, 3, 0, 1)
            // v0 = v0 + tmp                                  // v0  = (3+2, 2+3, 1+0, 0+1)
            // tmp = v0
            // tmp = shuffle(tmp, tmp, SHUFFLE_XYZW)          // tmp = (0+1, 1+0, 2+3, 3+2)
            // v0 = v0 + tmp                                  // v0  = (0+1+2+3, 0+1+2+3, 0+1+2+3, 0+1+2+3)
            //                                                // Essentially horizontal addition of all elements.
            //                                                // We could achieve the same using SSEv3 instruction
            //                                                // HADDPS.
            //
            inst_RV_RV(INS_mulps, targetReg, op2Reg);
            inst_RV_RV(INS_movaps, tmpReg1, targetReg);
            inst_RV_RV_IV(INS_shufps, EA_16BYTE, tmpReg1, tmpReg1, (int8_t)SHUFFLE_ZWXY);
            inst_RV_RV(INS_addps, targetReg, tmpReg1);
            inst_RV_RV(INS_movaps, tmpReg1, targetReg);
            inst_RV_RV_IV(INS_shufps, EA_16BYTE, tmpReg1, tmpReg1, (int8_t)SHUFFLE_XYZW);
            inst_RV_RV(INS_addps, targetReg, tmpReg1);
        }
        else
        {
            assert(baseType == TYP_DOUBLE);

            // v0 = v1 * v2
            // tmp = v0                                       // v0  = (1, 0) - each element is given by its position
            // tmp = shuffle(tmp, tmp, Shuffle(0,1))          // tmp = (0, 1)
            // v0 = v0 + tmp                                  // v0  = (1+0, 0+1)
            inst_RV_RV(INS_mulpd, targetReg, op2Reg);
            inst_RV_RV(INS_movaps, tmpReg1, targetReg);
            inst_RV_RV_IV(INS_shufpd, EA_16BYTE, tmpReg1, tmpReg1, 0x01);
            inst_RV_RV(INS_addpd, targetReg, tmpReg1);
        }
    }
    else
    {
        assert(level >= SIMD_SSE4_Supported);

        if (varTypeIsFloating(baseType))
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
                unsigned mask = ((simdNode->gtFlags & GTF_SIMD12_OP) != 0) ? 0x71 : 0xf1;
                assert((mask >= 0) && (mask <= 255));
                inst_RV_RV_IV(INS_dpps, emitSize, targetReg, op2Reg, (int8_t)mask);
                // dpps computes the dot product of the upper & lower halves of the 32-byte register.
                // Notice that if this is a TYP_SIMD16 or smaller on AVX, then we don't need a tmpReg.
                // If this is TYP_SIMD32, we need to combine the lower & upper results.
                if (simdEvalType == TYP_SIMD32)
                {
                    getEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, tmpReg1, targetReg, 0x01);
                    inst_RV_RV(INS_addps, targetReg, tmpReg1, targetType, emitTypeSize(targetType));
                }
            }
            else if (baseType == TYP_DOUBLE)
            {
                if (simdEvalType == TYP_SIMD32)
                {
                    // targetReg = targetReg * op2Reg
                    // targetReg = vhaddpd(targetReg, targetReg) ; horizontal sum of lower & upper halves
                    // tmpReg    = vextractf128(targetReg, 1)    ; Moves the upper sum into tempReg
                    // targetReg = targetReg + tmpReg1
                    inst_RV_RV(INS_mulpd, targetReg, op2Reg, simdEvalType, emitActualTypeSize(simdType));
                    inst_RV_RV(INS_haddpd, targetReg, targetReg, simdEvalType, emitActualTypeSize(simdType));
                    getEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, tmpReg1, targetReg, 0x01);
                    inst_RV_RV(INS_addpd, targetReg, tmpReg1, targetType, emitTypeSize(targetType));
                }
                else
                {
                    // On AVX, we have no 16-byte vectors of double.  Note that, if we did, we could use
                    // dppd directly.
                    assert(level == SIMD_SSE4_Supported);
                    inst_RV_RV_IV(INS_dppd, emitSize, targetReg, op2Reg, 0x31);
                }
            }
        }
        else
        {
            // Dot product of 32-byte int vector on SSE4/AVX.
            assert(baseType == TYP_INT);
            assert(simdEvalType == TYP_SIMD16 || simdEvalType == TYP_SIMD32);

#ifdef DEBUG
            // SSE4: We need 1 scratch register.
            // AVX2: We need 2 scratch registers.
            if (simdEvalType == TYP_SIMD16)
            {
                assert(tmpReg1 != REG_NA);
            }
            else
            {
                assert(tmpReg1 != REG_NA);
                assert(tmpReg2 != REG_NA);
            }
#endif

            // tmpReg1 = op1 * op2
            if (level == SIMD_AVX2_Supported)
            {
                // On AVX take advantage 3 operand form of pmulld
                inst_RV_RV_RV(INS_pmulld, tmpReg1, op1Reg, op2Reg, emitTypeSize(simdEvalType));
            }
            else
            {
                inst_RV_RV(ins_Copy(simdEvalType), tmpReg1, op1Reg, simdEvalType);
                inst_RV_RV(INS_pmulld, tmpReg1, op2Reg, simdEvalType);
            }

            if (simdEvalType == TYP_SIMD32)
            {
                // tmpReg2[127..0] = Upper 128-bits of tmpReg1
                getEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, tmpReg2, tmpReg1, 0x01);

                // tmpReg1[127..0] = tmpReg1[127..0] + tmpReg2[127..0]
                // This will compute
                //    tmpReg1[0] = op1[0]*op2[0] + op1[4]*op2[4]
                //    tmpReg1[1] = op1[1]*op2[1] + op1[5]*op2[5]
                //    tmpReg1[2] = op1[2]*op2[2] + op1[6]*op2[6]
                //    tmpReg1[4] = op1[4]*op2[4] + op1[7]*op2[7]
                inst_RV_RV(INS_paddd, tmpReg1, tmpReg2, TYP_SIMD16, EA_16BYTE);
            }

            // This horizontal add will compute
            //
            // TYP_SIMD16:
            //   tmpReg1[0] = tmpReg1[2] = op1[0]*op2[0] + op1[1]*op2[1]
            //   tmpReg1[1] = tmpReg1[3] = op1[2]*op2[2] + op1[4]*op2[4]
            //
            // TYP_SIMD32:
            //   tmpReg1[0] = tmpReg1[2] = op1[0]*op2[0] + op1[4]*op2[4] + op1[1]*op2[1] + op1[5]*op2[5]
            //   tmpReg1[1] = tmpReg1[3] = op1[2]*op2[2] + op1[6]*op2[6] + op1[4]*op2[4] + op1[7]*op2[7]
            inst_RV_RV(INS_phaddd, tmpReg1, tmpReg1, TYP_SIMD16, EA_16BYTE);

            // DotProduct(op1, op2) = tmpReg1[0] = tmpReg1[0] + tmpReg1[1]
            inst_RV_RV(INS_phaddd, tmpReg1, tmpReg1, TYP_SIMD16, EA_16BYTE);

            // TargetReg = integer result from tmpReg1
            // (Note that for mov_xmm2i, the int register is always in the reg2 position)
            inst_RV_RV(INS_mov_xmm2i, tmpReg1, targetReg, TYP_INT);
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

    // Optimize the case of op1 is in memory and trying to access ith element.
    if (!op1->isUsedFromReg())
    {
        assert(op1->isContained());

        regNumber baseReg;
        regNumber indexReg;
        int       offset = 0;

        if (op1->OperIsLocal())
        {
            // There are three parts to the total offset here:
            // {offset of local} + {offset of SIMD Vector field (lclFld only)} + {offset of element within SIMD vector}.
            bool     isEBPbased;
            unsigned varNum = op1->gtLclVarCommon.gtLclNum;
            offset += compiler->lvaFrameAddress(varNum, &isEBPbased);
            if (op1->OperGet() == GT_LCL_FLD)
            {
                offset += op1->gtLclFld.gtLclOffs;
            }
            baseReg = (isEBPbased) ? REG_EBP : REG_ESP;
        }
        else
        {
            // Require GT_IND addr to be not contained.
            assert(op1->OperGet() == GT_IND);

            GenTree* addr = op1->AsIndir()->Addr();
            assert(!addr->isContained());
            baseReg = addr->gtRegNum;
        }

        if (op2->isContainedIntOrIImmed())
        {
            indexReg = REG_NA;
            offset += (int)op2->AsIntConCommon()->IconValue() * genTypeSize(baseType);
        }
        else
        {
            indexReg = op2->gtRegNum;
            assert(genIsValidIntReg(indexReg));
        }

        // Now, load the desired element.
        getEmitter()->emitIns_R_ARX(ins_Move_Extend(baseType, false), // Load
                                    emitTypeSize(baseType),           // Of the vector baseType
                                    targetReg,                        // To targetReg
                                    baseReg,                          // Base Reg
                                    indexReg,                         // Indexed
                                    genTypeSize(baseType),            // by the size of the baseType
                                    offset);
        genProduceReg(simdNode);
        return;
    }

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
    noway_assert(op2->IsCnsIntOrI());
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
    if (simdNode->AvailableTempRegCount() != 0)
    {
        tmpReg = simdNode->GetSingleTempReg();
    }
    else
    {
        assert((byteShiftCnt == 0) || varTypeIsFloating(baseType) ||
               (varTypeIsSmallInt(baseType) && (byteShiftCnt < 16)));
    }

    if (byteShiftCnt >= 16)
    {
        assert(compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported);
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
            instruction ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, TYP_SIMD16);
            assert((byteShiftCnt > 0) && (byteShiftCnt < 32));
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
                assert(compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported);
                index -= 8;
            }

            assert((index >= 0) && (index <= 8));
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

                assert((byteShiftCnt > 0) && (byteShiftCnt <= 32));
                ins = getOpForSIMDIntrinsic(SIMDIntrinsicShiftRightInternal, TYP_SIMD16);
                getEmitter()->emitIns_R_I(ins, emitActualTypeSize(simdType), tmpReg, byteShiftCnt);
            }
            else
            {
                tmpReg = srcReg;
            }

            assert(tmpReg != REG_NA);
            ins = ins_CopyFloatToInt(TYP_FLOAT, baseType);
            // (Note that for mov_xmm2i, the int register is always in the reg2 position.)
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

    if (compiler->getSIMDSupportLevel() == SIMD_SSE2_Supported)
    {
        // We need one additional int register as scratch
        regNumber tmpReg = simdNode->GetSingleTempReg();
        assert(genIsValidIntReg(tmpReg));

        // Move the value from xmm reg to an int reg
        instruction ins = ins_CopyFloatToInt(TYP_FLOAT, TYP_INT);
        // (Note that for mov_xmm2i, the int register is always in the reg2 position.
        inst_RV_RV(ins, op2Reg, tmpReg, baseType);

        assert((index >= 0) && (index <= 15));

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
        assert((insertpsImm >= 0) && (insertpsImm <= 255));
        inst_RV_RV_IV(INS_insertps, EA_16BYTE, targetReg, op2Reg, (int8_t)insertpsImm);
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
    noway_assert(compiler->getSIMDSupportLevel() == SIMD_SSE2_Supported);

    GenTree* op1 = simdNode->gtGetOp1();
    GenTree* op2 = simdNode->gtGetOp2();
    assert(op2->isContained());
    assert(op2->IsCnsIntOrI());
    ssize_t   shuffleControl = op2->AsIntConCommon()->IconValue();
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
    assert((shuffleControl >= 0) && (shuffleControl <= 255));
    getEmitter()->emitIns_R_R_I(ins, emitTypeSize(baseType), targetReg, targetReg, (int8_t)shuffleControl);
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
    regNumber tmpReg = treeNode->GetSingleTempReg();

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

    regNumber targetReg = treeNode->gtRegNum;
    GenTree*  op1       = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());
    regNumber operandReg = genConsumeReg(op1);

    // Need an addtional Xmm register to read upper 4 bytes, which is different from targetReg
    regNumber tmpReg = treeNode->GetSingleTempReg();
    assert(tmpReg != targetReg);

    // Load upper 4 bytes in tmpReg
    getEmitter()->emitIns_R_AR(ins_Load(TYP_FLOAT), EA_4BYTE, tmpReg, operandReg, 8);

    // Load lower 8 bytes in targetReg
    getEmitter()->emitIns_R_AR(ins_Load(TYP_DOUBLE), EA_8BYTE, targetReg, operandReg, 0);

    // combine upper 4 bytes and lower 8 bytes in targetReg
    getEmitter()->emitIns_R_R_I(INS_shufps, emitActualTypeSize(TYP_SIMD16), targetReg, tmpReg, (int8_t)SHUFFLE_YXYX);

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

    unsigned offs   = 0;
    unsigned varNum = treeNode->gtLclVarCommon.gtLclNum;
    assert(varNum < compiler->lvaCount);

    if (treeNode->OperGet() == GT_STORE_LCL_FLD)
    {
        offs = treeNode->gtLclFld.gtLclOffs;
    }

    GenTree* op1 = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());
    regNumber operandReg = genConsumeReg(op1);

    // Need an addtional Xmm register to extract upper 4 bytes from data.
    regNumber tmpReg = treeNode->GetSingleTempReg();

    // store lower 8 bytes
    getEmitter()->emitIns_S_R(ins_Store(TYP_DOUBLE), EA_8BYTE, operandReg, varNum, offs);

    // Extract upper 4-bytes from operandReg
    getEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(TYP_SIMD16), tmpReg, operandReg, 0x02);

    // Store upper 4 bytes
    getEmitter()->emitIns_S_R(ins_Store(TYP_FLOAT), EA_4BYTE, tmpReg, varNum, offs + 8);
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

    regNumber targetReg = treeNode->gtRegNum;
    unsigned  offs      = 0;
    unsigned  varNum    = treeNode->gtLclVarCommon.gtLclNum;
    assert(varNum < compiler->lvaCount);

    if (treeNode->OperGet() == GT_LCL_FLD)
    {
        offs = treeNode->gtLclFld.gtLclOffs;
    }

    // Need an additional Xmm register that is different from targetReg to read upper 4 bytes.
    regNumber tmpReg = treeNode->GetSingleTempReg();
    assert(tmpReg != targetReg);

    // Read upper 4 bytes to tmpReg
    getEmitter()->emitIns_R_S(ins_Move_Extend(TYP_FLOAT, false), EA_4BYTE, tmpReg, varNum, offs + 8);

    // Read lower 8 bytes to targetReg
    getEmitter()->emitIns_R_S(ins_Move_Extend(TYP_DOUBLE, false), EA_8BYTE, targetReg, varNum, offs);

    // combine upper 4 bytes and lower 8 bytes in targetReg
    getEmitter()->emitIns_R_R_I(INS_shufps, emitActualTypeSize(TYP_SIMD16), targetReg, tmpReg, (int8_t)SHUFFLE_YXYX);

    genProduceReg(treeNode);
}

#ifdef _TARGET_X86_

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
    getEmitter()->emitIns_AR_R(ins_Store(TYP_DOUBLE), EA_8BYTE, operandReg, REG_SPBASE, 0);

    // Extract upper 4-bytes from data
    getEmitter()->emitIns_R_R_I(INS_pshufd, emitActualTypeSize(TYP_SIMD16), tmpReg, operandReg, 0x02);

    // 4-byte write
    getEmitter()->emitIns_AR_R(ins_Store(TYP_FLOAT), EA_4BYTE, tmpReg, REG_SPBASE, 8);
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

    GenTree* op1 = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());
    regNumber operandReg = genConsumeReg(op1);

    // Need an addtional Xmm register to extract upper 4 bytes from data.
    regNumber tmpReg = treeNode->GetSingleTempReg();

    genStoreSIMD12ToStack(operandReg, tmpReg);
}

#endif // _TARGET_X86_

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
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicUpperSave);

    GenTree* op1 = simdNode->gtGetOp1();
    assert(op1->IsLocal() && op1->TypeGet() == TYP_SIMD32);
    regNumber targetReg = simdNode->gtRegNum;
    regNumber op1Reg    = genConsumeReg(op1);
    assert(op1Reg != REG_NA);
    if (targetReg != REG_NA)
    {
        getEmitter()->emitIns_R_R_I(INS_vextractf128, EA_32BYTE, targetReg, op1Reg, 0x01);
        genProduceReg(simdNode);
    }
    else
    {
        // The localVar must have a stack home.
        unsigned   varNum = op1->AsLclVarCommon()->gtLclNum;
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
        assert(varDsc->lvOnFrame);
        // We want to store this to the upper 16 bytes of this localVar's home.
        int offs = 16;

        getEmitter()->emitIns_S_R_I(INS_vextractf128, EA_32BYTE, varNum, offs, op1Reg, 0x01);
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
    assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicUpperRestore);

    GenTree* op1 = simdNode->gtGetOp1();
    assert(op1->IsLocal() && op1->TypeGet() == TYP_SIMD32);
    regNumber srcReg    = simdNode->gtRegNum;
    regNumber lclVarReg = genConsumeReg(op1);
    assert(lclVarReg != REG_NA);
    if (srcReg != REG_NA)
    {
        getEmitter()->emitIns_R_R_R_I(INS_vinsertf128, EA_32BYTE, lclVarReg, lclVarReg, srcReg, 0x01);
    }
    else
    {
        // The localVar must have a stack home.
        unsigned   varNum = op1->AsLclVarCommon()->gtLclNum;
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
        assert(varDsc->lvOnFrame);
        // We will load this from the upper 16 bytes of this localVar's home.
        int offs = 16;
        getEmitter()->emitIns_R_R_S_I(INS_vinsertf128, EA_32BYTE, lclVarReg, lclVarReg, varNum, offs, 0x01);
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
    if (simdNode->gtSIMDBaseType != TYP_INT && simdNode->gtSIMDBaseType != TYP_LONG &&
        simdNode->gtSIMDBaseType != TYP_FLOAT && simdNode->gtSIMDBaseType != TYP_DOUBLE &&
        simdNode->gtSIMDBaseType != TYP_USHORT && simdNode->gtSIMDBaseType != TYP_UBYTE &&
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
        case SIMDIntrinsicAbs:
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

        case SIMDIntrinsicWidenLo:
        case SIMDIntrinsicWidenHi:
            genSIMDIntrinsicWiden(simdNode);
            break;

        case SIMDIntrinsicNarrow:
            genSIMDIntrinsicNarrow(simdNode);
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
#endif //_TARGET_XARCH_
