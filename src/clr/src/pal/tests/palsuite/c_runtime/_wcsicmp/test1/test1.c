//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
int __cdecl main(int argc, char *argv[])
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
