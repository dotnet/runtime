// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _SIMD_H_
#define _SIMD_H_

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

#ifdef _TARGET_XARCH_
// SSE2 Shuffle control byte to shuffle vector <W, Z, Y, X>
// These correspond to shuffle immediate byte in shufps SSE2 instruction.
#define SHUFFLE_XXXX 0x00 // 00 00 00 00
#define SHUFFLE_XXWW 0x0F // 00 00 11 11
#define SHUFFLE_XYZW 0x1B // 00 01 10 11
#define SHUFFLE_YXYX 0x44 // 01 00 01 00
#define SHUFFLE_YYZZ 0x5A // 01 01 10 10
#define SHUFFLE_ZXXY 0x81 // 10 00 00 01
#define SHUFFLE_ZWXY 0xB1 // 10 11 00 01
#define SHUFFLE_WWYY 0xF5 // 11 11 01 01
#define SHUFFLE_ZZXX 0xA0 // 10 10 00 00
#endif

#endif // FEATURE_SIMD

#endif //_SIMD_H_
