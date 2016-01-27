// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#if defined(PLATFORM_UNIX) && !defined (FEATURE_PAL)
#define _NATIVE_WCHAR_T_DEFINED
#endif // defined(PLATFORM_UNIX) && !defined (FEATURE_PAL)

#if defined(PLATFORM_UNIX) && !defined (FEATURE_PAL)
#define _vsnprintf vsnprintf
#endif // defined(PLATFORM_UNIX) && !defined (FEATURE_PAL)

#include <stdio.h>      // for _vsnprintf, _vsnwprintf, getc, getwc
#include <string.h>     // for memset
#include <stdarg.h>     // for va_start, etc.

#ifndef _SIZE_T_DEFINED
#ifdef  _WIN64
typedef unsigned __int64    size_t;
#else
typedef __w64 unsigned int  size_t;
#endif  // !_WIN64
#define _SIZE_T_DEFINED
#endif  // !_SIZE_T_DEFINED

#if !defined(_WCHAR_T_DEFINED) && !defined(_NATIVE_WCHAR_T_DEFINED)
#error Unexpected define.
typedef char16_t WCHAR;
#define _WCHAR_T_DEFINED
#endif

#ifndef FEATURE_PAL
#ifndef _HRESULT_DEFINED
#define _HRESULT_DEFINED
typedef LONG HRESULT;
#endif // !_HRESULT_DEFINED
#endif // !FEATURE_PAL

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
STRSAFEAPI StringCopyExWorkerA(char* pszDest, size_t cchDest, size_t cbDest, const char* pszSrc, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFEAPI StringCopyExWorkerW(WCHAR* pszDest, size_t cchDest, size_t cbDest, const WCHAR* pszSrc, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFEAPI StringCopyNWorkerA(char* pszDest, size_t cchDest, const char* pszSrc, size_t cchSrc);
STRSAFEAPI StringCopyNWorkerW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, size_t cchSrc);
STRSAFEAPI StringCopyNExWorkerA(char* pszDest, size_t cchDest, size_t cbDest, const char* pszSrc, size_t cchSrc, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFEAPI StringCopyNExWorkerW(WCHAR* pszDest, size_t cchDest, size_t cbDest, const WCHAR* pszSrc, size_t cchSrc, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFEAPI StringCatWorkerA(char* pszDest, size_t cchDest, const char* pszSrc);
STRSAFEAPI StringCatWorkerW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc);
STRSAFEAPI StringCatExWorkerA(char* pszDest, size_t cchDest, size_t cbDest, const char* pszSrc, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFEAPI StringCatExWorkerW(WCHAR* pszDest, size_t cchDest, size_t cbDest, const WCHAR* pszSrc, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFEAPI StringCatNWorkerA(char* pszDest, size_t cchDest, const char* pszSrc, size_t cchMaxAppend);
STRSAFEAPI StringCatNWorkerW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, size_t cchMaxAppend);
STRSAFEAPI StringCatNExWorkerA(char* pszDest, size_t cchDest, size_t cbDest, const char* pszSrc, size_t cchMaxAppend, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFEAPI StringCatNExWorkerW(WCHAR* pszDest, size_t cchDest, size_t cbDest, const WCHAR* pszSrc, size_t cchMaxAppend, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFEAPI StringVPrintfWorkerA(char* pszDest, size_t cchDest, const char* pszFormat, va_list argList);
STRSAFEAPI StringVPrintfWorkerW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszFormat, va_list argList);
STRSAFEAPI StringVPrintfExWorkerA(char* pszDest, size_t cchDest, size_t cbDest, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags, const char* pszFormat, va_list argList);
STRSAFEAPI StringVPrintfExWorkerW(WCHAR* pszDest, size_t cchDest, size_t cbDest, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags, const WCHAR* pszFormat, va_list argList);
STRSAFEAPI StringLengthWorkerA(const char* psz, size_t cchMax, size_t* pcch);
STRSAFEAPI StringLengthWorkerW(const WCHAR* psz, size_t cchMax, size_t* pcch);
#endif  // STRSAFE_INLINE

#ifndef STRSAFE_LIB_IMPL
#ifndef FEATURE_PAL
// these functions are always inline
STRSAFE_INLINE_API StringGetsExWorkerA(char* pszDest, size_t cchDest, size_t cbDest, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFE_INLINE_API StringGetsExWorkerW(WCHAR* pszDest, size_t cchDest, size_t cbDest, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
#endif // !FEATURE_PAL
#endif

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
                    code for all hresult falure cases

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

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
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
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbCopy(LPTSTR pszDest,
                    size_t cbDest,
                    LPCTSTR pszSrc);

Routine Description:

    This routine is a safer version of the C built-in function 'strcpy'.
    The size of the destination buffer (in bytes) is a parameter and this
    function will not write past the end of this buffer and it will ALWAYS
    null terminate the destination buffer (unless it is zero length).

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

    cbDest      -   size of destination buffer in bytes.
                    length must be = ((_tcslen(src) + 1) * sizeof(TCHAR)) to
                    hold all of the source including the null terminator

    pszSrc      -   source string which must be null terminated

Notes: 
    Behavior is undefined if source and destination strings overlap.
   
    pszDest and pszSrc should not be NULL.  See StringCbCopyEx if you require 
    the handling of NULL values.

Return Value:

    S_OK        -   if there was source data and it was all copied and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all hresult falure cases

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

STRSAFEAPI StringCbCopyA(char* pszDest, size_t cbDest, const char* pszSrc);
STRSAFEAPI StringCbCopyW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc);
#ifdef UNICODE
#define StringCbCopy  StringCbCopyW
#else
#define StringCbCopy  StringCbCopyA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbCopyA(char* pszDest, size_t cbDest, const char* pszSrc)
{
    HRESULT hr;
    size_t cchDest;
    
    // convert to count of characters
    cchDest = cbDest / sizeof(char);

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

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbCopyW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc)
{
    HRESULT hr;
    size_t cchDest;
    
    // convert to count of characters
    cchDest = cbDest / sizeof(WCHAR);

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
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchCopyEx(TCHAR pszDest,
                       size_t cchDest,
                       LPCTSTR pszSrc,
                       LPTSTR* ppszDestEnd,
                       size_t* pcchRemaining,
                       DWORD dwFlags);

Routine Description:

    This routine is a safer version of the C built-in function 'strcpy' with
    some additional parameters.  In addition to functionality provided by
    StringCchCopy, this routine also returns a pointer to the end of the
    destination string and the number of characters left in the destination string
    including the null terminator. The flags parameter allows additional controls.

Arguments:

    pszDest         -   destination string

    cchDest         -   size of destination buffer in characters.
                        length must be = (_tcslen(pszSrc) + 1) to hold all of
                        the source including the null terminator

    pszSrc          -   source string which must be null terminated

    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a
                        pointer to the end of the destination string.  If the
                        function copied any data, the result will point to the
                        null termination character

    pcchRemaining   -   if pcchRemaining is non-null, the function will return the
                        number of characters left in the destination string,
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT("")).
                    this flag is useful for emulating functions like lstrcpy

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated. This will overwrite any truncated 
                    string returned when the failure is
                    STRSAFE_E_INSUFFICIENT_BUFFER

        STRSAFE_NO_TRUNCATION /
        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. This will overwrite any truncated string
                    returned when the failure is STRSAFE_E_INSUFFICIENT_BUFFER.

Notes:
    Behavior is undefined if source and destination strings overlap.

    pszDest and pszSrc should not be NULL unless the STRSAFE_IGNORE_NULLS flag
    is specified.  If STRSAFE_IGNORE_NULLS is passed, both pszDest and pszSrc
    may be NULL.  An error may still be returned even though NULLS are ignored
    due to insufficient space.

Return Value:

    S_OK        -   if there was source data and it was all copied and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the copy operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCchCopyExA(char* pszDest, size_t cchDest, const char* pszSrc, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFEAPI StringCchCopyExW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
#ifdef UNICODE
#define StringCchCopyEx  StringCchCopyExW
#else
#define StringCchCopyEx  StringCchCopyExA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchCopyExA(char* pszDest, size_t cchDest, const char* pszSrc, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;

        // safe to multiply cchDest * sizeof(char) since cchDest < STRSAFE_MAX_CCH and sizeof(char) is 1
        cbDest = cchDest * sizeof(char);

        hr = StringCopyExWorkerA(pszDest, cchDest, cbDest, pszSrc, ppszDestEnd, pcchRemaining, dwFlags);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCchCopyExW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    
    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;

        // safe to multiply cchDest * sizeof(WCHAR) since cchDest < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
        cbDest = cchDest * sizeof(WCHAR);

        hr = StringCopyExWorkerW(pszDest, cchDest, cbDest, pszSrc, ppszDestEnd, pcchRemaining, dwFlags);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbCopyEx(TCHAR pszDest,
                      size_t cbDest,
                      LPCTSTR pszSrc,
                      LPTSTR* ppszDestEnd,
                      size_t* pcbRemaining,
                      DWORD dwFlags);

Routine Description:

    This routine is a safer version of the C built-in function 'strcpy' with
    some additional parameters.  In addition to functionality provided by
    StringCbCopy, this routine also returns a pointer to the end of the 
    destination string and the number of bytes left in the destination string
    including the null terminator. The flags parameter allows additional controls.

Arguments:

    pszDest         -   destination string

    cbDest          -   size of destination buffer in bytes.
                        length must be ((_tcslen(pszSrc) + 1) * sizeof(TCHAR)) to 
                        hold all of the source including the null terminator

    pszSrc          -   source string which must be null terminated

    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a
                        pointer to the end of the destination string.  If the
                        function copied any data, the result will point to the
                        null termination character

    pcbRemaining    -   pcbRemaining is non-null,the function will return the
                        number of bytes left in the destination string, 
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT("")).
                    this flag is useful for emulating functions like lstrcpy

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated. This will overwrite any truncated 
                    string returned when the failure is
                    STRSAFE_E_INSUFFICIENT_BUFFER

        STRSAFE_NO_TRUNCATION /
        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. This will overwrite any truncated string
                    returned when the failure is STRSAFE_E_INSUFFICIENT_BUFFER.

Notes:
    Behavior is undefined if source and destination strings overlap.

    pszDest and pszSrc should not be NULL unless the STRSAFE_IGNORE_NULLS flag
    is specified.  If STRSAFE_IGNORE_NULLS is passed, both pszDest and pszSrc
    may be NULL.  An error may still be returned even though NULLS are ignored
    due to insufficient space.

Return Value:

    S_OK        -   if there was source data and it was all copied and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the copy operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCbCopyExA(char* pszDest, size_t cbDest, const char* pszSrc, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags);
STRSAFEAPI StringCbCopyExW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags);
#ifdef UNICODE
#define StringCbCopyEx  StringCbCopyExW
#else
#define StringCbCopyEx  StringCbCopyExA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbCopyExA(char* pszDest, size_t cbDest, const char* pszSrc, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(char);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCopyExWorkerA(pszDest, cchDest, cbDest, pszSrc, ppszDestEnd, &cchRemaining, dwFlags);
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(char) since cchRemaining < STRSAFE_MAX_CCH and sizeof(char) is 1
            *pcbRemaining = (cchRemaining * sizeof(char)) + (cbDest % sizeof(char));
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbCopyExW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(WCHAR);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCopyExWorkerW(pszDest, cchDest, cbDest, pszSrc, ppszDestEnd, &cchRemaining, dwFlags);
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(WCHAR) since cchRemaining < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
            *pcbRemaining = (cchRemaining * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR));
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchCopyN(LPTSTR pszDest,
                      size_t cchDest,
                      LPCTSTR pszSrc,
                      size_t cchSrc);

Routine Description:

    This routine is a safer version of the C built-in function 'strncpy'.
    The size of the destination buffer (in characters) is a parameter and
    this function will not write past the end of this buffer and it will
    ALWAYS null terminate the destination buffer (unless it is zero length).

    This routine is meant as a replacement for strncpy, but it does behave
    differently. This function will not pad the destination buffer with extra
    null termination characters if cchSrc is greater than the length of pszSrc.

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if the entire string or the first cchSrc characters were copied without
    truncation and the resultant destination string was null terminated, otherwise
    it will return a failure code. In failure cases as much of pszSrc will be
    copied to pszDest as possible, and pszDest will be null terminated.

Arguments:

    pszDest     -   destination string

    cchDest     -   size of destination buffer in characters.
                    length must be = (_tcslen(src) + 1) to hold all of the
                    source including the null terminator

    pszSrc      -   source string

    cchSrc      -   maximum number of characters to copy from source string

Notes: 
    Behavior is undefined if source and destination strings overlap.
   
    pszDest and pszSrc should not be NULL. See StringCchCopyNEx if you require
    the handling of NULL values.

Return Value:

    S_OK        -   if there was source data and it was all copied and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all hresult falure cases

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

STRSAFEAPI StringCchCopyNA(char* pszDest, size_t cchDest, const char* pszSrc, size_t cchSrc);
STRSAFEAPI StringCchCopyNW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, size_t cchSrc);
#ifdef UNICODE
#define StringCchCopyN  StringCchCopyNW
#else
#define StringCchCopyN  StringCchCopyNA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchCopyNA(char* pszDest, size_t cchDest, const char* pszSrc, size_t cchSrc)
{
    HRESULT hr;

    if ((cchDest > STRSAFE_MAX_CCH) ||
        (cchSrc > STRSAFE_MAX_CCH))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCopyNWorkerA(pszDest, cchDest, pszSrc, cchSrc);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCchCopyNW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, size_t cchSrc)
{
    HRESULT hr;

    if ((cchDest > STRSAFE_MAX_CCH) || 
        (cchSrc > STRSAFE_MAX_CCH))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCopyNWorkerW(pszDest, cchDest, pszSrc, cchSrc);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbCopyN(LPTSTR pszDest,
                     size_t cbDest,
                     LPCTSTR pszSrc,
                     size_t cbSrc);

Routine Description:

    This routine is a safer version of the C built-in function 'strncpy'.
    The size of the destination buffer (in bytes) is a parameter and this
    function will not write past the end of this buffer and it will ALWAYS
    null terminate the destination buffer (unless it is zero length).

    This routine is meant as a replacement for strncpy, but it does behave
    differently. This function will not pad the destination buffer with extra
    null termination characters if cbSrc is greater than the size of pszSrc.

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if the entire string or the first cbSrc characters were copied without
    truncation and the resultant destination string was null terminated, otherwise
    it will return a failure code. In failure cases as much of pszSrc will be
    copied to pszDest as possible, and pszDest will be null terminated.

Arguments:

    pszDest     -   destination string

    cbDest      -   size of destination buffer in bytes.
                    length must be = ((_tcslen(src) + 1) * sizeof(TCHAR)) to
                    hold all of the source including the null terminator

    pszSrc      -   source string

    cbSrc       -   maximum number of bytes to copy from source string

Notes: 
    Behavior is undefined if source and destination strings overlap.
   
    pszDest and pszSrc should not be NULL.  See StringCbCopyEx if you require 
    the handling of NULL values.

Return Value:

    S_OK        -   if there was source data and it was all copied and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all hresult falure cases

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

STRSAFEAPI StringCbCopyNA(char* pszDest, size_t cbDest, const char* pszSrc, size_t cbSrc);
STRSAFEAPI StringCbCopyNW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc, size_t cbSrc);
#ifdef UNICODE
#define StringCbCopyN  StringCbCopyNW
#else
#define StringCbCopyN  StringCbCopyNA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbCopyNA(char* pszDest, size_t cbDest, const char* pszSrc, size_t cbSrc)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchSrc;

    // convert to count of characters
    cchDest = cbDest / sizeof(char);
    cchSrc = cbSrc / sizeof(char);

    if ((cchDest > STRSAFE_MAX_CCH) || 
        (cchSrc > STRSAFE_MAX_CCH))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCopyNWorkerA(pszDest, cchDest, pszSrc, cchSrc);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbCopyNW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc, size_t cbSrc)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchSrc;
    
    // convert to count of characters
    cchDest = cbDest / sizeof(WCHAR);
    cchSrc = cbSrc / sizeof(WCHAR);

    if ((cchDest > STRSAFE_MAX_CCH) ||
        (cchSrc > STRSAFE_MAX_CCH))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCopyNWorkerW(pszDest, cchDest, pszSrc, cchSrc);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchCopyNEx(TCHAR pszDest,
                        size_t cchDest,
                        LPCTSTR pszSrc,
                        size_t cchSrc,
                        LPTSTR* ppszDestEnd,
                        size_t* pcchRemaining,
                        DWORD dwFlags);

Routine Description:

    This routine is a safer version of the C built-in function 'strncpy' with
    some additional parameters.  In addition to functionality provided by
    StringCchCopyN, this routine also returns a pointer to the end of the
    destination string and the number of characters left in the destination
    string including the null terminator. The flags parameter allows
    additional controls.

    This routine is meant as a replacement for strncpy, but it does behave
    differently. This function will not pad the destination buffer with extra
    null termination characters if cchSrc is greater than the length of pszSrc.

Arguments:

    pszDest         -   destination string

    cchDest         -   size of destination buffer in characters.
                        length must be = (_tcslen(pszSrc) + 1) to hold all of
                        the source including the null terminator

    pszSrc          -   source string

    cchSrc          -   maximum number of characters to copy from the source
                        string

    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a
                        pointer to the end of the destination string.  If the
                        function copied any data, the result will point to the
                        null termination character

    pcchRemaining   -   if pcchRemaining is non-null, the function will return the
                        number of characters left in the destination string,
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT("")).
                    this flag is useful for emulating functions like lstrcpy

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated. This will overwrite any truncated 
                    string returned when the failure is
                    STRSAFE_E_INSUFFICIENT_BUFFER

        STRSAFE_NO_TRUNCATION /
        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. This will overwrite any truncated string
                    returned when the failure is STRSAFE_E_INSUFFICIENT_BUFFER.

