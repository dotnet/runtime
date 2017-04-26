// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/******************************************************************
*                                                                 *
*  intsafe.h -- This module defines helper functions to prevent   *
*               integer overflow issues.                          *
*                                                                 *
*                                                                 *
******************************************************************/
#ifndef _INTSAFE_H_INCLUDED_
#define _INTSAFE_H_INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif

#include <specstrings.h>    // for IN, etc.

#define INTSAFE_E_ARITHMETIC_OVERFLOW       ((HRESULT)0x80070216L)  // 0x216 = 534 = ERROR_ARITHMETIC_OVERFLOW

#ifndef LOWORD
#define LOWORD(l)       ((WORD)(((DWORD_PTR)(l)) & 0xffff))
#endif

#ifndef HIWORD
#define HIWORD(l)       ((WORD)(((DWORD_PTR)(l)) >> 16))
#endif

#define HIDWORD(_qw)    ((ULONG)((_qw) >> 32))
#define LODWORD(_qw)    ((ULONG)(_qw))

#if defined(MIDL_PASS) || defined(RC_INVOKED) || defined(_M_CEE_PURE) \
    || defined(_M_AMD64) || defined(__ARM_ARCH)

#ifndef UInt32x32To64
#define UInt32x32To64(a, b) ((unsigned __int64)((ULONG)(a)) * (unsigned __int64)((ULONG)(b)))
#endif

#elif defined(_M_IX86)

#ifndef UInt32x32To64
#define UInt32x32To64(a, b) (unsigned __int64)((unsigned __int64)(ULONG)(a) * (ULONG)(b))
#endif

#else

#error Must define a target architecture.

#endif

#define INT_MAX         2147483647
#define LONG_MAX        2147483647L
#define USHRT_MAX       0xffff
#define UINT_MAX        0xffffffff
#define ULONG_MAX       0xffffffffUL
#define DWORD_MAX       0xffffffffUL

//
// It is common for -1 to be used as an error value for various types
//
#define USHORT_ERROR    (0xffff)
#define INT_ERROR       (-1)
#define LONG_ERROR      (-1L)
#define UINT_ERROR      (0xffffffff)
#define ULONG_ERROR     (0xffffffffUL)
#ifdef _MSC_VER
#define ULONGLONG_ERROR (0xffffffffffffffffui64)
#define HIDWORD_MASK (0xffffffff00000000ui64)
#else // _MSC_VER
#define ULONGLONG_ERROR (0xffffffffffffffffULL)
#define HIDWORD_MASK (0xffffffff00000000ULL)
#endif // _MSC_VER
#ifdef _WIN64
#define SIZET_ERROR     ULONGLONG_ERROR
#else
#define SIZET_ERROR     ULONG_ERROR
#endif

//
// We make some assumptions about the sizes of various types. Let's be
// explicit about those assumptions and check them.
//
C_ASSERT(sizeof(unsigned short) == 2);
C_ASSERT(sizeof(unsigned int) == 4);
C_ASSERT(sizeof(ULONG) == 4);

//
// INT -> signed char conversion
//
__inline
HRESULT
IntToSignedChar(
    IN INT iOperand,
    OUT signed char* pch)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pch = 0;

    if ((iOperand >= -128) && (iOperand <= 127))
    {
        *pch = (signed char)iOperand;
        hr = S_OK;
    }

    return hr;
}

//
// INT -> UCHAR conversion
//
__inline
HRESULT
IntToUChar(
    IN INT iOperand,
    OUT UCHAR* pch)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pch = 0;

    if ((iOperand >= 0) && (iOperand <= 255))
    {
        *pch = (UCHAR)iOperand;
        hr = S_OK;
    }

    return hr;
}

//
// LONG -> UCHAR conversion
//
__inline
HRESULT
LongToUChar(
    IN LONG lOperand,
    OUT UCHAR* pch)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pch = 0;

    if ((lOperand >= 0) && (lOperand <= 255))
    {
        *pch = (UCHAR)lOperand;
        hr = S_OK;
    }

    return hr;
}

