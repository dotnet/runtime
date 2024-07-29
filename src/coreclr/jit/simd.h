// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _SIMD_H_
#define _SIMD_H_

template <typename T>
static bool ElementsAreSame(T* array, size_t size)
{
    for (size_t i = 1; i < size; i++)
    {
        if (array[0] != array[i])
            return false;
    }
    return true;
}

template <typename T>
static bool ElementsAreAllBitsSetOrZero(T* array, size_t size)
{
    for (size_t i = 0; i < size; i++)
    {
        if (array[i] != static_cast<T>(0) && array[i] != static_cast<T>(~0))
            return false;
    }
    return true;
}

struct simd8_t
{
    union
    {
        float    f32[2];
        double   f64[1];
        int8_t   i8[8];
        int16_t  i16[4];
        int32_t  i32[2];
        int64_t  i64[1];
        uint8_t  u8[8];
        uint16_t u16[4];
        uint32_t u32[2];
        uint64_t u64[1];
    };

    bool operator==(const simd8_t& other) const
    {
        return (u64[0] == other.u64[0]);
    }

    bool operator!=(const simd8_t& other) const
    {
        return !(*this == other);
    }

    static simd8_t AllBitsSet()
    {
        simd8_t result;

        result.u64[0] = 0xFFFFFFFFFFFFFFFF;

        return result;
    }

    bool IsAllBitsSet() const
    {
        return *this == AllBitsSet();
    }

    bool IsZero() const
    {
        return *this == Zero();
    }

    static simd8_t Zero()
    {
        return {};
    }
};
static_assert_no_msg(sizeof(simd8_t) == 8);

#include <pshpack4.h>
struct simd12_t
{
    union
    {
        float    f32[3];
        int8_t   i8[12];
        int16_t  i16[6];
        int32_t  i32[3];
        uint8_t  u8[12];
        uint16_t u16[6];
        uint32_t u32[3];

        // These three exist to simplify templatized code
        // they won't actually be accessed for real scenarios

        double   f64[1];
        int64_t  i64[1];
        uint64_t u64[1];
    };

    bool operator==(const simd12_t& other) const
    {
        return (u32[0] == other.u32[0]) && (u32[1] == other.u32[1]) && (u32[2] == other.u32[2]);
    }

    bool operator!=(const simd12_t& other) const
    {
        return !(*this == other);
    }

    static simd12_t AllBitsSet()
    {
        simd12_t result;

        result.u32[0] = 0xFFFFFFFF;
        result.u32[1] = 0xFFFFFFFF;
        result.u32[2] = 0xFFFFFFFF;

        return result;
    }

    bool IsAllBitsSet() const
    {
        return *this == AllBitsSet();
    }

    bool IsZero() const
    {
        return *this == Zero();
    }

    static simd12_t Zero()
    {
        return {};
    }
};
#include <poppack.h>
static_assert_no_msg(sizeof(simd12_t) == 12);

struct simd16_t
{
    union
    {
        float    f32[4];
        double   f64[2];
        int8_t   i8[16];
        int16_t  i16[8];
        int32_t  i32[4];
        int64_t  i64[2];
        uint8_t  u8[16];
        uint16_t u16[8];
        uint32_t u32[4];
        uint64_t u64[2];
        simd8_t  v64[2];
    };

    bool operator==(const simd16_t& other) const
    {
        return (v64[0] == other.v64[0]) && (v64[1] == other.v64[1]);
    }

    bool operator!=(const simd16_t& other) const
    {
        return !(*this == other);
    }

    static simd16_t AllBitsSet()
    {
        simd16_t result;

        result.v64[0] = simd8_t::AllBitsSet();
        result.v64[1] = simd8_t::AllBitsSet();

        return result;
    }

    bool IsAllBitsSet() const
    {
        return *this == AllBitsSet();
    }

    bool IsZero() const
    {
        return *this == Zero();
    }

    static simd16_t Zero()
    {
        return {};
    }
};
static_assert_no_msg(sizeof(simd16_t) == 16);

#if defined(TARGET_XARCH)
struct simd32_t
{
    union
    {
        float    f32[8];
        double   f64[4];
        int8_t   i8[32];
        int16_t  i16[16];
        int32_t  i32[8];
        int64_t  i64[4];
        uint8_t  u8[32];
        uint16_t u16[16];
        uint32_t u32[8];
        uint64_t u64[4];
        simd8_t  v64[4];
        simd16_t v128[2];
    };

    bool operator==(const simd32_t& other) const
    {
        return (v128[0] == other.v128[0]) && (v128[1] == other.v128[1]);
    }

    bool operator!=(const simd32_t& other) const
    {
        return !(*this == other);
    }

    static simd32_t AllBitsSet()
    {
        simd32_t result;

        result.v128[0] = simd16_t::AllBitsSet();
        result.v128[1] = simd16_t::AllBitsSet();

        return result;
    }

    bool IsAllBitsSet() const
    {
        return *this == AllBitsSet();
    }

    bool IsZero() const
    {
        return *this == Zero();
    }

    static simd32_t Zero()
    {
        return {};
    }
};
static_assert_no_msg(sizeof(simd32_t) == 32);

