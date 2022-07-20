// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _NAMEDINTRINSICLIST_H_
#define _NAMEDINTRINSICLIST_H_

// Named jit intrinsics.

// When adding a new intrinsic that will use the GT_INTRINSIC node and can throw, make sure
// to update the "OperMayThrow" and "fgValueNumberAddExceptionSet" methods to account for that.

enum NamedIntrinsic : unsigned short
{
    NI_Illegal = 0,

    NI_System_Enum_HasFlag,

    NI_System_BitConverter_DoubleToInt64Bits,
    NI_System_BitConverter_Int32BitsToSingle,
    NI_System_BitConverter_Int64BitsToDouble,
    NI_System_BitConverter_SingleToInt32Bits,

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
    NI_System_Math_Max,
    NI_System_Math_Min,
    NI_System_Math_Pow,
    NI_System_Math_Round,
    NI_System_Math_Sin,
    NI_System_Math_Sinh,
    NI_System_Math_Sqrt,
    NI_System_Math_Tan,
    NI_System_Math_Tanh,
    NI_System_Math_Truncate,
    NI_SYSTEM_MATH_END,

    NI_System_Collections_Generic_Comparer_get_Default,
    NI_System_Collections_Generic_EqualityComparer_get_Default,
    NI_System_Buffers_Binary_BinaryPrimitives_ReverseEndianness,
    NI_System_Numerics_BitOperations_PopCount,
    NI_System_GC_KeepAlive,
    NI_System_Threading_Thread_get_CurrentThread,
    NI_System_Threading_Thread_get_ManagedThreadId,
    NI_System_Type_get_IsValueType,
    NI_System_Type_get_IsByRefLike,
    NI_System_Type_IsAssignableFrom,
    NI_System_Type_IsAssignableTo,
    NI_System_Type_op_Equality,
    NI_System_Type_op_Inequality,
    NI_System_Type_GetTypeFromHandle,
    NI_System_Array_Clone,
    NI_System_Array_GetLength,
    NI_System_Array_GetLowerBound,
    NI_System_Array_GetUpperBound,
    NI_System_Object_MemberwiseClone,
    NI_System_Object_GetType,
    NI_System_RuntimeTypeHandle_GetValueInternal,
    NI_System_StubHelpers_GetStubContext,
    NI_System_StubHelpers_NextCallReturnAddress,

    NI_Array_Address,
    NI_Array_Get,
    NI_Array_Set,

    NI_System_Activator_AllocatorOf,
    NI_System_Activator_DefaultConstructorOf,
    NI_System_EETypePtr_EETypePtrOf,

    NI_Internal_Runtime_MethodTable_Of,

    NI_System_Runtime_CompilerServices_RuntimeHelpers_CreateSpan,
    NI_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray,
    NI_System_Runtime_CompilerServices_RuntimeHelpers_IsKnownConstant,

    NI_System_String_Equals,
    NI_System_String_get_Chars,
    NI_System_String_get_Length,
    NI_System_String_op_Implicit,
    NI_System_String_StartsWith,
    NI_System_Span_get_Item,
    NI_System_ReadOnlySpan_get_Item,

    NI_System_MemoryExtensions_AsSpan,
    NI_System_MemoryExtensions_Equals,
    NI_System_MemoryExtensions_SequenceEqual,
    NI_System_MemoryExtensions_StartsWith,

    // These are used by HWIntrinsics but are defined more generally
    // to allow dead code optimization and handle the recursion case

    NI_IsSupported_True,
    NI_IsSupported_False,
    NI_IsSupported_Dynamic,
    NI_Throw_PlatformNotSupportedException,

    NI_System_Threading_Interlocked_And,
    NI_System_Threading_Interlocked_Or,
    NI_System_Threading_Interlocked_CompareExchange,
    NI_System_Threading_Interlocked_Exchange,
    NI_System_Threading_Interlocked_ExchangeAdd,
    NI_System_Threading_Interlocked_MemoryBarrier,
    NI_System_Threading_Interlocked_ReadMemoryBarrier,

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

    NI_SRCS_UNSAFE_START,

    NI_SRCS_UNSAFE_Add,
    NI_SRCS_UNSAFE_AddByteOffset,
    NI_SRCS_UNSAFE_AreSame,
    NI_SRCS_UNSAFE_As,
    NI_SRCS_UNSAFE_AsPointer,
    NI_SRCS_UNSAFE_AsRef,
    NI_SRCS_UNSAFE_ByteOffset,
    NI_SRCS_UNSAFE_Copy,
    NI_SRCS_UNSAFE_CopyBlock,
    NI_SRCS_UNSAFE_CopyBlockUnaligned,
    NI_SRCS_UNSAFE_InitBlock,
    NI_SRCS_UNSAFE_InitBlockUnaligned,
    NI_SRCS_UNSAFE_IsAddressGreaterThan,
    NI_SRCS_UNSAFE_IsAddressLessThan,
    NI_SRCS_UNSAFE_IsNullRef,
    NI_SRCS_UNSAFE_NullRef,
    NI_SRCS_UNSAFE_Read,
    NI_SRCS_UNSAFE_ReadUnaligned,
    NI_SRCS_UNSAFE_SizeOf,
    NI_SRCS_UNSAFE_SkipInit,
    NI_SRCS_UNSAFE_Subtract,
    NI_SRCS_UNSAFE_SubtractByteOffset,
    NI_SRCS_UNSAFE_Unbox,
    NI_SRCS_UNSAFE_Write,
    NI_SRCS_UNSAFE_WriteUnaligned,

    NI_SRCS_UNSAFE_END,
};

#endif // _NAMEDINTRINSICLIST_H_