//
// __inline is not sufficient. __forceinline is necessary.
// If the function is not inlined and you link .objs compiled with different compiler switches,
// you get one or the other function arbitrarily chosen.
//
// INT -> CHAR conversion
//
__forceinline
HRESULT
IntToChar(
    IN INT iOperand,
    OUT CHAR* pch)
{
#ifdef _CHAR_UNSIGNED
    return IntToUChar(iOperand, (UCHAR*)pch);
#else
    return IntToSignedChar(iOperand, (signed char*)pch);
#endif
}

//
// INT -> USHORT conversion
//
__inline
HRESULT
IntToUShort(
    IN INT iOperand,
    OUT USHORT* pusResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pusResult = USHORT_ERROR;

    if ((iOperand >= 0) && (iOperand <= USHRT_MAX))
    {
        *pusResult = (USHORT)iOperand;
        hr = S_OK;
    }

    return hr;
}

//
// INT -> UINT conversion
//
__inline
HRESULT
IntToUInt(
    IN INT iOperand,
    OUT UINT* puResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *puResult = UINT_ERROR;

    if (iOperand >= 0)
    {
        *puResult = (UINT)iOperand;
        hr = S_OK;
    }

    return hr;
}

//
// INT -> ULONG conversion
//
__inline
HRESULT
IntToULong(
    IN INT iOperand,
    OUT ULONG* pulResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pulResult = ULONG_ERROR;

    if (iOperand >= 0)
    {
        *pulResult = (ULONG)iOperand;
        hr = S_OK;
    }

    return hr;
}

//
// INT -> ULONGLONG conversion
//
__inline
HRESULT
IntToULongLong(
    IN INT iOperand,
    OUT ULONGLONG* pullResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pullResult = ULONG_ERROR;

    if (iOperand >= 0)
    {
        *pullResult = (ULONGLONG)iOperand;
        hr = S_OK;
    }

    return hr;
}

//
// UINT -> signed char conversion
//
__inline
HRESULT
UIntToSignedChar(
    IN UINT uOperand,
    OUT signed char* pch)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pch = 0;

    if (uOperand <= 127)
    {
        *pch = (signed char)uOperand;
        hr = S_OK;
    }

    return hr;
}

//
// UINT -> UCHAR conversion
//
__inline
HRESULT
UIntToUChar(
    IN UINT uOperand,
    OUT UCHAR* pch)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pch = 0;

    if (uOperand <= 255)
    {
        *pch = (UCHAR)uOperand;
        hr = S_OK;
    }

    return hr;
}

//
// UINT -> BYTE conversion
//
#define UIntToByte   UIntToUChar

//
// __inline is not sufficient. __forceinline is necessary.
// If the function is not inlined and you link .objs compiled with different compiler switches,
// you get one or the other function arbitrarily chosen.
//
// UINT -> CHAR conversion
//
__forceinline
HRESULT
UIntToChar(
    IN UINT uOperand,
    OUT CHAR* pch)
{
#ifdef _CHAR_UNSIGNED
    return UIntToUChar(uOperand, (UCHAR*)pch);
#else
    return UIntToSignedChar(uOperand, (signed char*)pch);
#endif // _CHAR_UNSIGNED
}

//
// UINT -> INT conversion
//
__inline
HRESULT
UIntToInt(
    IN UINT uOperand,
    OUT INT* piResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *piResult = INT_ERROR;

    if (uOperand <= INT_MAX)
    {
        *piResult = (INT)uOperand;
        hr = S_OK;
    }

    return hr;
}

//
// UINT -> LONG conversion
//
__inline
HRESULT
UIntToLong(
    IN UINT Operand,
    OUT LONG* Result)
{
    if (Operand <= LONG_MAX)
    {
        *Result = (LONG)Operand;
        return S_OK;
    }
    else
    {
        *Result = LONG_ERROR;
        return INTSAFE_E_ARITHMETIC_OVERFLOW;
    }
}

//
// UINT -> ULONG conversion
//
__inline
HRESULT
UIntToULong(
    IN UINT uOperand,
    OUT ULONG* pulResult)
{
    *pulResult = (ULONG)uOperand;
    
    return S_OK;
}

//
// ULONG -> UCHAR conversion
//
__inline
HRESULT
ULongToSignedChar(
    IN ULONG ulOperand,
    OUT signed char* pch)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pch = 0;

    if (ulOperand <= 127)
    {
        *pch = (signed char)ulOperand;
        hr = S_OK;
    }

    return hr;
}

