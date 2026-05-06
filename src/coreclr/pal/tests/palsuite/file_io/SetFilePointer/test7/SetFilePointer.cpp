// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  SetFilePointer.c (test 7)
**
** Purpose: Tests the PAL implementation of the SetFilePointer function.
**          Test the FILE_END option with high order support
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




PALTEST(file_io_SetFilePointer_test7_paltest_setfilepointer_test7, "file_io/SetFilePointer/test7/paltest_setfilepointer_test7")
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


    /* move -1 from beginning which should fail */
    dwHighOrder = -1;
    dwOffset = 0;
    dwRc = SetFilePointer(hFile, dwOffset, &dwHighOrder, FILE_END);
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
    dwRc = SetFilePointer(hFile, dwOffset, &dwHighOrder, FILE_END);
    if ((dwRc != 10) || (dwHighOrder != 1))
    {
        Trace("SetFilePointer: ERROR -> Asked to move 4GB plus 10 bytes from "
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
        dwReturnedHighOrder = 0;
        dwReturnedOffset = SetFilePointer(hFile, 0, &dwReturnedHighOrder, FILE_CURRENT);
        if ((dwReturnedOffset != dwOffset) || (dwReturnedHighOrder != dwHighOrder))
        {
            Trace("SetFilePointer: ERROR -> Asked to move far past the "
                "end of the file. low order sent: %ld  low order returned: %ld "
                "high order sent: %ld  high order returned: %ld",
                dwOffset, dwReturnedOffset,
                dwHighOrder, dwReturnedHighOrder);
            if (!DeleteFileA(szTextFile))
            {
                Trace("SetFilePointer: ERROR -> Unable to delete file \"%s\".\n",
                    szTextFile);
            }
            PAL_TerminateEx(FAIL);
            return FAIL;
        }
    }

    DWORD bytesWritten = 0;
    char ch = ' ';
    if (!WriteFile(hFile, &ch, 1, &bytesWritten, NULL))
    {
        Trace("WriteFile: ERROR\n");
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    /*
     * move the pointer backwards in the file and verify
     */
    dwOffset = -1;
    dwHighOrder = -1;
    dwRc = SetFilePointer(hFile, dwOffset, &dwHighOrder, FILE_END);
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
