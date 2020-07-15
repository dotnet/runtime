// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  SetEndOfFile.c (test 2)
**
** Purpose: Tests the PAL implementation of the SetEndOfFile function.
**          This test will attempt to truncate a file
**
**
**===================================================================*/

#include <palsuite.h>


const char* szStringTest = "The quick fox jumped over the lazy dog's back.";
const char* szTextFile = "text.txt";


int __cdecl main(int argc, char *argv[])
{
    HANDLE hFile = NULL;
    DWORD dwByteCount = 0;
    DWORD dwBytesWritten;
    BOOL bRc = FALSE;
    char szBuffer[100];
    DWORD dwBytesRead = 0;
    FILE *pFile = NULL;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    // create a test file
    hFile = CreateFile(szTextFile, 
        GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("SetEndOfFile: ERROR -> Unable to create file \"%s\".\n", 
            szTextFile);
    }

    bRc = WriteFile(hFile, szStringTest, 20, &dwBytesWritten, NULL);
    if (bRc != TRUE)
    {
        Trace("SetEndOfFile: ERROR -> Uable to write to \"%s\".\n", 
            szTextFile);
        bRc = CloseHandle(hFile);
        if (bRc != TRUE)
        {
            Trace("SetEndOfFile: ERROR -> Unable to close file \"%s\".\n", 
                szTextFile);
        }
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    bRc = CloseHandle(hFile);
    if (bRc != TRUE)
    {
        Fail("SetEndOfFile: ERROR -> Unable to close file \"%s\".\n", 
            szTextFile);
    }

    // open the test file
    hFile = CreateFile(szTextFile, 
        GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("SetEndOfFile: ERROR -> Unable to open file \"%s\".\n", 
            szTextFile);
    }

    // read a bit of the file to move the file pointer
    dwByteCount = 10;
    bRc = ReadFile(hFile, szBuffer, dwByteCount, &dwBytesRead, NULL);
    if (bRc != TRUE)
    {
        Trace("SetEndOfFile: ERROR -> Uable to read from \"%s\".\n", 
            szTextFile);
        bRc = CloseHandle(hFile);
        if (bRc != TRUE)
        {
            Trace("SetEndOfFile: ERROR -> Unable to close file \"%s\".\n", 
                szTextFile);
        }
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    bRc = SetEndOfFile(hFile);
    if (bRc != TRUE)
    {
        Trace("SetEndOfFile: ERROR -> Uable to set end of file.\n");
        bRc = CloseHandle(hFile);
        if (bRc != TRUE)
        {
            Trace("SetEndOfFile: ERROR -> Unable to close file \"%s\".\n", 
                szTextFile);
        }
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    bRc = CloseHandle(hFile);
    if (bRc != TRUE)
    {
        Fail("SetEndOfFile: ERROR -> Unable to close file \"%s\".\n", 
            szTextFile);
    }

    // open and read the test file
    pFile = fopen(szTextFile, "r");
    if (pFile == NULL)
    {
        Fail("SetEndOfFile: ERROR -> fopen was unable to open file \"%s\".\n", 
            szTextFile);
    }

    // since we truncated the file at 10 characters, 
    // try reading 20 just to be safe
    memset(szBuffer, 0, 100);
    fgets(szBuffer, 20, pFile);
    fclose(pFile);
    if (strlen(szBuffer) != dwByteCount)
    {
        Fail("SetEndOfFile: ERROR -> file apparently not truncated at "
            "correct position.\n");
    }
    if (strncmp(szBuffer, szStringTest, dwByteCount) != 0)
    {
        Fail("SetEndOfFile: ERROR -> truncated file contents doesn't "
            "compare with what should be there\n");
    }

    PAL_Terminate();
    return PASS;
}