//
// ULONG -> UCHAR conversion
//
__inline
HRESULT
ULongToUChar(
    IN ULONG ulOperand,
    OUT unsigned char* pch)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pch = 0;

    if (ulOperand <= 255)
    {
        *pch = (unsigned char)ulOperand;
        hr = S_OK;
    }

    return hr;
}

//
// __inline is not sufficient. __forceinline is necessary.
// If the function is not inlined and you link .objs compiled with different compiler switches,
// you get one or the other function arbitrarily chosen.
//
// ULONG -> CHAR conversion
//
__forceinline
HRESULT
ULongToChar(
    IN ULONG ulOperand,
    OUT CHAR* pch)
{
#ifdef _CHAR_UNSIGNED
    return ULongToUChar(ulOperand, (unsigned char*)pch);
#else
    return ULongToSignedChar(ulOperand, (signed char*)pch);
#endif // _CHAR_UNSIGNED
}

//
// ULONG -> USHORT conversion
//
__inline
HRESULT
ULongToUShort(
    IN ULONG ulOperand,
    OUT USHORT* pusResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pusResult = USHORT_ERROR;

    if (ulOperand <= USHRT_MAX)
    {
        *pusResult = (USHORT)ulOperand;
        hr = S_OK;
    }

    return hr;
}

//
// ULONG -> INT conversion
//
__inline
HRESULT
ULongToInt(
    IN ULONG ulOperand,
    OUT INT* piResult)
{
    if (ulOperand <= INT_MAX)
    {
        *piResult = (INT)ulOperand;
        return S_OK;
    }
    else
    {
        *piResult = INT_ERROR;
        return INTSAFE_E_ARITHMETIC_OVERFLOW;
    }
}

//
// ULONG -> UINT conversion
//
__inline
HRESULT
ULongToUInt(
    IN ULONG ulOperand,
    OUT UINT* puResult)
{
    *puResult = (UINT)ulOperand;
    
    return S_OK;
}

//
// ULONG -> LONG conversion
//
__inline
HRESULT
ULongToLong(
    IN ULONG Operand,
    OUT LONG* Result)
{
    if (Operand <= LONG_MAX)
    {
        *Result = (LONG)Operand;
        return S_OK;
    }
    else
    {
        *Result = LONG_ERROR;
        return INTSAFE_E_ARITHMETIC_OVERFLOW;
    }
}

//
// ULONGLONG -> INT conversion
//
__inline
HRESULT
ULongLongToInt(
    IN ULONGLONG ullOperand,
    OUT INT* piResult)
{
    if (ullOperand <= INT_MAX)
    {
        *piResult = (INT)ullOperand;
        return S_OK;
    }
    else
    {
        *piResult = INT_ERROR;
        return INTSAFE_E_ARITHMETIC_OVERFLOW;
    }
}

//
// ULONGLONG -> LONG conversion
//
__inline
HRESULT
ULongLongToLong(
    IN ULONGLONG Operand,
    OUT LONG* Result)
{
    if (Operand <= LONG_MAX)
    {
        *Result = (LONG)Operand;
        return S_OK;
    }
    else
    {
        *Result = LONG_ERROR;
        return INTSAFE_E_ARITHMETIC_OVERFLOW;
    }
}

//
// UINT -> USHORT conversion
//
__inline
HRESULT
UIntToUShort(
    IN UINT uOperand,
    OUT USHORT* pusResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pusResult = USHORT_ERROR;

    if (uOperand <= USHRT_MAX)
    {
        *pusResult = (USHORT)uOperand;
        hr = S_OK;
    }

    return hr;
}

//
// ULONGLONG -> USHORT conversion
//
__inline
HRESULT
ULongLongToUShort(
    IN ULONGLONG ullOperand,
    OUT USHORT* pusResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    USHORT usResult = USHORT_ERROR;

    if (ullOperand <= USHRT_MAX)
    {
        usResult = (USHORT)ullOperand;
        hr = S_OK;
    }
    *pusResult = usResult;

    return hr;
}

