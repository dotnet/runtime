//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test17.c
**
** Purpose: Tests sscanf with floats (compact notation, uppercase)
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
