//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test11.c
**
** Purpose: Tests sscanf with strings
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

    DoStrTest("foo bar", "foo %s", "bar");
    DoStrTest("foo bar", "foo %2s", "ba");
    DoStrTest("foo bar", "foo %hs", "bar");
    DoWStrTest("foo bar", "foo %ls", convert("bar"));
    DoStrTest("foo bar", "foo %Ls", "bar");
    DoStrTest("foo bar", "foo %I64s", "bar");

    PAL_Terminate();
    return PASS;
}
