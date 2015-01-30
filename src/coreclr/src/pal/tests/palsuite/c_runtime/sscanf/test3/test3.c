//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test3.c
**
** Purpose: Tests sscanf with bracketed set strings
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

    DoStrTest("bar1", "%[a-z]", "bar");
    DoStrTest("bar1", "%[z-a]", "bar");
    DoStrTest("bar1", "%[ab]", "ba");
    DoStrTest("bar1", "%[ar1b]", "bar1");
    DoStrTest("bar1", "%[^4]", "bar1");
    DoStrTest("bar1", "%[^4a]", "b");

    PAL_Terminate();
    return PASS;
}
