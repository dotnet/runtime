// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test3.c
**
** Purpose: Test #3 for the wcstol function. Tests an invalid string
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
    WCHAR str[] = {'Z',0};
    WCHAR *end;
    LONG l;
    
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    l = wcstol(str, &end, 10);

    if (l != 0)
    {
        Fail("ERROR: Expected wcstol to return %d, got %d\n", 0, l);
    }

    if (end != str)
    {
        Fail("ERROR: Expected wcstol to give an end value of %p, got %p\n",
            str + 3, end);
    }

    PAL_Terminate();
    return PASS;
}
