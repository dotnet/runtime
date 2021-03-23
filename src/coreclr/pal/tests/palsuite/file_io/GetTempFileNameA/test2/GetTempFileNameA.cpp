// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetTempFileNameA.c (test 2)
**
** Purpose: Tests the number of files GetTempFileNameA can create.
**
** Depends on:
**          GetFileAttributesA
**          oodles of free disk space (>4.07GB)
**
**
**===================================================================*/

#include <palsuite.h>



PALTEST(file_io_GetTempFileNameA_test2_paltest_gettempfilenamea_test2, "file_io/GetTempFileNameA/test2/paltest_gettempfilenamea_test2")
{
    UINT uiError = 0;
    DWORD dwError = 0;
    const UINT uUnique = 0;
    const char* szDot = {"."};
    const char* szValidPrefix = {"cfr"};
    char szReturnedName[256];
    DWORD i;

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /* test the number of temp files that can be created */
    for (i = 0; i < 0x10005; i++)
    {
        uiError = GetTempFileNameA(szDot, szValidPrefix, uUnique, szReturnedName);
        if (uiError == 0)
        {
            dwError = GetLastError();
            if (dwError == ERROR_FILE_EXISTS)
            {
                /* file already existes so break out of the loop */
                i--; /* decrement the count because it wasn't successful */
                break;
            }
            else
            {
                /* it was something other than the file already existing? */
                Fail("GetTempFileNameA: ERROR -> Call failed with a valid "
                    "path and prefix with the error code: %ld\n", GetLastError());
            }
        }
        else
        {
            /* verify temp file was created */
            if (GetFileAttributesA(szReturnedName) == -1)
            {
                Fail("GetTempFileNameA: ERROR -> GetFileAttributes failed "
                    "on the returned temp file \"%s\" with error code: %ld.\n",
                    szReturnedName,
                    GetLastError());
            }
        }
    }

    /* did it create more than 0xffff files */
    if (i > 0xffff)
    {
        Fail("GetTempFileNameA: ERROR -> Was able to create more than 0xffff"
            " temp files.\n");
    }

    PAL_Terminate();
    return PASS;
}
