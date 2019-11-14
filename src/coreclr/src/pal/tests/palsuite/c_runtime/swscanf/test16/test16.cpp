// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test16.c
**
** Purpose: Tests swscanf with floats (compact notation, lowercase)
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swscanf.h"

int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoFloatTest(convert("123.0"), convert("%g"), 123.0f);
    DoFloatTest(convert("123.0"), convert("%2g"), 12.0f);
    DoFloatTest(convert("10E1"), convert("%g"), 100.0f);
    DoFloatTest(convert("-12.01e-2"), convert("%g"), -0.1201f);
    DoFloatTest(convert("+12.01e-2"), convert("%g"), 0.1201f);
    DoFloatTest(convert("-12.01e+2"), convert("%g"), -1201.0f);
    DoFloatTest(convert("+12.01e+2"), convert("%g"), 1201.0f);
    DoFloatTest(convert("1234567890.0123456789f"), convert("%g"), 1234567936);

    PAL_Terminate();
    return PASS;
}
