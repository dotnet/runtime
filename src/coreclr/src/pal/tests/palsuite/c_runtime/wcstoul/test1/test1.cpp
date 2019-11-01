// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test #1 for the wcstoul function
**
**
**==========================================================================*/
#include <palsuite.h>

/*
 * Notes: wcstoul should depend on the current locale's LC_NUMERIC category, 
 * this is not currently tested.
 */


int __cdecl main(int argc, char *argv[])
{
    WCHAR teststr[] = {'1','2','3','4','5',0};
    WCHAR *end;
    ULONG result = 27;
    ULONG l;
        
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    l = wcstoul(teststr, &end, 4);

    if (l != result)
    {
        Fail("ERROR: Expected wcstoul to return %u, got %u\n", result, l);
    }

    if (end != teststr + 3)
    {
        Fail("ERROR: Expected wcstoul to give an end value of %p, got %p\n",
            teststr + 3, end);
    }
    
    PAL_Terminate();
    return PASS;
}
