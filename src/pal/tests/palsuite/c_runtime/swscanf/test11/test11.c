//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test11.c
**
** Purpose: Tests swscanf with strings
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

    DoWStrTest(convert("foo bar"), convert("foo %s"), convert("bar"));
    DoWStrTest(convert("foo bar"), convert("foo %2s"), convert("ba"));
    DoStrTest(convert("foo bar"), convert("foo %hs"), "bar");
    DoWStrTest(convert("foo bar"), convert("foo %ls"), convert("bar"));
    DoWStrTest(convert("foo bar"), convert("foo %Ls"), convert("bar"));
    DoWStrTest(convert("foo bar"), convert("foo %I64s"), convert("bar"));

    PAL_Terminate();
    return PASS;
}
