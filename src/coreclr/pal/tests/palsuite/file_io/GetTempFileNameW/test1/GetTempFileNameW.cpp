// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetTempFileNameW.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetTempFileNameW function.
**
**
**===================================================================*/

#include <palsuite.h>



PALTEST(file_io_GetTempFileNameW_test1_paltest_gettempfilenamew_test1, "file_io/GetTempFileNameW/test1/paltest_gettempfilenamew_test1")
{
    UINT uiError = 0;
    const UINT uUnique = 0;
    WCHAR* wPrefix = NULL;
    WCHAR* wPath = NULL;
    WCHAR wReturnedName[256];
    WCHAR wTempString[256];

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }


    // valid path with null ext
    wPath = convert(".");
    uiError = GetTempFileNameW(wPath, wPrefix, uUnique, wReturnedName);
    free (wPath);
    if (uiError == 0)
    {
        Fail("GetTempFileNameW: ERROR -> Call failed with a valid path "
            "with the error code: %ld\n", GetLastError());
    }
    else
    {
        // verify temp file was created
        if (GetFileAttributesW(wReturnedName) == -1)
        {
            Fail("GetTempFileNameW: ERROR -> GetFileAttributes failed on the "
                "returned temp file with error code: %ld.\n", GetLastError());
        }
        if (DeleteFileW(wReturnedName) != TRUE)
        {
            Fail("GetTempFileNameW: ERROR -> DeleteFileW failed to delete"
                "the created temp file with error code: %lld.\n", GetLastError());
        }
    }


    // valid path with valid prefix
    wPath = convert(".");
    wPrefix = convert("cfr");
    uiError = GetTempFileNameW(wPath, wPrefix, uUnique, wReturnedName);
    free (wPath);
    free (wPrefix);
    if (uiError == 0)
    {
        Fail("GetTempFileNameW: ERROR -> Call failed with a valid path and "
            "prefix with the error code: %ld\n", GetLastError());
    }
    else
    {
        // verify temp file was created
        if (GetFileAttributesW(wReturnedName) == -1)
        {
            Fail("GetTempFileNameW: ERROR -> GetFileAttributes failed on the "
                "returned temp file with error code: %ld.\n", GetLastError());
        }
        if (DeleteFileW(wReturnedName) != TRUE)
        {
            Fail("GetTempFileNameW: ERROR -> DeleteFileW failed to delete"
                "the created temp file with error code: %lld.\n", GetLastError());
        }
    }

    // valid path with long prefix
    wPath = convert(".");
    wPrefix = convert("cfrwxyz");
    uiError = GetTempFileNameW(wPath, wPrefix, uUnique, wReturnedName);
    if (uiError == 0)
    {
        free (wPath);
        free (wPrefix);
        Fail("GetTempFileNameW: ERROR -> Call failed with a valid path and "
            "prefix with the error code: %ld\n", GetLastError());
    }
    else
    {
        // verify temp file was created
        if (GetFileAttributesW(wReturnedName) == -1)
        {
            free (wPath);
            free (wPrefix);
            Fail("GetTempFileNameW: ERROR -> GetFileAttributes failed on the "
                "returned temp file with error code: %ld.\n", GetLastError());
        }

        // now verify that it only used the first 3 characters of the prefix
        WCHAR* wCurr = wTempString;
        memcpy(wCurr, wPath, wcslen(wPath) * sizeof(WCHAR));
        wCurr += wcslen(wPath);
        wcscat(wCurr, W("\\"));
        wCurr += wcslen(W("\\"));
        wcscat(wCurr, wPrefix);
        if (memcmp(wTempString, wReturnedName, wcslen(wTempString)*sizeof(WCHAR)) == 0)
        {
            free (wPath);
            free (wPrefix);
            Fail("GetTempFileNameW: ERROR -> It appears that an improper prefix "
                "was used.\n");
        }

        if (DeleteFileW(wReturnedName) != TRUE)
        {
            free (wPath);
            free (wPrefix);
            Fail("GetTempFileNameW: ERROR -> DeleteFileW failed to delete"
                "the created temp file with error code: %lld.\n", GetLastError());
        }
    }

    free (wPath);
    free (wPrefix);
    PAL_Terminate();
    return PASS;
}
