// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  setendoffile.c (test 4)
**
** Purpose: Tests the PAL implementation of the SetEndOfFile function.
**          Verify that the file pointer is the same before
**          and after a SetEndOfFile using SetFilePointer with
**          FILE_BEGIN, FILE_CURRENT and FILE_END
**
**
**===================================================================*/

#include <palsuite.h>


const char* szStringTest = "The quick fox jumped over the lazy dog's back.";
const char* szTextFile = "test.tmp";

static void Cleanup(HANDLE hFile)
{
    if (!CloseHandle(hFile))
    {
        Trace("SetEndOfFile: ERROR -> Unable to close file \"%s\". ", 
            "GetLastError returned %u.\n", 
            szTextFile,
            GetLastError());
    }
    if (!DeleteFileA(szTextFile))
    {
        Trace("SetEndOfFile: ERROR -> Unable to delete file \"%s\". ", 
            "GetLastError returned %u.\n", 
            szTextFile,
            GetLastError());
    }
}

static void DoTest(HANDLE hFile, DWORD dwOffset, DWORD dwMethod)
{
    DWORD dwFP1 = 0;
    DWORD dwFP2 = 0;
    DWORD dwError;

    /* set the pointer*/
    dwFP1 = SetFilePointer(hFile, dwOffset, NULL, dwMethod);
    if ((dwFP1 == INVALID_SET_FILE_POINTER) &&
        ((dwError = GetLastError()) != ERROR_SUCCESS))
    {
        Trace("SetEndOfFile: ERROR -> Unable to set the pointer to the "
            "end of the file. GetLastError returned %u.\n",
            dwError);
        Cleanup(hFile);
        Fail("");
    }

    /* set EOF */
    if (!SetEndOfFile(hFile))
    {
        Trace("SetEndOfFile: ERROR -> Unable to set end of file. "
            "GetLastError returned %u.\n",
            GetLastError());
        Cleanup(hFile);
        Fail("");
    }

    /* get current file pointer pointer */
    dwFP2 = SetFilePointer(hFile, 0, NULL, FILE_CURRENT);
    if ((dwFP1 == INVALID_SET_FILE_POINTER) &&
        ((dwError = GetLastError()) != ERROR_SUCCESS))
    {
        Trace("SetEndOfFile: ERROR -> Unable to set the pointer to the "
            "end of the file. GetLastError returned %u.\n",
            dwError);
        Cleanup(hFile);
        Fail("");
    }

    /* are they the same? */
    if (dwFP1 != dwFP2)
    {
        Trace("SetEndOfFile: ERROR -> File pointer before (%u) the "
            "SetEndOfFile call was different than after (%u).\n",
            dwFP1,
            dwFP2);
        Cleanup(hFile);
        Fail("");
    }
}

int __cdecl main(int argc, char *argv[])
{
    HANDLE hFile = NULL;
    DWORD dwBytesWritten;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* create a test file */
    hFile = CreateFile(szTextFile, 
        GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("SetEndOfFile: ERROR -> Unable to create file \"%s\". "
            "GetLastError returned %u.\n", 
            szTextFile,
            GetLastError());
    }

    if (!WriteFile(hFile, szStringTest, strlen(szStringTest), &dwBytesWritten, NULL))
    {
        Trace("SetEndOfFile: ERROR -> Unable to write to \"%s\". ", 
            "GetLastError returned %u.\n", 
            szTextFile,
            GetLastError());
        Cleanup(hFile);
        Fail("");
    }

    DoTest(hFile, -2, FILE_END);        /* test the end */
    DoTest(hFile, -10, FILE_CURRENT);   /* test the middle-ish */
    DoTest(hFile, 0, FILE_BEGIN);       /* test the start */

    Cleanup(hFile);

    PAL_Terminate();
    return PASS;
}
