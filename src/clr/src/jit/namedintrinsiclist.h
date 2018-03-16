// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _NAMEDINTRINSICLIST_H_
#define _NAMEDINTRINSICLIST_H_

// Named jit intrinsics

enum NamedIntrinsic : unsigned int
{
    NI_Illegal                                                 = 0,
    NI_System_Enum_HasFlag                                     = 1,
    NI_MathF_Round                                             = 2,
    NI_Math_Round                                              = 3,
    NI_System_Collections_Generic_EqualityComparer_get_Default = 4,
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

#if defined(FEATURE_HW_INTRINSICS) && defined(_TARGET_XARCH_)
enum HWIntrinsicFlag : unsigned int
{
    HW_Flag_NoFlag = 0,

    // Commutative
    // - if a binary-op intrinsic is commutative (e.g., Add, Multiply), its op1 can be contained
    HW_Flag_Commutative = 0x1,

    // Full range IMM intrinsic
    // - the immediate value is valid on the full range of imm8 (0-255)
    HW_Flag_FullRangeIMM = 0x2,

    // Generic
    // - must throw NotSupportException if the type argument is not numeric type
    HW_Flag_OneTypeGeneric = 0x4,
    // Two-type Generic
    // - the intrinsic has two type parameters
    HW_Flag_TwoTypeGeneric = 0x8,

    // NoCodeGen
    // - should be transformed in the compiler front-end, cannot reach CodeGen
    HW_Flag_NoCodeGen = 0x10,

    // Unfixed SIMD-size
    // - overloaded on multiple vector sizes (SIMD size in the table is unreliable)
    HW_Flag_UnfixedSIMDSize = 0x20,

    // Complex overload
    // - the codegen of overloads cannot be determined by intrinsicID and base type
    HW_Flag_ComplexOverloads = 0x40,

    // Multi-instruction
    // - that one intrinsic can generate multiple instructions
    HW_Flag_MultiIns = 0x80,

    // NoContainment
    // the intrinsic cannot be contained
    HW_Flag_NoContainment = 0x100,

    // Copy Upper bits
    // some SIMD scalar intrinsics need the semantics of copying upper bits from the source operand
    HW_Flag_CopyUpperBits = 0x200,

    // Select base type using the first argument type
    HW_Flag_BaseTypeFromFirstArg = 0x400,

    // Indicates compFloatingPointUsed does not need to be set.
    HW_Flag_NoFloatingPointUsed = 0x800,

    // Maybe IMM
    // the intrinsic has either imm or Vector overloads
    HW_Flag_MaybeIMM = 0x1000,

    // NoJmpTable IMM
    // the imm intrinsic does not need jumptable fallback when it gets non-const argument
    HW_Flag_NoJmpTableIMM = 0x2000,

    // 64-bit intrinsics
    // Intrinsics that operate over 64-bit general purpose registers are not supported on 32-bit platform
    HW_Flag_64BitOnly           = 0x4000,
    HW_Flag_SecondArgMaybe64Bit = 0x8000,

    // Select base type using the second argument type
    HW_Flag_BaseTypeFromSecondArg = 0x10000,

    // Special codegen
    // the intrinsics need special rules in CodeGen,
    // but may be table-driven in the front-end
    HW_Flag_SpecialCodeGen = 0x20000,

    // No Read/Modify/Write Semantics
    // the intrinsic doesn't have read/modify/write semantics in two/three-operand form.
    HW_Flag_NoRMWSemantics = 0x40000,

    // Special import
    // the intrinsics need special rules in importer,
    // but may be table-driven in the back-end
    HW_Flag_SpecialImport = 0x80000,
};

inline HWIntrinsicFlag operator|(HWIntrinsicFlag c1, HWIntrinsicFlag c2)
{
    return static_cast<HWIntrinsicFlag>(static_cast<unsigned>(c1) | static_cast<unsigned>(c2));
}

enum HWIntrinsicCategory : unsigned int
{
    // Simple SIMD intrinsics
    // - take Vector128/256<T> parameters
    // - return a Vector128/256<T>
    // - the codegen of overloads can be determined by intrinsicID and base type of returned vector
    HW_Category_SimpleSIMD,

    // IsSupported Property
    // - each ISA class has an "IsSupported" property
    HW_Category_IsSupportedProperty,

    // IMM intrinsics
    // - some SIMD intrinsics requires immediate value (i.e. imm8) to generate instruction
    HW_Category_IMM,

    // Scalar intrinsics
    // - operate over general purpose registers, like crc32, lzcnt, popcnt, etc.
    HW_Category_Scalar,

    // SIMD scalar
    // - operate over vector registers(XMM), but just compute on the first element
    HW_Category_SIMDScalar,

    // Memory access intrinsics
    // - e.g., Avx.Load, Avx.Store, Sse.LoadAligned
    HW_Category_MemoryLoad,
    HW_Category_MemoryStore,

    // Helper intrinsics
    // - do not directly correspond to a instruction, such as Avx.SetAllVector256
    HW_Category_Helper,

    // Special intrinsics
    // - have to be addressed specially
    HW_Category_Special
};

#endif // FEATURE_HW_INTRINSICS && defined(_TARGET_XARCH_)

#endif // _NAMEDINTRINSICLIST_H_
