// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test15.c
**
** Purpose: Tests sscanf_s with floats (exponential notation, uppercase
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"

PALTEST(c_runtime_sscanf_s_test15_paltest_sscanf_test15, "c_runtime/sscanf_s/test15/paltest_sscanf_test15")
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoFloatTest("123.0", "%E", 123.0f);
    DoFloatTest("123.0", "%2E", 12.0f);
    DoFloatTest("10E1", "%E", 100.0f);
    DoFloatTest("-12.01e-2", "%E", -0.1201f);
    DoFloatTest("+12.01e-2", "%E", 0.1201f);
    DoFloatTest("-12.01e+2", "%E", -1201.0f);
    DoFloatTest("+12.01e+2", "%E", 1201.0f);
    
    PAL_Terminate();
    return PASS;
}
