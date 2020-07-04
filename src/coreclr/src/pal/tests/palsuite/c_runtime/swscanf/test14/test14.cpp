// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test14.c
**
** Purpose: Tests swscanf with floats (exponential notation, lowercase)
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

    DoFloatTest(convert("123.0"), convert("%e"), 123.0f);
    DoFloatTest(convert("123.0"), convert("%2e"), 12.0f);
    DoFloatTest(convert("10E1"), convert("%e"), 100.0f);
    DoFloatTest(convert("-12.01e-2"), convert("%e"), -0.1201f);
    DoFloatTest(convert("+12.01e-2"), convert("%e"), 0.1201f);
    DoFloatTest(convert("-12.01e+2"), convert("%e"), -1201.0f);
    DoFloatTest(convert("+12.01e+2"), convert("%e"), 1201.0f);
    DoFloatTest(convert("1234567890.0123456789f"), convert("%e"), 1234567936);

    PAL_Terminate();
    return PASS;
}
