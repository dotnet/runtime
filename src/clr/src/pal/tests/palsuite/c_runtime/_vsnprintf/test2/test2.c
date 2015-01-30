//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:    test2.c
**
** Purpose:   Test #2 for the _vsnprintf function.
**
**
**===================================================================*/ 
 
#include <palsuite.h>
#include "../_vsnprintf.h"
/*
 * Notes: memcmp is used, as is strlen.
 */

int __cdecl main(int argc, char *argv[])
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

    PAL_Terminate();
    return PASS;
}

