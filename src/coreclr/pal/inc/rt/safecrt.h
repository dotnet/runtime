// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
/***
*safecrt.h - secure crt downlevel for windows build
*
*Purpose:
*       This file contains a subset of the Secure CRT. It is meant to
*       be used in the Windows source tree.
*
****/

/* #pragma once */

/* guard against other includes */
#if !defined(_CRT_ALTERNATIVE_INLINES)
#error "_CRT_ALTERNATIVE_INLINES needs to be defined to use safecrt.h. This will make sure the safecrt functions are not declared in the standard headers."
#endif

#if defined(_CRT_ALTERNATIVE_IMPORTED)
#error "_CRT_ALTERNATIVE_IMPORTED is defined. This means some files were included with _CRT_ALTERNATIVE_INLINES undefined."
#endif

#if !defined(_INC_SAFECRT)
#define _INC_SAFECRT

#if !defined(_SAFECRT_NO_INCLUDES)
#include <stdarg.h>     /* for va_start, etc. */
#endif

/* _SAFECRT switches */
#if !defined(_SAFECRT_USE_INLINES)
#define _SAFECRT_USE_INLINES 0
#endif

#if !defined(_SAFECRT_SET_ERRNO)
#define _SAFECRT_SET_ERRNO 1
#endif

#if !defined(_SAFECRT_DEFINE_TCS_MACROS)
#define _SAFECRT_DEFINE_TCS_MACROS 0
#endif

#if !defined(_SAFECRT_DEFINE_MBS_FUNCTIONS)
#define _SAFECRT_DEFINE_MBS_FUNCTIONS 1
#endif

#if !defined(_SAFECRT_USE_CPP_OVERLOADS)
#define _SAFECRT_USE_CPP_OVERLOADS 0
#endif

#if !defined(_SAFECRT_FILL_BUFFER)
#if defined(_DEBUG)
#define _SAFECRT_FILL_BUFFER 1
#else
#define _SAFECRT_FILL_BUFFER 0
#endif
#endif

#if !defined(_SAFECRT_FILL_BUFFER_PATTERN)
#define _SAFECRT_FILL_BUFFER_PATTERN 0xFD
#endif

#if !defined(_SAFECRT_INVALID_PARAMETER_DEBUG_INFO)
#define _SAFECRT_INVALID_PARAMETER_DEBUG_INFO 0
#endif

#if !defined(_SAFECRT_IMPL) && defined (_SAFECRT_USE_INLINES)
#define _SAFECRT__INLINE __inline
#else
#define _SAFECRT__INLINE
#endif

/* additional includes */
#if _SAFECRT_USE_INLINES && !defined(_SAFECRT_NO_INCLUDES)
#include <stdlib.h>     /* for _MAX_DRIVE */
#include <string.h>     /* for memset */
#include <windows.h>    /* for NTSTATUS, RaiseException */
#if _SAFECRT_SET_ERRNO
#include <errno.h>
#endif
#if _SAFECRT_DEFINE_MBS_FUNCTIONS
#include <mbctype.h>
#endif
#endif

/* _W64 */
#if !defined(_W64)
#if !defined(__midl) && (defined(HOST_X86) || defined(_M_IX86)) && _MSC_VER >= 1300
#define _W64 __w64
#else
#define _W64
#endif
#endif

/* uintptr_t */
#if !defined(_UINTPTR_T_DEFINED)
#if defined(HOST_64BIT)
typedef unsigned __int64    uintptr_t;
#else
typedef _W64 unsigned int   uintptr_t;
#endif
#define _UINTPTR_T_DEFINED
#endif

#ifdef __GNUC__
#define SAFECRT_DEPRECATED __attribute__((deprecated))
#else
#define SAFECRT_DEPRECATED __declspec(deprecated)
#endif

/* errno_t */
#if !defined(_ERRCODE_DEFINED)
#define _ERRCODE_DEFINED
/* errcode is deprecated in favor or errno_t, which is part of the standard proposal */
SAFECRT_DEPRECATED typedef int errcode;
typedef int errno_t; /* standard */
#endif

/* error codes */
#if !defined(_SECURECRT_ERRCODE_VALUES_DEFINED)
#define _SECURECRT_ERRCODE_VALUES_DEFINED
#if !defined(EINVAL)
#define EINVAL          22
#endif
#if !defined(ERANGE)
#define ERANGE          34
#endif
#if !defined(EILSEQ)
#define EILSEQ          42
#endif
#if !defined(STRUNCATE)
#define STRUNCATE       80
#endif
#endif

/* _TRUNCATE */
#if !defined(_TRUNCATE)
#define _TRUNCATE ((size_t)-1)
#endif

