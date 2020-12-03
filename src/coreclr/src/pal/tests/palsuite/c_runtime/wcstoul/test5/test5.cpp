// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test5.c
**
** Purpose: Test #5 for the wcstoul function
**
**
**==========================================================================*/
#include <palsuite.h>

/*
 * Notes: wcstoul should depend on the current locale's LC_NUMERIC category, 
 * this is not currently tested.
 */


PALTEST(c_runtime_wcstoul_test5_paltest_wcstoul_test5, "c_runtime/wcstoul/test5/paltest_wcstoul_test5")
{
    WCHAR overstr[] = {'4','2','9','4','9','6','7','2','9','6',0};
    WCHAR understr[] = {'-','1',0}; 
    WCHAR *end;
    ULONG l;
    
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    errno = 0;
    l = wcstoul(overstr, &end, 10);

    if (l != _UI32_MAX)
    {
        Fail("ERROR: Expected wcstoul to return %u, got %u\n", _UI32_MAX, l);
    }
    if (end != overstr + 10)
    {
        Fail("ERROR: Expected wcstoul to give an end value of %p, got %p\n",
            overstr + 10, end);
    }
    if (errno != ERANGE)
    {
        Fail("ERROR: wcstoul did not set errno to ERANGE (%d)\n", errno);
    }

    errno = 0;
    l = wcstoul(understr, &end, 10);

    if (l != _UI32_MAX)
    {
        Fail("ERROR: Expected wcstoul to return %u, got %u\n", _UI32_MAX, l);
    }
    if (end != understr + 2)
    {
        Fail("ERROR: Expected wcstoul to give an end value of %p, got %p\n",
            understr + 2, end);
    }
    if (errno != 0)
    {
        Fail("ERROR: wcstoul set errno to non-zero (%d)\n", errno);
    }
   
    PAL_Terminate();
    return PASS;
}
