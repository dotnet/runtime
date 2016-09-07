// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test4.c
**
** Purpose: Test #4 for the wcstol function. Tests the limits of the
**          conversions.
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
    WCHAR maxstr[] = {'2','1','4','7','4','8','3','6','4','7',0};
    LONG max = 2147483647;
    WCHAR minstr[] = {'-','2','1','4','7','4','8','3','6','4','8',0};
    LONG min = 0x80000000; /* putting -2147483648 gives a warning */
    WCHAR *end;
    LONG l;
    
    if ( 0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    errno = 0;

    l = wcstol(maxstr, &end, 10);

    if (l != max)
    {
        Fail("ERROR: Expected wcstol to return %d, got %d\n", max, l);
    }
    if (end != maxstr + 10)
    {
        Fail("ERROR: Expected wcstol to give an end value of %p, got %p\n",
            maxstr + 10, end);
    }
    if (errno != 0)
    {
        Fail("ERROR: wcstol set errno to non-zero (%d)\n", errno);
    }


    l = wcstol(minstr, &end, 10);

    if (l != min)
    {
        Fail("ERROR: Expected wcstol to return %d, got %d\n", min, l);
    }
    if (end != minstr + 11)
    {
        Fail("ERROR: Expected wcstol to give an end value of %p, got %p\n",
            minstr + 11, end);
    }
    if (errno != 0)
    {
        Fail("ERROR: wcstol set errno to non-zero (%d)\n", errno);
    }

    PAL_Terminate();
    return PASS;
}
