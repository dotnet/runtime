//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test12.c
**
** Purpose: Tests swscanf with wide strings
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swscanf.h"

int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoStrTest(convert("foo bar"), convert("foo %S"), "bar");
    DoStrTest(convert("foo bar"), convert("foo %2S"), "ba");
    DoStrTest(convert("foo bar"), convert("foo %hS"), "bar");
    DoWStrTest(convert("foo bar"), convert("foo %lS"), convert("bar"));
    DoStrTest(convert("foo bar"), convert("foo %LS"), "bar");
    DoStrTest(convert("foo bar"), convert("foo %I64S"), "bar");

    PAL_Terminate();
    return PASS;
}
