// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++





--*/

/******************************************************************
*                                                                 *
*  strsafe.h -- This module defines safer C library string        *
*               routine replacements. These are meant to make C   *
*               a bit more safe in reference to security and      *
*               robustness                                        *
*                                                                 *
******************************************************************/
#ifndef _STRSAFE_H_INCLUDED_
#define _STRSAFE_H_INCLUDED_
#ifdef _MSC_VER
#pragma once
#endif

#include <stdio.h>      // for _vsnprintf, getc, getwc
#include <string.h>     // for memset
#include <stdarg.h>     // for va_start, etc.

#ifndef _SIZE_T_DEFINED
#ifdef  HOST_64BIT
typedef unsigned __int64    size_t;
#else
typedef __w64 unsigned int  size_t;
#endif  // !HOST_64BIT
#define _SIZE_T_DEFINED
#endif  // !_SIZE_T_DEFINED

#ifndef SUCCEEDED
#define SUCCEEDED(hr)  ((HRESULT)(hr) >= 0)
#endif

#ifndef FAILED
#define FAILED(hr)  ((HRESULT)(hr) < 0)
#endif

#ifndef S_OK
#define S_OK  ((HRESULT)0x00000000L)
#endif

#ifdef __cplusplus
#define _STRSAFE_EXTERN_C    extern "C"
#else
#define _STRSAFE_EXTERN_C    extern
#endif

// If you do not want to use these functions inline (and instead want to link w/ strsafe.lib), then
// #define STRSAFE_LIB before including this header file.
#if defined(STRSAFE_LIB)
#define STRSAFEAPI  _STRSAFE_EXTERN_C HRESULT __stdcall
#pragma comment(lib, "strsafe.lib")
#elif defined(STRSAFE_LIB_IMPL)
#define STRSAFEAPI  _STRSAFE_EXTERN_C HRESULT __stdcall
#else
#define STRSAFEAPI  __inline HRESULT __stdcall
#define STRSAFE_INLINE
#endif

// Some functions always run inline because they use stdin and we want to avoid building multiple
// versions of strsafe lib depending on if you use msvcrt, libcmt, etc.
#define STRSAFE_INLINE_API  __inline HRESULT __stdcall

// The user can request no "Cb" or no "Cch" fuctions, but not both!
#if defined(STRSAFE_NO_CB_FUNCTIONS) && defined(STRSAFE_NO_CCH_FUNCTIONS)
#error cannot specify both STRSAFE_NO_CB_FUNCTIONS and STRSAFE_NO_CCH_FUNCTIONS !!
#endif

// This should only be defined when we are building strsafe.lib
#ifdef STRSAFE_LIB_IMPL
#define STRSAFE_INLINE
#endif


#define STRSAFE_MAX_CCH  2147483647 // max # of characters we support (same as INT_MAX)

// STRSAFE error return codes
//
#define STRSAFE_E_INSUFFICIENT_BUFFER       ((HRESULT)0x8007007AL)  // 0x7A = 122L = ERROR_INSUFFICIENT_BUFFER
#define STRSAFE_E_INVALID_PARAMETER         ((HRESULT)0x80070057L)  // 0x57 =  87L = ERROR_INVALID_PARAMETER
#define STRSAFE_E_END_OF_FILE               ((HRESULT)0x80070026L)  // 0x26 =  38L = ERROR_HANDLE_EOF

// Flags for controling the Ex functions
//
//      STRSAFE_FILL_BYTE(0xFF)     0x000000FF  // bottom byte specifies fill pattern
#define STRSAFE_IGNORE_NULLS        0x00000100  // treat null as TEXT("") -- don't fault on NULL buffers
#define STRSAFE_FILL_BEHIND_NULL    0x00000200  // fill in extra space behind the null terminator
#define STRSAFE_FILL_ON_FAILURE     0x00000400  // on failure, overwrite pszDest with fill pattern and null terminate it
#define STRSAFE_NULL_ON_FAILURE     0x00000800  // on failure, set *pszDest = TEXT('\0')
#define STRSAFE_NO_TRUNCATION       0x00001000  // instead of returning a truncated result, copy/append nothing to pszDest and null terminate it

#define STRSAFE_VALID_FLAGS         (0x000000FF | STRSAFE_IGNORE_NULLS | STRSAFE_FILL_BEHIND_NULL | STRSAFE_FILL_ON_FAILURE | STRSAFE_NULL_ON_FAILURE | STRSAFE_NO_TRUNCATION)