Notes:
    Behavior is undefined if source and destination strings overlap.

    pszDest and pszSrc should not be NULL unless the STRSAFE_IGNORE_NULLS flag
    is specified. If STRSAFE_IGNORE_NULLS is passed, both pszDest and pszSrc
    may be NULL. An error may still be returned even though NULLS are ignored
    due to insufficient space.

Return Value:

    S_OK        -   if there was source data and it was all copied and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the copy operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCchCopyNExA(char* pszDest, size_t cchDest, const char* pszSrc, size_t cchSrc, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFEAPI StringCchCopyNExW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, size_t cchSrc, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
#ifdef UNICODE
#define StringCchCopyNEx  StringCchCopyNExW
#else
#define StringCchCopyNEx  StringCchCopyNExA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchCopyNExA(char* pszDest, size_t cchDest, const char* pszSrc, size_t cchSrc, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr;

    if ((cchDest > STRSAFE_MAX_CCH) ||
        (cchSrc > STRSAFE_MAX_CCH))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;

        // safe to multiply cchDest * sizeof(char) since cchDest < STRSAFE_MAX_CCH and sizeof(char) is 1
        cbDest = cchDest * sizeof(char);

        hr = StringCopyNExWorkerA(pszDest, cchDest, cbDest, pszSrc, cchSrc, ppszDestEnd, pcchRemaining, dwFlags);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCchCopyNExW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, size_t cchSrc, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    
    if ((cchDest > STRSAFE_MAX_CCH) ||
        (cchSrc > STRSAFE_MAX_CCH))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;

        // safe to multiply cchDest * sizeof(WCHAR) since cchDest < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
        cbDest = cchDest * sizeof(WCHAR);

        hr = StringCopyNExWorkerW(pszDest, cchDest, cbDest, pszSrc, cchSrc, ppszDestEnd, pcchRemaining, dwFlags);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbCopyNEx(TCHAR pszDest,
                       size_t cbDest,
                       LPCTSTR pszSrc,
                       size_t cbSrc,
                       LPTSTR* ppszDestEnd,
                       size_t* pcbRemaining,
                       DWORD dwFlags);

Routine Description:

    This routine is a safer version of the C built-in function 'strncpy' with
    some additional parameters.  In addition to functionality provided by
    StringCbCopyN, this routine also returns a pointer to the end of the 
    destination string and the number of bytes left in the destination string
    including the null terminator. The flags parameter allows additional controls.

    This routine is meant as a replacement for strncpy, but it does behave
    differently. This function will not pad the destination buffer with extra
    null termination characters if cbSrc is greater than the size of pszSrc.

Arguments:

    pszDest         -   destination string

    cbDest          -   size of destination buffer in bytes.
                        length must be ((_tcslen(pszSrc) + 1) * sizeof(TCHAR)) to 
                        hold all of the source including the null terminator

    pszSrc          -   source string

    cbSrc           -   maximum number of bytes to copy from source string

    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a
                        pointer to the end of the destination string.  If the
                        function copied any data, the result will point to the
                        null termination character

    pcbRemaining    -   pcbRemaining is non-null,the function will return the
                        number of bytes left in the destination string, 
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT("")).
                    this flag is useful for emulating functions like lstrcpy

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated. This will overwrite any truncated 
                    string returned when the failure is
                    STRSAFE_E_INSUFFICIENT_BUFFER

        STRSAFE_NO_TRUNCATION /
        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. This will overwrite any truncated string
                    returned when the failure is STRSAFE_E_INSUFFICIENT_BUFFER.

Notes:
    Behavior is undefined if source and destination strings overlap.

    pszDest and pszSrc should not be NULL unless the STRSAFE_IGNORE_NULLS flag
    is specified.  If STRSAFE_IGNORE_NULLS is passed, both pszDest and pszSrc
    may be NULL.  An error may still be returned even though NULLS are ignored
    due to insufficient space.

Return Value:

    S_OK        -   if there was source data and it was all copied and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the copy operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCbCopyNExA(char* pszDest, size_t cbDest, const char* pszSrc, size_t cbSrc, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags);
STRSAFEAPI StringCbCopyNExW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc, size_t cbSrc, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags);
#ifdef UNICODE
#define StringCbCopyNEx  StringCbCopyNExW
#else
#define StringCbCopyNEx  StringCbCopyNExA
#endif // !UNICODE


#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbCopyNExA(char* pszDest, size_t cbDest, const char* pszSrc, size_t cbSrc, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchSrc;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(char);
    cchSrc = cbSrc / sizeof(char);

    if ((cchDest > STRSAFE_MAX_CCH) ||
        (cchSrc > STRSAFE_MAX_CCH))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCopyNExWorkerA(pszDest, cchDest, cbDest, pszSrc, cchSrc, ppszDestEnd, &cchRemaining, dwFlags);
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(char) since cchRemaining < STRSAFE_MAX_CCH and sizeof(char) is 1
            *pcbRemaining = (cchRemaining * sizeof(char)) + (cbDest % sizeof(char));
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbCopyNExW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc, size_t cbSrc, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchSrc;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(WCHAR);
    cchSrc = cbSrc / sizeof(WCHAR);

    if ((cchDest > STRSAFE_MAX_CCH) ||
        (cchSrc > STRSAFE_MAX_CCH))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCopyNExWorkerW(pszDest, cchDest, cbDest, pszSrc, cchSrc, ppszDestEnd, &cchRemaining, dwFlags);
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(WCHAR) since cchRemaining < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
            *pcbRemaining = (cchRemaining * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR));
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchCat(LPTSTR pszDest,
                    size_t cchDest,
                    LPCTSTR pszSrc);

Routine Description:

    This routine is a safer version of the C built-in function 'strcat'.
    The size of the destination buffer (in characters) is a parameter and this
    function will not write past the end of this buffer and it will ALWAYS
    null terminate the destination buffer (unless it is zero length).

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if the string was concatenated without truncation and null terminated, otherwise
    it will return a failure code. In failure cases as much of pszSrc will be
    appended to pszDest as possible, and pszDest will be null terminated.

Arguments:

    pszDest     -  destination string which must be null terminated

    cchDest     -  size of destination buffer in characters.
                   length must be = (_tcslen(pszDest) + _tcslen(pszSrc) + 1)
                   to hold all of the combine string plus the null 
                   terminator

    pszSrc      -  source string which must be null terminated

Notes: 
    Behavior is undefined if source and destination strings overlap.
   
    pszDest and pszSrc should not be NULL.  See StringCchCatEx if you require
    the handling of NULL values.

Return Value:

    S_OK        -   if there was source data and it was all concatenated and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCchCatA(char* pszDest, size_t cchDest, const char* pszSrc);
STRSAFEAPI StringCchCatW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc);
#ifdef UNICODE
#define StringCchCat  StringCchCatW
#else
#define StringCchCat  StringCchCatA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchCatA(char* pszDest, size_t cchDest, const char* pszSrc)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCatWorkerA(pszDest, cchDest, pszSrc);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCchCatW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCatWorkerW(pszDest, cchDest, pszSrc);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbCat(LPTSTR pszDest,
                   size_t cbDest,
                   LPCTSTR pszSrc);

Routine Description:

    This routine is a safer version of the C built-in function 'strcat'.
    The size of the destination buffer (in bytes) is a parameter and this
    function will not write past the end of this buffer and it will ALWAYS
    null terminate the destination buffer (unless it is zero length).

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if the string was concatenated without truncation and null terminated, otherwise
    it will return a failure code. In failure cases as much of pszSrc will be
    appended to pszDest as possible, and pszDest will be null terminated.

Arguments:

    pszDest     -  destination string which must be null terminated

    cbDest      -  size of destination buffer in bytes.
                   length must be = ((_tcslen(pszDest) + _tcslen(pszSrc) + 1) * sizeof(TCHAR)
                   to hold all of the combine string plus the null 
                   terminator

    pszSrc      -  source string which must be null terminated

Notes: 
    Behavior is undefined if source and destination strings overlap.
   
    pszDest and pszSrc should not be NULL.  See StringCbCatEx if you require
    the handling of NULL values.

Return Value:

    S_OK        -   if there was source data and it was all concatenated and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCbCatA(char* pszDest, size_t cbDest, const char* pszSrc);
STRSAFEAPI StringCbCatW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc);
#ifdef UNICODE
#define StringCbCat  StringCbCatW
#else
#define StringCbCat  StringCbCatA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbCatA(char* pszDest, size_t cbDest, const char* pszSrc)
{
    HRESULT hr;
    size_t cchDest;
    
    cchDest = cbDest / sizeof(char);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCatWorkerA(pszDest, cchDest, pszSrc);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbCatW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc)
{
    HRESULT hr;
    size_t cchDest;
    
    cchDest = cbDest / sizeof(WCHAR);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCatWorkerW(pszDest, cchDest, pszSrc);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchCatEx(LPTSTR pszDest,
                      size_t cchDest,
                      LPCTSTR pszSrc,
                      LPTSTR* ppszDestEnd,
                      size_t* pcchRemaining,
                      DWORD dwFlags);

Routine Description:
    
    This routine is a safer version of the C built-in function 'strcat' with
    some additional parameters.  In addition to functionality provided by
    StringCchCat, this routine also returns a pointer to the end of the 
    destination string and the number of characters left in the destination string
    including the null terminator. The flags parameter allows additional controls.

Arguments:

    pszDest         -   destination string which must be null terminated

    cchDest         -   size of destination buffer in characters
                        length must be (_tcslen(pszDest) + _tcslen(pszSrc) + 1)
                        to hold all of the combine string plus the null
                        terminator.

    pszSrc          -   source string which must be null terminated

    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a
                        pointer to the end of the destination string.  If the
                        function appended any data, the result will point to the
                        null termination character

    pcchRemaining   -   if pcchRemaining is non-null, the function will return the
                        number of characters left in the destination string,
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT("")).
                    this flag is useful for emulating functions like lstrcat

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated. This will overwrite any pre-existing
                    or truncated string

        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. This will overwrite any pre-existing or
                    truncated string

        STRSAFE_NO_TRUNCATION
                    if the function returns STRSAFE_E_INSUFFICIENT_BUFFER, pszDest
                    will not contain a truncated string, it will remain unchanged.

Notes:
    Behavior is undefined if source and destination strings overlap.

    pszDest and pszSrc should not be NULL unless the STRSAFE_IGNORE_NULLS flag
    is specified.  If STRSAFE_IGNORE_NULLS is passed, both pszDest and pszSrc
    may be NULL.  An error may still be returned even though NULLS are ignored
    due to insufficient space.

Return Value:

    S_OK        -   if there was source data and it was all concatenated and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCchCatExA(char* pszDest, size_t cchDest, const char* pszSrc, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFEAPI StringCchCatExW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
#ifdef UNICODE
#define StringCchCatEx  StringCchCatExW
#else
#define StringCchCatEx  StringCchCatExA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchCatExA(char* pszDest, size_t cchDest, const char* pszSrc, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;

        // safe to multiply cchDest * sizeof(char) since cchDest < STRSAFE_MAX_CCH and sizeof(char) is 1
        cbDest = cchDest * sizeof(char);

        hr = StringCatExWorkerA(pszDest, cchDest, cbDest, pszSrc, ppszDestEnd, pcchRemaining, dwFlags);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCchCatExW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    
    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;

        // safe to multiply cchDest * sizeof(WCHAR) since cchDest < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
        cbDest = cchDest * sizeof(WCHAR);

        hr = StringCatExWorkerW(pszDest, cchDest, cbDest, pszSrc, ppszDestEnd, pcchRemaining, dwFlags);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbCatEx(LPTSTR pszDest,
                     size_t cbDest,
                     LPCTSTR pszSrc,
                     LPTSTR* ppszDestEnd,
                     size_t* pcbRemaining,
                     DWORD dwFlags);

Routine Description:

    This routine is a safer version of the C built-in function 'strcat' with
    some additional parameters.  In addition to functionality provided by
    StringCbCat, this routine also returns a pointer to the end of the 
    destination string and the number of bytes left in the destination string
    including the null terminator. The flags parameter allows additional controls.

Arguments:

    pszDest         -   destination string which must be null terminated

    cbDest          -   size of destination buffer in bytes.
                        length must be ((_tcslen(pszDest) + _tcslen(pszSrc) + 1) * sizeof(TCHAR) 
                        to hold all of the combine string plus the null
                        terminator.

    pszSrc          -   source string which must be null terminated

    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a
                        pointer to the end of the destination string.  If the
                        function appended any data, the result will point to the
                        null termination character

    pcbRemaining    -   if pcbRemaining is non-null, the function will return 
                        the number of bytes left in the destination string,
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT("")).
                    this flag is useful for emulating functions like lstrcat

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated. This will overwrite any pre-existing
                    or truncated string

        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. This will overwrite any pre-existing or
                    truncated string

        STRSAFE_NO_TRUNCATION
                    if the function returns STRSAFE_E_INSUFFICIENT_BUFFER, pszDest
                    will not contain a truncated string, it will remain unchanged.

Notes:
    Behavior is undefined if source and destination strings overlap.

    pszDest and pszSrc should not be NULL unless the STRSAFE_IGNORE_NULLS flag
    is specified.  If STRSAFE_IGNORE_NULLS is passed, both pszDest and pszSrc
    may be NULL.  An error may still be returned even though NULLS are ignored
    due to insufficient space.

Return Value:

    S_OK        -   if there was source data and it was all concatenated and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCbCatExA(char* pszDest, size_t cbDest, const char* pszSrc, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags);
STRSAFEAPI StringCbCatExW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags);
#ifdef UNICODE
#define StringCbCatEx  StringCbCatExW
#else
#define StringCbCatEx  StringCbCatExA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbCatExA(char* pszDest, size_t cbDest, const char* pszSrc, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(char);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCatExWorkerA(pszDest, cchDest, cbDest, pszSrc, ppszDestEnd, &cchRemaining, dwFlags);
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(char) since cchRemaining < STRSAFE_MAX_CCH and sizeof(char) is 1
            *pcbRemaining = (cchRemaining * sizeof(char)) + (cbDest % sizeof(char));
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbCatExW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(WCHAR);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCatExWorkerW(pszDest, cchDest, cbDest, pszSrc, ppszDestEnd, &cchRemaining, dwFlags);
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(WCHAR) since cchRemaining < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
            *pcbRemaining = (cchRemaining * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR));
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchCatN(LPTSTR pszDest,
                     size_t cchDest,
                     LPCTSTR pszSrc,
                     size_t cchMaxAppend);