/* _SAFECRT_AUTOMATICALLY_REPLACED_CALL */
#if !defined(_SAFECRT_AUTOMATICALLY_REPLACED_CALL)
#define _SAFECRT_AUTOMATICALLY_REPLACED_CALL(v) (v)
#endif

/* internal macros */
#if _SAFECRT_USE_INLINES
#define _SAFECRT__EXTERN_C
#else
#if defined(__cplusplus)
#define _SAFECRT__EXTERN_C extern "C"
#else
#define _SAFECRT__EXTERN_C extern
#endif
#endif /* _SAFECRT_USE_INLINES */

#if !defined(_SAFECRT_IMPL)

#define _SAFECRT__STR2WSTR(str)     L##str

#define _SAFECRT__STR2WSTR2(str)    _SAFECRT__STR2WSTR(str)

#if !defined(__FILEW__)
#define __FILEW__                   _SAFECRT__STR2WSTR2(__FILE__)
#endif

#if !defined(__FUNCTIONW__)
#define __FUNCTIONW__               _SAFECRT__STR2WSTR2(__FUNCTION__)
#endif

#endif

/* validation macros */
#if !defined(_SAFECRT_INVALID_PARAMETER)
#if _SAFECRT_INVALID_PARAMETER_DEBUG_INFO
#define _SAFECRT_INVALID_PARAMETER(message) _invalid_parameter(message, __FUNCTIONW__, __FILEW__, __LINE__, 0)
#else
#define _SAFECRT_INVALID_PARAMETER(message) _invalid_parameter(nullptr, nullptr, nullptr, 0, 0)
#endif
#endif

#if !defined(_SAFECRT__SET_ERRNO)
#if _SAFECRT_SET_ERRNO
#define _SAFECRT__SET_ERRNO(_ErrorCode) errno = (_ErrorCode)
#else
#define _SAFECRT__SET_ERRNO(_ErrorCode)
#endif
#endif

#if !defined(_SAFECRT__RETURN_ERROR)
#define _SAFECRT__RETURN_ERROR(_Msg, _Ret) \
    _SAFECRT__SET_ERRNO(EINVAL); \
    _SAFECRT_INVALID_PARAMETER(_Msg); \
    return _Ret
#endif