//
// ULONGLONG -> ULONG conversion
//
__inline
HRESULT
ULongLongToULong(
    IN ULONGLONG ullOperand,
    OUT ULONG* pulResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pulResult = ULONG_ERROR;
    
    if (ullOperand <= ULONG_MAX)
    {
        *pulResult = (ULONG)ullOperand;
        hr = S_OK;
    }
    
    return hr;
}

//
// UINT_PTR -> ULONG conversion
// ULONG_PTR -> ULONG conversion
//
#ifdef _WIN64

#define UIntPtrToULong  ULongLongToULong
#define ULongPtrToULong ULongLongToULong

#else

__inline
HRESULT
UIntPtrToULong(
    IN UINT_PTR Operand,
    OUT ULONG* pResult)
{
    *pResult = (ULONG)Operand;
    return S_OK;
}

__inline
HRESULT
ULongPtrToULong(
    IN ULONG_PTR Operand,
    OUT ULONG* pResult)
{
    *pResult = (ULONG)Operand;
    return S_OK;
}

#endif

//
// ULONGLONG -> UINT conversion
//
__inline
HRESULT
ULongLongToUInt(
    IN ULONGLONG ullOperand,
    OUT UINT* puResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *puResult = UINT_ERROR;
    
    if (ullOperand <= UINT_MAX)
    {
        *puResult = (UINT)ullOperand;
        hr = S_OK;
    }
    
    return hr;
}

//
// UINT_PTR -> UINT conversion
// ULONG_PTR -> UINT conversion
//
#ifdef _WIN64

#define UIntPtrToUInt  ULongLongToUInt
#define ULongPtrToUInt ULongLongToUInt

#else

__inline
HRESULT
UIntPtrToUInt(
    IN UINT_PTR Operand,
    OUT UINT* pResult)
{
    *pResult = (UINT)Operand;
    return S_OK;
}

__inline
HRESULT
ULongPtrToUInt(
    IN ULONG_PTR Operand,
    OUT UINT* pResult)
{
    *pResult = (UINT)Operand;
    return S_OK;
}

#endif

//
// * -> BYTE conversion (BYTE is always unsigned char)
//
#define  IntToByte   IntToUChar
#define UIntToByte  UIntToUChar
#define  LongToByte  LongToUChar
#define ULongToByte ULongToUChar

//
// * -> WORD conversion (WORD is always unsigned short)
//
#define IntToWord               IntToUShort
#define LongToWord              LongToUShort
#define LongLongToWord          LongLongToUShort
#define UIntToWord              UIntToUShort
#define ULongToWord             ULongToUShort
#define ULongLongToWord         ULongLongToUShort
#define UIntPtrToWord           UIntPtrToUShort
#define ULongPtrToWord          ULongPtrToUShort
#define SizeTToWord             SizeTToUShort
#define SIZETToWord             SIZETToUShort

//
// WORD -> * conversion (WORD is always unsigned short)
//
#define WordToUChar             UShortToUChar
#define WordToByte              UShortToByte
#define WordToChar              UShortToChar
#define WordToSignedChar        UShortToSignedChar
#define WordToInt               UShortToInt
#define WordToLong              UShortToLong
#define WordToLongLong          UShortToLongLong
#define WordToIntPtr            UShortToIntPtr
#define WordToLongPtr           UShortToLongPtr

//
// * -> DWORD conversion (DWORD is always ULONG)
//
#define CharToDWord             CharToULong
#define SignedCharToDWord       SignedCharToULong
#define ShortToDWord            ShortToULong
#define IntToDWord              IntToULong
#define LongToDWord             LongToULong
#define LongLongToDWord         LongLongToULong
#define UIntToDWord             UIntToULong
#define ULongLongToDWord        ULongLongToULong
#define IntPtrToDWord           IntPtrToULong
#define LongPtrToDWord          LongPtrToULong
#define UIntPtrToDWord          UIntPtrToULong
#define ULongPtrToDWord         ULongPtrToULong
#define SizeTToDWord            SizeTToULong
#define SIZETToDWord            SIZETToULong

