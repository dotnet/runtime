// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _NAMEDINTRINSICLIST_H_
#define _NAMEDINTRINSICLIST_H_

// Named jit intrinsics

enum NamedIntrinsic : unsigned short
{
    NI_Illegal = 0,

    NI_System_Enum_HasFlag,

    NI_SYSTEM_MATH_START,
    NI_System_Math_Abs,
    NI_System_Math_Acos,
    NI_System_Math_Acosh,
    NI_System_Math_Asin,
    NI_System_Math_Asinh,
    NI_System_Math_Atan,
    NI_System_Math_Atanh,
    NI_System_Math_Atan2,
    NI_System_Math_Cbrt,
    NI_System_Math_Ceiling,
    NI_System_Math_Cos,
    NI_System_Math_Cosh,
    NI_System_Math_Exp,
    NI_System_Math_Floor,
    NI_System_Math_FMod,
    NI_System_Math_FusedMultiplyAdd,
    NI_System_Math_ILogB,
    NI_System_Math_Log,
    NI_System_Math_Log2,
    NI_System_Math_Log10,
    NI_System_Math_Pow,
    NI_System_Math_Round,
    NI_System_Math_Sin,
    NI_System_Math_Sinh,
    NI_System_Math_Sqrt,
    NI_System_Math_Tan,
    NI_System_Math_Tanh,
    NI_SYSTEM_MATH_END,

    NI_System_Collections_Generic_Comparer_get_Default,
    NI_System_Collections_Generic_EqualityComparer_get_Default,
    NI_System_Buffers_Binary_BinaryPrimitives_ReverseEndianness,
    NI_System_Numerics_BitOperations_PopCount,
    NI_System_GC_KeepAlive,
    NI_System_Threading_Thread_get_CurrentThread,
    NI_System_Threading_Thread_get_ManagedThreadId,
    NI_System_Type_get_IsValueType,
    NI_System_Type_IsAssignableFrom,
    NI_System_Type_IsAssignableTo,
    NI_System_Array_Clone,
    NI_System_Object_MemberwiseClone,

    // These are used by HWIntrinsics but are defined more generally
    // to allow dead code optimization and handle the recursion case

    NI_IsSupported_True,
    NI_IsSupported_False,
    NI_IsSupported_Dynamic,
    NI_Throw_PlatformNotSupportedException,

    NI_System_Threading_Interlocked_And,
    NI_System_Threading_Interlocked_Or,

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