#if !defined(_SAFECRT__VALIDATE_STRING_ERROR)
#define _SAFECRT__VALIDATE_STRING_ERROR(_String, _Size, _Ret) \
    if ((_String) == nullptr || (_Size) == 0) \
    { \
        _SAFECRT__SET_ERRNO(EINVAL); \
        _SAFECRT_INVALID_PARAMETER(L"String " _SAFECRT__STR2WSTR(#_String) L" is invalid"); \
        return _Ret; \
    }
#endif

#if !defined(_SAFECRT__VALIDATE_STRING)
#define _SAFECRT__VALIDATE_STRING(_String, _Size) _SAFECRT__VALIDATE_STRING_ERROR(_String, _Size, EINVAL)
#endif

#if !defined(_SAFECRT__VALIDATE_POINTER_ERROR_RETURN)
#define _SAFECRT__VALIDATE_POINTER_ERROR_RETURN(_Pointer, _ErrorCode, _Ret) \
    if ((_Pointer) == nullptr) \
    { \
        _SAFECRT__SET_ERRNO(_ErrorCode); \
        _SAFECRT_INVALID_PARAMETER(L"Pointer " _SAFECRT__STR2WSTR(#_Pointer) L" is invalid"); \
        return _Ret; \
    }
#endif

#if !defined(_SAFECRT__VALIDATE_POINTER_ERROR)
#define _SAFECRT__VALIDATE_POINTER_ERROR(_Pointer, _Ret) \
    _SAFECRT__VALIDATE_POINTER_ERROR_RETURN(_Pointer, EINVAL, _Ret)
#endif

#if !defined(_SAFECRT__VALIDATE_POINTER)
#define _SAFECRT__VALIDATE_POINTER(_Pointer) \
    _SAFECRT__VALIDATE_POINTER_ERROR(_Pointer, EINVAL)
#endif

#if !defined(_SAFECRT__VALIDATE_POINTER_RESET_STRING_ERROR)
#define _SAFECRT__VALIDATE_POINTER_RESET_STRING_ERROR(_Pointer, _String, _Size, _Ret) \
    if ((_Pointer) == nullptr) \
    { \
        _SAFECRT__SET_ERRNO(EINVAL); \
        _SAFECRT__RESET_STRING(_String, _Size); \
        _SAFECRT_INVALID_PARAMETER(L"Pointer " _SAFECRT__STR2WSTR(#_Pointer) L" is invalid"); \
        return _Ret; \
    }
#endif

#if !defined(_SAFECRT__VALIDATE_POINTER_RESET_STRING)
#define _SAFECRT__VALIDATE_POINTER_RESET_STRING(_Pointer, _String, _Size) \
    _SAFECRT__VALIDATE_POINTER_RESET_STRING_ERROR(_Pointer, _String, _Size, EINVAL)
#endif

#if !defined(_SAFECRT__VALIDATE_CONDITION_ERROR_RETURN)
#define _SAFECRT__VALIDATE_CONDITION_ERROR_RETURN(_Condition, _ErrorCode, _Ret) \
    if (!(_Condition)) \
    { \
        _SAFECRT__SET_ERRNO(_ErrorCode); \
        _SAFECRT_INVALID_PARAMETER(_SAFECRT__STR2WSTR(#_Condition)); \
        return _Ret; \
    }
#endif

#if !defined(_SAFECRT__VALIDATE_CONDITION_ERROR)
#define _SAFECRT__VALIDATE_CONDITION_ERROR(_Condition, _Ret) \
    _SAFECRT__VALIDATE_CONDITION_ERROR_RETURN(_Condition, EINVAL, _Ret)
#endif

/* if _SAFECRT_FILL_BUFFER is on, fill the interval [_Offset, _Size) with _SAFECRT_FILL_BUFFER_PATTERN;
 * assume that the string has been validated with _SAFECRT__VALIDATE_STRING
 */
#if !defined(_SAFECRT__FILL_STRING)
#if _SAFECRT_FILL_BUFFER
#define _SAFECRT__FILL_STRING(_String, _Size, _Offset) \
    if ((size_t)(_Offset) < (_Size)) \
    { \
        memset((_String) + (_Offset), _SAFECRT_FILL_BUFFER_PATTERN, ((_Size) - (_Offset)) * sizeof(*(_String))); \
    }
#else
#define _SAFECRT__FILL_STRING(_String, _Size, _Offset)
#endif
#endif

/* if _SAFECRT_FILL_BUFFER is on, set the byte to _SAFECRT_FILL_BUFFER_PATTERN
 */
#if !defined(_SAFECRT__FILL_BYTE)
#if _SAFECRT_FILL_BUFFER
#define _SAFECRT__FILL_BYTE(_Position) \
    (_Position) = _SAFECRT_FILL_BUFFER_PATTERN
#else
#define _SAFECRT__FILL_BYTE(_Position)
#endif
#endif

/* put a null terminator at the beginning of the string and then calls _SAFECRT__FILL_STRING;
 * assume that the string has been validated with _SAFECRT__VALIDATE_STRING
 */
#if !defined(_SAFECRT__RESET_STRING)
#define _SAFECRT__RESET_STRING(_String, _Size) \
    *(_String) = 0; \
    _SAFECRT__FILL_STRING(_String, _Size, 1);
#endif

#if !defined(_SAFECRT__RETURN_BUFFER_TOO_SMALL_ERROR)
#define _SAFECRT__RETURN_BUFFER_TOO_SMALL_ERROR(_String, _Size, _Ret) \
    _SAFECRT__SET_ERRNO(ERANGE); \
    _SAFECRT_INVALID_PARAMETER(L"Buffer " _SAFECRT__STR2WSTR(#_String) L" is too small"); \
    return _Ret;
#endif

#if !defined(_SAFECRT__RETURN_BUFFER_TOO_SMALL)
#define _SAFECRT__RETURN_BUFFER_TOO_SMALL(_String, _Size) \
    _SAFECRT__RETURN_BUFFER_TOO_SMALL_ERROR(_String, _Size, ERANGE)
#endif

#if !defined(_SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED)
#define _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_String, _Size) \
    _SAFECRT__SET_ERRNO(EINVAL); \
    _SAFECRT_INVALID_PARAMETER(L"String " _SAFECRT__STR2WSTR(#_String) L" is not terminated"); \
    return EINVAL;
#endif

#if !defined(_SAFECRT__RETURN_EINVAL)
#define _SAFECRT__RETURN_EINVAL \
    _SAFECRT__SET_ERRNO(EINVAL); \
    _SAFECRT_INVALID_PARAMETER(L"Invalid parameter"); \
    return EINVAL;
#endif

_SAFECRT__EXTERN_C
void __cdecl _invalid_parameter(const WCHAR *_Message, const WCHAR *_FunctionName, const WCHAR *_FileName, unsigned int _LineNumber, uintptr_t _Reserved);

#if (_SAFECRT_USE_INLINES || _SAFECRT_IMPL) && !defined(_SAFECRT_DO_NOT_DEFINE_INVALID_PARAMETER)

#ifndef STATUS_INVALID_PARAMETER
#define STATUS_INVALID_PARAMETER ((NTSTATUS)0xC000000DL)
#endif

_SAFECRT__INLINE
void __cdecl _invalid_parameter(const WCHAR *_Message, const WCHAR *_FunctionName, const WCHAR *_FileName, unsigned int _LineNumber, uintptr_t _Reserved)
{
#ifdef _MSC_VER
    (_Message);
    (_FunctionName);
    (_FileName);
    (_LineNumber);
    (_Reserved);
#endif
    /* invoke Watson */
    RaiseException((DWORD)STATUS_INVALID_PARAMETER, 0, 0, nullptr);
}

#endif

//#if !defined(_SAFECRT_IMPL)

#if _SAFECRT_DEFINE_TCS_MACROS

/* _tcs macros */
#if !defined(_UNICODE) && !defined(UNICODE)

#define _tcscpy_s       strcpy_s
#define _tcsncpy_s      strncpy_s
#define _tcscat_s       strcat_s
#define _tcsncat_s      strncat_s
#define _tcsset_s       _strset_s
#define _tcsnset_s      _strnset_s
#define _vsntprintf_s   _vsnprintf_s

#elif defined(_UNICODE) || defined(UNICODE)

#define _tcscpy_s       wcscpy_s
#define _tcsncpy_s      wcsncpy_s
#define _tcscat_s       wcscat_s
#define _tcsncat_s      wcsncat_s
#define _tcsset_s       _wcsset_s
#define _tcsnset_s      _wcsnset_s
#define _tmakepath_s    _wmakepath_s
#define _stprintf_s     swprintf_s
#define _tscanf_s       wscanf_s
#define _tsscanf_s      swscanf_s

#else

#error We should not get here...

#endif

#endif /* _SAFECRT_DEFINE_TCS_MACROS */

/* strcpy_s */
/*
 * strcpy_s, wcscpy_s copy string _Src into _Dst;
 * will call _SAFECRT_INVALID_PARAMETER if string _Src does not fit into _Dst
 */


_SAFECRT__EXTERN_C
errno_t __cdecl strcpy_s(char *_Dst, size_t _SizeInBytes, const char *_Src);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl strcpy_s(char (&_Dst)[_SizeInBytes], const char *_Src)
{
    return strcpy_s(_Dst, _SizeInBytes, _Src);
}
#endif


#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL


_SAFECRT__INLINE
errno_t __cdecl strcpy_s(char *_Dst, size_t _SizeInBytes, const char *_Src)
{

    char *p;
    size_t available;

    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
    _SAFECRT__VALIDATE_POINTER_RESET_STRING(_Src, _Dst, _SizeInBytes);

    p = _Dst;
    available = _SizeInBytes;
    while ((*p++ = *_Src++) != 0 && --available > 0)
    {
    }

    if (available == 0)
    {
       _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
       _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInBytes);
    }

    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    return 0;
}

#endif

/* wcscpy_s */
_SAFECRT__EXTERN_C
errno_t __cdecl wcscpy_s(WCHAR *_Dst, size_t _SizeInWords, const WCHAR *_Src);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInWords>
inline
errno_t __cdecl wcscpy_s(WCHAR (&_Dst)[_SizeInWords], const WCHAR *_Src)
{
    return wcscpy_s(_Dst, _SizeInWords, _Src);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
errno_t __cdecl wcscpy_s(WCHAR *_Dst, size_t _SizeInWords, const WCHAR *_Src)
{
    WCHAR *p;
    size_t available;

    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInWords);
    _SAFECRT__VALIDATE_POINTER_RESET_STRING(_Src, _Dst, _SizeInWords);

    p = _Dst;
    available = _SizeInWords;
    while ((*p++ = *_Src++) != 0 && --available > 0)
    {
    }

    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInWords);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInWords);
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInWords, _SizeInWords - available + 1);
    return 0;
}

#endif

/* strncpy_s */
/*
 * strncpy_s, wcsncpy_s copy at max _Count characters from string _Src into _Dst;
 * string _Dst will always be null-terminated;
 * will call _SAFECRT_INVALID_PARAMETER if there is not enough space in _Dst;
 * if _Count == _TRUNCATE, we will copy as many characters as we can from _Src into _Dst, and
 *      return STRUNCATE if _Src does not entirely fit into _Dst (we will not call _SAFECRT_INVALID_PARAMETER);
 * if _Count == 0, then (_Dst == nullptr && _SizeInBytes == 0) is allowed
 */
_SAFECRT__EXTERN_C
errno_t __cdecl strncpy_s(char *_Dst, size_t _SizeInBytes, const char *_Src, size_t _Count);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl strncpy_s(char (&_Dst)[_SizeInBytes], const char *_Src, size_t _Count)
{
    return strncpy_s(_Dst, _SizeInBytes, _Src, _Count);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
errno_t __cdecl strncpy_s(char *_Dst, size_t _SizeInBytes, const char *_Src, size_t _Count)
{
    char *p;
    size_t available;

    if (_Count == 0 && _Dst == nullptr && _SizeInBytes == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }

    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
    if (_Count == 0)
    {
        /* notice that the source string pointer can be nullptr in this case */
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        return 0;
    }
    _SAFECRT__VALIDATE_POINTER_RESET_STRING(_Src, _Dst, _SizeInBytes);

    p = _Dst;
    available = _SizeInBytes;
    if (_Count == _TRUNCATE)
    {
        while ((*p++ = *_Src++) != 0 && --available > 0)
        {
        }
    }
    else
    {
        while ((*p++ = *_Src++) != 0 && --available > 0 && --_Count > 0)
        {
        }
        if (_Count == 0)
        {
            *p = 0;
        }
    }

    if (available == 0)
    {
        if (_Count == _TRUNCATE)
        {
            _Dst[_SizeInBytes - 1] = 0;
            return STRUNCATE;
        }
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInBytes);
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    return 0;
}

#endif

/* wcsncpy_s */
_SAFECRT__EXTERN_C
errno_t __cdecl wcsncpy_s(WCHAR *_Dst, size_t _SizeInWords, const WCHAR *_Src, size_t _Count);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInWords>
inline
errno_t __cdecl wcsncpy_s(WCHAR (&_Dst)[_SizeInWords], const WCHAR *_Src, size_t _Count)
{
    return wcsncpy_s(_Dst, _SizeInWords, _Src, _Count);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
errno_t __cdecl wcsncpy_s(WCHAR *_Dst, size_t _SizeInWords, const WCHAR *_Src, size_t _Count)
{
    WCHAR *p;
    size_t available;

    if (_Count == 0 && _Dst == nullptr && _SizeInWords == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }

    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInWords);
    if (_Count == 0)
    {
        /* notice that the source string pointer can be nullptr in this case */
        _SAFECRT__RESET_STRING(_Dst, _SizeInWords);
        return 0;
    }
    _SAFECRT__VALIDATE_POINTER_RESET_STRING(_Src, _Dst, _SizeInWords);

    p = _Dst;
    available = _SizeInWords;
    if (_Count == _TRUNCATE)
    {
        while ((*p++ = *_Src++) != 0 && --available > 0)
        {
        }
    }
    else
    {
        while ((*p++ = *_Src++) != 0 && --available > 0 && --_Count > 0)
        {
        }
        if (_Count == 0)
        {
            *p = 0;
        }
    }

    if (available == 0)
    {
        if (_Count == _TRUNCATE)
        {
            _Dst[_SizeInWords - 1] = 0;
            return STRUNCATE;
        }
        _SAFECRT__RESET_STRING(_Dst, _SizeInWords);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInWords);
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInWords, _SizeInWords - available + 1);
    return 0;
}

#endif

/* strcat_s */
/*
 * strcat_s, wcscat_s append string _Src to _Dst;
 * will call _SAFECRT_INVALID_PARAMETER if there is not enough space in _Dst
 */
_SAFECRT__EXTERN_C
errno_t __cdecl strcat_s(char *_Dst, size_t _SizeInBytes, const char *_Src);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl strcat_s(char (&_Dst)[_SizeInBytes], const char *_Src)
{
    return strcat_s(_Dst, _SizeInBytes, _Src);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
errno_t __cdecl strcat_s(char *_Dst, size_t _SizeInBytes, const char *_Src)
{
    char *p;
    size_t available;

    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
    _SAFECRT__VALIDATE_POINTER_RESET_STRING(_Src, _Dst, _SizeInBytes);

    p = _Dst;
    available = _SizeInBytes;
    while (available > 0 && *p != 0)
    {
        p++;
        available--;
    }

    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInBytes);
    }

    while ((*p++ = *_Src++) != 0 && --available > 0)
    {
    }

    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInBytes);
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    return 0;
}

#endif

/* wcscat_s */
_SAFECRT__EXTERN_C
errno_t __cdecl wcscat_s(WCHAR *_Dst, size_t _SizeInWords, const WCHAR *_Src);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInWords>
inline
errno_t __cdecl wcscat_s(WCHAR (&_Dst)[_SizeInWords], const WCHAR *_Src)
{
    return wcscat_s(_Dst, _SizeInWords, _Src);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
errno_t __cdecl wcscat_s(WCHAR *_Dst, size_t _SizeInWords, const WCHAR *_Src)
{
    WCHAR *p;
    size_t available;

    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInWords);
    _SAFECRT__VALIDATE_POINTER_RESET_STRING(_Src, _Dst, _SizeInWords);

    p = _Dst;
    available = _SizeInWords;
    while (available > 0 && *p != 0)
    {
        p++;
        available--;
    }

    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInWords);
        _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInWords);
    }

    while ((*p++ = *_Src++) != 0 && --available > 0)
    {
    }

    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInWords);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInWords);
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInWords, _SizeInWords - available + 1);
    return 0;
}