struct simd64_t
{
    union
    {
        float    f32[16];
        double   f64[8];
        int8_t   i8[64];
        int16_t  i16[32];
        int32_t  i32[16];
        int64_t  i64[8];
        uint8_t  u8[64];
        uint16_t u16[32];
        uint32_t u32[16];
        uint64_t u64[8];
        simd8_t  v64[8];
        simd16_t v128[4];
        simd32_t v256[2];
    };

    bool operator==(const simd64_t& other) const
    {
        return (v256[0] == other.v256[0]) && (v256[1] == other.v256[1]);
    }

    bool operator!=(const simd64_t& other) const
    {
        return !(*this == other);
    }

    static simd64_t AllBitsSet()
    {
        simd64_t result;

        result.v256[0] = simd32_t::AllBitsSet();
        result.v256[1] = simd32_t::AllBitsSet();

        return result;
    }

    bool IsAllBitsSet() const
    {
        return *this == AllBitsSet();
    }

    bool IsZero() const
    {
        return *this == Zero();
    }

    static simd64_t Zero()
    {
        return {};
    }
};
static_assert_no_msg(sizeof(simd64_t) == 64);
#endif // TARGET_XARCH

#if defined(FEATURE_MASKED_HW_INTRINSICS)
struct simdmask_t
{
    union
    {
        int8_t   i8[8];
        int16_t  i16[4];
        int32_t  i32[2];
        int64_t  i64[1];
        uint8_t  u8[8];
        uint16_t u16[4];
        uint32_t u32[2];
        uint64_t u64[1];
    };

    bool operator==(const simdmask_t& other) const
    {
        return (u64[0] == other.u64[0]);
    }

    bool operator!=(const simdmask_t& other) const
    {
        return !(*this == other);
    }

    static simdmask_t AllBitsSet()
    {
        simdmask_t result;

        result.u64[0] = 0xFFFFFFFFFFFFFFFF;

        return result;
    }

    bool IsAllBitsSet() const
    {
        return *this == AllBitsSet();
    }

    bool IsZero() const
    {
        return *this == Zero();
    }

    static simdmask_t Zero()
    {
        return {};
    }
};
static_assert_no_msg(sizeof(simdmask_t) == 8);
#endif // FEATURE_MASKED_HW_INTRINSICS

#if defined(TARGET_XARCH)
typedef simd64_t simd_t;
#else
typedef simd16_t simd_t;
#endif

template <typename TBase>
TBase EvaluateUnaryScalarSpecialized(genTreeOps oper, TBase arg0)
{
    switch (oper)
    {
        case GT_NOT:
        {
            return ~arg0;
        }

        case GT_LZCNT:
        {
            if (sizeof(TBase) == sizeof(uint32_t))
            {
                uint32_t result = BitOperations::LeadingZeroCount(static_cast<uint32_t>(arg0));
                return static_cast<TBase>(result);
            }
            else if (sizeof(TBase) == sizeof(uint64_t))
            {
                uint64_t result = BitOperations::LeadingZeroCount(static_cast<uint64_t>(arg0));
                return static_cast<TBase>(result);
            }

            unreached();
        }

        default:
        {
            unreached();
        }
    }
}

template <>
inline float EvaluateUnaryScalarSpecialized<float>(genTreeOps oper, float arg0)
{
    uint32_t arg0Bits   = BitOperations::SingleToUInt32Bits(arg0);
    uint32_t resultBits = EvaluateUnaryScalarSpecialized<uint32_t>(oper, arg0Bits);
    return BitOperations::UInt32BitsToSingle(resultBits);
}

template <>
inline double EvaluateUnaryScalarSpecialized<double>(genTreeOps oper, double arg0)
{
    uint64_t arg0Bits   = BitOperations::DoubleToUInt64Bits(arg0);
    uint64_t resultBits = EvaluateUnaryScalarSpecialized<uint64_t>(oper, arg0Bits);
    return BitOperations::UInt64BitsToDouble(resultBits);
}

template <typename TBase>
TBase EvaluateUnaryScalar(genTreeOps oper, TBase arg0)
{
    switch (oper)
    {
        case GT_NEG:
        {
            return static_cast<TBase>(0) - arg0;
        }

        default:
        {
            return EvaluateUnaryScalarSpecialized<TBase>(oper, arg0);
        }
    }
}

