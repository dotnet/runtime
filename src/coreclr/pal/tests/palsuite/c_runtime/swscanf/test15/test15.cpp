// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test15.c
**
** Purpose: Tests swscanf with floats (exponential notation, uppercase)
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swscanf.h"

PALTEST(c_runtime_swscanf_test15_paltest_swscanf_test15, "c_runtime/swscanf/test15/paltest_swscanf_test15")
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoFloatTest(convert("123.0"), convert("%E"), 123.0f);
    DoFloatTest(convert("123.0"), convert("%2E"), 12.0f);
    DoFloatTest(convert("10E1"), convert("%E"), 100.0f);
    DoFloatTest(convert("-12.01e-2"), convert("%E"), -0.1201f);
    DoFloatTest(convert("+12.01e-2"), convert("%E"), 0.1201f);
    DoFloatTest(convert("-12.01e+2"), convert("%E"), -1201.0f);
    DoFloatTest(convert("+12.01e+2"), convert("%E"), 1201.0f);
    DoFloatTest(convert("1234567890.0123456789f"), convert("%E"), 1234567936);

    PAL_Terminate();
    return PASS;
}