#endif

/* strncat_s */
/*
 * strncat_s, wcsncat_s append at max _Count characters from string _Src to _Dst;
 * string _Dst will always be null-terminated;
 * will call _SAFECRT_INVALID_PARAMETER if there is not enough space in _Dst;
 * if _Count == _TRUNCATE, we will append as many characters as we can from _Src to _Dst, and
 *      return STRUNCATE if _Src does not entirely fit into _Dst (we will not call _SAFECRT_INVALID_PARAMETER);
 * if _Count == 0, then (_Dst == nullptr && _SizeInBytes == 0) is allowed
 */
_SAFECRT__EXTERN_C
errno_t __cdecl strncat_s(char *_Dst, size_t _SizeInBytes, const char *_Src, size_t _Count);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl strncat_s(char (&_Dst)[_SizeInBytes], const char *_Src, size_t _Count)
{
    return strncat_s(_Dst, _SizeInBytes, _Src, _Count);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
errno_t __cdecl strncat_s(char *_Dst, size_t _SizeInBytes, const char *_Src, size_t _Count)
{
    char *p;
    size_t available;
    if (_Count == 0 && _Dst == nullptr && _SizeInBytes == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }
    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
    if (_Count != 0)
    {
        _SAFECRT__VALIDATE_POINTER_RESET_STRING(_Src, _Dst, _SizeInBytes);
    }

    p = _Dst;
    available = _SizeInBytes;
    while (available > 0 && *p != 0)
    {
        p++;
        available--;
    }

    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInBytes);
    }

    if (_Count == _TRUNCATE)
    {
        while ((*p++ = *_Src++) != 0 && --available > 0)
        {
        }
    }
    else
    {
        while (_Count > 0 && (*p++ = *_Src++) != 0 && --available > 0)
        {
            _Count--;
        }
        if (_Count == 0)
        {
            *p = 0;
        }
    }

    if (available == 0)
    {
        if (_Count == _TRUNCATE)
        {
            _Dst[_SizeInBytes - 1] = 0;
            return STRUNCATE;
        }
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInBytes);
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    return 0;
}