#if defined(FEATURE_MASKED_HW_INTRINSICS)
template <typename TBase>
void EvaluateUnaryMask(genTreeOps oper, bool scalar, unsigned simdSize, simdmask_t* result, const simdmask_t& arg0)
{
    uint32_t count = simdSize / sizeof(TBase);

#if defined(TARGET_XARCH)
    // For xarch we have count sequential bits, but an 8 count minimum

    if (count < 8)
    {
        count = 8;
    }
    assert((count == 8) || (count == 16) || (count == 32) || (count == 64));

    uint64_t bitMask = static_cast<uint64_t>((static_cast<int64_t>(1) << count) - 1);
#elif defined(TARGET_ARM64)
    // For Arm64 we have count total bits to write, but they are sizeof(TBase) bits apart
    uint64_t bitMask;

    switch (sizeof(TBase))
    {
        case 1:
        {
            bitMask = 0xFFFFFFFFFFFFFFFF;
            break;
        }

        case 2:
        {
            bitMask = 0x5555555555555555;
            break;
        }

        case 4:
        {
            bitMask = 0x1111111111111111;
            break;
        }

        case 8:
        {
            bitMask = 0x0101010101010101;
            break;
        }

        default:
        {
            unreached();
        }
    }
#else
#error Unsupported platform
#endif

    uint64_t arg0Value;
    memcpy(&arg0Value, &arg0.u64[0], sizeof(simdmask_t));

    // We're only considering these bits
    arg0Value &= bitMask;

    uint64_t resultValue = 0;

    switch (oper)
    {
        case GT_NOT:
        {
            resultValue = ~arg0Value;
            break;
        }

        default:
        {
            unreached();
        }
    }

    resultValue &= bitMask;

    if (resultValue == bitMask)
    {
        // Output is equivalent to AllBitsSet, so normalize
        memset(&resultValue, 0xFF, sizeof(uint64_t));
    }
    memcpy(&result->u64[0], &resultValue, sizeof(uint64_t));
}

inline void EvaluateUnaryMask(
    genTreeOps oper, bool scalar, var_types baseType, unsigned simdSize, simdmask_t* result, const simdmask_t& arg0)
{
    switch (baseType)
    {
        case TYP_FLOAT:
        case TYP_INT:
        case TYP_UINT:
        {
            EvaluateUnaryMask<uint32_t>(oper, scalar, simdSize, result, arg0);
            break;
        }

        case TYP_DOUBLE:
        case TYP_LONG:
        case TYP_ULONG:
        {
            EvaluateUnaryMask<uint64_t>(oper, scalar, simdSize, result, arg0);
            break;
        }

        case TYP_BYTE:
        case TYP_UBYTE:
        {
            EvaluateUnaryMask<uint8_t>(oper, scalar, simdSize, result, arg0);
            break;
        }

        case TYP_SHORT:
        case TYP_USHORT:
        {
            EvaluateUnaryMask<uint16_t>(oper, scalar, simdSize, result, arg0);
            break;
        }

        default:
        {
            unreached();
        }
    }
}
#endif // FEATURE_MASKED_HW_INTRINSICS

template <typename TSimd, typename TBase>
void EvaluateUnarySimd(genTreeOps oper, bool scalar, TSimd* result, const TSimd& arg0)
{
    uint32_t count = sizeof(TSimd) / sizeof(TBase);

    if (scalar)
    {
        count = 1;

#if defined(TARGET_XARCH)
        // scalar operations on xarch copy the upper bits from arg0
        *result = arg0;
#elif defined(TARGET_ARM64)
        // scalar operations on arm64 zero the upper bits
        *result = {};
#endif
    }

    for (uint32_t i = 0; i < count; i++)
    {
        // Safely execute `result[i] = oper(arg0[i])`

        TBase input0;
        memcpy(&input0, &arg0.u8[i * sizeof(TBase)], sizeof(TBase));

        TBase output = EvaluateUnaryScalar<TBase>(oper, input0);
        memcpy(&result->u8[i * sizeof(TBase)], &output, sizeof(TBase));
    }
}

template <typename TSimd>
void EvaluateUnarySimd(genTreeOps oper, bool scalar, var_types baseType, TSimd* result, const TSimd& arg0)
{
    switch (baseType)
    {
        case TYP_FLOAT:
        {
            EvaluateUnarySimd<TSimd, float>(oper, scalar, result, arg0);
            break;
        }

        case TYP_DOUBLE:
        {
            EvaluateUnarySimd<TSimd, double>(oper, scalar, result, arg0);
            break;
        }

        case TYP_BYTE:
        {
            EvaluateUnarySimd<TSimd, int8_t>(oper, scalar, result, arg0);
            break;
        }

        case TYP_SHORT:
        {
            EvaluateUnarySimd<TSimd, int16_t>(oper, scalar, result, arg0);
            break;
        }

        case TYP_INT:
        {
            EvaluateUnarySimd<TSimd, int32_t>(oper, scalar, result, arg0);
            break;
        }

        case TYP_LONG:
        {
            EvaluateUnarySimd<TSimd, int64_t>(oper, scalar, result, arg0);
            break;
        }

        case TYP_UBYTE:
        {
            EvaluateUnarySimd<TSimd, uint8_t>(oper, scalar, result, arg0);
            break;
        }

        case TYP_USHORT:
        {
            EvaluateUnarySimd<TSimd, uint16_t>(oper, scalar, result, arg0);
            break;
        }

        case TYP_UINT:
        {
            EvaluateUnarySimd<TSimd, uint32_t>(oper, scalar, result, arg0);
            break;
        }

        case TYP_ULONG:
        {
            EvaluateUnarySimd<TSimd, uint64_t>(oper, scalar, result, arg0);
            break;
        }

        default:
        {
            unreached();
        }
    }
}

