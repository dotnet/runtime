// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

/* NULL */
#if !defined(NULL)
#if !defined(__cplusplus)
#define NULL 0
#else
#define NULL ((void *)0)
#endif
#endif

/* WCHAR */
#if defined (SAFECRT_INCLUDE_REDEFINES)
#if !defined(_WCHAR_T_DEFINED)
typedef unsigned short WCHAR;
#define _WCHAR_T_DEFINED
#endif
#endif

/* _W64 */
#if !defined(_W64)
#if !defined(__midl) && (defined(_X86_) || defined(_M_IX86)) && _MSC_VER >= 1300
#define _W64 __w64
#else
#define _W64
#endif
#endif

/* size_t */
#if defined (SAFECRT_INCLUDE_REDEFINES)
#if !defined(_SIZE_T_DEFINED)
#if defined(_WIN64)
typedef unsigned __int64    size_t;
#else
typedef _W64 unsigned int   size_t;
#endif
#define _SIZE_T_DEFINED
#endif
#endif

/* uintptr_t */
#if !defined(_UINTPTR_T_DEFINED)
#if defined(_WIN64)
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

/* MBCS handling: change these definitions if you do not need to support mbcs strings */
#if !defined(_SAFECRT__ISMBBLEAD)
#define _SAFECRT__ISMBBLEAD(_Character) \
    _ismbblead(_Character)
#endif

#if !defined(_SAFECRT__MBSDEC)
#define _SAFECRT__MBSDEC(_String, _Current) \
    _mbsdec(_String, _Current)
#endif

_SAFECRT__EXTERN_C
void __cdecl _invalid_parameter(const WCHAR *_Message, const WCHAR *_FunctionName, const WCHAR *_FileName, unsigned int _LineNumber, uintptr_t _Reserved);

#if (_SAFECRT_USE_INLINES || _SAFECRT_IMPL) && !defined(_SAFECRT_DO_NOT_DEFINE_INVALID_PARAMETER)

#ifndef STATUS_INVALID_PARAMETER
#if defined (SAFECRT_INCLUDE_REDEFINES)
typedef LONG NTSTATUS;
#endif
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
#if !defined(_UNICODE) && !defined(UNICODE) && !defined(_MBCS)

#define _tcscpy_s       strcpy_s
#define _tcsncpy_s      strncpy_s
#define _tcscat_s       strcat_s
#define _tcsncat_s      strncat_s
#define _tcsset_s       _strset_s
#define _tcsnset_s      _strnset_s
#define _tcstok_s       strtok_s
#define _tmakepath_s    _makepath_s
#define _tsplitpath_s   _splitpath_s
#define _stprintf_s     sprintf_s
#define _sntprintf_s    _snprintf_s
#define _vsntprintf_s   _vsnprintf_s
#define _tscanf_s       scanf_s
#define _tsscanf_s      sscanf_s
#define _tsnscanf_s     _snscanf_s

#elif defined(_UNICODE) || defined(UNICODE)

#define _tcscpy_s       wcscpy_s
#define _tcsncpy_s      wcsncpy_s
#define _tcscat_s       wcscat_s
#define _tcsncat_s      wcsncat_s
#define _tcsset_s       _wcsset_s
#define _tcsnset_s      _wcsnset_s
#define _tcstok_s       wcstok_s
#define _tmakepath_s    _wmakepath_s
#define _tsplitpath_s   _wsplitpath_s
#define _stprintf_s     swprintf_s
#define _vsntprintf_s   _vsnwprintf_s
#define _tscanf_s       wscanf_s
#define _tsscanf_s      swscanf_s
#define _tsnscanf_s     _swnscanf_s

#elif defined(_MBCS)

#define _tcscpy_s       _mbscpy_s
#define _tcsncpy_s      _mbsnbcpy_s
#define _tcscat_s       _mbscat_s
#define _tcsncat_s      _mbsnbcat_s
#define _tcsset_s       _mbsset_s
#define _tcsnset_s      _mbsnbset_s
#define _tcstok_s       _mbstok_s
#define _tmakepath_s    _makepath_s
#define _tsplitpath_s   _splitpath_s
#define _stprintf_s     sprintf_s
#define _sntprintf_s    _snprintf_s
#define _tscanf_s       scanf_s
#define _tsscanf_s      sscanf_s
#define _tsnscanf_s     _snscanf_s

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

/* _mbscpy_s */
#if _SAFECRT_DEFINE_MBS_FUNCTIONS

_SAFECRT__EXTERN_C
errno_t __cdecl _mbscpy_s(unsigned char *_Dst, size_t _SizeInBytes, const unsigned char *_Src);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _mbscpy_s(unsigned char (&_Dst)[_SizeInBytes], const unsigned char *_Src)
{
    return _mbscpy_s(_Dst, _SizeInBytes, _Src);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _mbscpy_s(unsigned char *_Dst, size_t _SizeInBytes, const unsigned char *_Src)
{
    unsigned char *p;
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
        if (*_Src == 0 && _SAFECRT__ISMBBLEAD(p[-1]))
        {
            /* the source string ended with a lead byte: we remove it */
            p[-1] = 0;
            return 0;
        }
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInBytes);
    }
    if (available < _SizeInBytes && _SAFECRT__ISMBBLEAD(p[-2]))
    {
        /* the source string ended with a lead byte: we remove it */
        p[-2] = 0;
        available++;
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    return 0;
}
 
#endif

#endif /* _SAFECRT_DEFINE_MBS_FUNCTIONS */

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

/* _mbsnbcpy_s */
#if _SAFECRT_DEFINE_MBS_FUNCTIONS