#endif

/* wcsncat_s */
_SAFECRT__EXTERN_C
errno_t __cdecl wcsncat_s(WCHAR *_Dst, size_t _SizeInWords, const WCHAR *_Src, size_t _Count);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInWords>
inline
errno_t __cdecl wcsncat_s(WCHAR (&_Dst)[_SizeInWords], const WCHAR *_Src, size_t _Count)
{
    return wcsncat_s(_Dst, _SizeInWords, _Src, _Count);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
errno_t __cdecl wcsncat_s(WCHAR *_Dst, size_t _SizeInWords, const WCHAR *_Src, size_t _Count)
{
    WCHAR *p;
    size_t available;
    if (_Count == 0 && _Dst == nullptr && _SizeInWords == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }
    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInWords);
    if (_Count != 0)
    {
        _SAFECRT__VALIDATE_POINTER_RESET_STRING(_Src, _Dst, _SizeInWords);
    }

    p = _Dst;
    available = _SizeInWords;
    while (available > 0 && *p != 0)
    {
        p++;
        available--;
    }

    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInWords);
        _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInWords);
    }

    if (_Count == _TRUNCATE)
    {
        while ((*p++ = *_Src++) != 0 && --available > 0)
        {
        }
    }
    else
    {
        while (_Count > 0 && (*p++ = *_Src++) != 0 && --available > 0)
        {
            _Count--;
        }
        if (_Count == 0)
        {
            *p = 0;
        }
    }

    if (available == 0)
    {
        if (_Count == _TRUNCATE)
        {
            _Dst[_SizeInWords - 1] = 0;
            return STRUNCATE;
        }
        _SAFECRT__RESET_STRING(_Dst, _SizeInWords);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInWords);
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInWords, _SizeInWords - available + 1);
    return 0;
}