template <typename TBase>
TBase EvaluateBinaryScalarRSZ(TBase arg0, TBase arg1)
{
#if defined(TARGET_XARCH)
    if ((arg1 < 0) || (arg1 >= (sizeof(TBase) * 8)))
    {
        // For SIMD, xarch allows overshifting and treats
        // it as zeroing. So ensure we do the same here.
        //
        // The xplat APIs ensure the shiftAmount is masked
        // to be within range, so we can't hit this for them.

        return static_cast<TBase>(0);
    }
#else
    // Other platforms enforce masking in their encoding
    unsigned shiftCountMask = (sizeof(TBase) * 8) - 1;
    arg1 &= shiftCountMask;
#endif

    return arg0 >> arg1;
}

template <>
inline int8_t EvaluateBinaryScalarRSZ<int8_t>(int8_t arg0, int8_t arg1)
{
    uint8_t arg0Bits = static_cast<uint8_t>(arg0);
    uint8_t arg1Bits = static_cast<uint8_t>(arg1);

    uint8_t resultBits = EvaluateBinaryScalarRSZ<uint8_t>(arg0Bits, arg1Bits);
    return static_cast<int8_t>(resultBits);
}

template <>
inline int16_t EvaluateBinaryScalarRSZ<int16_t>(int16_t arg0, int16_t arg1)
{
    uint16_t arg0Bits = static_cast<uint16_t>(arg0);
    uint16_t arg1Bits = static_cast<uint16_t>(arg1);

    uint16_t resultBits = EvaluateBinaryScalarRSZ<uint16_t>(arg0Bits, arg1Bits);
    return static_cast<int16_t>(resultBits);
}

template <>
inline int32_t EvaluateBinaryScalarRSZ<int32_t>(int32_t arg0, int32_t arg1)
{
    uint32_t arg0Bits = static_cast<uint32_t>(arg0);
    uint32_t arg1Bits = static_cast<uint32_t>(arg1);

    uint32_t resultBits = EvaluateBinaryScalarRSZ<uint32_t>(arg0Bits, arg1Bits);
    return static_cast<int32_t>(resultBits);
}

template <>
inline int64_t EvaluateBinaryScalarRSZ<int64_t>(int64_t arg0, int64_t arg1)
{
    uint64_t arg0Bits = static_cast<uint64_t>(arg0);
    uint64_t arg1Bits = static_cast<uint64_t>(arg1);

    uint64_t resultBits = EvaluateBinaryScalarRSZ<uint64_t>(arg0Bits, arg1Bits);
    return static_cast<int64_t>(resultBits);
}

template <typename TBase>
TBase EvaluateBinaryScalarSpecialized(genTreeOps oper, TBase arg0, TBase arg1)
{
    switch (oper)
    {
        case GT_AND:
        {
            return arg0 & arg1;
        }

        case GT_AND_NOT:
        {
            return arg0 & ~arg1;
        }

        case GT_EQ:
        {
            return (arg0 == arg1) ? static_cast<TBase>(~0) : static_cast<TBase>(0);
        }

        case GT_GT:
        {
            return (arg0 > arg1) ? static_cast<TBase>(~0) : static_cast<TBase>(0);
        }

        case GT_GE:
        {
            return (arg0 >= arg1) ? static_cast<TBase>(~0) : static_cast<TBase>(0);
        }

        case GT_LSH:
        {
#if defined(TARGET_XARCH)
            if ((arg1 < 0) || (arg1 >= (sizeof(TBase) * 8)))
            {
                // For SIMD, xarch allows overshifting and treats
                // it as zeroing. So ensure we do the same here.
                //
                // The xplat APIs ensure the shiftAmount is masked
                // to be within range, so we can't hit this for them.

                return static_cast<TBase>(0);
            }
#else
            // Other platforms enforce masking in their encoding
            unsigned shiftCountMask = (sizeof(TBase) * 8) - 1;
            arg1 &= shiftCountMask;
#endif
            return arg0 << arg1;
        }

        case GT_LT:
        {
            return (arg0 < arg1) ? static_cast<TBase>(~0) : static_cast<TBase>(0);
        }

        case GT_LE:
        {
            return (arg0 <= arg1) ? static_cast<TBase>(~0) : static_cast<TBase>(0);
        }

        case GT_NE:
        {
            return (arg0 != arg1) ? static_cast<TBase>(~0) : static_cast<TBase>(0);
        }

        case GT_OR:
        {
            return arg0 | arg1;
        }

        case GT_ROL:
        {
            // Normalize the "rotate by" value
            arg1 %= (sizeof(TBase) * BITS_PER_BYTE);
            return EvaluateBinaryScalarSpecialized<TBase>(GT_LSH, arg0, arg1) |
                   EvaluateBinaryScalarRSZ<TBase>(arg0, (sizeof(TBase) * 8) - arg1);
        }

        case GT_ROR:
        {
            // Normalize the "rotate by" value
            arg1 %= (sizeof(TBase) * BITS_PER_BYTE);
            return EvaluateBinaryScalarRSZ<TBase>(arg0, arg1) |
                   EvaluateBinaryScalarSpecialized<TBase>(GT_LSH, arg0, (sizeof(TBase) * 8) - arg1);
        }

        case GT_RSH:
        {
#if defined(TARGET_XARCH)
            if ((arg1 < 0) || (arg1 >= (sizeof(TBase) * 8)))
            {
                // For SIMD, xarch allows overshifting and treats
                // it as propagating the sign bit (returning Zero
                // or AllBitsSet). So ensure we do the same here.
                //
                // The xplat APIs ensure the shiftAmount is masked
                // to be within range, so we can't hit this for them.

                arg0 >>= ((sizeof(TBase) * 8) - 1);
                arg1 = static_cast<TBase>(1);
            }
#else
            // Other platforms enforce masking in their encoding
            unsigned shiftCountMask = (sizeof(TBase) * 8) - 1;
            arg1 &= shiftCountMask;
#endif
            return arg0 >> arg1;
        }

        case GT_RSZ:
        {
            return EvaluateBinaryScalarRSZ<TBase>(arg0, arg1);
        }

        case GT_XOR:
        {
            return arg0 ^ arg1;
        }

        default:
        {
            unreached();
        }
    }
}