_SAFECRT__EXTERN_C
errno_t __cdecl _mbsnbcpy_s(unsigned char *_Dst, size_t _SizeInBytes, const unsigned char *_Src, size_t _CountInBytes);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _mbsnbcpy_s(unsigned char (&_Dst)[_SizeInBytes], const unsigned char *_Src, size_t _CountInBytes)
{
    return _mbsnbcpy_s(_Dst, _SizeInBytes, _Src, _CountInBytes);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _mbsnbcpy_s(unsigned char *_Dst, size_t _SizeInBytes, const unsigned char *_Src, size_t _CountInBytes)
{
    unsigned char *p;
    size_t available;
 
    if (_CountInBytes == 0 && _Dst == nullptr && _SizeInBytes == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }
 
    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
    if (_CountInBytes == 0)
    {
        /* notice that the source string pointer can be nullptr in this case */
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        return 0;
    }
    _SAFECRT__VALIDATE_POINTER_RESET_STRING(_Src, _Dst, _SizeInBytes);
 
    p = _Dst;
    available = _SizeInBytes;
    if (_CountInBytes == _TRUNCATE)
    {
        while ((*p++ = *_Src++) != 0 && --available > 0)
        {
        }
    }
    else
    {
        while ((*p++ = *_Src++) != 0 && --available > 0 && --_CountInBytes > 0)
        {
        }
        if (_CountInBytes == 0)
        {
            *p++ = 0;
        }
    }
 
    if (available == 0)
    {
        if ((*_Src == 0 || _CountInBytes == 1) && _SAFECRT__ISMBBLEAD(p[-1]))
        {
            /* the source string ended with a lead byte: we remove it */
            p[-1] = 0;
            return 0;
        }
        if (_CountInBytes == _TRUNCATE)
        {
            if (_SizeInBytes > 1 && _SAFECRT__ISMBBLEAD(_Dst[_SizeInBytes - 2]))
            {
                _Dst[_SizeInBytes - 2] = 0;
                _SAFECRT__FILL_BYTE(_Dst[_SizeInBytes - 1]);
            }
            else
            {
                _Dst[_SizeInBytes - 1] = 0;
            }
            return STRUNCATE;
        }
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInBytes);
    }
    if (available < _SizeInBytes && _SAFECRT__ISMBBLEAD(p[-2]))
    {
        /* the source string ended with a lead byte: we remove it */
        p[-2] = 0;
        available++;
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    return 0;
}
 
#endif

#endif /* _SAFECRT_DEFINE_MBS_FUNCTIONS */

/* _mbsncpy_s */
#if _SAFECRT_DEFINE_MBS_FUNCTIONS

_SAFECRT__EXTERN_C
errno_t __cdecl _mbsncpy_s(unsigned char *_Dst, size_t _SizeInBytes, const unsigned char *_Src, size_t _CountInChars);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _mbsncpy_s(unsigned char (&_Dst)[_SizeInBytes], const unsigned char *_Src, size_t _CountInChars)
{
    return _mbsncpy_s(_Dst, _SizeInBytes, _Src, _CountInChars);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _mbsncpy_s(unsigned char *_Dst, size_t _SizeInBytes, const unsigned char *_Src, size_t _CountInChars)
{
    unsigned char *p;
    size_t available;
 
    if (_CountInChars == 0 && _Dst == nullptr && _SizeInBytes == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }
 
    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
    if (_CountInChars == 0)
    {
        /* notice that the source string pointer can be nullptr in this case */
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        return 0;
    }
    _SAFECRT__VALIDATE_POINTER_RESET_STRING(_Src, _Dst, _SizeInBytes);
 
    p = _Dst;
    available = _SizeInBytes;
    if (_CountInChars == _TRUNCATE)
    {
        while ((*p++ = *_Src++) != 0 && --available > 0)
        {
        }
    }
    else
    {
        do
        {
            if (_SAFECRT__ISMBBLEAD(*_Src))
            {
                if (_Src[1] == 0)
                {
                    /* the source string ended with a lead byte: we remove it */
                    *p = 0;
                    break;
                }
                if (available <= 2)
                {
                    /* not enough space */
                    available = 0;
                    break;
                }
                *p++ = *_Src++;
                *p++ = *_Src++;
                available -= 2;
            }
            else
            {
                if ((*p++ = *_Src++) == 0 || --available == 0)
                {
                    break;
                }
            }
        }
        while (--_CountInChars > 0);
        if (_CountInChars == 0)
        {
            *p++ = 0;
        }
    }
 
    if (available == 0)
    {
        if (_CountInChars == _TRUNCATE)
        {
            if (_SizeInBytes > 1 && _SAFECRT__ISMBBLEAD(_Dst[_SizeInBytes - 2]))
            {
                _Dst[_SizeInBytes - 2] = 0;
                _SAFECRT__FILL_BYTE(_Dst[_SizeInBytes - 1]);
            }
            else
            {
                _Dst[_SizeInBytes - 1] = 0;
            }
            return STRUNCATE;
        }
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInBytes);
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    return 0;
}
 
#endif

#endif /* _SAFECRT_DEFINE_MBS_FUNCTIONS */

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

/* _mbscat_s */
#if _SAFECRT_DEFINE_MBS_FUNCTIONS

_SAFECRT__EXTERN_C
errno_t __cdecl _mbscat_s(unsigned char *_Dst, size_t _SizeInBytes, const unsigned char *_Src);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _mbscat_s(unsigned char (&_Dst)[_SizeInBytes], const unsigned char *_Src)
{
    return _mbscat_s(_Dst, _SizeInBytes, _Src);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _mbscat_s(unsigned char *_Dst, size_t _SizeInBytes, const unsigned char *_Src)
{
    unsigned char *p;
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
        if (*p == 0 && _SAFECRT__ISMBBLEAD(p[-1]))
        {
            /* the original string ended with a lead byte: we remove it */
            p--;
            *p = 0;
            available = 1;
        }
        else
        {
            _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
            _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInBytes);
        }
    }
    if (available < _SizeInBytes && _SAFECRT__ISMBBLEAD(p[-1]))
    {
        /* the original string ended with a lead byte: we remove it */
        p--;
        *p = 0;
        available++;
    }
 
    while ((*p++ = *_Src++) != 0 && --available > 0)
    {
    }
 
    if (available == 0)
    {
        if (*_Src == 0 && _SAFECRT__ISMBBLEAD(p[-1]))
        {
            /* the source string ended with a lead byte: we remove it */
            p[-1] = 0;
            return 0;
        }
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInBytes);
    }
    if (available < _SizeInBytes && _SAFECRT__ISMBBLEAD(p[-2]))
    {
        /* the source string ended with a lead byte: we remove it */
        p[-2] = 0;
        available++;
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    return 0;
}
 
#endif

#endif /* _SAFECRT_DEFINE_MBS_FUNCTIONS */

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

/* _mbsnbcat_s */
#if _SAFECRT_DEFINE_MBS_FUNCTIONS

_SAFECRT__EXTERN_C
errno_t __cdecl _mbsnbcat_s(unsigned char *_Dst, size_t _SizeInBytes, const unsigned char *_Src, size_t _CountInBytes);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _mbsnbcat_s(unsigned char (&_Dst)[_SizeInBytes], const unsigned char *_Src, size_t _CountInBytes)
{
    return _mbsnbcat_s(_Dst, _SizeInBytes, _Src, size_t _CountInBytes);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _mbsnbcat_s(unsigned char *_Dst, size_t _SizeInBytes, const unsigned char *_Src, size_t _CountInBytes)
{
    unsigned char *p;
    size_t available;
    if (_CountInBytes == 0 && _Dst == nullptr && _SizeInBytes == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }
    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
    if (_CountInBytes != 0)
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
        if (*p == 0 && _SAFECRT__ISMBBLEAD(p[-1]))
        {
            /* the original string ended with a lead byte: we remove it */
            p--;
            *p = 0;
            available = 1;
        }
        else
        {
            _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
            _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInBytes);
        }
    }
    if (available < _SizeInBytes && _SAFECRT__ISMBBLEAD(p[-1]))
    {
        /* the original string ended with a lead byte: we remove it */
        p--;
        *p = 0;
        available++;
    }
 
    if (_CountInBytes == _TRUNCATE)
    {
        while ((*p++ = *_Src++) != 0 && --available > 0)
        {
        }
    }
    else
    {
        while (_CountInBytes > 0 && (*p++ = *_Src++) != 0 && --available > 0)
        {
            _CountInBytes--;
        }
        if (_CountInBytes == 0)
        {
            *p++ = 0;
        }
    }
 
    if (available == 0)
    {
        if ((*_Src == 0 || _CountInBytes == 1) && _SAFECRT__ISMBBLEAD(p[-1]))
        {
            /* the source string ended with a lead byte: we remove it */
            p[-1] = 0;
            return 0;
        }
        if (_CountInBytes == _TRUNCATE)
        {
            if (_SizeInBytes > 1 && _SAFECRT__ISMBBLEAD(_Dst[_SizeInBytes - 2]))
            {
                _Dst[_SizeInBytes - 2] = 0;
                _SAFECRT__FILL_BYTE(_Dst[_SizeInBytes - 1]);
            }
            else
            {
                _Dst[_SizeInBytes - 1] = 0;
            }
            return STRUNCATE;
        }
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInBytes);
    }
    if (available < _SizeInBytes && _SAFECRT__ISMBBLEAD(p[-2]))
    {
        /* the source string ended with a lead byte: we remove it */
        p[-2] = 0;
        available++;
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    return 0;
}
 