//
// DWORD -> * conversion (DWORD is always ULONG)
//
#define DWordToChar             ULongToChar
#define DWordToUChar            ULongToUChar
#define DWordToByte             ULongToByte
#define DWordToSignedChar       ULongToSignedChar
#define DWordToUShort           ULongToUShort
#define DWordToUInt             ULongToUInt
#define DWordToInt              ULongToInt
#define DWordToLong             ULongToLong
#define DWordToLongLong         ULongToLongLong
#define DWordToIntPtr           ULongToIntPtr
#define DWordToLongPtr          ULongToLongPtr


//
// * -> UINT_PTR conversion (UINT_PTR is UINT on Win32, ULONGLONG on Win64)
//
#ifdef _WIN64
#define CharToUIntPtr           CharToULongLong
#define SignedCharToUIntPtr     SignedCharToULongLong
#define ShortToUIntPtr          ShortToULongLong
#define IntToUIntPtr            IntToULongLong
#define LongToUIntPtr           LongToULongLong
#define LongLongToUIntPtr       LongLongToULongLong
#define IntPtrToUIntPtr         IntPtrToULongLong
#define LongPtrToUIntPtr        LongPtrToULongLong
#else
#define CharToUIntPtr           CharToUInt
#define SignedCharToUIntPtr     SignedCharToUInt
#define ShortToUIntPtr          ShortToUInt

__inline
HRESULT
IntToUIntPtr(
    IN INT iOperand,
    OUT UINT_PTR* puResult)
{
	return IntToUInt(iOperand, (UINT*)puResult);
}

#define LongToUIntPtr           LongToUInt
#define LongLongToUIntPtr       LongLongToUInt

#define IntPtrToUIntPtr         IntPtrToUInt
#define LongPtrToUIntPtr        LongPtrToUInt
#endif

__inline
HRESULT
ULongLongToUIntPtr(
    IN ULONGLONG ullOperand,
    OUT UINT_PTR* puResult)
{
#ifdef _WIN64
	*puResult = ullOperand;
	return S_OK;
#else
	return ULongLongToUInt(ullOperand, (UINT*)puResult);
#endif
}


//
// UINT_PTR -> * conversion (UINT_PTR is UINT on Win32, ULONGLONG on Win64)
//
#ifdef _WIN64
#define UIntPtrToUShort         ULongLongToUShort
#define UIntPtrToInt            ULongLongToInt
#define UIntPtrToLong           ULongLongToLong
#define UIntPtrToLongLong       ULongLongToLongLong
#define UIntPtrToIntPtr         ULongLongToIntPtr
#define UIntPtrToLongPtr        ULongLongToLongPtr
#else

__inline
HRESULT
UIntPtrToUShort(
    IN UINT_PTR uOperand,
    OUT USHORT* pusResult)
{
	return UIntToUShort((UINT)uOperand, pusResult);
}

__inline
HRESULT
UIntPtrToInt(
    IN UINT_PTR uOperand,
    OUT INT* piResult)
{
	return UIntToInt((UINT)uOperand, piResult);
}

__inline
HRESULT
UIntPtrToLong(
    IN UINT_PTR Operand,
    OUT LONG* Result)
{
	return UIntToLong((UINT)Operand, Result);
}

#define UIntPtrToLongLong       UIntToLongLong
#define UIntPtrToIntPtr         UIntToIntPtr
#define UIntPtrToLongPtr        UIntToLongPtr
#endif


//
// * -> ULONG_PTR conversion (ULONG_PTR is ULONG on Win32, ULONGLONG on Win64)
//
#ifdef _WIN64
#define CharToULongPtr          CharToULongLong
#define SignedCharToULongPtr    SignedCharToULongLong
#define ShortToULongPtr         ShortToULongLong
#define IntToULongPtr           IntToULongLong
#define LongToULongPtr          LongToULongLong
#define LongLongToULongPtr      LongLongToULongLong
#define IntPtrToULongPtr        IntPtrToULongLong
#define LongPtrToULongPtr       LongPtrToULongLong
#else
#define CharToULongPtr          CharToULong
#define SignedCharToULongPtr    SignedCharToULong
#define ShortToULongPtr         ShortToULong

__inline
HRESULT
IntToULongPtr(
    IN INT iOperand,
    OUT ULONG_PTR* pulResult)
{
	return IntToULong(iOperand, (ULONG*)pulResult);
}

#define LongToULongPtr          LongToULong
#define LongLongToULongPtr      LongLongToULong

