// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetTempFileNameW.c (test 3)
**
** Purpose: Tests the PAL implementation of the GetTempFileNameW function.
**          Checks the file attributes and ensures that getting a file name,
**          deleting the file and getting another doesn't produce the same 
**          as the just deleted file. Also checks the file size is 0.
**
** Depends on:
**          GetFileAttributesW
**          DeleteFileW
**          CreateFileW
**          GetFileSize
**          CloseHandle
**
**
**===================================================================*/

#include <palsuite.h>



PALTEST(file_io_GetTempFileNameW_test3_paltest_gettempfilenamew_test3, "file_io/GetTempFileNameW/test3/paltest_gettempfilenamew_test3")
{
    const UINT uUnique = 0;
    UINT uiError;
    WCHAR szwReturnedName[MAX_LONGPATH];
    WCHAR szwReturnedName_02[MAX_LONGPATH];
    DWORD dwFileSize = 0;
    HANDLE hFile;
    const WCHAR szwDot[] = {'.','\0'};
    const WCHAR szwPre[] = {'c','\0'};

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /* valid path with null prefix */
    uiError = GetTempFileNameW(szwDot, szwPre, uUnique, szwReturnedName);
    if (uiError == 0)
    {
        Fail("GetTempFileNameW: ERROR -> Call failed with a valid path "
            "with the error code: %u.\n", 
            GetLastError());
    }

    /* verify temp file was created */
    if (GetFileAttributesW(szwReturnedName) == -1) 
    {
        Fail("GetTempFileNameW: ERROR -> GetFileAttributes failed on the "
            "returned temp file \"%S\" with error code: %u.\n", 
            szwReturnedName,
            GetLastError());
    }

    /* 
    ** verify that the file size is 0 bytes
    */

    hFile = CreateFileW(szwReturnedName,
                        GENERIC_READ,
                        FILE_SHARE_READ,
                        NULL,
                        OPEN_EXISTING,
                        FILE_ATTRIBUTE_NORMAL,
                        NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Trace("GetTempFileNameW: ERROR -> CreateFileW failed to open"
            " the created temp file with error code: %u.\n", 
            GetLastError());
        if (!DeleteFileW(szwReturnedName))
        {
            Trace("GetTempFileNameW: ERROR -> DeleteFileW failed to delete"
                " the created temp file with error code: %u.\n", 
                GetLastError());
        }
        Fail("");
    }

    if ((dwFileSize = GetFileSize(hFile, NULL)) != (DWORD)0)
    {
        Trace("GetTempFileNameW: ERROR -> GetFileSize returned %u whereas"
            "it should have returned 0.\n", 
            dwFileSize);
        if (!CloseHandle(hFile))
        {
            Trace("GetTempFileNameW: ERROR -> CloseHandle was unable to close the "
                "opened file. GetLastError returned %u.\n",
                GetLastError());
        }
        if (!DeleteFileW(szwReturnedName))
        {
            Trace("GetTempFileNameW: ERROR -> DeleteFileW failed to delete"
                " the created temp file with error code: %u.\n", 
                GetLastError());
        }
        Fail("");
    }

    if (!CloseHandle(hFile))
    {
        Fail("GetTempFileNameW: ERROR -> CloseHandle was unable to close the "
            "opened file. GetLastError returned %u.\n",
            GetLastError());
    }


    /* delete the file to see if we get the same name next time around */
    if (DeleteFileW(szwReturnedName) != TRUE)
    {
        Fail("GetTempFileNameW: ERROR -> DeleteFileW failed to delete"
            " the created temp file with error code: %u.\n", 
            GetLastError());
    }

    /* get another and make sure it's not the same as the last */
    uiError = GetTempFileNameW(szwDot, szwPre, uUnique, szwReturnedName_02);
    if (uiError == 0)
    {
        Fail("GetTempFileNameW: ERROR -> Call failed with a valid path "
            "with the error code: %u.\n", 
            GetLastError());
    }

    /* did we get different names? */
    if (wcsncmp(szwReturnedName, szwReturnedName_02, wcslen(szwReturnedName)) == 0)
    {
        Fail("GetTempFileNameW: ERROR -> The first call returned \"%S\". "
            "The second call returned \"%S\" and the two should not be"
            " the same.\n",
            szwReturnedName,
            szwReturnedName_02);
        if (!DeleteFileW(szwReturnedName_02))
        {
            Trace("GetTempFileNameW: ERROR -> DeleteFileW failed to delete"
                " the created temp file with error code: %u.\n", 
                GetLastError());
        }
        Fail("");
    }

    /* clean up */
    if (!DeleteFileW(szwReturnedName_02))
    {
        Fail("GetTempFileNameW: ERROR -> DeleteFileW failed to delete"
            " the created temp file with error code: %u.\n", 
            GetLastError());
    }


    PAL_Terminate();
    return PASS;
}
