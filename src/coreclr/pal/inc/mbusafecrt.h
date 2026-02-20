// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*   mbusafecrt.h - public declarations for SafeCRT lib
*
*   Purpose:
*       This file contains the public declarations SafeCRT
*       functions ported to MacOS. These are the safe versions of
*       functions standard functions banned by SWI
****/

/* shields! */

#ifndef MBUSAFECRT_H
#define MBUSAFECRT_H

//#include <wchar.h>

/* MacOS does not define a specifc type for errnos, but SafeCRT does */
typedef int errno_t;

/* errno value that specific to SafeCRT */
#define STRUNCATE       80

// define the return value for success
#define SAFECRT_SUCCESS 0

#ifdef __cplusplus
    extern "C" {
#endif

extern errno_t strcat_s( char* ioDest, size_t inDestBufferSize, const char* inSrc );
extern errno_t wcscat_s( WCHAR* ioDest, size_t inDestBufferSize, const WCHAR* inSrc );

extern errno_t strncat_s( char* ioDest, size_t inDestBufferSize, const char* inSrc, size_t inCount );
extern errno_t wcsncat_s( WCHAR* ioDest, size_t inDestBufferSize, const WCHAR* inSrc, size_t inCount );

extern errno_t strcpy_s( char* outDest, size_t inDestBufferSize, const char* inSrc );
extern errno_t wcscpy_s( WCHAR* outDest, size_t inDestBufferSize, const WCHAR* inSrc );

extern errno_t strncpy_s( char* outDest, size_t inDestBufferSize, const char* inSrc, size_t inCount );
extern errno_t wcsncpy_s( WCHAR* outDest, size_t inDestBufferSize, const WCHAR* inSrc, size_t inCount );

extern size_t PAL_wcsnlen( const WCHAR* inString, size_t inMaxSize );

extern errno_t _wmakepath_s( WCHAR* outDest, size_t inDestBufferSize, const WCHAR* inDrive, const WCHAR* inDirectory, const WCHAR* inFilename, const WCHAR* inExtension );

extern int sprintf_s( char *string, size_t sizeInBytes, const char *format, ... );

extern int _snprintf_s( char *string, size_t sizeInBytes, size_t count, const char *format, ... );

extern int vsprintf_s( char* string, size_t sizeInBytes, const char* format, va_list arglist );
extern int _vsnprintf_s( char* string, size_t sizeInBytes, size_t count, const char* format, va_list arglist );

extern int sscanf_s( const char *string, const char *format, ... );

extern errno_t memcpy_s( void * dst, size_t sizeInBytes, const void * src, size_t count );
extern errno_t memmove_s( void * dst, size_t sizeInBytes, const void * src, size_t count );

extern errno_t _wcslwr_s(char16_t *string, size_t sz);

#ifdef __cplusplus
    }
#endif

#endif	/* MBUSAFECRT_H */