#endif

#endif /* _SAFECRT_DEFINE_MBS_FUNCTIONS */

/* _mbsncat_s */
#if _SAFECRT_DEFINE_MBS_FUNCTIONS

_SAFECRT__EXTERN_C
errno_t __cdecl _mbsncat_s(unsigned char *_Dst, size_t _SizeInBytes, const unsigned char *_Src, size_t _CountInChars);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _mbsncat_s(unsigned char (&_Dst)[_SizeInBytes], const unsigned char *_Src, size_t _CountInChars)
{
    return _mbsncat_s(_Dst, _SizeInBytes, _Src, size_t _CountInChars);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _mbsncat_s(unsigned char *_Dst, size_t _SizeInBytes, const unsigned char *_Src, size_t _CountInChars)
{
    unsigned char *p;
    size_t available;
    if (_CountInChars == 0 && _Dst == nullptr && _SizeInBytes == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }
    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
    if (_CountInChars != 0)
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
        if (*p == 0 && _SAFECRT__ISMBBLEAD(p[-1]))
        {
            /* the original string ended with a lead byte: we remove it */
            p--;
            *p = 0;
            available = 1;
        }
        else
        {
            _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
            _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInBytes);
        }
    }
    if (available < _SizeInBytes && _SAFECRT__ISMBBLEAD(p[-1]))
    {
        /* the original string ended with a lead byte: we remove it */
        p--;
        *p = 0;
        available++;
    }
 
    if (_CountInChars == _TRUNCATE)
    {
        while ((*p++ = *_Src++) != 0 && --available > 0)
        {
        }
    }
    else
    {
        while (_CountInChars > 0)
        {
            if (_SAFECRT__ISMBBLEAD(*_Src))
            {
                if (_Src[1] == 0)
                {
                    /* the source string ended with a lead byte: we remove it */
                    *p = 0;
                    break;
                }
                if (available <= 2)
                {
                    /* not enough space */
                    available = 0;
                    break;
                }
                *p++ = *_Src++;
                *p++ = *_Src++;
                available -= 2;
            }
            else
            {
                if ((*p++ = *_Src++) == 0 || --available == 0)
                {
                    break;
                }
            }
            _CountInChars--;
        }
        if (_CountInChars == 0)
        {
            *p++ = 0;
        }
    }
 
    if (available == 0)
    {
        if (_CountInChars == _TRUNCATE)
        {
            if (_SizeInBytes > 1 && _SAFECRT__ISMBBLEAD(_Dst[_SizeInBytes - 2]))
            {
                _Dst[_SizeInBytes - 2] = 0;
                _SAFECRT__FILL_BYTE(_Dst[_SizeInBytes - 1]);
            }
            else
            {
                _Dst[_SizeInBytes - 1] = 0;
            }
            return STRUNCATE;
        }
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInBytes);
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    return 0;
}
 
#endif

#endif /* _SAFECRT_DEFINE_MBS_FUNCTIONS */

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

/* _mbsset_s */
#if _SAFECRT_DEFINE_MBS_FUNCTIONS

_SAFECRT__EXTERN_C
errno_t __cdecl _mbsset_s(unsigned char *_Dst, size_t _SizeInBytes, unsigned int _Value);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _mbsset_s(unsigned char (&_Dst)[_SizeInBytes], unsigned int _Value)
{
    return _mbsset_s(_Dst, _SizeInBytes, _Value);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _mbsset_s(unsigned char *_Dst, size_t _SizeInBytes, unsigned int _Value)
{
    int mbcs_error = 0;
    unsigned char *p;
    size_t available;
    unsigned char highval, lowval;
 
    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
 
    p = _Dst;
    available = _SizeInBytes;
    highval = (unsigned char)(_Value >> 8);
    lowval = (unsigned char)(_Value & 0x00ff);
    if (highval != 0)
    {
        if (_SAFECRT__ISMBBLEAD(highval) && lowval != 0)
        {
            while (*p != 0 && --available > 0)
            {
                if (p[1] == 0)
                {
                    /* do not orphan leadbyte */
                    *p++ = ' ';
                    break;
                }
                *p++ = highval;
                if (--available == 0)
                {
                    break;
                }
                *p++ = lowval;
            }
        }
        else
        {
            mbcs_error = 1;
            highval = 0;
            lowval = ' ';
        }
    }
    else
    {
        if (_SAFECRT__ISMBBLEAD(lowval))
        {
            mbcs_error = 1;
            lowval = ' ';
        }
    }
    if (highval == 0)
    {
        while (*p != 0 && --available > 0)
        {
            *p++ = lowval;
        }
    }
 
    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInBytes);
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    if (mbcs_error)
    {
        _SAFECRT__SET_ERRNO(EILSEQ); return EILSEQ;
    }
    else
    {
        return 0;
    }
}
 
#endif

#endif /* _SAFECRT_DEFINE_MBS_FUNCTIONS */

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

/* _mbsnbset_s */
#if _SAFECRT_DEFINE_MBS_FUNCTIONS