template <>
inline float EvaluateBinaryScalarSpecialized<float>(genTreeOps oper, float arg0, float arg1)
{
    switch (oper)
    {
        case GT_EQ:
        {
            return (arg0 == arg1) ? BitOperations::UInt32BitsToSingle(0xFFFFFFFF) : 0;
        }

        case GT_GT:
        {
            return (arg0 > arg1) ? BitOperations::UInt32BitsToSingle(0xFFFFFFFF) : 0;
        }

        case GT_GE:
        {
            return (arg0 >= arg1) ? BitOperations::UInt32BitsToSingle(0xFFFFFFFF) : 0;
        }

        case GT_LT:
        {
            return (arg0 < arg1) ? BitOperations::UInt32BitsToSingle(0xFFFFFFFF) : 0;
        }

        case GT_LE:
        {
            return (arg0 <= arg1) ? BitOperations::UInt32BitsToSingle(0xFFFFFFFF) : 0;
        }

        case GT_NE:
        {
            return (arg0 != arg1) ? BitOperations::UInt32BitsToSingle(0xFFFFFFFF) : 0;
        }

        default:
        {
            uint32_t arg0Bits = BitOperations::SingleToUInt32Bits(arg0);
            uint32_t arg1Bits = BitOperations::SingleToUInt32Bits(arg1);

            uint32_t resultBits = EvaluateBinaryScalarSpecialized<uint32_t>(oper, arg0Bits, arg1Bits);
            return BitOperations::UInt32BitsToSingle(resultBits);
        }
    }
}

template <>
inline double EvaluateBinaryScalarSpecialized<double>(genTreeOps oper, double arg0, double arg1)
{
    switch (oper)
    {
        case GT_EQ:
        {
            return (arg0 == arg1) ? BitOperations::UInt64BitsToDouble(0xFFFFFFFFFFFFFFFF) : 0;
        }

        case GT_GT:
        {
            return (arg0 > arg1) ? BitOperations::UInt64BitsToDouble(0xFFFFFFFFFFFFFFFF) : 0;
        }

        case GT_GE:
        {
            return (arg0 >= arg1) ? BitOperations::UInt64BitsToDouble(0xFFFFFFFFFFFFFFFF) : 0;
        }

        case GT_LT:
        {
            return (arg0 < arg1) ? BitOperations::UInt64BitsToDouble(0xFFFFFFFFFFFFFFFF) : 0;
        }

        case GT_LE:
        {
            return (arg0 <= arg1) ? BitOperations::UInt64BitsToDouble(0xFFFFFFFFFFFFFFFF) : 0;
        }

        case GT_NE:
        {
            return (arg0 != arg1) ? BitOperations::UInt64BitsToDouble(0xFFFFFFFFFFFFFFFF) : 0;
        }

        default:
        {
            uint64_t arg0Bits = BitOperations::DoubleToUInt64Bits(arg0);
            uint64_t arg1Bits = BitOperations::DoubleToUInt64Bits(arg1);

            uint64_t resultBits = EvaluateBinaryScalarSpecialized<uint64_t>(oper, arg0Bits, arg1Bits);
            return BitOperations::UInt64BitsToDouble(resultBits);
        }
    }
}

template <typename TBase>
TBase EvaluateBinaryScalar(genTreeOps oper, TBase arg0, TBase arg1)
{
    switch (oper)
    {
        case GT_ADD:
        {
            return arg0 + arg1;
        }

        case GT_DIV:
        {
            return arg0 / arg1;
        }

        case GT_MUL:
        {
            return arg0 * arg1;
        }

        case GT_SUB:
        {
            return arg0 - arg1;
        }

        default:
        {
            return EvaluateBinaryScalarSpecialized<TBase>(oper, arg0, arg1);
        }
    }
}

