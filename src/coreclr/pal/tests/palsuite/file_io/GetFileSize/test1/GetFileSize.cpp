// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetFileSize.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetFileSize function.
**
**
**===================================================================*/

#include <palsuite.h>



void CleanUp_GetFileSize_test1(HANDLE hFile)
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

PALTEST(file_io_GetFileSize_test1_paltest_getfilesize_test1, "file_io/GetFileSize/test1/paltest_getfilesize_test1")
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

    /*  test if file size changes by writing to it. */
    /* get file size */
    dwRc = GetFileSize(hFile, NULL);

    /* test writing to the file */
    if(WriteFile(hFile, data, strlen(data), &lpNumberOfBytesWritten, NULL)==0)
    {
        Trace("GetFileSize: ERROR -> Call to WriteFile failed with %ld.\n", 
             GetLastError());
        CleanUp_GetFileSize_test1(hFile);
        Fail("");
    }
    
    /* make sure the buffer flushed.*/
    if(FlushFileBuffers(hFile)==0)
    {
        Trace("GetFileSize: ERROR -> Call to FlushFileBuffers failed with %ld.\n", 
             GetLastError());
        CleanUp_GetFileSize_test1(hFile);
        Fail("");
    }

    /* get file size after writing some chars */
    dwRc2 = GetFileSize(hFile, NULL);
    if((dwRc2-dwRc) !=strlen(data))
    {
        CleanUp_GetFileSize_test1(hFile);
        Fail("GetFileSize: ERROR -> File size did not increase properly after.\n"
             "writing %d chars\n", strlen(data));        
    }

    CleanUp_GetFileSize_test1(hFile);
    PAL_Terminate();
    return PASS;
}
