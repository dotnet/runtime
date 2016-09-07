// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test5.c
**
** Purpose: Test #5 for the wcstol function. Tests over and under flowing.
**
**
**==========================================================================*/



#include <palsuite.h>


/*
 * Notes: wcstol should depend on the current locale's LC_NUMERIC category, 
 * this is not currently tested.
 */


int __cdecl main(int argc, char *argv[])
{
    WCHAR overstr[] = {'2','1','4','7','4','8','3','6','4','8',0};
    WCHAR understr[] = {'-','2','1','4','7','4','8','3','6','4','9',0};
    WCHAR *end;
    LONG l;
        
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    errno = 0;

    l = wcstol(overstr, &end, 10);

    if (l != LONG_MAX)
    {
        Fail("ERROR: Expected wcstol to return %u, got %u\n", LONG_MAX, l);
    }
    if (end != overstr + 10)
    {
        Fail("ERROR: Expected wcstol to give an end value of %p, got %p\n",
            overstr + 10, end);
    }
    if (errno != ERANGE)
    {
        Fail("ERROR: wcstol did not set errno to ERANGE (%d)\n", errno);
    }


    errno = 0;
    l = wcstol(understr, &end, 10);

    if (l != LONG_MIN)
    {
        Fail("ERROR: Expected wcstol to return %u, got %u\n", LONG_MIN, l);
    }
    if (end != understr + 11)
    {
        Fail("ERROR: Expected wcstol to give an end value of %p, got %p\n",
            understr + 11, end);
    }
    if (errno != ERANGE)
    {
        Fail("ERROR: wcstol did not set errno to ERANGE (%d)\n", errno);
    }

    PAL_Terminate();
    return PASS;
}
