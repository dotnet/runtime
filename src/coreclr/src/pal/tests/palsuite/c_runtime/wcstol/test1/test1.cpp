// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test #1 for the wcstol function. Does a simple test with radix 4.
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
    WCHAR *end;
    WCHAR teststr[] = {'1','2','3','4','5',0};
    LONG result = 27;
    LONG l;
        
    if ( 0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    l = wcstol(teststr, &end, 4);

    if (l != result)
    {
        Fail("ERROR: Expected wcstol to return %d, got %d\n", result, l);
    }

    if (end != teststr + 3)
    {
        Fail("ERROR: Expected wcstol to give an end value of %p, got %p\n",
            teststr + 3, end);
    }

    PAL_Terminate();
    return PASS;
}
