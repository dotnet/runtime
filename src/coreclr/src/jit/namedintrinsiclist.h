// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _NAMEDINTRINSICLIST_H_
#define _NAMEDINTRINSICLIST_H_

// Named jit intrinsics

enum NamedIntrinsic : unsigned short
{
    NI_Illegal = 0,

    NI_System_Enum_HasFlag,
    NI_System_Math_FusedMultiplyAdd,
    NI_System_Math_Round,
    NI_System_MathF_FusedMultiplyAdd,
    NI_System_MathF_Round,
    NI_System_Collections_Generic_EqualityComparer_get_Default,
    NI_System_Buffers_Binary_BinaryPrimitives_ReverseEndianness,
    NI_System_GC_KeepAlive,
    NI_System_Type_get_IsValueType,
    NI_System_Type_IsAssignableFrom,

    // These are used by HWIntrinsics but are defined more generally
    // to allow dead code optimization and handle the recursion case

    NI_IsSupported_True,
    NI_IsSupported_False,
    NI_Throw_PlatformNotSupportedException,

#ifdef FEATURE_HW_INTRINSICS
    NI_HW_INTRINSIC_START,
#if defined(TARGET_XARCH)
#define HARDWARE_INTRINSIC(isa, name, size, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag)           \
    NI_##isa##_##name,
#include "hwintrinsiclistxarch.h"
#elif defined(TARGET_ARM64)
#define HARDWARE_INTRINSIC(isa, name, size, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag)           \
    NI_##isa##_##name,
#include "hwintrinsiclistarm64.h"
#endif // !defined(TARGET_XARCH) && !defined(TARGET_ARM64)
    NI_HW_INTRINSIC_END,

    NI_SIMD_AS_HWINTRINSIC_START,
#if defined(TARGET_XARCH)
#define SIMD_AS_HWINTRINSIC(classId, id, name, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, flag)                  \
    NI_##classId##_##id,
#include "simdashwintrinsiclistxarch.h"
#elif defined(TARGET_ARM64)
#define SIMD_AS_HWINTRINSIC(classId, id, name, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, flag)                  \
    NI_##classId##_##id,
#include "simdashwintrinsiclistarm64.h"
#endif // !defined(TARGET_XARCH) && !defined(TARGET_ARM64)
    NI_SIMD_AS_HWINTRINSIC_END,
#endif // FEATURE_HW_INTRINSICS

};

#endif // _NAMEDINTRINSICLIST_H_