Routine Description:

    This routine is a safer version of the C built-in function 'strncat'.
    The size of the destination buffer (in characters) is a parameter as well as
    the maximum number of characters to append, excluding the null terminator.
    This function will not write past the end of the destination buffer and it will
    ALWAYS null terminate pszDest (unless it is zero length).

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if all of pszSrc or the first cchMaxAppend characters were appended to the
    destination string and it was null terminated, otherwise it will return a
    failure code. In failure cases as much of pszSrc will be appended to pszDest
    as possible, and pszDest will be null terminated.

Arguments:

    pszDest         -   destination string which must be null terminated

    cchDest         -   size of destination buffer in characters.
                        length must be (_tcslen(pszDest) + min(cchMaxAppend, _tcslen(pszSrc)) + 1)
                        to hold all of the combine string plus the null
                        terminator.

    pszSrc          -   source string

    cchMaxAppend    -   maximum number of characters to append

Notes:
    Behavior is undefined if source and destination strings overlap.

    pszDest and pszSrc should not be NULL. See StringCchCatNEx if you require
    the handling of NULL values.

Return Value:

    S_OK        -   if all of pszSrc or the first cchMaxAppend characters were 
                    concatenated to pszDest and the resultant dest string was
                    null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCchCatNA(char* pszDest, size_t cchDest, const char* pszSrc, size_t cchMaxAppend);
STRSAFEAPI StringCchCatNW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, size_t cchMaxAppend);
#ifdef UNICODE
#define StringCchCatN  StringCchCatNW
#else
#define StringCchCatN  StringCchCatNA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchCatNA(char* pszDest, size_t cchDest, const char* pszSrc, size_t cchMaxAppend)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCatNWorkerA(pszDest, cchDest, pszSrc, cchMaxAppend);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCchCatNW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, size_t cchMaxAppend)
{
    HRESULT hr;
    
    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringCatNWorkerW(pszDest, cchDest, pszSrc, cchMaxAppend);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbCatN(LPTSTR pszDest,
                    size_t cbDest,
                    LPCTSTR pszSrc,
                    size_t cbMaxAppend);

Routine Description:

    This routine is a safer version of the C built-in function 'strncat'.
    The size of the destination buffer (in bytes) is a parameter as well as
    the maximum number of bytes to append, excluding the null terminator.
    This function will not write past the end of the destination buffer and it will
    ALWAYS null terminate pszDest (unless it is zero length).

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if all of pszSrc or the first cbMaxAppend bytes were appended to the
    destination string and it was null terminated, otherwise it will return a
    failure code. In failure cases as much of pszSrc will be appended to pszDest
    as possible, and pszDest will be null terminated.

Arguments:

    pszDest         -   destination string which must be null terminated

    cbDest          -   size of destination buffer in bytes.
                        length must be ((_tcslen(pszDest) + min(cbMaxAppend / sizeof(TCHAR), _tcslen(pszSrc)) + 1) * sizeof(TCHAR) 
                        to hold all of the combine string plus the null
                        terminator.

    pszSrc          -   source string

    cbMaxAppend     -   maximum number of bytes to append

Notes:
    Behavior is undefined if source and destination strings overlap.

    pszDest and pszSrc should not be NULL. See StringCbCatNEx if you require
    the handling of NULL values.

Return Value:

    S_OK        -   if all of pszSrc or the first cbMaxAppend bytes were 
                    concatenated to pszDest and the resultant dest string was
                    null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCbCatNA(char* pszDest, size_t cbDest, const char* pszSrc, size_t cbMaxAppend);
STRSAFEAPI StringCbCatNW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc, size_t cbMaxAppend);
#ifdef UNICODE
#define StringCbCatN  StringCbCatNW
#else
#define StringCbCatN  StringCbCatNA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbCatNA(char* pszDest, size_t cbDest, const char* pszSrc, size_t cbMaxAppend)
{
    HRESULT hr;
    size_t cchDest;

    cchDest = cbDest / sizeof(char);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cchMaxAppend;
        
        cchMaxAppend = cbMaxAppend / sizeof(char);

        hr = StringCatNWorkerA(pszDest, cchDest, pszSrc, cchMaxAppend);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbCatNW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc, size_t cbMaxAppend)
{
    HRESULT hr;
    size_t cchDest;

    cchDest = cbDest / sizeof(WCHAR);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cchMaxAppend;

        cchMaxAppend = cbMaxAppend / sizeof(WCHAR);

        hr = StringCatNWorkerW(pszDest, cchDest, pszSrc, cchMaxAppend);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchCatNEx(LPTSTR pszDest,
                       size_t cchDest,
                       LPCTSTR pszSrc,
                       size_t cchMaxAppend,
                       LPTSTR* ppszDestEnd,
                       size_t* pcchRemaining,
                       DWORD dwFlags);

Routine Description:
 
    This routine is a safer version of the C built-in function 'strncat', with 
    some additional parameters.  In addition to functionality provided by
    StringCchCatN, this routine also returns a pointer to the end of the 
    destination string and the number of characters left in the destination string
    including the null terminator. The flags parameter allows additional controls.

Arguments:

    pszDest         -   destination string which must be null terminated

    cchDest         -   size of destination buffer in characters.
                        length must be (_tcslen(pszDest) + min(cchMaxAppend, _tcslen(pszSrc)) + 1)
                        to hold all of the combine string plus the null
                        terminator.

    pszSrc          -   source string

    cchMaxAppend    -   maximum number of characters to append

    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a
                        pointer to the end of the destination string.  If the
                        function appended any data, the result will point to the
                        null termination character

    pcchRemaining   -   if pcchRemaining is non-null, the function will return the
                        number of characters left in the destination string,
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT(""))

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated. This will overwrite any pre-existing
                    or truncated string

        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. This will overwrite any pre-existing or
                    truncated string

        STRSAFE_NO_TRUNCATION
                    if the function returns STRSAFE_E_INSUFFICIENT_BUFFER, pszDest
                    will not contain a truncated string, it will remain unchanged.

Notes:
    Behavior is undefined if source and destination strings overlap.

    pszDest and pszSrc should not be NULL unless the STRSAFE_IGNORE_NULLS flag
    is specified.  If STRSAFE_IGNORE_NULLS is passed, both pszDest and pszSrc
    may be NULL.  An error may still be returned even though NULLS are ignored
    due to insufficient space.

Return Value:

    S_OK        -   if all of pszSrc or the first cchMaxAppend characters were 
                    concatenated to pszDest and the resultant dest string was
                    null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCchCatNExA(char* pszDest, size_t cchDest, const char* pszSrc, size_t cchMaxAppend, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFEAPI StringCchCatNExW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, size_t cchMaxAppend, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
#ifdef UNICODE
#define StringCchCatNEx  StringCchCatNExW
#else
#define StringCchCatNEx  StringCchCatNExA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchCatNExA(char* pszDest, size_t cchDest, const char* pszSrc, size_t cchMaxAppend, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;

        // safe to multiply cchDest * sizeof(char) since cchDest < STRSAFE_MAX_CCH and sizeof(char) is 1
        cbDest = cchDest * sizeof(char);

        hr = StringCatNExWorkerA(pszDest, cchDest, cbDest, pszSrc, cchMaxAppend, ppszDestEnd, pcchRemaining, dwFlags);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCchCatNExW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, size_t cchMaxAppend, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    
    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;

        // safe to multiply cchDest * sizeof(WCHAR) since cchDest < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
        cbDest = cchDest * sizeof(WCHAR);

        hr = StringCatNExWorkerW(pszDest, cchDest, cbDest, pszSrc, cchMaxAppend, ppszDestEnd, pcchRemaining, dwFlags);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbCatNEx(LPTSTR pszDest,
                      size_t cbDest,
                      LPCTSTR pszSrc,
                      size_t cbMaxAppend
                      LPTSTR* ppszDestEnd,
                      size_t* pcchRemaining,
                      DWORD dwFlags);

Routine Description:
    
    This routine is a safer version of the C built-in function 'strncat', with 
    some additional parameters.  In addition to functionality provided by
    StringCbCatN, this routine also returns a pointer to the end of the 
    destination string and the number of bytes left in the destination string
    including the null terminator. The flags parameter allows additional controls.

Arguments:

    pszDest         -   destination string which must be null terminated

    cbDest          -   size of destination buffer in bytes.
                        length must be ((_tcslen(pszDest) + min(cbMaxAppend / sizeof(TCHAR), _tcslen(pszSrc)) + 1) * sizeof(TCHAR) 
                        to hold all of the combine string plus the null
                        terminator.

    pszSrc          -   source string

    cbMaxAppend     -   maximum number of bytes to append

    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a
                        pointer to the end of the destination string.  If the
                        function appended any data, the result will point to the
                        null termination character

    pcbRemaining    -   if pcbRemaining is non-null, the function will return the
                        number of bytes left in the destination string,
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT(""))

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated. This will overwrite any pre-existing
                    or truncated string

        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. This will overwrite any pre-existing or
                    truncated string

        STRSAFE_NO_TRUNCATION
                    if the function returns STRSAFE_E_INSUFFICIENT_BUFFER, pszDest
                    will not contain a truncated string, it will remain unchanged.

Notes:
    Behavior is undefined if source and destination strings overlap.

    pszDest and pszSrc should not be NULL unless the STRSAFE_IGNORE_NULLS flag
    is specified.  If STRSAFE_IGNORE_NULLS is passed, both pszDest and pszSrc
    may be NULL.  An error may still be returned even though NULLS are ignored
    due to insufficient space.

Return Value:

    S_OK        -   if all of pszSrc or the first cbMaxAppend bytes were 
                    concatenated to pszDest and the resultant dest string was
                    null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCbCatNExA(char* pszDest, size_t cbDest, const char* pszSrc, size_t cbMaxAppend, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags);
STRSAFEAPI StringCbCatNExW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc, size_t cbMaxAppend, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags);
#ifdef UNICODE
#define StringCbCatNEx  StringCbCatNExW
#else
#define StringCbCatNEx  StringCbCatNExA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbCatNExA(char* pszDest, size_t cbDest, const char* pszSrc, size_t cbMaxAppend, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(char);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cchMaxAppend;
        
        cchMaxAppend = cbMaxAppend / sizeof(char);

        hr = StringCatNExWorkerA(pszDest, cchDest, cbDest, pszSrc, cchMaxAppend, ppszDestEnd, &cchRemaining, dwFlags);
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(char) since cchRemaining < STRSAFE_MAX_CCH and sizeof(char) is 1
            *pcbRemaining = (cchRemaining * sizeof(char)) + (cbDest % sizeof(char));
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbCatNExW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszSrc, size_t cbMaxAppend, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(WCHAR);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cchMaxAppend;
        
        cchMaxAppend = cbMaxAppend / sizeof(WCHAR);

        hr = StringCatNExWorkerW(pszDest, cchDest, cbDest, pszSrc, cchMaxAppend, ppszDestEnd, &cchRemaining, dwFlags);
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(WCHAR) since cchRemaining < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
            *pcbRemaining = (cchRemaining * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR));
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchVPrintf(LPTSTR pszDest,
                        size_t cchDest,
                        LPCTSTR pszFormat,
                        va_list argList);

Routine Description:

    This routine is a safer version of the C built-in function 'vsprintf'.
    The size of the destination buffer (in characters) is a parameter and
    this function will not write past the end of this buffer and it will
    ALWAYS null terminate the destination buffer (unless it is zero length).

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if the string was printed without truncation and null terminated, otherwise
    it will return a failure code. In failure cases it will return a truncated
    version of the ideal result.

Arguments:

    pszDest     -  destination string

    cchDest     -  size of destination buffer in characters
                   length must be sufficient to hold the resulting formatted
                   string, including the null terminator.

    pszFormat   -  format string which must be null terminated

    argList     -  va_list from the variable arguments according to the
                   stdarg.h convention

Notes: 
    Behavior is undefined if destination, format strings or any arguments
    strings overlap.
   
    pszDest and pszFormat should not be NULL.  See StringCchVPrintfEx if you
    require the handling of NULL values.

Return Value:

    S_OK        -   if there was sufficient space in the dest buffer for
                    the resultant string and it was null terminated.

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the print operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCchVPrintfA(char* pszDest, size_t cchDest, const char* pszFormat, va_list argList);
STRSAFEAPI StringCchVPrintfW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszFormat, va_list argList);
#ifdef UNICODE
#define StringCchVPrintf  StringCchVPrintfW
#else
#define StringCchVPrintf  StringCchVPrintfA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchVPrintfA(char* pszDest, size_t cchDest, const char* pszFormat, va_list argList)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringVPrintfWorkerA(pszDest, cchDest, pszFormat, argList);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCchVPrintfW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszFormat, va_list argList)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringVPrintfWorkerW(pszDest, cchDest, pszFormat, argList);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbVPrintf(LPTSTR pszDest,
                       size_t cbDest,
                       LPCTSTR pszFormat,
                       va_list argList);

Routine Description:

    This routine is a safer version of the C built-in function 'vsprintf'.
    The size of the destination buffer (in bytes) is a parameter and
    this function will not write past the end of this buffer and it will
    ALWAYS null terminate the destination buffer (unless it is zero length).

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if the string was printed without truncation and null terminated, otherwise
    it will return a failure code. In failure cases it will return a truncated
    version of the ideal result.

Arguments:

    pszDest     -  destination string

    cbDest      -  size of destination buffer in bytes
                   length must be sufficient to hold the resulting formatted
                   string, including the null terminator.

    pszFormat   -  format string which must be null terminated

    argList     -  va_list from the variable arguments according to the
                   stdarg.h convention

Notes: 
    Behavior is undefined if destination, format strings or any arguments
    strings overlap.
   
    pszDest and pszFormat should not be NULL.  See StringCbVPrintfEx if you
    require the handling of NULL values.


Return Value:

    S_OK        -   if there was sufficient space in the dest buffer for
                    the resultant string and it was null terminated.

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the print operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCbVPrintfA(char* pszDest, size_t cbDest, const char* pszFormat, va_list argList);
STRSAFEAPI StringCbVPrintfW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszFormat, va_list argList);
#ifdef UNICODE
#define StringCbVPrintf  StringCbVPrintfW
#else
#define StringCbVPrintf  StringCbVPrintfA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbVPrintfA(char* pszDest, size_t cbDest, const char* pszFormat, va_list argList)
{
    HRESULT hr;
    size_t cchDest;

    cchDest = cbDest / sizeof(char);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringVPrintfWorkerA(pszDest, cchDest, pszFormat, argList);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbVPrintfW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszFormat, va_list argList)
{
    HRESULT hr;
    size_t cchDest;

    cchDest = cbDest / sizeof(WCHAR);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringVPrintfWorkerW(pszDest, cchDest, pszFormat, argList);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchPrintf(LPTSTR pszDest,
                       size_t cchDest,
                       LPCTSTR pszFormat,
                       ...);

Routine Description:

    This routine is a safer version of the C built-in function 'sprintf'.
    The size of the destination buffer (in characters) is a parameter and
    this function will not write past the end of this buffer and it will
    ALWAYS null terminate the destination buffer (unless it is zero length).

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if the string was printed without truncation and null terminated, otherwise
    it will return a failure code. In failure cases it will return a truncated
    version of the ideal result.

Arguments:

    pszDest     -  destination string

    cchDest     -  size of destination buffer in characters
                   length must be sufficient to hold the resulting formatted
                   string, including the null terminator.

    pszFormat   -  format string which must be null terminated

    ...         -  additional parameters to be formatted according to
                   the format string

Notes: 
    Behavior is undefined if destination, format strings or any arguments
    strings overlap.
   
    pszDest and pszFormat should not be NULL.  See StringCchPrintfEx if you
    require the handling of NULL values.

Return Value:

    S_OK        -   if there was sufficient space in the dest buffer for
                    the resultant string and it was null terminated.

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the print operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCchPrintfA(char* pszDest, size_t cchDest, const char* pszFormat, ...);
STRSAFEAPI StringCchPrintfW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszFormat, ...);
#ifdef UNICODE
#define StringCchPrintf  StringCchPrintfW
#else
#define StringCchPrintf  StringCchPrintfA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchPrintfA(char* pszDest, size_t cchDest, const char* pszFormat, ...)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        va_list argList;

        va_start(argList, pszFormat);

        hr = StringVPrintfWorkerA(pszDest, cchDest, pszFormat, argList);
        
        va_end(argList);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCchPrintfW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszFormat, ...)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        va_list argList;

        va_start(argList, pszFormat);

        hr = StringVPrintfWorkerW(pszDest, cchDest, pszFormat, argList);
    
        va_end(argList);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbPrintf(LPTSTR pszDest,
                      size_t cbDest,
                      LPCTSTR pszFormat,
                      ...);

