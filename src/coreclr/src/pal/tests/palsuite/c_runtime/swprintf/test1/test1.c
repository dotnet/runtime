//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: General test to see if swprintf works correctly
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swprintf.h"

/*
 * Uses memcmp & wcslen
 */

int __cdecl main(int argc, char *argv[])
{
    WCHAR *checkstr;
    WCHAR buf[256];

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    checkstr = convert("hello world");
    swprintf(buf, convert("hello world"));

    if (memcmp(checkstr, buf, wcslen(checkstr)*2+2) != 0)
    {
        Fail("ERROR: Expected \"%s\", got \"%s\".\n", "hello world",
             convertC(buf));
    }

    PAL_Terminate();
    return PASS;
}
