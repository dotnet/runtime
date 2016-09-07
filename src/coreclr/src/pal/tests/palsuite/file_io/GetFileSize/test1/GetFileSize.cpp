// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  GetFileSize.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetFileSize function.
**
**
**===================================================================*/

#include <palsuite.h>

const char* szTextFile = "text.txt";

void CleanUp(HANDLE hFile)
{
    if (CloseHandle(hFile) != TRUE)
    {
        Fail("GetFileSize: ERROR -> Unable to close file \"%s\".\n", 
            szTextFile);
    }
    if (!DeleteFileA(szTextFile))
    {
        Fail("GetFileSize: ERROR -> Unable to delete file \"%s\".\n", 
            szTextFile);
    }
}

void CheckFileSize(HANDLE hFile, DWORD dwOffset, DWORD dwHighOrder)
{
    DWORD dwRc = 0;
    DWORD dwReturnedHighOrder = 0;
    DWORD dwReturnedOffset = 0;

    dwRc = SetFilePointer(hFile, dwOffset, (PLONG)&dwHighOrder, FILE_BEGIN);
    if (dwRc == INVALID_SET_FILE_POINTER)
    {
        Trace("GetFileSize: ERROR -> Call to SetFilePointer failed with %ld.\n", 
            GetLastError());
        CleanUp(hFile);
        Fail("");
    }
    else
    {
        if (!SetEndOfFile(hFile))
        {
            Trace("GetFileSize: ERROR -> Call to SetEndOfFile failed with %ld.\n", 
                GetLastError());
            CleanUp(hFile);
            Fail("");
        }
        dwReturnedOffset = GetFileSize(hFile, &dwReturnedHighOrder);
        if ((dwReturnedOffset != dwOffset) || 
            (dwReturnedHighOrder != dwHighOrder))
        {
            CleanUp(hFile);
            Fail("GetFileSize: ERROR -> File sizes do not match up.\n");
        }
    }
}


int __cdecl main(int argc, char *argv[])
{
    HANDLE hFile = NULL;
    DWORD dwRc = 0;
    DWORD dwRc2 = 0;
    DWORD dwHighOrder = 0;
    DWORD lpNumberOfBytesWritten;
    char * data = "1234567890";

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }


    /* test on a null file handle */
    dwRc = GetFileSize(hFile, NULL);
    if (dwRc != INVALID_FILE_SIZE)
    {
        Fail("GetFileSize: ERROR -> A file size was returned for "
            "a null handle.\n");
    }

    /* test on a null file handle using the high order option */
    dwRc = GetFileSize(hFile, &dwHighOrder);
    if (dwRc != INVALID_FILE_SIZE)
    {
        Fail("GetFileSize: ERROR -> A file size was returned for "
            "a null handle.\n");
    }

    /* test on an invalid file handle */
    dwRc = GetFileSize(INVALID_HANDLE_VALUE, NULL);
    if (dwRc != INVALID_FILE_SIZE)
    {
        Fail("GetFileSize: ERROR -> A file size was returned for "
            "an invalid handle.\n");
    }

    /* test on an invalid file handle using the high order option */
    dwRc = GetFileSize(INVALID_HANDLE_VALUE, &dwHighOrder);
    if (dwRc != INVALID_FILE_SIZE)
    {
        Fail("GetFileSize: ERROR -> A file size was returned for "
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
        Fail("GetFileSize: ERROR -> Unable to create file \"%s\".\n", 
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
    dwRc = GetFileSize(hFile, NULL);

    /* test writing to the file */
    if(WriteFile(hFile, data, strlen(data), &lpNumberOfBytesWritten, NULL)==0)
    {
        Trace("GetFileSize: ERROR -> Call to WriteFile failed with %ld.\n", 
             GetLastError());
        CleanUp(hFile);
        Fail("");
    }
    
    /* make sure the buffer flushed.*/
    if(FlushFileBuffers(hFile)==0)
    {
        Trace("GetFileSize: ERROR -> Call to FlushFileBuffers failed with %ld.\n", 
             GetLastError());
        CleanUp(hFile);
        Fail("");
    }

    /* get file size after writing some chars */
    dwRc2 = GetFileSize(hFile, NULL);
    if((dwRc2-dwRc) !=strlen(data))
    {
        CleanUp(hFile);
        Fail("GetFileSize: ERROR -> File size did not increase properly after.\n"
             "writing %d chars\n", strlen(data));        
    }

    CleanUp(hFile);
    PAL_Terminate();
    return PASS;
}
