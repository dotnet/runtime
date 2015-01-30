//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  GetStdHandle.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetStdHandle function.
**
**
**===================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    HANDLE hFile = NULL;
    DWORD dwBytesWritten = 0;
    DWORD dwFileType;
    BOOL bRc = FALSE;
    const char* szText = "this is a test of GetStdHandle\n";


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /*
     * attempt to get an invalid handle
     */
    hFile = GetStdHandle(-2);
    if (hFile != INVALID_HANDLE_VALUE)
    {
        Fail("GetStdHandle: ERROR -> A request for the STD_INPUT_HANDLE "
            "returned an invalid handle.\n");
    }


    /*
     * test the STD_INPUT_HANDLE handle
     */
    hFile = GetStdHandle(STD_INPUT_HANDLE);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail("GetStdHandle: ERROR -> A request for the STD_INPUT_HANDLE "
            "returned an invalid handle.\n");
    }

    /* an attempt to write to the input handle should fail */
    /* I don't know how to automate a read from the input handle */
    bRc = WriteFile(hFile, szText, (DWORD)strlen(szText), &dwBytesWritten, NULL);
    if (bRc != FALSE)
    {
        Fail("GetStdHandle: ERROR -> WriteFile was able to write to "
            "STD_INPUT_HANDLE when it should have failed.\n");
    }


    /*
     * test the STD_OUTPUT_HANDLE handle
     */
    hFile = GetStdHandle(STD_OUTPUT_HANDLE);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail("GetStdHandle: ERROR -> A request for the STD_OUTPUT_HANDLE "
            "returned an invalid handle.\n");
    }

    /* try to write to the output handle */
    bRc = WriteFile(hFile, szText, (DWORD)strlen(szText), &dwBytesWritten, NULL);
    if (bRc != TRUE)
    {
        Fail("GetStdHandle: ERROR -> WriteFile failed to write to "
            "STD_OUTPUT_HANDLE with the error %ld\n",
            GetLastError());
    }


    /* test the STD_ERROR_HANDLE handle */
    hFile = GetStdHandle(STD_ERROR_HANDLE);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail("GetStdHandle: ERROR -> A request for the STD_ERROR_HANDLE "
            "returned an invalid handle.\n");
    }

    /* try to write to the error handle */
    bRc = WriteFile(hFile, szText, (DWORD)strlen(szText), &dwBytesWritten, NULL);
    if (bRc != TRUE)
    {
        Fail("GetStdHandle: ERROR -> WriteFile failed to write to "
            "STD_ERROR_HANDLE with the error %ld\n",
            GetLastError());
    }

    /* check if the file type is correct for the handle */
    if((dwFileType = GetFileType(hFile)) != FILE_TYPE_CHAR)
    {
        Fail("GetStdHandle: ERROR -> GetFileType returned %u for "
            "STD_ERROR_HANDLE instead of the expected FILE_TYPE_CHAR (%u).\n",
            dwFileType,
            FILE_TYPE_CHAR);
    }

    /* check to see if we can CloseHandle works on the STD_ERROR_HANDLE */
    if (!CloseHandle(hFile))
    {
        Fail("GetStdHandle: ERROR -> CloseHandle failed. GetLastError "
            "returned %u.\n",
            GetLastError());
    }

    /* try to write to the closed error handle */
    bRc = WriteFile(hFile, 
        szText, 
        (DWORD)strlen(szText), 
        &dwBytesWritten, 
        NULL);
    if (bRc)
    {
        Fail("GetStdHandle: ERROR -> WriteFile was able to write to the closed"
            " STD_ERROR_HANDLE handle.\n");
    }


    PAL_Terminate();
    return PASS;
}

