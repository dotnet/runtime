// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetFilePointer.c (test 5)
**
** Purpose: Tests the PAL implementation of the SetFilePointer function.
**          Test the FILE_BEGIN option using the high word parameter
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
    DWORD dwOffset = 1;
    LONG dwHighWord = 1;
    DWORD dwReturnedOffset = 0;
    DWORD dwReturnedHighWord = 0;
    DWORD dwRc = 0;
    DWORD dwError = 0;
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
        dwError = GetLastError();
        Fail("SetFilePointer: ERROR -> Unable to create file \"%s\".\n with "
            "error %ld",
            szTextFile,
            GetLastError());
    }



    /* move -1 from beginning which should fail */
    dwRc = SetFilePointer(hFile, -1, &dwHighWord, FILE_BEGIN);
    if (dwRc != INVALID_SET_FILE_POINTER)
    {
        Trace("SetFilePointer: ERROR -> Succeeded to move the pointer "
            "before the beginning of the file using the high word.\n");
        bRc = CloseHandle(hFile);
        if (bRc != TRUE)
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

    /* set the pointer past the end of the file and verify */
    dwRc = SetFilePointer(hFile, dwOffset, &dwHighWord, FILE_BEGIN);
    if ((dwRc == INVALID_SET_FILE_POINTER) &&
        ((dwError = GetLastError()) != ERROR_SUCCESS))
    {
        Trace("SetFilePointer: ERROR -> Failed to move pointer past EOF.\n");
        bRc = CloseHandle(hFile);
        if (bRc != TRUE)
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
        /* verify */
        bRc = SetEndOfFile(hFile);
        if (bRc != TRUE)
        {
            dwError = GetLastError();
            if (dwError == 112)
            {
                Trace("SetFilePointer: ERROR -> SetEndOfFile failed due to "
                    "lack of disk space\n");
            }
            else
            {
                Trace("SetFilePointer: ERROR -> SetEndOfFile call failed with "
                    "error %ld\n", dwError);
            }
            bRc = CloseHandle(hFile);
            if (bRc != TRUE)
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

        dwReturnedOffset = GetFileSize(hFile, &dwReturnedHighWord);
        if ((dwOffset != dwReturnedOffset) ||
           (dwHighWord != dwReturnedHighWord))
        {
            Trace("SetFilePointer: ERROR -> Failed to move pointer past"
                  " EOF.\n");
            bRc = CloseHandle(hFile);
            if (bRc != TRUE)
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

    bRc = CloseHandle(hFile);
    if (bRc != TRUE)
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