#if defined(FEATURE_MASKED_HW_INTRINSICS)
template <typename TBase>
void EvaluateBinaryMask(
    genTreeOps oper, bool scalar, unsigned simdSize, simdmask_t* result, const simdmask_t& arg0, const simdmask_t& arg1)
{
    uint32_t count = simdSize / sizeof(TBase);

#if defined(TARGET_XARCH)
    // For xarch we have count sequential bits, but an 8 count minimum

    if (count < 8)
    {
        count = 8;
    }
    assert((count == 8) || (count == 16) || (count == 32) || (count == 64));

    uint64_t bitMask = static_cast<uint64_t>((static_cast<int64_t>(1) << count) - 1);
#elif defined(TARGET_ARM64)
    // For Arm64 we have count total bits to write, but they are sizeof(TBase) bits apart
    uint64_t bitMask;

    switch (sizeof(TBase))
    {
        case 1:
        {
            bitMask = 0xFFFFFFFFFFFFFFFF;
            break;
        }

        case 2:
        {
            bitMask = 0x5555555555555555;
            break;
        }

        case 4:
        {
            bitMask = 0x1111111111111111;
            break;
        }

        case 8:
        {
            bitMask = 0x0101010101010101;
            break;
        }

        default:
        {
            unreached();
        }
    }
#else
#error Unsupported platform
#endif

    uint64_t arg0Value;
    memcpy(&arg0Value, &arg0.u64[0], sizeof(simdmask_t));

    uint64_t arg1Value;
    memcpy(&arg1Value, &arg1.u64[0], sizeof(simdmask_t));

    // We're only considering these bits
    arg0Value &= bitMask;
    arg1Value &= bitMask;

    uint64_t resultValue = 0;

    switch (oper)
    {
        case GT_AND_NOT:
        {
            resultValue = arg0Value & ~arg1Value;
            break;
        }

        case GT_AND:
        {
            resultValue = arg0Value & arg1Value;
            break;
        }

        case GT_OR:
        {
            resultValue = arg0Value | arg1Value;
            break;
        }

        case GT_XOR:
        {
            resultValue = arg0Value ^ arg1Value;
            break;
        }

        default:
        {
            unreached();
        }
    }

    resultValue &= bitMask;

    if (resultValue == bitMask)
    {
        // Output is equivalent to AllBitsSet, so normalize
        memset(&resultValue, 0xFF, sizeof(uint64_t));
    }
    memcpy(&result->u64[0], &resultValue, sizeof(uint64_t));
}

inline void EvaluateBinaryMask(genTreeOps        oper,
                               bool              scalar,
                               var_types         baseType,
                               unsigned          simdSize,
                               simdmask_t*       result,
                               const simdmask_t& arg0,
                               const simdmask_t& arg1)
{
    switch (baseType)
    {
        case TYP_FLOAT:
        case TYP_INT:
        case TYP_UINT:
        {
            EvaluateBinaryMask<uint32_t>(oper, scalar, simdSize, result, arg0, arg1);
            break;
        }

        case TYP_DOUBLE:
        case TYP_LONG:
        case TYP_ULONG:
        {
            EvaluateBinaryMask<uint64_t>(oper, scalar, simdSize, result, arg0, arg1);
            break;
        }

        case TYP_BYTE:
        case TYP_UBYTE:
        {
            EvaluateBinaryMask<uint8_t>(oper, scalar, simdSize, result, arg0, arg1);
            break;
        }

        case TYP_SHORT:
        case TYP_USHORT:
        {
            EvaluateBinaryMask<uint16_t>(oper, scalar, simdSize, result, arg0, arg1);
            break;
        }

        default:
        {
            unreached();
        }
    }
}
#endif // FEATURE_MASKED_HW_INTRINSICS

template <typename TSimd, typename TBase>
void EvaluateBinarySimd(genTreeOps oper, bool scalar, TSimd* result, const TSimd& arg0, const TSimd& arg1)
{
    uint32_t count = sizeof(TSimd) / sizeof(TBase);

    if (scalar)
    {
        count = 1;

#if defined(TARGET_XARCH)
        // scalar operations on xarch copy the upper bits from arg0
        *result = arg0;
#elif defined(TARGET_ARM64)
        // scalar operations on arm64 zero the upper bits
        *result = {};
#endif
    }

    for (uint32_t i = 0; i < count; i++)
    {
        // Safely execute `result[i] = oper(arg0[i], arg1[i])`

        TBase input0;
        memcpy(&input0, &arg0.u8[i * sizeof(TBase)], sizeof(TBase));

        TBase input1;
        memcpy(&input1, &arg1.u8[i * sizeof(TBase)], sizeof(TBase));

        TBase output = EvaluateBinaryScalar<TBase>(oper, input0, input1);
        memcpy(&result->u8[i * sizeof(TBase)], &output, sizeof(TBase));
    }
}

template <typename TSimd>
void EvaluateBinarySimd(
    genTreeOps oper, bool scalar, var_types baseType, TSimd* result, const TSimd& arg0, const TSimd& arg1)
{
    switch (baseType)
    {
        case TYP_FLOAT:
        {
            EvaluateBinarySimd<TSimd, float>(oper, scalar, result, arg0, arg1);
            break;
        }

        case TYP_DOUBLE:
        {
            EvaluateBinarySimd<TSimd, double>(oper, scalar, result, arg0, arg1);
            break;
        }

        case TYP_BYTE:
        {
            EvaluateBinarySimd<TSimd, int8_t>(oper, scalar, result, arg0, arg1);
            break;
        }

        case TYP_SHORT:
        {
            EvaluateBinarySimd<TSimd, int16_t>(oper, scalar, result, arg0, arg1);
            break;
        }

        case TYP_INT:
        {
            EvaluateBinarySimd<TSimd, int32_t>(oper, scalar, result, arg0, arg1);
            break;
        }

        case TYP_LONG:
        {
            EvaluateBinarySimd<TSimd, int64_t>(oper, scalar, result, arg0, arg1);
            break;
        }

        case TYP_UBYTE:
        {
            EvaluateBinarySimd<TSimd, uint8_t>(oper, scalar, result, arg0, arg1);
            break;
        }

        case TYP_USHORT:
        {
            EvaluateBinarySimd<TSimd, uint16_t>(oper, scalar, result, arg0, arg1);
            break;
        }

        case TYP_UINT:
        {
            EvaluateBinarySimd<TSimd, uint32_t>(oper, scalar, result, arg0, arg1);
            break;
        }

        case TYP_ULONG:
        {
            EvaluateBinarySimd<TSimd, uint64_t>(oper, scalar, result, arg0, arg1);
            break;
        }

        default:
        {
            unreached();
        }
    }
}

