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

#ifdef FEATURE_SIMD

#ifdef DEBUG
extern const char* const simdIntrinsicNames[];
#endif

enum SIMDIntrinsicID
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
