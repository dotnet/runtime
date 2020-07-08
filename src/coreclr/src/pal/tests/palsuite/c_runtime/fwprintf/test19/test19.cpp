// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      test19.c
**
** Purpose:     Tests the variable length precision argument.
**              This test is modeled after the sprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fwprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */



int __cdecl main(int argc, char *argv[])
{
    int n = -1;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoArgumentPrecTest(convert("%.*s"), 2, (void*)convert("bar"), "bar", "ba", "ba");
    DoArgumentPrecTest(convert("%.*S"), 2, (void*)"bar", "bar", "ba", "ba");
    DoArgumentPrecTest(convert("foo %.*n"), 3, (void*)&n, "pointer to int", "foo ", 
        "foo ");
    if (n != 4)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %X\n", 
            4, n);
    }

    DoArgumentPrecTest(convert("%.*c"), 0, (void*)'a', "a", "a", "a");
    DoArgumentPrecTest(convert("%.*c"), 4, (void*)'a', "a", "a", "a");
    DoArgumentPrecTest(convert("%.*C"), 0, (void*)'a', "a", "a", "a");
    DoArgumentPrecTest(convert("%.*C"), 4, (void*)'a', "a", "a", "a");
    DoArgumentPrecTest(convert("%.*d"), 1, (void*)42, "42", "42", "42");
    DoArgumentPrecTest(convert("%.*d"), 3, (void*)42, "42", "042", "042");
    DoArgumentPrecTest(convert("%.*i"), 1, (void*)42, "42", "42", "42");
    DoArgumentPrecTest(convert("%.*i"), 3, (void*)42, "42", "042", "042");
    DoArgumentPrecTest(convert("%.*o"), 1, (void*)42, "42", "52", "52");
    DoArgumentPrecTest(convert("%.*o"), 3, (void*)42, "42", "052", "052");
    DoArgumentPrecTest(convert("%.*u"), 1, (void*)42, "42", "42", "42");
    DoArgumentPrecTest(convert("%.*u"), 3, (void*)42, "42", "042", "042");
    DoArgumentPrecTest(convert("%.*x"), 1, (void*)0x42, "0x42", "42", "42");
    DoArgumentPrecTest(convert("%.*x"), 3, (void*)0x42, "0x42", "042", "042");
    DoArgumentPrecTest(convert("%.*X"), 1, (void*)0x42, "0x42", "42", "42");
    DoArgumentPrecTest(convert("%.*X"), 3, (void*)0x42, "0x42", "042", "042");


    DoArgumentPrecDoubleTest(convert("%.*e"), 1, 2.01, "2.0e+000", "2.0e+00");
    DoArgumentPrecDoubleTest(convert("%.*e"), 3, 2.01, "2.010e+000", 
        "2.010e+00");
    DoArgumentPrecDoubleTest(convert("%.*E"), 1, 2.01, "2.0E+000", "2.0E+00");
    DoArgumentPrecDoubleTest(convert("%.*E"), 3, 2.01, "2.010E+000", 
        "2.010E+00");
    DoArgumentPrecDoubleTest(convert("%.*f"), 1, 2.01, "2.0", "2.0");
    DoArgumentPrecDoubleTest(convert("%.*f"), 3, 2.01, "2.010", "2.010");
    DoArgumentPrecDoubleTest(convert("%.*g"), 1, 256.01, "3e+002", "3e+02");
    DoArgumentPrecDoubleTest(convert("%.*g"), 3, 256.01, "256", "256");
    DoArgumentPrecDoubleTest(convert("%.*g"), 4, 256.01, "256", "256");
    DoArgumentPrecDoubleTest(convert("%.*g"), 6, 256.01, "256.01", "256.01");
    DoArgumentPrecDoubleTest(convert("%.*G"), 1, 256.01, "3E+002", "3E+02");
    DoArgumentPrecDoubleTest(convert("%.*G"), 3, 256.01, "256", "256");
    DoArgumentPrecDoubleTest(convert("%.*G"), 4, 256.01, "256", "256");
    DoArgumentPrecDoubleTest(convert("%.*G"), 6, 256.01, "256.01", "256.01");

    PAL_Terminate();

    return PASS;
}