#define IntPtrToULongPtr        IntPtrToULong
#define LongPtrToULongPtr       LongPtrToULong
#endif

__inline
HRESULT
ULongLongToULongPtr(
    IN ULONGLONG ullOperand,
    OUT ULONG_PTR* pulResult)
{
#ifdef _WIN64
	*pulResult = ullOperand;
	return S_OK;
#else
	return ULongLongToULong(ullOperand, (ULONG*)pulResult);
#endif
}


//
// ULONG_PTR -> * conversion (ULONG_PTR is ULONG on Win32, ULONGLONG on Win64)
//
#ifdef _WIN64
#define ULongPtrToUShort        ULongLongToUShort
#define ULongPtrToInt           ULongLongToInt
#define ULongPtrToLong          ULongLongToLong
#define ULongPtrToLongLong      ULongLongToLongLong
#define ULongPtrToIntPtr        ULongLongToIntPtr
#define ULongPtrToLongPtr       ULongLongToLongPtr
#else

__inline
HRESULT
ULongPtrToUShort(
    IN ULONG_PTR ulOperand,
    OUT USHORT* pusResult)
{
	return ULongToUShort((ULONG)ulOperand, pusResult);
}

__inline
HRESULT
ULongPtrToInt(
    IN ULONG_PTR ulOperand,
    OUT INT* piResult)
{
	return ULongToInt((ULONG)ulOperand, piResult);
}

__inline
HRESULT
ULongPtrToLong(
    IN ULONG_PTR Operand,
    OUT LONG* Result)
{
	return ULongToLong((ULONG)Operand, Result);
}

#define ULongPtrToLongLong      ULongToLongLong
#define ULongPtrToIntPtr        ULongToIntPtr
#define ULongPtrToLongPtr       ULongToLongPtr
#endif

//
// * -> size_t conversion (size_t is always UINT_PTR)
//
#define CharToSizeT             CharToUIntPtr
#define SignedCharToSizeT       SignedCharToUIntPtr
#define ShortToSizeT            ShortToUIntPtr
#define IntToSizeT              IntToUIntPtr
#define LongToSizeT             LongToUIntPtr
#define LongLongToSizeT         LongLongToUIntPtr
#define ULongLongToSizeT        ULongLongToUIntPtr
#define IntPtrToSizeT           IntPtrToUIntPtr
#define LongPtrToSizeT          LongPtrToUIntPtr

//
// size_t -> * conversion (size_t is always UINT_PTR)
//
#define SizeTToUShort           UIntPtrToUShort
#define SizeTToUInt             UIntPtrToUInt
#define SizeTToULong            UIntPtrToULong
#define SizeTToInt              UIntPtrToInt
#define SizeTToLong             UIntPtrToLong
#define SizeTToLongLong         UIntPtrToLongLong
#define SizeTToIntPtr           UIntPtrToIntPtr
#define SizeTToLongPtr          UIntPtrToLongPtr

//
// * -> SIZE_T conversion (SIZE_T is always ULONG_PTR)
//
#define CharToSIZET             CharToULongPtr
#define SignedCharToSIZET       SignedCharToULongPtr
#define ShortToSIZET            ShortToULongPtr
#define IntToSIZET              IntToULongPtr
#define LongToSIZET             LongToULongPtr
#define LongLongToSIZET         LongLongToULongPtr
#define IntPtrToSIZET           IntPtrToULongPtr
#define LongPtrToSIZET          LongPtrToULongPtr
#define ULongLongToSIZET        ULongLongToULongPtr

//
// SIZE_T -> * conversion (SIZE_T is always ULONG_PTR)
//
#define SIZETToUShort           ULongPtrToUShort
#define SIZETToUInt             ULongPtrToUInt
#define SIZETToULong            ULongPtrToULong
#define SIZETToUIntPtr          ULongPtrToUIntPtr
#define SIZETToULongPtr         ULongPtrToULongPtr
#define SIZETToInt              ULongPtrToInt
#define SIZETToLong             ULongPtrToLong
#define SIZETToLongLong         ULongPtrToLongLong
#define SIZETToIntPtr           ULongPtrToIntPtr
#define SIZETToLongPtr          ULongPtrToLongPtr

