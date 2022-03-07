// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetTempFileNameA.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetTempFileNameA function.
**
** Depends on:
**          GetFileAttributesA
**          DeleteFileA
**
**
**===================================================================*/

#include <palsuite.h>



PALTEST(file_io_GetTempFileNameA_test1_paltest_gettempfilenamea_test1, "file_io/GetTempFileNameA/test1/paltest_gettempfilenamea_test1")
{
    UINT uiError = 0;
    const UINT uUnique = 0;
    const char* szDot = {"."};
    const char* szValidPrefix = {"cfr"};
    const char* szLongValidPrefix = {"cfrwxyz"};
    char szReturnedName[256];
    char szTempString[256];

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* valid path with null prefix */
    uiError = GetTempFileNameA(szDot, NULL, uUnique, szReturnedName);
    if (uiError == 0)
    {
        Fail("GetTempFileNameA: ERROR -> Call failed with a valid path "
            "with the error code: %ld\n", GetLastError());
    }
    else
    {
        /* verify temp file was created */
        if (GetFileAttributesA(szReturnedName) == -1)
        {
            Fail("GetTempFileNameA: ERROR -> GetFileAttributes failed on the "
                "returned temp file \"%s\" with error code: %ld.\n",
                szReturnedName,
                GetLastError());
        }
        if (DeleteFileA(szReturnedName) != TRUE)
        {
            Fail("GetTempFileNameA: ERROR -> DeleteFileW failed to delete"
                "the created temp file with error code: %ld.\n", GetLastError());
        }
    }


    /* valid path with valid prefix */
    uiError = GetTempFileNameA(szDot, szValidPrefix, uUnique, szReturnedName);
    if (uiError == 0)
    {
        Fail("GetTempFileNameA: ERROR -> Call failed with a valid path and "
            "prefix with the error code: %ld\n", GetLastError());
    }
    else
    {
        /* verify temp file was created */
        if (GetFileAttributesA(szReturnedName) == -1)
        {
            Fail("GetTempFileNameA: ERROR -> GetFileAttributes failed on the "
                "returned temp file \"%s\" with error code: %ld.\n",
                szReturnedName,
                GetLastError());
        }
        if (DeleteFileA(szReturnedName) != TRUE)
        {
            Fail("GetTempFileNameA: ERROR -> DeleteFileW failed to delete"
                "the created temp \"%s\" file with error code: %ld.\n",
                szReturnedName,
                GetLastError());
        }
    }

    /* valid path with long prefix */
    uiError = GetTempFileNameA(szDot, szLongValidPrefix, uUnique, szReturnedName);
    if (uiError == 0)
    {
        Fail("GetTempFileNameA: ERROR -> Call failed with a valid path and "
            "prefix with the error code: %ld\n", GetLastError());
    }
    else
    {
        /* verify temp file was created */
        if (GetFileAttributesA(szReturnedName) == -1)
        {
            Fail("GetTempFileNameA: ERROR -> GetFileAttributes failed on the "
                "returned temp file \"%s\" with error code: %ld.\n",
                szReturnedName,
                GetLastError());
        }

        /* now verify that it only used the first 3 characters of the prefix */
        sprintf_s(szTempString, ARRAY_SIZE(szTempString), "%s\\%s", szDot, szLongValidPrefix);
        if (strncmp(szTempString, szReturnedName, 6) == 0)
        {
            Fail("GetTempFileNameA: ERROR -> It appears that an improper prefix "
                "was used.\n");
        }

        if (DeleteFileA(szReturnedName) != TRUE)
        {
            Fail("GetTempFileNameA: ERROR -> DeleteFileW failed to delete"
                "the created temp file \"%s\" with error code: %ld.\n",
                szReturnedName,
                GetLastError());
        }
    }

    PAL_Terminate();
    return PASS;
}
