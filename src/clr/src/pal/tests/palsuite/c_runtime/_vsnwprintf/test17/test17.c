//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:    test17.c
**
** Purpose:   Test #17 for the _vsnwprintf function.
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

    DoDoubleTest(convert("foo %g"), val, convert("foo 2560"),
        convert("foo 2560"));
    DoDoubleTest(convert("foo %lg"), val, convert("foo 2560"),
        convert("foo 2560"));
    DoDoubleTest(convert("foo %hg"), val, convert("foo 2560"),
        convert("foo 2560"));
    DoDoubleTest(convert("foo %Lg"), val, convert("foo 2560"),
        convert("foo 2560"));
    DoDoubleTest(convert("foo %I64g"), val, convert("foo 2560"),
        convert("foo 2560"));
    DoDoubleTest(convert("foo %5g"), val, convert("foo  2560"),
        convert("foo  2560"));
    DoDoubleTest(convert("foo %-5g"), val, convert("foo 2560 "),
        convert("foo 2560 "));
    DoDoubleTest(convert("foo %.1g"), val, convert("foo 3e+003"),
        convert("foo 3e+03"));
    DoDoubleTest(convert("foo %.2g"), val, convert("foo 2.6e+003"),
        convert("foo 2.6e+03"));
    DoDoubleTest(convert("foo %.12g"), val, convert("foo 2560.001"),
        convert("foo 2560.001"));
    DoDoubleTest(convert("foo %06g"), val, convert("foo 002560"),
        convert("foo 002560"));
    DoDoubleTest(convert("foo %#g"), val, convert("foo 2560.00"),
        convert("foo 2560.00"));
    DoDoubleTest(convert("foo %+g"), val, convert("foo +2560"),
        convert("foo +2560"));
    DoDoubleTest(convert("foo % g"), val, convert("foo  2560"),
        convert("foo  2560"));
    DoDoubleTest(convert("foo %+g"), neg, convert("foo -2560"),
        convert("foo -2560"));
    DoDoubleTest(convert("foo % g"), neg, convert("foo -2560"),
        convert("foo -2560"));

    PAL_Terminate();
    return PASS;
}