// helper macro to set the fill character and specify buffer filling
#define STRSAFE_FILL_BYTE(x)        ((unsigned long)((x & 0x000000FF) | STRSAFE_FILL_BEHIND_NULL))
#define STRSAFE_FAILURE_BYTE(x)     ((unsigned long)((x & 0x000000FF) | STRSAFE_FILL_ON_FAILURE))

#define STRSAFE_GET_FILL_PATTERN(dwFlags)  ((int)(dwFlags & 0x000000FF))

// prototypes for the worker functions
#ifdef STRSAFE_INLINE
STRSAFEAPI StringCopyWorkerA(char* pszDest, size_t cchDest, const char* pszSrc);
STRSAFEAPI StringCopyWorkerW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc);
#endif  // STRSAFE_INLINE

#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchCopy(LPTSTR pszDest,
                     size_t cchDest,
                     LPCTSTR pszSrc);

Routine Description:

    This routine is a safer version of the C built-in function 'strcpy'.
    The size of the destination buffer (in characters) is a parameter and
    this function will not write past the end of this buffer and it will
    ALWAYS null terminate the destination buffer (unless it is zero length).

    This routine is not a replacement for strncpy.  That function will pad the
    destination string with extra null termination characters if the count is
    greater than the length of the source string, and it will fail to null
    terminate the destination string if the source string length is greater
    than or equal to the count. You can not blindly use this instead of strncpy:
    it is common for code to use it to "patch" strings and you would introduce
    errors if the code started null terminating in the middle of the string.

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if the string was copied without truncation and null terminated, otherwise
    it will return a failure code. In failure cases as much of pszSrc will be
    copied to pszDest as possible, and pszDest will be null terminated.

Arguments:

    pszDest     -   destination string

    cchDest     -   size of destination buffer in characters.
                    length must be = (_tcslen(src) + 1) to hold all of the
                    source including the null terminator

    pszSrc      -   source string which must be null terminated

Notes:
    Behavior is undefined if source and destination strings overlap.

    pszDest and pszSrc should not be NULL. See StringCchCopyEx if you require
    the handling of NULL values.

Return Value:

    S_OK        -   if there was source data and it was all copied and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all hresult failure cases

    STRSAFE_E_INSUFFICIENT_BUFFER /
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the copy operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function.

--*/

STRSAFEAPI StringCchCopyA(char* pszDest, size_t cchDest, const char* pszSrc);
STRSAFEAPI StringCchCopyW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc);
#ifdef UNICODE
#define StringCchCopy  StringCchCopyW
#else
#define StringCchCopy  StringCchCopyA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchCopyA(char* pszDest, size_t cchDest, const char* pszSrc)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCopyWorkerA(pszDest, cchDest, pszSrc);
    }

    return hr;
}

STRSAFEAPI StringCchCopyW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCopyWorkerW(pszDest, cchDest, pszSrc);
    }

    return hr;
}
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS

// these are the worker functions that actually do the work
#ifdef STRSAFE_INLINE
STRSAFEAPI StringCopyWorkerA(char* pszDest, size_t cchDest, const char* pszSrc)
{
    HRESULT hr = S_OK;

    if (cchDest == 0)
    {
        // can not null terminate a zero-byte dest buffer
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        while (cchDest && (*pszSrc != '\0'))
        {
            *pszDest++ = *pszSrc++;
            cchDest--;
        }

        if (cchDest == 0)
        {
            // we are going to truncate pszDest
            pszDest--;
            hr = STRSAFE_E_INSUFFICIENT_BUFFER;
        }

        *pszDest= '\0';
    }

    return hr;
}

STRSAFEAPI StringCopyWorkerW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc)
{
    HRESULT hr = S_OK;

    if (cchDest == 0)
    {
        // can not null terminate a zero-byte dest buffer
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        while (cchDest && (*pszSrc != L'\0'))
        {
            *pszDest++ = *pszSrc++;
            cchDest--;
        }

        if (cchDest == 0)
        {
            // we are going to truncate pszDest
            pszDest--;
            hr = STRSAFE_E_INSUFFICIENT_BUFFER;
        }

        *pszDest= L'\0';
    }

    return hr;
}
#endif  // STRSAFE_INLINE

#endif  // _STRSAFE_H_INCLUDED_
