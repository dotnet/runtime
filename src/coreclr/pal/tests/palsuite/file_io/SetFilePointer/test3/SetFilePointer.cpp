// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  SetFilePointer.c (test 3)
**
** Purpose: Tests the PAL implementation of the SetFilePointer function.
**          Test the FILE_CURRENT option 
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

const char* const szText =
	"The quick brown fox jumped over the lazy dog's back.";



PALTEST(file_io_SetFilePointer_test3_paltest_setfilepointer_test3, "file_io/SetFilePointer/test3/paltest_setfilepointer_test3")
{
    HANDLE hFile = NULL;
    DWORD dwByteCount = 0;
    DWORD dwRc = 0;
    BOOL bRc = FALSE;
    char szBuffer[100];
    const char* szPtr;


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

    /* reset the pointer to the beginning */
    if (SetFilePointer(hFile, 0, NULL, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
    {
        if (GetLastError() != ERROR_SUCCESS)
        {
            Trace("SetFilePointer: ERROR -> Unable to reset the pointer to the "
                "beginning of the file");
            if (CloseHandle(hFile) != TRUE)
            {
                Trace("SetFilePointer: ERROR -> Unable to close file \"%s\".\n",
                      szTextFile);
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

    /* move -1 from beginning which should fail */
    dwRc = SetFilePointer(hFile, -1, NULL, FILE_CURRENT);
    if ((dwRc != INVALID_SET_FILE_POINTER) ||
        (GetLastError() == ERROR_SUCCESS))
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


    /* move the file pointer 0 bytes from the beginning and verify */
    dwRc = SetFilePointer(hFile, 0, NULL, FILE_CURRENT);
    if (dwRc != 0)
    {
        Trace("SetFilePointer: ERROR -> Asked to move 0 bytes from the "
            "beginning of the file but moved %ld bytes.\n", dwRc);
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
    dwRc = SetFilePointer(hFile, 20, NULL, FILE_CURRENT);
    if (dwRc != 20)
    {
        Trace("SetFilePointer: ERROR -> Asked to move 20 bytes from the "
            "beginning of the file but moved %ld bytes.\n", dwRc);
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
        memset(szBuffer, 0, 100);
        bRc = ReadFile(hFile, szBuffer, (DWORD)strlen(szText)-20, &dwByteCount,
                       NULL);
        if ((bRc != TRUE) || (dwByteCount != strlen(szText)-20))
        {
            Trace("SetFilePointer: ERROR -> ReadFile failed to read correctly");
            if (CloseHandle(hFile) != TRUE)
            {
                Trace("SetFilePointer: ERROR -> Unable to close file \"%s\".\n",
                      szTextFile);
            }
            if (!DeleteFileA(szTextFile))
            {
                Trace("SetFilePointer: ERROR -> Unable to delete file"
                      " \"%s\".\n", szTextFile);
            }
            PAL_TerminateEx(FAIL);
            return FAIL;
        }
        szPtr = szText + 20;;
        if (strcmp(szPtr, szBuffer) != 0)
        {
            Trace("SetFilePointer: ERROR -> Apparently failed to move the"
                  " pointer properly\n");
            if (CloseHandle(hFile) != TRUE)
            {
                Trace("SetFilePointer: ERROR -> Unable to close file \"%s\".\n",
                      szTextFile);
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


    /* get the current file pointer position (should be 52) */
    dwRc = SetFilePointer(hFile, 0, NULL, FILE_CURRENT);
    if (dwRc != 52)
    {
        Trace("SetFilePointer: ERROR -> Asked for current position."
              " Should be 52 but was %ld.\n", dwRc);
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


    /* move the pointer backwards in the file and verify */
    dwRc = SetFilePointer(hFile, -10, NULL, FILE_CURRENT);
    if (dwRc != 42)
    {
        Trace("SetFilePointer: ERROR -> Asked to move back 10 bytes from the"
            "end of the file but moved it to position %ld.\n", dwRc);
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
        memset(szBuffer, 0, 100);
        bRc = ReadFile(hFile, szBuffer, 10, &dwByteCount, NULL);
        if ((bRc != TRUE) || (dwByteCount != 10))
        {
            Trace("SetFilePointer: ERROR -> ReadFile failed to read correctly");
            if (CloseHandle(hFile) != TRUE)
            {
                Trace("SetFilePointer: ERROR -> Unable to close file \"%s\".\n",
                      szTextFile);
            }
            if (!DeleteFileA(szTextFile))
            {
                Trace("SetFilePointer: ERROR -> Unable to delete file"
                      " \"%s\".\n", szTextFile);
            }
            PAL_TerminateEx(FAIL);
            return FAIL;
        }
        szPtr = szText + 42;
        if (strcmp(szPtr, szBuffer) != 0)
        {
            Trace("SetFilePointer: ERROR -> Apparently failed to move the"
                  " pointer properly\n");
            if (CloseHandle(hFile) != TRUE)
            {
                Trace("SetFilePointer: ERROR -> Unable to close file \"%s\".\n",
                      szTextFile);
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
     * the file pointer is currently at the end of the file so...
     * set the pointer past the end of the file and verify
     */
    dwRc = SetFilePointer(hFile, 20, NULL, FILE_CURRENT);
    if ((dwRc == INVALID_SET_FILE_POINTER) && (GetLastError() != ERROR_SUCCESS))
    {
        Trace("SetFilePointer: ERROR -> Failed to move pointer past EOF.\n");
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
        if (SetFilePointer(hFile, 0, NULL, FILE_CURRENT) != strlen(szText)+20)
        {
            Trace("SetFilePointer: ERROR -> Failed to move pointer past"
                  " EOF.\n");
            if (CloseHandle(hFile) != TRUE)
            {
                Trace("SetFilePointer: ERROR -> Unable to close file \"%s\".\n",
                      szTextFile);
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
