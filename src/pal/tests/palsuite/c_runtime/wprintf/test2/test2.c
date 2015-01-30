//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

