// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ================================================================================
// Standard primitive types for CLR code
//
// This header serves as a platform layer containing all of the primitive types
// which we use across CLR implementation code.
// ================================================================================


#ifndef CLRTYPES_H_
#define CLRTYPES_H_

#include <stdint.h>
#include "crtwrap.h"
#include "winwrap.h"
#include "staticcontract.h"

#if HOST_64BIT
    #define POINTER_BITS (64)
#else
    #define POINTER_BITS (32)
#endif

// UINTX_MINs aren't defined in standard header files,
// so definition must be separately predicated.
#ifndef UINT8_MIN
    #ifdef _MSC_VER
        #define UINT8_MIN       0ui8
        #define UINT16_MIN      0ui16
        #define UINT32_MIN      0ui32
        #define UINT64_MIN      0ui64
    #else
        #define UINT8_MIN       ((UINT8)0U)
        #define UINT16_MIN      ((UINT16)0U)
        #define UINT32_MIN      ((UINT32)0U)
        #define UINT64_MIN      ((UINT64)0ULL)
    #endif
#endif


// --------------------------------------------------------------------------------
// Use these types for pointer-sized integral types
// SIZE_T SSIZE_T
//
// These types are the ONLY types which can be safely cast back and forth from a
// pointer.
// --------------------------------------------------------------------------------

#ifndef SIZE_T_MAX
    #if NEED_POINTER_SIZED_TYPEDEFS
        typedef size_t                SIZE_T;
        typedef ptrdiff_t             SSIZE_T;
    #endif

    #if POINTER_BITS == 64
        #define SIZE_T_MAX              UINT64_MAX
        #define SIZE_T_MIN              UINT64_MIN

        #define SSIZE_T_MAX             INT64_MAX
        #define SSIZE_T_MIN             INT64_MIN
    #else
        #define SIZE_T_MAX              UINT32_MAX
        #define SIZE_T_MIN              UINT32_MIN

        #define SSIZE_T_MAX             INT32_MAX
        #define SSIZE_T_MIN             INT32_MIN
    #endif
#endif

// --------------------------------------------------------------------------------
// Non-pointer sized types
// COUNT_T SCOUNT_T
//
// Use these types for "large" counts or indexes which will not exceed 32 bits.  They
// may also be used for pointer differences, if you can guarantee that the pointers
// are pointing to the same region of memory. (It can NOT be used for arbitrary
// pointer subtraction.)
// --------------------------------------------------------------------------------

#ifndef COUNT_T_MAX
    typedef UINT32                  COUNT_T;
    typedef INT32                   SCOUNT_T;

    #define COUNT_T_MAX             UINT32_MAX
    #define COUNT_T_MIN             UINT32_MIN

    #define SCOUNT_T_MAX            INT32_MAX
    #define SCOUNT_T_MIN            INT32_MIN
#endif

// --------------------------------------------------------------------------------
// Integral types with additional semantic content
// BOOL BYTE
// --------------------------------------------------------------------------------

#ifndef BYTE_MAX
    #if NEED_BOOL_TYPEDEF
        typedef bool                    BOOL;
    #endif

    #define BOOL_MAX                1
    #define BOOL_MIN                0

    #define TRUE                    1
    #define FALSE                   0

    typedef UINT8                   BYTE;

    #define BYTE_MAX                UINT8_MAX
    #define BYTE_MIN                UINT8_MIN
#endif

// --------------------------------------------------------------------------------
// Character types
// CHAR SCHAR UCHAR WCHAR
// --------------------------------------------------------------------------------

typedef char                    CHAR;
typedef signed char             SCHAR;
typedef unsigned char           UCHAR;

typedef CHAR                    ASCII;
typedef CHAR                    ANSI;
typedef CHAR                    UTF8;

// Standard C defines:

// CHAR_MAX
// CHAR_MIN
// SCHAR_MAX
// SCHAR_MIN
// UCHAR_MAX
// UCHAR_MIN
// WCHAR_MAX
// WCHAR_MIN

#ifndef ASCII_MAX
    #define ASCII_MIN              ((ASCII)0)
    #define ASCII_MAX              ((ASCII)127)

    #define ANSI_MIN               ((ANSI)0)
    #define ANSI_MAX               ((ANSI)255)

    #define UTF8_MIN               ((UTF8)0)
    #define UTF8_MAX               ((UTF8)255)
#endif

// ================================================================================
// Non-integral types
// These types are in ALL_CAPS.
// ================================================================================

// --------------------------------------------------------------------------------
// Floating point types
// FLOAT DOUBLE
// --------------------------------------------------------------------------------

// ================================================================================
// Runtime type definitions - these are guaranteed to be identical with the
// corresponding managed type
// ================================================================================

typedef WCHAR       CLR_CHAR;
typedef INT8        CLR_I1;
typedef UINT8       CLR_U1;
typedef INT16       CLR_I2;
typedef UINT16      CLR_U2;
typedef INT32       CLR_I4;
typedef UINT32      CLR_U4;
typedef INT64       CLR_I8;
typedef UINT64      CLR_U8;
typedef FLOAT       CLR_R4;
typedef DOUBLE      CLR_R8;
typedef SSIZE_T     CLR_I;
typedef SIZE_T      CLR_U;

#define CLR_CHAR_MAX    WCHAR_MAX
#define CLR_CHAR_MIN    WCHAR_MIN