#endif


/* _strset_s */
/*
 * _strset_s, _wcsset_s ;
 * will call _SAFECRT_INVALID_PARAMETER if _Dst is not null terminated.
 */
_SAFECRT__EXTERN_C
errno_t __cdecl _strset_s(char *_Dst, size_t _SizeInBytes, int _Value);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _strset_s(char (&_Dst)[_SizeInBytes], int _Value)
{
    return _strset_s(_Dst, _SizeInBytes, _Value);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
errno_t __cdecl _strset_s(char *_Dst, size_t _SizeInBytes, int _Value)
{
    char *p;
    size_t available;

    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);

    p = _Dst;
    available = _SizeInBytes;
    while (*p != 0 && --available > 0)
    {
        *p++ = (char)_Value;
    }

    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInBytes);
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    return 0;
}

#endif

/* _wcsset_s */
_SAFECRT__EXTERN_C
errno_t __cdecl _wcsset_s(WCHAR *_Dst, size_t _SizeInWords, WCHAR _Value);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInWords>
inline
errno_t __cdecl _wcsset_s(WCHAR (&_Dst)[_SizeInWords], WCHAR _Value)
{
    return _wcsset_s(_Dst, _SizeInWords, _Value);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
errno_t __cdecl _wcsset_s(WCHAR *_Dst, size_t _SizeInWords, WCHAR _Value)
{
    WCHAR *p;
    size_t available;

    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInWords);

    p = _Dst;
    available = _SizeInWords;
    while (*p != 0 && --available > 0)
    {
        *p++ = (WCHAR)_Value;
    }

    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInWords);
        _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInWords);
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInWords, _SizeInWords - available + 1);
    return 0;
}

