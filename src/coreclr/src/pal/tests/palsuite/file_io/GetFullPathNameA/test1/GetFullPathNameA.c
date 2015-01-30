//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  GetFullPathNameA.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetFullPathNameA function.
**
**
**===================================================================*/

#include <palsuite.h>

const char* szFileName = "testing.tmp";

int __cdecl main(int argc, char *argv[])
{
    DWORD dwRc = 0;
    char szReturnedPath[_MAX_DIR+1];
    char szShortBuff[2];
    LPSTR pPathPtr;
    HANDLE hFile = NULL;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* perform a short buffer test */
    if (GetFullPathNameA(szFileName, 2, szShortBuff, &pPathPtr) <= 2)
    {
        /* this test should have failed but didn't */
        Fail("GetFullPathNameA: ERROR -> The API was passed a buffer that was"
            " too small for the path name and yet it apparently passed.\n");
    }

    memset(szReturnedPath, 0, _MAX_DIR+1);
    dwRc = GetFullPathNameA(szFileName, 
        _MAX_DIR, 
        szReturnedPath, 
        &pPathPtr);

    if (dwRc == 0)
    {
        // this test should have passed but didn't
        Fail("GetFullPathNameA: ERROR -> Function failed for the "
            "file \"%s\" with error code: %ld.\n", szFileName, GetLastError());
    }

    // the returned value should be the current directory with the 
    // file name appended
    hFile = CreateFileA(szFileName,
        GENERIC_READ,
        FILE_SHARE_READ,
        NULL,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail("GetFullPathNameA: ERROR -> CreateFileA failed to create "
            "file \"%s\" with error code: %ld.\n", 
            szFileName,
            GetLastError());
    }
    if (CloseHandle(hFile) != TRUE)
    {
        Fail("GetFullPathNameA: ERROR -> CloseHandle failed with error "
            "code: %ld.\n", GetLastError());
    }

    // now try to create the file based on the returned value with the 
    // CREATE_NEW option which should fail since the file should 
    // already exist
    hFile = CreateFileA(szReturnedPath,
        GENERIC_READ,
        FILE_SHARE_READ,
        NULL,
        CREATE_NEW,
        FILE_ATTRIBUTE_NORMAL,
        NULL);
    if (hFile != INVALID_HANDLE_VALUE)
    {
        Fail("GetFullPathNameA: ERROR -> CreateFileA was able to "
            "CREATE_NEW the returned file \"%s\". The returned file "
            "name is therefore apparently wrong.\n", 
            szReturnedPath);
        if (CloseHandle(hFile) != TRUE)
        {
            Fail("GetFullPathNameA: ERROR -> CloseHandle failed with "
                "error code: %ld.\n", GetLastError());
        }
        if ((DeleteFileA(szReturnedPath) != TRUE) ||
            (DeleteFileA(szFileName) != TRUE))
        {
            Fail("GetFullPathNameA: ERROR -> DeleteFileA failed to "
                "delete the test files with error code: %ld.\n", 
                GetLastError());
        }
    }

    // now make sure the pPathPtr is the same as the file name
    if (strcmp(pPathPtr, szFileName) != 0)
    {
        Fail("GetFullPathNameA: ERROR -> %s != %s\n",
            pPathPtr, szFileName);
    }
    if (DeleteFileA(szFileName) != TRUE)
    {
        Fail("GetFullPathNameA: ERROR -> DeleteFileA failed to "
            "delete \"%s\" with error code: %ld.\n",
            szFileName,
            GetLastError());
    }

    PAL_Terminate();
    return PASS;
}

