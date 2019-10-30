// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test #1 for the vprintf function. A single, basic, test
**          case with no formatting.
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../vprintf.h"

int __cdecl main(int argc, char *argv[])
{
    char checkstr[] = "hello world";
    int ret;


    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    ret = vprintf("hello world", NULL);

    if (ret != strlen(checkstr))
    {
        Fail("Expected vprintf to return %d, got %d.\n", 
            strlen(checkstr), ret);

    }

    PAL_Terminate();
    return PASS;
}

