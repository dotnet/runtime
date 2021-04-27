// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Tests that wcsncmp case-sensitively compares wide strings, making sure that
** the count argument is handled correctly.
**
**
**==========================================================================*/



#include <palsuite.h>

/*
 * Notes: uses wcslen.
 */

PALTEST(c_runtime_wcsncmp_test1_paltest_wcsncmp_test1, "c_runtime/wcsncmp/test1/paltest_wcsncmp_test1")
{
    WCHAR str1[] = {'f','o','o',0};
    WCHAR str2[] = {'f','o','o','x',0};
    WCHAR str3[] = {'f','O','o',0};
    char cstr1[] = "foo";
    char cstr2[] = "foox";
    char cstr3[] = "fOo";
    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }



    if (wcsncmp(str1, str2, wcslen(str2)) >= 0)
    {
        Fail("ERROR: wcsncmp(\"%s\", \"%s\", %d) returned >= 0\n", cstr1, 
            cstr2, wcslen(str2));
    }

    if (wcsncmp(str2, str1, wcslen(str2)) <= 0)
    {
        Fail("ERROR: wcsncmp(\"%s\", \"%s\", %d) returned <= 0\n", cstr2, 
            cstr1, wcslen(str2));
    }

    if (wcsncmp(str1, str2, wcslen(str1)) != 0)
    {
        Fail("ERROR: wcsncmp(\"%s\", \"%s\", %d) returned != 0\n", cstr1, 
            cstr2, wcslen(str1));
    }

    if (wcsncmp(str1, str3, wcslen(str1)) <= 0)
    {
        Fail("ERROR: wcsncmp(\"%s\", \"%s\", %d) returned >= 0\n", cstr1, 
            cstr3, wcslen(str1));
    }

    if (wcsncmp(str3, str1, wcslen(str1)) >= 0)
    {
        Fail("ERROR: wcsncmp(\"%s\", \"%s\", %d) returned >= 0\n", cstr3, 
            cstr1, wcslen(str1));
    }

    PAL_Terminate();
    return PASS;
}