#endif

/* _strnset_s */
/*
 * _strnset_s, _wcsnset_s ;
 * will call _SAFECRT_INVALID_PARAMETER if _Dst is not null terminated.
 */
_SAFECRT__EXTERN_C
errno_t __cdecl _strnset_s(char *_Dst, size_t _SizeInBytes, int _Value, size_t _Count);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _strnset_s(char (&_Dst)[_SizeInBytes], int _Value, size_t _Count)
{
    return _strnset_s(_Dst, _SizeInBytes, _Value, _Count);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
errno_t __cdecl _strnset_s(char *_Dst, size_t _SizeInBytes, int _Value, size_t _Count)
{
    char *p;
    size_t available;

    /* validation section */
    if (_Count == 0 && _Dst == nullptr && _SizeInBytes == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);

    p = _Dst;
    available = _SizeInBytes;
    while (*p != 0 && _Count > 0 && --available > 0)
    {
        *p++ = (char)_Value;
        --_Count;
    }

    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInBytes);
    }
    if (_Count == 0)
    {
        *p = 0;
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    return 0;
}

#endif

/* _wcsnset_s */
_SAFECRT__EXTERN_C
errno_t __cdecl _wcsnset_s(WCHAR *_Dst, size_t _SizeInWords, WCHAR _Value, size_t _Count);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInWords>
inline
errno_t __cdecl _wcsnset_s(WCHAR (&_Dst)[_SizeInWords], WCHAR _Value, size_t _Count)
{
    return _wcsnset_s(_Dst, _SizeInWords, _Value, _Count);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
errno_t __cdecl _wcsnset_s(WCHAR *_Dst, size_t _SizeInWords, WCHAR _Value, size_t _Count)
{
    WCHAR *p;
    size_t available;

    /* validation section */
    if (_Count == 0 && _Dst == nullptr && _SizeInWords == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInWords);

    p = _Dst;
    available = _SizeInWords;
    while (*p != 0 && _Count > 0 && --available > 0)
    {
        *p++ = (WCHAR)_Value;
        --_Count;
    }

    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInWords);
        _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInWords);
    }
    if (_Count == 0)
    {
        *p = 0;
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInWords, _SizeInWords - available + 1);
    return 0;
}

#endif

/* wcsnlen */
extern
size_t __cdecl wcsnlen(const WCHAR *inString, size_t inMaxSize);

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
size_t __cdecl wcsnlen(const WCHAR *inString, size_t inMaxSize)
{
    size_t n;

    /* Note that we do not check if s == nullptr, because we do not
     * return errno_t...
     */

    for (n = 0; n < inMaxSize && *inString; n++, inString++)
        ;

    return n;
}

#endif

