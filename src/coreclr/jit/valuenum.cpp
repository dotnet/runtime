// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           ValueNum                                        XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "valuenum.h"
#include "ssaconfig.h"

// Windows x86 and Windows ARM/ARM64 may not define _isnanf() but they do define _isnan().
// We will redirect the macros to these other functions if the macro is not defined for the
// platform. This has the side effect of a possible implicit upcasting for arguments passed.
#if (defined(HOST_X86) || defined(HOST_ARM) || defined(HOST_ARM64)) && !defined(HOST_UNIX)

#if !defined(_isnanf)
#define _isnanf _isnan
#endif

#endif // (defined(HOST_X86) || defined(HOST_ARM) || defined(HOST_ARM64)) && !defined(HOST_UNIX)

// We need to use target-specific NaN values when statically compute expressions.
// Otherwise, cross crossgen (e.g. x86_arm) would have different binary outputs
// from native crossgen (i.e. arm_arm) when the NaN got "embedded" into code.
//
// For example, when placing NaN value in r3 register
// x86_arm crossgen would emit
//   movw    r3, 0x00
//   movt    r3, 0xfff8
// while arm_arm crossgen (and JIT) output is
//   movw    r3, 0x00
//   movt    r3, 0x7ff8

struct FloatTraits
{
    //------------------------------------------------------------------------
    // NaN: Return target-specific float NaN value
    //
    // Notes:
    //    "Default" NaN value returned by expression 0.0f / 0.0f on x86/x64 has
    //    different binary representation (0xffc00000) than NaN on
    //    ARM32/ARM64/LoongArch64 (0x7fc00000).

    static float NaN()
    {
#if defined(TARGET_XARCH)
        unsigned bits = 0xFFC00000u;
#elif defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        unsigned           bits = 0x7FC00000u;
#else
#error Unsupported or unset target architecture
#endif
        float result;
        static_assert(sizeof(bits) == sizeof(result), "sizeof(unsigned) must equal sizeof(float)");
        memcpy(&result, &bits, sizeof(result));
        return result;
    }
};

struct DoubleTraits
{
    //------------------------------------------------------------------------
    // NaN: Return target-specific double NaN value
    //
    // Notes:
    //    "Default" NaN value returned by expression 0.0 / 0.0 on x86/x64 has
    //    different binary representation (0xfff8000000000000) than NaN on
    //    ARM32/ARM64/LoongArch64 (0x7ff8000000000000).

    static double NaN()
    {
#if defined(TARGET_XARCH)
        unsigned long long bits = 0xFFF8000000000000ull;
#elif defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        unsigned long long bits = 0x7FF8000000000000ull;
#else
#error Unsupported or unset target architecture
#endif
        double result;
        static_assert(sizeof(bits) == sizeof(result), "sizeof(unsigned long long) must equal sizeof(double)");
        memcpy(&result, &bits, sizeof(result));
        return result;
    }
};

//------------------------------------------------------------------------
// FpAdd: Computes value1 + value2
//
// Return Value:
//    TFpTraits::NaN() - If target ARM32/ARM64 and result value is NaN
//    value1 + value2  - Otherwise
//
// Notes:
//    See FloatTraits::NaN() and DoubleTraits::NaN() notes.

template <typename TFp, typename TFpTraits>
TFp FpAdd(TFp value1, TFp value2)
{
#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    // If [value1] is negative infinity and [value2] is positive infinity
    //   the result is NaN.
    // If [value1] is positive infinity and [value2] is negative infinity
    //   the result is NaN.

    if (!_finite(value1) && !_finite(value2))
    {
        if (value1 < 0 && value2 > 0)
        {
            return TFpTraits::NaN();
        }

        if (value1 > 0 && value2 < 0)
        {
            return TFpTraits::NaN();
        }
    }
#endif // TARGET_ARMARCH || TARGET_LOONGARCH64 || TARGET_RISCV64

    return value1 + value2;
}

//------------------------------------------------------------------------
// FpSub: Computes value1 - value2
//
// Return Value:
//    TFpTraits::NaN() - If target ARM32/ARM64 and result value is NaN
//    value1 - value2  - Otherwise
//
// Notes:
//    See FloatTraits::NaN() and DoubleTraits::NaN() notes.

template <typename TFp, typename TFpTraits>
TFp FpSub(TFp value1, TFp value2)
{
#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    // If [value1] is positive infinity and [value2] is positive infinity
    //   the result is NaN.
    // If [value1] is negative infinity and [value2] is negative infinity
    //   the result is NaN.

    if (!_finite(value1) && !_finite(value2))
    {
        if (value1 > 0 && value2 > 0)
        {
            return TFpTraits::NaN();
        }

        if (value1 < 0 && value2 < 0)
        {
            return TFpTraits::NaN();
        }
    }
#endif // TARGET_ARMARCH || TARGET_LOONGARCH64 || TARGET_RISCV64

    return value1 - value2;
}

//------------------------------------------------------------------------
// FpMul: Computes value1 * value2
//
// Return Value:
//    TFpTraits::NaN() - If target ARM32/ARM64 and result value is NaN
//    value1 * value2  - Otherwise
//
// Notes:
//    See FloatTraits::NaN() and DoubleTraits::NaN() notes.

template <typename TFp, typename TFpTraits>
TFp FpMul(TFp value1, TFp value2)
{
#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    // From the ECMA standard:
    //
    // If [value1] is zero and [value2] is infinity
    //   the result is NaN.
    // If [value1] is infinity and [value2] is zero
    //   the result is NaN.

    if (value1 == 0 && !_finite(value2) && !_isnan(value2))
    {
        return TFpTraits::NaN();
    }
    if (!_finite(value1) && !_isnan(value1) && value2 == 0)
    {
        return TFpTraits::NaN();
    }
#endif // TARGET_ARMARCH || TARGET_LOONGARCH64 || TARGET_RISCV64

    return value1 * value2;
}

//------------------------------------------------------------------------
// FpDiv: Computes value1 / value2
//
// Return Value:
//    TFpTraits::NaN() - If target ARM32/ARM64 and result value is NaN
//    value1 / value2  - Otherwise
//
// Notes:
//    See FloatTraits::NaN() and DoubleTraits::NaN() notes.

template <typename TFp, typename TFpTraits>
TFp FpDiv(TFp dividend, TFp divisor)
{
#if defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    // From the ECMA standard:
    //
    // If [dividend] is zero and [divisor] is zero
    //   the result is NaN.
    // If [dividend] is infinity and [divisor] is infinity
    //   the result is NaN.

    if (dividend == 0 && divisor == 0)
    {
        return TFpTraits::NaN();
    }
    else if (!_finite(dividend) && !_isnan(dividend) && !_finite(divisor) && !_isnan(divisor))
    {
        return TFpTraits::NaN();
    }
#endif // TARGET_ARMARCH || TARGET_LOONGARCH64 || TARGET_RISCV64

    return dividend / divisor;
}

template <typename TFp, typename TFpTraits>
TFp FpRem(TFp dividend, TFp divisor)
{
    // From the ECMA standard:
    //
    // If [divisor] is zero or [dividend] is infinity
    //   the result is NaN.
    // If [divisor] is infinity,
    //   the result is [dividend]

    if (divisor == 0 || !_finite(dividend))
    {
        return TFpTraits::NaN();
    }
    else if (!_finite(divisor) && !_isnan(divisor))
    {
        return dividend;
    }

    return (TFp)fmod((double)dividend, (double)divisor);
}

//--------------------------------------------------------------------------------
// GetVNFuncForNode: Given a GenTree node, this returns the proper VNFunc to use
// for ValueNumbering
//
// Arguments:
//    node - The GenTree node that we need the VNFunc for.
//
// Return Value:
//    The VNFunc to use for this GenTree node
//
// Notes:
//    Some opers have their semantics affected by GTF flags so they need to be
//    replaced by special VNFunc values:
//      - relops are affected by GTF_UNSIGNED/GTF_RELOP_NAN_UN
//      - ADD/SUB/MUL are affected by GTF_OVERFLOW and GTF_UNSIGNED
//
VNFunc GetVNFuncForNode(GenTree* node)
{
    static const VNFunc relopUnFuncs[]{VNF_LT_UN, VNF_LE_UN, VNF_GE_UN, VNF_GT_UN};
    static_assert_no_msg(GT_LE - GT_LT == 1);
    static_assert_no_msg(GT_GE - GT_LT == 2);
    static_assert_no_msg(GT_GT - GT_LT == 3);

    static const VNFunc binopOvfFuncs[]{VNF_ADD_OVF, VNF_SUB_OVF, VNF_MUL_OVF};
    static const VNFunc binopUnOvfFuncs[]{VNF_ADD_UN_OVF, VNF_SUB_UN_OVF, VNF_MUL_UN_OVF};
    static_assert_no_msg(GT_SUB - GT_ADD == 1);
    static_assert_no_msg(GT_MUL - GT_ADD == 2);

    switch (node->OperGet())
    {
        case GT_EQ:
            if (varTypeIsFloating(node->gtGetOp1()))
            {
                assert(varTypeIsFloating(node->gtGetOp2()));
                assert((node->gtFlags & GTF_RELOP_NAN_UN) == 0);
            }
            break;

        case GT_NE:
            if (varTypeIsFloating(node->gtGetOp1()))
            {
                assert(varTypeIsFloating(node->gtGetOp2()));
                assert((node->gtFlags & GTF_RELOP_NAN_UN) != 0);
            }
            break;

        case GT_LT:
        case GT_LE:
        case GT_GT:
        case GT_GE:
            if (varTypeIsFloating(node->gtGetOp1()))
            {
                assert(varTypeIsFloating(node->gtGetOp2()));
                if ((node->gtFlags & GTF_RELOP_NAN_UN) != 0)
                {
                    return relopUnFuncs[node->OperGet() - GT_LT];
                }
            }
            else
            {
                assert(varTypeIsIntegralOrI(node->gtGetOp1()));
                assert(varTypeIsIntegralOrI(node->gtGetOp2()));
                if (node->IsUnsigned())
                {
                    return relopUnFuncs[node->OperGet() - GT_LT];
                }
            }
            break;

        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
            if (varTypeIsIntegralOrI(node->gtGetOp1()) && node->gtOverflow())
            {
                assert(varTypeIsIntegralOrI(node->gtGetOp2()));
                if (node->IsUnsigned())
                {
                    return binopUnOvfFuncs[node->OperGet() - GT_ADD];
                }
                else
                {
                    return binopOvfFuncs[node->OperGet() - GT_ADD];
                }
            }
            break;

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            return VNFunc(VNF_HWI_FIRST + (node->AsHWIntrinsic()->GetHWIntrinsicId() - NI_HW_INTRINSIC_START - 1));
#endif // FEATURE_HW_INTRINSICS

        case GT_CAST:
            // GT_CAST can overflow but it has special handling and it should not appear here.
            unreached();

        default:
            // Make sure we don't miss an onverflow oper, if a new one is ever added.
            assert(!GenTree::OperMayOverflow(node->OperGet()));
            break;
    }

    return VNFunc(node->OperGet());
}

bool ValueNumStore::VNFuncIsOverflowArithmetic(VNFunc vnf)
{
    static_assert_no_msg(VNF_ADD_OVF + 1 == VNF_SUB_OVF);
    static_assert_no_msg(VNF_SUB_OVF + 1 == VNF_MUL_OVF);
    static_assert_no_msg(VNF_MUL_OVF + 1 == VNF_ADD_UN_OVF);
    static_assert_no_msg(VNF_ADD_UN_OVF + 1 == VNF_SUB_UN_OVF);
    static_assert_no_msg(VNF_SUB_UN_OVF + 1 == VNF_MUL_UN_OVF);

    return VNF_ADD_OVF <= vnf && vnf <= VNF_MUL_UN_OVF;
}

bool ValueNumStore::VNFuncIsNumericCast(VNFunc vnf)
{
    return (vnf == VNF_Cast) || (vnf == VNF_CastOvf);
}

unsigned ValueNumStore::VNFuncArity(VNFunc vnf)
{
    // Read the bit field out of the table...
    return (s_vnfOpAttribs[vnf] & VNFOA_ArityMask) >> VNFOA_ArityShift;
}

template <>
bool ValueNumStore::IsOverflowIntDiv(int v0, int v1)
{
    return (v1 == -1) && (v0 == INT32_MIN);
}

template <>
bool ValueNumStore::IsOverflowIntDiv(INT64 v0, INT64 v1)
{
    return (v1 == -1) && (v0 == INT64_MIN);
}

template <typename T>
bool ValueNumStore::IsOverflowIntDiv(T v0, T v1)
{
    return false;
}

template <>
bool ValueNumStore::IsIntZero(int v)
{
    return v == 0;
}
template <>
bool ValueNumStore::IsIntZero(unsigned v)
{
    return v == 0;
}
template <>
bool ValueNumStore::IsIntZero(INT64 v)
{
    return v == 0;
}
template <>
bool ValueNumStore::IsIntZero(UINT64 v)
{
    return v == 0;
}
template <typename T>
bool ValueNumStore::IsIntZero(T v)
{
    return false;
}

ValueNumStore::ValueNumStore(Compiler* comp, CompAllocator alloc)
    : m_pComp(comp)
    , m_alloc(alloc)
    , m_nextChunkBase(0)
    , m_fixedPointMapSels(alloc, 8)
    , m_checkedBoundVNs(alloc)
    , m_chunks(alloc, 8)
    , m_intCnsMap(nullptr)
    , m_longCnsMap(nullptr)
    , m_handleMap(nullptr)
    , m_embeddedToCompileTimeHandleMap(alloc)
    , m_fieldAddressToFieldSeqMap(alloc)
    , m_floatCnsMap(nullptr)
    , m_doubleCnsMap(nullptr)
    , m_byrefCnsMap(nullptr)
#if defined(FEATURE_SIMD)
    , m_simd8CnsMap(nullptr)
    , m_simd12CnsMap(nullptr)
    , m_simd16CnsMap(nullptr)
#if defined(TARGET_XARCH)
    , m_simd32CnsMap(nullptr)
    , m_simd64CnsMap(nullptr)
#endif // TARGET_XARCH
#endif // FEATURE_SIMD
    , m_VNFunc0Map(nullptr)
    , m_VNFunc1Map(nullptr)
    , m_VNFunc2Map(nullptr)
    , m_VNFunc3Map(nullptr)
    , m_VNFunc4Map(nullptr)
#ifdef DEBUG
    , m_numMapSels(0)
#endif
{
    // We have no current allocation chunks.
    for (unsigned i = 0; i < TYP_COUNT; i++)
    {
        for (unsigned j = CEA_Const; j <= CEA_Count; j++)
        {
            m_curAllocChunk[i][j] = NoChunk;
        }
    }

    for (unsigned i = 0; i < SmallIntConstNum; i++)
    {
        m_VNsForSmallIntConsts[i] = NoVN;
    }
    // We will reserve chunk 0 to hold some special constants.
    Chunk* specialConstChunk = new (m_alloc) Chunk(m_alloc, &m_nextChunkBase, TYP_REF, CEA_Const);
    specialConstChunk->m_numUsed += SRC_NumSpecialRefConsts;
    ChunkNum cn = m_chunks.Push(specialConstChunk);
    assert(cn == 0);

    m_mapSelectBudget = (int)JitConfig.JitVNMapSelBudget(); // We cast the unsigned DWORD to a signed int.

    // This value must be non-negative and non-zero, reset the value to DEFAULT_MAP_SELECT_BUDGET if it isn't.
    if (m_mapSelectBudget <= 0)
    {
        m_mapSelectBudget = DEFAULT_MAP_SELECT_BUDGET;
    }

#ifdef DEBUG
    if (comp->compStressCompile(Compiler::STRESS_VN_BUDGET, 50))
    {
        // Bias toward smaller budgets as we want to stress returning
        // unexpectedly opaque results.
        //
        CLRRandom* random = comp->m_inlineStrategy->GetRandom(comp->info.compMethodHash());
        double     p      = random->NextDouble();

        if (p <= 0.5)
        {
            m_mapSelectBudget = random->Next(0, 5);
        }
        else
        {
            int limit         = random->Next(1, DEFAULT_MAP_SELECT_BUDGET + 1);
            m_mapSelectBudget = random->Next(0, limit);
        }

        JITDUMP("VN Stress: setting select budget to %u\n", m_mapSelectBudget);
    }
#endif
}

//
// Unary EvalOp
//

template <typename T>
T ValueNumStore::EvalOp(VNFunc vnf, T v0)
{
    genTreeOps oper = genTreeOps(vnf);

    // Here we handle unary ops that are the same for all types.
    switch (oper)
    {
        case GT_NEG:
            // Note that GT_NEG is the only valid unary floating point operation
            return -v0;

        default:
            break;
    }

    // Otherwise must be handled by the type specific method
    return EvalOpSpecialized(vnf, v0);
}

template <>
double ValueNumStore::EvalOpSpecialized<double>(VNFunc vnf, double v0)
{
    // Here we handle specialized double unary ops.
    noway_assert(!"EvalOpSpecialized<double> - unary");
    return 0.0;
}

template <>
float ValueNumStore::EvalOpSpecialized<float>(VNFunc vnf, float v0)
{
    // Here we handle specialized float unary ops.
    noway_assert(!"EvalOpSpecialized<float> - unary");
    return 0.0f;
}

template <typename T>
T ValueNumStore::EvalOpSpecialized(VNFunc vnf, T v0)
{
    if (vnf < VNF_Boundary)
    {
        genTreeOps oper = genTreeOps(vnf);

        switch (oper)
        {
            case GT_NEG:
                return -v0;

            case GT_NOT:
                return ~v0;

            case GT_BSWAP16:
            {
                UINT16 v0_unsigned = UINT16(v0);

                v0_unsigned = ((v0_unsigned >> 8) & 0xFF) | ((v0_unsigned << 8) & 0xFF00);
                return T(v0_unsigned);
            }

            case GT_BSWAP:
                if (sizeof(T) == 4)
                {
                    UINT32 v0_unsigned = UINT32(v0);

                    v0_unsigned = ((v0_unsigned >> 24) & 0xFF) | ((v0_unsigned >> 8) & 0xFF00) |
                                  ((v0_unsigned << 8) & 0xFF0000) | ((v0_unsigned << 24) & 0xFF000000);
                    return T(v0_unsigned);
                }
                else if (sizeof(T) == 8)
                {
                    UINT64 v0_unsigned = UINT64(v0);

                    v0_unsigned = ((v0_unsigned >> 56) & 0xFF) | ((v0_unsigned >> 40) & 0xFF00) |
                                  ((v0_unsigned >> 24) & 0xFF0000) | ((v0_unsigned >> 8) & 0xFF000000) |
                                  ((v0_unsigned << 8) & 0xFF00000000) | ((v0_unsigned << 24) & 0xFF0000000000) |
                                  ((v0_unsigned << 40) & 0xFF000000000000) | ((v0_unsigned << 56) & 0xFF00000000000000);
                    return T(v0_unsigned);
                }
                else
                {
                    break; // unknown primitive
                }

            default:
                break;
        }
    }

    noway_assert(!"Unhandled operation in EvalOpSpecialized<T> - unary");
    return v0;
}

//
// Binary EvalOp
//

template <typename T>
T ValueNumStore::EvalOp(VNFunc vnf, T v0, T v1)
{
    // Here we handle the binary ops that are the same for all types.

    // Currently there are none (due to floating point NaN representations)

    // Otherwise must be handled by the type specific method
    return EvalOpSpecialized(vnf, v0, v1);
}

template <>
double ValueNumStore::EvalOpSpecialized<double>(VNFunc vnf, double v0, double v1)
{
    // Here we handle specialized double binary ops.
    if (vnf < VNF_Boundary)
    {
        genTreeOps oper = genTreeOps(vnf);

        // Here we handle
        switch (oper)
        {
            case GT_ADD:
                return FpAdd<double, DoubleTraits>(v0, v1);
            case GT_SUB:
                return FpSub<double, DoubleTraits>(v0, v1);
            case GT_MUL:
                return FpMul<double, DoubleTraits>(v0, v1);
            case GT_DIV:
                return FpDiv<double, DoubleTraits>(v0, v1);
            case GT_MOD:
                return FpRem<double, DoubleTraits>(v0, v1);

            default:
                // For any other value of 'oper', we will assert below
                break;
        }
    }

    noway_assert(!"EvalOpSpecialized<double> - binary");
    return v0;
}

template <>
float ValueNumStore::EvalOpSpecialized<float>(VNFunc vnf, float v0, float v1)
{
    // Here we handle specialized float binary ops.
    if (vnf < VNF_Boundary)
    {
        genTreeOps oper = genTreeOps(vnf);

        // Here we handle
        switch (oper)
        {
            case GT_ADD:
                return FpAdd<float, FloatTraits>(v0, v1);
            case GT_SUB:
                return FpSub<float, FloatTraits>(v0, v1);
            case GT_MUL:
                return FpMul<float, FloatTraits>(v0, v1);
            case GT_DIV:
                return FpDiv<float, FloatTraits>(v0, v1);
            case GT_MOD:
                return FpRem<float, FloatTraits>(v0, v1);

            default:
                // For any other value of 'oper', we will assert below
                break;
        }
    }
    assert(!"EvalOpSpecialized<float> - binary");
    return v0;
}

template <typename T>
T ValueNumStore::EvalOpSpecialized(VNFunc vnf, T v0, T v1)
{
    typedef typename std::make_unsigned<T>::type UT;

    assert((sizeof(T) == 4) || (sizeof(T) == 8));

    // Here we handle binary ops that are the same for all integer types
    if (vnf < VNF_Boundary)
    {
        genTreeOps oper = genTreeOps(vnf);

        switch (oper)
        {
            case GT_ADD:
                return v0 + v1;
            case GT_SUB:
                return v0 - v1;
            case GT_MUL:
                return v0 * v1;

            case GT_DIV:
                assert(IsIntZero(v1) == false);
                assert(IsOverflowIntDiv(v0, v1) == false);
                return v0 / v1;

            case GT_MOD:
                assert(IsIntZero(v1) == false);
                assert(IsOverflowIntDiv(v0, v1) == false);
                return v0 % v1;

            case GT_UDIV:
                assert(IsIntZero(v1) == false);
                return T(UT(v0) / UT(v1));

            case GT_UMOD:
                assert(IsIntZero(v1) == false);
                return T(UT(v0) % UT(v1));

            case GT_AND:
                return v0 & v1;
            case GT_OR:
                return v0 | v1;
            case GT_XOR:
                return v0 ^ v1;

            case GT_LSH:
                if (sizeof(T) == 8)
                {
                    return v0 << (v1 & 0x3F);
                }
                else
                {
                    return v0 << v1;
                }
            case GT_RSH:
                if (sizeof(T) == 8)
                {
                    return v0 >> (v1 & 0x3F);
                }
                else
                {
                    return v0 >> v1;
                }
            case GT_RSZ:
                if (sizeof(T) == 8)
                {
                    return UINT64(v0) >> (v1 & 0x3F);
                }
                else
                {
                    return UINT32(v0) >> v1;
                }
            case GT_ROL:
                if (sizeof(T) == 8)
                {
                    return (v0 << v1) | (UINT64(v0) >> (64 - v1));
                }
                else
                {
                    return (v0 << v1) | (UINT32(v0) >> (32 - v1));
                }

            case GT_ROR:
                if (sizeof(T) == 8)
                {
                    return (v0 << (64 - v1)) | (UINT64(v0) >> v1);
                }
                else
                {
                    return (v0 << (32 - v1)) | (UINT32(v0) >> v1);
                }

            default:
                // For any other value of 'oper', we will assert below
                break;
        }
    }
    else // must be a VNF_ function
    {
        switch (vnf)
        {
            // Here we handle those that are the same for all integer types.
            case VNF_ADD_OVF:
            case VNF_ADD_UN_OVF:
                assert(!CheckedOps::AddOverflows(v0, v1, vnf == VNF_ADD_UN_OVF));
                return v0 + v1;

            case VNF_SUB_OVF:
            case VNF_SUB_UN_OVF:
                assert(!CheckedOps::SubOverflows(v0, v1, vnf == VNF_SUB_UN_OVF));
                return v0 - v1;

            case VNF_MUL_OVF:
            case VNF_MUL_UN_OVF:
                assert(!CheckedOps::MulOverflows(v0, v1, vnf == VNF_MUL_UN_OVF));
                return v0 * v1;

            default:
                // For any other value of 'vnf', we will assert below
                break;
        }
    }

    noway_assert(!"Unhandled operation in EvalOpSpecialized<T> - binary");
    return v0;
}

template <>
int ValueNumStore::EvalComparison<double>(VNFunc vnf, double v0, double v1)
{
    // Here we handle specialized double comparisons.

    // We must check for a NaN argument as they they need special handling
    bool hasNanArg = (_isnan(v0) || _isnan(v1));

    if (vnf < VNF_Boundary)
    {
        genTreeOps oper = genTreeOps(vnf);

        if (hasNanArg)
        {
            // return false in all cases except for GT_NE;
            return (oper == GT_NE);
        }

        switch (oper)
        {
            case GT_EQ:
                return v0 == v1;
            case GT_NE:
                return v0 != v1;
            case GT_GT:
                return v0 > v1;
            case GT_GE:
                return v0 >= v1;
            case GT_LT:
                return v0 < v1;
            case GT_LE:
                return v0 <= v1;
            default:
                // For any other value of 'oper', we will assert below
                break;
        }
    }
    else // must be a VNF_ function
    {
        if (hasNanArg)
        {
            // unordered comparisons with NaNs always return true
            return true;
        }

        switch (vnf)
        {
            case VNF_GT_UN:
                return v0 > v1;
            case VNF_GE_UN:
                return v0 >= v1;
            case VNF_LT_UN:
                return v0 < v1;
            case VNF_LE_UN:
                return v0 <= v1;
            default:
                // For any other value of 'vnf', we will assert below
                break;
        }
    }
    noway_assert(!"Unhandled operation in EvalComparison<double>");
    return 0;
}

template <>
int ValueNumStore::EvalComparison<float>(VNFunc vnf, float v0, float v1)
{
    // Here we handle specialized float comparisons.

    // We must check for a NaN argument as they they need special handling
    bool hasNanArg = (_isnanf(v0) || _isnanf(v1));

    if (vnf < VNF_Boundary)
    {
        genTreeOps oper = genTreeOps(vnf);

        if (hasNanArg)
        {
            // return false in all cases except for GT_NE;
            return (oper == GT_NE);
        }

        switch (oper)
        {
            case GT_EQ:
                return v0 == v1;
            case GT_NE:
                return v0 != v1;
            case GT_GT:
                return v0 > v1;
            case GT_GE:
                return v0 >= v1;
            case GT_LT:
                return v0 < v1;
            case GT_LE:
                return v0 <= v1;
            default:
                // For any other value of 'oper', we will assert below
                break;
        }
    }
    else // must be a VNF_ function
    {
        if (hasNanArg)
        {
            // unordered comparisons with NaNs always return true
            return true;
        }

        switch (vnf)
        {
            case VNF_GT_UN:
                return v0 > v1;
            case VNF_GE_UN:
                return v0 >= v1;
            case VNF_LT_UN:
                return v0 < v1;
            case VNF_LE_UN:
                return v0 <= v1;
            default:
                // For any other value of 'vnf', we will assert below
                break;
        }
    }
    noway_assert(!"Unhandled operation in EvalComparison<float>");
    return 0;
}

template <typename T>
int ValueNumStore::EvalComparison(VNFunc vnf, T v0, T v1)
{
    typedef typename std::make_unsigned<T>::type UT;

    // Here we handle the compare ops that are the same for all integer types.
    if (vnf < VNF_Boundary)
    {
        genTreeOps oper = genTreeOps(vnf);
        switch (oper)
        {
            case GT_EQ:
                return v0 == v1;
            case GT_NE:
                return v0 != v1;
            case GT_GT:
                return v0 > v1;
            case GT_GE:
                return v0 >= v1;
            case GT_LT:
                return v0 < v1;
            case GT_LE:
                return v0 <= v1;
            default:
                // For any other value of 'oper', we will assert below
                break;
        }
    }
    else // must be a VNF_ function
    {
        switch (vnf)
        {
            case VNF_GT_UN:
                return T(UT(v0) > UT(v1));
            case VNF_GE_UN:
                return T(UT(v0) >= UT(v1));
            case VNF_LT_UN:
                return T(UT(v0) < UT(v1));
            case VNF_LE_UN:
                return T(UT(v0) <= UT(v1));
            default:
                // For any other value of 'vnf', we will assert below
                break;
        }
    }
    noway_assert(!"Unhandled operation in EvalComparison<T>");
    return 0;
}

// Create a ValueNum for an exception set singleton for 'x'
//
ValueNum ValueNumStore::VNExcSetSingleton(ValueNum x)
{
    return VNForFuncNoFolding(TYP_REF, VNF_ExcSetCons, x, VNForEmptyExcSet());
}
// Create a ValueNumPair for an exception set singleton for 'xp'
//
ValueNumPair ValueNumStore::VNPExcSetSingleton(ValueNumPair xp)
{
    return ValueNumPair(VNExcSetSingleton(xp.GetLiberal()), VNExcSetSingleton(xp.GetConservative()));
}

//-------------------------------------------------------------------------------------------
// VNCheckAscending: - Helper method used to verify that elements in an exception set list
//                     are sorted in ascending order.  This method only checks that the
//                     next value in the list has a greater value number than 'item'.
//
// Arguments:
//    item           - The previous item visited in the exception set that we are iterating
//    xs1            - The tail portion of the exception set that we are iterating.
//
// Return Value:
//                   - Returns true when the next value is greater than 'item'
//                   - or when we have an empty list remaining.
//
// Note:  - Duplicates items aren't allowed in an exception set
//          Used to verify that exception sets are in ascending order when processing them.
//
bool ValueNumStore::VNCheckAscending(ValueNum item, ValueNum xs1)
{
    if (xs1 == VNForEmptyExcSet())
    {
        return true;
    }
    else
    {
        VNFuncApp funcXs1;
        bool      b1 = GetVNFunc(xs1, &funcXs1);
        assert(b1 && funcXs1.m_func == VNF_ExcSetCons); // Precondition: xs1 is an exception set.

        return (item < funcXs1.m_args[0]);
    }
}

//-------------------------------------------------------------------------------------------
// VNExcSetUnion: - Given two exception sets, performs a set Union operation
//                  and returns the value number for the combined exception set.
//
// Arguments:     - The arguments must be applications of VNF_ExcSetCons or the empty set
//    xs0         - The value number of the first exception set
//    xs1         - The value number of the second exception set
//
// Return Value:  - The value number of the combined exception set
//
// Note: - Checks and relies upon the invariant that exceptions sets
//          1. Have no duplicate values
//          2. all elements in an exception set are in sorted order.
//
ValueNum ValueNumStore::VNExcSetUnion(ValueNum xs0, ValueNum xs1)
{
    if (xs0 == VNForEmptyExcSet())
    {
        return xs1;
    }
    else if (xs1 == VNForEmptyExcSet())
    {
        return xs0;
    }
    else
    {
        VNFuncApp funcXs0;
        bool      b0 = GetVNFunc(xs0, &funcXs0);
        assert(b0 && funcXs0.m_func == VNF_ExcSetCons); // Precondition: xs0 is an exception set.
        VNFuncApp funcXs1;
        bool      b1 = GetVNFunc(xs1, &funcXs1);
        assert(b1 && funcXs1.m_func == VNF_ExcSetCons); // Precondition: xs1 is an exception set.
        ValueNum res = NoVN;
        if (funcXs0.m_args[0] < funcXs1.m_args[0])
        {
            assert(VNCheckAscending(funcXs0.m_args[0], funcXs0.m_args[1]));

            // add the lower one (from xs0) to the result, advance xs0
            res = VNForFuncNoFolding(TYP_REF, VNF_ExcSetCons, funcXs0.m_args[0], VNExcSetUnion(funcXs0.m_args[1], xs1));
        }
        else if (funcXs0.m_args[0] == funcXs1.m_args[0])
        {
            assert(VNCheckAscending(funcXs0.m_args[0], funcXs0.m_args[1]));
            assert(VNCheckAscending(funcXs1.m_args[0], funcXs1.m_args[1]));

            // Equal elements; add one (from xs0) to the result, advance both sets
            res = VNForFuncNoFolding(TYP_REF, VNF_ExcSetCons, funcXs0.m_args[0],
                                     VNExcSetUnion(funcXs0.m_args[1], funcXs1.m_args[1]));
        }
        else
        {
            assert(funcXs0.m_args[0] > funcXs1.m_args[0]);
            assert(VNCheckAscending(funcXs1.m_args[0], funcXs1.m_args[1]));

            // add the lower one (from xs1) to the result, advance xs1
            res = VNForFuncNoFolding(TYP_REF, VNF_ExcSetCons, funcXs1.m_args[0], VNExcSetUnion(xs0, funcXs1.m_args[1]));
        }

        return res;
    }
}

//--------------------------------------------------------------------------------
// VNPExcSetUnion: - Returns a Value Number Pair that represents the set union
//                   for both parts.
//                   (see VNExcSetUnion for more details)
//
// Notes:   - This method is used to form a Value Number Pair when we
//            want both the Liberal and Conservative Value Numbers
//
ValueNumPair ValueNumStore::VNPExcSetUnion(ValueNumPair xs0vnp, ValueNumPair xs1vnp)
{
    return ValueNumPair(VNExcSetUnion(xs0vnp.GetLiberal(), xs1vnp.GetLiberal()),
                        VNExcSetUnion(xs0vnp.GetConservative(), xs1vnp.GetConservative()));
}

//-------------------------------------------------------------------------------------------
// VNExcSetIntersection: - Given two exception sets, performs a set Intersection operation
//                         and returns the value number for this exception set.
//
// Arguments:     - The arguments must be applications of VNF_ExcSetCons or the empty set
//    xs0         - The value number of the first exception set
//    xs1         - The value number of the second exception set
//
// Return Value:  - The value number of the new exception set.
//                  if the e are no values in common then VNForEmptyExcSet() is returned.
//
// Note: - Checks and relies upon the invariant that exceptions sets
//          1. Have no duplicate values
//          2. all elements in an exception set are in sorted order.
//
ValueNum ValueNumStore::VNExcSetIntersection(ValueNum xs0, ValueNum xs1)
{
    if ((xs0 == VNForEmptyExcSet()) || (xs1 == VNForEmptyExcSet()))
    {
        return VNForEmptyExcSet();
    }
    else
    {
        VNFuncApp funcXs0;
        bool      b0 = GetVNFunc(xs0, &funcXs0);
        assert(b0 && funcXs0.m_func == VNF_ExcSetCons); // Precondition: xs0 is an exception set.
        VNFuncApp funcXs1;
        bool      b1 = GetVNFunc(xs1, &funcXs1);
        assert(b1 && funcXs1.m_func == VNF_ExcSetCons); // Precondition: xs1 is an exception set.
        ValueNum res = NoVN;

        if (funcXs0.m_args[0] < funcXs1.m_args[0])
        {
            assert(VNCheckAscending(funcXs0.m_args[0], funcXs0.m_args[1]));
            res = VNExcSetIntersection(funcXs0.m_args[1], xs1);
        }
        else if (funcXs0.m_args[0] == funcXs1.m_args[0])
        {
            assert(VNCheckAscending(funcXs0.m_args[0], funcXs0.m_args[1]));
            assert(VNCheckAscending(funcXs1.m_args[0], funcXs1.m_args[1]));

            // Equal elements; Add it to the result.
            res = VNForFunc(TYP_REF, VNF_ExcSetCons, funcXs0.m_args[0],
                            VNExcSetIntersection(funcXs0.m_args[1], funcXs1.m_args[1]));
        }
        else
        {
            assert(funcXs0.m_args[0] > funcXs1.m_args[0]);
            assert(VNCheckAscending(funcXs1.m_args[0], funcXs1.m_args[1]));
            res = VNExcSetIntersection(xs0, funcXs1.m_args[1]);
        }

        return res;
    }
}

//--------------------------------------------------------------------------------
// VNPExcSetIntersection: - Returns a Value Number Pair that represents the set
//                 intersection for both parts.
//                 (see VNExcSetIntersection for more details)
//
// Notes:   - This method is used to form a Value Number Pair when we
//            want both the Liberal and Conservative Value Numbers
//
ValueNumPair ValueNumStore::VNPExcSetIntersection(ValueNumPair xs0vnp, ValueNumPair xs1vnp)
{
    return ValueNumPair(VNExcSetIntersection(xs0vnp.GetLiberal(), xs1vnp.GetLiberal()),
                        VNExcSetIntersection(xs0vnp.GetConservative(), xs1vnp.GetConservative()));
}

//----------------------------------------------------------------------------------------
// VNExcIsSubset     - Given two exception sets, returns true when vnCandidateSet is a
//                     subset of vnFullSet
//
// Arguments:        - The arguments must be applications of VNF_ExcSetCons or the empty set
//    vnFullSet      - The value number of the 'full' exception set
//    vnCandidateSet - The value number of the 'candidate' exception set
//
// Return Value:     - Returns true if every singleton ExcSet value in the vnCandidateSet
//                     is also present in the vnFullSet.
//
// Note: - Checks and relies upon the invariant that exceptions sets
//          1. Have no duplicate values
//          2. all elements in an exception set are in sorted order.
//
bool ValueNumStore::VNExcIsSubset(ValueNum vnFullSet, ValueNum vnCandidateSet)
{
    if (vnCandidateSet == VNForEmptyExcSet())
    {
        return true;
    }
    else if ((vnFullSet == VNForEmptyExcSet()) || (vnFullSet == ValueNumStore::NoVN))
    {
        return false;
    }

    VNFuncApp funcXsFull;
    bool      b0 = GetVNFunc(vnFullSet, &funcXsFull);
    assert(b0 && funcXsFull.m_func == VNF_ExcSetCons); // Precondition: vnFullSet is an exception set.
    VNFuncApp funcXsCand;
    bool      b1 = GetVNFunc(vnCandidateSet, &funcXsCand);
    assert(b1 && funcXsCand.m_func == VNF_ExcSetCons); // Precondition: vnCandidateSet is an exception set.

    ValueNum vnFullSetPrev = VNForNull();
    ValueNum vnCandSetPrev = VNForNull();

    ValueNum vnFullSetRemainder = funcXsFull.m_args[1];
    ValueNum vnCandSetRemainder = funcXsCand.m_args[1];

    while (true)
    {
        ValueNum vnFullSetItem = funcXsFull.m_args[0];
        ValueNum vnCandSetItem = funcXsCand.m_args[0];

        // Enforce that both sets are sorted by increasing ValueNumbers
        //
        assert(vnFullSetItem > vnFullSetPrev);
        assert(vnCandSetItem >= vnCandSetPrev); // equal when we didn't advance the candidate set

        if (vnFullSetItem > vnCandSetItem)
        {
            // The Full set does not contain the vnCandSetItem
            return false;
        }
        // now we must have (vnFullSetItem <= vnCandSetItem)

        // When we have a matching value we advance the candidate set
        //
        if (vnFullSetItem == vnCandSetItem)
        {
            // Have we finished matching?
            //
            if (vnCandSetRemainder == VNForEmptyExcSet())
            {
                // We matched every item in the candidate set'
                //
                return true;
            }

            // Advance the candidate set
            //
            b1 = GetVNFunc(vnCandSetRemainder, &funcXsCand);
            assert(b1 && funcXsCand.m_func == VNF_ExcSetCons); // Precondition: vnCandSetRemainder is an exception set.
            vnCandSetRemainder = funcXsCand.m_args[1];
        }

        if (vnFullSetRemainder == VNForEmptyExcSet())
        {
            // No more items are left in the full exception set
            return false;
        }

        //
        // We will advance the full set
        //
        b0 = GetVNFunc(vnFullSetRemainder, &funcXsFull);
        assert(b0 && funcXsFull.m_func == VNF_ExcSetCons); // Precondition: vnFullSetRemainder is an exception set.
        vnFullSetRemainder = funcXsFull.m_args[1];

        vnFullSetPrev = vnFullSetItem;
        vnCandSetPrev = vnCandSetItem;
    }
}

//----------------------------------------------------------------------------------------
// VNPExcIsSubset     - Given two exception sets, returns true when both the liberal and
//                      conservative value numbers of vnpCandidateSet represent subsets of
//                      the corresponding numbers in vnpFullSet (see VNExcIsSubset).
//
bool ValueNumStore::VNPExcIsSubset(ValueNumPair vnpFullSet, ValueNumPair vnpCandidateSet)
{
    return VNExcIsSubset(vnpFullSet.GetLiberal(), vnpCandidateSet.GetLiberal()) &&
           VNExcIsSubset(vnpFullSet.GetConservative(), vnpCandidateSet.GetConservative());
}

//-------------------------------------------------------------------------------------
// VNUnpackExc: - Given a ValueNum 'vnWx, return via write back parameters both
//                the normal and the exception set components.
//
// Arguments:
//    vnWx        - A value number, it may have an exception set
//    pvn         - a write back pointer to the normal value portion of 'vnWx'
//    pvnx        - a write back pointer for the exception set portion of 'vnWx'
//
// Return Values: - This method signature is void but returns two values using
//                  the write back parameters.
//
// Note: When 'vnWx' does not have an exception set, the original value is the
//       normal value and is written to 'pvn' and VNForEmptyExcSet() is
//       written to 'pvnx'.
//       When we have an exception set 'vnWx' will be a VN func with m_func
//       equal to VNF_ValWithExc.
//
void ValueNumStore::VNUnpackExc(ValueNum vnWx, ValueNum* pvn, ValueNum* pvnx)
{
    assert(vnWx != NoVN);
    VNFuncApp funcApp;
    if (GetVNFunc(vnWx, &funcApp) && funcApp.m_func == VNF_ValWithExc)
    {
        *pvn  = funcApp.m_args[0];
        *pvnx = funcApp.m_args[1];
    }
    else
    {
        *pvn  = vnWx;
        *pvnx = VNForEmptyExcSet();
    }
}

//-------------------------------------------------------------------------------------
// VNPUnpackExc: - Given a ValueNumPair 'vnpWx, return via write back parameters
//                 both the normal and the exception set components.
//                 (see VNUnpackExc for more details)
//
// Notes:   - This method is used to form a Value Number Pair when we
//            want both the Liberal and Conservative Value Numbers
//
void ValueNumStore::VNPUnpackExc(ValueNumPair vnpWx, ValueNumPair* pvnp, ValueNumPair* pvnpx)
{
    VNUnpackExc(vnpWx.GetLiberal(), pvnp->GetLiberalAddr(), pvnpx->GetLiberalAddr());
    VNUnpackExc(vnpWx.GetConservative(), pvnp->GetConservativeAddr(), pvnpx->GetConservativeAddr());
}

//-------------------------------------------------------------------------------------
// VNUnionExcSet: - Given a ValueNum 'vnWx' and a current 'vnExcSet', return an
//                  exception set of the Union of both exception sets.
//
// Arguments:
//    vnWx        - A value number, it may have an exception set
//    vnExcSet    - The value number for the current exception set
//
// Return Values: - The value number of the Union of the exception set of 'vnWx'
//                  with the current 'vnExcSet'.
//
// Note: When 'vnWx' does not have an exception set, 'vnExcSet' is returned.
//
ValueNum ValueNumStore::VNUnionExcSet(ValueNum vnWx, ValueNum vnExcSet)
{
    assert(vnWx != NoVN);
    VNFuncApp funcApp;
    if (GetVNFunc(vnWx, &funcApp) && funcApp.m_func == VNF_ValWithExc)
    {
        vnExcSet = VNExcSetUnion(funcApp.m_args[1], vnExcSet);
    }
    return vnExcSet;
}

//-------------------------------------------------------------------------------------
// VNPUnionExcSet: - Given a ValueNum 'vnWx' and a current 'excSet', return an
//                   exception set of the Union of both exception sets.
//                   (see VNUnionExcSet for more details)
//
// Notes:   - This method is used to form a Value Number Pair when we
//            want both the Liberal and Conservative Value Numbers
//
ValueNumPair ValueNumStore::VNPUnionExcSet(ValueNumPair vnpWx, ValueNumPair vnpExcSet)
{
    return ValueNumPair(VNUnionExcSet(vnpWx.GetLiberal(), vnpExcSet.GetLiberal()),
                        VNUnionExcSet(vnpWx.GetConservative(), vnpExcSet.GetConservative()));
}

//--------------------------------------------------------------------------------
// VNNormalValue: - Returns a Value Number that represents the result for the
//                  normal (non-exceptional) evaluation for the expression.
//
// Arguments:
//    vn         - The Value Number for the expression, including any excSet.
//                 This excSet is an optional item and represents the set of
//                 possible exceptions for the expression.
//
// Return Value:
//               - The Value Number for the expression without the exception set.
//                 This can be the original 'vn', when there are no exceptions.
//
// Notes:        - Whenever we have an exception set the Value Number will be
//                 a VN func with VNF_ValWithExc.
//                 This VN func has the normal value as m_args[0]
//
ValueNum ValueNumStore::VNNormalValue(ValueNum vn)
{
    VNFuncApp funcApp;
    if (GetVNFunc(vn, &funcApp) && funcApp.m_func == VNF_ValWithExc)
    {
        return funcApp.m_args[0];
    }
    else
    {
        return vn;
    }
}

//------------------------------------------------------------------------------------
// VNMakeNormalUnique:
//
// Arguments:
//    vn         - The current Value Number for the expression, including any excSet.
//                 This excSet is an optional item and represents the set of
//                 possible exceptions for the expression.
//
// Return Value:
//               - The normal value is set to a new unique VN, while keeping
//                 the excSet (if any)
//
ValueNum ValueNumStore::VNMakeNormalUnique(ValueNum orig)
{
    // First Unpack the existing Norm,Exc for 'elem'
    ValueNum vnOrigNorm;
    ValueNum vnOrigExcSet;
    VNUnpackExc(orig, &vnOrigNorm, &vnOrigExcSet);

    // Replace the normal value with a unique ValueNum
    ValueNum vnUnique = VNForExpr(m_pComp->compCurBB, TypeOfVN(vnOrigNorm));

    // Keep any ExcSet from 'elem'
    return VNWithExc(vnUnique, vnOrigExcSet);
}

//--------------------------------------------------------------------------------
// VNPMakeNormalUniquePair:
//
// Arguments:
//    vnp         - The Value Number Pair for the expression, including any excSet.
//
// Return Value:
//               - The normal values are set to a new unique VNs, while keeping
//                 the excSets (if any)
//
ValueNumPair ValueNumStore::VNPMakeNormalUniquePair(ValueNumPair vnp)
{
    return ValueNumPair(VNMakeNormalUnique(vnp.GetLiberal()), VNMakeNormalUnique(vnp.GetConservative()));
}

//------------------------------------------------------------------------------------
// VNUniqueWithExc:
//
// Arguments:
//    type       - The type for the unique Value Number
//    vnExcSet   - The Value Number for the exception set.
//
// Return Value:
//               - VN representing a "new, unique" value, with
//                 the exceptions contained in "vnExcSet".
//
ValueNum ValueNumStore::VNUniqueWithExc(var_types type, ValueNum vnExcSet)
{
    ValueNum normVN = VNForExpr(m_pComp->compCurBB, type);

    if (vnExcSet == VNForEmptyExcSet())
    {
        return normVN;
    }

#ifdef DEBUG
    VNFuncApp excSetFunc;
    assert(GetVNFunc(vnExcSet, &excSetFunc) && (excSetFunc.m_func == VNF_ExcSetCons));
#endif // DEBUG

    return VNWithExc(normVN, vnExcSet);
}

//------------------------------------------------------------------------------------
// VNPUniqueWithExc:
//
// Arguments:
//    type       - The type for the unique Value Numbers
//    vnExcSet   - The Value Number Pair for the exception set.
//
// Return Value:
//               - VN Pair representing a "new, unique" value (liberal and conservative
//                 values will be equal), with the exceptions contained in "vnpExcSet".
//
// Notes:        - We use the same unique value number both for liberal and conservative
//                 portions of the pair to save memory (it would not be useful to make
//                 them different).
//
ValueNumPair ValueNumStore::VNPUniqueWithExc(var_types type, ValueNumPair vnpExcSet)
{
#ifdef DEBUG
    VNFuncApp excSetFunc;
    assert((GetVNFunc(vnpExcSet.GetLiberal(), &excSetFunc) && (excSetFunc.m_func == VNF_ExcSetCons)) ||
           (vnpExcSet.GetLiberal() == VNForEmptyExcSet()));
    assert((GetVNFunc(vnpExcSet.GetConservative(), &excSetFunc) && (excSetFunc.m_func == VNF_ExcSetCons)) ||
           (vnpExcSet.GetConservative() == VNForEmptyExcSet()));
#endif // DEBUG

    ValueNum normVN = VNForExpr(m_pComp->compCurBB, type);

    return VNPWithExc(ValueNumPair(normVN, normVN), vnpExcSet);
}

//--------------------------------------------------------------------------------
// VNNormalValue: - Returns a Value Number that represents the result for the
//                  normal (non-exceptional) evaluation for the expression.
//
// Arguments:
//    vnp        - The Value Number Pair for the expression, including any excSet.
//                 This excSet is an optional item and represents the set of
//                 possible exceptions for the expression.
//    vnk        - The ValueNumKind either liberal or conservative
//
// Return Value:
//               - The Value Number for the expression without the exception set.
//                 This can be the original 'vn', when there are no exceptions.
//
// Notes:        - Whenever we have an exception set the Value Number will be
//                 a VN func with VNF_ValWithExc.
//                 This VN func has the normal value as m_args[0]
//
ValueNum ValueNumStore::VNNormalValue(ValueNumPair vnp, ValueNumKind vnk)
{
    return VNNormalValue(vnp.Get(vnk));
}

//--------------------------------------------------------------------------------
// VNPNormalPair: - Returns a Value Number Pair that represents the result for the
//                  normal (non-exceptional) evaluation for the expression.
//                  (see VNNormalValue for more details)
// Arguments:
//    vnp         - The Value Number Pair for the expression, including any excSet.
//
// Notes:         - This method is used to form a Value Number Pair using both
//                  the Liberal and Conservative Value Numbers normal (non-exceptional)
//
ValueNumPair ValueNumStore::VNPNormalPair(ValueNumPair vnp)
{
    return ValueNumPair(VNNormalValue(vnp.GetLiberal()), VNNormalValue(vnp.GetConservative()));
}

//---------------------------------------------------------------------------
// VNExceptionSet: - Returns a Value Number that represents the set of possible
//                   exceptions that could be encountered for the expression.
//
// Arguments:
//    vn         - The Value Number for the expression, including any excSet.
//                 This excSet is an optional item and represents the set of
//                 possible exceptions for the expression.
//
// Return Value:
//               - The Value Number for the set of exceptions of the expression.
//                 If the 'vn' has no exception set then a special Value Number
//                 representing the empty exception set is returned.
//
// Notes:        - Whenever we have an exception set the Value Number will be
//                 a VN func with VNF_ValWithExc.
//                 This VN func has the exception set as m_args[1]
//
ValueNum ValueNumStore::VNExceptionSet(ValueNum vn)
{
    VNFuncApp funcApp;
    if (GetVNFunc(vn, &funcApp) && funcApp.m_func == VNF_ValWithExc)
    {
        return funcApp.m_args[1];
    }
    else
    {
        return VNForEmptyExcSet();
    }
}

//--------------------------------------------------------------------------------
// VNPExceptionSet:    - Returns a Value Number Pair that represents the set of possible
//                 exceptions that could be encountered for the expression.
//                 (see VNExceptionSet for more details)
//
// Notes:        - This method is used to form a Value Number Pair when we
//                 want both the Liberal and Conservative Value Numbers
//
ValueNumPair ValueNumStore::VNPExceptionSet(ValueNumPair vnp)
{
    return ValueNumPair(VNExceptionSet(vnp.GetLiberal()), VNExceptionSet(vnp.GetConservative()));
}

//---------------------------------------------------------------------------
// VNWithExc:    - Returns a Value Number that also can have both a normal value
//                 as well as am exception set.
//
// Arguments:
//    vn         - The current Value Number for the expression, it may include
//                 an exception set.
//    excSet     - The Value Number representing the new exception set that
//                 is to be added to any exceptions already present in 'vn'
//
// Return Value:
//               - The new Value Number for the combination the two inputs.
//                 If the 'excSet' is the special Value Number representing
//                 the empty exception set then 'vn' is returned.
//
// Notes:        - We use a Set Union operation, 'VNExcSetUnion', to add any
//                 new exception items from  'excSet' to the existing set.
//
ValueNum ValueNumStore::VNWithExc(ValueNum vn, ValueNum excSet)
{
    if (excSet == VNForEmptyExcSet())
    {
        return vn;
    }
    else
    {
        ValueNum vnNorm;
        ValueNum vnX;
        VNUnpackExc(vn, &vnNorm, &vnX);
        return VNForFuncNoFolding(TypeOfVN(vnNorm), VNF_ValWithExc, vnNorm, VNExcSetUnion(vnX, excSet));
    }
}

//--------------------------------------------------------------------------------
// VNPWithExc:   - Returns a Value Number Pair that also can have both a normal value
//                 as well as am exception set.
//                 (see VNWithExc for more details)
//
// Notes:        = This method is used to form a Value Number Pair when we
//                 want both the Liberal and Conservative Value Numbers
//
ValueNumPair ValueNumStore::VNPWithExc(ValueNumPair vnp, ValueNumPair excSetVNP)
{
    return ValueNumPair(VNWithExc(vnp.GetLiberal(), excSetVNP.GetLiberal()),
                        VNWithExc(vnp.GetConservative(), excSetVNP.GetConservative()));
}

bool ValueNumStore::IsKnownNonNull(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }

    if (IsVNHandle(vn))
    {
        assert(CoercedConstantValue<size_t>(vn) != 0);
        return true;
    }

    VNFuncApp funcAttr;
    return GetVNFunc(vn, &funcAttr) && (s_vnfOpAttribs[funcAttr.m_func] & VNFOA_KnownNonNull) != 0;
}

bool ValueNumStore::IsSharedStatic(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }
    VNFuncApp funcAttr;
    return GetVNFunc(vn, &funcAttr) && (s_vnfOpAttribs[funcAttr.m_func] & VNFOA_SharedStatic) != 0;
}

ValueNumStore::Chunk::Chunk(CompAllocator alloc, ValueNum* pNextBaseVN, var_types typ, ChunkExtraAttribs attribs)
    : m_defs(nullptr), m_numUsed(0), m_baseVN(*pNextBaseVN), m_typ(typ), m_attribs(attribs)
{
    // Allocate "m_defs" here, according to the typ/attribs pair.
    switch (attribs)
    {
        case CEA_Const:
            switch (typ)
            {
                case TYP_INT:
                    m_defs = new (alloc) Alloc<TYP_INT>::Type[ChunkSize];
                    break;
                case TYP_FLOAT:
                    m_defs = new (alloc) Alloc<TYP_FLOAT>::Type[ChunkSize];
                    break;
                case TYP_LONG:
                    m_defs = new (alloc) Alloc<TYP_LONG>::Type[ChunkSize];
                    break;
                case TYP_DOUBLE:
                    m_defs = new (alloc) Alloc<TYP_DOUBLE>::Type[ChunkSize];
                    break;
                case TYP_BYREF:
                    m_defs = new (alloc) Alloc<TYP_BYREF>::Type[ChunkSize];
                    break;
                case TYP_REF:
                    // We allocate space for a single REF constant, NULL, so we can access these values uniformly.
                    // Since this value is always the same, we represent it as a static.
                    m_defs = &s_specialRefConsts[0];
                    break; // Nothing to do.

#if defined(FEATURE_SIMD)
                case TYP_SIMD8:
                {
                    m_defs = new (alloc) Alloc<TYP_SIMD8>::Type[ChunkSize];
                    break;
                }

                case TYP_SIMD12:
                {
                    m_defs = new (alloc) Alloc<TYP_SIMD12>::Type[ChunkSize];
                    break;
                }

                case TYP_SIMD16:
                {
                    m_defs = new (alloc) Alloc<TYP_SIMD16>::Type[ChunkSize];
                    break;
                }

#if defined(TARGET_XARCH)
                case TYP_SIMD32:
                {
                    m_defs = new (alloc) Alloc<TYP_SIMD32>::Type[ChunkSize];
                    break;
                }

                case TYP_SIMD64:
                {
                    m_defs = new (alloc) Alloc<TYP_SIMD64>::Type[ChunkSize];
                    break;
                }
#endif // TARGET_XARCH
#endif // FEATURE_SIMD

                default:
                    assert(false); // Should not reach here.
            }
            break;

        case CEA_Handle:
            m_defs = new (alloc) VNHandle[ChunkSize];
            break;

        case CEA_Func0:
            m_defs = new (alloc) VNFunc[ChunkSize];
            break;

        case CEA_Func1:
            m_defs = alloc.allocate<char>((sizeof(VNDefFuncAppFlexible) + sizeof(ValueNum) * 1) * ChunkSize);
            break;
        case CEA_Func2:
            m_defs = alloc.allocate<char>((sizeof(VNDefFuncAppFlexible) + sizeof(ValueNum) * 2) * ChunkSize);
            break;
        case CEA_Func3:
            m_defs = alloc.allocate<char>((sizeof(VNDefFuncAppFlexible) + sizeof(ValueNum) * 3) * ChunkSize);
            break;
        case CEA_Func4:
            m_defs = alloc.allocate<char>((sizeof(VNDefFuncAppFlexible) + sizeof(ValueNum) * 4) * ChunkSize);
            break;
        default:
            unreached();
    }
    *pNextBaseVN += ChunkSize;
}

ValueNumStore::Chunk* ValueNumStore::GetAllocChunk(var_types typ, ChunkExtraAttribs attribs)
{
    Chunk*   res;
    unsigned index = attribs;
    ChunkNum cn    = m_curAllocChunk[typ][index];
    if (cn != NoChunk)
    {
        res = m_chunks.Get(cn);
        if (res->m_numUsed < ChunkSize)
        {
            return res;
        }
    }
    // Otherwise, must allocate a new one.
    res                         = new (m_alloc) Chunk(m_alloc, &m_nextChunkBase, typ, attribs);
    cn                          = m_chunks.Push(res);
    m_curAllocChunk[typ][index] = cn;
    return res;
}

//------------------------------------------------------------------------
// VnForConst: Return value number for a constant.
//
// Arguments:
//   cnsVal - `T` constant to return a VN for;
//   numMap - VNMap<T> map where `T` type constants should be stored;
//   varType - jit type for the `T`: TYP_INT for int, TYP_LONG for long etc.
//
// Return value:
//    value number for the given constant.
//
// Notes:
//   First try to find an existing VN for `cnsVal` in `numMap`,
//   if it fails then allocate a new `varType` chunk and return that.
//
template <typename T, typename NumMap>
ValueNum ValueNumStore::VnForConst(T cnsVal, NumMap* numMap, var_types varType)
{
    ValueNum res;
    if (numMap->Lookup(cnsVal, &res))
    {
        return res;
    }
    else
    {
        Chunk*   chunk               = GetAllocChunk(varType, CEA_Const);
        unsigned offsetWithinChunk   = chunk->AllocVN();
        res                          = chunk->m_baseVN + offsetWithinChunk;
        T* chunkDefs                 = reinterpret_cast<T*>(chunk->m_defs);
        chunkDefs[offsetWithinChunk] = cnsVal;
        numMap->Set(cnsVal, res);
        return res;
    }
}

ValueNum ValueNumStore::VNForIntCon(INT32 cnsVal)
{
    if (IsSmallIntConst(cnsVal))
    {
        unsigned ind = cnsVal - SmallIntConstMin;
        ValueNum vn  = m_VNsForSmallIntConsts[ind];
        if (vn != NoVN)
        {
            return vn;
        }
        vn                          = VnForConst(cnsVal, GetIntCnsMap(), TYP_INT);
        m_VNsForSmallIntConsts[ind] = vn;
        return vn;
    }
    else
    {
        return VnForConst(cnsVal, GetIntCnsMap(), TYP_INT);
    }
}

ValueNum ValueNumStore::VNForIntPtrCon(ssize_t cnsVal)
{
#ifdef HOST_64BIT
    return VNForLongCon(cnsVal);
#else  // !HOST_64BIT
    return VNForIntCon(cnsVal);
#endif // !HOST_64BIT
}

ValueNum ValueNumStore::VNForLongCon(INT64 cnsVal)
{
    return VnForConst(cnsVal, GetLongCnsMap(), TYP_LONG);
}

ValueNum ValueNumStore::VNForFloatCon(float cnsVal)
{
    return VnForConst(cnsVal, GetFloatCnsMap(), TYP_FLOAT);
}

ValueNum ValueNumStore::VNForDoubleCon(double cnsVal)
{
    return VnForConst(cnsVal, GetDoubleCnsMap(), TYP_DOUBLE);
}

ValueNum ValueNumStore::VNForByrefCon(target_size_t cnsVal)
{
    return VnForConst(cnsVal, GetByrefCnsMap(), TYP_BYREF);
}

#if defined(FEATURE_SIMD)
ValueNum ValueNumStore::VNForSimd8Con(simd8_t cnsVal)
{
    return VnForConst(cnsVal, GetSimd8CnsMap(), TYP_SIMD8);
}

ValueNum ValueNumStore::VNForSimd12Con(simd12_t cnsVal)
{
    return VnForConst(cnsVal, GetSimd12CnsMap(), TYP_SIMD12);
}

ValueNum ValueNumStore::VNForSimd16Con(simd16_t cnsVal)
{
    return VnForConst(cnsVal, GetSimd16CnsMap(), TYP_SIMD16);
}

#if defined(TARGET_XARCH)
ValueNum ValueNumStore::VNForSimd32Con(simd32_t cnsVal)
{
    return VnForConst(cnsVal, GetSimd32CnsMap(), TYP_SIMD32);
}

ValueNum ValueNumStore::VNForSimd64Con(simd64_t cnsVal)
{
    return VnForConst(cnsVal, GetSimd64CnsMap(), TYP_SIMD64);
}
#endif // TARGET_XARCH
#endif // FEATURE_SIMD

ValueNum ValueNumStore::VNForGenericCon(var_types typ, uint8_t* cnsVal)
{
    // For now we only support these primitives, we can extend this list to FP, SIMD and structs in future.
    switch (typ)
    {
#define READ_VALUE(typ)                                                                                                \
    typ val = {};                                                                                                      \
    memcpy(&val, cnsVal, sizeof(typ));

        case TYP_UBYTE:
        {
            READ_VALUE(uint8_t);
            return VNForIntCon(val);
        }
        case TYP_BYTE:
        {
            READ_VALUE(int8_t);
            return VNForIntCon(val);
        }
        case TYP_SHORT:
        {
            READ_VALUE(int16_t);
            return VNForIntCon(val);
        }
        case TYP_USHORT:
        {
            READ_VALUE(uint16_t);
            return VNForIntCon(val);
        }
        case TYP_INT:
        {
            READ_VALUE(int32_t);
            return VNForIntCon(val);
        }
        case TYP_UINT:
        {
            READ_VALUE(uint32_t);
            return VNForIntCon(val);
        }
        case TYP_LONG:
        {
            READ_VALUE(int64_t);
            return VNForLongCon(val);
        }
        case TYP_ULONG:
        {
            READ_VALUE(uint64_t);
            return VNForLongCon(val);
        }
        case TYP_FLOAT:
        {
            READ_VALUE(float);
            return VNForFloatCon(val);
        }
        case TYP_DOUBLE:
        {
            READ_VALUE(double);
            return VNForDoubleCon(val);
        }
        case TYP_REF:
        {
            READ_VALUE(ssize_t);
            if (val == 0)
            {
                return VNForNull();
            }
            else
            {
                return VNForHandle(val, GTF_ICON_OBJ_HDL);
            }
        }
#if defined(FEATURE_SIMD)
        case TYP_SIMD8:
        {
            READ_VALUE(simd8_t);
            return VNForSimd8Con(val);
        }
        case TYP_SIMD12:
        {
            READ_VALUE(simd12_t);
            return VNForSimd12Con(val);
        }
        case TYP_SIMD16:
        {
            READ_VALUE(simd16_t);
            return VNForSimd16Con(val);
        }
#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        {
            READ_VALUE(simd32_t);
            return VNForSimd32Con(val);
        }
        case TYP_SIMD64:
        {
            READ_VALUE(simd64_t);
            return VNForSimd64Con(val);
        }
#endif // TARGET_XARCH
#endif // FEATURE_SIMD
        default:
            unreached();
            break;

#undef READ_VALUE
    }
}

ValueNum ValueNumStore::VNForCastOper(var_types castToType, bool srcIsUnsigned)
{
    assert(castToType != TYP_STRUCT);
    INT32 cnsVal = INT32(castToType) << INT32(VCA_BitCount);
    assert((cnsVal & INT32(VCA_ReservedBits)) == 0);

    if (srcIsUnsigned)
    {
        // We record the srcIsUnsigned by or-ing a 0x01
        cnsVal |= INT32(VCA_UnsignedSrc);
    }
    ValueNum result = VNForIntCon(cnsVal);

    return result;
}

void ValueNumStore::GetCastOperFromVN(ValueNum vn, var_types* pCastToType, bool* pSrcIsUnsigned)
{
    assert(pCastToType != nullptr);
    assert(pSrcIsUnsigned != nullptr);
    assert(IsVNInt32Constant(vn));

    int value = GetConstantInt32(vn);
    assert(value >= 0);

    *pSrcIsUnsigned = (value & INT32(VCA_UnsignedSrc)) != 0;
    *pCastToType    = var_types(value >> INT32(VCA_BitCount));

    assert(VNForCastOper(*pCastToType, *pSrcIsUnsigned) == vn);
}

ValueNum ValueNumStore::VNForHandle(ssize_t cnsVal, GenTreeFlags handleFlags)
{
    assert((handleFlags & ~GTF_ICON_HDL_MASK) == 0);

    ValueNum res;
    VNHandle handle;
    VNHandle::Initialize(&handle, cnsVal, handleFlags);
    if (GetHandleMap()->Lookup(handle, &res))
    {
        return res;
    }
    else
    {
        Chunk* const    c                 = GetAllocChunk(TYP_I_IMPL, CEA_Handle);
        unsigned const  offsetWithinChunk = c->AllocVN();
        VNHandle* const chunkSlots        = reinterpret_cast<VNHandle*>(c->m_defs);

        chunkSlots[offsetWithinChunk] = handle;
        res                           = c->m_baseVN + offsetWithinChunk;

        GetHandleMap()->Set(handle, res);
        return res;
    }
}

ValueNum ValueNumStore::VNZeroForType(var_types typ)
{
    switch (typ)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_SHORT:
        case TYP_USHORT:
        case TYP_INT:
        case TYP_UINT:
            return VNForIntCon(0);
        case TYP_LONG:
        case TYP_ULONG:
            return VNForLongCon(0);
        case TYP_FLOAT:
            return VNForFloatCon(0.0f);
        case TYP_DOUBLE:
            return VNForDoubleCon(0.0);
        case TYP_REF:
            return VNForNull();
        case TYP_BYREF:
            return VNForByrefCon(0);

#ifdef FEATURE_SIMD
        case TYP_SIMD8:
        {
            return VNForSimd8Con(simd8_t::Zero());
        }

        case TYP_SIMD12:
        {
            return VNForSimd12Con(simd12_t::Zero());
        }

        case TYP_SIMD16:
        {
            return VNForSimd16Con(simd16_t::Zero());
        }

#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        {
            return VNForSimd32Con(simd32_t::Zero());
        }

        case TYP_SIMD64:
        {
            return VNForSimd64Con(simd64_t::Zero());
        }
#endif // TARGET_XARCH
#endif // FEATURE_SIMD

        // These should be unreached.
        default:
            unreached(); // Should handle all types.
    }
}

ValueNum ValueNumStore::VNForZeroObj(ClassLayout* layout)
{
    assert(layout != nullptr);

    ValueNum layoutVN  = VNForIntPtrCon(reinterpret_cast<ssize_t>(layout));
    ValueNum zeroObjVN = VNForFunc(TYP_STRUCT, VNF_ZeroObj, layoutVN);

    return zeroObjVN;
}

// Returns the value number for one of the given "typ".
// It returns NoVN for a "typ" that has no one value, such as TYP_REF.
ValueNum ValueNumStore::VNOneForType(var_types typ)
{
    switch (typ)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_SHORT:
        case TYP_USHORT:
        case TYP_INT:
        case TYP_UINT:
            return VNForIntCon(1);
        case TYP_LONG:
        case TYP_ULONG:
            return VNForLongCon(1);
        case TYP_FLOAT:
            return VNForFloatCon(1.0f);
        case TYP_DOUBLE:
            return VNForDoubleCon(1.0);

        default:
        {
            assert(!varTypeIsSIMD(typ));
            return NoVN;
        }
    }
}

ValueNum ValueNumStore::VNAllBitsForType(var_types typ)
{
    switch (typ)
    {
        case TYP_INT:
        case TYP_UINT:
        {
            return VNForIntCon(0xFFFFFFFF);
        }

        case TYP_LONG:
        case TYP_ULONG:
        {
            return VNForLongCon(0xFFFFFFFFFFFFFFFF);
        }

#ifdef FEATURE_SIMD
        case TYP_SIMD8:
        {
            return VNForSimd8Con(simd8_t::AllBitsSet());
        }

        case TYP_SIMD12:
        {
            return VNForSimd12Con(simd12_t::AllBitsSet());
        }

        case TYP_SIMD16:
        {
            return VNForSimd16Con(simd16_t::AllBitsSet());
        }

#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        {
            return VNForSimd32Con(simd32_t::AllBitsSet());
        }

        case TYP_SIMD64:
        {
            return VNForSimd64Con(simd64_t::AllBitsSet());
        }
#endif // TARGET_XARCH
#endif // FEATURE_SIMD

        default:
        {
            return NoVN;
        }
    }
}

#ifdef FEATURE_SIMD
ValueNum ValueNumStore::VNOneForSimdType(var_types simdType, var_types simdBaseType)
{
    assert(varTypeIsSIMD(simdType));

    simd_t simdVal  = {};
    int    simdSize = genTypeSize(simdType);

    switch (simdBaseType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        {
            for (int i = 0; i < simdSize; i++)
            {
                simdVal.u8[i] = 1;
            }
            break;
        }

        case TYP_SHORT:
        case TYP_USHORT:
        {
            for (int i = 0; i < (simdSize / 2); i++)
            {
                simdVal.u16[i] = 1;
            }
            break;
        }

        case TYP_INT:
        case TYP_UINT:
        {
            for (int i = 0; i < (simdSize / 4); i++)
            {
                simdVal.u32[i] = 1;
            }
            break;
        }

        case TYP_LONG:
        case TYP_ULONG:
        {
            for (int i = 0; i < (simdSize / 8); i++)
            {
                simdVal.u64[i] = 1;
            }
            break;
        }

        case TYP_FLOAT:
        {
            for (int i = 0; i < (simdSize / 4); i++)
            {
                simdVal.f32[i] = 1.0f;
            }
            break;
        }

        case TYP_DOUBLE:
        {
            for (int i = 0; i < (simdSize / 8); i++)
            {
                simdVal.f64[i] = 1.0;
            }
            break;
        }

        default:
        {
            unreached();
        }
    }

    switch (simdType)
    {
        case TYP_SIMD8:
        {
            simd8_t simd8Val;
            memcpy(&simd8Val, &simdVal, sizeof(simd8_t));
            return VNForSimd8Con(simd8Val);
        }

        case TYP_SIMD12:
        {
            simd12_t simd12Val;
            memcpy(&simd12Val, &simdVal, sizeof(simd12_t));
            return VNForSimd12Con(simd12Val);
        }

        case TYP_SIMD16:
        {
            simd16_t simd16Val;
            memcpy(&simd16Val, &simdVal, sizeof(simd16_t));
            return VNForSimd16Con(simd16Val);
        }

#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        {
            simd32_t simd32Val;
            memcpy(&simd32Val, &simdVal, sizeof(simd32_t));
            return VNForSimd32Con(simd32Val);
        }

        case TYP_SIMD64:
        {
            simd64_t simd64Val;
            memcpy(&simd64Val, &simdVal, sizeof(simd64_t));
            return VNForSimd64Con(simd64Val);
        }
#endif // TARGET_XARCH

        default:
        {
            unreached();
        }
    }
}

ValueNum ValueNumStore::VNForSimdType(unsigned simdSize, CorInfoType simdBaseJitType)
{
    ValueNum baseTypeVN = VNForIntCon(INT32(simdBaseJitType));
    ValueNum sizeVN     = VNForIntCon(simdSize);
    ValueNum simdTypeVN = VNForFunc(TYP_REF, VNF_SimdType, sizeVN, baseTypeVN);

    return simdTypeVN;
}
#endif // FEATURE_SIMD

class Object* ValueNumStore::s_specialRefConsts[] = {nullptr, nullptr, nullptr};

//----------------------------------------------------------------------------------------
//  VNForFunc  - Returns the ValueNum associated with 'func'
//               There is a one-to-one relationship between the ValueNum and 'func'
//
// Arguments:
//    typ            - The type of the resulting ValueNum produced by 'func'
//    func           - Any nullary VNFunc
//
// Return Value:     - Returns the ValueNum associated with 'func'
//
// Note: - This method only handles Nullary operators (i.e., symbolic constants).
//
ValueNum ValueNumStore::VNForFunc(var_types typ, VNFunc func)
{
    assert(VNFuncArity(func) == 0);

    ValueNum resultVN;

    // Have we already assigned a ValueNum for 'func' ?
    //
    if (!GetVNFunc0Map()->Lookup(func, &resultVN))
    {
        // Allocate a new ValueNum for 'func'
        Chunk* const   c                 = GetAllocChunk(typ, CEA_Func0);
        unsigned const offsetWithinChunk = c->AllocVN();
        VNFunc* const  chunkSlots        = reinterpret_cast<VNFunc*>(c->m_defs);

        chunkSlots[offsetWithinChunk] = func;
        resultVN                      = c->m_baseVN + offsetWithinChunk;
        GetVNFunc0Map()->Set(func, resultVN);
    }
    return resultVN;
}

//----------------------------------------------------------------------------------------
//  VNForFunc  - Returns the ValueNum associated with 'func'('arg0VN')
//               There is a one-to-one relationship between the ValueNum
//               and 'func'('arg0VN')
//
// Arguments:
//    typ            - The type of the resulting ValueNum produced by 'func'
//    func           - Any unary VNFunc
//    arg0VN         - The ValueNum of the argument to 'func'
//
// Return Value:     - Returns the ValueNum associated with 'func'('arg0VN')
//
// Note: - This method only handles Unary operators
//
ValueNum ValueNumStore::VNForFunc(var_types typ, VNFunc func, ValueNum arg0VN)
{
    assert(func != VNF_MemOpaque);
    assert(arg0VN == VNNormalValue(arg0VN)); // Arguments don't carry exceptions.

    ValueNum resultVN = NoVN;

    // Have we already assigned a ValueNum for 'func'('arg0VN') ?
    //
    VNDefFuncApp<1> fstruct(func, arg0VN);
    if (GetVNFunc1Map()->Lookup(fstruct, &resultVN))
    {
        assert(resultVN != NoVN);
    }
    else
    {
        // Check if we can fold GT_ARR_LENGTH on top of a known array (immutable)
        if (func == VNFunc(GT_ARR_LENGTH))
        {
            // Case 1: ARR_LENGTH(FROZEN_OBJ)
            ValueNum addressVN = VNNormalValue(arg0VN);
            if (IsVNObjHandle(addressVN))
            {
                size_t handle = CoercedConstantValue<size_t>(addressVN);
                int    len    = m_pComp->info.compCompHnd->getArrayOrStringLength((CORINFO_OBJECT_HANDLE)handle);
                if (len >= 0)
                {
                    resultVN = VNForIntCon(len);
                }
            }

            // Case 2: ARR_LENGTH(static-readonly-field)
            VNFuncApp funcApp;
            if ((resultVN == NoVN) && GetVNFunc(addressVN, &funcApp) && (funcApp.m_func == VNF_InvariantNonNullLoad))
            {
                ValueNum fieldSeqVN = VNNormalValue(funcApp.m_args[0]);
                if (IsVNHandle(fieldSeqVN) && (GetHandleFlags(fieldSeqVN) == GTF_ICON_FIELD_SEQ))
                {
                    FieldSeq* fieldSeq = FieldSeqVNToFieldSeq(fieldSeqVN);
                    if (fieldSeq != nullptr)
                    {
                        CORINFO_FIELD_HANDLE field = fieldSeq->GetFieldHandle();
                        if (field != NULL)
                        {
                            uint8_t buffer[TARGET_POINTER_SIZE] = {0};
                            if (m_pComp->info.compCompHnd->getStaticFieldContent(field, buffer, TARGET_POINTER_SIZE, 0,
                                                                                 false))
                            {
                                // In case of 64bit jit emitting 32bit codegen this handle will be 64bit
                                // value holding 32bit handle with upper half zeroed (hence, "= NULL").
                                // It's done to match the current crossgen/ILC behavior.
                                CORINFO_OBJECT_HANDLE objHandle = NULL;
                                memcpy(&objHandle, buffer, TARGET_POINTER_SIZE);
                                int len = m_pComp->info.compCompHnd->getArrayOrStringLength(objHandle);
                                if (len >= 0)
                                {
                                    resultVN = VNForIntCon(len);
                                }
                            }
                        }
                    }
                }
            }

            // Case 3: ARR_LENGTH(new T[cns])
            // TODO: Add support for MD arrays
            int knownSize;
            if ((resultVN == NoVN) && TryGetNewArrSize(addressVN, &knownSize))
            {
                resultVN = VNForIntCon(knownSize);
            }
        }

        // Try to perform constant-folding.
        //
        if ((resultVN == NoVN) && VNEvalCanFoldUnaryFunc(typ, func, arg0VN))
        {
            resultVN = EvalFuncForConstantArgs(typ, func, arg0VN);
        }

        // Otherwise, Allocate a new ValueNum for 'func'('arg0VN')
        //
        if (resultVN == NoVN)
        {
            Chunk* const          c                 = GetAllocChunk(typ, CEA_Func1);
            unsigned const        offsetWithinChunk = c->AllocVN();
            VNDefFuncAppFlexible* fapp              = c->PointerToFuncApp(offsetWithinChunk, 1);
            fapp->m_func                            = func;
            fapp->m_args[0]                         = arg0VN;
            resultVN                                = c->m_baseVN + offsetWithinChunk;
        }

        // Record 'resultVN' in the Func1Map
        //
        GetVNFunc1Map()->Set(fstruct, resultVN);
    }
    return resultVN;
}

//----------------------------------------------------------------------------------------
//  VNForFunc  - Returns the ValueNum associated with 'func'('arg0VN','arg1VN')
//               There is a one-to-one relationship between the ValueNum
//               and 'func'('arg0VN','arg1VN')
//
// Arguments:
//    typ            - The type of the resulting ValueNum produced by 'func'
//    func           - Any binary VNFunc
//    arg0VN         - The ValueNum of the first argument to 'func'
//    arg1VN         - The ValueNum of the second argument to 'func'
//
// Return Value:     - Returns the ValueNum associated with 'func'('arg0VN','arg1VN')
//
// Note: - This method only handles Binary operators
//
ValueNum ValueNumStore::VNForFunc(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(arg0VN != NoVN && arg1VN != NoVN);
    assert(arg0VN == VNNormalValue(arg0VN)); // Arguments carry no exceptions.
    assert(arg1VN == VNNormalValue(arg1VN)); // Arguments carry no exceptions.

    // Some SIMD functions with variable number of arguments are defined with zero arity
    assert((VNFuncArity(func) == 0) || (VNFuncArity(func) == 2));
    assert(func != VNF_MapSelect); // Precondition: use the special function VNForMapSelect defined for that.

    ValueNum resultVN = NoVN;

    // Even if the argVNs differ, if both operands runtime types constructed from handles,
    // we can sometimes also fold.
    //
    // The case where the arg VNs are equal is handled by EvalUsingMathIdentity below.
    // This is the VN analog of gtFoldTypeCompare.
    //
    const genTreeOps oper = genTreeOps(func);
    if ((arg0VN != arg1VN) && GenTree::StaticOperIs(oper, GT_EQ, GT_NE))
    {
        resultVN = VNEvalFoldTypeCompare(typ, func, arg0VN, arg1VN);
        if (resultVN != NoVN)
        {
            return resultVN;
        }
    }

    // We canonicalize commutative operations.
    // (Perhaps should eventually handle associative/commutative [AC] ops -- but that gets complicated...)
    if (VNFuncIsCommutative(func))
    {
        // Order arg0 arg1 by numerical VN value.
        if (arg0VN > arg1VN)
        {
            std::swap(arg0VN, arg1VN);
        }
    }

    // Have we already assigned a ValueNum for 'func'('arg0VN','arg1VN') ?
    //
    VNDefFuncApp<2> fstruct(func, arg0VN, arg1VN);
    if (GetVNFunc2Map()->Lookup(fstruct, &resultVN))
    {
        assert(resultVN != NoVN);
    }
    else
    {
        if (func == VNF_CastClass)
        {
            // In terms of values, a castclass always returns its second argument, the object being cast.
            // The operation may also throw an exception
            ValueNum vnExcSet = VNExcSetSingleton(VNForFuncNoFolding(TYP_REF, VNF_InvalidCastExc, arg1VN, arg0VN));
            resultVN          = VNWithExc(arg1VN, vnExcSet);
        }
        else
        {
            // When both operands are constants we can usually perform constant-folding,
            // except if the expression will always throw an exception (constant VN-based
            // propagation depends on that).
            //
            bool folded = false;
            if (VNEvalCanFoldBinaryFunc(typ, func, arg0VN, arg1VN) && VNEvalShouldFold(typ, func, arg0VN, arg1VN))
            {
                resultVN = EvalFuncForConstantArgs(typ, func, arg0VN, arg1VN);
            }

            if (resultVN != NoVN)
            {
                folded = true;
            }
            else
            {
                resultVN = EvalUsingMathIdentity(typ, func, arg0VN, arg1VN);
            }

            // Do we have a valid resultVN?
            if ((resultVN == NoVN) || (!folded && (genActualType(TypeOfVN(resultVN)) != genActualType(typ))))
            {
                // Otherwise, Allocate a new ValueNum for 'func'('arg0VN','arg1VN')
                //
                Chunk* const          c                 = GetAllocChunk(typ, CEA_Func2);
                unsigned const        offsetWithinChunk = c->AllocVN();
                VNDefFuncAppFlexible* fapp              = c->PointerToFuncApp(offsetWithinChunk, 2);
                fapp->m_func                            = func;
                fapp->m_args[0]                         = arg0VN;
                fapp->m_args[1]                         = arg1VN;
                resultVN                                = c->m_baseVN + offsetWithinChunk;
            }
        }

        // Record 'resultVN' in the Func2Map
        GetVNFunc2Map()->Set(fstruct, resultVN);
    }
    return resultVN;
}

//----------------------------------------------------------------------------------------
//  VNForFuncNoFolding  - Returns the ValueNum associated with
//                        'func'('arg0VN','arg1VN') without doing any folding.
//
// Arguments:
//    typ            - The type of the resulting ValueNum produced by 'func'
//    func           - Any binary VNFunc
//    arg0VN         - The ValueNum of the first argument to 'func'
//    arg1VN         - The ValueNum of the second argument to 'func'
//
// Return Value:     - Returns the ValueNum associated with 'func'('arg0VN','arg1VN')
//
ValueNum ValueNumStore::VNForFuncNoFolding(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(arg0VN != NoVN && arg1VN != NoVN);

    // Function arguments carry no exceptions.
    assert(arg0VN == VNNormalValue(arg0VN));
    assert(arg1VN == VNNormalValue(arg1VN));
    assert(VNFuncArity(func) == 2);

    ValueNum resultVN;

    // Have we already assigned a ValueNum for 'func'('arg0VN','arg1VN') ?
    //
    VNDefFuncApp<2> fstruct(func, arg0VN, arg1VN);
    if (!GetVNFunc2Map()->Lookup(fstruct, &resultVN))
    {
        // Otherwise, Allocate a new ValueNum for 'func'('arg0VN','arg1VN')
        //
        Chunk* const          c                 = GetAllocChunk(typ, CEA_Func2);
        unsigned const        offsetWithinChunk = c->AllocVN();
        VNDefFuncAppFlexible* fapp              = c->PointerToFuncApp(offsetWithinChunk, 2);
        fapp->m_func                            = func;
        fapp->m_args[0]                         = arg0VN;
        fapp->m_args[1]                         = arg1VN;
        resultVN                                = c->m_baseVN + offsetWithinChunk;

        // Record 'resultVN' in the Func2Map
        GetVNFunc2Map()->Set(fstruct, resultVN);
    }

    return resultVN;
}

//----------------------------------------------------------------------------------------
//  VNForFunc  - Returns the ValueNum associated with 'func'('arg0VN','arg1VN','arg2VN')
//               There is a one-to-one relationship between the ValueNum
//               and 'func'('arg0VN','arg1VN','arg2VN')
//
// Arguments:
//    typ            - The type of the resulting ValueNum produced by 'func'
//    func           - Any binary VNFunc
//    arg0VN         - The ValueNum of the first argument to 'func'
//    arg1VN         - The ValueNum of the second argument to 'func'
//    arg2VN         - The ValueNum of the third argument to 'func'
//
// Return Value:     - Returns the ValueNum associated with 'func'('arg0VN','arg1VN','arg1VN)
//
// Note: - This method only handles Trinary operations
//         We have to special case VNF_PhiDef, as it's first two arguments are not ValueNums
//
ValueNum ValueNumStore::VNForFunc(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN, ValueNum arg2VN)
{
    assert(arg0VN != NoVN);
    assert(arg1VN != NoVN);
    assert(arg2VN != NoVN);
    // Some SIMD functions with variable number of arguments are defined with zero arity
    assert((VNFuncArity(func) == 0) || (VNFuncArity(func) == 3));

#ifdef DEBUG
    // Function arguments carry no exceptions.
    //
    if (func != VNF_PhiDef)
    {
        // For a phi definition first and second argument are "plain" local/ssa numbers.
        // (I don't know if having such non-VN arguments to a VN function is a good idea -- if we wanted to declare
        // ValueNum to be "short" it would be a problem, for example.  But we'll leave it for now, with these explicit
        // exceptions.)
        assert(arg0VN == VNNormalValue(arg0VN));
        assert(arg1VN == VNNormalValue(arg1VN));
    }
    assert(arg2VN == VNNormalValue(arg2VN));
#endif

    ValueNum resultVN;

    // Have we already assigned a ValueNum for 'func'('arg0VN','arg1VN','arg2VN') ?
    //
    VNDefFuncApp<3> fstruct(func, arg0VN, arg1VN, arg2VN);
    if (!GetVNFunc3Map()->Lookup(fstruct, &resultVN))
    {
        // Otherwise, Allocate a new ValueNum for 'func'('arg0VN','arg1VN','arg2VN')
        //
        Chunk* const          c                 = GetAllocChunk(typ, CEA_Func3);
        unsigned const        offsetWithinChunk = c->AllocVN();
        VNDefFuncAppFlexible* fapp              = c->PointerToFuncApp(offsetWithinChunk, 3);
        fapp->m_func                            = func;
        fapp->m_args[0]                         = arg0VN;
        fapp->m_args[1]                         = arg1VN;
        fapp->m_args[2]                         = arg2VN;
        resultVN                                = c->m_baseVN + offsetWithinChunk;

        // Record 'resultVN' in the Func3Map
        GetVNFunc3Map()->Set(fstruct, resultVN);
    }
    return resultVN;
}

// ----------------------------------------------------------------------------------------
//  VNForFunc  - Returns the ValueNum associated with 'func'('arg0VN','arg1VN','arg2VN','arg3VN')
//               There is a one-to-one relationship between the ValueNum
//               and 'func'('arg0VN','arg1VN','arg2VN','arg3VN')
//
// Arguments:
//    typ            - The type of the resulting ValueNum produced by 'func'
//    func           - Any binary VNFunc
//    arg0VN         - The ValueNum of the first argument to 'func'
//    arg1VN         - The ValueNum of the second argument to 'func'
//    arg2VN         - The ValueNum of the third argument to 'func'
//    arg3VN         - The ValueNum of the fourth argument to 'func'
//
// Return Value:     - Returns the ValueNum associated with 'func'('arg0VN','arg1VN','arg2VN','arg3VN')
//
// Note:   Currently the only four operand funcs are VNF_PtrToArrElem and VNF_MapStore.
//
ValueNum ValueNumStore::VNForFunc(
    var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN, ValueNum arg2VN, ValueNum arg3VN)
{
    assert(arg0VN != NoVN && arg1VN != NoVN && arg2VN != NoVN && ((arg3VN != NoVN) || func == VNF_MapStore));

    // Function arguments carry no exceptions.
    assert(arg0VN == VNNormalValue(arg0VN));
    assert(arg1VN == VNNormalValue(arg1VN));
    assert(arg2VN == VNNormalValue(arg2VN));
    assert((func == VNF_MapStore) || (arg3VN == VNNormalValue(arg3VN)));
    assert(VNFuncArity(func) == 4);

    ValueNum resultVN;

    // Have we already assigned a ValueNum for 'func'('arg0VN','arg1VN','arg2VN','arg3VN') ?
    //
    VNDefFuncApp<4> fstruct(func, arg0VN, arg1VN, arg2VN, arg3VN);
    if (!GetVNFunc4Map()->Lookup(fstruct, &resultVN))
    {
        // Otherwise, Allocate a new ValueNum for 'func'('arg0VN','arg1VN','arg2VN','arg3VN')
        //
        Chunk* const          c                 = GetAllocChunk(typ, CEA_Func4);
        unsigned const        offsetWithinChunk = c->AllocVN();
        VNDefFuncAppFlexible* fapp              = c->PointerToFuncApp(offsetWithinChunk, 4);
        fapp->m_func                            = func;
        fapp->m_args[0]                         = arg0VN;
        fapp->m_args[1]                         = arg1VN;
        fapp->m_args[2]                         = arg2VN;
        fapp->m_args[3]                         = arg3VN;
        resultVN                                = c->m_baseVN + offsetWithinChunk;

        // Record 'resultVN' in the Func4Map
        GetVNFunc4Map()->Set(fstruct, resultVN);
    }
    return resultVN;
}

//------------------------------------------------------------------------------
// VNForMapStore: Create the VN for a precise store (to a precise map).
//
// Arguments:
//    map   - (VN of) the precise map
//    index - Index value number
//    value - New value for map[index]
//
// Return Value:
//    Value number for "map" with "map[index]" set to "value".
//
ValueNum ValueNumStore::VNForMapStore(ValueNum map, ValueNum index, ValueNum value)
{
    assert(MapIsPrecise(map));

    BasicBlock* const     bb        = m_pComp->compCurBB;
    FlowGraphNaturalLoop* bbLoop    = m_pComp->m_blockToLoop->GetLoop(bb);
    unsigned              loopIndex = bbLoop == nullptr ? UINT_MAX : bbLoop->GetIndex();

    ValueNum const result = VNForFunc(TypeOfVN(map), VNF_MapStore, map, index, value, loopIndex);

#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("    VNForMapStore(" FMT_VN ", " FMT_VN ", " FMT_VN "):%s in " FMT_BB " returns ", map, index, value,
               VNMapTypeName(TypeOfVN(result)), bb->bbNum);
        m_pComp->vnPrint(result, 1);
        printf("\n");
    }
#endif
    return result;
}

//------------------------------------------------------------------------------
// VNForMapPhysicalStore: Create the VN for a physical store (to a physical map).
//
// Arguments:
//    map    - (VN of) the physical map
//    offset - The store offset
//    size   - Size of "value" (in bytes)
//    value  - The value being stored
//
// Return Value:
//    Value number for "map" with "map[offset:offset + size - 1]" set to "value".
//
ValueNum ValueNumStore::VNForMapPhysicalStore(ValueNum map, unsigned offset, unsigned size, ValueNum value)
{
    assert(MapIsPhysical(map));

    ValueNum selector = EncodePhysicalSelector(offset, size);
    ValueNum result   = VNForFunc(TypeOfVN(map), VNF_MapPhysicalStore, map, selector, value);

    JITDUMP("    VNForMapPhysicalStore:%s returns ", VNMapTypeName(TypeOfVN(result)));
    JITDUMPEXEC(m_pComp->vnPrint(result, 1));
    JITDUMP("\n");

    return result;
}

//------------------------------------------------------------------------------
// VNForMapSelect: Select value from a precise map.
//
// Arguments:
//    vnk   - Value number kind (see "VNForMapSelectWork" notes)
//    type  - The type to select
//    map   - Map value number
//    index - Index value number
//
// Return Value:
//    Value number corresponding to "map[index]".
//
ValueNum ValueNumStore::VNForMapSelect(ValueNumKind vnk, var_types type, ValueNum map, ValueNum index)
{
    assert(MapIsPrecise(map));

    ValueNum result = VNForMapSelectInner(vnk, type, map, index);

    JITDUMP("    VNForMapSelect(" FMT_VN ", " FMT_VN "):%s returns ", map, index, VNMapTypeName(type));
    JITDUMPEXEC(m_pComp->vnPrint(result, 1));
    JITDUMP("\n");

    return result;
}

//------------------------------------------------------------------------------
// VNForMapPhysicalSelect: Select value from a physical map.
//
// Arguments:
//    vnk    - Value number kind (see the notes for "VNForMapSelect")
//    type   - The type to select
//    map    - (VN of) the physical map
//    offset - Offset to select from
//    size   - Size to select
//
// Return Value:
//    Value number corresponding to "map[offset:offset + size - 1]". The value
//    number returned can be of a type different from "type", as physical maps
//    do not have canonical types, only sizes.
//
ValueNum ValueNumStore::VNForMapPhysicalSelect(
    ValueNumKind vnk, var_types type, ValueNum map, unsigned offset, unsigned size)
{
    assert(MapIsPhysical(map));

    ValueNum selector = EncodePhysicalSelector(offset, size);
    ValueNum result   = VNForMapSelectInner(vnk, type, map, selector);

    JITDUMP("    VNForMapPhysicalSelect(" FMT_VN ", ", map);
    JITDUMPEXEC(vnDumpPhysicalSelector(selector));
    JITDUMP("):%s returns ", VNMapTypeName(type));
    JITDUMPEXEC(m_pComp->vnPrint(result, 1));
    JITDUMP("\n");

    return result;
}

typedef JitHashTable<ValueNum, JitSmallPrimitiveKeyFuncs<ValueNum>, bool> ValueNumSet;

class SmallValueNumSet
{
    union {
        ValueNum     m_inlineElements[4];
        ValueNumSet* m_set;
    };
    unsigned m_numElements = 0;

public:
    unsigned Count()
    {
        return m_numElements;
    }

    template <typename Func>
    void ForEach(Func func)
    {
        if (m_numElements <= ArrLen(m_inlineElements))
        {
            for (unsigned i = 0; i < m_numElements; i++)
            {
                func(m_inlineElements[i]);
            }
        }
        else
        {
            for (ValueNum vn : ValueNumSet::KeyIteration(m_set))
            {
                func(vn);
            }
        }
    }

    void Add(Compiler* comp, ValueNum vn)
    {
        if (m_numElements <= ArrLen(m_inlineElements))
        {
            for (unsigned i = 0; i < m_numElements; i++)
            {
                if (m_inlineElements[i] == vn)
                {
                    return;
                }
            }

            if (m_numElements < ArrLen(m_inlineElements))
            {
                m_inlineElements[m_numElements] = vn;
                m_numElements++;
            }
            else
            {
                ValueNumSet* set = new (comp, CMK_ValueNumber) ValueNumSet(comp->getAllocator(CMK_ValueNumber));
                for (ValueNum oldVn : m_inlineElements)
                {
                    set->Set(oldVn, true);
                }

                set->Set(vn, true);

                m_set = set;
                m_numElements++;
                assert(m_numElements == set->GetCount());
            }
        }
        else
        {
            m_set->Set(vn, true, ValueNumSet::SetKind::Overwrite);
            m_numElements = m_set->GetCount();
        }
    }
};

//------------------------------------------------------------------------------
// VNForMapSelectInner: Select value from a map and record loop memory dependencies.
//
// Arguments:
//    vnk   - Value number kind (see the notes for "VNForMapSelect")
//    type  - The type to select
//    map   - (VN of) the physical map
//    index - The selector
//
// Return Value:
//    Value number for the result of the evaluation.
//
ValueNum ValueNumStore::VNForMapSelectInner(ValueNumKind vnk, var_types type, ValueNum map, ValueNum index)
{
    int              budget          = m_mapSelectBudget;
    bool             usedRecursiveVN = false;
    SmallValueNumSet memoryDependencies;
    ValueNum         result = VNForMapSelectWork(vnk, type, map, index, &budget, &usedRecursiveVN, memoryDependencies);

    // The remaining budget should always be between [0..m_mapSelectBudget]
    assert((budget >= 0) && (budget <= m_mapSelectBudget));

    // If the current tree is in a loop then record memory dependencies for
    // hoisting. Note that this function may be called by other phases than VN
    // (such as VN-based dead store removal).
    if ((m_pComp->compCurBB != nullptr) && (m_pComp->compCurTree != nullptr))
    {
        FlowGraphNaturalLoop* loop = m_pComp->m_blockToLoop->GetLoop(m_pComp->compCurBB);
        if (loop != nullptr)
        {
            memoryDependencies.ForEach([this](ValueNum vn) {
                m_pComp->optRecordLoopMemoryDependence(m_pComp->compCurTree, m_pComp->compCurBB, vn);
            });
        }
    }

    return result;
}

//------------------------------------------------------------------------------
// SetMemoryDependencies: Set the cached memory dependencies for a map-select
// cache entry.
//
// Arguments:
//    comp - Compiler instance
//    set  - Set of memory dependencies to store in the entry.
//
void ValueNumStore::MapSelectWorkCacheEntry::SetMemoryDependencies(Compiler* comp, SmallValueNumSet& set)
{
    m_numMemoryDependencies = set.Count();
    ValueNum* arr;
    if (m_numMemoryDependencies > ArrLen(m_inlineMemoryDependencies))
    {
        m_memoryDependencies = new (comp, CMK_ValueNumber) ValueNum[m_numMemoryDependencies];

        arr = m_memoryDependencies;
    }
    else
    {
        arr = m_inlineMemoryDependencies;
    }

    size_t i = 0;
    set.ForEach([&i, arr](ValueNum vn) {
        arr[i] = vn;
        i++;
    });
}

//------------------------------------------------------------------------------
// GetMemoryDependencies: Push all of the memory dependencies cached in this
// entry into the specified set.
//
// Arguments:
//    comp   - Compiler instance
//    result - Set to add memory dependencies to.
//
void ValueNumStore::MapSelectWorkCacheEntry::GetMemoryDependencies(Compiler* comp, SmallValueNumSet& result)
{
    ValueNum* arr = m_numMemoryDependencies <= ArrLen(m_inlineMemoryDependencies) ? m_inlineMemoryDependencies
                                                                                  : m_memoryDependencies;

    for (unsigned i = 0; i < m_numMemoryDependencies; i++)
    {
        result.Add(comp, arr[i]);
    }
}

//------------------------------------------------------------------------------
// VNForMapSelectWork : A method that does the work for VNForMapSelect and may call itself recursively.
//
// Arguments:
//    vnk                - Value number kind
//    type               - Value type
//    map                - The map to select from
//    index              - The selector
//    pBudget            - Remaining budget for the outer evaluation
//    pUsedRecursiveVN   - Out-parameter that is set to true iff RecursiveVN was returned from this method
//                         or from a method called during one of recursive invocations.
//    memoryDependencies - Set that records VNs of memories that the result is dependent upon.
//
// Return Value:
//    Value number for the result of the evaluation.
//
// Notes:
//    This requires a "ValueNumKind" because it will attempt, given "select(phi(m1, ..., mk), ind)", to evaluate
//    "select(m1, ind)", ..., "select(mk, ind)" to see if they agree.  It needs to know which kind of value number
//    (liberal/conservative) to read from the SSA def referenced in the phi argument.
//
ValueNum ValueNumStore::VNForMapSelectWork(ValueNumKind      vnk,
                                           var_types         type,
                                           ValueNum          map,
                                           ValueNum          index,
                                           int*              pBudget,
                                           bool*             pUsedRecursiveVN,
                                           SmallValueNumSet& memoryDependencies)
{
TailCall:
    // This label allows us to directly implement a tail call by setting up the arguments, and doing a goto to here.

    assert(map != NoVN && index != NoVN);
    assert(map == VNNormalValue(map));     // Arguments carry no exceptions.
    assert(index == VNNormalValue(index)); // Arguments carry no exceptions.

    *pUsedRecursiveVN = false;

#ifdef DEBUG
    // Provide a mechanism for writing tests that ensure we don't call this ridiculously often.
    m_numMapSels++;
#if 1
// This printing is sometimes useful in debugging.
// if ((m_numMapSels % 1000) == 0) printf("%d VNF_MapSelect applications.\n", m_numMapSels);
#endif
    unsigned selLim = JitConfig.JitVNMapSelLimit();
    assert(selLim == 0 || m_numMapSels < selLim);
#endif

    MapSelectWorkCacheEntry entry;

    VNDefFuncApp<2> fstruct(VNF_MapSelect, map, index);
    if (GetMapSelectWorkCache()->Lookup(fstruct, &entry))
    {
        entry.GetMemoryDependencies(m_pComp, memoryDependencies);
        return entry.Result;
    }

    // Give up if we've run out of budget.
    if (*pBudget == 0)
    {
        // We have to use 'nullptr' for the basic block here, because subsequent expressions
        // in different blocks may find this result in the VNFunc2Map -- other expressions in
        // the IR may "evaluate" to this same VNForExpr, so it is not "unique" in the sense
        // that permits the BasicBlock attribution.
        entry.Result = VNForExpr(nullptr, type);
        GetMapSelectWorkCache()->Set(fstruct, entry);
        return entry.Result;
    }

    // Reduce our budget by one
    (*pBudget)--;

    // If it's recursive, stop the recursion.
    if (SelectIsBeingEvaluatedRecursively(map, index))
    {
        *pUsedRecursiveVN = true;
        return RecursiveVN;
    }

    SmallValueNumSet recMemoryDependencies;

    VNFuncApp funcApp;
    if (GetVNFunc(map, &funcApp))
    {
        switch (funcApp.m_func)
        {
            case VNF_MapStore:
            {
                assert(MapIsPrecise(map));

                // select(store(m, i, v), i) == v
                if (funcApp.m_args[1] == index)
                {
#if FEATURE_VN_TRACE_APPLY_SELECTORS
                    JITDUMP("      AX1: select([" FMT_VN "]store(" FMT_VN ", " FMT_VN ", " FMT_VN "), " FMT_VN
                            ") ==> " FMT_VN ".\n",
                            funcApp.m_args[0], map, funcApp.m_args[1], funcApp.m_args[2], index, funcApp.m_args[2]);
#endif

                    memoryDependencies.Add(m_pComp, funcApp.m_args[0]);

                    return funcApp.m_args[2];
                }
                // i # j ==> select(store(m, i, v), j) == select(m, j)
                // Currently the only source of distinctions is when both indices are constants.
                else if (IsVNConstant(index) && IsVNConstant(funcApp.m_args[1]))
                {
                    assert(funcApp.m_args[1] != index); // we already checked this above.
#if FEATURE_VN_TRACE_APPLY_SELECTORS
                    JITDUMP("      AX2: " FMT_VN " != " FMT_VN " ==> select([" FMT_VN "]store(" FMT_VN ", " FMT_VN
                            ", " FMT_VN "), " FMT_VN ") ==> select(" FMT_VN ", " FMT_VN ") remaining budget is %d.\n",
                            index, funcApp.m_args[1], map, funcApp.m_args[0], funcApp.m_args[1], funcApp.m_args[2],
                            index, funcApp.m_args[0], index, *pBudget);
#endif
                    // This is the equivalent of the recursive tail call:
                    // return VNForMapSelect(vnk, typ, funcApp.m_args[0], index);
                    // Make sure we capture any exceptions from the "i" and "v" of the store...
                    map = funcApp.m_args[0];
                    goto TailCall;
                }
            }
            break;

            case VNF_MapPhysicalStore:
            {
                assert(MapIsPhysical(map));

#if FEATURE_VN_TRACE_APPLY_SELECTORS
                JITDUMP("      select(");
                JITDUMPEXEC(m_pComp->vnPrint(map, 1));
                JITDUMP(", ");
                JITDUMPEXEC(vnDumpPhysicalSelector(index));
                JITDUMP(")");
#endif
                ValueNum storeSelector = funcApp.m_args[1];

                if (index == storeSelector)
                {
#if FEATURE_VN_TRACE_APPLY_SELECTORS
                    JITDUMP(" ==> " FMT_VN "\n", funcApp.m_args[2]);
#endif
                    return funcApp.m_args[2];
                }

                unsigned selectSize;
                unsigned selectOffset = DecodePhysicalSelector(index, &selectSize);

                unsigned storeSize;
                unsigned storeOffset = DecodePhysicalSelector(storeSelector, &storeSize);

                unsigned selectEndOffset = selectOffset + selectSize; // Exclusive.
                unsigned storeEndOffset  = storeOffset + storeSize;   // Exclusive.

                if ((storeOffset <= selectOffset) && (selectEndOffset <= storeEndOffset))
                {
#if FEATURE_VN_TRACE_APPLY_SELECTORS
                    JITDUMP(" ==> enclosing, selecting inner, remaining budget is %d\n", *pBudget);
#endif
                    map   = funcApp.m_args[2];
                    index = EncodePhysicalSelector(selectOffset - storeOffset, selectSize);
                    goto TailCall;
                }

                // If it was disjoint with the location being selected, continue the linear search.
                if ((storeEndOffset <= selectOffset) || (selectEndOffset <= storeOffset))
                {
#if FEATURE_VN_TRACE_APPLY_SELECTORS
                    JITDUMP(" ==> disjoint, remaining budget is %d\n", *pBudget);
#endif
                    map = funcApp.m_args[0];
                    goto TailCall;
                }
                else
                {
#if FEATURE_VN_TRACE_APPLY_SELECTORS
                    JITDUMP(" ==> aliasing!\n");
#endif
                }
            }
            break;

            case VNF_BitCast:
                assert(MapIsPhysical(map));
#if FEATURE_VN_TRACE_APPLY_SELECTORS
                JITDUMP("      select(bitcast<%s>(" FMT_VN ")) ==> select(" FMT_VN ")\n",
                        varTypeName(TypeOfVN(funcApp.m_args[0])), funcApp.m_args[0], funcApp.m_args[0]);
#endif // FEATURE_VN_TRACE_APPLY_SELECTORS

                map = funcApp.m_args[0];
                goto TailCall;

            case VNF_ZeroObj:
                assert(MapIsPhysical(map));

                // TODO-CQ: support selection of TYP_STRUCT here.
                if (type != TYP_STRUCT)
                {
                    return VNZeroForType(type);
                }
                break;

            case VNF_PhiDef:
            case VNF_PhiMemoryDef:
            {
                unsigned  lclNum   = BAD_VAR_NUM;
                bool      isMemory = false;
                VNFuncApp phiFuncApp;
                bool      defArgIsFunc = false;
                if (funcApp.m_func == VNF_PhiDef)
                {
                    lclNum       = unsigned(funcApp.m_args[0]);
                    defArgIsFunc = GetVNFunc(funcApp.m_args[2], &phiFuncApp);
                }
                else
                {
                    assert(funcApp.m_func == VNF_PhiMemoryDef);
                    isMemory     = true;
                    defArgIsFunc = GetVNFunc(funcApp.m_args[1], &phiFuncApp);
                }
                if (defArgIsFunc && phiFuncApp.m_func == VNF_Phi)
                {
                    // select(phi(m1, m2), x): if select(m1, x) == select(m2, x), return that, else new fresh.
                    // Get the first argument of the phi.

                    // We need to be careful about breaking infinite recursion.  Record the outer select.
                    m_fixedPointMapSels.Push(VNDefFuncApp<2>(VNF_MapSelect, map, index));

                    assert(IsVNConstant(phiFuncApp.m_args[0]));
                    unsigned phiArgSsaNum = ConstantValue<unsigned>(phiFuncApp.m_args[0]);
                    ValueNum phiArgVN;
                    if (isMemory)
                    {
                        phiArgVN = m_pComp->GetMemoryPerSsaData(phiArgSsaNum)->m_vnPair.Get(vnk);
                    }
                    else
                    {
                        phiArgVN = m_pComp->lvaTable[lclNum].GetPerSsaData(phiArgSsaNum)->m_vnPair.Get(vnk);
                    }
                    if (phiArgVN != ValueNumStore::NoVN)
                    {
                        bool     allSame       = true;
                        ValueNum argRest       = phiFuncApp.m_args[1];
                        ValueNum sameSelResult = VNForMapSelectWork(vnk, type, phiArgVN, index, pBudget,
                                                                    pUsedRecursiveVN, recMemoryDependencies);

                        // It is possible that we just now exceeded our budget, if so we need to force an early exit
                        // and stop calling VNForMapSelectWork
                        if (*pBudget <= 0)
                        {
                            // We don't have any budget remaining to verify that all phiArgs are the same
                            // so setup the default failure case now.
                            allSame = false;
                        }

                        while (allSame && argRest != ValueNumStore::NoVN)
                        {
                            ValueNum  cur = argRest;
                            VNFuncApp phiArgFuncApp;
                            if (GetVNFunc(argRest, &phiArgFuncApp) && phiArgFuncApp.m_func == VNF_Phi)
                            {
                                cur     = phiArgFuncApp.m_args[0];
                                argRest = phiArgFuncApp.m_args[1];
                            }
                            else
                            {
                                argRest = ValueNumStore::NoVN; // Cause the loop to terminate.
                            }
                            assert(IsVNConstant(cur));
                            phiArgSsaNum = ConstantValue<unsigned>(cur);
                            if (isMemory)
                            {
                                phiArgVN = m_pComp->GetMemoryPerSsaData(phiArgSsaNum)->m_vnPair.Get(vnk);
                            }
                            else
                            {
                                phiArgVN = m_pComp->lvaTable[lclNum].GetPerSsaData(phiArgSsaNum)->m_vnPair.Get(vnk);
                            }
                            if (phiArgVN == ValueNumStore::NoVN)
                            {
                                allSame = false;
                            }
                            else
                            {
                                bool     usedRecursiveVN = false;
                                ValueNum curResult       = VNForMapSelectWork(vnk, type, phiArgVN, index, pBudget,
                                                                        &usedRecursiveVN, recMemoryDependencies);

                                *pUsedRecursiveVN |= usedRecursiveVN;
                                if (sameSelResult == ValueNumStore::RecursiveVN)
                                {
                                    sameSelResult = curResult;
                                }
                                if (curResult != ValueNumStore::RecursiveVN && curResult != sameSelResult)
                                {
                                    allSame = false;
                                }
                            }
                        }
                        if (allSame && sameSelResult != ValueNumStore::RecursiveVN)
                        {
                            // Make sure we're popping what we pushed.
                            assert(FixedPointMapSelsTopHasValue(map, index));
                            m_fixedPointMapSels.Pop();

                            // To avoid exponential searches, we make sure that this result is memo-ized.
                            // The result is always valid for memoization if we didn't rely on RecursiveVN to get
                            // it.
                            // If RecursiveVN was used, we are processing a loop and we can't memo-ize this
                            // intermediate
                            // result if, e.g., this block is in a multi-entry loop.
                            if (!*pUsedRecursiveVN)
                            {
                                entry.Result = sameSelResult;
                                entry.SetMemoryDependencies(m_pComp, recMemoryDependencies);

                                GetMapSelectWorkCache()->Set(fstruct, entry);
                            }

                            recMemoryDependencies.ForEach(
                                [this, &memoryDependencies](ValueNum vn) { memoryDependencies.Add(m_pComp, vn); });

                            return sameSelResult;
                        }
                        // Otherwise, fall through to creating the select(phi(m1, m2), x) function application.
                    }
                    // Make sure we're popping what we pushed.
                    assert(FixedPointMapSelsTopHasValue(map, index));
                    m_fixedPointMapSels.Pop();
                }
            }
            break;

            default:
                break;
        }
    }

    // We may have run out of budget and already assigned a result
    if (!GetMapSelectWorkCache()->Lookup(fstruct, &entry))
    {
        // Otherwise, assign a new VN for the function application.
        Chunk* const          c                 = GetAllocChunk(type, CEA_Func2);
        unsigned const        offsetWithinChunk = c->AllocVN();
        VNDefFuncAppFlexible* fapp              = c->PointerToFuncApp(offsetWithinChunk, 2);
        fapp->m_func                            = fstruct.m_func;
        fapp->m_args[0]                         = fstruct.m_args[0];
        fapp->m_args[1]                         = fstruct.m_args[1];

        entry.Result = c->m_baseVN + offsetWithinChunk;
        entry.SetMemoryDependencies(m_pComp, recMemoryDependencies);

        GetMapSelectWorkCache()->Set(fstruct, entry);
    }

    recMemoryDependencies.ForEach([this, &memoryDependencies](ValueNum vn) { memoryDependencies.Add(m_pComp, vn); });

    return entry.Result;
}

//------------------------------------------------------------------------
// EncodePhysicalSelector: Get the VN representing a physical selector.
//
// Arguments:
//    offset - The offset to encode
//    size   - The size to encode
//
// Return Value:
//    VN encoding the "{ offset, size }" tuple.
//
ValueNum ValueNumStore::EncodePhysicalSelector(unsigned offset, unsigned size)
{
    assert(size != 0);

    return VNForLongCon(static_cast<uint64_t>(offset) | (static_cast<uint64_t>(size) << 32));
}

//------------------------------------------------------------------------
// DecodePhysicalSelector: Get the components of a physical selector from the
//                         VN representing one.
//
// Arguments:
//    selector - The VN of the selector obtained via "EncodePhysicalSelector"
//    pSize    - [out] parameter for the size
//
// Return Value:
//    The offset.
//
unsigned ValueNumStore::DecodePhysicalSelector(ValueNum selector, unsigned* pSize)
{
    uint64_t value  = ConstantValue<uint64_t>(selector);
    unsigned offset = static_cast<unsigned>(value);
    unsigned size   = static_cast<unsigned>(value >> 32);

    *pSize = size;
    return offset;
}

//------------------------------------------------------------------------
// VNForFieldSelector: A specialized version (with logging) of VNForHandle
//                     that is used for field handle selectors.
//
// Arguments:
//    fieldHnd   - handle of the field in question
//    pFieldType - [out] parameter for the field's type
//    pSize      - optional [out] parameter for the size of the field
//
// Return Value:
//    Value number corresponding to the given field handle.
//
ValueNum ValueNumStore::VNForFieldSelector(CORINFO_FIELD_HANDLE fieldHnd, var_types* pFieldType, unsigned* pSize)
{
    CORINFO_CLASS_HANDLE structHnd = NO_CLASS_HANDLE;
    ValueNum             fldHndVN  = VNForHandle(ssize_t(fieldHnd), GTF_ICON_FIELD_HDL);
    var_types            fieldType = m_pComp->eeGetFieldType(fieldHnd, &structHnd);
    unsigned             size      = 0;

    if (fieldType == TYP_STRUCT)
    {
        size = m_pComp->info.compCompHnd->getClassSize(structHnd);

        // We have to normalize here since there is no CorInfoType for vectors...
        if (m_pComp->structSizeMightRepresentSIMDType(size))
        {
            fieldType = m_pComp->impNormStructType(structHnd);
        }
    }
    else
    {
        size = genTypeSize(fieldType);
    }

#ifdef DEBUG
    if (m_pComp->verbose)
    {
        char        buffer[128];
        const char* fldName = m_pComp->eeGetFieldName(fieldHnd, false, buffer, sizeof(buffer));

        printf("    VNForHandle(%s) is " FMT_VN ", fieldType is %s", fldName, fldHndVN, varTypeName(fieldType));

        if (size != 0)
        {
            printf(", size = %u", size);
        }
        printf("\n");
    }
#endif

    *pFieldType = fieldType;
    *pSize      = size;

    return fldHndVN;
}

ValueNum ValueNumStore::EvalFuncForConstantArgs(var_types typ, VNFunc func, ValueNum arg0VN)
{
    assert(VNEvalCanFoldUnaryFunc(typ, func, arg0VN));

    switch (TypeOfVN(arg0VN))
    {
        case TYP_INT:
        {
            int resVal = EvalOp<int>(func, ConstantValue<int>(arg0VN));
            // Unary op on a handle results in a handle.
            return IsVNHandle(arg0VN) ? VNForHandle(ssize_t(resVal), GetFoldedArithOpResultHandleFlags(arg0VN))
                                      : VNForIntCon(resVal);
        }
        case TYP_LONG:
        {
            INT64 resVal = EvalOp<INT64>(func, ConstantValue<INT64>(arg0VN));
            // Unary op on a handle results in a handle.
            return IsVNHandle(arg0VN) ? VNForHandle(ssize_t(resVal), GetFoldedArithOpResultHandleFlags(arg0VN))
                                      : VNForLongCon(resVal);
        }
        case TYP_FLOAT:
        {
            float resVal = EvalOp<float>(func, ConstantValue<float>(arg0VN));
            return VNForFloatCon(resVal);
        }
        case TYP_DOUBLE:
        {
            double resVal = EvalOp<double>(func, ConstantValue<double>(arg0VN));
            return VNForDoubleCon(resVal);
        }
        case TYP_REF:
        {
            // If arg0 has a possible exception, it wouldn't have been constant.
            assert(!VNHasExc(arg0VN));
            // Otherwise...
            assert(arg0VN == VNForNull()); // Only other REF constant.

            // Only functions we can apply to a REF constant!
            assert((func == VNFunc(GT_ARR_LENGTH)) || (func == VNF_MDArrLength) ||
                   (func == VNFunc(GT_MDARR_LOWER_BOUND)));
            return VNWithExc(VNForVoid(), VNExcSetSingleton(VNForFunc(TYP_REF, VNF_NullPtrExc, VNForNull())));
        }
        default:
            // We will assert below
            break;
    }
    noway_assert(!"Unhandled operation in EvalFuncForConstantArgs");
    return NoVN;
}

bool ValueNumStore::SelectIsBeingEvaluatedRecursively(ValueNum map, ValueNum ind)
{
    for (unsigned i = 0; i < m_fixedPointMapSels.Size(); i++)
    {
        VNDefFuncApp<2>& elem = m_fixedPointMapSels.GetRef(i);
        assert(elem.m_func == VNF_MapSelect);
        if (elem.m_args[0] == map && elem.m_args[1] == ind)
        {
            return true;
        }
    }
    return false;
}

#ifdef DEBUG
bool ValueNumStore::FixedPointMapSelsTopHasValue(ValueNum map, ValueNum index)
{
    if (m_fixedPointMapSels.Size() == 0)
    {
        return false;
    }
    VNDefFuncApp<2>& top = m_fixedPointMapSels.TopRef();
    return top.m_func == VNF_MapSelect && top.m_args[0] == map && top.m_args[1] == index;
}
#endif

// Given an integer constant value number return its value as an int.
//
int ValueNumStore::GetConstantInt32(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    var_types argVNtyp = TypeOfVN(argVN);

    int result = 0;

    switch (argVNtyp)
    {
        case TYP_INT:
            result = ConstantValue<int>(argVN);
            break;
#ifndef TARGET_64BIT
        case TYP_REF:
        case TYP_BYREF:
            result = (int)ConstantValue<size_t>(argVN);
            break;
#endif
        default:
            unreached();
    }
    return result;
}

// Given an integer constant value number return its value as an INT64.
//
INT64 ValueNumStore::GetConstantInt64(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    var_types argVNtyp = TypeOfVN(argVN);

    INT64 result = 0;

    switch (argVNtyp)
    {
        case TYP_INT:
            result = (INT64)ConstantValue<int>(argVN);
            break;
        case TYP_LONG:
            result = ConstantValue<INT64>(argVN);
            break;
        case TYP_REF:
        case TYP_BYREF:
            result = (INT64)ConstantValue<size_t>(argVN);
            break;
        default:
            unreached();
    }
    return result;
}

// Given a double constant value number return its value as a double.
//
double ValueNumStore::GetConstantDouble(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    assert(TypeOfVN(argVN) == TYP_DOUBLE);

    return ConstantValue<double>(argVN);
}

// Given a float constant value number return its value as a float.
//
float ValueNumStore::GetConstantSingle(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    assert(TypeOfVN(argVN) == TYP_FLOAT);

    return ConstantValue<float>(argVN);
}

#if defined(FEATURE_SIMD)
// Given a simd8 constant value number return its value as a simd8.
//
simd8_t ValueNumStore::GetConstantSimd8(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    assert(TypeOfVN(argVN) == TYP_SIMD8);

    return ConstantValue<simd8_t>(argVN);
}

// Given a simd12 constant value number return its value as a simd12.
//
simd12_t ValueNumStore::GetConstantSimd12(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    assert(TypeOfVN(argVN) == TYP_SIMD12);

    return ConstantValue<simd12_t>(argVN);
}

// Given a simd16 constant value number return its value as a simd16.
//
simd16_t ValueNumStore::GetConstantSimd16(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    assert(TypeOfVN(argVN) == TYP_SIMD16);

    return ConstantValue<simd16_t>(argVN);
}

#if defined(TARGET_XARCH)
// Given a simd32 constant value number return its value as a simd32.
//
simd32_t ValueNumStore::GetConstantSimd32(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    assert(TypeOfVN(argVN) == TYP_SIMD32);

    return ConstantValue<simd32_t>(argVN);
}

// Given a simd64 constant value number return its value as a simd32.
//
simd64_t ValueNumStore::GetConstantSimd64(ValueNum argVN)
{
    assert(IsVNConstant(argVN));
    assert(TypeOfVN(argVN) == TYP_SIMD64);

    return ConstantValue<simd64_t>(argVN);
}
#endif // TARGET_XARCH
#endif // FEATURE_SIMD

// Compute the proper value number when the VNFunc has all constant arguments
// This essentially performs constant folding at value numbering time
//
ValueNum ValueNumStore::EvalFuncForConstantArgs(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(VNEvalCanFoldBinaryFunc(typ, func, arg0VN, arg1VN));

    // if our func is the VNF_Cast operation we handle it first
    if (VNFuncIsNumericCast(func))
    {
        return EvalCastForConstantArgs(typ, func, arg0VN, arg1VN);
    }

    if (func == VNF_BitCast)
    {
        return EvalBitCastForConstantArgs(typ, arg0VN);
    }

    var_types arg0VNtyp = TypeOfVN(arg0VN);
    var_types arg1VNtyp = TypeOfVN(arg1VN);

    // When both arguments are floating point types
    // We defer to the EvalFuncForConstantFPArgs()
    if (varTypeIsFloating(arg0VNtyp) && varTypeIsFloating(arg1VNtyp))
    {
        return EvalFuncForConstantFPArgs(typ, func, arg0VN, arg1VN);
    }

    // after this we shouldn't have to deal with floating point types for arg0VN or arg1VN
    assert(!varTypeIsFloating(arg0VNtyp));
    assert(!varTypeIsFloating(arg1VNtyp));

    // Stack-normalize the result type.
    if (varTypeIsSmall(typ))
    {
        typ = TYP_INT;
    }

    ValueNum result; // left uninitialized, we are required to initialize it on all paths below.

    // Are both args of the same type?
    if (arg0VNtyp == arg1VNtyp)
    {
        if (arg0VNtyp == TYP_INT)
        {
            int arg0Val = ConstantValue<int>(arg0VN);
            int arg1Val = ConstantValue<int>(arg1VN);

            if (VNFuncIsComparison(func))
            {
                assert(typ == TYP_INT);
                result = VNForIntCon(EvalComparison(func, arg0Val, arg1Val));
            }
            else
            {
                assert(typ == TYP_INT);
                int resultVal = EvalOp<int>(func, arg0Val, arg1Val);
                // Bin op on a handle results in a handle.
                ValueNum handleVN = IsVNHandle(arg0VN) ? arg0VN : IsVNHandle(arg1VN) ? arg1VN : NoVN;
                if (handleVN != NoVN)
                {
                    result = VNForHandle(ssize_t(resultVal), GetFoldedArithOpResultHandleFlags(handleVN));
                }
                else
                {
                    result = VNForIntCon(resultVal);
                }
            }
        }
        else if (arg0VNtyp == TYP_LONG)
        {
            INT64 arg0Val = ConstantValue<INT64>(arg0VN);
            INT64 arg1Val = ConstantValue<INT64>(arg1VN);

            if (VNFuncIsComparison(func))
            {
                assert(typ == TYP_INT);
                result = VNForIntCon(EvalComparison(func, arg0Val, arg1Val));
            }
            else
            {
                assert(typ == TYP_LONG);
                INT64    resultVal = EvalOp<INT64>(func, arg0Val, arg1Val);
                ValueNum handleVN  = IsVNHandle(arg0VN) ? arg0VN : IsVNHandle(arg1VN) ? arg1VN : NoVN;

                if (handleVN != NoVN)
                {
                    result = VNForHandle(ssize_t(resultVal), GetFoldedArithOpResultHandleFlags(handleVN));
                }
                else
                {
                    result = VNForLongCon(resultVal);
                }
            }
        }
        else // both args are TYP_REF or both args are TYP_BYREF
        {
            size_t arg0Val = ConstantValue<size_t>(arg0VN); // We represent ref/byref constants as size_t's.
            size_t arg1Val = ConstantValue<size_t>(arg1VN); // Also we consider null to be zero.

            if (VNFuncIsComparison(func))
            {
                assert(typ == TYP_INT);
                result = VNForIntCon(EvalComparison(func, arg0Val, arg1Val));
            }
            else if (typ == TYP_INT) // We could see GT_OR of a constant ByRef and Null
            {
                int resultVal = (int)EvalOp<size_t>(func, arg0Val, arg1Val);
                result        = VNForIntCon(resultVal);
            }
            else // We could see GT_OR of a constant ByRef and Null
            {
                assert((typ == TYP_BYREF) || (typ == TYP_I_IMPL));
                size_t resultVal = EvalOp<size_t>(func, arg0Val, arg1Val);
                result           = VNForByrefCon((target_size_t)resultVal);
            }
        }
    }
    else // We have args of different types
    {
        // We represent ref/byref constants as size_t's.
        // Also we consider null to be zero.
        //
        INT64 arg0Val = GetConstantInt64(arg0VN);
        INT64 arg1Val = GetConstantInt64(arg1VN);

        if (VNFuncIsComparison(func))
        {
            assert(typ == TYP_INT);
            result = VNForIntCon(EvalComparison(func, arg0Val, arg1Val));
        }
        else if (typ == TYP_INT) // We could see GT_OR of an int and constant ByRef or Null
        {
            int resultVal = (int)EvalOp<INT64>(func, arg0Val, arg1Val);
            result        = VNForIntCon(resultVal);
        }
        else
        {
            assert(typ != TYP_INT);
            INT64 resultVal = EvalOp<INT64>(func, arg0Val, arg1Val);

            switch (typ)
            {
                case TYP_BYREF:
                    result = VNForByrefCon((target_size_t)resultVal);
                    break;
                case TYP_LONG:
                    result = VNForLongCon(resultVal);
                    break;
                case TYP_REF:
                    assert(resultVal == 0); // Only valid REF constant
                    result = VNForNull();
                    break;
                default:
                    unreached();
            }
        }
    }

    return result;
}

// Compute the proper value number when the VNFunc has all constant floating-point arguments
// This essentially must perform constant folding at value numbering time
//
ValueNum ValueNumStore::EvalFuncForConstantFPArgs(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(VNEvalCanFoldBinaryFunc(typ, func, arg0VN, arg1VN));

    // We expect both argument types to be floating-point types
    var_types arg0VNtyp = TypeOfVN(arg0VN);
    var_types arg1VNtyp = TypeOfVN(arg1VN);

    assert(varTypeIsFloating(arg0VNtyp));
    assert(varTypeIsFloating(arg1VNtyp));

    // We also expect both arguments to be of the same floating-point type
    assert(arg0VNtyp == arg1VNtyp);

    ValueNum result; // left uninitialized, we are required to initialize it on all paths below.

    if (VNFuncIsComparison(func))
    {
        assert(genActualType(typ) == TYP_INT);

        if (arg0VNtyp == TYP_FLOAT)
        {
            result = VNForIntCon(EvalComparison<float>(func, GetConstantSingle(arg0VN), GetConstantSingle(arg1VN)));
        }
        else
        {
            assert(arg0VNtyp == TYP_DOUBLE);
            result = VNForIntCon(EvalComparison<double>(func, GetConstantDouble(arg0VN), GetConstantDouble(arg1VN)));
        }
    }
    else
    {
        // We expect the return type to be the same as the argument type
        assert(varTypeIsFloating(typ));
        assert(arg0VNtyp == typ);

        if (typ == TYP_FLOAT)
        {
            float floatResultVal = EvalOp<float>(func, GetConstantSingle(arg0VN), GetConstantSingle(arg1VN));
            result               = VNForFloatCon(floatResultVal);
        }
        else
        {
            assert(typ == TYP_DOUBLE);

            double doubleResultVal = EvalOp<double>(func, GetConstantDouble(arg0VN), GetConstantDouble(arg1VN));
            result                 = VNForDoubleCon(doubleResultVal);
        }
    }

    return result;
}

// Compute the proper value number for a VNF_Cast with constant arguments
// This essentially must perform constant folding at value numbering time
//
ValueNum ValueNumStore::EvalCastForConstantArgs(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(VNFuncIsNumericCast(func));
    assert(IsVNConstant(arg0VN) && IsVNConstant(arg1VN));

    // Stack-normalize the result type.
    if (varTypeIsSmall(typ))
    {
        typ = TYP_INT;
    }

    var_types arg0VNtyp = TypeOfVN(arg0VN);

    if (IsVNHandle(arg0VN))
    {
        // We don't allow handles to be cast to random var_types.
        assert(typ == TYP_I_IMPL);
    }

    // We previously encoded the castToType operation using VNForCastOper().
    var_types castToType;
    bool      srcIsUnsigned;
    GetCastOperFromVN(arg1VN, &castToType, &srcIsUnsigned);
    var_types castFromType = arg0VNtyp;
    bool      checkedCast  = func == VNF_CastOvf;

    switch (castFromType) // GT_CAST source type
    {
#ifndef TARGET_64BIT
        case TYP_REF:
        case TYP_BYREF:
#endif
        case TYP_INT:
        {
            int arg0Val = GetConstantInt32(arg0VN);
            assert(!checkedCast || !CheckedOps::CastFromIntOverflows(arg0Val, castToType, srcIsUnsigned));

            switch (castToType)
            {
                case TYP_BYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT8(arg0Val));
                case TYP_UBYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT8(arg0Val));
                case TYP_SHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT16(arg0Val));
                case TYP_USHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT16(arg0Val));
                case TYP_INT:
                case TYP_UINT:
                    assert(typ == TYP_INT);
                    return arg0VN;
                case TYP_LONG:
                case TYP_ULONG:
                    assert(!IsVNHandle(arg0VN));
#ifdef TARGET_64BIT
                    if (typ == TYP_LONG)
                    {
                        if (srcIsUnsigned)
                        {
                            return VNForLongCon(INT64(unsigned(arg0Val)));
                        }
                        else
                        {
                            return VNForLongCon(INT64(arg0Val));
                        }
                    }
                    else
                    {
                        assert(typ == TYP_BYREF);
                        return VNForByrefCon(target_size_t(arg0Val));
                    }
#else // TARGET_32BIT
                    if (srcIsUnsigned)
                        return VNForLongCon(INT64(unsigned(arg0Val)));
                    else
                        return VNForLongCon(INT64(arg0Val));
#endif
                case TYP_BYREF:
                    assert(typ == TYP_BYREF);
                    return VNForByrefCon(target_size_t(arg0Val));

                case TYP_FLOAT:
                    assert(typ == TYP_FLOAT);
                    if (srcIsUnsigned)
                    {
                        return VNForFloatCon(float(unsigned(arg0Val)));
                    }
                    else
                    {
                        return VNForFloatCon(float(arg0Val));
                    }
                case TYP_DOUBLE:
                    assert(typ == TYP_DOUBLE);
                    if (srcIsUnsigned)
                    {
                        return VNForDoubleCon(double(unsigned(arg0Val)));
                    }
                    else
                    {
                        return VNForDoubleCon(double(arg0Val));
                    }
                default:
                    unreached();
            }
            break;
        }
#ifdef TARGET_64BIT
        case TYP_REF:
        case TYP_BYREF:
#endif
        case TYP_LONG:
        {
            INT64 arg0Val = GetConstantInt64(arg0VN);
            assert(!checkedCast || !CheckedOps::CastFromLongOverflows(arg0Val, castToType, srcIsUnsigned));

            switch (castToType)
            {
                case TYP_BYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT8(arg0Val));
                case TYP_UBYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT8(arg0Val));
                case TYP_SHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT16(arg0Val));
                case TYP_USHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT16(arg0Val));
                case TYP_INT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT32(arg0Val));
                case TYP_UINT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT32(arg0Val));
                case TYP_LONG:
                case TYP_ULONG:
                    assert(typ == TYP_LONG);
                    return arg0VN;
                case TYP_BYREF:
                    assert(typ == TYP_BYREF);
                    return VNForByrefCon((target_size_t)arg0Val);
                case TYP_FLOAT:
                    assert(typ == TYP_FLOAT);
                    if (srcIsUnsigned)
                    {
                        return VNForFloatCon(FloatingPointUtils::convertUInt64ToFloat(UINT64(arg0Val)));
                    }
                    else
                    {
                        return VNForFloatCon(float(arg0Val));
                    }
                case TYP_DOUBLE:
                    assert(typ == TYP_DOUBLE);
                    if (srcIsUnsigned)
                    {
                        return VNForDoubleCon(FloatingPointUtils::convertUInt64ToDouble(UINT64(arg0Val)));
                    }
                    else
                    {
                        return VNForDoubleCon(double(arg0Val));
                    }
                default:
                    unreached();
            }
        }
        case TYP_FLOAT:
        {
            float arg0Val = GetConstantSingle(arg0VN);
            assert(!CheckedOps::CastFromFloatOverflows(arg0Val, castToType));

            switch (castToType)
            {
                case TYP_BYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT8(arg0Val));
                case TYP_UBYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT8(arg0Val));
                case TYP_SHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT16(arg0Val));
                case TYP_USHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT16(arg0Val));
                case TYP_INT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT32(arg0Val));
                case TYP_UINT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT32(arg0Val));
                case TYP_LONG:
                    assert(typ == TYP_LONG);
                    return VNForLongCon(INT64(arg0Val));
                case TYP_ULONG:
                    assert(typ == TYP_LONG);
                    return VNForLongCon(UINT64(arg0Val));
                case TYP_FLOAT:
                    assert(typ == TYP_FLOAT);
                    return VNForFloatCon(arg0Val);
                case TYP_DOUBLE:
                    assert(typ == TYP_DOUBLE);
                    return VNForDoubleCon(double(arg0Val));
                default:
                    unreached();
            }
        }
        case TYP_DOUBLE:
        {
            double arg0Val = GetConstantDouble(arg0VN);
            assert(!CheckedOps::CastFromDoubleOverflows(arg0Val, castToType));

            switch (castToType)
            {
                case TYP_BYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT8(arg0Val));
                case TYP_UBYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT8(arg0Val));
                case TYP_SHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT16(arg0Val));
                case TYP_USHORT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT16(arg0Val));
                case TYP_INT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT32(arg0Val));
                case TYP_UINT:
                    assert(typ == TYP_INT);
                    return VNForIntCon(UINT32(arg0Val));
                case TYP_LONG:
                    assert(typ == TYP_LONG);
                    return VNForLongCon(INT64(arg0Val));
                case TYP_ULONG:
                    assert(typ == TYP_LONG);
                    return VNForLongCon(UINT64(arg0Val));
                case TYP_FLOAT:
                    assert(typ == TYP_FLOAT);
                    return VNForFloatCon(float(arg0Val));
                case TYP_DOUBLE:
                    assert(typ == TYP_DOUBLE);
                    return VNForDoubleCon(arg0Val);
                default:
                    unreached();
            }
        }
        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// EvalBitCastForConstantArgs: Evaluate "BitCast(const)".
//
// Arguments:
//    dstType - The target type
//    arg0VN  - VN of the argument (must be a constant)
//
// Return Value:
//    The constant VN representing "BitCast<dstType>(arg0VN)".
//
ValueNum ValueNumStore::EvalBitCastForConstantArgs(var_types dstType, ValueNum arg0VN)
{
    // Handles - when generating relocatable code - don't represent their final
    // values, so we'll not fold bitcasts from them (always, for simplicity).
    assert(!IsVNHandle(arg0VN));

    var_types srcType = TypeOfVN(arg0VN);
    assert((genTypeSize(srcType) == genTypeSize(dstType)) || (varTypeIsSmall(dstType) && (srcType == TYP_INT)));

    int           int32    = 0;
    int64_t       int64    = 0;
    target_size_t nuint    = 0;
    float         float32  = 0;
    double        float64  = 0;
    simd8_t       simd8    = {};
    unsigned char bytes[8] = {};

    switch (srcType)
    {
        case TYP_INT:
            int32 = ConstantValue<int>(arg0VN);
            memcpy(bytes, &int32, sizeof(int32));
            break;
        case TYP_LONG:
            int64 = ConstantValue<int64_t>(arg0VN);
            memcpy(bytes, &int64, sizeof(int64));
            break;
        case TYP_BYREF:
            nuint = ConstantValue<target_size_t>(arg0VN);
            memcpy(bytes, &nuint, sizeof(nuint));
            break;
        case TYP_REF:
            noway_assert(arg0VN == VNForNull());
            nuint = 0;
            memcpy(bytes, &nuint, sizeof(nuint));
            break;
        case TYP_FLOAT:
            float32 = ConstantValue<float>(arg0VN);
            memcpy(bytes, &float32, sizeof(float32));
            break;
        case TYP_DOUBLE:
            float64 = ConstantValue<double>(arg0VN);
            memcpy(bytes, &float64, sizeof(float64));
            break;
#if defined(FEATURE_SIMD)
        case TYP_SIMD8:
            simd8 = ConstantValue<simd8_t>(arg0VN);
            memcpy(bytes, &simd8, sizeof(simd8));
            break;
#endif // FEATURE_SIMD
        default:
            unreached();
    }

    // "BitCast<small type>" has the semantic of only changing the upper bits (without truncation).
    if (varTypeIsSmall(dstType))
    {
        assert(FitsIn(varTypeToSigned(dstType), int32) || FitsIn(varTypeToUnsigned(dstType), int32));
    }

    switch (dstType)
    {
        case TYP_UBYTE:
            memcpy(&int32, bytes, sizeof(int32));
            return VNForIntCon(static_cast<uint8_t>(int32));
        case TYP_BYTE:
            memcpy(&int32, bytes, sizeof(int32));
            return VNForIntCon(static_cast<int8_t>(int32));
        case TYP_USHORT:
            memcpy(&int32, bytes, sizeof(int32));
            return VNForIntCon(static_cast<uint16_t>(int32));
        case TYP_SHORT:
            memcpy(&int32, bytes, sizeof(int32));
            return VNForIntCon(static_cast<int16_t>(int32));
        case TYP_INT:
            memcpy(&int32, bytes, sizeof(int32));
            return VNForIntCon(int32);
        case TYP_LONG:
            memcpy(&int64, bytes, sizeof(int64));
            return VNForLongCon(int64);
        case TYP_BYREF:
            memcpy(&nuint, bytes, sizeof(nuint));
            return VNForByrefCon(nuint);
        case TYP_FLOAT:
            memcpy(&float32, bytes, sizeof(float32));
            return VNForFloatCon(float32);
        case TYP_DOUBLE:
            memcpy(&float64, bytes, sizeof(float64));
            return VNForDoubleCon(float64);
#if defined(FEATURE_SIMD)
        case TYP_SIMD8:
            memcpy(&simd8, bytes, sizeof(simd8));
            return VNForSimd8Con(simd8);
#endif // FEATURE_SIMD
        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// VNEvalFoldTypeCompare:
//
// Arguments:
//    type   - The result type
//    func   - The function
//    arg0VN - VN of the first argument
//    arg1VN - VN of the second argument
//
// Return Value:
//    NoVN if this is not a foldable type compare
//    Simplified (perhaps constant) VN if it is foldable.
//
// Notes:
//    Value number counterpart to gtFoldTypeCompare
//    Doesn't handle all the cases (yet).
//
//    (EQ/NE (TypeHandleToRuntimeType x) (TypeHandleToRuntimeType y)) == (EQ/NE x y)
//
ValueNum ValueNumStore::VNEvalFoldTypeCompare(var_types type, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    const genTreeOps oper = genTreeOps(func);
    assert(GenTree::StaticOperIs(oper, GT_EQ, GT_NE));

    VNFuncApp  arg0Func;
    const bool arg0IsFunc = GetVNFunc(arg0VN, &arg0Func);

    if (!arg0IsFunc || (arg0Func.m_func != VNF_TypeHandleToRuntimeType))
    {
        return NoVN;
    }

    VNFuncApp  arg1Func;
    const bool arg1IsFunc = GetVNFunc(arg1VN, &arg1Func);

    if (!arg1IsFunc || (arg1Func.m_func != VNF_TypeHandleToRuntimeType))
    {
        return NoVN;
    }

    // Only re-express as handle equality when we have known
    // class handles and the VM agrees comparing these gives the same
    // result as comparing the runtime types.
    //
    // Note that VN actually tracks the value of embedded handle;
    // we need to pass the VM the associated the compile time handles,
    // in case they differ (say for prejitting or AOT).
    //
    ValueNum handle0 = arg0Func.m_args[0];
    if (!IsVNHandle(handle0))
    {
        return NoVN;
    }

    ValueNum handle1 = arg1Func.m_args[0];
    if (!IsVNHandle(handle1))
    {
        return NoVN;
    }

    assert(GetHandleFlags(handle0) == GTF_ICON_CLASS_HDL);
    assert(GetHandleFlags(handle1) == GTF_ICON_CLASS_HDL);

    const ssize_t handleVal0 = ConstantValue<ssize_t>(handle0);
    const ssize_t handleVal1 = ConstantValue<ssize_t>(handle1);
    ssize_t       compileTimeHandle0;
    ssize_t       compileTimeHandle1;

    // These mappings should always exist.
    //
    const bool found0 = m_embeddedToCompileTimeHandleMap.TryGetValue(handleVal0, &compileTimeHandle0);
    const bool found1 = m_embeddedToCompileTimeHandleMap.TryGetValue(handleVal1, &compileTimeHandle1);
    assert(found0 && found1);

    // We may see null compile time handles for some constructed class handle cases.
    // We should fix the construction if possible. But just skip those cases for now.
    //
    if ((compileTimeHandle0 == 0) || (compileTimeHandle1 == 0))
    {
        return NoVN;
    }

    JITDUMP("Asking runtime to compare %p (%s) and %p (%s) for equality\n", dspPtr(compileTimeHandle0),
            m_pComp->eeGetClassName(CORINFO_CLASS_HANDLE(compileTimeHandle0)), dspPtr(compileTimeHandle1),
            m_pComp->eeGetClassName(CORINFO_CLASS_HANDLE(compileTimeHandle1)));

    ValueNum               result = NoVN;
    const TypeCompareState s =
        m_pComp->info.compCompHnd->compareTypesForEquality(CORINFO_CLASS_HANDLE(compileTimeHandle0),
                                                           CORINFO_CLASS_HANDLE(compileTimeHandle1));
    if (s != TypeCompareState::May)
    {
        const bool typesAreEqual = (s == TypeCompareState::Must);
        const bool operatorIsEQ  = (oper == GT_EQ);
        const int  compareResult = operatorIsEQ ^ typesAreEqual ? 0 : 1;
        JITDUMP("Runtime reports comparison is known at jit time: %u\n", compareResult);
        result = VNForIntCon(compareResult);
    }

    return result;
}

//------------------------------------------------------------------------
// VNEvalCanFoldBinaryFunc: Can the given binary function be constant-folded?
//
// Arguments:
//    type   - The result type
//    func   - The function
//    arg0VN - VN of the first argument
//    arg1VN - VN of the second argument
//
// Return Value:
//    Whether the caller can constant-fold "func" with the given arguments.
//
// Notes:
//    Returning "true" from this method implies support for evaluating the
//    function in "EvalFuncForConstantArgs" (one of its callees).
//
bool ValueNumStore::VNEvalCanFoldBinaryFunc(var_types type, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    if (!IsVNConstant(arg0VN) || !IsVNConstant(arg1VN))
    {
        return false;
    }

    if (func < VNF_Boundary)
    {
        switch (genTreeOps(func))
        {
            case GT_ADD:
            case GT_SUB:
            case GT_MUL:
            case GT_DIV:
            case GT_MOD:

            case GT_UDIV:
            case GT_UMOD:

            case GT_AND:
            case GT_OR:
            case GT_XOR:

            case GT_LSH:
            case GT_RSH:
            case GT_RSZ:
            case GT_ROL:
            case GT_ROR:

            case GT_EQ:
            case GT_NE:
            case GT_GT:
            case GT_GE:
            case GT_LT:
            case GT_LE:
                break;

            default:
                return false;
        }
    }
    else
    {
        switch (func)
        {
            case VNF_GT_UN:
            case VNF_GE_UN:
            case VNF_LT_UN:
            case VNF_LE_UN:

            case VNF_ADD_OVF:
            case VNF_SUB_OVF:
            case VNF_MUL_OVF:
            case VNF_ADD_UN_OVF:
            case VNF_SUB_UN_OVF:
            case VNF_MUL_UN_OVF:

            case VNF_Cast:
            case VNF_CastOvf:
                if ((type != TYP_I_IMPL) && IsVNHandle(arg0VN))
                {
                    return false;
                }
                break;

            case VNF_BitCast:
                if (!varTypeIsArithmetic(type) || IsVNHandle(arg0VN))
                {
                    return false;
                }
                break;

            default:
                return false;
        }
    }

    // It is possible for us to have mismatched types (see Bug 750863)
    // We don't try to fold a binary operation when one of the constant operands
    // is a floating-point constant and the other is not, except for casts.
    // For casts, the second operand just carries the information about the type.

    var_types arg0VNtyp      = TypeOfVN(arg0VN);
    bool      arg0IsFloating = varTypeIsFloating(arg0VNtyp);

    var_types arg1VNtyp      = TypeOfVN(arg1VN);
    bool      arg1IsFloating = varTypeIsFloating(arg1VNtyp);

    if (!VNFuncIsNumericCast(func) && (func != VNF_BitCast) && (arg0IsFloating != arg1IsFloating))
    {
        return false;
    }

    if (type == TYP_BYREF)
    {
        // We don't want to fold expressions that produce TYP_BYREF
        return false;
    }

    return true;
}

//------------------------------------------------------------------------
// VNEvalCanFoldUnaryFunc: Can the given unary function be constant-folded?
//
// Arguments:
//    type   - The result type
//    func   - The function
//    arg0VN - VN of the argument
//
// Return Value:
//    Whether the caller can constant-fold "func" with the given argument.
//
// Notes:
//    Returning "true" from this method implies support for evaluating the
//    function in "EvalFuncForConstantArgs" (one of its callees).
//
bool ValueNumStore::VNEvalCanFoldUnaryFunc(var_types typ, VNFunc func, ValueNum arg0VN)
{
    if (!IsVNConstant(arg0VN))
    {
        return false;
    }

    if (func < VNF_Boundary)
    {
        switch (genTreeOps(func))
        {
            case GT_NEG:
            case GT_NOT:
            case GT_BSWAP16:
            case GT_BSWAP:
                return true;

            default:
                return false;
        }
    }

    return false;
}

//----------------------------------------------------------------------------------------
//  VNEvalShouldFold - Returns true if we should perform the folding operation.
//                     It returns false if we don't want to fold the expression,
//                     because it will always throw an exception.
//
// Arguments:
//    typ            - The type of the resulting ValueNum produced by 'func'
//    func           - Any binary VNFunc
//    arg0VN         - The ValueNum of the first argument to 'func'
//    arg1VN         - The ValueNum of the second argument to 'func'
//
// Return Value:     - Returns true if we should perform a folding operation.
//
// Notes:            - Does not handle operations producing TYP_BYREF.
//
bool ValueNumStore::VNEvalShouldFold(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(typ != TYP_BYREF);

    // We have some arithmetic operations that will always throw
    // an exception given particular constant argument(s).
    // (i.e. integer division by zero)
    //
    // We will avoid performing any constant folding on them
    // since they won't actually produce any result.
    // Instead they always will throw an exception.

    // Floating point operations do not throw exceptions.
    if (varTypeIsFloating(typ))
    {
        return true;
    }

    genTreeOps oper = genTreeOps(func);
    // Is this an integer divide/modulo that will always throw an exception?
    if (GenTree::StaticOperIs(oper, GT_DIV, GT_UDIV, GT_MOD, GT_UMOD))
    {
        if (!((typ == TYP_INT) || (typ == TYP_LONG)))
        {
            assert(!"Unexpected type in VNEvalShouldFold for integer division/modulus");
            return false;
        }
        // Just in case we have mismatched types.
        if ((TypeOfVN(arg0VN) != typ) || (TypeOfVN(arg1VN) != typ))
        {
            return false;
        }

        INT64 divisor = CoercedConstantValue<INT64>(arg1VN);

        if (divisor == 0)
        {
            // Don't fold, we have a divide by zero.
            return false;
        }
        else if ((oper == GT_DIV || oper == GT_MOD) && (divisor == -1))
        {
            // Don't fold if we have a division of INT32_MIN or INT64_MIN by -1.
            // Note that while INT_MIN % -1 is mathematically well-defined (and equal to 0),
            // we still give up on folding it because the "idiv" instruction is used to compute it on x64.
            // And "idiv" raises an exception on such inputs.
            INT64 dividend    = CoercedConstantValue<INT64>(arg0VN);
            INT64 badDividend = typ == TYP_INT ? INT32_MIN : INT64_MIN;

            // Only fold if our dividend is good.
            return dividend != badDividend;
        }
    }

    // Is this a checked operation that will always throw an exception?
    if (VNFuncIsOverflowArithmetic(func))
    {
        if (typ == TYP_INT)
        {
            int op1 = ConstantValue<int>(arg0VN);
            int op2 = ConstantValue<int>(arg1VN);

            switch (func)
            {
                case VNF_ADD_OVF:
                    return !CheckedOps::AddOverflows(op1, op2, CheckedOps::Signed);
                case VNF_SUB_OVF:
                    return !CheckedOps::SubOverflows(op1, op2, CheckedOps::Signed);
                case VNF_MUL_OVF:
                    return !CheckedOps::MulOverflows(op1, op2, CheckedOps::Signed);
                case VNF_ADD_UN_OVF:
                    return !CheckedOps::AddOverflows(op1, op2, CheckedOps::Unsigned);
                case VNF_SUB_UN_OVF:
                    return !CheckedOps::SubOverflows(op1, op2, CheckedOps::Unsigned);
                case VNF_MUL_UN_OVF:
                    return !CheckedOps::MulOverflows(op1, op2, CheckedOps::Unsigned);
                default:
                    assert(!"Unexpected checked operation in VNEvalShouldFold");
                    return false;
            }
        }
        else if (typ == TYP_LONG)
        {
            INT64 op1 = ConstantValue<INT64>(arg0VN);
            INT64 op2 = ConstantValue<INT64>(arg1VN);

            switch (func)
            {
                case VNF_ADD_OVF:
                    return !CheckedOps::AddOverflows(op1, op2, CheckedOps::Signed);
                case VNF_SUB_OVF:
                    return !CheckedOps::SubOverflows(op1, op2, CheckedOps::Signed);
                case VNF_MUL_OVF:
                    return !CheckedOps::MulOverflows(op1, op2, CheckedOps::Signed);
                case VNF_ADD_UN_OVF:
                    return !CheckedOps::AddOverflows(op1, op2, CheckedOps::Unsigned);
                case VNF_SUB_UN_OVF:
                    return !CheckedOps::SubOverflows(op1, op2, CheckedOps::Unsigned);
                case VNF_MUL_UN_OVF:
                    return !CheckedOps::MulOverflows(op1, op2, CheckedOps::Unsigned);
                default:
                    assert(!"Unexpected checked operation in VNEvalShouldFold");
                    return false;
            }
        }
        else
        {
            assert(!"Unexpected type in VNEvalShouldFold for overflow arithmetic");
            return false;
        }
    }

    // Is this a checked cast that will always throw an exception or one with an implementation-defined result?
    if (VNFuncIsNumericCast(func))
    {
        var_types castFromType = TypeOfVN(arg0VN);

        // By policy, we do not fold conversions from floating-point types that result in
        // overflow, as the value the C++ compiler gives us does not always match our own codegen.
        if ((func == VNF_CastOvf) || varTypeIsFloating(castFromType))
        {
            var_types castToType;
            bool      fromUnsigned;
            GetCastOperFromVN(arg1VN, &castToType, &fromUnsigned);

            switch (castFromType)
            {
                case TYP_INT:
                    return !CheckedOps::CastFromIntOverflows(GetConstantInt32(arg0VN), castToType, fromUnsigned);
                case TYP_LONG:
                    return !CheckedOps::CastFromLongOverflows(GetConstantInt64(arg0VN), castToType, fromUnsigned);
                case TYP_FLOAT:
                    return !CheckedOps::CastFromFloatOverflows(GetConstantSingle(arg0VN), castToType);
                case TYP_DOUBLE:
                    return !CheckedOps::CastFromDoubleOverflows(GetConstantDouble(arg0VN), castToType);
                default:
                    return false;
            }
        }
    }

    return true;
}

//----------------------------------------------------------------------------------------
//  EvalUsingMathIdentity
//                   - Attempts to evaluate 'func' by using mathematical identities
//                     that can be applied to 'func'.
//
// Arguments:
//    typ            - The type of the resulting ValueNum produced by 'func'
//    func           - Any binary VNFunc
//    arg0VN         - The ValueNum of the first argument to 'func'
//    arg1VN         - The ValueNum of the second argument to 'func'
//
// Return Value:     - When successful a  ValueNum for the expression is returned.
//                     When unsuccessful NoVN is returned.
//
ValueNum ValueNumStore::EvalUsingMathIdentity(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    ValueNum resultVN = NoVN; // set default result to unsuccessful

    if (typ == TYP_BYREF) // We don't want/need to optimize a zero byref
    {
        return resultVN; // return the unsuccessful value
    }

    // (0 + x) == x
    // (x + 0) == x
    // This identity does not apply for floating point (when x == -0.0).
    auto identityForAddition = [=]() -> ValueNum {
        if (!varTypeIsFloating(typ))
        {
            ValueNum ZeroVN = VNZeroForType(typ);
            if (arg0VN == ZeroVN)
            {
                return arg1VN;
            }
            else if (arg1VN == ZeroVN)
            {
                return arg0VN;
            }
        }

        return NoVN;
    };

    // (x - 0) == x
    // (x - x) == 0
    // This identity does not apply for floating point (when x == -0.0).
    auto identityForSubtraction = [=](bool ovf) -> ValueNum {
        if (!varTypeIsFloating(typ))
        {
            ValueNum ZeroVN = VNZeroForType(typ);
            if (arg1VN == ZeroVN)
            {
                return arg0VN;
            }
            else if (arg0VN == arg1VN)
            {
                return ZeroVN;
            }

            if (!ovf)
            {
                // (x + a) - x == a
                // (a + x) - x == a
                VNFuncApp add;
                if (GetVNFunc(arg0VN, &add) && (add.m_func == (VNFunc)GT_ADD))
                {
                    if (add.m_args[0] == arg1VN)
                        return add.m_args[1];
                    if (add.m_args[1] == arg1VN)
                        return add.m_args[0];

                    // (x + a) - (x + b) == a - b
                    // (a + x) - (x + b) == a - b
                    // (x + a) - (b + x) == a - b
                    // (a + x) - (b + x) == a - b
                    VNFuncApp add2;
                    if (GetVNFunc(arg1VN, &add2) && (add2.m_func == (VNFunc)GT_ADD))
                    {
                        for (int a = 0; a < 2; a++)
                        {
                            for (int b = 0; b < 2; b++)
                            {
                                if (add.m_args[a] == add2.m_args[b])
                                {
                                    return VNForFunc(typ, (VNFunc)GT_SUB, add.m_args[1 - a], add2.m_args[1 - b]);
                                }
                            }
                        }
                    }
                }
            }
        }

        return NoVN;
    };

    // These identities do not apply for floating point.
    auto identityForMultiplication = [=]() -> ValueNum {
        if (!varTypeIsFloating(typ))
        {
            // (0 * x) == 0
            // (x * 0) == 0
            // This identity does not apply for floating-point (when x == -0.0, NaN, +Inf, -Inf)
            ValueNum ZeroVN = VNZeroForType(typ);
            if (arg0VN == ZeroVN)
            {
                return ZeroVN;
            }
            else if (arg1VN == ZeroVN)
            {
                return ZeroVN;
            }
        }

        // (x * 1) == x
        // (1 * x) == x
        // This is safe for all floats since we do not fault for sNaN
        ValueNum OneVN = VNOneForType(typ);
        if (arg0VN == OneVN)
        {
            return arg1VN;
        }
        else if (arg1VN == OneVN)
        {
            return arg0VN;
        }

        return NoVN;
    };

    // We have ways of evaluating some binary functions.
    if (func < VNF_Boundary)
    {
        ValueNum ZeroVN;
        ValueNum OneVN;
        ValueNum AllBitsVN;

        switch (genTreeOps(func))
        {
            case GT_ADD:
                resultVN = identityForAddition();
                break;

            case GT_SUB:
                resultVN = identityForSubtraction(/* ovf */ false);
                break;

            case GT_MUL:
                resultVN = identityForMultiplication();
                break;

            case GT_DIV:
            case GT_UDIV:
            {
                // (x / 1) == x
                // This is safe for all floats since we do not fault for sNaN
                OneVN = VNOneForType(typ);

                if (arg1VN == OneVN)
                {
                    resultVN = arg0VN;
                }
                break;
            }

            case GT_OR:
            {
                // (0 | x) == x
                // (x | 0) == x
                ZeroVN = VNZeroForType(typ);
                if (arg0VN == ZeroVN)
                {
                    resultVN = arg1VN;
                    break;
                }
                else if (arg1VN == ZeroVN)
                {
                    resultVN = arg0VN;
                    break;
                }

                // (x | ~0) == ~0
                // (~0 | x) == ~0
                AllBitsVN = VNAllBitsForType(typ);
                if (arg0VN == AllBitsVN)
                {
                    resultVN = AllBitsVN;
                    break;
                }
                else if (arg1VN == AllBitsVN)
                {
                    resultVN = AllBitsVN;
                    break;
                }

                // x | x == x
                if (arg0VN == arg1VN)
                {
                    resultVN = arg0VN;
                }
                break;
            }

            case GT_XOR:
            {
                // (0 ^ x) == x
                // (x ^ 0) == x
                ZeroVN = VNZeroForType(typ);
                if (arg0VN == ZeroVN)
                {
                    resultVN = arg1VN;
                    break;
                }
                else if (arg1VN == ZeroVN)
                {
                    resultVN = arg0VN;
                    break;
                }

                // x ^ x == 0
                if (arg0VN == arg1VN)
                {
                    resultVN = ZeroVN;
                }
                break;
            }

            case GT_AND:
            {
                // (x & 0) == 0
                // (0 & x) == 0
                ZeroVN = VNZeroForType(typ);
                if (arg0VN == ZeroVN)
                {
                    resultVN = ZeroVN;
                    break;
                }
                else if (arg1VN == ZeroVN)
                {
                    resultVN = ZeroVN;
                    break;
                }

                // (x & ~0) == x
                // (~0 & x) == x
                AllBitsVN = VNAllBitsForType(typ);
                if (arg0VN == AllBitsVN)
                {
                    resultVN = arg1VN;
                    break;
                }
                else if (arg1VN == AllBitsVN)
                {
                    resultVN = arg0VN;
                    break;
                }

                // x & x == x
                if (arg0VN == arg1VN)
                {
                    resultVN = arg0VN;
                }
                break;
            }

            case GT_LSH:
            case GT_RSH:
            case GT_RSZ:
            case GT_ROL:
            case GT_ROR:
            {
                // (x << 0)  == x
                // (x >> 0)  == x
                // (x rol 0) == x
                // (x ror 0) == x
                ZeroVN = VNZeroForType(typ);
                if (arg1VN == ZeroVN)
                {
                    resultVN = arg0VN;
                }

                // (0 << x)  == 0
                // (0 >> x)  == 0
                // (0 rol x) == 0
                // (0 ror x) == 0
                if (arg0VN == ZeroVN)
                {
                    resultVN = ZeroVN;
                }
                break;
            }

            case GT_EQ:
                // (null == non-null) == false
                // (non-null == null) == false
                if (((arg0VN == VNForNull()) && IsKnownNonNull(arg1VN)) ||
                    ((arg1VN == VNForNull()) && IsKnownNonNull(arg0VN)))
                {
                    resultVN = VNZeroForType(typ);
                    break;
                }
                // (relop == 0) == !relop
                ZeroVN = VNZeroForType(typ);
                if (IsVNRelop(arg0VN) && (arg1VN == ZeroVN))
                {
                    ValueNum rev0VN = GetRelatedRelop(arg0VN, VN_RELATION_KIND::VRK_Reverse);
                    if (rev0VN != NoVN)
                    {
                        resultVN = rev0VN;
                        break;
                    }
                }
                else if (IsVNRelop(arg1VN) && (arg0VN == ZeroVN))
                {
                    ValueNum rev1VN = GetRelatedRelop(arg1VN, VN_RELATION_KIND::VRK_Reverse);
                    if (rev1VN != NoVN)
                    {
                        resultVN = rev1VN;
                        break;
                    }
                }
                // (relop == 1) == relop
                OneVN = VNOneForType(typ);
                if (IsVNRelop(arg0VN) && (arg1VN == OneVN))
                {
                    resultVN = arg0VN;
                    break;
                }
                else if (IsVNRelop(arg1VN) && (arg0VN == OneVN))
                {
                    resultVN = arg1VN;
                    break;
                }
                // (x == x) == true (integer only)
                FALLTHROUGH;
            case GT_GE:
            case GT_LE:
                // (x <= x) == true (integer only)
                // (x >= x) == true (integer only)
                if ((arg0VN == arg1VN) && varTypeIsIntegralOrI(TypeOfVN(arg0VN)))
                {
                    resultVN = VNOneForType(typ);
                }
                else if (varTypeIsIntegralOrI(TypeOfVN(arg0VN)))
                {
                    ZeroVN = VNZeroForType(typ);
                    if (genTreeOps(func) == GT_GE)
                    {
                        // (never negative) >= 0 == true
                        if ((arg1VN == ZeroVN) && IsVNNeverNegative(arg0VN))
                        {
                            resultVN = VNOneForType(typ);
                        }
                    }
                    else if (genTreeOps(func) == GT_LE)
                    {
                        // 0 <= (never negative) == true
                        if ((arg0VN == ZeroVN) && IsVNNeverNegative(arg1VN))
                        {
                            resultVN = VNOneForType(typ);
                        }
                    }
                }
                break;

            case GT_NE:
                // (null != non-null) == true
                // (non-null != null) == true
                if (((arg0VN == VNForNull()) && IsKnownNonNull(arg1VN)) ||
                    ((arg1VN == VNForNull()) && IsKnownNonNull(arg0VN)))
                {
                    resultVN = VNOneForType(typ);
                    break;
                }
                // (x != x) == false (integer only)
                else if ((arg0VN == arg1VN) && varTypeIsIntegralOrI(TypeOfVN(arg0VN)))
                {
                    resultVN = VNZeroForType(typ);
                    break;
                }
                // (relop != 0) == relop
                ZeroVN = VNZeroForType(typ);
                if (IsVNRelop(arg0VN) && (arg1VN == ZeroVN))
                {
                    resultVN = arg0VN;
                    break;
                }
                else if (IsVNRelop(arg1VN) && (arg0VN == ZeroVN))
                {
                    resultVN = arg1VN;
                    break;
                }
                // (relop != 1) == !relop
                OneVN = VNOneForType(typ);
                if (IsVNRelop(arg0VN) && (arg1VN == OneVN))
                {
                    ValueNum rev0VN = GetRelatedRelop(arg0VN, VN_RELATION_KIND::VRK_Reverse);
                    if (rev0VN != NoVN)
                    {
                        resultVN = rev0VN;
                        break;
                    }
                }
                else if (IsVNRelop(arg1VN) && (arg0VN == OneVN))
                {
                    ValueNum rev1VN = GetRelatedRelop(arg1VN, VN_RELATION_KIND::VRK_Reverse);
                    if (rev1VN != NoVN)
                    {
                        resultVN = rev1VN;
                        break;
                    }
                }
                break;

            case GT_GT:
            case GT_LT:
                // (x > x) == false (integer & floating point)
                // (x < x) == false (integer & floating point)
                if (arg0VN == arg1VN)
                {
                    resultVN = VNZeroForType(typ);
                }
                else if (varTypeIsIntegralOrI(TypeOfVN(arg0VN)))
                {
                    ZeroVN = VNZeroForType(typ);
                    if (genTreeOps(func) == GT_LT)
                    {
                        // (never negative) < 0 == false
                        if ((arg1VN == ZeroVN) && IsVNNeverNegative(arg0VN))
                        {
                            resultVN = ZeroVN;
                        }
                    }
                    else if (genTreeOps(func) == GT_GT)
                    {
                        // 0 > (never negative) == false
                        if ((arg0VN == ZeroVN) && IsVNNeverNegative(arg1VN))
                        {
                            resultVN = ZeroVN;
                        }
                    }
                }
                break;

            default:
                break;
        }
    }
    else // must be a VNF_ function
    {
        switch (func)
        {
            case VNF_ADD_OVF:
            case VNF_ADD_UN_OVF:
                resultVN = identityForAddition();
                break;

            case VNF_SUB_OVF:
            case VNF_SUB_UN_OVF:
                resultVN = identityForSubtraction(/* ovf */ true);
                break;

            case VNF_MUL_OVF:
            case VNF_MUL_UN_OVF:
                resultVN = identityForMultiplication();
                break;

            case VNF_LT_UN:
                // (x < 0) == false
                // (x < x) == false
                std::swap(arg0VN, arg1VN);
                FALLTHROUGH;
            case VNF_GT_UN:
                // (0 > x) == false
                // (x > x) == false
                // None of the above identities apply to floating point comparisons.
                // For example, (NaN > NaN) is true instead of false because these are
                // unordered comparisons.
                if (varTypeIsIntegralOrI(TypeOfVN(arg0VN)) &&
                    ((arg0VN == VNZeroForType(TypeOfVN(arg0VN))) || (arg0VN == arg1VN)))
                {
                    resultVN = VNZeroForType(typ);
                }
                break;

            case VNF_GE_UN:
                // (x >= 0) == true
                // (x >= x) == true
                std::swap(arg0VN, arg1VN);
                FALLTHROUGH;
            case VNF_LE_UN:
                // (0 <= x) == true
                // (x <= x) == true
                // Unlike (x < x) and (x > x), (x >= x) and (x <= x) also apply to floating
                // point comparisons: x is either equal to itself or is unordered if it's NaN.
                if ((varTypeIsIntegralOrI(TypeOfVN(arg0VN)) && (arg0VN == VNZeroForType(TypeOfVN(arg0VN)))) ||
                    (arg0VN == arg1VN))
                {
                    resultVN = VNOneForType(typ);
                }
                break;

            default:
                break;
        }
    }
    return resultVN;
}

//------------------------------------------------------------------------
// VNForExpr: Opaque value number that is equivalent to itself but unique
//    from all other value numbers.
//
// Arguments:
//    block - BasicBlock where the expression that produces this value occurs.
//            May be nullptr to force conservative "could be anywhere" interpretation.
//     type - Type of the expression in the IR
//
// Return Value:
//    A new value number distinct from any previously generated, that compares as equal
//    to itself, but not any other value number, and is annotated with the given
//    type and block.
//
ValueNum ValueNumStore::VNForExpr(BasicBlock* block, var_types type)
{
    unsigned loopIndex = ValueNumStore::UnknownLoop;
    if (block != nullptr)
    {
        FlowGraphNaturalLoop* loop = m_pComp->m_blockToLoop->GetLoop(block);
        loopIndex                  = loop == nullptr ? ValueNumStore::NoLoop : loop->GetIndex();
    }

    // VNForFunc(typ, func, vn) but bypasses looking in the cache
    //
    Chunk* const          c                 = GetAllocChunk(type, CEA_Func1);
    unsigned const        offsetWithinChunk = c->AllocVN();
    VNDefFuncAppFlexible* fapp              = c->PointerToFuncApp(offsetWithinChunk, 1);
    fapp->m_func                            = VNF_MemOpaque;
    fapp->m_args[0]                         = loopIndex;

    ValueNum resultVN = c->m_baseVN + offsetWithinChunk;
    return resultVN;
}

//------------------------------------------------------------------------
// VNPairForExpr - Create a "new, unique" pair of value numbers.
//
// "VNForExpr" equivalent for "ValueNumPair"s.
//
ValueNumPair ValueNumStore::VNPairForExpr(BasicBlock* block, var_types type)
{
    ValueNum     uniqVN = VNForExpr(block, type);
    ValueNumPair uniqVNP(uniqVN, uniqVN);

    return uniqVNP;
}

//------------------------------------------------------------------------
// VNForLoad: Get the VN for a load from a location (physical map).
//
// Arguments:
//    vnk           - The kind of VN to select (see "VNForMapSelectWork" notes)
//    locationValue - (VN of) the value location has
//    locationSize  - Size of the location
//    loadType      - Type being loaded
//    offset        - In-location offset being loaded from
//    loadSize      - Number of bytes being loaded
//
// Return Value:
//    Value number representing "locationValue[offset:offset + loadSize - 1]",
//    normalized to the same actual type as "loadType". Handles out-of-bounds
//    loads by returning a "new, unique" VN.
//
ValueNum ValueNumStore::VNForLoad(ValueNumKind vnk,
                                  ValueNum     locationValue,
                                  unsigned     locationSize,
                                  var_types    loadType,
                                  ssize_t      offset,
                                  unsigned     loadSize)
{
    assert((loadSize > 0));

    unsigned loadOffset = static_cast<unsigned>(offset);

    if ((offset < 0) || (locationSize < (loadOffset + loadSize)))
    {
        JITDUMP("    *** VNForLoad: out-of-bounds load!\n");
        return VNForExpr(m_pComp->compCurBB, loadType);
    }

    ValueNum loadValue;
    if (LoadStoreIsEntire(locationSize, loadOffset, loadSize))
    {
        loadValue = locationValue;
    }
    else
    {
        JITDUMP("  VNForLoad:\n");
        loadValue = VNForMapPhysicalSelect(vnk, loadType, locationValue, loadOffset, loadSize);
    }

    // Unlike with stores, loads we always normalize (to have the property that the tree's type
    // is the same as its VN's).
    loadValue = VNForLoadStoreBitCast(loadValue, loadType, loadSize);

    assert(genActualType(TypeOfVN(loadValue)) == genActualType(loadType));

    return loadValue;
}

//------------------------------------------------------------------------
// VNPairForLoad: VNForLoad applied to a ValueNumPair.
//
ValueNumPair ValueNumStore::VNPairForLoad(
    ValueNumPair locationValue, unsigned locationSize, var_types loadType, ssize_t offset, unsigned loadSize)
{
    ValueNum liberalVN = VNForLoad(VNK_Liberal, locationValue.GetLiberal(), locationSize, loadType, offset, loadSize);
    ValueNum conservVN =
        VNForLoad(VNK_Conservative, locationValue.GetConservative(), locationSize, loadType, offset, loadSize);

    return ValueNumPair(liberalVN, conservVN);
}

//------------------------------------------------------------------------
// VNForStore: Get the VN for a store to a location (physical map).
//
// Arguments:
//    locationValue - (VN of) the value location had before the store
//    locationSize  - Size of the location
//    offset        - In-location offset being stored to
//    storeSize     - Number of bytes being stored
//    value         - (VN of) the value being stored
//
// Return Value:
//    Value number for "locationValue" with "storeSize" bytes starting at
//    "offset" set to "value". "NoVN" in case of an out-of-bounds store
//    (the caller is expected to explicitly handle that).
//
// Notes:
//    Does not handle "entire" (whole/identity) stores.
//
ValueNum ValueNumStore::VNForStore(
    ValueNum locationValue, unsigned locationSize, ssize_t offset, unsigned storeSize, ValueNum value)
{
    assert(storeSize > 0);

    // The caller is expected to handle identity stores, applying the appropriate normalization policy.
    assert(!LoadStoreIsEntire(locationSize, offset, storeSize));

    unsigned storeOffset = static_cast<unsigned>(offset);

    if ((offset < 0) || (locationSize < (storeOffset + storeSize)))
    {
        JITDUMP("    *** VNForStore: out-of-bounds store -- location size is %u, offset is %zd, store size is %u\n",
                locationSize, offset, storeSize);
        // Some callers will need to invalidate parenting maps, so force explicit
        // handling of this case instead of returning a "new, unique" VN.
        return NoVN;
    }

    JITDUMP("  VNForStore:\n");
    return VNForMapPhysicalStore(locationValue, storeOffset, storeSize, value);
}

//------------------------------------------------------------------------
// VNPairForStore: VNForStore applied to a ValueNumPair.
//
ValueNumPair ValueNumStore::VNPairForStore(
    ValueNumPair locationValue, unsigned locationSize, ssize_t offset, unsigned storeSize, ValueNumPair value)
{
    ValueNum liberalVN = VNForStore(locationValue.GetLiberal(), locationSize, offset, storeSize, value.GetLiberal());
    ValueNum conservVN;
    if (locationValue.BothEqual() && value.BothEqual())
    {
        conservVN = liberalVN;
    }
    else
    {
        conservVN =
            VNForStore(locationValue.GetConservative(), locationSize, offset, storeSize, value.GetConservative());
    }

    return ValueNumPair(liberalVN, conservVN);
}

//------------------------------------------------------------------------
// VNForLoadStoreBitCast: Normalize a value number to the desired type.
//
// Arguments:
//    value   - (VN of) the value needed normalization
//    indType - The type to normalize to
//    indSize - The size of "indType" and "value" (relevant for structs)
//
// Return Value:
//    Value number the logical "BitCast<indType>(value)".
//
// Notes:
//    As far as the physical maps are concerned, all values with the same
//    size "are equal". However, both IR and the rest of VN do distinguish
//    between "4 bytes of TYP_INT" and "4 bytes of TYP_FLOAT". This method
//    is called in cases where that gap needs to be bridged and the value
//    "normalized" to the appropriate type. Notably, this normalization is
//    only performed for primitives -- TYP_STRUCTs of different handles but
//    same size are treated as equal (intentionally so -- this is good from
//    CQ, TP and simplicity standpoints).
//
ValueNum ValueNumStore::VNForLoadStoreBitCast(ValueNum value, var_types indType, unsigned indSize)
{
    var_types typeOfValue = TypeOfVN(value);

    if (typeOfValue != indType)
    {
        assert((typeOfValue == TYP_STRUCT) || (indType == TYP_STRUCT) || (genTypeSize(indType) == indSize));

        value = VNForBitCast(value, indType, indSize);

        JITDUMP("    VNForLoadStoreBitcast returns ");
        JITDUMPEXEC(m_pComp->vnPrint(value, 1));
        JITDUMP("\n");
    }

    assert(genActualType(TypeOfVN(value)) == genActualType(indType));

    return value;
}

//------------------------------------------------------------------------
// VNPairForLoadStoreBitCast: VNForLoadStoreBitCast applied to a ValueNumPair.
//
ValueNumPair ValueNumStore::VNPairForLoadStoreBitCast(ValueNumPair value, var_types indType, unsigned indSize)
{
    ValueNum liberalVN = VNForLoadStoreBitCast(value.GetLiberal(), indType, indSize);
    ValueNum conservVN;
    if (value.BothEqual())
    {
        conservVN = liberalVN;
    }
    else
    {
        conservVN = VNForLoadStoreBitCast(value.GetConservative(), indType, indSize);
    }

    return ValueNumPair(liberalVN, conservVN);
}

//------------------------------------------------------------------------
// VNForFieldSeq: Get the value number representing a field sequence.
//
// Arguments:
//    fieldSeq - the field sequence
//
// Return Value:
//    "GTF_FIELD_SEQ_PTR" handle VN for the sequences.
//
ValueNum ValueNumStore::VNForFieldSeq(FieldSeq* fieldSeq)
{
    // This encoding relies on the canonicality of field sequences.
    ValueNum fieldSeqVN = VNForHandle(reinterpret_cast<ssize_t>(fieldSeq), GTF_ICON_FIELD_SEQ);

#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("    ");
        vnDump(m_pComp, fieldSeqVN);
        printf(" is " FMT_VN "\n", fieldSeqVN);
    }
#endif

    return fieldSeqVN;
}

//------------------------------------------------------------------------
// FieldSeqVNToFieldSeq: Decode the field sequence from a VN representing one.
//
// Arguments:
//    vn - the value number, must be one obtained using "VNForFieldSeq"
//
// Return Value:
//    The field sequence associated with "vn".
//
FieldSeq* ValueNumStore::FieldSeqVNToFieldSeq(ValueNum vn)
{
    assert(IsVNHandle(vn) && (GetHandleFlags(vn) == GTF_ICON_FIELD_SEQ));

    return reinterpret_cast<FieldSeq*>(ConstantValue<ssize_t>(vn));
}

ValueNum ValueNumStore::ExtendPtrVN(GenTree* opA, GenTree* opB)
{
    if (opB->OperGet() == GT_CNS_INT)
    {
        return ExtendPtrVN(opA, opB->AsIntCon()->gtFieldSeq, opB->AsIntCon()->IconValue());
    }

    return NoVN;
}

ValueNum ValueNumStore::ExtendPtrVN(GenTree* opA, FieldSeq* fldSeq, ssize_t offset)
{
    ValueNum res = NoVN;

    ValueNum opAvnWx = opA->gtVNPair.GetLiberal();
    assert(VNIsValid(opAvnWx));
    ValueNum opAvn;
    ValueNum opAvnx;
    VNUnpackExc(opAvnWx, &opAvn, &opAvnx);
    assert(VNIsValid(opAvn) && VNIsValid(opAvnx));

    VNFuncApp funcApp;
    if (!GetVNFunc(opAvn, &funcApp))
    {
        return res;
    }

    if (funcApp.m_func == VNF_PtrToStatic)
    {
        fldSeq = m_pComp->GetFieldSeqStore()->Append(FieldSeqVNToFieldSeq(funcApp.m_args[1]), fldSeq);
        res    = VNForFunc(TYP_BYREF, VNF_PtrToStatic, funcApp.m_args[0], VNForFieldSeq(fldSeq),
                        VNForIntPtrCon(ConstantValue<ssize_t>(funcApp.m_args[2]) + offset));
    }
    else if (funcApp.m_func == VNF_PtrToArrElem)
    {
        res = VNForFunc(TYP_BYREF, VNF_PtrToArrElem, funcApp.m_args[0], funcApp.m_args[1], funcApp.m_args[2],
                        VNForIntPtrCon(ConstantValue<ssize_t>(funcApp.m_args[3]) + offset));
    }
    if (res != NoVN)
    {
        res = VNWithExc(res, opAvnx);
    }

    return res;
}

//------------------------------------------------------------------------
// fgValueNumberLocalStore: Assign VNs to the SSA definition corresponding
//                          to a local store.
//
// Or update the current heap state in case the local was address-exposed.
//
// Arguments:
//    storeNode  - The node performing the store
//    lclDefNode - The local node representing the SSA definition
//    offset     - The offset, relative to the local, of the target location
//    storeSize  - The number of bytes being stored
//    value      - (VN of) the value being stored
//    normalize  - Whether "value" should be normalized to the local's type
//                 (in case the store overwrites the entire variable) before
//                 being written to the SSA descriptor
//
void Compiler::fgValueNumberLocalStore(GenTree*             storeNode,
                                       GenTreeLclVarCommon* lclDefNode,
                                       ssize_t              offset,
                                       unsigned             storeSize,
                                       ValueNumPair         value,
                                       bool                 normalize)
{
    // Should not have been recorded as updating the GC heap.
    assert(!GetMemorySsaMap(GcHeap)->Lookup(storeNode));

    auto processDef = [=](unsigned defLclNum, unsigned defSsaNum, ssize_t defOffset, unsigned defSize,
                          ValueNumPair defValue) {

        LclVarDsc* defVarDsc = lvaGetDesc(defLclNum);

        if (defSsaNum != SsaConfig::RESERVED_SSA_NUM)
        {
            unsigned lclSize = lvaLclExactSize(defLclNum);

            ValueNumPair newLclValue;
            if (vnStore->LoadStoreIsEntire(lclSize, defOffset, defSize))
            {
                newLclValue = defValue;
            }
            else
            {
                assert((lclDefNode->gtFlags & GTF_VAR_USEASG) != 0);
                unsigned     oldDefSsaNum = defVarDsc->GetPerSsaData(defSsaNum)->GetUseDefSsaNum();
                ValueNumPair oldLclValue  = defVarDsc->GetPerSsaData(oldDefSsaNum)->m_vnPair;
                newLclValue               = vnStore->VNPairForStore(oldLclValue, lclSize, defOffset, defSize, defValue);
            }

            // Any out-of-bounds stores should have made the local address-exposed.
            assert(newLclValue.BothDefined());

            if (normalize)
            {
                // We normalize types stored in local locations because things outside VN itself look at them.
                newLclValue = vnStore->VNPairForLoadStoreBitCast(newLclValue, defVarDsc->TypeGet(), lclSize);
                assert((genActualType(vnStore->TypeOfVN(newLclValue.GetLiberal())) == genActualType(defVarDsc)));
            }

            defVarDsc->GetPerSsaData(defSsaNum)->m_vnPair = newLclValue;

            JITDUMP("Tree [%06u] assigned VN to local var V%02u/%d: ", dspTreeID(storeNode), defLclNum, defSsaNum);
            JITDUMPEXEC(vnpPrint(newLclValue, 1));
            JITDUMP("\n");
        }
        else if (defVarDsc->IsAddressExposed())
        {
            ValueNum heapVN = vnStore->VNForExpr(compCurBB, TYP_HEAP);
            recordAddressExposedLocalStore(storeNode, heapVN DEBUGARG("local assign"));
        }
        else
        {
            JITDUMP("Tree [%06u] assigns to non-address-taken local V%02u; excluded from SSA, so value not tracked\n",
                    dspTreeID(storeNode), defLclNum);
        }
    };

    if (lclDefNode->HasCompositeSsaName())
    {
        LclVarDsc* varDsc = lvaGetDesc(lclDefNode);
        assert(varDsc->lvPromoted);

        for (unsigned index = 0; index < varDsc->lvFieldCnt; index++)
        {
            unsigned   fieldLclNum = varDsc->lvFieldLclStart + index;
            LclVarDsc* fieldVarDsc = lvaGetDesc(fieldLclNum);

            ssize_t  fieldStoreOffset;
            unsigned fieldStoreSize;
            if (gtStoreDefinesField(fieldVarDsc, offset, storeSize, &fieldStoreOffset, &fieldStoreSize))
            {
                // TYP_STRUCT can represent the general case where the value could be of any size.
                var_types fieldStoreType = TYP_STRUCT;
                if (vnStore->LoadStoreIsEntire(genTypeSize(fieldVarDsc), fieldStoreOffset, fieldStoreSize))
                {
                    // Avoid redundant bitcasts for the common case of a full definition.
                    fieldStoreType = fieldVarDsc->TypeGet();
                }

                // Calculate offset of this field's value, relative to the entire one.
                ssize_t      fieldOffset      = fieldVarDsc->lvFldOffset;
                ssize_t      fieldValueOffset = (fieldOffset < offset) ? 0 : (fieldOffset - offset);
                ValueNumPair fieldStoreValue =
                    vnStore->VNPairForLoad(value, storeSize, fieldStoreType, fieldValueOffset, fieldStoreSize);

                processDef(fieldLclNum, lclDefNode->GetSsaNum(this, index), fieldStoreOffset, fieldStoreSize,
                           fieldStoreValue);
            }
        }
    }
    else
    {
        processDef(lclDefNode->GetLclNum(), lclDefNode->GetSsaNum(), offset, storeSize, value);
    }
}

//------------------------------------------------------------------------
// fgValueNumberArrayElemLoad: Value number a load from an array element.
//
// Arguments:
//    loadTree - The indirection tree performing the load
//    addrFunc - The "VNF_PtrToArrElem" function representing the address
//
// Notes:
//    Only assigns normal VNs to "loadTree".
//
void Compiler::fgValueNumberArrayElemLoad(GenTree* loadTree, VNFuncApp* addrFunc)
{
    assert(loadTree->OperIsIndir() && (addrFunc->m_func == VNF_PtrToArrElem));

    CORINFO_CLASS_HANDLE elemTypeEq = CORINFO_CLASS_HANDLE(vnStore->ConstantValue<ssize_t>(addrFunc->m_args[0]));
    ValueNum             arrVN      = addrFunc->m_args[1];
    ValueNum             inxVN      = addrFunc->m_args[2];
    ssize_t              offset     = vnStore->ConstantValue<ssize_t>(addrFunc->m_args[3]);

    // The VN inputs are required to be non-exceptional values.
    assert(arrVN == vnStore->VNNormalValue(arrVN));
    assert(inxVN == vnStore->VNNormalValue(inxVN));

    // Heap[elemTypeEq][arrVN][inx][offset + size].
    var_types elemType     = DecodeElemType(elemTypeEq);
    ValueNum  elemTypeEqVN = vnStore->VNForHandle(ssize_t(elemTypeEq), GTF_ICON_CLASS_HDL);
    JITDUMP("  Array element load: elemTypeEq is " FMT_VN " for %s[]\n", elemTypeEqVN,
            (elemType == TYP_STRUCT) ? eeGetClassName(elemTypeEq) : varTypeName(elemType));

    ValueNum hAtArrType = vnStore->VNForMapSelect(VNK_Liberal, TYP_MEM, fgCurMemoryVN[GcHeap], elemTypeEqVN);
    JITDUMP("  GcHeap[elemTypeEq: " FMT_VN "] is " FMT_VN "\n", elemTypeEqVN, hAtArrType);

    ValueNum hAtArrTypeAtArr = vnStore->VNForMapSelect(VNK_Liberal, TYP_MEM, hAtArrType, arrVN);
    JITDUMP("  GcHeap[elemTypeEq][array: " FMT_VN "] is " FMT_VN "\n", arrVN, hAtArrTypeAtArr);

    ValueNum wholeElem = vnStore->VNForMapSelect(VNK_Liberal, elemType, hAtArrTypeAtArr, inxVN);
    JITDUMP("  GcHeap[elemTypeEq][array][index: " FMT_VN "] is " FMT_VN "\n", inxVN, wholeElem);

    unsigned  elemSize = (elemType == TYP_STRUCT) ? info.compCompHnd->getClassSize(elemTypeEq) : genTypeSize(elemType);
    var_types loadType = loadTree->TypeGet();
    unsigned  loadSize = loadTree->AsIndir()->Size();
    ValueNum  loadValueVN = vnStore->VNForLoad(VNK_Liberal, wholeElem, elemSize, loadType, offset, loadSize);

    loadTree->gtVNPair.SetLiberal(loadValueVN);
    loadTree->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, loadType));
}

//------------------------------------------------------------------------
// fgValueNumberArrayElemStore: Update the current heap state after a store
//                              to an array element.
//
// Arguments:
//    storeNode - The store node
//    addrFunc  - The "VNF_PtrToArrElem" function representing the address
//    storeSize - The number of bytes being stored
//    value     - (VN of) the value being stored
//
void Compiler::fgValueNumberArrayElemStore(GenTree* storeNode, VNFuncApp* addrFunc, unsigned storeSize, ValueNum value)
{
    assert(addrFunc->m_func == VNF_PtrToArrElem);

    CORINFO_CLASS_HANDLE elemTypeEq = CORINFO_CLASS_HANDLE(vnStore->ConstantValue<ssize_t>(addrFunc->m_args[0]));
    ValueNum             arrVN      = addrFunc->m_args[1];
    ValueNum             inxVN      = addrFunc->m_args[2];
    ssize_t              offset     = vnStore->ConstantValue<ssize_t>(addrFunc->m_args[3]);

    bool      invalidateArray = false;
    var_types elemType        = DecodeElemType(elemTypeEq);
    ValueNum  elemTypeEqVN    = vnStore->VNForHandle(ssize_t(elemTypeEq), GTF_ICON_CLASS_HDL);
    JITDUMP("  Array element store: elemTypeEq is " FMT_VN " for %s[]\n", elemTypeEqVN,
            (elemType == TYP_STRUCT) ? eeGetClassName(elemTypeEq) : varTypeName(elemType));

    ValueNum hAtArrType = vnStore->VNForMapSelect(VNK_Liberal, TYP_MEM, fgCurMemoryVN[GcHeap], elemTypeEqVN);
    JITDUMP("  GcHeap[elemTypeEq: " FMT_VN "] is " FMT_VN "\n", elemTypeEqVN, hAtArrType);

    ValueNum hAtArrTypeAtArr = vnStore->VNForMapSelect(VNK_Liberal, TYP_MEM, hAtArrType, arrVN);
    JITDUMP("  GcHeap[elemTypeEq][array: " FMT_VN "] is " FMT_VN "\n", arrVN, hAtArrTypeAtArr);

    unsigned elemSize = (elemType == TYP_STRUCT) ? info.compCompHnd->getClassSize(elemTypeEq) : genTypeSize(elemType);

    // This is the value that should be stored at "arr[inx]".
    ValueNum newWholeElem = ValueNumStore::NoVN;

    if (vnStore->LoadStoreIsEntire(elemSize, offset, storeSize))
    {
        // For memory locations (as opposed to locals), we do not normalize types.
        newWholeElem = value;
    }
    else
    {
        ValueNum oldWholeElem = vnStore->VNForMapSelect(VNK_Liberal, elemType, hAtArrTypeAtArr, inxVN);
        JITDUMP("  GcHeap[elemTypeEq][array][index: " FMT_VN "] is " FMT_VN "\n", inxVN, oldWholeElem);

        newWholeElem = vnStore->VNForStore(oldWholeElem, elemSize, offset, storeSize, value);
    }

    if (newWholeElem != ValueNumStore::NoVN)
    {
        JITDUMP("  GcHeap[elemTypeEq][array][index: " FMT_VN "] = " FMT_VN ":\n", inxVN, newWholeElem);
        ValueNum newValAtArr = vnStore->VNForMapStore(hAtArrTypeAtArr, inxVN, newWholeElem);

        JITDUMP("  GcHeap[elemTypeEq][array: " FMT_VN "] = " FMT_VN ":\n", arrVN, newValAtArr);
        ValueNum newValAtArrType = vnStore->VNForMapStore(hAtArrType, arrVN, newValAtArr);

        JITDUMP("  GcHeap[elemTypeEq: " FMT_VN "] = " FMT_VN ":\n", elemTypeEqVN, newValAtArrType);
        ValueNum newHeapVN = vnStore->VNForMapStore(fgCurMemoryVN[GcHeap], elemTypeEqVN, newValAtArrType);

        recordGcHeapStore(storeNode, newHeapVN DEBUGARG("array element store"));
    }
    else
    {
        // An out-of-bounds store: invalidate the whole heap, for simplicity.
        fgMutateGcHeap(storeNode DEBUGARG("out-of-bounds array element store"));
    }
}

//------------------------------------------------------------------------
// fgValueNumberFieldLoad: Value number a class/static field load.
//
// Arguments:
//    loadTree - The indirection tree performing the load
//    baseAddr - The "base address" of the field (see "GenTree::IsFieldAddr")
//    fieldSeq - The field sequence representing the address
//    offset   - The offset, relative to the field, being loaded from
//
// Notes:
//    Only assigns normal VNs to "loadTree".
//
void Compiler::fgValueNumberFieldLoad(GenTree* loadTree, GenTree* baseAddr, FieldSeq* fieldSeq, ssize_t offset)
{
    noway_assert(fieldSeq != nullptr);

    // Two cases:
    //
    //  1) Instance field / "complex" static: heap[field][baseAddr][offset + load size].
    //  2) "Simple" static:                   heap[field][offset + load size].
    //
    var_types fieldType;
    unsigned  fieldSize;
    ValueNum  fieldSelectorVN = vnStore->VNForFieldSelector(fieldSeq->GetFieldHandle(), &fieldType, &fieldSize);

    ValueNum fieldMapVN           = ValueNumStore::NoVN;
    ValueNum fieldValueSelectorVN = ValueNumStore::NoVN;
    if (baseAddr != nullptr)
    {
        fieldMapVN           = vnStore->VNForMapSelect(VNK_Liberal, TYP_MEM, fgCurMemoryVN[GcHeap], fieldSelectorVN);
        fieldValueSelectorVN = vnStore->VNLiberalNormalValue(baseAddr->gtVNPair);
    }
    else
    {
        fieldMapVN           = fgCurMemoryVN[GcHeap];
        fieldValueSelectorVN = fieldSelectorVN;
    }

    ValueNum fieldValueVN = vnStore->VNForMapSelect(VNK_Liberal, fieldType, fieldMapVN, fieldValueSelectorVN);

    // Finally, account for the struct fields and type mismatches.
    var_types loadType    = loadTree->TypeGet();
    unsigned  loadSize    = loadTree->OperIsBlk() ? loadTree->AsBlk()->Size() : genTypeSize(loadTree);
    ValueNum  loadValueVN = vnStore->VNForLoad(VNK_Liberal, fieldValueVN, fieldSize, loadType, offset, loadSize);

    loadTree->gtVNPair.SetLiberal(loadValueVN);
    loadTree->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, loadType));
}

//------------------------------------------------------------------------
// fgValueNumberFieldStore: Update the current heap state after a store to
//                          a class/static field.
//
// Arguments:
//    storeNode - The store node
//    baseAddr  - The "base address" of the field (see "GenTree::IsFieldAddr")
//    fieldSeq  - The field sequence representing the address
//    offset    - The offset, relative to the field, of the target location
//    storeSize - The number of bytes being stored
//    value     - The value being stored
//
void Compiler::fgValueNumberFieldStore(
    GenTree* storeNode, GenTree* baseAddr, FieldSeq* fieldSeq, ssize_t offset, unsigned storeSize, ValueNum value)
{
    noway_assert(fieldSeq != nullptr);

    // Two cases:
    //  1) Instance field / "complex" static: heap[field][baseAddr][offset + load size] = value.
    //  2) "Simple" static:                   heap[field][offset + load size]           = value.
    //
    unsigned  fieldSize;
    var_types fieldType;
    ValueNum  fieldSelectorVN = vnStore->VNForFieldSelector(fieldSeq->GetFieldHandle(), &fieldType, &fieldSize);

    ValueNum fieldMapVN           = ValueNumStore::NoVN;
    ValueNum fieldValueSelectorVN = ValueNumStore::NoVN;
    if (baseAddr != nullptr)
    {
        // Construct the "field map" VN. It represents memory state of the first field of all objects
        // on the heap. This is our primary map.
        fieldMapVN           = vnStore->VNForMapSelect(VNK_Liberal, TYP_MEM, fgCurMemoryVN[GcHeap], fieldSelectorVN);
        fieldValueSelectorVN = vnStore->VNLiberalNormalValue(baseAddr->gtVNPair);
    }
    else
    {
        fieldMapVN           = fgCurMemoryVN[GcHeap];
        fieldValueSelectorVN = fieldSelectorVN;
    }

    ValueNum newFieldValueVN = ValueNumStore::NoVN;
    if (vnStore->LoadStoreIsEntire(fieldSize, offset, storeSize))
    {
        // For memory locations (as opposed to locals), we do not normalize types.
        newFieldValueVN = value;
    }
    else
    {
        ValueNum oldFieldValueVN = vnStore->VNForMapSelect(VNK_Liberal, fieldType, fieldMapVN, fieldValueSelectorVN);
        newFieldValueVN          = vnStore->VNForStore(oldFieldValueVN, fieldSize, offset, storeSize, value);
    }

    if (newFieldValueVN != ValueNumStore::NoVN)
    {
        // Construct the new field map...
        ValueNum newFieldMapVN = vnStore->VNForMapStore(fieldMapVN, fieldValueSelectorVN, newFieldValueVN);

        // ...and a new value for the heap.
        ValueNum newHeapVN = ValueNumStore::NoVN;
        if (baseAddr != nullptr)
        {
            newHeapVN = vnStore->VNForMapStore(fgCurMemoryVN[GcHeap], fieldSelectorVN, newFieldMapVN);
        }
        else
        {
            newHeapVN = newFieldMapVN;
        }

        recordGcHeapStore(storeNode, newHeapVN DEBUGARG("StoreField"));
    }
    else
    {
        // For out-of-bounds stores, the heap has to be invalidated as other fields may be affected.
        fgMutateGcHeap(storeNode DEBUGARG("out-of-bounds store to a field"));
    }
}

ValueNum Compiler::fgValueNumberByrefExposedLoad(var_types type, ValueNum pointerVN)
{
    if (type == TYP_STRUCT)
    {
        // We can't assign a value number for a read of a struct as we can't determine
        // how many bytes will be read by this load, so return a new unique value number
        //
        return vnStore->VNForExpr(compCurBB, TYP_STRUCT);
    }
    else
    {
        ValueNum memoryVN = fgCurMemoryVN[ByrefExposed];
        // The memoization for VNFunc applications does not factor in the result type, so
        // VNF_ByrefExposedLoad takes the loaded type as an explicit parameter.
        ValueNum typeVN = vnStore->VNForIntCon(type);
        ValueNum loadVN =
            vnStore->VNForFunc(type, VNF_ByrefExposedLoad, typeVN, vnStore->VNNormalValue(pointerVN), memoryVN);
        return loadVN;
    }
}

var_types ValueNumStore::TypeOfVN(ValueNum vn) const
{
    if (vn == NoVN)
    {
        return TYP_UNDEF;
    }

    Chunk* c = m_chunks.GetNoExpand(GetChunkNum(vn));
    return c->m_typ;
}

//------------------------------------------------------------------------
// LoopOfVN: If the given value number is VNF_MemOpaque, VNF_MapStore, or
//    VNF_MemoryPhiDef, return the loop where the memory update occurs,
//    otherwise returns nullptr
//
// Arguments:
//    vn - Value number to query
//
// Return Value:
//    The memory loop.
//
FlowGraphNaturalLoop* ValueNumStore::LoopOfVN(ValueNum vn)
{
    VNFuncApp funcApp;
    if (GetVNFunc(vn, &funcApp))
    {
        if (funcApp.m_func == VNF_MemOpaque)
        {
            unsigned index = (unsigned)funcApp.m_args[0];
            if ((index == ValueNumStore::NoLoop) || (index == ValueNumStore::UnknownLoop))
            {
                return nullptr;
            }

            return m_pComp->m_loops->GetLoopByIndex(index);
        }
        else if (funcApp.m_func == VNF_MapStore)
        {
            unsigned index = (unsigned)funcApp.m_args[3];
            if (index == ValueNumStore::NoLoop)
            {
                return nullptr;
            }

            return m_pComp->m_loops->GetLoopByIndex(index);
        }
        else if (funcApp.m_func == VNF_PhiMemoryDef)
        {
            BasicBlock* const block = reinterpret_cast<BasicBlock*>(ConstantValue<ssize_t>(funcApp.m_args[0]));
            return m_pComp->m_blockToLoop->GetLoop(block);
        }
    }

    return nullptr;
}

bool ValueNumStore::IsVNConstant(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }
    Chunk* c = m_chunks.GetNoExpand(GetChunkNum(vn));
    if (c->m_attribs == CEA_Const)
    {
        return vn != VNForVoid(); // Void is not a "real" constant -- in the sense that it represents no value.
    }
    else
    {
        return c->m_attribs == CEA_Handle;
    }
}

bool ValueNumStore::IsVNConstantNonHandle(ValueNum vn)
{
    return IsVNConstant(vn) && !IsVNHandle(vn);
}

bool ValueNumStore::IsVNInt32Constant(ValueNum vn)
{
    if (!IsVNConstant(vn))
    {
        return false;
    }

    return TypeOfVN(vn) == TYP_INT;
}

bool ValueNumStore::IsVNNeverNegative(ValueNum vn)
{
    assert(varTypeIsIntegral(TypeOfVN(vn)));

    if (IsVNConstant(vn))
    {
        var_types vnTy = TypeOfVN(vn);
        if (vnTy == TYP_INT)
        {
            return GetConstantInt32(vn) >= 0;
        }
        else if (vnTy == TYP_LONG)
        {
            return GetConstantInt64(vn) >= 0;
        }

        return false;
    }

    // Array length can never be negative.
    if (IsVNArrLen(vn))
    {
        return true;
    }

    VNFuncApp funcApp;
    if (GetVNFunc(vn, &funcApp))
    {
        switch (funcApp.m_func)
        {
            case VNF_GE_UN:
            case VNF_GT_UN:
            case VNF_LE_UN:
            case VNF_LT_UN:
            case VNF_COUNT:
            case VNF_ADD_UN_OVF:
            case VNF_SUB_UN_OVF:
            case VNF_MUL_UN_OVF:
#ifdef FEATURE_HW_INTRINSICS
#ifdef TARGET_XARCH
            case VNF_HWI_POPCNT_PopCount:
            case VNF_HWI_POPCNT_X64_PopCount:
            case VNF_HWI_LZCNT_LeadingZeroCount:
            case VNF_HWI_LZCNT_X64_LeadingZeroCount:
            case VNF_HWI_BMI1_TrailingZeroCount:
            case VNF_HWI_BMI1_X64_TrailingZeroCount:
                return true;
#elif defined(TARGET_ARM64)
            case VNF_HWI_AdvSimd_PopCount:
            case VNF_HWI_AdvSimd_LeadingZeroCount:
            case VNF_HWI_AdvSimd_LeadingSignCount:
            case VNF_HWI_ArmBase_LeadingZeroCount:
            case VNF_HWI_ArmBase_Arm64_LeadingZeroCount:
            case VNF_HWI_ArmBase_Arm64_LeadingSignCount:
                return true;
#endif
#endif // FEATURE_HW_INTRINSICS

            default:
                break;
        }
    }

    return false;
}

GenTreeFlags ValueNumStore::GetHandleFlags(ValueNum vn)
{
    assert(IsVNHandle(vn));
    Chunk*             c           = m_chunks.GetNoExpand(GetChunkNum(vn));
    unsigned           offset      = ChunkOffset(vn);
    VNHandle*          handle      = &reinterpret_cast<VNHandle*>(c->m_defs)[offset];
    const GenTreeFlags handleFlags = handle->m_flags;
    assert((handleFlags & ~GTF_ICON_HDL_MASK) == 0);
    return handleFlags;
}

GenTreeFlags ValueNumStore::GetFoldedArithOpResultHandleFlags(ValueNum vn)
{
    GenTreeFlags flags = GetHandleFlags(vn);
    assert((flags & GTF_ICON_HDL_MASK) == flags);

    switch (flags)
    {
        case GTF_ICON_SCOPE_HDL:
        case GTF_ICON_CLASS_HDL:
        case GTF_ICON_METHOD_HDL:
        case GTF_ICON_FIELD_HDL:
        case GTF_ICON_TOKEN_HDL:
        case GTF_ICON_STR_HDL:
        case GTF_ICON_OBJ_HDL:
        case GTF_ICON_CONST_PTR:
        case GTF_ICON_VARG_HDL:
        case GTF_ICON_PINVKI_HDL:
        case GTF_ICON_FTN_ADDR:
        case GTF_ICON_CIDMID_HDL:
        case GTF_ICON_TLS_HDL:
        case GTF_ICON_STATIC_BOX_PTR:
        case GTF_ICON_STATIC_ADDR_PTR:
            return GTF_ICON_CONST_PTR;
        case GTF_ICON_STATIC_HDL:
        case GTF_ICON_GLOBAL_PTR:
        case GTF_ICON_BBC_PTR:
            return GTF_ICON_GLOBAL_PTR;
        default:
            assert(!"Unexpected handle type");
            return flags;
    }
}

bool ValueNumStore::IsVNHandle(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }

    Chunk* c = m_chunks.GetNoExpand(GetChunkNum(vn));
    return c->m_attribs == CEA_Handle;
}

bool ValueNumStore::IsVNObjHandle(ValueNum vn)
{
    return IsVNHandle(vn) && (GetHandleFlags(vn) == GTF_ICON_OBJ_HDL);
}

//------------------------------------------------------------------------
// SwapRelop: return VNFunc for swapped relop
//
// Arguments:
//    vnf - vnf for original relop
//
// Returns:
//    VNFunc for swapped relop, or VNF_MemOpaque if the original VNFunc
//    was not a relop.
//
VNFunc ValueNumStore::SwapRelop(VNFunc vnf)
{
    VNFunc swappedFunc = VNF_MemOpaque;
    if (vnf >= VNF_Boundary)
    {
        switch (vnf)
        {
            case VNF_LT_UN:
                swappedFunc = VNF_GT_UN;
                break;
            case VNF_LE_UN:
                swappedFunc = VNF_GE_UN;
                break;
            case VNF_GE_UN:
                swappedFunc = VNF_LE_UN;
                break;
            case VNF_GT_UN:
                swappedFunc = VNF_LT_UN;
                break;
            default:
                break;
        }
    }
    else
    {
        const genTreeOps op = (genTreeOps)vnf;

        if (GenTree::OperIsCompare(op))
        {
            swappedFunc = (VNFunc)GenTree::SwapRelop(op);
        }
    }

    return swappedFunc;
}

//------------------------------------------------------------------------
// GetRelatedRelop: return value number for reversed/swapped comparison
//
// Arguments:
//    vn - vn to base things on
//    vrk - whether the new vn should swap, reverse, or both
//
// Returns:
//    vn for related comparison, or NoVN.
//
// Note:
//    If "vn" corresponds to (x > y), the resulting VN corresponds to
//    VRK_Inferred           (x ? y) (NoVN)
//    VRK_Same               (x > y)
//    VRK_Swap               (y < x)
//    VRK_Reverse            (x <= y)
//    VRK_SwapReverse        (y >= x)
//
//    VRK_Same will always return the VN passed in.
//    For other relations, this method will return NoVN for all float comparisons.
//
ValueNum ValueNumStore::GetRelatedRelop(ValueNum vn, VN_RELATION_KIND vrk)
{
    assert(vn == VNNormalValue(vn));

    if (vrk == VN_RELATION_KIND::VRK_Same)
    {
        return vn;
    }

    if (vrk == VN_RELATION_KIND::VRK_Inferred)
    {
        return NoVN;
    }

    if (vn == NoVN)
    {
        return NoVN;
    }

    // Verify we have a binary func application
    //
    VNFuncApp funcAttr;
    if (!GetVNFunc(vn, &funcAttr))
    {
        return NoVN;
    }

    if (funcAttr.m_arity != 2)
    {
        return NoVN;
    }

    // Don't try and model float compares.
    //
    if (varTypeIsFloating(TypeOfVN(funcAttr.m_args[0])))
    {
        return NoVN;
    }

    const bool reverse = (vrk == VN_RELATION_KIND::VRK_Reverse) || (vrk == VN_RELATION_KIND::VRK_SwapReverse);
    const bool swap    = (vrk == VN_RELATION_KIND::VRK_Swap) || (vrk == VN_RELATION_KIND::VRK_SwapReverse);

    // Set up the new function
    //
    VNFunc newFunc = funcAttr.m_func;

    // Swap the predicate, if so asked.
    //
    if (swap)
    {
        newFunc = SwapRelop(newFunc);

        if (newFunc == VNF_MemOpaque)
        {
            return NoVN;
        }
    }

    // Reverse the predicate, if so asked.
    //
    if (reverse)
    {
        if (newFunc >= VNF_Boundary)
        {
            switch (newFunc)
            {
                case VNF_LT_UN:
                    newFunc = VNF_GE_UN;
                    break;
                case VNF_LE_UN:
                    newFunc = VNF_GT_UN;
                    break;
                case VNF_GE_UN:
                    newFunc = VNF_LT_UN;
                    break;
                case VNF_GT_UN:
                    newFunc = VNF_LE_UN;
                    break;
                default:
                    return NoVN;
            }
        }
        else
        {
            const genTreeOps op = (genTreeOps)newFunc;

            if (!GenTree::OperIsCompare(op))
            {
                return NoVN;
            }

            newFunc = (VNFunc)GenTree::ReverseRelop(op);
        }
    }

    // Create the resulting VN, swapping arguments if needed.
    //
    ValueNum result = VNForFunc(TYP_INT, newFunc, funcAttr.m_args[swap ? 1 : 0], funcAttr.m_args[swap ? 0 : 1]);

    return result;
}

#ifdef DEBUG
const char* ValueNumStore::VNRelationString(VN_RELATION_KIND vrk)
{
    switch (vrk)
    {
        case VN_RELATION_KIND::VRK_Inferred:
            return "inferred";
        case VN_RELATION_KIND::VRK_Same:
            return "same";
        case VN_RELATION_KIND::VRK_Reverse:
            return "reversed";
        case VN_RELATION_KIND::VRK_Swap:
            return "swapped";
        case VN_RELATION_KIND::VRK_SwapReverse:
            return "swapped and reversed";
        default:
            return "unknown vn relation";
    }
}
#endif

bool ValueNumStore::IsVNRelop(ValueNum vn)
{
    VNFuncApp funcAttr;
    if (!GetVNFunc(vn, &funcAttr))
    {
        return false;
    }

    if (funcAttr.m_arity != 2)
    {
        return false;
    }

    const VNFunc func = funcAttr.m_func;

    if (func >= VNF_Boundary)
    {
        switch (func)
        {
            case VNF_LT_UN:
            case VNF_LE_UN:
            case VNF_GE_UN:
            case VNF_GT_UN:
                return true;
            default:
                return false;
        }
    }
    else
    {
        const genTreeOps op = (genTreeOps)func;
        return GenTree::OperIsCompare(op);
    }
}

bool ValueNumStore::IsVNConstantBound(ValueNum vn)
{
    VNFuncApp funcApp;
    if ((vn != NoVN) && GetVNFunc(vn, &funcApp))
    {
        if ((funcApp.m_func == (VNFunc)GT_LE) || (funcApp.m_func == (VNFunc)GT_GE) ||
            (funcApp.m_func == (VNFunc)GT_LT) || (funcApp.m_func == (VNFunc)GT_GT))
        {
            const bool op1IsConst = IsVNInt32Constant(funcApp.m_args[0]);
            const bool op2IsConst = IsVNInt32Constant(funcApp.m_args[1]);
            return op1IsConst != op2IsConst;
        }
    }
    return false;
}

bool ValueNumStore::IsVNConstantBoundUnsigned(ValueNum vn)
{
    VNFuncApp funcApp;
    if ((vn != NoVN) && GetVNFunc(vn, &funcApp))
    {
        const bool op1IsPositiveConst = IsVNPositiveInt32Constant(funcApp.m_args[0]);
        const bool op2IsPositiveConst = IsVNPositiveInt32Constant(funcApp.m_args[1]);
        if (!op1IsPositiveConst && op2IsPositiveConst)
        {
            // (uint)index < CNS
            // (uint)index >= CNS
            return (funcApp.m_func == VNF_LT_UN) || (funcApp.m_func == VNF_GE_UN);
        }
        else if (op1IsPositiveConst && !op2IsPositiveConst)
        {
            // CNS > (uint)index
            // CNS <= (uint)index
            return (funcApp.m_func == VNF_GT_UN) || (funcApp.m_func == VNF_LE_UN);
        }
    }
    return false;
}

void ValueNumStore::GetConstantBoundInfo(ValueNum vn, ConstantBoundInfo* info)
{
    assert(IsVNConstantBound(vn) || IsVNConstantBoundUnsigned(vn));
    assert(info);

    VNFuncApp funcAttr;
    GetVNFunc(vn, &funcAttr);

    bool       isUnsigned = true;
    genTreeOps op;
    switch (funcAttr.m_func)
    {
        case VNF_GT_UN:
            op = GT_GT;
            break;
        case VNF_GE_UN:
            op = GT_GE;
            break;
        case VNF_LT_UN:
            op = GT_LT;
            break;
        case VNF_LE_UN:
            op = GT_LE;
            break;
        default:
            op         = (genTreeOps)funcAttr.m_func;
            isUnsigned = false;
            break;
    }

    if (IsVNInt32Constant(funcAttr.m_args[1]))
    {
        info->cmpOper  = op;
        info->cmpOpVN  = funcAttr.m_args[0];
        info->constVal = GetConstantInt32(funcAttr.m_args[1]);
    }
    else
    {
        info->cmpOper  = GenTree::SwapRelop(op);
        info->cmpOpVN  = funcAttr.m_args[1];
        info->constVal = GetConstantInt32(funcAttr.m_args[0]);
    }
    info->isUnsigned = isUnsigned;
}

//------------------------------------------------------------------------
// IsVNPositiveInt32Constant: returns true iff vn is a known Int32 constant that is greater then 0
//
// Arguments:
//    vn - Value number to query
bool ValueNumStore::IsVNPositiveInt32Constant(ValueNum vn)
{
    return IsVNInt32Constant(vn) && (ConstantValue<INT32>(vn) > 0);
}

//------------------------------------------------------------------------
// IsVNArrLenUnsignedBound: Checks if the specified vn represents an expression
//    of one of the following forms:
//    - "(uint)i < (uint)len" that implies (0 <= i < len)
//    - "const < (uint)len" that implies "len > const"
//    - "const <= (uint)len" that implies "len > const - 1"
//
// Arguments:
//    vn - Value number to query
//    info - Pointer to an UnsignedCompareCheckedBoundInfo object to return information about
//           the expression. Not populated if the vn expression isn't suitable (e.g. i <= len).
//           This enables optCreateJTrueBoundAssertion to immediately create an OAK_NO_THROW
//           assertion instead of the OAK_EQUAL/NOT_EQUAL assertions created by signed compares
//           (IsVNCompareCheckedBound, IsVNCompareCheckedBoundArith) that require further processing.
//
// Note:
//   For comparisons of the form constant <= length, this returns them as (constant - 1) < length
//
bool ValueNumStore::IsVNUnsignedCompareCheckedBound(ValueNum vn, UnsignedCompareCheckedBoundInfo* info)
{
    VNFuncApp funcApp;

    if (GetVNFunc(vn, &funcApp))
    {
        if ((funcApp.m_func == VNF_LT_UN) || (funcApp.m_func == VNF_GE_UN))
        {
            // We only care about "(uint)i < (uint)len" and its negation "(uint)i >= (uint)len"
            if (IsVNCheckedBound(funcApp.m_args[1]))
            {
                info->vnIdx   = funcApp.m_args[0];
                info->cmpOper = funcApp.m_func;
                info->vnBound = funcApp.m_args[1];
                return true;
            }
            // We care about (uint)len < constant and its negation "(uint)len >= constant"
            else if (IsVNPositiveInt32Constant(funcApp.m_args[1]) && IsVNCheckedBound(funcApp.m_args[0]))
            {
                // Change constant < len into (uint)len >= (constant - 1)
                // to make consuming this simpler (and likewise for it's negation).
                INT32 validIndex = ConstantValue<INT32>(funcApp.m_args[1]) - 1;
                assert(validIndex >= 0);

                info->vnIdx   = VNForIntCon(validIndex);
                info->cmpOper = (funcApp.m_func == VNF_GE_UN) ? VNF_LT_UN : VNF_GE_UN;
                info->vnBound = funcApp.m_args[0];
                return true;
            }
        }
        else if ((funcApp.m_func == VNF_GT_UN) || (funcApp.m_func == VNF_LE_UN))
        {
            // We only care about "(uint)a.len > (uint)i" and its negation "(uint)a.len <= (uint)i"
            if (IsVNCheckedBound(funcApp.m_args[0]))
            {
                info->vnIdx = funcApp.m_args[1];
                // Let's keep a consistent operand order - it's always i < len, never len > i
                info->cmpOper = (funcApp.m_func == VNF_GT_UN) ? VNF_LT_UN : VNF_GE_UN;
                info->vnBound = funcApp.m_args[0];
                return true;
            }
            // Look for constant > (uint)len and its negation "constant <= (uint)len"
            else if (IsVNPositiveInt32Constant(funcApp.m_args[0]) && IsVNCheckedBound(funcApp.m_args[1]))
            {
                // Change constant <= (uint)len to (constant - 1) < (uint)len
                // to make consuming this simpler (and likewise for it's negation).
                INT32 validIndex = ConstantValue<INT32>(funcApp.m_args[0]) - 1;
                assert(validIndex >= 0);

                info->vnIdx   = VNForIntCon(validIndex);
                info->cmpOper = (funcApp.m_func == VNF_LE_UN) ? VNF_LT_UN : VNF_GE_UN;
                info->vnBound = funcApp.m_args[1];
                return true;
            }
        }
    }

    return false;
}

bool ValueNumStore::IsVNCompareCheckedBound(ValueNum vn)
{
    // Do we have "var < len"?
    if (vn == NoVN)
    {
        return false;
    }

    VNFuncApp funcAttr;
    if (!GetVNFunc(vn, &funcAttr))
    {
        return false;
    }
    if (funcAttr.m_func != (VNFunc)GT_LE && funcAttr.m_func != (VNFunc)GT_GE && funcAttr.m_func != (VNFunc)GT_LT &&
        funcAttr.m_func != (VNFunc)GT_GT)
    {
        return false;
    }
    if (!IsVNCheckedBound(funcAttr.m_args[0]) && !IsVNCheckedBound(funcAttr.m_args[1]))
    {
        return false;
    }

    return true;
}

void ValueNumStore::GetCompareCheckedBound(ValueNum vn, CompareCheckedBoundArithInfo* info)
{
    assert(IsVNCompareCheckedBound(vn));

    // Do we have var < a.len?
    VNFuncApp funcAttr;
    GetVNFunc(vn, &funcAttr);

    bool isOp1CheckedBound = IsVNCheckedBound(funcAttr.m_args[1]);
    if (isOp1CheckedBound)
    {
        info->cmpOper = funcAttr.m_func;
        info->cmpOp   = funcAttr.m_args[0];
        info->vnBound = funcAttr.m_args[1];
    }
    else
    {
        info->cmpOper = GenTree::SwapRelop((genTreeOps)funcAttr.m_func);
        info->cmpOp   = funcAttr.m_args[1];
        info->vnBound = funcAttr.m_args[0];
    }
}

bool ValueNumStore::IsVNCheckedBoundArith(ValueNum vn)
{
    // Do we have "a.len +or- var"
    if (vn == NoVN)
    {
        return false;
    }

    VNFuncApp funcAttr;

    return GetVNFunc(vn, &funcAttr) &&                                                     // vn is a func.
           (funcAttr.m_func == (VNFunc)GT_ADD || funcAttr.m_func == (VNFunc)GT_SUB) &&     // the func is +/-
           (IsVNCheckedBound(funcAttr.m_args[0]) || IsVNCheckedBound(funcAttr.m_args[1])); // either op1 or op2 is a.len
}

void ValueNumStore::GetCheckedBoundArithInfo(ValueNum vn, CompareCheckedBoundArithInfo* info)
{
    // Do we have a.len +/- var?
    assert(IsVNCheckedBoundArith(vn));
    VNFuncApp funcArith;
    GetVNFunc(vn, &funcArith);

    bool isOp1CheckedBound = IsVNCheckedBound(funcArith.m_args[1]);
    if (isOp1CheckedBound)
    {
        info->arrOper = funcArith.m_func;
        info->arrOp   = funcArith.m_args[0];
        info->vnBound = funcArith.m_args[1];
    }
    else
    {
        info->arrOper = funcArith.m_func;
        info->arrOp   = funcArith.m_args[1];
        info->vnBound = funcArith.m_args[0];
    }
}

bool ValueNumStore::IsVNCompareCheckedBoundArith(ValueNum vn)
{
    // Do we have: "var < a.len - var"
    if (vn == NoVN)
    {
        return false;
    }

    VNFuncApp funcAttr;
    if (!GetVNFunc(vn, &funcAttr))
    {
        return false;
    }

    // Suitable comparator.
    if (funcAttr.m_func != (VNFunc)GT_LE && funcAttr.m_func != (VNFunc)GT_GE && funcAttr.m_func != (VNFunc)GT_LT &&
        funcAttr.m_func != (VNFunc)GT_GT)
    {
        return false;
    }

    // Either the op0 or op1 is arr len arithmetic.
    if (!IsVNCheckedBoundArith(funcAttr.m_args[0]) && !IsVNCheckedBoundArith(funcAttr.m_args[1]))
    {
        return false;
    }

    return true;
}

void ValueNumStore::GetCompareCheckedBoundArithInfo(ValueNum vn, CompareCheckedBoundArithInfo* info)
{
    assert(IsVNCompareCheckedBoundArith(vn));

    VNFuncApp funcAttr;
    GetVNFunc(vn, &funcAttr);

    // Check whether op0 or op1 is checked bound arithmetic.
    bool isOp1CheckedBoundArith = IsVNCheckedBoundArith(funcAttr.m_args[1]);
    if (isOp1CheckedBoundArith)
    {
        info->cmpOper = funcAttr.m_func;
        info->cmpOp   = funcAttr.m_args[0];
        GetCheckedBoundArithInfo(funcAttr.m_args[1], info);
    }
    else
    {
        info->cmpOper = GenTree::SwapRelop((genTreeOps)funcAttr.m_func);
        info->cmpOp   = funcAttr.m_args[1];
        GetCheckedBoundArithInfo(funcAttr.m_args[0], info);
    }
}

ValueNum ValueNumStore::GetArrForLenVn(ValueNum vn)
{
    if (vn == NoVN)
    {
        return NoVN;
    }

    VNFuncApp funcAttr;
    if (GetVNFunc(vn, &funcAttr) &&
        ((funcAttr.m_func == (VNFunc)GT_ARR_LENGTH) || (funcAttr.m_func == VNF_MDArrLength)))
    {
        return funcAttr.m_args[0];
    }
    return NoVN;
}

// TODO-MDArray: support JitNewMdArr, probably with a IsVNNewMDArr() function
bool ValueNumStore::IsVNNewArr(ValueNum vn, VNFuncApp* funcApp)
{
    if (vn == NoVN)
    {
        return false;
    }
    bool result = false;
    if (GetVNFunc(vn, funcApp))
    {
        result = (funcApp->m_func == VNF_JitNewArr) || (funcApp->m_func == VNF_JitReadyToRunNewArr);
    }
    return result;
}

// TODO-MDArray: support array dimension length of a specific dimension for JitNewMdArr, with a GetNewMDArrSize()
// function.
bool ValueNumStore::TryGetNewArrSize(ValueNum vn, int* size)
{
    VNFuncApp funcApp;
    if (IsVNNewArr(vn, &funcApp))
    {
        ValueNum arg1VN = funcApp.m_args[1];
        if (IsVNConstant(arg1VN))
        {
            ssize_t val = CoercedConstantValue<ssize_t>(arg1VN);
            if ((size_t)val <= INT_MAX)
            {
                *size = (int)val;
                return true;
            }
        }
    }
    *size = 0;
    return false;
}

bool ValueNumStore::IsVNArrLen(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }
    VNFuncApp funcAttr;
    return GetVNFunc(vn, &funcAttr) &&
           ((funcAttr.m_func == (VNFunc)GT_ARR_LENGTH) || (funcAttr.m_func == VNF_MDArrLength));
}

bool ValueNumStore::IsVNCheckedBound(ValueNum vn)
{
    bool dummy;
    if (m_checkedBoundVNs.TryGetValue(vn, &dummy))
    {
        // This VN appeared as the conservative VN of the length argument of some
        // GT_BOUNDS_CHECK node.
        return true;
    }
    if (IsVNArrLen(vn))
    {
        // Even if we haven't seen this VN in a bounds check, if it is an array length
        // VN then consider it a checked bound VN.  This facilitates better bounds check
        // removal by ensuring that compares against array lengths get put in the
        // optCseCheckedBoundMap; such an array length might get CSEd with one that was
        // directly used in a bounds check, and having the map entry will let us update
        // the compare's VN so that OptimizeRangeChecks can recognize such compares.
        return true;
    }

    return false;
}

void ValueNumStore::SetVNIsCheckedBound(ValueNum vn)
{
    // This is meant to flag VNs for lengths that aren't known at compile time, so we can
    // form and propagate assertions about them.  Ensure that callers filter out constant
    // VNs since they're not what we're looking to flag, and assertion prop can reason
    // directly about constants.
    assert(!IsVNConstant(vn));
    m_checkedBoundVNs.AddOrUpdate(vn, true);
}

#ifdef FEATURE_HW_INTRINSICS
template <typename TSimd>
TSimd BroadcastConstantToSimd(ValueNumStore* vns, var_types baseType, ValueNum argVN)
{
    assert(vns->IsVNConstant(argVN));
    assert(!varTypeIsSIMD(vns->TypeOfVN(argVN)));

    TSimd result = {};

    switch (baseType)
    {
        case TYP_FLOAT:
        {
            float arg = vns->GetConstantSingle(argVN);
            BroadcastConstantToSimd<TSimd, float>(&result, arg);
            break;
        }

        case TYP_DOUBLE:
        {
            double arg = vns->GetConstantDouble(argVN);
            BroadcastConstantToSimd<TSimd, double>(&result, arg);
            break;
        }

        case TYP_BYTE:
        case TYP_UBYTE:
        {
            uint8_t arg = static_cast<uint8_t>(vns->GetConstantInt32(argVN));
            BroadcastConstantToSimd<TSimd, uint8_t>(&result, arg);
            break;
        }

        case TYP_SHORT:
        case TYP_USHORT:
        {
            uint16_t arg = static_cast<uint16_t>(vns->GetConstantInt32(argVN));
            BroadcastConstantToSimd<TSimd, uint16_t>(&result, arg);
            break;
        }

        case TYP_INT:
        case TYP_UINT:
        {
            uint32_t arg = static_cast<uint32_t>(vns->GetConstantInt32(argVN));
            BroadcastConstantToSimd<TSimd, uint32_t>(&result, arg);
            break;
        }

        case TYP_LONG:
        case TYP_ULONG:
        {
            uint64_t arg = static_cast<uint64_t>(vns->GetConstantInt64(argVN));
            BroadcastConstantToSimd<TSimd, uint64_t>(&result, arg);
            break;
        }

        default:
        {
            unreached();
        }
    }

    return result;
}

simd8_t GetConstantSimd8(ValueNumStore* vns, var_types baseType, ValueNum argVN)
{
    assert(vns->IsVNConstant(argVN));

    if (vns->TypeOfVN(argVN) == TYP_SIMD8)
    {
        return vns->GetConstantSimd8(argVN);
    }

    return BroadcastConstantToSimd<simd8_t>(vns, baseType, argVN);
}

simd12_t GetConstantSimd12(ValueNumStore* vns, var_types baseType, ValueNum argVN)
{
    assert(vns->IsVNConstant(argVN));

    if (vns->TypeOfVN(argVN) == TYP_SIMD12)
    {
        return vns->GetConstantSimd12(argVN);
    }

    return BroadcastConstantToSimd<simd12_t>(vns, baseType, argVN);
}

simd16_t GetConstantSimd16(ValueNumStore* vns, var_types baseType, ValueNum argVN)
{
    assert(vns->IsVNConstant(argVN));

    if (vns->TypeOfVN(argVN) == TYP_SIMD16)
    {
        return vns->GetConstantSimd16(argVN);
    }

    return BroadcastConstantToSimd<simd16_t>(vns, baseType, argVN);
}

#if defined(TARGET_XARCH)
simd32_t GetConstantSimd32(ValueNumStore* vns, var_types baseType, ValueNum argVN)
{
    assert(vns->IsVNConstant(argVN));

    if (vns->TypeOfVN(argVN) == TYP_SIMD32)
    {
        return vns->GetConstantSimd32(argVN);
    }

    return BroadcastConstantToSimd<simd32_t>(vns, baseType, argVN);
}

simd64_t GetConstantSimd64(ValueNumStore* vns, var_types baseType, ValueNum argVN)
{
    assert(vns->IsVNConstant(argVN));

    if (vns->TypeOfVN(argVN) == TYP_SIMD64)
    {
        return vns->GetConstantSimd64(argVN);
    }

    return BroadcastConstantToSimd<simd64_t>(vns, baseType, argVN);
}
#endif // TARGET_XARCH

ValueNum EvaluateUnarySimd(
    ValueNumStore* vns, genTreeOps oper, bool scalar, var_types simdType, var_types baseType, ValueNum arg0VN)
{
    switch (simdType)
    {
        case TYP_SIMD8:
        {
            simd8_t arg0 = GetConstantSimd8(vns, baseType, arg0VN);

            simd8_t result = {};
            EvaluateUnarySimd<simd8_t>(oper, scalar, baseType, &result, arg0);
            return vns->VNForSimd8Con(result);
        }

        case TYP_SIMD12:
        {
            simd12_t arg0 = GetConstantSimd12(vns, baseType, arg0VN);

            simd12_t result = {};
            EvaluateUnarySimd<simd12_t>(oper, scalar, baseType, &result, arg0);
            return vns->VNForSimd12Con(result);
        }

        case TYP_SIMD16:
        {
            simd16_t arg0 = GetConstantSimd16(vns, baseType, arg0VN);

            simd16_t result = {};
            EvaluateUnarySimd<simd16_t>(oper, scalar, baseType, &result, arg0);
            return vns->VNForSimd16Con(result);
        }

#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        {
            simd32_t arg0 = GetConstantSimd32(vns, baseType, arg0VN);

            simd32_t result = {};
            EvaluateUnarySimd<simd32_t>(oper, scalar, baseType, &result, arg0);
            return vns->VNForSimd32Con(result);
        }

        case TYP_SIMD64:
        {
            simd64_t arg0 = GetConstantSimd64(vns, baseType, arg0VN);

            simd64_t result = {};
            EvaluateUnarySimd<simd64_t>(oper, scalar, baseType, &result, arg0);
            return vns->VNForSimd64Con(result);
        }
#endif // TARGET_XARCH

        default:
        {
            unreached();
        }
    }
}

ValueNum EvaluateBinarySimd(ValueNumStore* vns,
                            genTreeOps     oper,
                            bool           scalar,
                            var_types      simdType,
                            var_types      baseType,
                            ValueNum       arg0VN,
                            ValueNum       arg1VN)
{
    switch (simdType)
    {
        case TYP_SIMD8:
        {
            simd8_t arg0 = GetConstantSimd8(vns, baseType, arg0VN);
            simd8_t arg1 = GetConstantSimd8(vns, baseType, arg1VN);

            simd8_t result = {};
            EvaluateBinarySimd<simd8_t>(oper, scalar, baseType, &result, arg0, arg1);
            return vns->VNForSimd8Con(result);
        }

        case TYP_SIMD12:
        {
            simd12_t arg0 = GetConstantSimd12(vns, baseType, arg0VN);
            simd12_t arg1 = GetConstantSimd12(vns, baseType, arg1VN);

            simd12_t result = {};
            EvaluateBinarySimd<simd12_t>(oper, scalar, baseType, &result, arg0, arg1);
            return vns->VNForSimd12Con(result);
        }

        case TYP_SIMD16:
        {
            simd16_t arg0 = GetConstantSimd16(vns, baseType, arg0VN);
            simd16_t arg1 = GetConstantSimd16(vns, baseType, arg1VN);

            simd16_t result = {};
            EvaluateBinarySimd<simd16_t>(oper, scalar, baseType, &result, arg0, arg1);
            return vns->VNForSimd16Con(result);
        }

#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        {
            simd32_t arg0 = GetConstantSimd32(vns, baseType, arg0VN);
            simd32_t arg1 = GetConstantSimd32(vns, baseType, arg1VN);

            simd32_t result = {};
            EvaluateBinarySimd<simd32_t>(oper, scalar, baseType, &result, arg0, arg1);
            return vns->VNForSimd32Con(result);
        }

        case TYP_SIMD64:
        {
            simd64_t arg0 = GetConstantSimd64(vns, baseType, arg0VN);
            simd64_t arg1 = GetConstantSimd64(vns, baseType, arg1VN);

            simd64_t result = {};
            EvaluateBinarySimd<simd64_t>(oper, scalar, baseType, &result, arg0, arg1);
            return vns->VNForSimd64Con(result);
        }
#endif // TARGET_XARCH

        default:
        {
            unreached();
        }
    }
}

template <typename TSimd>
ValueNum EvaluateSimdGetElement(ValueNumStore* vns, var_types baseType, TSimd arg0, int arg1)
{
    switch (baseType)
    {
        case TYP_FLOAT:
        {
            float result = arg0.f32[arg1];
            return vns->VNForFloatCon(static_cast<float>(result));
        }

        case TYP_DOUBLE:
        {
            double result = arg0.f64[arg1];
            return vns->VNForDoubleCon(static_cast<double>(result));
        }

        case TYP_BYTE:
        {
            int8_t result = arg0.i8[arg1];
            return vns->VNForIntCon(static_cast<int32_t>(result));
        }

        case TYP_SHORT:
        {
            int16_t result = arg0.i16[arg1];
            return vns->VNForIntCon(static_cast<int32_t>(result));
        }

        case TYP_INT:
        {
            int32_t result = arg0.i32[arg1];
            return vns->VNForIntCon(static_cast<int32_t>(result));
        }

        case TYP_LONG:
        {
            int64_t result = arg0.i64[arg1];
            return vns->VNForLongCon(static_cast<int64_t>(result));
        }

        case TYP_UBYTE:
        {
            uint8_t result = arg0.u8[arg1];
            return vns->VNForIntCon(static_cast<int32_t>(result));
        }

        case TYP_USHORT:
        {
            uint16_t result = arg0.u16[arg1];
            return vns->VNForIntCon(static_cast<int32_t>(result));
        }

        case TYP_UINT:
        {
            uint32_t result = arg0.u32[arg1];
            return vns->VNForIntCon(static_cast<int32_t>(result));
        }

        case TYP_ULONG:
        {
            uint64_t result = arg0.u64[arg1];
            return vns->VNForLongCon(static_cast<int64_t>(result));
        }

        default:
        {
            unreached();
        }
    }
}

ValueNum EvaluateSimdGetElement(ValueNumStore* vns, var_types type, var_types baseType, ValueNum arg0VN, int arg1)
{
    switch (vns->TypeOfVN(arg0VN))
    {
        case TYP_SIMD8:
        {
            return EvaluateSimdGetElement<simd8_t>(vns, baseType, vns->GetConstantSimd8(arg0VN), arg1);
        }

        case TYP_SIMD12:
        {
            return EvaluateSimdGetElement<simd12_t>(vns, baseType, vns->GetConstantSimd12(arg0VN), arg1);
        }

        case TYP_SIMD16:
        {
            return EvaluateSimdGetElement<simd16_t>(vns, baseType, vns->GetConstantSimd16(arg0VN), arg1);
        }

#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        {
            return EvaluateSimdGetElement<simd32_t>(vns, baseType, vns->GetConstantSimd32(arg0VN), arg1);
        }

        case TYP_SIMD64:
        {
            return EvaluateSimdGetElement<simd64_t>(vns, baseType, vns->GetConstantSimd64(arg0VN), arg1);
        }
#endif // TARGET_XARCH

        default:
        {
            unreached();
        }
    }
}

ValueNum ValueNumStore::EvalHWIntrinsicFunUnary(var_types      type,
                                                var_types      baseType,
                                                NamedIntrinsic ni,
                                                VNFunc         func,
                                                ValueNum       arg0VN,
                                                bool           encodeResultType,
                                                ValueNum       resultTypeVN)
{
    if (IsVNConstant(arg0VN))
    {
        switch (ni)
        {
#ifdef TARGET_ARM64
            case NI_ArmBase_LeadingZeroCount:
#else
            case NI_LZCNT_LeadingZeroCount:
#endif
            {
                assert(!varTypeIsSmall(type) && !varTypeIsLong(type));

                int32_t  value  = GetConstantInt32(arg0VN);
                uint32_t result = BitOperations::LeadingZeroCount(static_cast<uint32_t>(value));

                return VNForIntCon(static_cast<int32_t>(result));
            }

#ifdef TARGET_ARM64
            case NI_ArmBase_Arm64_LeadingZeroCount:
            {
                assert(varTypeIsInt(type));

                int64_t  value  = GetConstantInt64(arg0VN);
                uint32_t result = BitOperations::LeadingZeroCount(static_cast<uint64_t>(value));

                return VNForIntCon(static_cast<int32_t>(result));
            }
#else
            case NI_LZCNT_X64_LeadingZeroCount:
            {
                assert(varTypeIsLong(type));

                int64_t  value  = GetConstantInt64(arg0VN);
                uint32_t result = BitOperations::LeadingZeroCount(static_cast<uint64_t>(value));

                return VNForLongCon(static_cast<int64_t>(result));
            }
#endif

#if defined(TARGET_ARM64)
            case NI_ArmBase_ReverseElementBits:
            {
                assert(!varTypeIsSmall(type) && !varTypeIsLong(type));

                int32_t  value  = GetConstantInt32(arg0VN);
                uint32_t result = BitOperations::ReverseBits(static_cast<uint32_t>(value));

                return VNForIntCon(static_cast<uint32_t>(result));
            }

            case NI_ArmBase_Arm64_ReverseElementBits:
            {
                assert(varTypeIsLong(type));

                int64_t  value  = GetConstantInt64(arg0VN);
                uint64_t result = BitOperations::ReverseBits(static_cast<uint64_t>(value));

                return VNForLongCon(static_cast<int64_t>(result));
            }

            case NI_AdvSimd_Negate:
            case NI_AdvSimd_Arm64_Negate:
            {
                return EvaluateUnarySimd(this, GT_NEG, /* scalar */ false, type, baseType, arg0VN);
            }

            case NI_AdvSimd_NegateScalar:
            case NI_AdvSimd_Arm64_NegateScalar:
            {
                return EvaluateUnarySimd(this, GT_NEG, /* scalar */ true, type, baseType, arg0VN);
            }

            case NI_AdvSimd_Not:
            {
                return EvaluateUnarySimd(this, GT_NOT, /* scalar */ false, type, baseType, arg0VN);
            }
#endif // TARGET_ARM64

#if defined(TARGET_XARCH)
            case NI_AVX512CD_LeadingZeroCount:
            case NI_AVX512CD_VL_LeadingZeroCount:
            {
                return EvaluateUnarySimd(this, GT_LZCNT, /* scalar */ false, type, baseType, arg0VN);
            }

            case NI_BMI1_TrailingZeroCount:
            {
                assert(!varTypeIsSmall(type) && !varTypeIsLong(type));

                int32_t  value  = GetConstantInt32(arg0VN);
                uint32_t result = BitOperations::TrailingZeroCount(static_cast<uint32_t>(value));

                return VNForIntCon(static_cast<int32_t>(result));
            }

            case NI_BMI1_X64_TrailingZeroCount:
            {
                assert(varTypeIsLong(type));

                int64_t  value  = GetConstantInt64(arg0VN);
                uint32_t result = BitOperations::TrailingZeroCount(static_cast<uint64_t>(value));

                return VNForLongCon(static_cast<int64_t>(result));
            }

            case NI_POPCNT_PopCount:
            {
                assert(!varTypeIsSmall(type) && !varTypeIsLong(type));

                int32_t  value  = GetConstantInt32(arg0VN);
                uint32_t result = BitOperations::PopCount(static_cast<uint32_t>(value));

                return VNForIntCon(static_cast<int32_t>(result));
            }

            case NI_POPCNT_X64_PopCount:
            {
                assert(varTypeIsLong(type));

                int64_t  value  = GetConstantInt64(arg0VN);
                uint32_t result = BitOperations::PopCount(static_cast<uint64_t>(value));

                return VNForLongCon(static_cast<int64_t>(result));
            }

            case NI_X86Base_BitScanForward:
            {
                assert(!varTypeIsSmall(type) && !varTypeIsLong(type));
                int32_t value = GetConstantInt32(arg0VN);

                if (value == 0)
                {
                    // bsf is undefined for 0
                    break;
                }

                uint32_t result = BitOperations::BitScanForward(static_cast<uint32_t>(value));
                return VNForIntCon(static_cast<int32_t>(result));
            }

            case NI_X86Base_X64_BitScanForward:
            {
                assert(varTypeIsLong(type));
                int64_t value = GetConstantInt64(arg0VN);

                if (value == 0)
                {
                    // bsf is undefined for 0
                    break;
                }

                uint32_t result = BitOperations::BitScanForward(static_cast<uint64_t>(value));
                return VNForLongCon(static_cast<int64_t>(result));
            }

            case NI_X86Base_BitScanReverse:
            {
                assert(!varTypeIsSmall(type) && !varTypeIsLong(type));
                int32_t value = GetConstantInt32(arg0VN);

                if (value == 0)
                {
                    // bsr is undefined for 0
                    break;
                }

                uint32_t result = BitOperations::BitScanReverse(static_cast<uint32_t>(value));
                return VNForIntCon(static_cast<int32_t>(result));
            }

            case NI_X86Base_X64_BitScanReverse:
            {
                assert(varTypeIsLong(type));
                int64_t value = GetConstantInt64(arg0VN);

                if (value == 0)
                {
                    // bsr is undefined for 0
                    break;
                }

                uint32_t result = BitOperations::BitScanReverse(static_cast<uint64_t>(value));
                return VNForLongCon(static_cast<int64_t>(result));
            }
#endif // TARGET_XARCH

            case NI_Vector128_ToScalar:
#ifdef TARGET_ARM64
            case NI_Vector64_ToScalar:
#else
            case NI_Vector256_ToScalar:
            case NI_Vector512_ToScalar:
#endif
            {
                return EvaluateSimdGetElement(this, type, baseType, arg0VN, 0);
            }

            default:
                break;
        }
    }

    if (encodeResultType)
    {
        return VNForFunc(type, func, arg0VN, resultTypeVN);
    }
    return VNForFunc(type, func, arg0VN);
}

ValueNum ValueNumStore::EvalHWIntrinsicFunBinary(var_types      type,
                                                 var_types      baseType,
                                                 NamedIntrinsic ni,
                                                 VNFunc         func,
                                                 ValueNum       arg0VN,
                                                 ValueNum       arg1VN,
                                                 bool           encodeResultType,
                                                 ValueNum       resultTypeVN)
{
    ValueNum cnsVN = NoVN;
    ValueNum argVN = NoVN;

    if (IsVNConstant(arg0VN))
    {
        cnsVN = arg0VN;

        if (!IsVNConstant(arg1VN))
        {
            argVN = arg1VN;
        }
    }
    else
    {
        argVN = arg0VN;

        if (IsVNConstant(arg1VN))
        {
            cnsVN = arg1VN;
        }
    }

    if (argVN == NoVN)
    {
        assert(IsVNConstant(arg0VN) && IsVNConstant(arg1VN));

        switch (ni)
        {
#ifdef TARGET_ARM64
            case NI_AdvSimd_Add:
            case NI_AdvSimd_Arm64_Add:
#else
            case NI_SSE_Add:
            case NI_SSE2_Add:
            case NI_AVX_Add:
            case NI_AVX2_Add:
            case NI_AVX512F_Add:
            case NI_AVX512BW_Add:
#endif
            {
                return EvaluateBinarySimd(this, GT_ADD, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_AddScalar:
#else
            case NI_SSE_AddScalar:
            case NI_SSE2_AddScalar:
#endif
            {
                return EvaluateBinarySimd(this, GT_ADD, /* scalar */ true, type, baseType, arg0VN, arg1VN);
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_And:
#else
            case NI_SSE_And:
            case NI_SSE2_And:
            case NI_AVX_And:
            case NI_AVX2_And:
#endif
            {
                return EvaluateBinarySimd(this, GT_AND, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_BitwiseClear:
            {
                return EvaluateBinarySimd(this, GT_AND_NOT, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }
#else
            case NI_SSE_AndNot:
            case NI_SSE2_AndNot:
            case NI_AVX_AndNot:
            case NI_AVX2_AndNot:
            {
                // xarch does: ~arg0VN & arg1VN
                return EvaluateBinarySimd(this, GT_AND_NOT, /* scalar */ false, type, baseType, arg1VN, arg0VN);
            }
#endif

#ifdef TARGET_ARM64
            case NI_AdvSimd_Arm64_Divide:
#else
            case NI_SSE_Divide:
            case NI_SSE2_Divide:
            case NI_AVX_Divide:
            case NI_AVX512F_Divide:
#endif
            {
                return EvaluateBinarySimd(this, GT_DIV, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_DivideScalar:
#else
            case NI_SSE_DivideScalar:
            case NI_SSE2_DivideScalar:
#endif
            {
                return EvaluateBinarySimd(this, GT_DIV, /* scalar */ true, type, baseType, arg0VN, arg1VN);
            }

            case NI_Vector128_GetElement:
#ifdef TARGET_ARM64
            case NI_Vector64_GetElement:
#else
            case NI_Vector256_GetElement:
            case NI_Vector512_GetElement:
#endif
            {
                return EvaluateSimdGetElement(this, type, baseType, arg0VN, GetConstantInt32(arg1VN));
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_MultiplyByScalar:
            case NI_AdvSimd_Arm64_MultiplyByScalar:
            {
                // MultiplyByScalar takes a vector as the second operand but only utilizes element 0
                // We need to extract it and then functionally broadcast it up for the evaluation to
                // work as expected.

                arg1VN = EvaluateSimdGetElement(this, type, baseType, arg1VN, 0);
                FALLTHROUGH;
            }
#endif

#ifdef TARGET_ARM64
            case NI_AdvSimd_Multiply:
            case NI_AdvSimd_Arm64_Multiply:
#else
            case NI_SSE_Multiply:
            case NI_SSE2_Multiply:
            case NI_SSE2_MultiplyLow:
            case NI_SSE41_MultiplyLow:
            case NI_AVX_Multiply:
            case NI_AVX2_MultiplyLow:
#endif
            {
                return EvaluateBinarySimd(this, GT_MUL, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_MultiplyScalar:
#else
            case NI_SSE_MultiplyScalar:
            case NI_SSE2_MultiplyScalar:
#endif
            {
                return EvaluateBinarySimd(this, GT_MUL, /* scalar */ true, type, baseType, arg0VN, arg1VN);
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_Or:
#else
            case NI_SSE_Or:
            case NI_SSE2_Or:
            case NI_AVX_Or:
            case NI_AVX2_Or:
#endif
            {
                return EvaluateBinarySimd(this, GT_OR, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }

#ifdef TARGET_XARCH
            case NI_AVX512F_RotateLeft:
            case NI_AVX512F_VL_RotateLeft:
            {
                return EvaluateBinarySimd(this, GT_ROL, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }

            case NI_AVX512F_RotateRight:
            case NI_AVX512F_VL_RotateRight:
            {
                return EvaluateBinarySimd(this, GT_ROR, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }
#endif // TARGET_XARCH

#ifdef TARGET_ARM64
            case NI_AdvSimd_ShiftLeftLogical:
#else
            case NI_SSE2_ShiftLeftLogical:
            case NI_AVX2_ShiftLeftLogical:
            case NI_AVX512F_ShiftLeftLogical:
            case NI_AVX512BW_ShiftLeftLogical:
#endif
            {
#ifdef TARGET_XARCH
                if (TypeOfVN(arg1VN) == TYP_SIMD16)
                {
                    // The xarch shift instructions support taking the shift amount as
                    // a simd16, in which case they take the shift amount from the lower
                    // 64-bits.

                    uint64_t shiftAmount = GetConstantSimd16(arg1VN).u64[0];

                    if (genTypeSize(baseType) != 8)
                    {
                        arg1VN = VNForIntCon(static_cast<int32_t>(shiftAmount));
                    }
                    else
                    {
                        arg1VN = VNForLongCon(static_cast<int64_t>(shiftAmount));
                    }
                }
#endif // TARGET_XARCH

                return EvaluateBinarySimd(this, GT_LSH, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_ShiftRightArithmetic:
#else
            case NI_SSE2_ShiftRightArithmetic:
            case NI_AVX2_ShiftRightArithmetic:
            case NI_AVX512F_ShiftRightArithmetic:
            case NI_AVX512F_VL_ShiftRightArithmetic:
            case NI_AVX512BW_ShiftRightArithmetic:
#endif
            {
#ifdef TARGET_XARCH
                if (TypeOfVN(arg1VN) == TYP_SIMD16)
                {
                    // The xarch shift instructions support taking the shift amount as
                    // a simd16, in which case they take the shift amount from the lower
                    // 64-bits.

                    uint64_t shiftAmount = GetConstantSimd16(arg1VN).u64[0];

                    if (genTypeSize(baseType) != 8)
                    {
                        arg1VN = VNForIntCon(static_cast<int32_t>(shiftAmount));
                    }
                    else
                    {
                        arg1VN = VNForLongCon(static_cast<int64_t>(shiftAmount));
                    }
                }
#endif // TARGET_XARCH

                return EvaluateBinarySimd(this, GT_RSH, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_ShiftRightLogical:
#else
            case NI_SSE2_ShiftRightLogical:
            case NI_AVX2_ShiftRightLogical:
            case NI_AVX512F_ShiftRightLogical:
            case NI_AVX512BW_ShiftRightLogical:
#endif
            {
#ifdef TARGET_XARCH
                if (TypeOfVN(arg1VN) == TYP_SIMD16)
                {
                    // The xarch shift instructions support taking the shift amount as
                    // a simd16, in which case they take the shift amount from the lower
                    // 64-bits.

                    uint64_t shiftAmount = GetConstantSimd16(arg1VN).u64[0];

                    if (genTypeSize(baseType) != 8)
                    {
                        arg1VN = VNForIntCon(static_cast<int32_t>(shiftAmount));
                    }
                    else
                    {
                        arg1VN = VNForLongCon(static_cast<int64_t>(shiftAmount));
                    }
                }
#endif // TARGET_XARCH

                return EvaluateBinarySimd(this, GT_RSZ, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_ShiftLeftLogicalScalar:
            {
                return EvaluateBinarySimd(this, GT_LSH, /* scalar */ true, type, baseType, arg0VN, arg1VN);
            }

            case NI_AdvSimd_ShiftRightArithmeticScalar:
            {
                return EvaluateBinarySimd(this, GT_RSH, /* scalar */ true, type, baseType, arg0VN, arg1VN);
            }

            case NI_AdvSimd_ShiftRightLogicalScalar:
            {
                return EvaluateBinarySimd(this, GT_RSZ, /* scalar */ true, type, baseType, arg0VN, arg1VN);
            }
#endif // TARGET_ARM64

#ifdef TARGET_ARM64
            case NI_AdvSimd_Subtract:
            case NI_AdvSimd_Arm64_Subtract:
#else
            case NI_SSE_Subtract:
            case NI_SSE2_Subtract:
            case NI_AVX_Subtract:
            case NI_AVX2_Subtract:
            case NI_AVX512F_Subtract:
            case NI_AVX512BW_Subtract:
#endif
            {
                return EvaluateBinarySimd(this, GT_SUB, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_SubtractScalar:
#else
            case NI_SSE_SubtractScalar:
            case NI_SSE2_SubtractScalar:
#endif
            {
                return EvaluateBinarySimd(this, GT_SUB, /* scalar */ true, type, baseType, arg0VN, arg1VN);
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_Xor:
#else
            case NI_SSE_Xor:
            case NI_SSE2_Xor:
            case NI_AVX_Xor:
            case NI_AVX2_Xor:
#endif
            {
                return EvaluateBinarySimd(this, GT_XOR, /* scalar */ false, type, baseType, arg0VN, arg1VN);
            }

            default:
                break;
        }
    }
    else if (cnsVN != NoVN)
    {
        switch (ni)
        {
#ifdef TARGET_ARM64
            case NI_AdvSimd_Add:
            case NI_AdvSimd_Arm64_Add:
#else
            case NI_SSE_Add:
            case NI_SSE2_Add:
            case NI_AVX_Add:
            case NI_AVX2_Add:
            case NI_AVX512F_Add:
            case NI_AVX512BW_Add:
#endif
            {
                if (varTypeIsFloating(baseType))
                {
                    // Not safe for floating-point when x == -0.0
                    break;
                }

                // Handle `x + 0 == x` and `0 + x == x`
                ValueNum zeroVN = VNZeroForType(type);

                if (cnsVN == zeroVN)
                {
                    return argVN;
                }
                break;
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_And:
#else
            case NI_SSE_And:
            case NI_SSE2_And:
            case NI_AVX_And:
            case NI_AVX2_And:
#endif
            {
                // Handle `x & 0 == 0` and `0 & x == 0`
                ValueNum zeroVN = VNZeroForType(type);

                if (cnsVN == zeroVN)
                {
                    return zeroVN;
                }

                // Handle `x & ~0 == x` and `~0 & x == x`
                ValueNum allBitsVN = VNAllBitsForType(type);

                if (cnsVN == allBitsVN)
                {
                    return argVN;
                }
                break;
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_BitwiseClear:
#else
            case NI_SSE_AndNot:
            case NI_SSE2_AndNot:
            case NI_AVX_AndNot:
            case NI_AVX2_AndNot:
            {
#ifdef TARGET_ARM64
                if (cnsVN == arg0VN)
                {
                    // arm64 preserves the args, so we can only handle `x & ~cns`
                    break;
                }
#else
                if (cnsVN == arg1VN)
                {
                    // xarch swaps the args, so we can only handle `~cns & x`
                    break;
                }
#endif

                // Handle `x & ~0 == x`
                ValueNum zeroVN = VNZeroForType(type);

                if (cnsVN == zeroVN)
                {
                    return argVN;
                }

                // Handle `x & 0 == 0`
                ValueNum allBitsVN = VNAllBitsForType(type);

                if (cnsVN == allBitsVN)
                {
                    return zeroVN;
                }
                break;
            }
#endif

#ifdef TARGET_ARM64
            case NI_AdvSimd_Arm64_Divide:
#else
            case NI_SSE_Divide:
            case NI_SSE2_Divide:
            case NI_AVX_Divide:
            case NI_AVX512F_Divide:
#endif
            {
                // Handle `x / 1 == x`.
                // This is safe for all floats since we do not fault for sNaN
                ValueNum oneVN;

                if (varTypeIsSIMD(TypeOfVN(arg1VN)))
                {
                    oneVN = VNOneForSimdType(type, baseType);
                }
                else
                {
                    oneVN = VNOneForType(baseType);
                }

                if (arg1VN == oneVN)
                {
                    return arg0VN;
                }
                break;
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_MultiplyByScalar:
            case NI_AdvSimd_Arm64_MultiplyByScalar:
            {
                if (!varTypeIsFloating(baseType))
                {
                    // Handle `x * 0 == 0` and `0 * x == 0`
                    // Not safe for floating-point when x == -0.0, NaN, +Inf, -Inf
                    ValueNum zeroVN = VNZeroForType(TypeOfVN(cnsVN));

                    if (cnsVN == zeroVN)
                    {
                        return VNZeroForType(type);
                    }
                }

                assert((TypeOfVN(arg0VN) == type) && (TypeOfVN(arg1VN) == TYP_SIMD8));

                // Handle x * 1 => x, but only if the scalar RHS is <1, ...>.
                if (IsVNConstant(arg1VN))
                {
                    if (EvaluateSimdGetElement(this, TYP_SIMD8, baseType, arg1VN, 0) == VNOneForType(baseType))
                    {
                        return arg0VN;
                    }
                }
                break;
            }
#endif

#ifdef TARGET_ARM64
            case NI_AdvSimd_Multiply:
            case NI_AdvSimd_Arm64_Multiply:
#else
            case NI_SSE_Multiply:
            case NI_SSE2_Multiply:
            case NI_SSE2_MultiplyLow:
            case NI_SSE41_MultiplyLow:
            case NI_AVX_Multiply:
            case NI_AVX2_MultiplyLow:
            case NI_AVX512F_Multiply:
            case NI_AVX512F_MultiplyLow:
            case NI_AVX512BW_MultiplyLow:
            case NI_AVX512DQ_MultiplyLow:
            case NI_AVX512DQ_VL_MultiplyLow:
#endif
            {
                if (!varTypeIsFloating(baseType))
                {
                    // Handle `x * 0 == 0` and `0 * x == 0`
                    // Not safe for floating-point when x == -0.0, NaN, +Inf, -Inf
                    ValueNum zeroVN = VNZeroForType(TypeOfVN(cnsVN));

                    if (cnsVN == zeroVN)
                    {
                        return zeroVN;
                    }
                }

                // Handle `x * 1 == x` and `1 * x == x`
                // This is safe for all floats since we do not fault for sNaN
                ValueNum oneVN;

                if (varTypeIsSIMD(TypeOfVN(cnsVN)))
                {
                    oneVN = VNOneForSimdType(type, baseType);
                }
                else
                {
                    oneVN = VNOneForType(baseType);
                }

                if (cnsVN == oneVN)
                {
                    return argVN;
                }
                break;
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_Or:
#else
            case NI_SSE_Or:
            case NI_SSE2_Or:
            case NI_AVX_Or:
            case NI_AVX2_Or:
#endif
            {
                // Handle `x | 0 == x` and `0 | x == x`
                ValueNum zeroVN = VNZeroForType(type);

                if (cnsVN == zeroVN)
                {
                    return argVN;
                }

                // Handle `x | ~0 == ~0` and `~0 | x== ~0`
                ValueNum allBitsVN = VNAllBitsForType(type);

                if (cnsVN == allBitsVN)
                {
                    return allBitsVN;
                }
                break;
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_ShiftLeftLogical:
            case NI_AdvSimd_ShiftRightArithmetic:
            case NI_AdvSimd_ShiftRightLogical:
#else
            case NI_SSE2_ShiftLeftLogical:
            case NI_SSE2_ShiftRightArithmetic:
            case NI_SSE2_ShiftRightLogical:
            case NI_AVX2_ShiftLeftLogical:
            case NI_AVX2_ShiftRightArithmetic:
            case NI_AVX2_ShiftRightLogical:
            case NI_AVX512F_ShiftLeftLogical:
            case NI_AVX512F_ShiftRightArithmetic:
            case NI_AVX512F_ShiftRightLogical:
            case NI_AVX512F_VL_ShiftRightArithmetic:
            case NI_AVX512BW_ShiftLeftLogical:
            case NI_AVX512BW_ShiftRightArithmetic:
            case NI_AVX512BW_ShiftRightLogical:
#endif
            {
                // Handle `x <<  0 == x` and `0 <<  x == 0`
                // Handle `x >>  0 == x` and `0 >>  x == 0`
                // Handle `x >>> 0 == x` and `0 >>> x == 0`
                ValueNum zeroVN = VNZeroForType(TypeOfVN(cnsVN));

                if (cnsVN == zeroVN)
                {
                    return (cnsVN == arg1VN) ? argVN : zeroVN;
                }
                break;
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_Subtract:
            case NI_AdvSimd_Arm64_Subtract:
#else
            case NI_SSE_Subtract:
            case NI_SSE2_Subtract:
            case NI_AVX_Subtract:
            case NI_AVX2_Subtract:
            case NI_AVX512F_Subtract:
            case NI_AVX512BW_Subtract:
#endif
            {
                if (varTypeIsFloating(baseType))
                {
                    // Not safe for floating-point when x == -0.0
                    break;
                }

                // Handle `x - 0 == x`
                ValueNum zeroVN = VNZeroForType(type);

                if (arg1VN == zeroVN)
                {
                    return argVN;
                }
                break;
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_Xor:
#else
            case NI_SSE_Xor:
            case NI_SSE2_Xor:
            case NI_AVX_Xor:
            case NI_AVX2_Xor:
#endif
            {
                // Handle `x | 0 == x` and `0 | x == x`
                ValueNum zeroVN = VNZeroForType(type);

                if (cnsVN == zeroVN)
                {
                    return argVN;
                }
                break;
            }

            default:
                break;
        }
    }
    else if (arg0VN == arg1VN)
    {
        switch (ni)
        {
#ifdef TARGET_ARM64
            case NI_AdvSimd_And:
#else
            case NI_SSE_And:
            case NI_SSE2_And:
            case NI_AVX_And:
            case NI_AVX2_And:
#endif
            {
                // Handle `x & x == x`
                return arg0VN;
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_BitwiseClear:
#else
            case NI_SSE_AndNot:
            case NI_SSE2_AndNot:
            case NI_AVX_AndNot:
            case NI_AVX2_AndNot:
            {
                // Handle `x & ~x == 0`
                return VNZeroForType(type);
            }
#endif

#ifdef TARGET_ARM64
            case NI_AdvSimd_Or:
#else
            case NI_SSE_Or:
            case NI_SSE2_Or:
            case NI_AVX_Or:
            case NI_AVX2_Or:
#endif
            {
                // Handle `x | x == x`
                return arg0VN;
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_Subtract:
            case NI_AdvSimd_Arm64_Subtract:
#else
            case NI_SSE_Subtract:
            case NI_SSE2_Subtract:
            case NI_AVX_Subtract:
            case NI_AVX2_Subtract:
            case NI_AVX512F_Subtract:
            case NI_AVX512BW_Subtract:
#endif
            {
                if (varTypeIsFloating(baseType))
                {
                    // Not safe for floating-point when x == -0.0, NaN, +Inf, -Inf
                    break;
                }

                // Handle `x - x == 0`
                return VNZeroForType(type);
            }

#ifdef TARGET_ARM64
            case NI_AdvSimd_Xor:
#else
            case NI_SSE_Xor:
            case NI_SSE2_Xor:
            case NI_AVX_Xor:
            case NI_AVX2_Xor:
#endif
            {
                // Handle `x ^ x == 0`
                return VNZeroForType(type);
            }

            default:
                break;
        }
    }

    if (encodeResultType)
    {
        return VNForFunc(type, func, arg0VN, arg1VN, resultTypeVN);
    }
    return VNForFunc(type, func, arg0VN, arg1VN);
}

ValueNum EvaluateSimdFloatWithElement(ValueNumStore* vns, var_types type, ValueNum arg0VN, int index, float value)
{
    assert(vns->IsVNConstant(arg0VN));
    assert(static_cast<unsigned>(index) < genTypeSize(type) / genTypeSize(TYP_FLOAT));

    switch (type)
    {
        case TYP_SIMD8:
        {
            simd8_t cnsVec    = vns->GetConstantSimd8(arg0VN);
            cnsVec.f32[index] = value;
            return vns->VNForSimd8Con(cnsVec);
        }
        case TYP_SIMD12:
        {
            simd12_t cnsVec   = vns->GetConstantSimd12(arg0VN);
            cnsVec.f32[index] = value;
            return vns->VNForSimd12Con(cnsVec);
        }
        case TYP_SIMD16:
        {
            simd16_t cnsVec   = vns->GetConstantSimd16(arg0VN);
            cnsVec.f32[index] = value;
            return vns->VNForSimd16Con(cnsVec);
        }
#if defined TARGET_XARCH
        case TYP_SIMD32:
        {
            simd32_t cnsVec   = vns->GetConstantSimd32(arg0VN);
            cnsVec.f32[index] = value;
            return vns->VNForSimd32Con(cnsVec);
        }
        case TYP_SIMD64:
        {
            simd64_t cnsVec   = vns->GetConstantSimd64(arg0VN);
            cnsVec.f32[index] = value;
            return vns->VNForSimd64Con(cnsVec);
        }
#endif // TARGET_XARCH
        default:
        {
            unreached();
        }
    }
}

ValueNum ValueNumStore::EvalHWIntrinsicFunTernary(var_types      type,
                                                  var_types      baseType,
                                                  NamedIntrinsic ni,
                                                  VNFunc         func,
                                                  ValueNum       arg0VN,
                                                  ValueNum       arg1VN,
                                                  ValueNum       arg2VN,
                                                  bool           encodeResultType,
                                                  ValueNum       resultTypeVN)
{
    if (IsVNConstant(arg0VN) && IsVNConstant(arg1VN) && IsVNConstant(arg2VN))
    {

        switch (ni)
        {
            case NI_Vector128_WithElement:
#ifdef TARGET_ARM64
            case NI_Vector64_WithElement:
#else
            case NI_Vector256_WithElement:
            case NI_Vector512_WithElement:
#endif
            {
                int index = GetConstantInt32(arg1VN);

                assert(varTypeIsSIMD(type));

                // No meaningful diffs for other base-types.
                if ((baseType != TYP_FLOAT) || (TypeOfVN(arg0VN) != type) ||
                    (static_cast<unsigned>(index) >= (genTypeSize(type) / genTypeSize(baseType))))
                {
                    break;
                }

                float value = GetConstantSingle(arg2VN);

                return EvaluateSimdFloatWithElement(this, type, arg0VN, index, value);
            }
            default:
            {
                break;
            }
        }
    }

    if (encodeResultType)
    {
        return VNForFunc(type, func, arg0VN, arg1VN, arg2VN, resultTypeVN);
    }
    else
    {
        return VNForFunc(type, func, arg0VN, arg1VN, arg2VN);
    }
}

#endif // FEATURE_HW_INTRINSICS

ValueNum ValueNumStore::EvalMathFuncUnary(var_types typ, NamedIntrinsic gtMathFN, ValueNum arg0VN)
{
    assert(arg0VN == VNNormalValue(arg0VN));
    assert(m_pComp->IsMathIntrinsic(gtMathFN));

    // If the math intrinsic is not implemented by target-specific instructions, such as implemented
    // by user calls, then don't do constant folding on it during ReadyToRun. This minimizes precision loss.

    if (IsVNConstant(arg0VN) && (!m_pComp->opts.IsReadyToRun() || m_pComp->IsTargetIntrinsic(gtMathFN)))
    {
        assert(varTypeIsFloating(TypeOfVN(arg0VN)));

        if (typ == TYP_DOUBLE)
        {
            // Both operand and its result must be of the same floating point type.
            assert(typ == TypeOfVN(arg0VN));
            double arg0Val = GetConstantDouble(arg0VN);

            double res = 0.0;
            switch (gtMathFN)
            {
                case NI_System_Math_Abs:
                    res = fabs(arg0Val);
                    break;

                case NI_System_Math_Acos:
                    res = acos(arg0Val);
                    break;

                case NI_System_Math_Acosh:
                    res = acosh(arg0Val);
                    break;

                case NI_System_Math_Asin:
                    res = asin(arg0Val);
                    break;

                case NI_System_Math_Asinh:
                    res = asinh(arg0Val);
                    break;

                case NI_System_Math_Atan:
                    res = atan(arg0Val);
                    break;

                case NI_System_Math_Atanh:
                    res = atanh(arg0Val);
                    break;

                case NI_System_Math_Cbrt:
                    res = cbrt(arg0Val);
                    break;

                case NI_System_Math_Ceiling:
                    res = ceil(arg0Val);
                    break;

                case NI_System_Math_Cos:
                    res = cos(arg0Val);
                    break;

                case NI_System_Math_Cosh:
                    res = cosh(arg0Val);
                    break;

                case NI_System_Math_Exp:
                    res = exp(arg0Val);
                    break;

                case NI_System_Math_Floor:
                    res = floor(arg0Val);
                    break;

                case NI_System_Math_Log:
                    res = log(arg0Val);
                    break;

                case NI_System_Math_Log2:
                    res = log2(arg0Val);
                    break;

                case NI_System_Math_Log10:
                    res = log10(arg0Val);
                    break;

                case NI_System_Math_Sin:
                    res = sin(arg0Val);
                    break;

                case NI_System_Math_Sinh:
                    res = sinh(arg0Val);
                    break;

                case NI_System_Math_Round:
                    res = FloatingPointUtils::round(arg0Val);
                    break;

                case NI_System_Math_Sqrt:
                    res = sqrt(arg0Val);
                    break;

                case NI_System_Math_Tan:
                    res = tan(arg0Val);
                    break;

                case NI_System_Math_Tanh:
                    res = tanh(arg0Val);
                    break;

                case NI_System_Math_Truncate:
                    res = trunc(arg0Val);
                    break;

                default:
                    // the above are the only math intrinsics at the time of this writing.
                    unreached();
            }

            return VNForDoubleCon(res);
        }
        else if (typ == TYP_FLOAT)
        {
            // Both operand and its result must be of the same floating point type.
            assert(typ == TypeOfVN(arg0VN));
            float arg0Val = GetConstantSingle(arg0VN);

            float res = 0.0f;
            switch (gtMathFN)
            {
                case NI_System_Math_Abs:
                    res = fabsf(arg0Val);
                    break;

                case NI_System_Math_Acos:
                    res = acosf(arg0Val);
                    break;

                case NI_System_Math_Acosh:
                    res = acoshf(arg0Val);
                    break;

                case NI_System_Math_Asin:
                    res = asinf(arg0Val);
                    break;

                case NI_System_Math_Asinh:
                    res = asinhf(arg0Val);
                    break;

                case NI_System_Math_Atan:
                    res = atanf(arg0Val);
                    break;

                case NI_System_Math_Atanh:
                    res = atanhf(arg0Val);
                    break;

                case NI_System_Math_Cbrt:
                    res = cbrtf(arg0Val);
                    break;

                case NI_System_Math_Ceiling:
                    res = ceilf(arg0Val);
                    break;

                case NI_System_Math_Cos:
                    res = cosf(arg0Val);
                    break;

                case NI_System_Math_Cosh:
                    res = coshf(arg0Val);
                    break;

                case NI_System_Math_Exp:
                    res = expf(arg0Val);
                    break;

                case NI_System_Math_Floor:
                    res = floorf(arg0Val);
                    break;

                case NI_System_Math_Log:
                    res = logf(arg0Val);
                    break;

                case NI_System_Math_Log2:
                    res = log2f(arg0Val);
                    break;

                case NI_System_Math_Log10:
                    res = log10f(arg0Val);
                    break;

                case NI_System_Math_Sin:
                    res = sinf(arg0Val);
                    break;

                case NI_System_Math_Sinh:
                    res = sinhf(arg0Val);
                    break;

                case NI_System_Math_Round:
                    res = FloatingPointUtils::round(arg0Val);
                    break;

                case NI_System_Math_Sqrt:
                    res = sqrtf(arg0Val);
                    break;

                case NI_System_Math_Tan:
                    res = tanf(arg0Val);
                    break;

                case NI_System_Math_Tanh:
                    res = tanhf(arg0Val);
                    break;

                case NI_System_Math_Truncate:
                    res = truncf(arg0Val);
                    break;

                default:
                    // the above are the only math intrinsics at the time of this writing.
                    unreached();
            }

            return VNForFloatCon(res);
        }
        else
        {
            assert(typ == TYP_INT);
            int res = 0;

            if (gtMathFN == NI_System_Math_ILogB)
            {
                switch (TypeOfVN(arg0VN))
                {
                    case TYP_DOUBLE:
                    {
                        double arg0Val = GetConstantDouble(arg0VN);
                        res            = ilogb(arg0Val);
                        break;
                    }

                    case TYP_FLOAT:
                    {
                        float arg0Val = GetConstantSingle(arg0VN);
                        res           = ilogbf(arg0Val);
                        break;
                    }

                    default:
                        unreached();
                }
            }
            else
            {
                assert(gtMathFN == NI_System_Math_Round);

                switch (TypeOfVN(arg0VN))
                {
                    case TYP_DOUBLE:
                    {
                        double arg0Val = GetConstantDouble(arg0VN);
                        res            = int(FloatingPointUtils::round(arg0Val));
                        break;
                    }

                    case TYP_FLOAT:
                    {
                        float arg0Val = GetConstantSingle(arg0VN);
                        res           = int(FloatingPointUtils::round(arg0Val));
                        break;
                    }

                    default:
                        unreached();
                }
            }

            return VNForIntCon(res);
        }
    }
    else
    {
        assert((typ == TYP_DOUBLE) || (typ == TYP_FLOAT) ||
               ((typ == TYP_INT) && ((gtMathFN == NI_System_Math_ILogB) || (gtMathFN == NI_System_Math_Round))));

        VNFunc vnf = VNF_Boundary;
        switch (gtMathFN)
        {
            case NI_System_Math_Abs:
                vnf = VNF_Abs;
                break;
            case NI_System_Math_Acos:
                vnf = VNF_Acos;
                break;
            case NI_System_Math_Acosh:
                vnf = VNF_Acosh;
                break;
            case NI_System_Math_Asin:
                vnf = VNF_Asin;
                break;
            case NI_System_Math_Asinh:
                vnf = VNF_Asinh;
                break;
            case NI_System_Math_Atan:
                vnf = VNF_Atan;
                break;
            case NI_System_Math_Atanh:
                vnf = VNF_Atanh;
                break;
            case NI_System_Math_Cbrt:
                vnf = VNF_Cbrt;
                break;
            case NI_System_Math_Ceiling:
                vnf = VNF_Ceiling;
                break;
            case NI_System_Math_Cos:
                vnf = VNF_Cos;
                break;
            case NI_System_Math_Cosh:
                vnf = VNF_Cosh;
                break;
            case NI_System_Math_Exp:
                vnf = VNF_Exp;
                break;
            case NI_System_Math_Floor:
                vnf = VNF_Floor;
                break;
            case NI_System_Math_ILogB:
                vnf = VNF_ILogB;
                break;
            case NI_System_Math_Log:
                vnf = VNF_Log;
                break;
            case NI_System_Math_Log2:
                vnf = VNF_Log2;
                break;
            case NI_System_Math_Log10:
                vnf = VNF_Log10;
                break;
            case NI_System_Math_Round:
                if (typ == TYP_DOUBLE)
                {
                    vnf = VNF_RoundDouble;
                }
                else if (typ == TYP_INT)
                {
                    vnf = VNF_RoundInt32;
                }
                else if (typ == TYP_FLOAT)
                {
                    vnf = VNF_RoundSingle;
                }
                else
                {
                    noway_assert(!"Invalid INTRINSIC_Round");
                }
                break;
            case NI_System_Math_Sin:
                vnf = VNF_Sin;
                break;
            case NI_System_Math_Sinh:
                vnf = VNF_Sinh;
                break;
            case NI_System_Math_Sqrt:
                vnf = VNF_Sqrt;
                break;
            case NI_System_Math_Tan:
                vnf = VNF_Tan;
                break;
            case NI_System_Math_Tanh:
                vnf = VNF_Tanh;
                break;
            case NI_System_Math_Truncate:
                vnf = VNF_Truncate;
                break;
            default:
                unreached(); // the above are the only math intrinsics at the time of this writing.
        }

        return VNForFunc(typ, vnf, arg0VN);
    }
}

ValueNum ValueNumStore::EvalMathFuncBinary(var_types typ, NamedIntrinsic gtMathFN, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(varTypeIsFloating(typ));
    assert(arg0VN == VNNormalValue(arg0VN));
    assert(arg1VN == VNNormalValue(arg1VN));
    assert(m_pComp->IsMathIntrinsic(gtMathFN));

    // If the math intrinsic is not implemented by target-specific instructions, such as implemented
    // by user calls, then don't do constant folding on it during ReadyToRun. This minimizes precision loss.

    if (IsVNConstant(arg0VN) && IsVNConstant(arg1VN) &&
        (!m_pComp->opts.IsReadyToRun() || m_pComp->IsTargetIntrinsic(gtMathFN)))
    {
        if (typ == TYP_DOUBLE)
        {
            // Both the first operand and its result must be of the same floating point type.
            assert(typ == TypeOfVN(arg0VN));
            double arg0Val = GetConstantDouble(arg0VN);

            double res = 0.0;
            switch (gtMathFN)
            {
                case NI_System_Math_Atan2:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    double arg1Val = GetConstantDouble(arg1VN);
                    res            = atan2(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_FMod:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    double arg1Val = GetConstantDouble(arg1VN);
                    res            = fmod(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_Pow:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    double arg1Val = GetConstantDouble(arg1VN);
                    res            = pow(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_Max:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    double arg1Val = GetConstantDouble(arg1VN);
                    res            = FloatingPointUtils::maximum(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_MaxMagnitude:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    double arg1Val = GetConstantDouble(arg1VN);
                    res            = FloatingPointUtils::maximumMagnitude(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_MaxMagnitudeNumber:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    double arg1Val = GetConstantDouble(arg1VN);
                    res            = FloatingPointUtils::maximumMagnitudeNumber(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_MaxNumber:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    double arg1Val = GetConstantDouble(arg1VN);
                    res            = FloatingPointUtils::maximumNumber(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_Min:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    double arg1Val = GetConstantDouble(arg1VN);
                    res            = FloatingPointUtils::minimum(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_MinMagnitude:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    double arg1Val = GetConstantDouble(arg1VN);
                    res            = FloatingPointUtils::minimumMagnitude(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_MinMagnitudeNumber:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    double arg1Val = GetConstantDouble(arg1VN);
                    res            = FloatingPointUtils::minimumMagnitudeNumber(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_MinNumber:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    double arg1Val = GetConstantDouble(arg1VN);
                    res            = FloatingPointUtils::minimumNumber(arg0Val, arg1Val);
                    break;
                }

                default:
                    // the above are the only binary math intrinsics at the time of this writing.
                    unreached();
            }

            return VNForDoubleCon(res);
        }
        else
        {
            // Both operand and its result must be of the same floating point type.
            assert(typ == TYP_FLOAT);
            assert(typ == TypeOfVN(arg0VN));
            float arg0Val = GetConstantSingle(arg0VN);

            float res = 0.0f;
            switch (gtMathFN)
            {
                case NI_System_Math_Atan2:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    float arg1Val = GetConstantSingle(arg1VN);
                    res           = atan2f(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_FMod:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    float arg1Val = GetConstantSingle(arg1VN);
                    res           = fmodf(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_Max:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    float arg1Val = GetConstantSingle(arg1VN);
                    res           = FloatingPointUtils::maximum(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_MaxMagnitude:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    float arg1Val = GetConstantSingle(arg1VN);
                    res           = FloatingPointUtils::maximumMagnitude(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_MaxMagnitudeNumber:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    float arg1Val = GetConstantSingle(arg1VN);
                    res           = FloatingPointUtils::maximumMagnitudeNumber(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_MaxNumber:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    float arg1Val = GetConstantSingle(arg1VN);
                    res           = FloatingPointUtils::maximumNumber(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_Min:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    float arg1Val = GetConstantSingle(arg1VN);
                    res           = FloatingPointUtils::minimum(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_MinMagnitude:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    float arg1Val = GetConstantSingle(arg1VN);
                    res           = FloatingPointUtils::minimumMagnitude(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_MinMagnitudeNumber:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    float arg1Val = GetConstantSingle(arg1VN);
                    res           = FloatingPointUtils::minimumMagnitudeNumber(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_MinNumber:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    float arg1Val = GetConstantSingle(arg1VN);
                    res           = FloatingPointUtils::minimumNumber(arg0Val, arg1Val);
                    break;
                }

                case NI_System_Math_Pow:
                {
                    assert(typ == TypeOfVN(arg1VN));
                    float arg1Val = GetConstantSingle(arg1VN);
                    res           = powf(arg0Val, arg1Val);
                    break;
                }

                default:
                    // the above are the only binary math intrinsics at the time of this writing.
                    unreached();
            }

            return VNForFloatCon(res);
        }
    }
    else
    {
        VNFunc vnf = VNF_Boundary;

        switch (gtMathFN)
        {
            case NI_System_Math_Atan2:
                vnf = VNF_Atan2;
                break;

            case NI_System_Math_FMod:
                vnf = VNF_FMod;
                break;

            case NI_System_Math_Max:
                vnf = VNF_Max;
                break;

            case NI_System_Math_MaxMagnitude:
                vnf = VNF_MaxMagnitude;
                break;

            case NI_System_Math_MaxMagnitudeNumber:
                vnf = VNF_MaxMagnitudeNumber;
                break;

            case NI_System_Math_MaxNumber:
                vnf = VNF_MaxNumber;
                break;

            case NI_System_Math_Min:
                vnf = VNF_Min;
                break;

            case NI_System_Math_MinMagnitude:
                vnf = VNF_MinMagnitude;
                break;

            case NI_System_Math_MinMagnitudeNumber:
                vnf = VNF_MinMagnitudeNumber;
                break;

            case NI_System_Math_MinNumber:
                vnf = VNF_MinNumber;
                break;

            case NI_System_Math_Pow:
                vnf = VNF_Pow;
                break;

            default:
                // the above are the only binary math intrinsics at the time of this writing.
                unreached();
        }

        return VNForFunc(typ, vnf, arg0VN, arg1VN);
    }
}

bool ValueNumStore::IsVNFunc(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }
    Chunk* c = m_chunks.GetNoExpand(GetChunkNum(vn));
    switch (c->m_attribs)
    {
        case CEA_Func0:
        case CEA_Func1:
        case CEA_Func2:
        case CEA_Func3:
        case CEA_Func4:
            return true;
        default:
            return false;
    }
}

bool ValueNumStore::GetVNFunc(ValueNum vn, VNFuncApp* funcApp)
{
    if (vn == NoVN)
    {
        return false;
    }

    Chunk*   c      = m_chunks.GetNoExpand(GetChunkNum(vn));
    unsigned offset = ChunkOffset(vn);
    assert(offset < c->m_numUsed);
    static_assert_no_msg(AreContiguous(CEA_Func0, CEA_Func1, CEA_Func2, CEA_Func3, CEA_Func4));
    unsigned arity = c->m_attribs - CEA_Func0;
    if (arity <= 4)
    {
        static_assert_no_msg(sizeof(VNFunc) == sizeof(VNDefFuncAppFlexible));
        funcApp->m_arity           = arity;
        VNDefFuncAppFlexible* farg = c->PointerToFuncApp(offset, arity);
        funcApp->m_func            = farg->m_func;
        funcApp->m_args            = farg->m_args;
        return true;
    }

    return false;
}

bool ValueNumStore::VNIsValid(ValueNum vn)
{
    ChunkNum cn = GetChunkNum(vn);
    if (cn >= m_chunks.Size())
    {
        return false;
    }
    // Otherwise...
    Chunk* c = m_chunks.GetNoExpand(cn);
    return ChunkOffset(vn) < c->m_numUsed;
}

#ifdef DEBUG

void ValueNumStore::vnDump(Compiler* comp, ValueNum vn, bool isPtr)
{
    printf(" {");
    if (vn == NoVN)
    {
        printf("NoVN");
    }
    else if (IsVNHandle(vn) && (GetHandleFlags(vn) == GTF_ICON_FIELD_SEQ))
    {
        comp->gtDispFieldSeq(FieldSeqVNToFieldSeq(vn), 0);
        printf(" ");
    }
    else if (IsVNHandle(vn))
    {
        ssize_t            val         = ConstantValue<ssize_t>(vn);
        const GenTreeFlags handleFlags = GetHandleFlags(vn);
        printf("Hnd const: 0x%p %s", dspPtr(val), GenTree::gtGetHandleKindString(handleFlags));
    }
    else if (IsVNConstant(vn))
    {
        var_types vnt = TypeOfVN(vn);
        switch (vnt)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            case TYP_SHORT:
            case TYP_USHORT:
            case TYP_INT:
            case TYP_UINT:
            {
                int val = ConstantValue<int>(vn);
                if (isPtr)
                {
                    printf("PtrCns[%p]", dspPtr(val));
                }
                else
                {
                    printf("IntCns");
                    if ((val > -1000) && (val < 1000))
                    {
                        printf(" %ld", val);
                    }
                    else
                    {
                        printf(" 0x%X", val);
                    }
                }
            }
            break;
            case TYP_LONG:
            case TYP_ULONG:
            {
                INT64 val = ConstantValue<INT64>(vn);
                if (isPtr)
                {
                    printf("LngPtrCns: 0x%p", dspPtr(val));
                }
                else
                {
                    printf("LngCns");
                    if ((val > -1000) && (val < 1000))
                    {
                        printf(" %ld", val);
                    }
                    else if ((val & 0xFFFFFFFF00000000LL) == 0)
                    {
                        printf(" 0x%X", val);
                    }
                    else
                    {
                        printf(" 0x%llx", val);
                    }
                }
            }
            break;
            case TYP_FLOAT:
                printf("FltCns[%f]", ConstantValue<float>(vn));
                break;
            case TYP_DOUBLE:
                printf("DblCns[%f]", ConstantValue<double>(vn));
                break;
            case TYP_REF:
                if (vn == VNForNull())
                {
                    printf("null");
                }
                else if (vn == VNForVoid())
                {
                    printf("void");
                }
                break;
            case TYP_BYREF:
                printf("byrefVal");
                break;
            case TYP_STRUCT:
                printf("structVal(zero)");
                break;

#ifdef FEATURE_SIMD
            case TYP_SIMD8:
            {
                simd8_t cnsVal = GetConstantSimd8(vn);
                printf("Simd8Cns[0x%08x, 0x%08x]", cnsVal.u32[0], cnsVal.u32[1]);
                break;
            }

            case TYP_SIMD12:
            {
                simd12_t cnsVal = GetConstantSimd12(vn);
                printf("Simd12Cns[0x%08x, 0x%08x, 0x%08x]", cnsVal.u32[0], cnsVal.u32[1], cnsVal.u32[2]);
                break;
            }

            case TYP_SIMD16:
            {
                simd16_t cnsVal = GetConstantSimd16(vn);
                printf("Simd16Cns[0x%08x, 0x%08x, 0x%08x, 0x%08x]", cnsVal.u32[0], cnsVal.u32[1], cnsVal.u32[2],
                       cnsVal.u32[3]);
                break;
            }

#if defined(TARGET_XARCH)
            case TYP_SIMD32:
            {
                simd32_t cnsVal = GetConstantSimd32(vn);
                printf("Simd32Cns[0x%016llx, 0x%016llx, 0x%016llx, 0x%016llx]", cnsVal.u64[0], cnsVal.u64[1],
                       cnsVal.u64[2], cnsVal.u64[3]);
                break;
            }

            case TYP_SIMD64:
            {
                simd64_t cnsVal = GetConstantSimd64(vn);
                printf(
                    "Simd64Cns[0x%016llx, 0x%016llx, 0x%016llx, 0x%016llx, 0x%016llx, 0x%016llx, 0x%016llx, 0x%016llx]",
                    cnsVal.u64[0], cnsVal.u64[1], cnsVal.u64[2], cnsVal.u64[3], cnsVal.u64[4], cnsVal.u64[5],
                    cnsVal.u64[6], cnsVal.u64[7]);
                break;
            }
#endif // TARGET_XARCH
#endif // FEATURE_SIMD

            // These should be unreached.
            default:
                unreached();
        }
    }
    else if (IsVNCompareCheckedBound(vn))
    {
        CompareCheckedBoundArithInfo info;
        GetCompareCheckedBound(vn, &info);
        info.dump(this);
    }
    else if (IsVNCompareCheckedBoundArith(vn))
    {
        CompareCheckedBoundArithInfo info;
        GetCompareCheckedBoundArithInfo(vn, &info);
        info.dump(this);
    }
    else if (IsVNFunc(vn))
    {
        VNFuncApp funcApp;
        GetVNFunc(vn, &funcApp);
        // A few special cases...
        switch (funcApp.m_func)
        {
            case VNF_MapSelect:
                vnDumpMapSelect(comp, &funcApp);
                break;
            case VNF_MapStore:
                vnDumpMapStore(comp, &funcApp);
                break;
            case VNF_MapPhysicalStore:
                vnDumpMapPhysicalStore(comp, &funcApp);
                break;
            case VNF_ValWithExc:
                vnDumpValWithExc(comp, &funcApp);
                break;
            case VNF_MemOpaque:
                vnDumpMemOpaque(comp, &funcApp);
                break;
#ifdef FEATURE_SIMD
            case VNF_SimdType:
                vnDumpSimdType(comp, &funcApp);
                break;
#endif // FEATURE_SIMD
            case VNF_Cast:
            case VNF_CastOvf:
                vnDumpCast(comp, vn);
                break;
            case VNF_BitCast:
                vnDumpBitCast(comp, &funcApp);
                break;
            case VNF_ZeroObj:
                vnDumpZeroObj(comp, &funcApp);
                break;

            default:
                printf("%s(", VNFuncName(funcApp.m_func));
                for (unsigned i = 0; i < funcApp.m_arity; i++)
                {
                    if (i > 0)
                    {
                        printf(", ");
                    }

                    printf(FMT_VN, funcApp.m_args[i]);

#if FEATURE_VN_DUMP_FUNC_ARGS
                    printf("=");
                    vnDump(comp, funcApp.m_args[i]);
#endif
                }
                printf(")");
        }
    }
    else
    {
        // Otherwise, just a VN with no structure; print just the VN.
        printf("%x", vn);
    }
    printf("}");
}

// Requires "valWithExc" to be a value with an exception set VNFuncApp.
// Prints a representation of the exception set on standard out.
void ValueNumStore::vnDumpValWithExc(Compiler* comp, VNFuncApp* valWithExc)
{
    assert(valWithExc->m_func == VNF_ValWithExc); // Precondition.

    ValueNum normVN = valWithExc->m_args[0]; // First arg is the VN from normal execution
    ValueNum excVN  = valWithExc->m_args[1]; // Second arg is the set of possible exceptions

    assert(IsVNFunc(excVN));
    VNFuncApp excSeq;
    GetVNFunc(excVN, &excSeq);

    printf("norm=");
    comp->vnPrint(normVN, 1);
    printf(", exc=");
    printf(FMT_VN, excVN);
    vnDumpExcSeq(comp, &excSeq, true);
}

// Requires "excSeq" to be a ExcSetCons sequence.
// Prints a representation of the set of exceptions on standard out.
void ValueNumStore::vnDumpExcSeq(Compiler* comp, VNFuncApp* excSeq, bool isHead)
{
    assert(excSeq->m_func == VNF_ExcSetCons); // Precondition.

    ValueNum curExc  = excSeq->m_args[0];
    bool     hasTail = (excSeq->m_args[1] != VNForEmptyExcSet());

    if (isHead && hasTail)
    {
        printf("(");
    }

    vnDump(comp, curExc);

    if (hasTail)
    {
        printf(", ");
        assert(IsVNFunc(excSeq->m_args[1]));
        VNFuncApp tail;
        GetVNFunc(excSeq->m_args[1], &tail);
        vnDumpExcSeq(comp, &tail, false);
    }

    if (isHead && hasTail)
    {
        printf(")");
    }
}

void ValueNumStore::vnDumpMapSelect(Compiler* comp, VNFuncApp* mapSelect)
{
    assert(mapSelect->m_func == VNF_MapSelect); // Precondition.

    ValueNum mapVN   = mapSelect->m_args[0]; // First arg is the map id
    ValueNum indexVN = mapSelect->m_args[1]; // Second arg is the index

    comp->vnPrint(mapVN, 0);
    printf("[");
    comp->vnPrint(indexVN, 0);
    printf("]");
}

void ValueNumStore::vnDumpMapStore(Compiler* comp, VNFuncApp* mapStore)
{
    assert(mapStore->m_func == VNF_MapStore); // Precondition.

    ValueNum mapVN    = mapStore->m_args[0]; // First arg is the map id
    ValueNum indexVN  = mapStore->m_args[1]; // Second arg is the index
    ValueNum newValVN = mapStore->m_args[2]; // Third arg is the new value
    unsigned loopNum  = mapStore->m_args[3]; // Fourth arg is the loop num

    comp->vnPrint(mapVN, 0);
    printf("[");
    comp->vnPrint(indexVN, 0);
    printf(" := ");
    comp->vnPrint(newValVN, 0);
    printf("]");
    if (loopNum != ValueNumStore::NoLoop)
    {
        printf("@" FMT_LP, loopNum);
    }
}

void ValueNumStore::vnDumpPhysicalSelector(ValueNum selector)
{
    unsigned size;
    unsigned offset = DecodePhysicalSelector(selector, &size);

    if (size == 1)
    {
        printf("[%u]", offset);
    }
    else
    {
        printf("[%u:%u]", offset, offset + size - 1);
    }
}

void ValueNumStore::vnDumpMapPhysicalStore(Compiler* comp, VNFuncApp* mapPhysicalStore)
{
    ValueNum mapVN    = mapPhysicalStore->m_args[0];
    ValueNum selector = mapPhysicalStore->m_args[1];
    ValueNum valueVN  = mapPhysicalStore->m_args[2];

    unsigned size;
    unsigned offset    = DecodePhysicalSelector(selector, &size);
    unsigned endOffset = offset + size;

    comp->vnPrint(mapVN, 0);
    vnDumpPhysicalSelector(selector);
    printf(" := ");
    comp->vnPrint(valueVN, 0);
    printf("]");
}

void ValueNumStore::vnDumpMemOpaque(Compiler* comp, VNFuncApp* memOpaque)
{
    assert(memOpaque->m_func == VNF_MemOpaque); // Precondition.
    const unsigned loopNum = memOpaque->m_args[0];

    if (loopNum == ValueNumStore::NoLoop)
    {
        printf("MemOpaque:NotInLoop");
    }
    else if (loopNum == ValueNumStore::UnknownLoop)
    {
        printf("MemOpaque:Indeterminate");
    }
    else
    {
        printf("MemOpaque:" FMT_LP, loopNum);
    }
}

#ifdef FEATURE_SIMD
void ValueNumStore::vnDumpSimdType(Compiler* comp, VNFuncApp* simdType)
{
    assert(simdType->m_func == VNF_SimdType); // Preconditions.
    assert(IsVNConstant(simdType->m_args[0]));
    assert(IsVNConstant(simdType->m_args[1]));

    int         simdSize    = ConstantValue<int>(simdType->m_args[0]);
    CorInfoType baseJitType = (CorInfoType)ConstantValue<int>(simdType->m_args[1]);

    printf("%s(simd%d, %s)", VNFuncName(simdType->m_func), simdSize, varTypeName(JitType2PreciseVarType(baseJitType)));
}
#endif // FEATURE_SIMD

void ValueNumStore::vnDumpCast(Compiler* comp, ValueNum castVN)
{
    VNFuncApp cast;
    bool      castFound = GetVNFunc(castVN, &cast);
    assert(castFound && ((cast.m_func == VNF_Cast) || (cast.m_func == VNF_CastOvf)));

    var_types castToType;
    bool      srcIsUnsigned;
    GetCastOperFromVN(cast.m_args[1], &castToType, &srcIsUnsigned);

    var_types castFromType = TypeOfVN(cast.m_args[0]);
    var_types resultType   = TypeOfVN(castVN);
    if (srcIsUnsigned)
    {
        castFromType = varTypeToUnsigned(castFromType);
    }

    comp->vnPrint(cast.m_args[0], 0);

    printf(", ");
    if ((resultType != castToType) && (castToType != castFromType))
    {
        printf("%s <- %s <- %s", varTypeName(resultType), varTypeName(castToType), varTypeName(castFromType));
    }
    else
    {
        printf("%s <- %s", varTypeName(resultType), varTypeName(castFromType));
    }
}

void ValueNumStore::vnDumpBitCast(Compiler* comp, VNFuncApp* bitCast)
{
    var_types srcType    = TypeOfVN(bitCast->m_args[0]);
    unsigned  size       = 0;
    var_types castToType = DecodeBitCastType(bitCast->m_args[1], &size);

    printf("BitCast<%s", varTypeName(castToType));
    if (castToType == TYP_STRUCT)
    {
        printf("<%u>", size);
    }
    printf(" <- %s>(", varTypeName(srcType));
    comp->vnPrint(bitCast->m_args[0], 0);
    printf(")");
}

void ValueNumStore::vnDumpZeroObj(Compiler* comp, VNFuncApp* zeroObj)
{
    printf("ZeroObj(");
    comp->vnPrint(zeroObj->m_args[0], 0);
    ClassLayout* layout = reinterpret_cast<ClassLayout*>(ConstantValue<ssize_t>(zeroObj->m_args[0]));
    printf(": %s)", layout->GetClassName());
}
#endif // DEBUG

// Static fields, methods.

#define ValueNumFuncDef(vnf, arity, commute, knownNonNull, sharedStatic, extra)                                        \
    static_assert((arity) >= 0 || !(extra), "valuenumfuncs.h has EncodesExtraTypeArg==true and arity<0 for " #vnf);
#include "valuenumfuncs.h"

#ifdef FEATURE_HW_INTRINSICS

#define HARDWARE_INTRINSIC(isa, name, size, argCount, extra, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag)  \
    static_assert((size) != 0 || !(extra),                                                                             \
                  "hwintrinsicslist<arch>.h has EncodesExtraTypeArg==true and size==0 for " #isa " " #name);
#if defined(TARGET_XARCH)
#include "hwintrinsiclistxarch.h"
#elif defined(TARGET_ARM64)
#include "hwintrinsiclistarm64.h"
#else
#error Unsupported platform
#endif

#endif // FEATURE_HW_INTRINSICS

/* static */ constexpr uint8_t ValueNumStore::GetOpAttribsForArity(genTreeOps oper, GenTreeOperKind kind)
{
    return ((GenTree::StaticOperIs(oper, GT_SELECT) ? 3 : (((kind & GTK_UNOP) >> 1) | ((kind & GTK_BINOP) >> 1)))
            << VNFOA_ArityShift) &
           VNFOA_ArityMask;
}

/* static */ constexpr uint8_t ValueNumStore::GetOpAttribsForGenTree(genTreeOps      oper,
                                                                     bool            commute,
                                                                     bool            illegalAsVNFunc,
                                                                     GenTreeOperKind kind)
{
    return GetOpAttribsForArity(oper, kind) | (static_cast<uint8_t>(commute) << VNFOA_CommutativeShift) |
           (static_cast<uint8_t>(illegalAsVNFunc) << VNFOA_IllegalGenTreeOpShift);
}

/* static */ constexpr uint8_t ValueNumStore::GetOpAttribsForFunc(int  arity,
                                                                  bool commute,
                                                                  bool knownNonNull,
                                                                  bool sharedStatic)
{
    return (static_cast<uint8_t>(commute) << VNFOA_CommutativeShift) |
           (static_cast<uint8_t>(knownNonNull) << VNFOA_KnownNonNullShift) |
           (static_cast<uint8_t>(sharedStatic) << VNFOA_SharedStaticShift) |
           ((static_cast<uint8_t>(arity & ~(arity >> 31)) << VNFOA_ArityShift) & VNFOA_ArityMask);
}

const uint8_t ValueNumStore::s_vnfOpAttribs[VNF_COUNT] = {
#define GTNODE(en, st, cm, ivn, ok)                                                                                    \
    GetOpAttribsForGenTree(static_cast<genTreeOps>(GT_##en), cm, ivn, static_cast<GenTreeOperKind>(ok)),
#include "gtlist.h"

    0, // VNF_Boundary

#define ValueNumFuncDef(vnf, arity, commute, knownNonNull, sharedStatic, extra)                                        \
    GetOpAttribsForFunc((arity) + static_cast<int>(extra), commute, knownNonNull, sharedStatic),
#include "valuenumfuncs.h"
};

static genTreeOps genTreeOpsIllegalAsVNFunc[] = {GT_IND, // When we do heap memory.
                                                 GT_NULLCHECK, GT_QMARK, GT_COLON, GT_LOCKADD, GT_XADD, GT_XCHG,
                                                 GT_CMPXCHG, GT_LCLHEAP, GT_BOX, GT_XORR, GT_XAND, GT_STORE_DYN_BLK,
                                                 GT_STORE_LCL_VAR, GT_STORE_LCL_FLD, GT_STOREIND, GT_STORE_BLK,
                                                 // These need special semantics:
                                                 GT_COMMA, // == second argument (but with exception(s) from first).
                                                 GT_ARR_ADDR, GT_BOUNDS_CHECK,
                                                 GT_BLK,      // May reference heap memory.
                                                 GT_INIT_VAL, // Not strictly a pass-through.
                                                 GT_MDARR_LENGTH,
                                                 GT_MDARR_LOWER_BOUND, // 'dim' value must be considered
                                                 GT_BITCAST,           // Needs to encode the target type.
                                                 GT_NOP,

                                                 // These control-flow operations need no values.
                                                 GT_JTRUE, GT_RETURN, GT_SWITCH, GT_RETFILT, GT_CKFINITE};

void ValueNumStore::ValidateValueNumStoreStatics()
{
#if DEBUG
    uint8_t arr[VNF_COUNT] = {};
    for (unsigned i = 0; i < GT_COUNT; i++)
    {
        genTreeOps gtOper = static_cast<genTreeOps>(i);
        unsigned   arity  = 0;
        if (GenTree::OperIsUnary(gtOper))
        {
            arity = 1;
        }
        else if (GenTree::OperIsBinary(gtOper))
        {
            arity = 2;
        }
        else if (GenTree::StaticOperIs(gtOper, GT_SELECT))
        {
            arity = 3;
        }

        arr[i] |= ((arity << VNFOA_ArityShift) & VNFOA_ArityMask);

        if (GenTree::OperIsCommutative(gtOper))
        {
            arr[i] |= VNFOA_Commutative;
        }
    }

    // I so wish this wasn't the best way to do this...

    int vnfNum = VNF_Boundary + 1; // The macro definition below will update this after using it.

#define ValueNumFuncDef(vnf, arity, commute, knownNonNull, sharedStatic, extra)                                        \
    if (commute)                                                                                                       \
        arr[vnfNum] |= VNFOA_Commutative;                                                                              \
    if (knownNonNull)                                                                                                  \
        arr[vnfNum] |= VNFOA_KnownNonNull;                                                                             \
    if (sharedStatic)                                                                                                  \
        arr[vnfNum] |= VNFOA_SharedStatic;                                                                             \
    if (arity > 0)                                                                                                     \
        arr[vnfNum] |= ((arity << VNFOA_ArityShift) & VNFOA_ArityMask);                                                \
    vnfNum++;

#include "valuenumfuncs.h"

    assert(vnfNum == VNF_COUNT);

#define ValueNumFuncSetArity(vnfNum, arity)                                                                            \
    arr[vnfNum] &= ~VNFOA_ArityMask;                               /* clear old arity value   */                       \
    arr[vnfNum] |= ((arity << VNFOA_ArityShift) & VNFOA_ArityMask) /* set the new arity value */

#ifdef FEATURE_HW_INTRINSICS

    for (NamedIntrinsic id = (NamedIntrinsic)(NI_HW_INTRINSIC_START + 1); (id < NI_HW_INTRINSIC_END);
         id                = (NamedIntrinsic)(id + 1))
    {
        bool encodeResultType = Compiler::vnEncodesResultTypeForHWIntrinsic(id);

        if (encodeResultType)
        {
            // These HW_Intrinsic's have an extra VNF_SimdType arg.
            //
            VNFunc   func     = VNFunc(VNF_HWI_FIRST + (id - NI_HW_INTRINSIC_START - 1));
            unsigned oldArity = (arr[func] & VNFOA_ArityMask) >> VNFOA_ArityShift;
            unsigned newArity = oldArity + 1;

            ValueNumFuncSetArity(func, newArity);
        }

        if (HWIntrinsicInfo::IsCommutative(id))
        {
            VNFunc func = VNFunc(VNF_HWI_FIRST + (id - NI_HW_INTRINSIC_START - 1));
            arr[func] |= VNFOA_Commutative;
        }
    }

#endif // FEATURE_HW_INTRINSICS

#undef ValueNumFuncSetArity

    for (unsigned i = 0; i < ArrLen(genTreeOpsIllegalAsVNFunc); i++)
    {
        arr[genTreeOpsIllegalAsVNFunc[i]] |= VNFOA_IllegalGenTreeOp;
    }

    assert(ArrLen(arr) == ArrLen(s_vnfOpAttribs));
    for (unsigned i = 0; i < ArrLen(arr); i++)
    {
        assert(arr[i] == s_vnfOpAttribs[i]);
    }
#endif // DEBUG
}

#ifdef DEBUG
// Define the name array.
#define ValueNumFuncDef(vnf, arity, commute, knownNonNull, sharedStatic, extra) #vnf,

const char* ValueNumStore::VNFuncNameArr[] = {
#include "valuenumfuncs.h"
};

/* static */ const char* ValueNumStore::VNFuncName(VNFunc vnf)
{
    if (vnf < VNF_Boundary)
    {
        return GenTree::OpName(genTreeOps(vnf));
    }
    else
    {
        return VNFuncNameArr[vnf - (VNF_Boundary + 1)];
    }
}

/* static */ const char* ValueNumStore::VNMapTypeName(var_types type)
{
    switch (type)
    {
        case TYP_HEAP:
            return "heap";
        case TYP_MEM:
            return "mem";
        default:
            return varTypeName(type);
    }
}

static const char* s_reservedNameArr[] = {
    "$VN.Recursive",  // -2  RecursiveVN
    "$VN.No",         // -1  NoVN
    "$VN.Null",       //  0  VNForNull()
    "$VN.Void",       //  1  VNForVoid()
    "$VN.EmptyExcSet" //  2  VNForEmptyExcSet()
};

// Returns the string name of "vn" when it is a reserved value number, nullptr otherwise
// static
const char* ValueNumStore::reservedName(ValueNum vn)
{
    int val = vn - ValueNumStore::RecursiveVN; // Add two, making 'RecursiveVN' equal to zero
    int max = ValueNumStore::SRC_NumSpecialRefConsts - ValueNumStore::RecursiveVN;

    if ((val >= 0) && (val < max))
    {
        return s_reservedNameArr[val];
    }
    return nullptr;
}

#endif // DEBUG

// Returns true if "vn" is a reserved value number

// static
bool ValueNumStore::isReservedVN(ValueNum vn)
{
    int val = vn - ValueNumStore::RecursiveVN; // Adding two, making 'RecursiveVN' equal to zero
    int max = ValueNumStore::SRC_NumSpecialRefConsts - ValueNumStore::RecursiveVN;

    if ((val >= 0) && (val < max))
    {
        return true;
    }
    return false;
}

#ifdef DEBUG
void ValueNumStore::RunTests(Compiler* comp)
{
    VNFunc VNF_Add = GenTreeOpToVNFunc(GT_ADD);

    ValueNumStore* vns    = new (comp->getAllocatorDebugOnly()) ValueNumStore(comp, comp->getAllocatorDebugOnly());
    ValueNum       vnNull = VNForNull();
    assert(vnNull == VNForNull());

    ValueNum vnFor1 = vns->VNForIntCon(1);
    assert(vnFor1 == vns->VNForIntCon(1));
    assert(vns->TypeOfVN(vnFor1) == TYP_INT);
    assert(vns->IsVNConstant(vnFor1));
    assert(vns->ConstantValue<int>(vnFor1) == 1);

    ValueNum vnFor100 = vns->VNForIntCon(100);
    assert(vnFor100 == vns->VNForIntCon(100));
    assert(vnFor100 != vnFor1);
    assert(vns->TypeOfVN(vnFor100) == TYP_INT);
    assert(vns->IsVNConstant(vnFor100));
    assert(vns->ConstantValue<int>(vnFor100) == 100);

    ValueNum vnFor1F = vns->VNForFloatCon(1.0f);
    assert(vnFor1F == vns->VNForFloatCon(1.0f));
    assert(vnFor1F != vnFor1 && vnFor1F != vnFor100);
    assert(vns->TypeOfVN(vnFor1F) == TYP_FLOAT);
    assert(vns->IsVNConstant(vnFor1F));
    assert(vns->ConstantValue<float>(vnFor1F) == 1.0f);

    ValueNum vnFor1D = vns->VNForDoubleCon(1.0);
    assert(vnFor1D == vns->VNForDoubleCon(1.0));
    assert(vnFor1D != vnFor1F && vnFor1D != vnFor1 && vnFor1D != vnFor100);
    assert(vns->TypeOfVN(vnFor1D) == TYP_DOUBLE);
    assert(vns->IsVNConstant(vnFor1D));
    assert(vns->ConstantValue<double>(vnFor1D) == 1.0);

    ValueNum vnRandom1   = vns->VNForExpr(nullptr, TYP_INT);
    ValueNum vnForFunc2a = vns->VNForFunc(TYP_INT, VNF_Add, vnFor1, vnRandom1);
    assert(vnForFunc2a == vns->VNForFunc(TYP_INT, VNF_Add, vnFor1, vnRandom1));
    assert(vnForFunc2a != vnFor1D && vnForFunc2a != vnFor1F && vnForFunc2a != vnFor1 && vnForFunc2a != vnRandom1);
    assert(vns->TypeOfVN(vnForFunc2a) == TYP_INT);
    assert(!vns->IsVNConstant(vnForFunc2a));
    assert(vns->IsVNFunc(vnForFunc2a));
    VNFuncApp fa2a;
    bool      b = vns->GetVNFunc(vnForFunc2a, &fa2a);
    assert(b);
    assert(fa2a.m_func == VNF_Add && fa2a.m_arity == 2 && fa2a.m_args[0] == vnFor1 && fa2a.m_args[1] == vnRandom1);

    ValueNum vnForFunc2b = vns->VNForFunc(TYP_INT, VNF_Add, vnFor1, vnFor100);
    assert(vnForFunc2b == vns->VNForFunc(TYP_INT, VNF_Add, vnFor1, vnFor100));
    assert(vnForFunc2b != vnFor1D && vnForFunc2b != vnFor1F && vnForFunc2b != vnFor1 && vnForFunc2b != vnFor100);
    assert(vns->TypeOfVN(vnForFunc2b) == TYP_INT);
    assert(vns->IsVNConstant(vnForFunc2b));
    assert(vns->ConstantValue<int>(vnForFunc2b) == 101);

    // printf("Did ValueNumStore::RunTests.\n");
}
#endif // DEBUG

class ValueNumberState
{
    Compiler*    m_comp;
    BitVecTraits m_blockTraits;
    BitVec       m_provenUnreachableBlocks;

public:
    ValueNumberState(Compiler* comp)
        : m_comp(comp)
        , m_blockTraits(comp->fgBBNumMax + 1, comp)
        , m_provenUnreachableBlocks(BitVecOps::MakeEmpty(&m_blockTraits))
    {
    }

    //------------------------------------------------------------------------
    // SetUnreachable: Mark that a block has been proven unreachable.
    //
    // Parameters:
    //   bb - The block.
    //
    void SetUnreachable(BasicBlock* bb)
    {
        BitVecOps::AddElemD(&m_blockTraits, m_provenUnreachableBlocks, bb->bbNum);
    }

    //------------------------------------------------------------------------
    // IsReachable: Check if a block is reachable. Takes static reachability
    // and proven unreachability into account.
    //
    // Parameters:
    //   bb - The block.
    //
    // Returns:
    //   True if the basic block is potentially reachable. False if the basic
    //   block is definitely not reachable.
    //
    bool IsReachable(BasicBlock* bb)
    {
        return m_comp->m_dfsTree->Contains(bb) &&
               !BitVecOps::IsMember(&m_blockTraits, m_provenUnreachableBlocks, bb->bbNum);
    }

    //------------------------------------------------------------------------
    // IsReachableThroughPred: Check if a block can be reached through one of
    // its direct predecessors.
    //
    // Parameters:
    //   block     - A block
    //   predBlock - A predecessor of 'block'
    //
    // Returns:
    //   False if 'block' is definitely not reachable through 'predBlock', even
    //   though it is a direct successor of 'predBlock'.
    //
    bool IsReachableThroughPred(BasicBlock* block, BasicBlock* predBlock)
    {
        if (!IsReachable(predBlock))
        {
            return false;
        }

        if (!predBlock->KindIs(BBJ_COND) || predBlock->TrueTargetIs(predBlock->GetFalseTarget()))
        {
            return true;
        }

        GenTree* lastTree = predBlock->lastStmt()->GetRootNode();
        assert(lastTree->OperIs(GT_JTRUE));

        GenTree* cond = lastTree->gtGetOp1();
        // TODO-Cleanup: Using liberal VNs here is a bit questionable as it
        // adds a cross-phase dependency on RBO to definitely fold this branch
        // away.
        ValueNum normalVN = m_comp->vnStore->VNNormalValue(cond->GetVN(VNK_Liberal));
        if (!m_comp->vnStore->IsVNConstant(normalVN))
        {
            return true;
        }

        bool        isTaken         = normalVN != m_comp->vnStore->VNZeroForType(TYP_INT);
        BasicBlock* unreachableSucc = isTaken ? predBlock->GetFalseTarget() : predBlock->GetTrueTarget();
        return block != unreachableSucc;
    }
};

//------------------------------------------------------------------------
// fgValueNumber: Run value numbering for the entire method
//
// Returns:
//   suitable phase status
//
PhaseStatus Compiler::fgValueNumber()
{
#ifdef DEBUG
    // This could be a JITDUMP, but some people find it convenient to set a breakpoint on the printf.
    if (verbose)
    {
        printf("\n*************** In fgValueNumber()\n");
    }
#endif

    // If we skipped SSA, skip VN as well.
    if (fgSsaPassesCompleted == 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // Allocate the value number store.
    assert(fgVNPassesCompleted > 0 || vnStore == nullptr);
    if (fgVNPassesCompleted == 0)
    {
        CompAllocator allocator(getAllocator(CMK_ValueNumber));
        vnStore = new (allocator) ValueNumStore(this, allocator);
    }
    else
    {
        ValueNumPair noVnp;
        // Make sure the memory SSA names have no value numbers.
        for (unsigned i = 0; i < lvMemoryPerSsaData.GetCount(); i++)
        {
            lvMemoryPerSsaData.GetSsaDefByIndex(i)->m_vnPair = noVnp;
        }
        for (BasicBlock* const blk : Blocks())
        {
            for (Statement* const stmt : blk->NonPhiStatements())
            {
                for (GenTree* const tree : stmt->TreeList())
                {
                    tree->gtVNPair.SetBoth(ValueNumStore::NoVN);
                }
            }
        }
    }

    m_blockToLoop = BlockToNaturalLoopMap::Build(m_loops);
    // Compute the side effects of loops.
    optComputeLoopSideEffects();

    // At the block level, we will use a modified worklist algorithm.  We will have two
    // "todo" sets of unvisited blocks.  Blocks (other than the entry block) are put in a
    // todo set only when some predecessor has been visited, so all blocks have at least one
    // predecessor visited.  The distinction between the two sets is whether *all* predecessors have
    // already been visited.  We visit such blocks preferentially if they exist, since phi definitions
    // in such blocks will have all arguments defined, enabling a simplification in the case that all
    // arguments to the phi have the same VN.  If no such blocks exist, we pick a block with at least
    // one unvisited predecessor.  In this case, we assign a new VN for phi definitions.

    // Start by giving incoming arguments value numbers.
    // Also give must-init vars a zero of their type.
    for (unsigned lclNum = 0; lclNum < lvaCount; lclNum++)
    {
        if (!lvaInSsa(lclNum))
        {
            continue;
        }

        LclVarDsc* varDsc = lvaGetDesc(lclNum);
        assert(varDsc->lvTracked);

        if (varDsc->lvIsParam)
        {
            // We assume that code equivalent to this variable initialization loop
            // has been performed when doing SSA naming, so that all the variables we give
            // initial VNs to here have been given initial SSA definitions there.
            // SSA numbers always start from FIRST_SSA_NUM, and we give the value number to SSA name FIRST_SSA_NUM.
            // We use the VNF_InitVal(i) from here so we know that this value is loop-invariant
            // in all loops.
            ValueNum      initVal = vnStore->VNForFunc(varDsc->TypeGet(), VNF_InitVal, vnStore->VNForIntCon(lclNum));
            LclSsaVarDsc* ssaDef  = varDsc->GetPerSsaData(SsaConfig::FIRST_SSA_NUM);
            ssaDef->m_vnPair.SetBoth(initVal);
            ssaDef->SetBlock(fgFirstBB);
        }
        else if (info.compInitMem || varDsc->lvMustInit ||
                 VarSetOps::IsMember(this, fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
        {
            // The last clause covers the use-before-def variables (the ones that are live-in to the first block),
            // these are variables that are read before being initialized (at least on some control flow paths)
            // if they are not must-init, then they get VNF_InitVal(i), as with the param case.)
            bool      isZeroed = !fgVarNeedsExplicitZeroInit(lclNum, /* bbInALoop */ false, /* bbIsReturn */ false);
            ValueNum  initVal  = ValueNumStore::NoVN; // We must assign a new value to initVal
            var_types typ      = varDsc->TypeGet();

            if (isZeroed)
            {
                // By default we will zero init these LclVars
                initVal =
                    (typ == TYP_STRUCT) ? vnStore->VNForZeroObj(varDsc->GetLayout()) : vnStore->VNZeroForType(typ);
            }
            else
            {
                initVal = vnStore->VNForFunc(typ, VNF_InitVal, vnStore->VNForIntCon(lclNum));
            }
#ifdef TARGET_X86
            bool isVarargParam = (lclNum == lvaVarargsBaseOfStkArgs || lclNum == lvaVarargsHandleArg);
            if (isVarargParam)
                initVal = vnStore->VNForExpr(fgFirstBB); // a new, unique VN.
#endif
            assert(initVal != ValueNumStore::NoVN);

            LclSsaVarDsc* ssaDef = varDsc->GetPerSsaData(SsaConfig::FIRST_SSA_NUM);
            ssaDef->m_vnPair.SetBoth(initVal);
            ssaDef->SetBlock(fgFirstBB);
        }
    }
    // Give memory an initial value number (about which we know nothing).
    ValueNum memoryInitVal = vnStore->VNForFunc(TYP_HEAP, VNF_InitVal, vnStore->VNForIntCon(-1)); // Use -1 for memory.
    GetMemoryPerSsaData(SsaConfig::FIRST_SSA_NUM)->m_vnPair.SetBoth(memoryInitVal);
#ifdef DEBUG
    if (verbose)
    {
        printf("Memory Initial Value in BB01 is: " FMT_VN "\n", memoryInitVal);
    }
#endif // DEBUG

    ValueNumberState vs(this);
    vnState = &vs;

    // SSA has already computed a post-order taking EH successors into account.
    // Visiting that in reverse will ensure we visit a block's predecessors
    // before itself whenever possible.
    BasicBlock** postOrder      = m_dfsTree->GetPostOrder();
    unsigned     postOrderCount = m_dfsTree->GetPostOrderCount();
    for (unsigned i = postOrderCount; i != 0; i--)
    {
        BasicBlock* block = postOrder[i - 1];
        JITDUMP("Visiting " FMT_BB "\n", block->bbNum);

        if (block != fgFirstBB)
        {
            bool anyPredReachable = false;
            for (FlowEdge* pred = BlockPredsWithEH(block); pred != nullptr; pred = pred->getNextPredEdge())
            {
                BasicBlock* predBlock = pred->getSourceBlock();
                if (!vs.IsReachableThroughPred(block, predBlock))
                {
                    JITDUMP("  Unreachable through pred " FMT_BB "\n", predBlock->bbNum);
                    continue;
                }

                JITDUMP("  Reachable through pred " FMT_BB "\n", predBlock->bbNum);
                anyPredReachable = true;
                break;
            }

            if (!anyPredReachable)
            {
                JITDUMP("  " FMT_BB " was proven unreachable\n", block->bbNum);
                vs.SetUnreachable(block);
            }
        }

        fgValueNumberBlock(block);
    }

#ifdef DEBUG
    JitTestCheckVN();
    fgDebugCheckExceptionSets();
#endif // DEBUG

    fgVNPassesCompleted++;

    return PhaseStatus::MODIFIED_EVERYTHING;
}

void Compiler::fgValueNumberBlock(BasicBlock* blk)
{
    compCurBB = blk;

    Statement* stmt = blk->firstStmt();

    // First: visit phis and check to see if all phi args have the same value.
    for (; (stmt != nullptr) && stmt->IsPhiDefnStmt(); stmt = stmt->GetNextStmt())
    {
        GenTreeLclVar* newSsaDef = stmt->GetRootNode()->AsLclVar();
        GenTreePhi*    phiNode   = newSsaDef->AsLclVar()->Data()->AsPhi();
        ValueNumPair   phiVNP;
        ValueNumPair   sameVNP;

        for (GenTreePhi::Use& use : phiNode->Uses())
        {
            GenTreePhiArg* phiArg = use.GetNode()->AsPhiArg();
            if (!vnState->IsReachableThroughPred(blk, phiArg->gtPredBB))
            {
                JITDUMP("  Phi arg [%06u] is unnecessary; path through pred " FMT_BB " cannot be taken\n",
                        dspTreeID(phiArg), phiArg->gtPredBB->bbNum);

                if ((use.GetNext() != nullptr) || (phiVNP.GetLiberal() != ValueNumStore::NoVN))
                {
                    continue;
                }

                assert(!vnState->IsReachable(blk));
                JITDUMP("  ..but no other path can, so we are using it anyway\n");
            }

            ValueNum     phiArgSsaNumVN = vnStore->VNForIntCon(phiArg->GetSsaNum());
            ValueNumPair phiArgVNP      = lvaGetDesc(phiArg)->GetPerSsaData(phiArg->GetSsaNum())->m_vnPair;

            phiArg->gtVNPair = phiArgVNP;

            if (phiVNP.GetLiberal() == ValueNumStore::NoVN)
            {
                // This is the first PHI argument
                phiVNP  = ValueNumPair(phiArgSsaNumVN, phiArgSsaNumVN);
                sameVNP = phiArgVNP;
            }
            else
            {
                phiVNP = vnStore->VNPairForFuncNoFolding(newSsaDef->TypeGet(), VNF_Phi,
                                                         ValueNumPair(phiArgSsaNumVN, phiArgSsaNumVN), phiVNP);

                if ((sameVNP.GetLiberal() != phiArgVNP.GetLiberal()) ||
                    (sameVNP.GetConservative() != phiArgVNP.GetConservative()))
                {
                    // If this argument's VNs are different from "same" then change "same" to NoVN.
                    // Note that this means that if any argument's VN is NoVN then the final result
                    // will also be NoVN, which is what we want.
                    sameVNP.SetBoth(ValueNumStore::NoVN);
                }
            }
        }

        // We should have visited at least one phi arg in the loop above
        assert(phiVNP.GetLiberal() != ValueNumStore::NoVN);
        assert(phiVNP.GetConservative() != ValueNumStore::NoVN);

        ValueNumPair newSsaDefVNP;

        if (sameVNP.BothDefined())
        {
            // If all the args of the phi had the same value(s, liberal and conservative), then there wasn't really
            // a reason to have the phi -- just pass on that value.
            newSsaDefVNP = sameVNP;
        }
        else
        {
            // They were not the same; we need to create a phi definition.
            ValueNum lclNumVN = ValueNum(newSsaDef->GetLclNum());
            ValueNum ssaNumVN = ValueNum(newSsaDef->GetSsaNum());

            newSsaDefVNP = vnStore->VNPairForFunc(newSsaDef->TypeGet(), VNF_PhiDef, ValueNumPair(lclNumVN, lclNumVN),
                                                  ValueNumPair(ssaNumVN, ssaNumVN), phiVNP);
        }

        LclSsaVarDsc* newSsaDefDsc = lvaGetDesc(newSsaDef)->GetPerSsaData(newSsaDef->GetSsaNum());
        newSsaDefDsc->m_vnPair     = newSsaDefVNP;
#ifdef DEBUG
        if (verbose)
        {
            printf("SSA PHI definition: set VN of local %d/%d to ", newSsaDef->GetLclNum(), newSsaDef->GetSsaNum());
            vnpPrint(newSsaDefVNP, 1);
            printf(" %s.\n", sameVNP.BothDefined() ? "(all same)" : "");
        }
#endif // DEBUG

        newSsaDef->gtVNPair = vnStore->VNPForVoid();
        phiNode->gtVNPair   = newSsaDefVNP;
    }

    // Now do the same for each MemoryKind.
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        // Is there a phi for this block?
        if (blk->bbMemorySsaPhiFunc[memoryKind] == nullptr)
        {
            ValueNum newMemoryVN = GetMemoryPerSsaData(blk->bbMemorySsaNumIn[memoryKind])->m_vnPair.GetLiberal();
            fgSetCurrentMemoryVN(memoryKind, newMemoryVN);
        }
        else
        {
            if ((memoryKind == ByrefExposed) && byrefStatesMatchGcHeapStates)
            {
                // The update for GcHeap will copy its result to ByrefExposed.
                assert(memoryKind < GcHeap);
                assert(blk->bbMemorySsaPhiFunc[memoryKind] == blk->bbMemorySsaPhiFunc[GcHeap]);
                continue;
            }

            ValueNum              newMemoryVN;
            FlowGraphNaturalLoop* loop = m_blockToLoop->GetLoop(blk);
            if ((loop != nullptr) && (loop->GetHeader() == blk))
            {
                newMemoryVN = fgMemoryVNForLoopSideEffects(memoryKind, blk, loop);
            }
            else
            {
                // Are all the VN's the same?
                BasicBlock::MemoryPhiArg* phiArgs = blk->bbMemorySsaPhiFunc[memoryKind];
                assert(phiArgs != BasicBlock::EmptyMemoryPhiDef);
                // There should be > 1 args to a phi.
                // But OSR might leave around "dead" try entry blocks...
                assert((phiArgs->m_nextArg != nullptr) || opts.IsOSR());
                ValueNum phiAppVN = vnStore->VNForIntCon(phiArgs->GetSsaNum());
                JITDUMP("  Building phi application: $%x = SSA# %d.\n", phiAppVN, phiArgs->GetSsaNum());
                bool     allSame = true;
                ValueNum sameVN  = GetMemoryPerSsaData(phiArgs->GetSsaNum())->m_vnPair.GetLiberal();
                if (sameVN == ValueNumStore::NoVN)
                {
                    allSame = false;
                }
                phiArgs = phiArgs->m_nextArg;
                while (phiArgs != nullptr)
                {
                    ValueNum phiArgVN = GetMemoryPerSsaData(phiArgs->GetSsaNum())->m_vnPair.GetLiberal();
                    if (phiArgVN == ValueNumStore::NoVN || phiArgVN != sameVN)
                    {
                        allSame = false;
                    }
#ifdef DEBUG
                    ValueNum oldPhiAppVN = phiAppVN;
#endif
                    unsigned phiArgSSANum   = phiArgs->GetSsaNum();
                    ValueNum phiArgSSANumVN = vnStore->VNForIntCon(phiArgSSANum);
                    JITDUMP("  Building phi application: $%x = SSA# %d.\n", phiArgSSANumVN, phiArgSSANum);
                    phiAppVN = vnStore->VNForFuncNoFolding(TYP_HEAP, VNF_Phi, phiArgSSANumVN, phiAppVN);
                    JITDUMP("  Building phi application: $%x = phi($%x, $%x).\n", phiAppVN, phiArgSSANumVN,
                            oldPhiAppVN);
                    phiArgs = phiArgs->m_nextArg;
                }
                if (allSame)
                {
                    newMemoryVN = sameVN;
                }
                else
                {
                    newMemoryVN = vnStore->VNForFuncNoFolding(TYP_HEAP, VNF_PhiMemoryDef,
                                                              vnStore->VNForHandle(ssize_t(blk), GTF_EMPTY), phiAppVN);
                }
            }
            GetMemoryPerSsaData(blk->bbMemorySsaNumIn[memoryKind])->m_vnPair.SetLiberal(newMemoryVN);
            fgSetCurrentMemoryVN(memoryKind, newMemoryVN);
            if ((memoryKind == GcHeap) && byrefStatesMatchGcHeapStates)
            {
                // Keep the CurMemoryVNs in sync
                fgSetCurrentMemoryVN(ByrefExposed, newMemoryVN);
            }
        }
#ifdef DEBUG
        if (verbose)
        {
            printf("The SSA definition for %s (#%d) at start of " FMT_BB " is ", memoryKindNames[memoryKind],
                   blk->bbMemorySsaNumIn[memoryKind], blk->bbNum);
            vnPrint(fgCurMemoryVN[memoryKind], 1);
            printf("\n");
        }
#endif // DEBUG
    }

    // Now iterate over the remaining statements, and their trees.
    for (; stmt != nullptr; stmt = stmt->GetNextStmt())
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\n***** " FMT_BB ", " FMT_STMT "(before)\n", blk->bbNum, stmt->GetID());
            gtDispTree(stmt->GetRootNode());
            printf("\n");
        }
#endif

        for (GenTree* const tree : stmt->TreeList())
        {
            // Set up ambient var referring to current tree.
            compCurTree = tree;
            fgValueNumberTree(tree);
            compCurTree = nullptr;
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("\n***** " FMT_BB ", " FMT_STMT "(after)\n", blk->bbNum, stmt->GetID());
            gtDispTree(stmt->GetRootNode());
            printf("\n");
            if (stmt->GetNextStmt() != nullptr)
            {
                printf("---------\n");
            }
        }
#endif
    }

    for (MemoryKind memoryKind : allMemoryKinds())
    {
        if ((memoryKind == GcHeap) && byrefStatesMatchGcHeapStates)
        {
            // The update to the shared SSA data will have already happened for ByrefExposed.
            assert(memoryKind > ByrefExposed);
            assert(blk->bbMemorySsaNumOut[memoryKind] == blk->bbMemorySsaNumOut[ByrefExposed]);
            assert(GetMemoryPerSsaData(blk->bbMemorySsaNumOut[memoryKind])->m_vnPair.GetLiberal() ==
                   fgCurMemoryVN[memoryKind]);
            continue;
        }

        if (blk->bbMemorySsaNumOut[memoryKind] != blk->bbMemorySsaNumIn[memoryKind])
        {
            GetMemoryPerSsaData(blk->bbMemorySsaNumOut[memoryKind])->m_vnPair.SetLiberal(fgCurMemoryVN[memoryKind]);
        }
    }

    compCurBB = nullptr;
}

ValueNum Compiler::fgMemoryVNForLoopSideEffects(MemoryKind            memoryKind,
                                                BasicBlock*           entryBlock,
                                                FlowGraphNaturalLoop* loop)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("Computing %s state for block " FMT_BB ", entry block for loop " FMT_LP ":\n",
               memoryKindNames[memoryKind], entryBlock->bbNum, loop->GetIndex());
    }
#endif // DEBUG

    const LoopSideEffects& sideEffs = m_loopSideEffects[loop->GetIndex()];

    // If this loop has memory havoc effects, just use a new, unique VN.
    if (sideEffs.HasMemoryHavoc[memoryKind])
    {
        ValueNum res = vnStore->VNForExpr(entryBlock, TYP_HEAP);
#ifdef DEBUG
        if (verbose)
        {
            printf("  Loop " FMT_LP " has memory havoc effect; heap state is new unique $%x.\n", loop->GetIndex(), res);
        }
#endif // DEBUG
        return res;
    }

    // Otherwise, find the predecessors of the entry block that are not in the loop.
    // If there is only one such, use its memory value as the "base."  If more than one,
    // use a new unique VN.
    // TODO-Cleanup: Ensure canonicalization creates loop preheaders properly for handlers
    // and simplify this logic.
    BasicBlock* nonLoopPred          = nullptr;
    bool        multipleNonLoopPreds = false;
    for (FlowEdge* pred = BlockPredsWithEH(entryBlock); pred != nullptr; pred = pred->getNextPredEdge())
    {
        BasicBlock* predBlock = pred->getSourceBlock();
        if (!loop->ContainsBlock(predBlock))
        {
            if (nonLoopPred == nullptr)
            {
                nonLoopPred = predBlock;
            }
            else
            {
#ifdef DEBUG
                if (verbose)
                {
                    printf("  Entry block has >1 non-loop preds: (at least) " FMT_BB " and " FMT_BB ".\n",
                           nonLoopPred->bbNum, predBlock->bbNum);
                }
#endif // DEBUG
                multipleNonLoopPreds = true;
                break;
            }
        }
    }
    if (multipleNonLoopPreds)
    {
        ValueNum res = vnStore->VNForExpr(entryBlock, TYP_HEAP);
#ifdef DEBUG
        if (verbose)
        {
            printf("  Therefore, memory state is new, fresh $%x.\n", res);
        }
#endif // DEBUG
        return res;
    }
    // Otherwise, there is a single non-loop pred.
    assert(nonLoopPred != nullptr);
    // What is its memory post-state?
    ValueNum newMemoryVN = GetMemoryPerSsaData(nonLoopPred->bbMemorySsaNumOut[memoryKind])->m_vnPair.GetLiberal();
    assert(newMemoryVN != ValueNumStore::NoVN); // We must have processed the single non-loop pred before reaching the
                                                // loop entry.

#ifdef DEBUG
    if (verbose)
    {
        printf("  Init %s state is $%x, with new, fresh VN at:\n", memoryKindNames[memoryKind], newMemoryVN);
    }
#endif // DEBUG
    // Modify "base" by setting all the modified fields/field maps/array maps to unknown values.
    // These annotations apply specifically to the GcHeap, where we disambiguate across such stores.
    if (memoryKind == GcHeap)
    {
        // First the fields/field maps.
        FieldHandleSet* fieldsMod = sideEffs.FieldsModified;
        if (fieldsMod != nullptr)
        {
            for (FieldHandleSet::Node* const ki : FieldHandleSet::KeyValueIteration(fieldsMod))
            {
                CORINFO_FIELD_HANDLE fldHnd    = ki->GetKey();
                FieldKindForVN       fieldKind = ki->GetValue();
                ValueNum             fldHndVN  = vnStore->VNForHandle(ssize_t(fldHnd), GTF_ICON_FIELD_HDL);

#ifdef DEBUG
                if (verbose)
                {
                    char        buffer[128];
                    const char* fldName = eeGetFieldName(fldHnd, false, buffer, sizeof(buffer));
                    printf("     VNForHandle(%s) is " FMT_VN "\n", fldName, fldHndVN);
                }
#endif // DEBUG

                // Instance fields and "complex" statics select "first field maps"
                // with a placeholder type. "Simple" statics select their own types.
                var_types fldMapType = (fieldKind == FieldKindForVN::WithBaseAddr) ? TYP_MEM : eeGetFieldType(fldHnd);

                newMemoryVN = vnStore->VNForMapStore(newMemoryVN, fldHndVN, vnStore->VNForExpr(entryBlock, fldMapType));
            }
        }
        // Now do the array maps.
        ClassHandleSet* elemTypesMod = sideEffs.ArrayElemTypesModified;
        if (elemTypesMod != nullptr)
        {
            for (const CORINFO_CLASS_HANDLE elemClsHnd : ClassHandleSet::KeyIteration(elemTypesMod))
            {
#ifdef DEBUG
                if (verbose)
                {
                    var_types elemTyp = DecodeElemType(elemClsHnd);
                    // If a valid class handle is given when the ElemType is set, DecodeElemType will
                    // return TYP_STRUCT, and elemClsHnd is that handle.
                    // Otherwise, elemClsHnd is NOT a valid class handle, and is the encoded var_types value.
                    if (elemTyp == TYP_STRUCT)
                    {
                        printf("     Array map %s[]\n", eeGetClassName(elemClsHnd));
                    }
                    else
                    {
                        printf("     Array map %s[]\n", varTypeName(elemTyp));
                    }
                }
#endif // DEBUG

                ValueNum elemTypeVN = vnStore->VNForHandle(ssize_t(elemClsHnd), GTF_ICON_CLASS_HDL);
                ValueNum uniqueVN   = vnStore->VNForExpr(entryBlock, TYP_MEM);
                newMemoryVN         = vnStore->VNForMapStore(newMemoryVN, elemTypeVN, uniqueVN);
            }
        }
    }
    else
    {
        // If there were any fields/elements modified, this should have been recorded as havoc
        // for ByrefExposed.
        assert(memoryKind == ByrefExposed);
        assert((sideEffs.FieldsModified == nullptr) || sideEffs.HasMemoryHavoc[memoryKind]);
        assert((sideEffs.ArrayElemTypesModified == nullptr) || sideEffs.HasMemoryHavoc[memoryKind]);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("  Final %s state is $%x.\n", memoryKindNames[memoryKind], newMemoryVN);
    }
#endif // DEBUG
    return newMemoryVN;
}

void Compiler::fgMutateGcHeap(GenTree* tree DEBUGARG(const char* msg))
{
    // Update the current memory VN, and if we're tracking the heap SSA # caused by this node, record it.
    recordGcHeapStore(tree, vnStore->VNForExpr(compCurBB, TYP_HEAP) DEBUGARG(msg));
}

void Compiler::fgMutateAddressExposedLocal(GenTree* tree DEBUGARG(const char* msg))
{
    // Update the current ByrefExposed VN, and if we're tracking the heap SSA # caused by this node, record it.
    recordAddressExposedLocalStore(tree, vnStore->VNForExpr(compCurBB, TYP_HEAP) DEBUGARG(msg));
}

void Compiler::recordGcHeapStore(GenTree* curTree, ValueNum gcHeapVN DEBUGARG(const char* msg))
{
    // bbMemoryDef must include GcHeap for any block that mutates the GC Heap
    // and GC Heap mutations are also ByrefExposed mutations
    assert((compCurBB->bbMemoryDef & memoryKindSet(GcHeap, ByrefExposed)) == memoryKindSet(GcHeap, ByrefExposed));
    fgSetCurrentMemoryVN(GcHeap, gcHeapVN);

    if (byrefStatesMatchGcHeapStates)
    {
        // Since GcHeap and ByrefExposed share SSA nodes, they need to share
        // value numbers too.
        fgSetCurrentMemoryVN(ByrefExposed, gcHeapVN);
    }
    else
    {
        // GcHeap and ByrefExposed have different defnums and VNs.  We conservatively
        // assume that this GcHeap store may alias any byref load/store, so don't
        // bother trying to record the map/select stuff, and instead just an opaque VN
        // for ByrefExposed
        fgSetCurrentMemoryVN(ByrefExposed, vnStore->VNForExpr(compCurBB, TYP_HEAP));
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("  fgCurMemoryVN[GcHeap] assigned for %s at ", msg);
        Compiler::printTreeID(curTree);
        printf(" to VN: " FMT_VN ".\n", gcHeapVN);
    }
#endif // DEBUG

    // If byrefStatesMatchGcHeapStates is true, then since GcHeap and ByrefExposed share
    // their SSA map entries, the below will effectively update both.
    fgValueNumberRecordMemorySsa(GcHeap, curTree);
}

void Compiler::recordAddressExposedLocalStore(GenTree* curTree, ValueNum memoryVN DEBUGARG(const char* msg))
{
    // This should only happen if GcHeap and ByrefExposed are being tracked separately;
    // otherwise we'd go through recordGcHeapStore.
    assert(!byrefStatesMatchGcHeapStates);

    // bbMemoryDef must include ByrefExposed for any block that mutates an address-exposed local
    assert((compCurBB->bbMemoryDef & memoryKindSet(ByrefExposed)) != 0);
    fgSetCurrentMemoryVN(ByrefExposed, memoryVN);

#ifdef DEBUG
    if (verbose)
    {
        printf("  fgCurMemoryVN[ByrefExposed] assigned for %s at ", msg);
        Compiler::printTreeID(curTree);
        printf(" to VN: " FMT_VN ".\n", memoryVN);
    }
#endif // DEBUG

    fgValueNumberRecordMemorySsa(ByrefExposed, curTree);
}

void Compiler::fgSetCurrentMemoryVN(MemoryKind memoryKind, ValueNum newMemoryVN)
{
    assert(vnStore->VNIsValid(newMemoryVN));
    assert(vnStore->TypeOfVN(newMemoryVN) == TYP_HEAP);
    fgCurMemoryVN[memoryKind] = newMemoryVN;
}

void Compiler::fgValueNumberRecordMemorySsa(MemoryKind memoryKind, GenTree* tree)
{
    unsigned ssaNum;
    if (GetMemorySsaMap(memoryKind)->Lookup(tree, &ssaNum))
    {
        GetMemoryPerSsaData(ssaNum)->m_vnPair.SetLiberal(fgCurMemoryVN[memoryKind]);
#ifdef DEBUG
        if (verbose)
        {
            printf("Node ");
            Compiler::printTreeID(tree);
            printf(" sets %s SSA # %d to VN $%x: ", memoryKindNames[memoryKind], ssaNum, fgCurMemoryVN[memoryKind]);
            vnStore->vnDump(this, fgCurMemoryVN[memoryKind]);
            printf("\n");
        }
#endif // DEBUG
    }
}

// The input 'tree' is a leaf node that is a constant
// Assign the proper value number to the tree
void Compiler::fgValueNumberTreeConst(GenTree* tree)
{
    genTreeOps oper = tree->OperGet();
    var_types  typ  = tree->TypeGet();
    assert(GenTree::OperIsConst(oper));

    switch (typ)
    {
        case TYP_LONG:
        case TYP_ULONG:
        case TYP_INT:
        case TYP_UINT:
        case TYP_USHORT:
        case TYP_SHORT:
        case TYP_BYTE:
        case TYP_UBYTE:
            if (tree->IsIconHandle())
            {
                const GenTreeIntCon* cns         = tree->AsIntCon();
                const GenTreeFlags   handleFlags = tree->GetIconHandleFlag();
                tree->gtVNPair.SetBoth(vnStore->VNForHandle(cns->IconValue(), handleFlags));
                if (handleFlags == GTF_ICON_CLASS_HDL)
                {
                    vnStore->AddToEmbeddedHandleMap(cns->IconValue(), cns->gtCompileTimeHandle);
                }
            }
            else if ((typ == TYP_LONG) || (typ == TYP_ULONG))
            {
                tree->gtVNPair.SetBoth(vnStore->VNForLongCon(INT64(tree->AsIntConCommon()->LngValue())));
            }
            else
            {
                tree->gtVNPair.SetBoth(vnStore->VNForIntCon(int(tree->AsIntConCommon()->IconValue())));
            }

            if (tree->IsCnsIntOrI())
            {
                fgValueNumberRegisterConstFieldSeq(tree->AsIntCon());
            }

            break;

#ifdef FEATURE_SIMD
        case TYP_SIMD8:
        {
            simd8_t simd8Val;
            memcpy(&simd8Val, &tree->AsVecCon()->gtSimdVal, sizeof(simd8_t));

            tree->gtVNPair.SetBoth(vnStore->VNForSimd8Con(simd8Val));
            break;
        }

        case TYP_SIMD12:
        {
            simd12_t simd12Val;
            memcpy(&simd12Val, &tree->AsVecCon()->gtSimdVal, sizeof(simd12_t));

            tree->gtVNPair.SetBoth(vnStore->VNForSimd12Con(simd12Val));
            break;
        }

        case TYP_SIMD16:
        {
            simd16_t simd16Val;
            memcpy(&simd16Val, &tree->AsVecCon()->gtSimdVal, sizeof(simd16_t));

            tree->gtVNPair.SetBoth(vnStore->VNForSimd16Con(simd16Val));
            break;
        }

#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        {
            simd32_t simd32Val;
            memcpy(&simd32Val, &tree->AsVecCon()->gtSimdVal, sizeof(simd32_t));

            tree->gtVNPair.SetBoth(vnStore->VNForSimd32Con(simd32Val));
            break;
        }

        case TYP_SIMD64:
        {
            simd64_t simd64Val;
            memcpy(&simd64Val, &tree->AsVecCon()->gtSimdVal, sizeof(simd64_t));

            tree->gtVNPair.SetBoth(vnStore->VNForSimd64Con(simd64Val));
            break;
        }
#endif // TARGET_XARCH
#endif // FEATURE_SIMD

        case TYP_FLOAT:
        {
            float f32Cns = FloatingPointUtils::convertToSingle(tree->AsDblCon()->DconValue());
            tree->gtVNPair.SetBoth(vnStore->VNForFloatCon(f32Cns));
            break;
        }

        case TYP_DOUBLE:
        {
            tree->gtVNPair.SetBoth(vnStore->VNForDoubleCon(tree->AsDblCon()->DconValue()));
            break;
        }

        case TYP_REF:
            if (tree->AsIntConCommon()->IconValue() == 0)
            {
                tree->gtVNPair.SetBoth(ValueNumStore::VNForNull());
            }
            else
            {
                assert(doesMethodHaveFrozenObjects());
                tree->gtVNPair.SetBoth(
                    vnStore->VNForHandle(ssize_t(tree->AsIntConCommon()->IconValue()), tree->GetIconHandleFlag()));

                fgValueNumberRegisterConstFieldSeq(tree->AsIntCon());
            }
            break;

        case TYP_BYREF:
            if (tree->AsIntConCommon()->IconValue() == 0)
            {
                tree->gtVNPair.SetBoth(ValueNumStore::VNForNull());
            }
            else
            {
                assert(tree->IsCnsIntOrI());

                if (tree->IsIconHandle())
                {
                    tree->gtVNPair.SetBoth(
                        vnStore->VNForHandle(ssize_t(tree->AsIntConCommon()->IconValue()), tree->GetIconHandleFlag()));

                    fgValueNumberRegisterConstFieldSeq(tree->AsIntCon());
                }
                else
                {
                    tree->gtVNPair.SetBoth(vnStore->VNForByrefCon((target_size_t)tree->AsIntConCommon()->IconValue()));
                }
            }
            break;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// fgValueNumberRegisterConstFieldSeq: If a VN'd integer constant has a
// field sequence we want to keep track of, then register it in the side table.
//
// Arguments:
//   tree - the integer constant
//
void Compiler::fgValueNumberRegisterConstFieldSeq(GenTreeIntCon* tree)
{
    if (tree->gtFieldSeq == nullptr)
    {
        return;
    }

    if (tree->gtFieldSeq->GetKind() != FieldSeq::FieldKind::SimpleStaticKnownAddress)
    {
        return;
    }

    // For now we're interested only in SimpleStaticKnownAddress
    vnStore->AddToFieldAddressToFieldSeqMap(tree->gtVNPair.GetLiberal(), tree->gtFieldSeq);
}

//------------------------------------------------------------------------
// fgValueNumberStore: Does value numbering for a store.
//
// While this method does indeed give a VN to the store tree itself, its
// main objective is to update the various state that holds values, i.e.
// the per-SSA VNs for tracked variables and the heap states for analyzable
// (to fields and arrays) stores.
//
// Arguments:
//    tree - the store tree
//
void Compiler::fgValueNumberStore(GenTree* store)
{
    assert(store->OperIsStore());

    GenTree* data = store->Data();

    // Only normal values are to be stored in SSA defs, VN maps, etc.
    ValueNumPair dataExcSet;
    ValueNumPair dataVNPair;
    vnStore->VNPUnpackExc(data->gtVNPair, &dataVNPair, &dataExcSet);
    assert(dataVNPair.BothDefined());

    // Is the type being stored different from the type computed by "data"?
    if (data->TypeGet() != store->TypeGet())
    {
        if (store->OperIsInitBlkOp())
        {
            ValueNum initObjVN;
            if (data->IsIntegralConst(0))
            {
                initObjVN = vnStore->VNForZeroObj(store->GetLayout(this));
            }
            else
            {
                initObjVN = vnStore->VNForExpr(compCurBB, TYP_STRUCT);
            }

            dataVNPair.SetBoth(initObjVN);
        }
        else if (data->TypeGet() == TYP_REF)
        {
            // If we have an unsafe IL assignment of a TYP_REF to a non-ref (typically a TYP_BYREF)
            // then don't propagate this ValueNumber to the lhs, instead create a new unique VN.
            dataVNPair.SetBoth(vnStore->VNForExpr(compCurBB, store->TypeGet()));
        }
        else
        {
            // This means that there is an implicit cast on the rhs value
            // We will add a cast function to reflect the possible narrowing of the rhs value
            dataVNPair = vnStore->VNPairForCast(dataVNPair, store->TypeGet(), data->TypeGet());
        }
    }

    // Now, record the new VN for an assignment (performing the indicated "state update").
    // It's safe to use gtEffectiveVal here, because the non-last elements of a comma list on the
    // LHS will come before the assignment in evaluation order.
    switch (store->OperGet())
    {
        case GT_STORE_LCL_VAR:
        {
            GenTreeLclVarCommon* lcl = store->AsLclVarCommon();
            fgValueNumberLocalStore(store, lcl, 0, lvaLclExactSize(lcl->GetLclNum()), dataVNPair,
                                    /* normalize */ false);
        }
        break;

        case GT_STORE_LCL_FLD:
        {
            GenTreeLclFld* lclFld = store->AsLclFld();
            fgValueNumberLocalStore(store, lclFld, lclFld->GetLclOffs(), lclFld->GetSize(), dataVNPair);
        }
        break;

        case GT_STOREIND:
        case GT_STORE_BLK:
        {
            if (store->AsIndir()->IsVolatile())
            {
                // For Volatile store indirection, first mutate GcHeap/ByrefExposed
                fgMutateGcHeap(store DEBUGARG("GTF_IND_VOLATILE - store"));
            }

            GenTree*  addr = store->AsIndir()->Addr();
            VNFuncApp funcApp;
            ValueNum  addrVN       = addr->gtVNPair.GetLiberal();
            bool      addrIsVNFunc = vnStore->GetVNFunc(vnStore->VNNormalValue(addrVN), &funcApp);

            GenTreeLclVarCommon* lclVarTree = nullptr;
            ssize_t              offset     = 0;
            unsigned             storeSize  = store->AsIndir()->Size();
            GenTree*             baseAddr   = nullptr;
            FieldSeq*            fldSeq     = nullptr;

            if (addrIsVNFunc && (funcApp.m_func == VNF_PtrToStatic))
            {
                baseAddr = nullptr; // All VNF_PtrToStatic statics are currently "simple".
                fldSeq   = vnStore->FieldSeqVNToFieldSeq(funcApp.m_args[1]);
                offset   = vnStore->ConstantValue<ssize_t>(funcApp.m_args[2]);

                fgValueNumberFieldStore(store, baseAddr, fldSeq, offset, storeSize, dataVNPair.GetLiberal());
            }
            else if (addrIsVNFunc && (funcApp.m_func == VNF_PtrToArrElem))
            {
                fgValueNumberArrayElemStore(store, &funcApp, storeSize, dataVNPair.GetLiberal());
            }
            else if (addr->IsFieldAddr(this, &baseAddr, &fldSeq, &offset))
            {
                assert(fldSeq != nullptr);
                fgValueNumberFieldStore(store, baseAddr, fldSeq, offset, storeSize, dataVNPair.GetLiberal());
            }
            else
            {
                assert(!store->DefinesLocal(this, &lclVarTree));
                // If it doesn't define a local, then it might update GcHeap/ByrefExposed.
                // For the new ByrefExposed VN, we could use an operator here like
                // VNF_ByrefExposedStore that carries the VNs of the pointer and RHS, then
                // at byref loads if the current ByrefExposed VN happens to be
                // VNF_ByrefExposedStore with the same pointer VN, we could propagate the
                // VN from the RHS to the VN for the load.  This would e.g. allow tracking
                // values through assignments to out params.  For now, just model this
                // as an opaque GcHeap/ByrefExposed mutation.
                fgMutateGcHeap(store DEBUGARG("assign-of-IND"));
            }
        }
        break;

        default:
            unreached();
    }

    // Stores produce no values, and as such are given the "Void" VN.
    ValueNumPair storeExcSet = dataExcSet;
    if (store->OperIsIndir())
    {
        storeExcSet = vnStore->VNPUnionExcSet(store->AsIndir()->Addr()->gtVNPair, storeExcSet);
    }
    store->gtVNPair = vnStore->VNPWithExc(vnStore->VNPForVoid(), storeExcSet);
}

//------------------------------------------------------------------------
// fgValueNumberSsaVarDef: Perform value numbering for an SSA variable use.
//
// Arguments:
//    lcl - the LCL_VAR node
//
void Compiler::fgValueNumberSsaVarDef(GenTreeLclVarCommon* lcl)
{
    assert(lcl->OperIs(GT_LCL_VAR) && lcl->HasSsaName());

    unsigned   lclNum = lcl->GetLclNum();
    LclVarDsc* varDsc = lvaGetDesc(lclNum);

    ValueNumPair wholeLclVarVNP = varDsc->GetPerSsaData(lcl->GetSsaNum())->m_vnPair;
    assert(wholeLclVarVNP.BothDefined());

    // Account for type mismatches.
    if (genActualType(varDsc) != genActualType(lcl))
    {
        if (genTypeSize(varDsc) != genTypeSize(lcl))
        {
            assert((varDsc->TypeGet() == TYP_LONG) && lcl->TypeIs(TYP_INT));
            lcl->gtVNPair = vnStore->VNPairForCast(wholeLclVarVNP, lcl->TypeGet(), varDsc->TypeGet());
        }
        else
        {
            assert(((varDsc->TypeGet() == TYP_I_IMPL) && lcl->TypeIs(TYP_BYREF)) ||
                   ((varDsc->TypeGet() == TYP_BYREF) && lcl->TypeIs(TYP_I_IMPL)));
            lcl->gtVNPair = wholeLclVarVNP;
        }
    }
    else
    {
        lcl->gtVNPair = wholeLclVarVNP;
    }
}

//----------------------------------------------------------------------------------
// fgGetStaticFieldSeqAndAddress: Try to obtain a constant address with a FieldSeq from the
//    given tree. It can be either INT_CNS or e.g. ADD(INT_CNS, ADD(INT_CNS, INT_CNS))
//    tree where only one of the constants is expected to have a field sequence.
//
// Arguments:
//    vnStore    - ValueNumStore object
//    tree       - tree node to inspect
//    byteOffset - [Out] resulting byte offset
//    pFseq      - [Out] field sequence
//
// Return Value:
//    true if the given tree is a static field address
//
static bool GetStaticFieldSeqAndAddress(ValueNumStore* vnStore, GenTree* tree, ssize_t* byteOffset, FieldSeq** pFseq)
{
    VNFuncApp funcApp;
    if (vnStore->GetVNFunc(tree->gtVNPair.GetLiberal(), &funcApp) && (funcApp.m_func == VNF_PtrToStatic))
    {
        FieldSeq* fseq = vnStore->FieldSeqVNToFieldSeq(funcApp.m_args[1]);
        // TODO-Cleanup: We may see null field seqs here due to how the base of
        // boxed statics are VN'd. We should get rid of this case.
        if ((fseq != nullptr) && (fseq->GetKind() == FieldSeq::FieldKind::SimpleStatic))
        {
            *byteOffset = vnStore->ConstantValue<ssize_t>(funcApp.m_args[2]);
            *pFseq      = fseq;
            return true;
        }
    }
    ssize_t val = 0;

    // Special cases for NativeAOT:
    //   ADD(ICON_STATIC, CNS_INT)                // nonGC-static base
    //   ADD(IND(ICON_STATIC_ADDR_PTR), CNS_INT)  // GC-static base
    // where CNS_INT has field sequence corresponding to field's offset
    if (tree->OperIs(GT_ADD) && tree->gtGetOp2()->IsCnsIntOrI() && !tree->gtGetOp2()->IsIconHandle())
    {
        GenTreeIntCon* cns2 = tree->gtGetOp2()->AsIntCon();
        if ((cns2->gtFieldSeq != nullptr) && (cns2->gtFieldSeq->GetKind() == FieldSeq::FieldKind::SimpleStatic))
        {
            *byteOffset = cns2->IconValue() - cns2->gtFieldSeq->GetOffset();
            *pFseq      = cns2->gtFieldSeq;
            return true;
        }
    }

    // Accumulate final offset
    while (tree->OperIs(GT_ADD))
    {
        GenTree* op1   = tree->gtGetOp1();
        GenTree* op2   = tree->gtGetOp2();
        ValueNum op1vn = op1->gtVNPair.GetLiberal();
        ValueNum op2vn = op2->gtVNPair.GetLiberal();

        if (op1->gtVNPair.BothEqual() && vnStore->IsVNConstant(op1vn) && !vnStore->IsVNHandle(op1vn) &&
            varTypeIsIntegral(vnStore->TypeOfVN(op1vn)))
        {
            val += vnStore->CoercedConstantValue<ssize_t>(op1vn);
            tree = op2;
        }
        else if (op2->gtVNPair.BothEqual() && vnStore->IsVNConstant(op2vn) && !vnStore->IsVNHandle(op2vn) &&
                 varTypeIsIntegral(vnStore->TypeOfVN(op2vn)))
        {
            val += vnStore->CoercedConstantValue<ssize_t>(op2vn);
            tree = op1;
        }
        else
        {
            // We only inspect constants and additions
            return false;
        }
    }

    // Base address is expected to be static field's address
    ValueNum treeVN = tree->gtVNPair.GetLiberal();
    if (tree->gtVNPair.BothEqual() && vnStore->IsVNConstant(treeVN))
    {
        FieldSeq* fldSeq = vnStore->GetFieldSeqFromAddress(treeVN);
        if (fldSeq != nullptr)
        {
            assert(fldSeq->GetKind() == FieldSeq::FieldKind::SimpleStaticKnownAddress);
            *pFseq      = fldSeq;
            *byteOffset = vnStore->CoercedConstantValue<ssize_t>(treeVN) - fldSeq->GetOffset() + val;
            return true;
        }
    }
    return false;
}

//----------------------------------------------------------------------------------
// GetObjectHandleAndOffset: Try to obtain a constant object handle with an offset from
//    the given tree.
//
// Arguments:
//    tree       - tree node to inspect
//    byteOffset - [Out] resulting byte offset
//    pObj       - [Out] constant object handle
//
// Return Value:
//    true if the given tree is a ObjHandle + CNS
//
bool Compiler::GetObjectHandleAndOffset(GenTree* tree, ssize_t* byteOffset, CORINFO_OBJECT_HANDLE* pObj)
{
    if (!tree->gtVNPair.BothEqual())
    {
        return false;
    }

    ValueNum       treeVN = tree->gtVNPair.GetLiberal();
    VNFuncApp      funcApp;
    target_ssize_t offset = 0;
    while (vnStore->GetVNFunc(treeVN, &funcApp) && (funcApp.m_func == (VNFunc)GT_ADD))
    {
        if (vnStore->IsVNConstantNonHandle(funcApp.m_args[0]) && (vnStore->TypeOfVN(funcApp.m_args[0]) == TYP_I_IMPL))
        {
            offset += vnStore->ConstantValue<target_ssize_t>(funcApp.m_args[0]);
            treeVN = funcApp.m_args[1];
        }
        else if (vnStore->IsVNConstantNonHandle(funcApp.m_args[1]) &&
                 (vnStore->TypeOfVN(funcApp.m_args[1]) == TYP_I_IMPL))
        {
            offset += vnStore->ConstantValue<target_ssize_t>(funcApp.m_args[1]);
            treeVN = funcApp.m_args[0];
        }
        else
        {
            return false;
        }
    }

    if (vnStore->IsVNObjHandle(treeVN))
    {
        *pObj       = vnStore->ConstantObjHandle(treeVN);
        *byteOffset = offset;
        return true;
    }
    return false;
}

//----------------------------------------------------------------------------------
// fgValueNumberConstLoad: Try to detect const_immutable_array[cns_index] tree
//    and apply a constant VN representing given element at cns_index in that array.
//
// Arguments:
//    tree - the GT_IND node
//
// Return Value:
//    true if the pattern was recognized and a new VN is assigned
//
bool Compiler::fgValueNumberConstLoad(GenTreeIndir* tree)
{
    if (!tree->gtVNPair.BothEqual())
    {
        return false;
    }

    // First, let's check if we can detect RVA[const_index] pattern to fold, e.g.:
    //
    //   static ReadOnlySpan<sbyte> RVA => new sbyte[] { -100, 100 }
    //
    //   sbyte GetVal() => RVA[1]; // fold to '100'
    //
    ssize_t               byteOffset     = 0;
    FieldSeq*             fieldSeq       = nullptr;
    CORINFO_OBJECT_HANDLE obj            = nullptr;
    int                   size           = (int)genTypeSize(tree->TypeGet());
    const int             maxElementSize = sizeof(simd_t);

    if (!tree->TypeIs(TYP_BYREF, TYP_STRUCT) &&
        GetStaticFieldSeqAndAddress(vnStore, tree->gtGetOp1(), &byteOffset, &fieldSeq))
    {
        CORINFO_FIELD_HANDLE fieldHandle = fieldSeq->GetFieldHandle();
        if ((fieldHandle != nullptr) && (size > 0) && (size <= maxElementSize) && ((size_t)byteOffset < INT_MAX))
        {
            uint8_t buffer[maxElementSize] = {0};
            if (info.compCompHnd->getStaticFieldContent(fieldHandle, buffer, size, (int)byteOffset))
            {
                ValueNum vn = vnStore->VNForGenericCon(tree->TypeGet(), buffer);
                if (vnStore->IsVNObjHandle(vn))
                {
                    setMethodHasFrozenObjects();
                }
                tree->gtVNPair.SetBoth(vn);
                return true;
            }
        }
    }
    else if (!tree->TypeIs(TYP_REF, TYP_BYREF, TYP_STRUCT) &&
             GetObjectHandleAndOffset(tree->gtGetOp1(), &byteOffset, &obj))
    {
        // See if we can fold IND(ADD(FrozenObj, CNS)) to a constant
        assert(obj != nullptr);
        if ((size > 0) && (size <= maxElementSize) && ((size_t)byteOffset < INT_MAX))
        {
            uint8_t buffer[maxElementSize] = {0};
            if (info.compCompHnd->getObjectContent(obj, buffer, size, (int)byteOffset))
            {
                // If we have IND<size_t>(frozenObj) then it means we're reading object type
                // so make sure we report the constant as class handle
                if ((size == TARGET_POINTER_SIZE) && (byteOffset == 0))
                {
                    // In case of 64bit jit emitting 32bit codegen this handle will be 64bit
                    // value holding 32bit handle with upper half zeroed (hence, "= NULL").
                    // It's done to match the current crossgen/ILC behavior.
                    CORINFO_CLASS_HANDLE rawHandle = NULL;
                    memcpy(&rawHandle, buffer, TARGET_POINTER_SIZE);

                    void* pEmbedClsHnd;
                    void* embedClsHnd = (void*)info.compCompHnd->embedClassHandle(rawHandle, &pEmbedClsHnd);
                    if (pEmbedClsHnd == nullptr)
                    {
                        // getObjectContent doesn't support reading handles for AOT (NativeAOT) yet
                        tree->gtVNPair.SetBoth(vnStore->VNForHandle((ssize_t)embedClsHnd, GTF_ICON_CLASS_HDL));
                        return true;
                    }
                }
                else
                {
                    ValueNum vn = vnStore->VNForGenericCon(tree->TypeGet(), buffer);
                    assert(!vnStore->IsVNObjHandle(vn));
                    tree->gtVNPair.SetBoth(vn);
                    return true;
                }
            }
        }
    }

    // Throughput check, the logic below is only for USHORT (char)
    if (!tree->OperIs(GT_IND) || !tree->TypeIs(TYP_USHORT))
    {
        return false;
    }

    ValueNum  addrVN = tree->gtGetOp1()->gtVNPair.GetLiberal();
    VNFuncApp funcApp;
    if (!vnStore->GetVNFunc(addrVN, &funcApp))
    {
        return false;
    }

    // Is given VN representing a frozen object handle
    auto isCnsObjHandle = [](ValueNumStore* vnStore, ValueNum vn, CORINFO_OBJECT_HANDLE* handle) -> bool {
        if (vnStore->IsVNObjHandle(vn))
        {
            *handle = vnStore->ConstantObjHandle(vn);
            return true;
        }
        return false;
    };

    CORINFO_OBJECT_HANDLE objHandle = NO_OBJECT_HANDLE;
    size_t                index     = -1;

    // First, let see if we have PtrToArrElem
    ValueNum addr = funcApp.m_args[0];
    if (funcApp.m_func == VNF_PtrToArrElem)
    {
        ValueNum arrVN  = funcApp.m_args[1];
        ValueNum inxVN  = funcApp.m_args[2];
        ssize_t  offset = vnStore->ConstantValue<ssize_t>(funcApp.m_args[3]);

        if (isCnsObjHandle(vnStore, arrVN, &objHandle) && (offset == 0) && vnStore->IsVNConstant(inxVN))
        {
            index = vnStore->CoercedConstantValue<size_t>(inxVN);
        }
    }
    else if (funcApp.m_func == (VNFunc)GT_ADD)
    {
        ssize_t  dataOffset = 0;
        ValueNum baseVN     = ValueNumStore::NoVN;

        // Loop to accumulate total dataOffset, e.g.:
        // ADD(C1, ADD(ObjHandle, C2)) -> C1 + C2
        do
        {
            ValueNum op1VN = funcApp.m_args[0];
            ValueNum op2VN = funcApp.m_args[1];

            if (vnStore->IsVNConstant(op1VN) && varTypeIsIntegral(vnStore->TypeOfVN(op1VN)) &&
                !isCnsObjHandle(vnStore, op1VN, &objHandle))
            {
                dataOffset += vnStore->CoercedConstantValue<ssize_t>(op1VN);
                baseVN = op2VN;
            }
            else if (vnStore->IsVNConstant(op2VN) && varTypeIsIntegral(vnStore->TypeOfVN(op2VN)) &&
                     !isCnsObjHandle(vnStore, op2VN, &objHandle))
            {
                dataOffset += vnStore->CoercedConstantValue<ssize_t>(op2VN);
                baseVN = op1VN;
            }
            else
            {
                // one of the args is expected to be an integer constant
                return false;
            }
        } while (vnStore->GetVNFunc(baseVN, &funcApp) && (funcApp.m_func == (VNFunc)GT_ADD));

        if (isCnsObjHandle(vnStore, baseVN, &objHandle) && (dataOffset >= (ssize_t)OFFSETOF__CORINFO_String__chars) &&
            ((dataOffset % 2) == 0))
        {
            static_assert_no_msg((OFFSETOF__CORINFO_String__chars % 2) == 0);
            index = (dataOffset - OFFSETOF__CORINFO_String__chars) / 2;
        }
    }

    USHORT charValue;
    if (((size_t)index < INT_MAX) && (objHandle != NO_OBJECT_HANDLE) &&
        info.compCompHnd->getStringChar(objHandle, (int)index, &charValue))
    {
        JITDUMP("Folding \"cns_str\"[%d] into %u", (int)index, (unsigned)charValue);

        tree->gtVNPair.SetBoth(vnStore->VNForIntCon(charValue));
        return true;
    }
    return false;
}

void Compiler::fgValueNumberTree(GenTree* tree)
{
    genTreeOps oper = tree->OperGet();
    var_types  typ  = tree->TypeGet();

    if (GenTree::OperIsConst(oper))
    {
        // If this is a struct assignment, with a constant rhs, (i,.e. an initBlk),
        // it is not useful to value number the constant.
        if (tree->TypeGet() != TYP_STRUCT)
        {
            fgValueNumberTreeConst(tree);
        }
    }
    else if (GenTree::OperIsLeaf(oper))
    {
        switch (oper)
        {
            case GT_LCL_ADDR:
            {
                unsigned lclNum  = tree->AsLclFld()->GetLclNum();
                unsigned lclOffs = tree->AsLclFld()->GetLclOffs();
                tree->gtVNPair.SetBoth(vnStore->VNForFunc(TYP_BYREF, VNF_PtrToLoc, vnStore->VNForIntCon(lclNum),
                                                          vnStore->VNForIntPtrCon(lclOffs)));
                assert(lvaGetDesc(lclNum)->IsAddressExposed() || lvaGetDesc(lclNum)->IsHiddenBufferStructArg());
            }
            break;

            case GT_LCL_VAR:
            {
                GenTreeLclVarCommon* lcl    = tree->AsLclVarCommon();
                unsigned             lclNum = lcl->GetLclNum();
                LclVarDsc*           varDsc = lvaGetDesc(lclNum);

                if (lcl->HasSsaName())
                {
                    fgValueNumberSsaVarDef(lcl);
                }
                else if (varDsc->IsAddressExposed())
                {
                    // Address-exposed locals are part of ByrefExposed.
                    ValueNum addrVN = vnStore->VNForFunc(TYP_BYREF, VNF_PtrToLoc, vnStore->VNForIntCon(lclNum),
                                                         vnStore->VNForIntPtrCon(lcl->GetLclOffs()));
                    ValueNum loadVN = fgValueNumberByrefExposedLoad(lcl->TypeGet(), addrVN);

                    lcl->gtVNPair.SetLiberal(loadVN);
                    lcl->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, lcl->TypeGet()));
                }
                else
                {
                    // An untracked local, and other odd cases.
                    lcl->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lcl->TypeGet()));
                }
            }
            break;

            case GT_LCL_FLD:
            {
                GenTreeLclFld* lclFld = tree->AsLclFld();
                unsigned       lclNum = lclFld->GetLclNum();
                LclVarDsc*     varDsc = lvaGetDesc(lclNum);

                if (lclFld->HasSsaName())
                {
                    ValueNumPair lclVarValue = varDsc->GetPerSsaData(lclFld->GetSsaNum())->m_vnPair;
                    lclFld->gtVNPair = vnStore->VNPairForLoad(lclVarValue, lvaLclExactSize(lclNum), lclFld->TypeGet(),
                                                              lclFld->GetLclOffs(), lclFld->GetSize());
                }
                else if (varDsc->IsAddressExposed())
                {
                    // Address-exposed locals are part of ByrefExposed.
                    ValueNum addrVN = vnStore->VNForFunc(TYP_BYREF, VNF_PtrToLoc, vnStore->VNForIntCon(lclNum),
                                                         vnStore->VNForIntPtrCon(lclFld->GetLclOffs()));
                    ValueNum loadVN = fgValueNumberByrefExposedLoad(lclFld->TypeGet(), addrVN);

                    lclFld->gtVNPair.SetLiberal(loadVN);
                    lclFld->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, lclFld->TypeGet()));
                }
                else
                {
                    // An untracked local, and other odd cases.
                    lclFld->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lclFld->TypeGet()));
                }
            }
            break;

            case GT_CATCH_ARG:
                // We know nothing about the value of a caught expression.
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                break;

            case GT_MEMORYBARRIER: // Leaf
                // For MEMORYBARRIER add an arbitrary side effect on GcHeap/ByrefExposed.
                fgMutateGcHeap(tree DEBUGARG("MEMORYBARRIER"));
                tree->gtVNPair = vnStore->VNPForVoid();
                break;

            // These do not represent values.
            case GT_NO_OP:
            case GT_NOP:
            case GT_JMP:   // Control flow
            case GT_LABEL: // Control flow
#if !defined(FEATURE_EH_FUNCLETS)
            case GT_END_LFIN: // Control flow
#endif
                tree->gtVNPair = vnStore->VNPForVoid();
                break;

            case GT_PHI_ARG:
                // This one is special because we should never process it in this method: it should
                // always be taken care of, when needed, during pre-processing of a blocks phi definitions.
                assert(!"PHI_ARG in fgValueNumberTree");
                break;

            default:
                unreached();
        }
    }
    else if (GenTree::OperIsSimple(oper))
    {
        if ((oper == GT_IND) || (oper == GT_BLK))
        {
            // So far, we handle cases in which the address is a ptr-to-local, or if it's
            // a pointer to an object field or array element.  Other cases become uses of
            // the current ByrefExposed value and the pointer value, so that at least we
            // can recognize redundant loads with no stores between them.
            GenTree*  addr       = tree->AsIndir()->Addr();
            FieldSeq* fldSeq     = nullptr;
            GenTree*  baseAddr   = nullptr;
            bool      isVolatile = (tree->gtFlags & GTF_IND_VOLATILE) != 0;

            // See if the addr has any exceptional part.
            ValueNumPair addrNvnp;
            ValueNumPair addrXvnp;
            vnStore->VNPUnpackExc(addr->gtVNPair, &addrNvnp, &addrXvnp);

            if (tree->gtFlags & GTF_IND_INVARIANT)
            {
                assert(!isVolatile); // We don't expect both volatile and invariant

                bool returnsTypeHandle = false;
                if ((oper == GT_IND) && addr->TypeIs(TYP_REF) && tree->TypeIs(TYP_I_IMPL))
                {
                    // We try to access GC object's type, let's see if we know the exact type already
                    // First, we're trying to do that via gtGetClassHandle.
                    //
                    bool                 isExact   = false;
                    bool                 isNonNull = false;
                    CORINFO_CLASS_HANDLE handle    = gtGetClassHandle(addr, &isExact, &isNonNull);
                    if (isExact && (handle != NO_CLASS_HANDLE))
                    {
                        JITDUMP("IND(obj) is actually a class handle for %s\n", eeGetClassName(handle));
                        // Filter out all shared generic instantiations
                        if ((info.compCompHnd->getClassAttribs(handle) & CORINFO_FLG_SHAREDINST) == 0)
                        {
                            void* pEmbedClsHnd;
                            void* embedClsHnd = (void*)info.compCompHnd->embedClassHandle(handle, &pEmbedClsHnd);
                            if (pEmbedClsHnd == nullptr)
                            {
                                // Skip indirect handles for now since this path is mostly for PGO scenarios
                                assert(embedClsHnd != nullptr);
                                ValueNum handleVN = vnStore->VNForHandle((ssize_t)embedClsHnd, GTF_ICON_CLASS_HDL);
                                tree->gtVNPair    = vnStore->VNPWithExc(ValueNumPair(handleVN, handleVN), addrXvnp);
                                returnsTypeHandle = true;
                            }
                        }
                    }
                    else
                    {
                        // Then, let's see if we can find JitNew at least
                        VNFuncApp  funcApp;
                        const bool addrIsVNFunc = vnStore->GetVNFunc(addrNvnp.GetLiberal(), &funcApp);
                        if (addrIsVNFunc && (funcApp.m_func == VNF_JitNew) && addrNvnp.BothEqual())
                        {
                            tree->gtVNPair =
                                vnStore->VNPWithExc(ValueNumPair(funcApp.m_args[0], funcApp.m_args[0]), addrXvnp);
                            returnsTypeHandle = true;
                        }
                    }
                }

                if (!returnsTypeHandle)
                {
                    // TYP_REF check to improve TP since we mostly target invariant loads of
                    // frozen objects here
                    if (addr->TypeIs(TYP_REF) && fgValueNumberConstLoad(tree->AsIndir()))
                    {
                        // VN is assigned inside fgValueNumberConstLoad
                    }
                    else if (addr->IsIconHandle(GTF_ICON_STATIC_BOX_PTR))
                    {
                        // Indirections off of addresses for boxed statics represent bases for
                        // the address of the static itself. Here we will use "nullptr" for the
                        // field sequence and assume the actual static field will be appended to
                        // it later, as part of numbering the method table pointer offset addition.
                        assert(addrNvnp.BothEqual() && (addrXvnp == vnStore->VNPForEmptyExcSet()));
                        ValueNum boxAddrVN  = addrNvnp.GetLiberal();
                        ValueNum fieldSeqVN = vnStore->VNForFieldSeq(nullptr);
                        ValueNum offsetVN   = vnStore->VNForIntPtrCon(-TARGET_POINTER_SIZE);
                        ValueNum staticAddrVN =
                            vnStore->VNForFunc(tree->TypeGet(), VNF_PtrToStatic, boxAddrVN, fieldSeqVN, offsetVN);
                        tree->gtVNPair = ValueNumPair(staticAddrVN, staticAddrVN);
                    }
                    else // TODO-VNTypes: this code needs to encode the types of the indirections.
                    {
                        // Is this invariant indirect expected to always return a non-null value?
                        VNFunc loadFunc =
                            ((tree->gtFlags & GTF_IND_NONNULL) != 0) ? VNF_InvariantNonNullLoad : VNF_InvariantLoad;

                        // Special case: for initialized non-null 'static readonly' fields we want to keep field
                        // sequence to be able to fold their value
                        if ((loadFunc == VNF_InvariantNonNullLoad) && addr->IsIconHandle(GTF_ICON_CONST_PTR) &&
                            (addr->AsIntCon()->gtFieldSeq != nullptr) &&
                            (addr->AsIntCon()->gtFieldSeq->GetOffset() == addr->AsIntCon()->IconValue()))
                        {
                            addrNvnp.SetBoth(vnStore->VNForFieldSeq(addr->AsIntCon()->gtFieldSeq));
                        }

                        tree->gtVNPair = vnStore->VNPairForFunc(tree->TypeGet(), loadFunc, addrNvnp);
                        tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
                    }
                }
            }
            else if (isVolatile)
            {
                // We just mutate GcHeap/ByrefExposed if isVolatile is true, and then do the read as normal.
                //
                // This allows:
                //   1: read s;
                //   2: volatile read s;
                //   3: read s;
                //
                // We should never assume that the values read by 1 and 2 are the same (because the heap was mutated
                // in between them)... but we *should* be able to prove that the values read in 2 and 3 are the
                // same.
                //
                fgMutateGcHeap(tree DEBUGARG("GTF_IND_VOLATILE - read"));

                // The value read by the GT_IND can immediately change
                ValueNum newUniq = vnStore->VNForExpr(compCurBB, tree->TypeGet());
                tree->gtVNPair   = vnStore->VNPWithExc(ValueNumPair(newUniq, newUniq), addrXvnp);
            }
            else
            {
                var_types loadType = tree->TypeGet();
                ssize_t   offset   = 0;
                unsigned  loadSize = tree->AsIndir()->Size();
                VNFuncApp funcApp{VNF_COUNT};

                // TODO-1stClassStructs: delete layout-less "IND(struct)" nodes and the "loadSize == 0" condition.
                if (loadSize == 0)
                {
                    tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, loadType));
                }
                else if (fgValueNumberConstLoad(tree->AsIndir()))
                {
                    // VN is assigned inside fgValueNumberConstLoad
                }
                else if (vnStore->GetVNFunc(addrNvnp.GetLiberal(), &funcApp) && (funcApp.m_func == VNF_PtrToStatic))
                {
                    fldSeq = vnStore->FieldSeqVNToFieldSeq(funcApp.m_args[1]);
                    offset = vnStore->ConstantValue<ssize_t>(funcApp.m_args[2]);

                    // Note VNF_PtrToStatic statics are currently always "simple".
                    fgValueNumberFieldLoad(tree, /* baseAddr */ nullptr, fldSeq, offset);
                }
                else if (vnStore->GetVNFunc(addrNvnp.GetLiberal(), &funcApp) && (funcApp.m_func == VNF_PtrToArrElem))
                {
                    fgValueNumberArrayElemLoad(tree, &funcApp);
                }
                else if (addr->IsFieldAddr(this, &baseAddr, &fldSeq, &offset))
                {
                    assert(fldSeq != nullptr);
                    fgValueNumberFieldLoad(tree, baseAddr, fldSeq, offset);
                }
                else // We don't know where the address points, so it is an ByrefExposed load.
                {
                    ValueNum addrVN = addr->gtVNPair.GetLiberal();
                    ValueNum loadVN = fgValueNumberByrefExposedLoad(typ, addrVN);
                    tree->gtVNPair.SetLiberal(loadVN);
                    tree->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                }

                tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
            }
        }
        else if (tree->OperGet() == GT_CAST)
        {
            fgValueNumberCastTree(tree);
        }
        else if (tree->OperGet() == GT_INTRINSIC)
        {
            fgValueNumberIntrinsic(tree);
        }
        else // Look up the VNFunc for the node
        {
            VNFunc vnf = GetVNFuncForNode(tree);

            if (ValueNumStore::VNFuncIsLegal(vnf))
            {
                if (GenTree::OperIsUnary(oper))
                {
                    assert(tree->gtGetOp1() != nullptr);
                    ValueNumPair op1VNP;
                    ValueNumPair op1VNPx;
                    vnStore->VNPUnpackExc(tree->AsOp()->gtOp1->gtVNPair, &op1VNP, &op1VNPx);

                    // If we are fetching the array length for an array ref that came from global memory
                    // then for CSE safety we must use the conservative value number for both
                    //
                    if (tree->OperIsArrLength() && ((tree->AsOp()->gtOp1->gtFlags & GTF_GLOB_REF) != 0))
                    {
                        // use the conservative value number for both when computing the VN for the ARR_LENGTH
                        op1VNP.SetBoth(op1VNP.GetConservative());
                    }

                    tree->gtVNPair = vnStore->VNPWithExc(vnStore->VNPairForFunc(tree->TypeGet(), vnf, op1VNP), op1VNPx);
                }
                else // we have a binary oper
                {
                    assert(GenTree::OperIsBinary(oper));

                    // Handle a few special cases: if we add a field offset constant to a PtrToXXX, we will get back a
                    // new
                    // PtrToXXX.

                    ValueNumPair op1vnp;
                    ValueNumPair op1Xvnp;
                    vnStore->VNPUnpackExc(tree->AsOp()->gtOp1->gtVNPair, &op1vnp, &op1Xvnp);

                    ValueNumPair op2vnp;
                    ValueNumPair op2Xvnp;
                    vnStore->VNPUnpackExc(tree->AsOp()->gtOp2->gtVNPair, &op2vnp, &op2Xvnp);
                    ValueNumPair excSetPair = vnStore->VNPExcSetUnion(op1Xvnp, op2Xvnp);

                    ValueNum newVN = ValueNumStore::NoVN;

                    // Check for the addition of a field offset constant
                    //
                    if ((oper == GT_ADD) && !tree->gtOverflowEx())
                    {
                        newVN = vnStore->ExtendPtrVN(tree->AsOp()->gtOp1, tree->AsOp()->gtOp2);
                    }

                    if (newVN != ValueNumStore::NoVN)
                    {
                        // We don't care about differences between liberal and conservative for pointer values.
                        tree->gtVNPair = vnStore->VNPWithExc(ValueNumPair(newVN, newVN), excSetPair);
                    }
                    else
                    {
                        VNFunc       vnf        = GetVNFuncForNode(tree);
                        ValueNumPair normalPair = vnStore->VNPairForFunc(tree->TypeGet(), vnf, op1vnp, op2vnp);
                        tree->gtVNPair          = vnStore->VNPWithExc(normalPair, excSetPair);
                        // For overflow checking operations the VNF_OverflowExc will be added below
                        // by fgValueNumberAddExceptionSet
                    }
                }
            }
            else // ValueNumStore::VNFuncIsLegal returns false
            {
                // Some of the genTreeOps that aren't legal VNFuncs so they get special handling.
                switch (oper)
                {
                    case GT_STORE_LCL_VAR:
                    case GT_STORE_LCL_FLD:
                    case GT_STOREIND:
                    case GT_STORE_BLK:
                        fgValueNumberStore(tree);
                        break;

                    case GT_COMMA:
                    {
                        ValueNumPair op1Xvnp = vnStore->VNPExceptionSet(tree->AsOp()->gtOp1->gtVNPair);
                        tree->gtVNPair       = vnStore->VNPWithExc(tree->AsOp()->gtOp2->gtVNPair, op1Xvnp);
                    }
                    break;

                    case GT_ARR_ADDR:
                        fgValueNumberArrIndexAddr(tree->AsArrAddr());
                        break;

                    case GT_MDARR_LENGTH:
                    case GT_MDARR_LOWER_BOUND:
                    {
                        VNFunc   mdarrVnf = (oper == GT_MDARR_LENGTH) ? VNF_MDArrLength : VNF_MDArrLowerBound;
                        GenTree* arrRef   = tree->AsMDArr()->ArrRef();
                        unsigned dim      = tree->AsMDArr()->Dim();

                        ValueNumPair arrVNP;
                        ValueNumPair arrVNPx;
                        vnStore->VNPUnpackExc(arrRef->gtVNPair, &arrVNP, &arrVNPx);

                        // If we are fetching the array length for an array ref that came from global memory
                        // then for CSE safety we must use the conservative value number for both.
                        //
                        if ((arrRef->gtFlags & GTF_GLOB_REF) != 0)
                        {
                            arrVNP.SetBoth(arrVNP.GetConservative());
                        }

                        ValueNumPair intPair;
                        intPair.SetBoth(vnStore->VNForIntCon(dim));

                        ValueNumPair normalPair = vnStore->VNPairForFunc(tree->TypeGet(), mdarrVnf, arrVNP, intPair);

                        tree->gtVNPair = vnStore->VNPWithExc(normalPair, arrVNPx);
                        break;
                    }

                    case GT_BOUNDS_CHECK:
                    {
                        ValueNumPair vnpIndex  = tree->AsBoundsChk()->GetIndex()->gtVNPair;
                        ValueNumPair vnpArrLen = tree->AsBoundsChk()->GetArrayLength()->gtVNPair;

                        ValueNumPair vnpExcSet = ValueNumStore::VNPForEmptyExcSet();

                        // And collect the exceptions  from Index and ArrLen
                        vnpExcSet = vnStore->VNPUnionExcSet(vnpIndex, vnpExcSet);
                        vnpExcSet = vnStore->VNPUnionExcSet(vnpArrLen, vnpExcSet);

                        // A bounds check node has no value, but may throw exceptions.
                        tree->gtVNPair = vnStore->VNPWithExc(vnStore->VNPForVoid(), vnpExcSet);

                        // next add the bounds check exception set for the current tree node
                        fgValueNumberAddExceptionSet(tree);

                        // Record non-constant value numbers that are used as the length argument to bounds checks, so
                        // that assertion prop will know that comparisons against them are worth analyzing.
                        ValueNum lengthVN = tree->AsBoundsChk()->GetArrayLength()->gtVNPair.GetConservative();
                        if ((lengthVN != ValueNumStore::NoVN) && !vnStore->IsVNConstant(lengthVN))
                        {
                            vnStore->SetVNIsCheckedBound(lengthVN);
                        }
                    }
                    break;

                    case GT_XORR: // Binop
                    case GT_XAND: // Binop
                    case GT_XADD: // Binop
                    case GT_XCHG: // Binop
                    {
                        // For XADD and XCHG other intrinsics add an arbitrary side effect on GcHeap/ByrefExposed.
                        fgMutateGcHeap(tree DEBUGARG("Interlocked intrinsic"));

                        ValueNumPair vnpExcSet = ValueNumStore::VNPForEmptyExcSet();
                        vnpExcSet              = vnStore->VNPUnionExcSet(tree->AsIndir()->Addr()->gtVNPair, vnpExcSet);
                        vnpExcSet              = vnStore->VNPUnionExcSet(tree->AsIndir()->Data()->gtVNPair, vnpExcSet);

                        // The normal value is a new unique VN. The null reference exception will be added below.
                        tree->gtVNPair = vnStore->VNPUniqueWithExc(tree->TypeGet(), vnpExcSet);
                        break;
                    }

                    // These unary nodes do not produce values. Note that for NULLCHECK the
                    // additional exception will be added below by "fgValueNumberAddExceptionSet".
                    case GT_JTRUE:
                    case GT_SWITCH:
                    case GT_RETURN:
                    case GT_RETFILT:
                    case GT_NULLCHECK:
                        if (tree->gtGetOp1() != nullptr)
                        {
                            tree->gtVNPair = vnStore->VNPWithExc(vnStore->VNPForVoid(),
                                                                 vnStore->VNPExceptionSet(tree->gtGetOp1()->gtVNPair));
                        }
                        else
                        {
                            tree->gtVNPair = vnStore->VNPForVoid();
                        }
                        break;

                    // BOX and CKFINITE are passthrough nodes (like NOP). We'll add the exception for the latter later.
                    case GT_BOX:
                    case GT_CKFINITE:
                        tree->gtVNPair = tree->gtGetOp1()->gtVNPair;
                        break;

                    // These unary nodes will receive a unique VN.
                    // TODO-CQ: model INIT_VAL properly.
                    case GT_LCLHEAP:
                    case GT_INIT_VAL:
                        tree->gtVNPair =
                            vnStore->VNPUniqueWithExc(tree->TypeGet(),
                                                      vnStore->VNPExceptionSet(tree->gtGetOp1()->gtVNPair));
                        break;

                    case GT_BITCAST:
                    {
                        fgValueNumberBitCast(tree);
                        break;
                    }

                    default:
                        assert(!"Unhandled node in fgValueNumberTree");
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                        break;
                }
            }
        }

        // next we add any exception sets for the current tree node
        fgValueNumberAddExceptionSet(tree);
    }
    else
    {
        assert(GenTree::OperIsSpecial(oper));

        // TBD: We must handle these individually.  For now:
        switch (oper)
        {
            case GT_CALL:
                fgValueNumberCall(tree->AsCall());
                break;

#ifdef FEATURE_HW_INTRINSICS
            case GT_HWINTRINSIC:
                fgValueNumberHWIntrinsic(tree->AsHWIntrinsic());
                break;
#endif // FEATURE_HW_INTRINSICS

            case GT_STORE_DYN_BLK:
            {
                // Conservatively, mutate the heaps - we don't analyze these rare stores.
                // Likewise, any locals possibly defined by them we mark as address-exposed.
                fgMutateGcHeap(tree DEBUGARG("dynamic block store"));

                GenTreeStoreDynBlk* store     = tree->AsStoreDynBlk();
                ValueNumPair        vnpExcSet = ValueNumStore::VNPForEmptyExcSet();

                // Propagate the exceptions...
                vnpExcSet = vnStore->VNPUnionExcSet(store->Addr()->gtVNPair, vnpExcSet);
                vnpExcSet = vnStore->VNPUnionExcSet(store->Data()->gtVNPair, vnpExcSet);
                vnpExcSet = vnStore->VNPUnionExcSet(store->gtDynamicSize->gtVNPair, vnpExcSet);

                // This is a store, it produces no value. Thus we use VNPForVoid().
                store->gtVNPair = vnStore->VNPWithExc(vnStore->VNPForVoid(), vnpExcSet);

                // Note that we are only adding the exception for the destination address.
                // Currently, "Data()" is an explicit indirection in case this is a "cpblk".
                assert(store->Data()->gtEffectiveVal()->OperIsIndir() || store->OperIsInitBlkOp());
                fgValueNumberAddExceptionSetForIndirection(store, store->Addr());
                break;
            }

            case GT_CMPXCHG: // Specialop
            {
                // For CMPXCHG and other intrinsics add an arbitrary side effect on GcHeap/ByrefExposed.
                fgMutateGcHeap(tree DEBUGARG("Interlocked intrinsic"));

                GenTreeCmpXchg* const cmpXchg = tree->AsCmpXchg();

                assert(tree->OperIsImplicitIndir()); // special node with an implicit indirections

                GenTree* location  = cmpXchg->Addr();
                GenTree* value     = cmpXchg->Data();
                GenTree* comparand = cmpXchg->Comparand();

                ValueNumPair vnpExcSet = ValueNumStore::VNPForEmptyExcSet();

                // Collect the exception sets from our operands
                vnpExcSet = vnStore->VNPUnionExcSet(location->gtVNPair, vnpExcSet);
                vnpExcSet = vnStore->VNPUnionExcSet(value->gtVNPair, vnpExcSet);
                vnpExcSet = vnStore->VNPUnionExcSet(comparand->gtVNPair, vnpExcSet);

                // The normal value is a new unique VN.
                tree->gtVNPair = vnStore->VNPUniqueWithExc(tree->TypeGet(), vnpExcSet);

                // add the null check exception for 'location' to the tree's value number
                fgValueNumberAddExceptionSetForIndirection(tree, location);
                break;
            }

            case GT_ARR_ELEM:
                unreached(); // we expect these to be gone by the time value numbering runs
                break;

            // FIELD_LIST is an R-value that we currently don't model.
            case GT_FIELD_LIST:
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                for (GenTreeFieldList::Use& use : tree->AsFieldList()->Uses())
                {
                    tree->gtVNPair =
                        vnStore->VNPWithExc(tree->gtVNPair, vnStore->VNPExceptionSet(use.GetNode()->gtVNPair));
                }
                break;

            case GT_SELECT:
            {
                GenTreeConditional* const conditional = tree->AsConditional();

                ValueNumPair condvnp;
                ValueNumPair condXvnp;
                vnStore->VNPUnpackExc(conditional->gtCond->gtVNPair, &condvnp, &condXvnp);

                ValueNumPair op1vnp;
                ValueNumPair op1Xvnp;
                vnStore->VNPUnpackExc(conditional->gtOp1->gtVNPair, &op1vnp, &op1Xvnp);

                ValueNumPair op2vnp;
                ValueNumPair op2Xvnp;
                vnStore->VNPUnpackExc(conditional->gtOp2->gtVNPair, &op2vnp, &op2Xvnp);

                // Collect the exception sets.
                ValueNumPair vnpExcSet = vnStore->VNPExcSetUnion(condXvnp, op1Xvnp);
                vnpExcSet              = vnStore->VNPExcSetUnion(vnpExcSet, op2Xvnp);

                // Get the normal value using the VN func.
                VNFunc vnf = GetVNFuncForNode(tree);
                assert(ValueNumStore::VNFuncIsLegal(vnf));
                ValueNumPair normalPair = vnStore->VNPairForFunc(tree->TypeGet(), vnf, condvnp, op1vnp, op2vnp);

                // Attach the combined exception set
                tree->gtVNPair = vnStore->VNPWithExc(normalPair, vnpExcSet);

                break;
            }

            default:
                assert(!"Unhandled special node in fgValueNumberTree");
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                break;
        }
    }
#ifdef DEBUG
    if (verbose)
    {
        if (tree->gtVNPair.GetLiberal() != ValueNumStore::NoVN)
        {
            printf("N%03u ", tree->gtSeqNum);
            printTreeID(tree);
            printf(" ");
            gtDispNodeName(tree);
            if (tree->OperIsLocalStore())
            {
                gtDispLocal(tree->AsLclVarCommon(), nullptr);
            }
            else if (tree->OperIsLeaf())
            {
                gtDispLeaf(tree, nullptr);
            }
            printf(" => ");
            vnpPrint(tree->gtVNPair, 1);
            printf("\n");
        }
    }
#endif // DEBUG
}

void Compiler::fgValueNumberIntrinsic(GenTree* tree)
{
    assert(tree->OperGet() == GT_INTRINSIC);
    GenTreeIntrinsic* intrinsic = tree->AsIntrinsic();
    ValueNumPair      arg0VNP, arg1VNP;
    ValueNumPair      arg0VNPx = ValueNumStore::VNPForEmptyExcSet();
    ValueNumPair      arg1VNPx = ValueNumStore::VNPForEmptyExcSet();

    vnStore->VNPUnpackExc(intrinsic->AsOp()->gtOp1->gtVNPair, &arg0VNP, &arg0VNPx);

    if (intrinsic->AsOp()->gtOp2 != nullptr)
    {
        vnStore->VNPUnpackExc(intrinsic->AsOp()->gtOp2->gtVNPair, &arg1VNP, &arg1VNPx);
    }

    if (IsMathIntrinsic(intrinsic->gtIntrinsicName))
    {
        // GT_INTRINSIC is a currently a subtype of binary operators. But most of
        // the math intrinsics are actually unary operations.

        if (intrinsic->AsOp()->gtOp2 == nullptr)
        {
            intrinsic->gtVNPair =
                vnStore->VNPWithExc(vnStore->EvalMathFuncUnary(tree->TypeGet(), intrinsic->gtIntrinsicName, arg0VNP),
                                    arg0VNPx);
        }
        else
        {
            ValueNumPair newVNP =
                vnStore->EvalMathFuncBinary(tree->TypeGet(), intrinsic->gtIntrinsicName, arg0VNP, arg1VNP);
            ValueNumPair excSet = vnStore->VNPExcSetUnion(arg0VNPx, arg1VNPx);
            intrinsic->gtVNPair = vnStore->VNPWithExc(newVNP, excSet);
        }
    }
    else
    {
        assert(intrinsic->gtIntrinsicName == NI_System_Object_GetType);

        // Try to fold obj.GetType() if we know the exact type of obj.
        bool                 isExact   = false;
        bool                 isNonNull = false;
        CORINFO_CLASS_HANDLE cls       = gtGetClassHandle(tree->gtGetOp1(), &isExact, &isNonNull);
        if ((cls != NO_CLASS_HANDLE) && isExact && isNonNull)
        {
            CORINFO_OBJECT_HANDLE typeObj = info.compCompHnd->getRuntimeTypePointer(cls);
            if (typeObj != nullptr)
            {
                setMethodHasFrozenObjects();
                ValueNum handleVN   = vnStore->VNForHandle((ssize_t)typeObj, GTF_ICON_OBJ_HDL);
                intrinsic->gtVNPair = vnStore->VNPWithExc(ValueNumPair(handleVN, handleVN), arg0VNPx);
                return;
            }
        }

        intrinsic->gtVNPair =
            vnStore->VNPWithExc(vnStore->VNPairForFunc(intrinsic->TypeGet(), VNF_ObjGetType, arg0VNP), arg0VNPx);
    }
}

//------------------------------------------------------------------------
// fgValueNumberArrIndexAddr: Numbers the ARR_ADDR tree using VNF_PtrToArrElem.
//
// Arguments:
//    arrAddr - The GT_ARR_ADDR tree to number
//
void Compiler::fgValueNumberArrIndexAddr(GenTreeArrAddr* arrAddr)
{
    GenTree* arr   = nullptr;
    ValueNum inxVN = ValueNumStore::NoVN;
    arrAddr->ParseArrayAddress(this, &arr, &inxVN);

    if (arr == nullptr)
    {
        JITDUMP("    *** ARR_ADDR -- an unparsable array expression, assigning a new, unique VN\n");
        arrAddr->gtVNPair = vnStore->VNPUniqueWithExc(TYP_BYREF, vnStore->VNPExceptionSet(arrAddr->Addr()->gtVNPair));
        return;
    }

    // Get the element type equivalence class representative.
    var_types            elemType       = arrAddr->GetElemType();
    CORINFO_CLASS_HANDLE elemStructType = arrAddr->GetElemClassHandle();
    CORINFO_CLASS_HANDLE elemTypeEq     = EncodeElemType(elemType, elemStructType);
    ValueNum             elemTypeEqVN   = vnStore->VNForHandle(ssize_t(elemTypeEq), GTF_ICON_CLASS_HDL);
    JITDUMP("    VNForHandle(arrElemType: %s) is " FMT_VN "\n",
            (elemType == TYP_STRUCT) ? eeGetClassName(elemStructType) : varTypeName(elemType), elemTypeEqVN);

    ValueNum arrVN    = vnStore->VNNormalValue(arr->GetVN(VNK_Liberal));
    inxVN             = vnStore->VNNormalValue(inxVN);
    ValueNum offsetVN = vnStore->VNForIntPtrCon(0);

    ValueNum     arrAddrVN  = vnStore->VNForFunc(TYP_BYREF, VNF_PtrToArrElem, elemTypeEqVN, arrVN, inxVN, offsetVN);
    ValueNumPair arrAddrVNP = ValueNumPair(arrAddrVN, arrAddrVN);
    arrAddr->gtVNPair       = vnStore->VNPWithExc(arrAddrVNP, vnStore->VNPExceptionSet(arrAddr->Addr()->gtVNPair));
}

#ifdef FEATURE_HW_INTRINSICS
void Compiler::fgValueNumberHWIntrinsic(GenTreeHWIntrinsic* tree)
{
    NamedIntrinsic intrinsicId   = tree->GetHWIntrinsicId();
    GenTree*       addr          = nullptr;
    const bool     isMemoryLoad  = tree->OperIsMemoryLoad(&addr);
    const bool     isMemoryStore = !isMemoryLoad && tree->OperIsMemoryStore(&addr);

    // We do not model HWI stores precisely.
    if (isMemoryStore)
    {
        fgMutateGcHeap(tree DEBUGARG("HWIntrinsic - MemoryStore"));
    }
#if defined(TARGET_XARCH)
    else if (HWIntrinsicInfo::HasSpecialSideEffect_Barrier(intrinsicId))
    {
        // This is modeled the same as GT_MEMORYBARRIER
        fgMutateGcHeap(tree DEBUGARG("HWIntrinsic - Barrier"));
    }
#endif // TARGET_XARCH

    ValueNumPair excSetPair = ValueNumStore::VNPForEmptyExcSet();
    ValueNumPair normalPair = ValueNumPair();

    const size_t opCount = tree->GetOperandCount();

    if ((opCount > 3) || (JitConfig.JitDisableSimdVN() & 2) == 2)
    {
        // TODO-CQ: allow intrinsics with > 3 operands to be properly VN'ed.
        normalPair = vnStore->VNPairForExpr(compCurBB, tree->TypeGet());

        for (GenTree* operand : tree->Operands())
        {
            excSetPair = vnStore->VNPUnionExcSet(operand->gtVNPair, excSetPair);
        }
    }
    else
    {
        VNFunc       func             = GetVNFuncForNode(tree);
        ValueNumPair resultTypeVNPair = ValueNumPair();
        bool         encodeResultType = vnEncodesResultTypeForHWIntrinsic(intrinsicId);

        if (encodeResultType)
        {
            ValueNum simdTypeVN = vnStore->VNForSimdType(tree->GetSimdSize(), tree->GetNormalizedSimdBaseJitType());
            resultTypeVNPair.SetBoth(simdTypeVN);

            JITDUMP("    simdTypeVN is ");
            JITDUMPEXEC(vnPrint(simdTypeVN, 1));
            JITDUMP("\n");
        }

        auto getOperandVNs = [this, addr](GenTree* operand, ValueNumPair* pNormVNPair, ValueNumPair* pExcVNPair) {
            vnStore->VNPUnpackExc(operand->gtVNPair, pNormVNPair, pExcVNPair);

            // If we have a load operation we will use the fgValueNumberByrefExposedLoad
            // method to assign a value number that depends upon the current heap state.
            //
            if (operand == addr)
            {
                // We need to "insert" the "ByrefExposedLoad" VN somewhere here. We choose
                // to do so by effectively altering the semantics of "addr" operands, making
                // them represent "the load", on top of which the HWI func itself is applied.
                // This is a workaround, but doing this "properly" would entail adding the
                // heap and type VNs to HWI load funcs themselves.
                var_types loadType = operand->TypeGet();
                ValueNum  loadVN   = fgValueNumberByrefExposedLoad(loadType, pNormVNPair->GetLiberal());

                pNormVNPair->SetLiberal(loadVN);
                pNormVNPair->SetConservative(vnStore->VNForExpr(compCurBB, loadType));
            }
        };

        const bool isVariableNumArgs = HWIntrinsicInfo::lookupNumArgs(intrinsicId) == -1;

        // There are some HWINTRINSICS operations that have zero args, i.e.  NI_Vector128_Zero
        if (opCount == 0)
        {
            // Currently we don't have intrinsics with variable number of args with a parameter-less option.
            assert(!isVariableNumArgs);

            if (encodeResultType)
            {
                // There are zero arg HWINTRINSICS operations that encode the result type, i.e.  Vector128_AllBitSet
                normalPair = vnStore->VNPairForFunc(tree->TypeGet(), func, resultTypeVNPair);
                assert(vnStore->VNFuncArity(func) == 1);
            }
            else
            {
                normalPair = vnStore->VNPairForFunc(tree->TypeGet(), func);
                assert(vnStore->VNFuncArity(func) == 0);
            }
        }
        else // HWINTRINSIC unary or binary or ternary operator.
        {
            ValueNumPair op1vnp;
            ValueNumPair op1Xvnp;
            getOperandVNs(tree->Op(1), &op1vnp, &op1Xvnp);

            if (opCount == 1)
            {
                ValueNum normalLVN = vnStore->EvalHWIntrinsicFunUnary(tree->TypeGet(), tree->GetSimdBaseType(),
                                                                      intrinsicId, func, op1vnp.GetLiberal(),
                                                                      encodeResultType, resultTypeVNPair.GetLiberal());
                ValueNum normalCVN =
                    vnStore->EvalHWIntrinsicFunUnary(tree->TypeGet(), tree->GetSimdBaseType(), intrinsicId, func,
                                                     op1vnp.GetConservative(), encodeResultType,
                                                     resultTypeVNPair.GetConservative());

                normalPair = ValueNumPair(normalLVN, normalCVN);
                excSetPair = op1Xvnp;
            }
            else
            {
                ValueNumPair op2vnp;
                ValueNumPair op2Xvnp;
                getOperandVNs(tree->Op(2), &op2vnp, &op2Xvnp);

                if (opCount == 2)
                {
                    ValueNum normalLVN =
                        vnStore->EvalHWIntrinsicFunBinary(tree->TypeGet(), tree->GetSimdBaseType(), intrinsicId, func,
                                                          op1vnp.GetLiberal(), op2vnp.GetLiberal(), encodeResultType,
                                                          resultTypeVNPair.GetLiberal());
                    ValueNum normalCVN =
                        vnStore->EvalHWIntrinsicFunBinary(tree->TypeGet(), tree->GetSimdBaseType(), intrinsicId, func,
                                                          op1vnp.GetConservative(), op2vnp.GetConservative(),
                                                          encodeResultType, resultTypeVNPair.GetConservative());

                    normalPair = ValueNumPair(normalLVN, normalCVN);
                    excSetPair = vnStore->VNPExcSetUnion(op1Xvnp, op2Xvnp);
                }
                else
                {
                    assert(opCount == 3);

                    ValueNumPair op3vnp;
                    ValueNumPair op3Xvnp;
                    getOperandVNs(tree->Op(3), &op3vnp, &op3Xvnp);

                    ValueNum normalLVN =
                        vnStore->EvalHWIntrinsicFunTernary(tree->TypeGet(), tree->GetSimdBaseType(), intrinsicId, func,
                                                           op1vnp.GetLiberal(), op2vnp.GetLiberal(),
                                                           op3vnp.GetLiberal(), encodeResultType,
                                                           resultTypeVNPair.GetLiberal());
                    ValueNum normalCVN =
                        vnStore->EvalHWIntrinsicFunTernary(tree->TypeGet(), tree->GetSimdBaseType(), intrinsicId, func,
                                                           op1vnp.GetConservative(), op2vnp.GetConservative(),
                                                           op3vnp.GetConservative(), encodeResultType,
                                                           resultTypeVNPair.GetConservative());

                    normalPair = ValueNumPair(normalLVN, normalCVN);

                    excSetPair = vnStore->VNPExcSetUnion(op1Xvnp, op2Xvnp);
                    excSetPair = vnStore->VNPExcSetUnion(excSetPair, op3Xvnp);
                }
            }
        }
    }

    // Some intrinsics should always be unique
    bool makeUnique = false;

#if defined(TARGET_XARCH)
    switch (intrinsicId)
    {
        case NI_AVX512F_ConvertMaskToVector:
        {
            // We want to ensure that we get a TYP_MASK local to
            // ensure the relevant optimizations can kick in

            makeUnique = true;
            break;
        }

        default:
        {
            break;
        }
    }
#endif // TARGET_XARCH

    if (makeUnique)
    {
        tree->gtVNPair = vnStore->VNPUniqueWithExc(tree->TypeGet(), excSetPair);
    }
    else
    {
        tree->gtVNPair = vnStore->VNPWithExc(normalPair, excSetPair);
    }

    // Currently, the only exceptions these intrinsics could throw are NREs.
    //
    if (isMemoryLoad || isMemoryStore)
    {
        // Most load operations are simple "IND<SIMD>(addr)" equivalents. However, there are exceptions such as AVX
        // "gather" operations, where the "effective" address - one from which the actual load will be performed and
        // NullReferenceExceptions are associated with does not match the value of "addr". We will punt handling those
        // precisely for now.
        switch (intrinsicId)
        {
#ifdef TARGET_XARCH
            case NI_SSE2_MaskMove:
            case NI_AVX_MaskStore:
            case NI_AVX2_MaskStore:
            case NI_AVX_MaskLoad:
            case NI_AVX2_MaskLoad:
            case NI_AVX2_GatherVector128:
            case NI_AVX2_GatherVector256:
            case NI_AVX2_GatherMaskVector128:
            case NI_AVX2_GatherMaskVector256:
            {
                ValueNumPair uniqAddrVNPair   = vnStore->VNPairForExpr(compCurBB, TYP_BYREF);
                ValueNumPair uniqExcVNPair    = vnStore->VNPairForFunc(TYP_REF, VNF_NullPtrExc, uniqAddrVNPair);
                ValueNumPair uniqExcSetVNPair = vnStore->VNPExcSetSingleton(uniqExcVNPair);

                tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, uniqExcSetVNPair);
            }
            break;
#endif // TARGET_XARCH

            default:
                fgValueNumberAddExceptionSetForIndirection(tree, addr);
                break;
        }
    }
}
#endif // FEATURE_HW_INTRINSICS

void Compiler::fgValueNumberCastTree(GenTree* tree)
{
    assert(tree->OperGet() == GT_CAST);

    ValueNumPair srcVNPair        = tree->AsOp()->gtOp1->gtVNPair;
    var_types    castToType       = tree->CastToType();
    var_types    castFromType     = tree->CastFromType();
    bool         srcIsUnsigned    = ((tree->gtFlags & GTF_UNSIGNED) != 0);
    bool         hasOverflowCheck = tree->gtOverflowEx();

    assert(genActualType(castToType) == genActualType(tree->TypeGet())); // Ensure that the resultType is correct

    tree->gtVNPair = vnStore->VNPairForCast(srcVNPair, castToType, castFromType, srcIsUnsigned, hasOverflowCheck);
}

// Compute the ValueNumber for a cast operation
ValueNum ValueNumStore::VNForCast(ValueNum  srcVN,
                                  var_types castToType,
                                  var_types castFromType,
                                  bool      srcIsUnsigned,    /* = false */
                                  bool      hasOverflowCheck) /* = false */
{

    if ((castFromType == TYP_I_IMPL) && (castToType == TYP_BYREF) && IsVNHandle(srcVN))
    {
        // Omit cast for (h)CNS_INT [TYP_I_IMPL -> TYP_BYREF]
        return srcVN;
    }

    // The resulting type after performing the cast is always widened to a supported IL stack size
    var_types resultType = genActualType(castToType);

    // For integral unchecked casts, only the "int -> long" upcasts use
    // "srcIsUnsigned", to decide whether to use sign or zero extension.
    if (!hasOverflowCheck && !varTypeIsFloating(castToType) && (genTypeSize(castToType) <= genTypeSize(castFromType)))
    {
        srcIsUnsigned = false;
    }

    ValueNum srcExcVN;
    ValueNum srcNormVN;
    VNUnpackExc(srcVN, &srcNormVN, &srcExcVN);

    VNFunc   castFunc     = hasOverflowCheck ? VNF_CastOvf : VNF_Cast;
    ValueNum castTypeVN   = VNForCastOper(castToType, srcIsUnsigned);
    ValueNum resultNormVN = VNForFunc(resultType, castFunc, srcNormVN, castTypeVN);
    ValueNum resultExcVN  = srcExcVN;

    // Add an exception, except if folding took place.
    // We only fold checked casts that do not overflow.
    if (hasOverflowCheck && !IsVNConstant(resultNormVN))
    {
        ValueNum ovfChk = VNForFunc(TYP_REF, VNF_ConvOverflowExc, srcNormVN, castTypeVN);
        resultExcVN     = VNExcSetUnion(VNExcSetSingleton(ovfChk), srcExcVN);
    }

    ValueNum resultVN = VNWithExc(resultNormVN, resultExcVN);

    return resultVN;
}

// Compute the ValueNumberPair for a cast operation
ValueNumPair ValueNumStore::VNPairForCast(ValueNumPair srcVNPair,
                                          var_types    castToType,
                                          var_types    castFromType,
                                          bool         srcIsUnsigned,    /* = false */
                                          bool         hasOverflowCheck) /* = false */
{
    ValueNum srcLibVN = srcVNPair.GetLiberal();
    ValueNum srcConVN = srcVNPair.GetConservative();

    ValueNum castLibVN = VNForCast(srcLibVN, castToType, castFromType, srcIsUnsigned, hasOverflowCheck);
    ValueNum castConVN;

    if (srcVNPair.BothEqual())
    {
        castConVN = castLibVN;
    }
    else
    {
        castConVN = VNForCast(srcConVN, castToType, castFromType, srcIsUnsigned, hasOverflowCheck);
    }

    return {castLibVN, castConVN};
}

//------------------------------------------------------------------------
// fgValueNumberBitCast: Value number a bitcast.
//
// Arguments:
//    tree - The tree performing the bitcast
//
void Compiler::fgValueNumberBitCast(GenTree* tree)
{
    assert(tree->OperIs(GT_BITCAST));

    ValueNumPair srcVNPair  = tree->gtGetOp1()->gtVNPair;
    var_types    castToType = tree->TypeGet();

    ValueNumPair srcNormVNPair;
    ValueNumPair srcExcVNPair;
    vnStore->VNPUnpackExc(srcVNPair, &srcNormVNPair, &srcExcVNPair);

    ValueNumPair resultNormVNPair = vnStore->VNPairForBitCast(srcNormVNPair, castToType, genTypeSize(castToType));
    ValueNumPair resultExcVNPair  = srcExcVNPair;

    tree->gtVNPair = vnStore->VNPWithExc(resultNormVNPair, resultExcVNPair);
}

//------------------------------------------------------------------------
// EncodeBitCastType: Encode the target type of a bitcast.
//
// In most cases, it is sufficient to simply encode the numerical value of
// "castToType", as "size" will be implicitly encoded in the source VN's
// type. There is one instance where this is not true: small structs, as
// numbering, much like IR, does not support "true" small types. Thus, we
// encode structs (all of them, for simplicity) specially.
//
// Arguments:
//    castToType - The target type
//    size       - Its size
//
// Return Value:
//    Value number representing the target type.
//
ValueNum ValueNumStore::EncodeBitCastType(var_types castToType, unsigned size)
{
    if (castToType != TYP_STRUCT)
    {
        assert(size == genTypeSize(castToType));
        return VNForIntCon(castToType);
    }

    assert(size != 0);
    return VNForIntCon(TYP_COUNT + size);
}

//------------------------------------------------------------------------
// DecodeBitCastType: Decode the target type of a bitcast.
//
// Decodes VNs produced by "EncodeBitCastType".
//
// Arguments:
//    castToTypeVN - VN representing the target type
//    pSize        - [out] parameter for the size of the target type
//
// Return Value:
//    The target type.
//
var_types ValueNumStore::DecodeBitCastType(ValueNum castToTypeVN, unsigned* pSize)
{
    unsigned encodedType = ConstantValue<unsigned>(castToTypeVN);

    if (encodedType < TYP_COUNT)
    {
        var_types castToType = static_cast<var_types>(encodedType);

        *pSize = genTypeSize(castToType);
        return castToType;
    }

    *pSize = encodedType - TYP_COUNT;
    return TYP_STRUCT;
}

//------------------------------------------------------------------------
// VNForBitCast: Get the VN representing bitwise reinterpretation of types.
//
// Arguments:
//    srcVN      - (VN of) the value being cast from
//    castToType - The type being cast to
//    size       - Size of the target type
//
// Return Value:
//    The value number representing "IND<castToType>(ADDR(srcVN))". Notably,
//    this includes the special sign/zero-extension semantic for small types.
//
// Notes:
//    Bitcasts play a very significant role of representing identity (entire)
//    selections and stores in the physical maps: when we have to "normalize"
//    the types, we need something that represents a "nop" in the selection
//    process, and "VNF_BitCast" is that function. See also the notes for
//    "VNForLoadStoreBitCast".
//
ValueNum ValueNumStore::VNForBitCast(ValueNum srcVN, var_types castToType, unsigned size)
{
    // BitCast<type one>(BitCast<type two>(x)) => BitCast<type one>(x).
    // This ensures we do not end up with pathologically long chains of
    // bitcasts in physical maps. We could do a similar optimization in
    // "VNForMapPhysical[Store|Select]"; we presume that's not worth it,
    // and it is better TP-wise to skip bitcasts "lazily" when doing the
    // selection, as the scenario where they are expected to be common,
    // single-field structs, implies short selection chains.
    VNFuncApp srcVNFunc{VNF_COUNT};
    if (GetVNFunc(srcVN, &srcVNFunc) && (srcVNFunc.m_func == VNF_BitCast))
    {
        srcVN = srcVNFunc.m_args[0];
    }

    var_types srcType = TypeOfVN(srcVN);

    if (srcType == castToType)
    {
        return srcVN;
    }

    assert((castToType != TYP_STRUCT) || (srcType != TYP_STRUCT));

    if (srcVNFunc.m_func == VNF_ZeroObj)
    {
        return VNZeroForType(castToType);
    }

    return VNForFunc(castToType, VNF_BitCast, srcVN, EncodeBitCastType(castToType, size));
}

//------------------------------------------------------------------------
// VNPairForBitCast: VNForBitCast applied to a ValueNumPair.
//
ValueNumPair ValueNumStore::VNPairForBitCast(ValueNumPair srcVNPair, var_types castToType, unsigned size)
{
    ValueNum srcLibVN = srcVNPair.GetLiberal();
    ValueNum srcConVN = srcVNPair.GetConservative();

    ValueNum bitCastLibVN = VNForBitCast(srcLibVN, castToType, size);
    ValueNum bitCastConVN;

    if (srcVNPair.BothEqual())
    {
        bitCastConVN = bitCastLibVN;
    }
    else
    {
        bitCastConVN = VNForBitCast(srcConVN, castToType, size);
    }

    return ValueNumPair(bitCastLibVN, bitCastConVN);
}

void Compiler::fgValueNumberHelperCallFunc(GenTreeCall* call, VNFunc vnf, ValueNumPair vnpExc)
{
    unsigned nArgs = ValueNumStore::VNFuncArity(vnf);
    assert(vnf != VNF_Boundary);
    CallArgs* args                    = &call->gtArgs;
    bool      generateUniqueVN        = false;
    bool      useEntryPointAddrAsArg0 = false;

    switch (vnf)
    {
        case VNF_JitNew:
        {
            generateUniqueVN = true;
            vnpExc           = ValueNumStore::VNPForEmptyExcSet();
        }
        break;

        case VNF_JitNewArr:
        {
            generateUniqueVN  = true;
            ValueNumPair vnp1 = vnStore->VNPNormalPair(args->GetArgByIndex(1)->GetNode()->gtVNPair);

            // The New Array helper may throw an overflow exception
            vnpExc = vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_NewArrOverflowExc, vnp1));
        }
        break;

        case VNF_JitNewMdArr:
        {
            // TODO-MDArray: support value numbering new MD array helper
            generateUniqueVN = true;
        }
        break;

        case VNF_Box:
        case VNF_BoxNullable:
        {
            // Generate unique VN so, VNForFunc generates a unique value number for box nullable.
            // Alternatively instead of using vnpUniq below in VNPairForFunc(...),
            // we could use the value number of what the byref arg0 points to.
            //
            // But retrieving the value number of what the byref arg0 points to is quite a bit more work
            // and doing so only very rarely allows for an additional optimization.
            generateUniqueVN = true;
        }
        break;

        case VNF_JitReadyToRunNew:
        {
            generateUniqueVN        = true;
            vnpExc                  = ValueNumStore::VNPForEmptyExcSet();
            useEntryPointAddrAsArg0 = true;
        }
        break;

        case VNF_JitReadyToRunNewArr:
        {
            generateUniqueVN  = true;
            ValueNumPair vnp1 = vnStore->VNPNormalPair(args->GetArgByIndex(0)->GetNode()->gtVNPair);

            // The New Array helper may throw an overflow exception
            vnpExc = vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_NewArrOverflowExc, vnp1));
            useEntryPointAddrAsArg0 = true;
        }
        break;

        case VNF_ReadyToRunStaticBaseGC:
        case VNF_ReadyToRunStaticBaseNonGC:
        case VNF_ReadyToRunStaticBaseThread:
        case VNF_ReadyToRunStaticBaseThreadNonGC:
        case VNF_ReadyToRunGenericStaticBase:
        case VNF_ReadyToRunIsInstanceOf:
        case VNF_ReadyToRunCastClass:
        case VNF_ReadyToRunGenericHandle:
        {
            useEntryPointAddrAsArg0 = true;
        }
        break;

        default:
        {
            assert(s_helperCallProperties.IsPure(eeGetHelperNum(call->gtCallMethHnd)));

#ifdef DEBUG
            for (CallArg& arg : call->gtArgs.Args())
            {
                assert(!arg.AbiInfo.PassedByRef &&
                       "Helpers taking implicit byref arguments should not be marked as pure");
            }
#endif
        }
        break;
    }

    if (generateUniqueVN)
    {
        nArgs--;
    }

    ValueNumPair vnpUniq;
    if (generateUniqueVN)
    {
        // Generate unique VN so, VNForFunc generates a unique value number.
        vnpUniq.SetBoth(vnStore->VNForExpr(compCurBB, call->TypeGet()));
    }

    if (call->GetIndirectionCellArgKind() != WellKnownArg::None)
    {
        // If we are VN'ing a call with indirection cell arg (e.g. because this
        // is a helper in a R2R compilation) then morph should already have
        // added this arg, so we do not need to use EntryPointAddrAsArg0
        // because the indirection cell itself allows us to disambiguate.
        useEntryPointAddrAsArg0 = false;
    }

    CallArg* curArg = args->Args().begin().GetArg();
    if (nArgs == 0)
    {
        if (generateUniqueVN)
        {
            call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnpUniq);
        }
        else
        {
            call->gtVNPair.SetBoth(vnStore->VNForFunc(call->TypeGet(), vnf));
        }
    }
    else
    {
        // Has at least one argument.
        ValueNumPair vnp0;
        ValueNumPair vnp0x = ValueNumStore::VNPForEmptyExcSet();
#ifdef FEATURE_READYTORUN
        if (useEntryPointAddrAsArg0)
        {
            ssize_t  addrValue  = (ssize_t)call->gtEntryPoint.addr;
            ValueNum callAddrVN = vnStore->VNForHandle(addrValue, GTF_ICON_FTN_ADDR);
            vnp0                = ValueNumPair(callAddrVN, callAddrVN);
        }
        else
#endif // FEATURE_READYTORUN
        {
            assert(!useEntryPointAddrAsArg0);
            ValueNumPair vnp0wx = curArg->GetNode()->gtVNPair;
            vnStore->VNPUnpackExc(vnp0wx, &vnp0, &vnp0x);

            // Also include in the argument exception sets
            vnpExc = vnStore->VNPExcSetUnion(vnpExc, vnp0x);

            curArg = curArg->GetNext();
        }
        if (nArgs == 1)
        {
            if (generateUniqueVN)
            {
                call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnp0, vnpUniq);
            }
            else
            {
                call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnp0);
            }
        }
        else
        {
            // Has at least two arguments.
            ValueNumPair vnp1wx = curArg->GetNode()->gtVNPair;
            ValueNumPair vnp1;
            ValueNumPair vnp1x;
            vnStore->VNPUnpackExc(vnp1wx, &vnp1, &vnp1x);
            vnpExc = vnStore->VNPExcSetUnion(vnpExc, vnp1x);

            curArg = curArg->GetNext();
            if (nArgs == 2)
            {
                if (generateUniqueVN)
                {
                    call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnp0, vnp1, vnpUniq);
                }
                else
                {
                    call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnp0, vnp1);
                }
            }
            else
            {
                ValueNumPair vnp2wx = curArg->GetNode()->gtVNPair;
                ValueNumPair vnp2;
                ValueNumPair vnp2x;
                vnStore->VNPUnpackExc(vnp2wx, &vnp2, &vnp2x);
                vnpExc = vnStore->VNPExcSetUnion(vnpExc, vnp2x);

                curArg = curArg->GetNext();
                assert(nArgs == 3); // Our current maximum.
                assert(curArg == nullptr);
                if (generateUniqueVN)
                {
                    call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnp0, vnp1, vnp2, vnpUniq);
                }
                else
                {
                    call->gtVNPair = vnStore->VNPairForFunc(call->TypeGet(), vnf, vnp0, vnp1, vnp2);
                }
            }
        }
        // Add the accumulated exceptions.
        call->gtVNPair = vnStore->VNPWithExc(call->gtVNPair, vnpExc);
    }
    assert(curArg == nullptr || generateUniqueVN); // All arguments should be processed or we generate unique VN and do
                                                   // not care.
}

void Compiler::fgValueNumberCall(GenTreeCall* call)
{
    if (call->gtCallType == CT_HELPER)
    {
        bool modHeap = fgValueNumberHelperCall(call);

        if (modHeap)
        {
            // For now, arbitrary side effect on GcHeap/ByrefExposed.
            fgMutateGcHeap(call DEBUGARG("HELPER - modifies heap"));
        }
    }
    else
    {
        if (call->TypeGet() == TYP_VOID)
        {
            call->gtVNPair.SetBoth(ValueNumStore::VNForVoid());
        }
        else
        {
            call->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, call->TypeGet()));
        }

        // For now, arbitrary side effect on GcHeap/ByrefExposed.
        fgMutateGcHeap(call DEBUGARG("CALL"));
    }

    // If the call generates a definition, because it uses "return buffer", then VN the local
    // as well.
    GenTreeLclVarCommon* lclVarTree = nullptr;
    ssize_t              offset     = 0;
    unsigned             storeSize  = 0;
    if (call->DefinesLocal(this, &lclVarTree, /* pIsEntire */ nullptr, &offset, &storeSize))
    {
        ValueNumPair storeValue;
        storeValue.SetBoth(vnStore->VNForExpr(compCurBB, TYP_STRUCT));

        fgValueNumberLocalStore(call, lclVarTree, offset, storeSize, storeValue);
    }
}

void Compiler::fgValueNumberCastHelper(GenTreeCall* call)
{
    CorInfoHelpFunc helpFunc         = eeGetHelperNum(call->gtCallMethHnd);
    var_types       castToType       = TYP_UNDEF;
    var_types       castFromType     = TYP_UNDEF;
    bool            srcIsUnsigned    = false;
    bool            hasOverflowCheck = false;

    switch (helpFunc)
    {
        case CORINFO_HELP_LNG2DBL:
            castToType   = TYP_DOUBLE;
            castFromType = TYP_LONG;
            break;

        case CORINFO_HELP_ULNG2DBL:
            castToType    = TYP_DOUBLE;
            castFromType  = TYP_LONG;
            srcIsUnsigned = true;
            break;

        case CORINFO_HELP_DBL2INT:
            castToType   = TYP_INT;
            castFromType = TYP_DOUBLE;
            break;

        case CORINFO_HELP_DBL2INT_OVF:
            castToType       = TYP_INT;
            castFromType     = TYP_DOUBLE;
            hasOverflowCheck = true;
            break;

        case CORINFO_HELP_DBL2LNG:
            castToType   = TYP_LONG;
            castFromType = TYP_DOUBLE;
            break;

        case CORINFO_HELP_DBL2LNG_OVF:
            castToType       = TYP_LONG;
            castFromType     = TYP_DOUBLE;
            hasOverflowCheck = true;
            break;

        case CORINFO_HELP_DBL2UINT:
            castToType   = TYP_UINT;
            castFromType = TYP_DOUBLE;
            break;

        case CORINFO_HELP_DBL2UINT_OVF:
            castToType       = TYP_UINT;
            castFromType     = TYP_DOUBLE;
            hasOverflowCheck = true;
            break;

        case CORINFO_HELP_DBL2ULNG:
            castToType   = TYP_ULONG;
            castFromType = TYP_DOUBLE;
            break;

        case CORINFO_HELP_DBL2ULNG_OVF:
            castToType       = TYP_ULONG;
            castFromType     = TYP_DOUBLE;
            hasOverflowCheck = true;
            break;

        default:
            unreached();
    }

    ValueNumPair argVNP  = call->gtArgs.GetArgByIndex(0)->GetNode()->gtVNPair;
    ValueNumPair castVNP = vnStore->VNPairForCast(argVNP, castToType, castFromType, srcIsUnsigned, hasOverflowCheck);

    call->SetVNs(castVNP);
}

VNFunc Compiler::fgValueNumberJitHelperMethodVNFunc(CorInfoHelpFunc helpFunc)
{
    assert(s_helperCallProperties.IsPure(helpFunc) || s_helperCallProperties.IsAllocator(helpFunc));

    VNFunc vnf = VNF_Boundary; // An illegal value...
    switch (helpFunc)
    {
        // These translate to other function symbols:
        case CORINFO_HELP_DIV:
            vnf = VNFunc(GT_DIV);
            break;
        case CORINFO_HELP_MOD:
            vnf = VNFunc(GT_MOD);
            break;
        case CORINFO_HELP_UDIV:
            vnf = VNFunc(GT_UDIV);
            break;
        case CORINFO_HELP_UMOD:
            vnf = VNFunc(GT_UMOD);
            break;
        case CORINFO_HELP_LLSH:
            vnf = VNFunc(GT_LSH);
            break;
        case CORINFO_HELP_LRSH:
            vnf = VNFunc(GT_RSH);
            break;
        case CORINFO_HELP_LRSZ:
            vnf = VNFunc(GT_RSZ);
            break;
        case CORINFO_HELP_LMUL:
            vnf = VNFunc(GT_MUL);
            break;
        case CORINFO_HELP_LMUL_OVF:
            vnf = VNF_MUL_OVF;
            break;
        case CORINFO_HELP_ULMUL_OVF:
            vnf = VNF_MUL_UN_OVF;
            break;
        case CORINFO_HELP_LDIV:
            vnf = VNFunc(GT_DIV);
            break;
        case CORINFO_HELP_LMOD:
            vnf = VNFunc(GT_MOD);
            break;
        case CORINFO_HELP_ULDIV:
            vnf = VNFunc(GT_UDIV);
            break;
        case CORINFO_HELP_ULMOD:
            vnf = VNFunc(GT_UMOD);
            break;
        case CORINFO_HELP_FLTREM:
            vnf = VNFunc(GT_MOD);
            break;
        case CORINFO_HELP_DBLREM:
            vnf = VNFunc(GT_MOD);
            break;
        case CORINFO_HELP_FLTROUND:
            vnf = VNF_FltRound;
            break; // Is this the right thing?
        case CORINFO_HELP_DBLROUND:
            vnf = VNF_DblRound;
            break; // Is this the right thing?

        // These allocation operations probably require some augmentation -- perhaps allocSiteId,
        // something about array length...
        case CORINFO_HELP_NEWFAST:
        case CORINFO_HELP_NEWSFAST:
        case CORINFO_HELP_NEWSFAST_FINALIZE:
        case CORINFO_HELP_NEWSFAST_ALIGN8:
        case CORINFO_HELP_NEWSFAST_ALIGN8_VC:
        case CORINFO_HELP_NEWSFAST_ALIGN8_FINALIZE:
            vnf = VNF_JitNew;
            break;

        case CORINFO_HELP_READYTORUN_NEW:
            vnf = VNF_JitReadyToRunNew;
            break;

        case CORINFO_HELP_NEWARR_1_DIRECT:
        case CORINFO_HELP_NEWARR_1_OBJ:
        case CORINFO_HELP_NEWARR_1_VC:
        case CORINFO_HELP_NEWARR_1_ALIGN8:
            vnf = VNF_JitNewArr;
            break;

        case CORINFO_HELP_NEW_MDARR:
        case CORINFO_HELP_NEW_MDARR_RARE:
            vnf = VNF_JitNewMdArr;
            break;

        case CORINFO_HELP_READYTORUN_NEWARR_1:
            vnf = VNF_JitReadyToRunNewArr;
            break;

        case CORINFO_HELP_NEWFAST_MAYBEFROZEN:
            vnf = opts.IsReadyToRun() ? VNF_JitReadyToRunNew : VNF_JitNew;
            break;

        case CORINFO_HELP_NEWARR_1_MAYBEFROZEN:
            vnf = opts.IsReadyToRun() ? VNF_JitReadyToRunNewArr : VNF_JitNewArr;
            break;

        case CORINFO_HELP_GETGENERICS_GCSTATIC_BASE:
            vnf = VNF_GetgenericsGcstaticBase;
            break;
        case CORINFO_HELP_GETGENERICS_NONGCSTATIC_BASE:
            vnf = VNF_GetgenericsNongcstaticBase;
            break;
        case CORINFO_HELP_GETSHARED_GCSTATIC_BASE:
            vnf = VNF_GetsharedGcstaticBase;
            break;
        case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE:
            vnf = VNF_GetsharedNongcstaticBase;
            break;
        case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR:
            vnf = VNF_GetsharedGcstaticBaseNoctor;
            break;
        case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR:
            vnf = VNF_GetsharedNongcstaticBaseNoctor;
            break;
        case CORINFO_HELP_READYTORUN_GCSTATIC_BASE:
            vnf = VNF_ReadyToRunStaticBaseGC;
            break;
        case CORINFO_HELP_READYTORUN_NONGCSTATIC_BASE:
            vnf = VNF_ReadyToRunStaticBaseNonGC;
            break;
        case CORINFO_HELP_READYTORUN_THREADSTATIC_BASE:
            vnf = VNF_ReadyToRunStaticBaseThread;
            break;
        case CORINFO_HELP_READYTORUN_NONGCTHREADSTATIC_BASE:
            vnf = VNF_ReadyToRunStaticBaseThreadNonGC;
            break;
        case CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE:
            vnf = VNF_ReadyToRunGenericStaticBase;
            break;
        case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_DYNAMICCLASS:
            vnf = VNF_GetsharedGcstaticBaseDynamicclass;
            break;
        case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_DYNAMICCLASS:
            vnf = VNF_GetsharedNongcstaticBaseDynamicclass;
            break;
        case CORINFO_HELP_CLASSINIT_SHARED_DYNAMICCLASS:
            vnf = VNF_ClassinitSharedDynamicclass;
            break;
        case CORINFO_HELP_GETGENERICS_GCTHREADSTATIC_BASE:
            vnf = VNF_GetgenericsGcthreadstaticBase;
            break;
        case CORINFO_HELP_GETGENERICS_NONGCTHREADSTATIC_BASE:
            vnf = VNF_GetgenericsNongcthreadstaticBase;
            break;
        case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE:
            vnf = VNF_GetsharedGcthreadstaticBase;
            break;
        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE:
            vnf = VNF_GetsharedNongcthreadstaticBase;
            break;
        case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR:
            vnf = VNF_GetsharedGcthreadstaticBaseNoctor;
            break;
        case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED:
            vnf = VNF_GetsharedGcthreadstaticBaseNoctorOptimized;
            break;
        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR:
            vnf = VNF_GetsharedNongcthreadstaticBaseNoctor;
            break;
        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED:
            vnf = VNF_GetsharedNongcthreadstaticBaseNoctorOptimized;
            break;
        case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_DYNAMICCLASS:
            vnf = VNF_GetsharedGcthreadstaticBaseDynamicclass;
            break;
        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_DYNAMICCLASS:
            vnf = VNF_GetsharedNongcthreadstaticBaseDynamicclass;
            break;
        case CORINFO_HELP_GETSTATICFIELDADDR_TLS:
            vnf = VNF_GetStaticAddrTLS;
            break;

        case CORINFO_HELP_RUNTIMEHANDLE_METHOD:
        case CORINFO_HELP_RUNTIMEHANDLE_METHOD_LOG:
            vnf = VNF_RuntimeHandleMethod;
            break;

        case CORINFO_HELP_READYTORUN_GENERIC_HANDLE:
            vnf = VNF_ReadyToRunGenericHandle;
            break;

        case CORINFO_HELP_RUNTIMEHANDLE_CLASS:
        case CORINFO_HELP_RUNTIMEHANDLE_CLASS_LOG:
            vnf = VNF_RuntimeHandleClass;
            break;

        case CORINFO_HELP_STRCNS:
            vnf = VNF_LazyStrCns;
            break;

        case CORINFO_HELP_CHKCASTCLASS:
        case CORINFO_HELP_CHKCASTCLASS_SPECIAL:
        case CORINFO_HELP_CHKCASTARRAY:
        case CORINFO_HELP_CHKCASTINTERFACE:
        case CORINFO_HELP_CHKCASTANY:
            vnf = VNF_CastClass;
            break;

        case CORINFO_HELP_READYTORUN_CHKCAST:
            vnf = VNF_ReadyToRunCastClass;
            break;

        case CORINFO_HELP_ISINSTANCEOFCLASS:
        case CORINFO_HELP_ISINSTANCEOFINTERFACE:
        case CORINFO_HELP_ISINSTANCEOFARRAY:
        case CORINFO_HELP_ISINSTANCEOFANY:
            vnf = VNF_IsInstanceOf;
            break;

        case CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE:
            vnf = VNF_TypeHandleToRuntimeType;
            break;

        case CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE:
            vnf = VNF_TypeHandleToRuntimeTypeHandle;
            break;

        case CORINFO_HELP_READYTORUN_ISINSTANCEOF:
            vnf = VNF_ReadyToRunIsInstanceOf;
            break;

        case CORINFO_HELP_LDELEMA_REF:
            vnf = VNF_LdElemA;
            break;

        case CORINFO_HELP_UNBOX:
            vnf = VNF_Unbox;
            break;

        // A constant within any method.
        case CORINFO_HELP_GETCURRENTMANAGEDTHREADID:
            vnf = VNF_ManagedThreadId;
            break;

        case CORINFO_HELP_GETREFANY:
            // TODO-CQ: This should really be interpreted as just a struct field reference, in terms of values.
            vnf = VNF_GetRefanyVal;
            break;

        case CORINFO_HELP_GETCLASSFROMMETHODPARAM:
            vnf = VNF_GetClassFromMethodParam;
            break;

        case CORINFO_HELP_GETSYNCFROMCLASSHANDLE:
            vnf = VNF_GetSyncFromClassHandle;
            break;

        case CORINFO_HELP_LOOP_CLONE_CHOICE_ADDR:
            vnf = VNF_LoopCloneChoiceAddr;
            break;

        case CORINFO_HELP_BOX:
            vnf = VNF_Box;
            break;

        case CORINFO_HELP_BOX_NULLABLE:
            vnf = VNF_BoxNullable;
            break;

        default:
            unreached();
    }

    assert(vnf != VNF_Boundary);
    return vnf;
}

bool Compiler::fgValueNumberHelperCall(GenTreeCall* call)
{
    CorInfoHelpFunc helpFunc = eeGetHelperNum(call->gtCallMethHnd);

    switch (helpFunc)
    {
        case CORINFO_HELP_LNG2DBL:
        case CORINFO_HELP_ULNG2DBL:
        case CORINFO_HELP_DBL2INT:
        case CORINFO_HELP_DBL2INT_OVF:
        case CORINFO_HELP_DBL2LNG:
        case CORINFO_HELP_DBL2LNG_OVF:
        case CORINFO_HELP_DBL2UINT:
        case CORINFO_HELP_DBL2UINT_OVF:
        case CORINFO_HELP_DBL2ULNG:
        case CORINFO_HELP_DBL2ULNG_OVF:
            fgValueNumberCastHelper(call);
            return false;

        default:
            break;
    }

    bool pure        = s_helperCallProperties.IsPure(helpFunc);
    bool isAlloc     = s_helperCallProperties.IsAllocator(helpFunc);
    bool modHeap     = s_helperCallProperties.MutatesHeap(helpFunc);
    bool mayRunCctor = s_helperCallProperties.MayRunCctor(helpFunc);
    bool noThrow     = s_helperCallProperties.NoThrow(helpFunc);

    ValueNumPair vnpExc = ValueNumStore::VNPForEmptyExcSet();

    // If the JIT helper can throw an exception make sure that we fill in
    // vnpExc with a Value Number that represents the exception(s) that can be thrown.
    if (!noThrow)
    {
        // If the helper is known to only throw only one particular exception
        // we can set vnpExc to that exception, otherwise we conservatively
        // model the JIT helper as possibly throwing multiple different exceptions
        //
        switch (helpFunc)
        {
            // This helper always throws the VNF_OverflowExc exception.
            case CORINFO_HELP_OVERFLOW:
                vnpExc = vnStore->VNPExcSetSingleton(
                    vnStore->VNPairForFunc(TYP_REF, VNF_OverflowExc, vnStore->VNPForVoid()));
                break;

            default:
                // Setup vnpExc with the information that multiple different exceptions
                // could be generated by this helper
                vnpExc = vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_HelperMultipleExc));
        }
    }

    ValueNumPair vnpNorm;

    if (call->TypeGet() == TYP_VOID)
    {
        vnpNorm = ValueNumStore::VNPForVoid();
    }
    else
    {
        if (pure || isAlloc)
        {
            VNFunc vnf = fgValueNumberJitHelperMethodVNFunc(helpFunc);

            if (mayRunCctor)
            {
                if ((call->gtFlags & GTF_CALL_HOISTABLE) == 0)
                {
                    modHeap = true;
                }
            }

            fgValueNumberHelperCallFunc(call, vnf, vnpExc);
            return modHeap;
        }
        else
        {
            vnpNorm.SetBoth(vnStore->VNForExpr(compCurBB, call->TypeGet()));
        }
    }

    call->gtVNPair = vnStore->VNPWithExc(vnpNorm, vnpExc);
    return modHeap;
}

//--------------------------------------------------------------------------------
// fgValueNumberAddExceptionSetForIndirection
//         - Adds the exception sets for the current tree node
//           which is performing a memory indirection operation
//
// Arguments:
//    tree       - The current GenTree node,
//                 It must be some kind of an indirection node
//                 or have an implicit indirection
//    baseAddr   - The address that we are indirecting
//
// Return Value:
//               - The tree's gtVNPair is updated to include the VNF_nullPtrExc
//                 exception set.  We calculate a base address to use as the
//                 argument to the VNF_nullPtrExc function.
//
// Notes:        - The calculation of the base address removes any constant
//                 offsets, so that obj.x and obj.y will both have obj as
//                 their base address.
//                 For arrays the base address currently includes the
//                 index calculations.
//
void Compiler::fgValueNumberAddExceptionSetForIndirection(GenTree* tree, GenTree* baseAddr)
{
    // We should have tree that a unary indirection or a tree node with an implicit indirection
    assert(tree->OperIsIndir() || tree->OperIsImplicitIndir());

    // if this indirection can be folded into a constant it means it can't trigger NullRef
    if (tree->gtVNPair.BothEqual() && vnStore->IsVNConstant(tree->gtVNPair.GetLiberal()))
    {
        return;
    }

    // We evaluate the baseAddr ValueNumber further in order
    // to obtain a better value to use for the null check exception.
    //
    ValueNumPair baseVNP = vnStore->VNPNormalPair(baseAddr->gtVNPair);
    ValueNum     baseLVN = baseVNP.GetLiberal();
    ValueNum     baseCVN = baseVNP.GetConservative();
    ssize_t      offsetL = 0;
    ssize_t      offsetC = 0;
    VNFuncApp    funcAttr;

    while (vnStore->GetVNFunc(baseLVN, &funcAttr) && (funcAttr.m_func == (VNFunc)GT_ADD) &&
           (vnStore->TypeOfVN(baseLVN) == TYP_BYREF))
    {
        // The arguments in value numbering functions are sorted in increasing order
        // Thus either arg could be the constant.
        if (vnStore->IsVNConstant(funcAttr.m_args[0]) && varTypeIsIntegral(vnStore->TypeOfVN(funcAttr.m_args[0])))
        {
            offsetL += vnStore->CoercedConstantValue<ssize_t>(funcAttr.m_args[0]);
            baseLVN = funcAttr.m_args[1];
        }
        else if (vnStore->IsVNConstant(funcAttr.m_args[1]) && varTypeIsIntegral(vnStore->TypeOfVN(funcAttr.m_args[1])))
        {
            offsetL += vnStore->CoercedConstantValue<ssize_t>(funcAttr.m_args[1]);
            baseLVN = funcAttr.m_args[0];
        }
        else // neither argument is a constant
        {
            break;
        }

        if (fgIsBigOffset(offsetL))
        {
            // Failure: Exit this loop if we have a "big" offset

            // reset baseLVN back to the full address expression
            baseLVN = baseVNP.GetLiberal();
            break;
        }
    }

    while (vnStore->GetVNFunc(baseCVN, &funcAttr) && (funcAttr.m_func == (VNFunc)GT_ADD) &&
           (vnStore->TypeOfVN(baseCVN) == TYP_BYREF))
    {
        // The arguments in value numbering functions are sorted in increasing order
        // Thus either arg could be the constant.
        if (vnStore->IsVNConstant(funcAttr.m_args[0]) && varTypeIsIntegral(vnStore->TypeOfVN(funcAttr.m_args[0])))
        {
            offsetL += vnStore->CoercedConstantValue<ssize_t>(funcAttr.m_args[0]);
            baseCVN = funcAttr.m_args[1];
        }
        else if (vnStore->IsVNConstant(funcAttr.m_args[1]) && varTypeIsIntegral(vnStore->TypeOfVN(funcAttr.m_args[1])))
        {
            offsetC += vnStore->CoercedConstantValue<ssize_t>(funcAttr.m_args[1]);
            baseCVN = funcAttr.m_args[0];
        }
        else // neither argument is a constant
        {
            break;
        }

        if (fgIsBigOffset(offsetC))
        {
            // Failure: Exit this loop if we have a "big" offset

            // reset baseCVN back to the full address expression
            baseCVN = baseVNP.GetConservative();
            break;
        }
    }

    // The exceptions in "baseVNP" should have been added to the "tree"'s set already.
    assert(vnStore->VNPExcIsSubset(vnStore->VNPExceptionSet(tree->gtVNPair),
                                   vnStore->VNPExceptionSet(ValueNumPair(baseLVN, baseCVN))));

    // The normal VNs for base address are used to create the NullPtrExcs
    ValueNumPair excChkSet = vnStore->VNPForEmptyExcSet();

    if (!vnStore->IsKnownNonNull(baseLVN))
    {
        excChkSet.SetLiberal(vnStore->VNExcSetSingleton(vnStore->VNForFunc(TYP_REF, VNF_NullPtrExc, baseLVN)));
    }

    if (!vnStore->IsKnownNonNull(baseCVN))
    {
        excChkSet.SetConservative(vnStore->VNExcSetSingleton(vnStore->VNForFunc(TYP_REF, VNF_NullPtrExc, baseCVN)));
    }

    // Add the NullPtrExc to "tree"'s value numbers.
    tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, excChkSet);
}

//--------------------------------------------------------------------------------
// fgValueNumberAddExceptionSetForDivision
//         - Adds the exception sets for the current tree node
//           which is performing an integer division operation
//
// Arguments:
//    tree       - The current GenTree node,
//                 It must be a node that performs an integer division
//
// Return Value:
//               - The tree's gtVNPair is updated to include
//                 VNF_DivideByZeroExc and VNF_ArithmeticExc,
//                 We will omit one or both of them when the operation
//                 has constants arguments that preclude the exception.
//
void Compiler::fgValueNumberAddExceptionSetForDivision(GenTree* tree)
{
    genTreeOps oper = tree->OperGet();

    // A Divide By Zero exception may be possible.
    // The divisor is held in tree->AsOp()->gtOp2
    //
    bool isUnsignedOper         = (oper == GT_UDIV) || (oper == GT_UMOD);
    bool needDivideByZeroExcLib = true;
    bool needDivideByZeroExcCon = true;
    bool needArithmeticExcLib   = !isUnsignedOper; // Overflow isn't possible for unsigned divide
    bool needArithmeticExcCon   = !isUnsignedOper;

    // Determine if we have a 32-bit or 64-bit divide operation
    var_types typ = genActualType(tree->TypeGet());
    assert((typ == TYP_INT) || (typ == TYP_LONG));

    // Retrieve the Norm VN for op2 to use it for the DivideByZeroExc
    ValueNumPair vnpOp2Norm   = vnStore->VNPNormalPair(tree->AsOp()->gtOp2->gtVNPair);
    ValueNum     vnOp2NormLib = vnpOp2Norm.GetLiberal();
    ValueNum     vnOp2NormCon = vnpOp2Norm.GetConservative();

    if (typ == TYP_INT)
    {
        if (vnStore->IsVNConstant(vnOp2NormLib))
        {
            INT32 kVal = vnStore->ConstantValue<INT32>(vnOp2NormLib);
            if (kVal != 0)
            {
                needDivideByZeroExcLib = false;
            }
            if (!isUnsignedOper && (kVal != -1))
            {
                needArithmeticExcLib = false;
            }
        }
        if (vnStore->IsVNConstant(vnOp2NormCon))
        {
            INT32 kVal = vnStore->ConstantValue<INT32>(vnOp2NormCon);
            if (kVal != 0)
            {
                needDivideByZeroExcCon = false;
            }
            if (!isUnsignedOper && (kVal != -1))
            {
                needArithmeticExcCon = false;
            }
        }
    }
    else // (typ == TYP_LONG)
    {
        if (vnStore->IsVNConstant(vnOp2NormLib))
        {
            INT64 kVal = vnStore->ConstantValue<INT64>(vnOp2NormLib);
            if (kVal != 0)
            {
                needDivideByZeroExcLib = false;
            }
            if (!isUnsignedOper && (kVal != -1))
            {
                needArithmeticExcLib = false;
            }
        }
        if (vnStore->IsVNConstant(vnOp2NormCon))
        {
            INT64 kVal = vnStore->ConstantValue<INT64>(vnOp2NormCon);
            if (kVal != 0)
            {
                needDivideByZeroExcCon = false;
            }
            if (!isUnsignedOper && (kVal != -1))
            {
                needArithmeticExcCon = false;
            }
        }
    }

    // Retrieve the Norm VN for op1 to use it for the ArithmeticExc
    ValueNumPair vnpOp1Norm   = vnStore->VNPNormalPair(tree->AsOp()->gtOp1->gtVNPair);
    ValueNum     vnOp1NormLib = vnpOp1Norm.GetLiberal();
    ValueNum     vnOp1NormCon = vnpOp1Norm.GetConservative();

    if (needArithmeticExcLib || needArithmeticExcCon)
    {
        if (typ == TYP_INT)
        {
            if (vnStore->IsVNConstant(vnOp1NormLib))
            {
                INT32 kVal = vnStore->ConstantValue<INT32>(vnOp1NormLib);

                if (!isUnsignedOper && (kVal != INT32_MIN))
                {
                    needArithmeticExcLib = false;
                }
            }
            if (vnStore->IsVNConstant(vnOp1NormCon))
            {
                INT32 kVal = vnStore->ConstantValue<INT32>(vnOp1NormCon);

                if (!isUnsignedOper && (kVal != INT32_MIN))
                {
                    needArithmeticExcCon = false;
                }
            }
        }
        else // (typ == TYP_LONG)
        {
            if (vnStore->IsVNConstant(vnOp1NormLib))
            {
                INT64 kVal = vnStore->ConstantValue<INT64>(vnOp1NormLib);

                if (!isUnsignedOper && (kVal != INT64_MIN))
                {
                    needArithmeticExcLib = false;
                }
            }
            if (vnStore->IsVNConstant(vnOp1NormCon))
            {
                INT64 kVal = vnStore->ConstantValue<INT64>(vnOp1NormCon);

                if (!isUnsignedOper && (kVal != INT64_MIN))
                {
                    needArithmeticExcCon = false;
                }
            }
        }
    }

    // Unpack, Norm,Exc for the tree's VN
    ValueNumPair vnpTreeNorm;
    ValueNumPair vnpTreeExc;
    ValueNumPair vnpDivZeroExc = ValueNumStore::VNPForEmptyExcSet();
    ValueNumPair vnpArithmExc  = ValueNumStore::VNPForEmptyExcSet();

    vnStore->VNPUnpackExc(tree->gtVNPair, &vnpTreeNorm, &vnpTreeExc);

    if (needDivideByZeroExcLib)
    {
        vnpDivZeroExc.SetLiberal(
            vnStore->VNExcSetSingleton(vnStore->VNForFunc(TYP_REF, VNF_DivideByZeroExc, vnOp2NormLib)));
    }
    if (needDivideByZeroExcCon)
    {
        vnpDivZeroExc.SetConservative(
            vnStore->VNExcSetSingleton(vnStore->VNForFunc(TYP_REF, VNF_DivideByZeroExc, vnOp2NormCon)));
    }
    if (needArithmeticExcLib)
    {
        vnpArithmExc.SetLiberal(vnStore->VNExcSetSingleton(
            vnStore->VNForFuncNoFolding(TYP_REF, VNF_ArithmeticExc, vnOp1NormLib, vnOp2NormLib)));
    }
    if (needArithmeticExcCon)
    {
        vnpArithmExc.SetConservative(vnStore->VNExcSetSingleton(
            vnStore->VNForFuncNoFolding(TYP_REF, VNF_ArithmeticExc, vnOp1NormLib, vnOp2NormCon)));
    }

    // Combine vnpDivZeroExc with the exception set of tree
    ValueNumPair newExcSet = vnStore->VNPExcSetUnion(vnpTreeExc, vnpDivZeroExc);
    // Combine vnpArithmExc with the newExcSet
    newExcSet = vnStore->VNPExcSetUnion(newExcSet, vnpArithmExc);

    // Updated VN for tree, it now includes DivideByZeroExc and/or ArithmeticExc
    tree->gtVNPair = vnStore->VNPWithExc(vnpTreeNorm, newExcSet);
}

//--------------------------------------------------------------------------------
// fgValueNumberAddExceptionSetForOverflow
//         - Adds the exception set for the current tree node
//           which is performing an overflow checking math operation
//
// Arguments:
//    tree       - The current GenTree node,
//                 It must be a node that performs an overflow
//                 checking math operation
//
// Return Value:
//               - The tree's gtVNPair is updated to include the VNF_OverflowExc
//                 exception set, except for constant VNs and those produced from identities.
//
void Compiler::fgValueNumberAddExceptionSetForOverflow(GenTree* tree)
{
    assert(tree->gtOverflowEx());

    // We should only be dealing with an Overflow checking ALU operation.
    VNFunc vnf = GetVNFuncForNode(tree);
    assert(ValueNumStore::VNFuncIsOverflowArithmetic(vnf));

    ValueNumKind vnKinds[2] = {VNK_Liberal, VNK_Conservative};
    for (ValueNumKind vnKind : vnKinds)
    {
        ValueNum vn = tree->GetVN(vnKind);

        // Unpack Norm, Exc for the current VN.
        ValueNum vnNorm;
        ValueNum vnExcSet;
        vnStore->VNUnpackExc(vn, &vnNorm, &vnExcSet);

        // Don't add exceptions if the normal VN represents a constant.
        // We only fold to constant VNs for operations that provably cannot overflow.
        if (vnStore->IsVNConstant(vnNorm))
        {
            continue;
        }

        // Don't add exceptions if the tree's normal VN has been derived from an identity.
        // This takes care of x + 0 == x, 0 + x == x, x - 0 == x, x * 1 == x, 1 * x == x.
        // The x - x == 0 and x * 0 == 0, 0 * x == 0 cases are handled by the "IsVNConstant" check above.
        if ((vnNorm == vnStore->VNNormalValue(tree->gtGetOp1()->GetVN(vnKind))) ||
            (vnNorm == vnStore->VNNormalValue(tree->gtGetOp2()->GetVN(vnKind))))
        {
            // TODO-Review: would it be acceptable to make ValueNumStore::EvalUsingMathIdentity
            // public just to assert here?
            continue;
        }

#ifdef DEBUG
        // The normal value number function should now be the same overflow checking ALU operation as 'vnf'.
        VNFuncApp normFuncApp;
        assert(vnStore->GetVNFunc(vnNorm, &normFuncApp) && (normFuncApp.m_func == vnf));
#endif // DEBUG

        // Overflow-checking operations add an overflow exception.
        // The normal result is used as the input argument for the OverflowExc.
        ValueNum vnOverflowExc = vnStore->VNExcSetSingleton(vnStore->VNForFunc(TYP_REF, VNF_OverflowExc, vnNorm));

        // Combine the new Overflow exception with the original exception set.
        vnExcSet = vnStore->VNExcSetUnion(vnExcSet, vnOverflowExc);

        // Update the VN to include the Overflow exception.
        ValueNum newVN = vnStore->VNWithExc(vnNorm, vnExcSet);
        tree->SetVN(vnKind, newVN);
    }
}

//--------------------------------------------------------------------------------
// fgValueNumberAddExceptionSetForBoundsCheck
//          - Adds the exception set for the current tree node
//            which is performing an bounds check operation
//
// Arguments:
//    tree  - The current GenTree node,
//            It must be a node that performs a bounds check operation
//
// Return Value:
//          - The tree's gtVNPair is updated to include the
//            VNF_IndexOutOfRangeExc exception set.
//
void Compiler::fgValueNumberAddExceptionSetForBoundsCheck(GenTree* tree)
{
    GenTreeBoundsChk* node = tree->AsBoundsChk();
    assert(node != nullptr);

    ValueNumPair vnpIndex  = node->GetIndex()->gtVNPair;
    ValueNumPair vnpArrLen = node->GetArrayLength()->gtVNPair;

    // Unpack, Norm,Exc for the tree's VN
    //
    ValueNumPair vnpTreeNorm;
    ValueNumPair vnpTreeExc;

    vnStore->VNPUnpackExc(tree->gtVNPair, &vnpTreeNorm, &vnpTreeExc);

    // Construct the exception set for bounds check
    ValueNumPair boundsChkExcSet = vnStore->VNPExcSetSingleton(
        vnStore->VNPairForFuncNoFolding(TYP_REF, VNF_IndexOutOfRangeExc, vnStore->VNPNormalPair(vnpIndex),
                                        vnStore->VNPNormalPair(vnpArrLen)));

    // Combine the new Overflow exception with the original exception set of tree
    ValueNumPair newExcSet = vnStore->VNPExcSetUnion(vnpTreeExc, boundsChkExcSet);

    // Update the VN for the tree it, the updated VN for tree
    // now includes the IndexOutOfRange exception.
    tree->gtVNPair = vnStore->VNPWithExc(vnpTreeNorm, newExcSet);
}

//--------------------------------------------------------------------------------
// fgValueNumberAddExceptionSetForCkFinite
//         - Adds the exception set for the current tree node
//           which is a CkFinite operation
//
// Arguments:
//    tree       - The current GenTree node,
//                 It must be a CkFinite node
//
// Return Value:
//               - The tree's gtVNPair is updated to include the VNF_ArithmeticExc
//                 exception set.
//
void Compiler::fgValueNumberAddExceptionSetForCkFinite(GenTree* tree)
{
    // We should only be dealing with an check finite operation.
    assert(tree->OperGet() == GT_CKFINITE);

    // Unpack, Norm,Exc for the tree's VN
    //
    ValueNumPair vnpTreeNorm;
    ValueNumPair vnpTreeExc;
    ValueNumPair newExcSet;

    vnStore->VNPUnpackExc(tree->gtVNPair, &vnpTreeNorm, &vnpTreeExc);

    // ckfinite adds an Arithmetic exception
    // The normal result is used as the input argument for the ArithmeticExc
    ValueNumPair arithmeticExcSet =
        vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_ArithmeticExc, vnpTreeNorm));

    // Combine the new Arithmetic exception with the original exception set of tree
    newExcSet = vnStore->VNPExcSetUnion(vnpTreeExc, arithmeticExcSet);

    // Updated VN for tree, it now includes Arithmetic exception
    tree->gtVNPair = vnStore->VNPWithExc(vnpTreeNorm, newExcSet);
}

//--------------------------------------------------------------------------------
// fgValueNumberAddExceptionSet
//         - Adds any exception sets needed for the current tree node
//
// Arguments:
//    tree       - The current GenTree node,
//
// Return Value:
//               - The tree's gtVNPair is updated to include the exception sets.
//
// Notes:        - This method relies upon OperMayTHrow to determine if we need
//                 to add an exception set.  If OPerMayThrow returns false no
//                 exception set will be added.
//
void Compiler::fgValueNumberAddExceptionSet(GenTree* tree)
{
    if (tree->OperMayThrow(this))
    {
        switch (tree->OperGet())
        {
            case GT_CAST: // A cast with an overflow check
                break;    // Already handled by VNPairForCast()

            case GT_ADD: // An Overflow checking ALU operation
            case GT_SUB:
            case GT_MUL:
                assert(tree->gtOverflowEx());
                fgValueNumberAddExceptionSetForOverflow(tree);
                break;

            case GT_DIV:
            case GT_UDIV:
            case GT_MOD:
            case GT_UMOD:
                fgValueNumberAddExceptionSetForDivision(tree);
                break;

            case GT_BOUNDS_CHECK:
                fgValueNumberAddExceptionSetForBoundsCheck(tree);
                break;

            case GT_LCLHEAP:
                // It is not necessary to model the StackOverflow exception for GT_LCLHEAP
                break;

            case GT_INTRINSIC:
                assert(tree->AsIntrinsic()->gtIntrinsicName == NI_System_Object_GetType);
                fgValueNumberAddExceptionSetForIndirection(tree, tree->AsIntrinsic()->gtGetOp1());
                break;

            case GT_XAND:
            case GT_XORR:
            case GT_XADD:
            case GT_XCHG:
            case GT_CMPXCHG:
            case GT_IND:
            case GT_BLK:
            case GT_STOREIND:
            case GT_STORE_BLK:
            case GT_NULLCHECK:
                fgValueNumberAddExceptionSetForIndirection(tree, tree->AsIndir()->Addr());
                break;

            case GT_ARR_LENGTH:
            case GT_MDARR_LENGTH:
            case GT_MDARR_LOWER_BOUND:
                fgValueNumberAddExceptionSetForIndirection(tree, tree->AsArrCommon()->ArrRef());
                break;

            case GT_CKFINITE:
                fgValueNumberAddExceptionSetForCkFinite(tree);
                break;

#ifdef FEATURE_HW_INTRINSICS
            case GT_HWINTRINSIC:
                // ToDo: model the exceptions for Intrinsics
                break;
#endif // FEATURE_HW_INTRINSICS

            default:
                assert(!"Handle this oper in fgValueNumberAddExceptionSet");
                break;
        }
    }
}

#ifdef DEBUG
//------------------------------------------------------------------------
// fgDebugCheckExceptionSets: Verify the exception sets on trees.
//
// This function checks that the node's exception set is a superset of
// the exception sets of its operands.
//
void Compiler::fgDebugCheckExceptionSets()
{
    struct ExceptionSetsChecker
    {
        static void CheckTree(GenTree* tree, ValueNumStore* vnStore)
        {
            // We will fail to VN some PHI_ARGs - their values may not
            // be known at the point we number them because of loops.
            assert(tree->gtVNPair.BothDefined() || tree->OperIs(GT_PHI_ARG));

            ValueNumPair operandsExcSet = vnStore->VNPForEmptyExcSet();
            tree->VisitOperands([&](GenTree* operand) -> GenTree::VisitResult {

                CheckTree(operand, vnStore);

                ValueNumPair operandVNP = operand->gtVNPair.BothDefined() ? operand->gtVNPair : vnStore->VNPForVoid();
                operandsExcSet          = vnStore->VNPUnionExcSet(operandVNP, operandsExcSet);

                return GenTree::VisitResult::Continue;
            });

            // Currently, we fail to properly maintain the exception sets for trees with user calls.
            if ((tree->gtFlags & GTF_CALL) != 0)
            {
                return;
            }

            ValueNumPair nodeExcSet = vnStore->VNPExceptionSet(tree->gtVNPair);
            assert(vnStore->VNExcIsSubset(nodeExcSet.GetLiberal(), operandsExcSet.GetLiberal()));
            assert(vnStore->VNExcIsSubset(nodeExcSet.GetConservative(), operandsExcSet.GetConservative()));
        }
    };

    for (BasicBlock* const block : Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            // Exclude statements VN hasn't visited for whichever reason...
            if (stmt->GetRootNode()->GetVN(VNK_Liberal) == ValueNumStore::NoVN)
            {
                continue;
            }

            ExceptionSetsChecker::CheckTree(stmt->GetRootNode(), vnStore);
        }
    }
}

// This method asserts that SSA name constraints specified are satisfied.
// Until we figure out otherwise, all VN's are assumed to be liberal.
// TODO-Cleanup: new JitTestLabels for lib vs cons vs both VN classes?
void Compiler::JitTestCheckVN()
{
    typedef JitHashTable<ssize_t, JitSmallPrimitiveKeyFuncs<ssize_t>, ValueNum>  LabelToVNMap;
    typedef JitHashTable<ValueNum, JitSmallPrimitiveKeyFuncs<ValueNum>, ssize_t> VNToLabelMap;

    // If we have no test data, early out.
    if (m_nodeTestData == nullptr)
    {
        return;
    }

    NodeToTestDataMap* testData = GetNodeTestData();

    // First we have to know which nodes in the tree are reachable.
    typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, int> NodeToIntMap;
    NodeToIntMap* reachable = FindReachableNodesInNodeTestData();

    LabelToVNMap* labelToVN = new (getAllocatorDebugOnly()) LabelToVNMap(getAllocatorDebugOnly());
    VNToLabelMap* vnToLabel = new (getAllocatorDebugOnly()) VNToLabelMap(getAllocatorDebugOnly());

    if (verbose)
    {
        printf("\nJit Testing: Value numbering.\n");
    }
    for (GenTree* const node : NodeToTestDataMap::KeyIteration(testData))
    {
        TestLabelAndNum tlAndN;
        ValueNum        nodeVN = node->GetVN(VNK_Liberal);

        bool b = testData->Lookup(node, &tlAndN);
        assert(b);
        if (tlAndN.m_tl == TL_VN || tlAndN.m_tl == TL_VNNorm)
        {
            int dummy;
            if (!reachable->Lookup(node, &dummy))
            {
                printf("Node ");
                Compiler::printTreeID(node);
                printf(" had a test constraint declared, but has become unreachable at the time the constraint is "
                       "tested.\n"
                       "(This is probably as a result of some optimization -- \n"
                       "you may need to modify the test case to defeat this opt.)\n");
                assert(false);
            }

            if (verbose)
            {
                printf("  Node ");
                Compiler::printTreeID(node);
                printf(" -- VN class %d.\n", tlAndN.m_num);
            }

            if (tlAndN.m_tl == TL_VNNorm)
            {
                nodeVN = vnStore->VNNormalValue(nodeVN);
            }

            ValueNum vn;
            if (labelToVN->Lookup(tlAndN.m_num, &vn))
            {
                if (verbose)
                {
                    printf("      Already in hash tables.\n");
                }
                // The mapping(s) must be one-to-one: if the label has a mapping, then the ssaNm must, as well.
                ssize_t num2;
                bool    found = vnToLabel->Lookup(vn, &num2);
                assert(found);
                // And the mappings must be the same.
                if (tlAndN.m_num != num2)
                {
                    printf("Node: ");
                    Compiler::printTreeID(node);
                    printf(", with value number " FMT_VN ", was declared in VN class %d,\n", nodeVN, tlAndN.m_num);
                    printf("but this value number " FMT_VN
                           " has already been associated with a different SSA name class: %d.\n",
                           vn, num2);
                    assert(false);
                }
                // And the current node must be of the specified SSA family.
                if (nodeVN != vn)
                {
                    printf("Node: ");
                    Compiler::printTreeID(node);
                    printf(", " FMT_VN " was declared in SSA name class %d,\n", nodeVN, tlAndN.m_num);
                    printf("but that name class was previously bound to a different value number: " FMT_VN ".\n", vn);
                    assert(false);
                }
            }
            else
            {
                ssize_t num;
                // The mapping(s) must be one-to-one: if the label has no mapping, then the ssaNm may not, either.
                if (vnToLabel->Lookup(nodeVN, &num))
                {
                    printf("Node: ");
                    Compiler::printTreeID(node);
                    printf(", " FMT_VN " was declared in value number class %d,\n", nodeVN, tlAndN.m_num);
                    printf(
                        "but this value number has already been associated with a different value number class: %d.\n",
                        num);
                    assert(false);
                }
                // Add to both mappings.
                labelToVN->Set(tlAndN.m_num, nodeVN);
                vnToLabel->Set(nodeVN, tlAndN.m_num);
                if (verbose)
                {
                    printf("      added to hash tables.\n");
                }
            }
        }
    }
}

void Compiler::vnpPrint(ValueNumPair vnp, unsigned level)
{
    if (vnp.BothEqual())
    {
        vnPrint(vnp.GetLiberal(), level);
    }
    else
    {
        printf("<l:");
        vnPrint(vnp.GetLiberal(), level);
        printf(", c:");
        vnPrint(vnp.GetConservative(), level);
        printf(">");
    }
}

void Compiler::vnPrint(ValueNum vn, unsigned level)
{
    if (ValueNumStore::isReservedVN(vn))
    {
        printf(ValueNumStore::reservedName(vn));
    }
    else
    {
        printf(FMT_VN, vn);
        if (level > 0)
        {
            vnStore->vnDump(this, vn);
        }
    }
}

#endif // DEBUG

// Methods of ValueNumPair.
ValueNumPair::ValueNumPair() : m_liberal(ValueNumStore::NoVN), m_conservative(ValueNumStore::NoVN)
{
}

bool ValueNumPair::BothDefined() const
{
    return (m_liberal != ValueNumStore::NoVN) && (m_conservative != ValueNumStore::NoVN);
}
