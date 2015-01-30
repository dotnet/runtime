//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test16.c
**
** Purpose:Tests swprintf with decimal point format doubles 
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swprintf.h"

/*
 * Uses memcmp & wcslen
 */

int __cdecl main(int argc, char *argv[])
{
    double val = 2560.001;
    double neg = -2560.001;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoDoubleTest(convert("foo %f"), val,  convert("foo 2560.001000"),
                 convert("foo 2560.001000"));
    DoDoubleTest(convert("foo %lf"), val,  convert("foo 2560.001000"),
                 convert("foo 2560.001000"));
    DoDoubleTest(convert("foo %hf"), val,  convert("foo 2560.001000"),
                 convert("foo 2560.001000"));
    DoDoubleTest(convert("foo %Lf"), val,  convert("foo 2560.001000"),
                 convert("foo 2560.001000"));
    DoDoubleTest(convert("foo %I64f"), val,  convert("foo 2560.001000"),
                 convert("foo 2560.001000"));
    DoDoubleTest(convert("foo %12f"), val,  convert("foo  2560.001000"),
                 convert("foo  2560.001000"));
    DoDoubleTest(convert("foo %-12f"), val,  convert("foo 2560.001000 "),
                 convert("foo 2560.001000 "));
    DoDoubleTest(convert("foo %.1f"), val,  convert("foo 2560.0"),
                 convert("foo 2560.0"));
    DoDoubleTest(convert("foo %.8f"), val,  convert("foo 2560.00100000"),
                 convert("foo 2560.00100000"));
    DoDoubleTest(convert("foo %012f"), val,  convert("foo 02560.001000"),
                 convert("foo 02560.001000"));
    DoDoubleTest(convert("foo %#f"), val,  convert("foo 2560.001000"),
                 convert("foo 2560.001000"));
    DoDoubleTest(convert("foo %+f"), val,  convert("foo +2560.001000"),
                 convert("foo +2560.001000"));
    DoDoubleTest(convert("foo % f"), val,  convert("foo  2560.001000"),
                 convert("foo  2560.001000"));
    DoDoubleTest(convert("foo %+f"), neg,  convert("foo -2560.001000"),
                 convert("foo -2560.001000"));
    DoDoubleTest(convert("foo % f"), neg,  convert("foo -2560.001000"),
                 convert("foo -2560.001000"));

    PAL_Terminate();
    return PASS;
}