Routine Description:

    This routine is a safer version of the C built-in function 'sprintf'.
    The size of the destination buffer (in bytes) is a parameter and
    this function will not write past the end of this buffer and it will
    ALWAYS null terminate the destination buffer (unless it is zero length).

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if the string was printed without truncation and null terminated, otherwise
    it will return a failure code. In failure cases it will return a truncated
    version of the ideal result.

Arguments:

    pszDest     -  destination string

    cbDest      -  size of destination buffer in bytes
                   length must be sufficient to hold the resulting formatted
                   string, including the null terminator.

    pszFormat   -  format string which must be null terminated

    ...         -  additional parameters to be formatted according to
                   the format string

Notes: 
    Behavior is undefined if destination, format strings or any arguments
    strings overlap.
   
    pszDest and pszFormat should not be NULL.  See StringCbPrintfEx if you
    require the handling of NULL values.


Return Value:

    S_OK        -   if there was sufficient space in the dest buffer for
                    the resultant string and it was null terminated.

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the print operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCbPrintfA(char* pszDest, size_t cbDest, const char* pszFormat, ...);
STRSAFEAPI StringCbPrintfW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszFormat, ...);
#ifdef UNICODE
#define StringCbPrintf  StringCbPrintfW
#else
#define StringCbPrintf  StringCbPrintfA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbPrintfA(char* pszDest, size_t cbDest, const char* pszFormat, ...)
{
    HRESULT hr;
    size_t cchDest;
    
    cchDest = cbDest / sizeof(char);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        va_list argList;

        va_start(argList, pszFormat);
   
        hr = StringVPrintfWorkerA(pszDest, cchDest, pszFormat, argList);

        va_end(argList);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbPrintfW(WCHAR* pszDest, size_t cbDest, const WCHAR* pszFormat, ...)
{
    HRESULT hr;
    size_t cchDest;
    
    cchDest = cbDest / sizeof(WCHAR);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        va_list argList;

        va_start(argList, pszFormat);
    
        hr = StringVPrintfWorkerW(pszDest, cchDest, pszFormat, argList);

        va_end(argList);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchPrintfEx(LPTSTR pszDest,
                         size_t cchDest,
                         LPTSTR* ppszDestEnd,
                         size_t* pcchRemaining,
                         DWORD dwFlags,
                         LPCTSTR pszFormat,
                         ...);

Routine Description:

    This routine is a safer version of the C built-in function 'sprintf' with
    some additional parameters.  In addition to functionality provided by
    StringCchPrintf, this routine also returns a pointer to the end of the 
    destination string and the number of characters left in the destination string
    including the null terminator. The flags parameter allows additional controls.

Arguments:

    pszDest         -   destination string

    cchDest         -   size of destination buffer in characters.
                        length must be sufficient to contain the resulting
                        formatted string plus the null terminator.

    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a
                        pointer to the end of the destination string.  If the
                        function printed any data, the result will point to the
                        null termination character

    pcchRemaining   -   if pcchRemaining is non-null, the function will return 
                        the number of characters left in the destination string,
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT(""))

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated. This will overwrite any truncated 
                    string returned when the failure is
                    STRSAFE_E_INSUFFICIENT_BUFFER

        STRSAFE_NO_TRUNCATION /
        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. This will overwrite any truncated string
                    returned when the failure is STRSAFE_E_INSUFFICIENT_BUFFER.

    pszFormat       -   format string which must be null terminated

    ...             -   additional parameters to be formatted according to
                        the format string

Notes:
    Behavior is undefined if destination, format strings or any arguments
    strings overlap.

    pszDest and pszFormat should not be NULL unless the STRSAFE_IGNORE_NULLS
    flag is specified.  If STRSAFE_IGNORE_NULLS is passed, both pszDest and
    pszFormat may be NULL.  An error may still be returned even though NULLS
    are ignored due to insufficient space.

Return Value:

    S_OK        -   if there was source data and it was all concatenated and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the print operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCchPrintfExA(char* pszDest, size_t cchDest, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags, const char* pszFormat, ...);
STRSAFEAPI StringCchPrintfExW(WCHAR* pszDest, size_t cchDest, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags, const WCHAR* pszFormat, ...);
#ifdef UNICODE
#define StringCchPrintfEx  StringCchPrintfExW
#else
#define StringCchPrintfEx  StringCchPrintfExA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchPrintfExA(char* pszDest, size_t cchDest, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags, const char* pszFormat, ...)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;
        va_list argList;

        // safe to multiply cchDest * sizeof(char) since cchDest < STRSAFE_MAX_CCH and sizeof(char) is 1
        cbDest = cchDest * sizeof(char);
        va_start(argList, pszFormat);

        hr = StringVPrintfExWorkerA(pszDest, cchDest, cbDest, ppszDestEnd, pcchRemaining, dwFlags, pszFormat, argList);
        
        va_end(argList);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCchPrintfExW(WCHAR* pszDest, size_t cchDest, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags, const WCHAR* pszFormat, ...)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;
        va_list argList;

        // safe to multiply cchDest * sizeof(WCHAR) since cchDest < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
        cbDest = cchDest * sizeof(WCHAR);
        va_start(argList, pszFormat);

        hr = StringVPrintfExWorkerW(pszDest, cchDest, cbDest, ppszDestEnd, pcchRemaining, dwFlags, pszFormat, argList);
    
        va_end(argList);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbPrintfEx(LPTSTR pszDest,
                        size_t cbDest,
                        LPTSTR* ppszDestEnd,
                        size_t* pcbRemaining,
                        DWORD dwFlags,
                        LPCTSTR pszFormat,
                        ...);

Routine Description:

    This routine is a safer version of the C built-in function 'sprintf' with
    some additional parameters.  In addition to functionality provided by
    StringCbPrintf, this routine also returns a pointer to the end of the 
    destination string and the number of bytes left in the destination string
    including the null terminator. The flags parameter allows additional controls.

Arguments:

    pszDest         -   destination string

    cbDest          -   size of destination buffer in bytes.
                        length must be sufficient to contain the resulting
                        formatted string plus the null terminator.

    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a
                        pointer to the end of the destination string.  If the
                        function printed any data, the result will point to the
                        null termination character

    pcbRemaining    -   if pcbRemaining is non-null, the function will return 
                        the number of bytes left in the destination string,
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT(""))

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated. This will overwrite any truncated 
                    string returned when the failure is
                    STRSAFE_E_INSUFFICIENT_BUFFER

        STRSAFE_NO_TRUNCATION /
        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. This will overwrite any truncated string
                    returned when the failure is STRSAFE_E_INSUFFICIENT_BUFFER.

    pszFormat       -   format string which must be null terminated

    ...             -   additional parameters to be formatted according to
                        the format string

Notes:
    Behavior is undefined if destination, format strings or any arguments
    strings overlap.

    pszDest and pszFormat should not be NULL unless the STRSAFE_IGNORE_NULLS
    flag is specified.  If STRSAFE_IGNORE_NULLS is passed, both pszDest and
    pszFormat may be NULL.  An error may still be returned even though NULLS
    are ignored due to insufficient space.

Return Value:

    S_OK        -   if there was source data and it was all concatenated and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the print operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCbPrintfExA(char* pszDest, size_t cbDest, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags, const char* pszFormat, ...);
STRSAFEAPI StringCbPrintfExW(WCHAR* pszDest, size_t cbDest, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags, const WCHAR* pszFormat, ...);
#ifdef UNICODE
#define StringCbPrintfEx  StringCbPrintfExW
#else
#define StringCbPrintfEx  StringCbPrintfExA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbPrintfExA(char* pszDest, size_t cbDest, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags, const char* pszFormat, ...)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(char);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        va_list argList;

        va_start(argList, pszFormat);

        hr = StringVPrintfExWorkerA(pszDest, cchDest, cbDest, ppszDestEnd, &cchRemaining, dwFlags, pszFormat, argList);
    
        va_end(argList);
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(char) since cchRemaining < STRSAFE_MAX_CCH and sizeof(char) is 1
            *pcbRemaining = (cchRemaining * sizeof(char)) + (cbDest % sizeof(char));
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbPrintfExW(WCHAR* pszDest, size_t cbDest, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags, const WCHAR* pszFormat, ...)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(WCHAR);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        va_list argList;

        va_start(argList, pszFormat);

        hr = StringVPrintfExWorkerW(pszDest, cchDest, cbDest, ppszDestEnd, &cchRemaining, dwFlags, pszFormat, argList);
    
        va_end(argList);
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(WCHAR) since cchRemaining < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
            *pcbRemaining = (cchRemaining * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR));
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchVPrintfEx(LPTSTR pszDest,
                          size_t cchDest,
                          LPTSTR* ppszDestEnd,
                          size_t* pcchRemaining,
                          DWORD dwFlags,
                          LPCTSTR pszFormat,
                          va_list argList);

Routine Description:

    This routine is a safer version of the C built-in function 'vsprintf' with
    some additional parameters.  In addition to functionality provided by
    StringCchVPrintf, this routine also returns a pointer to the end of the 
    destination string and the number of characters left in the destination string
    including the null terminator. The flags parameter allows additional controls.

Arguments:

    pszDest         -   destination string

    cchDest         -   size of destination buffer in characters.
                        length must be sufficient to contain the resulting
                        formatted string plus the null terminator.

    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a 
                        pointer to the end of the destination string.  If the 
                        function printed any data, the result will point to the 
                        null termination character

    pcchRemaining   -   if pcchRemaining is non-null, the function will return
                        the number of characters left in the destination string,
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT(""))

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated. This will overwrite any truncated 
                    string returned when the failure is
                    STRSAFE_E_INSUFFICIENT_BUFFER

        STRSAFE_NO_TRUNCATION /
        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. This will overwrite any truncated string
                    returned when the failure is STRSAFE_E_INSUFFICIENT_BUFFER.

    pszFormat       -   format string which must be null terminated

    argList         -   va_list from the variable arguments according to the
                        stdarg.h convention

Notes:
    Behavior is undefined if destination, format strings or any arguments
    strings overlap.

    pszDest and pszFormat should not be NULL unless the STRSAFE_IGNORE_NULLS
    flag is specified.  If STRSAFE_IGNORE_NULLS is passed, both pszDest and
    pszFormat may be NULL.  An error may still be returned even though NULLS
    are ignored due to insufficient space.

Return Value:

    S_OK        -   if there was source data and it was all concatenated and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the print operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCchVPrintfExA(char* pszDest, size_t cchDest, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags, const char* pszFormat, va_list argList);
STRSAFEAPI StringCchVPrintfExW(WCHAR* pszDest, size_t cchDest, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags, const WCHAR* pszFormat, va_list argList);
#ifdef UNICODE
#define StringCchVPrintfEx  StringCchVPrintfExW
#else
#define StringCchVPrintfEx  StringCchVPrintfExA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchVPrintfExA(char* pszDest, size_t cchDest, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags, const char* pszFormat, va_list argList)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;

        // safe to multiply cchDest * sizeof(char) since cchDest < STRSAFE_MAX_CCH and sizeof(char) is 1
        cbDest = cchDest * sizeof(char);
        
        hr = StringVPrintfExWorkerA(pszDest, cchDest, cbDest, ppszDestEnd, pcchRemaining, dwFlags, pszFormat, argList);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCchVPrintfExW(WCHAR* pszDest, size_t cchDest, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags, const WCHAR* pszFormat, va_list argList)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;

        // safe to multiply cchDest * sizeof(WCHAR) since cchDest < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
        cbDest = cchDest * sizeof(WCHAR);

        hr = StringVPrintfExWorkerW(pszDest, cchDest, cbDest, ppszDestEnd, pcchRemaining, dwFlags, pszFormat, argList);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbVPrintfEx(LPTSTR pszDest,
                         size_t cbDest,
                         LPTSTR* ppszDestEnd,
                         size_t* pcbRemaining,
                         DWORD dwFlags,
                         LPCTSTR pszFormat,
                         va_list argList);

Routine Description:

    This routine is a safer version of the C built-in function 'vsprintf' with
    some additional parameters.  In addition to functionality provided by
    StringCbVPrintf, this routine also returns a pointer to the end of the 
    destination string and the number of characters left in the destination string
    including the null terminator. The flags parameter allows additional controls.

Arguments:

    pszDest         -   destination string

    cbDest          -   size of destination buffer in bytes.
                        length must be sufficient to contain the resulting
                        formatted string plus the null terminator.

    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return 
                        a pointer to the end of the destination string.  If the
                        function printed any data, the result will point to the
                        null termination character

    pcbRemaining    -   if pcbRemaining is non-null, the function will return
                        the number of bytes left in the destination string,
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT(""))

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated. This will overwrite any truncated 
                    string returned when the failure is
                    STRSAFE_E_INSUFFICIENT_BUFFER

        STRSAFE_NO_TRUNCATION /
        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. This will overwrite any truncated string
                    returned when the failure is STRSAFE_E_INSUFFICIENT_BUFFER.

    pszFormat       -   format string which must be null terminated

    argList         -   va_list from the variable arguments according to the
                        stdarg.h convention

Notes:
    Behavior is undefined if destination, format strings or any arguments
    strings overlap.

    pszDest and pszFormat should not be NULL unless the STRSAFE_IGNORE_NULLS
    flag is specified.  If STRSAFE_IGNORE_NULLS is passed, both pszDest and
    pszFormat may be NULL.  An error may still be returned even though NULLS
    are ignored due to insufficient space.

Return Value:

    S_OK        -   if there was source data and it was all concatenated and the
                    resultant dest string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all falure cases

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that the print operation
                    failed due to insufficient space. When this error occurs,
                    the destination buffer is modified to contain a truncated
                    version of the ideal result and is null terminated. This
                    is useful for situations where truncation is ok.

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function

--*/

STRSAFEAPI StringCbVPrintfExA(char* pszDest, size_t cbDest, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags, const char* pszFormat, va_list argList);
STRSAFEAPI StringCbVPrintfExW(WCHAR* pszDest, size_t cbDest, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags, const WCHAR* pszFormat, va_list argList);
#ifdef UNICODE
#define StringCbVPrintfEx  StringCbVPrintfExW
#else
#define StringCbVPrintfEx  StringCbVPrintfExA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbVPrintfExA(char* pszDest, size_t cbDest, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags, const char* pszFormat, va_list argList)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(char);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringVPrintfExWorkerA(pszDest, cchDest, cbDest, ppszDestEnd, &cchRemaining, dwFlags, pszFormat, argList);
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(char) since cchRemaining < STRSAFE_MAX_CCH and sizeof(char) is 1
            *pcbRemaining = (cchRemaining * sizeof(char)) + (cbDest % sizeof(char));
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbVPrintfExW(WCHAR* pszDest, size_t cbDest, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags, const WCHAR* pszFormat, va_list argList)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchRemaining = 0;
    
    cchDest = cbDest / sizeof(WCHAR);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringVPrintfExWorkerW(pszDest, cchDest, cbDest, ppszDestEnd, &cchRemaining, dwFlags, pszFormat, argList);
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(WCHAR) since cchRemaining < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
            *pcbRemaining = (cchRemaining * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR));
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchGets(LPTSTR pszDest,
                     size_t cchDest);