_SAFECRT__EXTERN_C
errno_t __cdecl _mbsnbset_s(unsigned char *_Dst, size_t _SizeInBytes, unsigned int _Value, size_t _CountInBytes);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _mbsnbset_s(unsigned char (&_Dst)[_SizeInBytes], unsigned int _Value, size_t _CountInBytes)
{
    return _mbsnbset_s(_Dst, _SizeInBytes, _Value, _CountInBytes);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _mbsnbset_s(unsigned char *_Dst, size_t _SizeInBytes, unsigned int _Value, size_t _CountInBytes)
{
    int mbcs_error = 0;
    unsigned char *p;
    size_t available;
    unsigned char highval, lowval;
 
    /* validation section */
    if (_CountInBytes == 0 && _Dst == nullptr && _SizeInBytes == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
 
    p = _Dst;
    available = _SizeInBytes;
    highval = (unsigned char)(_Value >> 8);
    lowval = (unsigned char)(_Value & 0x00ff);
    if (highval != 0)
    {
        if (_SAFECRT__ISMBBLEAD(highval) && lowval != 0)
        {
            while (*p != 0 && _CountInBytes > 0 && --available > 0)
            {
                if (_CountInBytes == 1 || p[1] == 0)
                {
                    /* do not orphan leadbyte */
                    *p++ = ' ';
                    --_CountInBytes;
                    break;
                }
                *p++ = highval;
                if (--available == 0)
                {
                    break;
                }
                *p++ = lowval;
                _CountInBytes -= 2;
            }
        }
        else
        {
            mbcs_error = 1;
            highval = 0;
            lowval = ' ';
        }
    }
    else
    {
        if (_SAFECRT__ISMBBLEAD(lowval))
        {
            mbcs_error = 1;
            lowval = ' ';
        }
    }
    if (highval == 0)
    {
        while (*p != 0 && available > 0 && _CountInBytes > 0)
        {
            *p++ = lowval;
            --available;
            --_CountInBytes;
        }
    }
    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInBytes);
    }
    if (_CountInBytes == 0)
    {
        *p = 0;
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    if (mbcs_error)
    {
        _SAFECRT__SET_ERRNO(EILSEQ); return EILSEQ;
    }
    else
    {
        return 0;
    }
}
 
#endif

#endif /* _SAFECRT_DEFINE_MBS_FUNCTIONS */

/* _mbsnset_s */
#if _SAFECRT_DEFINE_MBS_FUNCTIONS

_SAFECRT__EXTERN_C
errno_t __cdecl _mbsnset_s(unsigned char *_Dst, size_t _SizeInBytes, unsigned int _Value, size_t _CountInChars);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _mbsnset_s(unsigned char (&_Dst)[_SizeInBytes], unsigned int _Value, size_t _CountInChars)
{
    return _mbsnset_s(_Dst, _SizeInBytes, _Value, _CountInChars);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _mbsnset_s(unsigned char *_Dst, size_t _SizeInBytes, unsigned int _Value, size_t _CountInChars)
{
    int mbcs_error = 0;
    unsigned char *p;
    size_t available;
    unsigned char highval, lowval;
 
    /* validation section */
    if (_CountInChars == 0 && _Dst == nullptr && _SizeInBytes == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
 
    p = _Dst;
    available = _SizeInBytes;
    highval = (unsigned char)(_Value >> 8);
    lowval = (unsigned char)(_Value & 0x00ff);
    if (highval != 0)
    {
        if (_SAFECRT__ISMBBLEAD(highval) && lowval != 0)
        {
            while (*p != 0 && _CountInChars > 0 && --available > 0)
            {
                if (p[1] == 0)
                {
                    /* do not orphan leadbyte */
                    *p++ = ' ';
                    break;
                }
                *p++ = highval;
                if (--available == 0)
                {
                    break;
                }
                *p++ = lowval;
                --_CountInChars;
            }
        }
        else
        {
            mbcs_error = 1;
            highval = 0;
            lowval = ' ';
        }
    }
    else
    {
        if (_SAFECRT__ISMBBLEAD(lowval))
        {
            mbcs_error = 1;
            lowval = ' ';
        }
    }
    if (highval == 0)
    {
        while (*p != 0 && available > 0 && _CountInChars > 0)
        {
            *p++ = lowval;
            --available;
            --_CountInChars;
        }
    }
    if (available == 0)
    {
        _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
        _SAFECRT__RETURN_DEST_NOT_NULL_TERMINATED(_Dst, _SizeInBytes);
    }
    if (_CountInChars == 0)
    {
        *p = 0;
    }
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, _SizeInBytes - available + 1);
    if (mbcs_error)
    {
        _SAFECRT__SET_ERRNO(EILSEQ); return EILSEQ;
    }
    else
    {
        return 0;
    }
}
 
#endif

#endif /* _SAFECRT_DEFINE_MBS_FUNCTIONS */

/* _mbccpy_s */
#if _SAFECRT_DEFINE_MBS_FUNCTIONS

_SAFECRT__EXTERN_C
errno_t __cdecl _mbccpy_s(unsigned char *_Dst, size_t _SizeInBytes, int *_PCopied, const unsigned char *_Src);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _mbccpy_s(unsigned char (&_Dst)[_SizeInBytes], int *_PCopied, const unsigned char *_Src)
{
    return _mbccpy_s(_Dst, _SizeInBytes, _PCopied, _Src);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _mbccpy_s(unsigned char *_Dst, size_t _SizeInBytes, int *_PCopied, const unsigned char *_Src)
{
    /* validation section */
    if (_PCopied != nullptr) { *_PCopied = 0; };
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
    if (_Src == nullptr)
    {
        *_Dst = '\0';
        _SAFECRT__RETURN_EINVAL;
    }
 
    /* copy */
    if (_SAFECRT__ISMBBLEAD(*_Src))
    {
        if (_Src[1] == '\0')
        {
            /* the source string contained a lead byte followed by the null terminator:
               we copy only the null terminator and return EILSEQ to indicate the
               malformed char */
            *_Dst = '\0';
            if (_PCopied != nullptr) { *_PCopied = 1; };
            _SAFECRT__SET_ERRNO(EILSEQ); return EILSEQ;
        }
        if (_SizeInBytes < 2)
        {
            *_Dst = '\0';
            _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInBytes);
        }
        *_Dst++ = *_Src++;
        *_Dst = *_Src;
        if (_PCopied != nullptr) { *_PCopied = 2; };
    }
    else
    {
        *_Dst = *_Src;
        if (_PCopied != nullptr) { *_PCopied = 1; };
    }
 
    return 0;
}
#endif

#endif /* _SAFECRT_DEFINE_MBS_FUNCTIONS */

/* strtok_s */
/* 
 * strtok_s, wcstok_s ;
 * uses _Context to keep track of the position in the string.
 */
_SAFECRT__EXTERN_C
char * __cdecl strtok_s(char *_String, const char *_Control, char **_Context);

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
char * __cdecl strtok_s(char *_String, const char *_Control, char **_Context)
{
    unsigned char *str;
    const unsigned char *ctl = (const unsigned char *)_Control;
    unsigned char map[32];
    int count;
 
    /* validation section */
    _SAFECRT__VALIDATE_POINTER_ERROR_RETURN(_Context, EINVAL, nullptr);
    _SAFECRT__VALIDATE_POINTER_ERROR_RETURN(_Control, EINVAL, nullptr);
    _SAFECRT__VALIDATE_CONDITION_ERROR_RETURN(_String != nullptr || *_Context != nullptr, EINVAL, nullptr);
 
    /* Clear control map */
    for (count = 0; count < 32; count++)
    {
        map[count] = 0;
    }
 
    /* Set bits in delimiter table */
    do {
        map[*ctl >> 3] |= (1 << (*ctl & 7));
    } while (*ctl++);
 
    /* If string is nullptr, set str to the saved
    * pointer (i.e., continue breaking tokens out of the string
    * from the last strtok call) */
    if (_String != nullptr)
    {
        str = (unsigned char *)_String;
    }
    else
    {
        str = (unsigned char *)*_Context;
    }
 
    /* Find beginning of token (skip over leading delimiters). Note that
    * there is no token iff this loop sets str to point to the terminal
    * null (*str == 0) */
    while ((map[*str >> 3] & (1 << (*str & 7))) && *str != 0)
    {
        str++;
    }
 
    _String = (char *)str;
 
    /* Find the end of the token. If it is not the end of the string,
    * put a null there. */
    for ( ; *str != 0 ; str++ )
    {
        if (map[*str >> 3] & (1 << (*str & 7)))
        {
            *str++ = 0;
            break;
        }
    }
 
    /* Update context */
    *_Context = (char *)str;
 
    /* Determine if a token has been found. */
    if (_String == (char *)str)
    {
        return nullptr;
    }
    else
    {
        return _String;
    }
}
#endif

/* wcstok_s */
_SAFECRT__EXTERN_C
WCHAR * __cdecl wcstok_s(WCHAR *_String, const WCHAR *_Control, WCHAR **_Context);

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
WCHAR * __cdecl wcstok_s(WCHAR *_String, const WCHAR *_Control, WCHAR **_Context)
{
    WCHAR *token;
    const WCHAR *ctl;
 
    /* validation section */
    _SAFECRT__VALIDATE_POINTER_ERROR_RETURN(_Context, EINVAL, nullptr);
    _SAFECRT__VALIDATE_POINTER_ERROR_RETURN(_Control, EINVAL, nullptr);
    _SAFECRT__VALIDATE_CONDITION_ERROR_RETURN(_String != nullptr || *_Context != nullptr, EINVAL, nullptr);
 
    /* If string==nullptr, continue with previous string */
    if (!_String)
    {
        _String = *_Context;
    }
 
    /* Find beginning of token (skip over leading delimiters). Note that
    * there is no token iff this loop sets string to point to the terminal null. */
    for ( ; *_String != 0 ; _String++)
    {
        for (ctl = _Control; *ctl != 0 && *ctl != *_String; ctl++)
            ;
        if (*ctl == 0)
        {
            break;
        }
    }
 
    token = _String;
 
    /* Find the end of the token. If it is not the end of the string,
    * put a null there. */
    for ( ; *_String != 0 ; _String++)
    {
        for (ctl = _Control; *ctl != 0 && *ctl != *_String; ctl++)
            ;
        if (*ctl != 0)
        {
            *_String++ = 0;
            break;
        }
    }
 
    /* Update the context */
    *_Context = _String;
 
    /* Determine if a token has been found. */
    if (token == _String)
    {
        return nullptr;
    }
    else
    {
        return token;
    }
}
#endif

/* _mbstok_s */
#if _SAFECRT_DEFINE_MBS_FUNCTIONS

_SAFECRT__EXTERN_C
unsigned char * __cdecl _mbstok_s(unsigned char *_String, const unsigned char *_Control, unsigned char **_Context);

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
unsigned char * __cdecl _mbstok_s(unsigned char *_String, const unsigned char *_Control, unsigned char **_Context)
{
    unsigned char *token;
    const unsigned char *ctl;
    int dbc;
 
    /* validation section */
    _SAFECRT__VALIDATE_POINTER_ERROR_RETURN(_Context, EINVAL, nullptr);
    _SAFECRT__VALIDATE_POINTER_ERROR_RETURN(_Control, EINVAL, nullptr);
    _SAFECRT__VALIDATE_CONDITION_ERROR_RETURN(_String != nullptr || *_Context != nullptr, EINVAL, nullptr);
 
    /* If string==nullptr, continue with previous string */
    if (!_String)
    {
        _String = *_Context;
    }
 
    /* Find beginning of token (skip over leading delimiters). Note that
    * there is no token iff this loop sets string to point to the terminal null. */
    for ( ; *_String != 0; _String++)
    {
        for (ctl = _Control; *ctl != 0; ctl++)
        {
            if (_SAFECRT__ISMBBLEAD(*ctl))
            {
                if (*ctl == *_String && (ctl[1] == 0 || ctl[1] == _String[1]))
                {
                    break;
                }
                ctl++;
            }
            else
            {
                if (*ctl == *_String)
                {
                    break;
                }
            }
        }
        if (*ctl == 0)
        {
            break;
        }
        if (_SAFECRT__ISMBBLEAD(*_String))
        {
            _String++;
            if (*_String == 0)
            {
                break;
            }
        }
    }
 
    token = _String;
 
    /* Find the end of the token. If it is not the end of the string,
    * put a null there. */
    for ( ; *_String != 0; _String++)
    {
        for (ctl = _Control, dbc = 0; *ctl != 0; ctl++)
        {
            if (_SAFECRT__ISMBBLEAD(*ctl))
            {
                if (*ctl == *_String && (ctl[1] == 0 || ctl[1] == _String[1]))
                {
                    dbc = 1;
                    break;
                }
                ctl++;
            }
            else
            {
                if (*ctl == *_String)
                {
                    break;
                }
            }
        }
        if (*ctl != 0)
        {
            *_String++ = 0;
            if (dbc && ctl[1] != 0)
            {
                *_String++ = 0;
            }
            break;
        }
        if (_SAFECRT__ISMBBLEAD(*_String))
        {
            _String++;
            if (*_String == 0)
            {
                break;
            }
        }
    }
 
    /* Update the context */
    *_Context = _String;
 
    /* Determine if a token has been found. */
    if (token == _String)
    {
        return nullptr;
    }
    else
    {
        return token;
    }
}
#endif

#endif /* _SAFECRT_DEFINE_MBS_FUNCTIONS */

#ifndef PAL_STDCPP_COMPAT
/* strnlen */
/*
 * strnlen, wcsnlen ;
 * returns inMaxSize if the null character is not found.
 */
_SAFECRT__EXTERN_C
size_t __cdecl strnlen(const char* inString, size_t inMaxSize);

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL

_SAFECRT__INLINE
size_t __cdecl strnlen(const char* inString, size_t inMaxSize)
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

/* wcsnlen */
_SAFECRT__EXTERN_C
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
#endif // PAL_STDCPP_COMPAT

/* _makepath_s */
/* 
 * _makepath_s, _wmakepath_s build up a path starting from the specified components;
 * will call _SAFECRT_INVALID_PARAMETER if there is not enough space in _Dst;
 * any of _Drive, _Dir, _Filename and _Ext can be nullptr
 */
_SAFECRT__EXTERN_C
errno_t __cdecl _makepath_s(char *_Dst, size_t _SizeInBytes, const char *_Drive, const char *_Dir, const char *_Filename, const char *_Ext);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
errno_t __cdecl _makepath_s(char (&_Dst)[_SizeInBytes], const char *_Drive, const char *_Dir, const char *_Filename, const char *_Ext)
{
    return _makepath_s(_Dst, _SizeInBytes, _Drive, _Dir, _Filename, _Ext);
}
#endif

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _makepath_s(char *_Dst, size_t _SizeInBytes, const char *_Drive, const char *_Dir, const char *_Filename, const char *_Ext)
{
    size_t written;
    const char *p;
    char *d;
 
    /* validation section */
    _SAFECRT__VALIDATE_STRING(_Dst, _SizeInBytes);
 
    /* copy drive */
    written = 0;
    d = _Dst;
    if (_Drive != nullptr && *_Drive != 0)
    {
        written += 2;
        if(written >= _SizeInBytes)
        {
            goto error_return;
        }
        *d++ = *_Drive;
        *d++ = ':';
    }
 
    /* copy dir */
    p = _Dir;
    if (p != nullptr && *p != 0)
    {
        do {
            if(++written >= _SizeInBytes)
            {
                goto error_return;
            }
            *d++ = *p++;
        } while (*p != 0);
 
        p = (const char *)_SAFECRT__MBSDEC((const unsigned char *)_Dir, (const unsigned char *)p);
        if (*p != '/' && *p != '\\')
        {
            if(++written >= _SizeInBytes)
            {
                goto error_return;
            }
            *d++ = '\\';
        }
    }
 
    /* copy fname */
    p = _Filename;
    if (p != nullptr)
    {
        while (*p != 0) 
        {
            if(++written >= _SizeInBytes)
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
        if (*p != 0 && *p != '.')
        {
            if(++written >= _SizeInBytes)
            {
                goto error_return;
            }
            *d++ = '.';
        }
        while (*p != 0)
        {
            if(++written >= _SizeInBytes)
            {
                goto error_return;
            }
            *d++ = *p++;
        }
    }
 
    if(++written > _SizeInBytes)
    {
        goto error_return;
    }
    *d = 0;
    _SAFECRT__FILL_STRING(_Dst, _SizeInBytes, written);
    return 0;
 
error_return:
    _SAFECRT__RESET_STRING(_Dst, _SizeInBytes);
    _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Dst, _SizeInBytes);
    /* should never happen, but compiler can't tell */
    return EINVAL;
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
        if (*p != L'/' && *p != L'\\')
        {
            if(++written >= _SizeInWords)
            {
                goto error_return;
            }
            *d++ = L'\\';
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

/* _splitpath_s */
/* 
 * _splitpath_s, _wsplitpath_s decompose a path into the specified components;
 * will call _SAFECRT_INVALID_PARAMETER if there is not enough space in
 *      any of _Drive, _Dir, _Filename and _Ext;
 * any of _Drive, _Dir, _Filename and _Ext can be nullptr, but the correspondent size must
 *      be set to 0, e.g. (_Drive == nullptr && _DriveSize == 0) is allowed, but
 *      (_Drive == nullptr && _DriveSize != 0) is considered an invalid parameter
 */
_SAFECRT__EXTERN_C
errno_t __cdecl _splitpath_s(
    const char *_Path,
    char *_Drive, size_t _DriveSize,
    char *_Dir, size_t _DirSize,
    char *_Filename, size_t _FilenameSize,
    char *_Ext, size_t _ExtSize
);

/* no C++ overload for _splitpath_s */

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _splitpath_s(
    const char *_Path,
    char *_Drive, size_t _DriveSize,
    char *_Dir, size_t _DirSize,
    char *_Filename, size_t _FilenameSize,
    char *_Ext, size_t _ExtSize
)
{
    const char *tmp;
    const char *last_slash;
    const char *dot;
    int drive_set = 0;
    size_t length = 0;
    int bEinval = 0;
 
    /* validation section */
    _SAFECRT__VALIDATE_POINTER(_Path);
    if ((_Drive == nullptr && _DriveSize != 0) || (_Drive != nullptr && _DriveSize == 0))
    {
        goto error_einval;
    }
    if ((_Dir == nullptr && _DirSize != 0) || (_Dir != nullptr && _DirSize == 0))
    {
        goto error_einval;
    }
    if ((_Filename == nullptr && _FilenameSize != 0) || (_Filename != nullptr && _FilenameSize == 0))
    {
        goto error_einval;
    }
    if ((_Ext == nullptr && _ExtSize != 0) || (_Ext != nullptr && _ExtSize == 0))
    {
        goto error_einval;
    }
 
    /* check if _Path begins with the longpath prefix */
    if (_Path[0] == '\\' && _Path[1] == '\\' && _Path[2] == '?' && _Path[3] == '\\')
    {
        _Path += 4;
    }
 
    /* extract drive letter and ':', if any */
    if (!drive_set)
    {
        size_t skip = _MAX_DRIVE - 2;
        tmp = _Path;
        while (skip > 0 && *tmp != 0)
        {
            skip--;
            tmp++;
        }
        if (*tmp == ':')
        {
            if (_Drive != nullptr)
            {
                if (_DriveSize < _MAX_DRIVE)
                {
                    goto error_erange;
                }
                strncpy_s(_Drive, _DriveSize, _Path, _MAX_DRIVE - 1);
            }
            _Path = tmp + 1;
        }
        else
        {
            if (_Drive != nullptr)
            {
                _SAFECRT__RESET_STRING(_Drive, _DriveSize);
            }
        }
    }
 
    /* extract path string, if any. _Path now points to the first character
     * of the path, if any, or the filename or extension, if no path was
     * specified.  Scan ahead for the last occurence, if any, of a '/' or
     * '\' path separator character.  If none is found, there is no path.
     * We will also note the last '.' character found, if any, to aid in
     * handling the extension.
     */
    last_slash = nullptr;
    dot = nullptr;
    tmp = _Path;
    for (; *tmp != 0; ++tmp)
    {
#if _SAFECRT_DEFINE_MBS_FUNCTIONS
#pragma warning(push)
#pragma warning(disable:4127)
        if (_SAFECRT__ISMBBLEAD(*tmp))
#pragma warning(pop)
#else
        if (0)
#endif
        {
            tmp++;
        }
        else 
        {
            if (*tmp == '/' || *tmp == '\\')
            {
                /* point to one beyond for later copy */
                last_slash = tmp + 1;
            }
            else if (*tmp == '.')
            {
                dot = tmp;
            }
        }
    }
 
    if (last_slash != nullptr) 
    {
        /* found a path - copy up through last_slash or max characters
         * allowed, whichever is smaller
         */
        if (_Dir != nullptr) {
            length = (size_t)(last_slash - _Path);
            if (_DirSize <= length)
            {
                goto error_erange;
            }
            strncpy_s(_Dir, _DirSize, _Path, length);
        }
        _Path = last_slash;
    }
    else
    {
        /* there is no path */
        if (_Dir != nullptr)
        {
            _SAFECRT__RESET_STRING(_Dir, _DirSize);
        }
    }
 
    /* extract file name and extension, if any.  Path now points to the
     * first character of the file name, if any, or the extension if no
     * file name was given.  Dot points to the '.' beginning the extension,
     * if any.
     */
    if (dot != nullptr && (dot >= _Path))
    {
        /* found the marker for an extension - copy the file name up to the '.' */
        if (_Filename)
        {
            length = (size_t)(dot - _Path);
            if (_FilenameSize <= length)
            {
                goto error_erange;
            }
            strncpy_s(_Filename, _FilenameSize, _Path, length);
        }
        /* now we can get the extension - remember that tmp still points
         * to the terminating nullptr character of path.
         */
        if (_Ext)
        {
            length = (size_t)(tmp - dot);
            if (_ExtSize <= length)
            {
                goto error_erange;
            }
            strncpy_s(_Ext, _ExtSize, dot, length);
        }
    }
    else
    {
        /* found no extension, give empty extension and copy rest of
         * string into fname.
         */
        if (_Filename)
        {
            length = (size_t)(tmp - _Path);
            if (_FilenameSize <= length)
            {
                goto error_erange;
            }
            strncpy_s(_Filename, _FilenameSize, _Path, length);
        }
        if (_Ext)
        {
            _SAFECRT__RESET_STRING(_Ext, _ExtSize);
        }
    }
 
    return 0;
 
error_einval:
    bEinval = 1;
 
error_erange:
    if (_Drive != nullptr && _DriveSize > 0)
    {
        _SAFECRT__RESET_STRING(_Drive, _DriveSize);
    }
    if (_Dir != nullptr && _DirSize > 0)
    {
        _SAFECRT__RESET_STRING(_Dir, _DirSize);
    }
    if (_Filename != nullptr && _FilenameSize > 0)
    {
        _SAFECRT__RESET_STRING(_Filename, _FilenameSize);
    }
    if (_Ext != nullptr && _ExtSize > 0)
    {
        _SAFECRT__RESET_STRING(_Ext, _ExtSize);
    }
 
    if (bEinval)
    {
        _SAFECRT__RETURN_EINVAL;
    }
 
    _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Strings, _StringSizes);
    /* should never happen, but compiler can't tell */
    return EINVAL;
}
#endif

/* _wsplitpath_s */
_SAFECRT__EXTERN_C
errno_t __cdecl _wsplitpath_s(
    const WCHAR *_Path,
    WCHAR *_Drive, size_t _DriveSize,
    WCHAR *_Dir, size_t _DirSize,
    WCHAR *_Filename, size_t _FilenameSize,
    WCHAR *_Ext, size_t _ExtSize
);

/* no C++ overload for _wsplitpath_s */

#if _SAFECRT_USE_INLINES || _SAFECRT_IMPL
 
_SAFECRT__INLINE
errno_t __cdecl _wsplitpath_s(
    const WCHAR *_Path,
    WCHAR *_Drive, size_t _DriveSize,
    WCHAR *_Dir, size_t _DirSize,
    WCHAR *_Filename, size_t _FilenameSize,
    WCHAR *_Ext, size_t _ExtSize
)
{
    const WCHAR *tmp;
    const WCHAR *last_slash;
    const WCHAR *dot;
    int drive_set = 0;
    size_t length = 0;
    int bEinval = 0;
 
    /* validation section */
    _SAFECRT__VALIDATE_POINTER(_Path);
    if ((_Drive == nullptr && _DriveSize != 0) || (_Drive != nullptr && _DriveSize == 0))
    {
        goto error_einval;
    }
    if ((_Dir == nullptr && _DirSize != 0) || (_Dir != nullptr && _DirSize == 0))
    {
        goto error_einval;
    }
    if ((_Filename == nullptr && _FilenameSize != 0) || (_Filename != nullptr && _FilenameSize == 0))
    {
        goto error_einval;
    }
    if ((_Ext == nullptr && _ExtSize != 0) || (_Ext != nullptr && _ExtSize == 0))
    {
        goto error_einval;
    }
 
    /* check if _Path begins with the longpath prefix */
    if (_Path[0] == L'\\' && _Path[1] == L'\\' && _Path[2] == L'?' && _Path[3] == L'\\')
    {
        _Path += 4;
    }
 
    /* extract drive letter and ':', if any */
    if (!drive_set)
    {
        size_t skip = _MAX_DRIVE - 2;
        tmp = _Path;
        while (skip > 0 && *tmp != 0)
        {
            skip--;
            tmp++;
        }
        if (*tmp == L':')
        {
            if (_Drive != nullptr)
            {
                if (_DriveSize < _MAX_DRIVE)
                {
                    goto error_erange;
                }
                wcsncpy_s(_Drive, _DriveSize, _Path, _MAX_DRIVE - 1);
            }
            _Path = tmp + 1;
        }
        else
        {
            if (_Drive != nullptr)
            {
                _SAFECRT__RESET_STRING(_Drive, _DriveSize);
            }
        }
    }
 
    /* extract path string, if any. _Path now points to the first character
     * of the path, if any, or the filename or extension, if no path was
     * specified.  Scan ahead for the last occurence, if any, of a '/' or
     * '\' path separator character.  If none is found, there is no path.
     * We will also note the last '.' character found, if any, to aid in
     * handling the extension.
     */
    last_slash = nullptr;
    dot = nullptr;
    tmp = _Path;
    for (; *tmp != 0; ++tmp)
    {
        {
            if (*tmp == L'/' || *tmp == L'\\')
            {
                /* point to one beyond for later copy */
                last_slash = tmp + 1;
            }
            else if (*tmp == L'.')
            {
                dot = tmp;
            }
        }
    }
 
    if (last_slash != nullptr) 
    {
        /* found a path - copy up through last_slash or max characters
         * allowed, whichever is smaller
         */
        if (_Dir != nullptr) {
            length = (size_t)(last_slash - _Path);
            if (_DirSize <= length)
            {
                goto error_erange;
            }
            wcsncpy_s(_Dir, _DirSize, _Path, length);
        }
        _Path = last_slash;
    }
    else
    {
        /* there is no path */
        if (_Dir != nullptr)
        {
            _SAFECRT__RESET_STRING(_Dir, _DirSize);
        }
    }
 
    /* extract file name and extension, if any.  Path now points to the
     * first character of the file name, if any, or the extension if no
     * file name was given.  Dot points to the '.' beginning the extension,
     * if any.
     */
    if (dot != nullptr && (dot >= _Path))
    {
        /* found the marker for an extension - copy the file name up to the '.' */
        if (_Filename)
        {
            length = (size_t)(dot - _Path);
            if (_FilenameSize <= length)
            {
                goto error_erange;
            }
            wcsncpy_s(_Filename, _FilenameSize, _Path, length);
        }
        /* now we can get the extension - remember that tmp still points
         * to the terminating nullptr character of path.
         */
        if (_Ext)
        {
            length = (size_t)(tmp - dot);
            if (_ExtSize <= length)
            {
                goto error_erange;
            }
            wcsncpy_s(_Ext, _ExtSize, dot, length);
        }
    }
    else
    {
        /* found no extension, give empty extension and copy rest of
         * string into fname.
         */
        if (_Filename)
        {
            length = (size_t)(tmp - _Path);
            if (_FilenameSize <= length)
            {
                goto error_erange;
            }
            wcsncpy_s(_Filename, _FilenameSize, _Path, length);
        }
        if (_Ext)
        {
            _SAFECRT__RESET_STRING(_Ext, _ExtSize);
        }
    }
 
    return 0;
 
error_einval:
    bEinval = 1;
 
error_erange:
    if (_Drive != nullptr && _DriveSize > 0)
    {
        _SAFECRT__RESET_STRING(_Drive, _DriveSize);
    }
    if (_Dir != nullptr && _DirSize > 0)
    {
        _SAFECRT__RESET_STRING(_Dir, _DirSize);
    }
    if (_Filename != nullptr && _FilenameSize > 0)
    {
        _SAFECRT__RESET_STRING(_Filename, _FilenameSize);
    }
    if (_Ext != nullptr && _ExtSize > 0)
    {
        _SAFECRT__RESET_STRING(_Ext, _ExtSize);
    }
 
    if (bEinval)
    {
        _SAFECRT__RETURN_EINVAL;
    }
 
    _SAFECRT__RETURN_BUFFER_TOO_SMALL(_Strings, _StringSizes);
    /* should never happen, but compiler can't tell */
    return EINVAL;
}
#endif

/* sprintf_s, vsprintf_s */
/* 
 * sprintf_s, swprintf_s, vsprintf_s, vswprintf_s format a string and copy it into _Dst;
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
int __cdecl sprintf_s(char *_Dst, size_t _SizeInBytes, const char *_Format, ...);
_SAFECRT__EXTERN_C
int __cdecl vsprintf_s(char *_Dst, size_t _SizeInBytes, const char *_Format, va_list _ArgList);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInBytes>
inline
int __cdecl sprintf_s(char (&_Dst)[_SizeInBytes], const char *_Format, ...)
{
    int ret;
    va_list _ArgList;
    va_start(_ArgList, _Format);
    ret = vsprintf_s(_Dst, _SizeInBytes, _Format, _ArgList);
    va_end(_ArgList);
    return ret;
}

template <size_t _SizeInBytes>
inline
int __cdecl vsprintf_s(char (&_Dst)[_SizeInBytes], const char *_Format, va_list _ArgList)
{
    return vsprintf_s(_Dst, _SizeInBytes, _Format, _ArgList);
}
#endif

/* no inline version of sprintf_s, vsprintf_s */

/* swprintf_s, vswprintf_s */
_SAFECRT__EXTERN_C
int __cdecl swprintf_s(WCHAR *_Dst, size_t _SizeInWords, const WCHAR *_Format, ...);
_SAFECRT__EXTERN_C
int __cdecl vswprintf_s(WCHAR *_Dst, size_t _SizeInWords, const WCHAR *_Format, va_list _ArgList);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInWords>
inline
int __cdecl swprintf_s(WCHAR (&_Dst)[_SizeInWords], const WCHAR *_Format, ...)
{
    int ret;
    va_list _ArgList;
    va_start(_ArgList, _Format);
    ret = vswprintf_s(_Dst, _SizeInWords, _Format, _ArgList);
    va_end(_ArgList);
    return ret;
}

template <size_t _SizeInWords>
inline
int __cdecl vswprintf_s(WCHAR (&_Dst)[_SizeInWords], const WCHAR *_Format, va_list _ArgList)
{
    return vswprintf_s(_Dst, _SizeInWords, _Format, _ArgList);
}
#endif

/* no inline version of swprintf_s, vswprintf_s */

/* _snprintf_s, _vsnprintf_s */
/* 
 * _snprintf_s, _snwprintf_s, _vsnprintf_s, _vsnwprintf_s format a string and copy at max _Count characters into _Dst;
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
int __cdecl _snprintf_s(char (&_Dst)[_SizeInBytes], size_t _Count, const char *_Format, ...)
{
    int ret;
    va_list _ArgList;
    va_start(_ArgList, _Format);
    ret = _vsnprintf_s(_Dst, _SizeInBytes, _Count, _Format, _ArgList);
    va_end(_ArgList);
    return ret;
}

template <size_t _SizeInBytes>
inline
int __cdecl _vsnprintf_s(char (&_Dst)[_SizeInBytes], size_t _Count, const char *_Format, va_list _ArgList)
{
    return _vsnprintf_s(_Dst, _SizeInBytes, _Count, _Format, _ArgList);
}
#endif

/* no inline version of _snprintf_s, _vsnprintf_s */

/* _snwprintf_s, _vsnwprintf_s */
_SAFECRT__EXTERN_C
int __cdecl _vsnwprintf_s(WCHAR *_Dst, size_t _SizeInWords, size_t _Count, const WCHAR *_Format, va_list _ArgList);

#if defined(__cplusplus) && _SAFECRT_USE_CPP_OVERLOADS
template <size_t _SizeInWords>
inline
int __cdecl _snwprintf_s(WCHAR (&_Dst)[_SizeInWords], size_t _Count, const WCHAR *_Format, ...)
{
    int ret;
    va_list _ArgList;
    va_start(_ArgList, _Format);
    ret = _vsnwprintf_s(_Dst, _SizeInWords, _Count, _Format, _ArgList);
    va_end(_ArgList);
    return ret;
}

template <size_t _SizeInWords>
inline
int __cdecl _vsnwprintf_s(char (&_Dst)[_SizeInWords], size_t _Count, const char *_Format, va_list _ArgList)
{
    return _vsnwprintf_s(_Dst, _SizeInWords, _Count, _Format, _ArgList);
}
#endif

/* no inline version of _snwprintf_s, _vsnwprintf_s */

/* scanf_s */
/*
 * read formatted data from the standard input stream;
 * need safecrt.lib and msvcrt.dll;
 * will call _SAFECRT_INVALID_PARAMETER if the format string is malformed;
 * for format types %s, %S, %[, %c and %C, in the argument list the buffer pointer
 *      need to be followed by the size of the buffer, e.g.:
 *          #define BUFFSIZE 100
 *          char buff[BUFFSIZE];
 *          scanf_s("%s", buff, BUFFSIZE);
 * as scanf, returns the number of fields successfully converted and assigned;
 * if a buffer field is too small, scanf set the buffer to the empty string and returns.
 * do not support floating-point, for now
 */
_SAFECRT__EXTERN_C
int __cdecl scanf_s(const char *_Format, ...);

/* no C++ overload for scanf_s */

/* no inline version of scanf_s */

/* wscanf_s */
_SAFECRT__EXTERN_C
int __cdecl wscanf_s(const WCHAR *_Format, ...);

/* no C++ overload for wscanf_s */

/* no inline version of wscanf_s */

/* sscanf_s */
_SAFECRT__EXTERN_C
int __cdecl sscanf_s(const char *_String, const char *_Format, ...);

/* no C++ overload for sscanf_s */

/* no inline version of sscanf_s */

/* swscanf_s */
_SAFECRT__EXTERN_C
int __cdecl swscanf_s(const WCHAR *_String, const WCHAR *_Format, ...);

/* no C++ overload for swscanf_s */

/* no inline version of swscanf_s */

/* _snscanf_s */
_SAFECRT__EXTERN_C
int __cdecl _snscanf_s(const char *_String, size_t _Count, const char *_Format, ...);

/* no C++ overload for snscanf_s */

/* no inline version of snscanf_s */

/* _swnscanf_s */
_SAFECRT__EXTERN_C
int __cdecl _swnscanf_s(const WCHAR *_String, size_t _Count, const WCHAR *_Format, ...);

/* no C++ overload for _swnscanf_s */

/* no inline version of _swnscanf_s */

//#endif /* ndef _SAFECRT_IMPL */

#endif  /* _INC_SAFECRT */
