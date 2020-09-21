// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test14.c
**
** Purpose: Tests sscanf_s with floats (exponential notation, lowercase)
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"

PALTEST(c_runtime_sscanf_s_test14_paltest_sscanf_test14, "c_runtime/sscanf_s/test14/paltest_sscanf_test14")
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoFloatTest("123.0", "%e", 123.0f);
    DoFloatTest("123.0", "%2e", 12.0f);
    DoFloatTest("10E1", "%e", 100.0f);
    DoFloatTest("-12.01e-2", "%e", -0.1201f);
    DoFloatTest("+12.01e-2", "%e", 0.1201f);
    DoFloatTest("-12.01e+2", "%e", -1201.0f);
    DoFloatTest("+12.01e+2", "%e", 1201.0f);
    
    PAL_Terminate();
    return PASS;
}
