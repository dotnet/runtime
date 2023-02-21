// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _SIMD_H_
#define _SIMD_H_

// Underlying hardware information
// This type is used to control
// 1. The length of System.Numerics.Vector<T>.
// 2. Codegen of System.Numerics.Vectors.
// 3. Codegen of floating-point arithmetics (VEX-encoding or not).
//
// Note
// - Hardware SIMD support is classified to the levels. Do not directly use
//   InstructionSet (instr.h) for System.Numerics.Vectors.
// - Values of SIMDLevel have strictly increasing order that each SIMD level
//   is a superset of the previous levels.
enum SIMDLevel
{
    SIMD_Not_Supported = 0,
#ifdef TARGET_XARCH
    // SSE2 - The min bar of SIMD ISA on x86/x64.
    // Vector<T> length is 128-bit.
    // Floating-point instructions are legacy SSE encoded.
    SIMD_SSE2_Supported = 1,

    // SSE4 - RyuJIT may generate SSE3, SSSE3, SSE4.1 and SSE4.2 instructions for certain intrinsics.
    // Vector<T> length is 128-bit.
    // Floating-point instructions are legacy SSE encoded.
    SIMD_SSE4_Supported = 2,

    // AVX2 - Hardware has AVX and AVX2 instruction set.
    // Vector<T> length is 256-bit and SIMD instructions are VEX-256 encoded.
    // Floating-point instructions are VEX-128 encoded.
    SIMD_AVX2_Supported = 3
#endif
};

struct simd8_t
{
    union {
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
        return (u64[0] != other.u64[0]);
    }
};

struct simd12_t
{
    union {
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
        return (u32[0] != other.u32[0]) || (u32[1] != other.u32[1]) || (u32[2] != other.u32[2]);
    }
};

struct simd16_t
{
    union {
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
        return (u64[0] == other.u64[0]) && (u64[1] == other.u64[1]);
    }

    bool operator!=(const simd16_t& other) const
    {
        return (u64[0] != other.u64[0]) || (u64[1] != other.u64[1]);
    }
};

struct simd32_t
{
    union {
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
        return (u64[0] == other.u64[0]) && (u64[1] == other.u64[1]) && (u64[2] == other.u64[2]) &&
               (u64[3] == other.u64[3]);
    }

    bool operator!=(const simd32_t& other) const
    {
        return (u64[0] != other.u64[0]) || (u64[1] != other.u64[1]) || (u64[2] != other.u64[2]) ||
               (u64[3] != other.u64[3]);
    }
};

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
            unreached();
        }
    }
}

template <typename TSimd, typename TBase>
void EvaluateUnarySimd(genTreeOps oper, bool scalar, TSimd* result, TSimd arg0)
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
void EvaluateUnarySimd(genTreeOps oper, bool scalar, var_types baseType, TSimd* result, TSimd arg0)
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
TBase EvaluateBinaryScalar(genTreeOps oper, TBase arg0, TBase arg1)
{
    switch (oper)
    {
        case GT_ADD:
        {
            return arg0 + arg1;
        }

        case GT_SUB:
        {
            return arg0 - arg1;
        }

        default:
        {
            unreached();
        }
    }
}

template <typename TSimd, typename TBase>
void EvaluateBinarySimd(genTreeOps oper, bool scalar, TSimd* result, TSimd arg0, TSimd arg1)
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
void EvaluateBinarySimd(genTreeOps oper, bool scalar, var_types baseType, TSimd* result, TSimd arg0, TSimd arg1)
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