Routine Description:

    This routine is a safer version of the C built-in function 'gets'.
    The size of the destination buffer (in characters) is a parameter and
    this function will not write past the end of this buffer and it will
    ALWAYS null terminate the destination buffer (unless it is zero length).

    This routine is not a replacement for fgets.  That function does not replace
    newline characters with a null terminator.

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if any characters were read from stdin and copied to pszDest and pszDest was
    null terminated, otherwise it will return a failure code.

Arguments:

    pszDest     -   destination string

    cchDest     -   size of destination buffer in characters.

Notes: 
    pszDest should not be NULL. See StringCchGetsEx if you require the handling 
    of NULL values.

    cchDest must be > 1 for this function to succeed.

Return Value:

    S_OK        -   data was read from stdin and copied, and the resultant dest
                    string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all hresult falure cases

    STRSAFE_E_END_OF_FILE
                -   this return value indicates an error or end-of-file condition,
                    use feof or ferror to determine which one has occurred.

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that there was insufficient
                    space in the destination buffer to copy any data

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function.

--*/

#ifndef FEATURE_PAL
#ifndef STRSAFE_LIB_IMPL
STRSAFE_INLINE_API StringCchGetsA(char* pszDest, size_t cchDest);
STRSAFE_INLINE_API StringCchGetsW(WCHAR* pszDest, size_t cchDest);
#ifdef UNICODE
#define StringCchGets  StringCchGetsW
#else
#define StringCchGets  StringCchGetsA
#endif // !UNICODE

STRSAFE_INLINE_API StringCchGetsA(char* pszDest, size_t cchDest)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;
        
        // safe to multiply cchDest * sizeof(char) since cchDest < STRSAFE_MAX_CCH and sizeof(char) is 1
        cbDest = cchDest * sizeof(char);

        hr = StringGetsExWorkerA(pszDest, cchDest, cbDest, NULL, NULL, 0);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFE_INLINE_API StringCchGetsW(WCHAR* pszDest, size_t cchDest)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;
        
        // safe to multiply cchDest * sizeof(WCHAR) since cchDest < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
        cbDest = cchDest * sizeof(WCHAR);

        hr = StringGetsExWorkerW(pszDest, cchDest, cbDest, NULL, NULL, 0);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // !STRSAFE_NO_CCH_FUNCTIONS
#endif  // !STRSAFE_LIB_IMPL
#endif  // !FEATURE_PAL

#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbGets(LPTSTR pszDest,
                    size_t cbDest);

Routine Description:

    This routine is a safer version of the C built-in function 'gets'.
    The size of the destination buffer (in bytes) is a parameter and
    this function will not write past the end of this buffer and it will
    ALWAYS null terminate the destination buffer (unless it is zero length).

    This routine is not a replacement for fgets.  That function does not replace
    newline characters with a null terminator.

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if any characters were read from stdin and copied to pszDest and pszDest was
    null terminated, otherwise it will return a failure code.

Arguments:

    pszDest     -   destination string

    cbDest      -   size of destination buffer in bytes.

Notes: 
    pszDest should not be NULL. See StringCbGetsEx if you require the handling 
    of NULL values.

    cbDest must be > sizeof(TCHAR) for this function to succeed.

Return Value:

    S_OK        -   data was read from stdin and copied, and the resultant dest
                    string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all hresult falure cases

    STRSAFE_E_END_OF_FILE
                -   this return value indicates an error or end-of-file condition,
                    use feof or ferror to determine which one has occurred.

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that there was insufficient
                    space in the destination buffer to copy any data

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function.

--*/

#ifndef FEATURE_PAL
#ifndef STRSAFE_LIB_IMPL
STRSAFE_INLINE_API StringCbGetsA(char* pszDest, size_t cbDest);
STRSAFE_INLINE_API StringCbGetsW(WCHAR* pszDest, size_t cbDest);
#ifdef UNICODE
#define StringCbGets  StringCbGetsW
#else
#define StringCbGets  StringCbGetsA
#endif // !UNICODE

STRSAFE_INLINE_API StringCbGetsA(char* pszDest, size_t cbDest)
{
    HRESULT hr;
    size_t cchDest;
    
    // convert to count of characters
    cchDest = cbDest / sizeof(char);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringGetsExWorkerA(pszDest, cchDest, cbDest, NULL, NULL, 0);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFE_INLINE_API StringCbGetsW(WCHAR* pszDest, size_t cbDest)
{
    HRESULT hr;
    size_t cchDest;
    
    // convert to count of characters
    cchDest = cbDest / sizeof(WCHAR);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringGetsExWorkerW(pszDest, cchDest, cbDest, NULL, NULL, 0);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // !STRSAFE_NO_CB_FUNCTIONS
#endif  // !STRSAFE_LIB_IMPL
#endif  // !FEATURE_PAL

#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchGetsEx(LPTSTR pszDest,
                       size_t cchDest,
                       LPTSTR* ppszDestEnd,
                       size_t* pcchRemaining,
                       DWORD dwFlags);

Routine Description:

    This routine is a safer version of the C built-in function 'gets' with
    some additional parameters. In addition to functionality provided by
    StringCchGets, this routine also returns a pointer to the end of the
    destination string and the number of characters left in the destination string
    including the null terminator. The flags parameter allows additional controls.

Arguments:

    pszDest         -   destination string

    cchDest         -   size of destination buffer in characters.
                     
    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a
                        pointer to the end of the destination string.  If the
                        function copied any data, the result will point to the
                        null termination character

    pcchRemaining   -   if pcchRemaining is non-null, the function will return the
                        number of characters left in the destination string,
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT("")).

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated.

        STRSAFE_NO_TRUNCATION /
        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. 
                    
Notes:
    pszDest should not be NULL unless the STRSAFE_IGNORE_NULLS flag is specified.
    If STRSAFE_IGNORE_NULLS is passed and pszDest is NULL, an error may still be 
    returned even though NULLS are ignored

    cchDest must be > 1 for this function to succeed.

Return Value:

    S_OK        -   data was read from stdin and copied, and the resultant dest
                    string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all hresult falure cases

    STRSAFE_E_END_OF_FILE
                -   this return value indicates an error or end-of-file condition,
                    use feof or ferror to determine which one has occurred.

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that there was insufficient
                    space in the destination buffer to copy any data

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function.

--*/

