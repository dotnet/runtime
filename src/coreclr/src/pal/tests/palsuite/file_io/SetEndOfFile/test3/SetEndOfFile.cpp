// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetEndOfFile.c (test 3)
**
** Purpose: Tests the PAL implementation of the SetEndOfFile function.
**          This test will attempt to expand a file. Assumes successful
**          SetFilePointer and GetFileSize tests.
**
**
**===================================================================*/

#include <palsuite.h>


const char* szTextFile = "text.txt";


int __cdecl main(int argc, char *argv[])
{
    HANDLE hFile = NULL;
    DWORD dwByteCount = 0;
    DWORD dwOffset = 25;
    DWORD dwRc = 0;
    BOOL bRc = FALSE;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* create a test file */
    hFile = CreateFile(szTextFile,
        GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("SetEndOfFile: ERROR -> Unable to create file \"%s\".\n",
              szTextFile);
    }

    /* move the file pointer */
    /* assumes a successful SetFilePointer test */
    dwRc = SetFilePointer(hFile, dwOffset, NULL, FILE_BEGIN);
    if (dwRc == INVALID_SET_FILE_POINTER)
    {
        Trace("SetEndOfFile: ERROR -> Call to SetFilePointer failed\n");
        bRc = CloseHandle(hFile);
        if (bRc != TRUE)
        {
            Trace("SetEndOfFile: ERROR -> Unable to close file \"%s\".\n",
                  szTextFile);
        }
        PAL_Terminate();
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


    /* call GetFileSize to verify pointer position */
    /* assumes a successful GetFileSize test */

    dwByteCount = GetFileSize(hFile, NULL);
    if (dwByteCount != dwOffset)
    {
        Trace("SetEndOfFile: ERROR -> file apparently not expanded to the"
              " correct size.\n");
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

    PAL_Terminate();
    return PASS;
}
