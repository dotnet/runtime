// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  gettemppatha.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetTempPathA function.
**
**
**===================================================================*/

#include <palsuite.h>

static void SetTmpDir(CHAR path[])
{
    DWORD result = SetEnvironmentVariableA("TMPDIR", path);
    if (!result)
    {
        Fail("ERROR -> SetEnvironmentVariableA failed with result %d and error code %d.\n", 
             result, GetLastError());
    }
}

static void SetAndCompare(CHAR tmpDirPath[], CHAR expected[])
{
    DWORD dwBufferLength = _MAX_DIR;
    CHAR  path[dwBufferLength];
    
    SetTmpDir(tmpDirPath);

    DWORD dwResultLen = GetTempPathA(dwBufferLength, path);
    if (dwResultLen <= 0)
    {
        Fail("ERROR: GetTempPathA returned %d with error code %d.\n", dwResultLen, GetLastError());
    }
    if (dwResultLen >= dwBufferLength)
    {
        Fail("ERROR: Buffer of length %d passed to GetTempPathA was too small to hold %d chars..\n", dwBufferLength, dwResultLen);
    }
    if (strcmp(expected, path) != 0)
    {
        Fail("ERROR: GetTempPathA expected to get '%s' but instead got '%s'.\n", expected, path);
    }
    if (expected[dwResultLen - 1] != '/')
    {
        Fail("ERROR: GetTempPathA returned '%s', which should have ended in '/'.\n", path);
    }
}

static void SetAndCheckLength(CHAR tmpDirPath[], int bufferLength, int expectedResultLength)
{
    CHAR  path[bufferLength];

    SetTmpDir(tmpDirPath);
    DWORD dwResultLen = GetTempPathA(bufferLength, path);

    if (dwResultLen != expectedResultLength)
    {
        Fail("GetTempPathA(%d, %s) expected to return %d but returned %d.\n",
             bufferLength, tmpDirPath?tmpDirPath:"NULL", expectedResultLength, dwResultLen);
    }
}

PALTEST(file_io_gettemppatha_test1_paltest_gettemppatha_test1, "file_io/gettemppatha/test1/paltest_gettemppatha_test1")
{
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    SetAndCompare("/tmp", "/tmp/");
    SetAndCompare("/tmp/", "/tmp/");
    SetAndCompare("", "/tmp/");
    SetAndCompare(NULL, "/tmp/");
    SetAndCompare("/", "/");
    SetAndCompare("/var/tmp", "/var/tmp/");
    SetAndCompare("/var/tmp/", "/var/tmp/");
    SetAndCompare("~", "~/");
    SetAndCompare("~/", "~/");
    SetAndCompare(".tmp", ".tmp/");
    SetAndCompare("./tmp", "./tmp/");
    SetAndCompare("/home/someuser/sometempdir", "/home/someuser/sometempdir/");
    SetAndCompare(NULL, "/tmp/");

    DWORD dwResultLen = GetTempPathA(0, NULL);
    if (dwResultLen != 0 || GetLastError() != ERROR_INVALID_PARAMETER)
    {
        Fail("GetTempPath(NULL, ...) returned %d with error code %d but "
             "should have failed with ERROR_INVALID_PARAMETER (%d).\n", 
             dwResultLen, GetLastError(), ERROR_INVALID_PARAMETER);
    }

    SetAndCheckLength("abc/", 5, 4);
    SetAndCheckLength("abcd", 5, 6);
    SetAndCheckLength("abcde", 5, 7);
    SetAndCheckLength("abcdef/", 5, 9);
    SetAndCheckLength(NULL, 5, 6);

    PAL_Terminate();
    return PASS;
}
