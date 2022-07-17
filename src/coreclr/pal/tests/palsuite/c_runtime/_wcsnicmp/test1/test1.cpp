// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Take two wide strings and compare them, giving different lengths.
** Comparing str1 and str2 with str2 length, should return <0
** Comparing str2 and str1 with str2 length, should return >0
** Comparing str1 and str2 with str1 length, should return 0
** Bring in str3, which has a capital, but this function is doing a lower
** case compare.  Just ensure that two strings which differ only by capitals
** return 0.
**
**
**==========================================================================*/

#include <palsuite.h>

/*
 * Notes: uses wcslen
 */

PALTEST(c_runtime__wcsnicmp_test1_paltest_wcsnicmp_test1, "c_runtime/_wcsnicmp/test1/paltest_wcsnicmp_test1")
{
    WCHAR str1[] = {'f','o','o',0};
    WCHAR str2[] = {'f','o','o','x',0};
    WCHAR str3[] = {'f','O','o',0};
    WCHAR str4[] = {'A','B','C','D','E',0};
    WCHAR str5[] = {'A','B','C','D',']',0};
    WCHAR str6[] = {'a','b','c','d','e',0};
    WCHAR str7[] = {'a','b','c','d',']',0};
    char cstr1[] = "foo";
    char cstr2[] = "foox";
    char cstr3[] = "fOo";
    char cstr4[] = "ABCDE";
    char cstr5[] = "ABCD]";
    char cstr6[] = "abcde";
    char cstr7[] = "abcd]";


    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    if (_wcsnicmp(str1, str2, wcslen(str2)) >= 0)
    {
        Fail ("ERROR: wcsnicmp(\"%s\", \"%s\", %d) returned >= 0\n", cstr1,
                cstr2, wcslen(str2));
    }

    if (_wcsnicmp(str2, str1, wcslen(str2)) <= 0)
    {
        Fail ("ERROR: wcsnicmp(\"%s\", \"%s\", %d) returned <= 0\n", cstr2,
                cstr1, wcslen(str2));
    }

    if (_wcsnicmp(str1, str2, wcslen(str1)) != 0)
    {
        Fail ("ERROR: wcsnicmp(\"%s\", \"%s\", %d) returned != 0\n", cstr1,
                cstr2, wcslen(str1));
    }

    if (_wcsnicmp(str1, str3, wcslen(str1)) != 0)
    {
        Fail ("ERROR: wcsnicmp(\"%s\", \"%s\", %d) returned != 0\n", cstr1,
                cstr3, wcslen(str1));
    }

    /* new testing */

    /* str4 should be greater than str5 */
    if (_wcsnicmp(str4, str5, wcslen(str4)) <= 0)
    {
        Fail ("ERROR: _wcsnicmp(\"%s\", \"%s\", %d) returned >= 0\n",
                cstr4, cstr5, wcslen(str4));
    }

    /* str6 should be greater than str7 */
    if (_wcsnicmp(str6, str7, wcslen(str6)) <= 0)
    {
        Fail ("ERROR: _wcsnicmp(\"%s\", \"%s\", %d) returned <= 0\n",
                cstr6, cstr7, wcslen(str6));
    }

    PAL_Terminate();
    return PASS;
}

