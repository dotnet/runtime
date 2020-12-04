// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetTempFileNameA.c (test 3)
**
** Purpose: Tests the PAL implementation of the GetTempFileNameA function.
**          Checks the file attributes and ensures that getting a file name,
**          deleting the file and getting another doesn't produce the same 
**          as the just deleted file. Also checks the file size is 0.
**
** Depends on:
**          GetFileAttributesA
**          CloseHandle
**          DeleteFileA
**          CreateFileA
**          GetFileSize
**
**
**===================================================================*/

#include <palsuite.h>



PALTEST(file_io_GetTempFileNameA_test3_paltest_gettempfilenamea_test3, "file_io/GetTempFileNameA/test3/paltest_gettempfilenamea_test3")
{
    const UINT uUnique = 0;
    UINT uiError;
    const char* szDot = {"."};
    char szReturnedName[MAX_LONGPATH];
    char szReturnedName_02[MAX_LONGPATH];
    DWORD dwFileSize = 0;
    HANDLE hFile;

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /* valid path with null prefix */
    uiError = GetTempFileNameA(szDot, NULL, uUnique, szReturnedName);
    if (uiError == 0)
    {
        Fail("GetTempFileNameA: ERROR -> Call failed with a valid path "
            "with the error code: %u.\n", 
            GetLastError());
    }

    /* verify temp file was created */
    if (GetFileAttributesA(szReturnedName) == -1) 
    {
        Fail("GetTempFileNameA: ERROR -> GetFileAttributes failed on the "
            "returned temp file \"%s\" with error code: %u.\n", 
            szReturnedName,
            GetLastError());
    }

    /* 
    ** verify that the file size is 0 bytes
    */

    hFile = CreateFileA(szReturnedName,
                        GENERIC_READ,
                        FILE_SHARE_READ,
                        NULL,
                        OPEN_EXISTING,
                        FILE_ATTRIBUTE_NORMAL,
                        NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Trace("GetTempFileNameA: ERROR -> CreateFileA failed to open"
            " the created temp file with error code: %u.\n", 
            GetLastError());
        if (!DeleteFileA(szReturnedName))
        {
            Trace("GetTempFileNameA: ERROR -> DeleteFileA failed to delete"
                " the created temp file with error code: %u.\n", 
                GetLastError());
        }
        Fail("");
    }

    if ((dwFileSize = GetFileSize(hFile, NULL)) != (DWORD)0)
    {
        Trace("GetTempFileNameA: ERROR -> GetFileSize returned %u whereas"
            "it should have returned 0.\n", 
            dwFileSize);
        if (!CloseHandle(hFile))
        {
            Trace("GetTempFileNameA: ERROR -> CloseHandle failed. "
                "GetLastError returned: %u.\n", 
                GetLastError());
        }
        if (!DeleteFileA(szReturnedName))
        {
            Trace("GetTempFileNameA: ERROR -> DeleteFileA failed to delete"
                " the created temp file with error code: %u.\n", 
                GetLastError());
        }
        Fail("");
    }


    if (!CloseHandle(hFile))
    {
        Fail("GetTempFileNameA: ERROR -> CloseHandle failed. "
            "GetLastError returned: %u.\n", 
            GetLastError());
    }

    if (DeleteFileA(szReturnedName) != TRUE)
    {
        Fail("GetTempFileNameA: ERROR -> DeleteFileA failed to delete"
            " the created temp file with error code: %u.\n", 
            GetLastError());
    }

    /* get another and make sure it's not the same as the last */
    uiError = GetTempFileNameA(szDot, NULL, uUnique, szReturnedName_02);
    if (uiError == 0)
    {
        Fail("GetTempFileNameA: ERROR -> Call failed with a valid path "
            "with the error code: %u.\n", 
            GetLastError());
    }

    /* did we get different names? */
    if (strcmp(szReturnedName, szReturnedName_02) == 0)
    {
        Trace("GetTempFileNameA: ERROR -> The first call returned \"%s\". "
            "The second call returned \"%s\" and the two should not be"
            " the same.\n",
            szReturnedName,
            szReturnedName_02);
        if (!DeleteFileA(szReturnedName_02))
        {
            Trace("GetTempFileNameA: ERROR -> DeleteFileA failed to delete"
                " the created temp file with error code: %u.\n", 
                GetLastError());
        }
        Fail("");
    }

    /* clean up */
    if (!DeleteFileA(szReturnedName_02))
    {
        Fail("GetTempFileNameA: ERROR -> DeleteFileA failed to delete"
            " the created temp file with error code: %u.\n", 
            GetLastError());
    }


    PAL_Terminate();
    return PASS;
}
