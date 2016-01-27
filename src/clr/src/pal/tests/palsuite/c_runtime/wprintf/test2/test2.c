// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Test #2 for the wprintf function. Tests the string specifier
**          (%s).
**
**
**==========================================================================*/


#include <palsuite.h>
#include "../wprintf.h"



int __cdecl main(int argc, char *argv[])
{

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    DoStrTest(u"foo %s", u"bar", u"foo bar");
    DoStrTest(u"foo %ws", u"bar", u"foo bar");
    DoStrTest(u"foo %ls", u"bar", u"foo bar");
    DoStrTest(u"foo %ws", u"bar", u"foo bar");
    DoStrTest(u"foo %Ls", u"bar", u"foo bar");
    DoStrTest(u"foo %I64s", u"bar", u"foo bar");
    DoStrTest(u"foo %5s", u"bar", u"foo   bar");
    DoStrTest(u"foo %.2s", u"bar", u"foo ba");
    DoStrTest(u"foo %5.2s", u"bar", u"foo    ba");
    DoStrTest(u"foo %-5s", u"bar", u"foo bar  ");
    DoStrTest(u"foo %05s", u"bar", u"foo 00bar");

    PAL_Terminate();
    return PASS;    
}

