//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test12.c
**
** Purpose:  Tests sscanf with wide strings
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf.h"

int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoWStrTest("foo bar", "foo %S", convert("bar"));
    DoWStrTest("foo bar", "foo %2S", convert("ba"));
    DoStrTest("foo bar", "foo %hS", "bar");
    DoWStrTest("foo bar", "foo %lS", convert("bar"));
    DoWStrTest("foo bar", "foo %LS", convert("bar"));
    DoWStrTest("foo bar", "foo %I64S", convert("bar"));

    PAL_Terminate();
    return PASS;
}