//
// * -> DWORD_PTR conversion (DWORD_PTR is always ULONG_PTR)
//
#define CharToDWordPtr             CharToULongPtr
#define SignedCharToDWordPtr       SignedCharToULongPtr
#define ShortToDWordPtr            ShortToULongPtr
#define IntToDWordPtr              IntToULongPtr
#define LongToDWordPtr             LongToULongPtr
#define LongLongToDWordPtr         LongLongToULongPtr
#define ULongLongToDWordPtr        ULongLongToULongPtr
#define IntPtrToDWordPtr           IntPtrToULongPtr
#define LongPtrToDWordPtr          LongPtrToULongPtr

//
// DWORD_PTR -> * conversion (DWORD_PTR is always ULONG_PTR)
//
#define DWordPtrToUShort           ULongPtrToUShort
#define DWordPtrToUInt             ULongPtrToUInt
#define DWordPtrToULong            ULongPtrToULong
#define DWordPtrToDWord            ULongPtrToDWord
#define DWordPtrToInt              ULongPtrToInt
#define DWordPtrToLong             ULongPtrToLong
#define DWordPtrToLongLong         ULongPtrToLongLong
#define DWordPtrToIntPtr           ULongPtrToIntPtr
#define DWordPtrToLongPtr          ULongPtrToLongPtr

//
// USHORT addition
//
__inline
HRESULT
UShortAdd(
    IN USHORT usAugend,
    IN USHORT usAddend,
    OUT USHORT* pusResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pusResult = USHORT_ERROR;

    if (((USHORT)(usAugend + usAddend)) >= usAugend)
    {
        *pusResult = (usAugend + usAddend);
        hr = S_OK;
    }
    
    return hr;
}

//
// WORD addtition
//
#define WordAdd     UShortAdd

//
// UINT addition
//
__inline
HRESULT
UIntAdd(
    IN UINT uAugend,
    IN UINT uAddend,
    OUT UINT* puResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *puResult = UINT_ERROR;

    if ((uAugend + uAddend) >= uAugend)
    {
        *puResult = (uAugend + uAddend);
        hr = S_OK;
    }
    
    return hr;
}

//
// UINT_PTR addition
//
#define UIntPtrAdd		SizeTAdd

//
// ULONG addition
//
__inline
HRESULT
ULongAdd(
    IN ULONG ulAugend,
    IN ULONG ulAddend,
    OUT ULONG* pulResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pulResult = ULONG_ERROR;

    if ((ulAugend + ulAddend) >= ulAugend)
    {
        *pulResult = (ulAugend + ulAddend);
        hr = S_OK;
    }
    
    return hr;
}

//
// ULONG_PTR addition
//
#ifdef _WIN64
#define ULongPtrAdd     ULongLongAdd
#else
__inline
HRESULT
ULongPtrAdd(
    IN ULONG_PTR ulAugend,
    IN ULONG_PTR ulAddend,
    OUT ULONG_PTR* pulResult)
{
	return ULongAdd((ULONG)ulAugend, (ULONG)ulAddend, (ULONG*)pulResult);
}
#endif // _WIN64

//
// DWORD addition
//
#define DWordAdd        ULongAdd

//
// DWORD_PTR addition
//
#define DWordPtrAdd		ULongPtrAdd

//
// size_t addition
//
__inline
HRESULT
SizeTAdd(
    IN size_t Augend,
    IN size_t Addend,
    OUT size_t* pResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pResult = SIZET_ERROR;

    if ((Augend + Addend) >= Augend)
    {
        *pResult = (Augend + Addend);
        hr = S_OK;
    }
    
    return hr;
}

//
// SIZE_T addition
//
#define SIZETAdd	ULongPtrAdd

//
// ULONGLONG addition
//
__inline
HRESULT
ULongLongAdd(
    IN ULONGLONG ullAugend,
    IN ULONGLONG ullAddend,
    OUT ULONGLONG* pullResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pullResult = ULONGLONG_ERROR;

    if ((ullAugend + ullAddend) >= ullAugend)
    {
        *pullResult = (ullAugend + ullAddend);
        hr = S_OK;
    }
    
    return hr;
}

