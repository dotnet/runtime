//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// CrtWrap.h
//
// Wrapper code for the C runtime library.
//
//*****************************************************************************

#ifndef __CrtWrap_h__
#define __CrtWrap_h__

//workaround  Remove the crt wrapper to incorporate SecureCRT functions.
#ifdef NO_CRT
#define REDEFINE_NO_CRT
#undef NO_CRT
#endif

//*****************************************************************************
// If the CRT is allowed in the current compiland, then just include the
// correct CRT header files.
//*****************************************************************************
#ifndef NO_CRT

#include <windows.h>
#include <objbase.h>
#include <tchar.h>
#include "debugmacros.h"
#include <stdlib.h>
#include <malloc.h>
#include <wchar.h>
#include <stdio.h>

//*****************************************************************************
// Else no CRT references are allowed.  Provide stubs and macros for common
// functionality, and otherwise abstract the CRT from the user.
//*****************************************************************************
#else // NO_CRT

// Fake out include directive on stdlib.h.
#ifdef _INC_STDLIB
#error "Include crtwrap.h before any other include files."
#endif
#undef _INC_STDLIB
#define _INC_STDLIB

#ifdef _INC_MALLOC
#error "Include crtwrap.h before any other include files."
#endif
#undef _INC_MALLOC
#define _INC_MALLOC

#ifdef _INC_TIME
#error "Include crtwrap.h before any other include files."
#endif
#undef _INC_TIME
#define _INC_TIME

#ifdef _INC_STDIO
#error "Include crtwrap.h before any other include files."
#endif
#undef _INC_STDIO
#define _INC_STDIO


#if !defined( _CRTBLD ) && defined( _DLL )
#define _CRTIMP_TODO __declspec(dllimport)
#else
#define _CRTIMP_TODO
#endif

#ifndef _CONST_RETURN
#ifdef __cplusplus
#define _CONST_RETURN const
#else
#define _CONST_RETURN
#endif
#endif


#include <windows.h>
#include <objbase.h>
#include <intrinsic.h>
#include "debugmacros.h"



/*
 * Sizes for buffers used by the _makepath() and _splitpath() functions.
 * note that the sizes include space for 0-terminator
 */
#ifndef _MAC
#define _MAX_PATH   260 /* max. length of full pathname */
#define _MAX_DRIVE  3   /* max. length of drive component */
#define _MAX_DIR    256 /* max. length of path component */
#define _MAX_FNAME  256 /* max. length of file name component */
#define _MAX_EXT    256 /* max. length of extension component */
#else   /* def _MAC */
#define _MAX_PATH   256 /* max. length of full pathname */
#define _MAX_DIR    32  /* max. length of path component */
#define _MAX_FNAME  64  /* max. length of file name component */
#endif  /* _MAC */


#ifndef __min
#define __min(x, y) ((x) < (y) ? (x) : (y))
#endif
#ifndef __max
#define __max(x, y) ((x) > (y) ? (x) : (y))
#endif


#define sprintf     wsprintfA
#define vsprintf    wvsprintfA

#define _stricmp(s1, s2, slen) (SString::_stricmp(s1, s2))
#define _strnicmp(s1, s2, slen) (SString::_strnicmp(s1, s2, slen))

#if defined(UNICODE) || defined(_UNICODE)

#define _tcscat     wcscat
#define _tcslen     wcslen
#define _tcscmp     wcscmp
#define _tcsicmp    _wcsicmp
#define _tcsncmp(s1, s2, slen)  memcmp(s1, s2, (slen) * sizeof(wchar_t))
#define _tcsnccmp(s1, s2, slen)   memcmp(s1, s2, (slen) * sizeof(wchar_t))
#define _tcsnicmp   _wcsnicmp
#define _tcsncicmp  _wcsnicmp
#define _tprintf    wprintf
#define _stprintf   swprintf
#define _tcscpy     wcscpy
#define _tcsncpy(s1, s2, slen)  memcpy(s1, s2, (slen) * sizeof(wchar_t))

