// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Test #2 for the wcstol function. Does a simple test with radix
**          10.
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
    LONG result = 12345;
    LONG l;
    
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    l = wcstol(teststr, &end, 10);

    if (l != result)
    {
        Fail("ERROR: Expected wcstol to return %d, got %d\n", result, l);
    }

    if (end != teststr + 5)
    {
        Fail("ERROR: Expected wcstol to give an end value of %p, got %p\n",
            teststr + 5, end);
    }

    PAL_Terminate();
    return PASS;
}
