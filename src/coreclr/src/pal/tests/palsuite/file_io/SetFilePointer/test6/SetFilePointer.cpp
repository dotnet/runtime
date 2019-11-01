// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetFilePointer.c (test 6)
**
** Purpose: Tests the PAL implementation of the SetFilePointer function.
**          Test the FILE_CURRENT option with high order support
**
** Assumes Successful:
**          CreateFile
**          ReadFile
**          WriteFile
**          strlen
**          CloseHandle
**          strcmp
**          GetFileSize
**
**
**===================================================================*/

#include <palsuite.h>

const char* szTextFile = "text.txt";


int __cdecl main(int argc, char *argv[])
{
    HANDLE hFile = NULL;
    DWORD dwOffset = 0;
    LONG dwHighOrder = 0;
    DWORD dwReturnedOffset = 0;
    LONG dwReturnedHighOrder = 0;
    DWORD dwRc = 0;

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
        Fail("SetFilePointer: ERROR -> Unable to create file \"%s\".\n",
            szTextFile);
    }


    /* move waaaay before the beginning which should fail */
    dwHighOrder = -1;
    dwOffset = 0;
    dwRc = SetFilePointer(hFile, dwOffset, &dwHighOrder, FILE_CURRENT);
    if (dwRc != INVALID_SET_FILE_POINTER)
    {
        Trace("SetFilePointer: ERROR -> Succeeded to move the pointer "
            "before the beginning of the file.\n");
        if (CloseHandle(hFile) != TRUE)
        {
            Trace("SetFilePointer: ERROR -> Unable to close file \"%s\".\n",
                szTextFile);
        }
        if (!DeleteFileA(szTextFile))
        {
            Trace("SetFilePointer: ERROR -> Unable to delete file \"%s\".\n",
                szTextFile);
        }
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    /* move the pointer ahead in the file and verify */
    dwHighOrder = 1;
    dwOffset = 10;
    dwRc = SetFilePointer(hFile, dwOffset, &dwHighOrder, FILE_CURRENT);
    if ((dwRc != 10) || (dwHighOrder != 1))
    {
        Trace("SetFilePointer: ERROR -> Asked to move 2GB plus 10 bytes from "
            "the beginning of the file but didn't.\n");
        if (CloseHandle(hFile) != TRUE)
        {
            Trace("SetFilePointer: ERROR -> Unable to close file \"%s\".\n",
                szTextFile);
        }
        if (!DeleteFileA(szTextFile))
        {
            Trace("SetFilePointer: ERROR -> Unable to delete file \"%s\".\n",
                szTextFile);
        }
        PAL_TerminateEx(FAIL);
        return FAIL;
    }
    else
    {
        /* verify results */
        if (SetEndOfFile(hFile) != TRUE)
        {
            Trace("SetFilePointer: ERROR -> Call to SetEndOfFile failed with "
                "error code: %d\n", GetLastError());
            if (CloseHandle(hFile) != TRUE)
            {
                Trace("SetFilePointer: ERROR -> Unable to close file"
                      " \"%s\".\n", szTextFile);
            }
            if (!DeleteFileA(szTextFile))
            {
                Trace("SetFilePointer: ERROR -> Unable to delete file"
                      " \"%s\".\n", szTextFile);
            }
            PAL_TerminateEx(FAIL);
            return FAIL;
        }
        dwReturnedOffset = GetFileSize(hFile, (DWORD*)&dwReturnedHighOrder);
        if ((dwReturnedOffset != dwOffset) ||
            (dwReturnedHighOrder != dwHighOrder))
        {
            Trace("SetFilePointer: ERROR -> Asked to move far past the "
                "current file pointer. "
                "low order sent: %ld  low order returned: %ld "
                "high order sent: %ld  high order returned: %ld",
                dwOffset, dwReturnedOffset,
                dwHighOrder, dwReturnedHighOrder);
            if (!DeleteFileA(szTextFile))
            {
                Trace("SetFilePointer: ERROR -> Unable to delete file"
                      " \"%s\".\n", szTextFile);
            }
            PAL_TerminateEx(FAIL);
            return FAIL;
        }
    }


    /*
     * move the pointer backwards in the file and verify
     */
    dwOffset = 0;
    dwHighOrder = -1;
    dwRc = SetFilePointer(hFile, dwOffset, &dwHighOrder, FILE_CURRENT);
    if (dwRc != 10)
    {
        Trace("SetFilePointer: ERROR -> Asked to move back to 10 bytes from the"
            "beginning of the file but moved it to position %ld.\n", dwRc);
        if (CloseHandle(hFile) != TRUE)
        {
            Trace("SetFilePointer: ERROR -> Unable to close file \"%s\".\n",
                szTextFile);
        }
        if (!DeleteFileA(szTextFile))
        {
            Trace("SetFilePointer: ERROR -> Unable to delete file \"%s\".\n",
                szTextFile);
        }
        PAL_TerminateEx(FAIL);
        return FAIL;
    }
    else
    {
        /* verify results */
        dwReturnedHighOrder = 0;
        dwRc = SetFilePointer(hFile, 0, &dwReturnedHighOrder, FILE_CURRENT);
        if (dwRc != 10)
        {
            Trace("SetFilePointer: ERROR -> Asked for current position. "
                "Should be 10 but was %ld.\n", dwRc);
            if (CloseHandle(hFile) != TRUE)
            {
                Trace("SetFilePointer: ERROR -> Unable to close file"
                      " \"%s\".\n", szTextFile);
            }
            if (!DeleteFileA(szTextFile))
            {
                Trace("SetFilePointer: ERROR -> Unable to delete file"
                      " \"%s\".\n", szTextFile);
            }
            PAL_TerminateEx(FAIL);
            return FAIL;
        }
    }


    /* clean up, clean up, everybody do their share... */
    if (CloseHandle(hFile) != TRUE)
    {
        Trace("SetFilePointer: ERROR -> Unable to close file \"%s\".\n", 
            szTextFile);
        if (!DeleteFileA(szTextFile))
        {
            Trace("SetFilePointer: ERROR -> Unable to delete file \"%s\".\n", 
                szTextFile);
        }
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    if (!DeleteFileA(szTextFile))
    {
        Fail("SetFilePointer: ERROR -> Unable to delete file \"%s\".\n", 
            szTextFile);
    }

    PAL_Terminate();
    return PASS;
}
