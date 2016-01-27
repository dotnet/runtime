// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:    test5.c
**
** Purpose:   Test #5 for the _vsnprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnprintf.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

static void DoTest(char *formatstr, int param, char *checkstr)
{
    char buf[256] = { 0 };
    int n = -1;
    
    Testvsnprintf(buf, 256, formatstr, &n);

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %X\n",
             param, n);
    }
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n", checkstr, buf);
    }    
}

static void DoShortTest(char *formatstr, int param, char *checkstr)
{
    char buf[256] = { 0 };
    short int n = -1;
    
    Testvsnprintf(buf, 256, formatstr, &n);

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %X\n",
             param, n);
    }
    if (memcmp(buf, checkstr, strlen(buf) + 1) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n", checkstr, buf);
    }    
}

int __cdecl main(int argc, char *argv[])
{    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoTest("foo %n bar", 4, "foo  bar");
    DoTest("foo %#n bar", 4, "foo  bar");
    DoTest("foo % n bar", 4, "foo  bar");
    DoTest("foo %+n bar", 4, "foo  bar");
    DoTest("foo %-n bar", 4, "foo  bar");
    DoTest("foo %0n bar", 4, "foo  bar");
    DoShortTest("foo %hn bar", 4, "foo  bar");
    DoTest("foo %ln bar", 4, "foo  bar");
    DoTest("foo %Ln bar", 4, "foo  bar");
    DoTest("foo %I64n bar", 4, "foo  bar");
    DoTest("foo %20.3n bar", 4, "foo  bar");

    PAL_Terminate();
    return PASS;
}
