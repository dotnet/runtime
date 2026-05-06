// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Test to that wcscat correctly concatenates wide strings, including placing
** null pointers.
**
**==========================================================================*/

#include <palsuite.h>

/*
 * Notes: uses memcmp and the (pal) sprintf_s
 */

PALTEST(c_runtime_wcscat_test1_paltest_wcscat_test1, "c_runtime/wcscat/test1/paltest_wcscat_test1")
{
    WCHAR dest[80];
    WCHAR test[] = {'f','o','o',' ','b','a','r',' ','b','a','z',0};
    WCHAR str1[] = {'f','o','o',' ',0};
    WCHAR str2[] = {'b','a','r',' ',0};
    WCHAR str3[] = {'b','a','z',0};
    WCHAR *ptr;
    char buffer[256];


    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    dest[0] = 0;

    ptr = wcscat(dest, str1);
    if (ptr != dest)
    {
        Fail("ERROR: Expected wcscat to return ptr to %p, got %p", dest, ptr);
    }

    ptr = wcscat(dest, str2);
    if (ptr != dest)
    {
        Fail("ERROR: Expected wcscat to return ptr to %p, got %p", dest, ptr);
    }

    ptr = wcscat(dest, str3);
    if (ptr != dest)
    {
        Fail("ERROR: Expected wcscat to return ptr to %p, got %p", dest, ptr);
    }

    if (memcmp(dest, test, sizeof(test)) != 0)
    {
        sprintf_s(buffer, ARRAY_SIZE(buffer), "%S", dest);
        Fail("ERROR: Expected wcscat to give \"%s\", got \"%s\"\n",
            "foo bar baz", buffer);
    }

    PAL_Terminate();
    return PASS;
}

