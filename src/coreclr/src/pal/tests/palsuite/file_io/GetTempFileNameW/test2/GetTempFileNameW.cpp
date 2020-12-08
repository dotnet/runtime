// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetTempFileNameW.c (test 2)
**
** Purpose: Tests the PAL implementation of the GetTempFileNameW function.
**
**
**===================================================================*/

#include <palsuite.h>



PALTEST(file_io_GetTempFileNameW_test2_paltest_gettempfilenamew_test2, "file_io/GetTempFileNameW/test2/paltest_gettempfilenamew_test2")
{
    UINT uiError = 0;
    DWORD dwError = 0;
    const UINT uUnique = 0;
    WCHAR* wPrefix = NULL;
    WCHAR* wPath = NULL;
    WCHAR wReturnedName[256];
    DWORD i;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }


    // test the number of temp files that can be created
    wPrefix = convert("cfr");
    wPath = convert(".");
    for (i = 0; i < 0x10005; i++)
    {
        uiError = GetTempFileNameW(wPath, wPrefix, uUnique, wReturnedName);
        if (uiError == 0)
        {
            dwError = GetLastError();
            if (dwError == ERROR_FILE_EXISTS)
            {
                // file already existes so break out of the loop
                i--; // decrement the count because it wasn't successful
                break;
            }
            else
            {
                // it was something other than the file already existing?
                free (wPath);
                free (wPrefix);
                Fail("GetTempFileNameW: ERROR -> Call failed with a valid "
                    "path and prefix with the error code: %ld\n", GetLastError());
            }
        }
        else
        {
            // verify temp file was created
            if (GetFileAttributesW(wReturnedName) == -1)
            {
                free (wPath);
                free (wPrefix);
                Fail("GetTempFileNameW: ERROR -> GetFileAttributes failed "
                    "on the returned temp file with error code: %ld.\n", 
                    GetLastError());
            }
        }
    }

    free (wPath);
    free (wPrefix);

    // did it create more than 0xffff files
    if (i > 0xffff)
    {
        Fail("GetTempFileNameW: ERROR -> Was able to create more than 0xffff"
            " temp files.\n");
    }

    PAL_Terminate();
    return PASS;
}
