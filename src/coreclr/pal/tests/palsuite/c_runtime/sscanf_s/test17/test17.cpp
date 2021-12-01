// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test17.c
**
** Purpose: Tests sscanf_s with floats (compact notation, uppercase)
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"

PALTEST(c_runtime_sscanf_s_test17_paltest_sscanf_test17, "c_runtime/sscanf_s/test17/paltest_sscanf_test17")
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoFloatTest("123.0", "%G", 123.0f);
    DoFloatTest("123.0", "%2G", 12.0f);
    DoFloatTest("10E1", "%G", 100.0f);
    DoFloatTest("-12.01e-2", "%G", -0.1201f);
    DoFloatTest("+12.01e-2", "%G", 0.1201f);
    DoFloatTest("-12.01e+2", "%G", -1201.0f);
    DoFloatTest("+12.01e+2", "%G", 1201.0f);
    DoFloatTest("1234567890.0123456789G", "%G", 1234567936);
    
    PAL_Terminate();
    return PASS;
}
