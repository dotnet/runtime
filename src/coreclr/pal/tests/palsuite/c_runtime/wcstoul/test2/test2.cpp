// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Test #2 for the wcstoul function
**
**
**==========================================================================*/
#include <palsuite.h>

/*
 * Notes: wcstoul should depend on the current locale's LC_NUMERIC category, 
 * this is not currently tested.
 */


PALTEST(c_runtime_wcstoul_test2_paltest_wcstoul_test2, "c_runtime/wcstoul/test2/paltest_wcstoul_test2")
{
    WCHAR teststr[] = {'1','2','3','4','5',0};
    WCHAR *end;    
    ULONG result = 12345;
    ULONG l;
    
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    l = wcstoul((char16_t*)teststr, &end, 10);

    if (l != result)
    {
        Fail("ERROR: Expected wcstoul to return %u, got %u\n", result, l);
    }

    if (end != teststr + 5)
    {
        Fail("ERROR: Expected wcstoul to give an end value of %p, got %p\n",
            teststr + 5, end);
    }

    PAL_Terminate();
    return PASS;
}
