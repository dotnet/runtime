// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*   mbusafecrt.h - public declarations for SafeCRT lib
*

*
*   Purpose:
*       This file contains the public declarations SafeCRT
*       functions ported to MacOS. These are the safe versions of
*       functions standard functions banned by SWI
*

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

#ifndef THROW_DECL
#if defined(_MSC_VER) || defined(__llvm__) || !defined(__cplusplus)
#define THROW_DECL
#else
#define THROW_DECL throw()
#endif // !_MSC_VER
#endif // !THROW_DECL

#ifdef __cplusplus
    extern "C" {
#endif

typedef void ( *tSafeCRT_AssertFuncPtr )( const char* inExpression, const char* inComment, const char* inFile, const unsigned long inLineNum );
void MBUSafeCRTSetAssertFunc( tSafeCRT_AssertFuncPtr inAssertFuncPtr );

extern errno_t strcat_s( char* ioDest, size_t inDestBufferSize, const char* inSrc );
extern errno_t wcscat_s( WCHAR* ioDest, size_t inDestBufferSize, const WCHAR* inSrc );

extern errno_t strncat_s( char* ioDest, size_t inDestBufferSize, const char* inSrc, size_t inCount );
extern errno_t wcsncat_s( WCHAR* ioDest, size_t inDestBufferSize, const WCHAR* inSrc, size_t inCount );

extern errno_t strcpy_s( char* outDest, size_t inDestBufferSize, const char* inSrc );
extern errno_t wcscpy_s( WCHAR* outDest, size_t inDestBufferSize, const WCHAR* inSrc );

extern errno_t strncpy_s( char* outDest, size_t inDestBufferSize, const char* inSrc, size_t inCount );
extern errno_t wcsncpy_s( WCHAR* outDest, size_t inDestBufferSize, const WCHAR* inSrc, size_t inCount );

extern char* strtok_s( char* inString, const char* inControl, char** ioContext );
extern WCHAR* wcstok_s( WCHAR* inString, const WCHAR* inControl, WCHAR** ioContext );

// strnlen is not required unless the source string is completely untrusted (e.g. anonymous input on a website)
#ifndef SUPPRESS_STRNLEN
    extern size_t PAL_strnlen( const char* inString, size_t inMaxSize );
    extern size_t PAL_wcsnlen( const WCHAR* inString, size_t inMaxSize );
#endif

extern errno_t _itoa_s( int inValue, char* outBuffer, size_t inDestBufferSize, int inRadix );
extern errno_t _itow_s( int inValue, WCHAR* outBuffer, size_t inDestBufferSize, int inRadix );

extern errno_t _ltoa_s( long inValue, char* outBuffer, size_t inDestBufferSize, int inRadix );
extern errno_t _ltow_s( long inValue, WCHAR* outBuffer, size_t inDestBufferSize, int inRadix );

extern errno_t _ultoa_s( unsigned long inValue, char* outBuffer, size_t inDestBufferSize, int inRadix );
extern errno_t _ultow_s( unsigned long inValue, WCHAR* outBuffer, size_t inDestBufferSize, int inRadix );

extern errno_t _i64tow_s( long long inValue, WCHAR* outBuffer, size_t inDestBufferSize, int inRadix );

extern errno_t _makepath_s( char* outDest, size_t inDestBufferSize, const char* inDrive, const char* inDirectory, const char* inFilename, const char* inExtension );
extern errno_t _wmakepath_s( WCHAR* outDest, size_t inDestBufferSize, const WCHAR* inDrive, const WCHAR* inDirectory, const WCHAR* inFilename, const WCHAR* inExtension );

extern errno_t _splitpath_s( const char* inPath, char* outDrive, size_t inDriveSize, char* outDirectory, size_t inDirectorySize, char* outFilename, size_t inFilenameSize, char* outExtension, size_t inExtensionSize );
extern errno_t _wsplitpath_s( const WCHAR* inPath, WCHAR* outDrive, size_t inDriveSize, WCHAR* outDirectory, size_t inDirectorySize, WCHAR* outFilename, size_t inFilenameSize, WCHAR* outExtension, size_t inExtensionSize );

extern int sprintf_s( char *string, size_t sizeInBytes, const char *format, ... );

extern int _snprintf_s( char *string, size_t sizeInBytes, size_t count, const char *format, ... );

extern int vsprintf_s( char* string, size_t sizeInBytes, const char* format, va_list arglist );
extern int _vsnprintf_s( char* string, size_t sizeInBytes, size_t count, const char* format, va_list arglist );

extern int sscanf_s( const char *string, const char *format, ... );
extern int swscanf_s( const WCHAR *string, const WCHAR *format, ... );

extern errno_t memcpy_s( void * dst, size_t sizeInBytes, const void * src, size_t count ) THROW_DECL;
extern errno_t memmove_s( void * dst, size_t sizeInBytes, const void * src, size_t count );

extern errno_t _wcslwr_s(char16_t *string, size_t sz);

#ifdef __cplusplus
    }
#endif

#endif	/* MBUSAFECRT_H */
