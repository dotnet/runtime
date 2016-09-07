// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test6.c
**
** Purpose: Test #6 for the wcstol function. Tests strings with octal/hex
**          number specifers
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
    WCHAR test1[] = {'0','x','1','2', 0};
    WCHAR test2[] = {'0','1','2',0};
    WCHAR *end;    
    LONG l;
        
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    l = wcstol(test1, &end, 16);
    if (l != 0x12)
    {
        Fail("ERROR: Expected wcstol to return %d, got %d\n", 0x12, l);
    }
    if (end != test1 + 4)
    {
        Fail("ERROR: Expected wcstol to give an end value of %p, got %p\n",
            test1 + 4, end);
    }
    
    l = wcstol(test1, &end, 10);
    if (l != 0)
    {
        Fail("ERROR: Expected wcstol to return %d, got %d\n", 0, l);
    }
    if (end != test1+1)
    {
        Fail("ERROR: Expected wcstol to give an end value of %p, got %p\n",
            test1+1, end);
    }

    l = wcstol(test2, &end, 8);
    if (l != 10)
    {
        Fail("ERROR: Expected wcstol to return %d, got %d\n", 10, l);
    }
    if (end != test2 + 3)
    {
        Fail("ERROR: Expected wcstol to give an end value of %p, got %p\n",
            test2 + 3, end);
    }

    l = wcstol(test2, &end, 10);
    if (l != 12)
    {
        Fail("ERROR: Expected wcstol to return %d, got %d\n", 12, l);
    }
    
    if (end != test2 + 3)
    {
        Fail("ERROR: Expected wcstol to give an end value of %p, got %p\n",
            test2 + 3, end);
    }

    PAL_Terminate();
    return PASS;
}
