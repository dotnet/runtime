// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:    test5.c
**
** Purpose:   Test #5 for the vswprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../vswprintf.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */

static void DoTest(WCHAR *formatstr, int param, WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };
    int n = -1;

    testvswp(buf, formatstr, &n);

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %d\n",
              param, n);
    }

    if (memcmp(buf, checkstr, wcslen(buf)*2 + 2) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n", 
            convertC(checkstr), convertC(buf));
    }    
}

static void DoShortTest(WCHAR *formatstr, int param, WCHAR *checkstr)
{
    WCHAR buf[256] = { 0 };
    short int n = -1;

    testvswp(buf, formatstr, &n);

    if (n != param)
    {
        Fail("ERROR: Expected count parameter to resolve to %d, got %d\n",
              param, n);
    }

    if (memcmp(buf, checkstr, wcslen(buf)*2 + 2) != 0)
    {
        Fail("ERROR: Expected \"%s\" got \"%s\".\n", 
            convertC(checkstr), convertC(buf));
    }    
}

int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);

    DoTest(convert("foo %n bar"), 4, convert("foo  bar"));
    DoTest(convert("foo %#n bar"), 4, convert("foo  bar"));
    DoTest(convert("foo % n bar"), 4, convert("foo  bar"));
    DoTest(convert("foo %+n bar"), 4, convert("foo  bar"));
    DoTest(convert("foo %-n bar"), 4, convert("foo  bar"));
    DoTest(convert("foo %0n bar"), 4, convert("foo  bar"));
    DoShortTest(convert("foo %hn bar"), 4, convert("foo  bar"));
    DoTest(convert("foo %ln bar"), 4, convert("foo  bar"));
    DoTest(convert("foo %Ln bar"), 4, convert("foo  bar"));
    DoTest(convert("foo %I64n bar"), 4, convert("foo  bar"));
    DoTest(convert("foo %20.3n bar"), 4, convert("foo  bar"));

    PAL_Terminate();
    return PASS;
}
