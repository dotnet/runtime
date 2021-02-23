// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test2.c
**
** Purpose:   Test #2 for the _vsnprintf function.
**
**
**===================================================================*/ 
 
#include <palsuite.h>
#include "../_vsnprintf_s.h"
/*
 * Notes: memcmp is used, as is strlen.
 */

PALTEST(c_runtime__vsnprintf_s_test2_paltest_vsnprintf_test2, "c_runtime/_vsnprintf_s/test2/paltest_vsnprintf_test2")
{
    WCHAR szwStr[] = {'b','a','r','\0'};

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoStrTest("foo %s", "bar", "foo bar");  
    DoStrTest("foo %hs", "bar", "foo bar");
    DoWStrTest("foo %ls", szwStr, "foo bar");
    DoWStrTest("foo %ws", szwStr, "foo bar");
    DoStrTest("foo %Ls", "bar", "foo bar");
    DoStrTest("foo %I64s", "bar", "foo bar");
    DoStrTest("foo %5s", "bar", "foo   bar");
    DoStrTest("foo %.2s", "bar", "foo ba");
    DoStrTest("foo %5.2s", "bar", "foo    ba");
    DoStrTest("foo %-5s", "bar", "foo bar  ");
    DoStrTest("foo %05s", "bar", "foo 00bar");   
    DoStrTest("foo %s", NULL, "foo (null)");
    DoStrTest("foo %hs", NULL, "foo (null)");
    DoWStrTest("foo %ls", NULL, "foo (null)");
    DoWStrTest("foo %ws", NULL, "foo (null)");
    DoStrTest("foo %Ls", NULL, "foo (null)");
    DoStrTest("foo %I64s", NULL, "foo (null)");

    PAL_Terminate();
    return PASS;
}

