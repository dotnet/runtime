// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  GetCurrentDirectoryA.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetCurrentDirectoryA function.
**
**
**===================================================================*/

#include <palsuite.h>

const char* szFileName = "blah";


int __cdecl main(int argc, char *argv[])
{
    DWORD dwRc = 0;
    DWORD dwRc2 = 0;
    char szReturnedPath[_MAX_PATH+1];
    char szCurrentDir[_MAX_PATH+1];
    LPSTR pPathPtr;
    size_t nCount = 0;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    // use GetFullPathName to to get the current path by stripping 
    // the file name off the end
    memset(szReturnedPath, 0, sizeof(char)*(_MAX_PATH+1));
    dwRc = GetFullPathNameA(szFileName, _MAX_PATH, szReturnedPath, &pPathPtr);
    if (dwRc == 0)
    {
        // GetFullPathName failed
        Fail("GetCurrentDirectoryA: ERROR -> GetFullPathNameA failed "
            "with error code: %ld.\n", GetLastError());
    }
    else if(dwRc > _MAX_PATH)
    {
        Fail("GetCurrentDirectoryA: ERROR -> The path name GetFullPathNameA "
            "returned is longer than _MAX_PATH characters.\n");
    }


    // strip the file name from the full path to get the current path
    nCount = strlen(szReturnedPath) - strlen(szFileName) - 1;
    memset(szCurrentDir, 0, sizeof(char)*(_MAX_PATH+1));
    strncpy(szCurrentDir, szReturnedPath, nCount);

    // compare the results of GetCurrentDirectoryA with the above
    memset(szReturnedPath, 0, sizeof(char)*(_MAX_PATH+1));
    dwRc = GetCurrentDirectoryA((sizeof(char)*(_MAX_PATH+1)), szReturnedPath);
    if (dwRc == 0)
    {
        Fail("GetCurrentDirectoryA: ERROR -> GetCurrentDirectoryA failed "
            "with error code: %ld.\n", GetLastError());
    }
    else if(dwRc > _MAX_PATH)
    {
        Fail("GetCurrentDirectoryA: ERROR -> The path name "
            "returned is longer than _MAX_PATH characters.\n");
    }


    /* test case  the passed buffer size  is not big enough
     * function should return the size required + 1 a terminating null character
     */

    /* good buffer size */
    dwRc = GetCurrentDirectoryA((sizeof(CHAR)*(_MAX_PATH+1)), szReturnedPath);

    /* small buffer (0 size)*/
    dwRc2 = GetCurrentDirectoryA(0, szReturnedPath);
    if (dwRc2 != (dwRc+1) )
    {
        Fail("GetCurrentDirectoryA: ERROR -> failed to give the correct "
             "return value when passed a buffer not big enough. "
             "Expected %u while result is %u \n",(dwRc+1),dwRc2);

    }

    if (strcmp(szReturnedPath, szCurrentDir) != 0)
    {
        Fail("GetCurrentDirectoryA: ERROR -> The computed and returned "
            "directories do not compare.\n");
    }


    PAL_Terminate();
    return PASS;
}