/* _wmakepath_s */
_SAFECRT__EXTERN_C
errno_t __cdecl _wmakepath_s(WCHAR *_Dst, size_t _SizeInWords, const WCHAR *_Drive, const WCHAR *_Dir, const WCHAR *_Filename, const WCHAR *_Ext);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInWords>
inline
errno_t __cdecl _wmakepath_s(WCHAR (&_Dst)[_SizeInWords], const WCHAR *_Drive, const WCHAR *_Dir, const WCHAR *_Filename, const WCHAR *_Ext)
{
    return _wmakepath_s(_Dst, _SizeInWords, _Drive, _Dir, _Filename, _Ext);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
errno_t __cdecl _wmakepath_s(WCHAR *_Dst, size_t _SizeInWords, const WCHAR *_Drive, const WCHAR *_Dir, const WCHAR *_Filename, const WCHAR *_Ext)
{
    size_t written;
    const WCHAR *p;
    WCHAR *d;

    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInWords);

    /* copy drive */
    written = 0;
    d = _Dst;
    if (_Drive != nullptr && *_Drive != 0)
    {
        written += 2;
        if(written >= _SizeInWords)
        {
            goto error_return;
        }
        *d++ = *_Drive;
        *d++ = L':';
    }

    /* copy dir */
    p = _Dir;
    if (p != nullptr && *p != 0)
    {
        do {
            if(++written >= _SizeInWords)
            {
                goto error_return;
            }
            *d++ = *p++;
        } while (*p != 0);

        p = p - 1;
        if (*p != L'/')
        {
            if(++written >= _SizeInWords)
            {
                goto error_return;
            }
            *d++ = L'/';
        }
    }

    /* copy fname */
    p = _Filename;
    if (p != nullptr)
    {
        while (*p != 0)
        {
            if(++written >= _SizeInWords)
            {
                goto error_return;
            }
            *d++ = *p++;
        }
    }

    /* copy extension; check to see if a '.' needs to be inserted */
    p = _Ext;
    if (p != nullptr)
    {
        if (*p != 0 && *p != L'.')
        {
            if(++written >= _SizeInWords)
            {
                goto error_return;
            }
            *d++ = L'.';
        }
        while (*p != 0)
        {
            if(++written >= _SizeInWords)
            {
                goto error_return;
            }
            *d++ = *p++;
        }
    }

    if(++written > _SizeInWords)
    {
        goto error_return;
    }
    *d = 0;
    _SAFECRT__FILL_STRING(_Dst, _SizeInWords, written);
    return 0;

error_return:
    _SAFECRT__RESET_STRING(_Dst, _SizeInWords);
    _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInWords);
    /* should never happen, but compiler can't tell */
    return EINVAL;
}
#endif


/* vsprintf_s */
/*
 * swprintf_s, vsprintf_s format a string and copy it into _Dst;
 * need safecrt.lib and msvcrt.dll;
 * will call _SAFECRT_INVALID_PARAMETER if there is not enough space in _Dst;
 * will call _SAFECRT_INVALID_PARAMETER if the format string is malformed;
 * the %n format type is not allowed;
 * return the length of string _Dst;
 * return a negative number if something goes wrong with mbcs conversions (we will not call _SAFECRT_INVALID_PARAMETER);
 * _SizeInBytes/_SizeInWords must be <= (INT_MAX / sizeof(char/WCHAR));
 * cannot be used without safecrt.lib
 */
_SAFECRT__EXTERN_C
int __cdecl vsprintf_s(char *_Dst, size_t _SizeInBytes, const char *_Format, va_list _ArgList);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
int __cdecl vsprintf_s(char (&_Dst)[_SizeInBytes], const char *_Format, va_list _ArgList)
{
    return vsprintf_s(_Dst, _SizeInBytes, _Format, _ArgList);
}
#endif


/* no inline version of swprintf_s */

/* _vsnprintf_s */
/*
 * _vsnprintf_s formats a string and copy at max _Count characters into _Dst;
 * need safecrt.lib and msvcrt.dll;
 * string _Dst will always be null-terminated;
 * will call _SAFECRT_INVALID_PARAMETER if there is not enough space in _Dst;
 * will call _SAFECRT_INVALID_PARAMETER if the format string is malformed;
 * the %n format type is not allowed;
 * return the length of string _Dst;
 * return a negative number if something goes wrong with mbcs conversions (we will not call _SAFECRT_INVALID_PARAMETER);
 * _SizeInBytes/_SizeInWords must be <= (INT_MAX / sizeof(char/WCHAR));
 * cannot be used without safecrt.lib;
 * if _Count == _TRUNCATE, we will copy into _Dst as many characters as we can, and
 *      return -1 if the formatted string does not entirely fit into _Dst (we will not call _SAFECRT_INVALID_PARAMETER);
 * if _Count == 0, then (_Dst == nullptr && _SizeInBytes == 0) is allowed
 */

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
int __cdecl _vsnprintf_s(char (&_Dst)[_SizeInBytes], size_t _Count, const char *_Format, va_list _ArgList)
{
    return _vsnprintf_s(_Dst, _SizeInBytes, _Count, _Format, _ArgList);
}
#endif

/* no inline version of _vsnprintf_s */

/* wscanf_s */
_SAFECRT__EXTERN_C
int __cdecl wscanf_s(const WCHAR *_Format, ...);

/* no C++ overload for wscanf_s */

/* no inline version of wscanf_s */

/* swscanf_s */
_SAFECRT__EXTERN_C
int __cdecl swscanf_s(const WCHAR *_String, const WCHAR *_Format, ...);

/* no C++ overload for swscanf_s */

/* no inline version of swscanf_s */

/* _swnscanf_s */
_SAFECRT__EXTERN_C
int __cdecl _swnscanf_s(const WCHAR *_String, size_t _Count, const WCHAR *_Format, ...);

/* no C++ overload for _swnscanf_s */

/* no inline version of _swnscanf_s */

//#endif /* ndef _SAFECRT_IMPL */

#endif  /* _INC_SAFECRT */
