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

#ifdef FEATURE_SIMD

#ifdef DEBUG
extern const char* const simdIntrinsicNames[];
#endif

enum SIMDIntrinsicID : uint16_t
{
#define SIMD_INTRINSIC(m, i, id, n, r, ac, arg1, arg2, arg3, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10) SIMDIntrinsic##id,
#include "simdintrinsiclist.h"
};

// Static info about a SIMD intrinsic
struct SIMDIntrinsicInfo
{
    SIMDIntrinsicID id;
    const char*     methodName;
    bool            isInstMethod;
    var_types       retType;
    unsigned char   argCount;
    var_types       argType[SIMD_INTRINSIC_MAX_MODELED_PARAM_COUNT];
    var_types       supportedBaseTypes[SIMD_INTRINSIC_MAX_BASETYPE_COUNT];
};

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
