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



void CleanUp_GetFileSizeEx_test1(HANDLE hFile)
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

PALTEST(file_io_GetFileSizeEx_test1_paltest_getfilesizeex_test1, "file_io/GetFileSizeEx/test1/paltest_getfilesizeex_test1")
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

    /*  test if file size changes by writing to it. */
    /* get file size */
    GetFileSizeEx(hFile, &qwFileSize);

    /* test writing to the file */
    if(WriteFile(hFile, data, strlen(data), &lpNumberOfBytesWritten, NULL)==0)
    {
        Trace("GetFileSizeEx: ERROR -> Call to WriteFile failed with %ld.\n", 
             GetLastError());
        CleanUp_GetFileSizeEx_test1(hFile);
        Fail("");
    }
    
    /* make sure the buffer flushed.*/
    if(FlushFileBuffers(hFile)==0)
    {
        Trace("GetFileSizeEx: ERROR -> Call to FlushFileBuffers failed with %ld.\n", 
             GetLastError());
        CleanUp_GetFileSizeEx_test1(hFile);
        Fail("");
    }

    /* get file size after writing some chars */
    GetFileSizeEx(hFile, &qwFileSize2);
    if((qwFileSize2.QuadPart-qwFileSize.QuadPart) !=strlen(data))
    {
        CleanUp_GetFileSizeEx_test1(hFile);
        Fail("GetFileSizeEx: ERROR -> File size did not increase properly after.\n"
             "writing %d chars\n", strlen(data));        
    }

    CleanUp_GetFileSizeEx_test1(hFile);
    PAL_Terminate();
    return PASS;
}
