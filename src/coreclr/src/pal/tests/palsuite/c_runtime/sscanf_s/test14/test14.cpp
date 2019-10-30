// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

int __cdecl main(int argc, char *argv[])
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
