// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test17.c
**
** Purpose: Tests swscanf with floats (compact notation, uppercase)
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swscanf.h"

PALTEST(c_runtime_swscanf_test17_paltest_swscanf_test17, "c_runtime/swscanf/test17/paltest_swscanf_test17")
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoFloatTest(convert("123.0"), convert("%G"), 123.0f);
    DoFloatTest(convert("123.0"), convert("%2G"), 12.0f);
    DoFloatTest(convert("10E1"), convert("%G"), 100.0f);
    DoFloatTest(convert("-12.01e-2"), convert("%G"), -0.1201f);
    DoFloatTest(convert("+12.01e-2"), convert("%G"), 0.1201f);
    DoFloatTest(convert("-12.01e+2"), convert("%G"), -1201.0f);
    DoFloatTest(convert("+12.01e+2"), convert("%G"), 1201.0f);
    DoFloatTest(convert("1234567890.0123456789f"), convert("%G"), 1234567936);

    PAL_Terminate();
    return PASS;
}
