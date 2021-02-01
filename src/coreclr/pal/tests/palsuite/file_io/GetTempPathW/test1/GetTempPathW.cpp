// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetTempPathW.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetTempPathW function.
**
**
**===================================================================*/

#include <palsuite.h>

static void SetTmpDir(const WCHAR path[])
{
    DWORD result = SetEnvironmentVariableW(W("TMPDIR"), path);
    if (!result)
    {
        Fail("ERROR -> SetEnvironmentVariableW failed with result %d and error code %d.\n",
             result, GetLastError());
    }
}

static void SetAndCompare(const WCHAR tmpDirPath[], const WCHAR expected[])
{
    DWORD dwBufferLength = _MAX_DIR;
    WCHAR path[dwBufferLength];

    SetTmpDir(tmpDirPath);

    DWORD dwResultLen = GetTempPathW(dwBufferLength, path);
    if (dwResultLen <= 0)
    {
        Fail("ERROR: GetTempPathW returned %d with error code %d.\n", dwResultLen, GetLastError());
    }
    if (dwResultLen >= dwBufferLength)
    {
        Fail("ERROR: Buffer of length %d passed to GetTempPathA was too small to hold %d chars..\n", dwBufferLength, dwResultLen);
    }
    if (wcscmp(expected, path) != 0)
    {
        Fail("ERROR: GetTempPathW expected to get '%S' but instead got '%S'.\n", expected, path);
    }
    if (expected[dwResultLen - 1] != '/')
    {
        Fail("ERROR: GetTempPathW returned '%S', which should have ended in '/'.\n", path);
    }
}

static void SetAndCheckLength(const WCHAR tmpDirPath [], int bufferLength, int expectedResultLength)
{
    WCHAR path[bufferLength];

    SetTmpDir(tmpDirPath);
    DWORD dwResultLen = GetTempPathW(bufferLength, path);

    if (dwResultLen != expectedResultLength)
    {
        Fail("GetTempPathW(%d, %S) expected to return %d but returned %d.\n",
             bufferLength, tmpDirPath?tmpDirPath:W("NULL"), expectedResultLength, dwResultLen);
    }
}

PALTEST(file_io_GetTempPathW_test1_paltest_gettemppathw_test1, "file_io/GetTempPathW/test1/paltest_gettemppathw_test1")
{
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    SetAndCompare(W("/tmp"), W("/tmp/"));
    SetAndCompare(W("/tmp/"), W("/tmp/"));
    SetAndCompare(W(""), W("/tmp/"));
    SetAndCompare(NULL, W("/tmp/"));
    SetAndCompare(W("/"), W("/"));
    SetAndCompare(W("/var/tmp"), W("/var/tmp/"));
    SetAndCompare(W("/var/tmp/"), W("/var/tmp/"));
    SetAndCompare(W("~"), W("~/"));
    SetAndCompare(W("~/"), W("~/"));
    SetAndCompare(W(".tmp"), W(".tmp/"));
    SetAndCompare(W("./tmp"), W("./tmp/"));
    SetAndCompare(W("/home/someuser/sometempdir"), W("/home/someuser/sometempdir/"));
    SetAndCompare(NULL, W("/tmp/"));

    DWORD dwResultLen = GetTempPathA(0, NULL);
    if (dwResultLen != 0 || GetLastError() != ERROR_INVALID_PARAMETER)
    {
        Fail("GetTempPathW(NULL, ...) returned %d with error code %d but "
             "should have failed with ERROR_INVALID_PARAMETER (%d).\n",
             dwResultLen, GetLastError(), ERROR_INVALID_PARAMETER);
    }

    SetAndCheckLength(W("abc/"), 5, 4);
    SetAndCheckLength(W("abcd"), 5, 6);
    SetAndCheckLength(W("abcde"), 5, 7);
    SetAndCheckLength(W("abcdef/"), 5, 9);
    SetAndCheckLength(NULL, 5, 6);

    PAL_Terminate();
    return PASS;
}
