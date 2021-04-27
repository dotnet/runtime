// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  FindNextFileW.c
**
** Purpose: Tests the PAL implementation of the FindNextFileW function.
**          Tests '*' and '*.*' to ensure that '.' and '..' are
**          returned in the expected order
**
**
**===================================================================*/

#include <palsuite.h>
                                                                   

const WCHAR szwDot[] =         {'.','\0'};
const WCHAR szwDotDot[] =      {'.','.','\0'};
const WCHAR szwStar[] =        {'*','\0'};
const WCHAR szwStarDotStar[] = {'*','.','*','\0'};


static void DoTest(const WCHAR* szwDir, 
                   const WCHAR* szwResult1, 
                   const WCHAR* szwResult2)
{
    HANDLE hFind;
    WIN32_FIND_DATAW findFileData;

    /*
    ** find the first
    */
    if ((hFind = FindFirstFileW(szwDir, &findFileData)) == INVALID_HANDLE_VALUE)
    {
        Fail("FindNextFileW: ERROR -> FindFirstFileW(\"%S\") failed. "
            "GetLastError returned %u.\n", 
            szwStar,
            GetLastError());
    }

    /* did we find the expected */
    if (wcscmp(szwResult1, findFileData.cFileName) != 0)
    {
        if (!FindClose(hFind))
        {
            Trace("FindNextFileW: ERROR -> Failed to close the find handle. "
                "GetLastError returned %u.\n",
                GetLastError());
        }
        Fail("FindNextFileW: ERROR -> FindFirstFile(\"%S\") didn't find"
            " the expected \"%S\" but found \"%S\" instead.\n",
            szwDir,
            szwResult1,
            findFileData.cFileName);
    }

    /* we found the first expected, let's see if we find the next expected*/
    if (!FindNextFileW(hFind, &findFileData))
    {
        Trace("FindNextFileW: ERROR -> FindNextFileW should have found \"%S\"" 
            " but failed. GetLastError returned %u.\n",
            szwResult2,
            GetLastError());
        if (!FindClose(hFind))
        {
            Trace("FindNextFileW: ERROR -> Failed to close the find handle. "
                "GetLastError returned %u.\n",
                GetLastError());
        }
        Fail("");
    }

    /* we found something, but was it '.' */
    if (wcscmp(szwResult2, findFileData.cFileName) != 0)
    {
        if (!FindClose(hFind))
        {
            Trace("FindNextFileW: ERROR -> Failed to close the find handle. "
                "GetLastError returned %u.\n",
                GetLastError());
        }
        Fail("FindNextFileW: ERROR -> FindNextFileW based on \"%S\" didn't find"
            " the expected \"%S\" but found \"%S\" instead.\n",
            szwDir,
            szwResult2,
            findFileData.cFileName);
    }
}

PALTEST(file_io_FindNextFileW_test2_paltest_findnextfilew_test2, "file_io/FindNextFileW/test2/paltest_findnextfilew_test2")
{

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    DoTest(szwStar, szwDot, szwDotDot);
    DoTest(szwStarDotStar, szwDot, szwDotDot);


    PAL_Terminate();  

    return PASS;
}
