// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test19.c
**
** Purpose: Tests swprintf with argument specified precision 
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swprintf.h"

/*
 * Uses memcmp & wcslen
 */

PALTEST(c_runtime_swprintf_test19_paltest_swprintf_test19, "c_runtime/swprintf/test19/paltest_swprintf_test19")
{
    int n = -1;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    DoArgumentPrecTest(convert("%.*s"), 2, (void*)convert("bar"), "bar",
                       convert("ba"), convert("ba"));
    DoArgumentPrecTest(convert("%.*S"), 2, (void*)"bar", "bar", convert("ba"),
                       convert("ba"));
    DoArgumentPrecTest(convert("%.*c"), 0, (void*)'a', "a", convert("a"),
                       convert("a"));
    DoArgumentPrecTest(convert("%.*c"), 4, (void*)'a', "a", convert("a"),
                       convert("a"));
    DoArgumentPrecTest(convert("%.*C"), 0, (void*)'a', "a", convert("a"),
                       convert("a"));
    DoArgumentPrecTest(convert("%.*C"), 4, (void*)'a', "a", convert("a"),
                       convert("a"));
    DoArgumentPrecTest(convert("%.*d"), 1, (void*)42, "42", convert("42"),
                       convert("42"));
    DoArgumentPrecTest(convert("%.*d"), 3, (void*)42, "42", convert("042"),
                       convert("042"));
    DoArgumentPrecTest(convert("%.*i"), 1, (void*)42, "42", convert("42"),
                       convert("42"));
    DoArgumentPrecTest(convert("%.*i"), 3, (void*)42, "42", convert("042"),
                       convert("042"));
    DoArgumentPrecTest(convert("%.*o"), 1, (void*)42, "42", convert("52"),
                       convert("52"));
    DoArgumentPrecTest(convert("%.*o"), 3, (void*)42, "42", convert("052"),
                       convert("052"));
    DoArgumentPrecTest(convert("%.*u"), 1, (void*)42, "42", convert("42"),
                       convert("42"));
    DoArgumentPrecTest(convert("%.*u"), 3, (void*)42, "42", convert("042"),
                       convert("042"));
    DoArgumentPrecTest(convert("%.*x"), 1, (void*)0x42, "0x42", convert("42"),
                       convert("42"));
    DoArgumentPrecTest(convert("%.*x"), 3, (void*)0x42, "0x42", convert("042"),
                       convert("042"));
    DoArgumentPrecTest(convert("%.*X"), 1, (void*)0x42, "0x42", convert("42"),
                       convert("42"));
    DoArgumentPrecTest(convert("%.*X"), 3, (void*)0x42, "0x42", convert("042"),
                       convert("042"));


    DoArgumentPrecDoubleTest(convert("%.*e"), 1, 2.01, convert("2.0e+000"),
                             convert("2.0e+000"));
    DoArgumentPrecDoubleTest(convert("%.*e"), 3, 2.01, convert("2.010e+000"),
                             convert("2.010e+000"));
    DoArgumentPrecDoubleTest(convert("%.*E"), 1, 2.01, convert("2.0E+000"),
                             convert("2.0E+000"));
    DoArgumentPrecDoubleTest(convert("%.*E"), 3, 2.01, convert("2.010E+000"),
                             convert("2.010E+000"));
    DoArgumentPrecDoubleTest(convert("%.*f"), 1, 2.01, convert("2.0"),
                             convert("2.0"));
    DoArgumentPrecDoubleTest(convert("%.*f"), 3, 2.01, convert("2.010"),
                             convert("2.010"));
    DoArgumentPrecDoubleTest(convert("%.*g"), 1, 256.01, convert("3e+002"),
                             convert("3e+002"));
    DoArgumentPrecDoubleTest(convert("%.*g"), 3, 256.01, convert("256"),
                             convert("256"));
    DoArgumentPrecDoubleTest(convert("%.*g"), 4, 256.01, convert("256"),
                             convert("256"));
    DoArgumentPrecDoubleTest(convert("%.*g"), 6, 256.01, convert("256.01"),
                             convert("256.01"));
    DoArgumentPrecDoubleTest(convert("%.*G"), 1, 256.01, convert("3E+002"),
                             convert("3E+002"));
    DoArgumentPrecDoubleTest(convert("%.*G"), 3, 256.01, convert("256"),
                             convert("256"));
    DoArgumentPrecDoubleTest(convert("%.*G"), 4, 256.01, convert("256"),
                             convert("256"));
    DoArgumentPrecDoubleTest(convert("%.*G"), 6, 256.01, convert("256.01"),
                             convert("256.01"));
    
    PAL_Terminate();
    return PASS;
}
