// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetFullPathNameW.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetFullPathNameW function.
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(file_io_GetFullPathNameW_test1_paltest_getfullpathnamew_test1, "file_io/GetFullPathNameW/test1/paltest_getfullpathnamew_test1")
{
    const char* szFileName = "testing.tmp";

    DWORD dwRc = 0;
    WCHAR szwReturnedPath[_MAX_DIR+1];
    WCHAR szwShortBuff[2];
    LPWSTR pPathPtr;
    HANDLE hFile = NULL;
    WCHAR* szwFileName = NULL;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    szwFileName = convert((char*)szFileName);

    /* perform a short buffer test */
    if (GetFullPathNameW(szwFileName, 2, szwShortBuff, &pPathPtr) <= 2)
    {
        free(szwFileName);
        /* this test should have failed but didn't */
        Fail("GetFullPathNameW: ERROR -> The API was passed a buffer that was"
            " too small for the path name and yet it apparently passed.\n");
    }


    memset(szwReturnedPath, 0, sizeof(szwReturnedPath));
    dwRc = GetFullPathNameW(szwFileName,
        _MAX_DIR,
        szwReturnedPath,
        &pPathPtr);

    if (dwRc == 0)
    {
        /* this test should have passed but didn't */
        free(szwFileName);
        Fail("GetFullPathNameW: ERROR -> Function failed for the "
            "file \"%s\" with error code: %ld.\n", szFileName, GetLastError());
    }
    /*
     * the returned value should be the current directory with the
     * file name appended
     */
    hFile = CreateFileW(szwFileName,
        GENERIC_READ,
        FILE_SHARE_READ,
        NULL,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        free(szwFileName);
        Fail("GetFullPathNameW: ERROR -> CreateFileW failed to create "
            "file \"%s\" with error code: %ld.\n",
            szFileName,
            GetLastError());
    }
    if (CloseHandle(hFile) != TRUE)
    {
        free(szwFileName);
        Trace("GetFullPathNameW: ERROR -> CloseHandle failed with error "
            "code: %ld.\n", GetLastError());
        if (DeleteFileA(szFileName) != TRUE)
        {
            Trace("GetFullPathNameW: ERROR -> DeleteFileW failed to "
                "delete the test file with error code: %ld.\n",
                GetLastError());
        }
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    /*
     * now try to create the file based on the returned value with the
     * CREATE_NEW option which should fail since the file should
     * already exist
     */
    hFile = CreateFileW(szwReturnedPath,
        GENERIC_READ,
        FILE_SHARE_READ,
        NULL,
        CREATE_NEW,
        FILE_ATTRIBUTE_NORMAL,
        NULL);
    if (hFile != INVALID_HANDLE_VALUE)
    {
        Trace("GetFullPathNameW: ERROR -> CreateFileW was able to "
            "CREATE_NEW the returned file \"%s\". The returned file "
            "name is therefore apparently wrong.\n",
            szwReturnedPath);
        if (CloseHandle(hFile) != TRUE)
        {
            Trace("GetFullPathNameW: ERROR -> CloseHandle failed with "
                "error code: %ld.\n", GetLastError());
        }
        if ((DeleteFileW(szwReturnedPath) != TRUE) ||
            (DeleteFileW(szwFileName) != TRUE))
        {
            Trace("GetFullPathNameW: ERROR -> DeleteFileW failed to "
                "delete the test files with error code: %ld.\n",
                GetLastError());
        }
        free(szwFileName);
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    /* now make sure the pPathPtr is the same as the file name */
    if (wcsncmp(pPathPtr, szwFileName, wcslen(szwFileName)) != 0)
    {
        Trace("GetFullPathNameW: ERROR -> %s != %s\n",
            pPathPtr, szFileName);
        if ((DeleteFileW(szwReturnedPath) != TRUE) ||
            (DeleteFileW(szwFileName) != TRUE))
        {
            Trace("GetFullPathNameW: ERROR -> DeleteFileW failed to "
                "delete the test files with error code: %ld.\n",
                GetLastError());
        }
        free(szwFileName);
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    /* clean up */
    free(szwFileName);
    if (DeleteFileA(szFileName) != TRUE)
    {
        Fail("GetFullPathNameW: ERROR -> DeleteFileW failed to "
            "delete \"%s\" with error code: %ld.\n",
            szFileName,
            GetLastError());
    }

    PAL_Terminate();
    return PASS;
}

