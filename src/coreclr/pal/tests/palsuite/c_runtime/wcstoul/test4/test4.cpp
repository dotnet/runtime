// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test4.c
**
** Purpose: Test #4 for the wcstoul function
**
**
**==========================================================================*/
#include <palsuite.h>

/*
 * Notes: wcstoul should depend on the current locale's LC_NUMERIC category, 
 * this is not currently tested.
 */

PALTEST(c_runtime_wcstoul_test4_paltest_wcstoul_test4, "c_runtime/wcstoul/test4/paltest_wcstoul_test4")
{
    WCHAR maxstr[] = {'4','2','9','4','9','6','7','2','9','5',0};
    ULONG max =    4294967295ul;
    WCHAR *end;
    ULONG l;

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    errno = 0;

    l = wcstoul(maxstr, &end, 10);
    if (l != max)
    {
        Fail("ERROR: Expected wcstoul to return %u, got %u\n", max, l);
    }
    if (end != maxstr + 10)
    {
        Fail("ERROR: Expected wcstoul to give an end value of %p, got %p\n",
            maxstr + 10, end);
    }
    if (errno != 0)
    {
        Fail("ERROR: wcstoul set errno to non-zero (%d)\n", errno);
    }

    PAL_Terminate();
    return PASS;
}
