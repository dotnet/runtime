// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  palsuite.h
**
** Purpose: Define constants and implement functions that are useful to
**          multiple function categories. If common functions are useful
**          only amongst the test cases for a particular function, a separate
**          header file is placed in the root of those test cases.
**
**
**==========================================================================*/

#ifndef __PALSUITE_H__
#define __PALSUITE_H__

#ifndef __cplusplus
typedef unsigned short char16_t;
#endif

#include <pal_assert.h>
#include <pal.h>
#include <palprivate.h>
#include <minipal/utils.h>
#include <minipal/types.h>
#include <minipal/time.h>
#include <errno.h>

#define PALTEST(testfunc, testname) \
 int __cdecl testfunc(int argc, char* argv[]); \
 static PALTest testfunc##_lookup(testfunc, testname); \
 int __cdecl testfunc(int argc, char* argv[]) \

enum
{
    PASS = 0,
    FAIL = 1
};

inline void Trace(const char *format, ...)
{
    va_list arglist;

    va_start(arglist, format);

    vprintf(format, arglist);

    va_end(arglist);
}

inline void Fail(const char *format, ...)
{
    va_list arglist;

    va_start(arglist, format);

    vprintf(format, arglist);

    va_end(arglist);
    printf("\n");

    // This will exit the test process
    PAL_TerminateEx(FAIL);
}

typedef int __cdecl(*PALTestEntrypoint)(int argc, char*[]);

struct PALTest
{
    static PALTest* s_tests;
    PALTest *_next;
    PALTestEntrypoint _entrypoint;
    const char *_name;
    PALTest(PALTestEntrypoint entrypoint, const char *entrypointName)
    {
        _entrypoint = entrypoint;
        _name = entrypointName;
        _next = s_tests;
        s_tests = this;
    }
};

#ifdef BIGENDIAN
inline ULONG   VAL32(ULONG x)
{
    return( ((x & 0xFF000000L) >> 24) |
            ((x & 0x00FF0000L) >>  8) |
            ((x & 0x0000FF00L) <<  8) |
            ((x & 0x000000FFL) << 24) );
}
#define th_htons(w)  (w)
#else   // BIGENDIAN
#define VAL32(x)    (x)
#define th_htons(w)  (((w) >> 8) | ((w) << 8))
#endif  // BIGENDIAN

WCHAR* convert(const char * aString);
char* convertC(const WCHAR * wString);

extern const char* szTextFile;

int
mkAbsoluteFilename( LPSTR dirName,
                    DWORD dwDirLength,
                    LPCSTR fileName,
                    DWORD dwFileLength,
                    LPSTR absPathName );

BOOL CleanupHelper (HANDLE *hArray, DWORD dwIndex);
BOOL Cleanup(HANDLE *hArray, DWORD dwIndex);


/*
 * Tokens 0 and 1 are events.  Token 2 is the thread.
 */
#define NUM_TOKENS 3

extern HANDLE hToken[NUM_TOKENS];

/*
 * Take two wide strings representing file and directory names
 * (dirName, fileName), join the strings with the appropriate path
 * delimiter and populate a wide character buffer (absPathName) with
 * the resulting string.
 *
 * Returns: The number of wide characters in the resulting string.
 * 0 is returned on Error.
 */
int
mkAbsoluteFilenameW (
    LPWSTR dirName,
    DWORD dwDirLength,
    LPCWSTR fileName,
    DWORD dwFileLength,
    LPWSTR absPathName );

/*
 * Take two wide strings representing file and directory names
 * (dirName, fileName), join the strings with the appropriate path
 * delimiter and populate a wide character buffer (absPathName) with
 * the resulting string.
 *
 * Returns: The number of wide characters in the resulting string.
 * 0 is returned on Error.
 */
int
mkAbsoluteFilenameA (
    LPSTR dirName,
    DWORD dwDirLength,
    LPCSTR fileName,
    DWORD dwFileLength,
    LPSTR absPathName );

BOOL
CreatePipe(
        OUT PHANDLE hReadPipe,
        OUT PHANDLE hWritePipe,
        IN LPSECURITY_ATTRIBUTES lpPipeAttributes,
        IN DWORD nSize);

BOOL
DeleteFileW(
        IN LPCWSTR lpFileName);

#define wcstod        PAL_wcstod
#define wcstoul       PAL_wcstoul
#define wcscat        PAL_wcscat
#define wcscpy        PAL_wcscpy
#define wcslen        PAL_wcslen
#define wcsncmp       PAL_wcsncmp
#define wcschr        PAL_wcschr
#define wcsrchr        PAL_wcsrchr
#define wcspbrk       PAL_wcspbrk
#define wcsstr        PAL_wcsstr
#define wcscmp        PAL_wcscmp
#define wcsncpy       PAL_wcsncpy

#endif
