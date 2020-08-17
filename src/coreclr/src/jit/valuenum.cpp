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
    //    ARM32/ARM64 (0x7fc00000).

    static float NaN()
    {
#if defined(TARGET_XARCH)
        unsigned bits = 0xFFC00000u;
#elif defined(TARGET_ARMARCH)
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
    //    ARM32/ARM64 (0x7ff8000000000000).

    static double NaN()
    {
#if defined(TARGET_XARCH)
        unsigned long long bits = 0xFFF8000000000000ull;
#elif defined(TARGET_ARMARCH)
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
#ifdef TARGET_ARMARCH
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
#endif // TARGET_ARMARCH

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
#ifdef TARGET_ARMARCH
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
#endif // TARGET_ARMARCH

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
#ifdef TARGET_ARMARCH
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
#endif // TARGET_ARMARCH

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
#ifdef TARGET_ARMARCH
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
#endif // TARGET_ARMARCH

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

#ifdef FEATURE_SIMD
        case GT_SIMD:
            return VNFunc(VNF_SIMD_FIRST + node->AsSIMD()->gtSIMDIntrinsicID);
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            return VNFunc(VNF_HWI_FIRST + (node->AsHWIntrinsic()->gtHWIntrinsicId - NI_HW_INTRINSIC_START - 1));
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
    , m_floatCnsMap(nullptr)
    , m_doubleCnsMap(nullptr)
    , m_byrefCnsMap(nullptr)
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
        for (unsigned j = CEA_None; j <= CEA_Count + MAX_LOOP_NUM; j++)
        {
            m_curAllocChunk[i][j] = NoChunk;
        }
    }

    for (unsigned i = 0; i < SmallIntConstNum; i++)
    {
        m_VNsForSmallIntConsts[i] = NoVN;
    }
    // We will reserve chunk 0 to hold some special constants, like the constant NULL, the "exception" value, and the
    // "zero map."
    Chunk* specialConstChunk = new (m_alloc) Chunk(m_alloc, &m_nextChunkBase, TYP_REF, CEA_Const, MAX_LOOP_NUM);
    specialConstChunk->m_numUsed +=
        SRC_NumSpecialRefConsts; // Implicitly allocate 0 ==> NULL, and 1 ==> Exception, 2 ==> ZeroMap.
    ChunkNum cn = m_chunks.Push(specialConstChunk);
    assert(cn == 0);

    m_mapSelectBudget = (int)JitConfig.JitVNMapSelBudget(); // We cast the unsigned DWORD to a signed int.

    // This value must be non-negative and non-zero, reset the value to DEFAULT_MAP_SELECT_BUDGET if it isn't.
    if (m_mapSelectBudget <= 0)
    {
        m_mapSelectBudget = DEFAULT_MAP_SELECT_BUDGET;
    }
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
            // always returns true
            return false;
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
    return VNForFunc(TYP_REF, VNF_ExcSetCons, x, VNForEmptyExcSet());
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
//                   - or whne we have an empty list remaining.
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
            res = VNForFunc(TYP_REF, VNF_ExcSetCons, funcXs0.m_args[0], VNExcSetUnion(funcXs0.m_args[1], xs1));
        }
        else if (funcXs0.m_args[0] == funcXs1.m_args[0])
        {
            assert(VNCheckAscending(funcXs0.m_args[0], funcXs0.m_args[1]));
            assert(VNCheckAscending(funcXs1.m_args[0], funcXs1.m_args[1]));

            // Equal elements; add one (from xs0) to the result, advance both sets
            res = VNForFunc(TYP_REF, VNF_ExcSetCons, funcXs0.m_args[0],
                            VNExcSetUnion(funcXs0.m_args[1], funcXs1.m_args[1]));
        }
        else
        {
            assert(funcXs0.m_args[0] > funcXs1.m_args[0]);
            assert(VNCheckAscending(funcXs1.m_args[0], funcXs1.m_args[1]));

            // add the lower one (from xs1) to the result, advance xs1
            res = VNForFunc(TYP_REF, VNF_ExcSetCons, funcXs1.m_args[0], VNExcSetUnion(xs0, funcXs1.m_args[1]));
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
// Note: When 'vnWx' does not have an exception set, the orginal value is the
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
//                 This can be the orginal 'vn', when there are no exceptions.
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
//                 This can be the orginal 'vn', when there are no exceptions.
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
        return VNForFunc(TypeOfVN(vnNorm), VNF_ValWithExc, vnNorm, VNExcSetUnion(vnX, excSet));
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

ValueNumStore::Chunk::Chunk(CompAllocator          alloc,
                            ValueNum*              pNextBaseVN,
                            var_types              typ,
                            ChunkExtraAttribs      attribs,
                            BasicBlock::loopNumber loopNum)
    : m_defs(nullptr), m_numUsed(0), m_baseVN(*pNextBaseVN), m_typ(typ), m_attribs(attribs), m_loopNum(loopNum)
{
    // Allocate "m_defs" here, according to the typ/attribs pair.
    switch (attribs)
    {
        case CEA_None:
        case CEA_NotAField:
            break; // Nothing to do.
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
            m_defs = new (alloc) VNDefFunc1Arg[ChunkSize];
            break;
        case CEA_Func2:
            m_defs = new (alloc) VNDefFunc2Arg[ChunkSize];
            break;
        case CEA_Func3:
            m_defs = new (alloc) VNDefFunc3Arg[ChunkSize];
            break;
        case CEA_Func4:
            m_defs = new (alloc) VNDefFunc4Arg[ChunkSize];
            break;
        default:
            unreached();
    }
    *pNextBaseVN += ChunkSize;
}

ValueNumStore::Chunk* ValueNumStore::GetAllocChunk(var_types              typ,
                                                   ChunkExtraAttribs      attribs,
                                                   BasicBlock::loopNumber loopNum)
{
    Chunk*   res;
    unsigned index;
    if (loopNum == MAX_LOOP_NUM)
    {
        // Loop nest is unknown/irrelevant for this VN.
        index = attribs;
    }
    else
    {
        // Loop nest is interesting.  Since we know this is only true for unique VNs, we know attribs will
        // be CEA_None and can just index based on loop number.
        noway_assert(attribs == CEA_None);
        // Map NOT_IN_LOOP -> MAX_LOOP_NUM to make the index range contiguous [0..MAX_LOOP_NUM]
        index = CEA_Count + (loopNum == BasicBlock::NOT_IN_LOOP ? MAX_LOOP_NUM : loopNum);
    }
    ChunkNum cn = m_curAllocChunk[typ][index];
    if (cn != NoChunk)
    {
        res = m_chunks.Get(cn);
        if (res->m_numUsed < ChunkSize)
        {
            return res;
        }
    }
    // Otherwise, must allocate a new one.
    res                         = new (m_alloc) Chunk(m_alloc, &m_nextChunkBase, typ, attribs, loopNum);
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

ValueNum ValueNumStore::VNForByrefCon(size_t cnsVal)
{
    return VnForConst(cnsVal, GetByrefCnsMap(), TYP_BYREF);
}

ValueNum ValueNumStore::VNForCastOper(var_types castToType, bool srcIsUnsigned /*=false*/)
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

#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("    VNForCastOper(%s%s) is " FMT_VN "\n", varTypeName(castToType), srcIsUnsigned ? ", unsignedSrc" : "",
               result);
    }
#endif

    return result;
}

ValueNum ValueNumStore::VNForHandle(ssize_t cnsVal, unsigned handleFlags)
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
        Chunk*   c                                                = GetAllocChunk(TYP_I_IMPL, CEA_Handle);
        unsigned offsetWithinChunk                                = c->AllocVN();
        res                                                       = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<VNHandle*>(c->m_defs)[offsetWithinChunk] = handle;
        GetHandleMap()->Set(handle, res);
        return res;
    }
}

// Returns the value number for zero of the given "typ".
// It has an unreached() for a "typ" that has no zero value, such as TYP_VOID.
ValueNum ValueNumStore::VNZeroForType(var_types typ)
{
    switch (typ)
    {
        case TYP_BOOL:
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
        case TYP_STRUCT:
            return VNForZeroMap(); // Recursion!

#ifdef FEATURE_SIMD
        case TYP_SIMD8:
        case TYP_SIMD12:
        case TYP_SIMD16:
        case TYP_SIMD32:
            return VNForLongCon(0);
#endif // FEATURE_SIMD

        // These should be unreached.
        default:
            unreached(); // Should handle all types.
    }
}

// Returns the value number for one of the given "typ".
// It returns NoVN for a "typ" that has no one value, such as TYP_REF.
ValueNum ValueNumStore::VNOneForType(var_types typ)
{
    switch (typ)
    {
        case TYP_BOOL:
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
            return NoVN;
    }
}

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
    assert(func != VNF_NotAField);

    ValueNum resultVN;

    // Have we already assigned a ValueNum for 'func' ?
    //
    if (!GetVNFunc0Map()->Lookup(func, &resultVN))
    {
        // Allocate a new ValueNum for 'func'
        Chunk*   c                                              = GetAllocChunk(typ, CEA_Func0);
        unsigned offsetWithinChunk                              = c->AllocVN();
        resultVN                                                = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<VNFunc*>(c->m_defs)[offsetWithinChunk] = func;
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
    assert(arg0VN == VNNormalValue(arg0VN)); // Arguments don't carry exceptions.

    // Try to perform constant-folding.
    if (CanEvalForConstantArgs(func) && IsVNConstant(arg0VN))
    {
        return EvalFuncForConstantArgs(typ, func, arg0VN);
    }

    ValueNum resultVN;

    // Have we already assigned a ValueNum for 'func'('arg0VN') ?
    //
    VNDefFunc1Arg fstruct(func, arg0VN);
    if (!GetVNFunc1Map()->Lookup(fstruct, &resultVN))
    {
        // Otherwise, Allocate a new ValueNum for 'func'('arg0VN')
        //
        Chunk*   c                                                     = GetAllocChunk(typ, CEA_Func1);
        unsigned offsetWithinChunk                                     = c->AllocVN();
        resultVN                                                       = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<VNDefFunc1Arg*>(c->m_defs)[offsetWithinChunk] = fstruct;
        // Record 'resultVN' in the Func1Map
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
    assert(VNFuncArity(func) == 2);
    assert(func != VNF_MapSelect); // Precondition: use the special function VNForMapSelect defined for that.

    ValueNum resultVN;

    // When both operands are constants we can usually perform constant-folding.
    //
    if (CanEvalForConstantArgs(func) && IsVNConstant(arg0VN) && IsVNConstant(arg1VN))
    {
        bool canFold = true; // Normally we will be able to fold this 'func'

        // Special case for VNF_Cast of constant handles
        // Don't allow an eval/fold of a GT_CAST(non-I_IMPL, Handle)
        //
        if ((func == VNF_Cast) && (typ != TYP_I_IMPL) && IsVNHandle(arg0VN))
        {
            canFold = false;
        }

        // Currently CanEvalForConstantArgs() returns false for VNF_CastOvf
        // In the future we could handle this case in folding.
        assert(func != VNF_CastOvf);

        // It is possible for us to have mismatched types (see Bug 750863)
        // We don't try to fold a binary operation when one of the constant operands
        // is a floating-point constant and the other is not.
        //
        var_types arg0VNtyp      = TypeOfVN(arg0VN);
        bool      arg0IsFloating = varTypeIsFloating(arg0VNtyp);

        var_types arg1VNtyp      = TypeOfVN(arg1VN);
        bool      arg1IsFloating = varTypeIsFloating(arg1VNtyp);

        if (arg0IsFloating != arg1IsFloating)
        {
            canFold = false;
        }

        // NaNs are unordered wrt to other floats. While an ordered
        // comparison would return false, an unordered comparison
        // will return true if any operands are a NaN. We only perform
        // ordered NaN comparison in EvalComparison.
        if ((arg0IsFloating && (((arg0VNtyp == TYP_FLOAT) && _isnanf(GetConstantSingle(arg0VN))) ||
                                ((arg0VNtyp == TYP_DOUBLE) && _isnan(GetConstantDouble(arg0VN))))) ||
            (arg1IsFloating && (((arg1VNtyp == TYP_FLOAT) && _isnanf(GetConstantSingle(arg1VN))) ||
                                ((arg1VNtyp == TYP_DOUBLE) && _isnan(GetConstantDouble(arg1VN))))))
        {
            canFold = false;
        }

        if (typ == TYP_BYREF)
        {
            // We don't want to fold expressions that produce TYP_BYREF
            canFold = false;
        }

        bool shouldFold = canFold;

        if (canFold)
        {
            // We can fold the expression, but we don't want to fold
            // when the expression will always throw an exception
            shouldFold = VNEvalShouldFold(typ, func, arg0VN, arg1VN);
        }

        if (shouldFold)
        {
            return EvalFuncForConstantArgs(typ, func, arg0VN, arg1VN);
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
    VNDefFunc2Arg fstruct(func, arg0VN, arg1VN);
    if (!GetVNFunc2Map()->Lookup(fstruct, &resultVN))
    {
        if (func == VNF_CastClass)
        {
            // In terms of values, a castclass always returns its second argument, the object being cast.
            // The operation may also throw an exception
            ValueNum vnExcSet = VNExcSetSingleton(VNForFunc(TYP_REF, VNF_InvalidCastExc, arg1VN, arg0VN));
            resultVN          = VNWithExc(arg1VN, vnExcSet);
        }
        else
        {
            resultVN = EvalUsingMathIdentity(typ, func, arg0VN, arg1VN);

            // Do we have a valid resultVN?
            if ((resultVN == NoVN) || (TypeOfVN(resultVN) != typ))
            {
                // Otherwise, Allocate a new ValueNum for 'func'('arg0VN','arg1VN')
                //
                Chunk*   c                                                     = GetAllocChunk(typ, CEA_Func2);
                unsigned offsetWithinChunk                                     = c->AllocVN();
                resultVN                                                       = c->m_baseVN + offsetWithinChunk;
                reinterpret_cast<VNDefFunc2Arg*>(c->m_defs)[offsetWithinChunk] = fstruct;
                // Record 'resultVN' in the Func2Map
                GetVNFunc2Map()->Set(fstruct, resultVN);
            }
        }
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
    assert(VNFuncArity(func) == 3);

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
    assert(VNFuncArity(func) == 3);

    ValueNum resultVN;

    // Have we already assigned a ValueNum for 'func'('arg0VN','arg1VN','arg2VN') ?
    //
    VNDefFunc3Arg fstruct(func, arg0VN, arg1VN, arg2VN);
    if (!GetVNFunc3Map()->Lookup(fstruct, &resultVN))
    {
        // Otherwise, Allocate a new ValueNum for 'func'('arg0VN','arg1VN','arg2VN')
        //
        Chunk*   c                                                     = GetAllocChunk(typ, CEA_Func3);
        unsigned offsetWithinChunk                                     = c->AllocVN();
        resultVN                                                       = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<VNDefFunc3Arg*>(c->m_defs)[offsetWithinChunk] = fstruct;
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
// Note:   Currently the only four operand func is the VNF_PtrToArrElem operation
//
ValueNum ValueNumStore::VNForFunc(
    var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN, ValueNum arg2VN, ValueNum arg3VN)
{
    assert(arg0VN != NoVN && arg1VN != NoVN && arg2VN != NoVN && arg3VN != NoVN);

    // Function arguments carry no exceptions.
    assert(arg0VN == VNNormalValue(arg0VN));
    assert(arg1VN == VNNormalValue(arg1VN));
    assert(arg2VN == VNNormalValue(arg2VN));
    assert(arg3VN == VNNormalValue(arg3VN));
    assert(VNFuncArity(func) == 4);

    ValueNum resultVN;

    // Have we already assigned a ValueNum for 'func'('arg0VN','arg1VN','arg2VN','arg3VN') ?
    //
    VNDefFunc4Arg fstruct(func, arg0VN, arg1VN, arg2VN, arg3VN);
    if (!GetVNFunc4Map()->Lookup(fstruct, &resultVN))
    {
        // Otherwise, Allocate a new ValueNum for 'func'('arg0VN','arg1VN','arg2VN','arg3VN')
        //
        Chunk*   c                                                     = GetAllocChunk(typ, CEA_Func4);
        unsigned offsetWithinChunk                                     = c->AllocVN();
        resultVN                                                       = c->m_baseVN + offsetWithinChunk;
        reinterpret_cast<VNDefFunc4Arg*>(c->m_defs)[offsetWithinChunk] = fstruct;
        // Record 'resultVN' in the Func4Map
        GetVNFunc4Map()->Set(fstruct, resultVN);
    }
    return resultVN;
}

//------------------------------------------------------------------------------
// VNForMapStore : Evaluate VNF_MapStore with the given arguments.
//
//
// Arguments:
//    typ  -    Value type
//    arg0VN  - Map value number
//    arg1VN  - Index value number
//    arg2VN  - New value for map[index]
//
// Return Value:
//    Value number for the result of the evaluation.

ValueNum ValueNumStore::VNForMapStore(var_types typ, ValueNum arg0VN, ValueNum arg1VN, ValueNum arg2VN)
{
    ValueNum result = VNForFunc(typ, VNF_MapStore, arg0VN, arg1VN, arg2VN);
#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("    VNForMapStore(" FMT_VN ", " FMT_VN ", " FMT_VN "):%s returns ", arg0VN, arg1VN, arg2VN,
               varTypeName(typ));
        m_pComp->vnPrint(result, 1);
        printf("\n");
    }
#endif
    return result;
}

//------------------------------------------------------------------------------
// VNForMapSelect : Evaluate VNF_MapSelect with the given arguments.
//
//
// Arguments:
//    vnk  -    Value number kind
//    typ  -    Value type
//    arg0VN  - Map value number
//    arg1VN  - Index value number
//
// Return Value:
//    Value number for the result of the evaluation.
//
// Notes:
//    This requires a "ValueNumKind" because it will attempt, given "select(phi(m1, ..., mk), ind)", to evaluate
//    "select(m1, ind)", ..., "select(mk, ind)" to see if they agree.  It needs to know which kind of value number
//    (liberal/conservative) to read from the SSA def referenced in the phi argument.

ValueNum ValueNumStore::VNForMapSelect(ValueNumKind vnk, var_types typ, ValueNum arg0VN, ValueNum arg1VN)
{
    int      budget          = m_mapSelectBudget;
    bool     usedRecursiveVN = false;
    ValueNum result          = VNForMapSelectWork(vnk, typ, arg0VN, arg1VN, &budget, &usedRecursiveVN);

    // The remaining budget should always be between [0..m_mapSelectBudget]
    assert((budget >= 0) && (budget <= m_mapSelectBudget));

#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("    VNForMapSelect(" FMT_VN ", " FMT_VN "):%s returns ", arg0VN, arg1VN, varTypeName(typ));
        m_pComp->vnPrint(result, 1);
        printf("\n");
    }
#endif
    return result;
}

//------------------------------------------------------------------------------
// VNForMapSelectWork : A method that does the work for VNForMapSelect and may call itself recursively.
//
//
// Arguments:
//    vnk  -             Value number kind
//    typ  -             Value type
//    arg0VN  -          Zeroth argument
//    arg1VN  -          First argument
//    pBudget -          Remaining budget for the outer evaluation
//    pUsedRecursiveVN - Out-parameter that is set to true iff RecursiveVN was returned from this method
//                       or from a method called during one of recursive invocations.
//
// Return Value:
//    Value number for the result of the evaluation.
//
// Notes:
//    This requires a "ValueNumKind" because it will attempt, given "select(phi(m1, ..., mk), ind)", to evaluate
//    "select(m1, ind)", ..., "select(mk, ind)" to see if they agree.  It needs to know which kind of value number
//    (liberal/conservative) to read from the SSA def referenced in the phi argument.

ValueNum ValueNumStore::VNForMapSelectWork(
    ValueNumKind vnk, var_types typ, ValueNum arg0VN, ValueNum arg1VN, int* pBudget, bool* pUsedRecursiveVN)
{
TailCall:
    // This label allows us to directly implement a tail call by setting up the arguments, and doing a goto to here.
    assert(arg0VN != NoVN && arg1VN != NoVN);
    assert(arg0VN == VNNormalValue(arg0VN)); // Arguments carry no exceptions.
    assert(arg1VN == VNNormalValue(arg1VN)); // Arguments carry no exceptions.

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
    ValueNum res;

    VNDefFunc2Arg fstruct(VNF_MapSelect, arg0VN, arg1VN);
    if (GetVNFunc2Map()->Lookup(fstruct, &res))
    {
        return res;
    }
    else
    {
        // Give up if we've run out of budget.
        if (--(*pBudget) <= 0)
        {
            // We have to use 'nullptr' for the basic block here, because subsequent expressions
            // in different blocks may find this result in the VNFunc2Map -- other expressions in
            // the IR may "evaluate" to this same VNForExpr, so it is not "unique" in the sense
            // that permits the BasicBlock attribution.
            res = VNForExpr(nullptr, typ);
            GetVNFunc2Map()->Set(fstruct, res);
            return res;
        }

        // If it's recursive, stop the recursion.
        if (SelectIsBeingEvaluatedRecursively(arg0VN, arg1VN))
        {
            *pUsedRecursiveVN = true;
            return RecursiveVN;
        }

        if (arg0VN == VNForZeroMap())
        {
            return VNZeroForType(typ);
        }
        else if (IsVNFunc(arg0VN))
        {
            VNFuncApp funcApp;
            GetVNFunc(arg0VN, &funcApp);
            if (funcApp.m_func == VNF_MapStore)
            {
                // select(store(m, i, v), i) == v
                if (funcApp.m_args[1] == arg1VN)
                {
#if FEATURE_VN_TRACE_APPLY_SELECTORS
                    JITDUMP("      AX1: select([" FMT_VN "]store(" FMT_VN ", " FMT_VN ", " FMT_VN "), " FMT_VN
                            ") ==> " FMT_VN ".\n",
                            funcApp.m_args[0], arg0VN, funcApp.m_args[1], funcApp.m_args[2], arg1VN, funcApp.m_args[2]);
#endif
                    return funcApp.m_args[2];
                }
                // i # j ==> select(store(m, i, v), j) == select(m, j)
                // Currently the only source of distinctions is when both indices are constants.
                else if (IsVNConstant(arg1VN) && IsVNConstant(funcApp.m_args[1]))
                {
                    assert(funcApp.m_args[1] != arg1VN); // we already checked this above.
#if FEATURE_VN_TRACE_APPLY_SELECTORS
                    JITDUMP("      AX2: " FMT_VN " != " FMT_VN " ==> select([" FMT_VN "]store(" FMT_VN ", " FMT_VN
                            ", " FMT_VN "), " FMT_VN ") ==> select(" FMT_VN ", " FMT_VN ").\n",
                            arg1VN, funcApp.m_args[1], arg0VN, funcApp.m_args[0], funcApp.m_args[1], funcApp.m_args[2],
                            arg1VN, funcApp.m_args[0], arg1VN);
#endif
                    // This is the equivalent of the recursive tail call:
                    // return VNForMapSelect(vnk, typ, funcApp.m_args[0], arg1VN);
                    // Make sure we capture any exceptions from the "i" and "v" of the store...
                    arg0VN = funcApp.m_args[0];
                    goto TailCall;
                }
            }
            else if (funcApp.m_func == VNF_PhiDef || funcApp.m_func == VNF_PhiMemoryDef)
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
                    m_fixedPointMapSels.Push(VNDefFunc2Arg(VNF_MapSelect, arg0VN, arg1VN));

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
                        bool     allSame = true;
                        ValueNum argRest = phiFuncApp.m_args[1];
                        ValueNum sameSelResult =
                            VNForMapSelectWork(vnk, typ, phiArgVN, arg1VN, pBudget, pUsedRecursiveVN);

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
                                ValueNum curResult =
                                    VNForMapSelectWork(vnk, typ, phiArgVN, arg1VN, pBudget, &usedRecursiveVN);
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
                            assert(FixedPointMapSelsTopHasValue(arg0VN, arg1VN));
                            m_fixedPointMapSels.Pop();

                            // To avoid exponential searches, we make sure that this result is memo-ized.
                            // The result is always valid for memoization if we didn't rely on RecursiveVN to get it.
                            // If RecursiveVN was used, we are processing a loop and we can't memo-ize this intermediate
                            // result if, e.g., this block is in a multi-entry loop.
                            if (!*pUsedRecursiveVN)
                            {
                                GetVNFunc2Map()->Set(fstruct, sameSelResult);
                            }

                            return sameSelResult;
                        }
                        // Otherwise, fall through to creating the select(phi(m1, m2), x) function application.
                    }
                    // Make sure we're popping what we pushed.
                    assert(FixedPointMapSelsTopHasValue(arg0VN, arg1VN));
                    m_fixedPointMapSels.Pop();
                }
            }
        }

        // We may have run out of budget and already assigned a result
        if (!GetVNFunc2Map()->Lookup(fstruct, &res))
        {
            // Otherwise, assign a new VN for the function application.
            Chunk*   c                                                     = GetAllocChunk(typ, CEA_Func2);
            unsigned offsetWithinChunk                                     = c->AllocVN();
            res                                                            = c->m_baseVN + offsetWithinChunk;
            reinterpret_cast<VNDefFunc2Arg*>(c->m_defs)[offsetWithinChunk] = fstruct;
            GetVNFunc2Map()->Set(fstruct, res);
        }
        return res;
    }
}

