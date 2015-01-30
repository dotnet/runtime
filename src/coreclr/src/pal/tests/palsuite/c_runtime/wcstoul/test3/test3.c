//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test3.c
**
** Purpose: Test #3 for the wcstoul function
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
    WCHAR str[] = {'Z',0};
    WCHAR *end;
    ULONG l;
    
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    l = wcstoul(str, &end, 10);

    if (l != 0)
    {
        Fail("ERROR: Expected wcstoul to return %u, got %u\n", 0, l);
    }

    if (end != str)
    {
        Fail("ERROR: Expected wcstoul to give an end value of %p, got %p\n",
            str, end);
    }

    PAL_Terminate();
    return PASS;
}

