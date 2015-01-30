//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:      test16.c
**
** Purpose:     Tests the decimal notation double specifier (%f).
**              This test is modeled after the sprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fwprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

int __cdecl main(int argc, char *argv[])
{
    double val = 2560.001;
    double neg = -2560.001;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoDoubleTest(convert("foo %f"), val,  "foo 2560.001000", 
        "foo 2560.001000");
    DoDoubleTest(convert("foo %lf"), val,  "foo 2560.001000",
        "foo 2560.001000");
    DoDoubleTest(convert("foo %hf"), val,  "foo 2560.001000", 
        "foo 2560.001000");
    DoDoubleTest(convert("foo %Lf"), val,  "foo 2560.001000", 
        "foo 2560.001000");
    DoDoubleTest(convert("foo %I64f"), val,  "foo 2560.001000", 
        "foo 2560.001000");
    DoDoubleTest(convert("foo %12f"), val,  "foo  2560.001000", 
        "foo  2560.001000");
    DoDoubleTest(convert("foo %-12f"), val,  "foo 2560.001000 ", 
        "foo 2560.001000 ");
    DoDoubleTest(convert("foo %.1f"), val,  "foo 2560.0", 
        "foo 2560.0");
    DoDoubleTest(convert("foo %.8f"), val,  "foo 2560.00100000", 
        "foo 2560.00100000");
    DoDoubleTest(convert("foo %012f"), val,  "foo 02560.001000", 
        "foo 02560.001000");
    DoDoubleTest(convert("foo %#f"), val,  "foo 2560.001000", 
        "foo 2560.001000");
    DoDoubleTest(convert("foo %+f"), val,  "foo +2560.001000", 
        "foo +2560.001000");
    DoDoubleTest(convert("foo % f"), val,  "foo  2560.001000", 
        "foo  2560.001000");
    DoDoubleTest(convert("foo %+f"), neg,  "foo -2560.001000", 
        "foo -2560.001000");
    DoDoubleTest(convert("foo % f"), neg,  "foo -2560.001000", 
        "foo -2560.001000");

    PAL_Terminate();
    return PASS;
}