template <typename TSimd>
double EvaluateGetElementFloating(var_types simdBaseType, const TSimd& arg0, int32_t arg1)
{
    switch (simdBaseType)
    {
        case TYP_FLOAT:
        {
            return arg0.f32[arg1];
        }

        case TYP_DOUBLE:
        {
            return arg0.f64[arg1];
        }

        default:
        {
            unreached();
        }
    }
}

template <typename TSimd>
int64_t EvaluateGetElementIntegral(var_types simdBaseType, const TSimd& arg0, int32_t arg1)
{
    switch (simdBaseType)
    {
        case TYP_BYTE:
        {
            return arg0.i8[arg1];
        }

        case TYP_UBYTE:
        {
            return arg0.u8[arg1];
        }

        case TYP_SHORT:
        {
            return arg0.i16[arg1];
        }

        case TYP_USHORT:
        {
            return arg0.u16[arg1];
        }

        case TYP_INT:
        {
            return arg0.i32[arg1];
        }

        case TYP_UINT:
        {
            return arg0.u32[arg1];
        }

        case TYP_LONG:
        {
            return arg0.i64[arg1];
        }

        case TYP_ULONG:
        {
            return static_cast<int64_t>(arg0.u64[arg1]);
        }

        default:
        {
            unreached();
        }
    }
}

template <typename TSimd>
void EvaluateWithElementFloating(var_types simdBaseType, TSimd* result, const TSimd& arg0, int32_t arg1, double arg2)
{
    *result = arg0;

    switch (simdBaseType)
    {
        case TYP_FLOAT:
        {
            result->f32[arg1] = static_cast<float>(arg2);
            break;
        }

        case TYP_DOUBLE:
        {
            result->f64[arg1] = static_cast<float>(arg2);
            break;
        }

        default:
        {
            unreached();
        }
    }
}

template <typename TSimd>
void EvaluateWithElementIntegral(var_types simdBaseType, TSimd* result, const TSimd& arg0, int32_t arg1, int64_t arg2)
{
    *result = arg0;

    switch (simdBaseType)
    {
        case TYP_BYTE:
        {
            result->i8[arg1] = static_cast<int8_t>(arg2);
            break;
        }

        case TYP_UBYTE:
        {
            result->u8[arg1] = static_cast<uint8_t>(arg2);
            break;
        }

        case TYP_SHORT:
        {
            result->i16[arg1] = static_cast<int16_t>(arg2);
            break;
        }

        case TYP_USHORT:
        {
            result->u16[arg1] = static_cast<uint16_t>(arg2);
            break;
        }

        case TYP_INT:
        {
            result->i32[arg1] = static_cast<int32_t>(arg2);
            break;
        }

        case TYP_UINT:
        {
            result->u32[arg1] = static_cast<uint32_t>(arg2);
            break;
        }

        case TYP_LONG:
        {
            result->i64[arg1] = static_cast<int64_t>(arg2);
            break;
        }

        case TYP_ULONG:
        {
            result->u64[arg1] = static_cast<uint64_t>(arg2);
            break;
        }

        default:
        {
            unreached();
        }
    }
}

template <typename TSimd, typename TBase>
void BroadcastConstantToSimd(TSimd* result, TBase arg0)
{
    uint32_t count = sizeof(TSimd) / sizeof(TBase);

    for (uint32_t i = 0; i < count; i++)
    {
        // Safely execute `result[i] = arg0`
        memcpy(&result->u8[i * sizeof(TBase)], &arg0, sizeof(TBase));
    }
}

#if defined(FEATURE_MASKED_HW_INTRINSICS)
template <typename TSimd, typename TBase>
void EvaluateSimdCvtMaskToVector(TSimd* result, simdmask_t arg0)
{
    uint32_t count = sizeof(TSimd) / sizeof(TBase);

    uint64_t mask;
    memcpy(&mask, &arg0.u8[0], sizeof(uint64_t));

    for (uint32_t i = 0; i < count; i++)
    {
        bool isSet;

#if defined(TARGET_XARCH)
        // For xarch we have count sequential bits to read
        // setting the result element to AllBitsSet or Zero
        // depending on the corresponding mask bit

        isSet = ((mask >> i) & 1) != 0;
#elif defined(TARGET_ARM64)
        // For Arm64 we have count total bits to read, but
        // they are sizeof(TBase) bits apart. We still set
        // the result element to AllBitsSet or Zero depending
        // on the corresponding mask bit

        isSet = ((mask >> (i * sizeof(TBase))) & 1) != 0;
#else
        unreached();
#endif

        TBase output;

        if (isSet)
        {
            memset(&output, 0xFF, sizeof(TBase));
        }
        else
        {
            memset(&output, 0x00, sizeof(TBase));
        }

        memcpy(&result->u8[i * sizeof(TBase)], &output, sizeof(TBase));
    }
}

