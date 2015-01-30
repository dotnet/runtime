//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test3.c
**
** Purpose: Tests swscanf with bracketed set strings
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

    DoWStrTest(convert("bar1"), convert("%[a-z]"), convert("bar"));
    DoWStrTest(convert("bar1"), convert("%[z-a]"), convert("bar"));
    DoWStrTest(convert("bar1"), convert("%[ab]"), convert("ba"));
    DoWStrTest(convert("bar1"), convert("%[ar1b]"), convert("bar1"));
    DoWStrTest(convert("bar1"), convert("%[^4]"), convert("bar1"));
    DoWStrTest(convert("bar1"), convert("%[^4a]"), convert("b"));

    PAL_Terminate();
    return PASS;
}
