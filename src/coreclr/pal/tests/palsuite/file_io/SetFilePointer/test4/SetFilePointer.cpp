// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  SetFilePointer.c (test 4)
**
** Purpose: Tests the PAL implementation of the SetFilePointer function.
**          Test the FILE_END option 
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

const char* szText = "The quick brown fox jumped over the lazy dog's back.";



PALTEST(file_io_SetFilePointer_test4_paltest_setfilepointer_test4, "file_io/SetFilePointer/test4/paltest_setfilepointer_test4")
{
    HANDLE hFile = NULL;
    DWORD dwByteCount = 0;
    DWORD dwOffset = 0;
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
        Fail("SetFilePointer: ERROR -> Unable to create file \"%s\".\n",
            szTextFile);
    }

    bRc = WriteFile(hFile, szText, (DWORD)strlen(szText), &dwByteCount, NULL);
    if (bRc == FALSE)
    {
        Trace("SetFilePointer: ERROR -> Unable to write to file \"%s\".\n",
            szTextFile);
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


    /*
     * move -1 from the end
     */
    dwRc = SetFilePointer(hFile, -1, NULL, FILE_END);
    if (dwRc == INVALID_SET_FILE_POINTER)
    {
        if (GetLastError() != ERROR_SUCCESS)
        {
            Trace("SetFilePointer: ERROR -> Failed to move the pointer "
                "back one character from EOF.\n");
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
    else
    {
        /* verify */
        if ((dwRc != strlen(szText)-1))
        {
            Trace("SetFilePointer: ERROR -> Failed to move the pointer"
                  " -1 bytes from EOF\n");
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

    /*
     * move the file pointer 0 bytes from the end and verify
     */
    dwRc = SetFilePointer(hFile, 0, NULL, FILE_END);
    if (dwRc != strlen(szText))
    {
        Trace("SetFilePointer: ERROR -> Asked to move 0 bytes from the "
            "end of the file. Function returned %ld instead of 52.\n", dwRc);
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

    /*
     * move the pointer past the end of the file and verify
     */
    dwRc = SetFilePointer(hFile, 20, NULL, FILE_END);
    if (dwRc != strlen(szText)+20)
    {
        Trace("SetFilePointer: ERROR -> Asked to move 20 bytes past the "
            "end of the file. Function returned %ld instead of %d.\n",
            dwRc,
            strlen(szText)+20);
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

    /*
     * move the pointer backwards to before the start of the file and verify
     */

    dwOffset = (dwRc + 20) * -1;
    dwRc = SetFilePointer(hFile, dwOffset, NULL, FILE_END);
    if ((dwRc != INVALID_SET_FILE_POINTER) ||
    (GetLastError() == ERROR_SUCCESS))
    {
        Trace("SetFilePointer: ERROR -> Was able to move the pointer "
            "to before the beginning of the file.\n");
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
