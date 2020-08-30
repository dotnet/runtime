// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetCurrentDirectoryW.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetCurrentDirectoryW function.
**
**
**===================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    DWORD dwRc = 0;
    DWORD dwRc2 = 0;
    WCHAR szwReturnedPath[_MAX_PATH+1];
    WCHAR szwCurrentDir[_MAX_PATH+1];
    WCHAR szwFileName[_MAX_PATH] = {'b','l','a','h','\0'};
    LPWSTR pPathPtr;
    size_t nCount = 0;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* use GetFullPathName to to get the current path by stripping 
     * the file name off the end */
    memset(szwReturnedPath, 0, sizeof(WCHAR)*(_MAX_PATH+1));
    dwRc = GetFullPathNameW(szwFileName, _MAX_PATH, szwReturnedPath, &pPathPtr);
    if (dwRc == 0)
    {
        /* GetFullPathName failed */
        Fail("GetCurrentDirectoryW: ERROR -> GetFullPathNameW failed "
            "with error code: %ld.\n", GetLastError());
    }
    else if(dwRc >_MAX_PATH)
    {
        Fail("GetCurrentDirectoryW: ERROR -> The path name GetFullPathNameW "
            "returned is longer than _MAX_PATH characters.\n");
    }

    /* strip the file name from the full path to get the current path */
    nCount = wcslen(szwReturnedPath) - wcslen(szwFileName) - 1;
    memset(szwCurrentDir, 0, sizeof(WCHAR)*(_MAX_PATH+1));
    memcpy(szwCurrentDir, szwReturnedPath, nCount*sizeof(WCHAR));

    /* compare the results of GetCurrentDirectoryW with the above */
    memset(szwReturnedPath, 0, sizeof(WCHAR)*(_MAX_PATH+1));
    dwRc = GetCurrentDirectoryW((sizeof(WCHAR)*(_MAX_PATH+1)), szwReturnedPath);
    if (dwRc == 0)
    {
        Fail("GetCurrentDirectoryW: ERROR -> GetCurrentDirectoryW failed "
            "with error code: %ld.\n", GetLastError());
    }
    else if(dwRc >_MAX_PATH)
    {
        Fail("GetCurrentDirectoryW: ERROR -> The path name "
            "returned is longer than _MAX_PATH characters.\n");
    }

    /* check to see whether the length of the returned string is equal to
     * the DWORD returned by GetCurrentDirectoryW.
     */
    if(wcslen(szwReturnedPath) != dwRc)
    {
        Fail("GetCurrentDirectoryW: ERROR -> The Length of the path name "
            "returned \"%u\" is not equal to the return value of the "
            "function \"%u\".\n" , wcslen(szwReturnedPath), dwRc);
    }



    /* test case  the passed buffer size  is not big enough
     * function should return the size required + 1 for a terminating null character
     */

    /* good buffer size */
    dwRc = GetCurrentDirectoryW((sizeof(WCHAR)*(_MAX_PATH+1)), szwReturnedPath);

    /* small buffer (0 size)*/
    dwRc2 = GetCurrentDirectoryW(0, szwReturnedPath);
    if (dwRc2 != (dwRc+1) )
    {
        Fail("GetCurrentDirectoryW: ERROR -> failed to give the correct "
             "return value when passed a buffer not big enough. "
             "Expected %u while result is %u ",(dwRc+1),dwRc2);

    }

    if (wcsncmp(szwReturnedPath, szwCurrentDir, wcslen(szwReturnedPath)) != 0)
    {
        Fail("GetCurrentDirectoryW: ERROR -> The computed and returned "
            "directories do not compare.\n");
    }


    PAL_Terminate();
    return PASS;
}

