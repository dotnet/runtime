//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:    test15.c
**
** Purpose:   Test #15 for the vswprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../vswprintf.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */


int __cdecl main(int argc, char *argv[])
{
    double val = 256.0;
    double neg = -256.0;
    
    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);

    DoDoubleTest(convert("foo %E"),    val, convert("foo 2.560000E+002"),
        convert("foo 2.560000E+02"));
    DoDoubleTest(convert("foo %lE"),   val, convert("foo 2.560000E+002"),
        convert("foo 2.560000E+02"));
    DoDoubleTest(convert("foo %hE"),   val, convert("foo 2.560000E+002"),
        convert("foo 2.560000E+02"));
    DoDoubleTest(convert("foo %LE"),   val, convert("foo 2.560000E+002"),
        convert("foo 2.560000E+02"));
    DoDoubleTest(convert("foo %I64E"), val, convert("foo 2.560000E+002"),
        convert("foo 2.560000E+02"));
    DoDoubleTest(convert("foo %14E"),  val, convert("foo  2.560000E+002"),
        convert("foo   2.560000E+02"));
    DoDoubleTest(convert("foo %-14E"), val, convert("foo 2.560000E+002 "),
        convert("foo 2.560000E+02 "));
    DoDoubleTest(convert("foo %.1E"),  val, convert("foo 2.6E+002"),
        convert("foo 2.6E+02"));
    DoDoubleTest(convert("foo %.8E"),  val, convert("foo 2.56000000E+002"),
        convert("foo 2.56000000E+02"));
    DoDoubleTest(convert("foo %014E"), val, convert("foo 02.560000E+002"),
        convert("foo 002.560000E+02"));
    DoDoubleTest(convert("foo %#E"),   val, convert("foo 2.560000E+002"),
        convert("foo 2.560000E+02"));
    DoDoubleTest(convert("foo %+E"),   val, convert("foo +2.560000E+002"),
        convert("foo +2.560000E+02"));
    DoDoubleTest(convert("foo % E"),   val, convert("foo  2.560000E+002"),
        convert("foo  2.560000E+02"));
    DoDoubleTest(convert("foo %+E"),   neg, convert("foo -2.560000E+002"), 
        convert("foo -2.560000E+02"));
    DoDoubleTest(convert("foo % E"),   neg, convert("foo -2.560000E+002"), 
        convert("foo -2.560000E+002"));

    PAL_Terminate();
    return PASS;
}
