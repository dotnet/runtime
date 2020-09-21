// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test13.c
**
** Purpose: Tests sscanf_s with floats (decimal notation)
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"

PALTEST(c_runtime_sscanf_s_test13_paltest_sscanf_test13, "c_runtime/sscanf_s/test13/paltest_sscanf_test13")
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoFloatTest("123.0", "%f", 123.0f);
    DoFloatTest("123.0", "%2f", 12.0f);
    DoFloatTest("10E1", "%f", 100.0f);
    DoFloatTest("-12.01e-2", "%f", -0.1201f);
    DoFloatTest("+12.01e-2", "%f", 0.1201f);
    DoFloatTest("-12.01e+2", "%f", -1201.0f);
    DoFloatTest("+12.01e+2", "%f", 1201.0f);
    DoFloatTest("1234567890.0123456789f", "%f", 1234567936);
    
    PAL_Terminate();
    return PASS;
}
