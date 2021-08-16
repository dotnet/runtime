// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test5.c
**
** Purpose: Test #5 for the vfprintf function. Tests the count specifier (%n).
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../vfprintf.h"




PALTEST(c_runtime_vfprintf_test5_paltest_vfprintf_test5, "c_runtime/vfprintf/test5/paltest_vfprintf_test5")
{    
    char *longStr = 
        "really-long-string-that-just-keeps-going-on-and-on-and-on.."
        "..................useless-filler.................................."
        "..................useless-filler.................................."
        "..................useless-filler.................................."
        "%n bar";
    char *longResult = 
        "really-long-string-that-just-keeps-going-on-and-on-and-on.."
        "..................useless-filler.................................."
        "..................useless-filler.................................."
        "..................useless-filler.................................."
        " bar";

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoCountTest("foo %n bar", 4, "foo  bar");
    DoCountTest(longStr, 257, longResult);
    DoCountTest("fo%n bar", 2, "fo bar");
    DoCountTest("%n ", 0, " ");
    DoCountTest("foo %#n bar", 4, "foo  bar");
    DoCountTest("foo % n bar", 4, "foo  bar");
    DoCountTest("foo %+n bar", 4, "foo  bar");
    DoCountTest("foo %-n bar", 4, "foo  bar");
    DoCountTest("foo %0n bar", 4, "foo  bar");
    DoShortCountTest("foo %hn bar", 4, "foo  bar");
    DoCountTest("foo %ln bar", 4, "foo  bar");
    DoCountTest("foo %Ln bar", 4, "foo  bar");
    DoCountTest("foo %I64n bar", 4, "foo  bar");
    DoCountTest("foo %20.3n bar", 4, "foo  bar");

    PAL_Terminate();
   
    return PASS;
}
