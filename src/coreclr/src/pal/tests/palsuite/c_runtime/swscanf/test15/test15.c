//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main(int argc, char *argv[])
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