#else   // Note: you really are supposed to be using UNICODE here

#define _tcscat     strcat
#define _tcslen     strlen
#define _tcscmp     strcmp
#define _tcsicmp    _stricmp
#define _tcsncmp(s1, s2, slen)  memcmp(s1, s2, (slen))
#define _tcsnccmp(s1, s2, slen)   memcmp(s1, s2, (slen))
#define _tcsnicmp   _strnicmp
#define _tcsncicmp  _strnicmp
#define _tprintf    printf
#define _stprintf   sprintf
#define _tcscpy     strcpy
#define _tcsncpy(s1, s2, slen)  memcpy(s1, s2, slen)

#endif


#ifdef __cplusplus
extern "C"{
#endif 


// Memory.
void    __cdecl free(void *);
void *  __cdecl malloc(size_t);
void *  __cdecl realloc(void *, size_t);
void *  __cdecl _alloca(size_t);
size_t  __cdecl _msize(void *);
void *  __cdecl _expand(void *, size_t);
void * __cdecl calloc(size_t num, size_t size);


#if !__STDC__
/* Non-ANSI names for compatibility */
#define alloca  _alloca
#endif  /* !__STDC__ */

#if defined (_M_MRX000) || defined (_M_PPC) || defined (_M_ALPHA)
#pragma intrinsic(_alloca)
#endif  /* defined (_M_MRX000) || defined (_M_PPC) || defined (_M_ALPHA) */


// Time.

#ifndef _TIME_T_DEFINED
#if     _INTEGRAL_MAX_BITS >= 64
typedef __int64   time_t;       /* time value */
#else
typedef int time_t;            /* time value */
#endif
#define _TIME_T_DEFINED         /* avoid multiple def's of time_t */
#endif

// 4 byte time, no check for daylight savings
_CRTIMP time_t __cdecl time(time_t *timeptr);

// Strings.
_CRTIMP int __cdecl _vsnwprintf(__inout_ecount(iSize) wchar_t *szOutput, size_t iSize, const wchar_t *szFormat, va_list args);
_CRTIMP int __cdecl _vswprintf_c(__inout_ecount(iSize) wchar_t *szOutput, size_t iSize, const wchar_t *szFormat, va_list args);
_CRTIMP int __cdecl wprintf(const wchar_t *format, ...);
_CRTIMP int __cdecl _snwprintf(__inout_ecount(iSize) wchar_t *szOutput, size_t iSize, const wchar_t *szFormat, ...);
_CRTIMP int __cdecl _snprintf(__inout_ecount(iSize) char *szOutput, size_t iSize, const char *szFormat, ...);
#ifndef _PREFAST_
    // Prefast does  not like these because of the const return.  
_CRTIMP _CONST_RETURN wchar_t * __cdecl wcsrchr(const wchar_t * string, wchar_t ch);
_CRTIMP _CONST_RETURN wchar_t * __cdecl wcsstr(const wchar_t * wcs1, const wchar_t * wcs2);
_CRTIMP _CONST_RETURN wchar_t * __cdecl wcspbrk(const wchar_t *, const wchar_t *);
#endif
_CRTIMP int __cdecl _swprintf_c(__inout_ecount(iSize) wchar_t *szOutput, size_t iSize, const wchar_t *szFormat, ...);
_CRTIMP int __cdecl wcstol(const wchar_t *, __in wchar_t **, int);
_CRTIMP unsigned int __cdecl wcstoul(const wchar_t *, __in wchar_t **, int);
_CRTIMP __int64   __cdecl _wcstoi64(const wchar_t *, __in wchar_t **, int);
_CRTIMP unsigned __int64  __cdecl _wcstoui64(const wchar_t *, __in wchar_t **, int);

_CRTIMP int __cdecl _vsnprintf(__inout_ecount(iSize) char *szOutput, size_t iSize, const char *szFormat, va_list args);
_CRTIMP int __cdecl vprintf(const char *, va_list);
_CRTIMP int __cdecl printf(const char *, ...);

#define swprintf    _swprintf_c
#define vswprintf   _vswprintf_c

#ifdef __cplusplus
#ifndef _CPP_WIDE_INLINES_DEFINED
#define _CPP_WIDE_INLINES_DEFINED
extern "C++" {
inline wchar_t * __cdecl wcschr(__in wchar_t *_S, wchar_t _C)
        {return ((wchar_t *)wcschr((const wchar_t *)_S, _C)); }
inline wchar_t * __cdecl wcspbrk(__in wchar_t *_S, const wchar_t *_P)
        {return ((wchar_t *)wcspbrk((const wchar_t *)_S, _P)); }
inline wchar_t * __cdecl wcsrchr(__in wchar_t *_S, wchar_t _C)
        {return ((wchar_t *)wcsrchr((const wchar_t *)_S, _C)); }
inline wchar_t * __cdecl wcsstr(__in wchar_t *_S, const wchar_t *_P)
        {return ((wchar_t *)wcsstr((const wchar_t *)_S, _P)); }
}
#endif
#endif

// Utilities.
unsigned int __cdecl _rotl(unsigned int, int);
unsigned int __cdecl _rotr(unsigned int, int);
unsigned int __cdecl _lrotl(unsigned int, int);
unsigned int __cdecl _lrotr(unsigned int, int);

_CRTIMP int __cdecl atol(const char *nptr);
_CRTIMP int __cdecl atoi(const char *nptr);
_CRTIMP __int64 __cdecl _atoi64(const char *nptr);
_CRTIMP char *__cdecl _ltoa( int value, __inout char *string, int radix );

_CRTIMP int __cdecl _wtoi(const wchar_t *);
_CRTIMP int __cdecl _wtol(const wchar_t *);
_CRTIMP __int64   __cdecl _wtoi64(const wchar_t *);
_CRTIMP wchar_t * __cdecl _ltow (int, __inout wchar_t *, int);

_CRTIMP void __cdecl qsort(void *base, unsigned num, unsigned width,
    int (__cdecl *comp)(const void *, const void *));

#ifdef _CRT_DEPENDENCY_

#define EOF     (-1)

#ifndef _FILE_DEFINED
struct _iobuf {
        char *_ptr;
        int   _cnt;
        char *_base;
        int   _flag;
        int   _file;
        int   _charbuf;
        int   _bufsiz;
        char *_tmpfname;
        };
typedef struct _iobuf FILE;
#define _FILE_DEFINED
#endif

#define _IOB_ENTRIES 20

#ifndef _STDIO_DEFINED
_CRTIMP_TODO extern FILE _iob[];
#endif  /* _STDIO_DEFINED */

#define stdin  (&_iob[0])
#define stdout (&_iob[1])
#define stderr (&_iob[2])

_CRTIMP_TODO FILE * __cdecl fopen(const char *, const char *);
_CRTIMP_TODO FILE * __cdecl _wfopen(const wchar_t *, const wchar_t *);
_CRTIMP_TODO size_t __cdecl fwrite(const void *, size_t, size_t, FILE *);
_CRTIMP_TODO int __cdecl ftell(FILE *);
_CRTIMP_TODO int __cdecl fprintf(FILE *, const char *, ...);
_CRTIMP_TODO int __cdecl fflush(FILE *);
_CRTIMP_TODO int __cdecl fclose(FILE *);


#endif // _CRT_DEPENDENCY_


#ifdef __cplusplus
}
#endif 



#ifdef __cplusplus

void* __cdecl operator new(size_t cb);
void __cdecl operator delete(void *p);

#endif // __cplusplus


#endif // NO_CRT

#ifndef PUB
// PUB is defined to influence method visibility for some compilers.
#define PUB
#endif // !PUB

#ifdef REDEFINE_NO_CRT
#undef REDEFINE_NO_CRT
#define NO_CRT 1
#endif

#endif // __CrtWrap_h__

