//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:    test18.c
**
** Purpose:   Test #18 for the vsprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../vsprintf.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

int __cdecl main(int argc, char *argv[])
{
    double val = 2560.001;
    double neg = -2560.001;

   if (PAL_Initialize(argc, argv) != 0)
   {
        return(FAIL);
   }

    DoDoubleTest("foo %G", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %lG", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %hG", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %LG", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %I64G", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %5G", val,  "foo  2560", "foo  2560");
    DoDoubleTest("foo %-5G", val,  "foo 2560 ", "foo 2560 ");
    DoDoubleTest("foo %.1G", val,  "foo 3E+003", "foo 3E+03");
    DoDoubleTest("foo %.2G", val,  "foo 2.6E+003", "foo 2.6E+03");
    DoDoubleTest("foo %.12G", val,  "foo 2560.001", "foo 2560.001");
    DoDoubleTest("foo %06G", val,  "foo 002560", "foo 002560");
    DoDoubleTest("foo %#G", val,  "foo 2560.00", "foo 2560.00");
    DoDoubleTest("foo %+G", val,  "foo +2560", "foo +2560");
    DoDoubleTest("foo % G", val,  "foo  2560", "foo  2560");
    DoDoubleTest("foo %+G", neg,  "foo -2560", "foo -2560");
    DoDoubleTest("foo % G", neg,  "foo -2560", "foo -2560");

    PAL_Terminate();
    return PASS;
}
