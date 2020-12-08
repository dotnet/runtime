// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Tests that _wcsicmp correctly compares two strings with 
**          case insensitivity.
**
**
**==========================================================================*/

#include <palsuite.h>

/*
 * Note: The _wcsicmp is dependent on the LC_CTYPE category of the locale,
 *      and this is ignored by these tests.
 */
PALTEST(c_runtime__wcsicmp_test1_paltest_wcsicmp_test1, "c_runtime/_wcsicmp/test1/paltest_wcsicmp_test1")
{
    WCHAR str1[] = {'f','o','o',0};
    WCHAR str2[] = {'f','O','o',0};
    WCHAR str3[] = {'f','o','o','_','b','a','r',0};
    WCHAR str4[] = {'f','o','o','b','a','r',0};

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    if (_wcsicmp(str1, str2) != 0)
    {
        Fail ("ERROR: _wcsicmp returning incorrect value:\n"
                "_wcsicmp(\"%S\", \"%S\") != 0\n", str1, str2);
    }

    if (_wcsicmp(str2, str3) >= 0)
    {
        Fail ("ERROR: _wcsicmp returning incorrect value:\n"
                "_wcsicmp(\"%S\", \"%S\") >= 0\n", str2, str3);
    }

    if (_wcsicmp(str3, str4) >= 0)
    {
        Fail ("ERROR: _wcsicmp returning incorrect value:\n"
                "_wcsicmp(\"%S\", \"%S\") >= 0\n", str3, str4);
    }

    if (_wcsicmp(str4, str1) <= 0)
    {
        Fail ("ERROR: _wcsicmp returning incorrect value:\n"
                "_wcsicmp(\"%S\", \"%S\") <= 0\n", str4, str1);
    }

    if (_wcsicmp(str3, str2) <= 0)
    {
        Fail ("ERROR: _wcsicmp returning incorrect value:\n"
                "_wcsicmp(\"%S\", \"%S\") <= 0\n", str2, str3);
    }

    PAL_Terminate();
    return PASS;
}
