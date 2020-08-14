// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetFileSizeEx.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetFileSizeEx function.
**
**
**===================================================================*/

#include <palsuite.h>

const char* szTextFile = "text.txt";

void CleanUp(HANDLE hFile)
{
    if (CloseHandle(hFile) != TRUE)
    {
        Fail("GetFileSizeEx: ERROR -> Unable to close file \"%s\".\n"
             " Error is %d\n", 
            szTextFile, GetLastError());
    }
    if (!DeleteFileA(szTextFile))
    {
        Fail("GetFileSizeEx: ERROR -> Unable to delete file \"%s\".\n"
             " Error is %d\n", 
            szTextFile, GetLastError());
    }
}

void CheckFileSize(HANDLE hFile, DWORD dwOffset, DWORD dwHighOrder)
{
    DWORD dwRc = 0;
    DWORD dwError = 0;
    LARGE_INTEGER qwFileSize;

    dwRc = SetFilePointer(hFile, dwOffset, (PLONG)&dwHighOrder, FILE_BEGIN);
    if (dwRc == INVALID_SET_FILE_POINTER)
    {
        Trace("GetFileSizeEx: ERROR -> Call to SetFilePointer failed with %ld.\n", 
            GetLastError());
        CleanUp(hFile);
        Fail("");
    }
    else
    {
        if (!SetEndOfFile(hFile))
        {
            dwError = GetLastError();
            CleanUp(hFile);
            if (dwError == 112)
            {
                Fail("GetFileSizeEx: ERROR -> SetEndOfFile failed due to lack of "
                    "disk space\n");
            }
            else
            {
                Fail("GetFileSizeEx: ERROR -> SetEndOfFile call failed "
                    "with error %ld\n", dwError);
            }
        }
        else
        {
            GetFileSizeEx(hFile, &qwFileSize);
            if ((qwFileSize.u.LowPart != dwOffset) || 
                (qwFileSize.u.HighPart != dwHighOrder))
            {
                CleanUp(hFile);
                Fail("GetFileSizeEx: ERROR -> File sizes do not match up.\n");
            }
        }
    }
}

int __cdecl main(int argc, char *argv[])
{
    HANDLE hFile = NULL;
    BOOL bRc = FALSE;
    DWORD lpNumberOfBytesWritten;
    LARGE_INTEGER qwFileSize;
    LARGE_INTEGER qwFileSize2;
    char * data = "1234567890";

    qwFileSize.QuadPart = 0;
    qwFileSize2.QuadPart = 0;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }


    /* test on a null file */
    bRc = GetFileSizeEx(NULL, &qwFileSize);
    if (bRc != FALSE)
    {
        Fail("GetFileSizeEx: ERROR -> Returned status as TRUE for "
            "a null handle.\n");
    }


    /* test on an invalid file */
    bRc = GetFileSizeEx(INVALID_HANDLE_VALUE, &qwFileSize);
    if (bRc != FALSE)
    {
        Fail("GetFileSizeEx: ERROR -> Returned status as TRUE for "
            "an invalid handle.\n");
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
        Fail("GetFileSizeEx: ERROR -> Unable to create file \"%s\".\n", 
            szTextFile);
    }

    /* give the file a size */
    CheckFileSize(hFile, 256, 0);

    /* make the file large using the high order option */
    CheckFileSize(hFile, 256, 1);


    /* set the file size to zero */
    CheckFileSize(hFile, 0, 0);

    /*  test if file size changes by writing to it. */
    /* get file size */
    GetFileSizeEx(hFile, &qwFileSize);

    /* test writing to the file */
    if(WriteFile(hFile, data, strlen(data), &lpNumberOfBytesWritten, NULL)==0)
    {
        Trace("GetFileSizeEx: ERROR -> Call to WriteFile failed with %ld.\n", 
             GetLastError());
        CleanUp(hFile);
        Fail("");
    }
    
    /* make sure the buffer flushed.*/
    if(FlushFileBuffers(hFile)==0)
    {
        Trace("GetFileSizeEx: ERROR -> Call to FlushFileBuffers failed with %ld.\n", 
             GetLastError());
        CleanUp(hFile);
        Fail("");
    }

    /* get file size after writing some chars */
    GetFileSizeEx(hFile, &qwFileSize2);
    if((qwFileSize2.QuadPart-qwFileSize.QuadPart) !=strlen(data))
    {
        CleanUp(hFile);
        Fail("GetFileSizeEx: ERROR -> File size did not increase properly after.\n"
             "writing %d chars\n", strlen(data));        
    }

    CleanUp(hFile);
    PAL_Terminate();
    return PASS;
}