ValueNum ValueNumStore::EvalFuncForConstantArgs(var_types typ, VNFunc func, ValueNum arg0VN)
{
    assert(CanEvalForConstantArgs(func));
    assert(IsVNConstant(arg0VN));
    switch (TypeOfVN(arg0VN))
    {
        case TYP_INT:
        {
            int resVal = EvalOp<int>(func, ConstantValue<int>(arg0VN));
            // Unary op on a handle results in a handle.
            return IsVNHandle(arg0VN) ? VNForHandle(ssize_t(resVal), GetHandleFlags(arg0VN)) : VNForIntCon(resVal);
        }
        case TYP_LONG:
        {
            INT64 resVal = EvalOp<INT64>(func, ConstantValue<INT64>(arg0VN));
            // Unary op on a handle results in a handle.
            return IsVNHandle(arg0VN) ? VNForHandle(ssize_t(resVal), GetHandleFlags(arg0VN)) : VNForLongCon(resVal);
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
            assert(arg0VN == VNForNull());         // Only other REF constant.
            assert(func == VNFunc(GT_ARR_LENGTH)); // Only function we can apply to a REF constant!
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
        VNDefFunc2Arg& elem = m_fixedPointMapSels.GetRef(i);
        assert(elem.m_func == VNF_MapSelect);
        if (elem.m_arg0 == map && elem.m_arg1 == ind)
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
    VNDefFunc2Arg& top = m_fixedPointMapSels.TopRef();
    return top.m_func == VNF_MapSelect && top.m_arg0 == map && top.m_arg1 == index;
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

// Compute the proper value number when the VNFunc has all constant arguments
// This essentially performs constant folding at value numbering time
//
ValueNum ValueNumStore::EvalFuncForConstantArgs(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    assert(CanEvalForConstantArgs(func));
    assert(IsVNConstant(arg0VN) && IsVNConstant(arg1VN));
    assert(!VNHasExc(arg0VN) && !VNHasExc(arg1VN)); // Otherwise, would not be constant.

    // if our func is the VNF_Cast operation we handle it first
    if (func == VNF_Cast)
    {
        return EvalCastForConstantArgs(typ, func, arg0VN, arg1VN);
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
                    result = VNForHandle(ssize_t(resultVal), GetHandleFlags(handleVN)); // Use VN for Handle
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
                    result = VNForHandle(ssize_t(resultVal), GetHandleFlags(handleVN)); // Use VN for Handle
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
                assert((typ == TYP_BYREF) || (typ == TYP_LONG));
                size_t resultVal = EvalOp<size_t>(func, arg0Val, arg1Val);
                result           = VNForByrefCon(resultVal);
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
                    result = VNForByrefCon((size_t)resultVal);
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
    assert(CanEvalForConstantArgs(func));
    assert(IsVNConstant(arg0VN) && IsVNConstant(arg1VN));

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
    assert(func == VNF_Cast);
    assert(IsVNConstant(arg0VN) && IsVNConstant(arg1VN));

    // Stack-normalize the result type.
    if (varTypeIsSmall(typ))
    {
        typ = TYP_INT;
    }

    var_types arg0VNtyp = TypeOfVN(arg0VN);
    var_types arg1VNtyp = TypeOfVN(arg1VN);

    // arg1VN is really the gtCastType that we are casting to
    assert(arg1VNtyp == TYP_INT);
    int arg1Val = ConstantValue<int>(arg1VN);
    assert(arg1Val >= 0);

    if (IsVNHandle(arg0VN))
    {
        // We don't allow handles to be cast to random var_types.
        assert(typ == TYP_I_IMPL);
    }

    // We previously encoded the castToType operation using vnForCastOper()
    //
    bool      srcIsUnsigned = ((arg1Val & INT32(VCA_UnsignedSrc)) != 0);
    var_types castToType    = var_types(arg1Val >> INT32(VCA_BitCount));

    var_types castFromType = arg0VNtyp;

    switch (castFromType) // GT_CAST source type
    {
#ifndef TARGET_64BIT
        case TYP_REF:
        case TYP_BYREF:
#endif
        case TYP_INT:
        {
            int arg0Val = GetConstantInt32(arg0VN);

            switch (castToType)
            {
                case TYP_BYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT8(arg0Val));
                case TYP_BOOL:
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
                        return VNForByrefCon(size_t(arg0Val));
                    }
#else // TARGET_32BIT
                    if (srcIsUnsigned)
                        return VNForLongCon(INT64(unsigned(arg0Val)));
                    else
                        return VNForLongCon(INT64(arg0Val));
#endif
                case TYP_BYREF:
                    assert(typ == TYP_BYREF);
                    return VNForByrefCon(size_t(arg0Val));

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
            {
#ifdef TARGET_64BIT
                case TYP_REF:
                case TYP_BYREF:
#endif
                case TYP_LONG:
                    INT64 arg0Val = GetConstantInt64(arg0VN);

                    switch (castToType)
                    {
                        case TYP_BYTE:
                            assert(typ == TYP_INT);
                            return VNForIntCon(INT8(arg0Val));
                        case TYP_BOOL:
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
                            return VNForByrefCon((size_t)arg0Val);
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

            switch (castToType)
            {
                case TYP_BYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT8(arg0Val));
                case TYP_BOOL:
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

            switch (castToType)
            {
                case TYP_BYTE:
                    assert(typ == TYP_INT);
                    return VNForIntCon(INT8(arg0Val));
                case TYP_BOOL:
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

//-----------------------------------------------------------------------------------
// CanEvalForConstantArgs:  - Given a VNFunc value return true when we can perform
//                            compile-time constant folding for the operation.
//
// Arguments:
//    vnf        - The VNFunc that we are inquiring about
//
// Return Value:
//               - Returns true if we can always compute a constant result
//                 when given all constant args.
//
// Notes:        - When this method returns true, the logic to compute the
//                 compile-time result must also be added to EvalOP,
//                 EvalOpspecialized or EvalComparison
//
bool ValueNumStore::CanEvalForConstantArgs(VNFunc vnf)
{
    if (vnf < VNF_Boundary)
    {
        genTreeOps oper = genTreeOps(vnf);

        switch (oper)
        {
            // Only return true for the node kinds that have code that supports
            // them in EvalOP, EvalOpspecialized or EvalComparison

            // Unary Ops
            case GT_NEG:
            case GT_NOT:
            case GT_BSWAP16:
            case GT_BSWAP:

            // Binary Ops
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

            // Equality Ops
            case GT_EQ:
            case GT_NE:
            case GT_GT:
            case GT_GE:
            case GT_LT:
            case GT_LE:

                // We can evaluate these.
                return true;

            default:
                // We can not evaluate these.
                return false;
        }
    }
    else
    {
        // some VNF_ that we can evaluate
        switch (vnf)
        {
            // Consider adding:
            //   case VNF_GT_UN:
            //   case VNF_GE_UN:
            //   case VNF_LT_UN:
            //   case VNF_LE_UN:
            //

            case VNF_Cast:
                // We can evaluate these.
                return true;

            default:
                // We can not evaluate these.
                return false;
        }
    }
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
bool ValueNumStore::VNEvalShouldFold(var_types typ, VNFunc func, ValueNum arg0VN, ValueNum arg1VN)
{
    bool shouldFold = true;

    // We have some arithmetic operations that will always throw
    // an exception given particular constant argument(s).
    // (i.e. integer division by zero)
    //
    // We will avoid performing any constant folding on them
    // since they won't actually produce any result.
    // Instead they always will throw an exception.
    //
    if (func < VNF_Boundary)
    {
        genTreeOps oper = genTreeOps(func);

        // Floating point operations do not throw exceptions
        //
        if (!varTypeIsFloating(typ))
        {
            // Is this an integer divide/modulo that will always throw an exception?
            //
            if ((oper == GT_DIV) || (oper == GT_UDIV) || (oper == GT_MOD) || (oper == GT_UMOD))
            {
                if ((TypeOfVN(arg0VN) != typ) || (TypeOfVN(arg1VN) != typ))
                {
                    // Just in case we have mismatched types
                    shouldFold = false;
                }
                else
                {
                    bool isUnsigned = (oper == GT_UDIV) || (oper == GT_UMOD);
                    if (typ == TYP_LONG)
                    {
                        INT64 kArg0 = ConstantValue<INT64>(arg0VN);
                        INT64 kArg1 = ConstantValue<INT64>(arg1VN);

                        if (IsIntZero(kArg1))
                        {
                            // Don't fold, we have a divide by zero
                            shouldFold = false;
                        }
                        else if (!isUnsigned || IsOverflowIntDiv(kArg0, kArg1))
                        {
                            // Don't fold, we have a divide of INT64_MIN/-1
                            shouldFold = false;
                        }
                    }
                    else if (typ == TYP_INT)
                    {
                        int kArg0 = ConstantValue<int>(arg0VN);
                        int kArg1 = ConstantValue<int>(arg1VN);

                        if (IsIntZero(kArg1))
                        {
                            // Don't fold, we have a divide by zero
                            shouldFold = false;
                        }
                        else if (!isUnsigned && IsOverflowIntDiv(kArg0, kArg1))
                        {
                            // Don't fold, we have a divide of INT32_MIN/-1
                            shouldFold = false;
                        }
                    }
                    else // strange value for 'typ'
                    {
                        assert(!"unexpected 'typ' in VNForFunc constant folding");
                        shouldFold = false;
                    }
                }
            }
        }
    }
    else // (func > VNF_Boundary)
    {
        // OK to fold,
        // Add checks in the future if we support folding of VNF_ADD_OVF, etc...
    }

    return shouldFold;
}

//----------------------------------------------------------------------------------------
//  EvalUsingMathIdentity
//                   - Attempts to evaluate 'func' by using mathimatical identities
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

    // We have ways of evaluating some binary functions.
    if (func < VNF_Boundary)
    {
        switch (genTreeOps(func))
        {
            ValueNum ZeroVN;
            ValueNum OneVN;

            case GT_ADD:
                // (0 + x) == x
                // (x + 0) == x
                // This identity does not apply for floating point (when x == -0.0)
                //
                if (!varTypeIsFloating(typ))
                {
                    ZeroVN = VNZeroForType(typ);
                    if (arg0VN == ZeroVN)
                    {
                        resultVN = arg1VN;
                    }
                    else if (arg1VN == ZeroVN)
                    {
                        resultVN = arg0VN;
                    }
                }
                break;

            case GT_SUB:
                // (x - 0) == x
                // (x - x) == 0
                // This identity does not apply for floating point (when x == -0.0)
                //
                if (!varTypeIsFloating(typ))
                {
                    ZeroVN = VNZeroForType(typ);
                    if (arg1VN == ZeroVN)
                    {
                        resultVN = arg0VN;
                    }
                    else if (arg0VN == arg1VN)
                    {
                        resultVN = ZeroVN;
                    }
                }
                break;

            case GT_MUL:
                // These identities do not apply for floating point
                //
                if (!varTypeIsFloating(typ))
                {
                    // (0 * x) == 0
                    // (x * 0) == 0
                    ZeroVN = VNZeroForType(typ);
                    if (arg0VN == ZeroVN)
                    {
                        resultVN = ZeroVN;
                    }
                    else if (arg1VN == ZeroVN)
                    {
                        resultVN = ZeroVN;
                    }

                    // (x * 1) == x
                    // (1 * x) == x
                    OneVN = VNOneForType(typ);
                    if (arg0VN == OneVN)
                    {
                        resultVN = arg1VN;
                    }
                    else if (arg1VN == OneVN)
                    {
                        resultVN = arg0VN;
                    }
                }
                break;

            case GT_DIV:
            case GT_UDIV:
                // (x / 1) == x
                // This identity does not apply for floating point
                //
                if (!varTypeIsFloating(typ))
                {
                    OneVN = VNOneForType(typ);
                    if (arg1VN == OneVN)
                    {
                        resultVN = arg0VN;
                    }
                }
                break;

            case GT_OR:
            case GT_XOR:
                // (0 | x) == x,  (0 ^ x) == x
                // (x | 0) == x,  (x ^ 0) == x
                ZeroVN = VNZeroForType(typ);
                if (arg0VN == ZeroVN)
                {
                    resultVN = arg1VN;
                }
                else if (arg1VN == ZeroVN)
                {
                    resultVN = arg0VN;
                }
                break;

            case GT_AND:
                // (x & 0) == 0
                // (0 & x) == 0
                ZeroVN = VNZeroForType(typ);
                if (arg0VN == ZeroVN)
                {
                    resultVN = ZeroVN;
                }
                else if (arg1VN == ZeroVN)
                {
                    resultVN = ZeroVN;
                }
                break;

            case GT_LSH:
            case GT_RSH:
            case GT_RSZ:
            case GT_ROL:
            case GT_ROR:
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

            case GT_EQ:
                // (null == non-null) == false
                // (non-null == null) == false
                if (((arg0VN == VNForNull()) && IsKnownNonNull(arg1VN)) ||
                    ((arg1VN == VNForNull()) && IsKnownNonNull(arg0VN)))
                {
                    resultVN = VNZeroForType(typ);
                    break;
                }
                // (x == x) == true (integer only)
                __fallthrough;
            case GT_GE:
            case GT_LE:
                // (x <= x) == true (integer only)
                // (x >= x) == true (integer only)
                if ((arg0VN == arg1VN) && varTypeIsIntegralOrI(TypeOfVN(arg0VN)))
                {
                    resultVN = VNOneForType(typ);
                }
                break;

            case GT_NE:
                // (null != non-null) == true
                // (non-null != null) == true
                if (((arg0VN == VNForNull()) && IsKnownNonNull(arg1VN)) ||
                    ((arg1VN == VNForNull()) && IsKnownNonNull(arg0VN)))
                {
                    resultVN = VNOneForType(typ);
                }
                // (x != x) == false (integer only)
                else if ((arg0VN == arg1VN) && varTypeIsIntegralOrI(TypeOfVN(arg0VN)))
                {
                    resultVN = VNZeroForType(typ);
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
                break;

            default:
                break;
        }
    }
    else // must be a VNF_ function
    {
        switch (func)
        {
            case VNF_LT_UN:
                // (x < 0) == false
                // (x < x) == false
                std::swap(arg0VN, arg1VN);
                __fallthrough;
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
                __fallthrough;
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
//     typ - Type of the expression in the IR
//
// Return Value:
//    A new value number distinct from any previously generated, that compares as equal
//    to itself, but not any other value number, and is annotated with the given
//    type and block.

ValueNum ValueNumStore::VNForExpr(BasicBlock* block, var_types typ)
{
    BasicBlock::loopNumber loopNum;
    if (block == nullptr)
    {
        loopNum = MAX_LOOP_NUM;
    }
    else
    {
        loopNum = block->bbNatLoopNum;
    }

    // We always allocate a new, unique VN in this call.
    // The 'typ' is used to partition the allocation of VNs into different chunks.
    Chunk*   c                 = GetAllocChunk(typ, CEA_None, loopNum);
    unsigned offsetWithinChunk = c->AllocVN();
    ValueNum result            = c->m_baseVN + offsetWithinChunk;
    return result;
}

ValueNum ValueNumStore::VNApplySelectors(ValueNumKind  vnk,
                                         ValueNum      map,
                                         FieldSeqNode* fieldSeq,
                                         size_t*       wbFinalStructSize)
{
    if (fieldSeq == nullptr)
    {
        return map;
    }
    else
    {
        assert(fieldSeq != FieldSeqStore::NotAField());

        // Skip any "FirstElem" pseudo-fields or any "ConstantIndex" pseudo-fields
        if (fieldSeq->IsPseudoField())
        {
            return VNApplySelectors(vnk, map, fieldSeq->m_next, wbFinalStructSize);
        }

        // Otherwise, is a real field handle.
        CORINFO_FIELD_HANDLE fldHnd    = fieldSeq->m_fieldHnd;
        CORINFO_CLASS_HANDLE structHnd = NO_CLASS_HANDLE;
        ValueNum             fldHndVN  = VNForHandle(ssize_t(fldHnd), GTF_ICON_FIELD_HDL);
        noway_assert(fldHnd != nullptr);
        CorInfoType fieldCit  = m_pComp->info.compCompHnd->getFieldType(fldHnd, &structHnd);
        var_types   fieldType = JITtype2varType(fieldCit);

        size_t structSize = 0;
        if (varTypeIsStruct(fieldType))
        {
            structSize = m_pComp->info.compCompHnd->getClassSize(structHnd);
            // We do not normalize the type field accesses during importation unless they
            // are used in a call, return or assignment.
            if ((fieldType == TYP_STRUCT) && m_pComp->structSizeMightRepresentSIMDType(structSize))
            {
                fieldType = m_pComp->impNormStructType(structHnd);
            }
        }
        if (wbFinalStructSize != nullptr)
        {
            *wbFinalStructSize = structSize;
        }

#ifdef DEBUG
        if (m_pComp->verbose)
        {
            printf("  VNApplySelectors:\n");
            const char* modName;
            const char* fldName = m_pComp->eeGetFieldName(fldHnd, &modName);
            printf("    VNForHandle(%s) is " FMT_VN ", fieldType is %s", fldName, fldHndVN, varTypeName(fieldType));
            if (varTypeIsStruct(fieldType))
            {
                printf(", size = %d", structSize);
            }
            printf("\n");
        }
#endif

        if (fieldSeq->m_next != nullptr)
        {
            ValueNum newMap = VNForMapSelect(vnk, fieldType, map, fldHndVN);
            return VNApplySelectors(vnk, newMap, fieldSeq->m_next, wbFinalStructSize);
        }
        else // end of fieldSeq
        {
            return VNForMapSelect(vnk, fieldType, map, fldHndVN);
        }
    }
}

ValueNum ValueNumStore::VNApplySelectorsTypeCheck(ValueNum elem, var_types indType, size_t elemStructSize)
{
    var_types elemTyp = TypeOfVN(elem);

    // Check if the elemTyp is matching/compatible

    if (indType != elemTyp)
    {
        // We are trying to read from an 'elem' of type 'elemType' using 'indType' read

        size_t elemTypSize = (elemTyp == TYP_STRUCT) ? elemStructSize : genTypeSize(elemTyp);
        size_t indTypeSize = genTypeSize(indType);

        if (indTypeSize > elemTypSize)
        {
            // Reading beyong the end of 'elem'

            // return a new unique value number
            elem = VNMakeNormalUnique(elem);

            JITDUMP("    *** Mismatched types in VNApplySelectorsTypeCheck (reading beyond the end)\n");
        }
        else if (varTypeIsStruct(indType))
        {
            // return a new unique value number
            elem = VNMakeNormalUnique(elem);

            JITDUMP("    *** Mismatched types in VNApplySelectorsTypeCheck (indType is TYP_STRUCT)\n");
        }
        else
        {
            // We are trying to read an 'elem' of type 'elemType' using 'indType' read

            // insert a cast of elem to 'indType'
            elem = VNForCast(elem, indType, elemTyp);
        }
    }

    return elem;
}

ValueNum ValueNumStore::VNApplySelectorsAssignTypeCoerce(ValueNum elem, var_types indType, BasicBlock* block)
{
    var_types elemTyp = TypeOfVN(elem);

    // Check if the elemTyp is matching/compatible

    if (indType != elemTyp)
    {
        bool isConstant = IsVNConstant(elem);
        if (isConstant && (elemTyp == genActualType(indType)))
        {
            // (i.e. We recorded a constant of TYP_INT for a TYP_BYTE field)
        }
        else
        {
            // We are trying to write an 'elem' of type 'elemType' using 'indType' store

            if (varTypeIsStruct(indType))
            {
                // return a new unique value number
                elem = VNMakeNormalUnique(elem);

                JITDUMP("    *** Mismatched types in VNApplySelectorsAssignTypeCoerce (indType is TYP_STRUCT)\n");
            }
            else
            {
                // We are trying to write an 'elem' of type 'elemType' using 'indType' store

                // insert a cast of elem to 'indType'
                elem = VNForCast(elem, indType, elemTyp);

                JITDUMP("    Cast to %s inserted in VNApplySelectorsAssignTypeCoerce (elemTyp is %s)\n",
                        varTypeName(indType), varTypeName(elemTyp));
            }
        }
    }
    return elem;
}

//------------------------------------------------------------------------
// VNApplySelectorsAssign: Compute the value number corresponding to "map" but with
//    the element at "fieldSeq" updated to have type "elem"; this is the new memory
//    value for an assignment of value "elem" into the memory at location "fieldSeq"
//    that occurs in block "block" and has type "indType" (so long as the selectors
//    into that memory occupy disjoint locations, which is true for GcHeap).
//
// Arguments:
//    vnk - Identifies whether to recurse to Conservative or Liberal value numbers
//          when recursing through phis
//    map - Value number for the field map before the assignment
//    elem - Value number for the value being stored (to the given field)
//    indType - Type of the indirection storing the value to the field
//    block - Block where the assignment occurs
//
// Return Value:
//    The value number corresponding to memory after the assignment.

ValueNum ValueNumStore::VNApplySelectorsAssign(
    ValueNumKind vnk, ValueNum map, FieldSeqNode* fieldSeq, ValueNum elem, var_types indType, BasicBlock* block)
{
    if (fieldSeq == nullptr)
    {
        return VNApplySelectorsAssignTypeCoerce(elem, indType, block);
    }
    else
    {
        assert(fieldSeq != FieldSeqStore::NotAField());

        // Skip any "FirstElem" pseudo-fields or any "ConstantIndex" pseudo-fields
        // These will occur, at least, in struct static expressions, for method table offsets.
        if (fieldSeq->IsPseudoField())
        {
            return VNApplySelectorsAssign(vnk, map, fieldSeq->m_next, elem, indType, block);
        }

        // Otherwise, fldHnd is a real field handle.
        CORINFO_FIELD_HANDLE fldHnd   = fieldSeq->m_fieldHnd;
        ValueNum             fldHndVN = VNForHandle(ssize_t(fldHnd), GTF_ICON_FIELD_HDL);
        noway_assert(fldHnd != nullptr);
        CorInfoType fieldCit  = m_pComp->info.compCompHnd->getFieldType(fldHnd);
        var_types   fieldType = JITtype2varType(fieldCit);

        ValueNum elemAfter;
        if (fieldSeq->m_next)
        {
#ifdef DEBUG
            if (m_pComp->verbose)
            {
                const char* modName;
                const char* fldName = m_pComp->eeGetFieldName(fldHnd, &modName);
                printf("    VNForHandle(%s) is " FMT_VN ", fieldType is %s\n", fldName, fldHndVN,
                       varTypeName(fieldType));
            }
#endif
            ValueNum fseqMap = VNForMapSelect(vnk, fieldType, map, fldHndVN);
            elemAfter        = VNApplySelectorsAssign(vnk, fseqMap, fieldSeq->m_next, elem, indType, block);
        }
        else
        {
#ifdef DEBUG
            if (m_pComp->verbose)
            {
                if (fieldSeq->m_next == nullptr)
                {
                    printf("  VNApplySelectorsAssign:\n");
                }
                const char* modName;
                const char* fldName = m_pComp->eeGetFieldName(fldHnd, &modName);
                printf("    VNForHandle(%s) is " FMT_VN ", fieldType is %s\n", fldName, fldHndVN,
                       varTypeName(fieldType));
            }
#endif
            elemAfter = VNApplySelectorsAssignTypeCoerce(elem, indType, block);
        }

        ValueNum newMap = VNForMapStore(fieldType, map, fldHndVN, elemAfter);
        return newMap;
    }
}

ValueNumPair ValueNumStore::VNPairApplySelectors(ValueNumPair map, FieldSeqNode* fieldSeq, var_types indType)
{
    size_t   structSize = 0;
    ValueNum liberalVN  = VNApplySelectors(VNK_Liberal, map.GetLiberal(), fieldSeq, &structSize);
    liberalVN           = VNApplySelectorsTypeCheck(liberalVN, indType, structSize);

    structSize         = 0;
    ValueNum conservVN = VNApplySelectors(VNK_Conservative, map.GetConservative(), fieldSeq, &structSize);
    conservVN          = VNApplySelectorsTypeCheck(conservVN, indType, structSize);

    return ValueNumPair(liberalVN, conservVN);
}

bool ValueNumStore::IsVNNotAField(ValueNum vn)
{
    return m_chunks.GetNoExpand(GetChunkNum(vn))->m_attribs == CEA_NotAField;
}

ValueNum ValueNumStore::VNForFieldSeq(FieldSeqNode* fieldSeq)
{
    if (fieldSeq == nullptr)
    {
        return VNForNull();
    }
    else if (fieldSeq == FieldSeqStore::NotAField())
    {
        // We always allocate a new, unique VN in this call.
        Chunk*   c                 = GetAllocChunk(TYP_REF, CEA_NotAField);
        unsigned offsetWithinChunk = c->AllocVN();
        ValueNum result            = c->m_baseVN + offsetWithinChunk;
        return result;
    }
    else
    {
        ssize_t  fieldHndVal = ssize_t(fieldSeq->m_fieldHnd);
        ValueNum fieldHndVN  = VNForHandle(fieldHndVal, GTF_ICON_FIELD_HDL);
        ValueNum seqNextVN   = VNForFieldSeq(fieldSeq->m_next);
        ValueNum fieldSeqVN  = VNForFunc(TYP_REF, VNF_FieldSeq, fieldHndVN, seqNextVN);

#ifdef DEBUG
        if (m_pComp->verbose)
        {
            printf("    FieldSeq");
            vnDump(m_pComp, fieldSeqVN);
            printf(" is " FMT_VN "\n", fieldSeqVN);
        }
#endif

        return fieldSeqVN;
    }
}

FieldSeqNode* ValueNumStore::FieldSeqVNToFieldSeq(ValueNum vn)
{
    if (vn == VNForNull())
    {
        return nullptr;
    }

    assert(IsVNFunc(vn));

    VNFuncApp funcApp;
    GetVNFunc(vn, &funcApp);
    if (funcApp.m_func == VNF_NotAField)
    {
        return FieldSeqStore::NotAField();
    }

    assert(funcApp.m_func == VNF_FieldSeq);
    const ssize_t fieldHndVal = ConstantValue<ssize_t>(funcApp.m_args[0]);
    FieldSeqNode* head =
        m_pComp->GetFieldSeqStore()->CreateSingleton(reinterpret_cast<CORINFO_FIELD_HANDLE>(fieldHndVal));
    FieldSeqNode* tail = FieldSeqVNToFieldSeq(funcApp.m_args[1]);
    return m_pComp->GetFieldSeqStore()->Append(head, tail);
}

ValueNum ValueNumStore::FieldSeqVNAppend(ValueNum fsVN1, ValueNum fsVN2)
{
    if (fsVN1 == VNForNull())
    {
        return fsVN2;
    }

    assert(IsVNFunc(fsVN1));

    VNFuncApp funcApp1;
    GetVNFunc(fsVN1, &funcApp1);

    if ((funcApp1.m_func == VNF_NotAField) || IsVNNotAField(fsVN2))
    {
        return VNForFieldSeq(FieldSeqStore::NotAField());
    }

    assert(funcApp1.m_func == VNF_FieldSeq);
    ValueNum tailRes    = FieldSeqVNAppend(funcApp1.m_args[1], fsVN2);
    ValueNum fieldSeqVN = VNForFunc(TYP_REF, VNF_FieldSeq, funcApp1.m_args[0], tailRes);

#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("  fieldSeq " FMT_VN " is ", fieldSeqVN);
        vnDump(m_pComp, fieldSeqVN);
        printf("\n");
    }
#endif

    return fieldSeqVN;
}

ValueNum ValueNumStore::ExtendPtrVN(GenTree* opA, GenTree* opB)
{
    if (opB->OperGet() == GT_CNS_INT)
    {
        FieldSeqNode* fldSeq = opB->AsIntCon()->gtFieldSeq;
        if (fldSeq != nullptr)
        {
            return ExtendPtrVN(opA, fldSeq);
        }
    }
    return NoVN;
}

ValueNum ValueNumStore::ExtendPtrVN(GenTree* opA, FieldSeqNode* fldSeq)
{
    assert(fldSeq != nullptr);

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

    if (funcApp.m_func == VNF_PtrToLoc)
    {
#ifdef DEBUG
        // For PtrToLoc, lib == cons.
        VNFuncApp consFuncApp;
        assert(GetVNFunc(VNConservativeNormalValue(opA->gtVNPair), &consFuncApp) && consFuncApp.Equals(funcApp));
#endif
        ValueNum fldSeqVN = VNForFieldSeq(fldSeq);
        res = VNForFunc(TYP_BYREF, VNF_PtrToLoc, funcApp.m_args[0], FieldSeqVNAppend(funcApp.m_args[1], fldSeqVN));
    }
    else if (funcApp.m_func == VNF_PtrToStatic)
    {
        ValueNum fldSeqVN = VNForFieldSeq(fldSeq);
        res               = VNForFunc(TYP_BYREF, VNF_PtrToStatic, FieldSeqVNAppend(funcApp.m_args[0], fldSeqVN));
    }
    else if (funcApp.m_func == VNF_PtrToArrElem)
    {
        ValueNum fldSeqVN = VNForFieldSeq(fldSeq);
        res = VNForFunc(TYP_BYREF, VNF_PtrToArrElem, funcApp.m_args[0], funcApp.m_args[1], funcApp.m_args[2],
                        FieldSeqVNAppend(funcApp.m_args[3], fldSeqVN));
    }
    if (res != NoVN)
    {
        res = VNWithExc(res, opAvnx);
    }
    return res;
}

ValueNum Compiler::fgValueNumberArrIndexAssign(CORINFO_CLASS_HANDLE elemTypeEq,
                                               ValueNum             arrVN,
                                               ValueNum             inxVN,
                                               FieldSeqNode*        fldSeq,
                                               ValueNum             rhsVN,
                                               var_types            indType)
{
    bool      invalidateArray      = false;
    ValueNum  elemTypeEqVN         = vnStore->VNForHandle(ssize_t(elemTypeEq), GTF_ICON_CLASS_HDL);
    var_types arrElemType          = DecodeElemType(elemTypeEq);
    ValueNum  hAtArrType           = vnStore->VNForMapSelect(VNK_Liberal, TYP_REF, fgCurMemoryVN[GcHeap], elemTypeEqVN);
    ValueNum  hAtArrTypeAtArr      = vnStore->VNForMapSelect(VNK_Liberal, TYP_REF, hAtArrType, arrVN);
    ValueNum  hAtArrTypeAtArrAtInx = vnStore->VNForMapSelect(VNK_Liberal, arrElemType, hAtArrTypeAtArr, inxVN);

    ValueNum newValAtInx     = ValueNumStore::NoVN;
    ValueNum newValAtArr     = ValueNumStore::NoVN;
    ValueNum newValAtArrType = ValueNumStore::NoVN;

    if (fldSeq == FieldSeqStore::NotAField())
    {
        // This doesn't represent a proper array access
        JITDUMP("    *** NotAField sequence encountered in fgValueNumberArrIndexAssign\n");

        // Store a new unique value for newValAtArrType
        newValAtArrType = vnStore->VNForExpr(compCurBB, TYP_REF);
        invalidateArray = true;
    }
    else
    {
        // Note that this does the right thing if "fldSeq" is null -- returns last "rhs" argument.
        // This is the value that should be stored at "arr[inx]".
        newValAtInx =
            vnStore->VNApplySelectorsAssign(VNK_Liberal, hAtArrTypeAtArrAtInx, fldSeq, rhsVN, indType, compCurBB);

        var_types arrElemFldType = arrElemType; // Uses arrElemType unless we has a non-null fldSeq
        if (vnStore->IsVNFunc(newValAtInx))
        {
            VNFuncApp funcApp;
            vnStore->GetVNFunc(newValAtInx, &funcApp);
            if (funcApp.m_func == VNF_MapStore)
            {
                arrElemFldType = vnStore->TypeOfVN(newValAtInx);
            }
        }

        if (indType != arrElemFldType)
        {
            // Mismatched types: Store between different types (indType into array of arrElemFldType)
            //

            JITDUMP("    *** Mismatched types in fgValueNumberArrIndexAssign\n");

            // Store a new unique value for newValAtArrType
            newValAtArrType = vnStore->VNForExpr(compCurBB, TYP_REF);
            invalidateArray = true;
        }
    }

    if (!invalidateArray)
    {
        newValAtArr     = vnStore->VNForMapStore(indType, hAtArrTypeAtArr, inxVN, newValAtInx);
        newValAtArrType = vnStore->VNForMapStore(TYP_REF, hAtArrType, arrVN, newValAtArr);
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("  hAtArrType " FMT_VN " is MapSelect(curGcHeap(" FMT_VN "), ", hAtArrType, fgCurMemoryVN[GcHeap]);

        if (arrElemType == TYP_STRUCT)
        {
            printf("%s[]).\n", eeGetClassName(elemTypeEq));
        }
        else
        {
            printf("%s[]).\n", varTypeName(arrElemType));
        }
        printf("  hAtArrTypeAtArr " FMT_VN " is MapSelect(hAtArrType(" FMT_VN "), arr=" FMT_VN ")\n", hAtArrTypeAtArr,
               hAtArrType, arrVN);
        printf("  hAtArrTypeAtArrAtInx " FMT_VN " is MapSelect(hAtArrTypeAtArr(" FMT_VN "), inx=" FMT_VN "):%s\n",
               hAtArrTypeAtArrAtInx, hAtArrTypeAtArr, inxVN, varTypeName(arrElemType));

        if (!invalidateArray)
        {
            printf("  newValAtInd " FMT_VN " is ", newValAtInx);
            vnStore->vnDump(this, newValAtInx);
            printf("\n");

            printf("  newValAtArr " FMT_VN " is ", newValAtArr);
            vnStore->vnDump(this, newValAtArr);
            printf("\n");
        }

        printf("  newValAtArrType " FMT_VN " is ", newValAtArrType);
        vnStore->vnDump(this, newValAtArrType);
        printf("\n");
    }
#endif // DEBUG

    return vnStore->VNForMapStore(TYP_REF, fgCurMemoryVN[GcHeap], elemTypeEqVN, newValAtArrType);
}

ValueNum Compiler::fgValueNumberArrIndexVal(GenTree* tree, VNFuncApp* pFuncApp, ValueNum addrXvn)
{
    assert(vnStore->IsVNHandle(pFuncApp->m_args[0]));
    CORINFO_CLASS_HANDLE arrElemTypeEQ = CORINFO_CLASS_HANDLE(vnStore->ConstantValue<ssize_t>(pFuncApp->m_args[0]));
    ValueNum             arrVN         = pFuncApp->m_args[1];
    ValueNum             inxVN         = pFuncApp->m_args[2];
    FieldSeqNode*        fldSeq        = vnStore->FieldSeqVNToFieldSeq(pFuncApp->m_args[3]);
    return fgValueNumberArrIndexVal(tree, arrElemTypeEQ, arrVN, inxVN, addrXvn, fldSeq);
}

ValueNum Compiler::fgValueNumberArrIndexVal(GenTree*             tree,
                                            CORINFO_CLASS_HANDLE elemTypeEq,
                                            ValueNum             arrVN,
                                            ValueNum             inxVN,
                                            ValueNum             excVN,
                                            FieldSeqNode*        fldSeq)
{
    assert(tree == nullptr || tree->OperIsIndir());

    // The VN inputs are required to be non-exceptional values.
    assert(arrVN == vnStore->VNNormalValue(arrVN));
    assert(inxVN == vnStore->VNNormalValue(inxVN));

    var_types elemTyp = DecodeElemType(elemTypeEq);
    var_types indType = (tree == nullptr) ? elemTyp : tree->TypeGet();
    ValueNum  selectedElem;

    if (fldSeq == FieldSeqStore::NotAField())
    {
        // This doesn't represent a proper array access
        JITDUMP("    *** NotAField sequence encountered in fgValueNumberArrIndexVal\n");

        // a new unique value number
        selectedElem = vnStore->VNForExpr(compCurBB, elemTyp);

#ifdef DEBUG
        if (verbose)
        {
            printf("  IND of PtrToArrElem is unique VN " FMT_VN ".\n", selectedElem);
        }
#endif // DEBUG

        if (tree != nullptr)
        {
            tree->gtVNPair.SetBoth(selectedElem);
        }
    }
    else
    {
        ValueNum elemTypeEqVN    = vnStore->VNForHandle(ssize_t(elemTypeEq), GTF_ICON_CLASS_HDL);
        ValueNum hAtArrType      = vnStore->VNForMapSelect(VNK_Liberal, TYP_REF, fgCurMemoryVN[GcHeap], elemTypeEqVN);
        ValueNum hAtArrTypeAtArr = vnStore->VNForMapSelect(VNK_Liberal, TYP_REF, hAtArrType, arrVN);
        ValueNum wholeElem       = vnStore->VNForMapSelect(VNK_Liberal, elemTyp, hAtArrTypeAtArr, inxVN);

#ifdef DEBUG
        if (verbose)
        {
            printf("  hAtArrType " FMT_VN " is MapSelect(curGcHeap(" FMT_VN "), ", hAtArrType, fgCurMemoryVN[GcHeap]);
            if (elemTyp == TYP_STRUCT)
            {
                printf("%s[]).\n", eeGetClassName(elemTypeEq));
            }
            else
            {
                printf("%s[]).\n", varTypeName(elemTyp));
            }

            printf("  hAtArrTypeAtArr " FMT_VN " is MapSelect(hAtArrType(" FMT_VN "), arr=" FMT_VN ").\n",
                   hAtArrTypeAtArr, hAtArrType, arrVN);

            printf("  wholeElem " FMT_VN " is MapSelect(hAtArrTypeAtArr(" FMT_VN "), ind=" FMT_VN ").\n", wholeElem,
                   hAtArrTypeAtArr, inxVN);
        }
#endif // DEBUG

        selectedElem          = wholeElem;
        size_t elemStructSize = 0;
        if (fldSeq)
        {
            selectedElem = vnStore->VNApplySelectors(VNK_Liberal, wholeElem, fldSeq, &elemStructSize);
            elemTyp      = vnStore->TypeOfVN(selectedElem);
        }
        selectedElem = vnStore->VNApplySelectorsTypeCheck(selectedElem, indType, elemStructSize);
        selectedElem = vnStore->VNWithExc(selectedElem, excVN);

#ifdef DEBUG
        if (verbose && (selectedElem != wholeElem))
        {
            printf("  selectedElem is " FMT_VN " after applying selectors.\n", selectedElem);
        }
#endif // DEBUG

        if (tree != nullptr)
        {
            tree->gtVNPair.SetLiberal(selectedElem);

            // TODO-CQ: what to do here about exceptions?  We don't have the array and ind conservative
            // values, so we don't have their exceptions.  Maybe we should.
            tree->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
        }
    }

    return selectedElem;
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

var_types ValueNumStore::TypeOfVN(ValueNum vn)
{
    if (vn == NoVN)
    {
        return TYP_UNDEF;
    }

    Chunk* c = m_chunks.GetNoExpand(GetChunkNum(vn));
    return c->m_typ;
}

//------------------------------------------------------------------------
// LoopOfVN: If the given value number is an opaque one associated with a particular
//    expression in the IR, give the loop number where the expression occurs; otherwise,
//    returns MAX_LOOP_NUM.
//
// Arguments:
//    vn - Value number to query
//
// Return Value:
//    The correspondingblock's bbNatLoopNum, which may be BasicBlock::NOT_IN_LOOP.
//    Returns MAX_LOOP_NUM if this VN is not an opaque value number associated with
//    a particular expression/location in the IR.

BasicBlock::loopNumber ValueNumStore::LoopOfVN(ValueNum vn)
{
    if (vn == NoVN)
    {
        return MAX_LOOP_NUM;
    }

    Chunk* c = m_chunks.GetNoExpand(GetChunkNum(vn));
    return c->m_loopNum;
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

bool ValueNumStore::IsVNInt32Constant(ValueNum vn)
{
    if (!IsVNConstant(vn))
    {
        return false;
    }

    return TypeOfVN(vn) == TYP_INT;
}

unsigned ValueNumStore::GetHandleFlags(ValueNum vn)
{
    assert(IsVNHandle(vn));
    Chunk*    c      = m_chunks.GetNoExpand(GetChunkNum(vn));
    unsigned  offset = ChunkOffset(vn);
    VNHandle* handle = &reinterpret_cast<VNHandle*>(c->m_defs)[offset];
    return handle->m_flags;
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

bool ValueNumStore::IsVNConstantBound(ValueNum vn)
{
    // Do we have "var < 100"?
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

    return IsVNInt32Constant(funcAttr.m_args[0]) != IsVNInt32Constant(funcAttr.m_args[1]);
}

void ValueNumStore::GetConstantBoundInfo(ValueNum vn, ConstantBoundInfo* info)
{
    assert(IsVNConstantBound(vn));
    assert(info);

    // Do we have var < 100?
    VNFuncApp funcAttr;
    GetVNFunc(vn, &funcAttr);

    bool isOp1Const = IsVNInt32Constant(funcAttr.m_args[1]);

    if (isOp1Const)
    {
        info->cmpOper  = funcAttr.m_func;
        info->cmpOpVN  = funcAttr.m_args[0];
        info->constVal = GetConstantInt32(funcAttr.m_args[1]);
    }
    else
    {
        info->cmpOper  = GenTree::SwapRelop((genTreeOps)funcAttr.m_func);
        info->cmpOpVN  = funcAttr.m_args[1];
        info->constVal = GetConstantInt32(funcAttr.m_args[0]);
    }
}

//------------------------------------------------------------------------
// IsVNArrLenUnsignedBound: Checks if the specified vn represents an expression
//    such as "(uint)i < (uint)len" that implies that the index is valid
//    (0 <= i && i < a.len).
//
// Arguments:
//    vn - Value number to query
//    info - Pointer to an UnsignedCompareCheckedBoundInfo object to return information about
//           the expression. Not populated if the vn expression isn't suitable (e.g. i <= len).
//           This enables optCreateJTrueBoundAssertion to immediatly create an OAK_NO_THROW
//           assertion instead of the OAK_EQUAL/NOT_EQUAL assertions created by signed compares
//           (IsVNCompareCheckedBound, IsVNCompareCheckedBoundArith) that require further processing.

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
    if (GetVNFunc(vn, &funcAttr) && funcAttr.m_func == (VNFunc)GT_ARR_LENGTH)
    {
        return funcAttr.m_args[0];
    }
    return NoVN;
}

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

int ValueNumStore::GetNewArrSize(ValueNum vn)
{
    VNFuncApp funcApp;
    if (IsVNNewArr(vn, &funcApp))
    {
        ValueNum arg1VN = funcApp.m_args[1];
        if (IsVNConstant(arg1VN) && TypeOfVN(arg1VN) == TYP_INT)
        {
            return ConstantValue<int>(arg1VN);
        }
    }
    return 0;
}

bool ValueNumStore::IsVNArrLen(ValueNum vn)
{
    if (vn == NoVN)
    {
        return false;
    }
    VNFuncApp funcAttr;
    return (GetVNFunc(vn, &funcAttr) && funcAttr.m_func == (VNFunc)GT_ARR_LENGTH);
}

bool ValueNumStore::IsVNCheckedBound(ValueNum vn)
{
    bool dummy;
    if (m_checkedBoundVNs.TryGetValue(vn, &dummy))
    {
        // This VN appeared as the conservative VN of the length argument of some
        // GT_ARR_BOUND node.
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

ValueNum ValueNumStore::EvalMathFuncUnary(var_types typ, NamedIntrinsic gtMathFN, ValueNum arg0VN)
{
    assert(arg0VN == VNNormalValue(arg0VN));

    // If the math intrinsic is not implemented by target-specific instructions, such as implemented
    // by user calls, then don't do constant folding on it. This minimizes precision loss.

    if (IsVNConstant(arg0VN) && m_pComp->IsTargetIntrinsic(gtMathFN))
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
                case NI_System_Math_Sin:
                    res = sin(arg0Val);
                    break;
                case NI_System_Math_Cos:
                    res = cos(arg0Val);
                    break;
                case NI_System_Math_Sqrt:
                    res = sqrt(arg0Val);
                    break;
                case NI_System_Math_Abs:
                    res = fabs(arg0Val);
                    break;
                case NI_System_Math_Ceiling:
                    res = ceil(arg0Val);
                    break;
                case NI_System_Math_Floor:
                    res = floor(arg0Val);
                    break;
                case NI_System_Math_Round:
                    res = FloatingPointUtils::round(arg0Val);
                    break;
                default:
                    unreached(); // the above are the only math intrinsics at the time of this writing.
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
                case NI_System_Math_Sin:
                    res = sinf(arg0Val);
                    break;
                case NI_System_Math_Cos:
                    res = cosf(arg0Val);
                    break;
                case NI_System_Math_Sqrt:
                    res = sqrtf(arg0Val);
                    break;
                case NI_System_Math_Abs:
                    res = fabsf(arg0Val);
                    break;
                case NI_System_Math_Ceiling:
                    res = ceilf(arg0Val);
                    break;
                case NI_System_Math_Floor:
                    res = floorf(arg0Val);
                    break;
                case NI_System_Math_Round:
                    res = FloatingPointUtils::round(arg0Val);
                    break;
                default:
                    unreached(); // the above are the only math intrinsics at the time of this writing.
            }

            return VNForFloatCon(res);
        }
        else
        {
            // NI_System_Math{F}_Round are currently the only intrinsic that take floating-point arguments
            // and return a non floating-point result.

            assert(typ == TYP_INT);
            assert(gtMathFN == NI_System_Math_Round);

            int res = 0;

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

            return VNForIntCon(res);
        }
    }
    else
    {
        assert(typ == TYP_DOUBLE || typ == TYP_FLOAT || typ == TYP_INT && gtMathFN == NI_System_Math_Round);

        VNFunc vnf = VNF_Boundary;
        switch (gtMathFN)
        {
            case NI_System_Math_Sin:
                vnf = VNF_Sin;
                break;
            case NI_System_Math_Cos:
                vnf = VNF_Cos;
                break;
            case NI_System_Math_Cbrt:
                vnf = VNF_Cbrt;
                break;
            case NI_System_Math_Sqrt:
                vnf = VNF_Sqrt;
                break;
            case NI_System_Math_Abs:
                vnf = VNF_Abs;
                break;
            case NI_System_Math_Round:
                if (typ == TYP_DOUBLE)
                {
                    vnf = VNF_RoundDouble;
                }
                else if (typ == TYP_FLOAT)
                {
                    vnf = VNF_RoundFloat;
                }
                else if (typ == TYP_INT)
                {
                    vnf = VNF_RoundInt;
                }
                else
                {
                    noway_assert(!"Invalid INTRINSIC_Round");
                }
                break;
            case NI_System_Math_Cosh:
                vnf = VNF_Cosh;
                break;
            case NI_System_Math_Sinh:
                vnf = VNF_Sinh;
                break;
            case NI_System_Math_Tan:
                vnf = VNF_Tan;
                break;
            case NI_System_Math_Tanh:
                vnf = VNF_Tanh;
                break;
            case NI_System_Math_Asin:
                vnf = VNF_Asin;
                break;
            case NI_System_Math_Asinh:
                vnf = VNF_Asinh;
                break;
            case NI_System_Math_Acos:
                vnf = VNF_Acos;
                break;
            case NI_System_Math_Acosh:
                vnf = VNF_Acosh;
                break;
            case NI_System_Math_Atan:
                vnf = VNF_Atan;
                break;
            case NI_System_Math_Atanh:
                vnf = VNF_Atanh;
                break;
            case NI_System_Math_Log10:
                vnf = VNF_Log10;
                break;
            case NI_System_Math_Exp:
                vnf = VNF_Exp;
                break;
            case NI_System_Math_Ceiling:
                vnf = VNF_Ceiling;
                break;
            case NI_System_Math_Floor:
                vnf = VNF_Floor;
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

    VNFunc vnf = VNF_Boundary;

    // Currently, none of the binary math intrinsic are implemented by target-specific instructions.
    // To minimize precision loss, do not do constant folding on them.

    switch (gtMathFN)
    {
        case NI_System_Math_Atan2:
            vnf = VNF_Atan2;
            break;

        case NI_System_Math_Pow:
            vnf = VNF_Pow;
            break;

        default:
            unreached(); // the above are the only binary math intrinsics at the time of this writing.
    }

    return VNForFunc(typ, vnf, arg0VN, arg1VN);
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
        case CEA_NotAField:
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
    switch (c->m_attribs)
    {
        case CEA_Func4:
        {
            VNDefFunc4Arg* farg4 = &reinterpret_cast<VNDefFunc4Arg*>(c->m_defs)[offset];
            funcApp->m_func      = farg4->m_func;
            funcApp->m_arity     = 4;
            funcApp->m_args[0]   = farg4->m_arg0;
            funcApp->m_args[1]   = farg4->m_arg1;
            funcApp->m_args[2]   = farg4->m_arg2;
            funcApp->m_args[3]   = farg4->m_arg3;
            return true;
        }
        case CEA_Func3:
        {
            VNDefFunc3Arg* farg3 = &reinterpret_cast<VNDefFunc3Arg*>(c->m_defs)[offset];
            funcApp->m_func      = farg3->m_func;
            funcApp->m_arity     = 3;
            funcApp->m_args[0]   = farg3->m_arg0;
            funcApp->m_args[1]   = farg3->m_arg1;
            funcApp->m_args[2]   = farg3->m_arg2;
            return true;
        }
        case CEA_Func2:
        {
            VNDefFunc2Arg* farg2 = &reinterpret_cast<VNDefFunc2Arg*>(c->m_defs)[offset];
            funcApp->m_func      = farg2->m_func;
            funcApp->m_arity     = 2;
            funcApp->m_args[0]   = farg2->m_arg0;
            funcApp->m_args[1]   = farg2->m_arg1;
            return true;
        }
        case CEA_Func1:
        {
            VNDefFunc1Arg* farg1 = &reinterpret_cast<VNDefFunc1Arg*>(c->m_defs)[offset];
            funcApp->m_func      = farg1->m_func;
            funcApp->m_arity     = 1;
            funcApp->m_args[0]   = farg1->m_arg0;
            return true;
        }
        case CEA_Func0:
        {
            VNDefFunc0Arg* farg0 = &reinterpret_cast<VNDefFunc0Arg*>(c->m_defs)[offset];
            funcApp->m_func      = farg0->m_func;
            funcApp->m_arity     = 0;
            return true;
        }
        case CEA_NotAField:
        {
            funcApp->m_func  = VNF_NotAField;
            funcApp->m_arity = 0;
            return true;
        }
        default:
            return false;
    }
}

ValueNum ValueNumStore::VNForRefInAddr(ValueNum vn)
{
    var_types vnType = TypeOfVN(vn);
    if (vnType == TYP_REF)
    {
        return vn;
    }
    // Otherwise...
    assert(vnType == TYP_BYREF);
    VNFuncApp funcApp;
    if (GetVNFunc(vn, &funcApp))
    {
        assert(funcApp.m_arity == 2 && (funcApp.m_func == VNFunc(GT_ADD) || funcApp.m_func == VNFunc(GT_SUB)));
        var_types vnArg0Type = TypeOfVN(funcApp.m_args[0]);
        if (vnArg0Type == TYP_REF || vnArg0Type == TYP_BYREF)
        {
            return VNForRefInAddr(funcApp.m_args[0]);
        }
        else
        {
            assert(funcApp.m_func == VNFunc(GT_ADD) &&
                   (TypeOfVN(funcApp.m_args[1]) == TYP_REF || TypeOfVN(funcApp.m_args[1]) == TYP_BYREF));
            return VNForRefInAddr(funcApp.m_args[1]);
        }
    }
    else
    {
        assert(IsVNConstant(vn));
        return vn;
    }
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
    else if (IsVNHandle(vn))
    {
        ssize_t val = ConstantValue<ssize_t>(vn);
        printf("Hnd const: 0x%p", dspPtr(val));
    }
    else if (IsVNConstant(vn))
    {
        var_types vnt = TypeOfVN(vn);
        switch (vnt)
        {
            case TYP_BOOL:
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
                    printf("LngCns: ");
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
                else
                {
                    assert(vn == VNForZeroMap());
                    printf("zeroMap");
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
            case TYP_SIMD12:
            case TYP_SIMD16:
            case TYP_SIMD32:
            {
                // Only the zero constant is currently allowed for SIMD types
                //
                INT64 val = ConstantValue<INT64>(vn);
                assert(val == 0);
                printf(" 0");
            }
            break;
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
            case VNF_FieldSeq:
                vnDumpFieldSeq(comp, &funcApp, true);
                break;
            case VNF_MapSelect:
                vnDumpMapSelect(comp, &funcApp);
                break;
            case VNF_MapStore:
                vnDumpMapStore(comp, &funcApp);
                break;
            case VNF_ValWithExc:
                vnDumpValWithExc(comp, &funcApp);
                break;
#ifdef FEATURE_SIMD
            case VNF_SimdType:
                vnDumpSimdType(comp, &funcApp);
                break;
#endif // FEATURE_SIMD

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

// Requires "valWithExc" to be a value with an exeception set VNFuncApp.
// Prints a representation of the exeception set on standard out.
void ValueNumStore::vnDumpValWithExc(Compiler* comp, VNFuncApp* valWithExc)
{
    assert(valWithExc->m_func == VNF_ValWithExc); // Precondition.

    ValueNum normVN = valWithExc->m_args[0]; // First arg is the VN from normal execution
    ValueNum excVN  = valWithExc->m_args[1]; // Second arg is the set of possible exceptions

    assert(IsVNFunc(excVN));
    VNFuncApp excSeq;
    GetVNFunc(excVN, &excSeq);

    printf("norm=");
    printf(FMT_VN, normVN);
    vnDump(comp, normVN);
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

void ValueNumStore::vnDumpFieldSeq(Compiler* comp, VNFuncApp* fieldSeq, bool isHead)
{
    assert(fieldSeq->m_func == VNF_FieldSeq); // Precondition.
    // First arg is the field handle VN.
    assert(IsVNConstant(fieldSeq->m_args[0]) && TypeOfVN(fieldSeq->m_args[0]) == TYP_I_IMPL);
    ssize_t fieldHndVal = ConstantValue<ssize_t>(fieldSeq->m_args[0]);
    bool    hasTail     = (fieldSeq->m_args[1] != VNForNull());

    if (isHead && hasTail)
    {
        printf("(");
    }

    CORINFO_FIELD_HANDLE fldHnd = CORINFO_FIELD_HANDLE(fieldHndVal);
    if (fldHnd == FieldSeqStore::FirstElemPseudoField)
    {
        printf("#FirstElem");
    }
    else if (fldHnd == FieldSeqStore::ConstantIndexPseudoField)
    {
        printf("#ConstantIndex");
    }
    else
    {
        const char* modName;
        const char* fldName = m_pComp->eeGetFieldName(fldHnd, &modName);
        printf("%s", fldName);
    }

    if (hasTail)
    {
        printf(", ");
        assert(IsVNFunc(fieldSeq->m_args[1]));
        VNFuncApp tail;
        GetVNFunc(fieldSeq->m_args[1], &tail);
        vnDumpFieldSeq(comp, &tail, false);
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

    comp->vnPrint(mapVN, 0);
    printf("[");
    comp->vnPrint(indexVN, 0);
    printf(" := ");
    comp->vnPrint(newValVN, 0);
    printf("]");
}

#ifdef FEATURE_SIMD
void ValueNumStore::vnDumpSimdType(Compiler* comp, VNFuncApp* simdType)
{
    assert(simdType->m_func == VNF_SimdType); // Preconditions.
    assert(IsVNConstant(simdType->m_args[0]));
    assert(IsVNConstant(simdType->m_args[1]));

    int       simdSize = ConstantValue<int>(simdType->m_args[0]);
    var_types baseType = (var_types)ConstantValue<int>(simdType->m_args[1]);

    printf("%s(simd%d, %s)", VNFuncName(simdType->m_func), simdSize, varTypeName(baseType));
}
#endif // FEATURE_SIMD

#endif // DEBUG

// Static fields, methods.
static UINT8      vnfOpAttribs[VNF_COUNT];
static genTreeOps genTreeOpsIllegalAsVNFunc[] = {GT_IND, // When we do heap memory.
                                                 GT_NULLCHECK, GT_QMARK, GT_COLON, GT_LOCKADD, GT_XADD, GT_XCHG,
                                                 GT_CMPXCHG, GT_LCLHEAP, GT_BOX,

                                                 // These need special semantics:
                                                 GT_COMMA, // == second argument (but with exception(s) from first).
                                                 GT_ADDR, GT_ARR_BOUNDS_CHECK,
                                                 GT_OBJ,      // May reference heap memory.
                                                 GT_BLK,      // May reference heap memory.
                                                 GT_INIT_VAL, // Not strictly a pass-through.

                                                 // These control-flow operations need no values.
                                                 GT_JTRUE, GT_RETURN, GT_SWITCH, GT_RETFILT, GT_CKFINITE};

UINT8* ValueNumStore::s_vnfOpAttribs = nullptr;

void ValueNumStore::InitValueNumStoreStatics()
{
    // Make sure we have the constants right...
    assert(unsigned(VNFOA_Arity1) == (1 << VNFOA_ArityShift));
    assert(VNFOA_ArityMask == (VNFOA_MaxArity << VNFOA_ArityShift));

    s_vnfOpAttribs = &vnfOpAttribs[0];
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
        // Since GT_ARR_BOUNDS_CHECK is not currently GTK_BINOP
        else if (gtOper == GT_ARR_BOUNDS_CHECK)
        {
            arity = 2;
        }
        vnfOpAttribs[i] |= ((arity << VNFOA_ArityShift) & VNFOA_ArityMask);

        if (GenTree::OperIsCommutative(gtOper))
        {
            vnfOpAttribs[i] |= VNFOA_Commutative;
        }
    }

    // I so wish this wasn't the best way to do this...

    int vnfNum = VNF_Boundary + 1; // The macro definition below will update this after using it.

#define ValueNumFuncDef(vnf, arity, commute, knownNonNull, sharedStatic)                                               \
    if (commute)                                                                                                       \
        vnfOpAttribs[vnfNum] |= VNFOA_Commutative;                                                                     \
    if (knownNonNull)                                                                                                  \
        vnfOpAttribs[vnfNum] |= VNFOA_KnownNonNull;                                                                    \
    if (sharedStatic)                                                                                                  \
        vnfOpAttribs[vnfNum] |= VNFOA_SharedStatic;                                                                    \
    if (arity > 0)                                                                                                     \
        vnfOpAttribs[vnfNum] |= ((arity << VNFOA_ArityShift) & VNFOA_ArityMask);                                       \
    vnfNum++;

#include "valuenumfuncs.h"
#undef ValueNumFuncDef

    assert(vnfNum == VNF_COUNT);

#define ValueNumFuncSetArity(vnfNum, arity)                                                                            \
    vnfOpAttribs[vnfNum] &= ~VNFOA_ArityMask;                               /* clear old arity value   */              \
    vnfOpAttribs[vnfNum] |= ((arity << VNFOA_ArityShift) & VNFOA_ArityMask) /* set the new arity value */

#ifdef FEATURE_SIMD

    // SIMDIntrinsicInit has an entry of 2 for numArgs, but it only has one normal arg
    ValueNumFuncSetArity(VNF_SIMD_Init, 1);
    // SIMDIntrinsicWidenHi has an entry of 2 for numArgs, but it only has one normal arg
    ValueNumFuncSetArity(VNF_SIMD_WidenHi, 1);
    // SIMDIntrinsicWidenLo has an entry of 2 for numArgs, but it only has one normal arg
    ValueNumFuncSetArity(VNF_SIMD_WidenLo, 1);

    // Some SIMD intrinsic nodes have an extra VNF_SimdType arg
    //
    for (SIMDIntrinsicID id = SIMDIntrinsicID::SIMDIntrinsicNone; (id < SIMDIntrinsicID::SIMDIntrinsicInvalid);
         id                 = (SIMDIntrinsicID)(id + 1))
    {
        bool encodeResultType = Compiler::vnEncodesResultTypeForSIMDIntrinsic(id);

        if (encodeResultType)
        {
            // These SIMDIntrinsic's have an extra VNF_SimdType arg.
            //
            VNFunc   func     = VNFunc(VNF_SIMD_FIRST + id);
            unsigned oldArity = VNFuncArity(func);
            unsigned newArity = oldArity + 1;

            ValueNumFuncSetArity(func, newArity);
        }
    }

#endif // FEATURE_SIMD

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
            unsigned oldArity = VNFuncArity(func);
            unsigned newArity = oldArity + 1;

            ValueNumFuncSetArity(func, newArity);
        }
    }

#endif // FEATURE_HW_INTRINSICS

#undef ValueNumFuncSetArity

    for (unsigned i = 0; i < _countof(genTreeOpsIllegalAsVNFunc); i++)
    {
        vnfOpAttribs[genTreeOpsIllegalAsVNFunc[i]] |= VNFOA_IllegalGenTreeOp;
    }
}

#ifdef DEBUG
// Define the name array.
#define ValueNumFuncDef(vnf, arity, commute, knownNonNull, sharedStatic) #vnf,

const char* ValueNumStore::VNFuncNameArr[] = {
#include "valuenumfuncs.h"
#undef ValueNumFuncDef
};

// static
const char* ValueNumStore::VNFuncName(VNFunc vnf)
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

static const char* s_reservedNameArr[] = {
    "$VN.Recursive",    // -2  RecursiveVN
    "$VN.No",           // -1  NoVN
    "$VN.Null",         //  0  VNForNull()
    "$VN.ZeroMap",      //  1  VNForZeroMap()
    "$VN.ReadOnlyHeap", //  2  VNForROH()
    "$VN.Void",         //  3  VNForVoid()
    "$VN.EmptyExcSet"   //  4  VNForEmptyExcSet()
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

typedef JitExpandArrayStack<BasicBlock*> BlockStack;

// This represents the "to do" state of the value number computation.
struct ValueNumberState
{
    // These two stacks collectively represent the set of blocks that are candidates for
    // processing, because at least one predecessor has been processed.  Blocks on "m_toDoAllPredsDone"
    // have had *all* predecessors processed, and thus are candidates for some extra optimizations.
    // Blocks on "m_toDoNotAllPredsDone" have at least one predecessor that has not been processed.
    // Blocks are initially on "m_toDoNotAllPredsDone" may be moved to "m_toDoAllPredsDone" when their last
    // unprocessed predecessor is processed, thus maintaining the invariants.
    BlockStack m_toDoAllPredsDone;
    BlockStack m_toDoNotAllPredsDone;

    Compiler* m_comp;

    // TBD: This should really be a bitset...
    // For now:
    // first bit indicates completed,
    // second bit indicates that it's been pushed on all-done stack,
    // third bit indicates that it's been pushed on not-all-done stack.
    BYTE* m_visited;

    enum BlockVisitBits
    {
        BVB_complete     = 0x1,
        BVB_onAllDone    = 0x2,
        BVB_onNotAllDone = 0x4,
    };

    bool GetVisitBit(unsigned bbNum, BlockVisitBits bvb)
    {
        return (m_visited[bbNum] & bvb) != 0;
    }
    void SetVisitBit(unsigned bbNum, BlockVisitBits bvb)
    {
        m_visited[bbNum] |= bvb;
    }

    ValueNumberState(Compiler* comp)
        : m_toDoAllPredsDone(comp->getAllocator(), /*minSize*/ 4)
        , m_toDoNotAllPredsDone(comp->getAllocator(), /*minSize*/ 4)
        , m_comp(comp)
        , m_visited(new (comp, CMK_ValueNumber) BYTE[comp->fgBBNumMax + 1]())
    {
    }

    BasicBlock* ChooseFromNotAllPredsDone()
    {
        assert(m_toDoAllPredsDone.Size() == 0);
        // If we have no blocks with all preds done, then (ideally, if all cycles have been captured by loops)
        // we must have at least one block within a loop.  We want to do the loops first.  Doing a loop entry block
        // should break the cycle, making the rest of the body of the loop (unless there's a nested loop) doable by the
        // all-preds-done rule.  If several loop entry blocks are available, at least one should have all non-loop preds
        // done -- we choose that.
        for (unsigned i = 0; i < m_toDoNotAllPredsDone.Size(); i++)
        {
            BasicBlock* cand = m_toDoNotAllPredsDone.Get(i);

            // Skip any already-completed blocks (a block may have all its preds finished, get added to the
            // all-preds-done todo set, and get processed there).  Do this by moving the last one down, to
            // keep the array compact.
            while (GetVisitBit(cand->bbNum, BVB_complete))
            {
                if (i + 1 < m_toDoNotAllPredsDone.Size())
                {
                    cand = m_toDoNotAllPredsDone.Pop();
                    m_toDoNotAllPredsDone.Set(i, cand);
                }
                else
                {
                    // "cand" is the last element; delete it.
                    (void)m_toDoNotAllPredsDone.Pop();
                    break;
                }
            }
            // We may have run out of non-complete candidates above.  If so, we're done.
            if (i == m_toDoNotAllPredsDone.Size())
            {
                break;
            }

            // See if "cand" is a loop entry.
            unsigned lnum;
            if (m_comp->optBlockIsLoopEntry(cand, &lnum))
            {
                // "lnum" is the innermost loop of which "cand" is the entry; find the outermost.
                unsigned lnumPar = m_comp->optLoopTable[lnum].lpParent;
                while (lnumPar != BasicBlock::NOT_IN_LOOP)
                {
                    if (m_comp->optLoopTable[lnumPar].lpEntry == cand)
                    {
                        lnum = lnumPar;
                    }
                    else
                    {
                        break;
                    }
                    lnumPar = m_comp->optLoopTable[lnumPar].lpParent;
                }

                bool allNonLoopPredsDone = true;
                for (flowList* pred = m_comp->BlockPredsWithEH(cand); pred != nullptr; pred = pred->flNext)
                {
                    BasicBlock* predBlock = pred->flBlock;
                    if (!m_comp->optLoopTable[lnum].lpContains(predBlock))
                    {
                        if (!GetVisitBit(predBlock->bbNum, BVB_complete))
                        {
                            allNonLoopPredsDone = false;
                        }
                    }
                }
                if (allNonLoopPredsDone)
                {
                    return cand;
                }
            }
        }

        // If we didn't find a loop entry block with all non-loop preds done above, then return a random member (if
        // there is one).
        if (m_toDoNotAllPredsDone.Size() == 0)
        {
            return nullptr;
        }
        else
        {
            return m_toDoNotAllPredsDone.Pop();
        }
    }

// Debugging output that is too detailed for a normal JIT dump...
#define DEBUG_VN_VISIT 0

    // Record that "blk" has been visited, and add any unvisited successors of "blk" to the appropriate todo set.
    void FinishVisit(BasicBlock* blk)
    {
#ifdef DEBUG_VN_VISIT
        JITDUMP("finish(" FMT_BB ").\n", blk->bbNum);
#endif // DEBUG_VN_VISIT

        SetVisitBit(blk->bbNum, BVB_complete);

        for (BasicBlock* succ : blk->GetAllSuccs(m_comp))
        {
#ifdef DEBUG_VN_VISIT
            JITDUMP("   Succ(" FMT_BB ").\n", succ->bbNum);
#endif // DEBUG_VN_VISIT

            if (GetVisitBit(succ->bbNum, BVB_complete))
            {
                continue;
            }
#ifdef DEBUG_VN_VISIT
            JITDUMP("     Not yet completed.\n");
#endif // DEBUG_VN_VISIT

            bool allPredsVisited = true;
            for (flowList* pred = m_comp->BlockPredsWithEH(succ); pred != nullptr; pred = pred->flNext)
            {
                BasicBlock* predBlock = pred->flBlock;
                if (!GetVisitBit(predBlock->bbNum, BVB_complete))
                {
                    allPredsVisited = false;
                    break;
                }
            }

            if (allPredsVisited)
            {
#ifdef DEBUG_VN_VISIT
                JITDUMP("     All preds complete, adding to allDone.\n");
#endif // DEBUG_VN_VISIT

                assert(!GetVisitBit(succ->bbNum, BVB_onAllDone)); // Only last completion of last succ should add to
                                                                  // this.
                m_toDoAllPredsDone.Push(succ);
                SetVisitBit(succ->bbNum, BVB_onAllDone);
            }
            else
            {
#ifdef DEBUG_VN_VISIT
                JITDUMP("     Not all preds complete  Adding to notallDone, if necessary...\n");
#endif // DEBUG_VN_VISIT

                if (!GetVisitBit(succ->bbNum, BVB_onNotAllDone))
                {
#ifdef DEBUG_VN_VISIT
                    JITDUMP("       Was necessary.\n");
#endif // DEBUG_VN_VISIT
                    m_toDoNotAllPredsDone.Push(succ);
                    SetVisitBit(succ->bbNum, BVB_onNotAllDone);
                }
            }
        }
    }

    bool ToDoExists()
    {
        return m_toDoAllPredsDone.Size() > 0 || m_toDoNotAllPredsDone.Size() > 0;
    }
};

void Compiler::fgValueNumber()
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
        return;
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
        for (BasicBlock* blk = fgFirstBB; blk != nullptr; blk = blk->bbNext)
        {
            // Now iterate over the block's statements, and their trees.
            for (Statement* stmt : StatementList(blk->FirstNonPhiDef()))
            {
                for (GenTree* tree = stmt->GetTreeList(); tree != nullptr; tree = tree->gtNext)
                {
                    tree->gtVNPair.SetBoth(ValueNumStore::NoVN);
                }
            }
        }
    }

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

        LclVarDsc* varDsc = &lvaTable[lclNum];
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
            // The last clause covers the use-before-def variables (the ones that are live-in to the the first block),
            // these are variables that are read before being initialized (at least on some control flow paths)
            // if they are not must-init, then they get VNF_InitVal(i), as with the param case.)

            bool isZeroed = (info.compInitMem || varDsc->lvMustInit);

            // For OSR, locals or promoted fields of locals may be missing the initial def
            // because of partial importation. We can't assume they are zero.
            if (lvaIsOSRLocal(lclNum))
            {
                isZeroed = false;
            }

            ValueNum  initVal = ValueNumStore::NoVN; // We must assign a new value to initVal
            var_types typ     = varDsc->TypeGet();

            switch (typ)
            {
                case TYP_LCLBLK: // The outgoing args area for arm and x64
                case TYP_BLK:    // A blob of memory
                    // TYP_BLK is used for the EHSlots LclVar on x86 (aka shadowSPslotsVar)
                    // and for the lvaInlinedPInvokeFrameVar on x64, arm and x86
                    // The stack associated with these LclVars are not zero initialized
                    // thus we set 'initVN' to a new, unique VN.
                    //
                    initVal = vnStore->VNForExpr(fgFirstBB);
                    break;

                case TYP_BYREF:
                    if (isZeroed)
                    {
                        // LclVars of TYP_BYREF can be zero-inited.
                        initVal = vnStore->VNForByrefCon(0);
                    }
                    else
                    {
                        // Here we have uninitialized TYP_BYREF
                        initVal = vnStore->VNForFunc(typ, VNF_InitVal, vnStore->VNForIntCon(lclNum));
                    }
                    break;

                default:
                    if (isZeroed)
                    {
                        // By default we will zero init these LclVars
                        initVal = vnStore->VNZeroForType(typ);
                    }
                    else
                    {
                        initVal = vnStore->VNForFunc(typ, VNF_InitVal, vnStore->VNForIntCon(lclNum));
                    }
                    break;
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
    ValueNum memoryInitVal = vnStore->VNForFunc(TYP_REF, VNF_InitVal, vnStore->VNForIntCon(-1)); // Use -1 for memory.
    GetMemoryPerSsaData(SsaConfig::FIRST_SSA_NUM)->m_vnPair.SetBoth(memoryInitVal);
#ifdef DEBUG
    if (verbose)
    {
        printf("Memory Initial Value in BB01 is: " FMT_VN "\n", memoryInitVal);
    }
#endif // DEBUG

    ValueNumberState vs(this);

    // Push the first block.  This has no preds.
    vs.m_toDoAllPredsDone.Push(fgFirstBB);

    while (vs.ToDoExists())
    {
        while (vs.m_toDoAllPredsDone.Size() > 0)
        {
            BasicBlock* toDo = vs.m_toDoAllPredsDone.Pop();
            fgValueNumberBlock(toDo);
            // Record that we've visited "toDo", and add successors to the right sets.
            vs.FinishVisit(toDo);
        }
        // OK, we've run out of blocks whose predecessors are done.  Pick one whose predecessors are not all done,
        // process that.  This may make more "all-done" blocks, so we'll go around the outer loop again --
        // note that this is an "if", not a "while" loop.
        if (vs.m_toDoNotAllPredsDone.Size() > 0)
        {
            BasicBlock* toDo = vs.ChooseFromNotAllPredsDone();
            if (toDo == nullptr)
            {
                continue; // We may have run out, because of completed blocks on the not-all-preds done list.
            }

            fgValueNumberBlock(toDo);
            // Record that we've visited "toDo", and add successors to the right sest.
            vs.FinishVisit(toDo);
        }
    }

#ifdef DEBUG
    JitTestCheckVN();
#endif // DEBUG

    fgVNPassesCompleted++;
}

void Compiler::fgValueNumberBlock(BasicBlock* blk)
{
    compCurBB = blk;

    Statement* stmt = blk->firstStmt();

    // First: visit phi's.  If "newVNForPhis", give them new VN's.  If not,
    // first check to see if all phi args have the same value.
    for (; (stmt != nullptr) && stmt->IsPhiDefnStmt(); stmt = stmt->GetNextStmt())
    {
        GenTree* asg = stmt->GetRootNode();
        assert(asg->OperIs(GT_ASG));

        GenTreeLclVar* newSsaDef = asg->AsOp()->gtGetOp1()->AsLclVar();
        ValueNumPair   phiVNP;
        ValueNumPair   sameVNP;

        for (GenTreePhi::Use& use : asg->AsOp()->gtGetOp2()->AsPhi()->Uses())
        {
            GenTreePhiArg* phiArg         = use.GetNode()->AsPhiArg();
            ValueNum       phiArgSsaNumVN = vnStore->VNForIntCon(phiArg->GetSsaNum());
            ValueNumPair   phiArgVNP      = lvaGetDesc(phiArg)->GetPerSsaData(phiArg->GetSsaNum())->m_vnPair;

            phiArg->gtVNPair = phiArgVNP;

            if (phiVNP.GetLiberal() == ValueNumStore::NoVN)
            {
                // This is the first PHI argument
                phiVNP  = ValueNumPair(phiArgSsaNumVN, phiArgSsaNumVN);
                sameVNP = phiArgVNP;
            }
            else
            {
                phiVNP = vnStore->VNPairForFunc(newSsaDef->TypeGet(), VNF_Phi,
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

#ifdef DEBUG
        // There should be at least to 2 PHI arguments so phiVN's VNs should always be VNF_Phi functions.
        VNFuncApp phiFunc;
        assert(vnStore->GetVNFunc(phiVNP.GetLiberal(), &phiFunc) && (phiFunc.m_func == VNF_Phi));
        assert(vnStore->GetVNFunc(phiVNP.GetConservative(), &phiFunc) && (phiFunc.m_func == VNF_Phi));
#endif

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
    }

    // Now do the same for each MemoryKind.
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        // Is there a phi for this block?
        if (blk->bbMemorySsaPhiFunc[memoryKind] == nullptr)
        {
            fgCurMemoryVN[memoryKind] = GetMemoryPerSsaData(blk->bbMemorySsaNumIn[memoryKind])->m_vnPair.GetLiberal();
            assert(fgCurMemoryVN[memoryKind] != ValueNumStore::NoVN);
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

            unsigned loopNum;
            ValueNum newMemoryVN;
            if (optBlockIsLoopEntry(blk, &loopNum))
            {
                newMemoryVN = fgMemoryVNForLoopSideEffects(memoryKind, blk, loopNum);
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
                    phiAppVN = vnStore->VNForFunc(TYP_REF, VNF_Phi, phiArgSSANumVN, phiAppVN);
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
                    newMemoryVN =
                        vnStore->VNForFunc(TYP_REF, VNF_PhiMemoryDef, vnStore->VNForHandle(ssize_t(blk), 0), phiAppVN);
                }
            }
            GetMemoryPerSsaData(blk->bbMemorySsaNumIn[memoryKind])->m_vnPair.SetLiberal(newMemoryVN);
            fgCurMemoryVN[memoryKind] = newMemoryVN;
            if ((memoryKind == GcHeap) && byrefStatesMatchGcHeapStates)
            {
                // Keep the CurMemoryVNs in sync
                fgCurMemoryVN[ByrefExposed] = newMemoryVN;
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

        for (GenTree* tree = stmt->GetTreeList(); tree != nullptr; tree = tree->gtNext)
        {
            fgValueNumberTree(tree);
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

ValueNum Compiler::fgMemoryVNForLoopSideEffects(MemoryKind  memoryKind,
                                                BasicBlock* entryBlock,
                                                unsigned    innermostLoopNum)
{
    // "loopNum" is the innermost loop for which "blk" is the entry; find the outermost one.
    assert(innermostLoopNum != BasicBlock::NOT_IN_LOOP);
    unsigned loopsInNest = innermostLoopNum;
    unsigned loopNum     = innermostLoopNum;
    while (loopsInNest != BasicBlock::NOT_IN_LOOP)
    {
        if (optLoopTable[loopsInNest].lpEntry != entryBlock)
        {
            break;
        }
        loopNum     = loopsInNest;
        loopsInNest = optLoopTable[loopsInNest].lpParent;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("Computing %s state for block " FMT_BB ", entry block for loops %d to %d:\n",
               memoryKindNames[memoryKind], entryBlock->bbNum, innermostLoopNum, loopNum);
    }
#endif // DEBUG

    // If this loop has memory havoc effects, just use a new, unique VN.
    if (optLoopTable[loopNum].lpLoopHasMemoryHavoc[memoryKind])
    {
        ValueNum res = vnStore->VNForExpr(entryBlock, TYP_REF);
#ifdef DEBUG
        if (verbose)
        {
            printf("  Loop %d has memory havoc effect; heap state is new unique $%x.\n", loopNum, res);
        }
#endif // DEBUG
        return res;
    }

    // Otherwise, find the predecessors of the entry block that are not in the loop.
    // If there is only one such, use its memory value as the "base."  If more than one,
    // use a new unique VN.
    BasicBlock* nonLoopPred          = nullptr;
    bool        multipleNonLoopPreds = false;
    for (flowList* pred = BlockPredsWithEH(entryBlock); pred != nullptr; pred = pred->flNext)
    {
        BasicBlock* predBlock = pred->flBlock;
        if (!optLoopTable[loopNum].lpContains(predBlock))
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
        ValueNum res = vnStore->VNForExpr(entryBlock, TYP_REF);
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
        Compiler::LoopDsc::FieldHandleSet* fieldsMod = optLoopTable[loopNum].lpFieldsModified;
        if (fieldsMod != nullptr)
        {
            for (Compiler::LoopDsc::FieldHandleSet::KeyIterator ki = fieldsMod->Begin(); !ki.Equal(fieldsMod->End());
                 ++ki)
            {
                CORINFO_FIELD_HANDLE fldHnd   = ki.Get();
                ValueNum             fldHndVN = vnStore->VNForHandle(ssize_t(fldHnd), GTF_ICON_FIELD_HDL);

#ifdef DEBUG
                if (verbose)
                {
                    const char* modName;
                    const char* fldName = eeGetFieldName(fldHnd, &modName);
                    printf("     VNForHandle(%s) is " FMT_VN "\n", fldName, fldHndVN);
                }
#endif // DEBUG

                newMemoryVN =
                    vnStore->VNForMapStore(TYP_REF, newMemoryVN, fldHndVN, vnStore->VNForExpr(entryBlock, TYP_REF));
            }
        }
        // Now do the array maps.
        Compiler::LoopDsc::ClassHandleSet* elemTypesMod = optLoopTable[loopNum].lpArrayElemTypesModified;
        if (elemTypesMod != nullptr)
        {
            for (Compiler::LoopDsc::ClassHandleSet::KeyIterator ki = elemTypesMod->Begin();
                 !ki.Equal(elemTypesMod->End()); ++ki)
            {
                CORINFO_CLASS_HANDLE elemClsHnd = ki.Get();

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
                ValueNum uniqueVN   = vnStore->VNForExpr(entryBlock, TYP_REF);
                newMemoryVN         = vnStore->VNForMapStore(TYP_REF, newMemoryVN, elemTypeVN, uniqueVN);
            }
        }
    }
    else
    {
        // If there were any fields/elements modified, this should have been recorded as havoc
        // for ByrefExposed.
        assert(memoryKind == ByrefExposed);
        assert((optLoopTable[loopNum].lpFieldsModified == nullptr) ||
               optLoopTable[loopNum].lpLoopHasMemoryHavoc[memoryKind]);
        assert((optLoopTable[loopNum].lpArrayElemTypesModified == nullptr) ||
               optLoopTable[loopNum].lpLoopHasMemoryHavoc[memoryKind]);
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
    recordGcHeapStore(tree, vnStore->VNForExpr(compCurBB, TYP_REF) DEBUGARG(msg));
}

void Compiler::fgMutateAddressExposedLocal(GenTree* tree DEBUGARG(const char* msg))
{
    // Update the current ByrefExposed VN, and if we're tracking the heap SSA # caused by this node, record it.
    recordAddressExposedLocalStore(tree, vnStore->VNForExpr(compCurBB) DEBUGARG(msg));
}

void Compiler::recordGcHeapStore(GenTree* curTree, ValueNum gcHeapVN DEBUGARG(const char* msg))
{
    // bbMemoryDef must include GcHeap for any block that mutates the GC Heap
    // and GC Heap mutations are also ByrefExposed mutations
    assert((compCurBB->bbMemoryDef & memoryKindSet(GcHeap, ByrefExposed)) == memoryKindSet(GcHeap, ByrefExposed));
    fgCurMemoryVN[GcHeap] = gcHeapVN;

    if (byrefStatesMatchGcHeapStates)
    {
        // Since GcHeap and ByrefExposed share SSA nodes, they need to share
        // value numbers too.
        fgCurMemoryVN[ByrefExposed] = gcHeapVN;
    }
    else
    {
        // GcHeap and ByrefExposed have different defnums and VNs.  We conservatively
        // assume that this GcHeap store may alias any byref load/store, so don't
        // bother trying to record the map/select stuff, and instead just an opaque VN
        // for ByrefExposed
        fgCurMemoryVN[ByrefExposed] = vnStore->VNForExpr(compCurBB);
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
    fgCurMemoryVN[ByrefExposed] = memoryVN;

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
        case TYP_BOOL:
            if (tree->IsCnsIntOrI() && tree->IsIconHandle())
            {
                tree->gtVNPair.SetBoth(
                    vnStore->VNForHandle(ssize_t(tree->AsIntConCommon()->IconValue()), tree->GetIconHandleFlag()));
            }
            else if ((typ == TYP_LONG) || (typ == TYP_ULONG))
            {
                tree->gtVNPair.SetBoth(vnStore->VNForLongCon(INT64(tree->AsIntConCommon()->LngValue())));
            }
            else
            {
                tree->gtVNPair.SetBoth(vnStore->VNForIntCon(int(tree->AsIntConCommon()->IconValue())));
            }
            break;

#ifdef FEATURE_SIMD
        case TYP_SIMD8:
        case TYP_SIMD12:
        case TYP_SIMD16:
        case TYP_SIMD32:

#ifdef TARGET_64BIT
            // Only the zero constant is currently allowed for SIMD types
            //
            assert(tree->AsIntConCommon()->LngValue() == 0);
            tree->gtVNPair.SetBoth(vnStore->VNForLongCon(tree->AsIntConCommon()->LngValue()));
#else // 32BIT
            assert(tree->AsIntConCommon()->IconValue() == 0);
            tree->gtVNPair.SetBoth(vnStore->VNForIntCon(tree->AsIntConCommon()->IconValue()));
#endif
            break;
#endif // FEATURE_SIMD

        case TYP_FLOAT:
            tree->gtVNPair.SetBoth(vnStore->VNForFloatCon((float)tree->AsDblCon()->gtDconVal));
            break;
        case TYP_DOUBLE:
            tree->gtVNPair.SetBoth(vnStore->VNForDoubleCon(tree->AsDblCon()->gtDconVal));
            break;
        case TYP_REF:
            if (tree->AsIntConCommon()->IconValue() == 0)
            {
                tree->gtVNPair.SetBoth(ValueNumStore::VNForNull());
            }
            else
            {
                assert(tree->gtFlags == GTF_ICON_STR_HDL); // Constant object can be only frozen string.
                tree->gtVNPair.SetBoth(
                    vnStore->VNForHandle(ssize_t(tree->AsIntConCommon()->IconValue()), tree->GetIconHandleFlag()));
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
                }
                else
                {
                    tree->gtVNPair.SetBoth(vnStore->VNForByrefCon(tree->AsIntConCommon()->IconValue()));
                }
            }
            break;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// fgValueNumberBlockAssignment: Perform value numbering for block assignments.
//
// Arguments:
//    tree          - the block assignment to be value numbered.
//
// Return Value:
//    None.
//
// Assumptions:
//    'tree' must be a block assignment (GT_INITBLK, GT_COPYBLK, GT_COPYOBJ).

void Compiler::fgValueNumberBlockAssignment(GenTree* tree)
{
    GenTree* lhs = tree->gtGetOp1();
    GenTree* rhs = tree->gtGetOp2();

    if (tree->OperIsInitBlkOp())
    {
        GenTreeLclVarCommon* lclVarTree;
        bool                 isEntire;

        if (tree->DefinesLocal(this, &lclVarTree, &isEntire))
        {
            assert(lclVarTree->gtFlags & GTF_VAR_DEF);
            // Should not have been recorded as updating the GC heap.
            assert(!GetMemorySsaMap(GcHeap)->Lookup(tree));

            unsigned lclNum = lclVarTree->GetLclNum();

            // Ignore vars that we excluded from SSA (for example, because they're address-exposed). They don't have
            // SSA names in which to store VN's on defs.  We'll yield unique VN's when we read from them.
            if (lvaInSsa(lclNum))
            {
                // Should not have been recorded as updating ByrefExposed.
                assert(!GetMemorySsaMap(ByrefExposed)->Lookup(tree));

                unsigned lclDefSsaNum = GetSsaNumForLocalVarDef(lclVarTree);

                ValueNum initBlkVN = ValueNumStore::NoVN;
                GenTree* initConst = rhs;
                if (isEntire && initConst->OperGet() == GT_CNS_INT)
                {
                    unsigned initVal = 0xFF & (unsigned)initConst->AsIntConCommon()->IconValue();
                    if (initVal == 0)
                    {
                        initBlkVN = vnStore->VNZeroForType(lclVarTree->TypeGet());
                    }
                }
                ValueNum lclVarVN = (initBlkVN != ValueNumStore::NoVN)
                                        ? initBlkVN
                                        : vnStore->VNForExpr(compCurBB, var_types(lvaTable[lclNum].lvType));

                lvaTable[lclNum].GetPerSsaData(lclDefSsaNum)->m_vnPair.SetBoth(lclVarVN);
#ifdef DEBUG
                if (verbose)
                {
                    printf("N%03u ", tree->gtSeqNum);
                    Compiler::printTreeID(tree);
                    printf(" ");
                    gtDispNodeName(tree);
                    printf(" V%02u/%d => ", lclNum, lclDefSsaNum);
                    vnPrint(lclVarVN, 1);
                    printf("\n");
                }
#endif // DEBUG
            }
            else if (lvaVarAddrExposed(lclVarTree->GetLclNum()))
            {
                fgMutateAddressExposedLocal(tree DEBUGARG("INITBLK - address-exposed local"));
            }
        }
        else
        {
            // For now, arbitrary side effect on GcHeap/ByrefExposed.
            // TODO-CQ: Why not be complete, and get this case right?
            fgMutateGcHeap(tree DEBUGARG("INITBLK - non local"));
        }
        // Initblock's are of type void.  Give them the void "value" -- they may occur in argument lists, which we
        // want to be able to give VN's to.
        tree->gtVNPair.SetBoth(ValueNumStore::VNForVoid());
    }
    else
    {
        assert(tree->OperIsCopyBlkOp());
        // TODO-Cleanup: We should factor things so that we uniformly rely on "PtrTo" VN's, and
        // the memory cases can be shared with assignments.
        GenTreeLclVarCommon* lclVarTree = nullptr;
        bool                 isEntire   = false;
        // Note that we don't care about exceptions here, since we're only using the values
        // to perform an assignment (which happens after any exceptions are raised...)

        if (tree->DefinesLocal(this, &lclVarTree, &isEntire))
        {
            // Should not have been recorded as updating the GC heap.
            assert(!GetMemorySsaMap(GcHeap)->Lookup(tree));

            unsigned      lhsLclNum = lclVarTree->GetLclNum();
            FieldSeqNode* lhsFldSeq = nullptr;
            // If it's excluded from SSA, don't need to do anything.
            if (lvaInSsa(lhsLclNum))
            {
                // Should not have been recorded as updating ByrefExposed.
                assert(!GetMemorySsaMap(ByrefExposed)->Lookup(tree));

                unsigned lclDefSsaNum = GetSsaNumForLocalVarDef(lclVarTree);

                if (lhs->IsLocalExpr(this, &lclVarTree, &lhsFldSeq))
                {
                    noway_assert(lclVarTree->GetLclNum() == lhsLclNum);
                }
                else
                {
                    GenTree* lhsAddr;
                    if (lhs->OperIsBlk())
                    {
                        lhsAddr = lhs->AsBlk()->Addr();
                    }
                    else
                    {
                        assert(lhs->OperGet() == GT_IND);
                        lhsAddr = lhs->AsOp()->gtOp1;
                    }

                    // For addr-of-local expressions, lib/cons shouldn't matter.
                    assert(lhsAddr->gtVNPair.BothEqual());
                    ValueNum lhsAddrVN = lhsAddr->GetVN(VNK_Liberal);

                    // Unpack the PtrToLoc value number of the address.
                    assert(vnStore->IsVNFunc(lhsAddrVN));

                    VNFuncApp lhsAddrFuncApp;
                    vnStore->GetVNFunc(lhsAddrVN, &lhsAddrFuncApp);

                    assert(lhsAddrFuncApp.m_func == VNF_PtrToLoc);
                    assert(vnStore->IsVNConstant(lhsAddrFuncApp.m_args[0]) &&
                           vnStore->ConstantValue<unsigned>(lhsAddrFuncApp.m_args[0]) == lhsLclNum);

                    lhsFldSeq = vnStore->FieldSeqVNToFieldSeq(lhsAddrFuncApp.m_args[1]);
                }

                // Now we need to get the proper RHS.
                GenTreeLclVarCommon* rhsLclVarTree = nullptr;
                LclVarDsc*           rhsVarDsc     = nullptr;
                FieldSeqNode*        rhsFldSeq     = nullptr;
                ValueNumPair         rhsVNPair;
                bool                 isNewUniq = false;
                if (!rhs->OperIsIndir())
                {
                    if (rhs->IsLocalExpr(this, &rhsLclVarTree, &rhsFldSeq))
                    {
                        unsigned rhsLclNum = rhsLclVarTree->GetLclNum();
                        rhsVarDsc          = &lvaTable[rhsLclNum];
                        if (!lvaInSsa(rhsLclNum) || rhsFldSeq == FieldSeqStore::NotAField())
                        {
                            rhsVNPair.SetBoth(vnStore->VNForExpr(compCurBB, rhsLclVarTree->TypeGet()));
                            isNewUniq = true;
                        }
                        else
                        {
                            rhsVNPair = lvaTable[rhsLclVarTree->GetLclNum()]
                                            .GetPerSsaData(rhsLclVarTree->GetSsaNum())
                                            ->m_vnPair;
                            var_types indType = rhsLclVarTree->TypeGet();

                            rhsVNPair = vnStore->VNPairApplySelectors(rhsVNPair, rhsFldSeq, indType);
                        }
                    }
                    else
                    {
                        rhsVNPair.SetBoth(vnStore->VNForExpr(compCurBB, rhs->TypeGet()));
                        isNewUniq = true;
                    }
                }
                else
                {
                    GenTree*  srcAddr = rhs->AsIndir()->Addr();
                    VNFuncApp srcAddrFuncApp;
                    if (srcAddr->IsLocalAddrExpr(this, &rhsLclVarTree, &rhsFldSeq))
                    {
                        unsigned rhsLclNum = rhsLclVarTree->GetLclNum();
                        rhsVarDsc          = &lvaTable[rhsLclNum];
                        if (!lvaInSsa(rhsLclNum) || rhsFldSeq == FieldSeqStore::NotAField())
                        {
                            isNewUniq = true;
                        }
                        else
                        {
                            rhsVNPair = lvaTable[rhsLclVarTree->GetLclNum()]
                                            .GetPerSsaData(rhsLclVarTree->GetSsaNum())
                                            ->m_vnPair;
                            var_types indType = rhsLclVarTree->TypeGet();

                            rhsVNPair = vnStore->VNPairApplySelectors(rhsVNPair, rhsFldSeq, indType);
                        }
                    }
                    else if (vnStore->GetVNFunc(vnStore->VNLiberalNormalValue(srcAddr->gtVNPair), &srcAddrFuncApp))
                    {
                        if (srcAddrFuncApp.m_func == VNF_PtrToStatic)
                        {
                            var_types     indType            = lclVarTree->TypeGet();
                            ValueNum      fieldSeqVN         = srcAddrFuncApp.m_args[0];
                            FieldSeqNode* fldSeqForStaticVar = vnStore->FieldSeqVNToFieldSeq(fieldSeqVN);
#ifdef DEBUG
                            FieldSeqNode* zeroOffsetFldSeq = nullptr;
                            if (GetZeroOffsetFieldMap()->Lookup(srcAddr, &zeroOffsetFldSeq))
                            {
                                // Check that the zero offset field seq was attached for `srcAddr`.
                                assert(fldSeqForStaticVar->GetTail() == zeroOffsetFldSeq);
                            }
#endif
                            if (fldSeqForStaticVar != FieldSeqStore::NotAField())
                            {
                                // We model statics as indices into GcHeap (which is a subset of ByrefExposed).
                                ValueNum selectedStaticVar;
                                size_t   structSize = 0;
                                selectedStaticVar   = vnStore->VNApplySelectors(VNK_Liberal, fgCurMemoryVN[GcHeap],
                                                                              fldSeqForStaticVar, &structSize);
                                selectedStaticVar =
                                    vnStore->VNApplySelectorsTypeCheck(selectedStaticVar, indType, structSize);

                                rhsVNPair.SetLiberal(selectedStaticVar);
                                rhsVNPair.SetConservative(vnStore->VNForExpr(compCurBB, indType));
                            }
                            else
                            {
                                JITDUMP("    *** Missing field sequence info for Src/RHS of COPYBLK\n");
                                isNewUniq = true;
                            }
                        }
                        else if (srcAddrFuncApp.m_func == VNF_PtrToArrElem)
                        {
                            ValueNum elemLib =
                                fgValueNumberArrIndexVal(nullptr, &srcAddrFuncApp, vnStore->VNForEmptyExcSet());
                            rhsVNPair.SetLiberal(elemLib);
                            rhsVNPair.SetConservative(vnStore->VNForExpr(compCurBB, lclVarTree->TypeGet()));
                        }
                        else
                        {
                            isNewUniq = true;
                        }
                    }
                    else
                    {
                        isNewUniq = true;
                    }
                }

                if (lhsFldSeq == FieldSeqStore::NotAField())
                {
                    // We don't have proper field sequence information for the lhs
                    //
                    JITDUMP("    *** Missing field sequence info for Dst/LHS of COPYBLK\n");
                    isNewUniq = true;
                }

                if (isNewUniq)
                {
                    rhsVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lclVarTree->TypeGet()));
                }
                else // We will assign rhsVNPair into a map[lhsFldSeq]
                {
                    if (lhsFldSeq != nullptr && isEntire)
                    {
                        // This can occur for structs with one field, itself of a struct type.
                        // We are assigning the one field and it is also the entire enclosing struct.
                        //
                        // Use an unique value number for the old map, as this is an an entire assignment
                        // and we won't have any other values in the map
                        ValueNumPair uniqueMap;
                        uniqueMap.SetBoth(vnStore->VNForExpr(compCurBB, lclVarTree->TypeGet()));
                        rhsVNPair = vnStore->VNPairApplySelectorsAssign(uniqueMap, lhsFldSeq, rhsVNPair,
                                                                        lclVarTree->TypeGet(), compCurBB);
                    }
                    else
                    {
                        ValueNumPair oldLhsVNPair =
                            lvaTable[lhsLclNum].GetPerSsaData(lclVarTree->GetSsaNum())->m_vnPair;
                        rhsVNPair = vnStore->VNPairApplySelectorsAssign(oldLhsVNPair, lhsFldSeq, rhsVNPair,
                                                                        lclVarTree->TypeGet(), compCurBB);
                    }
                }

                lvaTable[lhsLclNum].GetPerSsaData(lclDefSsaNum)->m_vnPair = vnStore->VNPNormalPair(rhsVNPair);

#ifdef DEBUG
                if (verbose)
                {
                    printf("Tree ");
                    Compiler::printTreeID(tree);
                    printf(" assigned VN to local var V%02u/%d: ", lhsLclNum, lclDefSsaNum);
                    if (isNewUniq)
                    {
                        printf("new uniq ");
                    }
                    vnpPrint(rhsVNPair, 1);
                    printf("\n");
                }
#endif // DEBUG
            }
            else if (lvaVarAddrExposed(lhsLclNum))
            {
                fgMutateAddressExposedLocal(tree DEBUGARG("COPYBLK - address-exposed local"));
            }
        }
        else
        {
            // For now, arbitrary side effect on GcHeap/ByrefExposed.
            // TODO-CQ: Why not be complete, and get this case right?
            fgMutateGcHeap(tree DEBUGARG("COPYBLK - non local"));
        }
        // Copyblock's are of type void.  Give them the void "value" -- they may occur in argument lists, which we want
        // to be able to give VN's to.
        tree->gtVNPair.SetBoth(ValueNumStore::VNForVoid());
    }
}

void Compiler::fgValueNumberTree(GenTree* tree)
{
    genTreeOps oper = tree->OperGet();

#ifdef FEATURE_SIMD
    if ((JitConfig.JitDisableSimdVN() & 1) == 1)
    {
        // This Jit Config forces the previous behavior of value numbering for SIMD nodes
        if (oper == GT_SIMD)
        {
            tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, TYP_UNKNOWN));
            return;
        }
    }
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
    if ((JitConfig.JitDisableSimdVN() & 2) == 2)
    {
        // This Jit Config forces the previous behavior of value numbering for HW Intrinsic nodes
        if (oper == GT_HWINTRINSIC)
        {
            tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, TYP_UNKNOWN));

            GenTreeHWIntrinsic* hwIntrinsicNode = tree->AsHWIntrinsic();
            assert(hwIntrinsicNode != nullptr);

            // For safety/correctness we must mutate the global heap valuenumber
            //  for any HW intrinsic that performs a memory store operation
            if (hwIntrinsicNode->OperIsMemoryStore())
            {
                fgMutateGcHeap(tree DEBUGARG("HWIntrinsic - MemoryStore"));
            }

            return;
        }
    }
#endif // FEATURE_HW_INTRINSICS

    var_types typ = tree->TypeGet();
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
            case GT_LCL_VAR_ADDR:
            case GT_LCL_FLD_ADDR:
                assert(lvaVarAddrExposed(tree->AsLclVarCommon()->GetLclNum()));
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                break;

            case GT_LCL_VAR:
            {
                GenTreeLclVarCommon* lcl    = tree->AsLclVarCommon();
                unsigned             lclNum = lcl->GetLclNum();
                LclVarDsc*           varDsc = &lvaTable[lclNum];

                if (varDsc->CanBeReplacedWithItsField(this))
                {
                    lclNum = varDsc->lvFieldLclStart;
                    varDsc = &lvaTable[lclNum];
                }

                // Do we have a Use (read) of the LclVar?
                //
                if ((lcl->gtFlags & GTF_VAR_DEF) == 0 ||
                    (lcl->gtFlags & GTF_VAR_USEASG)) // If it is a "pure" def, will handled as part of the assignment.
                {
                    bool          generateUniqueVN = false;
                    FieldSeqNode* zeroOffsetFldSeq = nullptr;

                    // When we have a TYP_BYREF LclVar it can have a zero offset field sequence that needs to be added
                    if (typ == TYP_BYREF)
                    {
                        GetZeroOffsetFieldMap()->Lookup(tree, &zeroOffsetFldSeq);
                    }

                    if (varDsc->lvPromoted && varDsc->lvFieldCnt == 1)
                    {
                        // If the promoted var has only one field var, treat like a use of the field var.
                        lclNum = varDsc->lvFieldLclStart;
                    }

                    if (lcl->GetSsaNum() == SsaConfig::RESERVED_SSA_NUM)
                    {
                        // Not an SSA variable.

                        if (lvaVarAddrExposed(lclNum))
                        {
                            // Address-exposed locals are part of ByrefExposed.
                            ValueNum addrVN = vnStore->VNForFunc(TYP_BYREF, VNF_PtrToLoc, vnStore->VNForIntCon(lclNum),
                                                                 vnStore->VNForFieldSeq(nullptr));
                            ValueNum loadVN = fgValueNumberByrefExposedLoad(typ, addrVN);

                            lcl->gtVNPair.SetBoth(loadVN);
                        }
                        else
                        {
                            // Assign odd cases a new, unique, VN.
                            generateUniqueVN = true;
                        }
                    }
                    else
                    {
                        ValueNumPair wholeLclVarVNP = varDsc->GetPerSsaData(lcl->GetSsaNum())->m_vnPair;

                        // Check for mismatched LclVar size
                        //
                        unsigned typSize = genTypeSize(genActualType(typ));
                        unsigned varSize = genTypeSize(genActualType(varDsc->TypeGet()));

                        if (typSize == varSize)
                        {
                            lcl->gtVNPair = wholeLclVarVNP;
                        }
                        else // mismatched LclVar definition and LclVar use size
                        {
                            if (typSize < varSize)
                            {
                                // the indirection is reading less that the whole LclVar
                                // create a new VN that represent the partial value
                                //
                                ValueNumPair partialLclVarVNP =
                                    vnStore->VNPairForCast(wholeLclVarVNP, typ, varDsc->TypeGet());
                                lcl->gtVNPair = partialLclVarVNP;
                            }
                            else
                            {
                                assert(typSize > varSize);
                                // the indirection is reading beyond the end of the field
                                //
                                generateUniqueVN = true;
                            }
                        }
                    }

                    if (!generateUniqueVN)
                    {
                        // There are a couple of cases where we haven't assigned a valid value number to 'lcl'
                        //
                        if (lcl->gtVNPair.GetLiberal() == ValueNumStore::NoVN)
                        {
                            // So far, we know about two of these cases:
                            // Case 1) We have a local var who has never been defined but it's seen as a use.
                            //         This is the case of storeIndir(addr(lclvar)) = expr.  In this case since we only
                            //         take the address of the variable, this doesn't mean it's a use nor we have to
                            //         initialize it, so in this very rare case, we fabricate a value number.
                            // Case 2) Local variables that represent structs which are assigned using CpBlk.
                            //
                            // Make sure we have either case 1 or case 2
                            //
                            GenTree* nextNode = lcl->gtNext;
                            assert((nextNode->gtOper == GT_ADDR && nextNode->AsOp()->gtOp1 == lcl) ||
                                   varTypeIsStruct(lcl->TypeGet()));

                            // We will assign a unique value number for these
                            //
                            generateUniqueVN = true;
                        }
                    }

                    if (!generateUniqueVN && (zeroOffsetFldSeq != nullptr))
                    {
                        ValueNum addrExtended = vnStore->ExtendPtrVN(lcl, zeroOffsetFldSeq);
                        if (addrExtended != ValueNumStore::NoVN)
                        {
                            lcl->gtVNPair.SetBoth(addrExtended);
                        }
                    }

                    if (generateUniqueVN)
                    {
                        ValueNum uniqVN = vnStore->VNForExpr(compCurBB, lcl->TypeGet());
                        lcl->gtVNPair.SetBoth(uniqVN);
                    }
                }
                else if ((lcl->gtFlags & GTF_VAR_DEF) != 0)
                {
                    // We have a Def (write) of the LclVar

                    // TODO-Review: For the short term, we have a workaround for copyblk/initblk.  Those that use
                    // addrSpillTemp will have a statement like "addrSpillTemp = addr(local)."  If we previously decided
                    // that this block operation defines the local, we will have labeled the "local" node as a DEF
                    // This flag propagates to the "local" on the RHS.  So we'll assume that this is correct,
                    // and treat it as a def (to a new, unique VN).
                    //
                    if (lcl->GetSsaNum() != SsaConfig::RESERVED_SSA_NUM)
                    {
                        ValueNum uniqVN = vnStore->VNForExpr(compCurBB, lcl->TypeGet());
                        varDsc->GetPerSsaData(lcl->GetSsaNum())->m_vnPair.SetBoth(uniqVN);
                    }

                    lcl->gtVNPair = ValueNumPair(); // Avoid confusion -- we don't set the VN of a lcl being defined.
                }
            }
            break;

            case GT_FTN_ADDR:
                // Use the value of the function pointer (actually, a method handle.)
                tree->gtVNPair.SetBoth(
                    vnStore->VNForHandle(ssize_t(tree->AsFptrVal()->gtFptrMethod), GTF_ICON_METHOD_HDL));
                break;

            // This group passes through a value from a child node.
            case GT_RET_EXPR:
                tree->SetVNsFromNode(tree->AsRetExpr()->gtInlineCandidate);
                break;

            case GT_LCL_FLD:
            {
                GenTreeLclFld* lclFld = tree->AsLclFld();
                assert(!lvaInSsa(lclFld->GetLclNum()) || (lclFld->GetFieldSeq() != nullptr));
                // If this is a (full) def, then the variable will be labeled with the new SSA number,
                // which will not have a value.  We skip; it will be handled by one of the assignment-like
                // forms (assignment, or initBlk or copyBlk).
                if (((lclFld->gtFlags & GTF_VAR_DEF) == 0) || (lclFld->gtFlags & GTF_VAR_USEASG))
                {
                    unsigned   lclNum = lclFld->GetLclNum();
                    unsigned   ssaNum = lclFld->GetSsaNum();
                    LclVarDsc* varDsc = &lvaTable[lclNum];

                    var_types indType = tree->TypeGet();
                    if ((lclFld->GetFieldSeq() == FieldSeqStore::NotAField()) || !lvaInSsa(lclFld->GetLclNum()))
                    {
                        // This doesn't represent a proper field access or it's a struct
                        // with overlapping fields that is hard to reason about; return a new unique VN.
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, indType));
                    }
                    else
                    {
                        ValueNumPair lclVNPair = varDsc->GetPerSsaData(ssaNum)->m_vnPair;
                        tree->gtVNPair = vnStore->VNPairApplySelectors(lclVNPair, lclFld->GetFieldSeq(), indType);
                    }
                }
            }
            break;

            // The ones below here all get a new unique VN -- but for various reasons, explained after each.
            case GT_CATCH_ARG:
                // We know nothing about the value of a caught expression.
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                break;

            case GT_CLS_VAR:
                // Skip GT_CLS_VAR nodes that are the LHS of an assignment.  (We labeled these earlier.)
                // We will "evaluate" this as part of the assignment.
                //
                if ((tree->gtFlags & GTF_CLS_VAR_ASG_LHS) == 0)
                {
                    bool isVolatile = (tree->gtFlags & GTF_FLD_VOLATILE) != 0;

                    if (isVolatile)
                    {
                        // For Volatile indirection, first mutate GcHeap/ByrefExposed
                        fgMutateGcHeap(tree DEBUGARG("GTF_FLD_VOLATILE - read"));
                    }

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

                    ValueNumPair clsVarVNPair;

                    // If the static field handle is for a struct type field, then the value of the static
                    // is a "ref" to the boxed struct -- treat it as the address of the static (we assume that a
                    // first element offset will be added to get to the actual struct...)
                    GenTreeClsVar* clsVar = tree->AsClsVar();
                    FieldSeqNode*  fldSeq = clsVar->gtFieldSeq;
                    assert(fldSeq != nullptr); // We need to have one.
                    ValueNum selectedStaticVar = ValueNumStore::NoVN;
                    if (gtIsStaticFieldPtrToBoxedStruct(clsVar->TypeGet(), fldSeq->m_fieldHnd))
                    {
                        clsVarVNPair.SetBoth(
                            vnStore->VNForFunc(TYP_BYREF, VNF_PtrToStatic, vnStore->VNForFieldSeq(fldSeq)));
                    }
                    else
                    {
                        // This is a reference to heap memory.
                        // We model statics as indices into GcHeap (which is a subset of ByrefExposed).

                        FieldSeqNode* fldSeqForStaticVar =
                            GetFieldSeqStore()->CreateSingleton(tree->AsClsVar()->gtClsVarHnd);
                        size_t structSize = 0;
                        selectedStaticVar = vnStore->VNApplySelectors(VNK_Liberal, fgCurMemoryVN[GcHeap],
                                                                      fldSeqForStaticVar, &structSize);
                        selectedStaticVar =
                            vnStore->VNApplySelectorsTypeCheck(selectedStaticVar, tree->TypeGet(), structSize);

                        clsVarVNPair.SetLiberal(selectedStaticVar);
                        // The conservative interpretation always gets a new, unique VN.
                        clsVarVNPair.SetConservative(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    }

                    // The ValueNum returned must represent the full-sized IL-Stack value
                    // If we need to widen this value then we need to introduce a VNF_Cast here to represent
                    // the widened value.    This is necessary since the CSE package can replace all occurrences
                    // of a given ValueNum with a LclVar that is a full-sized IL-Stack value
                    //
                    if (varTypeIsSmall(tree->TypeGet()))
                    {
                        var_types castToType = tree->TypeGet();
                        clsVarVNPair         = vnStore->VNPairForCast(clsVarVNPair, castToType, castToType);
                    }
                    tree->gtVNPair = clsVarVNPair;
                }
                break;

            case GT_MEMORYBARRIER: // Leaf
                // For MEMORYBARRIER add an arbitrary side effect on GcHeap/ByrefExposed.
                fgMutateGcHeap(tree DEBUGARG("MEMORYBARRIER"));
                break;

            // These do not represent values.
            case GT_NO_OP:
            case GT_JMP:   // Control flow
            case GT_LABEL: // Control flow
#if !defined(FEATURE_EH_FUNCLETS)
            case GT_END_LFIN: // Control flow
#endif
            case GT_ARGPLACE:
                // This node is a standin for an argument whose value will be computed later.  (Perhaps it's
                // a register argument, and we don't want to preclude use of the register in arg evaluation yet.)
                // We give this a "fake" value number now; if the call in which it occurs cares about the
                // value (e.g., it's a helper call whose result is a function of argument values) we'll reset
                // this later, when the later args have been assigned VNs.
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                break;

            case GT_PHI_ARG:
                // This one is special because we should never process it in this method: it should
                // always be taken care of, when needed, during pre-processing of a blocks phi definitions.
                assert(false);
                break;

            default:
                unreached();
        }
    }
    else if (GenTree::OperIsSimple(oper))
    {
#ifdef DEBUG
        // Sometimes we query the memory ssa map in an assertion, and need a dummy location for the ignored result.
        unsigned memorySsaNum;
#endif

        // Allow assignments for all enregisterable types to be value numbered (SIMD types)
        if ((oper == GT_ASG) && varTypeIsEnregisterable(tree))
        {
            GenTree* lhs = tree->AsOp()->gtOp1;
            GenTree* rhs = tree->AsOp()->gtOp2;

            ValueNumPair rhsVNPair = rhs->gtVNPair;

            // Is the type being stored different from the type computed by the rhs?
            if ((rhs->TypeGet() != lhs->TypeGet()) && (lhs->OperGet() != GT_BLK))
            {
                // This means that there is an implicit cast on the rhs value
                //
                // We will add a cast function to reflect the possible narrowing of the rhs value
                //
                var_types castToType   = lhs->TypeGet();
                var_types castFromType = rhs->TypeGet();
                bool      isUnsigned   = varTypeIsUnsigned(castFromType);

                rhsVNPair = vnStore->VNPairForCast(rhsVNPair, castToType, castFromType, isUnsigned);
            }

            if (tree->TypeGet() != TYP_VOID)
            {
                // Assignment operators, as expressions, return the value of the RHS.
                tree->gtVNPair = rhsVNPair;
            }

            // Now that we've labeled the assignment as a whole, we don't care about exceptions.
            rhsVNPair = vnStore->VNPNormalPair(rhsVNPair);

            // Record the exeception set for this 'tree' in vnExcSet.
            // First we'll record the exeception set for the rhs and
            // later we will union in the exeception set for the lhs
            //
            ValueNum vnExcSet;

            // Unpack, Norm,Exc for 'rhsVNPair'
            ValueNum vnRhsLibNorm;
            vnStore->VNUnpackExc(rhsVNPair.GetLiberal(), &vnRhsLibNorm, &vnExcSet);

            // Now that we've saved the rhs exeception set, we we will use the normal values.
            rhsVNPair = ValueNumPair(vnRhsLibNorm, vnStore->VNNormalValue(rhsVNPair.GetConservative()));

            // If the types of the rhs and lhs are different then we
            //  may want to change the ValueNumber assigned to the lhs.
            //
            if (rhs->TypeGet() != lhs->TypeGet())
            {
                if (rhs->TypeGet() == TYP_REF)
                {
                    // If we have an unsafe IL assignment of a TYP_REF to a non-ref (typically a TYP_BYREF)
                    // then don't propagate this ValueNumber to the lhs, instead create a new unique VN
                    //
                    rhsVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lhs->TypeGet()));
                }
            }

            // We have to handle the case where the LHS is a comma.  In that case, we don't evaluate the comma,
            // so we give it VNForVoid, and we're really interested in the effective value.
            GenTree* lhsCommaIter = lhs;
            while (lhsCommaIter->OperGet() == GT_COMMA)
            {
                lhsCommaIter->gtVNPair.SetBoth(vnStore->VNForVoid());
                lhsCommaIter = lhsCommaIter->AsOp()->gtOp2;
            }
            lhs = lhs->gtEffectiveVal();

            // Now, record the new VN for an assignment (performing the indicated "state update").
            // It's safe to use gtEffectiveVal here, because the non-last elements of a comma list on the
            // LHS will come before the assignment in evaluation order.
            switch (lhs->OperGet())
            {
                case GT_LCL_VAR:
                {
                    GenTreeLclVarCommon* lcl          = lhs->AsLclVarCommon();
                    unsigned             lclDefSsaNum = GetSsaNumForLocalVarDef(lcl);

                    // Should not have been recorded as updating the GC heap.
                    assert(!GetMemorySsaMap(GcHeap)->Lookup(tree, &memorySsaNum));

                    if (lclDefSsaNum != SsaConfig::RESERVED_SSA_NUM)
                    {
                        // Should not have been recorded as updating ByrefExposed mem.
                        assert(!GetMemorySsaMap(ByrefExposed)->Lookup(tree, &memorySsaNum));

                        assert(rhsVNPair.GetLiberal() != ValueNumStore::NoVN);

                        lhs->gtVNPair                                                    = rhsVNPair;
                        lvaTable[lcl->GetLclNum()].GetPerSsaData(lclDefSsaNum)->m_vnPair = rhsVNPair;

#ifdef DEBUG
                        if (verbose)
                        {
                            printf("N%03u ", lhs->gtSeqNum);
                            Compiler::printTreeID(lhs);
                            printf(" ");
                            gtDispNodeName(lhs);
                            gtDispLeaf(lhs, nullptr);
                            printf(" => ");
                            vnpPrint(lhs->gtVNPair, 1);
                            printf("\n");
                        }
#endif // DEBUG
                    }
                    else if (lvaVarAddrExposed(lcl->GetLclNum()))
                    {
                        // We could use MapStore here and MapSelect on reads of address-exposed locals
                        // (using the local nums as selectors) to get e.g. propagation of values
                        // through address-taken locals in regions of code with no calls or byref
                        // writes.
                        // For now, just use a new opaque VN.
                        ValueNum heapVN = vnStore->VNForExpr(compCurBB);
                        recordAddressExposedLocalStore(tree, heapVN DEBUGARG("local assign"));
                    }
#ifdef DEBUG
                    else
                    {
                        if (verbose)
                        {
                            JITDUMP("Tree ");
                            Compiler::printTreeID(tree);
                            printf(" assigns to non-address-taken local var V%02u; excluded from SSA, so value not "
                                   "tracked.\n",
                                   lcl->GetLclNum());
                        }
                    }
#endif // DEBUG
                }
                break;
                case GT_LCL_FLD:
                {
                    GenTreeLclFld* lclFld       = lhs->AsLclFld();
                    unsigned       lclDefSsaNum = GetSsaNumForLocalVarDef(lclFld);

                    // Should not have been recorded as updating the GC heap.
                    assert(!GetMemorySsaMap(GcHeap)->Lookup(tree, &memorySsaNum));

                    if (lclDefSsaNum != SsaConfig::RESERVED_SSA_NUM)
                    {
                        ValueNumPair newLhsVNPair;
                        // Is this a full definition?
                        if ((lclFld->gtFlags & GTF_VAR_USEASG) == 0)
                        {
                            assert(!lclFld->IsPartialLclFld(this));
                            assert(rhsVNPair.GetLiberal() != ValueNumStore::NoVN);
                            newLhsVNPair = rhsVNPair;
                        }
                        else
                        {
                            // We should never have a null field sequence here.
                            assert(lclFld->GetFieldSeq() != nullptr);
                            if (lclFld->GetFieldSeq() == FieldSeqStore::NotAField())
                            {
                                // We don't know what field this represents.  Assign a new VN to the whole variable
                                // (since we may be writing to an unknown portion of it.)
                                newLhsVNPair.SetBoth(
                                    vnStore->VNForExpr(compCurBB, lvaGetActualType(lclFld->GetLclNum())));
                            }
                            else
                            {
                                // We do know the field sequence.
                                // The "lclFld" node will be labeled with the SSA number of its "use" identity
                                // (we looked in a side table above for its "def" identity).  Look up that value.
                                ValueNumPair oldLhsVNPair =
                                    lvaTable[lclFld->GetLclNum()].GetPerSsaData(lclFld->GetSsaNum())->m_vnPair;
                                newLhsVNPair = vnStore->VNPairApplySelectorsAssign(oldLhsVNPair, lclFld->GetFieldSeq(),
                                                                                   rhsVNPair, // Pre-value.
                                                                                   lclFld->TypeGet(), compCurBB);
                            }
                        }
                        lvaTable[lclFld->GetLclNum()].GetPerSsaData(lclDefSsaNum)->m_vnPair = newLhsVNPair;
                        lhs->gtVNPair                                                       = newLhsVNPair;
#ifdef DEBUG
                        if (verbose)
                        {
                            if (lhs->gtVNPair.GetLiberal() != ValueNumStore::NoVN)
                            {
                                printf("N%03u ", lhs->gtSeqNum);
                                Compiler::printTreeID(lhs);
                                printf(" ");
                                gtDispNodeName(lhs);
                                gtDispLeaf(lhs, nullptr);
                                printf(" => ");
                                vnpPrint(lhs->gtVNPair, 1);
                                printf("\n");
                            }
                        }
#endif // DEBUG
                    }
                    else if (lvaVarAddrExposed(lclFld->GetLclNum()))
                    {
                        // This side-effects ByrefExposed.  Just use a new opaque VN.
                        // As with GT_LCL_VAR, we could probably use MapStore here and MapSelect at corresponding
                        // loads, but to do so would have to identify the subset of address-exposed locals
                        // whose fields can be disambiguated.
                        ValueNum heapVN = vnStore->VNForExpr(compCurBB);
                        recordAddressExposedLocalStore(tree, heapVN DEBUGARG("local field assign"));
                    }
                }
                break;

                case GT_PHI_ARG:
                    noway_assert(!"Phi arg cannot be LHS.");
                    break;

                case GT_OBJ:
                    noway_assert(!"GT_OBJ can not be LHS when (tree->TypeGet() != TYP_STRUCT)!");
                    break;

                case GT_BLK:
                    __fallthrough;

                case GT_IND:
                {
                    bool isVolatile = (lhs->gtFlags & GTF_IND_VOLATILE) != 0;

                    if (isVolatile)
                    {
                        // For Volatile store indirection, first mutate GcHeap/ByrefExposed
                        fgMutateGcHeap(lhs DEBUGARG("GTF_IND_VOLATILE - store"));
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lhs->TypeGet()));
                    }

                    GenTree* arg = lhs->AsOp()->gtOp1;

                    // Indicates whether the argument of the IND is the address of a local.
                    bool wasLocal = false;

                    lhs->gtVNPair = rhsVNPair;

                    VNFuncApp funcApp;
                    ValueNum  argVN = arg->gtVNPair.GetLiberal();

                    bool argIsVNFunc = vnStore->GetVNFunc(vnStore->VNNormalValue(argVN), &funcApp);

                    // Is this an assignment to a (field of, perhaps) a local?
                    // If it is a PtrToLoc, lib and cons VNs will be the same.
                    if (argIsVNFunc)
                    {
                        if (funcApp.m_func == VNF_PtrToLoc)
                        {
                            assert(arg->gtVNPair.BothEqual()); // If it's a PtrToLoc, lib/cons shouldn't differ.
                            assert(vnStore->IsVNConstant(funcApp.m_args[0]));
                            unsigned lclNum = vnStore->ConstantValue<unsigned>(funcApp.m_args[0]);

                            wasLocal = true;

                            if (lvaInSsa(lclNum))
                            {
                                FieldSeqNode* fieldSeq = vnStore->FieldSeqVNToFieldSeq(funcApp.m_args[1]);

                                // Either "arg" is the address of (part of) a local itself, or else we have
                                // a "rogue" PtrToLoc, one that should have made the local in question
                                // address-exposed.  Assert on that.
                                GenTreeLclVarCommon* lclVarTree   = nullptr;
                                bool                 isEntire     = false;
                                unsigned             lclDefSsaNum = SsaConfig::RESERVED_SSA_NUM;
                                ValueNumPair         newLhsVNPair;

                                if (arg->DefinesLocalAddr(this, genTypeSize(lhs->TypeGet()), &lclVarTree, &isEntire))
                                {
                                    // The local #'s should agree.
                                    assert(lclNum == lclVarTree->GetLclNum());

                                    if (fieldSeq == FieldSeqStore::NotAField())
                                    {
                                        // We don't know where we're storing, so give the local a new, unique VN.
                                        // Do this by considering it an "entire" assignment, with an unknown RHS.
                                        isEntire = true;
                                        rhsVNPair.SetBoth(vnStore->VNForExpr(compCurBB, lclVarTree->TypeGet()));
                                    }

                                    if (isEntire)
                                    {
                                        newLhsVNPair = rhsVNPair;
                                        lclDefSsaNum = lclVarTree->GetSsaNum();
                                    }
                                    else
                                    {
                                        // Don't use the lclVarTree's VN: if it's a local field, it will
                                        // already be dereferenced by it's field sequence.
                                        ValueNumPair oldLhsVNPair = lvaTable[lclVarTree->GetLclNum()]
                                                                        .GetPerSsaData(lclVarTree->GetSsaNum())
                                                                        ->m_vnPair;
                                        lclDefSsaNum = GetSsaNumForLocalVarDef(lclVarTree);
                                        newLhsVNPair =
                                            vnStore->VNPairApplySelectorsAssign(oldLhsVNPair, fieldSeq, rhsVNPair,
                                                                                lhs->TypeGet(), compCurBB);
                                    }
                                    lvaTable[lclNum].GetPerSsaData(lclDefSsaNum)->m_vnPair = newLhsVNPair;
                                }
                                else
                                {
                                    unreached(); // "Rogue" PtrToLoc, as discussed above.
                                }
#ifdef DEBUG
                                if (verbose)
                                {
                                    printf("Tree ");
                                    Compiler::printTreeID(tree);
                                    printf(" assigned VN to local var V%02u/%d: VN ", lclNum, lclDefSsaNum);
                                    vnpPrint(newLhsVNPair, 1);
                                    printf("\n");
                                }
#endif // DEBUG
                            }
                            else if (lvaVarAddrExposed(lclNum))
                            {
                                // Need to record the effect on ByrefExposed.
                                // We could use MapStore here and MapSelect on reads of address-exposed locals
                                // (using the local nums as selectors) to get e.g. propagation of values
                                // through address-taken locals in regions of code with no calls or byref
                                // writes.
                                // For now, just use a new opaque VN.
                                ValueNum heapVN = vnStore->VNForExpr(compCurBB);
                                recordAddressExposedLocalStore(tree, heapVN DEBUGARG("PtrToLoc indir"));
                            }
                        }
                    }

                    // Was the argument of the GT_IND the address of a local, handled above?
                    if (!wasLocal)
                    {
                        GenTree*      obj          = nullptr;
                        GenTree*      staticOffset = nullptr;
                        FieldSeqNode* fldSeq       = nullptr;

                        // Is the LHS an array index expression?
                        if (argIsVNFunc && funcApp.m_func == VNF_PtrToArrElem)
                        {
                            CORINFO_CLASS_HANDLE elemTypeEq =
                                CORINFO_CLASS_HANDLE(vnStore->ConstantValue<ssize_t>(funcApp.m_args[0]));
                            ValueNum      arrVN  = funcApp.m_args[1];
                            ValueNum      inxVN  = funcApp.m_args[2];
                            FieldSeqNode* fldSeq = vnStore->FieldSeqVNToFieldSeq(funcApp.m_args[3]);
#ifdef DEBUG
                            if (verbose)
                            {
                                printf("Tree ");
                                Compiler::printTreeID(tree);
                                printf(" assigns to an array element:\n");
                            }
#endif // DEBUG

                            ValueNum heapVN = fgValueNumberArrIndexAssign(elemTypeEq, arrVN, inxVN, fldSeq,
                                                                          rhsVNPair.GetLiberal(), lhs->TypeGet());
                            recordGcHeapStore(tree, heapVN DEBUGARG("ArrIndexAssign (case 1)"));
                        }
                        // It may be that we haven't parsed it yet.  Try.
                        else if (lhs->gtFlags & GTF_IND_ARR_INDEX)
                        {
                            ArrayInfo arrInfo;
                            bool      b = GetArrayInfoMap()->Lookup(lhs, &arrInfo);
                            assert(b);
                            ValueNum      arrVN  = ValueNumStore::NoVN;
                            ValueNum      inxVN  = ValueNumStore::NoVN;
                            FieldSeqNode* fldSeq = nullptr;

                            // Try to parse it.
                            GenTree* arr = nullptr;
                            arg->ParseArrayAddress(this, &arrInfo, &arr, &inxVN, &fldSeq);
                            if (arr == nullptr)
                            {
                                fgMutateGcHeap(tree DEBUGARG("assignment to unparseable array expression"));
                                return;
                            }
                            // Otherwise, parsing succeeded.

                            // Need to form H[arrType][arr][ind][fldSeq] = rhsVNPair.GetLiberal()

                            // Get the element type equivalence class representative.
                            CORINFO_CLASS_HANDLE elemTypeEq =
                                EncodeElemType(arrInfo.m_elemType, arrInfo.m_elemStructType);
                            arrVN = arr->gtVNPair.GetLiberal();

                            FieldSeqNode* zeroOffsetFldSeq = nullptr;
                            if (GetZeroOffsetFieldMap()->Lookup(arg, &zeroOffsetFldSeq))
                            {
                                fldSeq = GetFieldSeqStore()->Append(fldSeq, zeroOffsetFldSeq);
                            }

                            ValueNum heapVN = fgValueNumberArrIndexAssign(elemTypeEq, arrVN, inxVN, fldSeq,
                                                                          rhsVNPair.GetLiberal(), lhs->TypeGet());
                            recordGcHeapStore(tree, heapVN DEBUGARG("ArrIndexAssign (case 2)"));
                        }
                        else if (arg->IsFieldAddr(this, &obj, &staticOffset, &fldSeq))
                        {
                            if (fldSeq == FieldSeqStore::NotAField())
                            {
                                fgMutateGcHeap(tree DEBUGARG("NotAField"));
                            }
                            else
                            {
                                assert(fldSeq != nullptr);
#ifdef DEBUG
                                CORINFO_CLASS_HANDLE fldCls = info.compCompHnd->getFieldClass(fldSeq->m_fieldHnd);
                                if (obj != nullptr)
                                {
                                    // Make sure that the class containing it is not a value class (as we are expecting
                                    // an instance field)
                                    assert((info.compCompHnd->getClassAttribs(fldCls) & CORINFO_FLG_VALUECLASS) == 0);
                                    assert(staticOffset == nullptr);
                                }
#endif // DEBUG

                                // Get the first (instance or static) field from field seq.  GcHeap[field] will yield
                                // the "field map".
                                if (fldSeq->IsFirstElemFieldSeq())
                                {
                                    fldSeq = fldSeq->m_next;
                                    assert(fldSeq != nullptr);
                                }

                                // Get a field sequence for just the first field in the sequence
                                //
                                FieldSeqNode* firstFieldOnly = GetFieldSeqStore()->CreateSingleton(fldSeq->m_fieldHnd);

                                // The final field in the sequence will need to match the 'indType'
                                var_types indType = lhs->TypeGet();
                                ValueNum  fldMapVN =
                                    vnStore->VNApplySelectors(VNK_Liberal, fgCurMemoryVN[GcHeap], firstFieldOnly);

                                // The type of the field is "struct" if there are more fields in the sequence,
                                // otherwise it is the type returned from VNApplySelectors above.
                                var_types firstFieldType = vnStore->TypeOfVN(fldMapVN);

                                // The value number from the rhs of the assignment
                                ValueNum storeVal    = rhsVNPair.GetLiberal();
                                ValueNum newFldMapVN = ValueNumStore::NoVN;

                                // when (obj != nullptr) we have an instance field, otherwise a static field
                                // when (staticOffset != nullptr) it represents a offset into a static or the call to
                                // Shared Static Base
                                if ((obj != nullptr) || (staticOffset != nullptr))
                                {
                                    ValueNum valAtAddr = fldMapVN;
                                    ValueNum normVal   = ValueNumStore::NoVN;

                                    if (obj != nullptr)
                                    {
                                        // Unpack, Norm,Exc for 'obj'
                                        ValueNum vnObjExcSet;
                                        vnStore->VNUnpackExc(obj->gtVNPair.GetLiberal(), &normVal, &vnObjExcSet);
                                        vnExcSet = vnStore->VNExcSetUnion(vnExcSet, vnObjExcSet);

                                        // construct the ValueNumber for 'fldMap at obj'
                                        valAtAddr =
                                            vnStore->VNForMapSelect(VNK_Liberal, firstFieldType, fldMapVN, normVal);
                                    }
                                    else // (staticOffset != nullptr)
                                    {
                                        // construct the ValueNumber for 'fldMap at staticOffset'
                                        normVal = vnStore->VNLiberalNormalValue(staticOffset->gtVNPair);
                                        valAtAddr =
                                            vnStore->VNForMapSelect(VNK_Liberal, firstFieldType, fldMapVN, normVal);
                                    }
                                    // Now get rid of any remaining struct field dereferences. (if they exist)
                                    if (fldSeq->m_next)
                                    {
                                        storeVal =
                                            vnStore->VNApplySelectorsAssign(VNK_Liberal, valAtAddr, fldSeq->m_next,
                                                                            storeVal, indType, compCurBB);
                                    }

                                    // From which we can construct the new ValueNumber for 'fldMap at normVal'
                                    newFldMapVN = vnStore->VNForMapStore(vnStore->TypeOfVN(fldMapVN), fldMapVN, normVal,
                                                                         storeVal);
                                }
                                else
                                {
                                    // plain static field

                                    // Now get rid of any remaining struct field dereferences. (if they exist)
                                    if (fldSeq->m_next)
                                    {
                                        storeVal =
                                            vnStore->VNApplySelectorsAssign(VNK_Liberal, fldMapVN, fldSeq->m_next,
                                                                            storeVal, indType, compCurBB);
                                    }

                                    newFldMapVN = vnStore->VNApplySelectorsAssign(VNK_Liberal, fgCurMemoryVN[GcHeap],
                                                                                  fldSeq, storeVal, indType, compCurBB);
                                }

                                // It is not strictly necessary to set the lhs value number,
                                // but the dumps read better with it set to the 'storeVal' that we just computed
                                lhs->gtVNPair.SetBoth(storeVal);

                                // Update the field map for firstField in GcHeap to this new value.
                                ValueNum heapVN =
                                    vnStore->VNApplySelectorsAssign(VNK_Liberal, fgCurMemoryVN[GcHeap], firstFieldOnly,
                                                                    newFldMapVN, indType, compCurBB);

                                recordGcHeapStore(tree, heapVN DEBUGARG("StoreField"));
                            }
                        }
                        else
                        {
                            GenTreeLclVarCommon* lclVarTree = nullptr;
                            bool                 isLocal    = tree->DefinesLocal(this, &lclVarTree);

                            if (isLocal && lvaVarAddrExposed(lclVarTree->GetLclNum()))
                            {
                                // Store to address-exposed local; need to record the effect on ByrefExposed.
                                // We could use MapStore here and MapSelect on reads of address-exposed locals
                                // (using the local nums as selectors) to get e.g. propagation of values
                                // through address-taken locals in regions of code with no calls or byref
                                // writes.
                                // For now, just use a new opaque VN.
                                ValueNum memoryVN = vnStore->VNForExpr(compCurBB);
                                recordAddressExposedLocalStore(tree, memoryVN DEBUGARG("PtrToLoc indir"));
                            }
                            else if (!isLocal)
                            {
                                // If it doesn't define a local, then it might update GcHeap/ByrefExposed.
                                // For the new ByrefExposed VN, we could use an operator here like
                                // VNF_ByrefExposedStore that carries the VNs of the pointer and RHS, then
                                // at byref loads if the current ByrefExposed VN happens to be
                                // VNF_ByrefExposedStore with the same pointer VN, we could propagate the
                                // VN from the RHS to the VN for the load.  This would e.g. allow tracking
                                // values through assignments to out params.  For now, just model this
                                // as an opaque GcHeap/ByrefExposed mutation.
                                fgMutateGcHeap(tree DEBUGARG("assign-of-IND"));
                            }
                        }
                    }

                    // We don't actually evaluate an IND on the LHS, so give it the Void value.
                    tree->gtVNPair.SetBoth(vnStore->VNForVoid());
                }
                break;

                case GT_CLS_VAR:
                {
                    bool isVolatile = (lhs->gtFlags & GTF_FLD_VOLATILE) != 0;

                    if (isVolatile)
                    {
                        // For Volatile store indirection, first mutate GcHeap/ByrefExposed
                        fgMutateGcHeap(lhs DEBUGARG("GTF_CLS_VAR - store")); // always change fgCurMemoryVN
                    }

                    // We model statics as indices into GcHeap (which is a subset of ByrefExposed).
                    FieldSeqNode* fldSeqForStaticVar =
                        GetFieldSeqStore()->CreateSingleton(lhs->AsClsVar()->gtClsVarHnd);
                    assert(fldSeqForStaticVar != FieldSeqStore::NotAField());

                    ValueNum storeVal = rhsVNPair.GetLiberal(); // The value number from the rhs of the assignment
                    storeVal = vnStore->VNApplySelectorsAssign(VNK_Liberal, fgCurMemoryVN[GcHeap], fldSeqForStaticVar,
                                                               storeVal, lhs->TypeGet(), compCurBB);

                    // It is not strictly necessary to set the lhs value number,
                    // but the dumps read better with it set to the 'storeVal' that we just computed
                    lhs->gtVNPair.SetBoth(storeVal);

                    // bbMemoryDef must include GcHeap for any block that mutates the GC heap
                    assert((compCurBB->bbMemoryDef & memoryKindSet(GcHeap)) != 0);

                    // Update the field map for the fgCurMemoryVN and SSA for the tree
                    recordGcHeapStore(tree, storeVal DEBUGARG("Static Field store"));
                }
                break;

                default:
                    assert(!"Unknown node for lhs of assignment!");

                    // For Unknown stores, mutate GcHeap/ByrefExposed
                    fgMutateGcHeap(lhs DEBUGARG("Unkwown Assignment - store")); // always change fgCurMemoryVN
                    break;
            }
        }
        // Other kinds of assignment: initblk and copyblk.
        else if (oper == GT_ASG && (tree->TypeGet() == TYP_STRUCT))
        {
            fgValueNumberBlockAssignment(tree);
        }
        else if (oper == GT_ADDR)
        {
            // We have special representations for byrefs to lvalues.
            GenTree* arg = tree->AsOp()->gtOp1;
            if (arg->OperIsLocal())
            {
                FieldSeqNode* fieldSeq = nullptr;
                ValueNum      newVN    = ValueNumStore::NoVN;
                if (!lvaInSsa(arg->AsLclVarCommon()->GetLclNum()))
                {
                    newVN = vnStore->VNForExpr(compCurBB, TYP_BYREF);
                }
                else if (arg->OperGet() == GT_LCL_FLD)
                {
                    fieldSeq = arg->AsLclFld()->GetFieldSeq();
                    if (fieldSeq == nullptr)
                    {
                        // Local field with unknown field seq -- not a precise pointer.
                        newVN = vnStore->VNForExpr(compCurBB, TYP_BYREF);
                    }
                }
                if (newVN == ValueNumStore::NoVN)
                {
                    assert(arg->AsLclVarCommon()->GetSsaNum() != ValueNumStore::NoVN);
                    newVN = vnStore->VNForFunc(TYP_BYREF, VNF_PtrToLoc,
                                               vnStore->VNForIntCon(arg->AsLclVarCommon()->GetLclNum()),
                                               vnStore->VNForFieldSeq(fieldSeq));
                }
                tree->gtVNPair.SetBoth(newVN);
            }
            else if ((arg->gtOper == GT_IND) || arg->OperIsBlk())
            {
                // Usually the ADDR and IND just cancel out...
                // except when this GT_ADDR has a valid zero-offset field sequence
                //
                FieldSeqNode* zeroOffsetFieldSeq = nullptr;
                if (GetZeroOffsetFieldMap()->Lookup(tree, &zeroOffsetFieldSeq) &&
                    (zeroOffsetFieldSeq != FieldSeqStore::NotAField()))
                {
                    ValueNum addrExtended = vnStore->ExtendPtrVN(arg->AsOp()->gtOp1, zeroOffsetFieldSeq);
                    if (addrExtended != ValueNumStore::NoVN)
                    {
                        tree->gtVNPair.SetBoth(addrExtended); // We don't care about lib/cons differences for addresses.
                    }
                    else
                    {
                        // ExtendPtrVN returned a failure result
                        // So give this address a new unique value
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, TYP_BYREF));
                    }
                }
                else
                {
                    // They just cancel, so fetch the ValueNumber from the op1 of the GT_IND node.
                    //
                    GenTree* addr  = arg->AsIndir()->Addr();
                    tree->gtVNPair = addr->gtVNPair;

                    // For the CSE phase mark the address as GTF_DONT_CSE
                    // because it will end up with the same value number as tree (the GT_ADDR).
                    addr->gtFlags |= GTF_DONT_CSE;
                }
            }
            else
            {
                // May be more cases to do here!  But we'll punt for now.
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, TYP_BYREF));
            }
        }
        else if ((oper == GT_IND) || GenTree::OperIsBlk(oper))
        {
            // So far, we handle cases in which the address is a ptr-to-local, or if it's
            // a pointer to an object field or array element.  Other cases become uses of
            // the current ByrefExposed value and the pointer value, so that at least we
            // can recognize redundant loads with no stores between them.
            GenTree*             addr         = tree->AsIndir()->Addr();
            GenTreeLclVarCommon* lclVarTree   = nullptr;
            FieldSeqNode*        fldSeq2      = nullptr;
            GenTree*             obj          = nullptr;
            GenTree*             staticOffset = nullptr;
            bool                 isVolatile   = (tree->gtFlags & GTF_IND_VOLATILE) != 0;

            // See if the addr has any exceptional part.
            ValueNumPair addrNvnp;
            ValueNumPair addrXvnp;
            vnStore->VNPUnpackExc(addr->gtVNPair, &addrNvnp, &addrXvnp);

            // Is the dereference immutable?  If so, model it as referencing the read-only heap.
            if (tree->gtFlags & GTF_IND_INVARIANT)
            {
                assert(!isVolatile); // We don't expect both volatile and invariant
                tree->gtVNPair =
                    ValueNumPair(vnStore->VNForMapSelect(VNK_Liberal, TYP_REF, ValueNumStore::VNForROH(),
                                                         addrNvnp.GetLiberal()),
                                 vnStore->VNForMapSelect(VNK_Conservative, TYP_REF, ValueNumStore::VNForROH(),
                                                         addrNvnp.GetConservative()));
                tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
            }
            else if (isVolatile)
            {
                // For Volatile indirection, mutate GcHeap/ByrefExposed
                fgMutateGcHeap(tree DEBUGARG("GTF_IND_VOLATILE - read"));

                // The value read by the GT_IND can immediately change
                ValueNum newUniq = vnStore->VNForExpr(compCurBB, tree->TypeGet());
                tree->gtVNPair   = vnStore->VNPWithExc(ValueNumPair(newUniq, newUniq), addrXvnp);
            }
            // We always want to evaluate the LHS when the GT_IND node is marked with GTF_IND_ARR_INDEX
            // as this will relabel the GT_IND child correctly using the VNF_PtrToArrElem
            else if ((tree->gtFlags & GTF_IND_ARR_INDEX) != 0)
            {
                ArrayInfo arrInfo;
                bool      b = GetArrayInfoMap()->Lookup(tree, &arrInfo);
                assert(b);

                ValueNum      inxVN  = ValueNumStore::NoVN;
                FieldSeqNode* fldSeq = nullptr;

                // Try to parse it.
                GenTree* arr = nullptr;
                addr->ParseArrayAddress(this, &arrInfo, &arr, &inxVN, &fldSeq);
                if (arr == nullptr)
                {
                    tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    return;
                }
                assert(fldSeq != FieldSeqStore::NotAField());

                // Otherwise...
                // Need to form H[arrType][arr][ind][fldSeq]
                // Get the array element type equivalence class rep.
                CORINFO_CLASS_HANDLE elemTypeEq   = EncodeElemType(arrInfo.m_elemType, arrInfo.m_elemStructType);
                ValueNum             elemTypeEqVN = vnStore->VNForHandle(ssize_t(elemTypeEq), GTF_ICON_CLASS_HDL);
                JITDUMP("    VNForHandle(arrElemType: %s) is " FMT_VN "\n",
                        (arrInfo.m_elemType == TYP_STRUCT) ? eeGetClassName(arrInfo.m_elemStructType)
                                                           : varTypeName(arrInfo.m_elemType),
                        elemTypeEqVN)

                // We take the "VNNormalValue"s here, because if either has exceptional outcomes, they will be captured
                // as part of the value of the composite "addr" operation...
                ValueNum arrVN = vnStore->VNLiberalNormalValue(arr->gtVNPair);
                inxVN          = vnStore->VNNormalValue(inxVN);

                // Additionally, relabel the address with a PtrToArrElem value number.
                ValueNum fldSeqVN = vnStore->VNForFieldSeq(fldSeq);
                ValueNum elemAddr =
                    vnStore->VNForFunc(TYP_BYREF, VNF_PtrToArrElem, elemTypeEqVN, arrVN, inxVN, fldSeqVN);

                // The aggregate "addr" VN should have had all the exceptions bubble up...
                elemAddr = vnStore->VNWithExc(elemAddr, addrXvnp.GetLiberal());
                addr->gtVNPair.SetBoth(elemAddr);
#ifdef DEBUG
                if (verbose)
                {
                    printf("  Relabeled IND_ARR_INDEX address node ");
                    Compiler::printTreeID(addr);
                    printf(" with l:" FMT_VN ": ", elemAddr);
                    vnStore->vnDump(this, elemAddr);
                    printf("\n");
                    if (vnStore->VNNormalValue(elemAddr) != elemAddr)
                    {
                        printf("      [" FMT_VN " is: ", vnStore->VNNormalValue(elemAddr));
                        vnStore->vnDump(this, vnStore->VNNormalValue(elemAddr));
                        printf("]\n");
                    }
                }
#endif // DEBUG

                // We now need to retrieve the value number for the array element value
                // and give this value number to the GT_IND node 'tree'
                // We do this whenever we have an rvalue, but we don't do it for a
                // normal LHS assignment into an array element.
                //
                if ((tree->gtFlags & GTF_IND_ASG_LHS) == 0)
                {
                    fgValueNumberArrIndexVal(tree, elemTypeEq, arrVN, inxVN, addrXvnp.GetLiberal(), fldSeq);
                }
            }
            // In general we skip GT_IND nodes on that are the LHS of an assignment.  (We labeled these earlier.)
            // We will "evaluate" this as part of the assignment.
            else if ((tree->gtFlags & GTF_IND_ASG_LHS) == 0)
            {
                FieldSeqNode* localFldSeq = nullptr;
                VNFuncApp     funcApp;

                // Is it a local or a heap address?
                if (addr->IsLocalAddrExpr(this, &lclVarTree, &localFldSeq) && lvaInSsa(lclVarTree->GetLclNum()))
                {
                    unsigned   lclNum = lclVarTree->GetLclNum();
                    unsigned   ssaNum = lclVarTree->GetSsaNum();
                    LclVarDsc* varDsc = &lvaTable[lclNum];

                    if ((localFldSeq == FieldSeqStore::NotAField()) || (localFldSeq == nullptr))
                    {
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    }
                    else
                    {
                        var_types    indType   = tree->TypeGet();
                        ValueNumPair lclVNPair = varDsc->GetPerSsaData(ssaNum)->m_vnPair;
                        tree->gtVNPair         = vnStore->VNPairApplySelectors(lclVNPair, localFldSeq, indType);
                        ;
                    }
                    tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
                }
                else if (vnStore->GetVNFunc(addrNvnp.GetLiberal(), &funcApp) && funcApp.m_func == VNF_PtrToStatic)
                {
                    var_types indType    = tree->TypeGet();
                    ValueNum  fieldSeqVN = funcApp.m_args[0];

                    FieldSeqNode* fldSeqForStaticVar = vnStore->FieldSeqVNToFieldSeq(fieldSeqVN);

                    if (fldSeqForStaticVar != FieldSeqStore::NotAField())
                    {
                        ValueNum selectedStaticVar;
                        // We model statics as indices into the GcHeap (which is a subset of ByrefExposed).
                        size_t structSize = 0;
                        selectedStaticVar = vnStore->VNApplySelectors(VNK_Liberal, fgCurMemoryVN[GcHeap],
                                                                      fldSeqForStaticVar, &structSize);
                        selectedStaticVar = vnStore->VNApplySelectorsTypeCheck(selectedStaticVar, indType, structSize);

                        tree->gtVNPair.SetLiberal(selectedStaticVar);
                        tree->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, indType));
                    }
                    else
                    {
                        JITDUMP("    *** Missing field sequence info for VNF_PtrToStatic value GT_IND\n");
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, indType)); //  a new unique value number
                    }
                    tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
                }
                else if (vnStore->GetVNFunc(addrNvnp.GetLiberal(), &funcApp) && (funcApp.m_func == VNF_PtrToArrElem))
                {
                    fgValueNumberArrIndexVal(tree, &funcApp, addrXvnp.GetLiberal());
                }
                else if (addr->IsFieldAddr(this, &obj, &staticOffset, &fldSeq2))
                {
                    if (fldSeq2 == FieldSeqStore::NotAField())
                    {
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    }
                    else if (fldSeq2 != nullptr)
                    {
                        // Get the first (instance or static) field from field seq.  GcHeap[field] will yield the "field
                        // map".
                        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
                        CORINFO_CLASS_HANDLE fldCls = info.compCompHnd->getFieldClass(fldSeq2->m_fieldHnd);
                        if (obj != nullptr)
                        {
                            // Make sure that the class containing it is not a value class (as we are expecting an
                            // instance field)
                            assert((info.compCompHnd->getClassAttribs(fldCls) & CORINFO_FLG_VALUECLASS) == 0);
                            assert(staticOffset == nullptr);
                        }
#endif // DEBUG

                        // Get a field sequence for just the first field in the sequence
                        //
                        FieldSeqNode* firstFieldOnly = GetFieldSeqStore()->CreateSingleton(fldSeq2->m_fieldHnd);
                        size_t        structSize     = 0;
                        ValueNum      fldMapVN =
                            vnStore->VNApplySelectors(VNK_Liberal, fgCurMemoryVN[GcHeap], firstFieldOnly, &structSize);

                        // The final field in the sequence will need to match the 'indType'
                        var_types indType = tree->TypeGet();

                        // The type of the field is "struct" if there are more fields in the sequence,
                        // otherwise it is the type returned from VNApplySelectors above.
                        var_types firstFieldType = vnStore->TypeOfVN(fldMapVN);

                        ValueNum valAtAddr = fldMapVN;
                        if (obj != nullptr)
                        {
                            // construct the ValueNumber for 'fldMap at obj'
                            ValueNum objNormVal = vnStore->VNLiberalNormalValue(obj->gtVNPair);
                            valAtAddr = vnStore->VNForMapSelect(VNK_Liberal, firstFieldType, fldMapVN, objNormVal);
                        }
                        else if (staticOffset != nullptr)
                        {
                            // construct the ValueNumber for 'fldMap at staticOffset'
                            ValueNum offsetNormVal = vnStore->VNLiberalNormalValue(staticOffset->gtVNPair);
                            valAtAddr = vnStore->VNForMapSelect(VNK_Liberal, firstFieldType, fldMapVN, offsetNormVal);
                        }

                        // Now get rid of any remaining struct field dereferences.
                        if (fldSeq2->m_next)
                        {
                            valAtAddr = vnStore->VNApplySelectors(VNK_Liberal, valAtAddr, fldSeq2->m_next, &structSize);
                        }
                        valAtAddr = vnStore->VNApplySelectorsTypeCheck(valAtAddr, indType, structSize);

                        tree->gtVNPair.SetLiberal(valAtAddr);

                        // The conservative value is a new, unique VN.
                        tree->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                        tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
                    }
                    else
                    {
                        // Occasionally we do an explicit null test on a REF, so we just dereference it with no
                        // field sequence.  The result is probably unused.
                        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                        tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
                    }
                }
                else // We don't know where the address points, so it is an ByrefExposed load.
                {
                    ValueNum addrVN = addr->gtVNPair.GetLiberal();
                    ValueNum loadVN = fgValueNumberByrefExposedLoad(typ, addrVN);
                    tree->gtVNPair.SetLiberal(loadVN);
                    tree->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                    tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, addrXvnp);
                }
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

#ifdef FEATURE_SIMD
        else if (tree->OperGet() == GT_SIMD)
        {
            fgValueNumberSimd(tree);
        }
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
        else if (tree->OperGet() == GT_HWINTRINSIC)
        {
            fgValueNumberHWIntrinsic(tree);
        }
#endif // FEATURE_HW_INTRINSICS

        else // Look up the VNFunc for the node
        {
            VNFunc vnf = GetVNFuncForNode(tree);

            if (ValueNumStore::VNFuncIsLegal(vnf))
            {
                if (GenTree::OperIsUnary(oper))
                {
                    if (tree->AsOp()->gtOp1 != nullptr)
                    {
                        if (tree->OperGet() == GT_NOP)
                        {
                            // Pass through arg vn.
                            tree->gtVNPair = tree->AsOp()->gtOp1->gtVNPair;
                        }
                        else
                        {
                            ValueNumPair op1VNP;
                            ValueNumPair op1VNPx;
                            vnStore->VNPUnpackExc(tree->AsOp()->gtOp1->gtVNPair, &op1VNP, &op1VNPx);

                            // If we are fetching the array length for an array ref that came from global memory
                            // then for CSE safety we must use the conservative value number for both
                            //
                            if ((tree->OperGet() == GT_ARR_LENGTH) &&
                                ((tree->AsOp()->gtOp1->gtFlags & GTF_GLOB_REF) != 0))
                            {
                                // use the conservative value number for both when computing the VN for the ARR_LENGTH
                                op1VNP.SetBoth(op1VNP.GetConservative());
                            }

                            tree->gtVNPair =
                                vnStore->VNPWithExc(vnStore->VNPairForFunc(tree->TypeGet(), vnf, op1VNP), op1VNPx);
                        }
                    }
                    else // Is actually nullary.
                    {
                        // Mostly we'll leave these without a value number, assuming we'll detect these as VN failures
                        // if they actually need to have values.  With the exception of NOPs, which can sometimes have
                        // meaning.
                        if (tree->OperGet() == GT_NOP)
                        {
                            tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
                        }
                    }
                }
                else // we have a binary oper
                {
                    assert(oper != GT_ASG); // We handled assignments earlier.
                    assert(GenTree::OperIsBinary(oper));
                    // Standard binary operator.
                    ValueNumPair op2VNPair;
                    if (tree->AsOp()->gtOp2 == nullptr)
                    {
                        // Handle any GT_LIST nodes as they can have a nullptr for op2.
                        op2VNPair.SetBoth(ValueNumStore::VNForNull());
                    }
                    else
                    {
                        op2VNPair = tree->AsOp()->gtOp2->gtVNPair;
                    }

                    // Handle a few special cases: if we add a field offset constant to a PtrToXXX, we will get back a
                    // new
                    // PtrToXXX.

                    ValueNumPair op1vnp;
                    ValueNumPair op1Xvnp;
                    vnStore->VNPUnpackExc(tree->AsOp()->gtOp1->gtVNPair, &op1vnp, &op1Xvnp);

                    ValueNumPair op2vnp;
                    ValueNumPair op2Xvnp;
                    vnStore->VNPUnpackExc(op2VNPair, &op2vnp, &op2Xvnp);
                    ValueNumPair excSetPair = vnStore->VNPExcSetUnion(op1Xvnp, op2Xvnp);

                    ValueNum newVN = ValueNumStore::NoVN;

                    // Check for the addition of a field offset constant
                    //
                    if ((oper == GT_ADD) && (!tree->gtOverflowEx()))
                    {
                        newVN = vnStore->ExtendPtrVN(tree->AsOp()->gtOp1, tree->AsOp()->gtOp2);
                    }

                    if (newVN != ValueNumStore::NoVN)
                    {
                        // We don't care about differences between liberal and conservative for pointer values.
                        newVN = vnStore->VNWithExc(newVN, excSetPair.GetLiberal());
                        tree->gtVNPair.SetBoth(newVN);
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
                    case GT_COMMA:
                    {
                        ValueNumPair op1vnp;
                        ValueNumPair op1Xvnp;
                        vnStore->VNPUnpackExc(tree->AsOp()->gtOp1->gtVNPair, &op1vnp, &op1Xvnp);
                        ValueNumPair op2vnp;
                        ValueNumPair op2Xvnp = ValueNumStore::VNPForEmptyExcSet();
                        GenTree*     op2     = tree->gtGetOp2();

                        if (op2->OperIsIndir() && ((op2->gtFlags & GTF_IND_ASG_LHS) != 0))
                        {
                            // If op2 represents the lhs of an assignment then we give a VNForVoid for the lhs
                            op2vnp = ValueNumPair(ValueNumStore::VNForVoid(), ValueNumStore::VNForVoid());
                        }
                        else if ((op2->OperGet() == GT_CLS_VAR) && (op2->gtFlags & GTF_CLS_VAR_ASG_LHS))
                        {
                            // If op2 represents the lhs of an assignment then we give a VNForVoid for the lhs
                            op2vnp = ValueNumPair(ValueNumStore::VNForVoid(), ValueNumStore::VNForVoid());
                        }
                        else
                        {
                            vnStore->VNPUnpackExc(op2->gtVNPair, &op2vnp, &op2Xvnp);
                        }
                        tree->gtVNPair = vnStore->VNPWithExc(op2vnp, vnStore->VNPExcSetUnion(op1Xvnp, op2Xvnp));
                    }
                    break;

                    case GT_NULLCHECK:
                    {
                        // An Explicit null check, produces no value
                        // But we do persist any execeptions produced by op1
                        //
                        tree->gtVNPair = vnStore->VNPWithExc(vnStore->VNPForVoid(),
                                                             vnStore->VNPExceptionSet(tree->AsOp()->gtOp1->gtVNPair));
                        // The exception set with VNF_NullPtrExc will be added below
                        // by fgValueNumberAddExceptionSet
                    }
                    break;

                    case GT_LOCKADD: // Binop
                        noway_assert("LOCKADD should not appear before lowering");
                        break;

                    case GT_XADD: // Binop
                    case GT_XCHG: // Binop
                    {
                        // For XADD and XCHG other intrinsics add an arbitrary side effect on GcHeap/ByrefExposed.
                        fgMutateGcHeap(tree DEBUGARG("Interlocked intrinsic"));

                        assert(tree->OperIsImplicitIndir()); // special node with an implicit indirections

                        GenTree* addr = tree->AsOp()->gtOp1; // op1
                        GenTree* data = tree->AsOp()->gtOp2; // op2

                        ValueNumPair vnpExcSet = ValueNumStore::VNPForEmptyExcSet();

                        vnpExcSet = vnStore->VNPUnionExcSet(data->gtVNPair, vnpExcSet);
                        vnpExcSet = vnStore->VNPUnionExcSet(addr->gtVNPair, vnpExcSet);

                        // The normal value is a new unique VN.
                        ValueNumPair normalPair;
                        normalPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));

                        // Attach the combined exception set
                        tree->gtVNPair = vnStore->VNPWithExc(normalPair, vnpExcSet);

                        // add the null check exception for 'addr' to the tree's value number
                        fgValueNumberAddExceptionSetForIndirection(tree, addr);
                        break;
                    }

                    case GT_JTRUE:
                    case GT_LIST:
                        // These nodes never need to have a ValueNumber
                        tree->gtVNPair.SetBoth(ValueNumStore::NoVN);
                        break;

                    case GT_BOX:
                        // BOX doesn't do anything at this point, the actual object allocation
                        // and initialization happens separately (and not numbering BOX correctly
                        // prevents seeing allocation related assertions through it)
                        tree->gtVNPair = tree->gtGetOp1()->gtVNPair;
                        break;

                    default:
                        // The default action is to give the node a new, unique VN.
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

            case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
            case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
            case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
            {
                ValueNumPair vnpIndex  = tree->AsBoundsChk()->gtIndex->gtVNPair;
                ValueNumPair vnpArrLen = tree->AsBoundsChk()->gtArrLen->gtVNPair;

                ValueNumPair vnpExcSet = ValueNumStore::VNPForEmptyExcSet();

                // And collect the exceptions  from Index and ArrLen
                vnpExcSet = vnStore->VNPUnionExcSet(vnpIndex, vnpExcSet);
                vnpExcSet = vnStore->VNPUnionExcSet(vnpArrLen, vnpExcSet);

                // A bounds check node has no value, but may throw exceptions.
                tree->gtVNPair = vnStore->VNPWithExc(vnStore->VNPForVoid(), vnpExcSet);

                // next add the bounds check exception set for the current tree node
                fgValueNumberAddExceptionSet(tree);

                // Record non-constant value numbers that are used as the length argument to bounds checks, so that
                // assertion prop will know that comparisons against them are worth analyzing.
                ValueNum lengthVN = tree->AsBoundsChk()->gtArrLen->gtVNPair.GetConservative();
                if ((lengthVN != ValueNumStore::NoVN) && !vnStore->IsVNConstant(lengthVN))
                {
                    vnStore->SetVNIsCheckedBound(lengthVN);
                }
            }
            break;

            case GT_CMPXCHG: // Specialop
            {
                // For CMPXCHG and other intrinsics add an arbitrary side effect on GcHeap/ByrefExposed.
                fgMutateGcHeap(tree DEBUGARG("Interlocked intrinsic"));

                GenTreeCmpXchg* const cmpXchg = tree->AsCmpXchg();

                assert(tree->OperIsImplicitIndir()); // special node with an implicit indirections

                GenTree* location  = cmpXchg->gtOpLocation;  // arg1
                GenTree* value     = cmpXchg->gtOpValue;     // arg2
                GenTree* comparand = cmpXchg->gtOpComparand; // arg3

                ValueNumPair vnpExcSet = ValueNumStore::VNPForEmptyExcSet();

                // Collect the exception sets from our operands
                vnpExcSet = vnStore->VNPUnionExcSet(location->gtVNPair, vnpExcSet);
                vnpExcSet = vnStore->VNPUnionExcSet(value->gtVNPair, vnpExcSet);
                vnpExcSet = vnStore->VNPUnionExcSet(comparand->gtVNPair, vnpExcSet);

                // The normal value is a new unique VN.
                ValueNumPair normalPair;
                normalPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));

                // Attach the combined exception set
                tree->gtVNPair = vnStore->VNPWithExc(normalPair, vnpExcSet);

                // add the null check exception for 'location' to the tree's value number
                fgValueNumberAddExceptionSetForIndirection(tree, location);
                // add the null check exception for 'comparand' to the tree's value number
                fgValueNumberAddExceptionSetForIndirection(tree, comparand);
                break;
            }

            default:
                tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
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
            if (tree->OperIsLeaf() || tree->OperIsLocalStore()) // local stores used to be leaves
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
        switch (intrinsic->gtIntrinsicId)
        {
            case CORINFO_INTRINSIC_Object_GetType:
                intrinsic->gtVNPair =
                    vnStore->VNPWithExc(vnStore->VNPairForFunc(intrinsic->TypeGet(), VNF_ObjGetType, arg0VNP),
                                        arg0VNPx);
                break;

            default:
                unreached();
        }
    }
}

#ifdef FEATURE_SIMD
// Does value-numbering for a GT_SIMD node.
void Compiler::fgValueNumberSimd(GenTree* tree)
{
    assert(tree->OperGet() == GT_SIMD);
    GenTreeSIMD* simdNode = tree->AsSIMD();
    assert(simdNode != nullptr);
    ValueNumPair excSetPair;
    ValueNumPair normalPair;

    // There are some SIMD operations that have zero args, i.e.  NI_Vector128_Zero
    if (tree->AsOp()->gtOp1 == nullptr)
    {
        excSetPair = ValueNumStore::VNPForEmptyExcSet();
        normalPair = vnStore->VNPairForFunc(tree->TypeGet(), GetVNFuncForNode(tree));
    }
    else if (tree->AsOp()->gtOp1->OperIs(GT_LIST))
    {
        assert(tree->AsOp()->gtOp2 == nullptr);

        // We have a SIMD node in the GT_LIST form with 3 or more args
        // For now we will generate a unique value number for this case.

        // Generate a unique VN
        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
        return;
    }
    else // SIMD unary or binary operator.
    {
        ValueNumPair resvnp = ValueNumPair();
        ValueNumPair op1vnp;
        ValueNumPair op1Xvnp;
        vnStore->VNPUnpackExc(tree->AsOp()->gtOp1->gtVNPair, &op1vnp, &op1Xvnp);

        if (simdNode->gtSIMDIntrinsicID == SIMDIntrinsicInitArray)
        {
            // rationalize rewrites this as an explicit load with op1 as the base address

            ValueNumPair op2vnp;
            if (tree->AsOp()->gtOp2 == nullptr)
            {
                // a nullptr for op2 means that we have an impicit index of zero
                op2vnp = ValueNumPair(vnStore->VNZeroForType(TYP_INT), vnStore->VNZeroForType(TYP_INT));

                excSetPair = op1Xvnp;
            }
            else // We have an explicit index in op2
            {
                ValueNumPair op2Xvnp;
                vnStore->VNPUnpackExc(tree->AsOp()->gtOp2->gtVNPair, &op2vnp, &op2Xvnp);

                excSetPair = vnStore->VNPExcSetUnion(op1Xvnp, op2Xvnp);
            }

            ValueNum addrVN =
                vnStore->VNForFunc(TYP_BYREF, GetVNFuncForNode(tree), op1vnp.GetLiberal(), op2vnp.GetLiberal());
#ifdef DEBUG
            if (verbose)
            {
                printf("Treating GT_SIMD InitArray as a ByrefExposed load , addrVN is ");
                vnPrint(addrVN, 0);
            }
#endif // DEBUG

            // The address points into the heap, so it is an ByrefExposed load.
            //
            ValueNum loadVN = fgValueNumberByrefExposedLoad(tree->TypeGet(), addrVN);
            tree->gtVNPair.SetLiberal(loadVN);
            tree->gtVNPair.SetConservative(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
            tree->gtVNPair = vnStore->VNPWithExc(tree->gtVNPair, excSetPair);
            fgValueNumberAddExceptionSetForIndirection(tree, tree->AsOp()->gtOp1);
            return;
        }

        bool encodeResultType = vnEncodesResultTypeForSIMDIntrinsic(simdNode->gtSIMDIntrinsicID);

        if (encodeResultType)
        {
            ValueNum vnSize     = vnStore->VNForIntCon(simdNode->gtSIMDSize);
            ValueNum vnBaseType = vnStore->VNForIntCon(INT32(simdNode->gtSIMDBaseType));
            ValueNum simdTypeVN = vnStore->VNForFunc(TYP_REF, VNF_SimdType, vnSize, vnBaseType);
            resvnp.SetBoth(simdTypeVN);

#ifdef DEBUG
            if (verbose)
            {
                printf("    simdTypeVN is ");
                vnPrint(simdTypeVN, 1);
                printf("\n");
            }
#endif
        }

        VNFunc simdFunc = GetVNFuncForNode(tree);

        if (tree->AsOp()->gtOp2 == nullptr)
        {
            // Unary SIMD nodes have a nullptr for op2.
            excSetPair = op1Xvnp;
            if (encodeResultType)
            {
                normalPair = vnStore->VNPairForFunc(tree->TypeGet(), simdFunc, op1vnp, resvnp);
                assert(vnStore->VNFuncArity(simdFunc) == 2);
            }
            else
            {
                normalPair = vnStore->VNPairForFunc(tree->TypeGet(), simdFunc, op1vnp);
                assert(vnStore->VNFuncArity(simdFunc) == 1);
            }
        }
        else
        {
            ValueNumPair op2vnp;
            ValueNumPair op2Xvnp;
            vnStore->VNPUnpackExc(tree->AsOp()->gtOp2->gtVNPair, &op2vnp, &op2Xvnp);

            excSetPair = vnStore->VNPExcSetUnion(op1Xvnp, op2Xvnp);
            if (encodeResultType)
            {
                normalPair = vnStore->VNPairForFunc(tree->TypeGet(), simdFunc, op1vnp, op2vnp, resvnp);
                assert(vnStore->VNFuncArity(simdFunc) == 3);
            }
            else
            {
                normalPair = vnStore->VNPairForFunc(tree->TypeGet(), simdFunc, op1vnp, op2vnp);
                assert(vnStore->VNFuncArity(simdFunc) == 2);
            }
        }
    }
    tree->gtVNPair = vnStore->VNPWithExc(normalPair, excSetPair);
}
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
// Does value-numbering for a GT_HWINTRINSIC node
void Compiler::fgValueNumberHWIntrinsic(GenTree* tree)
{
    assert(tree->OperGet() == GT_HWINTRINSIC);
    GenTreeHWIntrinsic* hwIntrinsicNode = tree->AsHWIntrinsic();
    assert(hwIntrinsicNode != nullptr);

    // For safety/correctness we must mutate the global heap valuenumber
    //  for any HW intrinsic that performs a memory store operation
    if (hwIntrinsicNode->OperIsMemoryStore())
    {
        fgMutateGcHeap(tree DEBUGARG("HWIntrinsic - MemoryStore"));
    }

    int    lookupNumArgs    = HWIntrinsicInfo::lookupNumArgs(hwIntrinsicNode->gtHWIntrinsicId);
    bool   encodeResultType = vnEncodesResultTypeForHWIntrinsic(hwIntrinsicNode->gtHWIntrinsicId);
    VNFunc func             = GetVNFuncForNode(tree);

    ValueNumPair excSetPair = ValueNumStore::VNPForEmptyExcSet();
    ValueNumPair normalPair;
    ValueNumPair resvnp = ValueNumPair();

    if (encodeResultType)
    {
        ValueNum vnSize     = vnStore->VNForIntCon(hwIntrinsicNode->gtSIMDSize);
        ValueNum vnBaseType = vnStore->VNForIntCon(INT32(hwIntrinsicNode->gtSIMDBaseType));
        ValueNum simdTypeVN = vnStore->VNForFunc(TYP_REF, VNF_SimdType, vnSize, vnBaseType);
        resvnp.SetBoth(simdTypeVN);

#ifdef DEBUG
        if (verbose)
        {
            printf("    simdTypeVN is ");
            vnPrint(simdTypeVN, 1);
            printf("\n");
        }
#endif
    }

    // There are some HWINTRINSICS operations that have zero args, i.e.  NI_Vector128_Zero
    if (tree->AsOp()->gtOp1 == nullptr)
    {
        if (encodeResultType)
        {
            // There are zero arg HWINTRINSICS operations that encode the result type, i.e.  Vector128_AllBitSet
            normalPair = vnStore->VNPairForFunc(tree->TypeGet(), func, resvnp);
            assert(vnStore->VNFuncArity(func) == 1);
        }
        else
        {
            normalPair = vnStore->VNPairForFunc(tree->TypeGet(), func);
            assert(vnStore->VNFuncArity(func) == 0);
        }
    }
    else if (tree->AsOp()->gtOp1->OperIs(GT_LIST) || (lookupNumArgs == -1))
    {
        // We have a HWINTRINSIC node in the GT_LIST form with 3 or more args
        // Or the numArgs was specified as -1 in the numArgs column in "hwintrinsiclistxarch.h"
        // For now we will generate a unique value number for this case.

        // Generate unique VN
        tree->gtVNPair.SetBoth(vnStore->VNForExpr(compCurBB, tree->TypeGet()));
        return;
    }
    else // HWINTRINSIC unary or binary operator.
    {
        ValueNumPair op1vnp;
        ValueNumPair op1Xvnp;
        vnStore->VNPUnpackExc(tree->AsOp()->gtOp1->gtVNPair, &op1vnp, &op1Xvnp);

        if (tree->AsOp()->gtOp2 == nullptr)
        {
            excSetPair = op1Xvnp;

            if (encodeResultType)
            {
                normalPair = vnStore->VNPairForFunc(tree->TypeGet(), func, op1vnp, resvnp);
                assert(vnStore->VNFuncArity(func) == 2);
            }
            else
            {
                normalPair = vnStore->VNPairForFunc(tree->TypeGet(), func, op1vnp);
                assert(vnStore->VNFuncArity(func) == 1);
            }
        }
        else
        {
            ValueNumPair op2vnp;
            ValueNumPair op2Xvnp;
            vnStore->VNPUnpackExc(tree->AsOp()->gtOp2->gtVNPair, &op2vnp, &op2Xvnp);

            excSetPair = vnStore->VNPExcSetUnion(op1Xvnp, op2Xvnp);
            if (encodeResultType)
            {
                normalPair = vnStore->VNPairForFunc(tree->TypeGet(), func, op1vnp, op2vnp, resvnp);
                assert(vnStore->VNFuncArity(func) == 3);
            }
            else
            {
                normalPair = vnStore->VNPairForFunc(tree->TypeGet(), func, op1vnp, op2vnp);
                assert(vnStore->VNFuncArity(func) == 2);
            }
        }
    }
    tree->gtVNPair = vnStore->VNPWithExc(normalPair, excSetPair);
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

    assert(genActualType(castToType) == genActualType(tree->TypeGet())); // Insure that the resultType is correct

    tree->gtVNPair = vnStore->VNPairForCast(srcVNPair, castToType, castFromType, srcIsUnsigned, hasOverflowCheck);
}

// Compute the normal ValueNumber for a cast operation with no exceptions
ValueNum ValueNumStore::VNForCast(ValueNum  srcVN,
                                  var_types castToType,
                                  var_types castFromType,
                                  bool      srcIsUnsigned /* = false */)
{
    // The resulting type after performingthe cast is always widened to a supported IL stack size
    var_types resultType = genActualType(castToType);

    // When we're considering actual value returned by a non-checking cast whether or not the source is
    // unsigned does *not* matter for non-widening casts.  That is, if we cast an int or a uint to short,
    // we just extract the first two bytes from the source bit pattern, not worrying about the interpretation.
    // The same is true in casting between signed/unsigned types of the same width.  Only when we're doing
    // a widening cast do we care about whether the source was unsigned,so we know whether to sign or zero extend it.
    //
    bool srcIsUnsignedNorm = srcIsUnsigned;
    if (genTypeSize(castToType) <= genTypeSize(castFromType))
    {
        srcIsUnsignedNorm = false;
    }

    ValueNum castTypeVN = VNForCastOper(castToType, srcIsUnsigned);
    ValueNum resultVN   = VNForFunc(resultType, VNF_Cast, srcVN, castTypeVN);

#ifdef DEBUG
    if (m_pComp->verbose)
    {
        printf("    VNForCast(" FMT_VN ", " FMT_VN ") returns ", srcVN, castTypeVN);
        m_pComp->vnPrint(resultVN, 1);
        printf("\n");
    }
#endif

    return resultVN;
}

// Compute the ValueNumberPair for a cast operation
ValueNumPair ValueNumStore::VNPairForCast(ValueNumPair srcVNPair,
                                          var_types    castToType,
                                          var_types    castFromType,
                                          bool         srcIsUnsigned,    /* = false */
                                          bool         hasOverflowCheck) /* = false */
{
    // The resulting type after performingthe cast is always widened to a supported IL stack size
    var_types resultType = genActualType(castToType);

    ValueNumPair castArgVNP;
    ValueNumPair castArgxVNP;
    VNPUnpackExc(srcVNPair, &castArgVNP, &castArgxVNP);

    // When we're considering actual value returned by a non-checking cast, (hasOverflowCheck is false)
    // whether or not the source is unsigned does *not* matter for non-widening casts.
    // That is, if we cast an int or a uint to short, we just extract the first two bytes from the source
    // bit pattern, not worrying about the interpretation.  The same is true in casting between signed/unsigned
    // types of the same width.  Only when we're doing a widening cast do we care about whether the source
    // was unsigned, so we know whether to sign or zero extend it.
    //
    // Important: Casts to floating point cannot be optimized in this fashion. (bug 946768)
    //
    bool srcIsUnsignedNorm = srcIsUnsigned;
    if (!hasOverflowCheck && !varTypeIsFloating(castToType) && (genTypeSize(castToType) <= genTypeSize(castFromType)))
    {
        srcIsUnsignedNorm = false;
    }

    VNFunc       vnFunc     = hasOverflowCheck ? VNF_CastOvf : VNF_Cast;
    ValueNum     castTypeVN = VNForCastOper(castToType, srcIsUnsignedNorm);
    ValueNumPair castTypeVNPair(castTypeVN, castTypeVN);
    ValueNumPair castNormRes = VNPairForFunc(resultType, vnFunc, castArgVNP, castTypeVNPair);

    ValueNumPair resultVNP = VNPWithExc(castNormRes, castArgxVNP);

    // If we have a check for overflow, add the exception information.
    if (hasOverflowCheck)
    {
        ValueNumPair ovfChk = VNPairForFunc(TYP_REF, VNF_ConvOverflowExc, castArgVNP, castTypeVNPair);
        ValueNumPair excSet = VNPExcSetSingleton(ovfChk);
        excSet              = VNPExcSetUnion(excSet, castArgxVNP);
        resultVNP           = VNPWithExc(castNormRes, excSet);
    }

    return resultVNP;
}

void Compiler::fgValueNumberHelperCallFunc(GenTreeCall* call, VNFunc vnf, ValueNumPair vnpExc)
{
    unsigned nArgs = ValueNumStore::VNFuncArity(vnf);
    assert(vnf != VNF_Boundary);
    GenTreeCall::Use* args                    = call->gtCallArgs;
    bool              generateUniqueVN        = false;
    bool              useEntryPointAddrAsArg0 = false;

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
            ValueNumPair vnp1 = vnStore->VNPNormalPair(args->GetNext()->GetNode()->gtVNPair);

            // The New Array helper may throw an overflow exception
            vnpExc = vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_NewArrOverflowExc, vnp1));
        }
        break;

        case VNF_Box:
        case VNF_BoxNullable:
        {
            // Generate unique VN so, VNForFunc generates a uniq value number for box nullable.
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
            ValueNumPair vnp1 = vnStore->VNPNormalPair(args->GetNode()->gtVNPair);

            // The New Array helper may throw an overflow exception
            vnpExc = vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_NewArrOverflowExc, vnp1));
            useEntryPointAddrAsArg0 = true;
        }
        break;

        case VNF_ReadyToRunStaticBase:
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

#if defined(FEATURE_READYTORUN_COMPILER) && defined(TARGET_ARMARCH)
    if (call->IsR2RRelativeIndir())
    {
#ifdef DEBUG
        assert(args->GetNode()->OperGet() == GT_ARGPLACE);

        // Find the corresponding late arg.
        GenTree* indirectCellAddress = call->fgArgInfo->GetArgNode(0);
        assert(indirectCellAddress->IsCnsIntOrI() && indirectCellAddress->GetRegNum() == REG_R2R_INDIRECT_PARAM);
#endif // DEBUG

        // For ARM indirectCellAddress is consumed by the call itself, so it should have added as an implicit argument
        // in morph. So we do not need to use EntryPointAddrAsArg0, because arg0 is already an entry point addr.
        useEntryPointAddrAsArg0 = false;
    }
#endif // FEATURE_READYTORUN_COMPILER && TARGET_ARMARCH

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
        auto getCurrentArg = [call, &args, useEntryPointAddrAsArg0](int currentIndex) {
            GenTree* arg = args->GetNode();
            if ((arg->gtFlags & GTF_LATE_ARG) != 0)
            {
                // This arg is a setup node that moves the arg into position.
                // Value-numbering will have visited the separate late arg that
                // holds the actual value, and propagated/computed the value number
                // for this arg there.
                if (useEntryPointAddrAsArg0)
                {
                    // The args in the fgArgInfo don't include the entry point, so
                    // index into them using one less than the requested index.
                    --currentIndex;
                }
                return call->fgArgInfo->GetArgNode(currentIndex);
            }
            return arg;
        };
        // Has at least one argument.
        ValueNumPair vnp0;
        ValueNumPair vnp0x = ValueNumStore::VNPForEmptyExcSet();
#ifdef FEATURE_READYTORUN_COMPILER
        if (useEntryPointAddrAsArg0)
        {
            ssize_t  addrValue  = (ssize_t)call->gtEntryPoint.addr;
            ValueNum callAddrVN = vnStore->VNForHandle(addrValue, GTF_ICON_FTN_ADDR);
            vnp0                = ValueNumPair(callAddrVN, callAddrVN);
        }
        else
#endif // FEATURE_READYTORUN_COMPILER
        {
            assert(!useEntryPointAddrAsArg0);
            ValueNumPair vnp0wx = getCurrentArg(0)->gtVNPair;
            vnStore->VNPUnpackExc(vnp0wx, &vnp0, &vnp0x);

            // Also include in the argument exception sets
            vnpExc = vnStore->VNPExcSetUnion(vnpExc, vnp0x);

            args = args->GetNext();
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
            ValueNumPair vnp1wx = getCurrentArg(1)->gtVNPair;
            ValueNumPair vnp1;
            ValueNumPair vnp1x;
            vnStore->VNPUnpackExc(vnp1wx, &vnp1, &vnp1x);
            vnpExc = vnStore->VNPExcSetUnion(vnpExc, vnp1x);

            args = args->GetNext();
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
                ValueNumPair vnp2wx = getCurrentArg(2)->gtVNPair;
                ValueNumPair vnp2;
                ValueNumPair vnp2x;
                vnStore->VNPUnpackExc(vnp2wx, &vnp2, &vnp2x);
                vnpExc = vnStore->VNPExcSetUnion(vnpExc, vnp2x);

                args = args->GetNext();
                assert(nArgs == 3); // Our current maximum.
                assert(args == nullptr);
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
    assert(args == nullptr || generateUniqueVN); // All arguments should be processed or we generate unique VN and do
                                                 // not care.
}

void Compiler::fgValueNumberCall(GenTreeCall* call)
{
    // First: do value numbering of any argument placeholder nodes in the argument list
    // (by transferring from the VN of the late arg that they are standing in for...)
    unsigned i = 0;
    for (GenTreeCall::Use& use : call->Args())
    {
        GenTree* arg = use.GetNode();
        if (arg->OperGet() == GT_ARGPLACE)
        {
            // Find the corresponding late arg.
            GenTree* lateArg = call->fgArgInfo->GetArgNode(i);
            assert(lateArg->gtVNPair.BothDefined());
            arg->gtVNPair = lateArg->gtVNPair;
#ifdef DEBUG
            if (verbose)
            {
                printf("VN of ARGPLACE tree ");
                Compiler::printTreeID(arg);
                printf(" updated to ");
                vnpPrint(arg->gtVNPair, 1);
                printf("\n");
            }
#endif
        }
        i++;
    }

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
        case CORINFO_HELP_LMUL_OVF:
            vnf = VNFunc(GT_MUL);
            break;
        case CORINFO_HELP_ULMUL_OVF:
            vnf = VNFunc(GT_MUL);
            break; // Is this the right thing?
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

        case CORINFO_HELP_LNG2DBL:
            vnf = VNF_Lng2Dbl;
            break;
        case CORINFO_HELP_ULNG2DBL:
            vnf = VNF_ULng2Dbl;
            break;
        case CORINFO_HELP_DBL2INT:
            vnf = VNF_Dbl2Int;
            break;
        case CORINFO_HELP_DBL2INT_OVF:
            vnf = VNF_Dbl2Int;
            break;
        case CORINFO_HELP_DBL2LNG:
            vnf = VNF_Dbl2Lng;
            break;
        case CORINFO_HELP_DBL2LNG_OVF:
            vnf = VNF_Dbl2Lng;
            break;
        case CORINFO_HELP_DBL2UINT:
            vnf = VNF_Dbl2UInt;
            break;
        case CORINFO_HELP_DBL2UINT_OVF:
            vnf = VNF_Dbl2UInt;
            break;
        case CORINFO_HELP_DBL2ULNG:
            vnf = VNF_Dbl2ULng;
            break;
        case CORINFO_HELP_DBL2ULNG_OVF:
            vnf = VNF_Dbl2ULng;
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

        case CORINFO_HELP_READYTORUN_NEWARR_1:
            vnf = VNF_JitReadyToRunNewArr;
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
        case CORINFO_HELP_READYTORUN_STATIC_BASE:
            vnf = VNF_ReadyToRunStaticBase;
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
        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR:
            vnf = VNF_GetsharedNongcthreadstaticBaseNoctor;
            break;
        case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_DYNAMICCLASS:
            vnf = VNF_GetsharedGcthreadstaticBaseDynamicclass;
            break;
        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_DYNAMICCLASS:
            vnf = VNF_GetsharedNongcthreadstaticBaseDynamicclass;
            break;
        case CORINFO_HELP_GETSTATICFIELDADDR_CONTEXT:
            vnf = VNF_GetStaticAddrContext;
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
            vnf = VNF_StrCns;
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

        case CORINFO_HELP_ARE_TYPES_EQUIVALENT:
            vnf = VNF_AreTypesEquivalent;
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
    CorInfoHelpFunc helpFunc    = eeGetHelperNum(call->gtCallMethHnd);
    bool            pure        = s_helperCallProperties.IsPure(helpFunc);
    bool            isAlloc     = s_helperCallProperties.IsAllocator(helpFunc);
    bool            modHeap     = s_helperCallProperties.MutatesHeap(helpFunc);
    bool            mayRunCctor = s_helperCallProperties.MayRunCctor(helpFunc);
    bool            noThrow     = s_helperCallProperties.NoThrow(helpFunc);

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
            case CORINFO_HELP_OVERFLOW:
                // This helper always throws the VNF_OverflowExc exception
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
        // TODO-CQ: this is a list of helpers we're going to treat as non-pure,
        // because they raise complications.  Eventually, we need to handle those complications...
        bool needsFurtherWork = false;
        switch (helpFunc)
        {
            case CORINFO_HELP_NEW_MDARR:
                // This is a varargs helper.  We need to represent the array shape in the VN world somehow.
                needsFurtherWork = true;
                break;
            default:
                break;
        }

        if (!needsFurtherWork && (pure || isAlloc))
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
    assert(tree->OperIsUnary() || tree->OperIsImplicitIndir());

    // We evaluate the baseAddr ValueNumber further in order
    // to obtain a better value to use for the null check exeception.
    //
    ValueNumPair baseVNP = baseAddr->gtVNPair;
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

    // Create baseVNP, from the values we just computed,
    baseVNP = ValueNumPair(baseLVN, baseCVN);

    // Unpack, Norm,Exc for the tree's op1 VN
    ValueNumPair vnpBaseNorm;
    ValueNumPair vnpBaseExc;
    vnStore->VNPUnpackExc(baseVNP, &vnpBaseNorm, &vnpBaseExc);

    // The Norm VN for op1 is used to create the NullPtrExc
    ValueNumPair excChkSet = vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_NullPtrExc, vnpBaseNorm));

    // Combine the excChkSet with exception set of op1
    ValueNumPair excSetBoth = vnStore->VNPExcSetUnion(excChkSet, vnpBaseExc);

    // Retrieve the Normal VN for tree, note that it may be NoVN, so we handle that case
    ValueNumPair vnpNorm = vnStore->VNPNormalPair(tree->gtVNPair);

    // For as GT_IND on the lhs of an assignment we will get a NoVN value
    if (vnpNorm.GetLiberal() == ValueNumStore::NoVN)
    {
        // Use the special Void VN value instead.
        vnpNorm = vnStore->VNPForVoid();
    }
    tree->gtVNPair = vnStore->VNPWithExc(vnpNorm, excSetBoth);
}

//--------------------------------------------------------------------------------
// fgValueNumberAddExceptionSetForDivison
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
        vnpArithmExc.SetLiberal(
            vnStore->VNExcSetSingleton(vnStore->VNForFunc(TYP_REF, VNF_ArithmeticExc, vnOp1NormLib, vnOp2NormLib)));
    }
    if (needArithmeticExcCon)
    {
        vnpArithmExc.SetConservative(
            vnStore->VNExcSetSingleton(vnStore->VNForFunc(TYP_REF, VNF_ArithmeticExc, vnOp1NormLib, vnOp2NormCon)));
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
//                 exception set.
//
void Compiler::fgValueNumberAddExceptionSetForOverflow(GenTree* tree)
{
    assert(tree->gtOverflowEx());

    // We should only be dealing with an Overflow checking ALU operation.
    VNFunc vnf = GetVNFuncForNode(tree);
    assert((vnf >= VNF_ADD_OVF) && (vnf <= VNF_MUL_UN_OVF));

    // Unpack, Norm,Exc for the tree's VN
    //
    ValueNumPair vnpTreeNorm;
    ValueNumPair vnpTreeExc;

    vnStore->VNPUnpackExc(tree->gtVNPair, &vnpTreeNorm, &vnpTreeExc);

#ifdef DEBUG
    // The normal value number function should be the same overflow checking ALU operation as 'vnf'
    VNFuncApp treeNormFuncApp;
    assert(vnStore->GetVNFunc(vnpTreeNorm.GetLiberal(), &treeNormFuncApp) && (treeNormFuncApp.m_func == vnf));
#endif // DEBUG

    // Overflow-checking operations add an overflow exception
    // The normal result is used as the input argument for the OverflowExc
    ValueNumPair overflowExcSet =
        vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_OverflowExc, vnpTreeNorm));

    // Combine the new Overflow exception with the original exception set of tree
    ValueNumPair newExcSet = vnStore->VNPExcSetUnion(vnpTreeExc, overflowExcSet);

    // Updated VN for tree, it now includes Overflow exception
    tree->gtVNPair = vnStore->VNPWithExc(vnpTreeNorm, newExcSet);
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

    ValueNumPair vnpIndex  = node->gtIndex->gtVNPair;
    ValueNumPair vnpArrLen = node->gtArrLen->gtVNPair;

    // Unpack, Norm,Exc for the tree's VN
    //
    ValueNumPair vnpTreeNorm;
    ValueNumPair vnpTreeExc;

    vnStore->VNPUnpackExc(tree->gtVNPair, &vnpTreeNorm, &vnpTreeExc);

    // Construct the exception set for bounds check
    ValueNumPair boundsChkExcSet = vnStore->VNPExcSetSingleton(
        vnStore->VNPairForFunc(TYP_REF, VNF_IndexOutOfRangeExc, vnStore->VNPNormalPair(vnpIndex),
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

#ifdef FEATURE_SIMD
            case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
            case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
            case GT_ARR_BOUNDS_CHECK:
                fgValueNumberAddExceptionSetForBoundsCheck(tree);
                break;

            case GT_LCLHEAP:
                // It is not necessary to model the StackOverflow exception for GT_LCLHEAP
                break;

            case GT_INTRINSIC:
                // ToDo: model the exceptions for Intrinsics
                break;

            case GT_IND: // Implicit null check.
                if ((tree->gtFlags & GTF_IND_ASG_LHS) != 0)
                {
                    // Don't add exception set on LHS of assignment
                    break;
                }
                __fallthrough;

            case GT_BLK:
            case GT_OBJ:
            case GT_DYN_BLK:
            case GT_NULLCHECK:
                fgValueNumberAddExceptionSetForIndirection(tree, tree->AsIndir()->Addr());
                break;

            case GT_ARR_LENGTH:
                fgValueNumberAddExceptionSetForIndirection(tree, tree->AsArrLen()->ArrRef());
                break;

            case GT_ARR_ELEM:
                fgValueNumberAddExceptionSetForIndirection(tree, tree->AsArrElem()->gtArrObj);
                break;

            case GT_ARR_INDEX:
                fgValueNumberAddExceptionSetForIndirection(tree, tree->AsArrIndex()->ArrObj());
                break;

            case GT_ARR_OFFSET:
                fgValueNumberAddExceptionSetForIndirection(tree, tree->AsArrOffs()->gtArrObj);
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
    for (NodeToTestDataMap::KeyIterator ki = testData->Begin(); !ki.Equal(testData->End()); ++ki)
    {
        TestLabelAndNum tlAndN;
        GenTree*        node   = ki.Get();
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
