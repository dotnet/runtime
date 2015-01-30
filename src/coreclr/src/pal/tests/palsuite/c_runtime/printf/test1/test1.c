//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test #1 for the printf function. A single, basic, test
**          case with no formatting.
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../printf.h"

int __cdecl main(int argc, char *argv[])
{
    char checkstr[] = "hello world";
    int ret;


    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    ret = printf("hello world");

    if (ret != strlen(checkstr))
    {
        Fail("Expected printf to return %d, got %d.\n", 
            strlen(checkstr), ret);

    }

    PAL_Terminate();
    return PASS;
}

