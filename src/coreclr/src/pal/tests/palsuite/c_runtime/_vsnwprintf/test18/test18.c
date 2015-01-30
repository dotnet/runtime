//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:    test18.c
**
** Purpose:   Test #18 for the _vsnwprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnwprintf.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */

int __cdecl main(int argc, char *argv[])
{
    double val = 2560.001;
    double neg = -2560.001;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoDoubleTest(convert("foo %G"), val, convert("foo 2560"),
        convert("foo 2560"));
    DoDoubleTest(convert("foo %lG"), val, convert("foo 2560"),
        convert("foo 2560"));
    DoDoubleTest(convert("foo %hG"), val, convert("foo 2560"),
        convert("foo 2560"));
    DoDoubleTest(convert("foo %LG"), val, convert("foo 2560"),
        convert("foo 2560"));
    DoDoubleTest(convert("foo %I64G"), val, convert("foo 2560"),
        convert("foo 2560"));
    DoDoubleTest(convert("foo %5G"), val, convert("foo  2560"),
        convert("foo  2560"));
    DoDoubleTest(convert("foo %-5G"), val, convert("foo 2560 "),
        convert("foo 2560 "));
    DoDoubleTest(convert("foo %.1G"), val, convert("foo 3E+003"),
        convert("foo 3E+03"));
    DoDoubleTest(convert("foo %.2G"), val, convert("foo 2.6E+003"),
        convert("foo 2.6E+03"));
    DoDoubleTest(convert("foo %.12G"), val, convert("foo 2560.001"),
        convert("foo 2560.001"));
    DoDoubleTest(convert("foo %06G"), val, convert("foo 002560"),
        convert("foo 002560"));
    DoDoubleTest(convert("foo %#G"), val, convert("foo 2560.00"),
        convert("foo 2560.00"));
    DoDoubleTest(convert("foo %+G"), val, convert("foo +2560"),
        convert("foo +2560"));
    DoDoubleTest(convert("foo % G"), val, convert("foo  2560"),
        convert("foo  2560"));
    DoDoubleTest(convert("foo %+G"), neg, convert("foo -2560"),
        convert("foo -2560"));
    DoDoubleTest(convert("foo % G"), neg, convert("foo -2560"),
        convert("foo -2560"));

    PAL_Terminate();
    return PASS;
}