//
// USHORT subtraction
//
__inline
HRESULT
UShortSub(
    IN USHORT usMinuend,
    IN USHORT usSubtrahend,
    OUT USHORT* pusResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pusResult = USHORT_ERROR;

    if (usMinuend >= usSubtrahend)
    {
        *pusResult = (usMinuend - usSubtrahend);
        hr = S_OK;
    }
    
    return hr;
}

//
// WORD subtraction
//
#define WordSub     UShortSub


//
// UINT subtraction
//
__inline
HRESULT
UIntSub(
    IN UINT uMinuend,
    IN UINT uSubtrahend,
    OUT UINT* puResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *puResult = UINT_ERROR;

    if (uMinuend >= uSubtrahend)
    {
        *puResult = (uMinuend - uSubtrahend);
        hr = S_OK;
    }
    
    return hr;
}

//
// UINT_PTR subtraction
//
#define UIntPtrSub	SizeTSub

//
// ULONG subtraction
//
__inline
HRESULT
ULongSub(
    IN ULONG ulMinuend,
    IN ULONG ulSubtrahend,
    OUT ULONG* pulResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pulResult = ULONG_ERROR;

    if (ulMinuend >= ulSubtrahend)
    {
        *pulResult = (ulMinuend - ulSubtrahend);
        hr = S_OK;
    }
    
    return hr;
}

//
// ULONG_PTR subtraction
//
#ifdef _WIN64
#define ULongPtrSub ULongLongSub
#else
__inline
HRESULT
ULongPtrSub(
    IN ULONG_PTR ulMinuend,
    IN ULONG_PTR ulSubtrahend,
    OUT ULONG_PTR* pulResult)
{
	return ULongSub((ULONG)ulMinuend, (ULONG)ulSubtrahend, (ULONG*)pulResult);
}
#endif // _WIN64


//
// DWORD subtraction
//
#define DWordSub        ULongSub

//
// DWORD_PTR subtraction
//
#define DWordPtrSub		ULongPtrSub

//
// size_t subtraction
//
__inline
HRESULT
SizeTSub(
    IN size_t Minuend,
    IN size_t Subtrahend,
    OUT size_t* pResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pResult = SIZET_ERROR;

    if (Minuend >= Subtrahend)
    {
        *pResult = (Minuend - Subtrahend);
        hr = S_OK;
    }
    
    return hr;
}

//
// SIZE_T subtraction
//
#define SIZETSub	ULongPtrSub

//
// ULONGLONG subtraction
//
__inline
HRESULT
ULongLongSub(
    IN ULONGLONG ullMinuend,
    IN ULONGLONG ullSubtrahend,
    OUT ULONGLONG* pullResult)
{
    HRESULT hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    *pullResult = ULONGLONG_ERROR;

    if (ullMinuend >= ullSubtrahend)
    {
        *pullResult = (ullMinuend - ullSubtrahend);
        hr = S_OK;
    }
    
    return hr;
}

//
// USHORT multiplication
//
__inline
HRESULT
UShortMult(
    IN USHORT usMultiplicand,
    IN USHORT usMultiplier,
    OUT USHORT* pusResult)
{
    ULONG ulResult = ((ULONG)usMultiplicand) * (ULONG)usMultiplier;
    
    return ULongToUShort(ulResult, pusResult);
}

//
// WORD multiplication
//
#define WordMult      UShortMult

//
// UINT multiplication
//
__inline
HRESULT
UIntMult(
    IN UINT uMultiplicand,
    IN UINT uMultiplier,
    OUT UINT* puResult)
{
    ULONGLONG ull64Result = UInt32x32To64(uMultiplicand, uMultiplier);

    return ULongLongToUInt(ull64Result, puResult);
}

//
// ULONG multiplication
//
__inline
HRESULT
ULongMult(
    IN ULONG ulMultiplicand,
    IN ULONG ulMultiplier,
    OUT ULONG* pulResult)
{
    ULONGLONG ull64Result = UInt32x32To64(ulMultiplicand, ulMultiplier);
    
    return ULongLongToULong(ull64Result, pulResult);
}

//
// DWORD multiplication
//
#define DWordMult       ULongMult

//
// DWORD_PTR multiplication
//
#define DWordPtrMult	ULongPtrMult

#endif // _INTSAFE_H_INCLUDED_