#ifndef FEATURE_PAL
#ifndef STRSAFE_LIB_IMPL
STRSAFE_INLINE_API StringCchGetsExA(char* pszDest, size_t cchDest, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
STRSAFE_INLINE_API StringCchGetsExW(WCHAR* pszDest, size_t cchDest, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags);
#ifdef UNICODE
#define StringCchGetsEx  StringCchGetsExW
#else
#define StringCchGetsEx  StringCchGetsExA
#endif // !UNICODE

STRSAFE_INLINE_API StringCchGetsExA(char* pszDest, size_t cchDest, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;
        
        // safe to multiply cchDest * sizeof(char) since cchDest < STRSAFE_MAX_CCH and sizeof(char) is 1
        cbDest = cchDest * sizeof(char);

        hr = StringGetsExWorkerA(pszDest, cchDest, cbDest, ppszDestEnd, pcchRemaining, dwFlags);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFE_INLINE_API StringCchGetsExW(WCHAR* pszDest, size_t cchDest, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr;

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cbDest;
        
        // safe to multiply cchDest * sizeof(WCHAR) since cchDest < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
        cbDest = cchDest * sizeof(WCHAR);

        hr = StringGetsExWorkerW(pszDest, cchDest, cbDest, ppszDestEnd, pcchRemaining, dwFlags);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // !STRSAFE_NO_CCH_FUNCTIONS
#endif  // !STRSAFE_LIB_IMPL
#endif  // !FEATURE_PAL

#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbGetsEx(LPTSTR pszDest,
                      size_t cbDest,
                      LPTSTR* ppszDestEnd,
                      size_t* pcbRemaining,
                      DWORD dwFlags);

Routine Description:

    This routine is a safer version of the C built-in function 'gets' with
    some additional parameters. In addition to functionality provided by
    StringCbGets, this routine also returns a pointer to the end of the
    destination string and the number of characters left in the destination string
    including the null terminator. The flags parameter allows additional controls.

Arguments:

    pszDest         -   destination string

    cbDest          -   size of destination buffer in bytes.
                     
    ppszDestEnd     -   if ppszDestEnd is non-null, the function will return a
                        pointer to the end of the destination string.  If the
                        function copied any data, the result will point to the
                        null termination character

    pcbRemaining    -   if pbRemaining is non-null, the function will return the
                        number of bytes left in the destination string,
                        including the null terminator

    dwFlags         -   controls some details of the string copy:

        STRSAFE_FILL_BEHIND_NULL
                    if the function succeeds, the low byte of dwFlags will be
                    used to fill the uninitialize part of destination buffer
                    behind the null terminator

        STRSAFE_IGNORE_NULLS
                    treat NULL string pointers like empty strings (TEXT("")).

        STRSAFE_FILL_ON_FAILURE
                    if the function fails, the low byte of dwFlags will be
                    used to fill all of the destination buffer, and it will
                    be null terminated.

        STRSAFE_NO_TRUNCATION /
        STRSAFE_NULL_ON_FAILURE
                    if the function fails, the destination buffer will be set
                    to the empty string. 
                    
Notes:
    pszDest should not be NULL unless the STRSAFE_IGNORE_NULLS flag is specified.
    If STRSAFE_IGNORE_NULLS is passed and pszDest is NULL, an error may still be 
    returned even though NULLS are ignored

    cbDest must be > sizeof(TCHAR) for this function to succeed

Return Value:

    S_OK        -   data was read from stdin and copied, and the resultant dest
                    string was null terminated

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all hresult falure cases

    STRSAFE_E_END_OF_FILE
                -   this return value indicates an error or end-of-file condition,
                    use feof or ferror to determine which one has occurred.

    STRSAFE_E_INSUFFICIENT_BUFFER / 
    HRESULT_CODE(hr) == ERROR_INSUFFICIENT_BUFFER
                -   this return value is an indication that there was insufficient
                    space in the destination buffer to copy any data

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function.

--*/

#ifndef FEATURE_PAL
#ifndef STRSAFE_LIB_IMPL
STRSAFE_INLINE_API StringCbGetsExA(char* pszDest, size_t cbDest, char** ppszDestEnd, size_t* pbRemaining, unsigned long dwFlags);
STRSAFE_INLINE_API StringCbGetsExW(WCHAR* pszDest, size_t cbDest, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags);
#ifdef UNICODE
#define StringCbGetsEx  StringCbGetsExW
#else
#define StringCbGetsEx  StringCbGetsExA
#endif // !UNICODE

STRSAFE_INLINE_API StringCbGetsExA(char* pszDest, size_t cbDest, char** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(char);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringGetsExWorkerA(pszDest, cchDest, cbDest, ppszDestEnd, &cchRemaining, dwFlags);
    }

    if (SUCCEEDED(hr)                           ||
        (hr == STRSAFE_E_INSUFFICIENT_BUFFER)   ||
        (hr == STRSAFE_E_END_OF_FILE))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(char) since cchRemaining < STRSAFE_MAX_CCH and sizeof(char) is 1
            *pcbRemaining = (cchRemaining * sizeof(char)) + (cbDest % sizeof(char));
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFE_INLINE_API StringCbGetsExW(WCHAR* pszDest, size_t cbDest, WCHAR** ppszDestEnd, size_t* pcbRemaining, unsigned long dwFlags)
{
    HRESULT hr;
    size_t cchDest;
    size_t cchRemaining = 0;

    cchDest = cbDest / sizeof(WCHAR);

    if (cchDest > STRSAFE_MAX_CCH)
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringGetsExWorkerW(pszDest, cchDest, cbDest, ppszDestEnd, &cchRemaining, dwFlags);
    }

    if (SUCCEEDED(hr)                           ||
        (hr == STRSAFE_E_INSUFFICIENT_BUFFER)   ||
        (hr == STRSAFE_E_END_OF_FILE))
    {
        if (pcbRemaining)
        {
            // safe to multiply cchRemaining * sizeof(WCHAR) since cchRemaining < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
            *pcbRemaining = (cchRemaining * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR));
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // !STRSAFE_NO_CB_FUNCTIONS
#endif  // !STRSAFE_LIB_IMPL
#endif  // !FEATURE_PAL

#ifndef STRSAFE_NO_CCH_FUNCTIONS
/*++

STDAPI StringCchLength(LPCTSTR psz,
                       size_t cchMax,
                       size_t* pcch);

Routine Description:

    This routine is a safer version of the C built-in function 'strlen'.
    It is used to make sure a string is not larger than a given length, and
    it optionally returns the current length in characters not including
    the null terminator.

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if the string is non-null and the length including the null terminator is
    less than or equal to cchMax characters.

Arguments:

    psz         -   string to check the length of

    cchMax      -   maximum number of characters including the null terminator
                    that psz is allowed to contain

    pcch        -   if the function succeeds and pcch is non-null, the current length
                    in characters of psz excluding the null terminator will be returned.
                    This out parameter is equivalent to the return value of strlen(psz)

Notes: 
    psz can be null but the function will fail

    cchMax should be greater than zero or the function will fail

Return Value:

    S_OK        -   psz is non-null and the length including the null terminator is
                    less than or equal to cchMax characters

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all hresult falure cases

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function.

--*/

STRSAFEAPI StringCchLengthA(const char* psz, size_t cchMax, size_t* pcch);
STRSAFEAPI StringCchLengthW(const WCHAR* psz, size_t cchMax, size_t* pcch);
#ifdef UNICODE
#define StringCchLength  StringCchLengthW
#else
#define StringCchLength  StringCchLengthA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCchLengthA(const char* psz, size_t cchMax, size_t* pcch)
{
    HRESULT hr;
    
    if ((psz == NULL) || (cchMax > STRSAFE_MAX_CCH))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringLengthWorkerA(psz, cchMax, pcch);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCchLengthW(const WCHAR* psz, size_t cchMax, size_t* pcch)
{
    HRESULT hr;

    if ((psz == NULL) || (cchMax > STRSAFE_MAX_CCH))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringLengthWorkerW(psz, cchMax, pcch);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CCH_FUNCTIONS


#ifndef STRSAFE_NO_CB_FUNCTIONS
/*++

STDAPI StringCbLength(LPCTSTR psz,
                      size_t cbMax,
                      size_t* pcb);

Routine Description:

    This routine is a safer version of the C built-in function 'strlen'.
    It is used to make sure a string is not larger than a given length, and
    it optionally returns the current length in bytes not including 
    the null terminator.

    This function returns a hresult, and not a pointer.  It returns a S_OK
    if the string is non-null and the length including the null terminator is
    less than or equal to cbMax bytes.

Arguments:

    psz         -   string to check the length of

    cbMax       -   maximum number of bytes including the null terminator
                    that psz is allowed to contain

    pcb         -   if the function succeeds and pcb is non-null, the current length
                    in bytes of psz excluding the null terminator will be returned.
                    This out parameter is equivalent to the return value of strlen(psz) * sizeof(TCHAR)

Notes: 
    psz can be null but the function will fail

    cbMax should be greater than or equal to sizeof(TCHAR) or the function will fail

Return Value:

    S_OK        -   psz is non-null and the length including the null terminator is
                    less than or equal to cbMax bytes

    failure     -   you can use the macro HRESULT_CODE() to get a win32 error
                    code for all hresult falure cases

    It is strongly recommended to use the SUCCEEDED() / FAILED() macros to test the
    return value of this function.

--*/

STRSAFEAPI StringCbLengthA(const char* psz, size_t cchMax, size_t* pcch);
STRSAFEAPI StringCbLengthW(const WCHAR* psz, size_t cchMax, size_t* pcch);
#ifdef UNICODE
#define StringCbLength  StringCbLengthW
#else
#define StringCbLength  StringCbLengthA
#endif // !UNICODE

#ifdef STRSAFE_INLINE
STRSAFEAPI StringCbLengthA(const char* psz, size_t cbMax, size_t* pcb)
{
    HRESULT hr;
    size_t cchMax;
    size_t cch = 0;

    cchMax = cbMax / sizeof(char);

    if ((psz == NULL) || (cchMax > STRSAFE_MAX_CCH))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringLengthWorkerA(psz, cchMax, &cch);
    }

    if (SUCCEEDED(hr) && pcb)
    {
        // safe to multiply cch * sizeof(char) since cch < STRSAFE_MAX_CCH and sizeof(char) is 1
        *pcb = cch * sizeof(char);
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCbLengthW(const WCHAR* psz, size_t cbMax, size_t* pcb)
{
    HRESULT hr;
    size_t cchMax;
    size_t cch = 0;

    cchMax = cbMax / sizeof(WCHAR);

    if ((psz == NULL) || (cchMax > STRSAFE_MAX_CCH))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        hr = StringLengthWorkerW(psz, cchMax, &cch);
    }

    if (SUCCEEDED(hr) && pcb)
    {
        // safe to multiply cch * sizeof(WCHAR) since cch < STRSAFE_MAX_CCH and sizeof(WCHAR) is 2
        *pcb = cch * sizeof(WCHAR);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE
#endif  // !STRSAFE_NO_CB_FUNCTIONS


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

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
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
#endif // FEATURE_PAL || !PLATFORM_UNIX

STRSAFEAPI StringCopyExWorkerA(char* pszDest, size_t cchDest, size_t cbDest, const char* pszSrc, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr = S_OK;
    char* pszDestEnd = pszDest;
    size_t cchRemaining = 0;

    // ASSERT(cbDest == (cchDest * sizeof(char))    ||
    //        cbDest == (cchDest * sizeof(char)) + (cbDest % sizeof(char)));
 
    // only accept valid flags
    if (dwFlags & (~STRSAFE_VALID_FLAGS))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        if (dwFlags & STRSAFE_IGNORE_NULLS)
        {
            if (pszDest == NULL)
            {
                if ((cchDest != 0) || (cbDest != 0))
                {
                    // NULL pszDest and non-zero cchDest/cbDest is invalid
                    hr = STRSAFE_E_INVALID_PARAMETER;
                }
            }

            if (pszSrc == NULL)
            {
                pszSrc = "";
            }
        }

        if (SUCCEEDED(hr))
        {
            if (cchDest == 0)
            {
                pszDestEnd = pszDest;
                cchRemaining = 0;

                // only fail if there was actually src data to copy
                if (*pszSrc != '\0')
                {
                    if (pszDest == NULL)
                    {
                        hr = STRSAFE_E_INVALID_PARAMETER;
                    }
                    else
                    {
                        hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                    }
                }
            }
            else
            {
                pszDestEnd = pszDest;
                cchRemaining = cchDest;

                while (cchRemaining && (*pszSrc != '\0'))
                {
                    *pszDestEnd++= *pszSrc++;
                    cchRemaining--;
                }
    
                if (cchRemaining > 0)
                {
                    if (dwFlags & STRSAFE_FILL_BEHIND_NULL)
                    {
                        memset(pszDestEnd + 1, STRSAFE_GET_FILL_PATTERN(dwFlags), ((cchRemaining - 1) * sizeof(char)) + (cbDest % sizeof(char)));
                    }
                }
                else
                {
                    // we are going to truncate pszDest
                    pszDestEnd--;
                    cchRemaining++;

                    hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                }

                *pszDestEnd = '\0';
            }
        }
    }

    if (FAILED(hr))
    {
        if (pszDest)
        {
            if (dwFlags & STRSAFE_FILL_ON_FAILURE)
            {
                memset(pszDest, STRSAFE_GET_FILL_PATTERN(dwFlags), cbDest);
            
                if (STRSAFE_GET_FILL_PATTERN(dwFlags) == 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;
                }
                else if (cchDest > 0)
                {
                    pszDestEnd = pszDest + cchDest - 1;
                    cchRemaining = 1;

                    // null terminate the end of the string
                    *pszDestEnd = '\0';
                }
            }

            if (dwFlags & (STRSAFE_NULL_ON_FAILURE | STRSAFE_NO_TRUNCATION))
            {
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;

                    // null terminate the beginning of the string
                    *pszDestEnd = '\0';
                }
            }
        }
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (ppszDestEnd) 
        {
            *ppszDestEnd = pszDestEnd;
        }

        if (pcchRemaining)
        {
            *pcchRemaining = cchRemaining;
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCopyExWorkerW(WCHAR* pszDest, size_t cchDest, size_t cbDest, const WCHAR* pszSrc, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr = S_OK;
    WCHAR* pszDestEnd = pszDest;
    size_t cchRemaining = 0;

    // ASSERT(cbDest == (cchDest * sizeof(WCHAR)) ||
    //        cbDest == (cchDest * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR)));
 
    // only accept valid flags
    if (dwFlags & (~STRSAFE_VALID_FLAGS))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        if (dwFlags & STRSAFE_IGNORE_NULLS)
        {
            if (pszDest == NULL)
            {
                if ((cchDest != 0) || (cbDest != 0))
                {
                    // NULL pszDest and non-zero cchDest/cbDest is invalid
                    hr = STRSAFE_E_INVALID_PARAMETER;
                }
            }

            if (pszSrc == NULL)
            {
                pszSrc = u"";
            }
        }

        if (SUCCEEDED(hr))
        {
            if (cchDest == 0)
            {
                pszDestEnd = pszDest;
                cchRemaining = 0;

                // only fail if there was actually src data to copy
                if (*pszSrc != u'\0')
                {
                    if (pszDest == NULL)
                    {
                        hr = STRSAFE_E_INVALID_PARAMETER;
                    }
                    else
                    {
                        hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                    }
                }
            }
            else
            {
                pszDestEnd = pszDest;
                cchRemaining = cchDest;

                while (cchRemaining && (*pszSrc != u'\0'))
                {
                    *pszDestEnd++= *pszSrc++;
                    cchRemaining--;
                }
    
                if (cchRemaining > 0)
                {
                    if (dwFlags & STRSAFE_FILL_BEHIND_NULL)
                    {
                        memset(pszDestEnd + 1, STRSAFE_GET_FILL_PATTERN(dwFlags), ((cchRemaining - 1) * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR)));
                    }
                }
                else
                {
                    // we are going to truncate pszDest
                    pszDestEnd--;
                    cchRemaining++;

                    hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                }

                *pszDestEnd = u'\0';
            }
        }
    }

    if (FAILED(hr))
    {
        if (pszDest)
        {
            if (dwFlags & STRSAFE_FILL_ON_FAILURE)
            {
                memset(pszDest, STRSAFE_GET_FILL_PATTERN(dwFlags), cbDest);
                           
                if (STRSAFE_GET_FILL_PATTERN(dwFlags) == 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;
                }
                else if (cchDest > 0)
                {
                    pszDestEnd = pszDest + cchDest - 1;
                    cchRemaining = 1;

                    // null terminate the end of the string
                    *pszDestEnd = L'\0';
                }
            }

            if (dwFlags & (STRSAFE_NULL_ON_FAILURE | STRSAFE_NO_TRUNCATION))
            {
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;

                    // null terminate the beginning of the string
                    *pszDestEnd = L'\0';
                }
            }
        }
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (ppszDestEnd) 
        {
            *ppszDestEnd = pszDestEnd;
        }

        if (pcchRemaining)
        {
            *pcchRemaining = cchRemaining;
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX

STRSAFEAPI StringCopyNWorkerA(char* pszDest, size_t cchDest, const char* pszSrc, size_t cchSrc)
{
    HRESULT hr = S_OK;

    if (cchDest == 0)
    {
        // can not null terminate a zero-byte dest buffer
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        while (cchDest && cchSrc && (*pszSrc != '\0'))
        {
            *pszDest++= *pszSrc++;
            cchDest--;
            cchSrc--;
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

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCopyNWorkerW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, size_t cchSrc)
{
    HRESULT hr = S_OK;

    if (cchDest == 0)
    {
        // can not null terminate a zero-byte dest buffer
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        while (cchDest && cchSrc && (*pszSrc != L'\0'))
        {
            *pszDest++= *pszSrc++;
            cchDest--;
            cchSrc--;
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
#endif // FEATURE_PAL || !PLATFORM_UNIX

STRSAFEAPI StringCopyNExWorkerA(char* pszDest, size_t cchDest, size_t cbDest, const char* pszSrc, size_t cchSrc, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr = S_OK;
    char* pszDestEnd = pszDest;
    size_t cchRemaining = 0;

    // ASSERT(cbDest == (cchDest * sizeof(char))    ||
    //        cbDest == (cchDest * sizeof(char)) + (cbDest % sizeof(char)));
 
    // only accept valid flags
    if (dwFlags & (~STRSAFE_VALID_FLAGS))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        if (dwFlags & STRSAFE_IGNORE_NULLS)
        {
            if (pszDest == NULL)
            {
                if ((cchDest != 0) || (cbDest != 0))
                {
                    // NULL pszDest and non-zero cchDest/cbDest is invalid
                    hr = STRSAFE_E_INVALID_PARAMETER;
                }
            }

            if (pszSrc == NULL)
            {
                pszSrc = "";
            }
        }

        if (SUCCEEDED(hr))
        {
            if (cchDest == 0)
            {
                pszDestEnd = pszDest;
                cchRemaining = 0;

                // only fail if there was actually src data to copy
                if (*pszSrc != '\0')
                {
                    if (pszDest == NULL)
                    {
                        hr = STRSAFE_E_INVALID_PARAMETER;
                    }
                    else
                    {
                        hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                    }
                }
            }
            else
            {
                pszDestEnd = pszDest;
                cchRemaining = cchDest;

                while (cchRemaining && cchSrc && (*pszSrc != '\0'))
                {
                    *pszDestEnd++= *pszSrc++;
                    cchRemaining--;
                    cchSrc--;
                }
    
                if (cchRemaining > 0)
                {
                    if (dwFlags & STRSAFE_FILL_BEHIND_NULL)
                    {
                        memset(pszDestEnd + 1, STRSAFE_GET_FILL_PATTERN(dwFlags), ((cchRemaining - 1) * sizeof(char)) + (cbDest % sizeof(char)));
                    }
                }
                else
                {
                    // we are going to truncate pszDest
                    pszDestEnd--;
                    cchRemaining++;

                    hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                }

                *pszDestEnd = '\0';
            }
        }
    }

    if (FAILED(hr))
    {
        if (pszDest)
        {
            if (dwFlags & STRSAFE_FILL_ON_FAILURE)
            {
                memset(pszDest, STRSAFE_GET_FILL_PATTERN(dwFlags), cbDest);
            
                if (STRSAFE_GET_FILL_PATTERN(dwFlags) == 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;
                }
                else if (cchDest > 0)
                {
                    pszDestEnd = pszDest + cchDest - 1;
                    cchRemaining = 1;

                    // null terminate the end of the string
                    *pszDestEnd = '\0';
                }
            }

            if (dwFlags & (STRSAFE_NULL_ON_FAILURE | STRSAFE_NO_TRUNCATION))
            {
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;

                    // null terminate the beginning of the string
                    *pszDestEnd = '\0';
                }
            }
        }
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (ppszDestEnd) 
        {
            *ppszDestEnd = pszDestEnd;
        }

        if (pcchRemaining)
        {
            *pcchRemaining = cchRemaining;
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCopyNExWorkerW(WCHAR* pszDest, size_t cchDest, size_t cbDest, const WCHAR* pszSrc, size_t cchSrc, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr = S_OK;
    WCHAR* pszDestEnd = pszDest;
    size_t cchRemaining = 0;

    // ASSERT(cbDest == (cchDest * sizeof(WCHAR)) ||
    //        cbDest == (cchDest * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR)));
 
    // only accept valid flags
    if (dwFlags & (~STRSAFE_VALID_FLAGS))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        if (dwFlags & STRSAFE_IGNORE_NULLS)
        {
            if (pszDest == NULL)
            {
                if ((cchDest != 0) || (cbDest != 0))
                {
                    // NULL pszDest and non-zero cchDest/cbDest is invalid
                    hr = STRSAFE_E_INVALID_PARAMETER;
                }
            }

            if (pszSrc == NULL)
            {
                pszSrc = u"";
            }
        }

        if (SUCCEEDED(hr))
        {
            if (cchDest == 0)
            {
                pszDestEnd = pszDest;
                cchRemaining = 0;

                // only fail if there was actually src data to copy
                if (*pszSrc != L'\0')
                {
                    if (pszDest == NULL)
                    {
                        hr = STRSAFE_E_INVALID_PARAMETER;
                    }
                    else
                    {
                        hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                    }
                }
            }
            else
            {
                pszDestEnd = pszDest;
                cchRemaining = cchDest;

                while (cchRemaining && cchSrc && (*pszSrc != L'\0'))
                {
                    *pszDestEnd++= *pszSrc++;
                    cchRemaining--;
                    cchSrc--;
                }
    
                if (cchRemaining > 0)
                {
                    if (dwFlags & STRSAFE_FILL_BEHIND_NULL)
                    {
                        memset(pszDestEnd + 1, STRSAFE_GET_FILL_PATTERN(dwFlags), ((cchRemaining - 1) * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR)));
                    }
                }
                else
                {
                    // we are going to truncate pszDest
                    pszDestEnd--;
                    cchRemaining++;

                    hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                }

                *pszDestEnd = L'\0';
            }
        }
    }

    if (FAILED(hr))
    {
        if (pszDest)
        {
            if (dwFlags & STRSAFE_FILL_ON_FAILURE)
            {
                memset(pszDest, STRSAFE_GET_FILL_PATTERN(dwFlags), cbDest);
            
                if (STRSAFE_GET_FILL_PATTERN(dwFlags) == 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;
                }
                else if (cchDest > 0)
                {
                    pszDestEnd = pszDest + cchDest - 1;
                    cchRemaining = 1;

                    // null terminate the end of the string
                    *pszDestEnd = L'\0';
                }
            }

            if (dwFlags & (STRSAFE_NULL_ON_FAILURE | STRSAFE_NO_TRUNCATION))
            {
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;

                    // null terminate the beginning of the string
                    *pszDestEnd = L'\0';
                }
            }
        }
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (ppszDestEnd) 
        {
            *ppszDestEnd = pszDestEnd;
        }

        if (pcchRemaining)
        {
            *pcchRemaining = cchRemaining;
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX

STRSAFEAPI StringCatWorkerA(char* pszDest, size_t cchDest, const char* pszSrc)
{
   HRESULT hr;
   size_t cchDestCurrent;

   hr = StringLengthWorkerA(pszDest, cchDest, &cchDestCurrent);

   if (SUCCEEDED(hr))
   {
       hr = StringCopyWorkerA(pszDest + cchDestCurrent,
                              cchDest - cchDestCurrent,
                              pszSrc);
   }

   return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCatWorkerW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc)
{
   HRESULT hr;
   size_t cchDestCurrent;

   hr = StringLengthWorkerW(pszDest, cchDest, &cchDestCurrent);

   if (SUCCEEDED(hr))
   {
       hr = StringCopyWorkerW(pszDest + cchDestCurrent,
                              cchDest - cchDestCurrent,
                              pszSrc);
   }

   return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX

STRSAFEAPI StringCatExWorkerA(char* pszDest, size_t cchDest, size_t cbDest, const char* pszSrc, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr = S_OK;
    char* pszDestEnd = pszDest;
    size_t cchRemaining = 0;

    // ASSERT(cbDest == (cchDest * sizeof(char))    ||
    //        cbDest == (cchDest * sizeof(char)) + (cbDest % sizeof(char)));

    // only accept valid flags
    if (dwFlags & (~STRSAFE_VALID_FLAGS))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cchDestCurrent;

        if (dwFlags & STRSAFE_IGNORE_NULLS)
        {
            if (pszDest == NULL)
            {
                if ((cchDest == 0) && (cbDest == 0))
                {
                    cchDestCurrent = 0;
                }
                else
                {
                    // NULL pszDest and non-zero cchDest/cbDest is invalid
                    hr = STRSAFE_E_INVALID_PARAMETER;
                }
            }
            else
            {
                hr = StringLengthWorkerA(pszDest, cchDest, &cchDestCurrent);

                if (SUCCEEDED(hr))
                {
                    pszDestEnd = pszDest + cchDestCurrent;
                    cchRemaining = cchDest - cchDestCurrent;
                }
            }

            if (pszSrc == NULL)
            {
                pszSrc = "";
            }
        }
        else
        {
            hr = StringLengthWorkerA(pszDest, cchDest, &cchDestCurrent);

            if (SUCCEEDED(hr))
            {
                pszDestEnd = pszDest + cchDestCurrent;
                cchRemaining = cchDest - cchDestCurrent;
            }
        }

        if (SUCCEEDED(hr))
        {
            if (cchDest == 0)
            {
                // only fail if there was actually src data to append
                if (*pszSrc != '\0')
                {
                    if (pszDest == NULL)
                    {
                        hr = STRSAFE_E_INVALID_PARAMETER;
                    }
                    else
                    {
                        hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                    }
                }
            }
            else
            {
                // we handle the STRSAFE_FILL_ON_FAILURE and STRSAFE_NULL_ON_FAILURE cases below, so do not pass
                // those flags through
                hr = StringCopyExWorkerA(pszDestEnd,
                                         cchRemaining,
                                         (cchRemaining * sizeof(char)) + (cbDest % sizeof(char)),
                                         pszSrc,
                                         &pszDestEnd,
                                         &cchRemaining,
                                         dwFlags & (~(STRSAFE_FILL_ON_FAILURE | STRSAFE_NULL_ON_FAILURE)));
            }
        }
    }
    
    if (FAILED(hr))
    {
        if (pszDest)
        {
            // STRSAFE_NO_TRUNCATION is taken care of by StringCopyExWorkerA()

            if (dwFlags & STRSAFE_FILL_ON_FAILURE)
            {
                memset(pszDest, STRSAFE_GET_FILL_PATTERN(dwFlags), cbDest);

                if (STRSAFE_GET_FILL_PATTERN(dwFlags) == 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;
                }
                else
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest + cchDest - 1;
                    cchRemaining = 1;

                    // null terminate the end of the string
                    *pszDestEnd = '\0';
                }
            }

            if (dwFlags & STRSAFE_NULL_ON_FAILURE)
            {
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;

                    // null terminate the beginning of the string
                    *pszDestEnd = '\0';
                }
            }
        }
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (ppszDestEnd) 
        {
            *ppszDestEnd = pszDestEnd;
        }

        if (pcchRemaining)
        {
            *pcchRemaining = cchRemaining;
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCatExWorkerW(WCHAR* pszDest, size_t cchDest, size_t cbDest, const WCHAR* pszSrc, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr = S_OK;
    WCHAR* pszDestEnd = pszDest;
    size_t cchRemaining = 0;

    // ASSERT(cbDest == (cchDest * sizeof(WCHAR)) ||
    //        cbDest == (cchDest * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR)));

    // only accept valid flags
    if (dwFlags & (~STRSAFE_VALID_FLAGS))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        size_t cchDestCurrent;

        if (dwFlags & STRSAFE_IGNORE_NULLS)
        {
            if (pszDest == NULL)
            {
                if ((cchDest == 0) && (cbDest == 0))
                {
                    cchDestCurrent = 0;
                }
                else
                {
                    // NULL pszDest and non-zero cchDest/cbDest is invalid
                    hr = STRSAFE_E_INVALID_PARAMETER;
                }
            }
            else
            {
                hr = StringLengthWorkerW(pszDest, cchDest, &cchDestCurrent);

                if (SUCCEEDED(hr))
                {
                    pszDestEnd = pszDest + cchDestCurrent;
                    cchRemaining = cchDest - cchDestCurrent;
                }
            }

            if (pszSrc == NULL)
            {
                pszSrc = u"";
            }
        }
        else
        {
            hr = StringLengthWorkerW(pszDest, cchDest, &cchDestCurrent);

            if (SUCCEEDED(hr))
            {
                pszDestEnd = pszDest + cchDestCurrent;
                cchRemaining = cchDest - cchDestCurrent;
            }
        }

        if (SUCCEEDED(hr))
        {
            if (cchDest == 0)
            {
                // only fail if there was actually src data to append
                if (*pszSrc != L'\0')
                {
                    if (pszDest == NULL)
                    {
                        hr = STRSAFE_E_INVALID_PARAMETER;
                    }
                    else
                    {
                        hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                    }
                }
            }
            else
            {
                // we handle the STRSAFE_FILL_ON_FAILURE and STRSAFE_NULL_ON_FAILURE cases below, so do not pass
                // those flags through
                hr = StringCopyExWorkerW(pszDestEnd,
                                         cchRemaining,
                                         (cchRemaining * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR)),
                                         pszSrc,
                                         &pszDestEnd,
                                         &cchRemaining,
                                         dwFlags & (~(STRSAFE_FILL_ON_FAILURE | STRSAFE_NULL_ON_FAILURE)));            
            }
        }
    }

    if (FAILED(hr))
    {
        if (pszDest)
        {
            // STRSAFE_NO_TRUNCATION is taken care of by StringCopyExWorkerW()

            if (dwFlags & STRSAFE_FILL_ON_FAILURE)
            {
                memset(pszDest, STRSAFE_GET_FILL_PATTERN(dwFlags), cbDest);
            
                if (STRSAFE_GET_FILL_PATTERN(dwFlags) == 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;
                }
                else if (cchDest > 0)
                {
                    pszDestEnd = pszDest + cchDest - 1;
                    cchRemaining = 1;

                    // null terminate the end of the string
                    *pszDestEnd = L'\0';
                }
            }

            if (dwFlags & STRSAFE_NULL_ON_FAILURE)
            {
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;

                    // null terminate the beginning of the string
                    *pszDestEnd = L'\0';
                }
            }
        }
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (ppszDestEnd) 
        {
            *ppszDestEnd = pszDestEnd;
        }

        if (pcchRemaining)
        {
            *pcchRemaining = cchRemaining;
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX

STRSAFEAPI StringCatNWorkerA(char* pszDest, size_t cchDest, const char* pszSrc, size_t cchMaxAppend)
{
    HRESULT hr;
    size_t cchDestCurrent;

    hr = StringLengthWorkerA(pszDest, cchDest, &cchDestCurrent);

    if (SUCCEEDED(hr))
    {
        hr = StringCopyNWorkerA(pszDest + cchDestCurrent,
                                cchDest - cchDestCurrent,
                                pszSrc,
                                cchMaxAppend);
    }    

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCatNWorkerW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszSrc, size_t cchMaxAppend)
{
    HRESULT hr;
    size_t cchDestCurrent;

    hr = StringLengthWorkerW(pszDest, cchDest, &cchDestCurrent);

    if (SUCCEEDED(hr))
    {
        hr = StringCopyNWorkerW(pszDest + cchDestCurrent,
                                cchDest - cchDestCurrent,
                                pszSrc,
                                cchMaxAppend);
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX

STRSAFEAPI StringCatNExWorkerA(char* pszDest, size_t cchDest, size_t cbDest, const char* pszSrc, size_t cchMaxAppend, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr = S_OK;
    char* pszDestEnd = pszDest;
    size_t cchRemaining = 0;
    size_t cchDestCurrent = 0;

    // ASSERT(cbDest == (cchDest * sizeof(char))    ||
    //        cbDest == (cchDest * sizeof(char)) + (cbDest % sizeof(char)));

    // only accept valid flags
    if (dwFlags & (~STRSAFE_VALID_FLAGS))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        if (dwFlags & STRSAFE_IGNORE_NULLS)
        {
            if (pszDest == NULL)
            {
                if ((cchDest == 0) && (cbDest == 0))
                {
                    cchDestCurrent = 0;
                }
                else
                {
                    // NULL pszDest and non-zero cchDest/cbDest is invalid
                    hr = STRSAFE_E_INVALID_PARAMETER;
                }
            }
            else
            {
                hr = StringLengthWorkerA(pszDest, cchDest, &cchDestCurrent);

                if (SUCCEEDED(hr))
                {
                    pszDestEnd = pszDest + cchDestCurrent;
                    cchRemaining = cchDest - cchDestCurrent;
                }
            }

            if (pszSrc == NULL)
            {
                pszSrc = "";
            }
        }
        else
        {
            hr = StringLengthWorkerA(pszDest, cchDest, &cchDestCurrent);

            if (SUCCEEDED(hr))
            {
                pszDestEnd = pszDest + cchDestCurrent;
                cchRemaining = cchDest - cchDestCurrent;
            }
        }

        if (SUCCEEDED(hr))
        {
            if (cchDest == 0)
            {
                // only fail if there was actually src data to append
                if (*pszSrc != '\0')
                {
                    if (pszDest == NULL)
                    {
                        hr = STRSAFE_E_INVALID_PARAMETER;
                    }
                    else
                    {
                        hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                    }
                }
            }
            else
            {
                // we handle the STRSAFE_FILL_ON_FAILURE and STRSAFE_NULL_ON_FAILURE cases below, so do not pass
                // those flags through
                hr = StringCopyNExWorkerA(pszDestEnd,
                                          cchRemaining,
                                          (cchRemaining * sizeof(char)) + (cbDest % sizeof(char)),
                                          pszSrc,
                                          cchMaxAppend,
                                          &pszDestEnd,
                                          &cchRemaining,
                                          dwFlags & (~(STRSAFE_FILL_ON_FAILURE | STRSAFE_NULL_ON_FAILURE)));
            }
        }
    }

    if (FAILED(hr))
    {
        if (pszDest)
        {
            // STRSAFE_NO_TRUNCATION is taken care of by StringCopyNExWorkerA()

            if (dwFlags & STRSAFE_FILL_ON_FAILURE)
            {
                memset(pszDest, STRSAFE_GET_FILL_PATTERN(dwFlags), cbDest);

                if (STRSAFE_GET_FILL_PATTERN(dwFlags) == 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;
                }
                else if (cchDest > 0)
                {
                    pszDestEnd = pszDest + cchDest - 1;
                    cchRemaining = 1;

                    // null terminate the end of the string
                    *pszDestEnd = '\0';
                }
            }

            if (dwFlags & (STRSAFE_NULL_ON_FAILURE))
            {
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;

                    // null terminate the beginning of the string
                    *pszDestEnd = '\0';
                }
            }
        }
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (ppszDestEnd)
        {
            *ppszDestEnd = pszDestEnd;
        }

        if (pcchRemaining)
        {
            *pcchRemaining = cchRemaining;
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringCatNExWorkerW(WCHAR* pszDest, size_t cchDest, size_t cbDest, const WCHAR* pszSrc, size_t cchMaxAppend, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr = S_OK;
    WCHAR* pszDestEnd = pszDest;
    size_t cchRemaining = 0;
    size_t cchDestCurrent = 0;


    // ASSERT(cbDest == (cchDest * sizeof(WCHAR)) ||
    //        cbDest == (cchDest * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR)));

    // only accept valid flags
    if (dwFlags & (~STRSAFE_VALID_FLAGS))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        if (dwFlags & STRSAFE_IGNORE_NULLS)
        {
            if (pszDest == NULL)
            {
                if ((cchDest == 0) && (cbDest == 0))
                {
                    cchDestCurrent = 0;
                }
                else
                {
                    // NULL pszDest and non-zero cchDest/cbDest is invalid
                    hr = STRSAFE_E_INVALID_PARAMETER;
                }
            }
            else
            {
                hr = StringLengthWorkerW(pszDest, cchDest, &cchDestCurrent);

                if (SUCCEEDED(hr))
                {
                    pszDestEnd = pszDest + cchDestCurrent;
                    cchRemaining = cchDest - cchDestCurrent;
                }
            }

            if (pszSrc == NULL)
            {
                pszSrc = u"";
            }
        }
        else
        {
            hr = StringLengthWorkerW(pszDest, cchDest, &cchDestCurrent);

            if (SUCCEEDED(hr))
            {
                pszDestEnd = pszDest + cchDestCurrent;
                cchRemaining = cchDest - cchDestCurrent;
            }
        }

        if (SUCCEEDED(hr))
        {
            if (cchDest == 0)
            {
                // only fail if there was actually src data to append
                if (*pszSrc != L'\0')
                {
                    if (pszDest == NULL)
                    {
                        hr = STRSAFE_E_INVALID_PARAMETER;
                    }
                    else
                    {
                        hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                    }
                }
            }
            else
            {
                // we handle the STRSAFE_FILL_ON_FAILURE and STRSAFE_NULL_ON_FAILURE cases below, so do not pass
                // those flags through
                hr = StringCopyNExWorkerW(pszDestEnd,
                                          cchRemaining,
                                          (cchRemaining * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR)),
                                          pszSrc,
                                          cchMaxAppend,
                                          &pszDestEnd,
                                          &cchRemaining,
                                          dwFlags & (~(STRSAFE_FILL_ON_FAILURE | STRSAFE_NULL_ON_FAILURE)));
            }
        }
    }

    if (FAILED(hr))
    {
        if (pszDest)
        {
            // STRSAFE_NO_TRUNCATION is taken care of by StringCopyNExWorkerW()

            if (dwFlags & STRSAFE_FILL_ON_FAILURE)
            {
                memset(pszDest, STRSAFE_GET_FILL_PATTERN(dwFlags), cbDest);

                if (STRSAFE_GET_FILL_PATTERN(dwFlags) == 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;
                }
                else if (cchDest > 0)
                {
                    pszDestEnd = pszDest + cchDest - 1;
                    cchRemaining = 1;

                    // null terminate the end of the string
                    *pszDestEnd = L'\0';
                }
            }

            if (dwFlags & (STRSAFE_NULL_ON_FAILURE))
            {
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;

                    // null terminate the beginning of the string
                    *pszDestEnd = L'\0';
                }
            }
        }
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (ppszDestEnd)
        {
            *ppszDestEnd = pszDestEnd;
        }

        if (pcchRemaining)
        {
            *pcchRemaining = cchRemaining;
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX

STRSAFEAPI StringVPrintfWorkerA(char* pszDest, size_t cchDest, const char* pszFormat, va_list argList)
{
    HRESULT hr = S_OK;

    if (cchDest == 0)
    {
        // can not null terminate a zero-byte dest buffer
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        int iRet;
        size_t cchMax;

        // leave the last space for the null terminator
        cchMax = cchDest - 1;

        iRet = _vsnprintf(pszDest, cchMax, pszFormat, argList);
        // ASSERT((iRet < 0) || (((size_t)iRet) <= cchMax));

        if ((iRet < 0) || (((size_t)iRet) > cchMax))
        {
            // need to null terminate the string
            pszDest += cchMax;
            *pszDest = '\0';

            // we have truncated pszDest
            hr = STRSAFE_E_INSUFFICIENT_BUFFER;
        }
        else if (((size_t)iRet) == cchMax)
        {
            // need to null terminate the string
            pszDest += cchMax;
            *pszDest = '\0';
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringVPrintfWorkerW(WCHAR* pszDest, size_t cchDest, const WCHAR* pszFormat, va_list argList)
{
    HRESULT hr = S_OK;

    if (cchDest == 0)
    {
        // can not null terminate a zero-byte dest buffer
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else    
    {
        int iRet;
        size_t cchMax;

        // leave the last space for the null terminator
        cchMax = cchDest - 1;

        iRet = _vsnwprintf(pszDest, cchMax, pszFormat, argList);
        // ASSERT((iRet < 0) || (((size_t)iRet) <= cchMax));

        if ((iRet < 0) || (((size_t)iRet) > cchMax))
        {
            // need to null terminate the string
            pszDest += cchMax;
            *pszDest = L'\0';

            // we have truncated pszDest
            hr = STRSAFE_E_INSUFFICIENT_BUFFER;
        }
        else if (((size_t)iRet) == cchMax)
        {
            // need to null terminate the string
            pszDest += cchMax;
            *pszDest = L'\0';
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX

STRSAFEAPI StringVPrintfExWorkerA(char* pszDest, size_t cchDest, size_t cbDest, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags, const char* pszFormat, va_list argList)
{
    HRESULT hr = S_OK;
    char* pszDestEnd = pszDest;
    size_t cchRemaining = 0;

    // ASSERT(cbDest == (cchDest * sizeof(char))    ||
    //        cbDest == (cchDest * sizeof(char)) + (cbDest % sizeof(char)));

    // only accept valid flags
    if (dwFlags & (~STRSAFE_VALID_FLAGS))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        if (dwFlags & STRSAFE_IGNORE_NULLS)
        {
            if (pszDest == NULL)
            {
                if ((cchDest != 0) || (cbDest != 0))
                {
                    // NULL pszDest and non-zero cchDest/cbDest is invalid
                    hr = STRSAFE_E_INVALID_PARAMETER;
                }
            }

            if (pszFormat == NULL)
            {
                pszFormat = "";
            }
        }
    
        if (SUCCEEDED(hr))
        {
            if (cchDest == 0)
            {
                pszDestEnd = pszDest;
                cchRemaining = 0;

                // only fail if there was actually a non-empty format string
                if (*pszFormat != '\0')
                {
                    if (pszDest == NULL)
                    {
                        hr = STRSAFE_E_INVALID_PARAMETER;
                    }
                    else
                    {
                        hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                    }
                }
            }
            else
            {
                int iRet;
                size_t cchMax;

                // leave the last space for the null terminator
                cchMax = cchDest - 1;

                iRet = _vsnprintf(pszDest, cchMax, pszFormat, argList);
                // ASSERT((iRet < 0) || (((size_t)iRet) <= cchMax));

                if ((iRet < 0) || (((size_t)iRet) > cchMax))
                {
                    // we have truncated pszDest
                    pszDestEnd = pszDest + cchMax;
                    cchRemaining = 1;

                    // need to null terminate the string
                    *pszDestEnd = '\0';

                    hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                }
                else if (((size_t)iRet) == cchMax)
                {
                    // string fit perfectly
                    pszDestEnd = pszDest + cchMax;
                    cchRemaining = 1;

                    // need to null terminate the string
                    *pszDestEnd = '\0';
                }
                else if (((size_t)iRet) < cchMax)
                {
                    // there is extra room
                    pszDestEnd = pszDest + iRet;
                    cchRemaining = cchDest - iRet;

                    if (dwFlags & STRSAFE_FILL_BEHIND_NULL)
                    {
                        memset(pszDestEnd + 1, STRSAFE_GET_FILL_PATTERN(dwFlags), ((cchRemaining - 1) * sizeof(char)) + (cbDest % sizeof(char)));
                    }
                }
            }
        }
    }

    if (FAILED(hr))
    {
        if (pszDest)
        {
            if (dwFlags & STRSAFE_FILL_ON_FAILURE)
            {
                memset(pszDest, STRSAFE_GET_FILL_PATTERN(dwFlags), cbDest);

                if (STRSAFE_GET_FILL_PATTERN(dwFlags) == 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;
                }
                else if (cchDest > 0)
                {
                    pszDestEnd = pszDest + cchDest - 1;
                    cchRemaining = 1;

                    // null terminate the end of the string
                    *pszDestEnd = '\0';
                }
            }

            if (dwFlags & (STRSAFE_NULL_ON_FAILURE | STRSAFE_NO_TRUNCATION))
            {
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;

                    // null terminate the beginning of the string
                    *pszDestEnd = '\0';
                }
            }
        }
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (ppszDestEnd) 
        {
            *ppszDestEnd = pszDestEnd;
        }

        if (pcchRemaining)
        {
            *pcchRemaining = cchRemaining;
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringVPrintfExWorkerW(WCHAR* pszDest, size_t cchDest, size_t cbDest, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags, const WCHAR* pszFormat, va_list argList)
{
    HRESULT hr = S_OK;
    WCHAR* pszDestEnd = pszDest;
    size_t cchRemaining = 0;

    // ASSERT(cbDest == (cchDest * sizeof(WCHAR)) ||
    //        cbDest == (cchDest * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR)));

    // only accept valid flags
    if (dwFlags & (~STRSAFE_VALID_FLAGS))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        if (dwFlags & STRSAFE_IGNORE_NULLS)
        {
            if (pszDest == NULL)
            {
                if ((cchDest != 0) || (cbDest != 0))
                {
                    // NULL pszDest and non-zero cchDest/cbDest is invalid
                    hr = STRSAFE_E_INVALID_PARAMETER;
                }
            }

            if (pszFormat == NULL)
            {
                pszFormat = u"";
            }
        }

        if (SUCCEEDED(hr))
        {
            if (cchDest == 0)
            {
                pszDestEnd = pszDest;
                cchRemaining = 0;

                // only fail if there was actually a non-empty format string
                if (*pszFormat != L'\0')
                {
                    if (pszDest == NULL)
                    {
                        hr = STRSAFE_E_INVALID_PARAMETER;
                    }
                    else
                    {
                        hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                    }
                }
            }
            else
            {
                int iRet;
                size_t cchMax;

                // leave the last space for the null terminator
                cchMax = cchDest - 1;

                iRet = _vsnwprintf(pszDest, cchMax, pszFormat, argList);
                // ASSERT((iRet < 0) || (((size_t)iRet) <= cchMax));

                if ((iRet < 0) || (((size_t)iRet) > cchMax))
                {
                    // we have truncated pszDest
                    pszDestEnd = pszDest + cchMax;
                    cchRemaining = 1;

                    // need to null terminate the string
                    *pszDestEnd = L'\0';

                    hr = STRSAFE_E_INSUFFICIENT_BUFFER;
                }
                else if (((size_t)iRet) == cchMax)
                {
                    // string fit perfectly
                    pszDestEnd = pszDest + cchMax;
                    cchRemaining = 1;

                    // need to null terminate the string
                    *pszDestEnd = L'\0';
                }
                else if (((size_t)iRet) < cchMax)
                {
                    // there is extra room
                    pszDestEnd = pszDest + iRet;
                    cchRemaining = cchDest - iRet;

                    if (dwFlags & STRSAFE_FILL_BEHIND_NULL)
                    {
                        memset(pszDestEnd + 1, STRSAFE_GET_FILL_PATTERN(dwFlags), ((cchRemaining - 1) * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR)));
                    }
                }
            }
        }
    }
    
    if (FAILED(hr))
    {
        if (pszDest)
        {
            if (dwFlags & STRSAFE_FILL_ON_FAILURE)
            {
                memset(pszDest, STRSAFE_GET_FILL_PATTERN(dwFlags), cbDest);

                if (STRSAFE_GET_FILL_PATTERN(dwFlags) == 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;
                }
                else if (cchDest > 0)
                {
                    pszDestEnd = pszDest + cchDest - 1;
                    cchRemaining = 1;

                    // null terminate the end of the string
                    *pszDestEnd = L'\0';
                }
            }

            if (dwFlags & (STRSAFE_NULL_ON_FAILURE | STRSAFE_NO_TRUNCATION))
            {
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;

                    // null terminate the beginning of the string
                    *pszDestEnd = L'\0';
                }
            }
        }
    }

    if (SUCCEEDED(hr) || (hr == STRSAFE_E_INSUFFICIENT_BUFFER))
    {
        if (ppszDestEnd) 
        {
            *ppszDestEnd = pszDestEnd;
        }

        if (pcchRemaining)
        {
            *pcchRemaining = cchRemaining;
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX

STRSAFEAPI StringLengthWorkerA(const char* psz, size_t cchMax, size_t* pcch)
{
    HRESULT hr = S_OK;
    size_t cchMaxPrev = cchMax;

    while (cchMax && (*psz != '\0'))
    {
        psz++;
        cchMax--;
    }

    if (cchMax == 0)
    {
        // the string is longer than cchMax
        hr = STRSAFE_E_INVALID_PARAMETER;
    }

    if (SUCCEEDED(hr) && pcch)
    {
        *pcch = cchMaxPrev - cchMax;
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFEAPI StringLengthWorkerW(const WCHAR* psz, size_t cchMax, size_t* pcch)
{
    HRESULT hr = S_OK;
    size_t cchMaxPrev = cchMax;

    while (cchMax && (*psz != L'\0'))
    {
        psz++;
        cchMax--;
    }

    if (cchMax == 0)
    {
        // the string is longer than cchMax
        hr = STRSAFE_E_INVALID_PARAMETER;
    }

    if (SUCCEEDED(hr) && pcch)
    {
        *pcch = cchMaxPrev - cchMax;
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // STRSAFE_INLINE

#ifndef STRSAFE_LIB_IMPL
#ifndef FEATURE_PAL
STRSAFE_INLINE_API StringGetsExWorkerA(char* pszDest, size_t cchDest, size_t cbDest, char** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr = S_OK;
    char* pszDestEnd = pszDest;
    size_t cchRemaining = 0;

    // ASSERT(cbDest == (cchDest * sizeof(char))    ||
    //        cbDest == (cchDest * sizeof(char)) + (cbDest % sizeof(char)));

    // only accept valid flags
    if (dwFlags & (~STRSAFE_VALID_FLAGS))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        if (dwFlags & STRSAFE_IGNORE_NULLS)
        {
            if (pszDest == NULL)
            {
                if ((cchDest != 0) || (cbDest != 0))
                {
                    // NULL pszDest and non-zero cchDest/cbDest is invalid
                    hr = STRSAFE_E_INVALID_PARAMETER;
                }
            }
        }

        if (SUCCEEDED(hr))
        {
            if (cchDest <= 1)
            {
                pszDestEnd = pszDest;
                cchRemaining = cchDest;

                if (cchDest == 1)
                {
                    *pszDestEnd = '\0';
                }

                hr = STRSAFE_E_INSUFFICIENT_BUFFER;
            }
            else
            {
                char ch;

                pszDestEnd = pszDest;
                cchRemaining = cchDest;

                while ((cchRemaining > 1) && (ch = (char)getc(stdin)) != '\n')
                {
                    if (ch == EOF)
                    {
                        if (pszDestEnd == pszDest)
                        {
                            // we failed to read anything from stdin
                            hr = STRSAFE_E_END_OF_FILE;
                        }
                        break;
                    }

                    *pszDestEnd = ch;

                    pszDestEnd++;
                    cchRemaining--;
                }

                if (cchRemaining > 0)
                {
                    // there is extra room
                    if (dwFlags & STRSAFE_FILL_BEHIND_NULL)
                    {
                        memset(pszDestEnd + 1, STRSAFE_GET_FILL_PATTERN(dwFlags), ((cchRemaining - 1) * sizeof(char)) + (cbDest % sizeof(char)));
                    }
                }

                *pszDestEnd = '\0';
            }
        }
    }

    if (FAILED(hr))
    {
        if (pszDest)
        {
            if (dwFlags & STRSAFE_FILL_ON_FAILURE)
            {
                memset(pszDest, STRSAFE_GET_FILL_PATTERN(dwFlags), cbDest);
            
                if (STRSAFE_GET_FILL_PATTERN(dwFlags) == 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;
                }
                else if (cchDest > 0)
                {
                    pszDestEnd = pszDest + cchDest - 1;
                    cchRemaining = 1;

                    // null terminate the end of the string
                    *pszDestEnd = '\0';
                }
            }

            if (dwFlags & (STRSAFE_NULL_ON_FAILURE | STRSAFE_NO_TRUNCATION))
            {
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;

                    // null terminate the beginning of the string
                    *pszDestEnd = '\0';
                }
            }
        }
    }

    if (SUCCEEDED(hr)                           ||
        (hr == STRSAFE_E_INSUFFICIENT_BUFFER)   ||
        (hr == STRSAFE_E_END_OF_FILE))
    {
        if (ppszDestEnd)
        {
            *ppszDestEnd = pszDestEnd;
        }

        if (pcchRemaining)
        {
            *pcchRemaining = cchRemaining;
        }
    }

    return hr;
}

#if defined(FEATURE_PAL) || !defined(PLATFORM_UNIX)
STRSAFE_INLINE_API StringGetsExWorkerW(WCHAR* pszDest, size_t cchDest, size_t cbDest, WCHAR** ppszDestEnd, size_t* pcchRemaining, unsigned long dwFlags)
{
    HRESULT hr = S_OK;
    WCHAR* pszDestEnd = pszDest;
    size_t cchRemaining = 0;

    // ASSERT(cbDest == (cchDest * sizeof(char))    ||
    //        cbDest == (cchDest * sizeof(char)) + (cbDest % sizeof(char)));

    // only accept valid flags
    if (dwFlags & (~STRSAFE_VALID_FLAGS))
    {
        hr = STRSAFE_E_INVALID_PARAMETER;
    }
    else
    {
        if (dwFlags & STRSAFE_IGNORE_NULLS)
        {
            if (pszDest == NULL)
            {
                if ((cchDest != 0) || (cbDest != 0))
                {
                    // NULL pszDest and non-zero cchDest/cbDest is invalid
                    hr = STRSAFE_E_INVALID_PARAMETER;
                }
            }
        }

        if (SUCCEEDED(hr))
        {
            if (cchDest <= 1)
            {
                pszDestEnd = pszDest;
                cchRemaining = cchDest;

                if (cchDest == 1)
                {
                    *pszDestEnd = L'\0';
                }

                hr = STRSAFE_E_INSUFFICIENT_BUFFER;
            }
            else
            {
                WCHAR ch;

                pszDestEnd = pszDest;
                cchRemaining = cchDest;

                while ((cchRemaining > 1) && (ch = (WCHAR)getwc(stdin)) != L'\n')
                {
                    if (ch == EOF)
                    {
                        if (pszDestEnd == pszDest)
                        {
                            // we failed to read anything from stdin
                            hr = STRSAFE_E_END_OF_FILE;
                        }
                        break;
                    }

                    *pszDestEnd = ch;

                    pszDestEnd++;
                    cchRemaining--;
                }

                if (cchRemaining > 0)
                {
                    // there is extra room
                    if (dwFlags & STRSAFE_FILL_BEHIND_NULL)
                    {
                        memset(pszDestEnd + 1, STRSAFE_GET_FILL_PATTERN(dwFlags), ((cchRemaining - 1) * sizeof(WCHAR)) + (cbDest % sizeof(WCHAR)));
                    }
                }

                *pszDestEnd = L'\0';
            }
        }
    }

    if (FAILED(hr))
    {
        if (pszDest)
        {
            if (dwFlags & STRSAFE_FILL_ON_FAILURE)
            {
                memset(pszDest, STRSAFE_GET_FILL_PATTERN(dwFlags), cbDest);

                if (STRSAFE_GET_FILL_PATTERN(dwFlags) == 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;
                }
                else if (cchDest > 0)
                {
                    pszDestEnd = pszDest + cchDest - 1;
                    cchRemaining = 1;

                    // null terminate the end of the string
                    *pszDestEnd = L'\0';
                }
            }

            if (dwFlags & (STRSAFE_NULL_ON_FAILURE | STRSAFE_NO_TRUNCATION))
            {
                if (cchDest > 0)
                {
                    pszDestEnd = pszDest;
                    cchRemaining = cchDest;

                    // null terminate the beginning of the string
                    *pszDestEnd = L'\0';
                }
            }
        }
    }

    if (SUCCEEDED(hr)                           ||
        (hr == STRSAFE_E_INSUFFICIENT_BUFFER)   ||
        (hr == STRSAFE_E_END_OF_FILE))
    {
        if (ppszDestEnd)
        {
            *ppszDestEnd = pszDestEnd;
        }

        if (pcchRemaining)
        {
            *pcchRemaining = cchRemaining;
        }
    }

    return hr;
}
#endif // FEATURE_PAL || !PLATFORM_UNIX
#endif  // !FEATURE_PAL
#endif  // !STRSAFE_LIB_IMPL

#endif  // _STRSAFE_H_INCLUDED_