template <typename TSimd>
void EvaluateSimdCvtMaskToVector(var_types baseType, TSimd* result, simdmask_t arg0)
{
    switch (baseType)
    {
        case TYP_FLOAT:
        case TYP_INT:
        case TYP_UINT:
        {
            EvaluateSimdCvtMaskToVector<TSimd, uint32_t>(result, arg0);
            break;
        }

        case TYP_DOUBLE:
        case TYP_LONG:
        case TYP_ULONG:
        {
            EvaluateSimdCvtMaskToVector<TSimd, uint64_t>(result, arg0);
            break;
        }

        case TYP_BYTE:
        case TYP_UBYTE:
        {
            EvaluateSimdCvtMaskToVector<TSimd, uint8_t>(result, arg0);
            break;
        }

        case TYP_SHORT:
        case TYP_USHORT:
        {
            EvaluateSimdCvtMaskToVector<TSimd, uint16_t>(result, arg0);
            break;
        }

        default:
        {
            unreached();
        }
    }
}

template <typename TSimd, typename TBase>
void EvaluateSimdCvtVectorToMask(simdmask_t* result, TSimd arg0)
{
    uint32_t count = sizeof(TSimd) / sizeof(TBase);
    uint64_t mask  = 0;

    TBase mostSignificantBit = static_cast<TBase>(1) << ((sizeof(TBase) * 8) - 1);

    for (uint32_t i = 0; i < count; i++)
    {
        TBase input0;
        memcpy(&input0, &arg0.u8[i * sizeof(TBase)], sizeof(TBase));

        if ((input0 & mostSignificantBit) != 0)
        {
#if defined(TARGET_XARCH)
            // For xarch we have count sequential bits to write
            // depending on if the corresponding the input element
            // has its most significant bit set

            mask |= static_cast<uint64_t>(1) << i;
#elif defined(TARGET_ARM64)
            // For Arm64 we have count total bits to write, but
            // they are sizeof(TBase) bits apart. We still set
            // depending on if the corresponding input element
            // has its most significant bit set

            mask |= static_cast<uint64_t>(1) << (i * sizeof(TBase));
#else
            unreached();
#endif
        }
    }

    memcpy(&result->u8[0], &mask, sizeof(uint64_t));
}

template <typename TSimd>
void EvaluateSimdCvtVectorToMask(var_types baseType, simdmask_t* result, TSimd arg0)
{
    switch (baseType)
    {
        case TYP_FLOAT:
        case TYP_INT:
        case TYP_UINT:
        {
            EvaluateSimdCvtVectorToMask<TSimd, uint32_t>(result, arg0);
            break;
        }

        case TYP_DOUBLE:
        case TYP_LONG:
        case TYP_ULONG:
        {
            EvaluateSimdCvtVectorToMask<TSimd, uint64_t>(result, arg0);
            break;
        }

        case TYP_BYTE:
        case TYP_UBYTE:
        {
            EvaluateSimdCvtVectorToMask<TSimd, uint8_t>(result, arg0);
            break;
        }

        case TYP_SHORT:
        case TYP_USHORT:
        {
            EvaluateSimdCvtVectorToMask<TSimd, uint16_t>(result, arg0);
            break;
        }

        default:
        {
            unreached();
        }
    }
}
#endif // FEATURE_MASKED_HW_INTRINSICS

#ifdef FEATURE_SIMD

#ifdef TARGET_XARCH
// SSE2 Shuffle control byte to shuffle vector <W, Z, Y, X>
// These correspond to shuffle immediate byte in shufps SSE2 instruction.
#define SHUFFLE_XXXX 0x00 // 00 00 00 00
#define SHUFFLE_XXZX 0x08 // 00 00 10 00
#define SHUFFLE_XXWW 0x0F // 00 00 11 11
#define SHUFFLE_XYZW 0x1B // 00 01 10 11
#define SHUFFLE_YXYX 0x44 // 01 00 01 00
#define SHUFFLE_YWXZ 0x72 // 01 11 00 10
#define SHUFFLE_YWXW 0x73 // 01 11 00 11
#define SHUFFLE_YYZZ 0x5A // 01 01 10 10
#define SHUFFLE_ZXXX 0x80 // 10 00 00 00
#define SHUFFLE_ZXXY 0x81 // 10 00 00 01
#define SHUFFLE_ZWXY 0xB1 // 10 11 00 01
#define SHUFFLE_WYZX 0xD8 // 11 01 10 00
#define SHUFFLE_WWYY 0xF5 // 11 11 01 01
#define SHUFFLE_ZZXX 0xA0 // 10 10 00 00
#endif

#endif // FEATURE_SIMD

#endif //_SIMD_H_