#define CLR_I1_MAX      INT8_MAX
#define CLR_I1_MIN      INT8_MIN

#define CLR_U1_MAX      UINT8_MAX
#define CLR_U1_MIN      UINT8_MIN

#define CLR_I2_MAX      INT16_MAX
#define CLR_I2_MIN      INT16_MIN

#define CLR_U2_MAX      UINT16_MAX
#define CLR_U2_MIN      UINT16_MIN

#define CLR_I4_MAX      INT32_MAX
#define CLR_I4_MIN      INT32_MIN

#define CLR_U4_MAX      UINT32_MAX
#define CLR_U4_MIN      UINT32_MIN

#define CLR_I8_MAX      INT64_MAX
#define CLR_I8_MIN      INT64_MIN

#define CLR_U8_MAX      UINT64_MAX
#define CLR_U8_MIN      UINT64_MIN

#define CLR_I_MAX       SSIZE_T_MAX
#define CLR_I_MIN       SSIZE_T_MIN

#define CLR_U_MAX       SIZE_T_MAX
#define CLR_U_MIN       SIZE_T_MIN

    typedef bool            CLR_BOOL;

static_assert(sizeof(CLR_BOOL) == 1);

#define CLR_BOOL_MAX    BOOL_MAX
#define CLR_BOOL_MIN    BOOL_MIN

#define CLR_NAN_32 0xFFC00000
#define CLR_NAN_64 0xFFF8000000000000LL

// ================================================================================
// Simple utility functions
// ================================================================================

// Note that these routines are in terms of UINT, ULONG, and ULONG64, since those are
// the unsigned integral types the compiler overloads based on.

// --------------------------------------------------------------------------------
// Min/Max
// --------------------------------------------------------------------------------

template <typename T>
T Min(T v1, T v2)
{
    STATIC_CONTRACT_LEAF;
    return v1 < v2 ? v1 : v2;
}

template <typename T>
T Max(T v1, T v2)
{
    STATIC_CONTRACT_LEAF;
    return v1 > v2 ? v1 : v2;
}

// --------------------------------------------------------------------------------
// Alignment bit twiddling macros - "alignment" must be power of 2
//
// AlignUp - align value to given increment, rounding up
// AlignmentPad - amount adjusted by AlignUp
//       AlignUp(value, x) == value + AlignmentPad(value, x)
//
// AlignDown - align value to given increment, rounding down
// AlignmentTrim - amount adjusted by AlignDown
//       AlignDown(value, x) == value - AlignmentTrim(value, x)
// --------------------------------------------------------------------------------

inline UINT AlignUp(UINT value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return (value+alignment-1)&~(alignment-1);
}

#if defined(_MSC_VER)
inline ULONG AlignUp(ULONG value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return (value+alignment-1)&~(alignment-1);
}
#endif

inline UINT64 AlignUp(UINT64 value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return (value+alignment-1)&~(UINT64)(alignment-1);
}

#if defined(__APPLE__) || defined(__wasm__)
inline SIZE_T AlignUp(SIZE_T value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return (value+alignment-1)&~(SIZE_T)(alignment-1);
}
#endif // __APPLE__

inline UINT AlignDown(UINT value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return (value&~(alignment-1));
}

#if defined(_MSC_VER)
inline ULONG AlignDown(ULONG value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return (value&~(ULONG)(alignment-1));
}
#endif

inline UINT64 AlignDown(UINT64 value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return (value&~(UINT64)(alignment-1));
}

#ifdef __wasm__
inline uintptr_t AlignDown(uintptr_t value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return (value&~(uintptr_t)(alignment-1));
}
#endif

#ifdef __APPLE__
inline SIZE_T AlignDown(SIZE_T value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return (value&~(SIZE_T)(alignment-1));
}
#endif // __APPLE__

inline UINT AlignmentPad(UINT value, UINT alignment)
{
    STATIC_CONTRACT_WRAPPER;
    return AlignUp(value, alignment) - value;
}

#if defined(_MSC_VER)
inline UINT AlignmentPad(ULONG value, UINT alignment)
{
    STATIC_CONTRACT_WRAPPER;
    return AlignUp(value, alignment) - value;
}
#endif

inline UINT AlignmentPad(UINT64 value, UINT alignment)
{
    STATIC_CONTRACT_WRAPPER;
    return (UINT) (AlignUp(value, alignment) - value);
}

#if defined(__APPLE__) || defined(__wasm__)
inline UINT AlignmentPad(SIZE_T value, UINT alignment)
{
    STATIC_CONTRACT_WRAPPER;
    return (UINT) (AlignUp(value, alignment) - value);
}
#endif // __APPLE__ || __wasm__

inline UINT AlignmentTrim(UINT value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return value&(alignment-1);
}

#ifndef HOST_UNIX
// For Unix this and the previous function get the same types.
// So, exclude this one.
inline UINT AlignmentTrim(ULONG value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return value&(alignment-1);
}
#endif // HOST_UNIX

inline UINT AlignmentTrim(UINT64 value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return ((UINT)value)&(alignment-1);
}

#if defined(__APPLE__) || defined(__wasm__)
inline UINT AlignmentTrim(SIZE_T value, UINT alignment)
{
    STATIC_CONTRACT_LEAF;
    STATIC_CONTRACT_SUPPORTS_DAC;
    return ((UINT)value)&(alignment-1);
}
#endif // __APPLE__

#endif  // CLRTYPES_H_
