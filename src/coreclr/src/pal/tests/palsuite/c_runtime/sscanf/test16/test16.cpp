// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test16.c
**
** Purpose:Tests sscanf with floats (compact notation, lowercase) 
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf.h"

int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoFloatTest("123.0", "%g", 123.0f);
    DoFloatTest("123.0", "%2g", 12.0f);
    DoFloatTest("10E1", "%g", 100.0f);
    DoFloatTest("-12.01e-2", "%g", -0.1201f);
    DoFloatTest("+12.01e-2", "%g", 0.1201f);
    DoFloatTest("-12.01e+2", "%g", -1201.0f);
    DoFloatTest("+12.01e+2", "%g", 1201.0f);
    DoFloatTest("1234567890.0123456789g", "%g", 1234567936);
    
    PAL_Terminate();
    return PASS;
}
