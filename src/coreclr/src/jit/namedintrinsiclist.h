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

#ifdef FEATURE_HW_INTRINSICS
    NI_HW_INTRINSIC_START,
#if defined(_TARGET_XARCH_)
#define HARDWARE_INTRINSIC(id, name, isa, ival, size, numarg, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, category, flag) \
    NI_##id,
#include "hwintrinsiclistxarch.h"
#elif defined(_TARGET_ARM64_)
    NI_ARM64_IsSupported_False,
    NI_ARM64_IsSupported_True,
    NI_ARM64_PlatformNotSupported,
#define HARDWARE_INTRINSIC(id, isa, name, form, ins0, ins1, ins2, flags) id,
#include "hwintrinsiclistArm64.h"
#endif // !defined(_TARGET_XARCH_) && !defined(_TARGET_ARM64_)
    NI_HW_INTRINSIC_END,
#endif // FEATURE_HW_INTRINSICS
};

#endif // _NAMEDINTRINSICLIST_H_
