// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  findnextfilea.c
**
** Purpose: Tests the PAL implementation of the FindNextFileA function.
**          Tests '*' and '*.*' to ensure that '.' and '..' are
**          returned in the expected order
**
**
**===================================================================*/

#include <palsuite.h>
                                                                   

const char* szDot =         ".";
const char* szDotDot =      "..";
const char* szStar =        "*";
const char* szStarDotStar = "*.*";


static void DoTest(const char* szDir, 
                   const char* szResult1, 
                   const char* szResult2)
{
    HANDLE hFind;
    WIN32_FIND_DATA findFileData;

    /*
    ** find the first
    */
    if ((hFind = FindFirstFileA(szDir, &findFileData)) == INVALID_HANDLE_VALUE)
    {
        Fail("FindNextFileA: ERROR -> FindFirstFileA(\"%s\") failed. "
            "GetLastError returned %u.\n", 
            szStar,
            GetLastError());
    }

    /* did we find the expected */
    if (strcmp(szResult1, findFileData.cFileName) != 0)
    {
        if (!FindClose(hFind))
        {
            Trace("FindNextFileA: ERROR -> Failed to close the find handle. "
                "GetLastError returned %u.\n",
                GetLastError());
        }
        Fail("FindNextFileA: ERROR -> FindFirstFile(\"%s\") didn't find"
            " the expected \"%s\" but found \"%s\" instead.\n",
            szDir,
            szResult1,
            findFileData.cFileName);
    }

    /* we found the first expected, let's see if we find the next expected*/
    if (!FindNextFileA(hFind, &findFileData))
    {
        Trace("FindNextFileA: ERROR -> FindNextFileA should have found \"%s\"" 
            " but failed. GetLastError returned %u.\n",
            szResult2,
            GetLastError());
        if (!FindClose(hFind))
        {
            Trace("FindNextFileA: ERROR -> Failed to close the find handle. "
                "GetLastError returned %u.\n",
                GetLastError());
        }
        Fail("");
    }

    /* we found something, but was it '.' */
    if (strcmp(szResult2, findFileData.cFileName) != 0)
    {
        if (!FindClose(hFind))
        {
            Trace("FindNextFileA: ERROR -> Failed to close the find handle. "
                "GetLastError returned %u.\n",
                GetLastError());
        }
        Fail("FindNextFileA: ERROR -> FindNextFileA based on \"%s\" didn't find"
            " the expected \"%s\" but found \"%s\" instead.\n",
            szDir,
            szResult2,
            findFileData.cFileName);
    }
}

PALTEST(file_io_FindNextFileA_test2_paltest_findnextfilea_test2, "file_io/FindNextFileA/test2/paltest_findnextfilea_test2")
{

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    DoTest(szStar, szDot, szDotDot);
    DoTest(szStarDotStar, szDot, szDotDot);


    PAL_Terminate();  

    return PASS;
}
